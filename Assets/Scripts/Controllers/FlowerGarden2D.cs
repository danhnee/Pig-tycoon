using System;
using System.Collections.Generic;
using UnityEngine;
using PigTycoon.Core;

namespace PigTycoon.Presentation
{
    public enum RoyalFlowerType
    {
        JulietRose,      // Hoa Hồng Juliet - Nữ hoàng hoa triệu đô (4 ngày)
        LilyOfTheValley, // Hoa Linh Lan Chuông - Thuần khiết & May mắn (3 ngày)
        RoyalTulip       // Hoa Tulip Hoàng Gia - Kiệt tác Hà Lan kiêu sa (4 ngày)
    }

    public enum FlowerGrowthStage
    {
        HatGiong = 0,    // Ngày 1: Hạt giống nảy mầm
        MamNon = 1,      // Ngày 2: Chồi non mơn mởn
        KetNu = 2,       // Ngày 3: Búp nụ hé sắc
        NoRo = 3         // Ngày 3-4: Nở rộ tuyệt đẹp
    }

    /// <summary>
    /// Đại diện cho một khóm hoa hoàng gia trên luống đất vườn.
    /// </summary>
    [SelectionBase]
    public class FlowerPlotView : MonoBehaviour
    {
        [Header("Flower Profile")]
        public RoyalFlowerType FlowerType;
        public string FlowerName = "Hoa Hoàng Gia";
        public int DaysToMature = 4;
        public int PlantedDay = 1;

        [Header("Current State")]
        public FlowerGrowthStage CurrentStage = FlowerGrowthStage.HatGiong;
        public int CurrentDayAge = 0;
        public bool IsFullyBloomed => CurrentStage == FlowerGrowthStage.NoRo;

        [Header("Renderers")]
        public SpriteRenderer PlantRenderer;

        private float checkTimer = 0f;

        private void Start()
        {
            if (PlantedDay <= 0)
            {
                PlantedDay = GetCurrentGameDay();
            }
            UpdateGrowthStage();
        }

        private void Update()
        {
            checkTimer += Time.deltaTime;
            if (checkTimer >= 1.0f)
            {
                checkTimer = 0f;
                UpdateGrowthStage();
            }
        }

        public int GetCurrentGameDay()
        {
            var clock = MobileGameController.Instance?.Engine?.Clock;
            return clock != null ? clock.CurrentDay : 1;
        }

        public void UpdateGrowthStage()
        {
            int curDay = GetCurrentGameDay();
            CurrentDayAge = Mathf.Max(0, curDay - PlantedDay);

            FlowerGrowthStage newStage;
            if (DaysToMature <= 3)
            {
                if (CurrentDayAge >= 3) newStage = FlowerGrowthStage.NoRo;
                else if (CurrentDayAge == 2) newStage = FlowerGrowthStage.KetNu;
                else if (CurrentDayAge == 1) newStage = FlowerGrowthStage.MamNon;
                else newStage = FlowerGrowthStage.HatGiong;
            }
            else
            {
                if (CurrentDayAge >= 4) newStage = FlowerGrowthStage.NoRo;
                else if (CurrentDayAge >= 2) newStage = FlowerGrowthStage.KetNu;
                else if (CurrentDayAge == 1) newStage = FlowerGrowthStage.MamNon;
                else newStage = FlowerGrowthStage.HatGiong;
            }

            CurrentStage = newStage;
            ApplySpriteVisual();
        }

        public void ReplantSeed()
        {
            PlantedDay = GetCurrentGameDay();
            UpdateGrowthStage();
        }

        public void ForceBloom()
        {
            PlantedDay = GetCurrentGameDay() - DaysToMature;
            UpdateGrowthStage();
        }

        public void ApplySpriteVisual()
        {
            if (PlantRenderer == null) return;
            PlantRenderer.sprite = FlowerGardenBuilder.GetFlowerSprite(FlowerType, CurrentStage);
            PlantRenderer.sortingOrder = Mathf.RoundToInt(-(transform.position.y - 0.15f) * 100);
        }

        public void Interact()
        {
            var player = PlayerInteractionController.Instance;
            var engine = MobileGameController.Instance?.Engine;

            if (IsFullyBloomed)
            {
                if (engine?.Character?.Stamina != null)
                {
                    engine.Character.Stamina.CurrentStamina = engine.Character.Stamina.MaxStamina;
                }
                player?.ShowFeedback($"✨ Khoa ngắm nhìn {FlowerName} nở rộ ngào ngạt! Hồi phục 100% Thể Lực!");
            }
            else
            {
                int remaining = Mathf.Max(1, DaysToMature - CurrentDayAge);
                player?.ShowFeedback($"🌱 {FlowerName} đang lớn khỏe mạnh! Còn khoảng {remaining} ngày nữa sẽ nở rộ.");
            }
        }
    }

    /// <summary>
    /// Xây dựng vườn hoa hoàng gia: 3 hàng hoa trên 3 luống đất tơi xốp màu mỡ kéo dài từ đường chính đến hàng rào.
    /// </summary>
    public static class FlowerGardenBuilder
    {
        public const string RootGardenName = "Royal_Flower_Garden";

        private static Dictionary<string, Sprite> spriteCache = new Dictionary<string, Sprite>();

        public static void BuildIfMissing()
        {
            RemoveOldFlowerBush();

            var existingGarden = GameObject.Find(RootGardenName);
            if (existingGarden != null)
            {
                var plots = existingGarden.GetComponentsInChildren<FlowerPlotView>();
                foreach (var p in plots) p.UpdateGrowthStage();
                return;
            }

            BuildGarden();
        }

        public static void RemoveOldFlowerBush()
        {
            var allObjects = UnityEngine.Object.FindObjectsByType<GameObject>(FindObjectsSortMode.None);
            foreach (var go in allObjects)
            {
                if (go == null) continue;
                if (go.name.Contains("Flower_Bush"))
                {
                    Vector3 pos = go.transform.position;
                    if (pos.x >= 0.5f && pos.x <= 8.0f && pos.y >= -1.5f && pos.y <= 6.0f)
                    {
                        if (Application.isPlaying) UnityEngine.Object.Destroy(go);
                        else UnityEngine.Object.DestroyImmediate(go);
                    }
                }
            }
        }

        public static GameObject BuildGarden()
        {
            RemoveOldFlowerBush();

            var oldRoot = GameObject.Find(RootGardenName);
            if (oldRoot != null)
            {
                if (Application.isPlaying) UnityEngine.Object.Destroy(oldRoot);
                else UnityEngine.Object.DestroyImmediate(oldRoot);
            }

            var root = new GameObject(RootGardenName);
            root.transform.position = Vector3.zero;

            // =========================================================================
            // 3 LUỐNG ĐẤT TRỒNG HOA (TILLED GARDEN SOIL BEDS) CHẠY DÀI TỪ ĐƯỜNG ĐẾN HÀNG RÀO:
            // Tọa độ ngang: X từ 1.3f đến 19.5f (Dài 18.2m, phủ kín đất trống đến rào)
            // Tọa độ dọc:
            //   - Hàng 1 (Y = 4.2f): 🌹 Luống đất Hoa Hồng Juliet (14 khóm) - 4 ngày
            //   - Hàng 2 (Y = 2.5f): 🔔 Luống đất Hoa Linh Lan Chuông (14 khóm) - 3 ngày
            //   - Hàng 3 (Y = 0.8f): 🌷 Luống đất Hoa Tulip Hoàng Gia (14 khóm) - 4 ngày
            // =========================================================================

            float startX = 1.8f;
            float endX = 19.0f;
            int plantsPerRow = 14;

            float bedStartX = 1.3f;
            float bedEndX = 19.5f;
            float bedWidth = bedEndX - bedStartX;
            float bedCenterX = (bedStartX + bedEndX) * 0.5f;

            // HÀNG 1: HOA HỒNG JULIET
            BuildFullRowWithSoilBed(root.transform, RoyalFlowerType.JulietRose, "Hoa Hồng Juliet", 4, 4.2f, bedCenterX, bedWidth, startX, endX, plantsPerRow);

            // HÀNG 2: HOA LINH LAN CHUÔNG
            BuildFullRowWithSoilBed(root.transform, RoyalFlowerType.LilyOfTheValley, "Hoa Linh Lan Chuông", 3, 2.5f, bedCenterX, bedWidth, startX, endX, plantsPerRow);

            // HÀNG 3: HOA TULIP HOÀNG GIA
            BuildFullRowWithSoilBed(root.transform, RoyalFlowerType.RoyalTulip, "Hoa Tulip Hoàng Gia", 4, 0.8f, bedCenterX, bedWidth, startX, endX, plantsPerRow);

            return root;
        }

        private static void BuildFullRowWithSoilBed(
            Transform parent,
            RoyalFlowerType type,
            string flowerName,
            int matureDays,
            float rowY,
            float bedCenterX,
            float bedWidth,
            float startX,
            float endX,
            int count)
        {
            var rowObj = new GameObject($"Row_{type}");
            rowObj.transform.SetParent(parent, false);

            // 1. Luống đất tơi xốp màu mỡ (Tilled Soil Bed) chạy dài suốt cả hàng
            var soilObj = new GameObject($"SoilBed_{type}");
            soilObj.transform.SetParent(rowObj.transform, false);
            soilObj.transform.position = new Vector3(bedCenterX, rowY - 0.05f, 0f);

            var soilSr = soilObj.AddComponent<SpriteRenderer>();
            soilSr.sprite = GetTilledSoilBedSprite();
            soilSr.drawMode = SpriteDrawMode.Tiled;
            soilSr.tileMode = SpriteTileMode.Continuous;
            soilSr.size = new Vector2(bedWidth, 1.0f);
            soilSr.sortingOrder = -9500; // Nằm trên cỏ nhưng dưới hoa

            // 2. Các khóm hoa được trồng thẳng hàng trên luống đất
            float step = (endX - startX) / (count - 1);
            for (int i = 0; i < count; i++)
            {
                float px = startX + i * step;
                var plantObj = new GameObject($"Flower_{type}_{i + 1}");
                plantObj.transform.SetParent(rowObj.transform, false);
                plantObj.transform.position = new Vector3(px, rowY, 0f);
                plantObj.transform.localScale = new Vector3(1.15f, 1.15f, 1f);

                var sr = plantObj.AddComponent<SpriteRenderer>();

                var col = plantObj.AddComponent<CircleCollider2D>();
                col.radius = 0.5f;
                col.isTrigger = true;

                var plot = plantObj.AddComponent<FlowerPlotView>();
                plot.FlowerType = type;
                plot.FlowerName = flowerName;
                plot.DaysToMature = matureDays;
                plot.PlantedDay = 1;
                plot.PlantRenderer = sr;

                plot.UpdateGrowthStage();
            }
        }

        // =========================================================================
        // SPRITE LUỐNG ĐẤT TƠI XỐP MÀU MỠ (TILLED SOIL BED SPRITE)
        // =========================================================================

        public static Sprite GetTilledSoilBedSprite()
        {
            string key = "tilled_garden_soil_bed_v2";
            if (spriteCache.TryGetValue(key, out var cached) && cached != null)
                return cached;

            int w = 32;
            int h = 32;
            var pixels = new Color32[w * h];

            Color32 soilBase = new Color32(65, 42, 25, 255);       // Đất mùn nâu sẫm phì nhiêu
            Color32 soilDark = new Color32(42, 25, 14, 255);       // Rãnh luống sâu
            Color32 soilRidge = new Color32(84, 56, 35, 255);      // Gờ đất tơi xốp bắt sáng
            Color32 soilMoist = new Color32(52, 33, 19, 255);      // Đất ẩm tơi
            Color32 edgeGrassBlend = new Color32(48, 68, 28, 220); // Viền cỏ tự nhiên

            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    Color32 c = soilBase;

                    // Đường rãnh cày xới ngang đều đặn (furrows)
                    int furrowY = y % 8;
                    if (furrowY == 0 || furrowY == 1) c = soilDark;
                    else if (furrowY == 2 || furrowY == 3) c = soilRidge;
                    else if (furrowY == 6) c = soilMoist;

                    // Đốm hạt đất tơi xốp tự nhiên
                    if ((x * 7 + y * 13) % 11 == 0) c = soilRidge;
                    if ((x * 13 + y * 17) % 17 == 0) c = soilDark;

                    // Viền trên và viền dưới chuyển tiếp mềm mại vào nền cỏ
                    if (y >= 29 || y <= 2)
                    {
                        if ((x * 5 + y * 3) % 3 == 0) c = edgeGrassBlend;
                    }

                    pixels[y * w + x] = c;
                }
            }

            var texture = new Texture2D(w, h, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Repeat
            };
            texture.SetPixels32(pixels);
            texture.Apply(false, true);

            var spr = Sprite.Create(texture, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f), 32f);
            spriteCache[key] = spr;
            return spr;
        }

        // =========================================================================
        // HỆ THỐNG SPRITE PIXEL ART HOA TỰ NHIÊN ĐẸP MẮT (PHONG CÁCH STARDEW VALLEY)
        // =========================================================================

        public static Sprite GetFlowerSprite(RoyalFlowerType type, FlowerGrowthStage stage)
        {
            string key = $"stardew_flower_{type}_{stage}_v2";
            if (spriteCache.TryGetValue(key, out var cached) && cached != null)
                return cached;

            Sprite created = null;
            switch (type)
            {
                case RoyalFlowerType.JulietRose:
                    created = CreateSprite(34, 38, (p, w, h) => DrawJulietRose(p, w, h, stage));
                    break;
                case RoyalFlowerType.LilyOfTheValley:
                    created = CreateSprite(34, 38, (p, w, h) => DrawLilyOfTheValley(p, w, h, stage));
                    break;
                case RoyalFlowerType.RoyalTulip:
                    created = CreateSprite(34, 38, (p, w, h) => DrawRoyalTulip(p, w, h, stage));
                    break;
            }

            if (created != null) spriteCache[key] = created;
            return created;
        }

        private static Sprite CreateSprite(int w, int h, Action<Color32[], int, int> draw)
        {
            var pixels = new Color32[w * h];
            for (int i = 0; i < pixels.Length; i++) pixels[i] = new Color32(0, 0, 0, 0);
            draw(pixels, w, h);

            var texture = new Texture2D(w, h, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Clamp
            };
            texture.SetPixels32(pixels);
            texture.Apply(false, true);

            return Sprite.Create(texture, new Rect(0, 0, w, h), new Vector2(0.5f, 0.12f), 32f);
        }

        // -------------------------------------------------------------------------
        // 1. HOA HỒNG JULIET (JULIET ROSE) - Sắc cam đào xoáy nhiều lớp quý phái
        // -------------------------------------------------------------------------
        private static void DrawJulietRose(Color32[] p, int w, int h, FlowerGrowthStage stage)
        {
            Color32 soilShadow = new Color32(28, 42, 20, 160);
            Color32 soilLoam = new Color32(65, 45, 28, 220);
            DrawEllipse(p, w, h, 17, 4, 14, 5, soilShadow);
            DrawEllipse(p, w, h, 17, 5, 11, 4, soilLoam);

            Color32 leafDark = new Color32(30, 85, 38, 255);
            Color32 leafMid = new Color32(48, 135, 58, 255);
            Color32 leafLight = new Color32(85, 185, 95, 255);

            Color32 peachDark = new Color32(185, 75, 55, 255);
            Color32 peachMid = new Color32(235, 125, 90, 255);
            Color32 peachLight = new Color32(252, 175, 140, 255);
            Color32 peachCream = new Color32(255, 225, 200, 255);

            if (stage == FlowerGrowthStage.HatGiong)
            {
                DrawRect(p, w, h, 16, 6, 2, 4, leafMid);
                DrawPoint(p, w, h, 15, 9, leafLight);
                DrawPoint(p, w, h, 18, 10, leafLight);
                return;
            }

            if (stage == FlowerGrowthStage.MamNon)
            {
                DrawLine(p, w, h, 17, 5, 17, 14, leafDark, 2);
                DrawEllipse(p, w, h, 13, 11, 6, 4, leafMid);
                DrawEllipse(p, w, h, 21, 12, 6, 4, leafLight);
                DrawPoint(p, w, h, 17, 15, leafLight);
                return;
            }

            if (stage == FlowerGrowthStage.KetNu)
            {
                DrawEllipse(p, w, h, 12, 10, 8, 6, leafDark);
                DrawEllipse(p, w, h, 22, 11, 8, 6, leafMid);
                DrawLine(p, w, h, 17, 5, 17, 17, leafDark, 2);

                DrawEllipse(p, w, h, 17, 20, 9, 9, peachDark);
                DrawEllipse(p, w, h, 17, 21, 6, 6, peachMid);
                DrawPoint(p, w, h, 17, 22, peachLight);
                DrawLine(p, w, h, 14, 17, 20, 17, leafMid, 1);
                return;
            }

            // NỞ RỘ: Khóm hoa hồng Juliet bung cánh tròn lộng lẫy
            DrawEllipse(p, w, h, 10, 11, 9, 7, leafDark);
            DrawEllipse(p, w, h, 24, 12, 9, 7, leafMid);
            DrawEllipse(p, w, h, 17, 9, 12, 6, leafDark);

            DrawEllipse(p, w, h, 17, 20, 17, 15, peachDark);
            DrawEllipse(p, w, h, 17, 20, 14, 12, peachMid);
            DrawEllipse(p, w, h, 17, 21, 10, 8, peachLight);
            DrawEllipse(p, w, h, 17, 22, 6, 4, peachCream);

            DrawLine(p, w, h, 15, 21, 19, 22, peachDark, 1);
            DrawPoint(p, w, h, 16, 23, peachMid);

            DrawEllipse(p, w, h, 9, 17, 6, 6, peachMid);
            DrawPoint(p, w, h, 9, 18, peachLight);

            DrawPoint(p, w, h, 11, 28, new Color32(255, 240, 180, 240));
            DrawPoint(p, w, h, 24, 27, new Color32(255, 240, 180, 240));
        }

        // -------------------------------------------------------------------------
        // 2. HOA LINH LAN CHUÔNG (LILY OF THE VALLEY) - Những quả chuông trắng tinh khôi
        // -------------------------------------------------------------------------
        private static void DrawLilyOfTheValley(Color32[] p, int w, int h, FlowerGrowthStage stage)
        {
            Color32 soilShadow = new Color32(28, 42, 20, 160);
            Color32 soilLoam = new Color32(65, 45, 28, 220);
            DrawEllipse(p, w, h, 17, 4, 14, 5, soilShadow);
            DrawEllipse(p, w, h, 17, 5, 11, 4, soilLoam);

            Color32 leafDark = new Color32(26, 78, 32, 255);
            Color32 leafMid = new Color32(45, 125, 52, 255);
            Color32 leafLight = new Color32(75, 175, 85, 255);

            Color32 bellShade = new Color32(185, 215, 210, 255);
            Color32 bellWhite = new Color32(255, 255, 255, 255);
            Color32 bellGlow = new Color32(240, 255, 245, 255);

            if (stage == FlowerGrowthStage.HatGiong)
            {
                DrawRect(p, w, h, 16, 6, 2, 4, leafMid);
                DrawPoint(p, w, h, 17, 9, leafLight);
                return;
            }

            if (stage == FlowerGrowthStage.MamNon)
            {
                DrawEllipse(p, w, h, 13, 12, 5, 10, leafDark);
                DrawEllipse(p, w, h, 21, 12, 5, 10, leafMid);
                return;
            }

            if (stage == FlowerGrowthStage.KetNu)
            {
                DrawEllipse(p, w, h, 11, 13, 6, 12, leafDark);
                DrawEllipse(p, w, h, 23, 13, 6, 12, leafLight);

                DrawLine(p, w, h, 16, 5, 18, 18, leafMid, 2);
                DrawLine(p, w, h, 18, 18, 22, 23, leafMid, 1);

                DrawEllipse(p, w, h, 22, 20, 4, 3, bellShade);
                DrawEllipse(p, w, h, 19, 16, 3, 3, bellShade);
                return;
            }

            // NỞ RỘ: Cành uốn cong với những chiếc chuông trắng thuần khiết
            DrawEllipse(p, w, h, 9, 14, 7, 15, leafDark);
            DrawEllipse(p, w, h, 25, 14, 7, 15, leafLight);

            DrawLine(p, w, h, 15, 5, 17, 16, leafDark, 2);
            DrawLine(p, w, h, 17, 16, 21, 24, leafMid, 1);
            DrawLine(p, w, h, 21, 24, 24, 27, leafMid, 1);

            DrawMiniBell(p, w, h, 23, 23, bellShade, bellWhite);
            DrawMiniBell(p, w, h, 20, 19, bellShade, bellWhite);
            DrawMiniBell(p, w, h, 17, 15, bellShade, bellWhite);
            DrawMiniBell(p, w, h, 25, 27, bellShade, bellGlow);

            DrawPoint(p, w, h, 13, 27, new Color32(230, 255, 245, 230));
        }

        private static void DrawMiniBell(Color32[] p, int w, int h, int cx, int cy, Color32 shade, Color32 white)
        {
            DrawEllipse(p, w, h, cx, cy, 6, 5, shade);
            DrawEllipse(p, w, h, cx, cy + 1, 4, 4, white);
            DrawPoint(p, w, h, cx - 2, cy - 2, shade);
            DrawPoint(p, w, h, cx + 2, cy - 2, shade);
        }

        // -------------------------------------------------------------------------
        // 3. HOA TULIP HOÀNG GIA (ROYAL TULIP) - Kiệt tác chén ngọc đỏ viền ánh kim
        // -------------------------------------------------------------------------
        private static void DrawRoyalTulip(Color32[] p, int w, int h, FlowerGrowthStage stage)
        {
            Color32 soilShadow = new Color32(28, 42, 20, 160);
            Color32 soilLoam = new Color32(65, 45, 28, 220);
            DrawEllipse(p, w, h, 17, 4, 14, 5, soilShadow);
            DrawEllipse(p, w, h, 17, 5, 11, 4, soilLoam);

            Color32 leafDark = new Color32(35, 95, 45, 255);
            Color32 leafMid = new Color32(55, 145, 65, 255);
            Color32 leafLight = new Color32(85, 190, 100, 255);

            Color32 tulipDark = new Color32(145, 15, 35, 255);
            Color32 tulipRed = new Color32(215, 30, 55, 255);
            Color32 tulipGold = new Color32(255, 205, 55, 255);
            Color32 tulipLight = new Color32(255, 235, 130, 255);

            if (stage == FlowerGrowthStage.HatGiong)
            {
                DrawRect(p, w, h, 16, 6, 2, 4, leafMid);
                DrawPoint(p, w, h, 17, 9, leafLight);
                return;
            }

            if (stage == FlowerGrowthStage.MamNon)
            {
                DrawLine(p, w, h, 17, 5, 17, 14, leafDark, 2);
                DrawEllipse(p, w, h, 12, 11, 5, 9, leafDark);
                DrawEllipse(p, w, h, 22, 11, 5, 9, leafMid);
                return;
            }

            if (stage == FlowerGrowthStage.KetNu)
            {
                DrawLine(p, w, h, 17, 5, 17, 18, leafDark, 2);
                DrawEllipse(p, w, h, 11, 13, 5, 12, leafDark);
                DrawEllipse(p, w, h, 23, 13, 5, 12, leafMid);

                DrawEllipse(p, w, h, 17, 21, 7, 10, tulipDark);
                DrawEllipse(p, w, h, 17, 22, 5, 8, tulipRed);
                DrawLine(p, w, h, 17, 19, 17, 25, tulipGold, 1);
                return;
            }

            // NỞ RỘ: Chén ngọc tulip nở kiêu hãnh
            DrawLine(p, w, h, 17, 5, 17, 17, leafDark, 2);
            DrawEllipse(p, w, h, 10, 13, 6, 13, leafDark);
            DrawEllipse(p, w, h, 24, 13, 6, 13, leafLight);

            DrawEllipse(p, w, h, 17, 22, 13, 13, tulipDark);
            DrawEllipse(p, w, h, 17, 22, 10, 11, tulipRed);

            DrawPoint(p, w, h, 13, 26, tulipGold);
            DrawPoint(p, w, h, 21, 26, tulipGold);
            DrawLine(p, w, h, 17, 18, 17, 25, tulipGold, 1);
            DrawPoint(p, w, h, 17, 26, tulipLight);

            DrawEllipse(p, w, h, 24, 18, 7, 7, tulipDark);
            DrawEllipse(p, w, h, 24, 18, 5, 5, tulipRed);
            DrawPoint(p, w, h, 24, 20, tulipGold);

            DrawPoint(p, w, h, 12, 28, tulipGold);
            DrawPoint(p, w, h, 27, 23, tulipGold);
        }

        // =========================================================================
        // HÀM VẼ CƠ BẢN
        // =========================================================================

        private static void DrawPoint(Color32[] p, int w, int h, int x, int y, Color32 color)
        {
            if (x >= 0 && x < w && y >= 0 && y < h)
            {
                p[y * w + x] = BlendColor(p[y * w + x], color);
            }
        }

        private static void DrawRect(Color32[] p, int w, int h, int rx, int ry, int rw, int rh, Color32 color)
        {
            int x0 = Mathf.Clamp(rx, 0, w);
            int y0 = Mathf.Clamp(ry, 0, h);
            int x1 = Mathf.Clamp(rx + rw, 0, w);
            int y1 = Mathf.Clamp(ry + rh, 0, h);

            for (int y = y0; y < y1; y++)
            {
                int row = y * w;
                for (int x = x0; x < x1; x++)
                {
                    p[row + x] = BlendColor(p[row + x], color);
                }
            }
        }

        private static void DrawEllipse(Color32[] p, int w, int h, int cx, int cy, int rx, int ry, Color32 color)
        {
            int x0 = Mathf.Clamp(cx - rx / 2, 0, w);
            int x1 = Mathf.Clamp(cx + rx / 2, 0, w);
            int y0 = Mathf.Clamp(cy - ry / 2, 0, h);
            int y1 = Mathf.Clamp(cy + ry / 2, 0, h);

            float a2 = Mathf.Max(1f, (rx * 0.5f) * (rx * 0.5f));
            float b2 = Mathf.Max(1f, (ry * 0.5f) * (ry * 0.5f));

            for (int y = y0; y < y1; y++)
            {
                float dy2 = (y - cy) * (y - cy);
                int row = y * w;
                for (int x = x0; x < x1; x++)
                {
                    float dx2 = (x - cx) * (x - cx);
                    if (dx2 / a2 + dy2 / b2 <= 1.0f)
                    {
                        p[row + x] = BlendColor(p[row + x], color);
                    }
                }
            }
        }

        private static void DrawLine(Color32[] p, int w, int h, int x0, int y0, int x1, int y1, Color32 color, int thickness = 1)
        {
            int dx = Math.Abs(x1 - x0);
            int dy = Math.Abs(y1 - y0);
            int sx = x0 < x1 ? 1 : -1;
            int sy = y0 < y1 ? 1 : -1;
            int err = dx - dy;

            while (true)
            {
                if (thickness <= 1)
                {
                    DrawPoint(p, w, h, x0, y0, color);
                }
                else
                {
                    int half = thickness / 2;
                    DrawRect(p, w, h, x0 - half, y0 - half, thickness, thickness, color);
                }

                if (x0 == x1 && y0 == y1) break;
                int e2 = 2 * err;
                if (e2 > -dy)
                {
                    err -= dy;
                    x0 += sx;
                }
                if (e2 < dx)
                {
                    err += dx;
                    y0 += sy;
                }
            }
        }

        private static Color32 BlendColor(Color32 dst, Color32 src)
        {
            if (src.a == 255) return src;
            if (src.a == 0) return dst;
            float a = src.a / 255f;
            float inv = 1f - a;
            byte r = (byte)(src.r * a + dst.r * inv);
            byte g = (byte)(src.g * a + dst.g * inv);
            byte b = (byte)(src.b * a + dst.b * inv);
            byte finalA = (byte)Mathf.Clamp(dst.a + src.a, 0, 255);
            return new Color32(r, g, b, finalA);
        }
    }
}
