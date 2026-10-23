# Chạy Blender / Unity ngầm cho agent, ít RAM, không giảm chất lượng

Ngày: 2026-10-23. Máy đo: 7,7 GB RAM, RTX 3050 Laptop 4 GB, Windows 10. Blender 5.3.0 tại `F:\Tools\blender-5.3.0-windows-x64`, Unity `D:\Unity\Editor\6000.6.3f1`.

## 1. Mục tiêu và giới hạn vật lý

Yêu cầu: khi agent cần render, Blender/Unity chỉ chạy ngầm và chiếm ít RAM nhất có thể, chất lượng ảnh giữ nguyên.

Số đo thật trên máy này (Zone05 candidate, 910k poly, Cycles 8 mẫu, 640×400):

| Tình huống | Working set đỉnh | Commit (private) đỉnh |
|---|---|---|
| Blender `--background` nghỉ, profile mặc định | 159 MB | 187 MB |
| Blender `--background --factory-startup` nghỉ | 115 MB | 123 MB |
| Render CPU | 902 MB | 1 354 MB |
| Render GPU OptiX | 1 221 MB | 1 362 MB |
| Render CPU `--threads 2` | 1 078 MB | 1 219 MB |
| Render CPU + prelude tiết kiệm (undo off, purge, auto-tile) | 485–555 MB | 1 349 MB |

Kết luận cần nói thẳng:

- **Không thể đưa tiến trình Blender xuống "vài chục MB".** Sàn của Blender headless là ~115 MB chưa tải cảnh. Khi render, bộ nhớ là dữ liệu cảnh + BVH của Cycles; cùng một cảnh, cùng chất lượng thì commit không đổi (~1,35 GB ở ví dụ trên). Mọi cách hạ số này đều làm đổi ảnh (giảm mẫu, giảm độ phân giải, `texture_limit`, `simplify`), tức là giảm chất lượng. Ta không làm.
- Con số working set thấp (485 MB) chỉ là Windows nén/đẩy trang ra khi thiếu RAM. Nó không phải tiết kiệm thật; commit vẫn 1,35 GB. Đừng dùng working set làm thước đo.
- GPU không giảm RAM hệ thống: OptiX vẫn giữ bản sao và dựng BVH phía host, đỉnh còn cao hơn CPU. GPU chỉ nhanh hơn.
- Unity Editor batchmode có sàn 1,5–3 GB (chưa đo trên máy này vì RAM trống lúc đo dưới 800 MB). Không có cờ nào đưa nó xuống vài trăm MB.

Vì vậy "ít RAM" được thiết kế lại thành ba điều có thể làm thật:

1. **Phía agent tốn 0 MB thường trực.** Agent không `import bpy`, không giữ Unity mở. Mỗi job là một tiến trình con sống đúng thời gian render rồi thoát.
2. **Cả máy chỉ gánh một job nặng một lúc**, chạy khi đủ RAM trống, có trần cứng, ưu tiên thấp, không cửa sổ. Máy không bao giờ đơ vì hai Blender + một Unity cùng chạy.
3. **Phần tiết kiệm lossless** được bật mặc định cho mọi script Blender (undo, cache, persistent data, tile), nhất là với script xây geometry, nơi undo stack mới là thứ ngốn RAM.

## 2. Kiến trúc

```
agent (Claude/Codex/Argon)
  │  python tools/headless_runner.py blender --script X.py -- args
  ▼
tools/headless_runner.py            (Python, không bpy, ~30 MB, thoát ngay khi job xong)
  ├─ lock  .agent_scratch/headless/heavy-process.lock   → 1 job nặng/lúc, lock mồ côi tự dọn
  ├─ cổng RAM: chờ tới khi RAM trống ≥ --min-free-mb (Blender 1200, Unity 2500), tối đa --wait-sec
  ├─ Win32 Job Object: trần commit --max-mem-mb + KILL_ON_JOB_CLOSE (runner chết thì con chết theo)
  ├─ CREATE_NO_WINDOW + BELOW_NORMAL_PRIORITY_CLASS   → ngầm, nhường CPU cho người dùng
  ├─ Blender: --background --factory-startup --python tools/blender_lean_prelude.py --python X.py
  ├─ Unity:   -batchmode -nographics -quit -silent-crashes -projectPath . -logFile L -executeMethod M
  ├─ lấy mẫu RSS/private mỗi 0,5 s (kể cả tiến trình con của Unity)
  ├─ cache theo hash: khoá = chế độ + lệnh + nội dung script/prelude + file đầu vào + bản exe;
  │    hit khi lần trước exit 0 và mọi --output còn đúng sha256  → không chạy gì, trả report cache_hit=true
  └─ ghi  <ts>-<mode>.log  +  <ts>-<mode>.json {exit_code, peak_private_mb, peak_ws_mb, killed_by_memory_cap, elapsed_sec, cache_key}
                                                ▲
agent chỉ đọc JSON + đuôi log (--tail 25) ───────┘   ảnh/GLB nằm trên đĩa, không đi qua context
```

Khi vượt trần: Job Object không giết mà làm `malloc` trả null, Blender in `Out of memory` và thoát mã 1. Runner nhận diện (đỉnh commit ≥ 90 % trần, mã ≠ 0), gắn `killed_by_memory_cap`, trả mã 137. Đã kiểm chứng với trần 600 MB: đỉnh 588 MB, thoát sạch, không treo máy.

## 3. Đòn bẩy lossless trong Blender (`tools/blender_lean_prelude.py`)

| Thiết lập | Vì sao không đổi ảnh | Tác dụng |
|---|---|---|
| `--factory-startup` | Không tải addon, theme, startup.blend của người dùng | −44 MB sàn; script ổn định giữa các máy |
| `preferences.edit.use_global_undo=False`, `undo_steps=0` | Undo chỉ phục vụ UI | Script xây geometry (world_*.py, build_*.py) không giữ nhiều bản sao mesh. Lớn nhất với job sinh cảnh. |
| `render.use_persistent_data=False` | Chỉ giữ BVH giữa các frame của animation | Giải phóng sau mỗi ảnh tĩnh |
| `cycles.use_auto_tile=True`, `tile_size=1024` | Cycles X ghép tile không khác pixel nào | Buffer render + denoise chia nhỏ với ảnh 1600–1800 px đang dùng |
| `orphans_purge` + bỏ scene thừa sau `open_mainfile` | Datablock 0 user không ảnh hưởng render | Rẻ, tránh giữ mesh cũ của candidate trước |
| `system.memory_cache_limit=256` | Cache ảnh/sequencer, không phải texture render | Blender giải phóng ảnh đã dùng |
| `open_mainfile(load_ui=False)` (script tự gọi) | Headless không có UI | Không dựng workspace/screen |

Kiểm chứng: so pixel (RGBA, 640×400) render có prelude với render không prelude: 0/256 000 pixel khác. Để so sánh, hai lần render không prelude cùng cấu hình đã khác nhau 4 pixel (tối đa 2/255) do lịch luồng của Cycles. SHA-256 của PNG vì thế không dùng được làm thước đo; dùng hiệu pixel.

Không được dùng (đổi ảnh): `cycles.samples` thấp hơn GDD, `resolution_percentage<100`, `texture_limit_render`, `use_simplify`, `max_subdivision`, `use_denoising=False` khi pipeline đang bật.

Đòn bẩy lớn nhất còn lại nằm ở **cảnh**, không ở cờ: render từng zone trong tiến trình riêng (đỉnh = zone lớn nhất, không phải tổng), và không render lại khi đầu vào không đổi (cache theo hash .blend + script + tham số; mục 6).

## 4. Chính sách Unity

- Unity không dùng để render ảnh duyệt art. Mọi ảnh overview/cận cảnh ra từ Blender (đã là luồng hiện tại). Unity chỉ import, dựng scene, kiểm physics/camera (`Verify3DMapPhysicsAndCameraUnity`, `BuildDocumentWorldCandidates`).
- Luôn `-batchmode -nographics -quit`, một `-executeMethod` mỗi lần chạy, log ra file. Không để Editor mở nền. AGENTS.md: Editor phải tắt, khoá `6000.6.3f1`; runner từ chối nếu `Unity.exe` đang chạy hoặc đường dẫn không đúng bản.
- Giữ thư mục `Library/` giữa các lần chạy: lần đầu import tốn vài GB và nhiều phút, lần sau chỉ vài trăm MB tăng thêm. Không xoá `Library/` để "tiết kiệm".
- Gộp nhiều việc vào một method khi có thể: mỗi lần khởi động batchmode trả thêm ~1 phút và sàn 1,5 GB.

## 5. Cách dùng

```powershell
cd D:\Pig-tycoon
python tools/headless_runner.py status
python tools/headless_runner.py blender --script tools/review_observatory.py -- --source ArtSource\Blender_3D_Map\Farm_Main_3D_Master.blend
python tools/headless_runner.py unity --method BuildDocumentWorldCandidates.Build
```

- Cache theo hash: khai báo file/thư mục script sẽ tạo bằng `--output` (lặp được). Khoá gồm chế độ, toàn bộ lệnh, nội dung script và prelude, mọi file/thư mục đầu vào xuất hiện trong tham số (hash nội dung, ví dụ .blend), và bản Blender/Unity (đường dẫn, kích thước, mtime). Hit chỉ khi lần trước thoát 0 và mọi output còn đúng sha256; đổi tham số, sửa script, sửa .blend hay mất/đổi output đều chạy lại. `--force` chạy lại dù hit, `--no-cache` tắt hẳn. Không khai báo `--output` thì không bao giờ hit (runner cảnh báo). Bản ghi nằm ở `.agent_scratch/headless/cache/<sha256>.json`.

  Đã đo: hit trả về trong 0,4 s thay cho 11–32 s và ~750 MB đỉnh của một job Blender nhỏ.

  ```powershell
  python tools/headless_runner.py blender --script tools/review_observatory.py --output .collab/artifacts/observatory-review -- --source ArtSource\Blender_3D_Map\Farm_Main_3D_Master.blend
  ```

- Script Blender hiện có chạy không cần sửa. Script nào gọi `open_mainfile` nên gọi thêm `bpy.app.driver_namespace["pig_lean_after_load"]()` ngay sau để purge và bật tile cho scene vừa mở.
- Biến môi trường: `PIG_TYCOON_BLENDER_EXE`, `PIG_TYCOON_UNITY_EXE`, `PIG_BLENDER_TILE`, `PIG_BLENDER_CACHE_MB`.
- Hub Argon (`D:\Collab-Hub\mcp\plugins\pig_tycoon_plugin.py`, `tool_collab_run_blender_generator`) nên gọi runner thay vì `subprocess.run` trực tiếp để cùng dùng lock và trần RAM với job của Claude/Codex.
- Kết quả cho agent: đọc file JSON (`peak_private_mb`, `exit_code`) và `--tail`. Không `cat` cả log vào context.

## 6. Việc tiếp theo (chưa làm)

1. Cache theo hash: đã làm (mục 5). Còn thiếu: dọn bản ghi cũ (`cache/` chỉ tăng), và với Unity `-projectPath` không được hash (cả repo) nên job Unity chỉ hit khi tự khai báo đầu vào qua tham số.
2. Đo Unity batchmode trên máy này khi RAM trống ≥ 3 GB, điền vào bảng mục 1.
3. Chuyển `tool_collab_run_blender_generator` của hub sang runner.
4. Script build cảnh lớn (`build_reference_world_art_master.py`, `world_*.py`): chạy qua runner và ghi đỉnh commit trước/sau prelude để có số cho job sinh geometry, nơi undo-off mới phát huy.
