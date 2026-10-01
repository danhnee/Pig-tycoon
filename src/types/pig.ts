import { type GeneLineId, type GeneRarity, type MeatQuality, type MoodState, type PigStage } from './enums.ts';

export interface PigTrait {
  id: string;
  name: string;
  isPositive: boolean;
  isDeviant?: boolean; // Dị biến (cho Hư Thể)
  isAwakened?: boolean; // Dành cho tính chất xấu Dị Biến (Ngủ hay Thức)
  isSealed?: boolean;   // Đã phong ấn thành công hay chưa
  description: string;
}

export type DiseaseId =
  // Nhóm A: Dịch truyền nhiễm
  | 'HoHapLanh'
  | 'GheKySinh'
  | 'DichTaHeo'
  // Nhóm B: Bệnh môi trường
  | 'SotBun'
  | 'ChuongNhiem'
  // Nhóm C: Cấp / Mãn tính
  | 'SayNang'
  | 'RoiLoanSinhSan'
  // Nhóm D: Bệnh đặc biệt theo gen
  | 'NamPhatQuang'
  | 'LoiTam'
  | 'HuMach';

export interface DiseaseRecord {
  id: DiseaseId;
  name: string;
  group: 'A' | 'B' | 'C' | 'D';
  severity: number; // 0 - 100
  durationDays: number;
  isDiagnosed: boolean;
}

export interface PigData {
  id: string;
  name: string;
  ageDays: number;            // Tuổi tính bằng ngày
  weightKg: number;           // Cân nặng
  stage: type PigStage;            // Giai đoạn vòng đời
  gender: 'Duc' | 'Nai';
  
  // Gen & Nhận diện
  geneLine: type GeneLineId;
  rarity: type GeneRarity;
  isIdentified: boolean;      // True = đã biết tên & tính chất; False = dấu "?"
  isXichMaoDomesticated: boolean; // Dành cho Xích Mao sau nghiên cứu
  
  // Tính chất phụ (0-2)
  traits: PigTrait[];
  
  // Sức khỏe & Cảm xúc
  health: number;             // 0 - 100 (dưới 40 tăng trọng -20%, dễ lây x1.5)
  temperament: number;        // Tính khí bẩm sinh (cố định, gốc 50)
  mood: number;               // Tinh thần động (0 - 100, gốc 70)
  moodState: type MoodState;
  bonding: number;            // Hòa nhập đàn (0 - 100, gốc 40, >= 50 = gắn kết)
  adhesionScore: number;      // Độ bám đàn = (Hòa nhập + Tinh thần)/2
  
  // Sản xuất & Thương mại
  meatQuality: type MeatQuality;
  growthRateMultiplier: number; // Tốc độ tăng trọng (gốc 1.0)
  maxWeightKg: number;          // Trọng lượng trần (gốc 110kg)
  fcrMultiplier: number;        // Hệ số chuyển đổi thức ăn
  isQuarantinedForSale: boolean;// Đã kiểm dịch đạt chuẩn chưa (60G)
  hasDiseaseFlag: boolean;      // Đang có bệnh không được bán
  diseases: DiseaseRecord[];
  
  // Sinh sản & Kế thừa
  isF1: boolean;
  isF2: boolean;
  isPregnant: boolean;
  pregnancyDaysLeft: number;
  recoveryDaysLeft: number;
  littersDelivered: number;
  
  // Trạng thái sống
  isAlive: boolean;
  isInProcessingLot: boolean;  // Nếu chết, đã đưa về ô tập kết Khu Xử Lý chưa
  daysDeceased: number;        // Số ngày đã chết trong trại
}
