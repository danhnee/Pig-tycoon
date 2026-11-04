using System;
using System.Linq;

namespace PigTycoon.Core
{
    /// <summary>
    /// Using the GDD 12.8 farm, info and event items from the warehouse (3 Nov audit: none of them had an effect, so
    /// Cám Linh Chi could never feed Bồi Dưỡng Kế Vị, Mái Che Tạm never shaded, the info items told nothing).
    /// Combat items go through QuickBag / MartialSkillRunner; exit charms through OccultExpedition.
    /// Every effect is the GDD table text; durations and numbers are the GDD's.
    /// </summary>
    public static class ItemUse
    {
        public const float DouseStamina = 3f;          // GDD 6.4 Say Nắng: "dội Xô Nước (3 Stamina/con)"
        public const int TempShadePigs = 4;            // "tính như 1 mái trú cho 4 heo"
        public const int SedativeMood = 30;            // "Tinh thần +30, dập cơn Lôi Tâm"
        public const int LinhChiMood = 5;              // "+5 Tinh thần/ngày cho con được ăn"
        public const int GateKeyMinutes = 360;         // "Giữ một Cổng không biến mất thêm 6 giờ"
        public const int FogLeafHours = 24;            // "24 giờ: … cảnh báo trước 10 phút game"

        /// <summary>Items that need a pig as target.</summary>
        public static bool NeedsPig(string itemId) =>
            itemId is "XoNuoc" or "CamLinhChi" or "CamHaHoa" or "ThuocAnThanHeo" or "HoSoGiongLoai";

        public static bool IsUsable(string itemId) => NeedsPig(itemId) ||
            itemId is "MaiCheTam" or "TinDonCho" or "LaDoSuong" or "BanDoHuongQuai" or "ChiaCongNguoc";

        /// <summary>Uses one <paramref name="itemId"/>: from the space bag slot <paramref name="bagIndex"/> when given, else from the
        /// warehouse (through the bracelet link out of combat, or directly when <paramref name="atWarehouse"/>).</summary>
        public static (bool ok, string message) Use(GameEngine e, string itemId, string? pigId = null, int bagIndex = -1, bool atWarehouse = true,
            float placeX = float.NaN, float placeY = float.NaN)
        {
            if (!IsUsable(itemId)) return (false, "Vật phẩm này không dùng trực tiếp được.");
            if (bagIndex >= 0)
            {
                var bag = e.SpaceBag.Items;
                if (bagIndex >= bag.Count || !SameId(bag[bagIndex].ItemId, itemId)) return (false, "Túi đã thay đổi.");
            }
            else
            {
                if (!e.Inventory.ContainsId(itemId)) return (false, "Không có vật phẩm này trong kho.");
                if (!atWarehouse && !e.Carry.CanUseWarehouseLink(out var linkWhy)) return (false, linkWhy);
            }
            Pig? pig = null;
            if (NeedsPig(itemId))
            {
                pig = e.Pigs.Find(p => p.Id == pigId && p.IsAlive);
                if (pig == null) return (false, "Chọn một con heo còn sống.");
            }
            bool consume = true;
            string msg;
            switch (itemId)
            {
                case "XoNuoc":
                    if (pig!.ActiveDisease != DiseaseId.SayNang) return (false, $"{pig.Name} không bị Say Nắng.");
                    // one cost path: GDD 9.2 Dội Xô Nước 3 × Nắng Gắt 1.15 × GDD 9.3 environment/health/hunger
                    float douse = FarmLaborStamina.Cost(e, FarmLaborTask.DouseBucket);
                    if (!e.Character.Stamina.TryConsumeStamina(douse, 1f)) return (false, $"Cần {douse:0.#} Thể lực.");
                    pig.ActiveDisease = null;
                    pig.HeatCollapsedHours = 0;
                    consume = false; // "dùng lại được"
                    msg = $"Dội nước hạ nhiệt cho {pig.Name}.";
                    break;
                case "MaiCheTam":
                    e.Farm.TempShadePigsToday += TempShadePigs;
                    if (!float.IsNaN(placeX) && !float.IsNaN(placeY)) e.GridZones.TempShades.Add((placeX, placeY)); // shade 6 m where the player stands
                    msg = $"Dựng Mái Che Tạm: bóng mát 6 m tại chỗ, +{TempShadePigs} chỗ mái trú tới hết ngày.";
                    break;
                case "CamLinhChi":
                    if (pig!.LinhChiFedDay == e.Clock.CurrentDay) return (false, $"{pig.Name} đã ăn Cám Linh Chi hôm nay.");
                    pig.LinhChiFedDay = e.Clock.CurrentDay;
                    pig.Mood = Math.Min(100, pig.Mood + LinhChiMood);
                    msg = $"{pig.Name} ăn Cám Linh Chi (+{LinhChiMood} Tinh thần; tính cho Bồi Dưỡng Kế Vị hôm nay).";
                    break;
                case "CamHaHoa":
                    pig!.CalmFeedDay = e.Clock.CurrentDay;
                    msg = $"{pig.Name} ăn Cám Hạ Hỏa: Tinh thần mất ít hơn 20% tới hết ngày.";
                    break;
                case "ThuocAnThanHeo":
                    pig!.Mood = Math.Min(100, pig.Mood + SedativeMood);
                    pig.SeizurePending = false;
                    msg = $"{pig.Name} dịu lại (+{SedativeMood} Tinh thần).";
                    break;
                case "HoSoGiongLoai":
                    if (pig!.IsIdentified) return (false, $"{pig.Name} đã được định danh.");
                    pig.IsIdentified = true;
                    msg = $"Hồ Sơ Giống Loài: {pig.Name} là {GeneticsAndBreedingSystem.GetGeneProfile(pig.GeneLine).Name}.";
                    break;
                case "TinDonCho":
                    msg = $"Tin đồn chợ: chu kỳ này là \"{MarketCycleThemes.NameVi(e.Economy.DayMarket.Theme)}\".";
                    break;
                case "LaDoSuong":
                    e.FogWarningUntilMinute = e.Clock.TotalElapsedGameMinutes + FogLeafHours * 60;
                    msg = "Lá Dò Sương: 24 giờ tới, sương mù huyền bí sẽ được báo trước 10 phút.";
                    break;
                case "BanDoHuongQuai":
                    var entries = e.PlayableMap?.MonsterEntries.Select(m => m.Sector).Distinct().ToList();
                    var sectors = WaveApproachPlanner.PlanSectors(e.BaseSeed, e.Clock.CycleNumber, 0, true,
                        entries != null && entries.Count > 0 ? entries : WaveApproachPlanner.AllSectors.ToList());
                    msg = sectors.Count > 0 ? $"Bản Đồ Hướng Quái: làn đầu đợt lớn tới từ {WaveApproachPlanner.Describe(sectors.Take(1))}." : "Bản đồ không đọc được hướng nào.";
                    break;
                case "ChiaCongNguoc":
                    if (!e.ForestAndEvents.Director.ExtendOpenGate(e, GateKeyMinutes)) return (false, "Không có Cổng nào đang mở.");
                    msg = "Chìa Cổng Ngược: Cổng giữ thêm 6 giờ.";
                    break;
                default:
                    return (false, "Vật phẩm này không dùng trực tiếp được.");
            }
            if (consume)
            {
                if (bagIndex >= 0) e.SpaceBag.TryConsumeOne(bagIndex, out _, out _);
                else e.Inventory.ConsumeItem(itemId);
            }
            return (true, msg);
        }

        static bool SameId(string? a, string b) => a != null &&
            string.Equals(QualifiedItemRegistry.QualifyItemId(a), QualifiedItemRegistry.QualifyItemId(b), StringComparison.OrdinalIgnoreCase);

        /// <summary>Unqualified catalogue id ("(O)CamLinhChi" → "CamLinhChi") for <see cref="IsUsable"/> checks from UI rows.</summary>
        public static string BareId(string? id)
        {
            if (string.IsNullOrEmpty(id)) return string.Empty;
            int close = id!.IndexOf(')');
            return id.StartsWith("(") && close > 0 ? id.Substring(close + 1) : id;
        }
    }
}
