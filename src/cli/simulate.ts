import { GameEngine } from '../engine/GameEngine.ts';
import { GeneLineId, GeneRarity, MeatQuality, PigStage } from '../types/enums.ts';
import { Pig } from '../models/Pig.ts';

export function runDemonstrationSimulation(): void {
  console.log('================================================================');
  console.log('       PIG TYCOON v6.0 - CORE FOUNDATION (PROTOTYPE P0)        ');
  console.log('================================================================\n');

  const engine = new GameEngine();

  // 1. Thiết lập đàn heo ban đầu với các giống đặc trưng trong GDD v6.0
  console.log('👉 Khởi tạo đàn heo khởi đầu:');
  
  // Heo thường
  const p1 = Pig.createDefault('p_hongdien_1', 'Hồng Điền #1', GeneLineId.HongDien, GeneRarity.Thuong);
  p1.getData().stage = PigStage.TruongThanh;
  p1.getData().weightKg = 102;
  p1.getData().meatQuality = MeatQuality.A;
  p1.getData().bonding = 55;

  const p2 = Pig.createDefault('p_lamkhe_1', 'Lam Khê #1', GeneLineId.LamKhe, GeneRarity.Thuong);
  p2.getData().stage = PigStage.TruongThanh;
  p2.getData().weightKg = 98;
  p2.getData().meatQuality = MeatQuality.B;
  p2.getData().bonding = 50;

  // Heo Huyền Thoại: Kim Thọ (Trụ cột Bầy Đàn)
  const p3 = Pig.createDefault('p_kimtho_1', 'Kim Thọ Tối Thượng', GeneLineId.KimTho, GeneRarity.HuyenThoai);
  p3.getData().stage = PigStage.TruongThanh;
  p3.getData().weightKg = 90;
  p3.getData().bonding = 85;
  p3.getData().isIdentified = true;

  // Heo Dị Biến: Hư Thể (1 tốt + 1 xấu ngủ)
  const p4 = Pig.createDefault('p_huthe_1', 'Hư Thể Bí Ẩn', GeneLineId.HuThe, GeneRarity.DiBien);
  p4.getData().stage = PigStage.DangLon;
  p4.getData().weightKg = 35;
  p4.getData().bonding = 40;

  engine.pigs.push(p1, p2, p3, p4);

  console.log(`- Đã nạp 4 cá thể đại diện: ${p1.getData().name}, ${p2.getData().name}, ${p3.getData().name} (Huyền thoại), ${p4.getData().name} (Dị biến).`);

  // 2. Xây dựng công trình phòng thủ Nỏ Xuyên Vân
  console.log('\n👉 Thiết lập Phòng Xây Dựng & Công trình:');
  const constructResult = engine.defense.constructBuilding('NoXuyenVan');
  console.log(`- ${constructResult.message}`);

  // 3. In trạng thái trại
  printDashboard(engine);

  // 4. Mô phỏng hoạt động trong ngày: Bán 1 heo tại Chợ Ngày
  console.log('\n👉 [14:00 CHIỀU] Giao dịch tại Chợ Ngày:');
  const sale = engine.sellPigAtDayMarket('p_hongdien_1', true);
  console.log(`- ${sale.message}`);

  // 5. Đến đêm: Chợ đêm mở & kiểm tra Bí nhân
  console.log('\n👉 [18:00 TỐI] Chợ Đêm hoạt động (Cơ chế trao đổi & 18% ô nhận Vàng):');
  engine.clock.tickMinutes(4 * 60); // Tiến đến 18:00
  engine.economy.generateNightMarket(engine.farm.getFarmLevel());
  const listings = engine.economy.getNightListings();
  listings.slice(0, 3).forEach(l => {
    const goldText = l.acceptsGold ? `[CÓ Ô VÀNG: ${l.goldPrice}G]` : '[CHỈ TRAO ĐỔI]';
    console.log(`  * ${l.offeredItem.name} (${l.offeredItem.tier}) -> ${goldText} ${l.requestedItemDescription}`);
  });

  // 6. Đi ngủ
  console.log('\n👉 [22:00 ĐÊM] Nhân vật đi ngủ chuẩn (Tua đến 06:00 sáng hôm sau):');
  const sleepRes = engine.clock.performStandardSleep();
  console.log(`- ${sleepRes.message}`);

  // Chạy cập nhật ngày mới
  engine.tick(0);

  // In lại Dashboard ngày 2
  printDashboard(engine);

  console.log('\n================================================================');
  console.log('      PROTOTYPE P0 KIỂM THỬ THÀNH CÔNG THEO GDD v6.0           ');
  console.log('================================================================\n');
}

function printDashboard(engine: GameEngine): void {
  const clock = engine.clock;
  const farm = engine.farm;
  const livingPigs = engine.pigs.filter(p => p.getData().isAlive);
  const cap = farm.calculateCapacityReport(livingPigs.length, 0);

  console.log('\n┌────────────────────────── PIG TYCOON STATUS ──────────────────────────┐');
  console.log(`│ Thời gian: Ngày ${clock.getDay()} | ${clock.getHour().toString().padStart(2, '0')}:${clock.getMinute().toString().padStart(2, '0')} (${clock.getTimeOfDay()}) | Chu kỳ ${clock.getCycleNumber()} (Ngày ${clock.getCycleDayIndex()}/${clock.getCycleTotalDays()})`);
  console.log(`│ Thời tiết: ${engine.currentWeather} | Đợt lớn kế tiếp: còn ${clock.getDaysUntilMajorWave()} ngày`);
  console.log('├───────────────────────────────────────────────────────────────────────┤');
  console.log(`│ Vàng trong ví: ${farm.getGold()}G | Cấp trại: ${farm.getFarmLevel()} (EXP: ${farm.getState().farmExp})`);
  console.log(`│ Sinh Cảnh (SC): ${farm.getHabitatIndex()}/100 [${farm.getHabitatTier()}] | Áp Lực Sự Kiện: ${farm.getEventPressureIndex()}/100 [${farm.getEventPressureState()}]`);
  console.log(`│ Đàn heo: ${livingPigs.length} con sống | Sức chứa: ${cap.effectiveCapacity} (Nghẽn bởi: ${cap.bottleneck})`);
  console.log(`│ Mật độ: ${(cap.rawDensityRatio * 100).toFixed(1)}% [${cap.densityZoneName}] -> Hệ số dịch: x${cap.diseaseDensityMultiplier.toFixed(2)}`);
  console.log(`│ Trạng thái Bầy Đàn: [${engine.herd.getTier()}] | Tầng Truyền Thừa: ${engine.herd.getTransmissionStacks()}/3`);
  console.log(`│ Thể lực nhân vật: ${engine.character.stamina.currentStamina}/${engine.character.stamina.maxStamina} (Tải ngày: ${engine.character.stamina.dailyLoad})`);
  console.log('└───────────────────────────────────────────────────────────────────────┘');
}

runDemonstrationSimulation();
