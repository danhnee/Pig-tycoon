import test from 'node:test';
import assert from 'node:assert';
import { HerdManager } from '../models/HerdManager.ts';
import { Farm } from '../models/Farm.ts';
import { Pig } from '../models/Pig.ts';
import { GeneLineId, GeneRarity, HerdStateTier, PigStage } from '../types/enums.ts';

test('HerdManager - 7 conditions for Trạng thái Bầy Đàn', () => {
  const herd = new HerdManager();
  
  // Trại thỏa mãn cấp và đợt lớn
  const farm = new Farm({
    farmLevel: 6,
    habitatIndex: 75, // SC >= 50
    infrastructure: {
      landPlots: 20,      // 160
      shelters: 25,       // 150
      waterTroughs: 15,   // 180
      feeders: 15,        // 150
      processingLotCapacity: 4,
      drainageSystemLevel: 0
    }
  });

  const pigs: Pig[] = [];

  // Tạo 95 heo thường trưởng thành, gắn kết (Hòa nhập = 60)
  for (let i = 0; i < 95; i++) {
    const p = Pig.createDefault(`pig_${i}`, `Heo_${i}`, GeneLineId.HongDien, GeneRarity.Thuong);
    p.getData().bonding = 60;
    p.getData().stage = PigStage.TruongThanh;
    pigs.push(p);
  }

  // Thêm 2 con Kim Thọ trưởng thành (mỗi con tính = 3 con gắn kết -> 95 + 6 = 101)
  for (let i = 0; i < 2; i++) {
    const kt = Pig.createDefault(`kt_${i}`, `KimTho_${i}`, GeneLineId.KimTho, GeneRarity.HuyenThoai);
    kt.getData().bonding = 60;
    kt.getData().stage = PigStage.TruongThanh;
    pigs.push(kt);
  }

  // Thêm 6 heo già có Hòa nhập >= 65
  for (let i = 0; i < 6; i++) {
    const elder = Pig.createDefault(`elder_${i}`, `Elder_${i}`, GeneLineId.ThoTram, GeneRarity.Kha);
    elder.getData().stage = PigStage.HeoGia;
    elder.getData().bonding = 70;
    pigs.push(elder);
  }

  // Tổng heo: 95 + 2 + 6 = 103 con sống
  // Sức chứa hiệu dụng = 150 (theo máng và mái)
  // Mật độ = 103 / 150 = 68.6% -> Chưa đạt điều kiện 4 (cần 71 - 110%)!
  let report = herd.evaluateConditions(pigs, farm, 4);
  assert.strictEqual(report.condition1_Met, true); // 101 + 6 = 107 con gắn kết >= 100
  assert.strictEqual(report.condition4_Met, false); // Mật độ 68% < 71%
  assert.strictEqual(report.allConditionsMet, false);

  // Thêm 10 heo để mật độ lên 113 / 150 = 75.3% (trong khoảng 71 - 110%)
  for (let i = 0; i < 10; i++) {
    const extra = Pig.createDefault(`extra_${i}`, `Extra_${i}`, GeneLineId.HongDien, GeneRarity.Thuong);
    extra.getData().bonding = 60;
    pigs.push(extra);
  }

  report = herd.evaluateConditions(pigs, farm, 4);
  assert.strictEqual(report.condition4_Met, true);
  assert.strictEqual(report.condition5_Met, true); // >= 6 heo già hòa nhập >= 65
  assert.strictEqual(report.condition7_Met, true); // Cấp 6, 4 đợt lớn
  assert.strictEqual(report.allConditionsMet, true);

  // Thử nghiệm Invariant: SC < 50 không bao giờ lập được bầy
  farm.getState().habitatIndex = 45;
  const reportLowSC = herd.evaluateConditions(pigs, farm, 4);
  assert.strictEqual(reportLowSC.condition2_Met, false);
  assert.strictEqual(reportLowSC.allConditionsMet, false);
});

test('HerdManager - 3 consecutive days requirement for Sơ Khai', () => {
  const herd = new HerdManager();
  const farm = new Farm({ farmLevel: 6, habitatIndex: 75 });
  
  // Thiết lập đàn đủ điều kiện
  const pigs: Pig[] = [];
  for (let i = 0; i < 110; i++) {
    const p = Pig.createDefault(`p_${i}`, `P_${i}`, GeneLineId.HongDien, GeneRarity.Thuong);
    p.getData().bonding = 60;
    pigs.push(p);
  }
  for (let i = 0; i < 6; i++) {
    const e = Pig.createDefault(`e_${i}`, `E_${i}`, GeneLineId.ThoTram, GeneRarity.Kha);
    e.getData().stage = PigStage.HeoGia;
    e.getData().bonding = 70;
    pigs.push(e);
  }

  // Ngày 1: Đạt điều kiện nhưng chưa đủ 3 ngày
  herd.advanceDay(pigs, farm, 4);
  assert.strictEqual(herd.getTier(), HerdStateTier.KhongCo);

  // Ngày 2: Đạt điều kiện
  herd.advanceDay(pigs, farm, 4);
  assert.strictEqual(herd.getTier(), HerdStateTier.KhongCo);

  // Ngày 3: Đủ 3 ngày liên tục -> Thành lập Bầy Đàn Sơ Khai!
  const resDay3 = herd.advanceDay(pigs, farm, 4);
  assert.strictEqual(herd.getTier(), HerdStateTier.SoKhai);
  assert.strictEqual(resDay3.tierChanged, true);
});
