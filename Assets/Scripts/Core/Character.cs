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
        public float BaseMaxStamina => Math.Max(0f, MaxStamina * (1f - Math.Min(4, ReboundFatigueStacks) * 0.08f));

        /// <summary>
        /// Giới hạn thể lực hiệu dụng tức thời: Trần thể lực trừ đi Tải ngày (Daily Load)
        /// </summary>
        public float EffectiveMaxStamina => Math.Max(0f, BaseMaxStamina - DailyLoad);

        /// <summary>
        /// Vùng mệt mỏi theo GDD 9.4: CurrentStamina &lt; 20% MaxStamina.
        /// Chạy -15%, đòn nặng / combo trễ +20%, không khóa hành động.
        /// </summary>
        public bool IsFatigued => CurrentStamina < (MaxStamina * 0.2f);

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
        public void PerformSleepReset()
        {
            DailyLoad = 0f;
            float baseCap = BaseMaxStamina;
            CurrentStamina = Math.Max(CurrentStamina, baseCap * 0.8f);
            Condition = Math.Min(100, Condition + 6);
            Immunity = Math.Min(100, Immunity + 5);
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
        public string Id;
        public string Name;
        public EquipSlot Slot;
        public int WeightScore = 1; // 1=Nhẹ, 2=Vừa, 3=Nặng
        public int Defense;
        public int BonusStr;
        public int BonusAgi;
        public int BonusCtrl;
        public int BonusRes;
        public string SpecialMechanic;
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
        public Dictionary<EquipSlot, EquipmentItem> Equipment = new Dictionary<EquipSlot, EquipmentItem>();
        public MartialSchool PrimarySchool = MartialSchool.TrongKich_B;

        [NonSerialized]
        public Dictionary<CombatSlotType, string> ActiveMartialSkills = new Dictionary<CombatSlotType, string>();

        public CharacterData() : this(PlayerId.Khoa)
        {
        }

        public CharacterData(PlayerId identity)
        {
            foreach (EquipSlot slot in Enum.GetValues(typeof(EquipSlot)))
            {
                Equipment[slot] = null;
            }

            ActiveMartialSkills[CombatSlotType.TheCong] = "B_Cong_1_BoNgan";
            ActiveMartialSkills[CombatSlotType.TheThu] = "B_Thu_1_TruVung";
            ActiveMartialSkills[CombatSlotType.TheBien] = "B_Bien_1_ChanDia";
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

        public void EquipStarterSet()
        {
            Equipment[EquipSlot.Weapon] = new EquipmentItem
            {
                Id = "wpn_gay_chan_dan",
                Name = "Gậy Chăn Đàn Bọc Thép",
                Slot = EquipSlot.Weapon,
                WeightScore = 2,
                Defense = 8,
                BonusStr = 2,
                BonusCtrl = 1,
                SpecialMechanic = "ChanDan"
            };
            Equipment[EquipSlot.Boots] = new EquipmentItem
            {
                Id = "boot_ung_da_rung",
                Name = "Ủng Da Rừng Bờ Rìa",
                Slot = EquipSlot.Boots,
                WeightScore = 2,
                Defense = 6,
                BonusAgi = 2,
                SpecialMechanic = "ChongBun"
            };
            Equipment[EquipSlot.BodyArmor] = new EquipmentItem
            {
                Id = "armor_ao_vai_day",
                Name = "Áo Vải Dày Gia Cố",
                Slot = EquipSlot.BodyArmor,
                WeightScore = 2,
                Defense = 14,
                BonusRes = 2,
                SpecialMechanic = "GiamChanThuong"
            };
            Equipment[EquipSlot.Pants] = new EquipmentItem
            {
                Id = "pants_quan_giap_xich",
                Name = "Quần Giáp Xích Nhẹ",
                Slot = EquipSlot.Pants,
                WeightScore = 2,
                Defense = 10,
                BonusRes = 1,
                SpecialMechanic = "QuanGiapXich"
            };
            Equipment[EquipSlot.Bracelet] = new EquipmentItem
            {
                Id = "brc_vong_co_bach_van",
                Name = "Vòng Tay Đồng Cũ",
                Slot = EquipSlot.Bracelet,
                WeightScore = 1,
                Defense = 4,
                BonusCtrl = 2,
                SpecialMechanic = "TichNoNhanh"
            };
            Equipment[EquipSlot.Necklace] = new EquipmentItem
            {
                Id = "nck_day_chuyen_nanh",
                Name = "Dây Chuyền Nanh Sói",
                Slot = EquipSlot.Necklace,
                WeightScore = 1,
                Defense = 4,
                BonusStr = 1,
                BonusAgi = 1,
                SpecialMechanic = "TangSatThuongTinhAnh"
            };
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
                _ => false
            };
        }

        public bool EquipItem(EquipmentItem item)
        {
            Equipment[item.Slot] = item;
            RecalculateBonusStats();
            return true;
        }

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

            int totalRes = BaseStats.Res + BonusStats.Res;
            int sealPenalty = EngravedSeals.Count * 10;
            MaxHp = Math.Max(40, 100 - sealPenalty);
            if (Hp > MaxHp) Hp = MaxHp;

            float newMaxGuard = Math.Max(50f, 100f + totalRes * 2.5f - sealPenalty);
            if (GetPlaystyle() == "TrọngGiáp") newMaxGuard += 25f;
            MaxGuard = newMaxGuard;
            if (CurrentGuard > MaxGuard) CurrentGuard = MaxGuard;
        }

        public float GetEffectiveMaxStamina()
        {
            float condFactor = Stamina.Condition >= 70 ? 1.0f : (Stamina.Condition >= 40 ? 0.85f : 0.70f);
            float reboundPenalty = Stamina.ReboundFatigueStacks * 18f;
            float cap = Stamina.MaxStamina * condFactor - Stamina.DailyLoad - reboundPenalty;
            return Math.Max(25f, cap);
        }

        public bool IsFatigued()
        {
            return Stamina.CurrentStamina < (Stamina.MaxStamina * 0.20f);
        }

        public float GetMoveSpeedMultiplier()
        {
            float mult = 1.0f;
            string style = GetPlaystyle();
            if (style == "KhinhThân") mult += 0.12f;
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

            if (Stamina.CurrentStamina <= 0f)
            {
                TriggerFaintIfExhaustedOrGuardBroken("Cạn Kiệt Thể Lực Chiến Đấu (0 Thể Lực)");
            }
            return !IsFainted;
        }

        /// <summary>
        /// Xử lý đòn đánh của quái vào nhân vật:
        /// - Nhân vật không dùng thanh máu truyền thống trên HUD mà dùng cơ chế Đỡ Mặc Định (Auto-Guard) kết hợp Thanh Chống Chịu (Guard Bar) + Thanh Thể Lực (Stamina Bar), hoặc Đỡ Chủ Động (Active Parry).
        /// - Khi Đỡ Chủ Động (isActiveParry = true): chỉ tốn 5 STA (hoặc hồi +2 STA nếu mặc Quần Giáp Xích), mất rất ít điểm Chống Chịu (15%), phản sát thương 50% và đẩy lùi quái.
        /// - Khi Đỡ Mặc Định (Auto-Guard): nhân vật tự gồng vũ khí đỡ đòn, tiêu hao Thể Lực (65% sát thương sau giảm trừ) VÀ trừ điểm Thanh Chống Chịu (90% sát thương sau giảm trừ).
        /// - Khi Thanh Chống Chịu về 0 (dù còn Stamina) HOẶC Stamina về 0 (dù còn Chống Chịu), nhân vật lập tức bị ngất và được kéo về trại hồi sức!
        /// </summary>
        public CombatHitResolution ProcessIncomingAttack(float rawEnemyDamage, bool isActiveParry, bool isHoldingGuardStance = false)
        {
            var result = new CombatHitResolution
            {
                RawDamage = rawEnemyDamage
            };

            int totalDef = GetTotalEquipmentDefense();
            int totalRes = BaseStats.Res + BonusStats.Res;
            float defFactor = 100f / (100f + Math.Max(0, totalDef));
            float resFactor = Math.Max(0.40f, 1.0f - totalRes * 0.005f);
            float styleFactor = GetPlaystyle() == "TrọngGiáp" ? 0.80f : (GetPlaystyle() == "KhinhThân" ? 1.08f : 1.0f);

            float mitigated = Math.Max(1.0f, (float)Math.Round(rawEnemyDamage * defFactor * resFactor * styleFactor, 1));
            result.MitigatedDamage = mitigated;

            bool hasQuanGiapXich = Equipment.TryGetValue(EquipSlot.Pants, out var pants) &&
                                   pants != null &&
                                   pants.SpecialMechanic == "QuanGiapXich";

            if (isActiveParry)
            {
                result.WasActiveParry = true;
                float staDelta = hasQuanGiapXich ? -2.0f : 4.0f; // Quần Giáp Xích hoàn +2 STA khi đỡ đúng nhịp (GDD 12.2)
                if (staDelta > 0f)
                {
                    Stamina.CurrentStamina = Math.Max(0f, Stamina.CurrentStamina - staDelta);
                    Stamina.DailyLoad = Math.Min(Stamina.MaxStamina * 0.65f, Stamina.DailyLoad + staDelta * 0.25f);
                    result.StaminaDrained = staDelta;
                }
                else
                {
                    Stamina.CurrentStamina = Math.Min(GetEffectiveMaxStamina(), Stamina.CurrentStamina + Math.Abs(staDelta));
                    result.StaminaDrained = staDelta;
                }

                float guardLoss = (float)Math.Round(Math.Max(1.0f, mitigated * 0.18f), 1);
                CurrentGuard = Math.Max(0f, CurrentGuard - guardLoss);
                result.GuardDrained = guardLoss;
                result.CounterDamage = CombatFormulas.CalculateParryCounterDamage((int)Math.Ceiling(rawEnemyDamage));

                int rageGain = Math.Clamp((int)(rawEnemyDamage * 0.4f) + 4, 5, 18);
                if (Equipment.TryGetValue(EquipSlot.Bracelet, out var brc) && brc != null && brc.SpecialMechanic == "TichNoNhanh")
                {
                    rageGain += 3;
                }
                Rage = Math.Min(100, Rage + rageGain);
                result.RageGained = rageGain;
                result.SummaryMessage = $"ĐỠ CHỦ ĐỘNG HOÀN HẢO! (-{guardLoss:0.0} Chống Chịu, Phản {result.CounterDamage:0} ST, +{rageGain} Nộ)";
            }
            else
            {
                // Mặc định nhân vật luôn tự đỡ đòn (Auto-Guard) hoặc giữ thế đỡ (Guard Stance)
                result.WasAutoGuarded = true;
                float stanceStaMult = isHoldingGuardStance ? 0.42f : 0.65f;
                float stanceGuardMult = isHoldingGuardStance ? 0.50f : 0.90f;

                float staLoss = (float)Math.Round(Math.Max(1.5f, mitigated * stanceStaMult), 1);
                float guardLoss = (float)Math.Round(Math.Max(2.0f, mitigated * stanceGuardMult), 1);

                Stamina.CurrentStamina = Math.Max(0f, Stamina.CurrentStamina - staLoss);
                Stamina.DailyLoad = Math.Min(Stamina.MaxStamina * 0.65f, Stamina.DailyLoad + staLoss * 0.30f);
                CurrentGuard = Math.Max(0f, CurrentGuard - guardLoss);

                result.StaminaDrained = staLoss;
                result.GuardDrained = guardLoss;

                int rageGain = CombatFormulas.CalculateRageGainOnDamage(mitigated);
                Rage = Math.Min(100, Rage + rageGain);
                result.RageGained = rageGain;
                result.SummaryMessage = $"ĐỠ TỰ ĐỘNG: -{staLoss:0.0} Thể Lực, -{guardLoss:0.0} Chống Chịu (+{rageGain} Nộ)";
            }

            // Kiểm tra điều kiện Ngất (Thanh Chống Chịu về 0 dù còn Stamina, HOẶC Stamina về 0 dù còn Chống Chịu)
            if (CurrentGuard <= 0f || Stamina.CurrentStamina <= 0f)
            {
                string reason = CurrentGuard <= 0f && Stamina.CurrentStamina > 0f
                    ? "Vỡ Thanh Chống Chịu (0 Điểm Chống Chịu dù còn Thể Lực)"
                    : (Stamina.CurrentStamina <= 0f && CurrentGuard > 0f
                        ? "Cạn Kiệt Thể Lực Đỡ Đòn (0 Thể Lực dù còn Chống Chịu)"
                        : "Vỡ Chống Chịu & Cạn Kiệt Thể Lực");

                if (TriggerFaintIfExhaustedOrGuardBroken(reason))
                {
                    result.CausedFaint = true;
                    result.FaintReason = LastFaintReason;
                    result.SummaryMessage = $"NGẤT XỈU ({LastFaintReason})! Được kéo về hiên nhà chính hồi sức!";
                }
                else
                {
                    result.TriggeredAnBatKhuat = true;
                    result.SummaryMessage = "ẤN BẤT KHUẤT KÍCH HOẠT! Giữ lại 15 Chống Chịu & Thể Lực, bất tử 2 giây!";
                }
            }

            return result;
        }

        private bool TriggerFaintIfExhaustedOrGuardBroken(string reason)
        {
            if (EngravedSeals.Contains("AnBatKhuat") && !AnBatKhuatTriggeredThisWave)
            {
                AnBatKhuatTriggeredThisWave = true;
                CurrentGuard = Math.Max(15f, CurrentGuard);
                Stamina.CurrentStamina = Math.Max(15f, Stamina.CurrentStamina);
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

            float maxAllowedSta = GetEffectiveMaxStamina();
            if (!inCombat && secondsSinceLastCombatAction >= 3.0f)
            {
                Stamina.CurrentStamina = Math.Min(maxAllowedSta, Stamina.CurrentStamina + 10.0f * stationMult * deltaSeconds);
                CurrentGuard = Math.Min(MaxGuard, CurrentGuard + 14.0f * stationMult * deltaSeconds);
            }
            else if (secondsSinceLastCombatAction >= 1.5f || healingStationLevel > 0)
            {
                Stamina.CurrentStamina = Math.Min(maxAllowedSta, Stamina.CurrentStamina + 2.0f * stationMult * deltaSeconds);
                CurrentGuard = Math.Min(MaxGuard, CurrentGuard + 4.0f * stationMult * deltaSeconds);
            }
        }

        public MartialSchool? LastExecutedSchool { get; set; } = null;

        /// <summary>
        /// Kiểm tra liên kết vòng một chiều A -> C -> B -> D -> A và trả về tên Hợp Kích (GDD 12.6).
        /// </summary>
        public static (bool isSynergy, string synergyName, string effectSummary, float bonusMultiplier) EvaluateSchoolComboSynergy(
            MartialSchool? previousSchool,
            MartialSchool nextSchool)
        {
            if (!previousSchool.HasValue) return (false, string.Empty, string.Empty, 1.0f);

            return (previousSchool.Value, nextSchool) switch
            {
                (MartialSchool.TocBo_A, MartialSchool.KhongChe_C) =>
                    (true, "Ảnh Móc (A → C)", "Sau lướt/chém nhanh, móc kéo hoặc trói tăng +25% tầm và tốc độ", 1.25f),
                (MartialSchool.KhongChe_C, MartialSchool.TrongKich_B) =>
                    (true, "Trói Chùy (C → B)", "Mục tiêu đang bị trói/làm chậm nhận thêm +30% sát thương từ đòn đập nặng", 1.30f),
                (MartialSchool.TrongKich_B, MartialSchool.SinhTon_D) =>
                    (true, "Phản Thủ (B → D)", "Sau đòn đập nặng hoặc Phá Giáp, bật khiên/trụ nhận thêm +25% giá trị chắn", 1.25f),
                (MartialSchool.SinhTon_D, MartialSchool.TocBo_A) =>
                    (true, "Thủ Chuyển Công (D → A)", "Sau khi đỡ đòn hoặc phản chấn, đòn lướt kế tiếp miễn Thể lực và +20% sát thương", 1.20f),
                _ => (false, string.Empty, string.Empty, 1.0f)
            };
        }

        /// <summary>
        /// Thực thi kỹ năng võ học ở 3 ô Thế Công / Thế Thủ / Thế Biến (GDD 12.4 - 12.7).
        /// Hỗ trợ bánh xe liên kết A -> C -> B -> D -> A (Hợp kích) và Huyết Tế trên Thế Thủ.
        /// </summary>
        public (bool executed, string message, float damageMultiplier) TryExecuteMartialSkill(CombatSlotType slot, bool allowBloodSacrifice = true)
        {
            if (!IsMartialSlotUnlocked(slot))
            {
                return (false, $"Ô [{slot}] bị khóa! Cần mặc đủ cặp trang bị tương ứng.", 1.0f);
            }

            string skillId = ActiveMartialSkills.TryGetValue(slot, out var sid) && !string.IsNullOrEmpty(sid)
                ? sid
                : "pig.skill.chan_dia";
            var skillDef = MartialSkillCatalog.GetByIdOrFallback(skillId, PrimarySchool, slot);
            var synergy = EvaluateSchoolComboSynergy(LastExecutedSchool, skillDef.School);

            float staRequired = skillDef.StaminaCost > 0f
                ? skillDef.StaminaCost
                : (slot switch
                {
                    CombatSlotType.TheCong => 14f,
                    CombatSlotType.TheThu => 12f,
                    CombatSlotType.TheBien => 15f,
                    _ => 14f
                });

            // Hợp kích D -> A (Thủ Chuyển Công): miễn Thể lực cho kỹ năng Tốc Bộ kế tiếp!
            if (synergy.isSynergy && LastExecutedSchool == MartialSchool.SinhTon_D && skillDef.School == MartialSchool.TocBo_A)
            {
                staRequired = 0f;
            }

            if (Stamina.CurrentStamina < staRequired)
            {
                if (slot == CombatSlotType.TheThu && allowBloodSacrifice)
                {
                    // Huyết Tế (GDD 12.4): 1 điểm Chống Chịu / Máu thay cho 2 điểm Thể Lực thiếu hụt
                    float missingSta = staRequired - Stamina.CurrentStamina;
                    float guardSacrifice = (float)Math.Ceiling(missingSta / 2.0f);
                    if (CurrentGuard > guardSacrifice + 5f)
                    {
                        Stamina.CurrentStamina = 1f;
                        CurrentGuard -= guardSacrifice;
                        Rage = Math.Min(100, Rage + 12);
                        LastExecutedSchool = skillDef.School;
                        return (true, $"[HUYẾT TẾ · THẾ THỦ] Hiến {guardSacrifice:0} Chống Chịu phát động [{skillDef.Name}]!", 1.35f * synergy.bonusMultiplier);
                    }
                }
                return (false, $"Không đủ Thể Lực ({Stamina.CurrentStamina:0}/{staRequired:0}) để thi triển [{skillDef.Name}]!", 1.0f);
            }

            if (staRequired > 0f)
            {
                TryConsumeCombatStamina(staRequired, 0.35f, out _);
            }

            LastExecutedSchool = skillDef.School;
            string synTag = synergy.isSynergy ? $" + HỢP KÍCH [{synergy.synergyName}]" : string.Empty;

            if (slot == CombatSlotType.TheThu)
            {
                float guardGain = 22f * (synergy.isSynergy ? synergy.bonusMultiplier : 1.0f);
                CurrentGuard = Math.Min(MaxGuard, CurrentGuard + guardGain);
                return (true, $"Thi triển [{skillDef.Name}] ({skillDef.School}){synTag}: Hồi +{guardGain:0} Chống Chịu & tạo khiên phản chấn!", 1.25f * synergy.bonusMultiplier);
            }
            if (slot == CombatSlotType.TheBien)
            {
                Rage = Math.Min(100, Rage + 15);
                return (true, $"Thi triển [{skillDef.Name}] ({skillDef.School}){synTag}: Biến chiêu chuẩn xác (+15 Nộ Khí)!", 1.45f * synergy.bonusMultiplier);
            }
            return (true, $"Thi triển [{skillDef.Name}] ({skillDef.School}){synTag}: Đòn Thế Công uy lực!", 1.65f * synergy.bonusMultiplier);
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
        public int UnlockTier; // 1 = Nền, 2 = Nhánh, 3 = Đỉnh
        public float StaminaCost = 14f;
        public int RageCost = 0;
        public float CooldownSeconds = 6f;
        public float DamageMultiplier = 1.5f;
        public string EffectDescription = "";
    }

    /// <summary>
    /// Danh mục đầy đủ 24 Kỹ Năng Võ Học thuộc 4 Trường Phái (A — Tốc Bộ, B — Trọng Kích, C — Khống Chế, D — Sinh Tồn) theo GDD 12.7.
    /// </summary>
    public static class MartialSkillCatalog
    {
        public static readonly IReadOnlyList<MartialSkillDefinition> AllSkills = new List<MartialSkillDefinition>
        {
            // Trường phái A — Tốc Bộ (6 kỹ năng)
            new MartialSkillDefinition { Id = "pig.skill.bo_phap_luot", Name = "Bộ Pháp Lướt", School = MartialSchool.TocBo_A, SlotType = CombatSlotType.TheBien, UnlockTier = 1, StaminaCost = 10f, CooldownSeconds = 3.5f, DamageMultiplier = 1.20f, EffectDescription = "Lướt nhanh 4.5m xuyên vật cản nhỏ, đòn đánh kế tiếp +20% tốc độ." },
            new MartialSkillDefinition { Id = "pig.skill.doan_anh_tram", Name = "Đoạn Ảnh Trảm", School = MartialSchool.TocBo_A, SlotType = CombatSlotType.TheCong, UnlockTier = 1, StaminaCost = 13f, CooldownSeconds = 4.5f, DamageMultiplier = 1.55f, EffectDescription = "Chém 2 nhát liên hoàn tốc độ cao vào mục tiêu trước mặt." },
            new MartialSkillDefinition { Id = "pig.skill.xuyen_bay", Name = "Xuyên Bầy", School = MartialSchool.TocBo_A, SlotType = CombatSlotType.TheCong, UnlockTier = 2, StaminaCost = 15f, CooldownSeconds = 6.0f, DamageMultiplier = 1.65f, EffectDescription = "Đột kích cắt ngang đội hình địch, không bị chặn bởi quái nhỏ." },
            new MartialSkillDefinition { Id = "pig.skill.cuu_nguy", Name = "Cứu Nguy", School = MartialSchool.TocBo_A, SlotType = CombatSlotType.TheThu, UnlockTier = 2, StaminaCost = 12f, CooldownSeconds = 7.0f, DamageMultiplier = 1.30f, EffectDescription = "Lao tới heo đang bị bắt cóc trong 8m, đánh bật Kẻ Bắt Heo và hồi +15 Chống Chịu." },
            new MartialSkillDefinition { Id = "pig.skill.tiep_vien_toc_hanh", Name = "Tiếp Viện Tốc Hành", School = MartialSchool.TocBo_A, SlotType = CombatSlotType.TheBien, UnlockTier = 2, StaminaCost = 14f, CooldownSeconds = 10.0f, DamageMultiplier = 1.25f, EffectDescription = "Tăng +30% tốc chạy cho bản thân và lính đánh thuê quanh 10m trong 5s." },
            new MartialSkillDefinition { Id = "pig.skill.vo_anh_bo", Name = "Vô Ảnh Bộ", School = MartialSchool.TocBo_A, SlotType = CombatSlotType.TheCong, UnlockTier = 3, StaminaCost = 18f, RageCost = 50, CooldownSeconds = 14.0f, DamageMultiplier = 2.40f, EffectDescription = "Tuyệt kỹ Đỉnh Tốc Bộ: Hóa tàn ảnh chém 5 nhát cực nhanh, miễn sát thương trong 1.2s." },

            // Trường phái B — Trọng Kích (6 kỹ năng)
            new MartialSkillDefinition { Id = "pig.skill.pha_giap", Name = "Phá Giáp", School = MartialSchool.TrongKich_B, SlotType = CombatSlotType.TheCong, UnlockTier = 1, StaminaCost = 14f, CooldownSeconds = 5.0f, DamageMultiplier = 1.65f, EffectDescription = "Đòn bổ mạnh làm giảm 30% giáp mục tiêu trong 6s." },
            new MartialSkillDefinition { Id = "pig.skill.chan_dia", Name = "Chấn Địa", School = MartialSchool.TrongKich_B, SlotType = CombatSlotType.TheBien, UnlockTier = 1, StaminaCost = 15f, CooldownSeconds = 6.5f, DamageMultiplier = 1.50f, EffectDescription = "Nện xuống đất tạo sóng xung kích 3.5m làm choáng nhẹ 1.2s." },
            new MartialSkillDefinition { Id = "pig.skill.tram_tuyen", Name = "Trảm Tuyến", School = MartialSchool.TrongKich_B, SlotType = CombatSlotType.TheCong, UnlockTier = 2, StaminaCost = 16f, CooldownSeconds = 6.0f, DamageMultiplier = 1.80f, EffectDescription = "Quét vòng cung rộng 160 độ đẩy lùi toàn bộ kẻ địch phía trước." },
            new MartialSkillDefinition { Id = "pig.skill.phan_chan", Name = "Phản Chấn", School = MartialSchool.TrongKich_B, SlotType = CombatSlotType.TheThu, UnlockTier = 2, StaminaCost = 12f, CooldownSeconds = 7.5f, DamageMultiplier = 1.40f, EffectDescription = "Gồng lực hấp thụ đòn đánh và phản lại sóng chấn động +22 Chống Chịu." },
            new MartialSkillDefinition { Id = "pig.skill.neo_kich", Name = "Neo Kích", School = MartialSchool.TrongKich_B, SlotType = CombatSlotType.TheBien, UnlockTier = 2, StaminaCost = 15f, CooldownSeconds = 8.0f, DamageMultiplier = 1.70f, EffectDescription = "Đóng cọc chấn địa ghim chặt chân quái tinh anh/UFO tầm thấp trong 3s." },
            new MartialSkillDefinition { Id = "pig.skill.thien_chuy", Name = "Thiên Chùy", School = MartialSchool.TrongKich_B, SlotType = CombatSlotType.TheCong, UnlockTier = 3, StaminaCost = 20f, RageCost = 50, CooldownSeconds = 16.0f, DamageMultiplier = 2.60f, EffectDescription = "Tuyệt kỹ Đỉnh Trọng Kích: Nhảy bổ từ không trung gây sát thương nổ diện rộng và phá khiên." },

            // Trường phái C — Khống Chế (6 kỹ năng)
            new MartialSkillDefinition { Id = "pig.skill.moc_keo", Name = "Móc Kéo", School = MartialSchool.KhongChe_C, SlotType = CombatSlotType.TheCong, UnlockTier = 1, StaminaCost = 13f, CooldownSeconds = 5.0f, DamageMultiplier = 1.35f, EffectDescription = "Phóng móc kéo Kẻ Bắt Heo hoặc quái từ khoảng cách 6m về phía mình." },
            new MartialSkillDefinition { Id = "pig.skill.an_tin_hieu", Name = "Ấn Tín Hiệu", School = MartialSchool.KhongChe_C, SlotType = CombatSlotType.TheBien, UnlockTier = 1, StaminaCost = 12f, CooldownSeconds = 6.0f, DamageMultiplier = 1.25f, EffectDescription = "Đánh dấu mục tiêu khiến công trình phòng thủ và thú cưng tập trung hỏa lực (+25% ST nhận)." },
            new MartialSkillDefinition { Id = "pig.skill.xoay_bay", Name = "Xoay Bầy", School = MartialSchool.KhongChe_C, SlotType = CombatSlotType.TheThu, UnlockTier = 2, StaminaCost = 12f, CooldownSeconds = 7.0f, DamageMultiplier = 1.20f, EffectDescription = "Điều hướng đàn heo quanh 12m dạt ra sau lưng nhân vật và hồi +10 Tinh thần." },
            new MartialSkillDefinition { Id = "pig.skill.troi_luoi", Name = "Trói Lưới", School = MartialSchool.KhongChe_C, SlotType = CombatSlotType.TheBien, UnlockTier = 2, StaminaCost = 14f, CooldownSeconds = 8.5f, DamageMultiplier = 1.30f, EffectDescription = "Tung lưới giữ chân nhóm quái trong bán kính 3m suốt 3.5s." },
            new MartialSkillDefinition { Id = "pig.skill.dung_chot", Name = "Dựng Chốt", School = MartialSchool.KhongChe_C, SlotType = CombatSlotType.TheThu, UnlockTier = 2, StaminaCost = 15f, CooldownSeconds = 12.0f, DamageMultiplier = 1.25f, EffectDescription = "Cắm cọc chắn tạm thời làm chậm 45% tốc độ quái đi qua trong 8s." },
            new MartialSkillDefinition { Id = "pig.skill.thien_la", Name = "Thiên La", School = MartialSchool.KhongChe_C, SlotType = CombatSlotType.TheBien, UnlockTier = 3, StaminaCost = 18f, RageCost = 50, CooldownSeconds = 15.0f, DamageMultiplier = 2.10f, EffectDescription = "Tuyệt kỹ Đỉnh Khống Chế: Trải trận lưới điện từ khóa chặt cả quái đất lẫn UFO tầm thấp." },

            // Trường phái D — Sinh Tồn (6 kỹ năng)
            new MartialSkillDefinition { Id = "pig.skill.tru_bo", Name = "Trụ Bộ", School = MartialSchool.SinhTon_D, SlotType = CombatSlotType.TheThu, UnlockTier = 1, StaminaCost = 10f, CooldownSeconds = 5.0f, DamageMultiplier = 1.20f, EffectDescription = "Đứng tấn vững chắc, miễn đẩy lùi và giảm 45% hao Chống Chịu trong 4s." },
            new MartialSkillDefinition { Id = "pig.skill.ho_than", Name = "Hộ Thân", School = MartialSchool.SinhTon_D, SlotType = CombatSlotType.TheThu, UnlockTier = 1, StaminaCost = 12f, CooldownSeconds = 6.5f, DamageMultiplier = 1.25f, EffectDescription = "Tạo lớp giáp hộ thân hồi ngay +28 Chống Chịu và xóa làm chậm." },
            new MartialSkillDefinition { Id = "pig.skill.khieu_chien", Name = "Khiêu Chiến", School = MartialSchool.SinhTon_D, SlotType = CombatSlotType.TheBien, UnlockTier = 2, StaminaCost = 13f, CooldownSeconds = 8.0f, DamageMultiplier = 1.30f, EffectDescription = "Hét lớn thu hút toàn bộ quái trong 10m bỏ mục tiêu heo/công trình để quay sang đánh mình." },
            new MartialSkillDefinition { Id = "pig.skill.hoi_khi", Name = "Hồi Khí", School = MartialSchool.SinhTon_D, SlotType = CombatSlotType.TheBien, UnlockTier = 2, StaminaCost = 8f, CooldownSeconds = 10.0f, DamageMultiplier = 1.15f, EffectDescription = "Điều tức hồi lại +30 Thể Lực tức thời và giảm -8 Tích tải ngày." },
            new MartialSkillDefinition { Id = "pig.skill.chan_mang", Name = "Chắn Mạng", School = MartialSchool.SinhTon_D, SlotType = CombatSlotType.TheCong, UnlockTier = 2, StaminaCost = 14f, CooldownSeconds = 9.0f, DamageMultiplier = 1.50f, EffectDescription = "Lao ra đỡ thay sát thương cho Thủ Lĩnh đàn hoặc cổng chính và phản đòn." },
            new MartialSkillDefinition { Id = "pig.skill.kim_cuong_the", Name = "Kim Cương Thể", School = MartialSchool.SinhTon_D, SlotType = CombatSlotType.TheThu, UnlockTier = 3, StaminaCost = 16f, RageCost = 50, CooldownSeconds = 18.0f, DamageMultiplier = 2.00f, EffectDescription = "Tuyệt kỹ Đỉnh Sinh Tồn: Hồi đầy 100% Thanh Chống Chịu và miễn ngất trong 5 giây." }
        };

        public static MartialSkillDefinition GetByIdOrFallback(string id, MartialSchool school, CombatSlotType slot)
        {
            for (int i = 0; i < AllSkills.Count; i++)
            {
                if (string.Equals(AllSkills[i].Id, id, StringComparison.OrdinalIgnoreCase))
                    return AllSkills[i];
            }
            for (int i = 0; i < AllSkills.Count; i++)
            {
                if (AllSkills[i].School == school && AllSkills[i].SlotType == slot)
                    return AllSkills[i];
            }
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
        public static readonly IReadOnlyList<EquipmentItem> AllEquipmentItems = new List<EquipmentItem>
        {
            // Vũ khí (4)
            new EquipmentItem { Id = "wpn_dao_rua", Name = "Dao Rựa", Slot = EquipSlot.Weapon, WeightScore = 1, Defense = 4, BonusStr = 2, BonusAgi = 2, SpecialMechanic = "ChemNhanh_PhaBui" },
            new EquipmentItem { Id = "wpn_giao_moc", Name = "Giáo Móc", Slot = EquipSlot.Weapon, WeightScore = 2, Defense = 6, BonusStr = 2, BonusCtrl = 3, SpecialMechanic = "KeoQuaiBatHeo" },
            new EquipmentItem { Id = "wpn_bua_ta", Name = "Búa Tạ", Slot = EquipSlot.Weapon, WeightScore = 3, Defense = 10, BonusStr = 4, BonusRes = 1, SpecialMechanic = "PhaGiap_DayLui" },
            new EquipmentItem { Id = "wpn_no_tay", Name = "Nỏ Tay", Slot = EquipSlot.Weapon, WeightScore = 1, Defense = 3, BonusAgi = 2, BonusCtrl = 3, SpecialMechanic = "BanTamXa_DanhDauUfo" },

            // Giày (2)
            new EquipmentItem { Id = "boot_giay_dinh", Name = "Giày Đinh", Slot = EquipSlot.Boots, WeightScore = 2, Defense = 8, BonusRes = 2, BonusCtrl = 1, SpecialMechanic = "KhongTruotBun_GiamDayLui" },
            new EquipmentItem { Id = "boot_guoc_gio", Name = "Guốc Gió", Slot = EquipSlot.Boots, WeightScore = 1, Defense = 5, BonusAgi = 3, SpecialMechanic = "TangTocChay_GiamHaoLuot" },

            // Giáp thân (4)
            new EquipmentItem { Id = "armor_giap_da_thuoc", Name = "Giáp Da Thuộc", Slot = EquipSlot.BodyArmor, WeightScore = 1, Defense = 12, BonusAgi = 1, BonusRes = 2, SpecialMechanic = "CanBang_Nhe" },
            new EquipmentItem { Id = "armor_giap_luoi_thep", Name = "Giáp Lưới Thép", Slot = EquipSlot.BodyArmor, WeightScore = 3, Defense = 22, BonusRes = 4, SpecialMechanic = "GiamSatThuongCanChien" },
            new EquipmentItem { Id = "armor_ao_mua_dau", Name = "Áo Mưa Dầu", Slot = EquipSlot.BodyArmor, WeightScore = 1, Defense = 10, BonusAgi = 1, BonusRes = 2, SpecialMechanic = "MienPhatMua_NomAm" },
            new EquipmentItem { Id = "armor_ao_chi", Name = "Áo Chì", Slot = EquipSlot.BodyArmor, WeightScore = 3, Defense = 18, BonusRes = 3, SpecialMechanic = "KhangMuaBucXa_Plasma" },

            // Quần (2)
            new EquipmentItem { Id = "pants_quan_da_lot_bong", Name = "Quần Da Lót Bông", Slot = EquipSlot.Pants, WeightScore = 1, Defense = 9, BonusRes = 2, SpecialMechanic = "KhangRetDam_GiamHaoTheLucDem" },
            new EquipmentItem { Id = "pants_quan_giap_xich", Name = "Quần Giáp Xích", Slot = EquipSlot.Pants, WeightScore = 2, Defense = 14, BonusRes = 2, BonusStr = 1, SpecialMechanic = "QuanGiapXich" },

            // Vòng tay (2)
            new EquipmentItem { Id = "brc_vong_cach_dien", Name = "Vòng Cách Điện", Slot = EquipSlot.Bracelet, WeightScore = 1, Defense = 5, BonusRes = 2, BonusCtrl = 1, SpecialMechanic = "GiamSatThuongSet_SuaMachDien" },
            new EquipmentItem { Id = "brc_vong_huyet_nha", Name = "Vòng Huyết Nha", Slot = EquipSlot.Bracelet, WeightScore = 1, Defense = 4, BonusStr = 2, BonusCtrl = 2, SpecialMechanic = "TichNoNhanh" },

            // Vòng cổ (2 — tổng 16 món bao gồm cả 2 vòng cổ GDD 12.3)
            new EquipmentItem { Id = "nck_coi_chan_heo", Name = "Còi Chăn Heo", Slot = EquipSlot.Necklace, WeightScore = 1, Defense = 4, BonusCtrl = 3, SpecialMechanic = "TangBanKinhLuaDan_5m" },
            new EquipmentItem { Id = "nck_mat_day_den_bao", Name = "Mặt Dây Đèn Bão", Slot = EquipSlot.Necklace, WeightScore = 1, Defense = 5, BonusCtrl = 2, BonusRes = 1, SpecialMechanic = "GiamPhatTamNhinDem_SuongMu" }
        };

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

        public static readonly IReadOnlyList<GameItem> AllTwentyTacticalItems = new List<GameItem>
        {
            // Nhóm Trang trại (5)
            new GameItem { Id = "XoNuoc", Name = "Xô Nước", Tier = ItemTier.I, ReferenceValueGold = 45, Category = "Farm", Description = "Chữa cháy công trình hoặc làm mát tạm 1 cụm heo." },
            new GameItem { Id = "MaiCheTam", Name = "Mái Che Tạm", Tier = ItemTier.I, ReferenceValueGold = 120, Category = "Farm", Description = "Che mưa/nắng cho 1 ô chuồng trong 1 ngày." },
            new GameItem { Id = "CamLinhChi", Name = "Cám Linh Chi", Tier = ItemTier.II, ReferenceValueGold = 260, Category = "Farm", Description = "+15% tăng trọng và +10 Tinh thần trong 1 ngày." },
            new GameItem { Id = "CamHaHoa", Name = "Cám Hạ Hỏa", Tier = ItemTier.II, ReferenceValueGold = 240, Category = "Farm", Description = "Giúp thuần dưỡng dòng Xích Mao và giảm hoảng sợ." },
            new GameItem { Id = "ThuocAnThanHeo", Name = "Thuốc An Thần Heo", Tier = ItemTier.II, ReferenceValueGold = 210, Category = "Farm", Description = "Đưa 1 heo Hoảng loạn về Bình ổn ngay lập tức." },

            // Nhóm Thông tin (4)
            new GameItem { Id = "TinDonCho", Name = "Tin Đồn Chợ", Tier = ItemTier.I, ReferenceValueGold = 150, Category = "Info", Description = "Biết trước chủ đề Chợ Ngày của chu kỳ kế tiếp." },
            new GameItem { Id = "LaDoSuong", Name = "Lá Dò Sương", Tier = ItemTier.II, ReferenceValueGold = 320, Category = "Info", Description = "Phát hiện vùng ẩn có Cổng trong bán kính 40m." },
            new GameItem { Id = "BanDoHuongQuai", Name = "Bản Đồ Hướng Quái", Tier = ItemTier.II, ReferenceValueGold = 280, Category = "Info", Description = "+1 bậc Tình báo cho đợt quái sắp tới." },
            new GameItem { Id = "HoSoGiongLoai", Name = "Hồ Sơ Giống Loài", Tier = ItemTier.III, ReferenceValueGold = 950, Category = "Info", Description = "Giám định chắc chắn 100% 1 heo chưa định danh." },

            // Nhóm Chiến đấu (6)
            new GameItem { Id = "BinhHoiLuc", Name = "Bình Hồi Lực", Tier = ItemTier.I, ReferenceValueGold = 90, Category = "Combat", Description = "Hồi ngay +40 Thể lực tức thời." },
            new GameItem { Id = "BangGac", Name = "Băng Gạc", Tier = ItemTier.I, ReferenceValueGold = 85, Category = "Combat", Description = "Hồi +35 máu/Chống chịu trong 5 giây." },
            new GameItem { Id = "LuuKhoi", Name = "Lựu Khói", Tier = ItemTier.II, ReferenceValueGold = 180, Category = "Combat", Description = "Che khuất đàn heo trong 6 giây, giảm khả năng bị nhắm." },
            new GameItem { Id = "BomKeo", Name = "Bom Keo", Tier = ItemTier.II, ReferenceValueGold = 190, Category = "Combat", Description = "Làm chậm 50% vùng 4m trong 5 giây." },
            new GameItem { Id = "PhaoSang", Name = "Pháo Sáng", Tier = ItemTier.I, ReferenceValueGold = 110, Category = "Combat", Description = "Soi sáng vùng tối 18m trong 20 giây, làm lộ Bóng Huyền." },
            new GameItem { Id = "TinhNoDan", Name = "Tinh Nộ Đan", Tier = ItemTier.II, ReferenceValueGold = 310, Category = "Combat", Description = "+50 Nộ khí ngay lập tức." },

            // Nhóm Sự kiện & Nghiên cứu (5 -> tổng 20 vật phẩm)
            new GameItem { Id = "BuaHoiHuong", Name = "Bùa Hồi Hương", Tier = ItemTier.III, ReferenceValueGold = 680, Category = "Event", Description = "Rút khỏi Cổng ngay lập tức, giữ lại 50% phần thưởng đã đạt." },
            new GameItem { Id = "LaBanTanVu", Name = "La Bàn Tán Vụ", Tier = ItemTier.III, ReferenceValueGold = 720, Category = "Event", Description = "Kết thúc sớm Sương Mù Dị Giới từ ngày thứ 2 trở đi." },
            new GameItem { Id = "NgocPhaSuong", Name = "Ngọc Phá Sương", Tier = ItemTier.II, ReferenceValueGold = 450, Category = "Event", Description = "Xóa sương mù trong bán kính 25m suốt 60 giây." },
            new GameItem { Id = "ChiaCongNguoc", Name = "Chìa Cổng Ngược", Tier = ItemTier.III, ReferenceValueGold = 820, Category = "Event", Description = "Giữ một Cổng chưa đóng tồn tại thêm 6 giờ." },
            new GameItem { Id = "MauSinhHoc", Name = "Mẫu Sinh Học", Tier = ItemTier.II, ReferenceValueGold = 220, Category = "Research", Description = "Nguyên liệu dùng tại Phòng Nghiên Cứu để giám định gen và điều chế thuốc." }
        };
    }
}

