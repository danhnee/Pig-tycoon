import { type ItemTier, type MarketTheme } from './enums.ts';

export interface GameItem {
  id: string;
  name: string;
  tier: type ItemTier;
  referenceValueGold: number; // GTTC (Giá Trị Tham Chiếu)
  category: 'Pig' | 'Material' | 'Feed' | 'Consumable' | 'Blueprint' | 'Special';
  description: string;
}

export interface DayMarketState {
  theme: type MarketTheme;
  themePriceMultiplier: number;
  dailyPurchasingPowerCap: number; // Sức mua heo trưởng thành hôm nay
  currentPigsSoldToday: number;
  
  feedPrices: {
    standardKg: number;
    growthKg: number;
    premiumKg: number;
  };
  dieselPerLiter: number;
  quarantineFeePerPig: number;     // 60G
  screeningFeePerPig: number;      // 120G
}

export interface NightMarketListing {
  slotId: string;
  offeredItem: GameItem;
  quantity: number;
  
  // Cơ chế Trao Đổi
  requestedItemDescription: string;
  requestedCategoryId: 'Pig' | 'Material' | 'Feed' | 'Consumable' | 'Blueprint' | 'Special';
  requestedItemIds?: string[];
  minimumReferenceValue: number;    // GTTC tối thiểu yêu cầu
  
  // 18% ô nhận vàng
  acceptsGold: boolean;
  goldPrice?: number;              // GTTC * 1.4
  
  feePercentage: number;           // 5% GTTC
}

export interface MysticMerchantState {
  isActive: boolean;
  spawnTimeHour: number;           // 18:00 - 04:00
  durationHoursRemaining: number;  // Tồn tại đúng 2 giờ game
  distanceFromBoundaryMeters: number; // Trong vòng 30m
  
  listings: Array<{
    id: string;
    offeredItem: GameItem;
    requestedPriceMultiplier: number; // 1.30 - 2.00 (130% - 200% GTTC)
    eventPressureDelta: number;       // +2 đến +10 ÁLSK tức thời
  }>;
}
