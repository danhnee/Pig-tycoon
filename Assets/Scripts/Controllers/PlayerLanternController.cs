using UnityEngine;
using UnityEngine.Rendering.Universal;
using PigTycoon.Core;

namespace PigTycoon.Presentation
{
    /// <summary>
    /// Powerful Flashlight (Phương án 2):
    /// - Chiếc đèn pin kim loại pixel art cầm chắc chắn trên tay nhân vật.
    /// - Tia sáng hình nón (Spotlight Cone) quét sáng theo hướng di chuyển/nhìn của nhân vật.
    /// - Kết hợp URP Light2D Spotlight và hiệu ứng chùm sáng thể tích (Volumetric Light Beam).
    /// - Tự động bật ban đêm (18:30 - 06:00), đồng thời hỗ trợ phím F để bật/tắt thủ công mọi lúc.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class PlayerLanternController : MonoBehaviour
    {
        private const float BeamRange = 10f;
        private const float BeamAngle = 60f;

        private PlayerMobileController playerController;
        private SpriteRenderer playerSpriteRenderer;

        // Flashlight hierarchy
        private Transform flashlightRoot;
        private Transform flashlightPivot;
        private SpriteRenderer flashlightSpriteRenderer;
        private SpriteRenderer beamConeRenderer;
        private Light2D spotLight;
        private Light2D ambientFeetLight;

        // Sprites
        private static Sprite flashlightSprite;
        private static Sprite beamConeSprite;

        // Manual toggle
        private bool? manualOverrideState = null;

        public float CurrentLightRadius => BeamRange;

        private void Awake()
        {
            playerController = GetComponent<PlayerMobileController>();
            playerSpriteRenderer = playerController != null
                ? playerController.SpriteRenderer
                : GetComponent<SpriteRenderer>();

            EnsureSprites();
            BuildFlashlightHierarchy();
        }

        private void BuildFlashlightHierarchy()
        {
            // Root container
            var rootObj = new GameObject("PlayerFlashlightRig");
            flashlightRoot = rootObj.transform;
            flashlightRoot.SetParent(transform, false);
            flashlightRoot.localPosition = new Vector3(0.2f, -0.1f, 0f);

            // Pivot for rotating towards facing direction
            var pivotObj = new GameObject("FlashlightPivot");
            flashlightPivot = pivotObj.transform;
            flashlightPivot.SetParent(flashlightRoot, false);
            flashlightPivot.localPosition = Vector3.zero;

            // 1. Flashlight device sprite (cầm trên tay)
            var deviceObj = new GameObject("FlashlightDevice");
            deviceObj.transform.SetParent(flashlightPivot, false);
            deviceObj.transform.localPosition = new Vector3(0f, 0.15f, 0f);
            flashlightSpriteRenderer = deviceObj.AddComponent<SpriteRenderer>();
            flashlightSpriteRenderer.sprite = flashlightSprite;
            flashlightSpriteRenderer.sortingLayerID = playerSpriteRenderer != null ? playerSpriteRenderer.sortingLayerID : 0;

            // 2. Visible Volumetric Light Beam Cone (Vệt sáng hình nón nhìn thấy được)
            var coneObj = new GameObject("FlashlightBeamCone");
            coneObj.transform.SetParent(flashlightPivot, false);
            coneObj.transform.localPosition = new Vector3(0f, 0.35f, 0f); // Starts right at the lens
            coneObj.transform.localScale = new Vector3(3.6f, 6.5f, 1f);
            beamConeRenderer = coneObj.AddComponent<SpriteRenderer>();
            beamConeRenderer.sprite = beamConeSprite;
            beamConeRenderer.color = new Color(1f, 0.98f, 0.88f, 0.75f);
            beamConeRenderer.sortingLayerID = playerSpriteRenderer != null ? playerSpriteRenderer.sortingLayerID : 0;

            // 3. URP Light2D Focused Spotlight (Chiếu sáng mặt đất và cảnh vật 2D)
            var spotObj = new GameObject("FlashlightSpotLight2D");
            spotObj.transform.SetParent(flashlightPivot, false);
            spotObj.transform.localPosition = new Vector3(0f, 0.35f, 0f);
            spotLight = spotObj.AddComponent<Light2D>();
            spotLight.lightType = Light2D.LightType.Point;
            spotLight.pointLightInnerAngle = 28f;
            spotLight.pointLightOuterAngle = BeamAngle;
            spotLight.pointLightInnerRadius = 0.5f;
            spotLight.pointLightOuterRadius = BeamRange;
            spotLight.color = new Color(1f, 0.96f, 0.82f, 1f);
            spotLight.intensity = 1.15f;
            spotLight.blendStyleIndex = 0;

            // 4. Soft secondary light around player feet (quầng sáng phụ dưới chân)
            var ambientObj = new GameObject("FlashlightAmbientFeet");
            ambientObj.transform.SetParent(flashlightRoot, false);
            ambientObj.transform.localPosition = Vector3.zero;
            ambientFeetLight = ambientObj.AddComponent<Light2D>();
            ambientFeetLight.lightType = Light2D.LightType.Point;
            ambientFeetLight.pointLightInnerRadius = 0.2f;
            ambientFeetLight.pointLightOuterRadius = 2.6f;
            ambientFeetLight.color = new Color(1f, 0.88f, 0.65f, 0.6f);
            ambientFeetLight.intensity = 0.45f;
            ambientFeetLight.blendStyleIndex = 0;

            flashlightRoot.gameObject.SetActive(false);
        }

        private void Update()
        {
            // Phím F: Cho phép người chơi chủ động bật/tắt đèn pin bất cứ lúc nào
            if (Input.GetKeyDown(KeyCode.F))
            {
                bool currentActive = flashlightRoot != null && flashlightRoot.gameObject.activeSelf;
                manualOverrideState = !currentActive;
            }
        }

        private void LateUpdate()
        {
            if (flashlightRoot == null)
                return;

            GameEngine engine = MobileGameController.Instance != null
                ? MobileGameController.Instance.Engine
                : null;
            bool isNight = engine != null && engine.Clock != null && engine.Clock.IsNight;

            // Nếu người chơi nhấn phím F thì ưu tiên trạng thái thủ công, ngược lại tự động theo ngày/đêm
            bool shouldBeActive = manualOverrideState.HasValue ? manualOverrideState.Value : isNight;

            if (flashlightRoot.gameObject.activeSelf != shouldBeActive)
                flashlightRoot.gameObject.SetActive(shouldBeActive);

            if (!shouldBeActive)
                return;

            UpdateFlashlightAimAndPose();
        }

        private void UpdateFlashlightAimAndPose()
        {
            Vector2 facing = playerController != null
                ? playerController.FacingDirection
                : Vector2.down;

            if (facing.sqrMagnitude < 0.001f)
                facing = Vector2.down;

            facing.Normalize();

            // Tính góc quay theo hướng nhân vật (hướng lên là 0 độ trong sprite cone)
            float angleDeg = Mathf.Atan2(facing.y, facing.x) * Mathf.Rad2Deg - 90f;
            if (flashlightPivot != null)
                flashlightPivot.rotation = Quaternion.Euler(0f, 0f, angleDeg);

            // Tùy theo hướng nhìn mà điều chỉnh vị trí cầm tay và độ sâu hiển thị (Sorting Order)
            bool sideFacing = Mathf.Abs(facing.x) > Mathf.Abs(facing.y) * 0.75f;
            bool facingUp = !sideFacing && facing.y > 0f;

            int sortingBase = playerSpriteRenderer != null ? playerSpriteRenderer.sortingOrder : 0;
            int sortingLayer = playerSpriteRenderer != null ? playerSpriteRenderer.sortingLayerID : 0;

            if (sideFacing)
            {
                float side = facing.x >= 0f ? 1f : -1f;
                flashlightRoot.localPosition = new Vector3(side * 0.28f, -0.12f, 0f);

                // Cầm tay bên cạnh: hiển thị phía trước nhân vật
                if (flashlightSpriteRenderer != null)
                {
                    flashlightSpriteRenderer.sortingLayerID = sortingLayer;
                    flashlightSpriteRenderer.sortingOrder = sortingBase + 1;
                }
                if (beamConeRenderer != null)
                {
                    beamConeRenderer.sortingLayerID = sortingLayer;
                    beamConeRenderer.sortingOrder = sortingBase + 1;
                }
            }
            else if (facingUp)
            {
                flashlightRoot.localPosition = new Vector3(0.20f, 0.08f, 0f);

                // Nhìn lên trên: đèn và chùm sáng ẩn sau lưng nhân vật
                if (flashlightSpriteRenderer != null)
                {
                    flashlightSpriteRenderer.sortingLayerID = sortingLayer;
                    flashlightSpriteRenderer.sortingOrder = sortingBase - 1;
                }
                if (beamConeRenderer != null)
                {
                    beamConeRenderer.sortingLayerID = sortingLayer;
                    beamConeRenderer.sortingOrder = sortingBase - 1;
                }
            }
            else
            {
                // Nhìn xuống dưới (mặc định): đèn nằm trước ngực/tay, chùm sáng chiếu xuống trước
                flashlightRoot.localPosition = new Vector3(0.18f, -0.25f, 0f);

                if (flashlightSpriteRenderer != null)
                {
                    flashlightSpriteRenderer.sortingLayerID = sortingLayer;
                    flashlightSpriteRenderer.sortingOrder = sortingBase + 2;
                }
                if (beamConeRenderer != null)
                {
                    beamConeRenderer.sortingLayerID = sortingLayer;
                    beamConeRenderer.sortingOrder = sortingBase + 1;
                }
            }
        }

        #region Procedural Sprite Generation
        private static void EnsureSprites()
        {
            if (flashlightSprite == null)
                flashlightSprite = GenerateFlashlightSprite();

            if (beamConeSprite == null)
                beamConeSprite = GenerateBeamConeSprite();
        }

        private static Sprite GenerateFlashlightSprite()
        {
            const int w = 14;
            const int h = 24;
            var pixels = new Color32[w * h];

            Color32 metalDark = new Color32(38, 42, 48, 255);
            Color32 metalGrip = new Color32(235, 175, 45, 255); // Safety yellow grip ring
            Color32 metalBezel = new Color32(185, 195, 205, 255); // Silver bezel
            Color32 lensGlow = new Color32(255, 255, 230, 255); // Bright illuminated lens

            // Handle (y: 2 - 14)
            for (int y = 2; y <= 14; y++)
            for (int x = 4; x <= 9; x++)
            {
                pixels[y * w + x] = (y == 5 || y == 9 || y == 13) ? metalGrip : metalDark;
            }

            // Body widen (y: 15 - 19)
            for (int y = 15; y <= 19; y++)
            {
                int expand = (y - 15) / 2;
                for (int x = 4 - expand; x <= 9 + expand; x++)
                    pixels[y * w + x] = metalDark;
            }

            // Head & Bezel (y: 20 - 22)
            for (int y = 20; y <= 21; y++)
            for (int x = 2; x <= 11; x++)
                pixels[y * w + x] = metalBezel;

            // Glass Lens (y: 22 - 23)
            for (int y = 22; y < h; y++)
            for (int x = 3; x <= 10; x++)
                pixels[y * w + x] = lensGlow;

            var texture = new Texture2D(w, h, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Clamp
            };
            texture.SetPixels32(pixels);
            texture.Apply(false, true);

            // Pivot at bottom handle
            return Sprite.Create(texture, new Rect(0, 0, w, h), new Vector2(0.5f, 0.2f), 32f);
        }

        private static Sprite GenerateBeamConeSprite()
        {
            const int w = 64;
            const int h = 128;
            var pixels = new Color32[w * h];

            float apexX = w * 0.5f;
            float apexY = 4f;

            for (int y = 0; y < h; y++)
            {
                float tY = Mathf.Clamp01((y - apexY) / (h - apexY));
                if (y < apexY) continue;

                // Chiều rộng chùm sáng nở dần theo chiều cao
                float halfWidth = Mathf.Lerp(3f, w * 0.48f, tY);
                float xMin = apexX - halfWidth;
                float xMax = apexX + halfWidth;

                // Độ mờ dần theo khoảng cách (sáng mạnh ở đầu đèn, mờ dần về xa)
                float lengthFalloff = Mathf.Pow(1f - tY, 1.25f);

                for (int x = 0; x < w; x++)
                {
                    if (x >= xMin && x <= xMax)
                    {
                        // Độ mờ ở hai bên rìa hình nón (soft edge)
                        float edgeDist = Mathf.Abs(x - apexX) / halfWidth;
                        float edgeFalloff = Mathf.Clamp01(1f - edgeDist * edgeDist);

                        float alpha = lengthFalloff * edgeFalloff * 0.65f;
                        byte a = (byte)(Mathf.Clamp01(alpha) * 255f);

                        // Ánh sáng vàng ấm dịu mắt
                        pixels[y * w + x] = new Color32(255, 248, 205, a);
                    }
                }
            }

            var texture = new Texture2D(w, h, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp
            };
            texture.SetPixels32(pixels);
            texture.Apply(false, true);

            // Pivot tại gốc chùm sáng (ngay đầu đèn pin)
            return Sprite.Create(texture, new Rect(0, 0, w, h), new Vector2(0.5f, 0.03f), 32f);
        }
        #endregion
    }
}
