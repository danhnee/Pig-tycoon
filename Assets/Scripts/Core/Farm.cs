using System;

namespace PigTycoon.Core
{
    [Serializable]
    public class FarmInfrastructure
    {
        public int LandPlots = 12;      // Ô Đất (x8, duy trì 4G/ngày)
        public int Shelters = 14;       // Mái trú (x6, duy trì 6G/ngày)
        public int WaterTroughs = 8;    // Bồn nước (x12, duy trì 3G/ngày)
        public int Feeders = 9;         // Máng ăn (x10, duy trì 5G/ngày)
        public int ProcessingLotCapacity = 4;
        public int DrainageSystemLevel = 0;
    }

    [Serializable]
    public class FarmCapacityReport
    {
        public int CapacityByLand;
        public int CapacityByShelters;
        public int CapacityByWater;
        public int CapacityByFeeders;
        public string Bottleneck;
        public int BaseCapacity;
        public int UnprocessedCorpsesOutsideLot;
        public int EffectiveCapacity;
        public int LivingPigCount;
        public float RawDensityRatio;
        public string DensityZoneName;
        public float DiseaseDensityMultiplier;
        public int DailyEmptyMaintenanceCost;
    }

    [Serializable]
    public class Farm
    {
        public int FarmLevel { get; set; } = 1;
        public int FarmExp { get; set; } = 0;
        public FarmInfrastructure Infrastructure { get; set; } = new FarmInfrastructure();

        public int HabitatIndex { get; set; } = 75; // SC 0 - 100
        public HabitatTier HabitatTier => GetHabitatTier();

        public int EventPressureIndex { get; set; } = 10; // ÁLSK 0 - 100
        public EventPressureState EventPressureState => GetEventPressureState();

        public int Gold { get; set; } = 2500;
        public int FuelLiters { get; set; } = 40;
        public int CorpseLotCount { get; set; } = 0;

        public FarmCapacityReport CalculateCapacityReport(int livingPigCount, int corpsesOutsideLot)
        {
            int capLand = Infrastructure.LandPlots * 8;
            int capShelters = Infrastructure.Shelters * 6;
            int capWater = Infrastructure.WaterTroughs * 12;
            int capFeeders = Infrastructure.Feeders * 10;

            int baseCap = Math.Min(capLand, Math.Min(capShelters, Math.Min(capWater, capFeeders)));

            string bottleneck = "LandPlots";
            if (baseCap == capShelters) bottleneck = "Shelters";
            else if (baseCap == capFeeders) bottleneck = "Feeders";
            else if (baseCap == capWater) bottleneck = "WaterTroughs";

            int effectiveCap = Math.Max(1, baseCap - corpsesOutsideLot);
            float rawDensity = (float)livingPigCount / effectiveCap;
            float diseaseMultiplier = CalculateSoftInterpolatedDensity(rawDensity);

            string zoneName = "Vận hành kinh tế (71–100%)";
            if (rawDensity <= 0.70f) zoneName = "Dư sức chứa (≤ 70%)";
            else if (rawDensity <= 1.00f) zoneName = "Vận hành kinh tế (71–100%)";
            else if (rawDensity <= 1.20f) zoneName = "Quá tải nhẹ (101–120%)";
            else if (rawDensity <= 1.50f) zoneName = "Áp lực hệ thống (121–150%)";
            else zoneName = "Khủng hoảng (> 150%)";

            int maintCost = Infrastructure.LandPlots * 4 +
                            Infrastructure.Shelters * 6 +
                            Infrastructure.WaterTroughs * 3 +
                            Infrastructure.Feeders * 5;

            return new FarmCapacityReport
            {
                CapacityByLand = capLand,
                CapacityByShelters = capShelters,
                CapacityByWater = capWater,
                CapacityByFeeders = capFeeders,
                Bottleneck = bottleneck,
                BaseCapacity = baseCap,
                UnprocessedCorpsesOutsideLot = corpsesOutsideLot,
                EffectiveCapacity = effectiveCap,
                LivingPigCount = livingPigCount,
                RawDensityRatio = rawDensity,
                DensityZoneName = zoneName,
                DiseaseDensityMultiplier = diseaseMultiplier,
                DailyEmptyMaintenanceCost = maintCost
            };
        }

        public float CalculateSoftInterpolatedDensity(float density)
        {
            if (density <= 0.70f) return 0.85f;
            if (density <= 1.00f)
            {
                return 0.85f + (1.00f - 0.85f) * ((density - 0.70f) / 0.30f);
            }
            if (density <= 1.20f)
            {
                return 1.00f + (1.25f - 1.00f) * ((density - 1.00f) / 0.20f);
            }
            if (density <= 1.50f)
            {
                return 1.25f + (1.65f - 1.25f) * ((density - 1.20f) / 0.30f);
            }
            float t = Math.Min(1.0f, (density - 1.50f) / 0.30f);
            return 1.65f + (2.20f - 1.65f) * t;
        }

        public void UpdateDailyHabitat(int cleanings, int corpsesOutside, int activeDiseases, float density, bool goodPasture, bool leaderSafe)
        {
            int delta = 0;
            delta += Math.Min(12, cleanings * 4);
            if (goodPasture) delta += 1;
            if (leaderSafe && corpsesOutside == 0) delta += 1;
            if (Infrastructure.DrainageSystemLevel > 0) delta += 1;

            delta -= (corpsesOutside * 3);
            delta -= CorpseLotCount;
            delta -= activeDiseases;

            if (density > 1.50f) delta -= 4;
            else if (density > 1.20f) delta -= 2;
            else if (density > 1.00f) delta -= 1;

            if (delta == 0)
            {
                if (HabitatIndex > 60) delta = -1;
                else if (HabitatIndex < 60) delta = 1;
            }

            HabitatIndex = Math.Max(0, Math.Min(100, HabitatIndex + delta));
        }

        public void UpdateDailyEventPressure(int elderPigs, int totalPigs, float density, int corpsesOutside)
        {
            int delta = 0;
            int softElderThreshold = (int)(totalPigs * 0.15f);
            int excessElder = Math.Max(0, elderPigs - softElderThreshold);
            delta += (excessElder / 10);
            delta += CorpseLotCount;

            if (density > 1.20f && density <= 1.50f) delta += 3;
            if (HabitatIndex >= 30 && HabitatIndex < 50) delta += 3;

            delta += Math.Min(12, corpsesOutside * 3);

            if (density > 1.50f) delta += 6;
            if (HabitatIndex < 30) delta += 6;

            if (delta == 0 && EventPressureIndex > 0)
            {
                EventPressureIndex = Math.Max(0, (int)(EventPressureIndex * 0.92f));
            }
            else
            {
                EventPressureIndex = Math.Max(0, Math.Min(100, EventPressureIndex + delta));
            }
        }

        public HabitatTier GetHabitatTier()
        {
            if (HabitatIndex >= 90) return HabitatTier.TrongLanh;
            if (HabitatIndex >= 70) return HabitatTier.OnDinh;
            if (HabitatIndex >= 50) return HabitatTier.TrungTinh;
            if (HabitatIndex >= 30) return HabitatTier.ONhiem;
            return HabitatTier.OUe;
        }

        public EventPressureState GetEventPressureState()
        {
            if (EventPressureIndex >= 80) return EventPressureState.RanNut;
            if (EventPressureIndex >= 50) return EventPressureState.BatOn;
            if (EventPressureIndex >= 20) return EventPressureState.GonSong;
            return EventPressureState.YenA;
        }

        public void AddGold(int amount) => Gold += amount;

        public bool SpendGold(int amount)
        {
            if (Gold >= amount)
            {
                Gold -= amount;
                return true;
            }
            return false;
        }

        public void AddFarmExp(int exp)
        {
            FarmExp += exp;
            int threshold = FarmLevel * 1500;
            if (FarmExp >= threshold && FarmLevel < 20)
            {
                FarmLevel += 1;
            }
        }
    }
}
