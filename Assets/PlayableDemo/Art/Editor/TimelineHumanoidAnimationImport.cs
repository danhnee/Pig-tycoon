using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using UnityEditor;
using PackageInfo = UnityEditor.PackageManager.PackageInfo;
using UnityEngine;

namespace PigTycoon.PlayableDemo3D.EditorTools
{
    /// <summary>Isolated motion candidates; never changes an existing controller, prefab or scene.</summary>
    public static class TimelineHumanoidAnimationImport
    {
        public const string DefaultRoot = "Assets/PlayableDemo/Art/Animation/TimelineMotionCandidates";
        public const string StagedRoot = ".collab/artifacts/playable-demo-20261026/sol-asset-support/animation-retarget/staged";
        private const string RequiredUnity = "6000.6.3f1";
        private const string PackageLicenseHash = "73111ef76bd8a502c29b167865620de787af612b5f0ba6b3a517ade3b9519853";
        private const string FullLicenseHash = "3d223da697b5a03e6f2d990d885d88b1099fec2800b90e17fe042844bbcd15f6";
        private const string StanceHash = "8a30dbdbdfb2e9c0f853536bbfc7fec69e9f085600b7c36067504342bbb3d984";
        private const string NavigationHash = "0b74cb636b68ade09c257159d0f5064270ac9bf46860e0f64468ae9da381cce3";

        public sealed class Candidates
        {
            public Avatar SourceAvatar;
            public AnimationClip Idle, Walk, Jog, Sprint;
            public string NoticeDirectory;
            public string Root;
        }

        [Serializable]
        public sealed class ClipMetric
        {
            public string name, assetPath;
            public float durationSeconds, framesPerSecond, sourceHorizontalMetersPerSecond;
            public bool humanoid;
        }

        [Serializable]
        private sealed class ImportReceipt
        {
            public string status = "IMPORTED_CANDIDATES_GAIT_NOT_REVIEWED";
            public string packageVersion, packagePath, stanceSha256, navigationSha256;
            public string sourceAvatarAsset, noticesDirectory;
            public bool sourceAvatarValid, sourceAvatarHuman;
            public ClipMetric[] clips;
        }

        /// <summary>Import two animation-only FBXs. Source Avatar belongs to the Timeline hierarchy.</summary>
        public static Candidates ImportSources(string destinationRoot = DefaultRoot)
        {
            RequireEditorVersion();
            string root = RequireOwnedAssetRoot(destinationRoot);
            var package = PackageInfo.FindForAssetPath("Packages/com.unity.timeline/package.json");
            if (package == null || package.name != "com.unity.timeline")
                throw new InvalidOperationException("Installed Timeline package could not be resolved.");
            string sourceDirectory = Path.Combine(package.resolvedPath, "Samples~/GameplaySequenceDemo/Animation");
            string sourceStance = Path.Combine(sourceDirectory, "Stance.fbx");
            string sourceNavigation = Path.Combine(sourceDirectory, "Nav-Accelerations.fbx");
            VerifyHash(sourceStance, StanceHash);
            VerifyHash(sourceNavigation, NavigationHash);
            VerifyHash(Path.Combine(package.resolvedPath, "LICENSE.md"), PackageLicenseHash);
            string notices = root + "/Licenses/UnityTimeline";
            CopyStagedNotices(AssetFullPath(notices));
            Directory.CreateDirectory(AssetFullPath(root + "/Sources"));
            string stancePath = root + "/Sources/Stance.fbx";
            string navigationPath = root + "/Sources/Nav-Accelerations.fbx";
            CopyVerified(sourceStance, AssetFullPath(stancePath), StanceHash);
            CopyVerified(sourceNavigation, AssetFullPath(navigationPath), NavigationHash);
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);

            var stance = RequireModelImporter(stancePath);
            ConfigureAnimationImporter(stance);
            stance.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;
            stance.sourceAvatar = null;
            stance.clipAnimations = new[] { Clip("Timeline_IdleCandidate", "Stance", 0f, 150f, false) };
            stance.SaveAndReimport();
            Avatar sourceAvatar = RequireHumanAvatar(stancePath);

            var navigation = RequireModelImporter(navigationPath);
            ConfigureAnimationImporter(navigation);
            navigation.avatarSetup = ModelImporterAvatarSetup.CopyFromOther;
            navigation.sourceAvatar = sourceAvatar; // Same Timeline source hierarchy, never Khoa's Avatar.
            navigation.clipAnimations = new[]
            {
                Clip("Timeline_WalkCandidate", "Accelerations", 31f, 67f, true),
                Clip("Timeline_JogCandidate", "Accelerations", 126f, 149f, true),
                Clip("Timeline_SprintCandidate", "Accelerations", 598.3f, 616f, true)
            };
            navigation.SaveAndReimport();
            // CopyFromOther may reference the source asset instead of emitting
            // a second Avatar subasset. Validate the binding and Human clips.
            if (navigation.sourceAvatar != sourceAvatar || !sourceAvatar.isValid || !sourceAvatar.isHuman)
                throw new InvalidOperationException("Navigation clips lost their shared Timeline source Avatar.");
            RejectUnexpectedMesh(stancePath);
            RejectUnexpectedMesh(navigationPath);

            var result = new Candidates
            {
                Root = root, SourceAvatar = sourceAvatar, NoticeDirectory = notices,
                Idle = RequireClip(stancePath, "Timeline_IdleCandidate"),
                Walk = RequireClip(navigationPath, "Timeline_WalkCandidate"),
                Jog = RequireClip(navigationPath, "Timeline_JogCandidate"),
                Sprint = RequireClip(navigationPath, "Timeline_SprintCandidate")
            };
            var receipt = new ImportReceipt
            {
                packageVersion = package.version, packagePath = package.resolvedPath,
                stanceSha256 = StanceHash, navigationSha256 = NavigationHash,
                sourceAvatarAsset = stancePath, noticesDirectory = notices,
                sourceAvatarValid = sourceAvatar.isValid, sourceAvatarHuman = sourceAvatar.isHuman,
                clips = new[] { Metric(result.Idle), Metric(result.Walk), Metric(result.Jog), Metric(result.Sprint) }
            };
            string json = JsonUtility.ToJson(receipt, true);
            File.WriteAllText(AssetFullPath(root + "/timeline-motion-import-receipt.json"), json);
            File.WriteAllText(Path.Combine(AssetFullPath(notices), "timeline-motion-provenance.json"), json);
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            return result;
        }

        /// <summary>Validate an independently imported Khoa Humanoid; no source importer is changed.</summary>
        public static Avatar RequireKhoaHumanoid(string khoaModelAssetPath)
        {
            var importer = RequireModelImporter(khoaModelAssetPath);
            if (importer.animationType != ModelImporterAnimationType.Human ||
                importer.avatarSetup != ModelImporterAvatarSetup.CreateFromThisModel || importer.sourceAvatar != null)
                throw new InvalidOperationException("Khoa must create his own Humanoid Avatar; current Generic baseline is unchanged.");
            return RequireHumanAvatar(khoaModelAssetPath);
        }

        /// <summary>Optional separate Humanoid copy of owned Khoa, preserving the original Generic baseline.</summary>
        public static Avatar CreateKhoaHumanoidCandidate(string ownedKhoaModelAssetPath, string destinationRoot = DefaultRoot)
        {
            RequireEditorVersion();
            string root = RequireOwnedAssetRoot(destinationRoot);
            if (Path.GetFileName(ownedKhoaModelAssetPath) != "Khoa_Candidate.fbx")
                throw new ArgumentException("Only the owned Khoa_Candidate.fbx is accepted; no sample body geometry is copied.");
            RequireModelImporter(ownedKhoaModelAssetPath);
            string target = root + "/KhoaHumanoid/Khoa_MotionCandidate.fbx";
            Directory.CreateDirectory(AssetFullPath(root + "/KhoaHumanoid"));
            File.Copy(AssetFullPath(ownedKhoaModelAssetPath), AssetFullPath(target), true);
            AssetDatabase.ImportAsset(target, ImportAssetOptions.ForceSynchronousImport);
            var importer = RequireModelImporter(target);
            GameObject model = AssetDatabase.LoadAssetAtPath<GameObject>(target);
            if (model == null) throw new InvalidOperationException("Khoa candidate model did not import.");
            var transforms = model.GetComponentsInChildren<Transform>(true);
            var names = new HashSet<string>(transforms.Select(t => t.name));
            var humans = KhoaHumanMapping().Select(pair =>
            {
                if (!names.Contains(pair.Value)) throw new InvalidOperationException("Khoa source lacks bone " + pair.Value);
                return new HumanBone { humanName = pair.Key, boneName = pair.Value, limit = new HumanLimit { useDefaultValues = true } };
            }).ToArray();
            importer.animationType = ModelImporterAnimationType.Human;
            importer.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;
            importer.sourceAvatar = null;
            importer.importAnimation = false;
            importer.optimizeGameObjects = false;
            importer.humanDescription = new HumanDescription
            {
                human = humans,
                skeleton = transforms.Select(t => new SkeletonBone
                {
                    name = t.name,
                    position = t.localPosition, rotation = t.localRotation, scale = t.localScale
                }).ToArray(),
                upperArmTwist = 0.5f, lowerArmTwist = 0.5f, upperLegTwist = 0.5f, lowerLegTwist = 0.5f,
                armStretch = 0.05f, legStretch = 0.05f, feetSpacing = 0f, hasTranslationDoF = false
            };
            importer.SaveAndReimport();
            return RequireKhoaHumanoid(target);
        }

        public static ClipMetric Metric(AnimationClip clip)
        {
            if (clip == null) throw new ArgumentNullException(nameof(clip));
            Vector3 velocity = clip.averageSpeed;
            return new ClipMetric { name = clip.name, assetPath = AssetDatabase.GetAssetPath(clip),
                durationSeconds = clip.length, framesPerSecond = clip.frameRate,
                sourceHorizontalMetersPerSecond = new Vector2(velocity.x, velocity.z).magnitude,
                humanoid = clip.humanMotion };
        }

        /// <summary>Use measured retargeted stride speed; never change Core movement to fit the art.</summary>
        public static float PlaybackRate(float actualCoreMetersPerSecond, float measuredRetargetedMetersPerSecond)
        {
            if (float.IsNaN(actualCoreMetersPerSecond) || float.IsInfinity(actualCoreMetersPerSecond) || actualCoreMetersPerSecond < 0f ||
                float.IsNaN(measuredRetargetedMetersPerSecond) || float.IsInfinity(measuredRetargetedMetersPerSecond) || measuredRetargetedMetersPerSecond <= 0.001f)
                throw new ArgumentOutOfRangeException("Measured stride speed must be finite and positive.");
            return actualCoreMetersPerSecond / measuredRetargetedMetersPerSecond;
        }

        /// <summary>Runtime build owner calls after packaging. Can also be used before importing clips.</summary>
        public static string CopyDeliveryNotices(string buildDirectory, string importedNoticeDirectory = null)
        {
            string build = Path.GetFullPath(buildDirectory);
            if (!Directory.Exists(build)) throw new DirectoryNotFoundException("Build directory does not exist: " + build);
            string source = importedNoticeDirectory == null
                ? Path.Combine(ProjectRoot, StagedRoot, "Licenses/UnityTimeline")
                : AssetFullPath(importedNoticeDirectory);
            ValidateNoticeFiles(source);
            string destination = Path.Combine(build, "ThirdPartyNotices/UnityTimeline");
            Directory.CreateDirectory(destination);
            CopyNoticeFiles(source, destination);
            return destination;
        }

        private static Dictionary<string, string> KhoaHumanMapping()
        {
            var map = new Dictionary<string, string>
            {
                {"Hips", "Hips"}, {"Spine", "Spine"}, {"Chest", "Spine1"}, {"UpperChest", "Spine2"},
                {"Neck", "Neck"}, {"Head", "Head"}
            };
            foreach (string side in new[] { "Left", "Right" })
            {
                foreach (var pair in new[] { ("Shoulder", "Shoulder"), ("UpperArm", "Arm"), ("LowerArm", "ForeArm"),
                    ("Hand", "Hand"), ("UpperLeg", "UpLeg"), ("LowerLeg", "Leg"), ("Foot", "Foot"), ("Toes", "ToeBase") })
                    map.Add(side + pair.Item1, side + pair.Item2);
                foreach (var finger in new[] { ("Thumb", "Thumb"), ("Index", "Index"), ("Middle", "Middle"), ("Ring", "Ring"), ("Little", "Pinky") })
                {
                    string[] segments = { "Proximal", "Intermediate", "Distal" };
                    for (int index = 0; index < segments.Length; index++)
                        map.Add(side + " " + finger.Item1 + " " + segments[index], side + "Hand" + finger.Item2 + (index + 1));
                }
            }
            return map.ToDictionary(pair => pair.Key, pair => "mixamorig:" + pair.Value);
        }

        private static void ConfigureAnimationImporter(ModelImporter importer)
        {
            importer.animationType = ModelImporterAnimationType.Human;
            importer.importAnimation = true;
            importer.importCameras = false;
            importer.importLights = false;
            importer.optimizeGameObjects = false;
            importer.resampleCurves = true;
            importer.animationCompression = ModelImporterAnimationCompression.Off;
            importer.materialImportMode = ModelImporterMaterialImportMode.None;
        }

        private static ModelImporterClipAnimation Clip(string name, string take, float first, float last, bool blend)
        {
            return new ModelImporterClipAnimation
            {
                name = name, takeName = take, firstFrame = first, lastFrame = last,
                loopTime = true, loopPose = blend,
                keepOriginalOrientation = true, keepOriginalPositionY = true, keepOriginalPositionXZ = false,
                lockRootRotation = false, lockRootHeightY = false, lockRootPositionXZ = false,
                heightFromFeet = false
            };
        }

        private static ModelImporter RequireModelImporter(string path)
        {
            var importer = AssetImporter.GetAtPath(path) as ModelImporter;
            if (importer == null) throw new InvalidOperationException("Missing model importer: " + path);
            return importer;
        }

        private static Avatar RequireHumanAvatar(string path)
        {
            Avatar avatar = AssetDatabase.LoadAllAssetsAtPath(path).OfType<Avatar>().FirstOrDefault();
            if (avatar == null || !avatar.isValid || !avatar.isHuman)
                throw new InvalidOperationException("A valid Humanoid Avatar was not generated: " + path);
            return avatar;
        }

        private static AnimationClip RequireClip(string path, string name)
        {
            AnimationClip clip = AssetDatabase.LoadAllAssetsAtPath(path).OfType<AnimationClip>().FirstOrDefault(c => c.name == name);
            if (clip == null || !clip.humanMotion || clip.length <= 0f)
                throw new InvalidOperationException("Missing/non-Humanoid/empty clip " + name + " in " + path);
            return clip;
        }

        private static void RejectUnexpectedMesh(string path)
        {
            if (AssetDatabase.LoadAllAssetsAtPath(path).OfType<Mesh>().Any())
                throw new InvalidOperationException("Unexpected geometry in animation-only source " + path);
        }

        private static string ProjectRoot => Directory.GetParent(Application.dataPath).FullName;
        private static string AssetFullPath(string path) => Path.GetFullPath(Path.Combine(ProjectRoot, path));

        private static string RequireOwnedAssetRoot(string root)
        {
            root = root.Replace('\\', '/').TrimEnd('/');
            string full = AssetFullPath(root);
            string owned = AssetFullPath(DefaultRoot).TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;
            if (full != AssetFullPath(DefaultRoot) && !full.StartsWith(owned, StringComparison.OrdinalIgnoreCase))
                throw new ArgumentException("Motion candidates must remain inside " + DefaultRoot);
            return Path.GetRelativePath(ProjectRoot, full).Replace('\\', '/');
        }

        private static void RequireEditorVersion()
        {
            if (Application.unityVersion != RequiredUnity)
                throw new InvalidOperationException("Expected Unity " + RequiredUnity + "; got " + Application.unityVersion);
        }

        private static void CopyStagedNotices(string destination)
        {
            string source = Path.Combine(ProjectRoot, StagedRoot, "Licenses/UnityTimeline");
            ValidateNoticeFiles(source);
            Directory.CreateDirectory(destination);
            CopyNoticeFiles(source, destination);
        }

        private static void CopyNoticeFiles(string source, string destination)
        {
            foreach (string file in Directory.GetFiles(source))
                if (!file.EndsWith(".meta", StringComparison.OrdinalIgnoreCase))
                    File.Copy(file, Path.Combine(destination, Path.GetFileName(file)), true);
        }

        private static void ValidateNoticeFiles(string directory)
        {
            VerifyHash(Path.Combine(directory, "UnityTimeline-LICENSE.md"), PackageLicenseHash);
            VerifyHash(Path.Combine(directory, "UNITY-COMPANION-LICENSE-v1.4.txt"), FullLicenseHash);
        }

        private static void CopyVerified(string source, string destination, string hash)
        {
            if (!File.Exists(destination) || Hash(destination) != hash) File.Copy(source, destination, true);
            VerifyHash(destination, hash);
        }

        private static void VerifyHash(string path, string expected)
        {
            if (!File.Exists(path) || Hash(path) != expected)
                throw new InvalidOperationException("Missing or changed pinned source/notice: " + path);
        }

        private static string Hash(string path)
        {
            using (var sha = SHA256.Create())
            using (var stream = File.OpenRead(path))
                return BitConverter.ToString(sha.ComputeHash(stream)).Replace("-", "").ToLowerInvariant();
        }
    }
}
