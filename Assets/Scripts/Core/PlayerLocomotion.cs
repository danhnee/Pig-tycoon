namespace PigTycoon.Core
{
    /// <summary>
    /// Tốc độ gốc GDD 3.6 và hao thể lực khi chạy GDD 9.2.
    /// Đi bộ không nằm trong bảng tiêu hao theo giây.
    /// </summary>
    public static class MovementSpec
    {
        public const float WalkMetersPerSecond = 4.5f;
        public const float RunMetersPerSecond = 7f;
        public const float AdultPigWalkMetersPerSecond = 1.5f;
        public const float AdultPigPanicMetersPerSecond = 5f;
        public const float RunStaminaPerSecond = 1.3f;
        public const float RunStaminaPerSecondHighAgi = 1.0f;
        public const int HighAgiRunThreshold = 30;
        public const float RunDailyLoadFraction = 0.20f;
    }

    public readonly struct LocomotionStep
    {
        public float MetersPerSecond { get; }
        public float StaminaDelta { get; }
        public float DailyLoadDelta { get; }
        public bool IsRunning { get; }

        public LocomotionStep(float metersPerSecond, float staminaDelta, float dailyLoadDelta, bool isRunning)
        {
            MetersPerSecond = metersPerSecond;
            StaminaDelta = staminaDelta;
            DailyLoadDelta = dailyLoadDelta;
            IsRunning = isRunning;
        }
    }

    public static class PlayerLocomotion
    {
        public static LocomotionStep Advance(bool wantsRun, int baseAgi, float currentStamina, float deltaSeconds)
        {
            if (deltaSeconds < 0f)
            {
                deltaSeconds = 0f;
            }

            if (!wantsRun || currentStamina <= 0f)
            {
                return new LocomotionStep(MovementSpec.WalkMetersPerSecond, 0f, 0f, false);
            }

            float rate = baseAgi >= MovementSpec.HighAgiRunThreshold
                ? MovementSpec.RunStaminaPerSecondHighAgi
                : MovementSpec.RunStaminaPerSecond;
            float staminaCost = rate * deltaSeconds;
            if (staminaCost > currentStamina)
            {
                staminaCost = currentStamina;
            }

            float dailyLoad = staminaCost * MovementSpec.RunDailyLoadFraction;
            return new LocomotionStep(MovementSpec.RunMetersPerSecond, -staminaCost, dailyLoad, true);
        }
    }
}
