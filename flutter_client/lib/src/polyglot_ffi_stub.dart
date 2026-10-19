import 'dart:typed_data';

class NativeGdd54SealResult {
  final int sealYearsPlusTag;
  final int probPermille;
  final int goldCost;
  final int staCost;
  final int hours;
  final bool usedNativeRust;

  const NativeGdd54SealResult({
    required this.sealYearsPlusTag,
    required this.probPermille,
    required this.goldCost,
    required this.staCost,
    required this.hours,
    required this.usedNativeRust,
  });
}

class PlatformPolyglotFfi {
  bool get isLoaded => false;
  String get diagnostic => 'Web/JS runtime (dart:ffi unavailable; using zero-GC TypedData kernel)';

  int? runBfs(int iterations) => null;
  int? runBoids(int steps) => null;
  int? runCombat(int ticks) => null;
  int? runEconomy(int seedsCount) => null;

  int? computeFlowField8Dir({
    required int width,
    required int height,
    required Uint8List obstacles,
    required Int32List targetsX,
    required Int32List targetsY,
    required int targetCount,
    required Int32List outDist,
    required Float32List outVx,
    required Float32List outVy,
  }) =>
      null;

  NativeGdd54SealResult? evaluateGdd54AndSeal({
    required int channelCode,
    required int rarityCode,
    required int protagonistTag,
  }) =>
      null;
}
