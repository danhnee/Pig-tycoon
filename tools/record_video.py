"""Quay video gameplay 3-5 phút từ bản build Windows (Builds/PixelMyth/PigTycoon.exe).

Player tự chạy DemoAutopilot + DemoFrameRecorder khi có cờ -pigtycoon-scenario=video: ghi JPG 30 fps vào thư mục
khung hình rồi tự thoát (route xong hoặc 330 s thời gian game). Script này chạy player, đợi, rồi gọi ffmpeg.

  python tools/record_video.py                       # qua tools/headless_runner.py (Job Object, trần RAM)
  python tools/record_video.py --direct              # chạy exe trực tiếp, cửa sổ hiện (nếu runner ẩn cửa sổ làm trắng ảnh)
  python tools/record_video.py --encode-only         # chỉ mã hoá lại từ khung hình đã có

Save cũ (pig_tycoon_save.json trong persistentDataPath) được đổi tên tạm để player bắt đầu ở Farm, rồi trả lại.
Đầu ra: Builds/PixelMyth/gameplay.mp4 (H.264, yuv420p, crf 18, 30 fps). Yêu cầu ffmpeg trên PATH.
"""
from __future__ import annotations

import argparse
import os
import shutil
import subprocess
import sys
import time
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
EXE = Path(os.environ["PIGTYCOON_EXE"]) if os.environ.get("PIGTYCOON_EXE") else ROOT / "Builds" / "PixelMyth" / "PigTycoon.exe"  # PIGTYCOON_EXE: probe the release build
OUT = ROOT / "Builds" / "PixelMyth" / "gameplay.mp4"
FRAMES = ROOT / ".agent_scratch" / "video_frames"
COMPANY, PRODUCT = "DefaultCompany", "pig-tycoon-core"  # ProjectSettings.asset
SAVE_DIR = Path(os.environ.get("USERPROFILE", str(Path.home()))) / "AppData" / "LocalLow" / COMPANY / PRODUCT
FPS = 30


def player_args(frames: Path) -> list[str]:
    # 1920×1080: the camera picks a ×2 whole-pixel scale there (8.4 m half-height, phone framing)
    w, h = os.environ.get("PIGTYCOON_CAPTURE", "1920x1080").split("x")
    return ["-screen-width", w, "-screen-height", h, "-screen-fullscreen", "0",
            f"-pigtycoon-scenario={os.environ.get('PIGTYCOON_SCENARIO', 'video')}", f"-pigtycoon-frames={frames}",
            *os.environ.get("PIGTYCOON_EXTRA_ARGS", "").split()]


def run_player(a: argparse.Namespace) -> int:
    frames = Path(a.frames).resolve()
    shutil.rmtree(frames, ignore_errors=True)
    frames.mkdir(parents=True, exist_ok=True)
    log = ROOT / ".agent_scratch" / "headless" / f"video-{time.strftime('%Y%m%d-%H%M%S')}.log"
    log.parent.mkdir(parents=True, exist_ok=True)
    if a.direct:
        cmd = [str(EXE), "-logFile", str(log), *player_args(frames)]
    else:
        cmd = [sys.executable, str(ROOT / "tools" / "headless_runner.py"), "unity", "--player", str(EXE),
               "--graphics", "--log", str(log),
               # the player needs ~0.5 GB, not the editor's 2.5 GB default floor
               "--min-free-mb", "400", "--max-mem-mb", "3072", "--runtime-min-free-mb", "64",
               "--runtime-low-free-sec", "30", "--wait-sec", "1800", "--", *player_args(frames)]
    print("[record] " + subprocess.list2cmdline(cmd), flush=True)
    save = SAVE_DIR / "pig_tycoon_save.json"
    backup = save.with_suffix(".json.record_bak")
    moved = False
    started = time.time()
    try:
        # rename inside the try: any later failure (even a print) still restores the user's save in finally
        if save.is_file():
            save.replace(backup)
            moved = True
            print(f"[record] save cũ tạm đổi tên: {backup}", flush=True)
        # 330 s game-time ở 30 fps; máy chậm có thể mất vài lần thời gian thật.
        code = subprocess.run(cmd, cwd=ROOT, timeout=a.timeout_min * 60).returncode
    except subprocess.TimeoutExpired:
        print(f"[record] quá {a.timeout_min} phút, dừng player", file=sys.stderr)
        subprocess.run(["taskkill", "/F", "/IM", EXE.name], capture_output=True)
        code = 124
    finally:
        if moved:
            if save.exists():
                save.unlink()  # save do kịch bản video tạo ra
            backup.replace(save)
            print("[record] đã trả save cũ", flush=True)
    print(f"[record] player thoát mã {code} sau {time.time() - started:.0f}s; log: {log}", flush=True)
    if log.is_file():
        lines = [l for l in log.read_text(encoding="utf-8", errors="replace").splitlines() if "[Autopilot]" in l]
        print("[record] %d dòng [Autopilot]; 8 dòng cuối:" % len(lines))
        for l in lines[-8:]:
            print("   " + l)
    return code


def encode(frames: Path) -> None:
    count = len(list(frames.glob("f_*.jpg")))
    if count == 0:
        raise SystemExit(f"[record] không có khung hình trong {frames}")
    ffmpeg = shutil.which("ffmpeg")
    if not ffmpeg:
        raise SystemExit("[record] không thấy ffmpeg trên PATH")
    OUT.parent.mkdir(parents=True, exist_ok=True)
    cmd = [ffmpeg, "-y", "-hide_banner", "-loglevel", "error", "-framerate", str(FPS), "-i", str(frames / "f_%06d.jpg"),
           "-vf", "scale=trunc(iw/2)*2:trunc(ih/2)*2", "-c:v", "libx264", "-pix_fmt", "yuv420p", "-crf", "18",
           "-r", str(FPS), "-movflags", "+faststart", str(OUT)]
    print("[record] " + subprocess.list2cmdline(cmd), flush=True)
    subprocess.run(cmd, check=True)
    # 1080p JPG frames are ~5 GB per run on a nearly full disk: drop them once the video exists
    if os.environ.get("PIGTYCOON_KEEP_FRAMES") != "1":
        shutil.rmtree(frames, ignore_errors=True)
    probe = shutil.which("ffprobe")
    if probe:
        d = subprocess.run([probe, "-v", "error", "-show_entries", "format=duration", "-of", "default=nw=1:nk=1", str(OUT)],
                           capture_output=True, text=True).stdout.strip()
        print(f"[record] {OUT} : {float(d):.1f} s ({count} khung hình / {FPS} fps = {count / FPS:.1f} s), {OUT.stat().st_size / 1e6:.1f} MB")
    else:
        print(f"[record] {OUT} : {count / FPS:.1f} s ({count} khung hình), {OUT.stat().st_size / 1e6:.1f} MB")


def main() -> int:
    # stdout is cp1252 when redirected on Windows; Vietnamese log lines must not crash the recorder
    for stream in (sys.stdout, sys.stderr):
        if hasattr(stream, "reconfigure"):
            stream.reconfigure(encoding="utf-8", errors="replace")
    p = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    p.add_argument("--direct", action="store_true", help="chạy exe trực tiếp thay vì headless_runner")
    p.add_argument("--encode-only", action="store_true")
    p.add_argument("--no-encode", action="store_true", help="chỉ chạy player (vd. PIGTYCOON_SCENARIO=uiprobe), không ghép video")
    p.add_argument("--frames", default=str(FRAMES), help="thư mục khung hình (cũng truyền cho player)")
    p.add_argument("--timeout-min", type=int, default=75, help="trần thời gian thật cho player")
    a = p.parse_args()
    if not a.encode_only:
        if not EXE.is_file():
            raise SystemExit(f"[record] chưa có bản build: {EXE} (chạy PixelMythWorldBuilder.BuildWindows trước)")
        code = run_player(a)
        if code not in (0, 124) and not list(Path(a.frames).glob("f_*.jpg")):
            return code
    if a.no_encode:
        return 0
    encode(Path(a.frames))
    return 0


if __name__ == "__main__":
    sys.exit(main())
