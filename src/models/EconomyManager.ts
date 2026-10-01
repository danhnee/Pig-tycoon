import { GeneRarity, MarketTheme, MeatQuality, PigStage, ItemTier } from '../types/enums.ts';
import { type PigData } from '../types/pig.ts';
import { type DayMarketState, type GameItem, type MysticMerchantState, type NightMarketListing } from '../types/market.ts';

export class EconomyManager {
  private dayMarket: DayMarketState;
  private nightListings: NightMarketListing[];
  private mysticMerchant: MysticMerchantState;

  constructor() {
    this.dayMarket = {
      theme: MarketTheme.CanBang,
      themePriceMultiplier: 1.0,
      dailyPurchasingPowerCap: 40,
      currentPigsSoldToday: 0,
      feedPrices: {
        standardKg: 0.8,
        growthKg: 1.4,
        premiumKg: 2.2
      },
      dieselPerLiter: 12,
      quarantineFeePerPig: 60,
      screeningFeePerPig: 120
    };

    this.nightListings = [];
    this.mysticMerchant = {
      isActive: false,
      spawnTimeHour: 0,
      durationHoursRemaining: 0,
      distanceFromBoundaryMeters: 25,
      listings: []
    };
  }

  /**
   * Tính giá bán heo chính thức (Chương 4.5)
   */
  public calculatePigSalePrice(pig: PigData, farmLevel: number): { priceGold: number; successChanceWithoutQuarantine: number } {
    // Dị Biến không bán được ở chợ ngày
    if (pig.rarity === GeneRarity.DiBien) {
      return { priceGold: 0, successChanceWithoutQuarantine: 0 };
    }

    // 1. Hệ số Hạng thịt
    let meatMult = 1.0;
    if (pig.meatQuality === MeatQuality.C) meatMult = 0.8;
    else if (pig.meatQuality === MeatQuality.B) meatMult = 1.0;
    else if (pig.meatQuality === MeatQuality.A) meatMult = 1.2;
    else if (pig.meatQuality === MeatQuality.S) meatMult = 1.5;

    // 2. Hệ số Giống
    let breedMult = 1.0;
    let noQuarantineRate = 0.70; // -30% nếu chưa kiểm dịch

    if (pig.isIdentified) {
      if (pig.rarity === GeneRarity.Kha) { breedMult = 1.10; noQuarantineRate = 0.65; }
      else if (pig.rarity === GeneRarity.Hiem) { breedMult = 1.30; noQuarantineRate = 0.55; }
      else if (pig.rarity === GeneRarity.Quy) { breedMult = 1.60; noQuarantineRate = 0.45; }
      else if (pig.rarity === GeneRarity.HuyenThoai) { breedMult = 2.00; noQuarantineRate = 0.35; }
    }

    // 3. Hệ số Giai đoạn
    const isRareOrAbove = pig.isIdentified && (
      pig.rarity === GeneRarity.Hiem ||
      pig.rarity === GeneRarity.Quy ||
      pig.rarity === GeneRarity.HuyenThoai
    );

    let stageMult = 1.0;
    let baseRefPrice = pig.weightKg * 3.0; // 3G/kg

    if (pig.stage === PigStage.HeoNon) {
      stageMult = isRareOrAbove ? 1.0 : 0.35;
      baseRefPrice = 315; // Heo giống tham chiếu
    } else if (pig.stage === PigStage.DangLon) {
      stageMult = isRareOrAbove ? 1.0 : 0.55;
      baseRefPrice = 315;
    } else if (pig.stage === PigStage.TruongThanh) {
      stageMult = (pig.weightKg >= 95 && pig.weightKg <= 115) ? 1.0 : 0.85;
    } else if (pig.stage === PigStage.HeoGia) {
      stageMult = 0.25; // 0.15 - 0.30
    }

    // 4. Giảm giá do vượt quá 50% sức mua trong ngày
    let purchasingPressureMult = 1.0;
    const halfCap = this.dayMarket.dailyPurchasingPowerCap * 0.5;
    if (this.dayMarket.currentPigsSoldToday > halfCap) {
      const extraPigs = this.dayMarket.currentPigsSoldToday - halfCap;
      const penalty = Math.min(0.25, extraPigs * 0.005); // -0.5% mỗi con, max -25%
      purchasingPressureMult = 1.0 - penalty;
    }

    const price = Math.round(
      baseRefPrice *
      meatMult *
      stageMult *
      breedMult *
      this.dayMarket.themePriceMultiplier *
      purchasingPressureMult
    );

    return {
      priceGold: Math.max(10, price),
      successChanceWithoutQuarantine: noQuarantineRate
    };
  }

  /**
   * Tạo phiên Chợ Đêm với cơ chế trao đổi và 18% ô nhận vàng (Chương 20)
   */
  public generateNightMarket(farmLevel: number): void {
    this.nightListings = [];
    const totalSlots = 8;

    const sampleItems: GameItem[] = [
      { id: 'TraLaVong', name: 'Trà Lá Vông', tier: ItemTier.I, referenceValueGold: 650, category: 'Consumable', description: 'Ngủ ngắn 3h' },
      { id: 'ThuocAnThan', name: 'Thuốc An Thần', tier: ItemTier.II, referenceValueGold: 1400, category: 'Consumable', description: 'Ngủ chuẩn xóa cooldown' },
      { id: 'BinhHoiLuc', name: 'Bình Hồi Lực', tier: ItemTier.I, referenceValueGold: 90, category: 'Consumable', description: 'Hoàn 40 Stamina' },
      { id: 'BanVeNoXuyenVan', name: 'Bản vẽ Nỏ Xuyên Vân', tier: ItemTier.II, referenceValueGold: 2400, category: 'Blueprint', description: 'Bản vẽ công trình tấn công' },
      { id: 'LoiSet', name: 'Lõi Sét Đài Thiên Lôi', tier: ItemTier.III, referenceValueGold: 900, category: 'Material', description: 'Nhiên liệu Đài Thiên Lôi' },
      { id: 'DauEstropic', name: 'Dầu Estropic (Thiên Nhãn)', tier: ItemTier.III, referenceValueGold: 840, category: 'Material', description: 'Nhiên liệu Thiên Nhãn Vọng Đài' }
    ];

    for (let i = 0; i < totalSlots; i++) {
      const item = sampleItems[i % sampleItems.length];
      
      // Quy định 18% ô nhận vàng
      const acceptsGold = Math.random() < 0.18;
      const goldPrice = acceptsGold ? Math.round(item.referenceValueGold * 1.4) : undefined;

      this.nightListings.push({
        slotId: `slot_${i + 1}`,
        offeredItem: item,
        quantity: 1,
        requestedItemDescription: acceptsGold 
          ? `Chấp nhận Vàng hoặc trao đổi ${item.category}` 
          : `Đòi hỏi: Vật phẩm nhóm ${item.category} hoặc Heo đạt GTTC`,
        requestedCategoryId: item.category,
        minimumReferenceValue: item.referenceValueGold,
        acceptsGold,
        goldPrice,
        feePercentage: 0.05
      });
    }
  }

  /**
   * Tạo sự xuất hiện của Bí Nhân (Chương 21)
   * 0% vàng, tồn tại 2 giờ game, đòi 130-200% GTTC, ÁLSK +2..+10
   */
  public rollMysticMerchant(currentHour: number, eventPressure: number): boolean {
    // Chỉ xuất hiện trong đêm (18:00 - 04:00)
    const isNight = currentHour >= 18 || currentHour < 4;
    if (!isNight) {
      this.mysticMerchant.isActive = false;
      return false;
    }

    // Tỉ lệ xuất hiện 3%/đêm * (1 + ÁLSK/100)
    const chance = Math.min(0.25, 0.03 * (1 + eventPressure / 100));
    if (Math.random() < chance) {
      this.mysticMerchant.isActive = true;
      this.mysticMerchant.spawnTimeHour = currentHour;
      this.mysticMerchant.durationHoursRemaining = 2; // Tồn tại đúng 2 giờ game
      this.mysticMerchant.distanceFromBoundaryMeters = 20 + Math.floor(Math.random() * 10);
      
      this.mysticMerchant.listings = [
        {
          id: 'mystic_1',
          offeredItem: {
            id: 'HoSoGiongLoai',
            name: 'Hồ Sơ Giống Loài (Định danh 100%)',
            tier: ItemTier.III,
            referenceValueGold: 3500,
            category: 'Special',
            description: 'Định danh vĩnh viễn 1 dòng gen hiếm'
          },
          requestedPriceMultiplier: 1.6, // Đòi 160% GTTC
          eventPressureDelta: 6
        },
        {
          id: 'mystic_2',
          offeredItem: {
            id: 'QuaMong',
            name: 'Quả Mộng (Ngủ ngay không tác dụng phụ)',
            tier: ItemTier.IV,
            referenceValueGold: 5000,
            category: 'Consumable',
            description: 'Vật phẩm ngủ tối thượng'
          },
          requestedPriceMultiplier: 1.8, // Đòi 180% GTTC
          eventPressureDelta: 8
        }
      ];
      return true;
    }

    return false;
  }

  public getDayMarket(): DayMarketState { return this.dayMarket; }
  public getNightListings(): NightMarketListing[] { return this.nightListings; }
  public getMysticMerchant(): MysticMerchantState { return this.mysticMerchant; }
}
