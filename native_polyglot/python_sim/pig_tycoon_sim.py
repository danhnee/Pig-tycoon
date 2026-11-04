#!/usr/bin/env python3
"""
Pig Tycoon v6.0 · Python 3 Reference Simulation & Native C-ABI ctypes Bridge
=============================================================================
Kiểm chứng chéo bit-for-bit 4 nhân tính toán đa ngôn ngữ (C/C++20, Rust, Go, Java 21, C#, Dart, Python 3)
và kết nối trực tiếp tới thư viện gốc `pig_tycoon_native.dll` / `pig_tycoon_cpp.dll` qua `ctypes`:
- Kernel 1: Flow-Field BFS 120x80 (500 iters -> Checksum 266653114)
- Kernel 2: Boids + Bilinear Heightfield 180 thực thể (5000 steps -> Checksum 2091061385)
- Kernel 3: Combat Wave + Lưới Điện 31 công trình (50000 ticks -> Checksum 2782632973)
- Kernel 4: Monte Carlo Kinh tế & Di truyền 60 ngày (2000 seeds -> Checksum 258354468)
"""

from __future__ import annotations

import ctypes
import json
import os
import sys
import time
from pathlib import Path
from typing import Dict, Any, Optional

EXPECTED_CHECKSUM_1 = 266653114
EXPECTED_CHECKSUM_2 = 2091061385
EXPECTED_CHECKSUM_3 = 2782632973
EXPECTED_CHECKSUM_4 = 258354468

MASK64 = 0xFFFFFFFFFFFFFFFF


def xorshift64star(state: list[int]) -> int:
    x = state[0] & MASK64
    if x == 0:
        x = 0x9E3779B97F4A7C15
    x ^= (x >> 12) & MASK64
    x ^= (x << 25) & MASK64
    x ^= (x >> 27) & MASK64
    state[0] = x
    return (x * 0x2545F4914F6CDD1D) & MASK64


def next_u32(state: list[int], bound: int) -> int:
    r = (xorshift64star(state) >> 32) & 0xFFFFFFFF
    return r % bound


def c_div(a: int, b: int) -> int:
    """Truncate-toward-zero integer division matching C/C++/Rust/C#/Go/Java/Dart."""
    if (a < 0) ^ (b < 0):
        return -(abs(a) // abs(b))
    return a // b


def c_mod(a: int, b: int) -> int:
    """Remainder with sign of dividend matching C/C++/Rust/C#/Go/Java/Dart."""
    return a - c_div(a, b) * b


def py_flowfield_bfs_120x80(iterations: int = 500) -> int:
    w, h = 120, 80
    n = w * h
    obstacles = bytearray(n)
    for y in range(h):
        for x in range(w):
            if 10 < x < 110 and 10 < y < 70 and (x % 17 == 0) and (y % 11 != 0):
                obstacles[y * w + x] = 1

    dist = [-1] * n
    queue = [0] * n
    total_checksum = 0

    for it in range(iterations):
        for i in range(n):
            dist[i] = -1
        tx = 20 + (it % 40)
        ty = 15 + (it % 30)
        start_idx = ty * w + tx
        obstacles[start_idx] = 0

        head = 0
        tail = 1
        dist[start_idx] = 0
        queue[0] = start_idx

        while head < tail:
            cur = queue[head]
            head += 1
            d = dist[cur] + 1
            cx = cur % w
            cy = cur // w

            if cx > 0:
                nb = cur - 1
                if obstacles[nb] == 0 and dist[nb] < 0:
                    dist[nb] = d
                    queue[tail] = nb
                    tail += 1
            if cx + 1 < w:
                nb = cur + 1
                if obstacles[nb] == 0 and dist[nb] < 0:
                    dist[nb] = d
                    queue[tail] = nb
                    tail += 1
            if cy > 0:
                nb = cur - w
                if obstacles[nb] == 0 and dist[nb] < 0:
                    dist[nb] = d
                    queue[tail] = nb
                    tail += 1
            if cy + 1 < h:
                nb = cur + w
                if obstacles[nb] == 0 and dist[nb] < 0:
                    dist[nb] = d
                    queue[tail] = nb
                    tail += 1

        iter_sum = 0
        for d_val in dist:
            if d_val > 0:
                iter_sum += d_val
        total_checksum += iter_sum

    return total_checksum


def py_boids_heightfield_step(steps: int = 5000) -> int:
    agents = 180
    px = [((i * 173) % 40000) - 20000 for i in range(agents)]
    pz = [((i * 251) % 24000) - 12000 for i in range(agents)]
    vx = [((i * 37) % 200) - 100 for i in range(agents)]
    vz = [((i * 53) % 200) - 100 for i in range(agents)]

    height_checksum = 0
    for step in range(steps):
        wind_bias_x = ((step * 13) % 21) - 10
        wind_bias_z = ((step * 7) % 21) - 10

        for i in range(agents):
            sep_x = 0
            sep_z = 0
            px_i = px[i]
            pz_i = pz[i]

            for offset in range(1, 9):
                j = (i + offset) % agents
                dx = px_i - px[j]
                dz = pz_i - pz[j]
                dist_sq = dx * dx + dz * dz
                if 0 < dist_sq < 4000000:
                    sep_x += c_div(dx, 32)
                    sep_z += c_div(dz, 32)

            pull_x = c_div(-px_i, 128)
            pull_z = c_div(-pz_i, 128)

            n_vx = c_div(vx[i] * 3, 4) + sep_x + pull_x + wind_bias_x
            n_vz = c_div(vz[i] * 3, 4) + sep_z + pull_z + wind_bias_z

            if n_vx < -600:
                n_vx = -600
            elif n_vx > 600:
                n_vx = 600
            if n_vz < -600:
                n_vz = -600
            elif n_vz > 600:
                n_vz = 600

            vx[i] = n_vx
            vz[i] = n_vz

            n_px = px_i + n_vx
            n_pz = pz_i + n_vz
            if n_px < -22000:
                n_px = -22000
            elif n_px > 22000:
                n_px = 22000
            if n_pz < -14000:
                n_pz = -14000
            elif n_pz > 14000:
                n_pz = 14000

            px[i] = n_px
            pz[i] = n_pz

            gx = (n_px + 60000) // 1000
            gz = (n_pz + 40000) // 1000
            fx = (n_px + 60000) % 1000
            fz = (n_pz + 40000) % 1000

            h00 = (gx * 19 + gz * 31) % 2500
            h10 = ((gx + 1) * 19 + gz * 31) % 2500
            h01 = (gx * 19 + (gz + 1) * 31) % 2500
            h11 = ((gx + 1) * 19 + (gz + 1) * 31) % 2500

            hx0 = h00 * (1000 - fx) + h10 * fx
            hx1 = h01 * (1000 - fx) + h11 * fx
            h_interp = (hx0 * (1000 - fz) + hx1 * fz) // 1000000

            height_checksum += h_interp + (n_px >> 4) - (n_pz >> 4)

    return height_checksum


def py_combat_power_wave_tick(ticks: int = 50000) -> int:
    towers = 31
    raiders = 60

    tower_ammo = [50] * towers
    tower_cd = [0] * towers
    raider_hp = [450 + r * 25 for r in range(raiders)]
    raider_dist = [100 + r * 4 for r in range(raiders)]

    cont_capacity = 70
    capacitor = 1200
    breaker_ticks = 0
    total_damage_dealt = 0
    total_ammo_spent = 0
    breaker_trips = 0
    player_guard = 1400
    player_stamina = 1800

    for tick in range(ticks):
        demand = 55 + ((tick * 7) % 60)
        if breaker_ticks > 0:
            breaker_ticks -= 1
            fire_mult_pct = 0
        elif demand <= cont_capacity:
            capacitor = min(1200, max(0, capacitor + 5))
            fire_mult_pct = 100
        elif demand * 10 <= cont_capacity * 11:
            capacitor = min(1200, max(0, capacitor - (demand - cont_capacity)))
            fire_mult_pct = 100
        elif demand * 10 <= cont_capacity * 14:
            fire_mult_pct = 75
        else:
            breaker_ticks = 4
            breaker_trips += 1
            fire_mult_pct = 0

        target_raider = tick % raiders
        if raider_hp[target_raider] <= 0:
            raider_hp[target_raider] = 500 + (tick % 300)
            raider_dist[target_raider] = 180
        raider_dist[target_raider] = min(1000, max(5, raider_dist[target_raider] - 3))

        if fire_mult_pct > 0:
            for t in range(towers):
                if tower_cd[t] > 0:
                    tower_cd[t] -= 1
                    continue
                if tower_ammo[t] <= 0:
                    tower_ammo[t] = 50
                base_dmg = 18 + (t % 5) * 6
                dmg = (base_dmg * fire_mult_pct) // 100
                raider_hp[target_raider] -= dmg
                tower_ammo[t] -= 1
                tower_cd[t] = 3 + (t % 4)
                total_damage_dealt += dmg
                total_ammo_spent += 1
                if raider_hp[target_raider] <= 0:
                    break

        raw_enemy_hit = 28
        if tick % 4 == 0:
            player_stamina = min(1800, max(200, player_stamina + 20))
            player_guard = min(1400, max(150, player_guard - 5))
            total_damage_dealt += raw_enemy_hit // 2
        else:
            player_stamina = min(1800, max(200, player_stamina - 12 + 15))
            player_guard = min(1400, max(200, player_guard - 18 + 22))

    return total_damage_dealt * 1000 + total_ammo_spent * 10 + breaker_trips + player_guard


def py_montecarlo_economy_60d(seeds_count: int = 2000) -> int:
    grand_total_gold = 0
    rng_state = [0]

    for s in range(1, seeds_count + 1):
        rng_state[0] = (s * 0x9E3779B97F4A7C15) & MASK64
        gold = 1200
        feed_grams = 250000
        adult_pigs = 12
        elder_pigs = 2
        pig_weight_tenths = 750
        theme_price_pct = 100
        major_waves_survived = 0

        for day in range(1, 61):
            if day % 7 == 1:
                r = next_u32(rng_state, 100)
                if r < 24:
                    theme_price_pct = 100
                elif r < 36:
                    theme_price_pct = 85
                elif r < 48:
                    theme_price_pct = 130
                elif r < 58:
                    theme_price_pct = 95
                elif r < 66:
                    theme_price_pct = 75
                elif r < 76:
                    theme_price_pct = 115
                else:
                    theme_price_pct = 105

            daily_need_grams = adult_pigs * 3500 + elder_pigs * 4200
            if feed_grams < daily_need_grams and gold >= 180:
                gold -= 180
                feed_grams += 150000

            if feed_grams >= daily_need_grams:
                feed_grams -= daily_need_grams
                pig_weight_tenths = min(1150, max(500, pig_weight_tenths + 45))
            else:
                feed_grams = 0
                pig_weight_tenths = min(1150, max(500, pig_weight_tenths - 20))

            if pig_weight_tenths >= 950 and adult_pigs > 8:
                sale_price = (pig_weight_tenths * 3 * 120 * theme_price_pct * 130) // 1000000
                gold += sale_price
                adult_pigs -= 1

            if day % 5 == 0:
                roll = next_u32(rng_state, 1000)
                if roll < 595:
                    litter = 2 + next_u32(rng_state, 3)
                    adult_pigs = min(36, max(0, adult_pigs + litter))

            if day % 6 == 0:
                cycle = day // 6
                total_pigs = adult_pigs + elder_pigs
                threat_budget = (100 * (100 + 18 * (cycle - 1)) * (1000 + 4 * total_pigs)) // 100000
                major_waves_survived += 1
                gold += 150 + (threat_budget // 2)

        grand_total_gold += gold + major_waves_survived * 10 + adult_pigs

    return grand_total_gold


def try_load_native_dll(dll_name: str) -> Optional[ctypes.CDLL]:
    repo_root = Path(__file__).resolve().parents[2]
    candidates = [
        repo_root / "native_polyglot" / "bin" / dll_name,
        repo_root / "native_polyglot" / "rust_core" / "target" / "release" / dll_name,
        repo_root / "native_polyglot" / "cpp_core" / dll_name,
    ]
    for path in candidates:
        if path.is_file():
            return ctypes.CDLL(str(path))
    return None


def run_verification_and_benchmark() -> Dict[str, Any]:
    rust_dll = try_load_native_dll("pig_tycoon_native.dll")
    cpp_dll = try_load_native_dll("pig_tycoon_cpp.dll")

    # 1. Verify Native Rust C-ABI via ctypes if available
    native_results = {}
    if rust_dll is not None:
        rust_dll.pt_flowfield_bfs_120x80.argtypes = [ctypes.c_int]
        rust_dll.pt_flowfield_bfs_120x80.restype = ctypes.c_longlong
        rust_dll.pt_boids_heightfield_step.argtypes = [ctypes.c_int]
        rust_dll.pt_boids_heightfield_step.restype = ctypes.c_longlong
        rust_dll.pt_combat_power_wave_tick.argtypes = [ctypes.c_int]
        rust_dll.pt_combat_power_wave_tick.restype = ctypes.c_longlong
        rust_dll.pt_montecarlo_economy_60d.argtypes = [ctypes.c_int]
        rust_dll.pt_montecarlo_economy_60d.restype = ctypes.c_longlong

        t0 = time.perf_counter()
        rc1 = int(rust_dll.pt_flowfield_bfs_120x80(500))
        t1 = time.perf_counter()
        rc2 = int(rust_dll.pt_boids_heightfield_step(5000))
        t2 = time.perf_counter()
        rc3 = int(rust_dll.pt_combat_power_wave_tick(50000))
        t3 = time.perf_counter()
        rc4 = int(rust_dll.pt_montecarlo_economy_60d(2000))
        t4 = time.perf_counter()
        native_results["rust_ctypes"] = {
            "bfs_ms": round((t1 - t0) * 1000.0, 3),
            "boids_ms": round((t2 - t1) * 1000.0, 3),
            "combat_ms": round((t3 - t2) * 1000.0, 3),
            "economy_ms": round((t4 - t3) * 1000.0, 3),
            "total_ms": round((t4 - t0) * 1000.0, 3),
            "checksums": [rc1, rc2, rc3, rc4],
            "bit_exact": [rc1, rc2, rc3, rc4]
            == [EXPECTED_CHECKSUM_1, EXPECTED_CHECKSUM_2, EXPECTED_CHECKSUM_3, EXPECTED_CHECKSUM_4],
        }

    # 2. Verify Pure Python 3 Reference Kernels
    p0 = time.perf_counter()
    pc1 = py_flowfield_bfs_120x80(500)
    p1 = time.perf_counter()
    pc2 = py_boids_heightfield_step(5000)
    p2 = time.perf_counter()
    pc3 = py_combat_power_wave_tick(50000)
    p3 = time.perf_counter()
    pc4 = py_montecarlo_economy_60d(2000)
    p4 = time.perf_counter()

    py_bit_exact = [pc1, pc2, pc3, pc4] == [
        EXPECTED_CHECKSUM_1,
        EXPECTED_CHECKSUM_2,
        EXPECTED_CHECKSUM_3,
        EXPECTED_CHECKSUM_4,
    ]

    report = {
        "python_version": sys.version.split()[0],
        "python_pure_reference": {
            "bfs_ms": round((p1 - p0) * 1000.0, 3),
            "boids_ms": round((p2 - p1) * 1000.0, 3),
            "combat_ms": round((p3 - p2) * 1000.0, 3),
            "economy_ms": round((p4 - p3) * 1000.0, 3),
            "total_ms": round((p4 - p0) * 1000.0, 3),
            "checksums": [pc1, pc2, pc3, pc4],
            "bit_exact": py_bit_exact,
        },
        "native_bridges": native_results,
    }
    return report


if __name__ == "__main__":
    result = run_verification_and_benchmark()
    print(json.dumps(result, indent=2, ensure_ascii=False))
    if not result["python_pure_reference"]["bit_exact"]:
        sys.exit(1)
