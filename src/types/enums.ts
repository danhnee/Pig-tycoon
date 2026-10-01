/**
 * PIG TYCOON GDD v6.0 - Official Core Constants & Types
 * Uses 'as const' objects compatible with modern TypeScript type-stripping.
 */

export const TimeOfDay = {
  Sang: 'Sáng',       // 06:00 - 10:00 (Stamina outdoor -5%, SC hồi +5%, heo ăn +5%)
  Trua: 'Trưa',       // 10:00 - 14:00 (Stamina outdoor +10%, nhiệt +15%, nước +10%)
  Chieu: 'Chiều',     // 14:00 - 18:00 (Trung tính; chợ ngày sức mua +10%)
  Toi: 'Tối'          // 18:00 - 06:00 (Tầm nhìn -15%, quái trộm +20%, chợ đêm mở)
} as const;
export type TimeOfDay = (typeof TimeOfDay)[keyof typeof TimeOfDay];

export const WeatherType = {
  QuangDang: 'Quang Đãng',     // 25% Thường
  NhieuMay: 'Nhiều Mây',       // 20% Thường
  NangGat: 'Nắng Gắt',         // 12% Cố định (Say Nắng)
  Mua: 'Mưa',                  // 15% Cố định (Ướt Lạnh, Hô Hấp Lạnh)
  GiongBao: 'Giông Bão',       // 6% Cố định (Sét Đánh, Cảm Gió)
  SuongMu: 'Sương Mù',         // 8% Thường (Lạc Hướng, tầm nhìn -35%)
  RetDam: 'Rét Đậm',           // 5% Thường (Cóng Tay)
  NomAm: 'Nồm Ẩm',             // 5% Thường (Trơn Trượt, Ghẻ Ký Sinh)
  GioKho: 'Gió Khô',           // 3% Thường (Khô Rát)
  MuaBucXa: 'Mưa Bức Xạ'       // 1% Cố định từ chu kỳ 6 (Nhiễm Xạ, đột biến)
} as const;
export type WeatherType = (typeof WeatherType)[keyof typeof WeatherType];

export const PigStage = {
  HeoNon: 'Heo Non',           // 0-4 ngày (1.5 -> 12kg)
  DangLon: 'Đang Lớn',         // 5-10 ngày (12 -> 60kg, cửa sổ định hình)
  TruongThanh: 'Trưởng Thành', // 11-28 ngày (60 -> 110kg, đỉnh 95-115kg)
  HeoGia: 'Heo Già'            // 29+ ngày (giảm 1kg/ngày, mở ứng viên thủ lĩnh)
} as const;
export type PigStage = (typeof PigStage)[keyof typeof PigStage];

export const MeatQuality = {
  C: 'Hạng C', // x0.80
  B: 'Hạng B', // x1.00 (Chuẩn)
  A: 'Hạng A', // x1.20
  S: 'Hạng S'  // x1.50
} as const;
export type MeatQuality = (typeof MeatQuality)[keyof typeof MeatQuality];

export const MoodState = {
  BinhOn: 'Bình Ổn',   // 60 - 100: Bình thường
  BatAn: 'Bất An',     // 40 - 59: Tăng trọng -5%, lây bệnh +10%
  HoangSo: 'Hoảng Sợ', // 20 - 39: Chạy tán loạn, UFO khóa nhanh hơn 1s
  HoangLoan: 'Hoảng Loạn' // 0 - 19: Húc rào, bỏ ăn, lây hoảng loạn
} as const;
export type MoodState = (typeof MoodState)[keyof typeof MoodState];

export const GeneRarity = {
  Thuong: 'Thường',       // 78% tự nhiên, hiện tên ngay
  Kha: 'Khá',             // 17% tự nhiên, hiện tên ngay
  Hiem: 'Hiếm',           // 4% tự nhiên, dấu "?"
  Quy: 'Quý',             // 0.9% tự nhiên, dấu "?"
  HuyenThoai: 'Huyền Thoại', // 0.1% tự nhiên, dấu "?"
  DiBien: 'Dị Biến'       // 0% tự nhiên, dấu "?"
} as const;
export type GeneRarity = (typeof GeneRarity)[keyof typeof GeneRarity];

export const GeneLineId = {
  // Bậc Thường
  HongDien: 'Hồng Điền',
  LamKhe: 'Lam Khê',
  MocCuoc: 'Mộc Cước',
  
  // Bậc Khá
  ThoTram: 'Thổ Trầm',
  XichMao: 'Xích Mao',
  PhongMau: 'Phong Mẫu',
  
  // Bậc Hiếm
  ThietBi: 'Thiết Bì',
  TinhQuang: 'Tinh Quang',
  
  // Bậc Quý
  LoiMach: 'Lôi Mạch',
  MongNhan: 'Mộng Nhãn',
  
  // Bậc Huyền Thoại
  KimTho: 'Kim Thọ',
  
  // Bậc Dị Biến
  HuThe: 'Hư Thể'
} as const;
export type GeneLineId = (typeof GeneLineId)[keyof typeof GeneLineId];

export const HabitatTier = {
  TrongLanh: 'Trong Lành', // 90 - 100
  OnDinh: 'Ổn Định',       // 70 - 89
  TrungTinh: 'Trung Tính', // 50 - 69
  ONhiem: 'Ô Nhiễm',       // 30 - 49
  OUe: 'Ô Uế'             // 0 - 29
} as const;
export type HabitatTier = (typeof HabitatTier)[keyof typeof HabitatTier];

export const HerdStateTier = {
  KhongCo: 'Không Có',
  SoKhai: 'Sơ Khai',
  Vung: 'Vững',
  HungThinh: 'Hưng Thịnh'
} as const;
export type HerdStateTier = (typeof HerdStateTier)[keyof typeof HerdStateTier];

export const MarketTheme = {
  CanBang: 'Cân Bằng',
  MuaHeoRe: 'Mùa Heo Rẻ',
  ConSotThit: 'Cơn Sốt Thịt',
  KhanHiemThucAn: 'Khan Hiếm Thức Ăn',
  HoangLoanDich: 'Hoảng Loạn Dịch',
  KhanHiemHauDotKich: 'Khan Hiếm Hậu Đột Kích',
  MuaTrangBi: 'Mùa Trang Bị',
  MuaCongTrinh: 'Mùa Công Trình'
} as const;
export type MarketTheme = (typeof MarketTheme)[keyof typeof MarketTheme];

export const BuildingCategory = {
  ThongTin_TrinhTham: 'Thông Tin - Trinh Thám',
  ThongTin_HangNgay: 'Thông Tin - Hằng Ngày',
  HoTro: 'Hỗ Trợ & Lưới Điện',
  TanCong: 'Tấn Công',
  PhongThu: 'Phòng Thủ',
  Bay: 'Bẫy',
  TrienKhai: 'Triển Khai'
} as const;
export type BuildingCategory = (typeof BuildingCategory)[keyof typeof BuildingCategory];

export const ItemTier = {
  I: 'I - Thường',
  II: 'II - Tốt',
  III: 'III - Quý',
  IV: 'IV - Cực Phẩm'
} as const;
export type ItemTier = (typeof ItemTier)[keyof typeof ItemTier];

export const MartialSchool = {
  TocBo_A: 'Phái A - Tốc Bộ',
  TrongKich_B: 'Phái B - Trọng Kích',
  KhongChe_C: 'Phái C - Khống Chế',
  SinhTon_D: 'Phái D - Sinh Tồn'
} as const;
export type MartialSchool = (typeof MartialSchool)[keyof typeof MartialSchool];

export const CombatSlotType = {
  TheCong: 'Thế Công',
  TheThu: 'Thế Thủ',
  TheBien: 'Thế Biến'
} as const;
export type CombatSlotType = (typeof CombatSlotType)[keyof typeof CombatSlotType];

export const EventPressureState = {
  YenA: 'Yên ả',
  GonSong: 'Gợn sóng',
  BatOn: 'Bất ổn',
  RanNut: 'Rạn nứt'
} as const;
export type EventPressureState = (typeof EventPressureState)[keyof typeof EventPressureState];
