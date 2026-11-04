# ADR-0016 — Provisional numbers: night auction and night-market pets

Date: 2026-11-02 · Status: PROVISIONAL — replace when the GDD gives numbers

The GDD names the night auction (GDD 20 table: "Đấu giá (bậc III–IV, giấy Đỏ, bản vẽ Bạch Vân)"; opens with farm level
8–10) and says pets are traded at the night market ("trao đổi theo yêu cầu người bán: … thú cưng"; first pet at farm
level 3–4; slots 2/4/6 at levels 3/8/14). It gives no reserve price, rival rule or pet price. To make both playable:

| Rule | Value used | Where |
|---|---|---|
| Auction unlock | farm level 8 (lower end of the GDD 8–10 row) | `NightAuction.UnlockFarmLevel` |
| Lots | 3 per night, blueprints the farm has not learned, refreshed with the night market | `NightAuction.EnsureLots` |
| Reserve | the blueprint's build cost | `NightAuction` |
| Rival | one sealed bid per lot, 100–150% of reserve, seeded by day | `NightAuction` |
| Bid steps offered | reserve, +25%, +50% | `AuctionPopup` |
| Daily-support pet (Chó Chăn Đàn, Mèo Kho, Heo Săn Nấm, Quạ Tin) | 1 adult pig | `PetMarket.AdultPigsAsked` |
| Guard pet (Chó Ngao Canh, Ngỗng Báo Động, Chó Săn Chiến) | 2 adult pigs | `PetMarket.AdultPigsAsked` |

Pets are paid in pigs because the GDD's cheapest comparable trade (mercenary Vệ Binh Khiên) is priced in adult pigs
and the night market does not take gold for pets. Tests: `NightStallTests.RunAuction`, `RunPets`.
