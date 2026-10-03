namespace PigTycoon.Core
{
    public enum TimeOfDay
    {
        Sang,   // 06:00 - 10:00 (Stamina outdoor -5%, SC hồi +5%, heo ăn +5%)
        Trua,   // 10:00 - 14:00 (Stamina outdoor +10%, nhiệt +15%, nước +10%)
        Chieu,  // 14:00 - 18:00 (Trung tính; chợ ngày sức mua +10%)
        Toi     // 18:00 - 06:00 (Tầm nhìn -15%, quái trộm +20%, chợ đêm mở)
    }

    public enum WeatherType
    {
        QuangDang,  // 25% Thường
        NhieuMay,   // 20% Thường
        NangGat,    // 12% Cố định (Say Nắng)
        Mua,        // 15% Cố định (Ướt Lạnh, Hô Hấp Lạnh)
        GiongBao,   // 6% Cố định (Sét Đánh, Cảm Gió)
        SuongMu,    // 8% Thường (Lạc Hướng, tầm nhìn -35%)
        RetDam,     // 5% Thường (Cóng Tay)
        NomAm,      // 5% Thường (Trơn Trượt, Ghẻ Ký Sinh)
        GioKho,     // 3% Thường (Khô Rát)
        MuaBucXa    // 1% Cố định từ chu kỳ 6 (Nhiễm Xạ, đột biến)
    }

    public enum PigStage
    {
        HeoNon,       // 0-4 ngày (1.5 -> 12kg)
        DangLon,      // 5-10 ngày (12 -> 60kg, cửa sổ định hình)
        TruongThanh,  // 11-28 ngày (60 -> 110kg, đỉnh 95-115kg)
        HeoGia        // 29+ ngày (giảm 1kg/ngày, mở ứng viên thủ lĩnh)
    }

    public enum MeatQuality
    {
        C, // x0.80
        B, // x1.00 (Chuẩn)
        A, // x1.20
        S  // x1.50
    }

    public enum MoodState
    {
        BinhOn,    // 60 - 100: Bình thường
        BatAn,     // 40 - 59: Tăng trọng -5%, lây bệnh +10%
        HoangSo,   // 20 - 39: Chạy tán loạn, UFO khóa nhanh hơn 1s
        HoangLoan  // 0 - 19: Húc rào, bỏ ăn, lây hoảng loạn
    }

    public enum GeneRarity
    {
        Thuong,      // 78% tự nhiên, hiện tên ngay
        Kha,         // 17% tự nhiên, hiện tên ngay
        Hiem,        // 4% tự nhiên, dấu "?"
        Quy,         // 0.9% tự nhiên, dấu "?"
        HuyenThoai,  // 0.1% tự nhiên, dấu "?"
        DiBien       // 0% tự nhiên, dấu "?"
    }

    public enum GeneLineId
    {
        // Bậc Thường
        HongDien,
        LamKhe,
        MocCuoc,

        // Bậc Khá
        ThoTram,
        XichMao,
        PhongMau,

        // Bậc Hiếm
        ThietBi,
        TinhQuang,

        // Bậc Quý
        LoiMach,
        MongNhan,

        // Bậc Huyền Thoại
        KimTho,

        // Bậc Dị Biến
        HuThe
    }

    public enum HabitatTier
    {
        TrongLanh, // 90 - 100
        OnDinh,    // 70 - 89
        TrungTinh, // 50 - 69
        ONhiem,    // 30 - 49
        OUe        // 0 - 29
    }

    public enum HerdStateTier
    {
        KhongCo,
        SoKhai,
        Vung,
        HungThinh
    }

    public enum MarketTheme
    {
        CanBang,
        MuaHeoRe,
        ConSotThit,
        KhanHiemThucAn,
        HoangLoanDich,
        KhanHiemHauDotKich,
        MuaTrangBi,
        MuaCongTrinh
    }

    public enum BuildingCategory
    {
        ThongTin_TrinhTham,
        ThongTin_HangNgay,
        HoTro,
        TanCong,
        PhongThu,
        Bay,
        TrienKhai
    }

    public enum ItemTier
    {
        I,
        II,
        III,
        IV
    }

    public enum MartialSchool
    {
        TocBo_A,
        TrongKich_B,
        KhongChe_C,
        SinhTon_D
    }

    public enum CombatSlotType
    {
        TheCong, // Mở bởi Vũ khí + Giày
        TheThu,  // Mở bởi Giáp thân + Quần
        TheBien  // Mở bởi Vòng tay + Vòng cổ
    }

    public enum EventPressureState
    {
        YenA,    // 0 - 19
        GonSong, // 20 - 49
        BatOn,   // 50 - 79
        RanNut   // 80 - 100
    }
}
