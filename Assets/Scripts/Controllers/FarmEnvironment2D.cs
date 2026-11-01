using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace PigTycoon.Presentation
{
    public class FarmEnvironment2D : MonoBehaviour
    {
        public static FarmEnvironment2D Instance { get; private set; }

        [Header("Prairie Map & Pasture Bounds")]
        [Tooltip("RÃ¬a toÃ n bá»™ báº£n Ä‘á»“ tháº£o nguyÃªn (Camera follow bá»‹ giá»›i háº¡n táº¡i Ä‘Ã¢y)")]
        public Rect MapBounds = new Rect(-45f, -32f, 90f, 64f);

        [Tooltip("Khu vá»±c hÃ ng rÃ o chÄƒn tháº£ heo (Heo chá»‰ Ä‘i dáº¡o trong khu vá»±c nÃ y)")]
        public Rect PigPastureBounds = new Rect(-20f, -14f, 40f, 28f);

        public Rect FarmBounds
        {
            get => MapBounds;
            set => MapBounds = value;
        }

        [Header("Automation Settings")]
        [Tooltip("Tá»± Ä‘á»™ng Ã©p vá»‹ trÃ­ mÃ¡ng vá» 2 hÃ ng dá»c khi Play. Máº·c Ä‘á»‹nh táº¯t (false) Ä‘á»ƒ báº¡n tá»± do di chuyá»ƒn mÃ¡ng theo Ã½ thÃ­ch.")]
        public bool autoAlignTroughsOnPlay = false;

        [Header("Registered Infrastructure")]
        public List<PigAgentView> Pigs = new List<PigAgentView>();
        public List<Fence2DView> Fences = new List<Fence2DView>();
        public List<Feeder2DView> Feeders = new List<Feeder2DView>();
        public List<WaterTrough2DView> WaterTroughs = new List<WaterTrough2DView>();
        public List<MudPit2DView> MudPits = new List<MudPit2DView>();
        public List<Shelter2DView> Shelters = new List<Shelter2DView>();
        public Shelter2DView Shelter => Shelters != null && Shelters.Count > 0 ? Shelters[0] : null;
        public CorpseLot2DView CorpseLot;
        public WaterWell2DView WaterWell;
        public FeedSilo2DView FeedSilo;

        [Header("Fence Sprites")]
        public Sprite FenceHSprite;
        public Sprite FenceVSprite;

        [Header("Path Tile Variations")]
        public Tile TileV;
        public Tile TileH;
        public Tile TileCross;
        public Tile TileTWest;
        public Tile TileTEast;
        public Tile TileTNorth;
        public Tile TileTSouth;
        public Tile TileCornerSW;
        public Tile TileCornerSE;
        public Tile TileCornerNW;
        public Tile TileCornerNE;
        public Tile TileEndW;
        public Tile TileEndE;
        public Tile TileEndN;
        public Tile TileEndS;

        public void SetPathTiles(
            Tile v, Tile h, Tile cross,
            Tile tWest, Tile tEast, Tile tNorth, Tile tSouth,
            Tile cornerSW, Tile cornerSE, Tile cornerNW, Tile cornerNE,
            Tile endW, Tile endE, Tile endN, Tile endS)
        {
            TileV = v; TileH = h; TileCross = cross;
            TileTWest = tWest; TileTEast = tEast; TileTNorth = tNorth; TileTSouth = tSouth;
            TileCornerSW = cornerSW; TileCornerSE = cornerSE; TileCornerNW = cornerNW; TileCornerNE = cornerNE;
            TileEndW = endW; TileEndE = endE; TileEndN = endN; TileEndS = endS;
        }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            // Tá»± Ä‘á»™ng Ä‘iá»u chá»‰nh Sorting Order cá»§a Tilemap náº¿u scene cÅ© cÃ³ Ground >= -100
            // Äáº£m báº£o nhÃ¢n váº­t vÃ  thÃº nuÃ´i khÃ´ng bao giá» bá»‹ chÃ¬m dÆ°á»›i ná»n cá» á»Ÿ ná»­a trÃªn map
            var tilemapRenderers = FindObjectsByType<TilemapRenderer>();
            foreach (var tr in tilemapRenderers)
            {
                if (tr.sortingOrder >= -100)
                {
                    if (tr.gameObject.name.Contains("Ground")) tr.sortingOrder = -10000;
                    else if (tr.gameObject.name.Contains("Path")) tr.sortingOrder = -9900;
                    else if (tr.gameObject.name.Contains("Water")) tr.sortingOrder = -9800;
                    else if (tr.gameObject.name.Contains("Mud")) tr.sortingOrder = -9700;
                    else tr.sortingOrder = -9600;
                }
            }

            EnforceRuntimePhysicsAndColliders();
            KhoaHouse2DBuilder.BuildIfMissing();
            EnforceRuntimePaths();
            EnforceBarnTroughs();
            FlowerGardenBuilder.BuildIfMissing();
            NPCVillageManager.BuildIfMissing();
            RefreshInfrastructureRegistries();

            // Dam bao toan bo heo duoc kich hoat model
            foreach (var pig in FindObjectsByType<PigAgentView>())
            {
                pig.EnsurePigModel();
            }
        }

        private void EnforceRuntimePhysicsAndColliders()
        {
            // 1. Há»“ nÆ°á»›c (Water Tilemap) - Bá»• sung TilemapCollider2D náº¿u thiáº¿u
            var tilemaps = FindObjectsByType<Tilemap>();
            foreach (var tm in tilemaps)
            {
                if (tm.gameObject.name.Contains("Water"))
                {
                    var col = tm.GetComponent<TilemapCollider2D>();
                    if (col == null)
                    {
                        col = tm.gameObject.AddComponent<TilemapCollider2D>();
                    }
                    var bounds = tm.cellBounds;
                    for (int x = bounds.xMin; x <= bounds.xMax; x++)
                    {
                        for (int y = bounds.yMin; y <= bounds.yMax; y++)
                        {
                            var tile = tm.GetTile(new Vector3Int(x, y, 0)) as Tile;
                            if (tile != null && tile.colliderType != Tile.ColliderType.Grid)
                            {
                                tile.colliderType = Tile.ColliderType.Grid;
                                tm.RefreshTile(new Vector3Int(x, y, 0));
                            }
                        }
                    }
                }
            }

            // 2. MÃ¡ng Äƒn & Bá»“n nÆ°á»›c - Chuyá»ƒn sang Solid Collider, khÃ´ng cho Ä‘i xuyÃªn
            foreach (var feeder in FindObjectsByType<Feeder2DView>())
            {
                var col = feeder.GetComponent<BoxCollider2D>();
                if (col != null && col.isTrigger)
                {
                    col.isTrigger = false;
                    col.size = new Vector2(1.8f, 0.7f);
                    col.offset = new Vector2(0f, -0.1f);
                }
            }

            foreach (var trough in FindObjectsByType<WaterTrough2DView>())
            {
                var col = trough.GetComponent<BoxCollider2D>();
                if (col != null && col.isTrigger)
                {
                    col.isTrigger = false;
                    col.size = new Vector2(1.8f, 0.7f);
                    col.offset = new Vector2(0f, -0.1f);
                }
            }

            // 3. MÃ¡i trÃº (Shelter) - Cáº¥u hÃ¬nh chuáº©n xÃ¡c cáº£ Solid Collider (cháº·n 100% thÃ¢n chuá»“ng, khÃ´ng cho Ä‘i xuyÃªn) vÃ  Trigger Collider (vÃ¹ng hiÃªn trÃº áº©n cho heo)
            foreach (var shelter in FindObjectsByType<Shelter2DView>())
            {
                var cols = shelter.GetComponents<BoxCollider2D>();
                BoxCollider2D solidCol = null;
                BoxCollider2D triggerCol = null;

                foreach (var c in cols)
                {
                    if (!c.isTrigger && solidCol == null)
                    {
                        solidCol = c;
                    }
                    else if (c.isTrigger && triggerCol == null)
                    {
                        triggerCol = c;
                    }
                    else
                    {
                        Destroy(c);
                    }
                }

                if (solidCol == null) solidCol = shelter.gameObject.AddComponent<BoxCollider2D>();
                if (triggerCol == null) triggerCol = shelter.gameObject.AddComponent<BoxCollider2D>();

                // Solid Collider: ChÃ¢n Ä‘áº¿ mÃ³ng nhÃ  (Footprint) tiáº¿p Ä‘áº¥t 4x2 Ã´ (~5.7m x 1.5m). NÃ³c mÃ¡i phÃ­a trÃªn hoÃ n toÃ n KHÃ”NG cáº£n nhÃ¢n váº­t!
                solidCol.isTrigger = false;
                solidCol.size = new Vector2(3.8f, 1.0f);
                solidCol.offset = new Vector2(0f, -0.85f);

                // Trigger Collider: VÃ¹ng hiÃªn trÆ°á»›c cá»­a chuá»“ng Ä‘á»ƒ heo náº±m trÃº mÆ°a / ngá»§
                triggerCol.isTrigger = true;
                triggerCol.size = new Vector2(4.0f, 0.8f);
                triggerCol.offset = new Vector2(0f, -1.35f);

                // Dynamic Y-Sorting: Tá»a Ä‘á»™ gá»‘c sáº¯p xáº¿p táº¡i Ä‘Ãºng chÃ¢n mÃ³ng tiáº¿p Ä‘áº¥t
                var shelterSr = shelter.GetComponent<SpriteRenderer>();
                if (shelterSr != null)
                {
                    shelterSr.sortingOrder = Mathf.RoundToInt(-(shelter.transform.position.y - 1.28f) * 100);
                }
            }

            var towerObj = GameObject.Find("DefenseTower (NoXuyenVan)");
            if (towerObj != null)
            {
                var towerCol = towerObj.GetComponent<CircleCollider2D>();
                if (towerCol == null) towerCol = towerObj.AddComponent<CircleCollider2D>();
                towerCol.offset = new Vector2(0f, -0.35f);
                towerCol.radius = 0.4f;
                towerCol.isTrigger = false;

                var towerSr = towerObj.GetComponent<SpriteRenderer>();
                if (towerSr != null)
                {
                    towerSr.sortingOrder = Mathf.RoundToInt(-(towerObj.transform.position.y - 1.12f) * 100);
                }
            }

            // 4. ThÃ¢n cÃ¢y & Táº£ng Ä‘Ã¡ - Äáº·t collider chuáº©n xÃ¡c táº¡i gá»‘c & cáº­p nháº­t sortingOrder theo gá»‘c
            var allTransforms = FindObjectsByType<Transform>();
            foreach (var tr in allTransforms)
            {
                if (tr.gameObject.name.Contains("Prairie_Tree") || tr.gameObject.name == "Prairie_Tree")
                {
                    var circleCol = tr.GetComponent<CircleCollider2D>();
                    if (circleCol != null)
                    {
                        circleCol.offset = new Vector2(0f, -0.85f);
                        circleCol.radius = 0.22f;
                        circleCol.isTrigger = false;
                    }
                    var sr = tr.GetComponent<SpriteRenderer>();
                    if (sr != null)
                    {
                        sr.sortingOrder = Mathf.RoundToInt(-(tr.position.y - 1.53f) * 100);
                    }
                }
                else if (tr.gameObject.name.Contains("Prairie_Rock") || tr.gameObject.name == "Prairie_Rock")
                {
                    var circleCol = tr.GetComponent<CircleCollider2D>();
                    if (circleCol != null)
                    {
                        circleCol.offset = new Vector2(0f, -0.1f);
                        circleCol.radius = 0.35f;
                        circleCol.isTrigger = false;
                    }
                    var sr = tr.GetComponent<SpriteRenderer>();
                    if (sr != null)
                    {
                        sr.sortingOrder = Mathf.RoundToInt(-(tr.position.y - 0.14f) * 100);
                    }
                }
            }

            // 5. Cá»•ng hÃ ng rÃ o cháº¯n heo (Pasture Gate Barrier)
            if (FindAnyObjectByType<PigGateBarrier>() == null)
            {
                var gateObj = new GameObject("Pasture_Gate_Barrier");
                gateObj.transform.position = new Vector3(0, PigPastureBounds.yMin, 0);
                var gateCol = gateObj.AddComponent<BoxCollider2D>();
                gateCol.size = new Vector2(6.2f, 0.8f);
                gateCol.offset = Vector2.zero;
                gateObj.AddComponent<PigGateBarrier>();
            }

            // 6. Tá»± Ä‘á»™ng thu gá»n rÃ o cÅ© (náº¿u scene cÅ© cÃ³ rÃ o bao ngoÃ i 70x50m) vá» Ä‘Ãºng chuáº©n chuá»“ng PigPastureBounds
            var fenceTop = GameObject.Find("Fence_Top");
            if (fenceTop != null && fenceTop.transform.position.y > 20f)
            {
                fenceTop.transform.position = new Vector3(0, 14f, 0);
                fenceTop.transform.localScale = new Vector3(40.0f, 0.8f, 1f);

                var fLeft = GameObject.Find("Fence_Left");
                if (fLeft != null)
                {
                    fLeft.transform.position = new Vector3(-20f, 0, 0);
                    fLeft.transform.localScale = new Vector3(0.8f, 28.8f, 1f);
                }

                var fRight = GameObject.Find("Fence_Right");
                if (fRight != null)
                {
                    fRight.transform.position = new Vector3(20f, 0, 0);
                    fRight.transform.localScale = new Vector3(0.8f, 28.8f, 1f);
                }

                var fBL = GameObject.Find("Fence_Bottom_Left");
                if (fBL != null)
                {
                    fBL.transform.position = new Vector3(-11.5f, -14f, 0);
                    fBL.transform.localScale = new Vector3(17.0f, 0.8f, 1f);
                }

                var fBR = GameObject.Find("Fence_Bottom_Right");
                if (fBR != null)
                {
                    fBR.transform.position = new Vector3(11.5f, -14f, 0);
                    fBR.transform.localScale = new Vector3(17.0f, 0.8f, 1f);
                }
            }

            // 7. Äá»“ng bá»™ kÃ­ch thÆ°á»›c vÃ  sprite rÃ o chuáº©n 2.5D cho khu chuá»“ng tháº£
            var fPastureTop = GameObject.Find("Fence_Pasture_Top");
            if (fPastureTop != null)
            {
                var sr = fPastureTop.GetComponent<SpriteRenderer>();
                if (sr != null && sr.size.x != 40.0f) sr.size = new Vector2(40.0f, sr.size.y);
                var col = fPastureTop.GetComponent<BoxCollider2D>();
                if (col != null && col.size.x != 40.0f) col.size = new Vector2(40.0f, col.size.y);
            }

            var fPastureBLObj = GameObject.Find("Fence_Pasture_Bottom_Left");
            if (fPastureBLObj != null)
            {
                var sr = fPastureBLObj.GetComponent<SpriteRenderer>();
                if (sr != null && sr.size.x != 17.0f) sr.size = new Vector2(17.0f, sr.size.y);
                var col = fPastureBLObj.GetComponent<BoxCollider2D>();
                if (col != null && col.size.x != 17.0f) col.size = new Vector2(17.0f, col.size.y);
            }

            var fPastureBRObj = GameObject.Find("Fence_Pasture_Bottom_Right");
            if (fPastureBRObj != null)
            {
                var sr = fPastureBRObj.GetComponent<SpriteRenderer>();
                if (sr != null && sr.size.x != 17.0f) sr.size = new Vector2(17.0f, sr.size.y);
                var col = fPastureBRObj.GetComponent<BoxCollider2D>();
                if (col != null && col.size.x != 17.0f) col.size = new Vector2(17.0f, col.size.y);
            }

            if (FenceVSprite != null)
            {
                var fPastureLeft = GameObject.Find("Fence_Pasture_Left");
                if (fPastureLeft != null)
                {
                    var sr = fPastureLeft.GetComponent<SpriteRenderer>();
                    if (sr != null && sr.sprite != FenceVSprite) sr.sprite = FenceVSprite;
                }
                var fPastureRight = GameObject.Find("Fence_Pasture_Right");
                if (fPastureRight != null)
                {
                    var sr = fPastureRight.GetComponent<SpriteRenderer>();
                    if (sr != null && sr.sprite != FenceVSprite) sr.sprite = FenceVSprite;
                }
                var fLeftOld = GameObject.Find("Fence_Left");
                if (fLeftOld != null)
                {
                    var sr = fLeftOld.GetComponent<SpriteRenderer>();
                    if (sr != null && sr.sprite != FenceVSprite) sr.sprite = FenceVSprite;
                }
                var fRightOld = GameObject.Find("Fence_Right");
                if (fRightOld != null)
                {
                    var sr = fRightOld.GetComponent<SpriteRenderer>();
                    if (sr != null && sr.sprite != FenceVSprite) sr.sprite = FenceVSprite;
                }
            }
        }

        public void EnforceRuntimePaths()
        {
            // 1. Tu dong kiem tra va doi cay che duong mon neu co cay tai (-5, 9)
            var allTrees = FindObjectsByType<SpriteRenderer>();
            foreach (var r in allTrees)
            {
                if (r.gameObject.name.Contains("Prairie_Tree") && Mathf.Abs(r.transform.position.x - (-5f)) < 0.3f && Mathf.Abs(r.transform.position.y - 9f) < 0.6f)
                {
                    r.transform.position = new Vector3(-5f, 11f, 0f);
                    r.sortingOrder = Mathf.RoundToInt(-(11f - 1.53f) * 100);
                }
            }

            // 2. Keo dai con duong mon dan den tan bac them nha rieng cua Khoa
            var tilemaps = FindObjectsByType<Tilemap>();
            foreach (var tm in tilemaps)
            {
                if (tm.gameObject.name.Contains("Path"))
                {
                    Tile defaultTile = TileH != null ? TileH : (TileV != null ? TileV : (tm.GetTile(new Vector3Int(-1, 6, 0)) as Tile));
                    if (defaultTile != null)
                    {
                        var home = GameObject.Find("Khoa's Home");
                        Vector3 homePos = home != null ? home.transform.position : new Vector3(10f, 8f, 0f);
                        int targetX = Mathf.RoundToInt(homePos.x + 1.8f);
                        int targetY = Mathf.RoundToInt(homePos.y - 0.9f);

                        // Keo nhanh ngang tu truc chinh X: 0 den X: targetX (tai Y: 6)
                        int minX = Mathf.Min(0, targetX);
                        int maxX = Mathf.Max(0, targetX);
                        for (int x = minX; x <= maxX; x++)
                        {
                            tm.SetTile(new Vector3Int(x, 6, 0), defaultTile);
                        }

                        // Re nhanh thang len them cua nha rieng (tu Y: 6 len targetY)
                        int minY = Mathf.Min(6, targetY);
                        int maxY = Mathf.Max(6, targetY);
                        for (int y = minY; y <= maxY; y++)
                        {
                            tm.SetTile(new Vector3Int(targetX, y, 0), defaultTile);
                        }

                        // 3. Hoan thien truc duong chinh phia Nam xuong cong lang NPC (tu Y: -28 xuong Y: -34)
                        for (int y = -28; y >= -34; y--)
                        {
                            tm.SetTile(new Vector3Int(0, y, 0), defaultTile);
                        }
                    }

                    // Tu dong quet va khop noi toan bo duong mon theo huong ket noi
                    if (TileH != null && TileV != null)
                    {
                        AutoTilePathMap(
                            tm,
                            TileV, TileH, TileCross,
                            TileTWest, TileTEast, TileTNorth, TileTSouth,
                            TileCornerSW, TileCornerSE, TileCornerNW, TileCornerNE,
                            TileEndW, TileEndE, TileEndN, TileEndS
                        );
                    }
                }
            }
        }

        public void EnforceBarnTroughs()
        {
            var shelter = FindAnyObjectByType<Shelter2DView>();
            Vector3 barnPos = shelter != null ? shelter.transform.position : new Vector3(-13f, 8.5f, 0f);

            // Toa do 2 cot ngay ngan song song thang tap tu nha kho xuong duoi
            // Cot trai (Mang Cam): X = barnPos.x - 2.5f (-15.5f)
            // Cot phai (Mang Nuoc): X = barnPos.x + 2.5f (-10.5f)
            // Cach nhau 5m thong thoang o giua, cach deu xuong duoi moi hang cach nhau 3.6m
            float colFeederX = barnPos.x - 2.5f;
            float colWaterX = barnPos.x + 2.5f;
            float[] rowY = { barnPos.y - 3.7f, barnPos.y - 7.3f, barnPos.y - 10.9f }; // 4.8f, 1.2f, -2.4f

            // 1. Thu thap toan bo Mang Cam (Feeders) trong nong trai va xep thang hang doc ben trai
            var allFeeders = new List<Feeder2DView>(FindObjectsByType<Feeder2DView>());
            while (allFeeders.Count < 3)
            {
                var go = new GameObject($"Barn_Feeder_{allFeeders.Count + 1}");
                var sr = go.AddComponent<SpriteRenderer>();
                sr.sprite = UISpriteLoader.GetFeederSprite();
                sr.color = Color.white;
                var col = go.AddComponent<BoxCollider2D>();
                col.size = new Vector2(1.8f, 0.7f);
                col.offset = new Vector2(0f, -0.1f);
                col.isTrigger = false;
                var f = go.AddComponent<Feeder2DView>();
                f.MaxFoodKg = 50f;
                f.CurrentFoodKg = 50f;
                allFeeders.Add(f);
            }

            for (int i = 0; i < allFeeders.Count; i++)
            {
                var f = allFeeders[i];
                float y = (i < rowY.Length) ? rowY[i] : (barnPos.y - 3.7f - i * 3.6f);
                f.transform.position = new Vector3(colFeederX, y, 0f);
                f.transform.localScale = new Vector3(1.3f, 1.3f, 1f);
                var sr = f.GetComponent<SpriteRenderer>();
                if (sr != null)
                {
                    sr.sortingOrder = Mathf.RoundToInt(-y * 100);
                    if (sr.sprite == null) sr.sprite = UISpriteLoader.GetFeederSprite();
                }
                var col = f.GetComponent<BoxCollider2D>();
                if (col != null)
                {
                    col.size = new Vector2(1.8f, 0.7f);
                    col.offset = new Vector2(0f, -0.1f);
                    col.isTrigger = false;
                }
                f.CurrentFoodKg = Mathf.Max(f.CurrentFoodKg, 45f);
                if (!Feeders.Contains(f)) Feeders.Add(f);
            }

            // 2. Thu thap toan bo Mang Nuoc (Water Troughs) trong nong trai va xep thang hang doc ben phai
            var allWater = new List<WaterTrough2DView>(FindObjectsByType<WaterTrough2DView>());
            while (allWater.Count < 3)
            {
                var go = new GameObject($"Barn_WaterTrough_{allWater.Count + 1}");
                var sr = go.AddComponent<SpriteRenderer>();
                sr.sprite = UISpriteLoader.GetWaterTroughSprite();
                sr.color = Color.white;
                var col = go.AddComponent<BoxCollider2D>();
                col.size = new Vector2(1.8f, 0.7f);
                col.offset = new Vector2(0f, -0.1f);
                col.isTrigger = false;
                var w = go.AddComponent<WaterTrough2DView>();
                w.MaxWaterLiters = 100f;
                w.CurrentWaterLiters = 100f;
                allWater.Add(w);
            }

            for (int i = 0; i < allWater.Count; i++)
            {
                var w = allWater[i];
                float y = (i < rowY.Length) ? rowY[i] : (barnPos.y - 3.7f - i * 3.6f);
                w.transform.position = new Vector3(colWaterX, y, 0f);
                w.transform.localScale = new Vector3(1.3f, 1.3f, 1f);
                var sr = w.GetComponent<SpriteRenderer>();
                if (sr != null)
                {
                    sr.sortingOrder = Mathf.RoundToInt(-y * 100);
                    if (sr.sprite == null) sr.sprite = UISpriteLoader.GetWaterTroughSprite();
                }
                var col = w.GetComponent<BoxCollider2D>();
                if (col != null)
                {
                    col.size = new Vector2(1.8f, 0.7f);
                    col.offset = new Vector2(0f, -0.1f);
                    col.isTrigger = false;
                }
                w.CurrentWaterLiters = Mathf.Max(w.CurrentWaterLiters, 90f);
                if (!WaterTroughs.Contains(w)) WaterTroughs.Add(w);
            }
        }

        public static void AutoTilePathMap(
            Tilemap pathTilemap,
            Tile tileV,
            Tile tileH,
            Tile tileCross,
            Tile tileTWest,
            Tile tileTEast,
            Tile tileTNorth,
            Tile tileTSouth,
            Tile tileCornerSW,
            Tile tileCornerSE,
            Tile tileCornerNW,
            Tile tileCornerNE,
            Tile tileEndW,
            Tile tileEndE,
            Tile tileEndN,
            Tile tileEndS)
        {
            if (pathTilemap == null) return;

            var pathPositions = new HashSet<Vector3Int>();
            var bounds = pathTilemap.cellBounds;
            for (int x = bounds.xMin; x <= bounds.xMax; x++)
            {
                for (int y = bounds.yMin; y <= bounds.yMax; y++)
                {
                    var p = new Vector3Int(x, y, 0);
                    if (pathTilemap.HasTile(p))
                    {
                        pathPositions.Add(p);
                    }
                }
            }

            foreach (var p in pathPositions)
            {
                bool n = pathPositions.Contains(p + new Vector3Int(0, 1, 0));
                bool s = pathPositions.Contains(p + new Vector3Int(0, -1, 0));
                bool e = pathPositions.Contains(p + new Vector3Int(1, 0, 0));
                bool w = pathPositions.Contains(p + new Vector3Int(-1, 0, 0));

                Tile chosenTile = tileH != null ? tileH : tileV;

                if (n && s && e && w)
                {
                    chosenTile = tileCross != null ? tileCross : tileH;
                }
                else if (n && s && w && !e)
                {
                    chosenTile = tileTWest != null ? tileTWest : (tileV != null ? tileV : tileH);
                }
                else if (n && s && e && !w)
                {
                    chosenTile = tileTEast != null ? tileTEast : (tileV != null ? tileV : tileH);
                }
                else if (e && w && n && !s)
                {
                    chosenTile = tileTNorth != null ? tileTNorth : (tileH != null ? tileH : tileV);
                }
                else if (e && w && s && !n)
                {
                    chosenTile = tileTSouth != null ? tileTSouth : (tileH != null ? tileH : tileV);
                }
                else if (s && w && !n && !e)
                {
                    chosenTile = tileCornerSW != null ? tileCornerSW : tileH;
                }
                else if (s && e && !n && !w)
                {
                    chosenTile = tileCornerSE != null ? tileCornerSE : tileH;
                }
                else if (n && w && !s && !e)
                {
                    chosenTile = tileCornerNW != null ? tileCornerNW : tileH;
                }
                else if (n && e && !s && !w)
                {
                    chosenTile = tileCornerNE != null ? tileCornerNE : tileH;
                }
                else if (n && s && !e && !w)
                {
                    chosenTile = tileV != null ? tileV : tileH;
                }
                else if (e && w && !n && !s)
                {
                    chosenTile = tileH != null ? tileH : tileV;
                }
                else if (e && !w && !n && !s)
                {
                    chosenTile = tileEndW != null ? tileEndW : tileH;
                }
                else if (w && !e && !n && !s)
                {
                    chosenTile = tileEndE != null ? tileEndE : tileH;
                }
                else if (s && !n && !e && !w)
                {
                    chosenTile = tileEndN != null ? tileEndN : (tileV != null ? tileV : tileH);
                }
                else if (n && !s && !e && !w)
                {
                    chosenTile = tileEndS != null ? tileEndS : (tileV != null ? tileV : tileH);
                }

                pathTilemap.SetTile(p, chosenTile);
            }
        }

        public void RefreshInfrastructureRegistries()
        {
            Pigs.Clear();
            Pigs.AddRange(FindObjectsByType<PigAgentView>());

            Fences.Clear();
            Fences.AddRange(FindObjectsByType<Fence2DView>());
            SubdivideMonolithicFences();

            Feeders.Clear();
            Feeders.AddRange(FindObjectsByType<Feeder2DView>());

            WaterTroughs.Clear();
            WaterTroughs.AddRange(FindObjectsByType<WaterTrough2DView>());

            MudPits.Clear();
            MudPits.AddRange(FindObjectsByType<MudPit2DView>());

            Shelters.Clear();
            Shelters.AddRange(FindObjectsByType<Shelter2DView>());

            if (CorpseLot == null)
            {
                CorpseLot = FindAnyObjectByType<CorpseLot2DView>();
            }

            if (WaterWell == null)
            {
                WaterWell = FindAnyObjectByType<WaterWell2DView>();
            }

            if (FeedSilo == null)
            {
                FeedSilo = FindAnyObjectByType<FeedSilo2DView>();
            }
        }

        private readonly Dictionary<Vector2Int, Fence2DView> fenceGrid = new Dictionary<Vector2Int, Fence2DView>();
        private Grid farmGrid;

        public Grid FarmGrid
        {
            get
            {
                if (farmGrid == null) farmGrid = FindAnyObjectByType<Grid>();
                return farmGrid;
            }
        }

        public Vector2Int WorldToGrid(Vector2 worldPos)
        {
            if (FarmGrid != null)
            {
                Vector3Int cell = FarmGrid.WorldToCell(worldPos);
                return new Vector2Int(cell.x, cell.y);
            }
            return new Vector2Int(Mathf.FloorToInt(worldPos.x), Mathf.FloorToInt(worldPos.y));
        }

        public Vector2 GridToWorldCenter(Vector2Int gridPos)
        {
            if (FarmGrid != null)
            {
                Vector3 center = FarmGrid.GetCellCenterWorld(new Vector3Int(gridPos.x, gridPos.y, 0));
                return new Vector2(center.x, center.y);
            }
            return new Vector2(gridPos.x + 0.5f, gridPos.y + 0.5f);
        }

        public Fence2DView GetFenceAtGrid(Vector2Int gridPos)
        {
            if (fenceGrid.TryGetValue(gridPos, out var fence) && fence != null)
            {
                return fence;
            }

            // Fallback: QuÃ©t danh sÃ¡ch Fences náº¿u chÆ°a Ä‘á»“ng bá»™ vÃ o Dictionary
            for (int i = 0; i < Fences.Count; i++)
            {
                var f = Fences[i];
                if (f != null && f.GridPosition == gridPos)
                {
                    fenceGrid[gridPos] = f;
                    return f;
                }
            }
            return null;
        }

        public bool HasFenceAt(Vector2Int gridPos)
        {
            return GetFenceAtGrid(gridPos) != null;
        }

        public void UpdateFenceConnectionsAt(Vector2Int gridPos)
        {
            // Cáº­p nháº­t Ã´ chá»‰ Ä‘á»‹nh vÃ  4 Ã´ lÃ¢n cáº­n (Báº¯c, ÄÃ´ng, Nam, TÃ¢y)
            Vector2Int[] positionsToUpdate = new Vector2Int[]
            {
                gridPos,
                gridPos + Vector2Int.up,    // Báº¯c (0, 1)
                gridPos + Vector2Int.right, // ÄÃ´ng (1, 0)
                gridPos + Vector2Int.down,  // Nam (0, -1)
                gridPos + Vector2Int.left   // TÃ¢y (-1, 0)
            };

            foreach (var pos in positionsToUpdate)
            {
                var f = GetFenceAtGrid(pos);
                if (f != null)
                {
                    bool n = HasFenceAt(pos + Vector2Int.up);
                    bool e = HasFenceAt(pos + Vector2Int.right);
                    bool s = HasFenceAt(pos + Vector2Int.down);
                    bool w = HasFenceAt(pos + Vector2Int.left);
                    f.SetConnections(n, e, s, w);
                }
            }
        }

        public void UpdateAllFenceConnections()
        {
            fenceGrid.Clear();
            for (int i = Fences.Count - 1; i >= 0; i--)
            {
                if (Fences[i] == null)
                {
                    Fences.RemoveAt(i);
                    continue;
                }

                var f = Fences[i];
                Vector2Int pos = WorldToGrid(f.transform.position);
                f.GridPosition = pos;
                Vector2 center = GridToWorldCenter(pos);
                f.transform.position = new Vector3(center.x, center.y, 0f);
                var sr = f.GetComponent<SpriteRenderer>();
                if (sr != null) sr.sortingOrder = Mathf.RoundToInt(-center.y * 100);
                fenceGrid[pos] = f;
            }

            foreach (var f in Fences)
            {
                if (f == null) continue;
                Vector2Int pos = f.GridPosition;
                bool n = HasFenceAt(pos + Vector2Int.up);
                bool e = HasFenceAt(pos + Vector2Int.right);
                bool s = HasFenceAt(pos + Vector2Int.down);
                bool w = HasFenceAt(pos + Vector2Int.left);
                f.SetConnections(n, e, s, w);
            }
        }

        public void SubdivideMonolithicFences()
        {
            var oldFences = new List<Fence2DView>(Fences);
            bool subdividedAny = false;

            foreach (var fence in oldFences)
            {
                if (fence == null) continue;
                var col = fence.GetComponent<BoxCollider2D>();
                if (col == null) continue;

                Vector2 size = col.size;
                // Náº¿u rÃ o nÃ y lÃ  1 Ä‘oáº¡n dÃ i liÃªn tá»¥c (> 1.4m) do setup cÅ© sinh ra
                if (size.x > 1.4f || size.y > 1.4f)
                {
                    subdividedAny = true;
                    Transform parent = fence.transform.parent;
                    Vector3 basePos = fence.transform.position;
                    bool isHorizontal = size.x > size.y;

                    if (isHorizontal)
                    {
                        float leftEdge = basePos.x - size.x * 0.5f;
                        float rightEdge = basePos.x + size.x * 0.5f;
                        int minCellX = WorldToGrid(new Vector2(leftEdge + 0.1f, basePos.y)).x;
                        int maxCellX = WorldToGrid(new Vector2(rightEdge - 0.1f, basePos.y)).x;
                        int cellY = WorldToGrid(basePos).y;

                        for (int x = minCellX; x <= maxCellX; x++)
                        {
                            Vector2Int postPos = new Vector2Int(x, cellY);
                            if (!HasFenceAt(postPos))
                            {
                                BuildFenceUnit(postPos, parent);
                            }
                        }
                    }
                    else
                    {
                        float bottomEdge = basePos.y - size.y * 0.5f;
                        float topEdge = basePos.y + size.y * 0.5f;
                        int minCellY = WorldToGrid(new Vector2(basePos.x, bottomEdge + 0.1f)).y;
                        int maxCellY = WorldToGrid(new Vector2(basePos.x, topEdge - 0.1f)).y;
                        int cellX = WorldToGrid(basePos).x;

                        for (int y = minCellY; y <= maxCellY; y++)
                        {
                            Vector2Int postPos = new Vector2Int(cellX, y);
                            if (!HasFenceAt(postPos))
                            {
                                BuildFenceUnit(postPos, parent);
                            }
                        }
                    }

                    Fences.Remove(fence);
                    Destroy(fence.gameObject);
                }
            }

            UpdateAllFenceConnections();
        }

        private Fence2DView BuildFenceUnit(Vector2Int gridPos, Transform parent = null)
        {
            var go = new GameObject($"Fence_Post_{gridPos.x}_{gridPos.y}");
            if (parent != null) go.transform.SetParent(parent, false);
            Vector2 center = GridToWorldCenter(gridPos);
            go.transform.position = new Vector3(center.x, center.y, 0f);

            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = UISpriteLoader.GetFenceSprite(0);
            sr.drawMode = SpriteDrawMode.Simple;
            sr.color = Color.white;
            sr.sortingOrder = Mathf.RoundToInt(-center.y * 100);

            var col = go.AddComponent<BoxCollider2D>();
            col.size = new Vector2(0.6f, 0.6f);
            col.isTrigger = false;

            var fenceView = go.AddComponent<Fence2DView>();
            fenceView.MaxHp = 300f;
            fenceView.CurrentHp = 300f;
            fenceView.GridPosition = gridPos;

            Fences.Add(fenceView);
            fenceGrid[gridPos] = fenceView;
            return fenceView;
        }

        public void ClearAllFences()
        {
            var list = Fences.ToArray();
            foreach (var f in list)
            {
                if (f != null && f.gameObject != null)
                {
                    Destroy(f.gameObject);
                }
            }
            Fences.Clear();
            fenceGrid.Clear();
        }

        public void RebuildPastureFences()
        {
            ClearAllFences();
            var parent = GameObject.Find("Pasture_Fences")?.transform;
            if (parent == null)
            {
                var pGo = new GameObject("Pasture_Fences");
                parent = pGo.transform;
            }

            // Chuá»“ng tháº£ heo 40m x 28m: Cell X: [-20..19], Cell Y: [-14..13]
            // RÃ o trÃªn: Cell Y = 13 (Center Y = 13.5)
            for (int x = -20; x <= 19; x++) BuildFenceUnit(new Vector2Int(x, 13), parent);
            // RÃ o trÃ¡i & pháº£i: Cell Y = [-13..12]
            for (int y = -13; y <= 12; y++) BuildFenceUnit(new Vector2Int(-20, y), parent);
            for (int y = -13; y <= 12; y++) BuildFenceUnit(new Vector2Int(19, y), parent);
            // RÃ o dÆ°á»›i (Cá»•ng Nam rá»™ng 5m tá»« Cell -2 Ä‘áº¿n 2, tháº³ng lá»‘i Ä‘i Cell 0)
            for (int x = -20; x <= -3; x++) BuildFenceUnit(new Vector2Int(x, -14), parent);
            for (int x = 3; x <= 19; x++) BuildFenceUnit(new Vector2Int(x, -14), parent);

            UpdateAllFenceConnections();
        }

        public Fence2DView GetFenceAt(Vector2 worldPos, float radius = 0.55f)
        {
            Vector2Int gridPos = WorldToGrid(worldPos);
            var exact = GetFenceAtGrid(gridPos);
            if (exact != null) return exact;

            float minDistSqr = radius * radius;
            Fence2DView best = null;
            for (int i = 0; i < Fences.Count; i++)
            {
                var f = Fences[i];
                if (f == null) continue;
                float dSqr = ((Vector2)f.transform.position - worldPos).sqrMagnitude;
                if (dSqr < minDistSqr)
                {
                    minDistSqr = dSqr;
                    best = f;
                }
            }
            return best;
        }

        public Fence2DView BuildFence(Vector2 position, bool isVertical = false)
        {
            Vector2Int gridPos = WorldToGrid(position);
            return BuildFence(gridPos);
        }

        public Fence2DView BuildFence(Vector2Int gridPos)
        {
            // Kiá»ƒm tra xem Ã´ nÃ y Ä‘Ã£ cÃ³ rÃ o chÆ°a
            if (HasFenceAt(gridPos))
            {
                return GetFenceAtGrid(gridPos);
            }

            var go = new GameObject($"Fence_Post_{gridPos.x}_{gridPos.y}");
            Vector2 center = GridToWorldCenter(gridPos);
            go.transform.position = new Vector3(center.x, center.y, 0f);

            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = UISpriteLoader.GetFenceSprite(0);
            sr.drawMode = SpriteDrawMode.Simple;
            sr.color = Color.white;
            sr.sortingOrder = Mathf.RoundToInt(-center.y * 100);

            var col = go.AddComponent<BoxCollider2D>();
            col.size = new Vector2(0.6f, 0.6f);
            col.isTrigger = false;

            var fenceView = go.AddComponent<Fence2DView>();
            fenceView.MaxHp = 300f;
            fenceView.CurrentHp = 300f;
            fenceView.GridPosition = gridPos;

            Fences.Add(fenceView);
            fenceGrid[gridPos] = fenceView;

            // Tá»± Ä‘á»™ng kiá»ƒm tra cÃ¡c Ã´ vuÃ´ng lÃ¢n cáº­n (Báº¯c, ÄÃ´ng, Nam, TÃ¢y) vÃ  ná»‘i vá»›i nhau
            UpdateFenceConnectionsAt(gridPos);

            return fenceView;
        }

        public void RemoveFence(Fence2DView fence)
        {
            if (fence == null) return;
            Vector2Int gridPos = fence.GridPosition;

            if (fenceGrid.ContainsKey(gridPos) && fenceGrid[gridPos] == fence)
            {
                fenceGrid.Remove(gridPos);
            }
            if (Fences.Contains(fence))
            {
                Fences.Remove(fence);
            }
            Destroy(fence.gameObject);

            // Cáº­p nháº­t láº¡i cÃ¡c hÃ ng rÃ o lÃ¢n cáº­n Ä‘á»ƒ tá»± Ä‘á»™ng tÃ¡ch khá»›p ná»‘i
            UpdateFenceConnectionsAt(gridPos);
        }

        public Feeder2DView BuildFeeder(Vector2Int gridPos)
        {
            var go = new GameObject($"Feeder_{gridPos.x}_{gridPos.y}");
            Vector2 center = GridToWorldCenter(gridPos);
            go.transform.position = new Vector3(center.x, center.y, 0f);
            go.transform.localScale = new Vector3(1.3f, 1.3f, 1f);

            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = UISpriteLoader.GetFeederSprite();
            sr.color = Color.white;
            sr.sortingOrder = Mathf.RoundToInt(-center.y * 100);

            var col = go.AddComponent<BoxCollider2D>();
            col.size = new Vector2(1.8f, 0.7f);
            col.offset = new Vector2(0f, -0.1f);
            col.isTrigger = false;

            var feederView = go.AddComponent<Feeder2DView>();
            feederView.MaxFoodKg = 50f;
            feederView.CurrentFoodKg = 15f;

            Feeders.Add(feederView);
            return feederView;
        }

        public WaterTrough2DView BuildWaterTrough(Vector2Int gridPos)
        {
            var go = new GameObject($"WaterTrough_{gridPos.x}_{gridPos.y}");
            Vector2 center = GridToWorldCenter(gridPos);
            go.transform.position = new Vector3(center.x, center.y, 0f);
            go.transform.localScale = new Vector3(1.3f, 1.3f, 1f);

            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = UISpriteLoader.GetWaterTroughSprite();
            sr.color = Color.white;
            sr.sortingOrder = Mathf.RoundToInt(-center.y * 100);

            var col = go.AddComponent<BoxCollider2D>();
            col.size = new Vector2(1.8f, 0.7f);
            col.offset = new Vector2(0f, -0.1f);
            col.isTrigger = false;

            var troughView = go.AddComponent<WaterTrough2DView>();
            troughView.MaxWaterLiters = 100f;
            troughView.CurrentWaterLiters = 30f;

            WaterTroughs.Add(troughView);
            return troughView;
        }

        public void RemoveFeeder(Feeder2DView feeder)
        {
            if (feeder == null) return;
            if (Feeders.Contains(feeder))
            {
                Feeders.Remove(feeder);
            }
            for (int i = 0; i < Pigs.Count; i++)
            {
                if (Pigs[i] != null && (Pigs[i].CurrentActivity == PigActivityState.Eating || Pigs[i].CurrentActivity == PigActivityState.SeekingFood))
                {
                    if (Vector2.Distance(Pigs[i].transform.position, feeder.transform.position) < 3.0f)
                    {
                        Pigs[i].CurrentActivity = PigActivityState.Idling;
                    }
                }
            }
            Destroy(feeder.gameObject);
        }

        public void RemoveWaterTrough(WaterTrough2DView trough)
        {
            if (trough == null) return;
            if (WaterTroughs.Contains(trough))
            {
                WaterTroughs.Remove(trough);
            }
            for (int i = 0; i < Pigs.Count; i++)
            {
                if (Pigs[i] != null && (Pigs[i].CurrentActivity == PigActivityState.Drinking || Pigs[i].CurrentActivity == PigActivityState.SeekingWater))
                {
                    if (Vector2.Distance(Pigs[i].transform.position, trough.transform.position) < 3.0f)
                    {
                        Pigs[i].CurrentActivity = PigActivityState.Idling;
                    }
                }
            }
            Destroy(trough.gameObject);
        }

        public void RemoveDefenseTower(DefenseTower2DView tower)
        {
            if (tower == null) return;
            if (tower.BuildingModel != null)
            {
                var defManager = MobileGameController.Instance?.Engine?.Defense;
                if (defManager != null && defManager.Buildings.Contains(tower.BuildingModel))
                {
                    defManager.Buildings.Remove(tower.BuildingModel);
                }
            }
            Destroy(tower.gameObject);
        }

        public Feeder2DView GetNearestFeeder(Vector2 position, bool requireFood = true)
        {
            Feeder2DView nearest = null;
            float minDistSqr = float.MaxValue;

            for (int i = 0; i < Feeders.Count; i++)
            {
                var feeder = Feeders[i];
                if (feeder == null || (requireFood && !feeder.HasFood)) continue;

                float distSqr = ((Vector2)feeder.transform.position - position).sqrMagnitude;
                if (distSqr < minDistSqr)
                {
                    minDistSqr = distSqr;
                    nearest = feeder;
                }
            }

            return nearest;
        }

        public WaterTrough2DView GetNearestWaterTrough(Vector2 position, bool requireWater = true)
        {
            WaterTrough2DView nearest = null;
            float minDistSqr = float.MaxValue;

            for (int i = 0; i < WaterTroughs.Count; i++)
            {
                var trough = WaterTroughs[i];
                if (trough == null || (requireWater && !trough.HasWater)) continue;

                float distSqr = ((Vector2)trough.transform.position - position).sqrMagnitude;
                if (distSqr < minDistSqr)
                {
                    minDistSqr = distSqr;
                    nearest = trough;
                }
            }

            return nearest;
        }

        public MudPit2DView GetNearestMudPit(Vector2 position)
        {
            MudPit2DView nearest = null;
            float minDistSqr = float.MaxValue;

            for (int i = 0; i < MudPits.Count; i++)
            {
                var pit = MudPits[i];
                if (pit == null) continue;

                float distSqr = ((Vector2)pit.transform.position - position).sqrMagnitude;
                if (distSqr < minDistSqr)
                {
                    minDistSqr = distSqr;
                    nearest = pit;
                }
            }

            return nearest;
        }

        public Shelter2DView GetNearestShelter(Vector2 position, bool requireCapacity = true)
        {
            Shelter2DView nearest = null;
            float minDistSqr = float.MaxValue;

            for (int i = 0; i < Shelters.Count; i++)
            {
                var shelter = Shelters[i];
                if (shelter == null || (requireCapacity && !shelter.HasCapacity)) continue;

                float distSqr = ((Vector2)shelter.transform.position - position).sqrMagnitude;
                if (distSqr < minDistSqr)
                {
                    minDistSqr = distSqr;
                    nearest = shelter;
                }
            }

            return nearest;
        }

        public bool IsInsideFarm(Vector2 position)
        {
            return MapBounds.Contains(position);
        }

        public bool IsInsidePasture(Vector2 position)
        {
            return PigPastureBounds.Contains(position);
        }

        // ÄÃ n heo chá»‰ Ä‘i dáº¡o trong khuÃ´n viÃªn rÃ o chÄƒn tháº£ (PigPastureBounds)
        public Vector2 ClampInsideFarm(Vector2 position, float padding = 1f)
        {
            float clampedX = Mathf.Clamp(position.x, PigPastureBounds.xMin + padding, PigPastureBounds.xMax - padding);
            float clampedY = Mathf.Clamp(position.y, PigPastureBounds.yMin + padding, PigPastureBounds.yMax - padding);
            return new Vector2(clampedX, clampedY);
        }

        // ToÃ n bá»™ báº£n Ä‘á»“ tháº£o nguyÃªn rá»™ng lá»›n
        public Vector2 ClampInsideMap(Vector2 position, float padding = 1f)
        {
            float clampedX = Mathf.Clamp(position.x, MapBounds.xMin + padding, MapBounds.xMax - padding);
            float clampedY = Mathf.Clamp(position.y, MapBounds.yMin + padding, MapBounds.yMax - padding);
            return new Vector2(clampedX, clampedY);
        }

        public Vector2 GetRandomPositionInsideFarm(float padding = 2f)
        {
            float randX = Random.Range(PigPastureBounds.xMin + padding, PigPastureBounds.xMax - padding);
            float randY = Random.Range(PigPastureBounds.yMin + padding, PigPastureBounds.yMax - padding);
            return new Vector2(randX, randY);
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireCube(MapBounds.center, MapBounds.size);

            Gizmos.color = Color.yellow;
            Gizmos.DrawWireCube(PigPastureBounds.center, PigPastureBounds.size);
        }
#if UNITY_EDITOR
        [UnityEditor.MenuItem("PigTycoon/Tao Duong Di & Mang Trai (Create Path & Troughs)")]
        public static void CreatePathAndTroughsEditor()
        {
            var env = FindAnyObjectByType<FarmEnvironment2D>();
            if (env != null)
            {
                KhoaHouse2DBuilder.BuildIfMissing();
                env.EnforceRuntimePaths();
                env.EnforceBarnTroughs();
                FlowerGardenBuilder.BuildIfMissing();
                env.RefreshInfrastructureRegistries();
                UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(env.gameObject.scene);
            }
        }

        [UnityEditor.MenuItem("PigTycoon/Tao Vuon Hoa Hoang Gia (Create Royal Flower Garden)")]
        public static void CreateFlowerGardenEditor()
        {
            FlowerGardenBuilder.BuildGarden();
            var env = FindAnyObjectByType<FarmEnvironment2D>();
            if (env != null)
            {
                UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(env.gameObject.scene);
            }
        }

        [UnityEditor.MenuItem("PigTycoon/Tao Lang NPC & Cong Nam (Create NPC Village & South Gate)")]
        public static void CreateNPCVillageEditor()
        {
            var env = FindAnyObjectByType<FarmEnvironment2D>();
            if (env != null)
            {
                env.EnforceRuntimePaths();
                NPCVillageManager.BuildIfMissing();
                UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(env.gameObject.scene);
            }
        }
#endif
    }
}
