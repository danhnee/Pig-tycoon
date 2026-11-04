# ADR-0020 — Defense structures, Build Room, waves, monsters: provisional values

Date: 2026-11-02 · Status: PROVISIONAL (GDD v6.0 ch.13–19 gives no figure for the items below)

Everything else in this change takes its numbers from `Docs/GDD_PIG_MECHANICS.md` ch.13–19 and table 1.5-D. That
covers the Build Room prices (1.500 / 6.000G + 40 thép / 18.000G + 100 thép + 2 Linh Kiện Tinh), the attack caps per
level, the damage, range, cadence and magazines in ch.16, the HP and charges in ch.17, the wear and durability bands in
ch.18, the generator's 12 m power radius, the monster HP in ch.19.1 and the Boss HP in ch.19.2. The data lives in
`Core/DefenseStructureRules.cs` (`DefenseStructureSpecs`).

## PROVISIONAL values (to replace when the GDD locks them)

| Item | Value used | Why |
|---|---|---|
| Wave reward | Sum of the defeated monsters' existing `EnemyData.GoldReward` (replaces the flat 100 G) | The GDD lists "thưởng đợt quái" as income (ch.24) but gives no figure. |
| Wave time cap | 9 real minutes, then the survivors withdraw; also used for minor waves | GDD 19 gives "4–9 phút thực" for a major wave, so the upper bound is used. No figure for minor waves. |
| Trap re-arm (Tấm Sốc, Hố Keo, Lưới EMP, Mìn) | 2 s | Same as the existing Bàn Chông `SpringCooldown`. |
| Trap trigger radius for "1 Ô Xây" traps | 1 m | Half of the 2 m build cell. |
| Trap wear per use | 0 | The ch.16 wear column covers only attack and info structures. Traps lose charges and durability from hits only. |
| Nỏ pierce line width | 0,6 m | "xuyên 2" gives no width. |
| Cờ Tập Kết zone | 12 m | GDD 1.5-D leaves it "chưa ghi". The existing table 15D value is kept. |
| Chó Sương speed in Sương Mù | ×1,3 | GDD: "nhanh hơn trong Sương Mù", with no figure. |
| UFO Trinh Sát mark radius | 6 m | GDD: "đánh dấu 1 nhóm heo". |
| UFO Thu Hoạch extra pigs | Up to 2 more within 3 m (1–3 pigs in total, per the GDD) | No radius in the GDD. |
| Chúa Trộm Rìu Sắt against gates | ×2 damage plus heavy blows (−3 durability) | GDD: "phá cổng rất nhanh". |
| Mẫu Hạm UFO | Drops a UFO Thu Hoạch every 30 s; group beam every 30 s on pigs within 4 m; hovers 3 m above the herd centre | GDD gives the 6 s beam but no period or radius. |
| Thực Thể Sương | Teleports inside the fence on each phase change (65 % / 30 % HP, existing `GetBossPhase`). Sets the weather to Sương Mù when it spawns. | GDD: "3 pha, dịch chuyển giữa pha", with no thresholds. |
| Kẻ Mang Dịch zone | 3 m (visual only); turns into Chuồng Nhiễm after 12 game hours (GDD) unless limed with item `VoiBot` | No radius or lime item in the catalogue yet. |
| Pig damage | Monster `AttackDamage` taken 1:1 from `Pig.Health` (0–100) | The GDD gives no pig combat HP. |
| Steel item | `ThepCuongLuc` (village barter "Thép Cường Lực") | The only steel material in the game. |
| Unpriced fuels (Tinh Thể Cộng Hưởng, Hộp Y Tế, Tinh Thạch Bạch Vân, Pin Tín Hiệu) | Cannot be bought for gold at the farm | GDD: night market only, with no gold price. |
| Build Room upgrade during a wave | Refused | The same rule as repairs (GDD 13.3 rule 6). |

## Known GDD mismatches left as they were (outside this change's brief)

- `EnemyData` still has Bầy Nhảy 85 HP (GDD 30), Thú Bão 260/340 (GDD 1.600), Đầu Lĩnh Trộm 500 (GDD 1.400) and Kẻ Mang Dịch 210 (GDD 900).
- Existing tests assert the Thú Bão value.
- `CanonicalSagaAndGdd6Systems.ThirtyOneBuildingsTable15D` still has non-GDD ranges and caps, for example Nỏ 28 m and 8 per farm. The tower view now uses the GDD values from `DefenseStructureSpecs`.
