using System.Collections.Generic;
using UnityEngine;

namespace PigTycoon.PlayableDemo3D
{
    public readonly struct FarmLocomotionAnimationSample
    {
        public float HorizontalSpeed { get; }
        public bool IsRunning { get; }
        public float WalkPlaybackRate { get; }
        public float RunPlaybackRate { get; }
        public FarmLocomotionAnimationSample(float dx, float dy, float dz, float seconds, bool actualCoreRun, float walkReference, float runReference)
        {
            // Vertical grounding/gravity never selects or speeds up a gait.
            HorizontalSpeed = seconds > 0 ? (float)System.Math.Sqrt(dx * dx + dz * dz) / seconds : 0f;
            IsRunning = actualCoreRun;
            WalkPlaybackRate = Rate(HorizontalSpeed, walkReference);
            RunPlaybackRate = Rate(HorizontalSpeed, runReference);
        }
        private static float Rate(float speed, float measuredReference) => speed > 0f && measuredReference > 0f &&
            !float.IsNaN(measuredReference) && !float.IsInfinity(measuredReference) ? speed / measuredReference : 1f;
    }

    /// <summary>Identity and animation binding for an authored prefab; never a second simulation.</summary>
    public sealed class PlayableFarmActor : MonoBehaviour
    {
        public string CorePigId = "";
        public string CoreEnemyId = "";
        public Animator Animator;
        public string SpeedParameter = "Speed";
        public string IsRunningParameter = "IsRunning";
        public string WalkPlaybackRateParameter = "WalkPlaybackRate";
        public string RunPlaybackRateParameter = "RunPlaybackRate";
        [Tooltip("Measured clip root/stride speed at authored rate1. Leave0 until measured.")]
        public float WalkReferenceMetersPerSecond;
        public float RunReferenceMetersPerSecond;
        public bool IsRunning { get; private set; }
        public float HorizontalSpeed { get; private set; }
        public string InteractTrigger = "Interact";
        public string EatTrigger = "Eat";
        public string AttackTrigger = "Attack";
        public string TelegraphTrigger = "Telegraph", HitTrigger = "Hit", BindTrigger = "Bind", DeathTrigger = "Death", WhistleTrigger = "Whistle";
        public string IsDraggingParameter = "IsDragging", IsDeadParameter = "IsDead";
        public AnimationClip DeathClip;
        private Vector3 previousPosition;
        private bool hasSpeedParameter, hasInteractTrigger, hasEatTrigger;
        private bool hasAttackTrigger;
        private bool hasRunningParameter, hasWalkPlaybackRate, hasRunPlaybackRate;
        private bool hasDraggingParameter, hasDeadParameter;
        private readonly HashSet<string> optionalTriggers = new HashSet<string>();

        private Transform weaponBone;
        private Transform rightShoulderBone, rightUpperArmBone, rightLowerArmBone, spineBone;
        private GameObject[] weaponVisuals;
        private Renderer defaultWeaponMeshRenderer;
        private GameObject guardAuraObj;
        private int activeWeaponIndex = -1;
        private int comboStep;
        private float attackAnimTimer;
        private float attackAnimDuration = 0.36f;
        private bool isSkillSpin;
        private float parryGuardTimer;
        private float dashVisualTimer;

        private void Start()
        {
            if (Animator == null) Animator = GetComponentInChildren<Animator>();
            // Gameplay position belongs to the Core/motor; authoring root-motion curves remain intact.
            if (Animator != null) Animator.applyRootMotion = false;
            previousPosition = transform.position;
            CacheCombatBones();
            if (Animator == null || Animator.runtimeAnimatorController == null) return;
            foreach (var parameter in Animator.parameters)
            {
                if (parameter.type == AnimatorControllerParameterType.Trigger) optionalTriggers.Add(parameter.name);
                if (parameter.name == IsDraggingParameter && parameter.type == AnimatorControllerParameterType.Bool) hasDraggingParameter = true;
                if (parameter.name == IsDeadParameter && parameter.type == AnimatorControllerParameterType.Bool) hasDeadParameter = true;
                if (parameter.name == SpeedParameter && parameter.type == AnimatorControllerParameterType.Float) hasSpeedParameter = true;
                if (parameter.name == IsRunningParameter && parameter.type == AnimatorControllerParameterType.Bool) hasRunningParameter = true;
                if (parameter.name == WalkPlaybackRateParameter && parameter.type == AnimatorControllerParameterType.Float) hasWalkPlaybackRate = true;
                if (parameter.name == RunPlaybackRateParameter && parameter.type == AnimatorControllerParameterType.Float) hasRunPlaybackRate = true;
                if (parameter.name == InteractTrigger && parameter.type == AnimatorControllerParameterType.Trigger) hasInteractTrigger = true;
                if (parameter.name == EatTrigger && parameter.type == AnimatorControllerParameterType.Trigger) hasEatTrigger = true;
                if (parameter.name == AttackTrigger && parameter.type == AnimatorControllerParameterType.Trigger) hasAttackTrigger = true;
            }
        }

        private void CacheCombatBones()
        {
            foreach (var tr in GetComponentsInChildren<Transform>(true))
            {
                string n = tr.name;
                if (n == "Weapon_R" || (weaponBone == null && n.EndsWith("RightHand")))
                    weaponBone = tr;
                else if (n.EndsWith("RightShoulder"))
                    rightShoulderBone = tr;
                else if (n.EndsWith("RightArm") || n.EndsWith("RightUpperArm"))
                    rightUpperArmBone = tr;
                else if (n.EndsWith("RightForeArm") || n.EndsWith("RightLowerArm"))
                    rightLowerArmBone = tr;
                else if (n.EndsWith("Spine1") || n.EndsWith("Spine2"))
                    spineBone = tr;
                else if (n == "Khoa_Weapon" || n == "An_Weapon")
                    defaultWeaponMeshRenderer = tr.GetComponent<Renderer>();
            }
        }

        public void SetEquippedWeaponVisual(int weaponIndex)
        {
            if (weaponBone == null) CacheCombatBones();
            if (weaponBone == null) return;
            weaponBone.localScale = Vector3.one;

            if (weaponVisuals == null || weaponVisuals.Length != 3)
                BuildDynamicWeaponModels();

            activeWeaponIndex = weaponIndex;

            if (defaultWeaponMeshRenderer != null)
                defaultWeaponMeshRenderer.enabled = (weaponIndex == 0);

            SyncWeaponVisualScales();
        }

        private void SyncWeaponVisualScales()
        {
            if (weaponBone == null || weaponVisuals == null) return;
            float boneWorldScale = Mathf.Max(0.0001f, weaponBone.lossyScale.x);
            float invScale = 1f / boneWorldScale;
            for (int i = 0; i < weaponVisuals.Length; i++)
            {
                if (weaponVisuals[i] == null) continue;
                bool show = (i == activeWeaponIndex) && (i != 0 || defaultWeaponMeshRenderer == null || attackAnimTimer > 0.01f);
                if (weaponVisuals[i].activeSelf != show)
                    weaponVisuals[i].SetActive(show);
                weaponVisuals[i].transform.localScale = Vector3.one * invScale;
            }
        }

        private void BuildDynamicWeaponModels()
        {
            if (weaponBone == null) return;
            weaponVisuals = new GameObject[3];
            Shader sh = Shader.Find("PigTycoon/OriginalWorld/SurfaceLit3D")
                        ?? Shader.Find("Universal Render Pipeline/Lit")
                        ?? Shader.Find("Standard");

            Material steelMat = CreateQuickMat(sh, new Color(0.82f, 0.87f, 0.92f), 0.72f);
            Material cyanEdgeMat = CreateQuickMat(sh, new Color(0.55f, 0.92f, 1.00f), 0.85f);
            Material brassMat = CreateQuickMat(sh, new Color(0.86f, 0.68f, 0.26f), 0.65f);
            Material woodMat = CreateQuickMat(sh, new Color(0.38f, 0.23f, 0.12f), 0.25f);
            Material crimsonMat = CreateQuickMat(sh, new Color(0.86f, 0.16f, 0.18f), 0.35f);

            float invScale = 1f / Mathf.Max(0.0001f, weaponBone.lossyScale.x);

            // 0: Dao Rựa (Forged Steel Machete Cleaver enhancement edge)
            var daoRoot = new GameObject("Visual_DaoRua");
            daoRoot.transform.SetParent(weaponBone, false);
            daoRoot.transform.localScale = Vector3.one * invScale;
            AddPrim(daoRoot.transform, PrimitiveType.Cube, "BladeEdge", new Vector3(0f, 0.26f, 0.012f), new Vector3(0.012f, 0.44f, 0.052f), steelMat);
            AddPrim(daoRoot.transform, PrimitiveType.Cube, "BladeGleam", new Vector3(0f, 0.28f, 0.036f), new Vector3(0.015f, 0.42f, 0.012f), cyanEdgeMat);
            AddPrim(daoRoot.transform, PrimitiveType.Cylinder, "Guard", new Vector3(0f, 0.04f, 0f), new Vector3(0.045f, 0.012f, 0.075f), brassMat);
            weaponVisuals[0] = daoRoot;

            // 1: Búa Tạ (Heavy War Mallet — Bronze & Iron Great Hammer)
            var buaRoot = new GameObject("Visual_BuaTa");
            buaRoot.transform.SetParent(weaponBone, false);
            buaRoot.transform.localScale = Vector3.one * invScale;
            AddPrim(buaRoot.transform, PrimitiveType.Cylinder, "Haft", new Vector3(0f, 0.28f, 0f), new Vector3(0.032f, 0.38f, 0.032f), woodMat);
            AddPrim(buaRoot.transform, PrimitiveType.Cube, "HammerHead", new Vector3(0f, 0.60f, 0f), new Vector3(0.16f, 0.15f, 0.28f), steelMat);
            AddPrim(buaRoot.transform, PrimitiveType.Cube, "BrassBand", new Vector3(0f, 0.60f, 0f), new Vector3(0.17f, 0.06f, 0.29f), brassMat);
            weaponVisuals[1] = buaRoot;

            // 2: Giáo Tre (Martial Polearm Spear with Red Tassel & Forged Tip)
            var giaoRoot = new GameObject("Visual_GiaoTre");
            giaoRoot.transform.SetParent(weaponBone, false);
            giaoRoot.transform.localScale = Vector3.one * invScale;
            AddPrim(giaoRoot.transform, PrimitiveType.Cylinder, "Shaft", new Vector3(0f, 0.42f, 0f), new Vector3(0.026f, 0.68f, 0.026f), woodMat);
            AddPrim(giaoRoot.transform, PrimitiveType.Cylinder, "Collar", new Vector3(0f, 0.94f, 0f), new Vector3(0.038f, 0.04f, 0.038f), brassMat);
            AddPrim(giaoRoot.transform, PrimitiveType.Sphere, "Tassel", new Vector3(0f, 0.90f, 0.02f), new Vector3(0.075f, 0.065f, 0.075f), crimsonMat);
            AddPrim(giaoRoot.transform, PrimitiveType.Cube, "SpearBlade", new Vector3(0f, 1.14f, 0f), new Vector3(0.018f, 0.28f, 0.062f), cyanEdgeMat);
            weaponVisuals[2] = giaoRoot;
        }

        private static Material CreateQuickMat(Shader sh, Color col, float smoothness)
        {
            var m = new Material(sh);
            m.enableInstancing = true;
            if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", col);
            if (m.HasProperty("_Color")) m.SetColor("_Color", col);
            if (m.HasProperty("_Smoothness")) m.SetFloat("_Smoothness", smoothness);
            return m;
        }

        private static void AddPrim(Transform parent, PrimitiveType type, string name, Vector3 localPos, Vector3 localScale, Material mat)
        {
            var go = GameObject.CreatePrimitive(type);
            go.name = name;
            var col = go.GetComponent<Collider>();
            if (col != null) Destroy(col);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPos;
            go.transform.localRotation = Quaternion.identity;
            go.transform.localScale = localScale;
            var r = go.GetComponent<Renderer>();
            if (r != null) r.sharedMaterial = mat;
        }

        public void PlayComboAttack(int step, bool skillSpin = false)
        {
            comboStep = Mathf.Clamp(step, 0, 2);
            isSkillSpin = skillSpin;
            attackAnimDuration = skillSpin ? 0.48f : 0.34f;
            attackAnimTimer = attackAnimDuration;
            PlayAttack();
        }

        public void TriggerParryVisual(float duration = 0.42f)
        {
            parryGuardTimer = Mathf.Max(parryGuardTimer, duration);
            if (guardAuraObj == null)
            {
                guardAuraObj = new GameObject("GuardAegisRing");
                guardAuraObj.transform.SetParent(transform, false);
                guardAuraObj.transform.localPosition = new Vector3(0f, 0.95f, 0.35f);
                Shader sh = Shader.Find("PigTycoon/OriginalWorld/SurfaceLit3D") ?? Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
                Material goldMat = CreateQuickMat(sh, new Color(1.0f, 0.84f, 0.24f), 0.9f);
                const int segs = 8;
                const float radius = 0.48f;
                for (int i = 0; i < segs; i++)
                {
                    float ang = i * (360f / segs);
                    float rad = ang * Mathf.Deg2Rad;
                    Vector3 pos = new Vector3(Mathf.Cos(rad) * radius, Mathf.Sin(rad) * radius, 0f);
                    var seg = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    seg.name = "AegisSeg_" + i;
                    var col = seg.GetComponent<Collider>();
                    if (col != null) Destroy(col);
                    seg.transform.SetParent(guardAuraObj.transform, false);
                    seg.transform.localPosition = pos;
                    seg.transform.localEulerAngles = new Vector3(0f, 0f, ang + 90f);
                    seg.transform.localScale = new Vector3(0.36f, 0.035f, 0.035f);
                    var r = seg.GetComponent<Renderer>();
                    if (r != null) r.sharedMaterial = goldMat;
                }
            }
            guardAuraObj.SetActive(true);
        }

        public void TriggerDashVisual(float duration = 0.25f)
        {
            dashVisualTimer = Mathf.Max(dashVisualTimer, duration);
        }

        private void LateUpdate()
        {
            float dt = Time.deltaTime;
            Vector3 delta = transform.position - previousPosition;
            var sample = new FarmLocomotionAnimationSample(delta.x, delta.y, delta.z, dt,
                IsRunning, WalkReferenceMetersPerSecond, RunReferenceMetersPerSecond);
            HorizontalSpeed = sample.HorizontalSpeed;
            if (Animator != null)
            {
                if (hasSpeedParameter) Animator.SetFloat(SpeedParameter, sample.HorizontalSpeed);
                if (hasRunningParameter) Animator.SetBool(IsRunningParameter, sample.IsRunning);
                if (hasWalkPlaybackRate) Animator.SetFloat(WalkPlaybackRateParameter, sample.WalkPlaybackRate);
                if (hasRunPlaybackRate) Animator.SetFloat(RunPlaybackRateParameter, sample.RunPlaybackRate);
            }

            // Always keep weapon bone visible at full scale on protagonists and compensate child visual scale
            if (weaponBone != null && activeWeaponIndex >= 0)
            {
                weaponBone.localScale = Vector3.one;
                SyncWeaponVisualScales();
            }

            // Layer dynamic open-world combat poses (Combo 1-2-3, Skill Spin, Guard Stance, Dash Lean)
            if (attackAnimTimer > 0f)
            {
                attackAnimTimer = Mathf.Max(0f, attackAnimTimer - dt);
                float u = 1f - (attackAnimTimer / Mathf.Max(0.05f, attackAnimDuration));
                float swing = Mathf.Sin(u * Mathf.PI);
                if (spineBone != null)
                {
                    float yaw = isSkillSpin ? u * 360f : (comboStep == 0 ? Mathf.Lerp(-28f, 38f, u) : (comboStep == 1 ? Mathf.Lerp(36f, -34f, u) : 0f));
                    float pitch = comboStep == 2 ? Mathf.Lerp(-18f, 28f, u) * swing : 14f * swing;
                    spineBone.localRotation *= Quaternion.Euler(pitch, yaw * 0.45f, 0f);
                }
                if (rightUpperArmBone != null)
                {
                    Vector3 euler = comboStep == 0
                        ? new Vector3(-35f * swing, 0f, -45f * swing)
                        : (comboStep == 1 ? new Vector3(-48f * swing, 25f * swing, 35f * swing) : new Vector3(-75f * swing, 0f, 0f));
                    rightUpperArmBone.localRotation *= Quaternion.Euler(euler);
                }
            }
            if (parryGuardTimer > 0f)
            {
                parryGuardTimer = Mathf.Max(0f, parryGuardTimer - dt);
                if (rightUpperArmBone != null)
                    rightUpperArmBone.localRotation *= Quaternion.Euler(-55f, -35f, 25f);
                if (guardAuraObj != null)
                {
                    guardAuraObj.SetActive(parryGuardTimer > 0.01f);
                    guardAuraObj.transform.localRotation = Quaternion.Euler(0f, 0f, Time.time * 240f);
                }
            }
            else if (guardAuraObj != null && guardAuraObj.activeSelf)
            {
                guardAuraObj.SetActive(false);
            }

            if (dashVisualTimer > 0f)
            {
                dashVisualTimer = Mathf.Max(0f, dashVisualTimer - dt);
                if (spineBone != null)
                    spineBone.localRotation *= Quaternion.Euler(22f, 0f, 0f);
            }

            previousPosition = transform.position;
        }
        public void SetLocomotionState(bool actualCoreIsRunning) { IsRunning = actualCoreIsRunning; }
        public void ResetMotionSample()
        { previousPosition = transform.position; HorizontalSpeed = 0f; IsRunning = false; }
        public void PlayInteraction(bool eating = false)
        {
            if (Animator == null) return;
            if (eating && hasEatTrigger) Animator.SetTrigger(EatTrigger);
            else if (hasInteractTrigger) Animator.SetTrigger(InteractTrigger);
        }
        public void PlayAttack() { if (Animator != null && hasAttackTrigger) Animator.SetTrigger(AttackTrigger); }
        public void PlayWhistle() { TryTrigger(WhistleTrigger); }
        public float ApplyRaidAnimation(FarmRaidAnimationFrame frame)
        {
            if (Animator == null) return 0f;
            if (hasDraggingParameter) Animator.SetBool(IsDraggingParameter, frame.IsDragging);
            if (hasDeadParameter) Animator.SetBool(IsDeadParameter, frame.IsDead);
            if (frame.Telegraph) TryTrigger(TelegraphTrigger);
            if (frame.Attack) PlayAttack();
            if (frame.Bind) TryTrigger(BindTrigger);
            if (frame.Hit) TryTrigger(HitTrigger);
            if (!frame.Death) return 0f;
            bool deathTriggered = TryTrigger(DeathTrigger);
            if ((!deathTriggered && !hasDeadParameter) || Animator.runtimeAnimatorController == null) return 0f;
            if (DeathClip != null) return DeathClip.length;
            float duration = 0f;
            foreach (var clip in Animator.runtimeAnimatorController.animationClips)
                if (clip != null && clip.name.IndexOf("Death", System.StringComparison.OrdinalIgnoreCase) >= 0)
                    duration = Mathf.Max(duration, clip.length);
            return duration;
        }
        private bool TryTrigger(string parameter)
        {
            if (Animator == null || !optionalTriggers.Contains(parameter)) return false;
            Animator.SetTrigger(parameter); return true;
        }
    }
}
