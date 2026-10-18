import java.util.Arrays;

public class PigTycoonBenchmark {

    private static long xorshift64star(long[] state) {
        long x = state[0];
        if (x == 0L) {
            x = 0x9E3779B97F4A7C15L;
        }
        x ^= (x >>> 12);
        x ^= (x << 25);
        x ^= (x >>> 27);
        state[0] = x;
        return x * 0x2545F4914F6CDD1DL;
    }

    private static int nextU32(long[] state, int bound) {
        long r = (xorshift64star(state) >>> 32) & 0xFFFFFFFFL;
        return (int) (Long.divideUnsigned(r, bound) * 0 + Long.remainderUnsigned(r, bound));
    }

    private static long clampI64(long v, long lo, long hi) {
        return v < lo ? lo : (v > hi ? hi : v);
    }

    private static int clampI32(int v, int lo, int hi) {
        return v < lo ? lo : (v > hi ? hi : v);
    }

    public static long flowfieldBfs120x80(int iterations) {
        final int W = 120;
        final int H = 80;
        final int N = W * H;

        byte[] obstacles = new byte[N];
        for (int y = 0; y < H; y++) {
            for (int x = 0; x < W; x++) {
                if (x > 10 && x < 110 && y > 10 && y < 70 && (x % 17 == 0) && (y % 11 != 0)) {
                    obstacles[y * W + x] = 1;
                }
            }
        }

        int[] dist = new int[N];
        int[] queue = new int[N];
        long totalChecksum = 0L;

        for (int iter = 0; iter < iterations; iter++) {
            Arrays.fill(dist, -1);
            int tx = 20 + (iter % 40);
            int ty = 15 + (iter % 30);
            int startIdx = ty * W + tx;
            obstacles[startIdx] = 0;

            int head = 0;
            int tail = 0;
            dist[startIdx] = 0;
            queue[tail++] = startIdx;

            while (head < tail) {
                int cur = queue[head++];
                int d = dist[cur] + 1;
                int cx = cur % W;
                int cy = cur / W;

                if (cx > 0) {
                    int nb = cur - 1;
                    if (obstacles[nb] == 0 && dist[nb] < 0) {
                        dist[nb] = d;
                        queue[tail++] = nb;
                    }
                }
                if (cx + 1 < W) {
                    int nb = cur + 1;
                    if (obstacles[nb] == 0 && dist[nb] < 0) {
                        dist[nb] = d;
                        queue[tail++] = nb;
                    }
                }
                if (cy > 0) {
                    int nb = cur - W;
                    if (obstacles[nb] == 0 && dist[nb] < 0) {
                        dist[nb] = d;
                        queue[tail++] = nb;
                    }
                }
                if (cy + 1 < H) {
                    int nb = cur + W;
                    if (obstacles[nb] == 0 && dist[nb] < 0) {
                        dist[nb] = d;
                        queue[tail++] = nb;
                    }
                }
            }

            long iterSum = 0L;
            for (int i = 0; i < N; i++) {
                if (dist[i] > 0) {
                    iterSum += dist[i];
                }
            }
            totalChecksum += iterSum;
        }
        return totalChecksum;
    }

    public static long boidsHeightfieldStep(int steps) {
        final int AGENTS = 180;
        long[] px = new long[AGENTS];
        long[] pz = new long[AGENTS];
        long[] vx = new long[AGENTS];
        long[] vz = new long[AGENTS];

        for (int i = 0; i < AGENTS; i++) {
            long idx = i;
            px[i] = ((idx * 173) % 40000) - 20000;
            pz[i] = ((idx * 251) % 24000) - 12000;
            vx[i] = ((idx * 37) % 200) - 100;
            vz[i] = ((idx * 53) % 200) - 100;
        }

        long heightChecksum = 0L;
        for (int step = 0; step < steps; step++) {
            long windBiasX = (((long) step * 13) % 21) - 10;
            long windBiasZ = (((long) step * 7) % 21) - 10;

            for (int i = 0; i < AGENTS; i++) {
                long sepX = 0L;
                long sepZ = 0L;

                for (int offset = 1; offset <= 8; offset++) {
                    int j = (i + offset) % AGENTS;
                    long dx = px[i] - px[j];
                    long dz = pz[i] - pz[j];
                    long distSq = dx * dx + dz * dz;
                    if (distSq > 0 && distSq < 4000000L) {
                        sepX += dx / 32L;
                        sepZ += dz / 32L;
                    }
                }

                long pullX = -px[i] / 128L;
                long pullZ = -pz[i] / 128L;

                vx[i] = (vx[i] * 3L) / 4L + sepX + pullX + windBiasX;
                vz[i] = (vz[i] * 3L) / 4L + sepZ + pullZ + windBiasZ;

                vx[i] = clampI64(vx[i], -600L, 600L);
                vz[i] = clampI64(vz[i], -600L, 600L);

                px[i] = clampI64(px[i] + vx[i], -22000L, 22000L);
                pz[i] = clampI64(pz[i] + vz[i], -14000L, 14000L);

                long gx = (px[i] + 60000L) / 1000L;
                long gz = (pz[i] + 40000L) / 1000L;
                long fx = (px[i] + 60000L) % 1000L;
                long fz = (pz[i] + 40000L) % 1000L;

                long h00 = (gx * 19L + gz * 31L) % 2500L;
                long h10 = ((gx + 1L) * 19L + gz * 31L) % 2500L;
                long h01 = (gx * 19L + (gz + 1L) * 31L) % 2500L;
                long h11 = ((gx + 1L) * 19L + (gz + 1L) * 31L) % 2500L;

                long hx0 = h00 * (1000L - fx) + h10 * fx;
                long hx1 = h01 * (1000L - fx) + h11 * fx;
                long hInterp = (hx0 * (1000L - fz) + hx1 * fz) / 1000000L;

                heightChecksum += hInterp + (px[i] >> 4) - (pz[i] >> 4);
            }
        }
        return heightChecksum;
    }

    public static long combatPowerWaveTick(int ticks) {
        final int TOWERS = 31;
        final int RAIDERS = 60;

        int[] towerAmmo = new int[TOWERS];
        int[] towerCd = new int[TOWERS];
        int[] raiderHp = new int[RAIDERS];
        int[] raiderDist = new int[RAIDERS];

        Arrays.fill(towerAmmo, 50);
        for (int r = 0; r < RAIDERS; r++) {
            raiderHp[r] = 450 + r * 25;
            raiderDist[r] = 100 + r * 4;
        }

        int contCapacity = 70;
        int capacitor = 1200;
        int breakerTicks = 0;
        long totalDamageDealt = 0L;
        long totalAmmoSpent = 0L;
        long breakerTrips = 0L;
        int playerGuard = 1400;
        int playerStamina = 1800;

        for (int tick = 0; tick < ticks; tick++) {
            int demand = 55 + ((tick * 7) % 60);
            int fireMultPct;
            if (breakerTicks > 0) {
                breakerTicks--;
                fireMultPct = 0;
            } else if (demand <= contCapacity) {
                capacitor = Math.min(1200, capacitor + 5);
                fireMultPct = 100;
            } else if (demand * 10 <= contCapacity * 11) {
                capacitor = Math.max(0, capacitor - (demand - contCapacity));
                fireMultPct = 100;
            } else if (demand * 10 <= contCapacity * 14) {
                fireMultPct = 75;
            } else {
                breakerTicks = 4;
                breakerTrips++;
                fireMultPct = 0;
            }

            int targetRaider = tick % RAIDERS;
            if (raiderHp[targetRaider] <= 0) {
                raiderHp[targetRaider] = 500 + (tick % 300);
                raiderDist[targetRaider] = 180;
            }
            raiderDist[targetRaider] = Math.max(5, raiderDist[targetRaider] - 3);

            if (fireMultPct > 0) {
                for (int t = 0; t < TOWERS; t++) {
                    if (towerCd[t] > 0) {
                        towerCd[t]--;
                        continue;
                    }
                    if (towerAmmo[t] <= 0) {
                        towerAmmo[t] = 50;
                    }
                    int baseDmg = 18 + (t % 5) * 6;
                    int dmg = (baseDmg * fireMultPct) / 100;
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

            int rawEnemyHit = 28;
            if (tick % 4 == 0) {
                playerStamina = Math.min(1800, playerStamina + 20);
                playerGuard = Math.max(150, playerGuard - 5);
                totalDamageDealt += (rawEnemyHit / 2);
            } else {
                playerStamina = clampI32(playerStamina - 12 + 15, 200, 1800);
                playerGuard = clampI32(playerGuard - 18 + 22, 200, 1400);
            }
        }

        return totalDamageDealt * 1000L + totalAmmoSpent * 10L + breakerTrips + playerGuard;
    }

    public static long montecarloEconomy60d(int seedsCount) {
        long grandTotalGold = 0L;
        long[] rngState = new long[1];

        for (int s = 1; s <= seedsCount; s++) {
            rngState[0] = ((long) s) * 0x9E3779B97F4A7C15L;
            long gold = 1200L;
            long feedGrams = 250000L;
            long adultPigs = 12L;
            long elderPigs = 2L;
            long pigWeightTenths = 750L;
            long themePricePct = 100L;
            long majorWavesSurvived = 0L;

            for (int day = 1; day <= 60; day++) {
                if (day % 7 == 1) {
                    int r = nextU32(rngState, 100);
                    if (r < 24) themePricePct = 100L;
                    else if (r < 36) themePricePct = 85L;
                    else if (r < 48) themePricePct = 130L;
                    else if (r < 58) themePricePct = 95L;
                    else if (r < 66) themePricePct = 75L;
                    else if (r < 76) themePricePct = 115L;
                    else themePricePct = 105L;
                }

                long dailyNeedGrams = adultPigs * 3500L + elderPigs * 4200L;
                if (feedGrams < dailyNeedGrams && gold >= 180L) {
                    gold -= 180L;
                    feedGrams += 150000L;
                }

                if (feedGrams >= dailyNeedGrams) {
                    feedGrams -= dailyNeedGrams;
                    pigWeightTenths = Math.min(1150L, pigWeightTenths + 45L);
                } else {
                    feedGrams = 0L;
                    pigWeightTenths = Math.max(500L, pigWeightTenths - 20L);
                }

                if (pigWeightTenths >= 950L && adultPigs > 8L) {
                    long salePrice = (pigWeightTenths * 3L * 120L * themePricePct * 130L) / 1000000L;
                    gold += salePrice;
                    adultPigs--;
                }

                if (day % 5 == 0) {
                    int roll = nextU32(rngState, 1000);
                    if (roll < 595) {
                        long litter = 2L + nextU32(rngState, 3);
                        adultPigs = Math.min(36L, adultPigs + litter);
                    }
                }

                if (day % 6 == 0) {
                    long cycle = day / 6;
                    long totalPigs = adultPigs + elderPigs;
                    long threatBudget = (100L * (100L + 18L * (cycle - 1L)) * (1000L + 4L * totalPigs)) / 100000L;
                    majorWavesSurvived++;
                    gold += 150L + threatBudget / 2L;
                }
            }

            grandTotalGold += gold + majorWavesSurvived * 10L + adultPigs;
        }

        return grandTotalGold;
    }

    public static void main(String[] args) {
        // JVM JIT Warmup
        for (int w = 0; w < 3; w++) {
            flowfieldBfs120x80(20);
            boidsHeightfieldStep(100);
            combatPowerWaveTick(500);
            montecarloEconomy60d(30);
        }

        long t0 = System.nanoTime();
        long c1 = flowfieldBfs120x80(500);
        long t1 = System.nanoTime();
        long c2 = boidsHeightfieldStep(5000);
        long t2 = System.nanoTime();
        long c3 = combatPowerWaveTick(50000);
        long t3 = System.nanoTime();
        long c4 = montecarloEconomy60d(2000);
        long t4 = System.nanoTime();

        double d1 = (t1 - t0) / 1e6;
        double d2 = (t2 - t1) / 1e6;
        double d3 = (t3 - t2) / 1e6;
        double d4 = (t4 - t3) / 1e6;

        System.out.printf("LANGUAGE=Java (OpenJDK 21.0.9 HotSpot C2 JIT)%n");
        System.out.printf("KERNEL1_BFS_MS=%.3f%n", d1);
        System.out.printf("KERNEL2_BOIDS_MS=%.3f%n", d2);
        System.out.printf("KERNEL3_COMBAT_MS=%.3f%n", d3);
        System.out.printf("KERNEL4_ECONOMY_MS=%.3f%n", d4);
        System.out.printf("TOTAL_MS=%.3f%n", d1 + d2 + d3 + d4);
        System.out.printf("CHECKSUMS=%d|%d|%d|%d%n", c1, c2, c3, c4);
    }
}
