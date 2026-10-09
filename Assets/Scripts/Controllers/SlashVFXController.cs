using System.Collections;
using UnityEngine;
using PigTycoon.Core;

namespace PigTycoon.Presentation
{
    public class SlashVFXController : MonoBehaviour
    {
        public static SlashVFXController Instance { get; private set; }

        private Sprite vfxSlashDown;
        private Sprite vfxSlashUp;
        private Sprite vfxSlashLeft;
        private Sprite vfxSlashRight;

        private Sprite vfxThrustDown;
        private Sprite vfxThrustUp;
        private Sprite vfxThrustLeft;
        private Sprite vfxThrustRight;

        private Sprite vfxSmash;

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
            vfxSlashDown = UISpriteLoader.LoadSprite("Art/Sprites/VFX/vfx_trail_slash_down.png") ?? UISpriteLoader.LoadSprite("Art/Sprites/VFX/vfx_slash_down.png");
            vfxSlashUp = UISpriteLoader.LoadSprite("Art/Sprites/VFX/vfx_trail_slash_up.png") ?? UISpriteLoader.LoadSprite("Art/Sprites/VFX/vfx_slash_up.png");
            vfxSlashLeft = UISpriteLoader.LoadSprite("Art/Sprites/VFX/vfx_trail_slash_left.png") ?? UISpriteLoader.LoadSprite("Art/Sprites/VFX/vfx_slash_side.png");
            vfxSlashRight = UISpriteLoader.LoadSprite("Art/Sprites/VFX/vfx_trail_slash_right.png") ?? UISpriteLoader.LoadSprite("Art/Sprites/VFX/vfx_slash_side.png");

            vfxThrustDown = UISpriteLoader.LoadSprite("Art/Sprites/VFX/vfx_trail_thrust_down.png") ?? UISpriteLoader.LoadSprite("Art/Sprites/VFX/vfx_thrust_spike.png");
            vfxThrustUp = UISpriteLoader.LoadSprite("Art/Sprites/VFX/vfx_trail_thrust_up.png") ?? UISpriteLoader.LoadSprite("Art/Sprites/VFX/vfx_thrust_spike.png");
            vfxThrustLeft = UISpriteLoader.LoadSprite("Art/Sprites/VFX/vfx_trail_thrust_left.png") ?? UISpriteLoader.LoadSprite("Art/Sprites/VFX/vfx_thrust_spike.png");
            vfxThrustRight = UISpriteLoader.LoadSprite("Art/Sprites/VFX/vfx_trail_thrust_right.png") ?? UISpriteLoader.LoadSprite("Art/Sprites/VFX/vfx_thrust_spike.png");

            vfxSmash = UISpriteLoader.LoadSprite("Art/Sprites/VFX/vfx_smash_impact.png");
        }

        public void SpawnVFX(CombatActionType actionType, Vector2 playerPos, Vector2 aimDir, CharacterFacing facing)
        {
            GameObject vfxObj = new GameObject("VFX_CombatHit");
            var sr = vfxObj.AddComponent<SpriteRenderer>();
            sr.sortingOrder = 500; // Hiển thị phía trên nhân vật
            sr.flipX = false;

            Vector3 spawnPos = (Vector3)playerPos;
            bool isLeft = aimDir.x < 0;

            switch (actionType)
            {
                case CombatActionType.Smash:
                    sr.sprite = vfxSmash;
                    spawnPos += (Vector3)(aimDir.normalized * 0.45f) + new Vector3(0f, -0.3f, 0f);
                    break;
                case CombatActionType.Thrust:
                    if (facing == CharacterFacing.Up)
                    {
                        sr.sprite = vfxThrustUp;
                    }
                    else if (facing == CharacterFacing.Down)
                    {
                        sr.sprite = vfxThrustDown;
                    }
                    else if (facing == CharacterFacing.Left || isLeft)
                    {
                        sr.sprite = vfxThrustLeft;
                    }
                    else
                    {
                        sr.sprite = vfxThrustRight;
                    }
                    break;
                case CombatActionType.Slash:
                default:
                    if (facing == CharacterFacing.Up)
                    {
                        sr.sprite = vfxSlashUp;
                    }
                    else if (facing == CharacterFacing.Down)
                    {
                        sr.sprite = vfxSlashDown;
                    }
                    else if (facing == CharacterFacing.Left || isLeft)
                    {
                        sr.sprite = vfxSlashLeft;
                    }
                    else
                    {
                        sr.sprite = vfxSlashRight;
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
