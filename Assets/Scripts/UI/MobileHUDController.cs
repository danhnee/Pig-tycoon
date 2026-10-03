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

            RefreshHUD();
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
            }
            else
            {
                Debug.LogWarning("[Bạch Vân] Chưa có công trình Bạch Vân nào sẵn sàng!");
            }
        }
    }
}
