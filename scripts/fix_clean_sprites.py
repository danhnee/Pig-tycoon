import os, glob
from PIL import Image
from collections import deque

SOURCE_IMAGE_PATH = "/home/danh/.gemini/antigravity/brain/e605a811-d043-4617-bbe7-f0f18bf72b28/.user_uploaded/media_1791560585771.jpg"
OUT_DIR = "Assets/Art/Sprites/Characters"
os.makedirs(OUT_DIR, exist_ok=True)

# 1. Clean background via flood fill from image borders ONLY
def clean_bg(crop_img, thresh=238):
    img = crop_img.convert('RGBA')
    w, h = img.size
    pix = img.load()
    visited = [[False]*h for _ in range(w)]
    q = deque()
    
    for x in range(w):
        for y in (0, h-1):
            if (pix[x,y][0] + pix[x,y][1] + pix[x,y][2]) / 3 >= thresh:
                visited[x][y] = True
                q.append((x, y))
    for y in range(h):
        for x in (0, w-1):
            if not visited[x][y] and (pix[x,y][0] + pix[x,y][1] + pix[x,y][2]) / 3 >= thresh:
                visited[x][y] = True
                q.append((x, y))
                
    while q:
        cx, cy = q.popleft()
        pix[cx, cy] = (0, 0, 0, 0)
        for dx, dy in [(-1,0), (1,0), (0,-1), (0,1)]:
            nx, ny = cx + dx, cy + dy
            if 0 <= nx < w and 0 <= ny < h and not visited[nx][ny]:
                if (pix[nx,ny][0] + pix[nx,ny][1] + pix[nx,ny][2]) / 3 >= thresh:
                    visited[nx][ny] = True
                    q.append((nx, ny))
                    
    # Clean compression fringe directly adjacent to transparent
    for x in range(w):
        for y in range(h):
            if pix[x, y][3] > 0:
                is_edge = any(0 <= x+dx < w and 0 <= y+dy < h and pix[x+dx, y+dy][3] == 0 for dx, dy in [(-1,0),(1,0),(0,-1),(0,1)])
                if is_edge:
                    br = (pix[x,y][0] + pix[x,y][1] + pix[x,y][2]) / 3.0
                    if br >= 232:
                        pix[x, y] = (0, 0, 0, 0)
    return img

src = Image.open(SOURCE_IMAGE_PATH).convert('RGB')

# Row band coordinates: exact row band height = 99px
scale = 54.0 / 99.0

def process_sprite(crop_box):
    crop = src.crop(crop_box)
    cleaned = clean_bg(crop)
    sw = max(1, int(cleaned.width * scale))
    sh = max(1, int(cleaned.height * scale))
    scaled = cleaned.resize((sw, sh), Image.LANCZOS)
    
    canvas = Image.new('RGBA', (64, 64), (0, 0, 0, 0))
    top_y = 6
    left_x = (64 - sw) // 2
    canvas.paste(scaled, (left_x, top_y), scaled)
    return canvas

# Definitions: (y0, y1, [c0, c1, c2, c3_bp, c4_bp, c5_bp])
bands = {
    'down': (533, 632, [(119, 157), (187, 228), (259, 297), (415, 453), (484, 524), (555, 595)]),
    'up':   (648, 747, [(121, 159), (187, 227), (260, 298), (415, 453), (484, 524), (557, 595)]),
    'side': (762, 861, [(115, 165), (188, 225), (256, 305), (411, 461), (484, 528), (552, 600)])
}

for dir_name, (y0, y1, cols) in bands.items():
    # Base (no backpack)
    c0 = process_sprite((cols[0][0], y0, cols[0][1], y1))
    c1 = process_sprite((cols[1][0], y0, cols[1][1], y1))
    c2 = process_sprite((cols[2][0], y0, cols[2][1], y1))
    
    # Save Idle frames (Pure authentic c1 stand pose!)
    c1.save(os.path.join(OUT_DIR, f"player_{dir_name}_idle.png"))
    for i in range(1, 5):
        c1.save(os.path.join(OUT_DIR, f"player_{dir_name}_idle_{i}.png"))
        
    # Save Walk frames (c0 -> c1 -> c2 -> c1)
    c0.save(os.path.join(OUT_DIR, f"player_{dir_name}_walk1.png"))
    c1.save(os.path.join(OUT_DIR, f"player_{dir_name}_walk2.png"))
    c2.save(os.path.join(OUT_DIR, f"player_{dir_name}_walk3.png"))
    c1.save(os.path.join(OUT_DIR, f"player_{dir_name}_walk4.png"))
    
    # Backpack variants
    bp_c0 = process_sprite((cols[3][0], y0, cols[3][1], y1))
    bp_c1 = process_sprite((cols[4][0], y0, cols[4][1], y1))
    bp_c2 = process_sprite((cols[5][0], y0, cols[5][1], y1))
    
    for i in range(1, 5):
        bp_c1.save(os.path.join(OUT_DIR, f"player_backpack_{dir_name}_idle_{i}.png"))
        
    bp_c0.save(os.path.join(OUT_DIR, f"player_backpack_{dir_name}_walk1.png"))
    bp_c1.save(os.path.join(OUT_DIR, f"player_backpack_{dir_name}_walk2.png"))
    bp_c2.save(os.path.join(OUT_DIR, f"player_backpack_{dir_name}_walk3.png"))
    bp_c1.save(os.path.join(OUT_DIR, f"player_backpack_{dir_name}_walk4.png"))

# Action sprite: clean down stand
down_stand = process_sprite((bands['down'][2][1][0], bands['down'][0], bands['down'][2][1][1], bands['down'][1]))
down_stand.save(os.path.join(OUT_DIR, "player_action.png"))

# Happy emote frames: subtle 1-2 pixel joyful bounce of authentic sprite
for i in range(4):
    hop = [0, 2, 2, 0][i]
    h_canvas = Image.new('RGBA', (64, 64), (0, 0, 0, 0))
    p_stand = down_stand.load()
    p_h = h_canvas.load()
    for x in range(64):
        for y in range(64):
            if y - hop >= 0 and y < 64:
                p_h[x, y - hop] = p_stand[x, y]
    h_canvas.save(os.path.join(OUT_DIR, f"player_happy_{i+1}.png"))

print("All clean idle, walk, and emote sprites generated successfully!")

# Delete all 27 attack animation files and their .meta files
attack_patterns = ['player_slash_*', 'player_smash_*', 'player_thrust_*']
deleted_count = 0
for pat in attack_patterns:
    for f in glob.glob(os.path.join(OUT_DIR, pat)):
        os.remove(f)
        deleted_count += 1
print(f"Deleted {deleted_count} attack animation sprites and meta files.")
