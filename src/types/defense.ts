import { type BuildingCategory } from './enums.ts';

export type BlueprintRarity = 'Thuong' | 'Tinh' | 'CaoCap' | 'BachVan';

export interface DefenseBlueprint {
  id: string;
  name: string;
  category: BuildingCategory;
  rarity: BlueprintRarity;
  requiredHallLevel: number; // 1, 2 hoặc 3
  buildCostGold: number;
  buildMaterials: {
    wood?: number;
    steel?: number;
    refinedParts?: number;
  };
  baseBuildHours: number;
  isUnlocked: boolean;
  isBachVan: boolean;
}

export interface DefenseBuildingInstance {
  instanceId: string;
  blueprintId: string;
  name: string;
  category: BuildingCategory;
  isBachVan: boolean;
  
  // Trạng thái vận hành
  durability: number;          // 0 - 100
  maxHp: number;
  currentHp: number;
  isBroken: boolean;
  
  // Nạp trước (không nạp trong đợt)
  ammoCurrent: number;
  ammoCapacity: number;
  ammoName: string;
  ammoCostPerShot: number;
  
  // Năng lượng & hồi chiêu
  powerDrawIdle: number;       // u
  powerDrawActive: number;     // u
  cooldownSeconds: number;
  currentCooldownRemaining: number;
  
  // Chế độ bắn
  fireMode: 'TuDo' | 'TietKiem' | 'ChiTinhAnh';
  
  // Đối với Bạch Vân (kích hoạt thủ công)
  isManualTriggerReady: boolean;
  warmupTimeRemainingSeconds: number;
}

export interface BuildingHallState {
  level: number;               // 1 - 3
  activeConstructionSlots: number;
  maxConstructionSlots: number; // 1 (cấp 1-2) hoặc 2 (cấp 3), +1 nếu Kỹ Sư mở đặc tính
  
  assignedWorkerType: 'None' | 'OtherNpc' | 'EngineerBasic' | 'EngineerMaster';
  workerEfficiency: number;    // 0.40, 0.70 hoặc 1.00
  
  unlockedBlueprints: string[];
  activeConstructions: Array<{
    blueprintId: string;
    targetSlotId: string;
    hoursRemaining: number;
    isRepair: boolean;
  }>;
}
