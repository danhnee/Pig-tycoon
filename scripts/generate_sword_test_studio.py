import os
import base64

ARTIFACT_DIR = '/home/danh/.gemini/antigravity/brain/e605a811-d043-4617-bbe7-f0f18bf72b28'
CHAR_DIR = 'Assets/Art/Sprites/Characters'
WEAPON_DIR = 'Assets/Art/Sprites/Weapons'
VFX_DIR = 'Assets/Art/Sprites/VFX'

def to_b64(path):
    with open(path, 'rb') as f:
        return 'data:image/png;base64,' + base64.b64encode(f.read()).decode('utf-8')

def gif_to_b64(path):
    with open(path, 'rb') as f:
        return 'data:image/gif;base64,' + base64.b64encode(f.read()).decode('utf-8')

# Base64 assets
c_down = to_b64(f'{CHAR_DIR}/player_down_idle_1.png')
c_right = to_b64(f'{CHAR_DIR}/player_right_idle_1.png')
c_up = to_b64(f'{CHAR_DIR}/player_up_idle_1.png')
c_left = to_b64(f'{CHAR_DIR}/player_left_idle_1.png')

w_kiem = to_b64(f'{WEAPON_DIR}/weapon_kiem_sat.png')
w_dao = to_b64(f'{WEAPON_DIR}/weapon_dao_go.png')
w_giao = to_b64(f'{WEAPON_DIR}/weapon_giao_tre.png')
w_bua = to_b64(f'{WEAPON_DIR}/weapon_bua_ta.png')

v_down = to_b64(f'{VFX_DIR}/vfx_trail_slash_down.png')
v_right = to_b64(f'{VFX_DIR}/vfx_trail_slash_right.png')
v_up = to_b64(f'{VFX_DIR}/vfx_trail_slash_up.png')
v_left = to_b64(f'{VFX_DIR}/vfx_trail_slash_left.png')

gif_down = gif_to_b64(f'{ARTIFACT_DIR}/sword_stardew_down.gif')
gif_right = gif_to_b64(f'{ARTIFACT_DIR}/sword_stardew_right.gif')
gif_up = gif_to_b64(f'{ARTIFACT_DIR}/sword_stardew_up.gif')
gif_left = gif_to_b64(f'{ARTIFACT_DIR}/sword_stardew_left.gif')

html_content = f'''<!DOCTYPE html>
<html lang="vi">
<head>
    <meta charset="UTF-8">
    <title>Stardew Valley Sword Combat Animation Studio - Pig Tycoon</title>
    <style>
        :root {{
            --bg-primary: #0f172a;
            --bg-secondary: #1e293b;
            --bg-card: #182234;
            --accent-gold: #fbbf24;
            --accent-cyan: #38bdf8;
            --accent-green: #34d399;
            --accent-red: #f87171;
            --text-light: #f8fafc;
            --text-muted: #94a3b8;
            --border-color: #334155;
        }}
        * {{ box-sizing: border-box; margin: 0; padding: 0; font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, sans-serif; }}
        body {{ background: var(--bg-primary); color: var(--text-light); line-height: 1.5; padding: 24px; }}
        .header {{ text-align: center; margin-bottom: 24px; border-bottom: 1px solid var(--border-color); padding-bottom: 16px; }}
        .header h1 {{ font-size: 26px; color: var(--accent-gold); margin-bottom: 6px; }}
        .header p {{ color: var(--text-muted); font-size: 14px; }}
        
        .grid-2col {{ display: grid; grid-template-columns: 1fr 1fr; gap: 24px; margin-bottom: 24px; }}
        @media (max-width: 900px) {{ .grid-2col {{ grid-template-columns: 1fr; }} }}

        .card {{ background: var(--bg-secondary); border: 1px solid var(--border-color); border-radius: 12px; padding: 20px; }}
        .card-title {{ font-size: 18px; color: var(--accent-cyan); font-weight: 600; margin-bottom: 14px; display: flex; align-items: center; justify-content: space-between; }}

        .canvas-container {{ display: flex; flex-direction: column; align-items: center; justify-content: center; background: #090d16; border-radius: 8px; padding: 20px; border: 1px solid #1e293b; }}
        canvas {{ image-rendering: pixelated; background: #0f172a; border-radius: 6px; box-shadow: 0 4px 12px rgba(0,0,0,0.5); }}

        .control-row {{ display: flex; flex-wrap: wrap; gap: 8px; margin-top: 14px; justify-content: center; }}
        button {{ background: #334155; color: white; border: none; border-radius: 6px; padding: 8px 14px; font-size: 13px; font-weight: 500; cursor: pointer; transition: all 0.15s; }}
        button:hover {{ background: #475569; }}
        button.active {{ background: #0284c7; color: white; font-weight: 600; box-shadow: 0 0 10px rgba(2,132,199,0.5); }}
        button.btn-attack {{ background: #e11d48; font-weight: 700; padding: 8px 20px; }}
        button.btn-attack:hover {{ background: #f43f5e; }}

        .gif-gallery {{ display: grid; grid-template-columns: repeat(4, 1fr); gap: 12px; margin-top: 12px; }}
        .gif-item {{ background: #090d16; border-radius: 8px; padding: 10px; text-align: center; border: 1px solid #1e293b; }}
        .gif-item img {{ image-rendering: pixelated; width: 100%; max-width: 128px; display: block; margin: 0 auto; }}
        .gif-label {{ font-size: 12px; color: var(--accent-gold); margin-top: 6px; font-weight: 600; }}

        .info-table {{ width: 100%; border-collapse: collapse; margin-top: 12px; font-size: 13px; }}
        .info-table th, .info-table td {{ padding: 8px 10px; border: 1px solid var(--border-color); text-align: left; }}
        .info-table th {{ background: #0f172a; color: var(--accent-gold); }}
        .badge-front {{ background: rgba(52, 211, 153, 0.2); color: #34d399; padding: 2px 6px; border-radius: 4px; font-weight: 600; }}
        .badge-back {{ background: rgba(248, 113, 113, 0.2); color: #f87171; padding: 2px 6px; border-radius: 4px; font-weight: 600; }}
        .badge-hit {{ background: rgba(251, 191, 36, 0.3); color: #fbbf24; padding: 2px 6px; border-radius: 4px; font-weight: 700; }}

        .stepper-info {{ margin-top: 10px; font-size: 13px; color: var(--accent-cyan); min-height: 20px; }}
    </style>
</head>
<body>

    <div class="header">
        <h1>⚔️ STARDEW VALLEY SWORD ATTACK TEST STUDIO</h1>
        <p>Kiểm nghiệm hoạt họa chém kiếm 4 hướng theo cơ chế phân lớp và xoay góc chuẩn Stardew Valley (Khoa 64x64 + Vũ khí Modular 24x24 + VFX Trail)</p>
    </div>

    <div class="grid-2col">
        <!-- INTERACTIVE PLAYGROUND -->
        <div class="card">
            <div class="card-title">
                <span>🎮 Interactive Realtime Test</span>
                <span style="font-size: 12px; color: var(--accent-green);">Active Socket Engine</span>
            </div>

            <div class="canvas-container">
                <canvas id="combatCanvas" width="256" height="256"></canvas>
                <div class="stepper-info" id="stepperText">Sẵn sàng. Nhấn 'Tấn Công' (Space) hoặc bấm nút.</div>
            </div>

            <div class="control-row">
                <button class="btn-attack" id="btnAttack">⚔️ TẤN CÔNG (Slash)</button>
                <button id="btnLoop">🔁 Lặp liên tục: TẮT</button>
                <button id="btnStep">⏭️ Từng bước Frame</button>
            </div>

            <div class="control-row">
                <span style="font-size: 13px; color: var(--text-muted); align-self: center;">Hướng:</span>
                <button class="active dir-btn" data-dir="down">Xuống (Down)</button>
                <button class="dir-btn" data-dir="right">Phải (Right)</button>
                <button class="dir-btn" data-dir="up">Lên (Up)</button>
                <button class="dir-btn" data-dir="left">Trái (Left)</button>
            </div>

            <div class="control-row">
                <span style="font-size: 13px; color: var(--text-muted); align-self: center;">Tốc độ:</span>
                <button class="speed-btn active" data-speed="1">1.0x (250ms)</button>
                <button class="speed-btn" data-speed="0.5">0.5x Chậm</button>
                <button class="speed-btn" data-speed="0.25">0.25x Rất chậm</button>
            </div>

            <div class="control-row">
                <span style="font-size: 13px; color: var(--text-muted); align-self: center;">Vũ khí:</span>
                <button class="weapon-btn active" data-weapon="kiem">Kiếm Sắt</button>
                <button class="weapon-btn" data-weapon="dao">Đao Gỗ</button>
                <button class="weapon-btn" data-weapon="giao">Giáo Tre</button>
                <button class="weapon-btn" data-weapon="bua">Búa Tạ</button>
            </div>

            <div class="control-row">
                <label style="font-size: 13px; color: var(--text-muted); display: flex; align-items: center; gap: 6px; cursor: pointer;">
                    <input type="checkbox" id="chkVFX" checked> Hiện VFX Trail Lưỡi Liềm
                </label>
                <label style="font-size: 13px; color: var(--text-muted); display: flex; align-items: center; gap: 6px; cursor: pointer; margin-left: 12px;">
                    <input type="checkbox" id="chkPivot"> Hiện Điểm Neo Chuôi (Pivot)
                </label>
            </div>
        </div>

        <!-- 4-DIRECTION ANIMATED GALLERY -->
        <div class="card">
            <div class="card-title">
                <span>🎞️ Hoạt Họa 4 Hướng Thực Tế (Animated GIFs)</span>
                <span style="font-size: 12px; color: var(--text-muted);">Chu kỳ 255ms chuẩn Stardew</span>
            </div>

            <div class="gif-gallery">
                <div class="gif-item">
                    <img src="{gif_down}" alt="Down">
                    <div class="gif-label">XUỐNG (Down)</div>
                </div>
                <div class="gif-item">
                    <img src="{gif_right}" alt="Right">
                    <div class="gif-label">PHẢI (Right)</div>
                </div>
                <div class="gif-item">
                    <img src="{gif_up}" alt="Up">
                    <div class="gif-label">LÊN (Up)</div>
                </div>
                <div class="gif-item">
                    <img src="{gif_left}" alt="Left">
                    <div class="gif-label">TRÁI (Left)</div>
                </div>
            </div>

            <div style="margin-top: 18px;">
                <div class="card-title" style="font-size: 15px;">
                    <span>📋 Bảng Tham Số Hoạt Họa 6 Frames Theo Hướng</span>
                </div>
                <table class="info-table" id="framesTable">
                    <thead>
                        <tr>
                            <th>Frame</th>
                            <th>Socket (X, Y)</th>
                            <th>Góc xoay (Z)</th>
                            <th>Phân lớp</th>
                            <th>Thời gian</th>
                            <th>Giai đoạn</th>
                        </tr>
                    </thead>
                    <tbody id="tableBody"></tbody>
                </table>
            </div>
        </div>
    </div>

    <!-- MECHANICS BREAKDOWN -->
    <div class="card">
        <div class="card-title">
            <span>🧠 Phân Tích Cơ Chế Khắc Phục Lỗi Của Stardew Valley</span>
        </div>
        <div style="display: grid; grid-template-columns: repeat(3, 1fr); gap: 16px; font-size: 13px;">
            <div style="background: #090d16; padding: 14px; border-radius: 8px;">
                <h4 style="color: var(--accent-gold); margin-bottom: 6px;">1. Không Vẽ Vũ Khí Vào Thân</h4>
                <p style="color: var(--text-muted);">Nhân vật Khoa giữ nguyên các Sprite Idle tự nhiên của nghệ nhân. Code game điều khiển vũ khí xoay quanh chuôi cầm tay bằng toán ma trận affine, triệt tiêu hoàn toàn hiện tượng méo hình hay rách sprite.</p>
            </div>
            <div style="background: #090d16; padding: 14px; border-radius: 8px;">
                <h4 style="color: var(--accent-cyan); margin-bottom: 6px;">2. Phân Lớp Động (Layer Depth)</h4>
                <p style="color: var(--text-muted);">Khi giương kiếm lên sau lưng (Frames 0-1) hoặc chém hướng Lên (Up), vũ khí tự động chuyển layer depth <code>ToolUp</code> (vẽ phía sau lưng). Khi vung đòn ra trước ngực (Frames 2-5), vũ khí nhảy lên layer <code>ToolDown</code> (trước ngực).</p>
            </div>
            <div style="background: #090d16; padding: 14px; border-radius: 8px;">
                <h4 style="color: var(--accent-green); margin-bottom: 6px;">3. Tốc Độ Chu Kỳ & VFX Lưỡi Liềm</h4>
                <p style="color: var(--text-muted);">Tổng thời gian vung kiếm chỉ 255ms. Tại Frame 2 (Peak Hit Frame 35ms), hitbox va chạm kích hoạt đồng thời với dải VFX lưỡi liềm 64x64 tạo cảm giác đòn đánh sắc bén, uy lực và mượt mà y hệt Stardew.</p>
            </div>
        </div>
    </div>

    <script>
        const CHARS = {{
            down: '{c_down}',
            right: '{c_right}',
            up: '{c_up}',
            left: '{c_left}'
        }};

        const WEAPONS = {{
            kiem: '{w_kiem}',
            dao: '{w_dao}',
            giao: '{w_giao}',
            bua: '{w_bua}'
        }};

        const VFX_TRAILS = {{
            down: '{v_down}',
            right: '{v_right}',
            up: '{v_up}',
            left: '{v_left}'
        }};

        const SWORD_DATA = {{
            right: [
                {{ x: 20, y: 24, rot: 25, sort: -1, dur: 55, event: 'Giương kiếm sau vai' }},
                {{ x: 32, y: 18, rot: -25, sort: -1, dur: 45, event: 'Vung qua đỉnh đầu' }},
                {{ x: 42, y: 26, rot: -80, sort: 1, dur: 35, event: '💥 Chém thẳng ra trước + VFX' }},
                {{ x: 44, y: 34, rot: -125, sort: 1, dur: 35, event: 'Chém xéo xuống hông' }},
                {{ x: 38, y: 40, rot: -160, sort: 1, dur: 35, event: 'Quét đà qua đùi' }},
                {{ x: 32, y: 36, rot: -140, sort: 1, dur: 75, event: 'Hồi kiếm cạnh hông' }}
            ],
            left: [
                {{ x: 44, y: 24, rot: -25, sort: -1, dur: 55, event: 'Giương kiếm sau vai' }},
                {{ x: 32, y: 18, rot: 25, sort: -1, dur: 45, event: 'Vung qua đỉnh đầu' }},
                {{ x: 22, y: 26, rot: 80, sort: 1, dur: 35, event: '💥 Chém thẳng ra trước + VFX' }},
                {{ x: 20, y: 34, rot: 125, sort: 1, dur: 35, event: 'Chém xéo xuống hông' }},
                {{ x: 26, y: 40, rot: 160, sort: 1, dur: 35, event: 'Quét đà qua đùi' }},
                {{ x: 32, y: 36, rot: 140, sort: 1, dur: 75, event: 'Hồi kiếm cạnh hông' }}
            ],
            down: [
                {{ x: 22, y: 24, rot: -35, sort: 1, dur: 55, event: 'Giương kiếm vai phải' }},
                {{ x: 26, y: 28, rot: -75, sort: 1, dur: 45, event: 'Bắt đầu chém chéo xuống' }},
                {{ x: 32, y: 32, rot: -120, sort: 1, dur: 35, event: '💥 Chém ngang ngực + VFX' }},
                {{ x: 36, y: 36, rot: -165, sort: 1, dur: 35, event: 'Cắt xéo xuống rốn' }},
                {{ x: 38, y: 40, rot: 150, sort: 1, dur: 35, event: 'Quét đà qua hông trái' }},
                {{ x: 36, y: 38, rot: 165, sort: 1, dur: 75, event: 'Thu kiếm thế thủ' }}
            ],
            up: [
                {{ x: 22, y: 38, rot: -150, sort: -1, dur: 55, event: 'Thấp cạnh hông phải' }},
                {{ x: 24, y: 26, rot: -90, sort: -1, dur: 45, event: 'Vung dọc sườn lên' }},
                {{ x: 28, y: 14, rot: -20, sort: -1, dur: 35, event: '💥 Chém vút qua đầu + VFX' }},
                {{ x: 34, y: 12, rot: 25, sort: -1, dur: 35, event: 'Quạt vòng cung trên cao' }},
                {{ x: 40, y: 20, rot: 75, sort: -1, dur: 35, event: 'Đưa đà sang vai trái' }},
                {{ x: 36, y: 26, rot: 45, sort: -1, dur: 75, event: 'Hồi thế thủ sau lưng' }}
            ]
        }};

        // Preload Images
        const imgCache = {{}};
        function loadImage(src) {{
            if (!imgCache[src]) {{
                const img = new Image();
                img.src = src;
                imgCache[src] = img;
            }}
            return imgCache[src];
        }}

        // State
        let currentDir = 'down';
        let currentWeapon = 'kiem';
        let currentSpeed = 1.0;
        let isLooping = false;
        let isAttacking = false;
        let attackFrame = -1; // -1: idle, 0..5: attack frames
        let animTimer = null;

        const canvas = document.getElementById('combatCanvas');
        const ctx = canvas.getContext('2d');
        ctx.imageSmoothingEnabled = false;

        function updateTable() {{
            const frames = SWORD_DATA[currentDir];
            const tbody = document.getElementById('tableBody');
            tbody.innerHTML = '';
            frames.forEach((f, idx) => {{
                const tr = document.createElement('tr');
                if (attackFrame === idx) tr.style.background = 'rgba(56, 189, 248, 0.2)';
                const sortBadge = f.sort === 1 ? '<span class="badge-front">Trước ngực (+1)</span>' : '<span class="badge-back">Sau lưng (-1)</span>';
                const eventText = idx === 2 ? `<span class="badge-hit">${{f.event}}</span>` : f.event;
                tr.innerHTML = `
                    <td><strong>#${{idx}}</strong></td>
                    <td>(${{ f.x }}, ${{ f.y }})</td>
                    <td>${{ f.rot }}°</td>
                    <td>${{ sortBadge }}</td>
                    <td>${{ f.dur }}ms</td>
                    <td>${{ eventText }}</td>
                `;
                tbody.appendChild(tr);
            }});
        }}

        function draw() {{
            ctx.clearRect(0, 0, canvas.width, canvas.height);
            const scale = 4; // 64x64 -> 256x256

            const charImg = loadImage(CHARS[currentDir]);
            const weaponImg = loadImage(WEAPONS[currentWeapon]);
            const vfxImg = loadImage(VFX_TRAILS[currentDir]);
            const showVFX = document.getElementById('chkVFX').checked;
            const showPivot = document.getElementById('chkPivot').checked;

            const frames = SWORD_DATA[currentDir];
            const f = attackFrame >= 0 ? frames[attackFrame] : null;

            // 1. Draw Behind Weapon
            if (f && f.sort === -1) {{
                drawWeapon(weaponImg, f.x, f.y, f.rot, scale);
            }}

            // 2. Draw Character
            if (charImg.complete) {{
                ctx.drawImage(charImg, 0, 0, 64 * scale, 64 * scale);
            }}

            // 3. Draw In-Front Weapon
            if (f && f.sort === 1) {{
                drawWeapon(weaponImg, f.x, f.y, f.rot, scale);
            }}

            // 4. Draw VFX Trail
            if (showVFX && (attackFrame === 2 || attackFrame === 3)) {{
                if (vfxImg.complete) {{
                    ctx.drawImage(vfxImg, 0, 0, 64 * scale, 64 * scale);
                }}
            }}

            // 5. Draw Pivot Gizmo
            if (showPivot && f) {{
                ctx.fillStyle = '#f43f5e';
                ctx.beginPath();
                ctx.arc(f.x * scale, f.y * scale, 4, 0, Math.PI * 2);
                ctx.fill();
            }}

            // Status label
            const textEl = document.getElementById('stepperText');
            if (attackFrame === -1) {{
                textEl.innerText = `[Idle] Hướng: ${{currentDir.toUpperCase()}} - Vũ khí sẵn sàng.`;
            }} else {{
                textEl.innerText = `[Tấn công] Frame ${{attackFrame}}/5 (${{frames[attackFrame].dur}}ms) - Góc: ${{frames[attackFrame].rot}}° - ${{frames[attackFrame].event}}`;
            }}
            updateTable();
        }}

        function drawWeapon(weaponImg, sockX, sockY, rotDeg, scale) {{
            if (!weaponImg.complete) return;
            const HANDLE_X = 11.5;
            const HANDLE_Y = 20.5;

            ctx.save();
            // Translate to socket pivot on canvas
            ctx.translate(sockX * scale, sockY * scale);
            // Rotate around pivot (positive in Unity is counter-clockwise, in canvas standard positive is clockwise, so negate rotDeg)
            ctx.rotate(-rotDeg * Math.PI / 180);
            // Draw weapon image centered at handle pivot
            ctx.drawImage(weaponImg, -HANDLE_X * scale, -HANDLE_Y * scale, 24 * scale, 24 * scale);
            ctx.restore();
        }}

        function triggerAttack() {{
            if (isAttacking) return;
            isAttacking = true;
            attackFrame = 0;
            draw();
            playNextFrame();
        }}

        function playNextFrame() {{
            const frames = SWORD_DATA[currentDir];
            if (attackFrame >= frames.length - 1) {{
                // Done
                setTimeout(() => {{
                    attackFrame = -1;
                    isAttacking = false;
                    draw();
                    if (isLooping) {{
                        setTimeout(triggerAttack, 300);
                    }}
                }}, (frames[frames.length - 1].dur / currentSpeed));
                return;
            }}

            const dur = frames[attackFrame].dur / currentSpeed;
            animTimer = setTimeout(() => {{
                attackFrame++;
                draw();
                playNextFrame();
            }}, dur);
        }}

        // Controls
        document.getElementById('btnAttack').onclick = triggerAttack;

        const loopBtn = document.getElementById('btnLoop');
        loopBtn.onclick = () => {{
            isLooping = !isLooping;
            loopBtn.innerText = isLooping ? '🔁 Lặp liên tục: BẬT' : '🔁 Lặp liên tục: TẮT';
            loopBtn.classList.toggle('active', isLooping);
            if (isLooping && !isAttacking) triggerAttack();
        }};

        document.getElementById('btnStep').onclick = () => {{
            if (animTimer) clearTimeout(animTimer);
            isAttacking = false;
            isLooping = false;
            loopBtn.innerText = '🔁 Lặp liên tục: TẮT';
            loopBtn.classList.remove('active');
            attackFrame = (attackFrame + 1) % 6;
            draw();
        }};

        document.querySelectorAll('.dir-btn').forEach(btn => {{
            btn.onclick = () => {{
                document.querySelectorAll('.dir-btn').forEach(b => b.classList.remove('active'));
                btn.classList.add('active');
                currentDir = btn.dataset.dir;
                draw();
            }};
        }});

        document.querySelectorAll('.speed-btn').forEach(btn => {{
            btn.onclick = () => {{
                document.querySelectorAll('.speed-btn').forEach(b => b.classList.remove('active'));
                btn.classList.add('active');
                currentSpeed = parseFloat(btn.dataset.speed);
            }};
        }});

        document.querySelectorAll('.weapon-btn').forEach(btn => {{
            btn.onclick = () => {{
                document.querySelectorAll('.weapon-btn').forEach(b => b.classList.remove('active'));
                btn.classList.add('active');
                currentWeapon = btn.dataset.weapon;
                draw();
            }};
        }});

        document.getElementById('chkVFX').onchange = draw;
        document.getElementById('chkPivot').onchange = draw;

        window.addEventListener('keydown', (e) => {{
            if (e.code === 'Space' || e.code === 'KeyJ') {{
                e.preventDefault();
                triggerAttack();
            }} else if (e.code === 'ArrowDown' || e.code === 'KeyS') {{
                document.querySelector('.dir-btn[data-dir="down"]').click();
            }} else if (e.code === 'ArrowRight' || e.code === 'KeyD') {{
                document.querySelector('.dir-btn[data-dir="right"]').click();
            }} else if (e.code === 'ArrowUp' || e.code === 'KeyW') {{
                document.querySelector('.dir-btn[data-dir="up"]').click();
            }} else if (e.code === 'ArrowLeft' || e.code === 'KeyA') {{
                document.querySelector('.dir-btn[data-dir="left"]').click();
            }}
        }});

        // Initial Draw
        setTimeout(draw, 100);
    </script>
</body>
</html>'''

out_file = f'{ARTIFACT_DIR}/stardew_sword_test_studio.html'
with open(out_file, 'w', encoding='utf-8') as f:
    f.write(html_content)

print(f'Successfully generated {out_file}!')
