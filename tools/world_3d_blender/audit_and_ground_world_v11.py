#!/usr/bin/env python3
"""
Audit UI C# files for unsupported emoji glyphs (LiberationSans.ttf safety),
ensure grounded contact shadows on all PixelMyth Buildings & Decor sprites so nothing floats,
and sync updated C# & Art files to from_argon/.
"""
import os
import shutil
import json
from pathlib import Path
from PIL import Image

ROOT = Path(r"D:\Pig-tycoon")
FROM_ARGON = ROOT / r".collab\claude-2d\ui_and_map_sync\from_argon"

EMOJI_REPLACEMENTS = {
    "🛡️": "[Phòng Thủ]",
    "🛡": "[Phòng Thủ]",
    "🔒": "[Khóa]",
    "🔓": "[Mở]",
    "🔨": "[Xây]",
    "⏸️": "[Chờ]",
    "⏸": "[Chờ]",
    "🏹": "",
    "🔧": "",
    "🎯": "",
    "💰": "",
    "🪵": "",
    "🪨": "",
    "🏛️": "",
    "🏛": "",
    "⚡": "",
    "⬆️": "[Nâng]",
    "⬆": "[Nâng]",
    "⛓️": "",
    "⛓": "",
    "⚔️": "",
    "⚔": "",
    "🐖": "",
    "🐷": "",
    "🍖": "",
    "🌾": "",
    "🌿": "",
    "💊": "",
    "🧪": "",
    "📦": "",
    "📜": "",
    "🏆": "",
    "⭐": "*",
    "🌟": "*",
    "✨": "*",
    "🔥": "",
    "💧": "",
    "❄️": "",
    "❄": "",
    "⚠️": "[!]",
    "⚠": "[!]",
    "✅": "[OK]",
    "❌": "[X]",
    "♂️": "[Đ]",
    "♂": "[Đ]",
    "♀️": "[C]",
    "♀": "[C]",
    "◄": "<",
    "►": ">",
    "▲": "^",
    "▼": "v",
    "★": "*",
    "☆": "*",
    "→": "->",
    "−": "-",
    "√": "[x]",
    "≥": ">=",
    "≤": "<=",
    "●": "•",
    "\uFE0F": "",
}


def clean_ui_emojis():
    # Also ensure VillageBarterPopup Header_Bar has 9-slice crimson/gold banner and non-overlapping TimeBadge
    vbp = ROOT / "Assets" / "Scripts" / "UI" / "VillageBarterPopup.cs"
    if vbp.exists():
        vtxt = vbp.read_text(encoding="utf-8")
        old_hdr = '            hRt.anchoredPosition = new Vector2(0f, -8f);\n            hRt.sizeDelta = new Vector2(-20f, 38f);\n\n            // Tiêu đề Tiệm'
        new_hdr = '            hRt.anchoredPosition = new Vector2(0f, -6f);\n            hRt.sizeDelta = new Vector2(-20f, 40f);\n            var hImg = header.AddComponent<Image>();\n            hImg.sprite = UISpriteLoader.GetFrameSlotSelected();\n            hImg.type = Image.Type.Sliced;\n            hImg.color = new Color(0.46f, 0.20f, 0.14f, 0.98f);\n\n            // Tiêu đề Tiệm'
        if old_hdr in vtxt:
            vtxt = vtxt.replace(old_hdr, new_hdr, 1)
            vtxt = vtxt.replace('tRt.sizeDelta = new Vector2(400f, 32f);', 'tRt.sizeDelta = new Vector2(465f, 32f);', 1)
            vtxt = vtxt.replace('headerTitleText.color = new Color(0.24f, 0.12f, 0.05f);', 'headerTitleText.color = new Color(1f, 0.94f, 0.68f);\n            headerTitleText.textWrappingMode = TextWrappingModes.NoWrap;\n            headerTitleText.overflowMode = TextOverflowModes.Ellipsis;', 1)
            vtxt = vtxt.replace('tmRt.anchoredPosition = new Vector2(20f, 0f);\n            tmRt.sizeDelta = new Vector2(240f, 30f);', 'tmRt.anchoredPosition = new Vector2(138f, 0f);\n            tmRt.sizeDelta = new Vector2(200f, 30f);', 1)
            vtxt = vtxt.replace('timeInfoText.color = new Color(0.35f, 0.22f, 0.12f);', 'timeInfoText.color = new Color(0.95f, 0.88f, 0.74f);', 1)
            vbp.write_text(vtxt, encoding="utf-8")

    # Also ensure CharacterPanel has 9-slice SlotCards behind BuildPhoi (left/right), BuildPotential (info), BuildEquipment (pairs) and active tab highlight
    cp = ROOT / "Assets" / "Scripts" / "UI" / "CharacterPanel.cs"
    if cp.exists():
        ctxt = cp.read_text(encoding="utf-8")
        if "phoiLeftCard" not in ctxt:
            ctxt = ctxt.replace(
                "            var pairs = DayMarketPopup.Text(body, new Vector2(-260f, -172f), new Vector2(510f, 92f), 11f, TextAlignmentOptions.TopLeft);",
                "            var pairsCard = DayMarketPopup.MakeSlotCard(body, new Vector2(-260f, -158f), new Vector2(526f, 84f));\n            var pairs = DayMarketPopup.Text(pairsCard.transform, Vector2.zero, new Vector2(506f, 76f), 10.5f, TextAlignmentOptions.TopLeft);",
                1,
            )
            ctxt = ctxt.replace(
                "            var left = DayMarketPopup.Text(body, new Vector2(-235f, 40f), new Vector2(460f, 440f), 12, TextAlignmentOptions.TopLeft);",
                "            var phoiLeftCard = DayMarketPopup.MakeSlotCard(body, new Vector2(-255f, 10f), new Vector2(496f, 436f));\n            var left = DayMarketPopup.Text(phoiLeftCard.transform, Vector2.zero, new Vector2(468f, 412f), 11.5f, TextAlignmentOptions.TopLeft);",
                1,
            )
            ctxt = ctxt.replace(
                "            var right = DayMarketPopup.Text(body, new Vector2(245f, 120f), new Vector2(470f, 280f), 11, TextAlignmentOptions.TopLeft);",
                "            var phoiRightCard = DayMarketPopup.MakeSlotCard(body, new Vector2(255f, 92f), new Vector2(496f, 268f));\n            var right = DayMarketPopup.Text(phoiRightCard.transform, Vector2.zero, new Vector2(468f, 248f), 11f, TextAlignmentOptions.TopLeft);",
                1,
            )
            ctxt = ctxt.replace(
                "            var info = DayMarketPopup.Text(body, new Vector2(-345f, 125f), new Vector2(330f, 165f), 11.5f, TextAlignmentOptions.TopLeft);",
                "            var infoCard = DayMarketPopup.MakeSlotCard(body, new Vector2(-345f, 126f), new Vector2(338f, 174f));\n            var info = DayMarketPopup.Text(infoCard.transform, Vector2.zero, new Vector2(318f, 158f), 10.5f, TextAlignmentOptions.TopLeft);",
                1,
            )
            cp.write_text(ctxt, encoding="utf-8")
        elif "phoiBottomRightCard" not in ctxt:
            ctxt = ctxt.replace(
                "            var phoiRightCard = DayMarketPopup.MakeSlotCard(body, new Vector2(255f, 92f), new Vector2(496f, 268f));\n            var right = DayMarketPopup.Text(phoiRightCard.transform, Vector2.zero, new Vector2(468f, 248f), 11f, TextAlignmentOptions.TopLeft);",
                "            var phoiRightCard = DayMarketPopup.MakeSlotCard(body, new Vector2(255f, 108f), new Vector2(496f, 240f));\n            var right = DayMarketPopup.Text(phoiRightCard.transform, Vector2.zero, new Vector2(468f, 224f), 10.5f, TextAlignmentOptions.TopLeft);\n            var phoiBottomRightCard = DayMarketPopup.MakeSlotCard(body, new Vector2(255f, -104f), new Vector2(496f, 168f));",
                1,
            )
            cp.write_text(ctxt, encoding="utf-8")

    rp = ROOT / "Assets" / "Scripts" / "UI" / "RecruitPopup.cs"
    if rp.exists():
        rtxt = rp.read_text(encoding="utf-8")
        old_open = "                var canvas = VillageBarterPopup.FindScreenSpaceCanvas();\n                if (canvas == null) return;\n                Instance = canvas.gameObject.AddComponent<RecruitPopup>();\n                Instance.Build(canvas.transform);"
        new_open = "                var go = new GameObject(\"RecruitCanvas\", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));\n                var c = go.GetComponent<Canvas>();\n                c.renderMode = RenderMode.ScreenSpaceOverlay;\n                c.sortingOrder = 115;\n                var sc = go.GetComponent<CanvasScaler>();\n                sc.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;\n                sc.referenceResolution = new Vector2(1280f, 720f);\n                sc.matchWidthOrHeight = 0.5f;\n                Instance = go.AddComponent<RecruitPopup>();\n                Instance.Build(go.transform);"
        if old_open in rtxt:
            rtxt = rtxt.replace(old_open, new_open, 1)
            rp.write_text(rtxt, encoding="utf-8")

    dmp = ROOT / "Assets" / "Scripts" / "UI" / "DayMarketPopup.cs"
    if dmp.exists():
        dtxt = dmp.read_text(encoding="utf-8")
        old_dmp = "                var canvas = VillageBarterPopup.FindScreenSpaceCanvas();\n                if (canvas == null) { Debug.LogWarning(\"[DayMarket] no screen-space canvas\"); return; }\n                Instance = canvas.gameObject.AddComponent<DayMarketPopup>();\n                Instance.Build(canvas.transform);"
        new_dmp = "                var go = new GameObject(\"DayMarketCanvas\", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));\n                var c = go.GetComponent<Canvas>();\n                c.renderMode = RenderMode.ScreenSpaceOverlay;\n                c.sortingOrder = 115;\n                var sc = go.GetComponent<CanvasScaler>();\n                sc.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;\n                sc.referenceResolution = new Vector2(1280f, 720f);\n                sc.matchWidthOrHeight = 0.5f;\n                Instance = go.AddComponent<DayMarketPopup>();\n                Instance.Build(go.transform);"
        if old_dmp in dtxt:
            dtxt = dtxt.replace(old_dmp, new_dmp, 1)
            dmp.write_text(dtxt, encoding="utf-8")

    bpp = ROOT / "Assets" / "Scripts" / "UI" / "BreedingPickerPopup.cs"
    if bpp.exists():
        btxt = bpp.read_text(encoding="utf-8")
        old_bpp = "                var canvas = VillageBarterPopup.FindScreenSpaceCanvas();\n                if (canvas == null) { Debug.LogWarning(\"[Breeding] no screen-space canvas\"); return; }\n                Instance = canvas.gameObject.AddComponent<BreedingPickerPopup>();\n                Instance.Build(canvas.transform);"
        new_bpp = "                var go = new GameObject(\"BreedingPickerCanvas\", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));\n                var c = go.GetComponent<Canvas>();\n                c.renderMode = RenderMode.ScreenSpaceOverlay;\n                c.sortingOrder = 116;\n                var sc = go.GetComponent<CanvasScaler>();\n                sc.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;\n                sc.referenceResolution = new Vector2(1280f, 720f);\n                sc.matchWidthOrHeight = 0.5f;\n                Instance = go.AddComponent<BreedingPickerPopup>();\n                Instance.Build(go.transform);"
        if old_bpp in btxt:
            btxt = btxt.replace(old_bpp, new_bpp, 1)
            bpp.write_text(btxt, encoding="utf-8")

    vbp2 = ROOT / "Assets" / "Scripts" / "UI" / "VillageBarterPopup.cs"
    if vbp2.exists():
        vtxt2 = vbp2.read_text(encoding="utf-8")
        if "playerGoldText.color = new Color(0.80f, 0.45f, 0.05f);" in vtxt2:
            vtxt2 = vtxt2.replace(
                "playerGoldText.color = new Color(0.80f, 0.45f, 0.05f);",
                "playerGoldText.color = new Color(1f, 0.90f, 0.38f);",
                1,
            )
            vbp2.write_text(vtxt2, encoding="utf-8")
        old_find = "            foreach (var c in allCanvases)\n            {\n                if (c != null && c.renderMode != RenderMode.WorldSpace)\n                {\n                    return c;\n                }\n            }"
        new_find = "            foreach (var c in allCanvases)\n            {\n                if (c != null && c.renderMode != RenderMode.WorldSpace && c.name != \"MythHud\")\n                {\n                    var sc = c.GetComponent<CanvasScaler>();\n                    if (sc == null || sc.referenceResolution.x >= 800f)\n                        return c;\n                }\n            }"
        if old_find in vtxt2:
            vtxt2 = vtxt2.replace(old_find, new_find, 1)
            vbp2.write_text(vtxt2, encoding="utf-8")

    dbp = ROOT / "Assets" / "Scripts" / "UI" / "DefenseBuildPopup.cs"
    if dbp.exists():
        dbtxt = dbp.read_text(encoding="utf-8")
        old_db = "            var canvas = GetComponentInParent<Canvas>();\n            if (canvas == null) canvas = FindAnyObjectByType<Canvas>();\n            if (canvas == null)\n            {\n                var cObj = new GameObject(\"Defense_Canvas\");\n                canvas = cObj.AddComponent<Canvas>();\n                canvas.renderMode = RenderMode.ScreenSpaceOverlay;\n                cObj.AddComponent<CanvasScaler>();\n                cObj.AddComponent<GraphicRaycaster>();\n            }"
        new_db = "            var canvas = GetComponentInParent<Canvas>();\n            if (canvas == null) canvas = VillageBarterPopup.FindScreenSpaceCanvas();\n            if (canvas == null)\n            {\n                var cObj = new GameObject(\"Defense_Canvas\");\n                canvas = cObj.AddComponent<Canvas>();\n                canvas.renderMode = RenderMode.ScreenSpaceOverlay;\n                canvas.sortingOrder = 115;\n                var sc = cObj.AddComponent<CanvasScaler>();\n                sc.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;\n                sc.referenceResolution = new Vector2(1280f, 720f);\n                sc.matchWidthOrHeight = 0.5f;\n                cObj.AddComponent<GraphicRaycaster>();\n            }"
        if old_db in dbtxt:
            dbtxt = dbtxt.replace(old_db, new_db, 1)
            dbp.write_text(dbtxt, encoding="utf-8")

    # QuestTrackerPanel: add right margin for '-' button and hide tracker while modal popups are open
    qtp = ROOT / "Assets" / "Scripts" / "UI" / "QuestTrackerPanel.cs"
    if qtp.exists():
        qtxt = qtp.read_text(encoding="utf-8")
        if "trackerText.margin = new Vector4" not in qtxt:
            qtxt = qtxt.replace(
                "            trackerText.raycastTarget = false;",
                "            trackerText.margin = new Vector4(2f, 2f, 30f, 2f);\n            trackerText.raycastTarget = false;",
                1,
            )
            qtxt = qtxt.replace('minTxt.text = "–";', 'minTxt.text = "-";')
            qtxt = qtxt.replace('minTxt.text = minimized ? "+" : "–";', 'minTxt.text = minimized ? "+" : "-";')
            qtxt = qtxt.replace(
                "            if (Input.GetKeyDown(KeyCode.N)) ToggleJournal();",
                "            if (tracker != null)\n            {\n                bool modalOpen = BackpackPopup.InputBlockedThisFrame || CharacterPanel.IsOpen || DayMarketPopup.IsOpen || RecruitPopup.IsOpen || (VillageBarterPopup.Instance != null && VillageBarterPopup.Instance.IsOpen);\n                if (tracker.gameObject.activeSelf == modalOpen) tracker.gameObject.SetActive(!modalOpen);\n            }\n            if (Input.GetKeyDown(KeyCode.N)) ToggleJournal();",
                1,
            )
            qtp.write_text(qtxt, encoding="utf-8")

    # CharacterPanel: auto-size Hợp Kích button labels so "Thủ Chuyển Công (D->A)" never clips
    cp2 = ROOT / "Assets" / "Scripts" / "UI" / "CharacterPanel.cs"
    if cp2.exists():
        ctxt2 = cp2.read_text(encoding="utf-8")
        if "bl.enableAutoSizing = true;" not in ctxt2:
            ctxt2 = ctxt2.replace(
                "                bl.fontSize = 9.5f;\n                bl.text = $\"{cb.label}\";",
                "                bl.fontSize = 8.6f;\n                bl.enableAutoSizing = true;\n                bl.fontSizeMin = 7.4f;\n                bl.fontSizeMax = 9.4f;\n                bl.text = $\"{cb.label}\";",
                1,
            )
            cp2.write_text(ctxt2, encoding="utf-8")

    # VillageBarterPopup: ensure sortingOrder 114/115 on modalBackdropObj and PopupPanel so [E] world prompts never show on top
    vbp3 = ROOT / "Assets" / "Scripts" / "UI" / "VillageBarterPopup.cs"
    if vbp3.exists():
        vtxt3 = vbp3.read_text(encoding="utf-8")
        if "pCanvas.sortingOrder = 115;" not in vtxt3:
            old_open_vbp = """            if (modalBackdropObj != null)
            {
                modalBackdropObj.transform.SetAsLastSibling();
                modalBackdropObj.SetActive(true);
            }
            if (PopupPanel != null)
            {
                PopupPanel.transform.SetAsLastSibling();
                PopupPanel.SetActive(true);
            }"""
            new_open_vbp = """            if (modalBackdropObj != null)
            {
                modalBackdropObj.transform.SetAsLastSibling();
                modalBackdropObj.SetActive(true);
                var mbCanvas = modalBackdropObj.GetComponent<Canvas>() ?? modalBackdropObj.AddComponent<Canvas>();
                if (modalBackdropObj.GetComponent<GraphicRaycaster>() == null) modalBackdropObj.AddComponent<GraphicRaycaster>();
                mbCanvas.overrideSorting = true;
                mbCanvas.sortingOrder = 114;
            }
            if (PopupPanel != null)
            {
                PopupPanel.transform.SetAsLastSibling();
                PopupPanel.SetActive(true);
                var pCanvas = PopupPanel.GetComponent<Canvas>() ?? PopupPanel.AddComponent<Canvas>();
                if (PopupPanel.GetComponent<GraphicRaycaster>() == null) PopupPanel.AddComponent<GraphicRaycaster>();
                pCanvas.overrideSorting = true;
                pCanvas.sortingOrder = 115;
            }"""
            if old_open_vbp in vtxt3:
                vtxt3 = vtxt3.replace(old_open_vbp, new_open_vbp, 1)
                vbp3.write_text(vtxt3, encoding="utf-8")
            elif old_open_vbp.replace("\n", "\r\n") in vtxt3:
                vtxt3 = vtxt3.replace(old_open_vbp.replace("\n", "\r\n"), new_open_vbp.replace("\n", "\r\n"), 1)
                vbp3.write_text(vtxt3, encoding="utf-8")

    # UISpriteLoader: map all QualifiedItemRegistry IDs in GetLootSprite & fallback in GetBanditOutpostSprite
    usl = ROOT / "Assets" / "Scripts" / "UI" / "UISpriteLoader.cs"
    if usl.exists():
        utxt = usl.read_text(encoding="utf-8")
        if '"CamThuong"' not in utxt:
            old_loot = """        public static Sprite GetLootSprite(string itemKey)
        {
            if (string.IsNullOrEmpty(itemKey)) return GetIconWoodPlank();

            // 1. Kiểm tra trực tiếp trong Art/UI/Items/ hoặc PixelMyth/UI/Icons/equip/
            Sprite eqSpr = GetEquipIcon(itemKey, 24);
            if (eqSpr != null) return eqSpr;
            Sprite uiSpr = LoadSprite($"Art/UI/Items/{itemKey}.png", 32);
            if (uiSpr != null) return uiSpr;

            // 2. Tra cứu theo từ khóa
            return itemKey switch
            {
                "item_wood_plank" => GetIconWoodPlank(),
                "item_corn_feed" => LoadSprite("Art/UI/Items/item_corn_feed.png", 32),
                "item_rusty_nail" => LoadSprite("Art/UI/Items/item_rusty_nail.png", 32),
                "item_broken_post" => LoadSprite("Art/UI/Items/item_broken_post.png", 32),
                "item_rat_tail" => LoadSprite("Art/UI/Items/item_rat_tail.png", 32),
                "item_fertilizer" => LoadSprite("Art/UI/Items/item_fertilizer.png", 32),
                "item_alloy_scrap" => LoadSprite("Art/UI/Items/item_alloy_scrap.png", 32),
                "item_quantum_fiber" => LoadSprite("Art/UI/Items/item_quantum_fiber.png", 32),
                "item_plasma_core" => LoadSprite("Art/UI/Items/item_plasma_core.png", 32),
                "item_rope" => LoadSprite("Art/UI/Items/item_rope.png", 32),
                "icon_coin_gold" => GetIconGold(),
                _ => GetIconWoodPlank()
            };
        }"""
            new_loot = """        public static Sprite GetLootSprite(string itemKey)
        {
            if (string.IsNullOrEmpty(itemKey)) return GetIconWoodPlank();
            string cleanKey = itemKey.Trim();
            if (cleanKey.StartsWith("("))
            {
                int closeIdx = cleanKey.IndexOf(')');
                if (closeIdx >= 0 && closeIdx + 1 < cleanKey.Length)
                    cleanKey = cleanKey.Substring(closeIdx + 1);
            }

            // 1. Kiểm tra trực tiếp trong Art/UI/Items/ hoặc PixelMyth/UI/Icons/equip/
            Sprite eqSpr = GetEquipIcon(cleanKey, 24);
            if (eqSpr != null) return eqSpr;
            Sprite uiSpr = LoadSprite($"Art/UI/Items/{cleanKey}.png", 32);
            if (uiSpr != null) return uiSpr;

            // 2. Tra cứu theo từ khóa (bao gồm toàn bộ mã vật phẩm QualifiedItemRegistry)
            return cleanKey switch
            {
                "item_wood_plank" => GetIconWoodPlank(),
                "item_corn_feed" or "item_mutant_corn" or "CamThuong" or "CamTangTrong" or "CamThaoDuoc" or "CamTinhLuyen"
                    => LoadSprite("Art/UI/Items/item_corn_feed.png", 32) ?? LoadSprite("Art/UI/Tools/tool_cam_hat.png", 32),
                "ThaoDuocKho" or "BangGac"
                    => LoadSprite("Art/PixelMyth/UI/Icons/quick/bang_gac_24.png", 32) ?? LoadSprite("Art/PixelMyth/UI/Icons/equip/bang_gac_24.png", 32),
                "BinhHoiLuc" or "ThuocHoHap" or "ThuocTieuHoa" or "ThuocKySinh" or "ThuocHaNhiet" or "ThuocAnThan" or "VacXinTongHop"
                    => LoadSprite("Art/PixelMyth/UI/Icons/quick/binh_hoi_luc_24.png", 32) ?? LoadSprite("Art/PixelMyth/UI/Icons/equip/binh_hoi_luc_24.png", 32),
                "LuuKhoi" => LoadSprite("Art/PixelMyth/UI/Icons/quick/luu_khoi_24.png", 32),
                "BomKeo" => LoadSprite("Art/PixelMyth/UI/Icons/quick/bom_keo_24.png", 32),
                "PhaoSang" => LoadSprite("Art/PixelMyth/UI/Icons/quick/phao_sang_24.png", 32),
                "TinhNoDan" => LoadSprite("Art/PixelMyth/UI/Icons/quick/tinh_no_dan_24.png", 32),
                "item_rusty_nail" or "TenThep" => LoadSprite("Art/UI/Items/item_rusty_nail.png", 32),
                "item_broken_post" => LoadSprite("Art/UI/Items/item_broken_post.png", 32),
                "item_rat_tail" => LoadSprite("Art/UI/Items/item_rat_tail.png", 32),
                "item_fertilizer" or "PhanHeoTho" or "PhanHuuCo" or "VoiBot" => LoadSprite("Art/UI/Items/item_fertilizer.png", 32),
                "item_alloy_scrap" or "item_iron_axe_head" or "ThepCuongLuc" => LoadSprite("Art/UI/Items/item_alloy_scrap.png", 32),
                "item_quantum_fiber" => LoadSprite("Art/UI/Items/item_quantum_fiber.png", 32),
                "item_plasma_core" or "item_quantum_chip" or "TinhThach" or "XangSinhHoc" => LoadSprite("Art/UI/Items/item_plasma_core.png", 32),
                "Paper_Blue" or "Paper_Yellow" or "Paper_Purple" or "Paper_Red" or "MauGenTho" or "AnTinGiaToc24" or "ManhGiayKienHang"
                    => LoadSprite("Art/PixelMyth/UI/Icons/misc/tinh_bao_24.png", 32) ?? GetBlueprintSprite(),
                "gay_sat" or "dao_bau" => LoadSprite("Art/PixelMyth/UI/Icons/weapons/wpn_dao_rua_24.png", 32),
                "thuong_tam_vong" => LoadSprite("Art/PixelMyth/UI/Icons/weapons/wpn_giao_moc_24.png", 32),
                "bua_ta" => LoadSprite("Art/PixelMyth/UI/Icons/weapons/wpn_bua_ta_24.png", 32),
                "ao_vai_day" => LoadSprite("Art/PixelMyth/UI/Icons/equip/armor_ao_chi_24.png", 32),
                "giap_da_heo" => LoadSprite("Art/PixelMyth/UI/Icons/equip/armor_giap_da_thuoc_24.png", 32),
                "vong_co_bac" or "LaBanTanVu" or "BuaHoiHuong" => LoadSprite("Art/PixelMyth/UI/Icons/equip/brc_vong_huyet_nha_24.png", 32),
                "item_rope" => LoadSprite("Art/UI/Items/item_rope.png", 32),
                "item_gold_pouch" or "icon_coin_gold" => GetIconGold(),
                _ => GetIconWoodPlank()
            };
        }"""
            if old_loot in utxt:
                utxt = utxt.replace(old_loot, new_loot, 1)
            elif old_loot.replace("\n", "\r\n") in utxt:
                utxt = utxt.replace(old_loot.replace("\n", "\r\n"), new_loot.replace("\n", "\r\n"), 1)
            utxt = utxt.replace(
                'public static Sprite GetBanditOutpostSprite() => LoadSprite("Art/Sprites/Environment/Buildings/building_storage_lvl1.png", 32);',
                'public static Sprite GetBanditOutpostSprite() => LoadBuildingRowZero("nha_kho") ?? LoadSprite("Art/PixelMyth/UI/Icons/places/kho_24.png", 32) ?? LoadSprite("Art/Sprites/Environment/Buildings/building_storage_lvl1.png", 32) ?? GetBarnSprite();',
            )
            usl.write_text(utxt, encoding="utf-8")

    ui_dir = ROOT / "Assets" / "Scripts" / "UI"
    cleaned_files = []
    for cs_path in sorted(ui_dir.rglob("*.cs")):
        text = cs_path.read_text(encoding="utf-8")
        orig = text
        for k, v in EMOJI_REPLACEMENTS.items():
            if k in text:
                text = text.replace(k, v)
        # Also strip any remaining astral plane emojis (U+1F000..U+1FFFF) or BMP dingbats/misc symbols (U+2600..U+27BF)
        new_chars = []
        changed_astral = False
        for ch in text:
            code = ord(ch)
            if (0x1F000 <= code <= 0x1FFFF) or (0x2600 <= code <= 0x27BF):
                changed_astral = True
                continue
            new_chars.append(ch)
        if changed_astral:
            text = "".join(new_chars)
        # Guard against empty string Contains("") or == "" left behind when stripping emojis in UISpriteLoader
        text = text.replace(' || raw.Contains("")', "")
        text = text.replace('raw.Contains("") || ', "")
        text = text.replace(' || key == ""', "")
        text = text.replace('key == "" || ', "")
        if text != orig:
            cs_path.write_text(text, encoding="utf-8")
            cleaned_files.append(cs_path.relative_to(ROOT).as_posix())
    print(f"[Emoji Audit] Cleaned {len(cleaned_files)} UI C# files: {cleaned_files}")


def ground_sprite_png(png_path: Path):
    """
    Ensure a building or decor sprite has a grounded contact shadow ellipse/band
    underneath its lowest opaque pixels so it never looks floating on terrain tiles.
    """
    im = Image.open(png_path).convert("RGBA")
    w, h = im.size
    px = im.load()

    # Find bounding box of opaque pixels (alpha >= 48)
    min_x, max_x = w, -1
    max_y = -1
    for y in range(h):
        for x in range(w):
            if px[x, y][3] >= 48:
                if x < min_x:
                    min_x = x
                if x > max_x:
                    max_x = x
                if y > max_y:
                    max_y = y

    if max_x < min_x or max_y < 0:
        return False

    # Add a subtle grounded contact shadow right around the base (max_y - 3 .. min(h-1, max_y + 2))
    span_w = max_x - min_x + 1
    if span_w < 8:
        return False

    cx = 0.5 * (min_x + max_x)
    rx = max(4.0, 0.48 * span_w)
    by = min(h - 2, max_y)
    ry = max(2.0, min(5.0, h * 0.04))

    modified = False
    for y in range(max(0, int(by - ry)), min(h, int(by + ry + 2))):
        dy = (y - by) / ry
        for x in range(max(0, int(cx - rx - 1)), min(w, int(cx + rx + 2))):
            dx = (x - cx) / rx
            d2 = dx * dx + dy * dy
            if d2 <= 1.0:
                r, g, b, a = px[x, y]
                shadow_a = int(115 * (1.0 - d2 * 0.75))
                if a == 0:
                    px[x, y] = (14, 18, 12, shadow_a)
                    modified = True
                elif a < 180 and y >= max_y - 1:
                    # Darken semi-transparent fringe at the base
                    na = max(a, shadow_a)
                    px[x, y] = (int(r * 0.7), int(g * 0.7), int(b * 0.7), na)
                    modified = True

    if modified:
        im.save(png_path)
    return modified


def ground_all_buildings_and_decor():
    b_dir = ROOT / "Assets" / "Art" / "PixelMyth" / "Buildings"
    d_dir = ROOT / "Assets" / "Art" / "PixelMyth" / "Decor"
    count = 0
    for folder in [b_dir, d_dir]:
        if not folder.exists():
            continue
        for png in sorted(folder.rglob("*.png")):
            if ground_sprite_png(png):
                count += 1
            # Copy to from_argon
            rel = png.relative_to(ROOT)
            dst = FROM_ARGON / rel
            dst.parent.mkdir(parents=True, exist_ok=True)
            shutil.copy2(png, dst)
    print(f"[Grounding] Grounded & synced {count} building/decor sprites.")


def sync_code_to_from_argon():
    ui_src = ROOT / "Assets" / "Scripts" / "UI"
    ui_dst = FROM_ARGON / "Assets" / "Scripts" / "UI"
    count = 0
    for cs in sorted(ui_src.rglob("*.cs")):
        rel = cs.relative_to(ROOT)
        dst = FROM_ARGON / rel
        dst.parent.mkdir(parents=True, exist_ok=True)
        shutil.copy2(cs, dst)
        count += 1

    # Also sync WeaponSocketController.cs
    wpn_src = ROOT / "Assets" / "Scripts" / "Controllers" / "WeaponSocketController.cs"
    if wpn_src.exists():
        wpn_dst = FROM_ARGON / "Assets" / "Scripts" / "Controllers" / "WeaponSocketController.cs"
        wpn_dst.parent.mkdir(parents=True, exist_ok=True)
        shutil.copy2(wpn_src, wpn_dst)
        count += 1

    print(f"[Sync] Synced {count} C# files to from_argon.")

    # Verify all Kit and Icon sprites referenced in Assets/Scripts/UI exist in Assets/Art/PixelMyth and from_argon
    import re
    kit_names = set()
    icon_calls = set()
    for cs in sorted(ui_src.rglob("*.cs")):
        t = cs.read_text(encoding="utf-8")
        for m in re.finditer(r'MythUi\.Kit\(\s*"([^"]+)"\s*\)', t):
            kit_names.add(m.group(1))
        for m in re.finditer(r'MythUi\.Icon\(\s*"([^"]+)"\s*,\s*"([^"]+)"\s*,\s*(\d+)\s*\)', t):
            icon_calls.add((m.group(1), m.group(2), int(m.group(3))))

    missing = []
    for k in sorted(kit_names):
        rel = Path("Assets/Art/PixelMyth/UI/Kit") / f"{k}.png"
        src_p = ROOT / rel
        dst_p = FROM_ARGON / rel
        if src_p.exists() and not dst_p.exists():
            dst_p.parent.mkdir(parents=True, exist_ok=True)
            shutil.copy2(src_p, dst_p)
        if not src_p.exists():
            missing.append(str(rel))

    for g, n, sz in sorted(icon_calls):
        rel = Path("Assets/Art/PixelMyth/UI/Icons") / g / f"{n}_{sz}.png"
        src_p = ROOT / rel
        dst_p = FROM_ARGON / rel
        if src_p.exists() and not dst_p.exists():
            dst_p.parent.mkdir(parents=True, exist_ok=True)
            shutil.copy2(src_p, dst_p)
        if not src_p.exists():
            missing.append(str(rel))

    print(f"[Sprite Audit] Verified {len(kit_names)} Kit stems & {len(icon_calls)} Icon calls. Missing: {missing}")


if __name__ == "__main__":
    frames_tmp = FROM_ARGON / "build24_frames"
    if frames_tmp.exists():
        shutil.rmtree(frames_tmp, ignore_errors=True)
        print("[Clean] Removed temporary build24_frames from from_argon.")
    clean_ui_emojis()
    ground_all_buildings_and_decor()
    sync_code_to_from_argon()
