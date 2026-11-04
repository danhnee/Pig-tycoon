using System;
using System.Collections.Generic;
using System.Linq;
using PigTycoon.Core;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace PigTycoon.Presentation
{
    /// <summary>
    /// Nhân vật (phím C) — Bản nâng cấp 2.5D Tactile Paper-Doll & Sân Khấu Diễn Võ 24 FPS:
    /// - Tab 0: Trang bị 6 ô Icon-First + Sân khấu nhân vật 2.5D hoạt họa trực tiếp (paperDollBodyImg + paperDollWeaponOverlayImg + gear layers)
    ///          + Lưới 4x4 (16 món trang bị Icon-First, viền bậc phẩm chất I–IV) + Thẻ So Sánh Chỉ Số Trực Quan (+Công/+Thủ/+Tải thanh xanh/đỏ).
    /// - Tab 1: Võ kỹ 24 chiêu (4 phái A/B/C/D) + Sân khấu Biểu Diễn Chiêu Thức 24 FPS + Xưởng Phối Chiêu Hợp Kích (2.0s).
    /// - Tab 2: Phối võ & Đổi phái chính + Túi nhanh chiến đấu.
    /// - Tab 3: Tiềm năng (Cấp 1–30, EXP, 4 chỉ số gốc vs thưởng đồ, Thể lực 2 lớp, Nghỉ/Ăn/Ngủ) + Bàn Khắc + 6 Khóa Đặc Tính Chuyên Biệt Ẩn.
    /// - Tab 4: Sổ Hai Cột của Khoa / Sổ Bìa Hồng của An & Bề Mặt Phản Chiếu.
    /// Giữ nguyên <see cref="Toggle"/>, <see cref="Show(int)"/> và <see cref="IsOpen"/>.
    /// </summary>
    public sealed class CharacterPanel : MonoBehaviour
    {
        static CharacterPanel instance;
        GameObject frame;
        RectTransform body;
        TextMeshProUGUI header;
        TextMeshProUGUI statusBanner;
        int tab;
        EquipSlot equipFilter = EquipSlot.Weapon;
        bool filterAllSlots = true;
        int reflectionIndex;
        int interactionSeq;

        // Live 2.5D Animation Stage state
        Image paperDollBodyImg;
        Image paperDollWeaponOverlayImg;
        readonly List<(string folder, Image img)> paperDollGearLayerImgs = new List<(string folder, Image img)>();
        TextMeshProUGUI paperDollPoseLabel;
        TextMeshProUGUI previewDirBtnLabel;
        string previewClipOverride;
        string previewLabelOverride;
        string previewWeaponBaseIdOverride;
        float previewClipTimer;
        int previewFacingRow; // 0=South, 1=West, 2=North, 3=East

        // Tab 0 In-Place UI references so clicking sockets/items never destroys buttons mid-probe
        string selectedItemId;
        bool shopMode;
        TextMeshProUGUI dollStatsText;
        TextMeshProUGUI bagTitleText;
        TextMeshProUGUI shopToggleLabel;
        readonly Dictionary<EquipSlot, SocketView> socketViews = new Dictionary<EquipSlot, SocketView>();
        readonly List<GearCellView> gearCellViews = new List<GearCellView>();
        readonly Dictionary<EquipSlot, Image> filterBtnBgs = new Dictionary<EquipSlot, Image>();
        Image filterAllBtnBg;

        // Tab 0 Inspector references
        Image detailIconImg;
        Image detailFrameImg;
        TextMeshProUGUI detailNameText;
        TextMeshProUGUI detailCompareText;
        RectTransform detailBarsRoot;
        Button detailEquipBtn;
        TextMeshProUGUI detailEquipBtnLabel;

        sealed class SocketView
        {
            public EquipSlot Slot;
            public Image FrameBg;
            public Image IconImg;
            public TextMeshProUGUI QualityLabel;
            public TextMeshProUGUI CaptionLabel;
        }

        sealed class GearCellView
        {
            public GameObject Root;
            public Image FrameBg;
            public Image IconImg;
            public TextMeshProUGUI QualityLabel;
            public TextMeshProUGUI EquippedBadge;
            public EquipmentItem BoundItem;
        }

        static GameEngine Engine => MobileGameController.Instance != null ? MobileGameController.Instance.Engine : null;
        public static bool IsOpen => instance != null && instance.frame != null && instance.frame.activeSelf;

        /// <summary>Opens the panel on a given tab (0 Trang bị, 1 Võ kỹ, 2 Phối võ, 3 Tiềm năng, 4 Sổ tay).</summary>
        public static void Show(int tabIndex)
        {
            if (!IsOpen) Toggle();
            if (instance == null) return;
            instance.tab = Mathf.Clamp(tabIndex, 0, 4);
            instance.Refresh();
        }

        public static void Toggle()
        {
            if (Engine == null) return;
            if (instance == null)
            {
                var go = new GameObject("CharacterPanelCanvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
                var c = go.GetComponent<Canvas>();
                c.renderMode = RenderMode.ScreenSpaceOverlay;
                c.sortingOrder = 120;
                var sc = go.GetComponent<CanvasScaler>();
                sc.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                sc.referenceResolution = new Vector2(1280f, 720f);
                sc.matchWidthOrHeight = 0.5f;
                instance = go.AddComponent<CharacterPanel>();
                instance.Build(go.transform);
            }
            instance.frame.SetActive(!instance.frame.activeSelf);
            if (instance.frame.activeSelf) instance.Refresh();
        }

        void Build(Transform root)
        {
            frame = DayMarketPopup.BuildStyledPopupFrame(root, "CharacterPanel", new Vector2(1180f, 680f), out body, out var hdr, out var ftr);
            header = DayMarketPopup.Text(hdr, new Vector2(-95f, 0f), new Vector2(920f, 42f), 16f, TextAlignmentOptions.Left);
            header.color = new Color(1f, 0.94f, 0.68f);
            header.fontStyle = FontStyles.Bold;
            header.richText = true;
            header.raycastTarget = false;

            var close = DayMarketPopup.Button(hdr, new Vector2(510f, 0f), new Vector2(96f, 36f), out var ct);
            ct.text = "Đóng [ESC]";
            ct.fontSize = 15f;
            ct.fontStyle = FontStyles.Bold;
            ct.raycastTarget = false;
            close.onClick.AddListener(() => frame.SetActive(false));

            string[] tabs = { "Trang bị (6 Ô)", "Võ kỹ (24 Chiêu)", "Phối võ & Phái", "Tiềm năng & Khóa", "Sổ Tay & Gương" };
            for (int i = 0; i < tabs.Length; i++)
            {
                int k = i;
                var b = DayMarketPopup.Button(ftr, new Vector2(-432f + i * 216f, 0f), new Vector2(204f, 34f), out var bt);
                bt.text = tabs[i];
                bt.fontSize = 15f;
                bt.fontStyle = FontStyles.Bold;
                bt.raycastTarget = false;
                b.onClick.AddListener(() => { tab = k; Refresh(); });
            }
            frame.SetActive(false);
        }

        void Update()
        {
            if (!IsOpen) return;
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                frame.SetActive(false);
                return;
            }
            StepLivePaperDollAnimation();
        }

        void StepLivePaperDollAnimation()
        {
            var e = Engine;
            if (e == null || paperDollBodyImg == null) return;
            var c = e.Character;
            string baseFolder = EquipmentAppearance.BaseBodyFamily(c.Identity);
            string weaponBaseId = ResolvePreviewWeaponBaseId(c);
            string archetype = WeaponArchetype(weaponBaseId);

            string activeClip;
            int totalCols = 6;
            int frameIdx;

            if (previewClipTimer > 0f && !string.IsNullOrEmpty(previewClipOverride))
            {
                previewClipTimer = Mathf.Max(0f, previewClipTimer - Time.unscaledDeltaTime);
                activeClip = previewClipOverride;
                totalCols = activeClip.StartsWith("skill_", StringComparison.Ordinal) ? 8 : 6;
                frameIdx = Mathf.FloorToInt((10f - previewClipTimer) * 24f) % totalCols;
                if (frameIdx < 0) frameIdx += totalCols;
            }
            else
            {
                // Default continuous cycle: idle -> attack_<archetype>_1 -> attack_<archetype>_2 -> attack_<archetype>_3
                string[] cycle =
                {
                    "idle",
                    $"attack_{archetype}_1",
                    $"attack_{archetype}_2",
                    $"attack_{archetype}_3"
                };
                int phase = Mathf.FloorToInt(Time.unscaledTime * 1.6f);
                activeClip = cycle[Mathf.Abs(phase) % cycle.Length];
                totalCols = 6;
                frameIdx = Mathf.FloorToInt(Time.unscaledTime * 10f) % totalCols;
            }

            int row = Mathf.Clamp(previewFacingRow, 0, 3);
            var bodySpr = UISpriteLoader.LoadCharacterClipCell(baseFolder, activeClip, row, frameIdx, 4, totalCols)
                       ?? UISpriteLoader.LoadCharacterClipCell(baseFolder, "idle", row, frameIdx % 6, 4, 6)
                       ?? UISpriteLoader.GetHeroPaperDollBaseSprite(c.Identity);
            if (bodySpr != null)
            {
                paperDollBodyImg.sprite = bodySpr;
                paperDollBodyImg.enabled = true;
            }

            for (int i = 0; i < paperDollGearLayerImgs.Count; i++)
            {
                var (folder, img) = paperDollGearLayerImgs[i];
                if (img == null) continue;
                var lSpr = UISpriteLoader.LoadCharacterClipCell(folder, activeClip, row, frameIdx, 4, totalCols)
                        ?? UISpriteLoader.LoadCharacterClipCell(folder, "idle", row, frameIdx % 6, 4, 6);
                if (lSpr != null)
                {
                    img.sprite = lSpr;
                    img.enabled = true;
                }
            }

            if (paperDollWeaponOverlayImg != null)
            {
                if (!string.IsNullOrEmpty(weaponBaseId))
                {
                    CharacterFacing dir = row switch
                    {
                        1 => CharacterFacing.Left,
                        2 => CharacterFacing.Up,
                        3 => CharacterFacing.Right,
                        _ => CharacterFacing.Down
                    };
                    var wSpr = UISpriteLoader.LoadWeaponOverlayFrame(weaponBaseId, dir, frameIdx % 6)
                            ?? UISpriteLoader.GetEquipIcon(weaponBaseId, 64);
                    paperDollWeaponOverlayImg.sprite = wSpr;
                    paperDollWeaponOverlayImg.enabled = wSpr != null;
                }
                else
                {
                    paperDollWeaponOverlayImg.enabled = false;
                }
            }
        }

        string ResolvePreviewWeaponBaseId(CharacterData c)
        {
            if (!string.IsNullOrEmpty(previewWeaponBaseIdOverride))
                return previewWeaponBaseIdOverride;
            if (!string.IsNullOrEmpty(selectedItemId))
            {
                var sel = EquipmentAndTacticalCatalog.Find(selectedItemId);
                if (sel != null && sel.Slot == EquipSlot.Weapon)
                    return sel.BaseId;
            }
            if (c.Equipment.TryGetValue(EquipSlot.Weapon, out var wornWeapon) && wornWeapon != null)
                return wornWeapon.BaseId;
            return "vk_dao_rua";
        }

        static string WeaponArchetype(string weaponBaseId)
        {
            if (string.IsNullOrEmpty(weaponBaseId)) return "slash";
            if (weaponBaseId.Contains("giao") || weaponBaseId.Contains("moc")) return "thrust";
            if (weaponBaseId.Contains("bua")) return "smash";
            if (weaponBaseId.Contains("no")) return "ranged";
            return "slash";
        }

        static string FacingName(int row) => (row % 4) switch
        {
            1 => "◄ Tây",
            2 => "▲ Bắc",
            3 => "► Đông",
            _ => "▼ Nam"
        };

        void TriggerAnimationPreview(CharacterData c, string clipName, string labelText, string weaponBaseId = null)
        {
            previewWeaponBaseIdOverride = weaponBaseId;
            previewClipOverride = clipName;
            previewLabelOverride = labelText;
            previewClipTimer = 2.6f;
            interactionSeq++;
            if (paperDollPoseLabel != null)
            {
                paperDollPoseLabel.text = $"▶ {labelText} (24 FPS · {FacingName(previewFacingRow)})";
            }
            StepLivePaperDollAnimation();
        }

        void TriggerSkillAnimation(CharacterData c, MartialSkillDefinition s)
        {
            string weaponBase = ResolvePreviewWeaponBaseId(c);
            string arch = WeaponArchetype(weaponBase);
            string key = s.SlotType switch
            {
                CombatSlotType.TheCong => "z",
                CombatSlotType.TheThu => "x",
                CombatSlotType.TheBien => "v",
                _ => "r"
            };
            string clip = $"skill_{key}_{arch}";
            TriggerAnimationPreview(c, clip, $"Diễn võ [{s.Name}] ({clip})", weaponBase);
        }

        static string Weight(int w) => w == 1 ? "Nhẹ (1)" : w == 2 ? "Vừa (2)" : "Nặng (3)";

        static string SlotName(EquipSlot s) => s switch
        {
            EquipSlot.Weapon => "Vũ Khí",
            EquipSlot.Boots => "Giày",
            EquipSlot.BodyArmor => "Áo Giáp",
            EquipSlot.Pants => "Quần",
            EquipSlot.Bracelet => "Vòng Tay",
            _ => "Dây Chuyền",
        };

        static bool CanShop(GameEngine e)
        {
            string scene = SceneManager.GetActiveScene().name;
            int h = e.Clock.CurrentHour;
            return scene == "Market_Town" && h >= 6 && h < 18;
        }

        void UpdateHeaderText(GameEngine e, CharacterData c)
        {
            if (header == null) return;
            header.text = $"<b>{c.Name}</b> ({(c.Identity == PlayerId.An ? "Nữ · 24t" : "Nam · 26t")})  ·  Cấp <b>{c.Level}</b>/30  ·  EXP {c.Exp}/{MartialProgression.ExpToNext(c.Level)}  ·  " +
                          $"Điểm Võ <b>{c.MartialPoints}</b>  ·  Bí Kíp <color=#FDE047><b>{c.SecretManuals}</b></color>  ·  Lối chơi <b>{MartialProgression.PlaystyleName(c)}</b> ({c.GetTotalWeightScore()} cân)  ·  {e.Farm.Gold:N0}G";
        }

        void Say(string m, bool rebuildAll = true)
        {
            interactionSeq++;
            PlayerInteractionController.Instance?.ShowFeedback(m);
            ToastOverlay.Show(m);
            if (rebuildAll)
            {
                Refresh();
            }
            else
            {
                var e = Engine;
                if (e != null)
                {
                    UpdateHeaderText(e, e.Character);
                    if (statusBanner != null)
                        statusBanner.text = $"✦ {m} (#{interactionSeq})";
                }
            }
            MobileGameController.Instance?.OnStateUpdated?.Invoke();
        }

        void Refresh()
        {
            var e = Engine;
            if (e == null || body == null) return;
            var c = e.Character;
            paperDollBodyImg = null;
            paperDollWeaponOverlayImg = null;
            paperDollGearLayerImgs.Clear();
            paperDollPoseLabel = null;
            previewDirBtnLabel = null;
            socketViews.Clear();
            gearCellViews.Clear();
            filterBtnBgs.Clear();

            foreach (Transform t in body) Destroy(t.gameObject);
            UpdateHeaderText(e, c);

            var statusGo = DayMarketPopup.Text(body, new Vector2(0f, -242f), new Vector2(1080f, 24f), 14f, TextAlignmentOptions.Center);
            statusGo.color = new Color(1f, 0.88f, 0.56f);
            statusGo.fontStyle = FontStyles.Bold;
            statusGo.raycastTarget = false;
            statusBanner = statusGo;
            statusBanner.text = "✦ Sẵn sàng: Chạm vào Vũ Khí hoặc Võ Kỹ để xem nhân vật 2.5D biểu diễn chiêu thức trực tiếp ở 24 FPS.";

            if (tab == 0) BuildEquipment(e, c);
            else if (tab == 1) BuildSkills(e, c);
            else if (tab == 2) BuildPhoi(e, c);
            else if (tab == 3) BuildPotential(e, c);
            else BuildNotebookAndReflection(e, c);

            UISpriteLoader.Apply25DWindowHierarchy(body);
        }

        // -------------------------------------------------------------- Tab 0: Trang bị (6 Ô Icon-First + Sân Khấu 2.5D + Lưới 4x4 + So sánh)
        static readonly (EquipSlot slot, Vector2 pos)[] DollSlots =
        {
            (EquipSlot.Necklace,  new Vector2(-162f, 118f)),
            (EquipSlot.BodyArmor, new Vector2(-162f, 16f)),
            (EquipSlot.Bracelet,  new Vector2(-162f, -86f)),
            (EquipSlot.Weapon,    new Vector2(162f, 118f)),
            (EquipSlot.Pants,     new Vector2(162f, 16f)),
            (EquipSlot.Boots,     new Vector2(162f, -86f)),
        };

        void BuildLiveHeroStage(Transform parent, CharacterData c, Vector2 cardPos, Vector2 cardSize, Vector2 heroOffset, Vector2 heroSize, bool withSockets)
        {
            var dollCard = DayMarketPopup.MakeSlotCard(parent, cardPos, cardSize);
            dollCard.GetComponent<Image>().color = new Color(0.15f, 0.12f, 0.10f, 0.98f);

            var stageHeader = DayMarketPopup.Text(dollCard.transform, new Vector2(0f, cardSize.y * 0.5f - 20f), new Vector2(cardSize.x - 24f, 26f), 16f, TextAlignmentOptions.Center);
            stageHeader.fontStyle = FontStyles.Bold;
            stageHeader.color = new Color(1f, 0.92f, 0.62f);
            stageHeader.text = withSockets ? $"BÚP BÊ 2.5D & 6 Ô TRANG BỊ ({c.Name.ToUpperInvariant()})" : $"SÂN KHẤU DIỄN VÕ 24 FPS ({c.Name.ToUpperInvariant()})";
            stageHeader.raycastTarget = false;

            // Pedestal glow
            var glow = new GameObject("StageHalo", typeof(RectTransform), typeof(Image));
            glow.transform.SetParent(dollCard.transform, false);
            var grt = (RectTransform)glow.transform;
            grt.anchoredPosition = heroOffset + new Vector2(0f, -12f);
            grt.sizeDelta = heroSize * 1.05f;
            var gImg = glow.GetComponent<Image>();
            gImg.sprite = UISpriteLoader.GetSoftCircleSprite();
            gImg.color = new Color(0.92f, 0.68f, 0.25f, 0.18f);
            gImg.raycastTarget = false;

            // Base body animated sprite
            var doll = new GameObject("PaperDollBase", typeof(RectTransform), typeof(Image));
            doll.transform.SetParent(dollCard.transform, false);
            var drt = (RectTransform)doll.transform;
            drt.anchoredPosition = heroOffset;
            drt.sizeDelta = heroSize;
            paperDollBodyImg = doll.GetComponent<Image>();
            paperDollBodyImg.sprite = UISpriteLoader.GetHeroPaperDollBaseSprite(c.Identity) ?? UISpriteLoader.GetHeroPaperDoll25D(c.Identity, combatPose: false);
            paperDollBodyImg.preserveAspect = true;
            paperDollBodyImg.raycastTarget = false;

            // Gear layers
            RebuildGearLayerImages(dollCard.transform, c, drt.anchoredPosition, drt.sizeDelta);

            // Weapon overlay animated sprite
            var wpnGo = new GameObject("PaperDollWeaponOverlay", typeof(RectTransform), typeof(Image));
            wpnGo.transform.SetParent(dollCard.transform, false);
            var wrt = (RectTransform)wpnGo.transform;
            wrt.anchoredPosition = heroOffset;
            wrt.sizeDelta = heroSize;
            paperDollWeaponOverlayImg = wpnGo.GetComponent<Image>();
            paperDollWeaponOverlayImg.preserveAspect = true;
            paperDollWeaponOverlayImg.raycastTarget = false;

            // Live pose label
            paperDollPoseLabel = DayMarketPopup.Text(dollCard.transform, heroOffset + new Vector2(0f, -heroSize.y * 0.5f - 8f), new Vector2(cardSize.x - 24f, 22f), 14f, TextAlignmentOptions.Center);
            paperDollPoseLabel.fontStyle = FontStyles.Bold;
            paperDollPoseLabel.color = new Color(0.55f, 0.94f, 0.68f);
            paperDollPoseLabel.text = $"▶ Chu kỳ chiến đấu 2.5D ({FacingName(previewFacingRow)})";
            paperDollPoseLabel.raycastTarget = false;

            // Stage Controls: [Biểu diễn chiêu ↻] and [Hướng: ▼ Nam]
            float btnY = heroOffset.y - heroSize.y * 0.5f - 36f;
            var demoBtn = DayMarketPopup.Button(dollCard.transform, new Vector2(-72f, btnY), new Vector2(156f, 30f), out var demoLbl);
            demoLbl.text = "Biểu diễn chiêu ↻";
            demoLbl.fontSize = 14f;
            demoLbl.fontStyle = FontStyles.Bold;
            demoLbl.raycastTarget = false;
            demoBtn.onClick.AddListener(() =>
            {
                string wBase = ResolvePreviewWeaponBaseId(c);
                string arch = WeaponArchetype(wBase);
                string[] skills = { $"skill_z_{arch}", $"skill_x_{arch}", $"skill_v_{arch}", $"attack_{arch}_3" };
                string chosenClip = skills[interactionSeq % skills.Length];
                TriggerAnimationPreview(c, chosenClip, $"Biểu diễn {chosenClip}", wBase);
                Say($"Đang biểu diễn tuyệt kỹ 24 FPS: {chosenClip}", rebuildAll: false);
            });

            var dirBtn = DayMarketPopup.Button(dollCard.transform, new Vector2(86f, btnY), new Vector2(136f, 30f), out previewDirBtnLabel);
            previewDirBtnLabel.text = $"Hướng: {FacingName(previewFacingRow)}";
            previewDirBtnLabel.fontSize = 14f;
            previewDirBtnLabel.fontStyle = FontStyles.Bold;
            previewDirBtnLabel.raycastTarget = false;
            dirBtn.onClick.AddListener(() =>
            {
                previewFacingRow = (previewFacingRow + 1) % 4;
                if (previewDirBtnLabel != null)
                    previewDirBtnLabel.text = $"Hướng: {FacingName(previewFacingRow)}";
                if (paperDollPoseLabel != null)
                    paperDollPoseLabel.text = $"▶ Góc nhìn 2.5D: {FacingName(previewFacingRow)}";
                StepLivePaperDollAnimation();
                Say($"Đã xoay nhân vật sang hướng {FacingName(previewFacingRow)}.", rebuildAll: false);
            });

            if (withSockets)
            {
                foreach (var (slot, pos) in DollSlots)
                {
                    var s = slot;
                    var socketGo = DayMarketPopup.MakeSlotCard(dollCard.transform, pos, new Vector2(72f, 72f));
                    var bg = socketGo.GetComponent<Image>();
                    bg.raycastTarget = true;

                    var iconGo = new GameObject("Icon", typeof(RectTransform), typeof(Image));
                    iconGo.transform.SetParent(socketGo.transform, false);
                    ((RectTransform)iconGo.transform).sizeDelta = new Vector2(48f, 48f);
                    var ii = iconGo.GetComponent<Image>();
                    ii.preserveAspect = true;
                    ii.raycastTarget = false;

                    var qBadge = DayMarketPopup.Text(socketGo.transform, new Vector2(-18f, 22f), new Vector2(32f, 20f), 14f, TextAlignmentOptions.Left);
                    qBadge.fontStyle = FontStyles.Bold;
                    qBadge.raycastTarget = false;

                    var cap = DayMarketPopup.Text(socketGo.transform, new Vector2(0f, -46f), new Vector2(104f, 20f), 14f, TextAlignmentOptions.Center);
                    cap.fontStyle = FontStyles.Bold;
                    cap.text = SlotName(slot);
                    cap.color = new Color(0.94f, 0.86f, 0.68f);
                    cap.raycastTarget = false;

                    var btn = socketGo.AddComponent<Button>();
                    btn.targetGraphic = bg;
                    btn.onClick.AddListener(() => OnSocketClicked(s));

                    socketViews[slot] = new SocketView
                    {
                        Slot = slot,
                        FrameBg = bg,
                        IconImg = ii,
                        QualityLabel = qBadge,
                        CaptionLabel = cap
                    };
                }

                dollStatsText = DayMarketPopup.Text(dollCard.transform, new Vector2(0f, -172f), new Vector2(436f, 54f), 14f, TextAlignmentOptions.Center);
                dollStatsText.richText = true;
                dollStatsText.raycastTarget = false;
            }

            StepLivePaperDollAnimation();
        }

        void RebuildGearLayerImages(Transform stageParent, CharacterData c, Vector2 pos, Vector2 size)
        {
            for (int i = 0; i < paperDollGearLayerImgs.Count; i++)
            {
                if (paperDollGearLayerImgs[i].img != null)
                    Destroy(paperDollGearLayerImgs[i].img.gameObject);
            }
            paperDollGearLayerImgs.Clear();

            foreach (var layer in EquipmentAppearance.Layers(c, CardinalDirection.South))
            {
                var spr = UISpriteLoader.GetHeroPaperDollGearLayerSprite(layer);
                var lg = new GameObject($"PaperDollLayer_{layer.Slot}", typeof(RectTransform), typeof(Image));
                lg.transform.SetParent(stageParent, false);
                var lrt = (RectTransform)lg.transform;
                lrt.anchoredPosition = pos;
                lrt.sizeDelta = size;
                var li = lg.GetComponent<Image>();
                li.sprite = spr;
                li.preserveAspect = true;
                li.raycastTarget = false;
                li.enabled = spr != null;
                paperDollGearLayerImgs.Add((string.IsNullOrEmpty(layer.QualityFamily) ? layer.Family : layer.QualityFamily, li));
            }
            if (paperDollWeaponOverlayImg != null)
                paperDollWeaponOverlayImg.transform.SetAsLastSibling();
        }

        void BuildEquipment(GameEngine e, CharacterData c)
        {
            bool canShop = CanShop(e);
            if (!canShop) shopMode = false;

            if (string.IsNullOrEmpty(selectedItemId))
            {
                if (c.Equipment.TryGetValue(EquipSlot.Weapon, out var w) && w != null)
                    selectedItemId = w.Id;
                else if (c.OwnedEquipment.Count > 0)
                    selectedItemId = c.OwnedEquipment[0];
            }

            // Left: Live Animated 2.5D Hero Stage + 6 Icon-First Equipment Sockets
            BuildLiveHeroStage(body, c, new Vector2(-318f, 16f), new Vector2(464f, 436f), new Vector2(0f, 38f), new Vector2(188f, 212f), withSockets: true);

            // Right Top: Icon-First 4x4 Gear Grid (16 tiles) + Slot Filters
            var bagCard = DayMarketPopup.MakeSlotCard(body, new Vector2(236f, 94f), new Vector2(620f, 282f));
            bagCard.GetComponent<Image>().color = new Color(0.14f, 0.11f, 0.09f, 0.98f);

            bagTitleText = DayMarketPopup.Text(bagCard.transform, new Vector2(-172f, 118f), new Vector2(260f, 26f), 16f, TextAlignmentOptions.Left);
            bagTitleText.fontStyle = FontStyles.Bold;
            bagTitleText.richText = true;
            bagTitleText.raycastTarget = false;

            // Filter button: Tất cả (16)
            var allBtn = DayMarketPopup.Button(bagCard.transform, new Vector2(-18f, 118f), new Vector2(72f, 28f), out var allLbl);
            allLbl.text = "Tất cả";
            allLbl.fontSize = 14f;
            allLbl.fontStyle = FontStyles.Bold;
            allLbl.raycastTarget = false;
            filterAllBtnBg = allBtn.GetComponent<Image>();
            allBtn.onClick.AddListener(() =>
            {
                filterAllSlots = true;
                RefreshEquipmentInPlace();
                Say("Hiển thị toàn bộ kho trang bị 4×4 (Icon-First).", rebuildAll: false);
            });

            EquipSlot[] filterOrder = { EquipSlot.Weapon, EquipSlot.BodyArmor, EquipSlot.Pants, EquipSlot.Boots, EquipSlot.Bracelet, EquipSlot.Necklace };
            for (int i = 0; i < filterOrder.Length; i++)
            {
                var s = filterOrder[i];
                var fb = DayMarketPopup.Button(bagCard.transform, new Vector2(46f + i * 40f, 118f), new Vector2(36f, 28f), out var fl);
                fl.text = "";
                fl.raycastTarget = false;
                filterBtnBgs[s] = fb.GetComponent<Image>();
                var ic = new GameObject("Icon", typeof(RectTransform), typeof(Image));
                ic.transform.SetParent(fb.transform, false);
                ((RectTransform)ic.transform).sizeDelta = new Vector2(22f, 22f);
                var icImg = ic.GetComponent<Image>();
                icImg.sprite = SlotSilhouette(s);
                icImg.preserveAspect = true;
                icImg.raycastTarget = false;
                fb.onClick.AddListener(() =>
                {
                    filterAllSlots = false;
                    equipFilter = s;
                    RefreshEquipmentInPlace();
                    Say($"Lọc trang bị theo ô: {SlotName(s)}.", rebuildAll: false);
                });
            }

            var tb = DayMarketPopup.Button(bagCard.transform, new Vector2(272f, 118f), new Vector2(64f, 28f), out shopToggleLabel);
            shopToggleLabel.text = shopMode ? "Túi" : "Tiệm";
            shopToggleLabel.fontSize = 14f;
            shopToggleLabel.fontStyle = FontStyles.Bold;
            shopToggleLabel.raycastTarget = false;
            tb.onClick.AddListener(() =>
            {
                if (!CanShop(Engine))
                {
                    Say("Tiệm rèn trang bị chỉ mở khi đứng tại Chợ Thị Trấn ban ngày (06:00–18:00).", rebuildAll: false);
                    return;
                }
                shopMode = !shopMode;
                RefreshEquipmentInPlace();
                Say(shopMode ? "Đã chuyển sang chế độ Tiệm Chợ." : "Đã chuyển về Túi Trang Bị.", rebuildAll: false);
            });

            // 16 Icon-First Gear Tiles (8 cols x 2 rows = 16 items, representing the 4x4 gear matrix)
            const int cols = 8;
            for (int i = 0; i < 16; i++)
            {
                int idx = i;
                int col = i % cols;
                int row = i / cols;
                Vector2 pos = new Vector2(-252f + col * 72f, 44f - row * 82f);

                var cellGo = DayMarketPopup.MakeSlotCard(bagCard.transform, pos, new Vector2(66f, 66f));
                var bg = cellGo.GetComponent<Image>();
                bg.raycastTarget = true;

                var iconGo = new GameObject("Icon", typeof(RectTransform), typeof(Image));
                iconGo.transform.SetParent(cellGo.transform, false);
                ((RectTransform)iconGo.transform).sizeDelta = new Vector2(46f, 46f);
                var ii = iconGo.GetComponent<Image>();
                ii.preserveAspect = true;
                ii.raycastTarget = false;

                var qBadge = DayMarketPopup.Text(cellGo.transform, new Vector2(-16f, 20f), new Vector2(30f, 18f), 14f, TextAlignmentOptions.Left);
                qBadge.fontStyle = FontStyles.Bold;
                qBadge.raycastTarget = false;

                var eqBadge = DayMarketPopup.Text(cellGo.transform, new Vector2(18f, -20f), new Vector2(26f, 18f), 14f, TextAlignmentOptions.Right);
                eqBadge.fontStyle = FontStyles.Bold;
                eqBadge.color = new Color(0.55f, 0.96f, 0.55f);
                eqBadge.raycastTarget = false;

                var btn = cellGo.AddComponent<Button>();
                btn.targetGraphic = bg;
                btn.onClick.AddListener(() => OnGearCellClicked(idx));

                gearCellViews.Add(new GearCellView
                {
                    Root = cellGo,
                    FrameBg = bg,
                    IconImg = ii,
                    QualityLabel = qBadge,
                    EquippedBadge = eqBadge
                });
            }

            // Right Bottom: Visual Inspector & Stat Delta Card (+Công / +Thủ / +Tải with green/red bars)
            var detail = DayMarketPopup.MakeSlotCard(body, new Vector2(236f, -142f), new Vector2(620f, 172f));
            detail.GetComponent<Image>().color = new Color(0.20f, 0.15f, 0.11f, 0.98f);

            var previewTile = DayMarketPopup.MakeSlotCard(detail.transform, new Vector2(-252f, 20f), new Vector2(82f, 82f));
            detailFrameImg = previewTile.GetComponent<Image>();
            detailFrameImg.raycastTarget = false;
            var dIconGo = new GameObject("DetailIcon", typeof(RectTransform), typeof(Image));
            dIconGo.transform.SetParent(previewTile.transform, false);
            ((RectTransform)dIconGo.transform).sizeDelta = new Vector2(60f, 60f);
            detailIconImg = dIconGo.GetComponent<Image>();
            detailIconImg.preserveAspect = true;
            detailIconImg.raycastTarget = false;

            detailNameText = DayMarketPopup.Text(detail.transform, new Vector2(12f, 60f), new Vector2(410f, 28f), 18f, TextAlignmentOptions.Left);
            detailNameText.fontStyle = FontStyles.Bold;
            detailNameText.richText = true;
            detailNameText.raycastTarget = false;

            detailCompareText = DayMarketPopup.Text(detail.transform, new Vector2(12f, 14f), new Vector2(410f, 62f), 14f, TextAlignmentOptions.TopLeft);
            detailCompareText.richText = true;
            detailCompareText.textWrappingMode = TextWrappingModes.Normal;
            detailCompareText.raycastTarget = false;

            var barsGo = new GameObject("DeltaBars", typeof(RectTransform));
            barsGo.transform.SetParent(detail.transform, false);
            detailBarsRoot = (RectTransform)barsGo.transform;
            detailBarsRoot.anchoredPosition = new Vector2(-46f, -54f);
            detailBarsRoot.sizeDelta = new Vector2(320f, 36f);

            detailEquipBtn = DayMarketPopup.Button(detail.transform, new Vector2(218f, -52f), new Vector2(156f, 42f), out detailEquipBtnLabel);
            detailEquipBtnLabel.fontSize = 15f;
            detailEquipBtnLabel.fontStyle = FontStyles.Bold;
            detailEquipBtnLabel.raycastTarget = false;
            detailEquipBtn.onClick.AddListener(OnDetailActionClicked);

            RefreshEquipmentInPlace();
        }

        void OnSocketClicked(EquipSlot slot)
        {
            var e = Engine;
            if (e == null) return;
            var c = e.Character;
            equipFilter = slot;
            filterAllSlots = false;
            shopMode = false;
            if (c.Equipment.TryGetValue(slot, out var worn) && worn != null)
            {
                selectedItemId = worn.Id;
                if (slot == EquipSlot.Weapon)
                {
                    string arch = WeaponArchetype(worn.BaseId);
                    TriggerAnimationPreview(c, $"skill_z_{arch}", $"Vũ khí đang đeo: {worn.Name}", worn.BaseId);
                }
                Say($"Đang xem ô {SlotName(slot)}: {worn.Name}.", rebuildAll: false);
            }
            else
            {
                var firstOwned = c.OwnedEquipment.Select(EquipmentAndTacticalCatalog.Find).FirstOrDefault(x => x != null && x.Slot == slot);
                selectedItemId = firstOwned?.Id;
                Say($"Đã lọc ô {SlotName(slot)}.", rebuildAll: false);
            }
            RefreshEquipmentInPlace();
        }

        void OnGearCellClicked(int cellIndex)
        {
            var e = Engine;
            if (e == null || cellIndex < 0 || cellIndex >= gearCellViews.Count) return;
            var c = e.Character;
            var item = gearCellViews[cellIndex].BoundItem;
            if (item == null)
            {
                Say("Ô trống — hãy mua thêm trang bị tại Chợ Thị Trấn hoặc nhận từ chiến lợi phẩm.", rebuildAll: false);
                return;
            }

            bool wornNow = c.Equipment.TryGetValue(item.Slot, out var cur) && cur != null && cur.Id == item.Id;
            if (selectedItemId == item.Id && !shopMode && !wornNow)
            {
                var res = MartialProgression.Equip(c, item.Id);
                RebuildGearLayerImages(paperDollBodyImg.transform.parent, c, paperDollBodyImg.rectTransform.anchoredPosition, paperDollBodyImg.rectTransform.sizeDelta);
                if (item.Slot == EquipSlot.Weapon)
                {
                    string arch = WeaponArchetype(item.BaseId);
                    TriggerAnimationPreview(c, $"skill_z_{arch}", $"Trang bị {item.Name}", item.BaseId);
                }
                RefreshEquipmentInPlace();
                Say(res.message, rebuildAll: false);
                return;
            }

            selectedItemId = item.Id;
            if (item.Slot == EquipSlot.Weapon)
            {
                string arch = WeaponArchetype(item.BaseId);
                TriggerAnimationPreview(c, $"skill_z_{arch}", $"Thử chiêu {item.Name}", item.BaseId);
            }
            else
            {
                TriggerAnimationPreview(c, $"attack_{WeaponArchetype(ResolvePreviewWeaponBaseId(c))}_1", $"Xem trang bị {item.Name}");
            }
            RefreshEquipmentInPlace();
            Say($"Đang so sánh: {item.Name} ({UISpriteLoader.QualityRoman(item.Quality)}).", rebuildAll: false);
        }

        void OnDetailActionClicked()
        {
            var e = Engine;
            if (e == null) return;
            var c = e.Character;
            var sel = selectedItemId != null ? EquipmentAndTacticalCatalog.Find(selectedItemId) : null;
            if (sel == null)
            {
                Say("Hãy chạm chọn một món trang bị trên lưới 4×4 trước.", rebuildAll: false);
                return;
            }

            bool isWorn = c.Equipment.TryGetValue(sel.Slot, out var wi) && wi != null && wi.Id == sel.Id;
            if (shopMode && !c.OwnedEquipment.Contains(sel.Id))
            {
                var r = MartialProgression.Buy(e, sel.Id);
                if (r.ok)
                {
                    shopMode = false;
                    selectedItemId = sel.Id;
                }
                RefreshEquipmentInPlace();
                Say(r.message, rebuildAll: false);
            }
            else if (isWorn)
            {
                MartialProgression.Unequip(c, sel.Slot);
                RebuildGearLayerImages(paperDollBodyImg.transform.parent, c, paperDollBodyImg.rectTransform.anchoredPosition, paperDollBodyImg.rectTransform.sizeDelta);
                RefreshEquipmentInPlace();
                Say($"Đã tháo {sel.Name}.", rebuildAll: false);
            }
            else
            {
                var r = MartialProgression.Equip(c, sel.Id);
                RebuildGearLayerImages(paperDollBodyImg.transform.parent, c, paperDollBodyImg.rectTransform.anchoredPosition, paperDollBodyImg.rectTransform.sizeDelta);
                if (sel.Slot == EquipSlot.Weapon)
                {
                    string arch = WeaponArchetype(sel.BaseId);
                    TriggerAnimationPreview(c, $"skill_z_{arch}", $"Trang bị {sel.Name}", sel.BaseId);
                }
                RefreshEquipmentInPlace();
                Say(r.message, rebuildAll: false);
            }
        }

        void RefreshEquipmentInPlace()
        {
            var e = Engine;
            if (e == null) return;
            var c = e.Character;
            UpdateHeaderText(e, c);

            // 1. Update 6 Sockets
            foreach (var kv in socketViews)
            {
                var slot = kv.Key;
                var sv = kv.Value;
                var worn = c.Equipment.TryGetValue(slot, out var w) ? w : null;
                bool isSel = (worn != null && selectedItemId == worn.Id) || (!filterAllSlots && equipFilter == slot);
                sv.FrameBg.color = isSel
                    ? new Color(1f, 0.86f, 0.36f, 1f)
                    : (worn != null ? UISpriteLoader.GetQualityFrameColor(worn.Quality) : new Color(0.32f, 0.26f, 0.20f, 0.95f));
                sv.IconImg.sprite = worn != null ? UISpriteLoader.GetEquipIcon(worn.BaseId, 24) : SlotSilhouette(slot);
                sv.IconImg.color = worn != null ? Color.white : new Color(1f, 1f, 1f, 0.28f);
                sv.QualityLabel.text = worn != null ? UISpriteLoader.QualityRoman(worn.Quality) : "";
                sv.QualityLabel.color = worn != null ? UISpriteLoader.GetQualityFrameColor(worn.Quality) : Color.clear;
            }

            if (dollStatsText != null)
            {
                string Stat(string n, int baseV, int bonus) => $"{n} <b>{baseV + bonus}</b>" + (bonus > 0 ? $"<color=#9be37b>+{bonus}</color>" : "");
                dollStatsText.text =
                    $"{Stat("Sức", c.BaseStats.Str, c.BonusStats.Str)}  ·  {Stat("Nhanh", c.BaseStats.Agi, c.BonusStats.Agi)}  ·  " +
                    $"{Stat("Khống", c.BaseStats.Ctrl, c.BonusStats.Ctrl)}  ·  {Stat("Bền", c.BaseStats.Res, c.BonusStats.Res)}\n" +
                    $"Thủ <b>{c.GetTotalEquipmentDefense()}</b>  ·  Chống Chịu <b>{c.CurrentGuard:0}/{c.MaxGuard:0}</b>  ·  " +
                    $"<color=#FFE270>{MartialProgression.PlaystyleName(c)}</color> ({c.GetTotalWeightScore()} cân)";
            }

            // 2. Update Filter Buttons & Grid Title
            if (bagTitleText != null)
            {
                string scope = filterAllSlots ? "16 Món (4×4)" : SlotName(equipFilter);
                bagTitleText.text = shopMode ? $"<b>Tiệm Chợ · {scope}</b>" : $"<b>Túi Trang Bị · {scope}</b>";
            }
            if (shopToggleLabel != null)
                shopToggleLabel.text = shopMode ? "Túi" : "Tiệm";
            if (filterAllBtnBg != null)
                filterAllBtnBg.color = filterAllSlots ? new Color(0.52f, 0.36f, 0.16f, 1f) : new Color(0.26f, 0.20f, 0.15f, 0.95f);
            foreach (var kv in filterBtnBgs)
            {
                kv.Value.color = (!filterAllSlots && equipFilter == kv.Key)
                    ? new Color(0.52f, 0.36f, 0.16f, 1f)
                    : new Color(0.26f, 0.20f, 0.15f, 0.95f);
            }

            // 3. Populate 16 Icon-First Gear Cells
            List<EquipmentItem> pieces;
            if (shopMode)
            {
                pieces = EquipmentAndTacticalCatalog.AllEquipmentItems
                    .Where(x => (filterAllSlots || x.Slot == equipFilter) && x.Quality == ItemTier.I && !c.OwnedEquipment.Contains(x.Id))
                    .ToList();
            }
            else
            {
                pieces = c.OwnedEquipment
                    .Select(EquipmentAndTacticalCatalog.Find)
                    .Where(x => x != null && (filterAllSlots || x.Slot == equipFilter))
                    .OrderByDescending(x => x.Quality)
                    .ThenBy(x => x.Slot)
                    .ThenBy(x => x.Name)
                    .ToList();
            }

            for (int i = 0; i < gearCellViews.Count; i++)
            {
                var cell = gearCellViews[i];
                var item = i < pieces.Count ? pieces[i] : null;
                cell.BoundItem = item;
                if (item != null)
                {
                    bool isSel = selectedItemId == item.Id;
                    bool wornNow = c.Equipment.TryGetValue(item.Slot, out var cur) && cur != null && cur.Id == item.Id;
                    cell.FrameBg.color = isSel
                        ? new Color(1f, 0.86f, 0.36f, 1f)
                        : UISpriteLoader.GetQualityFrameColor(item.Quality);
                    cell.IconImg.sprite = UISpriteLoader.GetEquipIcon(item.BaseId, 24);
                    cell.IconImg.color = Color.white;
                    cell.QualityLabel.text = UISpriteLoader.QualityRoman(item.Quality);
                    cell.QualityLabel.color = UISpriteLoader.GetQualityFrameColor(item.Quality);
                    cell.EquippedBadge.text = wornNow ? "E" : (shopMode ? $"{MartialProgression.PriceGold(item)}G" : "");
                }
                else
                {
                    cell.FrameBg.color = new Color(0.24f, 0.19f, 0.15f, 0.72f);
                    cell.IconImg.sprite = SlotSilhouette(filterAllSlots ? (EquipSlot)(i % 6) : equipFilter);
                    cell.IconImg.color = new Color(1f, 1f, 1f, 0.16f);
                    cell.QualityLabel.text = "";
                    cell.EquippedBadge.text = "";
                }
            }

            // 4. Update Inspector Card & Visual Stat Delta Bars
            var sel = selectedItemId != null ? EquipmentAndTacticalCatalog.Find(selectedItemId) : (pieces.Count > 0 ? pieces[0] : null);
            if (sel != null) selectedItemId = sel.Id;

            if (detailBarsRoot != null)
            {
                for (int i = detailBarsRoot.childCount - 1; i >= 0; i--)
                    Destroy(detailBarsRoot.GetChild(i).gameObject);
            }

            if (sel == null)
            {
                detailFrameImg.color = new Color(0.30f, 0.24f, 0.18f, 0.9f);
                detailIconImg.sprite = SlotSilhouette(equipFilter);
                detailIconImg.color = new Color(1f, 1f, 1f, 0.3f);
                detailNameText.text = "Chưa chọn trang bị";
                detailCompareText.text = "Hãy chạm vào một ô trang bị 2.5D phía trên để xem so sánh chỉ số +Công / +Thủ / +Tải.";
                detailEquipBtnLabel.text = "Chọn Đồ";
                return;
            }

            var wornInSlot = c.Equipment.TryGetValue(sel.Slot, out var wi) ? wi : null;
            bool isWornSel = wornInSlot != null && wornInSlot.Id == sel.Id;
            var against = isWornSel ? null : wornInSlot;

            detailFrameImg.color = UISpriteLoader.GetQualityFrameColor(sel.Quality);
            detailIconImg.sprite = UISpriteLoader.GetEquipIcon(sel.BaseId, 24);
            detailIconImg.color = Color.white;

            detailNameText.text = $"<b>{sel.Name}</b>  ·  Bậc {UISpriteLoader.QualityRoman(sel.Quality)}  ·  {SlotName(sel.Slot)} ({Weight(sel.WeightScore)})";
            detailNameText.color = UISpriteLoader.GetQualityFrameColor(sel.Quality);

            string DeltaStr(string label, float v, float old, bool lowerBetter = false)
            {
                if (Mathf.Approximately(v, 0f) && Mathf.Approximately(old, 0f)) return "";
                float diff = v - old;
                string dTag = "";
                if (against != null && !Mathf.Approximately(diff, 0f))
                {
                    bool good = lowerBetter ? diff < 0f : diff > 0f;
                    string col = good ? "#9be37b" : "#f87171";
                    string sign = diff > 0f ? "+" : "";
                    dTag = $" <color={col}>({sign}{diff:0.#})</color>";
                }
                return $"{label} <b>{v:0.#}</b>{dTag}   ";
            }

            float OldVal(Func<EquipmentItem, float> f) => against != null ? f(against) : 0f;
            string statSummary =
                (sel.Slot == EquipSlot.Weapon
                    ? DeltaStr("+Công", sel.BaseDamage, OldVal(x => x.BaseDamage)) + DeltaStr("Nhịp", sel.AttackInterval, OldVal(x => x.AttackInterval), lowerBetter: true)
                    : DeltaStr("+Thủ", sel.Defense, OldVal(x => x.Defense))) +
                DeltaStr("+Sức", sel.BonusStr, OldVal(x => x.BonusStr)) +
                DeltaStr("+Nhanh", sel.BonusAgi, OldVal(x => x.BonusAgi)) +
                DeltaStr("+Khống", sel.BonusCtrl, OldVal(x => x.BonusCtrl)) +
                DeltaStr("+Bền", sel.BonusRes, OldVal(x => x.BonusRes)) +
                DeltaStr("+Tải", sel.WeightScore, OldVal(x => x.WeightScore), lowerBetter: true);

            detailCompareText.text = statSummary +
                (string.IsNullOrEmpty(sel.MechanicText) ? "" : $"\n<color=#e8d2a6>{sel.MechanicText}</color>");

            // Build 3 visual delta bars (+Công, +Thủ, +Tải)
            if (detailBarsRoot != null)
            {
                float newAtk = sel.Slot == EquipSlot.Weapon ? sel.BaseDamage : sel.BonusStr * 2f;
                float oldAtk = against != null ? (against.Slot == EquipSlot.Weapon ? against.BaseDamage : against.BonusStr * 2f) : newAtk;
                BuildDeltaChip(detailBarsRoot, new Vector2(-112f, 0f), "+Công", newAtk, oldAtk, 35f, false);

                float newDef = sel.Defense + sel.BonusRes * 2f;
                float oldDef = against != null ? (against.Defense + against.BonusRes * 2f) : newDef;
                BuildDeltaChip(detailBarsRoot, new Vector2(0f, 0f), "+Thủ", newDef, oldDef, 25f, false);

                float newWt = sel.WeightScore;
                float oldWt = against != null ? against.WeightScore : newWt;
                BuildDeltaChip(detailBarsRoot, new Vector2(112f, 0f), "+Tải", newWt, oldWt, 3f, true);
            }

            if (shopMode && !c.OwnedEquipment.Contains(sel.Id))
                detailEquipBtnLabel.text = $"Mua {MartialProgression.PriceGold(sel)}G";
            else if (isWornSel)
                detailEquipBtnLabel.text = "Tháo Trang Bị";
            else
                detailEquipBtnLabel.text = "Trang Bị Ngay";
        }

        static void BuildDeltaChip(Transform parent, Vector2 pos, string label, float val, float oldVal, float maxScale, bool lowerBetter)
        {
            var chip = DayMarketPopup.MakeSlotCard(parent, pos, new Vector2(104f, 28f));
            var chipBg = chip.GetComponent<Image>();
            chipBg.color = new Color(0.12f, 0.09f, 0.07f, 0.95f);
            chipBg.raycastTarget = false;

            float fillRatio = Mathf.Clamp01(val / Mathf.Max(1f, maxScale));
            float diff = val - oldVal;
            Color barCol = Mathf.Approximately(diff, 0f)
                ? new Color(0.86f, 0.72f, 0.34f, 0.65f)
                : ((lowerBetter ? diff < 0f : diff > 0f)
                    ? new Color(0.28f, 0.78f, 0.38f, 0.75f)
                    : new Color(0.86f, 0.32f, 0.28f, 0.75f));

            var fillGo = new GameObject("BarFill", typeof(RectTransform), typeof(Image));
            fillGo.transform.SetParent(chip.transform, false);
            var frt = (RectTransform)fillGo.transform;
            frt.anchorMin = new Vector2(0f, 0f);
            frt.anchorMax = new Vector2(fillRatio, 1f);
            frt.offsetMin = new Vector2(2f, 2f);
            frt.offsetMax = new Vector2(-2f, -2f);
            var fImg = fillGo.GetComponent<Image>();
            fImg.color = barCol;
            fImg.raycastTarget = false;

            var txt = DayMarketPopup.Text(chip.transform, Vector2.zero, new Vector2(98f, 24f), 14f, TextAlignmentOptions.Center);
            txt.fontStyle = FontStyles.Bold;
            txt.text = $"{label}: {val:0.#}";
            txt.raycastTarget = false;
        }

        static Sprite SlotSilhouette(EquipSlot slot)
        {
            var first = EquipmentAndTacticalCatalog.AllEquipmentItems.FirstOrDefault(x => x.Slot == slot);
            return first != null ? UISpriteLoader.GetEquipIcon(first.BaseId, 24) : UISpriteLoader.GetFrameSlotNormal();
        }

        // -------------------------------------------------------------- Tab 1: Võ kỹ (24 kỹ năng, 4 phái) & Sân Khấu Biểu Diễn 24 FPS
        void BuildSkills(GameEngine e, CharacterData c)
        {
            // Top Combo Bar
            var comboBar = DayMarketPopup.MakeSlotCard(body, new Vector2(0f, 206f), new Vector2(1100f, 54f));
            comboBar.GetComponent<Image>().color = new Color(0.24f, 0.18f, 0.12f, 0.98f);

            var sCong = MartialProgression.Slotted(c, CombatSlotType.TheCong);
            var sThu = MartialProgression.Slotted(c, CombatSlotType.TheThu);
            var sBien = MartialProgression.Slotted(c, CombatSlotType.TheBien);

            var activeText = DayMarketPopup.Text(comboBar.transform, new Vector2(-276f, 0f), new Vector2(530f, 48f), 14f, TextAlignmentOptions.Left);
            activeText.richText = true;
            activeText.raycastTarget = false;
            activeText.text =
                $"<b>BA THẾ ĐANG LẮP</b> (Bí Kíp: <color=#FDE047><b>{c.SecretManuals}</b></color>):  " +
                $"[Z] <color=#FFE270>{(sCong != null ? sCong.Name : "Trống")}</color> · " +
                $"[X] <color=#9be37b>{(sThu != null ? sThu.Name : "Trống")}</color> · " +
                $"[V] <color=#7dd3fc>{(sBien != null ? sBien.Name : "Trống")}</color>";

            (string label, string firstId, string secondId, string desc)[] combos =
            {
                ("Ảnh Móc (A->C)", "pig.skill.bo_phap_luot", "pig.skill.moc_keo", "Lướt -> Móc Kéo (Tầm kéo ×2)"),
                ("Trói Chùy (C->B)", "pig.skill.troi_luoi", "pig.skill.chan_dia", "Trói Lưới -> Chấn Địa (ST ×1.3)"),
                ("Phản Thủ (B->D)", "pig.skill.phan_chan", "pig.skill.ho_than", "Phản Chấn -> Hộ Thân (Khiên ×1.5)"),
                ("Thủ Công (D->A)", "pig.skill.tru_bo", "pig.skill.doan_anh_tram", "Trụ Bộ -> Đoạn Ảnh Trảm (Chí mạng)")
            };

            for (int k = 0; k < combos.Length; k++)
            {
                var cb = combos[k];
                var btn = DayMarketPopup.Button(comboBar.transform, new Vector2(68f + k * 134f, 0f), new Vector2(128f, 38f), out var bl);
                bl.fontSize = 14f;
                bl.fontStyle = FontStyles.Bold;
                bl.text = cb.label;
                bl.raycastTarget = false;
                btn.onClick.AddListener(() =>
                {
                    var s1 = MartialSkillCatalog.Find(cb.firstId);
                    var s2 = MartialSkillCatalog.Find(cb.secondId);
                    if (s1 == null || s2 == null) return;
                    TriggerSkillAnimation(c, s1);
                    if (!c.LearnedSkills.Contains(s1.Id) || !c.LearnedSkills.Contains(s2.Id))
                    {
                        Say($"Diễn thử Hợp Kích [{cb.label}]: {cb.desc} — Cần học đủ [{s1.Name}] và [{s2.Name}] để lắp.", rebuildAll: false);
                        return;
                    }
                    var r1 = MartialProgression.Assign(c, s1.SlotType, s1.Id);
                    var r2 = MartialProgression.Assign(c, s2.SlotType, s2.Id);
                    bool both = r1.ok && r2.ok && MartialProgression.Slotted(c, s1.SlotType)?.Id == s1.Id && MartialProgression.Slotted(c, s2.SlotType)?.Id == s2.Id;
                    Say(both
                        ? $"Đã lắp bộ Hợp Kích [{cb.label}]: {cb.desc} (2.0s)!"
                        : $"Hợp Kích [{cb.label}]: {(r1.ok ? r2.message : r1.message)}", rebuildAll: true);
                });
            }

            // Left Column: Live 24 FPS Martial Preview Stage in Tab 1
            BuildLiveHeroStage(body, c, new Vector2(-426f, -20f), new Vector2(244f, 382f), new Vector2(0f, 36f), new Vector2(142f, 162f), withSockets: false);

            // 4 Columns for 24 Martial Skills (A, B, C, D)
            var schools = new[] { MartialSchool.TocBo_A, MartialSchool.TrongKich_B, MartialSchool.KhongChe_C, MartialSchool.SinhTon_D };
            for (int col = 0; col < schools.Length; col++)
            {
                var school = schools[col];
                float x = -182f + col * 208f;
                var h = DayMarketPopup.Text(body, new Vector2(x, 164f), new Vector2(202f, 24f), 15f, TextAlignmentOptions.Center);
                h.fontStyle = FontStyles.Bold;
                h.color = new Color(1f, 0.90f, 0.58f);
                h.text = MartialProgression.SchoolName(school);
                h.raycastTarget = false;

                var list = MartialSkillCatalog.AllSkills.Where(s => s.School == school).ToList();
                for (int i = 0; i < list.Count; i++)
                {
                    var s = list[i];
                    var card = DayMarketPopup.MakeSlotCard(body, new Vector2(x, 126f - i * 56f), new Vector2(202f, 52f));
                    bool learned = c.LearnedSkills.Contains(s.Id);
                    bool slotted = MartialProgression.SlottedId(c, s.SlotType) == s.Id;
                    var cardBg = card.GetComponent<Image>();
                    cardBg.color = slotted
                        ? new Color(0.22f, 0.42f, 0.20f, 0.98f)
                        : (learned ? new Color(0.38f, 0.30f, 0.16f, 0.98f) : new Color(0.20f, 0.16f, 0.13f, 0.96f));
                    cardBg.raycastTarget = true;

                    // Clicking the skill card plays the 24 FPS animation immediately!
                    var previewBtn = card.AddComponent<Button>();
                    previewBtn.targetGraphic = cardBg;
                    previewBtn.onClick.AddListener(() =>
                    {
                        TriggerSkillAnimation(c, s);
                        Say($"Đang biểu diễn chiêu [{s.Name}] ({MartialProgression.SlotName(s.SlotType)}) ở 24 FPS.", rebuildAll: false);
                    });

                    var iconGo = new GameObject("SkillIcon", typeof(RectTransform), typeof(Image));
                    iconGo.transform.SetParent(card.transform, false);
                    var irt = (RectTransform)iconGo.transform;
                    irt.anchoredPosition = new Vector2(-78f, 0f);
                    irt.sizeDelta = new Vector2(34f, 34f);
                    var iImg = iconGo.GetComponent<Image>();
                    iImg.sprite = UISpriteLoader.GetSkillIcon(s.Id, 24);
                    iImg.preserveAspect = true;
                    iImg.raycastTarget = false;

                    var t = DayMarketPopup.Text(card.transform, new Vector2(-8f, 0f), new Vector2(102f, 46f), 14f, TextAlignmentOptions.Left);
                    t.richText = true;
                    t.raycastTarget = false;
                    string tier = s.IsUltimate ? "Tuyệt" : "C" + s.UnlockTier;
                    t.text = $"<b>{s.Name}</b>\n<color=#d8c49a>{tier}·{MartialProgression.SlotName(s.SlotType)}</color>";

                    var b = DayMarketPopup.Button(card.transform, new Vector2(70f, 0f), new Vector2(54f, 32f), out var bl);
                    bl.fontSize = 14f;
                    bl.fontStyle = FontStyles.Bold;
                    bl.raycastTarget = false;
                    if (slotted)
                    {
                        bl.text = "Diễn";
                        b.onClick.AddListener(() =>
                        {
                            TriggerSkillAnimation(c, s);
                            Say($"Chiêu [{s.Name}] đang lắp — biểu diễn 24 FPS!", rebuildAll: false);
                        });
                    }
                    else if (learned)
                    {
                        bl.text = s.IsPassive ? "Nội" : "Lắp";
                        b.onClick.AddListener(() =>
                        {
                            TriggerSkillAnimation(c, s);
                            if (s.IsPassive)
                            {
                                Say($"[{s.Name}] là kỹ năng bị động — tự động kích hoạt khi chiến đấu.", rebuildAll: false);
                                return;
                            }
                            Say(MartialProgression.Assign(c, s.SlotType, s.Id).message, rebuildAll: true);
                        });
                    }
                    else
                    {
                        var (ok, reason) = MartialProgression.CanLearn(c, s);
                        bl.text = "Học";
                        if (!ok)
                            b.GetComponent<Image>().color = new Color(0.32f, 0.24f, 0.20f, 0.9f);
                        b.onClick.AddListener(() =>
                        {
                            TriggerSkillAnimation(c, s);
                            if (!ok)
                            {
                                Say($"Chưa đủ điều kiện học [{s.Name}]: {reason}", rebuildAll: false);
                                return;
                            }
                            Say(MartialProgression.Learn(e, s.Id).message, rebuildAll: true);
                        });
                    }
                }
            }
        }

        // -------------------------------------------------------------- Tab 2: Phối võ
        static string Tick(bool v) => v ? "<color=#9be37b>đủ</color>" : "<color=#d07a5a>chưa</color>";

        void BuildPhoi(GameEngine e, CharacterData c)
        {
            var linked = MartialProgression.LinkedSchool(c.PrimarySchool);
            var combo = MartialProgression.SchoolCombo(c.PrimarySchool);
            bool ready = MartialProgression.ComboReady(c, combo.first, combo.second);
            string style = MartialProgression.PlaystyleName(c);
            var w = MartialProgression.Weapon(c);

            var phoiLeftCard = DayMarketPopup.MakeSlotCard(body, new Vector2(-265f, 12f), new Vector2(516f, 430f));
            var left = DayMarketPopup.Text(phoiLeftCard.transform, Vector2.zero, new Vector2(486f, 404f), 14f, TextAlignmentOptions.TopLeft);
            left.richText = true;
            left.textWrappingMode = TextWrappingModes.Normal;
            left.raycastTarget = false;
            var sb = new System.Text.StringBuilder();
            sb.Append($"<b><color=#e8c26a>TRƯỜNG PHÁI VÕ HỌC</color></b>\nChính: <b>{MartialProgression.SchoolName(c.PrimarySchool)}</b> · Liên kết: {MartialProgression.SchoolName(linked)}\n\n");
            sb.Append($"<b><color=#e8c26a>HỢP KÍCH CỦA PHÁI</color></b>\n{combo.name}: {combo.effect}\nBộ chiêu đang lắp nối được: {Tick(ready)}\n\n");
            sb.Append("<b><color=#e8c26a>Ô VÕ KỸ & NĂNG LƯỢNG</color></b>\n");
            foreach (var slot in MartialProgression.AllSlots)
            {
                if (slot == CombatSlotType.TheSongThe && !c.IsMartialSlotUnlocked(slot)) { sb.Append("• Song Thế (R): khóa — cần Mệnh Ấn Song Thế\n"); continue; }
                var sk = MartialProgression.Slotted(c, slot);
                string energy = slot == CombatSlotType.TheCong ? "Thể lực" : slot == CombatSlotType.TheThu ? "Thể lực / Chống Chịu" : "Nộ";
                string state = !c.IsMartialSlotUnlocked(slot) ? $"<color=#d07a5a>khóa ({MartialProgression.SlotPairText(slot)})</color>" : sk == null ? "trống" : $"<b>{sk.Name}</b>";
                sb.Append($"• {MartialProgression.SlotName(slot)} [{energy}]: {state}\n");
            }
            sb.Append($"\n<b><color=#e8c26a>LỐI CHƠI: {style}</color></b> ({c.GetTotalWeightScore()} điểm cân)\n");
            sb.Append($"<b><color=#e8c26a>VŨ KHÍ</color></b>: {(w != null ? w.Name + " — " + w.MechanicText : "Tay không")}");
            left.text = sb.ToString();

            var phoiRightCard = DayMarketPopup.MakeSlotCard(body, new Vector2(265f, 112f), new Vector2(516f, 232f));
            var right = DayMarketPopup.Text(phoiRightCard.transform, Vector2.zero, new Vector2(486f, 208f), 14f, TextAlignmentOptions.TopLeft);
            right.richText = true;
            right.textWrappingMode = TextWrappingModes.Normal;
            right.raycastTarget = false;
            bool gate = e.DeepMechanics.MidGameGateUnlocked;
            right.text =
                $"<b><color=#e8c26a>ĐIỀU KIỆN PHÙ VĂN & MỆNH ẤN</color></b>\n" +
                $"• Cổng Giữa Game: {Tick(gate)} · >=12 đợt lớn: {Tick(e.MajorWavesSurvived >= 12)} ({e.MajorWavesSurvived}) · Cấp >=15: {Tick(c.Level >= 15)}\n" +
                $"• Phù Văn đang khắc ({c.EngravedRunes.Count}/6): {(c.EngravedRunes.Count > 0 ? string.Join(", ", c.EngravedRunes) : "Chưa khắc")}\n" +
                $"• Mệnh Ấn đang khắc ({c.EngravedSeals.Count}/3): {(c.EngravedSeals.Count > 0 ? string.Join(", ", c.EngravedSeals) : "Chưa khắc")}";

            var sch = DayMarketPopup.Text(body, new Vector2(265f, -32f), new Vector2(500f, 24f), 15f, TextAlignmentOptions.Left);
            sch.richText = true;
            sch.raycastTarget = false;
            sch.text = "<b><color=#e8c26a>Đổi phái chính (20.000G · 1 lần / 3 chu kỳ)</color></b>";
            var schools = new[] { MartialSchool.TocBo_A, MartialSchool.KhongChe_C, MartialSchool.TrongKich_B, MartialSchool.SinhTon_D };
            for (int i = 0; i < schools.Length; i++)
            {
                var sc = schools[i];
                var b = DayMarketPopup.Button(body, new Vector2(74f + i * 124f, -66f), new Vector2(118f, 34f), out var bl);
                bl.text = MartialProgression.SchoolName(sc);
                bl.fontSize = 14f;
                bl.fontStyle = FontStyles.Bold;
                bl.raycastTarget = false;
                b.onClick.AddListener(() => Say(MartialProgression.ChangePrimarySchool(e, sc).message));
            }

            bool shop = CanShop(e);
            var qb = DayMarketPopup.Text(body, new Vector2(265f, -110f), new Vector2(500f, 24f), 15f, TextAlignmentOptions.Left);
            qb.richText = true;
            qb.raycastTarget = false;
            var kinds = QuickBag.Kinds(c);
            qb.text = $"<b><color=#e8c26a>Túi nhanh chiến đấu ({kinds.Count}/{QuickBag.Slots} ô)</color></b>";
            int col = 0;
            foreach (var d in QuickBag.All.Where(x => x.PriceGold > 0))
            {
                var def = d;
                var b = DayMarketPopup.Button(body, new Vector2(96f + col * 168f, -148f), new Vector2(160f, 34f), out var bl);
                bl.text = $"{def.Name} ({def.PriceGold}G)";
                bl.fontSize = 14f;
                bl.fontStyle = FontStyles.Bold;
                bl.raycastTarget = false;
                b.onClick.AddListener(() =>
                {
                    if (!CanShop(e))
                    {
                        Say("Vật phẩm Túi Nhanh chỉ mua được tại Chợ Thị Trấn ban ngày (06:00–18:00).", rebuildAll: false);
                        return;
                    }
                    Say(QuickBag.Buy(e, def.Id).message);
                });
                col++;
            }
        }

        // -------------------------------------------------------------- Tab 3: Tiềm năng, Bàn Khắc & 6 Khóa Chuyên Biệt Ẩn
        void BuildEngraving(GameEngine e, CharacterData c, Vector2 origin)
        {
            var hdr = DayMarketPopup.Text(body, origin, new Vector2(360f, 26f), 15f, TextAlignmentOptions.Left);
            hdr.richText = true;
            hdr.raycastTarget = false;
            hdr.text = $"<b>Bàn Khắc</b> · Phù {c.EngravedRunes.Count}/6 · Ấn {c.EngravedSeals.Count}/3 · Mực <color=#FDE047><b>{RareSources.SealInkCount(e)}</b></color>";
            for (int i = 0; i < MartialProgression.RuneIds.Length; i++)
            {
                var rd = MartialProgression.RuneIds[i];
                bool has = c.EngravedRunes.Contains(rd.id);
                var rb = DayMarketPopup.Button(body, origin + new Vector2(-85f + (i % 2) * 172f, -30f - (i / 2) * 32f), new Vector2(166f, 28f), out var rbl);
                rbl.text = has ? $"√ {rd.name}" : $"Phù {rd.name}";
                rbl.fontSize = 14f;
                rbl.fontStyle = FontStyles.Bold;
                rbl.raycastTarget = false;
                rb.onClick.AddListener(() => Say(MartialProgression.EngraveRune(e, rd.id).message, rebuildAll: !has));
            }
            int rows = (MartialProgression.RuneIds.Length + 1) / 2;
            for (int i = 0; i < MartialProgression.SealIds.Length; i++)
            {
                var sd = MartialProgression.SealIds[i];
                bool has = c.EngravedSeals.Contains(sd.id);
                var sb = DayMarketPopup.Button(body, origin + new Vector2(-85f + (i % 2) * 172f, -36f - (rows + i / 2) * 32f), new Vector2(166f, 28f), out var sbl);
                sbl.text = has ? $"√ {sd.name}" : $"Ấn {sd.name}";
                sbl.fontSize = 14f;
                sbl.fontStyle = FontStyles.Bold;
                sbl.raycastTarget = false;
                sb.onClick.AddListener(() => Say(MartialProgression.EngraveSeal(e, sd.id).message, rebuildAll: !has));
            }
        }

        void BuildPotential(GameEngine e, CharacterData c)
        {
            BuildEngraving(e, c, new Vector2(100f, -24f));
            var st = c.Stamina;
            string sleepReason = PlayerRestCommands.SleepBlockedReason(e);
            var infoCard = DayMarketPopup.MakeSlotCard(body, new Vector2(-355f, 128f), new Vector2(354f, 174f));
            var info = DayMarketPopup.Text(infoCard.transform, Vector2.zero, new Vector2(332f, 158f), 14f, TextAlignmentOptions.TopLeft);
            info.richText = true;
            info.textWrappingMode = TextWrappingModes.Normal;
            info.raycastTarget = false;
            bool guardLow = c.MaxGuard > 0f && (c.CurrentGuard / c.MaxGuard) < 0.30f;
            float shrunkCap = guardLow ? Mathf.Round(st.EffectiveMaxStamina * 0.75f) : st.EffectiveMaxStamina;
            float shownCurSta = guardLow ? Mathf.Min(st.CurrentStamina, shrunkCap) : st.CurrentStamina;
            info.text =
                $"<b>CẤP & THỂ TRẠNG ({c.Name})</b>\n" +
                $"• Cấp <b>{c.Level}</b>/30 · EXP <b>{c.Exp}/{MartialProgression.ExpToNext(c.Level)}</b> · Điểm: <b><color=#9be37b>{c.StatPoints}</color></b>\n" +
                $"• Chống Chịu: <b>{c.CurrentGuard:0}/{c.MaxGuard:0}</b> · Nộ: <b>{c.Rage}/100</b>\n" +
                $"• Thể Lực: <b>{shownCurSta:0}/{shrunkCap:0}</b> (Tải ngày <b>{st.DailyLoad:0}</b>)\n" +
                $"• Thể trạng: <b>{st.Condition}/100</b> · Bữa ăn: <b>{st.MealsToday}/{StaminaSystem.MaxMealsPerDay}</b>";

            var restBtn = DayMarketPopup.Button(body, new Vector2(-442f, 14f), new Vector2(164f, 32f), out var rbt);
            rbt.text = "Ngồi nghỉ 1h (+6 TL)";
            rbt.fontSize = 14f;
            rbt.fontStyle = FontStyles.Bold;
            rbt.raycastTarget = false;
            restBtn.onClick.AddListener(() => Say(PlayerRestCommands.Rest(e, 1).message));

            var meal1Btn = DayMarketPopup.Button(body, new Vector2(-268f, 14f), new Vector2(164f, 32f), out var m1t);
            m1t.text = "Ăn bữa thường (+20)";
            m1t.fontSize = 14f;
            m1t.fontStyle = FontStyles.Bold;
            m1t.raycastTarget = false;
            meal1Btn.onClick.AddListener(() => Say(PlayerRestCommands.EatMeal(e, false).message));

            var meal2Btn = DayMarketPopup.Button(body, new Vector2(-442f, -22f), new Vector2(164f, 32f), out var m2t);
            m2t.text = "Ăn bữa tốt (+35)";
            m2t.fontSize = 14f;
            m2t.fontStyle = FontStyles.Bold;
            m2t.raycastTarget = false;
            meal2Btn.onClick.AddListener(() => Say(PlayerRestCommands.EatMeal(e, true).message));

            var sleepBtn = DayMarketPopup.Button(body, new Vector2(-268f, -22f), new Vector2(164f, 32f), out var slt);
            slt.text = "Ngủ: Nhà Chính";
            slt.fontSize = 14f;
            slt.fontStyle = FontStyles.Bold;
            slt.raycastTarget = false;
            sleepBtn.onClick.AddListener(() => Say(sleepReason ?? "Hãy tương tác với Giường tại Nhà Chính để ngủ tới 06:00 và lưu tiến trình.", rebuildAll: false));

            var stats = new[] { SkillStat.Str, SkillStat.Agi, SkillStat.Ctrl, SkillStat.Res };
            for (int i = 0; i < stats.Length; i++)
            {
                var stat = stats[i];
                var card = DayMarketPopup.MakeSlotCard(body, new Vector2(-355f, -68f - i * 44f), new Vector2(348f, 40f));
                var t = DayMarketPopup.Text(card.transform, new Vector2(-32f, 0f), new Vector2(264f, 36f), 14f, TextAlignmentOptions.Left);
                t.richText = true;
                t.raycastTarget = false;
                int baseV = MartialProgression.BaseStat(c, stat), bonus = MartialProgression.BonusStat(c, stat);
                t.text = $"<b>{MartialProgression.StatName(stat)} gốc {baseV}</b>" +
                         (bonus > 0 ? $" <color=#9be37b>(+{bonus} đồ = {baseV + bonus})</color>" : "");
                var b = DayMarketPopup.Button(card.transform, new Vector2(134f, 0f), new Vector2(64f, 30f), out var bl);
                int cost = MartialProgression.StatPointCost(baseV);
                bl.text = cost > 1 ? "+1 (2đ)" : "+1";
                bl.fontSize = 14f;
                bl.fontStyle = FontStyles.Bold;
                bl.raycastTarget = false;
                b.onClick.AddListener(() => Say(MartialProgression.SpendStatPoint(c, stat).message));
            }

            var locks = e.DeepMechanics.SpecialtyLocks;
            locks.Owner = c.Identity;
            var rightCard = DayMarketPopup.MakeSlotCard(body, new Vector2(195f, 122f), new Vector2(680f, 188f));
            var rtText = DayMarketPopup.Text(rightCard.transform, Vector2.zero, new Vector2(648f, 172f), 14f, TextAlignmentOptions.TopLeft);
            rtText.richText = true;
            rtText.textWrappingMode = TextWrappingModes.Normal;
            rtText.raycastTarget = false;

            var sb = new System.Text.StringBuilder();
            sb.AppendLine($"<b>ĐẶC TÍNH & 6 KHÓA CHUYÊN BIỆT ẨN ({locks.CompletedCount}/6 hoàn tất, {locks.RevealedLocks}/6 đã lộ)</b>");
            for (int n = 1; n <= SpecialtyLockTracker.LockCount; n++)
            {
                bool rev = locks.IsRevealed(n);
                bool done = locks.IsCompleted(n);
                var (prog, req) = locks.Progress(n);
                if (!rev)
                    sb.AppendLine($"• <b>Khóa {n}:</b> <color=#9a8a70>[Chưa lộ — Dùng Giấy Chuyên Biệt mở theo thứ tự 1->6]</color>");
                else
                    sb.AppendLine($"• <b>Khóa {n}:</b> {SpecialtyLockTracker.ConditionText(c.Identity, n)} — {(done ? "<color=#9be37b>[XONG]</color>" : $"({prog}/{req})")}");
            }
            rtText.text = sb.ToString();

            var paperColor = e.DeepMechanics.EquippedPaperColor;
            int owned = SpecialtyPaperActions.OwnedCount(e, paperColor);

            var colorBtn = DayMarketPopup.Button(body, new Vector2(-46f, -198f), new Vector2(158f, 34f), out var cbt);
            cbt.text = $"Giấy {paperColor} ×{owned}";
            cbt.fontSize = 14f;
            cbt.fontStyle = FontStyles.Bold;
            cbt.raycastTarget = false;
            colorBtn.onClick.AddListener(() =>
            {
                if (!SpecialtyPaperActions.SelectNextOwned(e))
                {
                    Say("Chưa có Giấy Chuyên Biệt nào trong kho.", rebuildAll: false);
                    return;
                }
                Refresh();
            });

            var usePaperBtn = DayMarketPopup.Button(body, new Vector2(124f, -198f), new Vector2(178f, 34f), out var upt);
            upt.text = "Dùng Giấy Lộ Khóa";
            upt.fontSize = 14f;
            upt.fontStyle = FontStyles.Bold;
            upt.raycastTarget = false;
            usePaperBtn.onClick.AddListener(() =>
            {
                var rng = new System.Random(Environment.TickCount);
                Say(SpecialtyPaperActions.Use(e, () => rng.NextDouble()).message);
            });

            var enhPaperBtn = DayMarketPopup.Button(body, new Vector2(312f, -198f), new Vector2(188f, 34f), out var ept);
            ept.text = $"Gia Cường (+{e.DeepMechanics.intEquippedPaperEnhancementLevel})";
            ept.fontSize = 14f;
            ept.fontStyle = FontStyles.Bold;
            ept.raycastTarget = false;
            enhPaperBtn.onClick.AddListener(() => Say(SpecialtyPaperActions.Enhance(e).Message));

            var salvageBtn = DayMarketPopup.Button(body, new Vector2(476f, -198f), new Vector2(128f, 34f), out var svt);
            svt.text = "Rã Giấy (+Đá)";
            svt.fontSize = 14f;
            svt.fontStyle = FontStyles.Bold;
            svt.raycastTarget = false;
            salvageBtn.onClick.AddListener(() => Say(SpecialtyPaperActions.Salvage(e).message));
        }

        // -------------------------------------------------------------- Tab 4: Sổ Tay & Bề Mặt Phản Chiếu
        void BuildNotebookAndReflection(GameEngine e, CharacterData c)
        {
            var leftCard = DayMarketPopup.MakeSlotCard(body, new Vector2(-225f, 20f), new Vector2(590f, 390f));
            var lt = DayMarketPopup.Text(leftCard.transform, Vector2.zero, new Vector2(558f, 362f), 14f, TextAlignmentOptions.TopLeft);
            lt.richText = true;
            lt.textWrappingMode = TextWrappingModes.Normal;
            lt.raycastTarget = false;

            var sb = new System.Text.StringBuilder();
            if (c.Identity == PlayerId.Khoa)
            {
                sb.AppendLine("<b><color=#FFE270>SỔ HAI CỘT CỦA KHOA ([THẤY] | [CÓ THẬT] | [LỀ BÚT CHÌ])</color></b>\n");
                foreach (var entry in e.Story.NotebookEntries)
                {
                    sb.AppendLine($"• <b>{entry.Title}</b>");
                    sb.AppendLine($"   [THẤY]: {entry.LeftColumn_Thay}");
                    if (entry.IsVerifiedToRightColumn && !string.IsNullOrEmpty(entry.RightColumn_CoThat))
                        sb.AppendLine($"   <color=#9be37b>[CÓ THẬT]: {entry.RightColumn_CoThat}</color>");
                    sb.AppendLine($"   <i><color=#d8c49a>[Lề bút chì]: {entry.PencilMargin_SuyDoan}</color></i>\n");
                }
            }
            else
            {
                sb.AppendLine("<b><color=#f9a8d4>SỔ BÌA HỒNG DÁN STICKER CỦA AN ([ĐÃ GẶP] | [TỐI NAY NGHĨ])</color></b>\n");
                foreach (var entry in e.Story.AnPinkNotebookEntries.Take(4))
                {
                    sb.AppendLine($"• <b>{entry.RecipientSalutation}</b> [{entry.Sticker}]");
                    sb.AppendLine($"   {entry.LetterBody}");
                    sb.AppendLine($"   <i><color=#d8c49a>[Lề bút chì]: {entry.PencilMarginHypothesis}</color></i>\n");
                }
            }
            lt.text = sb.ToString();

            var rightCard = DayMarketPopup.MakeSlotCard(body, new Vector2(315f, 20f), new Vector2(440f, 390f));
            string[] surfaces = { "Mirror_7Mornings", "WindowGlass_Q10", "WaterTrough_S07", "Necklace7Notches_Q19", "EndlessMirrors_Ending83" };
            string curSurface = surfaces[reflectionIndex % surfaces.Length];
            var (realFace, reflFace, ctx) = StoryJournalSystem.EvaluateReflectiveSurfaceEmotionForProtagonist(curSurface, c.Identity);

            var rt = DayMarketPopup.Text(rightCard.transform, new Vector2(0f, 18f), new Vector2(408f, 336f), 14f, TextAlignmentOptions.TopLeft);
            rt.richText = true;
            rt.textWrappingMode = TextWrappingModes.Normal;
            rt.raycastTarget = false;
            rt.text =
                $"<b><color=#FFE270>BỀ MẶT PHẢN CHIẾU ({c.Name})</color></b>\n" +
                $"<color=#d8c49a>{ctx}</color>\n\n" +
                $"• <b>Gương mặt ngoài đời:</b>\n{realFace}\n\n" +
                $"• <b>Bóng phản chiếu (chậm 2 giây):</b>\n<color=#7dd3fc>{reflFace}</color>";

            var nextReflBtn = DayMarketPopup.Button(body, new Vector2(315f, -198f), new Vector2(268f, 36f), out var nrt);
            nrt.text = "Soi Bề Mặt Phản Chiếu Kế Tiếp";
            nrt.fontSize = 15f;
            nrt.fontStyle = FontStyles.Bold;
            nrt.raycastTarget = false;
            nextReflBtn.onClick.AddListener(() =>
            {
                reflectionIndex++;
                string sKey = (c.Identity == PlayerId.An ? "An_" : "") + surfaces[reflectionIndex % surfaces.Length];
                e.CheckPlayerReflectiveSurface(sKey);
                Refresh();
            });
        }
    }
}
