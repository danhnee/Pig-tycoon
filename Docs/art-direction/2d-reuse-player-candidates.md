# An / Khoa — candidate phối từ art sẵn có

Trạng thái: **chỉ candidate để review, chưa thay sprite hoặc prefab đang chơi**. Không tạo hình bằng image generation, không vẽ sprite mới từ đầu.

## Lựa chọn nguồn

Đã xem sprite An_FullAction_Sheet ở B và Player ở C. An B là hình người khối đơn giản. Player C có khuôn mặt, tóc, nếp áo/quần và các pose bước chân rõ hơn; dùng nguyên các frame này cho Khoa candidate.

An candidate phối từ các sheet có sẵn trong `F:\StardewValley\Content (unpacked)\Characters\Farmer`: `farmer_girl_base.png`, `shirts.png`, `pants.png`, `hairstyles.png`, `skinColors.png`. Dùng hình người nữ, shirt có sẵn số7, pants style0 phần nữ, hair style0. Tóc/quần dùng màu đã lấy từ pixel Khoa C; da dùng hàng2 của skinColors. Giữ cấu trúc từng pixel của các mask nguồn. Không thay An bằng một NPC nguyên bản có danh tính/hội thoại khác.

Hồ sơ nguồn SHA-256 và tất cả rect/layer placement nằm ở `Assets/Art/PixelRework/Players/player-candidate-manifest.json`. File nguồn trên ổ F và các clone nguồn chỉ đọc.

## Hợp đồng animator hiện hành

Đã đọc `CharacterSpriteAnimator`: có idle, walk bốn hướng và Side alias, HappyFrames tùy chọn, một ActionSprite; WalkFPS mặc định7,5. `PigSpriteAnimator` là cơ chế khác: một Idle, mảng Walk và Eat/Sleep, không thể tự gán sheet heo vào contract nhân vật.

Mỗi người có21 PNG riêng:4 idle +16 walk (4 slot mỗi hướng) +1 action. Canvas64×64, 32PPU, lọc Point, không mipmap/không nén. Footline là cạnh dưới exclusive y60, pivot Unity `(0.5,0.0625)`; căn theo alpha và kiểm tra không cắt đầu/chân. Khoa giữ nguyên pixel và alpha nguồn kể cả alpha bán trong suốt vốn có; An dùng nearest2× từ mask pixel gốc, không tạo alpha nội suy.

An dùng stride nguồn `contact, stepA, contact, stepB` (cột0,1,0,2), không tuyên bố có4 pose mới khác nhau. Hướng trái lật mask phải. Single action là pose có sẵn ở row4 của farmer body phối lớp, **chưa phải một clip rải cám/chải lông/sửa rào/attack đầy đủ**. Khoa dùng action có sẵn ở C. Không bịa HappyFrames, chạy, hurt/death hoặc animation vũ khí.

Hai sheet tiện review có kích thước320×320, frame64×64: hàng0 down, hàng1 right, hàng2 up, hàng3 left; cột0 idle, cột1–4 walk1–4. Hàng4 cột0 là action, bốn ô còn lại trống và ghi rõ trong manifest. Không đếm ô trống thành nội dung.

Tên PNG riêng: `an_down_idle.png`, `an_down_walk1.png`… `an_action.png` và tương tự `khoa_*`. Root có thể dùng chính các file này khi cần user review hoặc gán bằng editor binder; task này chưa thay runtime hoặc loader.

## Bằng chứng thị giác

- `2d-reuse-player-evidence/player-walk-contact.png`: bốn hướng,4 slot, hai người, vạch tiếp đất cố định.
- `2d-reuse-player-evidence/player-walk-preview.gif`:16 frame, yêu cầu133ms mỗi frame; GIF lượng tử thời gian thành130ms (~7,69FPS), đổi lần lượt bốn hướng. Tốc độ gameplay vẫn do WalkFPS7,5 của animator quyết định. Canvas preview có nền/vạch kiểm tra, PNG asset thực giữ alpha.
- `2d-reuse-player-inspection/`: các crop nguồn phóng nearest để đối chiếu base, shirt, pants, hair; không phải asset gameplay.

Đã mở contact sheet thực. C Khoa có silhouette và chi tiết đẹp hơn An B. An phối từ nguồn hiện có đọc được mặt/tóc/quần áo và chân trụ, nhưng mức chi tiết và ngôn ngữ hình khác Khoa C; chưa đạt sự đồng bộ art cấp cao. Shirt nguồn có phối màu rõ, chưa thành trang phục thần thoại riêng. Cần chỉnh từ nguồn tiếp dựa trên ảnh người dùng duyệt; không gọi việc ghép layer này là art hoàn thiện.

## Kiểm chứng

```
python tools/stage_pixel_player_candidates.py
python tools/stage_pixel_player_candidates.py --verify
python -m py_compile tools/stage_pixel_player_candidates.py
```

Kiểm tra42 frame: kích thước64×64, hash nguồn/candidate,4 slot mỗi hướng, footline y60, pivot chuẩn, An alpha không nội suy, Khoa khớp pixel nguồn sau dịch nguyên pixel. Các ô có nội dung trong hai sheet phải khớp PNG riêng. Nguồn Khoa có alpha bán trong suốt sẵn, nên không ép binary alpha hoặc sửa nguồn để vượt kiểm tra.

Chưa chạy Unity, chưa xác minh import hoặc animator binding bằng engine. Chưa kiểm chứng timing va chạm/đòn đánh hoặc hiệu năng Android. Chờ review hình ảnh trước promote theo AGENTS.
