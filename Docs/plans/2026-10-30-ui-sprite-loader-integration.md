# Tích hợp UISpriteLoader B + helper C

Ngày30/10/2026. Phạm vi: khôi phục API loader mà controller/UI của C đang gọi, giữ đường nạp của B, thêm package resource không phụ thuộc thư mục Assets của máy phát triển.

## Lỗi và thay đổi

Log `.agent_scratch/source-compile/build-official-modules.log` ban đầu có lỗi CS0117 do thiếu các helper như GetEmoteSprite, GetVillagerAvatar, GetTowerSprite, GetSpikeTrapIdle/Sprung, GetBarnSprite, GetWaterWellSprite, GetFeedSiloSprite và GetCorpseLotSprite. B đọc PNG từ filesystem: editor dùng Assets, Windows build dùng thư mục PigTycoonArt cạnh player data. Đường filesystem này không đủ để truy cập PNG nằm trong APK Android.

- Khôi phục42 helper chưa có từ implementation C, gồm helper phụ thuộc cho frame tháp/VFX. Không thay các method đã có của B bằng bản C.
- `BanChong` là ID blueprint canonical hiện hành, gắn trực tiếp art `spike_trap_idle.png`; không đi nhánh tháp mặc định.
- GetVillagerAvatar dùng ảnh NPC thật ở đường dẫn C. Bỏ fallback dùng icon trái tim làm mặt NPC khi ảnh không tồn tại.
- Giữ các ảnh/UI primitive solid white và soft circle/shadow từ code C; không thay asset công trình/nhân vật bằng primitive.
- Cache key bổ sung border và lưu đầy đủ pivot để cùng một texture được gọi với border khác không nhận nhầm sprite đã cache.

## Resources package

`tools/package_ui_sprite_resources.py` sao chép nguyên byte840 PNG từ `Assets/Art`, trừ `PixelRework` chưa duyệt, thành `.bytes` trong `Assets/Resources/PigTycoonArt`. Mỗi entry có metadata Unity TextAsset riêng, path index và SHA-256 nguồn/package. Tổng PNG data4.626.162 byte. Không tạo ảnh hoặc bind player candidate vào game.

Loader trên built player ưu tiên `Resources.Load<TextAsset>` theo đường dẫn đầy đủ rồi giải mã byte PNG qua pipeline Texture2D/Sprite.Create hiện có. Nạp từng ảnh khi cần, không kéo sẵn toàn bộ texture vào RAM. Có alias basename theo entry đầu tiên trong catalog được sắp xếp, tương ứng hành vi filename fallback legacy; đường dẫn đầy đủ được ưu tiên và nên dùng để tránh ambiguity.

Editor giữ ưu tiên file nguồn; Windows player vẫn có thể đọc package raw PNG legacy khi resource không có. Entry được catalog khai báo nhưng thiếu byte resource hoặc decode lỗi sẽ báo lỗi, không tạo ảnh placeholder. Đường dẫn chưa khai báo trả null, cùng hợp đồng loader cũ.

Chạy lại packager mỗi khi bổ sung/sửa Art production. `--verify` phát hiện nguồn/hash/catalog không còn khớp. Không package các sheet candidate An/Khoa hoặc terrain chưa duyệt.

## Lệnh và bằng chứng

```
python tools/package_ui_sprite_resources.py
python tools/package_ui_sprite_resources.py --verify
python -m py_compile tools/package_ui_sprite_resources.py
```

Kết quả: PASS840 nguồn/package khớp SHA-256, catalog phủ đầy đủ Art production hiện tại, metadata có cho từng TextAsset. Chưa chạy kiểm thử Android APK.

Đã compile toàn bộ source Scripts + Editor bằng tham chiếu DLL Unity6000.6.3f1 thật trong project kiểm tra riêng `.agent_scratch/ui-loader-compile/LoaderCompile.csproj`. Lần compile sau sửa loader: loader và verifier không có lỗi; build còn1 lỗi ngoài phạm vi ở `DefenseBuildPopup.cs`: thiếu `DefenseManager.RepairBuilding`. Đã chuyển root xử lý; không tuyên bố toàn project build thành công từ kết quả này.

## Verifier engine cho root chạy tuần tự

```
python tools/headless_runner.py unity --method PigTycoon.EditorTools.UISpritePackagingVerifier.Verify --no-cache
```

Verifier cố ý gọi `LoadPackagedSprite` trong editor để kiểm tra đúng route Resources thay vì vô tình đọc file Assets. Kiểm tra ảnh UI/NPC/công trình/trap thật nạp được,32PPU, cache border tách biệt, prefix Assets normalize cùng một sprite, asset không tồn tại trả null, và BanChong gắn đúng bẫy. Task này không chạy Unity, nên chưa có bằng chứng verifier engine PASS hoặc Resources được đưa vào APK thành công.
