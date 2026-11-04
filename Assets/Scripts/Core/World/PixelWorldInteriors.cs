using System;
using System.Collections.Generic;

namespace PigTycoon.Core
{
    /// <summary>A building the player walks into (Stardew-style): its own small map behind the building's door.</summary>
    public sealed class PixelWorldInterior
    {
        public string LocationId { get; }
        public string SceneName { get; }
        public string ParentLocationId { get; }
        /// <summary>Layout interact id of the building outside ("main_house").</summary>
        public string BuildingId { get; }
        public string DisplayName { get; }

        public PixelWorldInterior(string id, string parent, string buildingId, string displayName)
        { LocationId = id; SceneName = id; ParentLocationId = parent; BuildingId = buildingId; DisplayName = displayName; }

        /// <summary>Portal in the parent map, at the building's door.</summary>
        public string DoorPortalId => ParentLocationId + "->" + LocationId;
        /// <summary>Portal inside the interior, at its exit door (the interior layout must carry this id).</summary>
        public string ExitPortalId => LocationId + "->" + ParentLocationId;
    }

    /// <summary>
    /// Walk-in interiors (4 Nov user ask: "công trình… không thể vào bên trong như Stardew"). Each interior is a map
    /// built from Assets/Art/PixelMyth/Maps/&lt;scene&gt;_layout.json like the four outdoor maps; until Argon delivers
    /// that layout the building keeps its interior panel. Ids are new so old saves ("FarmHouse" = Farm) stay valid.
    /// </summary>
    public static class PixelWorldInteriors
    {
        public static IReadOnlyList<PixelWorldInterior> All { get; } = Array.AsReadOnly(new[]
        {
            new PixelWorldInterior("Interior_NhaChinh", "Farm", "main_house", "Nhà Chính & Bếp Lò"),
            new PixelWorldInterior("Interior_XuongRen", "Farm", "construction_hall", "Xưởng Rèn & Phòng Xây Dựng"),
            new PixelWorldInterior("Interior_QuanBaSau", "Hub_Village", "quan_ba_sau", "Quán Bà Sáu & Trạm Tin"),
        });

        public static PixelWorldInterior? ForBuilding(string? buildingId)
        {
            if (string.IsNullOrEmpty(buildingId)) return null;
            foreach (var i in All) if (i.BuildingId == buildingId) return i;
            return null;
        }

        public static PixelWorldInterior? ForLocation(string? locationId)
        {
            if (string.IsNullOrEmpty(locationId)) return null;
            foreach (var i in All)
                if (string.Equals(i.LocationId, locationId, StringComparison.OrdinalIgnoreCase)) return i;
            return null;
        }

        /// <summary>An interior connects only to the map its door is in.</summary>
        public static bool Connects(string source, string target)
        {
            var a = ForLocation(source);
            if (a != null && a.ParentLocationId == target) return true;
            var b = ForLocation(target);
            return b != null && b.ParentLocationId == source;
        }
    }
}
