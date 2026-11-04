using UnityEngine;

namespace PigTycoon.Presentation
{
    /// <summary>
    /// One fence segment of a PixelMyth map (layout role "fence"). GDD 3.8: when every way in is closed, ground
    /// monsters break the cheapest fence piece instead of queueing; Kẻ Cào Phá targets fences on purpose (GDD 19).
    /// Hit points follow the legacy Fence2DView (300). A broken segment stops blocking and fades; the hammer
    /// (Búa Gỗ) repairs it. The pixel sprite is never tinted while intact.
    /// </summary>
    public sealed class PixelFenceSegment : MonoBehaviour
    {
        public float MaxHp = 300f;
        public float CurrentHp = 300f;
        public bool IsBroken => CurrentHp <= 0f;
        /// <summary>Closed with a temporary panel (GDD 23 "Rào tạm 30G/đoạn"); a real repair is still owed (story Q01 → Q06).</summary>
        public bool IsTemporaryPatch;

        public const int TempPatchGold = 30;          // GDD 23 cách ly: "Rào tạm 30G/đoạn"; story Q01 uses the same price
        public const int RepairGoldPer10Hp = 10;      // GDD 18: light works 10–25G per 10 durability; fence = lightest (ADR-0015)
        public const float TempPatchHpFraction = 1f / 3f;

        private BoxCollider2D box;
        private SpriteRenderer sr;

        private void Awake()
        {
            box = GetComponent<BoxCollider2D>();
            sr = GetComponent<SpriteRenderer>();
        }

        public void TakeDamage(float amount)
        {
            if (IsBroken || amount <= 0f) return;
            CurrentHp = Mathf.Max(0f, CurrentHp - amount);
            Refresh();
        }

        public void Repair(float amount)
        {
            CurrentHp = Mathf.Min(MaxHp, CurrentHp + amount);
            Refresh();
        }

        /// <summary>
        /// E near a fence. A broken segment gets a temporary panel for 30G. A patched or damaged segment is repaired for
        /// real only outside a wave and only when someone staffs the Phòng Xây Dựng (GDD 13.2, 18; story Q06).
        /// </summary>
        public static bool TryPlayerFix(Vector2 playerPos, out string message)
        {
            message = "";
            PixelFenceSegment best = null;
            float bestD = 2.2f * 2.2f;
            foreach (var f in FindObjectsByType<PixelFenceSegment>())
            {
                if (f == null || (!f.IsBroken && !f.IsTemporaryPatch && f.CurrentHp >= f.MaxHp)) continue;
                float d = ((Vector2)f.transform.position - playerPos).sqrMagnitude;
                if (d < bestD) { bestD = d; best = f; }
            }
            if (best == null) return false;
            var engine = MobileGameController.Instance != null ? MobileGameController.Instance.Engine : null;
            if (engine == null) return false;
            if (best.IsBroken)
            {
                if (!engine.Ledger.TrySpend(TempPatchGold, PigTycoon.Core.LedgerReason.RepairStructure, "fence_temp"))
                { message = $"Rào tạm cần {TempPatchGold}G."; return true; }
                best.IsTemporaryPatch = true;
                best.CurrentHp = best.MaxHp * TempPatchHpFraction;
                best.Refresh();
                engine.SignalQuest(PigTycoon.Core.QuestSignal.FenceTempPatched);
                PlayerMobileController.Instance?.GetComponent<CharacterSpriteAnimator>()?.TriggerWork("repair");
                message = $"Đã chặn đoạn rào bằng rào tạm (−{TempPatchGold}G). Cần sửa hẳn ở Phòng Xây Dựng.";
                return true;
            }
            if (engine.Clock.IsDuringCombatWave) { message = "Không sửa rào trong đợt quái."; return true; }
            if (PigTycoon.Core.ConstructionHall.OnDutyNpc(engine) == null) { message = "Phòng Xây Dựng chưa có người trực, chỉ chặn rào tạm được."; return true; }
            int cost = Mathf.CeilToInt((best.MaxHp - best.CurrentHp) / 10f) * RepairGoldPer10Hp;
            if (!engine.Ledger.TrySpend(cost, PigTycoon.Core.LedgerReason.RepairStructure, "fence_repair"))
            { message = $"Sửa đoạn rào cần {cost}G."; return true; }
            best.IsTemporaryPatch = false;
            best.Repair(best.MaxHp);
            engine.SignalQuest(PigTycoon.Core.QuestSignal.FenceRepaired);
            message = $"Đã sửa hẳn đoạn rào (−{cost}G).";
            return true;
        }

        public void Break()
        {
            CurrentHp = 0f;
            Refresh();
        }

        private void Refresh()
        {
            if (box != null) box.isTrigger = IsBroken;   // broken: pigs and monsters pass through
            if (sr == null) return;
            float ratio = MaxHp > 0f ? CurrentHp / MaxHp : 0f;
            sr.color = IsBroken ? new Color(0.55f, 0.45f, 0.4f, 0.35f)
                : ratio < 0.5f ? new Color(1f, 0.82f, 0.78f, 1f) : Color.white;
        }
    }
}
