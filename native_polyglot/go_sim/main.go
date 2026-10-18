package main

import (
	"fmt"
	"runtime"
	"sync"
	"sync/atomic"
	"time"
)

func xorshift64star(state *uint64) uint64 {
	x := *state
	if x == 0 {
		x = 0x9E3779B97F4A7C15
	}
	x ^= x >> 12
	x ^= x << 25
	x ^= x >> 27
	*state = x
	return x * 0x2545F4914F6CDD1D
}

func nextU32(state *uint64, bound uint32) uint32 {
	r := uint32(xorshift64star(state) >> 32)
	return r % bound
}

func clampI64(v, lo, hi int64) int64 {
	if v < lo {
		return lo
	}
	if v > hi {
		return hi
	}
	return v
}

func clampI32(v, lo, hi int32) int32 {
	if v < lo {
		return lo
	}
	if v > hi {
		return hi
	}
	return v
}

func flowfieldBfs120x80(iterations int) int64 {
	const W = 120
	const H = 80
	const N = W * H

	var obstacles [N]uint8
	for y := 0; y < H; y++ {
		for x := 0; x < W; x++ {
			if x > 10 && x < 110 && y > 10 && y < 70 && (x%17 == 0) && (y%11 != 0) {
				obstacles[y*W+x] = 1
			}
		}
	}

	var dist [N]int32
	var queue [N]int
	var totalChecksum int64

	for iter := 0; iter < iterations; iter++ {
		for i := 0; i < N; i++ {
			dist[i] = -1
		}
		tx := 20 + (iter % 40)
		ty := 15 + (iter % 30)
		startIdx := ty*W + tx
		obstacles[startIdx] = 0

		head := 0
		tail := 0
		dist[startIdx] = 0
		queue[tail] = startIdx
		tail++

		for head < tail {
			cur := queue[head]
			head++
			d := dist[cur] + 1
			cx := cur % W
			cy := cur / W

			if cx > 0 {
				nb := cur - 1
				if obstacles[nb] == 0 && dist[nb] < 0 {
					dist[nb] = d
					queue[tail] = nb
					tail++
				}
			}
			if cx+1 < W {
				nb := cur + 1
				if obstacles[nb] == 0 && dist[nb] < 0 {
					dist[nb] = d
					queue[tail] = nb
					tail++
				}
			}
			if cy > 0 {
				nb := cur - W
				if obstacles[nb] == 0 && dist[nb] < 0 {
					dist[nb] = d
					queue[tail] = nb
					tail++
				}
			}
			if cy+1 < H {
				nb := cur + W
				if obstacles[nb] == 0 && dist[nb] < 0 {
					dist[nb] = d
					queue[tail] = nb
					tail++
				}
			}
		}

		var iterSum int64
		for i := 0; i < N; i++ {
			if dist[i] > 0 {
				iterSum += int64(dist[i])
			}
		}
		totalChecksum += iterSum
	}
	return totalChecksum
}

func boidsHeightfieldStep(steps int) int64 {
	const AGENTS = 180
	var px, pz, vx, vz [AGENTS]int64

	for i := 0; i < AGENTS; i++ {
		idx := int64(i)
		px[i] = ((idx * 173) % 40000) - 20000
		pz[i] = ((idx * 251) % 24000) - 12000
		vx[i] = ((idx * 37) % 200) - 100
		vz[i] = ((idx * 53) % 200) - 100
	}

	var heightChecksum int64
	for step := 0; step < steps; step++ {
		windBiasX := ((int64(step) * 13) % 21) - 10
		windBiasZ := ((int64(step) * 7) % 21) - 10

		for i := 0; i < AGENTS; i++ {
			var sepX, sepZ int64
			for offset := 1; offset <= 8; offset++ {
				j := (i + offset) % AGENTS
				dx := px[i] - px[j]
				dz := pz[i] - pz[j]
				distSq := dx*dx + dz*dz
				if distSq > 0 && distSq < 4000000 {
					sepX += dx / 32
					sepZ += dz / 32
				}
			}

			pullX := -px[i] / 128
			pullZ := -pz[i] / 128

			vx[i] = (vx[i]*3)/4 + sepX + pullX + windBiasX
			vz[i] = (vz[i]*3)/4 + sepZ + pullZ + windBiasZ

			vx[i] = clampI64(vx[i], -600, 600)
			vz[i] = clampI64(vz[i], -600, 600)

			px[i] = clampI64(px[i]+vx[i], -22000, 22000)
			pz[i] = clampI64(pz[i]+vz[i], -14000, 14000)

			gx := (px[i] + 60000) / 1000
			gz := (pz[i] + 40000) / 1000
			fx := (px[i] + 60000) % 1000
			fz := (pz[i] + 40000) % 1000

			h00 := (gx*19 + gz*31) % 2500
			h10 := ((gx+1)*19 + gz*31) % 2500
			h01 := (gx*19 + (gz+1)*31) % 2500
			h11 := ((gx+1)*19 + (gz+1)*31) % 2500

			hx0 := h00*(1000-fx) + h10*fx
			hx1 := h01*(1000-fx) + h11*fx
			hInterp := (hx0*(1000-fz) + hx1*fz) / 1000000

			heightChecksum += hInterp + (px[i] >> 4) - (pz[i] >> 4)
		}
	}
	return heightChecksum
}

func combatPowerWaveTick(ticks int) int64 {
	const TOWERS = 31
	const RAIDERS = 60

	var towerAmmo, towerCd [TOWERS]int32
	var raiderHp, raiderDist [RAIDERS]int32

	for t := 0; t < TOWERS; t++ {
		towerAmmo[t] = 50
		towerCd[t] = 0
	}
	for r := 0; r < RAIDERS; r++ {
		raiderHp[r] = 450 + int32(r*25)
		raiderDist[r] = 100 + int32(r*4)
	}

	var contCapacity int32 = 70
	var capacitor int32 = 1200
	var breakerTicks int32 = 0
	var totalDamageDealt, totalAmmoSpent, breakerTrips int64
	var playerGuard int32 = 1400
	var playerStamina int32 = 1800

	for tick := 0; tick < ticks; tick++ {
		demand := int32(55 + ((tick * 7) % 60))
		var fireMultPct int32
		if breakerTicks > 0 {
			breakerTicks--
			fireMultPct = 0
		} else if demand <= contCapacity {
			if capacitor+5 < 1200 {
				capacitor += 5
			} else {
				capacitor = 1200
			}
			fireMultPct = 100
		} else if demand*10 <= contCapacity*11 {
			capacitor -= (demand - contCapacity)
			if capacitor < 0 {
				capacitor = 0
			}
			fireMultPct = 100
		} else if demand*10 <= contCapacity*14 {
			fireMultPct = 75
		} else {
			breakerTicks = 4
			breakerTrips++
			fireMultPct = 0
		}

		targetRaider := tick % RAIDERS
		if raiderHp[targetRaider] <= 0 {
			raiderHp[targetRaider] = 500 + int32(tick%300)
			raiderDist[targetRaider] = 180
		}
		if raiderDist[targetRaider]-3 > 5 {
			raiderDist[targetRaider] -= 3
		} else {
			raiderDist[targetRaider] = 5
		}

		if fireMultPct > 0 {
			for t := 0; t < TOWERS; t++ {
				if towerCd[t] > 0 {
					towerCd[t]--
					continue
				}
				if towerAmmo[t] <= 0 {
					towerAmmo[t] = 50
				}
				baseDmg := int32(18 + (t%5)*6)
				dmg := (baseDmg * fireMultPct) / 100
				raiderHp[targetRaider] -= dmg
				towerAmmo[t]--
				towerCd[t] = int32(3 + (t % 4))
				totalDamageDealt += int64(dmg)
				totalAmmoSpent++
				if raiderHp[targetRaider] <= 0 {
					break
				}
			}
		}

		var rawEnemyHit int32 = 28
		if tick%4 == 0 {
			if playerStamina+20 < 1800 {
				playerStamina += 20
			} else {
				playerStamina = 1800
			}
			if playerGuard-5 > 150 {
				playerGuard -= 5
			} else {
				playerGuard = 150
			}
			totalDamageDealt += int64(rawEnemyHit / 2)
		} else {
			playerStamina = clampI32(playerStamina-12+15, 200, 1800)
			playerGuard = clampI32(playerGuard-18+22, 200, 1400)
		}
	}

	return totalDamageDealt*1000 + totalAmmoSpent*10 + breakerTrips + int64(playerGuard)
}

func simulateSingleSeed60Days(seed uint64) int64 {
	rng := seed * 0x9E3779B97F4A7C15
	var gold int64 = 1200
	var feedGrams int64 = 250000
	var adultPigs int64 = 12
	var elderPigs int64 = 2
	var pigWeightTenths int64 = 750
	var themePricePct int64 = 100
	var majorWavesSurvived int64 = 0

	for day := 1; day <= 60; day++ {
		if day%7 == 1 {
			r := nextU32(&rng, 100)
			if r < 24 {
				themePricePct = 100
			} else if r < 36 {
				themePricePct = 85
			} else if r < 48 {
				themePricePct = 130
			} else if r < 58 {
				themePricePct = 95
			} else if r < 66 {
				themePricePct = 75
			} else if r < 76 {
				themePricePct = 115
			} else {
				themePricePct = 105
			}
		}

		dailyNeedGrams := adultPigs*3500 + elderPigs*4200
		if feedGrams < dailyNeedGrams && gold >= 180 {
			gold -= 180
			feedGrams += 150000
		}

		if feedGrams >= dailyNeedGrams {
			feedGrams -= dailyNeedGrams
			if pigWeightTenths+45 < 1150 {
				pigWeightTenths += 45
			} else {
				pigWeightTenths = 1150
			}
		} else {
			feedGrams = 0
			if pigWeightTenths-20 > 500 {
				pigWeightTenths -= 20
			} else {
				pigWeightTenths = 500
			}
		}

		if pigWeightTenths >= 950 && adultPigs > 8 {
			salePrice := (pigWeightTenths * 3 * 120 * themePricePct * 130) / 1000000
			gold += salePrice
			adultPigs--
		}

		if day%5 == 0 {
			roll := nextU32(&rng, 1000)
			if roll < 595 {
				litter := int64(2 + nextU32(&rng, 3))
				if adultPigs+litter < 36 {
					adultPigs += litter
				} else {
					adultPigs = 36
				}
			}
		}

		if day%6 == 0 {
			cycle := int64(day / 6)
			totalPigs := adultPigs + elderPigs
			threatBudget := (100 * (100 + 18*(cycle-1)) * (1000 + 4*totalPigs)) / 100000
			majorWavesSurvived++
			gold += 150 + threatBudget/2
		}
	}

	return gold + majorWavesSurvived*10 + adultPigs
}

func montecarloEconomy60dSingleThread(seedsCount int) int64 {
	var total int64
	for s := 1; s <= seedsCount; s++ {
		total += simulateSingleSeed60Days(uint64(s))
	}
	return total
}

func montecarloEconomy60dGoroutines(seedsCount int) int64 {
	workers := runtime.NumCPU()
	var total int64
	var wg sync.WaitGroup
	chunk := (seedsCount + workers - 1) / workers

	for w := 0; w < workers; w++ {
		start := w*chunk + 1
		end := (w + 1) * chunk
		if end > seedsCount {
			end = seedsCount
		}
		if start > end {
			continue
		}
		wg.Add(1)
		go func(st, en int) {
			defer wg.Done()
			var localSum int64
			for s := st; s <= en; s++ {
				localSum += simulateSingleSeed60Days(uint64(s))
			}
			atomic.AddInt64(&total, localSum)
		}(start, end)
	}
	wg.Wait()
	return total
}

func main() {
	// Warmup
	_ = flowfieldBfs120x80(10)
	_ = boidsHeightfieldStep(50)
	_ = combatPowerWaveTick(200)
	_ = montecarloEconomy60dSingleThread(10)

	t0 := time.Now()
	c1 := flowfieldBfs120x80(500)
	d1 := float64(time.Since(t0).Nanoseconds()) / 1e6

	t1 := time.Now()
	c2 := boidsHeightfieldStep(5000)
	d2 := float64(time.Since(t1).Nanoseconds()) / 1e6

	t2 := time.Now()
	c3 := combatPowerWaveTick(50000)
	d3 := float64(time.Since(t2).Nanoseconds()) / 1e6

	t3 := time.Now()
	c4 := montecarloEconomy60dSingleThread(2000)
	d4 := float64(time.Since(t3).Nanoseconds()) / 1e6

	t4 := time.Now()
	c4Parallel := montecarloEconomy60dGoroutines(2000)
	d4Parallel := float64(time.Since(t4).Nanoseconds()) / 1e6

	fmt.Printf("LANGUAGE=Go (go1.26.2 windows/amd64)\n")
	fmt.Printf("KERNEL1_BFS_MS=%.3f\n", d1)
	fmt.Printf("KERNEL2_BOIDS_MS=%.3f\n", d2)
	fmt.Printf("KERNEL3_COMBAT_MS=%.3f\n", d3)
	fmt.Printf("KERNEL4_ECONOMY_MS=%.3f\n", d4)
	fmt.Printf("KERNEL4_ECONOMY_GOROUTINES_MS=%.3f (Workers=%d, Checksum=%d)\n", d4Parallel, runtime.NumCPU(), c4Parallel)
	fmt.Printf("TOTAL_MS=%.3f\n", d1+d2+d3+d4)
	fmt.Printf("CHECKSUMS=%d|%d|%d|%d\n", c1, c2, c3, c4)
}
