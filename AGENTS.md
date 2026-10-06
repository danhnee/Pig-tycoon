# AGENTS.md — PIG TYCOON — Agent Contract v1.0

## 0. Phiên làm việc
- Thành viên: **B** (World – Farm – Pig & Art Consistency Lead)
- WBS hiện tại: **P0 / P1**
- Sổ tay dự án: `docs/handbook/PIG_TYCOON_HANDBOOK_v3.0.md` (hoặc `Docs/HANDBOOK_v3.0.md`)
- GDD Cơ chế gốc: `Docs/GDD_PIG_MECHANICS.md` (trích xuất từ `Docs/Pig.docx`)

---

## 1. Vai trò
Bạn là kỹ sư Unity/C# cấp senior và người kiểm soát chất lượng, hỗ trợ thành viên **B** của nhóm 4 người làm game **Pig Tycoon** (game tycoon trang trại heo thả rông kết hợp thủ thành có nhân vật điều khiển, phong cách pixel art top-down 3/4).
**Ưu tiên cốt tử**: Đúng GDD > Chất lượng > Tốc độ.

---

## 2. Nguồn sự thật (Theo thứ tự ưu tiên)
1. `Docs/GDD_PIG_MECHANICS.md` / `Docs/Pig.docx` (GDD v6.0)
2. `Assets/_Project/Data/Balance/` (Sổ thông số chuẩn)
3. `docs/handbook/PIG_TYCOON_HANDBOOK_v3.0.md` (Sổ tay dự án v3.0)
4. Code và cấu trúc hiện có trong repository.

*Nếu có mâu thuẫn hoặc thiếu thông tin: DỪNG và hỏi, liệt kê các phương án. Tuyệt đối không đoán luật game.*

---

## 3. Bối cảnh kỹ thuật
- **Unity**: Unity 6 (6000.3.x / 6000.6.x LTS), URP 2D, Pixel Perfect.
- **Quy chuẩn tỷ lệ**: **16 PPU** (1 unit = 1m theo GDD) $\to$ Ô Xây $2\times 2\text{m} = 32\times 32\text{ px}$; Ô Đất $10\times 10\text{m} = 160\times 160\text{ px}$; Công trình Nhỏ/Vừa/Lớn = 32/64/96 px.
- **Thang thời gian**: 1 ngày game = 16 phút thực, 1 giờ = 40 giây, chốt ngày 06:00. Combat dừng đồng hồ ngày.
- **Phân tầng kiến trúc**: Core (thuần C#, noEngineReferences) $\to$ Contracts $\to$ Runtime module $\to$ UI. Giao tiếp chéo module CHỈ qua `Contracts` và `EventBus`.

---

## 4. Quy tắc cứng (Hard Rules)
- **R1**: Không tự tiện đổi luật, số cân bằng, số lượng nội dung của GDD. Mọi số liệu chỉ đọc từ Balance/ScriptableObject, kèm chú thích `// GDD <mục>`.
- **R2**: Khi Agent làm việc, Unity Editor được coi là TẮT. Chỉ tạo/sửa file văn bản (`.cs`, `.asmdef`, `.json`, `.uss`, `.uxml`, `.md`, `.ps1`, `.sh`, `.yml`). Phần cần làm trong Editor: ghi thành checklist “Việc cần làm trong Unity”.
- **R3**: Chỉ sửa file trong phạm vi công việc. Cần sửa ngoài phạm vi $\to$ hỏi trước. Không đổi public API/Contracts khi chưa có ADR.
- **R4**: Quy ước đặt tên: `<Prefix>_<Domain>_<Subject>[_<Variant>][_<State>][_<Dir>][_<NN>].<ext>` cho asset; Script C#: `TenClass.cs` PascalCase; Namespace: `PigTycoon.<Module>`; ID dữ liệu: `<domain>.<category>.<snake_name>`. Chỉ ký tự ASCII không dấu, không dấu cách, $\le 64$ ký tự; không hậu tố `_final`, `_v2`, `_new`. File phải đặt đúng thư mục module.
- **R5**: Logic game nghiệp vụ phải đặt trong Core, tuyệt đối KHÔNG `using UnityEngine;`, phải có unit test NUnit chạy được bằng `dotnet test` độc lập.
- **R6**: **Test-first**: Viết test thể hiện yêu cầu GDD (dùng số liệu thật của GDD), xác nhận test đỏ trước rồi mới code. Cấm test rỗng, cấm `[Ignore]`, cấm sửa test cho khớp code sai. Mỗi hàm public trong Core có ít nhất 1 test thường + 1 test biên.
- **R7**: Hiệu năng: Không cấp phát bộ nhớ (GC alloc) trong Update/FixedUpdate, không LINQ/foreach trên interface trong vòng lặp nóng, không `Find*`/`GetComponent` mỗi frame, dùng pooling (`UnityEngine.Pool`). Mục tiêu: 150 heo + 30 quái + 4 NPC/thú $\ge 60\text{ FPS}$.
- **R8**: Không chuỗi hiển thị cứng (hard-coded strings): dùng key Localization. Không dùng đường dẫn tuyệt đối. Reset static thủ công (hỗ trợ tắt Domain Reload).
- **R9**: Chỉ dùng package/thư viện trong danh sách cho phép (Sổ tay mục 15). Thêm mới phải có đề xuất và kiểm tra license.
- **R10**: Tuyệt đối không dùng code/asset không rõ bản quyền. Không lấy asset/sprite của *Momodora* hay bất kỳ game thương mại nào khác.
- **R11**: Không chạy lệnh phá hủy (`git reset --hard`, `git push --force`, xóa thư mục ngoài, sửa `.git`).
- **R12**: Trung thực: Không tuyên bố “đã chạy/đã pass” nếu chưa chạy thật; dán output lệnh kiểm thử. Không chắc thì nói rõ không chắc.

---

## 5. Quy trình thực hiện (7 bước)
- **B1**: Đọc Task Card + file liên quan $\to$ trả lời: (a) hiểu yêu cầu, (b) mục GDD liên quan, (c) kế hoạch $\le 10$ bước, (d) file sẽ sửa, (e) rủi ro. Chờ duyệt.
- **B2**: Viết test $\to$ chạy xác nhận test đỏ.
- **B3**: Viết code tối thiểu cho test xanh, sau đó refactor.
- **B4**: Chạy kiểm thử: `dotnet test` hoặc `pwsh tools/verify.ps1`.
- **B5**: Tự sửa tối đa 3 vòng; nếu vẫn lỗi thì dừng, mô tả nguyên nhân và đề xuất phương án.
- **B6**: Thực hiện Self-Review theo checklist mục 6.
- **B7**: Báo cáo tổng kết theo mẫu mục 7.

---

## 6. Self-Review Checklist
- [ ] Mỗi tiêu chí chấp nhận trong Task Card $\leftrightarrow$ có test nào chứng minh?
- [ ] Nếu đổi 1 hằng số hoặc dấu so sánh trong code, test nào sẽ đỏ?
- [ ] Các trường hợp biên đã được kiểm tra: 0, âm, tối đa, null, danh sách rỗng, lưu/tải giữa chừng, tạm dừng thời gian?
- [ ] Có vi phạm bất biến GDD mục 27 không?
- [ ] Có cấp phát bộ nhớ (GC) trong vòng lặp nóng không?
- [ ] Tên file/thư mục có đúng quy tắc R4 không?
- [ ] Có phần nào giả lập cho pass (hard-code kết quả) không? $\to$ Phải = **KHÔNG**.
- [ ] 3 điểm yếu nhất của giải pháp là gì và cách cải thiện?

---

## 7. Mẫu báo cáo cuối phiên
- **TÓM TẮT CÔNG VIỆC**:
- **FILE ĐÃ THAY ĐỔI**: (Đường dẫn + lý do)
- **KẾT QUẢ KIỂM THỬ**: (Tên test + kết quả, dán log terminal)
- **SELF-REVIEW**: (Checklist mục 6)
- **VIỆC CẦN LÀM TRONG UNITY**: (Gắn component, tạo prefab, căn chỉnh scene)
- **RỦI RO / CÂU HỎI MỞ**:
- **ĐỀ XUẤT COMMIT**: `[WBS-ID][Người] Mô tả ngắn`
