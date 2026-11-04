namespace PigTycoon.Core
{
    /// <summary>
    /// Bờ Rìa cast (story bible Phần 3). Portrait sets are kept small on purpose: every id here is a picture Argon must draw.
    /// Nobody in the village names the thing in the sky; voice notes keep writers on that rule.
    /// </summary>
    internal static class NarrativeCastContent
    {
        internal static void Register(NarrativeCast.AddFn add)
        {
            string N = Portraits.Neutral, H = Portraits.Happy, S = Portraits.Sad, U = Portraits.Surprised, T = Portraits.Thinking, A = Portraits.Angry, W = Portraits.Worried;
            add(CastIds.BaSau, "Bà Sáu", "Quán cơm", "#E9A86B", 0.95f, "Ân cần, hay dúi đồ ăn; trả lời câu khó bằng một việc vặt. \"Ăn thêm miếng nữa đi con.\"", N, H, S, W);
            add(CastIds.Man, "Mận", "Nông Công", "#C9A0DC", 1.1f, "Ít lời, nói với heo như bạn cùng lớp; ô đen; hay ngân bài ru. Xưng em.", N, H, S, U, T);
            add(CastIds.TamVa, "Tám Vá", "Kỹ Sư Xây Dựng", "#D9B26A", 0.85f, "Đặt tên máy theo chiêu võ; gõ ba lần để nghe; nói chuyện máy như chuyện người.", N, H, U, T, A);
            add(CastIds.Dung, "Bác sĩ Dung", "Bác Sĩ Thú Y", "#8FC1A9", 0.9f, "Khô như ngói; ghét ai gọi bệnh nào cũng là \"bị ám\".", N, A, T, S);
            add(CastIds.Cuc, "Cúc", "Nhà Nghiên Cứu Gen", "#B7D47A", 1.05f, "Cựu nghiên cứu rau; hay chỉ vào chỗ trống; nói bằng số liệu.", N, H, U, T, W);
            add(CastIds.Le, "Lệ", "Nhà Buôn", "#E6C35C", 1.1f, "Lanh, bán \"bảo hiểm chồn\"; giữ danh sách người mua heo già.", N, H, T, A);
            add(CastIds.AnhCa, "Anh Ca", "Quản Lý Ca", "#7FA8D9", 1f, "Mỗi sáng một lịch mới; không ai đọc đủ tên anh.", N, H, T, W);
            add(CastIds.OngTon, "Ông Tồn", "Thủ Kho", "#A89A86", 0.8f, "Giữ hộp rỗng; ký biên nhận trước khi xuất; chữ ký bảy nét.", N, T, S, U);
            add(CastIds.AnhTu, "Anh Tư", "Xe ôm", "#C4A484", 1f, "Phương tiện công cộng duy nhất; mọi thứ \"theo giờ anh Tư\".", N, H);
            add(CastIds.BiNhan, "Bí nhân", "Người buôn sau rào", "#6E6A8F", 0.7f, "Không nhận vàng, không nhận \"đồ cũ, kể cả thứ Hai\". Câu ngắn.", N);
            // Mercenaries: personality only through short lines (story bible: names create character through scenes).
            add(CastIds.Do, "Độ", "Vệ Binh Khiên", "#9C8C7C", 0.85f, "Đo chiều rộng cổng trước khi nhận việc.", N, H);
            add(CastIds.Mien, "Miên", "Xạ Thủ Nỏ", "#88A0A8", 1f, "Ít nói; mở miệng là bình luận về gió.", N, T);
            add(CastIds.Chot, "Chốt", "Kỹ Sư Chiến Trường", "#B08A5A", 1.05f, "Gọi bẫy hết lượt là \"đồng nghiệp đang nghỉ phép\".", N, H);
            add(CastIds.TuTroi, "Tư Trời", "Thợ Săn UFO", "#7C8FB0", 0.95f, "Gọi việc mình là \"săn chim lớn\", không bao giờ tả con chim.", N, T);
            add(CastIds.Van, "Vân", "Thích Khách Đêm", "#5E5470", 1.1f, "Ghét bị thuê đi giao bưu phẩm chỉ vì đi được trong bóng tối.", N, A);
        }
    }

}
