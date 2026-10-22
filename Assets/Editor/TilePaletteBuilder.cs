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
        private const string VillagePalettePrefabPath = "Assets/Art/Tiles/VillageTilePalette.prefab";

        [MenuItem("PigTycoon/3. Tạo Tile Palette Để Tự Vẽ Map")]
        public static void GenerateTilePalette()
        {
            if (!AssetDatabase.IsValidFolder(TilesFolder))
            {
                AssetDatabase.CreateFolder("Assets/Art", "Tiles");
            }
            if (!AssetDatabase.IsValidFolder("Assets/Art/Tiles/TileAssets"))
            {
                AssetDatabase.CreateFolder("Assets/Art/Tiles", "TileAssets");
            }

            // 1. Tạo các Tile Asset cho Cỏ & Nước
            var tGrass = CreateOrLoadTile("grass_base.png", "Tile_Grass.asset");
            var tSteppe = CreateOrLoadTile("grass_steppe.png", "Tile_GrassSteppe.asset");
            var tTall = CreateOrLoadTile("grass_tall.png", "Tile_GrassTall.asset");
            var tFlower1 = CreateOrLoadTile("grass_flower_1.png", "Tile_Flower1.asset");
            var tFlower2 = CreateOrLoadTile("grass_flower_2.png", "Tile_Flower2.asset");
            var tFlower3 = CreateOrLoadTile("grass_flower_3.png", "Tile_Flower3.asset");
            var tWaterDeep = CreateOrLoadTile("water_deep.png", "Tile_WaterDeep.asset");
            var tWaterShallow = CreateOrLoadTile("water_shallow.png", "Tile_WaterShallow.asset");

            // 2. Tạo các Tile Asset cho Đường Mòn Đất (Đơn & 2-Block)
            var tTrailH = CreateOrLoadTile("dirt_trail_h.png", "Tile_DirtTrail_H.asset");
            var tTrailV = CreateOrLoadTile("dirt_trail_v.png", "Tile_DirtTrail_V.asset");
            var tTrailCross = CreateOrLoadTile("dirt_trail_cross.png", "Tile_DirtTrail_Cross.asset");
            var tTrailTN = CreateOrLoadTile("dirt_trail_t_north.png", "Tile_DirtTrail_T_North.asset");
            var tTrailTS = CreateOrLoadTile("dirt_trail_t_south.png", "Tile_DirtTrail_T_South.asset");
            var tTrailTW = CreateOrLoadTile("dirt_trail_t_west.png", "Tile_DirtTrail_T_West.asset");
            var tTrailTE = CreateOrLoadTile("dirt_trail_t_east.png", "Tile_DirtTrail_T_East.asset");
            var tTrailCNW = CreateOrLoadTile("dirt_trail_corner_nw.png", "Tile_DirtTrail_Corner_NW.asset");
            var tTrailCNE = CreateOrLoadTile("dirt_trail_corner_ne.png", "Tile_DirtTrail_Corner_NE.asset");
            var tTrailCSW = CreateOrLoadTile("dirt_trail_corner_sw.png", "Tile_DirtTrail_Corner_SW.asset");
            var tTrailCSE = CreateOrLoadTile("dirt_trail_corner_se.png", "Tile_DirtTrail_Corner_SE.asset");
            var tDirtCenter = CreateOrLoadTile("dirt_trail_center.png", "Tile_DirtTrail_Center.asset");
            var tDirtEN = CreateOrLoadTile("dirt_trail_edge_n.png", "Tile_DirtTrail_Edge_N.asset");
            var tDirtES = CreateOrLoadTile("dirt_trail_edge_s.png", "Tile_DirtTrail_Edge_S.asset");
            var tDirtEW = CreateOrLoadTile("dirt_trail_edge_w.png", "Tile_DirtTrail_Edge_W.asset");
            var tDirtEE = CreateOrLoadTile("dirt_trail_edge_e.png", "Tile_DirtTrail_Edge_E.asset");

            // 3. Khối Chéo (Diagonals 45 độ)
            var tDirtDiagNETop = CreateOrLoadTile("dirt_trail_diag_ne_top.png", "Tile_DirtTrail_Diag_NE_Top.asset");
            var tDirtDiagNEBot = CreateOrLoadTile("dirt_trail_diag_ne_bot.png", "Tile_DirtTrail_Diag_NE_Bot.asset");
            var tDirtDiagNWTop = CreateOrLoadTile("dirt_trail_diag_nw_top.png", "Tile_DirtTrail_Diag_NW_Top.asset");
            var tDirtDiagNWBot = CreateOrLoadTile("dirt_trail_diag_nw_bot.png", "Tile_DirtTrail_Diag_NW_Bot.asset");

            // 4. Đường Đá Cuội (Stone Path)
            var tStoneH = CreateOrLoadTile("stone_path_h.png", "Tile_StonePath_H.asset");
            var tStoneV = CreateOrLoadTile("stone_path_v.png", "Tile_StonePath_V.asset");
            var tStoneCenter = CreateOrLoadTile("stone_path_center.png", "Tile_StonePath_Center.asset");
            var tStoneEN = CreateOrLoadTile("stone_path_edge_n.png", "Tile_StonePath_Edge_N.asset");
            var tStoneES = CreateOrLoadTile("stone_path_edge_s.png", "Tile_StonePath_Edge_S.asset");
            var tStoneEW = CreateOrLoadTile("stone_path_edge_w.png", "Tile_StonePath_Edge_W.asset");
            var tStoneEE = CreateOrLoadTile("stone_path_edge_e.png", "Tile_StonePath_Edge_E.asset");
            var tStoneCNW = CreateOrLoadTile("stone_path_corner_nw.png", "Tile_StonePath_Corner_NW.asset");
            var tStoneCNE = CreateOrLoadTile("stone_path_corner_ne.png", "Tile_StonePath_Corner_NE.asset");
            var tStoneCSW = CreateOrLoadTile("stone_path_corner_sw.png", "Tile_StonePath_Corner_SW.asset");
            var tStoneCSE = CreateOrLoadTile("stone_path_corner_se.png", "Tile_StonePath_Corner_SE.asset");

            // 5. Tạo Prefab Palette cho Unity 2D Tile Palette Window
            var paletteRoot = new GameObject("VillageTilePalette");
            var grid = paletteRoot.AddComponent<Grid>();
            grid.cellSize = new Vector3(1f, 1f, 0f);

            var layerObj = new GameObject("Layer1");
            layerObj.transform.SetParent(paletteRoot.transform, false);
            var tilemap = layerObj.AddComponent<Tilemap>();
            layerObj.AddComponent<TilemapRenderer>();

            // Hàng 4: Cỏ & Hoa
            tilemap.SetTile(new Vector3Int(0, 4, 0), tGrass);
            tilemap.SetTile(new Vector3Int(1, 4, 0), tSteppe);
            tilemap.SetTile(new Vector3Int(2, 4, 0), tTall);
            tilemap.SetTile(new Vector3Int(3, 4, 0), tFlower1);
            tilemap.SetTile(new Vector3Int(4, 4, 0), tFlower2);
            tilemap.SetTile(new Vector3Int(5, 4, 0), tFlower3);
            tilemap.SetTile(new Vector3Int(6, 4, 0), tWaterDeep);
            tilemap.SetTile(new Vector3Int(7, 4, 0), tWaterShallow);

            // Hàng 3: Đường mòn đất cơ bản (ngang, dọc, ngã 3, ngã 4, 4 góc cua)
            tilemap.SetTile(new Vector3Int(0, 3, 0), tTrailH);
            tilemap.SetTile(new Vector3Int(1, 3, 0), tTrailV);
            tilemap.SetTile(new Vector3Int(2, 3, 0), tTrailCross);
            tilemap.SetTile(new Vector3Int(3, 3, 0), tTrailTN);
            tilemap.SetTile(new Vector3Int(4, 3, 0), tTrailTS);
            tilemap.SetTile(new Vector3Int(5, 3, 0), tTrailTW);
            tilemap.SetTile(new Vector3Int(6, 3, 0), tTrailTE);
            tilemap.SetTile(new Vector3Int(7, 3, 0), tTrailCNW);
            tilemap.SetTile(new Vector3Int(8, 3, 0), tTrailCNE);
            tilemap.SetTile(new Vector3Int(9, 3, 0), tTrailCSW);
            tilemap.SetTile(new Vector3Int(10, 3, 0), tTrailCSE);

            // Hàng 2: Đường đất 2-block & Khối chéo
            tilemap.SetTile(new Vector3Int(0, 2, 0), tDirtCenter);
            tilemap.SetTile(new Vector3Int(1, 2, 0), tDirtEN);
            tilemap.SetTile(new Vector3Int(2, 2, 0), tDirtES);
            tilemap.SetTile(new Vector3Int(3, 2, 0), tDirtEW);
            tilemap.SetTile(new Vector3Int(4, 2, 0), tDirtEE);
            tilemap.SetTile(new Vector3Int(6, 2, 0), tDirtDiagNETop);
            tilemap.SetTile(new Vector3Int(7, 2, 0), tDirtDiagNEBot);
            tilemap.SetTile(new Vector3Int(8, 2, 0), tDirtDiagNWTop);
            tilemap.SetTile(new Vector3Int(9, 2, 0), tDirtDiagNWBot);

            // Hàng 1: Đường đá cuội cơ bản & 2-block
            tilemap.SetTile(new Vector3Int(0, 1, 0), tStoneH);
            tilemap.SetTile(new Vector3Int(1, 1, 0), tStoneV);
            tilemap.SetTile(new Vector3Int(2, 1, 0), tStoneCenter);
            tilemap.SetTile(new Vector3Int(3, 1, 0), tStoneEN);
            tilemap.SetTile(new Vector3Int(4, 1, 0), tStoneES);
            tilemap.SetTile(new Vector3Int(5, 1, 0), tStoneEW);
            tilemap.SetTile(new Vector3Int(6, 1, 0), tStoneEE);
            tilemap.SetTile(new Vector3Int(7, 1, 0), tStoneCNW);
            tilemap.SetTile(new Vector3Int(8, 1, 0), tStoneCNE);
            tilemap.SetTile(new Vector3Int(9, 1, 0), tStoneCSW);
            tilemap.SetTile(new Vector3Int(10, 1, 0), tStoneCSE);

            PrefabUtility.SaveAsPrefabAsset(paletteRoot, VillagePalettePrefabPath);
            PrefabUtility.SaveAsPrefabAsset(paletteRoot, PalettePrefabPath);
            GameObject.DestroyImmediate(paletteRoot);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log($"<color=green>[PigTycoon] Đã tạo thành công Tile Palette đầy đủ tại: {VillagePalettePrefabPath}! Mở Window -> 2D -> Tile Palette để tự do vẽ đường thủ công.</color>");
        }

        private static Tile CreateOrLoadTile(string spriteFileName, string assetFileName)
        {
            string tileAssetPath = $"Assets/Art/Tiles/TileAssets/{assetFileName}";
            var existingTile = AssetDatabase.LoadAssetAtPath<Tile>(tileAssetPath);

            string spritePath = $"Assets/Art/Tiles/Sprites/{spriteFileName}";
            if (!File.Exists(spritePath))
            {
                spritePath = $"Assets/Art/Tiles/{spriteFileName}";
            }

            var sp = AssetDatabase.LoadAssetAtPath<Sprite>(spritePath);
            if (sp == null)
            {
                AssetDatabase.ImportAsset(spritePath, ImportAssetOptions.ForceUpdate);
                sp = AssetDatabase.LoadAssetAtPath<Sprite>(spritePath);
            }

            if (existingTile != null)
            {
                if (sp != null) existingTile.sprite = sp;
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
