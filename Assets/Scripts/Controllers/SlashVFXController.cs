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
            vfxSlashDown = UISpriteLoader.LoadSprite("Art/Sprites/VFX/Combat/vfx_trail_slash_down.png", 64) ?? UISpriteLoader.LoadSprite("Art/Sprites/VFX/Combat/vfx_slash_down.png", 64);
            vfxSlashUp = UISpriteLoader.LoadSprite("Art/Sprites/VFX/Combat/vfx_trail_slash_up.png", 64) ?? UISpriteLoader.LoadSprite("Art/Sprites/VFX/Combat/vfx_slash_up.png", 64);
            vfxSlashLeft = UISpriteLoader.LoadSprite("Art/Sprites/VFX/Combat/vfx_trail_slash_left.png", 64) ?? UISpriteLoader.LoadSprite("Art/Sprites/VFX/Combat/vfx_slash_side.png", 64);
            vfxSlashRight = UISpriteLoader.LoadSprite("Art/Sprites/VFX/Combat/vfx_trail_slash_right.png", 64) ?? UISpriteLoader.LoadSprite("Art/Sprites/VFX/Combat/vfx_slash_side.png", 64);

            vfxThrustDown = UISpriteLoader.LoadSprite("Art/Sprites/VFX/Combat/vfx_trail_thrust_down.png", 64) ?? UISpriteLoader.LoadSprite("Art/Sprites/VFX/Combat/vfx_thrust_spike.png", 64);
            vfxThrustUp = UISpriteLoader.LoadSprite("Art/Sprites/VFX/Combat/vfx_trail_thrust_up.png", 64) ?? UISpriteLoader.LoadSprite("Art/Sprites/VFX/Combat/vfx_thrust_spike.png", 64);
            vfxThrustLeft = UISpriteLoader.LoadSprite("Art/Sprites/VFX/Combat/vfx_trail_thrust_left.png", 64) ?? UISpriteLoader.LoadSprite("Art/Sprites/VFX/Combat/vfx_thrust_spike.png", 64);
            vfxThrustRight = UISpriteLoader.LoadSprite("Art/Sprites/VFX/Combat/vfx_trail_thrust_right.png", 64) ?? UISpriteLoader.LoadSprite("Art/Sprites/VFX/Combat/vfx_thrust_spike.png", 64);

            vfxSmash = UISpriteLoader.LoadSprite("Art/Sprites/VFX/Combat/vfx_smash_impact.png", 64);
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
                    spawnPos += (Vector3)(aimDir.normalized * 0.55f) + new Vector3(0f, -0.2f, 0f);
                    break;
                case CombatActionType.Thrust:
                    if (facing == CharacterFacing.Up)
                    {
                        sr.sprite = vfxThrustUp;
                        spawnPos += new Vector3(0f, 0.55f, 0f);
                    }
                    else if (facing == CharacterFacing.Down)
                    {
                        sr.sprite = vfxThrustDown;
                        spawnPos += new Vector3(0f, -0.45f, 0f);
                    }
                    else if (facing == CharacterFacing.Left || isLeft)
                    {
                        sr.sprite = vfxThrustLeft;
                        spawnPos += new Vector3(-0.55f, 0f, 0f);
                    }
                    else
                    {
                        sr.sprite = vfxThrustRight;
                        spawnPos += new Vector3(0.55f, 0f, 0f);
                    }
                    break;
                case CombatActionType.Slash:
                default:
                    if (facing == CharacterFacing.Up)
                    {
                        sr.sprite = vfxSlashUp;
                        spawnPos += new Vector3(0f, 0.45f, 0f);
                    }
                    else if (facing == CharacterFacing.Down)
                    {
                        sr.sprite = vfxSlashDown;
                        spawnPos += new Vector3(0f, -0.35f, 0f);
                    }
                    else if (facing == CharacterFacing.Left || isLeft)
                    {
                        sr.sprite = vfxSlashLeft;
                        spawnPos += new Vector3(-0.45f, 0f, 0f);
                    }
                    else
                    {
                        sr.sprite = vfxSlashRight;
                        spawnPos += new Vector3(0.45f, 0f, 0f);
                    }
                    break;
            }

            vfxObj.transform.position = spawnPos;
            StartCoroutine(FadeAndDestroyRoutine(vfxObj, sr, 0.14f));
        }

        /// <summary>
        /// Hiệu ứng hào quang hoàng kim khi kích hoạt Đỡ Đòn (Parry Stance) hoặc phản đòn thành công
        /// </summary>
        public void SpawnParryVFX(Vector2 playerPos, bool isSuccess = false)
        {
            GameObject vfxObj = new GameObject("VFX_ParryAura");
            var sr = vfxObj.AddComponent<SpriteRenderer>();
            sr.sprite = UISpriteLoader.GetFrameActionButton();
            sr.sortingOrder = 600;
            sr.color = isSuccess ? new Color(1f, 0.9f, 0.2f, 0.85f) : new Color(0.3f, 0.85f, 1f, 0.5f);
            vfxObj.transform.position = (Vector3)playerPos;
            vfxObj.transform.localScale = isSuccess ? new Vector3(1.6f, 1.6f, 1f) : new Vector3(1.1f, 1.1f, 1f);

            StartCoroutine(FadeAndDestroyRoutine(vfxObj, sr, isSuccess ? 0.25f : 0.4f));
        }

        /// <summary>
        /// Hiệu ứng tia lửa lóe sáng khi đánh trúng đòn Chí Mạng (Critical Hit)
        /// </summary>
        public void SpawnCritSparkVFX(Vector2 hitPos)
        {
            GameObject vfxObj = new GameObject("VFX_CritSpark");
            var sr = vfxObj.AddComponent<SpriteRenderer>();
            sr.sprite = vfxSmash ?? UISpriteLoader.GetFrameSlotSelected();
            sr.sortingOrder = 650;
            sr.color = new Color(1f, 0.6f, 0.1f, 0.95f); // Cam vàng rực rỡ
            vfxObj.transform.position = (Vector3)hitPos;
            vfxObj.transform.localScale = new Vector3(1.3f, 1.3f, 1f);

            StartCoroutine(FadeAndDestroyRoutine(vfxObj, sr, 0.16f));
        }

        /// <summary>
        /// Hiệu ứng vệt lướt gió và bụi đất khi người chơi thực hiện Lướt Né (Dodge Roll)
        /// </summary>
        public void SpawnDodgeDustVFX(Vector2 playerPos, Vector2 dodgeDir)
        {
            GameObject vfxObj = new GameObject("VFX_DodgeDust");
            var sr = vfxObj.AddComponent<SpriteRenderer>();

            Sprite dodgeSprite = vfxSlashDown;
            if (Mathf.Abs(dodgeDir.x) > Mathf.Abs(dodgeDir.y))
            {
                dodgeSprite = dodgeDir.x < 0 ? vfxSlashLeft : vfxSlashRight;
            }
            else
            {
                dodgeSprite = dodgeDir.y > 0 ? vfxSlashUp : vfxSlashDown;
            }

            sr.sprite = dodgeSprite ?? UISpriteLoader.GetFrameSlotSelected();
            sr.sortingOrder = 450;
            sr.color = new Color(0.85f, 0.95f, 1f, 0.65f); // Trắng lam nhạt lướt gió
            vfxObj.transform.position = (Vector3)(playerPos - (dodgeDir.normalized * 0.25f));
            float angle = Mathf.Atan2(dodgeDir.y, dodgeDir.x) * Mathf.Rad2Deg;
            vfxObj.transform.rotation = Quaternion.Euler(0f, 0f, angle + 90f);
            vfxObj.transform.localScale = new Vector3(0.9f, 0.9f, 1f);

            StartCoroutine(FadeAndDestroyRoutine(vfxObj, sr, 0.22f));
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
