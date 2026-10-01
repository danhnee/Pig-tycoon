import test from 'node:test';
import assert from 'node:assert';
import { Farm } from '../models/Farm.ts';
import { HabitatTier } from '../types/enums.ts';

test('Farm - Capacity calculation and bottleneck identification', () => {
  const farm = new Farm({
    infrastructure: {
      landPlots: 12,      // 12 * 8 = 96
      shelters: 12,       // 12 * 6 = 72 (Điểm nghẽn)
      waterTroughs: 9,    // 9 * 12 = 108
      feeders: 9,         // 9 * 10 = 90
      processingLotCapacity: 4,
      drainageSystemLevel: 0
    }
  });

  // Có 2 xác heo chưa đưa vào Khu Xử Lý
  const report = farm.calculateCapacityReport(50, 2);

  assert.strictEqual(report.capacityByLand, 96);
  assert.strictEqual(report.capacityByShelters, 72);
  assert.strictEqual(report.bottleneck, 'shelters');
  assert.strictEqual(report.baseCapacity, 72);
  assert.strictEqual(report.effectiveCapacity, 70); // 72 - 2
  assert.strictEqual(report.unprocessedCorpsesOutsideLot, 2);
});

test('Farm - Soft density interpolation formula matches GDD v6.0', () => {
  const farm = new Farm();

  // <= 70% -> x0.85
  assert.strictEqual(farm.calculateSoftInterpolatedDensity(0.50), 0.85);
  assert.strictEqual(farm.calculateSoftInterpolatedDensity(0.70), 0.85);

  // 100% -> x1.00
  assert.strictEqual(farm.calculateSoftInterpolatedDensity(1.00), 1.00);

  // 120% -> x1.25
  assert.strictEqual(farm.calculateSoftInterpolatedDensity(1.20), 1.25);

  // 150% -> x1.65
  assert.strictEqual(farm.calculateSoftInterpolatedDensity(1.50), 1.65);

  // 160% -> nội suy giữa 1.65 và 2.20
  // 1.65 + (0.55 * 10/30) = 1.65 + 0.1833 ≈ 1.833
  const d160 = farm.calculateSoftInterpolatedDensity(1.60);
  assert.ok(Math.abs(d160 - 1.833) < 0.01, `Giá trị ${d160} phải xấp xỉ 1.833`);

  // >= 180% -> trần x2.20
  assert.strictEqual(farm.calculateSoftInterpolatedDensity(1.80), 2.20);
  assert.strictEqual(farm.calculateSoftInterpolatedDensity(2.50), 2.20);
});

test('Farm - Habitat tiers (SC 0 - 100)', () => {
  const farm = new Farm({ habitatIndex: 95 });
  assert.strictEqual(farm.getHabitatTier(), HabitatTier.TrongLanh);

  farm.getState().habitatIndex = 75;
  farm.updateDailyHabitat(0, 0, 0, 1.0, false, false);
  assert.strictEqual(farm.getHabitatTier(), HabitatTier.OnDinh);

  farm.getState().habitatIndex = 25;
  farm.updateDailyHabitat(0, 0, 0, 1.0, false, false);
  assert.strictEqual(farm.getHabitatTier(), HabitatTier.OUe);
});
