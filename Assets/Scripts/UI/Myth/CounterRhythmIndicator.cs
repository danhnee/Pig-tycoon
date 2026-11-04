using System;
using System.Reflection;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using PigTycoon.Core;

namespace PigTycoon.Presentation
{
    /// <summary>
    /// ADR-0024 Counter-Attack Rhythm Indicator ([Q] Phản Kích đòn đặc biệt nháy tím "!! tên đòn" &amp; [Ctrl] Lướt Né đòn cơ bản nháy cam).
    /// - Hiển thị vòng hội tụ 32-PPU (counter_ring_shrink -&gt; counter_ring_outer) + thanh căn nhịp [Q] trên HUD.
    /// - Khi quái đang lấy đà (progress 0.00 -&gt; 0.78): cảnh báo KHÔNG bấm Q sớm/loạn (nếu bấm sớm/loạn tỉ lệ quay về 5%).
    /// - Khi sát khoảnh khắc đòn chạm (progress 0.78 -&gt; 0.98): vòng bừng sáng vàng kim (counter_sweetspot_glow), báo "* BẤM [Q] NGAY! *".
    /// - Hỗ trợ cả gọi trực tiếp qua hàm static (ShowSpecialTelegraph / ShowBasicTelegraph / ReportCounterResult)
    ///   do EnemyAgentView gọi từng khung hình trong lúc quái lấy đà (tiến độ thật của đòn, tỉ lệ thật từ CounterResolver).
    /// </summary>
    [DisallowMultipleComponent, DefaultExecutionOrder(1250)]
    public sealed class CounterRhythmIndicator : MonoBehaviour
    {
        public static CounterRhythmIndicator Instance { get; private set; }

        private Canvas hudCanvas;
        private RectTransform hudRoot;
        private RectTransform barContainer;
        private Image barFrameImg;
        private Image needleImg;
        private Image ringOuterImg;
        private Image ringShrinkImg;
        private Image sweetSpotGlowImg;
        private TextMeshProUGUI titleText;
        private TextMeshProUGUI subText;

        private Transform trackedEnemy;
        private bool activeTelegraph;
        private bool isSpecial;
        private string activeMoveName = "";
        private float progress01;
        private float counterChance01 = 0.05f;
        private int counterStreak;
        private bool spamPenalty;
        private float lastUpdateUnscaledTime;
        private float resultBannerUntil;
        private float readyStart01 = 0.62f;
        private float sweetStart01 = 0.78f;

        // Auto-detection cache for EnemyAgentView when explicit API is not called
        private float nextEnemyScan;
        private EnemyAgentView[] cachedEnemies = new EnemyAgentView[0];
        private Transform autoEnemy;
        private float autoStartTime;
        private float autoDuration = 0.85f;
        private bool autoIsSpecial;
        private string autoMoveName = "Đòn Đặc Biệt";
        private int autoQPressCountInWindow;
        private float autoEstimatedChance = 0.05f;
        private int autoStreak;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void AutoBoot()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
            SceneManager.sceneLoaded += OnSceneLoaded;
            Ensure();
        }

        private static void OnSceneLoaded(Scene s, LoadSceneMode m) => Ensure();

        public static CounterRhythmIndicator Ensure()
        {
            if (Instance != null) return Instance;
            var go = new GameObject("CounterRhythmIndicator");
            return go.AddComponent<CounterRhythmIndicator>();
        }

        // ---------------------------------------------------------------- Public Static API for ADR-0024
        /// <summary>
        /// Hiển thị chỉ báo nhịp phản kích [Q] cho đòn đặc biệt nháy tím ("!! tên đòn").
        /// progress01: 0.0 (bắt đầu lấy đà) -&gt; 1.0 (khoảnh khắc đòn chạm).
        /// </summary>
        public static void ShowSpecialTelegraph(
            Transform enemyTransform,
            string moveName,
            float normalizedProgress01,
            float currentSuccessChance01 = 0.05f,
            int comboStreak = 0,
            bool spammedPenalty = false,
            float readyZoneStart01 = 0.62f,
            float sweetSpotStart01 = 0.78f)
        {
            var inst = Ensure();
            inst.trackedEnemy = enemyTransform;
            inst.activeTelegraph = true;
            inst.isSpecial = true;
            inst.activeMoveName = string.IsNullOrEmpty(moveName) ? "Đòn Đặc Biệt" : moveName;
            inst.progress01 = Mathf.Clamp01(normalizedProgress01);
            inst.counterChance01 = Mathf.Clamp(currentSuccessChance01, 0.05f, 0.95f);
            inst.counterStreak = Mathf.Max(0, comboStreak);
            inst.spamPenalty = spammedPenalty;
            inst.readyStart01 = Mathf.Clamp01(readyZoneStart01);
            inst.sweetStart01 = Mathf.Clamp01(sweetSpotStart01);
            inst.lastUpdateUnscaledTime = Time.unscaledTime;
        }

        /// <summary>
        /// Hiển thị chỉ báo lướt né [Ctrl] cho đòn cơ bản nháy cam.
        /// </summary>
        public static void ShowBasicTelegraph(Transform enemyTransform, string moveName, float normalizedProgress01)
        {
            var inst = Ensure();
            inst.trackedEnemy = enemyTransform;
            inst.activeTelegraph = true;
            inst.isSpecial = false;
            inst.activeMoveName = string.IsNullOrEmpty(moveName) ? "Đòn Cơ Bản" : moveName;
            inst.progress01 = Mathf.Clamp01(normalizedProgress01);
            inst.lastUpdateUnscaledTime = Time.unscaledTime;
        }

        /// <summary>
        /// Báo kết quả phản kích [Q] để hiển thị hiệu ứng bừng sáng hoặc trượt nhịp.
        /// </summary>
        public static void ReportCounterResult(bool success, float chanceUsed01 = 0f, int streak = 0)
        {
            var inst = Ensure();
            inst.activeTelegraph = false;
            inst.resultBannerUntil = Time.unscaledTime + 0.85f;
            if (inst.barContainer != null) inst.barContainer.gameObject.SetActive(true);
            if (inst.ringOuterImg != null) inst.ringOuterImg.gameObject.SetActive(false);
            if (inst.titleText != null)
            {
                inst.titleText.text = success
                    ? $"<color=#FFE66D><b>> PHẢN KÍCH [Q] THÀNH CÔNG! (x{Mathf.Max(1, streak)}) <</b></color>"
                    : "<color=#FF6B6B><b>TRƯỢT NHỊP PHẢN KÍCH [Q]</b></color>";
            }
            if (inst.subText != null)
            {
                inst.subText.text = success
                    ? $"<color=#9BE37B>Chuẩn khoảnh khắc va chạm ({chanceUsed01 * 100f:0}% tỉ lệ)</color>"
                    : "<color=#E2C8A0>Hãy bấm [Q] đúng 1 lần sát lúc vòng tím chạm vòng vàng!</color>";
            }
        }

        public static void HideTelegraph(Transform enemyTransform = null)
        {
            if (Instance == null) return;
            if (enemyTransform == null || Instance.trackedEnemy == enemyTransform)
            {
                Instance.activeTelegraph = false;
            }
        }

        // ---------------------------------------------------------------- Unity Lifecycle & UI Construction
        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            BuildUI();
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        private void BuildUI()
        {
            hudCanvas = gameObject.AddComponent<Canvas>();
            hudCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
            hudCanvas.sortingOrder = 92;

            var scaler = gameObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1280f, 720f);
            scaler.matchWidthOrHeight = 0.5f;

            hudRoot = new GameObject("Root", typeof(RectTransform)).GetComponent<RectTransform>();
            hudRoot.SetParent(transform, false);
            hudRoot.anchorMin = Vector2.zero;
            hudRoot.anchorMax = Vector2.one;
            hudRoot.offsetMin = hudRoot.offsetMax = Vector2.zero;

            // 1. Vòng hội tụ bám theo quái trên màn hình (Converging Target Ring)
            var ringGo = new GameObject("TargetRing", typeof(RectTransform), typeof(Image));
            ringGo.transform.SetParent(hudRoot, false);
            ringOuterImg = ringGo.GetComponent<Image>();
            ringOuterImg.sprite = MythUi.Kit("counter_ring_outer");
            ringOuterImg.raycastTarget = false;
            ringOuterImg.rectTransform.sizeDelta = new Vector2(96f, 96f);

            var glowGo = new GameObject("SweetSpotGlow", typeof(RectTransform), typeof(Image));
            glowGo.transform.SetParent(ringGo.transform, false);
            sweetSpotGlowImg = glowGo.GetComponent<Image>();
            sweetSpotGlowImg.sprite = MythUi.Kit("counter_sweetspot_glow");
            sweetSpotGlowImg.raycastTarget = false;
            sweetSpotGlowImg.rectTransform.sizeDelta = new Vector2(104f, 104f);

            var shrinkGo = new GameObject("ShrinkRing", typeof(RectTransform), typeof(Image));
            shrinkGo.transform.SetParent(ringGo.transform, false);
            ringShrinkImg = shrinkGo.GetComponent<Image>();
            ringShrinkImg.sprite = MythUi.Kit("counter_ring_shrink");
            ringShrinkImg.raycastTarget = false;
            ringShrinkImg.rectTransform.sizeDelta = new Vector2(96f, 96f);

            ringGo.SetActive(false);

            // 2. Thanh căn nhịp phản kích [Q] / Lướt né [Ctrl] ở phía dưới giữa màn hình
            var barGo = new GameObject("CounterTimingBar", typeof(RectTransform));
            barGo.transform.SetParent(hudRoot, false);
            barContainer = barGo.GetComponent<RectTransform>();
            barContainer.anchorMin = barContainer.anchorMax = new Vector2(0.5f, 0f);
            barContainer.pivot = new Vector2(0.5f, 0f);
            barContainer.anchoredPosition = new Vector2(0f, 92f);
            barContainer.sizeDelta = new Vector2(340f, 68f);

            var frameGo = new GameObject("BarFrame", typeof(RectTransform), typeof(Image));
            frameGo.transform.SetParent(barContainer, false);
            barFrameImg = frameGo.GetComponent<Image>();
            barFrameImg.sprite = MythUi.Kit("counter_bar_frame");
            barFrameImg.raycastTarget = false;
            barFrameImg.rectTransform.anchoredPosition = new Vector2(0f, 2f);
            barFrameImg.rectTransform.sizeDelta = new Vector2(280f, 28f);

            var needleGo = new GameObject("Needle", typeof(RectTransform), typeof(Image));
            needleGo.transform.SetParent(frameGo.transform, false);
            needleImg = needleGo.GetComponent<Image>();
            needleImg.sprite = MythUi.Kit("counter_needle");
            needleImg.raycastTarget = false;
            needleImg.rectTransform.sizeDelta = new Vector2(16f, 34f);

            titleText = DayMarketPopup.Text(barContainer, new Vector2(0f, 26f), new Vector2(380f, 24f), 14f, TextAlignmentOptions.Center);
            titleText.outlineWidth = 0.25f;
            titleText.outlineColor = new Color32(20, 10, 25, 255);
            titleText.raycastTarget = false;

            subText = DayMarketPopup.Text(barContainer, new Vector2(0f, -20f), new Vector2(380f, 20f), 11.5f, TextAlignmentOptions.Center);
            subText.outlineWidth = 0.2f;
            subText.outlineColor = new Color32(20, 10, 25, 255);
            subText.raycastTarget = false;

            barContainer.gameObject.SetActive(false);
        }

        private void Update()
        {
            // Timeout explicit telegraph if not updated for >0.25s
            if (activeTelegraph && Time.unscaledTime - lastUpdateUnscaledTime > 0.25f)
            {
                activeTelegraph = false;
            }

            if (!activeTelegraph)
            {
                if (Time.unscaledTime < resultBannerUntil)
                {
                    if (barFrameImg != null) barFrameImg.gameObject.SetActive(false);
                    return;
                }
                if (barContainer != null && barContainer.gameObject.activeSelf) barContainer.gameObject.SetActive(false);
                if (ringOuterImg != null && ringOuterImg.gameObject.activeSelf) ringOuterImg.gameObject.SetActive(false);
                return;
            }

            RenderActiveTelegraph();
        }

        private void RenderActiveTelegraph()
        {
            if (barContainer != null && !barContainer.gameObject.activeSelf) barContainer.gameObject.SetActive(true);
            if (barFrameImg != null && !barFrameImg.gameObject.activeSelf) barFrameImg.gameObject.SetActive(true);

            // Update needle position along the 280px bar (-130 .. +130)
            if (needleImg != null)
            {
                float x = Mathf.Lerp(-130f, 130f, progress01);
                needleImg.rectTransform.anchoredPosition = new Vector2(x, 0f);
            }

            bool inSweetSpot = progress01 >= sweetStart01;
            bool inReadyZone = progress01 >= readyStart01 && progress01 < sweetStart01;

            if (isSpecial)
            {
                string moveHeader = $"<color=#E879F9><b>!! {activeMoveName.ToUpperInvariant()} !!</b></color>";
                if (spamPenalty)
                {
                    titleText.text = $"{moveHeader} — <color=#FF5555><b>BẤM SỚM / LOẠN (Tỉ lệ về 5%)</b></color>";
                }
                else if (inSweetSpot)
                {
                    titleText.text = $"{moveHeader} — <color=#FFE66D><b>> BẤM [Q] PHẢN KÍCH NGAY! <</b></color>";
                }
                else if (inReadyZone)
                {
                    titleText.text = $"{moveHeader} — <color=#FBBF24><b>Chuẩn bị căn nhịp [Q]...</b></color>";
                }
                else
                {
                    titleText.text = $"{moveHeader} — <color=#D8B4FE>Chờ quái lấy đà (Đừng bấm [Q] sớm)</color>";
                }

                string streakTag = counterStreak > 0 ? $" · Chuỗi x{counterStreak}" : "";
                subText.text = spamPenalty
                    ? $"Đòn đặc biệt không thể né — <b>Phản kích [Q]: 5%</b>{streakTag}"
                    : $"Đòn đặc biệt không thể né — <b>Bấm [Q] 1 lần đúng lúc vòng vàng: tới {counterChance01 * 100f:0}%</b>{streakTag}";
            }
            else
            {
                titleText.text = $"<color=#FB923C><b>> {activeMoveName} — BẤM [Ctrl] LƯỚT NÉ! <</b></color>";
                subText.text = "<color=#FDE68A>Đòn cơ bản nháy cam: lướt né bằng [Ctrl] trước khi đòn chạm</color>";
            }

            // Position converging ring on the monster in screen space
            if (trackedEnemy != null && Camera.main != null && ringOuterImg != null)
            {
                ringOuterImg.gameObject.SetActive(true);
                Vector3 screenPos = Camera.main.WorldToScreenPoint(trackedEnemy.position + new Vector3(0f, 0.6f, 0f));
                RectTransformUtility.ScreenPointToLocalPointInRectangle(hudRoot, screenPos, null, out var localPt);
                ringOuterImg.rectTransform.anchoredPosition = localPt;

                // Shrink ring from 2.1x down to 1.0x as progress01 goes 0 -> 0.90
                float scale = Mathf.Lerp(2.15f, 0.95f, Mathf.Clamp01(progress01 / 0.90f));
                ringShrinkImg.rectTransform.localScale = new Vector3(scale, scale, 1f);
                ringShrinkImg.color = isSpecial
                    ? (inSweetSpot ? new Color(1f, 0.95f, 0.45f, 1f) : new Color(0.92f, 0.45f, 1f, 0.92f))
                    : new Color(1f, 0.62f, 0.22f, 0.92f);

                if (sweetSpotGlowImg != null)
                {
                    sweetSpotGlowImg.gameObject.SetActive(isSpecial && inSweetSpot);
                }
            }
            else if (ringOuterImg != null)
            {
                ringOuterImg.gameObject.SetActive(false);
            }
        }
    }
}
