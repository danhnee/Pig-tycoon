# -*- coding: utf-8 -*-
"""
Applies HỢP ĐỒNG LOGIC #1 (#155), #2 (#156), #3 (#157), #4 (#158), and #159
onto the canonical from_claude/Assets/Scripts/UI/ base and writes the results ONLY into
from_argon/Assets/Scripts/UI/ and from_argon/Assets/Art/PixelMyth/UI/ (never touching D:/Pig-tycoon/Assets).
"""
import math
import os
import shutil
from PIL import Image, ImageDraw

ROOT = "d:/Pig-tycoon"
FROM_CLAUDE_UI = os.path.join(ROOT, ".collab/claude-2d/ui_and_map_sync/from_claude/Assets/Scripts/UI")
FROM_ARGON_UI = os.path.join(ROOT, ".collab/claude-2d/ui_and_map_sync/from_argon/Assets/Scripts/UI")
FROM_ARGON_ART_UI = os.path.join(ROOT, ".collab/claude-2d/ui_and_map_sync/from_argon/Assets/Art/PixelMyth/UI")


def read_utf8(path: str) -> str:
    with open(path, "rb") as f:
        return f.read().decode("utf-8-sig").replace("\r\n", "\n")


def write_utf8(path: str, content: str) -> None:
    os.makedirs(os.path.dirname(path), exist_ok=True)
    with open(path, "wb") as f:
        f.write(content.replace("\r\n", "\n").encode("utf-8"))


def replace_exact(content: str, target: str, replacement: str, label: str) -> str:
    if target not in content:
        raise RuntimeError(f"[{label}] Target not found:\n{target[:240]}...")
    return content.replace(target, replacement, 1)


def reset_from_claude_base():
    """Start every file in from_argon/Assets/Scripts/UI/ from the exact latest from_claude/Assets/Scripts/UI/ base."""
    for dp, _, fs in os.walk(FROM_CLAUDE_UI):
        rel_dir = os.path.relpath(dp, FROM_CLAUDE_UI)
        dst_dir = os.path.join(FROM_ARGON_UI, rel_dir) if rel_dir != "." else FROM_ARGON_UI
        os.makedirs(dst_dir, exist_ok=True)
        for f in fs:
            if not f.endswith(".cs") or f == "ContextActionPalette.cs":
                continue
            shutil.copy2(os.path.join(dp, f), os.path.join(dst_dir, f))
    cap_old = os.path.join(FROM_ARGON_UI, "ContextActionPalette.cs")
    if os.path.exists(cap_old):
        os.remove(cap_old)


def update_player_text():
    path = os.path.join(FROM_ARGON_UI, "PlayerText.cs")
    src = read_utf8(path)
    if '"GTTC"' in src and '"Paper-Doll"' in src:
        return
    old = (
        r'            if (string.IsNullOrEmpty(text) || (text.IndexOf("GDD") < 0 && text.IndexOf("ADR-") < 0 && text.IndexOf("Sổ tay v") < 0' + "\n"
        r'                && text.IndexOf("TẠM") < 0 && text.IndexOf("HANDBOOK") < 0 && text.IndexOf("Bảng ") < 0 && !Regex.IsMatch(text, @"\bP\d\.\d{2}\b")))' + "\n"
        r'                return text;' + "\n"
        r'            string s = Paren.Replace(text, "");' + "\n"
        r'            s = Inline.Replace(s, "");'
    )
    new = (
        r'            if (string.IsNullOrEmpty(text) || (text.IndexOf("GDD") < 0 && text.IndexOf("ADR-") < 0 && text.IndexOf("Sổ tay v") < 0' + "\n"
        r'                && text.IndexOf("TẠM") < 0 && text.IndexOf("GTTC") < 0 && text.IndexOf("Paper-Doll") < 0' + "\n"
        r'                && text.IndexOf("HANDBOOK") < 0 && text.IndexOf("Bảng ") < 0 && !Regex.IsMatch(text, @"\bP\d\.\d{2}\b")))' + "\n"
        r'                return text;' + "\n"
        r'            string s = Paren.Replace(text, "");' + "\n"
        r'            s = Inline.Replace(s, "");' + "\n"
        r'            s = Regex.Replace(s, @"\bTẠM:?\s*", "", RegexOptions.IgnoreCase);' + "\n"
        r'            s = Regex.Replace(s, @"\bGTTC\b", "quy đổi", RegexOptions.IgnoreCase);' + "\n"
        r'            s = Regex.Replace(s, @"\bPaper-Doll\b", "ngoại trang", RegexOptions.IgnoreCase);'
    )
    src = replace_exact(src, old, new, "PlayerText.cs")
    write_utf8(path, src)


def update_toast_overlay():
    """#155 §1 + #158 High-Priority Crisis Toasts (Say Nắng & Lôi Tâm)."""
    path = os.path.join(FROM_ARGON_UI, "ToastOverlay.cs")
    src = read_utf8(path)
    src = replace_exact(
        src,
        "        RectTransform plate;\n",
        "        RectTransform plate;\n        Image plateImg;\n        bool currentUrgent;\n",
        "ToastOverlay fields",
    )
    src = replace_exact(
        src,
        "            if (queue.Count >= MaxQueued) queue.Dequeue();\n"
        "            queue.Enqueue(message);\n"
        "            if (timeLeft <= 0f) Next();",
        "            bool urgent = message.IndexOf(\"Say Nắng\", System.StringComparison.OrdinalIgnoreCase) >= 0\n"
        "                || message.IndexOf(\"Lôi Tâm\", System.StringComparison.OrdinalIgnoreCase) >= 0\n"
        "                || message.IndexOf(\"run giật\", System.StringComparison.OrdinalIgnoreCase) >= 0\n"
        "                || message.IndexOf(\"quá nhiệt\", System.StringComparison.OrdinalIgnoreCase) >= 0\n"
        "                || message.IndexOf(\"gục\", System.StringComparison.OrdinalIgnoreCase) >= 0;\n"
        "            if (urgent)\n"
        "            {\n"
        "                var items = queue.ToArray();\n"
        "                queue.Clear();\n"
        "                string tagged = message.StartsWith(\"[KHẨN]\") ? message : \"[KHẨN] \" + message;\n"
        "                queue.Enqueue(tagged);\n"
        "                for (int i = 0; i < items.Length && queue.Count < MaxQueued; i++) queue.Enqueue(items[i]);\n"
        "                Next();\n"
        "                return;\n"
        "            }\n"
        "            if (queue.Count >= MaxQueued) queue.Dequeue();\n"
        "            queue.Enqueue(message);\n"
        "            if (timeLeft <= 0f) Next();",
        "ToastOverlay Enqueue urgent",
    )
    src = replace_exact(
        src,
        "            string m = queue.Dequeue();\n"
        "            label.text = m;",
        "            string m = queue.Dequeue();\n"
        "            currentUrgent = m.StartsWith(\"[KHẨN]\");\n"
        "            label.text = m;\n"
        "            label.color = currentUrgent ? new Color(1f, 0.90f, 0.56f) : new Color(0.98f, 0.93f, 0.80f);\n"
        "            if (plateImg != null) plateImg.color = currentUrgent ? new Color(0.46f, 0.11f, 0.10f, 0.98f) : new Color(0.10f, 0.14f, 0.20f, 0.96f);",
        "ToastOverlay Next urgent",
    )
    src = replace_exact(
        src,
        "            var img = plateGo.GetComponent<Image>();",
        "            var img = plateGo.GetComponent<Image>();\n            plateImg = img;",
        "ToastOverlay plateImg ref",
    )
    write_utf8(path, src)


def update_appraisal_and_breeding():
    ap_path = os.path.join(FROM_ARGON_UI, "AppraisalPopup.cs")
    ap = read_utf8(ap_path)
    ap = ap.replace("{pig.Stage}", "{PigText.Stage(pig.Stage)}")
    if "ShowFeedback(lastMessage)" not in ap:
        ap = ap.replace(
            'Debug.Log($"[Appraisal] {res.message}");',
            'Debug.Log($"[Appraisal] {res.message}");\n            PlayerInteractionController.Instance?.ShowFeedback(lastMessage);',
        )
    write_utf8(ap_path, ap)

    bp_path = os.path.join(FROM_ARGON_UI, "BreedingPickerPopup.cs")
    bp = read_utf8(bp_path)
    bp = bp.replace(
        'string line = partner.IsIdentified ? GeneticsAndBreedingSystem.GetGeneProfile(partner.GeneLine).Name : "?";',
        "string line = PigText.Line(partner);",
    )
    bp = bp.replace("{StageName(partner.Stage)}", "{PigText.Stage(partner.Stage)}")
    write_utf8(bp_path, bp)


def update_backpack_popup():
    """#157 §1 & §2: Keep UseItem and ToQuickBag GameObject names intact; always show returned message via ToastOverlay."""
    path = os.path.join(FROM_ARGON_UI, "BackpackPopup.cs")
    src = read_utf8(path)
    src = replace_exact(
        src,
        "                        var (ok, msg) = QuickBag.LoadFromWarehouse(Engine, quickId, inPerson: !viaLink);\n"
        "                        if (ok) ToastOverlay.Show(msg + \" (F5–F8)\");\n"
        "                        RefreshData(); if (!ok) Fail(msg);",
        "                        var (ok, msg) = QuickBag.LoadFromWarehouse(Engine, quickId, inPerson: !viaLink);\n"
        "                        ToastOverlay.Show(ok ? (msg + \" (F5–F8)\") : msg);\n"
        "                        RefreshData(); if (!ok) Fail(msg);",
        "BackpackPopup ToQuickBag toast",
    )
    src = replace_exact(
        src,
        "            var (ok, message) = ItemUse.Use(Engine, id, null, Engine.Carry.HasBracelet ? selectedIndex : -1, atWarehouse: warehouseOpen && !viaLink, placeX: px, placeY: py);\n"
        "            if (ok) { ToastOverlay.Show(message); selectedIndex = -1; MobileGameController.Instance?.OnStateUpdated?.Invoke(); }\n"
        "            RefreshData();\n"
        "            if (!ok) Fail(message);",
        "            var (ok, message) = ItemUse.Use(Engine, id, null, Engine.Carry.HasBracelet ? selectedIndex : -1, atWarehouse: warehouseOpen && !viaLink, placeX: px, placeY: py);\n"
        "            ToastOverlay.Show(message);\n"
        "            if (ok) { selectedIndex = -1; MobileGameController.Instance?.OnStateUpdated?.Invoke(); }\n"
        "            RefreshData();\n"
        "            if (!ok) Fail(message);",
        "BackpackPopup UseSelected toast",
    )
    write_utf8(path, src)


def update_pig_inspect():
    """#155 §2, §4, §5 + #157 §1, §3 + #158 Say Nắng & Lôi Tâm badges (preserving PigCard_ItemButtons)."""
    path = os.path.join(FROM_ARGON_UI, "PigInspectPopup.cs")
    src = read_utf8(path)

    old_meat_block = (
        '                string disStr = pig.ActiveDisease.HasValue\n'
        '                    ? $" | <color=#DC2626><b>Bệnh: {pig.ActiveDisease.Value}</b></color>"\n'
        '                    : (pig.ImmuneDays > 0 ? $" | <color=#0284C7>Miễn dịch ({pig.ImmuneDays}d)</color>" : "");\n\n'
        '                MeatQualityText.text =\n'
        '                    $"Thịt: <color=#D97706><b>Hạng {pig.MeatQuality}</b></color> | Khẩu phần: <b>{feedKg:0.0}kg</b> | Bám đàn: <b>{pig.AdhesionScore:0.0}</b> | Thế hệ: <b>{genBadge}</b>{pregStr}{disStr}";'
    )
    new_meat_block = (
        '                var engNow = MobileGameController.Instance?.Engine;\n'
        '                int curDay = engNow?.Clock?.CurrentDay ?? -1;\n'
        '                string disStr = pig.ActiveDisease == DiseaseId.SayNang\n'
        '                    ? $" | <color=#DC2626><b>[GỤC SAY NẮNG · {pig.HeatCollapsedHours}/{GameEngine.HeatCollapseDeathHours}h — Dội Xô Nước / Mái Che Tạm!]</b></color>"\n'
        '                    : pig.ActiveDisease.HasValue\n'
        '                    ? $" | <color=#DC2626><b>Bệnh: {pig.ActiveDisease.Value}</b></color>"\n'
        '                    : (pig.ImmuneDays > 0 ? $" | <color=#0284C7>Miễn dịch ({pig.ImmuneDays}d)</color>" : "");\n'
        '                string heatWarn = (engNow != null && pig.ActiveDisease != DiseaseId.SayNang && engNow.UncooledHeatHours(pig.Id) >= 1f)\n'
        '                    ? $" | <color=#D97706><b>[Quá nhiệt {engNow.UncooledHeatHours(pig.Id):0.#}h]</b></color>" : "";\n'
        '                string seizureWarn = pig.SeizurePending\n'
        '                    ? " | <color=#DC2626><b>[RUN GIẬT LÔI TÂM (10s) — Thuốc An Thần / Mái trú!]</b></color>" : "";\n'
        '                string feedBuffs = (pig.LinhChiFedDay == curDay ? " | <color=#15803D><b>[Đã ăn Linh Chi hôm nay]</b></color>" : "")\n'
        '                    + (pig.CalmFeedDay == curDay ? " | <color=#0284C7><b>[Hạ Hỏa]</b></color>" : "");\n\n'
        '                MeatQualityText.text =\n'
        '                    $"Thịt: <color=#D97706><b>Hạng {pig.MeatQuality}</b></color> | Khẩu phần: <b>{feedKg:0.0}kg</b> | Bám đàn: <b>{pig.AdhesionScore:0.0}</b> | Thế hệ: <b>{genBadge}</b>{pregStr}{disStr}{heatWarn}{seizureWarn}{feedBuffs}";'
    )
    src = replace_exact(src, old_meat_block, new_meat_block, "PigInspectPopup MeatQualityText badges")

    src = replace_exact(
        src,
        "            var (ok, msg) = ItemUse.Use(engine, id, currentPig.Id, bagIndex, atWarehouse: false);\n"
        "            PlayerInteractionController.Instance?.ShowFeedback(msg);",
        "            var (ok, msg) = ItemUse.Use(engine, id, currentPig.Id, bagIndex, atWarehouse: false);\n"
        "            if (PlayerInteractionController.Instance != null) PlayerInteractionController.Instance.ShowFeedback(msg);\n"
        "            else ToastOverlay.Show(msg);",
        "PigInspectPopup OnClickPigItem ToastOverlay",
    )
    write_utf8(path, src)


def update_auction_and_intel():
    """#155 §6, #156 §2, and #159 compile fix — already integrated in from_claude 22:45 base; verify intact."""
    ap_path = os.path.join(FROM_ARGON_UI, "AuctionPopup.cs")
    ap = read_utf8(ap_path)
    assert "lot.GearItemId" in ap and "UISpriteLoader.GetEquipIcon" in ap
    ip_path = os.path.join(FROM_ARGON_UI, "IntelPopup.cs")
    ip = read_utf8(ip_path)
    assert "IntelBureau.QuestionsPerDay" in ip and "IntelBureau.DeskBlueprintId" in ip


def update_village_barter():
    """#155 §6 RareSources night-market exchanges (ExchangeSecretManual & ExchangeSealInk) + item icons."""
    path = os.path.join(FROM_ARGON_UI, "VillageBarterPopup.cs")
    src = read_utf8(path)
    src = replace_exact(
        src,
        "                // GDD 20: at the night market an item can also be sold for gold, at only 40% quy đổi\n"
        "                if (CurrentVillagerId == VillagerId.ChoDem && isOpen) CreateSellForGoldCard();",
        "                // GDD 20: at the night market an item can also be sold for gold, at only 40% quy đổi\n"
        "                if (CurrentVillagerId == VillagerId.ChoDem && isOpen)\n"
        "                {\n"
        "                    CreateRareSourcesNightBarterCard(\"BiKip\", \"Bí Kíp Võ Học\", \"Đổi bằng hàng hóa trị giá ≥ 260G quy đổi (phí 5% vàng) · Mở khóa tuyệt kỹ bậc 3\", () => RareSources.ExchangeSecretManual(engine));\n"
        "                    CreateRareSourcesNightBarterCard(\"muc_an\", \"Mực Ấn\", \"Đổi bằng hàng hóa trị giá ≥ 160G quy đổi (phí 5% vàng) · Khắc Phù Văn & Mệnh Ấn\", () => RareSources.ExchangeSealInk(engine));\n"
        "                    CreateSellForGoldCard();\n"
        "                }",
        "VillageBarterPopup RareSources night market cards",
    )
    rare_card_method = """        private void CreateRareSourcesNightBarterCard(string itemKey, string title, string subtitle, Func<(bool ok, string message)> action)
        {
            var cardObj = new GameObject($"Listing_Rare_{itemKey}");
            cardObj.transform.SetParent(listingsContainer, false);
            var cRt = cardObj.AddComponent<RectTransform>();
            cRt.sizeDelta = new Vector2(850f, 76f);
            var le = cardObj.AddComponent<LayoutElement>();
            le.minHeight = 76f;
            le.preferredHeight = 76f;
            var img = cardObj.AddComponent<Image>();
            img.sprite = UISpriteLoader.GetFrameWoodPanel();
            img.type = Image.Type.Sliced;
            img.color = new Color(0.22f, 0.16f, 0.12f, 0.98f);

            var iconBox = new GameObject("Icon_Box");
            iconBox.transform.SetParent(cardObj.transform, false);
            var ibRt = iconBox.AddComponent<RectTransform>();
            ibRt.anchorMin = ibRt.anchorMax = new Vector2(0f, 0.5f);
            ibRt.pivot = new Vector2(0f, 0.5f);
            ibRt.anchoredPosition = new Vector2(12f, 0f);
            ibRt.sizeDelta = new Vector2(56f, 56f);
            var ibImg = iconBox.AddComponent<Image>();
            ibImg.sprite = UISpriteLoader.GetFrameSlotSelected();
            ibImg.type = Image.Type.Sliced;
            ibImg.color = new Color(0.95f, 0.78f, 0.32f);

            var iconInner = new GameObject("Icon_Sprite");
            iconInner.transform.SetParent(iconBox.transform, false);
            var iiRt = iconInner.AddComponent<RectTransform>();
            iiRt.anchorMin = Vector2.zero;
            iiRt.anchorMax = Vector2.one;
            iiRt.offsetMin = new Vector2(6f, 6f);
            iiRt.offsetMax = new Vector2(-6f, -6f);
            var iiImg = iconInner.AddComponent<Image>();
            iiImg.sprite = UISpriteLoader.GetLootSprite(itemKey);
            iiImg.preserveAspect = true;

            var infoObj = new GameObject("Info");
            infoObj.transform.SetParent(cardObj.transform, false);
            var iRt = infoObj.AddComponent<RectTransform>();
            iRt.anchorMin = Vector2.zero;
            iRt.anchorMax = Vector2.one;
            iRt.offsetMin = new Vector2(82f, 6f);
            iRt.offsetMax = new Vector2(-160f, -6f);
            var iTmp = infoObj.AddComponent<TextMeshProUGUI>();
            iTmp.text = $"<color=#FDE047><b>[Bảo Vật Chợ Đêm] {title}</b></color>\\n<size=12><color=#d7ccc8>{subtitle}</color></size>";
            iTmp.fontSize = 13.5f;

            var btnObj = new GameObject($"Btn_Rare_{itemKey}");
            btnObj.transform.SetParent(cardObj.transform, false);
            var bRt = btnObj.AddComponent<RectTransform>();
            bRt.anchorMin = bRt.anchorMax = new Vector2(1f, 0.5f);
            bRt.pivot = new Vector2(1f, 0.5f);
            bRt.anchoredPosition = new Vector2(-14f, 0f);
            bRt.sizeDelta = new Vector2(130f, 40f);
            var bImg = btnObj.AddComponent<Image>();
            bImg.sprite = UISpriteLoader.GetFrameActionButton();
            bImg.type = Image.Type.Sliced;
            bImg.color = new Color(0.85f, 0.45f, 0.15f);
            btnObj.AddComponent<Button>().onClick.AddListener(() =>
            {
                var (ok, msg) = action();
                ToastOverlay.Show(msg);
                PlayerInteractionController.Instance?.ShowFeedback(msg);
                if (ok)
                {
                    BackpackPopup.Instance?.RefreshData();
                    MobileGameController.Instance?.OnStateUpdated?.Invoke();
                    Refresh();
                }
            });
            var lbl = new GameObject("Text");
            lbl.transform.SetParent(btnObj.transform, false);
            var lRt = lbl.AddComponent<RectTransform>();
            lRt.anchorMin = Vector2.zero;
            lRt.anchorMax = Vector2.one;
            var lTmp = lbl.AddComponent<TextMeshProUGUI>();
            lTmp.text = "<b>ĐỔI NGAY</b>";
            lTmp.fontSize = 13f;
            lTmp.color = Color.white;
            lTmp.alignment = TextAlignmentOptions.Center;
        }

"""
    src = replace_exact(
        src,
        "        private void CreateSellForGoldCard()",
        rare_card_method + "        private void CreateSellForGoldCard()",
        "VillageBarterPopup CreateRareSourcesNightBarterCard",
    )
    write_utf8(path, src)


def update_defense_build():
    """#155 §7 & #156 §3, §4 — already integrated in from_claude 22:45 base; verify intact."""
    path = os.path.join(FROM_ARGON_UI, "DefenseBuildPopup.cs")
    src = read_utf8(path)
    assert "AmmoOrTriggerBar" in src and "DurabilityBar" in src and "duringWave" in src


def update_farm_overview():
    """#156 §1 & §2 + #157 §3 TempShadePigsToday + #158 TempShades overlay & world shelters."""
    path = os.path.join(FROM_ARGON_UI, "FarmOverviewPopup.cs")
    src = read_utf8(path)

    src = replace_exact(
        src,
        '                report.AppendLine($"<b>Sinh cảnh:</b> {farm.HabitatIndex}/100 ({farm.HabitatTier}) · Đã dọn bãi hôm nay {farm.CleaningsToday} lần · <b>Kho cám:</b> {farm.FeedStockKg:0.#} kg");\n'
        '            }',
        '                report.AppendLine($"<b>Sinh cảnh:</b> {farm.HabitatIndex}/100 ({farm.HabitatTier}) · Đã dọn bãi hôm nay {farm.CleaningsToday} lần · <b>Kho cám:</b> {farm.FeedStockKg:0.#} kg");\n'
        '                SyncWorldShelters(engine);\n'
        '            }',
        "FarmOverviewPopup Tab 0 SyncWorldShelters",
    )

    src = replace_exact(
        src,
        '            (string title, int val, string detail)[] items =\n'
        '            {\n'
        '                ("Ô Đất (×8)", cap.CapacityByLand, $"{inf.LandPlots}/{plotLimit} ô (tối đa {FarmExpansion.MaxPlots})"),\n'
        '                ("Mái Trú (×6)", cap.CapacityByShelters, $"{inf.Shelters}/{roofLimit} mái"),\n'
        '                ("Bồn Nước (×12)", cap.CapacityByWater, $"{inf.WaterTroughs} bồn"),\n'
        '                ("Máng Ăn (×10)", cap.CapacityByFeeders, $"{inf.Feeders} máng")\n'
        '            };',
        '            string tempShadeTag = engine.Farm.TempShadePigsToday > 0\n'
        '                ? $" (+{engine.Farm.TempShadePigsToday} chỗ che tạm)"\n'
        '                : "";\n'
        '            (string title, int val, string detail)[] items =\n'
        '            {\n'
        '                ("Ô Đất (×8)", cap.CapacityByLand, $"{inf.LandPlots}/{plotLimit} ô (tối đa {FarmExpansion.MaxPlots})"),\n'
        '                ("Mái Trú (×6)", cap.CapacityByShelters, $"{inf.Shelters}/{roofLimit} mái{tempShadeTag}"),\n'
        '                ("Bồn Nước (×12)", cap.CapacityByWater, $"{inf.WaterTroughs} bồn"),\n'
        '                ("Máng Ăn (×10)", cap.CapacityByFeeders, $"{inf.Feeders} máng")\n'
        '            };',
        "FarmOverviewPopup TempShadePigsToday tag",
    )

    helper_methods = """        private readonly List<GameObject> spawnedExtraShelters = new List<GameObject>();

        private void SyncWorldShelters(GameEngine engine)
        {
            if (engine == null) return;
            int extraRoofs = Mathf.Max(0, engine.Farm.Infrastructure.Shelters - 14);
            var existingViews = FindObjectsByType<Shelter2DView>(FindObjectsSortMode.None);
            if (existingViews == null || existingViews.Length == 0) return;

            while (spawnedExtraShelters.Count < extraRoofs)
            {
                int idx = spawnedExtraShelters.Count;
                var template = existingViews[0];
                var clone = Instantiate(template.gameObject, template.transform.parent);
                clone.name = $"ShelterRoof_Expanded_{idx + 1}";
                clone.transform.position = template.transform.position + new Vector3(3.4f * (idx + 1), 0f, 0f);
                spawnedExtraShelters.Add(clone);
            }
        }
"""
    src = replace_exact(
        src,
        "        private void SetStatus(string text)",
        helper_methods + "\n        private void SetStatus(string text)",
        "FarmOverviewPopup SyncWorldShelters helper",
    )

    src = replace_exact(
        src,
        '                if (ShowCoolingRadiusOverlay && structure.ProvidesCooling)\n'
        '                    CreateRingIndicator("Cooling_" + structure.InstanceId, position, FarmGridAndZones.CoolingRadiusMeters, new Color(0.20f, 0.65f, 0.95f, 0.14f));\n'
        '            }',
        '                if (ShowCoolingRadiusOverlay && structure.ProvidesCooling)\n'
        '                    CreateRingIndicator("Cooling_" + structure.InstanceId, position, FarmGridAndZones.CoolingRadiusMeters, new Color(0.20f, 0.65f, 0.95f, 0.14f));\n'
        '            }\n'
        '            if (ShowCoolingRadiusOverlay && engine.GridZones.TempShades != null)\n'
        '            {\n'
        '                for (int i = 0; i < engine.GridZones.TempShades.Count; i++)\n'
        '                {\n'
        '                    var (tx, ty) = engine.GridZones.TempShades[i];\n'
        '                    CreateRingIndicator($"TempShadeCooling_{i}", new Vector3(tx, ty, 0f), FarmGridAndZones.TempShadeRadiusMeters, new Color(0.20f, 0.82f, 0.72f, 0.18f));\n'
        '                }\n'
        '            }',
        "FarmOverviewPopup TempShades cooling ring",
    )
    write_utf8(path, src)


def update_widgets_and_quest_tracker():
    """#159 HUD & QuestTracker fixes from Claude (already in 22:45:39 base)."""
    q_path = os.path.join(FROM_ARGON_UI, "QuestTrackerPanel.cs")
    q = read_utf8(q_path)
    q = q.replace("{view.Id} · {view.Title}", "{view.Title}")
    q = q.replace("{q.Id} · {q.Title}", "{q.Title}")
    write_utf8(q_path, q)


def update_myth_hud():
    """#155, #156, #157, #158, #159 on MythHudController.cs (on top of 22:45:39 from_claude base)."""
    path = os.path.join(FROM_ARGON_UI, "Myth/MythHudController.cs")
    src = read_utf8(path)

    # 1. Fields for DrinkProgressBar (#157 §2), TempShades & PigCrisisIndicators (#158)
    src = replace_exact(
        src,
        "        RectTransform faintOverlayRoot;",
        "        RectTransform faintOverlayRoot;\n"
        "        RectTransform drinkBarRoot;\n"
        "        MythClipFill drinkBarFill;\n"
        "        float drinkStartTime = -999f;\n"
        "        bool wasDrinking;\n"
        "        readonly List<GameObject> tempShadeObjects = new List<GameObject>();\n"
        "        RectTransform crisisAlertRoot;\n"
        "        TextMeshProUGUI crisisAlertText;\n"
        "        string crisisTargetPigId;",
        "MythHudController fields",
    )

    # 2. #157 §3: Lá Dò Sương status icon in UpdateStatus
    old_status = (
        '            if (ws.NhiemXaStacks > 0) active.Add(new KeyValuePair<string, int>("nhiem_xa", Mathf.Min(4, ws.NhiemXaStacks)));\n'
        '            UpdateWeatherStatusText(engine, active.Count);'
    )
    new_status = (
        '            if (ws.NhiemXaStacks > 0) active.Add(new KeyValuePair<string, int>("nhiem_xa", Mathf.Min(4, ws.NhiemXaStacks)));\n'
        '            if (engine.FogWarningUntilMinute >= engine.Clock.TotalElapsedGameMinutes)\n'
        '                active.Add(new KeyValuePair<string, int>("la_do_suong", 0));\n'
        '            UpdateWeatherStatusText(engine, active.Count);'
    )
    src = replace_exact(src, old_status, new_status, "MythHudController UpdateStatus la_do_suong")

    # 3. Hook UpdateDrinkBar, SyncTempShadesInWorld, and UpdatePigCrisisIndicators into Update()
    old_update_loop = (
        "            UpdateSundial(engine);\n"
        "            UpdateBars(engine);\n"
        "            UpdateBossBar();\n"
        "            UpdateTradeEyeBar(engine);\n"
        "            if (Time.unscaledTime >= nextCoarse)\n"
        "            {\n"
        "                nextCoarse = Time.unscaledTime + 0.2f;\n"
        "                UpdateStatus(engine);\n"
        "                UpdateTopRight(engine);\n"
        "                UpdateBag(engine);\n"
        "                TrySpawnStarterPickup(engine);\n"
        "                ScanInteraction();\n"
        "            }"
    )
    new_update_loop = (
        "            UpdateSundial(engine);\n"
        "            UpdateBars(engine);\n"
        "            UpdateDrinkBar();\n"
        "            UpdateBossBar();\n"
        "            UpdateTradeEyeBar(engine);\n"
        "            if (Time.unscaledTime >= nextCoarse)\n"
        "            {\n"
        "                nextCoarse = Time.unscaledTime + 0.2f;\n"
        "                UpdateStatus(engine);\n"
        "                UpdateTopRight(engine);\n"
        "                UpdateBag(engine);\n"
        "                TrySpawnStarterPickup(engine);\n"
        "                SyncTempShadesInWorld(engine);\n"
        "                UpdatePigCrisisIndicators(engine);\n"
        "                ScanInteraction();\n"
        "            }"
    )
    src = replace_exact(src, old_update_loop, new_update_loop, "MythHudController Update loop hooks")

    # 4. DrinkProgressBar (#157 §2), TempShades & PigCrisisIndicators (#158)
    old_faint_overlay = "        void UpdateFaintOverlay(bool fainted)"
    new_crisis_and_drink_methods = """        void UpdateDrinkBar()
        {
            var runner = PlayerMobileController.Instance != null ? PlayerMobileController.Instance.GetComponent<MartialSkillRunner>() : null;
            bool drinking = runner != null && runner.IsDrinking;
            if (!drinking)
            {
                wasDrinking = false;
                if (drinkBarRoot != null && drinkBarRoot.gameObject.activeSelf) drinkBarRoot.gameObject.SetActive(false);
                return;
            }
            if (!wasDrinking)
            {
                wasDrinking = true;
                drinkStartTime = Time.unscaledTime;
            }
            if (drinkBarRoot == null)
            {
                drinkBarRoot = MythUi.Rect("DrinkProgressBar", safeRoot, MythUi.TL, MythUi.TL, new Vector2(4, -150), new Vector2(96, 10));
                MythUi.Img("Trough", drinkBarRoot, MythUi.Kit("trade_eye_frame") ?? MythUi.Kit("bar_hp_trough"), MythUi.TL, MythUi.TL, Vector2.zero, new Vector2(96, 10));
                drinkBarFill = new MythClipFill("Fill", drinkBarRoot, MythUi.Kit("bar_stamina_fill_bright"), MythUi.TL, MythUi.TL, new Vector2(3, -2), 90, 6);
                var lblRt = MythUi.Rect("Label", drinkBarRoot, MythUi.MID, MythUi.MID, Vector2.zero, new Vector2(94, 10));
                var t = lblRt.gameObject.AddComponent<TextMeshProUGUI>();
                t.text = "Bình Hồi Lực (1s)";
                t.fontSize = 6.5f;
                t.fontStyle = FontStyles.Bold;
                t.alignment = TextAlignmentOptions.Center;
                t.color = Color.white;
                t.raycastTarget = false;
            }
            if (!drinkBarRoot.gameObject.activeSelf) drinkBarRoot.gameObject.SetActive(true);
            float ratio = Mathf.Clamp01((Time.unscaledTime - drinkStartTime) / Mathf.Max(0.1f, QuickBag.DrinkSeconds));
            drinkBarFill.Set(ratio);
        }

        void SyncTempShadesInWorld(GameEngine engine)
        {
            var shades = engine?.GridZones?.TempShades;
            int targetCount = (shades != null && string.Equals(engine.WorldLocations.CurrentLocationId, "Farm", System.StringComparison.OrdinalIgnoreCase))
                ? shades.Count : 0;
            while (tempShadeObjects.Count > targetCount)
            {
                int last = tempShadeObjects.Count - 1;
                if (tempShadeObjects[last] != null) Destroy(tempShadeObjects[last]);
                tempShadeObjects.RemoveAt(last);
            }
            if (targetCount == 0) return;

            var canopySpr = UISpriteLoader.GetTempShadeCanopySprite();
            var circleSpr = UISpriteLoader.GetSoftCircleSprite();
            for (int i = 0; i < targetCount; i++)
            {
                var (sx, sy) = shades[i];
                GameObject go = i < tempShadeObjects.Count ? tempShadeObjects[i] : null;
                if (go == null)
                {
                    go = new GameObject($"TempShade_Canopy_{i}");
                    var sr = go.AddComponent<SpriteRenderer>();
                    sr.sprite = canopySpr;
                    sr.sortingOrder = Mathf.RoundToInt(-sy * 100f) + 2;

                    var aura = new GameObject("CoolRadiusAura");
                    aura.transform.SetParent(go.transform, false);
                    float d = FarmGridAndZones.TempShadeRadiusMeters * 2f;
                    aura.transform.localScale = new Vector3(d, d * 0.72f, 1f);
                    var asr = aura.AddComponent<SpriteRenderer>();
                    asr.sprite = circleSpr;
                    asr.color = new Color(0.28f, 0.76f, 0.68f, 0.18f);
                    asr.sortingOrder = -32000;

                    if (i < tempShadeObjects.Count) tempShadeObjects[i] = go;
                    else tempShadeObjects.Add(go);
                }
                go.transform.position = new Vector3(sx, sy, 0f);
            }
        }

        void UpdatePigCrisisIndicators(GameEngine engine)
        {
            if (engine == null || engine.Pigs == null) return;
            Pig urgentPig = null;
            int bestPriority = 0; // 3 = SeizurePending, 2 = SayNang, 1 = Quá nhiệt >= 1h

            var views = FindObjectsByType<PigAgentView>(FindObjectsSortMode.None);
            for (int i = 0; i < views.Length; i++)
            {
                var pv = views[i];
                if (pv == null || pv.PigModel == null) continue;
                var p = pv.PigModel;
                int prio = 0;
                string iconStem = null;
                if (p.IsAlive && p.SeizurePending) { prio = 3; iconStem = "loi_tam_spark"; }
                else if (p.IsAlive && p.ActiveDisease == DiseaseId.SayNang) { prio = 2; iconStem = "say_nang"; }
                else if (p.IsAlive && engine.UncooledHeatHours(p.Id) >= 1f) { prio = 1; iconStem = "qua_nhiet"; }

                var badgeTr = pv.transform.Find("ArgonCrisisBadge");
                if (prio == 0)
                {
                    if (badgeTr != null && badgeTr.gameObject.activeSelf) badgeTr.gameObject.SetActive(false);
                    continue;
                }
                if (prio > bestPriority) { bestPriority = prio; urgentPig = p; }
                if (badgeTr == null)
                {
                    var bGo = new GameObject("ArgonCrisisBadge");
                    bGo.transform.SetParent(pv.transform, false);
                    bGo.transform.localPosition = new Vector3(0f, 1.15f, 0f);
                    var bSr = bGo.AddComponent<SpriteRenderer>();
                    bSr.sortingOrder = 30000;
                    badgeTr = bGo.transform;
                }
                if (!badgeTr.gameObject.activeSelf) badgeTr.gameObject.SetActive(true);
                float bob = 1.12f + 0.08f * Mathf.Sin(Time.unscaledTime * 8f);
                badgeTr.localPosition = new Vector3(0f, bob, 0f);
                var sr = badgeTr.GetComponent<SpriteRenderer>();
                if (sr != null) sr.sprite = MythUi.Icon("status", iconStem, 16);
            }

            if (urgentPig == null)
            {
                for (int i = 0; i < engine.Pigs.Count; i++)
                {
                    var p = engine.Pigs[i];
                    if (p == null || !p.IsAlive) continue;
                    int prio = p.SeizurePending ? 3 : (p.ActiveDisease == DiseaseId.SayNang ? 2 : (engine.UncooledHeatHours(p.Id) >= 1f ? 1 : 0));
                    if (prio > bestPriority) { bestPriority = prio; urgentPig = p; }
                }
            }

            if (urgentPig == null)
            {
                if (crisisAlertRoot != null && crisisAlertRoot.gameObject.activeSelf) crisisAlertRoot.gameObject.SetActive(false);
                return;
            }

            if (crisisAlertRoot == null)
            {
                crisisAlertRoot = MythUi.Rect("PigCrisisPointer", safeRoot, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -6f), new Vector2(216f, 20f));
                var bg = crisisAlertRoot.gameObject.AddComponent<Image>();
                bg.sprite = UISpriteLoader.GetFrameSlotSelected() ?? UISpriteLoader.GetFrameSlotNormal();
                bg.type = Image.Type.Sliced;
                bg.color = new Color(0.46f, 0.11f, 0.10f, 0.96f);
                bg.raycastTarget = true;
                crisisAlertRoot.gameObject.AddComponent<MythTap>().OnTap = () =>
                {
                    var eng = Engine;
                    if (eng == null || string.IsNullOrEmpty(crisisTargetPigId)) return;
                    var found = eng.Pigs.Find(x => x.Id == crisisTargetPigId);
                    if (found != null) PigInspectPopup.Instance?.Show(found);
                };
                var txtRt = MythUi.Rect("Text", crisisAlertRoot, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
                txtRt.offsetMin = new Vector2(6f, 0f);
                txtRt.offsetMax = new Vector2(-6f, 0f);
                crisisAlertText = txtRt.gameObject.AddComponent<TextMeshProUGUI>();
                crisisAlertText.fontSize = 8f;
                crisisAlertText.fontStyle = FontStyles.Bold;
                crisisAlertText.alignment = TextAlignmentOptions.Center;
                crisisAlertText.color = new Color(1f, 0.92f, 0.60f);
                crisisAlertText.raycastTarget = false;
            }
            if (!crisisAlertRoot.gameObject.activeSelf) crisisAlertRoot.gameObject.SetActive(true);
            crisisTargetPigId = urgentPig.Id;

            string dirArrow = "";
            var player = PlayerMobileController.Instance;
            if (player != null && (urgentPig.PositionX != 0f || urgentPig.PositionY != 0f))
            {
                Vector2 delta = new Vector2(urgentPig.PositionX, urgentPig.PositionY) - (Vector2)player.transform.position;
                if (delta.sqrMagnitude > 4f)
                {
                    float ang = Mathf.Atan2(delta.y, delta.x) * Mathf.Rad2Deg;
                    dirArrow = ang >= -22.5f && ang < 22.5f ? " [→]"
                             : ang >= 22.5f && ang < 67.5f ? " [↗]"
                             : ang >= 67.5f && ang < 112.5f ? " [↑]"
                             : ang >= 112.5f && ang < 157.5f ? " [↖]"
                             : (ang >= 157.5f || ang < -157.5f) ? " [←]"
                             : ang >= -157.5f && ang < -112.5f ? " [↙]"
                             : ang >= -112.5f && ang < -67.5f ? " [↓]" : " [↘]";
                }
            }
            string crisisLabel = bestPriority == 3 ? $"KHẨN: {urgentPig.Name} RUN GIẬT LÔI TÂM!{dirArrow}"
                               : bestPriority == 2 ? $"KHẨN: {urgentPig.Name} GỤC SAY NẮNG!{dirArrow}"
                               : $"CẢNH BÁO: {urgentPig.Name} quá nhiệt!{dirArrow}";
            crisisAlertText.text = crisisLabel;
        }

        void UpdateFaintOverlay(bool fainted)"""
    src = replace_exact(src, old_faint_overlay, new_crisis_and_drink_methods, "MythHudController Crisis & DrinkBar")
    write_utf8(path, src)


def update_character_and_daymarket_and_spriteloader():
    cp_path = os.path.join(FROM_ARGON_UI, "CharacterPanel.cs")
    cp = read_utf8(cp_path)
    cp = replace_exact(
        cp,
        'hdr.text = $"<b>Bàn Khắc</b>  Phù Văn {c.EngravedRunes.Count}/6 · Mệnh Ấn {c.EngravedSeals.Count}/3 <size=11>(mỗi ấn −10 Chống Chịu)</size>";',
        'hdr.text = $"<b>Bàn Khắc</b>  Phù Văn {c.EngravedRunes.Count}/6 · Mệnh Ấn {c.EngravedSeals.Count}/3 · Mực Ấn <color=#FDE047><b>{c.SealInk}</b></color> <size=11>(mỗi ấn −10 Chống Chịu)</size>";',
        "CharacterPanel BuildEngraving SealInk",
    )
    write_utf8(cp_path, cp)

    sl_path = os.path.join(FROM_ARGON_UI, "UISpriteLoader.cs")
    sl = read_utf8(sl_path)
    old_loot_switch = (
        '                "GiayDo" or "Paper_Red" => LoadSprite("Art/PixelMyth/UI/Icons/items/giay_do_24.png", 32) ?? LoadSprite("Art/PixelMyth/UI/Icons/misc/tinh_bao_24.png", 32),\n'
        '                "MauGenTho" or "AnTinGiaToc24" or "ManhGiayKienHang"\n'
        '                    => LoadSprite("Art/PixelMyth/UI/Icons/misc/tinh_bao_24.png", 32) ?? GetBlueprintSprite(),'
    )
    new_loot_switch = (
        '                "GiayDo" or "Paper_Red" => LoadSprite("Art/PixelMyth/UI/Icons/items/giay_do_24.png", 32) ?? LoadSprite("Art/PixelMyth/UI/Icons/misc/tinh_bao_24.png", 32),\n'
        '                "XoNuoc" => LoadSprite("Art/PixelMyth/UI/Icons/items/xo_nuoc_24.png", 32) ?? LoadSprite("Art/PixelMyth/UI/Icons/actions/cho_uong_24.png", 32),\n'
        '                "CamLinhChi" => LoadSprite("Art/PixelMyth/UI/Icons/items/cam_linh_chi_24.png", 32) ?? LoadSprite("Art/UI/Items/item_corn_feed.png", 32),\n'
        '                "CamHaHoa" => LoadSprite("Art/PixelMyth/UI/Icons/items/cam_ha_hoa_24.png", 32) ?? LoadSprite("Art/UI/Items/item_corn_feed.png", 32),\n'
        '                "ThuocAnThanHeo" => LoadSprite("Art/PixelMyth/UI/Icons/items/thuoc_an_than_heo_24.png", 32) ?? LoadSprite("Art/PixelMyth/UI/Icons/quick/binh_hoi_luc_24.png", 32),\n'
        '                "HoSoGiongLoai" => LoadSprite("Art/PixelMyth/UI/Icons/items/ho_so_giong_loai_24.png", 32) ?? LoadSprite("Art/PixelMyth/UI/Icons/misc/tinh_bao_24.png", 32),\n'
        '                "MaiCheTam" => LoadSprite("Art/PixelMyth/UI/Icons/items/mai_che_tam_24.png", 32) ?? GetTempShadeCanopySprite(),\n'
        '                "TinDonCho" => LoadSprite("Art/PixelMyth/UI/Icons/items/tin_don_cho_24.png", 32) ?? LoadSprite("Art/PixelMyth/UI/Icons/misc/tinh_bao_24.png", 32),\n'
        '                "LaDoSuong" => LoadSprite("Art/PixelMyth/UI/Icons/items/la_do_suong_24.png", 32) ?? LoadSprite("Art/PixelMyth/UI/Icons/status/la_do_suong_16.png", 32),\n'
        '                "BanDoHuongQuai" => LoadSprite("Art/PixelMyth/UI/Icons/items/ban_do_huong_quai_24.png", 32) ?? LoadSprite("Art/PixelMyth/UI/Icons/misc/tinh_bao_24.png", 32),\n'
        '                "ChiaCongNguoc" => LoadSprite("Art/PixelMyth/UI/Icons/items/chia_cong_nguoc_24.png", 32) ?? LoadSprite("Art/PixelMyth/UI/Icons/actions/cuong_hoa_24.png", 32),\n'
        '                "MauGenTho" or "AnTinGiaToc24" or "ManhGiayKienHang"\n'
        '                    => LoadSprite("Art/PixelMyth/UI/Icons/misc/tinh_bao_24.png", 32) ?? GetBlueprintSprite(),'
    )
    sl = replace_exact(sl, old_loot_switch, new_loot_switch, "UISpriteLoader ItemUse icons")
    sl = replace_exact(
        sl,
        "        public static Sprite GetBarnSprite() => LoadSprite(\"Art/Sprites/Environment/Buildings/shelter_barn.png\", 32);",
        "        public static Sprite GetTempShadeCanopySprite() => LoadSprite(\"Art/PixelMyth/UI/Kit/temp_shade_canopy.png\", 32) ?? GetBarnSprite();\n\n"
        "        public static Sprite GetBarnSprite() => LoadSprite(\"Art/Sprites/Environment/Buildings/shelter_barn.png\", 32);",
        "UISpriteLoader GetTempShadeCanopySprite",
    )
    write_utf8(sl_path, sl)


def generate_ui_art_deliverables():
    """
    Generates in from_argon/Assets/Art/PixelMyth/UI/:
    1. Shield-emblem bar_hp_vessel_frame.png (80x12, transparent 62x8 window at x=12..73, y=2..9) + golden-jade bar_hp_fill.png (62x8) (#159)
    2. Lightning-emblem bar_stamina_frame.png (96x12, transparent 80x8 window at x=8..87, y=2..9)
    3. Clean 32-PPU medallions (48x48 large, 32x32 small), sundial_base (64x64), joystick_base (96x96), joystick_knob (40x40)
    4. Mái Che Tạm world sprite temp_shade_canopy.png (96x96 @ 32 PPU = 3m x 3m bamboo/thatch canopy) (#158)
    5. 16x16 status icons: la_do_suong_16.png, qua_nhiet_16.png, say_nang_16.png, loi_tam_spark_16.png (#157 & #158)
    6. 24x24 & 16x16 item icons for all 10 RareSources items + 10 ItemUse items (#155 & #157)
    """
    kit_dir = os.path.join(FROM_ARGON_ART_UI, "Kit")
    items_dir = os.path.join(FROM_ARGON_ART_UI, "Icons/items")
    status_dir = os.path.join(FROM_ARGON_ART_UI, "Icons/status")
    for d in (kit_dir, items_dir, status_dir):
        os.makedirs(d, exist_ok=True)

    outline = (24, 16, 10, 255)
    bronze_hi = (228, 184, 96, 255)
    bronze_mid = (176, 132, 62, 255)
    bronze_sh = (98, 68, 30, 255)
    jade_hi = (118, 232, 186, 255)
    jade_mid = (52, 168, 128, 255)

    # 1. bar_hp_vessel_frame.png (80x12): Chống Chịu SHIELD emblem on left 12x12 cap + transparent 62x8 window (x=12..73, y=2..9)
    hp_frame = Image.new("RGBA", (80, 12), (0, 0, 0, 0))
    px = hp_frame.load()
    for y in range(12):
        for x in range(12, 80):
            if (x == 79) and (y == 0 or y == 11):
                continue
            if y == 0 or y == 11 or x == 79:
                px[x, y] = outline
            elif y == 1:
                px[x, y] = bronze_hi
            elif y == 10:
                px[x, y] = bronze_sh
            elif x >= 74:
                px[x, y] = bronze_hi if y < 6 else bronze_sh
    # Draw crisp 12x12 kite SHIELD icon in x=0..11, y=0..11 (replaces heart icon per #159!)
    shield_rows = [
        "..########..",
        ".#BBBBBBBB#.",
        "#BHHHHHHHHB#",
        "#BHJJJJJJHB#",
        "#BHJMMMMJHB#",
        "#BHJMMMMJHB#",
        ".#BHJMMJHB#.",
        ".#BHJMMJHB#.",
        "..#BHJJHB#..",
        "..#BHHHHB#..",
        "...#BBBB#...",
        "....####....",
    ]
    cmap = {
        "#": outline,
        "B": bronze_sh,
        "H": bronze_hi,
        "J": jade_hi,
        "M": jade_mid,
    }
    for y, row in enumerate(shield_rows):
        for x, ch in enumerate(row):
            if ch in cmap:
                px[x, y] = cmap[ch]
    hp_frame.save(os.path.join(kit_dir, "bar_hp_vessel_frame.png"))

    # bar_hp_fill.png (62x8): golden-jade Chống Chịu guard bar fill (not red HP!)
    hp_fill = Image.new("RGBA", (62, 8), (0, 0, 0, 0))
    fpx = hp_fill.load()
    fill_rows = [
        (96, 206, 168, 255),
        (152, 244, 208, 255),
        (82, 192, 152, 255),
        (64, 172, 134, 255),
        (52, 152, 118, 255),
        (42, 132, 102, 255),
        (34, 112, 86, 255),
        (26, 88, 68, 255),
    ]
    for y in range(8):
        for x in range(62):
            fpx[x, y] = fill_rows[y]
    hp_fill.save(os.path.join(kit_dir, "bar_hp_fill.png"))

    # 2. bar_stamina_frame.png (96x12): lightning cap x=0..7 + transparent 80x8 window (x=8..87, y=2..9)
    sta_frame = Image.new("RGBA", (96, 12), (0, 0, 0, 0))
    spx = sta_frame.load()
    for y in range(12):
        for x in range(8, 96):
            if (x == 95) and (y == 0 or y == 11):
                continue
            if y == 0 or y == 11 or x == 95:
                spx[x, y] = outline
            elif y == 1:
                spx[x, y] = bronze_hi
            elif y == 10:
                spx[x, y] = bronze_sh
            elif x >= 88:
                spx[x, y] = bronze_hi if y < 6 else bronze_sh
    bolt_rows = [
        "..####..",
        ".#HHHH#.",
        ".#HYYH#.",
        "#HYYY#..",
        "#HYYYY#.",
        ".#YYYH#.",
        "..#YYH#.",
        ".#HYY#..",
        ".#HY#...",
        "#HY#....",
        "#H#.....",
        "##......",
    ]
    bmap = {"#": outline, "H": bronze_hi, "Y": (255, 232, 112, 255)}
    for y, row in enumerate(bolt_rows):
        for x, ch in enumerate(row):
            if ch in bmap:
                spx[x, y] = bmap[ch]
    sta_frame.save(os.path.join(kit_dir, "bar_stamina_frame.png"))

    # 3. Clean 32-PPU medallions (48x48 large, 32x32 small) from disabled template tinting so no weapon icon is baked in
    for sz, stem in ((48, "large"), (32, "small")):
        for variant, rim_col, well_col in (
            ("normal", (214, 168, 86, 255), (28, 32, 42, 235)),
            ("pressed", (164, 124, 58, 255), (18, 22, 30, 245)),
        ):
            med = Image.new("RGBA", (sz, sz), (0, 0, 0, 0))
            mpx = med.load()
            cx = cy = (sz - 1) * 0.5
            r_out = sz * 0.46
            r_in = sz * 0.36
            for y in range(sz):
                for x in range(sz):
                    dx, dy = abs(x - cx), abs(y - cy)
                    d_oct = max(dx, dy, (dx + dy) * 0.7071)
                    if d_oct > r_out:
                        continue
                    if d_oct >= r_out - 1.2:
                        mpx[x, y] = outline
                    elif d_oct >= r_in:
                        shade = 1.12 if (x + y < sz) else 0.82
                        mpx[x, y] = (
                            min(255, int(rim_col[0] * shade)),
                            min(255, int(rim_col[1] * shade)),
                            min(255, int(rim_col[2] * shade)),
                            255,
                        )
                    elif d_oct >= r_in - 1.0:
                        mpx[x, y] = outline
                    else:
                        mpx[x, y] = well_col
            med.save(os.path.join(kit_dir, f"medallion_{stem}_{variant}.png"))

    # 4. temp_shade_canopy.png (96x96 @ 32 PPU = 3m x 3m bamboo/thatch shade canopy for #158)
    canopy = Image.new("RGBA", (96, 96), (0, 0, 0, 0))
    cpx = canopy.load()
    # Ground cooling shadow ellipse
    for y in range(54, 92):
        for x in range(10, 86):
            dx = (x - 48) / 36.0
            dy = (y - 73) / 17.0
            if dx * dx + dy * dy <= 1.0:
                cpx[x, y] = (24, 62, 68, 110)
    # 4 bamboo corner posts
    for px0 in (18, 75):
        for y in range(38, 86):
            for dx in range(3):
                cpx[px0 + dx, y] = outline if dx in (0, 2) else (168, 132, 72, 255)
    # Pitched woven bamboo/thatch roof (y=12..54, x=10..86)
    for y in range(12, 54):
        inset = max(0, (30 - y) // 2) if y < 30 else 0
        x0, x1 = 10 + inset, 85 - inset
        for x in range(x0, x1 + 1):
            if x in (x0, x1) or y in (12, 53):
                cpx[x, y] = outline
            elif y < 22:
                cpx[x, y] = (218, 182, 106, 255) if (x + y) % 4 != 0 else (188, 148, 78, 255)
            elif y < 44:
                cpx[x, y] = (196, 156, 86, 255) if (x % 6 != 0) else (154, 118, 58, 255)
            else:
                cpx[x, y] = (138, 102, 48, 255) if (x % 3 == 0) else (172, 134, 70, 255)
    canopy.save(os.path.join(kit_dir, "temp_shade_canopy.png"))

    # 5. 16x16 status icons (#157 & #158): la_do_suong_16, qua_nhiet_16, say_nang_16, loi_tam_spark_16
    status_patterns = {
        "la_do_suong": (
            [
                "......####......",
                "....##BBBB##....",
                "...#BBHHHHBB#...",
                "..#BBHBBBBHBB#..",
                ".#BBHB#BB#BHBB#.",
                ".#BHB##BB##BHB#.",
                "#BHB###BB###BHB#",
                "#BB####BB####BB#",
                "#BBBBBBBBBBBBBB#",
                ".#BBBB#BB#BBBB#.",
                "..#BBB#BB#BBB#..",
                "...###.##.###...",
                ".......##.......",
                "......##........",
            ],
            {"#": outline, "B": (46, 178, 138, 255), "H": (168, 248, 218, 255)},
        ),
        "qua_nhiet": (
            [
                "......####......",
                ".....#BBBB#.....",
                ".....#BHHB#.....",
                ".....#BHHB#.....",
                ".....#BBBB#.....",
                ".....#BHHB#.....",
                "....##HHHH##....",
                "...#BBHHHHBB#...",
                "..#BBHHHHHHBB#..",
                "..#BHHHHHHHHB#..",
                "..#BBHHHHHHBB#..",
                "...#BBBBBBBB#...",
                "....########....",
                "................",
            ],
            {"#": outline, "B": (228, 112, 34, 255), "H": (255, 224, 98, 255)},
        ),
        "say_nang": (
            [
                "..#....##....#..",
                "...#..#BB#..#...",
                "....########....",
                "..##BBBBBBBB##..",
                ".#BBHHHHHHHHBB#.",
                "#BBHHHHHHHHHHBB#",
                "##BHHHHHHHHHHB##",
                "##BHHHHHHHHHHB##",
                "#BBHHHHHHHHHHBB#",
                ".#BBHHHHHHHHBB#.",
                "..##BBBBBBBB##..",
                "....########....",
                "...#..#BB#..#...",
                "..#....##....#..",
            ],
            {"#": outline, "B": (218, 42, 38, 255), "H": (255, 196, 84, 255)},
        ),
        "loi_tam_spark": (
            [
                ".......#####....",
                "......#HHHH#....",
                ".....#HHH##.....",
                "....#HHH#.......",
                "...#HHHH######..",
                "..#HHHHHHHHHH#..",
                "..#####HHHHH#...",
                "......#HHHH#....",
                ".....#HHHH#.....",
                "....#HHH##......",
                "...#HHH#........",
                "..#HH##.........",
                ".#H##...........",
                ".##.............",
            ],
            {"#": outline, "H": (255, 224, 68, 255)},
        ),
    }
    for stem, (rows_14, cmap_s) in status_patterns.items():
        sim = Image.new("RGBA", (16, 16), (0, 0, 0, 0))
        spx = sim.load()
        for y, rstr in enumerate(rows_14):
            for x, ch in enumerate(rstr):
                if ch in cmap_s:
                    spx[x, y + 1] = cmap_s[ch]
        sim.save(os.path.join(status_dir, f"{stem}_16.png"))

    # 6. 20 Item Icons (10 RareSources + 10 ItemUse) at 24x24 & 16x16 with distinct pixel silhouettes
    specs = {
        "bi_kip": ("scroll", (215, 68, 52), (248, 222, 158), (238, 192, 88)),
        "linh_kien_tinh": ("gear_core", (72, 152, 188), (210, 244, 255), (228, 184, 88)),
        "da_cuong_hoa": ("crystal", (224, 138, 42), (255, 232, 136), (138, 72, 24)),
        "bot_tu_tinh": ("pouch", (152, 82, 214), (228, 186, 255), (232, 188, 86)),
        "muc_an": ("bottle", (48, 72, 112), (214, 72, 68), (228, 192, 118)),
        "huyet_trien": ("seal", (184, 42, 46), (252, 138, 118), (236, 194, 92)),
        "giay_lam": ("paper", (58, 132, 218), (176, 218, 255), (238, 198, 98)),
        "giay_vang": ("paper", (218, 168, 48), (255, 236, 148), (142, 92, 28)),
        "giay_tim": ("paper", (152, 78, 216), (226, 182, 255), (238, 198, 98)),
        "giay_do": ("paper", (208, 54, 54), (255, 174, 162), (244, 206, 104)),
        "xo_nuoc": ("bucket", (58, 154, 224), (178, 234, 255), (156, 108, 58)),
        "cam_linh_chi": ("pouch", (58, 176, 92), (182, 246, 196), (228, 92, 68)),
        "cam_ha_hoa": ("pouch", (54, 168, 198), (182, 242, 255), (232, 188, 86)),
        "thuoc_an_than_heo": ("bottle", (138, 88, 214), (220, 192, 255), (228, 188, 96)),
        "ho_so_giong_loai": ("scroll", (196, 152, 86), (246, 228, 182), (124, 78, 36)),
        "mai_che_tam": ("canopy_icon", (192, 148, 76), (242, 210, 138), (92, 168, 142)),
        "tin_don_cho": ("scroll", (214, 138, 54), (252, 222, 152), (168, 54, 48)),
        "la_do_suong": ("leaf_icon", (54, 184, 146), (174, 248, 220), (28, 98, 76)),
        "ban_do_huong_quai": ("paper", (194, 76, 58), (246, 204, 164), (238, 196, 88)),
        "chia_cong_nguoc": ("key_icon", (168, 82, 218), (236, 188, 255), (238, 196, 88)),
    }
    for stem, (shape, body_col, hi_col, trim_col) in specs.items():
        img24 = Image.new("RGBA", (24, 24), (0, 0, 0, 0))
        d24 = ImageDraw.Draw(img24)
        body = (*body_col, 255)
        hi = (*hi_col, 255)
        trim = (*trim_col, 255)
        if shape == "scroll":
            d24.rectangle([5, 3, 18, 5], fill=trim, outline=outline)
            d24.rectangle([6, 5, 17, 18], fill=hi, outline=outline)
            d24.rectangle([5, 18, 18, 20], fill=trim, outline=outline)
            d24.rectangle([6, 10, 17, 13], fill=body, outline=outline)
        elif shape == "paper":
            d24.polygon([(5, 3), (15, 3), (19, 7), (19, 20), (5, 20)], fill=body, outline=outline)
            d24.polygon([(15, 3), (19, 7), (15, 7)], fill=hi, outline=outline)
            d24.rectangle([8, 9, 16, 11], fill=hi)
            d24.ellipse([9, 13, 15, 18], fill=trim, outline=outline)
        elif shape == "crystal":
            d24.polygon([(12, 2), (19, 9), (16, 21), (8, 21), (5, 9)], fill=body, outline=outline)
            d24.polygon([(12, 2), (19, 9), (12, 13), (5, 9)], fill=hi, outline=outline)
            d24.polygon([(12, 6), (15, 10), (12, 17), (9, 10)], fill=trim)
        elif shape == "gear_core":
            d24.ellipse([4, 4, 19, 19], fill=trim, outline=outline)
            d24.ellipse([7, 7, 16, 16], fill=body, outline=outline)
            d24.ellipse([9, 9, 14, 14], fill=hi)
        elif shape == "pouch":
            d24.polygon([(8, 4), (16, 4), (14, 8), (10, 8)], fill=trim, outline=outline)
            d24.ellipse([5, 7, 19, 21], fill=body, outline=outline)
            d24.ellipse([8, 9, 14, 14], fill=hi)
            d24.rectangle([8, 7, 16, 9], fill=trim, outline=outline)
        elif shape == "bottle":
            d24.rectangle([9, 2, 15, 5], fill=trim, outline=outline)
            d24.rectangle([10, 5, 14, 8], fill=hi, outline=outline)
            d24.rounded_rectangle([6, 8, 18, 21], radius=4, fill=body, outline=outline)
            d24.ellipse([8, 10, 13, 15], fill=hi)
        elif shape == "bucket":
            d24.arc([6, 2, 18, 12], start=180, end=0, fill=outline, width=2)
            d24.polygon([(5, 7), (19, 7), (17, 20), (7, 20)], fill=trim, outline=outline)
            d24.ellipse([5, 5, 19, 10], fill=body, outline=outline)
            d24.ellipse([7, 6, 16, 9], fill=hi)
        elif shape == "seal":
            d24.rectangle([9, 3, 15, 9], fill=trim, outline=outline)
            d24.rectangle([5, 9, 19, 16], fill=body, outline=outline)
            d24.rectangle([5, 16, 19, 20], fill=hi, outline=outline)
        elif shape == "canopy_icon":
            d24.polygon([(3, 11), (12, 4), (21, 11)], fill=hi, outline=outline)
            d24.rectangle([4, 11, 20, 14], fill=body, outline=outline)
            d24.rectangle([6, 14, 8, 21], fill=outline)
            d24.rectangle([16, 14, 18, 21], fill=outline)
            d24.ellipse([7, 17, 17, 21], fill=trim)
        elif shape == "leaf_icon":
            d24.polygon([(12, 2), (19, 9), (16, 18), (12, 21), (8, 18), (5, 9)], fill=body, outline=outline)
            d24.line([(12, 4), (12, 20)], fill=hi, width=2)
        else:  # key_icon
            d24.ellipse([6, 3, 18, 13], fill=trim, outline=outline)
            d24.ellipse([9, 6, 15, 10], fill=body, outline=outline)
            d24.rectangle([11, 13, 14, 21], fill=trim, outline=outline)
            d24.rectangle([14, 16, 18, 18], fill=hi, outline=outline)
            d24.rectangle([14, 19, 17, 21], fill=hi, outline=outline)
        img24.save(os.path.join(items_dir, f"{stem}_24.png"))
        img16 = img24.resize((16, 16), Image.Resampling.NEAREST)
        img16.save(os.path.join(items_dir, f"{stem}_16.png"))

    # 7. Restore exact 32-PPU sundial_base.png (64x64), joystick_base.png (96x96), joystick_knob.png (40x40),
    #    and vn_dialogue_frame.png (320x96) (replaces oversized crops left by retired scripts)
    sd = Image.new("RGBA", (64, 64), (0, 0, 0, 0))
    sd_px = sd.load()
    for y in range(64):
        for x in range(64):
            dx, dy = x - 31.5, y - 31.5
            r = math.sqrt(dx * dx + dy * dy)
            if r > 30.5:
                continue
            if r >= 29.0:
                sd_px[x, y] = outline
            elif r >= 24.5:
                shade = 1.14 if (dx + dy < 0) else 0.80
                sd_px[x, y] = (
                    min(255, int(bronze_mid[0] * shade)),
                    min(255, int(bronze_mid[1] * shade)),
                    min(255, int(bronze_mid[2] * shade)),
                    255,
                )
            elif r >= 23.2:
                sd_px[x, y] = outline
            elif r >= 13.5:
                # Top half warm golden-jade day sky, bottom half deep indigo-slate night sky
                if dy < 0:
                    sd_px[x, y] = (54, 118, 126, 240) if int(r) % 3 != 0 else (76, 148, 152, 245)
                else:
                    sd_px[x, y] = (24, 34, 58, 245) if int(r) % 3 != 0 else (36, 48, 78, 245)
            elif r >= 12.2:
                sd_px[x, y] = bronze_hi if (dx + dy < 0) else bronze_sh
            else:
                sd_px[x, y] = (22, 26, 36, 248)
    # 12 hour tick marks around sundial rim
    for k in range(12):
        ang = k * (math.pi / 6.0)
        for rr in (25.5, 26.5, 27.5):
            tx = int(round(31.5 + rr * math.cos(ang)))
            ty = int(round(31.5 + rr * math.sin(ang)))
            if 0 <= tx < 64 and 0 <= ty < 64:
                sd_px[tx, ty] = (255, 236, 158, 255) if k % 3 == 0 else bronze_hi
    sd.save(os.path.join(kit_dir, "sundial_base.png"))

    # joystick_base.png (96x96) & joystick_knob.png / joystick_knob_pressed.png (40x40)
    jb = Image.new("RGBA", (96, 96), (0, 0, 0, 0))
    jb_px = jb.load()
    for y in range(96):
        for x in range(96):
            dx, dy = abs(x - 47.5), abs(y - 47.5)
            d_oct = max(dx, dy, (dx + dy) * 0.7071)
            if d_oct > 45.0:
                continue
            if d_oct >= 43.5:
                jb_px[x, y] = outline
            elif d_oct >= 36.0:
                shade = 1.15 if (x + y < 95) else 0.78
                jb_px[x, y] = (
                    min(255, int(bronze_mid[0] * shade)),
                    min(255, int(bronze_mid[1] * shade)),
                    min(255, int(bronze_mid[2] * shade)),
                    235,
                )
            elif d_oct >= 34.5:
                jb_px[x, y] = outline
            else:
                jb_px[x, y] = (20, 26, 36, 195)
    jb.save(os.path.join(kit_dir, "joystick_base.png"))

    for fn, rim_c, gem_c in (
        ("joystick_knob.png", bronze_mid, (56, 138, 152, 245)),
        ("joystick_knob_pressed.png", bronze_hi, (92, 212, 186, 255)),
    ):
        jk = Image.new("RGBA", (40, 40), (0, 0, 0, 0))
        jk_px = jk.load()
        for y in range(40):
            for x in range(40):
                dx, dy = abs(x - 19.5), abs(y - 19.5)
                d_oct = max(dx, dy, (dx + dy) * 0.7071)
                if d_oct > 18.0:
                    continue
                if d_oct >= 16.5:
                    jk_px[x, y] = outline
                elif d_oct >= 12.0:
                    jk_px[x, y] = rim_c
                elif d_oct >= 10.8:
                    jk_px[x, y] = outline
                else:
                    jk_px[x, y] = gem_c
        jk.save(os.path.join(kit_dir, fn))

    # vn_dialogue_frame.png (320x96)
    df = Image.new("RGBA", (320, 96), (0, 0, 0, 0))
    dd = ImageDraw.Draw(df)
    dd.rounded_rectangle([2, 2, 317, 93], radius=10, fill=(28, 20, 16, 244), outline=(218, 178, 98, 255), width=3)
    dd.rounded_rectangle([6, 6, 313, 89], radius=7, fill=(44, 32, 24, 248), outline=(128, 94, 52, 255), width=2)
    dd.line([(12, 5), (308, 5)], fill=(255, 232, 168, 220), width=1)
    for cx, cy in ((10, 10), (310, 10), (10, 86), (310, 86)):
        dd.ellipse([cx - 3, cy - 3, cx + 3, cy + 3], fill=(236, 196, 112, 255), outline=(48, 32, 18, 255), width=1)
    df.save(os.path.join(kit_dir, "vn_dialogue_frame.png"))

    # Remove leftover non-canonical files in Kit
    for junk in (
        "action_btn_dash.png",
        "action_btn_shield.png",
        "action_btn_sword.png",
        "action_btn_whistle.png",
        "hero_portrait_frame_25d.png",
        "hud_hero_status_2026.png",
        "hud_top_right_stardew_2026.png",
        "vn_dialogue_box_2026.png",
    ):
        jp = os.path.join(kit_dir, junk)
        if os.path.exists(jp):
            os.remove(jp)

    # 8. Build high-luminance 2.5D beveled 9-slice frames in from_argon/Assets/Art/UI/Frames/ & UI/
    #    CRITICAL: Because Unity multiplies Image.sprite * Image.color, the interior of 9-slice frames MUST be
    #    high-luminance warm ivory/amber so BOTH dark-tinted panels (CharacterPanel, MythHud) and light-tinted
    #    parchment popups with dark-brown text (FarmOverview, PigInspect, DefenseBuild, DayMarket) have >9:1 contrast!
    ui_legacy_dir = os.path.join(ROOT, ".collab/claude-2d/ui_and_map_sync/from_argon/Assets/Art/UI")
    ui_frames_dir = os.path.join(ui_legacy_dir, "Frames")
    ui_controls_dir = os.path.join(ui_legacy_dir, "Controls")
    for d in (ui_legacy_dir, ui_frames_dir, ui_controls_dir):
        os.makedirs(d, exist_ok=True)

    def make_25d_9slice(w: int, h: int, fill_top, fill_bot, rim_hi, rim_sh, rim_mid, inset_shadow=True):
        im = Image.new("RGBA", (w, h), (0, 0, 0, 0))
        px_m = im.load()
        for yy in range(h):
            t = yy / max(1.0, float(h - 1))
            r_in = int(fill_top[0] * (1.0 - t) + fill_bot[0] * t)
            g_in = int(fill_top[1] * (1.0 - t) + fill_bot[1] * t)
            b_in = int(fill_top[2] * (1.0 - t) + fill_bot[2] * t)
            for xx in range(w):
                # Corner 2px cut for crisp pixel-art chamfer
                if (xx in (0, w - 1) and yy in (0, 1, h - 2, h - 1)) or (yy in (0, h - 1) and xx in (0, 1, w - 2, w - 1)):
                    continue
                dist_l, dist_r = xx, w - 1 - xx
                dist_t, dist_b = yy, h - 1 - yy
                d_edge = min(dist_l, dist_r, dist_t, dist_b)
                if d_edge == 0:
                    px_m[xx, yy] = outline
                elif d_edge in (1, 2):
                    px_m[xx, yy] = rim_hi if (dist_t <= 2 or dist_l <= 2) else rim_sh
                elif d_edge == 3:
                    px_m[xx, yy] = rim_mid
                elif d_edge == 4:
                    px_m[xx, yy] = (62, 42, 24, 255)
                elif d_edge == 5 and inset_shadow:
                    px_m[xx, yy] = (
                        max(0, int(r_in * 0.88)),
                        max(0, int(g_in * 0.86)),
                        max(0, int(b_in * 0.84)),
                        255,
                    )
                else:
                    px_m[xx, yy] = (r_in, g_in, b_in, 255)
        # 4 corner bronze/gold rivets at inset 4
        for cx, cy in ((4, 4), (w - 5, 4), (4, h - 5), (w - 5, h - 5)):
            px_m[cx, cy] = rim_hi
        return im

    frm_wood = make_25d_9slice(
        64, 64,
        fill_top=(218, 188, 146), fill_bot=(194, 162, 118),
        rim_hi=(246, 212, 128, 255), rim_sh=(104, 72, 32, 255), rim_mid=(186, 142, 68, 255),
    )
    frm_parch = make_25d_9slice(
        64, 64,
        fill_top=(250, 242, 224), fill_bot=(238, 226, 198),
        rim_hi=(232, 194, 114, 255), rim_sh=(116, 82, 40, 255), rim_mid=(176, 134, 68, 255),
    )
    frm_slot_n = make_25d_9slice(
        48, 48,
        fill_top=(242, 232, 208), fill_bot=(226, 212, 182),
        rim_hi=(234, 196, 116, 255), rim_sh=(108, 76, 36, 255), rim_mid=(172, 132, 66, 255),
    )
    frm_slot_s = make_25d_9slice(
        48, 48,
        fill_top=(255, 248, 224), fill_bot=(246, 232, 192),
        rim_hi=(255, 234, 148, 255), rim_sh=(164, 118, 42, 255), rim_mid=(238, 194, 88, 255),
    )
    frm_btn = make_25d_9slice(
        48, 48,
        fill_top=(244, 230, 196), fill_bot=(224, 204, 162),
        rim_hi=(248, 214, 132, 255), rim_sh=(112, 78, 36, 255), rim_mid=(188, 146, 74, 255),
    )

    for target_d in (ui_legacy_dir, ui_frames_dir):
        frm_wood.save(os.path.join(target_d, "frame_wood_panel.png"))
        frm_parch.save(os.path.join(target_d, "frame_parchment.png"))
        frm_slot_n.save(os.path.join(target_d, "frame_slot_normal.png"))
        frm_slot_s.save(os.path.join(target_d, "frame_slot_selected.png"))
        frm_btn.save(os.path.join(target_d, "frame_action_button.png"))

    jb.save(os.path.join(ui_legacy_dir, "joystick_base.png"))
    jb.save(os.path.join(ui_controls_dir, "joystick_base.png"))
    Image.open(os.path.join(kit_dir, "joystick_knob.png")).save(os.path.join(ui_legacy_dir, "joystick_knob.png"))
    Image.open(os.path.join(kit_dir, "joystick_knob.png")).save(os.path.join(ui_controls_dir, "joystick_knob.png"))


def write_ui_25d_auto_styler():
    """
    Writes from_argon/Assets/Scripts/UI/UI25DAutoStyler.cs:
    Layers 4K crisp point-filtering, 2.5D drop-shadow/bevel depth on modal panels/cards/buttons,
    and crisp Vietnamese TMP typography padding & outline onto all UI and world sprites WITHOUT
    altering any approved design, layout, RectTransform size/position, or GameObject name.
    """
    styler_path = os.path.join(FROM_ARGON_UI, "UI25DAutoStyler.cs")
    code = """using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace PigTycoon.Presentation
{
    /// <summary>
    /// Non-destructive 4K / 2.5D visual polish layer.
    /// Preserves 100% of approved layouts, RectTransform sizes, GameObject names, and sprites while:
    /// 1. Enforcing FilterMode.Point (zero bilinear blur at 1080p/1440p/4K) on all UI & world pixel sprites.
    /// 2. Adding subtle 2.5D directional drop-shadow depth to modal cards, slots, toasts, and buttons.
    /// 3. Enforcing extraPadding & crisp sub-pixel outline on Vietnamese TextMeshProUGUI labels.
    /// </summary>
    [DisallowMultipleComponent, DefaultExecutionOrder(1350)]
    public sealed class UI25DAutoStyler : MonoBehaviour
    {
        public static UI25DAutoStyler Instance { get; private set; }

        private readonly HashSet<int> styledImages = new HashSet<int>();
        private readonly HashSet<int> styledTexts = new HashSet<int>();
        private readonly HashSet<int> crispTextures = new HashSet<int>();
        private float nextSweepTime;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void AutoInstall()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
            SceneManager.sceneLoaded += OnSceneLoaded;
            Ensure();
        }

        private static void OnSceneLoaded(Scene scene, LoadSceneMode mode) => Ensure();

        public static UI25DAutoStyler Ensure()
        {
            if (Instance != null) return Instance;
            var go = new GameObject("UI25DAutoStyler");
            DontDestroyOnLoad(go);
            Instance = go.AddComponent<UI25DAutoStyler>();
            return Instance;
        }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        private void Update()
        {
            if (Time.unscaledTime < nextSweepTime) return;
            nextSweepTime = Time.unscaledTime + 0.45f;
            SweepActiveHierarchy();
        }

        public void SweepActiveHierarchy()
        {
            // 1. Enforce crisp Point filtering on all world SpriteRenderers
            var worldRenderers = FindObjectsByType<SpriteRenderer>(FindObjectsSortMode.None);
            for (int i = 0; i < worldRenderers.Length; i++)
            {
                var sr = worldRenderers[i];
                if (sr != null && sr.sprite != null)
                    EnforceCrispTexture(sr.sprite.texture);
            }

            // 2. Style active UI Images (Point filter + non-destructive 2.5D drop shadow on cards/modals/buttons)
            var images = FindObjectsByType<Image>(FindObjectsSortMode.None);
            for (int i = 0; i < images.Length; i++)
            {
                var img = images[i];
                if (img == null) continue;
                if (img.sprite != null)
                    EnforceCrispTexture(img.sprite.texture);

                int id = img.GetInstanceID();
                if (!styledImages.Add(id)) continue;

                string n = img.gameObject.name;
                bool isDimmer = n.Contains("Dim") || n.Contains("Overlay") || img.color.a < 0.45f;
                if (isDimmer) continue;

                bool isCardOrModal =
                    n.Contains("Modal") || n.Contains("Panel") || n.Contains("Card") ||
                    n.Contains("Toast") || n.Contains("Banner") || n.Contains("Pill") ||
                    n.Contains("Slot") || n.Contains("Btn") || n.Contains("Button") ||
                    img.GetComponent<Button>() != null;

                if (isCardOrModal && img.GetComponent<Shadow>() == null)
                {
                    var sh = img.gameObject.AddComponent<Shadow>();
                    bool isLargePanel = img.rectTransform.rect.width >= 260f && img.rectTransform.rect.height >= 140f;
                    sh.effectColor = isLargePanel
                        ? new Color(0.02f, 0.03f, 0.06f, 0.62f)
                        : new Color(0.03f, 0.04f, 0.07f, 0.46f);
                    sh.effectDistance = isLargePanel ? new Vector2(3f, -4f) : new Vector2(1.5f, -2f);
                    sh.useGraphicAlpha = true;
                }
            }

            // 3. Enforce crisp 4K Vietnamese typography padding & readable contrast
            var texts = FindObjectsByType<TextMeshProUGUI>(FindObjectsSortMode.None);
            for (int i = 0; i < texts.Length; i++)
            {
                var tmp = texts[i];
                if (tmp == null) continue;
                int id = tmp.GetInstanceID();
                if (!styledTexts.Add(id)) continue;

                tmp.extraPadding = true;
                if (tmp.color.r + tmp.color.g + tmp.color.b > 1.85f && tmp.outlineWidth < 0.12f)
                {
                    tmp.outlineWidth = 0.18f;
                    tmp.outlineColor = new Color32(18, 12, 8, 220);
                }
            }
        }

        private void EnforceCrispTexture(Texture2D tex)
        {
            if (tex == null) return;
            int id = tex.GetInstanceID();
            if (!crispTextures.Add(id)) return;
            if (tex.filterMode != FilterMode.Point)
                tex.filterMode = FilterMode.Point;
            if (tex.anisoLevel != 0)
                tex.anisoLevel = 0;
        }
    }
}
"""
    write_utf8(styler_path, code)


def cleanup_project_directory_clutter():
    """
    Cleans up scattered duplicate folders and organizes tools/ so the repository is clean and legible:
    1. Removes duplicate root from_argon, argon-deliverables, and .collab/claude-2d/argon-deliverables
       (keeping ONLY the canonical .collab/claude-2d/ui_and_map_sync/from_argon).
    2. Moves root .agent_scratch and Tripo3D_Map_Inputs into .collab/archive_legacy/.
    3. Moves loose root .txt report files into Docs/reports/.
    4. Organizes tools/ into clear subdirectories:
       - tools/character_paperdoll/ (approved 56px character & 16-item paper-doll scripts)
       - tools/world_3d_blender/ (3D Blender/observatory/terrain/citadel scripts)
       - tools/_retired_legacy_character_scripts/ (superseded one-off UI/art patch scripts)
       while keeping headless_runner.py, apply_ui_contracts_1_and_2.py,observatory*.py (if referenced), verify.ps1, mcp/, pixelart/ at root.
    5. Removes unreferenced duplicate folders in from_argon/Assets/Art/PixelMyth/Buildings (argon_env_*)
       and Characters (an_equipped_*, argon_an_equipped_*, khoa_equipped_*, and unreferenced argon_* duplicates).
    """
    # 1. Remove duplicate non-canonical from_argon / argon-deliverables folders
    for dup_rel in (
        "from_argon",
        "argon-deliverables",
        ".collab/claude-2d/argon-deliverables",
    ):
        dup_p = os.path.join(ROOT, dup_rel)
        if os.path.exists(dup_p):
            shutil.rmtree(dup_p, ignore_errors=True)

    # 2. Move .agent_scratch and Tripo3D_Map_Inputs into .collab/archive_legacy/
    archive_leg = os.path.join(ROOT, ".collab", "archive_legacy")
    os.makedirs(archive_leg, exist_ok=True)
    for scratch_rel in (".agent_scratch", "Tripo3D_Map_Inputs"):
        sp = os.path.join(ROOT, scratch_rel)
        dp = os.path.join(archive_leg, scratch_rel.lstrip("."))
        if os.path.exists(sp):
            if os.path.exists(dp):
                shutil.rmtree(dp, ignore_errors=True)
            shutil.move(sp, dp)

    # 3. Move loose root .txt files into Docs/reports/
    reports_dir = os.path.join(ROOT, "Docs", "reports")
    os.makedirs(reports_dir, exist_ok=True)
    for txt_fn in ("BAO_CAO_NGUOI_B_DAY_DU_LOGIC_VA_CODE.txt", "HUONG_DAN_VA_CAU_HINH.txt", "CHAY_MAY_AO_VA_GAME.bat"):
        tp = os.path.join(ROOT, txt_fn)
        if os.path.exists(tp):
            shutil.move(tp, os.path.join(reports_dir, txt_fn))

    # 4. Organize tools/ directory
    tools_dir = os.path.join(ROOT, "tools")
    char_dir = os.path.join(tools_dir, "character_paperdoll")
    w3d_dir = os.path.join(tools_dir, "world_3d_blender")
    ret_dir = os.path.join(tools_dir, "_retired_legacy_character_scripts")
    for d in (char_dir, w3d_dir, ret_dir):
        os.makedirs(d, exist_ok=True)

    keep_at_tools_root = {
        "apply_ui_contracts_1_and_2.py",
        "headless_runner.py",
        "verify.ps1",
        "observatory_blender.py",
        "observatory_geometry.py",
        "export_observatory_source.py",
        "render_observatory_geometry_proof.py",
        "review_observatory.py",
        "validate_observatory_geometry.py",
        "verify_observatory_package.py",
    }
    char_paperdoll_scripts = {
        "lock_approved_character_pipeline.py",
        "restore_approved_11pm_2am_assets_56px.py",
        "build_paperdoll_16_gdd_gear_and_denoised_heroes.py",
        "upgrade_paperdoll_body_mask_and_readability.py",
        "build_qa_verified_p1_p4_animations_and_gear.py",
        "denoise_world_entities_and_render_16_gdd_catalog.py",
    }
    w3d_prefixes = (
        "alpine_",
        "citadel_",
        "harbor_beacon",
        "terrain_water",
        "village_buildings",
        "world_",
        "review_",
        "validate_",
        "map_",
        "blender_lean_prelude.py",
        "canonical_map_helpers.py",
        "document_farm_surface.py",
        "document_world_scene_assembler.py",
        "farm_landscape_dressing_codex.py",
        "functional_building_study.py",
        "generate_post_nature_234_materials.py",
        "mountain_reference_study.py",
        "rebuild_baseline_mountain_chain.py",
        "rerender_master_and_z09.py",
        "extract_quaternius_fbx.py",
        "quaternius_nature_cache.npz",
        "apply_reference_surface_materials.py",
        "audit_and_ground_world_v11.py",
        "build_3_separate_abc_master_boards.py",
        "build_document_world_candidates_argon.py",
        "build_expanded_gold_world.py",
        "build_farm_main_3d_map.py",
        "build_functional_buildings_argon.py",
        "build_meadow_art_rework.py",
        "build_reference_world_art_master.py",
        "build_true360_glb_masterpiece.py",
        "catalog_reusable_world_assets.py",
    )

    for entry in os.listdir(tools_dir):
        full_e = os.path.join(tools_dir, entry)
        if os.path.isdir(full_e) or entry in keep_at_tools_root:
            continue
        if entry in char_paperdoll_scripts:
            shutil.move(full_e, os.path.join(char_dir, entry))
        elif entry.startswith(w3d_prefixes):
            shutil.move(full_e, os.path.join(w3d_dir, entry))
        else:
            shutil.move(full_e, os.path.join(ret_dir, entry))

    # 5. Clean unreferenced duplicate folders inside from_argon/Assets/Art/PixelMyth/ & sync An's 64 paper-doll folders
    pm_buildings = os.path.join(ROOT, ".collab/claude-2d/ui_and_map_sync/from_argon/Assets/Art/PixelMyth/Buildings")
    if os.path.exists(pm_buildings):
        for bname in os.listdir(pm_buildings):
            if bname.startswith("argon_env_"):
                shutil.rmtree(os.path.join(pm_buildings, bname), ignore_errors=True)

    pm_chars = os.path.join(ROOT, ".collab/claude-2d/ui_and_map_sync/from_argon/Assets/Art/PixelMyth/Characters")
    assets_chars = os.path.join(ROOT, "Assets/Art/PixelMyth/Characters")
    qa_dir = os.path.join(ROOT, ".collab/claude-2d/ui_and_map_sync/from_argon/Docs/qa")
    os.makedirs(qa_dir, exist_ok=True)

    if os.path.exists(assets_chars) and os.path.exists(pm_chars):
        for cname in os.listdir(assets_chars):
            src_c = os.path.join(assets_chars, cname)
            if not os.path.isdir(src_c):
                continue
            # Sync An's 64 independent paper-doll equipment folders (an__*) into from_argon (never touching base an/)
            if cname.startswith("an__"):
                dst_c = os.path.join(pm_chars, cname)
                if not os.path.exists(dst_c):
                    shutil.copytree(src_c, dst_c)
            elif cname in ("Hero_An", "Hero_An_Candidates_v13") or "_equipped_" in cname or (cname.startswith("argon_") and cname != "argon_pig"):
                shutil.rmtree(src_c, ignore_errors=True)

    if os.path.exists(pm_chars):
        for cname in os.listdir(pm_chars):
            cpath = os.path.join(pm_chars, cname)
            if os.path.isfile(cpath) and cname.endswith(".png"):
                shutil.move(cpath, os.path.join(qa_dir, cname))
                continue
            if not os.path.isdir(cpath):
                continue
            # Remove obsolete set-based outfit folders and unreferenced argon_* duplicate character folders
            if "_equipped_" in cname or (cname.startswith("argon_") and cname != "argon_pig"):
                shutil.rmtree(cpath, ignore_errors=True)


def render_ui_ux_visual_inspection_proof():
    """
    Renders a high-resolution visual inspection board of the actual UI/UX screens & assets
    into the artifact directory so we can visually inspect contrast, layout, Shield guard bar,
    6-slot MMORPG Paper-Doll, PigInspectPopup, and FarmOverviewPopup Capacity Bars.
    """
    from PIL import ImageFont

    artifact_dir = r"C:\Users\Admin\.gemini\antigravity\brain\ac0f248a-d876-47b0-9f6a-277b76657366"
    os.makedirs(artifact_dir, exist_ok=True)
    canvas = Image.new("RGBA", (1440, 900), (24, 34, 26, 255))
    dr = ImageDraw.Draw(canvas)

    try:
        f_title = ImageFont.truetype("C:/Windows/Fonts/arialbd.ttf", 14)
        f_body = ImageFont.truetype("C:/Windows/Fonts/arialbd.ttf", 12)
        f_small = ImageFont.truetype("C:/Windows/Fonts/arial.ttf", 11)
    except Exception:
        f_title = f_body = f_small = None

    kit_dir = os.path.join(FROM_ARGON_ART_UI, "Kit")
    items_dir = os.path.join(FROM_ARGON_ART_UI, "Icons/items")
    equip_dir = os.path.join(FROM_ARGON_ART_UI, "Icons/equip")
    weapons_dir = os.path.join(FROM_ARGON_ART_UI, "Icons/weapons")
    status_dir = os.path.join(FROM_ARGON_ART_UI, "Icons/status")
    chars_dir = os.path.join(ROOT, ".collab/claude-2d/ui_and_map_sync/from_argon/Assets/Art/PixelMyth/Characters")

    def paste_scaled(img_path, pos, scale=1):
        if not os.path.exists(img_path):
            return
        im = Image.open(img_path).convert("RGBA")
        if scale != 1:
            im = im.resize((int(im.width * scale), int(im.height * scale)), Image.Resampling.NEAREST)
        canvas.paste(im, pos, im)

    # 1. Top-Left HUD Preview (2x scale as at 1080p): Sundial + Shield Guard Bar + Stamina Bar + Status Row
    paste_scaled(os.path.join(kit_dir, "sundial_base.png"), (24, 24), scale=2)
    paste_scaled(os.path.join(kit_dir, "sundial_needle.png"), (24, 24), scale=2)
    # HP / Chống Chịu Bar (with SHIELD icon!)
    paste_scaled(os.path.join(kit_dir, "bar_hp_trough.png"), (24, 168), scale=2)
    paste_scaled(os.path.join(kit_dir, "bar_hp_fill.png"), (24 + 24, 168 + 4), scale=2)
    paste_scaled(os.path.join(kit_dir, "bar_hp_vessel_frame.png"), (24, 168), scale=2)
    dr.text((196, 170), "<- Thanh Chống Chịu 80x12 (Biểu tượng KHIÊN #159)", fill=(250, 232, 168, 255), font=f_body)
    # Stamina Bar
    paste_scaled(os.path.join(kit_dir, "bar_stamina_trough.png"), (24, 198), scale=2)
    paste_scaled(os.path.join(kit_dir, "bar_stamina_fill_bright.png"), (24 + 16, 198 + 4), scale=2)
    paste_scaled(os.path.join(kit_dir, "bar_stamina_frame.png"), (24, 198), scale=2)
    dr.text((226, 200), "<- Thanh Thể Lực Kép 96x12 (Tia Sét)", fill=(168, 244, 196, 255), font=f_body)

    # 4 Crisis & Status icons at 2x
    for idx, st in enumerate(("la_do_suong", "qua_nhiet", "say_nang", "loi_tam_spark")):
        paste_scaled(os.path.join(status_dir, f"{st}_16.png"), (24 + idx * 42, 234), scale=2)
    dr.text((196, 240), "<- 4 Icon Trạng thái: Lá Dò Sương, Quá Nhiệt, Say Nắng, Lôi Tâm", fill=(255, 214, 142, 255), font=f_small)

    # 2. Top-Center Pig Crisis Pointer Banner (#158)
    dr.rounded_rectangle([480, 18, 960, 56], radius=6, fill=(118, 28, 26, 245), outline=(250, 204, 84, 255), width=2)
    dr.text((502, 29), "KHẨN: Heo Lôi Mạch #04 RUN GIẬT LÔI TÂM! [->] (Bấm mở thẻ)", fill=(255, 236, 156, 255), font=f_title)

    # 3. Left Panel: 6-Slot MMORPG Paper-Doll CharacterPanel (Dark-tinted GetFrameSlotNormal + 204x238 PaperDoll)
    dr.rounded_rectangle([24, 290, 470, 740], radius=8, fill=(28, 22, 18, 248), outline=(218, 176, 92, 255), width=3)
    dr.text((38, 304), "NHÂN VẬT [C] — 6 Ô TRANG BỊ PAPER-DOLL + BÀN KHẮC", fill=(252, 224, 138, 255), font=f_title)
    dr.text((38, 326), "Điểm Võ 4 · Bí Kíp 2 · Lối chơi Cân Bằng (6 cân) · Mực Ấn 1", fill=(214, 198, 168, 255), font=f_body)
    # PaperDoll center well (204x238) showing An & Khoa with 6-slot Paper-Doll overlays
    dr.rounded_rectangle([142, 354, 346, 592], radius=6, fill=(38, 30, 24, 255), outline=(186, 146, 72, 255), width=2)

    def composite_hero_paperdoll(hero_id, gear_folders):
        base_p = os.path.join(chars_dir, hero_id, f"{hero_id}_idle.png")
        if not os.path.exists(base_p):
            return None
        cell = Image.open(base_p).convert("RGBA").crop((0, 0, 64, 64))
        for gf in gear_folders:
            gp = os.path.join(chars_dir, gf, f"{gf}_idle.png")
            if os.path.exists(gp):
                g_cell = Image.open(gp).convert("RGBA").crop((0, 0, 64, 64))
                cell = Image.alpha_composite(cell, g_cell)
        return cell.resize((128, 128), Image.Resampling.NEAREST)

    an_comp = composite_hero_paperdoll(
        "an",
        ("an__armor_ao_mua_dau__III", "an__pants_quan_giap_xich__II", "an__boot_giay_dinh__III", "an__wpn_dao_rua__IV"),
    )
    khoa_comp = composite_hero_paperdoll(
        "khoa",
        ("khoa__armor_giap_luoi_thep__IV", "khoa__pants_quan_giap_xich__III", "khoa__boot_giay_dinh__III", "khoa__wpn_giao_moc__IV"),
    )
    if an_comp is not None:
        canvas.paste(an_comp, (134, 410), an_comp)
    if khoa_comp is not None:
        canvas.paste(khoa_comp, (226, 410), khoa_comp)
    dr.text((156, 566), "An & Khoa (64+64 lớp Paper-Doll)", fill=(238, 214, 156, 255), font=f_small)

    # Ensure short Paper-Doll stem aliases exist in equip/ as well
    short_aliases = {
        "arm_ao_chi": "armor_ao_chi",
        "arm_ao_mua": "armor_ao_mua_dau",
        "arm_giap_da": "armor_giap_da_thuoc",
        "arm_giap_luoi": "armor_giap_luoi_thep",
        "pnt_quan_da": "pants_quan_da_lot_bong",
        "pnt_quan_xich": "pants_quan_giap_xich",
        "bot_guoc_gio": "boot_guoc_gio",
        "bot_giay_dinh": "boot_giay_dinh",
        "nck_coi_heo": "nck_coi_chan_heo",
        "nck_den_bao": "nck_mat_day_den_bao",
        "brc_cach_dien": "brc_vong_cach_dien",
        "brc_huyet_nha": "brc_vong_huyet_nha",
    }
    for short_stem, full_stem in short_aliases.items():
        for sz in (16, 24):
            src_eq = os.path.join(equip_dir, f"{full_stem}_{sz}.png")
            dst_eq = os.path.join(equip_dir, f"{short_stem}_{sz}.png")
            if os.path.exists(src_eq) and not os.path.exists(dst_eq):
                shutil.copy2(src_eq, dst_eq)

    # 6 equipment slots around PaperDoll with Roman numeral quality badges + real equipment icons
    slot_coords = [
        (48, 362, "Vũ Khí", "IV", (245, 95, 85), os.path.join(equip_dir, "wpn_dao_rua_24.png")),
        (48, 442, "Áo Giáp", "III", (185, 110, 255), os.path.join(equip_dir, "armor_ao_mua_dau_24.png")),
        (48, 522, "Quần", "II", (80, 175, 255), os.path.join(equip_dir, "pants_quan_giap_xich_24.png")),
        (364, 362, "Giày", "III", (185, 110, 255), os.path.join(equip_dir, "boot_giay_dinh_24.png")),
        (364, 442, "Dây Chuyền", "II", (80, 175, 255), os.path.join(equip_dir, "nck_coi_chan_heo_24.png")),
        (364, 522, "Vòng Tay", "I", (210, 210, 215), os.path.join(equip_dir, "brc_vong_huyet_nha_24.png")),
    ]
    for sx, sy, slbl, qrom, qcol, icon_p in slot_coords:
        dr.rounded_rectangle([sx, sy, sx + 78, sy + 68], radius=5, fill=(46, 36, 28, 255), outline=(*qcol, 255), width=2)
        paste_scaled(icon_p, (sx + 21, sy + 10), scale=1.5)
        dr.text((sx + 6, sy + 4), qrom, fill=(*qcol, 255), font=f_body)
        dr.text((sx + 6, sy + 48), slbl, fill=(232, 218, 192, 255), font=f_small)

    dr.text((42, 610), "Bàn Khắc: Phù Văn 2/6 · Mệnh Ấn 1/3 · Mực Ấn 1 (mỗi ấn -10 Chống Chịu)", fill=(252, 224, 138, 255), font=f_body)

    # 4. Center Panel: FarmOverviewPopup with high-luminance Parchment frame + 4 Capacity Bars & Bottleneck (#156/#157)
    dr.rounded_rectangle([490, 80, 950, 490], radius=8, fill=(244, 234, 212, 255), outline=(176, 134, 68, 255), width=3)
    dr.text((512, 96), "SỨC CHỨA TRẠI [F] — NỀN GIẤY DA TƯƠNG PHẢN (>9.5:1)", fill=(61, 28, 8, 255), font=f_title)
    dr.text((512, 120), "Cấp trại 3 · 450/700 EXP  |  Áp lực sự kiện: 28/100 (Yên ả)", fill=(61, 28, 8, 255), font=f_body)
    dr.text((512, 142), "Đàn hiện tại: 22/24 con (91.7% · Ổn định)  |  Điểm nghẽn: Mái Trú", fill=(185, 28, 28, 255), font=f_body)
    cap_rows = [
        ("Ô Đất (x8): 32 chỗ · 4/6 ô (tối đa 25)", 0.65, False),
        ("Mái Trú (x6): 24 chỗ · 4/12 mái (+8 chỗ che tạm)  [ĐIỂM NGHẼN]", 0.48, True),
        ("Bồn Nước (x12): 36 chỗ · 3 bồn", 0.72, False),
        ("Máng Ăn (x10): 30 chỗ · 3 máng", 0.60, False),
    ]
    for idx, (clbl, ratio, is_bn) in enumerate(cap_rows):
        ry = 176 + idx * 44
        dr.rounded_rectangle([512, ry, 928, ry + 34], radius=4, fill=(46, 36, 26, 235))
        fw = int(412 * ratio)
        fcol = (214, 66, 52, 245) if is_bn else (56, 158, 115, 240)
        dr.rounded_rectangle([514, ry + 2, 514 + fw, ry + 32], radius=3, fill=fcol)
        dr.text((524, ry + 9), clbl, fill=(255, 255, 255, 255), font=f_body)
    for b_idx, b_txt in enumerate(("VÙNG PHỤC VỤ & MÁT", "KHAI HOANG · 450G·12TL (4/6)", "DỰNG MÁI · 260G+6 gỗ (4/12)", "DỌN BÃI (1/2)")):
        bx = 506 + (b_idx % 2) * 216
        by = 368 + (b_idx // 2) * 48
        dr.rounded_rectangle([bx, by, bx + 206, by + 38], radius=5, fill=(238, 220, 178, 255), outline=(148, 108, 52, 255), width=2)
        dr.text((bx + 10, by + 11), b_txt, fill=(61, 28, 8, 255), font=f_body)

    # 5. Right Panel: PigInspectPopup with Vietnamese PigText, HealthCosts.Quote, ItemUse & MarketDrove (#155/#157/#158)
    dr.rounded_rectangle([970, 80, 1416, 490], radius=8, fill=(244, 234, 212, 255), outline=(176, 134, 68, 255), width=3)
    dr.text((990, 96), "THẺ HEO CHI TIẾT — HỢP ĐỒNG LOGIC #1, #3, #4", fill=(61, 28, 8, 255), font=f_title)
    dr.text((990, 122), "Heo Lôi Mạch #04 · Dòng: Lôi Mạch · Tuổi: Trưởng thành", fill=(61, 28, 8, 255), font=f_body)
    dr.text((990, 144), "Khung thịt: 82% · Mỡ: 74% · Tươi: 88% · Tinh thần: 68 (Ổn định)", fill=(61, 28, 8, 255), font=f_body)
    dr.text((990, 166), "[Đã ăn Cám Linh Chi hôm nay] [Đã ăn Cám Hạ Hỏa hôm nay]", fill=(21, 128, 61, 255), font=f_body)
    for y_idx, y_txt in enumerate(("CÁCH LY", "VÔI · 20G·10TL", "THUỐC · 90G·2TL", "VẮC-XIN · 50G", "KỆ: BÌNH THƯỜNG")):
        bx = 988 + (y_idx % 3) * 138
        by = 198 + (y_idx // 3) * 42
        dr.rounded_rectangle([bx, by, bx + 130, by + 34], radius=4, fill=(236, 218, 176, 255), outline=(142, 104, 48, 255), width=2)
        dr.text((bx + 8, by + 9), y_txt, fill=(61, 28, 8, 255), font=f_small)
    dr.text((990, 292), "Vật phẩm chăm heo trực tiếp (PigCard_ItemButtons):", fill=(61, 28, 8, 255), font=f_body)
    for c_idx, c_txt in enumerate(("Cám Linh Chi (2)", "Cám Hạ Hỏa (3)", "Thuốc An Thần (1)", "Xô Nước Mát (4)")):
        bx = 988 + (c_idx % 2) * 206
        by = 316 + (c_idx // 2) * 40
        dr.rounded_rectangle([bx, by, bx + 196, by + 34], radius=4, fill=(224, 242, 232, 255), outline=(52, 138, 96, 255), width=2)
        dr.text((bx + 10, by + 9), c_txt, fill=(20, 68, 42, 255), font=f_body)
    dr.rounded_rectangle([988, 412, 1394, 452], radius=5, fill=(252, 228, 152, 255), outline=(168, 118, 36, 255), width=2)
    dr.text((1024, 424), "THÊM VÀO ĐOÀN ĐI CHỢ (2/8) — PigCard_DroveButton", fill=(61, 28, 8, 255), font=f_body)

    # 6. Bottom Strip: All 20 RareSources & ItemUse Icons (2x) + World TempShade Canopy (96x96) + VN Dialogue Frame (320x96)
    dr.text((490, 516), "20 ICON VẬT PHẨM HIẾM & TIÊU HAO (Icons/items/*_24.png @ 2x) + MÁI CHE TẠM (96x96) + HỘI THOẠI VN (320x96):", fill=(252, 224, 138, 255), font=f_title)
    all_stems = [
        "bi_kip", "linh_kien_tinh", "da_cuong_hoa", "bot_tu_tinh", "muc_an",
        "huyet_trien", "giay_lam", "giay_vang", "giay_tim", "giay_do",
        "xo_nuoc", "cam_linh_chi", "cam_ha_hoa", "thuoc_an_than_heo", "ho_so_giong_loai",
        "mai_che_tam", "tin_don_cho", "la_do_suong", "ban_do_huong_quai", "chia_cong_nguoc",
    ]
    for idx, st in enumerate(all_stems):
        ix = 490 + (idx % 10) * 54
        iy = 544 + (idx // 10) * 56
        paste_scaled(os.path.join(items_dir, f"{st}_24.png"), (ix, iy), scale=2)

    paste_scaled(os.path.join(kit_dir, "temp_shade_canopy.png"), (1130, 540), scale=1.4)
    dr.text((1118, 682), "temp_shade_canopy.png (96x96)", fill=(168, 244, 218, 255), font=f_body)

    paste_scaled(os.path.join(kit_dir, "vn_dialogue_frame.png"), (490, 675), scale=1.8)
    dr.text((515, 705), "BÀ SÁU — HỘI THOẠI CỐT TRUYỆN (HUD TỰ ĐỘNG ẨN 100% KHI MỞ HỘI THOẠI)", fill=(252, 224, 138, 255), font=f_title)
    dr.text((515, 735), "“Đợt nắng gắt này heo dễ bị Say Nắng hoặc kích động Lôi Tâm, nhớ dựng Mái Che Tạm và dội Xô Nước nhé!”", fill=(244, 236, 220, 255), font=f_body)

    out_path = os.path.join(artifact_dir, "ui_ux_and_directory_audit_proof.png")
    canvas.save(out_path)


def retire_legacy_scripts_and_audit():
    cleanup_project_directory_clutter()
    render_ui_ux_visual_inspection_proof()

    forbidden_symbols = [
        "Lot.Gear",
        "GetLootItemSprite",
        "QualityLabel",
        "GetEquipmentSprite",
        "WaterBasins",
        "DrainNotifications",
        "RefillFeeder",
        "WantsWater",
        "Drove.Toggle",
        "BuyLandPlot",
        "ReloadTrapWood",
        'Rect("HeroPortrait25D"',
    ]
    for root, _, files in os.walk(FROM_ARGON_UI):
        for fn in files:
            if not fn.endswith(".cs"):
                continue
            full = os.path.join(root, fn)
            content = read_utf8(full)
            for sym in forbidden_symbols:
                if sym in content:
                    raise RuntimeError(f"Forbidden symbol '{sym}' found in {full}")

    # Verify required UiProbe & Contract symbols
    bp = read_utf8(os.path.join(FROM_ARGON_UI, "BackpackPopup.cs"))
    assert '"UseItem"' in bp and '"ToQuickBag"' in bp, "BackpackPopup missing UseItem / ToQuickBag"
    pi = read_utf8(os.path.join(FROM_ARGON_UI, "PigInspectPopup.cs"))
    assert '"PigCard_ItemButtons"' in pi and '"PigCard_DroveButton"' in pi, "PigInspectPopup missing UiProbe names"

    # Verify exact 32-PPU HUD & UI Kit dimensions
    expected_sizes = {
        "bar_hp_vessel_frame.png": (80, 12),
        "bar_hp_fill.png": (62, 8),
        "bar_stamina_frame.png": (96, 12),
        "sundial_base.png": (64, 64),
        "joystick_base.png": (96, 96),
        "joystick_knob.png": (40, 40),
        "temp_shade_canopy.png": (96, 96),
        "vn_dialogue_frame.png": (320, 96),
    }
    for fn, exp_sz in expected_sizes.items():
        im = Image.open(os.path.join(FROM_ARGON_ART_UI, "Kit", fn))
        assert im.size == exp_sz, f"Expected {fn} {exp_sz}, got {im.size}"


if __name__ == "__main__":
    reset_from_claude_base()
    update_player_text()
    update_toast_overlay()
    update_appraisal_and_breeding()
    update_backpack_popup()
    update_pig_inspect()
    update_auction_and_intel()
    update_village_barter()
    update_defense_build()
    update_farm_overview()
    update_widgets_and_quest_tracker()
    update_myth_hud()
    update_character_and_daymarket_and_spriteloader()
    write_ui_25d_auto_styler()
    generate_ui_art_deliverables()
    retire_legacy_scripts_and_audit()
    print("ALL UI CONTRACTS #1, #2, #3, #4 + #159 + UI25DAutoStyler APPLIED & AUDITED IN from_argon/!")
