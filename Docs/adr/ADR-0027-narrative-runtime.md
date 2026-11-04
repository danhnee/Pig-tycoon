# ADR-0027 — Narrative runtime: dialogue, scenes, NPC talk (Stardew / VN quality)

Status: accepted (2 Nov 2026). Owner: Claude (logic). UI and art: Argon.

## Context

The user rejected build 0.9.0. Story, quests and interaction screens were raw text. The bar the user set is "at least Stardew Valley storytelling or a Japanese visual novel", with UX first and market practice checked.

Before this ADR the game had:
- quest data in `StoryQuestBook`;
- one fixed 3-second speech bubble per NPC (`PixelMythNpc`);
- no scenes, no choices, no portraits and no prologue.

## Decision

### 1. One Core runtime for every story moment

All story moments — prologue, quest beats, NPC talk and mystery quiet beats — run through one runtime. It lives in `Assets/Scripts/Core/NarrativeRuntime.cs` (pure C#, no UnityEngine).

- **Scenes are command lists**, built in C# so the compiler checks the content:
  - lines: `Say` (speaker + portrait + side) and `Narrate` (first-person monologue at the bottom edge);
  - flow: `Choice`, `Label`, `Goto`, `IfGoto`;
  - effects: `Effect`;
  - stage: `PlaceAt`/`MoveTo` to map marks, `CameraTo`, `Face`, `Emote`, `Fade`, `Pause`, `Shake`, `Sfx`, `Music`, `Banner` (chapter or date card) and `Backdrop` (full-screen illustration for places that are not maps).
- **Triggers** (Stardew event preconditions) are: NewGame, DayStart, EnterLocation, Signal (QuestSignal), Talk, QuestCompleted, WaveEnded, Sleep and Manual.
  - Conditions are GameStateQuery strings. This ADR adds four keys: `STORY_FLAG`, `STORY_QUEST_DONE`, `STORY_QUEST_ACTIVE` and `SCENE_SEEN`.
  - Scenes are once-only by default. Each one is recorded in `TriggerManager.EventsSeen`, so `PLAYER_HAS_SEEN_EVENT` works.
- **Reading UX** follows the Ren'Py defaults, adapted for touch:
  - text types out at 45 cps; 0 shows it instantly;
  - the first tap completes the line, the next tap advances;
  - auto mode waits 1.2 s + 0.035 s per character;
  - hold to fast-forward, read lines only (an "skip unread" setting is off by default);
  - backlog of 200 entries, including the choices taken;
  - text scale setting.
  These are UX defaults, not balance numbers.
- **Skip** applies every remaining effect on the default choice path and ignores stage commands, so a skip can never leave the game half-updated. This avoids the bug where skipping Shane's heart event in Stardew left state broken. A skipped scene shows its Recap card (Honkai: Star Rail 3.4 style). Scenes can opt out of skipping.
- **Choices** can be locked. A locked choice can still be shown, with its reason (Ren'Py `menu_include_disabled`).
- **Protagonist variants** are written inline as `{an:…|khoa:…}`. Key scenes are separate per route (story bible 2.7: An's route is rewritten, not just re-addressed).

### 2. NPC talk

`GameEngine.TalkTo(castId)` picks, in order:
1. a pending story talk scene;
2. if the player already talked to that NPC today, the repeat line;
3. otherwise one of the NPC's daily lines, chosen deterministically from the day and the NPC.

Talking:
- sends the quest Talk signal;
- **never changes affinity, gold or items** (user rule, 2 Nov). `NarrativeContentTests` fails the build if talk content carries those effects.

### 3. Cast and portraits

The cast and its portraits are listed in `NarrativeCast` (and `NarrativeCastContent` for Bờ Rìa). That list is also the art request.

- **Khoa:** neutral plus three reflection portraits only. His face never changes in Season 1 (story bible 2.4).
- **An:** the standard set plus three hidden expressions (too_bright, stare, silent_tilt). They are used at most once per chapter, at a mask slip. This follows the user's direction: innocent and magnetic outside, a hidden slight madness.

### 4. Canon

The story canon is the 1 Nov 2026 revision of the story bible, which includes Phần 1B and 5B (An's route). It lives at `D:\Pig-tycoon\.collab\claude-2d\audit_20261101\doc_cot_truyen_pig_tycoon.txt`.

A copy is in the repo at `Docs/story/COT_TRUYEN_2026-11-01.txt`. The older "Bé Na" revision, `Docs/COT_TRUYEN_PIG_TYCOON.md`, now opens with a banner pointing to that copy.

### 5. Saving

`GameSaveData.Narrative` stores flags, read lines (for skip), talk-today marks and reading settings. Scenes are never saved mid-play; a scene in progress replays from its start.

## Consequences

- **Teach by doing:** a scene asks for one action and ends with a `tutorial:<verb>` hint; a Signal-triggered scene reacts when the player does it. No text-wall tutorials.
- **Presentation contract** (Argon): `Docs/plans/2026-11-02-narrative-ui-contract.md`.
- **Legacy tech debt, not removed yet:** `StoryJournalSystem` (StoryAndDeepSystems.cs) and nine `TriggerActionManager` rules still complete legacy quest records outside StoryQuestBook's order. They also carry invented lore that contradicts the bible. Retire them in a follow-up with tests.
