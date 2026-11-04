using System;
using System.Collections.Generic;
using PigTycoon.Core;
using TMPro;
using UnityEngine;

namespace PigTycoon.Presentation
{
    // Serialized identifiers retained for the existing action controllers and authored scenes.
    public enum StardewToolType { CamHat, XoNuoc, BuaGo, HangRao, MangAn, MangNuoc, BanChai, KinhLup, DaoGo, KhuTrung, BachVan }

    [Serializable]
    public class HotbarSlotData
    {
        public StardewToolType ToolType;
        public string ToolName;
        public string IconEmoji;
        public Color ThemeColor;
    }

    /// <summary>
    /// Compatibility facade over the Core tool wheel (GameEngine.Carry.Wheel, ADR-0012). There is no hotbar row:
    /// the wheel is opened by <see cref="RadialToolWheel"/>; this component only exposes the selected tool to the
    /// action controllers. Possessions belong to the bracelet/warehouse, not to a tool bar.
    /// </summary>
    public class HotbarController : MonoBehaviour
    {
        public static HotbarController Instance { get; private set; }
        public Action<StardewToolType> OnToolChanged;
        public Transform SlotsContainer;
        public TextMeshProUGUI ToolNameText;
        readonly ToolWheel offlineWheel = new ToolWheel();
        string notifiedToolId;

        GameEngine Engine => MobileGameController.Instance != null ? MobileGameController.Instance.Engine : null;
        public ToolWheel Wheel => Engine != null ? Engine.Carry.Wheel : offlineWheel;
        public int SelectedIndex => Wheel.SelectedIndex;
        public StardewToolType CurrentTool => ToolFromId(Wheel.SelectedToolId);

        public static StardewToolType ToolFromId(string id) =>
            Enum.TryParse<StardewToolType>(id, out var tool) ? tool : StardewToolType.CamHat;

        void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(this); return; }
            Instance = this;
        }
        void OnDestroy() { if (Instance == this) Instance = null; }
        void Start() { InitializeSlots(); NotifyIfChanged(); }
        public void InitializeSlots()
        {
            if (SlotsContainer != null) SlotsContainer.gameObject.SetActive(false);
            var background = GetComponent<UnityEngine.UI.Image>();
            if (background != null) background.enabled = false;
            UpdateToolNameDisplay();
        }
        public void UpdateToolNameDisplay()
        {
            if (ToolNameText != null) { ToolNameText.text = string.Empty; ToolNameText.gameObject.SetActive(false); }
        }

        void NotifyIfChanged()
        {
            string id = Wheel.SelectedToolId;
            if (id == notifiedToolId) return;
            notifiedToolId = id;
            OnToolChanged?.Invoke(CurrentTool);
        }

        public bool SelectSlot(int index) { bool ok = Wheel.TrySelect(index); NotifyIfChanged(); return ok; }
        public void Rotate(int direction) { Wheel.Rotate(direction); NotifyIfChanged(); }

        /// <summary>Context actions ask for a verb: select it when it is on the wheel. Never adds a tool to the wheel.</summary>
        public bool EquipTool(StardewToolType action)
        {
            bool ok = Wheel.TrySelectTool(action.ToString());
            NotifyIfChanged();
            return ok;
        }

        public bool Equip(int slot, StardewToolType tool, out string message)
        {
            bool ok = Wheel.TryEquip(slot, tool.ToString(), out message);
            NotifyIfChanged();
            return ok;
        }

        public bool Unequip(int slot, out string message)
        {
            bool ok = Wheel.TryUnequip(slot, out message);
            NotifyIfChanged();
            return ok;
        }

        public void OnBaloClicked()
        {
            var popup = BackpackPopup.Instance;
            if (popup == null)
            {
                var canvas = FindFirstObjectByType<Canvas>();
                if (canvas != null) popup = canvas.gameObject.AddComponent<BackpackPopup>();
            }
            popup?.Toggle();
        }

        void Update()
        {
            NotifyIfChanged(); // a loaded save may have changed the selection
            if (BackpackPopup.InputBlockedThisFrame || (RadialToolWheel.Instance != null && RadialToolWheel.Instance.IsOpen)) return;
            // Desktop quick keys 1..6 pick a wheel sector directly (no hotbar row is drawn).
            for (int i = 0; i < CarryRules.WheelSlotCount; i++)
                if (Input.GetKeyDown(KeyCode.Alpha1 + i)) { SelectSlot(i); break; }
        }

        public static HotbarSlotData CreateSlotData(StardewToolType action) => new HotbarSlotData
        {
            ToolType = action, ToolName = ToolName(action), IconEmoji = string.Empty, ThemeColor = Color.white
        };

        public static string ToolName(StardewToolType action) => action switch
        {
            StardewToolType.CamHat => "Rải cám", StardewToolType.XoNuoc => "Lấy nước",
            StardewToolType.BuaGo => "Bảo trì", StardewToolType.HangRao => "Dựng rào",
            StardewToolType.MangAn => "Đặt máng", StardewToolType.MangNuoc => "Đặt bồn",
            StardewToolType.BanChai => "Chăm sóc", StardewToolType.KinhLup => "Quan sát",
            StardewToolType.DaoGo => "Tấn công", StardewToolType.KhuTrung => "Khử trùng",
            StardewToolType.BachVan => "Bạch Vân", _ => action.ToString()
        };
    }
}
