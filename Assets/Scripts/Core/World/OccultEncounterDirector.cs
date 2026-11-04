using System;
using System.Collections.Generic;

namespace PigTycoon.Core
{
    /// <summary>GDD24 daily occurrence and six-hour farm gate offers. Unknown seeded predicates never qualify.</summary>
    public sealed class OccultEncounterDirector
    {
        readonly OccultExpeditionSession session;
        public OccultEncounterDirector(OccultExpeditionSession session) { this.session = session; }
        public static long FarmMinute(GameClock clock) =>
            ((long)clock.CurrentDay - 1) * 1440 + ((clock.CurrentHour + 18) % 24) * 60 + clock.CurrentMinute;
        public bool HasOpenGate(GameEngine engine)
        {
            var state = session.DirectorState;
            long elapsed = FarmMinute(engine.Clock) - state.OfferedAtMinute;
            return !session.IsActive && state.HasGateOffer && elapsed >= 0 && elapsed < 360;
        }
        /// <summary>Chìa Cổng Ngược (GDD 12.8): an open gate stays <paramref name="minutes"/> longer.</summary>
        public bool ExtendOpenGate(GameEngine engine, int minutes)
        {
            if (!HasOpenGate(engine)) return false;
            session.DirectorState.OfferedAtMinute += minutes;
            return true;
        }

        public bool TryEnterOfferedGate(GameEngine engine)
        {
            if (engine.WorldLocations.CurrentLocationId != "Farm" || !HasOpenGate(engine)) return false;
            return session.TryBegin(session.DirectorState.OfferedEventId, engine.Clock.CurrentDay);
        }
        /// <summary>
        /// Recording/demo scenario only (never called from the play flow): opens a six-hour gate offer for a
        /// gate-type event now, under the same rules a rolled offer obeys (on the Farm, nothing active or open).
        /// Entering and leaving still go through TryEnterOfferedGate and the expedition exit rules.
        /// </summary>
        public bool TrySeedGateOfferForScenario(GameEngine engine, MysteryEventId eventId)
        {
            if (eventId != MysteryEventId.E07_GiamDinhVoDanh && eventId != MysteryEventId.E08_DoiMotLayMot) return false;
            if (session.IsActive || HasOpenGate(engine) || engine.WorldLocations.CurrentLocationId != "Farm") return false;
            var state = session.DirectorState;
            state.HasGateOffer = true;
            state.OfferedEventId = eventId;
            state.OfferedAtMinute = FarmMinute(engine.Clock);
            return true;
        }

        /// <summary>
        /// Recording/demo scenario only (never called from the play flow): records task-completion progress for the
        /// active seeded gate encounter so the Bua Hoi Huong exit (needs &gt;= 50 %) can be shown. The exit itself
        /// still requires a carried charm and goes through EvaluateExit / CompleteExit unchanged.
        /// </summary>
        /// <summary>
        /// Play flow: the player acts at the event site; Core resolves the running event (GDD 24 table, choice 0 = the
        /// event's main action, 1 = the alternative) and marks it complete, so the patched gate lets them home with full
        /// reward. One resolution per expedition.
        /// </summary>
        public (bool resolved, string message) ResolveActiveEvent(GameEngine engine, int choiceIndex)
        {
            if (!session.IsActive) return (false, "Không có sự kiện huyền bí đang diễn ra.");
            if (session.CompletionPercent >= 100) return (false, "Sự kiện đã xong: quay lại cổng chắp vá để về trại.");
            var res = engine.PetsAndNpcs.ResolveAnyMysteryEventGdd24(session.EventId, choiceIndex, engine.Farm, engine.Pigs);
            if (res.resolved) session.RecordProgress(100f);
            return res;
        }

        public bool TryRecordScenarioProgress(float completionPercent)
        {
            if (!session.IsActive || !session.IsGateEvent) return false;
            return session.RecordProgress(completionPercent);
        }

        public (bool offered, string message) TryOfferDay(GameEngine engine)
        {
            var state = session.DirectorState;
            if (state.LastRolledDay >= engine.Clock.CurrentDay) return (false, "Đã kiểm tra sự kiện hôm nay.");
            state.LastRolledDay = engine.Clock.CurrentDay;
            if (session.IsActive || HasOpenGate(engine) || engine.WorldLocations.CurrentLocationId != "Farm")
                return (false, "Không mở chồng sự kiện hoặc mở cổng ngoài đồng cỏ.");
            state.HasGateOffer = false;
            var random = new Random(unchecked(engine.BaseSeed ^ (engine.Clock.CurrentDay * 16777619)));
            double chance = Math.Clamp(.0025 + .0008 * engine.Farm.EventPressureIndex, .0025, .08);
            if (random.NextDouble() >= chance) return (false, "Không có sự kiện bất chợt hôm nay.");
            double color = random.NextDouble(); // Red45%, Yellow40%, Green15%; never reroll a missing color.
            var candidates = new List<MysteryEventId>();
            if (color < .45)
            {
                if (engine.Farm.EventPressureIndex >= 70) candidates.Add(MysteryEventId.E13_BaNgayKhoa);
            }
            else if (color < .85)
            {
                if (engine.Pigs.Exists(p => p.IsAlive && !p.IsIdentified && p.Rarity >= GeneRarity.Quy))
                    candidates.Add(MysteryEventId.E07_GiamDinhVoDanh);
                if (engine.Pigs.Exists(p => p.IsAlive && p.Stage == PigStage.HeoNon && p.Generation >= 2))
                    candidates.Add(MysteryEventId.E08_DoiMotLayMot);
                if (engine.DeepMechanics.MidGameGateUnlocked) candidates.Add(MysteryEventId.E09_ThuVienMucAn);
            }
            if (candidates.Count == 0)
                return (false, "Không có mẫu đúng điều kiện đã xác định; không tự đặt ngưỡng cho mẫu còn thiếu seed.");
            var selected = candidates[random.Next(candidates.Count)];
            bool gate = selected == MysteryEventId.E07_GiamDinhVoDanh || selected == MysteryEventId.E08_DoiMotLayMot;
            if (gate)
            {
                state.HasGateOffer = true; state.OfferedEventId = selected; state.OfferedAtMinute = FarmMinute(engine.Clock);
                return (true, "Cổng huyền bí xuất hiện trong sáu giờ.");
            }
            if (engine.FogWarningUntilMinute >= FarmMinute(engine.Clock))
            {
                state.HasPendingFog = true; state.PendingFogEventId = selected;
                state.PendingFogAtMinute = FarmMinute(engine.Clock) + FogWarningLeadMinutes;
                return (true, FogWarningText);
            }
            return (session.TryBegin(selected, engine.Clock.CurrentDay), "Sương mù ập đến.");
        }

        public const int FogWarningLeadMinutes = 10;
        public const string FogWarningText = "Lá Dò Sương rung: sương mù huyền bí sẽ ập đến sau 10 phút. Lùa heo vào chuồng, chuẩn bị đồ.";

        /// <summary>Starts a fog announced by Lá Dò Sương once its 10 minutes are up. Called every engine tick.</summary>
        public (bool started, string message) TickPendingFog(GameEngine engine)
        {
            var state = session.DirectorState;
            if (!state.HasPendingFog || FarmMinute(engine.Clock) < state.PendingFogAtMinute) return (false, "");
            state.HasPendingFog = false;
            if (session.IsActive) return (false, "");
            return (session.TryBegin(state.PendingFogEventId, engine.Clock.CurrentDay), "Sương mù ập đến.");
        }
    }
}
