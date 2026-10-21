using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;
using PigTycoon.Core;

namespace PigTycoon.Presentation
{
    public class TransitionZoneController : MonoBehaviour
    {
        public static TransitionZoneController Instance { get; private set; }

        [Header("Scene Configuration")]
        public string FarmSceneName = "MainFarm2D";
        public string VillageSceneName = "BoRiaVillage2D";

        [Header("Zone Positions")]
        public Vector2 FarmSouthGatePosition = new Vector2(0f, -29.5f);
        public Vector2 FarmArrivalPosition = new Vector2(0f, -27.5f);
        public Vector2 VillageEntrancePosition = new Vector2(-14f, 0f);
        public Vector2 VillageArrivalPosition = new Vector2(-12f, 0f);

        [Header("Current State")]
        public bool IsInVillage { get; private set; } = false;
        public bool IsTransitioning { get; private set; } = false;

        public event Action<bool> OnZoneTransitionCompleted;

        // UI Transition Overlay Elements
        private Canvas transitionCanvas;
        private CanvasGroup overlayCanvasGroup;
        private Image darkBackground;
        private TextMeshProUGUI diaryQuoteText;
        private TextMeshProUGUI zoneTitleText;

        // Interactive Transition Waypoint Prompt
        private GameObject promptCanvasObj;
        private GameObject promptPanel;
        private TextMeshProUGUI promptText;
        private Button promptActionButton;
        private TransitionTriggerListener activeListener;
        private TextMeshProUGUI promptContinueText;
        private bool isWaitingForPlayerDismiss = false;

        // Narrative Lore Quotes from Khoa's Notebook (COT_TRUYEN_PIG_TYCOON.md)
        private readonly string[] ToVillageQuotes = new string[]
        {
            "\"Đoạn dốc đất đỏ trơn trượt sau cơn mưa phùn. Sương mù đọng quánh trên những bụi gai cằn cỗi.\nBiển gỗ mục xiêu vẹo: 'Làng Bờ Rìa - Cấm số 7, cấm gõ mõ sau giờ Dậu'.\nDân làng cúi gằm mặt bước vội, tuyệt nhiên không một ai nhìn lên bầu trời.\"",
            "\"Tiếng đục đẽo của Tám Vá vang lên khô khốc từ gian xưởng mộc đầu làng.\nKhói bếp quán Bà Sáu nồng mùi rơm ẩm và nước chè hắc xỉn.\nMọi người ở đây nhìn tôi với ánh mắt lờ đờ, như thể tôi là người lạ bước nhầm vào một giấc mộng dài.\"",
            "\"'Cái bát úp đó chớ có lật ngửa, khách quen chưa tới.'\nLời nhắc của bà Sáu văng vẳng bên tai khi tôi đặt chân xuống ranh giới xóm.\nMột cảm giác rợn ngợp lan tỏa sau gáy—hai phút độ trễ thời gian lại bắt đầu kéo căng thực tại.\""
        };

        private readonly string[] ToFarmQuotes = new string[]
        {
            "\"Vượt qua rặng tre gãy ngược trở lại ranh giới trang trại.\nMùi phân heo nồng ấm và tiếng máng gỗ lách cách kéo tôi trở về thực tại tỉnh táo.\nSau lưng, làng Bờ Rìa chìm dần vào làn sương xám đặc quánh.\"",
            "\"Tiếng rào gỗ cọt kẹt theo từng cơn gió dồn.\nBầy heo trong chuồng ngẩng đầu ngơ ngác khi thấy tôi bước qua Cổng Nam.\nÍt nhất ở đây, mọi sự cân đo và sinh sát đều nằm trong tay mình.\""
        };

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);
            EnsureUIExists();
            EnsurePromptUIExists();
        }

        private void OnEnable()
        {
            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        private void OnDisable()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
        }

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            IsInVillage = string.Equals(scene.name, VillageSceneName, StringComparison.OrdinalIgnoreCase);
            EnsureUIExists();
            EnsurePromptUIExists();
            CreateWorldTriggersIfMissing();
            ApplyAtmosphereTints(IsInVillage);
        }

        private void Start()
        {
            EnsureUIExists();
            EnsurePromptUIExists();
            CreateWorldTriggersIfMissing();
        }

        private void Update()
        {
            if (isWaitingForPlayerDismiss && (Input.anyKeyDown || Input.GetMouseButtonDown(0)))
            {
                isWaitingForPlayerDismiss = false;
            }
        }

        public void TravelToVillage(string customQuote = null)
        {
            if (IsTransitioning || IsInVillage) return;
            StartCoroutine(PerformTransitionRoutine(true, customQuote));
        }

        public void TravelToFarm(string customQuote = null)
        {
            if (IsTransitioning || !IsInVillage) return;
            StartCoroutine(PerformTransitionRoutine(false, customQuote));
        }

        public void ToggleZone()
        {
            if (IsInVillage)
            {
                TravelToFarm();
            }
            else
            {
                TravelToVillage();
            }
        }

        private IEnumerator PerformTransitionRoutine(bool destinationIsVillage, string customQuote)
        {
            IsTransitioning = true;
            EnsureUIExists();
            if (promptPanel != null) promptPanel.SetActive(false);

            string title = destinationIsVillage ? "— LỐI MÒN XUỐNG LÀNG BỜ RÌA —" : "— TRỞ VỀ TRANG TRẠI HEO —";
            string quote = customQuote;
            if (string.IsNullOrEmpty(quote))
            {
                var quotePool = destinationIsVillage ? ToVillageQuotes : ToFarmQuotes;
                quote = quotePool[UnityEngine.Random.Range(0, quotePool.Length)];
            }

            zoneTitleText.text = title;
            diaryQuoteText.text = quote;
            promptContinueText.text = "[ Nhấp chuột hoặc chạm bất kỳ để tiếp tục ]";
            promptContinueText.gameObject.SetActive(false);

            overlayCanvasGroup.gameObject.SetActive(true);
            overlayCanvasGroup.alpha = 0f;
            overlayCanvasGroup.blocksRaycasts = true;

            // 1. Fade Out to Black
            float elapsed = 0f;
            float fadeDuration = 0.5f;
            while (elapsed < fadeDuration)
            {
                elapsed += Time.deltaTime;
                overlayCanvasGroup.alpha = Mathf.Clamp01(elapsed / fadeDuration);
                yield return null;
            }
            overlayCanvasGroup.alpha = 1f;

            IsInVillage = destinationIsVillage;

            // 2. Load Destination Scene Asynchronously & Position Player
            string targetScene = destinationIsVillage ? VillageSceneName : FarmSceneName;
            string currentScene = SceneManager.GetActiveScene().name;
            if (!string.Equals(currentScene, targetScene, StringComparison.OrdinalIgnoreCase))
            {
                var asyncOp = SceneManager.LoadSceneAsync(targetScene);
                if (asyncOp != null)
                {
                    while (!asyncOp.isDone)
                    {
                        yield return null;
                    }
                }
            }

            Vector2 targetPos = destinationIsVillage ? VillageArrivalPosition : FarmArrivalPosition;
            var player = FindAnyObjectByType<PlayerMobileController>();
            if (player != null)
            {
                player.transform.position = new Vector3(targetPos.x, targetPos.y, player.transform.position.z);
                var rb = player.GetComponent<Rigidbody2D>();
                if (rb != null) rb.linearVelocity = Vector2.zero;
            }

            if (Camera.main != null)
            {
                Camera.main.transform.position = new Vector3(targetPos.x, targetPos.y, Camera.main.transform.position.z);
                var camFollow = Camera.main.GetComponent<CameraFollow2D>();
                if (camFollow != null) camFollow.SnapToTarget();
            }

            // Cập nhật không khí u ám nếu chuyển sang Làng
            ApplyAtmosphereTints(destinationIsVillage);

            // 3. Narrative display pause (1.2s minimum, then allow dismiss)
            yield return new WaitForSeconds(1.2f);
            promptContinueText.gameObject.SetActive(true);
            isWaitingForPlayerDismiss = true;

            float maxWait = 4.0f;
            float waitTimer = 0f;
            while (isWaitingForPlayerDismiss && waitTimer < maxWait)
            {
                waitTimer += Time.deltaTime;
                yield return null;
            }
            promptContinueText.gameObject.SetActive(false);

            // 4. Fade In from Black
            elapsed = 0f;
            while (elapsed < fadeDuration)
            {
                elapsed += Time.deltaTime;
                overlayCanvasGroup.alpha = Mathf.Clamp01(1f - (elapsed / fadeDuration));
                yield return null;
            }
            overlayCanvasGroup.alpha = 0f;
            overlayCanvasGroup.blocksRaycasts = false;
            overlayCanvasGroup.gameObject.SetActive(false);

            IsTransitioning = false;
            OnZoneTransitionCompleted?.Invoke(IsInVillage);

            string notifyMsg = destinationIsVillage
                ? "[Chuyển Cảnh] Bạn đã đến Làng Bờ Rìa. Nơi đây u ám, cấm kỵ số 7, dân làng chỉ nhận trao đổi (barter)."
                : "[Chuyển Cảnh] Bạn đã trở lại khu chuồng trại chính.";
            Debug.Log($"<color=cyan>[TransitionZone]</color> {notifyMsg}");
            MobileGameController.Instance?.Engine?.LogMessage(notifyMsg);
        }

        private void ApplyAtmosphereTints(bool inVillage)
        {
            if (Camera.main != null)
            {
                // Khi ở Làng Bờ Rìa, bầu không khí u ám hơn với sắc thái lạnh (cool desaturated fog)
                Camera.main.backgroundColor = inVillage
                    ? new Color(0.12f, 0.15f, 0.18f)
                    : new Color(0.18f, 0.22f, 0.15f);
            }
        }

        public void EnsureUIExists()
        {
            if (overlayCanvasGroup != null) return;

            var existingCanvasObj = GameObject.Find("Transition_Overlay_Canvas");
            if (existingCanvasObj != null)
            {
                transitionCanvas = existingCanvasObj.GetComponent<Canvas>();
                overlayCanvasGroup = existingCanvasObj.GetComponent<CanvasGroup>();
                return;
            }

            var canvasObj = new GameObject("Transition_Overlay_Canvas");
            canvasObj.transform.SetParent(transform, false);
            transitionCanvas = canvasObj.AddComponent<Canvas>();
            transitionCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
            transitionCanvas.sortingOrder = 999; // Trên mọi UI thông thường

            var scaler = canvasObj.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;

            canvasObj.AddComponent<GraphicRaycaster>();
            overlayCanvasGroup = canvasObj.AddComponent<CanvasGroup>();
            overlayCanvasGroup.alpha = 0f;
            overlayCanvasGroup.blocksRaycasts = false;

            // Fullscreen Dark Background
            var bgObj = new GameObject("Dark_Background");
            bgObj.transform.SetParent(canvasObj.transform, false);
            var bgRt = bgObj.AddComponent<RectTransform>();
            bgRt.anchorMin = Vector2.zero;
            bgRt.anchorMax = Vector2.one;
            bgRt.offsetMin = Vector2.zero;
            bgRt.offsetMax = Vector2.zero;
            darkBackground = bgObj.AddComponent<Image>();
            darkBackground.color = new Color(0.04f, 0.05f, 0.07f, 0.98f);

            // Container Panel Parchment Center
            var cardObj = new GameObject("Lore_Card");
            cardObj.transform.SetParent(bgObj.transform, false);
            var cardRt = cardObj.AddComponent<RectTransform>();
            cardRt.anchorMin = new Vector2(0.5f, 0.5f);
            cardRt.anchorMax = new Vector2(0.5f, 0.5f);
            cardRt.pivot = new Vector2(0.5f, 0.5f);
            cardRt.sizeDelta = new Vector2(850f, 420f);

            var cardImg = cardObj.AddComponent<Image>();
            cardImg.sprite = UISpriteLoader.GetFrameParchment();
            cardImg.type = Image.Type.Sliced;
            cardImg.color = new Color(0.92f, 0.88f, 0.82f, 0.95f);

            // Zone Title
            var titleObj = new GameObject("Zone_Title");
            titleObj.transform.SetParent(cardObj.transform, false);
            var tRt = titleObj.AddComponent<RectTransform>();
            tRt.anchorMin = new Vector2(0f, 1f);
            tRt.anchorMax = new Vector2(1f, 1f);
            tRt.pivot = new Vector2(0.5f, 1f);
            tRt.anchoredPosition = new Vector2(0f, -30f);
            tRt.sizeDelta = new Vector2(-60f, 45f);

            zoneTitleText = titleObj.AddComponent<TextMeshProUGUI>();
            zoneTitleText.fontSize = 26f;
            zoneTitleText.fontStyle = FontStyles.Bold;
            zoneTitleText.color = new Color(0.35f, 0.12f, 0.06f);
            zoneTitleText.alignment = TextAlignmentOptions.Center;

            // Diary Quote Content
            var quoteObj = new GameObject("Diary_Quote");
            quoteObj.transform.SetParent(cardObj.transform, false);
            var qRt = quoteObj.AddComponent<RectTransform>();
            qRt.anchorMin = new Vector2(0f, 0f);
            qRt.anchorMax = new Vector2(1f, 1f);
            qRt.offsetMin = new Vector2(50f, 80f);
            qRt.offsetMax = new Vector2(-50f, -90f);

            diaryQuoteText = quoteObj.AddComponent<TextMeshProUGUI>();
            diaryQuoteText.fontSize = 19f;
            diaryQuoteText.fontStyle = FontStyles.Italic;
            diaryQuoteText.color = new Color(0.18f, 0.16f, 0.14f);
            diaryQuoteText.lineSpacing = 18f;
            diaryQuoteText.alignment = TextAlignmentOptions.Center;

            // Continue Prompt
            var promptObj = new GameObject("Prompt_Continue");
            promptObj.transform.SetParent(cardObj.transform, false);
            var pRt = promptObj.AddComponent<RectTransform>();
            pRt.anchorMin = new Vector2(0f, 0f);
            pRt.anchorMax = new Vector2(1f, 0f);
            pRt.pivot = new Vector2(0.5f, 0f);
            pRt.anchoredPosition = new Vector2(0f, 25f);
            pRt.sizeDelta = new Vector2(-60f, 35f);

            promptContinueText = promptObj.AddComponent<TextMeshProUGUI>();
            promptContinueText.fontSize = 14f;
            promptContinueText.fontStyle = FontStyles.Bold;
            promptContinueText.color = new Color(0.5f, 0.4f, 0.3f);
            promptContinueText.alignment = TextAlignmentOptions.Center;

            canvasObj.SetActive(false);
        }

        private void EnsurePromptUIExists()
        {
            if (promptCanvasObj != null) return;

            promptCanvasObj = new GameObject("TransitionPrompt_Canvas");
            promptCanvasObj.transform.SetParent(transform, false);

            var canvas = promptCanvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 500; // Dưới overlay 999 nhưng trên gameplay bình thường

            var scaler = promptCanvasObj.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;

            promptCanvasObj.AddComponent<GraphicRaycaster>();

            // Button đặt ở góc dưới giữa màn hình
            promptPanel = new GameObject("Prompt_Button");
            promptPanel.transform.SetParent(promptCanvasObj.transform, false);
            var pRt = promptPanel.AddComponent<RectTransform>();
            pRt.anchorMin = new Vector2(0.5f, 0f);
            pRt.anchorMax = new Vector2(0.5f, 0f);
            pRt.pivot = new Vector2(0.5f, 0f);
            pRt.anchoredPosition = new Vector2(0f, 105f);
            pRt.sizeDelta = new Vector2(500f, 76f);

            var img = promptPanel.AddComponent<Image>();
            img.sprite = UISpriteLoader.GetFrameWoodPanel();
            img.type = Image.Type.Sliced;
            img.color = new Color(1f, 1f, 1f, 0.98f);

            promptActionButton = promptPanel.AddComponent<Button>();
            promptActionButton.onClick.AddListener(OnPromptButtonClicked);

            // Text hiển thị bên trong nút
            var textObj = new GameObject("Text");
            textObj.transform.SetParent(promptPanel.transform, false);
            var tRt = textObj.AddComponent<RectTransform>();
            tRt.anchorMin = Vector2.zero;
            tRt.anchorMax = Vector2.one;
            tRt.offsetMin = new Vector2(16f, 6f);
            tRt.offsetMax = new Vector2(-16f, -6f);

            promptText = textObj.AddComponent<TextMeshProUGUI>();
            promptText.fontSize = 20f;
            promptText.color = new Color(0.96f, 0.92f, 0.82f);
            promptText.alignment = TextAlignmentOptions.Center;

            promptPanel.SetActive(false);
        }

        private void OnPromptButtonClicked()
        {
            if (activeListener != null)
            {
                activeListener.TriggerTransition();
            }
        }

        public void ShowInteractionPrompt(TransitionTriggerListener listener)
        {
            activeListener = listener;
            EnsurePromptUIExists();
            if (promptPanel != null)
            {
                string actionName = listener.IsTriggerToVillage ? "Đi Sang Làng Bờ Rìa" : "Trở Về Nông Trại";
                string subInfo = listener.IsTriggerToVillage ? "Nhấp chuột hoặc bấm [E] / [Space] để chuyển cảnh" : "Nhấp chuột hoặc bấm [E] / [Space] để quay về";
                if (promptText != null)
                {
                    promptText.text = $"<color=#ffdd88><b>[ {actionName} ]</b></color>\n<size=13><color=#e5dac5>{subInfo}</color></size>";
                }
                promptPanel.SetActive(true);
            }
        }

        public void HideInteractionPrompt(TransitionTriggerListener listener)
        {
            if (activeListener == listener)
            {
                activeListener = null;
                if (promptPanel != null) promptPanel.SetActive(false);
            }
        }

        private void CreateWorldTriggersIfMissing()
        {
            string currentScene = SceneManager.GetActiveScene().name;
            bool isVillage = string.Equals(currentScene, VillageSceneName, StringComparison.OrdinalIgnoreCase) || IsInVillage;

            if (isVillage)
            {
                // In Village Scene: Only need Trigger_Village_To_Farm
                var triggerVillage = GameObject.Find("Trigger_Village_To_Farm");
                if (triggerVillage == null)
                {
                    triggerVillage = new GameObject("Trigger_Village_To_Farm");
                    triggerVillage.transform.position = VillageEntrancePosition;
                    var col = triggerVillage.AddComponent<BoxCollider2D>();
                    col.isTrigger = true;
                    col.size = new Vector2(4.5f, 3f);
                    var listener = triggerVillage.AddComponent<TransitionTriggerListener>();
                    listener.IsTriggerToVillage = false;
                }
                else
                {
                    triggerVillage.transform.position = VillageEntrancePosition;
                }
            }
            else
            {
                // In Farm Scene: Only need Trigger_Farm_To_Village
                var triggerFarm = GameObject.Find("Trigger_Farm_To_Village");
                if (triggerFarm == null)
                {
                    triggerFarm = new GameObject("Trigger_Farm_To_Village");
                    triggerFarm.transform.position = FarmSouthGatePosition;
                    var col = triggerFarm.AddComponent<BoxCollider2D>();
                    col.isTrigger = true;
                    col.size = new Vector2(5f, 3f);
                    var listener = triggerFarm.AddComponent<TransitionTriggerListener>();
                    listener.IsTriggerToVillage = true;
                }
                else
                {
                    triggerFarm.transform.position = FarmSouthGatePosition;
                }
            }
        }
    }

    public class TransitionTriggerListener : MonoBehaviour
    {
        public bool IsTriggerToVillage = true;
        private bool isPlayerInside = false;

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (other.GetComponent<PlayerMobileController>() != null || other.CompareTag("Player"))
            {
                isPlayerInside = true;
                TransitionZoneController.Instance?.ShowInteractionPrompt(this);
            }
        }

        private void OnTriggerExit2D(Collider2D other)
        {
            if (other.GetComponent<PlayerMobileController>() != null || other.CompareTag("Player"))
            {
                isPlayerInside = false;
                TransitionZoneController.Instance?.HideInteractionPrompt(this);
            }
        }

        private void Update()
        {
            if (isPlayerInside)
            {
                if (Input.GetKeyDown(KeyCode.E) || Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.Return))
                {
                    TriggerTransition();
                }
            }
        }

        public void TriggerTransition()
        {
            if (TransitionZoneController.Instance != null && !TransitionZoneController.Instance.IsTransitioning)
            {
                TransitionZoneController.Instance.HideInteractionPrompt(this);
                if (IsTriggerToVillage && !TransitionZoneController.Instance.IsInVillage)
                {
                    TransitionZoneController.Instance.TravelToVillage();
                }
                else if (!IsTriggerToVillage && TransitionZoneController.Instance.IsInVillage)
                {
                    TransitionZoneController.Instance.TravelToFarm();
                }
            }
        }
    }
}
