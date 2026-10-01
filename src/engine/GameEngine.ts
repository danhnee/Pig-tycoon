import { GameClock } from '../models/GameClock.ts';
import { Farm } from '../models/Farm.ts';
import { Pig } from '../models/Pig.ts';
import { HerdManager } from '../models/HerdManager.ts';
import { EconomyManager } from '../models/EconomyManager.ts';
import { DefenseManager } from '../models/DefenseManager.ts';
import { WeatherType, TimeOfDay, GeneLineId, GeneRarity, HerdStateTier, CombatSlotType, MartialSchool, PigStage } from '../types/enums.ts';
import { type CharacterData } from '../types/character.ts';
import { type GameSaveState } from '../types/save.ts';

export class GameEngine {
  public clock: GameClock;
  public farm: Farm;
  public herd: HerdManager;
  public economy: EconomyManager;
  public defense: DefenseManager;
  public character: type CharacterData;
  public pigs: Pig[];

  public currentWeather: WeatherType;
  public weatherDaysLeft: number;
  public nextWeatherForecast: WeatherType;

  public majorWavesSurvived: number;
  public minorWavesDefeated: number;
  public bossesDefeated: number;
  public totalPigsSold: number;
  public totalGoldEarned: number;

  constructor() {
    this.clock = new GameClock(1, 6, 0);
    this.farm = new Farm();
    this.herd = new HerdManager();
    this.economy = new EconomyManager();
    this.defense = new DefenseManager();
    this.pigs = [];

    this.currentWeather = WeatherType.QuangDang;
    this.weatherDaysLeft = 2;
    this.nextWeatherForecast = WeatherType.NhieuMay;

    this.majorWavesSurvived = 0;
    this.minorWavesDefeated = 0;
    this.bossesDefeated = 0;
    this.totalPigsSold = 0;
    this.totalGoldEarned = 0;

    this.character = this.initCharacter('Khoa');
  }

  private initCharacter(name: string): type CharacterData {
    return {
      id: 'char_1',
      name,
      level: 1,
      exp: 0,
      baseStats: { str: 24, agi: 18, ctrl: 18, res: 22 },
      bonusStats: { str: 0, agi: 0, ctrl: 0, res: 0 },
      hp: 100,
      maxHp: 100,
      rage: 0,
      stamina: {
        currentStamina: 180,
        maxStamina: 180,
        dailyLoad: 0,
        condition: 100,
        immunity: 100,
        reboundFatigueStacks: 0
      },
      equipment: {
        Weapon: null,
        Boots: null,
        BodyArmor: null,
        Pants: null,
        Bracelet: null,
        Necklace: null
      },
      currentPlaystyle: 'KhinhThan',
      primarySchool: MartialSchool.TrongKich_B,
      activeMartialSkills: {
        [CombatSlotType.TheCong]: null,
        [CombatSlotType.TheThu]: null,
        [CombatSlotType.TheBien]: null
      },
      learnedSkills: []
    };
  }

  /**
   * Bước tiến thời gian chính (Tick)
   */
  public tick(minutes = 1): {
    timeOfDay: TimeOfDay;
    newDayStarted: boolean;
    isMajorWaveDay: boolean;
    isMinorWaveTriggered: boolean;
    mysticMerchantAppeared: boolean;
    logs: string[];
  } {
    const logs: string[] = [];
    const tickResult = this.clock.tickMinutes(minutes);

    // Kiểm tra xuất hiện Bí nhân vào ban đêm
    let mysticMerchantAppeared = false;
    if (this.clock.getTimeOfDay() === TimeOfDay.Toi && this.clock.getMinute() === 0) {
      const appeared = this.economy.rollMysticMerchant(this.clock.getHour(), this.farm.getEventPressureIndex());
      if (appeared) {
        mysticMerchantAppeared = true;
        logs.push('CẢNH BÁO: Bí Nhân nửa người nửa quỷ đã xuất hiện sát ranh giới trang trại với đèn lồng xanh!');
      }
    }

    // Nếu bước sang ngày mới (06:00)
    if (tickResult.newDayStarted) {
      this.onDailyReset(logs);
    }

    return {
      timeOfDay: tickResult.currentTimeOfDay,
      newDayStarted: tickResult.newDayStarted,
      isMajorWaveDay: tickResult.isMajorWaveDay,
      isMinorWaveTriggered: tickResult.isMinorWaveTriggered,
      mysticMerchantAppeared,
      logs
    };
  }

  /**
   * Xử lý chu kỳ ngày mới lúc 06:00 sáng
   */
  private onDailyReset(logs: string[]): void {
    logs.push(`=== BẮT ĐẦU NGÀY THỨ ${this.clock.getDay()} (Chu kỳ ${this.clock.getCycleNumber()} - Ngày ${this.clock.getCycleDayIndex()}/${this.clock.getCycleTotalDays()}) ===`);

    const livingPigs = this.pigs.filter(p => p.getData().isAlive);
    const corpsesOutside = this.pigs.filter(p => !p.getData().isAlive && !p.getData().isInProcessingLot).length;
    const capacityReport = this.farm.calculateCapacityReport(livingPigs.length, corpsesOutside);

    // 1. Cập nhật đàn heo
    const isInHerd = this.herd.getTier() !== HerdStateTier.KhongCo;
    const densityPenalty = capacityReport.rawDensityRatio > 1.2 ? 0.08 : (capacityReport.rawDensityRatio > 1.5 ? 0.15 : 0);
    
    this.pigs.forEach(p => {
      p.advanceDay(this.farm.getHabitatIndex(), isInHerd, densityPenalty);
    });

    // 2. Trừ phí duy trì hạ tầng
    this.farm.spendGold(capacityReport.dailyEmptyMaintenanceCost);
    logs.push(`Đã chi trả ${capacityReport.dailyEmptyMaintenanceCost}G phí duy trì hạ tầng chuồng trại.`);

    // 3. Cập nhật Sinh Cảnh (SC) và Áp Lực Sự Kiện (ÁLSK)
    this.farm.updateDailyHabitat(1, corpsesOutside, 0, capacityReport.rawDensityRatio, true, true);
    
    const elderPigs = livingPigs.filter(p => p.getData().stage === PigStage.HeoGia).length;
    this.farm.updateDailyEventPressure(elderPigs, livingPigs.length, capacityReport.rawDensityRatio, corpsesOutside);

    // 4. Đánh giá Bầy Đàn & Kế thừa
    const herdResult = this.herd.advanceDay(this.pigs, this.farm, this.majorWavesSurvived);
    if (herdResult.tierChanged && herdResult.message) {
      logs.push(herdResult.message);
    }

    // 5. Làm mới chợ ngày & chợ đêm
    this.economy.getDayMarket().currentPigsSoldToday = 0;
    this.economy.generateNightMarket(this.farm.getFarmLevel());

    // 6. Xóa Tải ngày của nhân vật nếu ngủ đầy đủ
    this.character.stamina.dailyLoad = 0;
    this.character.stamina.currentStamina = this.character.stamina.maxStamina;
  }

  /**
   * Bán heo tại Chợ Ngày
   */
  public sellPigAtDayMarket(pigId: string, isQuarantined = true): { success: boolean; goldEarned: number; message: string } {
    const pig = this.pigs.find(p => p.getData().id === pigId);
    if (!pig || !pig.getData().isAlive) {
      return { success: false, goldEarned: 0, message: 'Heo không tồn tại hoặc đã chết!' };
    }

    const { priceGold, successChanceWithoutQuarantine } = this.economy.calculatePigSalePrice(
      pig.getData(),
      this.farm.getFarmLevel()
    );

    if (priceGold <= 0) {
      return { success: false, goldEarned: 0, message: 'Không thể bán heo này ở chợ ngày (Dị Biến phải đổi ở chợ đêm)!' };
    }

    if (!isQuarantined && Math.random() > successChanceWithoutQuarantine) {
      return { success: false, goldEarned: 0, message: 'Người mua phát hiện heo chưa qua kiểm dịch và từ chối mua!' };
    }

    // Trừ phí kiểm dịch nếu chọn kiểm dịch
    let finalGold = priceGold;
    if (isQuarantined) {
      finalGold -= this.economy.getDayMarket().quarantineFeePerPig;
    }

    // Xóa heo khỏi đàn
    this.pigs = this.pigs.filter(p => p.getData().id !== pigId);
    this.farm.addGold(finalGold);
    this.farm.addFarmExp(Math.floor(finalGold / 100));
    this.totalPigsSold += 1;
    this.totalGoldEarned += finalGold;
    this.economy.getDayMarket().currentPigsSoldToday += 1;

    return {
      success: true,
      goldEarned: finalGold,
      message: `Đã bán thành công heo ${pig.getData().name} thu về ${finalGold}G (Giá niêm yết: ${priceGold}G).`
    };
  }

  /**
   * Tạo Save State hoàn chỉnh
   */
  public exportSaveState(saveName = 'ManualSave'): type GameSaveState {
    return {
      version: '6.0.0',
      savedAtTimestamp: Date.now(),
      saveName,
      clock: this.clock.exportSaveState(),
      weather: {
        currentWeather: this.currentWeather,
        durationDaysRemaining: this.weatherDaysLeft,
        nextWeatherForecast: this.nextWeatherForecast
      },
      farm: this.farm.getState(),
      pigs: this.pigs.map(p => p.getData()),
      herd: this.herd.getState(),
      market: {
        dayMarket: this.economy.getDayMarket(),
        nightListings: this.economy.getNightListings(),
        mysticMerchant: this.economy.getMysticMerchant()
      },
      defense: {
        hall: this.defense.getHall(),
        buildings: this.defense.getBuildings()
      },
      character: this.character,
      statistics: {
        majorWavesSurvived: this.majorWavesSurvived,
        minorWavesDefeated: this.minorWavesDefeated,
        bossesDefeated: this.bossesDefeated,
        totalPigsSold: this.totalPigsSold,
        totalGoldEarned: this.totalGoldEarned,
        discoveredGeneLines: [GeneLineId.HongDien, GeneLineId.LamKhe, GeneLineId.MocCuoc]
      }
    };
  }

  /**
   * Nạp Save State
   */
  public importSaveState(saveState: type GameSaveState): void {
    this.clock.loadSaveState(saveState.clock);
    this.farm = new Farm(saveState.farm);
    this.herd = new HerdManager(saveState.herd);
    this.defense = new DefenseManager(saveState.defense.hall);
    this.pigs = saveState.pigs.map(data => new Pig(data));
    this.character = saveState.character;

    this.currentWeather = saveState.weather.currentWeather;
    this.weatherDaysLeft = saveState.weather.durationDaysRemaining;
    this.nextWeatherForecast = saveState.weather.nextWeatherForecast;

    this.majorWavesSurvived = saveState.statistics.majorWavesSurvived;
    this.minorWavesDefeated = saveState.statistics.minorWavesDefeated;
    this.bossesDefeated = saveState.statistics.bossesDefeated;
    this.totalPigsSold = saveState.statistics.totalPigsSold;
    this.totalGoldEarned = saveState.statistics.totalGoldEarned;
  }
}
