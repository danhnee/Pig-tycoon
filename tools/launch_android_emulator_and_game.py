import sys
import os
import re
import json
import time
import subprocess
from pathlib import Path
from datetime import datetime

sys.stdout.reconfigure(encoding="utf-8", line_buffering=True)

WORKSPACE = Path("D:/Pig-tycoon")
SDK = Path("C:/Users/Admin/AppData/Local/Android/Sdk")
EMU = SDK / "emulator/emulator.exe"
ADB = SDK / "platform-tools/adb.exe"
AVD_DIR = Path("D:/AndroidAvd/Medium_Phone.avd")
STATE_FILE = WORKSPACE / ".agent_scratch/installed_apk_state.json"


def find_aapt() -> Path | None:
    for base in [SDK, Path("D:/AndroidSdkForUnity")]:
        if base.exists():
            candidates = list(base.glob("build-tools/*/aapt.exe"))
            if candidates:
                return candidates[0]
    return None


def find_latest_pc_exe() -> Path | None:
    exes: list[Path] = []
    for d in [WORKSPACE / "Builds/Release", WORKSPACE / "Builds/PixelMyth"]:
        if d.exists():
            for p in d.rglob("PigTycoon*.exe"):
                if "CrashHandler" not in p.name:
                    exes.append(p.resolve())
    if exes:
        exes.sort(key=lambda p: p.stat().st_mtime, reverse=True)
        return exes[0]
    return None


def find_target_apk(cli_args: list[str]) -> Path:
    for arg in cli_args:
        if not arg.startswith("-"):
            p = Path(arg)
            if not p.is_absolute():
                p = WORKSPACE / p
            if p.exists() and p.suffix.lower() == ".apk":
                return p.resolve()

    search_dirs = [
        WORKSPACE / "Builds/Release",
        WORKSPACE / "Builds/Android",
        WORKSPACE / "Builds",
    ]
    apks: list[Path] = []
    for d in search_dirs:
        if d.exists():
            apks.extend(d.rglob("*.apk"))

    if apks:
        apks = sorted(set(p.resolve() for p in apks), key=lambda p: p.stat().st_mtime, reverse=True)
        return apks[0]

    raise FileNotFoundError("Không tìm thấy file .apk nào trong D:\\Pig-tycoon\\Builds!")


def inspect_apk(apk_path: Path) -> tuple[str, str]:
    pkg = "com.pigtycoon.pig_tycoon_flutter_client"
    activity = "com.unity3d.player.UnityPlayerActivity"
    aapt = find_aapt()
    if aapt and aapt.exists():
        r = subprocess.run(
            [str(aapt), "dump", "badging", str(apk_path)],
            capture_output=True, text=True, errors="replace"
        )
        m_pkg = re.search(r"package:\s+name='([^']+)'", r.stdout)
        if m_pkg:
            pkg = m_pkg.group(1)
        activities = re.findall(r"launchable-activity:\s+name='([^']+)'", r.stdout)
        if "com.unity3d.player.UnityPlayerActivity" in activities:
            activity = "com.unity3d.player.UnityPlayerActivity"
        elif activities:
            activity = activities[0]
    return pkg, activity


def optimize_avd_config() -> None:
    cfg_path = AVD_DIR / "config.ini"
    if cfg_path.exists():
        txt = cfg_path.read_text(encoding="utf-8", errors="replace")
        txt = txt.replace("hw.gpu.mode = angle_indirect", "hw.gpu.mode = host")
        txt = txt.replace("hw.cpu.ncore = 2", "hw.cpu.ncore = 4")
        txt = txt.replace("fastboot.forceColdBoot = yes", "fastboot.forceColdBoot = no")
        txt = txt.replace("fastboot.forceFastBoot = no", "fastboot.forceFastBoot = yes")
        cfg_path.write_text(txt, encoding="utf-8")


def main() -> None:
    cli_args = sys.argv[1:]
    if "--pc" in cli_args:
        exe = find_latest_pc_exe()
        if not exe:
            print("Không tìm thấy bản build PC (PigTycoon.exe) trong D:\\Pig-tycoon\\Builds!")
            sys.exit(1)
        mtime_str = datetime.fromtimestamp(exe.stat().st_mtime).strftime("%Y-%m-%d %H:%M:%S")
        print(f"Đang mở ngay bản PC mới nhất ({mtime_str}): {exe}")
        subprocess.Popen([str(exe)], cwd=str(exe.parent))
        return

    force_reinstall = "--reinstall" in cli_args or "-f" in cli_args

    os.makedirs("D:/tmp", exist_ok=True)
    os.makedirs(STATE_FILE.parent, exist_ok=True)
    env = os.environ.copy()
    env["ANDROID_SDK_ROOT"] = str(SDK)
    env["ANDROID_HOME"] = str(SDK)
    env["ANDROID_AVD_HOME"] = "D:\\AndroidAvd"
    env["TMP"] = "D:\\tmp"
    env["TEMP"] = "D:\\tmp"

    t0 = time.time()
    apk_path = find_target_apk(cli_args)
    pkg, activity = inspect_apk(apk_path)
    mtime_str = datetime.fromtimestamp(apk_path.stat().st_mtime).strftime("%Y-%m-%d %H:%M:%S")
    size_mb = apk_path.stat().st_size / (1024 * 1024)

    print("======================================================================")
    print("  PIG TYCOON ANDROID LAUNCHER (Tự động chọn APK mới nhất)")
    print(f"  • File APK : {apk_path}")
    print(f"  • Dung lượng: {size_mb:.1f} MB | Cập nhật: {mtime_str}")
    print(f"  • Package  : {pkg} -> {activity}")
    print("======================================================================")

    optimize_avd_config()
    subprocess.run([str(ADB), "start-server"], env=env, capture_output=True)
    r_dev = subprocess.run([str(ADB), "devices"], capture_output=True, text=True, env=env)

    if "emulator-5554\tdevice" not in r_dev.stdout:
        for lk in AVD_DIR.glob("*.lock"):
            try:
                lk.unlink()
            except Exception:
                pass
        print("[1/4] Đang mở cửa sổ máy ảo Medium_Phone (QuickBoot + GPU RTX 3050)...")
        subprocess.Popen(
            [str(EMU), "-avd", "Medium_Phone", "-gpu", "host"],
            env=env,
            cwd=str(SDK / "emulator"),
            creationflags=subprocess.CREATE_NEW_CONSOLE,
        )
    else:
        print("[1/4] Máy ảo emulator-5554 đang mở sẵn (0s khởi động)!")

    print("[2/4] Đang kết nối hệ điều hành Android...")
    for _ in range(180):
        r_boot = subprocess.run(
            [str(ADB), "-s", "emulator-5554", "shell", "getprop", "sys.boot_completed"],
            capture_output=True, text=True, env=env
        )
        if r_boot.stdout.strip() == "1":
            break
        time.sleep(0.5)

    for _ in range(60):
        r_pm = subprocess.run(
            [str(ADB), "-s", "emulator-5554", "shell", "pm", "path", "android"],
            capture_output=True, text=True, env=env
        )
        if "package:" in r_pm.stdout:
            break
        time.sleep(0.5)

    need_install = True
    if not force_reinstall and STATE_FILE.exists():
        try:
            st = json.loads(STATE_FILE.read_text(encoding="utf-8"))
            if (
                st.get("apk_path") == str(apk_path)
                and st.get("size") == apk_path.stat().st_size
                and st.get("mtime") == apk_path.stat().st_mtime
            ):
                r_chk = subprocess.run(
                    [str(ADB), "-s", "emulator-5554", "shell", "pm", "path", pkg],
                    capture_output=True, text=True, env=env
                )
                if "package:" in r_chk.stdout:
                    need_install = False
        except Exception:
            pass

    if need_install:
        print(f"[3/4] Đang cài đặt nhanh {apk_path.name} (--abi arm64-v8a --fastdeploy)...")
        subprocess.run(
            [str(ADB), "-s", "emulator-5554", "shell", "am", "force-stop", pkg],
            capture_output=True, env=env
        )
        r_inst = subprocess.run(
            [
                str(ADB), "-s", "emulator-5554", "install",
                "-r", "-t", "-g", "--abi", "arm64-v8a", "--fastdeploy",
                str(apk_path)
            ],
            capture_output=True, text=True, env=env
        )
        if r_inst.returncode != 0:
            r_inst = subprocess.run(
                [
                    str(ADB), "-s", "emulator-5554", "install",
                    "-r", "-t", "-g", "--abi", "arm64-v8a",
                    str(apk_path)
                ],
                capture_output=True, text=True, env=env
            )
        print("      ->", (r_inst.stdout or r_inst.stderr).strip())
        if r_inst.returncode == 0:
            STATE_FILE.write_text(
                json.dumps({
                    "apk_path": str(apk_path),
                    "size": apk_path.stat().st_size,
                    "mtime": apk_path.stat().st_mtime,
                    "pkg": pkg,
                }, indent=2),
                encoding="utf-8",
            )
    else:
        print(f"[3/4] Bản {apk_path.name} đã cài sẵn trên máy ảo (bỏ qua bước cài đặt -> 0s)!")

    print(f"[4/4] Đang mở game ({pkg}/{activity})...")
    subprocess.run([str(ADB), "-s", "emulator-5554", "shell", "input", "keyevent", "82"], env=env)
    subprocess.run([str(ADB), "-s", "emulator-5554", "shell", "wm", "dismiss-keyguard"], env=env)
    subprocess.run(
        [str(ADB), "-s", "emulator-5554", "shell", "am", "force-stop", pkg],
        capture_output=True, env=env
    )
    subprocess.run(
        [str(ADB), "-s", "emulator-5554", "shell", "am", "start", "-n", f"{pkg}/{activity}"],
        env=env
    )

    time.sleep(4)
    subprocess.run([str(ADB), "-s", "emulator-5554", "shell", "input", "tap", "480", "445"], env=env)
    time.sleep(1)
    subprocess.run([str(ADB), "-s", "emulator-5554", "shell", "input", "tap", "800", "550"], env=env)

    elapsed = time.time() - t0
    print("======================================================================")
    print(f"  HOÀN TẤT TRONG {elapsed:.1f} GIÂY! Game đang chạy trên màn hình máy ảo.")
    print("======================================================================")


if __name__ == "__main__":
    main()
