# Quy trình một nhánh, một nguồn (chốt 4/11/2026)

Mục đích: lần làm tiếp nào cũng bắt đầu đúng một chỗ. Không còn bản sao song song, không còn "Argon làm một kiểu, ghép vào ra một kiểu".

## 1. Một chỗ làm việc duy nhất (gộp ngày 4/11/2026)
| Cái gì | Ở đâu |
|---|---|
| Dự án game (Unity 6000.6.3f1, mọi code, art và tài liệu) | `D:\Pig-tycoon` |
| Nhánh git | `feat/codex/2d-mythic-rebuild`, nhánh duy nhất để làm tiếp. Repo đã tự chứa đủ object, không còn link sang `repository.git`. |
| Toolchain IL2CPP | `D:\Pig-tycoon\.toolchain` (gitignored). Biến `UNITY_IL2CPP_PATH` trỏ vào `.toolchain\compatible-apphosts\il2cpp`. **Không xoá.** |
| SDK Android cho build | `D:\Pig-tycoon\.agent_scratch\android_sdk_unity16` dùng cho Unity; `android_sdk_flutter` dùng cho Flutter, có sẵn NDK 27.2 của Unity. |
| Hộp thư trao đổi với Argon | `D:\Pig-tycoon\.collab\claude-2d\ui_and_map_sync\` (`from_claude/`, `from_argon/`) |
| Bản phát hành | `D:\Pig-tycoon\Builds\Release\` |

Thư mục `D:\Pig-tycoon-2D-migration-20261030` và repo 3D cũ đã được gộp lại rồi bỏ. Phần còn giá trị của dự án 3D (mốc đài thiên văn đã duyệt cùng các script đi kèm) nằm ở `ArtSource/Reference_3D_Approved/`.

## 2. Ai sở hữu phần nào
| Phần | Chủ | Thư mục |
|---|---|---|
| Logic game (Core, không dùng UnityEngine), test, tài liệu thiết kế, build và đóng gói | Claude | `Assets/Scripts/Core`, `Assets/Scripts/Controllers`, `Tests`, `Docs`, `Assets/Editor`, `.agent_scratch` |
| Giao diện, UX, art, hoạt ảnh, nền cốt truyện | Argon | `Assets/Scripts/UI`, `Assets/Art`, `tools/pixelart`, `tools/character_paperdoll`, `tools/world_3d_blender`, `tools/build_*`/`apply_*` của Argon |
| Cloud (Supabase) và app vỏ Flutter | Claude | `supabase`, `flutter_client` |

Sửa vào phần của người kia thì phải báo trước trong room `pig-tycoon/2d-pixel-rebuild` và chờ đồng ý. Riêng lỗi chặn người chơi (kẹt màn hình, crash) thì sửa ngay rồi báo lại.

## 3. Vòng làm việc
1. Claude chạy `.collab/claude-2d/ui_and_map_sync/sync_from_integration.sh` để đẩy code mới nhất ra `from_claude/`.
2. Argon bắt đầu sửa **từ bản `from_claude/` mới nhất** và giao vào `from_argon/`.
3. Claude ghép vào dự án theo đúng thứ tự:
   - chồng file lên;
   - chạy `.agent_scratch/quick_compile.sh`, phải ra OK;
   - chạy `.agent_scratch/run_tests.sh`, phải xanh hết;
   - chạy `.agent_scratch/ui_probe.sh`, không được có COVERED hay DEAD mới;
   - gửi ảnh thật cho Argon.
4. Mỗi lần ghép xong là một commit trên nhánh duy nhất, ghi rõ ai làm gì. Không dùng `git add -A`; chỉ add đúng các file đã ghép.

## 4. Đóng gói
- Bản Windows: build phát hành (`PixelMythWorldBuilder.BuildWindowsRelease`), rồi chạy `.agent_scratch/package_release.sh`.
- Bản Android có Supabase: chạy `.agent_scratch/flutter_apk.sh`. Script này dựng lại cảnh, xuất unityLibrary và build APK bằng Flutter.
