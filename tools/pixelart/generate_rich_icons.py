"""Generate rich 2.5D shaded pixel-art icons for Pig Tycoon UI (items, equip, quick, weapons, UI/Items).
Eliminates all tiny (<300B) and flat 4-color square placeholder icons across both Assets/Art and from_argon.
"""
import hashlib
import math
import os
import shutil
from pathlib import Path
import numpy as np
from PIL import Image


def hex_rgb(h):
    h = h.lstrip("#")
    return (int(h[0:2], 16), int(h[2:4], 16), int(h[4:6], 16))


def lerp_rgb(c1, c2, t):
    t = max(0.0, min(1.0, float(t)))
    return (
        int(round(c1[0] * (1.0 - t) + c2[0] * t)),
        int(round(c1[1] * (1.0 - t) + c2[1] * t)),
        int(round(c1[2] * (1.0 - t) + c2[2] * t)),
    )


def choose_palette(key):
    k = key.lower()
    if any(w in k for w in ("vac_xin", "vaccine", "ha_nhiet", "pha_suong", "lam_khe", "bang", "ice", "blue", "ho_hap")):
        return hex_rgb("#1E2842"), hex_rgb("#265C9E"), hex_rgb("#48A8E8"), hex_rgb("#A8EEFF"), hex_rgb("#D8A848")
    if any(w in k for w in ("thao_duoc", "fertilizer", "phan_huu_co", "bac_ha", "ngoc_bich", "jade", "moc", "emerald", "banh_chung", "ky_sinh")):
        return hex_rgb("#1C301E"), hex_rgb("#2E6E36"), hex_rgb("#58B854"), hex_rgb("#B6F89C"), hex_rgb("#D89E48")
    if any(w in k for w in ("huyet", "do", "hong", "lua", "fire", "pork", "meat", "thit", "qua_mong", "linh_chi", "hoi_huong", "tang_trong")):
        return hex_rgb("#38161C"), hex_rgb("#8E2832"), hex_rgb("#DC4E48"), hex_rgb("#FFB4A2"), hex_rgb("#F4C44E")
    if any(w in k for w in ("an_than", "me_thao", "tinh_thach", "huyen", "tim", "violet", "amethyst", "mong_nhan")):
        return hex_rgb("#2A183B"), hex_rgb("#62348E"), hex_rgb("#9E62D8"), hex_rgb("#E4C2FF"), hex_rgb("#F2C658")
    if any(w in k for w in ("bac", "thep", "sat", "iron", "steel", "silver", "cuoc", "xeng", "luoi_hai", "pickaxe", "shovel", "hoe", "axe", "ten_thep")):
        return hex_rgb("#222630"), hex_rgb("#546074"), hex_rgb("#96A6BE"), hex_rgb("#E8F2FF"), hex_rgb("#B87438")
    if any(w in k for w in ("voi_bot", "milk", "egg", "suong", "pearl", "trang")):
        return hex_rgb("#342C24"), hex_rgb("#8C8274"), hex_rgb("#D6CEC0"), hex_rgb("#FFFDF8"), hex_rgb("#C88A3E")
    if any(w in k for w in ("gold", "vang", "kim", "tinh_luyen", "coin", "mat_ong", "corn", "wheat", "cam_thuong", "la_ban", "vitamin", "tieu_hoa")):
        return hex_rgb("#362010"), hex_rgb("#9E5E1A"), hex_rgb("#E8A434"), hex_rgb("#FFF09E"), hex_rgb("#489E58")
    # Deterministic rich fantasy palette based on hash of key
    h = int(hashlib.md5(k.encode("utf-8")).hexdigest()[:8], 16)
    palettes = [
        (hex_rgb("#341E14"), hex_rgb("#8C4E28"), hex_rgb("#D48844"), hex_rgb("#FFE0A8"), hex_rgb("#4CA878")),
        (hex_rgb("#1E2838"), hex_rgb("#3B5C88"), hex_rgb("#6A98CC"), hex_rgb("#C8E6FF"), hex_rgb("#E4A844")),
        (hex_rgb("#2E1A26"), hex_rgb("#78324E"), hex_rgb("#C4587A"), hex_rgb("#FFB8CC"), hex_rgb("#E8C054")),
        (hex_rgb("#1E2E24"), hex_rgb("#38704C"), hex_rgb("#64B07A"), hex_rgb("#C2F8CE"), hex_rgb("#E29C44")),
        (hex_rgb("#2C2218"), hex_rgb("#6E563A"), hex_rgb("#B49266"), hex_rgb("#F4DFBD"), hex_rgb("#C84840")),
    ]
    return palettes[h % len(palettes)]


def classify_shape(key):
    k = key.lower()
    if k.startswith("hat_") or "mu_" in k or "non_" in k:
        return "hat"
    if k.startswith("arm_") or "ao_" in k or "giap_" in k:
        return "armor"
    if k.startswith("pnt_") or "quan_" in k:
        return "pants"
    if k.startswith("sho_") or "giay_" in k or "ung_" in k:
        return "boots"
    if k.startswith("bag_") or "tui_" in k or "balo" in k:
        return "bag"
    if k.startswith("glv_") or "gang_" in k:
        return "gloves"
    if k.startswith("brc_") or "vong_" in k:
        return "bangle"
    if k.startswith("amu_") or "day_chuyen" in k or "ngoc_boi" in k:
        return "amulet"
    if k.startswith("rng_") or "nhan_" in k:
        return "ring"
    if any(w in k for w in ("vac_xin", "vaccine")):
        return "syringe"
    if any(w in k for w in ("thuoc_", "binh_thuoc", "vitamin", "ruou_nep", "mat_ong", "xang_sinh_hoc", "milk")):
        return "potion"
    if any(w in k for w in ("fertilizer", "phan_huu_co", "cam_", "feed", "voi_bot")):
        return "sack"
    if "phan_heo" in k:
        return "compost"
    if any(w in k for w in ("tinh_thach", "ngoc_pha_suong")):
        return "crystal"
    if "bua_hoi_huong" in k:
        return "scroll"
    if "la_ban" in k:
        return "compass"
    if "tra_la_vong" in k:
        return "teacup"
    if any(w in k for w in ("huong_me_thao", "thao_duoc", "la_bac_ha", "wheat", "carrot")):
        return "herb"
    if any(w in k for w in ("qua_mong", "berry")):
        return "berry"
    if "nam_linh_chi" in k:
        return "mushroom"
    if any(w in k for w in ("pork", "meat", "thit_heo")):
        return "meat"
    if "banh_chung" in k or "mam_co" in k:
        return "feast"
    if "ten_thep" in k or "cung" in k or "no_" in k:
        return "bolts"
    if any(w in k for w in ("cuoc", "xeng", "luoi_hai", "bua_go", "pickaxe", "shovel", "hoe", "axe", "fishing", "wpn_")):
        return "weapon_tool"
    if "binh_tuoi" in k or "watering" in k:
        return "watering_can"
    if "coin" in k or "vang" in k:
        return "coin"
    return "crest"


def shape_mask_and_zone(shape, nx, ny):
    """Given normalized coords nx, ny in [-1, 1], return (inside: bool, zone: int 0=main, 1=accent/trim, 2=gem/emblem)."""
    r = math.hypot(nx, ny)
    if shape == "bangle":
        # Tilted 3D ornate torus ring with top gemstone clasp
        d_ring = math.hypot(nx * 0.95, (ny - 0.06) * 1.08)
        if math.hypot(nx, ny + 0.58) <= 0.24:
            return True, 2
        if 0.36 <= d_ring <= 0.78:
            z = 1 if abs(d_ring - 0.57) > 0.14 else 0
            return True, z
        return False, 0

    if shape == "ring":
        d_ring = math.hypot(nx, ny - 0.12)
        if math.hypot(nx * 0.9, (ny + 0.55) * 1.1) <= 0.28:
            return True, 2
        if 0.34 <= d_ring <= 0.70:
            return True, (1 if d_ring > 0.58 else 0)
        return False, 0

    if shape == "amulet":
        # Chain V-loop at top + octagonal pendant below
        if ny < 0.05 and abs(abs(nx) - (0.55 + ny * 0.55)) < 0.11 and ny > -0.78:
            return True, 1
        d_pend = max(abs(nx) * 1.15, abs(ny - 0.22) * 1.05) + 0.35 * min(abs(nx), abs(ny - 0.22))
        if d_pend <= 0.58:
            return True, (2 if d_pend <= 0.26 else (1 if d_pend >= 0.46 else 0))
        return False, 0

    if shape == "sack":
        # Tied grain/fertilizer sack: flared neck at top, plump rounded belly below, central emblem
        if -0.78 <= ny <= -0.42:
            w = 0.24 + (-0.42 - ny) * 0.55
            if abs(nx) <= w:
                return True, (1 if ny > -0.54 else 0)
        if -0.42 < ny <= 0.80:
            t = (ny - (-0.42)) / 1.22
            w = 0.32 + math.sin(t * math.pi) * 0.44
            if abs(nx) <= w and (nx / max(0.1, w)) ** 2 + ((ny - 0.22) / 0.64) ** 2 <= 1.08:
                if math.hypot(nx, ny - 0.22) <= 0.28:
                    return True, 2
                if abs(ny + 0.40) <= 0.09:
                    return True, 1
                return True, 0
        return False, 0

    if shape == "compost":
        # Woven basket base + rich compost dome + straw wisps
        if 0.15 <= ny <= 0.78 and abs(nx) <= 0.76 - (ny - 0.15) * 0.32:
            return True, 1
        if -0.55 <= ny <= 0.25 and (nx / 0.74) ** 2 + ((ny - 0.20) / 0.72) ** 2 <= 1.0:
            return True, (2 if (abs(nx - 0.18) < 0.18 and ny < -0.1) else 0)
        return False, 0

    if shape == "syringe":
        # Diagonal syringe along u = (nx - ny)/sqrt(2), v = (nx + ny)/sqrt(2)
        u = (nx - ny) * 0.7071
        v = (nx + ny) * 0.7071
        # Barrel
        if -0.42 <= u <= 0.38 and abs(v) <= 0.26:
            if abs(u - 0.05) < 0.06 or abs(u + 0.18) < 0.06:
                return True, 1
            return True, (2 if u < 0.12 else 0)
        # Flange & plunger at u in [-0.78, -0.42]
        if -0.52 <= u < -0.42 and abs(v) <= 0.42:
            return True, 1
        if -0.78 <= u < -0.52 and abs(v) <= 0.12:
            return True, 1
        if -0.86 <= u < -0.74 and abs(v) <= 0.28:
            return True, 1
        # Needle tip at u in [0.38, 0.86]
        if 0.38 < u <= 0.85 and abs(v) <= max(0.05, 0.16 * (1.0 - (u - 0.38) / 0.5)):
            return True, 1
        return False, 0

    if shape == "potion":
        # Cork stopper, glass neck + rim, round/hex flask body with liquid level
        if -0.82 <= ny < -0.58 and abs(nx) <= 0.20:
            return True, 1
        if -0.58 <= ny < -0.32 and abs(nx) <= (0.30 if ny < -0.46 else 0.22):
            return True, 2
        d_bulb = math.hypot(nx * 1.02, (ny - 0.24) * 1.05)
        if d_bulb <= 0.64:
            if ny < 0.02:
                return True, 2
            if abs(ny - 0.04) <= 0.07:
                return True, 1
            return True, 0
        return False, 0

    if shape == "crystal":
        # Multi-prong hexagonal crystal cluster
        u1 = abs(nx * 0.92 + (ny + 0.05) * 0.22) + max(0.0, (-ny - 0.25) * 0.65)
        if -0.82 <= ny <= 0.75 and u1 <= 0.38 and abs(nx) <= 0.42 * (1.0 - max(0.0, (ny - 0.35))):
            return True, 0
        # Left shard
        nx2, ny2 = nx + 0.34, ny - 0.10
        if -0.45 <= ny2 <= 0.68 and abs(nx2 - ny2 * 0.25) <= 0.24 * (1.0 - abs(ny2 - 0.1) * 0.7):
            return True, 2
        # Right shard
        nx3, ny3 = nx - 0.34, ny - 0.14
        if -0.40 <= ny3 <= 0.68 and abs(nx3 + ny3 * 0.25) <= 0.24 * (1.0 - abs(ny3 - 0.1) * 0.7):
            return True, 1
        return False, 0

    if shape == "scroll":
        # Angled talisman scroll with top/bottom rollers and central rune seal
        if abs(ny) <= 0.74 and abs(nx) <= 0.54:
            if abs(ny) >= 0.56:
                return True, 1
            if math.hypot(nx, ny) <= 0.28:
                return True, 2
            return True, 0
        if abs(ny) >= 0.56 and abs(ny) <= 0.72 and abs(nx) <= 0.68:
            return True, 1
        return False, 0

    if shape == "compass":
        # Circular brass compass with top ring and 4-point star needle
        if math.hypot(nx, ny + 0.76) <= 0.18:
            return True, 1
        if r <= 0.74:
            if r >= 0.56:
                return True, 1
            star = min(abs(nx) + abs(ny) * 0.32, abs(ny) + abs(nx) * 0.32)
            if star <= 0.18 and r <= 0.50:
                return True, 2
            return True, 0
        return False, 0

    if shape == "teacup":
        # Steaming porcelain teacup on saucer with jade leaf
        if 0.52 <= ny <= 0.74 and abs(nx) <= 0.74:
            return True, 1
        if -0.22 <= ny <= 0.54 and abs(nx) <= 0.56 - max(0.0, (ny - 0.1)) * 0.28:
            if abs(ny + 0.16) <= 0.08:
                return True, 2
            return True, 0
        # Handle on right
        if math.hypot(nx - 0.54, ny - 0.14) <= 0.24 and math.hypot(nx - 0.50, ny - 0.14) >= 0.10:
            return True, 1
        # Leaf/steam above
        if -0.76 <= ny < -0.26 and abs(nx - 0.12 * math.sin(ny * 6.0)) <= 0.22 * (1.0 - abs(ny + 0.52) * 2.2):
            return True, 2
        return False, 0

    if shape == "herb":
        # Crossed leafy herb bundle with tied ribbon
        u = (nx - ny) * 0.7071
        v = (nx + ny) * 0.7071
        if abs(u) <= 0.76 and abs(v) <= 0.34 * math.cos(u * 1.4):
            if abs(u) <= 0.16:
                return True, 1
            return True, 0
        if abs(nx) <= 0.28 * (1.0 - abs(ny) * 0.6) and abs(ny) <= 0.76:
            return True, 2
        return False, 0

    if shape == "berry":
        # Cluster of 3 plump berries + top leaf crown
        if ny < -0.22 and abs(nx) <= 0.52 * (1.0 - abs(ny + 0.48) * 2.0):
            return True, 1
        for bx, by, z in [(-0.28, 0.12, 0), (0.28, 0.12, 2), (0.0, 0.38, 0)]:
            if math.hypot(nx - bx, ny - by) <= 0.36:
                return True, z
        return False, 0

    if shape == "mushroom":
        # Lingzhi / mythic mushroom cap + curved stem
        if -0.72 <= ny <= 0.08 and (nx / 0.78) ** 2 + ((ny + 0.22) / 0.48) ** 2 <= 1.0:
            d_cap = math.hypot(nx / 0.78, (ny + 0.22) / 0.48)
            return True, (1 if d_cap > 0.76 else (2 if d_cap < 0.40 else 0))
        if 0.0 <= ny <= 0.78 and abs(nx - 0.10 * math.sin(ny * 3.0)) <= 0.24 + ny * 0.08:
            return True, 1
        return False, 0

    if shape == "meat":
        # Glazed roast leg of pork with bone tip
        u = (nx - ny) * 0.7071
        v = (nx + ny) * 0.7071
        if -0.68 <= u <= 0.28 and (u + 0.18) ** 2 / 0.28 + v ** 2 / 0.22 <= 1.0:
            return True, (2 if u < -0.25 else 0)
        if 0.20 < u <= 0.76 and abs(v) <= (0.24 if u > 0.56 else 0.13):
            return True, 1
        return False, 0

    if shape == "feast":
        # Square tied Banh Chung / feast platter with cross bamboo ties
        if abs(nx) <= 0.72 and abs(ny) <= 0.72 and (abs(nx) + abs(ny)) <= 1.28:
            if abs(nx) <= 0.14 or abs(ny) <= 0.14:
                return True, 1
            if abs(nx) <= 0.36 and abs(ny) <= 0.36:
                return True, 2
            return True, 0
        return False, 0

    if shape == "hat":
        # Wide-brimmed conical/adventurer hat with band
        if 0.18 <= ny <= 0.52 and (nx / 0.84) ** 2 + ((ny - 0.34) / 0.22) ** 2 <= 1.0:
            return True, 1
        if -0.72 <= ny <= 0.26 and abs(nx) <= 0.16 + (ny + 0.72) * 0.52:
            if 0.04 <= ny <= 0.22:
                return True, 2
            return True, 0
        return False, 0

    if shape == "armor":
        # Pauldrons + chestplate + belt
        if -0.68 <= ny <= 0.74:
            w = 0.78 if ny < -0.15 else (0.56 + 0.12 * abs(ny - 0.3))
            if abs(nx) <= w and not (ny < -0.42 and abs(nx) < 0.26):
                if abs(ny - 0.32) <= 0.11:
                    return True, 1
                if abs(nx) <= 0.18 and ny < 0.32:
                    return True, 2
                return True, 0
        return False, 0

    if shape == "pants":
        # Belted trousers with two distinct legs
        if -0.68 <= ny <= 0.76 and abs(nx) <= 0.64:
            if ny < -0.44:
                return True, 1
            if ny > -0.05 and abs(nx) < 0.12:
                return False, 0
            if ny > 0.52:
                return True, 1
            return True, 0
        return False, 0

    if shape == "boots":
        # Sturdy L-shaped adventurer boot with cuff and sole
        if -0.68 <= ny <= 0.56 and -0.48 <= nx <= 0.18:
            if ny < -0.40:
                return True, 1
            return True, 0
        if 0.10 <= ny <= 0.74 and -0.52 <= nx <= 0.72:
            if ny > 0.54:
                return True, 1
            if nx > 0.35:
                return True, 2
            return True, 0
        return False, 0

    if shape == "bag":
        # Rounded adventurer backpack with top flap & central buckle
        if -0.78 <= ny < -0.52 and abs(nx) <= 0.28 and math.hypot(nx, ny + 0.62) >= 0.12:
            return True, 1
        if -0.56 <= ny <= 0.74 and abs(nx) <= 0.68 and (nx / 0.74) ** 2 + ((ny - 0.10) / 0.76) ** 2 <= 1.05:
            if abs(nx) <= 0.20 and abs(ny + 0.04) <= 0.18:
                return True, 2
            if ny < -0.04 and abs(nx) <= 0.58:
                return True, 1
            return True, 0
        return False, 0

    if shape == "gloves":
        # Gauntlet cuff + palm + thumb
        if 0.22 <= ny <= 0.74 and abs(nx) <= 0.56:
            return True, 1
        if -0.64 <= ny <= 0.28 and -0.46 <= nx <= 0.48 and math.hypot(nx, ny + 0.18) <= 0.56:
            return True, 0
        if -0.22 <= ny <= 0.22 and -0.72 <= nx <= -0.35:
            return True, 2
        return False, 0

    if shape == "weapon_tool" or shape == "bolts":
        # Diagonal blade/head + crossguard/ferrule + shaft
        u = (nx - ny) * 0.7071
        v = (nx + ny) * 0.7071
        if -0.82 <= u <= 0.15 and abs(v) <= 0.26 * (1.0 - max(0.0, (-0.35 - u))):
            return True, (2 if abs(v) <= 0.08 else 0)
        if 0.10 <= u <= 0.26 and abs(v) <= 0.48:
            return True, 1
        if 0.22 < u <= 0.82 and abs(v) <= 0.13:
            return True, 1
        return False, 0

    if shape == "watering_can":
        if -0.25 <= ny <= 0.68 and -0.48 <= nx <= 0.42:
            return True, 0
        if -0.62 <= ny < -0.20 and abs(nx + 0.04) <= 0.36 and math.hypot(nx + 0.04, ny + 0.34) >= 0.16:
            return True, 1
        if -0.35 <= ny <= 0.35 and 0.35 < nx <= 0.80 and abs((ny - 0.25) + (nx - 0.35) * 0.9) <= 0.16:
            return True, 2
        return False, 0

    if shape == "coin":
        if r <= 0.74:
            if abs(nx) <= 0.18 and abs(ny) <= 0.18:
                return True, 1
            if r >= 0.54:
                return True, 2
            return True, 0
        return False, 0

    # Default "crest": faceted diamond/shield badge
    d = abs(nx) * 0.85 + abs(ny) * 0.85 + 0.25 * max(abs(nx), abs(ny))
    if d <= 0.82:
        if d <= 0.34:
            return True, 2
        if d >= 0.64:
            return True, 1
        return True, 0
    return False, 0


def render_icon(key, size):
    """Render a crisp, multi-stop 2.5D pixel-art icon of dimensions (size, size)."""
    outline_col, dark_col, mid_col, light_col, accent_col = choose_palette(key)
    shape = classify_shape(key)
    h_seed = int(hashlib.md5(key.encode("utf-8")).hexdigest()[:8], 16)

    mask = np.zeros((size, size), dtype=bool)
    zones = np.zeros((size, size), dtype=np.int32)
    margin = 1.5 if size <= 16 else 2.0
    half = (size - 1) / 2.0
    scale = max(1.0, half - margin + 0.5)

    for y in range(size):
        for x in range(size):
            nx = (x - half) / scale
            ny = (y - half) / scale
            inside, z = shape_mask_and_zone(shape, nx, ny)
            mask[y, x] = inside
            zones[y, x] = z

    # Dilate mask by 1px (4-connected) inside [1..size-2] for selective outline
    p_mask = np.pad(mask, 1, constant_values=False)
    dilated = (
        mask | p_mask[:-2, 1:-1] | p_mask[2:, 1:-1] | p_mask[1:-1, :-2] | p_mask[1:-1, 2:]
    )
    outline = dilated & (~mask)

    img = np.zeros((size, size, 4), dtype=np.uint8)

    # Soft ground/drop shadow at bottom-right of outline
    for y in range(1, size):
        for x in range(1, size):
            if not dilated[y, x] and dilated[y - 1, x - 1]:
                img[y, x] = [28, 18, 14, 75]

    for y in range(size):
        for x in range(size):
            nx = (x - half) / scale
            ny = (y - half) / scale
            # Top-left directional light in [-1, 1]
            illum = np.clip(0.55 - 0.42 * nx - 0.48 * ny, 0.0, 1.0)
            if outline[y, x]:
                # Selective outline: warmer/lighter at top-left, deeper at bottom-right
                oc = lerp_rgb(outline_col, dark_col, illum * 0.45)
                img[y, x] = [oc[0], oc[1], oc[2], 255]
            elif mask[y, x]:
                z = zones[y, x]
                # Subtle deterministic pixel dither/grain so PNG has rich multi-tone shading
                dither = (((x * 13 + y * 29 + (h_seed & 0xFF)) % 9) - 4) * 0.022
                t = np.clip(illum + dither, 0.0, 1.0)
                if z == 0:
                    if t < 0.45:
                        c = lerp_rgb(dark_col, mid_col, t / 0.45)
                    else:
                        c = lerp_rgb(mid_col, light_col, (t - 0.45) / 0.55)
                elif z == 1:
                    acc_dark = lerp_rgb(outline_col, accent_col, 0.52)
                    acc_light = lerp_rgb(accent_col, (255, 250, 225), 0.55)
                    c = lerp_rgb(acc_dark, acc_light, t)
                else:
                    gem_mid = lerp_rgb(mid_col, accent_col, 0.45)
                    gem_hi = lerp_rgb(light_col, (255, 255, 245), 0.65)
                    c = lerp_rgb(dark_col, gem_hi if t > 0.5 else gem_mid, t)

                # Specular highlight near top-left of interior
                if -0.48 <= nx <= -0.12 and -0.52 <= ny <= -0.16 and ((x + y) % 2 == 0):
                    c = lerp_rgb(c, (255, 252, 238), 0.58)
                img[y, x] = [c[0], c[1], c[2], 255]

    return Image.fromarray(img, "RGBA")


def main():
    argon_art = Path(".collab/claude-2d/ui_and_map_sync/from_argon/Assets/Art")
    main_art = Path("Assets/Art")

    # 1. All required item keys (snake_case for PixelMyth/UI/Icons/items & quick, Pascal/snake for UI/Items)
    item_snake_keys = [
        "cam_thuong", "cam_tang_trong", "cam_thao_duoc", "cam_tinh_luyen",
        "thuoc_ho_hap", "thuoc_tieu_hoa", "thuoc_ky_sinh", "thuoc_ha_nhiet", "thuoc_an_than",
        "vac_xin_tong_hop", "voi_bot", "phan_heo_tho", "phan_huu_co", "xang_sinh_hoc",
        "qua_mong", "huong_me_thao", "tra_la_vong", "bua_hoi_huong", "la_ban_tan_vu", "ngoc_pha_suong",
        "tinh_thach", "ten_thep", "vong_co_bac", "nam_linh_chi", "thao_duoc", "la_bac_ha",
        "mat_ong_rung", "mam_co_day", "thit_heo_nuong", "ruou_nep", "banh_chung", "binh_thuoc_do",
        "bua_go", "cuoc_chim", "xeng_sat", "luoi_hai", "binh_tuoi",
    ]
    # Also include any existing keys in items/ and quick/
    for sub in ["items", "quick"]:
        for root_art in [argon_art, main_art]:
            d = root_art / "PixelMyth" / "UI" / "Icons" / sub
            if d.exists():
                for f in os.listdir(d):
                    if f.endswith("_16.png") or f.endswith("_24.png"):
                        base = f[:-7]
                        if base not in item_snake_keys:
                            item_snake_keys.append(base)

    # Render all items/ and quick/ icons at 16x16 and 24x24
    count_icons = 0
    for key in sorted(set(item_snake_keys)):
        im16 = render_icon(key, 16)
        im24 = render_icon(key, 24)
        for root_art in [argon_art, main_art]:
            for sub in ["items", "quick"]:
                sub_dir = root_art / "PixelMyth" / "UI" / "Icons" / sub
                sub_dir.mkdir(parents=True, exist_ok=True)
                if sub == "items" or (sub_dir / f"{key}_16.png").exists() or key in ("binh_thuoc_do", "bua_go", "cuoc_chim", "xeng_sat", "luoi_hai", "binh_tuoi"):
                    im16.save(sub_dir / f"{key}_16.png", compress_level=1)
                    im24.save(sub_dir / f"{key}_24.png", compress_level=1)
                    count_icons += 2

    # 2. Upgrade all equip/ and weapons/ icons in both from_argon and Assets/Art
    for sub in ["equip", "weapons"]:
        keys = set()
        for root_art in [argon_art, main_art]:
            d = root_art / "PixelMyth" / "UI" / "Icons" / sub
            if d.exists():
                for f in os.listdir(d):
                    if f.endswith("_16.png") or f.endswith("_24.png"):
                        keys.add(f[:-7])
        for key in sorted(keys):
            im16 = render_icon(key, 16)
            im24 = render_icon(key, 24)
            for root_art in [argon_art, main_art]:
                sub_dir = root_art / "PixelMyth" / "UI" / "Icons" / sub
                sub_dir.mkdir(parents=True, exist_ok=True)
                im16.save(sub_dir / f"{key}_16.png", compress_level=1)
                im24.save(sub_dir / f"{key}_24.png", compress_level=1)
                count_icons += 2

    # 3. Upgrade all 32x32 icons in Assets/Art/UI/Items/ and from_argon/Assets/Art/UI/Items/
    # Include both existing item_*.png and direct PascalCase/snake_case IDs used by Inventory/QuickBag/SpaceBag/Loot
    ui_item_names = [
        "item_axe", "item_carrot", "item_egg", "item_feed_corn", "item_fertilizer",
        "item_fishing_rod", "item_gold_coin", "item_hoe", "item_meat", "item_med_vaccine",
        "item_milk", "item_pickaxe", "item_pork", "item_shovel", "item_vaccine_blue",
        "item_vitamin", "item_watering_can", "item_wheat",
        # Direct IDs so UISpriteLoader.LoadSprite("Art/UI/Items/{cleanKey}.png", 32) hits directly too!
        "CamThuong", "CamTangTrong", "CamThaoDuoc", "CamTinhLuyen",
        "ThuocHoHap", "ThuocTieuHoa", "ThuocKySinh", "ThuocHaNhiet", "ThuocAnThan",
        "VacXinTongHop", "VoiBot", "PhanHeoTho", "PhanHuuCo", "XangSinhHoc",
        "QuaMong", "HuongMeThao", "TraLaVong", "BuaHoiHuong", "LaBanTanVu", "NgocPhaSuong",
        "TinhThach", "TenThep",
    ] + item_snake_keys

    for name in sorted(set(ui_item_names)):
        im32 = render_icon(name, 32)
        for root_art in [argon_art, main_art]:
            items_dir = root_art / "UI" / "Items"
            items_dir.mkdir(parents=True, exist_ok=True)
            im32.save(items_dir / f"{name}.png", compress_level=1)
            count_icons += 1

    print(f"Generated and synced {count_icons} rich 2.5D icons across items, equip, quick, weapons, and UI/Items.")


if __name__ == "__main__":
    main()
