using System;

namespace PigTycoon.Core
{
    /// <summary>
    /// Growing the farm's capacity (GDD 3.2: capacity = MIN(Ô Đất×8, Mái trú×6, Bồn×12, Máng×10); GDD 3.6: the farm
    /// starts at 12 land plots and reaches about 40). 3 Nov audit: land plots and shelter roofs could never be added, so
    /// capacity stayed at 84 and the herd states needing 100 pigs were out of reach. Feeders and basins are placed in the
    /// world (FarmEnvironment2D); this class adds the other two sources. Decisions: design clarifications §C-10.
    /// </summary>
    public static class FarmExpansion
    {
        public const int StartPlots = 12, MaxPlots = 40;              // GDD 3.6
        public const int PlotsPerFarmLevel = 2;                        // TẠM (C-10): 12 at level 1 → 40 at level 15
        public const int PlotGold = 400;                               // TẠM (C-10): clearing + fencing 10×10 m
        public const float PlotStamina = 12f;                          // TẠM: same scale as lime per plot (GDD 6.5: 8–12)
        public const int RoofGold = 600, RoofWood = 20;                // TẠM (C-10): a 6-pig roof
        public const string WoodId = "item_wood_plank";
        public const int RoofsPerPlot = 2;                             // TẠM: at most two roofs per 10×10 m plot

        public static int PlotLimit(int farmLevel) => Math.Min(MaxPlots, StartPlots + PlotsPerFarmLevel * Math.Max(0, farmLevel - 1));

        public static (bool ok, string message) ExpandLandPlot(GameEngine e)
        {
            var inf = e.Farm.Infrastructure;
            int limit = PlotLimit(e.Farm.FarmLevel);
            if (inf.LandPlots >= limit)
                return (false, inf.LandPlots >= MaxPlots ? $"Trại đã rộng tối đa ({MaxPlots} ô đất)." : $"Cấp trại {e.Farm.FarmLevel} cho tối đa {limit} ô đất; lên cấp để mở rộng.");
            if (e.Clock.IsDuringCombatWave) return (false, "Không khai hoang trong đợt quái.");
            if (e.Farm.Gold < PlotGold) return (false, $"Cần {PlotGold}G để khai hoang và rào 1 ô đất.");
            if (e.Character.Stamina.CurrentStamina < PlotStamina) return (false, $"Cần {PlotStamina:0} Thể lực.");
            e.Ledger.TrySpend(PlotGold, LedgerReason.BuildStructure, "land_plot");
            e.Character.Stamina.TryConsumeStamina(PlotStamina, 1f);
            inf.LandPlots += 1;
            return (true, $"Khai hoang thêm 1 ô đất ({inf.LandPlots} ô, sức chứa theo đất {inf.LandPlots * 8}). Phí duy trì +4G/ngày.");
        }

        public static (bool ok, string message) BuildShelterRoof(GameEngine e)
        {
            var inf = e.Farm.Infrastructure;
            if (inf.Shelters >= inf.LandPlots * RoofsPerPlot) return (false, "Hết chỗ dựng mái: khai hoang thêm ô đất trước.");
            if (e.Clock.IsDuringCombatWave) return (false, "Không dựng mái trong đợt quái.");
            if (e.Farm.Gold < RoofGold || e.Inventory.CountId(WoodId) < RoofWood)
                return (false, $"Cần {RoofGold}G và {RoofWood} ván gỗ để dựng 1 mái trú.");
            e.Ledger.TrySpend(RoofGold, LedgerReason.BuildStructure, "shelter_roof");
            e.Inventory.ConsumeItem(WoodId, RoofWood);
            inf.Shelters += 1;
            return (true, $"Dựng thêm 1 mái trú ({inf.Shelters} mái, sức chứa theo mái {inf.Shelters * 6}). Phí duy trì +6G/ngày.");
        }
    }
}
