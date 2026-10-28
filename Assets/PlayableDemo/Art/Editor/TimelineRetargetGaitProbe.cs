using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;
using UnityEngine.SceneManagement;

namespace PigTycoon.PlayableDemo3D.EditorTools
{
    /// <summary>Isolated native measurements only. Never adopts clips or saves a gameplay scene.</summary>
    public static class TimelineRetargetGaitProbe
    {
        [Serializable]
        public sealed class Request
        {
            public string sourceAvatarModel = TimelineHumanoidAnimationImport.DefaultRoot + "/Sources/Stance.fbx";
            public string targetHumanModel = TimelineHumanoidAnimationImport.DefaultRoot + "/KhoaHumanoid/Khoa_MotionCandidate.fbx";
            public string originalGenericModel = "Assets/PlayableDemo/Art/Characters/Khoa_Candidate.fbx";
            public string originalController = "Assets/PlayableDemo/Art/Animators/KhoaAnimator.controller";
            public string outputDirectory = ".collab/artifacts/playable-demo-20261026/sol-asset-support/animation-retarget/gait-native-check";
            // Explicit current Core MovementSpec inputs, not proposed balance changes.
            public float actualWalkMetersPerSecond = 4.5f, actualRunMetersPerSecond = 7f;
            public float fixedDeltaSeconds = 1f / 60f;
            public int loops = 2;
            // Evaluation choices, not GDD constants or physical contact ground truth.
            public float groundY = 0f, contactHeightBandMeters = 0.025f;
            public float contactVerticalSpeedMetersPerSecond = 0.15f, soleBandMeters = 0.015f;
            public string bodyRendererName = "Khoa_Body", bootRendererName = "Khoa_Boots";
        }

        [Serializable]
        public sealed class Frame
        {
            public float seconds, clipSeconds;
            public Vector3 actorRoot, deltaPosition, leftFootBone, rightFootBone, leftSole, rightSole;
            public float leftMinimumY, rightMinimumY, bodyDeformationMeters;
            public bool deltaPositionValid;
        }

        [Serializable]
        public sealed class FootMetrics
        {
            public int inferredContactIntervals;
            public bool contactObservationSufficient;
            public float contactDriftMeters, meanContactDriftMetersPerSecond, maximumContactDriftMetersPerSecond;
            public float minimumSoleY, maximumSoleY, maximumGroundPenetrationMeters;
        }

        [Serializable]
        public sealed class Measurement
        {
            public string label, modelPath, clipPath, clipName, mode;
            public bool humanoid, applyFootIK, actualBodyDeforms;
            public float clipLength, fixedDeltaSeconds, imposedMotorMetersPerSecond, playbackRate;
            public float rootPathMeters, deltaPositionPathMeters, observedNaturalRootMetersPerSecond;
            public float maximumBodyDeformationMeters, bodyLoopSeamMeters, deltaPositionVsObservedRootPathDifferenceMeters;
            public int bodyVertexCount, samples;
            public FootMetrics left, right;
            public string csvPath;
        }

        [Serializable]
        public sealed class ControllerObservation
        {
            public string status, assetPath;
            public string[] conditions, stateSpeedBindings;
            public float testedWalkMetersPerSecond, testedRunMetersPerSecond;
            public string observedWalkState, observedRunState;
            public bool ordinaryWalkSelectsRun;
            public bool interactAdvancesAtRest, interactSpeedIndependentOfMovement;
            public bool attackStatePresent, idleAndActionSpeedBindingsSafe, usesIsRunningParameter;
        }

        [Serializable]
        public sealed class Report
        {
            public string status = "MEASUREMENTS_ONLY_VISUAL_REVIEW_REQUIRED";
            public bool sourceHumanValid, targetHumanValid, sourceAndTargetAvatarsDifferent;
            public string actualUnityVersion;
            public Request settings;
            public Measurement[] measurements;
            public ControllerObservation baselineController;
            public string[] findings;
        }

        public static void RunFromCommandLine()
        {
            var args = Environment.GetCommandLineArgs();
            int index = Array.IndexOf(args, "-gaitProbeRequest");
            var request = index >= 0 && index + 1 < args.Length
                ? JsonUtility.FromJson<Request>(File.ReadAllText(args[index + 1])) : new Request();
            Run(request);
        }

        public static Report Run(Request request)
        {
            ValidateRequest(request);
            Avatar source = RequireAvatar(request.sourceAvatarModel, true);
            Avatar target = TimelineHumanoidAnimationImport.RequireKhoaHumanoid(request.targetHumanModel);
            if (source == target) throw new InvalidOperationException("Source and Khoa target Avatars must be independent.");
            RequireAvatar(request.originalGenericModel, false);
            Directory.CreateDirectory(Path.GetFullPath(request.outputDirectory));
            var findings = new List<string>();
            var observations = new List<Measurement>();
            string nav = TimelineHumanoidAnimationImport.DefaultRoot + "/Sources/Nav-Accelerations.fbx";
            var candidates = new[]
            {
                ("Idle", Clip(request.sourceAvatarModel, "Timeline_IdleCandidate"), 0f),
                ("Walk", Clip(nav, "Timeline_WalkCandidate"), request.actualWalkMetersPerSecond),
                ("Jog", Clip(nav, "Timeline_JogCandidate"), request.actualRunMetersPerSecond),
                ("Sprint", Clip(nav, "Timeline_SprintCandidate"), request.actualRunMetersPerSecond)
            };
            foreach (var candidate in candidates)
            {
                // The source hierarchy supplies a stride reference without requiring a sample body.
                observations.Add(Measure(request, request.sourceAvatarModel, candidate.Item2,
                    "Source_" + candidate.Item1, true, 0f, 1f, false));
                var natural = Measure(request, request.targetHumanModel, candidate.Item2,
                    "Target_" + candidate.Item1 + "_Natural", true, 0f, 1f, true);
                observations.Add(natural);
                if (candidate.Item3 > 0f)
                {
                    if (natural.observedNaturalRootMetersPerSecond <= 0.001f)
                    {
                        findings.Add(candidate.Item1 + ": no positive measured target stride; motor-rate comparison was NOT guessed.");
                        continue;
                    }
                    float rate = TimelineHumanoidAnimationImport.PlaybackRate(candidate.Item3, natural.observedNaturalRootMetersPerSecond);
                    observations.Add(Measure(request, request.targetHumanModel, candidate.Item2,
                        "Target_" + candidate.Item1 + "_Motor", false, candidate.Item3, rate, true));
                }
            }
            foreach (var baseline in new[] { ("Idle", 0f), ("Walk", request.actualWalkMetersPerSecond),
                ("Run", request.actualRunMetersPerSecond), ("Interact", 0f) })
            {
                string path = "Assets/PlayableDemo/Art/Characters/Khoa@" + baseline.Item1 + ".fbx";
                observations.Add(Measure(request, request.originalGenericModel, Clip(path, baseline.Item1),
                    "Baseline_" + baseline.Item1 + "_Motor", false, baseline.Item2, 1f, true));
            }
            var controller = ObserveController(request);
            if (controller.ordinaryWalkSelectsRun)
                findings.Add("Existing baseline controller selects Run at actual walking speed; locomotion state selection requires owner correction.");
            if (!controller.attackStatePresent) findings.Add("Baseline controller has no Attack state; attack cadence could not be tested.");
            foreach (var measurement in observations.Where(m => m.left != null))
            {
                if (!measurement.left.contactObservationSufficient || !measurement.right.contactObservationSufficient)
                    findings.Add(measurement.label + ": insufficient inferred sole contact observations; a low drift number cannot establish planted feet.");
                if (!measurement.actualBodyDeforms && !measurement.clipName.Contains("Idle"))
                    findings.Add(measurement.label + ": selected actual body skin did not show useful deformation.");
            }
            var report = new Report
            {
                sourceHumanValid = source.isValid && source.isHuman, targetHumanValid = target.isValid && target.isHuman,
                sourceAndTargetAvatarsDifferent = source != target, actualUnityVersion = Application.unityVersion,
                settings = request, measurements = observations.ToArray(), baselineController = controller, findings = findings.ToArray()
            };
            File.WriteAllText(Path.Combine(Path.GetFullPath(request.outputDirectory), "gait-probe-report.json"), JsonUtility.ToJson(report, true));
            Debug.Log("Gait measurements written; visual motion review and gameplay capture are still required.");
            return report;
        }

        private static Measurement Measure(Request request, string modelPath, AnimationClip clip, string label,
            bool naturalRoot, float motorSpeed, float rate, bool requireSkin)
        {
            Scene preview = EditorSceneManager.NewPreviewScene();
            PlayableGraph graph = default;
            GameObject motor = null;
            Mesh bodyBake = null, bootBake = null;
            try
            {
                motor = new GameObject("IsolatedGaitMotor");
                SceneManager.MoveGameObjectToScene(motor, preview);
                GameObject asset = AssetDatabase.LoadAssetAtPath<GameObject>(modelPath);
                if (asset == null) throw new InvalidOperationException("Missing model: " + modelPath);
                var instance = UnityEngine.Object.Instantiate(asset, motor.transform);
                instance.transform.localPosition = Vector3.zero;
                instance.transform.localRotation = Quaternion.identity;
                var animator = instance.GetComponentInChildren<Animator>();
                if (animator == null) animator = instance.AddComponent<Animator>();
                animator.avatar = RequireAvatar(modelPath, clip.humanMotion);
                animator.runtimeAnimatorController = null;
                animator.applyRootMotion = naturalRoot;
                animator.speed = 1f; // This isolated clip is time-scaled through its playable only.
                animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
                animator.Rebind();
                var renderers = instance.GetComponentsInChildren<SkinnedMeshRenderer>(true);
                var body = renderers.FirstOrDefault(r => r.name == request.bodyRendererName);
                var boots = renderers.FirstOrDefault(r => r.name == request.bootRendererName);
                if (requireSkin && (body == null || boots == null))
                    throw new InvalidOperationException("Expected actual body/boots renderer missing on " + modelPath + ": " + request.bodyRendererName + ", " + request.bootRendererName);
                Transform leftFoot = null, rightFoot = null;
                int[] leftSole = null, rightSole = null;
                if (requireSkin)
                {
                    var transforms = instance.GetComponentsInChildren<Transform>(true);
                    leftFoot = transforms.Single(t => t.name == "mixamorig:LeftFoot");
                    rightFoot = transforms.Single(t => t.name == "mixamorig:RightFoot");
                    leftSole = SelectSole(boots, "Left", request.soleBandMeters);
                    rightSole = SelectSole(boots, "Right", request.soleBandMeters);
                    bodyBake = new Mesh(); bootBake = new Mesh();
                }
                graph = PlayableGraph.Create("IsolatedGait_" + label);
                graph.SetTimeUpdateMode(DirectorUpdateMode.Manual);
                var playable = AnimationClipPlayable.Create(graph, clip);
                playable.SetApplyFootIK(false);
                playable.SetApplyPlayableIK(false);
                playable.SetSpeed(rate);
                var output = AnimationPlayableOutput.Create(graph, "ActualTargetSkin", animator);
                output.SetSourcePlayable(playable);
                graph.Play();
                graph.Evaluate(0f);
                int loops = clip.isLooping ? request.loops : 1;
                float duration = clip.length * loops / rate;
                int steps = Mathf.CeilToInt(duration / request.fixedDeltaSeconds);
                if (steps > 3600) throw new InvalidOperationException("Probe exceeds bounded 3600-frame case budget.");
                var frames = new List<Frame>(steps + 1);
                Vector3[] initialBody = null, seamBody = null;
                float maximumDeformation = 0f, elapsed = 0f;
                for (int index = 0; index <= steps; index++)
                {
                    float delta = index == 0 ? 0f : Mathf.Min(request.fixedDeltaSeconds, duration - elapsed);
                    if (index > 0) { graph.Evaluate(delta); elapsed += delta; }
                    if (!naturalRoot) motor.transform.position = Vector3.forward * (motorSpeed * elapsed);
                    var frame = new Frame
                    {
                        seconds = elapsed, clipSeconds = elapsed * rate, actorRoot = animator.transform.position,
                        deltaPosition = naturalRoot ? animator.deltaPosition : Vector3.zero, deltaPositionValid = naturalRoot
                    };
                    if (!Finite(frame.actorRoot) || !Finite(frame.deltaPosition))
                        throw new InvalidOperationException("Nonfinite evaluated actor/root-motion sample.");
                    if (requireSkin)
                    {
                        body.BakeMesh(bodyBake, false);
                        Vector3[] local = bodyBake.vertices;
                        for (int vi = 0; vi < local.Length; vi++)
                        {
                            local[vi] = animator.transform.InverseTransformPoint(body.transform.TransformPoint(local[vi]));
                            if (!Finite(local[vi])) throw new InvalidOperationException("Nonfinite actual body skin sample.");
                        }
                        if (initialBody == null) initialBody = local;
                        frame.bodyDeformationMeters = MaximumDistance(initialBody, local);
                        maximumDeformation = Mathf.Max(maximumDeformation, frame.bodyDeformationMeters);
                        if (index == steps) seamBody = local;
                        boots.BakeMesh(bootBake, false);
                        Vector3[] bootVertices = bootBake.vertices;
                        frame.leftSole = SolePoint(boots, bootVertices, leftSole, out frame.leftMinimumY);
                        frame.rightSole = SolePoint(boots, bootVertices, rightSole, out frame.rightMinimumY);
                        frame.leftFootBone = leftFoot.position; frame.rightFootBone = rightFoot.position;
                    }
                    frames.Add(frame);
                }
                float path = 0f, deltaPath = 0f;
                for (int index = 1; index < frames.Count; index++)
                {
                    path += HorizontalDistance(frames[index - 1].actorRoot, frames[index].actorRoot);
                    deltaPath += Horizontal(frames[index].deltaPosition).magnitude;
                }
                string csv = Path.Combine(Path.GetFullPath(request.outputDirectory), label + ".csv");
                WriteCsv(csv, frames);
                return new Measurement
                {
                    label = label, modelPath = modelPath, clipPath = AssetDatabase.GetAssetPath(clip), clipName = clip.name,
                    mode = naturalRoot ? "ROOT_MOTION_PREVIEW" : "EXTERNAL_MOTOR_PREVIEW",
                    humanoid = clip.humanMotion, applyFootIK = false, clipLength = clip.length,
                    fixedDeltaSeconds = request.fixedDeltaSeconds, imposedMotorMetersPerSecond = motorSpeed, playbackRate = rate,
                    rootPathMeters = path, deltaPositionPathMeters = deltaPath,
                    observedNaturalRootMetersPerSecond = naturalRoot ? path / duration : 0f,
                    deltaPositionVsObservedRootPathDifferenceMeters = naturalRoot ? Mathf.Abs(path - deltaPath) : 0f,
                    bodyVertexCount = initialBody == null ? 0 : initialBody.Length, samples = frames.Count,
                    maximumBodyDeformationMeters = maximumDeformation, actualBodyDeforms = maximumDeformation > 0.002f,
                    bodyLoopSeamMeters = clip.isLooping && requireSkin ? MaximumDistance(initialBody, seamBody) : 0f,
                    left = requireSkin ? Foot(request, frames, true) : null,
                    right = requireSkin ? Foot(request, frames, false) : null, csvPath = csv
                };
            }
            finally
            {
                if (graph.IsValid()) graph.Destroy();
                if (bodyBake != null) UnityEngine.Object.DestroyImmediate(bodyBake);
                if (bootBake != null) UnityEngine.Object.DestroyImmediate(bootBake);
                if (motor != null) UnityEngine.Object.DestroyImmediate(motor);
                EditorSceneManager.ClosePreviewScene(preview);
            }
        }

        private static int[] SelectSole(SkinnedMeshRenderer renderer, string side, float band)
        {
            BoneWeight[] weights = renderer.sharedMesh.boneWeights;
            Vector3[] vertices = renderer.sharedMesh.vertices;
            var footBones = new HashSet<int>(renderer.bones.Select((bone, index) => (bone, index))
                .Where(pair => pair.bone != null && (pair.bone.name == "mixamorig:" + side + "Foot" || pair.bone.name == "mixamorig:" + side + "ToeBase"))
                .Select(pair => pair.index));
            if (weights.Length != vertices.Length || footBones.Count == 0)
                throw new InvalidOperationException("Missing readable actual boot weights/bones for " + side);
            var foot = Enumerable.Range(0, vertices.Length).Where(index => FootWeight(weights[index], footBones) > 0.5f).ToArray();
            if (foot.Length == 0) throw new InvalidOperationException("No foot-weighted boot vertices for " + side);
            float minimum = foot.Min(index => renderer.transform.TransformPoint(vertices[index]).y);
            var sole = foot.Where(index => renderer.transform.TransformPoint(vertices[index]).y <= minimum + band).ToArray();
            if (sole.Length < 3) throw new InvalidOperationException("Too few actual sole vertices for " + side);
            return sole;
        }

        private static float FootWeight(BoneWeight weight, HashSet<int> bones)
            => (bones.Contains(weight.boneIndex0) ? weight.weight0 : 0f) + (bones.Contains(weight.boneIndex1) ? weight.weight1 : 0f) +
               (bones.Contains(weight.boneIndex2) ? weight.weight2 : 0f) + (bones.Contains(weight.boneIndex3) ? weight.weight3 : 0f);

        private static Vector3 SolePoint(SkinnedMeshRenderer renderer, Vector3[] vertices, int[] indices, out float minimumY)
        {
            Vector3 sum = Vector3.zero; minimumY = float.PositiveInfinity;
            foreach (int index in indices)
            {
                Vector3 world = renderer.transform.TransformPoint(vertices[index]);
                if (!Finite(world)) throw new InvalidOperationException("Nonfinite deformed sole sample.");
                sum += world; minimumY = Mathf.Min(minimumY, world.y);
            }
            return sum / indices.Length;
        }

        private static FootMetrics Foot(Request request, List<Frame> frames, bool left)
        {
            var result = new FootMetrics { minimumSoleY = float.PositiveInfinity, maximumSoleY = float.NegativeInfinity };
            float contactTime = 0f;
            bool previousContact = false;
            for (int index = 0; index < frames.Count; index++)
            {
                var frame = frames[index];
                float minimum = left ? frame.leftMinimumY : frame.rightMinimumY;
                Vector3 point = left ? frame.leftSole : frame.rightSole;
                result.minimumSoleY = Mathf.Min(result.minimumSoleY, minimum);
                result.maximumSoleY = Mathf.Max(result.maximumSoleY, minimum);
                result.maximumGroundPenetrationMeters = Mathf.Max(result.maximumGroundPenetrationMeters, request.groundY - minimum);
                if (index == 0) continue;
                Vector3 previous = left ? frames[index - 1].leftSole : frames[index - 1].rightSole;
                float dt = frame.seconds - frames[index - 1].seconds;
                if (dt <= 0f) continue;
                bool contact = minimum <= request.groundY + request.contactHeightBandMeters &&
                    Mathf.Abs(point.y - previous.y) / dt <= request.contactVerticalSpeedMetersPerSecond;
                if (contact && previousContact)
                {
                    float drift = HorizontalDistance(point, previous);
                    result.contactDriftMeters += drift; contactTime += dt; result.inferredContactIntervals++;
                    result.maximumContactDriftMetersPerSecond = Mathf.Max(result.maximumContactDriftMetersPerSecond, drift / dt);
                }
                previousContact = contact;
            }
            result.contactObservationSufficient = result.inferredContactIntervals >= 3;
            result.meanContactDriftMetersPerSecond = contactTime > 0f ? result.contactDriftMeters / contactTime : 0f;
            return result;
        }

        private static ControllerObservation ObserveController(Request request)
        {
            var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(request.originalController);
            var result = new ControllerObservation { assetPath = request.originalController,
                testedWalkMetersPerSecond = request.actualWalkMetersPerSecond, testedRunMetersPerSecond = request.actualRunMetersPerSecond };
            if (controller == null) { result.status = "MISSING_CONTROLLER_NATIVE_AUDIT_PENDING"; return result; }
            var states = controller.layers.SelectMany(layer => layer.stateMachine.states).Select(child => child.state).ToArray();
            result.conditions = states.SelectMany(state => state.transitions.SelectMany(transition => transition.conditions.Select(condition =>
                state.name + " -> " + (transition.destinationState == null ? "submachine" : transition.destinationState.name) + ": " +
                condition.parameter + " " + condition.mode + " " + condition.threshold.ToString("R", CultureInfo.InvariantCulture)))).ToArray();
            result.stateSpeedBindings = states.Select(state => state.name + ": speed=" + state.speed + ", speedParameterActive=" +
                state.speedParameterActive + ", parameter=" + state.speedParameter).ToArray();
            result.attackStatePresent = states.Any(state => state.name == "Attack");
            result.idleAndActionSpeedBindingsSafe = states.Where(state => state.name == "Idle" || state.name == "Interact" || state.name == "Attack")
                .All(state => !state.speedParameterActive && Mathf.Abs(state.speed - 1f) < 1e-6f);
            result.usesIsRunningParameter = controller.parameters.Any(parameter => parameter.name == "IsRunning" && parameter.type == AnimatorControllerParameterType.Bool);
            result.observedWalkState = ObserveState(request.originalGenericModel, controller, states, request.actualWalkMetersPerSecond, false, false, out _);
            result.observedRunState = ObserveState(request.originalGenericModel, controller, states, request.actualRunMetersPerSecond, true, false, out _);
            ObserveState(request.originalGenericModel, controller, states, 0f, false, true, out float restProgress);
            ObserveState(request.originalGenericModel, controller, states, request.actualRunMetersPerSecond, true, true, out float movingProgress);
            result.interactAdvancesAtRest = restProgress > 0f;
            result.interactSpeedIndependentOfMovement = restProgress > 0f && Mathf.Abs(restProgress - movingProgress) < 0.01f;
            result.ordinaryWalkSelectsRun = result.observedWalkState == "Run";
            result.status = "OBSERVED_UNCHANGED_BASELINE";
            return result;
        }

        private static string ObserveState(string modelPath, AnimatorController controller, AnimatorState[] states,
            float speed, bool isRunning, bool interact, out float interactProgress)
        {
            Scene preview = EditorSceneManager.NewPreviewScene();
            GameObject instance = null; interactProgress = 0f;
            try
            {
                instance = UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(modelPath));
                SceneManager.MoveGameObjectToScene(instance, preview);
                var animator = instance.GetComponentInChildren<Animator>() ?? instance.AddComponent<Animator>();
                animator.avatar = RequireAvatar(modelPath, false); animator.runtimeAnimatorController = controller;
                animator.applyRootMotion = false; animator.speed = 1f; animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
                animator.Rebind(); animator.SetFloat("Speed", speed);
                if (controller.parameters.Any(parameter => parameter.name == "IsRunning" && parameter.type == AnimatorControllerParameterType.Bool))
                    animator.SetBool("IsRunning", isRunning);
                foreach (string parameterName in new[] { "WalkPlaybackRate", "RunPlaybackRate" })
                    if (controller.parameters.Any(parameter => parameter.name == parameterName && parameter.type == AnimatorControllerParameterType.Float))
                        animator.SetFloat(parameterName, 1f);
                for (int frame = 0; frame < 90; frame++) animator.Update(1f / 60f);
                if (interact)
                {
                    animator.SetTrigger("Interact"); animator.Update(0.12f);
                    var first = animator.GetCurrentAnimatorStateInfo(0);
                    animator.Update(0.20f);
                    var second = animator.GetCurrentAnimatorStateInfo(0);
                    if (first.shortNameHash == Animator.StringToHash("Interact") && second.shortNameHash == first.shortNameHash)
                        interactProgress = second.normalizedTime - first.normalizedTime;
                }
                int hash = animator.GetCurrentAnimatorStateInfo(0).shortNameHash;
                return states.FirstOrDefault(state => Animator.StringToHash(state.name) == hash)?.name ?? "UNKNOWN_STATE";
            }
            finally
            {
                if (instance != null) UnityEngine.Object.DestroyImmediate(instance);
                EditorSceneManager.ClosePreviewScene(preview);
            }
        }

        private static Avatar RequireAvatar(string path, bool human)
        {
            Avatar avatar = AssetDatabase.LoadAllAssetsAtPath(path).OfType<Avatar>().FirstOrDefault();
            if (avatar == null || !avatar.isValid || avatar.isHuman != human)
                throw new InvalidOperationException("Missing/invalid " + (human ? "Humanoid" : "Generic") + " Avatar: " + path);
            return avatar;
        }

        private static AnimationClip Clip(string path, string name)
        {
            var clip = AssetDatabase.LoadAllAssetsAtPath(path).OfType<AnimationClip>().FirstOrDefault(candidate => candidate.name == name);
            if (clip == null || clip.length <= 0f) throw new InvalidOperationException("Missing/nonpositive clip " + name + " in " + path);
            return clip;
        }

        private static float MaximumDistance(Vector3[] first, Vector3[] second)
        {
            if (first.Length != second.Length) throw new InvalidOperationException("Actual body vertex count changed during sampling.");
            float maximum = 0f;
            for (int index = 0; index < first.Length; index++) maximum = Mathf.Max(maximum, Vector3.Distance(first[index], second[index]));
            return maximum;
        }

        private static Vector3 Horizontal(Vector3 value) => new Vector3(value.x, 0f, value.z);
        private static float HorizontalDistance(Vector3 first, Vector3 second) => Horizontal(first - second).magnitude;
        private static bool Finite(Vector3 value) => !(float.IsNaN(value.x) || float.IsInfinity(value.x) || float.IsNaN(value.y) ||
            float.IsInfinity(value.y) || float.IsNaN(value.z) || float.IsInfinity(value.z));

        private static void ValidateRequest(Request request)
        {
            if (Application.unityVersion != "6000.6.3f1") throw new InvalidOperationException("Pinned Unity 6000.6.3f1 required.");
            if (request == null) throw new ArgumentNullException(nameof(request));
            float[] values = { request.fixedDeltaSeconds, request.actualWalkMetersPerSecond, request.actualRunMetersPerSecond,
                request.groundY, request.contactHeightBandMeters, request.contactVerticalSpeedMetersPerSecond, request.soleBandMeters };
            if (values.Any(value => float.IsNaN(value) || float.IsInfinity(value)) ||
                request.fixedDeltaSeconds <= 0f || request.fixedDeltaSeconds > 0.05f || request.loops < 1 || request.loops > 4 ||
                request.actualWalkMetersPerSecond <= 0f || request.actualRunMetersPerSecond <= 0f ||
                request.contactHeightBandMeters <= 0f || request.contactVerticalSpeedMetersPerSecond <= 0f || request.soleBandMeters <= 0f)
                throw new ArgumentException("Invalid bounded gait probe settings.");
            string artifactRoot = Path.GetFullPath(".collab/artifacts/playable-demo-20261026/sol-asset-support/animation-retarget") + Path.DirectorySeparatorChar;
            if (!Path.GetFullPath(request.outputDirectory).StartsWith(artifactRoot, StringComparison.OrdinalIgnoreCase))
                throw new ArgumentException("Gait probe output must remain inside its owned artifact directory.");
        }

        private static void WriteCsv(string path, List<Frame> frames)
        {
            var text = new StringBuilder("seconds,clipSeconds,rootX,rootY,rootZ,deltaValid,deltaX,deltaY,deltaZ,leftBoneX,leftBoneY,leftBoneZ,rightBoneX,rightBoneY,rightBoneZ,leftSoleX,leftSoleY,leftSoleZ,leftMinY,rightSoleX,rightSoleY,rightSoleZ,rightMinY,bodyDeformationMeters\n");
            foreach (var frame in frames)
            {
                float[] values = { frame.seconds, frame.clipSeconds, frame.actorRoot.x, frame.actorRoot.y, frame.actorRoot.z,
                    frame.deltaPositionValid ? 1f : 0f, frame.deltaPosition.x, frame.deltaPosition.y, frame.deltaPosition.z,
                    frame.leftFootBone.x, frame.leftFootBone.y, frame.leftFootBone.z,
                    frame.rightFootBone.x, frame.rightFootBone.y, frame.rightFootBone.z,
                    frame.leftSole.x, frame.leftSole.y, frame.leftSole.z, frame.leftMinimumY,
                    frame.rightSole.x, frame.rightSole.y, frame.rightSole.z, frame.rightMinimumY, frame.bodyDeformationMeters };
                text.AppendLine(string.Join(",", values.Select(value => value.ToString("R", CultureInfo.InvariantCulture))));
            }
            File.WriteAllText(path, text.ToString());
        }
    }
}
