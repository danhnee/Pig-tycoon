using System;
using System.Collections.Generic;
using UnityEngine;
using PigTycoon.Core;

namespace PigTycoon.Controllers
{
    /// <summary>
    /// Xây dựng và quản lý bản đồ 3D Farm_Main theo đúng bố cục Hình 1 và Camera Design v1.0:
    /// 1. Vùng chơi thật (Playable 3D):
    ///    - Cánh đồng cỏ hoàng hôn vàng hổ phách (120m x 90m), 3 ao nước phản chiếu, di tích đá cổ khắc nhật nguyệt,
    ///      cây thần thoại phát sáng ánh vàng.
    ///    - Khu chuồng trại (bên trái), Nhà xây dựng (phía trên chuồng), Nhà nghiên cứu có kính thiên văn (giữa-trái),
    ///      Kho & Nhà nấu cám, Trạm thông tin có quả cầu pha lê lam phát sáng (bên phải gần bờ sông).
    /// 2. Giới hạn vùng thả heo rông <= 1/3 diện tích đồng cỏ gần Khu chuồng trại:
    ///    - Heo thả rông (cả trong chuồng và bãi cỏ sát chuồng) chỉ đi lại trong tối đa 1/3 diện tích đồng cỏ sát trại;
    ///      2/3 diện tích đồng cỏ còn lại giữ thoáng cho xây dựng, phòng thủ và chiến đấu.
    /// 3. Vùng bối cảnh xa (Distant 3D Vista — dựng bằng Model 3D thật để nhìn từ xa, không đi bộ trực tiếp từ Farm_Main):
    ///    - Khu NPC ở cuối đường phía sau trang trại.
    ///    - Sông, cầu đá nhiều nhịp, Chợ / Thị trấn bên phải, thác nước, dãy núi và Lâu đài hoàng hôn phía xa.
    /// 4. Quy tắc điểm chuyển map (Map Transition):
    ///    - Từ Farm_Main CHỈ CÓ DUY NHẤT 1 điểm chuyển map dẫn tới Khu NPC (cuối đường mòn phía sau trại).
    ///    - Muốn tới Chợ / Thị trấn bắt buộc phải qua điểm chuyển map tới Khu NPC trước, rồi từ Khu NPC mới qua điểm chuyển sang Chợ / Thị trấn.
    /// </summary>
    public class FarmMain3DWorldBuilder : MonoBehaviour
    {
        [Header("Core System Binding")]
        public PigTycoonCameraController3D cameraController3D;
        public bool autoBuildOnAwake = true;

        [Header("Optional Pre-Imported 3D Prefabs (Blender / Tripo 3D)")]
        public GameObject fullEnvironment3DPrefab;
        public GameObject mainBarnPrefab;
        public GameObject constructionHallPrefab;
        public GameObject researchObservatoryPrefab;
        public GameObject storageBarnPrefab;
        public GameObject infoBeaconPrefab;
        public GameObject distantNpcVillagePrefab;
        public GameObject distantMarketAndBridgePrefab;
        public GameObject distantCastleWaterfallPrefab;
        public GameObject pig3DPrefab;

        public CameraAndWorldLayoutSystem WorldLayout { get; private set; } = new CameraAndWorldLayoutSystem();
        public Transform PlayableRoot { get; private set; }
        public Transform DistantVistaRoot { get; private set; }
        public Transform PigHerdRoot { get; private set; }
        public Transform PortalKhuNpcTransform { get; private set; }

        private readonly List<Transform> spawnedPigTransforms = new List<Transform>();
        private readonly List<Vector3> pigRoamTargets = new List<Vector3>();

        private void Awake()
        {
            if (autoBuildOnAwake)
            {
                BuildComplete3DWorld();
            }
        }

        public void BuildComplete3DWorld()
        {
            if (PlayableRoot != null) return;

            GameObject playableGo = new GameObject("01_Playable_FarmMain_3D");
            playableGo.transform.SetParent(transform, false);
            PlayableRoot = playableGo.transform;

            GameObject vistaGo = new GameObject("02_DistantVista_3D_Models_NonWalkable");
            vistaGo.transform.SetParent(transform, false);
            DistantVistaRoot = vistaGo.transform;

            GameObject pigsGo = new GameObject("03_FreeRoaming_Pigs_1Third_PastureZone");
            pigsGo.transform.SetParent(transform, false);
            PigHerdRoot = pigsGo.transform;

            BuildGoldenSunsetLightingAndAtmosphere();
            BuildGoldenMeadowTerrainAndPonds();
            BuildPlayableBuildingsAndRuins();
            BuildDistant3DVistaModels();
            BuildSingleMapTransitionPortalToKhuNpc();
            SpawnFreeRoamingPigsInOneThirdPasture(14);
        }

        private void BuildGoldenSunsetLightingAndAtmosphere()
        {
            RenderSettings.ambientLight = new Color(0.42f, 0.34f, 0.48f); // Tím lam chiều tà ở vùng tối
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.ExponentialSquared;
            RenderSettings.fogColor = new Color(0.95f, 0.68f, 0.45f); // Sương hoàng hôn cam vàng ấm
            RenderSettings.fogDensity = 0.0045f;

            Light sun = FindFirstObjectByType<Light>();
            if (sun == null)
            {
                GameObject sunGo = new GameObject("Sunset_DirectionalLight_GoldenHour");
                sunGo.transform.SetParent(transform, false);
                sun = sunGo.AddComponent<Light>();
                sun.type = LightType.Directional;
            }
            sun.color = new Color(1.0f, 0.78f, 0.48f);
            sun.intensity = 1.45f;
            sun.transform.rotation = Quaternion.Euler(19f, -145f, 0f);
        }

        private void BuildGoldenMeadowTerrainAndPonds()
        {
            // Địa hình đồng cỏ vàng hổ phách (120m x 90m)
            GameObject meadow = GameObject.CreatePrimitive(PrimitiveType.Cube);
            meadow.name = "GoldenAmber_Meadow_Terrain_120x90m";
            meadow.transform.SetParent(PlayableRoot, false);
            meadow.transform.localPosition = new Vector3(0f, -0.5f, 0f);
            meadow.transform.localScale = new Vector3(120f, 1.0f, 90f);
            ApplyColorMaterial(meadow, new Color(0.86f, 0.64f, 0.22f), 0.15f);

            // Ranh giới trực quan vùng 1/3 đồng cỏ thả heo sát Khu chuồng trại
            GameObject pastureGround = GameObject.CreatePrimitive(PrimitiveType.Cube);
            pastureGround.name = "PigPasture_OneThird_MeadowZone_NearBarn";
            pastureGround.transform.SetParent(PlayableRoot, false);
            float cx = (WorldLayout.PigPastureMinX + WorldLayout.PigPastureMaxX) * 0.5f;
            float cz = (WorldLayout.PigPastureMinZ + WorldLayout.PigPastureMaxZ) * 0.5f;
            float sx = WorldLayout.PigPastureMaxX - WorldLayout.PigPastureMinX;
            float sz = WorldLayout.PigPastureMaxZ - WorldLayout.PigPastureMinZ;
            pastureGround.transform.localPosition = new Vector3(cx, 0.02f, cz);
            pastureGround.transform.localScale = new Vector3(sx, 0.04f, sz);
            ApplyColorMaterial(pastureGround, new Color(0.89f, 0.69f, 0.26f), 0.2f);
        }

        private void BuildPlayableBuildingsAndRuins()
        {
            for (int i = 0; i < WorldLayout.Landmarks3D.Count; i++)
            {
                var spec = WorldLayout.Landmarks3D[i];
                if (spec.LayerType != EnvironmentLayerType3D.PlayableInteractive3D) continue;

                GameObject node = InstantiateOrCreateLandmark3D(spec, PlayableRoot, includeCollider: true);
                node.transform.localPosition = new Vector3(spec.WorldX, spec.WorldY * 0.5f, spec.WorldZ);
            }
        }

        private void BuildDistant3DVistaModels()
        {
            for (int i = 0; i < WorldLayout.Landmarks3D.Count; i++)
            {
                var spec = WorldLayout.Landmarks3D[i];
                if (spec.LayerType != EnvironmentLayerType3D.DistantVista3D) continue;

                // Bắt buộc dựng bằng Model 3D thật để nhìn từ xa, nhưng tắt Collider đi lại trực tiếp từ Farm_Main
                GameObject vistaModel = InstantiateOrCreateLandmark3D(spec, DistantVistaRoot, includeCollider: false);
                vistaModel.transform.localPosition = new Vector3(spec.WorldX, spec.WorldY, spec.WorldZ);
            }

            // Dòng sông 3D ngăn cách vùng chơi thật (Trạm thông tin) với Chợ / Thị trấn bên phải
            GameObject river3D = GameObject.CreatePrimitive(PrimitiveType.Cube);
            river3D.name = "River_And_Waterfall_Basin_3D";
            river3D.transform.SetParent(DistantVistaRoot, false);
            river3D.transform.localPosition = new Vector3(50f, -0.2f, 20f);
            river3D.transform.localScale = new Vector3(26f, 0.4f, 140f);
            ApplyColorMaterial(river3D, new Color(0.24f, 0.52f, 0.76f), 0.85f);
        }

        private void BuildSingleMapTransitionPortalToKhuNpc()
        {
            var farmPortals = WorldLayout.GetPortalsInZone(WorldMapZoneId.FarmMain);
            for (int i = 0; i < farmPortals.Count; i++)
            {
                var p = farmPortals[i];
                GameObject portalGo = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                portalGo.name = p.PortalId;
                portalGo.transform.SetParent(PlayableRoot, false);
                portalGo.transform.localPosition = new Vector3(p.WorldX, 0.5f, p.WorldZ);
                portalGo.transform.localScale = new Vector3(p.TriggerRadiusMeters, 0.5f, p.TriggerRadiusMeters);
                ApplyColorMaterial(portalGo, new Color(0.35f, 0.82f, 0.98f), 0.75f);

                Collider col = portalGo.GetComponent<Collider>();
                if (col != null) col.isTrigger = true;

                if (p.ToZone == WorldMapZoneId.KhuNPC)
                {
                    PortalKhuNpcTransform = portalGo.transform;
                }
            }
        }

        public void SpawnFreeRoamingPigsInOneThirdPasture(int count)
        {
            spawnedPigTransforms.Clear();
            pigRoamTargets.Clear();

            System.Random rng = new System.Random(2026);
            for (int i = 0; i < count; i++)
            {
                float rx = Mathf.Lerp(WorldLayout.PigPastureMinX + 3f, WorldLayout.PigPastureMaxX - 3f, (float)rng.NextDouble());
                float rz = Mathf.Lerp(WorldLayout.PigPastureMinZ + 3f, WorldLayout.PigPastureMaxZ - 3f, (float)rng.NextDouble());
                var (clampedX, clampedZ) = WorldLayout.ClampPigPositionToAllowedPasture(rx, rz);

                GameObject pigGo;
                if (pig3DPrefab != null)
                {
                    pigGo = Instantiate(pig3DPrefab, PigHerdRoot);
                }
                else
                {
                    pigGo = GameObject.CreatePrimitive(PrimitiveType.Capsule);
                    pigGo.transform.SetParent(PigHerdRoot, false);
                    pigGo.transform.localScale = new Vector3(0.75f, 0.55f, 1.15f);
                    ApplyColorMaterial(pigGo, new Color(0.98f, 0.72f, 0.68f), 0.35f);
                }

                pigGo.name = $"Pig3D_OneThirdPasture_{i + 1:00}";
                pigGo.transform.localPosition = new Vector3(clampedX, 0.55f, clampedZ);
                spawnedPigTransforms.Add(pigGo.transform);
                pigRoamTargets.Add(pigGo.transform.localPosition);
            }
        }

        private void Update()
        {
            float dt = Time.deltaTime;
            for (int i = 0; i < spawnedPigTransforms.Count; i++)
            {
                Transform pig = spawnedPigTransforms[i];
                if (pig == null) continue;

                Vector3 target = pigRoamTargets[i];
                Vector3 pos = Vector3.MoveTowards(pig.localPosition, target, 1.4f * dt);

                // Luôn kẹp cứng trong vùng <= 1/3 diện tích đồng cỏ sát Khu chuồng trại
                var (cx, cz) = WorldLayout.ClampPigPositionToAllowedPasture(pos.x, pos.z);
                pig.localPosition = new Vector3(cx, pos.y, cz);

                if (Vector3.Distance(pig.localPosition, target) < 0.35f)
                {
                    float nx = UnityEngine.Random.Range(WorldLayout.PigPastureMinX + 2f, WorldLayout.PigPastureMaxX - 2f);
                    float nz = UnityEngine.Random.Range(WorldLayout.PigPastureMinZ + 2f, WorldLayout.PigPastureMaxZ - 2f);
                    var (tx, tz) = WorldLayout.ClampPigPositionToAllowedPasture(nx, nz);
                    pigRoamTargets[i] = new Vector3(tx, pos.y, tz);
                }
            }
        }

        private GameObject InstantiateOrCreateLandmark3D(WorldLandmark3DSpec spec, Transform parent, bool includeCollider)
        {
            GameObject prefab = ResolvePrefabForLandmark(spec.Id);
            GameObject instance;
            if (prefab != null)
            {
                instance = Instantiate(prefab, parent);
                instance.name = spec.Id;
            }
            else
            {
                instance = GameObject.CreatePrimitive(PrimitiveType.Cube);
                instance.name = spec.Id;
                instance.transform.SetParent(parent, false);
                instance.transform.localScale = new Vector3(
                    Mathf.Max(2f, spec.FootprintWidthMeters),
                    Mathf.Max(0.4f, spec.WorldY * 2.0f),
                    Mathf.Max(2f, spec.FootprintDepthMeters));

                Color col = spec.LayerType == EnvironmentLayerType3D.DistantVista3D
                    ? new Color(0.68f, 0.48f, 0.36f)
                    : new Color(0.62f, 0.42f, 0.25f);
                if (spec.Id.Contains("AoNuoc"))
                {
                    col = new Color(0.32f, 0.58f, 0.78f);
                }
                else if (spec.Id == "NhaNghienCuu")
                {
                    col = new Color(0.24f, 0.38f, 0.68f);
                }
                else if (spec.Id == "TramThongTin")
                {
                    col = new Color(0.25f, 0.68f, 0.98f);
                }
                ApplyColorMaterial(instance, col, 0.4f);
            }

            if (!includeCollider)
            {
                Collider[] cols = instance.GetComponentsInChildren<Collider>();
                for (int c = 0; c < cols.Length; c++)
                {
                    Destroy(cols[c]);
                }
            }

            return instance;
        }

        private GameObject ResolvePrefabForLandmark(string id)
        {
            return id switch
            {
                "KhuChuongTrai" => mainBarnPrefab,
                "NhaXayDung" => constructionHallPrefab,
                "NhaNghienCuu" => researchObservatoryPrefab,
                "KhoVaNhaBep" => storageBarnPrefab,
                "TramThongTin" => infoBeaconPrefab,
                "KhuNPC_Vista3D" => distantNpcVillagePrefab,
                "ChoThiTran_Vista3D" => distantMarketAndBridgePrefab,
                "LauDaiVaThacNuoc_Vista3D" => distantCastleWaterfallPrefab,
                _ => null
            };
        }

        private static void ApplyColorMaterial(GameObject go, Color color, float smoothness)
        {
            Renderer rend = go.GetComponent<Renderer>();
            if (rend == null) return;
            Shader sh = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
            if (sh == null) return;
            Material mat = new Material(sh);
            if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", color);
            if (mat.HasProperty("_Color")) mat.color = color;
            if (mat.HasProperty("_Smoothness")) mat.SetFloat("_Smoothness", smoothness);
            rend.sharedMaterial = mat;
        }
    }
}
