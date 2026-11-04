# 2D pixel rebuild — Claude takeover plan (30 Oct 2026, 3-day window)

Root: Claude Opus 5.5. Hands: Sonnet/Haiku subagents (medium). Codex (ChatGPT): technical consultant
only — research, measurement, simulation, review — through collab room `pig-tycoon/2d-pixel-rebuild`;
no code edits. Codex conversations rotate after two compactions.

Inputs read: the three user DOCX files (digests in `../audit/claude/`), Codex thread 01a188b5,
ADR-0011, the user's six reference images (Seraphina sheets, harbor town, gold meadow diorama ×2).

## Decisions taken at takeover

1. **No Stardew pixels ship.** Codex had placed Farm.tmx, the barn, silo, well and Pelican Town into
   scenes; those are ConcernedApe's copyrighted assets. Moved to `../quarantine-stardew-DO-NOT-SHIP/`.
   F:\StardewValley is a measured template only (`../audit/claude/stardew-template-spec.md`).
2. **Art comes from the project's own 3D assets, rendered to pixel art** (`tools/pixelart/`):
   Blender headless, Cycles CPU (Eevee peaked > 2 GB), orthographic, 30° elevation, 32 px/m with 4×
   integer supersampling, shared k-means palette per family, warm umber selective outline, foot pivot
   from the projected model root. Peak ~950 MB per job.
3. **New art only where no asset exists**: terrain tiles, decorations and UI are drawn procedurally
   in code (`gen_terrain_tiles.py`, `gen_decor.py`, `ui/gen_hud_kit.py`, `ui/gen_icons.py`) following
   `Docs/art-direction/2d-ui-style-guide.md`.
4. Scale: 1 tile = 1 m = 32 px (ADR-0005). Pixel Perfect Camera reference 640×360 (20 × 11.25 m view).

## Asset sources

| Need | Source |
|---|---|
| Khoa (idle, walk, run, attack, interact, whistle) | `PlayableArt/Characters/Khoa@*.fbx` |
| Mận NPC (idle, walk, interact) | `Man@*.fbx` |
| Pigs (idle, walk, eat, sleep) | `Pig@*.fbx`; breeds by retexture (`Pig_HongDien`, `Pig_WhiteFoot`) |
| Enemies Crawler, Thief (+ bind, drag) | `PlayableArt/Enemies/*.fbx` |
| Farm service buildings | `.collab/artifacts/functional-buildings-codex/functional-buildings-local-origin.blend`, observatory `ArtSource/Blender_3D_Map/Zone05_ReferenceDetail/` |
| Fences, feeder, water basin, mud wallow, care post, capacity board, shelters | `PlayableArt/Environment/*.fbx` |
| Town houses (old-European, blue roofs, gold trim) | Argon harbor blend (351 MB — render when RAM allows) |
| An | **missing** — no 3D model; branch-B sheet is a placeholder. Needs the user's decision. |

## Order of work

Day 1 (30–31 Oct): pipeline proven; all character sheets; terrain, decor, HUD kit and icons; farm
service buildings rendered; Unity import (sprite slicing with pivots, animator clips); Farm scene
rebuilt by an editor builder with the new art; HUD replaced (icon-first, bag sockets 3/8).
Day 2: Market Town, NPC Village, Primeval Forest (event-gated) scenes; interiors/stations wired to
the existing Core commands (storage↔bag, build/refill/repair, enhancement, martial skills, trade,
Bí Nhân); pigs and enemies with their animations; day/night and weather visuals.
Day 3: Android build through the Flutter host, device/emulator verification (Unity 6000.6 does not
officially support emulators — see `../audit/codex/R1-report.md`), polish passes from screenshots,
user visual review.

## Acceptance (evidence, not claims)

Core runner stays 81/81+. Each map: overview, play-angle and close-up screenshots from a real build,
compared to the user's targets; no Stardew pixels in Assets (scripted check); no explanatory text on
the HUD (review list); every station reachable and calling its Core command; APK launched on a real
ARM64 device or an emulator with recorded ABI evidence. The user approves visuals before promotion.
