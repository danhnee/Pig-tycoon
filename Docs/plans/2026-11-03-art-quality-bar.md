# Pig Tycoon art, animation and UI quality bar (Claude → Argon), 3 Nov 2026

**User direction (3 Nov):**
- Judge harshly, against the best-looking pixel games.
- Keep pushing quality up.
- Many more animations, at higher quality: every weapon gets its own moves, and every skill gets its own animation that varies slightly with the weapon.
- Better UI/UX.
- A wider view: the old one was too close and looked rough.

**Benchmarks:** Eastward, Sea of Stars, Hyper Light Drifter, CrossCode, Dead Cells (same 3D → pixel pipeline as ours), Children of Morta, Chained Echoes, Moonlighter, Stardew Valley (framing, readability, farm loop).

**Gate:** a delivery passes only when the automatic check below passes **and** a frame review at 1920×1080 shows no item from the reject list. Your own "100% done" report does not count as passing.

## 1. Automatic check (must pass before you send)

```
python tools/pixelart/qa_sheets.py <your folder> --md report.md
```

| Check | Rule |
|---|---|
| F alpha | Hard pixel edges. Semi-transparent pixels ≤ 0.5% (the ground shadow is exempt). Downscaled renders with anti-aliasing fail. |
| F holes | No see-through specks inside the body (torn legs, holes in the armpit). |
| F fringe | No magenta key colour left on the sprite. |
| F legs | In walk / run / move, the legs really change frame to frame. |
| F side-view | Left / right rows are real profiles: narrower shoulders, head turned. Not the front pose sliding sideways. |
| W colours | A tight palette, about 48 colours or fewer per sheet. Today's sheets have 2,900–6,300 colours, which means blurred renders, not pixel art. |

**3 Nov sample:** 22 of 32 sheets FAIL. Report: `Docs/qa/2026-11-03-sheet-qa-sample.md`. Argon's new An/Khoa walk sheets have holes in almost every frame.

## 2. Animation bar

The full clip list is `Docs/plans/2026-11-03-animation-manifest.md` (generated from Core `AnimationClips`):
- per protagonist: locomotion, farm work, 3-hit strings for 4 weapon classes, reactions, idle fidgets;
- 22 active skills × (1 generic + 4 weapon-class variants);
- weapon overlay layers that follow the body.

| Topic | Rule |
|---|---|
| Frame counts | Minimums in the manifest. Walk/run 8 frames; attacks 8–10; idle 8. Four frames is never acceptable for a main character. |
| Principles | Anticipation → action (one smear frame on fast swings) → follow-through → recovery. Hair and cloth trail 1–2 frames behind (secondary motion). Volume stays constant; subtle squash/stretch on landing and impact. |
| Weapon character | Machete: quick flat arcs. Hook spear: thrust and pull back. Sledgehammer: big wind-up, heavy follow-through, body twist. Hand crossbow: raise, aim, recoil. The same skill keeps its rhythm but changes grip and swing path with the weapon. |
| Proportions & feet (revised 3 Nov after the user's "stilt legs" rejection) | Human figure ≈ 56 px tall (1.75 m) inside the 72×84 cell, legs ≈ 45% of height; never stretch the body or legs to cover ground. Walk: feet apart at most ~0.4 × height at contact, feet together on the passing frame, knees bend. Run: at most ~0.65 × height, a flight frame. Draw the natural step and write the real `stride_m_per_cycle` in the json; the runtime raises the frame rate to match the 4.5 / 7 m/s game speed. My earlier "1.8 m / 3.1 m stride" rule was wrong and caused the long splayed legs — ignore it. |
| Facings | 4 real views: front, back and 2 profiles. Asymmetric details (weapon hand, hair parting) stay on the correct side when mirrored. |
| VFX (separate from the body) | Hit flash, small sparks/dust 4–6 frames, impact ring. The slash VFX follows the blade in the overlay; never two blades at once. |

## 3. Look

| Topic | Rule |
|---|---|
| Readability | At 1080p with a ×2 pixel scale, the character always stands out from grass and dirt. Use a 1 px darker outline (selective outlining) or rim light, plus a clear contact shadow. Today the character sinks into the ground. |
| Light | One direction (top-left) for the world, characters and buildings. Shadows shift hue (blue-purple), not grey-black. Day is near-neutral, not a heavy yellow cast; night is blue, not dark moss. |
| World | Dense detail clusters: tall grass, rocks, flowers, path edges, puddles, fallen leaves, wind sway. Avoid AE-0001/AE-0002 (sparse, flat, confetti). Nothing repeats more than 3 times on one screen. |
| Consistency | Everything in-world is 32 PPU, never rotated or scaled (no mixels). Portraits match the in-game sprite's style (Mận is pixel 2.5D while An is flat anime: that fails). |
| Style | Mythic East–West, modern rural characters, light weapons. No gacha armour, wings, crowns or cosmic cloth. No sets (paper-doll per slot, `2026-11-03-paperdoll-gear-art-spec.md`). |

## 4. UI/UX bar

| Topic | Rule |
|---|---|
| Text | Body text ≥ 14 px and labels ≥ 12 px at 720p; scale with screen height. Contrast ≥ 4.5:1 (dark plate under text on the map). |
| Touch | Tap targets ≥ 48 dp. The main thumb cluster stays inside the safe area. Never more than 9 buttons in one cluster; group by use. |
| Dialogue | VN style: hide the gameplay HUD during scenes; hide empty portrait frames; narration as a dark bottom strip. |
| Consistency | One frame / 9-slice family, one icon style matching the world art, one font family. No text overflow, no rectangles where icons are missing. |
| Feedback | Every action gets an immediate response: sound, small motion, number popup. Locked things say why. |

## 5. Camera (done, logic side)

The pixel-perfect framing picks the largest whole-pixel scale that shows ≥ 7 m above and below the player:
- 1080p: ×2, 8.44 m (about Stardew's 30×17 tiles);
- 1440p: ×3;
- 4K: ×4.

Pixels are always whole; no fractional stretching. Reviews are now recorded at 1920×1080.
