# ADR-0028: The herd eats at the troughs the player fills

Date: 3 Nov 2026. Status: accepted (logic). Numbers marked TẠM need a designer decision.

## Context

The user reviewed the build on 3 Nov and said the game "can't do anything yet". They asked how scattered pigs get to the feeding spots and how pigs are taken to sell.

The gameplay audit found two feeding systems that never touched:

- **The farm view.** Silo, feeders and basins kept their own stock. Pigs walked to a feeder on a 25 % dice roll and added weight to the Core pig directly.
- **Core.** It fed every pig from `Farm.FeedStockKg` at day end, whatever the troughs held. `PlayableMapConfiguration.InteractiveHerdOwnership` already existed for this case, but was never set.

## Decision

- **Troughs hold stock in Core.** `FarmServicePoint` gains `FeedKg` / `WaterL`, saved with the map. Commands live in `TroughCommands`:
  - `TakeFeedFromStock` (silo → bag);
  - `FillFeeder`, `FillBasin`;
  - `PigEatsAtFeeder` (the daily ration `PigCareCommands.DailyFeedKilograms`, taken from the trough);
  - `PigDrinksAtBasin`;
  - `RegisterPoint` / `RemovePoint` for feeders and basins the player builds.
- **The farm map turns on `InteractiveHerdOwnership`.** Older saves are switched on when they load. The existing end-of-day rollup then marks a pig that did not eat at a trough as unfed: Hunger +35, `FedToday = false`, and the growth rules that follow.
- **Views are proxies.**
  - `Feeder2DView`, `WaterTrough2DView` and `FeedSilo2DView` read and write Core, binding to the nearest service point.
  - Pigs go to a feeder when Core says they have not eaten today (`WantsFood`), not on a dice roll. An empty trough makes them wait there with "Đói!".
  - The view no longer adds weight.
  - Far pigs keep their logic running. The old 40 m freeze made a scattered herd miss its ration.
- **Feed is restocked at the day market** (`BuyFeedSack`, 06:00–18:00, ledger reason `BuyFeed`). This was the only missing source.

## Numbers

| Value | Source |
|---|---|
| Feeder 50 kg, basin 100 L, scoop 20 kg, bucket 50 L | The scene objects already used these values |
| New-game troughs start full (50 kg / 100 L) | The previous scene defaults |
| Daily ration | Unchanged (`DailyFeedKilograms`) |
| **Feed price: 2 G/kg × the market's feed multiplier, sold in 50 kg sacks** | **TẠM.** The GDD gives no feed price, only "Giá cám +35–70 %" in Khan Hiếm Thức Ăn (L3897) |

## Not done yet

These come from the same audit and are follow-ups:

- **Selling.** The day market still lists every live pig, including piglets and pregnant sows; nothing is physically taken to market.
- **Buying pigs.** There is no command for it.
- **Animals.** No herding by walking behind them, no isolation pen, and no sick or heat-stroke visuals.
- **NPC chores.** NPCs do no farm chores in the world.
