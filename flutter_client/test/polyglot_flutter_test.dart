import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:pig_tycoon_flutter_client/main.dart';
import 'package:pig_tycoon_flutter_client/pig_tycoon_polyglot_engine.dart';

void main() {
  test(
      'Polyglot Engine: Dart TypedData & Rust Native FFI match exact 4-kernel checksums',
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

  testWidgets(
      'Flutter Dashboard: Switches An/Khoa and runs polyglot simulation step on Mobile/PC/Web',
      (WidgetTester tester) async {
    await tester.pumpWidget(const PigTycoonMultiPlatformApp());
    expect(find.textContaining('PIG TYCOON v6.0'), findsOneWidget);
    expect(find.textContaining('Khoa'), findsWidgets);

    await tester.tap(find.byKey(const Key('btn_switch_character')));
    await tester.pump();
    expect(find.textContaining('An (STR 17'), findsOneWidget);

    await tester.tap(find.byKey(const Key('btn_run_polyglot_step')));
    await tester.pump();
    expect(find.textContaining('Ngày 2'), findsOneWidget);
    expect(find.textContaining('Backend:'), findsOneWidget);
  });
}
