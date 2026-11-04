# Báo Cáo Hoàn Thành & Nghiệm Thu: Bản Chơi Thật 3D (`PigTycoon_CareDemo3D.exe`) Trên Bản Đồ Gốc Quy Mô Đầy Đủ (`OriginalMap_Farm_Main`)

## 1. Tóm Tắt Kết Quả Thực Thi

- **Bản chạy độc lập (Standalone Windows 64-bit)**: [`Builds/CareDemo3D/PigTycoon_CareDemo3D.exe`](file:///D:/Pig-tycoon/Builds/CareDemo3D/PigTycoon_CareDemo3D.exe)
- **Scene Unity chính**: [`Assets/Scenes/CarePlayableDemo3D.unity`](file:///D:/Pig-tycoon/Assets/Scenes/CarePlayableDemo3D.unity)
- **Mô hình bản đồ gốc 3D**: [`Assets/Art3D/Environment/OriginalMap/OriginalMap_Farm_Main.fbx`](file:///D:/Pig-tycoon/Assets/Art3D/Environment/OriginalMap/OriginalMap_Farm_Main.fbx) xuất trực tiếp từ [`ArtSource/Blender_3D_Map/PigTycoon_WorldMap_Master.blend`](file:///D:/Pig-tycoon/ArtSource/Blender_3D_Map/PigTycoon_WorldMap_Master.blend) qua [`Tools/export_original_master_map_fbx.py`](file:///D:/Pig-tycoon/Tools/export_original_master_map_fbx.py) (`653.900` đỉnh, `1.106.072` tam giác, `64` cụm mesh, `146` vật liệu, giữ nguyên địa hình núi non, thác nước, sông, cầu đá, lâu đài, thị trấn cảng và đài thiên văn mái xanh/viền vàng).
- **Kiểm định kỹ thuật & tự động**:
  - Core .NET Test Suite ([`Tests/PigTycoon.Runner.csproj`](file:///D:/Pig-tycoon/Tests/PigTycoon.Runner.csproj)): **308/308 PASS**
  - Báo cáo nghiệm thu thực thi độc lập ([`verification_report.json`](file:///D:/Pig-tycoon/.collab/artifacts/original-map-unity-20261026/unity_verification_v2/verification_report.json)): **`allPassed: true` (12/12 ảnh chụp tự động từ GPU trong quá trình chạy `.exe`)**

---

## 2. Danh Sách 12 Ảnh Chụp Nghiệm Thu Thực Tế Từ Bản Build `.exe`

1. **Toàn cảnh Tây-Nam (Overview Southwest)**: [`01_overview_southwest_unity.png`](file:///D:/Pig-tycoon/.collab/artifacts/original-map-unity-20261026/unity_verification_v2/01_overview_southwest_unity.png)
2. **Toàn cảnh Đông-Nam (Overview Southeast)**: [`02_overview_southeast_unity.png`](file:///D:/Pig-tycoon/.collab/artifacts/original-map-unity-20261026/unity_verification_v2/02_overview_southeast_unity.png)
3. **Toàn cảnh Đông-Bắc (Overview Northeast)**: [`03_overview_northeast_unity.png`](file:///D:/Pig-tycoon/.collab/artifacts/original-map-unity-20261026/unity_verification_v2/03_overview_northeast_unity.png)
4. **Toàn cảnh Tây-Bắc (Overview Northwest)**: [`04_overview_northwest_unity.png`](file:///D:/Pig-tycoon/.collab/artifacts/original-map-unity-20261026/unity_verification_v2/04_overview_northwest_unity.png)
5. **Góc nhìn vuông góc từ trên cao (Top-Down Orthographic)**: [`05_top_ortho_unity.png`](file:///D:/Pig-tycoon/.collab/artifacts/original-map-unity-20261026/unity_verification_v2/05_top_ortho_unity.png)
6. **Cận cảnh khu chuồng trung tâm, Khoa, An & đàn heo**: [`06_farm_pen_closeup_unity.png`](file:///D:/Pig-tycoon/.collab/artifacts/original-map-unity-20261026/unity_verification_v2/06_farm_pen_closeup_unity.png)
7. **Cận cảnh Nhà Chính, Phòng Nghiên Cứu, Kho & Trạm Thú Y**: [`07_mainhouse_and_lab_unity.png`](file:///D:/Pig-tycoon/.collab/artifacts/original-map-unity-20261026/unity_verification_v2/07_mainhouse_and_lab_unity.png)
8. **Cận cảnh Đài Thiên Văn (mái xanh/viền vàng), Cầu Đá & Sông**: [`08_observatory_and_bridge_unity.png`](file:///D:/Pig-tycoon/.collab/artifacts/original-map-unity-20261026/unity_verification_v2/08_observatory_and_bridge_unity.png)
9. **Cận cảnh Thị Trấn Cảng & Lâu Đài (Harbor Town & Castle)**: [`09_harbor_town_unity.png`](file:///D:/Pig-tycoon/.collab/artifacts/original-map-unity-20261026/unity_verification_v2/09_harbor_town_unity.png)
10. **Giao diện Trung Tâm Điều Hành Công Trình trực tiếp — Tab 3 (Phòng Nghiên Cứu & 4 Kênh Soi Gen)**: [`10_live_building_terminal_ui.png`](file:///D:/Pig-tycoon/.collab/artifacts/original-map-unity-20261026/unity_verification_v2/10_live_building_terminal_ui.png)
11. **Giao diện Trung Tâm Điều Hành Công Trình trực tiếp — Tab 6 (Đài Thiên Văn & Quét Sao Dự Báo Đột Kích)**: [`11_live_observatory_intel_ui.png`](file:///D:/Pig-tycoon/.collab/artifacts/original-map-unity-20261026/unity_verification_v2/11_live_observatory_intel_ui.png)
12. **Thực chiến cận chiến Dao Rựa & Chuyển pha ánh sáng Hoàng Hôn (18:00)**: [`12_live_combat_slash_and_dusk.png`](file:///D:/Pig-tycoon/.collab/artifacts/original-map-unity-20261026/unity_verification_v2/12_live_combat_slash_and_dusk.png)

---

## 3. Hệ Thống Điều Khiển & Công Trình Tương Tác Trong Bản Chơi Thật

| Phím | Chức năng trong game |
| :--- | :--- |
| `W` `A` `S` `D` | Di chuyển nhân vật đang điều khiển trên địa hình 3D |
| `Tab` | Đổi điều khiển tức thời giữa **Khoa** và **An** (nhân vật còn lại tự động đi theo hỗ trợ) |
| `1` / `2` / `3` | Đổi vũ khí của Khoa: `1` Dao Rựa · `2` Đòn Gánh · `3` Chày Giã Cám |
| `J` / `K` / `Space` | `J` Đòn đánh thường (Combo 3 nhịp) · `K` Tuyệt kỹ võ học · `Space` Đỡ đòn / Phản đòn (Parry) |
| `E` | Tương tác ngữ cảnh: Cho heo ăn, tắm heo, thu hoạch, bán heo tại chợ, hoặc **mở ngay bảng điều hành công trình khi đứng gần bất kỳ công trình nào** |
| `G` / `Q` | `G` Lùa heo vào chuồng · `Q` Huýt sáo gọi đàn heo tập hợp |
| `B` | Bật/tắt **Trung Tâm Điều Hành 7 Công Trình & Hệ Thống Trại 3D** (kèm camera cận cảnh từng công trình) |
| `F5` / `F9` | Lưu nhanh (`F5`) / Tải lại trạng thái (`F9`) |
