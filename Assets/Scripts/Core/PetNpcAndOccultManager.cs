using System;
using System.Collections.Generic;

namespace PigTycoon.Core
{
    public enum HealthTreatmentVerb
    {
        CachLy,             // 1. Cách ly sang Khu Cách Ly
        XuLyMoiTruong,      // 2. Dọn chuồng, khử trùng, thay ổ rơm, thoát nước
        DieuTriCaThe,       // 3. Dùng thuốc (Hạ nhiệt, Trị ghẻ, An thần) cho từng con
        PhongNguaDan,       // 4. Tiêm Vắc-xin / bổ sung khoáng cho đàn khỏe
        NghienCuu           // 5. Nghiên cứu mẫu bệnh mở khóa kháng thể / phác đồ Nhóm D
    }

    public enum PetId
    {
        ChoChanDan,     // Chó Chăn Đàn (B): +20% tốc độ lùa đàn, tự giữ heo con lạc
        MeoKho,         // Mèo Kho: -80% thất thoát cám do Chuột Gặm Kho
        HeoSanNam,      // Heo Săn Nấm: đào thảo dược & nấm quý mỗi ngày
        QuaTin,         // Quạ Tin: báo trước thương nhân / sự kiện 1 buổi
        ChoNgaoCanh,    // Chó Ngao Canh (B): chặn 1 kẻ trộm/đêm & sủa cảnh báo giảm hoảng
        NgongBaoDong,   // Ngỗng Báo Động: +4s cảnh báo sớm đợt quái
        ChoSanChien     // Chó Săn Chiến: cắn giữ mục tiêu bị đánh dấu
    }

    public enum NpcSpecialistId
    {
        QuanLyCa,
        NhaNghienCuuGen,
        BacSiThuY,          // NPC của B: tự động khám & chữa 2 heo/ngày, +15% hiệu quả thuốc
        KySuXayDung,
        NhaBuon,
        ThuKho,
        NongCong,           // NPC của B: tự động cho ăn & dọn vệ sinh 2 Ô Đất/ngày (+8 SC)
        VeBinhKhien,        // NPC của B: chốt chặn cổng chính, giảm 40% sát thương nhận
        XaThuNo,
        KySuChienTruong,
        ThoSanUfo,
        ThichKhachDem
    }

    public enum MemberBOccultEventId
    {
        VuonMuoiHaiQua,     // Vườn Mười Hai Quả (GDD 24.1 - B)
        DongCoKhongBong,    // Đồng Cỏ Không Bóng (GDD 24.1 - B)
        BiNhanMuaGia,       // Bí Nhân Mua Già (GDD 24.1 - B)
        DoiMotLayMot        // Đổi Một Lấy Một (GDD 24.1 - B)
    }

    [Serializable]
    public sealed class ActivePetSlot
    {
        public PetId Id;
        public string Name = "";
        public bool IsSupportCategory;
        public int StaminaOrSatiety = 100;
    }

    [Serializable]
    public sealed class HiredNpcRecord
    {
        public NpcSpecialistId Id;
        public string Name = "";
        public int DailyWageGold;
        public float AssignedWorldX;
        public float AssignedWorldY;
        public bool IsActive = true;
    }

    /// <summary>
    /// Quản lý 5 động từ xử lý bệnh (GDD 6.5), Framework 7 Thú cưng (GDD 11.3, tối đa 2 con đi kèm),
    /// 3 NPC chủ lực của B (Bác Sĩ Thú Y, Nông Công, Vệ Binh Khiên), và 4 Sự kiện Huyền Bí của B (GDD 24.1, P4.12).
    /// </summary>
    public sealed class PetNpcAndOccultManager
    {
        public const int MaxActivePets = 2;

        public List<ActivePetSlot> ActivePets { get; } = new List<ActivePetSlot>();
        public List<HiredNpcRecord> HiredNpcs { get; } = new List<HiredNpcRecord>();
        public HashSet<DiseaseId> ResearchedDiseases { get; } = new HashSet<DiseaseId>();

        public bool TryEquipPet(PetId petId)
        {
            if (ActivePets.Exists(p => p.Id == petId))
            {
                return true;
            }

            if (ActivePets.Count >= MaxActivePets)
            {
                return false;
            }

            ActivePets.Add(new ActivePetSlot
            {
                Id = petId,
                Name = GetPetDisplayName(petId),
                IsSupportCategory = petId == PetId.ChoChanDan || petId == PetId.MeoKho || petId == PetId.HeoSanNam || petId == PetId.QuaTin,
                StaminaOrSatiety = 100
            });
            return true;
        }

        public bool UnequipPet(PetId petId)
        {
            return ActivePets.RemoveAll(p => p.Id == petId) > 0;
        }

        public bool HasPet(PetId petId)
        {
            return ActivePets.Exists(p => p.Id == petId);
        }

        public static string GetPetDisplayName(PetId petId)
        {
            return petId switch
            {
                PetId.ChoChanDan => "Chó Chăn Đàn",
                PetId.MeoKho => "Mèo Kho",
                PetId.HeoSanNam => "Heo Săn Nấm",
                PetId.QuaTin => "Quạ Tin",
                PetId.ChoNgaoCanh => "Chó Ngao Canh",
                PetId.NgongBaoDong => "Ngỗng Báo Động",
                PetId.ChoSanChien => "Chó Săn Chiến",
                _ => petId.ToString()
            };
        }

        public (float recallSpeedMultiplier, float ratLossMultiplier, int dailyFreeHerbs, bool blocksFirstNightThief, float extraRaidWarningSeconds) GetActivePetBonuses()
        {
            float recallMult = HasPet(PetId.ChoChanDan) ? 1.20f : 1.0f;
            float ratMult = HasPet(PetId.MeoKho) ? 0.20f : 1.0f; // Giảm 80% mất cám
            int herbs = HasPet(PetId.HeoSanNam) ? 2 : 0;
            bool blocksThief = HasPet(PetId.ChoNgaoCanh);
            float warnSec = HasPet(PetId.NgongBaoDong) ? 4.0f : 0f;
            return (recallMult, ratMult, herbs, blocksThief, warnSec);
        }

        public bool HireNpc(NpcSpecialistId id, float worldX = 0f, float worldY = 0f)
        {
            if (HiredNpcs.Exists(n => n.Id == id)) return false;

            int wage = id switch
            {
                NpcSpecialistId.QuanLyCa => 60,
                NpcSpecialistId.NhaNghienCuuGen => 70,
                NpcSpecialistId.BacSiThuY => 65,
                NpcSpecialistId.KySuXayDung => 65,
                NpcSpecialistId.NhaBuon => 55,
                NpcSpecialistId.ThuKho => 50,
                NpcSpecialistId.NongCong => 40,
                NpcSpecialistId.VeBinhKhien => 75,
                NpcSpecialistId.XaThuNo => 70,
                NpcSpecialistId.KySuChienTruong => 80,
                NpcSpecialistId.ThoSanUfo => 95,
                NpcSpecialistId.ThichKhachDem => 85,
                _ => 55
            };

            HiredNpcs.Add(new HiredNpcRecord
            {
                Id = id,
                Name = GetNpcCanonicalTownName(id),
                DailyWageGold = wage,
                AssignedWorldX = worldX,
                AssignedWorldY = worldY,
                IsActive = true
            });
            return true;
        }

        /// <summary>
        /// Thực thi ca làm việc mỗi ngày của NPC (Bác Sĩ Thú Y, Nông Công, Vệ Binh Khiên, v.v.).
        /// </summary>
        public (int curedPigs, int habitatGained, float gateDefenseBonus) ExecuteDailyNpcShift(IList<HerdPig> pigs, Farm farm)
        {
            int cured = 0;
            int scBonus = 0;
            float gateDef = 0f;

            for (int i = 0; i < HiredNpcs.Count; i++)
            {
                var npc = HiredNpcs[i];
                if (!npc.IsActive) continue;

                if (npc.Id == NpcSpecialistId.NongCong)
                {
                    scBonus += 8; // Dọn 2 Ô Đất (+4 SC mỗi lần dọn)
                    farm.HabitatIndex = Math.Min(100, farm.HabitatIndex + 8);
                }
                else if (npc.Id == NpcSpecialistId.BacSiThuY && pigs != null)
                {
                    int quota = 2;
                    for (int p = 0; p < pigs.Count && quota > 0; p++)
                    {
                        var pig = pigs[p];
                        if (pig != null && pig.Alive && pig.Disease.HasValue && pig.Disease.Value != DiseaseId.HuMach)
                        {
                            pig.Disease = null;
                            pig.Health = Math.Min(100, pig.Health + 20);
                            cured++;
                            quota--;
                        }
                    }
                }
                else if (npc.Id == NpcSpecialistId.VeBinhKhien)
                {
                    gateDef += 0.40f;
                }
                else if (npc.Id == NpcSpecialistId.KySuXayDung)
                {
                    gateDef += 0.15f;
                }
                else if (npc.Id == NpcSpecialistId.ThuKho)
                {
                    farm.FeedStockKg += 5f;
                }
            }

            return (cured, scBonus, gateDef);
        }

        /// <summary>
        /// Thực thi ca làm việc mỗi ngày của 12 NPC (7 nông trại + 5 chiến đấu) và 7 thú cưng trực tiếp trên đàn Pig chính của GameEngine.
        /// </summary>
        public (int curedPigs, int habitatGained, float gateDefenseBonus) ExecuteDailyNpcShiftOnPigs(List<Pig> pigs, Farm farm)
        {
            int cured = 0;
            int scBonus = 0;
            float gateDef = 0f;

            if (HasPet(PetId.HeoSanNam))
            {
                farm.MedicalHerbCount += 2;
            }

            bool hasShiftManager = HiredNpcs.Exists(n => n.IsActive && n.Id == NpcSpecialistId.QuanLyCa);

            for (int i = 0; i < HiredNpcs.Count; i++)
            {
                var npc = HiredNpcs[i];
                if (!npc.IsActive) continue;

                int effectiveWage = hasShiftManager && npc.Id != NpcSpecialistId.QuanLyCa
                    ? (int)Math.Round(npc.DailyWageGold * 0.90f) // Quản Lý Ca (Bảy Ca) tối ưu -10% chi phí ca trực
                    : npc.DailyWageGold;

                if (effectiveWage > 0 && farm.Gold >= effectiveWage)
                {
                    farm.Gold -= effectiveWage;
                }

                if (npc.Id == NpcSpecialistId.NongCong)
                {
                    scBonus += 8;
                    farm.HabitatIndex = Math.Min(100, farm.HabitatIndex + 8);
                    if (farm.FeedStockKg < 50f)
                    {
                        farm.FeedStockKg += 15f;
                    }
                }
                else if (npc.Id == NpcSpecialistId.BacSiThuY && pigs != null)
                {
                    int quota = hasShiftManager ? 3 : 2;
                    for (int p = 0; p < pigs.Count && quota > 0; p++)
                    {
                        var pig = pigs[p];
                        if (pig != null && pig.IsAlive && pig.ActiveDisease.HasValue && pig.ActiveDisease.Value != DiseaseId.HuMach)
                        {
                            pig.ActiveDisease = null;
                            pig.SeizurePending = false;
                            pig.Health = Math.Min(100, pig.Health + 20);
                            cured++;
                            quota--;
                        }
                    }
                }
                else if (npc.Id == NpcSpecialistId.NhaNghienCuuGen && pigs != null)
                {
                    // Nhà Nghiên Cứu Gen (Cúc): giảm 6h khóa giám định mỗi ngày và hỗ trợ định danh
                    for (int p = 0; p < pigs.Count; p++)
                    {
                        if (pigs[p].AppraisalLockHoursRemaining > 0)
                        {
                            pigs[p].AppraisalLockHoursRemaining = Math.Max(0, pigs[p].AppraisalLockHoursRemaining - 6);
                        }
                    }
                }
                else if (npc.Id == NpcSpecialistId.ThuKho)
                {
                    // Thủ Kho (Ông Tồn): bảo quản cám tốt, +8kg cám dự phòng/ngày
                    farm.FeedStockKg += 8f;
                }
                else if (npc.Id == NpcSpecialistId.NhaBuon)
                {
                    // Nhà Buôn (Lệ): thu thập tin tức chợ +25G hoa hồng mỗi ngày
                    farm.AddGold(25);
                }
                else if (npc.Id == NpcSpecialistId.VeBinhKhien)
                {
                    gateDef += 0.40f;
                }
                else if (npc.Id == NpcSpecialistId.KySuXayDung || npc.Id == NpcSpecialistId.KySuChienTruong)
                {
                    gateDef += 0.20f;
                }
                else if (npc.Id == NpcSpecialistId.XaThuNo || npc.Id == NpcSpecialistId.ThoSanUfo || npc.Id == NpcSpecialistId.ThichKhachDem)
                {
                    gateDef += 0.15f;
                }
            }

            return (cured, scBonus, gateDef);
        }

        /// <summary>
        /// Thực thi 5 động từ xử lý sức khỏe đàn heo theo GDD 6.5 (P2.04) trực tiếp trên danh sách Pig của GameEngine.
        /// </summary>
        public (bool success, string message) ExecuteHealthVerbOnPigs(
            HealthTreatmentVerb verb,
            Farm farm,
            Pig? targetPig,
            List<Pig> allPigs,
            FarmDrug drug = FarmDrug.HaNhiet,
            DiseaseId researchTarget = DiseaseId.NamPhatQuang)
        {
            switch (verb)
            {
                case HealthTreatmentVerb.CachLy:
                    if (targetPig == null || !targetPig.IsAlive)
                        return (false, "Heo mục tiêu không hợp lệ hoặc đã chết.");
                    if (targetPig.IsIsolated)
                        return (false, $"Heo [{targetPig.Name}] đã ở trong Khu Cách Ly.");
                    int currentIsolated = allPigs.FindAll(p => p.IsAlive && p.IsIsolated).Count;
                    if (currentIsolated >= DiseaseBoard.IsolationCapacity * 2)
                        return (false, "Khu Cách Ly (2 Ô Đất) đã đầy!");
                    targetPig.IsIsolated = true;
                    targetPig.PositionX = -28f;
                    targetPig.PositionY = 16f;
                    return (true, $"Đã đưa heo [{targetPig.Name}] vào Khu Cách Ly chuyên biệt (-28, 16).");

                case HealthTreatmentVerb.XuLyMoiTruong:
                    farm.HabitatIndex = Math.Min(100, farm.HabitatIndex + 6);
                    farm.IsBarnContaminated = false;
                    int sotBunCured = 0;
                    for (int i = 0; i < allPigs.Count; i++)
                    {
                        var p = allPigs[i];
                        if (p.IsAlive && p.ActiveDisease == DiseaseId.SotBun)
                        {
                            p.ActiveDisease = null;
                            p.Health = Math.Min(100, p.Health + 12);
                            sotBunCured++;
                        }
                    }
                    return (true, $"Đã dọn chuồng, thay ổ rơm và khử trùng (+6 Sinh Cảnh, xóa Chuồng Nhiễm, khỏi Sốt Bùn: {sotBunCured}).");

                case HealthTreatmentVerb.DieuTriCaThe:
                    if (targetPig == null || !targetPig.IsAlive || !targetPig.ActiveDisease.HasValue)
                        return (false, "Heo không mắc bệnh hoặc không hợp lệ.");

                    DiseaseId dis = targetPig.ActiveDisease.Value;
                    if (dis == DiseaseId.HuMach && !ResearchedDiseases.Contains(DiseaseId.HuMach))
                        return (false, "Bệnh Hư Mạch (Nhóm D) bắt buộc phải Nghiên Cứu phác đồ trước khi điều trị!");

                    bool cured = false;
                    if (ResearchedDiseases.Contains(dis))
                    {
                        cured = true;
                    }
                    else if (drug == FarmDrug.HaNhiet && (dis == DiseaseId.SayNang || dis == DiseaseId.HoHapLanh || dis == DiseaseId.DichTaHeo))
                    {
                        cured = true;
                    }
                    else if (drug == FarmDrug.TriGhe && dis == DiseaseId.GheKySinh)
                    {
                        cured = true;
                    }
                    else if (drug == FarmDrug.AnThan && (dis == DiseaseId.LoiTam || dis == DiseaseId.RoiLoanSinhSan))
                    {
                        cured = true;
                    }

                    if (!cured)
                        return (false, $"Thuốc [{drug}] không khớp với bệnh [{dis}] hoặc cần Nghiên Cứu trước.");

                    targetPig.ActiveDisease = null;
                    targetPig.SeizurePending = false;
                    targetPig.Health = Math.Min(100, targetPig.Health + 18);
                    targetPig.Mood = Math.Min(100, targetPig.Mood + 8);
                    return (true, $"Đã điều trị khỏi bệnh [{dis}] cho [{targetPig.Name}]!");

                case HealthTreatmentVerb.PhongNguaDan:
                    if (allPigs == null || allPigs.Count == 0)
                        return (false, "Không có đàn heo để tiêm phòng.");
                    int immunized = 0;
                    for (int i = 0; i < allPigs.Count; i++)
                    {
                        var p = allPigs[i];
                        if (p.IsAlive && !p.ActiveDisease.HasValue)
                        {
                            p.ImmuneDays = DiseaseBoard.VaccineDays;
                            immunized++;
                        }
                    }
                    if (farm.VaccineDoseCount > 0) farm.VaccineDoseCount = Math.Max(0, farm.VaccineDoseCount - 1);
                    return (true, $"Đã tiêm Vắc-xin phòng ngừa cho {immunized} heo khỏe (miễn dịch {DiseaseBoard.VaccineDays} ngày).");

                case HealthTreatmentVerb.NghienCuu:
                    if (!farm.SpendGold(120))
                        return (false, "Cần 120 Vàng để Nghiên Cứu mẫu bệnh.");
                    ResearchedDiseases.Add(researchTarget);
                    return (true, $"Đã Nghiên Cứu thành công phác đồ khắc chế [{researchTarget}]!");

                default:
                    return (false, "Động từ không hợp lệ.");
            }
        }

        /// <summary>
        /// Thực thi 5 động từ xử lý sức khỏe đàn heo theo GDD 6.5 (P2.04):
        /// 1. Cách ly, 2. Xử lý môi trường, 3. Điều trị cá thể, 4. Phòng ngừa đàn, 5. Nghiên cứu.
        /// </summary>
        public (bool success, string message) ExecuteHealthVerb(
            HealthTreatmentVerb verb,
            Farm farm,
            IsolationPen? isolationPen,
            HerdPig? targetPig,
            IList<HerdPig>? herd,
            FarmDrug drug = FarmDrug.HaNhiet,
            DiseaseId researchTarget = DiseaseId.NamPhatQuang)
        {
            switch (verb)
            {
                case HealthTreatmentVerb.CachLy:
                    if (isolationPen == null || targetPig == null)
                        return (false, "Thiếu Khu Cách Ly hoặc heo mục tiêu.");
                    bool admitted = isolationPen.TryAdmit(targetPig);
                    return admitted
                        ? (true, $"Đã đưa heo [{targetPig.Id}] vào Khu Cách Ly.")
                        : (false, "Khu Cách Ly đã đầy hoặc heo đã ở trong Khu Cách Ly.");

                case HealthTreatmentVerb.XuLyMoiTruong:
                    farm.HabitatIndex = Math.Min(100, farm.HabitatIndex + 6);
                    if (targetPig != null && targetPig.Disease == DiseaseId.SotBun)
                    {
                        targetPig.Disease = null;
                        targetPig.Health = Math.Min(100, targetPig.Health + 12);
                    }
                    return (true, "Đã dọn chuồng, thay ổ rơm và khử trùng (+6 Sinh Cảnh).");

                case HealthTreatmentVerb.DieuTriCaThe:
                    if (targetPig == null || !targetPig.Disease.HasValue)
                        return (false, "Heo không mắc bệnh hoặc không hợp lệ.");
                    if (targetPig.Disease.Value == DiseaseId.NamPhatQuang && ResearchedDiseases.Contains(DiseaseId.NamPhatQuang))
                    {
                        targetPig.Disease = null;
                        targetPig.Health = Math.Min(100, targetPig.Health + 15);
                        return (true, "Đã điều trị khỏi Nấm Phát Quang nhờ phác đồ đã Nghiên Cứu!");
                    }
                    bool cured = DiseaseBoard.ApplyDrug(targetPig, drug, beddingReplaced: true);
                    return cured
                        ? (true, $"Đã điều trị khỏi bệnh cho heo [{targetPig.Id}] bằng thuốc [{drug}].")
                        : (false, "Thuốc không khớp bệnh hoặc bệnh Hư Mạch không thể chữa bằng thuốc thường.");

                case HealthTreatmentVerb.PhongNguaDan:
                    if (herd == null) return (false, "Không có đàn heo để tiêm phòng.");
                    int immunized = 0;
                    for (int i = 0; i < herd.Count; i++)
                    {
                        if (DiseaseBoard.TryImmunize(herd[i])) immunized++;
                    }
                    return (true, $"Đã tiêm Vắc-xin phòng ngừa cho {immunized} heo khỏe (miễn dịch {DiseaseBoard.VaccineDays} ngày).");

                case HealthTreatmentVerb.NghienCuu:
                    if (!farm.SpendGold(120))
                        return (false, "Cần 120 Vàng để Nghiên Cứu mẫu bệnh.");
                    ResearchedDiseases.Add(researchTarget);
                    return (true, $"Đã Nghiên Cứu thành công phác đồ khắc chế [{researchTarget}]!");

                default:
                    return (false, "Động từ không hợp lệ.");
            }
        }

        public static string GetNpcCanonicalTownName(NpcSpecialistId id)
        {
            return id switch
            {
                NpcSpecialistId.QuanLyCa => "Bảy Ca · Quản Lý Ca",
                NpcSpecialistId.NhaNghienCuuGen => "Cúc · Nhà Nghiên Cứu Gen",
                NpcSpecialistId.BacSiThuY => "Dung · Bác Sĩ Thú Y",
                NpcSpecialistId.KySuXayDung => "Tám Vá · Kỹ Sư Xây Dựng",
                NpcSpecialistId.NhaBuon => "Lệ · Nhà Buôn",
                NpcSpecialistId.ThuKho => "Ông Tồn · Thủ Kho",
                NpcSpecialistId.NongCong => "Mận · Nông Công",
                NpcSpecialistId.VeBinhKhien => "Độ · Vệ Binh Khiên",
                NpcSpecialistId.XaThuNo => "Miên · Xạ Thủ Nỏ",
                NpcSpecialistId.KySuChienTruong => "Chốt · Kỹ Sư Chiến Trường",
                NpcSpecialistId.ThoSanUfo => "Tư Trời · Thợ Săn UFO",
                NpcSpecialistId.ThichKhachDem => "Vân · Thích Khách Đêm",
                _ => id.ToString()
            };
        }

        public static string GetPetCanonicalNickname(PetId petId)
        {
            return petId switch
            {
                PetId.ChoChanDan => "Cò · Chó Chăn Đàn",
                PetId.MeoKho => "Phiếu · Mèo Kho",
                PetId.HeoSanNam => "Tròn · Heo Săn Nấm",
                PetId.QuaTin => "Tin · Quạ Tin",
                PetId.ChoNgaoCanh => "Móp · Chó Ngao Canh",
                PetId.NgongBaoDong => "Giám Đốc · Ngỗng Báo Động",
                PetId.ChoSanChien => "Răng · Chó Săn Chiến",
                _ => petId.ToString()
            };
        }

        /// <summary>
        /// Giải quyết 4 sự kiện Huyền Bí thuộc phạm vi Người B (GDD 24.1 / P4.12):
        /// 1. Vườn Mười Hai Quả, 2. Đồng Cỏ Không Bóng, 3. Bí Nhân Mua Già, 4. Đổi Một Lấy Một.
        /// </summary>
        public (bool resolved, string outcomeMessage) ResolveMemberBOccultEvent(
            MemberBOccultEventId eventId,
            int choiceIndex,
            Farm farm,
            List<Pig> pigs)
        {
            switch (eventId)
            {
                case MemberBOccultEventId.VuonMuoiHaiQua:
                    if (choiceIndex == 0)
                    {
                        pigs.ForEach(p => { if (p.IsAlive) { p.Mood = Math.Min(100, p.Mood + 10); p.GrowthRateMultiplier *= 1.15f; } });
                        farm.EventPressureIndex = Math.Min(100, farm.EventPressureIndex + 2);
                        return (true, "Vườn Mười Hai Quả: Hái 3 quả lành giúp đàn heo tăng trưởng +15% và +10 Tinh thần.");
                    }
                    else
                    {
                        farm.AddGold(600);
                        farm.EventPressureIndex = Math.Min(100, farm.EventPressureIndex + 18);
                        return (true, "Vườn Mười Hai Quả: Hái cạn 12 quả thu 600 Vàng nhưng ÁLSK tăng mạnh (+18).");
                    }

                case MemberBOccultEventId.DongCoKhongBong:
                    if (choiceIndex == 0)
                    {
                        farm.EventPressureIndex = Math.Max(0, farm.EventPressureIndex - 10);
                        farm.HabitatIndex = Math.Min(100, farm.HabitatIndex + 5);
                        return (true, "Đồng Cỏ Không Bóng: Chiếu sáng phá tan dị tượng (-10 ÁLSK, +5 Sinh Cảnh).");
                    }
                    else
                    {
                        var target = pigs.Find(p => p.IsAlive && p.GeneLine != GeneLineId.HuThe);
                        if (target != null)
                        {
                            target.GeneLine = GeneLineId.TinhQuang;
                            target.Rarity = GeneRarity.Hiem;
                        }
                        farm.EventPressureIndex = Math.Min(100, farm.EventPressureIndex + 12);
                        return (true, "Đồng Cỏ Không Bóng: Một cá thể hấp thụ ánh sáng dị giới và biến đổi thành dòng Tinh Quang!");
                    }

                case MemberBOccultEventId.BiNhanMuaGia:
                    var elder = pigs.Find(p => p.IsAlive && p.Stage == PigStage.HeoGia);
                    if (choiceIndex == 0 && elder != null)
                    {
                        pigs.Remove(elder);
                        farm.EventPressureIndex = Math.Max(0, farm.EventPressureIndex - 15);
                        return (true, "Bí Nhân Mua Già: Đã đổi 1 Heo Già lấy Ấn Phong Ấn Hư Thể và giảm 15 ÁLSK (0 Vàng mặt đúng chuẩn GDD).");
                    }
                    return (true, "Bí Nhân Mua Già: Từ chối giao dịch, Bí nhân lặng lẽ rời đi trong sương.");

                case MemberBOccultEventId.DoiMotLayMot:
                    var normalPig = pigs.Find(p => p.IsAlive && p.Rarity == GeneRarity.Thuong);
                    if (choiceIndex == 0 && normalPig != null)
                    {
                        normalPig.GeneLine = GeneLineId.HuThe;
                        normalPig.Rarity = GeneRarity.DiBien;
                        normalPig.Stage = PigStage.HeoNon;
                        normalPig.AgeDays = 1;
                        normalPig.Traits.Clear();
                        normalPig.Traits.Add(new PigTrait { Id = "ThitHuKhong", Name = "Thịt Hư Không", IsPositive = true, IsDeviant = true });
                        normalPig.Traits.Add(new PigTrait { Id = "HutSuong", Name = "Hút Sương", IsPositive = false, IsDeviant = true, IsAwakened = false, IsSealed = false });
                        return (true, "Đổi Một Lấy Một: Đã hoán đổi 1 heo thường lấy 1 Heo non Hư Thể (cần phong ấn trước ngày 3)!");
                    }
                    return (true, "Đổi Một Lấy Một: Giữ nguyên đàn hiện tại.");

                default:
                    return (false, "Sự kiện không xác định.");
            }
        }

        /// <summary>
        /// Giải quyết toàn bộ 14 Sự kiện Huyền Bí (E01–E14: 4 Trang trại, 5 Giao dịch, 5 Cổng/Sương Mù) theo GDD 24.1 & COT_TRUYEN_PIG_TYCOON.md Mục 7,
        /// bao gồm quy tắc vật phẩm rút sớm Bùa Hồi Hương (>=50% tiến độ) và La Bàn Tán Vụ (>=1 ngày sương).
        /// </summary>
        public (bool resolved, string outcomeMessage) ResolveAnyMysteryEventGdd24(
            MysteryEventId eventId,
            int choiceIndex,
            Farm farm,
            List<Pig> pigs,
            float gateOrFogProgressRatio = 1.0f,
            bool useBuaHoiHuong = false,
            bool useLaBanTanVu = false)
        {
            switch (eventId)
            {
                case MysteryEventId.E01_VuonMuoiHaiQua:
                    return ResolveMemberBOccultEvent(MemberBOccultEventId.VuonMuoiHaiQua, choiceIndex, farm, pigs);
                case MysteryEventId.E02_DongCoKhongBong:
                    return ResolveMemberBOccultEvent(MemberBOccultEventId.DongCoKhongBong, choiceIndex, farm, pigs);
                case MysteryEventId.E03_KhoLanhBoQuen:
                    farm.FeedStockKg += 35f;
                    farm.MedicalHerbCount += 3;
                    return (true, "E03 · Kho Lạnh Bỏ Quên: Thu hồi +35kg cám dự trữ và +3 Thảo dược trong hầm lạnh năm 2018.");
                case MysteryEventId.E04_HatGiongTrang:
                    farm.MedicalHerbCount += 4;
                    farm.HabitatIndex = Math.Min(100, farm.HabitatIndex + 4);
                    return (true, "E04 · Hạt Giống Trắng: Gieo hạt bên giếng cạn, thu hoạch +4 Thảo dược trắng và +4 Sinh Cảnh.");
                case MysteryEventId.E05_BiNhanMuaGia:
                    return ResolveMemberBOccultEvent(MemberBOccultEventId.BiNhanMuaGia, choiceIndex, farm, pigs);
                case MysteryEventId.E06_ChoKhongDongHo:
                    farm.EventPressureIndex = Math.Max(0, farm.EventPressureIndex - 6);
                    return (true, "E06 · Chợ Không Đồng Hồ: Đổi vật tư tại phiên chợ đứng kim lúc 02:00 (0 Vàng mặt, -6 ÁLSK).");
                case MysteryEventId.E07_GiamDinhVoDanh:
                    var unidentified = pigs.Find(p => p.IsAlive && !p.IsIdentified);
                    if (unidentified != null) unidentified.IsIdentified = true;
                    return (true, "E07 · Giám Định Vô Danh: Người đeo mặt nạ chì đã mở tên dòng gen ẩn cho 1 cá thể miễn phí.");
                case MysteryEventId.E08_DoiMotLayMot:
                    return ResolveMemberBOccultEvent(MemberBOccultEventId.DoiMotLayMot, choiceIndex, farm, pigs);
                case MysteryEventId.E09_ThuVienMucAn:
                    farm.EventPressureIndex = Math.Min(100, farm.EventPressureIndex + 5);
                    return (true, "E09 · Thư Viện Mực Ấn: Sao chép bản vẽ cổ và công thức Phù Văn tại sạp sách đêm sương.");
                case MysteryEventId.E10_DotSanTrongSuong:
                case MysteryEventId.E11_CanQuetUfo:
                case MysteryEventId.E12_GiuCongNguoc:
                case MysteryEventId.E13_BaNgayKhoa:
                case MysteryEventId.E14_LoMoHuKhong:
                    if (useBuaHoiHuong)
                    {
                        if (gateOrFogProgressRatio < 0.50f)
                            return (false, "Bùa Hồi Hương chỉ có thể dùng khi đã hoàn thành >= 50% tiến độ Cổng!");
                        farm.AddGold(250);
                        return (true, $"[{eventId}] Đã kích hoạt Bùa Hồi Hương rút lui an toàn và giữ lại 50% phần thưởng (+250G)!");
                    }
                    if (useLaBanTanVu)
                    {
                        if (gateOrFogProgressRatio < 0.34f)
                            return (false, "La Bàn Tán Vụ chỉ dùng được sau khi đã trụ qua ít nhất 1 ngày Sương Mù Dị Giới!");
                        farm.EventPressureIndex = Math.Max(0, farm.EventPressureIndex - 12);
                        return (true, $"[{eventId}] Đã dùng La Bàn Tán Vụ xua tan Sương Mù Dị Giới sớm (-12 ÁLSK)!");
                    }
                    farm.AddGold(500);
                    farm.EventPressureIndex = Math.Max(0, farm.EventPressureIndex - 10);
                    return (true, $"[{eventId}] Hoàn thành toàn bộ thử thách Cổng / Sương Mù Dị Giới: +500G và -10 ÁLSK!");
                default:
                    return (false, "Sự kiện không hợp lệ.");
            }
        }
    }

    public enum MysteryEventId
    {
        E01_VuonMuoiHaiQua,
        E02_DongCoKhongBong,
        E03_KhoLanhBoQuen,
        E04_HatGiongTrang,
        E05_BiNhanMuaGia,
        E06_ChoKhongDongHo,
        E07_GiamDinhVoDanh,
        E08_DoiMotLayMot,
        E09_ThuVienMucAn,
        E10_DotSanTrongSuong,
        E11_CanQuetUfo,
        E12_GiuCongNguoc,
        E13_BaNgayKhoa,
        E14_LoMoHuKhong
    }
}

