using PigTycoon.Core;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace PigTycoon.Presentation
{
    /// <summary>
    /// GDD 21 on the PixelMyth farm: Bí nhân stands just outside the fence (within 30 m, on the outer trail) only while
    /// Core says he is here (EconomyManager.UpdateMysticMerchantHour: one roll a night, 2 game hours), marked by a
    /// green lantern. E within 1.8 m opens his barter counter (VillageBarterPopup, VillagerId.BiNhan — never gold).
    /// Spots are the layout's trail points; tonight's spot follows his arrival hour.
    /// </summary>
    public sealed class MysticMerchantView : MonoBehaviour
    {
        public Vector2[] Spots = { new Vector2(23.5f, 110f) };
        private SpriteRenderer sr;
        private Light2D lantern;
        private PixelMythPlayer anim;
        private bool shown;
        private const float TalkRange = 1.8f;

        private void Start()
        {
            sr = gameObject.AddComponent<SpriteRenderer>();
            var lib = PixelMythRegistry.Find("bi_nhan");
            if (lib == null)
            {
                lib = PixelMythRegistry.Find("man");
                sr.color = new Color(0.55f, 0.45f, 0.7f, 1f);   // stand-in until a Bí nhân sheet exists: a shadowed figure
            }
            if (lib != null) { anim = new PixelMythPlayer(lib, sr); anim.SetBase("idle"); }
            var lightGo = new GameObject("GreenLantern");
            lightGo.transform.SetParent(transform, false);
            lightGo.transform.localPosition = new Vector3(0.55f, 1.1f, 0f);
            lantern = lightGo.AddComponent<Light2D>();
            lantern.lightType = Light2D.LightType.Point;
            lantern.color = new Color(0.35f, 1f, 0.55f);
            lantern.intensity = 1.6f;
            lantern.pointLightOuterRadius = 4.5f;
            lantern.pointLightInnerRadius = 0.4f;
            SetShown(false);
        }

        private void SetShown(bool on)
        {
            shown = on;
            if (sr != null) sr.enabled = on;
            if (lantern != null) lantern.enabled = on;
        }

        private void Update()
        {
            var engine = MobileGameController.Instance != null ? MobileGameController.Instance.Engine : null;
            if (engine == null) return;
            var m = engine.Economy.MysticMerchant;
            if (m.IsActive != shown)
            {
                if (m.IsActive && Spots.Length > 0)
                {
                    var spot = Spots[Mathf.Abs(m.SpawnTimeHour) % Spots.Length];
                    transform.position = new Vector3(spot.x, spot.y, 0f);
                    PlayerInteractionController.Instance?.ShowFeedback("Một ngọn đèn lồng xanh vừa sáng lên ngoài rào...");
                }
                SetShown(m.IsActive);
            }
            if (!shown) return;
            sr.sortingOrder = Mathf.RoundToInt(-transform.position.y * 100f);
            lantern.intensity = 1.4f + 0.25f * Mathf.Sin(Time.time * 3.1f);
            anim?.Tick(Time.deltaTime, CharacterFacing.Down);
            if (!Input.GetKeyDown(KeyCode.E) || BackpackPopup.InputBlockedThisFrame) return;
            var p = PlayerMobileController.Instance;
            if (p == null || Vector2.Distance(p.transform.position, transform.position) > TalkRange) return;
            var popup = VillageBarterPopup.Instance;
            if (popup == null)
            {
                var canvas = VillageBarterPopup.FindScreenSpaceCanvas();
                if (canvas != null) popup = canvas.gameObject.AddComponent<VillageBarterPopup>();
            }
            popup?.OpenForVillager(VillagerId.BiNhan);
        }
    }
}
