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
    }
}
