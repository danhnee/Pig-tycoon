import { type HabitatTier, type EventPressureState } from './enums.ts';

export interface FarmInfrastructure {
  landPlots: number;      // Ô Đất (x8 sức chứa, duy trì 4G/ngày)
  shelters: number;       // Mái trú (x6 sức chứa, duy trì 6G/ngày)
  waterTroughs: number;   // Bồn nước (x12 sức chứa, duy trì 3G/ngày)
  feeders: number;        // Máng ăn (x10 sức chứa, duy trì 5G/ngày)
  
  processingLotCapacity: number; // Ô tập kết Khu Xử Lý: 4 hoặc 8 ô
  drainageSystemLevel: number;   // Hệ thống thoát nước (SC +1/ngày)
}

export type CapacityBottleneck = 'landPlots' | 'shelters' | 'waterTroughs' | 'feeders';

export interface FarmCapacityReport {
  capacityByLand: number;
  capacityByShelters: number;
  capacityByWater: number;
  capacityByFeeders: number;
  bottleneck: CapacityBottleneck;
  baseCapacity: number;
  unprocessedCorpsesOutsideLot: number;
  effectiveCapacity: number;
  livingPigCount: number;
  rawDensityRatio: number;      // e.g. 0.85 or 1.35
  densityZoneName: string;
  diseaseDensityMultiplier: number; // Hệ số dịch nội suy mềm (0.85 -> 2.20)
  dailyEmptyMaintenanceCost: number; // Phí duy trì hạ tầng bỏ trống
}

export interface FarmState {
  farmLevel: number;             // Cấp trại 1 - 20
  farmExp: number;               // EXP tích lũy
  infrastructure: FarmInfrastructure;
  
  habitatIndex: number;          // Sinh Cảnh (SC 0 - 100)
  habitatTier: type HabitatTier;
  
  eventPressureIndex: number;    // Áp Lực Sự Kiện (ÁLSK 0 - 100)
  eventPressureState: type EventPressureState;
  
  gold: number;                  // Vàng trong ví
  feedInventoryKg: {
    standard: number;            // Cám Thường (0.8G/kg)
    growth: number;              // Cám Tăng Trọng (1.4G/kg)
    premium: number;             // Cám Cao Cấp (2.2G/kg)
    lingzhi: number;             // Cám Linh Chi (Bậc II, bồi dưỡng)
    haHoa: number;               // Cám Hạ Hỏa (Bậc II, thuần Xích Mao)
  };
  
  fuelLiters: number;            // Diesel (12G/lít)
  powerGrid: {
    currentCapacityUnits: number; // u
    peakCapacityUnits: number;
    currentLoadUnits: number;
    capacitorStorageUnits: number;
  };
  
  corpseLotCount: number;        // Số xác trong Khu Xử Lý ô tập kết
}
