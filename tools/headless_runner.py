"""Chạy Blender / Unity cho agent: ngầm, hàng đợi 1 tiến trình, trần RAM, báo cáo đỉnh bộ nhớ.

Dùng:
  python tools/headless_runner.py blender --script tools/review_observatory.py -- --source X.blend
  python tools/headless_runner.py unity   --method BuildDocumentWorldCandidates.Build
  python tools/headless_runner.py status

Tuỳ chọn chung:
  --max-mem-mb N      trần commit (Job Object). Vượt là cấp phát thất bại, tiến trình thoát lỗi, runner báo exit 137. Blender 3072, Unity 4096.
  --min-free-mb N     chờ tới khi RAM trống >= N trước khi chạy (Blender 1200, Unity 2500).
  --wait-sec N        chờ lock/RAM tối đa (mặc định 900).
  --no-lean           Blender: không chèn tools/blender_lean_prelude.py.
  --dry-run           chỉ in lệnh.
  --report PATH       JSON báo cáo (mặc định .agent_scratch/headless/<ts>-<mode>.json).
  --output PATH       (lặp được) file/thư mục script sẽ tạo. Cần cho cache: job chỉ bị bỏ qua khi lần trước
                      thoát 0 cùng khoá và mọi output còn nguyên (sha256). Không khai báo = không bao giờ hit.
  --no-cache          không đọc/ghi cache.   --force  chạy lại dù hit.
Cache: .agent_scratch/headless/cache/<sha256>.json. Khoá = chế độ + lệnh (trừ log/report) + nội dung script
+ prelude + mọi file đầu vào xuất hiện trong tham số + bản Blender/Unity.

Thiết kế: Docs/plans/2026-10-23-headless-render-runner.md.
"""
from __future__ import annotations

import argparse
import ctypes
import ctypes.wintypes as wt
import hashlib
import json
import os
import subprocess
import sys
import time
from datetime import datetime
from pathlib import Path

for _stream in (sys.stdout, sys.stderr):  # console cp1252 không in được tiếng Việt
    try:
        _stream.reconfigure(encoding="utf-8", errors="replace")
    except Exception:
        pass

ROOT = Path(__file__).resolve().parents[1]
BLENDER_EXE = Path(os.environ.get("PIG_TYCOON_BLENDER_EXE", r"F:\Tools\blender-5.3.0-windows-x64\blender.exe"))
UNITY_EXE = Path(os.environ.get("PIG_TYCOON_UNITY_EXE", r"D:\Unity\Editor\6000.6.3f1\Editor\Unity.exe"))
UNITY_VERSION = "6000.6.3f1"  # AGENTS.md: project khóa bản này
SCRATCH = ROOT / ".agent_scratch" / "headless"
LOCK = SCRATCH / "heavy-process.lock"
PRELUDE = ROOT / "tools" / "blender_lean_prelude.py"
CACHE = SCRATCH / "cache"

DEFAULTS = {
    "blender": {"max_mem_mb": 3072, "min_free_mb": 1200},
    "unity": {"max_mem_mb": 4096, "min_free_mb": 2500},
}

# ---------- Win32: Job Object (trần RAM + giết con khi runner chết) ----------
JOB_OBJECT_LIMIT_PROCESS_MEMORY = 0x00000100
JOB_OBJECT_LIMIT_KILL_ON_JOB_CLOSE = 0x00002000
JobObjectExtendedLimitInformation = 9


class IO_COUNTERS(ctypes.Structure):
    _fields_ = [(n, ctypes.c_ulonglong) for n in (
        "ReadOperationCount", "WriteOperationCount", "OtherOperationCount",
        "ReadTransferCount", "WriteTransferCount", "OtherTransferCount")]


class JOBOBJECT_BASIC_LIMIT_INFORMATION(ctypes.Structure):
    _fields_ = [
        ("PerProcessUserTimeLimit", ctypes.c_longlong),
        ("PerJobUserTimeLimit", ctypes.c_longlong),
        ("LimitFlags", wt.DWORD),
        ("MinimumWorkingSetSize", ctypes.c_size_t),
        ("MaximumWorkingSetSize", ctypes.c_size_t),
        ("ActiveProcessLimit", wt.DWORD),
        ("Affinity", ctypes.c_size_t),
        ("PriorityClass", wt.DWORD),
        ("SchedulingClass", wt.DWORD),
    ]


class JOBOBJECT_EXTENDED_LIMIT_INFORMATION(ctypes.Structure):
    _fields_ = [
        ("BasicLimitInformation", JOBOBJECT_BASIC_LIMIT_INFORMATION),
        ("IoInfo", IO_COUNTERS),
        ("ProcessMemoryLimit", ctypes.c_size_t),
        ("JobMemoryLimit", ctypes.c_size_t),
        ("PeakProcessMemoryUsed", ctypes.c_size_t),
        ("PeakJobMemoryUsed", ctypes.c_size_t),
    ]


def make_job(max_mem_mb: int):
    k32 = ctypes.windll.kernel32
    job = k32.CreateJobObjectW(None, None)
    if not job:
        raise ctypes.WinError()
    info = JOBOBJECT_EXTENDED_LIMIT_INFORMATION()
    info.BasicLimitInformation.LimitFlags = JOB_OBJECT_LIMIT_PROCESS_MEMORY | JOB_OBJECT_LIMIT_KILL_ON_JOB_CLOSE
    info.ProcessMemoryLimit = max_mem_mb * 1024 * 1024
    if not k32.SetInformationJobObject(job, JobObjectExtendedLimitInformation, ctypes.byref(info), ctypes.sizeof(info)):
        raise ctypes.WinError()
    return job


def assign_job(job, handle) -> bool:
    return bool(ctypes.windll.kernel32.AssignProcessToJobObject(job, handle))


# ---------- tiện ích ----------
def free_mb() -> int:
    import psutil
    return int(psutil.virtual_memory().available / (1024 * 1024))


def running(name: str) -> list[int]:
    import psutil
    return [p.pid for p in psutil.process_iter(["name"]) if (p.info["name"] or "").lower() == name.lower()]


def acquire_lock(wait_sec: int, label: str) -> None:
    SCRATCH.mkdir(parents=True, exist_ok=True)
    import psutil
    deadline = time.time() + wait_sec
    while True:
        try:
            fd = os.open(LOCK, os.O_CREAT | os.O_EXCL | os.O_WRONLY)
            os.write(fd, f"{os.getpid()} {label} {datetime.now().isoformat()}".encode())
            os.close(fd)
            return
        except FileExistsError:
            try:
                owner = int(LOCK.read_text().split()[0])
            except Exception:
                owner = -1
            if owner > 0 and not psutil.pid_exists(owner):
                LOCK.unlink(missing_ok=True)  # lock mồ côi
                continue
            if time.time() > deadline:
                raise SystemExit(f"[runner] lock busy by pid {owner} > {wait_sec}s: {LOCK}")
            time.sleep(5)


def release_lock() -> None:
    try:
        if LOCK.exists() and LOCK.read_text().split()[0] == str(os.getpid()):
            LOCK.unlink()
    except Exception:
        pass


def wait_free_ram(min_free: int, wait_sec: int) -> int:
    deadline = time.time() + wait_sec
    while True:
        f = free_mb()
        if f >= min_free:
            return f
        if time.time() > deadline:
            raise SystemExit(f"[runner] free RAM {f} MB < {min_free} MB sau {wait_sec}s. Đóng bớt app rồi chạy lại.")
        print(f"[runner] free RAM {f} MB < {min_free} MB, chờ...", flush=True)
        time.sleep(10)


# ---------- cache theo hash ----------
def _sha_file(path: Path) -> str:
    h = hashlib.sha256()
    with path.open("rb") as f:
        for chunk in iter(lambda: f.read(1 << 20), b""):
            h.update(chunk)
    return h.hexdigest()


def _sha_tree(path: Path) -> str:
    """Thư mục: hash tên + nội dung từng file, thứ tự ổn định."""
    h = hashlib.sha256()
    for f in sorted(q for q in path.rglob("*") if q.is_file()):
        h.update(str(f.relative_to(path)).encode("utf-8"))
        h.update(_sha_file(f).encode())
    return h.hexdigest()


def _sha_path(path: Path) -> str | None:
    if path.is_file():
        return _sha_file(path)
    if path.is_dir():
        return _sha_tree(path)
    return None


def _input_files(cmd: list[str], outputs: list[Path]) -> dict[str, str]:
    """Tham số nào là file/thư mục đang tồn tại thì coi là đầu vào; bỏ output, log, và -projectPath = repo."""
    out_set = {o.resolve() for o in outputs}
    found: dict[str, str] = {}
    for arg in cmd[1:]:
        q = Path(arg)
        if not q.exists():
            continue
        rq = q.resolve()
        if rq in out_set or rq.suffix.lower() == ".log":
            continue
        if rq.is_dir() and rq == ROOT.resolve():
            continue
        digest = _sha_path(rq)
        if digest:
            found[str(rq)] = digest
    return found


def cache_key(mode: str, cmd: list[str], outputs: list[Path]) -> tuple[str, dict]:
    exe = Path(cmd[0])
    st = exe.stat()
    argv = []
    for arg in cmd[1:]:
        q = Path(arg)
        if q.suffix.lower() in (".log", ".json") and not q.exists():  # log/report do runner đặt tên theo giờ
            continue
        argv.append(arg)
    parts = {
        "mode": mode,
        "exe": {"path": str(exe), "size": st.st_size, "mtime": int(st.st_mtime)},
        "argv": argv,
        "inputs": _input_files(cmd, outputs),
        "outputs": [str(o.resolve()) for o in outputs],
    }
    key = hashlib.sha256(json.dumps(parts, sort_keys=True, ensure_ascii=False).encode("utf-8")).hexdigest()
    return key, parts


def cache_lookup(key: str, outputs: list[Path]) -> dict | None:
    """Hit khi: có bản ghi, lần trước exit 0, và mọi output khai báo còn đúng sha."""
    entry = CACHE / f"{key}.json"
    if not entry.exists() or not outputs:
        return None
    try:
        rec = json.loads(entry.read_text(encoding="utf-8"))
    except Exception:
        return None
    if rec.get("exit_code") != 0 or not rec.get("output_sha"):
        return None
    for path, digest in rec["output_sha"].items():
        if _sha_path(Path(path)) != digest:
            return None
    return rec


def cache_store(key: str, parts: dict, rep: dict, outputs: list[Path]) -> dict[str, str]:
    CACHE.mkdir(parents=True, exist_ok=True)
    output_sha: dict[str, str] = {}
    for o in outputs:
        d = _sha_path(o)
        if d is None:
            rep.setdefault("missing_outputs", []).append(str(o))
        else:
            output_sha[str(o.resolve())] = d
    rec = {"key": key, "parts": parts, "exit_code": rep["exit_code"], "report": rep.get("report"),
           "output_sha": output_sha, "stored": datetime.now().isoformat(),
           "peak_private_mb": rep["peak_private_mb"], "elapsed_sec": rep["elapsed_sec"]}
    (CACHE / f"{key}.json").write_text(json.dumps(rec, ensure_ascii=False, indent=2), encoding="utf-8")
    return output_sha


# ---------- lệnh ----------
def blender_cmd(a) -> list[str]:
    if not BLENDER_EXE.is_file():
        raise SystemExit(f"[runner] không thấy Blender: {BLENDER_EXE} (đặt PIG_TYCOON_BLENDER_EXE)")
    cmd = [str(BLENDER_EXE), "--background", "--factory-startup", "--python-exit-code", "1"]
    if a.threads:
        cmd += ["--threads", str(a.threads)]
    if not a.no_lean:
        cmd += ["--python", str(PRELUDE)]
    cmd += ["--python", str(Path(a.script).resolve())]
    if a.script_args:
        cmd += ["--", *a.script_args]
    return cmd


def unity_cmd(a, log: Path) -> list[str]:
    if not UNITY_EXE.is_file():
        raise SystemExit(f"[runner] không thấy Unity: {UNITY_EXE}")
    if UNITY_VERSION not in str(UNITY_EXE):
        raise SystemExit(f"[runner] Unity phải là {UNITY_VERSION} (AGENTS.md), đang trỏ {UNITY_EXE}")
    cmd = [str(UNITY_EXE), "-batchmode", "-nographics", "-quit", "-silent-crashes",
           "-projectPath", str(Path(a.project).resolve()), "-logFile", str(log)]
    if a.method:
        cmd += ["-executeMethod", a.method]
    if a.script_args:
        cmd += list(a.script_args)
    return cmd


def run(cmd: list[str], a, log: Path, report: Path, mode: str) -> tuple[int, dict]:
    import psutil
    env = dict(os.environ, PYTHONIOENCODING="utf-8", PIG_HEADLESS_RUNNER="1")
    if mode == "blender":
        env.setdefault("PIG_BLENDER_TILE", "1024")
    log.parent.mkdir(parents=True, exist_ok=True)
    job = make_job(a.max_mem_mb)
    t0 = time.time()
    with log.open("ab") as lf:
        lf.write(("=== %s ===\n%s\n" % (datetime.now().isoformat(), subprocess.list2cmdline(cmd))).encode("utf-8"))
        lf.flush()
        proc = subprocess.Popen(
            cmd, cwd=str(ROOT), env=env,
            stdout=lf if mode == "blender" else subprocess.DEVNULL, stderr=subprocess.STDOUT,
            creationflags=subprocess.CREATE_NO_WINDOW | subprocess.BELOW_NORMAL_PRIORITY_CLASS,
        )
        assign_job(job, int(proc._handle))  # type: ignore[attr-defined]
        ps = psutil.Process(proc.pid)
        peak_ws = peak_priv = 0
        samples = 0
        while proc.poll() is None:
            try:
                mi = ps.memory_info()
                peak_ws = max(peak_ws, mi.rss)
                peak_priv = max(peak_priv, mi.private)
                for child in ps.children(recursive=True):  # Unity sinh con (shader compiler, bee)
                    try:
                        cmi = child.memory_info()
                        peak_ws = max(peak_ws, cmi.rss)
                        peak_priv = max(peak_priv, cmi.private)
                    except psutil.Error:
                        pass
                samples += 1
            except psutil.Error:
                pass
            time.sleep(0.5)
    code = proc.returncode
    # Job Object không giết: nó làm malloc thất bại, Blender/Unity thoát lỗi. Nhận diện qua đỉnh commit sát trần.
    killed_by_cap = code != 0 and peak_priv >= a.max_mem_mb * 1024 * 1024 * 0.9
    rep = {
        "mode": mode, "exit_code": code, "killed_by_memory_cap": killed_by_cap,
        "max_mem_mb": a.max_mem_mb, "peak_ws_mb": peak_ws // 2**20, "peak_private_mb": peak_priv // 2**20,
        "elapsed_sec": round(time.time() - t0, 1), "samples": samples, "log": str(log), "cmd": cmd,
        "started": datetime.fromtimestamp(t0).isoformat(), "report": str(report), "cache_hit": False,
    }
    report.parent.mkdir(parents=True, exist_ok=True)
    report.write_text(json.dumps(rep, ensure_ascii=False, indent=2), encoding="utf-8")
    tail = ""
    if a.tail > 0 and log.exists():
        tail = "\n".join(log.read_text(encoding="utf-8", errors="replace").splitlines()[-a.tail:])
    print(f"[runner] {mode} exit={code} peak_private={rep['peak_private_mb']}MB peak_ws={rep['peak_ws_mb']}MB "
          f"elapsed={rep['elapsed_sec']}s report={report}")
    if killed_by_cap:
        print(f"[runner] BỊ GIẾT vì vượt trần {a.max_mem_mb} MB. Tăng --max-mem-mb hoặc chia nhỏ cảnh.")
    if tail:
        print(f"[runner] log tail ({a.tail} dòng):\n{tail}")
    return (137 if killed_by_cap else code), rep


def status() -> None:
    info = {
        "free_mb": free_mb(), "lock": LOCK.read_text() if LOCK.exists() else None,
        "blender_pids": running("blender.exe"), "unity_pids": running("Unity.exe"),
        "blender_exe": str(BLENDER_EXE), "blender_exe_ok": BLENDER_EXE.is_file(),
        "unity_exe": str(UNITY_EXE), "unity_exe_ok": UNITY_EXE.is_file(),
    }
    print(json.dumps(info, ensure_ascii=False, indent=2))


def main(argv=None) -> int:
    p = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    sub = p.add_subparsers(dest="mode", required=True)
    for name in ("blender", "unity"):
        s = sub.add_parser(name)
        s.add_argument("--max-mem-mb", type=int, default=DEFAULTS[name]["max_mem_mb"])
        s.add_argument("--min-free-mb", type=int, default=DEFAULTS[name]["min_free_mb"])
        s.add_argument("--wait-sec", type=int, default=900)
        s.add_argument("--dry-run", action="store_true")
        s.add_argument("--report", type=Path)
        s.add_argument("--log", type=Path)
        s.add_argument("--tail", type=int, default=25)
        s.add_argument("--output", type=Path, action="append", default=[], help="file/thư mục script tạo ra (lặp được)")
        s.add_argument("--no-cache", action="store_true")
        s.add_argument("--force", action="store_true", help="chạy lại dù cache hit")
        s.add_argument("script_args", nargs="*")
    b = sub.choices["blender"]
    b.add_argument("--script", required=True)
    b.add_argument("--threads", type=int, default=0, help="0 = Blender tự chọn")
    b.add_argument("--no-lean", action="store_true")
    u = sub.choices["unity"]
    u.add_argument("--method", help="Namespace.Class.Method tĩnh trong Assets/Editor")
    u.add_argument("--project", default=str(ROOT))
    sub.add_parser("status")
    a = p.parse_args(argv)

    if a.mode == "status":
        status()
        return 0

    ts = datetime.now().strftime("%Y%m%d-%H%M%S")
    log = a.log or SCRATCH / f"{ts}-{a.mode}.log"
    report = a.report or SCRATCH / f"{ts}-{a.mode}.json"
    cmd = blender_cmd(a) if a.mode == "blender" else unity_cmd(a, log)
    key = parts = None
    if not a.no_cache:
        key, parts = cache_key(a.mode, cmd, a.output)
        hit = None if a.force else cache_lookup(key, a.output)
        if a.dry_run:
            print(subprocess.list2cmdline(cmd))
            print(f"[runner] cache key {key} hit={'yes' if hit else 'no'} inputs={list(parts['inputs'])}")
            return 0
        if hit:
            rep = {"mode": a.mode, "exit_code": 0, "cache_hit": True, "cache_key": key,
                   "cached_report": hit.get("report"), "cached_at": hit.get("stored"),
                   "outputs": list(hit.get("output_sha", {})), "cmd": cmd,
                   "saved_elapsed_sec": hit.get("elapsed_sec"), "saved_peak_private_mb": hit.get("peak_private_mb")}
            report.parent.mkdir(parents=True, exist_ok=True)
            report.write_text(json.dumps(rep, ensure_ascii=False, indent=2), encoding="utf-8")
            print(f"[runner] CACHE HIT {key[:12]} (lưu {hit.get('stored')}, tiết kiệm ~{hit.get('elapsed_sec')}s, "
                  f"đỉnh {hit.get('peak_private_mb')} MB). Output còn nguyên: {len(rep['outputs'])}. report={report}")
            return 0
        if not a.output:
            print("[runner] cache: không có --output nên lần sau không thể hit (chỉ ghi khoá).", flush=True)
    if a.dry_run:
        print(subprocess.list2cmdline(cmd))
        return 0

    if a.mode == "unity" and running("Unity.exe"):
        raise SystemExit("[runner] Unity.exe đang chạy (Editor mở?). AGENTS.md: tắt Editor trước.")
    if a.mode == "blender" and running("Unity.exe"):
        print("[runner] cảnh báo: Unity.exe đang chạy, RAM trống sẽ thấp.", flush=True)

    acquire_lock(a.wait_sec, a.mode)
    try:
        f = wait_free_ram(a.min_free_mb, a.wait_sec)
        print(f"[runner] free RAM {f} MB, trần {a.max_mem_mb} MB, lệnh: {subprocess.list2cmdline(cmd)}", flush=True)
        code, rep = run(cmd, a, log, report, a.mode)
        if key and not a.no_cache:
            rep["cache_key"] = key
            rep["output_sha"] = cache_store(key, parts, rep, a.output)
            report.write_text(json.dumps(rep, ensure_ascii=False, indent=2), encoding="utf-8")
            if rep.get("missing_outputs"):
                print(f"[runner] cảnh báo: output khai báo không thấy sau khi chạy: {rep['missing_outputs']}")
        return code
    finally:
        release_lock()


if __name__ == "__main__":
    sys.exit(main())
