using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;
using TMPro;
using PigTycoon.Core;

namespace PigTycoon.Presentation
{
    public static class DefaultMapBuilder
    {
        public static void EnsureDefaultMapExists(bool forceRebuild = false)
        {
            // 0. BẢO VỆ ĐẤU TRƯỜNG COMBAT (CombatTestArena2D)
            string activeSceneName = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
            var combatCtrl = GameObject.FindAnyObjectByType<CombatArenaController>();
            var arenaEnv = GameObject.Find("Arena_Environment") ?? GameObject.Find("Arena_Floor_Tiles");
            if (activeSceneName == "CombatTestArena2D" || combatCtrl != null || arenaEnv != null)
            {
                Debug.Log("<color=cyan>[DefaultMapBuilder] Đang ở Đấu Trường Combat (CombatTestArena2D) - Bỏ qua khởi tạo Farm Map!</color>");
                return;
            }

            var groundObj = GameObject.Find("Tilemap_Ground");
            var villageEnv = GameObject.Find("Village_Environment_2D");

            // Nếu đang trong Scene Làng Bờ Rìa (có Village_Environment_2D) và đã có Tilemap_Ground
            if (villageEnv != null && groundObj != null)
            {
                var tm = groundObj.GetComponent<Tilemap>();
                if (tm != null && tm.cellBounds.size.x > 10)
                {
                    EnsureVillageEntities();
                    return;
                }
            }

            var fencesObj = GameObject.Find("Pasture_Fences");
            var shelterObj = GameObject.Find("Shelter_Zone");

            bool isMissingOrEmpty = groundObj == null || fencesObj == null || shelterObj == null;
            if (!isMissingOrEmpty && !forceRebuild)
            {
                var tm = groundObj.GetComponent<Tilemap>();
                if (tm != null && tm.cellBounds.size.x > 10)
                {
                    // Map đã tồn tại đầy đủ
                    EnsureVillageEntities();
                    return;
                }
            }

            BuildCompleteDefaultMap();
        }

        public static void BuildCompleteDefaultMap()
        {
            Debug.Log("<color=green>[DefaultMapBuilder] Đang khởi tạo toàn bộ Map mặc định cho Pig Tycoon...</color>");

            // 1. Grid & Tilemap Layers
            var gridObj = GameObject.Find("Farm_Grid");
            if (gridObj == null)
            {
                gridObj = new GameObject("Farm_Grid");
            }
            var grid = gridObj.GetComponent<Grid>();
            if (grid == null) grid = gridObj.AddComponent<Grid>();
            grid.cellSize = new Vector3(1f, 1f, 0f);

            var groundTilemap = GetOrCreateTilemap(gridObj.transform, "Tilemap_Ground", -10000);
            var waterTilemap = GetOrCreateTilemap(gridObj.transform, "Tilemap_Water", -9800);
            var mudTilemap = GetOrCreateTilemap(gridObj.transform, "Tilemap_MudPit", -9700);
            var pathTilemap = GetOrCreateTilemap(gridObj.transform, "Tilemap_Paths", -9900);

            var waterCol = waterTilemap.GetComponent<TilemapCollider2D>();
            if (waterCol == null)
            {
                waterCol = waterTilemap.gameObject.AddComponent<TilemapCollider2D>();
            }

            // 2. Load Tiles
            var (grassSteppeTile, grassTallTile, grassBaseTile, flower1Tile, flower2Tile, flower3Tile) = CreateGrassTiles();
            var (trailV, trailH, trailCross, trailTWest, trailTEast, trailTNorth, trailTSouth,
                 cornerSW, cornerSE, cornerNW, cornerNE, endW, endE, endN, endS) = CreateTrailTiles();

            var waterTile = ScriptableObject.CreateInstance<Tile>();
            waterTile.sprite = UISpriteLoader.LoadSprite("Art/Tiles/Sprites/water_tile.png", 32);
            waterTile.colliderType = Tile.ColliderType.Grid;

            var mudTile = ScriptableObject.CreateInstance<Tile>();
            mudTile.sprite = UISpriteLoader.LoadSprite("Art/Tiles/Sprites/mud_tile.png", 32);

            // 3. Phủ Thảo Nguyên Bao La Với Biên Giới Rừng Rậm & Thảm Hoa Dại (-48 đến +48, -35 đến +35)
            groundTilemap.ClearAllTiles();
            for (int x = -48; x <= 48; x++)
            {
                for (int y = -35; y <= 35; y++)
                {
                    // Khoảng cách tới đường biên map (Border Distance)
                    float distToBorder = Mathf.Min(48 - Mathf.Abs(x), 35 - Mathf.Abs(y));

                    // Multi-layer Perlin Noise cho địa hình thảo nguyên tự nhiên
                    float baseNoise = Mathf.PerlinNoise((x + 200) * 0.085f, (y + 200) * 0.085f);
                    float detailNoise = Mathf.PerlinNoise((x + 500) * 0.22f, (y + 500) * 0.22f);
                    float flowerNoise = Mathf.PerlinNoise((x + 800) * 0.14f, (y + 800) * 0.14f);

                    Tile selectedTile = grassSteppeTile;

                    // A. Vùng rìa bản đồ sát các hàng cây (distToBorder <= 7.5): Cỏ mọc cao rậm rạp, hoang dã
                    if (distToBorder <= 7.5f)
                    {
                        if (baseNoise > 0.32f) selectedTile = grassTallTile;
                        else if (detailNoise > 0.58f) selectedTile = flower3Tile;
                        else selectedTile = grassSteppeTile;
                    }
                    // B. Vùng bờ Hồ Nước Ốc Đảo (Prairie Oasis: x=21..41, y=-12..8): Cỏ lau sậy & hoa dại mọc tươi tốt quanh bờ
                    else if (x >= 21 && x <= 41 && y >= -12 && y <= 8)
                    {
                        float lakeDist = Mathf.Sqrt(Mathf.Pow((x - 31f) / 7.5f, 2) + Mathf.Pow((y - (-2f)) / 6.0f, 2));
                        if (lakeDist > 0.95f && lakeDist <= 1.45f)
                        {
                            selectedTile = (detailNoise > 0.45f) ? grassTallTile : flower2Tile;
                        }
                        else
                        {
                            selectedTile = (baseNoise > 0.48f) ? grassSteppeTile : grassBaseTile;
                        }
                    }
                    // C. Thảo nguyên mở rộng (Central Prairie): Thảm hoa dại, đồng cỏ mượt mà
                    else
                    {
                        // Thảm hoa dại nở rộ thành từng luống uốn lượn
                        if (flowerNoise > 0.70f)
                        {
                            selectedTile = (detailNoise > 0.48f) ? flower1Tile : flower2Tile;
                        }
                        else if (flowerNoise < 0.22f && detailNoise > 0.62f)
                        {
                            selectedTile = flower3Tile;
                        }
                        // Cỏ cao xen kẽ
                        else if (baseNoise > 0.66f)
                        {
                            selectedTile = grassTallTile;
                        }
                        // Cỏ non mềm mại
                        else if (baseNoise < 0.28f)
                        {
                            selectedTile = grassBaseTile;
                        }
                        else
                        {
                            selectedTile = grassSteppeTile;
                        }
                    }

                    groundTilemap.SetTile(new Vector3Int(x, y, 0), selectedTile);
                }
            }

            // 4. Hồ Nước Tự Nhiên (Prairie Oasis Lake: x=23..39, y=-10..6)
            waterTilemap.ClearAllTiles();
            for (int wx = 23; wx <= 39; wx++)
            {
                for (int wy = -10; wy <= 6; wy++)
                {
                    float dx = (wx - 31f) / 7.5f;
                    float dy = (wy - (-2f)) / 6.0f;
                    if (dx * dx + dy * dy <= 1f)
                    {
                        waterTilemap.SetTile(new Vector3Int(wx, wy, 0), waterTile);
                    }
                }
            }

            // 5. Bãi Bùn Tắm Cho Heo (x=8..16, y=-11..-5)
            mudTilemap.ClearAllTiles();
            for (int mx = 8; mx <= 16; mx++)
            {
                for (int my = -11; my <= -5; my++)
                {
                    float dx = (mx - 12f) / 3.8f;
                    float dy = (my - (-8f)) / 2.8f;
                    if (dx * dx + dy * dy <= 1f)
                    {
                        mudTilemap.SetTile(new Vector3Int(mx, my, 0), mudTile);
                    }
                }
            }

            // 6. Đường Mòn Đất Đỏ (Paths)
            pathTilemap.ClearAllTiles();
            for (int y = -31; y <= 6; y++) pathTilemap.SetTile(new Vector3Int(0, y, 0), trailV);
            for (int x = -13; x <= 0; x++) pathTilemap.SetTile(new Vector3Int(x, 6, 0), trailH);
            for (int x = 0; x <= 7; x++) pathTilemap.SetTile(new Vector3Int(x, -1, 0), trailH);
            for (int x = -30; x <= 25; x++) pathTilemap.SetTile(new Vector3Int(x, -20, 0), trailH);

            // 6.5. Nền Đất Làng Bờ Rìa: Đã được chuyển sang Scene riêng biệt BoRiaVillage2D.unity.
            // MainFarm2D giữ nguyên bản đồ đồng cỏ tự nhiên, chỉ có đường mòn xuống Cổng Nam.

            FarmEnvironment2D.AutoTilePathMap(
                pathTilemap,
                trailV, trailH, trailCross,
                trailTWest, trailTEast, trailTNorth, trailTSouth,
                cornerSW, cornerSE, cornerNW, cornerNE,
                endW, endE, endN, endS
            );

            // 7. Cấu Hình FarmEnvironment2D & Hàng Rào Chuồng Thả Heo (Pasture Bounds 40m x 28m)
            var farmEnv = FarmEnvironment2D.Instance;
            if (farmEnv == null)
            {
                var feObj = GameObject.Find("FarmEnvironment_2D");
                if (feObj == null)
                {
                    feObj = new GameObject("FarmEnvironment_2D");
                }
                farmEnv = feObj.GetComponent<FarmEnvironment2D>();
                if (farmEnv == null)
                {
                    farmEnv = feObj.AddComponent<FarmEnvironment2D>();
                }
            }

            farmEnv.MapBounds = new Rect(-45f, -32f, 90f, 64f);
            farmEnv.PigPastureBounds = new Rect(-20f, -14f, 40f, 28f);
            farmEnv.SetPathTiles(
                trailV, trailH, trailCross,
                trailTWest, trailTEast, trailTNorth, trailTSouth,
                cornerSW, cornerSE, cornerNW, cornerNE,
                endW, endE, endN, endS
            );

            farmEnv.RebuildPastureFences();

            // 8. Cổng Chắn Heo Vô Hình (Pasture Gate Barrier tại Cổng Nam chuồng thả)
            if (GameObject.Find("Pasture_Gate_Barrier") == null)
            {
                var gateObj = new GameObject("Pasture_Gate_Barrier");
                gateObj.transform.position = new Vector3(0.5f, -13.5f, 0);
                var gCol = gateObj.AddComponent<BoxCollider2D>();
                gCol.size = new Vector2(5.0f, 0.8f);
                gateObj.AddComponent<PigGateBarrier>();
            }

            // 9. Công Trình Hạ Tầng Cơ Bản (Buildings & Infrastructure)
            CreateBuildings(farmEnv);

            // 10. Thiên Nhiên & Cây Cối (Trees, Rocks, Flowers)
            CreateNatureDecorations();

            // 11. Nhân Vật Người Chơi Khoa (Player)
            EnsurePlayerExists();

            // 12. Đàn Heo Mẫu Khởi Đầu
            EnsureStartingPigs();

            // 13. Phân Vùng Làng Bờ Rìa & Triggers
            EnsureVillageEntities();

            farmEnv.RefreshInfrastructureRegistries();
            Debug.Log("<color=green>[DefaultMapBuilder] ĐÃ KHỞI TẠO XONG TOÀN BỘ MAP MẶC ĐỊNH & LÀNG BỜ RÌA THÀNH CÔNG!</color>");
        }

        private static Tilemap GetOrCreateTilemap(Transform parent, string name, int sortingOrder)
        {
            var obj = parent.Find(name)?.gameObject;
            if (obj == null)
            {
                obj = new GameObject(name);
                obj.transform.SetParent(parent, false);
            }
            var tm = obj.GetComponent<Tilemap>();
            if (tm == null)
            {
                tm = obj.AddComponent<Tilemap>();
            }
            var tr = obj.GetComponent<TilemapRenderer>();
            if (tr == null)
            {
                tr = obj.AddComponent<TilemapRenderer>();
            }
            tr.sortingOrder = sortingOrder;
            return tm;
        }

        private static (Tile, Tile, Tile, Tile, Tile, Tile) CreateGrassTiles()
        {
            var tSteppe = ScriptableObject.CreateInstance<Tile>();
            tSteppe.sprite = UISpriteLoader.LoadSprite("Art/Tiles/Sprites/grass_steppe.png", 32);

            var tTall = ScriptableObject.CreateInstance<Tile>();
            tTall.sprite = UISpriteLoader.LoadSprite("Art/Tiles/Sprites/grass_tall.png", 32);

            var tBase = ScriptableObject.CreateInstance<Tile>();
            tBase.sprite = UISpriteLoader.LoadSprite("Art/Tiles/Sprites/grass_base.png", 32);

            var tF1 = ScriptableObject.CreateInstance<Tile>();
            tF1.sprite = UISpriteLoader.LoadSprite("Art/Tiles/Sprites/grass_flower_1.png", 32);

            var tF2 = ScriptableObject.CreateInstance<Tile>();
            tF2.sprite = UISpriteLoader.LoadSprite("Art/Tiles/Sprites/grass_flower_2.png", 32);

            var tF3 = ScriptableObject.CreateInstance<Tile>();
            tF3.sprite = UISpriteLoader.LoadSprite("Art/Tiles/Sprites/grass_flower_3.png", 32);

            return (tSteppe, tTall, tBase, tF1, tF2, tF3);
        }

        private static (Tile, Tile, Tile, Tile, Tile, Tile, Tile, Tile, Tile, Tile, Tile, Tile, Tile, Tile, Tile) CreateTrailTiles()
        {
            var v = ScriptableObject.CreateInstance<Tile>(); v.sprite = UISpriteLoader.LoadSprite("Art/Tiles/Sprites/dirt_trail_v.png", 32);
            var h = ScriptableObject.CreateInstance<Tile>(); h.sprite = UISpriteLoader.LoadSprite("Art/Tiles/Sprites/dirt_trail_h.png", 32);
            var cross = ScriptableObject.CreateInstance<Tile>(); cross.sprite = UISpriteLoader.LoadSprite("Art/Tiles/Sprites/dirt_trail_cross.png", 32);
            var tW = ScriptableObject.CreateInstance<Tile>(); tW.sprite = UISpriteLoader.LoadSprite("Art/Tiles/Sprites/dirt_trail_t_west.png", 32);
            var tE = ScriptableObject.CreateInstance<Tile>(); tE.sprite = UISpriteLoader.LoadSprite("Art/Tiles/Sprites/dirt_trail_t_east.png", 32);
            var tN = ScriptableObject.CreateInstance<Tile>(); tN.sprite = UISpriteLoader.LoadSprite("Art/Tiles/Sprites/dirt_trail_t_north.png", 32);
            var tS = ScriptableObject.CreateInstance<Tile>(); tS.sprite = UISpriteLoader.LoadSprite("Art/Tiles/Sprites/dirt_trail_t_south.png", 32);
            var cSW = ScriptableObject.CreateInstance<Tile>(); cSW.sprite = UISpriteLoader.LoadSprite("Art/Tiles/Sprites/dirt_trail_corner_sw.png", 32);
            var cSE = ScriptableObject.CreateInstance<Tile>(); cSE.sprite = UISpriteLoader.LoadSprite("Art/Tiles/Sprites/dirt_trail_corner_se.png", 32);
            var cNW = ScriptableObject.CreateInstance<Tile>(); cNW.sprite = UISpriteLoader.LoadSprite("Art/Tiles/Sprites/dirt_trail_corner_nw.png", 32);
            var cNE = ScriptableObject.CreateInstance<Tile>(); cNE.sprite = UISpriteLoader.LoadSprite("Art/Tiles/Sprites/dirt_trail_corner_ne.png", 32);
            var eW = ScriptableObject.CreateInstance<Tile>(); eW.sprite = UISpriteLoader.LoadSprite("Art/Tiles/Sprites/dirt_trail_end_w.png", 32);
            var eE = ScriptableObject.CreateInstance<Tile>(); eE.sprite = UISpriteLoader.LoadSprite("Art/Tiles/Sprites/dirt_trail_end_e.png", 32);
            var eN = ScriptableObject.CreateInstance<Tile>(); eN.sprite = UISpriteLoader.LoadSprite("Art/Tiles/Sprites/dirt_trail_end_n.png", 32);
            var eS = ScriptableObject.CreateInstance<Tile>(); eS.sprite = UISpriteLoader.LoadSprite("Art/Tiles/Sprites/dirt_trail_end_s.png", 32);

            return (v, h, cross, tW, tE, tN, tS, cSW, cSE, cNW, cNE, eW, eE, eN, eS);
        }

        private static void CreateBuildings(FarmEnvironment2D farmEnv)
        {
            // Mái Trú (Shelter Barn) tại góc Tây Bắc (-13, 8.5)
            if (GameObject.Find("Shelter_Zone") == null)
            {
                var sObj = new GameObject("Shelter_Zone");
                sObj.transform.position = new Vector3(-13f, 8.5f, 0);
                var sr = sObj.AddComponent<SpriteRenderer>();
                sr.sprite = UISpriteLoader.GetBarnSprite();
                sr.sortingOrder = Mathf.RoundToInt(-(sObj.transform.position.y - 1.28f) * 100);
                sObj.transform.localScale = new Vector3(1.5f, 1.5f, 1f);

                var sCol = sObj.AddComponent<BoxCollider2D>();
                sCol.size = new Vector2(3.8f, 1.0f);
                sCol.offset = new Vector2(0f, -0.85f);
                sCol.isTrigger = false;

                var tCol = sObj.AddComponent<BoxCollider2D>();
                tCol.size = new Vector2(4.0f, 0.8f);
                tCol.offset = new Vector2(0f, -1.35f);
                tCol.isTrigger = true;

                sObj.AddComponent<Shelter2DView>();
            }

            // Máng Ăn (Feeders)
            if (GameObject.Find("Feeder_1") == null)
            {
                var f1 = farmEnv.BuildFeeder(new Vector2Int(-6, 3));
                f1.name = "Feeder_1";
            }
            if (GameObject.Find("Feeder_2") == null)
            {
                var f2 = farmEnv.BuildFeeder(new Vector2Int(2, 5));
                f2.name = "Feeder_2";
            }

            // Bồn Nước (Water Troughs)
            if (GameObject.Find("WaterTrough_1") == null)
            {
                var w1 = farmEnv.BuildWaterTrough(new Vector2Int(7, -1));
                w1.name = "WaterTrough_1";
            }
            if (GameObject.Find("WaterTrough_2") == null)
            {
                var w2 = farmEnv.BuildWaterTrough(new Vector2Int(12, 4));
                w2.name = "WaterTrough_2";
            }

            // Giếng Nước & Kho Cám Nông Trại
            if (GameObject.Find("WaterWell_Main") == null)
            {
                var wellObj = new GameObject("WaterWell_Main");
                wellObj.transform.position = new Vector3(6f, -17.5f, 0);
                var sr = wellObj.AddComponent<SpriteRenderer>();
                sr.sprite = UISpriteLoader.GetWaterWellSprite();
                sr.sortingOrder = Mathf.RoundToInt(-wellObj.transform.position.y * 100);
                wellObj.transform.localScale = new Vector3(1.4f, 1.4f, 1f);
                var col = wellObj.AddComponent<BoxCollider2D>();
                col.size = new Vector2(1.5f, 1.2f);
                wellObj.AddComponent<WaterWell2DView>();
            }

            if (GameObject.Find("FeedSilo_Main") == null)
            {
                var siloObj = new GameObject("FeedSilo_Main");
                siloObj.transform.position = new Vector3(-8.5f, -17.5f, 0);
                var sr = siloObj.AddComponent<SpriteRenderer>();
                sr.sprite = UISpriteLoader.GetFeedSiloSprite();
                sr.sortingOrder = Mathf.RoundToInt(-siloObj.transform.position.y * 100);
                siloObj.transform.localScale = new Vector3(1.5f, 1.5f, 1f);
                var col = siloObj.AddComponent<BoxCollider2D>();
                col.size = new Vector2(1.8f, 1.4f);
                siloObj.AddComponent<FeedSilo2DView>();
            }

            // Vạt Bùn Tắm
            if (GameObject.Find("MudPit_Zone") == null)
            {
                var mudObj = new GameObject("MudPit_Zone");
                mudObj.transform.position = new Vector3(12f, -8f, 0);
                var col = mudObj.AddComponent<BoxCollider2D>();
                col.size = new Vector2(8f, 5f);
                col.isTrigger = true;
                mudObj.AddComponent<MudPit2DView>();
            }

            // Khu Xử Lý Cách Ly (Corpse Lot)
            if (GameObject.Find("CorpseLot_Zone") == null)
            {
                var corpseObj = new GameObject("CorpseLot_Zone");
                corpseObj.transform.position = new Vector3(-30f, -20f, 0);
                var sr = corpseObj.AddComponent<SpriteRenderer>();
                sr.sprite = UISpriteLoader.GetCorpseLotSprite();
                sr.sortingOrder = Mathf.RoundToInt(-corpseObj.transform.position.y * 100);
                corpseObj.transform.localScale = new Vector3(5f, 3.5f, 1f);
                corpseObj.AddComponent<CorpseLot2DView>();
            }

            // Tháp Phòng Thủ (Đồi cao Đông Bắc)
            if (GameObject.Find("DefenseTower (NoXuyenVan)") == null)
            {
                var towerObj = new GameObject("DefenseTower (NoXuyenVan)");
                towerObj.transform.position = new Vector3(32f, 20f, 0);
                var sr = towerObj.AddComponent<SpriteRenderer>();
                sr.sprite = UISpriteLoader.GetDefenseTowerSprite();
                sr.sortingOrder = Mathf.RoundToInt(-(towerObj.transform.position.y - 1.12f) * 100);
                towerObj.transform.localScale = new Vector3(2.4f, 3.2f, 1f);
                var col = towerObj.AddComponent<CircleCollider2D>();
                col.offset = new Vector2(0f, -0.35f);
                col.radius = 0.4f;
                towerObj.AddComponent<DefenseTower2DView>();
            }
        }

        private static void CreateNatureDecorations()
        {
            var natureGroup = GameObject.Find("Prairie_Nature");
            if (natureGroup != null)
            {
                if (Application.isPlaying) GameObject.Destroy(natureGroup);
                else GameObject.DestroyImmediate(natureGroup);
            }

            natureGroup = new GameObject("Prairie_Nature");
            var treeSp = UISpriteLoader.LoadSprite("Art/Sprites/Environment/Nature/tree_prairie.png", 32);
            var rockSp = UISpriteLoader.LoadSprite("Art/Sprites/Environment/Nature/rock_boulder.png", 32);
            var flowerSp = UISpriteLoader.LoadSprite("Art/Sprites/Environment/Nature/flower_patch.png", 32);

            // 1. RÌA BẢN ĐỒ: RỪNG CÂY RẬM RẠP NHIỀU HÀNG BAO QUANH NÔNG TRẠI (Dense Forest Borders)
            CreateDenseForestPerimeter(natureGroup.transform, treeSp, rockSp, flowerSp);

            // 2. CÂY BÓNG MÁT TRONG BÃI THẢ HEO (Pasture Shade Trees)
            CreatePrairieTree(natureGroup.transform, treeSp, new Vector3(-6f, 11f, 0));
            CreatePrairieTree(natureGroup.transform, treeSp, new Vector3(15f, 9f, 0));
            CreatePrairieTree(natureGroup.transform, treeSp, new Vector3(-15f, -4f, 0));
            CreatePrairieTree(natureGroup.transform, treeSp, new Vector3(4f, -9f, 0));

            // 3. CÁC RỪNG CÂY & GÒ ĐỒI ĐẶC TRƯNG NGOÀI THẢO NGUYÊN (Interior Woodlands & Landmarks)
            // A. Rừng cây Tây Bắc (North-West Woodlands)
            CreatePrairieTree(natureGroup.transform, treeSp, new Vector3(-28f, 16f, 0));
            CreatePrairieTree(natureGroup.transform, treeSp, new Vector3(-34f, 20f, 0));
            CreatePrairieTree(natureGroup.transform, treeSp, new Vector3(-30f, 24f, 0));
            CreatePrairieTree(natureGroup.transform, treeSp, new Vector3(-24f, 22f, 0));

            // B. Rừng cây gò đồi Tháp Nỏ Xuyên Vân (Northeast Defense Hill Grove)
            CreatePrairieTree(natureGroup.transform, treeSp, new Vector3(25f, 23f, 0));
            CreatePrairieTree(natureGroup.transform, treeSp, new Vector3(36f, 23f, 0));
            CreatePrairieTree(natureGroup.transform, treeSp, new Vector3(37f, 16f, 0));
            CreatePrairieTree(natureGroup.transform, treeSp, new Vector3(24f, 15f, 0));

            // C. Rặng cây ven Hồ Nước Ốc Đảo (Oasis Lakeside Grove)
            CreatePrairieTree(natureGroup.transform, treeSp, new Vector3(21f, 7f, 0));
            CreatePrairieTree(natureGroup.transform, treeSp, new Vector3(38f, 7f, 0));
            CreatePrairieTree(natureGroup.transform, treeSp, new Vector3(21f, -11f, 0));
            CreatePrairieTree(natureGroup.transform, treeSp, new Vector3(38f, -11f, 0));

            // D. Rừng cây u ám bao quanh Khu Cách Ly (Corpse Lot Grove)
            CreatePrairieTree(natureGroup.transform, treeSp, new Vector3(-34f, -16f, 0));
            CreatePrairieTree(natureGroup.transform, treeSp, new Vector3(-24f, -25f, 0));
            CreatePrairieTree(natureGroup.transform, treeSp, new Vector3(-32f, -26f, 0));

            // E. Cây ven thảo nguyên phía Nam (South Prairie Groves)
            CreatePrairieTree(natureGroup.transform, treeSp, new Vector3(-16f, -24f, 0));
            CreatePrairieTree(natureGroup.transform, treeSp, new Vector3(16f, -24f, 0));
            CreatePrairieTree(natureGroup.transform, treeSp, new Vector3(28f, -20f, 0));

            // 4. ĐẠI LỘ CÂY DỌC ĐƯỜNG MÒN XUỐNG LÀNG BỜ RÌA (Avenue of Trees towards Bờ Rìa)
            CreatePrairieTree(natureGroup.transform, treeSp, new Vector3(-4.5f, -20f, 0));
            CreatePrairieTree(natureGroup.transform, treeSp, new Vector3(-4.5f, -24f, 0));
            CreatePrairieTree(natureGroup.transform, treeSp, new Vector3(-4.5f, -28f, 0));
            CreatePrairieTree(natureGroup.transform, treeSp, new Vector3(4.5f, -20f, 0));
            CreatePrairieTree(natureGroup.transform, treeSp, new Vector3(4.5f, -24f, 0));
            CreatePrairieTree(natureGroup.transform, treeSp, new Vector3(4.5f, -28f, 0));

            // 5. TẢNG ĐÁ RÊU PHONG (Prairie Rocks)
            CreatePrairieBoulder(natureGroup.transform, rockSp, new Vector3(-8f, -6f, 0));
            CreatePrairieBoulder(natureGroup.transform, rockSp, new Vector3(5f, -6f, 0));
            CreatePrairieBoulder(natureGroup.transform, rockSp, new Vector3(-34f, 4f, 0));
            CreatePrairieBoulder(natureGroup.transform, rockSp, new Vector3(22f, 10f, 0));
            CreatePrairieBoulder(natureGroup.transform, rockSp, new Vector3(20f, -14f, 0));
            CreatePrairieBoulder(natureGroup.transform, rockSp, new Vector3(-24f, -24f, 0));
            CreatePrairieBoulder(natureGroup.transform, rockSp, new Vector3(34f, -6f, 0));

            // 6. CÁC BỤI HOA DẠI (Flower Bushes)
            CreatePrairieFlowerPatch(natureGroup.transform, flowerSp, new Vector3(-3f, 1f, 0));
            CreatePrairieFlowerPatch(natureGroup.transform, flowerSp, new Vector3(4f, 1f, 0));
            CreatePrairieFlowerPatch(natureGroup.transform, flowerSp, new Vector3(-25f, 10f, 0));
            CreatePrairieFlowerPatch(natureGroup.transform, flowerSp, new Vector3(24f, 12f, 0));
            CreatePrairieFlowerPatch(natureGroup.transform, flowerSp, new Vector3(-12f, -22f, 0));
            CreatePrairieFlowerPatch(natureGroup.transform, flowerSp, new Vector3(8f, -22f, 0));
            CreatePrairieFlowerPatch(natureGroup.transform, flowerSp, new Vector3(0f, 22f, 0));
        }

        private static void CreateDenseForestPerimeter(Transform parent, Sprite treeSp, Sprite rockSp, Sprite flowerSp)
        {
            float treeStep = 3.6f;

            // 1. RÌA BẮC (North Forest Border: 3 hàng cây so le rậm rạp)
            for (float x = -47f; x <= 47f; x += treeStep)
            {
                CreatePrairieTree(parent, treeSp, new Vector3(x, 33.5f, 0));
            }
            for (float x = -45.2f; x <= 45.2f; x += treeStep)
            {
                CreatePrairieTree(parent, treeSp, new Vector3(x, 31.0f, 0));
            }
            for (float x = -47f; x <= 47f; x += 4.5f)
            {
                float jitter = Mathf.Sin(x * 1.7f) * 0.4f;
                CreatePrairieTree(parent, treeSp, new Vector3(x, 28.5f + jitter, 0));
            }

            // 2. RÌA NAM (South Forest Border: 3 hàng cây, chừa lối đi |x| < 3.8m cho đường xuống Làng Bờ Rìa)
            for (float x = -47f; x <= 47f; x += treeStep)
            {
                if (Mathf.Abs(x) < 3.8f) continue;
                CreatePrairieTree(parent, treeSp, new Vector3(x, -33.5f, 0));
            }
            for (float x = -45.2f; x <= 45.2f; x += treeStep)
            {
                if (Mathf.Abs(x) < 3.8f) continue;
                CreatePrairieTree(parent, treeSp, new Vector3(x, -31.0f, 0));
            }
            for (float x = -47f; x <= 47f; x += 4.5f)
            {
                if (Mathf.Abs(x) < 3.8f) continue;
                float jitter = Mathf.Cos(x * 1.5f) * 0.4f;
                CreatePrairieTree(parent, treeSp, new Vector3(x, -28.5f + jitter, 0));
            }

            // 3. RÌA TÂY (West Forest Border: 3 hàng cây)
            for (float y = -31f; y <= 31f; y += treeStep)
            {
                CreatePrairieTree(parent, treeSp, new Vector3(-47.0f, y, 0));
            }
            for (float y = -29.2f; y <= 29.2f; y += treeStep)
            {
                CreatePrairieTree(parent, treeSp, new Vector3(-44.5f, y, 0));
            }
            for (float y = -28f; y <= 28f; y += 4.5f)
            {
                float jitter = Mathf.Sin(y * 1.8f) * 0.4f;
                CreatePrairieTree(parent, treeSp, new Vector3(-41.8f + jitter, y, 0));
            }

            // 4. RÌA ĐÔNG (East Forest Border: 3 hàng cây bao bọc bên ngoài hồ nước)
            for (float y = -31f; y <= 31f; y += treeStep)
            {
                CreatePrairieTree(parent, treeSp, new Vector3(47.0f, y, 0));
            }
            for (float y = -29.2f; y <= 29.2f; y += treeStep)
            {
                CreatePrairieTree(parent, treeSp, new Vector3(44.5f, y, 0));
            }
            for (float y = -28f; y <= 28f; y += 4.5f)
            {
                float jitter = Mathf.Cos(y * 1.6f) * 0.4f;
                CreatePrairieTree(parent, treeSp, new Vector3(41.8f + jitter, y, 0));
            }

            // 5. ĐIỂM XUYẾT ĐÁ TẢNG & BỤI HOA VEN BÌA RỪNG (Natural Boulders & Flower Edge)
            CreatePrairieBoulder(parent, rockSp, new Vector3(-40f, 26f, 0));
            CreatePrairieBoulder(parent, rockSp, new Vector3(39f, 26f, 0));
            CreatePrairieBoulder(parent, rockSp, new Vector3(-40f, -26f, 0));
            CreatePrairieBoulder(parent, rockSp, new Vector3(39f, -26f, 0));
            CreatePrairieFlowerPatch(parent, flowerSp, new Vector3(-38f, 27f, 0));
            CreatePrairieFlowerPatch(parent, flowerSp, new Vector3(37f, 27f, 0));
            CreatePrairieFlowerPatch(parent, flowerSp, new Vector3(-38f, -27f, 0));
            CreatePrairieFlowerPatch(parent, flowerSp, new Vector3(37f, -27f, 0));
        }

        private static void CreatePrairieTree(Transform parent, Sprite sp, Vector3 pos)
        {
            var go = new GameObject("Prairie_Tree");
            go.transform.SetParent(parent, false);
            go.transform.position = pos;
            go.transform.localScale = new Vector3(2f, 2f, 1f);

            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = sp;
            sr.sortingOrder = Mathf.RoundToInt(-(pos.y - 1.53f) * 100);

            var col = go.AddComponent<CircleCollider2D>();
            col.offset = new Vector2(0f, -0.85f);
            col.radius = 0.22f;
        }

        private static void CreatePrairieBoulder(Transform parent, Sprite sp, Vector3 pos)
        {
            var go = new GameObject("Prairie_Rock");
            go.transform.SetParent(parent, false);
            go.transform.position = pos;
            go.transform.localScale = new Vector3(1.3f, 1.3f, 1f);

            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = sp;
            sr.sortingOrder = Mathf.RoundToInt(-(pos.y - 0.14f) * 100);

            var col = go.AddComponent<CircleCollider2D>();
            col.offset = new Vector2(0f, -0.1f);
            col.radius = 0.35f;
        }

        private static void CreatePrairieFlowerPatch(Transform parent, Sprite sp, Vector3 pos)
        {
            var go = new GameObject("Flower_Bush");
            go.transform.SetParent(parent, false);
            go.transform.position = pos;

            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = sp;
            sr.sortingOrder = Mathf.RoundToInt(-(pos.y - 0.25f) * 100);

            var col = go.AddComponent<BoxCollider2D>();
            col.isTrigger = true;
            go.AddComponent<FlowerBush2D>();
        }

        private static void EnsurePlayerExists()
        {
            var player = GameObject.Find("Player (Khoa)");
            if (player == null)
            {
                player = new GameObject("Player (Khoa)");
                player.tag = "Player";
                player.transform.position = new Vector3(0f, 0f, 0f);

                var sr = player.AddComponent<SpriteRenderer>();
                sr.sprite = UISpriteLoader.LoadSprite("Art/Sprites/Characters/Player/player_down_idle.png", 32);
                sr.sortingOrder = 0;

                var rb = player.AddComponent<Rigidbody2D>();
                rb.gravityScale = 0f;
                rb.freezeRotation = true;
                rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

                var col = player.AddComponent<BoxCollider2D>();
                col.size = new Vector2(0.6f, 0.4f);
                col.offset = new Vector2(0f, -0.3f);

                player.AddComponent<PlayerMobileController>();
                player.AddComponent<PlayerInteractionController>();
                player.AddComponent<PlayerCombatController>();
            }
        }

        private static void EnsureStartingPigs()
        {
            var existingPigs = GameObject.FindObjectsByType<PigAgentView>(FindObjectsSortMode.None);
            if (existingPigs != null && existingPigs.Length > 0) return;

            var player = GameObject.FindWithTag("Player")?.transform;
            var engine = MobileGameController.Instance?.Engine;

            var p1Obj = new GameObject("Pig_1 (Hồng Điền #1)");
            p1Obj.transform.position = new Vector3(-2f, 2f, 0);
            SetupPigAgent(p1Obj, "hong_dien", "p_1", "Hồng Điền #1", GeneLineId.HongDien, GeneRarity.Thuong, player, engine);

            var p2Obj = new GameObject("Pig_2 (Kim Thọ #1)");
            p2Obj.transform.position = new Vector3(3f, -1f, 0);
            SetupPigAgent(p2Obj, "kim_tho", "p_2", "Kim Thọ #1", GeneLineId.KimTho, GeneRarity.HuyenThoai, player, engine);
        }

        private static void SetupPigAgent(GameObject go, string folder, string pigId, string pigName, GeneLineId gene, GeneRarity rarity, Transform player, GameEngine engine)
        {
            var sr = go.AddComponent<SpriteRenderer>();
            var pIdle = UISpriteLoader.LoadSprite($"Art/Sprites/Pigs/{folder}/idle.png", 16);
            var pWalk1 = UISpriteLoader.LoadSprite($"Art/Sprites/Pigs/{folder}/walk1.png", 16);
            if (pWalk1 == null) pWalk1 = pIdle;
            var pWalk2 = UISpriteLoader.LoadSprite($"Art/Sprites/Pigs/{folder}/walk2.png", 16);
            if (pWalk2 == null) pWalk2 = pIdle;
            var pEat = UISpriteLoader.LoadSprite($"Art/Sprites/Pigs/{folder}/eat.png", 16);
            if (pEat == null) pEat = pIdle;
            var pSleep = UISpriteLoader.LoadSprite($"Art/Sprites/Pigs/{folder}/sleep.png", 16);
            if (pSleep == null) pSleep = pIdle;
            sr.sprite = pIdle;
            go.transform.localScale = new Vector3(1.2f, 1.2f, 1f);

            var rb = go.AddComponent<Rigidbody2D>();
            rb.gravityScale = 0f;
            rb.freezeRotation = true;

            var col = go.AddComponent<CircleCollider2D>();
            col.offset = new Vector2(0f, -0.15f);
            col.radius = 0.32f;

            var agent = go.AddComponent<PigAgentView>();
            agent.SpriteRenderer = sr;
            agent.EnsureThoughtBubble();

            var anim = go.AddComponent<PigSpriteAnimator>();
            anim.SetBreedSprites(pIdle, new[] { pWalk1, pWalk2 }, pEat, pSleep);

            Pig model = null;
            if (engine != null)
            {
                model = engine.Pigs.Find(p => p.Id == pigId);
            }
            if (model == null)
            {
                model = Pig.CreateDefault(pigId, pigName, gene, rarity);
                if (engine != null && !engine.Pigs.Contains(model)) engine.Pigs.Add(model);
            }

            agent.Bind(model, player);
        }

        public static void BuildVillageTerrain(
            Tilemap groundTilemap, Tilemap pathTilemap, Tilemap mudTilemap,
            Tile grassSteppeTile, Tile grassTallTile, Tile grassBaseTile, Tile flower3Tile,
            Tile trailV, Tile trailH, Tile mudTile)
        {
            // 1. Nền Đất Làng Bờ Rìa (x: 94..146, y: -29..-4)
            // Khí hậu u ám, đất xám tro & cỏ úa mục nát
            for (int x = 94; x <= 146; x++)
            {
                for (int y = -29; y <= -4; y++)
                {
                    float distToBorder = Mathf.Min(x - 94, 146 - x, y - (-29), -4 - y);
                    float noise = Mathf.PerlinNoise((x + 700) * 0.12f, (y + 700) * 0.12f);

                    Tile sel = grassSteppeTile;
                    if (distToBorder <= 2.5f)
                    {
                        sel = (noise > 0.4f) ? grassTallTile : grassSteppeTile;
                    }
                    else if (noise > 0.65f)
                    {
                        sel = flower3Tile; // Cỏ gai & hoa dại úa tàn vùng ranh giới
                    }
                    else if (noise < 0.35f)
                    {
                        sel = grassBaseTile;
                    }

                    groundTilemap.SetTile(new Vector3Int(x, y, 0), sel);
                }
            }

            // 2. Mạng Lưới Đường Mòn & Quảng Trường Đất Nện Làng Bờ Rìa (Paths)
            // A. Đường trục chính Đông - Tây từ Cổng Làng qua trung tâm (x: 97..124, y: -16)
            for (int x = 97; x <= 124; x++)
            {
                pathTilemap.SetTile(new Vector3Int(x, -16, 0), trailH);
            }
            // Quảng trường trung tâm làng mở rộng (x: 110..116, y: -17..-15)
            for (int x = 110; x <= 116; x++)
            {
                for (int y = -17; y <= -15; y++)
                {
                    pathTilemap.SetTile(new Vector3Int(x, y, 0), trailH);
                }
            }

            // B. Nhánh Bắc lên Quán Bà Sáu (x: 108, y: -15..-13)
            for (int y = -15; y <= -13; y++) pathTilemap.SetTile(new Vector3Int(108, y, 0), trailV);

            // C. Nhánh Bắc lên Xưởng Mộc Tám Vá (x: 118, y: -15..-11)
            for (int y = -15; y <= -11; y++) pathTilemap.SetTile(new Vector3Int(118, y, 0), trailV);

            // D. Nhánh Nam xuống Tiệm Thuốc Cô Dung (x: 110, y: -21..-17)
            for (int y = -21; y <= -17; y++) pathTilemap.SetTile(new Vector3Int(110, y, 0), trailV);

            // E. Nhánh Nam xuống Trạm Cân "6+1" (x: 120..123, y: -18..-17)
            for (int x = 120; x <= 123; x++) pathTilemap.SetTile(new Vector3Int(x, -17, 0), trailH);
            for (int y = -18; y <= -17; y++) pathTilemap.SetTile(new Vector3Int(122, y, 0), trailV);

            // F. Lối mòn uốn lượn xuống Gốc Đa Ranh Giới (Bí Nhân 25m)
            // Từ (123, -18) đi chéo xuống (138, -25)
            int[,] trailSteps = new int[,] {
                { 124, -18 }, { 125, -19 }, { 126, -19 }, { 127, -20 },
                { 128, -20 }, { 129, -21 }, { 130, -21 }, { 131, -22 },
                { 132, -22 }, { 133, -23 }, { 134, -23 }, { 135, -24 },
                { 136, -24 }, { 137, -25 }, { 138, -25 }
            };
            for (int i = 0; i < trailSteps.GetLength(0); i++)
            {
                pathTilemap.SetTile(new Vector3Int(trailSteps[i, 0], trailSteps[i, 1], 0), trailH);
            }

            // 3. Vũng Bùn Đất Đỏ & Sình Lầy (Mud Pits in Village)
            // A. Vũng bùn trơn trượt đầu dốc cổng làng (x: 99..101, y: -17..-15)
            for (int x = 99; x <= 101; x++)
            {
                for (int y = -17; y <= -15; y++)
                {
                    mudTilemap.SetTile(new Vector3Int(x, y, 0), mudTile);
                }
            }
            // B. Vũng bùn giữa quảng trường cạnh giếng/quán nước (x: 113..115, y: -18..-17)
            for (int x = 113; x <= 115; x++)
            {
                for (int y = -18; y <= -17; y++)
                {
                    mudTilemap.SetTile(new Vector3Int(x, y, 0), mudTile);
                }
            }
            // C. Bãi sình lầy khu nhốt cân heo Trạm Cân (x: 120..123, y: -20..-19)
            for (int x = 120; x <= 123; x++)
            {
                for (int y = -20; y <= -19; y++)
                {
                    mudTilemap.SetTile(new Vector3Int(x, y, 0), mudTile);
                }
            }
            // D. Bãi sình lầy âm u ven Gốc Đa Ranh Giới (x: 135..137, y: -26..-25)
            for (int x = 135; x <= 137; x++)
            {
                for (int y = -26; y <= -25; y++)
                {
                    mudTilemap.SetTile(new Vector3Int(x, y, 0), mudTile);
                }
            }
        }

        public static void EnsureVillageEntities()
        {
            // 0. Đảm bảo TransitionZoneController tồn tại để quản lý chuyển cảnh & cốt truyện
            if (TransitionZoneController.Instance == null)
            {
                var tzcObj = GameObject.Find("TransitionZoneController");
                if (tzcObj == null)
                {
                    tzcObj = new GameObject("TransitionZoneController");
                    tzcObj.AddComponent<TransitionZoneController>();
                }
            }

            // 1. Cổng Nam Trang Trại (Khu vực chuyển cảnh tại cuối đường mòn đất đỏ)
            var tf = GameObject.Find("Trigger_Farm_To_Village");
            if (tf == null)
            {
                tf = new GameObject("Trigger_Farm_To_Village");
                tf.transform.position = new Vector3(0f, -29.5f, 0f);
                var col = tf.AddComponent<BoxCollider2D>();
                col.isTrigger = true;
                col.size = new Vector2(5f, 3f);
                var listener = tf.AddComponent<TransitionTriggerListener>();
                listener.IsTriggerToVillage = true;

                // Biển gỗ chỉ dẫn đặt bên lối đi xuyên rừng
                var sign = new GameObject("Sign_To_Village");
                sign.transform.SetParent(tf.transform, false);
                sign.transform.localPosition = new Vector3(-2.8f, 0.5f, 0);
                var sr = sign.AddComponent<SpriteRenderer>();
                sr.sprite = UISpriteLoader.LoadSprite("Art/UI/Frames/frame_wood_panel.png", 64);
                sr.sortingOrder = 10;
                sign.transform.localScale = new Vector3(0.5f, 0.5f, 1f);
            }
            else
            {
                tf.transform.position = new Vector3(0f, -29.5f, 0f);
            }

            // 2. Khu Vực Làng Bờ Rìa: Làng Bờ Rìa là Scene riêng biệt (BoRiaVillage2D.unity).
            // Dọn dẹp đối tượng Village_Area_BoRia tạm bợ cũ trong MainFarm2D nếu còn tồn tại.
            var villageRoot = GameObject.Find("Village_Area_BoRia");
            if (villageRoot != null)
            {
                if (Application.isPlaying) UnityEngine.Object.Destroy(villageRoot);
                else UnityEngine.Object.DestroyImmediate(villageRoot);
            }
        }

        private static void BuildVillageEnvironment(Transform villageRoot)
        {
            // Kiểm tra nếu các trạm đã tồn tại đầy đủ thì không tạo lại
            if (villageRoot.Find("Trigger_Village_To_Farm") != null &&
                villageRoot.Find("Station_BaSau") != null &&
                villageRoot.Find("Station_TamVa") != null &&
                villageRoot.Find("Station_CoDung") != null &&
                villageRoot.Find("Station_AnhCa") != null &&
                villageRoot.Find("Station_BiNhan") != null)
            {
                return;
            }

            // Xóa các con cũ nếu đang tái tạo (clean build)
            for (int i = villageRoot.childCount - 1; i >= 0; i--)
            {
                var child = villageRoot.GetChild(i);
                if (Application.isPlaying) UnityEngine.Object.Destroy(child.gameObject);
                else UnityEngine.Object.DestroyImmediate(child.gameObject);
            }

            // 1. CỔNG LÀNG & ĐIỂM TRỞ VỀ TRANG TRẠI (Return Gateway at x=100, y=-16)
            CreateVillageReturnGateway(villageRoot);

            // 2. HÀNG RÀO & RỪNG CÂY BAO QUANH LÀNG (Village Perimeter Enclosure)
            CreateVillagePerimeter(villageRoot);

            // 3. QUẢNG TRƯỜNG TRUNG TÂM & BẢNG HƯƠNG ƯỚC LÀNG (Square & Notice Board)
            CreateVillageSquare(villageRoot);

            // 4. NĂM GIAN HÀNG / TRẠM DÂN LÀNG THEO CỐT TRUYỆN:
            // A. Quán Bà Sáu (Bếp Ăn & Nước Chè Đầu Dốc) - x=108, y=-13 (Local: 8, 3)
            CreateStationBaSau(villageRoot, new Vector3(8f, 3f, 0));

            // B. Xưởng Mộc Tám Vá (Vá Rào Chống Quái) - x=118, y=-10 (Local: 18, 6)
            CreateStationTamVa(villageRoot, new Vector3(18f, 6f, 0));

            // C. Tiệm Thuốc Thú Y Cô Dung - x=110, y=-22 (Local: 10, -6)
            CreateStationCoDung(villageRoot, new Vector3(10f, -6f, 0));

            // D. Trạm Cân "6+1" (Anh Ca & Ông Tồn) - x=122, y=-18 (Local: 22, -2)
            CreateStationAnhCa(villageRoot, new Vector3(22f, -2f, 0));

            // E. Gốc Đa Ranh Giới (Bí Nhân 25m - Outskirts) - x=138, y=-25 (Local: 38, -9)
            CreateStationBiNhan(villageRoot, new Vector3(38f, -9f, 0));
        }

        private static void CreateVillageReturnGateway(Transform parent)
        {
            var tv = new GameObject("Trigger_Village_To_Farm");
            tv.transform.SetParent(parent, false);
            tv.transform.localPosition = Vector3.zero;

            var colV = tv.AddComponent<BoxCollider2D>();
            colV.isTrigger = true;
            colV.size = new Vector2(4.5f, 3f);
            var lV = tv.AddComponent<TransitionTriggerListener>();
            lV.IsTriggerToVillage = false;

            // Cột gỗ hai bên cổng
            CreateSpriteProp(tv.transform, "GatePost_Left", "Art/Sprites/Environment/Fences/fence_post.png", new Vector3(-1.8f, 0f, 0), 1f, Color.white, 50);
            CreateSpriteProp(tv.transform, "GatePost_Right", "Art/Sprites/Environment/Fences/fence_post.png", new Vector3(1.8f, 0f, 0), 1f, Color.white, 50);

            // Rào gãy xiêu vẹo cạnh cổng
            CreateSpriteProp(tv.transform, "Fence_Broken_L", "Art/Sprites/Environment/Fences/fence_broken.png", new Vector3(-2.9f, 0f, 0), 1f, new Color(0.7f, 0.65f, 0.6f), 45);
            CreateSpriteProp(tv.transform, "Fence_Broken_R", "Art/Sprites/Environment/Fences/fence_broken.png", new Vector3(2.9f, 0f, 0), 1f, new Color(0.7f, 0.65f, 0.6f), 45);

            // Đuốc mờ leo lét gắn cột cổng
            CreateSpriteProp(tv.transform, "Lantern_Gate", "Art/Sprites/VFX/Telegraphs/vfx_torch_fire.png", new Vector3(-1.8f, 1.1f, 0), 0.8f, new Color(1f, 0.85f, 0.5f, 0.9f), 100);

            // Biển gỗ chỉ dẫn
            CreateFloatingSignboard(tv.transform, "Sign_To_Farm", new Vector3(0f, 2.0f, 0), "<b>ĐƯỜNG VỀ TRANG TRẠI</b>\n<size=70%><color=#d1d5db>Dốc Đất Đỏ Sương Mù</color></size>", new Color(0.7f, 0.55f, 0.4f));
        }

        private static void CreateVillagePerimeter(Transform parent)
        {
            var perimObj = new GameObject("Village_Perimeter");
            perimObj.transform.SetParent(parent, false);

            var treeSp = UISpriteLoader.LoadSprite("Art/Sprites/Environment/Nature/tree_prairie.png", 64);
            var rockSp = UISpriteLoader.LoadSprite("Art/Sprites/Environment/Nature/rock_boulder.png", 32);

            Color treeTint = new Color(0.52f, 0.58f, 0.50f); // Tông xám lạnh u ám Bờ Rìa
            Color rockTint = new Color(0.48f, 0.48f, 0.52f);

            // Hàng cây bờ Bắc (Local y: 11..12, world y: -5..-4)
            for (float lx = -6f; lx <= 46f; lx += 3.5f)
            {
                CreateDarkTree(perimObj.transform, treeSp, new Vector3(lx, 11f, 0), treeTint);
            }
            // Hàng cây bờ Nam (Local y: -13..-14, world y: -29..-30)
            for (float lx = -6f; lx <= 46f; lx += 3.5f)
            {
                CreateDarkTree(perimObj.transform, treeSp, new Vector3(lx, -13f, 0), treeTint);
            }
            // Hàng cây bờ Tây (Local x: -6, chừa khoảng trống y in [-1.5, 1.5] cho cổng vào)
            for (float ly = -12f; ly <= 10f; ly += 3.2f)
            {
                if (Mathf.Abs(ly) < 2.0f) continue;
                CreateDarkTree(perimObj.transform, treeSp, new Vector3(-6f, ly, 0), treeTint);
            }
            // Hàng cây bờ Đông (Local x: 46)
            for (float ly = -12f; ly <= 10f; ly += 3.2f)
            {
                CreateDarkTree(perimObj.transform, treeSp, new Vector3(46f, ly, 0), treeTint);
            }

            // Tảng đá rêu góc ranh giới
            CreateDarkBoulder(perimObj.transform, rockSp, new Vector3(-5f, 9.5f, 0), rockTint);
            CreateDarkBoulder(perimObj.transform, rockSp, new Vector3(-5f, -11.5f, 0), rockTint);
            CreateDarkBoulder(perimObj.transform, rockSp, new Vector3(45f, 9.5f, 0), rockTint);
            CreateDarkBoulder(perimObj.transform, rockSp, new Vector3(45f, -11.5f, 0), rockTint);
        }

        private static void CreateVillageSquare(Transform parent)
        {
            var squareObj = new GameObject("Village_Square");
            squareObj.transform.SetParent(parent, false);

            // Bảng Hương Ước Làng Bờ Rìa (tại local x: 14, y: 1 - world x: 114, y: -15)
            var boardObj = new GameObject("Village_Notice_Board");
            boardObj.transform.SetParent(squareObj.transform, false);
            boardObj.transform.localPosition = new Vector3(14f, 1f, 0);

            CreateSpriteProp(boardObj.transform, "Post", "Art/Sprites/Environment/Fences/fence_post.png", Vector3.zero, 1.2f, Color.white, 30);
            CreateFloatingSignboard(boardObj.transform, "Rule_Board", new Vector3(0f, 1.8f, 0),
                "<b>HƯƠNG ƯỚC LÀNG BỜ RÌA</b>\n<size=65%><color=#fca5a5>1. Cấm Kỵ Số 7\n2. Bát Úp Chớ Lật\n3. Tuyệt Đối Không Nhìn Lên Trời</color></size>",
                new Color(0.65f, 0.45f, 0.35f), 3.2f, 1.4f);

            // Đèn lồng ngã tư đường (local x: 11, y: -1)
            var torchPost = CreateSpriteProp(squareObj.transform, "Crossroad_Post", "Art/Sprites/Environment/Fences/fence_post.png", new Vector3(11f, -1f, 0), 1f, Color.white, 30);
            CreateSpriteProp(torchPost.transform, "Flame", "Art/Sprites/VFX/Telegraphs/vfx_torch_fire.png", new Vector3(0f, 1.1f, 0), 0.7f, new Color(1f, 0.85f, 0.45f, 0.9f), 60);

            // Ghế gỗ dài ven đường (local x: 12f, y: 1.5f)
            CreateSpriteProp(squareObj.transform, "Bench", "Art/Sprites/Environment/Fences/fence_h.png", new Vector3(12f, 1.5f, 0), 1.3f, new Color(0.7f, 0.58f, 0.45f), 20);
        }

        private static void CreateStationBaSau(Transform parent, Vector3 localPos)
        {
            var stationObj = new GameObject("Station_BaSau");
            stationObj.transform.SetParent(parent, false);
            stationObj.transform.localPosition = localPos;

            // Gian nhà tranh / bếp ăn
            var hut = CreateSpriteProp(stationObj.transform, "Hut_BaSau", "Art/Sprites/Environment/Buildings/shelter_barn.png", Vector3.zero, 1.1f, new Color(0.78f, 0.68f, 0.58f), Mathf.RoundToInt(-(parent.position.y + localPos.y) * 100));
            var hutCol = hut.AddComponent<BoxCollider2D>();
            hutCol.size = new Vector2(2.8f, 1.5f);
            hutCol.offset = new Vector2(0f, 0.3f);

            // Bàn ăn gỗ trước quán
            var table = CreateSpriteProp(stationObj.transform, "Table", "Art/Sprites/Environment/Buildings/feeder_trough.png", new Vector3(0f, -1.2f, 0), new Vector3(1.1f, 0.75f, 1f), new Color(0.68f, 0.52f, 0.38f), Mathf.RoundToInt(-(parent.position.y + localPos.y - 1.2f) * 100));

            // "Cái Bát Úp Dở" (The Inverted Bowl Prop - Lore Taboo #2)
            var bowl = new GameObject("Prop_Inverted_Bowl");
            bowl.transform.SetParent(table.transform, false);
            bowl.transform.localPosition = new Vector3(0.4f, 0.15f, 0);
            bowl.transform.localEulerAngles = new Vector3(0f, 0f, 180f); // Úp ngược
            bowl.transform.localScale = new Vector3(0.35f, 0.35f, 1f);
            var bowlSr = bowl.AddComponent<SpriteRenderer>();
            bowlSr.sprite = UISpriteLoader.LoadSprite("Art/UI/Skin/Knob.psd", 32);
            bowlSr.color = new Color(0.92f, 0.94f, 0.90f); // Bát men xanh sứt mẻ
            bowlSr.sortingOrder = table.GetComponent<SpriteRenderer>().sortingOrder + 10;

            // Ấm trà / Nồi cháo
            CreateSpriteProp(table.transform, "Prop_Kettle", "Art/Sprites/Environment/Buildings/water_well.png", new Vector3(-0.4f, 0.25f, 0), 0.25f, new Color(0.45f, 0.35f, 0.25f), table.GetComponent<SpriteRenderer>().sortingOrder + 10);

            // Đèn lồng treo hiên quán
            CreateSpriteProp(stationObj.transform, "Lantern_Eaves", "Art/Sprites/VFX/Telegraphs/vfx_torch_fire.png", new Vector3(-1.3f, 0.9f, 0), 0.65f, new Color(1f, 0.85f, 0.45f, 0.95f), 150);

            // Nhân vật NPC Bà Sáu
            CreateSpriteProp(stationObj.transform, "NPC_BaSau", "Art/Sprites/Characters/Player/player_down_idle.png", new Vector3(-1.3f, -1.1f, 0), 0.9f, new Color(0.92f, 0.78f, 0.65f), Mathf.RoundToInt(-(parent.position.y + localPos.y - 1.1f) * 100));

            // Biển hiệu gỗ nổi
            CreateFloatingSignboard(stationObj.transform, "Signboard", new Vector3(0f, 2.2f, 0), "<b>Quán Bà Sáu</b>\n<size=70%><color=#fbbf24>Nước Chè & Cháo Nóng</color></size>", new Color(0.72f, 0.55f, 0.38f));

            // Vùng kích hoạt tương tác
            var trigCol = stationObj.AddComponent<BoxCollider2D>();
            trigCol.isTrigger = true;
            trigCol.size = new Vector2(4.2f, 3.5f);
            trigCol.offset = new Vector2(0f, -0.6f);

            var trig = stationObj.AddComponent<VillageInteractableTrigger>();
            trig.TargetVillagerId = VillagerId.BaSau;
            trig.StationTitle = "Quán Bà Sáu";
        }

        private static void CreateStationTamVa(Transform parent, Vector3 localPos)
        {
            var stationObj = new GameObject("Station_TamVa");
            stationObj.transform.SetParent(parent, false);
            stationObj.transform.localPosition = localPos;

            // Gian xưởng mộc
            var barn = CreateSpriteProp(stationObj.transform, "Barn_TamVa", "Art/Sprites/Environment/Buildings/shelter_barn.png", Vector3.zero, 1.15f, new Color(0.60f, 0.48f, 0.38f), Mathf.RoundToInt(-(parent.position.y + localPos.y) * 100));
            var barnCol = barn.AddComponent<BoxCollider2D>();
            barnCol.size = new Vector2(3.0f, 1.5f);
            barnCol.offset = new Vector2(0f, 0.3f);

            // Đống gỗ xẻ & cọc rào xếp bên xưởng
            CreateSpriteProp(stationObj.transform, "Lumber_Pile_1", "Art/Sprites/Environment/Fences/fence_h.png", new Vector3(-2.2f, -1.1f, 0), 1.1f, new Color(0.75f, 0.62f, 0.48f), Mathf.RoundToInt(-(parent.position.y + localPos.y - 1.1f) * 100));
            CreateSpriteProp(stationObj.transform, "Lumber_Pile_2", "Art/Sprites/Environment/Fences/fence_h.png", new Vector3(-2.2f, -0.8f, 0), 1.0f, new Color(0.70f, 0.58f, 0.44f), Mathf.RoundToInt(-(parent.position.y + localPos.y - 0.8f) * 100));
            CreateSpriteProp(stationObj.transform, "Lumber_Post", "Art/Sprites/Environment/Fences/fence_post.png", new Vector3(2.2f, -1.0f, 0), 1.2f, new Color(0.72f, 0.60f, 0.45f), Mathf.RoundToInt(-(parent.position.y + localPos.y - 1.0f) * 100));

            // Bù nhìn thử nghiệm độ bền rào
            CreateSpriteProp(stationObj.transform, "Testing_Dummy", "Art/Sprites/Environment/Training/training_dummy_straw.png", new Vector3(-1.8f, -1.4f, 0), 0.9f, Color.white, Mathf.RoundToInt(-(parent.position.y + localPos.y - 1.4f) * 100));

            // Nhân vật NPC Tám Vá
            CreateSpriteProp(stationObj.transform, "NPC_TamVa", "Art/Sprites/Characters/Player/player_down_idle.png", new Vector3(0.8f, -1.2f, 0), 0.95f, new Color(0.75f, 0.58f, 0.45f), Mathf.RoundToInt(-(parent.position.y + localPos.y - 1.2f) * 100));

            // Biển hiệu gỗ nổi
            CreateFloatingSignboard(stationObj.transform, "Signboard", new Vector3(0f, 2.3f, 0), "<b>Xưởng Mộc Tám Vá</b>\n<size=70%><color=#93c5fd>Vá Rào Chống Quái (Gỗ Xẻ)</color></size>", new Color(0.60f, 0.45f, 0.32f));

            // Vùng kích hoạt tương tác
            var trigCol = stationObj.AddComponent<BoxCollider2D>();
            trigCol.isTrigger = true;
            trigCol.size = new Vector2(4.5f, 3.5f);
            trigCol.offset = new Vector2(0f, -0.6f);

            var trig = stationObj.AddComponent<VillageInteractableTrigger>();
            trig.TargetVillagerId = VillagerId.TamVa;
            trig.StationTitle = "Xưởng Mộc Tám Vá";
        }

        private static void CreateStationCoDung(Transform parent, Vector3 localPos)
        {
            var stationObj = new GameObject("Station_CoDung");
            stationObj.transform.SetParent(parent, false);
            stationObj.transform.localPosition = localPos;

            // Gian tiệm thuốc thú y
            var hut = CreateSpriteProp(stationObj.transform, "Hut_CoDung", "Art/Sprites/Environment/Buildings/shelter_barn.png", Vector3.zero, 1.05f, new Color(0.48f, 0.58f, 0.45f), Mathf.RoundToInt(-(parent.position.y + localPos.y) * 100));
            var hutCol = hut.AddComponent<BoxCollider2D>();
            hutCol.size = new Vector2(2.6f, 1.5f);
            hutCol.offset = new Vector2(0f, 0.3f);

            // Giếng đá rửa dược liệu
            var well = CreateSpriteProp(stationObj.transform, "Well_Herbs", "Art/Sprites/Environment/Buildings/water_well.png", new Vector3(2.3f, -0.6f, 0), 1.15f, Color.white, Mathf.RoundToInt(-(parent.position.y + localPos.y - 0.6f) * 100));
            var wellCol = well.AddComponent<BoxCollider2D>();
            wellCol.size = new Vector2(1.1f, 0.7f);
            wellCol.offset = new Vector2(0f, -0.3f);

            // Tảng đá rêu & cối giã thuốc
            CreateSpriteProp(stationObj.transform, "Mossy_Boulder", "Art/Sprites/Environment/Nature/rock_boulder.png", new Vector3(-2.2f, -1.1f, 0), 1.1f, new Color(0.52f, 0.62f, 0.50f), Mathf.RoundToInt(-(parent.position.y + localPos.y - 1.1f) * 100));

            // Đèn lồng xanh thảo dược
            CreateSpriteProp(stationObj.transform, "Lantern_Herbal", "Art/Sprites/VFX/Telegraphs/vfx_torch_fire.png", new Vector3(-1.2f, 0.9f, 0), 0.65f, new Color(0.6f, 1f, 0.7f, 0.9f), 150);

            // Nhân vật NPC Cô Dung
            CreateSpriteProp(stationObj.transform, "NPC_CoDung", "Art/Sprites/Characters/Player/player_down_idle.png", new Vector3(-0.6f, -1.2f, 0), 0.9f, new Color(0.55f, 0.82f, 0.65f), Mathf.RoundToInt(-(parent.position.y + localPos.y - 1.2f) * 100));

            // Biển hiệu gỗ nổi
            CreateFloatingSignboard(stationObj.transform, "Signboard", new Vector3(0f, 2.2f, 0), "<b>Tiệm Thuốc Cô Dung</b>\n<size=70%><color=#86efac>Bột Kháng Dịch & An Thần</color></size>", new Color(0.45f, 0.55f, 0.42f));

            // Vùng kích hoạt tương tác
            var trigCol = stationObj.AddComponent<BoxCollider2D>();
            trigCol.isTrigger = true;
            trigCol.size = new Vector2(4.5f, 3.5f);
            trigCol.offset = new Vector2(0f, -0.6f);

            var trig = stationObj.AddComponent<VillageInteractableTrigger>();
            trig.TargetVillagerId = VillagerId.CoDung;
            trig.StationTitle = "Tiệm Thuốc Cô Dung";
        }

        private static void CreateStationAnhCa(Transform parent, Vector3 localPos)
        {
            var stationObj = new GameObject("Station_AnhCa");
            stationObj.transform.SetParent(parent, false);
            stationObj.transform.localPosition = localPos;

            // Bãi rào nhốt cân heo (4m x 3m)
            CreateSpriteProp(stationObj.transform, "Pen_Top", "Art/Sprites/Environment/Fences/fence_h.png", new Vector3(0f, 0.8f, 0), 2.0f, Color.white, Mathf.RoundToInt(-(parent.position.y + localPos.y + 0.8f) * 100));
            CreateSpriteProp(stationObj.transform, "Pen_Bottom_L", "Art/Sprites/Environment/Fences/fence_h.png", new Vector3(-1.2f, -1.8f, 0), 0.8f, Color.white, Mathf.RoundToInt(-(parent.position.y + localPos.y - 1.8f) * 100));
            CreateSpriteProp(stationObj.transform, "Pen_Bottom_R", "Art/Sprites/Environment/Fences/fence_h.png", new Vector3(1.2f, -1.8f, 0), 0.8f, Color.white, Mathf.RoundToInt(-(parent.position.y + localPos.y - 1.8f) * 100));
            CreateSpriteProp(stationObj.transform, "Pen_Left", "Art/Sprites/Environment/Fences/fence_v.png", new Vector3(-2.0f, -0.5f, 0), 1.8f, Color.white, Mathf.RoundToInt(-(parent.position.y + localPos.y - 0.5f) * 100));
            CreateSpriteProp(stationObj.transform, "Pen_Right", "Art/Sprites/Environment/Fences/fence_v.png", new Vector3(2.0f, -0.5f, 0), 1.8f, Color.white, Mathf.RoundToInt(-(parent.position.y + localPos.y - 0.5f) * 100));

            // Máng ăn tạm trong bãi cân
            CreateSpriteProp(stationObj.transform, "Scale_Trough", "Art/Sprites/Environment/Buildings/feeder_trough.png", new Vector3(0f, 0.1f, 0), 1.0f, new Color(0.7f, 0.6f, 0.5f), Mathf.RoundToInt(-(parent.position.y + localPos.y + 0.1f) * 100));

            // Bàn cân cơ học & Bàn ghi chép Sổ Đen của Ông Tồn
            var desk = CreateSpriteProp(stationObj.transform, "Ledger_Desk", "Art/Sprites/Environment/Buildings/feeder_trough.png", new Vector3(1.3f, -1.2f, 0), new Vector3(0.8f, 0.6f, 1f), new Color(0.5f, 0.4f, 0.35f), Mathf.RoundToInt(-(parent.position.y + localPos.y - 1.2f) * 100));

            // Cuốn "Sổ Đen" trên bàn
            CreateSpriteProp(desk.transform, "Black_Ledger", "Art/UI/Frames/frame_wood_panel.png", new Vector3(0f, 0.2f, 0), 0.25f, new Color(0.18f, 0.18f, 0.2f), desk.GetComponent<SpriteRenderer>().sortingOrder + 10);

            // Bảng vạch chia bàn cân (Noticeably jumping from 6 to 8 - Forbidden 7!)
            var scaleDial = CreateSpriteProp(stationObj.transform, "Scale_Dial_Board", "Art/UI/Frames/frame_wood_panel.png", new Vector3(-1.3f, -1.2f, 0), 0.45f, new Color(0.6f, 0.5f, 0.4f), Mathf.RoundToInt(-(parent.position.y + localPos.y - 1.2f) * 100));
            CreateFloatingSignboard(scaleDial.transform, "Dial_Text", Vector3.zero, "<color=#fca5a5><b>VẠCH CÂN:\n1 2 3 4 5 6 [8] 9</b></color>", Color.black, 2.0f, 0.8f);

            // NPC Anh Ca (Lực lưỡng phụ trách cân & lùa heo)
            CreateSpriteProp(stationObj.transform, "NPC_AnhCa", "Art/Sprites/Characters/Player/player_down_idle.png", new Vector3(-1.3f, -1.6f, 0), 1.0f, new Color(0.88f, 0.72f, 0.55f), Mathf.RoundToInt(-(parent.position.y + localPos.y - 1.6f) * 100));

            // NPC Ông Tồn (Lão làng giữ sổ đen & phân xử)
            CreateSpriteProp(stationObj.transform, "NPC_OngTon", "Art/Sprites/Characters/Player/player_down_idle.png", new Vector3(1.3f, -1.6f, 0), 0.9f, new Color(0.52f, 0.45f, 0.58f), Mathf.RoundToInt(-(parent.position.y + localPos.y - 1.6f) * 100));

            // Biển hiệu gỗ nổi
            CreateFloatingSignboard(stationObj.transform, "Signboard", new Vector3(0f, 2.3f, 0), "<b>Trạm Cân 6+1</b>\n<size=70%><color=#f87171>Anh Ca & Ông Tồn [CẤM SỐ 7]</color></size>", new Color(0.62f, 0.45f, 0.35f));

            // Vùng kích hoạt tương tác
            var trigCol = stationObj.AddComponent<BoxCollider2D>();
            trigCol.isTrigger = true;
            trigCol.size = new Vector2(5.0f, 3.8f);
            trigCol.offset = new Vector2(0f, -0.6f);

            var trig = stationObj.AddComponent<VillageInteractableTrigger>();
            trig.TargetVillagerId = VillagerId.AnhCa;
            trig.StationTitle = "Trạm Cân 6+1";
        }

        private static void CreateStationBiNhan(Transform parent, Vector3 localPos)
        {
            var stationObj = new GameObject("Station_BiNhan");
            stationObj.transform.SetParent(parent, false);
            stationObj.transform.localPosition = localPos;

            // Cây Đa Ranh Giới Khổng Lồ (Ancient Sprawling Tree 2.8x)
            var banyanTree = CreateSpriteProp(stationObj.transform, "Ancient_Banyan_Tree", "Art/Sprites/Environment/Nature/tree_prairie.png", Vector3.zero, 2.8f, new Color(0.32f, 0.35f, 0.42f), Mathf.RoundToInt(-(parent.position.y + localPos.y) * 100));
            var treeCol = banyanTree.AddComponent<CircleCollider2D>();
            treeCol.radius = 0.55f;
            treeCol.offset = new Vector2(0f, -1.2f);

            // Cự thạch ranh giới khắc bùa chú (Boundary Megaliths)
            CreateSpriteProp(stationObj.transform, "Megalith_Left", "Art/Sprites/Environment/Nature/rock_boulder.png", new Vector3(-2.8f, -1.0f, 0), 1.6f, new Color(0.28f, 0.32f, 0.40f), Mathf.RoundToInt(-(parent.position.y + localPos.y - 1.0f) * 100));
            CreateSpriteProp(stationObj.transform, "Megalith_Right", "Art/Sprites/Environment/Nature/rock_boulder.png", new Vector3(2.8f, -1.0f, 0), 1.6f, new Color(0.28f, 0.32f, 0.40f), Mathf.RoundToInt(-(parent.position.y + localPos.y - 1.0f) * 100));

            // Luồng hào quang huyền bí / Năng lượng dị biến (Tractor beam / Mystical purple aura)
            CreateSpriteProp(stationObj.transform, "Mystic_Aura", "Art/Sprites/VFX/Telegraphs/vfx_tractor_beam.png", new Vector3(0f, -0.4f, 0), 1.8f, new Color(0.65f, 0.25f, 0.95f, 0.35f), 10);

            // Rào chắn ranh giới cảnh báo không bước qua
            CreateSpriteProp(stationObj.transform, "Broken_Fence_Warning_L", "Art/Sprites/Environment/Fences/fence_broken.png", new Vector3(-1.8f, -2.0f, 0), 1.2f, new Color(0.5f, 0.45f, 0.55f), 50);
            CreateSpriteProp(stationObj.transform, "Broken_Fence_Warning_R", "Art/Sprites/Environment/Fences/fence_broken.png", new Vector3(1.8f, -2.0f, 0), 1.2f, new Color(0.5f, 0.45f, 0.55f), 50);

            // Nhân vật NPC Bí Nhân (Thương nhân áo choàng tím huyền bí)
            CreateSpriteProp(stationObj.transform, "NPC_BiNhan", "Art/Sprites/Characters/Player/player_down_idle.png", new Vector3(0.5f, -1.4f, 0), 0.95f, new Color(0.22f, 0.12f, 0.35f, 0.95f), Mathf.RoundToInt(-(parent.position.y + localPos.y - 1.4f) * 100));

            // Biển hiệu gỗ nổi
            CreateFloatingSignboard(stationObj.transform, "Signboard", new Vector3(0f, 2.6f, 0), "<b>Gốc Đa Ranh Giới</b>\n<size=70%><color=#c084fc>Bí Nhân [0% VÀNG - ĐỔI HEO LẠ]</color></size>", new Color(0.42f, 0.30f, 0.50f));

            // Vùng kích hoạt tương tác
            var trigCol = stationObj.AddComponent<BoxCollider2D>();
            trigCol.isTrigger = true;
            trigCol.size = new Vector2(5.0f, 4.0f);
            trigCol.offset = new Vector2(0f, -0.8f);

            var trig = stationObj.AddComponent<VillageInteractableTrigger>();
            trig.TargetVillagerId = VillagerId.BiNhan;
            trig.StationTitle = "Gốc Đa Ranh Giới (Bí Nhân)";
        }

        private static GameObject CreateSpriteProp(Transform parent, string name, string spritePath, Vector3 localPos, float uniformScale, Color tint, int sortingOrder)
        {
            return CreateSpriteProp(parent, name, spritePath, localPos, new Vector3(uniformScale, uniformScale, 1f), tint, sortingOrder);
        }

        private static GameObject CreateSpriteProp(Transform parent, string name, string spritePath, Vector3 localPos, Vector3 scale, Color tint, int sortingOrder)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPos;
            go.transform.localScale = scale;

            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = UISpriteLoader.LoadSprite(spritePath, 32);
            sr.color = tint;
            sr.sortingOrder = sortingOrder;
            return go;
        }

        private static void CreateDarkTree(Transform parent, Sprite sp, Vector3 localPos, Color tint)
        {
            var go = new GameObject("Dark_Tree");
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPos;
            go.transform.localScale = new Vector3(2.0f, 2.0f, 1f);

            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = sp;
            sr.color = tint;
            sr.sortingOrder = Mathf.RoundToInt(-(parent.position.y + localPos.y - 1.5f) * 100);

            var col = go.AddComponent<CircleCollider2D>();
            col.offset = new Vector2(0f, -0.85f);
            col.radius = 0.25f;
        }

        private static void CreateDarkBoulder(Transform parent, Sprite sp, Vector3 localPos, Color tint)
        {
            var go = new GameObject("Dark_Boulder");
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPos;
            go.transform.localScale = new Vector3(1.4f, 1.4f, 1f);

            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = sp;
            sr.color = tint;
            sr.sortingOrder = Mathf.RoundToInt(-(parent.position.y + localPos.y - 0.14f) * 100);

            var col = go.AddComponent<CircleCollider2D>();
            col.offset = new Vector2(0f, -0.1f);
            col.radius = 0.35f;
        }

        private static void CreateFloatingSignboard(Transform parent, string name, Vector3 localPos, string text, Color bgColor, float width = 3.2f, float height = 1.1f)
        {
            var boardObj = new GameObject(name);
            boardObj.transform.SetParent(parent, false);
            boardObj.transform.localPosition = localPos;

            // Tấm bảng gỗ nền
            var boardBg = new GameObject("Bg");
            boardBg.transform.SetParent(boardObj.transform, false);
            var sr = boardBg.AddComponent<SpriteRenderer>();
            sr.sprite = UISpriteLoader.LoadSprite("Art/UI/Frames/frame_wood_panel.png", 64);
            sr.color = bgColor;
            sr.sortingOrder = 300;
            boardBg.transform.localScale = new Vector3(width * 0.35f, height * 0.35f, 1f);

            // Chữ hiển thị
            var textObj = new GameObject("Text");
            textObj.transform.SetParent(boardObj.transform, false);
            textObj.transform.localPosition = new Vector3(0f, 0f, 0f);

            var tmp = textObj.AddComponent<TextMeshPro>();
            tmp.text = text;
            tmp.fontSize = 2.4f;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color = Color.white;
            tmp.sortingOrder = 310;

            if (tmp.rectTransform != null)
            {
                tmp.rectTransform.sizeDelta = new Vector2(width, height);
            }
        }
    }

    public class VillageInteractableTrigger : MonoBehaviour
    {
        public VillagerId TargetVillagerId;
        public string StationTitle = "";

        private bool isPlayerNearby = false;

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (other.GetComponent<PlayerMobileController>() != null || other.CompareTag("Player"))
            {
                isPlayerNearby = true;
                string prompt = $"[E / Chạm] Gặp {StationTitle}";
                Debug.Log($"<color=yellow>[Làng Bờ Rìa]</color> {prompt}");
            }
        }

        private void OnTriggerExit2D(Collider2D other)
        {
            if (other.GetComponent<PlayerMobileController>() != null || other.CompareTag("Player"))
            {
                isPlayerNearby = false;
            }
        }

        private void Update()
        {
            if (isPlayerNearby && (Input.GetKeyDown(KeyCode.E) || Input.GetKeyDown(KeyCode.Space)))
            {
                Interact();
            }
        }

        private void OnMouseDown()
        {
            if (isPlayerNearby)
            {
                Interact();
            }
        }

        public void Interact()
        {
            var popup = VillageBarterPopup.Instance;
            if (popup == null)
            {
                popup = FindAnyObjectByType<VillageBarterPopup>();
                if (popup == null)
                {
                    var canvas = VillageBarterPopup.FindScreenSpaceCanvas();
                    if (canvas != null) popup = canvas.gameObject.AddComponent<VillageBarterPopup>();
                }
            }

            if (popup != null)
            {
                popup.OpenForVillager(TargetVillagerId);
            }
        }
    }
}
