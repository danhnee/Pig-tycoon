using UnityEngine.Rendering.Universal;
using TMPro;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace PigTycoon.Presentation
{
    [SelectionBase]
    [DisallowMultipleComponent]
    public sealed class KhoaHouseView : MonoBehaviour
    {
        private void Awake()
        {
            EnsureColliders();
        }

        private void Start()
        {
            EnsureColliders();
        }

        private void OnValidate()
        {
            EnsureColliders();
        }

        public void EnsureColliders()
        {
            // 1. XÃ³a sáº¡ch má»i collider cÅ© hoáº·c thá»«a á»Ÿ táº¥t cáº£ cÃ¡c GameObject con (Exterior, v.v.)
            foreach (var c in GetComponentsInChildren<Collider2D>(true))
            {
                if (c != null && c.gameObject != gameObject)
                {
                    if (Application.isPlaying) Destroy(c);
                    else DestroyImmediate(c);
                }
            }

            // 2. XÃ³a sáº¡ch má»i collider dÆ° thá»«a (nhÆ° barrier cÅ©, planter box) trÃªn chÃ­nh ngÃ´i nhÃ , chá»‰ giá»¯ Ä‘Ãºng 1 collider thÃ¢n nhÃ 
            var allCols = GetComponents<Collider2D>();
            for (int i = allCols.Length - 1; i >= 1; i--)
            {
                if (allCols[i] != null)
                {
                    if (Application.isPlaying) Destroy(allCols[i]);
                    else DestroyImmediate(allCols[i]);
                }
            }

            // 3. Khung va cháº¡m duy nháº¥t cho thÃ¢n nhÃ  & mÃ¡i nhÃ  (Solid Walls & Roof):
            // Äáº·t chÃ¢n collider táº¡i Y >= 0.6f (bao trá»n tÆ°á»ng vÃ  mÃ¡i nhÃ , chá»«a toÃ n bá»™ thá»m cá»­a vÃ  con Ä‘Æ°á»ng bÃªn dÆ°á»›i).
            // Giáº£i phÃ³ng 100% con Ä‘Æ°á»ng trÆ°á»›c nhÃ , Ä‘áº£m báº£o nhÃ¢n váº­t cháº¡y ngang qua Ä‘Æ°á»ng hoÃ n toÃ n thÃ´ng thoÃ¡ng, khÃ´ng bao giá» bá»‹ vÆ°á»›ng hay káº¹t váº­t cáº£n vÃ´ hÃ¬nh!
            var cols = GetComponents<BoxCollider2D>();
            BoxCollider2D mainBodyCol = cols.Length > 0 ? cols[0] : gameObject.AddComponent<BoxCollider2D>();
            mainBodyCol.size = new Vector2(7.0f, 3.8f);
            mainBodyCol.offset = new Vector2(0f, 2.5f); // ChÃ¢n collider táº¡i Y = 0.6f, hoÃ n toÃ n khÃ´ng láº¥n xuá»‘ng lÃ²ng Ä‘Æ°á»ng
            mainBodyCol.isTrigger = false;
        }
    }

    /// <summary>
    /// Creates Khoa's warm, beautifully designed farmhouse interior inspired by classic pixel-art farm RPGs.
    /// Features cohesive wood & tile flooring, 2.5D walls with crown molding and wallpaper,
    /// arched windows with sunbeams, crackling fireplaces, and finely crafted furniture.
    /// </summary>
    public static class KhoaHouse2DBuilder
    {
        private const float PixelsPerUnit = 1f;
        private static Sprite whiteSprite;
        private static Sprite circleSprite;
        private static readonly Dictionary<string, Sprite> spriteCache = new Dictionary<string, Sprite>();

        // Architectural Palette
        private static readonly Color VoidBlack = new Color(0.015f, 0.012f, 0.010f);
        private static readonly Color OuterWallDark = new Color(0.20f, 0.11f, 0.07f);
        private static readonly Color OuterWallCap = new Color(0.38f, 0.22f, 0.14f);
        private static readonly Color WallDividerWood = new Color(0.32f, 0.18f, 0.11f);
        private static readonly Color WallTrimGold = new Color(0.68f, 0.44f, 0.24f);

#if UNITY_EDITOR
        [UnityEditor.MenuItem("PigTycoon/LÃ m Má»›i Ná»™i Tháº¥t NhÃ  Khoa (Rebuild House)")]
        public static void RebuildHouseEditor()
        {
            Vector3 savedPos = new Vector3(10f, 8f, 0f);
            var home = GameObject.Find("Khoa's Home");
            if (home != null)
            {
                savedPos = home.transform.position;
                if (Application.isPlaying)
                    GameObject.Destroy(home);
                else
                    GameObject.DestroyImmediate(home);
            }
            var interior = GameObject.Find("Khoa House Interior Map");
            if (interior != null)
            {
                if (Application.isPlaying)
                    GameObject.Destroy(interior);
                else
                    GameObject.DestroyImmediate(interior);
            }
            spriteCache.Clear();
            BuildIfMissing(savedPos);
        }

        [UnityEditor.MenuItem("PigTycoon/Äáº·t Láº¡i Vá»‹ TrÃ­ NhÃ  Vá» Ban Äáº§u (Reset House Position)")]
        public static void ResetHousePositionEditor()
        {
            var home = GameObject.Find("Khoa's Home");
            if (home != null)
            {
                home.transform.position = new Vector3(10f, 8f, 0f);
                home.transform.rotation = Quaternion.identity;
                home.transform.localScale = Vector3.one;
            }
            else
            {
                BuildIfMissing(new Vector3(10f, 8f, 0f));
            }
        }
#endif

        public static void BuildIfMissing()
        {
            BuildIfMissing(new Vector3(10f, 8f, 0f));
        }

        public static void BuildIfMissing(Vector3 defaultPosition)
        {
            var existingHome = GameObject.Find("Khoa's Home");
            if (existingHome != null)
            {
                // If it already has our modern unified KhoaHouseView, keep it at its position and ensure solid colliders
                var existingView = existingHome.GetComponent<KhoaHouseView>();
                if (existingView != null)
                {
                    existingView.EnsureColliders();
                    // Don dep triet de bat ky Point Light nao de tra lai nguyen ban hinh anh am cung, ro net 100% cua ngoi nha
                    foreach (var t in Resources.FindObjectsOfTypeAll<Transform>())
                    {
                        if (t.name == "Khoa House Interior Map" && t.gameObject.scene == existingHome.scene)
                        {
                            var fill = t.Find("Interior_Ambient_Fill");
                            if (fill != null)
                            {
                                if (Application.isPlaying) GameObject.Destroy(fill.gameObject);
                                else GameObject.DestroyImmediate(fill.gameObject);
                            }
                            for (int ci = t.childCount - 1; ci >= 0; ci--)
                            {
                                var child = t.GetChild(ci);
                                if (child.name.StartsWith("Light_"))
                                {
                                    if (Application.isPlaying) GameObject.Destroy(child.gameObject);
                                    else GameObject.DestroyImmediate(child.gameObject);
                                }
                            }
                            break;
                        }
                    }
                    return;
                }

                // Otherwise, it is an outdated or broken multi-quad instance: clean it up cleanly!
                defaultPosition = existingHome.transform.position;
                if (Application.isPlaying)
                    GameObject.Destroy(existingHome);
                else
                    GameObject.DestroyImmediate(existingHome);
            }

            var existingInterior = GameObject.Find("Khoa House Interior Map");
            if (existingInterior != null)
            {
                if (Application.isPlaying)
                    GameObject.Destroy(existingInterior);
                else
                    GameObject.DestroyImmediate(existingInterior);
            }

            EnsureBaseSprites();

            // Exterior stays in the farm scene
            var home = new GameObject("Khoa's Home");
            home.transform.position = defaultPosition;
            home.transform.localScale = Vector3.one;
            var houseView = home.AddComponent<KhoaHouseView>();
            houseView.EnsureColliders();

            // Interior is placed far outside farm bounds to avoid collision
            var interior = new GameObject("Khoa House Interior Map");
            interior.transform.position = new Vector3(200f, 0f, 0f);
            interior.transform.localScale = Vector3.one * 0.37f;
            interior.SetActive(false);

            // Construct the entire interior architecture
            BuildFloors(interior.transform);
            BuildWalls(interior.transform);
            BuildWindowsAndLighting(interior.transform);

            // Furnish each room with rich, cozy decor
            BuildCentralLivingRoom(interior.transform);
            BuildKitchenDining(interior.transform);
            BuildStudyLibrary(interior.transform);
            BuildDressingRoom(interior.transform);
            BuildNursery(interior.transform);
            BuildTeaGardenRoom(interior.transform);
            BuildEntertainmentRoom(interior.transform);
            BuildMasterBedroom(interior.transform);
            BuildCellar(interior.transform);

            // Build Exterior
            var exterior = new GameObject("Exterior");
            exterior.transform.SetParent(home.transform, false);
            BuildExterior(exterior.transform);

            // Door component on home root: spans entire front porch and entrance
            var door = home.AddComponent<KhoaHouseDoor2DView>();
            door.Initialize(home.transform, interior, exterior);

            var exitObject = new GameObject("Khoa House Interior Exit Door");
            exitObject.transform.SetParent(interior.transform, false);
            exitObject.transform.localPosition = new Vector3(5f, -4.5f, 0f);
            exitObject.AddComponent<BoxCollider2D>().size = new Vector2(3.5f, 0.6f);
            exitObject.AddComponent<KhoaHouseDoor2DView>().InitializeInteriorExit(door);
        }

        #region Exterior Construction
        private static void BuildExterior(Transform root)
        {
            var sr = root.gameObject.AddComponent<SpriteRenderer>();
            sr.sprite = GetProceduralSprite("house_exterior_facade");
            sr.sortingOrder = Mathf.RoundToInt(-(root.position.y) * 100);
        }
        #endregion

        #region Floors
        private static void BuildFloors(Transform root)
        {
            // Infinite dark background
            Rect(root, "Map Void Background", new Vector2(7.5f, -0.5f), new Vector2(85f, 65f), VoidBlack, -9000);

            // Continuous rich timber foundation base
            Rect(root, "Farmhouse Timber Foundation", new Vector2(7.5f, -0.5f), new Vector2(54f, 40f), new Color(0.24f, 0.14f, 0.08f), -8500);

            // Ground Floor: Living Hall, Study & Library, Nursery (Warm Oak Planks)
            FloorSurface(root, "Floor_LivingHall", new Vector2(3f, 1.5f), new Vector2(14f, 13f), "floor_oak");
            FloorSurface(root, "Floor_StudyLibrary", new Vector2(17f, 1.5f), new Vector2(14f, 13f), "floor_oak");
            FloorSurface(root, "Floor_Nursery", new Vector2(3f, 12.5f), new Vector2(14f, 9f), "floor_oak");

            // Kitchen & Dining: Warm Terracotta Tile
            FloorSurface(root, "Floor_KitchenDining", new Vector2(-11f, 1.5f), new Vector2(14f, 13f), "floor_kitchen_tile");

            // Sunroom & Tea Garden: Parquet / Herringbone
            FloorSurface(root, "Floor_TeaGarden", new Vector2(17f, 12.5f), new Vector2(14f, 9f), "floor_parquet");

            // Entertainment & Craft Room: Rich Chestnut Parquet
            FloorSurface(root, "Floor_Entertainment", new Vector2(-11f, 12.5f), new Vector2(14f, 9f), "floor_parquet");

            // Dressing Room (replacing small spouse bedroom): Warm Oak
            FloorSurface(root, "Floor_DressingRoom", new Vector2(28.5f, 4.75f), new Vector2(9f, 6.5f), "floor_oak");

            // Bathroom: Elegant Seafoam Mosaic Tile
            FloorSurface(root, "Floor_Bathroom", new Vector2(28.5f, -1.75f), new Vector2(9f, 6.5f), "floor_bath_tile");

            // Master Bedroom (Big Bedroom at the bottom right): Warm Oak Planks
            FloorSurface(root, "Floor_MasterBedroom", new Vector2(17f, -11.5f), new Vector2(14f, 13f), "floor_oak");

            // Wine Cellar: Aged Rustic Cobblestone
            FloorSurface(root, "Floor_WineCellar", new Vector2(-11f, -11.5f), new Vector2(14f, 13f), "floor_cellar_stone");

            // Subtle room threshold transitions (polished brass / oak threshold strips)
            Threshold(root, "Threshold_Kitchen_Living", new Vector2(-4f, 1.5f), new Vector2(0.5f, 3.6f));
            Threshold(root, "Threshold_Living_Study", new Vector2(10f, 1.5f), new Vector2(0.5f, 3.6f));
            Threshold(root, "Threshold_Study_Dressing", new Vector2(24f, 3.5f), new Vector2(0.5f, 3.0f));
            Threshold(root, "Threshold_Dressing_Bath", new Vector2(28.5f, 1.5f), new Vector2(3.0f, 0.5f));
            Threshold(root, "Threshold_Living_Nursery", new Vector2(3f, 8f), new Vector2(4.0f, 0.5f));
            Threshold(root, "Threshold_Nursery_Tea", new Vector2(10f, 12.5f), new Vector2(0.5f, 3.2f));
            Threshold(root, "Threshold_Nursery_Ent", new Vector2(-4f, 12.5f), new Vector2(0.5f, 3.2f));
            Threshold(root, "Threshold_Study_Tea", new Vector2(17f, 8f), new Vector2(4.0f, 0.5f));
            Threshold(root, "Threshold_Kitchen_Cellar", new Vector2(-11f, -5f), new Vector2(4.0f, 0.5f));
            Threshold(root, "Threshold_Study_Master", new Vector2(17f, -5f), new Vector2(4.0f, 0.5f));
        }

        private static void FloorSurface(Transform root, string name, Vector2 center, Vector2 size, string spriteKey)
        {
            var item = new GameObject(name);
            item.transform.SetParent(root, false);
            item.transform.localPosition = new Vector3(center.x, center.y, 0f);
            item.transform.localScale = new Vector3(size.x, size.y, 1f);
            var renderer = item.AddComponent<SpriteRenderer>();
            renderer.sprite = GetProceduralSprite(spriteKey);
            renderer.sortingOrder = -8000;
        }

        private static void Threshold(Transform root, string name, Vector2 center, Vector2 size)
        {
            var item = Rect(root, name, center, size, new Color(0.48f, 0.32f, 0.20f), -7900);
            Rect(item.transform, name + "_Highlight", Vector2.zero, new Vector2(size.x > size.y ? size.x : 0.08f, size.y > size.x ? size.y : 0.08f), new Color(0.75f, 0.55f, 0.32f), -7890);
        }
        #endregion

        #region 2.5D Walls & Partitions
        private static void BuildWalls(Transform root)
        {
            // North Walls (2.5D vertical wall faces with crown molding & wallpaper)
            BuildNorthWall(root, "NorthWall_Entertainment", -18f, -4f, 17f, "wall_wood");
            BuildNorthWall(root, "NorthWall_Nursery", -4f, 10f, 17f, "wall_nursery");
            BuildNorthWall(root, "NorthWall_TeaGarden", 10f, 24f, 17f, "wall_wood");

            BuildNorthWall(root, "MidWall_Kitchen", -18f, -4f, 8f, "wall_kitchen");
            BuildNorthWallWithOpening(root, "MidWall_Living_Nursery", -4f, 10f, 8f, 3f, 4.0f, "wall_wood");
            BuildNorthWallWithOpening(root, "MidWall_Study_Tea", 10f, 24f, 8f, 17f, 4.0f, "wall_wood");
            BuildNorthWall(root, "NorthWall_Dressing", 24f, 33f, 8f, "wall_wood");

            // Cellar North Wall with clear opening at x=-11 (width 4.0) so player can walk through!
            BuildNorthWallWithOpening(root, "NorthWall_Cellar", -18f, -4f, -5f, -11f, 4.0f, "wall_stone");
            // Master Bedroom North Wall with clear opening at x=17 (width 4.0)
            BuildNorthWallWithOpening(root, "NorthWall_Master", 10f, 24f, -5f, 17f, 4.0f, "wall_wood");

            // Outer Perimeter Walls (solid thick boundaries with colliders)
            // West Outer Wall
            WallSolid(root, "OuterWall_West", new Vector2(-18.35f, -0.5f), new Vector2(0.7f, 36f));
            // East Outer Wall
            WallSolid(root, "OuterWall_East_North", new Vector2(24.35f, 12.5f), new Vector2(0.7f, 10f));
            WallSolid(root, "OuterWall_East_Dressing", new Vector2(33.35f, 1.5f), new Vector2(0.7f, 14f));
            WallSolid(root, "OuterWall_East_Master", new Vector2(24.35f, -11.5f), new Vector2(0.7f, 14f));

            // North Outer Wall Cap
            WallSolid(root, "OuterWall_North_Cap", new Vector2(3f, 17.35f), new Vector2(43f, 0.7f));
            // South Outer Wall Caps
            WallSolid(root, "OuterWall_South_Cellar", new Vector2(-11f, -18.35f), new Vector2(15f, 0.7f));
            WallSolid(root, "OuterWall_South_Master", new Vector2(17f, -18.35f), new Vector2(15f, 0.7f));
            WallSolid(root, "OuterWall_South_Bath", new Vector2(28.5f, -5.35f), new Vector2(10f, 0.7f));

            // Front Wall of Main Floor (contains front entrance doorway at x=5)
            WallSolidWithOpening(root, "FrontWall_Living", -4f, 10f, -5f, 5f, 3.5f);
            // Kitchen Front Wall with clear opening at x=-11 (width 4.0) down into Wine Cellar!
            WallSolidWithOpening(root, "FrontWall_Kitchen", -18f, -4f, -5f, -11f, 4.0f);

            // Interior Vertical Dividers (with elegant door arches)
            VerticalPartition(root, "Div_Kitchen_Living", -4f, -5f, 8f, 1.5f, 3.6f);
            VerticalPartition(root, "Div_Living_Study", 10f, -5f, 8f, 1.5f, 3.6f);
            VerticalPartition(root, "Div_Study_Dressing", 24f, -5f, 8f, 3.5f, 3.0f);
            VerticalPartition(root, "Div_Ent_Nursery", -4f, 8f, 17f, 12.5f, 3.2f);
            VerticalPartition(root, "Div_Nursery_Tea", 10f, 8f, 17f, 12.5f, 3.2f);
            HorizontalPartition(root, "Div_Dressing_Bath", 24f, 33f, 1.5f, 28.5f, 3.0f);

            // Cellar and Master East/West interior dividing walls
            WallSolid(root, "Div_Cellar_East", new Vector2(-3.65f, -11.5f), new Vector2(0.7f, 13f));
            WallSolid(root, "Div_Master_West", new Vector2(9.65f, -11.5f), new Vector2(0.7f, 13f));
        }

        private static void BuildNorthWall(Transform root, string name, float xMin, float xMax, float yTop, string styleKey)
        {
            float width = xMax - xMin;
            float height = 2.4f;
            var wall = new GameObject(name);
            wall.transform.SetParent(root, false);
            wall.transform.localPosition = new Vector3((xMin + xMax) * 0.5f, yTop - height * 0.5f, 0f);
            wall.transform.localScale = new Vector3(width, height, 1f);
            var renderer = wall.AddComponent<SpriteRenderer>();
            renderer.sprite = GetProceduralSprite(styleKey);
            renderer.sortingOrder = SortingOrder(root, new Vector2((xMin + xMax) * 0.5f, yTop), -2);

            // Solid top collider so player cannot walk through the top wall
            var collider = wall.AddComponent<BoxCollider2D>();
            collider.size = new Vector2(1f, 0.35f);
            collider.offset = new Vector2(0f, 0.32f);
        }

        private static void BuildNorthWallWithOpening(Transform root, string name, float xMin, float xMax, float yTop, float openX, float openW, string styleKey)
        {
            if (openW <= 0f)
            {
                BuildNorthWall(root, name, xMin, xMax, yTop, styleKey);
                return;
            }
            float half = openW * 0.5f;
            if (openX - half > xMin)
                BuildNorthWall(root, name + "_L", xMin, openX - half, yTop, styleKey);
            if (openX + half < xMax)
                BuildNorthWall(root, name + "_R", openX + half, xMax, yTop, styleKey);

            // Overhead lintel beam connecting the opening
            var lintel = Rect(root, name + "_Lintel", new Vector2(openX, yTop - 0.3f), new Vector2(openW + 0.3f, 0.6f), WallDividerWood, 10);
            Rect(lintel.transform, "Trim", Vector2.zero, new Vector2(openW + 0.2f, 0.1f), WallTrimGold, 11);
        }

        private static void VerticalPartition(Transform root, string name, float x, float yMin, float yMax, float openY, float openH)
        {
            float half = openH * 0.5f;
            if (openY - half > yMin)
                WallSolid(root, name + "_Lower", new Vector2(x, (yMin + openY - half) * 0.5f), new Vector2(0.6f, openY - half - yMin));
            if (openY + half < yMax)
                WallSolid(root, name + "_Upper", new Vector2(x, (openY + half + yMax) * 0.5f), new Vector2(0.6f, yMax - (openY + half)));

            // Overhead door arch post & lintel
            Rect(root, name + "_ArchHeader", new Vector2(x, openY + half - 0.15f), new Vector2(0.7f, 0.4f), WallDividerWood, 10);
            Rect(root, name + "_PostBottom", new Vector2(x, openY - half + 0.15f), new Vector2(0.7f, 0.3f), WallDividerWood, 5);
        }

        private static void HorizontalPartition(Transform root, string name, float xMin, float xMax, float y, float openX, float openW)
        {
            float half = openW * 0.5f;
            if (openX - half > xMin)
                WallSolid(root, name + "_Left", new Vector2((xMin + openX - half) * 0.5f, y), new Vector2(openX - half - xMin, 0.6f));
            if (openX + half < xMax)
                WallSolid(root, name + "_Right", new Vector2((openX + half + xMax) * 0.5f, y), new Vector2(xMax - (openX + half), 0.6f));
        }

        private static void WallSolidWithOpening(Transform root, string name, float xMin, float xMax, float y, float openX, float openW)
        {
            float half = openW * 0.5f;
            if (openX - half > xMin)
                WallSolid(root, name + "_Left", new Vector2((xMin + openX - half) * 0.5f, y), new Vector2(openX - half - xMin, 0.6f));
            if (openX + half < xMax)
                WallSolid(root, name + "_Right", new Vector2((openX + half + xMax) * 0.5f, y), new Vector2(xMax - (openX + half), 0.6f));
        }

        private static GameObject WallSolid(Transform root, string name, Vector2 position, Vector2 size)
        {
            var wall = Rect(root, name, position, size, OuterWallDark, 0, true);
            // Decorative wood cap
            Rect(wall.transform, name + "_Cap", Vector2.zero, new Vector2(size.x, size.y * 0.35f), OuterWallCap, 1);
            return wall;
        }
        #endregion

        #region Windows & Atmospheric Lighting
        private static void BuildWindowsAndLighting(Transform root)
        {
            // 0. He thong den chieu sang am cung toan bo can nha cua Khoa (Indoor Warm Lighting)
            // Windows with morning sunbeams
            AddWindowWithSunbeam(root, "Window_Ent_Left", new Vector2(-15f, 15.6f));
            AddWindowWithSunbeam(root, "Window_Ent_Right", new Vector2(-7f, 15.6f));
            AddWindowWithSunbeam(root, "Window_Nursery_Left", new Vector2(-1f, 15.6f));
            AddWindowWithSunbeam(root, "Window_Nursery_Right", new Vector2(7f, 15.6f));
            AddWindowWithSunbeam(root, "Window_Tea_Center", new Vector2(17f, 15.6f));
            AddWindowWithSunbeam(root, "Window_Dressing", new Vector2(28.5f, 6.6f));
        }


        private static void AddWindowWithSunbeam(Transform root, string name, Vector2 pos)
        {
            var win = new GameObject(name);
            win.transform.SetParent(root, false);
            win.transform.localPosition = new Vector3(pos.x, pos.y, 0f);
            win.transform.localScale = new Vector3(2.4f, 2.0f, 1f);
            var rend = win.AddComponent<SpriteRenderer>();
            rend.sprite = GetProceduralSprite("window_arched");
            rend.sortingOrder = SortingOrder(root, pos, 2);

            // Soft golden sunbeam projecting across the floor
            var beam = new GameObject(name + "_Sunbeam");
            beam.transform.SetParent(root, false);
            beam.transform.localPosition = new Vector3(pos.x + 0.8f, pos.y - 2.8f, 0f);
            beam.transform.localScale = new Vector3(3.2f, 4.0f, 1f);
            var beamRend = beam.AddComponent<SpriteRenderer>();
            beamRend.sprite = GetProceduralSprite("sunbeam");
            beamRend.sortingOrder = -7500;
        }
        #endregion

        #region Room Furnishings
        private static void BuildCentralLivingRoom(Transform root)
        {
            // Front Entrance Welcome Mat (right before the exit door at 5, -4.5)
            Prop(root, "Welcome_Mat", new Vector2(5f, -3.9f), new Vector2(2.6f, 1.4f), "mat_welcome", -7600);

            // Grand Fireplace & crackling hearth (Left wall of living room)
            Prop(root, "Living_Fireplace", new Vector2(-1.5f, 6.6f), new Vector2(3.0f, 2.4f), "fireplace", 1, true, 2.6f, 1.0f);
            AddLightGlow(root, "Living_Fireplace_Glow", new Vector2(-1.5f, 5.8f), 3.5f, new Color(1f, 0.55f, 0.18f, 0.25f));

            // Elegant floral Persian rug
            Prop(root, "Living_Persian_Rug", new Vector2(3.0f, 1.2f), new Vector2(7.2f, 5.0f), "rug_persian", -7600);

            // Luxurious tufted ruby velvet sofa
            Prop(root, "Living_Sofa", new Vector2(3.0f, 2.6f), new Vector2(4.4f, 2.0f), "sofa_red", 2, true, 4.0f, 1.0f);

            // Carved wood coffee table with tea set
            Prop(root, "Living_Coffee_Table", new Vector2(3.0f, 0.8f), new Vector2(3.2f, 1.5f), "coffee_table", 2, true, 2.8f, 0.8f);

            // Matching plush armchairs flanking the coffee table
            Prop(root, "Living_Armchair_Left", new Vector2(0.2f, 1.0f), new Vector2(1.8f, 1.8f), "armchair_red", 2, true, 1.4f, 0.9f);
            Prop(root, "Living_Armchair_Right", new Vector2(5.8f, 1.0f), new Vector2(1.8f, 1.8f), "armchair_red", 2, true, 1.4f, 0.9f);

            // Tall oak bookcase packed with colorful books
            Prop(root, "Living_Bookcase", new Vector2(7.8f, 6.6f), new Vector2(2.6f, 2.6f), "bookcase", 1, true, 2.4f, 0.9f);

            // Vintage Grandfather Clock
            Prop(root, "Living_Grandfather_Clock", new Vector2(-3.0f, 6.6f), new Vector2(1.3f, 2.6f), "clock_grandfather", 1, true, 1.0f, 0.8f);

            // Brass floor lamp casting warm light
            Prop(root, "Living_Floor_Lamp", new Vector2(-3.0f, 1.6f), new Vector2(1.1f, 2.4f), "lamp_brass", 2, true, 0.8f, 0.6f);
            AddLightGlow(root, "Living_Lamp_Glow", new Vector2(-3.0f, 1.6f), 2.2f, new Color(1f, 0.85f, 0.45f, 0.22f));

            // Lush potted indoor palm / monstera
            Prop(root, "Living_Plant_1", new Vector2(8.2f, -3.2f), new Vector2(1.4f, 2.0f), "plant_monstera", 2, true, 1.0f, 0.7f);
            Prop(root, "Living_Plant_2", new Vector2(-2.8f, -3.2f), new Vector2(1.4f, 2.0f), "plant_fern", 2, true, 1.0f, 0.7f);
        }

        private static void BuildKitchenDining(Transform root)
        {
            // Kitchen Countertop along North wall with sink and cabinets
            Prop(root, "Kitchen_Counter_Sink", new Vector2(-15.2f, 6.5f), new Vector2(3.6f, 2.2f), "counter_sink", 1, true, 3.4f, 1.0f);
            Prop(root, "Kitchen_Counter_Prep", new Vector2(-11.8f, 6.5f), new Vector2(3.2f, 2.2f), "counter_drawers", 1, true, 3.0f, 1.0f);

            // Cast-iron country stove & oven with copper kettle
            Prop(root, "Kitchen_CastIron_Stove", new Vector2(-8.8f, 6.5f), new Vector2(2.4f, 2.4f), "stove_iron", 1, true, 2.2f, 1.0f);

            // Retro mint-green refrigerator
            Prop(root, "Kitchen_Retro_Fridge", new Vector2(-6.2f, 6.6f), new Vector2(2.0f, 2.8f), "fridge_retro", 1, true, 1.8f, 1.0f);

            // L-shaped counter return on the West wall
            Prop(root, "Kitchen_Counter_West", new Vector2(-17.0f, 3.5f), new Vector2(1.5f, 3.6f), "counter_side", 1, true, 1.2f, 3.2f);

            // Farmhouse Dining Table with lace runner & fruit basket
            Prop(root, "Kitchen_Dining_Table", new Vector2(-11.0f, 0.8f), new Vector2(4.6f, 2.4f), "dining_table", 2, true, 4.2f, 1.4f);

            // Dining chairs around the table
            Prop(root, "Dining_Chair_N1", new Vector2(-12.2f, 2.3f), new Vector2(1.2f, 1.2f), "chair_dining_n", 1, true, 0.9f, 0.6f);
            Prop(root, "Dining_Chair_N2", new Vector2(-9.8f, 2.3f), new Vector2(1.2f, 1.2f), "chair_dining_n", 1, true, 0.9f, 0.6f);
            Prop(root, "Dining_Chair_S1", new Vector2(-12.2f, -0.6f), new Vector2(1.2f, 1.2f), "chair_dining_s", 3, true, 0.9f, 0.6f);
            Prop(root, "Dining_Chair_S2", new Vector2(-9.8f, -0.6f), new Vector2(1.2f, 1.2f), "chair_dining_s", 3, true, 0.9f, 0.6f);

            // Smooth descending stone stairs leading down to the Wine Cellar
            Prop(root, "Cellar_Stairs_Descent", new Vector2(-11.0f, -4.5f), new Vector2(3.6f, 2.0f), "stairs_stone_down", -7500);

            // Hanging garlic, herbs & copper pans on the wall
            Prop(root, "Kitchen_Pans_Rack", new Vector2(-13.5f, 7.3f), new Vector2(2.0f, 0.8f), "pan_rack", 5);
        }

        private static void BuildStudyLibrary(Transform root)
        {
            // Grand bookcases lining the upper wall
            Prop(root, "Study_Bookcase_1", new Vector2(13.2f, 6.6f), new Vector2(2.6f, 2.6f), "bookcase", 1, true, 2.4f, 0.9f);
            Prop(root, "Study_Bookcase_2", new Vector2(20.8f, 6.6f), new Vector2(2.6f, 2.6f), "bookcase", 1, true, 2.4f, 0.9f);

            // Grandfather clock in corner
            Prop(root, "Study_Clock", new Vector2(22.8f, 6.6f), new Vector2(1.3f, 2.6f), "clock_grandfather", 1, true, 1.0f, 0.8f);

            // Executive writing desk & leather chair
            Prop(root, "Study_Writing_Desk", new Vector2(17.0f, 3.6f), new Vector2(3.6f, 2.2f), "desk_writing", 2, true, 3.2f, 1.2f);
            Prop(root, "Study_Desk_Chair", new Vector2(17.0f, 4.8f), new Vector2(1.2f, 1.2f), "chair_dining_n", 1, true, 0.9f, 0.6f);

            // Persian Medallion Area Rug
            Prop(root, "Study_Persian_Rug", new Vector2(17.0f, 1.6f), new Vector2(6.8f, 4.6f), "rug_persian", -7600);

            // Reading armchairs with side table and warm floor lamp
            Prop(root, "Study_Armchair_L", new Vector2(13.2f, 0.8f), new Vector2(1.8f, 1.8f), "armchair_red", 2, true, 1.4f, 0.9f);
            Prop(root, "Study_Armchair_R", new Vector2(20.8f, 0.8f), new Vector2(1.8f, 1.8f), "armchair_red", 2, true, 1.4f, 0.9f);
            Prop(root, "Study_Side_Table", new Vector2(17.0f, 0.8f), new Vector2(1.8f, 1.4f), "coffee_table", 2, true, 1.4f, 0.7f);

            // Stairs leading down to the Master Bedroom Suite below
            Prop(root, "Study_Stairs_To_Master", new Vector2(17.0f, -4.5f), new Vector2(3.6f, 2.0f), "stairs_wood_down", -7500);
        }

        private static void BuildDressingRoom(Transform root)
        {
            // Dressing Room (PhÃ²ng thay Ä‘á»“ thay tháº¿ cho phÃ²ng ngá»§ nhá»)
            // Elegant large oak wardrobe
            Prop(root, "Dressing_Armoire", new Vector2(28.5f, 6.6f), new Vector2(2.6f, 3.0f), "wardrobe_oak", 1, true, 2.4f, 1.0f);

            // Garment hanging rack with stylish coats and outfits
            Prop(root, "Dressing_Garment_Rack", new Vector2(25.6f, 6.5f), new Vector2(2.2f, 2.4f), "garment_rack", 1, true, 2.0f, 0.9f);

            // Full-length standing Cheval mirror
            Prop(root, "Dressing_Mirror_Standing", new Vector2(31.6f, 4.8f), new Vector2(1.6f, 2.8f), "mirror_standing", 2, true, 1.2f, 0.8f);

            // Shoe rack & storage bench
            Prop(root, "Dressing_Shoe_Rack", new Vector2(31.6f, 2.4f), new Vector2(1.8f, 1.4f), "shoe_rack", 2, true, 1.6f, 0.8f);
            Prop(root, "Dressing_Bench", new Vector2(28.5f, 3.8f), new Vector2(2.4f, 1.4f), "dressing_bench", 2, true, 2.0f, 0.8f);

            // Soft plush runner rug
            Prop(root, "Dressing_Runner_Rug", new Vector2(28.5f, 3.8f), new Vector2(4.6f, 2.4f), "rug_runner", -7600);

            // Potted orchid on stand
            Prop(root, "Dressing_Plant_Orchid", new Vector2(25.4f, 2.6f), new Vector2(1.2f, 1.6f), "vase_sakura", 2, true, 0.8f, 0.6f);

            // Bathroom below: Clawfoot porcelain bathtub with brass fixtures
            Prop(root, "Bathroom_Clawfoot_Tub", new Vector2(26.2f, -1.8f), new Vector2(3.2f, 2.2f), "tub_clawfoot", 2, true, 2.8f, 1.1f);

            // Pedestal sink & gilded wall mirror
            Prop(root, "Bathroom_Vanity_Sink", new Vector2(30.8f, 0.0f), new Vector2(1.8f, 2.4f), "sink_pedestal", 1, true, 1.4f, 0.8f);

            // Classic porcelain toilet
            Prop(root, "Bathroom_Toilet", new Vector2(31.0f, -3.2f), new Vector2(1.4f, 1.8f), "toilet_porcelain", 2, true, 1.1f, 0.8f);

            // Soft plush bath mat
            Prop(root, "Bathroom_Mat", new Vector2(26.2f, -3.2f), new Vector2(2.2f, 1.4f), "mat_bath", -7600);
        }

        private static void BuildNursery(Transform root)
        {
            // Soft pastel rainbow star rug
            Prop(root, "Nursery_Pastel_Rug", new Vector2(3.0f, 12.0f), new Vector2(5.5f, 4.2f), "rug_nursery", -7600);

            // Carved wooden baby crib with canopy & mobile
            Prop(root, "Nursery_Crib", new Vector2(0.0f, 14.8f), new Vector2(2.8f, 2.4f), "crib_baby", 1, true, 2.5f, 1.0f);

            // Child's single bed with star patterned quilt
            Prop(root, "Nursery_Child_Bed", new Vector2(6.5f, 14.8f), new Vector2(3.0f, 2.6f), "bed_child", 1, true, 2.6f, 1.2f);

            // Toy chest filled with colorful plushies
            Prop(root, "Nursery_Toy_Chest", new Vector2(3.0f, 14.8f), new Vector2(2.0f, 1.4f), "toy_chest", 1, true, 1.8f, 0.8f);

            // Miniature low bookshelf with fairy tale storybooks
            Prop(root, "Nursery_Kids_Bookshelf", new Vector2(-2.2f, 11.5f), new Vector2(2.0f, 2.0f), "bookcase_small", 2, true, 1.8f, 0.8f);

            // Giant plush teddy bear sitting in corner
            Prop(root, "Nursery_Teddy_Bear", new Vector2(8.5f, 11.2f), new Vector2(1.6f, 1.8f), "teddy_bear", 2, true, 1.2f, 0.8f);
        }

        private static void BuildTeaGardenRoom(Transform root)
        {
            // Tatami / woven bamboo mat
            Prop(root, "TeaGarden_Mat", new Vector2(17.0f, 12.2f), new Vector2(7.2f, 4.8f), "mat_bamboo", -7600);

            // Low Japanese tea table with teapot, cups, and matcha bowl
            Prop(root, "TeaGarden_Table", new Vector2(17.0f, 12.2f), new Vector2(3.4f, 1.6f), "tea_table", 2, true, 3.0f, 0.8f);

            // Zafu floor cushions around the table
            Prop(root, "TeaGarden_Cushion_L", new Vector2(14.5f, 12.2f), new Vector2(1.2f, 1.0f), "cushion_gold", 2);
            Prop(root, "TeaGarden_Cushion_R", new Vector2(19.5f, 12.2f), new Vector2(1.2f, 1.0f), "cushion_gold", 2);
            Prop(root, "TeaGarden_Cushion_T", new Vector2(17.0f, 13.5f), new Vector2(1.2f, 1.0f), "cushion_gold", 1);
            Prop(root, "TeaGarden_Cushion_B", new Vector2(17.0f, 10.9f), new Vector2(1.2f, 1.0f), "cushion_gold", 3);

            // Miniature Bonsai Tree display on carved stand
            Prop(root, "TeaGarden_Bonsai", new Vector2(21.8f, 15.0f), new Vector2(1.8f, 2.0f), "bonsai_pine", 1, true, 1.4f, 0.8f);

            // Flowering cherry blossom branches in porcelain vase
            Prop(root, "TeaGarden_Vase_Sakura", new Vector2(12.2f, 15.0f), new Vector2(1.6f, 2.2f), "vase_sakura", 1, true, 1.2f, 0.8f);

            // Hanging scroll painting of mountains and mist
            Prop(root, "TeaGarden_Scroll_Painting", new Vector2(17.0f, 16.0f), new Vector2(2.0f, 2.2f), "scroll_mountain", 5);
        }

        private static void BuildEntertainmentRoom(Transform root)
        {
            // Cozy Hearth Fireplace on West wall
            Prop(root, "Ent_Fireplace", new Vector2(-16.4f, 14.5f), new Vector2(2.4f, 2.4f), "fireplace", 1, true, 2.0f, 0.9f);
            AddLightGlow(root, "Ent_Fireplace_Glow", new Vector2(-16.4f, 13.8f), 2.8f, new Color(1f, 0.52f, 0.15f, 0.22f));

            // Retro wooden cabinet Television set between the 2 windows
            Prop(root, "Ent_Retro_TV", new Vector2(-11.0f, 15.6f), new Vector2(2.6f, 2.2f), "tv_retro", 1, true, 2.2f, 0.9f);

            // Comfortable 2-seater loveseat sofa facing the TV
            Prop(root, "Ent_Loveseat", new Vector2(-11.0f, 11.8f), new Vector2(3.4f, 1.8f), "sofa_blue", 2, true, 3.0f, 0.9f);

            // Braided oval rug under loveseat
            Prop(root, "Ent_Oval_Rug", new Vector2(-11.0f, 12.2f), new Vector2(5.0f, 3.5f), "rug_braided", -7600);

            // Sewing machine craft table with fabrics & spools
            Prop(root, "Ent_Sewing_Table", new Vector2(-6.2f, 14.5f), new Vector2(2.6f, 1.8f), "sewing_table", 1, true, 2.2f, 0.8f);

            // Dresser for fabrics and yarn
            Prop(root, "Ent_Yarn_Dresser", new Vector2(-16.4f, 10.8f), new Vector2(2.0f, 1.6f), "wardrobe_oak", 2, true, 1.6f, 0.8f);
        }

        private static void BuildMasterBedroom(Transform root)
        {
            // PHÃ’NG NGá»¦ TO á»ž GÃ“C DÆ¯á»šI BÃŠN PHáº¢I (Master Bedroom Suite at bottom right)
            // Grand King-Size Master Bed with carved posts and rich duvet
            Prop(root, "Master_Double_Bed", new Vector2(17.0f, -12.5f), new Vector2(4.4f, 3.8f), "bed_king", 2, true, 4.0f, 2.0f);

            // Bed interaction component for sleep mechanic
            var bedTarget = new GameObject("Khoa Bed - Sleep Interaction");
            bedTarget.transform.SetParent(root, false);
            bedTarget.transform.localPosition = new Vector3(17.0f, -12.5f, 0f);
            bedTarget.AddComponent<KhoaBed2DView>();

            // Royal braided round rug under / beside bed
            Prop(root, "Master_Round_Rug", new Vector2(17.0f, -13.2f), new Vector2(7.2f, 5.2f), "rug_braided", -7600);

            // Nightstands with warm lamps flanking the bed
            Prop(root, "Master_Nightstand_Left", new Vector2(13.8f, -12.2f), new Vector2(1.4f, 1.5f), "nightstand", 2, true, 1.1f, 0.8f);
            Prop(root, "Master_Lamp_Left", new Vector2(13.8f, -11.2f), new Vector2(0.9f, 1.2f), "lamp_table", 3);
            AddLightGlow(root, "Master_Lamp_Left_Glow", new Vector2(13.8f, -11.6f), 1.8f, new Color(1f, 0.88f, 0.52f, 0.25f));

            Prop(root, "Master_Nightstand_Right", new Vector2(20.2f, -12.2f), new Vector2(1.4f, 1.5f), "nightstand", 2, true, 1.1f, 0.8f);
            Prop(root, "Master_Lamp_Right", new Vector2(20.2f, -11.2f), new Vector2(0.9f, 1.2f), "lamp_table", 3);
            AddLightGlow(root, "Master_Lamp_Right_Glow", new Vector2(20.2f, -11.6f), 1.8f, new Color(1f, 0.88f, 0.52f, 0.25f));

            // Cozy brick fireplace in the master bedroom
            Prop(root, "Master_Fireplace", new Vector2(22.2f, -15.8f), new Vector2(2.6f, 2.4f), "fireplace", 1, true, 2.2f, 0.9f);
            AddLightGlow(root, "Master_Fireplace_Glow", new Vector2(22.2f, -15.0f), 3.0f, new Color(1f, 0.52f, 0.15f, 0.22f));

            // Large wooden wardrobe with brass handles
            Prop(root, "Master_Wardrobe", new Vector2(12.0f, -8.0f), new Vector2(2.6f, 3.2f), "wardrobe_oak", 1, true, 2.4f, 1.0f);

            // Vanity table with oval mirror
            Prop(root, "Master_Vanity_Dresser", new Vector2(22.0f, -8.0f), new Vector2(2.4f, 2.4f), "vanity_dresser", 1, true, 2.0f, 0.9f);

            // Reading wingback armchair & indoor plant
            Prop(root, "Master_Armchair", new Vector2(12.0f, -15.5f), new Vector2(1.8f, 1.8f), "armchair_red", 2, true, 1.4f, 0.9f);
            Prop(root, "Master_Plant", new Vector2(14.0f, -16.5f), new Vector2(1.2f, 1.8f), "plant_monstera", 2, true, 0.9f, 0.6f);
        }

        private static void BuildCellar(Transform root)
        {
            // Rustic aging oak wine casks stacked along the walls
            for (int r = 0; r < 3; r++)
            {
                float y = -9.0f - r * 3.0f;
                Prop(root, $"Cellar_Cask_W_{r}", new Vector2(-15.8f, y), new Vector2(2.2f, 1.8f), "cask_oak", 2, true, 1.8f, 1.0f);
                Prop(root, $"Cellar_Cask_E_{r}", new Vector2(-6.2f, y), new Vector2(2.2f, 1.8f), "cask_oak", 2, true, 1.8f, 1.0f);
            }

            // Central wine tasting barrels & vintage crates
            Prop(root, "Cellar_Wine_Crate_1", new Vector2(-11.0f, -11.5f), new Vector2(2.4f, 1.6f), "wine_crates", 2, true, 2.0f, 0.9f);
            Prop(root, "Cellar_Cask_Center", new Vector2(-11.0f, -15.5f), new Vector2(2.4f, 1.8f), "cask_oak", 2, true, 2.0f, 1.0f);

            // Wall lanterns with warm atmospheric glow
            Prop(root, "Cellar_Lantern_W", new Vector2(-17.2f, -11.0f), new Vector2(0.8f, 1.2f), "lamp_brass", 3);
            AddLightGlow(root, "Cellar_Lantern_W_Glow", new Vector2(-17.2f, -11.0f), 2.2f, new Color(1f, 0.75f, 0.35f, 0.22f));

            Prop(root, "Cellar_Lantern_E", new Vector2(-4.8f, -11.0f), new Vector2(0.8f, 1.2f), "lamp_brass", 3);
            AddLightGlow(root, "Cellar_Lantern_E_Glow", new Vector2(-4.8f, -11.0f), 2.2f, new Color(1f, 0.75f, 0.35f, 0.22f));
        }
        #endregion

        #region Helpers & Procedural Sprite Art
        private static GameObject Prop(Transform root, string name, Vector2 pos, Vector2 size, string spriteKey, int layerOrder = 2, bool hasCollider = false, float colW = 1f, float colH = 1f)
        {
            var item = new GameObject(name);
            item.transform.SetParent(root, false);
            item.transform.localPosition = new Vector3(pos.x, pos.y, 0f);
            item.transform.localScale = new Vector3(size.x, size.y, 1f);

            var renderer = item.AddComponent<SpriteRenderer>();
            renderer.sprite = GetProceduralSprite(spriteKey);
            renderer.sortingOrder = layerOrder <= -1000 ? layerOrder : SortingOrder(root, pos, layerOrder);

            if (hasCollider)
            {
                var col = item.AddComponent<BoxCollider2D>();
                // Normalize collider footprint to lower portion of furniture
                col.size = new Vector2(Mathf.Clamp01(colW / size.x), Mathf.Clamp01(colH / size.y));
                col.offset = new Vector2(0f, -0.5f + col.size.y * 0.5f);
            }

            return item;
        }

        private static void AddLightGlow(Transform root, string name, Vector2 pos, float diameter, Color color)
        {
            var glow = new GameObject(name);
            glow.transform.SetParent(root, false);
            glow.transform.localPosition = new Vector3(pos.x, pos.y, 0f);
            glow.transform.localScale = new Vector3(diameter, diameter * 0.75f, 1f);
            var rend = glow.AddComponent<SpriteRenderer>();
            rend.sprite = circleSprite;
            rend.color = color;
            rend.sortingOrder = -7400;
        }

        private static Sprite GetProceduralSprite(string key)
        {
            if (spriteCache.TryGetValue(key, out var cached) && cached != null)
                return cached;

            Sprite created = null;
            switch (key)
            {
                case "floor_oak":
                    created = CreateTileSprite(64, 64, (px, w, h) => DrawWoodFloor(px, w, h, new Color32(204, 153, 94, 255), new Color32(130, 82, 42, 255)));
                    break;
                case "floor_parquet":
                    created = CreateTileSprite(64, 64, DrawParquetFloor);
                    break;
                case "floor_kitchen_tile":
                    created = CreateTileSprite(64, 64, DrawKitchenTileFloor);
                    break;
                case "floor_bath_tile":
                    created = CreateTileSprite(64, 64, DrawBathTileFloor);
                    break;
                case "floor_cellar_stone":
                    created = CreateTileSprite(64, 64, DrawCellarStoneFloor);
                    break;

                case "wall_wood":
                    created = CreateTileSprite(64, 64, (px, w, h) => DrawNorthWall(px, w, h, new Color32(228, 208, 175, 255), new Color32(165, 112, 68, 255)));
                    break;
                case "wall_nursery":
                    created = CreateTileSprite(64, 64, (px, w, h) => DrawNorthWall(px, w, h, new Color32(245, 235, 220, 255), new Color32(200, 150, 160, 255)));
                    break;
                case "wall_kitchen":
                    created = CreateTileSprite(64, 64, (px, w, h) => DrawNorthWall(px, w, h, new Color32(235, 225, 205, 255), new Color32(180, 130, 85, 255)));
                    break;
                case "wall_stone":
                    created = CreateTileSprite(64, 64, DrawStoneWall);
                    break;

                case "window_arched":
                    created = CreateTileSprite(64, 64, DrawWindowArched);
                    break;
                case "sunbeam":
                    created = CreateTileSprite(64, 64, DrawSunbeam);
                    break;

                case "fireplace":
                    created = CreateTileSprite(64, 64, DrawFireplace);
                    break;
                case "bed_king":
                    created = CreateTileSprite(80, 80, DrawMasterBed);
                    break;
                case "bed_single":
                    created = CreateTileSprite(64, 64, DrawSingleBed);
                    break;
                case "bed_child":
                    created = CreateTileSprite(64, 64, DrawChildBed);
                    break;
                case "crib_baby":
                    created = CreateTileSprite(64, 64, DrawCrib);
                    break;
                case "sofa_red":
                    created = CreateTileSprite(96, 48, (px, w, h) => DrawSofa(px, w, h, new Color32(158, 42, 52, 255), new Color32(195, 75, 85, 255)));
                    break;
                case "sofa_blue":
                    created = CreateTileSprite(96, 48, (px, w, h) => DrawSofa(px, w, h, new Color32(45, 85, 125, 255), new Color32(75, 125, 168, 255)));
                    break;
                case "armchair_red":
                    created = CreateTileSprite(48, 48, (px, w, h) => DrawArmchair(px, w, h, new Color32(158, 42, 52, 255), new Color32(195, 75, 85, 255)));
                    break;
                case "coffee_table":
                    created = CreateTileSprite(64, 32, DrawCoffeeTable);
                    break;
                case "dining_table":
                    created = CreateTileSprite(96, 48, DrawDiningTable);
                    break;
                case "chair_dining_n":
                    created = CreateTileSprite(32, 32, DrawDiningChairN);
                    break;
                case "chair_dining_s":
                    created = CreateTileSprite(32, 32, DrawDiningChairS);
                    break;
                case "counter_sink":
                    created = CreateTileSprite(80, 48, DrawCounterSink);
                    break;
                case "counter_drawers":
                    created = CreateTileSprite(64, 48, DrawCounterDrawers);
                    break;
                case "counter_side":
                    created = CreateTileSprite(32, 80, DrawCounterSide);
                    break;
                case "stove_iron":
                    created = CreateTileSprite(48, 48, DrawStoveIron);
                    break;
                case "fridge_retro":
                    created = CreateTileSprite(48, 72, DrawRetroFridge);
                    break;
                case "bookcase":
                    created = CreateTileSprite(64, 64, DrawBookcase);
                    break;
                case "bookcase_small":
                    created = CreateTileSprite(48, 48, DrawSmallBookcase);
                    break;
                case "wardrobe_oak":
                    created = CreateTileSprite(56, 72, DrawWardrobe);
                    break;
                case "vanity_dresser":
                    created = CreateTileSprite(56, 64, DrawVanityDresser);
                    break;
                case "nightstand":
                    created = CreateTileSprite(32, 36, DrawNightstand);
                    break;
                case "desk_writing":
                    created = CreateTileSprite(48, 48, DrawWritingDesk);
                    break;
                case "clock_grandfather":
                    created = CreateTileSprite(32, 72, DrawGrandfatherClock);
                    break;
                case "lamp_brass":
                    created = CreateTileSprite(24, 64, DrawFloorLamp);
                    break;
                case "lamp_table":
                    created = CreateTileSprite(24, 32, DrawTableLamp);
                    break;
                case "tub_clawfoot":
                    created = CreateTileSprite(80, 56, DrawClawfootTub);
                    break;
                case "sink_pedestal":
                    created = CreateTileSprite(40, 56, DrawPedestalSink);
                    break;
                case "toilet_porcelain":
                    created = CreateTileSprite(32, 48, DrawToilet);
                    break;
                case "mat_bath":
                    created = CreateTileSprite(48, 32, DrawBathMat);
                    break;
                case "tea_table":
                    created = CreateTileSprite(64, 32, DrawTeaTable);
                    break;
                case "cushion_gold":
                    created = CreateTileSprite(32, 28, DrawFloorCushion);
                    break;
                case "bonsai_pine":
                    created = CreateTileSprite(40, 48, DrawBonsai);
                    break;
                case "vase_sakura":
                    created = CreateTileSprite(36, 48, DrawVaseSakura);
                    break;
                case "toy_chest":
                    created = CreateTileSprite(48, 36, DrawToyChest);
                    break;
                case "teddy_bear":
                    created = CreateTileSprite(36, 44, DrawTeddyBear);
                    break;
                case "sewing_table":
                    created = CreateTileSprite(56, 40, DrawSewingTable);
                    break;
                case "tv_retro":
                    created = CreateTileSprite(56, 48, DrawRetroTV);
                    break;
                case "cask_oak":
                    created = CreateTileSprite(56, 48, DrawWineCask);
                    break;
                case "wine_crates":
                    created = CreateTileSprite(56, 40, DrawWineCrates);
                    break;
                case "stairs_stone_down":
                    created = CreateTileSprite(64, 48, DrawStairsStoneDown);
                    break;
                case "stairs_wood_down":
                    created = CreateTileSprite(64, 48, DrawStairsWoodDown);
                    break;
                case "mat_welcome":
                    created = CreateTileSprite(64, 36, DrawWelcomeMat);
                    break;
                case "rug_persian":
                    created = CreateTileSprite(96, 64, DrawPersianRug);
                    break;
                case "rug_braided":
                    created = CreateTileSprite(64, 64, DrawBraidedRug);
                    break;
                case "rug_nursery":
                    created = CreateTileSprite(80, 56, DrawNurseryRug);
                    break;
                case "rug_runner":
                    created = CreateTileSprite(72, 36, DrawRunnerRug);
                    break;
                case "mat_bamboo":
                    created = CreateTileSprite(80, 56, DrawBambooMat);
                    break;
                case "plant_monstera":
                    created = CreateTileSprite(36, 52, DrawMonstera);
                    break;
                case "plant_fern":
                    created = CreateTileSprite(36, 52, DrawFern);
                    break;
                case "pan_rack":
                    created = CreateTileSprite(48, 20, DrawPanRack);
                    break;
                case "scroll_mountain":
                    created = CreateTileSprite(40, 56, DrawScrollPainting);
                    break;

                // New Dressing Room Sprites
                case "garment_rack":
                    created = CreateTileSprite(48, 48, DrawGarmentRack);
                    break;
                case "mirror_standing":
                    created = CreateTileSprite(32, 56, DrawStandingMirror);
                    break;
                case "dressing_bench":
                    created = CreateTileSprite(48, 28, DrawDressingBench);
                    break;
                case "shoe_rack":
                    created = CreateTileSprite(40, 28, DrawShoeRack);
                    break;

                case "house_exterior_facade":
                    created = CreateCustomSprite(192, 144, 24f, new Vector2(0.5f, 0.15f), DrawKhoaFarmhouseExterior);
                    break;

                default:
                    created = whiteSprite;
                    break;
            }

            if (created != null)
                spriteCache[key] = created;

            return created ?? whiteSprite;
        }

        private static Sprite CreateTileSprite(int w, int h, Action<Color32[], int, int> draw)
        {
            var pixels = new Color32[w * h];
            draw(pixels, w, h);
            var texture = new Texture2D(w, h, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Clamp
            };
            texture.SetPixels32(pixels);
            texture.Apply(false, true);
            return Sprite.Create(texture, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f), w);
        }

        private static Sprite CreateCustomSprite(int w, int h, float ppu, Vector2 pivot, Action<Color32[], int, int> draw)
        {
            var pixels = new Color32[w * h];
            draw(pixels, w, h);
            var texture = new Texture2D(w, h, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Clamp
            };
            texture.SetPixels32(pixels);
            texture.Apply(false, true);
            return Sprite.Create(texture, new Rect(0, 0, w, h), pivot, ppu);
        }

        private static void DrawKhoaFarmhouseExterior(Color32[] p, int w, int h)
        {
            // Clear to transparent
            for (int i = 0; i < p.Length; i++) p[i] = new Color32(0, 0, 0, 0);

            // 1. Soft Ground Shadow underneath building footprint
            PEllipse(p, w, h, 14, 2, 164, 18, new Color32(0, 0, 0, 75));

            // 2. Fieldstone Foundation (y: 10..22, x: 22..170)
            Color32 stoneBorder = new Color32(45, 40, 38, 255);
            Color32 stoneBase = new Color32(95, 90, 85, 255);
            Color32 stoneLight = new Color32(130, 125, 120, 255);
            Color32 mortar = new Color32(40, 36, 34, 255);
            PRect(p, w, h, 22, 10, 148, 12, stoneBorder);
            PRect(p, w, h, 23, 11, 146, 10, stoneBase);
            for (int sx = 24; sx < 168; sx += 12)
            {
                PRect(p, w, h, sx, 11, 1, 10, mortar);
                PRect(p, w, h, sx + 2, 17, 8, 2, stoneLight);
            }

            // 3. Main Timber Siding Wall (y: 20..76, x: 24..168)
            Color32 wallPlankMid = new Color32(195, 130, 75, 255);
            Color32 wallPlankLight = new Color32(220, 155, 95, 255);
            Color32 wallPlankSeam = new Color32(125, 75, 35, 255);
            PRect(p, w, h, 24, 20, 144, 56, wallPlankMid);
            for (int y = 20; y < 76; y += 7)
            {
                PRect(p, w, h, 24, y, 144, 1, wallPlankSeam);
                PRect(p, w, h, 24, y + 5, 144, 1, wallPlankLight);
            }

            // Vertical Timber Corner Trim
            Color32 cornerTrim = new Color32(110, 60, 28, 255);
            Color32 cornerTrimHi = new Color32(145, 85, 42, 255);
            PRect(p, w, h, 22, 20, 6, 56, cornerTrim);
            PRect(p, w, h, 24, 20, 2, 56, cornerTrimHi);
            PRect(p, w, h, 164, 20, 6, 56, cornerTrim);
            PRect(p, w, h, 166, 20, 2, 56, cornerTrimHi);

            // Eaves Shadow and Upper Frieze Beam
            PRect(p, w, h, 20, 73, 152, 4, new Color32(75, 40, 18, 255));
            PRect(p, w, h, 20, 72, 152, 2, new Color32(40, 20, 10, 180));

            // 4. Windows (Left: x: 38..66, Right: x: 76..104, y: 34..60)
            DrawExteriorWindow(p, w, h, 38, 34);
            DrawExteriorWindow(p, w, h, 76, 34);

            // 5. Front Porch & Door (x: 114..162)
            Color32 deckBorder = new Color32(65, 36, 18, 255);
            Color32 deckPlank = new Color32(150, 92, 48, 255);
            Color32 deckPlankHi = new Color32(180, 118, 68, 255);
            PRect(p, w, h, 114, 14, 48, 10, deckBorder);
            PRect(p, w, h, 115, 15, 46, 8, deckPlank);
            for (int dx = 120; dx < 160; dx += 8)
                PRect(p, w, h, dx, 15, 1, 8, deckBorder);
            PRect(p, w, h, 115, 22, 46, 1, deckPlankHi);

            // Porch Steps leading to lawn (x: 124..148, y: 6..14)
            PRect(p, w, h, 124, 6, 26, 9, deckBorder);
            PRect(p, w, h, 125, 7, 24, 3, deckPlank);
            PRect(p, w, h, 125, 10, 24, 1, deckBorder);
            PRect(p, w, h, 125, 11, 24, 3, deckPlankHi);

            // Welcome Doormat
            PRect(p, w, h, 126, 16, 22, 6, new Color32(195, 155, 105, 255));
            PFrame(p, w, h, 126, 16, 22, 6, new Color32(120, 80, 40, 255), new Color32(195, 155, 105, 255), 1);

            // Front Door (x: 125..149, y: 24..68)
            Color32 doorFrame = new Color32(70, 38, 18, 255);
            Color32 doorWood = new Color32(138, 76, 34, 255);
            Color32 doorPanelDark = new Color32(105, 54, 22, 255);
            Color32 doorPanelHi = new Color32(165, 96, 48, 255);
            PFrame(p, w, h, 125, 24, 24, 44, doorFrame, doorWood, 2);
            PFrame(p, w, h, 128, 48, 18, 17, doorPanelDark, doorWood, 1);
            PRect(p, w, h, 130, 63, 14, 1, doorPanelHi);
            PFrame(p, w, h, 128, 28, 18, 16, doorPanelDark, doorWood, 1);
            PRect(p, w, h, 130, 42, 14, 1, doorPanelHi);
            // Brass Doorknob & Keyhole
            PSet(p, w, h, 144, 45, new Color32(255, 220, 80, 255));
            PSet(p, w, h, 145, 45, new Color32(255, 235, 120, 255));
            PSet(p, w, h, 144, 44, new Color32(210, 160, 40, 255));
            PSet(p, w, h, 145, 44, new Color32(40, 20, 10, 255));

            // Porch Posts
            Color32 postWood = new Color32(110, 60, 28, 255);
            Color32 postHi = new Color32(145, 85, 42, 255);
            PRect(p, w, h, 115, 24, 4, 52, postWood);
            PRect(p, w, h, 116, 24, 1, 52, postHi);
            PRect(p, w, h, 157, 24, 4, 52, postWood);
            PRect(p, w, h, 158, 24, 1, 52, postHi);

            // Porch Railing & Spindles
            PRect(p, w, h, 115, 38, 11, 2, postWood);
            PRect(p, w, h, 149, 38, 11, 2, postWood);
            for (int rx = 117; rx <= 123; rx += 3)
                PRect(p, w, h, rx, 24, 1, 14, postWood);
            for (int rx = 151; rx <= 157; rx += 3)
                PRect(p, w, h, rx, 24, 1, 14, postWood);

            // Hanging Lantern next to door (x: 150..155, y: 53..61)
            PRect(p, w, h, 150, 60, 4, 1, new Color32(40, 35, 30, 255));
            PFrame(p, w, h, 150, 53, 5, 7, new Color32(40, 35, 30, 255), new Color32(255, 230, 110, 255), 1);
            PEllipse(p, w, h, 147, 51, 11, 11, new Color32(255, 240, 150, 80));

            // 6. Brick Chimney (x: 140..152, y: 92..138)
            Color32 brickDark = new Color32(130, 45, 32, 255);
            Color32 brickMid = new Color32(175, 68, 50, 255);
            Color32 brickLight = new Color32(205, 95, 75, 255);
            Color32 brickMortar = new Color32(215, 205, 195, 255);
            PRect(p, w, h, 141, 92, 12, 42, brickDark);
            PRect(p, w, h, 142, 92, 10, 41, brickMid);
            for (int cy = 94; cy < 133; cy += 4)
            {
                PRect(p, w, h, 141, cy, 12, 1, brickMortar);
                PRect(p, w, h, (cy % 8 == 0) ? 146 : 144, cy + 1, 1, 3, brickMortar);
                PRect(p, w, h, 143, cy + 2, 3, 1, brickLight);
            }
            PRect(p, w, h, 139, 133, 16, 4, new Color32(65, 70, 75, 255));
            PRect(p, w, h, 140, 135, 14, 1, new Color32(110, 115, 120, 255));
            PEllipse(p, w, h, 142, 137, 6, 4, new Color32(245, 245, 250, 140));
            PEllipse(p, w, h, 140, 140, 8, 4, new Color32(245, 245, 250, 90));

            // 7. Gable Cottage Roof (y: 75..132)
            Color32 roofShading = new Color32(115, 35, 20, 255);
            Color32 shingle1 = new Color32(180, 62, 36, 255);
            Color32 shingle2 = new Color32(205, 82, 48, 255);
            Color32 shingleHi = new Color32(230, 112, 68, 255);
            Color32 bargeWood = new Color32(95, 40, 18, 255);
            Color32 bargeHi = new Color32(140, 68, 32, 255);

            int roofBottom = 75;
            int roofTop = 130;
            int rowHeight = 7;
            int rows = (roofTop - roofBottom) / rowHeight;

            for (int r = 0; r <= rows; r++)
            {
                int ry = roofBottom + r * rowHeight;
                int indent = r * 5;
                int rxStart = 14 + indent;
                int rxEnd = 178 - indent;
                int rw = rxEnd - rxStart;
                if (rw <= 0) break;

                Color32 cBase = (r % 2 == 0) ? shingle1 : shingle2;
                PRect(p, w, h, rxStart, ry, rw, rowHeight, cBase);
                PRect(p, w, h, rxStart, ry, rw, 1, roofShading);
                PRect(p, w, h, rxStart + 1, ry + rowHeight - 2, rw - 2, 1, shingleHi);

                int tileStep = 10;
                int offset = (r % 2 == 0) ? 0 : 5;
                for (int tx = rxStart + offset; tx < rxEnd; tx += tileStep)
                {
                    PRect(p, w, h, tx, ry, 1, rowHeight - 1, roofShading);
                    PSet(p, w, h, tx + 1, ry + rowHeight - 2, shingleHi);
                }
            }

            // Ridge Capping
            PRect(p, w, h, 48, 129, 96, 4, bargeWood);
            PRect(p, w, h, 49, 131, 94, 2, new Color32(240, 130, 80, 255));

            // Eaves Bargeboards
            for (int r = 0; r <= rows; r++)
            {
                int ry = roofBottom + r * rowHeight;
                int indent = r * 5;
                PRect(p, w, h, 12 + indent, ry, 4, rowHeight + 1, bargeWood);
                PRect(p, w, h, 14 + indent, ry, 1, rowHeight + 1, bargeHi);
                PRect(p, w, h, 176 - indent, ry, 4, rowHeight + 1, bargeWood);
                PRect(p, w, h, 177 - indent, ry, 1, rowHeight + 1, bargeHi);
            }

            // 8. Attic Gable Window (x: 88..104, y: 96..112)
            PEllipse(p, w, h, 88, 96, 16, 16, bargeWood);
            PEllipse(p, w, h, 90, 98, 12, 12, new Color32(110, 180, 215, 255));
            PRect(p, w, h, 95, 98, 2, 12, bargeWood);
            PRect(p, w, h, 90, 103, 12, 2, bargeWood);
            PSet(p, w, h, 92, 106, new Color32(230, 245, 255, 255));
        }

        private static void DrawExteriorWindow(Color32[] p, int w, int h, int x, int y)
        {
            Color32 frameWood = new Color32(75, 40, 18, 255);
            Color32 frameHi = new Color32(120, 68, 32, 255);
            Color32 casing = new Color32(245, 240, 225, 255);
            Color32 glass = new Color32(110, 180, 215, 255);
            Color32 glare = new Color32(205, 240, 255, 255);

            PFrame(p, w, h, x, y, 28, 26, frameWood, casing, 2);
            PRect(p, w, h, x + 1, y + 25, 26, 1, frameHi);

            PRect(p, w, h, x + 3, y + 3, 10, 9, glass);
            PRect(p, w, h, x + 15, y + 3, 10, 9, glass);
            PRect(p, w, h, x + 3, y + 14, 10, 9, glass);
            PRect(p, w, h, x + 15, y + 14, 10, 9, glass);

            PRect(p, w, h, x + 5, y + 18, 5, 2, glare);
            PRect(p, w, h, x + 17, y + 18, 4, 2, glare);
            PRect(p, w, h, x + 5, y + 7, 4, 2, glare);

            Color32 planterWood = new Color32(95, 50, 22, 255);
            Color32 soil = new Color32(50, 32, 18, 255);
            PRect(p, w, h, x - 2, y - 8, 32, 8, planterWood);
            PRect(p, w, h, x - 1, y - 2, 30, 2, soil);
            PRect(p, w, h, x - 2, y - 1, 32, 1, frameHi);

            Color32 leafDark = new Color32(40, 115, 45, 255);
            Color32 leafLight = new Color32(75, 175, 60, 255);
            Color32 pink = new Color32(255, 105, 160, 255);
            Color32 yellow = new Color32(255, 215, 55, 255);
            Color32 white = new Color32(255, 255, 240, 255);

            for (int fx = x; fx < x + 28; fx += 5)
            {
                PEllipse(p, w, h, fx - 1, y - 3, 4, 5, leafDark);
                PSet(p, w, h, fx + 1, y - 1, leafLight);
                Color32 flowerCol = (fx % 10 == 0) ? pink : ((fx % 15 == 0) ? white : yellow);
                PEllipse(p, w, h, fx, y - 2, 3, 3, flowerCol);
                PSet(p, w, h, fx + 1, y - 1, new Color32(255, 255, 255, 255));
            }
        }
        #endregion

        #region Pixel Drawing Algorithms
        private static void PSet(Color32[] p, int w, int h, int x, int y, Color32 c)
        {
            if (x >= 0 && x < w && y >= 0 && y < h)
                p[y * w + x] = c;
        }

        private static void PRect(Color32[] p, int w, int h, int x, int y, int rw, int rh, Color32 c)
        {
            int xEnd = Mathf.Min(w, x + rw);
            int yEnd = Mathf.Min(h, y + rh);
            for (int cy = Mathf.Max(0, y); cy < yEnd; cy++)
            for (int cx = Mathf.Max(0, x); cx < xEnd; cx++)
                p[cy * w + cx] = c;
        }

        private static void PFrame(Color32[] p, int w, int h, int x, int y, int rw, int rh, Color32 border, Color32 fill, int bSize = 1)
        {
            PRect(p, w, h, x, y, rw, rh, border);
            PRect(p, w, h, x + bSize, y + bSize, rw - bSize * 2, rh - bSize * 2, fill);
        }

        private static void PEllipse(Color32[] p, int w, int h, int x, int y, int rw, int rh, Color32 c)
        {
            float rx = rw * 0.5f;
            float ry = rh * 0.5f;
            float cx = x + rx;
            float cy = y + ry;
            int xEnd = Mathf.Min(w, x + rw);
            int yEnd = Mathf.Min(h, y + rh);
            for (int cyI = Mathf.Max(0, y); cyI < yEnd; cyI++)
            for (int cxI = Mathf.Max(0, x); cxI < xEnd; cxI++)
            {
                float dx = (cxI + 0.5f - cx) / rx;
                float dy = (cyI + 0.5f - cy) / ry;
                if (dx * dx + dy * dy <= 1f)
                    p[cyI * w + cxI] = c;
            }
        }

        // Floors
        private static void DrawWoodFloor(Color32[] p, int w, int h, Color32 plankCol, Color32 seamCol)
        {
            PRect(p, w, h, 0, 0, w, h, plankCol);
            int plankHeight = h / 4;
            for (int i = 0; i < 4; i++)
            {
                int y = i * plankHeight;
                PRect(p, w, h, 0, y, w, 1, seamCol);
                // Nail heads & grain highlights
                Color32 nail = seamCol;
                PSet(p, w, h, 4, y + 2, nail);
                PSet(p, w, h, w - 6, y + 2, nail);
                PRect(p, w, h, 8, y + plankHeight / 2, w - 16, 1, new Color32((byte)Mathf.Min(255, plankCol.r + 14), (byte)Mathf.Min(255, plankCol.g + 14), (byte)Mathf.Min(255, plankCol.b + 14), 255));
            }
        }

        private static void DrawParquetFloor(Color32[] p, int w, int h)
        {
            Color32 dark = new Color32(138, 88, 48, 255);
            Color32 light = new Color32(178, 124, 76, 255);
            Color32 seam = new Color32(95, 55, 25, 255);
            PRect(p, w, h, 0, 0, w, h, light);
            int step = w / 4;
            for (int y = 0; y < 4; y++)
            for (int x = 0; x < 4; x++)
            {
                bool alt = (x + y) % 2 == 0;
                PRect(p, w, h, x * step, y * step, step, step, alt ? light : dark);
                PFrame(p, w, h, x * step, y * step, step, step, seam, alt ? light : dark, 1);
            }
        }

        private static void DrawKitchenTileFloor(Color32[] p, int w, int h)
        {
            Color32 grout = new Color32(148, 136, 124, 255);
            Color32 cream = new Color32(236, 222, 198, 255);
            Color32 terracotta = new Color32(195, 114, 78, 255);
            PRect(p, w, h, 0, 0, w, h, grout);
            int tileSize = w / 4;
            for (int y = 0; y < 4; y++)
            for (int x = 0; x < 4; x++)
            {
                Color32 tile = (x + y) % 2 == 0 ? cream : terracotta;
                PRect(p, w, h, x * tileSize + 1, y * tileSize + 1, tileSize - 2, tileSize - 2, tile);
            }
        }

        private static void DrawBathTileFloor(Color32[] p, int w, int h)
        {
            Color32 grout = new Color32(235, 245, 242, 255);
            Color32 tealLight = new Color32(172, 214, 206, 255);
            Color32 tealDark = new Color32(144, 192, 184, 255);
            PRect(p, w, h, 0, 0, w, h, grout);
            int tileSize = w / 4;
            for (int y = 0; y < 4; y++)
            for (int x = 0; x < 4; x++)
            {
                Color32 tile = (x + y) % 2 == 0 ? tealLight : tealDark;
                PRect(p, w, h, x * tileSize + 1, y * tileSize + 1, tileSize - 2, tileSize - 2, tile);
            }
        }

        private static void DrawCellarStoneFloor(Color32[] p, int w, int h)
        {
            Color32 mortar = new Color32(52, 48, 45, 255);
            Color32 slate1 = new Color32(96, 92, 88, 255);
            Color32 slate2 = new Color32(115, 110, 105, 255);
            Color32 moss = new Color32(78, 98, 68, 255);
            PRect(p, w, h, 0, 0, w, h, mortar);
            int s = w / 4;
            for (int y = 0; y < 4; y++)
            for (int x = 0; x < 4; x++)
            {
                Color32 st = (x * 3 + y * 7) % 2 == 0 ? slate1 : slate2;
                PRect(p, w, h, x * s + 1, y * s + 1, s - 2, s - 2, st);
                if ((x + y) % 3 == 0)
                    PSet(p, w, h, x * s + 2, y * s + 2, moss);
            }
        }

        // Walls
        private static void DrawNorthWall(Color32[] p, int w, int h, Color32 wallpaper, Color32 beadboard)
        {
            // Crown molding at top (y: 58-63)
            PRect(p, w, h, 0, 58, w, 6, new Color32(58, 30, 16, 255));
            PRect(p, w, h, 0, 60, w, 2, new Color32(95, 52, 28, 255));

            // Upper Wallpaper (y: 24-57)
            PRect(p, w, h, 0, 24, w, 34, wallpaper);
            for (int x = 4; x < w; x += 8)
                PRect(p, w, h, x, 24, 1, 34, new Color32((byte)Mathf.Max(0, wallpaper.r - 20), (byte)Mathf.Max(0, wallpaper.g - 20), (byte)Mathf.Max(0, wallpaper.b - 20), 255));

            // Chair rail (y: 21-23)
            PRect(p, w, h, 0, 21, w, 3, new Color32(110, 64, 34, 255));

            // Lower Beadboard Paneling (y: 6-20)
            PRect(p, w, h, 0, 6, w, 15, beadboard);
            for (int x = 4; x < w; x += 6)
                PRect(p, w, h, x, 6, 1, 15, new Color32((byte)Mathf.Max(0, beadboard.r - 30), (byte)Mathf.Max(0, beadboard.g - 30), (byte)Mathf.Max(0, beadboard.b - 30), 255));

            // Baseboard (y: 0-5)
            PRect(p, w, h, 0, 0, w, 6, new Color32(72, 38, 20, 255));
            PRect(p, w, h, 0, 4, w, 2, new Color32(105, 58, 30, 255));
        }

        private static void DrawStoneWall(Color32[] p, int w, int h)
        {
            Color32 mortar = new Color32(40, 36, 32, 255);
            Color32 stone = new Color32(82, 78, 74, 255);
            Color32 highlight = new Color32(110, 105, 100, 255);
            PRect(p, w, h, 0, 0, w, h, mortar);
            int rows = 6;
            int rHeight = h / rows;
            for (int r = 0; r < rows; r++)
            {
                int y = r * rHeight;
                int offset = (r % 2) * (w / 4);
                for (int b = -1; b < 5; b++)
                {
                    int bx = b * (w / 2) + offset;
                    PRect(p, w, h, bx + 1, y + 1, (w / 2) - 2, rHeight - 2, stone);
                    PRect(p, w, h, bx + 2, y + rHeight - 3, (w / 2) - 4, 1, highlight);
                }
            }
        }

        // Windows & Light
        private static void DrawWindowArched(Color32[] p, int w, int h)
        {
            Color32 wood = new Color32(85, 48, 24, 255);
            Color32 glass = new Color32(142, 205, 235, 255);
            Color32 curtain = new Color32(165, 42, 52, 255);
            Color32 gold = new Color32(235, 185, 65, 255);

            // Window frame & sky
            PFrame(p, w, h, 10, 8, 44, 48, wood, glass, 3);
            // Window crossbars
            PRect(p, w, h, 31, 8, 2, 48, wood);
            PRect(p, w, h, 10, 30, 44, 2, wood);

            // Curtains left and right
            PRect(p, w, h, 4, 10, 10, 48, curtain);
            PRect(p, w, h, 50, 10, 10, 48, curtain);
            // Curtain tiebacks
            PRect(p, w, h, 6, 26, 8, 3, gold);
            PRect(p, w, h, 50, 26, 8, 3, gold);

            // Window sill
            PRect(p, w, h, 8, 6, 48, 4, wood);
        }

        private static void DrawSunbeam(Color32[] p, int w, int h)
        {
            Color32 beamCol = new Color32(255, 238, 160, 48);
            for (int y = 0; y < h; y++)
            {
                float t = (float)y / h;
                int startX = Mathf.RoundToInt(t * (w * 0.4f));
                int width = Mathf.RoundToInt(w * 0.55f + t * (w * 0.35f));
                for (int x = startX; x < Mathf.Min(w, startX + width); x++)
                    p[y * w + x] = beamCol;
            }
        }

        // Furniture
        private static void DrawMasterBed(Color32[] p, int w, int h)
        {
            Color32 wood = new Color32(75, 40, 20, 255);
            Color32 headboard = new Color32(105, 58, 28, 255);
            Color32 sheets = new Color32(245, 242, 235, 255);
            Color32 duvet = new Color32(148, 36, 48, 255);
            Color32 goldTrim = new Color32(225, 178, 70, 255);
            Color32 shadow = new Color32(40, 22, 14, 255);

            // Bedstead shadow
            PEllipse(p, w, h, 6, 4, 68, 16, new Color32(25, 15, 10, 140));

            // Headboard
            PFrame(p, w, h, 8, 46, 64, 30, shadow, headboard, 3);
            PRect(p, w, h, 14, 72, 6, 6, goldTrim);
            PRect(p, w, h, 60, 72, 6, 6, goldTrim);

            // Pillows
            PFrame(p, w, h, 14, 50, 22, 14, shadow, sheets, 2);
            PFrame(p, w, h, 44, 50, 22, 14, shadow, sheets, 2);

            // Mattress & Duvet
            PFrame(p, w, h, 10, 10, 60, 42, shadow, duvet, 3);
            PRect(p, w, h, 12, 46, 56, 6, sheets); // Folded white sheet

            // Quilt embroidery
            for (int x = 20; x <= 60; x += 14)
                PRect(p, w, h, x, 24, 2, 16, goldTrim);

            // Footboard
            PFrame(p, w, h, 8, 8, 64, 10, shadow, wood, 2);
        }

        private static void DrawSingleBed(Color32[] p, int w, int h)
        {
            Color32 wood = new Color32(85, 48, 24, 255);
            Color32 sheets = new Color32(245, 242, 235, 255);
            Color32 duvet = new Color32(52, 92, 138, 255);

            PFrame(p, w, h, 10, 42, 44, 20, wood, new Color32(115, 68, 34, 255), 2);
            PFrame(p, w, h, 18, 44, 28, 12, wood, sheets, 2);
            PFrame(p, w, h, 12, 8, 40, 38, wood, duvet, 2);
            PRect(p, w, h, 14, 40, 36, 4, sheets);
            PFrame(p, w, h, 10, 6, 44, 8, wood, wood, 1);
        }

        private static void DrawChildBed(Color32[] p, int w, int h)
        {
            Color32 wood = new Color32(145, 95, 55, 255);
            Color32 sheets = new Color32(248, 245, 238, 255);
            Color32 duvet = new Color32(225, 175, 85, 255);

            PFrame(p, w, h, 10, 42, 44, 20, wood, new Color32(175, 125, 75, 255), 2);
            PFrame(p, w, h, 18, 44, 28, 12, wood, sheets, 2);
            PFrame(p, w, h, 12, 8, 40, 38, wood, duvet, 2);
            PRect(p, w, h, 14, 40, 36, 4, sheets);
        }

        private static void DrawCrib(Color32[] p, int w, int h)
        {
            Color32 wood = new Color32(178, 134, 92, 255);
            Color32 mattress = new Color32(245, 235, 195, 255);
            Color32 rail = new Color32(118, 78, 42, 255);

            PFrame(p, w, h, 8, 8, 48, 48, rail, wood, 2);
            PRect(p, w, h, 12, 12, 40, 24, mattress);
            // Rails
            for (int x = 14; x < 52; x += 6)
                PRect(p, w, h, x, 24, 2, 28, rail);
        }

        private static void DrawFireplace(Color32[] p, int w, int h)
        {
            Color32 brick = new Color32(148, 62, 45, 255);
            Color32 mortar = new Color32(75, 38, 30, 255);
            Color32 mantle = new Color32(105, 58, 28, 255);
            Color32 hearth = new Color32(25, 22, 20, 255);
            Color32 fire = new Color32(255, 145, 30, 255);
            Color32 gold = new Color32(255, 215, 80, 255);

            // Brick structure
            PFrame(p, w, h, 6, 6, 52, 54, mortar, brick, 3);
            // Wooden Mantel shelf
            PRect(p, w, h, 2, 54, 60, 8, mantle);
            PRect(p, w, h, 4, 58, 56, 2, new Color32(145, 88, 45, 255));

            // Hearth firebox
            PRect(p, w, h, 16, 10, 32, 34, hearth);
            // Crackling fire
            PEllipse(p, w, h, 20, 12, 24, 18, fire);
            PEllipse(p, w, h, 24, 14, 16, 12, gold);

            // Fireplace clock on mantel
            PRect(p, w, h, 28, 62, 8, 8, mantle);
            PSet(p, w, h, 31, 65, gold);
        }

        private static void DrawSofa(Color32[] p, int w, int h, Color32 baseCol, Color32 lightCol)
        {
            Color32 wood = new Color32(75, 40, 20, 255);
            // Backrest
            PFrame(p, w, h, 4, 16, w - 8, h - 20, wood, baseCol, 3);
            // Seat Cushion
            PFrame(p, w, h, 8, 8, w - 16, 24, wood, lightCol, 2);
            // Rolled Arms
            PFrame(p, w, h, 2, 8, 12, 28, wood, baseCol, 2);
            PFrame(p, w, h, w - 14, 8, 12, 28, wood, baseCol, 2);
            // Wooden bun feet
            PRect(p, w, h, 6, 2, 6, 6, wood);
            PRect(p, w, h, w - 12, 2, 6, 6, wood);
        }

        private static void DrawArmchair(Color32[] p, int w, int h, Color32 baseCol, Color32 lightCol)
        {
            Color32 wood = new Color32(75, 40, 20, 255);
            PFrame(p, w, h, 6, 14, 36, 30, wood, baseCol, 2);
            PFrame(p, w, h, 8, 6, 32, 20, wood, lightCol, 2);
            PFrame(p, w, h, 4, 8, 8, 22, wood, baseCol, 2);
            PFrame(p, w, h, 36, 8, 8, 22, wood, baseCol, 2);
        }

        private static void DrawCoffeeTable(Color32[] p, int w, int h)
        {
            Color32 wood = new Color32(110, 64, 32, 255);
            Color32 highlight = new Color32(165, 108, 62, 255);
            Color32 shadow = new Color32(65, 34, 16, 255);

            // Table legs
            PRect(p, w, h, 8, 2, 4, 16, shadow);
            PRect(p, w, h, 52, 2, 4, 16, shadow);
            // Table top
            PFrame(p, w, h, 4, 14, 56, 16, shadow, wood, 2);
            PRect(p, w, h, 6, 26, 52, 2, highlight);

            // Teacup on table
            PRect(p, w, h, 28, 20, 5, 4, new Color32(245, 242, 235, 255));
        }

        private static void DrawDiningTable(Color32[] p, int w, int h)
        {
            Color32 wood = new Color32(115, 68, 34, 255);
            Color32 runner = new Color32(245, 240, 230, 255);
            Color32 fruit = new Color32(215, 45, 45, 255);
            Color32 shadow = new Color32(65, 34, 16, 255);

            // Legs
            PRect(p, w, h, 10, 2, 6, 26, shadow);
            PRect(p, w, h, 80, 2, 6, 26, shadow);

            // Tabletop
            PFrame(p, w, h, 6, 22, 84, 22, shadow, wood, 2);
            // White lace runner down the center
            PRect(p, w, h, 32, 22, 32, 22, runner);

            // Fruit bowl center
            PEllipse(p, w, h, 42, 30, 12, 8, new Color32(220, 215, 205, 255));
            PSet(p, w, h, 46, 33, fruit);
            PSet(p, w, h, 49, 34, new Color32(85, 165, 55, 255));
        }

        private static void DrawDiningChairN(Color32[] p, int w, int h)
        {
            Color32 wood = new Color32(115, 68, 34, 255);
            // Backrest viewed from south
            PFrame(p, w, h, 6, 12, 20, 18, wood, new Color32(165, 108, 62, 255), 2);
            // Slats
            PRect(p, w, h, 11, 14, 2, 14, wood);
            PRect(p, w, h, 19, 14, 2, 14, wood);
            // Seat
            PRect(p, w, h, 4, 8, 24, 6, wood);
            // Legs
            PRect(p, w, h, 6, 0, 3, 8, wood);
            PRect(p, w, h, 23, 0, 3, 8, wood);
        }

        private static void DrawDiningChairS(Color32[] p, int w, int h)
        {
            Color32 wood = new Color32(115, 68, 34, 255);
            PRect(p, w, h, 4, 12, 24, 14, new Color32(165, 108, 62, 255));
            PRect(p, w, h, 6, 0, 3, 14, wood);
            PRect(p, w, h, 23, 0, 3, 14, wood);
        }

        private static void DrawCounterSink(Color32[] p, int w, int h)
        {
            Color32 baseCol = new Color32(95, 55, 28, 255);
            Color32 counter = new Color32(235, 230, 220, 255);
            Color32 basin = new Color32(185, 195, 200, 255);
            Color32 faucet = new Color32(235, 185, 65, 255);

            PFrame(p, w, h, 4, 4, 72, 38, new Color32(50, 25, 12, 255), baseCol, 2);
            // Countertop
            PRect(p, w, h, 2, 36, 76, 8, counter);
            // Sink basin
            PRect(p, w, h, 24, 34, 32, 10, basin);
            // Faucet
            PRect(p, w, h, 38, 42, 4, 6, faucet);
        }

        private static void DrawCounterDrawers(Color32[] p, int w, int h)
        {
            Color32 baseCol = new Color32(95, 55, 28, 255);
            Color32 counter = new Color32(235, 230, 220, 255);
            Color32 knob = new Color32(235, 185, 65, 255);

            PFrame(p, w, h, 4, 4, 56, 38, new Color32(50, 25, 12, 255), baseCol, 2);
            PRect(p, w, h, 2, 36, 60, 8, counter);

            // Drawers & knobs
            for (int y = 8; y < 32; y += 10)
            {
                PFrame(p, w, h, 8, y, 48, 8, new Color32(65, 35, 16, 255), baseCol, 1);
                PSet(p, w, h, 31, y + 3, knob);
            }
        }

        private static void DrawCounterSide(Color32[] p, int w, int h)
        {
            Color32 baseCol = new Color32(95, 55, 28, 255);
            Color32 counter = new Color32(235, 230, 220, 255);
            PFrame(p, w, h, 4, 4, 24, 72, new Color32(50, 25, 12, 255), baseCol, 2);
            PRect(p, w, h, 2, 4, 6, 72, counter);
        }

        private static void DrawStoveIron(Color32[] p, int w, int h)
        {
            Color32 iron = new Color32(48, 48, 50, 255);
            Color32 burner = new Color32(25, 25, 25, 255);
            Color32 copper = new Color32(195, 95, 45, 255);

            PFrame(p, w, h, 4, 4, 40, 40, new Color32(20, 20, 20, 255), iron, 2);
            // 4 burners on cooktop
            PEllipse(p, w, h, 8, 30, 12, 8, burner);
            PEllipse(p, w, h, 26, 30, 12, 8, burner);
            // Whistling copper kettle
            PEllipse(p, w, h, 10, 32, 8, 8, copper);

            // Oven glass door
            PFrame(p, w, h, 8, 8, 32, 18, iron, new Color32(25, 25, 25, 255), 2);
            PRect(p, w, h, 12, 12, 24, 10, new Color32(245, 175, 45, 120)); // Warm bake glow
        }

        private static void DrawRetroFridge(Color32[] p, int w, int h)
        {
            Color32 mint = new Color32(148, 192, 172, 255);
            Color32 chrome = new Color32(225, 230, 235, 255);
            Color32 shadow = new Color32(95, 135, 120, 255);

            PFrame(p, w, h, 4, 4, 40, 64, shadow, mint, 2);
            PRect(p, w, h, 6, 42, 36, 2, shadow); // Split freezer & fridge

            // Chrome handles
            PRect(p, w, h, 34, 48, 3, 10, chrome);
            PRect(p, w, h, 34, 22, 3, 16, chrome);

            // Cute colorful magnets
            PSet(p, w, h, 14, 52, new Color32(225, 65, 65, 255));
            PSet(p, w, h, 20, 50, new Color32(235, 215, 65, 255));
        }

        private static void DrawBookcase(Color32[] p, int w, int h)
        {
            Color32 wood = new Color32(95, 52, 25, 255);
            Color32 shelf = new Color32(135, 78, 38, 255);

            PFrame(p, w, h, 4, 4, 56, 56, new Color32(55, 30, 15, 255), wood, 2);
            Color32[] bookColors = {
                new Color32(185, 45, 45, 255), new Color32(45, 115, 175, 255),
                new Color32(45, 145, 65, 255), new Color32(215, 165, 55, 255),
                new Color32(135, 65, 155, 255)
            };

            for (int s = 0; s < 3; s++)
            {
                int y = 8 + s * 16;
                PRect(p, w, h, 6, y, 52, 3, shelf);
                // Books
                for (int b = 8; b < 52; b += 5)
                {
                    Color32 col = bookColors[(b * 7 + s) % bookColors.Length];
                    PRect(p, w, h, b, y + 3, 4, 11, col);
                }
            }
        }

        private static void DrawSmallBookcase(Color32[] p, int w, int h)
        {
            Color32 wood = new Color32(135, 88, 48, 255);
            PFrame(p, w, h, 4, 4, 40, 40, new Color32(75, 45, 22, 255), wood, 2);
            PRect(p, w, h, 6, 20, 36, 2, wood);
            for (int b = 8; b < 38; b += 5)
                PRect(p, w, h, b, 22, 4, 12, new Color32(215, 145, 75, 255));
        }

        private static void DrawWardrobe(Color32[] p, int w, int h)
        {
            Color32 wood = new Color32(95, 52, 25, 255);
            Color32 panel = new Color32(125, 72, 35, 255);
            Color32 brass = new Color32(235, 185, 65, 255);

            PFrame(p, w, h, 4, 4, 48, 64, new Color32(50, 25, 12, 255), wood, 2);
            // Double doors
            PFrame(p, w, h, 8, 16, 18, 46, wood, panel, 2);
            PFrame(p, w, h, 29, 16, 18, 46, wood, panel, 2);

            // Brass pulls
            PSet(p, w, h, 23, 38, brass);
            PSet(p, w, h, 31, 38, brass);
            // Lower drawer
            PFrame(p, w, h, 8, 8, 39, 6, wood, panel, 1);
        }

        private static void DrawVanityDresser(Color32[] p, int w, int h)
        {
            Color32 wood = new Color32(105, 58, 28, 255);
            Color32 mirror = new Color32(185, 225, 245, 255);
            Color32 gold = new Color32(235, 185, 65, 255);

            // Dresser base
            PFrame(p, w, h, 6, 4, 44, 28, new Color32(55, 30, 15, 255), wood, 2);
            // Oval Mirror
            PEllipse(p, w, h, 14, 32, 28, 28, gold);
            PEllipse(p, w, h, 16, 34, 24, 24, mirror);
        }

        private static void DrawNightstand(Color32[] p, int w, int h)
        {
            Color32 wood = new Color32(105, 58, 28, 255);
            PFrame(p, w, h, 4, 4, 24, 28, new Color32(55, 30, 15, 255), wood, 2);
            PFrame(p, w, h, 6, 16, 20, 10, wood, new Color32(135, 78, 38, 255), 1);
            PSet(p, w, h, 15, 21, new Color32(235, 185, 65, 255)); // Knob
        }

        private static void DrawWritingDesk(Color32[] p, int w, int h)
        {
            Color32 wood = new Color32(105, 58, 28, 255);
            PRect(p, w, h, 6, 2, 4, 26, wood);
            PRect(p, w, h, 38, 2, 4, 26, wood);
            PFrame(p, w, h, 4, 24, 40, 18, new Color32(55, 30, 15, 255), wood, 2);
            // Quill & paper
            PRect(p, w, h, 18, 28, 12, 10, new Color32(245, 240, 230, 255));
            PRect(p, w, h, 32, 30, 2, 8, new Color32(225, 220, 210, 255));
        }

        private static void DrawGrandfatherClock(Color32[] p, int w, int h)
        {
            Color32 wood = new Color32(85, 45, 22, 255);
            Color32 gold = new Color32(235, 185, 65, 255);

            PFrame(p, w, h, 6, 4, 20, 64, new Color32(45, 22, 12, 255), wood, 2);
            // Clock face
            PEllipse(p, w, h, 8, 48, 16, 16, new Color32(245, 242, 235, 255));
            PSet(p, w, h, 15, 55, new Color32(35, 35, 35, 255));
            // Pendulum window
            PRect(p, w, h, 10, 16, 12, 24, new Color32(35, 20, 12, 255));
            PEllipse(p, w, h, 13, 20, 6, 8, gold);
        }

        private static void DrawFloorLamp(Color32[] p, int w, int h)
        {
            Color32 brass = new Color32(215, 175, 65, 255);
            Color32 shade = new Color32(248, 238, 210, 255);

            PRect(p, w, h, 6, 2, 12, 4, brass);
            PRect(p, w, h, 11, 6, 2, 44, brass);
            // Shade
            PRect(p, w, h, 4, 48, 16, 12, shade);
        }

        private static void DrawTableLamp(Color32[] p, int w, int h)
        {
            Color32 brass = new Color32(215, 175, 65, 255);
            Color32 shade = new Color32(248, 238, 210, 255);

            PRect(p, w, h, 8, 2, 8, 4, brass);
            PRect(p, w, h, 11, 6, 2, 12, brass);
            PRect(p, w, h, 4, 16, 16, 12, shade);
        }

        private static void DrawClawfootTub(Color32[] p, int w, int h)
        {
            Color32 porcelain = new Color32(248, 250, 252, 255);
            Color32 water = new Color32(135, 205, 225, 255);
            Color32 gold = new Color32(225, 175, 65, 255);
            Color32 shadow = new Color32(185, 195, 205, 255);

            // Feet
            PRect(p, w, h, 10, 2, 6, 8, gold);
            PRect(p, w, h, 64, 2, 6, 8, gold);

            // Tub body
            PFrame(p, w, h, 6, 8, 68, 40, shadow, porcelain, 3);
            // Inside water
            PRect(p, w, h, 14, 16, 52, 26, water);
            // Faucet
            PRect(p, w, h, 66, 40, 4, 10, gold);
        }

        private static void DrawPedestalSink(Color32[] p, int w, int h)
        {
            Color32 porcelain = new Color32(248, 250, 252, 255);
            Color32 mirror = new Color32(185, 225, 245, 255);
            Color32 gold = new Color32(225, 175, 65, 255);

            // Pedestal & basin
            PRect(p, w, h, 16, 2, 8, 20, porcelain);
            PFrame(p, w, h, 8, 18, 24, 12, new Color32(185, 195, 205, 255), porcelain, 2);

            // Mirror
            PEllipse(p, w, h, 8, 32, 24, 22, gold);
            PEllipse(p, w, h, 10, 34, 20, 18, mirror);
        }

        private static void DrawToilet(Color32[] p, int w, int h)
        {
            Color32 porcelain = new Color32(248, 250, 252, 255);
            Color32 wood = new Color32(135, 78, 38, 255);

            PRect(p, w, h, 10, 4, 12, 18, porcelain);
            PEllipse(p, w, h, 6, 12, 20, 18, wood);
            PFrame(p, w, h, 6, 26, 20, 18, new Color32(195, 205, 215, 255), porcelain, 2);
        }

        private static void DrawBathMat(Color32[] p, int w, int h)
        {
            Color32 mat = new Color32(145, 195, 185, 255);
            PFrame(p, w, h, 4, 4, 40, 24, new Color32(115, 165, 155, 255), mat, 2);
        }

        private static void DrawTeaTable(Color32[] p, int w, int h)
        {
            Color32 wood = new Color32(75, 40, 20, 255);
            Color32 tea = new Color32(85, 145, 65, 255);

            PFrame(p, w, h, 6, 6, 52, 20, new Color32(45, 22, 12, 255), wood, 2);
            // Teapot & bowl
            PEllipse(p, w, h, 28, 12, 8, 8, new Color32(45, 45, 45, 255));
            PSet(p, w, h, 22, 14, tea);
            PSet(p, w, h, 38, 14, tea);
        }

        private static void DrawFloorCushion(Color32[] p, int w, int h)
        {
            Color32 gold = new Color32(225, 175, 55, 255);
            PEllipse(p, w, h, 4, 4, 24, 20, gold);
            PSet(p, w, h, 15, 13, new Color32(175, 125, 35, 255));
        }

        private static void DrawBonsai(Color32[] p, int w, int h)
        {
            Color32 pot = new Color32(65, 75, 85, 255);
            Color32 trunk = new Color32(95, 55, 28, 255);
            Color32 pine = new Color32(45, 105, 55, 255);

            // Ceramic dish
            PRect(p, w, h, 8, 4, 24, 8, pot);
            // Gnarled trunk
            PRect(p, w, h, 18, 12, 4, 16, trunk);
            PRect(p, w, h, 14, 22, 8, 4, trunk);
            // Pine foliage
            PEllipse(p, w, h, 6, 24, 16, 12, pine);
            PEllipse(p, w, h, 18, 28, 18, 14, pine);
        }

        private static void DrawVaseSakura(Color32[] p, int w, int h)
        {
            Color32 vase = new Color32(245, 245, 250, 255);
            Color32 branch = new Color32(85, 48, 24, 255);
            Color32 pink = new Color32(245, 165, 195, 255);

            PEllipse(p, w, h, 10, 4, 16, 20, vase);
            PRect(p, w, h, 16, 22, 2, 18, branch);
            PEllipse(p, w, h, 12, 34, 10, 8, pink);
            PEllipse(p, w, h, 18, 38, 12, 8, pink);
        }

        private static void DrawToyChest(Color32[] p, int w, int h)
        {
            Color32 wood = new Color32(175, 115, 65, 255);
            Color32 iron = new Color32(65, 45, 35, 255);
            PFrame(p, w, h, 4, 4, 40, 28, iron, wood, 2);
            PRect(p, w, h, 4, 26, 40, 6, iron);
            PSet(p, w, h, 23, 18, new Color32(235, 185, 65, 255));
        }

        private static void DrawTeddyBear(Color32[] p, int w, int h)
        {
            Color32 fur = new Color32(165, 105, 55, 255);
            Color32 dark = new Color32(55, 30, 15, 255);

            PEllipse(p, w, h, 6, 4, 24, 22, fur); // Body
            PEllipse(p, w, h, 8, 22, 20, 18, fur); // Head
            PEllipse(p, w, h, 6, 34, 6, 6, fur); // Left ear
            PEllipse(p, w, h, 24, 34, 6, 6, fur); // Right ear
            PSet(p, w, h, 14, 28, dark); // Eye
            PSet(p, w, h, 21, 28, dark); // Eye
            PSet(p, w, h, 17, 24, dark); // Nose
        }

        private static void DrawSewingTable(Color32[] p, int w, int h)
        {
            Color32 wood = new Color32(115, 68, 35, 255);
            Color32 castIron = new Color32(35, 35, 38, 255);

            PFrame(p, w, h, 4, 4, 48, 22, new Color32(65, 35, 16, 255), wood, 2);
            // Sewing machine body
            PRect(p, w, h, 18, 24, 20, 4, castIron);
            PRect(p, w, h, 32, 24, 4, 12, castIron);
            PRect(p, w, h, 20, 32, 16, 4, castIron);
        }

        private static void DrawRetroTV(Color32[] p, int w, int h)
        {
            Color32 wood = new Color32(85, 48, 25, 255);
            Color32 screen = new Color32(35, 45, 52, 255);

            PFrame(p, w, h, 4, 6, 48, 36, new Color32(45, 22, 12, 255), wood, 3);
            PFrame(p, w, h, 8, 10, 32, 28, new Color32(25, 25, 25, 255), screen, 2);
            PRect(p, w, h, 42, 26, 6, 6, new Color32(215, 175, 65, 255)); // Dials
            PRect(p, w, h, 42, 16, 6, 6, new Color32(215, 175, 65, 255));
        }

        private static void DrawWineCask(Color32[] p, int w, int h)
        {
            Color32 wood = new Color32(135, 78, 38, 255);
            Color32 hoop = new Color32(55, 55, 60, 255);
            Color32 cradle = new Color32(85, 48, 25, 255);

            // Cradle legs
            PRect(p, w, h, 8, 4, 6, 12, cradle);
            PRect(p, w, h, 42, 4, 6, 12, cradle);

            // Barrel body
            PEllipse(p, w, h, 6, 10, 44, 32, wood);
            // Steel hoops
            PRect(p, w, h, 14, 14, 3, 24, hoop);
            PRect(p, w, h, 38, 14, 3, 24, hoop);
            // Bung tap
            PRect(p, w, h, 26, 22, 4, 4, new Color32(225, 175, 65, 255));
        }

        private static void DrawWineCrates(Color32[] p, int w, int h)
        {
            Color32 wood = new Color32(155, 105, 55, 255);
            Color32 glass = new Color32(35, 85, 45, 255);

            PFrame(p, w, h, 4, 4, 48, 26, new Color32(75, 45, 22, 255), wood, 2);
            for (int b = 8; b < 48; b += 8)
                PRect(p, w, h, b, 24, 4, 10, glass);
        }

        private static void DrawStairsStoneDown(Color32[] p, int w, int h)
        {
            Color32 stone = new Color32(105, 100, 95, 255);
            Color32 shadow = new Color32(35, 32, 30, 255);

            PRect(p, w, h, 4, 4, 56, 40, shadow);
            for (int step = 0; step < 5; step++)
            {
                int y = step * 8 + 4;
                PRect(p, w, h, 8, y, 48, 5, stone);
                PRect(p, w, h, 8, y, 48, 2, shadow);
            }
        }

        private static void DrawStairsWoodDown(Color32[] p, int w, int h)
        {
            Color32 wood = new Color32(135, 85, 45, 255);
            Color32 shadow = new Color32(55, 30, 15, 255);

            PRect(p, w, h, 4, 4, 56, 40, shadow);
            for (int step = 0; step < 5; step++)
            {
                int y = step * 8 + 4;
                PRect(p, w, h, 8, y, 48, 5, wood);
                PRect(p, w, h, 8, y, 48, 2, shadow);
            }
        }

        private static void DrawWelcomeMat(Color32[] p, int w, int h)
        {
            Color32 coir = new Color32(178, 138, 88, 255);
            Color32 border = new Color32(85, 52, 28, 255);

            PFrame(p, w, h, 4, 4, 56, 28, border, coir, 3);
            // "WELCOME" scrollwork
            PRect(p, w, h, 12, 14, 40, 2, border);
            PRect(p, w, h, 16, 18, 32, 2, border);
        }

        private static void DrawPersianRug(Color32[] p, int w, int h)
        {
            Color32 ruby = new Color32(148, 35, 45, 255);
            Color32 navy = new Color32(32, 45, 68, 255);
            Color32 gold = new Color32(225, 185, 85, 255);
            Color32 fringe = new Color32(245, 242, 235, 255);

            // Fringe on left and right
            for (int y = 4; y < h - 4; y += 2)
            {
                PSet(p, w, h, 2, y, fringe);
                PSet(p, w, h, w - 3, y, fringe);
            }

            PFrame(p, w, h, 4, 4, w - 8, h - 8, navy, ruby, 4);
            // Gold inner border
            PFrame(p, w, h, 10, 10, w - 20, h - 20, gold, ruby, 2);
            // Center Medallion
            PEllipse(p, w, h, w / 2 - 16, h / 2 - 12, 32, 24, navy);
            PEllipse(p, w, h, w / 2 - 10, h / 2 - 8, 20, 16, gold);
            PEllipse(p, w, h, w / 2 - 4, h / 2 - 4, 8, 8, ruby);
        }

        private static void DrawBraidedRug(Color32[] p, int w, int h)
        {
            Color32 col1 = new Color32(185, 75, 85, 255);
            Color32 col2 = new Color32(225, 175, 85, 255);
            Color32 col3 = new Color32(75, 125, 95, 255);
            Color32 col4 = new Color32(235, 225, 195, 255);

            PEllipse(p, w, h, 4, 8, 56, 48, col1);
            PEllipse(p, w, h, 8, 12, 48, 40, col2);
            PEllipse(p, w, h, 13, 16, 38, 32, col3);
            PEllipse(p, w, h, 18, 21, 28, 22, col4);
        }

        private static void DrawNurseryRug(Color32[] p, int w, int h)
        {
            Color32 pastel = new Color32(245, 235, 215, 255);
            Color32 border = new Color32(225, 155, 165, 255);
            PFrame(p, w, h, 4, 4, 72, 48, border, pastel, 3);
            // Stars in corners
            PSet(p, w, h, 16, 16, new Color32(245, 205, 75, 255));
            PSet(p, w, h, 64, 16, new Color32(245, 205, 75, 255));
            PSet(p, w, h, 16, 40, new Color32(245, 205, 75, 255));
            PSet(p, w, h, 64, 40, new Color32(245, 205, 75, 255));
        }

        private static void DrawRunnerRug(Color32[] p, int w, int h)
        {
            Color32 green = new Color32(55, 105, 75, 255);
            Color32 gold = new Color32(225, 185, 85, 255);
            PFrame(p, w, h, 4, 4, 64, 28, gold, green, 2);
        }

        private static void DrawBambooMat(Color32[] p, int w, int h)
        {
            Color32 bamboo = new Color32(195, 175, 125, 255);
            Color32 seam = new Color32(145, 125, 85, 255);
            Color32 border = new Color32(45, 45, 45, 255);

            PFrame(p, w, h, 4, 4, 72, 48, border, bamboo, 2);
            for (int x = 8; x < 72; x += 6)
                PRect(p, w, h, x, 6, 1, 44, seam);
        }

        private static void DrawMonstera(Color32[] p, int w, int h)
        {
            Color32 pot = new Color32(185, 95, 55, 255);
            Color32 leaf = new Color32(45, 125, 55, 255);
            Color32 leafLight = new Color32(75, 165, 85, 255);

            // Terracotta pot
            PRect(p, w, h, 10, 4, 16, 14, pot);
            PRect(p, w, h, 8, 16, 20, 4, new Color32(215, 115, 75, 255));

            // Broad leaves
            PEllipse(p, w, h, 4, 22, 16, 20, leaf);
            PEllipse(p, w, h, 16, 26, 18, 22, leafLight);
            PEllipse(p, w, h, 8, 34, 16, 16, leaf);
        }

        private static void DrawFern(Color32[] p, int w, int h)
        {
            Color32 pot = new Color32(65, 95, 125, 255);
            Color32 leaf = new Color32(55, 145, 65, 255);

            PRect(p, w, h, 10, 4, 16, 14, pot);
            PEllipse(p, w, h, 4, 18, 28, 24, leaf);
        }

        private static void DrawPanRack(Color32[] p, int w, int h)
        {
            Color32 iron = new Color32(45, 45, 45, 255);
            Color32 copper = new Color32(195, 95, 45, 255);

            PRect(p, w, h, 4, 14, 40, 2, iron);
            for (int x = 8; x < 40; x += 10)
            {
                PRect(p, w, h, x, 6, 6, 8, copper);
                PRect(p, w, h, x + 2, 12, 2, 4, iron);
            }
        }

        private static void DrawScrollPainting(Color32[] p, int w, int h)
        {
            Color32 silk = new Color32(235, 225, 205, 255);
            Color32 roller = new Color32(55, 30, 15, 255);
            Color32 mountain = new Color32(115, 135, 145, 255);

            PFrame(p, w, h, 6, 4, 28, 48, roller, silk, 2);
            PRect(p, w, h, 4, 50, 32, 4, roller);
            PRect(p, w, h, 4, 2, 32, 4, roller);

            // Mountain silhouette
            PEllipse(p, w, h, 8, 14, 24, 20, mountain);
            PSet(p, w, h, 20, 38, new Color32(215, 55, 55, 255)); // Red sun
        }

        // Dressing Room Specific Sprites
        private static void DrawGarmentRack(Color32[] p, int w, int h)
        {
            Color32 brass = new Color32(215, 175, 65, 255);
            Color32 wood = new Color32(115, 68, 35, 255);
            // Rack stand
            PRect(p, w, h, 6, 2, 36, 4, wood);
            PRect(p, w, h, 8, 6, 3, 38, brass);
            PRect(p, w, h, 37, 6, 3, 38, brass);
            PRect(p, w, h, 6, 42, 36, 3, brass); // Top rail

            // Hanging clothes
            Color32[] clothes = {
                new Color32(45, 85, 135, 255), // Navy coat
                new Color32(165, 45, 55, 255), // Red dress
                new Color32(235, 225, 210, 255), // Cream shirt
                new Color32(65, 125, 75, 255)  // Green cardigan
            };
            for (int c = 0; c < 4; c++)
            {
                int cx = 13 + c * 6;
                PRect(p, w, h, cx, 14, 5, 26, clothes[c]);
                PSet(p, w, h, cx + 2, 41, brass); // Hanger hook
            }
        }

        private static void DrawStandingMirror(Color32[] p, int w, int h)
        {
            Color32 gold = new Color32(225, 175, 65, 255);
            Color32 mirror = new Color32(195, 230, 245, 255);
            Color32 wood = new Color32(95, 52, 25, 255);

            // Wooden stand
            PRect(p, w, h, 4, 2, 6, 16, wood);
            PRect(p, w, h, 22, 2, 6, 16, wood);

            // Tall oval mirror
            PEllipse(p, w, h, 6, 8, 20, 44, gold);
            PEllipse(p, w, h, 8, 10, 16, 40, mirror);
            // Glass sheen line
            for (int y = 20; y < 44; y++)
                PSet(p, w, h, 12 + (y / 8), y, new Color32(255, 255, 255, 200));
        }

        private static void DrawDressingBench(Color32[] p, int w, int h)
        {
            Color32 wood = new Color32(95, 52, 25, 255);
            Color32 velvet = new Color32(235, 220, 200, 255);
            // Legs
            PRect(p, w, h, 6, 2, 4, 14, wood);
            PRect(p, w, h, 38, 2, 4, 14, wood);
            // Padded cushion
            PFrame(p, w, h, 4, 12, 40, 12, new Color32(145, 95, 55, 255), velvet, 2);
            // Tufting buttons
            PSet(p, w, h, 14, 18, wood);
            PSet(p, w, h, 24, 18, wood);
            PSet(p, w, h, 34, 18, wood);
        }

        private static void DrawShoeRack(Color32[] p, int w, int h)
        {
            Color32 wood = new Color32(125, 78, 42, 255);
            PFrame(p, w, h, 4, 2, 32, 24, new Color32(75, 45, 22, 255), wood, 2);
            PRect(p, w, h, 6, 13, 28, 2, wood); // Middle shelf

            // Shoes lower shelf
            PRect(p, w, h, 8, 4, 6, 6, new Color32(45, 35, 30, 255)); // Black boots
            PRect(p, w, h, 16, 4, 6, 6, new Color32(145, 55, 40, 255)); // Red shoes
            PRect(p, w, h, 24, 4, 6, 6, new Color32(75, 55, 45, 255)); // Brown shoes
        }
        #endregion

        #region Infrastructure Helpers
        private static int SortingOrder(Transform root, Vector2 position, int layerOrder)
        {
            int layerBias = Mathf.Abs(layerOrder) < 30 ? layerOrder * 20 : layerOrder;
            return Mathf.RoundToInt(-root.TransformPoint(position).y * 100f) + layerBias;
        }

        private static GameObject Rect(Transform root, string name, Vector2 position, Vector2 size, Color color, int order, bool collider = false)
        {
            var item = new GameObject(name);
            item.transform.SetParent(root, false);
            item.transform.localPosition = new Vector3(position.x, position.y, 0f);
            item.transform.localScale = new Vector3(size.x, size.y, 1f);
            var renderer = item.AddComponent<SpriteRenderer>();
            renderer.sprite = whiteSprite;
            renderer.color = color;
            renderer.sortingOrder = order <= -1000 ? order : SortingOrder(root, position, order);
            if (collider)
                item.AddComponent<BoxCollider2D>();
            return item;
        }

        private static void AddLabel(Transform root, string text, Vector2 position, float width, float height, Color color, int order)
        {
            var label = new GameObject("Label - " + text);
            label.transform.SetParent(root, false);
            label.transform.localPosition = new Vector3(position.x, position.y, 0f);
            label.transform.localScale = Vector3.one;
            var textMesh = label.AddComponent<TextMeshPro>();
            textMesh.text = text;
            textMesh.fontSize = 3.6f;
            textMesh.alignment = TextAlignmentOptions.Center;
            textMesh.color = color;
            textMesh.rectTransform.sizeDelta = new Vector2(width, height);
            var renderer = label.GetComponent<MeshRenderer>();
            renderer.sortingOrder = SortingOrder(root, position, order);
        }

        private static void EnsureBaseSprites()
        {
            if (whiteSprite == null)
            {
                var texture = new Texture2D(1, 1, TextureFormat.RGBA32, false)
                {
                    filterMode = FilterMode.Point,
                    wrapMode = TextureWrapMode.Clamp
                };
                texture.SetPixel(0, 0, Color.white);
                texture.Apply();
                whiteSprite = Sprite.Create(texture, new Rect(0, 0, 1, 1), new Vector2(0.5f, 0.5f), PixelsPerUnit);
            }

            if (circleSprite == null)
            {
                const int resolution = 64;
                var texture = new Texture2D(resolution, resolution, TextureFormat.RGBA32, false)
                {
                    filterMode = FilterMode.Bilinear,
                    wrapMode = TextureWrapMode.Clamp
                };
                for (int y = 0; y < resolution; y++)
                for (int x = 0; x < resolution; x++)
                {
                    float dx = (x + 0.5f) / resolution * 2f - 1f;
                    float dy = (y + 0.5f) / resolution * 2f - 1f;
                    float distSq = dx * dx + dy * dy;
                    if (distSq <= 1f)
                    {
                        float alpha = Mathf.Clamp01(1f - Mathf.Sqrt(distSq));
                        texture.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
                    }
                    else
                    {
                        texture.SetPixel(x, y, Color.clear);
                    }
                }
                texture.Apply();
                circleSprite = Sprite.Create(texture, new Rect(0, 0, resolution, resolution), new Vector2(0.5f, 0.5f), resolution);
            }
        }
        #endregion
    }
}
