using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace PigTycoon.EditorTools
{
    public static class TilePaletteBuilder
    {
        private const string TilesFolder = "Assets/Art/Tiles";
        private const string PalettePrefabPath = "Assets/Art/Tiles/FarmTilePalette.prefab";

        [MenuItem("PigTycoon/3. Tạo Tile Palette Để Tự Vẽ Map")]
        public static void GenerateTilePalette()
        {
            if (!AssetDatabase.IsValidFolder(TilesFolder))
            {
                AssetDatabase.CreateFolder("Assets/Art", "Tiles");
            }

            // 1. Tạo các Tile Asset lưu trữ vĩnh viễn
            var tGrass = CreateOrLoadTile("Assets/Art/Tiles/grass_base.png", "Assets/Art/Tiles/Tile_Grass.asset");
            var tSteppe = CreateOrLoadTile("Assets/Art/Tiles/grass_steppe.png", "Assets/Art/Tiles/Tile_GrassSteppe.asset");
            var tTall = CreateOrLoadTile("Assets/Art/Tiles/grass_tall.png", "Assets/Art/Tiles/Tile_GrassTall.asset");
            var tFlower1 = CreateOrLoadTile("Assets/Art/Tiles/grass_flower_1.png", "Assets/Art/Tiles/Tile_Flower1.asset");
            var tFlower2 = CreateOrLoadTile("Assets/Art/Tiles/grass_flower_2.png", "Assets/Art/Tiles/Tile_Flower2.asset");
            var tFlower3 = CreateOrLoadTile("Assets/Art/Tiles/grass_flower_3.png", "Assets/Art/Tiles/Tile_Flower3.asset");
            var tStone = CreateOrLoadTile("Assets/Art/Tiles/stone_path.png", "Assets/Art/Tiles/Tile_StonePath.asset");
            var tTrail = CreateOrLoadTile("Assets/Art/Tiles/dirt_trail.png", "Assets/Art/Tiles/Tile_DirtTrail.asset");
            var tTrailH = CreateOrLoadTile("Assets/Art/Tiles/dirt_trail_h.png", "Assets/Art/Tiles/Tile_DirtTrail_H.asset");
            var tTrailV = CreateOrLoadTile("Assets/Art/Tiles/dirt_trail_v.png", "Assets/Art/Tiles/Tile_DirtTrail_V.asset");
            var tTrailCross = CreateOrLoadTile("Assets/Art/Tiles/dirt_trail_cross.png", "Assets/Art/Tiles/Tile_DirtTrail_Cross.asset");
            var tTrailCornerSW = CreateOrLoadTile("Assets/Art/Tiles/dirt_trail_corner_sw.png", "Assets/Art/Tiles/Tile_DirtTrail_Corner_SW.asset");
            var tTrailTEast = CreateOrLoadTile("Assets/Art/Tiles/dirt_trail_t_east.png", "Assets/Art/Tiles/Tile_DirtTrail_T_East.asset");
            var tTrailTWest = CreateOrLoadTile("Assets/Art/Tiles/dirt_trail_t_west.png", "Assets/Art/Tiles/Tile_DirtTrail_T_West.asset");
            var tDirt = CreateOrLoadTile("Assets/Art/Tiles/dirt_patch.png", "Assets/Art/Tiles/Tile_DirtPatch.asset");
            var tMud = CreateOrLoadTile("Assets/Art/Tiles/mud_tile.png", "Assets/Art/Tiles/Tile_Mud.asset");
            var tWater = CreateOrLoadTile("Assets/Art/Tiles/water_tile.png", "Assets/Art/Tiles/Tile_Water.asset");

            // 2. Tạo Prefab Palette cho Unity 2D Tile Palette Window
            var paletteRoot = new GameObject("FarmTilePalette");
            var grid = paletteRoot.AddComponent<Grid>();
            grid.cellSize = new Vector3(1f, 1f, 0f);

            var layerObj = new GameObject("Layer1");
            layerObj.transform.SetParent(paletteRoot.transform, false);
            var tilemap = layerObj.AddComponent<Tilemap>();
            layerObj.AddComponent<TilemapRenderer>();

            // Xếp các Tile lên Palette theo hàng lối trực quan
            // Hàng 2: Cỏ thảo nguyên & Cỏ cao
            tilemap.SetTile(new Vector3Int(0, 2, 0), tSteppe);
            tilemap.SetTile(new Vector3Int(1, 2, 0), tTall);
            tilemap.SetTile(new Vector3Int(2, 2, 0), tGrass);

            // Hàng 1: Hoa dại thảo nguyên
            tilemap.SetTile(new Vector3Int(0, 1, 0), tFlower1);
            tilemap.SetTile(new Vector3Int(1, 1, 0), tFlower2);
            tilemap.SetTile(new Vector3Int(2, 1, 0), tFlower3);

            // Hàng 0: Đường mòn đất (ngang, dọc, ngã tư, cua, ngã ba), Đá cuội, Bãi bùn & Hồ nước
            tilemap.SetTile(new Vector3Int(0, 0, 0), tTrailH);
            tilemap.SetTile(new Vector3Int(1, 0, 0), tTrailV);
            tilemap.SetTile(new Vector3Int(2, 0, 0), tTrailCross);
            tilemap.SetTile(new Vector3Int(3, 0, 0), tTrailCornerSW);
            tilemap.SetTile(new Vector3Int(4, 0, 0), tTrailTEast);
            tilemap.SetTile(new Vector3Int(5, 0, 0), tTrailTWest);
            tilemap.SetTile(new Vector3Int(6, 0, 0), tStone);
            tilemap.SetTile(new Vector3Int(7, 0, 0), tDirt);
            tilemap.SetTile(new Vector3Int(8, 0, 0), tMud);
            tilemap.SetTile(new Vector3Int(9, 0, 0), tWater);

            PrefabUtility.SaveAsPrefabAsset(paletteRoot, PalettePrefabPath);
            GameObject.DestroyImmediate(paletteRoot);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log($"<color=green>[PigTycoon] Đã tạo thành công Tile Palette tại: {PalettePrefabPath}! Mở Window -> 2D -> Tile Palette để bắt đầu vẽ map tự do.</color>");
        }

        private static Tile CreateOrLoadTile(string spritePath, string tileAssetPath)
        {
            var existingTile = AssetDatabase.LoadAssetAtPath<Tile>(tileAssetPath);
            var sp = AssetDatabase.LoadAssetAtPath<Sprite>(spritePath);
            if (sp == null)
            {
                AssetDatabase.ImportAsset(spritePath, ImportAssetOptions.ForceUpdate);
                sp = AssetDatabase.LoadAssetAtPath<Sprite>(spritePath);
            }

            if (existingTile != null)
            {
                existingTile.sprite = sp;
                EditorUtility.SetDirty(existingTile);
                return existingTile;
            }

            var newTile = ScriptableObject.CreateInstance<Tile>();
            newTile.sprite = sp;
            newTile.color = Color.white;
            AssetDatabase.CreateAsset(newTile, tileAssetPath);
            return newTile;
        }
    }
}
