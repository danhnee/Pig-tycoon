import { GeneLineId, HerdStateTier, MoodState, PigStage } from '../types/enums.ts';
import { type HerdConditionsReport, type HerdState } from '../types/herd.ts';
import { Pig } from './Pig.ts';
import { Farm } from './Farm.ts';

export class HerdManager {
  private state: type HerdState;
  private daysConditionMetStreak: number;
  private daysConditionFailedStreak: number;

  constructor(initialState?: Partial<type HerdState>) {
    this.daysConditionMetStreak = 0;
    this.daysConditionFailedStreak = 0;

    this.state = {
      tier: initialState?.tier ?? HerdStateTier.KhongCo,
      consecutiveDaysInState: initialState?.consecutiveDaysInState ?? 0,
      consecutiveDaysConditionsFailed: initialState?.consecutiveDaysConditionsFailed ?? 0,
      leader: initialState?.leader ?? null,
      successionTraining: initialState?.successionTraining ?? {
        candidatePigId: null,
        consecutiveCareDays: 0,
        bonusSuccessionPercentage: 0,
        bonusTransmissionPercentage: 0
      },
      transmissionStacks: initialState?.transmissionStacks ?? 0
    };
  }

  public evaluateConditions(
    pigs: Pig[],
    farm: Farm,
    majorWavesSurvived: number,
    hasActiveEpidemic = false,
    hasInfectedBarn = false
  ): type HerdConditionsReport {
    const livingPigs = pigs.filter(p => p.getData().isAlive);
    const farmState = farm.getState();
    const capacityReport = farm.calculateCapacityReport(livingPigs.length, 0);
    const currentSC = farmState.habitatIndex;

    // 1. Tính số lượng heo gắn kết
    // Kim Thọ trưởng thành/già tính bằng 3 con
    let cohesiveCount = 0;
    livingPigs.forEach(p => {
      const data = p.getData();
      if (data.bonding >= 50) {
        if (data.geneLine === GeneLineId.KimTho && (data.stage === PigStage.TruongThanh || data.stage === PigStage.HeoGia)) {
          cohesiveCount += 3;
        } else {
          cohesiveCount += 1;
        }
      }
    });

    // SC 30-49: tối đa 80 heo được tính gắn kết -> không thể đạt điều kiện 1
    if (currentSC < 50 && currentSC >= 30) {
      cohesiveCount = Math.min(80, cohesiveCount);
    } else if (currentSC < 30) {
      cohesiveCount = 0;
    }

    const condition1_Met = cohesiveCount >= 100;

    // 2. SC >= 50
    const condition2_Met = currentSC >= 50;

    // 3. Tinh thần TB >= 60 (>= 55 nếu có Kim Thọ), không hoảng loạn
    const hasKimTho = livingPigs.some(p => p.getData().geneLine === GeneLineId.KimTho);
    const requiredAvgMood = hasKimTho ? 55 : 60;
    const totalMood = livingPigs.reduce((sum, p) => sum + p.getData().mood, 0);
    const avgMood = livingPigs.length > 0 ? totalMood / livingPigs.length : 0;
    const anyPanic = livingPigs.some(p => p.getData().moodState === MoodState.HoangLoan);
    const condition3_Met = avgMood >= requiredAvgMood && !anyPanic;

    // 4. Mật độ 71 - 110%
    const density = capacityReport.rawDensityRatio;
    const condition4_Met = density >= 0.71 && density <= 1.10;

    // 5. >= 6 heo già có Hòa nhập >= 65
    const elderCohesiveCount = livingPigs.filter(p => {
      const d = p.getData();
      return d.stage === PigStage.HeoGia && d.bonding >= 65;
    }).length;
    const condition5_Met = elderCohesiveCount >= 6;

    // 6. Không có dịch nhóm A/B, không có Chuồng Nhiễm
    const condition6_Met = !hasActiveEpidemic && !hasInfectedBarn;

    // 7. Cấp trại >= 6 và đã qua >= 4 đợt lớn
    const condition7_Met = farmState.farmLevel >= 6 && majorWavesSurvived >= 4;

    const allConditionsMet = 
      condition1_Met &&
      condition2_Met &&
      condition3_Met &&
      condition4_Met &&
      condition5_Met &&
      condition6_Met &&
      condition7_Met;

    return {
      cohesivePigsCount: cohesiveCount,
      condition1_Met,
      currentSC,
      condition2_Met,
      avgMood,
      hasKimTho,
      requiredAvgMood,
      anyPanicLast24h: anyPanic,
      condition3_Met,
      densityRatio: density,
      condition4_Met,
      elderCohesiveCount,
      condition5_Met,
      hasActiveEpidemic,
      hasInfectedBarn,
      condition6_Met,
      farmLevel: farmState.farmLevel,
      majorWavesSurvived,
      condition7_Met,
      allConditionsMet,
      consecutiveDaysMet: this.daysConditionMetStreak
    };
  }

  /**
   * Cập nhật tiến trình Bầy Đàn mỗi ngày
   */
  public advanceDay(
    pigs: Pig[],
    farm: Farm,
    majorWavesSurvived: number
  ): { tierChanged: boolean; message?: string } {
    const report = this.evaluateConditions(pigs, farm, majorWavesSurvived);

    // Kiểm tra tính liên tục
    if (report.allConditionsMet) {
      this.daysConditionMetStreak += 1;
      this.daysConditionFailedStreak = 0;
    } else {
      this.daysConditionMetStreak = 0;
      this.daysConditionFailedStreak += 1;
    }

    let tierChanged = false;
    let message: string | undefined;

    // 1. Kiểm tra thành lập Bầy Đàn Sơ Khai (cần duy trì 3 ngày liên tục)
    if (this.state.tier === HerdStateTier.KhongCo) {
      if (this.daysConditionMetStreak >= 3) {
        this.state.tier = HerdStateTier.SoKhai;
        this.state.consecutiveDaysInState = 1;
        tierChanged = true;
        message = 'Trang trại đã chính thức đạt Trạng Thái Bầy Đàn (Bậc Sơ Khai)! Bắt đầu mở cuộc bầu thủ lĩnh.';
      }
    } else {
      // Đang có Bầy Đàn
      this.state.consecutiveDaysInState += 1;

      // Kiểm tra nguy cơ tan rã
      const sc = farm.getHabitatIndex();
      const shouldDisband = (sc < 30 && this.daysConditionFailedStreak >= 1) || (this.daysConditionFailedStreak >= 3);
      if (shouldDisband) {
        this.disbandHerd('Vi phạm điều kiện duy trì bầy đàn liên tục hoặc Sinh Cảnh dưới 30!');
        return { tierChanged: true, message: 'Bầy Đàn đã tan rã do không duy trì đủ điều kiện!' };
      }

      // Nâng cấp bậc Bầy Đàn
      if (this.state.tier === HerdStateTier.SoKhai) {
        // Bậc Vững: Có thủ lĩnh >= 5 ngày + SC >= 70
        if (this.state.leader && this.state.leader.daysAsLeader >= 5 && sc >= 70) {
          this.state.tier = HerdStateTier.Vung;
          tierChanged = true;
          message = 'Bầy Đàn đã nâng cấp lên Bậc VỮNG!';
        }
      } else if (this.state.tier === HerdStateTier.Vung) {
        // Bậc Hưng Thịnh: Bầy Vững >= 10 ngày + SC >= 90 + >= 1 tầng Truyền Thừa
        if (this.state.consecutiveDaysInState >= 10 && sc >= 90 && this.state.transmissionStacks >= 1) {
          this.state.tier = HerdStateTier.HungThinh;
          tierChanged = true;
          message = 'Bầy Đàn đã nâng cấp lên Bậc HƯNG THỊNH!';
        }
      }

      // Xử lý thủ lĩnh & bầu chọn
      this.processLeaderAndSuccession(pigs, majorWavesSurvived);
    }

    // Áp dụng Neo Tinh Thần cho toàn đàn
    if (this.state.leader && !this.state.leader.isUnderAttack) {
      const threshold = this.state.leader.neoThreshold;
      pigs.forEach(p => p.applyNeoTinhThan(threshold));
    }

    return { tierChanged, message };
  }

  private processLeaderAndSuccession(pigs: Pig[], majorWavesSurvived: number): void {
    const livingPigs = pigs.filter(p => p.getData().isAlive);

    // Nếu đã có thủ lĩnh
    if (this.state.leader) {
      const currentLeaderPig = livingPigs.find(p => p.getData().id === this.state.leader?.pigId);
      if (!currentLeaderPig || !currentLeaderPig.getData().isAlive) {
        // Thủ lĩnh đã chết / bị bắt -> mất tức thời tinh thần
        const isKimTho = currentLeaderPig?.getData().geneLine === GeneLineId.KimTho;
        const moodLoss = isKimTho ? 40 : 25;
        livingPigs.forEach(p => {
          const d = p.getData();
          p.getData().mood = Math.max(0, d.mood - moodLoss);
          p.updateMoodState();
        });

        this.state.leader = null;
        return;
      }

      this.state.leader.daysAsLeader += 1;
      
      // Xử lý bồi dưỡng kế vị nếu có chỉ định ứng viên
      if (this.state.successionTraining.candidatePigId) {
        const candidate = livingPigs.find(p => p.getData().id === this.state.successionTraining.candidatePigId);
        if (candidate && candidate.getData().mood >= 60) {
          this.state.successionTraining.consecutiveCareDays += 1;
          this.state.successionTraining.bonusSuccessionPercentage = Math.min(30, this.state.successionTraining.bonusSuccessionPercentage + 3);
          this.state.successionTraining.bonusTransmissionPercentage = Math.min(30, this.state.successionTraining.bonusTransmissionPercentage + 3);
        } else {
          // Bỏ bê 1 ngày -> mất 5%
          this.state.successionTraining.bonusSuccessionPercentage = Math.max(0, this.state.successionTraining.bonusSuccessionPercentage - 5);
          this.state.successionTraining.bonusTransmissionPercentage = Math.max(0, this.state.successionTraining.bonusTransmissionPercentage - 5);
        }
      }
      return;
    }

    // Ghế thủ lĩnh đang trống -> Bầu chọn / Kế thừa
    this.electNewLeader(livingPigs, majorWavesSurvived);
  }

  private electNewLeader(livingPigs: Pig[], majorWavesSurvived: number): void {
    // 1. Kiểm tra ứng viên kế vị (Succession)
    if (this.state.successionTraining.candidatePigId) {
      const candidate = livingPigs.find(p => p.getData().id === this.state.successionTraining.candidatePigId);
      if (candidate && candidate.getData().bonding >= 75) {
        const isKimTho = candidate.getData().geneLine === GeneLineId.KimTho;
        const baseRate = isKimTho ? 0.20 : 0.05;
        const totalRate = baseRate + (this.state.successionTraining.bonusSuccessionPercentage / 100);

        if (Math.random() < totalRate) {
          this.promoteToLeader(candidate, true);
          return;
        }
      }
    }

    // 2. Tìm ứng viên sáng lập tự nhiên (Heo già, Hòa nhập >= 65 hoặc Kim Thọ >= 50)
    for (const pig of livingPigs) {
      const data = pig.getData();
      const isKimTho = data.geneLine === GeneLineId.KimTho;
      const minBonding = isKimTho ? 50 : 65;

      if (data.stage === PigStage.HeoGia && data.bonding >= minBonding && data.mood >= 60 && data.ageDays >= 8) {
        // Tỉ lệ hình thành/ngày: 6% + 0.15%*(Hòa nhập - 65) + 2% nếu từng qua đợt lớn (*1.5 nếu Kim Thọ), max 20%
        let rate = 0.06 + (0.0015 * Math.max(0, data.bonding - 65));
        if (majorWavesSurvived > 0) rate += 0.02;
        if (isKimTho) rate *= 1.5;
        rate = Math.min(0.20, rate);

        if (Math.random() < rate) {
          this.promoteToLeader(pig, false);
          break;
        }
      }
    }
  }

  private promoteToLeader(pig: Pig, isSuccession: boolean): void {
    const data = pig.getData();
    const isKimTho = data.geneLine === GeneLineId.KimTho;

    // Kiểm tra nhận tầng Truyền Thừa nếu là kế vị
    if (isSuccession && this.state.transmissionStacks < 3) {
      const baseTransmissionRate = isKimTho ? 0.50 : 0.25;
      const totalTransmissionRate = baseTransmissionRate + (this.state.successionTraining.bonusTransmissionPercentage / 100);

      if (Math.random() < totalTransmissionRate) {
        this.state.transmissionStacks = Math.min(3, this.state.transmissionStacks + 1);

        // Kim Thọ đạt tỉ lệ thì có thêm 30% nhận tầng thứ 2
        if (isKimTho && this.state.transmissionStacks < 3 && Math.random() < 0.30) {
          this.state.transmissionStacks = Math.min(3, this.state.transmissionStacks + 1);
        }
      }
    }

    const neoBonus = this.state.transmissionStacks * 5;
    this.state.leader = {
      pigId: data.id,
      name: data.name,
      daysAsLeader: 0,
      neoThreshold: 25 + neoBonus,
      neoRadiusMeters: 20 + neoBonus,
      isUnderAttack: false,
      disableTimeLeftSeconds: 0
    };

    // Reset kế vị
    this.state.successionTraining = {
      candidatePigId: null,
      consecutiveCareDays: 0,
      bonusSuccessionPercentage: 0,
      bonusTransmissionPercentage: 0
    };
  }

  public disbandHerd(reason: string): void {
    this.state.tier = HerdStateTier.KhongCo;
    this.state.consecutiveDaysInState = 0;
    this.daysConditionMetStreak = 0;
    this.daysConditionFailedStreak = 0;
  }

  public getState(): type HerdState { return this.state; }
  public getTier(): HerdStateTier { return this.state.tier; }
  public getLeader(): AlphaLeaderData | null { return this.state.leader; }
  public getTransmissionStacks(): number { return this.state.transmissionStacks; }
}
