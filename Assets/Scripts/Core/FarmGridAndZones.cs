using System;
using System.Collections.Generic;

namespace PigTycoon.Core
{
    public enum BuildingFootprintSize
    {
        Small_1x1 = 1,   // 1x1 Ô Xây = 2x2 m
        Medium_2x2 = 2,  // 2x2 Ô Xây = 4x4 m
        Large_3x3 = 3    // 3x3 Ô Xây = 6x6 m
    }

    public enum BuildPlacementStatus
    {
        Green_Valid,
        Yellow_Warning,
        Red_Forbidden
    }

    public enum CarcassProcessMethod
    {
        Incinerate_ThieuHuy,       // 45 Vàng + 8 Lít, 6 giờ, 0 phụ phẩm, triệt mầm bệnh
        BioRecycle_TaiCheSinhHoc   // 80 Vàng + 10 Lít, 18 giờ, thu hồi 25–32% cám (cấm dùng cho Nhóm D)
    }

    [Serializable]
    public sealed class PlacedFarmStructure
    {
        public string InstanceId = "";
        public string StructureType = "";
        public int CellX;
        public int CellY;
        public BuildingFootprintSize Size = BuildingFootprintSize.Small_1x1;
        public int GoldCost;
        public float PlacedAtGameMinutes;
        public bool ProvidesServiceFeed;
        public bool ProvidesServiceWater;
        public bool ProvidesServiceShelter;
        public bool ProvidesCooling;

        public (float wx, float wy) WorldCenter(float originX = -45f, float originY = -32f)
        {
            float spanMeters = (int)Size * FarmGridAndZones.BuildCellMeters;
            return (
                originX + CellX * FarmGridAndZones.BuildCellMeters + spanMeters * 0.5f,
                originY + CellY * FarmGridAndZones.BuildCellMeters + spanMeters * 0.5f);
        }
    }

    [Serializable]
    public sealed class CarcassProcessingJob
    {
        public string PigId = "";
        public CarcassProcessMethod Method;
        public int HoursRemaining;
        public int FeedPortionsYield;
        public bool Completed;
    }

    [Serializable]
    public sealed class FarmChecklistReportGdd310
    {
        public bool GroupA_ScaleAndGridValid;
        public bool GroupB_FourSectorsAndMonsterSpawnsValid;
        public bool GroupC_GatesAndHiddenGateZonesValid;
        public bool GroupD_RefugeAndQuarantineValid;
        public bool GroupE_ProcessingLotAndDistanceValid;
        public bool GroupF_ServiceAndCoolingRadiusValid;
        public bool GroupG_MysticMerchantAndNoSpawnBufferValid;
        public bool GroupH_SortingLayersAndCameraFramesValid;

        public bool AllPassed =>
            GroupA_ScaleAndGridValid &&
            GroupB_FourSectorsAndMonsterSpawnsValid &&
            GroupC_GatesAndHiddenGateZonesValid &&
            GroupD_RefugeAndQuarantineValid &&
            GroupE_ProcessingLotAndDistanceValid &&
            GroupF_ServiceAndCoolingRadiusValid &&
            GroupG_MysticMerchantAndNoSpawnBufferValid &&
            GroupH_SortingLayersAndCameraFramesValid;
    }

    /// <summary>
    /// Hệ thống lưới Ô Xây (2×2m), Ô Đất (10×10m = 25 Ô Xây), Tầm phục vụ 30m, Nguồn mát 25m (Lam Khê 20m),
    /// Chen Chúc cục bộ, Nơi Trú Đàn, Khu Cách Ly, Khu Xử Lý xác, Điểm Bí nhân, 4 vùng ẩn Cổng,
    /// và bộ kiểm định nghiệm thu Farm_Main theo GDD 3.5–3.10 (P0.16, P1.05, P2.03, P2.23, P3.14, P3.18, P5.11).
    /// </summary>
    public sealed class FarmGridAndZones
    {
        public const float BuildCellMeters = 2.0f;
        public const float LandPlotMeters = 10.0f;
        public const int BuildCellsPerLandPlot = 25; // 5x5 Ô Xây = 10x10m
        public const int StartingLandPlots = 12;
        public const int MaxExpandedLandPlots = 40;

        public const float ServiceRadiusMeters = 30.0f;
        public const float CoolingRadiusMeters = 25.0f;
        public const float LamKheWaterCoolingRadiusMeters = 20.0f;
        public const float NoSpawnBufferRadiusMeters = 25.0f;
        public const float MysticSpawnMaxDistanceOutsideFenceMeters = 30.0f;
        public const float MinProcessingDistanceFromWaterOrShelterMeters = 20.0f;

        public float OriginX { get; }
        public float OriginY { get; }
        public int GridWidthCells { get; }
        public int GridHeightCells { get; }

        private readonly bool[,] occupiedCells;
        private readonly bool[,] secondaryPathCells;
        private readonly bool[,] forbiddenTerrainCells;

        public List<PlacedFarmStructure> Structures { get; } = new List<PlacedFarmStructure>();
        public List<FarmServicePoint> SceneServicePoints { get; set; } = new List<FarmServicePoint>();
        public List<CarcassProcessingJob> ActiveCarcassJobs { get; } = new List<CarcassProcessingJob>();

        // Khu vực chuyên biệt theo GDD 3.8–3.10
        public int HerdRefugePlotCount { get; set; } = 4; // 8 heo / Ô Đất = 32 chỗ mặc định
        public int HerdRefugeEntranceCount { get; set; } = 2;
        public float HerdRefugeMinEntranceWidthMeters { get; set; } = 6.2f;
        public (float x, float y) HerdRefugeCenter { get; set; } = (0f, 6f);

        public int QuarantinePlotCount { get; set; } = 2; // ≥ 2 Ô Đất
        public bool QuarantineHasSeparateGate { get; set; } = true;
        public (float x, float y) QuarantineCenter { get; set; } = (-28f, 16f);

        public int ProcessingLotSlots { get; set; } = 4; // 4 ô, nâng cấp lên 8 ô
        public (float x, float y) ProcessingLotCenter { get; set; } = (32f, -22f);

        // ≥ 4 vùng ẩn đủ chuẩn biến thành Cổng (GDD 3.9 / P3.18)
        public List<MapRect> HiddenGateCandidateZones { get; } = new List<MapRect>
        {
            new MapRect(-38f, 18f, 12f, 12f),  // Tây Bắc
            new MapRect(26f, 18f, 12f, 12f),   // Đông Bắc
            new MapRect(26f, -28f, 12f, 12f),  // Đông Nam
            new MapRect(-38f, -28f, 12f, 12f)  // Tây Nam
        };

        // Điểm xuất hiện Bí nhân ngoài rào trong 30m có đường đi rõ đến cổng chính
        public (float x, float y) MysticMerchantSpawnPoint { get; set; } = (0f, -26f);

        // 8–12 điểm vào quái chia 4 Khu Hướng (ngoài vùng đệm 25m từ tâm chuồng)
        public List<(ApproachSector sector, float x, float y)> MonsterSpawnPoints { get; } = new List<(ApproachSector, float, float)>
        {
            (ApproachSector.North, -14f, 29f),
            (ApproachSector.North, 0f, 30f),
            (ApproachSector.North, 14f, 29f),
            (ApproachSector.East, 41f, 10f),
            (ApproachSector.East, 42f, -8f),
            (ApproachSector.South, -12f, -29f),
            (ApproachSector.South, 12f, -29f),
            (ApproachSector.West, -41f, 10f),
            (ApproachSector.West, -42f, -8f),
            (ApproachSector.West, -40f, 0f)
        };

        public FarmGridAndZones(float originX = -45f, float originY = -32f, int widthMeters = 90, int heightMeters = 64, bool seedDefaults = true)
        {
            OriginX = originX;
            OriginY = originY;
            GridWidthCells = Math.Max(10, (int)(widthMeters / BuildCellMeters));
            GridHeightCells = Math.Max(10, (int)(heightMeters / BuildCellMeters));
            occupiedCells = new bool[GridWidthCells, GridHeightCells];
            secondaryPathCells = new bool[GridWidthCells, GridHeightCells];
            forbiddenTerrainCells = new bool[GridWidthCells, GridHeightCells];

            if (seedDefaults) SeedDefaultFarmInfrastructure();
        }

        private void SeedDefaultFarmInfrastructure()
        {
            // Đặt sẵn Máng ăn, Bồn nước, Mái trú, Bơm làm mát ở trung tâm chuồng để phủ tầm 30m / 25m
            TryPlaceStructure("Feeder_Main", 21, 15, BuildingFootprintSize.Small_1x1, 100, 0f, false, true, false, false, false);
            TryPlaceStructure("Water_Main", 23, 15, BuildingFootprintSize.Small_1x1, 100, 0f, false, false, true, false, false);
            TryPlaceStructure("Shelter_Main", 22, 17, BuildingFootprintSize.Medium_2x2, 200, 0f, false, false, false, true, false);
            TryPlaceStructure("MudPit_Cooling", 19, 15, BuildingFootprintSize.Medium_2x2, 150, 0f, false, false, false, false, true);
        }

        public void RestoreGridState(List<PlacedFarmStructure>? savedStructures, List<CarcassProcessingJob>? savedJobs)
        {
            if (savedStructures != null && savedStructures.Count > 0)
            {
                Array.Clear(occupiedCells, 0, occupiedCells.Length);
                Structures.Clear();
                foreach (var s in savedStructures)
                {
                    if (s == null) continue;
                    int span = (int)s.Size;
                    for (int x = s.CellX; x < s.CellX + span; x++)
                    {
                        for (int y = s.CellY; y < s.CellY + span; y++)
                        {
                            if (x >= 0 && x < GridWidthCells && y >= 0 && y < GridHeightCells)
                            {
                                occupiedCells[x, y] = true;
                            }
                        }
                    }
                    Structures.Add(s);
                }
            }

            if (savedJobs != null)
            {
                ActiveCarcassJobs.Clear();
                foreach (var job in savedJobs)
                {
                    if (job != null) ActiveCarcassJobs.Add(job);
                }
            }
        }

        public (int cx, int cy) WorldToBuildCell(float worldX, float worldY)
        {
            int cx = (int)Math.Floor((worldX - OriginX) / BuildCellMeters);
            int cy = (int)Math.Floor((worldY - OriginY) / BuildCellMeters);
            return (cx, cy);
        }

        public void SetSecondaryPathCell(int cx, int cy, bool isSecondaryPath)
        {
            if (cx >= 0 && cx < GridWidthCells && cy >= 0 && cy < GridHeightCells)
            {
                secondaryPathCells[cx, cy] = isSecondaryPath;
            }
        }

        public void SetForbiddenTerrainCell(int cx, int cy, bool forbidden)
        {
            if (cx >= 0 && cx < GridWidthCells && cy >= 0 && cy < GridHeightCells)
            {
                forbiddenTerrainCells[cx, cy] = forbidden;
            }
        }

        /// <summary>
        /// Kiểm tra ô đặt công trình (GDD 3.5 / P1.05):
        /// - Xanh: Hợp lệ hoàn toàn.
        /// - Vàng: Đặt được nhưng chắn lối phụ hoặc mật độ công trình cục bộ dày.
        /// - Đỏ: Cấm đặt (trùng công trình, địa hình cấm, ngoài biên, hoặc đang trong đợt quái).
        /// </summary>
        public BuildPlacementStatus EvaluatePlacement(int cellX, int cellY, BuildingFootprintSize size, bool isDuringCombatWave = false)
        {
            if (isDuringCombatWave)
            {
                return BuildPlacementStatus.Red_Forbidden;
            }

            int span = (int)size;
            if (cellX < 0 || cellY < 0 || cellX + span > GridWidthCells || cellY + span > GridHeightCells)
            {
                return BuildPlacementStatus.Red_Forbidden;
            }

            bool touchesSecondaryPath = false;
            for (int x = cellX; x < cellX + span; x++)
            {
                for (int y = cellY; y < cellY + span; y++)
                {
                    if (occupiedCells[x, y] || forbiddenTerrainCells[x, y])
                    {
                        return BuildPlacementStatus.Red_Forbidden;
                    }
                    if (secondaryPathCells[x, y])
                    {
                        touchesSecondaryPath = true;
                    }
                }
            }

            // Kiểm tra mật độ công trình lân cận
            int nearbyOccupied = 0;
            for (int x = Math.Max(0, cellX - 2); x < Math.Min(GridWidthCells, cellX + span + 2); x++)
            {
                for (int y = Math.Max(0, cellY - 2); y < Math.Min(GridHeightCells, cellY + span + 2); y++)
                {
                    if (occupiedCells[x, y]) nearbyOccupied++;
                }
            }

            if (touchesSecondaryPath || nearbyOccupied >= 10)
            {
                return BuildPlacementStatus.Yellow_Warning;
            }

            return BuildPlacementStatus.Green_Valid;
        }

        public (bool placed, BuildPlacementStatus status, PlacedFarmStructure? structure) TryPlaceStructure(
            string structureType,
            int cellX,
            int cellY,
            BuildingFootprintSize size,
            int goldCost,
            float currentGameMinutes,
            bool isDuringCombatWave = false,
            bool providesFeed = false,
            bool providesWater = false,
            bool providesShelter = false,
            bool providesCooling = false)
        {
            var status = EvaluatePlacement(cellX, cellY, size, isDuringCombatWave);
            if (status == BuildPlacementStatus.Red_Forbidden)
            {
                return (false, status, null);
            }

            int span = (int)size;
            for (int x = cellX; x < cellX + span; x++)
            {
                for (int y = cellY; y < cellY + span; y++)
                {
                    occupiedCells[x, y] = true;
                }
            }

            var item = new PlacedFarmStructure
            {
                InstanceId = $"struct_{Guid.NewGuid():N}",
                StructureType = structureType,
                CellX = cellX,
                CellY = cellY,
                Size = size,
                GoldCost = goldCost,
                PlacedAtGameMinutes = currentGameMinutes,
                ProvidesServiceFeed = providesFeed,
                ProvidesServiceWater = providesWater,
                ProvidesServiceShelter = providesShelter,
                ProvidesCooling = providesCooling
            };

            Structures.Add(item);
            return (true, status, item);
        }

        /// <summary>
        /// Tháo dỡ / di dời công trình (GDD 13.3 / P1.05):
        /// Không cho tháo khi đang đợt quái. Hoàn 100% vàng trước đợt quái đầu hoặc trong 60 phút game sau khi đặt; sau đó hoàn 70%.
        /// </summary>
        public (bool removed, int refundedGold) TryDemolishStructure(
            string instanceId,
            float currentGameMinutes,
            bool beforeFirstWave,
            bool isDuringCombatWave)
        {
            if (isDuringCombatWave)
            {
                return (false, 0);
            }

            var target = Structures.Find(s => s.InstanceId == instanceId);
            if (target == null)
            {
                return (false, 0);
            }

            int span = (int)target.Size;
            for (int x = target.CellX; x < target.CellX + span; x++)
            {
                for (int y = target.CellY; y < target.CellY + span; y++)
                {
                    if (x >= 0 && x < GridWidthCells && y >= 0 && y < GridHeightCells)
                    {
                        occupiedCells[x, y] = false;
                    }
                }
            }

            Structures.Remove(target);
            bool fullRefund = beforeFirstWave || (currentGameMinutes - target.PlacedAtGameMinutes) <= 60f;
            int refund = fullRefund ? target.GoldCost : (int)Math.Round(target.GoldCost * 0.70f);
            return (true, refund);
        }

        /// <summary>
        /// Kiểm tra tầm phục vụ 30m (Máng ăn + Bồn nước + Mái trú). Ngoài 30m: tăng trọng -10%, Tinh thần -1/giờ (GDD 3.7).
        /// </summary>
        public (bool withinServiceRadius, float growthMultiplier, int hourlyMoodDelta) EvaluateServiceCoverage(float worldX, float worldY)
        {
            float rSq = ServiceRadiusMeters * ServiceRadiusMeters;
            bool hasFeed = false;
            bool hasWater = false;
            bool hasShelter = false;

            for (int i = 0; i < Structures.Count; i++)
            {
                var s = Structures[i];
                var (wx, wy) = s.WorldCenter(OriginX, OriginY);
                float dSq = (worldX - wx) * (worldX - wx) + (worldY - wy) * (worldY - wy);
                if (dSq <= rSq)
                {
                    if (s.ProvidesServiceFeed) hasFeed = true;
                    if (s.ProvidesServiceWater) hasWater = true;
                    if (s.ProvidesServiceShelter) hasShelter = true;
                }
            }

            foreach (var service in SceneServicePoints)
            {
                float dx = worldX - service.X, dy = worldY - service.Y;
                if (dx * dx + dy * dy > rSq) continue;
                hasFeed |= service.Feed; hasWater |= service.Water; hasShelter |= service.Shelter;
            }
            bool covered = hasFeed && hasWater && hasShelter;
            return covered ? (true, 1.0f, 0) : (false, 0.90f, -1);
        }

        /// <summary>
        /// Kiểm tra nguồn mát 25m (hoặc 20m từ Bồn nước đối với dòng gen Lam Khê) trong thời tiết Nắng Gắt (GDD 3.7 & 5.2).
        /// </summary>
        public bool IsWithinCoolingRadius(float worldX, float worldY, GeneLineId geneLine = GeneLineId.HongDien)
        {
            float coolSq = CoolingRadiusMeters * CoolingRadiusMeters;
            float lamKheWaterSq = LamKheWaterCoolingRadiusMeters * LamKheWaterCoolingRadiusMeters;
            foreach (var service in SceneServicePoints)
            {
                float dx = worldX - service.X, dy = worldY - service.Y, distance = dx * dx + dy * dy;
                if ((service.Cooling && distance <= coolSq) || (geneLine == GeneLineId.LamKhe && service.Water && distance <= lamKheWaterSq)) return true;
            }

            for (int i = 0; i < Structures.Count; i++)
            {
                var s = Structures[i];
                var (wx, wy) = s.WorldCenter(OriginX, OriginY);
                float dSq = (worldX - wx) * (worldX - wx) + (worldY - wy) * (worldY - wy);
                if (s.ProvidesCooling && dSq <= coolSq)
                {
                    return true;
                }
                if (geneLine == GeneLineId.LamKhe && s.ProvidesServiceWater && dSq <= lamKheWaterSq)
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Đánh giá trạng thái "Chen Chúc cục bộ" (GDD 3.7 / P2.23):
        /// Kích hoạt khi > 16 heo trong 1 Ô Đất (10×10m) suốt ≥ 1 giờ hoặc > 12 heo tranh 1 máng/bồn.
        /// Tác động: Lây bệnh Nhóm A ×1.3, lan hoảng +10%, Tinh thần -1/giờ.
        /// </summary>
        public static (bool isCrowded, float groupAContagionMultiplier, float panicSpreadBonus, int hourlyMoodPenalty) EvaluateLocalCrowding(
            int pigsIn10x10Plot,
            float hoursInPlot,
            int maxPigsAtSingleTroughOrFeeder)
        {
            bool crowded = (pigsIn10x10Plot > 16 && hoursInPlot >= 1.0f) || maxPigsAtSingleTroughOrFeeder > 12;
            if (!crowded)
            {
                return (false, 1.0f, 0f, 0);
            }
            return (true, 1.30f, 0.10f, -1);
        }

        /// <summary>
        /// Chi phí thể lực khi kéo xác heo về Khu Xử Lý: +1 STA mỗi 10m kéo (GDD 4.6).
        /// </summary>
        public static int CalculateDragCarcassStaminaCost(float distanceMeters)
        {
            if (distanceMeters <= 0f) return 0;
            return (int)Math.Ceiling(distanceMeters / 10.0f);
        }

        /// <summary>
        /// Kiểm tra khoảng cách Khu Xử Lý tới Bồn nước / Mái trú gần nhất. Nếu < 20m thì bị phạt -1 SC/ngày (GDD 3.8).
        /// </summary>
        public int GetProcessingLotDailyHabitatPenalty()
        {
            float minDistSq = MinProcessingDistanceFromWaterOrShelterMeters * MinProcessingDistanceFromWaterOrShelterMeters;
            for (int i = 0; i < Structures.Count; i++)
            {
                var s = Structures[i];
                if (!s.ProvidesServiceWater && !s.ProvidesServiceShelter) continue;
                var (wx, wy) = s.WorldCenter(OriginX, OriginY);
                float dx = ProcessingLotCenter.x - wx;
                float dy = ProcessingLotCenter.y - wy;
                if (dx * dx + dy * dy < minDistSq)
                {
                    return -1;
                }
            }
            return 0;
        }

        /// <summary>
        /// Bắt đầu xử lý xác tại Khu Xử Lý (GDD 4.6 / P2.03):
        /// - Thiêu Hủy: 45 Vàng + 8 Lít nhiên liệu, 6 giờ, 0 phụ phẩm.
        /// - Tái Chế Sinh Học: 80 Vàng + 10 Lít nhiên liệu, 18 giờ, thu hồi 25–32% khẩu phần cám (cấm dùng cho heo chết vì bệnh Nhóm D).
        /// </summary>
        public (bool started, string message, CarcassProcessingJob? job) StartCarcassProcessing(
            Farm farm,
            string pigId,
            float pigWeightKg,
            CarcassProcessMethod method,
            DiseaseId? causeOfDeath = null)
        {
            if (ActiveCarcassJobs.FindAll(j => !j.Completed).Count >= ProcessingLotSlots)
            {
                return (false, "Khu Xử Lý đã đầy toàn bộ ô chứa tạm!", null);
            }

            if (method == CarcassProcessMethod.BioRecycle_TaiCheSinhHoc &&
                causeOfDeath.HasValue &&
                DiseaseBoard.GroupOf(causeOfDeath.Value) == DiseaseGroup.D)
            {
                return (false, "Xác nhiễm bệnh Dị Tượng (Nhóm D) bắt buộc phải Thiêu Hủy, cấm Tái Chế Sinh Học!", null);
            }

            int goldCost = method == CarcassProcessMethod.Incinerate_ThieuHuy ? 45 : 80;
            int fuelCost = method == CarcassProcessMethod.Incinerate_ThieuHuy ? 8 : 10;
            int hours = method == CarcassProcessMethod.Incinerate_ThieuHuy ? 6 : 18;

            if (farm.Gold < goldCost || farm.FuelLiters < fuelCost)
            {
                return (false, "Không đủ Vàng hoặc Nhiên liệu để vận hành Khu Xử Lý!", null);
            }

            farm.Gold -= goldCost;
            farm.FuelLiters -= fuelCost;

            int yieldPortions = 0;
            if (method == CarcassProcessMethod.BioRecycle_TaiCheSinhHoc)
            {
                // 28.5% trung bình trong dải [25%, 32%] quy đổi khẩu phần cám (3.5 kg/khẩu phần trưởng thành)
                yieldPortions = Math.Max(1, (int)Math.Round((pigWeightKg * 0.285f) / 3.5f));
            }

            var job = new CarcassProcessingJob
            {
                PigId = pigId,
                Method = method,
                HoursRemaining = hours,
                FeedPortionsYield = yieldPortions,
                Completed = false
            };

            ActiveCarcassJobs.Add(job);
            if (farm.CorpseLotCount > 0) farm.CorpseLotCount--;

            return (true, method == CarcassProcessMethod.Incinerate_ThieuHuy
                ? "Đã bắt đầu Thiêu Hủy xác heo (6 giờ)."
                : $"Đã bắt đầu Tái Chế Sinh Học (18 giờ, dự kiến +{yieldPortions} khẩu phần cám).", job);
        }

        public int TickCarcassProcessingHours(int elapsedHours)
        {
            int recoveredFeedPortions = 0;
            for (int i = 0; i < ActiveCarcassJobs.Count; i++)
            {
                var job = ActiveCarcassJobs[i];
                if (job.Completed) continue;

                job.HoursRemaining = Math.Max(0, job.HoursRemaining - elapsedHours);
                if (job.HoursRemaining == 0)
                {
                    job.Completed = true;
                    recoveredFeedPortions += job.FeedPortionsYield;
                }
            }
            return recoveredFeedPortions;
        }

        /// <summary>
        /// Kiểm định toàn bộ 8 nhóm tiêu chuẩn nghiệm thu Farm_Main theo GDD 3.10 (P5.11).
        /// </summary>
        public FarmChecklistReportGdd310 ValidateChecklistGdd310()
        {
            var report = new FarmChecklistReportGdd310();

            // Nhóm A: Tỷ lệ lưới Ô Xây 2x2m, Ô Đất 10x10m = 25 Ô Xây, khởi đầu 12 Ô Đất mở rộng 40 Ô Đất
            report.GroupA_ScaleAndGridValid =
                Math.Abs(BuildCellMeters - 2.0f) < 0.001f &&
                Math.Abs(LandPlotMeters - 10.0f) < 0.001f &&
                BuildCellsPerLandPlot == 25 &&
                StartingLandPlots == 12 &&
                MaxExpandedLandPlots == 40;

            // Nhóm B: Đủ 4 Khu Hướng (Bắc, Đông, Nam, Tây) và 8–12 điểm vào quái
            var sectors = new HashSet<ApproachSector>();
            foreach (var sp in MonsterSpawnPoints) sectors.Add(sp.sector);
            report.GroupB_FourSectorsAndMonsterSpawnsValid =
                sectors.Count == 4 &&
                MonsterSpawnPoints.Count >= 8 &&
                MonsterSpawnPoints.Count <= 12;

            // Nhóm C: 4 cổng chính ≥ 6m và ≥ 4 vùng ẩn đủ chuẩn biến thành Cổng (12x12m)
            BoundaryGap[] gates = PastureBoundary.FourGates();
            bool gatesOk = gates.Length == 4;
            for (int i = 0; i < gates.Length; i++)
            {
                if (gates[i].WidthMeters < 6.0f) gatesOk = false;
            }
            report.GroupC_GatesAndHiddenGateZonesValid =
                gatesOk && HiddenGateCandidateZones.Count >= 4;

            // Nhóm D: Nơi Trú Đàn (≥ 2 lối vào ≥ 6m) & Khu Cách Ly (≥ 2 Ô Đất, cổng riêng)
            report.GroupD_RefugeAndQuarantineValid =
                HerdRefugeEntranceCount >= 2 &&
                HerdRefugeMinEntranceWidthMeters >= 6.0f &&
                QuarantinePlotCount >= 2 &&
                QuarantineHasSeparateGate;

            // Nhóm E: Khu Xử Lý (4/8 ô) cách nguồn nước và mái trú ≥ 20m
            report.GroupE_ProcessingLotAndDistanceValid =
                (ProcessingLotSlots == 4 || ProcessingLotSlots == 8) &&
                GetProcessingLotDailyHabitatPenalty() == 0;

            // Nhóm F: Tầm phục vụ 30m và nguồn mát 25m phủ trung tâm chuồng (0, 0)
            var (covered, _, _) = EvaluateServiceCoverage(0f, 0f);
            bool cooled = IsWithinCoolingRadius(0f, 0f);
            report.GroupF_ServiceAndCoolingRadiusValid = covered && cooled;

            // Nhóm G: Điểm Bí nhân ngoài rào ≤ 30m và mọi điểm spawn quái nằm ngoài vùng đệm 25m từ tâm (0, 0)
            bool mysticOutsidePasture = !FarmMainLayout.IsInsidePasture(MysticMerchantSpawnPoint.x, MysticMerchantSpawnPoint.y);
            float distToSouthGate = Math.Abs(MysticMerchantSpawnPoint.y - FarmMainLayout.PastureBounds.YMin);
            bool bufferOk = true;
            foreach (var sp in MonsterSpawnPoints)
            {
                float d = (float)Math.Sqrt(sp.x * sp.x + sp.y * sp.y);
                if (d < NoSpawnBufferRadiusMeters) bufferOk = false;
            }
            report.GroupG_MysticMerchantAndNoSpawnBufferValid =
                mysticOutsidePasture && distToSouthGate <= MysticSpawnMaxDistanceOutsideFenceMeters && bufferOk;

            // Nhóm H: 5 Sorting Layers và 3 khung Camera chuẩn GDD 3.6
            report.GroupH_SortingLayersAndCameraFramesValid =
                FarmSorting.LayerOrder.Length == 5 &&
                Math.Abs(CameraFrames.OrthoSizeForHeight(CameraFrames.DefaultHeightMeters) - 18f) < 0.001f;

            return report;
        }
    }
}
