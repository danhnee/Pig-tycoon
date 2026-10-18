use pig_tycoon_native::pt_run_all_benchmarks;

fn main() {
    // Warmup
    let _ = pt_run_all_benchmarks(10, 50, 200, 10);

    // Production benchmark workload:
    // - Kernel 1: 500 x 120x80 BFS rebuilds (4,800,000 grid cells)
    // - Kernel 2: 5,000 x 180-agent Boids + Bilinear Heightfield steps (900,000 agent physics steps)
    // - Kernel 3: 50,000 x 31-tower + 60-raider + PowerGrid combat ticks
    // - Kernel 4: 2,000 seeds x 60-day Monte Carlo Economy & Genetics campaigns (120,000 farm days)
    let res = pt_run_all_benchmarks(500, 5000, 50000, 2000);

    println!("LANGUAGE=Rust (rustc 1.90.0 -O3 LTO)");
    println!("KERNEL1_BFS_MS={:.3}", res.kernel1_bfs_ms);
    println!("KERNEL2_BOIDS_MS={:.3}", res.kernel2_boids_ms);
    println!("KERNEL3_COMBAT_MS={:.3}", res.kernel3_combat_ms);
    println!("KERNEL4_ECONOMY_MS={:.3}", res.kernel4_economy_ms);
    println!("TOTAL_MS={:.3}", res.total_ms);
    println!(
        "CHECKSUMS={}|{}|{}|{}",
        res.checksum1, res.checksum2, res.checksum3, res.checksum4
    );
}
