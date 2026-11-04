# Tiêu chuẩn "game pixel đủ tốt" theo người chơi: nghiên cứu và checklist nghiệm thu

- Ngày: 2026-11-04
- Mục đích: định nghĩa ngưỡng chất lượng mà người chơi (không phải dev) dùng để đánh giá một game pixel 2D top-down 3/4 thể loại farm-sim, hành động/phòng thủ nhẹ và có cốt truyện bí ẩn, rồi chuyển ngưỡng đó thành tiêu chí có thể kiểm tra cho Pig Tycoon.
- Bối cảnh: người dùng chê bản build hiện tại là "chưa hoàn thiện": cốt truyện dồn vào cutscene dài, hành động được giải thích bằng popup chữ, cụm nút khó hiểu, HUD rối.
- Quy ước:
  - **[Nguồn]**: có trích dẫn kèm link.
  - **[Suy luận]**: kết luận của người viết, không có nguồn trực tiếp.
  - **[Chưa xác minh]**: chi tiết đáng tin nhưng chưa tìm được nguồn chính thức.
  - Trang stardewvalleywiki.com trả lỗi 403 khi tải trực tiếp, nên các chi tiết từ wiki được lấy qua trích đoạn của công cụ tìm kiếm. Đoạn trích có thể bị rút gọn, nên đối chiếu lại trang wiki trước khi dùng làm số liệu thiết kế.
  - Báo cáo này không thay đổi số cân bằng nào của GDD. Mọi con số dưới đây là tham chiếu ngoài ngành, không phải giá trị cho Pig Tycoon.

---

## 0. Tóm tắt điều hành

Người chơi thường không nói "game pixel này đẹp" hay "xấu". Họ khen hoặc chê ở bốn tầng, xếp theo mức gây bỏ game:

1. **Có được điều khiển sớm không.** Họ muốn cầm điều khiển ngay, không phải đọc. Thời lượng phiên đầu và việc chạm tới vòng lặp lõi tương quan mạnh với tỉ lệ quay lại.
2. **Cảm giác hành động (game feel).** Mỗi lần bấm nút phải có phản hồi bằng hình, tiếng và nhịp. Chữ chỉ là phụ.
3. **Đọc được màn hình.** HUD tối thiểu, icon đi trước chữ, pixel cùng một tỉ lệ, chữ sắc nét.
4. **Nhịp truyện.** Truyện được rải qua nhân vật, ngày và địa điểm theo từng mẩu ngắn. Không dồn thành khối thoại dài.

Các game được khen (Stardew Valley, Celeste, Dead Cells, Chained Echoes, Fields of Mistria) đều "tôn trọng thời gian người chơi". Các game bị chê (Eastward, phần đầu Sea of Stars, CrossCode, Graveyard Keeper, Sun Haven) bị phàn nàn đúng ở những chỗ đội Pig Tycoon đang gặp: thoại dài, tutorial chữ, hệ thống rối, HUD hoặc menu quá tải.

---

## 1. Mười phút đầu và onboarding

### Bằng chứng

- **Rơi rụng diễn ra rất sớm.** Dữ liệu Google Play cho thấy nhóm game kém nhất mất 46% lượt cài mới ở phút thứ 5, và hơn nửa không chơi tới phút 10. Nhóm tốt nhất chỉ mất 17% ở phút 5 và 24% ở phút 10 ([Google Play Apps & Games, Medium](https://medium.com/googleplaydev/why-the-first-ten-minutes-is-crucial-if-you-want-to-keep-players-coming-back-to-your-mobile-game-4a89031b6308)).
  - Theo deltaDNA (hơn 5.000 game), phần lớn game mất khoảng 20% lượt cài trong 2 phút đầu. Phiên đầu trung bình dài 9 phút. Game có phiên đầu dài hơn mức đó đạt khoảng 31% D1, game ngắn hơn chỉ khoảng 20% D1 ([Game Developer](https://www.gamedeveloper.com/business/how-first-session-length-impacts-game-performance)).
  - Đây là dữ liệu mobile/F2P. Áp sang PC indie là **[Suy luận]**.
- **Thời gian tới lần nhập đầu tiên (time-to-first-input).** Một bài phân tích indie khuyên đưa time-to-first-input xuống dưới 60 giây, cắt intro, bỏ logo và cho phép bỏ qua mọi thứ ([Block Bastards, Medium](https://medium.com/block-bastards/your-indie-game-isnt-failing-its-first-10-minutes-are-187d4aeac6b3)). Bài không đưa số liệu Steam cụ thể, nên chỉ xem như ý kiến.
- **Độ dài cảnh mở đầu.** Một devlog trên itch.io ghi nhận Final Fantasy thời SNES có cảnh mở đầu khoảng 5–6 phút, còn nhiều RPG indie nghiêng về kiểu visual novel với phần mở đầu kịch bản dày đặc 7–15 phút ([itch.io devlog](https://itch.io/devlog/677410/intro-cutscenes-how-long.amp)).
- **Stardew Valley là mẫu chuẩn của thể loại.**
  - Sau phần tạo nhân vật chỉ có một intro ngắn gặp Robin và Thị trưởng Lewis. Ngay sau đó người chơi đứng trong nhà lúc 6 giờ sáng, đã điều khiển được. Trong nhà có gói 15 hạt Parsnip, và nhật ký mở sẵn nhiệm vụ "gặp mọi người" và "thu hoạch Parsnip" ([TheGamer](https://www.thegamer.com/stardew-valley-getting-started-guide/)).
  - Gần như không có tutorial. Người chơi tự học bằng cách thử từng dụng cụ lên đá, gỗ và cỏ ([Neoseeker](https://www.neoseeker.com/stardew-valley/walkthrough/Year_1:_Spring-Day_1)).
  - Lớp hướng dẫn phụ đi qua TV ("Livin' Off The Land") và nhiệm vụ. Community Center chỉ mở vào ngày 5.
- **Celeste dạy bằng thiết kế màn.** Game chỉ hiện đúng hai lời nhắc điều khiển (leo và dash). Phần còn lại người chơi học bằng cách kết hợp hai lệnh đó qua thiết kế màn ([TheGamer](https://www.thegamer.com/clever-video-game-tutorials-didnt-feel-like-tutorials-hidden-blended/)).
- **Popup chữ bị xem là cách dạy dở nhất.** Đây là cách dễ làm nhất nhưng tệ nhất vì kéo người chơi ra khỏi trải nghiệm ([GameDev Academy](https://gamedevacademy.org/game-tutorials-guide/)). Thay vì hiện "Press Shift to Dash", hãy đặt một cây cột đổ buộc người chơi phải dash, như vậy giảm tải nhận thức vì không phải dịch chữ thành hành động ([OneManMMO](https://onemanmmo.com/the-best-tutorial-is-the-one-you-do-not-notice/)).
- **Phản ví dụ.**
  - CrossCode: người chơi than sau khoảng 2 giờ thấy "giống visual novel hơn ARPG" và gặp nhiều tutorial hơn bất kỳ game nào khác. RPGFan cũng ghi nhận "nửa đầu game nhịp chậm" ([RPGFan](https://www.rpgfan.com/review/crosscode/)).
  - Sea of Stars: người chơi phàn nàn game mở đầu bằng khoảng 10 phút "bấm A để qua hộp thoại" ([Grouvee](https://www.grouvee.com/user/80619-CatBut/reviews/3410809/)). Nintendo Life vẫn khen game nhưng thừa nhận "khởi đầu hơi chậm" ([Nintendo Life](https://www.nintendolife.com/reviews/switch-eshop/sea-of-stars)).

### Tiêu chí có thể kiểm tra

- Từ lúc bấm "New Game" tới lúc nhân vật di chuyển được: ≤ 60 giây với cảnh mở đầu bắt buộc, không tính màn tạo nhân vật **[Suy luận từ các nguồn trên]**.
- Cảnh mở đầu bắt buộc ≤ 2 phút, ≤ 2 nhân vật có thoại, và bỏ qua được.
- Trong 3 phút đầu người chơi đã dùng ít nhất 1 dụng cụ lên thế giới và thấy kết quả (đất xới, cỏ đổ, vật phẩm rơi).
- Trong 10 phút đầu người chơi đã hoàn thành ít nhất 1 vòng lặp lõi thu nhỏ: trồng, tưới, (tua hoặc ngủ), thu hoạch hoặc bán. Hoặc một trận chạm trán nhỏ có phần thưởng.
- Không có popup chữ nào chặn input quá 1 lần cho mỗi khái niệm. Mỗi khái niệm mới được dạy bằng một tình huống buộc phải làm.
- Hệ thống thứ hai trở đi (chế tạo, chợ, phòng thủ, thuê NPC) mở dần qua ngày hoặc sự kiện, không mở hết vào ngày 1. Stardew mở Community Center vào ngày 5. Sun Haven bị chê "ném 10 hệ thống vào mặt trong vài giờ đầu" (xem mục 9).

---

## 2. Phản hồi tức thời và "game feel": hành động tự nói, không cần chữ

### 2.1 Nguyên tắc nền (có nguồn)

- **Juice.**
  - Talk GDC 2012 "Juice it or Lose it" (Jonasson & Purho) biến một bản Breakout nhạt nhẽo thành game "đã tay" mà không đổi luật chơi. Họ chỉ thêm tween, co giãn, âm thanh, hạt (particle), rung màn hình và vài chi tiết cá tính. Bản dựng lại có thể chơi được ở [longwelwind.net](https://longwelwind.net/blog/juice-it/).
  - "The Art of Screenshake" (Vlambeer, 2013) liệt kê khoảng 30 thủ thuật, gồm: hiệu ứng va chạm ("cho người chơi biết đã trúng, đừng chỉ xóa viên đạn"), animation bị đánh, đẩy lùi kẻ địch ([artificials.ch](http://artificials.ch/juice-up-your-game/)). Thêm vào đó là "sleep/hit-pause": dừng 1–vài frame khi kẻ địch chết hoặc người chơi trúng đòn, đủ ngắn để người chơi không nhận ra là bị dừng ([bluetengu](https://www.bluetengu.com/2014/12/12/art-of-screenshake-experiments/)).
- **Hit-stop.** Sakurai (Smash Bros.) dành video đầu tiên của kênh thiết kế cho hit-stop: dừng hình ngắn ở khoảnh khắc va chạm để đòn có sức nặng, và đặc biệt nên dùng cho đòn kết liễu ("boss stop") ([YouTube: Stop for Big Moments!](https://www.youtube.com/watch?v=OdVkEOzdCPw)). Một số bài tổng hợp nêu mức khoảng 0,05 giây cho đòn lớn và cảnh báo để lâu sẽ làm combat lì **[Nguồn thứ cấp]**.
- **Chuẩn bị, ra đòn, thu đòn (anticipation, action, recovery).** Jonathan Cooper (*Game Anim*) viết:
  - Anticipation của nhân vật người chơi luôn phải tối thiểu để hành động gần như tức thời. Thiếu anticipation thì đòn mất trọng lượng, dài quá thì nhân vật lì.
  - Follow-through cũng không được giữ quyền điều khiển quá lâu. Engine nên cho phép hủy phần cuối animation.
  - Anticipation của NPC thường dài hơn để người chơi kịp phản ứng ([LinkedIn: 12 Principles](https://www.linkedin.com/pulse/12-principles-animation-video-games-jonathan-cooper); [gameanim.com](https://www.gameanim.com/2005/06/11/player-control-fast-vs-slow/)).
- **Dead Cells.**
  - Sébastien Bénard: đòn đánh làm kẻ địch nháy sáng và méo nhẹ, bị trúng đòn thì camera rung, nhảy xuống thì nhân vật dẹt lại một chút. Mục tiêu là giết kẻ địch lần thứ 200 vẫn sướng ([Point'n Think](https://www.pointnthink.fr/en/gamen-breakfast-sebastien-benard-2/)).
  - Talk GDC 2019 có mục "Control-freak" về nhảy "just-in-time", đoán ý người chơi, quay đầu tự do và auto-aim ([slides GDC](https://media.gdcvault.com/gdc2019/presentations/Benard-Sebastian-DeepCells.pdf)).
  - Đội dành "khoảng nửa thời gian phát triển" để làm cho phần lõi vui ([PocketGamer.biz](https://www.pocketgamer.biz/video-a-dead-cells-post-mortem-from-motion-twins-sebastian-bernard/)).
  - Shacknews gọi đây là "cảm giác combat tốt nhất" trong thể loại ([Shacknews](https://www.shacknews.com/article/106608/dead-cells-review-dying-never-felt-so-good)).
- **Âm thanh là phần thưởng.** Người chơi Stardew nhắc cụ thể tới tiếng "pop" khi hái cây nhiều trái, tiếng chặt gỗ, tiếng bước chân, tiếng "ploop" khi hái berry và tiếng khi tặng quà được yêu thích ([diễn đàn chính thức](https://forums.stardewvalley.net/threads/sound-effects-appreciation-thread.6083/); [Screen Rant](https://screenrant.com/stardew-valley-satisfying-things/)). Toàn bộ âm thanh do một người (Eric Barone) làm ([Wikipedia](https://en.wikipedia.org/wiki/Stardew_Valley)).

### 2.2 Stardew giao tiếp trạng thái thế nào (mẫu cụ thể)

| Tình huống | Stardew làm gì | Mức chắc chắn |
|---|---|---|
| Năng lượng xuống dưới 15 lần đầu | Thông báo "You're starting to feel exhausted." Chỉ là cảnh báo, chưa có hiệu lực | [Nguồn: wiki Energy qua trích đoạn tìm kiếm](https://stardewvalleywiki.com/Energy) |
| Năng lượng < 20 mà vẫn dùng dụng cụ | **Thanh năng lượng rung và bắn giọt mồ hôi.** Phản hồi bằng hình ngay trên HUD, không có chữ | [Nguồn: wiki Energy](https://stardewvalleywiki.com/Energy) |
| Năng lượng = 0 | **Bong bóng chữ X trên đầu nhân vật** cùng thông báo "You feel sluggish from over-exertion". Đi chậm hẳn, dùng dụng cụ chậm, không quăng cần câu được. Tới −15 thì ngất và hết ngày | [Nguồn: wiki Energy](https://stardewvalleywiki.com/Energy) |
| Túi đầy, đi ngang vật phẩm | Vật không bị hút vào túi. Có thông báo "inventory full". Người chơi kể có "dấu X đỏ lớn" khi chạm vào sách | [Nguồn: thảo luận người chơi trên Steam](https://steamcommunity.com/app/413150/discussions/0/365163686062280807/), [diễn đàn chính thức](https://forums.stardewvalley.net/tags/fullinventory/). Hình dạng chính xác **[Chưa xác minh]** |
| Nhặt vật phẩm | HUDMessage ở góc dưới trái, có icon vật phẩm và số lượng. Nhặt thêm cùng loại thì số trên cùng thông báo tăng, không tạo dòng mới. Có tham số thời gian hiển thị và loại để chống chồng thông báo | [Nguồn: wiki Modding:Common tasks](https://stardewvalleywiki.com/Modding:Common_tasks); [mod Farmer's Thoughts mô tả hành vi gốc](https://www.nexusmods.com/stardewvalley/mods/54254) |
| Nhận vật phẩm quan trọng hoặc lần đầu | Nhân vật **giơ vật phẩm lên đầu** (animation `holdItem`) | [Nguồn: wiki Console commands](https://stardewvalleywiki.com/Modding:Console_commands) |
| Đặt đồ hoặc nội thất | Ô xanh nếu đặt được, ô đỏ nếu không (xa quá, bị chặn, sai loại). Trên tay cầm có ảnh mờ (ghost) của vật | [Nguồn: wiki Furniture](https://stardewvalleywiki.com/Furniture); [thảo luận Steam](https://steamcommunity.com/app/413150/discussions/0/405692758713802564/) |
| Ô mục tiêu của dụng cụ | Tùy chọn "Always show tool hit location" vẽ viền đỏ quanh ô sẽ bị tác động | [Nguồn: wiki Controls và thảo luận người chơi](https://stardewvalleywiki.com/Controls) |
| Tích lực dụng cụ (hoe hoặc bình tưới đã nâng cấp) | Giữ nút: animation chuyển giai đoạn và vùng ô được tô sáng mở rộng theo cấp. Thả nút thì thực hiện | [Nguồn: wiki Modding:Farmer sprite](https://stardewvalleywiki.com/Modding:Farmer_sprite) |
| Bình tưới | Có thanh nước trên icon bình (water meter) | [Nguồn: mô tả mod Nexus nói về "in-game water meter"](https://www.nexusmods.com/stardewvalley/mods/40852) |
| Tưới khi bình rỗng | Mô tả thường gặp là nhân vật vẫn làm động tác, không có nước, và có thông báo lỗi ngắn (HUDMessage loại Error) | **[Chưa xác minh]**. Không tìm được trích dẫn chính thức về câu chữ hay animation |
| Chưa đủ tiền hoặc nguyên liệu | Không tìm được nguồn | Không đưa vào làm chuẩn |

Lưu ý: HUDMessage của Stardew có sẵn các loại Achievement, New Quest, **Error**, Stamina, Health, mỗi loại một icon ([wiki Modding](https://stardewvalleywiki.com/Modding:Common_tasks)). Như vậy ngay cả khi Stardew dùng chữ, chữ đi kèm icon, đặt ở góc màn hình, tự tắt và không chặn input.

### 2.3 Quy tắc phản hồi đề xuất cho Pig Tycoon

Đây là **[Suy luận]** tổng hợp từ các nguồn trên.

- **Thứ tự kênh phản hồi:** animation và vật thể trong thế giới, rồi âm thanh, rồi HUD (nháy, rung, đổi màu), cuối cùng mới tới chữ (một dòng, tự tắt, không chặn input).
- **Thiếu tài nguyên:** icon tài nguyên trên HUD nháy đỏ 2–3 lần, rung ngang 2–4 px trong khoảng 150–250 ms, kèm tiếng "dud" trầm. Nhân vật làm cử chỉ "không được" như lắc đầu hoặc nhún vai. Chỉ khi người chơi thử lại lần hai trong vài giây mới hiện dòng chữ ngắn, ví dụ "Thiếu 2 Gỗ".
- **Mỗi dụng cụ có animation riêng và hiệu ứng riêng ở ô đích:**
  - Cuốc: đất bắn và ô đổi sprite.
  - Rìu: dăm gỗ và cây rung.
  - Bình tưới: tia nước và đất sẫm màu.
  - Liềm: cỏ bay.
  Bấm vào ô không hợp lệ thì có animation "hụt" khác hẳn, ví dụ cuốc nảy trên đá kèm tia lửa.
- **Va chạm combat:** kẻ địch nháy trắng 1–2 frame, hit-stop ngắn, đẩy lùi, hạt bắn và âm va chạm. Đòn kết liễu có hit-stop dài hơn. Bị trúng đòn thì thanh Chống Chịu rung và camera rung nhẹ. Rung màn hình phải có tùy chọn tắt.
- **Nhặt đồ:** vật phẩm nảy lên khi rơi ra, bị hút về nhân vật khi tới gần, rồi có tiếng "pop". Toast gộp số lượng ở một góc cố định. Vật phẩm lần đầu nhận thì giơ lên đầu.
- **Túi đầy:** vật phẩm không bị hút mà nảy ra xa, icon túi nháy đỏ và có một lần toast "Túi đầy".
- **Mệt hoặc Chống Chịu thấp:** thanh rung khi tiêu hao, nhân vật đổ mồ hôi hoặc thở dốc, bước chân chậm lại. Không dùng popup chặn.

---

## 3. Thiết kế tương tác

### Bằng chứng

- **Stardew dùng hai nút chính.** Bàn phím và chuột: chuột trái để "Use tool / place item", chuột phải để "Check / Do action". Xbox: A để kiểm tra hoặc hành động, X để dùng dụng cụ, cần phải để di con trỏ ([wiki Controls](https://stardewvalleywiki.com/Controls)).
  - Với tay cầm, hạt giống, cuốc và cúp có thể dùng ngay bằng nút hành động lên ô trước mặt. Hàng rào thì phải dùng cần phải để đưa con trỏ tới. Con trỏ hiện ô đỏ, rồi chuyển xanh khi đủ gần ([thảo luận Steam](https://steamcommunity.com/app/413150/discussions/0/405692758713802564/)).
- **Bản mobile của Stardew.**
  - Có "auto-select" tự đổi dụng cụ theo mục tiêu và có auto-attack ([App Store](https://apps.apple.com/app/id1406710800)). Có nhiều sơ đồ điều khiển: chạm để đi kèm tự đánh, hoặc joystick kèm nút ([Cult of Mac](https://www.cultofmac.com/news/stardew-valley-ios-new-control-options)).
  - Nút "use tool" có thể giữ để lặp lại hành động ([wiki Mobile Controls, qua bản dịch](https://translate.google.com/translate?u=https%3A%2F%2Fstardewvalleywiki.com%2FMobile_Controls&hl=id&sl=en&tl=id&client=srp&prev=search)).
- **Tha thứ cho input (input forgiveness).** Celeste có coyote time, jump buffering, corner correction và cửa sổ wall-jump rộng 2 px. Thorson viết: "nó muốn bạn thành công" ([Maddy Thorson, Medium](https://maddythorson.medium.com/celeste-forgiveness-31e4a40399f1); [thread](https://threadreaderapp.com/thread/1238338574220546049.html)). Dead Cells có "đoán ý người chơi" và auto-aim ([slides GDC 2019](https://media.gdcvault.com/gdc2019/presentations/Benard-Sebastian-DeepCells.pdf)).
- **"Floaty" khác "input lag".** Người chơi gọi là floaty khi chuyển động có quán tính hoặc trọng lực lơ lửng, kể cả khi không có độ trễ ([NeoGAF](https://www.neogaf.com/threads/does-floaty-controls-ruin-the-game-for-anybody-else.1524012/)). Apotheon bị review tiêu cực vì "cảm giác điều khiển lỏng lẻo" ([Steam](https://steamcommunity.com/app/208750/discussions/0/611701999524522751/)).
- **Menu và túi đồ là một phần của tương tác.** Moonlighter được khen art nhưng bị chê "điều hướng túi đồ là ác mộng" vì di chuyển từng ô bằng tay cầm và không phản hồi nhanh ([Destructoid](https://www.destructoid.com/reviews/review-moonlighter/)). Tổng hợp review Steam: art đẹp nhưng bị điều khiển kém và combat cục mịch kéo xuống ([VaporLens](https://vaporlens.app/app/606150/moonlighter)).
- **Fields of Mistria** được khen vì đưa vào bản gốc những tiện ích trước đây phải dùng mod, ví dụ nhảy qua hàng rào ([Punished Backlog](https://punishedbacklog.com/fields-of-mistria-early-access-review/)).

### Tiêu chí

- Mỗi nền tảng có ≤ 2 nút hành động thế giới chính: "dùng thứ đang cầm" và "tương tác/kiểm tra". Các hành động khác lấy theo ngữ cảnh.
- Có ô mục tiêu hiển thị trước khi bấm (viền ô đích). Ô không hợp lệ có màu hoặc hình khác.
- Chế độ tự chọn dụng cụ là tùy chọn, mặc định bật trên mobile.
- Có input buffer cho đòn đánh, dùng dụng cụ và né: bấm sớm trong lúc thu đòn vẫn được ghi nhận. Thu đòn của nhân vật người chơi hủy được bằng né hoặc di chuyển **[Suy luận theo Cooper và Dead Cells]**.
- Giữ nút để lặp hành động (tưới hoặc cuốc nhiều ô) mà không cần bấm lại.
- Túi đồ hỗ trợ chuột kéo-thả, tay cầm (có nút chuyển nhanh cả chồng) và chạm. Sắp xếp tự động có sẵn.
- Không có hành động nào bắt buộc bấm hai nút cùng lúc trên mobile.
- Radial menu: chưa tìm được nguồn người chơi về radial trong farm-sim. Nếu dùng cho kỹ năng trang bị thì chỉ để tay cầm, ≤ 8 ô, và mở bằng giữ nút **[Suy luận]**.

---

## 4. HUD và giao diện

### Bằng chứng

- **HUD của Stardew rất ít.** Gồm hộp Ngày/Giờ/Tiền, thanh công cụ, thanh Máu và Năng lượng, và vùng buff ([mod HUD Customizer liệt kê thành phần](https://www.nexusmods.com/stardewvalley/mods/23704)). Thanh máu chỉ hiện khi cần. Một bài phân tích nhận xét "UI không bao giờ áp đảo" ([Kokutech](https://www.kokutech.com/blog/gamedev/design-patterns/world-building/stardew-valley)).
- **Ngay cả Stardew vẫn bị góp ý trên mobile.** Thanh ngày bị góc bo của iPhone che, túi đồ bị Dynamic Island che ([diễn đàn chính thức](https://forums.stardewvalley.net/threads/hud.53139/)). Vì vậy phải tôn trọng safe area.
- **Sun Haven bị chê** "UI rối, hệ thống không được giải thích rõ, menu có thể gây choáng" ([Pixels & Cocoa](https://pixelsandcocoa.com/2026/04/11/sun-haven-review/)). Tổng hợp Steam: nhiều người bị ngợp bởi lượng nội dung và thông tin ở đầu game ([VaporLens](https://vaporlens.app/app/1432860/sun_haven)).
- **Graveyard Keeper** bị chê không ghim được nhiệm vụ lên màn hình, phải mở bảng NPC liên tục, và không có nhật ký giữ thông tin quan trọng ([Metacritic user](https://www.metacritic.com/game/graveyard-keeper/user-reviews/)).
- **Chained Echoes** bị chê không có tra cứu hiệu ứng trạng thái trong game, phải mở wiki ([Switchaboo](https://www.switchaboo.com/chained-echoes-switch-review/)).
- **Hades: màu là ngôn ngữ.** Độ hiếm của boon theo màu: Common trắng, Rare xanh dương, Epic tím, Heroic đỏ ([Fextralife](https://hades.wiki.fextralife.com/Boons)). Team có người chuyên lo UI/FX (Josh Barnett) và dựng "color script" cho toàn game ([MCV/DEVELOP](https://mcvuk.com/business-news/behind-the-art-of-hades-we-value-artistic-integrity-and-excellence-in-artistic-craft-at-supergiant-however-were-first-and-foremost-a-game-design-lead-team/)).
- **Kích thước chữ.**
  - Xbox Accessibility Guideline: chữ mặc định không dưới mức tối thiểu của XAG 101, phụ đề nên lớn hơn, và cho phóng tới ít nhất 200% ([XAG 104](https://learn.microsoft.com/en-us/xbox/accessibility/xbox-accessibility-guidelines/104)).
  - Khuyến nghị thường gặp cho phụ đề là ≥ 46 px ở 1080p. Với thoại do người chơi tự bấm qua thì có thể nhỏ hơn nhưng không dưới 32 px. Tối đa 2 dòng ([80.lv](https://80.lv/articles/10-golden-rules-on-subtitles-for-games) và nguồn tổng hợp, **[Nguồn thứ cấp]**).
  - Độ tương phản chữ thường ≥ 4.5:1.
- **Font pixel bị mờ** là lỗi kinh điển. Nguyên nhân là scale không nguyên hoặc lọc bilinear. Cách sửa là chỉ scale theo bội số nguyên và lọc nearest-neighbour ([GDevelop](https://forum.gdevelop.io/t/solved-pixel-art-fonts-are-fuzzy-text-font-blury/14225); [Godot](https://forum.godotengine.org/t/blurry-font-but-only-in-full-screen-pixel-art/86540); [hướng dẫn pixel-perfect](https://spritesheetgenerator.online/guides/pixel-perfect-scaling)).

### Tiêu chí

- HUD luôn hiện ≤ 5 cụm: (1) ngày, giờ và thời tiết; (2) tiền; (3) thanh Chống Chịu và Thể lực; (4) thanh công cụ hoặc kỹ năng; (5) nhiệm vụ đang ghim, tối đa 1 dòng. Các thứ khác chỉ hiện theo ngữ cảnh rồi tự ẩn.
- Mỗi cụm HUD nằm ở góc hoặc mép, chừa trống vùng trung tâm màn hình.
- Toast có vị trí cố định, ≤ 3 dòng chồng, tự tắt trong 2–4 giây, gộp vật phẩm cùng loại.
- Icon đi trước chữ. Mọi tài nguyên có icon riêng, phân biệt được bằng hình dáng chứ không chỉ bằng màu.
- Chữ trong game là font bitmap ở đúng kích thước gốc ×N nguyên. Không có chữ nào bị mờ hoặc vỡ nét ở 1080p, 1440p và trên điện thoại.
- Kích thước chữ thoại ≥ 32 px ở 1080p, và có tùy chọn tăng cỡ chữ.
- Menu chính có ≤ 6 tab. Mỗi thông tin cần thiết tới được trong ≤ 2 lần bấm từ màn chơi.
- Có cách ghim nhiệm vụ lên HUD, và có sổ tay hoặc bách khoa tra cứu hiệu ứng, trạng thái và công thức trong game.
- HUD tôn trọng safe area trên điện thoại có tai thỏ hoặc Dynamic Island.
- Thanh giá trị có ngưỡng màu (bình thường, thấp, nguy hiểm), và chỉ animate khi giá trị thay đổi.

---

## 5. Trình bày cốt truyện

### Bằng chứng

- **Stardew rải truyện qua nhân vật, ngày và địa điểm.**
  - Cutscene phụ thuộc mức thân thiết và vị trí. Ví dụ đủ thân với Alex rồi nói chuyện khi anh ta đứng ở chuồng chó thì cutscene mở ([nghiên cứu ETPC 2025](https://doi.org/10.1108/ETPC-09-2025-0218)).
  - Cấu trúc heart event phổ biến: 2 tim là ấn tượng đầu; 2–4 tim mở mạch truyện lặp lại; 6 tim là tác động sâu hơn; 8 tim là cao trào gần; 10 tim là đỉnh; 14 tim là phần kết. Hướng dẫn khuyên giữ heart event ngắn, "trong vài phút", và rải phát triển nhân vật qua lễ hội, lịch sinh hoạt, nhiệm vụ và thoại thường ([Stardew Modding Wiki](https://stardewmodding.wiki.gg/wiki/Tips_for_Heart_Events_-_Writing_and_Narrative_Design)).
  - Một ngày trong game dài khoảng 14 phút 20 giây thực (cùng nguồn).
- **Thư là kênh truyện ít xâm lấn.** Thư tới hộp thư vào sáng hôm sau. Wizard, Krobus và Sandy có giấy viết riêng. Có cả thư giữa các dân làng nằm trong nhà họ, để người chơi tự đọc ([wiki Letters](https://stardewvalleywiki.com/Letters); [Modding:Mail data](https://stardewvalleywiki.com/Modding:Mail_data)).
- **Bí ẩn được gieo qua sự kiện địa điểm.**
  - Vào mùa đông, đi từ Farm sang Bus Stop trong khung 6:00–16:00 sẽ kích hoạt cảnh một "bóng đen" bỏ chạy. Hôm sau có thư "A Winter Mystery". Người chơi nhận Kính lúp, từ đó tìm được Secret Notes khi đào, chặt, câu cá hoặc đánh quái. Có 25 ghi chú gốc, thêm 2 ở bản 1.6. Mỗi ghi chú là câu đố, bản đồ kho báu hoặc mảnh truyện ẩn ([wiki Secret Notes](https://stardewvalleywiki.com/Secret_Notes); [PC Gamer](https://www.pcgamer.com/how-to-read-stardew-valley-secret-notes-magnifying-glass/)).
  - Phe phản diện Joja được gieo bằng hình ảnh ngay từ cảnh mở đầu: văn phòng màu xanh băng, tiếng gõ phím, không có nhạc ([nghiên cứu ETPC](https://doi.org/10.1108/ETPC-09-2025-0218); [GDC 2024 Game Narrative Review](https://media.gdcvault.com/gdc2024/GNR/Papers/brooke+glasier+-+game+narrative+review+2023+-+stardew+valley!.docx.pdf)).
  - ConcernedApe cố ý để vài mạch truyện mở để người chơi tự suy ([Screen Rant](https://screenrant.com/stardew-valley-concernedape-interview-character-stories/)).
- **Hades: hội thoại phản ứng theo hành động và chia nhỏ.** Hơn 20.000 câu thoại được chia thành mẩu ngắn giữa các lượt chơi. Có hàng ưu tiên: sự kiện quan trọng trước, thoại chung sau. Thoại không lặp cho tới khi dùng hết các câu chưa phát. Người chơi tới cùng các mốc truyện dù mất 10 hay 100 lượt ([Game Developer, GDC 2021](https://www.gamedeveloper.com/audio/dive-into-the-dialogue-of-i-hades-i-at-gdc-2021); [GamesHub](https://www.gameshub.com/news/features/hades-greg-kasavin-breaks-down-supergiants-unique-approach-to-narrative-262459-2193/)).
- **Hyper Light Drifter kể chuyện không chữ.** Cả đội quyết định ngay từ đầu không có chữ, bỏ luôn cả số. Thoại NPC thay bằng chuỗi hình. Truyện nằm trong môi trường ([Kill Screen](https://killscreen.com/articles/building-wordless-world-hyper-light-drifter/)). Owlboy có nhân vật chính câm nhưng có cá tính hoàn chỉnh nhờ animation nhỏ ([GameSpew](https://www.gamespew.com/2016/11/owlboy-review/)).
- **Hộp thoại kiểu Undertale.**
  - Chữ chạy ra dần, bấm một lần để hiện hết, bấm lần nữa để sang câu.
  - Nhịp chữ, âm "blip" và font riêng tạo "giọng" cho nhân vật mà không cần lồng tiếng ([Solar Princess, Substack](https://thesolarprincess.substack.com/p/undertale-dialogue)).
  - Một phần người chơi ghét hiệu ứng máy chữ nếu bị ép đọc theo tốc độ chữ ([RPG Maker forum](https://forums.rpgmakerweb.com/threads/what-are-your-thoughts-about-dialogue-blips-when-a-character-speaks.82886/)).
- **Phản ví dụ chính: Eastward.**
  - Nintendo Wire: "nhịp chậm đến ì ạch", hai chương đầu "chậm một cách đau đớn", muốn người chơi quan tâm tới dàn nhân vật đông chỉ bằng cách cho họ "thời lượng lên màn hình phi lý" ([Nintendo Wire](https://nintendowire.com/reviews/eastward/)).
  - Jump Dash Roll: "ít hơn chắc chắn sẽ tốt hơn", với nhiều cutscene dài không cần thiết ([Jump Dash Roll](https://www.jumpdashroll.com/article/eastward-review)).
  - Người chơi trên Steam: "đọc 2 giờ, chơi 30 phút". Cũng có lời phàn nàn thoại không bắt đầu cho tới khi mọi nhân vật đi chậm rãi tới bàn ([Steam negative reviews](https://steamcommunity.com/app/977880/negativereviews/?browsefilter=toprated); [thảo luận Steam](https://steamcommunity.com/app/977880/discussions/0/2954914688100337857/)).
  - Trong khi đó art của Eastward được khen hết lời ([Game Informer](https://gameinformer.com/review/eastward/eastward-review-pixel-paradise)). Nghĩa là art đẹp không cứu được nhịp truyện.
- **Phản ví dụ khác.** Sea of Stars có người chơi xin thêm tùy chọn tua hoặc bỏ cutscene ([Steam Sea of Stars](https://steamcommunity.com/app/1244090/discussions/0/3806156942403985478)). Ngược lại, Chained Echoes được khen vì "hiếm khi cảm thấy dev phí thời gian của bạn bằng thoại lan man" ([Tech-Gaming](https://www.tech-gaming.com/chained-echoes/)).

### Tiêu chí

- Cutscene bắt buộc: ≤ 90 giây cho mỗi cảnh **[Suy luận]**, ≤ 1 cảnh bắt buộc mỗi ngày trong game ở tuần đầu. Cảnh dài hơn phải chia thành nhiều ngày hoặc địa điểm.
- Mọi cutscene đều tua được (giữ nút) và bỏ qua được (có xác nhận). Mọi hộp thoại có "bấm một lần để hiện hết, bấm lần hai để sang câu".
- Hộp thoại ≤ 3 dòng, có chân dung nhân vật với ít nhất 3 biểu cảm, và âm blip hoặc nhịp riêng cho An, Khoa và mỗi NPC chính.
- Không có cảnh nào bắt người chơi chờ NPC đi bộ chậm để vào vị trí trước khi nói. Nếu cần dàn cảnh, dịch chuyển hoặc fade.
- Truyện chính được rải bằng ít nhất 4 kênh: (1) sự kiện kích hoạt theo địa điểm, ngày hoặc giờ; (2) thư buổi sáng; (3) sự kiện mức thân thiết với NPC; (4) vật thể hoặc ghi chú tùy chọn trong môi trường. Cutscene chỉ dùng cho mốc lớn.
- Bí ẩn được gieo bằng hình ảnh hoặc hành vi bất thường trước, ví dụ một bóng người bỏ chạy, màu sắc lạ, dấu vết. Lời giải thích đến sau hoặc tùy chọn.
- Lore tùy chọn không bao giờ chặn tiến trình.
- Thoại NPC thường có nhiều câu và phản ứng với trạng thái gần đây (vừa thu hoạch, vừa ngất, vừa thắng đợt phòng thủ). Thoại không lặp lại ngay trong cùng tuần **[Suy luận theo Hades]**.

---

## 6. Tính nhất quán của pixel art

### Bằng chứng

- **Mixels (trộn độ phân giải pixel) là lời chê phổ biến nhất** trong góp ý game indie trên itch.io: "art không cùng lưới pixel", "kích thước pixel khác nhau khiến game trông chưa hoàn thiện", "pixel art không hợp với scale và xoay" ([itch.io feedback](https://itch.io/post/13397761); [Flowlab: STOP USING MIXELS](https://community.flowlab.io/t/mini-rant-stop-using-mixels/38287)). Sách *Pixel Logic* (Michael Azzi) khuyên không trộn độ phân giải. Phản biện: UI đôi khi dùng tỉ lệ khác, ví dụ sprite lưng trong Pokémon gốc phóng đôi ([Threads, Kiana Mosser](https://www.threads.com/@kianamosser/post/C6F_8uuOV2F?hl=en)).
- **Xoay hoặc scale sprite pixel** sinh răng cưa và rung (jitter). Camera theo dõi ở tốc độ sub-pixel gây nhấp nháy. Cách sửa là render ở độ phân giải thấp, phóng theo bội nguyên và làm tròn vị trí camera ([Godot forum](https://forum.godotengine.org/t/smooth-camera-in-pixel-perfect-game/112586); [Godot issue #57221](https://github.com/godotengine/godot/issues/57221)).
- **Trộn asset pack và trộn phong cách** làm game trông rời rạc. Lỗi phối cảnh của bóng đổ cũng bị chỉ ra ([itch.io feedback tổng hợp](https://itch.io/post/11171031)).
- **Khung hình của Stardew.**
  - Đi bộ của farmer là vòng 4 bước quay về frame giữa (12, 13, 12, 14), mỗi frame 200 ms.
  - Tưới (hướng lên) dùng 3 frame 75/100/500 ms, chạy một lần.
  - Quăng cần có 5 giai đoạn 100/40/40/80/200 ms.
  - Dụng cụ nâng cấp khi giữ có các frame "tích lực" riêng.
  - Enchant Swift làm mọi frame nhanh lên ×0,66.
  - Sprite NPC đi bộ dùng 4 frame mỗi hướng ([wiki Modding:Farmer sprite](https://stardewvalleywiki.com/Modding:Farmer_sprite); [diễn đàn](https://forums.stardewvalley.net/threads/help-for-npc-sprite-movement.8087/)).
  - Kết luận: chuyển động đọc được không cần nhiều frame, cần **thời lượng frame không đều**: giữ lâu ở tư thế chạm hoặc kết quả, lướt nhanh ở pha chuyển.
- **Tham chiếu chất lượng cao.** Owlboy được khen nhờ bảng màu hiệu quả kết hợp ngôn ngữ cơ thể ([Cubed3](https://www.cubed3.com/games/reviews/nintendo-switch/owlboy)). Eastward kết hợp pixel art 2D với hệ thống ánh sáng phức tạp ([80.lv](https://80.lv/articles/eastward-charming-chinese-pixel-art-adventure)). Moonlighter: "animation mới là thứ làm mọi thứ sống động" ([Destructoid](https://www.destructoid.com/reviews/review-moonlighter/)).

### Tiêu chí (khớp với ADR-0005 đang dùng 32 PPU)

- Mọi sprite trong thế giới dùng cùng một PPU. Không sprite nào hiển thị với tỉ lệ pixel khác lưới, kể cả hiệu ứng, vật phẩm rơi và bóng.
- Camera render qua Pixel Perfect Camera hoặc render texture độ phân giải thấp, phóng theo bội nguyên. Vị trí hiển thị được làm tròn về lưới pixel.
- Không xoay sprite pixel bằng transform. Nếu cần nghiêng hoặc xoay, vẽ frame riêng. Không scale non-integer, kể cả "pop" khi nhặt đồ: dùng frame vẽ sẵn hoặc scale theo bước nguyên.
- Mỗi hành động của nhân vật người chơi có đủ 3 pha chuẩn bị, tiếp xúc và thu, với thời lượng frame không đều. Đi bộ ≥ 4 frame mỗi hướng. Mỗi dụng cụ có animation riêng.
- Silhouette của An, Khoa, NPC chính, kẻ địch và vật tương tác phân biệt được khi chuyển ảnh sang đen trắng hoặc đen đặc.
- Mỗi vùng hoặc bản đồ có bảng màu giới hạn, có tài liệu. Ánh sáng ngày và đêm dùng lớp phủ hoặc palette swap nhất quán, không đổi từng sprite một cách lẻ tẻ.
- Không có asset nào lẫn phong cách khác (asset pack, đường viền khác độ dày, đổ bóng khác hướng).
- Trước khi promote art phải tuân thủ AE-0001, AE-0002 và quy trình so ảnh của dự án (AGENTS.md).

---

## 7. Vòng lặp lõi và tiến trình cho farm-sim

### Bằng chứng

- **Nhịp ngày của Stardew.** Ngày dài khoảng 14 phút thực. Đồng hồ và năng lượng tạo áp lực "tận dụng ngày". Một bài phân tích cho rằng áp lực này đôi khi đi ngược tinh thần thư giãn ([Kokutech](https://www.kokutech.com/blog/gamedev/design-patterns/world-building/stardew-valley); [Stardew Modding Wiki](https://stardewmodding.wiki.gg/wiki/Tips_for_Heart_Events_-_Writing_and_Narrative_Design)).
- **Cozy Grove giới hạn nội dung truyện mỗi ngày** (khoảng 20–60 phút tùy nguồn) để tạo "nghi thức hằng ngày", cảm giác "hôm nay đã làm được gì và có điều để mong ngày mai" (David Edery) ([SVG](https://www.svg.com/381783/the-most-refreshing-part-of-cozy-grove/); [Spry Fox support](https://support.spryfox.com/hc/en-us/articles/1500006739102-Time-traveling-changing-the-system-clock-to-play-more-Cozy-Grove)). Mặt trái: GameSpew không chắc đây là lựa chọn đúng vì ngoài phần đó còn ít việc để làm ([GameSpew](https://www.gamespew.com/2021/04/cozy-grove-review/)).
- **Graveyard Keeper bị chê grind và khóa tầng tầng lớp lớp:** tài nguyên khóa sau nhiệm vụ, nhiệm vụ khóa sau bàn chế tạo, bàn chế tạo khóa sau cây công nghệ. Phải chờ NPC xuất hiện đúng ngày. Đi bộ chậm ([OpenCritic](https://opencritic.com/game/6488/score/reviews?page=2); [Steam: "feels like work"](https://steamcommunity.com/app/599140/discussions/0/1734336452589063440/)).
- **Sun Haven và Coral Island bị chê "phình nội dung".** Hàng nghìn công thức nhưng ít công dụng ([Steam Sun Haven](https://steamcommunity.com/app/1432860/discussions/0/3818528846671810236/)). Coral Island: điểm đặc biệt "lạc giữa đống tính năng farm-sim bắt buộc" ([Cozy Game Reviews](https://cozygamereviews.com/coral-island-review/)).
- **Chained Echoes được khen tôn trọng thời gian:**
  - di chuyển nhanh;
  - không có random encounter;
  - thua thì thử lại ngay trận đó;
  - hồi đầy sau mỗi trận;
  - fast travel dễ ([Serious Gamer](https://seriousgamer.ca/2023/01/08/chained-echoes-review-a-love-letter-to-classic-jrpgs-minus-their-baggage/); [God is a Geek](https://godisageek.com/reviews/chained-echoes-review/)).
  
  Nhưng game bị chê animation đòn dài không tua được ([Gaming Nexus](https://www.gamingnexus.com/Article/9751/Chained-Echoes/)).
- **Fields of Mistria:** bản 1.0 được khen độ dài ngày và tiện ích tốt hơn. Bị chê kinh tế đầu game chậm và hệ thống stamina "nhắc bạn là người hơi nhiều" ([Pixels & Cocoa](https://pixelsandcocoa.com/2025/05/18/fields-of-mistria-early-access-review/)).

### Tiêu chí

- Mỗi ngày trong game có ít nhất 3 lớp mục tiêu chồng nhau, nhìn thấy được: ngắn (việc hôm nay), vừa (mốc tuần hoặc mùa, công trình) và dài (bí ẩn, quan hệ).
- Trong mỗi 2 phút chơi thực có ít nhất 1 phần thưởng nhìn thấy được: vật phẩm, tiền, mở khóa, thoại mới hoặc tiến độ thanh **[Suy luận]**.
- Không có "thời gian chết" > 20 giây: đi bộ không có gì để làm hay chờ animation không tua được. Đường đi xa có lối tắt hoặc dịch chuyển mở sớm.
- Thua hoặc ngất (Chống Chịu bằng 0) có hậu quả rõ nhưng không xóa tiến độ của cả ngày. Có thể thử lại đợt phòng thủ.
- Giá mua và bán, lợi nhuận mỗi cây trồng và chi phí hiển thị ngay trong UI hoặc tooltip, không cần wiki.
- Mỗi hệ thống mới mở phải có ít nhất 1 công dụng ngay trong 1–2 ngày sau khi mở.
- Không có khóa tiến trình phụ thuộc NPC xuất hiện đúng 1 ngày cố định mà không có báo trước.

---

## 8. Kỳ vọng riêng cho mobile

### Bằng chứng

- **Epic (Fortnite mobile):** chỉ hiện thứ thiết yếu tại thời điểm đó, **không quá 5–6 control nhìn thấy cùng lúc**. Hành động chính đặt ở góc dưới (vùng ngón cái). Joystick động, xuất hiện ở chỗ ngón tay đặt xuống. "Điều khiển mobile tồi là bê nguyên layout tay cầm thành nút ảo." Control phải hợp thể loại ([Epic Dev Docs](https://dev.epicgames.com/documentation/fortnite/designing-for-mobile-in-fortnite?lang=en-US)).
- **Đặt nút dọc mép** để ngón tay không che vùng giữa màn hình ([Mobile Free To Play](https://mobilefreetoplay.com/control-mechanics/)).
- **Stardew mobile:**
  - Có tự chọn dụng cụ và auto-attack ([App Store](https://apps.apple.com/app/id1406710800)).
  - Tap-to-move cho phép zoom ra rồi chạm điểm xa để nhân vật tự đi. Bản mobile được đánh giá "điểm giữa", không bằng chuột hoặc tay cầm ([GameIndustry.com](https://www.gameindustry.com/reviews/time-waster/revisiting-stardew-valley-on-mobile/)).
  - Người chơi lâu năm khuyên dùng joystick kèm nút và tắt auto-attack, vì tap-to-move không chính xác khi thu hoạch ruộng lớn ([diễn đàn chính thức](https://forums.stardewvalley.net/threads/whats-the-most-efficient-control-on-mobile.32734/)).
  - Kết luận: **phải cho chọn nhiều sơ đồ điều khiển**.
- **Safe area:** HUD Stardew bị góc bo và Dynamic Island che ([diễn đàn](https://forums.stardewvalley.net/threads/hud.53139/)).

### Tiêu chí

- Màn chơi chuẩn trên mobile có ≤ 6 control cùng lúc: joystick, nút dùng hoặc hành động, nút tương tác, nút né (khi có combat), nút túi hoặc menu và thanh công cụ thu gọn. Nút kỹ năng trang bị chỉ hiện khi đang combat.
- Joystick động ở nửa trái. Nút chính ở góc dưới phải, cỡ chạm ≥ khoảng 9 mm (cỡ chạm là **[Suy luận / chuẩn HIG chung]**, chưa trích nguồn trong báo cáo này).
- Có ít nhất 2 sơ đồ điều khiển: joystick kèm nút, và chạm để đi kèm tự hành động. Có tự chọn dụng cụ.
- Giữ nút "dùng" để lặp hành động trên chuỗi ô.
- Không control nào nằm trong vùng an toàn bị che (tai thỏ, Dynamic Island, thanh điều hướng).
- Toast và thông báo không nằm dưới ngón cái.
- Có tùy chỉnh vị trí, kích thước và độ trong của nút (khuyến nghị phổ biến, **[Nguồn thứ cấp]**).

---

## 9. 25 dấu hiệu khiến người chơi gọi game pixel là "nghiệp dư"

| # | Dấu hiệu | Bằng chứng |
|---|---|---|
| 1 | Pixel khác cỡ (mixels), sprite bị phóng 2× giữa cảnh 1× | [itch.io](https://itch.io/post/13397761), [Flowlab](https://community.flowlab.io/t/mini-rant-stop-using-mixels/38287) |
| 2 | Xoay hoặc scale sprite pixel gây răng cưa, rung | [itch.io tổng hợp](https://itch.io/post/11171031), [Godot #57221](https://github.com/godotengine/godot/issues/57221) |
| 3 | Chữ pixel bị mờ hoặc nhòe do scale không nguyên | [GDevelop](https://forum.gdevelop.io/t/solved-pixel-art-fonts-are-fuzzy-text-font-blury/14225), [Godot](https://forum.godotengine.org/t/blurry-font-but-only-in-full-screen-pixel-art/86540) |
| 4 | Camera rung hoặc nhấp nháy khi theo nhân vật | [Godot forum](https://forum.godotengine.org/t/smooth-camera-in-pixel-perfect-game/112586) |
| 5 | Trộn asset pack hoặc phong cách, viền và bóng không thống nhất | [itch.io tổng hợp](https://itch.io/post/11171031) |
| 6 | Tô màu và nét kiểu "vẽ Paint", phối cảnh và bóng sai | [itch.io tổng hợp](https://itch.io/post/11171031) |
| 7 | Mở đầu bằng khoảng 10 phút bấm A qua thoại | [Grouvee/Sea of Stars](https://www.grouvee.com/user/80619-CatBut/reviews/3410809/) |
| 8 | "Đọc 2 giờ, chơi 30 phút", cutscene dài không cần thiết | [Steam/Eastward](https://steamcommunity.com/app/977880/negativereviews/?browsefilter=toprated), [Jump Dash Roll](https://www.jumpdashroll.com/article/eastward-review) |
| 9 | NPC đi bộ chậm vào vị trí rồi mới bắt đầu thoại | [Steam/Eastward](https://steamcommunity.com/app/977880/discussions/0/2954914688100337857/) |
| 10 | Dàn nhân vật đông, được cho nhiều thời lượng nhưng thiếu cá tính | [Nintendo Wire](https://nintendowire.com/reviews/eastward/) |
| 11 | Không tua hoặc bỏ qua được cutscene hay thoại | [Steam/Sea of Stars](https://steamcommunity.com/app/1244090/discussions/0/3806156942403985478) |
| 12 | Tutorial chữ chồng chất, "giống visual novel hơn game" | [RPGFan/CrossCode](https://www.rpgfan.com/review/crosscode/) |
| 13 | Ngược lại: không có hướng dẫn, phải mở wiki mới chơi được | [Metacritic/Graveyard Keeper](https://www.metacritic.com/game/graveyard-keeper/user-reviews/), [Switchaboo/Chained Echoes](https://www.switchaboo.com/chained-echoes-switch-review/) |
| 14 | Không ghim được nhiệm vụ, nhật ký thiếu thông tin quan trọng | [Metacritic/Graveyard Keeper](https://www.metacritic.com/game/graveyard-keeper/user-reviews/) |
| 15 | Ném quá nhiều hệ thống trong vài giờ đầu, UI rối | [Pixels & Cocoa/Sun Haven](https://pixelsandcocoa.com/2026/04/11/sun-haven-review/), [VaporLens](https://vaporlens.app/app/1432860/sun_haven) |
| 16 | Nội dung phình: hàng nghìn công thức nhưng ít công dụng | [Steam/Sun Haven](https://steamcommunity.com/app/1432860/discussions/0/3818528846671810236/), [Cozy Game Reviews/Coral Island](https://cozygamereviews.com/coral-island-review/) |
| 17 | Túi đồ điều hướng từng ô, chậm, thiếu tiện ích | [Destructoid/Moonlighter](https://www.destructoid.com/reviews/review-moonlighter/) |
| 18 | Điều khiển "dính", combat cục mịch | [Gamecritics/Moonlighter](https://gamecritics.com/gc-staff/moonlighter-review/), [VaporLens](https://vaporlens.app/app/606150/moonlighter) |
| 19 | Chuyển động "floaty": quán tính hoặc trọng lực lơ lửng, điều khiển lỏng | [NeoGAF](https://www.neogaf.com/threads/does-floaty-controls-ruin-the-game-for-anybody-else.1524012/), [Steam/Apotheon](https://steamcommunity.com/app/208750/discussions/0/611701999524522751/) |
| 20 | Đánh trúng mà không có phản hồi (không nháy, không đẩy lùi, không tiếng) | [Art of Screenshake](http://artificials.ch/juice-up-your-game/), [Point'n Think/Bénard](https://www.pointnthink.fr/en/gamen-breakfast-sebastien-benard-2/) |
| 21 | Animation đòn hoặc hành động dài, không hủy được nên mất quyền điều khiển | [Cooper](https://www.linkedin.com/pulse/12-principles-animation-video-games-jonathan-cooper), [Gaming Nexus/Chained Echoes](https://www.gamingnexus.com/Article/9751/Chained-Echoes/) |
| 22 | Grind và chuỗi khóa tầng tầng lớp lớp, phải chờ NPC đúng ngày | [OpenCritic/Graveyard Keeper](https://opencritic.com/game/6488/score/reviews?page=2) |
| 23 | Giật lag khi mở menu hoặc chuyển ngày | [VaporLens/Coral Island](https://vaporlens.app/app/1158160/coral_island) |
| 24 | Stamina hoặc áp lực đồng hồ nhắc liên tục, gây khó chịu | [Pixels & Cocoa/Fields of Mistria](https://pixelsandcocoa.com/2025/05/18/fields-of-mistria-early-access-review/), [Kokutech](https://www.kokutech.com/blog/gamedev/design-patterns/world-building/stardew-valley) |
| 25 | Layout tay cầm bê nguyên sang mobile, HUD bị tai thỏ che | [Epic](https://dev.epicgames.com/documentation/fortnite/designing-for-mobile-in-fortnite?lang=en-US), [diễn đàn Stardew](https://forums.stardewvalley.net/threads/hud.53139/) |

Ghi chú: một số người chơi cho rằng gameplay quan trọng hơn art: "họ bỏ qua art xấu nếu game vui, nhưng không bỏ qua gameplay dở dù art đẹp" ([itch.io](https://itch.io/post/11171031)). Moonlighter và Eastward là ví dụ art đẹp không cứu được điều khiển hoặc nhịp.

---

## 10. Checklist nghiệm thu mỗi build (trả lời Có/Không)

Cách chạy: một người **chưa từng chơi build này** chơi 20 phút đầu. Một người khác quay màn hình và bấm giờ. Mục nào "Không" thì build chưa đạt. Các ngưỡng số là **[Suy luận]** rút từ nguồn ở trên, có thể chỉnh nhưng phải ghi ADR.

### A. Onboarding

1. Từ "New Game" (sau tạo nhân vật) tới lúc di chuyển được ≤ 60 giây?
2. Cảnh mở đầu bắt buộc ≤ 2 phút và bỏ qua được?
3. Trong 3 phút đầu đã dùng một dụng cụ lên thế giới và thấy kết quả bằng hình?
4. Trong 10 phút đầu đã hoàn thành một vòng lặp lõi thu nhỏ có phần thưởng?
5. Không có popup chữ chặn input nào xuất hiện quá 1 lần cho cùng một khái niệm?
6. Ngày 1 mở ≤ 3 hệ thống: di chuyển và dụng cụ, trồng và tưới, bán?

### B. Phản hồi không cần chữ

7. Mỗi dụng cụ có animation riêng và hiệu ứng riêng ở ô đích?
8. Dùng dụng cụ vào ô không hợp lệ có animation hoặc tiếng "hụt" riêng, không chỉ chữ?
9. Thiếu tài nguyên thì icon tài nguyên nháy đỏ và rung, kèm tiếng báo, trước khi có chữ?
10. Chữ cảnh báo (nếu có) là 1 dòng, tự tắt, không chặn input?
11. Nhặt đồ có nảy, hút, tiếng "pop" và toast gộp số lượng ở vị trí cố định?
12. Túi đầy thì vật phẩm không bị hút và icon túi báo đỏ?
13. Chống Chịu hoặc Thể lực thấp thì thanh rung và nhân vật có dấu hiệu mệt nhìn thấy được?
14. Đánh trúng có nháy trắng, hit-stop ngắn, đẩy lùi, hạt và tiếng; đòn kết liễu có hit-stop dài hơn?
15. Có tùy chọn giảm hoặc tắt rung màn hình và nháy sáng?
16. Có ô mục tiêu hiển thị trước khi bấm, và ô không hợp lệ khác màu?

### C. Tương tác

17. ≤ 2 nút hành động thế giới chính trên bàn phím và tay cầm?
18. Có input buffer: bấm trong lúc thu đòn vẫn được ghi nhận?
19. Thu đòn của nhân vật người chơi hủy được bằng né hoặc di chuyển?
20. Giữ nút để lặp hành động trên chuỗi ô?
21. Túi đồ dùng được tốt bằng chuột, tay cầm và chạm, có sắp xếp tự động?

### D. HUD và UI

22. HUD thường trực ≤ 5 cụm, đặt ở mép, vùng giữa màn hình trống?
23. Mọi tài nguyên và trạng thái có icon phân biệt được bằng hình dáng?
24. Không có chữ mờ hoặc vỡ ở 1080p, 1440p và trên điện thoại thật?
25. Chữ thoại ≥ 32 px ở 1080p, có tùy chọn tăng cỡ chữ?
26. Mọi thông tin cần thiết tới được trong ≤ 2 lần bấm từ màn chơi?
27. Ghim được nhiệm vụ lên HUD, và có sổ tra cứu trạng thái, công thức, giá trong game?

### E. Cốt truyện

28. Mọi cutscene tua và bỏ qua được; mọi hộp thoại "bấm một lần hiện hết"?
29. Không cutscene bắt buộc nào > 90 giây; tuần đầu ≤ 1 cảnh bắt buộc mỗi ngày?
30. Hộp thoại ≤ 3 dòng, có chân dung và biểu cảm?
31. Truyện tuần đầu đến từ ít nhất 3 kênh khác cutscene (thư, sự kiện địa điểm, NPC, ghi chú môi trường)?
32. Bí ẩn đầu tiên được gieo bằng hình hoặc hành vi trước khi có lời giải thích?
33. Không cảnh nào bắt chờ NPC đi bộ vào vị trí?

### F. Pixel art

34. Mọi sprite thế giới cùng PPU (32), không có mixels?
35. Không sprite nào bị xoay hoặc scale không nguyên khi chạy?
36. Camera không rung hoặc nhấp nháy khi đi chậm và đi chéo?
37. Đi bộ ≥ 4 frame mỗi hướng; hành động có pha chuẩn bị, tiếp xúc và thu với thời lượng frame không đều?
38. Silhouette của An, Khoa, kẻ địch và vật tương tác phân biệt được ở dạng đen đặc?
39. Không asset nào lệch phong cách, viền hoặc hướng bóng; đã qua quy trình AE-0001, AE-0002 và có ảnh so sánh?

### G. Vòng lặp và nhịp

40. Mỗi 2 phút chơi có ít nhất 1 phần thưởng nhìn thấy được?
41. Không có đoạn chết > 20 giây (đi bộ trống hoặc animation không tua được)?
42. Giá, lợi nhuận và chi phí hiện trong UI mà không cần wiki?
43. Mỗi hệ thống mới có công dụng thật trong 1–2 ngày sau khi mở?

### H. Mobile

44. ≤ 6 control cùng lúc trên màn chơi thường; nút kỹ năng chỉ hiện khi combat?
45. Có ít nhất 2 sơ đồ điều khiển và tự chọn dụng cụ?
46. Không control hoặc HUD nào bị tai thỏ, Dynamic Island hoặc góc bo che?
47. Không có giật khi mở menu hoặc chuyển ngày trên máy mục tiêu?

---

## Phụ lục: giới hạn của nghiên cứu này

- Không truy cập được trực tiếp stardewvalleywiki.com (lỗi 403). Các chi tiết wiki lấy qua trích đoạn tìm kiếm.
- Hành vi chính xác khi tưới bằng bình rỗng và khi thiếu tiền trong Stardew **chưa xác minh**. Nên tự kiểm chứng bằng cách chơi thử trước khi dùng làm chuẩn sao chép.
- Không tìm được dữ liệu Steam công khai liên kết độ dài intro với tỉ lệ hoàn tiền. Dữ liệu rơi rụng trong báo cáo là của mobile.
- Chưa tìm được nguồn người chơi về radial menu trong farm-sim, và nguồn cỡ chạm tối thiểu chưa được trích.
- Các ngưỡng trong checklist (60 giây, 90 giây, 2 phút, ≤ 5 cụm HUD, ≤ 6 control) là đề xuất tổng hợp, không phải số của GDD. Muốn biến thành chuẩn chính thức thì ghi ADR.
