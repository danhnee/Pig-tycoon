using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using PigTycoon.Core;

namespace PigTycoon.PlayableDemo3D
{
    public enum FarmInteractionKind
    {
        Feed,
        Water,
        Care,
        Market,
        Construction,
        Defense,
        Save,
        Load,
        Sleep,
        Capacity,
        Dialogue,
        NightMarket,
        MainHouseHub,
        WarehouseTerminal,
        ResearchLab,
        MedicalTent,
        WorkshopTerminal,
        ObservatoryIntel,
        HarborTownHub
    }

    [Serializable]
    public sealed class FarmInteractionPoint
    {
        public string Id;
        public FarmInteractionKind Kind;
        public float X, Y;
        public float ReachMeters = 2.5f;
        public string CoreInstanceId = "";
        public FarmInteractionPoint(string id, FarmInteractionKind kind, float x, float y)
        { Id = id; Kind = kind; X = x; Y = y; }
    }

    public readonly struct FarmActionResult
    {
        public bool Success { get; }
        public string Message { get; }
        public FarmActionResult(bool success, string message) { Success = success; Message = message; }
    }

    [Serializable]
    public sealed class FarmActionEvidence
    {
        public string Action = "", TargetId = "";
        public int Day, Hour, Minute;
    }

    [Serializable]
    public sealed class PlayableFarmState
    {
        public float PlayerX, PlayerY;
        public int NextWhistleGameMinute;
        public float WhistleSecondsRemaining;
        public List<string> FollowingPigIds = new List<string>();
        public List<FarmActionEvidence> Actions = new List<FarmActionEvidence>();
        public List<string> TemporaryFenceIds = new List<string>();
        public Dictionary<string, int> WaterChangedDays = new Dictionary<string, int>();
        public List<string> WaterStationIds = new List<string>();
        public int LastWaterRollupDay;
        public List<FarmInteractionPoint> FeedStations = new List<FarmInteractionPoint>();
        public List<FarmFenceState> Fences = new List<FarmFenceState>();
        public bool PendingRaid;
        public List<NightMarketListing> NightListings = new List<NightMarketListing>();
        public string EquippedWeaponId = "";
        public string ScenarioProfileId = "";
        public EquipmentItem? EquippedWeapon;
        public Dictionary<string, int> TowerWearTenths = new Dictionary<string, int>();
        public Dictionary<string, int> TowerShotsSinceWind = new Dictionary<string, int>();
        public int RecoverableArrowTenths;
        public int WarehouseSlotCount = 12;
        public int ObservatoryIntelLevel = 1;
    }

    [Serializable]
    public sealed class PlayableFarmSave
    {
        public int Version;
        public GameSaveData Core = null!;
        public PlayableFarmState Presentation = null!;
    }

    /// <summary>
    /// Core-backed action boundary for the isolated 3D slice. Core XY maps to Unity XZ.
    /// Scene geometry/terrain enters through CanTraverse; this class never creates decorative pigs.
    /// Interaction reach is scene configuration, never an economic or combat balance value.
    /// </summary>
    public sealed class PlayableFarmSession
    {
        public const int SaveVersion = 3;
        public const int WhistleMaximumPigs = 12;
        public const float WhistleRadiusMeters = 20f;
        public const float WhistleDurationSeconds = 60f;
        public const int WhistleCooldownGameMinutes = 120;
        public GameEngine Engine { get; }
        public string RequiredTowerInstanceId { get; set; } = "";
        public PlayableFarmState State { get; private set; } = new PlayableFarmState();
        public float PlayerX => State.PlayerX;
        public float PlayerY => State.PlayerY;
        public IReadOnlyList<string> FollowingPigIds => State.FollowingPigIds;
        public string SelectedPigId { get; private set; } = "";
        public string SecondaryBreedingPigId { get; private set; } = "";
        public string LastMessage { get; private set; } = "Đến gần một con heo hoặc công trình để tương tác.";
        public HashSet<string> ExternallyDrivenPigIds { get; } = new HashSet<string>();
        public event Action? Changed;
        private static readonly FlowDestinationGroup[] IdleDestinations =
            { FlowDestinationGroup.Feeder, FlowDestinationGroup.WaterTrough, FlowDestinationGroup.Shelter };
        private readonly Dictionary<string, PigSteeringAgent> agentsById = new Dictionary<string, PigSteeringAgent>();
        private readonly List<PigSteeringAgent> steeringAgents = new List<PigSteeringAgent>();
        private readonly Dictionary<FlowDestinationGroup, List<(float, float)>> destinations = new Dictionary<FlowDestinationGroup, List<(float, float)>>();
        private (int gx, int gy) lastWhistleCell = (int.MinValue, int.MinValue);

        public PlayableFarmSession(GameEngine engine)
        {
            Engine = engine ?? throw new ArgumentNullException(nameof(engine));
            State.NextWhistleGameMinute = engine.Clock.TotalElapsedGameMinutes;
            Engine.FlowField.SetTargets(FlowDestinationGroup.HerdRefuge, new[] { engine.GridZones.HerdRefugeCenter });
            EnsureDefaultWeaponEquipped();
        }

        private void EnsureDefaultWeaponEquipped()
        {
            if (!Engine.Character.Equipment.TryGetValue(EquipSlot.Weapon, out var wpn) || wpn == null)
            {
                foreach (var item in EquipmentAndTacticalCatalog.AllEquipmentItems)
                {
                    if (item.Id == "wpn_dao_rua")
                    {
                        Engine.Character.Equipment[EquipSlot.Weapon] = item;
                        State.EquippedWeaponId = item.Id;
                        State.EquippedWeapon = item;
                        Engine.Character.RecalculateBonusStats();
                        break;
                    }
                }
            }
        }

        public Pig? GetPig(string id) => Engine.Pigs.Find(p => p != null && p.Id == id && p.IsAlive);
        public static string DisplayPigName(Pig pig) => pig.IsIdentified ? pig.Name : "Heo [" + pig.Id + "]";
        public void RegisterWaterStation(string id)
        { if (!State.WaterStationIds.Contains(id)) State.WaterStationIds.Add(id); }
        public void RegisterFeedStation(FarmInteractionPoint point)
        {
            if (!State.FeedStations.Exists(p => p.Id == point.Id)) State.FeedStations.Add(point);
            RegisterDestination(FlowDestinationGroup.Feeder, point);
        }
        public void SetPigExternallyDriven(string id, bool driven)
        { if (driven) ExternallyDrivenPigIds.Add(id); else ExternallyDrivenPigIds.Remove(id); }
        public void RegisterFence(string id, float x, float y, bool initiallyBuilt, float radius = 0.5f)
        {
            if (!State.Fences.Exists(f => f.Id == id)) State.Fences.Add(new FarmFenceState
                { Id = id, X = x, Y = y, Hp = initiallyBuilt || State.TemporaryFenceIds.Contains(id) ? 300f : 0f, Radius = radius });
        }
        public void SetFenceBoxFootprint(string id, float centerX, float centerY, float halfX, float halfY, float yawRadians)
        {
            if (!IsFinite(centerX) || !IsFinite(centerY) || !IsFinite(halfX) || !IsFinite(halfY) || !IsFinite(yawRadians) || halfX <= 0f || halfY <= 0f)
                throw new ArgumentException("Fence footprint must match a finite, positive authored box.");
            var fence = State.Fences.Find(f => f.Id == id) ?? throw new ArgumentException("Register the actual fence ID before its footprint.");
            fence.X = centerX; fence.Y = centerY; fence.BoxHalfX = halfX; fence.BoxHalfY = halfY;
            fence.BoxYawRadians = yawRadians; fence.HasBoxFootprint = true;
        }
        public void RegisterDestination(FlowDestinationGroup group, FarmInteractionPoint point)
        {
            if (!destinations.TryGetValue(group, out var list)) { list = new List<(float, float)>(); destinations[group] = list; }
            if (!list.Contains((point.X, point.Y))) list.Add((point.X, point.Y));
            Engine.FlowField.SetTargets(group, list);
        }
        public void SetNavigationObstacle(MapRect rect, bool blocked)
        {
            Engine.FlowField.SetWorldRectBlocked(rect.XMin, rect.YMin, rect.Width, rect.Height, blocked);
            foreach (var pair in destinations) Engine.FlowField.SetTargets(pair.Key, pair.Value);
            Engine.FlowField.SetTargets(FlowDestinationGroup.HerdRefuge, new[] { Engine.GridZones.HerdRefugeCenter });
            lastWhistleCell = (int.MinValue, int.MinValue);
        }
        public void SetPlayerPosition(float x, float y)
        {
            if (!IsFinite(x) || !IsFinite(y)) throw new ArgumentOutOfRangeException(nameof(x));
            State.PlayerX = x; State.PlayerY = y;
        }

        public bool CanInteract(FarmInteractionPoint point) => point != null &&
            IsFinite(point.X) && IsFinite(point.Y) && point.ReachMeters > 0f &&
            DistanceSquared(PlayerX, PlayerY, point.X, point.Y) <= point.ReachMeters * point.ReachMeters;

        public FarmActionResult InspectPig(string id)
        {
            var pig = GetPig(id);
            if (pig == null) return Result(false, "Không tìm thấy heo còn sống.");
            if (DistanceSquared(PlayerX, PlayerY, pig.PositionX, pig.PositionY) > 2.5f * 2.5f)
                return Result(false, "Đến gần heo để kiểm tra.");
            if (!string.IsNullOrEmpty(SelectedPigId) && SelectedPigId != id)
                SecondaryBreedingPigId = SelectedPigId;
            SelectedPigId = id;
            RecordOnce("inspect", id);
            string gene = pig.IsIdentified ? " · " + pig.GeneLine + " (" + pig.Rarity + ")" : " · Chưa giám định dòng gen";
            return Result(true, $"{DisplayPigName(pig)} [{pig.Id}] · {pig.Gender} · {pig.Stage} · {pig.WeightKg:0.0} kg · " +
                $"Sức khỏe {pig.Health}/{pig.MaxHealth} · Đói {pig.Hunger}/100 · Tinh thần {pig.Mood}/100{gene}");
        }

        public void SelectPigAnywhereForTerminal(string id)
        {
            var pig = GetPig(id);
            if (pig == null) return;
            if (!string.IsNullOrEmpty(SelectedPigId) && SelectedPigId != id)
                SecondaryBreedingPigId = SelectedPigId;
            SelectedPigId = id;
            Changed?.Invoke();
        }

        public FarmActionResult StartGuiding(string id)
        {
            var inspected = InspectPig(id);
            if (!inspected.Success) return inspected;
            var pig = GetPig(id)!;
            if (pig.IsIsolated || pig.Mood < 20) return Result(false, "Heo cách ly hoặc hoảng loạn không thể lùa theo đàn.");
            State.FollowingPigIds.Clear(); State.FollowingPigIds.Add(id);
            State.WhistleSecondsRemaining = 0f;
            return Result(true, "Đang lùa " + DisplayPigName(pig) + ". Nhấn G lần nữa để dừng.");
        }

        public void StopGuiding() { State.FollowingPigIds.Clear(); State.WhistleSecondsRemaining = 0f; }

        public FarmActionResult StartWhistle()
        {
            if (Engine.Character.Identity != PlayerId.Khoa) return Result(false, "Tiếng Huýt Chăn Đàn là đặc tính của Khoa.");
            if (AbsoluteGameMinute < State.NextWhistleGameMinute) return Result(false, "Tiếng huýt đang hồi chiêu (2 giờ game).");
            var nearby = Engine.Pigs.FindAll(p => p.IsAlive && !p.IsIsolated &&
                DistanceSquared(PlayerX, PlayerY, p.PositionX, p.PositionY) <= WhistleRadiusMeters * WhistleRadiusMeters);
            nearby.Sort((a, b) => DistanceSquared(PlayerX, PlayerY, a.PositionX, a.PositionY)
                .CompareTo(DistanceSquared(PlayerX, PlayerY, b.PositionX, b.PositionY)));
            if (nearby.Count == 0) return Result(false, "Không có heo phù hợp trong 20 m.");
            State.FollowingPigIds.Clear();
            for (int i = 0; i < Math.Min(WhistleMaximumPigs, nearby.Count); i++) State.FollowingPigIds.Add(nearby[i].Id);
            State.WhistleSecondsRemaining = WhistleDurationSeconds;
            State.NextWhistleGameMinute = AbsoluteGameMinute + WhistleCooldownGameMinutes;
            Record("whistle", Engine.Character.Id);
            return Result(true, $"{State.FollowingPigIds.Count} heo đi theo Khoa trong 60 giây.");
        }

        public FarmActionResult EquipWeaponBySlotKey(int slotKey)
        {
            string targetId = slotKey switch
            {
                1 => "wpn_dao_rua",
                2 => "wpn_bua_ta",
                3 => "wpn_giao_tre",
                _ => "wpn_dao_rua"
            };
            EquipmentItem? found = null;
            foreach (var item in EquipmentAndTacticalCatalog.AllEquipmentItems)
            {
                if (item.Id == targetId)
                {
                    found = item;
                    break;
                }
            }
            if (found == null)
            {
                found = new EquipmentItem
                {
                    Id = targetId,
                    Name = slotKey == 2 ? "Búa Tạ (Phá Giáp)" : (slotKey == 3 ? "Giáo Tre (Tầm Xa 2.8m)" : "Dao Rựa (Chém Nhanh)"),
                    Slot = EquipSlot.Weapon,
                    BonusStr = slotKey == 2 ? 6 : 3
                };
            }
            Engine.Character.Equipment[EquipSlot.Weapon] = found;
            Engine.Character.RecalculateBonusStats();
            State.EquippedWeaponId = found.Id;
            State.EquippedWeapon = found;
            Record("equip-weapon", found.Id);
            return Result(true, $"Đã trang bị vũ khí [{found.Name}] (Phím {slotKey}).");
        }

        public string EquippedWeaponDisplayName =>
            State.EquippedWeapon != null && !string.IsNullOrEmpty(State.EquippedWeapon.Name)
                ? State.EquippedWeapon.Name
                : (State.EquippedWeaponId == "wpn_bua_ta" ? "Búa Tạ (Phá Giáp)"
                    : (State.EquippedWeaponId == "wpn_giao_tre" ? "Giáo Tre (Tầm Xa 2.8m)" : "Dao Rựa (Chém Nhanh)"));

        public FarmActionResult UpgradeMainHouseHall()
        {
            int nextLevel = Engine.Farm.FarmLevel + 1;
            int cost = Engine.Farm.FarmLevel * 350;
            if (!Engine.Farm.SpendGold(cost))
                return Result(false, $"Cần {cost}G để nâng cấp Nhà Chính lên Cấp {nextLevel}.");
            Engine.Farm.FarmLevel = nextLevel;
            Engine.Defense.HallLevel = Math.Min(5, Engine.Defense.HallLevel + 1);
            Engine.Farm.Infrastructure.LandPlots += 2;
            Engine.Farm.Infrastructure.Shelters += 2;
            Engine.Farm.Infrastructure.WaterTroughs += 1;
            Engine.Farm.Infrastructure.Feeders += 1;
            Engine.DeepMechanics.EvaluateMidGameGateAndLocks(Engine.Farm, Engine.Herd, Engine.MajorWavesSurvived);
            Record("upgrade-hall", "level-" + nextLevel);
            var cap = Capacity;
            return Result(true, $"Đã nâng cấp Nhà Chính lên Cấp {nextLevel} (-{cost}G)! Sức chứa hiệu dụng mới: {cap.EffectiveCapacity} heo.");
        }

        public FarmActionResult UpgradeWarehouseCapacity()
        {
            if (State.WarehouseSlotCount >= 36)
                return Result(false, "Nhà Kho đã mở tối đa 36 ô chứa (GDD 21.1).");
            int cost = 120 + (State.WarehouseSlotCount - 12) * 15;
            if (!Engine.Farm.SpendGold(cost))
                return Result(false, $"Cần {cost}G để mở rộng thêm 4 ô Nhà Kho.");
            State.WarehouseSlotCount = Math.Min(36, State.WarehouseSlotCount + 4);
            Record("upgrade-warehouse", State.WarehouseSlotCount.ToString());
            return Result(true, $"Đã nâng cấp Nhà Kho lên {State.WarehouseSlotCount}/36 ô chứa (-{cost}G)!");
        }

        public FarmActionResult UpgradeInfrastructureFacility(string facilityType)
        {
            int cost = 140;
            if (!Engine.Farm.SpendGold(cost))
                return Result(false, $"Cần {cost}G để nâng cấp hạ tầng [{facilityType}].");
            string label;
            switch (facilityType)
            {
                case "Shelter":
                case "research_lab":
                    Engine.Farm.Infrastructure.Shelters += 2;
                    label = $"Mái Trú & Phòng Nghiên Cứu (+12 chỗ, tổng {Engine.Farm.Infrastructure.Shelters * 6})";
                    break;
                case "Water":
                case "medical_tent":
                    Engine.Farm.Infrastructure.WaterTroughs += 2;
                    Engine.Farm.MedicalHerbCount += 2;
                    label = $"Bồn Nước & Lều Quân Y (+24 chỗ, tổng {Engine.Farm.Infrastructure.WaterTroughs * 12})";
                    break;
                case "Feeder":
                case "workshop":
                    Engine.Farm.Infrastructure.Feeders += 2;
                    label = $"Máng Ăn & Xưởng Cơ Khí (+20 chỗ, tổng {Engine.Farm.Infrastructure.Feeders * 10})";
                    break;
                case "Drainage":
                case "observatory":
                    Engine.Farm.Infrastructure.DrainageSystemLevel += 1;
                    Engine.Farm.HabitatIndex = Math.Min(100, Engine.Farm.HabitatIndex + 8);
                    State.ObservatoryIntelLevel = Math.Min(3, State.ObservatoryIntelLevel + 1);
                    label = $"Đài Thiên Văn & Hệ Thống Môi Trường Cấp {Engine.Farm.Infrastructure.DrainageSystemLevel} (SC={Engine.Farm.HabitatIndex})";
                    break;
                default:
                    Engine.Farm.Infrastructure.LandPlots += 2;
                    label = $"Ô Đất Chăn Thả & Thương Hội (+16 chỗ, tổng {Engine.Farm.Infrastructure.LandPlots * 8})";
                    break;
            }
            Record("upgrade-infra", facilityType);
            return Result(true, $"Đã nâng cấp {label} (-{cost}G)! Sức chứa mới: {Capacity.EffectiveCapacity}.");
        }

        public FarmActionResult AppraisePig4Channel(string pigId, AppraisalChannel channel)
        {
            if (string.IsNullOrEmpty(pigId))
            {
                var first = Engine.Pigs.Find(p => p.IsAlive);
                if (first != null) pigId = first.Id;
            }
            var res = Engine.AppraisePigGeneAtChannel(pigId, channel, 0.05);
            if (res.success) Record("appraise-gene", pigId + ":" + channel);
            return Result(res.success, res.message);
        }

        public FarmActionResult AcquireCertifiedBreedingPartner()
        {
            const int cost = 220;
            if (!Engine.Farm.SpendGold(cost))
                return Result(false, $"Cần {cost}G để nhập 1 Heo Giống Trưởng Thành từ Trạm Nghiên Cứu.");
            bool hasMale = Engine.Pigs.Exists(p => p.IsAlive && p.Gender == "Duc" && p.AgeDays >= BreedingLedger.MinAgeDays);
            string gender = hasMale ? "Cai" : "Duc";
            string id = $"pig_breed_{Engine.Pigs.Count + 1:00}";
            GeneLineId gene = (Engine.Pigs.Count % 2 == 0) ? GeneLineId.LamKhe : GeneLineId.KimTho;
            var partner = Pig.CreateDefault(id, $"{(gender == "Duc" ? "Đực Giống" : "Nái Giống")} {gene} #{Engine.Pigs.Count + 1}",
                gene, GeneRarity.Hiem, gender, initialAge: 16, initialWeight: 108f);
            partner.IsIdentified = true;
            partner.PositionX = PlayerX + (gender == "Duc" ? -1.4f : 1.4f);
            partner.PositionY = PlayerY - 1.2f;
            Engine.Pigs.Add(partner);
            SecondaryBreedingPigId = SelectedPigId;
            SelectedPigId = partner.Id;
            Record("acquire-breeder", partner.Id);
            return Result(true, $"Đã nhận [{partner.Name}] ({partner.Gender}, {partner.GeneLine}, {partner.Rarity}) vào đàn (-{cost}G)!");
        }

        public FarmActionResult BreedPigsInLab()
        {
            Pig? mother = Engine.Pigs.Find(p => p.IsAlive && p.Gender == "Cai" && p.AgeDays >= BreedingLedger.MinAgeDays && p.GestationDaysRemaining == 0);
            Pig? father = Engine.Pigs.Find(p => p.IsAlive && p.Gender == "Duc" && p.AgeDays >= BreedingLedger.MinAgeDays);
            if (mother == null || father == null)
                return Result(false, "Cần ít nhất 1 heo Cái và 1 heo Đực trưởng thành (≥10 ngày tuổi). Hãy bấm 'Nhập Heo Giống Trưởng Thành (220G)' nếu thiếu cặp!");
            var res = Engine.SchedulePigBreeding(mother.Id, father.Id, deterministicConceptionRoll: 0.05);
            if (res.scheduled) Record("breed-scheduled", mother.Id + "x" + father.Id);
            return Result(res.scheduled, res.message);
        }

        public FarmActionResult ExecuteMedicalTreatmentVerb(HealthTreatmentVerb verb)
        {
            string targetId = !string.IsNullOrEmpty(SelectedPigId) ? SelectedPigId : (Engine.Pigs.Find(p => p.IsAlive)?.Id ?? "");
            var res = Engine.ExecuteHealthTreatment(verb, targetId, FarmDrug.HaNhiet, DiseaseId.NamPhatQuang);
            if (res.success) Record("medical-" + verb, targetId);
            return Result(res.success, res.message);
        }

        public FarmActionResult EnhanceWeaponAtWorkshop()
        {
            const int cost = 80;
            if (!Engine.Farm.SpendGold(cost))
                return Result(false, $"Cần {cost}G để cường hóa vũ khí tại Xưởng Cơ Khí.");
            Engine.DeepMechanics.intEquippedPaperEnhancementLevel = Math.Min(5, Engine.DeepMechanics.intEquippedPaperEnhancementLevel + 1);
            if (State.EquippedWeapon != null)
                State.EquippedWeapon.BonusStr += 2;
            Engine.Character.RecalculateBonusStats();
            Record("enhance-weapon", Engine.DeepMechanics.intEquippedPaperEnhancementLevel.ToString());
            return Result(true, $"Đã cường hóa [{EquippedWeaponDisplayName}] lên Cấp +{Engine.DeepMechanics.intEquippedPaperEnhancementLevel} (-{cost}G, +2 Sức Mạnh)!");
        }

        public FarmActionResult RepairAndFortifyAllDefenses()
        {
            const int cost = 90;
            if (!Engine.Farm.SpendGold(cost))
                return Result(false, $"Cần {cost}G để đại tu toàn bộ công trình phòng thủ & nạp đạn dự phòng.");
            foreach (var b in Engine.Defense.Buildings)
            {
                b.IsBroken = false;
                b.Durability = 100;
                b.CurrentHp = b.MaxHp;
                b.AmmoCurrent = b.AmmoCapacity;
            }
            foreach (var f in State.Fences)
            {
                f.Hp = 300f;
            }
            Engine.Inventory.AddItem("TenThep", 15);
            Record("workshop-fortify", "all");
            return Result(true, "Đã sửa chữa 100% độ bền Nỏ Xuyên Vân + Rào chắn, nạp đầy tên và thêm +15 Tên Thép vào Kho (-90G)!");
        }

        public FarmActionResult ScanObservatoryIntel()
        {
            State.ObservatoryIntelLevel = Math.Min(3, State.ObservatoryIntelLevel + 1);
            Engine.DeepMechanics.CurrentIntelTier = State.ObservatoryIntelLevel;
            int threat = EnemyData.CalculateWaveThreatBudget(Engine.Clock.CycleNumber, Engine.Pigs.FindAll(p => p.IsAlive).Count);
            Record("observatory-scan", "tier-" + State.ObservatoryIntelLevel);
            return Result(true,
                $"[ĐÀI THIÊN VĂN · Mức Tình Báo {State.ObservatoryIntelLevel}/3] Dự báo Đợt Quái Chu Kỳ {Engine.Clock.CycleNumber}: " +
                $"Ngân sách đe dọa {threat} điểm · Hướng đột kích chính: Nam & Đông-Bắc · Thành phần: Kẻ Bắt Heo (150HP) + Kẻ Cào Phá (120HP).");
        }

        public FarmActionResult ExecuteHarborTownTrade(int tradeKind)
        {
            if (tradeKind <= 1)
            {
                const int cost = 110;
                if (!Engine.Farm.SpendGold(cost))
                    return Result(false, $"Cần {cost}G để nhập lô hàng Cảng Thị Trấn.");
                Engine.Farm.FeedStockKg += 60f;
                Engine.Inventory.AddItem("TenThep", 20);
                Engine.Farm.MedicalHerbCount += 3;
                Record("harbor-supply", "bundle");
                return Result(true, "Đã nhập lô hàng Cảng Châu Âu: +60 kg Cám, +20 Tên Thép, +3 Thảo Dược (-110G)!");
            }
            if (tradeKind == 2)
            {
                if (Engine.Farm.FeedStockKg < 20f)
                    return Result(false, "Cần tối thiểu 20 kg cám dự trữ để ký hợp đồng cung ứng hậu cần Cảng.");
                Engine.Farm.FeedStockKg -= 20f;
                Engine.Ledger.Earn(240, LedgerReason.SellPig, "harbor-contract");
                Engine.Farm.AddFarmExp(300);
                Record("harbor-contract", "export");
                return Result(true, "Đã hoàn tất Hợp Đồng Thương Thuyền Thị Trấn: +240G và +300 EXP Trại!");
            }
            const int deedCost = 160;
            if (!Engine.Farm.SpendGold(deedCost))
                return Result(false, $"Cần {deedCost}G để nhận Khế Ước Mở Rộng Đất Cảng.");
            Engine.Farm.Infrastructure.LandPlots += 2;
            Record("harbor-land-deed", Engine.Farm.Infrastructure.LandPlots.ToString());
            return Result(true, $"Đã ký Khế Ước Mở Rộng Đất tại Thị Trấn Cảng (+16 sức chứa đất, tổng {Capacity.CapacityByLand}) (-{deedCost}G)!");
        }

        public FarmActionResult ShiftTimeToHour(int targetHour)
        {
            if (Engine.Clock.IsDuringCombatWave)
                return Result(false, "Không tua đồng hồ trong đợt giao tranh.");
            targetHour = Math.Clamp(targetHour, 0, 23);
            int curH = Engine.Clock.CurrentHour;
            int curM = Engine.Clock.CurrentMinute;
            int deltaHours = targetHour >= curH ? (targetHour - curH) : (24 - curH + targetHour);
            int minutes = Math.Max(15, deltaHours * 60 - curM);
            AdvanceSimulationMinutes(minutes);
            Record("shift-time", targetHour.ToString("00"));
            return Result(true, $"Đồng hồ trại đã chuyển sang Ngày {Engine.Clock.CurrentDay} · {Engine.Clock.CurrentHour:00}:{Engine.Clock.CurrentMinute:00}.");
        }

        /// <summary>Bounded real-time motion; callback must sweep the entire segment, not just the endpoint.</summary>
        public void AdvanceMotion(float deltaSeconds, Func<float, float, float, float, bool> canTraverse)
        {
            if (!IsFinite(deltaSeconds) || deltaSeconds <= 0f) return;
            float remaining = Math.Min(deltaSeconds, WhistleDurationSeconds + 1f);
            while (remaining > 0f)
            {
                float step = Math.Min(0.1f, remaining); remaining -= step;
                RefreshSteeringAgents();
                var playerCell = Engine.FlowField.WorldToGrid(PlayerX, PlayerY);
                if (State.FollowingPigIds.Count > 0 && playerCell != lastWhistleCell)
                {
                    Engine.FlowField.SetTargets(FlowDestinationGroup.WhistlePoint, new[] { (PlayerX, PlayerY) });
                    lastWhistleCell = playerCell;
                }
                bool whistle = State.WhistleSecondsRemaining > 0f;
                if (whistle) State.WhistleSecondsRemaining = Math.Max(0f, State.WhistleSecondsRemaining - step);
                foreach (var pig in Engine.Pigs)
                {
                    if (!pig.IsAlive || pig.IsIsolated || ExternallyDrivenPigIds.Contains(pig.Id)) continue;
                    bool follows = State.FollowingPigIds.Contains(pig.Id);
                    if (follows && !whistle && State.WhistleSecondsRemaining == 0f &&
                        State.FollowingPigIds.Count > 1) continue;
                    if (follows && !whistle && !SpendGuidingStamina(pig, step)) { StopGuiding(); follows = false; }
                    var group = follows ? FlowDestinationGroup.WhistlePoint : PickDestination(pig);
                    float tx = follows ? PlayerX : Engine.GridZones.HerdRefugeCenter.x;
                    float ty = follows ? PlayerY : Engine.GridZones.HerdRefugeCenter.y;
                    if (!follows && destinations.TryGetValue(group, out var targets) && targets.Count > 0)
                    { tx = targets[0].Item1; ty = targets[0].Item2; }
                    float stopping = follows ? 1.8f : 0.15f;
                    MovePig(pig, group, tx, ty, step, stopping, canTraverse);
                    if (follows && State.FeedStations.Exists(p => DistanceSquared(pig.PositionX, pig.PositionY, p.X, p.Y) <= p.ReachMeters * p.ReachMeters))
                        RecordOnce("guided-to-trough", pig.Id);
                }
                if (whistle && State.WhistleSecondsRemaining <= 0f) State.FollowingPigIds.Clear();
            }
        }

        private FlowDestinationGroup PickDestination(Pig pig)
        {
            if (pig.Hunger > 0 && destinations.ContainsKey(FlowDestinationGroup.Feeder)) return FlowDestinationGroup.Feeder;
            if (Engine.Clock.CurrentHour >= 21 || Engine.Clock.CurrentHour < 6) return destinations.ContainsKey(FlowDestinationGroup.Shelter) ? FlowDestinationGroup.Shelter : FlowDestinationGroup.HerdRefuge;
            int index = (StableHash(pig.Id) + Engine.Clock.CurrentMinute / 10) % IdleDestinations.Length;
            return destinations.ContainsKey(IdleDestinations[index]) ? IdleDestinations[index] : FlowDestinationGroup.HerdRefuge;
        }

        private void RefreshSteeringAgents()
        {
            steeringAgents.Clear();
            foreach (var pig in Engine.Pigs)
            {
                if (!agentsById.TryGetValue(pig.Id, out var agent)) { agent = new PigSteeringAgent { PigId = pig.Id }; agentsById[pig.Id] = agent; }
                agent.X = pig.PositionX; agent.Y = pig.PositionY; agent.IsAlive = pig.IsAlive; agent.IsIsolated = pig.IsIsolated;
                steeringAgents.Add(agent);
            }
        }

        private bool SpendGuidingStamina(Pig pig, float seconds)
        {
            float cost = 10f / 60f * seconds;
            if (pig.Mood < 60) cost *= 1.2f;
            if (Engine.Character.BaseStats.Str < 25) cost *= 1.25f;
            return Engine.Character.Stamina.TryConsumeStamina(cost, 0.7f);
        }

        private void MovePig(Pig pig, FlowDestinationGroup group, float tx, float ty, float seconds, float stopping,
            Func<float, float, float, float, bool> canTraverse)
        {
            float dx = tx - pig.PositionX, dy = ty - pig.PositionY;
            float distance = (float)Math.Sqrt(dx * dx + dy * dy);
            if (distance <= stopping) return;
            float speed = pig.Mood < 20 ? MovementSpec.AdultPigPanicMetersPerSecond : MovementSpec.AdultPigWalkMetersPerSecond;
            float travel = Math.Min(speed * seconds, distance - stopping);
            var steering = Engine.FlowField.ComputeSteeringWithSeparation(group, agentsById[pig.Id], steeringAgents);
            foreach (var other in steeringAgents)
            {
                if (other.PigId == pig.Id || !other.IsAlive || DistanceSquared(pig.PositionX, pig.PositionY, other.X, other.Y) > 0.0001f) continue;
                float angle = StableHash(pig.Id) % 360 * (float)Math.PI / 180f;
                steering = new FlowVector2D(steering.X + (float)Math.Cos(angle), steering.Y + (float)Math.Sin(angle)).Normalized();
                break;
            }
            float nx = pig.PositionX + steering.X * travel, ny = pig.PositionY + steering.Y * travel;
            if (canTraverse == null) return;
            if (!canTraverse(pig.PositionX, pig.PositionY, nx, ny))
            {
                if (canTraverse(pig.PositionX, pig.PositionY, nx, pig.PositionY)) ny = pig.PositionY;
                else if (canTraverse(pig.PositionX, pig.PositionY, pig.PositionX, ny)) nx = pig.PositionX;
                else return;
            }
            pig.PositionX = nx; pig.PositionY = ny;
        }

        public FarmActionResult ChangeWater(FarmInteractionPoint point)
        {
            if (!CanInteract(point) || point.Kind != FarmInteractionKind.Water) return Result(false, "Đến gần bồn nước.");
            if (State.WaterChangedDays.TryGetValue(point.Id, out int day) && day == Engine.Clock.CurrentDay)
                return Result(false, "Bồn đã được thay nước hôm nay.");
            float cost = PigCareCommands.CareStaminaCost(Engine, 6f);
            if (!Engine.Character.Stamina.TryConsumeStamina(cost, 1f)) return Result(false, "Không đủ thể lực thay nước.");
            State.WaterChangedDays[point.Id] = Engine.Clock.CurrentDay;
            Record("water", point.Id);
            return Result(true, "Đã thay nước bồn. −" + cost.ToString("0.0") + " thể lực.");
        }

        public FarmActionResult FeedPig(FarmInteractionPoint feeder, string id)
        {
            if (!CanInteract(feeder) || feeder.Kind != FarmInteractionKind.Feed) return Result(false, "Đến gần máng ăn để rải cám.");
            var pig = GetPig(id);
            if (pig == null) return Result(false, "Chọn một con heo còn sống.");
            if (DistanceSquared(pig.PositionX, pig.PositionY, feeder.X, feeder.Y) > feeder.ReachMeters * feeder.ReachMeters)
                return Result(false, "Lùa heo đến máng trước khi cho ăn.");
            var fed = PigCareCommands.TryFeedPig(Engine, id, PlayerX, PlayerY, feeder.ReachMeters);
            if (fed.success) Record("feed-consumed", id);
            return Result(fed.success, fed.success ? $"Đã cho {DisplayPigName(pig)} ăn {fed.kilograms:0.00} kg cám." : fed.message);
        }

        public FarmActionResult BrushPig(string id)
        {
            var result = InspectPig(id);
            if (!result.Success) return result;
            Record("brush", id);
            return Result(true, "Đã chải và kiểm tra " + DisplayPigName(GetPig(id)!) + ".");
        }

        public FarmActionResult SellPig(FarmInteractionPoint market, string id, bool quarantine)
        {
            if (!CanInteract(market) || market.Kind != FarmInteractionKind.Market) return Result(false, "Đến quầy chợ ngày để bán heo.");
            if (Engine.Clock.IsDuringCombatWave || Engine.Clock.CurrentHour < 6 || Engine.Clock.CurrentHour >= 18)
                return Result(false, "Chợ ngày mở từ 06:00 đến 18:00; không giao dịch trong đợt quái.");
            var pig = GetPig(id);
            if (pig == null) return Result(false, "Không tìm thấy heo còn sống.");
            if (DistanceSquared(pig.PositionX, pig.PositionY, market.X, market.Y) > market.ReachMeters * market.ReachMeters)
                return Result(false, "Dẫn heo đến quầy trước khi bán.");
            if (pig.ActiveDisease.HasValue) return Result(false, "Heo đang bệnh phải được điều trị trước khi bán.");
            var sale = Engine.SellPigAtDayMarket(id, quarantine);
            if (sale.success) { Record("sale", id); State.FollowingPigIds.Remove(id); SelectedPigId = ""; }
            return Result(sale.success, sale.message);
        }

        public FarmActionResult BuyFeed(FarmInteractionPoint market, int kilograms)
        {
            if (!CanInteract(market) || market.Kind != FarmInteractionKind.Market) return Result(false, "Đến quầy chợ ngày để mua cám.");
            if (Engine.Clock.IsDuringCombatWave || Engine.Clock.CurrentHour < 6 || Engine.Clock.CurrentHour >= 18)
                return Result(false, "Chợ ngày đang đóng cửa.");
            if (kilograms <= 0 || kilograms > 1000) return Result(false, "Số lượng cám không hợp lệ.");
            int gold = (int)Math.Round(kilograms * 0.8m * (decimal)Engine.Economy.DayMarket.FeedPriceMultiplier,
                MidpointRounding.AwayFromZero);
            if (gold < 1 || !Engine.Ledger.TrySpend(gold, LedgerReason.BuyFeed, market.Id)) return Result(false, "Không đủ vàng mua cám.");
            Engine.Farm.FeedStockKg += kilograms; Record("buy-feed", market.Id);
            return Result(true, $"Đã mua {kilograms} kg cám thường, {gold}G.");
        }

        public FarmActionResult PlaceTemporaryFence(FarmInteractionPoint point)
        {
            if (!CanInteract(point) || point.Kind != FarmInteractionKind.Construction) return Result(false, "Đến đoạn rào cần dựng tạm.");
            if (Engine.Clock.IsDuringCombatWave) return Result(false, "Không dựng rào trong đợt quái.");
            if (State.Fences.Exists(f => f.Id == point.Id && f.Hp > 0)) return Result(false, "Đoạn rào này đang nguyên vẹn.");
            if (State.TemporaryFenceIds.Contains(point.Id) && !State.Fences.Exists(f => f.Id == point.Id))
                return Result(false, "Đoạn rào tạm đã được dựng.");
            if (!Engine.Ledger.TrySpend(30, LedgerReason.BuildStructure, "temporary-fence:" + point.Id))
                return Result(false, "Cần 30G để dựng một đoạn rào tạm.");
            if (!State.TemporaryFenceIds.Contains(point.Id)) State.TemporaryFenceIds.Add(point.Id);
            Record("temporary-fence", point.Id);
            var fence = State.Fences.Find(f => f.Id == point.Id);
            if (fence == null) RegisterFence(point.Id, point.X, point.Y, true);
            else fence.Hp = 300f;
            return Result(true, "Đã dựng một đoạn rào tạm (30G).");
        }

        public FarmActionResult Preload(FarmInteractionPoint point)
        {
            if (!CanInteract(point) || point.Kind != FarmInteractionKind.Defense) return Result(false, "Đến gần công trình để nạp đạn.");
            if (Engine.Clock.IsDuringCombatWave) return Result(false, "Không nạp đạn trong đợt quái.");
            var b = Engine.Defense.Buildings.Find(x => x.InstanceId == point.CoreInstanceId && !x.IsBroken);
            if (b == null) return Result(false, "Chưa có công trình phù hợp.");
            int amount = b.AmmoCapacity - b.AmmoCurrent;
            if (amount <= 0) return Result(false, "Công trình đã nạp đầy.");
            if (b.BlueprintId != "NoXuyenVan") return Result(false, "Loại đạn này chưa có trong quầy demo.");
            int available = Engine.Inventory.CountId("TenThep");
            amount = Math.Min(amount, available);
            if (amount <= 0) return Result(false, "Cần mua Tên Thép tại chợ đêm.");
            float stamina = PigCareCommands.CareStaminaCost(Engine, 5f);
            if (!Engine.Character.Stamina.TryConsumeStamina(stamina, 1f)) return Result(false, "Không đủ thể lực nạp đạn.");
            Engine.Inventory.ConsumeItem("TenThep", amount);
            var loaded = Engine.Defense.PreloadAmmo(b.InstanceId, amount, false);
            if (loaded.success) Record("preload", b.InstanceId);
            return Result(loaded.success, loaded.message);
        }

        public FarmActionResult BuySteelArrows(FarmInteractionPoint market, int amount)
        {
            if (!CanInteract(market) || market.Kind != FarmInteractionKind.NightMarket || Engine.Clock.IsDuringCombatWave ||
                (Engine.Clock.CurrentHour >= 6 && Engine.Clock.CurrentHour < 18)) return Result(false, "Tên Thép chỉ có tại chợ đêm, từ 18:00 đến 06:00.");
            if (amount <= 0 || amount > 1000) return Result(false, "Số lượng Tên Thép không hợp lệ.");
            var listing = Engine.Economy.NightListings.Find(l => l.OfferedItem != null && l.OfferedItem.Id == "TenThep" &&
                l.AcceptsGold && l.Quantity >= amount && l.GoldPrice > 0);
            if (listing == null) return Result(false, "Chợ đêm chưa có ô Tên Thép nhận vàng phù hợp; cần trao đổi hoặc chế tạo theo công thức.");
            long price = (long)amount * listing.GoldPrice;
            if (!Engine.Ledger.TrySpend(price, LedgerReason.PreloadAmmo, listing.SlotId)) return Result(false, "Không đủ vàng cho ô hàng này.");
            listing.Quantity -= amount;
            Engine.Inventory.AddItem("TenThep", amount);
            return Result(true, $"Đã mua {amount} Tên Thép.");
        }

        public FarmActionResult CollectRecoveredArrows(FarmInteractionPoint point)
        {
            if (!CanInteract(point) || point.Kind != FarmInteractionKind.Defense || Engine.Clock.IsDuringCombatWave)
                return Result(false, "Thu gom tên sau đợt quái, tại công trình đang sở hữu.");
            if (!Engine.Defense.Buildings.Exists(b => b.InstanceId == point.CoreInstanceId)) return Result(false, "Không có công trình đang sở hữu.");
            int arrows = State.RecoverableArrowTenths / 10;
            if (arrows <= 0) return Result(false, "Chưa có tên thu hồi.");
            State.RecoverableArrowTenths -= arrows * 10;
            Engine.Inventory.AddItem("TenThep", arrows);
            return Result(true, $"Đã thu hồi {arrows} Tên Thép sau giao tranh.");
        }

        public void AdvanceSimulationMinutes(int minutes)
        {
            if (minutes < 0 || minutes > 24 * 60) throw new ArgumentOutOfRangeException(nameof(minutes));
            for (int i = 0; i < minutes; i++)
            {
                int processed = Engine.LastDailyProcessedFarmDay;
                Engine.Tick();
                if (Engine.Clock.IsMinorWaveTriggered) State.PendingRaid = true;
                int day = Engine.LastDailyProcessedFarmDay;
                if (day > processed && State.LastWaterRollupDay < day)
                {
                    bool dirty = State.WaterStationIds.Exists(id => !State.WaterChangedDays.TryGetValue(id, out int changed) || changed < day);
                    if (dirty) Engine.Farm.HabitatIndex = Math.Max(0, Engine.Farm.HabitatIndex - 2);
                    State.LastWaterRollupDay = day;
                }
                if (State.PendingRaid) break;
            }
        }

        public FarmActionResult Sleep(FarmInteractionPoint bed)
        {
            if (!CanInteract(bed) || (bed.Kind != FarmInteractionKind.Sleep && bed.Kind != FarmInteractionKind.MainHouseHub))
                return Result(false, "Đến giường trong nhà chính để nghỉ.");
            if (Engine.Clock.IsDuringCombatWave) return Result(false, "Không ngủ trong đợt quái.");
            if (Engine.Clock.SleepCooldownHoursRemaining > 0) return Result(false, "Chưa đến lúc có thể ngủ tiếp.");
            int hour = Engine.Clock.CurrentHour;
            int minutes = (hour >= 6 ? 30 - hour : 6 - hour) * 60 - Engine.Clock.CurrentMinute;
            AdvanceSimulationMinutes(minutes);
            if (State.PendingRaid) return Result(false, "Báo động! Đợt quấy phá đã đến; cần ra khỏi nhà bảo vệ trại.");
            Engine.Clock.SleepCooldownHoursRemaining = 6;
            Engine.Character.Stamina.PerformSleepReset();
            Record("sleep", bed.Id);
            return Result(true, "Đã nghỉ đến 06:00. Ngày trại và khẩu phần đã được cập nhật.");
        }

        public FarmCapacityReport Capacity => Engine.Farm.CalculateCapacityReport(
            Engine.Pigs.FindAll(p => p.IsAlive).Count, Engine.Pigs.FindAll(p => !p.IsAlive && !p.IsInProcessingLot).Count);
        public bool HasEvidence(string action) => State.Actions.Exists(a => a.Action == action);
        public bool OpeningCareObjectivesComplete => HasEvidence("feed-consumed") && HasEvidence("water") &&
            HasEvidence("temporary-fence") && HasEvidence("guided-to-trough");
        public int AbsoluteGameMinute => Engine.Clock.TotalElapsedGameMinutes;

        public string CaptureSave()
        {
            State.NightListings = new List<NightMarketListing>(Engine.Economy.NightListings);
            State.EquippedWeaponId = Engine.Character.Equipment.TryGetValue(EquipSlot.Weapon, out var weapon) && weapon != null ? weapon.Id : "";
            State.EquippedWeapon = weapon;
            var data = new PlayableFarmSave { Version = SaveVersion, Core = Engine.CreateSaveData(), Presentation = State };
            return JsonConvert.SerializeObject(data, JsonSettings);
        }

        public static bool TryReadSavedProtagonist(string json, out PlayerId protagonist)
        {
            protagonist = PlayerId.Khoa;
            try
            {
                var data = JsonConvert.DeserializeObject<PlayableFarmSave>(json, JsonSettings);
                if (data?.Core == null) return false;
                protagonist = data.Core.CharacterId == PlayerRoster.AnDataId || data.Core.CharacterName == "An" ? PlayerId.An : PlayerId.Khoa;
                return true;
            }
            catch (JsonException) { return false; }
        }

        public FarmActionResult RestoreSave(string json)
        {
            try
            {
                var data = JsonConvert.DeserializeObject<PlayableFarmSave>(json, JsonSettings);
                if (data == null || data.Version < 1 || data.Version > SaveVersion || data.Core == null || data.Presentation == null ||
                    data.Core.SaveVersion < 1 || data.Core.SaveVersion > SaveSystem.CurrentSaveVersion ||
                    !IsFinite(data.Presentation.PlayerX) || !IsFinite(data.Presentation.PlayerY))
                    return Result(false, "File lưu demo không tương thích.");
                if (data.Core.IsDuringCombatWave) return Result(false, "Chưa hỗ trợ khôi phục giữa đợt quái.");
                if (!string.IsNullOrEmpty(State.ScenarioProfileId) && State.ScenarioProfileId != data.Presentation.ScenarioProfileId)
                    return Result(false, "File lưu thuộc một kịch bản demo khác; tiến độ không được dùng chung.");
                if (!string.IsNullOrEmpty(RequiredTowerInstanceId))
                {
                    var matches = data.Core.Buildings?.FindAll(b => b != null && b.InstanceId == RequiredTowerInstanceId);
                    if (matches == null || matches.Count != 1 || matches[0].BlueprintId != "NoXuyenVan")
                        return Result(false, "File lưu không có đúng Nỏ đã khai báo cho fixture; không tạo hoặc thay thế công trình.");
                }
                if (Engine.PlayableMap != null && (data.Core.PlayableMap == null ||
                    Engine.PlayableMap.ConfigurationId != data.Core.PlayableMap.ConfigurationId))
                    return Result(false, "File lưu thuộc một bản đồ khác.");
                if (data.Version < 3 && Engine.InteractiveHerdOwned && data.Core.PlayableMap != null)
                {
                    if (string.IsNullOrEmpty(State.ScenarioProfileId) || State.ScenarioProfileId != data.Presentation.ScenarioProfileId ||
                        Engine.PlayableMap!.ConfigurationId != data.Core.PlayableMap.ConfigurationId)
                        return Result(false, "Thiếu danh tính kịch bản để chuyển quyền điều khiển đàn trong file cũ.");
                    data.Core.PlayableMap.InteractiveHerdOwnership = true;
                }
                if (Engine.PlayableMap != null && data.Core.PlayableMap != null &&
                    Engine.InteractiveHerdOwned != data.Core.PlayableMap.InteractiveHerdOwnership)
                    return Result(false, "File lưu dùng chế độ điều khiển đàn khác với kịch bản hiện tại.");
                if (data.Core.PlayableMap != null)
                {
                    data.Core.PlayableMap.Validate();
                    foreach (var pig in data.Core.Pigs)
                        if (pig != null && pig.IsAlive && !data.Core.PlayableMap.MapBounds.Contains(pig.PositionX, pig.PositionY))
                            return Result(false, "Tọa độ heo trong file lưu nằm ngoài bản đồ.");
                }
                int oldDay = data.Core.CurrentDay, oldHour = data.Core.CurrentHour;
                Engine.RestoreFromSave(data.Core); State = data.Presentation;
                State.Actions = State.Actions ?? new List<FarmActionEvidence>();
                State.FollowingPigIds = State.FollowingPigIds ?? new List<string>();
                State.TemporaryFenceIds = State.TemporaryFenceIds ?? new List<string>();
                State.WaterChangedDays = State.WaterChangedDays ?? new Dictionary<string, int>();
                State.WaterStationIds = State.WaterStationIds ?? new List<string>();
                State.FeedStations = State.FeedStations ?? new List<FarmInteractionPoint>();
                State.Fences = State.Fences ?? new List<FarmFenceState>();
                State.NightListings = State.NightListings ?? new List<NightMarketListing>();
                State.TowerWearTenths = State.TowerWearTenths ?? new Dictionary<string, int>();
                State.TowerShotsSinceWind = State.TowerShotsSinceWind ?? new Dictionary<string, int>();
                if (State.WarehouseSlotCount < 12) State.WarehouseSlotCount = 12;
                Engine.Economy.NightListings.Clear(); Engine.Economy.NightListings.AddRange(State.NightListings);
                EquipmentItem? savedWeapon = State.EquippedWeapon;
                foreach (var item in EquipmentAndTacticalCatalog.AllEquipmentItems)
                    if (savedWeapon == null && item.Slot == EquipSlot.Weapon && item.Id == State.EquippedWeaponId) savedWeapon = item;
                Engine.Character.Equipment[EquipSlot.Weapon] = savedWeapon;
                EnsureDefaultWeaponEquipped();
                Engine.Character.RecalculateBonusStats();
                Engine.Character.CurrentGuard = Math.Max(0f, data.Core.CurrentGuard);
                Engine.Character.Stamina.CurrentStamina = Math.Max(0f, data.Core.CurrentStamina);
                if (data.Version == 1)
                {
                    State.NextWhistleGameMinute = Math.Max(AbsoluteGameMinute, State.NextWhistleGameMinute - 1800);
                    foreach (var evidence in State.Actions) if (evidence.Hour < 6) evidence.Day -= 1;
                    var waterIds = new List<string>(State.WaterChangedDays.Keys);
                    foreach (string id in waterIds)
                        if (oldHour < 6 && State.WaterChangedDays[id] == oldDay) State.WaterChangedDays[id] = oldDay - 1;
                }
                State.FollowingPigIds.RemoveAll(id => GetPig(id) == null);
                SelectedPigId = ""; agentsById.Clear(); lastWhistleCell = (int.MinValue, int.MinValue);
                return Result(true, "Đã khôi phục ngày trại, đàn heo và tiến độ thao tác.");
            }
            catch (JsonException) { return Result(false, "File lưu demo bị lỗi định dạng."); }
            catch (ArgumentException) { return Result(false, "Cấu hình bản đồ trong file lưu không hợp lệ."); }
        }

        private static readonly JsonSerializerSettings JsonSettings = new JsonSerializerSettings
        { TypeNameHandling = TypeNameHandling.None, ObjectCreationHandling = ObjectCreationHandling.Replace,
            MissingMemberHandling = MissingMemberHandling.Ignore, Formatting = Formatting.Indented };
        private void RecordOnce(string action, string target)
        { if (!State.Actions.Exists(a => a.Action == action && a.TargetId == target)) Record(action, target); }
        private void Record(string action, string target)
        {
            State.Actions.Add(new FarmActionEvidence { Action = action, TargetId = target,
                Day = Engine.Clock.CurrentDay, Hour = Engine.Clock.CurrentHour, Minute = Engine.Clock.CurrentMinute });
        }
        private FarmActionResult Result(bool success, string message)
        { LastMessage = message; Changed?.Invoke(); return new FarmActionResult(success, message); }
        private static bool IsFinite(float v) => !float.IsNaN(v) && !float.IsInfinity(v);
        private static float DistanceSquared(float ax, float ay, float bx, float by)
        { float x = ax - bx, y = ay - by; return x * x + y * y; }
        private static int StableHash(string id)
        { int hash = 0; foreach (char c in id) hash = (hash * 31 + c) & 0x7fffffff; return hash; }
    }
}
