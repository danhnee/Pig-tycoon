"""Prelude chạy trước script Blender headless để cắt RAM mà không đổi chất lượng ảnh.

Dùng qua tools/headless_runner.py (được chèn tự động), hoặc tay:
  blender --background --factory-startup --python tools/blender_lean_prelude.py --python <script> -- <args>

Mọi thứ ở đây đều lossless: không đụng samples, resolution, denoise, texture limit.
Xem Docs/plans/2026-10-23-headless-render-runner.md.
"""
import os

import bpy

prefs = bpy.context.preferences

# Undo stack: script xây geometry hàng trăm nghìn poly giữ nhiều bản sao trong undo.
# Headless không cần undo.
prefs.edit.use_global_undo = False
prefs.edit.undo_steps = 0
prefs.edit.undo_memory_limit = 1

# Cache sequencer/ảnh: giới hạn để Blender giải phóng ảnh đã dùng.
prefs.system.memory_cache_limit = int(os.environ.get("PIG_BLENDER_CACHE_MB", "256"))

# Script preview/tooltip/viewport không tồn tại trong background; tắt cho chắc.
prefs.view.show_tooltips = False


def _lean_scene(scene):
    """Áp cho scene đang render. Có thể gọi lại sau khi open_mainfile."""
    scene.render.use_persistent_data = False  # không giữ BVH giữa các frame
    if scene.render.engine == "CYCLES":
        # Tile tự động: buffer render + denoise chia nhỏ khi ảnh lớn (1600+ px).
        # Kết quả pixel y hệt render không tile.
        scene.cycles.use_auto_tile = True
        scene.cycles.tile_size = int(os.environ.get("PIG_BLENDER_TILE", "1024"))


def lean_after_load():
    """Gọi sau bpy.ops.wm.open_mainfile: bỏ scene thừa, purge datablock mồ côi."""
    current = bpy.context.scene
    for scene in list(bpy.data.scenes):
        if scene is not current:
            bpy.data.scenes.remove(scene)
    bpy.data.orphans_purge(do_local_ids=True, do_linked_ids=True, do_recursive=True)
    _lean_scene(current)


# Scene mặc định của factory-startup: áp ngay. Script sau open_mainfile nên gọi lean_after_load().
for _scene in bpy.data.scenes:
    _lean_scene(_scene)

# Cho script sau nhận prelude đã chạy.
bpy.app.driver_namespace["pig_lean_after_load"] = lean_after_load
os.environ["PIG_BLENDER_LEAN"] = "1"
print("[lean-prelude] undo off, persistent_data off, auto_tile on, cache_limit=%s MB" % prefs.system.memory_cache_limit)
