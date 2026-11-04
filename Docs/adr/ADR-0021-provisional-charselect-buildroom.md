# ADR-0021 — Character select and Build Room duty: provisional values

Date: 2026-11-02 · Status: PROVISIONAL for the items in the table below. Everything else follows the GDD.

## Taken from the GDD (not provisional)

- GDD 13.2: a structure needs three things:
  - a learned blueprint;
  - a Phòng Xây Dựng of the required level;
  - an NPC on duty in the room.
- GDD 13.2 / 11.1: room efficiency by the NPC on duty:
  - another NPC: 40%;
  - Kỹ Sư Xây Dựng without his trait: 70%;
  - Kỹ Sư Xây Dựng with his trait: 100%.
- Real time = base time / efficiency. The example is Skyhook: base 8 h becomes 20 h, 11,4 h or 8 h.
- No NPC on duty: the room stops. It does not build and does not repair.
- Build slots ("Ô thi công") by room level: 1 / 1 (+1 if the Kỹ Sư has his trait) / 2 (+1).
- Repair base times are 15 / 30 / 45 / 60 min (GDD 18), divided by the same efficiency.
- GDD 8.1: An and Khoa base stats and basic traits, from `PlayerRoster`.

Code:
- `Core/DefenseStructureRules.cs`, class `ConstructionHall`;
- `Core/ConstructionQueue.cs`;
- `Core/NewGameSetup.cs`.

## PROVISIONAL values (replace when the GDD locks them)

| Item | Value used | Why |
|---|---|---|
| Base build time per structure | Thường 1 h • Tinh 4 h • Cao cấp 8 h • Bạch Vân 16 h (game time, at 100%) | The GDD gives only Skyhook (Cao cấp) 8 h. The other tiers are scaled from it by blueprint tier. |
| Who may staff the room | An active **farm** NPC (GDD 11.1, 7 roles). Kỹ Sư Xây Dựng is picked first. Mercenaries (11.2) never staff it. | GDD 11 role split: "NPC trang trại … không bao giờ tham chiến" and combat NPCs are only deployed in waves. "NPC bất kỳ khác nghề" is read as any other farm NPC. |
| Kỹ Sư trait flag | `HiredNpcRecord.TraitUnlocked` (default false; old saves false) | The GDD has six hidden locks per NPC, but the code tracks no NPC lock progress yet. Nothing unlocks the flag in play yet. |
| Room stopped with work in progress | Build and repair orders pause and resume when someone is on duty again. Nothing is refunded. | The GDD says only "phòng ngừng hoạt động". |
| Payment time | Gold is paid when the order starts. A finished order that somehow breaks a cap is refunded. | Not specified. |
| Progress with a changing NPC | Each game minute adds the current efficiency. Swapping the NPC changes the speed of the remaining work. | Follows "time = base / efficiency" when the efficiency changes mid-build. |
| Build during a wave | Orders advance only while the farm clock runs. `GameEngine.Tick` is paused during waves. | Same as repairs (GDD 13.3 rule 6, "Không sửa trong đợt quái"). |
| Start screen | Shown once on a new game with no save, no Android launcher preset (`pigtycoon.player` extra), no `-pigtycoon-scenario=video` and no `-pigTycoonSmoke`. Game time is paused while it is open. Default without a choice is An. | HANDBOOK P1.02/P1.21 asks for the screen but not for these details. |
| Mid-session sprite swap | The active actor keeps its controller and swaps to the other actor's `PixelMythSpriteLibrary`. Later scene loads activate the matching actor (`PixelWorldPlayerRoster.Awake`). | Both actors share one controller (P1.02). Swapping GameObjects mid-scene would break references other systems have cached. |

## Not done (gaps)

- No gameplay path unlocks the Kỹ Sư trait yet.
- The trait's other effects are not applied:
  - materials −10%;
  - 25% scrap refund on repairs.
- "Tự tay phụ Phòng Xây Dựng −15%" (GDD 9.2 labour table) is not implemented.
- `FarmCoreAndVillageSystem.GetConstructionHallEfficiency` (MemberACDBackendSystems.cs) is now unused. It ties 70%/100% to the room level instead of the trait, so it disagrees with the GDD and should be removed by its owner.
- `DefenseManager.TryConstructBuilding` stays an instant build for fixtures and old tests. The player flow uses `ConstructionQueue`.
