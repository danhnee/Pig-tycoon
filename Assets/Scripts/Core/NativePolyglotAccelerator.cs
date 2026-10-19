using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;

namespace PigTycoon.Core
{
    public enum PolyglotExecutionBackend
    {
        AutoHybridBestOfBreed = 0,
        RustNativeCAbi = 1,
        Cpp20SimdNative = 2,
        ManagedCSharpSoA = 3
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct NativeBenchmarkSummary
    {
        public double Kernel1BfsMs;
        public double Kernel2BoidsMs;
        public double Kernel3CombatMs;
        public double Kernel4EconomyMs;
        public double TotalMs;
        public long Checksum1;
        public long Checksum2;
        public long Checksum3;
        public long Checksum4;
    }

    [Serializable]
    public sealed class LanguageLayerBenchmarkReport
    {
        public string LanguageAndCompiler { get; set; } = string.Empty;
        public string TargetRole { get; set; } = string.Empty;
        public double Kernel1BfsMs { get; set; }
        public double Kernel2BoidsMs { get; set; }
        public double Kernel3CombatMs { get; set; }
        public double Kernel4EconomyMs { get; set; }
        public double TotalMs { get; set; }
        public long Checksum1 { get; set; }
        public long Checksum2 { get; set; }
        public long Checksum3 { get; set; }
        public long Checksum4 { get; set; }
        public bool BitExactMatch { get; set; }
    }

    /// <summary>
    /// Bộ tăng tốc Đa Ngôn Ngữ (Polyglot Native FFI Accelerator) cho Pig Tycoon:
    /// - Kết nối trực tiếp với thư viện C-ABI viết bằng Rust (`pig_tycoon_native`) và C/C++20 (`pig_tycoon_cpp`)
    ///   qua P/Invoke an toàn (không dùng `unsafe`, tương thích 100% `allowUnsafeCode: false` và `noEngineReferences: true`).
    /// - Tự động chuyển về nhân C# SoA (Structure-of-Arrays) tất định bit-for-bit khi chạy trên WebGL hoặc môi trường không có DLL native.
    /// - Đảm bảo cả 4 nhân tính toán (Flow-Field BFS 120×80, Boids + Bilinear Heightfield 180 thực thể,
    ///   Combat Wave + Lưới Điện 31 công trình, và Monte Carlo Kinh tế/Di truyền 60 ngày) trả về cùng một Checksum tuyệt đối.
    /// </summary>
    public static class NativePolyglotAccelerator
    {
        public const long ExpectedChecksum1_Bfs500 = 266653114L;
        public const long ExpectedChecksum2_Boids5000 = 2091061385L;
        public const long ExpectedChecksum3_Combat50000 = 2782632973L;
        public const long ExpectedChecksum4_Economy2000 = 258354468L;

        private const string RustDllName = "pig_tycoon_native";
        private const string CppDllName = "pig_tycoon_cpp";

        private static bool _probedNative;
        private static bool _rustAvailable;
        private static bool _cppAvailable;

        public static long NativeHardwareAcceleratedFlowFieldCount { get; private set; }
        public static string LastRustProbeDiagnostic { get; private set; } = "NotProbed";
        public static string LastCppProbeDiagnostic { get; private set; } = "NotProbed";

        [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        private static extern bool SetDllDirectoryW(string lpPathName);

        // Rust C-ABI Exports
        [DllImport(RustDllName, CallingConvention = CallingConvention.Cdecl, EntryPoint = "pt_flowfield_bfs_120x80")]
        private static extern long RustFlowFieldBfs(int iterations);

        [DllImport(RustDllName, CallingConvention = CallingConvention.Cdecl, EntryPoint = "pt_boids_heightfield_step")]
        private static extern long RustBoidsHeightfieldStep(int steps);

        [DllImport(RustDllName, CallingConvention = CallingConvention.Cdecl, EntryPoint = "pt_combat_power_wave_tick")]
        private static extern long RustCombatPowerWaveTick(int ticks);

        [DllImport(RustDllName, CallingConvention = CallingConvention.Cdecl, EntryPoint = "pt_montecarlo_economy_60d")]
        private static extern long RustMonteCarloEconomy60d(int seedsCount);

        [DllImport(RustDllName, CallingConvention = CallingConvention.Cdecl, EntryPoint = "pt_compute_flowfield_buffer")]
        private static extern int RustComputeFlowFieldBuffer(
            int targetX,
            int targetY,
            [In] byte[] obstacles,
            [Out] int[] outDist,
            int width,
            int height);

        [DllImport(RustDllName, CallingConvention = CallingConvention.Cdecl, EntryPoint = "pt_compute_flowfield_8dir")]
        private static extern int RustComputeFlowField8Dir(
            int width,
            int height,
            [In] byte[] obstacles,
            [In] int[] targetsX,
            [In] int[] targetsY,
            int targetCount,
            [Out] int[] outDist,
            [Out] float[] outVx,
            [Out] float[] outVy);

        [DllImport(RustDllName, CallingConvention = CallingConvention.Cdecl, EntryPoint = "pt_evaluate_gdd54_and_seal_api")]
        private static extern int RustEvaluateGdd54AndSealApi(
            int channel,
            int rarity,
            int protagonist,
            out int outProbPermille,
            out int outGoldCost,
            out int outStaCost,
            out int outHours);

        // C/C++20 Clang -O3 -march=native C-ABI Exports
        [DllImport(CppDllName, CallingConvention = CallingConvention.Cdecl, EntryPoint = "pt_cpp_flowfield_bfs_120x80")]
        private static extern long CppFlowFieldBfs(int iterations);

        [DllImport(CppDllName, CallingConvention = CallingConvention.Cdecl, EntryPoint = "pt_cpp_boids_heightfield_step")]
        private static extern long CppBoidsHeightfieldStep(int steps);

        [DllImport(CppDllName, CallingConvention = CallingConvention.Cdecl, EntryPoint = "pt_cpp_combat_power_wave_tick")]
        private static extern long CppCombatPowerWaveTick(int ticks);

        [DllImport(CppDllName, CallingConvention = CallingConvention.Cdecl, EntryPoint = "pt_cpp_montecarlo_economy_60d")]
        private static extern long CppMonteCarloEconomy60d(int seedsCount);

        [DllImport(CppDllName, CallingConvention = CallingConvention.Cdecl, EntryPoint = "pt_cpp_compute_flowfield_8dir")]
        private static extern int CppComputeFlowField8Dir(
            int width,
            int height,
            [In] byte[] obstacles,
            [In] int[] targetsX,
            [In] int[] targetsY,
            int targetCount,
            [Out] int[] outDist,
            [Out] float[] outVx,
            [Out] float[] outVy);

        public static bool IsRustNativeAvailable
        {
            get
            {
                EnsureNativeProbed();
                return _rustAvailable;
            }
        }

        public static bool IsCppNativeAvailable
        {
            get
            {
                EnsureNativeProbed();
                return _cppAvailable;
            }
        }

        public static void EnsureNativeProbed()
        {
            if (_probedNative) return;
            _probedNative = true;

            try
            {
                string baseDir = AppDomain.CurrentDomain.BaseDirectory;
                string[] candidateDirs =
                {
                    Path.Combine(baseDir, "native"),
                    Path.GetFullPath(Path.Combine(baseDir, "..", "..", "..", "..", "native_polyglot", "bin")),
                    Path.GetFullPath(Path.Combine(baseDir, "..", "..", "..", "native_polyglot", "bin")),
                    Path.Combine(Directory.GetCurrentDirectory(), "native_polyglot", "bin"),
                    Path.Combine(Directory.GetCurrentDirectory(), "native_polyglot", "rust_core", "target", "release"),
                    Path.Combine(Directory.GetCurrentDirectory(), "native_polyglot", "cpp_core")
                };

                for (int i = 0; i < candidateDirs.Length; i++)
                {
                    string dir = candidateDirs[i];
                    if (Directory.Exists(dir))
                    {
                        try
                        {
                            SetDllDirectoryW(dir);
                        }
                        catch (Exception ex)
                        {
                            LastRustProbeDiagnostic = $"SetDllDirectoryW ignored ({ex.GetType().Name})";
                        }
                        break;
                    }
                }
            }
            catch (Exception ex)
            {
                LastRustProbeDiagnostic = $"Path resolution notice: {ex.GetType().Name}: {ex.Message}";
            }

            try
            {
                long c = RustFlowFieldBfs(1);
                _rustAvailable = c > 0;
                LastRustProbeDiagnostic = _rustAvailable ? $"OK (checksum1={c})" : $"InvalidChecksum ({c})";
            }
            catch (Exception ex)
            {
                _rustAvailable = false;
                LastRustProbeDiagnostic = $"{ex.GetType().Name}: {ex.Message}";
            }

            try
            {
                long c = CppFlowFieldBfs(1);
                _cppAvailable = c > 0;
                LastCppProbeDiagnostic = _cppAvailable ? $"OK (checksum1={c})" : $"InvalidChecksum ({c})";
            }
            catch (Exception ex)
            {
                _cppAvailable = false;
                LastCppProbeDiagnostic = $"{ex.GetType().Name}: {ex.Message}";
            }
        }

        private static ulong Xorshift64Star(ref ulong state)
        {
            ulong x = state;
            if (x == 0UL)
            {
                x = 0x9E3779B97F4A7C15UL;
            }
            x ^= x >> 12;
            x ^= x << 25;
            x ^= x >> 27;
            state = x;
            return unchecked(x * 0x2545F4914F6CDD1DUL);
        }

        private static uint NextU32(ref ulong state, uint bound)
        {
            uint r = (uint)(Xorshift64Star(ref state) >> 32);
            return r % bound;
        }

        /// <summary>
        /// Nhân 1 (Pure C# SoA Fallback): Tìm đường Flow-Field BFS trên lưới 120×80 (9.600 ô 1m).
        /// </summary>
        public static long ManagedFlowFieldBfs120x80(int iterations)
        {
            const int W = 120;
            const int H = 80;
            const int N = W * H;

            Span<byte> obstacles = stackalloc byte[N];
            obstacles.Clear();
            for (int y = 0; y < H; y++)
            {
                for (int x = 0; x < W; x++)
                {
                    if (x > 10 && x < 110 && y > 10 && y < 70 && (x % 17 == 0) && (y % 11 != 0))
                    {
                        obstacles[y * W + x] = 1;
                    }
                }
            }

            int[] dist = new int[N];
            int[] queue = new int[N];
            long totalChecksum = 0L;
            int iters = Math.Max(1, iterations);

            for (int iter = 0; iter < iters; iter++)
            {
                Array.Fill(dist, -1);
                int tx = 20 + (iter % 40);
                int ty = 15 + (iter % 30);
                int startIdx = ty * W + tx;
                obstacles[startIdx] = 0;

                int head = 0;
                int tail = 0;
                dist[startIdx] = 0;
                queue[tail++] = startIdx;

                while (head < tail)
                {
                    int cur = queue[head++];
                    int d = dist[cur] + 1;
                    int cx = cur % W;
                    int cy = cur / W;

                    if (cx > 0)
                    {
                        int nb = cur - 1;
                        if (obstacles[nb] == 0 && dist[nb] < 0)
                        {
                            dist[nb] = d;
                            queue[tail++] = nb;
                        }
                    }
                    if (cx + 1 < W)
                    {
                        int nb = cur + 1;
                        if (obstacles[nb] == 0 && dist[nb] < 0)
                        {
                            dist[nb] = d;
                            queue[tail++] = nb;
                        }
                    }
                    if (cy > 0)
                    {
                        int nb = cur - W;
                        if (obstacles[nb] == 0 && dist[nb] < 0)
                        {
                            dist[nb] = d;
                            queue[tail++] = nb;
                        }
                    }
                    if (cy + 1 < H)
                    {
                        int nb = cur + W;
                        if (obstacles[nb] == 0 && dist[nb] < 0)
                        {
                            dist[nb] = d;
                            queue[tail++] = nb;
                        }
                    }
                }

                long iterSum = 0L;
                for (int i = 0; i < N; i++)
                {
                    if (dist[i] > 0)
                    {
                        iterSum += dist[i];
                    }
                }
                totalChecksum += iterSum;
            }

            return totalChecksum;
        }

        /// <summary>
        /// Nhân 2 (Pure C# SoA Fallback): Boids Steering 150 heo + 30 quái &amp; Nội suy độ cao Bilinear O(1).
        /// </summary>
        public static long ManagedBoidsHeightfieldStep(int steps)
        {
            const int AGENTS = 180;
            Span<long> px = stackalloc long[AGENTS];
            Span<long> pz = stackalloc long[AGENTS];
            Span<long> vx = stackalloc long[AGENTS];
            Span<long> vz = stackalloc long[AGENTS];

            for (int i = 0; i < AGENTS; i++)
            {
                long idx = i;
                px[i] = ((idx * 173) % 40000) - 20000;
                pz[i] = ((idx * 251) % 24000) - 12000;
                vx[i] = ((idx * 37) % 200) - 100;
                vz[i] = ((idx * 53) % 200) - 100;
            }

            int totalSteps = Math.Max(1, steps);
            long heightChecksum = 0L;

            for (int step = 0; step < totalSteps; step++)
            {
                long windBiasX = (((long)step * 13) % 21) - 10;
                long windBiasZ = (((long)step * 7) % 21) - 10;

                for (int i = 0; i < AGENTS; i++)
                {
                    long sepX = 0L;
                    long sepZ = 0L;

                    for (int offset = 1; offset <= 8; offset++)
                    {
                        int j = (i + offset) % AGENTS;
                        long dx = px[i] - px[j];
                        long dz = pz[i] - pz[j];
                        long distSq = dx * dx + dz * dz;
                        if (distSq > 0 && distSq < 4000000L)
                        {
                            sepX += dx / 32L;
                            sepZ += dz / 32L;
                        }
                    }

                    long pullX = -px[i] / 128L;
                    long pullZ = -pz[i] / 128L;

                    vx[i] = (vx[i] * 3L) / 4L + sepX + pullX + windBiasX;
                    vz[i] = (vz[i] * 3L) / 4L + sepZ + pullZ + windBiasZ;

                    vx[i] = Math.Clamp(vx[i], -600L, 600L);
                    vz[i] = Math.Clamp(vz[i], -600L, 600L);

                    px[i] = Math.Clamp(px[i] + vx[i], -22000L, 22000L);
                    pz[i] = Math.Clamp(pz[i] + vz[i], -14000L, 14000L);

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

        /// <summary>
        /// Nhân 3 (Pure C# SoA Fallback): Mô phỏng 31 Công trình Phòng thủ + Lưới Điện Vật Lý + 60 Quái + Auto-Guard/Parry.
        /// </summary>
        public static long ManagedCombatPowerWaveTick(int ticks)
        {
            const int TOWERS = 31;
            const int RAIDERS = 60;

            Span<int> towerAmmo = stackalloc int[TOWERS];
            Span<int> towerCd = stackalloc int[TOWERS];
            Span<int> raiderHp = stackalloc int[RAIDERS];
            Span<int> raiderDist = stackalloc int[RAIDERS];

            for (int t = 0; t < TOWERS; t++)
            {
                towerAmmo[t] = 50;
                towerCd[t] = 0;
            }
            for (int r = 0; r < RAIDERS; r++)
            {
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

            int maxTicks = Math.Max(1, ticks);
            for (int tick = 0; tick < maxTicks; tick++)
            {
                int demand = 55 + ((tick * 7) % 60);
                int fireMultPct;
                if (breakerTicks > 0)
                {
                    breakerTicks--;
                    fireMultPct = 0;
                }
                else if (demand <= contCapacity)
                {
                    capacitor = Math.Min(1200, capacitor + 5);
                    fireMultPct = 100;
                }
                else if (demand * 10 <= contCapacity * 11)
                {
                    capacitor = Math.Max(0, capacitor - (demand - contCapacity));
                    fireMultPct = 100;
                }
                else if (demand * 10 <= contCapacity * 14)
                {
                    fireMultPct = 75;
                }
                else
                {
                    breakerTicks = 4;
                    breakerTrips++;
                    fireMultPct = 0;
                }

                int targetRaider = tick % RAIDERS;
                if (raiderHp[targetRaider] <= 0)
                {
                    raiderHp[targetRaider] = 500 + (tick % 300);
                    raiderDist[targetRaider] = 180;
                }
                raiderDist[targetRaider] = Math.Max(5, raiderDist[targetRaider] - 3);

                if (fireMultPct > 0)
                {
                    for (int t = 0; t < TOWERS; t++)
                    {
                        if (towerCd[t] > 0)
                        {
                            towerCd[t]--;
                            continue;
                        }
                        if (towerAmmo[t] <= 0)
                        {
                            towerAmmo[t] = 50;
                        }
                        int baseDmg = 18 + (t % 5) * 6;
                        int dmg = (baseDmg * fireMultPct) / 100;
                        raiderHp[targetRaider] -= dmg;
                        towerAmmo[t]--;
                        towerCd[t] = 3 + (t % 4);
                        totalDamageDealt += dmg;
                        totalAmmoSpent++;
                        if (raiderHp[targetRaider] <= 0)
                        {
                            break;
                        }
                    }
                }

                int rawEnemyHit = 28;
                if (tick % 4 == 0)
                {
                    playerStamina = Math.Min(1800, playerStamina + 20);
                    playerGuard = Math.Max(150, playerGuard - 5);
                    totalDamageDealt += (rawEnemyHit / 2);
                }
                else
                {
                    playerStamina = Math.Clamp(playerStamina - 12 + 15, 200, 1800);
                    playerGuard = Math.Clamp(playerGuard - 18 + 22, 200, 1400);
                }
            }

            return totalDamageDealt * 1000L + totalAmmoSpent * 10L + breakerTrips + playerGuard;
        }

        /// <summary>
        /// Nhân 4 (Pure C# SoA Fallback): Mô phỏng Monte Carlo 60 ngày × N hạt giống (Kinh tế 8 chủ đề, Phối giống Kim Thọ, Ngân sách Uy hiếp).
        /// </summary>
        public static long ManagedMonteCarloEconomy60d(int seedsCount)
        {
            int count = Math.Max(1, seedsCount);
            long grandTotalGold = 0L;

            for (int s = 1; s <= count; s++)
            {
                ulong rng = unchecked((ulong)s * 0x9E3779B97F4A7C15UL);
                long gold = 1200L;
                long feedGrams = 250000L;
                long adultPigs = 12L;
                long elderPigs = 2L;
                long pigWeightTenths = 750L;
                long themePricePct = 100L;
                long majorWavesSurvived = 0L;

                for (int day = 1; day <= 60; day++)
                {
                    if (day % 7 == 1)
                    {
                        uint r = NextU32(ref rng, 100u);
                        if (r < 24u) themePricePct = 100L;
                        else if (r < 36u) themePricePct = 85L;
                        else if (r < 48u) themePricePct = 130L;
                        else if (r < 58u) themePricePct = 95L;
                        else if (r < 66u) themePricePct = 75L;
                        else if (r < 76u) themePricePct = 115L;
                        else themePricePct = 105L;
                    }

                    long dailyNeedGrams = adultPigs * 3500L + elderPigs * 4200L;
                    if (feedGrams < dailyNeedGrams && gold >= 180L)
                    {
                        gold -= 180L;
                        feedGrams += 150000L;
                    }

                    if (feedGrams >= dailyNeedGrams)
                    {
                        feedGrams -= dailyNeedGrams;
                        pigWeightTenths = Math.Min(1150L, pigWeightTenths + 45L);
                    }
                    else
                    {
                        feedGrams = 0L;
                        pigWeightTenths = Math.Max(500L, pigWeightTenths - 20L);
                    }

                    if (pigWeightTenths >= 950L && adultPigs > 8L)
                    {
                        long salePrice = (pigWeightTenths * 3L * 120L * themePricePct * 130L) / 1000000L;
                        gold += salePrice;
                        adultPigs--;
                    }

                    if (day % 5 == 0)
                    {
                        uint roll = NextU32(ref rng, 1000u);
                        if (roll < 595u)
                        {
                            long litter = 2L + NextU32(ref rng, 3u);
                            adultPigs = Math.Min(36L, adultPigs + litter);
                        }
                    }

                    if (day % 6 == 0)
                    {
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

        /// <summary>
        /// Chạy benchmark trên một Backend cụ thể (Rust C-ABI, C/C++20 Clang SIMD, Managed C# SoA, hoặc Auto Hybrid Best-of-Breed).
        /// </summary>
        public static NativeBenchmarkSummary ExecuteBenchmarkSuite(
            PolyglotExecutionBackend backend,
            int bfsIters = 500,
            int boidsSteps = 5000,
            int combatTicks = 50000,
            int economySeeds = 2000)
        {
            EnsureNativeProbed();
            var sw = new Stopwatch();

            // Kernel 1: FlowField BFS 120x80 (C++20 / Rust Native fastest)
            sw.Restart();
            long c1 = backend switch
            {
                PolyglotExecutionBackend.RustNativeCAbi when _rustAvailable => RustFlowFieldBfs(bfsIters),
                PolyglotExecutionBackend.Cpp20SimdNative when _cppAvailable => CppFlowFieldBfs(bfsIters),
                PolyglotExecutionBackend.AutoHybridBestOfBreed when _cppAvailable => CppFlowFieldBfs(bfsIters),
                PolyglotExecutionBackend.AutoHybridBestOfBreed when _rustAvailable => RustFlowFieldBfs(bfsIters),
                _ => ManagedFlowFieldBfs120x80(bfsIters)
            };
            double d1 = sw.Elapsed.TotalMilliseconds;

            // Kernel 2: Boids 180 entities + Bilinear Heightfield O(1) (C++20 / Rust Native fastest)
            sw.Restart();
            long c2 = backend switch
            {
                PolyglotExecutionBackend.RustNativeCAbi when _rustAvailable => RustBoidsHeightfieldStep(boidsSteps),
                PolyglotExecutionBackend.Cpp20SimdNative when _cppAvailable => CppBoidsHeightfieldStep(boidsSteps),
                PolyglotExecutionBackend.AutoHybridBestOfBreed when _cppAvailable => CppBoidsHeightfieldStep(boidsSteps),
                PolyglotExecutionBackend.AutoHybridBestOfBreed when _rustAvailable => RustBoidsHeightfieldStep(boidsSteps),
                _ => ManagedBoidsHeightfieldStep(boidsSteps)
            };
            double d2 = sw.Elapsed.TotalMilliseconds;

            // Kernel 3: 31-Tower Defense + Power Grid + Combat Wave (Rust / C++20 Native fastest)
            sw.Restart();
            long c3 = backend switch
            {
                PolyglotExecutionBackend.RustNativeCAbi when _rustAvailable => RustCombatPowerWaveTick(combatTicks),
                PolyglotExecutionBackend.Cpp20SimdNative when _cppAvailable => CppCombatPowerWaveTick(combatTicks),
                PolyglotExecutionBackend.AutoHybridBestOfBreed when _rustAvailable => RustCombatPowerWaveTick(combatTicks),
                PolyglotExecutionBackend.AutoHybridBestOfBreed when _cppAvailable => CppCombatPowerWaveTick(combatTicks),
                _ => ManagedCombatPowerWaveTick(combatTicks)
            };
            double d3 = sw.Elapsed.TotalMilliseconds;

            // Kernel 4: 60-Day Monte Carlo Economy & Genetics (Rust LTO Native fastest single-core)
            sw.Restart();
            long c4 = backend switch
            {
                PolyglotExecutionBackend.RustNativeCAbi when _rustAvailable => RustMonteCarloEconomy60d(economySeeds),
                PolyglotExecutionBackend.Cpp20SimdNative when _cppAvailable => CppMonteCarloEconomy60d(economySeeds),
                PolyglotExecutionBackend.AutoHybridBestOfBreed when _rustAvailable => RustMonteCarloEconomy60d(economySeeds),
                PolyglotExecutionBackend.AutoHybridBestOfBreed when _cppAvailable => CppMonteCarloEconomy60d(economySeeds),
                _ => ManagedMonteCarloEconomy60d(economySeeds)
            };
            double d4 = sw.Elapsed.TotalMilliseconds;

            return new NativeBenchmarkSummary
            {
                Kernel1BfsMs = d1,
                Kernel2BoidsMs = d2,
                Kernel3CombatMs = d3,
                Kernel4EconomyMs = d4,
                TotalMs = d1 + d2 + d3 + d4,
                Checksum1 = c1,
                Checksum2 = c2,
                Checksum3 = c3,
                Checksum4 = c4
            };
        }

        /// <summary>
        /// Tăng tốc trực tiếp việc tính bảng khoảng cách Flow-Field 120×80 cho HerdFlowField bằng Rust C-ABI (hoặc C# fallback trên WebGL).
        /// </summary>
        public static int ComputeAcceleratedFlowField120x80(int targetX, int targetY, byte[] obstacles, int[] outDistances)
        {
            if (obstacles == null || outDistances == null || obstacles.Length < 9600 || outDistances.Length < 9600)
                return -1;

            EnsureNativeProbed();
            if (_rustAvailable)
            {
                try
                {
                    return RustComputeFlowFieldBuffer(targetX, targetY, obstacles, outDistances, 120, 80);
                }
                catch
                {
                    // Fallback to managed if marshaling fails
                }
            }

            // Managed fallback (WebGL / Pure C#)
            const int W = 120;
            const int H = 80;
            const int N = W * H;
            Array.Fill(outDistances, -1);
            int tx = Math.Clamp(targetX, 0, W - 1);
            int ty = Math.Clamp(targetY, 0, H - 1);
            int start = ty * W + tx;
            outDistances[start] = 0;

            Span<int> queue = stackalloc int[N];
            int head = 0;
            int tail = 0;
            queue[tail++] = start;

            while (head < tail)
            {
                int cur = queue[head++];
                int d = outDistances[cur] + 1;
                int cx = cur % W;
                int cy = cur / W;

                if (cx > 0 && obstacles[cur - 1] == 0 && outDistances[cur - 1] < 0)
                {
                    outDistances[cur - 1] = d;
                    queue[tail++] = cur - 1;
                }
                if (cx + 1 < W && obstacles[cur + 1] == 0 && outDistances[cur + 1] < 0)
                {
                    outDistances[cur + 1] = d;
                    queue[tail++] = cur + 1;
                }
                if (cy > 0 && obstacles[cur - W] == 0 && outDistances[cur - W] < 0)
                {
                    outDistances[cur - W] = d;
                    queue[tail++] = cur - W;
                }
                if (cy + 1 < H && obstacles[cur + W] == 0 && outDistances[cur + W] < 0)
                {
                    outDistances[cur + W] = d;
                    queue[tail++] = cur + W;
                }
            }
            return tail;
        }

        /// <summary>
        /// Tăng tốc trực tiếp việc dựng Flow-Field 8 hướng chống cắt góc (step_cost 10/14) cho HerdFlowField.RebuildFlowField
        /// qua C/C++20 (`pt_cpp_compute_flowfield_8dir`) hoặc Rust (`pt_compute_flowfield_8dir`), tự động fallback về C# trên WebGL.
        /// </summary>
        public static int ComputeFlowField8DirNativeOrManaged(
            int width,
            int height,
            byte[] obstacles,
            int[] targetsX,
            int[] targetsY,
            int targetCount,
            int[] outDist,
            float[] outVx,
            float[] outVy,
            out string backendUsed)
        {
            int n = width * height;
            if (width <= 0 || height <= 0 || targetCount <= 0 ||
                obstacles == null || obstacles.Length < n ||
                targetsX == null || targetsX.Length < targetCount ||
                targetsY == null || targetsY.Length < targetCount ||
                outDist == null || outDist.Length < n ||
                outVx == null || outVx.Length < n ||
                outVy == null || outVy.Length < n)
            {
                backendUsed = "InvalidArgs";
                return -1;
            }

            EnsureNativeProbed();
            if (_rustAvailable)
            {
                try
                {
                    int reachable = RustComputeFlowField8Dir(width, height, obstacles, targetsX, targetsY, targetCount, outDist, outVx, outVy);
                    if (reachable >= 0)
                    {
                        NativeHardwareAcceleratedFlowFieldCount++;
                        backendUsed = "Rust_C_ABI";
                        return reachable;
                    }
                }
                catch
                {
                    // Fallback to C++20 or Managed
                }
            }

            if (_cppAvailable)
            {
                try
                {
                    int reachable = CppComputeFlowField8Dir(width, height, obstacles, targetsX, targetsY, targetCount, outDist, outVx, outVy);
                    if (reachable >= 0)
                    {
                        NativeHardwareAcceleratedFlowFieldCount++;
                        backendUsed = "C++20_Clang_SIMD";
                        return reachable;
                    }
                }
                catch
                {
                    // Fallback to Managed
                }
            }

            // Managed Pure C# Fallback (WebGL / Sandbox)
            backendUsed = "Managed_CSharp_SoA";
            const int UNREACHABLE = 999999;
            for (int i = 0; i < n; i++)
            {
                outDist[i] = UNREACHABLE;
                outVx[i] = 0f;
                outVy[i] = 0f;
            }

            int[] queue = new int[n * 4];
            int head = 0;
            int tail = 0;

            for (int k = 0; k < targetCount; k++)
            {
                int tx = targetsX[k];
                int ty = targetsY[k];
                if (tx >= 0 && tx < width && ty >= 0 && ty < height)
                {
                    int idx = ty * width + tx;
                    if (outDist[idx] != 0)
                    {
                        outDist[idx] = 0;
                        queue[tail++] = idx;
                    }
                }
            }

            int[] dx = { 0, 1, 0, -1, 1, 1, -1, -1 };
            int[] dy = { 1, 0, -1, 0, 1, -1, 1, -1 };
            int[] stepCost = { 10, 10, 10, 10, 14, 14, 14, 14 };

            while (head < tail)
            {
                int cur = queue[head++];
                int cx = cur % width;
                int cy = cur / width;
                int currentDist = outDist[cur];

                for (int dir = 0; dir < 8; dir++)
                {
                    int nx = cx + dx[dir];
                    int ny = cy + dy[dir];
                    if (nx < 0 || nx >= width || ny < 0 || ny >= height) continue;
                    int nidx = ny * width + nx;
                    if (obstacles[nidx] != 0) continue;

                    if (dir >= 4 && (obstacles[cy * width + nx] != 0 || obstacles[ny * width + cx] != 0))
                    {
                        continue;
                    }

                    int cand = currentDist + stepCost[dir];
                    if (cand < outDist[nidx])
                    {
                        outDist[nidx] = cand;
                        if (tail < queue.Length)
                        {
                            queue[tail++] = nidx;
                        }
                    }
                }
            }

            int reachableCells = 0;
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    int idx = y * width + x;
                    int d = outDist[idx];
                    if (d < UNREACHABLE) reachableCells++;
                    if (obstacles[idx] != 0 || d == 0 || d >= UNREACHABLE) continue;

                    int bestDist = d;
                    float bvx = 0f;
                    float bvy = 0f;
                    for (int dir = 0; dir < 8; dir++)
                    {
                        int nx = x + dx[dir];
                        int ny = y + dy[dir];
                        if (nx < 0 || nx >= width || ny < 0 || ny >= height) continue;
                        int nd = outDist[ny * width + nx];
                        if (nd < bestDist)
                        {
                            bestDist = nd;
                            bvx = dx[dir];
                            bvy = dy[dir];
                        }
                    }

                    float mag = (float)Math.Sqrt(bvx * bvx + bvy * bvy);
                    if (mag > 0.0001f)
                    {
                        outVx[idx] = bvx / mag;
                        outVy[idx] = bvy / mag;
                    }
                }
            }

            return reachableCells;
        }

        /// <summary>
        /// Gọi trực tiếp API C-ABI của Rust (`pt_evaluate_gdd54_and_seal_api`) để kiểm chứng thông số 4 kênh giám định GDD 5.4 và toán học phong ấn 3.000 năm.
        /// </summary>
        public static (int sealYearsPlusTag, float probability, int goldCost, float staminaCost, int hours, bool usedNativeRust) EvaluateGdd54AndSealNativeApi(
            AppraisalChannel channel,
            GeneRarity rarity,
            PlayerId protagonist)
        {
            EnsureNativeProbed();
            int chCode = channel switch
            {
                AppraisalChannel.ChoNgay => 0,
                AppraisalChannel.ChoDemGiamDinhCapCao => 1,
                AppraisalChannel.PhongNghienCuu => 2,
                _ => 3
            };
            int rarCode = rarity switch
            {
                GeneRarity.Hiem => 2,
                GeneRarity.Quy => 3,
                GeneRarity.HuyenThoai => 4,
                GeneRarity.DiBien => 5,
                _ => 1
            };
            int protTag = protagonist == PlayerId.Khoa ? 1 : 0;

            if (_rustAvailable)
            {
                try
                {
                    int ret = RustEvaluateGdd54AndSealApi(chCode, rarCode, protTag, out int probPm, out int gold, out int sta, out int hrs);
                    return (ret, probPm / 1000f, gold, sta, hrs, true);
                }
                catch
                {
                    // Fallback to managed
                }
            }

            var spec = CanonicalSagaAndGdd6Systems.GetCanonicalAppraisalChannelSpecGdd54(channel, rarity);
            return (3000 + protTag, spec.successRate, spec.goldCost, spec.staminaCost, spec.processingHours, false);
        }
    }
}
