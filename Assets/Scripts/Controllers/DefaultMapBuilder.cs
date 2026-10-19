using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;
using PigTycoon.Core;

namespace PigTycoon.Presentation
{
    public static class DefaultMapBuilder
    {
        public static void EnsureDefaultMapExists(bool forceRebuild = false)
        {
            var groundObj = GameObject.Find("Tilemap_Ground");
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
            var grid = gridObj.GetComponent<Grid>() ?? gridObj.AddComponent<Grid>();
            grid.cellSize = new Vector3(1f, 1f, 0f);

            var groundTilemap = GetOrCreateTilemap(gridObj.transform, "Tilemap_Ground", -10000);
            var waterTilemap = GetOrCreateTilemap(gridObj.transform, "Tilemap_Water", -9800);
            var mudTilemap = GetOrCreateTilemap(gridObj.transform, "Tilemap_MudPit", -9700);
            var pathTilemap = GetOrCreateTilemap(gridObj.transform, "Tilemap_Paths", -9900);

            var waterCol = waterTilemap.GetComponent<TilemapCollider2D>() ?? waterTilemap.gameObject.AddComponent<TilemapCollider2D>();

            // 2. Load Tiles
            var (grassSteppeTile, grassTallTile, grassBaseTile, flower1Tile, flower2Tile, flower3Tile) = CreateGrassTiles();
            var (trailV, trailH, trailCross, trailTWest, trailTEast, trailTNorth, trailTSouth,
                 cornerSW, cornerSE, cornerNW, cornerNE, endW, endE, endN, endS) = CreateTrailTiles();

            var waterTile = ScriptableObject.CreateInstance<Tile>();
            waterTile.sprite = UISpriteLoader.LoadSprite("Art/Tiles/Sprites/water_tile.png", 32);
            waterTile.colliderType = Tile.ColliderType.Grid;

            var mudTile = ScriptableObject.CreateInstance<Tile>();
            mudTile.sprite = UISpriteLoader.LoadSprite("Art/Tiles/Sprites/mud_tile.png", 32);

            // 3. Phủ Thảo Nguyên (-45 đến +45, -32 đến +32)
            groundTilemap.ClearAllTiles();
            for (int x = -48; x <= 48; x++)
            {
                for (int y = -35; y <= 35; y++)
                {
                    float r = Mathf.PerlinNoise((x + 100) * 0.12f, (y + 100) * 0.12f);
                    Tile t = grassSteppeTile;
                    if (r > 0.65f) t = grassTallTile;
                    else if (r < 0.25f) t = grassBaseTile;
                    else if (r > 0.58f && r <= 0.62f) t = flower1Tile;
                    else if (r > 0.48f && r <= 0.51f) t = flower2Tile;
                    else if (r > 0.38f && r <= 0.40f) t = flower3Tile;
                    groundTilemap.SetTile(new Vector3Int(x, y, 0), t);
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
            for (int y = -28; y <= 6; y++) pathTilemap.SetTile(new Vector3Int(0, y, 0), trailV);
            for (int x = -13; x <= 0; x++) pathTilemap.SetTile(new Vector3Int(x, 6, 0), trailH);
            for (int x = 0; x <= 7; x++) pathTilemap.SetTile(new Vector3Int(x, -1, 0), trailH);
            for (int x = -30; x <= 25; x++) pathTilemap.SetTile(new Vector3Int(x, -20, 0), trailH);

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
                var feObj = GameObject.Find("FarmEnvironment_2D") ?? new GameObject("FarmEnvironment_2D");
                farmEnv = feObj.GetComponent<FarmEnvironment2D>() ?? feObj.AddComponent<FarmEnvironment2D>();
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
            var tm = obj.GetComponent<Tilemap>() ?? obj.AddComponent<Tilemap>();
            var tr = obj.GetComponent<TilemapRenderer>() ?? obj.AddComponent<TilemapRenderer>();
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
            if (natureGroup != null) return;

            natureGroup = new GameObject("Prairie_Nature");
            var treeSp = UISpriteLoader.LoadSprite("Art/Sprites/Environment/Nature/tree_prairie.png", 32);
            var rockSp = UISpriteLoader.LoadSprite("Art/Sprites/Environment/Nature/rock_boulder.png", 32);
            var flowerSp = UISpriteLoader.LoadSprite("Art/Sprites/Environment/Nature/flower_patch.png", 32);

            // Cây đại thụ
            CreatePrairieTree(natureGroup.transform, treeSp, new Vector3(-5f, 11f, 0));
            CreatePrairieTree(natureGroup.transform, treeSp, new Vector3(15f, 9f, 0));
            CreatePrairieTree(natureGroup.transform, treeSp, new Vector3(-32f, 18f, 0));
            CreatePrairieTree(natureGroup.transform, treeSp, new Vector3(-28f, -6f, 0));
            CreatePrairieTree(natureGroup.transform, treeSp, new Vector3(28f, 18f, 0));
            CreatePrairieTree(natureGroup.transform, treeSp, new Vector3(32f, -18f, 0));

            // Tảng đá rêu phong
            CreatePrairieBoulder(natureGroup.transform, rockSp, new Vector3(-8f, -6f, 0));
            CreatePrairieBoulder(natureGroup.transform, rockSp, new Vector3(5f, -6f, 0));
            CreatePrairieBoulder(natureGroup.transform, rockSp, new Vector3(20f, -14f, 0));

            // Bụi hoa dại
            CreatePrairieFlowerPatch(natureGroup.transform, flowerSp, new Vector3(-3f, 1f, 0));
            CreatePrairieFlowerPatch(natureGroup.transform, flowerSp, new Vector3(4f, 1f, 0));
            CreatePrairieFlowerPatch(natureGroup.transform, flowerSp, new Vector3(-25f, 10f, 0));
            CreatePrairieFlowerPatch(natureGroup.transform, flowerSp, new Vector3(24f, 12f, 0));
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
            var pWalk1 = UISpriteLoader.LoadSprite($"Art/Sprites/Pigs/{folder}/walk1.png", 16) ?? pIdle;
            var pWalk2 = UISpriteLoader.LoadSprite($"Art/Sprites/Pigs/{folder}/walk2.png", 16) ?? pIdle;
            var pEat = UISpriteLoader.LoadSprite($"Art/Sprites/Pigs/{folder}/eat.png", 16) ?? pIdle;
            var pSleep = UISpriteLoader.LoadSprite($"Art/Sprites/Pigs/{folder}/sleep.png", 16) ?? pIdle;
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

            // 1. Cổng Nam Trang Trại (Khu vực chuyển cảnh)
            if (GameObject.Find("Trigger_Farm_To_Village") == null)
            {
                var tf = new GameObject("Trigger_Farm_To_Village");
                tf.transform.position = new Vector3(0f, -16f, 0f);
                var col = tf.AddComponent<BoxCollider2D>();
                col.isTrigger = true;
                col.size = new Vector2(5f, 2f);
                var listener = tf.AddComponent<TransitionTriggerListener>();
                listener.IsTriggerToVillage = true;

                // Biển gỗ chỉ dẫn
                var sign = new GameObject("Sign_To_Village");
                sign.transform.SetParent(tf.transform, false);
                sign.transform.localPosition = new Vector3(-3.2f, 0.5f, 0);
                var sr = sign.AddComponent<SpriteRenderer>();
                sr.sprite = UISpriteLoader.LoadSprite("Art/UI/Frames/frame_wood_panel.png", 64);
                sr.sortingOrder = 10;
                sign.transform.localScale = new Vector3(0.5f, 0.5f, 1f);
            }

            // 2. Khu Vực Làng Bờ Rìa (Village Zone tại Tọa độ x=100..140, y=-25..-8)
            var villageRoot = GameObject.Find("Village_Area_BoRia");
            if (villageRoot == null)
            {
                villageRoot = new GameObject("Village_Area_BoRia");
                villageRoot.transform.position = new Vector3(100f, -16f, 0f);

                // Điểm chuyển cảnh quay lại Trang Trại
                var tv = new GameObject("Trigger_Village_To_Farm");
                tv.transform.SetParent(villageRoot.transform, false);
                tv.transform.localPosition = Vector3.zero;
                var colV = tv.AddComponent<BoxCollider2D>();
                colV.isTrigger = true;
                colV.size = new Vector2(5f, 2f);
                var lV = tv.AddComponent<TransitionTriggerListener>();
                lV.IsTriggerToVillage = false;

                // Các gian hàng / nhà dân làng Bờ Rìa kèm Trigger tương tác
                CreateVillagerStation(villageRoot.transform, VillagerId.BaSau, "Quán Bà Sáu (Bếp Ăn & Nước Chè)", new Vector3(8f, 2f, 0));
                CreateVillagerStation(villageRoot.transform, VillagerId.TamVa, "Xưởng Mộc Tám Vá (Vá Rào)", new Vector3(16f, 6f, 0));
                CreateVillagerStation(villageRoot.transform, VillagerId.CoDung, "Tiệm Thuốc Thú Y Cô Dung", new Vector3(8f, -6f, 0));
                CreateVillagerStation(villageRoot.transform, VillagerId.AnhCa, "Trạm Cân 6+1 (Anh Ca & Ông Tồn)", new Vector3(20f, -2f, 0));
                CreateVillagerStation(villageRoot.transform, VillagerId.BiNhan, "Gốc Đa Ranh Giới (Bí Nhân 25m)", new Vector3(35f, -9f, 0));
            }
        }

        private static void CreateVillagerStation(Transform parent, VillagerId id, string title, Vector3 localPos)
        {
            var stationObj = new GameObject($"Station_{id}");
            stationObj.transform.SetParent(parent, false);
            stationObj.transform.localPosition = localPos;

            var sr = stationObj.AddComponent<SpriteRenderer>();
            sr.sprite = UISpriteLoader.LoadSprite("Art/Sprites/Environment/Buildings/shelter_barn.png", 64);
            sr.color = new Color(0.7f, 0.65f, 0.6f); // Tông xám u ám Bờ Rìa
            sr.sortingOrder = Mathf.RoundToInt(-(parent.position.y + localPos.y) * 100);
            stationObj.transform.localScale = new Vector3(0.9f, 0.9f, 1f);

            var col = stationObj.AddComponent<BoxCollider2D>();
            col.isTrigger = true;
            col.size = new Vector2(3.5f, 2.5f);

            var trig = stationObj.AddComponent<VillageInteractableTrigger>();
            trig.TargetVillagerId = id;
            trig.StationTitle = title;
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
                    var canvas = FindAnyObjectByType<Canvas>();
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
