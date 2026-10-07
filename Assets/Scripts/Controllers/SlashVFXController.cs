using System.Collections;
using UnityEngine;
using PigTycoon.Core;

namespace PigTycoon.Presentation
{
    public class SlashVFXController : MonoBehaviour
    {
        public static SlashVFXController Instance { get; private set; }

        private Sprite vfxSlashDown;
        private Sprite vfxSlashSide;
        private Sprite vfxSlashUp;
        private Sprite vfxSmash;
        private Sprite vfxThrustSide;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            LoadVFXSprites();
        }

        private void LoadVFXSprites()
        {
            vfxSlashDown = UISpriteLoader.LoadSprite("Art/Sprites/VFX/vfx_slash_down.png");
            vfxSlashSide = UISpriteLoader.LoadSprite("Art/Sprites/VFX/vfx_slash_side.png");
            vfxSlashUp = UISpriteLoader.LoadSprite("Art/Sprites/VFX/vfx_slash_up.png");
            vfxSmash = UISpriteLoader.LoadSprite("Art/Sprites/VFX/vfx_smash_impact.png");
            vfxThrustSide = UISpriteLoader.LoadSprite("Art/Sprites/VFX/vfx_thrust_spike.png");
        }

        public void SpawnVFX(CombatActionType actionType, Vector2 playerPos, Vector2 aimDir, CharacterFacing facing)
        {
            GameObject vfxObj = new GameObject("VFX_CombatHit");
            var sr = vfxObj.AddComponent<SpriteRenderer>();
            sr.sortingOrder = 500; // Hiển thị phía trên nhân vật

            Vector3 spawnPos = (Vector3)playerPos;
            bool isLeft = aimDir.x < 0;

            switch (actionType)
            {
                case CombatActionType.Smash:
                    sr.sprite = vfxSmash;
                    spawnPos += (Vector3)(aimDir.normalized * 0.45f) + new Vector3(0f, -0.3f, 0f);
                    break;
                case CombatActionType.Thrust:
                    sr.sprite = vfxThrustSide;
                    sr.flipX = isLeft;
                    if (facing == CharacterFacing.Up)
                    {
                        vfxObj.transform.rotation = Quaternion.Euler(0, 0, 90f);
                        spawnPos += Vector3.up * 0.7f;
                    }
                    else if (facing == CharacterFacing.Down)
                    {
                        vfxObj.transform.rotation = Quaternion.Euler(0, 0, -90f);
                        spawnPos += Vector3.down * 0.7f;
                    }
                    else
                    {
                        spawnPos += (isLeft ? Vector3.left : Vector3.right) * 0.75f;
                    }
                    break;
                case CombatActionType.Slash:
                default:
                    if (facing == CharacterFacing.Up)
                    {
                        sr.sprite = vfxSlashUp;
                        spawnPos += Vector3.up * 0.5f;
                    }
                    else if (facing == CharacterFacing.Side)
                    {
                        sr.sprite = vfxSlashSide;
                        sr.flipX = isLeft;
                        spawnPos += (isLeft ? Vector3.left : Vector3.right) * 0.55f;
                    }
                    else
                    {
                        sr.sprite = vfxSlashDown;
                        spawnPos += Vector3.down * 0.45f;
                    }
                    break;
            }

            vfxObj.transform.position = spawnPos;
            StartCoroutine(FadeAndDestroyRoutine(vfxObj, sr, 0.12f));
        }

        private IEnumerator FadeAndDestroyRoutine(GameObject obj, SpriteRenderer sr, float duration)
        {
            float elapsed = 0f;
            Color initialColor = sr.color;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float alpha = Mathf.Lerp(1f, 0f, elapsed / duration);
                if (sr != null)
                {
                    sr.color = new Color(initialColor.r, initialColor.g, initialColor.b, alpha);
                }
                yield return null;
            }
            if (obj != null) Destroy(obj);
        }
    }
}
