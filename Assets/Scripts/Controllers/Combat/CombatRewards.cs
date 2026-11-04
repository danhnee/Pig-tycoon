using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using PigTycoon.Core;

namespace PigTycoon.Presentation
{
    /// <summary>Kill → Nộ, Điểm Võ and EXP for the player (GDD 12.1, 12.6, 8.2), with a level-up message.</summary>
    public static class CombatRewards
    {
        /// <summary>GDD 19.1/19.2: Đầu Lĩnh Trộm and Kẻ Mang Dịch are elites; the cycle bosses are bosses.</summary>
        public static KillClass Classify(EnemyTypeId t)
        {
            switch (t)
            {
                case EnemyTypeId.DauLinhTrom:
                case EnemyTypeId.KeMangDich:
                case EnemyTypeId.AlienElite:
                    return KillClass.Elite;
                case EnemyTypeId.ChuaTromRiuSat:
                case EnemyTypeId.UfoMothership:
                case EnemyTypeId.ThucTheSuong:
                case EnemyTypeId.BossAlienWarlord:
                    return KillClass.Boss;
                default:
                    return KillClass.Normal;
            }
        }

        public static void OnEnemyKilled(EnemyTypeId type)
        {
            var engine = MobileGameController.Instance != null ? MobileGameController.Instance.Engine : null;
            if (engine == null) return;
            var c = engine.Character;
            int level = c.Level, points = c.MartialPoints;
            MartialProgression.OnKill(c, Classify(type));
            if (c.Level > level)
                PlayerInteractionController.Instance?.ShowFeedback($"Lên cấp {c.Level}! +{2 * (c.Level - level)} điểm chỉ số (phím C).");
            else if (c.MartialPoints > points)
                PlayerInteractionController.Instance?.ShowFeedback($"+{c.MartialPoints - points} Điểm Võ (đang {c.MartialPoints}).");
            MobileGameController.Instance?.OnStateUpdated?.Invoke();
        }
    }

    /// <summary>
    /// Ấn Tín Hiệu / Nỏ Tay marks (structures prefer marked targets for 5 s, GDD 12.3/12.7) and Trói Lưới / Neo Kích roots
    /// (Trói Chùy: rooted targets take ×1.3). Also tints the target so the player sees the effect.
    /// </summary>
    public static class CombatMarks
    {
        static readonly Dictionary<EnemyAgentView, float> marked = new Dictionary<EnemyAgentView, float>();
        static readonly Dictionary<EnemyAgentView, float> rooted = new Dictionary<EnemyAgentView, float>();

        public static void Mark(EnemyAgentView e, float seconds)
        {
            if (e == null) return;
            marked[e] = Time.time + seconds;
            Flash(e, new Color(1f, 0.35f, 0.35f), seconds);
        }

        public static bool IsMarked(EnemyAgentView e) => e != null && marked.TryGetValue(e, out var t) && Time.time < t;

        public static void Root(EnemyAgentView e, float seconds)
        {
            if (e != null) rooted[e] = Time.time + seconds;
        }

        public static bool IsRooted(EnemyAgentView e) => e != null && rooted.TryGetValue(e, out var t) && Time.time < t;

        public static void Flash(EnemyAgentView e, Color tint, float seconds)
        {
            if (e == null) return;
            var sr = e.GetComponent<SpriteRenderer>() ?? e.GetComponentInChildren<SpriteRenderer>();
            if (sr == null) return;
            e.StartCoroutine(TintRoutine(sr, tint, seconds));
        }

        static IEnumerator TintRoutine(SpriteRenderer sr, Color tint, float seconds)
        {
            float until = Time.time + seconds;
            while (sr != null && Time.time < until)
            {
                sr.color = Color.Lerp(Color.white, tint, 0.5f + 0.5f * Mathf.Sin(Time.time * 10f));
                yield return null;
            }
            if (sr != null) sr.color = Color.white;
        }
    }
}
