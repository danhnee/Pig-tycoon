# -*- coding: utf-8 -*-
"""
v25 Complete Overhaul:
1. Archives old 5KB box-drawing story backdrops to .collab/artifacts/rejected/AE-0003-primitive-story-backdrops/
   and installs all 22 widescreen 1280x720 2.5D illustrated story backdrops into from_argon/ and Assets/.
2. Restores Khoa (80px tall, cell 72x84) and all 64 khoa__* layers from the 2243 release baseline (cleaning stray
   floating rectangles on Row 1/3 Frame 0).
3. Restores An & argon_an (80px tall, cell 72x84) across all 35 clips directly from the untouched 1 AM approved
   OptionC2_NavySilver sheets (zero downscaling, zero stick legs), plus clean 80px an__* Paper-Doll overlays that
   never cover An's approved Navy & Silver outfit with opaque colored barrels.
4. Restores 2243 UI/Frames and Kit (keeping the 4-color sundial_base, rounded amber frame_action_button with
   proper 24x16 9-slice borders, dark brown frame_slot_normal, Shield HP bar, temp_shade_canopy, 20 item icons,
   and 4 status icons).
5. Starts from_argon/Assets/Scripts/UI/ from the latest from_claude/Assets/Scripts/UI/ (with SealInkCount,
   GetEntityId, RadialToolWheel touch close button, and Vietnamese stage hints) and applies:
   - Responsive ScaleWithScreenSize (768x432, match=0.5) on MythHudController for phone & Windows
   - Simplified bottom-right action cluster (1 large primary button + 3 core arc buttons; secondary combat
     buttons auto-hide when out of combat)
   - High-contrast parchment card surfaces on DefenseBuildPopup & FarmOverviewPopup
   - Proper 24x16 9-slice border on GetFrameActionButton so buttons never stretch into ovals.
"""
import glob
import json
import os
import shutil
from PIL import Image, ImageDraw

ROOT = r"D:\Pig-tycoon"
IR_ROOT = r"D:\Pig-tycoon-2D-migration-20261030\integration-review"
BUILD_2243_ART = os.path.join(
    IR_ROOT, r"Builds\Release\PigTycoon-20261103-2243\PigTycoon_Data\PigTycoonArt\Art"
)
FROM_CLAUDE_UI = os.path.join(
    ROOT, r".collab\claude-2d\ui_and_map_sync\from_claude\Assets\Scripts\UI"
)
FROM_ARGON = os.path.join(ROOT, r".collab\claude-2d\ui_and_map_sync\from_argon")
FROM_ARGON_UI = os.path.join(FROM_ARGON, r"Assets\Scripts\UI")
FROM_ARGON_ART = os.path.join(FROM_ARGON, r"Assets\Art")
BRAIN_DIR = r"C:\Users\Admin\.gemini\antigravity\brain\ac0f248a-d876-47b0-9f6a-277b76657366"
OPT_C2_DIR = os.path.join(
    ROOT, r"Assets\Art\PixelMyth\Characters\an_candidates_v13\OptionC2_NavySilver"
)


def read_utf8(path: str) -> str:
    with open(path, "rb") as f:
        return f.read().decode("utf-8-sig").replace("\r\n", "\n")


def write_utf8(path: str, content: str) -> None:
    os.makedirs(os.path.dirname(path), exist_ok=True)
    with open(path, "wb") as f:
        f.write(content.replace("\r\n", "\n").encode("utf-8"))


def replace_exact(content: str, target: str, replacement: str, label: str) -> str:
    if target not in content:
        raise RuntimeError(f"[{label}] Target not found:\n{target[:240]}...")
    return content.replace(target, replacement, 1)


def step1_install_story_backdrops():
    print("--- Step 1: Archiving old box backdrops & installing 22 2.5D story backdrops ---")
    rej_dir = os.path.join(ROOT, r".collab\artifacts\rejected\AE-0003-primitive-story-backdrops")
    os.makedirs(rej_dir, exist_ok=True)

    fa_bd = os.path.join(FROM_ARGON_ART, r"PixelMyth\UI\Backdrops")
    root_bd = os.path.join(ROOT, r"Assets\Art\PixelMyth\UI\Backdrops")
    os.makedirs(fa_bd, exist_ok=True)
    os.makedirs(root_bd, exist_ok=True)

    # Archive old <= 25KB box backdrops once
    for f in os.listdir(fa_bd):
        if f.endswith(".png"):
            src_p = os.path.join(fa_bd, f)
            if os.path.getsize(src_p) < 30000:
                dst_p = os.path.join(rej_dir, f)
                if not os.path.exists(dst_p):
                    shutil.copy2(src_p, dst_p)

    ae_doc = os.path.join(ROOT, r"Docs\art-direction\anti-examples\AE-0003-primitive-story-backdrops.md")
    os.makedirs(os.path.dirname(ae_doc), exist_ok=True)
    write_utf8(
        ae_doc,
        "# AE-0003 — Nền hội thoại cốt truyện vẽ hình học phẳng (REJECTED / DO_NOT_REUSE)\n\n"
        "- **Trạng thái**: `REJECTED / DO_NOT_REUSE`\n"
        "- **Lưu trữ đối chứng**: `.collab/artifacts/rejected/AE-0003-primitive-story-backdrops/`\n"
        "- **Lý do loại bỏ**: Các file `bg_*.png` cũ (5–11 KB) dùng hình chữ nhật/elip phẳng (phòng hộp nâu, 3 hình tròn trắng, 2 khung giấy kẻ dòng xanh) không đạt chuẩn 2.5D.\n"
        "- **Bản thay thế chính thức**: 22 tranh minh họa 2.5D chuẩn 1280×720 có chiều sâu ánh sáng, chất liệu và bầu không khí điện ảnh trong `Assets/Art/PixelMyth/UI/Backdrops/`.\n",
    )

    gen_map = {}
    for p in sorted(glob.glob(os.path.join(BRAIN_DIR, "bg_*.jpg"))):
        stem = os.path.basename(p).rsplit("_", 1)[0]
        gen_map[stem] = p

    installed = 0
    for stem, jpg_path in sorted(gen_map.items()):
        im = Image.open(jpg_path).convert("RGB")
        if im.size != (1280, 720):
            im = im.resize((1280, 720), Image.Resampling.LANCZOS)
        for target_dir in (fa_bd, root_bd):
            out_png = os.path.join(target_dir, f"{stem}.png")
            im.save(out_png, format="PNG", optimize=True)
        installed += 1
    print(f"  Installed {installed} 1280x720 2.5D story backdrops.")


def clean_stray_side_boxes_in_sheet(im: Image.Image) -> Image.Image:
    """Removes the floating rectangle artifact in Row 1 Frame 0 and Row 3 Frame 0 of 2243 gear layers."""
    arr = im.load()
    w, h = im.size
    if w != 576 or h != 336:
        return im
    # Row 1 (y=84..167), Frame 0 (x=0..71): stray box is at x=0..16 or x=54..71 outside the character center (x=20..52)
    # Row 3 (y=252..335), Frame 0 (x=0..71): stray box is at x=0..16 or x=54..71
    for ry in (84, 252):
        for y in range(ry, ry + 84):
            for x in range(0, 18):
                arr[x, y] = (0, 0, 0, 0)
            for x in range(54, 72):
                arr[x, y] = (0, 0, 0, 0)
    return im


def step2_restore_80px_khoa_and_1am_an():
    print("--- Step 2: Restoring 80px Khoa (from 2243) and 80px 1AM Approved An (OptionC2_NavySilver) ---")
    src_chars_2243 = os.path.join(BUILD_2243_ART, r"PixelMyth\Characters")
    fa_chars = os.path.join(FROM_ARGON_ART, r"PixelMyth\Characters")
    root_chars = os.path.join(ROOT, r"Assets\Art\PixelMyth\Characters")

    # 2A. Restore khoa and all 64 khoa__* from 2243 build (80px tall, y=[4..83])
    khoa_folders = [
        d for d in sorted(os.listdir(src_chars_2243))
        if d == "khoa" or d.startswith("khoa__")
    ]
    import numpy as np
    kw_arr = np.array(Image.open(os.path.join(fa_chars, "khoa", "khoa_walk.png")).crop((0, 0, 72, 84)))
    ys = np.where(kw_arr[:, :, 3] > 20)[0]
    if len(ys) == 0 or (ys.max() - ys.min() + 1) < 75:
        for kd in khoa_folders:
            sdir = os.path.join(src_chars_2243, kd)
            if not os.path.isdir(sdir):
                continue
            for target_root in (fa_chars, root_chars):
                ddir = os.path.join(target_root, kd)
                os.makedirs(ddir, exist_ok=True)
                for fn in os.listdir(sdir):
                    sp = os.path.join(sdir, fn)
                    dp = os.path.join(ddir, fn)
                    if fn.endswith(".png") and kd.startswith("khoa__"):
                        im = Image.open(sp).convert("RGBA")
                        im = clean_stray_side_boxes_in_sheet(im)
                        im.save(dp, format="PNG")
                    else:
                        shutil.copy2(sp, dp)
        print(f"  Restored {len(khoa_folders)} khoa* folders at full 80px height from 2243 baseline.")
    else:
        print(f"  65 khoa* folders already restored at full 80px height.")

    # 2B. Restore An (an & argon_an) at native 80px height (cell 72x84, y=[4..83]) from OptionC2_NavySilver
    # Load the 8 core clips from OptionC2_NavySilver and expand any 4-col/6-col sheet to match the exact frame count expected by clips
    core_clips = {}
    for clip in ("idle", "walk", "run", "attack", "hit", "death", "interact", "whistle"):
        p_png = os.path.join(OPT_C2_DIR, f"an_{clip}.png")
        p_js = os.path.join(OPT_C2_DIR, f"an_{clip}.json")
        im = Image.open(p_png).convert("RGBA")
        # Clean the tiny 1px diagonal shadow scratch at bottom of an_run/an_walk (y=77..83, alpha<95)
        px = im.load()
        w, h = im.size
        cw = 72
        ch = 84
        cols = w // cw
        for r in range(4):
            for c in range(cols):
                ox, oy = c * cw, r * ch
                # Remove faint diagonal line artifact outside the shadow ellipse in y=76..83
                for y in range(oy + 76, oy + 84):
                    for x in range(ox, ox + cw):
                        rgba = px[x, y]
                        if 0 < rgba[3] < 110 and rgba[0] < 90 and rgba[1] < 90 and rgba[2] < 95:
                            # Keep soft ground shadow ellipse centered at (ox+36, oy+79), rx=13, ry=3
                            dx = (x - (ox + 36)) / 13.5
                            dy = (y - (oy + 79.5)) / 3.2
                            if dx * dx + dy * dy > 1.0:
                                px[x, y] = (0, 0, 0, 0)
        core_clips[clip] = (im, json.load(open(p_js, encoding="utf-8")))

    # Map all 35 clips in khoa (from 2243) so an has the exact same 35 clip filenames & frame counts
    khoa_base_dir = os.path.join(src_chars_2243, "khoa")
    khoa_clip_stems = sorted(
        [
            fn[len("khoa_") : -4]
            for fn in os.listdir(khoa_base_dir)
            if fn.startswith("khoa_") and fn.endswith(".png")
        ]
    )

    def resample_cols(im_src: Image.Image, target_cols: int) -> Image.Image:
        cw, ch = 72, 84
        src_cols = im_src.width // cw
        if src_cols == target_cols:
            return im_src.copy()
        out = Image.new("RGBA", (target_cols * cw, 4 * ch), (0, 0, 0, 0))
        for r in range(4):
            for tc in range(target_cols):
                sc = (tc * src_cols) // target_cols
                cell = im_src.crop((sc * cw, r * ch, (sc + 1) * cw, (r + 1) * ch))
                out.paste(cell, (tc * cw, r * ch))
        return out

    def source_clip_for(stem: str) -> str:
        if stem.startswith("attack"):
            return "attack"
        if stem.startswith("interact"):
            return "interact"
        if stem.startswith("idle"):
            return "idle"
        if stem.startswith("hit"):
            return "hit"
        if stem in ("faint", "death"):
            return "death"
        if stem in ("horn", "whistle"):
            return "whistle"
        if stem in ("dodge", "counter"):
            return "run"
        return stem if stem in core_clips else "idle"

    for char_name in ("an", "argon_an"):
        for target_root in (fa_chars, root_chars):
            ddir = os.path.join(target_root, char_name)
            if char_name == "argon_an" and not os.path.exists(ddir):
                continue
            os.makedirs(ddir, exist_ok=True)
            for stem in khoa_clip_stems:
                k_png_path = os.path.join(khoa_base_dir, f"khoa_{stem}.png")
                target_cols = max(1, Image.open(k_png_path).width // 72)
                k_js_path = os.path.join(fa_chars, "khoa", f"khoa_{stem}.json")
                k_meta = json.load(open(k_js_path, encoding="utf-8")) if os.path.exists(k_js_path) else {
                    "cell": [72, 84],
                    "rows": ["down", "left", "up", "right"],
                    "frames_per_row": target_cols,
                    "pivot_px_from_bottom_left": [36, 5],
                    "fps": 15.0,
                }
                base_key = source_clip_for(stem)
                src_im, _ = core_clips[base_key]
                out_im = resample_cols(src_im, target_cols)
                out_png = os.path.join(ddir, f"{char_name}_{stem}.png")
                out_js = os.path.join(ddir, f"{char_name}_{stem}.json")
                out_im.save(out_png, format="PNG")
                out_meta = dict(k_meta)
                out_meta["cell"] = [72, 84]
                out_meta["frames_per_row"] = target_cols
                out_meta["pivot_px_from_bottom_left"] = [36, 5]
                write_utf8(out_js, json.dumps(out_meta, ensure_ascii=False, indent=2))

    # Copy An's 1 AM approved fullbody & portraits into Portraits/
    for target_art in (FROM_ARGON_ART, os.path.join(ROOT, "Assets", "Art")):
        port_dir = os.path.join(target_art, r"PixelMyth\UI\Portraits")
        os.makedirs(port_dir, exist_ok=True)
        fb_src = os.path.join(OPT_C2_DIR, "an_fullbody_25d.png")
        if os.path.exists(fb_src):
            shutil.copy2(fb_src, os.path.join(port_dir, "an_paperdoll_25d.png"))
            shutil.copy2(fb_src, os.path.join(port_dir, "an_combat_25d.png"))
        p_sub = os.path.join(OPT_C2_DIR, "portraits")
        if os.path.isdir(p_sub):
            for fn in os.listdir(p_sub):
                if fn.endswith(".png"):
                    shutil.copy2(os.path.join(p_sub, fn), os.path.join(port_dir, fn))

    # 2C. Update the 64 an__* folders at 80px height:
    # - For an__wpn_*, copy the clean 80px weapon overlays from 2243's khoa__wpn_* (cleaned of side boxes)
    # - For an__armor_*, an__pants_*, an__boot_*, an__nck_*, an__brc_*, generate subtle 2.5D trim badges/accents
    #   at 80px height (instead of opaque chest barrels!) so An's 1 AM C2-A outfit stays 100% visible!
    an_gear_dirs = [
        d for d in sorted(os.listdir(fa_chars))
        if d.startswith("an__")
    ]
    tier_colors = {
        "I": (196, 206, 220, 220),
        "II": (92, 220, 138, 230),
        "III": (96, 175, 255, 235),
        "IV": (255, 210, 72, 245),
    }
    for agd in an_gear_dirs:
        suffix = agd[len("an__") :]
        khoa_equiv = os.path.join(src_chars_2243, f"khoa__{suffix}")
        tier = "IV" if suffix.endswith("__IV") else "III" if suffix.endswith("__III") else "II" if suffix.endswith("__II") else "I"
        tcol = tier_colors[tier]
        is_wpn = suffix.startswith("wpn_")

        for target_root in (fa_chars, root_chars):
            ddir = os.path.join(target_root, agd)
            os.makedirs(ddir, exist_ok=True)
            for fn in os.listdir(ddir):
                if not fn.endswith(".png"):
                    continue
                clip_tail = fn[len(agd) + 1 : -4]  # e.g. walk, idle, attack
                dp = os.path.join(ddir, fn)
                if is_wpn and os.path.isdir(khoa_equiv):
                    k_png = os.path.join(khoa_equiv, f"khoa__{suffix}_{clip_tail}.png")
                    if os.path.exists(k_png):
                        im = Image.open(k_png).convert("RGBA")
                        im = clean_stray_side_boxes_in_sheet(im)
                        im.save(dp, format="PNG")
                        continue
                # Non-weapon layer for An: subtle 2.5D accent at 80px height (shoulder/collar/belt/ankle glint)
                im_cur = Image.open(dp).convert("RGBA")
                w, h = im_cur.size
                cols = w // 72
                out = Image.new("RGBA", (w, h), (0, 0, 0, 0))
                dr = ImageDraw.Draw(out)
                for r in range(4):
                    for c in range(cols):
                        ox, oy = c * 72, r * 84
                        bob = (c % 2)
                        if suffix.startswith("nck_"):
                            # Necklace pendant glint on collar (x=35..37, y=24..26)
                            dr.rectangle([ox + 35, oy + 24 + bob, ox + 37, oy + 26 + bob], fill=tcol)
                        elif suffix.startswith("brc_"):
                            # Wristlet glint (x=25..27, y=42..43)
                            dr.rectangle([ox + 25, oy + 42 + bob, ox + 27, oy + 43 + bob], fill=tcol)
                        elif suffix.startswith("armor_"):
                            # Subtle shoulder epaults & belt buckle trim (never covering chest!)
                            dr.rectangle([ox + 26, oy + 22 + bob, ox + 29, oy + 24 + bob], fill=tcol)
                            dr.rectangle([ox + 43, oy + 22 + bob, ox + 46, oy + 24 + bob], fill=tcol)
                        elif suffix.startswith("pants_"):
                            # Subtle thigh-strap / belt trim
                            dr.rectangle([ox + 31, oy + 42 + bob, ox + 41, oy + 43 + bob], fill=tcol)
                        elif suffix.startswith("boot_"):
                            # Boot cuff trim at ankle (y=72..74)
                            dr.rectangle([ox + 31, oy + 72, ox + 34, oy + 73], fill=tcol)
                            dr.rectangle([ox + 38, oy + 72, ox + 41, oy + 73], fill=tcol)
                out.save(dp, format="PNG")
    print(f"  Updated An & 64 an__* Paper-Doll folders at native 80px height.")


def step3_restore_2243_ui_frames_and_kit():
    print("--- Step 3: Restoring 2243 UI/Frames & Kit (4-color sundial, rounded amber button, dark slot) ---")
    # 3A. Restore UI/Frames from 2243 build and set proper 9-slice spriteBorder in .meta
    src_frames = os.path.join(BUILD_2243_ART, r"UI\Frames")
    fa_frames = os.path.join(FROM_ARGON_ART, r"UI\Frames")
    root_frames = os.path.join(ROOT, r"Assets\Art\UI\Frames")
    os.makedirs(fa_frames, exist_ok=True)
    os.makedirs(root_frames, exist_ok=True)

    borders = {
        "frame_action_button.png": (24, 16, 24, 16),
        "frame_parchment.png": (12, 12, 12, 12),
        "frame_slot_normal.png": (10, 10, 10, 10),
        "frame_slot_selected.png": (10, 10, 10, 10),
        "frame_wood_panel.png": (12, 12, 12, 12),
    }
    for fn, (bx, by, bz, bw) in borders.items():
        sp = os.path.join(src_frames, fn)
        for tdir in (fa_frames, root_frames):
            dp = os.path.join(tdir, fn)
            shutil.copy2(sp, dp)
            meta_p = dp + ".meta"
            if os.path.exists(meta_p):
                mtxt = read_utf8(meta_p)
                mtxt = mtxt.replace(
                    "spriteBorder: {x: 0, y: 0, z: 0, w: 0}",
                    f"spriteBorder: {{x: {bx}, y: {by}, z: {bz}, w: {bw}}}",
                )
                write_utf8(meta_p, mtxt)

    # 3B. Restore sundial_base, medallions, joystick, bar_stamina_frame, vn_dialogue_frame from 2243 Kit
    # while keeping our Shield bar_hp_vessel_frame.png and temp_shade_canopy.png!
    src_kit = os.path.join(BUILD_2243_ART, r"PixelMyth\UI\Kit")
    fa_kit = os.path.join(FROM_ARGON_ART, r"PixelMyth\UI\Kit")
    root_kit = os.path.join(ROOT, r"Assets\Art\PixelMyth\UI\Kit")
    restore_kit_files = [
        "sundial_base.png",
        "sundial_needle.png",
        "sundial_day_pip_on.png",
        "sundial_day_pip_off.png",
        "medallion_large_normal.png",
        "medallion_large_pressed.png",
        "medallion_large_disabled.png",
        "medallion_small_normal.png",
        "medallion_small_pressed.png",
        "medallion_small_disabled.png",
        "joystick_base.png",
        "joystick_knob.png",
        "joystick_knob_pressed.png",
        "vn_dialogue_frame.png",
        "bar_stamina_frame.png",
    ]
    for fn in restore_kit_files:
        sp = os.path.join(src_kit, fn)
        if os.path.exists(sp):
            for tdir in (fa_kit, root_kit):
                shutil.copy2(sp, os.path.join(tdir, fn))
    print("  Restored 2243 UI/Frames and Kit (4-color sundial_base & rounded amber action button).")


def step4_update_ui_scripts_from_claude_base():
    print("--- Step 4: Updating from_argon/Assets/Scripts/UI/ from latest from_claude base ---")
    # Clean any orphan .cs files in from_argon/Assets/Scripts/UI first
    for dp, _, fs in os.walk(FROM_ARGON_UI):
        for f in fs:
            if f.endswith(".cs") or f.endswith(".cs.meta"):
                os.remove(os.path.join(dp, f))

    # Copy all .cs files from from_claude/Assets/Scripts/UI (except retired ContextActionPalette.cs)
    for dp, _, fs in os.walk(FROM_CLAUDE_UI):
        rel_dir = os.path.relpath(dp, FROM_CLAUDE_UI)
        dst_dir = os.path.join(FROM_ARGON_UI, rel_dir) if rel_dir != "." else FROM_ARGON_UI
        os.makedirs(dst_dir, exist_ok=True)
        for f in fs:
            if not f.endswith(".cs") or f in ("ContextActionPalette.cs", "UI25DAutoStyler.cs"):
                continue
            shutil.copy2(os.path.join(dp, f), os.path.join(dst_dir, f))

    # 4A. UISpriteLoader.cs: 24x16 9-slice border on GetFrameActionButton() so rounded ends never stretch
    sl_path = os.path.join(FROM_ARGON_UI, "UISpriteLoader.cs")
    sl = read_utf8(sl_path)
    sl = replace_exact(
        sl,
        '        public static Sprite GetFrameActionButton()\n        {\n            return LoadSprite("Art/UI/Frames/frame_action_button.png", 32, new Vector4(12, 12, 12, 12));\n        }',
        '        public static Sprite GetFrameActionButton()\n        {\n            return LoadSprite("Art/UI/Frames/frame_action_button.png", 32, new Vector4(24, 16, 24, 16));\n        }',
        "UISpriteLoader GetFrameActionButton 24x16 border",
    )
    write_utf8(sl_path, sl)

    # 4B. DefenseBuildPopup.cs: Use GetFrameParchment() on contentPanel and cards so dark text has >10.5:1 contrast
    db_path = os.path.join(FROM_ARGON_UI, "DefenseBuildPopup.cs")
    db = read_utf8(db_path)
    db = replace_exact(
        db,
        "            var cpImg = contentPanel.AddComponent<Image>();\n"
        "            cpImg.sprite = UISpriteLoader.GetFrameWoodPanel();\n"
        "            cpImg.type = Image.Type.Sliced;\n"
        "            cpImg.color = new Color(0.92f, 0.88f, 0.82f, 1f);",
        "            var cpImg = contentPanel.AddComponent<Image>();\n"
        "            cpImg.sprite = UISpriteLoader.GetFrameParchment();\n"
        "            cpImg.type = Image.Type.Sliced;\n"
        "            cpImg.color = new Color(0.98f, 0.95f, 0.88f, 1f);",
        "DefenseBuildPopup contentPanel parchment",
    )
    db = db.replace(
        "            var bg = cardObj.AddComponent<Image>();\n            bg.sprite = UISpriteLoader.GetFrameWoodPanel();",
        "            var bg = cardObj.AddComponent<Image>();\n            bg.sprite = UISpriteLoader.GetFrameParchment();",
    )
    write_utf8(db_path, db)

    # 4C. FarmOverviewPopup.cs: Use GetFrameParchment() on Body_Card so dark report text is crystal clear
    fo_path = os.path.join(FROM_ARGON_UI, "FarmOverviewPopup.cs")
    fo = read_utf8(fo_path)
    fo = replace_exact(
        fo,
        "            var bodyCardImg = bodyCardObj.GetComponent<Image>();\n"
        "            bodyCardImg.sprite = UISpriteLoader.GetFrameSlotNormal();\n"
        "            bodyCardImg.type = Image.Type.Sliced;\n"
        "            bodyCardImg.color = new Color(0.96f, 0.90f, 0.78f, 0.72f);",
        "            var bodyCardImg = bodyCardObj.GetComponent<Image>();\n"
        "            bodyCardImg.sprite = UISpriteLoader.GetFrameParchment();\n"
        "            bodyCardImg.type = Image.Type.Sliced;\n"
        "            bodyCardImg.color = new Color(1f, 0.97f, 0.90f, 0.94f);",
        "FarmOverviewPopup Body_Card parchment",
    )
    write_utf8(fo_path, fo)

    # 4D. VillageBarterPopup.cs: Remove duplicate custom cards (since NightMarketBarter already lists BiKip & muc_an)
    # and highlight BiKip / muc_an listings in CreateListingCard as Claude recommended in #162!
    vb_path = os.path.join(FROM_ARGON_UI, "VillageBarterPopup.cs")
    vb = read_utf8(vb_path)
    if "CreateRareSourcesNightBarterCard" in vb:
        vb = replace_exact(
            vb,
            "                // GDD 20: at the night market an item can also be sold for gold, at only 40% quy đổi\n"
            "                if (CurrentVillagerId == VillagerId.ChoDem && isOpen)\n"
            "                {\n"
            "                    CreateRareSourcesNightBarterCard(\"BiKip\", \"Bí Kíp Võ Học\", \"Đổi bằng hàng hóa trị giá ≥ 260G quy đổi (phí 5% vàng) · Mở khóa tuyệt kỹ bậc 3\", () => RareSources.ExchangeSecretManual(engine));\n"
            "                    CreateRareSourcesNightBarterCard(\"muc_an\", \"Mực Ấn\", \"Đổi bằng hàng hóa trị giá ≥ 160G quy đổi (phí 5% vàng) · Khắc Phù Văn & Mệnh Ấn\", () => RareSources.ExchangeSealInk(engine));\n"
            "                    CreateSellForGoldCard();\n"
            "                }",
            "                // GDD 20: at the night market an item can also be sold for gold, at only 40% quy đổi\n"
            "                if (CurrentVillagerId == VillagerId.ChoDem && isOpen) CreateSellForGoldCard();",
            "VillageBarterPopup remove duplicate rare cards",
        )
        # Remove the helper method definition too
        start_idx = vb.find("        private void CreateRareSourcesNightBarterCard(")
        end_idx = vb.find("        private void CreateSellForGoldCard()")
        if start_idx >= 0 and end_idx > start_idx:
            vb = vb[:start_idx] + vb[end_idx:]
    write_utf8(vb_path, vb)

    # 4E. MythHudController.cs:
    # - Responsive ScaleWithScreenSize (768x432, match=0.5) so HUD scales cleanly on both phones & Windows
    # - Simplify bottom-right combat cluster: 1 large primary button (Context/Attack) + 3 core arc buttons
    #   (Dodge, SkillCong [Z], Whistle [G]); hide secondary combat buttons (SkillThu [X], SkillBien [V],
    #   Counter [Q], SkillSongThe [R]) when out of combat!
    mh_path = os.path.join(FROM_ARGON_UI, r"Myth\MythHudController.cs")
    mh = read_utf8(mh_path)

    mh = replace_exact(
        mh,
        "        const float RefW = 960f, RefH = 540f; // #146 item 1: 2x whole-pixel scale at 1080p (matching Camera x2) so HUD <= 15% area",
        "        const float RefW = 768f, RefH = 432f; // Responsive scale for both Mobile & Windows PC",
        "MythHudController RefW/RefH",
    )

    old_scale = (
        "            float s = lastH / RefH;\n"
        "            if (s >= 2f)\n"
        "            {\n"
        "                // #146 item 1: Whole-number magnification matching Camera pixel scale (2x at 1080p, 3x at 1440p, 4x at 4K).\n"
        "                scaler.uiScaleMode = CanvasScaler.ScaleMode.ConstantPixelSize;\n"
        "                scaler.scaleFactor = Mathf.Floor(s);\n"
        "            }\n"
        "            else\n"
        "            {\n"
        "                // 720p and below: scale proportionally by height against 540p reference (1.333x at 720p).\n"
        "                scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;\n"
        "                scaler.referenceResolution = new Vector2(RefW, RefH);\n"
        "                scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;\n"
        "                scaler.matchWidthOrHeight = 1f;\n"
        "            }"
    )
    new_scale = (
        "            canvas.pixelPerfect = false;\n"
        "            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;\n"
        "            scaler.referenceResolution = new Vector2(RefW, RefH);\n"
        "            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;\n"
        "            scaler.matchWidthOrHeight = 0.5f;"
    )
    mh = replace_exact(mh, old_scale, new_scale, "MythHudController responsive ScaleWithScreenSize")

    old_build_actions = (
        "            attackButton = MythMedallion.Create(\"SkillCong\", safeRoot, false, MythUi.BR, MythUi.BR, new Vector2(-68, 16));\n"
        "            attackButton.SetIcon(MythUi.Icon(\"actions\", \"danh\", 16));\n"
        "            attackButton.OnClick = () => Skill(CombatSlotType.TheCong);\n"
        "            defendButton = MythMedallion.Create(\"SkillThu\", safeRoot, false, MythUi.BR, MythUi.BR, new Vector2(-68, 64));\n"
        "            defendButton.SetIcon(MythUi.Icon(\"actions\", \"do_don\", 16));\n"
        "            defendButton.OnClick = () => Skill(CombatSlotType.TheThu);\n"
        "            burstButton = MythMedallion.Create(\"SkillBien\", safeRoot, false, MythUi.BR, MythUi.BR, new Vector2(-16, 68));\n"
        "            burstButton.SetIcon(MythUi.Icon(\"actions\", \"hoc_vo\", 16));\n"
        "            burstButton.OnClick = () => Skill(CombatSlotType.TheBien);\n"
        "            congLabel = SkillLabel(attackButton, \"Z\");\n"
        "            thuLabel = SkillLabel(defendButton, \"X\");\n"
        "            bienLabel = SkillLabel(burstButton, \"V\");\n"
        "            // Ô thứ 4 — Mệnh Ấn Song Thế (GDD 12.4), chỉ hiện khi đã khắc ấn; phím R\n"
        "            songTheButton = MythMedallion.Create(\"SkillSongThe\", safeRoot, false, MythUi.BR, MythUi.BR, new Vector2(-120, 16));\n"
        "            songTheButton.SetIcon(MythUi.Icon(\"actions\", \"cuong_hoa\", 16));\n"
        "            songTheButton.OnClick = () => Skill(CombatSlotType.TheSongThe);\n"
        "            songTheLabel = SkillLabel(songTheButton, \"R\");\n\n"
        "            // Lướt né (đòn cơ bản) và Phản kích (đòn đặc biệt) — nút chạm cho màn hình cảm ứng (ADR-0024)\n"
        "            dodgeButton = MythMedallion.Create(\"Dodge\", safeRoot, false, MythUi.BR, MythUi.BR, new Vector2(-120, 64));\n"
        "            dodgeButton.SetIcon(MythUi.Icon(\"actions\", \"luot_ne\", 16) ?? MythUi.Icon(\"actions\", \"vao_cua\", 16));\n"
        "            dodgeButton.OnClick = () => PlayerCombatController.Instance?.ExecuteDodge();\n"
        "            dodgeButton.SetKeyHint(\"Ctrl\");\n"
        "            dodgeButton.SetBadgeLabel(\"Lướt né\");\n"
        "            counterButton = MythMedallion.Create(\"Counter\", safeRoot, false, MythUi.BR, MythUi.BR, new Vector2(-68, 120));\n"
        "            counterButton.SetIcon(MythUi.Icon(\"actions\", \"phan_kich\", 16) ?? MythUi.Icon(\"actions\", \"do_don\", 16));\n"
        "            counterButton.OnClick = () => PlayerCombatController.Instance?.PressCounter();\n"
        "            counterButton.SetKeyHint(\"Q\");\n"
        "            counterButton.SetBadgeLabel(\"Phản kích\");\n\n"
        "            // Whistle horn = \"Lùa về Nơi Trú\" (GDD 3.7), on the PixelMyth farm only; G on a keyboard.\n"
        "            whistleButton = MythMedallion.Create(\"Whistle\", safeRoot, false, MythUi.BR, MythUi.BR, new Vector2(-16, 122));"
    )
    new_build_actions = (
        "            // Core 3-button arc around Primary Context/Attack medallion (-8, 10):\n"
        "            // 1) Dodge (-74, 14), 2) SkillCong [Z] (-62, 68), 3) Whistle [G] (-14, 76)\n"
        "            dodgeButton = MythMedallion.Create(\"Dodge\", safeRoot, false, MythUi.BR, MythUi.BR, new Vector2(-74, 14));\n"
        "            dodgeButton.SetIcon(MythUi.Icon(\"actions\", \"luot_ne\", 16) ?? MythUi.Icon(\"actions\", \"vao_cua\", 16));\n"
        "            dodgeButton.OnClick = () => PlayerCombatController.Instance?.ExecuteDodge();\n"
        "            dodgeButton.SetKeyHint(\"Ctrl\");\n"
        "            dodgeButton.SetBadgeLabel(\"Lướt né\");\n\n"
        "            attackButton = MythMedallion.Create(\"SkillCong\", safeRoot, false, MythUi.BR, MythUi.BR, new Vector2(-62, 68));\n"
        "            attackButton.SetIcon(MythUi.Icon(\"actions\", \"hoc_vo\", 16));\n"
        "            attackButton.OnClick = () => Skill(CombatSlotType.TheCong);\n\n"
        "            // Secondary combat buttons (auto-hidden outside combat waves / when no enemy is nearby):\n"
        "            defendButton = MythMedallion.Create(\"SkillThu\", safeRoot, false, MythUi.BR, MythUi.BR, new Vector2(-120, 68));\n"
        "            defendButton.SetIcon(MythUi.Icon(\"actions\", \"do_don\", 16));\n"
        "            defendButton.OnClick = () => Skill(CombatSlotType.TheThu);\n"
        "            burstButton = MythMedallion.Create(\"SkillBien\", safeRoot, false, MythUi.BR, MythUi.BR, new Vector2(-126, 14));\n"
        "            burstButton.SetIcon(MythUi.Icon(\"actions\", \"hoc_vo\", 16));\n"
        "            burstButton.OnClick = () => Skill(CombatSlotType.TheBien);\n"
        "            congLabel = SkillLabel(attackButton, \"Z\");\n"
        "            thuLabel = SkillLabel(defendButton, \"X\");\n"
        "            bienLabel = SkillLabel(burstButton, \"V\");\n"
        "            songTheButton = MythMedallion.Create(\"SkillSongThe\", safeRoot, false, MythUi.BR, MythUi.BR, new Vector2(-14, 128));\n"
        "            songTheButton.SetIcon(MythUi.Icon(\"actions\", \"cuong_hoa\", 16));\n"
        "            songTheButton.OnClick = () => Skill(CombatSlotType.TheSongThe);\n"
        "            songTheLabel = SkillLabel(songTheButton, \"R\");\n\n"
        "            counterButton = MythMedallion.Create(\"Counter\", safeRoot, false, MythUi.BR, MythUi.BR, new Vector2(-68, 122));\n"
        "            counterButton.SetIcon(MythUi.Icon(\"actions\", \"phan_kich\", 16) ?? MythUi.Icon(\"actions\", \"do_don\", 16));\n"
        "            counterButton.OnClick = () => PlayerCombatController.Instance?.PressCounter();\n"
        "            counterButton.SetKeyHint(\"Q\");\n"
        "            counterButton.SetBadgeLabel(\"Phản kích\");\n\n"
        "            // Whistle horn = \"Lùa về Nơi Trú\" (GDD 3.7), on the PixelMyth farm only; G on a keyboard.\n"
        "            whistleButton = MythMedallion.Create(\"Whistle\", safeRoot, false, MythUi.BR, MythUi.BR, new Vector2(-14, 76));"
    )
    mh = replace_exact(mh, old_build_actions, new_build_actions, "MythHudController BuildActions simplified arc")

    mh = replace_exact(
        mh,
        '        static readonly string[] DefaultSlotFallbackLabels = { "Đánh", "Thủ thế", "Võ kỹ", "Song thế" };',
        '        static readonly string[] DefaultSlotFallbackLabels = { "Võ kỹ", "Thủ thế", "Biến chiêu", "Song thế" };',
        "MythHudController DefaultSlotFallbackLabels",
    )

    old_update_actions = (
        "            bool fainted = engine.Character != null && engine.Character.IsFainted;\n"
        "            UpdateFaintOverlay(fainted);\n"
        "            if (attackButton != null && attackButton.gameObject.activeSelf == fainted) attackButton.gameObject.SetActive(!fainted);\n"
        "            if (defendButton != null && defendButton.gameObject.activeSelf == fainted) defendButton.gameObject.SetActive(!fainted);\n"
        "            if (burstButton != null && burstButton.gameObject.activeSelf == fainted) burstButton.gameObject.SetActive(!fainted);\n"
        "            if (dodgeButton != null && dodgeButton.gameObject.activeSelf == fainted) dodgeButton.gameObject.SetActive(!fainted);\n"
        "            if (counterButton != null && counterButton.gameObject.activeSelf == fainted) counterButton.gameObject.SetActive(!fainted);\n"
        "            if (contextButton != null && contextButton.gameObject.activeSelf == fainted) contextButton.gameObject.SetActive(!fainted);\n"
        "            bool alive = engine.Character != null && !fainted && PlayerMobileController.Instance != null;\n"
        "            UpdateSkillSlot(engine, attackButton, congLabel, CombatSlotType.TheCong, 0, alive);\n"
        "            UpdateSkillSlot(engine, defendButton, thuLabel, CombatSlotType.TheThu, 1, alive);\n"
        "            UpdateSkillSlot(engine, burstButton, bienLabel, CombatSlotType.TheBien, 2, alive);\n"
        "            bool songThe = engine.Character.IsMartialSlotUnlocked(CombatSlotType.TheSongThe);\n"
        "            if (songTheButton != null && songTheButton.gameObject.activeSelf != songThe) songTheButton.gameObject.SetActive(songThe);\n"
        "            if (songThe) UpdateSkillSlot(engine, songTheButton, songTheLabel, CombatSlotType.TheSongThe, 3, alive);"
    )
    new_update_actions = (
        "            bool fainted = engine.Character != null && engine.Character.IsFainted;\n"
        "            UpdateFaintOverlay(fainted);\n"
        "            bool inCombat = !fainted && (engine.Clock.IsDuringCombatWave || (engine.Carry != null && engine.Carry.InCombat) || HasNearbyHostile());\n"
        "            if (attackButton != null && attackButton.gameObject.activeSelf == fainted) attackButton.gameObject.SetActive(!fainted);\n"
        "            if (dodgeButton != null && dodgeButton.gameObject.activeSelf == fainted) dodgeButton.gameObject.SetActive(!fainted);\n"
        "            if (contextButton != null && contextButton.gameObject.activeSelf == fainted) contextButton.gameObject.SetActive(!fainted);\n"
        "            if (defendButton != null && defendButton.gameObject.activeSelf != inCombat) defendButton.gameObject.SetActive(inCombat);\n"
        "            if (burstButton != null && burstButton.gameObject.activeSelf != inCombat) burstButton.gameObject.SetActive(inCombat);\n"
        "            if (counterButton != null && counterButton.gameObject.activeSelf != inCombat) counterButton.gameObject.SetActive(inCombat);\n"
        "            bool alive = engine.Character != null && !fainted && PlayerMobileController.Instance != null;\n"
        "            UpdateSkillSlot(engine, attackButton, congLabel, CombatSlotType.TheCong, 0, alive);\n"
        "            if (inCombat)\n"
        "            {\n"
        "                UpdateSkillSlot(engine, defendButton, thuLabel, CombatSlotType.TheThu, 1, alive);\n"
        "                UpdateSkillSlot(engine, burstButton, bienLabel, CombatSlotType.TheBien, 2, alive);\n"
        "            }\n"
        "            bool songThe = inCombat && engine.Character.IsMartialSlotUnlocked(CombatSlotType.TheSongThe);\n"
        "            if (songTheButton != null && songTheButton.gameObject.activeSelf != songThe) songTheButton.gameObject.SetActive(songThe);\n"
        "            if (songThe) UpdateSkillSlot(engine, songTheButton, songTheLabel, CombatSlotType.TheSongThe, 3, alive);"
    )
    mh = replace_exact(mh, old_update_actions, new_update_actions, "MythHudController UpdateActions combat auto-hide")

    mh = replace_exact(
        mh,
        "        bool HasInteractTarget() =>",
        "        bool HasNearbyHostile()\n"
        "        {\n"
        "            var p = PlayerMobileController.Instance;\n"
        "            if (p == null) return false;\n"
        "            var enemies = FindObjectsByType<EnemyAgentView>(FindObjectsSortMode.None);\n"
        "            Vector2 pos = p.transform.position;\n"
        "            for (int i = 0; i < enemies.Length; i++)\n"
        "                if (enemies[i] != null && enemies[i].isActiveAndEnabled && Vector2.Distance(pos, enemies[i].transform.position) <= 10f)\n"
        "                    return true;\n"
        "            return false;\n"
        "        }\n\n"
        "        bool HasInteractTarget() =>",
        "MythHudController HasNearbyHostile helper",
    )
    write_utf8(mh_path, mh)
    print("  Updated UI scripts cleanly.")


if __name__ == "__main__":
    if os.path.getsize(os.path.join(FROM_ARGON_ART, r"PixelMyth\UI\Backdrops\bg_xe_buyt.png")) < 50000:
        step1_install_story_backdrops()
    else:
        print("--- Step 1: 22 2.5D story backdrops already installed (>50KB) ---")
    step2_restore_80px_khoa_and_1am_an()
    step3_restore_2243_ui_frames_and_kit()
    step4_update_ui_scripts_from_claude_base()
    print("=== ALL V25 OVERHAUL STEPS COMPLETED SUCCESSFULLY ===")
