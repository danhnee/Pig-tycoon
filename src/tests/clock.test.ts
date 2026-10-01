import test from 'node:test';
import assert from 'node:assert';
import { GameClock } from '../models/GameClock.ts';
import { TimeOfDay } from '../types/enums.ts';

test('GameClock - Time conversions and 4 day parts', () => {
  const clock = new GameClock(1, 6, 0); // 06:00 Day 1

  assert.strictEqual(clock.getTimeOfDay(), TimeOfDay.Sang);

  // Tua 4 giờ (240 phút) -> 10:00 (Trưa)
  clock.tickMinutes(240);
  assert.strictEqual(clock.getHour(), 10);
  assert.strictEqual(clock.getTimeOfDay(), TimeOfDay.Trua);

  // Tua 4 giờ -> 14:00 (Chiều)
  clock.tickMinutes(240);
  assert.strictEqual(clock.getHour(), 14);
  assert.strictEqual(clock.getTimeOfDay(), TimeOfDay.Chieu);

  // Tua 4 giờ -> 18:00 (Tối)
  clock.tickMinutes(240);
  assert.strictEqual(clock.getHour(), 18);
  assert.strictEqual(clock.getTimeOfDay(), TimeOfDay.Toi);
});

test('GameClock - Sleep cooldown and advance to morning', () => {
  const clock = new GameClock(1, 20, 0); // 20:00 Night
  
  // Ngủ chuẩn
  const sleepResult = clock.performStandardSleep();
  assert.strictEqual(sleepResult.success, true);
  assert.strictEqual(clock.getHour(), 6);
  assert.strictEqual(clock.getDay(), 2);
  assert.strictEqual(clock.getSleepCooldown(), 6); // Cooldown 6 giờ

  // Thử ngủ tiếp ngay lập tức -> phải thất bại
  const sleepAgainResult = clock.performStandardSleep();
  assert.strictEqual(sleepAgainResult.success, false);
  assert.match(sleepAgainResult.message, /Cooldown còn 6 giờ game/);

  // Tiến 6 giờ game (360 phút) -> hết cooldown
  clock.tickMinutes(360);
  assert.strictEqual(clock.getSleepCooldown(), 0);
});

test('GameClock - Cycle scheduling rules', () => {
  const clock = new GameClock(1, 6, 0);
  
  // Chu kỳ 1 (Đầu game: 6 - 8 ngày)
  const cycleLength = clock.getCycleTotalDays();
  assert.ok(cycleLength >= 6 && cycleLength <= 15, `Độ dài chu kỳ ${cycleLength} phải trong khoảng hợp lệ`);
});
