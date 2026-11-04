# Playtest findings — Core bot, 02/11/2026

Run from the repo root (.NET 8.0.417): `dotnet run --project Tests/Playtest/PigTycoon.Playtest.csproj -c Release`. Add `-- --diag` for a 16-day herd-mood trace.

Bot:
- 6 seeds (11, 23, 37, 41, 59, 73) × 31 cycles; An/Khoa alternate; seeds 59 and 73 play lean pure farming.
- Every day it farms, sells, breeds, appraises, buys intel, signs/renews/forces down/terminates NPC contracts, builds, buys gear and learns skills.
- It fights through `ExecuteLiveCombatWave` and checks a save/load JSON diff every 3 days, plus daily invariants.
- It also runs 22 probes (A–V); "state injection" marks a probe that sets a field directly.

**P0: none.** Status column: FIXED = fixed in this round (see the commit notes / `Docs/plans/2026-11-02-logic-ui-audit.md`).

| # | Sev | System | Observed vs expected | File:line | Status |
|---|---|---|---|---|---|
| 1 | P1 | Faint rescue | `ExecutePlayerFaintRescue` uses `Clock.TickMinutes(120)`. A faint at 04:00–05:59 crosses 06:00 without the daily reset | GameEngine.cs:1249 | FIXED |
| 2 | P1 | Contracts × Core waves | `NpcContracts.OnWaveEnded` runs only on won major waves in the Core resolver | GameEngine.cs:1187-1206 | FIXED |
| 3 | P1 | Wave major/minor | Threat + `PendingThreatBudgetBonus` ≥ 150 turns a minor wave into a major | GameEngine.cs:1129, 1187 | FIXED |
| 4 | P1 | Martial | `ExecuteMartialSkillWithCombo` learns any skill for free | GameEngine.cs:919-921 | FIXED |
| 5 | P1 | Low-guard tank | Eat/rest refill past the ×0.75 tank | Character.cs:152-157; PlayerRestCommands.cs:78-97 | FIXED |
| 6 | P1 | Rune engraving | Failed attempts cost nothing; unseeded `new Random()` | MemberACDBackendSystems.cs:882-897 | FIXED |
| 7 | P1 | Paper salvage | Enhancement level kept after salvaging | SpecialtyPaperActions.cs | FIXED |
| 8 | P1 | NPC quests | Elite/UFO/thief kill counters move only in the Unity runtime (global AnyDied hook), not in Core-only waves | NpcContracts.cs | runtime OK; Core sim gap |
| 9 | P1 | Cleaning | `TryCleanYard` and labour `CleanPlot` each update only half of the counters | HerdLifecycleRules.cs:376; FarmLaborStamina.cs:139 | FIXED |
| 10 | P1 | Save-scum | Appraisal lock/failures, sick-sale penalty, threat bonus and sold-today counters are not saved | GameEngineEconomyIntel.cs:52-62; GameEngine.cs:1415-1471 | FIXED |
| 11 | P1 | Save: pig state | Feed conversion, infection and latent multipliers, MaxHealth and Comfort reset on load | GameEngine.cs:1865-1925 | FIXED |
| 12 | P1? | Feed | A feed sack added to the inventory both stays and adds kg | StardewGradeArchitectureSpine.cs:632; PigCareCommands.cs:85 | suspected |
| 13 | P2 | Core wave resolver | Loses almost every early major wave; the cycle stalls without the runtime mirror | GameEngine.cs:1163-1208 | sim only |
| 14 | P2 | Cycle length | Every cycle after the first is 1 day shorter than rolled | GameClock.cs:117, 177 | FIXED |
| 15 | P2 | Save / clock | `CycleDayIndex` is clamped on load while the major wave is overdue | GameClock.cs:235 | open |
| 16 | P2 | Economy | Upkeep 201G/day from day 1; no feed shop; upkeep silently skipped when gold is low | Farm.cs:95; GameEngine.cs:608 | balance — needs the user |
| 17 | P2 | Piglet mill | Newborn piglets sell for a flat ~110G (81% of the lean bot's income) | EconomyManager.cs:256-265 | balance — needs the user |
| 18 | P2 | Contract exits | Defaulting costs less than terminating | NpcContracts.cs | open |
| 19 | P2 | Trust skip | Affinity 100 forgives the instalment instead of deferring it | NpcContracts.cs:490-495 | FIXED |
| 20 | P2 | Instalment rounding | Overpays 1–2G | NpcContracts.cs:323 | FIXED |
| 21 | P2 | Renewal ratchet | Force down when cheap, lock when dear → never pays market price | NpcContracts.cs:365-374 | design — needs the user |
| 22 | P2 | Day market | Selling allowed at night in Core | GameEngine.cs:1015-1079 | see status below |
| 23 | P2 | Seed | `BaseSeed` not saved; every runtime game uses seed 20260601 | GameEngine.cs:110; MobileGameController.cs:42 | FIXED |
| 24 | P2 | Low-guard flag | Stays true after the daily reset; lost on load | GameEngine.cs:657, 1667 | FIXED |
| 25 | P2 | Save aliasing | `IsPowered` false→true on load | GameEngine.cs:1340 | open |
| 26 | P2? | Night market | "Cám Hạ Hỏa" bought for gold is not feed | Character.cs:946 | suspected |

Economy trajectory:
- Seed 59, lean bot: 2.5k → 18k gold by cycle 10, carried by piglet sales. The breeding stock then aged out and gold fell to a 150–400 floor.
- Seed 23, full bot: 30 of 33 contracts defaulted; gold sat at the floor from cycle 4.
