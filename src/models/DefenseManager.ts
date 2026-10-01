import { BuildingCategory } from '../types/enums.ts';
import { type BuildingHallState, type DefenseBlueprint, type DefenseBuildingInstance } from '../types/defense.ts';

export class DefenseManager {
  private hall: BuildingHallState;
  private buildings: DefenseBuildingInstance[];
  private blueprintRegistry: Map<string, DefenseBlueprint>;

  constructor(initialHall?: Partial<BuildingHallState>, initialBuildings?: DefenseBuildingInstance[]) {
    this.hall = {
      level: initialHall?.level ?? 1,
      activeConstructionSlots: 0,
      maxConstructionSlots: 1,
      assignedWorkerType: 'EngineerBasic',
      workerEfficiency: 0.70, // Kỹ sư cơ bản 70%
      unlockedBlueprints: ['RaoGo', 'ThapCanh', 'BanChong', 'NoXuyenVan'],
      activeConstructions: []
    };

    this.buildings = initialBuildings ? [...initialBuildings] : [];
    this.blueprintRegistry = new Map();
    this.initBlueprints();
  }

  private initBlueprints(): void {
    const defaultBlueprints: DefenseBlueprint[] = [
      {
        id: 'NoXuyenVan',
        name: 'Nỏ Xuyên Vân',
        category: BuildingCategory.TanCong,
        rarity: 'Tinh',
        requiredHallLevel: 1,
        buildCostGold: 2400,
        buildMaterials: { wood: 30, steel: 10 },
        baseBuildHours: 6,
        isUnlocked: true,
        isBachVan: false
      },
      {
        id: 'ThapHoQuang',
        name: 'Tháp Hồ Quang',
        category: BuildingCategory.TanCong,
        rarity: 'CaoCap',
        requiredHallLevel: 2,
        buildCostGold: 4800,
        buildMaterials: { steel: 25 },
        baseBuildHours: 8,
        isUnlocked: false,
        isBachVan: false
      },
      {
        id: 'CuuTieuLoiPhao',
        name: 'Bạch Vân · Cửu Tiêu Lôi Pháo',
        category: BuildingCategory.TanCong,
        rarity: 'BachVan',
        requiredHallLevel: 3,
        buildCostGold: 18000,
        buildMaterials: { steel: 80, refinedParts: 4 },
        baseBuildHours: 16,
        isUnlocked: false,
        isBachVan: true
      },
      {
        id: 'ThienNhanVongDai',
        name: 'Bạch Vân · Thiên Nhãn Vọng Đài',
        category: BuildingCategory.ThongTin_TrinhTham,
        rarity: 'BachVan',
        requiredHallLevel: 3,
        buildCostGold: 12500,
        buildMaterials: { steel: 60, refinedParts: 3 },
        baseBuildHours: 12,
        isUnlocked: false,
        isBachVan: true
      },
      {
        id: 'VanLinhHoiNguyenTran',
        name: 'Bạch Vân · Vạn Linh Hồi Nguyên Trận',
        category: BuildingCategory.HoTro,
        rarity: 'BachVan',
        requiredHallLevel: 3,
        buildCostGold: 16000,
        buildMaterials: { refinedParts: 3 },
        baseBuildHours: 14,
        isUnlocked: false,
        isBachVan: true
      }
    ];

    defaultBlueprints.forEach(bp => this.blueprintRegistry.set(bp.id, bp));
  }

  /**
   * Kiểm tra điều kiện và xây công trình mới
   */
  public constructBuilding(blueprintId: string): { success: boolean; message: string; building?: DefenseBuildingInstance } {
    const bp = this.blueprintRegistry.get(blueprintId);
    if (!bp) {
      return { success: false, message: `Bản vẽ ${blueprintId} không tồn tại!` };
    }

    if (!this.hall.unlockedBlueprints.includes(blueprintId)) {
      return { success: false, message: `Chưa học bản vẽ ${bp.name}!` };
    }

    if (this.hall.level < bp.requiredHallLevel) {
      return { 
        success: false, 
        message: `Phòng Xây Dựng cấp ${this.hall.level} không đủ điều kiện (cần cấp ${bp.requiredHallLevel})!` 
      };
    }

    // Kiểm tra giới hạn Bạch Vân (tối đa 1 công trình mỗi loại)
    if (bp.isBachVan) {
      const existing = this.buildings.find(b => b.blueprintId === blueprintId && !b.isBroken);
      if (existing) {
        return { success: false, message: `Đã sở hữu ${bp.name}! Mỗi trại chỉ có tối đa 1 công trình mỗi loại Bạch Vân.` };
      }
    }

    // Tạo thực thể công trình
    const newBuilding: DefenseBuildingInstance = {
      instanceId: `bldg_${Date.now()}_${Math.floor(Math.random() * 1000)}`,
      blueprintId: bp.id,
      name: bp.name,
      category: bp.category,
      isBachVan: bp.isBachVan,
      durability: 100,
      maxHp: bp.isBachVan ? 5000 : 1500,
      currentHp: bp.isBachVan ? 5000 : 1500,
      isBroken: false,
      ammoCurrent: bp.id === 'NoXuyenVan' ? 50 : (bp.isBachVan ? 120 : 24),
      ammoCapacity: bp.id === 'NoXuyenVan' ? 50 : (bp.isBachVan ? 120 : 24),
      ammoName: bp.id === 'NoXuyenVan' ? 'Tên Thép' : (bp.isBachVan ? 'Đơn Vị Cộng Hưởng' : 'Pin'),
      ammoCostPerShot: bp.id === 'NoXuyenVan' ? 1 : (bp.isBachVan ? 50 : 1),
      powerDrawIdle: 2,
      powerDrawActive: bp.isBachVan ? 40 : 8,
      cooldownSeconds: bp.id === 'NoXuyenVan' ? 1.2 : (bp.isBachVan ? 75 : 2.2),
      currentCooldownRemaining: 0,
      fireMode: 'TuDo',
      isManualTriggerReady: bp.isBachVan,
      warmupTimeRemainingSeconds: 0
    };

    this.buildings.push(newBuilding);
    return { success: true, message: `Đã xây dựng thành công ${bp.name}!`, building: newBuilding };
  }

  /**
   * Nạp trước nhiên liệu / đạn cho công trình (Chỉ thực hiện ngoài đợt quái)
   */
  public preloadAmmo(instanceId: string, amount: number, isDuringWave: boolean): { success: boolean; message: string } {
    if (isDuringWave) {
      return { success: false, message: 'Luật 2: Tuyệt đối không được nạp đạn trong đợt quái!' };
    }

    const b = this.buildings.find(item => item.instanceId === instanceId);
    if (!b) return { success: false, message: 'Không tìm thấy công trình!' };

    const spaceLeft = b.ammoCapacity - b.ammoCurrent;
    const toAdd = Math.min(spaceLeft, amount);
    b.ammoCurrent += toAdd;

    return { success: true, message: `Đã nạp ${toAdd} ${b.ammoName}. Hiện có ${b.ammoCurrent}/${b.ammoCapacity}.` };
  }

  /**
   * Kích hoạt thủ công công trình Bạch Vân
   */
  public triggerBachVanManual(instanceId: string): { success: boolean; message: string } {
    const b = this.buildings.find(item => item.instanceId === instanceId);
    if (!b || !b.isBachVan) {
      return { success: false, message: 'Chỉ công trình Bạch Vân mới được kích hoạt thủ công!' };
    }

    if (b.isBroken || b.durability <= 0) {
      return { success: false, message: `${b.name} đã bị hỏng, cần sửa chữa!` };
    }

    if (b.ammoCurrent < b.ammoCostPerShot) {
      return { success: false, message: `${b.name} không đủ nhiên liệu/đạn để kích hoạt!` };
    }

    if (b.currentCooldownRemaining > 0) {
      return { success: false, message: `${b.name} đang trong thời gian hồi chiêu (${b.currentCooldownRemaining}s)!` };
    }

    // Tiêu hao nhiên liệu & độ bền
    b.ammoCurrent -= b.ammoCostPerShot;
    b.durability = Math.max(0, b.durability - 22);
    b.currentCooldownRemaining = b.cooldownSeconds;

    return { 
      success: true, 
      message: `ĐÃ KÍCH HOẠT ${b.name.toUpperCase()}! Bắt đầu truyền năng lượng hủy diệt!` 
    };
  }

  public getBuildings(): DefenseBuildingInstance[] { return this.buildings; }
  public getHall(): BuildingHallState { return this.hall; }
}
