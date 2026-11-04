# ADR-0022 — Trạng thái thời tiết lên người chơi: các giá trị tạm (PROVISIONAL)

- Ngày: 2026-11-02
- Trạng thái: PROVISIONAL (chờ thiết kế xác nhận)
- Phạm vi: GDD_PIG_MECHANICS.md mục 7.2 (cột "Trạng thái lên người (ngoài trời)" và "Phòng tránh"), 9.3 (hàng "Trạng thái thời tiết"), 12.3 (trang bị phòng tránh).
- Mã: `Assets/Scripts/Core/PlayerWeatherStatus.cs` (Core, chạy mỗi giờ từ `GameEngine.OnHourlyTick`, lưu ở `GameSaveData.PlayerWeatherStatus`),
  `Assets/Scripts/Controllers/PlayerWeatherStatusDriver.cs` (runtime), HUD `UI/Myth/MythHudController.cs`.

## Số lấy thẳng từ GDD (không phải tạm)

- Cảm Nắng: 2 giờ liên tục ngoài trời trong 10:00–16:00, 20% dính, Stamina tối đa −10%, hết khi nghỉ bóng mát 1 giờ.
- Ướt Lạnh: sau 1 giờ ngoài trời dưới Mưa, Miễn dịch hao ×1,5, kéo dài 3 giờ sau khi vào nhà. Áo Mưa Dầu miễn.
- Sét Đánh: mỗi giờ ngoài trời xa mái che > 15 m, 3%: HP −40, choáng 3 giây. Vòng Cách Điện: −80% khả năng bị sét (3% × 0,2), miễn choáng điện.
- Cảm Gió: mỗi 2 giờ ngoài trời trong Giông Bão, 10%: Miễn dịch −10, Stamina +10% suốt ngày.
- Lạc Hướng: bản đồ nhỏ không hiện gì ngoài 15 m. Mặt Dây Đèn Bão miễn.
- Cóng Tay: thời gian thao tác (sửa, tiêm, giám định) +15%. Quần Da Lót Bông miễn.
- Khô Rát: chạy nhanh tốn thêm 0,2 Stamina/giây.
- Nhiễm Xạ: mỗi giờ ngoài trời +1 tầng, tối đa 5; mỗi tầng Miễn dịch −4; ≥ 3 tầng HP hồi −50%. Áo Chì miễn.
- 9.3: Ướt Lạnh +5%, Cảm Gió +10%, Cảm Nắng trần −10%, Nhiễm Xạ ≥ 3 +10% (cộng vào `FarmLaborStamina.EnvironmentMultiplier`).

## Quyết định tạm (PROVISIONAL)

| Chỗ | Giá trị chọn | Lý do |
| --- | --- | --- |
| Ngoài trời / trong nhà | Runtime: ngoài trời trừ khi người chơi đứng trong vùng tương tác của một `CustomBuildingController` hoặc cảnh có tên chứa "Interior". Ngủ chuẩn (`PlayerRestCommands.Sleep`) đặt trong nhà. Core mặc định ngoài trời. | Chưa có khái niệm "trong nhà" trong game; chỉ đạo: coi là ngoài trời trừ khi tương tác công trình. |
| "Xa mái che > 15 m" | Khoảng cách tới tâm `CustomBuildingController` gần nhất > 15 m; cảnh không có công trình nào = xa. Mái Che Tạm / Cột Thu Lôi chưa tính. | GDD không định nghĩa "mái che" trong cảnh 2D. |
| Thời điểm tung Cảm Nắng | Tung 20% ở mỗi giờ thứ 2, 4, 6… liên tục ngoài trời trong khung giờ (1 lần mỗi khối 2 giờ). | GDD chỉ nói "sau 2 giờ liên tục". |
| "Bóng mát" | Chỉ tính giờ ở trong nhà (theo định nghĩa trên); ra ngoài trời ngoài khung nắng không tính là nghỉ mát. | Mái Che Tạm chưa có trong runtime. |
| Trần Cảm Nắng | Trần = Trần hiệu dụng − 10% × Stamina tối đa (sau Mệt Dội). Core kẹp mỗi giờ; runtime kẹp mỗi khung hình (LateUpdate) sau khi hồi. | Không sửa được `Character.cs`; GDD nói "Stamina tối đa −10%". |
| Ướt Lạnh "Miễn dịch hao ×1,5" | Game và GDD chưa có tốc độ hao Miễn dịch nền cho người chơi. ×1,5 áp cho mọi lượng Miễn dịch mất do thời tiết đi qua `PlayerWeatherStatus.ApplyImmunityLoss` (Cảm Gió −10, Nhiễm Xạ −4/tầng). Không tự đặt tốc độ hao nền. | Không bịa số nền. Hệ quả: trong thực tế Ướt Lạnh chủ yếu có tác dụng +5% Thể lực (9.3) cho tới khi thiết kế cho tốc độ hao nền. |
| Đếm 3 giờ Ướt Lạnh | Đếm lùi mỗi giờ không bị ướt (trong nhà, hết mưa, hoặc đã mặc Áo Mưa Dầu). | GDD chỉ nói "sau khi vào nhà". |
| Sét làm HP về 0 | HP còn tối thiểu 1, không bất tỉnh. | Luật bất tỉnh GDD 8.2 ("tỉnh lại sau đợt quái") gắn với đợt quái; runtime chưa có luồng bất tỉnh ngoài đợt quái. |
| Đếm "mỗi 2 giờ ngoài trời" của Cảm Gió | Cộng dồn số giờ ngoài trời trong Giông Bão của ngày nông trại hiện tại, tung 10% ở giờ thứ 2, 4, 6…; không tung khi đang Cảm Gió. | GDD không nói liên tục hay cộng dồn. |
| "Suốt ngày" của Cảm Gió | Tới khi đổi ngày nông trại (06:00, `Clock.CurrentDay`). | Ngày nông trại bắt đầu 06:00. |
| "Mỗi tầng Miễn dịch −4" | Trừ 4 một lần khi nhận tầng (không trừ lặp mỗi giờ). | GDD không nói trừ một lần hay theo giờ. |
| Nhiễm Xạ giảm tầng | −1 tầng mỗi giờ không bị phơi xạ (trong nhà, hết Mưa Bức Xạ, hoặc mặc Áo Chì). | GDD không cho tốc độ giảm. |
| Cóng Tay / Khô Rát / Lạc Hướng | Có hiệu lực ngay khi ở ngoài trời trong thời tiết tương ứng, hết ngay khi vào nhà. | GDD không cho thời gian phơi. |
| Khô Rát vào Tải ngày | Phần +0,2/giây theo tỉ lệ Tải ngày của chạy (20%, `MovementSpec.RunDailyLoadFraction`). | Cùng loại tiêu hao với chạy. |
| RNG | Dòng random riêng (seed ván XOR hằng), không lưu. | Không xê dịch chuỗi random của các hệ khác. |

## Chưa làm

- Lạc Hướng trên bản đồ nhỏ: chưa có minimap UI trong runtime 2D (chỉ có `MinimapProjectionSystem` trong Core, không ai vẽ).
  Core cung cấp `PlayerWeatherStatus.HasLacHuong` / `MinimapRadiusMeters` (đã tính Mặt Dây Đèn Bão) cho minimap sau này.
  `MinimapProjectionSystem.IsEntityVisibleOnMinimap` hiện ẩn theo thời tiết Sương Mù mà không xét Đèn Bão (file chung, không sửa).
- Cóng Tay chưa nối vào thời gian sửa công trình (`DefenseRepairs`, file phòng thủ, không thuộc phạm vi); tiêm và giám định hiện là thao tác tức thời.
  Chủ sở hữu nên nhân phút sửa với `engine.WeatherStatus.TaskTimeMultiplier(engine)`.
- Nhiễm Xạ "HP hồi −50%": chưa có hồi HP bị động; hồi HP của võ kỹ nằm trong `Controllers/Combat` (không thuộc phạm vi).
  Core cung cấp `PlayerWeatherStatus.HpRegenMultiplier`.
- "Sương mù huyền bí cuốn đi", Bình nước mang theo (Khô Rát), Mái Che Tạm, Cột Thu Lôi, sưởi mái trú: chưa làm.
- Trơn Trượt (Nồm Ẩm) do kỹ sư khác làm; không đụng.
- Miễn dịch người chơi (`StaminaSystem.Immunity`) chưa được `GameSaveData` lưu (lỗ có sẵn, không thuộc phạm vi).
