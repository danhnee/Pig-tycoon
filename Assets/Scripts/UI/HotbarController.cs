using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace PigTycoon.Presentation
{
    public enum StardewToolType
    {
        CamHat,      // 0: Bao Cám (Đổ cám vào máng)
        XoNuoc,      // 1: Xô Nước (Đổ nước vào bồn)
        BuaGo,       // 2: Búa Gỗ (Gõ sửa hàng rào)
        BanChai,     // 3: Bàn Chải (Chăm sóc, vuốt ve tăng Bonding)
        KinhLup,     // 4: Kính Lúp (Giám định gen heo & bệnh tật)
        DaoGo,       // 5: Đao Gỗ (Võ học chiến đấu khi có quái)
        KhuTrung,    // 6: Bình Khử Trùng (Dập tắt Ám Khí / Dịch Tả)
        BachVan      // 7: Lệnh Bài Bạch Vân (Phòng thủ khẩn cấp)
    }

    [Serializable]
    public class HotbarSlotData
    {
        public StardewToolType ToolType;
        public string ToolName;
        public string IconEmoji;
        public Color ThemeColor;
    }

    public class HotbarController : MonoBehaviour
    {
        public static HotbarController Instance { get; private set; }

        [Header("Selection")]
        public int SelectedIndex = 0;
        public Action<StardewToolType> OnToolChanged;

        [Header("UI References")]
        public Transform SlotsContainer;
        public TextMeshProUGUI ToolNameText;

        public readonly List<HotbarSlotData> ToolSlots = new List<HotbarSlotData>
        {
            new HotbarSlotData { ToolType = StardewToolType.CamHat, ToolName = "Bao Cám", IconEmoji = "Cám", ThemeColor = new Color(0.9f, 0.75f, 0.35f) },
            new HotbarSlotData { ToolType = StardewToolType.XoNuoc, ToolName = "Xô Nước", IconEmoji = "Nước", ThemeColor = new Color(0.3f, 0.7f, 1f) },
            new HotbarSlotData { ToolType = StardewToolType.BuaGo, ToolName = "Búa Sửa Rào", IconEmoji = "Búa", ThemeColor = new Color(0.7f, 0.45f, 0.25f) },
            new HotbarSlotData { ToolType = StardewToolType.BanChai, ToolName = "Bàn Chải Heo", IconEmoji = "Chải", ThemeColor = new Color(1f, 0.5f, 0.6f) },
            new HotbarSlotData { ToolType = StardewToolType.KinhLup, ToolName = "Kính Soi Gen", IconEmoji = "Kính", ThemeColor = new Color(0.5f, 0.85f, 0.5f) },
            new HotbarSlotData { ToolType = StardewToolType.DaoGo, ToolName = "Đao Gỗ", IconEmoji = "Đao", ThemeColor = new Color(0.85f, 0.3f, 0.3f) },
            new HotbarSlotData { ToolType = StardewToolType.KhuTrung, ToolName = "Khử Trùng", IconEmoji = "Thuốc", ThemeColor = new Color(0.4f, 0.85f, 0.7f) },
            new HotbarSlotData { ToolType = StardewToolType.BachVan, ToolName = "Lệnh Bạch Vân", IconEmoji = "Lệnh", ThemeColor = new Color(0.85f, 0.85f, 0.95f) }
        };

        private readonly List<Image> slotBackgrounds = new List<Image>();
        private readonly List<Outline> slotOutlines = new List<Outline>();

        public StardewToolType CurrentTool => ToolSlots[Mathf.Clamp(SelectedIndex, 0, ToolSlots.Count - 1)].ToolType;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        private void Start()
        {
            InitializeSlots();
            SelectSlot(0);
        }

        public void InitializeSlots()
        {
            if (SlotsContainer == null) return;

            slotBackgrounds.Clear();
            slotOutlines.Clear();

            int childCount = SlotsContainer.childCount;
            for (int i = 0; i < childCount && i < ToolSlots.Count; i++)
            {
                int slotIndex = i;
                Transform slotTr = SlotsContainer.GetChild(i);
                var btn = slotTr.GetComponent<Button>();
                if (btn != null)
                {
                    btn.onClick.RemoveAllListeners();
                    btn.onClick.AddListener(() => SelectSlot(slotIndex));
                }

                var img = slotTr.GetComponent<Image>();
                slotBackgrounds.Add(img);

                var outline = slotTr.GetComponent<Outline>();
                if (outline == null)
                {
                    outline = slotTr.gameObject.AddComponent<Outline>();
                }
                slotOutlines.Add(outline);

                // Gán icon text vào Text bên trong
                var txt = slotTr.GetComponentInChildren<TextMeshProUGUI>();
                if (txt != null)
                {
                    txt.text = ToolSlots[i].IconEmoji;
                    txt.fontSize = 16f;
                    txt.fontStyle = FontStyles.Bold;
                }
            }
        }

        public void SelectSlot(int index)
        {
            if (index < 0 || index >= ToolSlots.Count) return;

            SelectedIndex = index;

            // Cập nhật viền Highlight theo phong cách Stardew Valley (Viền vàng sáng / đỏ nổi bật)
            for (int i = 0; i < slotOutlines.Count; i++)
            {
                if (slotOutlines[i] != null)
                {
                    bool isSelected = (i == SelectedIndex);
                    slotOutlines[i].effectColor = isSelected ? new Color(1f, 0.88f, 0.2f, 1f) : new Color(0.35f, 0.2f, 0.1f, 0.7f);
                    slotOutlines[i].effectDistance = isSelected ? new Vector2(3.5f, -3.5f) : new Vector2(1.5f, -1.5f);
                }
            }

            UpdateToolNameDisplay();

            OnToolChanged?.Invoke(CurrentTool);
        }

        public void UpdateToolNameDisplay()
        {
            if (ToolNameText != null && SelectedIndex >= 0 && SelectedIndex < ToolSlots.Count)
            {
                var pInt = PlayerInteractionController.Instance;
                string extra = "";
                if (pInt != null)
                {
                    if (CurrentTool == StardewToolType.CamHat)
                    {
                        extra = pInt.CurrentBagFeedKg > 0 ? $" ({pInt.CurrentBagFeedKg:0}kg)" : " (Rỗng)";
                    }
                    else if (CurrentTool == StardewToolType.XoNuoc)
                    {
                        extra = pInt.CurrentBucketWaterLiters > 0 ? $" ({pInt.CurrentBucketWaterLiters:0}L)" : " (Rỗng)";
                    }
                    else if (CurrentTool == StardewToolType.BuaGo)
                    {
                        extra = $" (Gỗ: {pInt.CarriedWoodPlanks})";
                    }
                }
                ToolNameText.text = $"[ {ToolSlots[SelectedIndex].ToolName}{extra} ]";
            }
        }

        private void Update()
        {
            UpdateToolNameDisplay();

            // Hỗ trợ phím số 1-8 trên bàn phím máy tính
            for (int i = 0; i < 8; i++)
            {
                if (Input.GetKeyDown(KeyCode.Alpha1 + i))
                {
                    SelectSlot(i);
                    break;
                }
            }
        }
    }
}
