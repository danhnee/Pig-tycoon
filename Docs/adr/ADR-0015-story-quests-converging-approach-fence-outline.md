# ADR-0015 — Season 1 quest line, converging monster approach, non-rectangular pasture

Date: 2026-11-02 · Status: accepted (user direction 02/11, Opus = logic, Argon = UI/art)

## 1. Quest line follows the story doc, not the old Core table

`StoryJournalSystem` carried 20 main quests whose titles and goals were invented (Q01 "Dấu răng trên máng gỗ", UFO at
Q10 …). The story doc (`Docs/COT_TRUYEN_PIG_TYCOON.md` lines 344–531) has its own table: Q01 "Cậu tới nhận trại" …
Q20 "Rìu Sắt trả hàng", with unlock conditions, objectives and rewards (150G/80 EXP … 800G/300 EXP).

Decision: `Core/StoryQuestBook.cs` holds the doc's Q01–Q20 (titles, triggers, objectives, rewards copied) plus the side
quests whose triggers the game can observe (S02, S10, S11). Steps read real state (major waves survived, cycle bosses
defeated, herd tier, hired NPCs, cycle number) or `QuestSignal`s recorded *while the quest is active*, so an action done
before the quest opened does not count. The legacy records are overwritten with the doc titles when a quest completes.
Q20 ends with the 8.1/8.2/8.3 choice. Saved in `GameSaveData.QuestsCompleted/QuestSignals/StoryEnding`.

Simplifications (story beats that need evidence items the catalog does not have): "carry the Q03 slip to Bà Sáu" etc.
are visits to the right person (station ids in `StoryQuestBook.SignalForStation`); Q12 "buy intel" is a visit to Lệ.

## 2. Fence repair prices

- Temporary panel: 30G per segment (GDD 23 "Rào tạm 30G/đoạn", same price the story gives in Q01). It restores 1/3 of
  the segment's 300 HP (not specified by GDD; presentation choice).
- Real repair: GDD 18 gives light works "10–25G + 2 scrap per 10 durability". A wooden fence is the lightest work, so the
  lower bound 10G/10 HP is used; scrap is not charged because the 2D inventory has no scrap item yet. Only outside a
  wave and only with someone staffing the Construction Hall (GDD 13.2, story Q06).

## 3. One breach front

User 02/11: monsters come from several directions but must converge on one. Entries stay spread over four sectors
(GDD 3.8, the sector is intel). `Core/FarmApproachRoute` routes every ground monster along the outer lane (9 m outside
the fence) the shorter way round to one breach point on the south fence; flying/occult monsters fly straight in.
Players fortify one front; the HUD banner names the sector and "dồn về CỬA NAM".

## 4. Pasture outline

User 02/11: the pasture must not be a box; the town and the village lie in different directions. The fence is a
rectilinear polygon inside the old 120 × 90 m box (bites at SW, NE, NW), written to the layout as `fence_polygon`;
`FarmEnvironment2D.IsInsidePasture` and building rules use it. Core keeps the bounding rectangle for service radii.
Town portal north (122, 158.5), village portal west (1.5, 112).
