using System;
using System.Linq;
using PigTycoon.Core;
using UnityEngine;

namespace PigTycoon.Presentation
{
    /// <summary>
    /// Farm_Main (PixelMyth): ties the map layout to the Core farm rules. The scene builder serializes the map and
    /// fence rectangles, the refuge point and the layout's "enemy_entries"; at runtime this hands Core an explicit
    /// <see cref="PlayableMapConfiguration"/> (GDD 3.6–3.8: 30 m service radius from the real troughs/basins/shelter,
    /// refuge, 8–12 entry points in four sectors) instead of the legacy 90×64 m grid around (0,0), and issues the
    /// "Lùa về Nơi Trú" command (GDD 3.7) to the pig views.
    /// </summary>
    public sealed class PixelMythFarmGameplay : MonoBehaviour
    {
        public const string ConfigurationId = "pixelmyth_farm_v1";

        [Serializable]
        public struct Entry
        {
            public Vector2 Position;
            public ApproachSector Sector;
        }

        public static PixelMythFarmGameplay Instance { get; private set; }

        public Rect MapRect = new Rect(0, 0, 184, 160);
        public Rect FenceRect = new Rect(32, 34, 120, 90);
        public Vector2 Refuge = new Vector2(92, 96);
        public Entry[] EnemyEntries = Array.Empty<Entry>();
        /// <summary>The one point every ground monster converges on before attacking (south fence, outside).</summary>
        public Vector2 Breach = new Vector2(92, 31);
        public float LaneOffset = 9f;

        private bool configured;

        private void Awake() => Instance = this;

        private void Start()
        {
            // GDD 13 building on this farm: the blueprint menu (P/H, or the Construction Hall) and grid placement
            if (DefenseBuildController.Instance == null) new GameObject("DefenseBuildController").AddComponent<DefenseBuildController>();
            if (DefenseBuildPopup.Instance == null)
            {
                var canvas = VillageBarterPopup.FindScreenSpaceCanvas();
                if (canvas != null) canvas.gameObject.AddComponent<DefenseBuildPopup>();
            }
        }
        private void OnDestroy() { if (Instance == this) Instance = null; }

        private void Update()
        {
            if (!configured) TryConfigure();
        }

        /// <summary>
        /// GDD 8.1 Khoa "Tiếng Huýt Chăn Đàn": Core chọn tối đa 12 heo trong 20 m (hồi chiêu 2 giờ game, không tốn
        /// Thể lực); các heo đó đi theo Khoa 60 giây thực. Không phải lệnh lùa về Nơi Trú.
        /// </summary>
        public string KhoaWhistle(Transform khoa)
        {
            var engine = MobileGameController.Instance != null ? MobileGameController.Instance.Engine : null;
            if (engine == null || khoa == null) return "Chưa sẵn sàng.";
            var res = ProtagonistTraits.KhoaWhistle(engine, khoa.position.x, khoa.position.y);
            if (!res.success) return res.message;
            var chosen = new System.Collections.Generic.HashSet<string>(res.pigIds);
            int following = 0;
            foreach (var pig in FindObjectsByType<PigAgentView>().Where(p => p != null && p.isActiveAndEnabled))
            {
                if (pig.PigModel == null || !chosen.Contains(pig.PigModel.Id)) continue;
                pig.CommandFollowLeader(khoa, ProtagonistTraits.WhistleFollowRealSeconds);
                following++;
            }
            return following == res.pigIds.Count ? res.message
                : $"Khoa huýt sáo: {following} heo đi theo trong {ProtagonistTraits.WhistleFollowRealSeconds:0} giây.";
        }

        private void TryConfigure()
        {
            var engine = MobileGameController.Instance != null ? MobileGameController.Instance.Engine : null;
            if (engine == null) return;
            configured = true;
            BindStarterTower(engine);
            SetUpStoryOpening(engine);
            // a save restored this map already: keep its grid (player-built structures live there)
            if (engine.PlayableMap != null && engine.PlayableMap.ConfigurationId == ConfigurationId)
            {
                engine.PlayableMap.InteractiveHerdOwnership = true; // older saves: the herd eats at the troughs now
                return;
            }

            var cfg = new PlayableMapConfiguration
            {
                ConfigurationId = ConfigurationId,
                MapX = Mathf.Floor(MapRect.x), MapY = Mathf.Floor(MapRect.y),
                MapWidth = EvenMeters(MapRect.width), MapHeight = EvenMeters(MapRect.height),
                PastureX = FenceRect.x, PastureY = FenceRect.y, PastureWidth = FenceRect.width, PastureHeight = FenceRect.height,
                RefugeX = Refuge.x, RefugeY = Refuge.y,
                // Pigs eat at the feeders the player fills; the warehouse no longer feeds them by itself.
                InteractiveHerdOwnership = true,
            };
            int n = 0;
            foreach (var f in FindObjectsByType<Feeder2DView>())
                cfg.ServicePoints.Add(new FarmServicePoint { Id = $"feeder_{n++}", X = f.transform.position.x, Y = f.transform.position.y, Feed = true, FeedKg = TroughCommands.FeederCapacityKg });
            foreach (var w in FindObjectsByType<WaterTrough2DView>())
                cfg.ServicePoints.Add(new FarmServicePoint { Id = $"basin_{n++}", X = w.transform.position.x, Y = w.transform.position.y, Water = true, WaterL = TroughCommands.BasinCapacityL });
            foreach (var s in FindObjectsByType<Shelter2DView>())
                cfg.ServicePoints.Add(new FarmServicePoint { Id = $"shelter_{n++}", X = s.transform.position.x, Y = s.transform.position.y, Shelter = true, Cooling = true });
            foreach (var m in FindObjectsByType<MudPit2DView>())
                cfg.ServicePoints.Add(new FarmServicePoint { Id = $"wallow_{n++}", X = m.transform.position.x, Y = m.transform.position.y, Cooling = true });
            foreach (var e in EnemyEntries)
                cfg.MonsterEntries.Add(new FarmApproachPoint { Sector = e.Sector, X = e.Position.x, Y = e.Position.y });
            try
            {
                engine.ConfigurePlayableMap(cfg);
                Debug.Log($"[FarmGameplay] Core map configured: {cfg.ServicePoints.Count} service points, {cfg.MonsterEntries.Count} enemy entries, refuge {Refuge}");
            }
            catch (ArgumentException ex)
            {
                Debug.LogError($"[FarmGameplay] map configuration rejected: {ex.Message}");
            }
        }

        // Story Q01 "Cậu tới nhận trại": the farm is handed over with one broken fence segment (west side, facing the
        // lane in). It stays broken until the player closes it with a temporary panel.
        private void SetUpStoryOpening(GameEngine engine)
        {
            if (engine.Quests.ActiveMain?.Id != "Q01" || engine.Quests.Has("Q01", QuestSignal.FenceTempPatched)) return;
            var fences = FindObjectsByType<PixelFenceSegment>();
            if (fences.Length == 0 || fences.Any(f => f.IsBroken)) return;
            var gap = new Vector2(FenceRect.xMin, FenceRect.center.y);
            fences.OrderBy(f => ((Vector2)f.transform.position - gap).sqrMagnitude).First().Break();
        }

        // The crossbow tower Core gives a new farm (MobileGameController) is the layout's tower_w: make it shoot.
        private static void BindStarterTower(GameEngine engine)
        {
            foreach (var sr in FindObjectsByType<SpriteRenderer>())
            {
                if (sr == null || !sr.gameObject.name.StartsWith("building:tower_")) continue;
                if (sr.GetComponent<DefenseTower2DView>() != null) return;
                // the Nỏ saved at this sprite's position, else the unplaced starter Nỏ (never a player-built one)
                Vector2 at = sr.transform.position;
                var model = engine.Defense.Buildings.Find(b => b.BlueprintId == "NoXuyenVan" && b.HasPosition &&
                                                               Vector2.Distance(new Vector2(b.PosX, b.PosY), at) < 1.5f)
                         ?? engine.Defense.Buildings.Find(b => b.BlueprintId == "NoXuyenVan" && !b.HasPosition);
                if (model == null) return;
                var view = sr.gameObject.AddComponent<DefenseTower2DView>();
                view.BaseSprite = sr;
                view.Bind(model);
                return;
            }
        }

        private static float EvenMeters(float v) => Mathf.Floor(v / FarmGridAndZones.BuildCellMeters) * FarmGridAndZones.BuildCellMeters;

        public Vector2 EntryJittered(Entry e) => e.Position + UnityEngine.Random.insideUnitCircle * 1.5f;

        /// <summary>GDD 3.7 "Lùa về Nơi Trú" (thổi tù và): Core resolves the recall (signals HerdRecalled), views walk there.
        /// Khoa's whistle (GDD 8.1) is a separate action: <see cref="KhoaWhistle"/>.</summary>
        public string RecallHerd(Vector3 playerPos)
        {
            var engine = MobileGameController.Instance != null ? MobileGameController.Instance.Engine : null;
            string msg = "Thổi tù và: lùa đàn về Nơi Trú.";
            if (engine != null)
            {
                msg = "Thổi tù và — " + engine.CommandHerdRecallToRefuge(khoaWhistle: false).message;
            }
            var shelter = FarmEnvironment2D.Instance != null ? FarmEnvironment2D.Instance.Shelter : null;
            Vector3 target = shelter != null ? (Vector3)shelter.GetShelterRestSpot() : (Vector3)Refuge;
            foreach (var pig in FindObjectsByType<PigAgentView>().Where(p => p != null && p.isActiveAndEnabled))
                pig.CommandRecallToShelter(target);
            StopCoroutine(nameof(WatchGather));
            StartCoroutine(nameof(WatchGather), (Vector2)target);
            return msg;
        }

        /// <summary>GDD C2: ≥ 90% of the herd at the refuge within 2 game hours (≈ 80 s) counts as a gathered herd.</summary>
        public static int HerdAtRefugeCount(Vector2 refuge, out int total)
        {
            var pigs = FindObjectsByType<PigAgentView>().Where(p => p != null && p.isActiveAndEnabled).ToList();
            total = pigs.Count;
            return pigs.Count(p => p.IsHeldAtRefuge && Vector2.Distance(p.transform.position, refuge) <= 8f);
        }

        private System.Collections.IEnumerator WatchGather(Vector2 refuge)
        {
            float until = Time.time + 80f;
            while (Time.time < until)
            {
                yield return new WaitForSeconds(1f);
                int here = HerdAtRefugeCount(refuge, out int total);
                if (total > 0 && here * 10 >= total * 9)
                {
                    MobileGameController.Instance?.Engine?.SignalQuest(QuestSignal.HerdGathered90);
                    PlayerInteractionController.Instance?.ShowFeedback($"Đàn đã về Nơi Trú: {here}/{total}.");
                    yield break;
                }
            }
        }
    }
}
