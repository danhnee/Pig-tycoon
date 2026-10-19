import 'dart:math' as math;
import 'dart:typed_data';

import 'src/polyglot_ffi_stub.dart'
    if (dart.library.ffi) 'src/polyglot_ffi_native.dart';

export 'src/polyglot_ffi_stub.dart' show NativeGdd54SealResult;

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

/// Cross-Platform Polyglot Engine for Flutter (Android Studio Emulator/NDK, Windows PC, Linux, macOS, and Web JS/WASM).
/// - Uses conditional import (`dart.library.ffi`) so `flutter build web` compiles cleanly without `dart:ffi`/`dart:io`.
/// - Implements 64-bit unsigned `xorshift64*` arithmetic using two 32-bit halves (`Uint32List(2)`), guaranteeing
///   identical bit-for-bit checksums (`266653114 | 2091061385 | 2782632973 | 258354468`) on both 64-bit Native VM
///   and 53-bit JavaScript Web runtimes.
/// - Pre-allocates reusable TypedData buffers so repeated ticks on low-end Android Studio emulators incur zero GC allocations.
class PigTycoonPolyglotEngine {
  static const int expectedChecksum1 = 266653114;
  static const int expectedChecksum2 = 2091061385;
  static const int expectedChecksum3 = 2782632973;
  static const int expectedChecksum4 = 258354468;

  // 0x9E3779B97F4A7C15 split into 32-bit unsigned high & low words (Web-safe)
  static const int _seedGoldHi = 0x9E3779B9;
  static const int _seedGoldLo = 0x7F4A7C15;

  // 0x2545F4914F6CDD1D split into 32-bit unsigned high & low words (Web-safe)
  static const int _mulConstHi = 0x2545F491;
  static const int _mulConstLo = 0x4F6CDD1D;

  // Pre-allocated static scratch buffers for zero-GC execution on low-end Android emulators
  static final Uint8List _bfsObstacles = Uint8List(120 * 80);
  static final Int32List _bfsDist = Int32List(120 * 80);
  static final Int32List _bfsQueue = Int32List(120 * 80);

  static final Int32List _boidsPx = Int32List(180);
  static final Int32List _boidsPz = Int32List(180);
  static final Int32List _boidsVx = Int32List(180);
  static final Int32List _boidsVz = Int32List(180);

  static final Int32List _towerAmmo = Int32List(31);
  static final Int32List _towerCd = Int32List(31);
  static final Int32List _raiderHp = Int32List(60);
  static final Int32List _raiderDist = Int32List(60);

  final PlatformPolyglotFfi _ffi = PlatformPolyglotFfi();

  bool get isNativeFfiLoaded => _ffi.isLoaded;
  String get nativeLoadDiagnostic => _ffi.diagnostic;

  // ---------------------------------------------------------------------------
  // Web-safe & VM-safe 64-bit Unsigned Xorshift64* (using two 32-bit words)
  // state[0] = low 32 bits, state[1] = high 32 bits
  // ---------------------------------------------------------------------------

  static void _seed64FromIndex(Uint32List state, int s) {
    // Computes (s * 0x9E3779B97F4A7C15) mod 2^64 using 16-bit limb multiplication
    _mul64To(state, s & 0xFFFFFFFF, 0, _seedGoldLo, _seedGoldHi);
  }

  static void _mul64To(
      Uint32List out, int aLo, int aHi, int bLo, int bHi) {
    final int a0 = aLo & 0xFFFF;
    final int a1 = (aLo >>> 16) & 0xFFFF;
    final int a2 = aHi & 0xFFFF;
    final int a3 = (aHi >>> 16) & 0xFFFF;

    final int b0 = bLo & 0xFFFF;
    final int b1 = (bLo >>> 16) & 0xFFFF;
    final int b2 = bHi & 0xFFFF;
    final int b3 = (bHi >>> 16) & 0xFFFF;

    final int c0 = a0 * b0;
    int r0 = c0 & 0xFFFF;
    int carry = c0 >>> 16;

    final int c1 = carry + a0 * b1 + a1 * b0;
    int r1 = c1 & 0xFFFF;
    carry = c1 >>> 16;

    final int c2 = carry + a0 * b2 + a1 * b1 + a2 * b0;
    int r2 = c2 & 0xFFFF;
    carry = c2 >>> 16;

    final int c3 = carry + a0 * b3 + a1 * b2 + a2 * b1 + a3 * b0;
    int r3 = c3 & 0xFFFF;

    out[0] = ((r1 << 16) | r0) & 0xFFFFFFFF;
    out[1] = ((r3 << 16) | r2) & 0xFFFFFFFF;
  }

  static int _xorshift64starHigh32(Uint32List state, Uint32List scratchProd) {
    int lo = state[0];
    int hi = state[1];
    if (lo == 0 && hi == 0) {
      lo = _seedGoldLo;
      hi = _seedGoldHi;
    }

    // x ^= x >> 12
    final int s1Lo = ((lo >>> 12) | ((hi & 0xFFF) << 20)) & 0xFFFFFFFF;
    final int s1Hi = (hi >>> 12) & 0xFFFFFFFF;
    lo = (lo ^ s1Lo) & 0xFFFFFFFF;
    hi = (hi ^ s1Hi) & 0xFFFFFFFF;

    // x ^= x << 25
    final int s2Lo = ((lo & 0x7F) << 25) & 0xFFFFFFFF;
    final int s2Hi = (((hi & 0x7F) << 25) | (lo >>> 7)) & 0xFFFFFFFF;
    lo = (lo ^ s2Lo) & 0xFFFFFFFF;
    hi = (hi ^ s2Hi) & 0xFFFFFFFF;

    // x ^= x >> 27
    final int s3Lo = ((lo >>> 27) | ((hi & 0x7FFFFFF) << 5)) & 0xFFFFFFFF;
    final int s3Hi = (hi >>> 27) & 0xFFFFFFFF;
    lo = (lo ^ s3Lo) & 0xFFFFFFFF;
    hi = (hi ^ s3Hi) & 0xFFFFFFFF;

    state[0] = lo;
    state[1] = hi;

    _mul64To(scratchProd, lo, hi, _mulConstLo, _mulConstHi);
    return scratchProd[1];
  }

  static int _nextU32(Uint32List state, Uint32List scratchProd, int bound) {
    final int r = _xorshift64starHigh32(state, scratchProd);
    return r % bound;
  }

  // ---------------------------------------------------------------------------
  // Pure Dart Zero-GC TypedData Kernels (Web, Android Emulator & Desktop)
  // ---------------------------------------------------------------------------

  static int dartFlowFieldBfs120x80(int iterations) {
    const int w = 120;
    const int h = 80;
    const int n = w * h;

    final obstacles = _bfsObstacles;
    obstacles.fillRange(0, n, 0);
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

    final dist = _bfsDist;
    final queue = _bfsQueue;
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
        final int di = dist[i];
        if (di > 0) {
          iterSum += di;
        }
      }
      totalChecksum += iterSum;
    }
    return totalChecksum;
  }

  static int dartBoidsHeightfieldStep(int steps) {
    const int agents = 180;
    final px = _boidsPx;
    final pz = _boidsPz;
    final vx = _boidsVx;
    final vz = _boidsVz;

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

        vx[i] = (((vx[i] * 3) ~/ 4) + sepX + pullX + windBiasX).clamp(-600, 600);
        vz[i] = (((vz[i] * 3) ~/ 4) + sepZ + pullZ + windBiasZ).clamp(-600, 600);

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

    final towerAmmo = _towerAmmo..fillRange(0, towers, 50);
    final towerCd = _towerCd..fillRange(0, towers, 0);
    final raiderHp = _raiderHp;
    final raiderDist = _raiderDist;

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
    final rngState = Uint32List(2);
    final scratchProd = Uint32List(2);

    for (int s = 1; s <= seedsCount; s++) {
      _seed64FromIndex(rngState, s);
      int gold = 1200;
      int feedGrams = 250000;
      int adultPigs = 12;
      const int elderPigs = 2;
      int pigWeightTenths = 750;
      int themePricePct = 100;
      int majorWavesSurvived = 0;

      for (int day = 1; day <= 60; day++) {
        if (day % 7 == 1) {
          final int r = _nextU32(rngState, scratchProd, 100);
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
          final int roll = _nextU32(rngState, scratchProd, 1000);
          if (roll < 595) {
            final int litter = 2 + _nextU32(rngState, scratchProd, 3);
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

  /// Pure-Dart 8-direction corner-cut-preventing Flow-Field (step_cost 10/14) matching Rust/C++20 C-ABI bit-for-bit.
  static int dartComputeFlowField8Dir({
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
    final int n = width * height;
    if (width <= 0 ||
        height <= 0 ||
        targetCount <= 0 ||
        obstacles.length < n ||
        targetsX.length < targetCount ||
        targetsY.length < targetCount ||
        outDist.length < n ||
        outVx.length < n ||
        outVy.length < n) {
      return -1;
    }

    const int unreachable = 999999;
    outDist.fillRange(0, n, unreachable);
    outVx.fillRange(0, n, 0.0);
    outVy.fillRange(0, n, 0.0);

    final queue = Int32List(n * 4);
    int head = 0;
    int tail = 0;

    for (int k = 0; k < targetCount; k++) {
      final int tx = targetsX[k];
      final int ty = targetsY[k];
      if (tx >= 0 && tx < width && ty >= 0 && ty < height) {
        final int idx = ty * width + tx;
        if (outDist[idx] != 0) {
          outDist[idx] = 0;
          queue[tail++] = idx;
        }
      }
    }

    const dx = <int>[0, 1, 0, -1, 1, 1, -1, -1];
    const dy = <int>[1, 0, -1, 0, 1, -1, 1, -1];
    const stepCost = <int>[10, 10, 10, 10, 14, 14, 14, 14];

    while (head < tail) {
      final int cur = queue[head++];
      final int cx = cur % width;
      final int cy = cur ~/ width;
      final int currentDist = outDist[cur];

      for (int dir = 0; dir < 8; dir++) {
        final int nx = cx + dx[dir];
        final int ny = cy + dy[dir];
        if (nx < 0 || nx >= width || ny < 0 || ny >= height) continue;
        final int nidx = ny * width + nx;
        if (obstacles[nidx] != 0) continue;

        if (dir >= 4 &&
            (obstacles[cy * width + nx] != 0 ||
                obstacles[ny * width + cx] != 0)) {
          continue;
        }

        final int cand = currentDist + stepCost[dir];
        if (cand < outDist[nidx]) {
          outDist[nidx] = cand;
          if (tail < queue.length) {
            queue[tail++] = nidx;
          }
        }
      }
    }

    int reachableCells = 0;
    for (int y = 0; y < height; y++) {
      for (int x = 0; x < width; x++) {
        final int idx = y * width + x;
        final int d = outDist[idx];
        if (d < unreachable) reachableCells++;
        if (obstacles[idx] != 0 || d == 0 || d >= unreachable) continue;

        int bestDist = d;
        double bvx = 0.0;
        double bvy = 0.0;
        for (int dir = 0; dir < 8; dir++) {
          final int nx = x + dx[dir];
          final int ny = y + dy[dir];
          if (nx < 0 || nx >= width || ny < 0 || ny >= height) continue;
          final int nd = outDist[ny * width + nx];
          if (nd < bestDist) {
            bestDist = nd;
            bvx = dx[dir].toDouble();
            bvy = dy[dir].toDouble();
          }
        }

        final double mag = math.sqrt(bvx * bvx + bvy * bvy);
        if (mag > 0.0001) {
          outVx[idx] = bvx / mag;
          outVy[idx] = bvy / mag;
        }
      }
    }

    return reachableCells;
  }

  /// Evaluates GDD 5.4 4-channel gene appraisal and 3,000-year seal math via Native Rust C-ABI or pure Dart.
  NativeGdd54SealResult evaluateGdd54AndSeal({
    required int channelCode,
    required int rarityCode,
    required int protagonistTag,
    bool preferNativeFfi = true,
  }) {
    if (preferNativeFfi && isNativeFfiLoaded) {
      final res = _ffi.evaluateGdd54AndSeal(
        channelCode: channelCode,
        rarityCode: rarityCode,
        protagonistTag: protagonistTag,
      );
      if (res != null) return res;
    }

    final int sealYears = 24 * 125;
    final int tag = protagonistTag == 1 ? 1 : 0;
    int prob = 0;
    int gold = 0;
    int sta = 0;
    int hrs = 0;

    switch (channelCode) {
      case 0:
        if (rarityCode == 2) {
          prob = 700;
          gold = 300;
          sta = 5;
          hrs = 1;
        } else if (rarityCode == 3) {
          prob = 400;
          gold = 900;
          sta = 5;
          hrs = 1;
        } else {
          prob = 0;
          gold = 0;
          sta = 5;
          hrs = 1;
        }
        break;
      case 1:
        if (rarityCode == 2) {
          prob = 900;
          gold = 450;
          sta = 8;
          hrs = 2;
        } else if (rarityCode == 3) {
          prob = 720;
          gold = 1300;
          sta = 8;
          hrs = 2;
        } else if (rarityCode == 4) {
          prob = 500;
          gold = 3500;
          sta = 8;
          hrs = 2;
        } else if (rarityCode == 5) {
          prob = 350;
          gold = 5000;
          sta = 8;
          hrs = 2;
        } else {
          prob = 1000;
          gold = 250;
          sta = 8;
          hrs = 2;
        }
        break;
      case 2:
        if (rarityCode == 2) {
          prob = 950;
          gold = 700;
          sta = 25;
          hrs = 18;
        } else if (rarityCode == 3) {
          prob = 850;
          gold = 2000;
          sta = 30;
          hrs = 24;
        } else if (rarityCode == 4) {
          prob = 650;
          gold = 6500;
          sta = 40;
          hrs = 36;
        } else if (rarityCode == 5) {
          prob = 500;
          gold = 9000;
          sta = 45;
          hrs = 48;
        } else {
          prob = 1000;
          gold = 300;
          sta = 15;
          hrs = 6;
        }
        break;
      default:
        prob = 1000;
        gold = 0;
        sta = 10;
        hrs = 0;
        break;
    }

    return NativeGdd54SealResult(
      sealYearsPlusTag: sealYears + tag,
      probPermille: prob,
      goldCost: gold,
      staCost: sta,
      hours: hrs,
      usedNativeRust: false,
    );
  }

  int computeFlowField8Dir({
    required int width,
    required int height,
    required Uint8List obstacles,
    required Int32List targetsX,
    required Int32List targetsY,
    required int targetCount,
    required Int32List outDist,
    required Float32List outVx,
    required Float32List outVy,
    bool preferNativeFfi = true,
  }) {
    if (preferNativeFfi && isNativeFfiLoaded) {
      final res = _ffi.computeFlowField8Dir(
        width: width,
        height: height,
        obstacles: obstacles,
        targetsX: targetsX,
        targetsY: targetsY,
        targetCount: targetCount,
        outDist: outDist,
        outVx: outVx,
        outVy: outVy,
      );
      if (res != null && res >= 0) return res;
    }

    return dartComputeFlowField8Dir(
      width: width,
      height: height,
      obstacles: obstacles,
      targetsX: targetsX,
      targetsY: targetsY,
      targetCount: targetCount,
      outDist: outDist,
      outVx: outVx,
      outVy: outVy,
    );
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
        useFfi ? (_ffi.runBfs(bfsIters) ?? dartFlowFieldBfs120x80(bfsIters)) : dartFlowFieldBfs120x80(bfsIters);
    final double d1 = sw.elapsedMicroseconds / 1000.0;

    sw
      ..reset()
      ..start();
    final int c2 = useFfi
        ? (_ffi.runBoids(boidsSteps) ?? dartBoidsHeightfieldStep(boidsSteps))
        : dartBoidsHeightfieldStep(boidsSteps);
    final double d2 = sw.elapsedMicroseconds / 1000.0;

    sw
      ..reset()
      ..start();
    final int c3 = useFfi
        ? (_ffi.runCombat(combatTicks) ?? dartCombatPowerWaveTick(combatTicks))
        : dartCombatPowerWaveTick(combatTicks);
    final double d3 = sw.elapsedMicroseconds / 1000.0;

    sw
      ..reset()
      ..start();
    final int c4 = useFfi
        ? (_ffi.runEconomy(economySeeds) ?? dartMonteCarloEconomy60d(economySeeds))
        : dartMonteCarloEconomy60d(economySeeds);
    final double d4 = sw.elapsedMicroseconds / 1000.0;

    return PolyglotBenchmarkMetrics(
      backendName: useFfi
          ? 'Rust Native C-ABI FFI (pig_tycoon_native)'
          : 'Dart Zero-GC TypedData Kernel (Web / Android / Desktop)',
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
