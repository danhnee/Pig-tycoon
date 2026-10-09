using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace PigTycoon.Core
{
    public enum CatalogCategory
    {
        Gene,
        Npc,
        Pet,
        Building,
        Enemy,
        Boss,
        Skill,
        Weather,
        HealthThreat,
        Event
    }

    public sealed class CatalogEntry
    {
        public string Id { get; }
        public CatalogCategory Category { get; }
        public string Name { get; }
        public string Owner { get; }
        public string SourceGdd { get; }

        public CatalogEntry(
            string id,
            CatalogCategory category,
            string name,
            string owner,
            string sourceGdd)
        {
            Id = id;
            Category = category;
            Name = name;
            Owner = owner;
            SourceGdd = sourceGdd;
        }
    }

    public static class DataCatalog
    {
        private static readonly Dictionary<CatalogCategory, int> ExpectedCounts =
            new Dictionary<CatalogCategory, int>
            {
                { CatalogCategory.Gene, 12 },
                { CatalogCategory.Npc, 12 },
                { CatalogCategory.Pet, 7 },
                { CatalogCategory.Building, 31 },
                { CatalogCategory.Enemy, 14 },
                { CatalogCategory.Boss, 3 },
                { CatalogCategory.Skill, 24 },
                { CatalogCategory.Weather, 10 },
                { CatalogCategory.HealthThreat, 10 },
                { CatalogCategory.Event, 14 }
            };

        private static readonly CatalogEntry[] EntriesInternal =
        {
            // Genes (GDD 5.2)
            new CatalogEntry("pig.gene.hong_dien", CatalogCategory.Gene, "Hồng Điền", "D", "GDD 5.2"),
            new CatalogEntry("pig.gene.lam_khe", CatalogCategory.Gene, "Lam Khê", "D", "GDD 5.2"),
            new CatalogEntry("pig.gene.moc_cuoc", CatalogCategory.Gene, "Mộc Cước", "D", "GDD 5.2"),
            new CatalogEntry("pig.gene.tho_tram", CatalogCategory.Gene, "Thổ Trầm", "D", "GDD 5.2"),
            new CatalogEntry("pig.gene.xich_mao", CatalogCategory.Gene, "Xích Mao", "D", "GDD 5.2"),
            new CatalogEntry("pig.gene.phong_mau", CatalogCategory.Gene, "Phong Mẫu", "D", "GDD 5.2"),
            new CatalogEntry("pig.gene.thiet_bi", CatalogCategory.Gene, "Thiết Bì", "D", "GDD 5.2"),
            new CatalogEntry("pig.gene.tinh_quang", CatalogCategory.Gene, "Tinh Quang", "D", "GDD 5.2"),
            new CatalogEntry("pig.gene.loi_mach", CatalogCategory.Gene, "Lôi Mạch", "D", "GDD 5.2"),
            new CatalogEntry("pig.gene.mong_nhan", CatalogCategory.Gene, "Mộng Nhãn", "D", "GDD 5.2"),
            new CatalogEntry("pig.gene.kim_tho", CatalogCategory.Gene, "Kim Thọ", "D", "GDD 5.2"),
            new CatalogEntry("pig.gene.hu_the", CatalogCategory.Gene, "Hư Thể", "D", "GDD 5.2"),

            // Farm / specialist NPCs (GDD 11.1)
            new CatalogEntry("pig.npc.quan_ly_ca", CatalogCategory.Npc, "Quản Lý Ca", "D", "GDD 11.1"),
            new CatalogEntry("pig.npc.nha_nghien_cuu_gen", CatalogCategory.Npc, "Nhà Nghiên Cứu Gen", "D", "GDD 11.1"),
            new CatalogEntry("pig.npc.bac_si_thu_y", CatalogCategory.Npc, "Bác Sĩ Thú Y", "D", "GDD 11.1"),
            new CatalogEntry("pig.npc.ky_su_xay_dung", CatalogCategory.Npc, "Kỹ Sư Xây Dựng", "D", "GDD 11.1"),
            new CatalogEntry("pig.npc.nha_buon", CatalogCategory.Npc, "Nhà Buôn", "D", "GDD 11.1"),
            new CatalogEntry("pig.npc.thu_kho", CatalogCategory.Npc, "Thủ Kho", "D", "GDD 11.1"),
            new CatalogEntry("pig.npc.nong_cong", CatalogCategory.Npc, "Nông Công", "D", "GDD 11.1"),

            // Combat / mercenary NPCs (GDD 11.2)
            new CatalogEntry("pig.npc.ve_binh_khien", CatalogCategory.Npc, "Vệ Binh Khiên", "D", "GDD 11.2"),
            new CatalogEntry("pig.npc.xa_thu_no", CatalogCategory.Npc, "Xạ Thủ Nỏ", "D", "GDD 11.2"),
            new CatalogEntry("pig.npc.ky_su_chien_truong", CatalogCategory.Npc, "Kỹ Sư Chiến Trường", "D", "GDD 11.2"),
            new CatalogEntry("pig.npc.tho_san_ufo", CatalogCategory.Npc, "Thợ Săn UFO", "D", "GDD 11.2"),
            new CatalogEntry("pig.npc.thich_khach_dem", CatalogCategory.Npc, "Thích Khách Đêm", "D", "GDD 11.2"),

            // Pets (GDD 11.3)
            new CatalogEntry("pig.pet.cho_chan_dan", CatalogCategory.Pet, "Chó Chăn Đàn", "D", "GDD 11.3"),
            new CatalogEntry("pig.pet.meo_kho", CatalogCategory.Pet, "Mèo Kho", "D", "GDD 11.3"),
            new CatalogEntry("pig.pet.heo_san_nam", CatalogCategory.Pet, "Heo Săn Nấm", "D", "GDD 11.3"),
            new CatalogEntry("pig.pet.qua_tin", CatalogCategory.Pet, "Quạ Tin", "D", "GDD 11.3"),
            new CatalogEntry("pig.pet.cho_ngao_canh", CatalogCategory.Pet, "Chó Ngao Canh", "D", "GDD 11.3"),
            new CatalogEntry("pig.pet.ngong_bao_dong", CatalogCategory.Pet, "Ngỗng Báo Động", "D", "GDD 11.3"),
            new CatalogEntry("pig.pet.cho_san_chien", CatalogCategory.Pet, "Chó Săn Chiến", "D", "GDD 11.3"),

            // Buildings (GDD 14.1-17)
            new CatalogEntry("pig.building.thap_canh_quan_sat", CatalogCategory.Building, "Tháp Canh Quan Sát", "D", "GDD 14.1"),
            new CatalogEntry("pig.building.radar_uy_hiep", CatalogCategory.Building, "Radar Uy Hiếp", "D", "GDD 14.1"),
            new CatalogEntry("pig.building.may_cong_huong_huyen_bi", CatalogCategory.Building, "Máy Cộng Hưởng Huyền Bí", "D", "GDD 14.1"),
            new CatalogEntry("pig.building.thien_nhan_vong_dai", CatalogCategory.Building, "Thiên Nhãn Vọng Đài", "D", "GDD 14.1"),
            new CatalogEntry("pig.building.tram_khi_tuong", CatalogCategory.Building, "Trạm Khí Tượng", "D", "GDD 14.2"),
            new CatalogEntry("pig.building.tram_tiep_song_cho", CatalogCategory.Building, "Trạm Tiếp Sóng Chợ", "D", "GDD 14.2"),
            new CatalogEntry("pig.building.ban_tinh_bao", CatalogCategory.Building, "Bàn Tình Báo", "D", "GDD 14.2"),
            new CatalogEntry("pig.building.dai_do_di_tuong", CatalogCategory.Building, "Đài Dò Dị Tượng", "D", "GDD 14.2"),
            new CatalogEntry("pig.building.tram_hoi_suc", CatalogCategory.Building, "Trạm Hồi Sức", "D", "GDD 15"),
            new CatalogEntry("pig.building.den_pha_cong_nghiep", CatalogCategory.Building, "Đèn Pha Công Nghiệp", "D", "GDD 15"),
            new CatalogEntry("pig.building.may_phat_dien", CatalogCategory.Building, "Máy Phát Điện", "D", "GDD 15"),
            new CatalogEntry("pig.building.bom_lam_mat", CatalogCategory.Building, "Bơm Làm Mát", "D", "GDD 15"),
            new CatalogEntry("pig.building.cot_thu_loi", CatalogCategory.Building, "Cột Thu Lôi", "D", "GDD 15"),
            new CatalogEntry("pig.building.van_linh_hoi_nguyen_tran", CatalogCategory.Building, "Vạn Linh Hồi Nguyên Trận", "D", "GDD 15"),
            new CatalogEntry("pig.building.no_xuyen_van", CatalogCategory.Building, "Nỏ Xuyên Vân", "D", "GDD 16"),
            new CatalogEntry("pig.building.thap_ho_quang", CatalogCategory.Building, "Tháp Hồ Quang", "D", "GDD 16"),
            new CatalogEntry("pig.building.thap_phong_khong_skyhook", CatalogCategory.Building, "Tháp Phòng Không Skyhook", "D", "GDD 16"),
            new CatalogEntry("pig.building.dai_thien_loi", CatalogCategory.Building, "Đài Thiên Lôi", "D", "GDD 16"),
            new CatalogEntry("pig.building.cuu_tieu_loi_phao", CatalogCategory.Building, "Cửu Tiêu Lôi Pháo", "D", "GDD 16"),
            new CatalogEntry("pig.building.hang_rao", CatalogCategory.Building, "Hàng Rào Gỗ / Thép", "D", "GDD 17"),
            new CatalogEntry("pig.building.cong_gia_co", CatalogCategory.Building, "Cổng Gia Cố", "D", "GDD 17"),
            new CatalogEntry("pig.building.may_chieu_khien", CatalogCategory.Building, "Máy Chiếu Khiên", "D", "GDD 17"),
            new CatalogEntry("pig.building.u_bao_cat", CatalogCategory.Building, "Ụ Bao Cát", "D", "GDD 17"),
            new CatalogEntry("pig.building.ban_chong", CatalogCategory.Building, "Bàn Chông", "D", "GDD 17"),
            new CatalogEntry("pig.building.tam_soc_dien", CatalogCategory.Building, "Tấm Sốc Điện", "D", "GDD 17"),
            new CatalogEntry("pig.building.ho_keo", CatalogCategory.Building, "Hố Keo", "D", "GDD 17"),
            new CatalogEntry("pig.building.luoi_emp", CatalogCategory.Building, "Lưới EMP", "D", "GDD 17"),
            new CatalogEntry("pig.building.min_no", CatalogCategory.Building, "Mìn Nổ", "D", "GDD 17"),
            new CatalogEntry("pig.building.chot_canh_gac", CatalogCategory.Building, "Chốt Canh Gác", "D", "GDD 17"),
            new CatalogEntry("pig.building.co_tap_ket", CatalogCategory.Building, "Cờ Tập Kết", "D", "GDD 17"),
            new CatalogEntry("pig.building.den_hieu_danh_chan", CatalogCategory.Building, "Đèn Hiệu Đánh Chặn", "D", "GDD 17"),

            // Enemies (GDD 19.1)
            new CatalogEntry("pig.enemy.chuot_gam_kho", CatalogCategory.Enemy, "Chuột Gặm Kho", "D", "GDD 19.1"),
            new CatalogEntry("pig.enemy.ke_cat_day", CatalogCategory.Enemy, "Kẻ Cắt Dây", "D", "GDD 19.1"),
            new CatalogEntry("pig.enemy.ke_bat_heo", CatalogCategory.Enemy, "Kẻ Bắt Heo", "D", "GDD 19.1"),
            new CatalogEntry("pig.enemy.ke_cao_pha", CatalogCategory.Enemy, "Kẻ Cào Phá", "D", "GDD 19.1"),
            new CatalogEntry("pig.enemy.bay_nhay", CatalogCategory.Enemy, "Bầy Nhảy", "D", "GDD 19.1"),
            new CatalogEntry("pig.enemy.thiet_giap_cong_thanh", CatalogCategory.Enemy, "Thiết Giáp Công Thành", "D", "GDD 19.1"),
            new CatalogEntry("pig.enemy.ufo_trinh_sat", CatalogCategory.Enemy, "UFO Trinh Sát", "D", "GDD 19.1"),
            new CatalogEntry("pig.enemy.ufo_thu_hoach", CatalogCategory.Enemy, "UFO Thu Hoạch", "D", "GDD 19.1"),
            new CatalogEntry("pig.enemy.ufo_phao_ham", CatalogCategory.Enemy, "UFO Pháo Hạm", "D", "GDD 19.1"),
            new CatalogEntry("pig.enemy.bong_huyen", CatalogCategory.Enemy, "Bóng Huyền", "D", "GDD 19.1"),
            new CatalogEntry("pig.enemy.cho_suong", CatalogCategory.Enemy, "Chó Sương", "D", "GDD 19.1"),
            new CatalogEntry("pig.enemy.thu_bao", CatalogCategory.Enemy, "Thú Bão", "D", "GDD 19.1"),
            new CatalogEntry("pig.enemy.dau_linh_trom", CatalogCategory.Enemy, "Đầu Lĩnh Trộm", "D", "GDD 19.1"),
            new CatalogEntry("pig.enemy.ke_mang_dich", CatalogCategory.Enemy, "Kẻ Mang Dịch", "D", "GDD 19.1"),

            // Cycle bosses (GDD 19.2)
            new CatalogEntry("pig.boss.chua_trom_riu_sat", CatalogCategory.Boss, "Chúa Trộm Rìu Sắt", "D", "GDD 19.2"),
            new CatalogEntry("pig.boss.mau_ham_ufo", CatalogCategory.Boss, "Mẫu Hạm UFO", "D", "GDD 19.2"),
            new CatalogEntry("pig.boss.thuc_the_suong", CatalogCategory.Boss, "Thực Thể Sương", "D", "GDD 19.2"),

            // Skills (GDD 12.7)
            new CatalogEntry("pig.skill.bo_phap_luot", CatalogCategory.Skill, "Bộ Pháp Lướt", "D", "GDD 12.7"),
            new CatalogEntry("pig.skill.doan_anh_tram", CatalogCategory.Skill, "Đoạn Ảnh Trảm", "D", "GDD 12.7"),
            new CatalogEntry("pig.skill.xuyen_bay", CatalogCategory.Skill, "Xuyên Bầy", "D", "GDD 12.7"),
            new CatalogEntry("pig.skill.cuu_nguy", CatalogCategory.Skill, "Cứu Nguy", "D", "GDD 12.7"),
            new CatalogEntry("pig.skill.tiep_vien_toc_hanh", CatalogCategory.Skill, "Tiếp Viện Tốc Hành", "D", "GDD 12.7"),
            new CatalogEntry("pig.skill.vo_anh_bo", CatalogCategory.Skill, "Vô Ảnh Bộ", "D", "GDD 12.7"),
            new CatalogEntry("pig.skill.pha_giap", CatalogCategory.Skill, "Phá Giáp", "D", "GDD 12.7"),
            new CatalogEntry("pig.skill.chan_dia", CatalogCategory.Skill, "Chấn Địa", "D", "GDD 12.7"),
            new CatalogEntry("pig.skill.tram_tuyen", CatalogCategory.Skill, "Trảm Tuyến", "D", "GDD 12.7"),
            new CatalogEntry("pig.skill.phan_chan", CatalogCategory.Skill, "Phản Chấn", "D", "GDD 12.7"),
            new CatalogEntry("pig.skill.neo_kich", CatalogCategory.Skill, "Neo Kích", "D", "GDD 12.7"),
            new CatalogEntry("pig.skill.thien_chuy", CatalogCategory.Skill, "Thiên Chùy", "D", "GDD 12.7"),
            new CatalogEntry("pig.skill.moc_keo", CatalogCategory.Skill, "Móc Kéo", "D", "GDD 12.7"),
            new CatalogEntry("pig.skill.an_tin_hieu", CatalogCategory.Skill, "Ấn Tín Hiệu", "D", "GDD 12.7"),
            new CatalogEntry("pig.skill.xoay_bay", CatalogCategory.Skill, "Xoay Bầy", "D", "GDD 12.7"),
            new CatalogEntry("pig.skill.troi_luoi", CatalogCategory.Skill, "Trói Lưới", "D", "GDD 12.7"),
            new CatalogEntry("pig.skill.dung_chot", CatalogCategory.Skill, "Dựng Chốt", "D", "GDD 12.7"),
            new CatalogEntry("pig.skill.thien_la", CatalogCategory.Skill, "Thiên La", "D", "GDD 12.7"),
            new CatalogEntry("pig.skill.tru_bo", CatalogCategory.Skill, "Trụ Bộ", "D", "GDD 12.7"),
            new CatalogEntry("pig.skill.ho_than", CatalogCategory.Skill, "Hộ Thân", "D", "GDD 12.7"),
            new CatalogEntry("pig.skill.khieu_chien", CatalogCategory.Skill, "Khiêu Chiến", "D", "GDD 12.7"),
            new CatalogEntry("pig.skill.hoi_khi", CatalogCategory.Skill, "Hồi Khí", "D", "GDD 12.7"),
            new CatalogEntry("pig.skill.chan_mang", CatalogCategory.Skill, "Chắn Mạng", "D", "GDD 12.7"),
            new CatalogEntry("pig.skill.kim_cuong_the", CatalogCategory.Skill, "Kim Cương Thể", "D", "GDD 12.7"),

            // Weather (GDD 7.2)
            new CatalogEntry("pig.weather.quang_dang", CatalogCategory.Weather, "Quang Đãng", "D", "GDD 7.2"),
            new CatalogEntry("pig.weather.nhieu_may", CatalogCategory.Weather, "Nhiều Mây", "D", "GDD 7.2"),
            new CatalogEntry("pig.weather.nang_gat", CatalogCategory.Weather, "Nắng Gắt", "D", "GDD 7.2"),
            new CatalogEntry("pig.weather.mua", CatalogCategory.Weather, "Mưa", "D", "GDD 7.2"),
            new CatalogEntry("pig.weather.giong_bao", CatalogCategory.Weather, "Giông Bão", "D", "GDD 7.2"),
            new CatalogEntry("pig.weather.suong_mu", CatalogCategory.Weather, "Sương Mù", "D", "GDD 7.2"),
            new CatalogEntry("pig.weather.ret_dam", CatalogCategory.Weather, "Rét Đậm", "D", "GDD 7.2"),
            new CatalogEntry("pig.weather.nom_am", CatalogCategory.Weather, "Nồm Ẩm", "D", "GDD 7.2"),
            new CatalogEntry("pig.weather.gio_kho", CatalogCategory.Weather, "Gió Khô", "D", "GDD 7.2"),
            new CatalogEntry("pig.weather.mua_buc_xa", CatalogCategory.Weather, "Mưa Bức Xạ", "D", "GDD 7.2"),

            // Health threats (GDD 6.4)
            new CatalogEntry("pig.health_threat.ho_hap_lanh", CatalogCategory.HealthThreat, "Hô Hấp Lạnh", "D", "GDD 6.4"),
            new CatalogEntry("pig.health_threat.ghe_ky_sinh", CatalogCategory.HealthThreat, "Ghẻ Ký Sinh", "D", "GDD 6.4"),
            new CatalogEntry("pig.health_threat.dich_ta_heo", CatalogCategory.HealthThreat, "Dịch Tả Heo", "D", "GDD 6.4"),
            new CatalogEntry("pig.health_threat.sot_bun", CatalogCategory.HealthThreat, "Sốt Bùn", "D", "GDD 6.4"),
            new CatalogEntry("pig.health_threat.chuong_nhiem", CatalogCategory.HealthThreat, "Chuồng Nhiễm", "D", "GDD 6.4"),
            new CatalogEntry("pig.health_threat.say_nang", CatalogCategory.HealthThreat, "Say Nắng", "D", "GDD 6.4"),
            new CatalogEntry("pig.health_threat.roi_loan_sinh_san", CatalogCategory.HealthThreat, "Rối Loạn Sinh Sản", "D", "GDD 6.4"),
            new CatalogEntry("pig.health_threat.nam_phat_quang", CatalogCategory.HealthThreat, "Nấm Phát Quang", "D", "GDD 6.4"),
            new CatalogEntry("pig.health_threat.loi_tam", CatalogCategory.HealthThreat, "Lôi Tâm", "D", "GDD 6.4"),
            new CatalogEntry("pig.health_threat.hu_mach", CatalogCategory.HealthThreat, "Hư Mạch", "D", "GDD 6.4"),

            // Events (GDD 24.1)
            new CatalogEntry("pig.event.vuon_muoi_hai_qua", CatalogCategory.Event, "Vườn Mười Hai Quả", "D", "GDD 24.1"),
            new CatalogEntry("pig.event.dong_co_khong_bong", CatalogCategory.Event, "Đồng Cỏ Không Bóng", "D", "GDD 24.1"),
            new CatalogEntry("pig.event.kho_lanh_bo_quen", CatalogCategory.Event, "Kho Lạnh Bỏ Quên", "D", "GDD 24.1"),
            new CatalogEntry("pig.event.hat_giong_trang", CatalogCategory.Event, "Hạt Giống Trắng", "D", "GDD 24.1"),
            new CatalogEntry("pig.event.bi_nhan_mua_gia", CatalogCategory.Event, "Bí Nhân Mua Già", "D", "GDD 24.1"),
            new CatalogEntry("pig.event.cho_khong_dong_ho", CatalogCategory.Event, "Chợ Không Đồng Hồ", "D", "GDD 24.1"),
            new CatalogEntry("pig.event.giam_dinh_vo_danh", CatalogCategory.Event, "Giám Định Vô Danh", "D", "GDD 24.1"),
            new CatalogEntry("pig.event.doi_mot_lay_mot", CatalogCategory.Event, "Đổi Một Lấy Một", "D", "GDD 24.1"),
            new CatalogEntry("pig.event.thu_vien_muc_an", CatalogCategory.Event, "Thư Viện Mực Ấn", "D", "GDD 24.1"),
            new CatalogEntry("pig.event.dot_san_trong_suong", CatalogCategory.Event, "Đợt Săn Trong Sương", "D", "GDD 24.1"),
            new CatalogEntry("pig.event.can_quet_ufo", CatalogCategory.Event, "Càn Quét UFO", "D", "GDD 24.1"),
            new CatalogEntry("pig.event.giu_cong_nguoc", CatalogCategory.Event, "Giữ Cổng Ngược", "D", "GDD 24.1"),
            new CatalogEntry("pig.event.ba_ngay_khoa", CatalogCategory.Event, "Ba Ngày Khóa", "D", "GDD 24.1"),
            new CatalogEntry("pig.event.lo_mo_hu_khong", CatalogCategory.Event, "Lò Mổ Hư Không", "D", "GDD 24.1")
        };

        public static IReadOnlyList<CatalogEntry> Entries => EntriesInternal;

        public static List<string> Validate()
        {
            var errors = new List<string>();
            var ids = new HashSet<string>(StringComparer.Ordinal);
            var counts = new Dictionary<CatalogCategory, int>();

            foreach (CatalogCategory category in Enum.GetValues(typeof(CatalogCategory)))
            {
                counts[category] = 0;
            }

            foreach (CatalogEntry entry in EntriesInternal)
            {
                if (entry == null)
                {
                    errors.Add("Catalog có bản ghi null.");
                    continue;
                }

                if (string.IsNullOrWhiteSpace(entry.Id))
                {
                    errors.Add("Có bản ghi thiếu ID.");
                }
                else
                {
                    if (!Regex.IsMatch(
                            entry.Id,
                            @"^[a-z0-9]+\.[a-z0-9]+\.[a-z0-9_]+$"))
                    {
                        errors.Add("ID sai định dạng: " + entry.Id);
                    }

                    if (!ids.Add(entry.Id))
                    {
                        errors.Add("ID bị trùng: " + entry.Id);
                    }
                }

                if (string.IsNullOrWhiteSpace(entry.Name))
                {
                    errors.Add("Có bản ghi thiếu Name: " + entry.Id);
                }

                if (entry.Owner != "A" &&
                    entry.Owner != "B" &&
                    entry.Owner != "C" &&
                    entry.Owner != "D")
                {
                    errors.Add("Owner không hợp lệ ở bản ghi: " + entry.Id);
                }

                if (string.IsNullOrWhiteSpace(entry.SourceGdd))
                {
                    errors.Add("Thiếu mục GDD nguồn ở bản ghi: " + entry.Id);
                }

                if (counts.ContainsKey(entry.Category))
                {
                    counts[entry.Category]++;
                }
                else
                {
                    errors.Add("Category không hợp lệ ở bản ghi: " + entry.Id);
                }
            }

            foreach (KeyValuePair<CatalogCategory, int> expected in ExpectedCounts)
            {
                int actual = counts[expected.Key];
                if (actual != expected.Value)
                {
                    errors.Add(
                        expected.Key + " cần " + expected.Value +
                        " mục nhưng hiện có " + actual + ".");
                }
            }

            return errors;
        }
    }
}