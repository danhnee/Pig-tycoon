using System;

namespace PigTycoon.Core
{
    public enum PigAct
    {
        Wander,
        SeekFood,
        Eat,
        Sleep,
        Panic,
        Isolated,
        Dead
    }

    public readonly struct CareBar
    {
        public string Id { get; }
        public int Value { get; }
        public string Tooltip { get; }

        public CareBar(string id, int value, string tooltip)
        {
            Id = id;
            Value = value;
            Tooltip = tooltip;
        }
    }

    /// <summary>
    /// Một nhịp 1 Hz của heo trên máng đàn và ô ngủ. Heo cách ly không ăn máng đàn. Heo hoảng không ăn.
    /// </summary>
    public sealed class HerdPig
    {
        public string Id = "";
        public bool Alive = true;
        public int Health = 100;
        public int Mood = 70;
        public int Comfort = 50;
        public int Hunger;
        public PigAct Act = PigAct.Wander;
        public bool Panic;
        public bool Isolated;
        public DiseaseId? Disease;
        public int ImmuneDays;
        public bool SeizurePending;
    }

    public sealed class HerdTick
    {
        public const int HungerPerTick = 10;
        public const int EatThreshold = 60;
        public const int EatRelief = 60;

        public int FeedPortions;
        public int SleepSlots;
        public int Hour;
        public int OccupiedSlots;

        public void Tick(HerdPig pig)
        {
            if (!pig.Alive)
            {
                pig.Act = PigAct.Dead;
                return;
            }

            if (pig.Isolated)
            {
                pig.Act = PigAct.Isolated;
                pig.Hunger = Math.Min(100, pig.Hunger + HungerPerTick);
                return;
            }

            if (pig.Panic || pig.Mood < 20)
            {
                pig.Panic = true;
                pig.Act = PigAct.Panic;
                pig.Hunger = Math.Min(100, pig.Hunger + HungerPerTick);
                return;
            }

            pig.Hunger = Math.Min(100, pig.Hunger + HungerPerTick);
            if (pig.Hunger >= EatThreshold)
            {
                if (FeedPortions > 0)
                {
                    FeedPortions -= 1;
                    pig.Hunger = Math.Max(0, pig.Hunger - EatRelief);
                    pig.Act = PigAct.Eat;
                    return;
                }

                pig.Act = PigAct.SeekFood;
                return;
            }

            bool night = Hour >= 21 || Hour < 6;
            if (night && OccupiedSlots < SleepSlots)
            {
                OccupiedSlots += 1;
                pig.Act = PigAct.Sleep;
                pig.Comfort = Math.Min(100, pig.Comfort + 8);
                pig.Mood = Math.Min(100, pig.Mood + 3);
                return;
            }

            pig.Act = PigAct.Wander;
        }
    }

    public static class CareBars
    {
        public static CareBar[] For(HerdPig pig)
        {
            return new[]
            {
                new CareBar("Health", Clamp(pig.Health), "Sức khỏe 0–100. Dưới 40 heo tăng trọng kém và dễ lây."),
                new CareBar("Comfort", Clamp(pig.Comfort), "Thoải mái. Ngủ đúng ô làm tăng. Heo cách ly hoặc đói không được cộng."),
                new CareBar("Happiness", Clamp(pig.Mood), "Tinh thần. 60–100 bình ổn, 40–59 bất an, 20–39 hoảng sợ, dưới 20 hoảng loạn.")
            };
        }

        private static int Clamp(int value)
        {
            if (value < 0) return 0;
            if (value > 100) return 100;
            return value;
        }
    }
}
