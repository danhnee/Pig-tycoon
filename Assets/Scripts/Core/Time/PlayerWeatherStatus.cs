using System;
using System.Collections.Generic;

namespace PigTycoon.Core
{
    /// <summary>Trạng thái thời tiết lên người khi ở ngoài trời (GDD 7.2, cột "Trạng thái lên người").</summary>
    public enum PlayerWeatherStatusId
    {
        CamNang,   // Nắng Gắt
        UotLanh,   // Mưa
        CamGio,    // Giông Bão
        LacHuong,  // Sương Mù
        CongTay,   // Rét Đậm
        KhoRat,    // Gió Khô
        NhiemXa,   // Mưa Bức Xạ
    }

    /// <summary>Phần được lưu của <see cref="PlayerWeatherStatus"/> (GameSaveData.PlayerWeatherStatus; null trong save cũ).</summary>
    [Serializable]
    public class PlayerWeatherStatusSnapshot
    {
        public bool CamNang;
        public int SunExposureHours;
        public int ShadeHours;
        public bool UotLanh;
        public int RainExposureHours;
        public int UotLanhHoursLeft;
        public int StormOutdoorHours;
        public int StormCountDay = -1;
        public int CamGioDay = -1;
        public int NhiemXaStacks;
    }

    /// <summary>Một lần bị sét đánh, chờ runtime áp choáng và báo người chơi.</summary>
    public readonly struct LightningStrike
    {
        public int HpLost { get; }
        public float StunSeconds { get; }
        public LightningStrike(int hpLost, float stunSeconds) { HpLost = hpLost; StunSeconds = stunSeconds; }
    }

    /// <summary>
    /// Trạng thái thời tiết lên người chơi (GDD 7.2) và trang bị phòng tránh (GDD 12.3), tính mỗi giờ game từ
    /// <c>GameEngine.OnHourlyTick</c>. Runtime báo <see cref="IsOutdoors"/> và <see cref="IsFarFromRoof"/>; Core không thấy cảnh.
    /// Số không có trong GDD: ADR-0022 (PROVISIONAL).
    /// </summary>
    public sealed class PlayerWeatherStatus
    {
        // ---------------------------------------------------------------- số GDD 7.2 / 9.3
        public const int CamNangExposureHours = 2;          // 7.2: sau 2 giờ liên tục ngoài trời
        public const int CamNangWindowStartHour = 10;       // 7.2: 10:00–16:00
        public const int CamNangWindowEndHour = 16;
        public const double CamNangChance = 0.20;           // 7.2: 20% dính
        public const float CamNangMaxStaminaPenalty = 0.10f; // 7.2 / 9.3: Stamina tối đa −10%
        public const int CamNangShadeHoursToClear = 1;      // 7.2: nghỉ bóng mát 1 giờ
        public const int UotLanhExposureHours = 1;          // 7.2: sau 1 giờ ngoài trời
        public const float UotLanhImmunityDrainMultiplier = 1.5f; // 7.2: Miễn dịch hao ×1,5
        public const int UotLanhLingerHours = 3;            // 7.2: kéo dài 3 giờ sau khi vào nhà
        public const double LightningChancePerHour = 0.03;  // 7.2: 3% mỗi giờ
        public const float LightningRoofDistanceMeters = 15f; // 7.2: xa mái che > 15 m
        public const int LightningHpLoss = 40;              // 7.2: HP −40
        public const float LightningStunSeconds = 3f;       // 7.2: choáng 3 giây
        public const double InsulatedLightningFactor = 0.20; // 12.3 Vòng Cách Điện: −80% sét
        public const int CamGioEveryOutdoorHours = 2;       // 7.2: mỗi 2 giờ ngoài trời
        public const double CamGioChance = 0.10;            // 7.2: 10% dính
        public const int CamGioImmunityLoss = 10;           // 7.2: Miễn dịch −10
        public const float LacHuongMinimapRadiusMeters = 15f; // 7.2: không hiện gì ngoài 15 m
        public const float CongTayTaskTimeMultiplier = 1.15f; // 7.2: thời gian thao tác +15%
        public const float KhoRatSprintStaminaPerSecond = 0.2f; // 7.2: chạy nhanh +0,2 Stamina/giây
        public const int NhiemXaMaxStacks = 5;              // 7.2: tối đa 5 tầng
        public const int NhiemXaImmunityPerStack = 4;       // 7.2: mỗi tầng Miễn dịch −4
        public const int NhiemXaHeavyStacks = 3;            // 7.2: ≥ 3 tầng HP hồi −50%
        public const float NhiemXaHpRegenMultiplier = 0.5f;
        // 9.3 "Trạng thái thời tiết": Ướt Lạnh +5% / Cảm Gió +10% / Cảm Nắng trần −10% / Nhiễm Xạ ≥ 3 +10%
        public const float UotLanhStaminaFactor = 0.05f;
        public const float CamGioStaminaFactor = 0.10f;
        public const float NhiemXaStaminaFactor = 0.10f;

        // ---------------------------------------------------------------- mã cơ chế trang bị (Character.cs AllEquipmentItems)
        public const string RaincoatMechanic = "MienUotLanh";    // Áo Mưa Dầu
        public const string LeadCoatMechanic = "MienNhiemXa";    // Áo Chì
        public const string QuiltedPantsMechanic = "MienCongTay"; // Quần Da Lót Bông
        public const string InsulatorMechanic = "CachDien";      // Vòng Cách Điện
        public const string StormLampMechanic = "DenBao";        // Mặt Dây Đèn Bão

        /// <summary>Runtime báo mỗi khung hình. ADR-0022: ngoài trời trừ khi đứng ở cửa/tương tác một công trình hoặc ở cảnh nội thất.</summary>
        public bool IsOutdoors { get; set; } = true;
        /// <summary>Runtime báo: cách công trình (mái che) gần nhất &gt; 15 m. Không biết thì coi là xa (ADR-0022).</summary>
        public bool IsFarFromRoof { get; set; } = true;

        PlayerWeatherStatusSnapshot s = new PlayerWeatherStatusSnapshot();
        readonly Random rng;
        readonly Queue<LightningStrike> pendingStrikes = new Queue<LightningStrike>();
        readonly Queue<string> pendingMessages = new Queue<string>();

        public PlayerWeatherStatus(int seed) { rng = new Random(seed ^ 0x5EA7_0722); }

        public bool CamNang => s.CamNang;
        public bool UotLanh => s.UotLanh;
        public int NhiemXaStacks => s.NhiemXaStacks;
        public bool HasCamGio(GameEngine engine) => s.CamGioDay >= 0 && s.CamGioDay == engine.Clock.CurrentDay;

        // ---------------------------------------------------------------- trạng thái tức thời (theo thời tiết + ngoài trời + trang bị)
        public bool HasCongTay(GameEngine engine) =>
            IsOutdoors && engine.CurrentWeather == WeatherType.RetDam && !MartialProgression.Wears(engine.Character, QuiltedPantsMechanic);
        public bool HasKhoRat(GameEngine engine) => IsOutdoors && engine.CurrentWeather == WeatherType.GioKho;
        public bool HasLacHuong(GameEngine engine) =>
            IsOutdoors && engine.CurrentWeather == WeatherType.SuongMu && !MartialProgression.Wears(engine.Character, StormLampMechanic);

        public bool IsActive(GameEngine engine, PlayerWeatherStatusId id) => id switch
        {
            PlayerWeatherStatusId.CamNang => s.CamNang,
            PlayerWeatherStatusId.UotLanh => s.UotLanh,
            PlayerWeatherStatusId.CamGio => HasCamGio(engine),
            PlayerWeatherStatusId.LacHuong => HasLacHuong(engine),
            PlayerWeatherStatusId.CongTay => HasCongTay(engine),
            PlayerWeatherStatusId.KhoRat => HasKhoRat(engine),
            PlayerWeatherStatusId.NhiemXa => s.NhiemXaStacks > 0,
            _ => false
        };

        public List<PlayerWeatherStatusId> ActiveStatuses(GameEngine engine)
        {
            var list = new List<PlayerWeatherStatusId>();
            foreach (PlayerWeatherStatusId id in Enum.GetValues(typeof(PlayerWeatherStatusId)))
                if (IsActive(engine, id)) list.Add(id);
            return list;
        }

        public static string DisplayName(PlayerWeatherStatusId id) => id switch
        {
            PlayerWeatherStatusId.CamNang => "Cảm Nắng",
            PlayerWeatherStatusId.UotLanh => "Ướt Lạnh",
            PlayerWeatherStatusId.CamGio => "Cảm Gió",
            PlayerWeatherStatusId.LacHuong => "Lạc Hướng",
            PlayerWeatherStatusId.CongTay => "Cóng Tay",
            PlayerWeatherStatusId.KhoRat => "Khô Rát",
            PlayerWeatherStatusId.NhiemXa => "Nhiễm Xạ",
            _ => id.ToString()
        };

        // ---------------------------------------------------------------- hệ số cho các hệ khác
        /// <summary>GDD 9.3 "Trạng thái thời tiết" cộng vào ΣSức khỏe (Cảm Nắng là trần, không cộng ở đây).</summary>
        public float StaminaFactorSum(GameEngine engine)
        {
            float sum = 0f;
            if (s.UotLanh) sum += UotLanhStaminaFactor;
            if (HasCamGio(engine)) sum += CamGioStaminaFactor;
            if (s.NhiemXaStacks >= NhiemXaHeavyStacks) sum += NhiemXaStaminaFactor;
            return sum;
        }

        /// <summary>Cảm Nắng: trần Thể lực hiện tại = trần hiệu dụng − 10% Stamina tối đa (ADR-0022).</summary>
        public float StaminaCap(StaminaSystem stamina)
        {
            if (!s.CamNang) return stamina.EffectiveMaxStamina;
            return Math.Max(0f, stamina.EffectiveMaxStamina - CamNangMaxStaminaPenalty * stamina.BaseMaxStamina);
        }

        /// <summary>Kẹp Thể lực hiện tại dưới trần Cảm Nắng. Runtime gọi sau mỗi lần hồi; Core gọi mỗi giờ.</summary>
        public void EnforceStaminaCap(StaminaSystem stamina)
        {
            float cap = StaminaCap(stamina);
            if (stamina.CurrentStamina > cap) stamina.CurrentStamina = cap;
        }

        public float ImmunityDrainMultiplier => s.UotLanh ? UotLanhImmunityDrainMultiplier : 1f;
        public float TaskTimeMultiplier(GameEngine engine) => HasCongTay(engine) ? CongTayTaskTimeMultiplier : 1f;
        public float ExtraSprintStaminaPerSecond(GameEngine engine) => HasKhoRat(engine) ? KhoRatSprintStaminaPerSecond : 0f;
        public float HpRegenMultiplier => s.NhiemXaStacks >= NhiemXaHeavyStacks ? NhiemXaHpRegenMultiplier : 1f;
        /// <summary>Bán kính bản đồ nhỏ khi Lạc Hướng; null = không giới hạn.</summary>
        public float? MinimapRadiusMeters(GameEngine engine) => HasLacHuong(engine) ? LacHuongMinimapRadiusMeters : (float?)null;

        /// <summary>Mất Miễn dịch do thời tiết; Ướt Lạnh nhân ×1,5. Trả về lượng thực mất.</summary>
        public int ApplyImmunityLoss(CharacterData c, int amount)
        {
            if (amount <= 0) return 0;
            int loss = (int)Math.Round(amount * ImmunityDrainMultiplier, MidpointRounding.AwayFromZero);
            int before = c.Stamina.Immunity;
            c.Stamina.Immunity = Math.Max(0, before - loss);
            return before - c.Stamina.Immunity;
        }

        // ---------------------------------------------------------------- hàng đợi cho runtime
        public bool TryTakeLightningStrike(out LightningStrike strike)
        {
            if (pendingStrikes.Count > 0) { strike = pendingStrikes.Dequeue(); return true; }
            strike = default;
            return false;
        }

        public bool TryTakeMessage(out string message)
        {
            if (pendingMessages.Count > 0) { message = pendingMessages.Dequeue(); return true; }
            message = string.Empty;
            return false;
        }

        // ---------------------------------------------------------------- mô phỏng mỗi giờ game
        /// <summary>
        /// Một giờ game vừa trôi qua; <paramref name="hourReached"/> là giờ vừa tới (giờ đã qua = hourReached − 1).
        /// <paramref name="roll"/> thay RNG riêng (test).
        /// </summary>
        public void TickHour(GameEngine engine, int hourReached, Func<double>? roll = null)
        {
            if (engine == null) throw new ArgumentNullException(nameof(engine));
            var c = engine.Character;
            if (c == null) return;
            Func<double> r = roll ?? (() => rng.NextDouble());
            var w = engine.CurrentWeather;
            bool outside = IsOutdoors;
            int hourStarted = (hourReached + 23) % 24;

            // Cảm Nắng: 2 giờ liên tục ngoài trời trong 10:00–16:00, 20% dính; hết khi nghỉ bóng mát (trong nhà) 1 giờ.
            bool sunHour = outside && w == WeatherType.NangGat &&
                           hourStarted >= CamNangWindowStartHour && hourStarted < CamNangWindowEndHour;
            if (sunHour)
            {
                s.SunExposureHours++;
                s.ShadeHours = 0;
                if (!s.CamNang && s.SunExposureHours % CamNangExposureHours == 0 && r() < CamNangChance)
                {
                    s.CamNang = true;
                    pendingMessages.Enqueue("Cảm Nắng: Thể lực tối đa −10% đến khi nghỉ bóng mát 1 giờ.");
                }
            }
            else
            {
                s.SunExposureHours = 0;
                if (!outside && s.CamNang && ++s.ShadeHours >= CamNangShadeHoursToClear)
                {
                    s.CamNang = false;
                    s.ShadeHours = 0;
                    pendingMessages.Enqueue("Đã nghỉ mát đủ 1 giờ: hết Cảm Nắng.");
                }
            }

            // Ướt Lạnh: sau 1 giờ ngoài trời dưới Mưa (không mặc Áo Mưa Dầu); kéo dài 3 giờ sau khi vào nhà.
            bool wetHour = outside && w == WeatherType.Mua && !MartialProgression.Wears(c, RaincoatMechanic);
            if (wetHour)
            {
                s.RainExposureHours++;
                if (s.RainExposureHours >= UotLanhExposureHours)
                {
                    if (!s.UotLanh) pendingMessages.Enqueue("Ướt Lạnh: Miễn dịch hao ×1,5 (kéo dài 3 giờ sau khi vào nhà).");
                    s.UotLanh = true;
                    s.UotLanhHoursLeft = UotLanhLingerHours;
                }
            }
            else
            {
                s.RainExposureHours = 0;
                if (s.UotLanh && --s.UotLanhHoursLeft <= 0)
                {
                    s.UotLanh = false;
                    s.UotLanhHoursLeft = 0;
                    pendingMessages.Enqueue("Đã khô người: hết Ướt Lạnh.");
                }
            }

            // Giông Bão: Sét Đánh (xa mái che > 15 m) và Cảm Gió (mỗi 2 giờ ngoài trời).
            int today = engine.Clock.CurrentDay;
            if (s.StormCountDay != today) { s.StormCountDay = today; s.StormOutdoorHours = 0; }
            if (outside && w == WeatherType.GiongBao)
            {
                bool insulated = MartialProgression.Wears(c, InsulatorMechanic);
                if (IsFarFromRoof)
                {
                    double chance = LightningChancePerHour * (insulated ? InsulatedLightningFactor : 1.0);
                    if (r() < chance) StrikeByLightning(c, insulated);
                }

                s.StormOutdoorHours++;
                if (!HasCamGio(engine) && s.StormOutdoorHours % CamGioEveryOutdoorHours == 0 && r() < CamGioChance)
                {
                    s.CamGioDay = today;
                    int lost = ApplyImmunityLoss(c, CamGioImmunityLoss);
                    pendingMessages.Enqueue($"Cảm Gió: Miễn dịch −{lost}, Thể lực tiêu hao +10% suốt ngày.");
                }
            }

            // Nhiễm Xạ: mỗi giờ ngoài trời dưới Mưa Bức Xạ +1 tầng (tối đa 5), mỗi tầng Miễn dịch −4; Áo Chì miễn.
            bool radHour = outside && w == WeatherType.MuaBucXa && !MartialProgression.Wears(c, LeadCoatMechanic);
            if (radHour)
            {
                if (s.NhiemXaStacks < NhiemXaMaxStacks)
                {
                    s.NhiemXaStacks++;
                    int lost = ApplyImmunityLoss(c, NhiemXaImmunityPerStack);
                    pendingMessages.Enqueue($"Nhiễm Xạ tầng {s.NhiemXaStacks}/{NhiemXaMaxStacks}: Miễn dịch −{lost}" +
                                            (s.NhiemXaStacks >= NhiemXaHeavyStacks ? ", Chống Chịu hồi −50%." : "."));
                }
            }
            else if (s.NhiemXaStacks > 0)
            {
                s.NhiemXaStacks--; // ADR-0022: giảm 1 tầng mỗi giờ không bị phơi xạ
            }

            EnforceStaminaCap(c.Stamina);
        }

        void StrikeByLightning(CharacterData c, bool insulated)
        {
            // Không có HP (thiết kế người dùng): sét trừ vào thanh Chống Chịu; ADR-0022: sét không làm ngất
            float beforeGuard = c.CurrentGuard;
            c.CurrentGuard = Math.Max(Math.Min(1f, beforeGuard), beforeGuard - LightningHpLoss);
            int lost = (int)Math.Round(beforeGuard - c.CurrentGuard);
            c.SyncLowGuardStamina();
            float stun = insulated ? 0f : LightningStunSeconds;
            pendingStrikes.Enqueue(new LightningStrike(lost, stun));
            pendingMessages.Enqueue(insulated
                ? $"Sét đánh! −{lost} Chống Chịu (Vòng Cách Điện: miễn choáng)."
                : $"Sét đánh! −{lost} Chống Chịu, choáng {LightningStunSeconds:0} giây.");
        }

        // ---------------------------------------------------------------- lưu / nạp
        public PlayerWeatherStatusSnapshot Capture() => new PlayerWeatherStatusSnapshot
        {
            CamNang = s.CamNang,
            SunExposureHours = s.SunExposureHours,
            ShadeHours = s.ShadeHours,
            UotLanh = s.UotLanh,
            RainExposureHours = s.RainExposureHours,
            UotLanhHoursLeft = s.UotLanhHoursLeft,
            StormOutdoorHours = s.StormOutdoorHours,
            StormCountDay = s.StormCountDay,
            CamGioDay = s.CamGioDay,
            NhiemXaStacks = s.NhiemXaStacks,
        };

        /// <summary>null (save cũ) = không có trạng thái nào.</summary>
        public void Restore(PlayerWeatherStatusSnapshot? data)
        {
            s = new PlayerWeatherStatusSnapshot();
            pendingStrikes.Clear();
            pendingMessages.Clear();
            if (data == null) return;
            s.CamNang = data.CamNang;
            s.SunExposureHours = Math.Max(0, data.SunExposureHours);
            s.ShadeHours = Math.Max(0, data.ShadeHours);
            s.UotLanh = data.UotLanh;
            s.RainExposureHours = Math.Max(0, data.RainExposureHours);
            s.UotLanhHoursLeft = Math.Clamp(data.UotLanhHoursLeft, 0, UotLanhLingerHours);
            s.StormOutdoorHours = Math.Max(0, data.StormOutdoorHours);
            s.StormCountDay = data.StormCountDay;
            s.CamGioDay = data.CamGioDay;
            s.NhiemXaStacks = Math.Clamp(data.NhiemXaStacks, 0, NhiemXaMaxStacks);
        }
    }
}
