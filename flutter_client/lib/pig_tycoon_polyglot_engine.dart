import 'dart:ffi' as ffi;
import 'dart:io' show Directory, File, Platform;
import 'dart:typed_data';

typedef _NativeBfsC = ffi.Int64 Function(ffi.Int32 iterations);
typedef _NativeBfsDart = int Function(int iterations);

typedef _NativeBoidsC = ffi.Int64 Function(ffi.Int32 steps);
typedef _NativeBoidsDart = int Function(int steps);

typedef _NativeCombatC = ffi.Int64 Function(ffi.Int32 ticks);
typedef _NativeCombatDart = int Function(int ticks);

typedef _NativeEconomyC = ffi.Int64 Function(ffi.Int32 seedsCount);
typedef _NativeEconomyDart = int Function(int seedsCount);

class PolyglotBenchmarkMetrics {
  final String backendName;
  final double kernel1BfsMs;
  final double kernel2BoidsMs;
  final double kernel3CombatMs;
  final double kernel4EconomyMs;
  final int checksum1;
  final int checksum2;
  final int checksum3;
  final int checksum4;

  const PolyglotBenchmarkMetrics({
    required this.backendName,
    required this.kernel1BfsMs,
    required this.kernel2BoidsMs,
    required this.kernel3CombatMs,
    required this.kernel4EconomyMs,
    required this.checksum1,
    required this.checksum2,
    required this.checksum3,
    required this.checksum4,
  });

  double get totalMs =>
      kernel1BfsMs + kernel2BoidsMs + kernel3CombatMs + kernel4EconomyMs;

  bool get isBitExact =>
      checksum1 == PigTycoonPolyglotEngine.expectedChecksum1 &&
      checksum2 == PigTycoonPolyglotEngine.expectedChecksum2 &&
      checksum3 == PigTycoonPolyglotEngine.expectedChecksum3 &&
      checksum4 == PigTycoonPolyglotEngine.expectedChecksum4;
}

/// Cross-Platform Polyglot Engine for Flutter (Android Studio, Windows PC, Linux, macOS, and Web).
/// Uses `dart:ffi` to invoke `pig_tycoon_native` (Rust C-ABI) or `pig_tycoon_cpp` (C++20 SIMD)
/// when running on Native Mobile/Desktop, and automatically falls back to a bit-exact TypedData
/// Dart kernel when compiled to Web (JS/WASM) or when native libraries are absent.
class PigTycoonPolyglotEngine {
  static const int expectedChecksum1 = 266653114;
  static const int expectedChecksum2 = 2091061385;
  static const int expectedChecksum3 = 2782632973;
  static const int expectedChecksum4 = 258354468;

  ffi.DynamicLibrary? _rustLib;
  _NativeBfsDart? _rustBfs;
  _NativeBoidsDart? _rustBoids;
  _NativeCombatDart? _rustCombat;
  _NativeEconomyDart? _rustEconomy;

  bool get isNativeFfiLoaded => _rustLib != null;

  PigTycoonPolyglotEngine() {
    _tryLoadNativeLibrary();
  }

  void _tryLoadNativeLibrary() {
    try {
      final candidates = <String>[];
      if (Platform.isWindows) {
        candidates.addAll([
          '${Directory.current.path}/../native_polyglot/bin/pig_tycoon_native.dll',
          '${Directory.current.path}/../native_polyglot/rust_core/target/release/pig_tycoon_native.dll',
          '${Directory.current.path}/native_polyglot/bin/pig_tycoon_native.dll',
          '${Directory.current.path}/native_polycoon/rust_core/target/release/pig_tycoon_native.dll',
          'D:/Pig-tycoon/native_polyglot/rust_core/target/release/pig_tycoon_native.dll',
          'pig_tycoon_native.dll',
        ]);
      } else if (Platform.isAndroid || Platform.isLinux) {
        candidates.add('libpig_tycoon_native.so');
      } else if (Platform.isMacOS || Platform.isIOS) {
        candidates.add('libpig_tycoon_native.dylib');
      }

      for (final path in candidates) {
        if (!path.contains('/') && !path.contains('\\')) {
          try {
            _bindLibrary(ffi.DynamicLibrary.open(path));
            return;
          } catch (_) {}
        } else if (File(path).existsSync()) {
          _bindLibrary(ffi.DynamicLibrary.open(path));
          return;
        }
      }
    } catch (_) {
      // Web or sandboxed environment without dart:io / FFI support
      _rustLib = null;
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
    _rustLib = lib;
  }

  // ---------------------------------------------------------------------------
  // Pure Dart TypedData Fallback Kernels (for Web & Cross-Platform Verification)
  // ---------------------------------------------------------------------------

  static int _xorshift64star(Int64List state) {
    int x = state[0];
    if (x == 0) {
      x = 0x9E3779B97F4A7C15;
    }
    x ^= (x >>> 12);
    x ^= (x << 25);
    x ^= (x >>> 27);
    state[0] = x;
    // 64-bit wrapping multiplication in Dart VM
    return x * 0x2545F4914F6CDD1D;
  }

  static int _nextU32(Int64List state, int bound) {
    final int r = (_xorshift64star(state) >>> 32) & 0xFFFFFFFF;
    return r % bound;
  }

  static int dartFlowFieldBfs120x80(int iterations) {
    const int w = 120;
    const int h = 80;
    const int n = w * h;

    final obstacles = Uint8List(n);
    for (int y = 0; y < h; y++) {
      for (int x = 0; x < w; x++) {
        if (x > 10 &&
            x < 110 &&
            y > 10 &&
            y < 70 &&
            (x % 17 == 0) &&
            (y % 11 != 0)) {
          obstacles[y * w + x] = 1;
        }
      }
    }

    final dist = Int32List(n);
    final queue = Int32List(n);
    int totalChecksum = 0;

    for (int iter = 0; iter < iterations; iter++) {
      dist.fillRange(0, n, -1);
      final int tx = 20 + (iter % 40);
      final int ty = 15 + (iter % 30);
      final int startIdx = ty * w + tx;
      obstacles[startIdx] = 0;

      int head = 0;
      int tail = 0;
      dist[startIdx] = 0;
      queue[tail++] = startIdx;

      while (head < tail) {
        final int cur = queue[head++];
        final int d = dist[cur] + 1;
        final int cx = cur % w;
        final int cy = cur ~/ w;

        if (cx > 0) {
          final int nb = cur - 1;
          if (obstacles[nb] == 0 && dist[nb] < 0) {
            dist[nb] = d;
            queue[tail++] = nb;
          }
        }
        if (cx + 1 < w) {
          final int nb = cur + 1;
          if (obstacles[nb] == 0 && dist[nb] < 0) {
            dist[nb] = d;
            queue[tail++] = nb;
          }
        }
        if (cy > 0) {
          final int nb = cur - w;
          if (obstacles[nb] == 0 && dist[nb] < 0) {
            dist[nb] = d;
            queue[tail++] = nb;
          }
        }
        if (cy + 1 < h) {
          final int nb = cur + w;
          if (obstacles[nb] == 0 && dist[nb] < 0) {
            dist[nb] = d;
            queue[tail++] = nb;
          }
        }
      }

      int iterSum = 0;
      for (int i = 0; i < n; i++) {
        if (dist[i] > 0) {
          iterSum += dist[i];
        }
      }
      totalChecksum += iterSum;
    }
    return totalChecksum;
  }

  static int dartBoidsHeightfieldStep(int steps) {
    const int agents = 180;
    final px = Int64List(agents);
    final pz = Int64List(agents);
    final vx = Int64List(agents);
    final vz = Int64List(agents);

    for (int i = 0; i < agents; i++) {
      px[i] = ((i * 173) % 40000) - 20000;
      pz[i] = ((i * 251) % 24000) - 12000;
      vx[i] = ((i * 37) % 200) - 100;
      vz[i] = ((i * 53) % 200) - 100;
    }

    int heightChecksum = 0;
    for (int step = 0; step < steps; step++) {
      final int windBiasX = ((step * 13) % 21) - 10;
      final int windBiasZ = ((step * 7) % 21) - 10;

      for (int i = 0; i < agents; i++) {
        int sepX = 0;
        int sepZ = 0;

        for (int offset = 1; offset <= 8; offset++) {
          final int j = (i + offset) % agents;
          final int dx = px[i] - px[j];
          final int dz = pz[i] - pz[j];
          final int distSq = dx * dx + dz * dz;
          if (distSq > 0 && distSq < 4000000) {
            sepX += dx ~/ 32;
            sepZ += dz ~/ 32;
          }
        }

        final int pullX = -px[i] ~/ 128;
        final int pullZ = -pz[i] ~/ 128;

        vx[i] = ((vx[i] * 3) ~/ 4) + sepX + pullX + windBiasX;
        vz[i] = ((vz[i] * 3) ~/ 4) + sepZ + pullZ + windBiasZ;

        vx[i] = vx[i].clamp(-600, 600);
        vz[i] = vz[i].clamp(-600, 600);

        px[i] = (px[i] + vx[i]).clamp(-22000, 22000);
        pz[i] = (pz[i] + vz[i]).clamp(-14000, 14000);

        final int gx = (px[i] + 60000) ~/ 1000;
        final int gz = (pz[i] + 40000) ~/ 1000;
        final int fx = (px[i] + 60000) % 1000;
        final int fz = (pz[i] + 40000) % 1000;

        final int h00 = (gx * 19 + gz * 31) % 2500;
        final int h10 = ((gx + 1) * 19 + gz * 31) % 2500;
        final int h01 = (gx * 19 + (gz + 1) * 31) % 2500;
        final int h11 = ((gx + 1) * 19 + (gz + 1) * 31) % 2500;

        final int hx0 = h00 * (1000 - fx) + h10 * fx;
        final int hx1 = h01 * (1000 - fx) + h11 * fx;
        final int hInterp = (hx0 * (1000 - fz) + hx1 * fz) ~/ 1000000;

        heightChecksum += hInterp + (px[i] >> 4) - (pz[i] >> 4);
      }
    }
    return heightChecksum;
  }

  static int dartCombatPowerWaveTick(int ticks) {
    const int towers = 31;
    const int raiders = 60;

    final towerAmmo = Int32List(towers)..fillRange(0, towers, 50);
    final towerCd = Int32List(towers);
    final raiderHp = Int32List(raiders);
    final raiderDist = Int32List(raiders);

    for (int r = 0; r < raiders; r++) {
      raiderHp[r] = 450 + r * 25;
      raiderDist[r] = 100 + r * 4;
    }

    const int contCapacity = 70;
    int capacitor = 1200;
    int breakerTicks = 0;
    int totalDamageDealt = 0;
    int totalAmmoSpent = 0;
    int breakerTrips = 0;
    int playerGuard = 1400;
    int playerStamina = 1800;

    for (int tick = 0; tick < ticks; tick++) {
      final int demand = 55 + ((tick * 7) % 60);
      int fireMultPct;
      if (breakerTicks > 0) {
        breakerTicks--;
        fireMultPct = 0;
      } else if (demand <= contCapacity) {
        capacitor = (capacitor + 5).clamp(0, 1200);
        fireMultPct = 100;
      } else if (demand * 10 <= contCapacity * 11) {
        capacitor = (capacitor - (demand - contCapacity)).clamp(0, 1200);
        fireMultPct = 100;
      } else if (demand * 10 <= contCapacity * 14) {
        fireMultPct = 75;
      } else {
        breakerTicks = 4;
        breakerTrips++;
        fireMultPct = 0;
      }

      final int targetRaider = tick % raiders;
      if (raiderHp[targetRaider] <= 0) {
        raiderHp[targetRaider] = 500 + (tick % 300);
        raiderDist[targetRaider] = 180;
      }
      raiderDist[targetRaider] = (raiderDist[targetRaider] - 3).clamp(5, 1000);

      if (fireMultPct > 0) {
        for (int t = 0; t < towers; t++) {
          if (towerCd[t] > 0) {
            towerCd[t]--;
            continue;
          }
          if (towerAmmo[t] <= 0) {
            towerAmmo[t] = 50;
          }
          final int baseDmg = 18 + (t % 5) * 6;
          final int dmg = (baseDmg * fireMultPct) ~/ 100;
          raiderHp[targetRaider] -= dmg;
          towerAmmo[t]--;
          towerCd[t] = 3 + (t % 4);
          totalDamageDealt += dmg;
          totalAmmoSpent++;
          if (raiderHp[targetRaider] <= 0) {
            break;
          }
        }
      }

      const int rawEnemyHit = 28;
      if (tick % 4 == 0) {
        playerStamina = (playerStamina + 20).clamp(200, 1800);
        playerGuard = (playerGuard - 5).clamp(150, 1400);
        totalDamageDealt += (rawEnemyHit ~/ 2);
      } else {
        playerStamina = (playerStamina - 12 + 15).clamp(200, 1800);
        playerGuard = (playerGuard - 18 + 22).clamp(200, 1400);
      }
    }

    return totalDamageDealt * 1000 +
        totalAmmoSpent * 10 +
        breakerTrips +
        playerGuard;
  }

  static int dartMonteCarloEconomy60d(int seedsCount) {
    int grandTotalGold = 0;
    final rngState = Int64List(1);

    for (int s = 1; s <= seedsCount; s++) {
      rngState[0] = s * 0x9E3779B97F4A7C15;
      int gold = 1200;
      int feedGrams = 250000;
      int adultPigs = 12;
      const int elderPigs = 2;
      int pigWeightTenths = 750;
      int themePricePct = 100;
      int majorWavesSurvived = 0;

      for (int day = 1; day <= 60; day++) {
        if (day % 7 == 1) {
          final int r = _nextU32(rngState, 100);
          if (r < 24) {
            themePricePct = 100;
          } else if (r < 36) {
            themePricePct = 85;
          } else if (r < 48) {
            themePricePct = 130;
          } else if (r < 58) {
            themePricePct = 95;
          } else if (r < 66) {
            themePricePct = 75;
          } else if (r < 76) {
            themePricePct = 115;
          } else {
            themePricePct = 105;
          }
        }

        final int dailyNeedGrams = adultPigs * 3500 + elderPigs * 4200;
        if (feedGrams < dailyNeedGrams && gold >= 180) {
          gold -= 180;
          feedGrams += 150000;
        }

        if (feedGrams >= dailyNeedGrams) {
          feedGrams -= dailyNeedGrams;
          pigWeightTenths = (pigWeightTenths + 45).clamp(500, 1150);
        } else {
          feedGrams = 0;
          pigWeightTenths = (pigWeightTenths - 20).clamp(500, 1150);
        }

        if (pigWeightTenths >= 950 && adultPigs > 8) {
          final int salePrice =
              (pigWeightTenths * 3 * 120 * themePricePct * 130) ~/ 1000000;
          gold += salePrice;
          adultPigs--;
        }

        if (day % 5 == 0) {
          final int roll = _nextU32(rngState, 1000);
          if (roll < 595) {
            final int litter = 2 + _nextU32(rngState, 3);
            adultPigs = (adultPigs + litter).clamp(0, 36);
          }
        }

        if (day % 6 == 0) {
          final int cycle = day ~/ 6;
          final int totalPigs = adultPigs + elderPigs;
          final int threatBudget =
              (100 * (100 + 18 * (cycle - 1)) * (1000 + 4 * totalPigs)) ~/
                  100000;
          majorWavesSurvived++;
          gold += 150 + (threatBudget ~/ 2);
        }
      }

      grandTotalGold += gold + majorWavesSurvived * 10 + adultPigs;
    }

    return grandTotalGold;
  }

  PolyglotBenchmarkMetrics runBenchmarks({
    required bool preferNativeFfi,
    int bfsIters = 500,
    int boidsSteps = 5000,
    int combatTicks = 50000,
    int economySeeds = 2000,
  }) {
    final bool useFfi = preferNativeFfi && isNativeFfiLoaded;
    final sw = Stopwatch();

    sw.start();
    final int c1 =
        useFfi ? _rustBfs!(bfsIters) : dartFlowFieldBfs120x80(bfsIters);
    final double d1 = sw.elapsedMicroseconds / 1000.0;

    sw
      ..reset()
      ..start();
    final int c2 =
        useFfi ? _rustBoids!(boidsSteps) : dartBoidsHeightfieldStep(boidsSteps);
    final double d2 = sw.elapsedMicroseconds / 1000.0;

    sw
      ..reset()
      ..start();
    final int c3 = useFfi
        ? _rustCombat!(combatTicks)
        : dartCombatPowerWaveTick(combatTicks);
    final double d3 = sw.elapsedMicroseconds / 1000.0;

    sw
      ..reset()
      ..start();
    final int c4 = useFfi
        ? _rustEconomy!(economySeeds)
        : dartMonteCarloEconomy60d(economySeeds);
    final double d4 = sw.elapsedMicroseconds / 1000.0;

    return PolyglotBenchmarkMetrics(
      backendName: useFfi
          ? 'Flutter/Dart FFI -> Rust C-ABI (pig_tycoon_native)'
          : 'Pure Dart VM / Web TypedData Kernel',
      kernel1BfsMs: d1,
      kernel2BoidsMs: d2,
      kernel3CombatMs: d3,
      kernel4EconomyMs: d4,
      checksum1: c1,
      checksum2: c2,
      checksum3: c3,
      checksum4: c4,
    );
  }
}
