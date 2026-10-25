using UnityEngine;
using UnityEngine.UI;
using TMPro;
using PigTycoon.Core;

namespace PigTycoon.Presentation
{
    public class MobileHUDController : MonoBehaviour
    {
        [Header("Top Bar UI")]
        public TextMeshProUGUI TimeText;
        public TextMeshProUGUI GoldText;
        public TextMeshProUGUI HabitatText;
        public TextMeshProUGUI EventPressureText;
        public TextMeshProUGUI HerdStateText;
        public TextMeshProUGUI CapacityText;

        [Header("Combat Action Buttons")]
        public Button SkillAttackButton;   // Thế Công
        public Button SkillDefendButton;   // Thế Thủ
        public Button SkillRageButton;     // Thế Biến
        public Button BachVanTriggerButton;// Kích hoạt Bạch Vân thủ công
        public Button DefenseMenuButton;   // Bật/tắt menu Xưởng Phòng Thủ

        [Header("Player Reference")]
        public PlayerMobileController PlayerController;

        private void Start()
        {
            if (MobileGameController.Instance != null)
            {
                MobileGameController.Instance.OnStateUpdated += RefreshHUD;
            }

            if (SkillAttackButton != null)
            {
                SkillAttackButton.onClick.AddListener(() => PlayerController?.ExecuteMartialSkill(CombatSlotType.TheCong));
            }
            if (SkillDefendButton != null)
            {
                SkillDefendButton.onClick.AddListener(() => PlayerController?.ExecuteMartialSkill(CombatSlotType.TheThu));
            }
            if (SkillRageButton != null)
            {
                SkillRageButton.onClick.AddListener(() => PlayerController?.ExecuteMartialSkill(CombatSlotType.TheBien));
            }
            if (BachVanTriggerButton != null)
            {
                BachVanTriggerButton.onClick.AddListener(OnBachVanTriggerClicked);
            }
            if (DefenseMenuButton != null)
            {
                DefenseMenuButton.onClick.AddListener(() => DefenseBuildPopup.Instance?.Toggle());
            }

            EnsureCombatArenaButton();
            RefreshHUD();
        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.F8))
            {
                UnityEngine.SceneManagement.SceneManager.LoadScene("CombatTestArena2D");
            }
        }

        private void EnsureCombatArenaButton()
        {
            var canvas = GetComponentInParent<Canvas>() ?? FindAnyObjectByType<Canvas>();
            if (canvas == null) return;

            var existing = canvas.transform.Find("Btn_GoCombatArena");
            if (existing != null) return;

            var btnObj = new GameObject("Btn_GoCombatArena", typeof(RectTransform));
            btnObj.transform.SetParent(canvas.transform, false);

            var rt = btnObj.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(1f, 1f);
            rt.anchorMax = new Vector2(1f, 1f);
            rt.pivot = new Vector2(1f, 1f);
            rt.anchoredPosition = new Vector2(-16f, -16f);
            rt.sizeDelta = new Vector2(190f, 42f);

            var img = btnObj.AddComponent<Image>();
            img.sprite = UISpriteLoader.GetFrameWoodPanel();
            img.type = Image.Type.Sliced;
            img.color = new Color(0.12f, 0.16f, 0.24f, 0.95f);

            var btn = btnObj.AddComponent<Button>();
            btn.targetGraphic = img;
            btn.onClick.AddListener(() =>
            {
                UnityEngine.SceneManagement.SceneManager.LoadScene("CombatTestArena2D");
            });

            var textObj = new GameObject("Text", typeof(RectTransform));
            textObj.transform.SetParent(btnObj.transform, false);
            var textRt = textObj.GetComponent<RectTransform>();
            textRt.anchorMin = Vector2.zero;
            textRt.anchorMax = Vector2.one;
            textRt.sizeDelta = Vector2.zero;

            var txt = textObj.AddComponent<TextMeshProUGUI>();
            txt.text = "⚔️ ĐẤU TRƯỜNG (F8)";
            txt.fontSize = 13f;
            txt.fontStyle = FontStyles.Bold;
            txt.alignment = TextAlignmentOptions.Center;
            txt.color = new Color(1f, 0.85f, 0.25f, 1f);
        }

        private void OnDestroy()
        {
            if (MobileGameController.Instance != null)
            {
                MobileGameController.Instance.OnStateUpdated -= RefreshHUD;
            }
        }

        public void RefreshHUD()
        {
            if (MobileGameController.Instance == null || MobileGameController.Instance.Engine == null) return;

            var engine = MobileGameController.Instance.Engine;
            var clock = engine.Clock;
            var farm = engine.Farm;

            if (TimeText != null)
            {
                TimeText.text = $"Ngày {clock.CurrentDay} | {clock.CurrentHour:00}:{clock.CurrentMinute:00} ({clock.GetTimeOfDay()})";
            }

            if (GoldText != null)
            {
                GoldText.text = $"{farm.Gold:N0} G";
            }

            if (HabitatText != null)
            {
                HabitatText.text = $"SC: {farm.HabitatIndex}/100 [{farm.HabitatTier}]";
            }

            if (EventPressureText != null)
            {
                EventPressureText.text = $"ÁLSK: {farm.EventPressureIndex}/100 [{farm.EventPressureState}]";
            }

            if (HerdStateText != null)
            {
                HerdStateText.text = $"Bầy Đàn: [{engine.Herd.Tier}] | TT: {engine.Herd.TransmissionStacks}/3";
            }

            if (CapacityText != null)
            {
                var living = engine.Pigs.FindAll(p => p.IsAlive);
                var cap = farm.CalculateCapacityReport(living.Count, 0);
                CapacityText.text = $"Heo: {living.Count}/{cap.EffectiveCapacity} ({(cap.RawDensityRatio * 100):0.0}%)";
            }
        }

        private void OnBachVanTriggerClicked()
        {
            if (MobileGameController.Instance == null) return;
            var def = MobileGameController.Instance.Engine.Defense;

            var bv = def.Buildings.Find(b => b.IsBachVan && !b.IsBroken);
            if (bv != null)
            {
                var res = def.TriggerBachVanManual(bv.InstanceId);
                Debug.Log($"[Bạch Vân] {res.message}");
                PlayerInteractionController.Instance?.ShowFeedback(res.message);

                // Kích hoạt sấm sét toàn trại từ Thần Khí trong scene
                if (res.success)
                {
                    var towers = FindObjectsByType<DefenseTower2DView>();
                    foreach (var t in towers)
                    {
                        if (t.BuildingModel != null && t.BuildingModel.BlueprintId == "CuuTieuLoiPhao")
                        {
                            t.ExecuteBachVanMegaLightning();
                            break;
                        }
                    }
                }
            }
            else
            {
                PlayerInteractionController.Instance?.ShowFeedback("Chưa có công trình Bạch Vân nào sẵn sàng!");
            }
        }
    }
}
