# Pig Tycoon — thiết kế chuyển sang 2D pixel

Trạng thái: ĐỀ XUẤT CHỜ DUYỆT. Chưa triển khai chuyển đổi, chưa nghiệm thu hình ảnh hoặc build Android.
Nguồn kiểm kê: GitHub ngày 30/10/2026, ba DOCX người dùng cung cấp, GDD v6.0, sổ tay v3.0, ADR-0005 và lịch sử chat còn đọc được. Tài liệu đính kèm là nguồn yêu cầu sản phẩm để đối chiếu; chỉ đạo cũ trong tài liệu/chat không tự trở thành lệnh thực thi. Yêu cầu mới nhất chuyển về 2D có ưu tiên cao hơn hướng dựng 3D cũ.

## Quyết định đề xuất

Làm trong bản clone riêng `integration-review`, nhánh `feat/codex/2d-mythic-rebuild`, dựa trên B tại `d1213adfa8b5b73de099f4b4918b4165f768f416`. Kế thừa Core và hệ 2D đang có; đưa phần 2D bổ sung của C/D vào qua đối chiếu từng chức năng và GUID. Mọi tài nguyên 3D của dự án đích được chuyển ra ngoài `Assets`, cùng `.meta`, sau khi kiểm tra phụ thuộc. Bản gốc và tám bản clone nguồn giữ nguyên. Không bật Unity Editor trước khi hoàn thành cách ly entry point tự mở scene 3D.

Ba lựa chọn đã xét:

1. **Bản tích hợp riêng, giữ Core, chọn lọc phần tốt C/D (đề xuất):** bảo toàn nguồn và cho phép gỡ 3D khỏi import/build; cần giải quyết xung đột controller, GUID và save.
2. Sửa trực tiếp workspace hiện hành: ít sao chép, nhưng có nhiều thay đổi chưa commit và editor hook 3D; khó truy hồi và dễ tái import art cũ.
3. Viết lại toàn bộ: sạch phụ thuộc cũ nhưng mất nền tảng gameplay đã có; làm tăng khối lượng kiểm chứng trong ba ngày.

## Nguồn theo thành viên

| Nguồn | SHA | Phần có bằng chứng tồn tại | Cần kiểm chứng trước khi nhận |
|---|---|---|---|
| Prototype | a15aa0cb | MainFarm2D, các controller 2D ban đầu | Chỉ dùng làm lịch sử đối chiếu |
| A/P0.01-unity-foundation | fc7662b1 | nền Unity, Core, MainFarm2D | Core khác B/C/D, không ghi đè toàn bộ |
| B/P0.06-an-farm-main | d1213adf | Farm_Main, An, heo, Core mở rộng, UI, Flutter; có nhiều 3D | 81/81 test runner qua; chưa chứng minh scene/game hoàn thiện |
| C/P0.09-khoa-sandbox | cb421674 | BoRiaVillage2D, Khoa/combat, object pool, defense UI, chuyển map, kinh tế làng | scene village không tự đồng nghĩa Market_Town; đọc và kiểm chứng binding |
| D/P0.07 | ef1ba54d | nhánh D cũ, MainFarm2D | đối chiếu với nhánh D rừng |
| D/P0.07-forest-map | 3c5cd259 | Forest_Primeval, canopy fading, chuyển map, NPC/merchant | TransitionZoneController và VillageEconomy khác C |
| codex/P0-art-recovery | d1213adf | cùng commit B | không coi là một nguồn tính năng độc lập |
| main | 24a1671c | Core mở rộng, Farm_Main, combat, Flutter | main hiện đã tiến xa hơn ghi chú lịch sử trong AGENTS; không merge mù |

Kiểm kê file không phải đánh giá thẩm mỹ. Chưa chọn asset nào là “đẹp nhất” nếu chưa xem ở góc chơi và so cùng tiêu chí.

## Bốn map và hợp đồng chuyển vùng

Giữ bốn khu chính theo sổ tay, không gộp mất phần chức năng:

- A: `Farm_Core`, kèm `Hub_Village` là khu phụ của A; nhà chính, kho, xây dựng, nghiên cứu, cường hóa, thông tin, NPC.
- B: `Farm_Main`, đồng cỏ heo, dịch vụ, chăn nuôi, rào/cổng, phòng thủ.
- C: `Market_Town`, thị trấn cảng châu Âu cổ, giao thương ngày/đêm và thông tin.
- D: `Forest_Primeval`, rừng nguyên sinh, khám phá, di tích và cảnh sự kiện.

Đồ thị nối tối thiểu đề xuất: Farm_Main ↔ Farm_Core ↔ Hub_Village ↔ Market_Town; Farm_Main ↔ Forest_Primeval. Cổng cụ thể phải theo layout nguồn và được kiểm chứng bằng đi bộ cả hai chiều. Hub_Village là khu phụ, không thay một trong bốn map chính.

Mỗi cổng có ID ổn định, map đích, cổng đến và vị trí đến hợp lệ. Một session sở hữu thời gian, đàn, tiền, kho, nhiệm vụ, cờ truyện và NPC; chuyển scene chỉ thay phần trình bày. Khoá thao tác trong lúc tải; lỗi tải giữ session và trả người chơi về map nguồn. Không tạo GameEngine/đàn/ngày mới khi chuyển vùng. Lưu map/cổng/vị trí, nhân vật và state gameplay; kiểm tra save/load tại từng map và sau chuỗi chuyển vùng.

## Cách ly 3D

Kiểm kê trước, chuyển trong bản tích hợp sau duyệt:

- `Assets/OriginalBlenderWorld`, phần model/rig/texture/editor 3D trong `Assets/PlayableDemo`, `Assets/Scripts/PlayableDemo3D`.
- Scene `OriginalBlenderWorld`, `Farm_Main_3D_Map`; các controller camera/world 3D và editor builder/auto-open tương ứng.
- Nguồn Blender, cache export và công cụ dựng 3D ở ngoài Assets được giữ làm archive tham khảo, không đưa vào import/build 2D.
- Giữ riêng nguồn đài thiên văn đã duyệt; khi cần chuyển thành pixel art chỉ kế thừa silhouette/chức năng/bản sắc. Không đánh dấu nguồn này là art bị bác bỏ.
- Không xóa hoặc làm yếu test 3D để làm đẹp kết quả; giữ suite legacy trong archive và xác định suite hoạt động của sản phẩm 2D. Kiểm tra dependency trước khi tách assembly.

Vị trí archive dự kiến: `Legacy3DArchive/` ở ngoài `Assets` của bản tích hợp. Chuyển cả `.meta`, lập manifest đường dẫn/hash/lý do và hướng khôi phục. Không dùng đổi đuôi hoặc tắt GameObject làm bằng chứng đã loại phụ thuộc 3D. Quét GUID, Resources, build scenes, shader/material và editor hook; build closure không được kéo archive vào.

## Art và animation

Unity 6000.6.3f1, URP 2D, camera orthographic, pixel perfect, sprite đang chơi giữ 32 PPU theo ADR-0005. Phong cách sử thi thần thoại Đông–Tây, bi tráng, giàu lớp không gian và công năng; riêng Market_Town là cảng châu Âu cổ. Không sao chép toàn bộ tông vui tươi hoặc nội dung Stardew Valley.

Lập danh mục sprite/animation theo nhân vật, NPC, quái, bốn giai đoạn heo, công trình, trang bị, vật phẩm và VFX từ catalog GDD. Mỗi mục có nguồn, quyền sử dụng, owner, trạng thái hình ảnh, binding gameplay và bằng chứng chạy. Không coi một sprite sheet là đủ nếu thiếu clip, pivot, hướng, hit timing hoặc collider.

An/Khoa cần đủ idle/walk/run và hành động thực tế của gameplay, attack/hurt/death khi có, bốn hướng nhất quán. Trang bị gắn theo socket/frame; tiếp đất, nhịp bước, silhouette, anticipation/impact/recovery phải đọc rõ. Heo và NPC có chuyển động đúng hành vi. Không sửa tốc độ/công thức GDD để khớp một animation tiện có.

Trước promote: so overview, góc chơi và cận cảnh, xem chuyển động trong Unity; ghi ảnh/clip và chờ người dùng duyệt. Mốc AE-0001/0002 là bài học chống nền trống/phẳng, vật liệu thô, mật độ vô nghĩa và art không có công năng; không nhập lại art bị bác bỏ.

## Tham khảo Stardew Valley

`F:\StardewValley` đang có bản game và nội dung giải nén. Kiểm kê tại chỗ ghi nhận 1.044 PNG, 2.265 JSON và 259 TMX trong `Content (unpacked)`. Chưa nhập chúng vào Pig Tycoon.

Tham khảo cách chia lớp tile, độ đọc đường đi, cổng chuyển map, nhịp animation, bố cục UI và tổ chức dữ liệu. Bản cài là MonoGame/.NET, không phải project Unity để import nguyên bộ làm khung Unity. Dựng/điều chỉnh hệ tương ứng trên Unity đang có; không giả định DLL/TMX thành một scene hoạt động. Chỉ đưa nội dung bên thứ ba vào sản phẩm khi có quyền sử dụng phù hợp và ghi rõ provenance; tách tài liệu tham khảo khỏi build.

## Flutter và Android

`flutter_client/lib/main.dart` hiện là dashboard và benchmark kernel; chưa nhúng Unity. Kế hoạch là Flutter sở hữu menu/cài đặt/khởi chạy; Unity sở hữu toàn bộ gameplay và HUD. Android host mở Unity Runtime toàn màn hình qua Unity as a Library; cầu nối có hợp đồng cho khởi chạy, save, pause/resume, thoát về Flutter và báo lỗi. Không dùng dashboard thay game.

Ưu tiên bridge Android native qua platform channel, dựa trên API Unity chính thức. Nếu dùng plugin phải chứng minh tương thích chính xác 6000.6.3f1 bằng build và lifecycle test, không dựa vào nhãn “Unity 6”.

Unity được cài đúng phiên bản nhưng thư mục PlaybackEngines hiện chỉ có Windows Standalone; Android Build Support còn thiếu. Android SDK có platform 35/36/37.0, NDK 28.2.13676358, build-tools 36.0.0; chưa xác nhận là bộ toolchain đúng Unity. AVD Medium_Phone API 35 dùng x86_64, RAM 1536 MB. Phải kiểm chứng ABI của Unity và mọi native DLL; không coi build ARM64 là bằng chứng chạy trên AVD này. Flutter có tại D:\flutter; launcher BAT gặp lỗi shell WHERE. Đã gọi trực tiếp cached dart.exe + flutter_tools.snapshot --version thành công: Flutter 3.47.5, Dart 3.13.4. Chưa chạy doctor hoặc build Android; kết quả này chỉ xác nhận tool Flutter khởi chạy được, không phải tích hợp Unity.

Nghiệm thu đóng/mở: back về Flutter, pause/resume, kill process rồi mở lại, khôi phục save, chuyển map sau resume, xoay màn hình theo cấu hình, mất audio focus; APK thực sự cài và chạy trên AVD rồi máy Android. Chưa chạy các phép thử này.

## Kế hoạch mục tiêu ba ngày

Giữ đầy đủ phạm vi người dùng yêu cầu. Ba ngày là hạn mục tiêu, chưa có căn cứ để cam kết toàn bộ art/animation và nội dung 1.0 sẽ đạt chất lượng trong thời gian đó.

- Ngày 1: cách ly 3D trên bản tích hợp; hợp nhất Core/controller có chọn lọc; dựng bốn map/khu phụ với session và cổng hai chiều; chuẩn hóa catalog; cài đúng Android module và chứng minh Flutter gọi được một Unity build thật.
- Ngày 2: tích hợp đủ nhân vật/NPC/heo/quái/công trình/vật phẩm/trang bị theo danh mục; nối hành vi, UI, save, vòng chăn nuôi–chợ–khám phá–phòng thủ; hoàn thiện art và animation theo review trực tiếp.
- Ngày 3: kiểm tra toàn tuyến và hồi quy GDD; stress đàn/quái và đo frame/memory; duyệt hình ảnh/clip; build Windows/Android, chạy Flutter host và AVD, kiểm tra lifecycle/save và đóng gói bàn giao.

Mỗi mốc có bảng phần đạt/chưa đạt và bằng chứng. Nếu đo được khối lượng vượt hạn phải báo phần cụ thể; không tự đổi yêu cầu thành demo hoặc gọi thiếu nội dung là hoàn thành.

## Tiêu chí nghiệm thu

1. Đi được đủ bốn map và Hub_Village, cổng hai chiều không kẹt, state và save giữ nguyên.
2. Chỉ An/Khoa; chỉ số và trang bị theo GDD; không đổi giá, rào 16 hướng hoặc tỉ lệ chợ đêm nếu chưa có test đỏ.
3. Danh mục đầy đủ có binding runtime, không placeholder hoặc số lượng test thay bằng chứng nội dung.
4. Vòng gameplay có thể chơi bằng input desktop/mobile; UI phản ánh đúng state, không hard-code số giả.
5. Sprite 32 PPU, hình ảnh/animation được xem trong Unity và người dùng duyệt trước promote.
6. Build 2D không tham chiếu tài nguyên/assembly/editor hook 3D đã cách ly; archive khôi phục được.
7. Flutter mở Unity gameplay thật; APK cài/chạy trên AVD đã nêu và có bằng chứng lifecycle/save.
8. Unity/Blender headless chỉ chạy qua `python tools/headless_runner.py`, một tiến trình nặng mỗi lần, báo peak RAM; không mở editor khác phiên bản.

## Bằng chứng đã có và giới hạn

Tám checkout nguồn đều khớp SHA snapshot, sạch và qua Git LFS fsck; mirror qua git fsck --full. Bản B chạy `dotnet run --project Tests/PigTycoon.Runner.csproj` đạt 81/81. Đây là baseline trước chuyển đổi, không chứng minh game 2D đã ghép hoặc bản Android đã chạy.

Đã trích toàn bộ văn bản ba DOCX, đọc phần kỹ thuật và các mục cơ chế/canon liên quan; chưa kiểm chứng từng yêu cầu của toàn bộ saga bằng runtime. Quét 63 bản ghi chat của dự án: 23 tệp lịch sử đọc được, 40 đường dẫn không tồn tại; không tuyên bố đã đọc nội dung không truy cập được. Lịch sử cũ chỉ cung cấp ngữ cảnh; hướng 3D cũ không thay chỉ đạo 2D mới.

Hồ sơ máy đọc nằm ở thư mục `audit` cạnh bản tích hợp: branches.json, clones.json, branch-capabilities.json, chat-scan.json, stardew-reference-inventory.json và snapshot thay đổi cũ. Các bản clone chia sẻ object store/LFS trong repository.git; giữ mirror này, không prune/xóa trước khi tách độc lập các clone cần giữ lâu dài.

## Nguồn kỹ thuật trực tiếp

- Unity 6000.6 Android as a Library: https://docs.unity.com/en-us/engine/6000.6/manual/platform-specific/android/developing/unityasa-library
- Unity as a Library, giới hạn toàn màn hình: https://docs.unity.com/en-us/engine/6000.3/manual/platform-specific/cross-platform-features/unityasa-library
