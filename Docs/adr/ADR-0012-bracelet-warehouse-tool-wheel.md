# ADR-0012 — Vòng dụng cụ, Vòng đeo tay và kho liên kết

Ngày: 2026-10-31. Quyết định: phán quyết của người dùng (30–31/10/2026), thay cách đọc "túi luôn 3/8" của ADR-0011 mục 3–4. Các phần còn lại của ADR-0011 (bốn map, rừng chỉ qua sự kiện, vật chứng ngoài túi, không hotbar) giữ nguyên.

## Luật

1. **Vòng dụng cụ (6 ô).** Dụng cụ/vũ khí/trang bị dùng cả khi đánh lẫn khi làm trại (xô cám, xô nước, búa bảo trì, vũ khí, chổi chăm sóc, còi Bạch Vân...). Dùng lại id sẵn có của `StardewToolType`. Vòng luôn ẩn. Tab (PC) hoặc huy hiệu đồng nhỏ ở góc (mobile) mở vòng; sáu cung quanh vùng người chơi; cung sáng theo con trỏ/cần điều khiển/kéo; thả hoặc chạm để chọn. Trong vòng: kéo dụng cụ từ khay kho vào cung để trang bị (đổi chỗ nếu đã có), kéo cung xuống khay để gỡ, chạm tâm để xoay vòng sang dụng cụ kế. Không có hàng hotbar nằm ngang ở bất kỳ đâu. Dụng cụ đang chọn quyết định hành động theo ngữ cảnh và clip hoạt ảnh (Bạch Vân dùng clip `whistle`, còn lại `interact`). Mở/đóng là scale + fade 120 ms theo thời gian không co giãn; không làm chậm thời gian game.
2. **Túi chiến đấu.** Giới hạn 3 (cơ bản) / tối đa 8 (cao cấp) chỉ có hiệu lực khi đang giao tranh: đợt quái (`Clock.IsDuringCombatWave`) hoặc chuyến rừng huyền bí (`Expedition.IsActive`). Ngoài giao tranh, đồ nhặt đi thẳng vào kho qua liên kết, nên giới hạn túi không chặn việc nhặt. Dùng lại `SpaceBag`, không tạo cấu trúc thứ hai.
3. **Vòng đeo tay sơ cấp.** Cấp khi hoàn thành **Nhiệm vụ khởi đầu**. Trong `StoryJournalSystem` không có nhiệm vụ khởi đầu chơi được (P01–P08 là cảnh truyện dựng sẵn, Q01 là điều tra), nên thêm nhiệm vụ nhỏ, hai mục tiêu bằng hành động Core có thật: (a) cho đàn ăn một lần (`PigCareCommands.TryFeedPig` hoặc đổ cám vào máng) và (b) gửi một vật phẩm vào kho (`PlayerCarry.TryDepositHand`). Đủ hai mục tiêu thì cấp vòng.
   - Trước vòng: không có kho cá nhân. Đồ nhặt cầm trên tay (một chồng) và phải tự mang tới kho để gửi.
   - Sau vòng có hai không gian: (a) không gian riêng của vòng = túi chiến đấu 3…8; (b) ngoài giao tranh, liên kết kho từ xa tới kho (tối đa 100 ô, nhiều loại vật phẩm), mở ở bất kỳ đâu. Trong giao tranh liên kết đóng; chỉ còn lấy/gửi trực tiếp tại cửa kho.
4. **Lưu/tải.** `GameSaveData.PlayerCarry` lưu: có vòng, hai mục tiêu khởi đầu, đồ trên tay, vòng dụng cụ (6 ô + ô đang chọn). Save cũ (không có trường này): trạng thái trước vòng, vòng dụng cụ mặc định; riêng save cũ đã có đồ trong túi hoặc túi cao cấp thì cấp vòng để không mất đồ đã sở hữu. Vòng dụng cụ lưu sai (trùng, id lạ) quay về mặc định, không mất dụng cụ vì mọi dụng cụ đều vẫn thuộc sở hữu.

## Có thoải mái không: 3–8 món cho một trận đột kích

Vũ khí và dụng cụ nằm ở vòng 6 ô, không chiếm túi. Túi chỉ chứa thứ tiêu hao hoặc thứ cần tại chỗ: thuốc hồi, bó đạn tháp, bùa thoát (Bùa Hồi Hương, Ngọc Phá Sương, La Bàn Tán Vụ trong luật thoát rừng GDD §24) và đồ nhặt được giữa trận.

- **3 món (cơ bản)** đủ cho một đợt nhỏ: 1 bùa thoát + 1 thuốc + 1 chỗ cho đồ nhặt. Chật khi đi rừng: bùa thoát cho ba loại sự kiện đã là tới 3 ô, nên người chơi buộc chọn giữa thuốc và đồ nhặt. Đây là áp lực có chủ đích của cấp cơ bản, nhưng cần playtest.
- **8 món (cao cấp)** thoải mái cho đợt lớn: bùa thoát + 3 thuốc + 2 gói đạn + 2 chỗ nhặt. Không cần cấp trung gian để chơi được.
- Vì mỗi món chiếm đúng một ô (không xếp chồng), nhiều vật cùng loại (ba lọ thuốc) tốn ba ô. Đây là điểm khó chịu nhất: 3 ô đầy ngay nếu muốn mang vài lọ thuốc. Nên quyết định về kích thước chồng (câu hỏi 1) trước khi cân bằng số 3/8.
- Chuẩn bị xảy ra ngoài giao tranh qua liên kết kho nên không cần mang thừa. Khi bắt đầu đợt quái, túi giữ nguyên; liên kết đóng nên không thể "bơm" đồ giữa trận, chỉ tại cửa kho.

## Số chưa lấy được từ GDD (câu hỏi mở)

Triển khai bằng hằng số có tên trong `CarryRules` (Core/PlayerCarry.cs), mặc định thận trọng, không tự bịa cân bằng.

1. Kích thước chồng mỗi ô túi (`BagStackPerSlot`): đang là 1 món/ô (như ADR-0011). Một ô túi có chứa được nhiều món cùng loại không, và bao nhiêu?
2. Kích thước chồng trên tay trước vòng (`HandStackLimit`): "một chồng" chưa có kích thước; đang là 1 món. Đồ nhặt số lượng 2+ chỉ nhặt 1, phần còn lại nằm lại đất.
3. Kích thước chồng mỗi ô kho (`WarehouseStackLimitPerSlot`): kho cũ chồng không giới hạn; đang giữ nguyên. 100 ô tính theo (loại vật phẩm, chất lượng), không tính theo số lượng.
4. Các cấp vòng giữa sơ cấp và cao cấp: hiện chỉ có Basic (3) và Advanced (8) từ `SpaceBagTier`. Có cấp trung gian không, giá và nguồn nhận cấp cao cấp là gì (vẫn không có lệnh nâng cấp, như ADR-0011).
5. Nhiệm vụ khởi đầu chính thức: hai mục tiêu hiện tại là lựa chọn tối thiểu của người triển khai. Thiếu nguồn đồ nhặt ngoài đời để mục tiêu "gửi vật phẩm" làm được từ đầu ngoài việc lấy một món từ kho ra tay rồi gửi lại. Cần chỉ định đồ khởi đầu hoặc vật nhặt đầu tiên.
6. Dụng cụ nào được sở hữu từ đầu: hiện coi toàn bộ 11 loại của `StardewToolType` đều có sẵn (ở kho dụng cụ nếu chưa lên vòng). Có cần mở khóa dần không? Vòng mặc định: Rải cám, Lấy nước, Bảo trì, Tấn công, Chăm sóc, Bạch Vân.
7. Ranh giới "giao tranh": hiện là đợt quái hoặc chuyến rừng; đòn đánh lẻ ngoài đợt (`secondsSinceLastCombatAction` trong `PlayerCombatController`) không tính. Có cần tính không?
8. Lấy/gửi tại cửa kho trong giao tranh hiện được phép; chỉ liên kết từ xa bị đóng. Xác nhận ý định.

## Thay đổi liên quan

- Phím Tab của Tổng quan trại (`FarmOverviewPopup`) chuyển sang F vì Tab thuộc Vòng dụng cụ.
- Phím 1–6 chọn thẳng cung của vòng (không vẽ hàng hotbar).
- Test: `Tests/PlayerCarryTests.cs` (vòng dụng cụ, tay → vòng → kho 100 ô, túi chỉ giới hạn trong giao tranh, lưu/tải và save cũ).
