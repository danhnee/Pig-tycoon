# GDD and story gap backlog: 3 Nov 2026

**Source.** Six read-only audits of the code against the design and story documents:

- `Docs/GDD_PIG_MECHANICS.md` chapters 1–4, 5–7 + 9, 8 + 10–12 and 13–24;
- the story canon `Docs/story/COT_TRUYEN_2026-11-01.txt`;
- the UI research behind `2026-11-03-ui-ux-spec.md`.

The user directive this answers: finish all logic, make it actually run in the Unity game with the UI/UX, and clean the project.

**Status values**
- **done**: fixed, with a test or an in-game probe.
- **todo**
- **designer**: a number or a decision the GDD does not give. Do not guess it.

Line references are GDD lines.

## P0: blockers and exploits (S)

| # | Item | Status |
|---|---|---|
| 1 | `WaterRefilled` quest signal was never sent, so Q01 and the whole main line Q02–Q20 could not finish. Now sent from `WaterTrough2DView.Refill`, and every quest signal must have a sender (coverage test) | done |
| 2 | Opening the Character panel called `SpecialtyLocks.ObserveDay`, so each redraw counted as a day and completed Khoa's lock 3 | done |
| 3 | Base + bonus stat was not capped at 55 (8.3) | done |
| 4 | Appraisal lock never counts down (a failed pig stays locked forever). Lock and pity should be per line × channel, with a 12 h success lock and a 24 h failure lock (5.4) | done (hourly countdown, 12 h success lock; per line×channel still todo) |
| 5 | An unidentified "?" pig card leaks the gene line and passive; the breeding preview leaks the partner's line | done |
| 6 | Hư Thể seal and countermeasures are a free button. Per GDD they require real conditions (density ≤ 90 %, fixed feed, items) | todo |
| 7 | Hysterical pigs (Mood < 20) refuse food, take −12 mood/−8 health a day and spiral to death. Quiet recovery should be +5 per hour (4.3), not +5 per day | done (+5/h when no mood loss that hour; hourly latent check) |
| 8 | Prologue interrupted mid-chain never resumes, so `prologue_done` is never set and all Ch1 and village scenes stay locked | todo |
| 9 | (depends on #39: Q08–Q19 talk scenes must exist first, otherwise the main line blocks) Any small talk sends the quest Talk signal, so Q08, Q09, Q12, Q14, Q17 and Q19 finish by chatting | todo |
| 10 | Đột Phá breakthrough always boosts stat index 4 (Temperament); the index math is wrong | done |
| 11 | `EnemyWaveSpawner` calls `CompleteMajorWave()` with no arguments, so the cycle −1/+1 day rules never apply | done (world + sim use GDD 2.3: ≥30 % loss, pressure ≥60 / density >120 %) |
| 12 | The reflection overlay always shows Khoa's portraits; An's mirror scenes show Khoa | done (neutral silhouette until An portraits are approved) |
| 13 | Quest-completion toast uses Khoa's line on An's route | done (An route shows no Khoa line; AnLine content todo) |

## P1: core loop really playable (M)

| # | Item | Status |
|---|---|---|
| 14 | Troughs drive feeding; pigs seek feeders; feed sacks at the market (ADR-0028) | done (feed price **designer**) |
| 15 | **Selling:** the day market lists every live pig (piglets, pregnant, sick, isolated) and removes it with no transport. Needs consignment, a holding pen or cart, pregnant/isolated blocks, and market hours enforced in Core | done (C-01 drove) |
| 16 | **Buying pigs:** no command exists | done (C-02) |
| 17 | **Capacity frozen at ≤ 84:** counts are fixed defaults. Derive them from service points; add commands to build a shelter roof and clear a land plot (costs **designer**). The herd state is unreachable until this exists | done (C-10) |
| 18 | **Health verbs are free** (lime, treatment, vaccine, fence); environment treatment cures the whole farm; cures are instant. Needs costs, stamina, zone closure. Per-drug price **designer** (GDD gives 40–150 G) | done (C-07; zone closure + treatment course still todo) |
| 19 | **Outbreaks:** should roll per 10 m cluster with a 0.2 % floor (currently gated off at SC ≥ 50). Dịch Tả needs a 3-day lockdown, 8 %/day deaths, a cull command; Chuồng Nhiễm should be ×2 | todo |
| 20 | **Visible pig incidents:** sick icon, Say Nắng collapse clock (2 h → collapse, +1 h → death) with douse at 3 STA and Mái Che Tạm, Lôi Tâm seizure | **partial**: Say Nắng clock + douse + temp shade (C-13) and Lôi Tâm warning/fit (C-14) done; a sick-status icon over pigs is left for Argon |
| 21 | **Farm EXP:** a survived wave gives 150 × cycle (max 3,000) — done; still todo: new buildings give 5 % of their value; level unlocks (night market and breeding at 3, research at 8, …) need to be gated | todo |
| 22 | **Price fixes:** best weight 95–110 (code ≤ 115); old-pig price range plus a buyer chance (distribution **designer**) | **blocked (designer)**: weights above 110 kg exist only for Thiết Bì (cap 130 in code), and the GDD table gives no multiplier above 110. The old-pig ×0.15–0.30 and the 0–10 % buyer chance have no distribution. The price formula is protected (AGENTS.md), so nothing changes without a designer number and a red test |
| 23 | **Leader and heir:** a 20 m anchor applied hourly; real training tasks (+3 %/day, cap 30, −5 % per missed day); succession roll; herd conditions 3 and 6 and the disband rule; the world's alpha should be the Core leader | todo |
| 24 | **Morale hooks:** monster within 20 m, being hit (−20), peer death within 10 m (−6), corpse in sight (−2/h); hysterical fence ram at 5/s and 5 %/h break-out | todo |
| 25 | **Corpse staging lot:** 4/8 slots, SC/ÁLSK ticks; drop the world's ÁLSK −8 on cleaning | todo |
| 26 | **Warnings:** minor waves need 2–6 h notice; the golden-season flag (no minor wave in the first 5 days); the HUD countdown should depend on the Intel level instead of always being exact | **done** (C-15: `WaveForecast`, 1–2 day major warning, 2–6 h minor notice + Radar, golden season, HUD/journal show only what is known; 4 tests) |
| 27 | **Lab appraisal** (`EnqueueGeneAppraisalAtLab`) is unreachable; the 12 %-of-nights high appraisal is missing; day-market appraisal should cost 5 STA | todo |
| 28 | **Stamina 9.3/9.6:** hunger +10/+25 %; heavy carry ×1.35 (the condition branch is a dead string compare); per-minute cost for herding and carrying piglets; one cost path; forced-rest items | **partial** (C-16: hunger +10/+25 %, carry > 35 kg +35 %/+15 %, douse on the shared cost path; heavy-gear +8 % and Trạm Hồi Sức aura left) |

## P1: progression and items (M)

| # | Item | Status |
|---|---|---|
| 29 | **Bí Kíp has no source:** 4 Tuyệt kỹ and every off-school skill are unreachable. Add auction lots and Boss rewards (rate **designer**) | done (C-08) |
| 30 | **Specialty papers:** should drop only after the mid-game gate; the auction should be the main source (15/10/5/2 %); the stall should sell at 5/2/0.6 % with no Red; remove Red from the Bí nhân pool | done (C-08) |
| 31 | **Enhancement materials** (Mực Ấn, Đá Cường Hóa, Mực Phù) never reach the inventory, so +0→+1 is impossible. Runes and seals should cost materials and gold (prices **designer**) | partial (C-08 sources; rune/seal material costs todo) |
| 32 | **Tactical/farm items have no use:** Cám Linh Chi (heir), Mái Che Tạm, Thuốc An Thần, Tin Đồn Chợ, Lá Dò Sương, Bản Đồ Hướng Quái, Chìa Cổng Ngược. The inventory should fill the quick bag. Bình Hồi Lực needs a 1 s channel. 3 item tiers are wrong | **done** (C-13: `ItemUse`, quick bag from warehouse, 1 s drink, Linh Chi heir, 16 items aligned to GDD 12.8; 5 tests) |
| 33 | **10.8 specialty effects:** Làm Dứt Điểm missing; Xem Trước Chợ wrong; Giữ Chỗ never called; Mặc Cả 90 % wrong; Khoa lock 1 should require "main house not destroyed"; trait name text is wrong | todo |
| 34 | **Runes:** bind to an item (tier ≥ II, one per item); add Phù Hộ Đàn; delete the phantom runes | todo |
| 35 | **Faint:** Thể trạng −20 with no floor, wake after the wave. The gold fee is not in the GDD (**designer**) | done (C-12) |

## P2: world presence and story content (L)

| # | Item | Status |
|---|---|---|
| 36 | **Farm NPC verbs:** vet (10 free quarantines + diagnosis), Nông Công (4 feed/clean jobs, not 15 kg from nothing), Thủ Kho (preload ammo), Nhà Buôn (sell 10 pigs), Quản Lý Ca (scheduling). NPCs should walk and do the jobs | todo |
| 37 | **Pets in the world** with their verbs and food cost; Chó Ngao bite 20 (code 45); Chó Săn Chiến sold at the auction | todo |
| 38 | **Mercenary signatures:** Chặn cửa / Tường Người 30 %, trap reset, Đạn Xuyên Giáp ×4 | todo |
| 39 | **Chapters 2–4 scenes** (both routes): chapter cards, start/done scenes, chapter-end images | **done** (4 Nov: NarrativeContentChapter2/3/4, 130 scenes, both routes; new backdrops requested from Argon in #166) |
| 40 | **Endings 8.1–8.3** as scenes from the Q20 box choice; 8.4 epilogue; 8.5 post-credit; remove the tracker ending buttons | **done** (4 Nov: 8.1–8.3 from the Q20 choice, 8.4 epilogue, 8.5 post-credit; Core finishes Q20 from the `ending:` effect) |
| 41 | **Real notebook "Thấy / Có thật":** `notebook:` effects store entries, a verify step moves them; remove the old contradicting lore entries; gate the reflection browser | todo |
| 42 | **Side quests S01–S12 and mystery events E01–E14** with canon lines; `ForestEventResolved` signal | todo |
| 43 | **NPC arcs:** affinity milestone scenes (30/50/80/100) | todo |
| 44 | **Gene/trait behaviours:** remove invented numbers (Thổ Trầm −18 %, Mộc Cước speed, Hồng Điền ×1.10, Thiết Bì 130 kg, F1 ×1.08, inbred ×0.90, Trung Thành bonding) — each needs a red test plus an ADR. Add the missing ones (Xích Mao research, Kim Thọ −15 %/+20 %, Lôi Mạch/Mộng Nhãn signatures, Cắn Đàn/Ồn Ào/Kêu Báo, Kén Ăn) | todo |
| 45 | **Period and weather effects beyond stamina:** afternoon market +10 %, night thief +20 %, water demand, Rét piglets 3 %/day, storm strikes; Sinh Cảnh terms for dirty water, rain drainage, Nồm, vet | todo |

## Chapters 13–24 (defense, waves, markets, Bí nhân, budget, intel, events)

| # | Item | Size | Status |
|---|---|---|---|
| 46 | **Repair probably blocked in play:** `DefenseRepairs.Request` requires location `Farm_Core`, which the player flow never sets. Check the player is at the Phòng Xây Dựng / on the Farm instead, and confirm at runtime | S | done |
| 47 | **Linh Kiện Tinh has no source.** This locks hall level 3, every Bạch Vân and Đài Thiên Lôi, and Bạch Vân repairs. Add it to auction, Bí nhân, Red events and Boss drops (quantities **designer**) | S | done (C-08) |
| 48 | **Bạch Vân manual activation:** stand within 12 m and hold 1.5 s, or use the console (+3 s); Cửu Tiêu zone is the player's choice. It is only reachable from the dev panel today. Remove Vạn Linh's passive +100 % regen everywhere. The invariant "only Bạch Vân is manual" is violated | M | todo |
| 49 | **Ammo economy:** ammo comes only from the night market or crafting (no gold reload at the hall); the unpriced fuels need item ids (prices **designer**) | M | todo |
| 50 | **Build costs:** sync to ch.14–17 (e.g. Nỏ 2,400 G + 30 gỗ + 10 thép), charge materials in `TryStart`, drop the invented 6/14/24/32 cap or record it in an ADR. Hall level 1 costs 1,500 G | M | todo |
| 51 | **Monster HP:** Bầy Nhảy 30, Thú Bão 1,600, Kẻ Mang Dịch 900, Đầu Lĩnh Trộm 1,400 | S | done |
| 52 | **Blueprint tiers:** starting set is Thường only (RaoThep, TramKhiTuong, CotThuLoi); fix the night-market and auction tier filters | S | done (C-09) |
| 53 | **Fire mode:** Tiết kiệm means ≥ 3 targets or a target attacking a pig; mode locked during a wave | S | done |
| 54 | **Bàn Chông:** charges stored in durability, so a repair gives 100 charges | S | done |
| 55 | **Missing structure effects:** Trạm Hồi Sức aura, Ụ Bao Cát −40 %, Khiên overload, Chốt Canh Gác, Đèn Hiệu, Trạm Khí Tượng forecast, Đài Dò Dị Tượng, Radar / Cộng Hưởng day-before warnings | L | todo |
| 56 | **Real-time power grid:** per-shot loads, 10 s overload window, generator tiers 30/70/140 u, Diesel per minute | L | todo |
| 57 | **Waves:** target order pigs → storage → infrastructure → house → player; wire the faint penalty (`HandlePlayerUnconscious` has no caller; overlaps #35); farm-state multiplier (**designer**); defensive occult events | M | partial (faint wired; target order, farm-state multiplier, occult events todo) |
| 58 | **Intel:** needs a built Bàn Tình Báo; 2 questions a day | S | done (C-11) |
| 59 | **Events:** only 4 of the 14 templates can roll; Green never rolls; 3-day timeout reward (**designer**) | L | todo |
| 60 | **Day market:** buy pigs (#16), hard daily cap for adult pigs, Core hour check, delivery contracts (`PigDeliveryContract` has no callers), Diesel/material/medicine stock, regional epidemic sale ban | M | partial (buy pigs + hours in Core done; contracts, stock, epidemic ban todo) |


## Process rules

- One item at a time:
  - write a Core test;
  - wire it into the world and UI;
  - run `tools/ui_probe.sh` and record a video before marking it **done**.
- Number changes against existing code need a red test and an ADR (AGENTS.md).
