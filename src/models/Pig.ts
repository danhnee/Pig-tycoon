import { GeneLineId, GeneRarity, MeatQuality, MoodState, PigStage } from '../types/enums.ts';
import { type PigData, type PigTrait } from '../types/pig.ts';

export class Pig {
  private data: PigData;

  constructor(data: PigData) {
    this.data = data;
    this.updateStageAndLimits();
    this.updateMoodState();
  }

  public static createDefault(
    id: string,
    name: string,
    geneLine: GeneLineId,
    rarity: GeneRarity,
    gender: 'Duc' | 'Nai' = 'Duc',
    initialAgeDays = 0,
    initialWeightKg = 1.5
  ): Pig {
    const isHighRarity = rarity !== GeneRarity.Thuong && rarity !== GeneRarity.Kha;
    
    // Khởi tạo tính chất riêng cho Hư Thể
    const traits: PigTrait[] = [];
    if (geneLine === GeneLineId.HuThe) {
      traits.push({
        id: 'ThitHuKhong',
        name: 'Thịt Hư Không',
        isPositive: true,
        isDeviant: true,
        description: 'Chợ đêm có đơn trao đổi riêng, giá trị x3'
      });
      traits.push({
        id: 'HutSuong',
        name: 'Hút Sương',
        isPositive: false,
        isDeviant: true,
        isAwakened: false, // Ngủ ban đầu
        isSealed: false,
        description: 'Khi thức: ÁLSK +Vừa/ngày, thiên về sương mù'
      });
    }

    const data: PigData = {
      id,
      name,
      ageDays: initialAgeDays,
      weightKg: initialWeightKg,
      stage: PigStage.HeoNon,
      gender,
      geneLine,
      rarity,
      isIdentified: !isHighRarity, // Thường & Khá hiện tên ngay
      isXichMaoDomesticated: false,
      traits,
      health: 100,
      temperament: 50,
      mood: 70,
      moodState: MoodState.BinhOn,
      bonding: geneLine === GeneLineId.KimTho ? 60 : 40,
      adhesionScore: 55,
      meatQuality: MeatQuality.B,
      growthRateMultiplier: geneLine === GeneLineId.LoiMach ? 1.22 : 1.0,
      maxWeightKg: geneLine === GeneLineId.HongDien ? 105 : (geneLine === GeneLineId.KimTho ? 95 : 110),
      fcrMultiplier: 1.0,
      isQuarantinedForSale: false,
      hasDiseaseFlag: false,
      diseases: [],
      isF1: false,
      isF2: false,
      isPregnant: false,
      pregnancyDaysLeft: 0,
      recoveryDaysLeft: 0,
      littersDelivered: 0,
      isAlive: true,
      isInProcessingLot: false,
      daysDeceased: 0
    };

    return new Pig(data);
  }

  public getData(): PigData {
    return this.data;
  }

  public updateMoodState(): void {
    if (this.data.mood >= 60) {
      this.data.moodState = MoodState.BinhOn;
    } else if (this.data.mood >= 40) {
      this.data.moodState = MoodState.BatAn;
    } else if (this.data.mood >= 20) {
      this.data.moodState = MoodState.HoangSo;
    } else {
      this.data.moodState = MoodState.HoangLoan;
    }

    // Độ bám đàn = (Hòa nhập + Tinh thần) / 2
    this.data.adhesionScore = (this.data.bonding + this.data.mood) / 2;
  }

  public updateStageAndLimits(): void {
    const age = this.data.ageDays;
    
    // Mộc Cước trưởng thành kéo dài 11-34 ngày thay vì 11-28
    const adultEndDay = this.data.geneLine === GeneLineId.MocCuoc ? 34 : 28;

    if (age <= 4) {
      this.data.stage = PigStage.HeoNon;
    } else if (age <= 10) {
      this.data.stage = PigStage.DangLon;
    } else if (age <= adultEndDay) {
      this.data.stage = PigStage.TruongThanh;
    } else {
      this.data.stage = PigStage.HeoGia;
    }
  }

  /**
   * Cập nhật hàng ngày cho heo: lớn lên, ăn, già đi, kiểm tra phong ấn Hư Thể
   */
  public advanceDay(habitatIndex: number, isInHerdState: boolean, densityPenalty: number): void {
    if (!this.data.isAlive) {
      this.data.daysDeceased += 1;
      return;
    }

    this.data.ageDays += 1;
    const previousStage = this.data.stage;
    this.updateStageAndLimits();

    // 1. Hòa nhập đàn (+3/ngày khi sống cùng đàn; SC 30-49: +1.5; SC < 30: -1)
    let bondingRate = 3.0;
    if (habitatIndex < 30) bondingRate = -1.0;
    else if (habitatIndex < 50) bondingRate = 1.5;

    if (this.data.geneLine === GeneLineId.KimTho) {
      bondingRate *= 2.0; // Kim Thọ tăng x2
    }
    this.data.bonding = Math.max(0, Math.min(100, this.data.bonding + bondingRate));

    // 2. Tinh thần hồi phục tự nhiên nếu bình yên
    if (this.data.moodState !== MoodState.HoangLoan) {
      this.data.mood = Math.min(100, this.data.mood + 5);
    }
    this.updateMoodState();

    // 3. Cơ chế Phong ấn Hư Thể (mục 5.3):
    // Nếu qua hết giai đoạn Đang Lớn (age > 10) mà tính chất xấu chưa thức tỉnh -> phong ấn vĩnh viễn!
    if (this.data.geneLine === GeneLineId.HuThe && previousStage === PigStage.DangLon && this.data.stage === PigStage.TruongThanh) {
      const badTrait = this.data.traits.find(t => t.isDeviant && !t.isPositive);
      if (badTrait && !badTrait.isAwakened) {
        badTrait.isSealed = true;
      }
    }

    // 4. Kiểm tra điều kiện thức tỉnh tính chất xấu Hư Thể
    if (this.data.geneLine === GeneLineId.HuThe) {
      const badTrait = this.data.traits.find(t => t.isDeviant && !t.isPositive);
      if (badTrait && !badTrait.isAwakened && !badTrait.isSealed) {
        const hasBadCondition = this.data.mood < 40 || this.data.moodState === MoodState.HoangLoan || this.data.health < 60;
        if (hasBadCondition) {
          // Trong bầy đàn chỉ 50% thức tỉnh
          const chance = isInHerdState ? 0.50 : 1.0;
          if (Math.random() < chance) {
            badTrait.isAwakened = true;
          }
        }
      }
    }

    // 5. Tăng trọng
    this.growWeight(densityPenalty);

    // 6. Kiểm tra tử vong tự nhiên ở Heo Già
    this.checkNaturalMortality();
  }

  private growWeight(densityPenalty: number): void {
    if (this.data.stage === PigStage.HeoGia) {
      // Heo già giảm 1kg/ngày
      this.data.weightKg = Math.max(45, this.data.weightKg - 1.0);
      return;
    }

    let dailyGain = 0;
    if (this.data.stage === PigStage.HeoNon) {
      dailyGain = 2.1; // 1.5kg -> 12kg qua 5 ngày
    } else if (this.data.stage === PigStage.DangLon) {
      dailyGain = 8.0; // 12kg -> 60kg qua 6 ngày
    } else if (this.data.stage === PigStage.TruongThanh) {
      dailyGain = 3.0; // 60kg -> 110kg qua 17 ngày
    }

    // Áp dụng hệ số gen & mật độ
    let effectiveRate = this.data.growthRateMultiplier * (1 - densityPenalty);
    if (this.data.isXichMaoDomesticated) {
      effectiveRate *= 1.08; // Xích Mao thuần hóa +8% tăng trọng
    }

    this.data.weightKg = Math.min(this.data.maxWeightKg, this.data.weightKg + dailyGain * effectiveRate);
  }

  private checkNaturalMortality(): void {
    const age = this.data.ageDays;
    const maxLifespan = this.data.geneLine === GeneLineId.KimTho ? 56 : 40;
    const mortalityStartAge = maxLifespan - 4;

    if (age >= mortalityStartAge) {
      const daysOver = age - mortalityStartAge + 1;
      const deathChance = Math.min(1.0, 0.05 * daysOver); // 5%/ngày, +5% mỗi ngày
      if (Math.random() < deathChance) {
        this.die();
      }
    }
  }

  public die(): void {
    this.data.isAlive = false;
    this.data.health = 0;
    this.data.mood = 0;
    this.updateMoodState();
  }

  public applyNeoTinhThan(threshold: number): void {
    if (this.data.isAlive && this.data.mood < threshold) {
      this.data.mood = threshold;
      this.updateMoodState();
    }
  }

  public feedSpecialFeed(feedType: 'lingzhi' | 'hahoa'): void {
    if (feedType === 'lingzhi') {
      this.data.mood = Math.min(100, this.data.mood + 5);
    } else if (feedType === 'hahoa' && this.data.geneLine === GeneLineId.XichMao) {
      this.data.isXichMaoDomesticated = true;
    }
    this.updateMoodState();
  }
}
