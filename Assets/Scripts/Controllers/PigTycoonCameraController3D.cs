using System;
using System.Collections.Generic;
using UnityEngine;
using PigTycoon.Core;

namespace PigTycoon.Controllers
{
    /// <summary>
    /// Cấu hình hiển thị trên Inspector cho từng chế độ Camera theo Camera Design v1.0.
    /// </summary>
    [Serializable]
    public class CameraProfileInspectorConfig
    {
        public CameraMode3D mode;
        public string label;
        [Range(8f, 55f)] public float pitchDegrees = 24f;
        [Range(0.05f, 0.70f)] public float targetCharacterScreenHeightRatio = 0.21f;
        [Range(54f, 60f)] public float verticalFov = 58f;
        [Range(0f, 5f)] public float lookAheadDistance = 1.6f;
        [Range(0.05f, 1.0f)] public float transitionSmoothTime = 0.22f;
        public bool isFrontThreeQuarterDialogue = false;
    }

    /// <summary>
    /// Hệ thống Camera 3D góc chéo thấp có phối cảnh (Perspective 2.5D Nghệ Thuật)
    /// chuẩn hóa theo "Pig Tycoon — Camera Design v1.0" (# PIG TYCOON — FINAL CAMERA SYSTEM SPECIFICATION).
    /// - Giữ hướng thế giới cố định (không xoay 360° quanh nhân vật khi đổi hướng đi).
    /// - Tự động điều chỉnh góc nhìn xuống mặt đất (Pitch) và khoảng cách theo 6 hoạt động:
    ///   Farm (22-26°), Exploration (18-24°), Combat (30-38°), Defense (38-48°), Dialogue (3/4 cận cảnh 3D), Scenic (12-18°).
    /// - Hỗ trợ Look-ahead theo hướng di chuyển, chống xuyên địa hình, và làm mờ vật cản (cây, mái nhà, cột đá).
    /// </summary>
    public class PigTycoonCameraController3D : MonoBehaviour
    {
        [Header("Target & Core Binding")]
        public Transform targetCharacter;
        public float characterHeightMeters = 1.70f;
        public float focusHeightOffset = 1.05f;

        [Header("Camera Mode (6 Standardized Modes)")]
        public CameraMode3D currentMode = CameraMode3D.Farm;
        public bool lockWorldDirection = true;
        public float lockedWorldYawDegrees = 0.0f;

        [Header("Camera Profiles (Camera Design v1.0)")]
        public List<CameraProfileInspectorConfig> profiles = new List<CameraProfileInspectorConfig>();

        [Header("Obstruction Fading (Trees, Roofs, Stone Pillars)")]
        public LayerMask obstructionLayerMask = ~0;
        [Range(0.15f, 0.50f)] public float obstructedMinAlpha = 0.24f;
        public float obstructionSphereRadius = 0.45f;

        [Header("Collision Handling")]
        public LayerMask cameraCollisionMask = 1;
        public float minCollisionDistance = 2.0f;

        public CameraAndWorldLayoutSystem CoreCameraSystem { get; private set; } = new CameraAndWorldLayoutSystem();

        private Camera controlledCamera;
        private Vector3 currentVelocity;
        private Vector3 smoothedLookAhead;
        private Vector3 lastTargetPosition;
        private float currentYawVelocity;
        private float currentPitchVelocity;
        private float currentDistVelocity;
        private float currentFovVelocity;

        private readonly Dictionary<Renderer, float> fadedRenderers = new Dictionary<Renderer, float>();
        private readonly HashSet<Renderer> currentlyHitRenderers = new HashSet<Renderer>();

        private void Awake()
        {
            controlledCamera = GetComponent<Camera>();
            if (controlledCamera == null)
            {
                controlledCamera = Camera.main;
            }

            InitializeDefaultInspectorProfilesIfNeeded();
            ApplyPerspectiveSettings();
        }

        private void Start()
        {
            if (targetCharacter != null)
            {
                lastTargetPosition = targetCharacter.position;
            }
            CoreCameraSystem.ApplyModeImmediate(currentMode, characterHeightMeters);
        }

        private void InitializeDefaultInspectorProfilesIfNeeded()
        {
            if (profiles != null && profiles.Count == 6) return;

            profiles = new List<CameraProfileInspectorConfig>
            {
                new CameraProfileInspectorConfig
                {
                    mode = CameraMode3D.Farm,
                    label = "Sinh hoạt tại trại (22-26°, 18-24%)",
                    pitchDegrees = 24f,
                    targetCharacterScreenHeightRatio = 0.21f,
                    verticalFov = 58f,
                    lookAheadDistance = 1.5f,
                    transitionSmoothTime = 0.22f
                },
                new CameraProfileInspectorConfig
                {
                    mode = CameraMode3D.Exploration,
                    label = "Khám phá rừng / Huyền bí (18-24°, 22-30%)",
                    pitchDegrees = 21f,
                    targetCharacterScreenHeightRatio = 0.26f,
                    verticalFov = 58f,
                    lookAheadDistance = 2.0f,
                    transitionSmoothTime = 0.20f
                },
                new CameraProfileInspectorConfig
                {
                    mode = CameraMode3D.Combat,
                    label = "Chiến đấu thường (30-38°, 14-20%)",
                    pitchDegrees = 34f,
                    targetCharacterScreenHeightRatio = 0.17f,
                    verticalFov = 58f,
                    lookAheadDistance = 2.4f,
                    transitionSmoothTime = 0.16f
                },
                new CameraProfileInspectorConfig
                {
                    mode = CameraMode3D.Defense,
                    label = "Thủ trại / Boss lớn (38-48°, 9-15%)",
                    pitchDegrees = 43f,
                    targetCharacterScreenHeightRatio = 0.12f,
                    verticalFov = 58f,
                    lookAheadDistance = 2.8f,
                    transitionSmoothTime = 0.24f
                },
                new CameraProfileInspectorConfig
                {
                    mode = CameraMode3D.Dialogue,
                    label = "Hội thoại / Cốt truyện (Góc 3/4 Model 3D)",
                    pitchDegrees = 11f,
                    targetCharacterScreenHeightRatio = 0.55f,
                    verticalFov = 54f,
                    lookAheadDistance = 0.0f,
                    transitionSmoothTime = 0.18f,
                    isFrontThreeQuarterDialogue = true
                },
                new CameraProfileInspectorConfig
                {
                    mode = CameraMode3D.Scenic,
                    label = "Toàn cảnh điện ảnh (12-18°, Bầu trời & Lâu đài xa)",
                    pitchDegrees = 15f,
                    targetCharacterScreenHeightRatio = 0.07f,
                    verticalFov = 56f,
                    lookAheadDistance = 0.0f,
                    transitionSmoothTime = 0.45f
                }
            };
        }

        private void ApplyPerspectiveSettings()
        {
            if (controlledCamera == null) return;
            // Bắt buộc dùng Perspective Camera, không dùng Orthographic Camera
            controlledCamera.orthographic = false;
            controlledCamera.fieldOfView = Mathf.Clamp(CoreCameraSystem.CurrentVerticalFov, 54f, 60f);
            controlledCamera.nearClipPlane = 0.3f;
            controlledCamera.farClipPlane = 450f;
        }

        public void SetCameraMode(CameraMode3D mode)
        {
            currentMode = mode;
        }

        private void LateUpdate()
        {
            if (targetCharacter == null) return;

            float dt = Mathf.Max(0.001f, Time.deltaTime);
            Vector3 worldDelta = (targetCharacter.position - lastTargetPosition) / dt;
            lastTargetPosition = targetCharacter.position;

            Vector2 moveDirXZ = new Vector2(worldDelta.x, worldDelta.z);
            if (moveDirXZ.sqrMagnitude > 100f)
            {
                moveDirXZ = Vector2.zero;
            }

            float charYaw = targetCharacter.eulerAngles.y;
            CoreCameraSystem.StepCameraUpdate(
                currentMode,
                dt,
                moveDirXZ.x,
                moveDirXZ.y,
                characterHeightMeters,
                charYaw);

            var (camX, camY, camZ) = CoreCameraSystem.ComputeCameraWorldPosition(
                targetCharacter.position.x,
                targetCharacter.position.y,
                targetCharacter.position.z,
                focusHeightOffset);

            Vector3 desiredFocus = targetCharacter.position + new Vector3(
                CoreCameraSystem.LookAheadOffsetX,
                focusHeightOffset,
                CoreCameraSystem.LookAheadOffsetZ);

            Vector3 desiredCamPos = new Vector3(camX, camY, camZ);

            // Xử lý va chạm địa hình để Camera không chui xuống dưới mặt đất
            Vector3 dirFromFocus = desiredCamPos - desiredFocus;
            float desiredDist = dirFromFocus.magnitude;
            if (desiredDist > 0.01f && Physics.SphereCast(
                desiredFocus,
                0.25f,
                dirFromFocus.normalized,
                out RaycastHit hit,
                desiredDist,
                cameraCollisionMask,
                QueryTriggerInteraction.Ignore))
            {
                if (!hit.transform.IsChildOf(targetCharacter))
                {
                    float safeDist = Mathf.Max(minCollisionDistance, hit.distance - 0.2f);
                    desiredCamPos = desiredFocus + dirFromFocus.normalized * safeDist;
                }
            }

            transform.position = Vector3.SmoothDamp(
                transform.position,
                desiredCamPos,
                ref currentVelocity,
                0.08f);

            Quaternion targetRot = Quaternion.Euler(
                CoreCameraSystem.CurrentPitchDegrees,
                CoreCameraSystem.CurrentYawDegrees,
                0f);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, Mathf.Clamp01(dt * 10f));

            if (controlledCamera != null)
            {
                controlledCamera.orthographic = false;
                controlledCamera.fieldOfView = CoreCameraSystem.CurrentVerticalFov;
            }

            UpdateObstructionFading(desiredFocus, dt);
        }

        private void UpdateObstructionFading(Vector3 focusPoint, float dt)
        {
            currentlyHitRenderers.Clear();

            Vector3 camPos = transform.position;
            Vector3 toTarget = focusPoint - camPos;
            float dist = toTarget.magnitude;

            if (dist > 0.5f)
            {
                RaycastHit[] hits = Physics.SphereCastAll(
                    camPos,
                    obstructionSphereRadius,
                    toTarget.normalized,
                    dist - 0.4f,
                    obstructionLayerMask,
                    QueryTriggerInteraction.Ignore);

                for (int i = 0; i < hits.Length; i++)
                {
                    Transform t = hits[i].transform;
                    if (t == null || t == targetCharacter || t.IsChildOf(targetCharacter)) continue;

                    // Chỉ làm mờ vật cản gần camera (cây, mái nhà, cột đá, rào/tường cao)
                    if (t.name.Contains("Terrain") || t.name.Contains("Ground") || t.name.Contains("Meadow")) continue;

                    Renderer[] renderers = t.GetComponentsInChildren<Renderer>();
                    for (int r = 0; r < renderers.Length; r++)
                    {
                        currentlyHitRenderers.Add(renderers[r]);
                        if (!fadedRenderers.ContainsKey(renderers[r]))
                        {
                            fadedRenderers[renderers[r]] = 1.0f;
                        }
                    }
                }
            }

            var keys = new List<Renderer>(fadedRenderers.Keys);
            for (int i = 0; i < keys.Count; i++)
            {
                Renderer rend = keys[i];
                if (rend == null)
                {
                    fadedRenderers.Remove(rend);
                    continue;
                }

                bool isBlocking = currentlyHitRenderers.Contains(rend);
                float alpha = CoreCameraSystem.UpdateObstructionFade(
                    rend.GetInstanceID().ToString(),
                    "EnvironmentObstruction",
                    isBlocking,
                    dt,
                    obstructedMinAlpha);

                fadedRenderers[rend] = alpha;
                ApplyRendererAlpha(rend, alpha);

                if (!isBlocking && alpha >= 0.99f)
                {
                    fadedRenderers.Remove(rend);
                }
            }
        }

        private static void ApplyRendererAlpha(Renderer rend, float alpha)
        {
            if (rend == null || rend.sharedMaterial == null) return;
            Material mat = rend.material;
            if (mat.HasProperty("_BaseColor"))
            {
                Color c = mat.GetColor("_BaseColor");
                c.a = alpha;
                mat.SetColor("_BaseColor", c);
            }
            else if (mat.HasProperty("_Color"))
            {
                Color c = mat.color;
                c.a = alpha;
                mat.color = c;
            }
        }
    }
}
