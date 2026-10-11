using System;

namespace PigTycoon.Core
{
    public enum DiseaseId
    {
        HoHapLanh,
        GheKySinh,
        DichTaHeo,
        SotBun,
        SayNang,
        RoiLoanSinhSan,
        NamPhatQuang,
        LoiTam,
        HuMach
    }

    public enum DiseaseGroup
    {
        A,
        B,
        C,
        D
    }

    public enum FarmDrug
    {
        HaNhiet,
        TriGhe,
        AnThan
    }

    /// <summary>
    /// Mười mối trong GDD 6.4: chín bệnh trên heo và trạng thái Chuồng Nhiễm của cả khu.
    /// Ba thuốc là hạ nhiệt, trị ghẻ, an thần. Vắc-xin chỉ nhận trên heo chưa nhiễm.
    /// </summary>
    public static class DiseaseBoard
    {
        public const int IsolationCapacity = 4;
        public const int VaccineDays = 3;

        public static DiseaseGroup GroupOf(DiseaseId id)
        {
            if (id == DiseaseId.HoHapLanh || id == DiseaseId.GheKySinh || id == DiseaseId.DichTaHeo)
            {
                return DiseaseGroup.A;
            }

            if (id == DiseaseId.SotBun)
            {
                return DiseaseGroup.B;
            }

            if (id == DiseaseId.SayNang || id == DiseaseId.RoiLoanSinhSan)
            {
                return DiseaseGroup.C;
            }

            return DiseaseGroup.D;
        }

        public static float HabitatDiseaseFactor(int habitat)
        {
            if (habitat >= 90) return 0.60f;
            if (habitat >= 70) return 0.80f;
            if (habitat >= 50) return 1.00f;
            if (habitat >= 30) return 1.50f;
            return 2.30f;
        }

        public static float OutbreakChance(int habitat, float densityFactor, float weatherFactor, float corpseFactor, float specialFactor)
        {
            float raw = 0.01f * HabitatDiseaseFactor(habitat) * densityFactor * weatherFactor * corpseFactor * specialFactor;
            if (raw < 0.002f) return 0.002f;
            if (raw > 0.35f) return 0.35f;
            return raw;
        }

        public static float WeatherSpread(DiseaseId id, WeatherType weather, bool sheltered)
        {
            if (id == DiseaseId.HoHapLanh && !sheltered && (weather == WeatherType.Mua || weather == WeatherType.RetDam))
            {
                return 1.15f;
            }

            if (id == DiseaseId.GheKySinh && weather == WeatherType.NomAm)
            {
                return 1.40f;
            }

            if (weather == WeatherType.MuaBucXa && GroupOf(id) == DiseaseGroup.D)
            {
                return 1.50f;
            }

            if (id == DiseaseId.SayNang && weather == WeatherType.NangGat)
            {
                return 1.15f;
            }

            return 1f;
        }

        public static bool IsContaminatedPen(float density, int habitat, bool corpseOrWaste)
        {
            return density > 1.20f && habitat < 40 && corpseOrWaste;
        }

        public static bool Alerts(bool groupAbActive, float outbreakChance)
        {
            return groupAbActive || outbreakChance >= 0.05f;
        }

        public static bool TryInfect(HerdPig pig, DiseaseId id)
        {
            if (!pig.Alive || pig.ImmuneDays > 0 || pig.Disease != null)
            {
                return false;
            }

            pig.Disease = id;
            if (id == DiseaseId.LoiTam)
            {
                pig.SeizurePending = true;
            }

            return true;
        }

        public static bool TryImmunize(HerdPig pig)
        {
            if (!pig.Alive || pig.Disease != null)
            {
                return false;
            }

            pig.ImmuneDays = VaccineDays;
            return true;
        }

        public static bool ApplyDrug(HerdPig pig, FarmDrug drug, bool beddingReplaced)
        {
            if (!pig.Alive || pig.Disease == null)
            {
                return false;
            }

            DiseaseId id = pig.Disease.Value;
            if (id == DiseaseId.HuMach)
            {
                return false;
            }

            if (drug == FarmDrug.HaNhiet && id == DiseaseId.SayNang)
            {
                pig.Disease = null;
                pig.Health = Math.Min(100, pig.Health + 15);
                return true;
            }

            if (drug == FarmDrug.TriGhe && id == DiseaseId.GheKySinh)
            {
                if (!beddingReplaced)
                {
                    return false;
                }

                pig.Disease = null;
                pig.Health = Math.Min(100, pig.Health + 10);
                return true;
            }

            if (drug == FarmDrug.AnThan && id == DiseaseId.LoiTam)
            {
                pig.Disease = null;
                pig.SeizurePending = false;
                return true;
            }

            return false;
        }
    }

    public sealed class IsolationPen
    {
        private readonly HerdPig[] beds = new HerdPig[DiseaseBoard.IsolationCapacity];
        private int count;

        public int Count => count;
        public int Capacity => DiseaseBoard.IsolationCapacity;

        public bool TryAdmit(HerdPig pig)
        {
            if (pig.Isolated || count >= beds.Length)
            {
                return false;
            }

            beds[count] = pig;
            count += 1;
            pig.Isolated = true;
            return true;
        }
    }
}
