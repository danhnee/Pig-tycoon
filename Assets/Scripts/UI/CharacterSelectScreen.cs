using PigTycoon.Core;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace PigTycoon.Presentation
{
    /// <summary>
    /// HANDBOOK P1.02 / P1.21 & ADR-0027: 2.5D Character Selection Showcase for a NEW game (An vs Khoa).
    /// Reads <see cref="NarrativeContentStart.Card"/> (story bible 2.7, GDD 8.1) and displays each protagonist's
    /// 2.5D Bust Portrait, live animated 4-direction 32-PPU turnaround sprite, OpeningLine, Tagline, Blurb,
    /// TraitName, TraitText, and Catchphrase.
    /// </summary>
    public sealed class CharacterSelectScreen : MonoBehaviour
    {
        private static CharacterSelectScreen instance;
        private float previousTimeScale = 1f;

        private sealed class TurnaroundPreview
        {
            public Image SpriteImg;
            public string CharId;
            public Sprite[,] Grid; // [dir, frame]
        }

        private readonly TurnaroundPreview[] previews = new TurnaroundPreview[2];
        private float animTimer;
        private int currentDir;
        private int currentFrame;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Install()
        {
            SceneManager.sceneLoaded += (_, __) => TryShow();
            TryShow();
        }

        private static void TryShow()
        {
            var controller = MobileGameController.Instance;
            if (instance != null || controller == null || controller.Engine == null || !controller.NeedsCharacterSelect) return;
            var go = new GameObject("CharacterSelectScreen");
            instance = go.AddComponent<CharacterSelectScreen>();
            instance.Build();
        }

        private void Build()
        {
            previousTimeScale = Time.timeScale > 0f ? Time.timeScale : 1f;
            Time.timeScale = 0f;

            var canvas = gameObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 5000;
            var scaler = gameObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1280f, 720f);
            scaler.matchWidthOrHeight = 0.5f;
            gameObject.AddComponent<GraphicRaycaster>();
            if (FindFirstObjectByType<EventSystem>() == null)
                new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));

            var backdrop = new GameObject("Backdrop", typeof(RectTransform), typeof(Image));
            backdrop.transform.SetParent(transform, false);
            var brt = (RectTransform)backdrop.transform;
            brt.anchorMin = Vector2.zero;
            brt.anchorMax = Vector2.one;
            brt.offsetMin = brt.offsetMax = Vector2.zero;
            var bgImg = backdrop.GetComponent<Image>();
            var bgSpr = UISpriteLoader.GetBackdrop("bg_trai_chang_vang");
            if (bgSpr != null)
            {
                bgImg.sprite = bgSpr;
                bgImg.color = new Color(0.28f, 0.28f, 0.34f, 0.96f);
            }
            else
            {
                bgImg.color = new Color(0.03f, 0.04f, 0.07f, 0.92f);
            }

            var panel = DayMarketPopup.BuildStyledPopupFrame(transform, "CharacterSelectPanel", new Vector2(1080f, 628f),
                out _, out var header, out var footer);

            var title = DayMarketPopup.Text(header, Vector2.zero, new Vector2(960f, 38f), 22, TextAlignmentOptions.Center);
            title.text = "<b>CHỌN NHÂN VẬT KHỞI ĐẦU — MÙA 1 \"SÁU VỚI MỘT\"</b>";
            title.color = new Color(1f, 0.94f, 0.68f);

            var hint = DayMarketPopup.Text(footer, Vector2.zero, new Vector2(980f, 32f), 14, TextAlignmentOptions.Center);
            hint.text = "Cả hai đi qua cùng một câu chuyện với hai góc nhìn & đối thoại riêng. Bấm <b>[1]</b> chọn An hoặc <b>[2]</b> chọn Khoa.";
            hint.textWrappingMode = TextWrappingModes.Normal;

            previews[0] = BuildHeroCard(panel.transform, new Vector2(-252f, -4f), PlayerId.An);
            previews[1] = BuildHeroCard(panel.transform, new Vector2(252f, -4f), PlayerId.Khoa);
        }

        private TurnaroundPreview BuildHeroCard(Transform parent, Vector2 pos, PlayerId id)
        {
            ProtagonistCard cardData = NarrativeContentStart.Card(id);
            bool isAn = id == PlayerId.An;
            string charKey = isAn ? "an" : "khoa";

            var card = DayMarketPopup.MakeSlotCard(parent, pos, new Vector2(476f, 476f));

            // Left: 2.5D High-Craft Bust Portrait Frame
            var portraitBox = DayMarketPopup.MakeSlotCard(card.transform, new Vector2(-122f, 108f), new Vector2(184f, 218f));
            var pbImg = portraitBox.GetComponent<Image>();
            if (pbImg != null)
            {
                pbImg.sprite = UISpriteLoader.GetFrameSlotSelected();
                pbImg.type = Image.Type.Sliced;
                pbImg.color = new Color(0.20f, 0.15f, 0.11f, 0.98f);
            }
            var portIcon = DayMarketPopup.MakePortraitIcon(portraitBox.transform, Vector2.zero, new Vector2(168f, 202f));
            portIcon.sprite = UISpriteLoader.GetPortrait(charKey, cardData.Portrait);
            portIcon.preserveAspect = true;
            portIcon.enabled = portIcon.sprite != null;

            // Right top: Live 32-PPU 4-Direction Animated Turnaround Stage
            var stageBox = DayMarketPopup.MakeSlotCard(card.transform, new Vector2(106f, 140f), new Vector2(220f, 152f));
            var stImg = stageBox.GetComponent<Image>();
            if (stImg != null)
            {
                stImg.sprite = UISpriteLoader.GetFrameSlotNormal();
                stImg.type = Image.Type.Sliced;
                stImg.color = new Color(0.14f, 0.18f, 0.14f, 0.96f);
            }
            var stageLbl = DayMarketPopup.Text(stageBox.transform, new Vector2(0f, 58f), new Vector2(204f, 20f), 11.5f, TextAlignmentOptions.Center);
            stageLbl.text = $"<color=#d8c49a>{cardData.Catchphrase} · 32 PPU 4 hướng</color>";

            var sprIcon = DayMarketPopup.MakePortraitIcon(stageBox.transform, new Vector2(0f, -6f), new Vector2(92f, 108f));
            var grid = LoadTurnaroundGrid(charKey);
            if (grid[0, 0] != null) sprIcon.sprite = grid[0, 0];

            // Right middle: OpeningLine & Stats
            var statBox = DayMarketPopup.MakeSlotCard(card.transform, new Vector2(106f, 20f), new Vector2(220f, 74f));
            var statsTxt = DayMarketPopup.Text(statBox.transform, Vector2.zero, new Vector2(204f, 66f), 11.5f, TextAlignmentOptions.Left);
            statsTxt.textWrappingMode = TextWrappingModes.Normal;
            statsTxt.text = $"<color=#FDE047><i>“{cardData.OpeningLine}”</i></color>\n<color=#86EFAC>{NewGameSetup.StatLine(id)}</color>";

            // Name & Title
            var nameText = DayMarketPopup.Text(card.transform, new Vector2(0f, -34f), new Vector2(444f, 26f), 19, TextAlignmentOptions.Center);
            nameText.text = $"<b>{cardData.Title}</b>  <size=13.5><color=#d8c49a>— {cardData.Tagline}</color></size>";
            nameText.color = new Color(1f, 0.92f, 0.64f);

            // Trait Name & Trait Text
            var trait = DayMarketPopup.Text(card.transform, new Vector2(0f, -68f), new Vector2(444f, 36f), 12.5f, TextAlignmentOptions.Center);
            trait.textWrappingMode = TextWrappingModes.Normal;
            trait.text = $"Thiên phú <color=#86EFAC><b>{cardData.TraitName}</b></color>: {cardData.TraitText}";

            // Story Blurb
            var lore = DayMarketPopup.Text(card.transform, new Vector2(0f, -122f), new Vector2(440f, 58f), 12.5f, TextAlignmentOptions.Center);
            lore.textWrappingMode = TextWrappingModes.Normal;
            lore.color = new Color(0.90f, 0.86f, 0.78f);
            lore.text = $"<i>{cardData.Blurb}</i>";

            var button = DayMarketPopup.Button(card.transform, new Vector2(0f, -190f), new Vector2(268f, 44f), out var label);
            label.text = isAn ? "Khởi hành cùng An  [Phím 1]" : "Khởi hành cùng Khoa  [Phím 2]";
            label.fontSize = 16f;
            button.onClick.AddListener(() => Choose(id));

            return new TurnaroundPreview { SpriteImg = sprIcon, CharId = charKey, Grid = grid };
        }

        private static Sprite[,] LoadTurnaroundGrid(string charKey)
        {
            var grid = new Sprite[4, 4];
            Sprite sheet = UISpriteLoader.LoadSprite($"Art/PixelMyth/Characters/{charKey}/{charKey}_idle.png", 32);
            if (sheet == null || sheet.texture == null) return grid;
            var tex = sheet.texture;
            int cw = tex.width / 4;
            int ch = tex.height / 4;
            if (cw <= 0 || ch <= 0) return grid;
            for (int r = 0; r < 4; r++)
            {
                int texY = (3 - r) * ch;
                for (int c = 0; c < 4; c++)
                {
                    grid[r, c] = Sprite.Create(tex, new Rect(c * cw, texY, cw, ch), new Vector2(0.5f, 0.5f), 32f);
                }
            }
            return grid;
        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.Alpha1) || Input.GetKeyDown(KeyCode.Keypad1)) { Choose(PlayerId.An); return; }
            if (Input.GetKeyDown(KeyCode.Alpha2) || Input.GetKeyDown(KeyCode.Keypad2)) { Choose(PlayerId.Khoa); return; }

            animTimer += Time.unscaledDeltaTime;
            if (animTimer >= 0.22f)
            {
                animTimer = 0f;
                currentFrame = (currentFrame + 1) % 4;
                if (currentFrame == 0) currentDir = (currentDir + 1) % 4;
                for (int i = 0; i < previews.Length; i++)
                {
                    var p = previews[i];
                    if (p?.SpriteImg != null && p.Grid[currentDir, currentFrame] != null)
                        p.SpriteImg.sprite = p.Grid[currentDir, currentFrame];
                }
            }
        }

        private void Choose(PlayerId id)
        {
            var controller = MobileGameController.Instance;
            if (controller == null || !controller.NeedsCharacterSelect) { Close(); return; }
            Close();
            controller.CompleteCharacterSelect(id);
            foreach (var roster in FindObjectsByType<PixelWorldPlayerRoster>(FindObjectsSortMode.None))
                roster.ApplyChosenIdentity(id);
            Debug.Log($"[CharacterSelect] new game as {id}");
            PlayerInteractionController.Instance?.ShowFeedback($"Bắt đầu ván mới với {(id == PlayerId.Khoa ? "Khoa" : "An")}.");
        }

        private void Close()
        {
            Time.timeScale = previousTimeScale;
            if (instance == this) instance = null;
            Destroy(gameObject);
        }

        private void OnDestroy()
        {
            if (instance == this)
            {
                instance = null;
                Time.timeScale = previousTimeScale;
            }
        }
    }
}
