using UnityEngine;
using UnityEngine.UI;
using TMPro;
using PigTycoon.Core;

namespace PigTycoon.Presentation
{
    public class PlayerInteractionController : MonoBehaviour
    {
        public static PlayerInteractionController Instance { get; private set; }

        [Header("Interaction Settings")]
        public float InteractionRadius = 2.5f;

        [Header("Context Action Button UI")]
        public GameObject ActionButtonRoot;
        public Button ActionButton;
        public TextMeshProUGUI ActionButtonText;
        public TextMeshProUGUI FeedbackFloatingText;

        private Component currentTarget;
        private string currentPrompt = "";
        private float feedbackTimer = 0f;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            if (ActionButton != null)
            {
                ActionButton.onClick.AddListener(PerformAction);
            }
        }

        private void Update()
        {
            DetectNearestInteractable();

            // Phím Space hoặc E trên bàn phím cũng kích hoạt tương tác
            if (Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.E))
            {
                PerformAction();
            }

            // Xử lý ẩn feedback text
            if (feedbackTimer > 0f)
            {
                feedbackTimer -= Time.deltaTime;
                if (feedbackTimer <= 0f && FeedbackFloatingText != null)
                {
                    FeedbackFloatingText.gameObject.SetActive(false);
                }
            }
        }

        private void DetectNearestInteractable()
        {
            Vector2 playerPos = transform.position;
            Component bestTarget = null;
            float minDistSqr = InteractionRadius * InteractionRadius;

            // 1. Kiểm tra Heo gần nhất
            var pigs = FindObjectsByType<PigAgentView>();
            foreach (var p in pigs)
            {
                if (p == null || p.PigModel == null || !p.PigModel.IsAlive) continue;
                float dSqr = ((Vector2)p.transform.position - playerPos).sqrMagnitude;
                if (dSqr < minDistSqr && dSqr < 4.8f)
                {
                    minDistSqr = dSqr;
                    bestTarget = p;
                }
            }

            // 2. Kiểm tra Máng Ăn
            var feeders = FindObjectsByType<Feeder2DView>();
            foreach (var f in feeders)
            {
                if (f == null) continue;
                float dSqr = ((Vector2)f.transform.position - playerPos).sqrMagnitude;
                if (dSqr < minDistSqr && dSqr < 7.5f)
                {
                    minDistSqr = dSqr;
                    bestTarget = f;
                }
            }

            // 3. Kiểm tra Bồn Nước
            var troughs = FindObjectsByType<WaterTrough2DView>();
            foreach (var w in troughs)
            {
                if (w == null) continue;
                float dSqr = ((Vector2)w.transform.position - playerPos).sqrMagnitude;
                if (dSqr < minDistSqr && dSqr < 7.5f)
                {
                    minDistSqr = dSqr;
                    bestTarget = w;
                }
            }

            // 4. Kiểm tra Hàng Rào Hỏng
            var fences = FindObjectsByType<Fence2DView>();
            foreach (var fence in fences)
            {
                if (fence == null) continue;
                float dSqr = ((Vector2)fence.transform.position - playerPos).sqrMagnitude;
                if (dSqr < minDistSqr && (fence.CurrentHp < fence.MaxHp || dSqr < 3.5f))
                {
                    minDistSqr = dSqr;
                    bestTarget = fence;
                }
            }

            // 5. Kiểm tra Khu Xử Lý
            var corpseLot = FindAnyObjectByType<CorpseLot2DView>();
            if (corpseLot != null)
            {
                float dSqr = ((Vector2)corpseLot.transform.position - playerPos).sqrMagnitude;
                if (dSqr < minDistSqr && dSqr < 9f)
                {
                    minDistSqr = dSqr;
                    bestTarget = corpseLot;
                }
            }

            currentTarget = bestTarget;
            UpdateActionButtonVisual();
        }

        private void UpdateActionButtonVisual()
        {
            if (currentTarget == null)
            {
                if (ActionButtonRoot != null) ActionButtonRoot.SetActive(false);
                return;
            }

            if (ActionButtonRoot != null) ActionButtonRoot.SetActive(true);

            var tool = HotbarController.Instance != null ? HotbarController.Instance.CurrentTool : StardewToolType.CamHat;

            if (currentTarget is Feeder2DView)
            {
                currentPrompt = "🌾 Đổ Cám (20kg)";
            }
            else if (currentTarget is WaterTrough2DView)
            {
                currentPrompt = "💧 Bơm Nước (50L)";
            }
            else if (currentTarget is Fence2DView fence)
            {
                currentPrompt = fence.CurrentHp < fence.MaxHp ? $"🔨 Sửa Rào (+75 HP) [{fence.CurrentHp:0}/{fence.MaxHp}]" : "🔨 Gia Cố Rào";
            }
            else if (currentTarget is PigAgentView pig)
            {
                pig.EnsurePigModel();
                string pName = pig.PigModel != null ? pig.PigModel.Name : "Heo";
                if (tool == StardewToolType.BanChai)
                {
                    currentPrompt = $"❤️ Vuốt Ve {pName}";
                }
                else if (tool == StardewToolType.KinhLup)
                {
                    currentPrompt = $"🔍 Soi Gen {pName}";
                }
                else
                {
                    currentPrompt = $"📋 Xem {pName}";
                }
            }
            else if (currentTarget is CorpseLot2DView)
            {
                currentPrompt = "🧹 Vệ Sinh Khu Xử Lý";
            }

            if (ActionButtonText != null)
            {
                ActionButtonText.text = currentPrompt;
            }
        }

        public void PerformAction()
        {
            if (currentTarget == null) return;

            var engine = MobileGameController.Instance?.Engine;
            var stamina = engine?.Character?.Stamina;

            if (currentTarget is Feeder2DView feeder)
            {
                if (stamina != null && stamina.CurrentStamina < 5f)
                {
                    ShowFeedback("⚠️ Kiệt sức! Cần nghỉ ngơi.");
                    return;
                }
                feeder.Refill(20f);
                if (stamina != null) stamina.CurrentStamina -= 5f;
                ShowFeedback("🌾 Đã đổ thêm 20kg Cám vào máng!");
            }
            else if (currentTarget is WaterTrough2DView water)
            {
                if (stamina != null && stamina.CurrentStamina < 5f)
                {
                    ShowFeedback("⚠️ Kiệt sức! Cần nghỉ ngơi.");
                    return;
                }
                water.Refill(50f);
                if (stamina != null) stamina.CurrentStamina -= 5f;
                ShowFeedback("💧 Đã bơm thêm 50L Nước!");
            }
            else if (currentTarget is Fence2DView fence)
            {
                if (stamina != null && stamina.CurrentStamina < 8f)
                {
                    ShowFeedback("⚠️ Kiệt sức! Cần nghỉ ngơi.");
                    return;
                }
                fence.Repair(75f);
                if (stamina != null) stamina.CurrentStamina -= 8f;
                ShowFeedback($"🔨 Đã sửa rào! HP: {fence.CurrentHp:0}/{fence.MaxHp:0}");
            }
            else if (currentTarget is PigAgentView pigAgent)
            {
                pigAgent.EnsurePigModel();
                if (pigAgent.PigModel == null) return;

                var tool = HotbarController.Instance != null ? HotbarController.Instance.CurrentTool : StardewToolType.CamHat;
                if (tool == StardewToolType.BanChai)
                {
                    if (stamina != null && stamina.CurrentStamina < 4f)
                    {
                        ShowFeedback("⚠️ Kiệt sức! Cần nghỉ ngơi.");
                        return;
                    }
                    pigAgent.PigModel.Bonding = Mathf.Min(100f, pigAgent.PigModel.Bonding + 12f);
                    pigAgent.PigModel.Mood = Mathf.Min(100, pigAgent.PigModel.Mood + 15);
                    if (stamina != null) stamina.CurrentStamina -= 4f;
                    ShowFeedback($"❤️ {pigAgent.PigModel.Name} thích thú! (Bonding +12%)");
                }
                else
                {
                    // Mở popup soi chi tiết heo chuẩn Stardew Valley
                    if (PigInspectPopup.Instance != null)
                    {
                        PigInspectPopup.Instance.Show(pigAgent.PigModel);
                    }
                }
            }
            else if (currentTarget is CorpseLot2DView)
            {
                if (stamina != null && stamina.CurrentStamina < 10f)
                {
                    ShowFeedback("⚠️ Kiệt sức! Cần nghỉ ngơi.");
                    return;
                }
                if (engine != null)
                {
                    engine.Farm.EventPressureIndex = Mathf.Max(0, engine.Farm.EventPressureIndex - 8);
                }
                if (stamina != null) stamina.CurrentStamina -= 10f;
                ShowFeedback("🧹 Đã vệ sinh tiêu độc! (Ám khí -8)");
            }

            GetComponent<CharacterSpriteAnimator>()?.TriggerAction();

            MobileGameController.Instance?.OnStateUpdated?.Invoke();
        }

        public void ShowFeedback(string message)
        {
            if (FeedbackFloatingText != null)
            {
                FeedbackFloatingText.text = message;
                FeedbackFloatingText.gameObject.SetActive(true);
                feedbackTimer = 2.5f;
            }
        }
    }
}
