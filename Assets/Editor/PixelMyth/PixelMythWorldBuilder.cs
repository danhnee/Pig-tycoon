using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using PigTycoon.Presentation;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;

namespace PigTycoon.EditorTools.PixelMyth
{
    /// <summary>
    /// Builds the four map scenes from Assets/Art/PixelMyth/Maps/&lt;map&gt;_layout.json (written by
    /// tools/pixelart/compose_map.py). Terrain is a dual grid: materials live on cell corners, each cell gets a
    /// base tile of its lowest material plus alpha overlays (mask_&lt;mat&gt;) for every higher material, in
    /// terrain.json layer_order. Objects are y-sorted SpriteRenderers with optional colliders and stations.
    /// Headless: -executeMethod PigTycoon.EditorTools.PixelMyth.PixelMythWorldBuilder.BuildAll
    /// </summary>
    public static class PixelMythWorldBuilder
    {
        private const string MapsRoot = "Assets/Art/PixelMyth/Maps";
        private const string TerrainRoot = "Assets/Art/PixelMyth/Terrain";
        private const string TileAssets = "Assets/Art/PixelMyth/Terrain/Tiles";
        private const string SceneRoot = "Assets/Scenes/PixelMyth";
        private const string PlayerRig = "Assets/Prefabs/PixelWorldCandidates/PF_SourcePlayerRig.prefab";
        private static readonly string[] DefaultLayerOrder = { "water", "path", "stone", "meadow", "forest" };
        private static readonly string[] BlockingMaterials = { "water", "rampart" };   // ramparts are climbed only by their stairs

        [Serializable] private sealed class Terrain { public string[] corner_rows; }
        [Serializable] private sealed class Collider { public float w, h, ox, oy; }
        [Serializable] private sealed class Interact { public string function; public string id; }
        [Serializable] private sealed class Obj { public string sprite; public string sub; public float x, y; public string layer; public Collider collider; public string role; public Interact interact; }
        [Serializable] private sealed class Portal { public string id, target, target_portal; public float x, y; }
        [Serializable] private sealed class Point { public float x, y; }
        [Serializable] private sealed class NpcEntry { public string id, name, home, greeting_src; public float x, y; }
        [Serializable] private sealed class Zone { public string name; public float[] rect; }
        [Serializable] private sealed class EnemyEntry { public float x, y; public string sector; }
        [Serializable]
        private sealed class Layout
        {
            public string map, location_id, scene_name;
            public int[] size;
            public Terrain terrain;
            public Obj[] objects;
            public Portal[] portals;
            public Point spawn;
            public Zone[] zones;
            public NpcEntry[] npcs;
            public EnemyEntry[] enemy_entries;
            public Point[] fence_polygon;
        }
        [Serializable] private sealed class TerrainMeta { public string[] layer_order; }
        [Serializable] private sealed class LegendHolder { public Legend legend; }
        [Serializable] private sealed class Legend { public string m, p, s, w, f, g, d, t, r; }

        [MenuItem("Pig Tycoon/Pixel Myth/Build All Maps")]
        public static void BuildAll()
        {
            if (EditorApplication.isPlaying) throw new InvalidOperationException("Leave Play mode first.");
            PixelMythLibraryBuilder.ImportAndBuild();
            var built = new List<string>();
            foreach (var json in Directory.GetFiles(MapsRoot, "*_layout.json"))
            {
                built.Add(BuildMap(json.Replace('\\', '/')));
            }
            RegisterBuildScenes(built);
            Debug.Log($"[PixelMyth] built scenes: {string.Join(", ", built)}");
        }

        public static string BuildMap(string layoutPath)
        {
            var text = File.ReadAllText(layoutPath);
            var layout = JsonUtility.FromJson<Layout>(text);
            var legend = ParseLegend(text);
            int w = layout.size[0], h = layout.size[1];

            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var order = LoadLayerOrder();
            var corners = ParseCorners(layout.terrain.corner_rows, legend, w, h);

            BuildTerrain(corners, w, h, order);
            var root = new GameObject("World");
            foreach (var o in layout.objects ?? Array.Empty<Obj>()) PlaceObject(o, root.transform);

            var marker = new GameObject($"PixelWorld_{layout.location_id}").AddComponent<PixelWorldScene>();
            marker.LocationId = layout.location_id;
            marker.DefaultArrival = new Vector2(layout.spawn.x, layout.spawn.y);

            foreach (var p in layout.portals ?? Array.Empty<Portal>())
            {
                var go = new GameObject($"Portal_{p.id}");
                go.transform.position = new Vector3(p.x, p.y, 0f);
                var portal = go.AddComponent<PixelWorldPortal>();
                portal.PortalId = p.id;
                portal.TargetLocationId = p.target;
                portal.TargetPortalId = p.target_portal;
                portal.DisplayName = p.target;
                {
                    // arrive one step inside the map, away from whichever edge the portal sits on
                    float mw = layout.size != null && layout.size.Length > 1 ? layout.size[0] : 0f, mh = layout.size != null && layout.size.Length > 1 ? layout.size[1] : 0f;
                    float dl = p.x, dr = mw - p.x, db = p.y, dt = mh - p.y, m = Mathf.Min(Mathf.Min(dl, dr), Mathf.Min(db, dt));
                    if (mw > 0f && m < 6f)
                        portal.ArrivalOffset = m == dl ? new Vector2(3f, 0f) : m == dr ? new Vector2(-3f, 0f) : m == db ? new Vector2(0f, 3f) : new Vector2(0f, -3f);
                }
                // the occult gate is not a building on the map: it rises where/when the event opens (user 31 Oct)
                if (p.target == "Forest_Primeval" || layout.location_id == "Forest_Primeval")
                {
                    var gate = go.AddComponent<MysticGate>();
                    gate.Zones = (layout.zones ?? Array.Empty<Zone>())
                        .Where(z => z.name == "gate_event" && z.rect != null && z.rect.Length == 4)
                        .Select(z => new Rect(z.rect[0], z.rect[1], z.rect[2], z.rect[3])).ToArray();
                }
            }

            AddInteriorDoors(layout);

            if (layout.location_id == "Forest_Primeval")
                new GameObject("ForestEventSites").AddComponent<ForestEventSites>();
            if (layout.location_id == "Farm")
            {
                var spawner = new GameObject("HerdSpawner").AddComponent<PixelMythHerdSpawner>();
                var pasture = (layout.zones ?? Array.Empty<Zone>()).FirstOrDefault(z => z.name == "pasture" && z.rect != null && z.rect.Length == 4);
                if (pasture != null) spawner.Pasture = new Rect(pasture.rect[0], pasture.rect[1], pasture.rect[2], pasture.rect[3]);
                BuildFarmGameplay(layout, w, h);
            }

            SpawnNpcs(layout.npcs, text, root.transform);

            var cam = BuildCamera(w, h);
            BuildLight();
            var player = BuildPlayers(marker.DefaultArrival);
            if (player != null) cam.GetComponent<CameraFollow2D>().Target = player;
            if (UnityEngine.Object.FindFirstObjectByType<MobileGameController>() == null)
            {
                new GameObject("GameController").AddComponent<MobileGameController>();
            }

            Directory.CreateDirectory(SceneRoot);
            var scenePath = $"{SceneRoot}/{layout.scene_name}.unity";
            EditorSceneManager.SaveScene(scene, scenePath);
            return scenePath;
        }

        // NPCs: "npcs" array of {id,name,x,y,home,route,greeting_src}. JsonUtility cannot read the jagged "route"
        // array, so scalar fields come from NpcEntry and each route is cut out of the raw JSON by npc id.
        private static void SpawnNpcs(NpcEntry[] npcs, string json, Transform parent)
        {
            if (npcs == null || npcs.Length == 0) return;
            foreach (var n in npcs)
            {
                if (n == null || string.IsNullOrEmpty(n.id)) continue;
                var go = new GameObject($"NPC_{n.id}");
                go.transform.SetParent(parent, false);
                go.transform.position = new Vector3(n.x, n.y, 0f);
                var sr = go.AddComponent<SpriteRenderer>();
                sr.sortingOrder = Mathf.RoundToInt(-n.y * 100f);
                var rb = go.AddComponent<Rigidbody2D>();
                rb.bodyType = RigidbodyType2D.Kinematic;
                rb.gravityScale = 0f;
                rb.freezeRotation = true;
                var cap = go.AddComponent<CapsuleCollider2D>();
                cap.size = new Vector2(0.5f, 0.35f);
                cap.offset = new Vector2(0f, 0.15f);
                cap.direction = CapsuleDirection2D.Horizontal;
                var npc = go.AddComponent<PixelMythNpc>();
                npc.Id = n.id;
                npc.DisplayName = string.IsNullOrEmpty(n.name) ? n.id : n.name;
                npc.HomeRoleId = string.IsNullOrEmpty(n.home) ? null : n.home;
                npc.GreetingKey = n.greeting_src;
                var route = ParseNpcRoute(json, n.id);
                npc.Route = route.Count > 0 ? route.ToArray() : new[] { new Vector2(n.x, n.y) };
            }
        }

        private static List<Vector2> ParseNpcRoute(string json, string id)
        {
            var result = new List<Vector2>();
            int npcsAt = json.IndexOf("\"npcs\"", StringComparison.Ordinal);
            if (npcsAt < 0) return result;
            int idAt = json.IndexOf("\"id\"", npcsAt, StringComparison.Ordinal);
            while (idAt >= 0)
            {
                int q1 = json.IndexOf('"', json.IndexOf(':', idAt) + 1);
                int q2 = q1 < 0 ? -1 : json.IndexOf('"', q1 + 1);
                if (q2 < 0) return result;
                int next = json.IndexOf("\"id\"", q2, StringComparison.Ordinal);
                if (json.Substring(q1 + 1, q2 - q1 - 1) == id)
                {
                    int end = next < 0 ? json.Length : next;
                    int r = json.IndexOf("\"route\"", q2, StringComparison.Ordinal);
                    // route may be written before "id" inside the object; then search from the object start
                    if (r < 0 || r > end) r = json.LastIndexOf("\"route\"", idAt, StringComparison.Ordinal);
                    if (r < 0) return result;
                    int open = json.IndexOf('[', r);
                    if (open < 0) return result;
                    int depth = 0;
                    var nums = new List<float>();
                    for (int i = open; i < json.Length; i++)
                    {
                        char c = json[i];
                        if (c == '[') { depth++; continue; }
                        if (c == ']') { depth--; if (depth == 0) break; continue; }
                        if (c == '-' || c == '.' || char.IsDigit(c))
                        {
                            int j = i;
                            while (j < json.Length && (json[j] == '-' || json[j] == '.' || json[j] == 'e' || json[j] == 'E' || json[j] == '+' || char.IsDigit(json[j]))) j++;
                            if (float.TryParse(json.Substring(i, j - i), System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out var f)) nums.Add(f);
                            i = j - 1;
                        }
                    }
                    for (int k = 0; k + 1 < nums.Count; k += 2) result.Add(new Vector2(nums[k], nums[k + 1]));
                    return result;
                }
                idAt = next;
            }
            return result;
        }

        private static Dictionary<char, string> ParseLegend(string json)
        {
            var holder = JsonUtility.FromJson<LegendHolderRoot>(json);
            var map = new Dictionary<char, string> { { 'm', "meadow" }, { 'p', "path" }, { 's', "stone" }, { 'w', "water" }, { 'f', "forest" },
                { 'g', "meadow_lush" }, { 'd', "meadow_dry" }, { 't', "dirt" }, { 'r', "rampart" } };
            var lg = holder?.terrain?.legend;
            if (lg != null)
            {
                if (!string.IsNullOrEmpty(lg.m)) map['m'] = lg.m;
                if (!string.IsNullOrEmpty(lg.p)) map['p'] = lg.p;
                if (!string.IsNullOrEmpty(lg.s)) map['s'] = lg.s;
                if (!string.IsNullOrEmpty(lg.w)) map['w'] = lg.w;
                if (!string.IsNullOrEmpty(lg.f)) map['f'] = lg.f;
                if (!string.IsNullOrEmpty(lg.g)) map['g'] = lg.g;
                if (!string.IsNullOrEmpty(lg.d)) map['d'] = lg.d;
                if (!string.IsNullOrEmpty(lg.t)) map['t'] = lg.t;
                if (!string.IsNullOrEmpty(lg.r)) map['r'] = lg.r;
            }
            return map;
        }

        [Serializable] private sealed class LegendHolderRoot { public LegendHolder terrain; }

        private static string[] LoadLayerOrder()
        {
            var path = $"{TerrainRoot}/terrain.json";
            if (!File.Exists(path)) return DefaultLayerOrder;
            var meta = JsonUtility.FromJson<TerrainMeta>(File.ReadAllText(path));
            return meta?.layer_order != null && meta.layer_order.Length > 0 ? meta.layer_order : DefaultLayerOrder;
        }

        // corners[x, y] for x in 0..w, y in 0..h; rows in JSON are top (y = h) first.
        private static string[,] ParseCorners(string[] rows, Dictionary<char, string> legend, int w, int h)
        {
            var c = new string[w + 1, h + 1];
            for (int r = 0; r <= h; r++)
            {
                var row = r < rows.Length ? rows[r] : string.Empty;
                int y = h - r;
                for (int x = 0; x <= w; x++)
                {
                    char ch = x < row.Length ? row[x] : 'm';
                    c[x, y] = legend.TryGetValue(ch, out var mat) ? mat : "meadow";
                }
            }
            return c;
        }

        private static void BuildTerrain(string[,] corners, int w, int h, string[] order)
        {
            var grid = new GameObject("Grid").AddComponent<Grid>();
            grid.cellSize = Vector3.one;
            // Terrain sits at the bottom of the int16 sorting range: Y-sorted actors/props use -y * 100, which reaches
            // -16000 on the 160-tile farm. At -10000 everything north of y = 100 drew UNDER the ground (31 Oct video).
            var baseMap = NewTilemap(grid.transform, "T0_Base", TerrainSortBase);
            var overlays = order.Select((m, i) => NewTilemap(grid.transform, $"T{i + 1}_{m}", TerrainSortBase + i + 1)).ToArray();
            var blocking = NewTilemap(grid.transform, "Blocking", 0);
            blocking.GetComponent<TilemapRenderer>().enabled = false;
            blocking.gameObject.AddComponent<TilemapCollider2D>();
            var blockTile = ScriptableObject.CreateInstance<Tile>();
            blockTile.colliderType = Tile.ColliderType.Grid;

            var tileCache = new Dictionary<string, Tile>();
            Tile TileFor(string sheet, string spriteName)
            {
                if (tileCache.TryGetValue(spriteName, out var t)) return t;
                var sprite = AssetDatabase.LoadAllAssetsAtPath($"{TerrainRoot}/{sheet}.png").OfType<Sprite>().FirstOrDefault(s => s.name == spriteName);
                if (sprite == null) { Debug.LogWarning($"[PixelMyth] missing terrain sprite {spriteName}"); tileCache[spriteName] = null; return null; }
                Directory.CreateDirectory(TileAssets);
                var assetPath = $"{TileAssets}/{spriteName}.asset";
                t = AssetDatabase.LoadAssetAtPath<Tile>(assetPath);
                if (t == null)
                {
                    t = ScriptableObject.CreateInstance<Tile>();
                    AssetDatabase.CreateAsset(t, assetPath);
                }
                t.sprite = sprite;
                t.colliderType = Tile.ColliderType.None;
                EditorUtility.SetDirty(t);
                tileCache[spriteName] = t;
                return t;
            }

            int Rank(string mat) { int i = Array.IndexOf(order, mat); return i < 0 ? order.Length : i; }
            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    // bit order TL=1 TR=2 BL=4 BR=8
                    string tl = corners[x, y + 1], tr = corners[x + 1, y + 1], bl = corners[x, y], br = corners[x + 1, y];
                    var cs = new[] { tl, tr, bl, br };
                    var low = cs.OrderBy(Rank).First();
                    int variant = (x * 7 + y * 3) % 4;
                    var cell = new Vector3Int(x, y, 0);
                    baseMap.SetTile(cell, TileFor($"terrain_{low}", $"terrain_{low}_r0_{variant:00}"));
                    for (int i = Rank(low) + 1; i < order.Length; i++)
                    {
                        int bits = 0;
                        for (int k = 0; k < 4; k++) if (Rank(cs[k]) >= i) bits |= 1 << k;
                        // only layers that own at least one corner draw; higher layers cover the rest
                        if (bits == 0 || !cs.Any(c => Rank(c) == i)) continue;
                        var mat = order[i];
                        overlays[i].SetTile(cell, bits == 15 && cs.All(c => c == mat)
                            ? TileFor($"terrain_{mat}", $"terrain_{mat}_r0_{variant:00}")
                            : TileFor($"mask_{mat}", $"mask_{mat}_r{bits / 4}_{bits % 4:00}"));
                    }
                    int blocked = cs.Count(c => BlockingMaterials.Contains(c));
                    if (blocked >= 3) blocking.SetTile(cell, blockTile);
                }
            }
        }

        /// <summary>Below every Y-sorted renderer (-y * 100 for maps up to 300 tiles tall).</summary>
        public const int TerrainSortBase = -32000;
        /// <summary>Flat ground decals (paths, stones, flowers): above terrain, below every Y-sorted renderer.</summary>
        public const int GroundDecorSort = -31000;

        private static Tilemap NewTilemap(Transform parent, string name, int sortingOrder)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var map = go.AddComponent<Tilemap>();
            var r = go.AddComponent<TilemapRenderer>();
            r.sortingOrder = sortingOrder;
            r.mode = TilemapRenderer.Mode.Chunk;
            return map;
        }

        private static readonly Dictionary<string, Sprite[]> spriteCache = new Dictionary<string, Sprite[]>();

        private static Sprite LoadSprite(string path, string sub)
        {
            if (string.IsNullOrEmpty(path)) return null;
            if (!spriteCache.TryGetValue(path, out var all))
            {
                all = AssetDatabase.LoadAllAssetsAtPath(path).OfType<Sprite>().ToArray();
                spriteCache[path] = all;
            }
            if (all.Length == 0) return null;
            if (string.IsNullOrEmpty(sub)) return all.Length == 1 ? all[0] : all.OrderBy(s => s.name).First();
            // layouts may name the sheet row without the frame suffix ("x_static_down" vs sliced "x_static_down_00")
            return all.FirstOrDefault(s => s.name == sub) ?? all.FirstOrDefault(s => s.name == sub + "_00");
        }

        private static void PlaceObject(Obj o, Transform parent)
        {
            var sprite = LoadSprite(o.sprite, o.sub);
            if (sprite == null)
            {
                Debug.LogWarning($"[PixelMyth] missing sprite {o.sprite} {o.sub}");
                return;
            }
            var go = new GameObject(string.IsNullOrEmpty(o.role) ? sprite.name : $"{o.role}:{sprite.name}");
            go.transform.SetParent(parent, false);
            go.transform.position = new Vector3(o.x, o.y, 0f);
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            switch (o.layer)
            {
                case "ground": sr.sortingOrder = GroundDecorSort; break;
                case "under": sr.sortingOrder = GroundDecorSort - 100; break;   // pond depth decals under lily pads/ripples
                // tree crowns Y-sort by their trunk base like every prop: a fixed 10000 drew trees standing BEHIND a
                // house on top of its roof ("cây mọc trên nhà", user 31 Oct)
                default: sr.sortingOrder = Mathf.RoundToInt(-o.y * 100f); break;
            }
            if (o.collider != null && o.collider.w > 0f && o.collider.h > 0f)
            {
                var box = go.AddComponent<BoxCollider2D>();
                box.size = new Vector2(o.collider.w, o.collider.h);
                box.offset = new Vector2(o.collider.ox, o.collider.oy);
                var role = o.role ?? "";
                if (role.StartsWith("building:") && role != "building:herd_shelter")
                {
                    // 4 Nov user report "nhân vật đi xuyên qua công trình": the layout box was a thin strip centred on
                    // the wall base, so half of it lay on the grass in front and the whole building body above it was
                    // walkable. In 3/4 view the footprint starts at the base and runs back about 60% of the drawn
                    // width/height; the door stays reachable (door 1.2 m in front, radius 1.8 m). The open herd
                    // shelter keeps its strip: pigs and the player walk under it.
                    var size = sprite.bounds.size;
                    float depth = Mathf.Max(o.collider.h, Mathf.Min(0.6f * size.x, 0.6f * size.y));
                    const float frontStep = 0.3f;
                    box.size = new Vector2(o.collider.w, depth);
                    box.offset = new Vector2(o.collider.ox, -frontStep + depth * 0.5f);
                }
            }
            if (o.interact != null && !string.IsNullOrEmpty(o.interact.function)
                && Enum.TryParse<BuildingInteractionFunction>(o.interact.function, out var fn))
            {
                var station = go.AddComponent<CustomBuildingController>();
                station.AssignedFunction = fn;
                // the world prompt names the verb of THIS place (it said "Vào Nhà" on stalls, boards and forest sites)
                station.ActionPromptText = fn switch
                {
                    BuildingInteractionFunction.GiaoDichDanLang => "Giao dịch",
                    BuildingInteractionFunction.DocBangTinLore => "Xem",
                    BuildingInteractionFunction.MoKhoLuuTru => "Mở kho",
                    BuildingInteractionFunction.NghiNgoiGiuong => "Nghỉ",
                    _ => "Vào",
                };
                go.name += $" [{o.interact.id}]";
                ConfigureStation(station, o.interact.id);
            }
            AttachFarmInfrastructure(go, o.role ?? "");
        }

        /// <summary>
        /// Walk-in interiors (4 Nov): for every interior of this map whose layout exists, a portal at the building's
        /// door (1.2 m in front, where CustomBuildingController expects the door) leads inside; coming back out puts
        /// the player just in front of the door. The interior layout carries the matching exit portal.
        /// </summary>
        private static void AddInteriorDoors(Layout layout)
        {
            var interiorLayouts = new HashSet<string>();
            foreach (var json in Directory.GetFiles(MapsRoot, "*_layout.json"))
            {
                var l = JsonUtility.FromJson<Layout>(File.ReadAllText(json));
                if (l != null && !string.IsNullOrEmpty(l.location_id)) interiorLayouts.Add(l.location_id);
            }
            foreach (var inn in PigTycoon.Core.PixelWorldInteriors.All)
            {
                if (inn.ParentLocationId != layout.location_id || !interiorLayouts.Contains(inn.LocationId)) continue;
                var building = (layout.objects ?? Array.Empty<Obj>()).FirstOrDefault(o => o.interact != null && o.interact.id == inn.BuildingId);
                if (building == null) { Debug.LogWarning($"[PixelMyth] interior {inn.LocationId}: no building '{inn.BuildingId}' in {layout.location_id}"); continue; }
                var go = new GameObject($"Portal_{inn.DoorPortalId}");
                go.transform.position = new Vector3(building.x, building.y - 1.2f, 0f);
                var portal = go.AddComponent<PixelWorldPortal>();
                portal.PortalId = inn.DoorPortalId;
                portal.TargetLocationId = inn.LocationId;
                portal.TargetPortalId = inn.ExitPortalId;
                portal.DisplayName = inn.DisplayName;
                portal.InteractionRadius = 1.8f;
                portal.ArrivalOffset = new Vector2(0f, -1.0f);
            }
        }

        // ---- gameplay wiring (GDD 3.6–3.8): the layout's props become the farm systems the player uses ----

        private static void AttachFarmInfrastructure(GameObject go, string role)
        {
            if (role == "fence")
            {
                if (go.GetComponent<BoxCollider2D>() != null) go.AddComponent<PixelFenceSegment>();
                return;
            }
            if (role.StartsWith("prop:feeder"))
            {
                var f = EnsureSolidCollider(go).AddComponent<Feeder2DView>();
                f.SpriteRenderer = go.GetComponent<SpriteRenderer>();
                f.FullColor = Color.white;                                  // never tint the pixel art when full
                f.EmptyColor = new Color(0.78f, 0.74f, 0.7f, 1f);
            }
            else if (role.StartsWith("prop:basin"))
            {
                var t = EnsureSolidCollider(go).AddComponent<WaterTrough2DView>();
                t.SpriteRenderer = go.GetComponent<SpriteRenderer>();
                t.FullWaterColor = Color.white;
                t.EmptyWaterColor = new Color(0.8f, 0.78f, 0.74f, 1f);
            }
            else if (role.StartsWith("prop:wallow"))
            {
                var trig = go.AddComponent<BoxCollider2D>();
                trig.isTrigger = true;
                trig.size = new Vector2(3.2f, 2.0f);
                trig.offset = new Vector2(0f, 0.6f);
                go.AddComponent<MudPit2DView>();
            }
            else if (role == "building:herd_shelter")
            {
                // pigs rest in front of the shelter (Shelter2DView.GetShelterRestSpot is 1.35 m below the base)
                var trig = go.AddComponent<BoxCollider2D>();
                trig.isTrigger = true;
                trig.size = new Vector2(5f, 1.6f);
                trig.offset = new Vector2(0f, -1.35f);
                go.AddComponent<Shelter2DView>();
            }
            else if (role.StartsWith("prop:feed_silo"))
            {
                var s = EnsureSolidCollider(go).AddComponent<FeedSilo2DView>();
                s.SpriteRenderer = go.GetComponent<SpriteRenderer>();
            }
            else if (role.StartsWith("prop:well"))
            {
                var w = EnsureSolidCollider(go).AddComponent<WaterWell2DView>();
                w.SpriteRenderer = go.GetComponent<SpriteRenderer>();
            }
        }

        // trough/basin/silo/well views require a Collider2D; layout props may have none
        private static GameObject EnsureSolidCollider(GameObject go)
        {
            if (go.GetComponent<Collider2D>() == null)
            {
                var box = go.AddComponent<BoxCollider2D>();
                box.size = new Vector2(1.8f, 0.7f);
                box.offset = new Vector2(0f, 0.35f);
            }
            return go;
        }

        private static void BuildFarmGameplay(Layout layout, int w, int h)
        {
            var fences = (layout.objects ?? Array.Empty<Obj>()).Where(o => o.role == "fence").ToList();
            var fence = fences.Count > 0
                ? Rect.MinMaxRect(fences.Min(o => o.x), fences.Min(o => o.y), fences.Max(o => o.x), fences.Max(o => o.y))
                : new Rect(0, 0, w, h);
            var inner = new Rect(fence.x + 1.5f, fence.y + 1.5f, fence.width - 3f, fence.height - 3f);

            var envGo = new GameObject("FarmEnvironment2D");
            var env = envGo.AddComponent<FarmEnvironment2D>();
            env.ApplyLegacyLayout = false;
            env.MapBounds = new Rect(0, 0, w, h);
            env.PigPastureBounds = inner;
            if (layout.fence_polygon != null && layout.fence_polygon.Length >= 4)
                env.PasturePolygon = layout.fence_polygon.Select(p => new Vector2(p.x, p.y)).ToArray();
            env.PastureGates = fences.Where(o => (o.sprite ?? "").Contains("fence_gate_arch")).Select(o => new Vector2(o.x, o.y)).ToArray();

            var shelter = (layout.objects ?? Array.Empty<Obj>()).FirstOrDefault(o => o.role == "building:herd_shelter");
            var gameplay = new GameObject("FarmGameplay").AddComponent<PixelMythFarmGameplay>();
            gameplay.MapRect = new Rect(0, 0, w, h);
            gameplay.FenceRect = inner;
            gameplay.Refuge = shelter != null ? new Vector2(shelter.x, shelter.y - 1.35f) : inner.center;
            // every ground monster walks the outer lane (9 m outside the fence line) to one breach on the south fence
            gameplay.LaneOffset = 1.5f + 9f;
            gameplay.Breach = new Vector2(fence.center.x, fence.yMin - 3f);
            gameplay.EnemyEntries = (layout.enemy_entries ?? Array.Empty<EnemyEntry>())
                .Select(e => new PixelMythFarmGameplay.Entry
                {
                    Position = new Vector2(e.x, e.y),
                    Sector = e.sector switch { "N" => PigTycoon.Core.ApproachSector.North, "E" => PigTycoon.Core.ApproachSector.East,
                        "S" => PigTycoon.Core.ApproachSector.South, _ => PigTycoon.Core.ApproachSector.West },
                }).ToArray();
            Debug.Log($"[PixelMyth] farm gameplay: fence {inner}, {gameplay.EnemyEntries.Length} enemy entries, refuge {gameplay.Refuge}");

            // GDD 21: Bí nhân appears on the outer trail, outside the fence and within 30 m of it, only at night
            var merchant = new GameObject("BiNhan_MysticMerchant").AddComponent<MysticMerchantView>();
            merchant.Spots = new[]
            {
                new Vector2(fence.xMin - 8.5f, fence.yMax - 14f), new Vector2(fence.xMin - 8.5f, fence.yMin + 26f),
                new Vector2(fence.xMax + 8.5f, fence.center.y), new Vector2(fence.xMax + 8.5f, fence.yMin + 16f),
            };
        }

        /// <summary>
        /// What each station does. The defaults of CustomBuildingController load "MainFarm2D" (not in the build) and
        /// open Bà Sáu's shop at every counter; every station gets its own target here. Village craftsmen open their
        /// own shops (VillageEconomy). Town counters without a dedicated GDD market screen yet use the nearest shop.
        /// </summary>
        private static void ConfigureStation(CustomBuildingController s, string id)
        {
            void Lore(string title, string text)
            {
                s.AssignedFunction = BuildingInteractionFunction.DocBangTinLore;
                s.ActionPromptText = "Xem";
                s.LoreTitle = title;
                s.LoreContent = text;
            }
            void Shop(PigTycoon.Core.VillagerId who)
            {
                s.AssignedFunction = BuildingInteractionFunction.GiaoDichDanLang;
                s.ActionPromptText = "Giao dịch";
                s.TargetVillager = who;
            }
            s.BuildingId = id;
            s.BuildingDisplayName = id;
            switch (id)
            {
                case "main_house":
                    s.AssignedFunction = BuildingInteractionFunction.NghiNgoiGiuong;
                    s.ActionPromptText = "Nghỉ";
                    s.BuildingDisplayName = "Nhà chính";
                    break;
                case "herd_shelter": Lore("Nơi Trú Đàn", "Thổi tù và (nút tù và hoặc phím G) để lùa đàn về đây khi có đợt quái."); break;
                case "construction_hall":
                    s.AssignedFunction = BuildingInteractionFunction.PhongXayDung;
                    s.ActionPromptText = "Xây dựng";
                    s.BuildingDisplayName = "Phòng Xây Dựng";
                    break;
                case CustomBuildingController.IntelStationId:
                    // GDD 23: the farm's info station is the Bàn Tình Báo (IntelPopup)
                    s.AssignedFunction = BuildingInteractionFunction.BanTinhBao;
                    s.ActionPromptText = "Tình báo";
                    s.BuildingDisplayName = "Bàn Tình Báo";
                    break;
                case "research_lab":
                case "research_house": Lore("Phòng Nghiên Cứu", "Bàn giám định gen và sổ ghi chép dòng heo."); break;
                case "nha_tam_va": Shop(PigTycoon.Core.VillagerId.TamVa); break;
                case "nha_dung": Shop(PigTycoon.Core.VillagerId.CoDung); break;
                case "nha_anh_ca": Shop(PigTycoon.Core.VillagerId.AnhCa); break;
                case "nha_ong_ton": Shop(PigTycoon.Core.VillagerId.OngTon); break;
                case "quan_ba_sau": Shop(PigTycoon.Core.VillagerId.BaSau); break;
                case "nha_man": Lore("Nhà Mận", "Nhà của Mận, nông công."); break;
                case "nha_cuc": Lore("Nhà Cúc", "Nhà của Cúc, nhà nghiên cứu gen."); break;
                case "nha_le": Lore("Nhà Lệ", "Nhà của Lệ, nhà buôn heo."); break;
                case "nha_van_hoa": Lore("Nhà Văn Hóa", "Nhà văn hóa của làng Bờ Rìa."); break;
                case "hoi_cho": Lore("Hội Chợ", "Sảnh hội chợ của thị trấn."); break;
                case "kiem_dich_sang_loc": Shop(PigTycoon.Core.VillagerId.CoDung); break;
                case "giam_dinh":
                    s.AssignedFunction = BuildingInteractionFunction.GiamDinhGen;
                    s.ActionPromptText = "Giám định";
                    break;
                case "vat_tu": Shop(PigTycoon.Core.VillagerId.TamVa); break;
                // GDD 20: night counters trade the day's night-market slots (18% take gold); the auction runs there too
                case "cho_dem_1": case "cho_dem_2": case "cho_dem_3":
                    Shop(PigTycoon.Core.VillagerId.ChoDem); break;
                case "cho_dem_4":
                    s.AssignedFunction = BuildingInteractionFunction.DoiThuCung;
                    s.ActionPromptText = "Đổi thú cưng";
                    s.BuildingDisplayName = "Quầy Thú Cưng (chợ đêm)";
                    break;
                case "dau_gia_dem":
                    s.AssignedFunction = BuildingInteractionFunction.DauGiaDem;
                    s.ActionPromptText = "Đấu giá";
                    s.BuildingDisplayName = "Nhà Đấu Giá Đêm";
                    break;
                case "tuyen_npc":
                    s.AssignedFunction = BuildingInteractionFunction.TuyenNhanSu;
                    s.ActionPromptText = "Tuyển người";
                    break;
                case "thue_linh_doi_thu_cung":
                    s.AssignedFunction = BuildingInteractionFunction.ThueLinhDanhThue;
                    s.ActionPromptText = "Thuê lính";
                    break;
                default:
                    if (id.StartsWith("site_"))
                    {
                        // forest event site: resolve the running event here (Core OccultEncounterDirector)
                        s.AssignedFunction = BuildingInteractionFunction.SuKienRung;
                        s.ActionPromptText = "Hành động";
                    }
                    else if (id.StartsWith("cho_ngay"))
                    {
                        // GDD 20: the day market buys pigs for gold
                        s.AssignedFunction = BuildingInteractionFunction.ChoNgayBanHeo;
                        s.ActionPromptText = "Bán heo";
                    }
                    else if (s.AssignedFunction == BuildingInteractionFunction.VaoNhaScene) Lore(id, "");
                    break;
            }
        }

        private static Camera BuildCamera(int w, int h)
        {
            var go = new GameObject("Main Camera");
            go.tag = "MainCamera";
            var cam = go.AddComponent<Camera>();
            cam.orthographic = true;
            cam.orthographicSize = 360f / 2f / 32f;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color32(42, 26, 18, 255);
            go.transform.position = new Vector3(w / 2f, h / 2f, -10f);
            go.AddComponent<UniversalAdditionalCameraData>();
            var ppc = go.AddComponent<PixelPerfectCamera>();
            ppc.assetsPPU = 32;
            ppc.refResolutionX = 640;
            ppc.refResolutionY = 360;
            ppc.gridSnapping = PixelPerfectCamera.GridSnapping.PixelSnapping;
            go.AddComponent<WeatherFx2D>();   // GDD 7.2 rain / fog / lightning follow the camera
            var follow = go.AddComponent<CameraFollow2D>();
            follow.TargetOrthoSize = follow.MinOrthoSize = follow.MaxOrthoSize = cam.orthographicSize;
            follow.UseWorldBounds = true;
            follow.ClampToFarmBounds = true;
            follow.WorldBounds = new Rect(0f, 0f, w, h);
            go.AddComponent<AudioListener>();
            return cam;
        }

        private static void BuildLight()
        {
            var go = new GameObject("Global Light 2D");
            var light = go.AddComponent<Light2D>();
            light.lightType = Light2D.LightType.Global;
            light.intensity = 1f;
            light.color = new Color(1f, 0.95f, 0.86f);
            var dn = new GameObject("DayNight").AddComponent<DayNight2DController>();
            dn.GlobalLight2D = light;
        }

        private static Transform BuildPlayers(Vector2 spawn)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PlayerRig);
            if (prefab == null)
            {
                Debug.LogWarning($"[PixelMyth] player rig missing at {PlayerRig}");
                return null;
            }
            var rig = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            PrefabUtility.UnpackPrefabInstance(rig, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
            Transform first = null;
            var khoaLib = AssetDatabase.LoadAssetAtPath<PixelMythSpriteLibrary>($"{PixelMythLibraryBuilder.LibrariesRoot}/khoa.asset");
            var anLib = AssetDatabase.LoadAssetAtPath<PixelMythSpriteLibrary>($"{PixelMythLibraryBuilder.LibrariesRoot}/an.asset");
            foreach (var pc in rig.GetComponentsInChildren<PlayerMobileController>(true))
            {
                pc.transform.position = new Vector3(spawn.x, spawn.y, 0f);
                var lib = pc.name.IndexOf("Khoa", StringComparison.OrdinalIgnoreCase) >= 0 ? khoaLib
                    : pc.name.IndexOf("_An", StringComparison.OrdinalIgnoreCase) >= 0 ? anLib : null;
                if (lib != null)
                {
                    var anim = pc.GetComponent<CharacterSpriteAnimator>() ?? pc.gameObject.AddComponent<CharacterSpriteAnimator>();
                    anim.MythLibrary = lib;
                    var sr = pc.GetComponent<SpriteRenderer>();
                    var idle = lib.Get("idle");
                    if (sr != null && idle != null && idle.Down.Length > 0) sr.sprite = idle.Down[0];
                    pc.transform.localScale = Vector3.one;
                }
                if (first == null) first = pc.transform;
            }
            return first;
        }

        /// <summary>Separate Unity job after BuildAll. Mono + no stripping: the IL2CPP/UnityLinker path hit a
        /// CET host error on this machine (Codex, 30 Oct); this is the configuration that built and ran.</summary>
        public static void BuildWindows()
        {
            var scenes = EditorBuildSettings.scenes.Where(s => s.enabled).Select(s => s.path).ToArray();
            if (scenes.Length == 0) throw new InvalidOperationException("Run BuildAll first.");
            PlayerSettings.SetScriptingBackend(UnityEditor.Build.NamedBuildTarget.Standalone, ScriptingImplementation.Mono2x);
            PlayerSettings.SetManagedStrippingLevel(UnityEditor.Build.NamedBuildTarget.Standalone, ManagedStrippingLevel.Disabled);
            PlayerSettings.fullScreenMode = FullScreenMode.Windowed;
            // keep simulating without focus: the recording player runs with a hidden window and paused after
            // frame 1 (0 % CPU) whenever another app had focus (31 Oct runs 02:03-03:30)
            PlayerSettings.runInBackground = true;
            PlayerSettings.visibleInBackground = true;
            PlayerSettings.defaultScreenWidth = 1280;
            PlayerSettings.defaultScreenHeight = 720;
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            Directory.CreateDirectory("Builds/PixelMyth");
            var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
            {
                scenes = scenes,
                locationPathName = "Builds/PixelMyth/PigTycoon.exe",
                target = BuildTarget.StandaloneWindows64,
                options = BuildOptions.Development,
            });
            Debug.Log($"[PixelMyth] Windows build {report.summary.result} {report.summary.totalSize} bytes");
            if (report.summary.result != UnityEditor.Build.Reporting.BuildResult.Succeeded) EditorApplication.Exit(1);
        }

        /// <summary>
        /// Windows build to hand to players: same scenes as <see cref="BuildWindows"/> but NOT a Development build
        /// (no "Development Build" watermark, no dev console, DevModePopup stays locked). The probe/video pipeline keeps
        /// using the development build in Builds/PixelMyth; the package script copies from Builds/PixelMythRelease.
        /// </summary>
        [MenuItem("Pig Tycoon/Pixel Myth/Build Windows (release)")]
        public static void BuildWindowsRelease()
        {
            var scenes = EditorBuildSettings.scenes.Where(s => s.enabled && File.Exists(s.path)).Select(s => s.path).ToArray();
            if (scenes.Length == 0) { Debug.LogError("[PixelMyth] no enabled scenes"); EditorApplication.Exit(1); return; }
            // same player configuration as BuildWindows (Mono, no stripping: the configuration that builds and runs here)
            PlayerSettings.SetScriptingBackend(UnityEditor.Build.NamedBuildTarget.Standalone, ScriptingImplementation.Mono2x);
            PlayerSettings.SetManagedStrippingLevel(UnityEditor.Build.NamedBuildTarget.Standalone, ManagedStrippingLevel.Disabled);
            PlayerSettings.fullScreenMode = FullScreenMode.FullScreenWindow; // players: borderless full screen (Alt+Enter toggles)
            PlayerSettings.runInBackground = false;
            PlayerSettings.bundleVersion = "0.12.2";
            PlayerSettings.defaultScreenWidth = 1920;
            PlayerSettings.defaultScreenHeight = 1080;
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            Directory.CreateDirectory("Builds/PixelMythRelease");
            var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
            {
                scenes = scenes,
                locationPathName = "Builds/PixelMythRelease/PigTycoon.exe",
                target = BuildTarget.StandaloneWindows64,
                options = BuildOptions.CompressWithLz4HC, // 4 Nov: 2.1 GB of uncompressed pixel sheets; LZ4HC shrinks disk size without touching pixels
            });
            Debug.Log($"[PixelMyth] Windows RELEASE build {report.summary.result} {report.summary.totalSize} bytes");
            if (report.summary.result != UnityEditor.Build.Reporting.BuildResult.Succeeded) EditorApplication.Exit(1);
        }

        /// <summary>Export Unity 6000.6.3f1 Android unityLibrary for the Flutter host (Route A: ARM64 IL2CPP, OpenGLES3 only, Activity entry).</summary>
        [MenuItem("Pig Tycoon/Pixel Myth/Export Android unityLibrary")]
        public static void ExportAndroid()
        {
            if (Application.unityVersion != "6000.6.3f1")
                throw new UnityEditor.Build.BuildFailedException("Pig Tycoon requires Unity 6000.6.3f1.");
#if UNITY_ANDROID
            if (EditorUserBuildSettings.activeBuildTarget != BuildTarget.Android)
                throw new UnityEditor.Build.BuildFailedException("Start the pinned Editor with -buildTarget Android before exporting.");
            var scenes = EditorBuildSettings.scenes.Where(s => s.enabled && File.Exists(s.path)).Select(s => s.path).ToArray();
            if (scenes.Length == 0)
                throw new UnityEditor.Build.BuildFailedException("No enabled PixelMyth scenes found in EditorBuildSettings.");

            string module = Path.Combine(Path.GetDirectoryName(EditorApplication.applicationPath), "Data", "PlaybackEngines", "AndroidPlayer");
            string sdk = Environment.GetEnvironmentVariable("PIGTYCOON_ANDROID_SDK") ??
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Android", "Sdk");
            if (!Directory.Exists(Path.Combine(sdk, "platforms", "android-36")))
                throw new UnityEditor.Build.BuildFailedException("Android SDK platform 36 is required at " + sdk);

            UnityEditor.Android.AndroidExternalToolsSettings.sdkRootPath = sdk;
            UnityEditor.Android.AndroidExternalToolsSettings.jdkRootPath = Path.Combine(module, "OpenJDK");
            UnityEditor.Android.AndroidExternalToolsSettings.ndkRootPath = Path.Combine(module, "NDK");

            PlayerSettings.Android.minSdkVersion = AndroidSdkVersions.AndroidApiLevel26;
            PlayerSettings.Android.targetSdkVersion = (AndroidSdkVersions)36;
            PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;
            PlayerSettings.Android.applicationEntry = AndroidApplicationEntry.Activity;
            PlayerSettings.defaultInterfaceOrientation = UIOrientation.LandscapeLeft;
            PlayerSettings.SetApplicationIdentifier(UnityEditor.Build.NamedBuildTarget.Android, "com.pigtycoon.core");
            PlayerSettings.SetScriptingBackend(UnityEditor.Build.NamedBuildTarget.Android, ScriptingImplementation.IL2CPP);
            PlayerSettings.SetManagedStrippingLevel(UnityEditor.Build.NamedBuildTarget.Android, ManagedStrippingLevel.Minimal);
            // test builds: IL2CPP "Faster (smaller) builds" generates far less C++ (generic sharing), so the incremental
            // Bee build after a script change takes minutes instead of ~40 (4 Nov user ask)
            PlayerSettings.SetIl2CppCodeGeneration(UnityEditor.Build.NamedBuildTarget.Android, UnityEditor.Build.Il2CppCodeGeneration.OptimizeSize);
            PlayerSettings.SetIl2CppCompilerConfiguration(UnityEditor.Build.NamedBuildTarget.Android, Il2CppCompilerConfiguration.Release);

            // R3 Route A baseline for AVD libndk_translation + GLES translator: OpenGLES3 only, Graphics Jobs off.
            PlayerSettings.SetUseDefaultGraphicsAPIs(BuildTarget.Android, false);
            PlayerSettings.SetGraphicsAPIs(BuildTarget.Android, new[] { UnityEngine.Rendering.GraphicsDeviceType.OpenGLES3 });
            PlayerSettings.graphicsJobs = false;

            // Android players read art only from Resources: pack the UI kit, icons and every runtime sheet (as AndroidBuildPipeline does)
            MythHudPackaging.PackageRuntimeArt();
            EditorUserBuildSettings.exportAsGoogleAndroidProject = true;
            EditorUserBuildSettings.buildAppBundle = false;
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            EditorUtility.UnloadUnusedAssetsImmediate();
            GC.Collect();

            const string exportDir = "Builds/AndroidExport";
            if (Directory.Exists(exportDir)) Directory.Delete(exportDir, true);
            Directory.CreateDirectory(exportDir);

            var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
            {
                scenes = scenes,
                locationPathName = exportDir,
                target = BuildTarget.Android,
                options = BuildOptions.None,
            });
            Debug.Log($"[PixelMyth] Android unityLibrary export {report.summary.result} errors={report.summary.totalErrors}");
            if (report.summary.result != UnityEditor.Build.Reporting.BuildResult.Succeeded ||
                !File.Exists("Builds/AndroidExport/unityLibrary/build.gradle"))
            {
                throw new UnityEditor.Build.BuildFailedException("Unity Android library export failed: " + report.summary.result);
            }
#else
            throw new UnityEditor.Build.BuildFailedException("Start this export job with -buildTarget Android and installed Android support.");
#endif
        }

        private static void RegisterBuildScenes(List<string> scenes)
        {
            var ordered = scenes.OrderBy(s => s.EndsWith("/Farm_Main.unity") ? 0 : 1).ThenBy(s => s).ToList();
            EditorBuildSettings.scenes = ordered.Select(s => new EditorBuildSettingsScene(s, true)).ToArray();
        }
    }
}
