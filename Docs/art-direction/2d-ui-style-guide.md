# Pig Tycoon 2D — UI style guide (pixel, mythic decline)

Source of authority: the user's 30 Oct 2026 direction. The UI must *be* the style — epic myth seen from
the human side, at the climax of a myth's decline, warm sun-gold and very old — carved into frames,
icons and motion. Writing "mythic" on a label does not count. Explanatory text is not allowed on the HUD;
a player must understand a control from its shape, icon, fill and feedback. Story text, dialogue and the
An/Khoa notebooks stay as text because reading them is their purpose.

## Material language

| Layer | Look | Use |
|---|---|---|
| Sun-bronze | worn gold leaf over bronze: bright rim `#f4d27a`, gold `#e2b44c`, bronze `#b07a2c`, deep `#6e4518` | frames, rims, needles, sockets |
| Umber lacquer | old lacquered wood/stone, almost black-brown `#2a1a12`, `#3d2618`, `#55361f` | panel fill behind icons |
| Weathered stone | limestone `#ddd2b6` `#c2b597` `#a3967b` `#82755f`, cracks `#5f5446` | joystick ring, tablets, Bạch Vân controls |
| Lapis/jade accents | lapis `#2b5d86` `#5b9cc6`, jade `#3f8a6a`, cinnabar seal `#a8382a` | state accents only (water, ready, danger) |
| Parchment | `#efe0bb` `#e2cc98` `#c9ad74`, ink `#3a2414` | notebooks, dialogue |

Light always comes from the upper left. Every shape gets a 1 px outline in the darkest neighbouring ink
(never pure black), a 1 px highlight on its top-left edge and a 1 px shadow on its bottom-right edge.
No gradients wider than 2 px; use 2–4 flat shades plus ordered dithering only on large fills.

## Motifs (East–West blend, from the story bible)

- Kim Ô sun-disc (three-legged golden crow inside a sun) and a Western radiant sun (Sol Aureus) — the
  same disc, rays alternating straight (West) and flame-curled (East). Main HUD ornament.
- Sứ blossom with six open flowers and one bud (6+1). Never print the numeral seven; use 6+1 motifs.
- Wedge marks (cuneiform-like nicks) as tick marks on dials and bar ends.
- 24-notch ring for the day dial.
- Cracks, chipped corners, missing gold leaf on 1–3 px of every frame: the myth is fading.

## Scale

Pixel art is authored at 1× and displayed at an integer scale (3× on a 1080p phone). HUD icons are
16×16 (small, inside bars/badges) and 24×24 (primary). Action medallions 32×32. Frames are 9-slice with
8 px corners. Everything snaps to whole pixels; no rotation or non-integer scaling of pixel sprites.

## HUD layout (landscape phone, safe area respected)

- Top-left: sun dial (day arc + weather + day pips + raid countdown skull pips).
- Under it: HP vessel, two-layer Stamina bar (bright = recoverable, dark = daily load), Rage flame shown only during raids.
- Top-right: coin + amount (numbers allowed), herd medallion (pig count over capacity as a ring gauge with 5 colour bands), morale face 4 steps.
- Bottom-left: stone joystick ring with sun-ray compass ticks; knob is a gold sun disc.
- Bottom-right: one large context medallion (its icon changes with the target: open hand, feed trough, hammer, door, coin) plus three martial-skill medallions in an arc and the whistle horn.
- Space bag: a satchel medallion with 3 (basic) or up to 8 (premium) round sockets; each socket shows the actual item sprite, empty sockets are dark. No grid, no hotbar.
- Status effects: a row of 16 px icons under the stamina bar; stacks are dots, not numbers.

## States

Every interactive element has: normal, pressed (shift 1 px down, darker rim), disabled (desaturated, cracked
gold), cooldown (clockwise dark wedge), ready-pulse (rim brightens 2 frames). Danger uses cinnabar red rims;
ready uses jade.
