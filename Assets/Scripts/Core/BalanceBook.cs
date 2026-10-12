using System;
using System.Collections.Generic;
using System.Globalization;

namespace PigTycoon.Core
{
    public interface IBalance
    {
        float SecondsPerGameDay { get; }
        float SecondsPerGameHour { get; }
        int LandCapacityPerPlot { get; }
        int ShelterCapacityPerRoof { get; }
        int WaterCapacityPerBasin { get; }
        int FeedCapacityPerTrough { get; }
        int PlotUpkeepGold { get; }
        int RoofUpkeepGold { get; }
        int BasinUpkeepGold { get; }
        int TroughUpkeepGold { get; }
        int AnStr { get; }
        int AnAgi { get; }
        int AnCtrl { get; }
        int AnRes { get; }
        int KhoaStr { get; }
        int KhoaAgi { get; }
        int KhoaCtrl { get; }
        int KhoaRes { get; }
        float WalkMetersPerSecond { get; }
        float SprintMetersPerSecond { get; }
        float MainGateWidthMeters { get; }
        int DaoRuaLightDamage { get; }
        float LightStrikeStamina { get; }
        float NightMarketGoldRate { get; }
    }

    /// <summary>
    /// Sổ P0.08. Số lấy từ GameClock, Farm và roster. Parser chỉ dùng BCL để Unity netstandard biên dịch được.
    /// </summary>
    public sealed class BalanceBook : IBalance
    {
        public float SecondsPerGameDay { get; init; }
        public float SecondsPerGameHour { get; init; }
        public int LandCapacityPerPlot { get; init; }
        public int ShelterCapacityPerRoof { get; init; }
        public int WaterCapacityPerBasin { get; init; }
        public int FeedCapacityPerTrough { get; init; }
        public int PlotUpkeepGold { get; init; }
        public int RoofUpkeepGold { get; init; }
        public int BasinUpkeepGold { get; init; }
        public int TroughUpkeepGold { get; init; }
        public int AnStr { get; init; }
        public int AnAgi { get; init; }
        public int AnCtrl { get; init; }
        public int AnRes { get; init; }
        public int KhoaStr { get; init; }
        public int KhoaAgi { get; init; }
        public int KhoaCtrl { get; init; }
        public int KhoaRes { get; init; }
        public float WalkMetersPerSecond { get; init; }
        public float SprintMetersPerSecond { get; init; }
        public float MainGateWidthMeters { get; init; }
        public int DaoRuaLightDamage { get; init; }
        public float LightStrikeStamina { get; init; }
        public float NightMarketGoldRate { get; init; }

        public static BalanceBook Load(string json)
        {
            if (string.IsNullOrWhiteSpace(json))
            {
                throw new InvalidOperationException("Balance.json rỗng");
            }

            var values = ReadFlatNumbers(json);
            return new BalanceBook
            {
                SecondsPerGameDay = Req(values, "secondsPerGameDay"),
                SecondsPerGameHour = Req(values, "secondsPerGameHour"),
                LandCapacityPerPlot = ReqInt(values, "landCapacityPerPlot"),
                ShelterCapacityPerRoof = ReqInt(values, "shelterCapacityPerRoof"),
                WaterCapacityPerBasin = ReqInt(values, "waterCapacityPerBasin"),
                FeedCapacityPerTrough = ReqInt(values, "feedCapacityPerTrough"),
                PlotUpkeepGold = ReqInt(values, "plotUpkeepGold"),
                RoofUpkeepGold = ReqInt(values, "roofUpkeepGold"),
                BasinUpkeepGold = ReqInt(values, "basinUpkeepGold"),
                TroughUpkeepGold = ReqInt(values, "troughUpkeepGold"),
                AnStr = ReqInt(values, "anStr"),
                AnAgi = ReqInt(values, "anAgi"),
                AnCtrl = ReqInt(values, "anCtrl"),
                AnRes = ReqInt(values, "anRes"),
                KhoaStr = ReqInt(values, "khoaStr"),
                KhoaAgi = ReqInt(values, "khoaAgi"),
                KhoaCtrl = ReqInt(values, "khoaCtrl"),
                KhoaRes = ReqInt(values, "khoaRes"),
                WalkMetersPerSecond = Req(values, "walkMetersPerSecond"),
                SprintMetersPerSecond = Req(values, "sprintMetersPerSecond"),
                MainGateWidthMeters = Req(values, "mainGateWidthMeters"),
                DaoRuaLightDamage = ReqInt(values, "daoRuaLightDamage"),
                LightStrikeStamina = Req(values, "lightStrikeStamina"),
                NightMarketGoldRate = Req(values, "nightMarketGoldRate")
            };
        }

        static float Req(Dictionary<string, float> values, string key)
        {
            if (!values.TryGetValue(key, out float value))
            {
                throw new InvalidOperationException("Balance.json thiếu " + key);
            }

            return value;
        }

        static int ReqInt(Dictionary<string, float> values, string key)
        {
            float value = Req(values, key);
            int whole = (int)value;
            if (Math.Abs(value - whole) > 0.0001f)
            {
                throw new InvalidOperationException("Balance.json cần số nguyên ở " + key);
            }

            return whole;
        }

        static Dictionary<string, float> ReadFlatNumbers(string json)
        {
            var values = new Dictionary<string, float>(StringComparer.Ordinal);
            int i = 0;
            while (i < json.Length)
            {
                int quote = json.IndexOf('"', i);
                if (quote < 0)
                {
                    break;
                }

                int end = json.IndexOf('"', quote + 1);
                if (end < 0)
                {
                    throw new InvalidOperationException("Balance.json hỏng chuỗi");
                }

                string key = json.Substring(quote + 1, end - quote - 1);
                int colon = json.IndexOf(':', end + 1);
                if (colon < 0)
                {
                    throw new InvalidOperationException("Balance.json thiếu giá trị");
                }

                int start = colon + 1;
                while (start < json.Length && char.IsWhiteSpace(json[start]))
                {
                    start++;
                }

                int stop = start;
                while (stop < json.Length && (char.IsDigit(json[stop]) || json[stop] == '.' || json[stop] == '-'))
                {
                    stop++;
                }

                if (stop == start)
                {
                    throw new InvalidOperationException("Balance.json giá trị không phải số: " + key);
                }

                string raw = json.Substring(start, stop - start);
                if (!float.TryParse(raw, NumberStyles.Float, CultureInfo.InvariantCulture, out float value))
                {
                    throw new InvalidOperationException("Balance.json không đọc được " + key);
                }

                values[key] = value;
                i = stop;
            }

            return values;
        }
    }
}
