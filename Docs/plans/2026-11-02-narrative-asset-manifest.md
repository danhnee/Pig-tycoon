# Narrative asset manifest (generated from Core content, 2 Nov 2026)

Scenes: 178. Regenerate: `dotnet run --project tools/narrative/manifest -- Docs/plans/2026-11-02-narrative-asset-manifest.md`.

## 1. Portraits — `UI/Portraits/<cast>_<portrait>.png` (missing → neutral)

- **an** (An): happy, laugh, neutral _(chưa dùng)_, sad, silent_tilt, stare, surprised, thinking, too_bright _(chưa dùng)_, worried
- **anh_ca** (Anh Ca): happy, neutral, thinking, worried
- **anh_tu** (Anh Tư): happy, neutral
- **ba_sau** (Bà Sáu): happy, neutral, sad, worried
- **bi_nhan** (Bí nhân): neutral
- **chot** (Chốt): happy, neutral
- **cuc** (Cúc): happy, neutral, surprised _(chưa dùng)_, thinking, worried
- **do** (Độ): happy, neutral
- **dung** (Bác sĩ Dung): angry, neutral, sad _(chưa dùng)_, thinking
- **khoa** (Khoa): neutral, reflect_cry _(chưa dùng)_, reflect_frown _(chưa dùng)_, reflect_smile _(chưa dùng)_
- **le** (Lệ): angry, happy, neutral, thinking
- **man** (Mận): happy, neutral, sad, surprised, thinking
- **mien** (Miên): neutral, thinking
- **ong_ton** (Ông Tồn): neutral, sad, surprised, thinking
- **tam_va** (Tám Vá): angry, happy, neutral, surprised _(chưa dùng)_, thinking
- **tu_troi** (Tư Trời): neutral, thinking
- **van** (Vân): angry, neutral

## 2. Backdrops — pass 1 = 22 base plates (priority order); pass 2 = detailed variants (fallback to base)

- **bg_ngay_mat**: bg_nen_den, bg_nen_tinh_ngay_mat, bg_the_chuong_0
- **bg_nha_cu_phong_khach**: bg_nha_cu_ban_lam_viec_cha, bg_nha_cu_bua_com, bg_nha_cu_bua_com_ghe_trong, bg_nha_cu_bua_com_tu_cua_bep, bg_nha_cu_cua_chinh, bg_nha_cu_hu_gao, bg_nha_cu_khung_cua_vach_do, bg_nha_cu_phong_khach_bui, bg_nha_cu_phong_khach_chieu, bg_nha_cu_phong_minh_cai_quat, bg_nha_cu_tu_tho_ve_so, bg_nha_cu_tuong_cham_sang
- **bg_nha_cu_bep_2018**: bg_bep_me_gia_bat
- **bg_nha_cu_cau_thang**: bg_bau_cua_so, bg_bau_cua_vong_tron_sach, bg_cau_thang_chieu_nghi_hoa_tan, bg_cau_thang_chieu_nghi_ngay, bg_cau_thang_chieu_nghi_toi, bg_cau_thang_hoi_nho, bg_cau_thang_muoi_bon_bac
- **bg_nha_cu_hien**: bg_gieng_sau_nha_kien, bg_hien_nha_nen_cup_dien, bg_nha_cu_cong_mua_bong_may
- **bg_hem_nha_cu**: bg_hem_hoa_phuong_rung, bg_hem_nha_thuoc_sap_xoi, bg_tiem_tap_hoa
- **bg_chua**: bg_bac_da_chua, bg_san_chua_hoa_su
- **bg_goc_pho**: bg_goc_pho_cuoi, bg_goc_pho_sang_som, bg_nga_tu_den_do, bg_nga_tu_nguoi_ao_mua, bg_nga_tu_sang, bg_tru_so_cong_an_phuong
- **bg_so_tay**: bg_bien_ban_giay, bg_giay_bao_hoc_bong, bg_ho_so_ban_giao_trai, bg_la_thu_gui_quan_ba_sau, bg_so_hai_cot, bg_so_trang_dau_nam_dong, bg_vi_giay_chung_nhan
- **bg_phong_ngu**: bg_phong_ngu_an_cu, bg_phong_ngu_khoa_toi
- **bg_benh_vien**: bg_hanh_lang_ban_nga, bg_ho_so_benh_an, bg_phong_benh_giac_mo, bg_phong_benh_tinh_day
- **bg_phong_tro**: bg_phong_tro_cua_so_sang, bg_van_phong_thuong_nong
- **bg_phong_tro_guong**: bg_guong_nha_tam_mo, bg_nha_cu_nha_tam, bg_phong_tro_guong_nha_tam
- **bg_nghia_trang**: bg_nghia_trang_cong_cay_su, bg_phieu_thu_suat_mo, bg_so_sau_ngay_mat
- **bg_tranh_gia_dinh_em**: bg_tranh_gia_dinh_em_ban_goc, bg_tranh_gia_dinh_em_dong_khung
- **bg_hop_thiec**: bg_anh_ca_nha_o_trai, bg_anh_tet_2018_cham_sang, bg_co_tay_vet_bam, bg_hop_thiec_khoa_sao, bg_hop_thiec_mo, bg_manh_da_dau_nem, bg_manh_da_dung_dem, bg_manh_da_mat_sau, bg_so_bia_vai_bay_phan, bg_so_bia_vai_trang_cuoi_cha, bg_tui_ao_hat_dau_den, bg_vong_co_cua_me
- **bg_xe_buyt**: bg_duong_cau_sat, bg_duong_nga_ba_cay_gao, bg_duong_ranh_nuoc_chay_nguoc, bg_xe_khach_cua_so_ruong, bg_xe_khach_hang_ghe_cuoi
- **bg_bien_bo_ria**: bg_bien_bo_ria_xe_khach
- **bg_trai_chang_vang**: bg_dong_sau_rao, bg_dong_vang_chang_vang, bg_trai_mang_heo_gia_chet
- **bg_cong_trai**: bg_trai_cong_ban_ngay_xe_om, bg_trai_cong_then_xe
- **bg_phong_bep_lo**: bg_phong_canh_bep_lo_chieu, bg_phong_canh_bep_lo_dem_den_dau, bg_phong_canh_bep_lo_sang, bg_phong_canh_bep_lo_toi, bg_thanh_lo_vet_nang
- **bg_hoc_tuong_bay_cot**

## 3. Music ids ("" = silence) (15)

`bgm_bai_hat_me_sai_loi`, `bgm_bo_ria_chang_vang`, `bgm_bo_ria_lang`, `bgm_duong_len_trai`, `bgm_gio_nhe`, `bgm_lang_bo_ria`, `bgm_nha_cu_am`, `bgm_nha_cu_bui`, `bgm_nha_cu_buon`, `bgm_nha_cu_lang`, `bgm_nha_cu_ngay_xua`, `bgm_phong_tro`, `bgm_thanh_pho_sang`, `bgm_trai_cu_chieu`, `bgm_trai_cu_sang`

## 4. Sfx ids (94)

`sfx_anh_sang_tranh`, `sfx_bat_dua`, `sfx_bat_up`, `sfx_buoc_chan_voi`, `sfx_buoc_xuong_doc`, `sfx_cao_muoi`, `sfx_cau_sat`, `sfx_cau_thang_go_cot_ket`, `sfx_chen_dua`, `sfx_choi_quet_la`, `sfx_choi_tre`, `sfx_chuong_chua_xa`, `sfx_chuong_gio`, `sfx_chuong_gio_mot_tieng`, `sfx_co_le`, `sfx_co_le_leng_keng`, `sfx_coi_xe`, `sfx_coi_xe_khach`, `sfx_coi_xe_may`, `sfx_cua_khep`, `sfx_cuoi_vong`, `sfx_cup_dien`, `sfx_danh_rang`, `sfx_den_qua_duong`, `sfx_dep_bac_da`, `sfx_dep_keo_dat`, `sfx_dong_ho_deo_tay_tich_tac`, `sfx_dong_ho_tich`, `sfx_gach_truot`, `sfx_gau_gieng`, `sfx_gio_dong`, `sfx_gio_lon_dem`, `sfx_giot_nuoc`, `sfx_giot_nuoc_bon_rua`, `sfx_go_ba_cai`, `sfx_go_bac_dac`, `sfx_go_bac_rong`, `sfx_go_dac`, `sfx_go_rong`, `sfx_hat_dau`, `sfx_im_lang_tuyet_doi`, `sfx_ket`, `sfx_ket_cung`, `sfx_khoa_tach`, `sfx_khung_cua_cot`, `sfx_kim_loai_lach_cach`, `sfx_kim_loai_roi_san`, `sfx_ky_ten`, `sfx_lat_so`, `sfx_luc_cuc`, `sfx_may_do_bao_dong`, `sfx_may_do_nhip_tim`, `sfx_may_do_nhip_tim_bao`, `sfx_may_do_nhip_tim_gap`, `sfx_mua`, `sfx_mua_bong_may`, `sfx_mua_mai_ton`, `sfx_mua_to`, `sfx_mua_tren_mai`, `sfx_ngan_nga`, `sfx_nuoc_rach`, `sfx_nuoc_rua_chen_ngung`, `sfx_o_khoa_tron`, `sfx_phanh_xa`, `sfx_quat_cu_quay`, `sfx_quat_cu_quay_mot_vong`, `sfx_quat_xoay`, `sfx_rua_bat`, `sfx_tach`, `sfx_tach_bon`, `sfx_tach_hai`, `sfx_tach_nguoc`, `sfx_tach_nha`, `sfx_then_cong_xe`, `sfx_tich_tac_nhanh`, `sfx_tieng_vong`, `sfx_tieng_vong_phang`, `sfx_tim_dap`, `sfx_trong_truong`, `sfx_truot_em`, `sfx_tu_go_ket`, `sfx_tui_roi`, `sfx_va_cham_tui_roi`, `sfx_va_lo`, `sfx_vai_dap_lo_gach`, `sfx_van_go_nhac`, `sfx_vo_tay`, `sfx_xe_khach_di_xa`, `sfx_xe_may_di_xa`, `sfx_xe_may_hem`, `sfx_xe_may_no_may`, `sfx_xe_to_lich`, `sfx_xich_tach`, `sfx_xo_cam`

## 5. Emotes (2)

`...`, `♪`

## 6. Custom effects (reflection:/notebook:/tutorial:/goto_map:) (40)

`goto_map:Farm`, `notebook:ba_oi_cay_dinh`, `notebook:ba_oi_con_chep_ngay`, `notebook:le_hop_chua_tung_mo`, `notebook:me_oi_con_van_cuoi_duoc`, `notebook:me_oi_guong_noi_cham`, `notebook:nb_04_hoc_tuong_bay_cot`, `notebook:nha_minh_oi_bay_cot`, `notebook:nha_minh_oi_cai_chai`, `notebook:nha_minh_oi_phai_o`, `notebook:nha_minh_oi_xom_dem_cho`, `notebook:nhung_thu_tren_bau_cua_so`, `notebook:ong_oi_dung_duong_ong_ve`, `notebook:p02_nhung_thu_tren_bau_cua_so`, `notebook:p04_vong_tron_sach`, `notebook:p05_bay_buoi_sang_truoc_guong`, `notebook:p06_hop_thiec_khoa_sao`, `notebook:p06_phieu_thu_suat_mo`, `notebook:p08_ban_chep_dau_nem_ong_ton`, `notebook:p08_la_thu_21_09_2026`, `notebook:p08_ranh_khac_lanh_hon`, `notebook:p08_trang_cuoi_cua_cha`, `notebook:phieu_thu_suat_mo`, `notebook:Q01`, `notebook:Q02`, `notebook:Q03`, `notebook:Q04`, `notebook:Q05`, `notebook:trang_cuoi_cua_cha`, `reflection:neutral`, `tutorial:counter`, `tutorial:dodge`, `tutorial:feed`, `tutorial:interact`, `tutorial:market`, `tutorial:move`, `tutorial:observe`, `tutorial:recall`, `tutorial:water`, `tutorial:whistle`

## 7. Stage marks used (13)

`farm:spawn`, `farm:station:capacity_board`, `farm:station:herd_shelter`, `farm:station:main_house`, `farm:station:warehouse`, `village:eatery`, `village:gate`, `village:npc:anh_tu`, `village:npc:ong_ton`, `village:spawn`, `village:station:nha_van_hoa`, `village:station:quan_ba_sau`, `village:well`
