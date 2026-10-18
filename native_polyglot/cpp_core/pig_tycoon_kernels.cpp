// Pig Tycoon Polyglot Native Core (C / C++20 SIMD-Friendly Kernels)
// Compiles both as standalone benchmark binary and as C-ABI shared library (`pig_tycoon_cpp.dll`).

#include <cstdint>
#include <cstdio>
#include <cstring>
#include <chrono>
#include <algorithm>

#if defined(_WIN32)
#define PT_EXPORT extern "C" __declspec(dllexport)
#else
#define PT_EXPORT extern "C" __attribute__((visibility("default")))
#endif

static inline uint64_t xorshift64star(uint64_t* state) {
    uint64_t x = *state;
    if (x == 0) {
        x = 0x9E3779B97F4A7C15ULL;
    }
    x ^= x >> 12;
    x ^= x << 25;
    x ^= x >> 27;
    *state = x;
    return x * 0x2545F4914F6CDD1DULL;
}

static inline uint32_t next_u32(uint64_t* state, uint32_t bound) {
    uint32_t r = static_cast<uint32_t>(xorshift64star(state) >> 32);
    return r % bound;
}

PT_EXPORT int64_t pt_cpp_flowfield_bfs_120x80(int32_t iterations) {
    constexpr int W = 120;
    constexpr int H = 80;
    constexpr int N = W * H;

    uint8_t obstacles[N];
    std::memset(obstacles, 0, sizeof(obstacles));
    for (int y = 0; y < H; ++y) {
        for (int x = 0; x < W; ++x) {
            if (x > 10 && x < 110 && y > 10 && y < 70 && (x % 17 == 0) && (y % 11 != 0)) {
                obstacles[y * W + x] = 1;
            }
        }
    }

    int32_t dist[N];
    int32_t queue[N];
    int64_t total_checksum = 0;
    int iters = std::max(1, iterations);

    for (int iter = 0; iter < iters; ++iter) {
        for (int i = 0; i < N; ++i) dist[i] = -1;
        int tx = 20 + (iter % 40);
        int ty = 15 + (iter % 30);
        int start_idx = ty * W + tx;
        obstacles[start_idx] = 0;

        int head = 0;
        int tail = 0;
        dist[start_idx] = 0;
        queue[tail++] = start_idx;

        while (head < tail) {
            int cur = queue[head++];
            int32_t d = dist[cur] + 1;
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

        int64_t iter_sum = 0;
        for (int i = 0; i < N; ++i) {
            if (dist[i] > 0) {
                iter_sum += dist[i];
            }
        }
        total_checksum += iter_sum;
    }

    return total_checksum;
}

PT_EXPORT int64_t pt_cpp_boids_heightfield_step(int32_t steps) {
    constexpr int AGENTS = 180;
    int64_t px[AGENTS];
    int64_t pz[AGENTS];
    int64_t vx[AGENTS];
    int64_t vz[AGENTS];

    for (int i = 0; i < AGENTS; ++i) {
        int64_t idx = i;
        px[i] = ((idx * 173) % 40000) - 20000;
        pz[i] = ((idx * 251) % 24000) - 12000;
        vx[i] = ((idx * 37) % 200) - 100;
        vz[i] = ((idx * 53) % 200) - 100;
    }

    int total_steps = std::max(1, steps);
    int64_t height_checksum = 0;

    for (int step = 0; step < total_steps; ++step) {
        int64_t wind_bias_x = ((static_cast<int64_t>(step) * 13) % 21) - 10;
        int64_t wind_bias_z = ((static_cast<int64_t>(step) * 7) % 21) - 10;

        for (int i = 0; i < AGENTS; ++i) {
            int64_t sep_x = 0;
            int64_t sep_z = 0;

            for (int offset = 1; offset <= 8; ++offset) {
                int j = (i + offset) % AGENTS;
                int64_t dx = px[i] - px[j];
                int64_t dz = pz[i] - pz[j];
                int64_t dist_sq = dx * dx + dz * dz;
                if (dist_sq > 0 && dist_sq < 4000000) {
                    sep_x += dx / 32;
                    sep_z += dz / 32;
                }
            }

            int64_t pull_x = -px[i] / 128;
            int64_t pull_z = -pz[i] / 128;

            vx[i] = (vx[i] * 3) / 4 + sep_x + pull_x + wind_bias_x;
            vz[i] = (vz[i] * 3) / 4 + sep_z + pull_z + wind_bias_z;

            vx[i] = std::clamp<int64_t>(vx[i], -600, 600);
            vz[i] = std::clamp<int64_t>(vz[i], -600, 600);

            px[i] = std::clamp<int64_t>(px[i] + vx[i], -22000, 22000);
            pz[i] = std::clamp<int64_t>(pz[i] + vz[i], -14000, 14000);

            int64_t gx = (px[i] + 60000) / 1000;
            int64_t gz = (pz[i] + 40000) / 1000;
            int64_t fx = (px[i] + 60000) % 1000;
            int64_t fz = (pz[i] + 40000) % 1000;

            int64_t h00 = (gx * 19 + gz * 31) % 2500;
            int64_t h10 = ((gx + 1) * 19 + gz * 31) % 2500;
            int64_t h01 = (gx * 19 + (gz + 1) * 31) % 2500;
            int64_t h11 = ((gx + 1) * 19 + (gz + 1) * 31) % 2500;

            int64_t hx0 = h00 * (1000 - fx) + h10 * fx;
            int64_t hx1 = h01 * (1000 - fx) + h11 * fx;
            int64_t h_interp = (hx0 * (1000 - fz) + hx1 * fz) / 1000000;

            height_checksum += h_interp + (px[i] >> 4) - (pz[i] >> 4);
        }
    }

    return height_checksum;
}

PT_EXPORT int64_t pt_cpp_combat_power_wave_tick(int32_t ticks) {
    constexpr int TOWERS = 31;
    constexpr int RAIDERS = 60;

    int32_t tower_ammo[TOWERS];
    int32_t tower_cd[TOWERS];
    int32_t raider_hp[RAIDERS];
    int32_t raider_dist[RAIDERS];

    for (int t = 0; t < TOWERS; ++t) {
        tower_ammo[t] = 50;
        tower_cd[t] = 0;
    }
    for (int r = 0; r < RAIDERS; ++r) {
        raider_hp[r] = 450 + r * 25;
        raider_dist[r] = 100 + r * 4;
    }

    int32_t cont_capacity = 70;
    int32_t capacitor = 1200;
    int32_t breaker_ticks = 0;

    int64_t total_damage_dealt = 0;
    int64_t total_ammo_spent = 0;
    int64_t breaker_trips = 0;
    int32_t player_guard = 1400;
    int32_t player_stamina = 1800;

    int max_ticks = std::max(1, ticks);
    for (int tick = 0; tick < max_ticks; ++tick) {
        int32_t demand = 55 + ((tick * 7) % 60);
        int32_t fire_mult_pct = 0;

        if (breaker_ticks > 0) {
            breaker_ticks -= 1;
            fire_mult_pct = 0;
        } else if (demand <= cont_capacity) {
            capacitor = std::min(1200, capacitor + 5);
            fire_mult_pct = 100;
        } else if (demand * 10 <= cont_capacity * 11) {
            capacitor = std::max(0, capacitor - (demand - cont_capacity));
            fire_mult_pct = 100;
        } else if (demand * 10 <= cont_capacity * 14) {
            fire_mult_pct = 75;
        } else {
            breaker_ticks = 4;
            breaker_trips += 1;
            fire_mult_pct = 0;
        }

        int target_raider = tick % RAIDERS;
        if (raider_hp[target_raider] <= 0) {
            raider_hp[target_raider] = 500 + (tick % 300);
            raider_dist[target_raider] = 180;
        }
        raider_dist[target_raider] = std::max(5, raider_dist[target_raider] - 3);

        if (fire_mult_pct > 0) {
            for (int t = 0; t < TOWERS; ++t) {
                if (tower_cd[t] > 0) {
                    tower_cd[t] -= 1;
                    continue;
                }
                if (tower_ammo[t] <= 0) {
                    tower_ammo[t] = 50;
                }
                int32_t base_dmg = 18 + (t % 5) * 6;
                int32_t dmg = (base_dmg * fire_mult_pct) / 100;
                raider_hp[target_raider] -= dmg;
                tower_ammo[t] -= 1;
                tower_cd[t] = 3 + (t % 4);
                total_damage_dealt += dmg;
                total_ammo_spent += 1;
                if (raider_hp[target_raider] <= 0) {
                    break;
                }
            }
        }

        int32_t raw_enemy_hit = 28;
        if (tick % 4 == 0) {
            player_stamina = std::min(1800, player_stamina + 20);
            player_guard = std::max(150, player_guard - 5);
            total_damage_dealt += (raw_enemy_hit / 2);
        } else {
            player_stamina = std::clamp(player_stamina - 12 + 15, 200, 1800);
            player_guard = std::clamp(player_guard - 18 + 22, 200, 1400);
        }
    }

    return total_damage_dealt * 1000 + total_ammo_spent * 10 + breaker_trips + player_guard;
}

PT_EXPORT int64_t pt_cpp_montecarlo_economy_60d(int32_t seeds_count) {
    uint64_t count = static_cast<uint64_t>(std::max(1, seeds_count));
    int64_t grand_total_gold = 0;

    for (uint64_t s = 1; s <= count; ++s) {
        uint64_t rng = s * 0x9E3779B97F4A7C15ULL;
        int64_t gold = 1200;
        int64_t feed_grams = 250000;
        int64_t adult_pigs = 12;
        int64_t elder_pigs = 2;
        int64_t pig_weight_tenths = 750;
        int64_t theme_price_pct = 100;
        int64_t major_waves_survived = 0;

        for (int day = 1; day <= 60; ++day) {
            if (day % 7 == 1) {
                uint32_t r = next_u32(&rng, 100);
                if (r < 24) theme_price_pct = 100;
                else if (r < 36) theme_price_pct = 85;
                else if (r < 48) theme_price_pct = 130;
                else if (r < 58) theme_price_pct = 95;
                else if (r < 66) theme_price_pct = 75;
                else if (r < 76) theme_price_pct = 115;
                else if (r < 88) theme_price_pct = 105;
                else theme_price_pct = 105;
            }

            int64_t daily_need_grams = adult_pigs * 3500 + elder_pigs * 4200;
            if (feed_grams < daily_need_grams && gold >= 180) {
                gold -= 180;
                feed_grams += 150000;
            }

            if (feed_grams >= daily_need_grams) {
                feed_grams -= daily_need_grams;
                pig_weight_tenths = std::min<int64_t>(1150, pig_weight_tenths + 45);
            } else {
                feed_grams = 0;
                pig_weight_tenths = std::max<int64_t>(500, pig_weight_tenths - 20);
            }

            if (pig_weight_tenths >= 950 && adult_pigs > 8) {
                int64_t sale_price = (pig_weight_tenths * 3 * 120 * theme_price_pct * 130) / 1000000;
                gold += sale_price;
                adult_pigs -= 1;
            }

            if (day % 5 == 0) {
                uint32_t roll = next_u32(&rng, 1000);
                if (roll < 595) {
                    int64_t litter = 2 + static_cast<int64_t>(next_u32(&rng, 3));
                    adult_pigs = std::min<int64_t>(36, adult_pigs + litter);
                }
            }

            if (day % 6 == 0) {
                int64_t cycle = day / 6;
                int64_t total_pigs = adult_pigs + elder_pigs;
                int64_t threat_budget = (100 * (100 + 18 * (cycle - 1)) * (1000 + 4 * total_pigs)) / 100000;
                major_waves_survived += 1;
                gold += 150 + threat_budget / 2;
            }
        }

        grand_total_gold += gold + major_waves_survived * 10 + adult_pigs;
    }

    return grand_total_gold;
}

int main() {
    // Warmup
    pt_cpp_flowfield_bfs_120x80(10);
    pt_cpp_boids_heightfield_step(50);
    pt_cpp_combat_power_wave_tick(200);
    pt_cpp_montecarlo_economy_60d(10);

    auto t0 = std::chrono::high_resolution_clock::now();
    int64_t c1 = pt_cpp_flowfield_bfs_120x80(500);
    auto t1 = std::chrono::high_resolution_clock::now();
    int64_t c2 = pt_cpp_boids_heightfield_step(5000);
    auto t2 = std::chrono::high_resolution_clock::now();
    int64_t c3 = pt_cpp_combat_power_wave_tick(50000);
    auto t3 = std::chrono::high_resolution_clock::now();
    int64_t c4 = pt_cpp_montecarlo_economy_60d(2000);
    auto t4 = std::chrono::high_resolution_clock::now();

    double d1 = std::chrono::duration<double, std::milli>(t1 - t0).count();
    double d2 = std::chrono::duration<double, std::milli>(t2 - t1).count();
    double d3 = std::chrono::duration<double, std::milli>(t3 - t2).count();
    double d4 = std::chrono::duration<double, std::milli>(t4 - t3).count();

    std::printf("LANGUAGE=C/C++20 (LLVM Clang 19 -O3 -march=native)\n");
    std::printf("KERNEL1_BFS_MS=%.3f\n", d1);
    std::printf("KERNEL2_BOIDS_MS=%.3f\n", d2);
    std::printf("KERNEL3_COMBAT_MS=%.3f\n", d3);
    std::printf("KERNEL4_ECONOMY_MS=%.3f\n", d4);
    std::printf("TOTAL_MS=%.3f\n", d1 + d2 + d3 + d4);
    std::printf("CHECKSUMS=%lld|%lld|%lld|%lld\n",
                static_cast<long long>(c1),
                static_cast<long long>(c2),
                static_cast<long long>(c3),
                static_cast<long long>(c4));
    return 0;
}
