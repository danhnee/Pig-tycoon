import { type CombatSlotType, type MartialSchool } from './enums.ts';

export interface BaseStats {
  str: number; // Sức mạnh
  agi: number; // Nhanh nhẹn
  ctrl: number;// Khống chế
  res: number; // Bền bỉ
}

export interface BonusStats {
  str: number; // Tối đa +8 từ trang bị
  agi: number;
  ctrl: number;
  res: number;
}

export interface StaminaSystem {
  currentStamina: number;      // 0 - 180 (gốc)
  maxStamina: number;          // Trần 180 - 260
  dailyLoad: number;           // Tải ngày ("nợ thể lực" trong ngày, xóa khi ngủ)
  condition: number;           // Thể trạng (0 - 100, nợ sức khỏe nhiều ngày)
  immunity: number;            // Miễn dịch (0 - 100)
  reboundFatigueStacks: number;// Mệt Dội (tối đa 4)
}

export type EquipSlot = 
  | 'Weapon'    // Vũ khí
  | 'Boots'     // Giày
  | 'BodyArmor' // Giáp thân
  | 'Pants'     // Quần giáp
  | 'Bracelet'  // Vòng tay
  | 'Necklace'; // Vòng cổ

export interface EquipmentItem {
  id: string;
  name: string;
  slot: EquipSlot;
  weightScore: 1 | 2 | 3;      // 1=Nhẹ, 2=Vừa, 3=Nặng
  defense: number;
  bonusStats: Partial<BonusStats>;
  specialMechanic: string;
}

export type PlaystyleType = 'KhinhThan' | 'CanBang' | 'TrongGiap';

export interface CharacterData {
  id: string;
  name: string;
  level: number;
  exp: number;
  
  // Chỉ số gốc (Cố định, chỉ tăng theo cấp)
  baseStats: BaseStats;
  bonusStats: BonusStats;
  
  hp: number;
  maxHp: number;
  rage: number;                // Nộ khí 0 - 100
  
  stamina: StaminaSystem;
  
  // 6 ô trang bị
  equipment: Record<EquipSlot, EquipmentItem | null>;
  currentPlaystyle: PlaystyleType;
  
  // Võ kỹ
  primarySchool: type MartialSchool;
  activeMartialSkills: Record<type CombatSlotType, string | null>;
  learnedSkills: string[];
}
