using System;

namespace PigTycoon.Core
{
    /// <summary>Manual care shares the existing daily ration calculation; no price/growth formula changes.</summary>
    public static class PigCareCommands
    {
        public static float DailyFeedKilograms(GameEngine engine, Pig pig)
        {
            bool hearty = pig.Traits.Exists(t => (t.Id == "HauAn" || t.Id == "AnKhoe") && !t.IsSealed);
            float kg = GeneticsAndBreedingSystem.GetDailyFeedKilograms(pig.Stage, pig.GeneLine, hearty);
            if (pig.Traits.Exists(t => t.Id == "NhipTimCham" || t.Id == "CongSinh")) kg *= 0.75f;
            if (pig.GeneLine == GeneLineId.LoiMach && engine.CurrentWeather == WeatherType.GiongBao) kg *= 1.10f;
            if (pig.GeneLine == GeneLineId.HongDien && !pig.IsIsolated &&
                engine.CurrentWeather != WeatherType.GioKho && engine.CurrentWeather != WeatherType.SuongMu) kg *= 0.75f;
            return kg;
        }

        // GDD 9.2 feed4/water6, 100% daily load. GDD9.3 environment/health factors available in Core.
        public static float CareStaminaCost(GameEngine engine, float baseCost, bool lit = false)
        {
            float extra = 0f;
            bool fixedWeather = engine.CurrentWeather == WeatherType.NangGat || engine.CurrentWeather == WeatherType.Mua ||
                engine.CurrentWeather == WeatherType.GiongBao || engine.CurrentWeather == WeatherType.MuaBucXa;
            if (!fixedWeather)
            {
                switch (engine.Clock.GetTimeOfDay())
                {
                    case TimeOfDay.Sang: extra -= 0.05f; break;
                    case TimeOfDay.Trua: extra += 0.10f; break;
                    case TimeOfDay.Toi: if (!lit) extra += 0.05f; break;
                }
            }
            switch (engine.CurrentWeather)
            {
                case WeatherType.NangGat: extra += 0.15f; break;
                case WeatherType.Mua: case WeatherType.RetDam: extra += 0.10f; break;
                case WeatherType.GiongBao: extra += 0.20f; break;
                case WeatherType.NomAm: extra += 0.05f; break;
                case WeatherType.NhieuMay: extra -= 0.03f; break;
            }
            int condition = engine.Character.Stamina.Condition;
            extra += condition >= 80 ? 0f : condition >= 60 ? 0.05f : condition >= 40 ? 0.10f : condition >= 20 ? 0.20f : 0.35f;
            return baseCost * Math.Clamp(1f + extra, 0.60f, 2f);
        }

        public static (bool success, float kilograms, string message) TryFeedPig(
            GameEngine engine, string pigId, float playerX, float playerY, float reachMeters = 2.5f)
        {
            var pig = engine.Pigs.Find(p => p.Id == pigId && p.IsAlive);
            if (pig == null) return (false, 0f, "Không tìm thấy heo còn sống.");
            float dx = pig.PositionX - playerX, dy = pig.PositionY - playerY;
            if (float.IsNaN(dx) || float.IsInfinity(dx) || float.IsNaN(dy) || float.IsInfinity(dy) ||
                float.IsNaN(reachMeters) || float.IsInfinity(reachMeters) || reachMeters <= 0f || dx * dx + dy * dy > reachMeters * reachMeters)
                return (false, 0f, "Đến gần heo để cho ăn.");
            if (pig.IsIsolated || pig.Mood < 20) return (false, 0f, "Heo cách ly hoặc hoảng loạn không ăn ở máng đàn.");
            if (pig.LastPaidRationDay >= engine.Clock.CurrentDay) return (false, 0f, "Heo đã ăn đủ khẩu phần hôm nay.");
            float kg = DailyFeedKilograms(engine, pig);
            if (engine.Farm.FeedStockKg < kg) return (false, 0f, "Không đủ cám trong kho.");
            float work = CareStaminaCost(engine, 4f);
            if (!engine.Character.Stamina.TryConsumeStamina(work, 1f)) return (false, 0f, "Không đủ thể lực rải cám.");
            engine.Farm.FeedStockKg -= kg;
            pig.LastPaidRationDay = engine.Clock.CurrentDay;
            pig.Hunger = 0; pig.FedToday = true;
            engine.Ecosystem.RecordTroughFeeding(pig);
            return (true, kg, $"Đã cho {pig.Name} ăn {kg:0.00} kg cám.");
        }

        public static void FeedDailyRollup(GameEngine engine, Pig pig)
        {
            // Tick starts a new day before rolling up the day just finished. A manually paid ration
            // belongs to that completed day and must survive save/load without another warehouse debit.
            int completedDay = engine.Clock.CurrentDay - 1;
            if (pig.LastPaidRationDay >= completedDay && pig.LastPaidRationDay >= 0)
            { pig.FedToday = true; pig.Hunger = 0; return; }
            if (engine.InteractiveHerdOwned)
            { pig.FedToday = false; pig.Hunger = Math.Min(100, pig.Hunger + 35); return; }
            float kg = DailyFeedKilograms(engine, pig);
            if (engine.Farm.FeedStockKg >= kg)
            {
                engine.Farm.FeedStockKg -= kg; pig.LastPaidRationDay = completedDay;
                pig.FedToday = true; pig.Hunger = 0;
            }
            else { pig.FedToday = false; pig.Hunger = Math.Min(100, pig.Hunger + 35); }
        }
    }
}
