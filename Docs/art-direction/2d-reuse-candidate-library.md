# Thư viện pixel kế thừa — candidate ngày 30/10/2026

Trạng thái: **đã chuẩn bị để tích hợp có chọn lọc; chưa duyệt art, chưa chứng minh binding gameplay**.

Theo chỉ đạo người dùng đã tạm duyệt thiết kế: dùng lại asset có sẵn, nhất là bản local `F:\StardewValley`, điều chỉnh cho Pig Tycoon; chỉ tạo mới nếu không có asset thích hợp. Đợt này không sinh art mới.

## Nguồn và đường dẫn

- Nguồn đọc: `F:\StardewValley\Content (unpacked)`, giữ nguyên các tệp gốc.
- Candidate Unity: `Assets/Art/PixelRework/Candidates/`, giữ cấu trúc đường dẫn nguồn.
- Bản sao nguyên byte phục vụ so sánh: `Docs/art-direction/2d-reuse-source-sheets/`, nằm ngoài Assets để không import trùng texture.
- Sáu TMX nguyên byte để tham khảo cấu trúc: `Docs/art-direction/2d-reuse-source-maps/` — Farm, Forest, Town, FarmHouse, Mountain, Beach. Đây là tài liệu bố cục, không phải scene Unity đã hoạt động.
- Manifest nguồn và candidate: `Assets/Art/PixelRework/reuse-manifest.json`, gồm SHA-256, kích thước, grid, palette và đường dẫn từng tệp.
- Script tái lập: `tools/stage_pixel_reference_assets.py`.

## Phủ nội dung

| Nhóm | Số sheet | Asset tiêu biểu và mục đích |
|---|---:|---|
| Địa hình | 7 | spring/fall outdoors, đường, flooring, đất canh tác; giữ tile edge và chi tiết có sẵn |
| Thị trấn | 1 | spring_town: phần nhà gỗ/đá và đường; không dùng xe, biển thương hiệu, nhà hiện đại |
| Cảng | 3 | spring_beach, boatTunnelTiles, night market objects |
| Nội thất | 3 | townInterior, farmhouse_tiles, walls_and_floors |
| Rừng | 6 | hang, witch swamp/hut, cây tán lớn và mystic_tree |
| Công trình | 12 | các cấp kho/chăn nuôi, xưởng, cối xay, silo, giếng, chuồng ngựa, obelisk/đồng hồ |
| Heo | 2 | heo trưởng thành và heo non; không tự coi hai sheet là đủ bốn giai đoạn GDD |
| Thú cưng | 2 | chó và mèo; chỉ là nguồn động tác, phải khớp vai trò và catalog game |
| NPC | 4 | Robin/Marlon/Wizard/Dwarf; chưa gắn danh tính Pig Tycoon hoặc hội thoại |
| Quái | 5 | Skeleton, Mummy, Shadow Brute, Bat, Ghost; cần đối chiếu quái trong GDD trước binding |
| Vật phẩm | 2 | springobjects, Objects_2; ID Pig Tycoon phải do catalog gameplay quyết định |
| Trang bị | 5 | weapons, tools, hats, shirts, pants |
| Chế tạo | 1 | Craftables |
| Nội thất rời | 2 | furniture và wizard_furniture |
| Hiệu ứng | 3 | đạn, emote, mưa |

Tổng 58 PNG candidate, khoảng 2,75 MB dữ liệu PNG; 18.268 rectangle từ grid toàn sheet gồm cả ô trống. Con số rectangle không phải lượng nội dung game đã hoàn thiện. Bản sao nguồn khoảng 3,69 MB đã chuyển ra ngoài Assets. Metadata atlas khoảng 10,14 MB; khi import Unity phải theo dõi peak RAM bằng runner dự án.

## Quy ước frame để tích hợp

- Tất cả candidate: `spritePixelsToUnits=32`, Point filter, tắt mipmap, không nén, không đổi kích thước ảnh, không đổi alpha nguồn.
- Tile map: grid 16×16 px. Tại 32 PPU, một tile dài 0,5 đơn vị. Khi đặt vào ô 1 m hiện tại, dùng scale 2 có chủ đích; không tự đổi PPU toàn dự án.
- Tên sprite grid: `<tên_sheet>_r000_c000`, row/column bắt đầu ở góc trái trên; rect Y trong Unity đã đổi theo gốc trái dưới.
- Pig/BabyPig: frame 32×32, sheet 128×160, 4 cột × 5 hàng. Hàng động tác phụ không tự coi là hướng di chuyển. Cần khớp clip và timing với controller thực.
- Robin/Marlon/Wizard: frame 16×32. Dwarf: 16×24. Skeleton/Mummy/Shadow Brute: 16×32; Bat: 16×16; Ghost: 16×24. Số frame và kích thước từng sheet được ghi trong manifest.
- Mũ: grid 20×20. Shirt/pants là sheet phối lớp giữ nguyên, chưa suy diễn layout animation hoặc cắt tùy tiện.
- `night_market_tilesheet_objects.png` cao 312 px, không chia hết 16: giữ sheet nguyên, không làm mất 8 px cuối.
- Công trình và cây lớn giữ toàn sheet/sprite, không chia tile giả làm animation. Vùng cần tách chi tiết phải chọn theo nội dung ảnh và bố cục thực.

## Chỉnh palette đã thực hiện

Các candidate terrain/forest chuyển mảng lá về ngọc/moss; vật liệu nhà/đất giảm bão hòa có chọn lọc, giữ chênh sáng từng pixel. Heo, NPC, trang bị, quái và vật phẩm giữ nguyên màu; không đổ màu đồng loạt lên da hoặc icon ngữ nghĩa. Không đổi silhouette, lưới và alpha. Đây là nghiên cứu palette vật liệu, **không đủ để nhận là bản art thần thoại sử thi mới**.

Đã mở trực tiếp các ảnh: spring/fall outdoors, spring_town, Pig, Barn và ảnh so sánh trước/sau. Ảnh chứng cứ: `2d-reuse-evidence/source-candidate-comparison.png`. Ảnh so sánh ghi rõ crop góc trên (tối đa 320×125 px của từng sheet) phóng nearest 2× để nhìn pixel; các file asset thực vẫn giữ toàn bộ ảnh.

Kết quả nhìn: shading/tile edge nguồn còn nguyên; vật liệu đất/đá ấm giảm độ chói, lá bớt neon. Nhà trong atlas thị trấn vẫn có biển chữ/đồ hiện đại; không đặt những vùng đó vào map cảng thần thoại. Giếng và barn giữ cấu trúc chức năng rõ hơn hình khối placeholder, nhưng chưa tự có ngôn ngữ kiến trúc Đông–Tây riêng. Cần cắt/chỉnh các chi tiết cụ thể trên bản tích hợp và xem ở góc chơi trước khi duyệt.

Đã xem `Assets/Art/Characters/An/An_FullAction_Sheet.png` của B: hình người còn rất đơn giản, ít anatomy/chi tiết. Không thay danh tính/cơ chế An bằng stock NPC để che vấn đề. Các asset An/Khoa ở nhánh C/B phải được so sánh tiếp trước khi chọn phương án phối lớp từ asset tốt hơn.

## Kiểm chứng đã chạy

```
python tools/stage_pixel_reference_assets.py
python tools/stage_pixel_reference_assets.py --verify
```

Kết quả kiểm chứng: 58/58 sheet giữ hash nguồn/bản sao khớp; candidate hash khớp manifest; kích thước và toàn bộ kênh alpha khớp nguồn; importer có 32 PPU, Point, không mipmap/không nén; tên và số sprite grid không trùng hoặc thiếu. Sáu TMX tham khảo khớp hash nguồn. Không chạy Unity trong task này; chưa chứng minh Unity importer đã nhận mọi SpriteRect hoặc candidate đã xuất hiện trong một scene chạy.

Đọc AE-0001 và AE-0002 trước khi chuẩn bị. Không dùng payload/recipe art bị bác bỏ; không thay nguồn đài thiên văn đã duyệt. Cần bằng chứng overview, góc chơi/cận cảnh và sự duyệt hình ảnh của người dùng trước khi promote art.
