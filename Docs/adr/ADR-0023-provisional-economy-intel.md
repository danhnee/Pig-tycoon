# ADR-0023 — Kinh tế chu kỳ, chợ đêm "món muốn nhận", Bí nhân, Tình báo 0–5: các giá trị tạm (PROVISIONAL)

- Ngày: 2026-11-02
- Trạng thái: PROVISIONAL (chờ thiết kế xác nhận)
- Phạm vi: GDD_PIG_MECHANICS.md mục 20, 20.1, 20.2, 20.3, 21, 23.
- Mã Core: `MarketCycleThemes.cs` (bảng 8 chủ đề), `EconomyManager.cs` (gieo chủ đề, làm mới hằng ngày, phản ứng sau đột kích,
  sinh ô chợ đêm, kho Bí nhân), `NightMarketBarter.cs` (luật trao đổi, danh mục hàng), `VillageEconomy.cs` (`TradeForWants`),
  `IntelBureau.cs` (tình báo), `GameEngineEconomyIntel.cs` (nối chu kỳ, lưu/nạp `GameSaveData.EconomyIntel`).
- UI: `UI/DayMarketPopup.cs` (tên chủ đề), `UI/VillageBarterPopup.cs` (món muốn nhận, nhiều heo, bán 40%), `UI/IntelPopup.cs`,
  `Controllers/CustomBuildingController.cs` (`BanTinhBao = 15`, trạm `info_station`).
- Test: `Tests/EconomyIntelTests.cs` (6 test).

## Số lấy thẳng từ GDD (không phải tạm)

- 20.2 tỉ lệ chủ đề 24/12/12/10/8/10/12/12; giá heo và sức mua theo dải của từng chủ đề; Khan Hiếm Thức Ăn giá cám +35–70%;
  Mùa Heo Rẻ heo định giá ×1,0; Mùa Công Trình giá vàng −15%; sau đột kích heo +10–35% trong 1–3 ngày.
- 20.3 sức mua theo giai đoạn: đầu 20–60, giữa 50–120, cuối 100–220.
- 20.1 mỗi ô 1–2 món muốn nhận; thay cùng nhóm ≥ 110% GTTC; 18% ô nhận vàng, giá GTTC × 1,4; phí 5% GTTC bằng vàng;
  bán đồ lấy vàng 40% GTTC; heo = giá chợ ngày × 0,9. Bậc III–IV ≤ 2% ô vàng.
- 21 Bí nhân: 0% vàng, 130–200% GTTC, 2–4 ô từ bể hàng, ÁLSK +2…+10 mỗi giao dịch, 3% × hệ số × (1 + ÁLSK/100) tối đa 25%,
  2 giờ trong 18:00–04:00, hệ số Hoảng Loạn Dịch ×2,0 / Mùa Heo Rẻ ×1,5 / Cân Bằng ×1,0, đòi heo già 3–8 con.
- 23 tình báo: 150/300/450/600G cho cấp 1–4, cấp 5 chỉ Bí nhân/sự kiện, mua cấp N cần N−1, hết hạn khi sang chu kỳ,
  L1 70–85%, L3 "đợt lớn sau 8±2 ngày", L4 ±1 ngày + buổi; giao diện Cảnh báo / Tin đã xác nhận / Suy đoán.
- GTTC đã có trong code: `EquipmentAndTacticalCatalog.AllTwentyTacticalItems`, `QualifiedItemRegistry`, bản vẽ = giá xây
  (`DefenseBlueprint.BuildCostGold`, tiền lệ ADR-0016); Trà Lá Vông ≈ 650G (GDD 9); Tên Thép 6G/mũi (GDD 16).

## Quyết định tạm (PROVISIONAL)

| Chỗ | Giá trị chọn | Lý do |
| --- | --- | --- |
| Chu kỳ 1 | Cân Bằng trung tính ×1,0, sức mua 40, không làm mới hằng ngày; gieo chủ đề từ chu kỳ 2 | GDD không nói chu kỳ đầu; giữ hướng dẫn và các test cũ ổn định. |
| Làm mới hằng ngày | Giá heo, sức mua (và cám ở Khan Hiếm Thức Ăn) gieo lại mỗi 06:00 trong dải chủ đề; sức mua gốc gieo trong dải giai đoạn | GDD 20.2 "làm mới hằng ngày: giá, sức mua", không nói gieo lại hay cố định. |
| Giai đoạn sức mua | đầu = trước Cổng Giữa Game; giữa = `MidGameGateUnlocked`; cuối = chu kỳ ≥ 20 | Theo phân tích đọc GDD (cuối game từ chu kỳ 20+). |
| `ApplyMarketCycleTheme` tất định | Giá heo/sức mua = điểm giữa dải GDD | Cho mô phỏng/test; số chạy thật dùng `SeedCycleTheme` + `RollThemeDay`. |
| Số cũ còn giữ | Cơn Sốt Thịt cám ×1,10; Hoảng Loạn Dịch kiểm dịch 90G; Mùa Heo Rẻ heo con ×0,75; hệ số trang bị/vật liệu (×0,80/×1,25) | Có sẵn trong code trước ADR này, không có trong GDD; chưa có chỗ nào dùng hệ số trang bị/vật liệu. |
| Phản ứng sau đột kích | Áp sau MỌI đợt quái (nhỏ và lớn, qua `WaveEnded`); đợt kết thúc ngoài 06–18 thì tính từ ngày chợ kế; đè lên nhau lấy max | GDD chỉ nói "sau đột kích". Bỏ lối cũ ép chủ đề Khan Hiếm Hậu Đột Kích khi ≥ 2 công trình hỏng. |
| Hiệu ứng chợ đêm theo chủ đề | "nhiều ô / ×2" = trọng số ×2 cho nhóm hàng: Hoảng Loạn Dịch thuốc; Khan Hiếm Hậu Đột Kích đạn + vật liệu; Mùa Trang Bị trang bị; Mùa Công Trình bản vẽ + đạn; Mùa Heo Rẻ ô đòi heo; Khan Hiếm Thức Ăn ô đòi cám | GDD không cho tỉ lệ cụ thể. |
| Hệ số Bí nhân các chủ đề khác | ×1,0 | GDD 21 chỉ cho 3 chủ đề (dải chung 0,5–3,0). |
| Nhóm thay thế | Heo; vật liệu = danh mục Feed/Resource/Material/Ammo; còn lại = vật phẩm cùng bậc | GDD: "heo ↔ heo, vật liệu ↔ vật liệu, vật phẩm cùng bậc". |
| Bậc của món code không ghi bậc | GTTC < 300 → I, < 700 → II, < 1.500 → III, còn lại IV | Registry không có bậc. |
| Lô hàng mỗi ô | thuốc ×3, Tên Thép ×50, Xăng Sinh Học ×2, món < 100G ×2, còn lại ×1 | GDD không nói số lượng mỗi ô. |
| Số món muốn nhận | 50% 1 món / 50% 2 món; món đầu 60–80% giá trị | GDD "1–2". |
| Số heo mỗi món muốn nhận | 1–2 con (theo ví dụ GDD); bỏ yêu cầu "đã kiểm dịch" | Chưa có thao tác kiểm dịch heo giữ lại (chỉ kiểm dịch lúc bán). |
| Phí 5% | 5% GTTC của lô nhận về, làm tròn; Bí nhân 0 phí (0% vàng) | GDD "5% GTTC" không nói của bên nào. |
| Ô vàng bậc III–IV | Ô vàng chỉ bốc hàng bậc I–II (giữ đúng 18%) | GDD "gần như chỉ bậc I–II; III–IV ≤ 2%". |
| Bản vẽ ở chợ đêm | Bản vẽ chưa học, không Bạch Vân; GTTC = giá xây | Bạch Vân để cho đấu giá và Bí nhân. |
| Bí nhân — bể hàng | GTTC tạm: Tình Báo Toàn Phần 1.500G (= tổng 150+300+450+600), Quả Mộng 3.200G (IV), Hương Mê Thảo 2.000G (III), Thuốc chữa Hư Mạch 900G (III). "Bản vẽ Cao cấp/Bạch Vân" = bản vẽ Bạch Vân chưa học, không còn thì 3 bản vẽ đắt nhất | Không có số trong GDD/code. |
| Bí nhân — món đòi | 1 heo gen "?" / 1 heo Quý+ / heo già 3–8 / xác heo 1–3 / vật phẩm bậc III–IV (tổng đủ giá); trọng số ×2 xác (Hoảng Loạn Dịch), ×2 heo già (Mùa Heo Rẻ); xác heo GTTC = giá chợ ngày × 0,9 như heo; Bí nhân không nhận thay thế | GDD liệt kê loại, không cho tỉ lệ; "1–3 xác" là tạm. |
| Tình báo L1 | Đúng 78% (trong 70–85%); dải độ dài chu kỳ rộng 3 ngày luôn chứa giá trị thật | Một điểm trong dải GDD. |
| Tình báo L2 | 1–2 yếu tố thật mạnh nhất trong số: heo già vượt ngưỡng mềm, xác ngoài bãi, lô xác, mật độ > 1,2, Sinh Cảnh < 50, sương mù, heo bệnh | Lấy từ `Farm.UpdateDailyEventPressure`. |
| Tình báo L3 | "Đợt lớn sau c±2 ngày", c lệch thật −1..+1; dải ÁLSK rộng 20 chứa giá trị thật | GDD ví dụ "8±2"; dải ÁLSK là tạm. |
| Tình báo L4 | Cửa sổ rộng 2 ngày (±1) quanh tâm lệch −1..+1; giờ = buổi tối (đợt lớn 20:00, `EnemyWaveSpawner.MajorWaveHour`) | GDD "±1 ngày, thiên về buổi". |
| Phân loại giao diện | Cảnh báo: tín hiệu L0, "sau c±2 ngày", cửa sổ L4, giờ chính xác L5. Xác nhận: yếu tố L2, dải ÁLSK L3, mọi tin L5. Suy đoán: tin L1 | GDD chỉ yêu cầu tách 3 mục. |

## Còn thiếu (không tự đặt số)

- "1 ngày thời gian" của Bí nhân; Quạ Tin / Đài Dò Dị Tượng báo trước giờ Bí nhân; Huyết Triện / Tinh Hoa Dị Biến chưa có vật phẩm.
- Cơn Sốt Thịt "đấu giá sôi động", Mùa Trang Bị "bí kíp, Mực Phù" chưa có hàng tương ứng.
- Hệ số giá cám chưa có chỗ mua cám dùng tới; vật phẩm ngủ mua ở chợ vào kho chung, chưa vào `SleepItemsInventory`.
- Ô chợ đêm không lưu (sinh lại sau khi nạp), như trước ADR này.
- Mộng Nhãn "Giấc Báo" (tình báo cấp 4 miễn phí) chưa nối vào `IntelBureau.GrantFull/Level`.
