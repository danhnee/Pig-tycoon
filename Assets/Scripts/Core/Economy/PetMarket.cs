using System;
using System.Linq;

namespace PigTycoon.Core
{
    /// <summary>
    /// Pets at the night market (GDD 20 table: "Trao đổi theo yêu cầu người bán: … thú cưng"; first pet at farm level
    /// 3–4, GDD level table; slots 2/4/6 at levels 3/8/14, GDD 11.3). Paid by trade, never gold.
    /// PROVISIONAL (ADR-0016): the GDD names no pet price. Daily-support pets ask 1 adult pig, guard pets ask 2,
    /// the same currency the GDD uses for the cheapest mercenary (Vệ Binh Khiên: 3 adult pigs).
    /// </summary>
    public static class PetMarket
    {
        public static readonly PetId[] All =
        {
            PetId.ChoChanDan, PetId.MeoKho, PetId.HeoSanNam, PetId.QuaTin, PetId.ChoNgaoCanh, PetId.NgongBaoDong, PetId.ChoSanChien
        };

        public static bool IsGuard(PetId id) => id == PetId.ChoNgaoCanh || id == PetId.NgongBaoDong || id == PetId.ChoSanChien;

        public static int AdultPigsAsked(PetId id) => IsGuard(id) ? 2 : 1;

        public static string Terms(PetId id) => $"đổi {AdultPigsAsked(id)} heo trưởng thành";

        public static (bool success, string message) Adopt(GameEngine engine, PetId id)
        {
            var pets = engine.PetsAndNpcs;
            if (pets.HasPet(id)) return (false, "Đã có con này.");
            int slots = PetNpcAndOccultManager.PetSlotsFor(engine.Farm.FarmLevel);
            if (slots == 0) return (false, "Ô thú cưng mở từ cấp trại 3.");
            if (pets.ActivePets.Count >= slots) return (false, $"Hết ô thú cưng ({slots} ô ở cấp trại {engine.Farm.FarmLevel}).");
            int hour = engine.Clock.CurrentHour;
            if (!(hour >= 18 || hour < 6)) return (false, "Thú cưng chỉ đổi ở chợ đêm (18:00–06:00).");
            var give = engine.Pigs.Where(p => p.IsAlive && p.Stage == PigStage.TruongThanh).Take(AdultPigsAsked(id)).ToList();
            if (give.Count < AdultPigsAsked(id)) return (false, $"Cần {Terms(id)}.");
            foreach (var p in give) engine.Pigs.Remove(p);
            pets.TryEquipPet(id, engine.Farm.FarmLevel);
            engine.SignalQuest(QuestSignal.PetAdopted);
            return (true, $"{PetNpcAndOccultManager.GetPetDisplayName(id)} về trại.");
        }
    }
}
