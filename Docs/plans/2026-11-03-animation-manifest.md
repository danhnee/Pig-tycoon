# Animation request list (generated from Core AnimationClips)

Each clip: 4 facings (down/left/up/right), 72×84 cells, same pivot as idle. The runtime plays the first clip of its fallback chain that exists, so clips can land one at a time.

## Body (An and Khoa each)

| P | Clip | Min frames | Loop | Shows |
|---|---|---|---|---|
| 1 | `idle` | 8 | yes | Đứng thở, vai nhún nhẹ, tóc/áo đung đưa |
| 1 | `walk` | 8 | yes | Đi: dáng người thật (cao ~56 px, chân ~45%), 2 bước/chu kỳ, bàn chân dang tối đa ~0,4 chiều cao, khung giữa 2 chân chụm, tay đánh, nhún 1–2 px |
| 1 | `run` | 8 | yes | Chạy: nghiêng tới trước, gối gập, bàn chân dang tối đa ~0,65 chiều cao, có khung bay, tóc/áo bay |
| 1 | `attack_slash_1` | 8 |  | Đòn thường 1/3 kiểu slash: lấy đà → khung nhòe (smear) → theo đà → thu thế |
| 1 | `attack_slash_2` | 8 |  | Đòn thường 2/3 kiểu slash: lấy đà → khung nhòe (smear) → theo đà → thu thế |
| 1 | `attack_slash_3` | 10 |  | Đòn thường 3/3 kiểu slash: lấy đà → khung nhòe (smear) → theo đà → thu thế |
| 1 | `attack_thrust_1` | 8 |  | Đòn thường 1/3 kiểu thrust: lấy đà → khung nhòe (smear) → theo đà → thu thế |
| 1 | `attack_thrust_2` | 8 |  | Đòn thường 2/3 kiểu thrust: lấy đà → khung nhòe (smear) → theo đà → thu thế |
| 1 | `attack_thrust_3` | 10 |  | Đòn thường 3/3 kiểu thrust: lấy đà → khung nhòe (smear) → theo đà → thu thế |
| 1 | `attack_smash_1` | 8 |  | Đòn thường 1/3 kiểu smash: lấy đà → khung nhòe (smear) → theo đà → thu thế |
| 1 | `attack_smash_2` | 8 |  | Đòn thường 2/3 kiểu smash: lấy đà → khung nhòe (smear) → theo đà → thu thế |
| 1 | `attack_smash_3` | 10 |  | Đòn thường 3/3 kiểu smash: lấy đà → khung nhòe (smear) → theo đà → thu thế |
| 1 | `attack_ranged_1` | 8 |  | Đòn thường 1/3 kiểu ranged: lấy đà → khung nhòe (smear) → theo đà → thu thế |
| 1 | `attack_ranged_2` | 8 |  | Đòn thường 2/3 kiểu ranged: lấy đà → khung nhòe (smear) → theo đà → thu thế |
| 1 | `attack_ranged_3` | 10 |  | Đòn thường 3/3 kiểu ranged: lấy đà → khung nhòe (smear) → theo đà → thu thế |
| 1 | `dodge` | 8 |  | Lướt né: ngồi thấp, bật, lướt, chống tay tiếp đất |
| 1 | `counter` | 8 |  | Phản kích: gạt đòn rồi đánh trả ngay |
| 1 | `hit_light` | 4 |  | Trúng đòn nhẹ: giật đầu/vai 1 nhịp |
| 1 | `hit_heavy` | 6 |  | Trúng đòn nặng: lùi 2 bước, mất thăng bằng |
| 1 | `faint` | 10 |  | Ngất (Chống Chịu về 0): khuỵu gối, ngã nghiêng |
| 2 | `interact_feed` | 8 |  | Múc cám đổ máng |
| 2 | `interact_water` | 8 |  | Xách xô đổ nước |
| 2 | `interact_clean` | 8 |  | Cào dọn chuồng |
| 2 | `interact_repair` | 8 |  | Gõ búa vá rào |
| 2 | `interact_gather` | 8 |  | Cúi nhặt vật liệu |
| 2 | `interact_carry` | 8 | yes | Vác bao trên vai (đi được) |
| 2 | `interact_observe` | 8 |  | Ngồi xổm quan sát heo (An) |
| 2 | `whistle` | 8 |  | Khoa: huýt sáo gom đàn, tay đưa lên miệng |
| 2 | `horn` | 10 |  | Thổi tù và lùa đàn về Nơi Trú |
| 2 | `idle_fidget_1` | 12 |  | Đứng lâu: vươn vai / phủi bụi áo |
| 2 | `idle_fidget_2` | 12 |  | Đứng lâu: An xem sổ hồng / Khoa ghi sổ |
| 3 | `skill_bo_phap_luot` | 10 |  | Võ kỹ Bộ Pháp Lướt (Dash) — dáng chung |
| 3 | `skill_doan_anh_tram` | 10 |  | Võ kỹ Đoạn Ảnh Trảm (Flurry) — dáng chung |
| 3 | `skill_xuyen_bay` | 10 |  | Võ kỹ Xuyên Bầy (DashThrough) — dáng chung |
| 3 | `skill_cuu_nguy` | 10 |  | Võ kỹ Cứu Nguy (Rescue) — dáng chung |
| 3 | `skill_tiep_vien_toc_hanh` | 10 |  | Võ kỹ Tiếp Viện Tốc Hành (Teleport) — dáng chung |
| 3 | `skill_vo_anh_bo` | 10 |  | Võ kỹ Vô Ảnh Bộ (Decoy) — dáng chung |
| 3 | `skill_pha_giap` | 10 |  | Võ kỹ Phá Giáp (ArmorBreak) — dáng chung |
| 3 | `skill_chan_dia` | 10 |  | Võ kỹ Chấn Địa (PushCircle) — dáng chung |
| 3 | `skill_tram_tuyen` | 10 |  | Võ kỹ Trảm Tuyến (LineSweep) — dáng chung |
| 3 | `skill_phan_chan` | 10 |  | Võ kỹ Phản Chấn (Counter) — dáng chung |
| 3 | `skill_neo_kich` | 10 |  | Võ kỹ Neo Kích (Pin) — dáng chung |
| 3 | `skill_thien_chuy` | 10 |  | Võ kỹ Thiên Chùy (Execute) — dáng chung |
| 3 | `skill_moc_keo` | 10 |  | Võ kỹ Móc Kéo (Pull) — dáng chung |
| 3 | `skill_an_tin_hieu` | 10 |  | Võ kỹ Ấn Tín Hiệu (Mark) — dáng chung |
| 3 | `skill_xoay_bay` | 10 |  | Võ kỹ Xoay Bầy (ConeTurn) — dáng chung |
| 3 | `skill_troi_luoi` | 10 |  | Võ kỹ Trói Lưới (Root) — dáng chung |
| 3 | `skill_dung_chot` | 10 |  | Võ kỹ Dựng Chốt (Stakes) — dáng chung |
| 3 | `skill_thien_la` | 10 |  | Võ kỹ Thiên La (SlowField) — dáng chung |
| 3 | `skill_tru_bo` | 10 |  | Võ kỹ Trụ Bộ (Stance) — dáng chung |
| 3 | `skill_ho_than` | 10 |  | Võ kỹ Hộ Thân (Shield) — dáng chung |
| 3 | `skill_khieu_chien` | 10 |  | Võ kỹ Khiêu Chiến (Taunt) — dáng chung |
| 3 | `skill_chan_mang` | 10 |  | Võ kỹ Chắn Mạng (Intercept) — dáng chung |
| 3 | `skill_kim_cuong_the` | 10 |  | Võ kỹ Kim Cương Thể (Fortify) — dáng chung |
| 4 | `skill_bo_phap_luot_slash` | 10 |  | Bộ Pháp Lướt với vũ khí slash: cùng nhịp, đổi tay cầm/quỹ đạo |
| 4 | `skill_bo_phap_luot_thrust` | 10 |  | Bộ Pháp Lướt với vũ khí thrust: cùng nhịp, đổi tay cầm/quỹ đạo |
| 4 | `skill_bo_phap_luot_smash` | 10 |  | Bộ Pháp Lướt với vũ khí smash: cùng nhịp, đổi tay cầm/quỹ đạo |
| 4 | `skill_bo_phap_luot_ranged` | 10 |  | Bộ Pháp Lướt với vũ khí ranged: cùng nhịp, đổi tay cầm/quỹ đạo |
| 4 | `skill_doan_anh_tram_slash` | 10 |  | Đoạn Ảnh Trảm với vũ khí slash: cùng nhịp, đổi tay cầm/quỹ đạo |
| 4 | `skill_doan_anh_tram_thrust` | 10 |  | Đoạn Ảnh Trảm với vũ khí thrust: cùng nhịp, đổi tay cầm/quỹ đạo |
| 4 | `skill_doan_anh_tram_smash` | 10 |  | Đoạn Ảnh Trảm với vũ khí smash: cùng nhịp, đổi tay cầm/quỹ đạo |
| 4 | `skill_doan_anh_tram_ranged` | 10 |  | Đoạn Ảnh Trảm với vũ khí ranged: cùng nhịp, đổi tay cầm/quỹ đạo |
| 4 | `skill_xuyen_bay_slash` | 10 |  | Xuyên Bầy với vũ khí slash: cùng nhịp, đổi tay cầm/quỹ đạo |
| 4 | `skill_xuyen_bay_thrust` | 10 |  | Xuyên Bầy với vũ khí thrust: cùng nhịp, đổi tay cầm/quỹ đạo |
| 4 | `skill_xuyen_bay_smash` | 10 |  | Xuyên Bầy với vũ khí smash: cùng nhịp, đổi tay cầm/quỹ đạo |
| 4 | `skill_xuyen_bay_ranged` | 10 |  | Xuyên Bầy với vũ khí ranged: cùng nhịp, đổi tay cầm/quỹ đạo |
| 4 | `skill_cuu_nguy_slash` | 10 |  | Cứu Nguy với vũ khí slash: cùng nhịp, đổi tay cầm/quỹ đạo |
| 4 | `skill_cuu_nguy_thrust` | 10 |  | Cứu Nguy với vũ khí thrust: cùng nhịp, đổi tay cầm/quỹ đạo |
| 4 | `skill_cuu_nguy_smash` | 10 |  | Cứu Nguy với vũ khí smash: cùng nhịp, đổi tay cầm/quỹ đạo |
| 4 | `skill_cuu_nguy_ranged` | 10 |  | Cứu Nguy với vũ khí ranged: cùng nhịp, đổi tay cầm/quỹ đạo |
| 4 | `skill_tiep_vien_toc_hanh_slash` | 10 |  | Tiếp Viện Tốc Hành với vũ khí slash: cùng nhịp, đổi tay cầm/quỹ đạo |
| 4 | `skill_tiep_vien_toc_hanh_thrust` | 10 |  | Tiếp Viện Tốc Hành với vũ khí thrust: cùng nhịp, đổi tay cầm/quỹ đạo |
| 4 | `skill_tiep_vien_toc_hanh_smash` | 10 |  | Tiếp Viện Tốc Hành với vũ khí smash: cùng nhịp, đổi tay cầm/quỹ đạo |
| 4 | `skill_tiep_vien_toc_hanh_ranged` | 10 |  | Tiếp Viện Tốc Hành với vũ khí ranged: cùng nhịp, đổi tay cầm/quỹ đạo |
| 4 | `skill_vo_anh_bo_slash` | 10 |  | Vô Ảnh Bộ với vũ khí slash: cùng nhịp, đổi tay cầm/quỹ đạo |
| 4 | `skill_vo_anh_bo_thrust` | 10 |  | Vô Ảnh Bộ với vũ khí thrust: cùng nhịp, đổi tay cầm/quỹ đạo |
| 4 | `skill_vo_anh_bo_smash` | 10 |  | Vô Ảnh Bộ với vũ khí smash: cùng nhịp, đổi tay cầm/quỹ đạo |
| 4 | `skill_vo_anh_bo_ranged` | 10 |  | Vô Ảnh Bộ với vũ khí ranged: cùng nhịp, đổi tay cầm/quỹ đạo |
| 4 | `skill_pha_giap_slash` | 10 |  | Phá Giáp với vũ khí slash: cùng nhịp, đổi tay cầm/quỹ đạo |
| 4 | `skill_pha_giap_thrust` | 10 |  | Phá Giáp với vũ khí thrust: cùng nhịp, đổi tay cầm/quỹ đạo |
| 4 | `skill_pha_giap_smash` | 10 |  | Phá Giáp với vũ khí smash: cùng nhịp, đổi tay cầm/quỹ đạo |
| 4 | `skill_pha_giap_ranged` | 10 |  | Phá Giáp với vũ khí ranged: cùng nhịp, đổi tay cầm/quỹ đạo |
| 4 | `skill_chan_dia_slash` | 10 |  | Chấn Địa với vũ khí slash: cùng nhịp, đổi tay cầm/quỹ đạo |
| 4 | `skill_chan_dia_thrust` | 10 |  | Chấn Địa với vũ khí thrust: cùng nhịp, đổi tay cầm/quỹ đạo |
| 4 | `skill_chan_dia_smash` | 10 |  | Chấn Địa với vũ khí smash: cùng nhịp, đổi tay cầm/quỹ đạo |
| 4 | `skill_chan_dia_ranged` | 10 |  | Chấn Địa với vũ khí ranged: cùng nhịp, đổi tay cầm/quỹ đạo |
| 4 | `skill_tram_tuyen_slash` | 10 |  | Trảm Tuyến với vũ khí slash: cùng nhịp, đổi tay cầm/quỹ đạo |
| 4 | `skill_tram_tuyen_thrust` | 10 |  | Trảm Tuyến với vũ khí thrust: cùng nhịp, đổi tay cầm/quỹ đạo |
| 4 | `skill_tram_tuyen_smash` | 10 |  | Trảm Tuyến với vũ khí smash: cùng nhịp, đổi tay cầm/quỹ đạo |
| 4 | `skill_tram_tuyen_ranged` | 10 |  | Trảm Tuyến với vũ khí ranged: cùng nhịp, đổi tay cầm/quỹ đạo |
| 4 | `skill_phan_chan_slash` | 10 |  | Phản Chấn với vũ khí slash: cùng nhịp, đổi tay cầm/quỹ đạo |
| 4 | `skill_phan_chan_thrust` | 10 |  | Phản Chấn với vũ khí thrust: cùng nhịp, đổi tay cầm/quỹ đạo |
| 4 | `skill_phan_chan_smash` | 10 |  | Phản Chấn với vũ khí smash: cùng nhịp, đổi tay cầm/quỹ đạo |
| 4 | `skill_phan_chan_ranged` | 10 |  | Phản Chấn với vũ khí ranged: cùng nhịp, đổi tay cầm/quỹ đạo |
| 4 | `skill_neo_kich_slash` | 10 |  | Neo Kích với vũ khí slash: cùng nhịp, đổi tay cầm/quỹ đạo |
| 4 | `skill_neo_kich_thrust` | 10 |  | Neo Kích với vũ khí thrust: cùng nhịp, đổi tay cầm/quỹ đạo |
| 4 | `skill_neo_kich_smash` | 10 |  | Neo Kích với vũ khí smash: cùng nhịp, đổi tay cầm/quỹ đạo |
| 4 | `skill_neo_kich_ranged` | 10 |  | Neo Kích với vũ khí ranged: cùng nhịp, đổi tay cầm/quỹ đạo |
| 4 | `skill_thien_chuy_slash` | 10 |  | Thiên Chùy với vũ khí slash: cùng nhịp, đổi tay cầm/quỹ đạo |
| 4 | `skill_thien_chuy_thrust` | 10 |  | Thiên Chùy với vũ khí thrust: cùng nhịp, đổi tay cầm/quỹ đạo |
| 4 | `skill_thien_chuy_smash` | 10 |  | Thiên Chùy với vũ khí smash: cùng nhịp, đổi tay cầm/quỹ đạo |
| 4 | `skill_thien_chuy_ranged` | 10 |  | Thiên Chùy với vũ khí ranged: cùng nhịp, đổi tay cầm/quỹ đạo |
| 4 | `skill_moc_keo_slash` | 10 |  | Móc Kéo với vũ khí slash: cùng nhịp, đổi tay cầm/quỹ đạo |
| 4 | `skill_moc_keo_thrust` | 10 |  | Móc Kéo với vũ khí thrust: cùng nhịp, đổi tay cầm/quỹ đạo |
| 4 | `skill_moc_keo_smash` | 10 |  | Móc Kéo với vũ khí smash: cùng nhịp, đổi tay cầm/quỹ đạo |
| 4 | `skill_moc_keo_ranged` | 10 |  | Móc Kéo với vũ khí ranged: cùng nhịp, đổi tay cầm/quỹ đạo |
| 4 | `skill_an_tin_hieu_slash` | 10 |  | Ấn Tín Hiệu với vũ khí slash: cùng nhịp, đổi tay cầm/quỹ đạo |
| 4 | `skill_an_tin_hieu_thrust` | 10 |  | Ấn Tín Hiệu với vũ khí thrust: cùng nhịp, đổi tay cầm/quỹ đạo |
| 4 | `skill_an_tin_hieu_smash` | 10 |  | Ấn Tín Hiệu với vũ khí smash: cùng nhịp, đổi tay cầm/quỹ đạo |
| 4 | `skill_an_tin_hieu_ranged` | 10 |  | Ấn Tín Hiệu với vũ khí ranged: cùng nhịp, đổi tay cầm/quỹ đạo |
| 4 | `skill_xoay_bay_slash` | 10 |  | Xoay Bầy với vũ khí slash: cùng nhịp, đổi tay cầm/quỹ đạo |
| 4 | `skill_xoay_bay_thrust` | 10 |  | Xoay Bầy với vũ khí thrust: cùng nhịp, đổi tay cầm/quỹ đạo |
| 4 | `skill_xoay_bay_smash` | 10 |  | Xoay Bầy với vũ khí smash: cùng nhịp, đổi tay cầm/quỹ đạo |
| 4 | `skill_xoay_bay_ranged` | 10 |  | Xoay Bầy với vũ khí ranged: cùng nhịp, đổi tay cầm/quỹ đạo |
| 4 | `skill_troi_luoi_slash` | 10 |  | Trói Lưới với vũ khí slash: cùng nhịp, đổi tay cầm/quỹ đạo |
| 4 | `skill_troi_luoi_thrust` | 10 |  | Trói Lưới với vũ khí thrust: cùng nhịp, đổi tay cầm/quỹ đạo |
| 4 | `skill_troi_luoi_smash` | 10 |  | Trói Lưới với vũ khí smash: cùng nhịp, đổi tay cầm/quỹ đạo |
| 4 | `skill_troi_luoi_ranged` | 10 |  | Trói Lưới với vũ khí ranged: cùng nhịp, đổi tay cầm/quỹ đạo |
| 4 | `skill_dung_chot_slash` | 10 |  | Dựng Chốt với vũ khí slash: cùng nhịp, đổi tay cầm/quỹ đạo |
| 4 | `skill_dung_chot_thrust` | 10 |  | Dựng Chốt với vũ khí thrust: cùng nhịp, đổi tay cầm/quỹ đạo |
| 4 | `skill_dung_chot_smash` | 10 |  | Dựng Chốt với vũ khí smash: cùng nhịp, đổi tay cầm/quỹ đạo |
| 4 | `skill_dung_chot_ranged` | 10 |  | Dựng Chốt với vũ khí ranged: cùng nhịp, đổi tay cầm/quỹ đạo |
| 4 | `skill_thien_la_slash` | 10 |  | Thiên La với vũ khí slash: cùng nhịp, đổi tay cầm/quỹ đạo |
| 4 | `skill_thien_la_thrust` | 10 |  | Thiên La với vũ khí thrust: cùng nhịp, đổi tay cầm/quỹ đạo |
| 4 | `skill_thien_la_smash` | 10 |  | Thiên La với vũ khí smash: cùng nhịp, đổi tay cầm/quỹ đạo |
| 4 | `skill_thien_la_ranged` | 10 |  | Thiên La với vũ khí ranged: cùng nhịp, đổi tay cầm/quỹ đạo |
| 4 | `skill_tru_bo_slash` | 10 |  | Trụ Bộ với vũ khí slash: cùng nhịp, đổi tay cầm/quỹ đạo |
| 4 | `skill_tru_bo_thrust` | 10 |  | Trụ Bộ với vũ khí thrust: cùng nhịp, đổi tay cầm/quỹ đạo |
| 4 | `skill_tru_bo_smash` | 10 |  | Trụ Bộ với vũ khí smash: cùng nhịp, đổi tay cầm/quỹ đạo |
| 4 | `skill_tru_bo_ranged` | 10 |  | Trụ Bộ với vũ khí ranged: cùng nhịp, đổi tay cầm/quỹ đạo |
| 4 | `skill_ho_than_slash` | 10 |  | Hộ Thân với vũ khí slash: cùng nhịp, đổi tay cầm/quỹ đạo |
| 4 | `skill_ho_than_thrust` | 10 |  | Hộ Thân với vũ khí thrust: cùng nhịp, đổi tay cầm/quỹ đạo |
| 4 | `skill_ho_than_smash` | 10 |  | Hộ Thân với vũ khí smash: cùng nhịp, đổi tay cầm/quỹ đạo |
| 4 | `skill_ho_than_ranged` | 10 |  | Hộ Thân với vũ khí ranged: cùng nhịp, đổi tay cầm/quỹ đạo |
| 4 | `skill_khieu_chien_slash` | 10 |  | Khiêu Chiến với vũ khí slash: cùng nhịp, đổi tay cầm/quỹ đạo |
| 4 | `skill_khieu_chien_thrust` | 10 |  | Khiêu Chiến với vũ khí thrust: cùng nhịp, đổi tay cầm/quỹ đạo |
| 4 | `skill_khieu_chien_smash` | 10 |  | Khiêu Chiến với vũ khí smash: cùng nhịp, đổi tay cầm/quỹ đạo |
| 4 | `skill_khieu_chien_ranged` | 10 |  | Khiêu Chiến với vũ khí ranged: cùng nhịp, đổi tay cầm/quỹ đạo |
| 4 | `skill_chan_mang_slash` | 10 |  | Chắn Mạng với vũ khí slash: cùng nhịp, đổi tay cầm/quỹ đạo |
| 4 | `skill_chan_mang_thrust` | 10 |  | Chắn Mạng với vũ khí thrust: cùng nhịp, đổi tay cầm/quỹ đạo |
| 4 | `skill_chan_mang_smash` | 10 |  | Chắn Mạng với vũ khí smash: cùng nhịp, đổi tay cầm/quỹ đạo |
| 4 | `skill_chan_mang_ranged` | 10 |  | Chắn Mạng với vũ khí ranged: cùng nhịp, đổi tay cầm/quỹ đạo |
| 4 | `skill_kim_cuong_the_slash` | 10 |  | Kim Cương Thể với vũ khí slash: cùng nhịp, đổi tay cầm/quỹ đạo |
| 4 | `skill_kim_cuong_the_thrust` | 10 |  | Kim Cương Thể với vũ khí thrust: cùng nhịp, đổi tay cầm/quỹ đạo |
| 4 | `skill_kim_cuong_the_smash` | 10 |  | Kim Cương Thể với vũ khí smash: cùng nhịp, đổi tay cầm/quỹ đạo |
| 4 | `skill_kim_cuong_the_ranged` | 10 |  | Kim Cương Thể với vũ khí ranged: cùng nhịp, đổi tay cầm/quỹ đạo |

## Weapon overlay layers (per protagonist per weapon: `an__wpn_dao_rua`, …)

- **Dao Rựa** (`wpn_dao_rua`, slash): `idle`, `walk`, `run`, `attack_slash_1`, `attack_slash_2`, `attack_slash_3`, `dodge`, `counter`, `skill_bo_phap_luot_slash`, `skill_doan_anh_tram_slash`, `skill_xuyen_bay_slash`, `skill_cuu_nguy_slash`, `skill_tiep_vien_toc_hanh_slash`, `skill_vo_anh_bo_slash`, `skill_pha_giap_slash`, `skill_chan_dia_slash`, `skill_tram_tuyen_slash`, `skill_phan_chan_slash`, `skill_neo_kich_slash`, `skill_thien_chuy_slash`, `skill_moc_keo_slash`, `skill_an_tin_hieu_slash`, `skill_xoay_bay_slash`, `skill_troi_luoi_slash`, `skill_dung_chot_slash`, `skill_thien_la_slash`, `skill_tru_bo_slash`, `skill_ho_than_slash`, `skill_khieu_chien_slash`, `skill_chan_mang_slash`, `skill_kim_cuong_the_slash`
- **Giáo Móc** (`wpn_giao_moc`, thrust): `idle`, `walk`, `run`, `attack_thrust_1`, `attack_thrust_2`, `attack_thrust_3`, `dodge`, `counter`, `skill_bo_phap_luot_thrust`, `skill_doan_anh_tram_thrust`, `skill_xuyen_bay_thrust`, `skill_cuu_nguy_thrust`, `skill_tiep_vien_toc_hanh_thrust`, `skill_vo_anh_bo_thrust`, `skill_pha_giap_thrust`, `skill_chan_dia_thrust`, `skill_tram_tuyen_thrust`, `skill_phan_chan_thrust`, `skill_neo_kich_thrust`, `skill_thien_chuy_thrust`, `skill_moc_keo_thrust`, `skill_an_tin_hieu_thrust`, `skill_xoay_bay_thrust`, `skill_troi_luoi_thrust`, `skill_dung_chot_thrust`, `skill_thien_la_thrust`, `skill_tru_bo_thrust`, `skill_ho_than_thrust`, `skill_khieu_chien_thrust`, `skill_chan_mang_thrust`, `skill_kim_cuong_the_thrust`
- **Búa Tạ** (`wpn_bua_ta`, smash): `idle`, `walk`, `run`, `attack_smash_1`, `attack_smash_2`, `attack_smash_3`, `dodge`, `counter`, `skill_bo_phap_luot_smash`, `skill_doan_anh_tram_smash`, `skill_xuyen_bay_smash`, `skill_cuu_nguy_smash`, `skill_tiep_vien_toc_hanh_smash`, `skill_vo_anh_bo_smash`, `skill_pha_giap_smash`, `skill_chan_dia_smash`, `skill_tram_tuyen_smash`, `skill_phan_chan_smash`, `skill_neo_kich_smash`, `skill_thien_chuy_smash`, `skill_moc_keo_smash`, `skill_an_tin_hieu_smash`, `skill_xoay_bay_smash`, `skill_troi_luoi_smash`, `skill_dung_chot_smash`, `skill_thien_la_smash`, `skill_tru_bo_smash`, `skill_ho_than_smash`, `skill_khieu_chien_smash`, `skill_chan_mang_smash`, `skill_kim_cuong_the_smash`
- **Nỏ Tay** (`wpn_no_tay`, ranged): `idle`, `walk`, `run`, `attack_ranged_1`, `attack_ranged_2`, `attack_ranged_3`, `dodge`, `counter`, `skill_bo_phap_luot_ranged`, `skill_doan_anh_tram_ranged`, `skill_xuyen_bay_ranged`, `skill_cuu_nguy_ranged`, `skill_tiep_vien_toc_hanh_ranged`, `skill_vo_anh_bo_ranged`, `skill_pha_giap_ranged`, `skill_chan_dia_ranged`, `skill_tram_tuyen_ranged`, `skill_phan_chan_ranged`, `skill_neo_kich_ranged`, `skill_thien_chuy_ranged`, `skill_moc_keo_ranged`, `skill_an_tin_hieu_ranged`, `skill_xoay_bay_ranged`, `skill_troi_luoi_ranged`, `skill_dung_chot_ranged`, `skill_thien_la_ranged`, `skill_tru_bo_ranged`, `skill_ho_than_ranged`, `skill_khieu_chien_ranged`, `skill_chan_mang_ranged`, `skill_kim_cuong_the_ranged`

Weapon-specific basic strings (optional, they beat the class version): `attack_<weaponId>_1..3`. A skill on one specific weapon: `skill_<skill>_<weaponId>`.
