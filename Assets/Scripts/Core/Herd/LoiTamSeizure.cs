using System;

namespace PigTycoon.Core
{
    /// <summary>
    /// GDD 6.4 Lôi Tâm: "khi Tinh thần &lt; 30, heo run giật 10 giây (dấu hiệu nhìn thấy), sau đó lên cơn: chạy điên cuồng làm
    /// heo quanh 8 m Hoảng sợ, rồi 25% đột tử. Trong 10 giây báo trước, dùng thuốc an thần hoặc lùa vào mái trú để dập cơn."
    /// 3 Nov audit: SeizurePending was set but nothing ever read it, so the fit never happened.
    /// Flow: the runtime asks <see cref="ShouldWarn"/>, calls <see cref="BeginWarning"/> (pig twitches for 10 s), then
    /// <see cref="Resolve"/>. Thuốc An Thần Heo during the warning clears <see cref="Pig.SeizurePending"/> (ItemUse).
    /// </summary>
    public static class LoiTamSeizure
    {
        public const int MoodThreshold = 30;
        public const float WarningSeconds = 10f;
        public const float PanicRadiusMeters = 8f;
        public const double SuddenDeathChance = 0.25;
        /// <summary>"Hoảng sợ" is the 0–19 mood band (GDD 5.2 table).</summary>
        public const int PanicMood = 19;
        /// <summary>TẠM (C-14): at most one fit per pig per 6 game hours, so a pig below 30 does not fit back-to-back.</summary>
        public const int CooldownMinutes = 360;

        public static bool ShouldWarn(GameEngine e, Pig p) =>
            p.IsAlive && p.ActiveDisease == DiseaseId.LoiTam && p.Mood < MoodThreshold && !p.SeizurePending
            && (p.LastSeizureMinute < 0 || e.Clock.TotalElapsedGameMinutes - p.LastSeizureMinute >= CooldownMinutes);

        public static string BeginWarning(Pig p)
        {
            p.SeizurePending = true;
            return $"[{p.Name}] run giật: Lôi Tâm sắp lên cơn! 10 giây để cho uống Thuốc An Thần hoặc lùa vào mái trú.";
        }

        /// <summary>End of the 10 s warning. <paramref name="sheltered"/>: the pig is under a roof now.</summary>
        public static (bool fit, bool died, int panicked, string message) Resolve(GameEngine e, Pig p, bool sheltered, double roll)
        {
            if (!p.IsAlive) return (false, false, 0, "");
            if (!p.SeizurePending) return (false, false, 0, $"[{p.Name}] đã dịu, không lên cơn.");
            p.SeizurePending = false;
            p.LastSeizureMinute = e.Clock.TotalElapsedGameMinutes;
            if (sheltered) return (false, false, 0, $"[{p.Name}] được lùa vào mái trú kịp, cơn Lôi Tâm tắt.");

            int panicked = 0;
            float r2 = PanicRadiusMeters * PanicRadiusMeters;
            foreach (var o in e.Pigs)
            {
                if (o == p || !o.IsAlive) continue;
                float dx = o.PositionX - p.PositionX, dy = o.PositionY - p.PositionY;
                if (dx * dx + dy * dy > r2) continue;
                if (o.Mood > PanicMood) { o.Mood = PanicMood; panicked++; }
            }
            if (roll < SuddenDeathChance)
            {
                p.Die();
                e.EventBus.Publish(new PigDied(p.Id, "Lôi Tâm đột tử", e.Clock.CurrentDay));
                return (true, true, panicked, $"[{p.Name}] lên cơn Lôi Tâm và đột tử. {panicked} con quanh đó hoảng sợ.");
            }
            return (true, false, panicked, $"[{p.Name}] lên cơn Lôi Tâm, chạy điên cuồng; {panicked} con quanh đó hoảng sợ.");
        }
    }
}
