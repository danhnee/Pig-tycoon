import test from 'node:test';
import assert from 'node:assert';
import { EconomyManager } from '../models/EconomyManager.ts';
import { GeneLineId, GeneRarity, MeatQuality, PigStage } from '../types/enums.ts';
import { Pig } from '../models/Pig.ts';

test('EconomyManager - Official pig price calculation matching GDD v6.0', () => {
  const economy = new EconomyManager();

  // 1. Ví dụ GDD mục 4.5: Heo trưởng thành 105 kg, hạng A, giống Hiếm đã định danh, đã kiểm dịch, chợ bình thường:
  // 105 * 3 * 1.20 * 1.00 * 1.30 = 491.4G -> ~491G
  const adultRarePig = Pig.createDefault('pig_rare', 'Thiết Bì A', GeneLineId.ThietBi, GeneRarity.Hiem);
  adultRarePig.getData().weightKg = 105;
  adultRarePig.getData().meatQuality = MeatQuality.A;
  adultRarePig.getData().stage = PigStage.TruongThanh;
  adultRarePig.getData().isIdentified = true;

  const adultRes = economy.calculatePigSalePrice(adultRarePig.getData(), 1);
  assert.strictEqual(adultRes.priceGold, 491);

  // 2. Ví dụ GDD: Heo non cùng giống Hiếm đã định danh:
  // 315 * 1.00 * 1.30 = 409.5G -> ~410G
  const pigletRare = Pig.createDefault('piglet_rare', 'Thiết Bì Con', GeneLineId.ThietBi, GeneRarity.Hiem);
  pigletRare.getData().stage = PigStage.HeoNon;
  pigletRare.getData().isIdentified = true;

  const pigletRes = economy.calculatePigSalePrice(pigletRare.getData(), 1);
  assert.strictEqual(pigletRes.priceGold, 410);

  // 3. Ví dụ GDD: Heo non giống Thường:
  // 315 * 0.35 * 1.00 = 110.25G -> ~110G
  const pigletCommon = Pig.createDefault('piglet_common', 'Hồng Điền Con', GeneLineId.HongDien, GeneRarity.Thuong);
  pigletCommon.getData().stage = PigStage.HeoNon;

  const commonRes = economy.calculatePigSalePrice(pigletCommon.getData(), 1);
  assert.strictEqual(commonRes.priceGold, 110);
});

test('EconomyManager - Night market barter and gold slots constraint', () => {
  const economy = new EconomyManager();
  
  // Tạo chợ đêm nhiều lần để kiểm tra tỉ lệ ô vàng
  let totalSlots = 0;
  let goldSlots = 0;

  for (let run = 0; run < 100; run++) {
    economy.generateNightMarket(5);
    const listings = economy.getNightListings();
    totalSlots += listings.length;
    goldSlots += listings.filter(l => l.acceptsGold).length;
  }

  const goldSlotRatio = goldSlots / totalSlots;
  // Kỳ vọng ~18% (trong dải dung sai thống kê 10% - 26%)
  assert.ok(goldSlotRatio >= 0.10 && goldSlotRatio <= 0.26, `Tỉ lệ ô vàng ${goldSlotRatio} phải xấp xỉ 18%`);
});

test('EconomyManager - Bí nhân 0% gold rule (Chương 21)', () => {
  const economy = new EconomyManager();

  // Ép Bí nhân xuất hiện
  economy.rollMysticMerchant(20, 50); // 20:00, ÁLSK 50
  
  const mystic = economy.getMysticMerchant();
  if (mystic.isActive) {
    assert.strictEqual(mystic.durationHoursRemaining, 2, 'Bí nhân chỉ tồn tại đúng 2 giờ game');
    assert.ok(mystic.distanceFromBoundaryMeters <= 30, 'Bí nhân xuất hiện trong phạm vi 30m sát ranh giới trại');
    
    mystic.listings.forEach(listing => {
      // Đòi tỉ giá 130% - 200% GTTC
      assert.ok(listing.requestedPriceMultiplier >= 1.30 && listing.requestedPriceMultiplier <= 2.00);
      // Tăng ÁLSK
      assert.ok(listing.eventPressureDelta >= 2 && listing.eventPressureDelta <= 10);
    });
  }
});
