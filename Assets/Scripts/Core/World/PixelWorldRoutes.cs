using System;
using System.Collections.Generic;

namespace PigTycoon.Core
{
    public sealed class PixelWorldMap
    {
        public string LocationId { get; }
        public string SceneName { get; }
        public string DisplayName { get; }
        public PixelWorldMap(string id, string scene, string displayName)
        { LocationId = id; SceneName = scene; DisplayName = displayName; }
    }

    /// <summary>Scene bindings for the existing domain world. Existing saved IDs remain unchanged.</summary>
    public static class PixelWorldRoutes
    {
        /// <summary>Exactly the four MAIN maps (user contract). Walk-in interiors are rooms, not maps: <see cref="Interiors"/>.</summary>
        public static IReadOnlyList<PixelWorldMap> Maps { get; } = Array.AsReadOnly(new[]
        {
            new PixelWorldMap("Farm", "Farm_Main", "Đồng Cỏ Heo Thả Rông"),
            new PixelWorldMap("Hub_Village", "Hub_Village", "Làng Bờ Rìa"),
            new PixelWorldMap("MarketTown", "Market_Town", "Thị Trấn Cảng — Chợ Ngày / Đêm"),
            new PixelWorldMap("Forest_Primeval", "Forest_Primeval", "Rừng Nguyên Sinh")
        });

        /// <summary>Scene bindings of the walk-in interiors (<see cref="PixelWorldInteriors"/>).</summary>
        public static IReadOnlyList<PixelWorldMap> Interiors { get; } = Array.AsReadOnly(
            System.Linq.Enumerable.ToArray(System.Linq.Enumerable.Select(PixelWorldInteriors.All, i => new PixelWorldMap(i.LocationId, i.SceneName, i.DisplayName))));

        static readonly string[,] Connections =
        {
            { "Farm", "Hub_Village" }, { "Farm", "MarketTown" },
            { "Hub_Village", "MarketTown" }
        };

        public static PixelWorldMap ForLocation(string locationOrScene)
        {
            switch (locationOrScene)
            {
                case "Farm_Core": case "FarmHouse": case "GeneLab": case "Observatory": locationOrScene = "Farm"; break;
                case "Barn_Primary": case "Barn_Quarantine": locationOrScene = "Farm"; break;
                case "BoundaryEdge": locationOrScene = "Farm"; break;
            }
            if (string.Equals(locationOrScene, "BoRiaVillage2D", StringComparison.OrdinalIgnoreCase)) locationOrScene = "Hub_Village";
            foreach (var list in new[] { Maps, Interiors })
                foreach (var map in list)
                    if (string.Equals(map.LocationId, locationOrScene, StringComparison.OrdinalIgnoreCase) ||
                        string.Equals(map.SceneName, locationOrScene, StringComparison.OrdinalIgnoreCase)) return map;
            throw new ArgumentException("No 2D scene binding for location: " + locationOrScene, nameof(locationOrScene));
        }

        public static bool CanTravel(string source, string target)
        {
            if (PixelWorldInteriors.Connects(source, target)) return true;
            for (int i = 0; i < Connections.GetLength(0); i++)
                if ((Connections[i, 0] == source && Connections[i, 1] == target) ||
                    (Connections[i, 1] == source && Connections[i, 0] == target)) return true;
            return false;
        }

        /// <summary>Dynamic encounter routes never become permanent roads.</summary>
        public static bool CanTravel(string source, string target, GameEngine engine, string? exitItemId = null)
        {
            // the farm is the combat scene: nobody walks off to the market while a wave is inside the fence
            if (source == "Farm" && engine.Clock.IsDuringCombatWave) return false;
            if (source == "Farm" && target == "Forest_Primeval")
                return engine.ForestAndEvents.Expedition.IsActive;
            if (source == "Forest_Primeval" && target == "Farm")
                return engine.ForestAndEvents.Expedition.EvaluateExit(engine.Clock.CurrentDay, engine.SpaceBag, exitItemId).canExit;
            return CanTravel(source, target);
        }

        public static void Register(WorldLocationGraph graph)
        {
            if (graph == null) throw new ArgumentNullException(nameof(graph));
            foreach (var map in Maps)
                if (graph.GetLocation(map.LocationId) == null)
                    graph.RegisterLocation(new GameLocation { LocationId = map.LocationId, DisplayName = map.DisplayName, IsOutdoors = true });
            foreach (var map in Interiors)
                if (graph.GetLocation(map.LocationId) == null)
                    graph.RegisterLocation(new GameLocation { LocationId = map.LocationId, DisplayName = map.DisplayName, IsOutdoors = false });
            foreach (var i in PixelWorldInteriors.All)
            {
                AddPortal(graph.GetLocation(i.ParentLocationId)!, i.LocationId);
                AddPortal(graph.GetLocation(i.LocationId)!, i.ParentLocationId);
            }
            // These graph edges represent an encounter portal, not a walkable forest road.
            AddPortal(graph.GetLocation("Farm")!, "Forest_Primeval");
            AddPortal(graph.GetLocation("Forest_Primeval")!, "Farm");
            if (graph.GetLocation("Farm_Core") == null)
            {
                var services = new GameLocation { LocationId = "Farm_Core", DisplayName = "Khu dịch vụ đồng cỏ", IsOutdoors = false };
                graph.RegisterLocation(services);
                AddPortal(services, "Farm");
            }
            AddPortal(graph.GetLocation("Farm")!, "Farm_Core");
            for (int i = 0; i < Connections.GetLength(0); i++)
            {
                AddPortal(graph.GetLocation(Connections[i, 0])!, Connections[i, 1]);
                AddPortal(graph.GetLocation(Connections[i, 1])!, Connections[i, 0]);
            }
        }

        static void AddPortal(GameLocation source, string target)
        {
            if (!source.Warps.Exists(w => w.TargetLocationId == target))
                source.Warps.Add(new LocationWarpPortal { TargetLocationId = target });
        }
    }
}
