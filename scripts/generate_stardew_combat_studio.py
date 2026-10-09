import os
import base64
from PIL import Image

ARTIFACT_DIR = "/home/danh/.gemini/antigravity/brain/e605a811-d043-4617-bbe7-f0f18bf72b28"
CHAR_DIR = "Assets/Art/Sprites/Characters"
WEAPON_DIR = "Assets/Art/Sprites/Weapons"
VFX_DIR = "Assets/Art/Sprites/VFX"

WEAPONS = {
    'kiem_sat': ('Kiếm Sắt (Sword)', f'{WEAPON_DIR}/weapon_kiem_sat.png', 0, 15, 25),
    'dao_go': ('Đao Gỗ (Dagger)', f'{WEAPON_DIR}/weapon_dao_go.png', 2, 8, 14),
    'giao_tre': ('Giáo Tre (Spear)', f'{WEAPON_DIR}/weapon_giao_tre.png', 1, 12, 20),
    'bua_ta': ('Búa Tạ (Hammer)', f'{WEAPON_DIR}/weapon_bua_ta.png', -8, 25, 45)
}

SOCKETS = {
    "slash_down": [
        (33, 21, 60, -1),
        (37, 42, -145, 1),
        (40, 42, -90, 1)
    ],
    "slash_up": [
        (20, 43, -90, -1),
        (24, 22, 90, -1),
        (40, 44, -90, -1)
    ],
    "slash_right": [
        (16, 26, 170, -1),
        (49, 29, 15, 1),
        (37, 27, -45, 1)
    ],
    "slash_left": [
        (47, 26, 10, -1),
        (14, 29, 165, 1),
        (26, 27, -135, 1)
    ],
    "thrust_down": [
        (30, 25, -90, 1),
        (31, 36, -90, 1),
        (30, 30, -90, 1)
    ],
    "thrust_up": [
        (30, 34, 90, -1),
        (33, 24, 90, -1),
        (32, 32, 90, -1)
    ],
    "thrust_right": [
        (34, 28, 0, 1),
        (43, 32, 0, 1),
        (34, 28, 0, 1)
    ],
    "thrust_left": [
        (30, 28, 180, 1),
        (20, 32, 180, 1),
        (29, 28, 180, 1)
    ]
}

def render_composite(char_img, weapon_img, socket_x, socket_y, angle_deg, sort_order):
    HANDLE_X = 11.5
    HANDLE_Y = 20.5
    rot_angle = angle_deg - 90
    
    pad = 32
    big_w = 64 + pad * 2
    temp = Image.new('RGBA', (big_w, big_w), (0, 0, 0, 0))
    paste_x = big_w // 2 - int(HANDLE_X)
    paste_y = big_w // 2 - int(HANDLE_Y)
    temp.paste(weapon_img, (paste_x, paste_y), weapon_img)
    
    rotated = temp.rotate(rot_angle, resample=Image.NEAREST)
    
    out = Image.new('RGBA', (64, 64), (0, 0, 0, 0))
    dest_x = int(socket_x) - big_w // 2
    dest_y = int(socket_y) - big_w // 2
    
    if sort_order == -1:
        out.paste(rotated, (dest_x, dest_y), rotated)
        out.paste(char_img, (0, 0), char_img)
    else:
        out.paste(char_img, (0, 0), char_img)
        out.paste(rotated, (dest_x, dest_y), rotated)
    return out

# 1. GENERATE 4 HIGH-RES RENDER GRIDS (3x SCALE)
scale = 3
cell_size = 64 * scale
padding = 10
grid_w = cell_size * 3 + padding * 4
grid_h = cell_size * 8 + padding * 9

for w_key, (w_name, w_path, spd, min_dmg, max_dmg) in WEAPONS.items():
    w_img = Image.open(w_path).convert('RGBA')
    grid = Image.new('RGBA', (grid_w, grid_h), (15, 23, 42, 255))
    
    for r_idx, (action_key, frames_data) in enumerate(SOCKETS.items()):
        y_pos = padding + r_idx * (cell_size + padding)
        for f_idx in range(3):
            x_pos = padding + f_idx * (cell_size + padding)
            char_path = f'{CHAR_DIR}/player_{action_key}_{f_idx + 1}.png'
            if os.path.exists(char_path):
                char_img = Image.open(char_path).convert('RGBA')
                sx, sy, ang, srt = frames_data[f_idx]
                comp = render_composite(char_img, w_img, sx, sy, ang, srt)
                comp_scaled = comp.resize((cell_size, cell_size), Image.NEAREST)
                grid.paste(comp_scaled, (x_pos, y_pos), comp_scaled)
                
    out_path = f"{ARTIFACT_DIR}/stardew_combat_grid_{w_key}.png"
    grid.save(out_path)
    print(f"Saved render grid: {out_path}")

# 2. GENERATE STANDALONE HTML STUDIO
def to_b64(path):
    if os.path.exists(path):
        with open(path, "rb") as f:
            return "data:image/png;base64," + base64.b64encode(f.read()).decode("utf-8")
    return ""

char_b64 = {}
for action in SOCKETS.keys():
    for f in range(1, 4):
        k = f"{action}_{f}"
        char_b64[k] = to_b64(f"{CHAR_DIR}/player_{action}_{f}.png")

weapon_b64 = {k: to_b64(v[1]) for k, v in WEAPONS.items()}

vfx_b64 = {}
for act in ["slash", "thrust"]:
    for d in ["down", "up", "right", "left"]:
        k = f"{act}_{d}"
        vfx_b64[k] = to_b64(f"{VFX_DIR}/vfx_trail_{act}_{d}.png")

import json
char_json = json.dumps(char_b64)
weapon_json = json.dumps(weapon_b64)
vfx_json = json.dumps(vfx_b64)
socket_json = json.dumps(SOCKETS)

html_content = f"""<!DOCTYPE html>
<html lang="vi">
<head>
<meta charset="utf-8">
<title>Farmer Khoa - Stardew Valley Combat Studio 64x64</title>
<style>
  :root {{
    --bg-dark: #090d16;
    --panel-bg: #111827;
    --card-bg: #1f2937;
    --accent: #10b981;
    --accent-glow: rgba(16, 185, 129, 0.4);
    --border: #374151;
    --text-main: #f9fafb;
    --text-muted: #9ca3af;
  }}
  * {{ box-sizing: border-box; margin: 0; padding: 0; font-family: -apple-system, BlinkMacSystemFont, "Segoe UI", Roboto, sans-serif; }}
  body {{ background: var(--bg-dark); color: var(--text-main); padding: 24px; min-height: 100vh; }}
  .header {{ display: flex; justify-content: space-between; align-items: center; border-bottom: 1px solid var(--border); padding-bottom: 16px; margin-bottom: 20px; }}
  .header h1 {{ font-size: 20px; color: #38bdf8; display: flex; align-items: center; gap: 8px; }}
  .badges {{ display: flex; gap: 8px; }}
  .badge {{ background: #0284c7; color: #fff; padding: 4px 10px; border-radius: 999px; font-size: 12px; font-weight: 600; }}
  .badge-green {{ background: #059669; }}
  .badge-purple {{ background: #7c3aed; }}

  .layout {{ display: grid; grid-template-columns: 340px 1fr; gap: 24px; }}
  .sidebar {{ background: var(--panel-bg); border: 1px solid var(--border); border-radius: 12px; padding: 18px; display: flex; flex-direction: column; gap: 16px; }}
  .sec-title {{ font-size: 11px; text-transform: uppercase; font-weight: 700; color: var(--text-muted); letter-spacing: 0.05em; }}
  .btn-grid {{ display: grid; grid-template-columns: repeat(2, 1fr); gap: 6px; }}
  button {{
    background: var(--card-bg); border: 1px solid var(--border); color: var(--text-main); padding: 8px 12px;
    border-radius: 6px; font-size: 13px; font-weight: 600; cursor: pointer; transition: 0.15s;
  }}
  button:hover {{ background: #374151; border-color: #60a5fa; }}
  button.active {{ background: #2563eb; border-color: #3b82f6; color: #fff; box-shadow: 0 0 10px rgba(59, 130, 246, 0.5); }}

  .canvas-stage {{
    background: var(--panel-bg); border: 1px solid var(--border); border-radius: 12px; padding: 24px;
    display: flex; flex-direction: column; align-items: center; justify-content: center; gap: 20px;
  }}
  .preview-wrapper {{
    position: relative; width: 256px; height: 256px; border: 2px dashed #4b5563; border-radius: 8px;
    background: #000; display: flex; align-items: center; justify-content: center;
    background-image: linear-gradient(45deg, #1e293b 25%, transparent 25%), linear-gradient(-45deg, #1e293b 25%, transparent 25%), linear-gradient(45deg, transparent 75%, #1e293b 75%), linear-gradient(-45deg, transparent 75%, #1e293b 75%);
    background-size: 16px 16px; background-position: 0 0, 0 8px, 8px -8px, -8px 0px;
  }}
  canvas {{ width: 256px; height: 256px; image-rendering: pixelated; }}
  .telemetry {{
    display: grid; grid-template-columns: repeat(4, 1fr); gap: 12px; width: 100%; max-width: 600px;
  }}
  .tele-card {{
    background: var(--card-bg); border: 1px solid var(--border); border-radius: 8px; padding: 10px; text-align: center;
  }}
  .tele-card .val {{ font-size: 15px; font-weight: 700; color: #38bdf8; margin-top: 4px; }}
  .tele-card .lbl {{ font-size: 11px; color: var(--text-muted); }}

  .filmstrip {{
    display: flex; gap: 12px; justify-content: center; width: 100%;
  }}
  .strip-frame {{
    width: 96px; height: 96px; border: 2px solid var(--border); border-radius: 8px; background: #000;
    cursor: pointer; display: flex; align-items: center; justify-content: center; transition: 0.15s;
  }}
  .strip-frame.active {{ border-color: #38bdf8; box-shadow: 0 0 12px rgba(56, 189, 248, 0.5); }}
  .strip-frame canvas {{ width: 96px; height: 96px; }}

  .toggle-item {{ display: flex; align-items: center; gap: 8px; font-size: 13px; cursor: pointer; }}
</style>
</head>
<body>

<div class="header">
  <h1>⚔️ Farmer Khoa - Stardew Valley Combat Studio</h1>
  <div class="badges">
    <span class="badge badge-green">Stardew Decoupled Layering</span>
    <span class="badge">Pixel-Perfect 64x64</span>
    <span class="badge badge-purple">Hilt Pivot (11.5, 3.5)</span>
  </div>
</div>

<div class="layout">
  <div class="sidebar">
    <div class="sec-title">Hành Động (Action)</div>
    <div class="btn-grid">
      <button class="active" id="btnSlash" onclick="setAction('slash')">🗡️ Vung Chém (Slash)</button>
      <button id="btnThrust" onclick="setAction('thrust')">🔱 Đâm Thẳng (Thrust)</button>
    </div>

    <div class="sec-title">Hướng Tấn Công (Direction)</div>
    <div class="btn-grid">
      <button class="active" id="btnDown" onclick="setDir('down')">⬇️ Xuống (Down)</button>
      <button id="btnUp" onclick="setDir('up')">⬆️ Lên (Up)</button>
      <button id="btnRight" onclick="setDir('right')">➡️ Phải (Right)</button>
      <button id="btnLeft" onclick="setDir('left')">⬅️ Trái (Left)</button>
    </div>

    <div class="sec-title">Vũ Khí (Weapon Archetype)</div>
    <div class="btn-grid">
      <button class="active" id="btnKiemSat" onclick="setWeapon('kiem_sat')">Kiếm Sắt (Sword)</button>
      <button id="btnDaoGo" onclick="setWeapon('dao_go')">Đao Gỗ (Dagger)</button>
      <button id="btnGiaoTre" onclick="setWeapon('giao_tre')">Giáo Tre (Spear)</button>
      <button id="btnBuaTa" onclick="setWeapon('bua_ta')">Búa Tạ (Hammer)</button>
    </div>

    <div class="sec-title">Lớp Hiển Thị (Display Layers)</div>
    <label class="toggle-item"><input type="checkbox" id="chkBody" checked onchange="draw()"> 👤 Thân Khoa (Grip Pose)</label>
    <label class="toggle-item"><input type="checkbox" id="chkWeapon" checked onchange="draw()"> 🗡️ Vũ Khí Tại Chuôi Tay</label>
    <label class="toggle-item"><input type="checkbox" id="chkVfx" checked onchange="draw()"> ✨ Dải VFX Trails</label>
    <label class="toggle-item"><input type="checkbox" id="chkSocket" checked onchange="draw()"> 🔴 Hand Socket Pivot</label>
  </div>

  <div class="canvas-stage">
    <div class="preview-wrapper">
      <canvas id="mainCanvas" width="64" height="64"></canvas>
    </div>

    <div class="telemetry">
      <div class="tele-card"><div class="lbl">FRAME</div><div class="val" id="valFrame">1 / 3</div></div>
      <div class="tele-card"><div class="lbl">HAND PIVOT</div><div class="val" id="valSocket">(33, 21)</div></div>
      <div class="tele-card"><div class="lbl">WEAPON ANGLE</div><div class="val" id="valAngle">60°</div></div>
      <div class="tele-card"><div class="lbl">SORTING DEPTH</div><div class="val" id="valSort">Behind (-1)</div></div>
    </div>

    <div class="filmstrip">
      <div class="strip-frame active" id="f0" onclick="setFrame(0)"><canvas id="c0" width="64" height="64"></canvas></div>
      <div class="strip-frame" id="f1" onclick="setFrame(1)"><canvas id="c1" width="64" height="64"></canvas></div>
      <div class="strip-frame" id="f2" onclick="setFrame(2)"><canvas id="c2" width="64" height="64"></canvas></div>
    </div>

    <div style="display:flex; gap:10px;">
      <button onclick="prevFrame()">◀ Frame Trước</button>
      <button id="btnPlay" onclick="togglePlay()">▶ Auto Play (Stardew Speed)</button>
      <button onclick="nextFrame()">Frame Kế ▶</button>
    </div>
  </div>
</div>

<script>
const CHARS = {char_json};
const WEAPONS = {weapon_json};
const VFX = {vfx_json};
const SOCKETS = {socket_json};

let currentAction = 'slash';
let currentDir = 'down';
let currentWeapon = 'kiem_sat';
let currentFrame = 0;
let isPlaying = false;
let playInterval = null;

const imgCache = {{}};
function getImg(src) {{
  if (!src) return null;
  if (!imgCache[src]) {{
    const img = new Image();
    img.src = src;
    imgCache[src] = img;
  }}
  return imgCache[src];
}}

function drawScene(ctx, frameIdx, drawMarker = true) {{
  ctx.clearRect(0, 0, 64, 64);
  const actionKey = `${{currentAction}}_${{currentDir}}`;
  const socketData = SOCKETS[actionKey] ? SOCKETS[actionKey][frameIdx] : null;

  const charKey = `${{actionKey}}_${{frameIdx + 1}}`;
  const charImg = getImg(CHARS[charKey]);
  const weaponImg = getImg(WEAPONS[currentWeapon]);
  const vfxKey = `${{currentAction}}_${{currentDir}}`;
  const vfxImg = getImg(VFX[vfxKey]);

  const showBody = document.getElementById('chkBody').checked;
  const showWeapon = document.getElementById('chkWeapon').checked;
  const showVfx = document.getElementById('chkVfx').checked;
  const showSocket = document.getElementById('chkSocket').checked;

  if (socketData && showWeapon && weaponImg && socketData[3] === -1) {{
    renderRotatedWeapon(ctx, weaponImg, socketData[0], socketData[1], socketData[2]);
  }}

  if (showBody && charImg && charImg.complete) {{
    ctx.drawImage(charImg, 0, 0, 64, 64);
  }}

  if (socketData && showWeapon && weaponImg && socketData[3] === 1) {{
    renderRotatedWeapon(ctx, weaponImg, socketData[0], socketData[1], socketData[2]);
  }}

  if (showVfx && frameIdx === 1 && vfxImg && vfxImg.complete) {{
    ctx.save();
    ctx.globalAlpha = 0.9;
    ctx.drawImage(vfxImg, 0, 0, 64, 64);
    ctx.restore();
  }}

  if (showSocket && socketData && drawMarker) {{
    ctx.fillStyle = '#ff0055';
    ctx.fillRect(socketData[0] - 0.5, socketData[1] - 0.5, 2, 2);
    ctx.strokeStyle = '#38bdf8';
    ctx.beginPath();
    ctx.arc(socketData[0], socketData[1], 3, 0, Math.PI * 2);
    ctx.stroke();
  }}
}}

function renderRotatedWeapon(ctx, img, px, py, angleDeg) {{
  ctx.save();
  ctx.translate(px, py);
  const rad = (90 - angleDeg) * Math.PI / 180;
  ctx.rotate(rad);
  ctx.drawImage(img, -11.5, -20.5, 24, 24);
  ctx.restore();
}}

function draw() {{
  const mainCtx = document.getElementById('mainCanvas').getContext('2d');
  drawScene(mainCtx, currentFrame, true);

  for (let i = 0; i < 3; i++) {{
    const stripCtx = document.getElementById('c' + i).getContext('2d');
    drawScene(stripCtx, i, false);
    const fCard = document.getElementById('f' + i);
    if (i === currentFrame) fCard.classList.add('active');
    else fCard.classList.remove('active');
  }}

  const actionKey = `${{currentAction}}_${{currentDir}}`;
  const s = SOCKETS[actionKey] ? SOCKETS[actionKey][currentFrame] : null;
  document.getElementById('valFrame').innerText = `${{currentFrame + 1}} / 3`;
  if (s) {{
    document.getElementById('valSocket').innerText = `(${{s[0]}}, ${{s[1]}})`;
    document.getElementById('valAngle').innerText = `${{s[2]}}°`;
    document.getElementById('valSort').innerText = s[3] === -1 ? 'Behind (-1)' : 'Front (+1)';
  }}
}}

function setAction(act) {{
  currentAction = act;
  document.getElementById('btnSlash').classList.toggle('active', act === 'slash');
  document.getElementById('btnThrust').classList.toggle('active', act === 'thrust');
  draw();
}}

function setDir(d) {{
  currentDir = d;
  ['down', 'up', 'right', 'left'].forEach(item => {{
    document.getElementById('btn' + item.charAt(0).toUpperCase() + item.slice(1)).classList.toggle('active', item === d);
  }});
  draw();
}}

function setWeapon(w) {{
  currentWeapon = w;
  ['kiem_sat', 'dao_go', 'giao_tre', 'bua_ta'].forEach(item => {{
    const id = 'btn' + item.split('_').map(x => x.charAt(0).toUpperCase() + x.slice(1)).join('');
    const btn = document.getElementById(id);
    if (btn) btn.classList.toggle('active', item === w);
  }});
  draw();
}}

function setFrame(f) {{
  currentFrame = f;
  draw();
}}
function prevFrame() {{ currentFrame = (currentFrame + 2) % 3; draw(); }}
function nextFrame() {{ currentFrame = (currentFrame + 1) % 3; draw(); }}

function togglePlay() {{
  isPlaying = !isPlaying;
  document.getElementById('btnPlay').innerText = isPlaying ? '⏸ Tạm Dừng' : '▶ Auto Play (Stardew Speed)';
  if (isPlaying) {{
    playInterval = setInterval(() => {{ nextFrame(); }}, 133);
  }} else {{
    clearInterval(playInterval);
  }}
}}

window.onload = () => {{ setTimeout(draw, 100); }};
</script>
</body>
</html>
"""

html_path = f"{ARTIFACT_DIR}/stardew_combat_studio.html"
with open(html_path, "w", encoding="utf-8") as f:
    f.write(html_content)
print(f"Saved interactive combat studio: {html_path}")
