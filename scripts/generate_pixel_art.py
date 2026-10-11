import os
import math
from PIL import Image, ImageDraw

BASE_DIR = "Assets/Art"
TILES_DIR = os.path.join(BASE_DIR, "Tiles")
CHARS_DIR = os.path.join(BASE_DIR, "Sprites/Characters")
PIGS_DIR = os.path.join(BASE_DIR, "Sprites/Pigs")
ENV_DIR = os.path.join(BASE_DIR, "Sprites/Environment")

for d in [TILES_DIR, CHARS_DIR, PIGS_DIR, ENV_DIR]:
    os.makedirs(d, exist_ok=True)

# ==========================================
# 1. TILES GENERATION (32x32)
# ==========================================
def create_grass_base():
    img = Image.new("RGBA", (32, 32), (82, 147, 52, 255))
    draw = ImageDraw.Draw(img)
    # Add subtle pixel patterns for lush grass blades
    blades = [
        (4, 5), (5, 4), (12, 10), (13, 9), (20, 3), (21, 2),
        (8, 18), (9, 17), (25, 14), (26, 13), (3, 27), (4, 26),
        (15, 23), (16, 22), (28, 26), (29, 25), (18, 29), (19, 28)
    ]
    dark_blades = [
        (4, 6), (12, 11), (20, 4), (8, 19), (25, 15), (3, 28),
        (15, 24), (28, 27), (18, 30), (10, 2), (22, 20), (1, 14)
    ]
    for x, y in blades:
        img.putpixel((x % 32, y % 32), (107, 172, 70, 255))
        img.putpixel(((x + 1) % 32, y % 32), (118, 185, 78, 255))
    for x, y in dark_blades:
        img.putpixel((x % 32, y % 32), (61, 113, 36, 255))
        img.putpixel(((x) % 32, (y + 1) % 32), (50, 95, 29, 255))
    return img

def create_grass_flower(flower_type=1):
    img = create_grass_base()
    draw = ImageDraw.Draw(img)
    if flower_type == 1: # White daisies
        spots = [(8, 8), (22, 18), (14, 25)]
        for fx, fy in spots:
            # 4 white petals around center
            draw.rectangle([fx - 1, fy, fx + 1, fy], fill=(255, 255, 255, 255))
            draw.rectangle([fx, fy - 1, fx, fy + 1], fill=(255, 255, 255, 255))
            draw.point((fx, fy), fill=(255, 215, 0, 255)) # yellow center
    elif flower_type == 2: # Red poppies
        spots = [(10, 14), (24, 8), (6, 24)]
        for fx, fy in spots:
            draw.rectangle([fx - 1, fy - 1, fx + 1, fy + 1], fill=(225, 45, 40, 255))
            draw.point((fx, fy), fill=(35, 15, 15, 255))
    elif flower_type == 3: # Blue wildbells / clover
        spots = [(12, 6), (20, 24), (5, 16)]
        for fx, fy in spots:
            draw.point((fx, fy - 1), fill=(100, 180, 255, 255))
            draw.point((fx - 1, fy), fill=(70, 140, 245, 255))
            draw.point((fx + 1, fy), fill=(70, 140, 245, 255))
            draw.point((fx, fy), fill=(255, 255, 160, 255))
    return img

def create_stone_path():
    img = Image.new("RGBA", (32, 32), (65, 68, 72, 255)) # mortar dark base
    draw = ImageDraw.Draw(img)
    # Cobblestones layout
    stones = [
        (1, 1, 14, 13),
        (17, 1, 30, 9),
        (16, 11, 30, 21),
        (1, 15, 13, 23),
        (1, 25, 14, 30),
        (16, 23, 30, 30)
    ]
    for (x1, y1, x2, y2) in stones:
        draw.rectangle([x1, y1, x2, y2], fill=(138, 143, 152, 255))
        # Top-left highlight
        draw.line([(x1, y1), (x2 - 1, y1)], fill=(175, 180, 190, 255))
        draw.line([(x1, y1), (x1, y2 - 1)], fill=(175, 180, 190, 255))
        # Bottom-right shadow
        draw.line([(x1, y2), (x2, y2)], fill=(92, 96, 104, 255))
        draw.line([(x2, y1), (x2, y2)], fill=(92, 96, 104, 255))
    return img

def create_dirt_patch():
    img = Image.new("RGBA", (32, 32), (117, 72, 46, 255))
    draw = ImageDraw.Draw(img)
    # Tilled furrows
    for y in range(2, 30, 6):
        draw.line([(0, y), (31, y)], fill=(91, 54, 32, 255))
        draw.line([(0, y + 1), (31, y + 1)], fill=(143, 93, 61, 255))
    # Little clods of earth
    clods = [(5, 7), (18, 12), (9, 21), (26, 18), (14, 27)]
    for cx, cy in clods:
        draw.rectangle([cx, cy, cx + 1, cy + 1], fill=(155, 105, 70, 255))
    return img

def create_mud_tile():
    img = Image.new("RGBA", (32, 32), (84, 56, 30, 255))
    draw = ImageDraw.Draw(img)
    # Darker sludge puddles
    draw.ellipse([3, 4, 18, 16], fill=(62, 40, 20, 255))
    draw.ellipse([14, 12, 29, 27], fill=(62, 40, 20, 255))
    # Water sheen ripples
    draw.arc([5, 6, 16, 14], 180, 360, fill=(110, 76, 43, 255))
    draw.arc([16, 14, 27, 25], 180, 360, fill=(110, 76, 43, 255))
    # Wet highlight specks
    draw.point((10, 8), fill=(160, 115, 70, 255))
    draw.point((22, 17), fill=(160, 115, 70, 255))
    return img

# Save tiles
create_grass_base().save(os.path.join(TILES_DIR, "grass_base.png"))
create_grass_flower(1).save(os.path.join(TILES_DIR, "grass_flower_1.png"))
create_grass_flower(2).save(os.path.join(TILES_DIR, "grass_flower_2.png"))
create_grass_flower(3).save(os.path.join(TILES_DIR, "grass_flower_3.png"))
create_stone_path().save(os.path.join(TILES_DIR, "stone_path.png"))
create_dirt_patch().save(os.path.join(TILES_DIR, "dirt_patch.png"))
create_mud_tile().save(os.path.join(TILES_DIR, "mud_tile.png"))
print("Tiles saved successfully.")

# ==========================================
# 2. ENVIRONMENT & PROPS
# ==========================================
def create_fence_post():
    img = Image.new("RGBA", (32, 32), (0, 0, 0, 0))
    draw = ImageDraw.Draw(img)
    # Wooden vertical post
    x1, x2 = 12, 19
    draw.rectangle([x1, 4, x2, 28], fill=(139, 90, 43, 255))
    # Highlight left, shadow right
    draw.line([(x1, 4), (x1, 28)], fill=(175, 120, 68, 255))
    draw.line([(x2, 4), (x2, 28)], fill=(86, 53, 22, 255))
    # Pointed top
    draw.polygon([(x1, 4), (15, 1), (16, 1), (x2, 4)], fill=(160, 108, 55, 255))
    # Iron band in middle
    draw.rectangle([x1, 14, x2, 16], fill=(55, 58, 62, 255))
    draw.point((15, 15), fill=(190, 195, 200, 255)) # iron nail
    return img

def create_fence_h():
    img = Image.new("RGBA", (32, 32), (0, 0, 0, 0))
    draw = ImageDraw.Draw(img)
    # Center post
    draw.rectangle([13, 2, 18, 29], fill=(139, 90, 43, 255))
    draw.line([(13, 2), (13, 29)], fill=(175, 120, 68, 255))
    draw.line([(18, 2), (18, 29)], fill=(86, 53, 22, 255))
    # Top horizontal rail
    draw.rectangle([0, 7, 31, 12], fill=(150, 98, 48, 255))
    draw.line([(0, 7), (31, 7)], fill=(185, 130, 75, 255))
    draw.line([(0, 12), (31, 12)], fill=(95, 58, 26, 255))
    # Bottom horizontal rail
    draw.rectangle([0, 18, 31, 23], fill=(150, 98, 48, 255))
    draw.line([(0, 18), (31, 18)], fill=(185, 130, 75, 255))
    draw.line([(0, 23), (31, 23)], fill=(95, 58, 26, 255))
    # Iron nails
    draw.point((15, 9), fill=(40, 40, 45, 255))
    draw.point((15, 20), fill=(40, 40, 45, 255))
    return img

def create_feeder():
    img = Image.new("RGBA", (64, 32), (0, 0, 0, 0))
    draw = ImageDraw.Draw(img)
    # Outer wood box
    draw.rectangle([4, 6, 59, 27], fill=(139, 90, 43, 255))
    # Dark trough cavity
    draw.rectangle([7, 9, 56, 24], fill=(70, 42, 18, 255))
    # Grain piles (golden corn/oats)
    draw.rectangle([9, 12, 54, 22], fill=(245, 203, 66, 255))
    # Golden grain details & seeds
    for gx in range(11, 53, 4):
        draw.point((gx, 14), fill=(255, 235, 120, 255))
        draw.point((gx + 2, 18), fill=(215, 165, 40, 255))
    # Wooden border highlight & shadow
    draw.line([(4, 6), (59, 6)], fill=(180, 125, 70, 255))
    draw.line([(4, 6), (4, 27)], fill=(180, 125, 70, 255))
    draw.line([(4, 27), (59, 27)], fill=(75, 45, 18, 255))
    draw.line([(59, 6), (59, 27)], fill=(75, 45, 18, 255))
    # 4 corner wooden legs
    draw.rectangle([3, 24, 7, 30], fill=(105, 65, 28, 255))
    draw.rectangle([56, 24, 60, 30], fill=(105, 65, 28, 255))
    return img

def create_water_trough():
    img = Image.new("RGBA", (64, 32), (0, 0, 0, 0))
    draw = ImageDraw.Draw(img)
    # Granite stone trough rim
    draw.rectangle([4, 6, 59, 27], fill=(135, 140, 148, 255))
    draw.line([(4, 6), (59, 6)], fill=(175, 180, 190, 255))
    draw.line([(4, 27), (59, 27)], fill=(85, 90, 98, 255))
    # Water pool
    draw.rectangle([7, 9, 56, 24], fill=(50, 130, 200, 255))
    # Shimmering water highlights & wave ripples
    draw.line([(10, 12), (32, 12)], fill=(120, 190, 245, 255))
    draw.line([(36, 17), (52, 17)], fill=(120, 190, 245, 255))
    draw.point((15, 13), fill=(230, 245, 255, 255))
    draw.point((42, 18), fill=(230, 245, 255, 255))
    # Corner supports
    draw.rectangle([3, 24, 7, 30], fill=(95, 100, 108, 255))
    draw.rectangle([56, 24, 60, 30], fill=(95, 100, 108, 255))
    return img

def create_shelter_barn():
    img = Image.new("RGBA", (128, 96), (0, 0, 0, 0))
    draw = ImageDraw.Draw(img)
    # Barn Red Body
    draw.rectangle([12, 34, 115, 91], fill=(160, 52, 42, 255))
    # Vertical wood planks
    for px in range(16, 112, 8):
        draw.line([(px, 34), (px, 91)], fill=(115, 34, 26, 255))
    # Large open doorway in center
    draw.rectangle([44, 52, 83, 91], fill=(42, 20, 16, 255))
    # Golden hay bales inside doorway
    draw.rectangle([48, 74, 62, 90], fill=(235, 190, 55, 255))
    draw.rectangle([65, 78, 79, 90], fill=(215, 170, 45, 255))
    # Doorway wooden arch beam
    draw.rectangle([42, 50, 85, 54], fill=(195, 140, 80, 255))
    draw.line([(42, 54), (42, 91)], fill=(195, 140, 80, 255))
    draw.line([(85, 54), (85, 91)], fill=(195, 140, 80, 255))
    # Roof (Mansard / A-Frame Shingled)
    roof_poly = [(4, 38), (64, 4), (123, 38)]
    draw.polygon(roof_poly, fill=(88, 38, 30, 255))
    # Roof edge overhang highlight
    draw.line([(4, 38), (64, 4)], fill=(145, 65, 50, 255), width=2)
    draw.line([(64, 4), (123, 38)], fill=(145, 65, 50, 255), width=2)
    draw.line([(2, 39), (125, 39)], fill=(60, 24, 18, 255), width=2)
    # Cute little round attic window
    draw.ellipse([54, 20, 73, 32], fill=(225, 235, 245, 255), outline=(145, 100, 60, 255))
    draw.line([(63, 20), (63, 32)], fill=(145, 100, 60, 255))
    draw.line([(54, 26), (73, 26)], fill=(145, 100, 60, 255))
    # Lantern hanging beside entrance
    draw.line([(38, 56), (38, 62)], fill=(40, 40, 40, 255))
    draw.rectangle([36, 62, 40, 68], fill=(255, 215, 75, 255))
    return img

def create_defense_tower():
    img = Image.new("RGBA", (48, 64), (0, 0, 0, 0))
    draw = ImageDraw.Draw(img)
    # 4 Timber stilts
    draw.line([(10, 26), (6, 60)], fill=(130, 80, 36, 255), width=2)
    draw.line([(37, 26), (41, 60)], fill=(130, 80, 36, 255), width=2)
    # Stilt crossbeams
    draw.line([(8, 42), (39, 42)], fill=(105, 62, 25, 255), width=2)
    draw.line([(8, 42), (39, 56)], fill=(90, 52, 20, 255))
    draw.line([(39, 42), (8, 56)], fill=(90, 52, 20, 255))
    # Guard platform
    draw.rectangle([6, 20, 41, 26], fill=(160, 105, 52, 255))
    # Wooden railing
    draw.rectangle([6, 12, 41, 15], fill=(185, 125, 68, 255))
    for rx in range(8, 40, 6):
        draw.line([(rx, 15), (rx, 20)], fill=(125, 75, 32, 255))
    # Mounted Ballista / Crossbow on top
    draw.rectangle([21, 6, 26, 16], fill=(60, 65, 72, 255)) # mount
    draw.arc([14, 2, 33, 14], 180, 360, fill=(195, 140, 45, 255), width=2) # bow arms
    draw.line([(23, 2), (23, 12)], fill=(225, 230, 235, 255), width=2) # loaded silver bolt!
    # Little crimson pennant flag
    draw.polygon([(40, 4), (47, 7), (40, 10)], fill=(210, 40, 40, 255))
    draw.line([(40, 3), (40, 16)], fill=(50, 50, 50, 255))
    return img

def create_corpse_lot():
    img = Image.new("RGBA", (96, 64), (0, 0, 0, 0))
    draw = ImageDraw.Draw(img)
    # Cobblestone barrier
    draw.rectangle([6, 8, 89, 57], fill=(85, 90, 95, 255))
    draw.rectangle([10, 12, 85, 53], fill=(130, 120, 110, 255))
    # White disinfectant lime powder circles
    draw.ellipse([16, 18, 44, 46], fill=(220, 225, 230, 255), outline=(180, 185, 190, 255))
    draw.ellipse([50, 22, 78, 48], fill=(220, 225, 230, 255), outline=(180, 185, 190, 255))
    # Medical cross / bio symbol on post
    draw.rectangle([4, 42, 10, 58], fill=(100, 60, 25, 255))
    draw.rectangle([2, 36, 12, 46], fill=(240, 240, 240, 255), outline=(60, 60, 60, 255))
    draw.rectangle([5, 38, 9, 44], fill=(215, 45, 45, 255))
    draw.rectangle([3, 40, 11, 42], fill=(215, 45, 45, 255))
    return img

create_fence_post().save(os.path.join(ENV_DIR, "fence_post.png"))
create_fence_h().save(os.path.join(ENV_DIR, "fence_h.png"))
create_feeder().save(os.path.join(ENV_DIR, "feeder_trough.png"))
create_water_trough().save(os.path.join(ENV_DIR, "water_trough.png"))
create_shelter_barn().save(os.path.join(ENV_DIR, "shelter_barn.png"))
create_defense_tower().save(os.path.join(ENV_DIR, "defense_tower.png"))
create_corpse_lot().save(os.path.join(ENV_DIR, "corpse_lot.png"))
print("Environment props saved successfully.")

# ==========================================
# 3. PLAYER (KHOA - THE PIG TYCOON)
# ==========================================
# Colors:
C_HAT_BODY = (244, 203, 102, 255)
C_HAT_RIM  = (212, 163, 55, 255)
C_HAT_BAND = (156, 111, 26, 255)
C_SKIN     = (252, 210, 162, 255)
C_SKIN_SH  = (224, 169, 118, 255)
C_EYES     = (35, 20, 15, 255)
C_SHIRT    = (200, 70, 70, 255) # Red flannel
C_DENIM    = (53, 101, 152, 255)
C_DENIM_SH = (36, 69, 105, 255)
C_BOOTS    = (74, 44, 22, 255)
C_BRASS    = (240, 220, 100, 255)

def draw_player_down(walk_frame=0):
    img = Image.new("RGBA", (32, 32), (0, 0, 0, 0))
    draw = ImageDraw.Draw(img)
    # Straw Hat (Down view: wide round brim, conical center)
    draw.ellipse([8, 2, 23, 12], fill=C_HAT_BODY, outline=C_HAT_RIM)
    draw.line([(10, 7), (21, 7)], fill=C_HAT_BAND)
    
    # Face peeking under hat
    draw.rectangle([11, 11, 20, 15], fill=C_SKIN)
    draw.point((13, 13), fill=C_EYES)
    draw.point((18, 13), fill=C_EYES)
    draw.point((15, 14), fill=C_SKIN_SH) # nose

    # Flannel Shirt sleeves
    draw.rectangle([9, 16, 22, 20], fill=C_SHIRT)
    
    # Denim Overalls Body
    draw.rectangle([11, 18, 20, 23], fill=C_DENIM)
    # Straps and brass buckles
    draw.line([(12, 16), (12, 20)], fill=C_DENIM_SH)
    draw.line([(19, 16), (19, 20)], fill=C_DENIM_SH)
    draw.point((12, 18), fill=C_BRASS)
    draw.point((19, 18), fill=C_BRASS)

    # Legs & Boots based on walk_frame
    if walk_frame == 0: # Idle
        draw.rectangle([11, 24, 14, 28], fill=C_DENIM)
        draw.rectangle([17, 24, 20, 28], fill=C_DENIM)
        draw.rectangle([11, 28, 14, 30], fill=C_BOOTS)
        draw.rectangle([17, 28, 20, 30], fill=C_BOOTS)
        # Arms resting at side
        draw.rectangle([8, 17, 10, 21], fill=C_SKIN)
        draw.rectangle([21, 17, 23, 21], fill=C_SKIN)
    elif walk_frame == 1: # Left leg forward, right arm swung
        draw.rectangle([11, 24, 14, 29], fill=C_DENIM)
        draw.rectangle([17, 23, 20, 27], fill=C_DENIM_SH)
        draw.rectangle([11, 29, 14, 31], fill=C_BOOTS)
        draw.rectangle([17, 27, 20, 29], fill=C_BOOTS)
        # Arms swinging
        draw.rectangle([8, 15, 10, 19], fill=C_SKIN)
        draw.rectangle([21, 18, 23, 22], fill=C_SKIN)
    elif walk_frame == 2: # Right leg forward, left arm swung
        draw.rectangle([11, 23, 14, 27], fill=C_DENIM_SH)
        draw.rectangle([17, 24, 20, 29], fill=C_DENIM)
        draw.rectangle([11, 27, 14, 29], fill=C_BOOTS)
        draw.rectangle([17, 29, 20, 31], fill=C_BOOTS)
        # Arms swinging
        draw.rectangle([8, 18, 10, 22], fill=C_SKIN)
        draw.rectangle([21, 15, 23, 19], fill=C_SKIN)
    return img

def draw_player_up(walk_frame=0):
    img = Image.new("RGBA", (32, 32), (0, 0, 0, 0))
    draw = ImageDraw.Draw(img)
    # Hat from back (covers face)
    draw.ellipse([8, 2, 23, 13], fill=C_HAT_BODY, outline=C_HAT_RIM)
    draw.line([(10, 8), (21, 8)], fill=C_HAT_BAND)
    
    # Back of red flannel shirt
    draw.rectangle([9, 14, 22, 20], fill=C_SHIRT)
    # Crossed denim straps
    draw.line([(11, 16), (20, 22)], fill=C_DENIM_SH)
    draw.line([(20, 16), (11, 22)], fill=C_DENIM_SH)
    # Denim pants back
    draw.rectangle([11, 21, 20, 24], fill=C_DENIM)
    
    if walk_frame == 0:
        draw.rectangle([11, 24, 14, 28], fill=C_DENIM)
        draw.rectangle([17, 24, 20, 28], fill=C_DENIM)
        draw.rectangle([11, 28, 14, 30], fill=C_BOOTS)
        draw.rectangle([17, 28, 20, 30], fill=C_BOOTS)
    elif walk_frame == 1:
        draw.rectangle([11, 24, 14, 29], fill=C_DENIM)
        draw.rectangle([17, 23, 20, 27], fill=C_DENIM)
        draw.rectangle([11, 29, 14, 31], fill=C_BOOTS)
        draw.rectangle([17, 27, 20, 29], fill=C_BOOTS)
    elif walk_frame == 2:
        draw.rectangle([11, 23, 14, 27], fill=C_DENIM)
        draw.rectangle([17, 24, 20, 29], fill=C_DENIM)
        draw.rectangle([11, 27, 14, 29], fill=C_BOOTS)
        draw.rectangle([17, 29, 20, 31], fill=C_BOOTS)
    return img

def draw_player_side(walk_frame=0):
    img = Image.new("RGBA", (32, 32), (0, 0, 0, 0))
    draw = ImageDraw.Draw(img)
    # Side hat brim sloped
    draw.polygon([(7, 8), (17, 3), (25, 9), (9, 11)], fill=C_HAT_BODY, outline=C_HAT_RIM)
    draw.line([(10, 8), (21, 8)], fill=C_HAT_BAND)
    
    # Side face & nose
    draw.rectangle([13, 11, 19, 15], fill=C_SKIN)
    draw.point((19, 13), fill=C_SKIN_SH) # nose tip
    draw.point((17, 12), fill=C_EYES)

    # Torso
    draw.rectangle([12, 16, 18, 21], fill=C_SHIRT)
    draw.rectangle([13, 19, 18, 24], fill=C_DENIM)
    # Strap
    draw.line([(14, 16), (14, 21)], fill=C_DENIM_SH)
    draw.point((14, 19), fill=C_BRASS)

    if walk_frame == 0: # Idle
        draw.rectangle([13, 24, 17, 28], fill=C_DENIM)
        draw.rectangle([13, 28, 18, 30], fill=C_BOOTS)
        draw.rectangle([14, 17, 16, 21], fill=C_SKIN)
    elif walk_frame == 1: # Stride forward
        draw.polygon([(15, 23), (19, 28), (21, 28), (17, 23)], fill=C_DENIM)
        draw.rectangle([19, 28, 23, 30], fill=C_BOOTS)
        draw.polygon([(14, 23), (11, 27), (9, 27), (12, 23)], fill=C_DENIM_SH)
        draw.rectangle([8, 27, 11, 29], fill=C_BOOTS)
    elif walk_frame == 2: # Stride backward
        draw.polygon([(14, 23), (10, 28), (8, 28), (12, 23)], fill=C_DENIM)
        draw.rectangle([7, 28, 11, 30], fill=C_BOOTS)
        draw.polygon([(15, 23), (18, 27), (20, 27), (17, 23)], fill=C_DENIM_SH)
        draw.rectangle([18, 27, 22, 29], fill=C_BOOTS)
    return img

def draw_player_action():
    img = draw_player_down(0)
    draw = ImageDraw.Draw(img)
    # Hands outstretched holding golden feed / love hearts
    draw.rectangle([6, 15, 10, 18], fill=C_SKIN)
    draw.rectangle([21, 15, 25, 18], fill=C_SKIN)
    # Golden grain scatter sparkles
    draw.point((5, 13), fill=(255, 225, 75, 255))
    draw.point((26, 13), fill=(255, 225, 75, 255))
    draw.point((16, 10), fill=(255, 245, 140, 255))
    # Cute little heart icon above
    draw.rectangle([14, 0, 15, 1], fill=(255, 90, 120, 255))
    draw.rectangle([17, 0, 18, 1], fill=(255, 90, 120, 255))
    draw.rectangle([14, 2, 18, 3], fill=(255, 90, 120, 255))
    draw.point((16, 4), fill=(255, 90, 120, 255))
    return img

# Save player sprites
draw_player_down(0).save(os.path.join(CHARS_DIR, "player_down_idle.png"))
draw_player_down(1).save(os.path.join(CHARS_DIR, "player_down_walk1.png"))
draw_player_down(2).save(os.path.join(CHARS_DIR, "player_down_walk2.png"))
draw_player_up(0).save(os.path.join(CHARS_DIR, "player_up_idle.png"))
draw_player_up(1).save(os.path.join(CHARS_DIR, "player_up_walk1.png"))
draw_player_up(2).save(os.path.join(CHARS_DIR, "player_up_walk2.png"))
draw_player_side(0).save(os.path.join(CHARS_DIR, "player_side_idle.png"))
draw_player_side(1).save(os.path.join(CHARS_DIR, "player_side_walk1.png"))
draw_player_side(2).save(os.path.join(CHARS_DIR, "player_side_walk2.png"))
draw_player_action().save(os.path.join(CHARS_DIR, "player_action.png"))
print("Player sprites saved successfully.")

# ==========================================
# 4. PIG SPRITES (4 GIỐNG x 5 ANIMATION FRAMES)
# ==========================================
BREEDS = {
    "hong_dien": {
        "body": (255, 175, 189, 255),
        "shadow": (223, 128, 146, 255),
        "highlight": (255, 207, 216, 255),
        "snout": (255, 112, 142, 255),
        "hooves": (191, 86, 110, 255),
        "special": None
    },
    "lam_khe": {
        "body": (126, 196, 207, 255),
        "shadow": (79, 143, 155, 255),
        "highlight": (174, 230, 238, 255),
        "snout": (62, 120, 131, 255),
        "hooves": (44, 90, 99, 255),
        "special": "mud_drops"
    },
    "kim_tho": {
        "body": (246, 196, 69, 255),
        "shadow": (201, 151, 28, 255),
        "highlight": (255, 226, 120, 255),
        "snout": (184, 131, 8, 255),
        "hooves": (138, 95, 0, 255),
        "special": "gold_gleam"
    },
    "hu_the": {
        "body": (155, 105, 184, 255),
        "shadow": (109, 62, 132, 255),
        "highlight": (199, 159, 224, 255),
        "snout": (91, 47, 112, 255),
        "hooves": (62, 29, 80, 255),
        "special": "void_glow"
    }
}

def draw_pig(breed_key, anim="idle"):
    b = BREEDS[breed_key]
    img = Image.new("RGBA", (32, 32), (0, 0, 0, 0))
    draw = ImageDraw.Draw(img)

    # 1. Curly Tail at back
    if anim != "sleep":
        draw.line([(5, 16), (2, 14)], fill=b["shadow"], width=2)
        draw.point((3, 12), fill=b["snout"])

    # 2. Trotters / Legs
    if anim == "idle":
        draw.rectangle([7, 23, 9, 28], fill=b["shadow"])
        draw.rectangle([13, 23, 15, 28], fill=b["shadow"])
        draw.rectangle([18, 23, 20, 28], fill=b["body"])
        draw.rectangle([24, 23, 26, 28], fill=b["body"])
        draw.rectangle([7, 27, 9, 28], fill=b["hooves"])
        draw.rectangle([13, 27, 15, 28], fill=b["hooves"])
        draw.rectangle([18, 27, 20, 28], fill=b["hooves"])
        draw.rectangle([24, 27, 26, 28], fill=b["hooves"])
    elif anim == "walk1":
        # Front-right forward, rear-left back
        draw.rectangle([6, 22, 8, 27], fill=b["shadow"])
        draw.rectangle([12, 23, 14, 28], fill=b["shadow"])
        draw.rectangle([19, 23, 21, 28], fill=b["body"])
        draw.rectangle([26, 21, 28, 26], fill=b["body"])
        draw.rectangle([6, 26, 8, 27], fill=b["hooves"])
        draw.rectangle([12, 27, 14, 28], fill=b["hooves"])
        draw.rectangle([19, 27, 21, 28], fill=b["hooves"])
        draw.rectangle([26, 25, 28, 26], fill=b["hooves"])
    elif anim == "walk2":
        # Front-left forward, rear-right back
        draw.rectangle([8, 23, 10, 28], fill=b["shadow"])
        draw.rectangle([14, 21, 16, 26], fill=b["shadow"])
        draw.rectangle([17, 21, 19, 26], fill=b["body"])
        draw.rectangle([23, 23, 25, 28], fill=b["body"])
        draw.rectangle([8, 27, 10, 28], fill=b["hooves"])
        draw.rectangle([14, 25, 16, 26], fill=b["hooves"])
        draw.rectangle([17, 25, 19, 26], fill=b["hooves"])
        draw.rectangle([23, 27, 25, 28], fill=b["hooves"])
    elif anim == "sleep":
        # Tucked legs
        draw.rectangle([7, 25, 11, 27], fill=b["hooves"])
        draw.rectangle([21, 25, 25, 27], fill=b["hooves"])
    elif anim == "eat":
        draw.rectangle([7, 24, 9, 28], fill=b["shadow"])
        draw.rectangle([13, 24, 15, 28], fill=b["shadow"])
        draw.rectangle([18, 24, 20, 28], fill=b["body"])
        draw.rectangle([24, 24, 26, 28], fill=b["body"])
        draw.rectangle([7, 27, 9, 28], fill=b["hooves"])
        draw.rectangle([13, 27, 15, 28], fill=b["hooves"])
        draw.rectangle([18, 27, 20, 28], fill=b["hooves"])
        draw.rectangle([24, 27, 26, 28], fill=b["hooves"])

    # 3. Chubby Round Body
    if anim != "sleep":
        draw.ellipse([5, 11, 27, 24], fill=b["body"], outline=b["shadow"])
        draw.line([(9, 13), (23, 13)], fill=b["highlight"])
    else: # Flatter cozy sleep oval
        draw.ellipse([5, 14, 27, 26], fill=b["body"], outline=b["shadow"])
        draw.line([(9, 16), (23, 16)], fill=b["highlight"])

    # Breed specials
    if b["special"] == "mud_drops":
        draw.point((10, 16), fill=(84, 56, 30, 255))
        draw.point((11, 16), fill=(84, 56, 30, 255))
        draw.point((16, 18), fill=(84, 56, 30, 255))
    elif b["special"] == "gold_gleam":
        draw.point((14, 14), fill=(255, 255, 220, 255))
        draw.point((15, 15), fill=(255, 240, 140, 255))
    elif b["special"] == "void_glow":
        draw.point((12, 17), fill=(225, 110, 255, 255))
        draw.point((18, 15), fill=(225, 110, 255, 255))

    # 4. Pig Head & Snout
    if anim == "sleep":
        # Sleep head lowered
        draw.ellipse([18, 14, 28, 23], fill=b["body"], outline=b["shadow"])
        # Closed happy eye ^^
        draw.line([(22, 17), (23, 16)], fill=b["shadow"])
        draw.line([(24, 16), (25, 17)], fill=b["shadow"])
        # Snout
        draw.rectangle([26, 18, 30, 22], fill=b["snout"])
        draw.point((28, 20), fill=b["hooves"])
        # Floppy ear resting
        draw.polygon([(19, 14), (23, 13), (21, 18)], fill=b["snout"])
        # Zzz
        draw.line([(27, 6), (30, 6)], fill=(120, 160, 240, 255))
        draw.line([(30, 6), (27, 9)], fill=(120, 160, 240, 255))
        draw.line([(27, 9), (30, 9)], fill=(120, 160, 240, 255))
    elif anim == "eat":
        # Head angled down
        draw.ellipse([18, 15, 29, 25], fill=b["body"], outline=b["shadow"])
        # Ear flopped forward
        draw.polygon([(19, 14), (24, 13), (22, 19)], fill=b["snout"])
        # Eye focused on food
        draw.point((23, 18), fill=(35, 20, 20, 255))
        # Snout right at ground level
        draw.rectangle([26, 21, 31, 26], fill=b["snout"])
        draw.point((28, 23), fill=b["hooves"])
        draw.point((28, 25), fill=b["hooves"])
        # Food crumbs
        draw.point((29, 28), fill=(245, 205, 65, 255))
        draw.point((31, 27), fill=(255, 225, 120, 255))
    else: # Idle / Walk
        # Head perky
        draw.ellipse([18, 10, 29, 21], fill=b["body"], outline=b["shadow"])
        # Perky ear
        draw.polygon([(19, 8), (23, 6), (22, 12)], fill=b["snout"])
        # Eye
        if b["special"] == "void_glow":
            draw.point((23, 14), fill=(225, 110, 255, 255))
        else:
            draw.point((23, 14), fill=(35, 20, 20, 255))
        # Button Snout
        draw.rectangle([27, 13, 31, 18], fill=b["snout"])
        draw.point((29, 15), fill=b["hooves"])
        draw.point((29, 17), fill=b["hooves"])

    return img

for breed in BREEDS.keys():
    b_dir = os.path.join(PIGS_DIR, breed)
    os.makedirs(b_dir, exist_ok=True)
    draw_pig(breed, "idle").save(os.path.join(b_dir, "idle.png"))
    draw_pig(breed, "walk1").save(os.path.join(b_dir, "walk1.png"))
    draw_pig(breed, "walk2").save(os.path.join(b_dir, "walk2.png"))
    draw_pig(breed, "eat").save(os.path.join(b_dir, "eat.png"))
    draw_pig(breed, "sleep").save(os.path.join(b_dir, "sleep.png"))

print("All pig sprites generated successfully!")
