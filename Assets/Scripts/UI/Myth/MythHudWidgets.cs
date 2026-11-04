using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace PigTycoon.Presentation
{
    /// <summary>Sprite + RectTransform helpers for the icon-first Myth HUD. Point-filtered, integer-sized, no text.</summary>
    internal static class MythUi
    {
        const string KitRoot = "Art/PixelMyth/UI/Kit/";
        const string IconRoot = "Art/PixelMyth/UI/Icons/";
        public static readonly Vector2 Center = new Vector2(0.5f, 0.5f);

        /// <summary>Kit sprite by file stem (no extension). Returns null when the art is not packaged: callers must hide, not fake.</summary>
        public static Sprite Kit(string stem) => UISpriteLoader.LoadSprite(KitRoot + stem + ".png", 32, Center);

        /// <summary>Icon sprite, e.g. Icon("weather", "mua", 24).</summary>
        public static Sprite Icon(string group, string name, int size) =>
            UISpriteLoader.LoadSprite(IconRoot + group + "/" + name + "_" + size + ".png", 32, Center);

        public static RectTransform Rect(string name, Transform parent, Vector2 anchor, Vector2 pivot, Vector2 pos, Vector2 size)
        {
            var go = new GameObject(name, typeof(RectTransform));
            var rt = go.GetComponent<RectTransform>();
            rt.SetParent(parent, false);
            rt.anchorMin = rt.anchorMax = anchor;
            rt.pivot = pivot;
            rt.anchoredPosition = new Vector2(Mathf.Round(pos.x), Mathf.Round(pos.y));
            rt.sizeDelta = size;
            return rt;
        }

        /// <summary>Image sized exactly to the sprite's pixel size unless an explicit size is given.</summary>
        public static Image Img(string name, Transform parent, Sprite sprite, Vector2 anchor, Vector2 pivot, Vector2 pos, Vector2? size = null, bool raycast = false)
        {
            Vector2 s = size ?? (sprite != null ? new Vector2(sprite.rect.width, sprite.rect.height) : Vector2.zero);
            var rt = Rect(name, parent, anchor, pivot, pos, s);
            var img = rt.gameObject.AddComponent<Image>();
            img.sprite = sprite;
            img.enabled = sprite != null; // missing art stays invisible instead of becoming a white square
            img.raycastTarget = raycast;
            img.preserveAspect = false;
            return img;
        }

        public static Vector2 TL => new Vector2(0f, 1f);
        public static Vector2 TR => new Vector2(1f, 1f);
        public static Vector2 BL => new Vector2(0f, 0f);
        public static Vector2 BR => new Vector2(1f, 0f);
        public static Vector2 MID => new Vector2(0.5f, 0.5f);
    }

    /// <summary>Clips a child image from the left (or bottom) so a bar fills in whole pixels.</summary>
    internal sealed class MythClipFill
    {
        readonly RectTransform clip;
        readonly float fullSize;
        readonly float thickness;
        readonly bool vertical;
        public readonly Image Fill;

        public MythClipFill(string name, Transform parent, Sprite fillSprite, Vector2 anchor, Vector2 pivot, Vector2 pos,
            float fullSize, float thickness, bool vertical = false, bool rightAnchored = false, bool tile = true)
        {
            this.fullSize = fullSize; this.thickness = thickness; this.vertical = vertical;
            clip = MythUi.Rect(name, parent, anchor, pivot, pos, vertical ? new Vector2(thickness, fullSize) : new Vector2(fullSize, thickness));
            clip.gameObject.AddComponent<RectMask2D>();
            Vector2 childAnchor = vertical ? new Vector2(0.5f, 0f) : (rightAnchored ? new Vector2(1f, 0.5f) : new Vector2(0f, 0.5f));
            Fill = MythUi.Img("Fill", clip, fillSprite, childAnchor, childAnchor, Vector2.zero,
                vertical ? new Vector2(thickness, fullSize) : new Vector2(fullSize, thickness));
            if (tile && fillSprite != null) Fill.type = Image.Type.Tiled;
        }

        public RectTransform Root => clip;

        public void Set(float ratio)
        {
            float r = Mathf.Clamp01(ratio);
            float len = Mathf.Round(fullSize * r);
            clip.gameObject.SetActive(len > 0f);
            clip.sizeDelta = vertical ? new Vector2(thickness, len) : new Vector2(len, thickness);
        }
    }

    /// <summary>Tap handler for non-medallion widgets (herd gauge, satchel).</summary>
    internal sealed class MythTap : MonoBehaviour, IPointerClickHandler
    {
        public Action OnTap;
        public void OnPointerClick(PointerEventData eventData) { OnTap?.Invoke(); }
    }

    /// <summary>Swaps the joystick knob art while pressed. MobileJoystick itself still owns all input.</summary>
    internal sealed class MythJoystickSkin : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
    {
        public Image Knob;
        public Sprite Normal, Pressed;
        public void OnPointerDown(PointerEventData e) { if (Knob != null && Pressed != null) Knob.sprite = Pressed; }
        public void OnPointerUp(PointerEventData e) { if (Knob != null && Normal != null) Knob.sprite = Normal; }
    }

    /// <summary>
    /// Round bronze medallion: normal / pressed / disabled art, 8-step clockwise cooldown wedge, 2-frame ready pulse.
    /// Icon-only; no permanent label clutter.
    /// </summary>
    internal sealed class MythMedallion : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler, IPointerClickHandler
    {
        public Action OnClick;
        Image baseImg, iconImg, wedgeImg, pulseImg, badgeBgImg;
        TextMeshProUGUI keyHintText, badgeLabelText;
        Sprite normal, pressed, disabled;
        Sprite[] wedge = new Sprite[8];
        Sprite[] pulse = new Sprite[2];
        RectTransform iconRt;
        bool large, isEnabled = true, isDown, ready;
        float cooldownRemaining01;
        Color rimTint = Color.white;

        public static MythMedallion Create(string name, Transform parent, bool large, Vector2 anchor, Vector2 pivot, Vector2 pos)
        {
            string size = large ? "large" : "small";
            var normal = MythUi.Kit("medallion_" + size + "_normal");
            float px = large ? 48f : 32f;
            var rt = MythUi.Rect(name, parent, anchor, pivot, pos, new Vector2(px, px));
            var m = rt.gameObject.AddComponent<MythMedallion>();
            m.large = large;
            m.normal = normal;
            m.pressed = MythUi.Kit("medallion_" + size + "_pressed");
            m.disabled = MythUi.Kit("medallion_" + size + "_disabled");
            for (int i = 0; i < 8; i++) m.wedge[i] = MythUi.Kit("cooldown_wedge_" + size + "_" + i);
            m.pulse[0] = MythUi.Kit("ready_pulse_" + size + "_0");
            m.pulse[1] = MythUi.Kit("ready_pulse_" + size + "_1");

            m.baseImg = rt.gameObject.AddComponent<Image>();
            m.baseImg.sprite = normal;
            m.baseImg.enabled = normal != null;
            m.baseImg.raycastTarget = true;

            float iconPx = large ? 24f : 16f;
            m.iconImg = MythUi.Img("Icon", rt, null, MythUi.MID, MythUi.MID, Vector2.zero, new Vector2(iconPx, iconPx));
            m.iconRt = m.iconImg.rectTransform;
            m.wedgeImg = MythUi.Img("Cooldown", rt, null, MythUi.MID, MythUi.MID, Vector2.zero, new Vector2(px, px));
            m.pulseImg = MythUi.Img("ReadyPulse", rt, null, MythUi.MID, MythUi.MID, Vector2.zero, new Vector2(px, px));

            // Crisp 2.5D keycap badge (top-left corner of medallion)
            var keyRt = MythUi.Rect("KeyHint", rt, MythUi.TL, MythUi.TL, new Vector2(-3f, 3f), new Vector2(24f, 13f));
            var keyBg = keyRt.gameObject.AddComponent<Image>();
            keyBg.sprite = UISpriteLoader.GetFrameSlotNormal();
            keyBg.type = Image.Type.Sliced;
            keyBg.color = new Color(0.14f, 0.11f, 0.09f, 0.94f);
            keyBg.raycastTarget = false;
            var keyLblRt = MythUi.Rect("KeyText", keyRt, MythUi.MID, MythUi.MID, Vector2.zero, new Vector2(22f, 12f));
            m.keyHintText = keyLblRt.gameObject.AddComponent<TextMeshProUGUI>();
            m.keyHintText.fontSize = 9.0f;
            m.keyHintText.fontStyle = FontStyles.Bold;
            m.keyHintText.alignment = TextAlignmentOptions.Center;
            m.keyHintText.color = new Color(1f, 0.92f, 0.65f, 0.98f);
            m.keyHintText.outlineWidth = 0.28f;
            m.keyHintText.outlineColor = new Color32(20, 12, 6, 255);
            m.keyHintText.textWrappingMode = TextWrappingModes.NoWrap;
            m.keyHintText.raycastTarget = false;
            m.keyHintText.text = string.Empty;
            keyRt.gameObject.SetActive(false);

            // Optional crisp badge label pill under the medallion (used only for active [E] context prompts)
            var badgeRt = MythUi.Rect("BadgePill", rt, new Vector2(0.5f, 0f), new Vector2(0.5f, 1f), new Vector2(0f, 1f), new Vector2(large ? 84f : 64f, 15f));
            m.badgeBgImg = badgeRt.gameObject.AddComponent<Image>();
            m.badgeBgImg.sprite = UISpriteLoader.GetFrameSlotNormal();
            m.badgeBgImg.type = Image.Type.Sliced;
            m.badgeBgImg.color = new Color(0.16f, 0.12f, 0.09f, 0.94f);
            m.badgeBgImg.raycastTarget = false;
            var lblRt = MythUi.Rect("Label", badgeRt, MythUi.MID, MythUi.MID, Vector2.zero, new Vector2(large ? 80f : 60f, 14f));
            m.badgeLabelText = lblRt.gameObject.AddComponent<TextMeshProUGUI>();
            m.badgeLabelText.fontSize = 9.5f;
            m.badgeLabelText.fontStyle = FontStyles.Bold;
            m.badgeLabelText.alignment = TextAlignmentOptions.Center;
            m.badgeLabelText.color = new Color(0.98f, 0.90f, 0.68f, 1f);
            m.badgeLabelText.textWrappingMode = TextWrappingModes.NoWrap;
            m.badgeLabelText.overflowMode = TextOverflowModes.Overflow;
            m.badgeLabelText.raycastTarget = false;
            m.badgeLabelText.text = string.Empty;
            badgeRt.gameObject.SetActive(false);

            m.Refresh();
            return m;
        }

        public void SetIcon(Sprite s)
        {
            iconImg.sprite = s;
            iconImg.enabled = s != null;
        }

        public void SetRimTint(Color tint)
        {
            if (rimTint == tint) return;
            rimTint = tint;
            Refresh();
        }

        public void SetKeyHint(string keyText)
        {
            if (keyHintText == null) return;
            bool show = !string.IsNullOrEmpty(keyText);
            var pillGo = keyHintText.transform.parent != null ? keyHintText.transform.parent.gameObject : keyHintText.gameObject;
            if (pillGo.activeSelf != show) pillGo.SetActive(show);
            if (show && keyHintText.text != keyText) keyHintText.text = keyText;
        }

        public void SetBadgeLabel(string text, Color? color = null)
        {
            if (badgeLabelText == null || badgeBgImg == null) return;
            bool show = !string.IsNullOrEmpty(text);
            if (badgeBgImg.gameObject.activeSelf != show) badgeBgImg.gameObject.SetActive(show);
            if (show)
            {
                if (badgeLabelText.text != text) badgeLabelText.text = text;
                badgeLabelText.color = color ?? new Color(0.98f, 0.90f, 0.68f, 1f);
            }
        }

        public void SetEnabled(bool value) { if (isEnabled == value) return; isEnabled = value; Refresh(); }
        public void SetReady(bool value) { ready = value; }
        /// <summary>1 = just triggered (full dark disc), 0 = ready. Values above 0 show a wedge; the HUD binds this only to real cooldowns.</summary>
        public void SetCooldown(float remaining01) { cooldownRemaining01 = Mathf.Clamp01(remaining01); Refresh(); }

        void Update()
        {
            bool showPulse = ready && isEnabled && cooldownRemaining01 <= 0f;
            if (showPulse)
            {
                int frame = ((int)(Time.unscaledTime * 4f)) & 1; // ~4 fps, two frames
                pulseImg.sprite = pulse[frame];
                pulseImg.enabled = pulse[frame] != null;
            }
            else pulseImg.enabled = false;
        }

        void Refresh()
        {
            baseImg.sprite = !isEnabled ? (disabled ?? normal) : (isDown ? (pressed ?? normal) : normal);
            baseImg.color = isEnabled ? rimTint : new Color(0.65f, 0.62f, 0.58f, 0.9f);
            // Pressed: art moves 1 px down; disabled: icon desaturated + darkened.
            iconRt.anchoredPosition = new Vector2(0f, isDown && isEnabled ? -1f : 0f);
            iconImg.color = isEnabled ? Color.white : new Color(0.55f, 0.5f, 0.46f, 0.75f);
            if (cooldownRemaining01 > 0f)
            {
                int frame = Mathf.Clamp(Mathf.FloorToInt((1f - cooldownRemaining01) * 8f), 0, 7);
                wedgeImg.sprite = wedge[frame];
                wedgeImg.enabled = wedge[frame] != null;
            }
            else wedgeImg.enabled = false;
        }

        public void OnPointerDown(PointerEventData e) { if (!isEnabled) return; isDown = true; Refresh(); }
        public void OnPointerUp(PointerEventData e) { if (!isDown) return; isDown = false; Refresh(); }
        public void OnPointerExit(PointerEventData e) { if (!isDown) return; isDown = false; Refresh(); }
        public void OnPointerClick(PointerEventData e) { if (isEnabled && cooldownRemaining01 <= 0f) OnClick?.Invoke(); }
    }
}
