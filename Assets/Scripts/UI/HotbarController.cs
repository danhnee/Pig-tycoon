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
        BuaGo,       // 2: Búa Thợ Mộc (Chỉ dùng tháo dỡ & sửa chữa công trình)
        HangRao,     // 3: Cọc Rào Chuồng (Đặt hàng rào theo ô vuông)
        MangAn,      // 4: Máng Ăn Heo (Đặt máng ăn theo ô vuông)
        MangNuoc,    // 5: Máng Nước Sạch (Đặt máng nước theo ô vuông)
        BanChai,     // 6: Bàn Chải (Chăm sóc, vuốt ve tăng Bonding)
        KinhLup,     // 7: Kính Lúp (Giám định gen heo & bệnh tật)
        DaoGo,       // 8: Đao Gỗ (Võ học chiến đấu khi có quái)
        KhuTrung,    // 9: Bình Khử Trùng (Dập tắt Ám Khí / Dịch Tả)
        BachVan      // 10: Lệnh Bài Bạch Vân (Phòng thủ khẩn cấp)
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
            new HotbarSlotData { ToolType = StardewToolType.BuaGo, ToolName = "Búa Thợ Mộc", IconEmoji = "Búa", ThemeColor = new Color(0.7f, 0.45f, 0.25f) },
            new HotbarSlotData { ToolType = StardewToolType.HangRao, ToolName = "Cọc Rào Chuồng", IconEmoji = "Rào", ThemeColor = new Color(0.65f, 0.48f, 0.30f) },
            new HotbarSlotData { ToolType = StardewToolType.MangAn, ToolName = "Máng Ăn Heo", IconEmoji = "Máng", ThemeColor = new Color(0.85f, 0.65f, 0.25f) },
            new HotbarSlotData { ToolType = StardewToolType.MangNuoc, ToolName = "Máng Nước Sạch", IconEmoji = "Bồn", ThemeColor = new Color(0.25f, 0.65f, 0.85f) },
            new HotbarSlotData { ToolType = StardewToolType.BanChai, ToolName = "Bàn Chải Heo", IconEmoji = "Chải", ThemeColor = new Color(1f, 0.5f, 0.6f) },
            new HotbarSlotData { ToolType = StardewToolType.KinhLup, ToolName = "Kính Soi Gen", IconEmoji = "Kính", ThemeColor = new Color(0.5f, 0.85f, 0.5f) }
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

        private readonly List<Image> toolIconImages = new List<Image>();
        private readonly List<TextMeshProUGUI> quantityTexts = new List<TextMeshProUGUI>();

        public void InitializeSlots()
        {
            if (SlotsContainer == null) return;

            slotBackgrounds.Clear();
            slotOutlines.Clear();
            toolIconImages.Clear();
            quantityTexts.Clear();

            var normalFrame = UISpriteLoader.GetFrameSlotNormal();
            var hotbarBg = GetComponent<Image>();
            if (hotbarBg != null)
            {
                var woodFrame = UISpriteLoader.GetFrameWoodPanel();
                if (woodFrame != null)
                {
                    hotbarBg.sprite = woodFrame;
                    hotbarBg.type = Image.Type.Sliced;
                    hotbarBg.color = Color.white;
                }
            }

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
                if (normalFrame != null)
                {
                    img.sprite = normalFrame;
                    img.type = Image.Type.Sliced;
                    img.color = Color.white;
                }
                slotBackgrounds.Add(img);

                var outline = slotTr.GetComponent<Outline>();
                if (outline != null)
                {
                    outline.enabled = false; // Dùng 9-slice frame thay cho outline thô
                }
                slotOutlines.Add(outline);

                // 1. Hotkey Number ở góc trên bên trái (1, 2, ... 8)
                var oldTxt = slotTr.Find("Emoji")?.GetComponent<TextMeshProUGUI>();
                if (oldTxt != null)
                {
                    oldTxt.text = (i + 1).ToString();
                    oldTxt.fontSize = 11f;
                    oldTxt.fontStyle = FontStyles.Bold;
                    oldTxt.color = new Color(1f, 0.88f, 0.45f);
                    var rt = oldTxt.rectTransform;
                    rt.anchorMin = new Vector2(0, 1);
                    rt.anchorMax = new Vector2(0, 1);
                    rt.pivot = new Vector2(0, 1);
                    rt.anchoredPosition = new Vector2(4, -3);
                    rt.sizeDelta = new Vector2(16, 16);
                    oldTxt.alignment = TextAlignmentOptions.TopLeft;
                }

                // 2. Icon Tool Sprite ở giữa ô
                Transform iconTr = slotTr.Find("ToolIcon");
                Image iconImg = null;
                if (iconTr == null)
                {
                    var iconObj = new GameObject("ToolIcon");
                    iconObj.transform.SetParent(slotTr, false);
                    var iconRt = iconObj.AddComponent<RectTransform>();
                    iconRt.anchorMin = new Vector2(0.5f, 0.5f);
                    iconRt.anchorMax = new Vector2(0.5f, 0.5f);
                    iconRt.pivot = new Vector2(0.5f, 0.5f);
                    iconRt.anchoredPosition = new Vector2(0, 1);
                    iconRt.sizeDelta = new Vector2(38, 38);
                    iconImg = iconObj.AddComponent<Image>();
                    iconImg.raycastTarget = false;
                }
                else
                {
                    iconImg = iconTr.GetComponent<Image>();
                }

                if (iconImg != null)
                {
                    var toolSprite = UISpriteLoader.GetToolIcon(ToolSlots[i].ToolType);
                    if (toolSprite != null)
                    {
                        iconImg.sprite = toolSprite;
                        iconImg.color = Color.white;
                        iconImg.preserveAspect = true;
                    }
                    toolIconImages.Add(iconImg);
                }

                // 3. Text số lượng ở góc dưới bên phải (ví dụ 20kg, 50L, 10 gỗ)
                Transform qtyTr = slotTr.Find("QuantityText");
                TextMeshProUGUI qtyTxt = null;
                if (qtyTr == null)
                {
                    var qtyObj = new GameObject("QuantityText");
                    qtyObj.transform.SetParent(slotTr, false);
                    var qtyRt = qtyObj.AddComponent<RectTransform>();
                    qtyRt.anchorMin = new Vector2(1, 0);
                    qtyRt.anchorMax = new Vector2(1, 0);
                    qtyRt.pivot = new Vector2(1, 0);
                    qtyRt.anchoredPosition = new Vector2(-4, 3);
                    qtyRt.sizeDelta = new Vector2(40, 14);
                    qtyTxt = qtyObj.AddComponent<TextMeshProUGUI>();
                    qtyTxt.fontSize = 10f;
                    qtyTxt.fontStyle = FontStyles.Bold;
                    qtyTxt.color = Color.white;
                    qtyTxt.alignment = TextAlignmentOptions.BottomRight;
                    qtyTxt.raycastTarget = false;
                    var qtyOutline = qtyObj.AddComponent<Outline>();
                    qtyOutline.effectColor = new Color(0.15f, 0.08f, 0.02f, 0.9f);
                    qtyOutline.effectDistance = new Vector2(1, -1);
                }
                else
                {
                    qtyTxt = qtyTr.GetComponent<TextMeshProUGUI>();
                }
                quantityTexts.Add(qtyTxt);
            }

            // 4. Tạo ô Balo ở cuối thanh Hotbar
            CreateBackpackButton(normalFrame);
        }

        private void CreateBackpackButton(Sprite normalFrame)
        {
            Transform existingBalo = SlotsContainer.Find("Slot_Balo");
            GameObject baloObj = existingBalo != null ? existingBalo.gameObject : null;
            if (baloObj == null)
            {
                baloObj = new GameObject("Slot_Balo");
                baloObj.transform.SetParent(SlotsContainer, false);
                var rt = baloObj.AddComponent<RectTransform>();
                rt.anchorMin = new Vector2(0.5f, 0.5f);
                rt.anchorMax = new Vector2(0.5f, 0.5f);
                rt.pivot = new Vector2(0.5f, 0.5f);
                rt.anchoredPosition = new Vector2(248f, 0f);
                rt.sizeDelta = new Vector2(56f, 56f);

                var img = baloObj.AddComponent<Image>();
                img.sprite = normalFrame;
                img.type = Image.Type.Sliced;
                img.color = Color.white;

                var btn = baloObj.AddComponent<Button>();
                btn.onClick.AddListener(OnBaloClicked);

                // Icon Balo
                var iconObj = new GameObject("ToolIcon");
                iconObj.transform.SetParent(baloObj.transform, false);
                var iconRt = iconObj.AddComponent<RectTransform>();
                iconRt.anchorMin = new Vector2(0.5f, 0.5f);
                iconRt.anchorMax = new Vector2(0.5f, 0.5f);
                iconRt.pivot = new Vector2(0.5f, 0.5f);
                iconRt.anchoredPosition = new Vector2(0, 1);
                iconRt.sizeDelta = new Vector2(38, 38);
                var iconImg = iconObj.AddComponent<Image>();
                iconImg.sprite = UISpriteLoader.GetIconBackpack();
                iconImg.color = Color.white;
                iconImg.preserveAspect = true;
                iconImg.raycastTarget = false;

                // Hotkey badge 'B'
                var hotkeyObj = new GameObject("Hotkey");
                hotkeyObj.transform.SetParent(baloObj.transform, false);
                var hRt = hotkeyObj.AddComponent<RectTransform>();
                hRt.anchorMin = new Vector2(0, 1);
                hRt.anchorMax = new Vector2(0, 1);
                hRt.pivot = new Vector2(0, 1);
                hRt.anchoredPosition = new Vector2(4, -3);
                hRt.sizeDelta = new Vector2(16, 16);
                var hTxt = hotkeyObj.AddComponent<TextMeshProUGUI>();
                hTxt.text = "B";
                hTxt.fontSize = 11f;
                hTxt.fontStyle = FontStyles.Bold;
                hTxt.color = new Color(1f, 0.88f, 0.45f);
                hTxt.alignment = TextAlignmentOptions.TopLeft;
            }
            else
            {
                var rt = baloObj.GetComponent<RectTransform>();
                if (rt != null) rt.anchoredPosition = new Vector2(248f, 0f);
            }
        }

        public void OnBaloClicked()
        {
            var bp = BackpackPopup.Instance;
            if (bp == null)
            {
                var canvas = FindAnyObjectByType<Canvas>();
                if (canvas != null)
                {
                    bp = canvas.gameObject.AddComponent<BackpackPopup>();
                }
            }
            bp?.Toggle();
        }

        public void SelectSlot(int index)
        {
            if (index < 0 || index >= ToolSlots.Count) return;

            SelectedIndex = index;

            var normalFrame = UISpriteLoader.GetFrameSlotNormal();
            var selectedFrame = UISpriteLoader.GetFrameSlotSelected();

            // Cập nhật viền Highlight theo phong cách Stardew Valley bằng 9-slice vàng sáng
            for (int i = 0; i < slotBackgrounds.Count; i++)
            {
                if (slotBackgrounds[i] != null)
                {
                    bool isSelected = (i == SelectedIndex);
                    slotBackgrounds[i].sprite = isSelected ? selectedFrame : normalFrame;
                    slotBackgrounds[i].type = Image.Type.Sliced;
                    slotBackgrounds[i].color = Color.white;
                    slotBackgrounds[i].transform.localScale = isSelected ? new Vector3(1.08f, 1.08f, 1f) : Vector3.one;
                }
            }

            UpdateToolNameDisplay();

            OnToolChanged?.Invoke(CurrentTool);
        }

        public static HotbarSlotData CreateSlotData(StardewToolType tool)
        {
            return tool switch
            {
                StardewToolType.CamHat => new HotbarSlotData { ToolType = StardewToolType.CamHat, ToolName = "Bao Cám", IconEmoji = "Cám", ThemeColor = new Color(0.9f, 0.75f, 0.35f) },
                StardewToolType.XoNuoc => new HotbarSlotData { ToolType = StardewToolType.XoNuoc, ToolName = "Xô Nước", IconEmoji = "Nước", ThemeColor = new Color(0.3f, 0.7f, 1f) },
                StardewToolType.BuaGo => new HotbarSlotData { ToolType = StardewToolType.BuaGo, ToolName = "Búa Thợ Mộc", IconEmoji = "Búa", ThemeColor = new Color(0.7f, 0.45f, 0.25f) },
                StardewToolType.HangRao => new HotbarSlotData { ToolType = StardewToolType.HangRao, ToolName = "Cọc Rào Chuồng", IconEmoji = "Rào", ThemeColor = new Color(0.65f, 0.48f, 0.30f) },
                StardewToolType.MangAn => new HotbarSlotData { ToolType = StardewToolType.MangAn, ToolName = "Máng Ăn Heo", IconEmoji = "Máng", ThemeColor = new Color(0.85f, 0.65f, 0.25f) },
                StardewToolType.MangNuoc => new HotbarSlotData { ToolType = StardewToolType.MangNuoc, ToolName = "Máng Nước Sạch", IconEmoji = "Bồn", ThemeColor = new Color(0.25f, 0.65f, 0.85f) },
                StardewToolType.BanChai => new HotbarSlotData { ToolType = StardewToolType.BanChai, ToolName = "Bàn Chải Heo", IconEmoji = "Chải", ThemeColor = new Color(1f, 0.5f, 0.6f) },
                StardewToolType.KinhLup => new HotbarSlotData { ToolType = StardewToolType.KinhLup, ToolName = "Kính Soi Gen", IconEmoji = "Kính", ThemeColor = new Color(0.5f, 0.85f, 0.5f) },
                StardewToolType.DaoGo => new HotbarSlotData { ToolType = StardewToolType.DaoGo, ToolName = "Đao Gỗ", IconEmoji = "Đao", ThemeColor = new Color(0.85f, 0.3f, 0.3f) },
                StardewToolType.KhuTrung => new HotbarSlotData { ToolType = StardewToolType.KhuTrung, ToolName = "Khử Trùng", IconEmoji = "Thuốc", ThemeColor = new Color(0.4f, 0.85f, 0.7f) },
                StardewToolType.BachVan => new HotbarSlotData { ToolType = StardewToolType.BachVan, ToolName = "Lệnh Bạch Vân", IconEmoji = "Lệnh", ThemeColor = new Color(0.85f, 0.85f, 0.95f) },
                _ => new HotbarSlotData { ToolType = tool, ToolName = "Vật Phẩm", IconEmoji = "?", ThemeColor = Color.white }
            };
        }

        public void EquipTool(StardewToolType toolType)
        {
            // 1. Nếu công cụ đã có trên thanh Hotbar -> Chọn ngay ô đó
            for (int i = 0; i < ToolSlots.Count; i++)
            {
                if (ToolSlots[i].ToolType == toolType)
                {
                    SelectSlot(i);
                    return;
                }
            }

            // 2. Nếu chưa có trên thanh Hotbar -> Gán vào ô đang được chọn hiện tại
            int slotToReplace = Mathf.Clamp(SelectedIndex, 0, ToolSlots.Count - 1);
            ToolSlots[slotToReplace] = CreateSlotData(toolType);

            // Cập nhật lại Icon sprite của slot đó
            if (slotToReplace < toolIconImages.Count && toolIconImages[slotToReplace] != null)
            {
                toolIconImages[slotToReplace].sprite = UISpriteLoader.GetToolIcon(toolType);
            }

            SelectSlot(slotToReplace);
        }

        public void UpdateToolNameDisplay()
        {
            var pInt = PlayerInteractionController.Instance;

            // Cập nhật số lượng vật tư trên từng ô theo đúng ToolType của ô đó
            for (int i = 0; i < ToolSlots.Count && i < quantityTexts.Count; i++)
            {
                var txt = quantityTexts[i];
                if (txt == null) continue;

                var tool = ToolSlots[i].ToolType;
                if (pInt != null)
                {
                    txt.text = tool switch
                    {
                        StardewToolType.CamHat => pInt.CurrentBagFeedKg > 0 ? $"{pInt.CurrentBagFeedKg:0}k" : "",
                        StardewToolType.XoNuoc => pInt.CurrentBucketWaterLiters > 0 ? $"{pInt.CurrentBucketWaterLiters:0}L" : "",
                        StardewToolType.HangRao => pInt.CarriedFences > 0 ? $"{pInt.CarriedFences}" : "0",
                        StardewToolType.MangAn => pInt.CarriedFeeders > 0 ? $"{pInt.CarriedFeeders}" : "0",
                        StardewToolType.MangNuoc => pInt.CarriedWaterTroughs > 0 ? $"{pInt.CarriedWaterTroughs}" : "0",
                        _ => ""
                    };
                }
            }

            if (ToolNameText != null && SelectedIndex >= 0 && SelectedIndex < ToolSlots.Count)
            {
                string extra = "";
                if (pInt != null)
                {
                    extra = CurrentTool switch
                    {
                        StardewToolType.CamHat => pInt.CurrentBagFeedKg > 0 ? $" ({pInt.CurrentBagFeedKg:0}kg)" : " (Rỗng)",
                        StardewToolType.XoNuoc => pInt.CurrentBucketWaterLiters > 0 ? $" ({pInt.CurrentBucketWaterLiters:0}L)" : " (Rỗng)",
                        StardewToolType.BuaGo => " (Tháo Dỡ / Sửa Chữa)",
                        StardewToolType.HangRao => $" (Còn {pInt.CarriedFences} Rào)",
                        StardewToolType.MangAn => $" (Còn {pInt.CarriedFeeders} Máng)",
                        StardewToolType.MangNuoc => $" (Còn {pInt.CarriedWaterTroughs} Máng)",
                        _ => ""
                    };
                }
                ToolNameText.text = $"[ {ToolSlots[SelectedIndex].ToolName}{extra} ]";
            }
        }

        private void Update()
        {
            UpdateToolNameDisplay();

            // Phím B hoặc I hoặc phím số 9 để mở Túi Đồ (Balo)
            if (Input.GetKeyDown(KeyCode.B) || Input.GetKeyDown(KeyCode.I) || Input.GetKeyDown(KeyCode.Alpha9))
            {
                OnBaloClicked();
            }

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
