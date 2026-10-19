using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace PigTycoon.Core
{
    // =========================================================================
    // 1. CÔNG THỨC CỘNG DỒN CHỈ SỐ & XÁC SUẤT (GDD 1.4 & SỔ TAY MỤC 14.3 — NGƯỜI A)
    // =========================================================================

    /// <summary>
    /// Công thức cộng dồn chỉ số chuẩn GDD 1.4 &amp; Sổ tay dự án v3.0 mục 14.3:
    /// - Cuối = Gốc × (1 + Σ%cùng nhóm / 100) × Π(hệ số × khác nhóm), sau đó kẹp trong [min, max].
    /// - "điểm %" (đ%): cộng thẳng vào xác suất rồi kẹp trong [0, 100].
    /// </summary>
    public static class StatCalc
    {
        public static float Final(
            float baseValue,
            ReadOnlySpan<float> percents,
            ReadOnlySpan<float> multipliers,
            float min,
            float max)
        {
            float sum = 0f;
            for (int i = 0; i < percents.Length; i++)
            {
                sum += percents[i];
            }

            float v = baseValue * (1f + sum / 100f);
            for (int i = 0; i < multipliers.Length; i++)
            {
                v *= multipliers[i];
            }

            return Math.Clamp(v, min, max);
        }

        public static float AddPoints(float probability, float points)
        {
            return Math.Clamp(probability + points, 0f, 100f);
        }
    }

    // =========================================================================
    // 2. CONTRACTS & EVENT BUS LIÊN KẾT MODULE (SỔ TAY MỤC 10 — NGƯỜI A & D)
    // =========================================================================

    public enum LedgerReason
    {
        InitialCapital = 0,
        BuyFeed = 1,
        BuyPiglet = 2,
        SellPig = 3,
        QuarantineFee = 4,
        ScreeningFee = 5,
        AppraisalFee = 6,
        BuildStructure = 7,
        RepairStructure = 8,
        DismantleRefund = 9,
        PreloadAmmo = 10,
        HireNpc = 11,
        NpcDailyWage = 12,
        ContractReward = 13,
        AuctionBid = 14,
        NightMarketGoldSlot = 15,
        ResearchLab = 16,
        EnhanceWorkshop = 17,
        DailyMaintenance = 18,
        WaveReward = 19,
        OccultEventReward = 20,
        FaintPenalty = 21,
        DebugCheat = 22
    }

    public enum DeathCause
    {
        Starvation = 0,
        Disease = 1,
        OldAgeOrOrphan = 2,
        MonsterAttack = 3,
        Abducted = 4,
        ElderBirthStillborn = 5
    }

    public interface IEventBus
    {
        void Publish<T>(in T evt) where T : struct;
        IDisposable Subscribe<T>(Action<T> handler) where T : struct;
    }

    public interface IEconomyLedger
    {
        long Gold { get; }
        bool TrySpend(long amount, LedgerReason reason, string sourceId);
        void Earn(long amount, LedgerReason reason, string sourceId);
    }

    public interface IHerdQuery
    {
        int LiveCount { get; }
        int OldCount { get; }
        int CarcassCount { get; }
        float DensityRatio { get; }
        int EventPressure { get; }
        bool IsHerdState { get; }
    }

    public readonly struct PigDiedEvent
    {
        public readonly string PigId;
        public readonly GeneLineId GeneLine;
        public readonly DeathCause Cause;

        public PigDiedEvent(string pigId, GeneLineId geneLine, DeathCause cause)
        {
            PigId = pigId;
            GeneLine = geneLine;
            Cause = cause;
        }
    }

    public readonly struct GeneIdentifiedEvent
    {
        public readonly string PigId;
        public readonly GeneLineId GeneLine;
        public readonly GeneRarity Rarity;
        public readonly string Channel;

        public GeneIdentifiedEvent(string pigId, GeneLineId geneLine, GeneRarity rarity, string channel)
        {
            PigId = pigId;
            GeneLine = geneLine;
            Rarity = rarity;
            Channel = channel;
        }
    }

    public readonly struct PaperEnhancedEvent
    {
        public readonly SpecialtyPaperColor Color;
        public readonly int NewLevel;
        public readonly bool Success;

        public PaperEnhancedEvent(SpecialtyPaperColor color, int newLevel, bool success)
        {
            Color = color;
            NewLevel = newLevel;
            Success = success;
        }
    }

    public readonly struct RuneEngravedEvent
    {
        public readonly string EquipmentId;
        public readonly string RuneId;
        public readonly bool Success;

        public RuneEngravedEvent(string equipmentId, string runeId, bool success)
        {
            EquipmentId = equipmentId;
            RuneId = runeId;
            Success = success;
        }
    }

    public readonly struct SealEngravedEvent
    {
        public readonly PlayerId Player;
        public readonly string SealId;
        public readonly int MaxHpAfterPenalty;

        public SealEngravedEvent(PlayerId player, string sealId, int maxHpAfterPenalty)
        {
            Player = player;
            SealId = sealId;
            MaxHpAfterPenalty = maxHpAfterPenalty;
        }
    }

    public sealed class CoreEventBus : IEventBus
    {
        private readonly Dictionary<Type, List<Delegate>> _handlers = new Dictionary<Type, List<Delegate>>();

        public void Publish<T>(in T evt) where T : struct
        {
            if (!_handlers.TryGetValue(typeof(T), out var list)) return;
            var snapshot = list.ToArray();
            for (int i = 0; i < snapshot.Length; i++)
            {
                if (snapshot[i] is Action<T> action)
                {
                    action(evt);
                }
            }
        }

        public IDisposable Subscribe<T>(Action<T> handler) where T : struct
        {
            if (handler == null) throw new ArgumentNullException(nameof(handler));
            var type = typeof(T);
            if (!_handlers.TryGetValue(type, out var list))
            {
                list = new List<Delegate>();
                _handlers[type] = list;
            }
            list.Add(handler);
            return new Subscription(list, handler);
        }

        private sealed class Subscription : IDisposable
        {
            private List<Delegate>? _list;
            private Delegate? _handler;

            public Subscription(List<Delegate> list, Delegate handler)
            {
                _list = list;
                _handler = handler;
            }

            public void Dispose()
            {
                if (_list != null && _handler != null)
                {
                    _list.Remove(_handler);
                    _list = null;
                    _handler = null;
                }
            }
        }
    }

    // =========================================================================
    // 3. SỔ CÁI KINH TẾ CHUẨN GDD 20 & 22 (P1.10 — NGƯỜI D)
    // =========================================================================

    [Serializable]
    public sealed class LedgerTransactionRecord
    {
        public int Day { get; set; }
        public int Hour { get; set; }
        public bool IsExpense { get; set; }
        public long AmountGold { get; set; }
        public LedgerReason Reason { get; set; }
        public string SourceId { get; set; } = string.Empty;
        public long BalanceAfter { get; set; }
    }

    /// <summary>
    /// Sổ cái kinh tế trung tâm (P1.10 - Người D):
    /// - Mọi khoản thu/chi đều làm tròn tới 1G, ghi rõ LedgerReason và SourceId.
    /// - TrySpend trả về false nếu không đủ vàng, tuyệt đối không trừ âm ngân sách.
    /// - Đồng bộ hai chiều với Farm.Gold.
    /// </summary>
    [Serializable]
    public sealed class EconomyLedger : IEconomyLedger
    {
        private readonly Farm _farm;
        private readonly GameClock _clock;

        public List<LedgerTransactionRecord> Transactions { get; } = new List<LedgerTransactionRecord>();

        public EconomyLedger(Farm farm, GameClock clock)
        {
            _farm = farm ?? throw new ArgumentNullException(nameof(farm));
            _clock = clock ?? throw new ArgumentNullException(nameof(clock));
        }

        public long Gold => _farm.Gold;

        public bool TrySpend(long amount, LedgerReason reason, string sourceId)
        {
            if (amount <= 0) return true;
            if (_farm.Gold < amount) return false;

            _farm.Gold -= (int)amount;
            Transactions.Add(new LedgerTransactionRecord
            {
                Day = _clock.CurrentDay,
                Hour = _clock.CurrentHour,
                IsExpense = true,
                AmountGold = amount,
                Reason = reason,
                SourceId = sourceId ?? string.Empty,
                BalanceAfter = _farm.Gold
            });
            return true;
        }

        public void Earn(long amount, LedgerReason reason, string sourceId)
        {
            if (amount <= 0) return;
            _farm.Gold += (int)amount;
            Transactions.Add(new LedgerTransactionRecord
            {
                Day = _clock.CurrentDay,
                Hour = _clock.CurrentHour,
                IsExpense = false,
                AmountGold = amount,
                Reason = reason,
                SourceId = sourceId ?? string.Empty,
                BalanceAfter = _farm.Gold
            });
        }

        public long GetTotalSpentByReason(LedgerReason reason)
        {
            long sum = 0;
            for (int i = 0; i < Transactions.Count; i++)
            {
                if (Transactions[i].IsExpense && Transactions[i].Reason == reason)
                {
                    sum += Transactions[i].AmountGold;
                }
            }
            return sum;
        }
    }

    // =========================================================================
    // 4. HỆ THỐNG CẤP TRẠI 1–20, KHO HÀNG, BẤT TỈNH & TELEMETRY (NGƯỜI A)
    //    + MAP FARM_CORE (7 PHÒNG CHỨC NĂNG, ĐOẠN CHUYỂN MAP) & HUB_VILLAGE
    // =========================================================================

    public enum FarmCoreRoomId
    {
        NhaChinh = 1,          // ① Nhà chính: giường ngủ/lưu, chọn An/Khoa, Bảng Điều Khiển Bạch Vân (+3s trễ)
        Kho = 2,               // ② Kho: sức chứa, chống hỏng cám (Thủ Kho, Mèo Kho)
        PhongXayDung = 3,      // ③ Phòng Xây Dựng cấp 1–3: học bản vẽ, thi công, sửa chữa, chế đạn (Kỹ Sư 40/70/100%)
        PhongNghienCuu = 4,    // ④ Phòng Nghiên Cứu (Cấp trại 8–10): Bàn Giải Mã, Thuần Hóa Huyết Thống, Cám Hạ Hỏa, Hư Thể/Hư Mạch
        XuongCuongHoa = 5,     // ⑤ Xưởng Cường Hóa: Gia Cường Giấy, Rã Giấy, Dùng Giấy 6 khóa, Khắc Phù Văn, Khắc Mệnh Ấn
        LeuQuanY = 6,          // ⑥ Lều Quân Y (Cấp trại 3–4): hồi sức nhanh sau ngất, chữa thương lính đánh thuê
        KhuThongTin = 7        // ⑦ Khu thông tin: Bàn Tình Báo, Trạm Khí Tượng, chân Tháp Canh/Radar
    }

    public enum TransitionActorType
    {
        Player = 0,
        Monster = 1,
        Pig = 2
    }

    [Serializable]
    public sealed class ResearchQueueJob
    {
        public string JobId { get; set; } = string.Empty;
        public string PigId { get; set; } = string.Empty;
        public GeneLineId TrueGeneLine { get; set; }
        public GeneRarity Rarity { get; set; }
        public int GoldSpent { get; set; }
        public int ReagentsSpent { get; set; }
        public float StaminaSpent { get; set; }
        public int TotalDurationHours { get; set; }
        public int RemainingHours { get; set; }
        public float SuccessProbability { get; set; }
        public bool IsCompleted { get; set; }
        public bool Succeeded { get; set; }
    }

    [Serializable]
    public sealed class InventorySlotData
    {
        public int SlotIndex { get; set; }
        public string ItemId { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;
        public int Count { get; set; }
        public int MaxStack { get; set; } = 99;
    }

    /// <summary>
    /// Hệ thống Kho &amp; Inventory 36 ô (P2.13 - Người A):
    /// Hỗ trợ thêm vật phẩm xếp chồng (stacking), kéo-thả hoán đổi ô (drag-drop swap),
    /// và bảo vệ cám khỏi ẩm mốc / chuột gặm nhờ Thủ Kho &amp; Mèo Kho.
    /// </summary>
    [Serializable]
    public sealed class WarehouseInventorySystem
    {
        public const int TotalSlots = 36;
        public List<InventorySlotData> Slots { get; } = new List<InventorySlotData>(TotalSlots);
        public Dictionary<string, int> CraftingMaterials { get; } = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase)
        {
            { "thuoc_thu_gen", 10 },
            { "da_cuong_hoa", 8 },
            { "to_vang", 6 },
            { "bot_tu_tinh", 6 },
            { "huyet_trien", 3 },
            { "tinh_hoa_di_bien", 2 },
            { "muc_phu", 5 },
            { "go_rung", 20 },
            { "nam_rung", 10 }
        };

        public WarehouseInventorySystem()
        {
            for (int i = 0; i < TotalSlots; i++)
            {
                Slots.Add(new InventorySlotData { SlotIndex = i });
            }
        }

        public bool AddItem(string itemId, string displayName, int quantity, int maxStack = 99)
        {
            if (string.IsNullOrWhiteSpace(itemId) || quantity <= 0) return false;
            int remaining = quantity;

            // Xếp chồng vào các ô đã có cùng itemId
            for (int i = 0; i < Slots.Count && remaining > 0; i++)
            {
                var s = Slots[i];
                if (string.Equals(s.ItemId, itemId, StringComparison.OrdinalIgnoreCase) && s.Count < s.MaxStack)
                {
                    int add = Math.Min(remaining, s.MaxStack - s.Count);
                    s.Count += add;
                    remaining -= add;
                }
            }

            // Đặt vào ô trống
            for (int i = 0; i < Slots.Count && remaining > 0; i++)
            {
                var s = Slots[i];
                if (string.IsNullOrEmpty(s.ItemId) || s.Count <= 0)
                {
                    int add = Math.Min(remaining, maxStack);
                    s.ItemId = itemId;
                    s.DisplayName = displayName;
                    s.Count = add;
                    s.MaxStack = maxStack;
                    remaining -= add;
                }
            }

            if (CraftingMaterials.ContainsKey(itemId))
            {
                CraftingMaterials[itemId] += quantity - remaining;
            }
            return remaining == 0;
        }

        public bool SwapOrMergeSlots(int fromIndex, int toIndex)
        {
            if (fromIndex < 0 || fromIndex >= Slots.Count || toIndex < 0 || toIndex >= Slots.Count)
                return false;
            if (fromIndex == toIndex) return true;

            var a = Slots[fromIndex];
            var b = Slots[toIndex];

            if (!string.IsNullOrEmpty(a.ItemId) &&
                string.Equals(a.ItemId, b.ItemId, StringComparison.OrdinalIgnoreCase) &&
                b.Count < b.MaxStack)
            {
                int transfer = Math.Min(a.Count, b.MaxStack - b.Count);
                b.Count += transfer;
                a.Count -= transfer;
                if (a.Count <= 0)
                {
                    a.ItemId = string.Empty;
                    a.DisplayName = string.Empty;
                    a.Count = 0;
                }
                return true;
            }

            string tmpId = a.ItemId;
            string tmpName = a.DisplayName;
            int tmpCount = a.Count;
            int tmpMax = a.MaxStack;

            a.ItemId = b.ItemId;
            a.DisplayName = b.DisplayName;
            a.Count = b.Count;
            a.MaxStack = b.MaxStack;

            b.ItemId = tmpId;
            b.DisplayName = tmpName;
            b.Count = tmpCount;
            b.MaxStack = tmpMax;
            return true;
        }

        public int GetTotalCount(string itemId)
        {
            int sum = 0;
            for (int i = 0; i < Slots.Count; i++)
            {
                if (string.Equals(Slots[i].ItemId, itemId, StringComparison.OrdinalIgnoreCase))
                {
                    sum += Slots[i].Count;
                }
            }
            return sum;
        }

        public float CalculateDailyFeedSpoilageKg(float currentFeedKg, bool hasStorekeeperNpc, bool hasWarehouseCatPet)
        {
            float baseSpoilage = currentFeedKg * 0.03f;
            if (hasStorekeeperNpc) baseSpoilage *= 0.30f; // Thủ Kho giảm 70% hỏng cám
            if (hasWarehouseCatPet) baseSpoilage *= 0.20f; // Mèo Kho giảm 80% hao hụt do chuột
            return (float)Math.Round(baseSpoilage, 2);
        }
    }

    /// <summary>
    /// Hệ thống Khu Lõi Vận Hành `Farm_Core` (60×40m, 7 phòng chức năng), Đoạn Chuyển Map (20×8m),
    /// Làng NPC `Hub_Village` (40×30m), Cấp trại 1–20 (`P2.21`), Xử lý Bất tỉnh (`P1.21`),
    /// Phòng Nghiên Cứu (`P2.22`) và Xưởng Cường Hóa (`P4.11`) theo Sổ tay mục 6.2 &amp; 6.2.1.
    /// </summary>
    [Serializable]
    public sealed class FarmCoreAndVillageSystem
    {
        public const float FarmCoreWidthMeters = 60.0f;
        public const float FarmCoreHeightMeters = 40.0f;
        public const float TransitionCorridorLengthMeters = 20.0f;
        public const float TransitionCorridorWidthMeters = 8.0f;
        public const float BackGateWidthMeters = 6.2f;
        public const float MaxSceneTransitionSeconds = 1.5f;
        public const float RemoteBachVanDelaySeconds = 3.0f; // GDD 13.1: kích từ xa ở nhà chính trễ +3 giây

        public const float HubVillageWidthMeters = 40.0f;
        public const float HubVillageHeightMeters = 30.0f;

        public string CurrentActiveScene { get; private set; } = "Farm_Main";
        public string LastUsedSpawnId { get; private set; } = "spawn.farm.initial";
        public float LastTransitionDurationSeconds { get; private set; } = 0.45f;

        public bool DeepDecodingTraitUnlocked { get; set; } = false; // Đặc tính "Giải Mã Sâu" của Nhà Nghiên Cứu Gen
        public bool HasHighTierEngravingTableBlueprint { get; set; } = false;
        public int CyclesSinceRuneSystemUnlocked { get; set; } = 0;
        public int BossesDefeatedCount { get; set; } = 0;
        public int RedEventsCompletedCount { get; set; } = 0;

        public List<ResearchQueueJob> ResearchQueue { get; } = new List<ResearchQueueJob>();
        public WarehouseInventorySystem Warehouse { get; } = new WarehouseInventorySystem();

        public static readonly IReadOnlyDictionary<string, string> CanonicalSpawnLinks = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            { "Farm_Main->Farm_Core", "spawn.core.from_farm" },
            { "Farm_Core->Farm_Main", "spawn.farm.from_core" },
            { "Farm_Main->Market_Town", "spawn.market.from_farm" },
            { "Market_Town->Farm_Main", "spawn.farm.from_market" },
            { "Farm_Main->Hub_Village", "spawn.hub.from_farm" },
            { "Hub_Village->Farm_Main", "spawn.farm.from_hub" },
            { "Farm_Main->Forest_Primeval", "spawn.forest.from_farm" },
            { "Forest_Primeval->Farm_Main", "spawn.farm.from_forest" }
        };

        /// <summary>
        /// Sổ tay 6.2 &amp; 6.6: Đoạn Chuyển Map nối Farm_Main và Farm_Core cho phép Người chơi và Quái đi qua,
        /// nhưng TUYỆT ĐỐI chặn đàn heo (heo ở lại Nơi Trú Đàn tại Farm_Main).
        /// </summary>
        public static bool CanTraverseTransitionCorridor(TransitionActorType actorType)
        {
            return actorType == TransitionActorType.Player || actorType == TransitionActorType.Monster;
        }

        public (bool transitioned, string spawnId, float durationSeconds) TransitionBetweenMaps(
            string fromScene,
            string toScene,
            TransitionActorType actorType = TransitionActorType.Player)
        {
            if (actorType == TransitionActorType.Pig &&
                (string.Equals(toScene, "Farm_Core", StringComparison.OrdinalIgnoreCase) ||
                 string.Equals(fromScene, "Farm_Core", StringComparison.OrdinalIgnoreCase)))
            {
                return (false, string.Empty, 0f);
            }

            string key = $"{fromScene}->{toScene}";
            string spawnId = CanonicalSpawnLinks.TryGetValue(key, out var canonical)
                ? canonical
                : (toScene.StartsWith("Event_", StringComparison.OrdinalIgnoreCase)
                    ? $"spawn.event.{toScene.ToLowerInvariant()}.entry"
                    : "spawn.farm.from_event");

            CurrentActiveScene = toScene;
            LastUsedSpawnId = spawnId;
            LastTransitionDurationSeconds = 0.50f; // Màn hình mờ 0.5s <= 1.5s chuẩn mục 6.2
            return (true, spawnId, LastTransitionDurationSeconds);
        }

        /// <summary>
        /// Kiểm tra điều kiện mở khóa 7 phòng chức năng trong Farm_Core theo Cấp trại 1–20 (GDD 3.5 &amp; Sổ tay 6.2).
        /// </summary>
        public bool IsRoomUnlocked(FarmCoreRoomId room, int farmLevel, bool midGameGateUnlocked)
        {
            return room switch
            {
                FarmCoreRoomId.NhaChinh => true,
                FarmCoreRoomId.Kho => true,
                FarmCoreRoomId.PhongXayDung => farmLevel >= 1,
                FarmCoreRoomId.KhuThongTin => farmLevel >= 1,
                FarmCoreRoomId.LeuQuanY => farmLevel >= 3,         // Mở ở Cấp trại 3–4 (Sổ tay 6.2)
                FarmCoreRoomId.PhongNghienCuu => farmLevel >= 8,   // Mở ở Cấp trại 8–10 (Sổ tay 6.2)
                FarmCoreRoomId.XuongCuongHoa => farmLevel >= 10 || midGameGateUnlocked,
                _ => false
            };
        }

        /// <summary>
        /// Hiệu suất của Phòng Xây Dựng theo sự hiện diện và cấp độ của NPC Kỹ Sư Xây Dựng: 40% / 70% / 100% (Sổ tay 6.2).
        /// </summary>
        public static float GetConstructionHallEfficiency(bool hasEngineerNpc, int hallLevel)
        {
            if (!hasEngineerNpc) return 0.40f;
            return hallLevel switch
            {
                1 => 0.70f,
                >= 2 => 1.00f,
                _ => 0.40f
            };
        }

        /// <summary>
        /// Kích hoạt công trình Bạch Vân từ Bảng Điều Khiển trong Nhà chính (Farm_Core):
        /// Chịu độ trễ +3.0 giây so với kích hoạt trực tiếp tại chân công trình (GDD 13.1 &amp; Sổ tay 6.2, P3.17).
        /// </summary>
        public (bool success, float activationDelaySeconds, string message) TriggerBachVanFromMainHouseConsole(
            DefenseManager defense,
            string instanceId)
        {
            var res = defense.TriggerBachVanManual(instanceId);
            if (!res.success) return (false, 0f, res.message);
            return (true, RemoteBachVanDelaySeconds, $"{res.message} (Kích từ Bảng Điều Khiển Nhà Chính: trễ +{RemoteBachVanDelaySeconds:0.0}s)");
        }

        /// <summary>
        /// Xử lý khi nhân vật bị Bất Tỉnh (P1.21 — GDD 8, 19 &amp; Sổ tay 6.11):
        /// - Thể trạng (Condition) -20.
        /// - Mất 10% vàng mang theo (ghi nhận qua Sổ cái EconomyLedger).
        /// - Tỉnh lại ở giường Nhà chính (Farm_Core).
        /// </summary>
        public (int goldLost, int conditionAfter, string respawnLocation) HandlePlayerUnconscious(
            CharacterData character,
            Farm farm,
            EconomyLedger? ledger = null)
        {
            int goldPenalty = (int)Math.Round(farm.Gold * 0.10);
            if (goldPenalty > 0)
            {
                if (ledger != null)
                {
                    ledger.TrySpend(goldPenalty, LedgerReason.FaintPenalty, "player_unconscious");
                }
                else
                {
                    farm.SpendGold(goldPenalty);
                }
            }

            character.Stamina.Condition = Math.Max(0, character.Stamina.Condition - 20);
            character.IsFainted = false;
            character.CurrentGuard = Math.Max(40f, character.MaxGuard * 0.5f);
            character.Stamina.CurrentStamina = Math.Max(40f, character.Stamina.EffectiveMaxStamina * 0.5f);
            CurrentActiveScene = "Farm_Core";
            LastUsedSpawnId = "spawn.core.main_house_bed";

            return (goldPenalty, character.Stamina.Condition, LastUsedSpawnId);
        }

        /// <summary>
        /// Bàn Giải Mã tại Phòng Nghiên Cứu (P2.22 &amp; Sổ tay 6.2.1, GDD 5.4):
        /// - Cần mở Phòng Nghiên Cứu (Cấp trại &gt;= 8) và có Nhà Nghiên Cứu Gen trực.
        /// - Huyền Thoại / Dị Biến bắt buộc phải mở đặc tính "Giải Mã Sâu".
        /// - Thông số chuẩn GDD 5.4:
        ///   + Hiếm: 95%, 700G + 1 thuốc thử, 18 giờ
        ///   + Quý: 85%, 1800G + 2 thuốc thử, 24 giờ
        ///   + Huyền Thoại: 65%, 4500G + 4 thuốc thử, 36 giờ
        ///   + Dị Biến: 50%, 9000G + 6 thuốc thử, 48 giờ
        /// - Khi hoàn tất: khóa 12 giờ nếu thành công, 24 giờ nếu thất bại.
        /// </summary>
        public (bool queued, string message, ResearchQueueJob? job) EnqueueGeneAppraisalAtLab(
            Pig pig,
            Farm farm,
            PetNpcAndOccultManager npcManager,
            CharacterData character,
            EconomyLedger? ledger = null)
        {
            if (pig == null || !pig.IsAlive)
                return (false, "Cá thể heo không hợp lệ.", null);
            if (pig.IsIdentified)
                return (false, $"Heo [{pig.Name}] đã được định danh.", null);
            if (pig.AppraisalLockHoursRemaining > 0)
                return (false, $"Heo [{pig.Name}] đang bị khóa giám định còn {pig.AppraisalLockHoursRemaining} giờ.", null);
            if (!IsRoomUnlocked(FarmCoreRoomId.PhongNghienCuu, farm.FarmLevel, midGameGateUnlocked: false))
                return (false, "Phòng Nghiên Cứu chưa mở (cần Cấp trại >= 8).", null);

            bool hasGeneticist = npcManager.HiredNpcs.Exists(n => n.IsActive && n.Id == NpcSpecialistId.NhaNghienCuuGen);
            if (!hasGeneticist)
                return (false, "Cần có Nhà Nghiên Cứu Gen trực tại Phòng Nghiên Cứu!", null);

            if ((pig.Rarity == GeneRarity.HuyenThoai || pig.Rarity == GeneRarity.DiBien) && !DeepDecodingTraitUnlocked)
                return (false, "Dòng gen Huyền Thoại / Dị Biến bị khóa! Cần Nhà Nghiên Cứu Gen mở đặc tính 'Giải Mã Sâu'.", null);

            var (prob, goldCost, reagentCost, staCost, hours) =
                CanonicalSagaAndGdd6Systems.GetCanonicalAppraisalChannelSpecGdd54(AppraisalChannel.PhongNghienCuu, pig.Rarity);

            int currentReagents = Warehouse.CraftingMaterials.TryGetValue("thuoc_thu_gen", out var rCount) ? rCount : 0;
            if (currentReagents < reagentCost)
                return (false, $"Không đủ thuốc thử gen (cần {reagentCost}, hiện có {currentReagents}).", null);

            if (character.Stamina.CurrentStamina < staCost)
                return (false, $"Không đủ Thể Lực để vận hành Bàn Giải Mã (cần {staCost:0} STA).", null);

            bool paid = ledger != null
                ? ledger.TrySpend(goldCost, LedgerReason.ResearchLab, $"appraise_{pig.Id}")
                : farm.SpendGold(goldCost);
            if (!paid)
                return (false, $"Không đủ {goldCost}G để giám định tại Phòng Nghiên Cứu.", null);

            Warehouse.CraftingMaterials["thuoc_thu_gen"] = currentReagents - reagentCost;
            character.Stamina.TryConsumeStamina(staCost, 0.25f);

            var job = new ResearchQueueJob
            {
                JobId = $"res_{Guid.NewGuid():N}",
                PigId = pig.Id,
                TrueGeneLine = pig.GeneLine,
                Rarity = pig.Rarity,
                GoldSpent = goldCost,
                ReagentsSpent = reagentCost,
                StaminaSpent = staCost,
                TotalDurationHours = hours,
                RemainingHours = hours,
                SuccessProbability = prob,
                IsCompleted = false,
                Succeeded = false
            };
            ResearchQueue.Add(job);
            return (true, $"Đã đưa [{pig.Name}] vào hàng chờ Bàn Giải Mã ({hours}h, tỉ lệ {prob * 100f:0}%, phí {goldCost}G + {reagentCost} thuốc thử).", job);
        }

        public List<ResearchQueueJob> TickResearchQueueHours(int elapsedHours, List<Pig> pigs, double? deterministicRoll = null, IEventBus? eventBus = null)
        {
            var finished = new List<ResearchQueueJob>();
            if (elapsedHours <= 0) return finished;

            var rng = new Random(1337);
            for (int i = 0; i < ResearchQueue.Count; i++)
            {
                var job = ResearchQueue[i];
                if (job.IsCompleted) continue;

                job.RemainingHours = Math.Max(0, job.RemainingHours - elapsedHours);
                if (job.RemainingHours == 0)
                {
                    job.IsCompleted = true;
                    double roll = deterministicRoll ?? rng.NextDouble();
                    job.Succeeded = roll <= job.SuccessProbability;

                    var pig = pigs.Find(p => p.Id == job.PigId);
                    if (pig != null)
                    {
                        if (job.Succeeded)
                        {
                            pig.IsIdentified = true;
                            pig.AppraisalLockHoursRemaining = 12; // GDD 5.4: khóa 12 giờ nếu thành công
                            eventBus?.Publish(new GeneIdentifiedEvent(pig.Id, pig.GeneLine, pig.Rarity, "PhongNghienCuu"));
                        }
                        else
                        {
                            pig.AppraisalLockHoursRemaining = 24; // GDD 5.4: khóa 24 giờ nếu thất bại
                        }
                    }
                    finished.Add(job);
                }
            }
            return finished;
        }

        /// <summary>
        /// Xưởng Cường Hóa — Bàn Gia Cường Giấy Chuyên Biệt (P4.11 &amp; Sổ tay 6.2.1, GDD 10.6–10.7):
        /// - +1: 100%
        /// - +2: 65% + 6%/đá, tối đa 95%
        /// - +3: 40% + 5%/đá, tối đa 80%
        /// - Thiếu nguyên liệu: nhân hệ số (nộp / yêu cầu)^1.5, tối thiểu 10%.
        /// - Thất bại: mất nguyên liệu &amp; vàng, nhưng GIỮ NGUYÊN cấp giấy (không hỏng giấy!).
        /// </summary>
        public (bool success, float finalSuccessRate, int newPaperLevel, string message) EnhancePaperAtWorkshopGdd10(
            int currentPaperLevel,
            int stonesUsed,
            int submittedMaterials,
            int requiredMaterials,
            bool midGameGateUnlocked,
            SpecialtyPaperColor paperColor = SpecialtyPaperColor.Lam,
            double? deterministicRoll = null,
            IEventBus? eventBus = null)
        {
            if (!midGameGateUnlocked)
                return (false, 0f, currentPaperLevel, "Cần vượt qua Cổng Giữa Game để mở Bàn Gia Cường Giấy!");
            if (currentPaperLevel >= 3)
                return (false, 0f, currentPaperLevel, "Giấy Chuyên Biệt đã đạt cấp tối đa +3.");

            int targetLevel = currentPaperLevel + 1;
            int stones = Math.Max(0, stonesUsed);

            float baseRate = targetLevel switch
            {
                1 => 1.00f,
                2 => Math.Min(0.95f, 0.65f + 0.06f * stones), // +2: 65% + 6%/đá, trần 95%
                3 => Math.Min(0.80f, 0.40f + 0.05f * stones), // +3: 40% + 5%/đá, trần 80%
                _ => 0.80f
            };

            float finalRate = baseRate;
            if (requiredMaterials > 0 && submittedMaterials < requiredMaterials)
            {
                double ratio = Math.Clamp((double)submittedMaterials / requiredMaterials, 0.0, 1.0);
                float materialFactor = (float)Math.Pow(ratio, 1.5); // (nộp/yêu cầu)^1.5
                finalRate = Math.Max(0.10f, baseRate * materialFactor); // Tối thiểu 10%
            }

            double roll = deterministicRoll ?? new Random().NextDouble();
            bool succeeded = roll <= finalRate + 1e-6;
            int resultLevel = succeeded ? targetLevel : currentPaperLevel; // Thất bại không hỏng giấy

            eventBus?.Publish(new PaperEnhancedEvent(paperColor, resultLevel, succeeded));
            string msg = succeeded
                ? $"Gia cường Giấy [{paperColor}] thành công lên +{resultLevel} (tỉ lệ {finalRate * 100f:0.1}%)!"
                : $"Gia cường Giấy [{paperColor}] lên +{targetLevel} thất bại (tỉ lệ {finalRate * 100f:0.1}%). Giấy được giữ nguyên ở +{currentPaperLevel}.";
            return (succeeded, finalRate, resultLevel, msg);
        }

        /// <summary>
        /// Xưởng Cường Hóa — Bàn Rã Giấy chuẩn Sổ tay 6.2.1 (GDD 10.7):
        /// - Giấy Lam → 2 Tơ Vàng (to_vang)
        /// - Giấy Vàng → 2 Bột Tử Tinh (bot_tu_tinh)
        /// - Giấy Tím → 1 Huyết Triện (huyet_trien)
        /// </summary>
        public (string materialId, int quantity, string message) SalvageSpecialtyPaperCanonical(SpecialtyPaperColor color)
        {
            (string matId, string matName, int qty) = color switch
            {
                SpecialtyPaperColor.Lam => ("to_vang", "Tơ Vàng", 2),
                SpecialtyPaperColor.Vang => ("bot_tu_tinh", "Bột Tử Tinh", 2),
                SpecialtyPaperColor.Tim => ("huyet_trien", "Huyết Triện", 1),
                _ => ("tinh_hoa_di_bien", "Tinh Hoa Dị Biến", 1)
            };

            if (!Warehouse.CraftingMaterials.ContainsKey(matId))
                Warehouse.CraftingMaterials[matId] = 0;
            Warehouse.CraftingMaterials[matId] += qty;

            return (matId, qty, $"[BÀN RÃ GIẤY] Rã Giấy [{color}] nhận +{qty} {matName}.");
        }

        /// <summary>
        /// Bàn Dùng Giấy (6 khóa Đặc tính chuyên biệt - GDD 10.4–10.5 &amp; Sổ tay 6.2.1):
        /// Tên đặc tính chuyên biệt ẩn hoàn toàn ("???") cho đến khi mở đủ 6/6 khóa!
        /// </summary>
        public static string GetDisplayedSpecialtyTraitName(int unlockedLocksCount, string trueSpecialtyName)
        {
            return unlockedLocksCount >= 6 ? trueSpecialtyName : "??? (Ẩn — cần mở đủ 6/6 khóa)";
        }

        /// <summary>
        /// Xưởng Cường Hóa — Bàn Khắc Phù Văn (Sổ tay 6.2.1, GDD 12.4):
        /// Điều kiện mở: Qua Cổng Giữa Game + &gt;= 12 đợt lớn + Cấp nhân vật &gt;= 15 + Phòng Xây Dựng cấp 2 + bản vẽ Bàn Khắc (Cao cấp).
        /// Tỉ lệ khắc bậc I–III: 100% / 80% / 60%.
        /// </summary>
        public (bool success, float successRate, string message) EngraveRuneAtWorkshop(
            CharacterData character,
            string runeId,
            int runeTier,
            bool midGameGateUnlocked,
            int majorWavesSurvived,
            int hallLevel,
            double? deterministicRoll = null,
            IEventBus? eventBus = null)
        {
            if (!midGameGateUnlocked || majorWavesSurvived < 12 || character.Level < 15 || hallLevel < 2 || !HasHighTierEngravingTableBlueprint)
            {
                return (false, 0f, "Chưa đủ điều kiện mở Bàn Khắc Phù Văn (cần Cổng Giữa Game, >=12 đợt lớn, Cấp >=15, Phòng Xây Dựng cấp 2, bản vẽ Bàn Khắc Cao cấp).");
            }

            float rate = runeTier switch
            {
                1 => 1.00f,
                2 => 0.80f,
                _ => 0.60f
            };

            double roll = deterministicRoll ?? new Random().NextDouble();
            bool ok = roll <= rate + 1e-6 && character.EngraveRune(runeId);
            eventBus?.Publish(new RuneEngravedEvent("equipped_slot", runeId, ok));
            return (ok, rate, ok ? $"Khắc thành công Phù Văn [{runeId}] (tỉ lệ {rate * 100f:0}%)!" : $"Khắc Phù Văn [{runeId}] thất bại (tỉ lệ {rate * 100f:0}%).");
        }

        /// <summary>
        /// Xưởng Cường Hóa — Bàn Khắc Mệnh Ấn (Sổ tay 6.2.1, GDD 12.4):
        /// Điều kiện mở: Đã mở Phù Văn &gt;= 5 chu kỳ + &gt;= 18 đợt lớn + Cấp &gt;= 22 + đã hạ &gt;= 2 Boss + xong &gt;= 1 sự kiện Đỏ.
        /// Chỉ khắc cho nhân vật người chơi (không khắc cho NPC), tối đa 3 Mệnh Ấn, mỗi lần trừ vĩnh viễn 10 HP tối đa.
        /// </summary>
        public (bool success, int newMaxHp, string message) EngraveSealAtWorkshop(
            CharacterData character,
            string sealId,
            bool isPlayerCharacter,
            int majorWavesSurvived,
            IEventBus? eventBus = null)
        {
            if (!isPlayerCharacter)
                return (false, character.MaxHp, "Mệnh Ấn chỉ khắc lên nhân vật người chơi (Khoa/An), tuyệt đối không khắc cho NPC!");

            if (CyclesSinceRuneSystemUnlocked < 5 ||
                majorWavesSurvived < 18 ||
                character.Level < 22 ||
                BossesDefeatedCount < 2 ||
                RedEventsCompletedCount < 1)
            {
                return (false, character.MaxHp, "Chưa đủ điều kiện mở Bàn Khắc Mệnh Ấn (cần mở Phù Văn >=5 chu kỳ, >=18 đợt lớn, Cấp >=22, hạ >=2 Boss, xong >=1 sự kiện Đỏ).");
            }

            bool ok = character.EngraveSeal(sealId);
            if (!ok)
                return (false, character.MaxHp, "Đã đạt giới hạn tối đa 3 Mệnh Ấn!");

            eventBus?.Publish(new SealEngravedEvent(character.Identity, sealId, character.MaxHp));
            return (true, character.MaxHp, $"Đã khắc Mệnh Ấn [{sealId}]! Máu tối đa trừ vĩnh viễn 10 HP (còn {character.MaxHp} HP).");
        }
    }

    /// <summary>
    /// Hệ thống Cấp Trại 1–20 &amp; Bảng Mở Khóa Theo Mốc (P2.21 — Người A, GDD 3.5).
    /// </summary>
    [Serializable]
    public sealed class FarmLevelProgressionSystem
    {
        public const int MaxFarmLevel = 20;

        public static int GetRequiredExpForNextLevel(int currentLevel)
        {
            int lvl = Math.Clamp(currentLevel, 1, MaxFarmLevel);
            return 100 + (lvl - 1) * 75;
        }

        public static (int newLevel, List<string> newlyUnlockedFeatures) AddFarmExp(Farm farm, int expAmount)
        {
            var unlocked = new List<string>();
            if (expAmount <= 0 || farm.FarmLevel >= MaxFarmLevel)
                return (farm.FarmLevel, unlocked);

            farm.FarmExp += expAmount;
            while (farm.FarmLevel < MaxFarmLevel && farm.FarmExp >= GetRequiredExpForNextLevel(farm.FarmLevel))
            {
                farm.FarmExp -= GetRequiredExpForNextLevel(farm.FarmLevel);
                farm.FarmLevel += 1;
                unlocked.AddRange(GetMilestoneUnlocksForLevel(farm.FarmLevel));
            }
            return (farm.FarmLevel, unlocked);
        }

        public static List<string> GetMilestoneUnlocksForLevel(int level)
        {
            return level switch
            {
                2 => new List<string> { "KhoaChuyenBiet_1", "MoRongO_Dat_1" },
                3 => new List<string> { "LeuQuanY", "TuyenNpcCoBan" },
                4 => new List<string> { "KhoaChuyenBiet_2", "PhongXayDung_Cap2" },
                5 => new List<string> { "ChoDem", "HopDongGiaoHeo_CapCao", "CongGiuaGame_DieuKienCapTrai" },
                8 => new List<string> { "PhongNghienCuu", "GiamDinhGen_BanGiaiMa" },
                10 => new List<string> { "XuongCuongHoa", "PhongXayDung_Cap3" },
                12 => new List<string> { "CongTrinhBachVan", "MayPhatDien_Cap3" },
                15 => new List<string> { "BanKhacPhuVan", "HopDongGiaoHeo_HuyenThoai" },
                18 => new List<string> { "BanKhacMenhAn_DieuKienTrai" },
                20 => new List<string> { "DaiTraiChu_CapToiDa_20" },
                _ => new List<string>()
            };
        }
    }

    // =========================================================================
    // 5. HỆ THỐNG CỦA NGƯỜI C: CHỢ THỊ TRẤN (NGÀY/ĐÊM), LỊCH ĐỢT QUÁI & BÁO CÁO 7 MỤC
    // =========================================================================

    [Serializable]
    public sealed class MarketTownState
    {
        public const float MapWidthMeters = 50.0f;
        public const float MapHeightMeters = 40.0f;
        public const int ScreeningFeeGold = 120; // Quầy sàng lọc 120G (Sổ tay 6.4)
        public const int QuarantineFeeGold = 60; // Quầy kiểm dịch 60G (Sổ tay 6.4)
        public const float HighTierNightAppraisalSpawnChance = 0.12f; // 12% số đêm xuất hiện Quầy Giám Định Cấp Cao

        public bool IsNightLayerActive { get; private set; }
        public bool IsHighTierAppraisalBoothOpenTonight { get; private set; }
        public bool AllowsCombat => false; // Ngoại vi không có combat
        public bool HasMysticMerchantInsideTown => false; // Bí nhân chỉ xuất hiện ngoài rào trại (GDD 21 & Sổ tay 6.4)

        public static readonly IReadOnlyList<string> FourFunctionalMarketNpcs = new List<string>
        {
            "NguoiBanChoNgay",
            "ChuOHangChoDem",
            "DauGiaVien",
            "GiamDinhVien"
        };

        public static readonly IReadOnlyList<NpcSpecialistId> FiveMercenaryGuildNpcs = new List<NpcSpecialistId>
        {
            NpcSpecialistId.VeBinhKhien,
            NpcSpecialistId.XaThuNo,
            NpcSpecialistId.KySuChienTruong,
            NpcSpecialistId.ThoSanUfo,
            NpcSpecialistId.ThichKhachDem
        };

        public void SyncWithClock(int currentHour, Random rng, bool rollNewNight = false)
        {
            IsNightLayerActive = currentHour >= 18 || currentHour < 6;
            if (IsNightLayerActive && rollNewNight)
            {
                IsHighTierAppraisalBoothOpenTonight = rng.NextDouble() < HighTierNightAppraisalSpawnChance;
            }
            else if (!IsNightLayerActive)
            {
                IsHighTierAppraisalBoothOpenTonight = false;
            }
        }
    }

    /// <summary>
    /// Lịch đợt quái lớn/nhỏ theo giai đoạn game (P3.06 — Người C, GDD 1.2)
    /// và Báo cáo sau đợt đủ 7 mục chuẩn GDD 18 (P3.17 — Người C).
    /// </summary>
    public static class WaveScheduleAndReportSystem
    {
        public static (int minDays, int maxDays) GetMajorWaveIntervalByPhase(int cycleNumber)
        {
            if (cycleNumber <= 2) return (6, 8);   // Đầu game: 6–8 ngày
            if (cycleNumber <= 5) return (7, 10);  // Giữa game: 7–10 ngày
            return (8, 12);                        // Cuối game: 8–12 ngày
        }

        public static EnemyTypeId? GetScheduledCycleBoss(int cycleNumber)
        {
            if (cycleNumber < 5 || cycleNumber % 5 != 0) return null;
            int mod15 = cycleNumber % 15;
            if (mod15 == 5) return EnemyTypeId.ChuaTromRiuSat; // Chu kỳ 5, 20, 35...
            if (mod15 == 10) return EnemyTypeId.UfoMothership; // Chu kỳ 10, 25, 40... (Mẫu Hạm UFO)
            return EnemyTypeId.ThucTheSuong;                   // Chu kỳ 15, 30, 45...
        }

        public static List<string> BuildSevenSectionWaveReportGdd18(WaveBattleReport report, int repairCostEstimateGold, string bossPhaseSummary = "Không có Boss")
        {
            return new List<string>
            {
                $"1. Tổng quan đợt #{report.WaveNumber}: {(report.Victory ? "THẮNG LỢI" : "THẤT THỦ")}",
                $"2. Quái & Pha Boss: Hạ {report.EnemiesDefeated} quái | Boss: {bossPhaseSummary}",
                $"3. Thiệt hại Đàn heo: Mất/Bị bắt cóc {report.PigsLostOrAbducted} con",
                $"4. Độ bền Công trình & Chi phí sửa: Hao -{report.BuildingDurabilityLost} độ bền (Ước tính sửa: {repairCostEstimateGold}G)",
                $"5. Tiêu hao Đạn & Điện năng: Đã bắn {report.AmmoSpent} đơn vị đạn",
                $"6. Nhân vật & Lính đánh thuê: Số lần vỡ chống chịu/ngất: {report.PlayerGuardBreaks}",
                $"7. Chiến lợi phẩm & Phần thưởng: +{report.GoldRewarded} Vàng"
            };
        }
    }

    // =========================================================================
    // 6. HỆ THỐNG CỦA NGƯỜI D: 8 CHỦ ĐỀ CHU KỲ (24/12/12/10/8/10/12/12%),
    //    HỢP ĐỒNG GIAO HEO, ĐẤU GIÁ CHỢ ĐÊM, TÌNH BÁO LÀM MỜ, MINIMAP,
    //    LOCALIZATION, FOREST_PRIMEVAL & MÔ PHỎNG KINH TẾ HEADLESS 30–60 NGÀY
    // =========================================================================

    [Serializable]
    public sealed class PigDeliveryContract
    {
        public string ContractId { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public int RequiredPigCount { get; set; } = 2;
        public GeneLineId? RequiredGeneLine { get; set; }
        public GeneRarity MinimumRarity { get; set; } = GeneRarity.Thuong;
        public MeatQuality MinimumMeatQuality { get; set; } = MeatQuality.B;
        public int DeadlineDay { get; set; } = 7;
        public int RewardGold { get; set; } = 1200;
        public int RewardFarmExp { get; set; } = 150;
        public bool IsCompleted { get; set; }
    }

    [Serializable]
    public sealed class IntelForecastView
    {
        public int IntelLevel { get; set; }
        public string TimeWindowText { get; set; } = "???";
        public string DirectionText { get; set; } = "???";
        public string ScaleAndCountText { get; set; } = "???";
        public string EliteAndBossText { get; set; } = "???";
        public string TacticalHoldRecommendation { get; set; } = "???";
    }

    public static class EconomyAndIntelExpansionSystem
    {
        /// <summary>
        /// GDD 20.2 &amp; Sổ tay 6.11 (P2.24 — Người D):
        /// Sinh chủ đề chu kỳ chợ theo đúng phân phối xác suất chuẩn:
        /// CanBang 24%, MuaHeoRe 12%, ConSotThit 12%, KhanHiemThucAn 10%,
        /// HoangLoanDich 8%, KhanHiemHauDotKich 10%, MuaTrangBi 12%, MuaCongTrinh 12%.
        /// </summary>
        public static MarketTheme RollMarketCycleThemeWeighted(Random rng)
        {
            double r = rng.NextDouble() * 100.0;
            if (r < 24.0) return MarketTheme.CanBang;               // 0..24 (24%)
            if (r < 36.0) return MarketTheme.MuaHeoRe;              // 24..36 (12%)
            if (r < 48.0) return MarketTheme.ConSotThit;            // 36..48 (12%)
            if (r < 58.0) return MarketTheme.KhanHiemThucAn;        // 48..58 (10%)
            if (r < 66.0) return MarketTheme.HoangLoanDich;         // 58..66 (8%)
            if (r < 76.0) return MarketTheme.KhanHiemHauDotKich;    // 66..76 (10%)
            if (r < 88.0) return MarketTheme.MuaTrangBi;            // 76..88 (12%)
            return MarketTheme.MuaCongTrinh;                        // 88..100 (12%)
        }

        /// <summary>
        /// Hoàn thành Hợp đồng giao heo tại Chợ Ngày (P2.24 — Người D, GDD 20.3).
        /// </summary>
        public static (bool fulfilled, int goldEarned, int expEarned, string message) TryFulfillDeliveryContract(
            PigDeliveryContract contract,
            List<Pig> herdPigs,
            int currentDay,
            Farm farm,
            EconomyLedger? ledger = null)
        {
            if (contract == null || contract.IsCompleted)
                return (false, 0, 0, "Hợp đồng không hợp lệ hoặc đã hoàn thành.");
            if (currentDay > contract.DeadlineDay)
                return (false, 0, 0, "Hợp đồng đã quá hạn giao hàng!");

            var eligible = herdPigs.FindAll(p =>
                p.IsAlive &&
                p.Stage == PigStage.TruongThanh &&
                p.Rarity >= contract.MinimumRarity &&
                p.MeatQuality >= contract.MinimumMeatQuality &&
                (!contract.RequiredGeneLine.HasValue || p.GeneLine == contract.RequiredGeneLine.Value));

            if (eligible.Count < contract.RequiredPigCount)
                return (false, 0, 0, $"Chưa đủ heo đạt chuẩn hợp đồng (cần {contract.RequiredPigCount}, hiện có {eligible.Count}).");

            for (int i = 0; i < contract.RequiredPigCount; i++)
            {
                herdPigs.Remove(eligible[i]);
            }

            contract.IsCompleted = true;
            if (ledger != null)
            {
                ledger.Earn(contract.RewardGold, LedgerReason.ContractReward, contract.ContractId);
            }
            else
            {
                farm.AddGold(contract.RewardGold);
            }

            FarmLevelProgressionSystem.AddFarmExp(farm, contract.RewardFarmExp);
            return (true, contract.RewardGold, contract.RewardFarmExp,
                $"Đã giao đủ {contract.RequiredPigCount} heo cho hợp đồng [{contract.Title}]: +{contract.RewardGold}G, +{contract.RewardFarmExp} EXP trại!");
        }

        /// <summary>
        /// Sàn Đấu Giá Chợ Đêm (P2.09 — Người D, GDD 20):
        /// Đấu giá vật phẩm hiếm với NPC theo bước giá; thanh toán qua EconomyLedger.
        /// </summary>
        public static (bool wonAuction, int finalPriceGold, string message) RunNightMarketAuction(
            GameItem item,
            int playerMaxBidGold,
            int npcMaxValuationGold,
            int stepGold,
            EconomyLedger ledger)
        {
            int startBid = Math.Max(100, (int)Math.Round(item.ReferenceValueGold * 0.8f));
            int step = Math.Max(50, stepGold);

            if (playerMaxBidGold < startBid || ledger.Gold < startBid)
                return (false, 0, "Không đủ ngân sách để đặt giá khởi điểm.");

            int winningBid = Math.Min(playerMaxBidGold, npcMaxValuationGold + step);
            if (playerMaxBidGold <= npcMaxValuationGold)
            {
                return (false, npcMaxValuationGold, $"NPC trả giá cao hơn ({npcMaxValuationGold}G) và thắng đấu giá [{item.Name}].");
            }

            if (!ledger.TrySpend(winningBid, LedgerReason.AuctionBid, $"auction_{item.Id}"))
                return (false, 0, "Số dư vàng trong Sổ cái không đủ thanh toán mức giá thắng.");

            return (true, winningBid, $"Thắng đấu giá [{item.Name}] ở mức {winningBid}G!");
        }

        /// <summary>
        /// Giao dịch với Bí Nhân (P4.01 — Người D, GDD 21):
        /// Bất biến thiết kế: Bí nhân KHÔNG BAO GIỜ nhận vàng mặt và luôn đòi tổng GTTC vật phẩm trao đổi cao hơn GTTC món đưa ra (>= 1.15x).
        /// </summary>
        public static (bool accepted, string reason) TryBarterWithMysticMerchant(
            GameItem requestedSecretItem,
            List<GameItem> offeredPlayerItems,
            int offeredGoldAmount)
        {
            if (offeredGoldAmount > 0)
            {
                return (false, "BẤT BIẾN GDD 21: Bí Nhân tuyệt đối KHÔNG nhận Vàng trong mọi giao dịch!");
            }
            if (offeredPlayerItems == null || offeredPlayerItems.Count == 0)
            {
                return (false, "Cần đưa ra vật phẩm hoặc heo để trao đổi.");
            }

            int offeredGttc = 0;
            for (int i = 0; i < offeredPlayerItems.Count; i++)
            {
                if (offeredPlayerItems[i].Category == "Currency")
                    return (false, "Bí Nhân không nhận tiền tệ dưới mọi hình thức!");
                offeredGttc += offeredPlayerItems[i].ReferenceValueGold;
            }

            int requiredMinimumGttc = (int)Math.Ceiling(requestedSecretItem.ReferenceValueGold * 1.15); // Luôn đòi GTTC cao hơn món đưa ra
            if (offeredGttc < requiredMinimumGttc)
            {
                return (false, $"Tổng GTTC đưa ra ({offeredGttc}) thấp hơn mức Bí Nhân đòi hỏi ({requiredMinimumGttc} > {requestedSecretItem.ReferenceValueGold}).");
            }

            return (true, $"Bí Nhân gật đầu đổi [{requestedSecretItem.Name}] (GTTC {requestedSecretItem.ReferenceValueGold}) lấy các vật phẩm tổng GTTC {offeredGttc}!");
        }

        /// <summary>
        /// Tình báo cấp 0–5 (P3.08 — Người D, GDD 14, 23 &amp; Sổ tay 5.4):
        /// Dự báo đợt quái hiển thị đúng theo cấp tình báo 0–5; tuyệt đối KHÔNG lộ thông tin người chơi chưa mở/mua!
        /// </summary>
        public static IntelForecastView BuildIntelForecastByLevel(
            int intelLevel,
            int exactHoursUntilWave,
            string trueDirections,
            int trueEnemyCount,
            string trueEliteAndBossNames,
            string trueHoldRecommendation)
        {
            int lvl = Math.Clamp(intelLevel, 0, 5);
            var view = new IntelForecastView { IntelLevel = lvl };

            if (lvl >= 1)
            {
                view.TimeWindowText = lvl >= 3
                    ? $"Chính xác sau {exactHoursUntilWave}h00p"
                    : $"Khoảng {Math.Max(1, exactHoursUntilWave - 3)}–{exactHoursUntilWave + 3} giờ tới";
            }
            if (lvl >= 2)
            {
                view.DirectionText = trueDirections;
                view.ScaleAndCountText = lvl >= 5
                    ? $"Chính xác {trueEnemyCount} cá thể"
                    : (trueEnemyCount >= 20 ? "Quy mô Lớn" : "Quy mô Vừa/Nhỏ");
            }
            if (lvl >= 4)
            {
                view.EliteAndBossText = trueEliteAndBossNames;
            }
            if (lvl >= 5)
            {
                view.TacticalHoldRecommendation = trueHoldRecommendation;
            }

            return view;
        }
    }

    /// <summary>
    /// Hệ thống Minimap với viền ranh giới trại &amp; Sương Mù Lạc Hướng 15m (P2.25 — Người D, GDD 3.10-B1).
    /// </summary>
    [Serializable]
    public sealed class MinimapProjectionSystem
    {
        public const float FogDisorientationVisibilityRadiusMeters = 15.0f;
        public bool IsFarmBoundaryPolygonVisible { get; set; } = true;

        public bool IsEntityVisibleOnMinimap(float distanceFromPlayerMeters, WeatherType currentWeather, bool hasDisorientedStatus = false)
        {
            if (currentWeather == WeatherType.SuongMu || hasDisorientedStatus)
            {
                return distanceFromPlayerMeters <= FogDisorientationVisibilityRadiusMeters; // Ẩn mọi thứ ngoài 15m
            }
            return true;
        }
    }

    /// <summary>
    /// Hệ thống Rừng Nguyên Sinh `Forest_Primeval` (80×80m) &amp; Khung Sự Kiện Huyền Bí (P0.18, P1.20, P4.02, P4.12 — Người D, Sổ tay 6.5).
    /// </summary>
    [Serializable]
    public sealed class ForestPrimevalAndEventFramework
    {
        public const float ForestWidthMeters = 80.0f;
        public const float ForestHeightMeters = 80.0f;
        public const int MaxDaysTrappedInEvent = 3;

        public bool AllowsMonsterWavesInForest => false; // Ngoại vi không có đợt quái (Sổ tay 6.5)
        public bool HasFixedOccultGatesInForest => false; // Cổng chỉ xuất hiện ở nông trại (GDD 24 & Sổ tay 6.5)

        public static float CalculateCanopyOpacity(bool isActorUnderCanopy)
        {
            return isActorUnderCanopy ? 0.35f : 1.00f; // Tán cây mờ đi khi người chơi đi bên dưới
        }

        /// <summary>
        /// Khung sự kiện Cổng / Sương mù chuẩn GDD 24 &amp; Sổ tay 6.5:
        /// - Cổng (isGateEvent = true): thoát sớm bằng Bùa Hồi Hương khi hoàn thành &gt;= 50%.
        /// - Sương mù (isGateEvent = false): Bùa Hồi Hương vô hiệu, bắt buộc dùng La Bàn Tán Vụ hoặc Ngọc Phá Sương.
        /// - Kẹt tối đa 3 ngày; chia bậc thưởng 30% / 50% / 100%.
        /// </summary>
        public static (bool canExit, int rewardPercentTier, string message) EvaluateOccultEventExit(
            bool isGateEvent,
            float completionPercent,
            int daysElapsedInEvent,
            bool useBuaHoiHuong,
            bool useLaBanTanVuOrNgocPhaSuong)
        {
            if (completionPercent >= 100f)
            {
                return (true, 100, "Hoàn thành 100% sự kiện: Thoát an toàn và nhận 100% phần thưởng!");
            }

            if (daysElapsedInEvent >= MaxDaysTrappedInEvent)
            {
                return (true, 30, "Đã hết giới hạn kẹt tối đa 3 ngày: Tự động đẩy ra ngoài và chỉ nhận 30% phần thưởng.");
            }

            if (isGateEvent)
            {
                if (useBuaHoiHuong)
                {
                    if (completionPercent >= 50f)
                        return (true, 50, "Dùng Bùa Hồi Hương tại Cổng (>=50% tiến độ): Thoát thành công và nhận 50% phần thưởng!");
                    return (false, 0, "Chưa đạt 50% tiến độ Cổng nên không thể kích hoạt Bùa Hồi Hương!");
                }
            }
            else
            {
                if (useBuaHoiHuong && !useLaBanTanVuOrNgocPhaSuong)
                {
                    return (false, 0, "Trong Sương Mù Dị Giới, vật phẩm bậc Thường (Bùa Hồi Hương) vô hiệu! Cần La Bàn Tán Vụ hoặc Ngọc Phá Sương.");
                }
                if (useLaBanTanVuOrNgocPhaSuong)
                {
                    int tier = completionPercent >= 50f ? 50 : 30;
                    return (true, tier, $"Dùng La Bàn Tán Vụ / Ngọc Phá Sương phá Sương Mù: Thoát thành công và nhận {tier}% phần thưởng!");
                }
            }

            return (false, 0, "Sự kiện vẫn đang diễn ra.");
        }
    }

    /// <summary>
    /// Hệ thống Localization (vi/en) &amp; HUD v1 Presenter hỗ trợ mù màu (P1.11, P2.14 — Người D, Sổ tay 5.4).
    /// </summary>
    public static class LocalizationAndHudPresenter
    {
        private static readonly Dictionary<string, (string vi, string en)> Table = new Dictionary<string, (string vi, string en)>(StringComparer.OrdinalIgnoreCase)
        {
            { "hud.hp", ("Máu", "HP") },
            { "hud.guard", ("Chống Chịu", "Guard") },
            { "hud.stamina", ("Thể Lực", "Stamina") },
            { "hud.rage", ("Nộ Khí", "Rage") },
            { "hud.gold", ("Vàng", "Gold") },
            { "hud.day", ("Ngày", "Day") },
            { "hud.weather", ("Thời Tiết", "Weather") },
            { "hud.density", ("Mật Độ Đàn", "Herd Density") },
            { "hud.habitat", ("Sinh Cảnh", "Habitat") },
            { "hud.event_pressure", ("Áp Lực Sự Kiện", "Event Pressure") },
            { "gene.unknown", ("?", "?") }
        };

        public static string Get(string key, string lang = "vi")
        {
            if (string.IsNullOrWhiteSpace(key)) return string.Empty;
            if (!Table.TryGetValue(key, out var pair)) return $"[MISSING:{key}]";
            return string.Equals(lang, "en", StringComparison.OrdinalIgnoreCase) ? pair.en : pair.vi;
        }

        public static List<string> FindMissingKeys(IEnumerable<string> requiredKeys)
        {
            var missing = new List<string>();
            foreach (var k in requiredKeys)
            {
                if (!Table.TryGetValue(k, out var pair) || string.IsNullOrWhiteSpace(pair.vi))
                {
                    missing.Add(k);
                }
            }
            return missing;
        }

        /// <summary>
        /// Sổ tay 5.4: Mọi chỉ báo trạng thái (mật độ, Sinh Cảnh, Áp Lực Sự Kiện) luôn có cả biểu tượng hình học + màu + số để hỗ trợ người mù màu.
        /// </summary>
        public static (string shapeSymbol, string colorHex, string formattedBadge) BuildColorblindSafeIndicator(string metricKey, int value0To100, bool higherIsBetter)
        {
            int score = higherIsBetter ? value0To100 : (100 - value0To100);
            (string symbol, string hex) = score switch
            {
                >= 75 => ("[●]", "#2ECC71"), // Tròn Xanh Lá (Tốt)
                >= 50 => ("[▲]", "#F1C40F"), // Tam Giác Vàng (Chú ý)
                >= 25 => ("[◆]", "#E67E22"), // Hình Thoi Cam (Cảnh báo)
                _ => ("[✖]", "#E74C3C")      // Chữ Thập Đỏ (Nguy hiểm)
            };

            string label = Get(metricKey, "vi");
            return (symbol, hex, $"{symbol} {label}: {value0To100}");
        }

        /// <summary>
        /// P2.06 (Người D): Heo chưa định danh từ bậc Hiếm trở lên phải hiển thị dấu "?".
        /// </summary>
        public static string GetDisplayedPigGeneName(Pig pig)
        {
            if (pig == null) return "?";
            if (!pig.IsIdentified && pig.Rarity >= GeneRarity.Hiem)
            {
                return Get("gene.unknown");
            }
            return pig.GeneLine.ToString();
        }
    }

    // =========================================================================
    // 7. KIỂM ĐỊNH SAVE/LOAD 3 LẦN LIÊN TIẾP, VERTICAL SLICE & MÔ PHỎNG 30–60 NGÀY
    // =========================================================================

    public static class VerificationAndSimulationSuite
    {
        /// <summary>
        /// P1.01 (Người A): Lưu/tải 3 lần liên tiếp cho state giống hệt (so sánh chuỗi JSON chuẩn hóa &amp; mã băm).
        /// </summary>
        public static (bool identical, string hash1, string hash2, string hash3) VerifyThreeConsecutiveSaveLoadCycles(GameEngine sourceEngine)
        {
            string json1 = SaveSystem.Serialize(sourceEngine.CreateSaveData());
            var e1 = new GameEngine();
            SaveSystem.TryDeserialize(json1, out var d1, out _);
            e1.RestoreFromSave(d1);

            string json2 = SaveSystem.Serialize(e1.CreateSaveData());
            var e2 = new GameEngine();
            SaveSystem.TryDeserialize(json2, out var d2, out _);
            e2.RestoreFromSave(d2);

            string json3 = SaveSystem.Serialize(e2.CreateSaveData());

            string h1 = ComputeDeterministicHash(json1);
            string h2 = ComputeDeterministicHash(json2);
            string h3 = ComputeDeterministicHash(json3);
            return (h1 == h2 && h2 == h3 && json1 == json2 && json2 == json3, h1, h2, h3);
        }

        public static string ComputeDeterministicHash(string text)
        {
            unchecked
            {
                uint hash = 2166136261u;
                for (int i = 0; i < text.Length; i++)
                {
                    hash ^= text[i];
                    hash *= 16777619u;
                }
                return hash.ToString("X8");
            }
        }

        /// <summary>
        /// P1.14 (Người A): Xuất header và bản ghi CSV Telemetry mỗi ngày game.
        /// </summary>
        public static string BuildDailyTelemetryCsvRow(GameEngine engine, string waveResult)
        {
            int piglets = engine.Pigs.FindAll(p => p.IsAlive && p.Stage == PigStage.HeoNon).Count;
            int growing = engine.Pigs.FindAll(p => p.IsAlive && p.Stage == PigStage.DangLon).Count;
            int adults = engine.Pigs.FindAll(p => p.IsAlive && p.Stage == PigStage.TruongThanh).Count;
            int elders = engine.Pigs.FindAll(p => p.IsAlive && p.Stage == PigStage.HeoGia).Count;
            int corpses = engine.Pigs.FindAll(p => !p.IsAlive).Count;
            var cap = engine.Farm.CalculateCapacityReport(piglets + growing + adults + elders, corpses);

            return $"{engine.Clock.CurrentDay},{engine.Farm.Gold},{piglets},{growing},{adults},{elders},{cap.RawDensityRatio:0.00},{engine.Farm.HabitatIndex},{engine.Farm.EventPressureIndex},{waveResult}";
        }

        /// <summary>
        /// P1.15 (Người A): Chạy trọn kịch bản Vertical Slice 1 ngày game:
        /// Chọn An hoặc Khoa → Chăm heo → Bán ở chợ ngày → Nạp trước 1 công trình → Chống 1 đợt quái nhỏ → Lưu/Tải 3 lần.
        /// </summary>
        public static (bool success, int finalGold, string stateHash) RunVerticalSliceFullDayScenario(PlayerId selectedPlayer)
        {
            var engine = new GameEngine();
            PlayerRoster.Apply(engine.Character, selectedPlayer);
            var ledger = new EconomyLedger(engine.Farm, engine.Clock);

            // 1. Tạo đàn heo và chăm sóc buổi sáng
            var pigToSell = Pig.CreateDefault("vs_pig_1", "Heo Thương Phẩm", GeneLineId.HongDien, GeneRarity.Thuong);
            pigToSell.Stage = PigStage.TruongThanh;
            pigToSell.WeightKg = 105f;
            pigToSell.MeatQuality = MeatQuality.A;
            var pigStay = Pig.CreateDefault("vs_pig_2", "Heo Giống Lam Khê", GeneLineId.LamKhe, GeneRarity.Thuong);
            engine.Pigs.Add(pigToSell);
            engine.Pigs.Add(pigStay);

            // 2. Bán heo ở chợ ngày qua Sổ cái Ledger
            var (salePrice, _) = engine.Economy.CalculatePigSalePrice(pigToSell, engine.Farm.FarmLevel);
            engine.Pigs.Remove(pigToSell);
            ledger.Earn(salePrice, LedgerReason.SellPig, pigToSell.Id);

            // 3. Xây & nạp trước đạn cho 1 công trình phòng thủ (Nỏ Xuyên Vân)
            var (_, _, tower) = engine.Defense.ConstructBuilding("NoXuyenVan");
            tower!.AmmoCurrent = 30;
            var preloadRes = engine.Defense.PreloadAmmo(tower.InstanceId, 20, isDuringWave: false);

            // 4. Đợt quái nhỏ buổi tối (đồng hồ ngày tạm dừng trong đợt quái)
            engine.Clock.IsDuringCombatWave = true;
            int minBeforeWave = engine.Clock.CurrentMinute;
            engine.Clock.TickMinutes(10); // Không tăng phút khi đang đợt quái
            bool clockPausedProperly = engine.Clock.CurrentMinute == minBeforeWave;

            var raider = EnemyData.CreateKeBatHeo(engine.Clock.CurrentDay);
            raider.TakeDamage(200f, engine.Character.BaseStats.Str, 0);
            bool waveDefeated = !raider.IsAlive;
            engine.Clock.IsDuringCombatWave = false;
            engine.MinorWavesDefeated += 1;

            // 5. Sang ngày mới & kiểm tra Lưu/Tải 3 vòng liên tiếp
            engine.Tick(24 * 60);
            var (identical, h1, _, _) = VerifyThreeConsecutiveSaveLoadCycles(engine);

            bool ok = preloadRes.success && clockPausedProperly && waveDefeated && identical && engine.Farm.Gold > 0;
            return (ok, engine.Farm.Gold, h1);
        }

        /// <summary>
        /// P5.03 (Người D): Công cụ mô phỏng kinh tế headless 30–60 ngày với seed cố định (Determinism).
        /// </summary>
        public static (bool success, int finalGold, int wavesSurvived, string csvReport, string determinismHash) RunHeadlessEconomySimulation(int days, int seed)
        {
            int simDays = Math.Clamp(days, 1, 120);
            var rng = new Random(seed);
            var engine = new GameEngine(seed);
            var ledger = new EconomyLedger(engine.Farm, engine.Clock);

            for (int i = 0; i < 12; i++)
            {
                var p = Pig.CreateDefault($"sim_pig_{i + 1}", $"Heo #{i + 1}", i % 2 == 0 ? GeneLineId.HongDien : GeneLineId.LamKhe, GeneRarity.Thuong);
                p.Gender = i % 2 == 0 ? "Cai" : "Duc";
                p.Stage = PigStage.TruongThanh;
                p.AgeDays = 14;
                p.WeightKg = 82f;
                engine.Pigs.Add(p);
            }

            var sb = new StringBuilder();
            sb.AppendLine("Day,Gold,Piglets,Growing,Adults,Elders,DensityRatio,HabitatIndex,EventPressure,WaveResult");

            int wavesSurvived = 0;
            for (int d = 1; d <= simDays; d++)
            {
                if (d % 7 == 1)
                {
                    var theme = EconomyAndIntelExpansionSystem.RollMarketCycleThemeWeighted(rng);
                    engine.Economy.ApplyMarketCycleTheme(theme);
                }

                // Mua thêm cám nếu kho dưới 120kg
                if (engine.Farm.FeedStockKg < 120f && ledger.TrySpend(180, LedgerReason.BuyFeed, $"day_{d}_feed"))
                {
                    engine.Farm.FeedStockKg += 150f;
                }

                // Bán bớt heo trưởng thành đạt đỉnh cân nặng
                var readyToSell = engine.Pigs.Find(p => p.IsAlive && p.Stage == PigStage.TruongThanh && p.WeightKg >= 95f);
                if (readyToSell != null && engine.Pigs.FindAll(p => p.IsAlive).Count > 8)
                {
                    var (price, _) = engine.Economy.CalculatePigSalePrice(readyToSell, engine.Farm.FarmLevel);
                    engine.Pigs.Remove(readyToSell);
                    ledger.Earn(price, LedgerReason.SellPig, readyToSell.Id);
                    FarmLevelProgressionSystem.AddFarmExp(engine.Farm, 35);
                }

                string waveTag = "None";
                if (d % 6 == 0)
                {
                    wavesSurvived++;
                    engine.MajorWavesSurvived = wavesSurvived;
                    ledger.Earn(220, LedgerReason.WaveReward, $"wave_{wavesSurvived}");
                    waveTag = $"Survived_Wave_{wavesSurvived}";
                }

                engine.Tick(24 * 60);
                sb.AppendLine(BuildDailyTelemetryCsvRow(engine, waveTag));

                if (engine.Farm.Gold < 0 || float.IsNaN(engine.Farm.FeedStockKg))
                {
                    return (false, engine.Farm.Gold, wavesSurvived, sb.ToString(), "INVALID");
                }
            }

            string csv = sb.ToString();
            string hash = ComputeDeterministicHash(csv);
            return (true, engine.Farm.Gold, wavesSurvived, csv, hash);
        }
    }
}
