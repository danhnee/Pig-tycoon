import { TimeOfDay } from '../types/enums.ts';
import { type ClockSaveState } from '../types/save.ts';

export interface ClockTickResult {
  minutesAdvanced: number;
  newDayStarted: boolean;
  timeOfDayChanged: boolean;
  previousTimeOfDay: TimeOfDay;
  currentTimeOfDay: TimeOfDay;
  isMajorWaveDay: boolean;
  isMinorWaveTriggered: boolean;
}

export class GameClock {
  // Thực tế: 1 ngày game = 16 phút thực (960s).
  // 1 giờ game = 40s thực. 1 phút game = 0.6667s thực.
  public static readonly SECONDS_PER_GAME_MINUTE = 40 / 60;
  public static readonly SECONDS_PER_GAME_HOUR = 40;
  public static readonly SECONDS_PER_GAME_DAY = 960;

  private currentDay: number;
  private currentHour: number;
  private currentMinute: number;
  
  private cycleNumber: number;
  private cycleTotalDays: number;
  private cycleDayIndex: number; // 1 -> cycleTotalDays
  
  private minorWavesRemainingInCycle: number;
  private hoursUntilNextMinorWave: number;
  
  private sleepCooldownHoursRemaining: number;
  private isDuringCombatWave: boolean;

  constructor(initialDay = 1, initialHour = 6, initialMinute = 0) {
    this.currentDay = initialDay;
    this.currentHour = initialHour;
    this.currentMinute = initialMinute;
    this.cycleNumber = 1;
    this.cycleDayIndex = 1;
    this.sleepCooldownHoursRemaining = 0;
    this.isDuringCombatWave = false;
    
    // Gieo chu kỳ 1 (Đầu game: 6 - 8 ngày)
    this.cycleTotalDays = this.rollCycleLength(1, false, false);
    this.minorWavesRemainingInCycle = this.rollMinorWaveCount(1);
    this.hoursUntilNextMinorWave = this.scheduleNextMinorWave();
  }

  public getTimeOfDay(): TimeOfDay {
    if (this.currentHour >= 6 && this.currentHour < 10) {
      return TimeOfDay.Sang;
    } else if (this.currentHour >= 10 && this.currentHour < 14) {
      return TimeOfDay.Trua;
    } else if (this.currentHour >= 14 && this.currentHour < 18) {
      return TimeOfDay.Chieu;
    } else {
      return TimeOfDay.Toi;
    }
  }

  public tickMinutes(minutes = 1): ClockTickResult {
    const prevTimeOfDay = this.getTimeOfDay();
    let newDayStarted = false;
    let isMinorWaveTriggered = false;

    this.currentMinute += minutes;
    while (this.currentMinute >= 60) {
      this.currentMinute -= 60;
      this.currentHour += 1;

      // Giảm cooldown ngủ theo giờ game
      if (this.sleepCooldownHoursRemaining > 0) {
        this.sleepCooldownHoursRemaining = Math.max(0, this.sleepCooldownHoursRemaining - 1);
      }

      // Giảm đếm ngược đợt nhỏ
      if (this.hoursUntilNextMinorWave > 0) {
        this.hoursUntilNextMinorWave -= 1;
        if (this.hoursUntilNextMinorWave === 0 && this.minorWavesRemainingInCycle > 0) {
          isMinorWaveTriggered = true;
          this.minorWavesRemainingInCycle -= 1;
          this.hoursUntilNextMinorWave = this.scheduleNextMinorWave();
        }
      }

      if (this.currentHour >= 24) {
        this.currentHour = 0;
        this.currentDay += 1;
        this.cycleDayIndex += 1;
        newDayStarted = true;
      }
    }

    const curTimeOfDay = this.getTimeOfDay();
    const isMajorWaveDay = (this.cycleDayIndex >= this.cycleTotalDays);

    return {
      minutesAdvanced: minutes,
      newDayStarted,
      timeOfDayChanged: prevTimeOfDay !== curTimeOfDay,
      previousTimeOfDay: prevTimeOfDay,
      currentTimeOfDay: curTimeOfDay,
      isMajorWaveDay,
      isMinorWaveTriggered
    };
  }

  /**
   * Ngủ chuẩn: tua thời gian đến 06:00 sáng hôm sau.
   * Yêu cầu hết cooldown ngủ (6 giờ game) và không bị cấm ngủ.
   */
  public performStandardSleep(bypassCooldown = false): { success: boolean; message: string; hoursSlept: number } {
    if (this.isDuringCombatWave) {
      return { success: false, message: 'Không thể ngủ trong khi có đợt quái tấn công!', hoursSlept: 0 };
    }

    if (!bypassCooldown && this.sleepCooldownHoursRemaining > 0) {
      return { 
        success: false, 
        message: `Chưa thể ngủ! Cooldown còn ${this.sleepCooldownHoursRemaining} giờ game.`, 
        hoursSlept: 0 
      };
    }

    // Tính số giờ ngủ đến 06:00
    let hoursToSleep = 0;
    if (this.currentHour >= 6) {
      hoursToSleep = (24 - this.currentHour) + 6;
    } else {
      hoursToSleep = 6 - this.currentHour;
    }

    // Tua thời gian
    this.tickMinutes(hoursToSleep * 60 - this.currentMinute);
    this.currentMinute = 0;
    this.currentHour = 6;
    this.sleepCooldownHoursRemaining = 6; // Cooldown 6 giờ sau khi thức dậy

    return {
      success: true,
      message: `Đã ngủ ${hoursToSleep} giờ đến 06:00 sáng. Thể trạng và thể lực đã phục hồi.`,
      hoursSlept: hoursToSleep
    };
  }

  /**
   * Hoàn thành Đại Đợt (Major Wave) -> Khởi tạo chu kỳ mới
   */
  public completeMajorWave(hadSevereLosses = false, highEventPressure = false): void {
    this.cycleNumber += 1;
    this.cycleDayIndex = 1;
    this.cycleTotalDays = this.rollCycleLength(this.cycleNumber, hadSevereLosses, highEventPressure);
    this.minorWavesRemainingInCycle = this.rollMinorWaveCount(this.cycleNumber);
    this.hoursUntilNextMinorWave = this.scheduleNextMinorWave();
  }

  private rollCycleLength(cycle: number, hadLosses: boolean, highPressure: boolean): number {
    // 8% cơ hội "Mùa Kinh Tế Vàng" (12 - 15 ngày)
    if (Math.random() < 0.08) {
      return 12 + Math.floor(Math.random() * 4); // 12 - 15
    }

    let min = 6;
    let max = 8;
    if (cycle <= 7) {
      min = 6; max = 8; // Đầu game
    } else if (cycle <= 19) {
      min = 7; max = 10; // Giữa game
    } else {
      min = 8; max = 12; // Cuối game
    }

    let length = min + Math.floor(Math.random() * (max - min + 1));
    if (highPressure) {
      length = Math.max(6, length - 1); // -1 ngày nếu áp lực cao
    }
    if (hadLosses) {
      length += 1; // +1 ngày hồi phục
    }
    return length;
  }

  private rollMinorWaveCount(cycle: number): number {
    if (cycle <= 7) return Math.random() < 0.6 ? 1 : 0; // 0-1
    if (cycle <= 19) return 1 + (Math.random() < 0.5 ? 1 : 0); // 1-2
    return 1 + Math.floor(Math.random() * 3); // 1-3
  }

  private scheduleNextMinorWave(): number {
    // Lên lịch đợt nhỏ ngẫu nhiên trong khoảng 24 đến 72 giờ game
    return 24 + Math.floor(Math.random() * 48);
  }

  public setCombatWaveActive(active: boolean): void {
    this.isDuringCombatWave = active;
  }

  public getDaysUntilMajorWave(): number {
    return Math.max(0, this.cycleTotalDays - this.cycleDayIndex);
  }

  public exportSaveState(): ClockSaveState {
    return {
      currentDay: this.currentDay,
      currentHour: this.currentHour,
      currentMinute: this.currentMinute,
      cycleNumber: this.cycleNumber,
      cycleTotalDays: this.cycleTotalDays,
      cycleDayIndex: this.cycleDayIndex,
      daysUntilMajorWave: this.getDaysUntilMajorWave(),
      minorWavesRemainingInCycle: this.minorWavesRemainingInCycle,
      hoursUntilNextMinorWave: this.hoursUntilNextMinorWave,
      sleepCooldownHoursRemaining: this.sleepCooldownHoursRemaining
    };
  }

  public loadSaveState(state: ClockSaveState): void {
    this.currentDay = state.currentDay;
    this.currentHour = state.currentHour;
    this.currentMinute = state.currentMinute;
    this.cycleNumber = state.cycleNumber;
    this.cycleTotalDays = state.cycleTotalDays;
    this.cycleDayIndex = state.cycleDayIndex;
    this.minorWavesRemainingInCycle = state.minorWavesRemainingInCycle;
    this.hoursUntilNextMinorWave = state.hoursUntilNextMinorWave;
    this.sleepCooldownHoursRemaining = state.sleepCooldownHoursRemaining;
  }

  // Getters
  public getDay(): number { return this.currentDay; }
  public getHour(): number { return this.currentHour; }
  public getMinute(): number { return this.currentMinute; }
  public getCycleNumber(): number { return this.cycleNumber; }
  public getCycleTotalDays(): number { return this.cycleTotalDays; }
  public getCycleDayIndex(): number { return this.cycleDayIndex; }
  public getSleepCooldown(): number { return this.sleepCooldownHoursRemaining; }
}
