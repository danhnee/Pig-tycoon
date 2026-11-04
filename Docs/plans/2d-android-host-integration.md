# Flutter → Unity Android host

Status: Dart host tests pass; native Android integration is written but not built or exercised on a device. No APK or emulator success is claimed.

The default Flutter `main()` now opens `PigTycoonGameApp`, with An/Khoa selection and a real native launch request. The existing benchmark dashboard remains in source for its existing diagnostic tests. It is not the game launch target.

## Runtime contract

- Channel `pigtycoon/unity`, method `launchGame`, arguments `{player: An|Khoa}`.
- Android launches `com.pigtycoon.pig_tycoon_flutter_client.UnityGameActivity` full screen. It extends the Unity export's `UnityPlayerActivity`; the export must use the Activity entry point.
- Unity runs in the `:unity` process so that Unity termination does not terminate Flutter. Unity requires `singleTask`; the host therefore waits for Flutter's actual pause/resume rather than an incompatible cross-task activity result.
- Intent extra `pigtycoon.player` carries the selected protagonist. Unity preserves an existing saved identity on resume, rather than rewriting a loaded save from the menu.
- Back, including Android 13+ predictive Back, sends `UnityPlayer.UnitySendMessage("PixelWorldAndroidBridge", "SaveAndReturn", "")`.
- C# saves first. Only after successful commit it calls Java static `UnityGameActivity.finishAfterSave()`; on error it calls `saveFailed()` and keeps the game visible for retry. The map worker owns the C# bridge, including pause autosave and atomic writes.
- Returning to Flutter acknowledges menu control, not proof of save success. OS process death must be tested separately; pause autosave is the available recovery boundary, not a guarantee that Android sends pause before a hard kill.

## Build boundary

Export the pinned Unity project into `Builds/AndroidExport/`. Android settings include `Builds/AndroidExport/unityLibrary`. A missing export raises a named Gradle configuration error; the host cannot silently build an APK without gameplay.

Pinned host tool versions: Gradle 9.3.1 and Android Gradle Plugin 9.1.1, matching official Unity 6000.6.3f1+ compatibility. Minimum API 26. Gradle heap 1536 MiB, one worker. Final AGP/Kotlin/export integration still needs the real export build to verify, including launcher class package, native ABI and plugin manifest merging.

On this workstation Flutter's BAT launcher fails resolving WHERE. The verified invocation is:

```powershell
cd D:\Pig-tycoon\flutter_client
D:\flutter\bin\cache\dart-sdk\bin\dart.exe D:\flutter\bin\cache\flutter_tools.snapshot analyze
D:\flutter\bin\cache\dart-sdk\bin\dart.exe D:\flutter\bin\cache\flutter_tools.snapshot test
```

Observed: analyzer no issues; six tests pass, retaining all three legacy tests and adding three launch tests for protagonist arguments, native error display/retry and suppression of duplicate launches. These tests mock the native channel only to test Dart behavior; they do not prove a Unity runtime starts.

## Android toolchain evidence

Installed official Google command-line tools into the previously absent `C:\Users\Admin\AppData\Local\Android\Sdk\cmdline-tools\latest`, using `tools/android_install_cmdline_tools.py`. Repository metadata size and SHA-1 verified. Audit: `../audit/android-cmdline-tools-install.json` beside the integration project. The script does not accept licenses. Existing shared NDK r28.2 is not the NDK r27c requested by the pinned Unity release manifest.

The official exact-release manifest is saved beside the project as `audit/unity-release-6000.6.3f1.json`. It identifies changeset 45d8eee7de74, the Android module installer (1,327,762,424 bytes, MD5 integrity `md5-lJYP9Cg4VxSi7Ik7oL+k0w==`), JDK 17.0.18+8 and NDK r27c. Module download and install are distinct actions; see the download receipt for current status. Only the root worker installs it after checking that Unity is closed.

Unity's official 6000.6 documentation explicitly does not support Android emulators. The requested AVD remains a separate empirical target, not an engine-supported configuration. Its x86_64 ABI cannot be assumed compatible with a Unity ARM build or desktop Rust/C++ DLLs. Do not downgrade the pinned editor to make this claim. Verify the exported APK libraries, attempt installation only when ABI/translation permits, and report the actual result. Also test an ARM64 Android device.

## Required native acceptance

1. Build the real Unity export and host APK; verify native launcher classes, merged manifest and ABIs.
2. Launch each protagonist, traverse maps, save, Back to Flutter and reopen.
3. Background/foreground, interruption, process kill and resume; preserve save and avoid two Unity instances.
4. Verify audio focus, landscape, pixel camera/input and runtime memory on a physical device.
5. Separately record the AVD installation/run result. No emulator or native build has been run by this worker.

## Primary sources

- [Unity as a Library on Android](https://docs.unity.com/en-us/engine/6000.6/manual/platform-specific/android/developing/unityasa-library).
- [Exact release metadata](https://services.api.unity.com/unity/editor/release/v1/releases?version=6000.6.3f1).
- [Unity/Gradle compatibility](https://docs.unity.com/en-us/engine/6000.6/manual/platform-specific/android/introducing/gradle-overview/version-compatibility).
- [Android requirements and emulator limitations](https://docs.unity.com/en-us/engine/6000.6/manual/platform-specific/android/introducing/requirements-and-compatibility).
