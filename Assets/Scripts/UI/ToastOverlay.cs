using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace PigTycoon.Presentation
{
    /// <summary>
    /// The one visible channel for short game messages (action results, refusal reasons, quest done, lore).
    /// PlayerInteractionController.ShowFeedback used to write into a text that lived inside the legacy
    /// Context_Action_Button, which the Myth HUD switches off, so 165 call sites showed nothing (3 Nov audit).
    /// Own overlay canvas above the HUD and popups; messages queue, stay longer when longer, never block input.
    /// Look and feel is Argon's to restyle; keep Show(string) as the contract.
    /// </summary>
    public sealed class ToastOverlay : MonoBehaviour
    {
        const int MaxQueued = 4;
        static ToastOverlay instance;
        readonly Queue<string> queue = new Queue<string>();
        RectTransform plate;
        Image plateImg;
        bool currentUrgent;
        TextMeshProUGUI label;
        LayoutElement labelSize;
        CanvasGroup group;
        float timeLeft;

        /// <summary>Messages accepted so far and the latest one (read by the UI probe to tell feedback from silence).</summary>
        public static int ShownCount { get; private set; }
        public static string LastMessage { get; private set; }

        public static void Show(string message)
        {
            message = PlayerText.Clean(message);
            if (string.IsNullOrWhiteSpace(message)) return;
            ShownCount++;
            LastMessage = message;
            Debug.Log($"[PlayerFeedback] {message}");
            if (!Application.isPlaying) return;
            if (instance == null)
            {
                var go = new GameObject("ToastOverlay");
                DontDestroyOnLoad(go);
                instance = go.AddComponent<ToastOverlay>();
                instance.Build();
            }
            instance.Enqueue(message);
        }

        void Enqueue(string message)
        {
            // The same refusal fired every frame (e.g. holding a key) shows once.
            if (timeLeft > 0f && label.text == message) { timeLeft = Mathf.Max(timeLeft, 1.2f); return; }
            foreach (var q in queue) if (q == message) return;
            bool urgent = message.IndexOf("Say Nắng", System.StringComparison.OrdinalIgnoreCase) >= 0
                || message.IndexOf("Lôi Tâm", System.StringComparison.OrdinalIgnoreCase) >= 0
                || message.IndexOf("run giật", System.StringComparison.OrdinalIgnoreCase) >= 0
                || message.IndexOf("quá nhiệt", System.StringComparison.OrdinalIgnoreCase) >= 0
                || message.IndexOf("gục", System.StringComparison.OrdinalIgnoreCase) >= 0;
            if (urgent)
            {
                var items = queue.ToArray();
                queue.Clear();
                string tagged = message.StartsWith("[KHẨN]") ? message : "[KHẨN] " + message;
                queue.Enqueue(tagged);
                for (int i = 0; i < items.Length && queue.Count < MaxQueued; i++) queue.Enqueue(items[i]);
                Next();
                return;
            }
            if (queue.Count >= MaxQueued) queue.Dequeue();
            queue.Enqueue(message);
            if (timeLeft <= 0f) Next();
        }

        void Next()
        {
            if (queue.Count == 0) { group.alpha = 0f; return; }
            string m = queue.Dequeue();
            currentUrgent = m.StartsWith("[KHẨN]");
            label.text = m;
            label.color = currentUrgent ? new Color(1f, 0.90f, 0.56f) : new Color(0.98f, 0.93f, 0.80f);
            if (plateImg != null) plateImg.color = currentUrgent ? new Color(0.46f, 0.11f, 0.10f, 0.98f) : new Color(0.10f, 0.14f, 0.20f, 0.96f);
            labelSize.preferredWidth = Mathf.Min(640f, label.GetPreferredValues(m).x + 2f);
            // ~16 characters per second of reading, 2.2 s minimum, 9 s cap (lore boards are long).
            timeLeft = Mathf.Clamp(2.2f + m.Length / 16f, 2.2f, 9f);
            group.alpha = 1f;
        }

        void Update()
        {
            if (timeLeft <= 0f) return;
            timeLeft -= Time.unscaledDeltaTime;
            if (timeLeft < 0.35f) group.alpha = Mathf.Clamp01(timeLeft / 0.35f);
            if (timeLeft <= 0f) Next();
        }

        void Build()
        {
            var canvas = gameObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 32000; // above HUD and popups
            var scaler = gameObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1280, 720);
            scaler.matchWidthOrHeight = 1f;
            group = gameObject.AddComponent<CanvasGroup>();
            group.blocksRaycasts = false;
            group.interactable = false;
            group.alpha = 0f;

            var plateGo = new GameObject("Plate", typeof(RectTransform), typeof(Image));
            plateGo.transform.SetParent(transform, false);
            plate = (RectTransform)plateGo.transform;
            // top-center so it never overlaps bottom buttons in VillageBarterPopup / BackpackPopup
            plate.anchorMin = new Vector2(0.5f, 1f);
            plate.anchorMax = new Vector2(0.5f, 1f);
            plate.pivot = new Vector2(0.5f, 1f);
            plate.anchoredPosition = new Vector2(0f, -48f);
            var img = plateGo.GetComponent<Image>();
            plateImg = img;
            img.raycastTarget = false;
            var frame = UISpriteLoader.GetFrameSlotSelected() ?? UISpriteLoader.GetFrameSlotNormal();
            if (frame != null) { img.sprite = frame; img.type = Image.Type.Sliced; img.color = new Color(0.10f, 0.14f, 0.20f, 0.96f); }
            else img.color = new Color(0.07f, 0.10f, 0.15f, 0.95f);
            var sh = plateGo.AddComponent<Shadow>();
            sh.effectColor = new Color(0f, 0f, 0f, 0.72f);
            sh.effectDistance = new Vector2(0f, -3f);
            var fit = plateGo.AddComponent<ContentSizeFitter>();
            fit.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
            fit.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            var layout = plateGo.AddComponent<HorizontalLayoutGroup>();
            layout.padding = new RectOffset(22, 22, 12, 12);
            layout.childControlWidth = layout.childControlHeight = true;

            var textGo = new GameObject("Text", typeof(RectTransform));
            textGo.transform.SetParent(plate, false);
            label = textGo.AddComponent<TextMeshProUGUI>();
            label.fontSize = 19;
            label.color = new Color(0.98f, 0.93f, 0.80f);
            label.alignment = TextAlignmentOptions.Center;
            label.textWrappingMode = TextWrappingModes.Normal;
            label.raycastTarget = false;
            labelSize = textGo.AddComponent<LayoutElement>();
        }
    }
}
