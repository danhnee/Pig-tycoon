using System;
using System.Collections.Generic;

namespace PigTycoon.Core
{
    [Serializable]
    public class BaseStats
    {
        public int Str;
        public int Agi;
        public int Ctrl;
        public int Res;
        public int Luk = 0;
    }

    [Serializable]
    public class BonusStats
    {
        public int Str = 0;
        public int Agi = 0;
        public int Ctrl = 0;
        public int Res = 0;
        public int Luk = 0;
    }

    [Serializable]
    public class StaminaSystem
    {
        public float CurrentStamina = 180f;
        public float MaxStamina = 180f;
        public float DailyLoad = 0f;
        public int Condition = 100;
        public int Immunity = 100;
        public int ReboundFatigueStacks = 0; // Mỗi tầng giảm 8% MaxStamina (GDD 9.5)

        /// <summary>
        /// Trần thể lực sau khi trừ phạt Mệt Dội (Rebound Fatigue)
        /// </summary>
        public float BaseMaxStamina => Math.Max(0f, MaxStamina * (1f - Math.Clamp(ReboundFatigueStacks, 0, 4) * 0.08f));

        /// <summary>
        /// Giới hạn thể lực hiệu dụng tức thời: Trần thể lực trừ đi Tải ngày (Daily Load)
        /// </summary>
        public float EffectiveMaxStamina => Math.Max(0f, BaseMaxStamina - DailyLoad);

        /// <summary>
        /// Vùng mệt mỏi theo GDD 9.4: CurrentStamina &lt; 20% MaxStamina.
        /// Chạy -15%, đòn nặng / combo trễ +20%, không khóa hành động.
        /// </summary>
        public bool IsFatigued => CurrentStamina < (MaxStamina * 0.2f) || IsExhausted;

        /// <summary>
        /// Tỷ lệ thể lực tức thời so với MaxStamina (0.0 đến 1.0)
        /// </summary>
        public float StaminaRatio => MaxStamina > 0f ? Math.Clamp(CurrentStamina / MaxStamina, 0f, 1f) : 0f;

        /// <summary>
        /// Tỷ lệ Tải ngày so với MaxStamina (0.0 đến 1.0)
        /// </summary>
        public float DailyLoadRatio => MaxStamina > 0f ? Math.Clamp(DailyLoad / MaxStamina, 0f, 1f) : 0f;

        public bool CanAfford(float cost) => CurrentStamina >= cost;

        /// <summary>
        /// Tiêu hao thể lực theo cơ chế 2 lớp:
        /// - Trừ tức thời vào CurrentStamina
        /// - Tích lũy vào DailyLoad theo tỷ lệ quy định (GDD 9.2: combat 30-40%, lướt 30%)
        /// </summary>
        public bool TryConsumeStamina(float amount, float dailyLoadRatio = 0.3f)
        {
            if (amount <= 0f) return true;
            if (CurrentStamina < amount) return false;

            CurrentStamina -= amount;
            DailyLoad = Math.Min(MaxStamina, DailyLoad + (amount * dailyLoadRatio));

            // Đảm bảo CurrentStamina không vượt quá EffectiveMaxStamina khi DailyLoad tăng
            if (CurrentStamina > EffectiveMaxStamina)
            {
                CurrentStamina = EffectiveMaxStamina;
            }

            return true;
        }

        /// <summary>
        /// Tiêu hao cưỡng bức (cho phép cạn về 0)
        /// </summary>
        public void ForceConsumeStamina(float amount, float dailyLoadRatio = 0.3f)
        {
            if (amount <= 0f) return;
            float actualCost = Math.Min(CurrentStamina, amount);
            CurrentStamina = Math.Max(0f, CurrentStamina - amount);
            DailyLoad = Math.Min(MaxStamina, DailyLoad + (actualCost * dailyLoadRatio));

            if (CurrentStamina > EffectiveMaxStamina)
            {
                CurrentStamina = EffectiveMaxStamina;
            }
        }

        /// <summary>
        /// Hồi phục Thể lực tức thời đến trần EffectiveMaxStamina (Tải ngày không tự hồi - GDD 9.4)
        /// </summary>
        public void RegenerateStamina(float amount)
        {
            if (amount <= 0f) return;
            float cap = EffectiveMaxStamina;
            CurrentStamina = Math.Min(cap, CurrentStamina + amount);
        }

        /// <summary>
        /// Ngủ chuẩn theo GDD 9.5: Xóa sạch Tải ngày, khôi phục Stamina >= 80% tối đa, phục hồi Condition/Immunity
        /// </summary>
        public void PerformSleepReset(int bonusCondition = 0)
        {
            DailyLoad = 0f;
            float baseCap = BaseMaxStamina;
            CurrentStamina = Math.Max(CurrentStamina, baseCap * 0.8f);
            // GDD 9.5: Thể trạng +6 (khoảng +4..+10); mỗi tầng Mệt Dội làm hồi Thể trạng −15%.
            int stacks = Math.Clamp(ReboundFatigueStacks, 0, MaxReboundFatigueStacks);
            int conditionGain = (int)Math.Round((SleepConditionGain + Math.Max(0, bonusCondition)) * (1f - stacks * ReboundConditionRecoveryPenalty));
            Condition = Math.Min(100, Condition + Math.Max(0, conditionGain));
            Immunity = Math.Min(100, Immunity + 5);
            IsExhausted = false;
        }

        // ---------------------------------------------------------------- GDD 9.4 / 9.5 (hồi phục, quá sức, Mệt Dội)
        public const int SleepConditionGain = 6;                    // GDD 9.5: +4..+10
        public const int MaxReboundFatigueStacks = 4;               // GDD 9.5
        public const float ReboundConditionRecoveryPenalty = 0.15f; // GDD 9.5: hồi Thể trạng −15%/tầng
        public const float RestStaminaPerGameHour = 6f;             // GDD 9.4: ngồi nghỉ 6/giờ game
        public const float NormalMealStamina = 20f;                 // GDD 9.4: bữa thường +20
        public const float GoodMealStamina = 35f;                   // GDD 9.4: bữa tốt +35
        public const int MaxMealsPerDay = 3;                        // GDD 9.4
        public const float OverexertionRatio = 0.55f;               // GDD 9.4: Tải ngày > 55%
        public const float HeavyOverexertionRatio = 0.75f;          // GDD 9.4: Tải ngày > 75%
        public const int OverexertionConditionLoss = 8;             // GDD 9.4
        public const int HeavyOverexertionConditionLoss = 15;       // GDD 9.4
        public const double ExhaustedChance = 0.20;                 // GDD 9.4: "20% Kiệt Sức"

        /// <summary>Kiệt Sức (GDD 9.4, tải ngày &gt; 75%): tính như Vùng mệt tới lần ngủ chuẩn kế tiếp (ADR-0018, PROVISIONAL).</summary>
        public bool IsExhausted = false;
        /// <summary>Số bữa đã ăn trong ngày nông trại hiện tại (tối đa 3, GDD 9.4).</summary>
        public int MealsToday = 0;
        /// <summary>Game minute of the last meal (GDD 9.3 "Đói": not eaten > 8 h → +10 % cost, > 16 h → +25 %).
        /// 0 = the start of the game, which counts as a meal.</summary>
        public long LastMealMinute = 0;
        public const int HungryAfterMinutes = 8 * 60, StarvingAfterMinutes = 16 * 60;
        public const float HungryCostFactor = 0.10f, StarvingCostFactor = 0.25f;
        public float HungerCostFactor(long nowMinute)
        {
            long since = nowMinute - LastMealMinute;
            return since > StarvingAfterMinutes ? StarvingCostFactor : since > HungryAfterMinutes ? HungryCostFactor : 0f;
        }
        /// <summary>Đã nghỉ cưỡng bức bằng vật phẩm trong ngày: ngày đó không phải "ngày sạch" (ADR-0018).</summary>
        public bool ForcedRestToday = false;

        /// <summary>
        /// Nghỉ dài / ăn (GDD 9.1, 9.4): phần hồi trả Tải ngày trước rồi nâng Thể lực hiện tại, không vượt trần hiệu dụng.
        /// </summary>
        /// <summary>Set by CharacterData.SyncLowGuardStamina: 0,75 while Chống Chịu &lt; 30% (người dùng chốt), else 1.</summary>
        public float LowGuardCapMultiplier = 1f;

        public float RecoverLoadAndStamina(float amount)
        {
            if (amount <= 0f) return 0f;
            float before = CurrentStamina;
            DailyLoad = Math.Max(0f, DailyLoad - amount);
            CurrentStamina = Math.Min(EffectiveMaxStamina * LowGuardCapMultiplier, CurrentStamina + amount);
            return Math.Max(0f, CurrentStamina - before);
        }

        /// <summary>Ngồi nghỉ: 6 Thể lực mỗi giờ game (GDD 9.4).</summary>
        public float RestForGameHours(int hours) => hours <= 0 ? 0f : RecoverLoadAndStamina(hours * RestStaminaPerGameHour);

        /// <summary>Ăn một bữa: thường +20, tốt +35; tối đa 3 bữa/ngày (GDD 9.4).</summary>
        public bool TryEatMeal(bool goodMeal, out float recovered)
        {
            recovered = 0f;
            if (MealsToday >= MaxMealsPerDay) return false;
            MealsToday++;
            recovered = RecoverLoadAndStamina(goodMeal ? GoodMealStamina : NormalMealStamina);
            return true;
        }

        /// <summary>
        /// Chốt cuối ngày (GDD 9.4 Quá sức, 9.5 Mệt Dội): Tải ngày &gt; 55% → Thể trạng −8; &gt; 75% → −15 và 20% Kiệt Sức.
        /// Ngày sạch (không quá sức, không nghỉ cưỡng bức) giảm 1 tầng Mệt Dội. Tải ngày KHÔNG bị xóa ở đây:
        /// nó chỉ hồi bằng ăn, nghỉ dài hoặc ngủ (GDD 9.1). <paramref name="roll"/> chỉ được gọi khi tải &gt; 75%.
        /// </summary>
        public (int conditionLost, bool becameExhausted, bool cleanDay) EvaluateEndOfDay(Func<double>? roll)
        {
            float ratio = DailyLoadRatio;
            int loss = 0;
            bool exhausted = false;
            if (ratio > HeavyOverexertionRatio)
            {
                loss = HeavyOverexertionConditionLoss;
                exhausted = roll != null && roll() < ExhaustedChance;
            }
            else if (ratio > OverexertionRatio)
            {
                loss = OverexertionConditionLoss;
            }
            Condition = Math.Max(0, Condition - loss);
            if (exhausted) IsExhausted = true;
            bool clean = loss == 0 && !ForcedRestToday;
            if (clean && ReboundFatigueStacks > 0) ReboundFatigueStacks -= 1;
            ForcedRestToday = false;
            MealsToday = 0;
            return (loss, exhausted, clean);
        }
    }

    public enum EquipSlot
    {
        Weapon,
        Boots,
        BodyArmor,
        Pants,
        Bracelet,
        Necklace
    }

    [Serializable]
    public class EquipmentItem
    {
        public string Id = string.Empty;
        public string Name = string.Empty;
        public EquipSlot Slot;
        public int WeightScore = 1; // 1=Nhẹ, 2=Vừa, 3=Nặng
        public int Defense;
        public int BonusStr;
        public int BonusAgi;
        public int BonusCtrl;
        public int BonusRes;
        public string SpecialMechanic = string.Empty;
        /// <summary>GDD 12.3 weapon line: archetype, damage per hit and interval ("14 • 0,5 giây").</summary>
        public WeaponArchetype Archetype = WeaponArchetype.None;
        public float BaseDamage;
        public float AttackInterval;
        /// <summary>One line of what the item does, shown in the character panel (GDD 12.3 "Cơ chế riêng").</summary>
        public string MechanicText = string.Empty;
        /// <summary>GDD 12.3 quality I–IV. Raises only the item's bonus stat (+1 / +3 / +5 / +8); "Thủ" and damage stay.</summary>
        public ItemTier Quality = ItemTier.I;
        /// <summary>Catalog id without the quality suffix.</summary>
        public string BaseId => EquipmentQuality.BaseId(Id);

        /// <summary>A copy of this catalog piece at another quality (id "wpn_dao_rua#III").</summary>
        public EquipmentItem WithQuality(ItemTier q)
        {
            var c = (EquipmentItem)MemberwiseClone();
            c.Quality = q;
            c.Id = EquipmentQuality.QualifiedId(BaseId, q);
            c.Name = q == ItemTier.I ? Name : $"{Name} {q}";
            int bonus = EquipmentQuality.BonusFor(q);
            if (BonusStr > 0) c.BonusStr = bonus;
            if (BonusAgi > 0) c.BonusAgi = bonus;
            if (BonusCtrl > 0) c.BonusCtrl = bonus;
            if (BonusRes > 0) c.BonusRes = bonus;
            return c;
        }
    }

    /// <summary>
    /// GDD 12.3: "Phẩm chất I–IV chỉ tăng độ mạnh cơ chế và chỉ số thưởng (+1 / +3 / +5 / +8)". Owned gear keeps its
    /// quality in the id ("wpn_dao_rua#III"), so saves and the owned list stay plain strings; quality I has no suffix.
    /// The GDD gives no numbers for "độ mạnh cơ chế" per quality, so mechanics stay as at quality I for now.
    /// </summary>
    public static class EquipmentQuality
    {
        public const char Separator = '#';
        public static int BonusFor(ItemTier q) => q switch { ItemTier.II => 3, ItemTier.III => 5, ItemTier.IV => 8, _ => 1 };
        public static string QualifiedId(string baseId, ItemTier q) => q == ItemTier.I ? baseId : baseId + Separator + q;
        public static string BaseId(string id) { int i = (id ?? "").IndexOf(Separator); return i < 0 ? id ?? "" : id!.Substring(0, i); }
        public static ItemTier QualityOf(string id)
        {
            int i = (id ?? "").IndexOf(Separator);
            return i >= 0 && Enum.TryParse(id!.Substring(i + 1), out ItemTier q) ? q : ItemTier.I;
        }
    }

    [Serializable]
    public class CombatHitResolution
    {
        public float RawDamage;
        public float MitigatedDamage;
        public float StaminaDrained;
        public float GuardDrained;
        public float CounterDamage;
        public int RageGained;
        public bool WasActiveParry;
        public bool WasAutoGuarded;
        public bool TriggeredAnBatKhuat;
        public bool CausedFaint;
        public string FaintReason = string.Empty;
        public string SummaryMessage = string.Empty;
    }

    [Serializable]
    public class CharacterData
    {
        public string Id = "";
        public string Name = "";
        public PlayerId Identity { get; private set; }
        public string BasicTraitId = "";
        public int Level = 1;
        public int Exp = 0;

        public BaseStats BaseStats = new BaseStats();
        public BonusStats BonusStats = new BonusStats();

        public int Hp = 100;
        public int MaxHp = 100;
        public int Rage = 0; // Nộ khí 0 - 100

        /// <summary>
        /// Thanh Chống Chịu (Guard / Poise / Resilience Gauge):
        /// Nhân vật mặc định tự đỡ đòn của quái (Auto-Guard), làm hao cả Thể Lực lẫn Thanh Chống Chịu.
        /// Nếu đỡ đòn chủ động (Active Parry) đúng nhịp sẽ giảm mạnh mức hao Thanh Chống Chịu và Thể Lực.
        /// Khi Thanh Chống Chịu về 0 (dù còn Stamina) HOẶC Stamina về 0 (dù còn Chống Chịu), nhân vật lập tức bị ngất và được kéo về trại hồi sức.
        /// </summary>
        public float CurrentGuard = 140f;
        public float MaxGuard = 140f;
        public bool AutoGuardEnabled = true;
        public bool IsFainted = false;
        public int FaintCount = 0;
        public string LastFaintReason = string.Empty;

        public int MartialPoints = 1;
        public List<string> EngravedRunes = new List<string>(); // Tối đa 6 Phù Văn (trang bị Bậc II+)
        public List<string> EngravedSeals = new List<string>(); // Tối đa 3 Mệnh Ấn (-10 MaxGuard/MaxHp mỗi ấn)
        public bool AnBatKhuatTriggeredThisWave = false;

        public StaminaSystem Stamina = new StaminaSystem();

        [NonSerialized]
        public Dictionary<EquipSlot, EquipmentItem?> Equipment = new Dictionary<EquipSlot, EquipmentItem?>();
        public MartialSchool PrimarySchool = MartialSchool.TrongKich_B;

        [NonSerialized]
        public Dictionary<CombatSlotType, string> ActiveMartialSkills = new Dictionary<CombatSlotType, string>();

        /// <summary>GDD 8.2 / 12.6: skills learned (paid gold + Điểm Võ), equipment the player owns, unspent stat points.</summary>
        public List<string> LearnedSkills = new List<string>();
        public List<string> OwnedEquipment = new List<string>();
        public int StatPoints;
        /// <summary>Bí Kíp held (needed for ultimates and other-school skills, GDD 12.6).</summary>
        public int SecretManuals;
        /// <summary>Chu kỳ lần cuối đổi phái chính (GDD 12.6: 1 lần / 3 chu kỳ); 0 = chưa đổi.</summary>
        public int LastSchoolChangeCycle;
        /// <summary>GDD 12.5 túi nhanh (QuickBag): loại → số lượng.</summary>
        [NonSerialized]
        public Dictionary<QuickItemId, int> QuickItems = new Dictionary<QuickItemId, int>();
        /// <summary>Normal kills not yet turned into Điểm Võ (+1 per 20, GDD 12.6).</summary>
        public int NormalKillsTowardMartialPoint;
        /// <summary>Labor stamina not yet turned into EXP (1 EXP per 10 Stamina, GDD 8.2).</summary>
        public float LaborStaminaTowardExp;
        /// <summary>Runtime clock (seconds) at which each skill is ready again; not saved.</summary>
        [NonSerialized]
        public Dictionary<string, double> SkillReadyAt = new Dictionary<string, double>(); // per skill id (GDD 12.7 "Hồi")
        [NonSerialized]
        public string LastCastSkillId = string.Empty;
        [NonSerialized]
        public double LastCastAt = -999;

        public CharacterData() : this(PlayerId.Khoa)
        {
        }

        public CharacterData(PlayerId identity)
        {
            foreach (EquipSlot slot in Enum.GetValues(typeof(EquipSlot)))
            {
                Equipment[slot] = null;
            }

            // GDD 8: no innate skills; slots stay empty until a skill is learned and slotted.
            ActiveMartialSkills[CombatSlotType.TheCong] = "";
            ActiveMartialSkills[CombatSlotType.TheThu] = "";
            ActiveMartialSkills[CombatSlotType.TheBien] = "";
            ActiveMartialSkills[CombatSlotType.TheSongThe] = "";
            PlayerRoster.Apply(this, identity);
            RecalculateBonusStats();
            CurrentGuard = MaxGuard;
        }

        public void ApplyIdentity(PlayerId identity, string name, string dataId, string traitId, int str, int agi, int ctrl, int res)
        {
            Identity = identity;
            Name = name;
            Id = dataId;
            BasicTraitId = traitId;
            BaseStats.Str = str;
            BaseStats.Agi = agi;
            BaseStats.Ctrl = ctrl;
            BaseStats.Res = res;
            PrimarySchool = identity == PlayerId.Khoa ? MartialSchool.TrongKich_B : MartialSchool.KhongChe_C;
            RecalculateBonusStats();
            CurrentGuard = MaxGuard;
        }

        /// <summary>
        /// New-game kit (ADR-0017, PROVISIONAL: the GDD names no starting gear): the light Dao Rựa only,
        /// so no skill slot is open until the player buys the matching pair piece.
        /// </summary>
        public void EquipStarterSet()
        {
            var knife = EquipmentAndTacticalCatalog.Find("wpn_dao_rua");
            if (knife == null) return;
            if (!OwnedEquipment.Contains(knife.Id)) OwnedEquipment.Add(knife.Id);
            EquipItem(knife);
        }

        public int GetTotalWeightScore()
        {
            int totalWeight = 0;
            foreach (var kvp in Equipment)
            {
                if (kvp.Value != null) totalWeight += kvp.Value.WeightScore;
                else totalWeight += 1;
            }
            return totalWeight;
        }

        public string GetPlaystyle()
        {
            int totalWeight = GetTotalWeightScore();
            if (totalWeight <= 9) return "KhinhThân";
            if (totalWeight <= 13) return "CânBằng";
            return "TrọngGiáp";
        }

        public int GetTotalEquipmentDefense()
        {
            int def = 0;
            foreach (var kvp in Equipment)
            {
                if (kvp.Value != null) def += kvp.Value.Defense;
            }
            if (EngravedRunes.Contains("PhuVan_ThietGiap")) def += 12;
            return def;
        }

        /// <summary>
        /// Kiểm tra cặp trang bị mở khóa ô kỹ năng võ học (GDD 12.2):
        /// - Vũ khí + Giày -> Thế Công
        /// - Áo + Quần -> Thế Thủ
        /// - Vòng tay + Dây chuyền -> Thế Biến
        /// </summary>
        public bool IsMartialSlotUnlocked(CombatSlotType slot)
        {
            return slot switch
            {
                CombatSlotType.TheCong => Equipment.TryGetValue(EquipSlot.Weapon, out var w) && w != null &&
                                          Equipment.TryGetValue(EquipSlot.Boots, out var b) && b != null,
                CombatSlotType.TheThu => Equipment.TryGetValue(EquipSlot.BodyArmor, out var a) && a != null &&
                                         Equipment.TryGetValue(EquipSlot.Pants, out var p) && p != null,
                CombatSlotType.TheBien => Equipment.TryGetValue(EquipSlot.Bracelet, out var br) && br != null &&
                                          Equipment.TryGetValue(EquipSlot.Necklace, out var n) && n != null,
                CombatSlotType.TheSongThe => EngravedSeals.Contains("AnSongThe"),
                _ => false
            };
        }

        public bool EquipItem(EquipmentItem item)
        {
            Equipment[item.Slot] = item;
            RecalculateBonusStats();
            return true;
        }

        /// <summary>GDD 8.3: chỉ số gốc + thưởng tối đa 55.</summary>
        public const int StatHardCap = 55;

        public void RecalculateBonusStats()
        {
            int s = 0, a = 0, c = 0, r = 0;
            foreach (var kvp in Equipment)
            {
                if (kvp.Value != null)
                {
                    s += kvp.Value.BonusStr;
                    a += kvp.Value.BonusAgi;
                    c += kvp.Value.BonusCtrl;
                    r += kvp.Value.BonusRes;
                }
            }

            // Trần riêng tối đa +8 mỗi chỉ số thưởng (GDD 8.3)
            BonusStats.Str = Math.Min(8, s);
            BonusStats.Agi = Math.Min(8, a);
            BonusStats.Ctrl = Math.Min(8, c);
            BonusStats.Res = Math.Min(8, r);
            // GDD 8.3: base + bonus never above the hard cap 55
            BonusStats.Str = Math.Max(0, Math.Min(BonusStats.Str, StatHardCap - BaseStats.Str));
            BonusStats.Agi = Math.Max(0, Math.Min(BonusStats.Agi, StatHardCap - BaseStats.Agi));
            BonusStats.Ctrl = Math.Max(0, Math.Min(BonusStats.Ctrl, StatHardCap - BaseStats.Ctrl));
            BonusStats.Res = Math.Max(0, Math.Min(BonusStats.Res, StatHardCap - BaseStats.Res));

            int totalRes = BaseStats.Res + BonusStats.Res;
            int sealPenalty = EngravedSeals.Count * 10;
            // GDD 8.2: HP 100, +3 per level, soft cap 140 / hard cap 180; each Mệnh Ấn −10 permanently.
            MaxHp = Math.Max(40, Math.Min(180, 100 + 3 * (Level - 1)) - sealPenalty);
            if (Hp > MaxHp) Hp = MaxHp;

            // Không có HP: "+3 HP mỗi cấp" và "Mệnh Ấn −10 HP tối đa" của GDD áp lên thanh Chống Chịu.
            float newMaxGuard = Math.Max(50f, 100f + totalRes * 2.5f + 3f * (Level - 1) - sealPenalty);
            if (GetPlaystyle() == "TrọngGiáp") newMaxGuard += 25f;
            MaxGuard = newMaxGuard;
            if (CurrentGuard > MaxGuard) CurrentGuard = MaxGuard;
        }

        /// <summary>GDD 9.1/9.5: one source of truth with the regen cap (Mệt Dội −8%/stack, minus Tải ngày).</summary>
        /// <summary>
        /// Thiết kế người dùng: Chống Chịu dưới 30% thì Thể lực bị giảm và khả năng hồi Thể lực cũng giảm.
        /// Tỉ lệ do người dùng chốt 02/11: tổng Thể lực −25%, tốc độ hồi còn 6/10.
        /// </summary>
        public const float LowGuardThreshold = 0.30f;
        public static float LowGuardStaminaPenalty = 0.25f; // người dùng 02/11: tổng bình Thể lực −25%
        public static float LowGuardRegenPenalty = 0.40f; // người dùng 02/11: tốc độ hồi chỉ còn 6/10

        public bool IsGuardLow => MaxGuard > 0f && CurrentGuard < MaxGuard * LowGuardThreshold;

        /// <summary>True while the stamina tank is shrunk by the low-guard penalty (saved state is already scaled).</summary>
        public bool LowGuardStaminaScaled;

        /// <summary>
        /// Người dùng: "giảm tổng thanh" là thu nhỏ cả bình theo tỉ lệ — 140/180 thành 105/135 khi Chống Chịu xuống dưới 30%,
        /// và trở lại 140/180 khi Chống Chịu hồi lên trên 30%. Call after any change to Chống Chịu.
        /// </summary>
        public void SyncLowGuardStamina()
        {
            bool low = IsGuardLow;
            Stamina.LowGuardCapMultiplier = low ? 1f - LowGuardStaminaPenalty : 1f;
            if (low == LowGuardStaminaScaled) return;
            float k = 1f - LowGuardStaminaPenalty;
            Stamina.CurrentStamina = low ? Stamina.CurrentStamina * k : Math.Min(Stamina.EffectiveMaxStamina, Stamina.CurrentStamina / k);
            LowGuardStaminaScaled = low;
        }

        public float GetEffectiveMaxStamina() => Stamina.EffectiveMaxStamina * (IsGuardLow ? 1f - LowGuardStaminaPenalty : 1f);

        /// <summary>GDD 9.4: under 20% Stamina, or Kiệt Sức, runs 15% slower and swings slower.</summary>
        public bool IsFatigued() => Stamina.IsFatigued;

        public float GetMoveSpeedMultiplier()
        {
            float mult = 1.0f;
            string style = GetPlaystyle();
            if (style == "KhinhThân") mult += 0.08f;
            else if (style == "TrọngGiáp") mult -= 0.10f;

            if (IsFatigued()) mult *= 0.85f; // GDD 9.4: < 20% Stamina giảm 15% tốc chạy
            return mult;
        }

        /// <summary>
        /// Tiêu hao Thể lực chiến đấu 2 lớp (Tức thời + Tích tải trong ngày) theo GDD 9.2 & 12.2.
        /// Nếu thể lực về 0, nhân vật lập tức bị ngất (IsFainted = true).
        /// </summary>
        public bool TryConsumeCombatStamina(float baseCost, float dailyLoadRatio, out float actualCost, bool isDodgeOrParry = false)
        {
            float mult = 1.0f;
            string style = GetPlaystyle();
            if (isDodgeOrParry)
            {
                if (style == "KhinhThân") mult *= 0.85f;
                else if (style == "TrọngGiáp") mult *= 1.25f;
            }
            if (EngravedRunes.Contains("PhuVan_TinhKhi")) mult *= 0.90f;

            actualCost = (float)Math.Round(Math.Max(0.5f, baseCost * mult), 1);
            Stamina.CurrentStamina = Math.Max(0f, Stamina.CurrentStamina - actualCost);
            Stamina.DailyLoad = Math.Min(Stamina.MaxStamina * 0.65f, Stamina.DailyLoad + actualCost * dailyLoadRatio);

            // Hết Thể lực chỉ khiến không đủ sức ra đòn; ngất là do vỡ thanh Chống Chịu (thiết kế người dùng).
            return !IsFainted;
        }

        /// <summary>
        /// Đòn của quái vào người chơi. Theo thiết kế người dùng chốt: nhân vật KHÔNG có thanh máu; thanh sống còn là
        /// Thanh Chống Chịu. Bị đánh trúng thì vừa trừ Chống Chịu vừa trừ Thể Lực (ít sức để hành động hơn); Chống Chịu
        /// hoặc Thể Lực về 0 thì ngất và được kéo về hồi sức.
        /// - Sát thương sau giảm trừ = Gốc × 100/(100 + Phòng thủ) × (1 − RES gốc × 0,5%) (GDD 12.3) × Khinh Thân 1,1
        ///   × Kim Cương Thể × Phù Kiên Thạch; khiên Hộ Thân đỡ trước (ở runtime).
        /// - Đỡ chủ động đúng lúc: mất rất ít Chống Chịu, tốn Thể Lực đỡ (Quần Giáp Xích hoàn 2), +1 Nộ (GDD 12.1).
        /// - Tự đỡ (mặc định) hoặc giữ thế thủ: trừ cả Thể Lực lẫn Chống Chịu.
        /// </summary>
        public CombatHitResolution ProcessIncomingAttack(float rawEnemyDamage, bool isActiveParry, bool isHoldingGuardStance = false, float staminaDrainRatio = 0f)
        {
            var result = new CombatHitResolution { RawDamage = rawEnemyDamage };

            int totalDef = GetTotalEquipmentDefense();
            float defFactor = 100f / (100f + Math.Max(0, totalDef));
            float resFactor = Math.Max(0.40f, 1.0f - BaseStats.Res * 0.005f);
            float styleFactor = GetPlaystyle() == "KhinhThân" ? 1.10f : 1.0f;
            float skillFactor = DamageTakenMultiplier();
            if (EngravedRunes.Contains("PhuKienThach")) skillFactor *= 0.90f; // Phù Kiên Thạch (GDD 12.4): đỡ đòn giảm thêm 10%

            float mitigated = Math.Max(1.0f, (float)Math.Round(rawEnemyDamage * defFactor * resFactor * styleFactor * skillFactor, 1));
            result.MitigatedDamage = mitigated;

            bool hasQuanGiapXich = Equipment.TryGetValue(EquipSlot.Pants, out var pants) && pants != null && pants.SpecialMechanic == "QuanGiapXich";

            if (isActiveParry)
            {
                result.WasActiveParry = true;
                float staDelta = hasQuanGiapXich ? -2.0f : 4.0f; // Quần Giáp Xích hoàn +2 STA khi đỡ đúng nhịp (GDD 12.3)
                if (staDelta > 0f)
                {
                    Stamina.CurrentStamina = Math.Max(0f, Stamina.CurrentStamina - staDelta);
                    Stamina.DailyLoad = Math.Min(Stamina.MaxStamina * 0.65f, Stamina.DailyLoad + staDelta * 0.25f);
                }
                else Stamina.CurrentStamina = Math.Min(GetEffectiveMaxStamina(), Stamina.CurrentStamina + Math.Abs(staDelta));
                result.StaminaDrained = staDelta;

                float guardLoss = (float)Math.Round(Math.Max(1.0f, mitigated * 0.18f), 1);
                CurrentGuard = Math.Max(0f, CurrentGuard - guardLoss);
                result.GuardDrained = guardLoss;
                result.CounterDamage = 0f; // phản kích là việc của kỹ năng (Phản Chấn), không tự có khi đỡ
                Rage = Math.Min(100, Rage + 1); // GDD 12.1: +1 Nộ mỗi lần đỡ đòn thành công
                result.RageGained = 1;
                result.SummaryMessage = $"ĐỠ CHỦ ĐỘNG! (−{guardLoss:0.0} Chống Chịu, +1 Nộ)";
            }
            else
            {
                result.WasAutoGuarded = true;
                float stanceStaMult = isHoldingGuardStance ? 0.42f : 0.65f;
                float stanceGuardMult = isHoldingGuardStance ? 0.50f : 0.90f;
                // Chống Chịu khác Thể lực: đòn thường chỉ trừ Chống Chịu; chỉ đòn của quái có thuộc tính rút sức mới trừ Thể lực.
                float staLoss = staminaDrainRatio > 0f ? (float)Math.Round(Math.Max(1.5f, mitigated * staminaDrainRatio * (stanceStaMult / 0.65f)), 1) : 0f;
                float guardLoss = (float)Math.Round(Math.Max(2.0f, mitigated * stanceGuardMult), 1);
                if (staLoss > 0f)
                {
                    Stamina.CurrentStamina = Math.Max(0f, Stamina.CurrentStamina - staLoss);
                    Stamina.DailyLoad = Math.Min(Stamina.MaxStamina * 0.65f, Stamina.DailyLoad + staLoss * 0.30f);
                }
                CurrentGuard = Math.Max(0f, CurrentGuard - guardLoss);
                result.StaminaDrained = staLoss;
                result.GuardDrained = guardLoss;
                result.SummaryMessage = staLoss > 0f ? $"−{guardLoss:0} Chống Chịu, −{staLoss:0} Thể Lực" : $"−{guardLoss:0} Chống Chịu";
            }

            SyncLowGuardStamina();
            if (CurrentGuard <= 0f)
            {
                string reason = "Vỡ Thanh Chống Chịu";
                if (TriggerFaintIfExhaustedOrGuardBroken(reason))
                {
                    result.CausedFaint = true;
                    result.FaintReason = LastFaintReason;
                    result.SummaryMessage = $"NGẤT ({LastFaintReason})! Được kéo về hiên nhà chính hồi sức.";
                }
                else
                {
                    result.TriggeredAnBatKhuat = true;
                    result.SummaryMessage = "ẤN BẤT KHUẤT! Giữ lại 15 Chống Chịu & Thể Lực, miễn sát thương 2 giây.";
                }
            }
            return result;
        }

        /// <summary>Chống Chịu hoặc Thể Lực về 0 → ngất; Ấn Bất Khuất (GDD 12.4, áp lên Chống Chịu): 1 lần / đợt giữ 15 và miễn sát thương 2 giây.</summary>
        private bool TriggerFaintIfExhaustedOrGuardBroken(string reason)
        {
            if (EngravedSeals.Contains("AnBatKhuat") && !AnBatKhuatTriggeredThisWave)
            {
                AnBatKhuatTriggeredThisWave = true;
                CurrentGuard = Math.Max(15f, CurrentGuard);
                Stamina.CurrentStamina = Math.Max(15f, Stamina.CurrentStamina);
                FortifyFactor = 0f;
                FortifyUntil = Now + 2.0;
                IsFainted = false;
                return false;
            }

            IsFainted = true;
            FaintCount += 1;
            LastFaintReason = reason;
            return true;
        }

        /// <summary>
        /// Hồi phục Thể Lực và Thanh Chống Chịu theo thời gian thực (GDD 9.4):
        /// - Ngoài giao tranh (>= 3s): hồi 10 STA/s và 14 Chống Chịu/s.
        /// - Trong giao tranh (>= 1.5s không ra đòn/nhận đòn): hồi 2 STA/s và 4 Chống Chịu/s.
        /// - Trong vùng Trạm Hồi Sức (8m): tăng +50% / +75% / +100% tốc độ hồi ngay cả khi đang giao tranh.
        /// </summary>
        public void TickCombatRegen(float deltaSeconds, bool inCombat, float secondsSinceLastCombatAction, int healingStationLevel = 0)
        {
            if (IsFainted || deltaSeconds <= 0f) return;

            float stationMult = healingStationLevel switch
            {
                1 => 1.50f,
                2 => 1.75f,
                >= 3 => 2.00f,
                _ => 1.00f
            };

            // GDD 9.1/9.4: chỉ phần Hao phí tức thời hồi nhanh. Trần = StaminaSystem.EffectiveMaxStamina
            // (Mệt Dội −8%/tầng, trừ Tải ngày), nên phần đã ghi vào Tải ngày (việc trại 100%) không hồi ở đây.
            SyncLowGuardStamina();
            float maxAllowedSta = GetEffectiveMaxStamina();
            float lowGuardRegen = IsGuardLow ? 1f - LowGuardRegenPenalty : 1f; // Chống Chịu < 30%: hồi Thể lực chỉ còn 6/10
            if (Stamina.CurrentStamina >= maxAllowedSta)
            {
                CurrentGuard = Math.Min(MaxGuard, CurrentGuard + (!inCombat && secondsSinceLastCombatAction >= 3.0f ? 14.0f : (secondsSinceLastCombatAction >= 1.5f || healingStationLevel > 0 ? 4.0f : 0f)) * stationMult * deltaSeconds);
                SyncLowGuardStamina();
                return;
            }
            if (!inCombat && secondsSinceLastCombatAction >= 3.0f)
            {
                Stamina.CurrentStamina = Math.Min(maxAllowedSta, Stamina.CurrentStamina + 10.0f * stationMult * lowGuardRegen * deltaSeconds);
                CurrentGuard = Math.Min(MaxGuard, CurrentGuard + 14.0f * stationMult * deltaSeconds);
            }
            else if (secondsSinceLastCombatAction >= 1.5f || healingStationLevel > 0)
            {
                Stamina.CurrentStamina = Math.Min(maxAllowedSta, Stamina.CurrentStamina + 2.0f * stationMult * lowGuardRegen * deltaSeconds);
                CurrentGuard = Math.Min(MaxGuard, CurrentGuard + 4.0f * stationMult * deltaSeconds);
            }
            SyncLowGuardStamina();
        }

        public MartialSchool? LastExecutedSchool { get; set; } = null;

        /// <summary>Timed self-buffs set by skills (runtime seconds; not saved).</summary>
        [NonSerialized] public double FortifyUntil = -1;       // Kim Cương Thể: −50% damage taken
        [NonSerialized] public float FortifyFactor = 1f;
        [NonSerialized] public double ShieldUntil = -1;        // Hộ Thân: absorbs ShieldHp
        [NonSerialized] public float ShieldHp;
        [NonSerialized] public double StanceUntil = -1;        // Trụ Bộ: cannot be pushed or interrupted
        [NonSerialized] public double CounterUntil = -1;       // Phản Chấn: timed block window
        [NonSerialized] public float CounterFactor = 1.5f;
        [NonSerialized] public float RageDecayCarry;
        [NonSerialized] public double LastDodgeAt = -999;       // Guốc Gió: lướt lần 2 trong 1 giây miễn phí
        [NonSerialized] public double Now;                     // last runtime clock seen by MartialProgression

        public float DamageTakenMultiplier() => Now < FortifyUntil ? FortifyFactor : 1f;

        /// <summary>Hộ Thân shield soaks damage first; returns what is left over.</summary>
        public float AbsorbWithShield(float damage)
        {
            if (Now >= ShieldUntil || ShieldHp <= 0f) return damage;
            float soaked = Math.Min(ShieldHp, damage);
            ShieldHp -= soaked;
            return damage - soaked;
        }

        /// <summary>Kept for old callers: casts the slotted skill through <see cref="MartialProgression.Cast"/>.</summary>
        public (bool executed, string message, float damageMultiplier) TryExecuteMartialSkill(CombatSlotType slot, bool allowBloodSacrifice = true)
        {
            var r = MartialProgression.Cast(this, slot, Now, allowBloodSacrifice);
            return (r.Ok, r.Message, r.DamageMultiplier);
        }

        public bool EngraveRune(string runeId)
        {
            if (EngravedRunes.Contains(runeId)) return true;
            if (EngravedRunes.Count >= 6) return false;
            EngravedRunes.Add(runeId);
            RecalculateBonusStats();
            return true;
        }

        public bool EngraveSeal(string sealId)
        {
            if (EngravedSeals.Contains(sealId)) return true;
            if (EngravedSeals.Count >= 3) return false;
            EngravedSeals.Add(sealId);
            RecalculateBonusStats();
            return true;
        }

        /// <summary>
        /// Tính chi phí Thể lực Lướt né theo GDD 9.2 &amp; 12.2:
        /// Gốc 9.0 STA. AGI gốc &gt;= 35: né -15%. Khinh Thân: né -20%. Trọng Giáp: né +25%.
        /// </summary>
        public float CalculateDodgeCost()
        {
            float baseCost = 9.0f;
            float reduction = 0f;

            if (BaseStats.Agi >= 35)
            {
                reduction += 0.15f;
            }

            string playstyle = GetPlaystyle();
            if (playstyle == "KhinhThân")
            {
                reduction += 0.20f;
            }
            else if (playstyle == "TrọngGiáp")
            {
                reduction -= 0.25f;
            }

            float finalCost = baseCost * (1f - reduction);
            return Math.Max(1.0f, finalCost);
        }

        /// <summary>
        /// Tính chi phí Thể lực đòn đánh theo nhịp combo (GDD 9.2 &amp; 12):
        /// Nhịp 1 &amp; 2: 2.5 STA (30% Daily Load)
        /// Nhịp 3 (Finisher): 5.0 STA (40% Daily Load)
        /// </summary>
        public (float staminaCost, float dailyLoadRatio, float damageMultiplier) GetComboStepData(int comboStep)
        {
            switch (comboStep)
            {
                case 0: // Nhịp 1 (Light Strike)
                    return (2.5f, 0.30f, 1.0f);
                case 1: // Nhịp 2 (Follow-up Swing, nhanh hơn 15%)
                    return (2.5f, 0.30f, 1.15f);
                case 2: // Nhịp 3 (Heavy Finisher / Slam, sát thương x1.5, knockback x1.6)
                default:
                    return (5.0f, 0.40f, 1.50f);
            }
        }

        /// <summary>
        /// Bất biến Thiết kế GDD 8.3 &amp; 12.1 (P2.07 - Người C):
        /// Trang bị chỉ cộng vào BonusStats, TUYỆT ĐỐI KHÔNG được tính vào điều kiện học võ kỹ.
        /// Chỉ xét BaseStats (Str, Agi, Ctrl, Res gốc).
        /// </summary>
        public bool CanLearnMartialSkill(int requiredStr, int requiredAgi, int requiredCtrl, int requiredRes)
        {
            return BaseStats.Str >= requiredStr &&
                   BaseStats.Agi >= requiredAgi &&
                   BaseStats.Ctrl >= requiredCtrl &&
                   BaseStats.Res >= requiredRes;
        }
    }

    [Serializable]
    public sealed class MartialSkillDefinition
    {
        public string Id = "";
        public string Name = "";
        public MartialSchool School;
        public CombatSlotType SlotType;
        public int UnlockTier; // 1, 2, 3 = Cấp; 4 = Tuyệt kỹ
        public float StaminaCost;
        /// <summary>Thế Thủ price in HP when paid by Huyết Tế ("6 STA/3 HP"); 0 = 1 HP per 2 STA (GDD 12.1).</summary>
        public int HpCost;
        public int RageCost;
        public float CooldownSeconds;
        public SkillStat ReqStat;
        public int ReqValue;
        public SkillStat ReqStat2;
        public int ReqValue2;
        public bool IsPassive;
        public SkillShape Shape;
        /// <summary>Shape parameters from the GDD "Cơ chế" text: reach in metres, effect seconds, magnitude.</summary>
        public float Range;
        public float Duration;
        public float Magnitude;
        public float DamageMultiplier = 1f;
        public string EffectDescription = "";
        public bool IsUltimate => UnlockTier >= 4;
    }

    /// <summary>
    /// GDD 12.7: the 24 skills, 6 per school. Slot, cost, cooldown and base-stat requirement are copied from the table;
    /// Range/Duration/Magnitude come from the "Cơ chế" column. DamageMultiplier is set only where the GDD gives one
    /// (Thiên Chùy 400%); other damaging shapes use one weapon hit (ADR-0017).
    /// </summary>
    public static class MartialSkillCatalog
    {
        static MartialSkillDefinition S(string id, string name, MartialSchool school, int tier, CombatSlotType slot,
            float sta, int hp, int rage, float cd, SkillStat st, int req, SkillShape shape, float range, float dur, float mag, string text,
            SkillStat st2 = SkillStat.Str, int req2 = 0, bool passive = false, float dmg = 1f)
            => new MartialSkillDefinition
            {
                Id = id, Name = name, School = school, UnlockTier = tier, SlotType = slot, StaminaCost = sta, HpCost = hp, RageCost = rage,
                CooldownSeconds = cd, ReqStat = st, ReqValue = req, ReqStat2 = st2, ReqValue2 = req2, IsPassive = passive, Shape = shape,
                Range = range, Duration = dur, Magnitude = mag, DamageMultiplier = dmg, EffectDescription = text
            };

        const MartialSchool A = MartialSchool.TocBo_A, B = MartialSchool.TrongKich_B, C = MartialSchool.KhongChe_C, D = MartialSchool.SinhTon_D;
        const CombatSlotType Cong = CombatSlotType.TheCong, Thu = CombatSlotType.TheThu, Bien = CombatSlotType.TheBien;

        public static readonly IReadOnlyList<MartialSkillDefinition> AllSkills = new List<MartialSkillDefinition>
        {
            S("pig.skill.bo_phap_luot", "Bộ Pháp Lướt", A, 1, Cong, 8, 0, 0, 3, SkillStat.Agi, 24, SkillShape.Dash, 5, 0.2f, 0, "Lướt 5 m, miễn sát thương 0,2 giây"),
            S("pig.skill.doan_anh_tram", "Đoạn Ảnh Trảm", A, 1, Cong, 10, 0, 0, 6, SkillStat.Agi, 26, SkillShape.Flurry, 1.6f, 0, 3, "3 nhát nhanh; nhát 3 hất quái nhỏ khỏi heo đang bị cắn"),
            S("pig.skill.xuyen_bay", "Xuyên Bầy", A, 2, Cong, 12, 0, 0, 12, SkillStat.Agi, 32, SkillShape.DashThrough, 6, 3, 0.20f, "Chạy xuyên nhóm quái nhỏ, để vệt chậm 20%"),
            S("pig.skill.cuu_nguy", "Cứu Nguy", A, 2, Thu, 10, 0, 0, 20, SkillStat.Agi, 36, SkillShape.Rescue, 10, 0, 0, "Lao tới heo đang bị bắt/kéo trong 10 m và giật lại"),
            S("pig.skill.tiep_vien_toc_hanh", "Tiếp Viện Tốc Hành", A, 3, Bien, 0, 0, 30, 25, SkillStat.Agi, 40, SkillShape.Teleport, 25, 0, 0, "Dịch chuyển tới vùng Triển khai gần nhất trong 25 m"),
            S("pig.skill.vo_anh_bo", "Vô Ảnh Bộ", A, 4, Bien, 0, 0, 70, 60, SkillStat.Agi, 45, SkillShape.Decoy, 5, 5, 4, "2 ảnh ảo hút quái 5 giây; 4 lần né miễn phí"),

            S("pig.skill.pha_giap", "Phá Giáp", B, 1, Cong, 9, 0, 0, 5, SkillStat.Str, 26, SkillShape.ArmorBreak, 1.6f, 6, 0.25f, "Giáp mục tiêu −25% trong 6 giây (mọi nguồn hưởng lợi)"),
            S("pig.skill.chan_dia", "Chấn Địa", B, 1, Cong, 12, 0, 0, 8, SkillStat.Str, 28, SkillShape.PushCircle, 3, 0, 2, "Đẩy mọi quái nhỏ trong 3 m ra 2 m"),
            S("pig.skill.tram_tuyen", "Trảm Tuyến", B, 2, Cong, 15, 0, 0, 12, SkillStat.Str, 34, SkillShape.LineSweep, 6, 0, 2, "Quét đường thẳng 6 m, đẩy lùi cả hàng"),
            S("pig.skill.phan_chan", "Phản Chấn", B, 2, Thu, 12, 6, 0, 10, SkillStat.Str, 34, SkillShape.Counter, 0, 0.3f, 1.5f, "Đỡ đúng lúc 0,3 giây → phản 150%", SkillStat.Res, 28),
            S("pig.skill.neo_kich", "Neo Kích", B, 3, Bien, 0, 0, 40, 20, SkillStat.Str, 40, SkillShape.Pin, 2, 1.5f, 0.5f, "Ghim tinh anh 1,5 giây (Boss 0,5) — tạo cửa sổ cho Lôi Pháo"),
            S("pig.skill.thien_chuy", "Thiên Chùy", B, 4, Bien, 0, 0, 80, 60, SkillStat.Str, 45, SkillShape.Execute, 1.8f, 0, 4, "1 đòn 400%, xóa khiên năng lượng", dmg: 4f),

            S("pig.skill.moc_keo", "Móc Kéo", C, 1, Cong, 8, 0, 0, 6, SkillStat.Ctrl, 26, SkillShape.Pull, 6, 0, 0, "Kéo quái nhỏ 6 m vào bẫy/vùng hỏa lực"),
            S("pig.skill.an_tin_hieu", "Ấn Tín Hiệu", C, 1, Cong, 6, 0, 0, 8, SkillStat.Ctrl, 28, SkillShape.Mark, 8, 5, 0, "Đánh dấu 5 giây: công trình và lính đổi mục tiêu"),
            S("pig.skill.xoay_bay", "Xoay Bầy", C, 2, Bien, 0, 0, 25, 15, SkillStat.Ctrl, 34, SkillShape.ConeTurn, 6, 3, 60, "Đổi hướng bầy quái trong nón 60°, 6 m"),
            S("pig.skill.troi_luoi", "Trói Lưới", C, 2, Thu, 12, 0, 0, 14, SkillStat.Ctrl, 36, SkillShape.Root, 4, 2.5f, 0, "Trói 1 quái 2,5 giây; trói UFO Thu Hoạch làm ngắt chùm kéo"),
            S("pig.skill.dung_chot", "Dựng Chốt", C, 3, Bien, 0, 0, 35, 30, SkillStat.Ctrl, 40, SkillShape.Stakes, 2.5f, 8, 3, "Cắm 3 cọc tạm tạo cửa hẹp 8 giây"),
            S("pig.skill.thien_la", "Thiên La", C, 4, Bien, 0, 0, 75, 75, SkillStat.Ctrl, 45, SkillShape.SlowField, 5, 6, 0.5f, "Vùng 5 m: quái chậm 50% và không rời vùng 6 giây"),

            S("pig.skill.tru_bo", "Trụ Bộ", D, 1, Thu, 6, 3, 0, 6, SkillStat.Res, 24, SkillShape.Stance, 0, 3, 0, "3 giây không bị đẩy/ngắt"),
            S("pig.skill.ho_than", "Hộ Thân", D, 1, Thu, 8, 4, 0, 10, SkillStat.Res, 26, SkillShape.Shield, 0, 5, 40, "Khiên 40 HP trong 5 giây"),
            S("pig.skill.khieu_chien", "Khiêu Chiến", D, 2, Thu, 12, 0, 0, 18, SkillStat.Res, 34, SkillShape.Taunt, 6, 4, 0, "Buộc quái trong 6 m bỏ heo/công trình, đánh người chơi 4 giây"),
            S("pig.skill.hoi_khi", "Hồi Khí", D, 2, Thu, 0, 0, 0, 0, SkillStat.Res, 32, SkillShape.Passive, 0, 5, 0.20f, "Bị động: rời combat 5 giây thì hồi Stamina +20%", passive: true),
            S("pig.skill.chan_mang", "Chắn Mạng", D, 3, Bien, 0, 0, 40, 40, SkillStat.Res, 42, SkillShape.Intercept, 4, 6, 0, "Nhận thay 1 đòn lớn cho heo/công trình trong 4 m"),
            S("pig.skill.kim_cuong_the", "Kim Cương Thể", D, 4, Bien, 0, 0, 70, 90, SkillStat.Res, 45, SkillShape.Fortify, 0, 6, 0.5f, "6 giây giảm 50% sát thương; không tạo Tải ngày"),
        };

        public static MartialSkillDefinition? Find(string id)
        {
            for (int i = 0; i < AllSkills.Count; i++)
                if (string.Equals(AllSkills[i].Id, id, StringComparison.OrdinalIgnoreCase)) return AllSkills[i];
            return null;
        }

        public static MartialSkillDefinition GetByIdOrFallback(string id, MartialSchool school, CombatSlotType slot)
        {
            var found = Find(id);
            if (found != null) return found;
            for (int i = 0; i < AllSkills.Count; i++)
                if (AllSkills[i].School == school && AllSkills[i].SlotType == slot) return AllSkills[i];
            return AllSkills[0];
        }
    }

    [Serializable]
    public sealed class RuneOrSealDefinition
    {
        public string Id = "";
        public string Name = "";
        public bool IsSeal; // false = Phù Văn (tối đa 6), true = Mệnh Ấn (tối đa 3, -10 MaxHP)
        public int PermanentMaxHpPenalty;
        public string EffectDescription = "";
    }

    /// <summary>
    /// Danh mục đầy đủ 15 Trang bị (GDD 12.3), 4 Phù Văn & 3 Mệnh Ấn (GDD 12.4), và 20 Vật phẩm chiến thuật/trang trại/tình báo/sự kiện (GDD 12.5).
    /// </summary>
    public static class EquipmentAndTacticalCatalog
    {
        /// <summary>
        /// GDD 12.3 catalog at quality I. Weight (Nhẹ 1 / Vừa 2 / Nặng 3), "Thủ" and weapon damage • interval are the GDD's;
        /// "AGI/STR thưởng" items get the quality-I bonus +1. Nỏ Tay's interval is not in the GDD (ADR-0017).
        /// </summary>
        public static readonly IReadOnlyList<EquipmentItem> AllEquipmentItems = new List<EquipmentItem>
        {
            new EquipmentItem { Id = "wpn_dao_rua", Name = "Dao Rựa", Slot = EquipSlot.Weapon, WeightScore = 1, Archetype = WeaponArchetype.Slash, BaseDamage = 14, AttackInterval = 0.5f, SpecialMechanic = "Cat", MechanicText = "Cắt: chém đứt dây trói của quái trộm, giải cứu heo trong 1 giây; phát cỏ, dọn bụi −20% Stamina" },
            new EquipmentItem { Id = "wpn_giao_moc", Name = "Giáo Móc", Slot = EquipSlot.Weapon, WeightScore = 2, Archetype = WeaponArchetype.Thrust, BaseDamage = 22, AttackInterval = 0.8f, SpecialMechanic = "MocKeo", MechanicText = "Móc kéo: đòn nặng kéo quái nhỏ về 4 m (vào bẫy); kéo xác từ xa" },
            new EquipmentItem { Id = "wpn_bua_ta", Name = "Búa Tạ", Slot = EquipSlot.Weapon, WeightScore = 3, Archetype = WeaponArchetype.Smash, BaseDamage = 48, AttackInterval = 1.5f, SpecialMechanic = "Chan", MechanicText = "Chấn: đòn nặng choáng 0,3 giây, vỡ khiên năng lượng nhỏ; đóng cọc rào −20% thời gian" },
            new EquipmentItem { Id = "wpn_no_tay", Name = "Nỏ Tay", Slot = EquipSlot.Weapon, WeightScore = 2, Archetype = WeaponArchetype.Ranged, BaseDamage = 26, AttackInterval = 1.0f, SpecialMechanic = "TenTinHieu", MechanicText = "Tên tín hiệu: đánh dấu mục tiêu, công trình ưu tiên bắn 5 giây; mang tối đa 40 tên" },

            new EquipmentItem { Id = "boot_giay_dinh", Name = "Giày Đinh", Slot = EquipSlot.Boots, WeightScore = 2, Defense = 2, SpecialMechanic = "MienTronTruot", MechanicText = "Miễn Trơn Trượt; lướt trên bùn không chậm" },
            new EquipmentItem { Id = "boot_guoc_gio", Name = "Guốc Gió", Slot = EquipSlot.Boots, WeightScore = 1, BonusAgi = 1, SpecialMechanic = "LuotKep", MechanicText = "Lướt liên tiếp lần 2 trong 1 giây không tốn Stamina" },

            new EquipmentItem { Id = "armor_giap_da_thuoc", Name = "Giáp Da Thuộc", Slot = EquipSlot.BodyArmor, WeightScore = 2, Defense = 12, SpecialMechanic = "GiamNhiemTrung", MechanicText = "Giảm 50% nhiễm trùng khi bị quái cắn" },
            new EquipmentItem { Id = "armor_giap_luoi_thep", Name = "Giáp Lưới Thép", Slot = EquipSlot.BodyArmor, WeightScore = 3, Defense = 22, SpecialMechanic = "KhongBiKeoDay", MechanicText = "Không bị kéo/đẩy bởi quái nhỏ" },
            new EquipmentItem { Id = "armor_ao_mua_dau", Name = "Áo Mưa Dầu", Slot = EquipSlot.BodyArmor, WeightScore = 1, Defense = 4, SpecialMechanic = "MienUotLanh", MechanicText = "Miễn Ướt Lạnh" },
            new EquipmentItem { Id = "armor_ao_chi", Name = "Áo Chì", Slot = EquipSlot.BodyArmor, WeightScore = 3, Defense = 10, SpecialMechanic = "MienNhiemXa", MechanicText = "Miễn Nhiễm Xạ" },

            new EquipmentItem { Id = "pants_quan_da_lot_bong", Name = "Quần Da Lót Bông", Slot = EquipSlot.Pants, WeightScore = 1, Defense = 3, SpecialMechanic = "MienCongTay", MechanicText = "Miễn Cóng Tay (giữ ấm toàn thân); ngồi nghỉ hồi +20%" },
            new EquipmentItem { Id = "pants_quan_giap_xich", Name = "Quần Giáp Xích", Slot = EquipSlot.Pants, WeightScore = 3, Defense = 10, SpecialMechanic = "QuanGiapXich", MechanicText = "Đỡ đòn thành công hoàn 2 Stamina" },

            new EquipmentItem { Id = "brc_vong_cach_dien", Name = "Vòng Cách Điện", Slot = EquipSlot.Bracelet, WeightScore = 1, SpecialMechanic = "CachDien", MechanicText = "Giảm 80% khả năng bị sét đánh; miễn choáng điện" },
            new EquipmentItem { Id = "brc_vong_huyet_nha", Name = "Vòng Huyết Nha", Slot = EquipSlot.Bracelet, WeightScore = 2, BonusStr = 1, SpecialMechanic = "HuyetNha", MechanicText = "Nộ Khí nhận +50% từ tinh anh" },

            new EquipmentItem { Id = "nck_coi_chan_heo", Name = "Còi Chăn Heo", Slot = EquipSlot.Necklace, WeightScore = 1, SpecialMechanic = "CoiChanHeo", MechanicText = "Heo Hoảng sợ trong 10 m hồi +10 Tinh thần (1 lần/2 phút)" },
            new EquipmentItem { Id = "nck_mat_day_den_bao", Name = "Mặt Dây Đèn Bão", Slot = EquipSlot.Necklace, WeightScore = 1, SpecialMechanic = "DenBao", MechanicText = "Bỏ Lạc Hướng; lộ tàng hình trong 5 m" },
        };

        /// <summary>Catalog piece by id; "id#II".."#IV" returns that quality (GDD 12.3).</summary>
        public static EquipmentItem? Find(string id)
        {
            string baseId = EquipmentQuality.BaseId(id);
            for (int i = 0; i < AllEquipmentItems.Count; i++)
                if (string.Equals(AllEquipmentItems[i].Id, baseId, StringComparison.OrdinalIgnoreCase))
                {
                    var q = EquipmentQuality.QualityOf(id);
                    return q == ItemTier.I ? AllEquipmentItems[i] : AllEquipmentItems[i].WithQuality(q);
                }
            return null;
        }

        public static readonly IReadOnlyList<RuneOrSealDefinition> AllRunesAndSeals = new List<RuneOrSealDefinition>
        {
            // 4 Phù Văn (GDD 12.4)
            new RuneOrSealDefinition { Id = "PhuTruyPhong", Name = "Phù Truy Phong", IsSeal = false, PermanentMaxHpPenalty = 0, EffectDescription = "+4% tốc chạy sau khi dùng kỹ năng." },
            new RuneOrSealDefinition { Id = "PhuNoHoa", Name = "Phù Nộ Hỏa", IsSeal = false, PermanentMaxHpPenalty = 0, EffectDescription = "+2 Nộ mỗi lần đánh trúng mục tiêu đang bị khống chế." },
            new RuneOrSealDefinition { Id = "PhuKienThach", Name = "Phù Kiên Thạch", IsSeal = false, PermanentMaxHpPenalty = 0, EffectDescription = "+8% hiệu quả khiên từ kỹ năng Sinh Tồn." },
            new RuneOrSealDefinition { Id = "PhuHoDan", Name = "Phù Hộ Đàn", IsSeal = false, PermanentMaxHpPenalty = 0, EffectDescription = "Heo trong bán kính 6m quanh nhân vật giảm -15% sát thương nhận." },

            // 3 Mệnh Ấn (GDD 12.4 — mỗi ấn trừ vĩnh viễn -10 MaxHP)
            new RuneOrSealDefinition { Id = "AnBatKhuat", Name = "Ấn Bất Khuất", IsSeal = true, PermanentMaxHpPenalty = 10, EffectDescription = "Đổi -10 máu tối đa: mỗi đợt 1 lần chặn ngất ở 15 Chống Chịu & Thể Lực." },
            new RuneOrSealDefinition { Id = "AnSongThe", Name = "Ấn Song Thế", IsSeal = true, PermanentMaxHpPenalty = 10, EffectDescription = "Đổi -10 máu tối đa: kích hoạt Hợp kích hoàn lại 5 Thể Lực." },
            new RuneOrSealDefinition { Id = "AnMucDong", Name = "Ấn Mục Đồng", IsSeal = true, PermanentMaxHpPenalty = 10, EffectDescription = "Đổi -10 máu tối đa: heo Thủ Lĩnh còn sống giúp nhân vật +10% kháng." }
        };

        /// <summary>GDD 12.8 tactical items: tier and mechanic text from the GDD table (3 Nov: several were invented); the
        /// day-market prices the GDD gives (Xô 20, Mái Che 80, Bình 90, Băng Gạc 60, Pháo Sáng 80). Effects: ItemUse.</summary>
        public static readonly IReadOnlyList<GameItem> AllTwentyTacticalItems = new List<GameItem>
        {
            // Nhóm Trang trại (5)
            new GameItem { Id = "XoNuoc", Name = "Xô Nước", Tier = ItemTier.I, ReferenceValueGold = 20, Category = "Farm", Description = "Dội hạ nhiệt 1 heo đang Say Nắng (3 Thể lực); dùng lại được." },
            new GameItem { Id = "MaiCheTam", Name = "Mái Che Tạm", Tier = ItemTier.I, ReferenceValueGold = 80, Category = "Farm", Description = "Dựng bóng mát 6 m tại chỗ trong 1 ngày; tính như 1 mái trú cho 4 heo." },
            new GameItem { Id = "CamLinhChi", Name = "Cám Linh Chi", Tier = ItemTier.II, ReferenceValueGold = 260, Category = "Farm", Description = "Bắt buộc cho Bồi Dưỡng Kế Vị; +5 Tinh thần trong ngày cho con được ăn." },
            new GameItem { Id = "CamHaHoa", Name = "Cám Hạ Hỏa", Tier = ItemTier.II, ReferenceValueGold = 240, Category = "Farm", Description = "Thuần hóa Xích Mao (sau nghiên cứu); Tinh thần mất −20% trong 1 ngày." },
            new GameItem { Id = "ThuocAnThanHeo", Name = "Thuốc An Thần Heo", Tier = ItemTier.II, ReferenceValueGold = 210, Category = "Farm", Description = "Cho 1 heo: Tinh thần +30, dập cơn Lôi Tâm." },

            // Nhóm Thông tin (4)
            new GameItem { Id = "TinDonCho", Name = "Tin Đồn Chợ", Tier = ItemTier.I, ReferenceValueGold = 150, Category = "Info", Description = "Lộ chủ đề chu kỳ đang chạy." },
            new GameItem { Id = "LaDoSuong", Name = "Lá Dò Sương", Tier = ItemTier.II, ReferenceValueGold = 320, Category = "Info", Description = "24 giờ: nếu Sương mù huyền bí sắp ập đến sẽ cảnh báo trước 10 phút game." },
            new GameItem { Id = "BanDoHuongQuai", Name = "Bản Đồ Hướng Quái", Tier = ItemTier.III, ReferenceValueGold = 280, Category = "Info", Description = "Lộ hướng xuất hiện làn sóng đầu của đợt lớn kế tiếp." },
            new GameItem { Id = "HoSoGiongLoai", Name = "Hồ Sơ Giống Loài", Tier = ItemTier.III, ReferenceValueGold = 950, Category = "Info", Description = "Định danh trước 1 dòng gen." },

            // Nhóm Chiến đấu (6)
            new GameItem { Id = "BinhHoiLuc", Name = "Bình Hồi Lực", Tier = ItemTier.I, ReferenceValueGold = 90, Category = "Combat", Description = "Hoàn 40 Thể lực phần hao phí; uống 1 giây, trúng đòn bị ngắt." },
            new GameItem { Id = "BangGac", Name = "Băng Gạc", Tier = ItemTier.I, ReferenceValueGold = 60, Category = "Combat", Description = "+30 Chống Chịu trong 5 giây; dùng được cho lính." },
            new GameItem { Id = "LuuKhoi", Name = "Lựu Khói", Tier = ItemTier.II, ReferenceValueGold = 180, Category = "Combat", Description = "Quái trong 4 m mất mục tiêu 3 giây — để rút lui." },
            new GameItem { Id = "BomKeo", Name = "Bom Keo", Tier = ItemTier.II, ReferenceValueGold = 190, Category = "Combat", Description = "Vũng keo 3 m, 5 giây — dựng cửa hẹp tạm." },
            new GameItem { Id = "PhaoSang", Name = "Pháo Sáng", Tier = ItemTier.I, ReferenceValueGold = 80, Category = "Combat", Description = "Lộ tàng hình trong 12 m, 10 giây." },
            new GameItem { Id = "TinhNoDan", Name = "Tinh Nộ Đan", Tier = ItemTier.III, ReferenceValueGold = 310, Category = "Combat", Description = "+50 Nộ khí ngay lập tức." },

            // Nhóm Sự kiện & Nghiên cứu (5 -> tổng 20 vật phẩm)
            new GameItem { Id = "BuaHoiHuong", Name = "Bùa Hồi Hương", Tier = ItemTier.I, ReferenceValueGold = 680, Category = "Event", Description = "Thoát Cổng khi đã hoàn thành ≥ 50%, giữ 50% thưởng." },
            new GameItem { Id = "LaBanTanVu", Name = "La Bàn Tán Vụ", Tier = ItemTier.III, ReferenceValueGold = 720, Category = "Event", Description = "Thoát Sương mù sau khi đã ở ≥ 1 ngày, giữ 30% thưởng." },
            new GameItem { Id = "NgocPhaSuong", Name = "Ngọc Phá Sương", Tier = ItemTier.IV, ReferenceValueGold = 450, Category = "Event", Description = "Thoát Sương mù bất cứ lúc nào, giữ 50% thưởng." },
            new GameItem { Id = "ChiaCongNguoc", Name = "Chìa Cổng Ngược", Tier = ItemTier.III, ReferenceValueGold = 820, Category = "Event", Description = "Giữ một Cổng không biến mất thêm 6 giờ." },
            new GameItem { Id = "MauSinhHoc", Name = "Mẫu Sinh Học", Tier = ItemTier.II, ReferenceValueGold = 220, Category = "Research", Description = "Nguyên liệu dùng tại Phòng Nghiên Cứu để giám định gen và điều chế thuốc." }
        };
    }
}

