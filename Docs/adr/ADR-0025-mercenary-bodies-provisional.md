# ADR-0025 — Lính đánh thuê có thân trong trận

Ngày: 02/11/2026 · Trạng thái: chấp nhận. Hai số đánh dấu TẠM, chỉnh sau khi chơi thử.

## Bối cảnh

Trước đây, thuê lính (`RecruitmentRules.Hire`) chỉ tạo bản ghi `HiredNpcRecord`. Không có thân, không đánh nhau. Audit logic ↔ UI xếp mục này "chưa đạt": có logic, người chơi không thấy.

## Quyết định

- **Core `MercenarySpecs`:** 5 lính lấy đúng bảng GDD 11.2.
  - Vệ Binh Khiên: 420 HP · 18/đòn · cận chiến.
  - Xạ Thủ Nỏ: 180 HP · 35/phát · 18 m · săn tinh anh · Mắt Ưng +25% lên mục tiêu bị đánh dấu.
  - Kỹ Sư Chiến Trường: 220 HP · 12/đòn.
  - Thợ Săn UFO: 200 HP · 90/phát · 25 m · chỉ bắn quái bay · trúng UFO đang kéo heo thì heo rơi xuống.
  - Thích Khách Đêm: 240 HP · 45/đòn · ưu tiên quái trộm và quái tàng hình · lộ tàng hình trong 8 m.
  - Mọi lính có Stamina 150; hết Stamina thì rút về Cờ Tập Kết (không có thì về chỗ người chơi) trong 8 giây.
- **Runtime:**
  - `MercenaryRoster` thả lính đang thuê ra khi bắt đầu đợt quái ở trại và thu về khi hết đợt.
  - `MercenaryAgentView` là `IDamageable`, nên quái đánh lính được.
  - Lính cận chiến kéo quái về phía mình (`ForceTarget`). Vệ Binh Khiên giữ quái lâu hơn, đúng vai "chặn cửa".

## Số TẠM (GDD không ghi)

| Số | Giá trị tạm |
|---|---|
| Nhịp đánh của lính | 1,0 giây/đòn |
| Stamina mỗi đòn | 5 |

## Chưa làm

- Kỹ Sư "đặt lại bẫy đã cạn 2 lần/đợt".
- Thợ Săn UFO "Đạn Xuyên Giáp" (1 phát/đợt ×4).
- Vệ Binh Khiên "Tường Người" (hứng 30% sát thương cho công trình phía sau).
- Thuê dài hạn (×2,5, mở đặc tính).
- Lính bị hạ chỉ rời trận đợt đó; GDD chưa nói lính có chết hẳn không.

Test: `MartialProgressionTests.RunMercenaries`.
