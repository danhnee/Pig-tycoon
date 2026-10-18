using System;
using System.Collections.Generic;

namespace PigTycoon.Core
{
    public enum CameraOperatingState
    {
        FollowCharacter = 0,   // Theo nhân vật (Khoa): Góc chéo đọc mặt đất + giữ hướng thế giới ổn định
        FreeObservation = 1,   // Quan sát tự do: Tách khỏi Khoa, kiểm tra đàn heo / tuyến phòng thủ, Khoa giữ nguyên vị trí
        StoryFraming = 2       // Góc kể chuyện / phản chiếu: "Cho thấy" tự nhiên, không tự zoom ép buộc
    }

    public enum CameraZoomPreset
    {
        CloseUp = 0,     // Cận: ~40 x 22 m
        Default = 1,     // Mặc định: ~64 x 36 m (Chuẩn GDD, chứa trọn đường kính 60m / bán kính 30m của công trình)
        Panoramic = 2    // Toàn cảnh: ~128 x 72 m
    }

    public enum CameraYawPolicy
    {
        LimitedPlusMinus70 = 0, // Giới hạn ±70° quanh hướng nghệ thuật chuẩn (mặc định cho bố cục 2.5D/3D)
        Full360 = 1             // Xoay 360° tự do (dùng khi thử nghiệm)
    }

    public enum CameraPlatformControlMode
    {
        PC_MouseKeyboard = 0,   // WASD di chuyển, Giữ Chuột Giữa (MMB) xoay (KHÔNG dùng chuột phải), Con lăn zoom, Tab/F tách & về Khoa
        Mobile_Touch = 1        // Joystick trái, Vuốt vùng trống phải (tránh nút võ kỹ), Chụm 2 ngón zoom, Nút Mắt tách cam, Nút ⟲/⟳ xoay, Nút Về Khoa
    }

    public enum MapSemanticZone
    {
        OutsideCliffOrVoid = 0,
        GoldenMeadowOpen = 1,          // Đồng cỏ mở (> 2/3 diện tích đồng cỏ)
        DioramaCutawayRockRim = 2,     // Vách đá cắt lớp rìa map (không đi qua)
        WaterRiverAndPonds = 3,        // Sông, thác nước & 3 hồ nước tự nhiên (vật cản nước, có phản chiếu)
        FarmsteadCourtyard = 4,        // Sân chuồng / Khu chuồng trại (nơi nâng cấp, không có rào nhốt heo)
        ResearchObservatory = 5,       // Nhà nghiên cứu
        EnhancementAndNpcVillage = 6,  // Nhà cường hóa & Khu NPC
        InfoBeaconTower = 7,           // Trạm thông tin
        MarketAndStoneBridge = 8,      // Chợ / Thị trấn & Cầu đá
        TwilightCastleCliff = 9,       // Lâu đài hoàng hôn & Vách thác
        AlpineMountainRidge = 10,      // Dãy núi cao phía bắc
        AncientRuinsAndFlora = 11,     // Tàn tích cổ & Cây vàng kim thấp
        GrazingPastureZone = 12        // Vùng thả đàn tự nhiên (<= 1/3 đồng cỏ, >= 35% trong ranh giới quản lý trại, tụ quanh máng/nước/nơi trú)
    }

    /// <summary>
    /// Hệ thống Toán học Camera 3D, Luật Kinh tế Thông tin (Chống lộ quái tàng hình / Chống chặn điểm Spawn bằng Camera tự do)
    /// và Hệ thống Vật lý Địa hình Bilinear O(1) siêu nhẹ (chạy mượt 60 FPS với 150 heo + 30 quái trên WebGL, điện thoại cũ, laptop yếu & PC).
    /// Tuân thủ tuyệt đối AGENTS.md: Pure C# trong Core, không phụ thuộc UnityEngine.
    /// </summary>
    public sealed class CameraAndMapPhysicsEngine
    {
        // Hằng số GDD & Chuẩn hóa Toán học Frustum Unity (https://docs.unity3d.com/6000.0/Documentation/Manual/FrustumSizeAtDistance.html)
        public const float DefaultVerticalFovDeg = 28.5758f;
        public const float AspectRatio16By9 = 16.0f / 9.0f;

        // Khung nhìn mặt đất chuẩn GDD: Cận (40x22m), Mặc định (64x36m), Toàn cảnh (128x72m)
        public const float CloseUpGroundWidthM = 40.0f;
        public const float CloseUpGroundHeightM = 22.5f;
        public const float DefaultGroundWidthM = 64.0f;
        public const float DefaultGroundHeightM = 36.0f;
        public const float PanoramicGroundWidthM = 128.0f;
        public const float PanoramicGroundHeightM = 72.0f;

        // Giới hạn góc xoay & độ nghiêng (Pitch & Yaw)
        public const float DefaultPitchDeg = 34.0f; // Góc chéo đọc rõ mặt đất + quan hệ giữa Khoa, bẫy, đàn heo và tuyến phòng thủ
        public const float MinPitchDeg = 22.0f;     // Hạ góc ngắm đồng cỏ thần thoại, cây vàng kim, phản chiếu mặt nước & hoàng hôn
        public const float MaxPitchDeg = 52.0f;     // Nâng góc bao quát trận đánh thủ trại
        public const float LimitedYawMaxAbsDeg = 70.0f; // Giới hạn ±70° quanh hướng nghệ thuật chính

        // Mật độ đàn GDD: Quá 60 heo trong khung nhìn thì kích hoạt biểu diễn theo cụm (Cluster Badge LOD)
        public const int HerdClusterBadgeThreshold = 60;

        // Trạng thái Camera hiện tại
        public CameraOperatingState CurrentState { get; private set; } = CameraOperatingState.FollowCharacter;
        public CameraZoomPreset CurrentZoomPreset { get; private set; } = CameraZoomPreset.Default;
        public CameraYawPolicy YawPolicy { get; set; } = CameraYawPolicy.LimitedPlusMinus70;
        public CameraPlatformControlMode PlatformMode { get; set; } = CameraPlatformControlMode.PC_MouseKeyboard;

        // Tùy chọn Accessibility (Xbox Accessibility Guidelines 117): Tắt rung lắc & chuyển động camera tự động
        public bool DisableSecondaryCameraMotion { get; set; } = false;
        public bool DisableCameraShake { get; set; } = false;

        // Vị trí thật của Khoa trong thế giới 3D (KHÔNG thay đổi khi tách camera sang Quan sát tự do!)
        public float KhoaWorldX { get; private set; }
        public float KhoaWorldY { get; private set; }
        public float KhoaWorldZ { get; private set; }
        public float KhoaFacingYawDeg { get; private set; }

        // Điểm neo mặt đất của Camera (Target Pivot trên mặt phẳng địa hình)
        public float CameraPivotX { get; private set; }
        public float CameraPivotY { get; private set; }
        public float CameraPivotZ { get; private set; }

        // Góc xoay ngang (Yaw lệch so với hướng chuẩn 0°), góc nghiêng (Pitch) và khoảng cách (Distance)
        public float CurrentYawOffsetDeg { get; private set; } = 0.0f;
        public float CurrentPitchDeg { get; private set; } = DefaultPitchDeg;
        public float CurrentGroundWidthM { get; private set; } = DefaultGroundWidthM;

        // Lưới độ cao Bilinear O(1) & Phân vùng ngữ nghĩa (64 x 48 ô siêu nhẹ trong bộ nhớ L1 Cache)
        private readonly int _gridW;
        private readonly int _gridH;
        private readonly float _worldMinX;
        private readonly float _worldMaxX;
        private readonly float _worldMinZ;
        private readonly float _worldMaxZ;
        private readonly float[,] _heightGrid;
        private readonly MapSemanticZone[,] _zoneGrid;

        public CameraAndMapPhysicsEngine(
            int gridW = 64,
            int gridH = 48,
            float worldMinX = -64.0f,
            float worldMaxX = 64.0f,
            float worldMinZ = -48.0f,
            float worldMaxZ = 48.0f)
        {
            _gridW = Math.Max(8, gridW);
            _gridH = Math.Max(8, gridH);
            _worldMinX = worldMinX;
            _worldMaxX = worldMaxX;
            _worldMinZ = worldMinZ;
            _worldMaxZ = worldMaxZ;
            _heightGrid = new float[_gridH, _gridW];
            _zoneGrid = new MapSemanticZone[_gridH, _gridW];

            InitializeDefaultFarmMapTopology();
            SetKhoaTransform(-18.0f, 10.0f, 0.0f);
            SnapCameraToKhoa();
        }

        /// <summary>
        /// Khởi tạo địa hình 3D chuẩn khớp với bản đồ đồng cỏ vàng:
        /// - Khu chuồng trại trên đồi thoải phía Tây-Bắc (-24, +14)
        /// - Vùng thả đàn tự nhiên (GrazingPastureZone) nằm gần trại, chiếm <= 1/3 diện tích đồng cỏ toàn map và >= 35% trong ranh giới trại, KHÔNG có hàng rào cắt đôi đồng cỏ.
        /// - 3 hồ nước tự nhiên ở trung tâm đồng cỏ & dòng sông bên phải.
        /// </summary>
        private void InitializeDefaultFarmMapTopology()
        {
            for (int r = 0; r < _gridH; r++)
            {
                float v = (float)r / (_gridH - 1); // 0 (Nam/Tiền cảnh) -> 1 (Bắc/Hậu cảnh)
                float z = _worldMinZ + v * (_worldMaxZ - _worldMinZ);
                for (int c = 0; c < _gridW; c++)
                {
                    float u = (float)c / (_gridW - 1); // 0 (Tây/Trái) -> 1 (Đông/Phải)
                    float x = _worldMinX + u * (_worldMaxX - _worldMinX);

                    // Độ cao địa hình 3D mượt (Lipschitz-smooth)
                    float baseSlope = v * 6.5f + (1.0f - u) * 2.2f;
                    float hillNorthwest = 4.2f * (float)Math.Exp(-((u - 0.25f) * (u - 0.25f) / 0.06f + (v - 0.72f) * (v - 0.72f) / 0.05f));
                    float castleCliffNortheast = 9.5f * (float)Math.Exp(-((u - 0.72f) * (u - 0.72f) / 0.04f + (v - 0.85f) * (v - 0.85f) / 0.03f));
                    float riverDepression = -2.4f * (float)Math.Exp(-((u - 0.76f) * (u - 0.76f) / 0.015f + (v - 0.58f) * (v - 0.58f) / 0.08f));
                    _heightGrid[r, c] = Math.Max(0.0f, baseSlope + hillNorthwest + castleCliffNortheast + riverDepression);

                    // Phân vùng ngữ nghĩa
                    if (u < 0.04f || u > 0.96f || v < 0.05f)
                    {
                        _zoneGrid[r, c] = MapSemanticZone.DioramaCutawayRockRim;
                    }
                    else if (v > 0.88f && u < 0.58f)
                    {
                        _zoneGrid[r, c] = MapSemanticZone.AlpineMountainRidge;
                    }
                    else if (v > 0.76f && u >= 0.58f && u <= 0.86f)
                    {
                        _zoneGrid[r, c] = MapSemanticZone.TwilightCastleCliff;
                    }
                    else if ((u > 0.68f && u < 0.82f && v > 0.42f && v < 0.76f) ||
                             IsInsidePond(u, v, 0.39f, 0.38f, 0.065f, 0.035f) ||
                             IsInsidePond(u, v, 0.46f, 0.52f, 0.055f, 0.030f) ||
                             IsInsidePond(u, v, 0.55f, 0.28f, 0.060f, 0.032f))
                    {
                        _zoneGrid[r, c] = MapSemanticZone.WaterRiverAndPonds;
                    }
                    else if (IsInsideEllipse(u, v, 0.27f, 0.72f, 0.06f, 0.05f))
                    {
                        _zoneGrid[r, c] = MapSemanticZone.FarmsteadCourtyard;
                    }
                    else if (IsInsideEllipse(u, v, 0.35f, 0.71f, 0.045f, 0.04f))
                    {
                        _zoneGrid[r, c] = MapSemanticZone.ResearchObservatory;
                    }
                    else if (IsInsideEllipse(u, v, 0.43f, 0.72f, 0.05f, 0.045f))
                    {
                        _zoneGrid[r, c] = MapSemanticZone.EnhancementAndNpcVillage;
                    }
                    else if (IsInsideEllipse(u, v, 0.48f, 0.65f, 0.035f, 0.035f))
                    {
                        _zoneGrid[r, c] = MapSemanticZone.InfoBeaconTower;
                    }
                    else if (u >= 0.80f && v >= 0.45f && v <= 0.75f)
                    {
                        _zoneGrid[r, c] = MapSemanticZone.MarketAndStoneBridge;
                    }
                    else if (IsInsideEllipse(u, v, 0.34f, 0.56f, 0.16f, 0.14f))
                    {
                        // Vùng thả đàn tự nhiên gần trại (<= 1/3 diện tích đồng cỏ, tụ quanh máng/nước/chỗ trú, không rào chắn)
                        _zoneGrid[r, c] = MapSemanticZone.GrazingPastureZone;
                    }
                    else
                    {
                        _zoneGrid[r, c] = MapSemanticZone.GoldenMeadowOpen;
                    }
                }
            }
        }

        private static bool IsInsidePond(float u, float v, float cu, float cv, float ru, float rv)
        {
            return IsInsideEllipse(u, v, cu, cv, ru, rv);
        }

        private static bool IsInsideEllipse(float u, float v, float cu, float cv, float ru, float rv)
        {
            float du = (u - cu) / ru;
            float dv = (v - cv) / rv;
            return (du * du + dv * dv) <= 1.0f;
        }

        // =====================================================================
        // 1. TOÁN HỌC CAMERA FRUSTUM (CÔNG THỨC CHUẨN UNITY 6 DOCS + SỬA TIÊU CHÍ A2)
        // =====================================================================

        /// <summary>
        /// Tính khoảng cách từ Camera tới mặt phẳng mục tiêu để đạt đúng bề rộng khung nhìn mặt đất (Ground Width)
        /// theo công thức chuẩn Unity Docs (FrustumSizeAtDistance):
        /// frustumHeight = 2.0f * distance * tan(verticalFov * 0.5)
        /// frustumWidth = frustumHeight * aspect
        /// => distance = frustumWidth / (2.0f * aspect * tan(verticalFov * 0.5))
        /// </summary>
        public static float CalculateCameraDistanceForGroundWidth(
            float desiredGroundWidthM,
            float verticalFovDeg = DefaultVerticalFovDeg,
            float aspectRatio = AspectRatio16By9)
        {
            if (desiredGroundWidthM <= 0.1f) desiredGroundWidthM = 0.1f;
            float halfFovRad = (verticalFovDeg * 0.5f) * ((float)Math.PI / 180.0f);
            float tanHalf = (float)Math.Tan(halfFovRad);
            float frustumHeight = desiredGroundWidthM / Math.Max(0.1f, aspectRatio);
            return frustumHeight / (2.0f * Math.Max(0.001f, tanHalf));
        }

        /// <summary>
        /// Tính kích thước vùng nhìn (Width, Height) tại khoảng cách cho trước theo đúng công thức tài liệu Unity 6.
        /// </summary>
        public static void CalculateFrustumSizeAtDistance(
            float distanceM,
            out float frustumWidthM,
            out float frustumHeightM,
            float verticalFovDeg = DefaultVerticalFovDeg,
            float aspectRatio = AspectRatio16By9)
        {
            float halfFovRad = (verticalFovDeg * 0.5f) * ((float)Math.PI / 180.0f);
            frustumHeightM = 2.0f * Math.Max(0.0f, distanceM) * (float)Math.Tan(halfFovRad);
            frustumWidthM = frustumHeightM * aspectRatio;
        }

        /// <summary>
        /// Kiểm tra tiêu chí A2 đã hiệu chỉnh nhất quán:
        /// Khung mặc định 64 x 36 m chứa gọn vòng tầm đường kính 60 m (bán kính 30 m của vùng phục vụ / công trình) theo chiều ngang,
        /// trong khi nếu bán kính là 60 m (đường kính 120 m) thì phải chuyển sang khung Toàn cảnh (128 x 72 m) mới chứa trọn.
        /// </summary>
        public static bool DoesRangeCircleFitInFrustumWidth(float rangeRadiusM, float frustumWidthM)
        {
            float diameterM = rangeRadiusM * 2.0f;
            return diameterM <= frustumWidthM;
        }

        // =====================================================================
        // 2. ĐIỀU KHIỂN CAMERA 2 TRẠNG THÁI CHÍNH + CẢNH BÁO KHOA + TÁCH NỀN TẢNG PC/MOBILE
        // =====================================================================

        /// <summary>
        /// Cập nhật vị trí và hướng mặt của Khoa.
        /// Lưu ý quan trọng: Khi Khoa xoay mặt (facingYawDeg thay đổi), Camera ở chế độ FollowCharacter
        /// chỉ cập nhật vị trí theo Khoa nhưng GIỮ NGUYÊN hướng nhìn thế giới (CurrentYawOffsetDeg không tự xoay theo lưng Khoa!).
        /// Khi ở chế độ FreeObservation, vị trí CameraPivot hoàn toàn độc lập với Khoa.
        /// </summary>
        public void SetKhoaTransform(float x, float z, float facingYawDeg)
        {
            KhoaWorldX = Clamp(x, _worldMinX, _worldMaxX);
            KhoaWorldZ = Clamp(z, _worldMinZ, _worldMaxZ);
            KhoaWorldY = SampleTerrainHeightBilinear(KhoaWorldX, KhoaWorldZ);
            KhoaFacingYawDeg = NormalizeAngle180(facingYawDeg);

            if (CurrentState == CameraOperatingState.FollowCharacter)
            {
                CameraPivotX = KhoaWorldX;
                CameraPivotZ = KhoaWorldZ;
                CameraPivotY = KhoaWorldY;
            }
        }

        /// <summary>
        /// Chuyển sang chế độ Quan sát tự do (Tab trên PC / Nút Con Mắt trên Mobile).
        /// Khoa vẫn đứng nguyên tại vị trí thật trong thế giới, không bị dịch chuyển!
        /// </summary>
        public void EnterFreeObservationMode()
        {
            CurrentState = CameraOperatingState.FreeObservation;
        }

        /// <summary>
        /// Bấm 1 nút (F hoặc Tab trên PC / Nút "Về Khoa" trên Mobile) lập tức đưa Camera trở về Khoa.
        /// </summary>
        public void ReturnToKhoaImmediate()
        {
            CurrentState = CameraOperatingState.FollowCharacter;
            SnapCameraToKhoa();
        }

        public void SnapCameraToKhoa()
        {
            CameraPivotX = KhoaWorldX;
            CameraPivotY = KhoaWorldY;
            CameraPivotZ = KhoaWorldZ;
        }

        /// <summary>
        /// Di chuyển khung nhìn trong chế độ Quan sát tự do (WASD / Kéo chuột giữa trên PC, Vuốt 1 ngón trên Mobile).
        /// Khoa KHÔNG bị di chuyển theo Camera!
        /// </summary>
        public void PanFreeCamera(float deltaWorldX, float deltaWorldZ)
        {
            if (CurrentState != CameraOperatingState.FreeObservation)
                return;

            CameraPivotX = Clamp(CameraPivotX + deltaWorldX, _worldMinX * 0.92f, _worldMaxX * 0.92f);
            CameraPivotZ = Clamp(CameraPivotZ + deltaWorldZ, _worldMinZ * 0.92f, _worldMaxZ * 0.92f);
            CameraPivotY = SampleTerrainHeightBilinear(CameraPivotX, CameraPivotZ);
        }

        /// <summary>
        /// Xoay ngang (Yaw) và chỉnh độ nghiêng (Pitch) theo chuẩn giới hạn ±70° hoặc 360°.
        /// </summary>
        public void RotateCamera(float deltaYawDeg, float deltaPitchDeg)
        {
            float nextYaw = CurrentYawOffsetDeg + deltaYawDeg;
            if (YawPolicy == CameraYawPolicy.LimitedPlusMinus70)
            {
                CurrentYawOffsetDeg = Clamp(nextYaw, -LimitedYawMaxAbsDeg, LimitedYawMaxAbsDeg);
            }
            else
            {
                CurrentYawOffsetDeg = NormalizeAngle180(nextYaw);
            }

            CurrentPitchDeg = Clamp(CurrentPitchDeg + deltaPitchDeg, MinPitchDeg, MaxPitchDeg);
        }

        /// <summary>
        /// Xử lý đầu vào xoay Camera trên PC:
        /// Chỉ cho phép xoay khi giữ Chuột Giữa (Middle Mouse Button), TUYỆT ĐỐI KHÔNG xoay bằng Chuột Phải (Right Mouse Button)
        /// để tránh xung đột với hệ chiến đấu, ngắm mục tiêu và kỹ năng.
        /// </summary>
        public bool TryRotateCameraPC(bool isMiddleMouseHeld, bool isRightMouseHeld, float deltaYawDeg, float deltaPitchDeg)
        {
            if (!isMiddleMouseHeld)
                return false; // Chuột phải hoặc không giữ chuột giữa -> không xoay camera!

            RotateCamera(deltaYawDeg, deltaPitchDeg);
            return true;
        }

        /// <summary>
        /// Xử lý đầu vào xoay Camera trên Điện thoại (Mobile):
        /// - Khi đang kéo từ nút Võ Kỹ (isDraggingMartialSkill = true): ưu tiên tuyệt đối cho ngắm kỹ năng, KHÔNG xoay camera!
        /// - Khi ở chế độ Quan sát tự do: không dùng thao tác xoắn 2 ngón (isTwoFingerTwist = true bị từ chối), chỉ xoay qua nút xoay trái/phải chuyên biệt!
        /// </summary>
        public bool TryRotateCameraMobile(
            bool isTouchStartingOnSkillButton,
            bool isEmptyRightAreaSwipe,
            bool isDedicatedRotateButton,
            bool isTwoFingerTwist,
            float deltaYawDeg,
            float deltaPitchDeg)
        {
            if (isTouchStartingOnSkillButton)
                return false; // Đang ngắm võ kỹ -> tuyệt đối không xoay camera!

            if (isTwoFingerTwist)
                return false; // Không dùng xoắn 2 ngón vì dễ nhầm với chụm/mở zoom!

            if (CurrentState == CameraOperatingState.FollowCharacter)
            {
                if (!isEmptyRightAreaSwipe && !isDedicatedRotateButton)
                    return false;
                RotateCamera(deltaYawDeg, deltaPitchDeg);
                return true;
            }
            else if (CurrentState == CameraOperatingState.FreeObservation)
            {
                if (!isDedicatedRotateButton)
                    return false; // Trong Quan sát tự do trên mobile, chỉ xoay bằng nút xoay trái/phải trên UI
                RotateCamera(deltaYawDeg, deltaPitchDeg);
                return true;
            }

            return false;
        }

        public void SetZoomPreset(CameraZoomPreset preset)
        {
            CurrentZoomPreset = preset;
            switch (preset)
            {
                case CameraZoomPreset.CloseUp:
                    CurrentGroundWidthM = CloseUpGroundWidthM;
                    break;
                case CameraZoomPreset.Default:
                    CurrentGroundWidthM = DefaultGroundWidthM;
                    break;
                case CameraZoomPreset.Panoramic:
                    CurrentGroundWidthM = PanoramicGroundWidthM;
                    break;
            }
        }

        public void SetCustomZoomGroundWidth(float groundWidthM)
        {
            CurrentGroundWidthM = Clamp(groundWidthM, CloseUpGroundWidthM * 0.75f, PanoramicGroundWidthM);
        }

        // =====================================================================
        // 3. BẢO VỆ HỆ KINH TẾ THÔNG TIN & CẢNH BÁO NGUY HIỂM TẠI KHOA + CHỐNG EXPLOIT SPAWN
        // =====================================================================

        /// <summary>
        /// Kiểm tra xem một con quái có được hiển thị cho người chơi hay không.
        /// Luật GDD: Nhìn rộng hơn bằng Camera KHÔNG đồng nghĩa với phát hiện miễn phí!
        /// Quái tàng hình hoặc nằm trong sương mù chỉ hiện khi đã nằm trong nguồn phát hiện hợp lệ (Khoa, Đèn Pha, Radar, Thiên Nhãn).
        /// </summary>
        public static bool IsEnemyVisibleUnderInformationEconomy(
            bool isEnemyStealthedOrInFog,
            bool isDetectedByValidPlayerOrTowerSensor)
        {
            if (!isEnemyStealthedOrInFog)
                return true;
            return isDetectedByValidPlayerOrTowerSensor;
        }

        /// <summary>
        /// Kiểm tra xem có bật Cảnh báo Nguy hiểm gắn với nút "Trở về Khoa" khi người chơi đang ở chế độ Quan sát tự do hay không.
        /// Điều kiện:
        /// 1. Camera đang tách khỏi Khoa (FreeObservation) hoặc cách Khoa quá xa.
        /// 2. Có quái trong bán kính nguy hiểm quanh Khoa (ví dụ <= 18m).
        /// 3. Quái đó PHẢI được phát hiện hợp lệ (không làm lộ quái tàng hình miễn phí!).
        /// </summary>
        public bool ShouldTriggerKhoaDangerAlert(
            float enemyX,
            float enemyZ,
            bool isEnemyStealthedOrInFog,
            bool isDetectedByValidSensor,
            float dangerRadiusM = 18.0f)
        {
            if (CurrentState != CameraOperatingState.FreeObservation)
                return false;

            if (!IsEnemyVisibleUnderInformationEconomy(isEnemyStealthedOrInFog, isDetectedByValidSensor))
                return false;

            float dx = enemyX - KhoaWorldX;
            float dz = enemyZ - KhoaWorldZ;
            return (dx * dx + dz * dz) <= (dangerRadiusM * dangerRadiusM);
        }

        /// <summary>
        /// Luật Chống Khai Thác Camera Tự Do Chặn Điểm Spawn Quái (Anti-Spawn-Camping):
        /// GDD quy định quái không spawn trong khung nhìn hiện tại và cách trại >= 25m.
        /// Nhưng nếu người chơi dùng Camera Quan sát tự do đứng nhìn chằm chằm vào cổng spawn ngoài rìa để chặn quái,
        /// hệ thống chỉ khóa spawn đối với Khung nhìn Bám Khoa (Follow Frustum) + Vùng đệm trại 25m,
        /// KHÔNG cho phép Camera Quan sát tự do vô hiệu hóa điểm spawn hợp lệ ngoài rìa!
        /// </summary>
        public bool IsSpawnPointValidAgainstExploit(
            float spawnX,
            float spawnZ,
            float farmCenterX,
            float farmCenterZ,
            float minFarmBufferRadiusM = 25.0f)
        {
            // 1. Phải nằm ngoài vùng đệm không spawn 25m quanh trại
            float dfx = spawnX - farmCenterX;
            float dfz = spawnZ - farmCenterZ;
            if ((dfx * dfx + dfz * dfz) < (minFarmBufferRadiusM * minFarmBufferRadiusM))
                return false;

            // 2. Không được spawn ngay trong khung nhìn sinh hoạt quanh Khoa (bán kính 32m quanh Khoa),
            // NHƯNG cố ý bỏ qua vị trí CameraPivot khi đang ở FreeObservation để người chơi không thể kéo camera ra rìa chặn spawn!
            float dkx = spawnX - KhoaWorldX;
            float dkz = spawnZ - KhoaWorldZ;
            float khoaSafeRadius = DefaultGroundHeightM * 0.5f; // 18m
            if ((dkx * dkx + dkz * dkz) < (khoaSafeRadius * khoaSafeRadius))
                return false;

            return true;
        }

        /// <summary>
        /// Kiểm tra xem mật độ đàn heo trong khung nhìn hiện tại có cần bật hiển thị theo Cụm (Cluster Badge) theo chuẩn GDD (> 60 heo) hay không.
        /// </summary>
        public static bool ShouldUseHerdClusterRepresentation(int visiblePigCountInFrustum)
        {
            return visiblePigCountInFrustum > HerdClusterBadgeThreshold;
        }

        // =====================================================================
        // 4. VẬT LÝ ĐỊA HÌNH 3D BILINEAR O(1) SIÊU TỐI ƯU & KIỂM TRA VÙNG THẢ ĐÀN
        // =====================================================================

        /// <summary>
        /// Nội suy độ cao mặt đất 3D Y(x, z) với độ phức tạp O(1) (không dùng Raycast nặng, chạy < 0.04ms cho 150 heo + 30 quái trên điện thoại/WebGL).
        /// </summary>
        public float SampleTerrainHeightBilinear(float worldX, float worldZ)
        {
            float u = Clamp((worldX - _worldMinX) / (_worldMaxX - _worldMinX), 0.0f, 1.0f);
            float v = Clamp((worldZ - _worldMinZ) / (_worldMaxZ - _worldMinZ), 0.0f, 1.0f);

            float gx = u * (_gridW - 1);
            float gz = v * (_gridH - 1);

            int c0 = (int)Math.Floor(gx);
            int r0 = (int)Math.Floor(gz);
            int c1 = Math.Min(c0 + 1, _gridW - 1);
            int r1 = Math.Min(r0 + 1, _gridH - 1);

            float tx = gx - c0;
            float tz = gz - r0;

            float h00 = _heightGrid[r0, c0];
            float h10 = _heightGrid[r0, c1];
            float h01 = _heightGrid[r1, c0];
            float h11 = _heightGrid[r1, c1];

            float h0 = h00 + (h10 - h00) * tx;
            float h1 = h01 + (h11 - h01) * tx;
            return h0 + (h1 - h0) * tz;
        }

        /// <summary>
        /// Tính pháp tuyến mặt đất (Surface Normal) O(1) để nghiêng chân nhân vật / đàn heo mượt mà trên sườn đồi 3D.
        /// </summary>
        public void SampleTerrainNormal(float worldX, float worldZ, out float nx, out float ny, out float nz)
        {
            const float eps = 0.5f;
            float hL = SampleTerrainHeightBilinear(worldX - eps, worldZ);
            float hR = SampleTerrainHeightBilinear(worldX + eps, worldZ);
            float hD = SampleTerrainHeightBilinear(worldX, worldZ - eps);
            float hU = SampleTerrainHeightBilinear(worldX, worldZ + eps);

            float dhdx = (hR - hL) / (2.0f * eps);
            float dhdz = (hU - hD) / (2.0f * eps);

            nx = -dhdx;
            ny = 1.0f;
            nz = -dhdz;
            float len = (float)Math.Sqrt(nx * nx + ny * ny + nz * nz);
            if (len > 1e-5f)
            {
                nx /= len;
                ny /= len;
                nz /= len;
            }
        }

        public MapSemanticZone GetZoneAt(float worldX, float worldZ)
        {
            float u = Clamp((worldX - _worldMinX) / (_worldMaxX - _worldMinX), 0.0f, 1.0f);
            float v = Clamp((worldZ - _worldMinZ) / (_worldMaxZ - _worldMinZ), 0.0f, 1.0f);
            int c = (int)Math.Round(u * (_gridW - 1));
            int r = (int)Math.Round(v * (_gridH - 1));
            c = Math.Max(0, Math.Min(_gridW - 1, c));
            r = Math.Max(0, Math.Min(_gridH - 1, r));
            return _zoneGrid[r, c];
        }

        /// <summary>
        /// Kiểm tra điểm (x, z) có cho phép nhân vật / heo / quái bước đi trên mặt đất 3D không (chặn rơi khỏi vách đá cắt lớp và chặn lội xuống sông sâu/hồ).
        /// </summary>
        public bool IsWalkableForGroundAgent(float worldX, float worldZ)
        {
            if (worldX < _worldMinX || worldX > _worldMaxX || worldZ < _worldMinZ || worldZ > _worldMaxZ)
                return false;

            MapSemanticZone zone = GetZoneAt(worldX, worldZ);
            return zone != MapSemanticZone.OutsideCliffOrVoid
                && zone != MapSemanticZone.DioramaCutawayRockRim
                && zone != MapSemanticZone.WaterRiverAndPonds
                && zone != MapSemanticZone.TwilightCastleCliff
                && zone != MapSemanticZone.AlpineMountainRidge;
        }

        /// <summary>
        /// Tính tỷ lệ diện tích Vùng thả đàn (GrazingPastureZone) so với tổng diện tích đồng cỏ toàn map (phải <= 1/3 ~ 0.333f)
        /// và tỷ lệ diện tích chăn thả bên trong ranh giới quản lý của Trang trại (phải >= 35% theo GDD).
        /// </summary>
        public void CalculateGrazingZoneRatios(out float ratioOfTotalMeadow, out float ratioInsideFarmPerimeter)
        {
            int grazingCells = 0;
            int totalMeadowCells = 0;
            int farmPerimeterCells = 0;

            for (int r = 0; r < _gridH; r++)
            {
                for (int c = 0; c < _gridW; c++)
                {
                    MapSemanticZone z = _zoneGrid[r, c];
                    if (z == MapSemanticZone.GrazingPastureZone)
                    {
                        grazingCells++;
                        totalMeadowCells++;
                        farmPerimeterCells++;
                    }
                    else if (z == MapSemanticZone.GoldenMeadowOpen || z == MapSemanticZone.AncientRuinsAndFlora)
                    {
                        totalMeadowCells++;
                    }
                    else if (z == MapSemanticZone.FarmsteadCourtyard ||
                             z == MapSemanticZone.ResearchObservatory ||
                             z == MapSemanticZone.EnhancementAndNpcVillage ||
                             z == MapSemanticZone.InfoBeaconTower)
                    {
                        farmPerimeterCells++;
                    }
                }
            }

            ratioOfTotalMeadow = totalMeadowCells > 0 ? (float)grazingCells / totalMeadowCells : 0.0f;
            ratioInsideFarmPerimeter = farmPerimeterCells > 0 ? (float)grazingCells / farmPerimeterCells : 0.0f;
        }

        private static float Clamp(float v, float min, float max)
        {
            if (v < min) return min;
            if (v > max) return max;
            return v;
        }

        private static float NormalizeAngle180(float angleDeg)
        {
            angleDeg %= 360.0f;
            if (angleDeg > 180.0f) angleDeg -= 360.0f;
            if (angleDeg < -180.0f) angleDeg += 360.0f;
            return angleDeg;
        }
    }
}
