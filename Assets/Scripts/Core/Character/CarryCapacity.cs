namespace PigTycoon.Core
{
    /// <summary>
    /// Sức vác theo STR gốc. GDD 8.2 giao mang vác cho STR. GDD 9.2: trên 35 kg là vác nặng.
    /// Thưởng trang bị không nâng trần, cùng luật với chỉ số gốc.
    /// </summary>
    public static class CarryCapacity
    {
        public const float HeavyKilograms = 35f;

        public static float MaxKilograms(int baseStr)
        {
            if (baseStr < 0)
            {
                baseStr = 0;
            }

            return 15f + baseStr;
        }

        public static bool TryAdd(float carriedKilograms, int baseStr, float addKilograms, out float nextKilograms)
        {
            if (addKilograms <= 0f)
            {
                nextKilograms = carriedKilograms;
                return false;
            }

            float next = carriedKilograms + addKilograms;
            if (next > MaxKilograms(baseStr))
            {
                nextKilograms = carriedKilograms;
                return false;
            }

            nextKilograms = next;
            return true;
        }

        public static bool IsHeavy(float carriedKilograms)
        {
            return carriedKilograms > HeavyKilograms;
        }

        /// <summary>GDD 9.3 "Tải: mang vác > 35 kg +35 %"; GDD 9.2: with Thể trạng &lt; 40 another +15 %.
        /// (3 Nov: was ×1.25 and compared the int Thể trạng with "Met"/"KietSuc" strings, which never matched.)</summary>
        public static float StaminaMultiplier(float carriedKilograms, int condition = 100)
        {
            if (!IsHeavy(carriedKilograms)) return 1.0f;
            return 1.0f + 0.35f + (condition < 40 ? 0.15f : 0f);
        }
    }
}
