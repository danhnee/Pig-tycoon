using System.Collections.Generic;
using System.Linq;
using PigTycoon.Presentation.Services;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;

public static class Task3MapSceneBuilder
{
    private const string FarmSource = "Assets/Scenes/MainFarm2D.unity";
    private const string FarmTarget = "Assets/Scenes/Farm_Core.unity";
    private const string HubTarget = "Assets/Scenes/Hub_Village.unity";

    [MenuItem("Pig Tycoon/Task 3/Create Map Scenes (preserve source farm)")]
    public static void CreateMapScenes()
    {
        Scene previousActiveScene = SceneManager.GetActiveScene();
        if (!AssetDatabase.LoadAssetAtPath<SceneAsset>(FarmSource))
        {
            Debug.LogError($"Source farm scene not found: {FarmSource}");
            return;
        }

        bool createdFarm = false;
        bool createdHub = false;
        if (!AssetDatabase.LoadAssetAtPath<SceneAsset>(FarmTarget))
        {
            if (!AssetDatabase.CopyAsset(FarmSource, FarmTarget))
            {
                Debug.LogError($"Could not copy {FarmSource} to {FarmTarget}; source was not modified.");
                return;
            }

            createdFarm = true;
            Scene farm = EditorSceneManager.OpenScene(FarmTarget, OpenSceneMode.Additive);
            AddSpawn(farm, "FarmReturn", new Vector3(18f, 0f, 0f));
            AddTransition(farm, "Gateway_To_Hub", new Vector3(18f, 0f, 0f), "Hub_Village", "TrailEntrance");
            EditorSceneManager.SaveScene(farm);
        }

        if (!AssetDatabase.LoadAssetAtPath<SceneAsset>(HubTarget))
        {
            Scene hub = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
            hub.name = "Hub_Village";
            BuildHubLandscape(hub);
            AddSpawn(hub, "TrailEntrance", new Vector3(1000f, -30f, 0f));
            AddTransition(hub, "Gateway_To_Farm", new Vector3(1000f, -30f, 0f), "Farm_Core", "FarmReturn");
            EditorSceneManager.SaveScene(hub, HubTarget);
            createdHub = true;
        }

        ConfigureBuildScenes();
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        if (previousActiveScene.IsValid() && previousActiveScene.isLoaded)
            SceneManager.SetActiveScene(previousActiveScene);
        Debug.Log($"Task 3 map creation finished. Farm_Core created: {createdFarm}; Hub_Village created: {createdHub}. Existing MainFarm2D was only read/copied.");
    }

    private static void BuildHubLandscape(Scene scene)
    {
        GameObject gridObject = new GameObject("Hub_Landscape_Grid", typeof(Grid));
        SceneManager.MoveGameObjectToScene(gridObject, scene);
        Grid grid = gridObject.GetComponent<Grid>();
        grid.cellSize = Vector3.one;

        GameObject groundObject = new GameObject("Ground", typeof(Tilemap), typeof(TilemapRenderer));
        groundObject.transform.SetParent(gridObject.transform, false);
        Tilemap ground = groundObject.GetComponent<Tilemap>();
        TileBase grass = AssetDatabase.LoadAssetAtPath<TileBase>("Assets/Art/Tiles/Tile_GrassSteppe.asset") ??
                         AssetDatabase.LoadAssetAtPath<TileBase>("Assets/Art/Tiles/Tile_Grass.asset");
        TileBase path = AssetDatabase.LoadAssetAtPath<TileBase>("Assets/Art/Tiles/Tile_DirtTrail.asset");

        if (grass != null)
        {
            for (int x = 965; x <= 1055; x++)
            for (int y = -48; y <= 24; y++)
                ground.SetTile(new Vector3Int(x, y, 0), grass);
        }

        Vector2[] route =
        {
            new Vector2(1000f, -30f), new Vector2(1008f, -20f), new Vector2(993f, -11f),
            new Vector2(1003f, -1f), new Vector2(1018f, 7f), new Vector2(1030f, 10f),
            new Vector2(1040f, 1f), new Vector2(1044f, -2f)
        };
        if (path != null)
        {
            for (int segment = 0; segment < route.Length - 1; segment++)
            {
                Vector2 start = route[segment];
                Vector2 end = route[segment + 1];
                int steps = Mathf.CeilToInt(Vector2.Distance(start, end) * 4f);
                for (int i = 0; i <= steps; i++)
                {
                    Vector2 point = Vector2.Lerp(start, end, i / (float)steps);
                    Vector3Int cell = new Vector3Int(Mathf.RoundToInt(point.x), Mathf.RoundToInt(point.y), 0);
                    ground.SetTile(cell, path);
                    ground.SetTile(cell + Vector3Int.up, path);
                }
            }
        }

        Sprite tree = LoadSprite("Assets/Art/Sprites/Environment/tree_prairie.png");
        Sprite rock = LoadSprite("Assets/Art/Sprites/Environment/rock_boulder.png");
        Vector2[] trees =
        {
            new Vector2(992,-28),new Vector2(1007,-27),new Vector2(1014,-22),new Vector2(987,-18),
            new Vector2(995,-16),new Vector2(1012,-14),new Vector2(1022,-12),new Vector2(984,-9),
            new Vector2(1009,-7),new Vector2(1026,-4),new Vector2(991,0),new Vector2(1015,1),
            new Vector2(1034,3),new Vector2(1020,14),new Vector2(1040,13),new Vector2(1048,5),
            new Vector2(1031,-8),new Vector2(997,11),new Vector2(977,-25),new Vector2(1046,-15)
        };
        foreach (Vector2 position in trees)
            AddScenery(scene, "Prairie_Tree", tree, position, 1.8f, 2);

        Vector2[] rocks =
        {
            new Vector2(1003,-24),new Vector2(989,-13),new Vector2(1014,-9),new Vector2(1023,2),
            new Vector2(1036,-7),new Vector2(1000,8),new Vector2(1045,10)
        };
        foreach (Vector2 position in rocks)
            AddScenery(scene, "Trail_Rock", rock, position, 1.1f, 1);

        GameObject townCenter = new GameObject("Village_Expansion_Area");
        townCenter.transform.position = new Vector3(1040f, 0f, 0f);
        SceneManager.MoveGameObjectToScene(townCenter, scene);
    }

    private static Sprite LoadSprite(string path)
    {
        Object[] assets = AssetDatabase.LoadAllAssetsAtPath(path);
        return assets.OfType<Sprite>().FirstOrDefault();
    }

    private static void AddScenery(Scene scene, string prefix, Sprite sprite, Vector2 position, float scale, int order)
    {
        if (sprite == null)
            return;

        var item = new GameObject($"{prefix}_{Mathf.RoundToInt(position.x)}_{Mathf.RoundToInt(position.y)}");
        item.transform.position = new Vector3(position.x, position.y, 0f);
        item.transform.localScale = Vector3.one * scale;
        SceneManager.MoveGameObjectToScene(item, scene);
        SpriteRenderer renderer = item.AddComponent<SpriteRenderer>();
        renderer.sprite = sprite;
        renderer.sortingOrder = order;
        BoxCollider2D collider = item.AddComponent<BoxCollider2D>();
        collider.size = new Vector2(0.8f, 0.7f);
        collider.offset = new Vector2(0f, -0.15f);
    }

    private static void AddSpawn(Scene scene, string id, Vector3 position)
    {
        var spawn = new GameObject($"Spawn_{id}");
        spawn.transform.position = position;
        SceneManager.MoveGameObjectToScene(spawn, scene);
        MapSpawnPoint component = spawn.AddComponent<MapSpawnPoint>();
        SetSerializedString(component, "spawnId", id);
    }

    private static void AddTransition(Scene scene, string name, Vector3 position, string destination, string spawnId)
    {
        var gateway = new GameObject(name);
        gateway.transform.position = position;
        SceneManager.MoveGameObjectToScene(gateway, scene);
        BoxCollider2D trigger = gateway.AddComponent<BoxCollider2D>();
        trigger.isTrigger = true;
        trigger.size = new Vector2(2.5f, 2.5f);
        MapTransitionPoint point = gateway.AddComponent<MapTransitionPoint>();
        SetSerializedString(point, "destinationScene", destination);
        SetSerializedString(point, "destinationSpawnId", spawnId);
    }

    private static void SetSerializedString(Object target, string propertyName, string value)
    {
        var serialized = new SerializedObject(target);
        serialized.FindProperty(propertyName).stringValue = value;
        serialized.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void ConfigureBuildScenes()
    {
        List<EditorBuildSettingsScene> scenes = EditorBuildSettings.scenes.ToList();
        AddBuildScene(scenes, FarmTarget);
        AddBuildScene(scenes, HubTarget);
        scenes = scenes.OrderBy(scene => scene.path == FarmTarget ? 0 : scene.path == HubTarget ? 1 : 2).ToList();
        EditorBuildSettings.scenes = scenes.ToArray();
    }

    private static void AddBuildScene(List<EditorBuildSettingsScene> scenes, string path)
    {
        if (scenes.Any(scene => scene.path == path))
            return;

        scenes.Add(new EditorBuildSettingsScene(path, true));
    }
}
