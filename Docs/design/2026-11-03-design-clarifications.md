# Design clarifications: what the GDD leaves open, decided (3 Nov 2026)

The user asked on 3 Nov to dig out everything the GDD does not state or leaves unclear and decide it.

**Rules for this file**
- Each entry gives the gap, the GDD lines it touches, the decision, and why.
- Numbers marked **TẠM** are first values chosen to fit the GDD's own scale. The designer can change any of them; each lives in one named constant.
- Code points back here with the entry id (C-xx).

## C-00: Feed price
- **Gap.** The GDD sells "Thức ăn" at the day market (ch. 20) but gives no price. The only feed-price rule is "Khan Hiếm Thức Ăn: giá cám +35–70 %".
- **Decision.**
  - Feed is sold in 50 kg sacks at **2 G/kg** (TẠM).
  - The price is multiplied by the market's feed-price multiplier, so the Khan Hiếm theme raises it as the GDD says.
  - Sacks are sold 06:00–18:00 and go straight to the farm warehouse (the silo).
  - Constants: `TroughCommands.FeedSackKg`, `FeedPricePerKgProvisional`.
- **Why.**
  - An adult eats about 3.5 kg a day (GDD 4.1) and sells for a few hundred gold at about 15 days.
  - At 2 G/kg a pig's lifetime feed is about 90–110 G. That is a real cost but leaves margin, matching GDD 22: "gold always matters".

## C-01: Taking pigs to the day market (Đoàn heo đi chợ)
- **Gap.**
  - GDD 20 says the day market buys and sells pigs, and GDD 9.2 gives transport stamina (carry a piglet 3/min, drive adults 10/min, 70 % load).
  - GDD 4.4 says a pregnant sow should not be moved (stress +20).
  - It never says how a pig gets from the farm to the market. The old build sold any pig from a menu, so it vanished from the farm with no trip.
- **Decision.**
  1. On the farm, the pig card has **"Đưa vào đoàn đi chợ"**. Isolated pigs are refused. At most **12 pigs** per drove (TẠM; one gate pen).
  2. Pigs in the drove walk to the farm exit toward the town and wait there.
  3. When the player walks to the market town, the drove goes along. The drive costs **10 Stamina/min** if any adult is in the drove, plus **3/min per piglet**, for **4 game minutes** (TẠM), at 70 % load.
  4. A pregnant sow in the drove loses **Tinh thần 20** (the GDD "stress +20").
  5. At the market (06:00–18:00) only drove pigs can be sold.
  6. Walking back to the farm brings unsold and newly bought pigs home; they walk in by the shelter.
- **Why.** Selling becomes a decision made on the farm: which pigs, how many, is it worth the stamina. The GDD's transport costs finally apply, and the player can see the pigs leave.
- **Code.** `Core/Economy/MarketDrove.cs`, the world-graph warp hook, `PigInspectPopup` button, `PigAgentView` gathering, `DayMarketPopup` list.

## C-02: Buying pigs
- **Gap.** GDD 20 lists "Bán/mua heo thịt, heo giống" at the day market, but there was no way to buy a pig.
- **Decision.**
  - The day market offers **3–5 pigs a day**, refreshed with the day and the same all day.
  - Each pig's line is rolled with the natural rarity table (GDD 5.1: 78/17/4/0.9/0.1). Half are piglets (2–4 days old), half young pigs (6–10 days).
  - Price = what the market would pay for that pig × **1.15** (TẠM: within the GDD's 80–125 % day-market band).
  - Thường/Khá pigs show their line; rarer ones are "?" until appraised (GDD 5.1/5.4).
  - A bought pig joins the drove and walks home with the player.
- **Code.** `MarketPigShop`; `DayMarketPopup` "Mua heo" tab.

## C-03: When a pig eats
- **Gap.**
  - GDD 4.1 gives the daily ration, and GDD 3.4 a 30 m service radius.
  - It does not say when or how a pig eats on the interactive farm.
  - The old build fed every pig from the warehouse automatically while the troughs were decoration.
- **Decision (ADR-0028).**
  - A pig that has not had today's ration walks to a feeder outside night and storms.
  - It eats its whole ration from the trough if the trough holds enough. Otherwise it waits there showing "Đói!".
  - At day end a pig that did not eat is unfed, with the existing rules: Hunger +35 and no growth.
  - Feeders hold 50 kg and basins 100 L; a new game starts with them full.

## C-04: Quiet morale recovery
- **Gap.** GDD 4.3 says "Yên tĩnh, không có nguy hiểm: +5/giờ game" but not what "quiet" means.
- **Decision.** Quiet means outside a wave and with no Tinh thần loss for that pig during the hour. Farm bonuses add on top (Sinh Cảnh, density ≤ 70 %, old pigs −20 %).
- **Change.** A Hoảng loạn pig that refuses food is no longer punished again for the missed meal. That double rule killed hysterical pigs.

## C-05: Cycle length adjustment (two GDD thresholds)
- **Gap.** GDD 2.3 says Áp Lực ≥ 60 shortens the cycle by a day; L739 has a 50–79 pressure band for another table.
- **Decision.** The cycle rule follows 2.3:
  - pressure ≥ 60 or density > 120 % → −1 day (minimum 6);
  - losing ≥ 30 % of the herd in the major wave → +1 day.
- **Note.** "Main house destroyed" is not tracked yet; it counts as heavy loss once it is.

## C-06: Repairs on the 2D farm
- **Gap.** The repair rule needed location "Farm_Core", a 3D-era map that doesn't exist in 2D.
- **Decision.** Repairs are ordered while on the farm map, where the Phòng Xây Dựng stands.

## C-07: Health verb costs
- **GDD 6.5** gives "typical costs" and some ranges. Decision:
  - Isolation: 30 G per pig moved in (one temporary fence segment).
  - Lime: 20 G and **10** Stamina per plot (the GDD range is 8–12).
  - Treatment: 2 Stamina, plus **40 G** for group C (environment), **60 G** for A (contagious), **80 G** for B (persistent), **150 G** for D (gene). The GDD range is 40–150.
  - Vaccine: 50 G per healthy pig that is not yet immune.
  - Research: stays 120 G and is never charged twice.
- **Rules.** The cost is checked first and charged only when the action works.
- **Code.** `HealthCosts`, `GameEngine.ExecuteHealthTreatment`.

## C-08: Rare goods sources
**Gap.** Nothing in play ever gave Bí Kíp, Linh Kiện Tinh, Đá Cường Hóa, Bột Tử Tinh, Huyết Triện or Mực Ấn. That locked:
- Tuyệt kỹ and off-school skills;
- hall level 3, every Bạch Vân structure and Đài Thiên Lôi;
- paper enhancement.

**From the GDD.**
- Bí kíp: night-market barter (×2 weight in Mùa Trang Bị).
- Mực Ấn: night market.
- Đá Cường Hóa: auction, and 1–2 from a Boss.
- Bột Tử Tinh: Boss.
- Huyết Triện: cycle Boss from cycle 15.
- Auction paper lots: 15/10/5/2 % a night.
- Night stall papers: 5/2/0.6 %, never Đỏ.
- No papers at all before the mid-game gate.
- Bí nhân: no Đỏ.

**Decided (TẠM)**
- A cycle Boss also leaves 1 Linh Kiện Tinh, 1 Bí Kíp and 2 Bột Tử Tinh.
- Auction lots each night:

  | Lot | Chance | Reserve |
  |---|---|---|
  | Đá Cường Hóa (1–2) | 30 % | 500 G each |
  | Linh Kiện Tinh | 20 % | 3,000 G |
  | Bí Kíp | 15 % | 2,500 G |
  | Paper Lam | 15 % (GDD) | 600 G |
  | Paper Vàng | 10 % (GDD) | 1,800 G |
  | Paper Tím | 5 % (GDD) | 4,500 G |
  | Paper Đỏ | 2 % (GDD) | 12,000 G |

  Paper reserves are about 3× the colour's first enhancement gold.
- Barter values: Bí Kíp 2,400; Mực Ấn 60 per piece, 3 per stall.

**Code.** `RareSources`, `NightAuction`, `NightMarketCatalog`, `EconomyManager.GenerateNightMarket`.

## C-09: Blueprint sources by tier (GDD 13.1)
**The GDD's rules**
- Thường: available from the start.
- Tinh (Nỏ, Radar, Trạm Hồi Sức, Tấm Sốc, Cổng Gia Cố…): night market and Yellow events.
- Cao cấp: auction, Bí nhân and Red events, after the mid-game gate.
- Bạch Vân: "rất hiếm" at the auction, from cycle Bosses ≥ 10, high Red events and Bí nhân.

**Decided (TẠM)**
- Bạch Vân blueprints are possible at the auction one night in ten.
- A cycle Boss ≥ 10 gives one with a 1-in-4 chance.
- The farm still arrives with one Nỏ already built, but building more needs the bought blueprint.
- Instant placement (`ConstructBuilding`, used for the layout, the starter tower and fixtures) skips the learned check. The player's build order (`ConstructionQueue.TryStart`) enforces it.

## C-10: Farm expansion (GDD 3.2 / 3.6)
**Gap.** The farm starts at 12 land plots and can reach about 40, but the GDD gives no expansion rule ("Cần khóa"). Shelter roofs had no build path, so capacity was frozen at 84 and the 100-pig herd states were out of reach.

**Decided (TẠM)**

| Item | Rule | Cost |
|---|---|---|
| Land plot | limit = 12 + 2 per farm level above 1, at most 40 | 400 G + 12 Stamina (clear and fence 10×10 m) |
| Shelter roof | at most 2 per plot | 600 G + 20 planks |

- Upkeep stays as in the GDD: 4 G and 6 G a day.
- Feeders and basins are placed in the world as before.
- **Code.** `FarmExpansion`; buttons in the farm overview's capacity tab.

## C-11: Intel desk (GDD 15/23)
- Bàn Tình Báo is a Tinh structure (5,000 G) with "2 lượt hỏi". Intel can be bought only once the desk is built, and it answers **2 questions a day**.
- **Code.** `IntelBureau.DeskBlueprintId`, `QuestionsPerDay`.

## C-12: Fainting (GDD 8.2 / 19)
- **During a wave:** the player is carried to the main house and stays out until the wave ends. They lose 10 % of their gold and 20 Thể trạng, with no floor (the old floor was 20). At the end of the wave they wake with 60 % guard and stamina.
- **Outside a wave (exhaustion):** 2 game hours of recovery (TẠM).

## Fire mode and traps (GDD 13 / 17, rules not numbers)
- Fire mode is chosen before the wave and locked during it.
- Tiết kiệm fires only when ≥ 3 enemies are in range or the target is attacking or carrying a pig.
- Bàn Chông keeps its 6 charges as ammo; a repair restores health, not charges.

## C-13: Using items (GDD 12.5 / 12.8)
- **Where from.** A farm item is used from the space bag (always at hand), or from the warehouse through the bracelet link. The link is closed in combat. Before the bracelet there is no link.
- **Pig items** are given from the pig's card: Xô Nước, Thuốc An Thần Heo, Cám Linh Chi, Cám Hạ Hỏa and Hồ Sơ Giống Loài. The card shows up to three that the player has. In the bag, the button says "CHO HEO" and points to the card.
- **Effects follow the GDD table text:**
  - Xô Nước cures Say Nắng for 3 Thể lực and is kept.
  - Mái Che Tạm counts as a roof for 4 pigs until the next day starts.
  - Cám Linh Chi gives +5 Tinh thần, once per pig per day.
  - Cám Hạ Hỏa reduces Tinh thần loss by 20 % for that day.
  - Thuốc An Thần Heo gives +30 Tinh thần and stops a Lôi Tâm fit.
  - Hồ Sơ Giống Loài identifies the pig.
  - Tin Đồn Chợ names the cycle theme.
  - Bản Đồ Hướng Quái names the first-lane direction of the next major wave.
  - Chìa Cổng Ngược keeps an open gate 6 h longer (refused, and kept, with no open gate).
  - Lá Dò Sương watches for 24 h. A mystic fog rolled in that time is announced and starts 10 game minutes later.
- **Bồi Dưỡng Kế Vị** counts a day only when the candidate ate Cám Linh Chi that day: +3 %/day (+4 % with Khoa's Người Của Đàn), and −5 % for a missed day, using the GDD 4.6 trainer. The old +5 %/day trainer without feed is no longer used by the engine.
- **Quick bag (F5–F8).** Combat consumables move between the warehouse and the quick bag: through the link out of combat, or in person at the warehouse door (ADR-0012 rule).
- **Bình Hồi Lực** is drunk over 1 s. A hit or fainting in that second interrupts the drink and **keeps the bottle** (TẠM: the GDD says "bị ngắt" but not whether the bottle is lost).
- **Say Nắng incident (GDD 6.4).** In Nắng Gắt, 2 uncooled hours make a pig collapse. One more uncooled hour kills it. A collapsed pig that is cooled recovers: within reach of mud, a basin or a roof, under a Mái Che Tạm (6 m around where the player raised it), or doused with Xô Nước. On the farm, a collapsed pig lies still with a "Say nắng!" cue, and a pig overheating for 1 h or more shows "Nóng!".
- **Code.** `ItemUse`, `QuickBag.LoadFromWarehouse` / `UnloadToWarehouse` / `DrinkSeconds`, `OccultEncounterDirector.ExtendOpenGate` / `TickPendingFog`, `Farm.TempShadePigsToday`, `Pig.LinhChiFedDay` / `CalmFeedDay`, `GameEngine.FogWarningUntilMinute`.

## C-14: Lôi Tâm fit (GDD 6.4)
- **Trigger.** A pig with Lôi Tâm whose Tinh thần falls below 30 twitches for 10 s. This is visible: it shakes and shows a countdown bubble, and a toast fires.
- **Stopping it.** During the warning, Thuốc An Thần Heo or herding it to within 3 m of a shelter rest spot stops the fit. The 3 m is TẠM.
- **The fit.** Otherwise the pig runs wildly for 4 s. Every pig within 8 m drops into the panic band (Tinh thần 19), and there is a 25 % chance of sudden death.
- **Cooldown.** At most one warning per pig per 6 game hours (TẠM), so a pig that stays below 30 does not fit back-to-back.
- **What changed.** An outbreak, or the hourly disease tick, no longer marks a fit as pending. Before, a fit was marked pending every hour and never resolved.
- **Code.** `LoiTamSeizure`, `Pig.LastSeizureMinute`, `PigAgentView.UpdateLoiTamSeizure`.

## C-15: What the player knows about waves (GDD 2.3 / 23)
- **Major wave.** The day is shown exactly only inside the default warning, which is 1–2 days and rolled per cycle. Before that, only the Intel bought this cycle reveals it:
  - level 1: the stage's cycle band (6–8 / 7–10 / 8–12, golden 12–15);
  - level 3: ±2 days;
  - level 4: ±1 day, at night;
  - level 5: exact.
  - With no Intel, the HUD shows one dim skull and "chưa rõ ngày".
- **Minor wave.** Hidden until 2–6 game hours before it (rolled per wave). A powered Radar Uy Hiếp adds 6 h (TẠM, for "Radar có thể báo sớm hơn").
- **Golden season.** Announced at its start. No minor wave in its first 5 days.
- **Announcements.** Each warning is said once, as a toast. The old exact counters in the quest journal and the farm overview now show the forecast text.
- **Code.** `WaveForecast`, `GameClock.IsGoldenSeason` / `MajorWarningDays` / `MinorWaveNoticeHours`. The warning rolls use their own random stream, so a seed's cycle lengths are unchanged.

## C-16: Stamina terms (GDD 9.3)
- **Hunger.** Not eaten for more than 8 h adds +10 % to work cost; more than 16 h adds +25 %. Starting a game counts as a meal; eating resets it.
- **Carrying.** Carrying more than 35 kg adds +35 % to movement cost, plus 15 % more under Thể trạng 40. It was ×1.25 before, and its condition check never matched.
- **One cost path.** Dousing with Xô Nước goes through `FarmLaborStamina.Cost(DouseBucket)`, like every other farm job.
- **Still open.** The "bộ trang bị hạng Nặng +8 %" term waits for an equipment weight class. The Trạm Hồi Sức aura reduction waits for the player position in Core.

## Open items (need the designer's call; current behaviour noted)
| Item | GDD | Current |
|---|---|---|
| Medicine price per drug | 40–150 G range only | decided in C-07 |
| Build cost of a shelter roof / clearing a land plot | upkeep 4–6 G given, build cost not | decided in C-10 |
| Bí Kíp drop rate | sources named, no rate | decided in C-08 |
| Linh Kiện Tinh quantities | needed for hall lv3, no source rate | decided in C-08 |
| Unpriced fuels (Hộp Y Tế, Tinh Thể Cộng Hưởng…) | no price | cannot be reloaded (todo) |
| Faint gold fee | GDD: −10 % gold only in ch.19, ch.8 none | decided in C-12 (−10 % kept, ch.19) |
| Farm-state threat multiplier ×1.0–1.4 | formula input undefined | not applied |
| Event 3-day timeout reward | undefined | 30 % reward |
