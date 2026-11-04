import 'package:flutter/foundation.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:pig_tycoon_flutter_client/main.dart';
import 'package:pig_tycoon_flutter_client/pig_tycoon_polyglot_engine.dart';

void main() {
  test(
      'Polyglot Engine: Dart Zero-GC TypedData & Rust Native FFI match exact 4-kernel checksums',
      () {
    final engine = PigTycoonPolyglotEngine();

    final dartMetrics = engine.runBenchmarks(
      preferNativeFfi: false,
      bfsIters: 500,
      boidsSteps: 5000,
      combatTicks: 50000,
      economySeeds: 2000,
    );
    expect(dartMetrics.isBitExact, isTrue,
        reason: 'Pure Dart Web/VM fallback must match canonical checksums');

    if (engine.isNativeFfiLoaded) {
      final ffiMetrics = engine.runBenchmarks(
        preferNativeFfi: true,
        bfsIters: 500,
        boidsSteps: 5000,
        combatTicks: 50000,
        economySeeds: 2000,
      );
      expect(ffiMetrics.isBitExact, isTrue,
          reason: 'Dart Native FFI must match canonical checksums');
      expect(ffiMetrics.totalMs, lessThan(dartMetrics.totalMs));
    }
  });

  test(
      '8-Dir Flow-Field & GDD 5.4 + 3000-Year Seal match bit-for-bit between Dart and Native C-ABI',
      () {
    final engine = PigTycoonPolyglotEngine();
    const w = 24;
    const h = 16;
    const n = w * h;
    final obstacles = Uint8List(n);
    for (int i = 0; i < n; i++) {
      if (i % 13 == 5 && i != 0) obstacles[i] = 1;
    }
    final targetsX = Int32List.fromList([12, 4]);
    final targetsY = Int32List.fromList([8, 3]);

    final dartDist = Int32List(n);
    final dartVx = Float32List(n);
    final dartVy = Float32List(n);
    final dartReachable = engine.computeFlowField8Dir(
      width: w,
      height: h,
      obstacles: obstacles,
      targetsX: targetsX,
      targetsY: targetsY,
      targetCount: 2,
      outDist: dartDist,
      outVx: dartVx,
      outVy: dartVy,
      preferNativeFfi: false,
    );
    expect(dartReachable, greaterThan(300));
    expect(dartDist[8 * w + 12], 0);

    if (engine.isNativeFfiLoaded) {
      final ffiDist = Int32List(n);
      final ffiVx = Float32List(n);
      final ffiVy = Float32List(n);
      final ffiReachable = engine.computeFlowField8Dir(
        width: w,
        height: h,
        obstacles: obstacles,
        targetsX: targetsX,
        targetsY: targetsY,
        targetCount: 2,
        outDist: ffiDist,
        outVx: ffiVx,
        outVy: ffiVy,
        preferNativeFfi: true,
      );
      expect(ffiReachable, dartReachable);
      expect(ffiDist, orderedEquals(dartDist));
    }

    final gddDart = engine.evaluateGdd54AndSeal(
      channelCode: 1,
      rarityCode: 5,
      protagonistTag: 0,
      preferNativeFfi: false,
    );
    expect(gddDart.sealYearsPlusTag, 3000);
    expect(gddDart.probPermille, 350);
    expect(gddDart.goldCost, 5000);

    if (engine.isNativeFfiLoaded) {
      final gddFfi = engine.evaluateGdd54AndSeal(
        channelCode: 1,
        rarityCode: 5,
        protagonistTag: 1,
        preferNativeFfi: true,
      );
      expect(gddFfi.usedNativeRust, isTrue);
      expect(gddFfi.sealYearsPlusTag, 3001);
      expect(gddFfi.probPermille, 350);
      expect(gddFfi.goldCost, 5000);
    }
  });

  testWidgets(
      'Flutter Dashboard: Switches An/Khoa dual-perspective canon and runs polyglot simulation step on Mobile/PC/Web',
      (WidgetTester tester) async {
    await tester.pumpWidget(const PigTycoonMultiPlatformApp());
    expect(find.textContaining('PIG TYCOON v6.0'), findsOneWidget);
    expect(find.textContaining('Khoa'), findsWidgets);
    expect(find.textContaining('Em gái Bé An (13/05/2019)'), findsWidgets);
    expect(find.textContaining('Sổ bìa đen 2 cột'), findsOneWidget);

    await tester.tap(find.byKey(const Key('btn_switch_character')));
    await tester.pump();
    expect(find.textContaining('An (STR 17'), findsOneWidget);
    expect(find.textContaining('Em trai Bé Khoa (13/05/2019)'), findsWidgets);
    expect(
        find.textContaining('Sổ bìa hồng dán sticker con thỏ'), findsOneWidget);

    final runBtn = find.byKey(const Key('btn_run_polyglot_step'));
    await tester.ensureVisible(runBtn);
    await tester.pumpAndSettle();
    await tester.tap(runBtn);
    await tester.pump();
    expect(find.textContaining('Ngày 2'), findsOneWidget);
    expect(find.textContaining('Backend:'), findsOneWidget);
    expect(find.textContaining('GDD 5.4 Chợ Đêm Dị Biến: 35.0%'), findsOneWidget);
  });
}
