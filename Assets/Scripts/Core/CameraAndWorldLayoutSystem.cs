using System;
using System.Collections.Generic;

namespace PigTycoon.Core
{
    /// <summary>
    /// 6 chế độ Camera chuẩn hóa theo "Pig Tycoon — Camera Design v1.0"
    /// (# PIG TYCOON — FINAL CAMERA SYSTEM SPECIFICATION).
    /// </summary>
    public enum CameraMode3D
    {
        /// <summary>Sinh hoạt tại trại (mặc định): Pitch 22–26°, chiều cao nhân vật 18–24% màn hình.</summary>
        Farm = 0,
        /// <summary>Khám phá rừng / sự kiện huyền bí: Pitch 18–24°, chiều cao nhân vật 22–30% màn hình.</summary>
        Exploration = 1,
        /// <summary>Chiến đấu thường: Pitch 30–38°, chiều cao nhân vật 14–20% màn hình.</summary>
        Combat = 2,
        /// <summary>Thủ trại / Boss lớn: Pitch 38–48°, chiều cao nhân vật 9–15% màn hình.</summary>
        Defense = 3,
        /// <summary>Hội thoại / cốt truyện: Cận cảnh mặt hoặc nửa thân góc 3/4 phía trước trên cùng model 3D thật.</summary>
        Dialogue = 4,
        /// <summary>Toàn cảnh điện ảnh: Pitch 12–18°, ngắm bầu trời hoàng hôn, đồng cỏ, thác nước, lâu đài xa.</summary>
        Scenic = 5
    }

    /// <summary>
    /// Hướng nhìn thấy của nhân vật từ góc nhìn Camera cố định hướng thế giới.
    /// Khoa/An đi lên phía xa -> thấy lưng; đi về phía camera -> thấy mặt; đi ngang -> thấy dáng nghiêng.
    /// </summary>
    public enum CharacterCameraFacingView
    {
        Back = 0,
        Front = 1,
        LeftProfile = 2,
        RightProfile = 3,
        ThreeQuarterFront = 4
    }

    /// <summary>
    /// Định danh các Map / Khu vực liên kết qua điểm chuyển map (Map Transition).
    /// </summary>
    public enum WorldMapZoneId
    {
        FarmMain = 0,
        KhuNPC = 1,
        ChoThiTran = 2,
        RungNguyenSinh = 3
    }

    /// <summary>
    /// Phân loại vùng môi trường theo thiết kế Camera 3D phối cảnh 2.5D nghệ thuật.
    /// </summary>
    public enum EnvironmentLayerType3D
    {
        /// <summary>Vùng chơi thật: có địa hình 3D, collider, tương tác và đi lại thực tế.</summary>
        PlayableInteractive3D = 0,
        /// <summary>Vùng bối cảnh xa: dựng bằng model 3D thật kết hợp chiều sâu khí quyển để nhìn từ xa, không đi bộ trực tiếp từ Farm_Main.</summary>
        DistantVista3D = 1
    }

    /// <summary>
    /// Thông số cấu hình của một chế độ Camera trong Camera Design v1.0.
    /// Lưu ý: Pitch (góc nhìn xuống mặt đất) được đo từ phương ngang.
    /// </summary>
    [Serializable]
    public class CameraProfileSpec
    {
        public CameraMode3D Mode { get; set; }
        public string DisplayName { get; set; } = string.Empty;
        public float MinPitchDegrees { get; set; }
        public float MaxPitchDegrees { get; set; }
        public float DefaultPitchDegrees { get; set; }
        public float MinScreenHeightRatio { get; set; }
        public float MaxScreenHeightRatio { get; set; }
        public float DefaultScreenHeightRatio { get; set; }
        public float VerticalFovDegrees { get; set; } = 58f;
        public float LookAheadMeters { get; set; } = 1.6f;
        public float PositionSmoothTime { get; set; } = 0.22f;
        public float RotationSmoothTime { get; set; } = 0.25f;
        public bool IsFrontThreeQuarterDialogue { get; set; }

        public bool IsPitchValid(float pitchDeg) =>
            pitchDeg >= MinPitchDegrees - 0.001f && pitchDeg <= MaxPitchDegrees + 0.001f;

        public bool IsScreenHeightRatioValid(float ratio) =>
            ratio >= MinScreenHeightRatio - 0.001f && ratio <= MaxScreenHeightRatio + 0.001f;
    }

    /// <summary>
    /// Trạng thái vật thể che khuất giữa Camera và nhân vật (cây, mái nhà, cột đá).
    /// </summary>
    [Serializable]
    public class CameraObstructionFadeState
    {
        public string ObjectId { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty; // Tree, Roof, StonePillar, Ruin
        public bool IsBlockingLineOfSight { get; set; }
        public float CurrentAlpha { get; set; } = 1.0f;
        public float TargetAlpha { get; set; } = 1.0f;
    }

    /// <summary>
    /// Thông tin một công trình hoặc cụm kiến trúc 3D trên bản đồ Farm_Main (gồm cả Vùng chơi thật và Vùng bối cảnh xa).
    /// </summary>
    [Serializable]
    public class WorldLandmark3DSpec
    {
        public string Id { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;
        public EnvironmentLayerType3D LayerType { get; set; }
        public bool Is3DModel { get; set; } = true;
        public bool IsFlat2DBackdrop { get; set; } = false;
        public bool HasWalkableColliderInFarmMain { get; set; }
        public float WorldX { get; set; }
        public float WorldY { get; set; }
        public float WorldZ { get; set; }
        public float FootprintWidthMeters { get; set; }
        public float FootprintDepthMeters { get; set; }
        public string TripoReferenceAssetName { get; set; } = string.Empty;
    }

    /// <summary>
    /// Điểm chuyển map (Map Transition Portal).
    /// </summary>
    [Serializable]
    public class MapTransitionPointSpec
    {
        public string PortalId { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;
        public WorldMapZoneId FromZone { get; set; }
        public WorldMapZoneId ToZone { get; set; }
        public float WorldX { get; set; }
        public float WorldZ { get; set; }
        public float TriggerRadiusMeters { get; set; } = 3.5f;
    }

    /// <summary>
    /// Hệ thống Camera Design v1.0 + Bố cục không gian 3D Farm_Main:
    /// - Camera 3D góc chéo thấp, phối cảnh (Perspective, Vertical FOV 54–60°, mặc định 58°).
    /// - Giữ hướng thế giới cố định (không xoay 360° quanh lưng khi nhân vật đổi hướng).
    /// - 6 chế độ tự chuyển mượt mà: Farm, Exploration, Combat, Defense, Dialogue, Scenic.
    /// - Làm mờ vật cản (cây, mái nhà, cột đá) khi che nhân vật.
    /// - Giới hạn vùng thả heo rông <= 1/3 diện tích đồng cỏ gần Khu chuồng trại.
    /// - Khu NPC và Chợ/Thị trấn dựng model 3D nhìn từ xa; từ Farm_Main chỉ có duy nhất 1 điểm chuyển map tới Khu NPC,
    ///   và phải qua Khu NPC mới có điểm chuyển map sang Chợ/Thị trấn.
    /// </summary>
    [Serializable]
    public class CameraAndWorldLayoutSystem
    {
        public const float MinAllowedVerticalFov = 54.0f;
        public const float MaxAllowedVerticalFov = 60.0f;
        public const float DefaultVerticalFov = 58.0f;

        /// <summary>
        /// Hướng chuẩn thế giới của Camera (Yaw = 0° nhìn về phía Bắc-Đông Bắc: thấy Khu chuồng trại bên trái,
        /// Nhà nghiên cứu ở giữa, Trạm thông tin bên phải, Khu NPC phía xa sau trại, và Chợ/Thị trấn + Thác nước + Lâu đài bên phải xa).
        /// </summary>
        public const float LockedWorldCameraYawDegrees = 0.0f;

        /// <summary>
        /// Tỷ lệ diện tích tối đa cho phép heo đi lại tự do gần trang trại: đúng 1/3 (33.33%) diện tích đồng cỏ vùng chơi.
        /// </summary>
        public const float MaxAllowedPigRoamingAreaRatio = 1.0f / 3.0f;

        public bool IsPerspectiveCamera { get; private set; } = true;
        public bool IsOrthographicCamera => !IsPerspectiveCamera;
        public bool IsWorldDirectionLocked { get; private set; } = true;
        public bool AllowFree360Orbit => false;

        public CameraMode3D CurrentMode { get; private set; } = CameraMode3D.Farm;
        public float CurrentPitchDegrees { get; private set; } = 24.0f;
        public float CurrentYawDegrees { get; private set; } = LockedWorldCameraYawDegrees;
        public float CurrentVerticalFov { get; private set; } = DefaultVerticalFov;
        public float CurrentScreenHeightRatio { get; private set; } = 0.21f;
        public float CurrentDistanceMeters { get; private set; }
        public float LookAheadOffsetX { get; private set; }
        public float LookAheadOffsetZ { get; private set; }

        public WorldMapZoneId CurrentMapZone { get; private set; } = WorldMapZoneId.FarmMain;

        // Kích thước vùng đồng cỏ chơi thật (Playable Meadow) trong Farm_Main: 120m x 90m = 10,800 m2
        public float PlayableMeadowMinX { get; private set; } = -60.0f;
        public float PlayableMeadowMaxX { get; private set; } = 60.0f;
        public float PlayableMeadowMinZ { get; private set; } = -45.0f;
        public float PlayableMeadowMaxZ { get; private set; } = 45.0f;

        // Vùng thả heo (bao gồm cả trong chuồng và bãi cỏ sát chuồng trại) giới hạn <= 1/3 diện tích đồng cỏ sát Khu chuồng trại:
        // Kích thước: rộng 60m (-58m .. +2m) x sâu 54m (-6m .. +48m) -> cắt gọn vùng sát chuồng trại = 60m x 54m = 3,240 m2
        // Tỷ lệ: 3,240 / 10,800 = 0.30 (30.0% <= 33.33% = 1/3 diện tích đồng cỏ!).
        public float PigPastureMinX { get; private set; } = -56.0f;
        public float PigPastureMaxX { get; private set; } = 4.0f;
        public float PigPastureMinZ { get; private set; } = -10.0f;
        public float PigPastureMaxZ { get; private set; } = 44.0f;

        private readonly Dictionary<CameraMode3D, CameraProfileSpec> profiles = new Dictionary<CameraMode3D, CameraProfileSpec>();
        private readonly Dictionary<string, CameraObstructionFadeState> obstructions = new Dictionary<string, CameraObstructionFadeState>();
        public List<WorldLandmark3DSpec> Landmarks3D { get; private set; } = new List<WorldLandmark3DSpec>();
        public List<MapTransitionPointSpec> TransitionPortals { get; private set; } = new List<MapTransitionPointSpec>();

        public CameraAndWorldLayoutSystem()
        {
            InitializeCameraProfiles();
            InitializeWorldLandmarksAndTransitions();
            ApplyModeImmediate(CameraMode3D.Farm, characterHeightMeters: 1.70f);
        }

        private void InitializeCameraProfiles()
        {
            profiles[CameraMode3D.Farm] = new CameraProfileSpec
            {
                Mode = CameraMode3D.Farm,
                DisplayName = "Sinh hoạt tại trại (Mặc định)",
                MinPitchDegrees = 22.0f,
                MaxPitchDegrees = 26.0f,
                DefaultPitchDegrees = 24.0f,
                MinScreenHeightRatio = 0.18f,
                MaxScreenHeightRatio = 0.24f,
                DefaultScreenHeightRatio = 0.21f,
                VerticalFovDegrees = 58.0f,
                LookAheadMeters = 1.5f,
                PositionSmoothTime = 0.22f,
                RotationSmoothTime = 0.25f,
                IsFrontThreeQuarterDialogue = false
            };

            profiles[CameraMode3D.Exploration] = new CameraProfileSpec
            {
                Mode = CameraMode3D.Exploration,
                DisplayName = "Khám phá rừng / Sự kiện huyền bí",
                MinPitchDegrees = 18.0f,
                MaxPitchDegrees = 24.0f,
                DefaultPitchDegrees = 21.0f,
                MinScreenHeightRatio = 0.22f,
                MaxScreenHeightRatio = 0.30f,
                DefaultScreenHeightRatio = 0.26f,
                VerticalFovDegrees = 58.0f,
                LookAheadMeters = 2.0f,
                PositionSmoothTime = 0.20f,
                RotationSmoothTime = 0.22f,
                IsFrontThreeQuarterDialogue = false
            };

            profiles[CameraMode3D.Combat] = new CameraProfileSpec
            {
                Mode = CameraMode3D.Combat,
                DisplayName = "Chiến đấu thường",
                MinPitchDegrees = 30.0f,
                MaxPitchDegrees = 38.0f,
                DefaultPitchDegrees = 34.0f,
                MinScreenHeightRatio = 0.14f,
                MaxScreenHeightRatio = 0.20f,
                DefaultScreenHeightRatio = 0.17f,
                VerticalFovDegrees = 58.0f,
                LookAheadMeters = 2.4f,
                PositionSmoothTime = 0.16f,
                RotationSmoothTime = 0.18f,
                IsFrontThreeQuarterDialogue = false
            };

            profiles[CameraMode3D.Defense] = new CameraProfileSpec
            {
                Mode = CameraMode3D.Defense,
                DisplayName = "Thủ trại / Boss lớn",
                MinPitchDegrees = 38.0f,
                MaxPitchDegrees = 48.0f,
                DefaultPitchDegrees = 43.0f,
                MinScreenHeightRatio = 0.09f,
                MaxScreenHeightRatio = 0.15f,
                DefaultScreenHeightRatio = 0.12f,
                VerticalFovDegrees = 58.0f,
                LookAheadMeters = 2.8f,
                PositionSmoothTime = 0.24f,
                RotationSmoothTime = 0.25f,
                IsFrontThreeQuarterDialogue = false
            };

            profiles[CameraMode3D.Dialogue] = new CameraProfileSpec
            {
                Mode = CameraMode3D.Dialogue,
                DisplayName = "Hội thoại / Cốt truyện (Cận cảnh 3/4 Model 3D)",
                MinPitchDegrees = 8.0f,
                MaxPitchDegrees = 16.0f,
                DefaultPitchDegrees = 11.0f,
                MinScreenHeightRatio = 0.45f,
                MaxScreenHeightRatio = 0.65f,
                DefaultScreenHeightRatio = 0.55f,
                VerticalFovDegrees = 54.0f,
                LookAheadMeters = 0.0f,
                PositionSmoothTime = 0.18f,
                RotationSmoothTime = 0.18f,
                IsFrontThreeQuarterDialogue = true
            };

            profiles[CameraMode3D.Scenic] = new CameraProfileSpec
            {
                Mode = CameraMode3D.Scenic,
                DisplayName = "Toàn cảnh điện ảnh (Bầu trời, đồng cỏ, lâu đài xa)",
                MinPitchDegrees = 12.0f,
                MaxPitchDegrees = 18.0f,
                DefaultPitchDegrees = 15.0f,
                MinScreenHeightRatio = 0.05f,
                MaxScreenHeightRatio = 0.10f,
                DefaultScreenHeightRatio = 0.07f,
                VerticalFovDegrees = 56.0f,
                LookAheadMeters = 0.0f,
                PositionSmoothTime = 0.45f,
                RotationSmoothTime = 0.45f,
                IsFrontThreeQuarterDialogue = false
            };
        }

        private void InitializeWorldLandmarksAndTransitions()
        {
            Landmarks3D.Clear();

            // === 1. VÙNG CHƠI THẬT (PLAYABLE INTERACTIVE 3D) ===
            Landmarks3D.Add(new WorldLandmark3DSpec
            {
                Id = "KhuChuongTrai",
                DisplayName = "Khu chuồng trại",
                LayerType = EnvironmentLayerType3D.PlayableInteractive3D,
                Is3DModel = true,
                IsFlat2DBackdrop = false,
                HasWalkableColliderInFarmMain = true,
                WorldX = -32.0f,
                WorldY = 2.2f,
                WorldZ = 18.0f,
                FootprintWidthMeters = 18.0f,
                FootprintDepthMeters = 14.0f,
                TripoReferenceAssetName = "01_MainBarn_KhuChuongTrai.png"
            });

            Landmarks3D.Add(new WorldLandmark3DSpec
            {
                Id = "NhaXayDung",
                DisplayName = "Nhà xây dựng",
                LayerType = EnvironmentLayerType3D.PlayableInteractive3D,
                Is3DModel = true,
                IsFlat2DBackdrop = false,
                HasWalkableColliderInFarmMain = true,
                WorldX = -26.0f,
                WorldY = 4.5f,
                WorldZ = 34.0f,
                FootprintWidthMeters = 12.0f,
                FootprintDepthMeters = 10.0f,
                TripoReferenceAssetName = "02_ConstructionHall_NhaXayDung.png"
            });

            Landmarks3D.Add(new WorldLandmark3DSpec
            {
                Id = "NhaNghienCuu",
                DisplayName = "Nhà nghiên cứu",
                LayerType = EnvironmentLayerType3D.PlayableInteractive3D,
                Is3DModel = true,
                IsFlat2DBackdrop = false,
                HasWalkableColliderInFarmMain = true,
                WorldX = -6.0f,
                WorldY = 3.2f,
                WorldZ = 22.0f,
                FootprintWidthMeters = 12.0f,
                FootprintDepthMeters = 10.0f,
                TripoReferenceAssetName = "03_ResearchObservatory_NhaNghienCuu.png"
            });

            Landmarks3D.Add(new WorldLandmark3DSpec
            {
                Id = "KhoVaNhaBep",
                DisplayName = "Kho & Nhà nấu cám",
                LayerType = EnvironmentLayerType3D.PlayableInteractive3D,
                Is3DModel = true,
                IsFlat2DBackdrop = false,
                HasWalkableColliderInFarmMain = true,
                WorldX = -44.0f,
                WorldY = 2.6f,
                WorldZ = 24.0f,
                FootprintWidthMeters = 9.0f,
                FootprintDepthMeters = 8.0f,
                TripoReferenceAssetName = "05_StorageBarn_KhoCamVaNhaBep.png"
            });

            Landmarks3D.Add(new WorldLandmark3DSpec
            {
                Id = "TramThongTin",
                DisplayName = "Trạm thông tin",
                LayerType = EnvironmentLayerType3D.PlayableInteractive3D,
                Is3DModel = true,
                IsFlat2DBackdrop = false,
                HasWalkableColliderInFarmMain = true,
                WorldX = 28.0f,
                WorldY = 2.8f,
                WorldZ = 14.0f,
                FootprintWidthMeters = 6.0f,
                FootprintDepthMeters = 6.0f,
                TripoReferenceAssetName = "04_InfoBeacon_TramThongTin.png"
            });

            Landmarks3D.Add(new WorldLandmark3DSpec
            {
                Id = "AoNuocDongCo_1",
                DisplayName = "Ao nước đồng cỏ (Trung tâm)",
                LayerType = EnvironmentLayerType3D.PlayableInteractive3D,
                Is3DModel = true,
                IsFlat2DBackdrop = false,
                HasWalkableColliderInFarmMain = true,
                WorldX = 8.0f,
                WorldY = 0.2f,
                WorldZ = -4.0f,
                FootprintWidthMeters = 14.0f,
                FootprintDepthMeters = 8.0f,
                TripoReferenceAssetName = "09_Mythological_GoldenFlora_And_AncientRuin.png"
            });

            Landmarks3D.Add(new WorldLandmark3DSpec
            {
                Id = "AoNuocDongCo_2",
                DisplayName = "Ao nước đồng cỏ (Tiền cảnh trái)",
                LayerType = EnvironmentLayerType3D.PlayableInteractive3D,
                Is3DModel = true,
                IsFlat2DBackdrop = false,
                HasWalkableColliderInFarmMain = true,
                WorldX = -10.0f,
                WorldY = 0.1f,
                WorldZ = -18.0f,
                FootprintWidthMeters = 18.0f,
                FootprintDepthMeters = 9.0f,
                TripoReferenceAssetName = "09_Mythological_GoldenFlora_And_AncientRuin.png"
            });

            Landmarks3D.Add(new WorldLandmark3DSpec
            {
                Id = "AoNuocDongCo_3",
                DisplayName = "Ao nước đồng cỏ (Tiền cảnh phải)",
                LayerType = EnvironmentLayerType3D.PlayableInteractive3D,
                Is3DModel = true,
                IsFlat2DBackdrop = false,
                HasWalkableColliderInFarmMain = true,
                WorldX = 22.0f,
                WorldY = 0.1f,
                WorldZ = -26.0f,
                FootprintWidthMeters = 12.0f,
                FootprintDepthMeters = 7.0f,
                TripoReferenceAssetName = "09_Mythological_GoldenFlora_And_AncientRuin.png"
            });

            // === 2. VÙNG BỐI CẢNH XA (DISTANT 3D VISTA — MODEL 3D THẬT ĐỂ NHÌN TỪ XA, KHÔNG ĐI BỘ TRỰC TIẾP TRONG FARM_MAIN) ===
            Landmarks3D.Add(new WorldLandmark3DSpec
            {
                Id = "KhuNPC_Vista3D",
                DisplayName = "Khu NPC (Bối cảnh 3D phía sau trang trại)",
                LayerType = EnvironmentLayerType3D.DistantVista3D,
                Is3DModel = true,
                IsFlat2DBackdrop = false,
                HasWalkableColliderInFarmMain = false,
                WorldX = 12.0f,
                WorldY = 6.5f,
                WorldZ = 68.0f,
                FootprintWidthMeters = 32.0f,
                FootprintDepthMeters = 24.0f,
                TripoReferenceAssetName = "06_DistantVista_KhuNPC_CottageCluster.png"
            });

            Landmarks3D.Add(new WorldLandmark3DSpec
            {
                Id = "ChoThiTran_Vista3D",
                DisplayName = "Chợ / Thị trấn & Cầu đá (Bối cảnh 3D bên phải sông)",
                LayerType = EnvironmentLayerType3D.DistantVista3D,
                Is3DModel = true,
                IsFlat2DBackdrop = false,
                HasWalkableColliderInFarmMain = false,
                WorldX = 78.0f,
                WorldY = 3.5f,
                WorldZ = 28.0f,
                FootprintWidthMeters = 55.0f,
                FootprintDepthMeters = 48.0f,
                TripoReferenceAssetName = "07_DistantVista_ChoThiTran_BridgeAndMarket.png"
            });

            Landmarks3D.Add(new WorldLandmark3DSpec
            {
                Id = "LauDaiVaThacNuoc_Vista3D",
                DisplayName = "Lâu đài hoàng hôn & Thác nước (Bối cảnh 3D cực xa)",
                LayerType = EnvironmentLayerType3D.DistantVista3D,
                Is3DModel = true,
                IsFlat2DBackdrop = false,
                HasWalkableColliderInFarmMain = false,
                WorldX = 52.0f,
                WorldY = 24.0f,
                WorldZ = 125.0f,
                FootprintWidthMeters = 80.0f,
                FootprintDepthMeters = 60.0f,
                TripoReferenceAssetName = "08_DistantVista_TwilightCastleOnWaterfallCliff.png"
            });

            // === 3. ĐIỂM CHUYỂN MAP (MAP TRANSITION PORTALS) ===
            // Quy tắc bắt buộc:
            // - Trong Farm_Main CHỈ CÓ DUY NHẤT 1 điểm chuyển map dẫn tới Khu NPC (cuối đường mòn phía sau trại).
            // - KHÔNG có điểm chuyển map trực tiếp từ Farm_Main (hay từ Trạm thông tin) sang Chợ / Thị trấn.
            // - Điểm chuyển map sang Chợ / Thị trấn CHỈ nằm ở trong Khu NPC.
            TransitionPortals.Clear();
            TransitionPortals.Add(new MapTransitionPointSpec
            {
                PortalId = "Portal_FarmMain_To_KhuNPC",
                DisplayName = "Đường mòn sau trại -> Khu NPC",
                FromZone = WorldMapZoneId.FarmMain,
                ToZone = WorldMapZoneId.KhuNPC,
                WorldX = 8.0f,
                WorldZ = 43.5f,
                TriggerRadiusMeters = 4.0f
            });

            TransitionPortals.Add(new MapTransitionPointSpec
            {
                PortalId = "Portal_KhuNPC_To_FarmMain",
                DisplayName = "Khu NPC -> Quay về Trang trại",
                FromZone = WorldMapZoneId.KhuNPC,
                ToZone = WorldMapZoneId.FarmMain,
                WorldX = 0.0f,
                WorldZ = -18.0f,
                TriggerRadiusMeters = 4.0f
            });

            TransitionPortals.Add(new MapTransitionPointSpec
            {
                PortalId = "Portal_KhuNPC_To_ChoThiTran",
                DisplayName = "Cầu đá Khu NPC -> Chợ / Thị trấn",
                FromZone = WorldMapZoneId.KhuNPC,
                ToZone = WorldMapZoneId.ChoThiTran,
                WorldX = 24.0f,
                WorldZ = 6.0f,
                TriggerRadiusMeters = 4.0f
            });

            TransitionPortals.Add(new MapTransitionPointSpec
            {
                PortalId = "Portal_ChoThiTran_To_KhuNPC",
                DisplayName = "Chợ / Thị trấn -> Quay lại Khu NPC",
                FromZone = WorldMapZoneId.ChoThiTran,
                ToZone = WorldMapZoneId.KhuNPC,
                WorldX = -24.0f,
                WorldZ = 0.0f,
                TriggerRadiusMeters = 4.0f
            });
        }

        public CameraProfileSpec GetProfile(CameraMode3D mode) => profiles[mode];

        /// <summary>
        /// Tính khoảng cách từ Camera phối cảnh (Perspective) tới nhân vật sao cho chiều cao nhân vật
        /// chiếm đúng tỷ lệ targetScreenHeightRatio trên màn hình với trường nhìn dọc verticalFovDeg.
        /// Công thức hình học phối cảnh: D = H_char / (2 * ratio * tan(FOV_v / 2)).
        /// </summary>
        public static float ComputePerspectiveDistanceForScreenHeight(
            float characterHeightMeters,
            float targetScreenHeightRatio,
            float verticalFovDegrees)
        {
            float safeHeight = Math.Max(0.4f, characterHeightMeters);
            float safeRatio = Math.Clamp(targetScreenHeightRatio, 0.04f, 0.85f);
            float clampedFov = Math.Clamp(verticalFovDegrees, MinAllowedVerticalFov, MaxAllowedVerticalFov);
            double halfFovRad = (clampedFov * 0.5) * Math.PI / 180.0;
            double tanHalfFov = Math.Tan(halfFovRad);
            return (float)(safeHeight / (2.0 * safeRatio * tanHalfFov));
        }

        /// <summary>
        /// Tính tỷ lệ chiều cao nhân vật trên màn hình từ khoảng cách camera phối cảnh.
        /// </summary>
        public static float ComputeScreenHeightRatioFromDistance(
            float characterHeightMeters,
            float distanceMeters,
            float verticalFovDegrees)
        {
            float safeDist = Math.Max(0.5f, distanceMeters);
            float clampedFov = Math.Clamp(verticalFovDegrees, MinAllowedVerticalFov, MaxAllowedVerticalFov);
            double halfFovRad = (clampedFov * 0.5) * Math.PI / 180.0;
            double visibleFrustumHeight = 2.0 * safeDist * Math.Tan(halfFovRad);
            return (float)(characterHeightMeters / visibleFrustumHeight);
        }

        /// <summary>
        /// Đặt ngay chế độ Camera chuẩn hóa.
        /// </summary>
        public void ApplyModeImmediate(CameraMode3D mode, float characterHeightMeters = 1.70f, float characterFacingYawDegrees = 180.0f)
        {
            var spec = profiles[mode];
            CurrentMode = mode;
            CurrentPitchDegrees = spec.DefaultPitchDegrees;
            CurrentVerticalFov = Math.Clamp(spec.VerticalFovDegrees, MinAllowedVerticalFov, MaxAllowedVerticalFov);
            CurrentScreenHeightRatio = spec.DefaultScreenHeightRatio;
            CurrentDistanceMeters = ComputePerspectiveDistanceForScreenHeight(
                characterHeightMeters,
                CurrentScreenHeightRatio,
                CurrentVerticalFov);

            if (spec.IsFrontThreeQuarterDialogue)
            {
                // Góc 3/4 phía trước khuôn mặt nhân vật trên cùng model 3D thật
                CurrentYawDegrees = NormalizeDegrees(characterFacingYawDegrees + 150.0f);
            }
            else
            {
                // Giữ nguyên hướng camera thế giới làm chuẩn (không xoay 360° theo lưng nhân vật)
                CurrentYawDegrees = LockedWorldCameraYawDegrees;
            }
        }

        /// <summary>
        /// Cập nhật mượt (damping) giữa các chế độ Camera + tính Look-ahead theo vận tốc di chuyển của nhân vật.
        /// </summary>
        public void StepCameraUpdate(
            CameraMode3D targetMode,
            float deltaTime,
            float moveInputX,
            float moveInputZ,
            float characterHeightMeters = 1.70f,
            float characterFacingYawDegrees = 180.0f)
        {
            var spec = profiles[targetMode];
            CurrentMode = targetMode;

            float dt = Math.Max(0.001f, deltaTime);
            float blend = Math.Clamp(dt / Math.Max(0.05f, spec.PositionSmoothTime), 0.0f, 1.0f);

            float targetPitch = spec.DefaultPitchDegrees;
            float targetFov = Math.Clamp(spec.VerticalFovDegrees, MinAllowedVerticalFov, MaxAllowedVerticalFov);
            float targetRatio = spec.DefaultScreenHeightRatio;
            float targetDist = ComputePerspectiveDistanceForScreenHeight(characterHeightMeters, targetRatio, targetFov);

            CurrentPitchDegrees = Lerp(CurrentPitchDegrees, targetPitch, blend);
            CurrentVerticalFov = Lerp(CurrentVerticalFov, targetFov, blend);
            CurrentScreenHeightRatio = Lerp(CurrentScreenHeightRatio, targetRatio, blend);
            CurrentDistanceMeters = Lerp(CurrentDistanceMeters, targetDist, blend);

            if (spec.IsFrontThreeQuarterDialogue)
            {
                float targetYaw = NormalizeDegrees(characterFacingYawDegrees + 150.0f);
                CurrentYawDegrees = LerpAngleDegrees(CurrentYawDegrees, targetYaw, blend);
            }
            else
            {
                CurrentYawDegrees = LockedWorldCameraYawDegrees;
            }

            // Look-ahead một khoảng nhỏ theo hướng di chuyển
            float mag = (float)Math.Sqrt(moveInputX * moveInputX + moveInputZ * moveInputZ);
            if (mag > 0.001f && spec.LookAheadMeters > 0.0f)
            {
                float nx = moveInputX / mag;
                float nz = moveInputZ / mag;
                LookAheadOffsetX = Lerp(LookAheadOffsetX, nx * spec.LookAheadMeters, blend);
                LookAheadOffsetZ = Lerp(LookAheadOffsetZ, nz * spec.LookAheadMeters, blend);
            }
            else
            {
                LookAheadOffsetX = Lerp(LookAheadOffsetX, 0.0f, blend);
                LookAheadOffsetZ = Lerp(LookAheadOffsetZ, 0.0f, blend);
            }
        }

        /// <summary>
        /// Xác định góc nhìn thấy của nhân vật trên màn hình khi giữ cố định hướng Camera thế giới:
        /// - Nhân vật đi lên phía xa (+Z) -> thấy lưng (Back)
        /// - Nhân vật đi về phía camera (-Z) -> thấy mặt (Front)
        /// - Nhân vật đi sang trái (-X) -> thấy dáng nghiêng trái (LeftProfile)
        /// - Nhân vật đi sang phải (+X) -> thấy dáng nghiêng phải (RightProfile)
        /// </summary>
        public CharacterCameraFacingView GetCharacterFacingRelativeToCamera(float moveDirX, float moveDirZ)
        {
            if (CurrentMode == CameraMode3D.Dialogue)
            {
                return CharacterCameraFacingView.ThreeQuarterFront;
            }

            if (Math.Abs(moveDirZ) >= Math.Abs(moveDirX))
            {
                return moveDirZ >= 0.0f
                    ? CharacterCameraFacingView.Back
                    : CharacterCameraFacingView.Front;
            }

            return moveDirX < 0.0f
                ? CharacterCameraFacingView.LeftProfile
                : CharacterCameraFacingView.RightProfile;
        }

        /// <summary>
        /// Tính tọa độ đặt Camera 3D từ vị trí mục tiêu (nhân vật + look-ahead), góc Pitch nhìn xuống mặt đất
        /// và hướng Yaw khóa thế giới.
        /// </summary>
        public (float camX, float camY, float camZ) ComputeCameraWorldPosition(
            float targetX,
            float targetY,
            float targetZ,
            float focusHeightOffsetMeters = 1.05f)
        {
            float focusX = targetX + LookAheadOffsetX;
            float focusY = targetY + focusHeightOffsetMeters;
            float focusZ = targetZ + LookAheadOffsetZ;

            double pitchRad = CurrentPitchDegrees * Math.PI / 180.0;
            double yawRad = CurrentYawDegrees * Math.PI / 180.0;

            double horizontalDist = CurrentDistanceMeters * Math.Cos(pitchRad);
            double verticalOffset = CurrentDistanceMeters * Math.Sin(pitchRad);

            // Camera đặt lùi về phía sau hướng nhìn (ngược chiều vector forward của Yaw)
            float camX = (float)(focusX - Math.Sin(yawRad) * horizontalDist);
            float camY = (float)(focusY + verticalOffset);
            float camZ = (float)(focusZ - Math.Cos(yawRad) * horizontalDist);

            return (camX, camY, camZ);
        }

        /// <summary>
        /// Xử lý làm mờ vật thể che khuất (cây, mái nhà, cột đá) giữa Camera và nhân vật.
        /// Khi che nhân vật, alpha giảm mượt xuống 0.22f; khi hết che, alpha hồi về 1.0f.
        /// </summary>
        public float UpdateObstructionFade(string objectId, string category, bool isBlocking, float deltaTime, float fadedAlpha = 0.22f)
        {
            if (!obstructions.TryGetValue(objectId, out var state))
            {
                state = new CameraObstructionFadeState
                {
                    ObjectId = objectId,
                    Category = category,
                    CurrentAlpha = 1.0f,
                    TargetAlpha = 1.0f
                };
                obstructions[objectId] = state;
            }

            state.IsBlockingLineOfSight = isBlocking;
            state.TargetAlpha = isBlocking ? Math.Clamp(fadedAlpha, 0.12f, 0.45f) : 1.0f;

            float speed = 6.0f * Math.Max(0.001f, deltaTime);
            state.CurrentAlpha = Lerp(state.CurrentAlpha, state.TargetAlpha, Math.Clamp(speed, 0.0f, 1.0f));
            return state.CurrentAlpha;
        }

        /// <summary>
        /// Tính diện tích đồng cỏ chơi thật (m2).
        /// </summary>
        public float GetPlayableMeadowAreaSqMeters() =>
            Math.Max(1.0f, (PlayableMeadowMaxX - PlayableMeadowMinX) * (PlayableMeadowMaxZ - PlayableMeadowMinZ));

        /// <summary>
        /// Tính diện tích vùng thả heo tự do sát Khu chuồng trại (m2).
        /// </summary>
        public float GetPigPastureAreaSqMeters() =>
            Math.Max(0.0f, (PigPastureMaxX - PigPastureMinX) * (PigPastureMaxZ - PigPastureMinZ));

        /// <summary>
        /// Tỷ lệ vùng thả heo so với tổng diện tích đồng cỏ vùng chơi (bắt buộc <= 1/3 = 0.3333f).
        /// </summary>
        public float GetPigPastureAreaRatio() =>
            GetPigPastureAreaSqMeters() / GetPlayableMeadowAreaSqMeters();

        /// <summary>
        /// Kiểm tra một tọa độ (x, z) có nằm trong vùng 1/3 đồng cỏ sát Khu chuồng trại dành cho heo hay không.
        /// </summary>
        public bool IsInsidePigRoamingZone(float worldX, float worldZ) =>
            worldX >= PigPastureMinX && worldX <= PigPastureMaxX &&
            worldZ >= PigPastureMinZ && worldZ <= PigPastureMaxZ;

        /// <summary>
        /// Kẹp (clamp) tọa độ đi lại của heo thả rông để heo tuyệt đối không bao giờ đi quá 1/3 diện tích đồng cỏ gần trang trại.
        /// </summary>
        public (float clampedX, float clampedZ) ClampPigPositionToAllowedPasture(float worldX, float worldZ)
        {
            float cx = Math.Clamp(worldX, PigPastureMinX, PigPastureMaxX);
            float cz = Math.Clamp(worldZ, PigPastureMinZ, PigPastureMaxZ);
            return (cx, cz);
        }

        /// <summary>
        /// Kiểm tra xem từ khu vực hiện tại có điểm chuyển map trực tiếp sang targetZone hay không.
        /// Quy tắc bất khả xâm phạm:
        /// - Từ Farm_Main CHỈ chuyển tiếp được sang Khu NPC (cuối đường phía sau trại).
        /// - Từ Farm_Main KHÔNG THỂ chuyển trực tiếp sang Chợ / Thị trấn.
        /// - Muốn sang Chợ / Thị trấn bắt buộc phải đang ở Khu NPC rồi đi qua điểm chuyển map của Khu NPC.
        /// </summary>
        public bool CanTransitionDirectly(WorldMapZoneId fromZone, WorldMapZoneId toZone)
        {
            for (int i = 0; i < TransitionPortals.Count; i++)
            {
                var p = TransitionPortals[i];
                if (p.FromZone == fromZone && p.ToZone == toZone)
                {
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// Thực hiện chuyển map qua điểm chuyển tiếp.
        /// </summary>
        public (bool success, string message, WorldMapZoneId resultingZone) TryTransitionToZone(WorldMapZoneId targetZone)
        {
            if (CurrentMapZone == targetZone)
            {
                return (true, $"Đang ở tại {targetZone}.", CurrentMapZone);
            }

            if (!CanTransitionDirectly(CurrentMapZone, targetZone))
            {
                if (CurrentMapZone == WorldMapZoneId.FarmMain && targetZone == WorldMapZoneId.ChoThiTran)
                {
                    return (
                        false,
                        "Không có đường đi trực tiếp từ Trang trại sang Chợ / Thị trấn! Phải đi qua điểm chuyển map Khu NPC ở cuối đường phía sau trại trước.",
                        CurrentMapZone
                    );
                }

                return (
                    false,
                    $"Không có điểm chuyển map trực tiếp từ {CurrentMapZone} tới {targetZone}.",
                    CurrentMapZone
                );
            }

            var prev = CurrentMapZone;
            CurrentMapZone = targetZone;
            return (true, $"Đã chuyển map từ {prev} sang {CurrentMapZone} qua điểm chuyển tiếp.", CurrentMapZone);
        }

        /// <summary>
        /// Lấy tất cả điểm chuyển map khả dụng tại khu vực hiện tại.
        /// </summary>
        public List<MapTransitionPointSpec> GetPortalsInZone(WorldMapZoneId zone)
        {
            return TransitionPortals.FindAll(p => p.FromZone == zone);
        }

        private static float Lerp(float a, float b, float t) => a + (b - a) * Math.Clamp(t, 0.0f, 1.0f);

        private static float NormalizeDegrees(float deg)
        {
            float d = deg % 360.0f;
            if (d < 0.0f) d += 360.0f;
            return d;
        }

        private static float LerpAngleDegrees(float a, float b, float t)
        {
            float delta = ((b - a + 540.0f) % 360.0f) - 180.0f;
            return NormalizeDegrees(a + delta * Math.Clamp(t, 0.0f, 1.0f));
        }
    }
}
