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
                NpcSpecialistId.BacSiThuY => 65,
                NpcSpecialistId.NongCong => 40,
                NpcSpecialistId.VeBinhKhien => 75,
                _ => 55
            };

            HiredNpcs.Add(new HiredNpcRecord
            {
                Id = id,
                Name = id.ToString(),
                DailyWageGold = wage,
                AssignedWorldX = worldX,
                AssignedWorldY = worldY,
                IsActive = true
            });
            return true;
        }

        /// <summary>
        /// Thực thi ca làm việc mỗi ngày của 3 NPC thuộc phạm vi Người B (Bác Sĩ Thú Y, Nông Công, Vệ Binh Khiên).
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
            }

            return (cured, scBonus, gateDef);
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
                        // Hái 3 quả lành: heo tăng trưởng +15% và +10 Tinh thần, ÁLSK chỉ +2
                        pigs.ForEach(p => { if (p.IsAlive) { p.Mood = Math.Min(100, p.Mood + 10); p.GrowthRateMultiplier *= 1.15f; } });
                        farm.EventPressureIndex = Math.Min(100, farm.EventPressureIndex + 2);
                        return (true, "Vườn Mười Hai Quả: Hái 3 quả lành giúp đàn heo tăng trưởng +15% và +10 Tinh thần.");
                    }
                    else
                    {
                        // Hái sạch 12 quả: +600 Vàng nhưng ÁLSK +18
                        farm.AddGold(600);
                        farm.EventPressureIndex = Math.Min(100, farm.EventPressureIndex + 18);
                        return (true, "Vườn Mười Hai Quả: Hái cạn 12 quả thu 600 Vàng nhưng ÁLSK tăng mạnh (+18).");
                    }

                case MemberBOccultEventId.DongCoKhongBong:
                    if (choiceIndex == 0)
                    {
                        // Thắp đèn pha chiếu sáng đồng cỏ: -10 ÁLSK, +5 Sinh Cảnh
                        farm.EventPressureIndex = Math.Max(0, farm.EventPressureIndex - 10);
                        farm.HabitatIndex = Math.Min(100, farm.HabitatIndex + 5);
                        return (true, "Đồng Cỏ Không Bóng: Chiếu sáng phá tan dị tượng (-10 ÁLSK, +5 Sinh Cảnh).");
                    }
                    else
                    {
                        // Lùa 1 heo vào vùng không bóng để thử vận may: biến đổi thành dòng gen Tinh Quang
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
                        // Giao 1 Heo Già cho Bí nhân: không nhận Vàng mặt, nhận Ấn Phong Ấn Hư Thể + Giảm 15 ÁLSK
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
    }
}
