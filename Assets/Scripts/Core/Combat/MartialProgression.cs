using System;
using System.Collections.Generic;
using System.Linq;

namespace PigTycoon.Core
{
    public enum WeaponArchetype { None, Slash, Thrust, Smash, Ranged }
    public enum SkillStat { Str, Agi, Ctrl, Res }

    /// <summary>What a skill does in the world; the runtime maps each shape to hit shapes and effects.</summary>
    public enum SkillShape
    {
        Dash, Flurry, DashThrough, Rescue, Teleport, Decoy,
        ArmorBreak, PushCircle, LineSweep, Counter, Pin, Execute,
        Pull, Mark, ConeTurn, Root, Stakes, SlowField,
        Stance, Shield, Taunt, Passive, Intercept, Fortify
    }

    public enum SchoolRelation { Main, Linked, Other }
    public enum KillClass { Normal, Elite, Boss }

    public sealed class SkillCastResult
    {
        public bool Ok;
        public string Message = "";
        public MartialSkillDefinition? Skill;
        public float DamageMultiplier = 1f;
        public string Combo = "";
        public float StaminaPaid;
        public int HpPaid;
        public int RagePaid;
        /// <summary>Hợp kích effects (GDD 12.6): Ảnh Móc pull ×2, Trói Chùy rooted ×1.3, Phản Thủ shield ×1.5, Thủ Chuyển Công crit.</summary>
        public float RangeMultiplier = 1f;
        public float RootedTargetMultiplier = 1f;
        public float ShieldMultiplier = 1f;
        public bool GuaranteedCritLastHit;
    }

    /// <summary>
    /// GDD 8.2 and 12: character level and stat points, buying and wearing the 6 equipment pieces, learning and slotting the
    /// 24 skills, casting them (Stamina / Huyết Tế HP / Nộ Khí, cooldowns, play-style and school modifiers, hợp kích) and the
    /// Nộ / Điểm Võ economy. Numbers the GDD does not give are listed in ADR-0017 as PROVISIONAL.
    /// </summary>
    public static class MartialProgression
    {
        public const int MaxLevel = 30;
        public const int StatSoftCap = 40;
        public const int StatHardCap = 55;
        public const double ComboWindowSeconds = 2.0;

        // ------------------------------------------------------------------ schools
        /// <summary>The one-way ring A → C → B → D → A (GDD 12.6).</summary>
        static readonly MartialSchool[] Ring = { MartialSchool.TocBo_A, MartialSchool.KhongChe_C, MartialSchool.TrongKich_B, MartialSchool.SinhTon_D };

        /// <summary>
        /// GDD 12.6 "Vòng một chiều A → C → B → D → A: học phái A dùng được kỹ năng A và C; C dùng C và B; B dùng B và D;
        /// D dùng D và A." Linked = the NEXT school on the ring only. Each school's hợp kích goes main → linked.
        /// </summary>
        public static SchoolRelation Relation(MartialSchool main, MartialSchool other)
        {
            if (main == other) return SchoolRelation.Main;
            int d = (Array.IndexOf(Ring, other) - Array.IndexOf(Ring, main) + 4) % 4;
            return d == 1 ? SchoolRelation.Linked : SchoolRelation.Other;
        }

        public static MartialSchool LinkedSchool(MartialSchool main) => Ring[(Array.IndexOf(Ring, main) + 1) % 4];

        /// <summary>The one hợp kích each main school owns (main skill → linked-school skill, GDD 12.6).</summary>
        public static (string name, string first, string second, string effect) SchoolCombo(MartialSchool main) => main switch
        {
            MartialSchool.TocBo_A => ("Ảnh Móc", "pig.skill.bo_phap_luot", "pig.skill.moc_keo", "Bộ Pháp Lướt → Móc Kéo trong 2 giây: kéo xa gấp đôi"),
            MartialSchool.KhongChe_C => ("Trói Chùy", "pig.skill.troi_luoi", "pig.skill.chan_dia", "Trói Lưới → Chấn Địa: mục tiêu bị trói nhận ×1,3"),
            MartialSchool.TrongKich_B => ("Phản Thủ", "pig.skill.phan_chan", "pig.skill.ho_than", "Phản Chấn → Hộ Thân: khiên ×1,5"),
            _ => ("Thủ Chuyển Công", "pig.skill.tru_bo", "pig.skill.doan_anh_tram", "Trụ Bộ → Đoạn Ảnh Trảm: nhát cuối chắc chắn chí mạng"),
        };

        /// <summary>
        /// Can the slotted loadout actually chain this school's hợp kích? Both skills must sit in different open slots;
        /// same-tag pairs (Ảnh Móc Công→Công, Phản Thủ Thủ→Thủ) therefore need the 4th slot from Ấn Song Thế.
        /// </summary>
        public static bool ComboReady(CharacterData c, string first, string second)
        {
            CombatSlotType? a = null, b = null;
            foreach (var slot in AllSlots)
            {
                if (!c.IsMartialSlotUnlocked(slot)) continue;
                var id = SlottedId(c, slot);
                if (id == first && a == null) a = slot;
                else if (id == second && b == null) b = slot;
            }
            return a != null && b != null && a != b;
        }

        public static readonly CombatSlotType[] AllSlots = { CombatSlotType.TheCong, CombatSlotType.TheThu, CombatSlotType.TheBien, CombatSlotType.TheSongThe };

        public static SkillStat SchoolStat(MartialSchool s) => s switch
        {
            MartialSchool.TocBo_A => SkillStat.Agi,
            MartialSchool.TrongKich_B => SkillStat.Str,
            MartialSchool.KhongChe_C => SkillStat.Ctrl,
            _ => SkillStat.Res,
        };

        public static string SchoolName(MartialSchool s) => s switch
        {
            MartialSchool.TocBo_A => "A — Tốc Bộ",
            MartialSchool.TrongKich_B => "B — Trọng Kích",
            MartialSchool.KhongChe_C => "C — Khống Chế",
            _ => "D — Sinh Tồn",
        };

        public static string SlotName(CombatSlotType s) => s switch
        {
            CombatSlotType.TheCong => "Thế Công",
            CombatSlotType.TheThu => "Thế Thủ",
            CombatSlotType.TheBien => "Thế Biến",
            _ => "Song Thế",
        };

        public static string SlotPairText(CombatSlotType s) => s switch
        {
            CombatSlotType.TheCong => "Vũ khí + Giày",
            CombatSlotType.TheThu => "Giáp thân + Quần giáp",
            CombatSlotType.TheBien => "Vòng tay + Vòng cổ",
            _ => "Mệnh Ấn Song Thế",
        };

        /// <summary>Ô Song Thế trả bằng Nộ: 1 Thể lực của kỹ năng = 1 Nộ (ADR-0017, PROVISIONAL).</summary>
        public static int SongTheRageCost(MartialSkillDefinition s) =>
            s.RageCost > 0 ? s.RageCost : (int)Math.Ceiling(Math.Max(s.StaminaCost, s.HpCost * 2f));

        public static int BaseStat(CharacterData c, SkillStat s) => s switch
        {
            SkillStat.Str => c.BaseStats.Str,
            SkillStat.Agi => c.BaseStats.Agi,
            SkillStat.Ctrl => c.BaseStats.Ctrl,
            _ => c.BaseStats.Res,
        };

        public static int BonusStat(CharacterData c, SkillStat s) => s switch
        {
            SkillStat.Str => c.BonusStats.Str,
            SkillStat.Agi => c.BonusStats.Agi,
            SkillStat.Ctrl => c.BonusStats.Ctrl,
            _ => c.BonusStats.Res,
        };

        static void SetBaseStat(CharacterData c, SkillStat s, int v)
        {
            switch (s)
            {
                case SkillStat.Str: c.BaseStats.Str = v; break;
                case SkillStat.Agi: c.BaseStats.Agi = v; break;
                case SkillStat.Ctrl: c.BaseStats.Ctrl = v; break;
                default: c.BaseStats.Res = v; break;
            }
        }

        public static string StatName(SkillStat s) => s switch
        {
            SkillStat.Str => "STR",
            SkillStat.Agi => "AGI",
            SkillStat.Ctrl => "CTRL",
            _ => "RES",
        };

        // ------------------------------------------------------------------ play style (GDD 12.2)
        public static string PlaystyleName(CharacterData c) => c.GetPlaystyle() switch
        {
            "KhinhThân" => "Khinh Thân",
            "CânBằng" => "Cân Bằng",
            _ => "Trọng Giáp",
        };

        public static bool StyleMatches(string style, MartialSchool school) => style switch
        {
            "KhinhThân" => school == MartialSchool.TocBo_A,
            "CânBằng" => school == MartialSchool.KhongChe_C,
            _ => school == MartialSchool.TrongKich_B || school == MartialSchool.SinhTon_D,
        };

        /// <summary>Stamina factor for a skill: style match −10% / mismatch +15% (GDD 12.2); linked school +10%, other +20% (12.6).</summary>
        public static float StaminaFactor(CharacterData c, MartialSkillDefinition s)
        {
            float f = StyleMatches(c.GetPlaystyle(), s.School) ? 0.90f : 1.15f;
            var rel = Relation(c.PrimarySchool, s.School);
            if (rel == SchoolRelation.Linked) f *= 1.10f;
            else if (rel == SchoolRelation.Other) f *= 1.20f;
            return f;
        }

        public static float CooldownFor(CharacterData c, MartialSkillDefinition s)
        {
            float cd = s.CooldownSeconds;
            if (c.GetPlaystyle() == "CânBằng" && s.SlotType == CombatSlotType.TheBien) cd *= 0.90f;
            return cd;
        }

        // ------------------------------------------------------------------ learning (GDD 12.6)
        public static (int gold, int points) LearnCost(CharacterData c, MartialSkillDefinition s)
        {
            var (gold, points) = s.UnlockTier switch
            {
                1 => (800, 10),
                2 => (2500, 30),
                3 => (6000, 60),
                _ => (15000, 120),
            };
            if (Relation(c.PrimarySchool, s.School) == SchoolRelation.Linked) gold = (int)Math.Round(gold * 1.25);
            return (gold, points);
        }

        /// <summary>Requirement on base stats only (GDD 8 rule): main school −15%, linked school ≥30/≥34, other schools ≥38.</summary>
        public static int RequiredStat(CharacterData c, MartialSkillDefinition s, out int required2)
        {
            var rel = Relation(c.PrimarySchool, s.School);
            required2 = s.ReqValue2;
            if (rel == SchoolRelation.Main)
            {
                required2 = (int)Math.Ceiling(s.ReqValue2 * 0.85);
                return (int)Math.Ceiling(s.ReqValue * 0.85);
            }
            if (rel == SchoolRelation.Linked) return Math.Max(s.ReqValue, s.UnlockTier == 1 ? 30 : 34);
            return Math.Max(s.ReqValue, 38);
        }

        public static (bool ok, string reason) CanLearn(CharacterData c, MartialSkillDefinition s)
        {
            if (c.LearnedSkills.Contains(s.Id)) return (false, "Đã học.");
            var rel = Relation(c.PrimarySchool, s.School);
            if (s.IsUltimate && rel != SchoolRelation.Main) return (false, "Tuyệt kỹ chỉ học ở phái chính.");
            if (rel == SchoolRelation.Linked && s.UnlockTier > 2) return (false, "Phái liên kết chỉ học Cấp 1–2.");
            if (rel == SchoolRelation.Other)
            {
                if (s.UnlockTier > 1) return (false, "Phái khác chỉ học 1 kỹ năng Cấp 1.");
                if (c.LearnedSkills.Any(id => MartialSkillCatalog.Find(id)?.School == s.School))
                    return (false, "Đã học 1 kỹ năng của phái này.");
            }
            if ((s.IsUltimate || rel == SchoolRelation.Other) && c.SecretManuals <= 0)
                return (false, s.IsUltimate ? "Cần Bí Kíp đấu giá." : "Cần Bí Kíp bậc III.");

            int need = RequiredStat(c, s, out int need2);
            if (BaseStat(c, s.ReqStat) < need) return (false, $"Cần {StatName(s.ReqStat)} gốc {need} (đang {BaseStat(c, s.ReqStat)}).");
            if (s.ReqValue2 > 0 && BaseStat(c, s.ReqStat2) < need2)
                return (false, $"Cần {StatName(s.ReqStat2)} gốc {need2} (đang {BaseStat(c, s.ReqStat2)}).");
            var (gold, points) = LearnCost(c, s);
            if (c.MartialPoints < points) return (false, $"Cần {points} Điểm Võ (đang {c.MartialPoints}).");
            return (true, $"{gold:N0}G + {points} Điểm Võ");
        }

        public static (bool ok, string message) Learn(GameEngine e, string skillId)
        {
            var c = e.Character;
            var s = MartialSkillCatalog.Find(skillId);
            if (s == null) return (false, "Không có kỹ năng này.");
            var (can, reason) = CanLearn(c, s);
            if (!can) return (false, $"{s.Name}: {reason}");
            var (gold, points) = LearnCost(c, s);
            if (!e.Ledger.TrySpend(gold, LedgerReason.MartialTraining, "learn_" + s.Id)) return (false, $"Không đủ vàng ({gold:N0}G).");
            c.MartialPoints -= points;
            if ((s.IsUltimate || Relation(c.PrimarySchool, s.School) == SchoolRelation.Other) && c.SecretManuals > 0) c.SecretManuals--;
            c.LearnedSkills.Add(s.Id);
            if (!s.IsPassive && string.IsNullOrEmpty(SlottedId(c, s.SlotType))) c.ActiveMartialSkills[s.SlotType] = s.Id;
            return (true, $"Đã học {s.Name} ({SlotName(s.SlotType)}).");
        }

        public static string SlottedId(CharacterData c, CombatSlotType slot) =>
            c.ActiveMartialSkills.TryGetValue(slot, out var id) && id != null ? id : "";

        public static MartialSkillDefinition? Slotted(CharacterData c, CombatSlotType slot) => MartialSkillCatalog.Find(SlottedId(c, slot));

        public static (bool ok, string message) Assign(CharacterData c, CombatSlotType slot, string skillId)
        {
            var s = MartialSkillCatalog.Find(skillId);
            if (s == null || !c.LearnedSkills.Contains(s.Id)) return (false, "Chưa học kỹ năng này.");
            if (s.IsPassive) return (false, $"{s.Name} là bị động, không cần lắp.");
            if (slot != CombatSlotType.TheSongThe && s.SlotType != slot) return (false, $"{s.Name} thuộc ô {SlotName(s.SlotType)}.");
            c.ActiveMartialSkills[slot] = s.Id;
            return (true, $"Lắp {s.Name} vào {SlotName(slot)}.");
        }

        public static bool HasPassive(CharacterData c, string skillId) => c.LearnedSkills.Contains(skillId);

        // ------------------------------------------------------------------ casting
        public static float CooldownRemaining(CharacterData c, CombatSlotType slot, double now) =>
            CooldownRemaining(c, SlottedId(c, slot), now);

        public static float CooldownRemaining(CharacterData c, string skillId, double now) =>
            !string.IsNullOrEmpty(skillId) && c.SkillReadyAt.TryGetValue(skillId, out var t) ? (float)Math.Max(0, t - now) : 0f;

        public static float CooldownRemaining01(CharacterData c, CombatSlotType slot, double now)
        {
            var s = Slotted(c, slot);
            if (s == null) return 0f;
            float cd = CooldownFor(c, s);
            return cd <= 0f ? 0f : Math.Min(1f, CooldownRemaining(c, slot, now) / cd);
        }

        public static SkillCastResult Cast(CharacterData c, CombatSlotType slot, double now, bool allowBloodSacrifice = true)
        {
            c.Now = now;
            var r = new SkillCastResult();
            if (!c.IsMartialSlotUnlocked(slot)) { r.Message = $"Ô {SlotName(slot)} khóa: mặc đủ {SlotPairText(slot)}."; return r; }
            var s = Slotted(c, slot);
            if (s == null) { r.Message = $"Chưa lắp võ kỹ vào ô {SlotName(slot)} (phím C)."; return r; }
            if (s.IsPassive) { r.Message = $"{s.Name} là bị động."; return r; }
            float left = CooldownRemaining(c, slot, now);
            if (left > 0f) { r.Message = $"{s.Name} hồi chiêu {left:0.0}s."; return r; }
            bool songThe = slot == CombatSlotType.TheSongThe;
            int rageCost = songThe ? SongTheRageCost(s) : s.RageCost;
            if (c.Rage < rageCost) { r.Message = $"{s.Name} cần {rageCost} Nộ (đang {c.Rage})."; return r; }

            // Hợp kích (GDD 12.6): each named pair, within the 2-second window.
            bool inWindow = now - c.LastCastAt <= ComboWindowSeconds;
            string prev = inWindow ? c.LastCastSkillId : "";
            float sta = songThe ? 0f : s.StaminaCost * StaminaFactor(c, s) * DashFactor(c, s);
            if (prev == "pig.skill.bo_phap_luot" && s.Id == "pig.skill.moc_keo") { r.Combo = "Ảnh Móc"; r.RangeMultiplier = 2f; }
            else if (prev == "pig.skill.troi_luoi" && s.Id == "pig.skill.chan_dia") { r.Combo = "Trói Chùy"; r.RootedTargetMultiplier = 1.3f; }
            else if (prev == "pig.skill.phan_chan" && s.Id == "pig.skill.ho_than") { r.Combo = "Phản Thủ"; r.ShieldMultiplier = 1.5f; }
            else if (prev == "pig.skill.tru_bo" && s.Id == "pig.skill.doan_anh_tram") { r.Combo = "Thủ Chuyển Công"; r.GuaranteedCritLastHit = true; }

            // Pay: Stamina, or HP by Huyết Tế on Thế Thủ, or Nộ.
            if (sta > 0f)
            {
                if (c.Stamina.CurrentStamina >= sta)
                {
                    c.TryConsumeCombatStamina(sta, 0.35f, out float paid);
                    r.StaminaPaid = paid;
                }
                else if (slot == CombatSlotType.TheThu && allowBloodSacrifice)
                {
                    int hp = s.HpCost > 0 ? s.HpCost : (int)Math.Ceiling(sta / 2f);
                    // Không có HP (thiết kế người dùng): Huyết Tế hiến Chống Chịu, 1 điểm thay 2 Thể Lực
                    if (c.CurrentGuard <= hp + 5f) { r.Message = $"{s.Name}: không đủ Thể Lực ({c.Stamina.CurrentStamina:0}/{sta:0}) và Chống Chịu để Huyết Tế."; return r; }
                    c.CurrentGuard -= hp;
                    c.SyncLowGuardStamina();
                    r.HpPaid = hp;
                }
                else { r.Message = $"{s.Name}: không đủ Thể Lực ({c.Stamina.CurrentStamina:0}/{sta:0})."; return r; }
            }
            if (rageCost > 0) { c.Rage -= rageCost; r.RagePaid = rageCost; }

            c.SkillReadyAt[s.Id] = now + CooldownFor(c, s);
            c.LastCastSkillId = s.Id;
            c.LastCastAt = now;
            c.LastExecutedSchool = s.School;

            switch (s.Shape)
            {
                case SkillShape.Shield:
                    c.ShieldHp = s.Magnitude * r.ShieldMultiplier;
                    c.ShieldUntil = now + s.Duration;
                    break;
                case SkillShape.Fortify:
                    c.FortifyFactor = s.Magnitude;
                    c.FortifyUntil = now + s.Duration;
                    break;
                case SkillShape.Stance:
                    c.StanceUntil = now + s.Duration;
                    break;
                case SkillShape.Counter:
                    c.CounterFactor = s.Magnitude;
                    c.CounterUntil = now + s.Duration;
                    break;
            }

            r.Ok = true;
            r.Skill = s;
            r.DamageMultiplier = s.DamageMultiplier;
            string price = r.HpPaid > 0 ? $"Huyết Tế −{r.HpPaid} Chống Chịu" : r.RagePaid > 0 ? $"−{r.RagePaid} Nộ" : $"−{r.StaminaPaid:0} Thể Lực";
            r.Message = $"{s.Name} ({price})" + (r.Combo.Length > 0 ? $" · Hợp kích {r.Combo}" : "");
            return r;
        }

        /// <summary>Phản Chấn window: a hit inside it is blocked and reflected at 150% (GDD 12.7).</summary>
        public static bool TryCounter(CharacterData c, double now, float incoming, out float reflected)
        {
            reflected = 0f;
            if (now >= c.CounterUntil) return false;
            reflected = incoming * c.CounterFactor;
            c.CounterUntil = -1;
            return true;
        }

        public static bool IsUnstoppable(CharacterData c, double now) => now < c.StanceUntil;

        // ------------------------------------------------------------------ Nộ, Điểm Võ, EXP from fights
        /// <summary>GDD 12.1 Nộ: +2 per normal kill, +10 per elite (Vòng Huyết Nha +50%); 12.6 Điểm Võ: +1/20 normal, +5 elite, +25 Boss.</summary>
        public static void OnKill(CharacterData c, KillClass kind)
        {
            int rage = kind == KillClass.Normal ? 2 : 10;
            if (kind != KillClass.Normal && Wears(c, "HuyetNha")) rage = (int)Math.Round(rage * 1.5);
            c.Rage = Math.Min(100, c.Rage + rage);
            if (kind == KillClass.Normal)
            {
                c.NormalKillsTowardMartialPoint++;
                if (c.NormalKillsTowardMartialPoint >= 20) { c.NormalKillsTowardMartialPoint -= 20; c.MartialPoints++; }
            }
            else c.MartialPoints += kind == KillClass.Elite ? 5 : 25;
            AddExp(c, KillExp(kind));
        }

        /// <summary>Nộ fades 5/s once out of combat for 10 s (GDD 12.1).</summary>
        public static void TickRage(CharacterData c, float dt, float secondsSinceCombat, bool waveActive)
        {
            if (waveActive || secondsSinceCombat < 10f || c.Rage <= 0) { c.RageDecayCarry = 0f; return; }
            c.RageDecayCarry += 5f * dt;
            int drop = (int)c.RageDecayCarry;
            if (drop <= 0) return;
            c.RageDecayCarry -= drop;
            c.Rage = Math.Max(0, c.Rage - drop);
        }

        public static void OnWaveEnded(CharacterData c, int cycle)
        {
            c.Rage = 0; // GDD 12.1: về 0 khi hết đợt quái
            c.AnBatKhuatTriggeredThisWave = false;
            AddExp(c, WaveSurvivalExp(cycle));
        }

        public static bool Wears(CharacterData c, string mechanic) =>
            c.Equipment.Values.Any(i => i != null && i.SpecialMechanic == mechanic);

        // ------------------------------------------------------------------ lướt né, khống chế, đổi phái
        /// <summary>Lướt (GDD 12.2): Khinh Thân −20% Stamina, Trọng Giáp +25%. Applies to dodge and dash-shaped skills.</summary>
        public static float DashFactor(CharacterData c, MartialSkillDefinition? s = null)
        {
            if (s != null && s.Shape != SkillShape.Dash && s.Shape != SkillShape.DashThrough) return 1f;
            return c.GetPlaystyle() switch { "KhinhThân" => 0.80f, "TrọngGiáp" => 1.25f, _ => 1f };
        }

        /// <summary>
        /// GDD 9.2 lướt né: 9 Stamina, AGI gốc ≥ 35 → −15%, × lối chơi; Guốc Gió: lướt lần 2 trong 1 giây miễn phí.
        /// Returns the stamina paid, or −1 when the player cannot afford it.
        /// </summary>
        public static float TryDodge(CharacterData c, double now)
        {
            bool free = Wears(c, "LuotKep") && now - c.LastDodgeAt <= 1.0;
            c.LastDodgeAt = now;
            if (free) return 0f;
            float cost = 9f * (c.BaseStats.Agi >= 35 ? 0.85f : 1f) * DashFactor(c);
            if (c.Stamina.CurrentStamina < cost) return -1f;
            c.TryConsumeCombatStamina(cost, 0.30f, out float paid);
            return paid;
        }

        /// <summary>CTRL: "hiệu lực choáng/làm chậm" (GDD 8.2) — +0,2%/điểm thời gian khống chế (ADR-0017, cùng hệ số chí mạng).</summary>
        public static float ControlSeconds(CharacterData c, float seconds) =>
            seconds * (1f + (c.BaseStats.Ctrl + c.BonusStats.Ctrl) * 0.002f);

        /// <summary>GDD 12.6: đổi phái chính 20.000G, 1 lần / 3 chu kỳ. Learned skills stay; their relation follows the new main.</summary>
        public static readonly (string id, string name, string text)[] RuneIds =
        {
            ("PhuTruyPhong", "Truy Phong", "giày: lướt để lại vệt làm chậm"),
            ("PhuNoHoa", "Nộ Hỏa", "vòng tay: +1 Nộ mỗi đòn trúng"),
            ("PhuKienThach", "Kiên Thạch", "giáp: đỡ đòn giảm thêm 10%"),
        };

        public static readonly (string id, string name, string text)[] SealIds =
        {
            ("AnBatKhuat", "Bất Khuất", "1 lần/đợt giữ lại 15 Chống Chịu"),
            ("AnSongThe", "Song Thế", "mở ô võ kỹ thứ 4 (R) dùng Nộ"),
            ("AnMucDong", "Mục Đồng", "Tiếng Huýt hồi chiêu −50%"),
        };

        /// <summary>GDD 12.3 Còi Chăn Heo: heo Hoảng loạn trong 10 m hồi +10 Tinh thần, 1 lần / 2 phút.</summary>
        public const double CoiChanHeoCooldownSeconds = 120.0;

        public static int TryCalmWithHerdingWhistle(CharacterData c, IEnumerable<Pig> pigsWithin10m, double now, ref double readyAt)
        {
            if (!Wears(c, "CoiChanHeo") || now < readyAt) return 0;
            int n = 0;
            foreach (var p in pigsWithin10m)
            {
                if (p == null || !p.IsAlive || p.MoodState != MoodState.HoangLoan) continue;
                p.Mood = Math.Min(100, p.Mood + 10);
                n++;
            }
            if (n > 0) readyAt = now + CoiChanHeoCooldownSeconds;
            return n;
        }

        /// <summary>Hệ Phù Văn đã mở (GDD 12.4): Cổng Giữa Game + ≥ 12 đợt lớn + cấp ≥ 15 + Phòng Xây Dựng cấp 2 + có Bàn Khắc.</summary>
        public static bool RuneSystemOpen(GameEngine e) =>
            e.DeepMechanics.MidGameGateUnlocked && e.MajorWavesSurvived >= 12 && e.Character.Level >= 15 &&
            e.Defense.HallLevel >= 2 && e.CoreAndVillage.HasHighTierEngravingTableBlueprint;

        /// <summary>Daily: counts cycles since the rune system opened (Mệnh Ấn needs ≥ 5 such cycles).</summary>
        public static void TickRuneCycles(GameEngine e)
        {
            var w = e.CoreAndVillage;
            if (!RuneSystemOpen(e)) return;
            if (w.RuneSystemLastCountedCycle == 0) { w.RuneSystemLastCountedCycle = e.Clock.CycleNumber; return; }
            if (e.Clock.CycleNumber > w.RuneSystemLastCountedCycle)
            {
                w.CyclesSinceRuneSystemUnlocked += e.Clock.CycleNumber - w.RuneSystemLastCountedCycle;
                w.RuneSystemLastCountedCycle = e.Clock.CycleNumber;
            }
        }

        /// <summary>
        /// GDD 24 sự kiện Đỏ (E10–E14) đếm cho điều kiện Mệnh Ấn; GDD 16 (bảng nguồn bản vẽ): bản vẽ Cao cấp, gồm Bàn Khắc,
        /// đến từ sự kiện Đỏ sau Cổng Giữa Game. Tài liệu không ghi tỉ lệ rơi → tạm: sự kiện Đỏ đầu tiên sau Cổng trao Bàn Khắc (ADR-0017).
        /// </summary>
        public static string OnMysteryEventResolved(GameEngine e, MysteryEventId id)
        {
            bool red = id >= MysteryEventId.E10_DotSanTrongSuong && id <= MysteryEventId.E14_LoMoHuKhong;
            if (!red) return "";
            var w = e.CoreAndVillage;
            w.RedEventsCompletedCount++;
            if (e.DeepMechanics.MidGameGateUnlocked && !w.HasHighTierEngravingTableBlueprint)
            {
                w.HasHighTierEngravingTableBlueprint = true;
                return "Nhận bản vẽ Cao cấp: Bàn Khắc (Phù Văn / Mệnh Ấn).";
            }
            return "";
        }

        /// <summary>Khắc Phù Văn ở Bàn Khắc (GDD 12.4): điều kiện lấy từ tiến trình thật của ván chơi.</summary>
        public static (bool ok, string message) EngraveRune(GameEngine e, string runeId, int runeTier = 1)
        {
            var w = e.CoreAndVillage;
            w.BossesDefeatedCount = Math.Max(w.BossesDefeatedCount, e.DeepMechanics.CycleBossesDefeatedCount);
            if (w.RuneFailedOnDay == e.Clock.CurrentDay) return (false, "Mực trên bàn khắc chưa khô sau lần hỏng — thử lại vào ngày mai.");
            var rng = new Random(unchecked(e.BaseSeed * 389 + e.Clock.CurrentDay * 53 + e.Character.EngravedRunes.Count * 7 + runeId.Length));
            var r = w.EngraveRuneAtWorkshop(e.Character, runeId, runeTier, e.DeepMechanics.MidGameGateUnlocked, e.MajorWavesSurvived, e.Defense.HallLevel, rng.NextDouble());
            if (!r.success && r.successRate > 0f) w.RuneFailedOnDay = e.Clock.CurrentDay;
            return (r.success, r.message);
        }

        /// <summary>Khắc Mệnh Ấn (GDD 12.4): chỉ cho nhân vật người chơi, mỗi ấn −10 Chống Chịu tối đa.</summary>
        public static (bool ok, string message) EngraveSeal(GameEngine e, string sealId)
        {
            var w = e.CoreAndVillage;
            w.BossesDefeatedCount = Math.Max(w.BossesDefeatedCount, e.DeepMechanics.CycleBossesDefeatedCount);
            var r = w.EngraveSealAtWorkshop(e.Character, sealId, true, e.MajorWavesSurvived);
            if (r.success) e.Character.CurrentGuard = Math.Min(e.Character.CurrentGuard, e.Character.MaxGuard);
            return (r.success, r.success ? $"Đã khắc Mệnh Ấn [{sealId}]: Chống Chịu tối đa còn {e.Character.MaxGuard:0}." : r.message);
        }

        public static (bool ok, string message) ChangePrimarySchool(GameEngine e, MartialSchool school)
        {
            var c = e.Character;
            if (c.PrimarySchool == school) return (false, "Đang là phái chính.");
            int cycle = e.Clock.CycleNumber;
            if (c.LastSchoolChangeCycle > 0 && cycle - c.LastSchoolChangeCycle < 3)
                return (false, $"Chỉ đổi phái 1 lần / 3 chu kỳ (lần trước chu kỳ {c.LastSchoolChangeCycle}).");
            if (!e.Ledger.TrySpend(20000, LedgerReason.MartialTraining, "school_" + school)) return (false, "Cần 20.000G.");
            c.PrimarySchool = school;
            c.LastSchoolChangeCycle = Math.Max(1, cycle);
            return (true, $"Phái chính: {SchoolName(school)} · liên kết: {SchoolName(LinkedSchool(school))}.");
        }

        // ------------------------------------------------------------------ level and stat points (GDD 8.2)
        /// <summary>PROVISIONAL (ADR-0017): the GDD gives the level range and rewards but no EXP curve.</summary>
        public static int ExpToNext(int level) => 100 * level;
        /// <summary>PROVISIONAL (ADR-0017): GDD says kills give "2–400 EXP" without a split by kind.</summary>
        public static int KillExp(KillClass kind) => kind switch { KillClass.Normal => 2, KillClass.Elite => 40, _ => 400 };
        /// <summary>PROVISIONAL (ADR-0017): surviving a wave gives EXP; the GDD gives no amount.</summary>
        public static int WaveSurvivalExp(int cycle) => 20 * Math.Max(1, cycle);

        public static int AddExp(CharacterData c, int amount)
        {
            if (amount <= 0 || c.Level >= MaxLevel) return 0;
            c.Exp += amount;
            int gained = 0;
            while (c.Level < MaxLevel && c.Exp >= ExpToNext(c.Level))
            {
                c.Exp -= ExpToNext(c.Level);
                c.Level++;
                c.StatPoints += 2;
                if (c.Level % 5 == 0) c.Stamina.MaxStamina += 8f;
                gained++;
            }
            if (c.Level >= MaxLevel) c.Exp = 0;
            if (gained > 0) { c.RecalculateBonusStats(); c.CurrentGuard = Math.Min(c.MaxGuard, c.CurrentGuard + 3f * gained); }
            return gained;
        }

        /// <summary>1 EXP per 10 Stamina spent on labor (GDD 8.2).</summary>
        public static int AddLaborStamina(CharacterData c, float stamina)
        {
            if (stamina <= 0f) return 0;
            c.LaborStaminaTowardExp += stamina;
            int exp = (int)(c.LaborStaminaTowardExp / 10f);
            if (exp <= 0) return 0;
            c.LaborStaminaTowardExp -= exp * 10f;
            return AddExp(c, exp);
        }

        /// <summary>Above the soft cap 40 a point costs double; nothing above the hard cap 55.</summary>
        public static int StatPointCost(int current) => current >= StatSoftCap ? 2 : 1;

        public static (bool ok, string message) SpendStatPoint(CharacterData c, SkillStat stat)
        {
            int cur = BaseStat(c, stat);
            if (cur >= StatHardCap) return (false, $"{StatName(stat)} đã chạm trần {StatHardCap}.");
            int cost = StatPointCost(cur);
            if (c.StatPoints < cost) return (false, $"Cần {cost} điểm phân phối.");
            c.StatPoints -= cost;
            SetBaseStat(c, stat, cur + 1);
            c.RecalculateBonusStats();
            return (true, $"{StatName(stat)} {cur} → {cur + 1}");
        }

        // ------------------------------------------------------------------ equipment
        /// <summary>PROVISIONAL (ADR-0017): GDD 12.3 lists no prices; light/medium/heavy quality-I pieces at 150/300/500 G.</summary>
        public static int PriceGold(EquipmentItem item) => item.WeightScore switch { 1 => 150, 2 => 300, _ => 500 };

        public static (bool ok, string message) Buy(GameEngine e, string itemId)
        {
            var item = EquipmentAndTacticalCatalog.Find(itemId);
            if (item == null) return (false, "Không có món này.");
            // GDD 12.5: the day market sells for gold, quality I only; II–IV come from the night market, auction, Bí nhân, events
            if (item.Quality != ItemTier.I) return (false, "Chợ ngày chỉ bán đồ bậc I. Bậc cao hơn có ở chợ đêm, đấu giá, Bí nhân.");
            if (e.Character.OwnedEquipment.Contains(item.Id)) return (false, $"Đã có {item.Name}.");
            int price = PriceGold(item);
            if (!e.Ledger.TrySpend(price, LedgerReason.BuyEquipment, "equip_" + item.Id)) return (false, $"Không đủ vàng ({price}G).");
            e.Character.OwnedEquipment.Add(item.Id);
            return (true, $"Đã mua {item.Name} (−{price}G).");
        }

        public static (bool ok, string message) Equip(CharacterData c, string itemId)
        {
            var item = EquipmentAndTacticalCatalog.Find(itemId);
            if (item == null) return (false, "Không có món này.");
            if (!c.OwnedEquipment.Contains(item.Id)) return (false, $"Chưa có {item.Name}.");
            c.EquipItem(item);
            return (true, $"Mặc {item.Name} · lối chơi {PlaystyleName(c)}.");
        }

        public static void Unequip(CharacterData c, EquipSlot slot)
        {
            c.Equipment[slot] = null;
            c.RecalculateBonusStats();
        }

        public static EquipmentItem? Weapon(CharacterData c) =>
            c.Equipment.TryGetValue(EquipSlot.Weapon, out var w) ? w : null;

        /// <summary>
        /// One weapon hit (GDD 8.2 + 12.3): base damage × (1 + 1% per point of the weapon's stat, base + bonus).
        /// Heavy → STR, light → AGI, ranged → CTRL; the medium spear uses the mean of STR and AGI (ADR-0017).
        /// Bare hands: 5 (ADR-0017).
        /// </summary>
        public static float WeaponHitDamage(CharacterData c, bool heavyStrike = false)
        {
            var w = Weapon(c);
            if (w == null || w.BaseDamage <= 0f) return 5f;
            int str = c.BaseStats.Str + c.BonusStats.Str, agi = c.BaseStats.Agi + c.BonusStats.Agi, ctl = c.BaseStats.Ctrl + c.BonusStats.Ctrl;
            float stat = w.Archetype switch
            {
                WeaponArchetype.Smash => str,
                WeaponArchetype.Slash => agi,
                WeaponArchetype.Ranged => ctl,
                _ => (str + agi) * 0.5f,
            };
            float dmg = w.BaseDamage * (1f + stat * 0.01f);
            if (heavyStrike && c.GetPlaystyle() == "TrọngGiáp") dmg *= 1.15f; // Trọng Giáp: đòn nặng +15%
            return dmg;
        }

        /// <summary>CTRL: crit +0.2% per point (GDD 8.2).</summary>
        public static float CritChance(CharacterData c) => (c.BaseStats.Ctrl + c.BonusStats.Ctrl) * 0.002f;

        public static float AttackInterval(CharacterData c)
        {
            var w = Weapon(c);
            return w != null && w.AttackInterval > 0f ? w.AttackInterval : 0.5f;
        }

        /// <summary>Khinh Thân +8%, Trọng Giáp −10% run speed (GDD 12.2); AGI is already in PlayerLocomotion.</summary>
        public static float StyleSpeedMultiplier(CharacterData c) => c.GetPlaystyle() switch
        {
            "KhinhThân" => 1.08f,
            "TrọngGiáp" => 0.90f,
            _ => 1f,
        };

        /// <summary>Run speed: play style ±% and AGI +0.4% per point above the starting 18 (GDD 8.2; baseline in ADR-0017).</summary>
        public static float RunSpeedMultiplier(CharacterData c)
        {
            float agi = c.BaseStats.Agi + c.BonusStats.Agi;
            return c.GetMoveSpeedMultiplier() * (1f + (agi - 18f) * 0.004f);
        }
    }
}
