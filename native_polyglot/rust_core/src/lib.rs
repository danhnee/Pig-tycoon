//! Pig Tycoon Polyglot Native Core (Rust)
//! Exports zero-allocation C-ABI functions for:
//! - Unity C# Core P/Invoke (`NativePolyglotAccelerator.cs`)
//! - Flutter / Dart Native FFI (`dart:ffi` on Windows PC & Android Studio)
//! - Comparative multi-language simulation benchmarks

#[repr(C)]
#[derive(Clone, Copy, Debug, Default)]
pub struct PolyglotBenchmarkResult {
    pub kernel1_bfs_ms: f64,
    pub kernel2_boids_ms: f64,
    pub kernel3_combat_ms: f64,
    pub kernel4_economy_ms: f64,
    pub total_ms: f64,
    pub checksum1: i64,
    pub checksum2: i64,
    pub checksum3: i64,
    pub checksum4: i64,
}

#[inline(always)]
pub fn xorshift64star(state: &mut u64) -> u64 {
    let mut x = *state;
    if x == 0 {
        x = 0x9E3779B97F4A7C15;
    }
    x ^= x >> 12;
    x ^= x << 25;
    x ^= x >> 27;
    *state = x;
    x.wrapping_mul(0x2545F4914F6CDD1D)
}

#[inline(always)]
pub fn next_u32(state: &mut u64, bound: u32) -> u32 {
    let r = (xorshift64star(state) >> 32) as u32;
    r % bound
}

/// Kernel 1: 120x80 (9,600 cells) Flow-Field BFS Pathfinding across `iterations` rebuilds.
/// Produces bit-exact integer distance sum across all languages.
#[no_mangle]
pub extern "C" fn pt_flowfield_bfs_120x80(iterations: i32) -> i64 {
    const W: usize = 120;
    const H: usize = 80;
    const N: usize = W * H;

    let mut obstacles = [0u8; N];
    for y in 0..H {
        for x in 0..W {
            if x > 10 && x < 110 && y > 10 && y < 70 && (x % 17 == 0) && (y % 11 != 0) {
                obstacles[y * W + x] = 1;
            }
        }
    }

    let mut dist = [-1i32; N];
    let mut queue = [0usize; N];
    let mut total_checksum: i64 = 0;

    let iters = iterations.max(1) as usize;
    for iter in 0..iters {
        dist.fill(-1);
        let tx = 20 + (iter % 40);
        let ty = 15 + (iter % 30);
        let start_idx = ty * W + tx;
        obstacles[start_idx] = 0;

        let mut head = 0usize;
        let mut tail = 0usize;
        dist[start_idx] = 0;
        queue[tail] = start_idx;
        tail += 1;

        while head < tail {
            let cur = queue[head];
            head += 1;
            let d = dist[cur] + 1;
            let cx = cur % W;
            let cy = cur / W;

            if cx > 0 {
                let nb = cur - 1;
                if obstacles[nb] == 0 && dist[nb] < 0 {
                    dist[nb] = d;
                    queue[tail] = nb;
                    tail += 1;
                }
            }
            if cx + 1 < W {
                let nb = cur + 1;
                if obstacles[nb] == 0 && dist[nb] < 0 {
                    dist[nb] = d;
                    queue[tail] = nb;
                    tail += 1;
                }
            }
            if cy > 0 {
                let nb = cur - W;
                if obstacles[nb] == 0 && dist[nb] < 0 {
                    dist[nb] = d;
                    queue[tail] = nb;
                    tail += 1;
                }
            }
            if cy + 1 < H {
                let nb = cur + W;
                if obstacles[nb] == 0 && dist[nb] < 0 {
                    dist[nb] = d;
                    queue[tail] = nb;
                    tail += 1;
                }
            }
        }

        let mut iter_sum: i64 = 0;
        for i in 0..N {
            if dist[i] > 0 {
                iter_sum += dist[i] as i64;
            }
        }
        total_checksum += iter_sum;
    }

    total_checksum
}

/// Exported C-ABI helper that fills a caller-provided distance buffer (`len >= 9600`)
/// for direct use by Unity C# `HerdFlowField` and Flutter FFI.
#[no_mangle]
pub extern "C" fn pt_compute_flowfield_buffer(
    target_x: i32,
    target_y: i32,
    obstacles_ptr: *const u8,
    out_dist_ptr: *mut i32,
    width: i32,
    height: i32,
) -> i32 {
    if obstacles_ptr.is_null() || out_dist_ptr.is_null() || width <= 0 || height <= 0 {
        return -1;
    }
    let w = width as usize;
    let h = height as usize;
    let n = w * h;
    let tx = target_x.clamp(0, width - 1) as usize;
    let ty = target_y.clamp(0, height - 1) as usize;

    let obstacles = unsafe { std::slice::from_raw_parts(obstacles_ptr, n) };
    let dist = unsafe { std::slice::from_raw_parts_mut(out_dist_ptr, n) };

    for v in dist.iter_mut() {
        *v = -1;
    }

    let mut queue = vec![0usize; n];
    let start = ty * w + tx;
    dist[start] = 0;
    let mut head = 0usize;
    let mut tail = 0usize;
    queue[tail] = start;
    tail += 1;

    while head < tail {
        let cur = queue[head];
        head += 1;
        let d = dist[cur] + 1;
        let cx = cur % w;
        let cy = cur / w;

        if cx > 0 {
            let nb = cur - 1;
            if obstacles[nb] == 0 && dist[nb] < 0 {
                dist[nb] = d;
                queue[tail] = nb;
                tail += 1;
            }
        }
        if cx + 1 < w {
            let nb = cur + 1;
            if obstacles[nb] == 0 && dist[nb] < 0 {
                dist[nb] = d;
                queue[tail] = nb;
                tail += 1;
            }
        }
        if cy > 0 {
            let nb = cur - w;
            if obstacles[nb] == 0 && dist[nb] < 0 {
                dist[nb] = d;
                queue[tail] = nb;
                tail += 1;
            }
        }
        if cy + 1 < h {
            let nb = cur + w;
            if obstacles[nb] == 0 && dist[nb] < 0 {
                dist[nb] = d;
                queue[tail] = nb;
                tail += 1;
            }
        }
    }

    tail as i32
}

/// Kernel 2: 150 Pigs + 30 Monsters Boids Steering + Bilinear Heightfield Interpolation (Fixed-Point mm).
/// Uses integer millimeter coordinates so all 6 languages produce identical trajectories and checksums.
#[no_mangle]
pub extern "C" fn pt_boids_heightfield_step(steps: i32) -> i64 {
    const AGENTS: usize = 180; // 150 pigs + 30 monsters
    let mut px = [0i64; AGENTS];
    let mut pz = [0i64; AGENTS];
    let mut vx = [0i64; AGENTS];
    let mut vz = [0i64; AGENTS];

    for i in 0..AGENTS {
        let idx = i as i64;
        px[i] = ((idx * 173) % 40000) - 20000; // [-20000mm, +20000mm]
        pz[i] = ((idx * 251) % 24000) - 12000; // [-12000mm, +12000mm]
        vx[i] = ((idx * 37) % 200) - 100;
        vz[i] = ((idx * 53) % 200) - 100;
    }

    let total_steps = steps.max(1) as usize;
    let mut height_checksum: i64 = 0;

    for step in 0..total_steps {
        let wind_bias_x = ((step as i64 * 13) % 21) - 10;
        let wind_bias_z = ((step as i64 * 7) % 21) - 10;

        for i in 0..AGENTS {
            let mut sep_x: i64 = 0;
            let mut sep_z: i64 = 0;

            // Local neighborhood check (window of 16 neighbors in ring for O(1) bounded work per agent)
            for offset in 1..=8 {
                let j = (i + offset) % AGENTS;
                let dx = px[i] - px[j];
                let dz = pz[i] - pz[j];
                let dist_sq = dx * dx + dz * dz;
                if dist_sq > 0 && dist_sq < 4_000_000 {
                    // Within 2.0m (2000mm)
                    sep_x += dx / 32;
                    sep_z += dz / 32;
                }
            }

            // Pull toward refuge center (0, 0) + separation + wind
            let pull_x = -px[i] / 128;
            let pull_z = -pz[i] / 128;

            vx[i] = (vx[i] * 3) / 4 + sep_x + pull_x + wind_bias_x;
            vz[i] = (vz[i] * 3) / 4 + sep_z + pull_z + wind_bias_z;

            // Clamp velocity to [-600, 600] mm/step
            vx[i] = vx[i].clamp(-600, 600);
            vz[i] = vz[i].clamp(-600, 600);

            // Clamp position to 1/3 pasture bounds [-22000, 22000] x [-14000, 14000] mm
            px[i] = (px[i] + vx[i]).clamp(-22000, 22000);
            pz[i] = (pz[i] + vz[i]).clamp(-14000, 14000);

            // Bilinear terrain height evaluation in fixed-point micrometers
            let gx = (px[i] + 60000) / 1000; // cell 0..120
            let gz = (pz[i] + 40000) / 1000; // cell 0..80
            let fx = (px[i] + 60000) % 1000; // 0..999
            let fz = (pz[i] + 40000) % 1000; // 0..999

            let h00 = (gx * 19 + gz * 31) % 2500;
            let h10 = ((gx + 1) * 19 + gz * 31) % 2500;
            let h01 = (gx * 19 + (gz + 1) * 31) % 2500;
            let h11 = ((gx + 1) * 19 + (gz + 1) * 31) % 2500;

            let hx0 = h00 * (1000 - fx) + h10 * fx;
            let hx1 = h01 * (1000 - fx) + h11 * fx;
            let h_interp = (hx0 * (1000 - fz) + hx1 * fz) / 1_000_000;

            height_checksum += h_interp + (px[i] >> 4) - (pz[i] >> 4);
        }
    }

    height_checksum
}

/// Kernel 3: 31 Defense Buildings + Power Grid Overload/Breaker + 60 Raiders Combat Wave Simulation.
#[no_mangle]
pub extern "C" fn pt_combat_power_wave_tick(ticks: i32) -> i64 {
    const TOWERS: usize = 31;
    const RAIDERS: usize = 60;

    let mut tower_ammo = [50i32; TOWERS];
    let mut tower_cd = [0i32; TOWERS];
    let mut raider_hp = [0i32; RAIDERS];
    let mut raider_dist = [0i32; RAIDERS];

    for r in 0..RAIDERS {
        raider_hp[r] = 450 + (r as i32 * 25);
        raider_dist[r] = 100 + (r as i32 * 4);
    }

    let cont_capacity: i32 = 70; // Generator Level 2
    let mut capacitor: i32 = 1200; // 120.0u scaled x10
    let mut breaker_ticks: i32 = 0;

    let mut total_damage_dealt: i64 = 0;
    let mut total_ammo_spent: i64 = 0;
    let mut breaker_trips: i64 = 0;
    let mut player_guard: i32 = 1400; // 140.0 scaled x10
    let mut player_stamina: i32 = 1800; // 180.0 scaled x10

    let max_ticks = ticks.max(1) as usize;
    for tick in 0..max_ticks {
        let demand = 55 + ((tick as i32 * 7) % 60); // 55..114u

        let fire_mult_pct: i32;
        if breaker_ticks > 0 {
            breaker_ticks -= 1;
            fire_mult_pct = 0;
        } else if demand <= cont_capacity {
            capacitor = (capacitor + 5).min(1200);
            fire_mult_pct = 100;
        } else if demand * 10 <= cont_capacity * 11 {
            capacitor = (capacitor - (demand - cont_capacity)).max(0);
            fire_mult_pct = 100;
        } else if demand * 10 <= cont_capacity * 14 {
            fire_mult_pct = 75; // Overload 110-140%: -25% fire rate
        } else {
            breaker_ticks = 4; // >140% trips breaker for 4 ticks
            breaker_trips += 1;
            fire_mult_pct = 0;
        }

        // Advance raiders and resolve player Auto-Guard / Active Parry
        let target_raider = tick % RAIDERS;
        if raider_hp[target_raider] <= 0 {
            raider_hp[target_raider] = 500 + ((tick as i32) % 300);
            raider_dist[target_raider] = 180;
        }
        raider_dist[target_raider] = (raider_dist[target_raider] - 3).max(5);

        if fire_mult_pct > 0 {
            for t in 0..TOWERS {
                if tower_cd[t] > 0 {
                    tower_cd[t] -= 1;
                    continue;
                }
                if tower_ammo[t] <= 0 {
                    tower_ammo[t] = 50; // Preload between sub-waves
                }
                let base_dmg = 18 + (t as i32 % 5) * 6;
                let dmg = (base_dmg * fire_mult_pct) / 100;
                raider_hp[target_raider] -= dmg;
                tower_ammo[t] -= 1;
                tower_cd[t] = 3 + (t as i32 % 4);
                total_damage_dealt += dmg as i64;
                total_ammo_spent += 1;
                if raider_hp[target_raider] <= 0 {
                    break;
                }
            }
        }

        // Player Active Parry on every 4th tick, Auto-Guard otherwise
        let raw_enemy_hit: i32 = 28;
        if tick % 4 == 0 {
            // Active Parry with Quan Giap Xich: +2.0 STA (+20 scaled), 18% guard loss, 50% counter damage
            player_stamina = (player_stamina + 20).min(1800);
            player_guard = (player_guard - 5).max(150);
            total_damage_dealt += (raw_enemy_hit / 2) as i64;
        } else {
            // Auto-Guard: drains stamina & guard, then regenerates out of hit
            player_stamina = (player_stamina - 12 + 15).clamp(200, 1800);
            player_guard = (player_guard - 18 + 22).clamp(200, 1400);
        }
    }

    total_damage_dealt * 1000 + total_ammo_spent * 10 + breaker_trips + (player_guard as i64)
}

/// Kernel 4: Deterministic 60-Day Monte Carlo Economy, 8-Theme Market, Breeding & Threat Budget across `seeds_count` seeds.
#[no_mangle]
pub extern "C" fn pt_montecarlo_economy_60d(seeds_count: i32) -> i64 {
    let count = seeds_count.max(1) as u64;
    let mut grand_total_gold: i64 = 0;

    for s in 1..=count {
        let mut rng: u64 = s.wrapping_mul(0x9E3779B97F4A7C15);
        let mut gold: i64 = 1200;
        let mut feed_grams: i64 = 250_000; // 250 kg
        let mut adult_pigs: i64 = 12;
        let elder_pigs: i64 = 2;
        let mut pig_weight_tenths: i64 = 750; // 75.0 kg
        let mut theme_price_pct: i64 = 100;
        let mut major_waves_survived: i64 = 0;

        for day in 1..=60 {
            // Weekly market cycle theme roll (GDD 20.2: 24/12/12/10/8/10/12/12%)
            if day % 7 == 1 {
                let r = next_u32(&mut rng, 100);
                theme_price_pct = if r < 24 {
                    100 // CanBang (24%)
                } else if r < 36 {
                    85 // MuaHeoRe (12%)
                } else if r < 48 {
                    130 // ConSotThit (12%)
                } else if r < 58 {
                    95 // KhanHiemThucAn (10%)
                } else if r < 66 {
                    75 // HoangLoanDich (8%)
                } else if r < 76 {
                    115 // KhanHiemHauDotKich (10%)
                } else if r < 88 {
                    105 // MuaTrangBi (12%)
                } else {
                    105 // MuaCongTrinh (12%)
                };
            }

            // Daily feed consumption: 3.5kg (3500g) per adult, 4.2kg (4200g) per elder
            let daily_need_grams = adult_pigs * 3500 + elder_pigs * 4200;
            if feed_grams < daily_need_grams && gold >= 180 {
                gold -= 180;
                feed_grams += 150_000;
            }

            if feed_grams >= daily_need_grams {
                feed_grams -= daily_need_grams;
                pig_weight_tenths = (pig_weight_tenths + 45).min(1150);
            } else {
                feed_grams = 0;
                pig_weight_tenths = (pig_weight_tenths - 20).max(500);
            }

            // Sell 1 adult pig when weight >= 95.0kg (GDD 4.5 formula)
            if pig_weight_tenths >= 950 && adult_pigs > 8 {
                // GiaHeo = Round(Kg * 3 * H_thit(1.20) * H_cho * H_giong(1.30))
                let sale_price = (pig_weight_tenths * 3 * 120 * theme_price_pct * 130) / 1_000_000;
                gold += sale_price;
                adult_pigs -= 1;
            }

            // Elder 6-step breeding check every 5 days (GDD 4.8: Kim Tho 59.5% conception)
            if day % 5 == 0 {
                let roll = next_u32(&mut rng, 1000);
                if roll < 595 {
                    let litter = 2 + (next_u32(&mut rng, 3) as i64);
                    adult_pigs = (adult_pigs + litter).min(36);
                }
            }

            // Major wave every 6 days (GDD 18.2 Threat Budget)
            if day % 6 == 0 {
                let cycle = (day / 6) as i64;
                let total_pigs = adult_pigs + elder_pigs;
                let threat_budget = (100 * (100 + 18 * (cycle - 1)) * (1000 + 4 * total_pigs)) / 100_000;
                major_waves_survived += 1;
                gold += 150 + threat_budget / 2;
            }
        }

        grand_total_gold += gold + major_waves_survived * 10 + adult_pigs;
    }

    grand_total_gold
}

/// Runs all 4 simulation benchmarks and returns wall-clock milliseconds & deterministic checksums.
#[no_mangle]
pub extern "C" fn pt_run_all_benchmarks(
    bfs_iters: i32,
    boids_steps: i32,
    combat_ticks: i32,
    economy_seeds: i32,
) -> PolyglotBenchmarkResult {
    use std::time::Instant;

    let t0 = Instant::now();
    let c1 = pt_flowfield_bfs_120x80(bfs_iters);
    let d1 = t0.elapsed().as_secs_f64() * 1000.0;

    let t1 = Instant::now();
    let c2 = pt_boids_heightfield_step(boids_steps);
    let d2 = t1.elapsed().as_secs_f64() * 1000.0;

    let t2 = Instant::now();
    let c3 = pt_combat_power_wave_tick(combat_ticks);
    let d3 = t2.elapsed().as_secs_f64() * 1000.0;

    let t3 = Instant::now();
    let c4 = pt_montecarlo_economy_60d(economy_seeds);
    let d4 = t3.elapsed().as_secs_f64() * 1000.0;

    PolyglotBenchmarkResult {
        kernel1_bfs_ms: d1,
        kernel2_boids_ms: d2,
        kernel3_combat_ms: d3,
        kernel4_economy_ms: d4,
        total_ms: d1 + d2 + d3 + d4,
        checksum1: c1,
        checksum2: c2,
        checksum3: c3,
        checksum4: c4,
    }
}
