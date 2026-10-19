import 'dart:ffi' as ffi;
import 'dart:io' show Directory, File, Platform;
import 'dart:typed_data';

import 'polyglot_ffi_stub.dart';

export 'polyglot_ffi_stub.dart' show NativeGdd54SealResult;

typedef _NativeBfsC = ffi.Int64 Function(ffi.Int32 iterations);
typedef _NativeBfsDart = int Function(int iterations);

typedef _NativeBoidsC = ffi.Int64 Function(ffi.Int32 steps);
typedef _NativeBoidsDart = int Function(int steps);

typedef _NativeCombatC = ffi.Int64 Function(ffi.Int32 ticks);
typedef _NativeCombatDart = int Function(int ticks);

typedef _NativeEconomyC = ffi.Int64 Function(ffi.Int32 seedsCount);
typedef _NativeEconomyDart = int Function(int seedsCount);

typedef _NativeFlowField8DirC = ffi.Int32 Function(
  ffi.Int32 width,
  ffi.Int32 height,
  ffi.Pointer<ffi.Uint8> obstacles,
  ffi.Pointer<ffi.Int32> targetsX,
  ffi.Pointer<ffi.Int32> targetsY,
  ffi.Int32 targetCount,
  ffi.Pointer<ffi.Int32> outDist,
  ffi.Pointer<ffi.Float> outVx,
  ffi.Pointer<ffi.Float> outVy,
);
typedef _NativeFlowField8DirDart = int Function(
  int width,
  int height,
  ffi.Pointer<ffi.Uint8> obstacles,
  ffi.Pointer<ffi.Int32> targetsX,
  ffi.Pointer<ffi.Int32> targetsY,
  int targetCount,
  ffi.Pointer<ffi.Int32> outDist,
  ffi.Pointer<ffi.Float> outVx,
  ffi.Pointer<ffi.Float> outVy,
);

typedef _NativeEvaluateGdd54C = ffi.Int32 Function(
  ffi.Int32 channel,
  ffi.Int32 rarity,
  ffi.Int32 protagonist,
  ffi.Pointer<ffi.Int32> outProbPermille,
  ffi.Pointer<ffi.Int32> outGoldCost,
  ffi.Pointer<ffi.Int32> outStaCost,
  ffi.Pointer<ffi.Int32> outHours,
);
typedef _NativeEvaluateGdd54Dart = int Function(
  int channel,
  int rarity,
  int protagonist,
  ffi.Pointer<ffi.Int32> outProbPermille,
  ffi.Pointer<ffi.Int32> outGoldCost,
  ffi.Pointer<ffi.Int32> outStaCost,
  ffi.Pointer<ffi.Int32> outHours,
);

typedef _CallocC = ffi.Pointer<ffi.Void> Function(ffi.IntPtr num, ffi.IntPtr size);
typedef _CallocDart = ffi.Pointer<ffi.Void> Function(int num, int size);
typedef _FreeC = ffi.Void Function(ffi.Pointer<ffi.Void> ptr);
typedef _FreeDart = void Function(ffi.Pointer<ffi.Void> ptr);

class _StdlibAllocator {
  late final _CallocDart _calloc;
  late final _FreeDart _free;

  _StdlibAllocator() {
    final ffi.DynamicLibrary stdlib = Platform.isWindows
        ? ffi.DynamicLibrary.open('msvcrt.dll')
        : ffi.DynamicLibrary.process();
    _calloc = stdlib
        .lookup<ffi.NativeFunction<_CallocC>>('calloc')
        .asFunction<_CallocDart>();
    _free = stdlib
        .lookup<ffi.NativeFunction<_FreeC>>('free')
        .asFunction<_FreeDart>();
  }

  ffi.Pointer<T> allocate<T extends ffi.NativeType>(int count, int byteSizePerElem) {
    final ptr = _calloc(count, byteSizePerElem);
    if (ptr.address == 0) {
      throw StateError('Native calloc($count, $byteSizePerElem) failed');
    }
    return ptr.cast<T>();
  }

  void free(ffi.Pointer<ffi.NativeType> ptr) {
    if (ptr.address != 0) {
      _free(ptr.cast<ffi.Void>());
    }
  }
}

class PlatformPolyglotFfi {
  ffi.DynamicLibrary? _rustLib;
  _StdlibAllocator? _allocator;
  _NativeBfsDart? _rustBfs;
  _NativeBoidsDart? _rustBoids;
  _NativeCombatDart? _rustCombat;
  _NativeEconomyDart? _rustEconomy;
  _NativeFlowField8DirDart? _rustFlowField8Dir;
  _NativeEvaluateGdd54Dart? _rustEvaluateGdd54;
  String _diagnostic = 'NotInitialized';

  bool get isLoaded => _rustLib != null;
  String get diagnostic => _diagnostic;

  PlatformPolyglotFfi() {
    _tryLoadNativeLibrary();
  }

  void _tryLoadNativeLibrary() {
    final attemptedErrors = <String>[];
    try {
      final envOverride = Platform.environment['PIG_TYCOON_NATIVE_LIB'];
      final candidates = <String>[];
      if (envOverride != null && envOverride.trim().isNotEmpty) {
        candidates.add(envOverride.trim());
      }

      if (Platform.isWindows) {
        final cwd = Directory.current.path;
        candidates.addAll([
          '$cwd/../native_polyglot/bin/pig_tycoon_native.dll',
          '$cwd/../native_polyglot/rust_core/target/release/pig_tycoon_native.dll',
          '$cwd/native_polyglot/bin/pig_tycoon_native.dll',
          '$cwd/native_polyglot/rust_core/target/release/pig_tycoon_native.dll',
          'pig_tycoon_native.dll',
        ]);
      } else if (Platform.isAndroid || Platform.isLinux) {
        final cwd = Directory.current.path;
        candidates.addAll([
          '$cwd/../native_polyglot/bin/libpig_tycoon_native.so',
          '$cwd/native_polyglot/bin/libpig_tycoon_native.so',
          'libpig_tycoon_native.so',
        ]);
      } else if (Platform.isMacOS || Platform.isIOS) {
        candidates.add('libpig_tycoon_native.dylib');
      }

      for (final path in candidates) {
        final isBareName = !path.contains('/') && !path.contains('\\');
        if (!isBareName && !File(path).existsSync()) {
          continue;
        }
        try {
          final lib = ffi.DynamicLibrary.open(path);
          _bindLibrary(lib);
          _allocator = _StdlibAllocator();
          _diagnostic = 'Loaded native C-ABI ($path)';
          return;
        } catch (e) {
          attemptedErrors.add('$path -> $e');
        }
      }

      _rustLib = null;
      _diagnostic = attemptedErrors.isEmpty
          ? 'No native C-ABI binary found in candidate paths; using zero-GC TypedData Dart kernel'
          : 'Native C-ABI open failed (${attemptedErrors.join('; ')}); using zero-GC TypedData Dart kernel';
    } catch (e) {
      _rustLib = null;
      _diagnostic =
          'Native probe exception ($e); using zero-GC TypedData Dart kernel';
    }
  }

  void _bindLibrary(ffi.DynamicLibrary lib) {
    _rustBfs = lib
        .lookup<ffi.NativeFunction<_NativeBfsC>>('pt_flowfield_bfs_120x80')
        .asFunction<_NativeBfsDart>();
    _rustBoids = lib
        .lookup<ffi.NativeFunction<_NativeBoidsC>>('pt_boids_heightfield_step')
        .asFunction<_NativeBoidsDart>();
    _rustCombat = lib
        .lookup<ffi.NativeFunction<_NativeCombatC>>(
            'pt_combat_power_wave_tick')
        .asFunction<_NativeCombatDart>();
    _rustEconomy = lib
        .lookup<ffi.NativeFunction<_NativeEconomyC>>(
            'pt_montecarlo_economy_60d')
        .asFunction<_NativeEconomyDart>();
    _rustFlowField8Dir = lib
        .lookup<ffi.NativeFunction<_NativeFlowField8DirC>>(
            'pt_compute_flowfield_8dir')
        .asFunction<_NativeFlowField8DirDart>();
    _rustEvaluateGdd54 = lib
        .lookup<ffi.NativeFunction<_NativeEvaluateGdd54C>>(
            'pt_evaluate_gdd54_and_seal_api')
        .asFunction<_NativeEvaluateGdd54Dart>();
    _rustLib = lib;
  }

  int? runBfs(int iterations) => _rustBfs?.call(iterations);
  int? runBoids(int steps) => _rustBoids?.call(steps);
  int? runCombat(int ticks) => _rustCombat?.call(ticks);
  int? runEconomy(int seedsCount) => _rustEconomy?.call(seedsCount);

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
  }) {
    final fn = _rustFlowField8Dir;
    final alloc = _allocator;
    if (fn == null || alloc == null) return null;

    final int n = width * height;
    final obsPtr = alloc.allocate<ffi.Uint8>(n, 1);
    final txPtr = alloc.allocate<ffi.Int32>(targetCount, 4);
    final tyPtr = alloc.allocate<ffi.Int32>(targetCount, 4);
    final distPtr = alloc.allocate<ffi.Int32>(n, 4);
    final vxPtr = alloc.allocate<ffi.Float>(n, 4);
    final vyPtr = alloc.allocate<ffi.Float>(n, 4);

    try {
      obsPtr.asTypedList(n).setRange(0, n, obstacles);
      txPtr.asTypedList(targetCount).setRange(0, targetCount, targetsX);
      tyPtr.asTypedList(targetCount).setRange(0, targetCount, targetsY);

      final reachable = fn(
        width,
        height,
        obsPtr,
        txPtr,
        tyPtr,
        targetCount,
        distPtr,
        vxPtr,
        vyPtr,
      );

      if (reachable >= 0) {
        outDist.setRange(0, n, distPtr.asTypedList(n));
        outVx.setRange(0, n, vxPtr.asTypedList(n));
        outVy.setRange(0, n, vyPtr.asTypedList(n));
      }
      return reachable;
    } finally {
      alloc.free(obsPtr);
      alloc.free(txPtr);
      alloc.free(tyPtr);
      alloc.free(distPtr);
      alloc.free(vxPtr);
      alloc.free(vyPtr);
    }
  }

  NativeGdd54SealResult? evaluateGdd54AndSeal({
    required int channelCode,
    required int rarityCode,
    required int protagonistTag,
  }) {
    final fn = _rustEvaluateGdd54;
    final alloc = _allocator;
    if (fn == null || alloc == null) return null;

    final probPtr = alloc.allocate<ffi.Int32>(1, 4);
    final goldPtr = alloc.allocate<ffi.Int32>(1, 4);
    final staPtr = alloc.allocate<ffi.Int32>(1, 4);
    final hrsPtr = alloc.allocate<ffi.Int32>(1, 4);

    try {
      final ret = fn(
        channelCode,
        rarityCode,
        protagonistTag,
        probPtr,
        goldPtr,
        staPtr,
        hrsPtr,
      );
      return NativeGdd54SealResult(
        sealYearsPlusTag: ret,
        probPermille: probPtr.value,
        goldCost: goldPtr.value,
        staCost: staPtr.value,
        hours: hrsPtr.value,
        usedNativeRust: true,
      );
    } finally {
      alloc.free(probPtr);
      alloc.free(goldPtr);
      alloc.free(staPtr);
      alloc.free(hrsPtr);
    }
  }
}
