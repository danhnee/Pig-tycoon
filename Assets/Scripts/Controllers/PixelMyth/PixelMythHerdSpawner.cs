using System;
using System.Linq;
using PigTycoon.Core;
using UnityEngine;

namespace PigTycoon.Presentation
{
    /// <summary>
    /// Farm map: creates one PigAgentView per living pig in Core, scattered inside the pasture rect the map
    /// layout declares, drawn with the breed-specific 32-PPU sprite library (`pig_*`). Starter
    /// pigs come from Core (StarterHerd); views follow Core every 0.5 s: newborns appear, sold/processed pigs vanish.
    /// </summary>
    public sealed class PixelMythHerdSpawner : MonoBehaviour
    {
        public Rect Pasture = new Rect(44, 46, 96, 48);
        public int MaxViews = 160; // GDD 3.9: cảnh thử 150 heo
        public int StarterPigs = 12;
        private bool spawned;

        private static readonly Collider2D[] spawnHits = new Collider2D[8];
        private static bool IsBlocked(Vector2 p)
        {
            int n = Physics2D.OverlapCircle(p, 0.7f, new ContactFilter2D { useTriggers = false }, spawnHits);
            for (int i = 0; i < n; i++)
                if (spawnHits[i] != null && (spawnHits[i].attachedRigidbody == null || spawnHits[i].attachedRigidbody.bodyType == RigidbodyType2D.Static)) return true;
            return false;
        }

        private static PixelMythSpriteLibrary FindLib(string id)
        {
            if (string.IsNullOrEmpty(id)) return null;
            return PixelMythRegistry.Find(id);
        }

        // story S02: the white-footed pig is named "Chân Trắng" (StoryJournalSystem.EvaluateWhiteFootedPigWeighing)
        private static bool IsWhiteFoot(Pig pig) => pig.Name.IndexOf("Chân Trắng", StringComparison.OrdinalIgnoreCase) >= 0;

        public static PixelMythSpriteLibrary ResolvePigLibrary(Pig pig, PixelMythSpriteLibrary fallback, string leaderPigId = null)
        {
            if (pig == null) return fallback;
            if (IsWhiteFoot(pig))
            {
                var wf = FindLib("pig_white_foot") ?? FindLib("pig_white_feet");
                if (wf != null) return wf;
            }
            if (leaderPigId != null && pig.Id == leaderPigId)
            {
                var ld = FindLib("pig_leader");
                if (ld != null) return ld;
            }
            if (pig.Stage == PigStage.HeoNon)
            {
                var st = FindLib("pig_heo_non");
                if (st != null) return st;
            }
            else if (pig.Stage == PigStage.DangLon)
            {
                var st = FindLib("pig_dang_lon");
                if (st != null) return st;
            }
            else if (pig.Stage == PigStage.HeoGia)
            {
                var st = FindLib("pig_heo_gia");
                if (st != null) return st;
            }

            string geneKey = pig.GeneLine switch
            {
                GeneLineId.HongDien  => "pig_hong_dien",
                GeneLineId.LamKhe    => "pig_lam_khe",
                GeneLineId.MocCuoc   => "pig_moc_cuoc",
                GeneLineId.ThoTram   => "pig_tho_tram",
                GeneLineId.XichMao   => "pig_xich_mao",
                GeneLineId.PhongMau  => "pig_phong_mau",
                GeneLineId.ThietBi   => "pig_thiet_bi",
                GeneLineId.TinhQuang => "pig_tinh_quang",
                GeneLineId.LoiMach   => "pig_loi_mach",
                GeneLineId.MongNhan  => "pig_mong_nhan",
                GeneLineId.KimTho    => "pig_kim_tho",
                GeneLineId.HuThe     => "pig_hu_the",
                _                    => "pig_hong_dien",
            };
            return FindLib(geneKey) ?? fallback;
        }

        // id → view của mọi heo do spawner quản lý (GDD 4.4: heo con mới đẻ, heo mua về; heo bán/xử lý xác thì gỡ)
        private readonly System.Collections.Generic.Dictionary<string, PigAgentView> views =
            new System.Collections.Generic.Dictionary<string, PigAgentView>();
        private readonly System.Collections.Generic.List<Vector2> placed = new System.Collections.Generic.List<Vector2>();
        private readonly System.Random rng = new System.Random(1234);
        private PixelMythSpriteLibrary fallbackLibrary;
        private Transform playerTransform;
        private float syncTimer;
        private const float SyncIntervalSeconds = 0.5f;
        private static readonly Color CorpseTint = new Color(0.55f, 0.52f, 0.50f, 1f);

        private void Update()
        {
            var engine = MobileGameController.Instance != null ? MobileGameController.Instance.Engine : null;
            if (engine == null) return;

            if (!spawned)
            {
                var player = FindObjectsByType<PlayerMobileController>(FindObjectsSortMode.None).FirstOrDefault(p => p.isActiveAndEnabled);
                if (player == null) return;
                spawned = true;
                playerTransform = player.transform;
                fallbackLibrary = FindLib("pig");

                // A new game already holds the handed-over herd (StarterHerd, ADR-0019). Only the recording scenario or
                // the -pigtycoon-herd=N switch tops the herd up here; an empty save stays empty (no free pigs).
                int living = engine.Pigs.Count(p => p.IsAlive);
                int wanted = RecordingScenario.IsRequested ? StarterPigs : 0;
                foreach (var arg in Environment.GetCommandLineArgs())
                    if (arg.StartsWith("-pigtycoon-herd=", StringComparison.Ordinal) && int.TryParse(arg.Substring(16), out var n)) wanted = n;
                if (living < wanted && RecordingScenario.IsRequested)
                {
                    var lines = new[]
                    {
                        GeneLineId.HongDien, GeneLineId.KimTho, GeneLineId.LamKhe, GeneLineId.MocCuoc,
                        GeneLineId.ThoTram, GeneLineId.XichMao, GeneLineId.PhongMau, GeneLineId.ThietBi,
                        GeneLineId.TinhQuang, GeneLineId.LoiMach, GeneLineId.MongNhan, GeneLineId.HuThe
                    };
                    for (int i = living; i < wanted; i++)
                    {
                        // adults: the handed-over herd is grown stock, not orphaned newborn piglets (age 0 / 1.5 kg default)
                        var p = Pig.CreateDefault($"p_start_{i + 1}", $"Heo #{i + 1}", lines[i % lines.Length], GeneRarity.Thuong,
                            i % 2 == 0 ? "Cai" : "Duc", initialAge: 20, initialWeight: 90f);
                        if (i == living && !engine.Pigs.Any(IsWhiteFoot)) p.Name = "Heo Chân Trắng";
                        engine.PrepareNewPig(p);
                        engine.Pigs.Add(p);
                    }
                }
                else if (living < wanted)
                {
                    StarterHerd.FillTo(engine, wanted);
                }

                SyncViews(engine, initial: true);
                Debug.Log($"[HerdSpawner] {views.Count} pig views in pasture {Pasture}");
                return;
            }

            syncTimer -= Time.deltaTime;
            if (syncTimer > 0f) return;
            syncTimer = SyncIntervalSeconds;
            SyncViews(engine, initial: false);
        }

        /// <summary>Keeps exactly one view per Core pig on the farm: spawns newcomers, lays corpses down, removes pigs that left.</summary>
        private void SyncViews(GameEngine engine, bool initial)
        {
            var present = new System.Collections.Generic.HashSet<string>();
            foreach (var pig in engine.Pigs.ToList())
            {
                if (pig == null) continue;
                bool onFarm = pig.IsAlive || !pig.IsInProcessingLot; // a corpse dragged to the processing lot is no longer on the pasture
                if (!onFarm) continue;
                present.Add(pig.Id);

                if (!views.TryGetValue(pig.Id, out var view) || view == null)
                {
                    // the scene still serialises the old 40-view cap; GDD 3.9 load target is 150 pigs on the farm
                    if (views.Count >= Mathf.Max(MaxViews, 150)) continue;
                    view = SpawnView(engine, pig, initial);
                    views[pig.Id] = view;
                }
                if (!pig.IsAlive) LayDownCorpse(view);
            }

            var gone = new System.Collections.Generic.List<string>();
            foreach (var kv in views)
                if (kv.Value == null || !present.Contains(kv.Key)) gone.Add(kv.Key);
            foreach (var id in gone)
            {
                if (views.TryGetValue(id, out var v) && v != null) Destroy(v.gameObject);
                views.Remove(id);
            }
        }

        private Vector2 PickSpot(Pig pig, bool initial)
        {
            // newborns and pigs that already walked the farm keep their Core position (beside the mother for a litter)
            var known = new Vector2(pig.PositionX, pig.PositionY);
            var bounds = Pasture;
            bounds.xMin -= 4f; bounds.yMin -= 4f; bounds.xMax += 4f; bounds.yMax += 4f;
            bool hasPosition = pig.PositionX != 0f || pig.PositionY != 0f;
            Vector2 spot = hasPosition && bounds.Contains(known) ? known : ClusterSpot();
            // keep >= 1.5 m between pigs so no two bodies spawn inside each other (piglets may stand closer)
            float minSqr = pig.Stage == PigStage.HeoNon ? 0.36f : 2.25f;
            for (int tries = 0; tries < 12 && placed.Exists(q => (q - spot).sqrMagnitude < minSqr); tries++)
                spot += new Vector2((float)(rng.NextDouble() - 0.5) * 2f, (float)(rng.NextDouble() - 0.5) * 1.5f);
            // never inside the pond, a building or a rock: a pig spawned in water could not move (playtest 2 Nov)
            for (int tries = 0; tries < 24 && IsBlocked(spot); tries++)
                spot = Vector2.MoveTowards(spot, Pasture.center, 1.5f) + new Vector2((float)(rng.NextDouble() - 0.5) * 3f, (float)(rng.NextDouble() - 0.5) * 3f);
            placed.Add(spot);
            if (placed.Count > 256) placed.RemoveAt(0);
            return spot;
        }

        private Vector2 ClusterSpot()
        {
            // a herd stays together: gaussian cluster around the pasture centre, clamped to the pasture
            double u1 = 1.0 - rng.NextDouble(), u2 = rng.NextDouble();
            double r = Math.Sqrt(-2.0 * Math.Log(u1)), th = 2.0 * Math.PI * u2;
            float px = Pasture.center.x + (float)(r * Math.Cos(th)) * Pasture.width * 0.12f;
            float py = Pasture.center.y + (float)(r * Math.Sin(th)) * Pasture.height * 0.18f;
            return new Vector2(Mathf.Clamp(px, Pasture.xMin + 1, Pasture.xMax - 1), Mathf.Clamp(py, Pasture.yMin + 1, Pasture.yMax - 1));
        }

        private PigAgentView SpawnView(GameEngine engine, Pig pig, bool initial)
        {
            var go = new GameObject($"Pig_{pig.Id}");
            var spot = PickSpot(pig, initial);
            go.transform.position = new Vector3(spot.x, spot.y, 0f);
            var sr = go.AddComponent<SpriteRenderer>();
            var rb = go.AddComponent<Rigidbody2D>();
            rb.gravityScale = 0f;
            rb.freezeRotation = true;
            var col = go.AddComponent<CircleCollider2D>();
            col.radius = 0.35f;
            col.offset = new Vector2(0f, 0.2f);
            var agent = go.AddComponent<PigAgentView>();
            agent.SpriteRenderer = sr;
            agent.EnsureThoughtBubble();
            var library = ResolvePigLibrary(pig, fallbackLibrary, engine.Herd.Leader?.PigId);
            var anim = go.AddComponent<PigSpriteAnimator>();
            anim.MythLibrary = library;
            var idle = library != null ? library.Get("idle") : null;
            if (idle != null && idle.Down.Length > 0) sr.sprite = idle.Down[0];
            agent.Bind(pig, playerTransform);
            if (pig.IsAlive)
            {
                pig.PositionX = spot.x;
                pig.PositionY = spot.y;
            }
            return agent;
        }

        /// <summary>GDD 4.7: a corpse stays on the farm until disposed — shown lying on its side, greyed, not moving.</summary>
        private static void LayDownCorpse(PigAgentView view)
        {
            if (view == null || Mathf.Approximately(view.transform.localEulerAngles.z, 90f)) return;
            var rb = view.GetComponent<Rigidbody2D>();
            if (rb != null)
            {
                rb.linearVelocity = Vector2.zero;
                rb.angularVelocity = 0f;
                rb.bodyType = RigidbodyType2D.Kinematic;
            }
            view.transform.localEulerAngles = new Vector3(0f, 0f, 90f);
            var anim = view.GetComponent<PigSpriteAnimator>();
            if (anim != null) anim.enabled = false;
            var sr = view.SpriteRenderer != null ? view.SpriteRenderer : view.GetComponent<SpriteRenderer>();
            if (sr != null)
            {
                sr.color = CorpseTint;
                sr.sortingOrder = Mathf.RoundToInt(-view.transform.position.y * 100f);
            }
            if (view.ThoughtBubbleText != null) view.ThoughtBubbleText.gameObject.SetActive(false);
        }
    }
}
