"""Chạy Blender / Unity cho agent: ngầm, hàng đợi 1 tiến trình, trần RAM, báo cáo đỉnh bộ nhớ.

Dùng:
  python tools/headless_runner.py blender --script tools/review_observatory.py -- --source X.blend
  python tools/headless_runner.py unity   --method BuildDocumentWorldCandidates.Build
  python tools/headless_runner.py status

Tuỳ chọn chung:
  --max-mem-mb N      trần commit TỔNG Job Object, kể cả mọi tiến trình con. Blender 3072, Unity 4096.
  --min-free-mb N     chờ tới khi RAM trống >= N trước khi chạy (Blender 1200, Unity 2500).
  --runtime-min-free-mb N   dừng toàn job nếu RAM trống dưới N (mặc định 256 MB).
  --runtime-low-free-sec N  thời gian thiếu RAM liên tục trước khi dừng (mặc định 5 giây).
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
import math
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
JOB_OBJECT_LIMIT_JOB_MEMORY = 0x00000200
JOB_OBJECT_LIMIT_KILL_ON_JOB_CLOSE = 0x00002000
JobObjectExtendedLimitInformation = 9
_K32 = None


def kernel32():
    """HANDLE/ULONG_PTR must stay pointer-sized on 64-bit Windows."""
    global _K32
    if _K32 is None:
        if os.name != "nt":
            raise OSError("Windows Job Object enforcement is required; refusing unprotected launch")
        k = ctypes.WinDLL("kernel32", use_last_error=True)
        signatures = {
            "CreateJobObjectW": ([wt.LPVOID, wt.LPCWSTR], wt.HANDLE),
            "SetInformationJobObject": ([wt.HANDLE, ctypes.c_int, wt.LPVOID, wt.DWORD], wt.BOOL),
            "QueryInformationJobObject": ([wt.HANDLE, ctypes.c_int, wt.LPVOID, wt.DWORD, ctypes.POINTER(wt.DWORD)], wt.BOOL),
            "AssignProcessToJobObject": ([wt.HANDLE, wt.HANDLE], wt.BOOL),
            "IsProcessInJob": ([wt.HANDLE, wt.HANDLE, ctypes.POINTER(wt.BOOL)], wt.BOOL),
            "TerminateJobObject": ([wt.HANDLE, wt.UINT], wt.BOOL),
            "TerminateProcess": ([wt.HANDLE, wt.UINT], wt.BOOL),
            "GetExitCodeProcess": ([wt.HANDLE, ctypes.POINTER(wt.DWORD)], wt.BOOL),
            "WaitForSingleObject": ([wt.HANDLE, wt.DWORD], wt.DWORD),
            "ResumeThread": ([wt.HANDLE], wt.DWORD),
            "CloseHandle": ([wt.HANDLE], wt.BOOL),
            "CreateIoCompletionPort": ([wt.HANDLE, wt.HANDLE, ctypes.c_size_t, wt.DWORD], wt.HANDLE),
            "GetQueuedCompletionStatus": ([wt.HANDLE, ctypes.POINTER(wt.DWORD), ctypes.POINTER(ctypes.c_size_t), ctypes.POINTER(wt.LPVOID), wt.DWORD], wt.BOOL),
        }
        for name, (args, result) in signatures.items():
            function = getattr(k, name)
            function.argtypes, function.restype = args, result
        k.CreateProcessW.argtypes = [wt.LPCWSTR, wt.LPWSTR, wt.LPVOID, wt.LPVOID, wt.BOOL,
                                    wt.DWORD, wt.LPVOID, wt.LPCWSTR,
                                    ctypes.POINTER(STARTUPINFOW), ctypes.POINTER(PROCESS_INFORMATION)]
        k.CreateProcessW.restype = wt.BOOL
        _K32 = k
    return _K32


class STARTUPINFOW(ctypes.Structure):
    _fields_ = [("cb", wt.DWORD), ("lpReserved", wt.LPWSTR), ("lpDesktop", wt.LPWSTR), ("lpTitle", wt.LPWSTR)] + [
        (name, wt.DWORD) for name in ("dwX", "dwY", "dwXSize", "dwYSize", "dwXCountChars", "dwYCountChars", "dwFillAttribute", "dwFlags")
    ] + [("wShowWindow", wt.WORD), ("cbReserved2", wt.WORD), ("lpReserved2", wt.LPVOID),
         ("hStdInput", wt.HANDLE), ("hStdOutput", wt.HANDLE), ("hStdError", wt.HANDLE)]


class PROCESS_INFORMATION(ctypes.Structure):
    _fields_ = [("hProcess", wt.HANDLE), ("hThread", wt.HANDLE), ("dwProcessId", wt.DWORD), ("dwThreadId", wt.DWORD)]


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
    if max_mem_mb <= 0:
        raise ValueError("max_mem_mb must be positive")
    k32 = kernel32()
    job = k32.CreateJobObjectW(None, None)
    if not job:
        raise ctypes.WinError()
    info = JOBOBJECT_EXTENDED_LIMIT_INFORMATION()
    info.BasicLimitInformation.LimitFlags = JOB_OBJECT_LIMIT_JOB_MEMORY | JOB_OBJECT_LIMIT_KILL_ON_JOB_CLOSE
    info.JobMemoryLimit = max_mem_mb * 1024 * 1024
    if not k32.SetInformationJobObject(job, JobObjectExtendedLimitInformation, ctypes.byref(info), ctypes.sizeof(info)):
        error = ctypes.get_last_error()
        k32.CloseHandle(job)
        raise ctypes.WinError(error)
    return job


def job_limits(job):
    info = JOBOBJECT_EXTENDED_LIMIT_INFORMATION()
    if not kernel32().QueryInformationJobObject(job, 9, ctypes.byref(info), ctypes.sizeof(info), None):
        raise ctypes.WinError()
    return info


def assign_job(job, handle) -> bool:
    return bool(kernel32().AssignProcessToJobObject(job, handle))


def job_members(job):
    capacity = 64
    while capacity <= 65536:
        buffer = ctypes.create_string_buffer(8 + capacity * ctypes.sizeof(ctypes.c_size_t))
        if kernel32().QueryInformationJobObject(job, 3, buffer, len(buffer), None):
            count = wt.DWORD.from_buffer(buffer, 4).value
            return [ctypes.c_size_t.from_buffer(buffer, 8 + i * ctypes.sizeof(ctypes.c_size_t)).value for i in range(count)]
        if ctypes.get_last_error() != 234:  # ERROR_MORE_DATA
            raise ctypes.WinError()
        capacity *= 2
    raise OSError("Job process list exceeded bounded query capacity")


def completion_port(job):
    k = kernel32()
    port = k.CreateIoCompletionPort(wt.HANDLE(-1), None, 0, 1)
    if not port:
        raise ctypes.WinError()
    association = (wt.LPVOID * 2)(None, port)
    if not k.SetInformationJobObject(job, 7, ctypes.byref(association), ctypes.sizeof(association)):
        error = ctypes.get_last_error()
        k.CloseHandle(port)
        raise ctypes.WinError(error)
    return port


def job_messages(port):
    messages = []
    while True:
        message, key, detail = wt.DWORD(), ctypes.c_size_t(), wt.LPVOID()
        if not kernel32().GetQueuedCompletionStatus(port, ctypes.byref(message), ctypes.byref(key), ctypes.byref(detail), 0):
            if ctypes.get_last_error() == 258:  # WAIT_TIMEOUT
                return messages
            raise ctypes.WinError()
        messages.append(message.value)


class SuspendedProcess:
    def __init__(self, info):
        self._handle, self._thread, self.pid = info.hProcess, info.hThread, info.dwProcessId
        self.returncode = None

    def resume(self):
        if kernel32().ResumeThread(self._thread) == 0xFFFFFFFF:
            raise ctypes.WinError()
        kernel32().CloseHandle(self._thread)
        self._thread = None

    def poll(self):
        if self.returncode is None:
            code = wt.DWORD()
            if not kernel32().GetExitCodeProcess(self._handle, ctypes.byref(code)):
                raise ctypes.WinError()
            if code.value != 259:  # STILL_ACTIVE
                self.returncode = code.value
        return self.returncode

    def wait(self):
        if kernel32().WaitForSingleObject(self._handle, 10000) != 0:
            raise OSError("Owned process did not exit within cleanup deadline")
        return self.poll()

    def close(self):
        for handle in (self._thread, self._handle):
            if handle and not kernel32().CloseHandle(handle):
                raise ctypes.WinError()
        self._thread = self._handle = None


def start_suspended(cmd, env, stdout):
    """Create hidden/suspended. Caller MUST attach and verify the job before resume."""
    import msvcrt
    info, startup = PROCESS_INFORMATION(), STARTUPINFOW()
    startup.cb, startup.dwFlags = ctypes.sizeof(startup), 0x100  # STARTF_USESTDHANDLES
    with open(os.devnull, "rb") as stdin:
        handles = [msvcrt.get_osfhandle(stdin.fileno()), msvcrt.get_osfhandle(stdout.fileno())]
        inherited = [(handle, os.get_handle_inheritable(handle)) for handle in handles]
        try:
            for handle, _ in inherited:
                os.set_handle_inheritable(handle, True)
            startup.hStdInput, startup.hStdOutput, startup.hStdError = handles[0], handles[1], handles[1]
            environment = ctypes.create_unicode_buffer("\0".join(f"{key}={value}" for key, value in sorted(env.items())) + "\0\0")
            command = ctypes.create_unicode_buffer(subprocess.list2cmdline(cmd))
            flags = subprocess.CREATE_NO_WINDOW | subprocess.BELOW_NORMAL_PRIORITY_CLASS | 0x4 | 0x400
            if not kernel32().CreateProcessW(cmd[0], command, None, None, True, flags,
                                             environment, str(ROOT), ctypes.byref(startup), ctypes.byref(info)):
                raise ctypes.WinError()
        finally:
            try:
                for handle, previous in inherited:
                    os.set_handle_inheritable(handle, previous)
            except BaseException:
                # CreateProcess may already have succeeded. Do not strand an
                # unassigned suspended process if parent-handle restoration fails.
                if info.hProcess:
                    kernel32().TerminateProcess(info.hProcess, 1)
                    kernel32().WaitForSingleObject(info.hProcess, 10000)
                    kernel32().CloseHandle(info.hProcess)
                    kernel32().CloseHandle(info.hThread)
                raise
    return SuspendedProcess(info)


def sample_job_members(pids):
    import psutil
    working_set = private = unreadable = 0
    for pid in pids:
        try:
            memory = psutil.Process(pid).memory_info()
            working_set += memory.rss
            private += memory.private
        except psutil.NoSuchProcess:
            pass
        except psutil.Error:
            unreadable += 1
    return working_set, private, unreadable


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
    player_arg = getattr(a, "player", None)
    if player_arg:
        player = Path(player_arg).resolve()
        data = player.parent / f"{player.stem}_Data"
        if not player.is_relative_to(ROOT.resolve()) or player.suffix.lower() != ".exe":
            raise SystemExit("[runner] standalone player phải là .exe trong workspace")
        if not player.is_file() or not (data / "globalgamemanagers").is_file() or not (player.parent / "UnityPlayer.dll").is_file():
            raise SystemExit(f"[runner] Unity player thiếu executable/data/UnityPlayer.dll: {player}")
        if a.method:
            raise SystemExit("[runner] --player không dùng --method")
        render_flags = [] if getattr(a, "graphics", False) else ["-batchmode", "-nographics"]
        return [str(player), *render_flags, "-silent-crashes", "-logFile", str(log), *a.script_args]
    if getattr(a, "graphics", False):
        raise SystemExit("[runner] --graphics chỉ dùng với standalone --player")
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
    env = dict(os.environ, PYTHONIOENCODING="utf-8", PIG_HEADLESS_RUNNER="1")
    if mode == "blender":
        env.setdefault("PIG_BLENDER_TILE", "1024")
    log.parent.mkdir(parents=True, exist_ok=True)
    t0 = time.time()
    job = port = proc = None
    assigned = resumed = killed_by_cap = killed_by_free = False
    peak_ws = peak_sampled_private = peak_job_private = peak_members = samples = unreadable = 0
    runtime_min_free = getattr(a, "runtime_min_free_mb", 256)
    runtime_low_sec = getattr(a, "runtime_low_free_sec", 5.0)
    minimum_free = None
    low_since = None
    events = set()
    reason = error = None
    cleanup_errors = []
    raw_code, code = None, 1
    try:
        if runtime_min_free <= 0 or not math.isfinite(runtime_low_sec) or runtime_low_sec < 0:
            raise ValueError("Runtime free-RAM guard requires positive MB and finite nonnegative grace")
        job = make_job(a.max_mem_mb)
        limits = job_limits(job)
        if not (limits.BasicLimitInformation.LimitFlags & JOB_OBJECT_LIMIT_JOB_MEMORY) or limits.JobMemoryLimit != a.max_mem_mb * 2**20:
            raise OSError("Aggregate job memory enforcement was not configured")
        port = completion_port(job)
        with log.open("ab") as lf, open(os.devnull, "wb") as null:
            lf.write(("=== %s ===\n%s\n" % (datetime.now().isoformat(), subprocess.list2cmdline(cmd))).encode("utf-8"))
            lf.flush()
            proc = start_suspended(cmd, env, lf if mode == "blender" else null)
            # No child user code can run until both assignment and membership succeed.
            if not assign_job(job, proc._handle):
                raise ctypes.WinError(ctypes.get_last_error() or 5)
            contained = wt.BOOL()
            if not kernel32().IsProcessInJob(proc._handle, job, ctypes.byref(contained)) or not contained.value:
                raise OSError("Process job membership could not be verified")
            assigned = True
            proc.resume()
            resumed = True
            while True:
                events.update(job_messages(port))
                members = job_members(job)
                working_set, private, inaccessible = sample_job_members(members)
                peak_ws, peak_sampled_private = max(peak_ws, working_set), max(peak_sampled_private, private)
                peak_members, unreadable = max(peak_members, len(members)), unreadable + inaccessible
                peak_job_private = max(peak_job_private, job_limits(job).PeakJobMemoryUsed)
                samples += 1
                raw_code = proc.poll()
                if not members:
                    # The root may exit before its descendants. Wait for the whole job.
                    raw_code = proc.wait()
                    events.update(job_messages(port))
                    killed_by_cap = bool(events.intersection({9, 10}))
                    reason = "JOB_MEMORY_LIMIT" if killed_by_cap else "CHILD_EXIT"
                    code = 137 if killed_by_cap else raw_code
                    break
                free = free_mb()
                minimum_free = free if minimum_free is None else min(minimum_free, free)
                now = time.monotonic()
                low_since = (now if low_since is None else low_since) if free < runtime_min_free else None
                if events.intersection({9, 10}):  # PROCESS_MEMORY_LIMIT / JOB_MEMORY_LIMIT
                    killed_by_cap, reason = True, "JOB_MEMORY_LIMIT"
                elif low_since is not None and now - low_since >= runtime_low_sec:
                    killed_by_free, reason = True, "LOW_FREE_RAM"
                if killed_by_cap or killed_by_free:
                    if not kernel32().TerminateJobObject(job, 137):
                        raise ctypes.WinError()
                    code = 137
                    break
                if raw_code not in (None, 0):
                    reason, code = "CHILD_EXIT", raw_code
                    break
                time.sleep(0.5)
    except Exception as failure:
        reason = "MONITORING_FAILED" if resumed else "JOB_ENFORCEMENT_FAILED"
        error, code = str(failure), 1
    except KeyboardInterrupt:
        reason, code = "CANCELLED", 130
    finally:
        # Assignment can fail while the process is suspended and outside our job.
        # Kill that specific owned process; otherwise terminate the complete job.
        if proc is not None:
            try:
                if assigned:
                    if not kernel32().TerminateJobObject(job, code):
                        raise ctypes.WinError()
                elif proc.poll() is None:
                    if not kernel32().TerminateProcess(proc._handle, 1):
                        raise ctypes.WinError()
                raw_code = proc.wait()
            except Exception as failure:
                cleanup_errors.append(str(failure))
        if job:
            try:
                peak_job_private = max(peak_job_private, job_limits(job).PeakJobMemoryUsed)
            except Exception as failure:
                cleanup_errors.append(str(failure))
        for handle in (job, port):
            if handle and not kernel32().CloseHandle(handle):
                cleanup_errors.append(str(ctypes.WinError()))
        if proc is not None:
            try:
                proc.close()
            except Exception as failure:
                cleanup_errors.append(str(failure))
    if cleanup_errors and code == 0:
        reason, code = "CLEANUP_FAILED", 1
    rep = {
        "mode": mode, "exit_code": code, "killed_by_memory_cap": killed_by_cap,
        "raw_exit_code": raw_code, "termination_reason": reason,
        "job_assigned": assigned, "child_resumed": resumed, "enforcement_error": error,
        "cleanup_errors": cleanup_errors, "memory_enforcement": "Win32 aggregate job committed memory",
        "max_mem_mb": a.max_mem_mb, "peak_ws_mb": math.ceil(peak_ws / 2**20),
        "peak_private_mb": math.ceil(peak_job_private / 2**20), "peak_private_bytes": peak_job_private,
        "peak_private_source": "Win32 PeakJobMemoryUsed", "peak_member_count": peak_members,
        "peak_sampled_private_mb": math.ceil(peak_sampled_private / 2**20),
        "peak_ws_source": "sampled sum of job-member working sets; shared pages may repeat",
        "unreadable_process_samples": unreadable, "job_notifications": sorted(events),
        "memory_cap_notification_delivery_guaranteed": False,
        "memory_cap_suspected": reason == "CHILD_EXIT" and code != 0 and not killed_by_cap and peak_job_private >= a.max_mem_mb * 2**20 * .9,
        "killed_by_free_ram_guard": killed_by_free, "minimum_free_mb": minimum_free,
        "runtime_min_free_mb": runtime_min_free, "runtime_low_free_sec": runtime_low_sec,
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
        print(f"[runner] job chạm trần tổng {a.max_mem_mb} MB; đã dừng. Không tự tăng trần; chia nhỏ job hoặc đánh giá lại cấu hình.")
    if killed_by_free:
        print(f"[runner] dừng toàn job: RAM trống dưới {runtime_min_free} MB liên tục {runtime_low_sec}s.")
    if error:
        print(f"[runner] {reason}: {error}")
    if tail:
        print(f"[runner] log tail ({a.tail} dòng):\n{tail}")
    return code, rep


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
        s.add_argument("--runtime-min-free-mb", type=int, default=256)
        s.add_argument("--runtime-low-free-sec", type=float, default=5.0)
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
    u.add_argument("--player", help="Standalone Windows .exe đã build trong workspace; dùng cùng bảo vệ RAM/Job Object")
    u.add_argument("--graphics", action="store_true", help="Standalone player có đồ họa để chụp gameplay; giữ Job Object và bảo vệ RAM")
    u.add_argument("--project", default=str(ROOT))
    sub.add_parser("status")
    a = p.parse_args(argv)

    # A player smoke observes live state/save files and the complete Data directory.
    # It must execute even when the small player launcher executable is unchanged.
    if getattr(a, "player", None):
        a.no_cache = True

    if a.mode == "status":
        status()
        return 0
    if a.max_mem_mb <= 0 or a.min_free_mb < 0 or a.wait_sec < 0 or a.runtime_min_free_mb <= 0 or not math.isfinite(a.runtime_low_free_sec) or a.runtime_low_free_sec < 0:
        p.error("memory caps/guards must be positive; wait/grace must be finite nonnegative")

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
