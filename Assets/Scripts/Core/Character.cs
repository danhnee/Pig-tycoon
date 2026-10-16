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
        public int ReboundFatigueStacks = 0;
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

        /// <summary>
        /// Thực thi kỹ năng võ học ở 3 ô Thế Công / Thế Thủ / Thế Biến (GDD 12.4 - 12.5).
        /// Thế Thủ hỗ trợ Huyết Tế (dùng Chống Chịu/Máu bù Thể Lực theo tỉ lệ 1:2 khi thiếu Stamina).
        /// </summary>
        public (bool executed, string message, float damageMultiplier) TryExecuteMartialSkill(CombatSlotType slot, bool allowBloodSacrifice = true)
        {
            if (!IsMartialSlotUnlocked(slot))
            {
                return (false, $"Ô [{slot}] bị khóa! Cần mặc đủ cặp trang bị tương ứng.", 1.0f);
            }

            float staRequired = slot switch
            {
                CombatSlotType.TheCong => 14f,
                CombatSlotType.TheThu => 12f,
                CombatSlotType.TheBien => 15f,
                _ => 14f
            };

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
                        return (true, $"[HUYẾT TẾ · THẾ THỦ] Hiến {guardSacrifice:0} Chống Chịu phát động kỹ năng phòng ngự phản chấn!", 1.35f);
                    }
                }
                return (false, $"Không đủ Thể Lực ({Stamina.CurrentStamina:0}/{staRequired:0}) để thi triển [{slot}]!", 1.0f);
            }

            TryConsumeCombatStamina(staRequired, 0.35f, out _);
            if (slot == CombatSlotType.TheThu)
            {
                CurrentGuard = Math.Min(MaxGuard, CurrentGuard + 22f);
                return (true, $"Thi triển [{slot}] ({PrimarySchool}): Hồi +22 Chống Chịu & tạo khiên phản chấn!", 1.25f);
            }
            if (slot == CombatSlotType.TheBien)
            {
                Rage = Math.Min(100, Rage + 15);
                return (true, $"Thi triển [{slot}] ({PrimarySchool}): Lướt biến chiêu chuẩn xác (+15 Nộ Khí)!", 1.45f);
            }
            return (true, $"Thi triển [{slot}] ({PrimarySchool}): Đòn Thế Công uy lực!", 1.65f);
        }

        public bool EngraveSeal(string sealId)
        {
            if (EngravedSeals.Contains(sealId)) return true;
            if (EngravedSeals.Count >= 3) return false;
            EngravedSeals.Add(sealId);
            RecalculateBonusStats();
            return true;
        }
    }
}
