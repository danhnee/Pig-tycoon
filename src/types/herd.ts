import { type HerdStateTier } from './enums.ts';

export interface HerdConditionsReport {
  // 1. >= 100 heo gắn kết (Hòa nhập >= 50, Kim Thọ trưởng thành/già = 3)
  cohesivePigsCount: number;
  condition1_Met: boolean;
  
  // 2. Sinh Cảnh >= 50
  currentSC: number;
  condition2_Met: boolean;
  
  // 3. Tinh thần TB >= 60 (>= 55 nếu có Kim Thọ), không con nào hoảng loạn 24h
  avgMood: number;
  hasKimTho: boolean;
  requiredAvgMood: number;
  anyPanicLast24h: boolean;
  condition3_Met: boolean;
  
  // 4. Mật độ 71 - 110%
  densityRatio: number;
  condition4_Met: boolean;
  
  // 5. >= 6 heo già có Hòa nhập >= 65
  elderCohesiveCount: number;
  condition5_Met: boolean;
  
  // 6. Không có dịch nhóm A/B đang hoạt động, không có Chuồng Nhiễm
  hasActiveEpidemic: boolean;
  hasInfectedBarn: boolean;
  condition6_Met: boolean;
  
  // 7. Cấp trại >= 6 và đã qua >= 4 đợt lớn
  farmLevel: number;
  majorWavesSurvived: number;
  condition7_Met: boolean;
  
  allConditionsMet: boolean;
  consecutiveDaysMet: number; // Phải duy trì liên tục 3 ngày mới thành lập
}

export interface AlphaLeaderData {
  pigId: string;
  name: string;
  daysAsLeader: number;
  neoThreshold: number;       // 25 gốc (+5 mỗi tầng Truyền Thừa)
  neoRadiusMeters: number;    // 20m gốc (+5m mỗi tầng)
  isUnderAttack: boolean;     // Bị đánh trực tiếp -> Neo tắt 30s
  disableTimeLeftSeconds: number;
}

export interface SuccessionTrainingData {
  candidatePigId: string | null;
  consecutiveCareDays: number;
  bonusSuccessionPercentage: number;   // +3%/ngày (tối đa +30%)
  bonusTransmissionPercentage: number; // +3%/ngày (tối đa +30%)
}

export interface HerdState {
  tier: HerdStateTier;
  consecutiveDaysInState: number;
  consecutiveDaysConditionsFailed: number; // Vi phạm liên tục 3 ngày -> tan rã
  
  leader: AlphaLeaderData | null;
  successionTraining: SuccessionTrainingData;
  transmissionStacks: number;          // Tối đa 3 tầng Truyền Thừa
}
