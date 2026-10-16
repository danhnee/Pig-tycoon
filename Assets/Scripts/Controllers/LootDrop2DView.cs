using System.Collections;
using UnityEngine;
using TMPro;
using PigTycoon.Core;

namespace PigTycoon.Presentation
{
    public class LootDrop2DView : MonoBehaviour
    {
        public LootItemData ItemData { get; private set; }

        [Header("Magnet Settings")]
        public float MagnetRadius = 2.2f;
        public float FlySpeed = 6.5f;

        private SpriteRenderer spriteRenderer;
        private GameObject lootBeamObj;
        private Transform playerTransform;
        private bool isAttracted = false;
        private bool isCollected = false;
        private Vector3 groundOffset;

        public static LootDrop2DView Spawn(LootItemData data, Vector2 originPos)
        {
            var go = new GameObject($"Loot_{data.Id}_{data.Quantity}");
            go.transform.position = originPos;

            var loot = go.AddComponent<LootDrop2DView>();
            loot.Initialize(data, originPos);
            return loot;
        }

        private void Initialize(LootItemData data, Vector2 originPos)
        {
            ItemData = data;

            // 1. Sprite Renderer
            spriteRenderer = gameObject.AddComponent<SpriteRenderer>();
            spriteRenderer.sprite = UISpriteLoader.GetLootSprite(data.SpriteKey);
            spriteRenderer.sortingOrder = 180;
            transform.localScale = new Vector3(1.2f, 1.2f, 1f);

            // 2. Cột sáng (Loot Beam & Glow) theo màu độ hiếm
            CreateLootGlow(data.Rarity);

            // 3. Hiệu ứng nảy tung ra (Parabolic Bounce)
            Vector2 dropOffset = UnityEngine.Random.insideUnitCircle.normalized * UnityEngine.Random.Range(0.6f, 1.3f);
            Vector2 targetLandPos = originPos + dropOffset;
            StartCoroutine(BounceRoutine(originPos, targetLandPos));
        }

        private void CreateLootGlow(LootRarity rarity)
        {
            Color rarityColor = GetRarityUnityColor(rarity);

            // Vòng hào quang sáng dưới chân
            lootBeamObj = new GameObject("LootGlow");
            lootBeamObj.transform.SetParent(transform, false);
            lootBeamObj.transform.localPosition = new Vector3(0f, -0.15f, 0f);

            var glowSr = lootBeamObj.AddComponent<SpriteRenderer>();
            glowSr.sprite = UISpriteLoader.GetFrameSlotNormal();
            glowSr.color = new Color(rarityColor.r, rarityColor.g, rarityColor.b, 0.45f);
            glowSr.sortingOrder = 175;
            lootBeamObj.transform.localScale = new Vector3(0.5f, 0.15f, 1f);

            // Nếu là vật phẩm Hiếm hoặc Cực phẩm: Thêm cột sáng mờ chiếu thẳng lên
            if (rarity == LootRarity.Hiem || rarity == LootRarity.CucPham)
            {
                var beamObj = new GameObject("VerticalBeam");
                beamObj.transform.SetParent(transform, false);
                beamObj.transform.localPosition = new Vector3(0f, 0.6f, 0f);

                var beamSr = beamObj.AddComponent<SpriteRenderer>();
                beamSr.sprite = UISpriteLoader.GetFrameSlotNormal();
                beamSr.color = new Color(rarityColor.r, rarityColor.g, rarityColor.b, 0.25f);
                beamSr.sortingOrder = 176;
                beamObj.transform.localScale = new Vector3(0.08f, 1.2f, 1f);
            }
        }

        private IEnumerator BounceRoutine(Vector2 startPos, Vector2 endPos)
        {
            float duration = 0.45f;
            float elapsed = 0f;
            float arcHeight = 0.8f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);

                // Parabolic arc: 4 * t * (1 - t)
                float currentY = Mathf.Lerp(startPos.y, endPos.y, t) + (4f * t * (1f - t) * arcHeight);
                float currentX = Mathf.Lerp(startPos.x, endPos.x, t);

                transform.position = new Vector3(currentX, currentY, 0f);
                yield return null;
            }

            transform.position = new Vector3(endPos.x, endPos.y, 0f);
            groundOffset = transform.position;
        }

        private void Update()
        {
            if (isCollected) return;

            // Tìm người chơi
            if (playerTransform == null)
            {
                var player = FindAnyObjectByType<PlayerInteractionController>();
                if (player != null) playerTransform = player.transform;
            }

            if (playerTransform == null) return;

            float dist = Vector2.Distance(transform.position, playerTransform.position);

            // Kiểm tra nam châm hút (Magnet)
            if (dist <= MagnetRadius)
            {
                isAttracted = true;
            }

            if (isAttracted)
            {
                // Bay lướt nhanh về phía người chơi
                transform.position = Vector3.MoveTowards(transform.position, playerTransform.position, FlySpeed * Time.deltaTime);

                if (dist <= 0.35f)
                {
                    Collect();
                }
            }
            else
            {
                // Dao động nhẹ trên mặt cỏ
                if (groundOffset != Vector3.zero)
                {
                    float floatY = Mathf.Sin(Time.time * 3.5f) * 0.04f;
                    transform.position = groundOffset + new Vector3(0f, floatY, 0f);
                }
            }
        }

        private void Collect()
        {
            if (isCollected) return;
            isCollected = true;

            var player = playerTransform?.GetComponent<PlayerInteractionController>();
            var engine = MobileGameController.Instance?.Engine;

            string feedbackText = $"+{ItemData.Quantity} {ItemData.DisplayName}";

            if (ItemData.Category == LootCategory.Currency)
            {
                int goldAmount = ItemData.GoldValue * ItemData.Quantity;
                if (engine != null)
                {
                    engine.Farm.AddGold(goldAmount);
                    MobileGameController.Instance?.OnStateUpdated?.Invoke();
                }
                feedbackText = $"+{goldAmount}G ({ItemData.DisplayName})";
            }
            else if (ItemData.Id == "item_wood_plank" || ItemData.Id == "item_broken_post")
            {
                if (player != null)
                {
                    player.CarriedFences += ItemData.Quantity;
                }
            }
            else if (ItemData.Id == "item_corn_feed")
            {
                if (player != null)
                {
                    player.CarriedFeeders += ItemData.Quantity;
                }
            }
            else if (ItemData.Category == LootCategory.Material && ItemData.Id == "item_rusty_nail")
            {
                // Tăng cọc rào hoặc vật liệu sửa
                if (player != null)
                {
                    player.CarriedFences += 1;
                }
            }

            // Sinh thông báo nổi nhặt đồ
            SpawnLootPopup(feedbackText, GetRarityUnityColor(ItemData.Rarity));

            Destroy(gameObject);
        }

        private void SpawnLootPopup(string text, Color textColor)
        {
            GameObject popup = new GameObject("LootPickupPopup");
            popup.transform.position = transform.position + new Vector3(0f, 0.4f, 0f);

            var textMesh = popup.AddComponent<TextMeshPro>();
            textMesh.text = text;
            textMesh.fontSize = 4.5f;
            textMesh.color = textColor;
            textMesh.alignment = TextAlignmentOptions.Center;
            textMesh.sortingOrder = 750;

            var runner = FindAnyObjectByType<MonoBehaviour>();
            if (runner != null)
            {
                runner.StartCoroutine(FloatingLootTextRoutine(popup, textMesh));
            }
        }

        private static IEnumerator FloatingLootTextRoutine(GameObject obj, TextMeshPro textMesh)
        {
            float duration = 0.6f;
            float elapsed = 0f;
            Vector3 startPos = obj.transform.position;
            Color startColor = textMesh.color;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float progress = elapsed / duration;
                obj.transform.position = startPos + new Vector3(0f, progress * 0.7f, 0f);
                textMesh.color = new Color(startColor.r, startColor.g, startColor.b, 1f - progress);
                yield return null;
            }
            if (obj != null) Destroy(obj);
        }

        public static Color GetRarityUnityColor(LootRarity rarity)
        {
            return rarity switch
            {
                LootRarity.Thuong => new Color(0.95f, 0.95f, 0.95f, 1f),
                LootRarity.Tot => new Color(0.25f, 0.95f, 0.35f, 1f),
                LootRarity.Hiem => new Color(0.25f, 0.75f, 1f, 1f),
                LootRarity.CucPham => new Color(1f, 0.65f, 0.15f, 1f),
                _ => Color.white
            };
        }
    }
}
