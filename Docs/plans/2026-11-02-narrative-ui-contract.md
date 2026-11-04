# Narrative UI contract (Claude → Argon), 2 Nov 2026

Logic: `Assets/Scripts/Core/NarrativeRuntime.cs`, `GameEngineNarrative.cs`, `NarrativeCast*.cs`, `NarrativeContent*.cs`. See ADR-0027.

The view only renders what the runner exposes and reports input back. All state lives in Core.

## 1. Entry points

| When | Call |
|---|---|
| New game, after character select (`NewGameSetup.ChooseProtagonist`) | `engine.FireNarrative(NarrativeTrigger.NewGame)` |
| Player enters a map | `engine.FireNarrative(NarrativeTrigger.EnterLocation, "Farm" / "Hub_Village" / "MarketTown" / "Forest_Primeval")` |
| Player talks to an NPC (E / tap; replaces the 3-second bubble in `PixelMythNpc`) | `engine.TalkTo(castId)` |
| Every frame | `runner.Tick(Time.unscaledDeltaTime)` with `runner = engine.Narrative.Runner`, `runner.Headless = false` |

Day start, quest signals and quest completion fire inside Core; nothing to call for those.

- While `runner.LocksInput` is true, gameplay input is locked: joystick, skill buttons and the HUD, except the dialogue box.
- Saving is allowed only while it is false.

## 2. Character select (`NarrativeContentStart.Card(PlayerId)`)

`ProtagonistCard` fields: Name, Title, OpeningLine, Tagline, Blurb, TraitName, TraitText, Catchphrase, Portrait.

- Show both cards side by side as 2.5D standing art.
- Let the player switch freely and confirm with a button. Market practice: a pick that can be changed before confirming; differences shown as real gameplay, not cosmetic.

## 3. Dialogue box (`runner.Phase == RunnerPhase.Line`)

`runner.Line` (LineView) fields:
- SpeakerId, SpeakerName, NameplateColor (hex);
- Portrait id (falls back to neutral);
- Side (Left/Right);
- IsNarration;
- FullText, VisibleChars / VisibleText, Complete;
- ReadBefore.

Speaker lines:
- Portrait 2.5D, roughly 25–30% of screen height, on the given Side.
- Name plate tinted NameplateColor.
- Box at most 3 lines.
- Blinking ► when Complete.
- Text blip: `NarrativeCast.Get(id).BlipPitch` while characters appear.

Narration (`IsNarration`): no portrait, no name. Use a darker strip at the bottom edge (the narrator's inner voice).

Input:
- Tap anywhere: `runner.Tap()`.
- Long-press anywhere: `runner.FastForward = true/false`. On PC: Ctrl.
- Buttons in the box corner: **Tự động** (`runner.Auto`), **Bỏ qua** (`runner.Skip()`; confirm first when the scene has a Recap), **Nhật ký** (opens the backlog). Hide-UI toggle is optional.

## 4. Choices (`Phase == Choice`)

- Prompt: `runner.ChoicePrompt`.
- Options: `runner.Choices`, each with Index, Text, Enabled, LockedReason.
- Locked options are drawn greyed out with the reason.
- Tap an option: `runner.Choose(index)`.
- Stack options vertically, centred, large tap targets (≥ 48 dp).

## 5. Banner / Pause

- `Phase == Banner`: full-screen chapter or date card showing `runner.BannerText`. A tap skips it.
- `Phase == Pause`: show nothing.

## 6. Stage (`Phase == Stage` or non-blocking events)

Each frame, call `runner.TakeStageEvents()` and perform the events in order. When a blocking event (`ev.Blocking`) is finished, call `runner.StageDone(ev.Seq)`.

| `ev.Command.Kind` | What to do |
|---|---|
| ActorPlace / ActorMove | Actor = "player" or a cast id. Target = mark in `Label` ("village:gate", "village:npc:man", "village:station:quan_ba_sau", "farm:station:main_house" …, resolved from the map layout json), or X/Y tiles when Label is empty. Walk with the real walk animation and pathing, not a slide. |
| ActorFace | Face the given direction. |
| Emote | Bubble icon over the actor: "!", "?", "...", "♪", "♥". |
| Camera | Pan to the mark or X/Y over `Ms`. |
| Fade | "out" / "in" over `Ms`. |
| Shake | Screen shake for `Ms`. |
| Sfx / Music | `Label` = id; for Music, "" means silence (story rule: mystery beats have no music). |
| Backdrop | Full-screen illustration `Label`, fade `Ms`; "" returns to the live world. `runner.BackdropId` holds the current one. Backdrop ids are listed in each content file's `Backdrops` array. |

## 7. Custom effects (`engine.NarrativeCustom` event)

- `goto_map:<map>`: change map after the scene.
- `reflection:<portrait>`: show the protagonist's reflection (water trough, window, mirror) with that expression. Khoa's emotions appear only here.
- `notebook:<id>`: open the notebook page. Khoa: two columns "Thấy / Có thật" plus a pencil margin. An: pink cover, letters with ★/☾ stickers and ":)".
- `tutorial:<verb>`: one control hint with an arrow pointing at the real button. Verbs: move, interact, feed, water, whistle, observe, recall, dodge, counter, sleep, market. It disappears when the player does the action.

## 8. Backlog

`runner.Backlog` holds up to 200 BacklogEntry items (Speaker, Text, IsChoice, IsRecap). Show it as a scroll list with the newest at the bottom; choices shown as "► …" and the recap as a card.

## 9. Settings (`engine.NarrativeState.Settings`)

| Setting | Values | Default |
|---|---|---|
| TextCps | 0 / 30 / 45 / 70 | 45 |
| AutoDelayBase | — | — |
| SkipUnread | on / off | off |
| TextScale | 0.9 / 1 / 1.15 / 1.3 | — |

These are saved with the game.

## 10. Art request

Portraits:
- Every (character, portrait) pair: `NarrativeCast.PortraitManifest()`.
- An's hidden three: too_bright, stare, silent_tilt.
- Khoa: neutral plus reflect_frown, reflect_smile, reflect_cry (shown in reflections only).

Other art:
- Backdrops: the `Backdrops` arrays in `NarrativeContentPrologueKhoa` and `NarrativeContentPrologueAn`.
- Emote bubbles, nameplate frame, dialogue box, choice buttons, chapter card frame, notebook pages (two styles).

All art is subject to the user's visual approval before it is merged.
