# PIG TYCOON
# SỔ TAY DỰ ÁN HỢP NHẤT v3.0 — WBS đầy đủ • Phân công 4 người • Prompt chung cho Agent • Quy chuẩn Unity & Pixel Art • Kiểm thử nghiệm thu

| Hạng mục | Nội dung |
|---|---|
| **Cơ sở thiết kế** | Pig Tycoon – Game Design Document v6.0 (file “cơ chế game Pig.docx”, lập 28/09/2026). Mọi số liệu là baseline v0.1 theo Design Lock. |
| **Tài liệu kế thừa** | PigTycoon_WBS_v2 (bản phân công trước) – bản v3.0 này thay thế và mở rộng. |
| **Repository** | https://github.com/danhnee/Pig-tycoon |
| **Engine** | Unity 6.3 LTS (6000.3.x / 6000.6.x) – URP 2D – Pixel Perfect – C# |
| **Phong cách** | Pixel art top-down 3/4, tham chiếu cảm giác UI/UX của *Momodora: Moonlit Farewell* (không bắt buộc đạt). |
| **Nhóm** | **A** – Tech Lead/Core & QA + Map Khu Lõi Vận Hành (map riêng phía sau trang trại, nối bằng Đoạn Chuyển Map: nhà chính, kho, Phòng Xây Dựng, Phòng Nghiên Cứu, Xưởng Cường Hóa, khu thông tin) + Làng NPC<br>**B** – World/Farm/Pig & Art Lead + Đồng Cỏ Heo Thả Rông (đã có map + nhân vật nữ An)<br>**C** – Combat/Defense/Enemy + Chợ Thị Trấn (nhân vật nam Khoa)<br>**D** – Economy/Info/Event & Data + Rừng Nguyên Sinh và các cảnh sự kiện |
| **Ngày lập / bắt đầu** | 05/10/2026 (Thứ Hai, tuần W1) – Kế hoạch 34 tuần, phát hành 1.0 dự kiến 30/05/2027 |
| **Trạng thái** | Bản chính thức để thực thi. Thay đổi phải qua Issue + ADR, cập nhật số phiên bản tài liệu. |

---

## MỤC LỤC
1. Tổng quan, phạm vi và nguyên tắc
2. Tổ chức nhóm và vai trò
3. Định hướng kiến trúc và cấu trúc repo
4. Quy ước đặt tên thống nhất
5. Quy chuẩn Pixel Art và mục tiêu UI/UX
6. Phân công 4 map chính và ma trận thiết kế asset
7. WBS Master P0 – P6
8. Nhiệm vụ từng người: MUST và “Làm hơn mức đó thì tốt”
9. Lịch 34 tuần và các mốc nghiệm thu
10. Các yếu tố kết nối giữa module (Contracts)
11. Quy tắc làm việc trên GitHub
12. Quy trình dùng Agent kết hợp Unity (Unity tắt khi agent chạy)
13. Prompt chung cho Agent (AGENTS.md) – bắt buộc
14. Code tham chiếu cho Agent
15. Thư viện và công cụ được phép dùng
16. Nguồn tham khảo code, asset và kiến thức cho Agent
17. Kiểm thử và nghiệm thu theo giai đoạn
18. Quản trị dự án chuyên nghiệp
19. Phụ lục

---

## 1. Tổng quan, phạm vi và nguyên tắc

### 1.1. Mục tiêu của tài liệu
Tài liệu này là “nguồn sự thật vận hành” duy nhất của nhóm 4 người khi phát triển Pig Tycoon bằng Unity. Nó trả lời đủ các câu hỏi: ai làm gì (WBS và phân công), làm tới đâu là đạt (MUST) và làm hơn thì tốt (STRETCH), làm bằng công cụ gì (Unity + IDE có agent), đặt file ở đâu và tên gì, cập nhật GitHub thế nào, agent phải tuân thủ prompt nào, và mỗi giai đoạn được nghiệm thu ra sao.

Thứ tự ưu tiên khi có mâu thuẫn:
1. GDD v6.0
2. Sổ thông số chuẩn trong GDD mục 1.5 (được chuyển thành file dữ liệu Balance)
3. Sổ tay này
4. Code hiện có trong repo.

Agent và con người đều không được tự giải quyết mâu thuẫn bằng cách đoán; phải mở Issue.

### 1.2. Tóm tắt cơ chế GDD v6.0 cần hiện thực
Pig Tycoon là game tycoon mô phỏng hệ thống kết hợp thủ thành có nhân vật điều khiển.
- **Triết lý**: “Luật tối thiểu — Tác động chéo tối đa”, ngân sách là van khóa sức mạnh.
- **Vòng lặp lõi**: chăm đàn → tạo dòng tiền → phân bổ ngân sách → thu thập tình báo → nạp trước cho công trình → đợt quái/sự kiện → sửa chữa, phục hồi → lặp lại.
- **Thang thời gian**: 1 ngày game = 16 phút thực, 1 giờ game = 40 giây thực, mọi phép tính “mỗi ngày” chốt lúc 06:00; combat và đợt quái tính bằng giây và dừng đồng hồ ngày.

| Nhóm nội dung (bản 1.0) | Số lượng | Ghi chú theo GDD |
|---|---|---|
| Nhân vật người chơi | 2 | 1 nam (Khoa – C phụ trách), 1 nữ (An – B đã thiết kế). Người chơi chọn 1 để điều khiển. |
| NPC trang trại / chuyên môn | 7 | Không tham chiến; đặc tính chỉ hỗ trợ việc hằng ngày. |
| NPC chiến đấu / lính đánh thuê | 5 | Chỉ phòng thủ, thuê ở chợ đêm. |
| Thú cưng | 7 | 4 hỗ trợ hằng ngày + 3 canh gác. |
| Dòng gen heo | 12 | 6 bậc độ hiếm, mỗi dòng một cơ chế riêng; Xích Mao là gen “Khá xấu” khắc phục được; Hư Thể có tính xấu bị phong ấn. |
| Phái võ / kỹ năng | 4 / 24 | 6 kỹ năng mỗi phái, mỗi kỹ năng một “động từ” riêng. |
| Công trình phòng thủ | 31 | 28 tự động + 3 Bạch Vân kích hoạt thủ công (Cửu Tiêu Lôi Pháo, Thiên Nhãn Vọng Đài, Vạn Linh Hồi Nguyên Trận); cần bản vẽ + Phòng Xây Dựng. |
| Quái / Boss chu kỳ | 14 / 3 | Mặt đất, trên không, huyền bí, môi trường. |
| Thời tiết / Mối đe dọa sức khỏe | 10 / 10 | Sức khỏe chia 4 nhóm, xử lý bằng 5 “động từ”: cách ly, xử lý môi trường, điều trị cá thể, phòng ngừa đàn, nghiên cứu. |
| Vật phẩm phá cooldown ngủ | 5 | 4 bậc; chỉ Quả Mộng không có tác dụng phụ. |
| Mẫu sự kiện huyền bí | 14 | 3 màu (Đỏ/Vàng/Xanh) × 2 hình thức (Cổng / Sương mù). |

Các hệ thống liên quan còn lại: không gian trại (1 m = 1 unit, Ô Xây 2×2 m, Ô Đất 10×10 m, Nơi Trú, Khu Cách Ly, Khu Xử Lý, lưới điện), Tinh thần và Hòa nhập 0–100, Trạng thái Bầy Đàn (≥100 con gắn kết, Sinh Cảnh ≥50), thủ lĩnh/kế vị/Bồi Dưỡng Kế Vị, Sinh Cảnh 5 bậc, Stamina 2 lớp, 6 ô trang bị (mỗi cặp mở 1 ô võ kỹ), Phù Văn/Mệnh Ấn, chợ ngày bằng vàng, chợ đêm trao đổi (chỉ khoảng 18% ô nhận vàng), đấu giá, GTTC, Bí nhân (không nhận vàng), tình báo, Giấy Chuyên Biệt, Cổng Giữa Game và các bất biến thiết kế ở GDD mục 27.

### 1.3. Nguyên tắc không được phá (Design Lock)
1. Không thay đổi luật cấu trúc hoặc số cân bằng vì “agent gợi ý”. Muốn đổi số: mở Issue loại balance, trích mục GDD, cập nhật Sổ thông số chuẩn trước rồi mới sửa code.
2. Chỉ đúng 2 nhân vật người chơi (Khoa, An). A và D thể hiện phần “thiết kế nhân vật” qua NPC, thú cưng, quái, Boss và nhân vật cốt truyện, không tạo nhân vật người chơi thứ ba.
3. Mọi công trình tự động, trừ 3 công trình Bạch Vân kích tay. Trang bị chỉ cho chỉ số thưởng, không tính vào điều kiện học võ.
4. Mọi số liệu là baseline v0.1, chỉnh bằng dữ liệu playtest, không “cảm tính”.

### 1.4. Hiện trạng đầu vào
- B đã thiết kế map heo thả rông và nhân vật nữ An → được tính là đầu vào của P0, việc còn lại là chuẩn hóa theo Art Bible và đưa vào repo.
- C phụ trách thiết kế nhân vật nam Khoa từ đầu.
- Repo cần xong giai đoạn P0 (khung dự án, quy chuẩn, CI, catalog dữ liệu) trước khi chia việc code lớn.

---

## 2. Tổ chức nhóm và vai trò

### 2.1. Vai trò chính, người dự phòng và trách nhiệm review
Nhóm chia việc theo “trục sở hữu”: mỗi hệ thống có đúng một người chịu trách nhiệm cuối cùng, còn art/nội dung được chia chéo để cả 4 người đều trực tiếp thiết kế nhân vật, NPC, thú cưng, heo, công trình và quái. Không ai “chỉ code” hay “chỉ vẽ”.

| Người | Vai trò | Sở hữu hệ thống | Dự phòng | Review chính |
|---|---|---|---|---|
| **A** | Tech Lead / Core Systems & QA | Kiến trúc, thời gian/ngày-đêm, save/load, player core dùng chung, input, inventory, lưới điện và hạ tầng điện, thời tiết (logic), Cấp trại 1–20, NPC framework, debug/telemetry, CI, hiệu năng, build & release. Map Farm_Core + Hub_Village. UI: luồng màn hình chung (menu, chọn nhân vật, save, pause, cài đặt), HUD lõi, kho, ngủ, Cấp trại, nghiên cứu, cường hóa. | D | Review PR của D; duyệt mọi thay đổi kiến trúc/save/schema. |
| **B** | World – Farm – Pig & Art Consistency Lead *(người dùng tài liệu này)* | Map Farm_Main, lưới Ô Xây/Ô Đất, hạ tầng trại, Nơi Trú Đàn, đàn heo (AI thả rông, vòng đời, Tinh thần, Hòa nhập, Bầy Đàn, thủ lĩnh), sinh sản và hậu duệ heo già, xác/Khu Xử Lý, Sinh Cảnh, sức khỏe, thú cưng framework, nhân vật An, Art Bible. UI: thẻ heo, panel đàn, sức chứa điểm nghẽn, lớp phủ phục vụ/nguồn mát. | C | Review PR của A; duyệt mọi thay đổi Art Bible. |
| **C** | Combat – Defense – Enemy Lead | Nhân vật Khoa, combat, Stamina 2 lớp, chỉ số/trang bị/võ kỹ, 24 kỹ năng, Phù Văn/Mệnh Ấn (logic), Nộ Khí/Huyết Tế, bản vẽ + Phòng Xây Dựng (logic), 31 công trình, độ bền/sửa, wave/quái/Boss, báo cáo sau đợt, AI lính đánh thuê. Map Market_Town. UI: trang bị 6 ô, võ kỹ, xây dựng, báo cáo đợt. | A | Review PR của B; duyệt cân bằng combat. |
| **D** | Economy – Information – Event & Data Lead | Data Catalog & Balance, chợ ngày/đêm, hợp đồng giao heo, 8 chủ đề chu kỳ, đấu giá, GTTC, ngân sách, Bí nhân, tình báo, gen (logic định danh/giám định/nghiên cứu), Giấy Chuyên Biệt và gia cường, khung + 14 sự kiện huyền bí, Localization. Map Forest_Primeval. UI: chợ, đấu giá, Bí nhân, tình báo, giấy, giám định, ngân sách, minimap. | B | Review PR của C; duyệt thay đổi dữ liệu/catalog. |

### 2.2. Ma trận RACI theo hệ thống
*R = thực hiện, A = chịu trách nhiệm cuối cùng, C = được hỏi ý kiến, I = được thông báo.*

| Hệ thống | A | B | C | D |
|---|:---:|:---:|:---:|:---:|
| Kiến trúc, Time, Save, Input, CI | A/R | C | C | C |
| Map, Farm grid, Đàn heo, Sinh Cảnh, Sức khỏe | C | A/R | I | C |
| Combat, Kỹ năng, Công trình, Quái/Boss | C | I | A/R | C |
| Chợ, Ngân sách, Tình báo, Bí nhân, Sự kiện | C | C | C | A/R |
| Gen (logic + 12 gene pack art) | R (art) | R (rig + art) | R (art) | A/R |
| Art Bible, Palette, Import preset | C | A/R | C | C |
| UI Kit & UX | R (HUD) | C | R (combat UI) | A/R |
| Hiệu năng, Build, Release | A/R | R | R | R |
| Prompt chung cho agent (AGENTS.md) | A/R | C | C | C |

---

## 3. Định hướng kiến trúc và cấu trúc repo

### 3.1. Lựa chọn: Monorepo + Unity project mô-đun hóa bằng Assembly Definition
- Dự án dùng một repo duy nhất (monorepo) chứa Unity project, tài liệu, nguồn art, công cụ và CI. Bên trong Unity, code được chia thành nhiều assembly (asmdef) theo module.
- Tầng Core thuần C# (không dùng UnityEngine) có thể chạy unit test bằng `dotnet test` khi Unity đang tắt – chìa khóa để agent làm việc mà không cần mở Unity.

### 3.2. Cây thư mục chuẩn
```
Pig-tycoon/ (monorepo)
├── AGENTS.md ............ prompt chung bắt buộc cho mọi agent
├── CLAUDE.md / GEMINI.md / .github/copilot-instructions.md / .cursor/rules/ → chỉ 1 dòng trỏ về AGENTS.md
├── README.md • CHANGELOG.md • THIRD_PARTY_NOTICES.md • .gitignore • .gitattributes • .editorconfig
├── .github/ ............ workflows/, ISSUE_TEMPLATE/, PULL_REQUEST_TEMPLATE.md, CODEOWNERS
├── docs/ ............... gdd/ (GDD v6.0), handbook/ (sổ tay này), adr/, art-bible/, agent/ (task cards), playtest/
├── art-source/ ......... file gốc .aseprite/.psd theo Characters, Npcs, Pets, Pigs, Buildings, Enemies, Tiles, Ui, Vfx (Git LFS)
├── tools/ .............. verify.ps1, verify.sh, lint/Check-Names.ps1, DomainTests/ (dotnet test), sim/ (mô phỏng kinh tế)
└── game/PigTycoon/ ..... Unity project (Assets, Packages, ProjectSettings)
    └── Assets/_Project/
       ├── Art/ ........ Characters, Npcs, Pets, Pigs, Buildings, Enemies, Tiles, Ui, Vfx, Fonts (PNG/ase đã import)
       ├── Audio/ ...... Sfx, Music
       ├── Code/
       │   ├── Core/ ...... PigTycoon.Core (thuần C#, noEngineReferences): Time, Stats, Herd, Genetics, Economy, Combat rules…
       │   ├── Runtime/ ... PigTycoon.<Module>: Bootstrap, Time, Save, Input, Farm, Herd, Health, Player, Combat, Defense, Enemy, Economy, Intel, Events, Npc, Pet, Ui
       │   ├── Editor/ .... công cụ editor, importer preset, validator, CI entry
       │   └── Tests/ ..... EditMode/, PlayMode/
       ├── Data/ ....... Balance/ (Sổ thông số chuẩn .json), Catalog/ (ScriptableObject định nghĩa), Localization/
       ├── Prefabs/ .... theo module giống Code/Runtime
       ├── Scenes/ ..... Boot, Farm_Main (B), Farm_Core (A), Hub_Village (A), Market_Town (C), Forest_Primeval (D), Event_<Color>_<Name>, CombatSandbox, MarketEventSandbox, TestHarness, Sandbox_<Owner>_<Feature>
       └── Settings/ ... URP, Input Actions, Pixel Perfect, Presets
```

### 3.3. Các tầng kiến trúc và luật phụ thuộc
| Tầng | Chứa gì | Được phép phụ thuộc |
|---|---|---|
| **Core** (`PigTycoon.Core`) | Luật game, công thức, state thuần dữ liệu, không MonoBehaviour. | Không phụ thuộc gì ngoài .NET Standard 2.1. Test bằng dotnet và Unity EditMode. |
| **Runtime** (`PigTycoon.<Module>`) | MonoBehaviour, view, adapter đọc Core, service Unity. | Core + module khác chỉ qua interface trong `PigTycoon.Contracts`. |
| **Contracts** (`PigTycoon.Contracts`) | Interface, struct sự kiện, ID dùng chung. | Chỉ Core. A sở hữu; đổi contract phải có ADR. |
| **Ui** (`PigTycoon.Ui`) | UI Toolkit (UXML/USS), presenter. | Contracts; không gọi trực tiếp logic module. |
| **Editor / Tests** | Công cụ, validator, test. | Mọi tầng (chỉ trong Editor). |

### 3.4. Quyền sở hữu scene (tránh conflict YAML)
| Scene | Owner | Quy tắc |
|---|---|---|
| `Boot` + `TestHarness` | A | Khởi tạo service, nạp dữ liệu, SceneLoader nạp scene theo kiểu additive. Người khác đưa tính năng vào qua prefab/service. |
| `Farm_Main` | B | Cảnh mô phỏng chính trong ranh giới trại. Mọi đợt quái, Cổng sự kiện và Bí nhân đều xảy ra ở đây (GDD 3.6, 21, 24). |
| `Farm_Core` | A | Nhà chính, Kho, Phòng Xây Dựng, Phòng Nghiên Cứu, Xưởng Cường Hóa, Lều Quân Y, khu thông tin. Nằm phía sau (Bắc) trại, nối với Farm_Main bằng Đoạn Chuyển Map. Quái vẫn có thể nhắm tới Kho và Nhà chính. |
| `Hub_Village` | A | Điểm chuyển cảnh: nhà NPC, nhà Bà Sáu, cốt truyện. Không có combat. |
| `Market_Town` | C | Chợ ngày, chợ đêm, đấu giá, kiểm dịch/giám định, hội quán thuê lính. Không có combat. |
| `Forest_Primeval` | D | Khám phá, thu thập vật liệu. Không có đợt quái. |
| `Event_<Color>_<Name>` | Theo bảng 6.11 | D sở hữu khung chung (vào/thoát, đếm %, vật phẩm thoát). Mỗi người dựng các cảnh sự kiện được giao. |

---

## 4. Quy ước đặt tên thống nhất

### 4.1. Tên dự án, module, namespace
- Tên sản phẩm: **Pig Tycoon**. Tên kỹ thuật: `PigTycoon`. Repo: `Pig-tycoon`. Bundle ID: `com.pigtycoon.game`.
- Namespace: `PigTycoon.<Module>[.<SubModule>]`. Tên asmdef trùng namespace gốc của module.
- Danh sách module chuẩn: `Bootstrap`, `Time`, `Save`, `Input`, `Farm`, `Herd`, `Genetics`, `Health`, `Weather`, `Player`, `Combat`, `Skills`, `Defense`, `Enemy`, `Economy`, `Market`, `Intel`, `Events`, `Npc`, `Pet`, `Items`, `Ui`, `Audio`, `Narrative`, `Debug`.
- Tên tiếng Việt trong file/ID viết không dấu, PascalCase cho asset và snake_case cho ID dữ liệu.

### 4.2. Bảng tiền tố asset
| Tiền tố | Loại | Ví dụ |
|---|---|---|
| `SPR` | Sprite / sprite sheet | `SPR_Player_An_Walk_S.png` |
| `ASE` | File nguồn Aseprite | `ASE_Pig_LamKhe_Adult.aseprite` |
| `ANIM` / `AC` | Animation clip / Animator controller | `ANIM_Npc_BacSiThuY_Idle_E.anim` • `AC_Pig_Base.controller` |
| `PF` | Prefab | `PF_Building_TramKhiTuong.prefab` |
| `SO` | ScriptableObject dữ liệu | `SO_Gene_XichMao.asset` |
| `TILE` / `TP` | Tile / Tile palette | `TILE_Farm_Grass_01` • `TP_Farm_Main` |
| `UI` / `USS` / `UXML` | Sprite UI / stylesheet / layout | `UI_Icon_Gold.png` • `USS_Hud.uss` • `UXML_MarketNight.uxml` |
| `VFX` / `MAT` / `SHD` | Hiệu ứng / material / shader | `VFX_Combat_HitSpark` • `MAT_Sprite_Lit` |
| `SFX` / `MUS` | Âm thanh / nhạc | `SFX_Pig_Squeal_02.wav` • `MUS_Night_Market.ogg` |

### 4.3. Công thức đặt tên cho file mới
`<Prefix>_<Domain>_<Subject>[_<Variant>][_<State>][_<Dir>][_<NN>].<ext>`
- Chỉ dùng A–Z, a–z, 0–9 và dấu gạch dưới; không dấu cách, không tiếng Việt có dấu, không gạch ngang; tối đa 64 ký tự.
- Không đánh số phiên bản trong tên (cấm `_final`, `_v2`, `_new`).

---

## 5. Quy chuẩn Pixel Art và mục tiêu UI/UX

### 5.1. Thông số kỹ thuật thống nhất
| Thông số | Giá trị chuẩn |
|---|---|
| **Pixels Per Unit** | **16 PPU** (1 unit = 1 m theo GDD) → Ô Xây 2×2 m = 32×32 px; công trình Nhỏ/Vừa/Lớn = 32/64/96 px; Ô Đất 10×10 m = 160×160 px. Khớp khung camera của GDD 3.6. |
| **Góc nhìn** | Top-down 3/4; 4 hướng (N/S/E/W) là MUST, 8 hướng là STRETCH sau khi MUST xong. |
| **Độ phân giải tham chiếu** | Zoom Cận: 640×360 px = 40×22,5 m (×3 lên 1920×1080). Zoom Mặc định: 960×540 px = 60×33,75 m (×2). Toàn cảnh 128×72 m chỉ dùng để quản lý. |
| **Import Preset** | Filter Mode = Point, Compression = None, Mip Map tắt, Pivot = Bottom Center cho thực thể, Sprite Mode Multiple; dùng Preset chung `PRE_Sprite_Pixel16`. |
| **Palette** | Một palette chung 48 màu lưu tại `docs/art-bible/palette.gpl` + `.ase`. |
| **Kích thước canvas** | Nhân vật người chơi/NPC: canvas 32×32 (thân cao 26–30 px ≈ 1,7 m, tỉ lệ chibi kiểu Momodora). Heo trưởng thành 24×20, heo non 12×10, heo đang lớn 18×14, heo già 24×20 + dấu hiệu tuổi. Thú cưng 16–24 px. Quái 24–48 px; Boss 64–128 px. Icon 16×16 và 32×32. |
| **Animation FPS** | 8–12 fps cho đi bộ/idle; 12–16 fps cho đòn đánh; dùng hold frame tạo cảm giác mượt kiểu Momodora. |

### 5.2. Bộ animation tối thiểu (MUST)
- **Nhân vật người chơi (An, Khoa)**: Idle, Walk, Run, Attack (3 đòn combo), Dodge, Interact, Hurt, Death × 4 hướng.
- **NPC**: Idle, Walk, Action (làm nghề) × 4 hướng; NPC chiến đấu thêm Attack, Hurt, Death.
- **Heo**: Idle, Walk, Eat, Flee, Sleep × 4 giai đoạn tuổi; dùng chung rig cơ bản của B, mỗi gen thêm dấu hiệu riêng.
- **Thú cưng**: Idle, Walk, Action, Alert.
- **Quái**: Move, Telegraph (báo trước đòn), Attack, Hit, Death. Boss thêm mỗi pha một Telegraph riêng.
- **Công trình**: Idle, Active (bắn/hoạt động), Damaged, Destroyed, Building (đang xây).

### 5.3. Tham chiếu Momodora: Moonlit Farewell – học gì, không làm gì
- **Học**: Bảng màu đêm trăng xanh tím lạnh + đèn lồng ấm; silhouette chibi rõ nét; cảm giác đánh hit-stop ngắn (40-60ms) + flash shader + rung nhẹ; HUD tối giản; menu dạng lưới thẻ mảnh mai; hộp thoại chân dung 64x64.
- **Không làm**: Không sao chép hay trích xuất sprite/sound/font của Momodora.

---

## 6. Phân công 4 map chính và ma trận thiết kế asset

### 6.1. Tổng quan 4 map
1. **Map A – Khu Lõi Vận Hành (`Farm_Core`) + Làng NPC (`Hub_Village`)** (Owner: A)
   - Kích thước: Lõi 60×40 m (6×4 Ô Đất) phía Bắc trại + Đoạn Chuyển Map ≈ 20×8 m; Làng 40×30 m.
   - 7 phòng chức năng: Nhà chính, Kho, Phòng Xây Dựng, Phòng Nghiên Cứu, Xưởng Cường Hóa, Lều Quân Y, Khu thông tin.
   - Asset: Bà Sáu, 3 NPC, 2 thú cưng, 3 gen, 8 công trình, 3 UFO + Boss Mẫu Hạm UFO, 2 cảnh sự kiện.
2. **Map B – Đồng Cỏ Heo Thả Rông (`Farm_Main`)** (Owner: B - *Người dùng tài liệu này*)
   - Kích thước: Trong rào 120×90 m (12 Ô Đất), mở rộng 220×170 m (~40 Ô Đất) + vành ngoài 30–40 m.
   - Chiến trường duy nhất của đợt quái; nơi chăm đàn.
   - Asset: Nhân vật An, Base Pig Rig, 3 gen (Lam Khê, Xích Mao, Thiết Bì), hạ tầng trại, 3 NPC, 2 thú cưng, 8 công trình, 4 quái, 4 cảnh sự kiện.
3. **Map C – Chợ Thị Trấn (`Market_Town`)** (Owner: C)
   - Kích thước: 50×40 m × 2 lớp ngày/đêm.
   - Asset: Nhân vật Khoa, 3 NPC + 4 NPC chức năng chợ, 1 thú cưng, 3 gen, 8 công trình, 4 quái + Boss Chúa Trộm Rìu Sắt, 4 cảnh sự kiện.
4. **Map D – Rừng Nguyên Sinh (`Forest_Primeval`) + khung sự kiện** (Owner: D)
   - Kích thước: 80×80 m.
   - Asset: Mận, Bí nhân, 3 NPC, 2 thú cưng, 3 gen, 7 công trình, 3 quái + Boss Thực Thể Sương, 4 cảnh sự kiện + khung sự kiện 14 cảnh.

---

## 7. WBS Master P0 – P6
- **P0 – Nền tảng** (W1–W2: 05/10 – 18/10/2026): G0 – 18/10.
- **P1 – Vertical Slice** (W3–W8: 19/10 – 29/11/2026): G1 – 29/11 (chơi 1 ngày game trọn vẹn).
- **P2 – Alpha: Hệ thống lõi** (W9–W16: 30/11/2026 – 24/01/2027): G2 – 24/01 (12 gen, 12 NPC, 7 thú cưng, Bầy Đàn).
- **P3 – Phòng thủ, quái và Boss đầy đủ** (W17–W23: 25/01 – 14/03/2027): G3 – 14/03 (31 công trình, 14 quái + 3 Boss).
- **P4 – Chiều sâu** (W24–W29: 15/03 – 25/04/2027): G4 – 25/04 (Bí nhân, 14 sự kiện, Phù Văn/Mệnh Ấn, Content Complete).
- **P5 – Polish, UI/UX, cân bằng** (W30–W32: 26/04 – 16/05/2027): G5 – 16/05 (Momodora feel, 3 vòng playtest).
- **P6 – Release Candidate & Phát hành 1.0** (W33–W34: 17/05 – 30/05/2027): G6 – 30/05/2027.

---

## 8. Nhiệm vụ từng người (MUST và STRETCH)

### Thành viên B – World, Farm, Pig & Art Lead *(người dùng tài liệu này)*:
- **MUST**:
  - P0.05 (Art Bible + Palette 48 màu + Import Preset 16 PPU), P0.06 (chuẩn hóa map heo thả rông và An).
  - P1.03–P1.06 (di chuyển đàn flow-field, vòng đời heo 4 giai đoạn, lưới Ô Xây/Ô Đất, animation An đầy đủ), P1.12 (Base pig rig).
  - P2.01–P2.04 (Tinh thần, Hòa nhập, Bầy Đàn, thủ lĩnh, sinh sản F1/F2, Sinh Cảnh 5 bậc, 10 mối đe dọa sức khỏe), P2.12 (framework thú cưng).
  - P4.08 (Dị Biến / Hư Thể phong ấn - thức tỉnh).
  - P5.02 (ánh sáng ngày/đêm URP 2D).
  - Map `Farm_Main`: P0.16, P1.18, P2.18, P2.23, P3.14, P3.18, P5.08, P5.11 (Checklist GDD 3.10 nhóm A-H).
  - Asset: An đủ bộ, rig heo, 3 gen (Lam Khê, Xích Mao, Thiết Bì), hạ tầng trại, 3 NPC (Bác Sĩ Thú Y, Nông Công, Vệ Binh Khiên), 2 thú cưng (Chó Chăn Đàn, Chó Ngao Canh), 8 công trình (Hàng Rào gỗ/thép, Cổng Gia Cố, Ụ Bao Cát, Bàn Chông, Đèn Pha Công Nghiệp, Bơm Làm Mát, Chốt Canh Gác, Cờ Tập Kết), 4 quái (Chuột Gặm Kho, Bầy Nhảy, Thú Bão, Kẻ Mang Dịch), 4 cảnh sự kiện (Vườn Mười Hai Quả, Đồng Cỏ Không Bóng, Bí Nhân Mua Già, Đổi Một Lấy Một), tileset, VFX thời tiết, icon sức khỏe/Sinh Cảnh.
  - Review style mọi asset; review PR của A.
- **STRETCH**: P1.16 ánh sáng sớm; Đàn heo 8 hướng + bong bóng cảm xúc; Trang phục An thay theo trang bị; Trailer/GIF P6.03; Mô phỏng đàn bằng Burst/Jobs đạt 300 heo.

---

## 9. Lịch 34 tuần và nhịp làm việc
- Hai hạn cập nhật GitHub bắt buộc mỗi tuần: **Thứ Tư 23:59** và **Chủ Nhật 23:59**.
- Review tuần & Demo: Chủ Nhật 20:00 (45 phút).
- Điều kiện tính cập nhật hợp lệ: Hoàn thành ít nhất 1 work item / checkpoint WBS (có ID), demo được trong Unity, CI xanh, có bằng chứng (ảnh/GIF/log test).

---

## 10. Các yếu tố kết nối giữa module (Contracts)
Giao tiếp giữa các module chỉ qua interface trong `PigTycoon.Contracts` hoặc Event Bus:
- `ITimeService`: Đọc ngày, giờ, buổi, Pause/Resume khi combat.
- `ISaveService` / `ISaveable`: Lưu/tải state theo khóa có phiên bản.
- `IDataCatalog` / `IBalance`: Tra cứu định nghĩa ID và hằng số cân bằng.
- `IHerdQuery`: B cung cấp số heo, mật độ, Áp Lực Sự Kiện, Bầy Đàn cho C và D.
- `IHealthQuery`: B cung cấp bệnh dịch, Sinh Cảnh cho D.
- `IEconomyLedger`: D cung cấp TrySpend/Earn vàng cho A, B, C.
- `IWaveSchedule`: C cung cấp lịch quái; D làm mờ theo tình báo.
- `IIntelService`: D cung cấp dự báo đợt, thời tiết, giá chợ.
- `IPowerGrid`: A cung cấp trạng thái cấp điện.
- `IPlayerStats`: C cung cấp chỉ số, Stamina, trang bị cho A và D.
- `IEventBus`: Sự kiện struct (PigBorn, PigDied, PigSold, DayStarted, WeatherChanged, WaveStarted, WaveEnded...).

---

## 11. Quy tắc làm việc trên GitHub
- Nhánh `main`: Chỉ chứa bản qua cổng nghiệm thu (G0...G6), tag `v0.x.y`.
- Nhánh `develop`: Nhánh tích hợp chính.
- Nhánh làm việc: `feat/<A|B|C|D>/<WBS-ID>-<slug>`.
- Commit format: `[WBS-ID][Người] mô tả ngắn`. Commit có agent hỗ trợ thêm dòng `Agent-Assisted: <công cụ>`.
- Rebase lên develop trước khi mở PR. PR không quá 400 dòng code thay đổi.
- Review chéo theo vòng: A → B, B → C, C → D, D → A.

---

## 12. Quy trình dùng Agent kết hợp Unity
- **Nguyên tắc cốt tử**: Khi Agent đang chạy thì **Unity Editor TẮT**.
- Agent chỉ sửa file văn bản (`.cs`, `.asmdef`, `.json`, `.uss`, `.uxml`, `.md`, `.ps1`, `.sh`).
- Agent kiểm tra code bằng `dotnet test tools/DomainTests/DomainTests.csproj` (chạy ngoài Unity chỉ mất vài giây).
- Khi agent xong, người mở Unity (chế độ Human check) để import, kiểm tra Console 0 lỗi, chơi thử, gắn prefab/scene và chụp bằng chứng.

---

## 13. Prompt chung cho Agent (AGENTS.md)
Nội dung chi tiết xem tại `AGENTS.md` ở thư mục gốc repo.
Gồm 12 quy tắc cứng (R1 - R12), quy trình 7 bước (B1 - B7), bảng Self-Review và mẫu báo cáo cuối phiên.

---

## 14. Code tham chiếu cho Agent
Bao gồm mẫu asmdef `PigTycoon.Core.asmdef` (noEngineReferences: true), `GameClock.cs`, `StatCalc.cs` và test NUnit, `GeneDef.cs` ScriptableObject, `DomainTests.csproj`, và regex kiểm tra tên asset.

---

## 15. Thư viện và công cụ được phép dùng
- Gói chính thức: URP 2D, 2D Pixel Perfect, 2D Tilemap Editor/Extras, 2D Aseprite Importer, Input System, Cinemachine 3, UI Toolkit, Localization, Addressables, Burst/Collections/Mathematics/Jobs, Unity Test Framework, Newtonsoft Json, Recorder.
- Thư viện mở: UniTask (MIT), PrimeTween (MIT), VContainer (MIT), NavMeshPlus (MIT), ink (MIT), Graphy (MIT), NaughtyAttributes (MIT).

---

## 16. Nguồn tham khảo code, asset và kiến thức
Tài liệu chính thức Unity, Happy Harvest, Red Blob Games (flow-field), Game Programming Patterns, Lospec.

---

## 17. Kiểm thử và nghiệm thu
- Tháp kiểm thử: Unit test Core (dotnet test) → Integration test → Simulation test (headless 30-60 ngày) → Performance test (150 heo + 30 quái + 4 NPC ≥ 60 FPS) → Manual playtest → Art review.
- Các bất biến thiết kế phải có test tự động (mục 17.4).
- Kiểm soát chống "pass cho có" (mục 17.5): Test phải đỏ trước, kiểm tra đột biến, không hard-code kết quả, self-review bắt buộc.

---

## 18. Quản trị dự án & 19. Phụ lục
- Quản lý rủi ro, phân loại lỗi, semantic versioning, ADR.
- Checklist tuần khởi động W1 (05/10 - 18/10/2026).
