using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using PigTycoon.Core;

namespace PigTycoon.Presentation
{
    /// <summary>
    /// Vòng dụng cụ (ADR-0012): six sectors for tools, always hidden. Tab (PC) or the bronze corner medallion (mobile) opens it.
    /// Highlight follows the pointer / joystick direction; release or tap selects. Inside the wheel a stored tool can be
    /// dragged onto a sector (equip / swap), a sector can be dragged to the tray (unequip), and the hub cycles tools.
    /// Open/close is a 120 ms scale + fade on unscaled time. Game time is never touched.
    /// State lives in Core (GameEngine.Carry.Wheel); this class is presentation only.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class RadialToolWheel : MonoBehaviour
    {
        public static RadialToolWheel Instance { get; private set; }
        public bool IsOpen => open;

        const int Sectors = CarryRules.WheelSlotCount;
        const float AnimSeconds = 0.12f;
        const float HubRadius = 20f, RingRadius = 56f, OuterRadius = 86f, SectorSize = 48f, TrayCell = 34f, TrayY = -118f;
        const float HoldToSelectSeconds = 0.3f, DragThresholdUnits = 6f;

        static readonly Color Bright = new Color(0.957f, 0.824f, 0.478f);

        RectTransform wheelRect, panel, trayRoot, trayFrameRect;
        CanvasGroup fade;
        Image dim, ghost;
        TextMeshProUGUI nameText, headerText;
        readonly Image[] sectorBack = new Image[Sectors];
        readonly Image[] sectorIcon = new Image[Sectors];
        readonly Image[] sectorHalo = new Image[Sectors];
        readonly List<Image> trayCells = new List<Image>();
        readonly List<StardewToolType> trayTools = new List<StardewToolType>();
        Sprite normal, pressed, pulse0, pulse1;
        MythMedallion cornerButton;
        MobileJoystick joystick;

        bool open;
        float animT; // 0 closed .. 1 open
        float tabDownAt = -1f;
        int highlight = -1;
        enum Source { None, Sector, Tray }
        Source dragSource = Source.None; int dragIndex = -1; bool dragging; Vector2 pressLocal;
        string signature = string.Empty;

        GameEngine Engine => MobileGameController.Instance != null ? MobileGameController.Instance.Engine : null;
        ToolWheel Wheel => HotbarController.Instance != null ? HotbarController.Instance.Wheel : (Engine != null ? Engine.Carry.Wheel : null);

        public static RadialToolWheel Create(GameObject host, RectTransform safeRoot, MobileJoystick joystick)
        {
            if (HotbarController.Instance == null) host.AddComponent<HotbarController>();
            var w = host.AddComponent<RadialToolWheel>();
            w.joystick = joystick;
            w.Build(safeRoot);
            return w;
        }

        void Awake() { Instance = this; }
        void OnDestroy() { if (Instance == this) Instance = null; }

        // ---------------------------------------------------------------- build
        void Build(RectTransform safeRoot)
        {
            normal = MythUi.Kit("medallion_large_normal");
            pressed = MythUi.Kit("medallion_large_pressed");
            pulse0 = MythUi.Kit("ready_pulse_large_0");
            pulse1 = MythUi.Kit("ready_pulse_large_1");

            // mobile toggle: small bronze medallion right of the joystick
            // beside the touch stick on touch screens; in the corner on PC where the stick is hidden
            bool touch = Application.isMobilePlatform || Input.touchSupported;
            cornerButton = MythMedallion.Create("ToolWheelButton", safeRoot, false, MythUi.BL, MythUi.BL, new Vector2(touch ? 124 : 12, 20));
            cornerButton.SetKeyHint("Tab");
            cornerButton.SetIcon(MythUi.Icon("actions", "sua", 16));
            cornerButton.OnClick = () => { if (open) Close(false); else Open(); };

            wheelRect = MythUi.Rect("ToolWheel", safeRoot, MythUi.MID, MythUi.MID, Vector2.zero, Vector2.zero);
            wheelRect.anchorMin = Vector2.zero; wheelRect.anchorMax = Vector2.one;
            wheelRect.offsetMin = wheelRect.offsetMax = Vector2.zero;
            fade = wheelRect.gameObject.AddComponent<CanvasGroup>();
            // 3 Nov phone bug: an open wheel counts as a modal, so the HUD canvas group (its parent) went alpha 0 and
            // stopped raycasts; the wheel vanished with it and a touch player was stuck with no UI. The wheel keeps its own
            // visibility and input regardless of the HUD group.
            fade.ignoreParentGroups = true;
            // Full-screen catcher: gives geometry-based input and blocks world taps while open.
            dim = wheelRect.gameObject.AddComponent<Image>();
            dim.color = new Color(0.03f, 0.02f, 0.02f, 0.38f);
            dim.raycastTarget = true;
            wheelRect.gameObject.AddComponent<WheelPointer>().Owner = this;

            panel = MythUi.Rect("Panel", wheelRect, MythUi.MID, MythUi.MID, Vector2.zero, new Vector2(1, 1));
            var hub = MythUi.Img("Hub", panel, MythUi.Kit("medallion_large_disabled"), MythUi.MID, MythUi.MID, Vector2.zero, new Vector2(48, 48));
            hub.color = new Color(1f, 1f, 1f, 0.9f);
            var nameRt = MythUi.Rect("ToolName", panel, MythUi.MID, MythUi.MID, new Vector2(0, -1), new Vector2(96, 18));
            nameText = nameRt.gameObject.AddComponent<TextMeshProUGUI>();
            nameText.fontSize = 9.5f; nameText.fontStyle = FontStyles.Bold; nameText.alignment = TextAlignmentOptions.Center;
            nameText.color = Bright; nameText.textWrappingMode = TextWrappingModes.NoWrap; nameText.raycastTarget = false;
            nameText.outlineWidth = 0.25f; nameText.outlineColor = new Color32(18, 10, 6, 255);

            // Top 9-slice Crimson/Gold Header Plaque for crisp tool name + hotkey hint
            var hdrImg = MythUi.Img("WheelHeader", panel, UISpriteLoader.GetFrameSlotSelected(), MythUi.MID, MythUi.MID, new Vector2(0, 102), new Vector2(224, 24));
            hdrImg.type = Image.Type.Sliced;
            hdrImg.color = new Color(0.46f, 0.20f, 0.14f, 0.96f);
            var hdrTxtRt = MythUi.Rect("HeaderText", hdrImg.transform, MythUi.MID, MythUi.MID, Vector2.zero, new Vector2(212, 22));
            headerText = hdrTxtRt.gameObject.AddComponent<TextMeshProUGUI>();
            headerText.fontSize = 10.5f; headerText.fontStyle = FontStyles.Bold; headerText.alignment = TextAlignmentOptions.Center;
            headerText.color = new Color(1f, 0.94f, 0.72f); headerText.textWrappingMode = TextWrappingModes.NoWrap; headerText.raycastTarget = false;

            for (int i = 0; i < Sectors; i++)
            {
                var p = SectorCenter(i);
                sectorHalo[i] = MythUi.Img("Halo" + i, panel, null, MythUi.MID, MythUi.MID, p, new Vector2(SectorSize, SectorSize));
                sectorBack[i] = MythUi.Img("Sector" + i, panel, normal, MythUi.MID, MythUi.MID, p, new Vector2(SectorSize, SectorSize));
                sectorIcon[i] = MythUi.Img("Icon" + i, panel, null, MythUi.MID, MythUi.MID, p, new Vector2(24, 24));
            }
            var trayFrameImg = MythUi.Img("TrayFrame", panel, UISpriteLoader.GetFrameWoodPanel(), MythUi.MID, MythUi.MID, new Vector2(0, TrayY), new Vector2(210, 44));
            trayFrameImg.type = Image.Type.Sliced;
            trayFrameImg.color = new Color(0.88f, 0.80f, 0.70f, 0.95f);
            trayFrameRect = trayFrameImg.rectTransform;
            trayRoot = MythUi.Rect("ToolTray", panel, MythUi.MID, MythUi.MID, new Vector2(0, TrayY), new Vector2(1, 1));
            // an explicit close for touch players (tapping outside also closes)
            var closeBtn = MythMedallion.Create("ToolWheelClose", panel, false, MythUi.MID, MythUi.MID, new Vector2(118, 102));
            closeBtn.SetIcon(UISpriteLoader.GetIconCloseCross());
            closeBtn.OnClick = () => Close(false);
            ghost = MythUi.Img("DragGhost", wheelRect, null, MythUi.MID, MythUi.MID, Vector2.zero, new Vector2(24, 24));
            ghost.enabled = false;
            ApplyAnim(0f);
            wheelRect.gameObject.SetActive(false);
        }

        static Vector2 SectorCenter(int i)
        {
            float a = (90f - i * 60f) * Mathf.Deg2Rad;
            return new Vector2(Mathf.Round(Mathf.Cos(a) * RingRadius), Mathf.Round(Mathf.Sin(a) * RingRadius));
        }

        // ---------------------------------------------------------------- open / close
        public void Open()
        {
            if (open || Wheel == null || BackpackPopup.IsOpen) return;
            open = true; highlight = Wheel.SelectedIndex; dragSource = Source.None; dragging = false;
            signature = string.Empty;
            wheelRect.gameObject.SetActive(true);
            wheelRect.SetAsLastSibling();
            Refresh();
        }

        /// <summary>Close; when <paramref name="select"/> the highlighted sector becomes the active tool.</summary>
        public void Close(bool select)
        {
            if (!open) return;
            if (select && highlight >= 0) HotbarController.Instance?.SelectSlot(highlight);
            open = false; dragSource = Source.None; dragging = false; ghost.enabled = false;
        }

        // ---------------------------------------------------------------- input + animation
        void Update()
        {
            float target = open ? 1f : 0f;
            if (!Mathf.Approximately(animT, target))
            {
                animT = Mathf.MoveTowards(animT, target, Time.unscaledDeltaTime / AnimSeconds);
                ApplyAnim(animT);
                if (animT <= 0f && wheelRect.gameObject.activeSelf) wheelRect.gameObject.SetActive(false);
            }

            bool tabDown = Input.GetKeyDown(KeyCode.Tab), tabUp = Input.GetKeyUp(KeyCode.Tab);
            if (tabDown && !BackpackPopup.IsOpen)
            {
                if (open) Close(false);
                else { Open(); tabDownAt = Time.unscaledTime; }
            }
            else if (tabUp && open && tabDownAt >= 0f && Time.unscaledTime - tabDownAt >= HoldToSelectSeconds) Close(true); // hold-and-release
            if (tabUp) tabDownAt = -1f;
            if (open && Input.GetKeyDown(KeyCode.Escape)) Close(false);

            var engine = Engine;
            if (cornerButton != null) cornerButton.SetEnabled(engine != null && !BackpackPopup.IsOpen);
            if (!open || engine == null) return;
            if (BackpackPopup.IsOpen) { Close(false); return; }

            if (Input.GetKeyDown(KeyCode.Q)) CycleHighlight(-1);
            if (Input.GetKeyDown(KeyCode.E)) CycleHighlight(1);
            float scroll = Input.mouseScrollDelta.y;
            if (scroll > 0.5f) CycleHighlight(-1); else if (scroll < -0.5f) CycleHighlight(1);

            // Joystick direction drives the highlight when pushed; otherwise the pointer does (OnPointerMove).
            Vector2 key = new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical"));
            if (joystick != null && joystick.InputVector.sqrMagnitude > 0.25f) highlight = SectorFromDirection(joystick.InputVector);
            else if (key.sqrMagnitude > 0.25f) highlight = SectorFromDirection(key);
            else if (!dragging) { Vector2 local; if (PointerLocal(Input.mousePosition, out local) && !Input.touchSupported) UpdateHighlightFromLocal(local); }

            string sig = Signature();
            if (sig != signature) Refresh();
            RefreshHighlight();
        }

        void ApplyAnim(float t)
        {
            float e = 1f - (1f - t) * (1f - t);
            fade.alpha = e;
            panel.localScale = Vector3.one * Mathf.Lerp(0.85f, 1f, e);
        }

        void CycleHighlight(int dir)
        {
            var w = Wheel; if (w == null) return;
            for (int n = 1; n <= Sectors; n++)
            {
                int i = ((highlight + dir * n) % Sectors + Sectors) % Sectors;
                if (!string.IsNullOrEmpty(w.Slots[i])) { highlight = i; return; }
            }
        }

        // ---------------------------------------------------------------- geometry
        bool PointerLocal(Vector2 screen, out Vector2 local) =>
            RectTransformUtility.ScreenPointToLocalPointInRectangle(wheelRect, screen, null, out local);

        static int SectorFromDirection(Vector2 d)
        {
            float angle = Mathf.Atan2(d.y, d.x) * Mathf.Rad2Deg;                   // 0 = right, CCW
            float fromTop = Mathf.Repeat(90f - angle, 360f);                          // clockwise from top
            return Mathf.RoundToInt(fromTop / 60f) % Sectors;
        }

        /// <summary>Sector under a wheel-local point, -1 in the hub or outside the ring.</summary>
        static int SectorAt(Vector2 local)
        {
            float r = local.magnitude;
            if (r < HubRadius || r > OuterRadius) return -1;
            return SectorFromDirection(local);
        }

        int TrayAt(Vector2 local)
        {
            for (int i = 0; i < trayCells.Count; i++)
            {
                Vector2 c = (Vector2)trayCells[i].rectTransform.anchoredPosition + new Vector2(0, TrayY);
                if (Mathf.Abs(local.x - c.x) <= TrayCell * 0.5f && Mathf.Abs(local.y - c.y) <= TrayCell * 0.5f) return i;
            }
            return -1;
        }

        bool InTrayBand(Vector2 local) => local.y < TrayY + TrayCell * 0.6f && local.y > TrayY - TrayCell * 0.6f;

        void UpdateHighlightFromLocal(Vector2 local)
        {
            if (local.magnitude < HubRadius || local.magnitude > OuterRadius + 40f) return; // keep last sector near the hub / far away
            highlight = SectorFromDirection(local);
        }

        // ---------------------------------------------------------------- pointer events (forwarded by WheelPointer)
        internal void OnDown(PointerEventData e)
        {
            if (!open || !PointerLocal(e.position, out var local)) return;
            pressLocal = local; dragging = false; dragSource = Source.None; dragIndex = -1;
            int t = TrayAt(local);
            if (t >= 0) { dragSource = Source.Tray; dragIndex = t; return; }
            int s = SectorAt(local);
            if (s >= 0) { highlight = s; if (!string.IsNullOrEmpty(Wheel.Slots[s])) { dragSource = Source.Sector; dragIndex = s; } }
        }

        internal void OnDrag(PointerEventData e)
        {
            if (!open || dragSource == Source.None || !PointerLocal(e.position, out var local)) return;
            if (!dragging && (local - pressLocal).magnitude < DragThresholdUnits) return;
            dragging = true;
            ghost.rectTransform.anchoredPosition = new Vector2(Mathf.Round(local.x), Mathf.Round(local.y));
            ghost.sprite = dragSource == Source.Sector ? IconForId(ToolAtSector(dragIndex)) : IconFor(trayTools[dragIndex]);
            ghost.enabled = ghost.sprite != null;
            ghost.transform.SetAsLastSibling();
            int s = SectorAt(local);
            if (s >= 0) highlight = s;
        }

        internal void OnUp(PointerEventData e)
        {
            if (!open || !PointerLocal(e.position, out var local)) { Close(false); return; }
            var hotbar = HotbarController.Instance;
            if (dragging && hotbar != null)
            {
                int target = SectorAt(local);
                if (dragSource == Source.Tray && target >= 0) hotbar.Equip(target, trayTools[dragIndex], out _);
                else if (dragSource == Source.Sector && target >= 0 && target != dragIndex) hotbar.Equip(target, HotbarController.ToolFromId(ToolAtSector(dragIndex)), out _);
                else if (dragSource == Source.Sector && target < 0 && InTrayBand(local)) hotbar.Unequip(dragIndex, out _);
                dragging = false; dragSource = Source.None; ghost.enabled = false; signature = string.Empty;
                return;
            }
            dragSource = Source.None; ghost.enabled = false;
            float r = local.magnitude;
            int s = SectorAt(local);
            if (s >= 0 && !string.IsNullOrEmpty(Wheel.Slots[s])) { highlight = s; Close(true); }       // tap/release on a sector selects
            else if (r < HubRadius) { hotbar?.Rotate(1); highlight = Wheel.SelectedIndex; }            // hub cycles tools
            else if (TrayAt(local) < 0 && !InTrayBand(local)) Close(false);                            // tap outside closes
        }

        internal void OnMove(PointerEventData e)
        {
            if (!open || dragging) return;
            if (PointerLocal(e.position, out var local)) UpdateHighlightFromLocal(local);
        }

        // ---------------------------------------------------------------- content
        string ToolAtSector(int i) => Wheel.Slots[i];

        string Signature()
        {
            var w = Wheel; if (w == null) return string.Empty;
            return string.Join(",", w.Slots) + "|" + w.SelectedIndex;
        }

        static Sprite IconForId(string toolId) => string.IsNullOrEmpty(toolId) ? null : IconFor(HotbarController.ToolFromId(toolId));

        static Sprite IconFor(StardewToolType tool)
        {
            switch (tool)
            {
                case StardewToolType.CamHat: return MythUi.Icon("actions", "cho_an", 24);
                case StardewToolType.XoNuoc: return MythUi.Icon("actions", "cho_uong", 24);
                case StardewToolType.BuaGo: return MythUi.Icon("actions", "sua", 24);
                case StardewToolType.HangRao: return MythUi.Icon("actions", "xay", 24);
                case StardewToolType.MangAn: return MythUi.Icon("actions", "nap_dan", 24);
                case StardewToolType.MangNuoc: return MythUi.Icon("actions", "cho_uong", 24);
                case StardewToolType.BanChai: return MythUi.Icon("actions", "giam_dinh", 24);
                case StardewToolType.KinhLup: return MythUi.Icon("misc", "tinh_bao", 24);
                case StardewToolType.DaoGo: return MythUi.Icon("actions", "danh", 24);
                case StardewToolType.KhuTrung: return MythUi.Icon("places", "khu_cach_ly", 24);
                case StardewToolType.BachVan: return MythUi.Icon("actions", "huyt_sao", 24);
                default: return MythUi.Icon("actions", "sua", 24);
            }
        }

        void Refresh()
        {
            var w = Wheel; if (w == null) return;
            signature = Signature();
            for (int i = 0; i < Sectors; i++)
            {
                var spr = IconForId(w.Slots[i]);
                sectorIcon[i].sprite = spr; sectorIcon[i].enabled = spr != null;
            }
            for (int i = trayCells.Count - 1; i >= 0; i--) Destroy(trayCells[i].gameObject);
            trayCells.Clear(); trayTools.Clear();
            var stored = new List<string>(w.StoredToolIds());
            if (trayFrameRect != null)
            {
                trayFrameRect.gameObject.SetActive(stored.Count > 0);
                trayFrameRect.sizeDelta = new Vector2(Mathf.Max(96f, stored.Count * TrayCell + 20f), 42f);
            }
            for (int i = 0; i < stored.Count; i++)
            {
                float x = Mathf.Round((i - (stored.Count - 1) * 0.5f) * TrayCell);
                var cell = MythUi.Img("Stored" + i, trayRoot, MythUi.Kit("medallion_small_normal"), MythUi.MID, MythUi.MID, new Vector2(x, 0), new Vector2(32, 32));
                var icon = MythUi.Img("Icon", cell.transform, IconForId(stored[i]), MythUi.MID, MythUi.MID, Vector2.zero, new Vector2(24, 24));
                icon.rectTransform.localScale = Vector3.one * (16f / 24f);
                trayCells.Add(cell); trayTools.Add(HotbarController.ToolFromId(stored[i]));
            }
            RefreshHighlight();
        }

        void RefreshHighlight()
        {
            var w = Wheel; if (w == null) return;
            bool blink = ((int)(Time.unscaledTime * 4f) & 1) == 0;
            for (int i = 0; i < Sectors; i++)
            {
                bool hot = i == highlight, current = i == w.SelectedIndex;
                bool empty = string.IsNullOrEmpty(w.Slots[i]);
                sectorBack[i].sprite = hot && !empty ? (pressed ?? normal) : normal;
                sectorBack[i].enabled = sectorBack[i].sprite != null;
                sectorBack[i].color = empty ? new Color(1f, 1f, 1f, 0.45f) : Color.white;
                var halo = sectorHalo[i];
                if (hot && !empty) { halo.sprite = blink ? pulse0 : pulse1; halo.color = Color.white; }
                else if (current) { halo.sprite = pulse0; halo.color = new Color(1f, 0.85f, 0.45f, 0.7f); }
                else halo.sprite = null;
                halo.enabled = halo.sprite != null;
            }
            string id = highlight >= 0 && highlight < Sectors ? w.Slots[highlight] : string.Empty;
            string toolLabel = string.IsNullOrEmpty(id) ? string.Empty : HotbarController.ToolName(HotbarController.ToolFromId(id));
            nameText.text = toolLabel;
            if (headerText != null)
                headerText.text = string.IsNullOrEmpty(toolLabel) ? "<b>[Tab] VÒNG DỤNG CỤ</b>" : $"<b>[Tab] {toolLabel.ToUpperInvariant()}</b>";
        }

        // ---------------------------------------------------------------- forwarder
        sealed class WheelPointer : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IDragHandler, IBeginDragHandler, IEndDragHandler, IPointerMoveHandler
        {
            public RadialToolWheel Owner;
            public void OnPointerDown(PointerEventData e) { Owner.OnDown(e); }
            public void OnPointerUp(PointerEventData e) { Owner.OnUp(e); }
            public void OnBeginDrag(PointerEventData e) { }
            public void OnDrag(PointerEventData e) { Owner.OnDrag(e); }
            public void OnEndDrag(PointerEventData e) { }
            public void OnPointerMove(PointerEventData e) { }
        }
    }
}
