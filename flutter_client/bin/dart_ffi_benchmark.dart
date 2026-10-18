import '../lib/pig_tycoon_polyglot_engine.dart';

void main() {
  final engine = PigTycoonPolyglotEngine();

  // Warmup Dart VM & FFI
  engine.runBenchmarks(
    preferNativeFfi: false,
    bfsIters: 10,
    boidsSteps: 50,
    combatTicks: 200,
    economySeeds: 10,
  );
  if (engine.isNativeFfiLoaded) {
    engine.runBenchmarks(
      preferNativeFfi: true,
      bfsIters: 10,
      boidsSteps: 50,
      combatTicks: 200,
      economySeeds: 10,
    );
  }

  final dartVm = engine.runBenchmarks(
    preferNativeFfi: false,
    bfsIters: 500,
    boidsSteps: 5000,
    combatTicks: 50000,
    economySeeds: 2000,
  );

  print('LANGUAGE=Dart VM / Web Fallback (Dart 3.11.4 TypedData)');
  print('KERNEL1_BFS_MS=${dartVm.kernel1BfsMs.toStringAsFixed(3)}');
  print('KERNEL2_BOIDS_MS=${dartVm.kernel2BoidsMs.toStringAsFixed(3)}');
  print('KERNEL3_COMBAT_MS=${dartVm.kernel3CombatMs.toStringAsFixed(3)}');
  print('KERNEL4_ECONOMY_MS=${dartVm.kernel4EconomyMs.toStringAsFixed(3)}');
  print('TOTAL_MS=${dartVm.totalMs.toStringAsFixed(3)}');
  print(
      'CHECKSUMS=${dartVm.checksum1}|${dartVm.checksum2}|${dartVm.checksum3}|${dartVm.checksum4}');
  print('BIT_EXACT=${dartVm.isBitExact}');

  if (engine.isNativeFfiLoaded) {
    final ffiRes = engine.runBenchmarks(
      preferNativeFfi: true,
      bfsIters: 500,
      boidsSteps: 5000,
      combatTicks: 50000,
      economySeeds: 2000,
    );
    print('---');
    print('LANGUAGE=Flutter/Dart Native FFI (dart:ffi -> pig_tycoon_native.dll)');
    print('KERNEL1_BFS_MS=${ffiRes.kernel1BfsMs.toStringAsFixed(3)}');
    print('KERNEL2_BOIDS_MS=${ffiRes.kernel2BoidsMs.toStringAsFixed(3)}');
    print('KERNEL3_COMBAT_MS=${ffiRes.kernel3CombatMs.toStringAsFixed(3)}');
    print('KERNEL4_ECONOMY_MS=${ffiRes.kernel4EconomyMs.toStringAsFixed(3)}');
    print('TOTAL_MS=${ffiRes.totalMs.toStringAsFixed(3)}');
    print(
        'CHECKSUMS=${ffiRes.checksum1}|${ffiRes.checksum2}|${ffiRes.checksum3}|${ffiRes.checksum4}');
    print('BIT_EXACT=${ffiRes.isBitExact}');
  }
}
