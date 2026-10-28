using System;
using System.Collections.Generic;
using UnityEngine;
using PigTycoon.Core;

namespace PigTycoon.Controllers
{
    /// <summary>
    /// Cấu hình hiển thị trên Inspector cho từng chế độ Camera.
    /// </summary>
    [Serializable]
    public class CameraProfileInspectorConfig
    {
        public CameraMode3D mode;
        public string label;
        [Range(8f, 55f)] public float pitchDegrees = 30f;
        [Range(0.05f, 0.70f)] public float targetCharacterScreenHeightRatio = 0.16f;
        [Range(50f, 68f)] public float verticalFov = 58f;
        [Range(0f, 5f)] public float lookAheadDistance = 1.6f;
        [Range(0.05f, 1.0f)] public float transitionSmoothTime = 0.22f;
        public bool isFrontThreeQuarterDialogue = false;
    }

    /// <summary>
    /// Hệ thống Camera 3D phối cảnh 2.5D nghệ thuật chuẩn hóa cho Pig Tycoon (Camera Design v2.0):
    /// 1. Hai trạng thái chơi chính trên cùng một scene 3D:
    ///    - FollowCharacter (Theo nhân vật): bám vị trí Khoa, góc chéo đọc rõ mặt đất + đàn heo + bẫy/công trình
    ///      khi dùng Móc Kéo, Dao Rựa, Nỏ Tay; cho phép xoay ngang, chỉnh độ nghiêng (18°–48°) và zoom (40x22m <-> 64x36m)
    ///      mà không tự động giật/đổi góc liên tục giữa lúc giao tranh.
    ///    - FreeObservation (Quan sát tự do): tách camera khỏi Khoa để kiểm tra đàn heo, vùng xây dựng và tuyến phòng thủ
    ///      (zoom tới Toàn cảnh 128x72m). Khoa vẫn đứng ở vị trí thật trong thế giới. Có cảnh báo nguy hiểm hợp lệ tại vị trí Khoa
    ///      và bấm 1 nút (F / Tab hoặc nút UI) để lập tức quay về Khoa.
    /// 2. Điều khiển riêng biệt cho PC và Điện thoại:
    ///    - PC: giữ Chuột Giữa (MMB) để xoay/chỉnh nghiêng (TUYỆT ĐỐI KHÔNG dùng Chuột Phải để tránh xung đột combat/kỹ năng),
    ///      con lăn chuột để zoom, Tab chuyển Quan sát tự do, WASD hoặc MMB+Shift để kéo camera tự do, F hoặc Tab về Khoa ngay.
    ///    - Điện thoại: vuốt vùng trống bên phải khi theo Khoa (bỏ qua nếu bắt đầu kéo từ nút võ kỹ), chụm/mở 2 ngón để zoom,
    ///      nút Con Mắt cố định để bật Quan sát tự do, vuốt 1 ngón để di chuyển camera tự do, nút xoay trái/phải trên UI (không dùng xoắn 2 ngón),
    ///      nút Trở Về Nhân Vật kèm huy hiệu cảnh báo nguy hiểm.
    /// 3. Hỗ trợ thử nghiệm cả 2 phương án xoay ngang trong Unity:
    ///    - LimitedPlusMinus70 (Mặc định khuyên dùng: giới hạn ±70° quanh hướng chuẩn thế giới 0° để giữ bố cục 2.5D và cảnh xa).
    ///    - Full360Orbit (Xoay 360° tự do).
    /// 4. Bảo vệ Kinh tế Thông tin (GDD Chương 14 & 23) và Chống khai thác điểm spawn (GDD 3.8).
    /// </summary>
    public class PigTycoonCameraController3D : MonoBehaviour
    {
        [Header("Target & Core Binding")]
        public Transform targetCharacter;
        public float characterHeightMeters = 1.70f;
        public float focusHeightOffset = 1.05f;

        [Header("Camera Design v2.0 — Two Primary States & Rotation Policy")]
        public CameraPrimaryState3D primaryState = CameraPrimaryState3D.FollowCharacter;
        public CameraYawRotationPolicy yawRotationPolicy = CameraYawRotationPolicy.LimitedPlusMinus70;
        [Range(15f, 180f)] public float maxLimitedYawDegrees = 70.0f;
        public CameraPlatformInputScheme platformScheme = CameraPlatformInputScheme.PC_MouseKeyboard;

        [Header("User Controllable Pitch, Zoom & Pan")]
        [Range(18f, 48f)] public float userPitchDegrees = 30.0f;
        [Range(8f, 95f)] public float userDistanceMeters = 24.0f;
        public float minFollowDistanceMeters = 12.0f;   // Khung Cận ~40x22m
        public float defaultFollowDistanceMeters = 24.0f; // Khung Mặc định ~64x36m
        public float maxFreeObservationDistanceMeters = 52.0f; // Khung Toàn cảnh ~128x72m
        public float mouseOrbitSensitivity = 0.25f;
        public float mousePitchSensitivity = 0.18f;
        public float scrollZoomSensitivity = 3.2f;
        public float freePanSpeedMetersPerSec = 28.0f;

        [Header("Accessibility (Xbox Guideline 117)")]
        public bool enableAutoCameraAssist = false;
        public bool enableCameraShake = true;
        [Range(50f, 68f)] public float userVerticalFov = 58.0f;

        [Header("Legacy / Preset Modes (Optional Reference)")]
        public CameraMode3D currentMode = CameraMode3D.Farm;
        public bool lockWorldDirection = true;
        public float lockedWorldYawDegrees = 0.0f;
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
        private Vector3 lastTargetPosition;
        private Vector3 lastMousePosition;
        private bool isMiddleMouseDragging;

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
            CoreCameraSystem.SetYawRotationPolicy(yawRotationPolicy, maxLimitedYawDegrees);
            CoreCameraSystem.EnableAutoCameraAssist = enableAutoCameraAssist;
            CoreCameraSystem.EnableCameraShake = enableCameraShake;
            ApplyPerspectiveSettings();
        }

        private void Start()
        {
            if (targetCharacter != null)
            {
                lastTargetPosition = targetCharacter.position;
                CoreCameraSystem.SetPrimaryState(primaryState, targetCharacter.position.x, targetCharacter.position.y, targetCharacter.position.z);
            }
            CoreCameraSystem.ApplyModeImmediate(currentMode, characterHeightMeters);
            CoreCameraSystem.ApplyUserPitchAdjustment(userPitchDegrees - CoreCameraSystem.CurrentPitchDegrees);
        }

        private void InitializeDefaultInspectorProfilesIfNeeded()
        {
            if (profiles != null && profiles.Count == 6) return;

            profiles = new List<CameraProfileInspectorConfig>
            {
                new CameraProfileInspectorConfig
                {
                    mode = CameraMode3D.Farm,
                    label = "Theo nhân vật — Đọc rõ mặt đất, heo & bẫy (28-32°)",
                    pitchDegrees = 30f,
                    targetCharacterScreenHeightRatio = 0.16f,
                    verticalFov = 58f,
                    lookAheadDistance = 1.5f,
                    transitionSmoothTime = 0.22f
                },
                new CameraProfileInspectorConfig
                {
                    mode = CameraMode3D.Exploration,
                    label = "Khám phá / Ngắm đồng cỏ & nhân vật (20-24°)",
                    pitchDegrees = 22f,
                    targetCharacterScreenHeightRatio = 0.22f,
                    verticalFov = 58f,
                    lookAheadDistance = 1.8f,
                    transitionSmoothTime = 0.20f
                },
                new CameraProfileInspectorConfig
                {
                    mode = CameraMode3D.Combat,
                    label = "Chiến đấu & Kéo quái vào bẫy (32-36°)",
                    pitchDegrees = 34f,
                    targetCharacterScreenHeightRatio = 0.15f,
                    verticalFov = 58f,
                    lookAheadDistance = 2.2f,
                    transitionSmoothTime = 0.18f
                },
                new CameraProfileInspectorConfig
                {
                    mode = CameraMode3D.Defense,
                    label = "Quan sát tự do / Thủ trại toàn cảnh (38-46°)",
                    pitchDegrees = 42f,
                    targetCharacterScreenHeightRatio = 0.10f,
                    verticalFov = 58f,
                    lookAheadDistance = 0.0f,
                    transitionSmoothTime = 0.24f
                },
                new CameraProfileInspectorConfig
                {
                    mode = CameraMode3D.Dialogue,
                    label = "Góc điện ảnh / Kể chuyện có dàn dựng (3/4 Model 3D)",
                    pitchDegrees = 12f,
                    targetCharacterScreenHeightRatio = 0.50f,
                    verticalFov = 54f,
                    lookAheadDistance = 0.0f,
                    transitionSmoothTime = 0.18f,
                    isFrontThreeQuarterDialogue = true
                },
                new CameraProfileInspectorConfig
                {
                    mode = CameraMode3D.Scenic,
                    label = "Ngắm chân trời hoàng hôn & thác nước xa (14-18°)",
                    pitchDegrees = 16f,
                    targetCharacterScreenHeightRatio = 0.08f,
                    verticalFov = 56f,
                    lookAheadDistance = 0.0f,
                    transitionSmoothTime = 0.40f
                }
            };
        }

        private void ApplyPerspectiveSettings()
        {
            if (controlledCamera == null) return;
            controlledCamera.orthographic = false;
            controlledCamera.fieldOfView = Mathf.Clamp(userVerticalFov, 50f, 68f);
            controlledCamera.nearClipPlane = 0.3f;
            controlledCamera.farClipPlane = 600f;
        }

        public void SetCameraMode(CameraMode3D mode)
        {
            currentMode = mode;
        }

        /// <summary>
        /// Nút hình con mắt trên Mobile hoặc phím Tab trên PC: chuyển đổi giữa Theo nhân vật và Quan sát tự do.
        /// </summary>
        public void ToggleFreeObservationMode()
        {
            Vector3 p = targetCharacter != null ? targetCharacter.position : Vector3.zero;
            if (CoreCameraSystem.PrimaryState == CameraPrimaryState3D.FreeObservation)
            {
                ReturnToCharacterImmediate();
            }
            else
            {
                primaryState = CameraPrimaryState3D.FreeObservation;
                CoreCameraSystem.SetPrimaryState(CameraPrimaryState3D.FreeObservation, p.x, p.y, p.z);
                userPitchDegrees = Mathf.Max(userPitchDegrees, 36.0f);
                userDistanceMeters = Mathf.Max(userDistanceMeters, 32.0f);
            }
        }

        /// <summary>
        /// Phím F / Tab trên PC hoặc Nút Trở Về Nhân Vật trên Mobile: lập tức đưa camera quay lại Khoa.
        /// </summary>
        public void ReturnToCharacterImmediate()
        {
            Vector3 p = targetCharacter != null ? targetCharacter.position : Vector3.zero;
            primaryState = CameraPrimaryState3D.FollowCharacter;
            CoreCameraSystem.ReturnToCharacterImmediate(p.x, p.y, p.z);
            userDistanceMeters = Mathf.Clamp(userDistanceMeters, minFollowDistanceMeters, defaultFollowDistanceMeters * 1.35f);
        }

        /// <summary>
        /// Xử lý vuốt màn hình trên Điện thoại khi đang ở chế độ Theo nhân vật.
        /// Nếu thao tác bắt đầu từ một nút võ kỹ hoặc UI, tuyệt đối không xoay camera.
        /// </summary>
        public void OnMobileEmptyAreaSwipe(Vector2 deltaPixels, bool startedOnSkillOrUiButton)
        {
            if (startedOnSkillOrUiButton) return;

            if (CoreCameraSystem.PrimaryState == CameraPrimaryState3D.FollowCharacter)
            {
                CoreCameraSystem.TryApplyPlatformYawInput(
                    CameraPlatformInputScheme.Mobile_Touch,
                    deltaPixels.x * 0.15f,
                    isMiddleMouseHeldOnPc: false,
                    isRightMouseHeldOnPc: false,
                    isTouchStartedOnSkillOrUiButton: false,
                    isTwoFingerTwistGestureOnMobile: false,
                    isDedicatedRotateButtonOnMobile: false);

                userPitchDegrees = Mathf.Clamp(userPitchDegrees - deltaPixels.y * 0.10f, 18.0f, 48.0f);
                CoreCameraSystem.ApplyUserPitchAdjustment(userPitchDegrees - CoreCameraSystem.CurrentPitchDegrees);
            }
            else if (CoreCameraSystem.PrimaryState == CameraPrimaryState3D.FreeObservation)
            {
                // Vuốt 1 ngón trong chế độ Quan sát tự do để di chuyển khung nhìn
                Vector3 right = Quaternion.Euler(0f, CoreCameraSystem.CurrentYawDegrees, 0f) * Vector3.right;
                Vector3 forward = Quaternion.Euler(0f, CoreCameraSystem.CurrentYawDegrees, 0f) * Vector3.forward;
                Vector3 panDelta = (-right * deltaPixels.x - forward * deltaPixels.y) * 0.06f;
                CoreCameraSystem.PanFreeObservationCamera(panDelta.x, panDelta.z, Time.deltaTime);
            }
        }

        /// <summary>
        /// Nút xoay trái/phải trên giao diện Mobile khi đang ở chế độ Quan sát tự do (thay vì xoắn 2 ngón).
        /// </summary>
        public void OnMobileFreeObservationRotateButton(float directionSign)
        {
            CoreCameraSystem.TryApplyPlatformYawInput(
                CameraPlatformInputScheme.Mobile_Touch,
                directionSign * 15.0f,
                isMiddleMouseHeldOnPc: false,
                isRightMouseHeldOnPc: false,
                isTouchStartedOnSkillOrUiButton: false,
                isTwoFingerTwistGestureOnMobile: false,
                isDedicatedRotateButtonOnMobile: true);
        }

        private void HandlePcKeyboardAndMouseInput(float dt)
        {
            if (platformScheme != CameraPlatformInputScheme.PC_MouseKeyboard) return;

            // Tab: chuyển sang Quan sát tự do hoặc trở về Khoa
            if (Input.GetKeyDown(KeyCode.Tab))
            {
                ToggleFreeObservationMode();
            }

            // F: lập tức trở về Khoa
            if (Input.GetKeyDown(KeyCode.F) && CoreCameraSystem.PrimaryState == CameraPrimaryState3D.FreeObservation)
            {
                ReturnToCharacterImmediate();
            }

            // Con lăn chuột: Zoom mượt giữa khung Cận (40x22m) <-> Mặc định (64x36m) <-> Toàn cảnh (128x72m khi FreeObservation)
            float scroll = Input.mouseScrollDelta.y;
            if (Mathf.Abs(scroll) > 0.001f)
            {
                float maxDist = CoreCameraSystem.PrimaryState == CameraPrimaryState3D.FreeObservation
                    ? maxFreeObservationDistanceMeters
                    : defaultFollowDistanceMeters * 1.35f;
                userDistanceMeters = Mathf.Clamp(userDistanceMeters - scroll * scrollZoomSensitivity, minFollowDistanceMeters, maxDist);
            }

            // Giữ Chuột Giữa (MMB = button 2): xoay camera hoặc kéo camera tự do (kèm Shift/Alt)
            // TUYỆT ĐỐI KHÔNG dùng Chuột Phải (button 1) để tránh xung đột với combat / kỹ năng!
            if (Input.GetMouseButtonDown(2))
            {
                isMiddleMouseDragging = true;
                lastMousePosition = Input.mousePosition;
            }
            else if (Input.GetMouseButtonUp(2))
            {
                isMiddleMouseDragging = false;
            }

            if (isMiddleMouseDragging && Input.GetMouseButton(2))
            {
                Vector3 mouseDelta = Input.mousePosition - lastMousePosition;
                lastMousePosition = Input.mousePosition;

                bool modifierHeld = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift) ||
                                    Input.GetKey(KeyCode.LeftAlt) || Input.GetKey(KeyCode.RightAlt);

                if (CoreCameraSystem.PrimaryState == CameraPrimaryState3D.FreeObservation && modifierHeld)
                {
                    Vector3 right = Quaternion.Euler(0f, CoreCameraSystem.CurrentYawDegrees, 0f) * Vector3.right;
                    Vector3 forward = Quaternion.Euler(0f, CoreCameraSystem.CurrentYawDegrees, 0f) * Vector3.forward;
                    Vector3 pan = (-right * mouseDelta.x - forward * mouseDelta.y) * 0.08f;
                    CoreCameraSystem.PanFreeObservationCamera(pan.x, pan.z, dt);
                }
                else
                {
                    CoreCameraSystem.TryApplyPlatformYawInput(
                        CameraPlatformInputScheme.PC_MouseKeyboard,
                        mouseDelta.x * mouseOrbitSensitivity,
                        isMiddleMouseHeldOnPc: true,
                        isRightMouseHeldOnPc: Input.GetMouseButton(1),
                        isTouchStartedOnSkillOrUiButton: false,
                        isTwoFingerTwistGestureOnMobile: false,
                        isDedicatedRotateButtonOnMobile: false);

                    userPitchDegrees = Mathf.Clamp(userPitchDegrees - mouseDelta.y * mousePitchSensitivity, 18.0f, 48.0f);
                    CoreCameraSystem.ApplyUserPitchAdjustment(userPitchDegrees - CoreCameraSystem.CurrentPitchDegrees);
                }
            }

            // Trong chế độ Quan sát tự do (FreeObservation), phím WASD di chuyển khung nhìn camera trên toàn vùng chơi
            if (CoreCameraSystem.PrimaryState == CameraPrimaryState3D.FreeObservation)
            {
                float hx = 0f;
                float hz = 0f;
                if (Input.GetKey(KeyCode.D)) hx += 1f;
                if (Input.GetKey(KeyCode.A)) hx -= 1f;
                if (Input.GetKey(KeyCode.W)) hz += 1f;
                if (Input.GetKey(KeyCode.S)) hz -= 1f;

                if (Mathf.Abs(hx) > 0.01f || Mathf.Abs(hz) > 0.01f)
                {
                    Vector3 right = Quaternion.Euler(0f, CoreCameraSystem.CurrentYawDegrees, 0f) * Vector3.right;
                    Vector3 forward = Quaternion.Euler(0f, CoreCameraSystem.CurrentYawDegrees, 0f) * Vector3.forward;
                    Vector3 move = (right * hx + forward * hz).normalized * (freePanSpeedMetersPerSec * dt);
                    CoreCameraSystem.PanFreeObservationCamera(move.x, move.z, dt);
                }
                else
                {
                    CoreCameraSystem.PanFreeObservationCamera(0f, 0f, dt);
                }
            }
        }

        private void LateUpdate()
        {
            float dt = Mathf.Max(0.001f, Time.deltaTime);
            CoreCameraSystem.SetYawRotationPolicy(yawRotationPolicy, maxLimitedYawDegrees);
            CoreCameraSystem.EnableAutoCameraAssist = enableAutoCameraAssist;
            CoreCameraSystem.EnableCameraShake = enableCameraShake;

            HandlePcKeyboardAndMouseInput(dt);

            if (targetCharacter == null) return;

            Vector3 worldDelta = (targetCharacter.position - lastTargetPosition) / dt;
            lastTargetPosition = targetCharacter.position;

            Vector2 moveDirXZ = new Vector2(worldDelta.x, worldDelta.z);
            if (moveDirXZ.sqrMagnitude > 100f)
            {
                moveDirXZ = Vector2.zero;
            }

            // Chọn tâm quan sát: theo Khoa (FollowCharacter) hoặc tâm tự do (FreeObservation)
            Vector3 pivotPos = targetCharacter.position;
            if (CoreCameraSystem.PrimaryState == CameraPrimaryState3D.FreeObservation)
            {
                pivotPos = new Vector3(
                    CoreCameraSystem.FreeObservationPivotX,
                    targetCharacter.position.y,
                    CoreCameraSystem.FreeObservationPivotZ);
            }

            float pitchRad = userPitchDegrees * Mathf.Deg2Rad;
            float yawRad = CoreCameraSystem.CurrentYawDegrees * Mathf.Deg2Rad;
            float horizDist = userDistanceMeters * Mathf.Cos(pitchRad);
            float vertOffset = userDistanceMeters * Mathf.Sin(pitchRad);

            Vector3 desiredFocus = pivotPos + new Vector3(0f, focusHeightOffset, 0f);
            Vector3 desiredCamPos = new Vector3(
                desiredFocus.x - Mathf.Sin(yawRad) * horizDist,
                desiredFocus.y + vertOffset,
                desiredFocus.z - Mathf.Cos(yawRad) * horizDist);

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
                userPitchDegrees,
                CoreCameraSystem.CurrentYawDegrees,
                0f);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, Mathf.Clamp01(dt * 12f));

            if (controlledCamera != null)
            {
                controlledCamera.orthographic = false;
                controlledCamera.fieldOfView = Mathf.Clamp(userVerticalFov, 50f, 68f);
            }

            UpdateObstructionFading(targetCharacter.position + new Vector3(0f, focusHeightOffset, 0f), dt);
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
                    rend.GetHashCode().ToString(),
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
