using System;
using System.Collections.Generic;
using System.Linq;

namespace PigTycoon.Core
{
    /// <summary>
    /// The prologue names ~100 detailed backdrops. Art comes in two passes: first the 22 base plates below, then detailed
    /// variants. The view shows the exact id when its picture exists, otherwise <see cref="Base"/> of it, so every scene
    /// has a picture from the first pass on.
    /// </summary>
    public static class NarrativeBackdrops
    {
        /// <summary>First art pass, in priority order (the opening minutes first).</summary>
        public static readonly string[] BaseSet =
        {
            "bg_ngay_mat",            // static card for death dates and chapter cards (dark, still)
            "bg_nha_cu_phong_khach",  // old house: living room / family meal
            "bg_nha_cu_bep_2018",     // old house: kitchen
            "bg_nha_cu_cau_thang",    // old house: staircase + landing window sill with the bottle
            "bg_nha_cu_hien",         // old house: porch / gate / back well
            "bg_hem_nha_cu",          // the alley, small shops
            "bg_chua",                // temple yard with frangipani
            "bg_goc_pho",             // city street corner / crossroads
            "bg_so_tay",              // close-up of paper: notebook pages, letters, certificates, files
            "bg_phong_ngu",           // childhood bedroom
            "bg_benh_vien",           // hospital room / corridor
            "bg_phong_tro",           // rented room
            "bg_phong_tro_guong",     // bathroom mirror
            "bg_nghia_trang",         // cemetery
            "bg_tranh_gia_dinh_em",   // the child's drawing "Gia đình em"
            "bg_hop_thiec",           // close-up: tin box and its objects (leather piece, necklace, photo)
            "bg_xe_buyt",             // coach interior / road through the window
            "bg_bien_bo_ria",         // the "Rìa" sign where the coach stops
            "bg_trai_chang_vang",     // the farm field at dusk
            "bg_cong_trai",           // farm gate
            "bg_phong_bep_lo",        // the stove room at the farm
            "bg_hoc_tuong_bay_cot",   // the wall niche with seven height columns
        };

        // Longest matching prefix wins. Ids already in BaseSet map to themselves.
        static readonly (string prefix, string baseId)[] Rules =
        {
            ("bg_nen_tinh_ngay_mat", "bg_ngay_mat"), ("bg_nen_den", "bg_ngay_mat"), ("bg_the_chuong", "bg_ngay_mat"),
            ("bg_nha_cu_bep", "bg_nha_cu_bep_2018"), ("bg_bep_me", "bg_nha_cu_bep_2018"),
            ("bg_nha_cu_nha_tam", "bg_phong_tro_guong"), ("bg_guong", "bg_phong_tro_guong"), ("bg_phong_tro_guong", "bg_phong_tro_guong"),
            ("bg_nha_cu_cau_thang", "bg_nha_cu_cau_thang"), ("bg_cau_thang", "bg_nha_cu_cau_thang"), ("bg_bau_cua", "bg_nha_cu_cau_thang"),
            ("bg_nha_cu_hien", "bg_nha_cu_hien"), ("bg_hien_nha", "bg_nha_cu_hien"), ("bg_nha_cu_cong", "bg_nha_cu_hien"), ("bg_gieng", "bg_nha_cu_hien"),
            ("bg_nha_cu", "bg_nha_cu_phong_khach"),
            ("bg_hem", "bg_hem_nha_cu"), ("bg_tiem", "bg_hem_nha_cu"),
            ("bg_chua", "bg_chua"), ("bg_san_chua", "bg_chua"), ("bg_bac_da_chua", "bg_chua"),
            ("bg_goc_pho", "bg_goc_pho"), ("bg_nga_tu", "bg_goc_pho"), ("bg_tru_so", "bg_goc_pho"),
            ("bg_so_sau_ngay_mat", "bg_nghia_trang"), ("bg_phieu_thu_suat_mo", "bg_nghia_trang"), ("bg_nghia_trang", "bg_nghia_trang"),
            ("bg_so_bia_vai", "bg_hop_thiec"), ("bg_so_", "bg_so_tay"), ("bg_bien_ban", "bg_so_tay"), ("bg_vi_giay", "bg_so_tay"),
            ("bg_la_thu", "bg_so_tay"), ("bg_ho_so_ban_giao", "bg_so_tay"), ("bg_giay_bao", "bg_so_tay"),
            ("bg_phong_ngu", "bg_phong_ngu"),
            ("bg_benh_vien", "bg_benh_vien"), ("bg_phong_benh", "bg_benh_vien"), ("bg_ho_so_benh_an", "bg_benh_vien"), ("bg_hanh_lang", "bg_benh_vien"),
            ("bg_phong_tro", "bg_phong_tro"), ("bg_van_phong", "bg_phong_tro"),
            ("bg_tranh_gia_dinh_em", "bg_tranh_gia_dinh_em"),
            ("bg_hop_thiec", "bg_hop_thiec"), ("bg_manh_da", "bg_hop_thiec"), ("bg_vong_co", "bg_hop_thiec"), ("bg_anh_", "bg_hop_thiec"),
            ("bg_co_tay", "bg_hop_thiec"), ("bg_tui_ao", "bg_hop_thiec"),
            ("bg_xe_", "bg_xe_buyt"), ("bg_duong_", "bg_xe_buyt"),
            ("bg_bien_bo_ria", "bg_bien_bo_ria"),
            ("bg_trai_cong", "bg_cong_trai"), ("bg_cong_trai", "bg_cong_trai"),
            ("bg_dong_", "bg_trai_chang_vang"), ("bg_trai_", "bg_trai_chang_vang"),
            ("bg_phong_canh_bep_lo", "bg_phong_bep_lo"), ("bg_phong_bep_lo", "bg_phong_bep_lo"), ("bg_thanh_lo", "bg_phong_bep_lo"),
            ("bg_hoc_tuong", "bg_hoc_tuong_bay_cot"),
            // Chapters 2–4 detail plates.
            ("bg_bo_dem", "bg_phong_bep_lo"), ("bg_kho_trai", "bg_phong_bep_lo"), ("bg_kho_ong_ton", "bg_so_tay"),
            ("bg_hop_ky_thuat", "bg_so_tay"), ("bg_hop_nhat_ky", "bg_so_tay"), ("bg_giay_chung_nhan", "bg_so_tay"),
            ("bg_quan_ba_sau", "bg_hem_nha_cu"), ("bg_quan_ba_sau_bep", "bg_nha_cu_phong_khach"),
            ("bg_kinh_cua_so", "bg_phong_tro_guong"), ("bg_noi_tru", "bg_phong_tro"), ("bg_vong_ban_an", "bg_hop_thiec"),
        };

        static readonly HashSet<string> baseSet = new HashSet<string>(BaseSet, StringComparer.Ordinal);

        /// <summary>The first-pass plate to show while <paramref name="id"/> has no picture of its own ("" stays "").</summary>
        public static string Base(string id)
        {
            if (string.IsNullOrEmpty(id) || baseSet.Contains(id)) return id ?? "";
            var hit = Rules.Where(r => id.StartsWith(r.prefix, StringComparison.Ordinal)).OrderByDescending(r => r.prefix.Length).FirstOrDefault();
            return hit.baseId ?? "";
        }

        /// <summary>Every backdrop id the authored scenes use.</summary>
        public static IEnumerable<string> UsedIds() =>
            NarrativeLibrary.All().SelectMany(s => s.Commands).Where(c => c.Kind == NarrativeCmd.Backdrop && !string.IsNullOrEmpty(c.Label))
                .Select(c => c.Label).Distinct().OrderBy(x => x, StringComparer.Ordinal);
    }
}
