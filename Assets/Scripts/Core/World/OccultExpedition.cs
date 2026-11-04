using System;

namespace PigTycoon.Core
{
    [Serializable]
    public sealed class OccultExpeditionState
    {
        public int LastRolledDay { get; set; }
        public bool HasGateOffer { get; set; }
        public MysteryEventId OfferedEventId { get; set; }
        public long OfferedAtMinute { get; set; }
        public bool IsActive { get; set; }
        public MysteryEventId EventId { get; set; }
        public int StartedDay { get; set; }
        public float CompletionPercent { get; set; }
        public string ReturnLocationId { get; set; } = "Farm";
        /// <summary>Lá Dò Sương (GDD 12.8): a fog the leaf detected starts 10 game minutes after the warning.</summary>
        public bool HasPendingFog { get; set; }
        public MysteryEventId PendingFogEventId { get; set; }
        public long PendingFogAtMinute { get; set; }
        public OccultExpeditionState Clone() => (OccultExpeditionState)MemberwiseClone();
    }

    /// <summary>GDD24 encounter ownership. A portal/fog encounter is not a permanent map unlock.</summary>
    public sealed class OccultExpeditionSession
    {
        OccultExpeditionState state = new OccultExpeditionState();
        internal OccultExpeditionState DirectorState => state;
        public bool IsActive => state.IsActive;
        public bool IsGateEvent => IsGate(state.EventId);
        public int StartedDay => state.StartedDay;
        public float CompletionPercent => state.CompletionPercent;
        public MysteryEventId EventId => state.EventId;

        public static bool SupportsExpedition(MysteryEventId id) =>
            Enum.IsDefined(typeof(MysteryEventId), id) &&
            id != MysteryEventId.E10_DotSanTrongSuong && id != MysteryEventId.E11_CanQuetUfo;

        static bool IsGate(MysteryEventId id) => id == MysteryEventId.E01_VuonMuoiHaiQua ||
            id == MysteryEventId.E03_KhoLanhBoQuen || id == MysteryEventId.E05_BiNhanMuaGia ||
            id == MysteryEventId.E07_GiamDinhVoDanh || id == MysteryEventId.E08_DoiMotLayMot ||
            id == MysteryEventId.E12_GiuCongNguoc || id == MysteryEventId.E14_LoMoHuKhong;

        // Only the domain encounter director may start a qualified encounter. UI cannot unlock a forest road.
        internal bool TryBegin(MysteryEventId eventId, int currentDay)
        {
            if (state.IsActive || currentDay < 1 || !SupportsExpedition(eventId)) return false;
            state.IsActive = true; state.EventId = eventId; state.StartedDay = currentDay;
            state.CompletionPercent = 0; state.ReturnLocationId = "Farm"; state.HasGateOffer = false;
            return true;
        }

        internal bool RecordProgress(float completionPercent)
        {
            if (!IsActive || float.IsNaN(completionPercent) || float.IsInfinity(completionPercent) ||
                completionPercent < state.CompletionPercent || completionPercent > 100) return false;
            state.CompletionPercent = completionPercent;
            return true;
        }

        public (bool canExit, int rewardPercentTier, string message) EvaluateExit(
            int currentDay, SpaceBag bag, string? requestedItemId = null)
        {
            if (!IsActive) return (false, 0, "Không có sự kiện huyền bí đang diễn ra.");
            int elapsed = Math.Max(0, currentDay - state.StartedDay);
            if (state.CompletionPercent >= 100) return (true, 100, "Hoàn thành sự kiện.");
            if (elapsed >= ForestPrimevalAndEventFramework.MaxDaysTrappedInEvent)
                return (true, 30, "Hết ba ngày: tự động trở về.");
            bool owned = FindCarried(bag, requestedItemId) >= 0;
            if (!owned) return (false, 0, "Cần hoàn thành sự kiện hoặc dùng vật phẩm đang mang theo.");
            if (IsGateEvent && IsItem(requestedItemId, "BuaHoiHuong") && state.CompletionPercent >= 50)
                return (true, 50, "Bùa Hồi Hương: giữ 50% thưởng.");
            if (!IsGateEvent && IsItem(requestedItemId, "NgocPhaSuong"))
                return (true, 50, "Ngọc Phá Sương: giữ 50% thưởng.");
            if (!IsGateEvent && IsItem(requestedItemId, "LaBanTanVu") && elapsed >= 1)
                return (true, 30, "La Bàn Tán Vụ: giữ 30% thưởng.");
            return (false, 0, "Vật phẩm chưa đáp ứng điều kiện thoát Cổng/Sương.");
        }

        internal bool CompleteExit(int currentDay, SpaceBag bag, string? requestedItemId = null)
        {
            var result = EvaluateExit(currentDay, bag, requestedItemId);
            if (!result.canExit) return false;
            bool freeExit = state.CompletionPercent >= 100 || currentDay - state.StartedDay >= ForestPrimevalAndEventFramework.MaxDaysTrappedInEvent;
            if (!freeExit && !bag.TryConsumeOne(FindCarried(bag, requestedItemId), out _, out _)) return false;
            state.IsActive = false;
            return true;
        }

        static int FindCarried(SpaceBag bag, string? id)
        {
            if (string.IsNullOrWhiteSpace(id)) return -1;
            var items = bag.Items;
            for (int i = 0; i < items.Count; i++)
                if (IsItem(items[i].QualifiedItemId, id!)) return i;
            return -1;
        }
        static bool IsItem(string? id, string item) =>
            string.Equals(QualifiedItemRegistry.QualifyItemId(id!), QualifiedItemRegistry.QualifyItemId(item), StringComparison.OrdinalIgnoreCase);

        public OccultExpeditionState CreateSaveData() => state.Clone();
        public static bool IsValidSnapshot(OccultExpeditionState? saved, out string message)
        {
            if (saved == null) { message = string.Empty; return true; }
            bool valid = saved.LastRolledDay >= 0 && saved.OfferedAtMinute >= 0 &&
                saved.StartedDay >= 0 && Enum.IsDefined(typeof(MysteryEventId), saved.EventId) &&
                (!saved.HasGateOffer || (SupportsExpedition(saved.OfferedEventId) && IsGate(saved.OfferedEventId) && !saved.IsActive)) &&
                !float.IsNaN(saved.CompletionPercent) && !float.IsInfinity(saved.CompletionPercent) &&
                saved.CompletionPercent >= 0 && saved.CompletionPercent <= 100 && saved.ReturnLocationId == "Farm" &&
                (!saved.IsActive || (SupportsExpedition(saved.EventId) && saved.StartedDay >= 1));
            message = valid ? string.Empty : "Dữ liệu sự kiện huyền bí không hợp lệ; giữ bản lưu để khôi phục.";
            return valid;
        }
        public void Restore(OccultExpeditionState? saved)
        {
            if (!IsValidSnapshot(saved, out var message)) throw new ArgumentException(message, nameof(saved));
            state = saved?.Clone() ?? new OccultExpeditionState();
        }
    }
}
