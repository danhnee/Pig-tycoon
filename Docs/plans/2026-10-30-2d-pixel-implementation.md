# Pig Tycoon 2D migration — implementation plan

Status: user provisionally approved design on 30/10/2026; proceed and revise through evidence. Prefer existing branch and Stardew installation assets, create only genuinely missing content. No renewed approval needed for ordinary implementation.

Goal: retain Pig Tycoon mechanics while replacing the playable 3D presentation with connected pixel maps and a Flutter Android host.

Architecture: isolated integration clone, one persistent existing GameEngine, scene presentation from existing source scenes and prefabs, external Legacy3DArchive, existing sprite sheets adapted with provenance, Android Unity as a Library host.

Tech stack: Unity 6000.6.3f1, C#, URP 2D, 32 PPU, Flutter 3.47.5, Dart 3.13.4, Android.

## Work regions and execution

- [x] Clone eight GitHub branches and LFS; verify SHA, clean checkout, git fsck and LFS fsck.
- [x] Record baseline B runner: `dotnet run --project Tests/PigTycoon.Runner.csproj`, 81/81.
- [x] Root: inventory and move known 3D files with `.meta` to Legacy3DArchive, preserve SHA-256 manifest and original workspace. Validate paths stay inside integration-review before each move. Keep portable Core and legacy tests.
- [x] Root: import unique Assets from C then D preserving source meta/GUID; log conflicting paths and GUIDs, review dependencies before scene integration. Do not overwrite B mechanically.
- [ ] Map worker: use existing Farm_Main/BoRiaVillage2D/Forest_Primeval and existing prefabs to bind four map areas and Hub_Village; preserve persistent engine, location and save data. Own PixelWorld runtime/editor files and focused WorldLocationGraph registration region.
- [x] Map worker: add failing Core route/save regression, implement registration/traversal contracts, rerun canonical runner. Provide editor build/capture entry point; root runs Unity.
- [ ] Asset worker: stage useful existing sheets/TMX from F read-only into Assets/Art/PixelRework with hashes, source manifest, sprite grid and 32 PPU; inspect sheets and preserve alpha. Generate visual review evidence before promote.
- [x] Flutter worker: replace default dashboard entry with actual host menu/launch flow, Android platform channel/native Unity host and meaningful missing-export error; retain benchmark engine separately. Test launch failure propagation and lifecycle contract, run analyze/tests.
- [ ] Flutter worker: diagnose Android tooling, install official CLI components where possible, document exact Unity module/ABI requirements. Do not claim emulator works without APK execution.
- [ ] Root: run `python tools/headless_runner.py unity --method <verified-worker-entrypoint>` only after all code writes stabilize, with report and one heavy job; inspect compile log, fix integration failures, rerun only necessary checks.
- [ ] Root: render overview/gameplay/detail and inspect images; get art approval before promoting candidate visuals. Build and playthrough routes, farm/combat/market/inventory/save.
- [ ] Root: build Windows and export Android unityLibrary; Flutter APK, install on Medium_Phone API35 x86_64 with matching native ABIs, exercise launch/back/pause/resume/kill/reopen/save.
- [ ] Root: self-review and cross-check each deliverable against GDD; record unmet requirements explicitly. No push to main or claims of full game completion from baseline tests alone.

Independent workers own disjoint regions. Root serializes Unity/Blender. Shared scene changes and bulk import are root-owned until handoff. Working artifact details and test code live in each worker's focused files; this checklist tracks integration rather than inventing code before source inspection.
