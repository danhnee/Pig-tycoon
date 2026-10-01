import { HabitatTier, EventPressureState } from '../types/enums.ts';
import { type FarmCapacityReport, type FarmInfrastructure, type FarmState, type CapacityBottleneck } from '../types/farm.ts';

export class Farm {
  private state: type FarmState;

  constructor(initialState?: Partial<type FarmState>) {
    const defaultInfrastructure: type FarmInfrastructure = {
      landPlots: 12,      // 12 ô * 8 = 96
      shelters: 14,       // 14 mái * 6 = 84
      waterTroughs: 8,    // 8 bồn * 12 = 96
      feeders: 9,         // 9 máng * 10 = 90
      processingLotCapacity: 4,
      drainageSystemLevel: 0
    };

    this.state = {
      farmLevel: initialState?.farmLevel ?? 1,
      farmExp: initialState?.farmExp ?? 0,
      infrastructure: initialState?.infrastructure ?? defaultInfrastructure,
      habitatIndex: initialState?.habitatIndex ?? 75, // Ổn Định
      habitatTier: HabitatTier.OnDinh,
      eventPressureIndex: initialState?.eventPressureIndex ?? 10, // Yên ả
      eventPressureState: EventPressureState.YenA,
      gold: initialState?.gold ?? 2500,
      feedInventoryKg: initialState?.feedInventoryKg ?? {
        standard: 150,
        growth: 50,
        premium: 0,
        lingzhi: 0,
        haHoa: 0
      },
      fuelLiters: initialState?.fuelLiters ?? 40,
      powerGrid: initialState?.powerGrid ?? {
        currentCapacityUnits: 30,
        peakCapacityUnits: 60,
        currentLoadUnits: 10,
        capacitorStorageUnits: 0
      },
      corpseLotCount: initialState?.corpseLotCount ?? 0
    };

    this.updateHabitatTier();
    this.updateEventPressureState();
  }

  /**
   * Tính toán sức chứa & mật độ theo công thức chính thức GDD v6.0
   */
  public calculateCapacityReport(livingPigCount: number, corpsesOutsideLot: number): type FarmCapacityReport {
    const infra = this.state.infrastructure;
    const capacityByLand = infra.landPlots * 8;
    const capacityByShelters = infra.shelters * 6;
    const capacityByWater = infra.waterTroughs * 12;
    const capacityByFeeders = infra.feeders * 10;

    const baseCapacity = Math.min(
      capacityByLand,
      capacityByShelters,
      capacityByWater,
      capacityByFeeders
    );

    let bottleneck: type CapacityBottleneck = 'landPlots';
    if (baseCapacity === capacityByShelters) bottleneck = 'shelters';
    else if (baseCapacity === capacityByFeeders) bottleneck = 'feeders';
    else if (baseCapacity === capacityByWater) bottleneck = 'waterTroughs';

    // Sức chứa hiệu dụng = MAX(1 ; Sức chứa gốc - Xác nằm ngoài Khu Xử Lý)
    const effectiveCapacity = Math.max(1, baseCapacity - corpsesOutsideLot);
    const rawDensityRatio = livingPigCount / effectiveCapacity;

    // Tính hệ số dịch nội suy mềm
    const diseaseDensityMultiplier = this.calculateSoftInterpolatedDensity(rawDensityRatio);

    // Xác định tên vùng mật độ
    let densityZoneName = 'Vận hành kinh tế (71–100%)';
    if (rawDensityRatio <= 0.70) densityZoneName = 'Dư sức chứa (≤ 70%)';
    else if (rawDensityRatio <= 1.00) densityZoneName = 'Vận hành kinh tế (71–100%)';
    else if (rawDensityRatio <= 1.20) densityZoneName = 'Quá tải nhẹ (101–120%)';
    else if (rawDensityRatio <= 1.50) densityZoneName = 'Áp lực hệ thống (121–150%)';
    else densityZoneName = 'Khủng hoảng (> 150%)';

    // Phí duy trì hạ tầng cố định mỗi ngày
    const dailyEmptyMaintenanceCost = 
      infra.landPlots * 4 + 
      infra.shelters * 6 + 
      infra.waterTroughs * 3 + 
      infra.feeders * 5;

    return {
      capacityByLand,
      capacityByShelters,
      capacityByWater,
      capacityByFeeders,
      bottleneck,
      baseCapacity,
      unprocessedCorpsesOutsideLot: corpsesOutsideLot,
      effectiveCapacity,
      livingPigCount,
      rawDensityRatio,
      densityZoneName,
      diseaseDensityMultiplier,
      dailyEmptyMaintenanceCost
    };
  }

  /**
   * Nội suy mềm hệ số dịch từ mật độ đàn (Chương 3.3)
   * 70% -> x0.85
   * 100% -> x1.00
   * 120% -> x1.25
   * 150% -> x1.65
   * 180%+ -> x2.20 (trần)
   */
  public calculateSoftInterpolatedDensity(density: number): number {
    if (density <= 0.70) return 0.85;
    if (density <= 1.00) {
      return 0.85 + (1.00 - 0.85) * ((density - 0.70) / (1.00 - 0.70));
    }
    if (density <= 1.20) {
      return 1.00 + (1.25 - 1.00) * ((density - 1.00) / (1.20 - 1.00));
    }
    if (density <= 1.50) {
      return 1.25 + (1.65 - 1.25) * ((density - 1.20) / (1.50 - 1.20));
    }
    // > 1.50 đến 1.80+
    const t = Math.min(1.0, (density - 1.50) / (1.80 - 1.50));
    return 1.65 + (2.20 - 1.65) * t;
  }

  /**
   * Cập nhật chỉ số Sinh Cảnh (SC 0 - 100) hàng ngày
   */
  public updateDailyHabitat(
    cleaningsPerformed: number,
    corpsesOutsideLot: number,
    hasActiveDeseaseClusters: number,
    densityRatio: number,
    hasGoodPasture: boolean,
    hasLeaderWithoutCorpses: boolean
  ): void {
    let delta = 0;

    // Nguồn tăng
    delta += Math.min(12, cleaningsPerformed * 4); // Max +12/ngày
    if (hasGoodPasture) delta += 1;
    if (hasLeaderWithoutCorpses && corpsesOutsideLot === 0) delta += 1;
    if (this.state.infrastructure.drainageSystemLevel > 0) delta += 1;

    // Nguồn giảm
    delta -= (corpsesOutsideLot * 3);
    delta -= (this.state.corpseLotCount * 1);
    delta -= hasActiveDeseaseClusters;

    if (densityRatio > 1.50) delta -= 4;
    else if (densityRatio > 1.20) delta -= 2;
    else if (densityRatio > 1.00) delta -= 1;

    // Trôi tự nhiên về 60 nếu delta = 0
    if (delta === 0) {
      if (this.state.habitatIndex > 60) delta = -1;
      else if (this.state.habitatIndex < 60) delta = 1;
    }

    this.state.habitatIndex = Math.max(0, Math.min(100, this.state.habitatIndex + delta));
    this.updateHabitatTier();
  }

  private updateHabitatTier(): void {
    const sc = this.state.habitatIndex;
    if (sc >= 90) this.state.habitatTier = HabitatTier.TrongLanh;
    else if (sc >= 70) this.state.habitatTier = HabitatTier.OnDinh;
    else if (sc >= 50) this.state.habitatTier = HabitatTier.TrungTinh;
    else if (sc >= 30) this.state.habitatTier = HabitatTier.ONhiem;
    else this.state.habitatTier = HabitatTier.OUe;
  }

  /**
   * Cập nhật Thang Áp Lực Sự Kiện (ÁLSK 0 - 100)
   */
  public updateDailyEventPressure(
    elderPigsCount: number,
    totalPigsCount: number,
    densityRatio: number,
    corpsesOutsideLot: number
  ): void {
    let delta = 0;

    // +Thấp (+1): mỗi 10 heo già vượt ngưỡng mềm (15% đàn)
    const softElderThreshold = Math.floor(totalPigsCount * 0.15);
    const excessElder = Math.max(0, elderPigsCount - softElderThreshold);
    const elderPressure = Math.floor(excessElder / 10);
    delta += elderPressure;

    // +Thấp (+1): mỗi xác trong ô tập kết
    delta += this.state.corpseLotCount;

    // +Vừa (+3): mật độ 121–150%, SC 30–49
    if (densityRatio > 1.20 && densityRatio <= 1.50) delta += 3;
    if (this.state.habitatIndex >= 30 && this.state.habitatIndex < 50) delta += 3;

    // Mỗi xác ngoài Khu Xử Lý: +Vừa (+3/xác, tối đa +12)
    delta += Math.min(12, corpsesOutsideLot * 3);

    // +Cao (+6): mật độ > 150%, SC < 30
    if (densityRatio > 1.50) delta += 6;
    if (this.state.habitatIndex < 30) delta += 6;

    // Nếu không có nguồn gây áp lực mới: suy giảm tự nhiên -8%/ngày
    if (delta === 0 && this.state.eventPressureIndex > 0) {
      this.state.eventPressureIndex = Math.max(0, Math.floor(this.state.eventPressureIndex * 0.92));
    } else {
      this.state.eventPressureIndex = Math.max(0, Math.min(100, this.state.eventPressureIndex + delta));
    }

    this.updateEventPressureState();
  }

  public addInstantEventPressure(amount: number): void {
    this.state.eventPressureIndex = Math.max(0, Math.min(100, this.state.eventPressureIndex + amount));
    this.updateEventPressureState();
  }

  private updateEventPressureState(): void {
    const val = this.state.eventPressureIndex;
    if (val >= 80) this.state.eventPressureState = EventPressureState.RanNut;
    else if (val >= 50) this.state.eventPressureState = EventPressureState.BatOn;
    else if (val >= 20) this.state.eventPressureState = EventPressureState.GonSong;
    else this.state.eventPressureState = EventPressureState.YenA;
  }

  // Getters & Setters
  public getState(): type FarmState { return this.state; }
  public getGold(): number { return this.state.gold; }
  public addGold(amount: number): void { this.state.gold += amount; }
  public spendGold(amount: number): boolean {
    if (this.state.gold >= amount) {
      this.state.gold -= amount;
      return true;
    }
    return false;
  }
  public getHabitatIndex(): number { return this.state.habitatIndex; }
  public getHabitatTier(): HabitatTier { return this.state.habitatTier; }
  public getEventPressureIndex(): number { return this.state.eventPressureIndex; }
  public getEventPressureState(): EventPressureState { return this.state.eventPressureState; }
  public getFarmLevel(): number { return this.state.farmLevel; }
  public addFarmExp(exp: number): void {
    this.state.farmExp += exp;
    // Cấp 1-20
    const nextLevelThreshold = this.state.farmLevel * 1500;
    if (this.state.farmExp >= nextLevelThreshold && this.state.farmLevel < 20) {
      this.state.farmLevel += 1;
    }
  }
}
