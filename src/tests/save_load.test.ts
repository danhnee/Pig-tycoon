import test from 'node:test';
import assert from 'node:assert';
import { GameEngine } from '../engine/GameEngine.ts';
import { SaveLoadManager } from '../engine/SaveLoad.ts';
import { GeneLineId, GeneRarity, PigStage } from '../types/enums.ts';
import { Pig } from '../models/Pig.ts';

test('SaveLoadManager - Full deterministic serialization and deserialization', () => {
  const engine1 = new GameEngine();

  // Thiết lập trạng thái mẫu
  engine1.clock.tickMinutes(300); // 11:00 Trưa
  engine1.farm.addGold(5000);
  engine1.farm.getState().habitatIndex = 88;

  // Thêm 3 con heo
  const pig1 = Pig.createDefault('p1', 'Heo 1', GeneLineId.HongDien, GeneRarity.Thuong);
  pig1.getData().weightKg = 98;
  pig1.getData().stage = PigStage.TruongThanh;

  const pig2 = Pig.createDefault('p2', 'Heo 2 Kim Thọ', GeneLineId.KimTho, GeneRarity.HuyenThoai);
  pig2.getData().bonding = 75;

  const pig3 = Pig.createDefault('p3', 'Heo 3 Hư Thể', GeneLineId.HuThe, GeneRarity.DiBien);

  engine1.pigs = [pig1, pig2, pig3];

  // Xây 1 Nỏ Xuyên Vân
  engine1.defense.constructBuilding('NoXuyenVan');

  // Serialize to JSON
  const saveState = engine1.exportSaveState('TestSave');
  const jsonString = SaveLoadManager.serialize(saveState);

  assert.ok(jsonString.length > 500, 'JSON chuỗi lưu phải có dung lượng đầy đủ');

  // Deserialize to fresh instance
  const loadedState = SaveLoadManager.deserialize(jsonString);
  const engine2 = new GameEngine();
  engine2.importSaveState(loadedState);

  // Kiểm tra tính toàn vẹn
  assert.strictEqual(engine2.clock.getDay(), engine1.clock.getDay());
  assert.strictEqual(engine2.clock.getHour(), engine1.clock.getHour());
  assert.strictEqual(engine2.clock.getMinute(), engine1.clock.getMinute());
  assert.strictEqual(engine2.farm.getGold(), 7500); // 2500 ban đầu + 5000
  assert.strictEqual(engine2.farm.getHabitatIndex(), 88);

  assert.strictEqual(engine2.pigs.length, 3);
  assert.strictEqual(engine2.pigs[0].getData().name, 'Heo 1');
  assert.strictEqual(engine2.pigs[1].getData().geneLine, GeneLineId.KimTho);
  assert.strictEqual(engine2.pigs[2].getData().geneLine, GeneLineId.HuThe);

  assert.strictEqual(engine2.defense.getBuildings().length, 1);
  assert.strictEqual(engine2.defense.getBuildings()[0].name, 'Nỏ Xuyên Vân');
});
