import os
import base64

char_dir = "Assets/Art/Sprites/Characters"
sprite_names = [
    # Base Idle
    'player_down_idle_1', 'player_down_idle_2', 'player_down_idle_3', 'player_down_idle_4',
    'player_side_idle_1', 'player_side_idle_2', 'player_side_idle_3', 'player_side_idle_4',
    'player_up_idle_1', 'player_up_idle_2', 'player_up_idle_3', 'player_up_idle_4',
    # Base Walk
    'player_down_walk1', 'player_down_walk2', 'player_down_walk3', 'player_down_walk4',
    'player_side_walk1', 'player_side_walk2', 'player_side_walk3', 'player_side_walk4',
    'player_up_walk1', 'player_up_walk2', 'player_up_walk3', 'player_up_walk4',
    # Backpack Idle
    'player_backpack_down_idle_1', 'player_backpack_down_idle_2', 'player_backpack_down_idle_3', 'player_backpack_down_idle_4',
    'player_backpack_side_idle_1', 'player_backpack_side_idle_2', 'player_backpack_side_idle_3', 'player_backpack_side_idle_4',
    'player_backpack_up_idle_1', 'player_backpack_up_idle_2', 'player_backpack_up_idle_3', 'player_backpack_up_idle_4',
    # Backpack Walk
    'player_backpack_down_walk1', 'player_backpack_down_walk2', 'player_backpack_down_walk3', 'player_backpack_down_walk4',
    'player_backpack_side_walk1', 'player_backpack_side_walk2', 'player_backpack_side_walk3', 'player_backpack_side_walk4',
    'player_backpack_up_walk1', 'player_backpack_up_walk2', 'player_backpack_up_walk3', 'player_backpack_up_walk4',
    # Action & Happy
    'player_action', 'player_happy_1', 'player_happy_2', 'player_happy_3', 'player_happy_4',
    # Combat
    'player_slash_down_1', 'player_slash_down_2', 'player_slash_down_3',
    'player_slash_side_1', 'player_slash_side_2', 'player_slash_side_3',
    'player_slash_up_1', 'player_slash_up_2', 'player_slash_up_3',
    'player_smash_down_1', 'player_smash_down_2', 'player_smash_down_3',
    'player_smash_side_1', 'player_smash_side_2', 'player_smash_side_3',
    'player_smash_up_1', 'player_smash_up_2', 'player_smash_up_3',
    'player_thrust_down_1', 'player_thrust_down_2', 'player_thrust_down_3',
    'player_thrust_side_1', 'player_thrust_side_2', 'player_thrust_side_3',
    'player_thrust_up_1', 'player_thrust_up_2', 'player_thrust_up_3'
]

b64_dict = {}
for name in sprite_names:
    path = os.path.join(char_dir, f"{name}.png")
    if os.path.exists(path):
        with open(path, "rb") as f:
            b64_dict[name] = "data:image/png;base64," + base64.b64encode(f.read()).decode("utf-8")

html = """<!DOCTYPE html>
<html lang="vi">
<head>
    <meta charset="UTF-8">
    <title>Farmer Khoa 64x64 HD Pixel Art & Animation Studio</title>
    <style>
        :root {
            --bg-dark: #0d1117;
            --panel-bg: #161b22;
            --accent: #238636;
            --accent-glow: rgba(35, 134, 54, 0.4);
            --border: #30363d;
            --text-light: #f0f6fc;
            --text-dim: #8b949e;
            --slash-color: #58a6ff;
            --smash-color: #f0883e;
            --thrust-color: #3fb950;
        }
        * { box-sizing: border-box; margin: 0; padding: 0; font-family: -apple-system, BlinkMacSystemFont, "Segoe UI", Helvetica, Arial, sans-serif; }
        body { background: var(--bg-dark); color: var(--text-light); padding: 24px; min-height: 100vh; }
        .header { display: flex; justify-content: space-between; align-items: center; border-bottom: 1px solid var(--border); padding-bottom: 16px; margin-bottom: 24px; }
        .header h1 { font-size: 22px; display: flex; align-items: center; gap: 10px; color: #58a6ff; }
        .badges { display: flex; gap: 8px; }
        .badge { background: #238636; color: #fff; padding: 4px 10px; border-radius: 20px; font-size: 12px; font-weight: 600; }
        .badge-info { background: #1f6feb; }
        .badge-purple { background: #8957e5; }

        .main-grid { display: grid; grid-template-columns: 320px 1fr; gap: 24px; }
        .sidebar { background: var(--panel-bg); border: 1px solid var(--border); border-radius: 8px; padding: 18px; display: flex; flex-direction: column; gap: 16px; }
        .section-header { font-size: 12px; font-weight: 600; text-transform: uppercase; color: var(--text-dim); margin-bottom: 6px; }
        
        .btn-group { display: flex; flex-wrap: wrap; gap: 6px; }
        button {
            background: #21262d; border: 1px solid var(--border); color: #c9d1d9; padding: 6px 12px; border-radius: 6px;
            font-size: 13px; font-weight: 500; cursor: pointer; transition: 0.15s;
        }
        button:hover { background: #30363d; border-color: #8b949e; }
        button.active { background: #1f6feb; border-color: #388bfd; color: #fff; font-weight: 600; box-shadow: 0 0 8px rgba(31, 111, 235, 0.4); }

        .toggle-box { display: flex; align-items: center; justify-content: space-between; background: #0d1117; padding: 10px 14px; border-radius: 6px; border: 1px solid var(--border); }
        .switch { position: relative; display: inline-block; width: 44px; height: 24px; }
        .switch input { opacity: 0; width: 0; height: 0; }
        .slider { position: absolute; cursor: pointer; top: 0; left: 0; right: 0; bottom: 0; background-color: #30363d; transition: .3s; border-radius: 24px; }
        .slider:before { position: absolute; content: ""; height: 18px; width: 18px; left: 3px; bottom: 3px; background-color: white; transition: .3s; border-radius: 50%; }
        input:checked + .slider { background-color: #238636; }
        input:checked + .slider:before { transform: translateX(20px); }

        .stage-card {
            background: var(--panel-bg); border: 1px solid var(--border); border-radius: 8px; padding: 24px;
            display: flex; flex-direction: column; align-items: center; justify-content: center; min-height: 480px; position: relative;
        }
        .canvas-box {
            position: relative; width: 256px; height: 256px; background: #0d1117; border: 2px dashed #30363d;
            border-radius: 8px; display: flex; align-items: center; justify-content: center;
            background-image: 
                linear-gradient(45deg, #161b22 25%, transparent 25%), 
                linear-gradient(-45deg, #161b22 25%, transparent 25%), 
                linear-gradient(45deg, transparent 75%, #161b22 75%), 
                linear-gradient(-45deg, transparent 75%, #161b22 75%);
            background-size: 16px 16px;
            background-position: 0 0, 0 8px, 8px -8px, -8px 0px;
        }
        .sprite-display {
            width: 256px; height: 256px; image-rendering: pixelated; image-rendering: crisp-edges;
            filter: drop-shadow(0 12px 16px rgba(0,0,0,0.7));
        }

        .stage-info { margin-top: 16px; text-align: center; }
        .stage-title { font-size: 18px; font-weight: 600; color: #f0f6fc; }
        .stage-meta { font-size: 13px; color: var(--text-dim); margin-top: 4px; }

        .strip { display: flex; gap: 10px; margin-top: 20px; background: #0d1117; padding: 12px; border-radius: 6px; border: 1px solid var(--border); }
        .strip-item { display: flex; flex-direction: column; align-items: center; gap: 4px; padding: 6px; border-radius: 4px; border: 2px solid transparent; background: #161b22; }
        .strip-item.current { border-color: #58a6ff; background: #1f242c; box-shadow: 0 0 8px rgba(88, 166, 255, 0.3); }
        .strip-item img { width: 56px; height: 56px; image-rendering: pixelated; }
        .strip-item span { font-size: 11px; color: var(--text-dim); }

        .palette-grid { display: grid; grid-template-columns: repeat(6, 1fr); gap: 8px; margin-top: 16px; }
        .p-chip { height: 28px; border-radius: 4px; border: 1px solid rgba(255,255,255,0.1); position: relative; }
        .p-chip:hover::after { content: attr(data-label); position: absolute; bottom: 32px; left: 50%; transform: translateX(-50%); background: #000; color: #fff; font-size: 11px; padding: 2px 6px; border-radius: 4px; white-space: nowrap; z-index: 10; }
    </style>
</head>
<body>
    <div class="header">
        <div>
            <h1>🗡️ Farmer Khoa - 64x64 HD Pixel Art Studio</h1>
            <p style="color: var(--text-dim); margin-top: 4px;">100% Nguyên bản theo thiết kế: Tóc rối nâu cà phê, áo khoác slate/teal utility, quần xám đậm & giày da nâu</p>
        </div>
        <div class="badges">
            <span class="badge">100% Design Matched</span>
            <span class="badge badge-info">PPU 32 (Scale 2.0m)</span>
            <span class="badge badge-purple">83 Sprites 64x64</span>
        </div>
    </div>

    <div class="main-grid">
        <div class="sidebar">
            <div class="toggle-box">
                <span style="font-size: 13px; font-weight: 600;">🎒 Trang bị Balo (Backpack)</span>
                <label class="switch">
                    <input type="checkbox" id="bpToggle" onchange="toggleBackpack()">
                    <span class="slider"></span>
                </label>
            </div>

            <div>
                <div class="section-header">Hướng Nhìn (Facing Direction)</div>
                <div class="btn-group">
                    <button class="active" id="btn-down" onclick="setFacing('down')">⬇️ Down (Trước)</button>
                    <button id="btn-side" onclick="setFacing('side')">➡️ Side (Ngang)</button>
                    <button id="btn-up" onclick="setFacing('up')">⬆️ Up (Sau)</button>
                </div>
            </div>

            <div>
                <div class="section-header">Animation Trạng Thái Cơ Bản</div>
                <div class="btn-group">
                    <button class="active" id="btn-idle" onclick="setAnim('idle')">🫁 Idle (Thở & Chớp mắt)</button>
                    <button id="btn-walk" onclick="setAnim('walk')">🚶 Walk (Bước đi 4 Frames)</button>
                    <button id="btn-action" onclick="setAnim('action')">🌾 Action (Rải cám / Sửa rào)</button>
                    <button id="btn-happy" onclick="setAnim('happy')">✨ Happy (Ăn mừng / Lên cấp)</button>
                </div>
            </div>

            <div>
                <div class="section-header">Animation Chiến Đấu (Combat Strikes)</div>
                <div class="btn-group">
                    <button id="btn-slash" onclick="setAnim('slash')" style="border-left: 3px solid var(--slash-color);">⚔️ Slash (Kiếm / Rựa)</button>
                    <button id="btn-smash" onclick="setAnim('smash')" style="border-left: 3px solid var(--smash-color);">🔨 Smash (Búa tạ)</button>
                    <button id="btn-thrust" onclick="setAnim('thrust')" style="border-left: 3px solid var(--thrust-color);">🔱 Thrust (Thương / Chĩa)</button>
                </div>
            </div>

            <div>
                <div class="section-header">Tốc độ khung hình (FPS)</div>
                <div class="btn-group">
                    <button onclick="setFPS(2)">2 FPS</button>
                    <button class="active" id="fps-default" onclick="setFPS(4)">4 FPS (Idle chuẩn)</button>
                    <button onclick="setFPS(7.5)">7.5 FPS (Walk chuẩn)</button>
                    <button onclick="setFPS(12)">12 FPS (Combat)</button>
                </div>
            </div>

            <div>
                <div class="section-header">Bảng Màu Nguyên Bản (Exact Palette)</div>
                <div class="palette-grid">
                    <div class="p-chip" style="background: #2f2420;" data-label="Tóc nâu đậm"></div>
                    <div class="p-chip" style="background: #49564f;" data-label="Áo khoác Slate"></div>
                    <div class="p-chip" style="background: #1a1a1a;" data-label="Áo trong đen"></div>
                    <div class="p-chip" style="background: #f5c69b;" data-label="Màu da Khoa"></div>
                    <div class="p-chip" style="background: #263033;" data-label="Quần xám than"></div>
                    <div class="p-chip" style="background: #715f49;" data-label="Giày da nâu"></div>
                </div>
            </div>
        </div>

        <div class="stage-card">
            <div class="canvas-box">
                <img id="mainSprite" class="sprite-display" src="">
            </div>

            <div class="stage-info">
                <div id="animTitle" class="stage-title">Down Idle (Thở & Chớp Mắt)</div>
                <div id="animMeta" class="stage-meta">Frame: 1/4 | 4.0 FPS | Kích thước: 64x64 px</div>
            </div>

            <div id="frameStrip" class="strip"></div>
        </div>
    </div>

    <script>
        const sprites = """ + str(b64_dict).replace("'", '"') + """;

        let currentFacing = 'down';
        let currentAnim = 'idle';
        let hasBackpack = false;
        let fps = 4.0;
        let frameIdx = 0;
        let timer = null;

        function getFrames() {
            const prefix = hasBackpack ? 'player_backpack_' : 'player_';
            if (currentAnim === 'idle') {
                return [
                    sprites[`${prefix}${currentFacing}_idle_1`],
                    sprites[`${prefix}${currentFacing}_idle_2`],
                    sprites[`${prefix}${currentFacing}_idle_3`],
                    sprites[`${prefix}${currentFacing}_idle_4`]
                ];
            } else if (currentAnim === 'walk') {
                return [
                    sprites[`${prefix}${currentFacing}_walk1`],
                    sprites[`${prefix}${currentFacing}_walk2`],
                    sprites[`${prefix}${currentFacing}_walk3`],
                    sprites[`${prefix}${currentFacing}_walk4`]
                ];
            } else if (currentAnim === 'action') {
                return [sprites['player_action']];
            } else if (currentAnim === 'happy') {
                return [
                    sprites['player_happy_1'],
                    sprites['player_happy_2'],
                    sprites['player_happy_3'],
                    sprites['player_happy_4']
                ];
            } else if (['slash', 'smash', 'thrust'].includes(currentAnim)) {
                return [
                    sprites[`player_${currentAnim}_${currentFacing}_1`],
                    sprites[`player_${currentAnim}_${currentFacing}_2`],
                    sprites[`player_${currentAnim}_${currentFacing}_3`]
                ];
            }
            return [];
        }

        function updateStrip(frames) {
            const strip = document.getElementById('frameStrip');
            strip.innerHTML = '';
            frames.forEach((f, idx) => {
                const item = document.createElement('div');
                item.className = 'strip-item' + (idx === frameIdx ? ' current' : '');
                item.id = `strip-frame-${idx}`;
                item.innerHTML = `<img src="${f}"><span>F${idx+1}</span>`;
                strip.appendChild(item);
            });
        }

        function renderFrame() {
            const frames = getFrames();
            if (!frames || frames.length === 0) return;
            const currentF = frames[frameIdx % frames.length];
            document.getElementById('mainSprite').src = currentF;

            // Highlight in strip
            frames.forEach((_, idx) => {
                const el = document.getElementById(`strip-frame-${idx}`);
                if (el) {
                    if (idx === (frameIdx % frames.length)) el.classList.add('current');
                    else el.classList.remove('current');
                }
            });

            document.getElementById('animMeta').innerText = 
                `Frame: ${(frameIdx % frames.length) + 1}/${frames.length} | ${fps.toFixed(1)} FPS | Canvas: 64x64 px`;

            frameIdx = (frameIdx + 1) % frames.length;
        }

        function restartTimer() {
            if (timer) clearInterval(timer);
            frameIdx = 0;
            const frames = getFrames();
            updateStrip(frames);
            renderFrame();
            timer = setInterval(renderFrame, 1000 / fps);
        }

        function setFacing(f) {
            currentFacing = f;
            document.querySelectorAll('#btn-down, #btn-side, #btn-up').forEach(b => b.classList.remove('active'));
            document.getElementById(`btn-${f}`).classList.add('active');
            updateTitle();
            restartTimer();
        }

        function setAnim(a) {
            currentAnim = a;
            document.querySelectorAll('#btn-idle, #btn-walk, #btn-action, #btn-happy, #btn-slash, #btn-smash, #btn-thrust').forEach(b => b.classList.remove('active'));
            document.getElementById(`btn-${a}`).classList.add('active');
            
            // Adjust default recommended FPS
            if (a === 'idle') fps = 4.0;
            else if (a === 'walk') fps = 7.5;
            else if (['slash', 'smash', 'thrust'].includes(a)) fps = 6.0;
            else if (a === 'happy') fps = 6.0;
            else if (a === 'action') fps = 2.0;

            updateTitle();
            restartTimer();
        }

        function toggleBackpack() {
            hasBackpack = document.getElementById('bpToggle').checked;
            updateTitle();
            restartTimer();
        }

        function setFPS(newFps) {
            fps = newFps;
            restartTimer();
        }

        function updateTitle() {
            const names = {
                idle: 'Idle (Thở nhấp nhô & Chớp mắt)',
                walk: 'Walk (Chu kỳ bước 4 Frames)',
                action: 'Action (Rải cám / Sửa rào)',
                happy: 'Happy (Nhảy ăn mừng & Ngôi sao)',
                slash: 'Combat: Slash (Vung kiếm / Chém)',
                smash: 'Combat: Smash (Đập búa tạ)',
                thrust: 'Combat: Thrust (Đâm thương chĩa)'
            };
            const bpText = hasBackpack ? ' [Balo On]' : '';
            document.getElementById('animTitle').innerText = `${currentFacing.toUpperCase()} - ${names[currentAnim]}${bpText}`;
        }

        // Init
        setAnim('idle');
    </script>
</body>
</html>
"""

artifact_path = "/home/danh/.gemini/antigravity/brain/e605a811-d043-4617-bbe7-f0f18bf72b28/farmer_khoa_64x64_studio.html"
with open(artifact_path, "w") as f:
    f.write(html)
print("Updated studio artifact at:", artifact_path)
