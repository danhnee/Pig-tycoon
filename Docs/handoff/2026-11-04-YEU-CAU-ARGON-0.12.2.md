# Yêu cầu cho Argon (Gemini 4) — bản 0.12.2

Ngày 4/11/2026. Người soạn: Claude (logic), theo phản hồi của người dùng sau khi chơi APK 0.12.0 trên điện thoại.

Người dùng đánh giá: "cái gì cũng tệ". Bản này liệt kê đủ từng lỗi người dùng đã chỉ ra, kèm những lỗi Claude tìm thêm khi chơi thử như người chơi. Mỗi mục ghi rõ: lỗi thấy được là gì, ai làm phần nào, và thế nào thì coi là xong.

**Quy tắc làm việc (bắt buộc):**
- Chỉ làm trong `D:\Pig-tycoon`, nhánh `feat/codex/2d-mythic-rebuild`. Thư mục `D:\Pig-tycoon-2D-migration-20261030` không còn; không tạo lại.
- Bắt đầu từ bản `D:\Pig-tycoon\.collab\claude-2d\ui_and_map_sync\from_claude\` mới nhất (đồng bộ ngày 4/11 sau khi viết tài liệu này), giao vào `from_argon\`.
- Không sửa thẳng `D:\Pig-tycoon\Assets`.
- Đây là nâng cấp bản 0.12.2 trên cùng nhánh. Không tạo nhánh mới, không làm lại từ đầu.
- Không đổi số cân bằng trong GDD (giá, sát thương, thời gian).
- Mỗi lần giao, kèm danh sách file và ảnh chụp trong game, không phải ảnh dựng riêng.

---

## Phần A — Claude đã sửa trong 0.12.2 (Argon không cần làm lại)

| # | Lỗi người dùng thấy | Nguyên nhân | Đã sửa |
|---|---|---|---|
| A1 | Nhân vật "bị gắn chặt vũ khí dù chưa trang bị" | Game mới tự trang bị Dao Rựa (`Character.EquipStarterSet`, ADR-0017). Lớp paper-doll vẽ dao trên mọi khung hình: đứng, đi, cho ăn. `WeaponSocketController` còn tự vẽ Dao Rựa khi đánh dù ô vũ khí trống, nên có lúc hiện 2 lưỡi. | Vũ khí chỉ hiện khi đang ra đòn (clip `attack*`, `skill*`, `counter*`, `guard*`), giống Stardew: cầm dụng cụ lên khi vung. Ô vũ khí trống thì tay không. Paper-doll đã vẽ vũ khí thì socket cũ không vẽ thêm. Quy tắc nằm ở `EquipmentAppearance.ShowsWeapon(clip)` / `Visible(slot, clip)` trong Core, có test. |
| A2 | "Nhân vật đi xuyên qua các công trình" | Collider trong layout là một dải mỏng đặt giữa đường chân tường: một nửa nằm trên cỏ phía trước, cả thân nhà phía trên đi xuyên được (ảnh: `.agent_scratch/collider_check.png`). | `PixelMythWorldBuilder.PlaceObject`: với `building:*`, collider bắt đầu ở chân tường và chạy sâu khoảng 60 % kích thước hình. Riêng Nơi Trú Đàn mở giữ nguyên, vì heo phải đi vào được. Cửa vẫn tương tác được (cửa cách chân tường 1,2 m, bán kính 1,8 m). Đã kiểm tra không đè máng, giếng hay điểm NPC trên cả 4 map. |
| A3 | Điện thoại: bấm nút cạnh cần di chuyển (vòng đồ) thì "bay hết UI" | Vòng dụng cụ bị tính là một modal, nên `MythHudController` đặt cả HUD alpha = 0: mất cần di chuyển, mất mọi nút. | Thêm `UISpriteLoader.IsAnyModalOpenIgnoringToolWheel()`. HUD giữ nguyên khi mở vòng dụng cụ; vòng vẫn chặn phím tắt như cũ. |
| A4 | Hộp xác nhận nâng nhánh còn đè lên nút khi mở lại bảng Phòng Thủ | `DefenseBuildPopup.Hide()` không tắt `BranchConfirmModal`. | Đóng bảng thì tắt hộp và xoá hành động đang chờ. |
| A5 | Công trình "chỉ đứng, tương tác là ra 1 cái bảng" | Chưa có nội thất đi vào được. | Đã làm xong phần logic để đi vào nhà kiểu Stardew (xem Phần B1). Phần còn thiếu là map nội thất do Argon vẽ. |

---

## Phần B — Việc của Argon (xếp theo mức người dùng bực nhất)

### B1. Nội thất đi vào được như Stardew — ƯU TIÊN 1

**Người dùng nói:** "các công trình thuần dựng để đấy không thể vào bên trong… tương tác là ra 1 cái bảng thì gọi là tương tác cái gì… phải xây cả bên trong các công trình đấy, đi được vào nhà".

**Logic đã sẵn (Claude):**
- Mỗi nội thất là một map nhỏ, dựng tự động từ `Assets/Art/PixelMyth/Maps/<Scene>_layout.json`, cùng định dạng với 4 map ngoài trời.
- Có file layout thì `BuildAll` tự làm 3 việc:
  - dựng cảnh và đưa vào bản build;
  - đặt cổng ở cửa công trình bên ngoài (1,2 m trước chân tường);
  - nối cửa → nội thất → cửa.
- Lưu game khi đang ở trong nhà thì tải lại vẫn ở trong nhà. Đã có test.
- Chưa có file layout thì công trình vẫn mở bảng nội thất như hiện tại, nên không có gì bị hỏng.

**Danh sách nội thất (`Core/World/PixelWorldInteriors.cs`):**

| LocationId = tên scene = tên file `<…>_layout.json` | Ở map | Công trình (interact id ngoài map) | Cổng ra trong layout nội thất |
|---|---|---|---|
| `Interior_NhaChinh` | Farm | `main_house` | `Interior_NhaChinh->Farm` |
| `Interior_XuongRen` | Farm | `construction_hall` | `Interior_XuongRen->Farm` |
| `Interior_QuanBaSau` | Hub_Village | `quan_ba_sau` | `Interior_QuanBaSau->Hub_Village` |

Cần thêm nhà nào (chuồng, kho, nhà nghiên cứu, quán ở thị trấn), Argon báo id công trình và Claude thêm một dòng.

**Argon giao cho mỗi nội thất:**

1. **File layout `Assets/Art/PixelMyth/Maps/Interior_NhaChinh_layout.json`.** Ví dụ tối thiểu:
   ```json
   {
     "map": "Interior_NhaChinh", "location_id": "Interior_NhaChinh", "scene_name": "Interior_NhaChinh",
     "size": [18, 12], "tile_px": 32,
     "terrain": { "legend": { "t": "dirt", "s": "stone" }, "corner_rows": ["…19 dòng × 19 ký tự…"] },
     "objects": [
       { "sprite": "Assets/Art/PixelMyth/Interiors/nha_chinh/floor_wood.png", "x": 9, "y": 6, "layer": "under" },
       { "sprite": ".../wall_back.png", "x": 9, "y": 11, "collider": { "w": 18, "h": 1.2, "ox": 0, "oy": 0 } },
       { "sprite": ".../bed.png", "x": 3, "y": 8, "collider": { "w": 2, "h": 1, "ox": 0, "oy": 0 },
         "role": "station:bed", "interact": { "function": "NghiNgoiGiuong", "id": "nha_chinh_bed" } },
       { "sprite": ".../chest.png", "x": 14, "y": 8, "collider": { "w": 1.2, "h": 0.8, "ox": 0, "oy": 0 },
         "interact": { "function": "MoKhoLuuTru", "id": "nha_chinh_chest" } }
     ],
     "portals": [ { "id": "Interior_NhaChinh->Farm", "target": "Farm", "target_portal": "Farm->Interior_NhaChinh", "x": 9, "y": 0.8 } ],
     "spawn": { "x": 9, "y": 2.5 },
     "npcs": [], "landmarks": [], "zones": [], "gate_event_zones": [], "enemy_entries": []
   }
   ```
2. **Cửa ra nằm sát mép dưới map.** Builder tự đặt điểm xuất hiện vào trong 3 m từ mép.
3. **Tường, đồ đạc to (giường, bếp, quầy) phải có `collider`.** Đặt `x`/`y` ở chân vật (pivot đáy), giống ngoài map.
4. **Trạm tương tác dùng `interact.function` có sẵn:**
   - `NghiNgoiGiuong`: ngủ / nghỉ;
   - `MoKhoLuuTru`: rương / kho;
   - `PhongXayDung`: bàn bản vẽ;
   - `GiaoDichDanLang`: quầy Bà Sáu;
   - `BanTinhBao`: bàn tình báo;
   - `GiamDinhGen`: bàn giám định;
   - `DocBangTinLore`: tranh, sổ, bảng tin.

   Mỗi trạm làm đúng việc mà bảng nội thất hiện tại đang làm: bữa ăn +35, kho, sổ trại…
5. **Art nội thất:**
   - top-down 3/4 như ngoài map, 32 PPU;
   - phong cách thần thoại Đông–Tây, riêng thị trấn theo châu Âu cổ;
   - tường sau cao 2–3 ô, sàn gỗ hoặc đất nện, có ánh lửa bếp;
   - đồ vật đọc được không cần chữ: giường, bếp lò, chum, rương, đe, lò rèn, quầy rượu.
6. **Ngoài map:** cửa công trình phải nhìn ra được là cửa (khung, bậc, ánh đèn), đặt ở giữa chân tường phía trước.

**Coi là xong khi:**
- đi tới cửa, bấm E hoặc nút chạm thì vào nhà;
- đi lại trong nhà không xuyên tường;
- dùng được 2 trạm trở lên;
- đi ra thì đứng ngay trước cửa;
- lưu game khi ở trong nhà rồi tải lại vẫn ở trong nhà.

### B2. Dáng đi "như bay, hai chân dính vào nhau" — ƯU TIÊN 1

**Người dùng nói:** "nhân vật di chuyển mà như bay vậy, hai chân cứ dính vào nhau di chuyển cực kì dị dạng".

**Claude kiểm tra sheet `an_walk.png` / `khoa_walk.png`** (ảnh: `.agent_scratch/an_check.png`, `.agent_scratch/khoa_check.png`):
- nhìn trực diện, hai bàn chân gần như không tách nhau;
- thân không nhún lên xuống;
- tay không đánh;
- 8 khung gần giống nhau;
- kết quả là nhân vật trượt như bay.

**Thông số runtime giữ nguyên:**
- tốc độ đi 4,5 m/s;
- sheet khai `stride_m_per_cycle: 1.8`, 8 khung;
- runtime tự chỉnh fps cho chân khớp quãng đường.

Argon chỉ cần vẽ lại chu kỳ chân cho đúng.

**Yêu cầu vẽ lại** `walk`, `run` (An, Khoa và mọi lớp trang bị paper-doll đi kèm):
- **Chu kỳ chuẩn 8 khung:** tiếp đất (contact), hạ thấp (down), chuyển (passing), đẩy lên (up), lặp lại cho chân kia.
- **Hướng xuống/lên:** hai bàn chân phải tách rõ, mỗi chân luân phiên ra trước; khoảng cách gót ít nhất 6–8 px ở khung contact.
- **Nhún thân:** đầu và hông hạ 1–2 px ở khung down, lên 1 px ở khung up.
- **Tay:** đánh ngược chân (chân trái trước thì tay phải trước), biên độ 4–6 px.
- **Chạy:** có khung bay (hai chân rời đất), thân nghiêng tới trước.
- **Tỉ lệ:** giữ người khoảng 56 px, không kéo dài chân (đã có bài học "chân cà kheo").
- **Cách làm:** không cắt-dán-kéo giãn tay chân từ ảnh tĩnh. Đây là nguyên nhân hỏng lần trước; vẽ hoặc render lại cả khung.

**Coi là xong khi:** quay video 3 giây đi 4 hướng trong game, nhìn chân không trượt, đọc được bước trái/phải.

### B3. Animation hành động và chiến đấu "chỉ có 1 cái vệt chém" — ƯU TIÊN 1

**Người dùng nói:** "các animation thực thi… kể cả combat… tay chân không di chuyển tương ứng cả cơ thể, thiếu tính vật lý chuyển động, mà chỉ ra có 1 cái duy nhất là 1 cái vệt chém".

**Claude kiểm tra `an_attack_slash_1.png`:**
- thân chỉ nghiêng nhẹ, tay không vung rõ;
- toàn bộ cảm giác đòn nằm ở vệt sáng xanh vẽ dính trong sheet.

**Yêu cầu cho mỗi đòn** (slash, thrust, smash, ranged × 3 đòn liên hoàn, cùng counter, dodge, hit, faint):
- **Ba pha rõ:**
  1. lấy đà (anticipation): thân xoay ngược, tay kéo vũ khí ra sau, 2–3 khung;
  2. ra đòn (contact): thân xoay theo, chân trước bước tới hoặc trụ, tay duỗi hết, 1–2 khung;
  3. thu về (recovery): 2–3 khung.
- **Chân trụ:**
  - smash: hạ trọng tâm;
  - thrust: bước dài tới trước;
  - slash: xoay hông.
- **Vũ khí:** không vẽ chết vào sheet thân. Vũ khí là lớp paper-doll riêng (`an__wpn_<id>`), cùng khung với thân. Nhờ vậy tay không thì không có dao (khớp sửa A1), đổi vũ khí thì đổi lớp.
- **Vệt chém:** tách thành FX `Art/PixelMyth/FX/fx_<cue>.png` (8 khung 48×48), không vẽ dính vào thân. Runtime đã đọc `effect_frame` của sheet để tính khung trúng đòn; Claude sẽ cho FX vệt chém phát đúng khung đó.
- **Bị đánh:** giật lùi 2–4 px, nháy trắng 1 khung (runtime làm được nếu Argon cần).
- **Hành động nông trại** (cho ăn, xách nước, sửa rào, dọn, quan sát): cả thân tham gia (cúi, bước, nhấc), không chỉ đưa một tay. Đồ cầm theo đúng hành động (xô, cám, búa) và chỉ hiện trong lúc làm.

**Coi là xong khi:** tắt FX đi mà vẫn đọc được đòn là chém, đâm hay đập chỉ nhờ dáng người.

### B4. Cụm nút chiến đấu trên điện thoại "thiếu ít nhất 5 nút, icon nhìn không biết làm gì" — ƯU TIÊN 1

**Người dùng nói:** "phần icon gần icon tấn công… 3 nút là bị thiếu, ít là 5 cộng nút kể cả nút tấn công vào… thuần là các icon nhìn vào chẳng biết làm cái gì".

**Hiện trạng** (`MythHudController.BuildActions`, ảnh `.agent_scratch/uiprobe_panels/hud.png`):
- Ngoài đợt quái chỉ thấy 4 nút:
  - nút lớn (vừa tương tác vừa đánh);
  - lướt né [Ctrl];
  - Thế Công [Z];
  - tù và [G].
- Thế Thủ, Thế Biến, Phản kích chỉ hiện khi có đợt quái; Song Thế chỉ hiện khi có Mệnh Ấn.
- Thế Công và Thế Biến dùng chung icon `hoc_vo`, nên người chơi không phân biệt được.
- Icon là biểu tượng chung chung, không phải chiêu thật đang lắp.

**Yêu cầu (bố cục kiểu game hành động di động):**
- **Nút TẤN CÔNG riêng, to nhất,** góc phải dưới, icon là vũ khí đang cầm (tay không thì nắm đấm).
  - Tách khỏi nút TƯƠNG TÁC.
  - Nút tương tác đổi icon theo vật đang đứng gần (máng → bao cám, giếng → xô, cửa → cửa, heo → mắt), giống Stardew.
- **Quanh nút tấn công, luôn hiện** (mờ đi khi chưa dùng được, không biến mất):
  - Thế Công, Thế Thủ, Thế Biến: icon là chiêu đang lắp trong ô đó, kèm vòng hồi chiêu;
  - Lướt né;
  - Thủ / đỡ đòn (giữ);
  - Phản kích.
  - Tổng cộng ít nhất 6 nút quanh nút tấn công.
- **Song Thế:** nút thứ 7, chỉ hiện khi có Mệnh Ấn, có hiệu ứng nổi bật khi sẵn sàng.
- **Nút ngoài chiến đấu tách sang cụm khác** (tù và, quan sát), không trộn với cụm đánh.
- **Mỗi nút:**
  - vùng chạm ít nhất 48×48 dp, khoảng cách ít nhất 8 dp;
  - icon phải tự nói được việc nó làm;
  - lần đầu chạm hiện nhãn tiếng Việt 1 giây (đã có `VnStoryDialoguePopup` hint).

**API Claude cung cấp sẵn** (Core, gọi từ UI):
- `MartialProgression.Slotted(character, CombatSlotType.TheCong|TheThu|TheBien|TheSongThe)` → `MartialSkillDefinition` (tên, id để chọn icon) hoặc `null` (ô trống, nút mờ kèm khoá);
- `MartialProgression.CooldownRemaining01(character, slot, now)` → 0..1 cho vòng hồi chiêu;
- `MartialProgression.Weapon(character)` → vũ khí đang cầm, dùng cho icon nút tấn công (`null` là tay không);
- `PlayerCombatController.Instance.ExecuteDodge()`, `.PressCounter()`; lệnh đánh thường và giữ thủ gọi như nút hiện tại.

Claude sẽ thêm hàm nào còn thiếu nếu Argon báo tên.

### B5. Bảng nhân vật và trang bị: "icon chỗ trang bị đồ làm tệ quá" — ƯU TIÊN 2

**Lỗi thấy trong ảnh `.agent_scratch/uiprobe_panels/equipment.png`:**
1. Thông báo nổi (toast) đè lên dòng tiêu đề của bảng. Toast phải nằm dưới tiêu đề hoặc ở đáy màn hình khi có bảng đang mở.
2. Ký tự `□` hiện thay cho `◀` và các dấu đặc biệt. Font không có glyph, nên chỉ dùng ký tự có trong font hoặc dùng icon.
3. Chữ "…iểu diễn chiêu" bị cắt; nút "Hướng: ◀ Tây" đè lên ô Giày.
4. Chữ kỹ thuật lộ ra cho người chơi: "BÚP BÊ 2.5D", "Chu kỳ chiến đấu 2.5D", "24 FPS", "Icon-First". Thay bằng lời người chơi, ví dụ "Trang bị của An", "Xem thử chiêu".
5. Trong túi trang bị, 16 ô trống đều vẽ cùng một hình giày mờ, nên tưởng là có 16 đôi giày. Ô trống phải trống thật (khung gỗ rỗng); chỉ ô có đồ mới có icon.
6. **Icon trang bị:**
   - mỗi món phải có icon riêng, nhìn ra đúng vật (Dao Rựa khác Giáo Móc khác Búa Tạ khác Nỏ Tay; áo tơi khác giáp lưới);
   - bậc phẩm chất I–V thể hiện bằng màu viền khung, không phải chữ;
   - hiện có nhiều món dùng chung một hình;
   - cần đủ 16 món GDD × hình cầm tay.
7. Ô paper-doll 6 ô:
   - ô đã mặc hiện icon món đang mặc;
   - ô trống hiện bóng mờ của loại ô (dây chuyền, vòng tay…) cùng chữ "Trống".
8. Nút lọc ô thứ 6 (Vòng Tay) bị `SlotCard/Button` đè, bấm không được.

### B6. Bản đồ và bố trí công trình "lộn xộn" — ƯU TIÊN 2

**Người dùng nói:** "bố trí các công trình lộn xộn".

**Yêu cầu khi sửa `farm_layout.json`** (và village/town):
- Nhà chính, kho, xưởng rèn, chuồng gom thành một **khu sân trại** có lối đất nối cửa các nhà. Không rải rác giữa cỏ.
- Mỗi công trình có mặt tiền và cửa quay về lối đi.
- Khoảng trống trước cửa ít nhất 3 ô.
- Đồng cỏ thả heo, khu phòng thủ (rào, tháp), ao/suối, rừng rìa là các vùng tách rõ, có ranh giới tự nhiên (lối đi, hàng rào, hàng cây).
- Không đặt đá hoặc di tích lẻ loi giữa đồng (ảnh HUD: cột đá, khối đá giữa cỏ không có lý do). Đọc AE-0001/AE-0002 trước.
- Giữ các ràng buộc cũ: một ao, sông chứ không phải biển, cầu phía trên, đài thiên văn mái xanh viền vàng là mốc đã duyệt.
- Sau khi dời công trình, giữ `role` và `interact.id` như cũ. Builder tự tính lại collider và cửa.
- Collider của layout: để `w` bằng bề ngang chân tường; `h`/`oy` không cần tính tay nữa (sửa A2).

### B7. NPC — ƯU TIÊN 2

**Người dùng nói:** NPC tệ.

**Yêu cầu:**
- Sheet đi và đứng của NPC theo cùng chuẩn chân ở mục B2.
- Có hành động theo nghề: Bà Sáu nấu, thợ rèn đập búa, lính gác đi tuần.
- Lịch hằng ngày là phần của Claude: NPC đi tới chỗ làm buổi sáng, về nhà buổi tối. Argon cần cho biết mỗi NPC đứng ở đâu trong layout (`npcs` với `x`, `y` và nơi làm việc).
- Gặp NPC thì thấy chân dung và một câu chào, không mở bảng chữ dài.

### B8. Giao diện vỏ Flutter (đăng nhập, lưu mây) "xấu và rất tệ" — ƯU TIÊN 2

**Hiện trạng `flutter_client/lib/unity_game_host.dart`:**
- màn đăng nhập Material mặc định (ô Email, ô Mật khẩu, icon Material);
- chip "đã lưu mây" dùng `Icons.cloud_done`;
- lệch hẳn phong cách game.

**Yêu cầu:**
- Màn đăng nhập/đăng ký, nút "Chơi không cần tài khoản", trạng thái lưu mây đều dùng khung gỗ/đồng và font của game, nền là ảnh nông trại.
- Chip lưu mây nhỏ, đặt ở góc không đè HUD game, có trạng thái: đang lưu, đã lưu, lỗi mạng.
- **Phân việc:** Argon làm giao diện Flutter (widget, theme, ảnh); Claude giữ logic Supabase (`lib/cloud/*`). Không đổi chữ ký hàm trong `save_sync.dart` và `supabase_api.dart`.

### B9. Các nút đang "chết" (bấm không có phản hồi) — ƯU TIÊN 2

Theo probe tự động ngày 4/11 (`.agent_scratch/uiprobe.md`):
- **Bản đồ thế giới:** "Chọn / Đi", tên các khu, "Cổng Thị Trấn Chợ →", "← Cổng Rừng Nguyên Sinh", "Tháp #1", "An (Bạn)" bấm không làm gì. MapMarker chồng lên nhau.
  - Bấm khu phải hiện thông tin khu và nút "Đi" (gọi `PixelWorldTravelController.TravelToLocation(locationId)`).
  - Khu chưa đi được thì nút mờ, kèm lý do.
- **Túi:** 5 nút "I" không làm gì.
- **Tổng quan trại:** các tiêu đề "NHIỆM VỤ ĐANG MỞ", "TRANG SỔ TAY TIẾP", "THỂ TRẠNG HIỆN TẠI", "TRANG BỊ ĐANG MẶC" là nút nhưng không làm gì. Hoặc bỏ dạng nút, hoặc mở đúng bảng (Sổ tay, Nhân vật).

### B10. Các lỗi khác thấy khi chơi — ƯU TIÊN 3

- **Ban đêm quá tối:** HUD ảnh 02:27 gần như không thấy cỏ, rào, heo. Stardew vẫn đọc được ban đêm: phủ xanh tím nhẹ, đèn lồng, cửa sổ sáng.
- **Toast dài 2 dòng che giữa đỉnh màn hình** ("Lệnh Lùa Đàn về Nơi Trú: 12/12 heo…"):
  - rút còn 1 dòng ngắn kèm icon;
  - đặt ở chỗ không che tiêu đề bảng.
- **Nút H/P/N/C trên cùng:** 3 nút dùng chung icon ngôi sao vàng, phải có icon riêng (nhà, xây dựng, sổ tay, nhân vật).
- **Nút tròn đen "2/3" giữa phải màn hình:** không rõ là gì, cần icon và chú thích.
- **Thanh máu/thể lực:** chữ "148/148", "120/126" đè lên thanh; số bị cắt mép.

---

## Phần C — Thứ tự giao và cách Claude ghép

1. **Đợt 1 (chặn người chơi):**
   - B4: cụm nút chiến đấu;
   - B5 mục 1–5: bảng trang bị;
   - B10: đêm tối, toast.
2. **Đợt 2:**
   - B2: dáng đi An/Khoa;
   - B3: 3 đòn slash cùng tách FX;
   - B1: `Interior_NhaChinh`.
3. **Đợt 3:**
   - B1: 2 nội thất còn lại;
   - B6: bố trí map;
   - B7: NPC;
   - B8: Flutter;
   - B9: nút chết còn lại.

**Mỗi đợt, Claude ghép theo đúng quy trình `Docs/QUY_TRINH_MOT_NHANH.md`:**
1. chồng file;
2. `quick_compile.sh` phải OK;
3. `run_tests.sh` phải xanh hết;
4. `ui_probe.sh` không được có COVERED hay DEAD mới;
5. build bản tăng dần (Android export và Gradle chỉ build lại phần đổi, Windows build song song);
6. gửi ảnh thật lại cho Argon.

Có lỗi compile, Claude sửa phần gọi API; không sửa phần nhìn.

**Ảnh và tài liệu tham chiếu trong repo:**
- `.agent_scratch/uiprobe_panels/*.png`: ảnh thật từng bảng;
- `.agent_scratch/uiprobe.md`: kết quả bấm thử 155 nút;
- `.agent_scratch/an_check.png`, `.agent_scratch/khoa_check.png`: sheet đi và đánh hiện tại;
- `.agent_scratch/collider_check.png`: collider cũ (khung đỏ) so với hình nhà;
- `Docs/research/2026-11-04-tieu-chuan-game-pixel-du-tot.md`: 47 tiêu chí "game pixel đủ tốt";
- `Docs/art-direction/anti-examples/AE-0001…`, `AE-0002…`: những thứ đã bị bác, không làm lại.
