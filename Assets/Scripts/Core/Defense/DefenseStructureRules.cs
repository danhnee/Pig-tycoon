using System;
using System.Collections.Generic;

namespace PigTycoon.Core
{
    /// <summary>GDD 13.1: the six structure families (Thông tin is split 1A/1B; the generator is the grid source).</summary>
    public enum StructureRole { ScoutInfo, DailyInfo, Support, Generator, Attack, Barrier, Trap, Deploy }

    public enum StructureTargets { None, Ground, Air, GroundAndAir, BossOrElite }

    /// <summary>GDD 18: durability bands. 61–100 Tốt • 31–60 Mòn • 1–30 Nguy kịch • 0 Hỏng.</summary>
    public enum DurabilityState { Tot, Mon, NguyKich, Hong }

    /// <summary>
    /// Data row of one structure, taken from the GDD 14–17 tables (and 1.5-D for ranges). Values the GDD does not give
    /// are 0 or marked in ADR-0020 (PROVISIONAL).
    /// </summary>
    public sealed class StructureSpec
    {
        public string BlueprintId = "";
        public StructureRole Role;
        /// <summary>GDD 13.2 blueprint tier: Thuong / Tinh / CaoCap / BachVan.</summary>
        public string Tier = "Thuong";
        public bool Manual;
        public float RangeM;
        public float Damage;
        public float CooldownS;
        public StructureTargets Targets = StructureTargets.None;
        public int Pierce = 1;
        public int ChainTargets;
        public float ChainHopM;
        public int ChainBonusOnGlued;
        public float SplashM;
        public float SplashDamage;
        public int Bolts;
        public int ChargeToFire;
        public int MinTargets;
        public int MagazineShots;
        public float MagazineReloadS;
        public float PinSeconds;
        public float SlowMultiplier = 1f;
        public float SlowSeconds;
        public float StunSeconds;
        public float PullRadiusM;
        public float TriggerRadiusM;
        public float OverheatAfterS;
        public float OverheatRestS;
        public float RevealSeconds;
        public float ManualChargeS;
        public float EffectSeconds;
        public int ShieldPerPack;
        public int PigMoraleLossInRadius;
        public float PigMoraleRadiusM;
        public int AmmoCapacity;
        public int AmmoPerUse = 1;
        public string AmmoName = "";
        public string AmmoItemId = "";
        /// <summary>Gold per ammo unit from the GDD; 0 = the GDD gives no gold price (inventory stock only).</summary>
        public int AmmoUnitPriceGold;
        public float WearPerUse;
        /// <summary>0 = the GDD gives no HP for this structure (DefenseManager default).</summary>
        public int MaxHp;

        // ---- GDD 16.1 upgrade-branch mechanics (0 / false = not this branch) ----
        /// <summary>Xích Liên: every Nth bolt binds its targets for <see cref="BindSeconds"/>.</summary>
        public int BindEveryNth;
        public float BindSeconds;
        /// <summary>Hỏa Tiễn: a burning patch of this radius / duration that ground monsters cannot cross.</summary>
        public float FireZoneM;
        public float FireZoneS;
        /// <summary>Tê Liệt: chance per arc hit to stun for <see cref="StunSeconds"/>.</summary>
        public float StunChance;
        /// <summary>Skyhook debris screen: an air zone UFOs must fly around (base 4 m / 4 s; Mảnh Vụn 6 m / 6 s).</summary>
        public float ScreenM;
        public float ScreenS;
        /// <summary>Móc Trời: a hit UFO flies low for this long, so ground weapons can hit it.</summary>
        public float GroundedSeconds;
        /// <summary>Xuyên Tâm: the shot pierces a line of <see cref="RangeM"/> and hits each target for <see cref="Damage"/>.</summary>
        public bool LinePierce;

        public StructureSpec Clone() => (StructureSpec)MemberwiseClone();
    }

    /// <summary>
    /// GDD 16.1 upgrade branches (from Phòng Xây Dựng level 2; one per structure). Codes keep the 1A–5C slot so older
    /// saves (whose names were placeholders) still resolve by their first two characters.
    /// Not modelled: Skyhook "Theo Dấu" removes a penalty on small low-flying targets that neither the GDD nor the code
    /// defines yet, so the branch is selectable but changes nothing until that penalty exists.
    /// </summary>
    public static class DefenseBranches
    {
        public static readonly IReadOnlyDictionary<string, string[]> ByBlueprint = new Dictionary<string, string[]>
        {
            ["NoXuyenVan"] = new[] { "1A_XichLien", "1B_HoaTien", "1C_NoTroi" },
            ["ThapHoQuang"] = new[] { "2A_LuoiDien", "2B_TeLiet", "2C_CaoAp" },
            ["ThapPhongKhongSkyhook"] = new[] { "3A_ManhVun", "3B_MocTroi", "3C_TheoDau" },
            ["DaiThienLoi"] = new[] { "4A_LoiVong", "4B_ThuHon", "4C_CuongLoi" },
            ["CuuTieuLoiPhao"] = new[] { "5A_TuNhanh", "5B_XuyenTam", "5C_DungLuong" },
        };

        public static string DisplayName(string code) => Normalize(code) switch
        {
            "1A_XichLien" => "Xích Liên — mũi xích nối 3 mục tiêu, cứ 5 mũi trói 1 giây; chứa 40",
            "1B_HoaTien" => "Hỏa Tiễn — Tên Dầu; vùng cháy 2 m trong 4 giây chặn đường",
            "1C_NoTroi" => "Nỏ Trời — bắn cả mục tiêu bay thấp; chứa 36; mất xuyên",
            "2A_LuoiDien" => "Lưới Điện — nảy 6 mục tiêu",
            "2B_TeLiet" => "Tê Liệt — 20% choáng 0,8 giây",
            "2C_CaoAp" => "Cao Áp — 1 tia 150, đơn mục tiêu",
            "3A_ManhVun" => "Mảnh Vụn — màn mảnh 6 m trong 6 giây",
            "3B_MocTroi" => "Móc Trời — kéo UFO xuống thấp 3 giây",
            "3C_TheoDau" => "Theo Dấu — bỏ phạt mục tiêu nhỏ bay thấp",
            "4A_LoiVong" => "Lôi Võng — 12 tia × 320",
            "4B_ThuHon" => "Thu Hồn — chỉ cần 30 điện tích",
            "4C_CuongLoi" => "Cuồng Lôi — 8 tia × 600, hao mòn 25",
            "5A_TuNhanh" => "Tụ Nhanh — truyền năng 16 giây; 5.200",
            "5B_XuyenTam" => "Xuyên Tâm — xuyên tuyến 60 m, 3.500 mỗi mục tiêu",
            "5C_DungLuong" => "Dung Lượng — chứa 150 (3 phát)",
            _ => code ?? "",
        };

        /// <summary>Maps any stored code (current or legacy placeholder like "1A_TocBan") to the GDD code by slot.</summary>
        public static string Normalize(string code)
        {
            if (string.IsNullOrEmpty(code) || code.Length < 2) return "";
            string slot = code.Substring(0, 2).ToUpperInvariant();
            foreach (var list in ByBlueprint.Values)
                foreach (var c in list)
                    if (c.StartsWith(slot, StringComparison.Ordinal)) return c;
            return "";
        }

        /// <summary>The spec a built structure fights with: the base table plus its chosen branch.</summary>
        public static StructureSpec? For(string blueprintId, string chosenBranch)
        {
            var b = DefenseStructureSpecs.Get(blueprintId);
            if (b == null) return null;
            string code = Normalize(chosenBranch);
            if (code.Length == 0 || !ByBlueprint.TryGetValue(blueprintId, out var allowed) || Array.IndexOf(allowed, code) < 0) return b;
            var s = b.Clone();
            switch (code)
            {
                case "1A_XichLien": s.ChainTargets = 3; s.ChainHopM = 4f; s.BindEveryNth = 5; s.BindSeconds = 1f; s.AmmoCapacity = 40; break;
                case "1B_HoaTien": s.AmmoName = "Tên Dầu"; s.FireZoneM = 2f; s.FireZoneS = 4f; break;
                case "1C_NoTroi": s.Targets = StructureTargets.GroundAndAir; s.AmmoCapacity = 36; s.Pierce = 1; break;
                case "2A_LuoiDien": s.ChainTargets = 6; break;
                case "2B_TeLiet": s.StunChance = 0.2f; s.StunSeconds = 0.8f; break;
                case "2C_CaoAp": s.ChainTargets = 1; s.ChainBonusOnGlued = 0; s.Damage = 150f; break;
                case "3A_ManhVun": s.ScreenM = 6f; s.ScreenS = 6f; break;
                case "3B_MocTroi": s.GroundedSeconds = 3f; break;
                case "3C_TheoDau": break;
                case "4A_LoiVong": s.Bolts = 12; s.Damage = 320f; break;
                case "4B_ThuHon": s.ChargeToFire = 30; break;
                case "4C_CuongLoi": s.Bolts = 8; s.Damage = 600f; s.WearPerUse = 25f; break;
                case "5A_TuNhanh": s.ManualChargeS = 16f; s.Damage = 5200f; break;
                case "5B_XuyenTam": s.LinePierce = true; s.Damage = 3500f; break;
                case "5C_DungLuong": s.AmmoCapacity = 150; break;
            }
            return s;
        }
    }

    public static class DefenseStructureSpecs
    {
        /// <summary>GDD 1.5-D: "Máy Phát Điện — cột điện cấp 12 m".</summary>
        public const float GeneratorPowerRadiusM = 12f;
        /// <summary>GDD 13.2: Bạch Vân tier needs Phòng Xây Dựng 3 and farm level ≥ 12.</summary>
        public const int BachVanFarmLevel = 12;
        /// <summary>PROVISIONAL (ADR-0020): re-arm delay of the non-Bàn-Chông traps, same as the existing Bàn Chông 2 s.</summary>
        public const float TrapRearmSeconds = 2f;
        /// <summary>PROVISIONAL (ADR-0020): a "1 Ô Xây" (2 m) trap triggers within half a cell.</summary>
        public const float CellTrapRadiusM = 1f;
        /// <summary>GDD 14.1 / 13.3 rule 9 — the 2D projection of "lộ tàng hình"; Thiên Nhãn: +20% range for 60 s.</summary>
        public const float ThienNhanRangeBonus = 0.20f;

        static readonly Dictionary<string, StructureSpec> Table = Build();

        public static StructureSpec? Get(string blueprintId) =>
            blueprintId != null && Table.TryGetValue(blueprintId, out var s) ? s : null;

        public static IEnumerable<StructureSpec> All => Table.Values;

        public static int HallLevelForTier(string tier) => tier switch
        {
            "CaoCap" => 2,
            "BachVan" => 3,
            _ => 1
        };

        static Dictionary<string, StructureSpec> Build()
        {
            var list = new List<StructureSpec>
            {
                // ---- 1A Trinh thám (GDD 14.1) ----
                new StructureSpec { BlueprintId = "ThapCanh", Role = StructureRole.ScoutInfo, Tier = "Thuong", RangeM = 40f },
                new StructureSpec { BlueprintId = "RadarUyHiep", Role = StructureRole.ScoutInfo, Tier = "Tinh", CooldownS = 60f, RevealSeconds = 45f,
                    AmmoCapacity = 3, AmmoPerUse = 1, AmmoName = "Tín Hiệu", AmmoUnitPriceGold = 80, WearPerUse = 3f },
                new StructureSpec { BlueprintId = "MayCongHuongHuyenBi", Role = StructureRole.ScoutInfo, Tier = "CaoCap", CooldownS = 120f, RevealSeconds = 90f,
                    AmmoCapacity = 4, AmmoPerUse = 2, AmmoName = "Tinh Thể Cộng Hưởng", WearPerUse = 6f },
                new StructureSpec { BlueprintId = "ThienNhanVongDai", Role = StructureRole.ScoutInfo, Tier = "BachVan", Manual = true, CooldownS = 150f, RevealSeconds = 60f,
                    AmmoCapacity = 9, AmmoPerUse = 3, AmmoName = "Dầu Estropic (lít)", AmmoUnitPriceGold = 280, WearPerUse = 10f },
                // ---- 1B Hằng ngày (GDD 14.2): information only, no wave action ----
                new StructureSpec { BlueprintId = "TramKhiTuong", Role = StructureRole.DailyInfo, Tier = "Thuong" },
                new StructureSpec { BlueprintId = "TramTiepSongCho", Role = StructureRole.DailyInfo, Tier = "Tinh" },
                new StructureSpec { BlueprintId = "BanTinhBao", Role = StructureRole.DailyInfo, Tier = "Tinh" },
                new StructureSpec { BlueprintId = "DaiDoDiTuong", Role = StructureRole.DailyInfo, Tier = "CaoCap" },
                // ---- 2 Hỗ trợ & lưới điện (GDD 15, ranges 1.5-D) ----
                new StructureSpec { BlueprintId = "TramHoiSuc", Role = StructureRole.Support, Tier = "Tinh", RangeM = 8f, EffectSeconds = 60f,
                    AmmoCapacity = 3, AmmoPerUse = 1, AmmoName = "Hộp Y Tế" },
                new StructureSpec { BlueprintId = "DenPhaCongNghiep", Role = StructureRole.Support, Tier = "Tinh", RangeM = 25f },
                new StructureSpec { BlueprintId = "MayPhatDien", Role = StructureRole.Generator, Tier = "Thuong", RangeM = GeneratorPowerRadiusM },
                new StructureSpec { BlueprintId = "BomLamMat", Role = StructureRole.Support, Tier = "Tinh", RangeM = 15f },
                new StructureSpec { BlueprintId = "CotThuLoi", Role = StructureRole.Support, Tier = "Thuong", RangeM = 20f },
                new StructureSpec { BlueprintId = "VanLinhHoiNguyenTran", Role = StructureRole.Support, Tier = "BachVan", Manual = true, RangeM = 30f,
                    CooldownS = 180f, EffectSeconds = 20f, AmmoCapacity = 4, AmmoPerUse = 2, AmmoName = "Tinh Thạch Bạch Vân", WearPerUse = 20f },
                // ---- 3 Tấn công (GDD 16; ranges 1.5-D) ----
                new StructureSpec { BlueprintId = "NoXuyenVan", Role = StructureRole.Attack, Tier = "Tinh", Targets = StructureTargets.Ground,
                    Damage = 40f, Pierce = 2, RangeM = 22f, CooldownS = 1.2f, MagazineShots = 10, MagazineReloadS = 4f, PinSeconds = 0.5f,
                    AmmoCapacity = 50, AmmoPerUse = 1, AmmoName = "Tên Thép", AmmoItemId = "TenThep", AmmoUnitPriceGold = 6, WearPerUse = 0.4f },
                new StructureSpec { BlueprintId = "ThapHoQuang", Role = StructureRole.Attack, Tier = "CaoCap", Targets = StructureTargets.Ground,
                    Damage = 70f, ChainTargets = 4, ChainHopM = 4f, ChainBonusOnGlued = 2, RangeM = 18f, CooldownS = 2.2f,
                    OverheatAfterS = 20f, OverheatRestS = 6f,
                    AmmoCapacity = 24, AmmoPerUse = 1, AmmoName = "Pin Dẫn Điện", AmmoUnitPriceGold = 22, WearPerUse = 1.2f },
                new StructureSpec { BlueprintId = "ThapPhongKhongSkyhook", Role = StructureRole.Attack, Tier = "CaoCap", Targets = StructureTargets.Air,
                    Damage = 180f, SplashM = 4f, SplashDamage = 180f, RangeM = 35f, CooldownS = 3.8f, ScreenM = 4f, ScreenS = 4f,
                    AmmoCapacity = 30, AmmoPerUse = 3, AmmoName = "Đạn Phòng Không", AmmoUnitPriceGold = 35, WearPerUse = 2f },
                new StructureSpec { BlueprintId = "DaiThienLoi", Role = StructureRole.Attack, Tier = "BachVan", Targets = StructureTargets.GroundAndAir,
                    Damage = 450f, Bolts = 8, ChargeToFire = 40, MinTargets = 6, RangeM = 30f, CooldownS = 90f,
                    AmmoCapacity = 1, AmmoPerUse = 1, AmmoName = "Lõi Sét", AmmoUnitPriceGold = 900, WearPerUse = 15f },
                new StructureSpec { BlueprintId = "CuuTieuLoiPhao", Role = StructureRole.Attack, Tier = "BachVan", Manual = true, Targets = StructureTargets.BossOrElite,
                    Damage = 6000f, SplashM = 5f, SplashDamage = 800f, RangeM = 60f, ManualChargeS = 20f, CooldownS = 75f,
                    AmmoCapacity = 120, AmmoPerUse = 50, AmmoName = "Đơn Vị Cộng Hưởng", AmmoUnitPriceGold = 18, WearPerUse = 22f },
                // ---- 4 Phòng thủ (GDD 17): block and absorb, never shoot ----
                new StructureSpec { BlueprintId = "RaoGo", Role = StructureRole.Barrier, Tier = "Thuong", MaxHp = 300 },
                new StructureSpec { BlueprintId = "RaoThep", Role = StructureRole.Barrier, Tier = "Thuong", MaxHp = 1200 },
                new StructureSpec { BlueprintId = "CongGiaCo", Role = StructureRole.Barrier, Tier = "Tinh", MaxHp = 4000, RangeM = 15f },
                new StructureSpec { BlueprintId = "MayChieuKhien", Role = StructureRole.Barrier, Tier = "CaoCap", RangeM = 10f, ShieldPerPack = 1500,
                    AmmoCapacity = 4, AmmoPerUse = 1, AmmoName = "Gói Năng Lượng", AmmoUnitPriceGold = 260 },
                new StructureSpec { BlueprintId = "UBaoCat", Role = StructureRole.Barrier, Tier = "Thuong", MaxHp = 800 },
                // ---- 5 Bẫy (GDD 17): limited charges ----
                new StructureSpec { BlueprintId = "BanChong", Role = StructureRole.Trap, Tier = "Thuong", Targets = StructureTargets.Ground,
                    Damage = SpikeTrapSpec.Damage, SlowMultiplier = SpikeTrapSpec.SlowMultiplier, SlowSeconds = SpikeTrapSpec.SlowSeconds,
                    TriggerRadiusM = CellTrapRadiusM, AmmoCapacity = SpikeTrapSpec.Charges, AmmoPerUse = 1, AmmoName = "Lần kích" },
                new StructureSpec { BlueprintId = "TamSocDien", Role = StructureRole.Trap, Tier = "Tinh", Targets = StructureTargets.Ground,
                    StunSeconds = 1.2f, TriggerRadiusM = CellTrapRadiusM, AmmoCapacity = 4, AmmoPerUse = 1, AmmoName = "Gói Sạc", AmmoUnitPriceGold = 90 },
                new StructureSpec { BlueprintId = "HoKeo", Role = StructureRole.Trap, Tier = "Tinh", Targets = StructureTargets.Ground,
                    SlowMultiplier = 0.4f, SlowSeconds = 5f, PullRadiusM = 3f, TriggerRadiusM = CellTrapRadiusM,
                    AmmoCapacity = 3, AmmoPerUse = 1, AmmoName = "Keo", AmmoUnitPriceGold = 120 },
                new StructureSpec { BlueprintId = "LuoiEmp", Role = StructureRole.Trap, Tier = "CaoCap", Targets = StructureTargets.GroundAndAir,
                    RangeM = 6f, TriggerRadiusM = 6f, RevealSeconds = 6f, AmmoCapacity = 2, AmmoPerUse = 1, AmmoName = "Lõi EMP", AmmoUnitPriceGold = 400 },
                new StructureSpec { BlueprintId = "MinNo", Role = StructureRole.Trap, Tier = "Tinh", Targets = StructureTargets.Ground,
                    Damage = 500f, SplashM = 4f, SplashDamage = 500f, MinTargets = 4, TriggerRadiusM = 4f, PigMoraleLossInRadius = 12, PigMoraleRadiusM = 15f,
                    AmmoCapacity = 1, AmmoPerUse = 1, AmmoName = "Thuốc Nổ", AmmoUnitPriceGold = 250 },
                // ---- 6 Triển khai (GDD 17): zones, no attack ----
                new StructureSpec { BlueprintId = "ChotCanhGac", Role = StructureRole.Deploy, Tier = "Thuong", RangeM = 12f },
                // PROVISIONAL (ADR-0020): GDD 1.5-D leaves Cờ Tập Kết's zone "chưa ghi"; the existing table 15D value 12 m is kept.
                new StructureSpec { BlueprintId = "CoTapKet", Role = StructureRole.Deploy, Tier = "Thuong", RangeM = 12f },
                new StructureSpec { BlueprintId = "DenHieuDanhChan", Role = StructureRole.Deploy, Tier = "Tinh", RangeM = 20f,
                    AmmoCapacity = 3, AmmoPerUse = 1, AmmoName = "Pin Tín Hiệu" },
            };
            var d = new Dictionary<string, StructureSpec>(StringComparer.Ordinal);
            foreach (var s in list) d[s.BlueprintId] = s;
            return d;
        }

        /// <summary>Fills a new instance from its GDD row (HP, magazine, ammo and cadence).</summary>
        public static void ApplyTo(DefenseBuildingInstance b)
        {
            var s = Get(b.BlueprintId);
            if (s == null) return;
            if (s.MaxHp > 0) { b.MaxHp = s.MaxHp; b.CurrentHp = s.MaxHp; }
            b.AmmoCapacity = s.AmmoCapacity;
            b.AmmoCurrent = s.AmmoCapacity;
            b.AmmoName = s.AmmoName;
            b.AmmoCostPerShot = s.AmmoCapacity > 0 ? Math.Max(1, s.AmmoPerUse) : 0;
            b.CooldownSeconds = s.CooldownS;
            if (s.ShieldPerPack > 0 && b.AmmoCurrent > 0) { b.AmmoCurrent -= 1; b.ShieldPoints = s.ShieldPerPack; }
        }

        public static bool IsAirMonster(EnemyTypeId t) =>
            t == EnemyTypeId.UfoTrinhSat || t == EnemyTypeId.UfoThuHoach || t == EnemyTypeId.UfoPhaoHam || t == EnemyTypeId.UfoMothership;

        public static bool IsBossOrElite(EnemyTypeId t) =>
            t == EnemyTypeId.ChuaTromRiuSat || t == EnemyTypeId.UfoMothership || t == EnemyTypeId.ThucTheSuong ||
            t == EnemyTypeId.DauLinhTrom || t == EnemyTypeId.KeMangDich || t == EnemyTypeId.BossAlienWarlord || t == EnemyTypeId.AlienElite;

        public static bool CanTarget(StructureSpec s, EnemyTypeId t)
        {
            switch (s.Targets)
            {
                case StructureTargets.Ground: return !IsAirMonster(t);
                case StructureTargets.Air: return IsAirMonster(t);
                case StructureTargets.GroundAndAir: return true;
                case StructureTargets.BossOrElite: return true; // rule 9: the zone hits whatever stands in it
                default: return false;
            }
        }
    }

    /// <summary>GDD 13.2: Phòng Xây Dựng levels, upgrade price and attack-structure caps per level.</summary>
    public static class ConstructionHall
    {
        public const int MaxLevel = 3;
        /// <summary>"thép": the only steel material in the game is the village barter's Thép Cường Lực.</summary>
        public const string SteelItemId = "ThepCuongLuc";

        /// <summary>Price to reach <paramref name="toLevel"/>: 1 = 1.500G; 2 = 6.000G + 40 thép; 3 = 18.000G + 100 thép + 2 Linh Kiện Tinh.</summary>
        public static (int gold, int steel, int fineComponents)? UpgradeCost(int toLevel) => toLevel switch
        {
            1 => (1500, 0, 0),
            2 => (6000, 40, 0),
            3 => (18000, 100, 2),
            _ => ((int, int, int)?)null
        };

        public static string DescribeNext(int hallLevel)
        {
            var cost = UpgradeCost(hallLevel + 1);
            if (cost == null) return "Phòng Xây Dựng đã đạt cấp tối đa.";
            var c = cost.Value;
            string s = $"{c.gold:N0}G";
            if (c.steel > 0) s += $" + {c.steel} thép";
            if (c.fineComponents > 0) s += $" + {c.fineComponents} Linh Kiện Tinh";
            return $"Nâng lên cấp {hallLevel + 1}: {s}";
        }

        public static (bool success, string message) TryUpgrade(GameEngine engine)
        {
            if (engine == null) throw new ArgumentNullException(nameof(engine));
            var def = engine.Defense;
            if (def.HallLevel >= MaxLevel) return (false, "Phòng Xây Dựng đã đạt cấp tối đa (cấp 3).");
            if (engine.Clock.IsDuringCombatWave) return (false, "Không nâng cấp Phòng Xây Dựng khi đang có đợt quái.");
            int to = Math.Max(1, def.HallLevel + 1);
            var c = UpgradeCost(to)!.Value;
            if (engine.Farm.Gold < c.gold || engine.Inventory.CountId(SteelItemId) < c.steel ||
                engine.Inventory.CountId(DefenseRepairs.FineComponentId) < c.fineComponents)
                return (false, $"Thiếu nguyên liệu. {DescribeNext(def.HallLevel)}.");
            if (!engine.Ledger.TrySpend(c.gold, LedgerReason.BuildStructure, "construction_hall_lv" + to)) return (false, "Không đủ vàng.");
            if (c.steel > 0) engine.Inventory.ConsumeItem(SteelItemId, c.steel);
            if (c.fineComponents > 0) engine.Inventory.ConsumeItem(DefenseRepairs.FineComponentId, c.fineComponents);
            def.HallLevel = to;
            return (true, $"Phòng Xây Dựng đã lên cấp {to}! Mở bản vẽ cấp {to}.");
        }

        /// <summary>
        /// GDD 13.2 "Số lượng tối đa công trình tấn công": 1 → Nỏ 2 • 2 → Nỏ 3, Hồ Quang 2, Skyhook 2 •
        /// 3 → Nỏ 4, Hồ Quang 3, Skyhook 3, Thiên Lôi 1, mỗi Bạch Vân 1. Returns -1 for non-attack structures.
        /// </summary>
        public static int AttackCap(int hallLevel, string blueprintId)
        {
            var s = DefenseStructureSpecs.Get(blueprintId);
            if (s == null || s.Role != StructureRole.Attack) return -1;
            int h = Math.Clamp(hallLevel, 0, MaxLevel);
            switch (blueprintId)
            {
                case "NoXuyenVan": return h switch { 1 => 2, 2 => 3, 3 => 4, _ => 0 };
                case "ThapHoQuang":
                case "ThapPhongKhongSkyhook": return h switch { 2 => 2, 3 => 3, _ => 0 };
                case "DaiThienLoi": return h >= 3 ? 1 : 0;
                default: return h >= 3 ? 1 : 0; // each Bạch Vân
            }
        }

        // ---- GDD 13.2 + 11.1: NPC trực phòng, hiệu suất, ô thi công, thời gian xây ----

        /// <summary>GDD 13.2 / 11.1: NPC khác nghề 40% • Kỹ Sư chưa mở đặc tính 70% • đã mở 100% (integer percent).</summary>
        public const int OtherNpcEfficiencyPercent = 40;
        public const int EngineerEfficiencyPercent = 70;
        public const int EngineerTraitEfficiencyPercent = 100;

        /// <summary>
        /// GDD 11 role split: only the seven farm/specialist NPCs (11.1) do farm work; mercenaries (11.2) never staff a room.
        /// </summary>
        public static bool IsFarmNpc(NpcSpecialistId id) =>
            id == NpcSpecialistId.QuanLyCa || id == NpcSpecialistId.NhaNghienCuuGen || id == NpcSpecialistId.BacSiThuY ||
            id == NpcSpecialistId.KySuXayDung || id == NpcSpecialistId.NhaBuon || id == NpcSpecialistId.ThuKho ||
            id == NpcSpecialistId.NongCong;

        /// <summary>The NPC on duty in Phòng Xây Dựng: an active Kỹ Sư Xây Dựng first, else any active farm NPC; null = nobody.</summary>
        public static HiredNpcRecord? OnDutyNpc(GameEngine engine)
        {
            if (engine == null) return null;
            var npcs = engine.PetsAndNpcs.HiredNpcs;
            var engineer = npcs.Find(n => n != null && n.IsActive && n.Id == NpcSpecialistId.KySuXayDung);
            if (engineer != null) return engineer;
            return npcs.Find(n => n != null && n.IsActive && IsFarmNpc(n.Id));
        }

        /// <summary>Efficiency of the room in percent for a given on-duty NPC; 0 = nobody on duty (the room stops).</summary>
        public static int EfficiencyPercent(HiredNpcRecord? onDuty)
        {
            if (onDuty == null) return 0;
            if (onDuty.Id != NpcSpecialistId.KySuXayDung) return OtherNpcEfficiencyPercent;
            return onDuty.TraitUnlocked ? EngineerTraitEfficiencyPercent : EngineerEfficiencyPercent;
        }

        public static int EfficiencyPercent(GameEngine engine) => EfficiencyPercent(OnDutyNpc(engine));

        /// <summary>Kỹ Sư Xây Dựng with "Tay Nghề Xây Dựng" unlocked is on duty (he is always picked first).</summary>
        public static bool EngineerTraitOnDuty(GameEngine engine)
        {
            var npc = OnDutyNpc(engine);
            return npc != null && npc.Id == NpcSpecialistId.KySuXayDung && npc.TraitUnlocked;
        }

        /// <summary>GDD 13.2 "Ô thi công": cấp 1 → 1 • cấp 2 → 1 (+1 nếu Kỹ Sư đã mở đặc tính) • cấp 3 → 2 (+1).</summary>
        public static int BuildSlots(int hallLevel, bool engineerTrait)
        {
            if (hallLevel <= 0) return 0;
            if (hallLevel == 1) return 1;
            int bonus = engineerTrait ? 1 : 0;
            return hallLevel == 2 ? 1 + bonus : 2 + bonus;
        }

        /// <summary>Slots usable now: none while nobody is on duty ("Không có NPC trực: phòng ngừng hoạt động").</summary>
        public static int BuildSlots(GameEngine engine) =>
            engine == null || OnDutyNpc(engine) == null ? 0 : BuildSlots(engine.Defense.HallLevel, EngineerTraitOnDuty(engine));

        /// <summary>GDD 13.2 example: "Skyhook gốc 8 giờ".</summary>
        public const int SkyhookBaseBuildMinutes = 8 * 60;

        /// <summary>
        /// Base build time in game minutes at 100% efficiency. Only Skyhook (8 h) is given by the GDD; the other values
        /// are PROVISIONAL per blueprint tier (ADR-0021): Thường 1 h, Tinh 4 h, Cao cấp 8 h (= Skyhook), Bạch Vân 16 h.
        /// </summary>
        public static int BaseBuildMinutes(string blueprintId)
        {
            if (blueprintId == "ThapPhongKhongSkyhook") return SkyhookBaseBuildMinutes;
            string tier = DefenseStructureSpecs.Get(blueprintId)?.Tier ?? "Thuong";
            return tier switch
            {
                "Tinh" => 4 * 60,          // PROVISIONAL (ADR-0021)
                "CaoCap" => 8 * 60,        // PROVISIONAL (ADR-0021), anchored on Skyhook 8 h
                "BachVan" => 16 * 60,      // PROVISIONAL (ADR-0021)
                _ => 60                    // PROVISIONAL (ADR-0021): Thường
            };
        }

        /// <summary>"Thời gian thực tế = Thời gian gốc / Hiệu suất" in game minutes (rounded up); -1 = the room is not working.</summary>
        public static int RealMinutes(int baseMinutes, int efficiencyPercent) =>
            efficiencyPercent <= 0 ? -1 : (int)Math.Ceiling(baseMinutes * 100d / efficiencyPercent);

        /// <summary>Short Vietnamese status of the room for the build popup.</summary>
        public static string DescribeDuty(GameEngine engine)
        {
            var npc = OnDutyNpc(engine);
            if (npc == null) return "Chưa có NPC trực — phòng ngừng hoạt động (không xây, không sửa)";
            string who = string.IsNullOrWhiteSpace(npc.Name) ? npc.Id.ToString() : npc.Name;
            string role = npc.Id == NpcSpecialistId.KySuXayDung ? (npc.TraitUnlocked ? "Kỹ Sư, đã mở đặc tính" : "Kỹ Sư") : "khác nghề";
            return $"Trực phòng: {who} ({role}) • hiệu suất {EfficiencyPercent(npc)}%";
        }
    }

    /// <summary>GDD 18: wear from use and from hits, the four durability states and their effects.</summary>
    public static class DefenseDurability
    {
        public const int HitHpPerDurabilityPoint = 100;
        public const int HeavyBlowDurability = 3;
        public const int LightningDurability = 10;

        public static DurabilityState StateOf(int durability)
        {
            if (durability <= 0) return DurabilityState.Hong;
            if (durability <= 30) return DurabilityState.NguyKich;
            if (durability <= 60) return DurabilityState.Mon;
            return DurabilityState.Tot;
        }

        public static string StateName(DurabilityState s) => s switch
        {
            DurabilityState.Tot => "Tốt",
            DurabilityState.Mon => "Mòn",
            DurabilityState.NguyKich => "Nguy kịch",
            _ => "Hỏng"
        };

        /// <summary>Mòn: cooldown +5% • Nguy kịch: cooldown +15%.</summary>
        public static float CooldownMultiplier(DurabilityState s) => s == DurabilityState.Mon ? 1.05f : s == DurabilityState.NguyKich ? 1.15f : 1f;
        /// <summary>Nguy kịch: damage −10%.</summary>
        public static float DamageMultiplier(DurabilityState s) => s == DurabilityState.NguyKich ? 0.9f : 1f;
        /// <summary>Mòn: 3% jammed shots.</summary>
        public static float JamChance(DurabilityState s) => s == DurabilityState.Mon ? 0.03f : 0f;
        /// <summary>Nguy kịch: a Bạch Vân activation fails 5% of the time (fuel is still spent).</summary>
        public static float BachVanFailChance(DurabilityState s) => s == DurabilityState.NguyKich ? 0.05f : 0f;

        /// <summary>Per-shot / per-activation wear (fractional wear such as Nỏ 0.4/mũi accumulates).</summary>
        public static int ApplyUseWear(DefenseBuildingInstance b, float wear)
        {
            if (b == null || wear <= 0f || b.IsBroken) return 0;
            b.WearCarry += wear;
            int whole = (int)Math.Floor(b.WearCarry + 1e-4f);
            if (whole <= 0) return 0;
            b.WearCarry = Math.Max(0f, b.WearCarry - whole);
            int lost = Math.Min(whole, b.Durability);
            b.Durability -= lost;
            b.DurabilityLostFiring += lost;
            if (b.Durability <= 0) b.IsBroken = true;
            return lost;
        }

        /// <summary>
        /// A monster hit: shields of a Máy Chiếu Khiên within 10 m take it first; then every 100 HP of damage = −1
        /// durability, a heavy blow (elite/Boss plasma, siege) = −3; HP 0 = Hỏng at once.
        /// </summary>
        public static int ApplyHit(DefenseManager? def, DefenseBuildingInstance b, float damage, bool heavyBlow)
        {
            if (b == null || damage <= 0f || b.IsBroken) return 0;
            float rest = def != null ? def.AbsorbWithShield(b, damage) : damage;
            int hp = (int)Math.Round(rest);
            if (hp > 0) b.CurrentHp = Math.Max(0, b.CurrentHp - hp);
            b.HitDamageCarry += rest;
            int lost = (int)Math.Floor(b.HitDamageCarry / HitHpPerDurabilityPoint);
            b.HitDamageCarry -= lost * HitHpPerDurabilityPoint;
            if (heavyBlow) lost += HeavyBlowDurability;
            lost = Math.Min(lost, b.Durability);
            b.Durability -= lost;
            b.DurabilityLostHits += lost;
            if (b.CurrentHp <= 0 || b.Durability <= 0) b.IsBroken = true;
            return hp;
        }

        /// <summary>GDD 18 "Sét đánh −10".</summary>
        public static void ApplyLightning(DefenseBuildingInstance b)
        {
            if (b == null || b.IsBroken) return;
            int lost = Math.Min(LightningDurability, b.Durability);
            b.Durability -= lost;
            b.DurabilityLostHits += lost;
            if (b.Durability <= 0) b.IsBroken = true;
        }
    }

    public sealed class ReloadQuote
    {
        public int Units, FromStock, Gold;
        public bool Unpriced;
    }

    /// <summary>GDD 13.3 rule 2–3 + 16 tables: pre-load before a wave, up to the cap, at the GDD unit price.</summary>
    public static class DefenseReloads
    {
        public static ReloadQuote Quote(DefenseBuildingInstance b, UnifiedInventory? inventory)
        {
            var q = new ReloadQuote();
            if (b == null) return q;
            var s = DefenseStructureSpecs.Get(b.BlueprintId);
            int missing = Math.Max(0, b.AmmoCapacity - b.AmmoCurrent);
            if (s == null || missing == 0 || s.Role == StructureRole.Trap && b.BlueprintId == "BanChong") return q;
            q.Units = missing;
            if (!string.IsNullOrEmpty(s.AmmoItemId) && inventory != null)
                q.FromStock = Math.Min(missing, inventory.CountId(s.AmmoItemId));
            int toBuy = missing - q.FromStock;
            if (toBuy > 0 && s.AmmoUnitPriceGold <= 0) { q.Unpriced = true; q.Units = q.FromStock; toBuy = 0; }
            q.Gold = toBuy * s.AmmoUnitPriceGold;
            q.Units = q.FromStock + toBuy;
            return q;
        }

        public static (bool success, string message, int goldSpent) Reload(GameEngine engine, string instanceId)
        {
            if (engine == null) throw new ArgumentNullException(nameof(engine));
            if (engine.Clock.IsDuringCombatWave) return (false, "Không được nạp đạn trong đợt quái!", 0);
            var b = engine.Defense.Buildings.Find(x => x.InstanceId == instanceId);
            if (b == null) return (false, "Không tìm thấy công trình!", 0);
            if (b.AmmoCapacity <= 0) return (false, $"{b.Name} không dùng đạn/nhiên liệu.", 0);
            var q = Quote(b, engine.Inventory);
            if (q.Units == 0)
                return q.Unpriced
                    ? (false, $"{b.Name}: {b.AmmoName} chỉ có ở chợ đêm (không bán bằng vàng tại trại).", 0)
                    : (false, $"{b.Name} đã đầy ({b.AmmoCurrent}/{b.AmmoCapacity}).", 0);
            if (engine.Farm.Gold < q.Gold) return (false, $"Nạp {b.Name} cần {q.Gold}G ({q.Units - q.FromStock} {b.AmmoName}).", 0);
            if (q.Gold > 0 && !engine.Ledger.TrySpend(q.Gold, LedgerReason.PreloadAmmo, instanceId)) return (false, "Không đủ vàng.", 0);
            var s = DefenseStructureSpecs.Get(b.BlueprintId);
            if (q.FromStock > 0 && s != null) engine.Inventory.ConsumeItem(s.AmmoItemId, q.FromStock);
            b.AmmoCurrent = Math.Min(b.AmmoCapacity, b.AmmoCurrent + q.Units);
            string tail = q.Unpriced ? " (phần còn lại chỉ có ở chợ đêm)" : "";
            return (true, $"Đã nạp {q.Units} {b.AmmoName} cho {b.Name}: {q.Gold}G{(q.FromStock > 0 ? $", {q.FromStock} từ kho" : "")}{tail}. {b.AmmoCurrent}/{b.AmmoCapacity}.", q.Gold);
        }
    }

    [Serializable]
    public sealed class ContaminationZone
    {
        public float X, Y;
        public int MinutesLeft;
    }

    /// <summary>GDD 19.1 Kẻ Mang Dịch: its death leaves a contamination zone; unlimed for 12 game hours → Chuồng Nhiễm.</summary>
    public static class PlagueZones
    {
        public const int MinutesToContaminate = 12 * 60;
        public const string LimeItemId = "VoiBot";

        public static void Advance(GameEngine engine, int elapsedMinutes)
        {
            if (engine == null || elapsedMinutes <= 0) return;
            var zones = engine.Defense.ContaminationZones;
            for (int i = zones.Count - 1; i >= 0; i--)
            {
                zones[i].MinutesLeft -= elapsedMinutes;
                if (zones[i].MinutesLeft > 0) continue;
                zones.RemoveAt(i);
                engine.Defense.PlagueContaminatedPen = true;
                engine.Farm.IsBarnContaminated = true;
            }
        }

        /// <summary>Rải vôi: clears the zones within <paramref name="radius"/> m; one lime item per zone.</summary>
        public static int SpreadLime(GameEngine engine, float x, float y, float radius)
        {
            int cleared = 0;
            var zones = engine.Defense.ContaminationZones;
            for (int i = zones.Count - 1; i >= 0; i--)
            {
                float dx = zones[i].X - x, dy = zones[i].Y - y;
                if (dx * dx + dy * dy > radius * radius) continue;
                if (!engine.Inventory.ConsumeItem(LimeItemId, 1)) break;
                zones.RemoveAt(i);
                cleared++;
            }
            if (cleared > 0 && zones.Count == 0) engine.Defense.PlagueContaminatedPen = false;
            return cleared;
        }
    }

    /// <summary>Monsters hurting pigs: damage flows into the Core pig (Health 0–100) and its death into PigDied.</summary>
    public static class MonsterPigDamage
    {
        public static bool Apply(GameEngine? engine, Pig pig, float damage, string attackerName)
        {
            if (pig == null || !pig.IsAlive || damage <= 0f) return false;
            int dmg = Math.Max(1, (int)Math.Round(damage));
            pig.Health = Math.Max(0, pig.Health - dmg);
            if (pig.Health > 0) return false;
            pig.Die();
            if (engine != null)
            {
                engine.Defense.PigsLostThisWave += 1;
                engine.EventBus.Publish(new PigDied(pig.Id, $"Bị {attackerName} giết trong đợt quái", engine.Clock.CurrentDay));
            }
            return true;
        }

        /// <summary>Tinh thần loss (Bầy Nhảy bite −10, Mìn Nổ −12 …).</summary>
        public static void Morale(Pig pig, int loss)
        {
            if (pig == null || !pig.IsAlive || loss <= 0) return;
            pig.Mood = Math.Max(0, pig.Mood - loss);
        }
    }

    public sealed class AfterWaveReport
    {
        public int WaveIndex;
        public bool Victory;
        public int MonstersDefeated;
        public int AmmoUsed;
        public int BachVanActivations;
        public int ArrowsRecovered;
        public int PigsLost;
        public int LowestHerdMorale;
        public int RepairGoldEstimate;
        public int ReloadGoldEstimate;
        public int GoldReward;
        public int DurabilityLostFiring;
        public int DurabilityLostHits;
        public readonly List<string> StructureLines = new List<string>();

        public string ToVietnamese()
        {
            var lines = new List<string>
            {
                $"BÁO CÁO SAU ĐỢT {WaveIndex}: {(Victory ? "giữ được trại" : "thất thủ")} • hạ {MonstersDefeated} quái • thưởng {GoldReward}G",
                $"Đạn đã dùng: {AmmoUsed} • Kích Bạch Vân: {BachVanActivations} • Nhặt lại {ArrowsRecovered} tên",
                $"Độ bền mất: bắn {DurabilityLostFiring} • bị đánh {DurabilityLostHits}",
                $"Heo mất: {PigsLost} • Tinh thần đàn thấp nhất: {LowestHerdMorale}",
                $"Dự kiến sửa {RepairGoldEstimate}G • nạp lại {ReloadGoldEstimate}G",
            };
            lines.AddRange(StructureLines);
            return string.Join("\n", lines);
        }
    }

    /// <summary>
    /// The runtime wave hooks: <see cref="Begin"/> publishes WaveStarted on the Core EventBus and resets the per-wave
    /// counters; <see cref="Close"/> builds the GDD 18 after-wave report, pays the reward and publishes WaveEnded.
    /// </summary>
    public static class WaveDebrief
    {
        /// <summary>GDD 16: "sau đợt nhặt lại 30% tên".</summary>
        public const float ArrowRecoveryShare = 0.30f;
        /// <summary>GDD 19: a major wave lasts 4–9 real minutes; at the upper bound the survivors withdraw.</summary>
        public const float WaveTimeCapSeconds = 9f * 60f;

        public static void Begin(GameEngine engine, int waveIndex, int threatBudget, bool isBossWave)
        {
            if (engine == null) return;
            var def = engine.Defense;
            def.PigsLostThisWave = 0;
            def.BachVanActivationsThisWave = 0;
            def.LowestHerdMoraleThisWave = AverageMorale(engine);
            foreach (var b in def.Buildings)
            {
                if (b == null) continue;
                b.AmmoUsedThisWave = 0;
                b.DurabilityLostFiring = 0;
                b.DurabilityLostHits = 0;
            }
            engine.EventBus.Publish(new WaveStarted(waveIndex, threatBudget, isBossWave, engine.Clock.CurrentDay));
        }

        public static int AverageMorale(GameEngine engine)
        {
            int n = 0, sum = 0;
            foreach (var p in engine.Pigs) if (p != null && p.IsAlive) { n++; sum += p.Mood; }
            return n == 0 ? 0 : sum / n;
        }

        public static void ObserveMorale(GameEngine engine)
        {
            if (engine == null) return;
            int avg = AverageMorale(engine);
            if (avg < engine.Defense.LowestHerdMoraleThisWave) engine.Defense.LowestHerdMoraleThisWave = avg;
        }

        /// <param name="killReward">Sum of the defeated monsters' GoldReward (PROVISIONAL, ADR-0020: the GDD names
        /// "thưởng đợt quái" as income but gives no figure).</param>
        public static AfterWaveReport Close(GameEngine engine, int waveIndex, bool victory, int monstersDefeated, int killReward)
        {
            var def = engine.Defense;
            ObserveMorale(engine);
            var r = new AfterWaveReport
            {
                WaveIndex = waveIndex,
                Victory = victory,
                MonstersDefeated = monstersDefeated,
                BachVanActivations = def.BachVanActivationsThisWave,
                PigsLost = def.PigsLostThisWave,
                LowestHerdMorale = def.LowestHerdMoraleThisWave,
                GoldReward = Math.Max(0, killReward),
            };
            foreach (var b in def.Buildings)
            {
                if (b == null) continue;
                r.AmmoUsed += b.AmmoUsedThisWave;
                r.DurabilityLostFiring += b.DurabilityLostFiring;
                r.DurabilityLostHits += b.DurabilityLostHits;
                if (b.BlueprintId == "NoXuyenVan" && b.AmmoUsedThisWave > 0)
                    r.ArrowsRecovered += (int)Math.Floor(b.AmmoUsedThisWave * ArrowRecoveryShare);
                if (b.DurabilityLostFiring + b.DurabilityLostHits > 0 || b.IsBroken)
                    r.StructureLines.Add($"• {b.Name}: độ bền {b.Durability} ({DefenseDurability.StateName(DefenseDurability.StateOf(b.Durability))}) — mất do bắn {b.DurabilityLostFiring}, do bị đánh {b.DurabilityLostHits}");
            }
            if (r.ArrowsRecovered > 0) engine.Inventory.AddItem("TenThep", r.ArrowsRecovered);
            foreach (var b in def.Buildings)
            {
                if (b == null) continue;
                if (def.GetBlueprint(b.BlueprintId) != null && (b.Durability < 100 || b.CurrentHp < b.MaxHp || b.IsBroken))
                    r.RepairGoldEstimate += DefenseRepairs.Quote(engine, b).Gold;
                r.ReloadGoldEstimate += DefenseReloads.Quote(b, engine.Inventory).Gold;
            }
            if (r.GoldReward > 0) engine.Ledger.Earn(r.GoldReward, LedgerReason.WaveReward, $"wave_{waveIndex}");
            engine.DeepMechanics.RecordWaveBattleReport(waveIndex, victory, monstersDefeated, r.PigsLost, r.AmmoUsed,
                r.DurabilityLostFiring + r.DurabilityLostHits, engine.Character.FaintCount, r.GoldReward);
            engine.EventBus.Publish(new WaveEnded(waveIndex, victory, r.PigsLost, engine.Clock.CurrentDay));
            return r;
        }
    }

    /// <summary>Power, wires, shields, per-wave counters (GDD 15, 17, 18, 19.1).</summary>
    public partial class DefenseManager
    {
        public int PigsLostThisWave { get; set; }
        public int LowestHerdMoraleThisWave { get; set; } = 100;
        public int BachVanActivationsThisWave { get; set; }
        public List<ContaminationZone> ContaminationZones { get; set; } = new List<ContaminationZone>();
        public bool PlagueContaminatedPen { get; set; }
        /// <summary>Instance id of the last successful manual Bạch Vân activation (views play the effect).</summary>
        public string LastBachVanActivatedId { get; set; } = "";
        [NonSerialized] private Random? bachVanRng;

        public bool NeedsPower(DefenseBuildingInstance b) =>
            b != null && GetBlueprint(b.BlueprintId) is DefenseBlueprint bp && (bp.PowerUnitsContinuous > 0 || bp.PowerUnitsPeak > 0);

        public static bool IsWorkingGenerator(DefenseBuildingInstance b) =>
            b != null && b.BlueprintId == "MayPhatDien" && !b.IsBroken && b.CurrentHp > 0 && b.Durability > 0;

        public DefenseBuildingInstance? NearestWorkingGenerator(DefenseBuildingInstance b)
        {
            if (b == null || !b.HasPosition) return null;
            DefenseBuildingInstance? best = null;
            float bestD = DefenseStructureSpecs.GeneratorPowerRadiusM * DefenseStructureSpecs.GeneratorPowerRadiusM;
            foreach (var g in Buildings)
            {
                if (!IsWorkingGenerator(g) || !g.HasPosition) continue;
                float dx = g.PosX - b.PosX, dy = g.PosY - b.PosY, d = dx * dx + dy * dy;
                if (d <= bestD) { bestD = d; best = g; }
            }
            return best;
        }

        /// <summary>
        /// GDD 15 + 1.5-D: a placed structure that needs power runs only within 12 m of a working generator and while
        /// its wire is intact. Unplaced (Core-only) structures keep the grid-only rule.
        /// </summary>
        public bool PoweredByRadius(DefenseBuildingInstance b)
        {
            if (b == null) return false;
            if (!NeedsPower(b)) return true;
            if (!string.IsNullOrEmpty(b.WireCutBy)) return false;
            if (!b.HasPosition) return true;
            return NearestWorkingGenerator(b) != null;
        }

        /// <summary>Can the structure act now: not broken, has HP/durability and, if it needs power, is powered.</summary>
        public bool IsOperational(DefenseBuildingInstance b) =>
            b != null && !b.IsBroken && b.CurrentHp > 0 && b.Durability > 0 && (!NeedsPower(b) || b.IsPowered && PoweredByRadius(b));

        /// <summary>Kẻ Cắt Dây: the branch of the structure (everything fed by the same generator) loses power until the cutter dies.</summary>
        public int CutWireBranch(string cutterId, DefenseBuildingInstance target)
        {
            if (target == null || string.IsNullOrEmpty(cutterId)) return 0;
            var gen = NearestWorkingGenerator(target);
            int n = 0;
            foreach (var b in Buildings)
            {
                if (b == null || !NeedsPower(b) || !string.IsNullOrEmpty(b.WireCutBy)) continue;
                bool sameBranch = b == target || gen != null && NearestWorkingGenerator(b) == gen;
                if (!sameBranch) continue;
                b.WireCutBy = cutterId;
                n++;
            }
            return n;
        }

        public int RestoreWiresCutBy(string cutterId)
        {
            int n = 0;
            foreach (var b in Buildings)
                if (b != null && b.WireCutBy == cutterId) { b.WireCutBy = ""; n++; }
            return n;
        }

        /// <summary>GDD 17 Máy Chiếu Khiên: each pack is 1.500 shield points over 10 m; returns the damage that gets through.</summary>
        public float AbsorbWithShield(DefenseBuildingInstance target, float damage)
        {
            if (target == null || !target.HasPosition || damage <= 0f) return damage;
            foreach (var s in Buildings)
            {
                if (s == null || s.BlueprintId != "MayChieuKhien" || !s.HasPosition || !IsOperational(s)) continue;
                var spec = DefenseStructureSpecs.Get(s.BlueprintId);
                if (spec == null) continue;
                float dx = s.PosX - target.PosX, dy = s.PosY - target.PosY;
                if (dx * dx + dy * dy > spec.RangeM * spec.RangeM) continue;
                while (damage > 0f)
                {
                    if (s.ShieldPoints <= 0)
                    {
                        if (s.AmmoCurrent <= 0) break;
                        s.AmmoCurrent -= 1;
                        s.AmmoUsedThisWave += 1;
                        s.ShieldPoints = spec.ShieldPerPack;
                    }
                    int take = (int)Math.Min(s.ShieldPoints, Math.Ceiling(damage));
                    s.ShieldPoints -= take;
                    damage -= take;
                }
                if (damage <= 0f) return 0f;
            }
            return Math.Max(0f, damage);
        }

        public void AddContaminationZone(float x, float y) =>
            ContaminationZones.Add(new ContaminationZone { X = x, Y = y, MinutesLeft = PlagueZones.MinutesToContaminate });
    }
}
