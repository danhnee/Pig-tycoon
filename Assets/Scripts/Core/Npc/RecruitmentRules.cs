using System;
using System.Collections.Generic;
using System.Linq;

namespace PigTycoon.Core
{
    /// <summary>
    /// GDD v6.0 §11.1–11.2: where each NPC is recruited and what it costs.
    /// Farm NPCs: hired at a market counter (day / night / auction), paid a daily wage by the Core shift.
    /// Mercenaries: hired for a cycle by trading what they ask for. Only the asks the catalog can express are
    /// payable here (gold or pigs); item asks (Giáp Da, tên, pin, bản vẽ) are listed but not payable yet (ADR-0014).
    /// </summary>
    public static class RecruitmentRules
    {
        public enum Venue { DayMarket, NightMarket, NightAuction, DayOrNight }

        public sealed class Offer
        {
            public NpcSpecialistId Id;
            public string Role = "";
            public Venue Where;
            public string Terms = "hợp đồng (ADR-0026)";
            public bool IsMercenary;
        }

        public static readonly Offer[] Offers =
        {
            new Offer { Id = NpcSpecialistId.QuanLyCa, Role = "Quản Lý Ca", Where = Venue.NightMarket, Terms = "hợp đồng (ADR-0026)" },
            new Offer { Id = NpcSpecialistId.NhaNghienCuuGen, Role = "Nhà Nghiên Cứu Gen", Where = Venue.NightAuction, Terms = "hợp đồng (ADR-0026)" },
            new Offer { Id = NpcSpecialistId.BacSiThuY, Role = "Bác Sĩ Thú Y", Where = Venue.DayMarket, Terms = "hợp đồng (ADR-0026)" },
            new Offer { Id = NpcSpecialistId.KySuXayDung, Role = "Kỹ Sư Xây Dựng", Where = Venue.DayOrNight, Terms = "hợp đồng (ADR-0026)" },
            new Offer { Id = NpcSpecialistId.NhaBuon, Role = "Nhà Buôn", Where = Venue.NightMarket, Terms = "hợp đồng (ADR-0026)" },
            new Offer { Id = NpcSpecialistId.ThuKho, Role = "Thủ Kho", Where = Venue.DayMarket, Terms = "hợp đồng (ADR-0026)" },
            new Offer { Id = NpcSpecialistId.NongCong, Role = "Nông Công", Where = Venue.DayMarket, Terms = "hợp đồng (ADR-0026)" },
            new Offer { Id = NpcSpecialistId.VeBinhKhien, Role = "Vệ Binh Khiên", Where = Venue.DayOrNight, Terms = "hợp đồng (ADR-0026)", IsMercenary = true },
            new Offer { Id = NpcSpecialistId.XaThuNo, Role = "Xạ Thủ Nỏ", Where = Venue.DayOrNight, Terms = "hợp đồng (ADR-0026)", IsMercenary = true },
            new Offer { Id = NpcSpecialistId.KySuChienTruong, Role = "Kỹ Sư Chiến Trường", Where = Venue.DayOrNight, Terms = "hợp đồng (ADR-0026)", IsMercenary = true },
            new Offer { Id = NpcSpecialistId.ThoSanUfo, Role = "Thợ Săn UFO", Where = Venue.DayOrNight, Terms = "hợp đồng (ADR-0026)", IsMercenary = true },
            new Offer { Id = NpcSpecialistId.ThichKhachDem, Role = "Thích Khách Đêm", Where = Venue.DayOrNight, Terms = "hợp đồng (ADR-0026)", IsMercenary = true },
        };

        public static bool IsOpen(Venue where, int hour)
        {
            bool night = hour >= 18 || hour < 6;
            return where switch
            {
                Venue.DayMarket => !night,
                Venue.NightMarket => night,
                Venue.NightAuction => night,
                _ => true,
            };
        }

        /// <summary>ADR-0026: thuê = ký hợp đồng có kì hạn (mặc định kì ngắn nhất của NPC đó). Luật cũ "đổi đồ / theo ngày" bỏ.</summary>
        public static (bool success, string message) Hire(GameEngine engine, NpcSpecialistId id, bool payWithGold = false)
        {
            var offerV = Offers.FirstOrDefault(o => o.Id == id);
            if (offerV != null && !IsOpen(offerV.Where, engine.Clock.CurrentHour))
                return (false, offerV.Where == Venue.DayMarket ? "Chỉ tuyển ở chợ ngày (06:00–18:00)." : "Chỉ tuyển ở chợ đêm (18:00–06:00).");
            var profile = NpcContracts.Profile(id);
            if (profile == null) return (false, "Không có người này để thuê.");
            var kind = profile.Tier == RoleTier.Cam ? ContractKind.ChuKyLon : ContractKind.ChuKyNho;
            int terms = kind == ContractKind.ChuKyLon ? NpcContracts.MinLargeCycles : 1;
            return NpcContracts.Sign(engine, id, kind, terms);
        }
    }
}
