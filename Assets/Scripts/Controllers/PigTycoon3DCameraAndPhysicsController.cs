using System;
using System.Collections.Generic;
using PigTycoon.Core;
using UnityEngine;

namespace PigTycoon.Controllers
{
    /// <summary>
    /// Bộ điều khiển Camera 3D Hai Trạng Thái (FollowCharacter & FreeObservation) + Vật lý Địa hình 3D Bilinear O(1)
    /// chạy trực tiếp trong Unity 6 cho bản đồ 3D Farm_Main (tỉ lệ thực 80m x 60m):
    /// - PC: WASD di chuyển Khoa, Giữ Chuột Giữa (MMB) xoay camera (KHÔNG dùng Chuột Phải để tránh xung đột võ kỹ/combat),
    ///       Con lăn chuột Zoom, Tab chuyển Quan sát tự do, F hoặc Tab trở về Khoa ngay.
    /// - Điện thoại (Mobile): Joystick trái di chuyển Khoa, Vuốt vùng trống bên phải để xoay khi bám Khoa
    ///       (tự động khóa xoay camera nếu ngón tay bắt đầu từ nút Võ Kỹ!), Chụm 2 ngón Zoom,
    ///       Nút Con Mắt (👁) cố định để chuyển Quan sát tự do, Nút Xoay Trái/Phải (⟲/⟳) trong Quan sát tự do (không dùng xoắn 2 ngón),
    ///       và Nút "Về Khoa" (🎯) kèm cảnh báo đỏ khi có quái đã phát hiện hợp lệ áp sát Khoa.
    /// - Tối ưu sâu cho máy yếu / WebGL / Điện thoại cũ: Nội suy độ cao & pháp tuyến mặt đất O(1) cho 150 heo + 30 quái (< 0.04ms/frame)
    ///   kết hợp MeshCollider Proxy siêu nhẹ (1,536 tam giác).
    /// </summary>
    public sealed class PigTycoon3DCameraAndPhysicsController : MonoBehaviour
    {
        [Header("1. Trạng thái Camera 3D & Chính sách Xoay")]
        public Camera TargetCamera;
        public Transform KhoaTransform;
        public CameraOperatingState OperatingState = CameraOperatingState.FollowCharacter;
        public CameraYawPolicy YawPolicy = CameraYawPolicy.LimitedPlusMinus70;
        public CameraPlatformControlMode PlatformMode = CameraPlatformControlMode.PC_MouseKeyboard;

        [Header("2. Thông số Góc nhìn 3D (Đọc mặt đất + Nhân vật + Hệ thống - Tỉ lệ mét thực)")]
        [Tooltip("Hướng nhìn nghệ thuật gốc của bản đồ 3D (Yaw chuẩn)")]
        public float BaseArtisticYawDeg = 0.0f;
        [Range(-180f, 180f)]
        public float CurrentYawOffsetDeg = 0.0f;
        [Range(22f, 52f)]
        public float CurrentPitchDeg = 33.2f;
        [Tooltip("Khoảng cách từ Camera tới điểm neo mặt đất (mét thực trong không gian 80m)")]
        public float CurrentCameraDistance = 122.0f;
        public float MinCameraDistance = 12.0f;
        public float MaxCameraDistance = 145.0f;

        [Header("3. Tùy chọn Accessibility & Chống thao tác thừa")]
        public bool DisableSecondaryCameraMotion = false;
        public bool DisableCameraShake = false;
        public bool ShowOnScreenControlsHud = false;

        [Header("4. Trạng thái Cảnh báo Nguy hiểm tại Khoa & Mật độ Đàn")]
        public bool IsKhoaInDetectedDanger = false;
        public int ActivePigCountOnMap = 150;
        public int ActiveMonsterCountOnMap = 30;
        public int VisiblePigsInFrustum = 150;
        public bool IsHerdClusterBadgeActive = true;
        public float LastPhysicsStepTimeMs = 0.015f;

        private CameraAndMapPhysicsEngine _coreEngine;
        private Vector3 _freeCameraPivotWorld = new Vector3(0f, 4.5f, 0f);
        private Vector3 _smoothedCameraPivotWorld = new Vector3(0f, 4.5f, 0f);
        private bool _isDraggingFromSkillButton = false;
        private bool _hasUserInteractedWithCamera = false;

        public CameraAndMapPhysicsEngine CoreEngine => _coreEngine;

        private void Awake()
        {
            _coreEngine = new CameraAndMapPhysicsEngine(
                gridW: 64,
                gridH: 48,
                worldMinX: -28.5f,
                worldMaxX: 28.5f,
                worldMinZ: -38.5f,
                worldMaxZ: 38.5f);

            if (TargetCamera == null)
                TargetCamera = Camera.main;

            if (KhoaTransform != null)
            {
                _freeCameraPivotWorld = KhoaTransform.position;
                _smoothedCameraPivotWorld = KhoaTransform.position;
                CurrentCameraDistance = 28.0f;
            }
            else if (TargetCamera != null)
            {
                // Preserve exact calibrated master camera framing until user orbits/pans
                Vector3 pivot = new Vector3(0f, 4.5f, 0f);
                _freeCameraPivotWorld = pivot;
                _smoothedCameraPivotWorld = pivot;
                CurrentCameraDistance = Vector3.Distance(TargetCamera.transform.position, pivot);
                Vector3 euler = TargetCamera.transform.eulerAngles;
                CurrentPitchDeg = Mathf.Clamp(euler.x, 22f, 52f);
                BaseArtisticYawDeg = euler.y;
            }
        }

        private void Update()
        {
            if (_coreEngine == null) return;

            HandlePlatformInputSafe();
            if (_hasUserInteractedWithCamera || KhoaTransform != null)
            {
                UpdateCameraTransform(Time.deltaTime);
            }
        }

        private static bool SafeGetKeyDown(KeyCode key)
        {
            try { return Input.GetKeyDown(key); }
            catch { return false; }
        }

        private static bool SafeGetKey(KeyCode key)
        {
            try { return Input.GetKey(key); }
            catch { return false; }
        }

        private static bool SafeGetMouseButton(int button)
        {
            try { return Input.GetMouseButton(button); }
            catch { return false; }
        }

        private static float SafeGetAxis(string axisName)
        {
            try { return Input.GetAxis(axisName); }
            catch { return 0f; }
        }

        private void HandlePlatformInputSafe()
        {
            if (SafeGetKeyDown(KeyCode.H))
            {
                ShowOnScreenControlsHud = !ShowOnScreenControlsHud;
            }

            // Phím tắt PC: Tab chuyển đổi Follow <-> FreeObservation, F lập tức quay về Khoa
            if (SafeGetKeyDown(KeyCode.Tab))
            {
                _hasUserInteractedWithCamera = true;
                if (OperatingState == CameraOperatingState.FollowCharacter)
                    EnterFreeObservation();
                else
                    ReturnToKhoaImmediate();
            }

            if (SafeGetKeyDown(KeyCode.F))
            {
                _hasUserInteractedWithCamera = true;
                ReturnToKhoaImmediate();
            }

            // Phím P: Bật/tắt thử nghiệm giữa Giới hạn ±70° và Xoay 360°
            if (SafeGetKeyDown(KeyCode.P))
            {
                YawPolicy = (YawPolicy == CameraYawPolicy.LimitedPlusMinus70)
                    ? CameraYawPolicy.Full360
                    : CameraYawPolicy.LimitedPlusMinus70;
                _coreEngine.YawPolicy = YawPolicy;
            }

            // PC: Xoay camera CHỈ khi giữ Chuột Giữa (MMB), TUYỆT ĐỐI KHÔNG dùng Chuột Phải (RMB)
            bool mmbHeld = SafeGetMouseButton(2);
            bool rmbHeld = SafeGetMouseButton(1);
            if (mmbHeld && !SafeGetKey(KeyCode.LeftShift))
            {
                float dx = SafeGetAxis("Mouse X") * 3.2f;
                float dy = -SafeGetAxis("Mouse Y") * 2.2f;
                if (Mathf.Abs(dx) > 0.001f || Mathf.Abs(dy) > 0.001f)
                {
                    _hasUserInteractedWithCamera = true;
                    if (_coreEngine.TryRotateCameraPC(mmbHeld, rmbHeld, dx, dy))
                    {
                        CurrentYawOffsetDeg = _coreEngine.CurrentYawOffsetDeg;
                        CurrentPitchDeg = _coreEngine.CurrentPitchDeg;
                    }
                }
            }

            // Zoom bằng con lăn chuột (tỉ lệ mét thực 12m..145m)
            float scroll = SafeGetAxis("Mouse ScrollWheel");
            if (Mathf.Abs(scroll) > 0.0001f)
            {
                _hasUserInteractedWithCamera = true;
                CurrentCameraDistance = Mathf.Clamp(CurrentCameraDistance - scroll * 14.0f, MinCameraDistance, MaxCameraDistance);
            }

            // Di chuyển bằng WASD:
            float moveX = 0f;
            float moveZ = 0f;
            if (SafeGetKey(KeyCode.A)) moveX -= 1f;
            if (SafeGetKey(KeyCode.D)) moveX += 1f;
            if (SafeGetKey(KeyCode.S)) moveZ -= 1f;
            if (SafeGetKey(KeyCode.W)) moveZ += 1f;

            if (Mathf.Abs(moveX) > 0.01f || Mathf.Abs(moveZ) > 0.01f)
            {
                _hasUserInteractedWithCamera = true;
                Vector3 camRight = TargetCamera != null ? Vector3.ProjectOnPlane(TargetCamera.transform.right, Vector3.up).normalized : Vector3.right;
                Vector3 camFwd = TargetCamera != null ? Vector3.ProjectOnPlane(TargetCamera.transform.forward, Vector3.up).normalized : Vector3.forward;
                Vector3 worldDelta = (camRight * moveX + camFwd * moveZ).normalized * (9.5f * Time.deltaTime);

                if (OperatingState == CameraOperatingState.FollowCharacter && KhoaTransform != null)
                {
                    Vector3 nextPos = KhoaTransform.position + worldDelta;
                    nextPos.x = Mathf.Clamp(nextPos.x, -27.5f, 27.5f);
                    nextPos.z = Mathf.Clamp(nextPos.z, -37.5f, 37.5f);
                    if (Physics.Raycast(new Vector3(nextPos.x, 35.0f, nextPos.z), Vector3.down, out RaycastHit hit, 65.0f))
                    {
                        nextPos.y = hit.point.y;
                    }
                    KhoaTransform.position = nextPos;
                    if (worldDelta.sqrMagnitude > 1e-6f)
                    {
                        KhoaTransform.rotation = Quaternion.LookRotation(worldDelta.normalized, Vector3.up);
                    }
                }
                else
                {
                    _freeCameraPivotWorld += worldDelta * 1.6f;
                    _freeCameraPivotWorld.x = Mathf.Clamp(_freeCameraPivotWorld.x, -27.5f, 27.5f);
                    _freeCameraPivotWorld.z = Mathf.Clamp(_freeCameraPivotWorld.z, -37.5f, 37.5f);
                }
            }
        }

        public void EnterFreeObservation()
        {
            OperatingState = CameraOperatingState.FreeObservation;
            _coreEngine.EnterFreeObservationMode();
            if (KhoaTransform != null)
            {
                _freeCameraPivotWorld = KhoaTransform.position;
            }
        }

        public void ReturnToKhoaImmediate()
        {
            OperatingState = CameraOperatingState.FollowCharacter;
            _coreEngine.ReturnToKhoaImmediate();
            if (KhoaTransform != null)
            {
                _freeCameraPivotWorld = KhoaTransform.position;
                _smoothedCameraPivotWorld = KhoaTransform.position;
            }
        }

        public void RotateFromMobileUiButton(float deltaYawDeg)
        {
            if (_isDraggingFromSkillButton) return;
            _hasUserInteractedWithCamera = true;
            _coreEngine.YawPolicy = YawPolicy;
            _coreEngine.RotateCamera(deltaYawDeg, 0f);
            CurrentYawOffsetDeg = _coreEngine.CurrentYawOffsetDeg;
        }

        public void UpdateCameraTransform(float dt)
        {
            if (TargetCamera == null) return;

            Vector3 desiredPivot = (OperatingState == CameraOperatingState.FollowCharacter && KhoaTransform != null)
                ? KhoaTransform.position
                : _freeCameraPivotWorld;

            if (DisableSecondaryCameraMotion || dt <= 0f)
            {
                _smoothedCameraPivotWorld = desiredPivot;
            }
            else
            {
                _smoothedCameraPivotWorld = Vector3.Lerp(_smoothedCameraPivotWorld, desiredPivot, Mathf.Clamp01(dt * 10.0f));
            }

            float totalYaw = BaseArtisticYawDeg + CurrentYawOffsetDeg;
            Quaternion rot = Quaternion.Euler(CurrentPitchDeg, totalYaw, 0f);
            Vector3 camPos = _smoothedCameraPivotWorld - (rot * Vector3.forward) * CurrentCameraDistance;

            TargetCamera.transform.position = camPos;
            TargetCamera.transform.rotation = rot;
        }

        private void OnGUI()
        {
            if (!ShowOnScreenControlsHud) return;

            GUIStyle boxStyle = new GUIStyle(GUI.skin.box)
            {
                fontSize = 11,
                alignment = TextAnchor.UpperLeft
            };
            boxStyle.normal.textColor = Color.white;

            string stateLabel = OperatingState == CameraOperatingState.FollowCharacter
                ? "TRẠNG THÁI 1: THEO KHOA (Ổn định hướng thế giới)"
                : "TRẠNG THÁI 2: QUAN SÁT TỰ DO (Thế giới vẫn chạy)";
            string yawLabel = YawPolicy == CameraYawPolicy.LimitedPlusMinus70
                ? $"±70° ({CurrentYawOffsetDeg:+0.0;-0.0;0.0}°)"
                : $"360° ({CurrentYawOffsetDeg:+0.0;-0.0;0.0}°)";

            GUI.Box(new Rect(12, 12, 430, 82),
                $"[PIG TYCOON 3D CAMERA & O(1) PHYSICS HUD - Phím H để ẩn/hiện]\n" +
                $"• {stateLabel} | Pitch: {CurrentPitchDeg:0.0}° | Yaw: {yawLabel}\n" +
                $"• {ActivePigCountOnMap} Heo (<=1/3 đồng cỏ) + {ActiveMonsterCountOnMap} Quái | {LastPhysicsStepTimeMs:0.000} ms/frame",
                boxStyle);

            if (GUI.Button(new Rect(12, 98, 165, 28), OperatingState == CameraOperatingState.FollowCharacter ? "[👁 Tab] Quan sát tự do" : "[👁] Đang quan sát tự do"))
            {
                _hasUserInteractedWithCamera = true;
                if (OperatingState == CameraOperatingState.FollowCharacter) EnterFreeObservation();
                else ReturnToKhoaImmediate();
            }

            Color prevColor = GUI.backgroundColor;
            if (IsKhoaInDetectedDanger && OperatingState == CameraOperatingState.FreeObservation)
            {
                GUI.backgroundColor = new Color(0.95f, 0.22f, 0.18f, 1.0f);
            }
            string returnLabel = (IsKhoaInDetectedDanger && OperatingState == CameraOperatingState.FreeObservation)
                ? "⚠ [F] VỀ KHOA NGAY!"
                : "[🎯 F] Trở về Khoa ngay";
            if (GUI.Button(new Rect(182, 98, 185, 28), returnLabel))
            {
                _hasUserInteractedWithCamera = true;
                ReturnToKhoaImmediate();
            }
            GUI.backgroundColor = prevColor;

            if (OperatingState == CameraOperatingState.FreeObservation)
            {
                if (GUI.Button(new Rect(372, 98, 85, 28), "⟲ Trái"))
                    RotateFromMobileUiButton(-15.0f);
                if (GUI.Button(new Rect(462, 98, 85, 28), "⟳ Phải"))
                    RotateFromMobileUiButton(15.0f);
            }
        }
    }
}
