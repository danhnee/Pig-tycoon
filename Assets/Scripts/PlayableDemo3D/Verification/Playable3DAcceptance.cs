using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using PigTycoon.Core;
using PigTycoon.Presentation;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

namespace PigTycoon.PlayableDemo3D.Verification
{
    /// <summary>Opt-in standalone evidence. All movement uses the real public motor input hook.</summary>
    public sealed class Playable3DAcceptance : MonoBehaviour
    {
        [Serializable] public sealed class Check { public string name, detail; public bool passed; }
        [Serializable] public sealed class Result
        {
            public bool passed, graphical, artApproved;
            public string scene, unityVersion, scenario, error = "", savePath = "";
            public List<Check> checks = new List<Check>();
            public List<string> errors = new List<string>();
            public List<string> screenshots = new List<string>();
            public List<string> initialPigIds = new List<string>();
            public int waveStarted, waveEnded, enemiesSpawned, towerShots, hpChanges, reportAmmoSpent;
            public bool reportVictory;
        }
        [Serializable] public sealed class StateReceipt
        {
            public string profile;
            public List<string> pigs;
            public long gold;
            public float feed, stamina, playerX, playerY;
            public int minute, actions, steelArrows, towerAmmo, hp;
        }
        private readonly Result result = new Result();
        private PlayableDemoBootstrap bootstrap;
        private PlayableFarmController farm;
        private PlayableFarmRaidView raid;
        private AcceptanceRoute route;
        private string reportPath;
        private float started;
        private bool finished;
        private IDisposable startSubscription, endSubscription;
        private GameEngine Engine => farm.Session.Engine;

        public static bool Requested => Array.IndexOf(Environment.GetCommandLineArgs(), "-pigTycoonAcceptance") >= 0;
        private static bool ReloadOnly => Array.IndexOf(Environment.GetCommandLineArgs(), "-pigTycoonAcceptanceReload") >= 0;
        public static string Argument(string key, string fallback = "")
        {
            var args = Environment.GetCommandLineArgs(); int i = Array.IndexOf(args, key);
            return i >= 0 && i + 1 < args.Length ? args[i + 1] : fallback;
        }
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterAssembliesLoaded)]
        private static void MarkAssemblies()
        {
            if (!Requested) return;
            string path=Path.GetFullPath(Argument("-pigTycoonAcceptanceReport",Path.Combine(Application.persistentDataPath,"acceptance.json")));
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            File.WriteAllText(path+".trace.txt","assemblies-ready\n");
        }
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Install()
        {
            if (!Requested) return;
            var instance = new GameObject("Playable3DAcceptance").AddComponent<Playable3DAcceptance>();
            DontDestroyOnLoad(instance.gameObject);
        }
        private void Awake()
        {
            Application.runInBackground = true;
            reportPath = Path.GetFullPath(Argument("-pigTycoonAcceptanceReport", Path.Combine(Application.persistentDataPath, "acceptance.json")));
            Directory.CreateDirectory(Path.GetDirectoryName(reportPath));
            started = Time.realtimeSinceStartup;
            Application.logMessageReceived += Log;
            File.AppendAllText(reportPath + ".trace.txt", "before-scene-load\n");
        }
        private IEnumerator Start() { yield return Guard(Run()); }
        private void Update()
        { if (!finished && Time.realtimeSinceStartup - started > 480f) Finish(false, "Acceptance exceeded 480 seconds; partial evidence retained."); }
        private void Log(string message, string stack, LogType type)
        {
            if (type == LogType.Error || type == LogType.Exception || type == LogType.Assert)
                result.errors.Add(message + "\n" + stack);
        }
        // Unity does not propagate nested coroutine exceptions to their caller. Flatten them so
        // a blocked route or failed action always produces a failing JSON instead of a hang.
        private IEnumerator Guard(IEnumerator root)
        {
            var stack = new Stack<IEnumerator>(); stack.Push(root);
            while (stack.Count > 0 && !finished)
            {
                object current = null; bool advanced = false; Exception error = null;
                try { advanced = stack.Peek().MoveNext(); if (advanced) current = stack.Peek().Current; }
                catch (Exception ex) { error = ex; }
                if (error != null) { Finish(false, error.ToString()); yield break; }
                if (!advanced) { stack.Pop(); continue; }
                if (current is IEnumerator nested) stack.Push(nested); else yield return current;
            }
        }
        private IEnumerator Run()
        {
            yield return null; yield return null;
            result.scene = SceneManager.GetActiveScene().path;
            result.unityVersion = Application.unityVersion;
            result.graphical = SystemInfo.graphicsDeviceType != GraphicsDeviceType.Null;
            File.AppendAllText(reportPath + ".trace.txt", "after-scene-load\n");
            Need(!Application.isEditor && result.unityVersion == "6000.6.3f1", "standalone-version", result.unityVersion);
            bootstrap = FindAnyObjectByType<PlayableDemoBootstrap>();
            // Fail safely if a candidate omitted the agreed pre-Awake save hook: redirect this
            // controller's quit autosave before recording failure, never overwrite a user profile.
            farm = bootstrap == null ? FindAnyObjectByType<PlayableFarmController>() : bootstrap.Farm;
            RequireIsolatedSaveOwner();
            Need(bootstrap != null && bootstrap.Ready, "bootstrap-ready", "Fresh populated scene must initialize without fallback.");
            farm = bootstrap.Farm; route = FindAnyObjectByType<AcceptanceRoute>();
            Need(farm != null && farm.Session != null && route != null, "canonical-owner-route", "Authored acceptance route required.");
            Need(FindObjectsByType<PlayableFarmController>(FindObjectsSortMode.None).Length == 1 &&
                FindObjectsByType<MobileGameController>(FindObjectsSortMode.None).Length == 0, "single-engine-owner", "One populated bootstrap, no legacy owner.");
            result.scenario = farm.ScenarioProfileId;
            Need(Engine.InteractiveHerdOwned,"interactive-herd-owner","Bootstrap owns the canonical interactive herd before actions.");
            Need(Engine.Character.Identity == PlayerId.Khoa && Enum.GetValues(typeof(PlayerId)).Length == 2,
                "canonical-khoa", Engine.Character.Name);
            Need(farm.PlayerMotor != null && HasMesh(farm.PlayerRoot) && farm.GameplayCamera != null,
                "authored-khoa-camera", "Live Khoa renderers, CharacterController and gameplay camera.");
            foreach (var pig in Engine.Pigs.Where(p => p.IsAlive))
            {
                result.initialPigIds.Add(pig.Id);
                Need(farm.PigViews.TryGetValue(pig.Id, out var view) && HasMesh(view) &&
                    view.GetComponent<PlayableFarmActor>()?.CorePigId == pig.Id,
                    "pig-model-id:" + pig.Id, "Actual model bound to actual live Core ID.");
            }
            Need(farm.PigViews.Count == result.initialPigIds.Count && farm.PigViews.Count > 0, "herd-count", farm.PigViews.Count.ToString());
            Need(!string.IsNullOrEmpty(Argument("-pigTycoonSaveRoot")) && Path.IsPathRooted(Argument("-pigTycoonSaveRoot")),
                "isolated-save-root", "Acceptance requires an explicit absolute save root before bootstrap Awake.");
            // Bootstrap must disable the clock before Initialize; don't hide a missing launch binding here.
            Need(!farm.OwnSimulationClock,"acceptance-clock-isolated","Motor, collision, steering and rendering still update normally.");
            farm.SetMoveInput(Vector2.zero, false);
            if (ReloadOnly)
            {
                Need(farm.LoadedExistingSave, "second-process-save-loaded", "Bootstrap must restore the isolated existing profile.");
                string expectedPath = Path.Combine(Path.GetFullPath(Argument("-pigTycoonSaveRoot")), bootstrap.CreatedScenario.SaveFileName + ".acceptance-expected.json");
                Need(File.Exists(expectedPath), "second-process-receipt-exists", expectedPath);
                var expected = JsonUtility.FromJson<StateReceipt>(File.ReadAllText(expectedPath));
                Need(Matches(expected, CurrentState()), "second-process-state-preserved", "Compare against first process's IDs/resources/stamina/clock/actions/position/ammo/HP.");
                yield return Snapshot("reload-gameplay"); Finish(true, ""); yield break;
            }
            Need(!farm.LoadedExistingSave, "fresh-profile", "Fresh acceptance must skip prior progress without deleting it.");
            yield return Snapshot("startup-gameplay");
            yield return VerifyWall();
            if (bootstrap.Scenario == DemoScenarioKind.PreRaidFixture) yield return VerifyRaid();
            else yield return VerifyCare();
            yield return VerifySave();
            Need(result.errors.Count == 0, "no-runtime-errors", result.errors.Count.ToString());
            Finish(true, "");
        }
        private void RequireIsolatedSaveOwner()
        {
            if (farm == null) return;
            string root=Argument("-pigTycoonSaveRoot");
            string expected=bootstrap?.CreatedScenario == null || !Path.IsPathRooted(root) ? "" :
                Path.GetFullPath(Path.Combine(root,bootstrap.CreatedScenario.SaveFileName));
            string actual=farm.SavePath;
            bool isolated=expected.Length>0 && actual!=null && string.Equals(Path.GetFullPath(actual),expected,StringComparison.OrdinalIgnoreCase);
            if (!isolated) farm.SaveFileName=Path.Combine(Path.GetDirectoryName(reportPath),"failed-isolation-quit-save.json");
            Need(isolated,"actual-save-owner-isolated","Actual controller path="+actual+"; expected="+expected);
        }
        private IEnumerator VerifyWall()
        {
            Need(route.WallCollider != null && route.WallCollider.enabled && !route.WallCollider.isTrigger &&
                route.WallApproach != null && route.WallBeyond != null, "wall-authoring", "Solid authored collision wall and endpoints required.");
            foreach (var point in route.WallRoute) { Need(point != null, "wall-waypoint", "Missing waypoint."); yield return Walk(point.position); }
            yield return Walk(route.WallApproach.position);
            Vector3 before = farm.PlayerRoot.position;
            Ray ray = new Ray(before + Vector3.up * 0.9f, Flat(route.WallBeyond.position - before).normalized);
            Need(route.WallCollider.Raycast(ray, out var hit, Flat(route.WallBeyond.position - before).magnitude),
                "wall-test-crosses-solid", route.WallCollider.name);
            float until = Time.realtimeSinceStartup + 3f;
            while (Time.realtimeSinceStartup < until) { Aim(route.WallBeyond.position); yield return null; }
            farm.SetMoveInput(Vector2.zero, false);
            Vector3 after = farm.PlayerRoot.position;
            bool overlap = Physics.ComputePenetration(farm.PlayerMotor, after, farm.PlayerRoot.rotation,
                route.WallCollider, route.WallCollider.transform.position, route.WallCollider.transform.rotation, out _, out float depth) && depth > 0.04f;
            Need(Flat(after - before).magnitude > 0.1f && Flat(after - before).magnitude < hit.distance + farm.PlayerMotor.radius &&
                Flat(after - route.WallBeyond.position).magnitude > 0.5f && !overlap,
                "motor-wall-stop", "Before=" + before + "; after=" + after + "; penetration=" + depth);
            yield return Snapshot("wall-stop-gameplay");
            yield return Walk(route.WallApproach.position);
        }
        private IEnumerator VerifyCare()
        {
            Need(route.CareRoute.Length > 0 && route.FeedStop >= 0 && route.WaterStop > route.FeedStop &&
                route.NpcStop > route.WaterStop && route.MarketStop > route.NpcStop && route.MarketStop < route.CareRoute.Length,
                "care-route-contract", "Ordered feed/water/NPC/market authored stops required.");
            string selected = "";
            for (int i = 0; i < route.CareRoute.Length; i++)
            {
                Need(route.CareRoute[i] != null, "care-waypoint", i.ToString());
                yield return Walk(route.CareRoute[i].position);
                if (i == route.FeedStop)
                {
                    var feeder = Station(FarmInteractionKind.Feed);
                    float until = Time.realtimeSinceStartup + 18f;
                    Pig pig;
                    do { pig = Engine.Pigs.Find(p => p.IsAlive && Dist(p.PositionX, p.PositionY, farm.Session.PlayerX, farm.Session.PlayerY) < 2.4f &&
                        Dist(p.PositionX, p.PositionY, feeder.Point.X, feeder.Point.Y) < 2.4f); if (pig == null) yield return null; }
                    while (pig == null && Time.realtimeSinceStartup < until);
                    Need(pig != null, "pig-reached-feeder", "No model/coordinate teleport allowed.");
                    selected = pig.Id; Action(farm.SelectPig(selected), "inspect-real-pig");
                    float stock = Engine.Farm.FeedStockKg, stamina = Engine.Character.Stamina.CurrentStamina;
                    Action(farm.InteractNearest(), "feed-action");
                    Need(Engine.Farm.FeedStockKg < stock && Engine.Character.Stamina.CurrentStamina < stamina && pig.FedToday &&
                        farm.Session.HasEvidence("feed-consumed"), "feed-real-resource-deltas", "Feed=" + stock + "→" + Engine.Farm.FeedStockKg + "; stamina=" + stamina + "→" + Engine.Character.Stamina.CurrentStamina);
                    Action(farm.Session.StartGuiding(selected), "guide-real-pig");
                    yield return Snapshot("feeding-gameplay");
                }
                if (i == route.WaterStop)
                {
                    var water = Station(FarmInteractionKind.Water); float stamina = Engine.Character.Stamina.CurrentStamina;
                    Action(farm.InteractNearest(), "water-action");
                    Need(farm.Session.State.WaterChangedDays.TryGetValue(water.Point.Id, out int day) && day == Engine.Clock.CurrentDay &&
                        Engine.Character.Stamina.CurrentStamina < stamina, "water-real-state-delta", "Stamina=" + stamina + "→" + Engine.Character.Stamina.CurrentStamina);
                }
                if (i == route.NpcStop)
                {
                    var npc = FindObjectsByType<PlayableFarmNpcView>(FindObjectsSortMode.None).Single(n => n.NpcId == "Man");
                    Need(HasMesh(npc.transform), "authored-man-model", npc.name);
                    long gold = Engine.Farm.Gold; int hired = Engine.PetsAndNpcs.HiredNpcs.Count;
                    Action(farm.InteractNearest(), "npc-talk-action");
                    Need(farm.Session.State.Actions.Exists(a => a.Action == "talk" && a.TargetId == "Man") &&
                        Engine.Farm.Gold == gold && Engine.PetsAndNpcs.HiredNpcs.Count == hired && !PlayableNpcConversation.HasActiveFarmContract(Engine, "Man"),
                        "free-talk-not-hired", npc.EmploymentStatus);
                    yield return Snapshot("npc-gameplay");
                }
                if (i == route.MarketStop)
                {
                    var market = Station(FarmInteractionKind.Market);
                    float until = Time.realtimeSinceStartup + 15f;
                    while (farm.Session.GetPig(selected) != null && Dist(farm.Session.GetPig(selected).PositionX, farm.Session.GetPig(selected).PositionY, market.Point.X, market.Point.Y) > market.ReachMeters && Time.realtimeSinceStartup < until) yield return null;
                    long gold = Engine.Farm.Gold; int pigs = Engine.Pigs.Count;
                    Action(farm.Session.SellPig(market.Point, selected, false), "real-day-sale");
                    farm.RefreshViews(); yield return null;
                    Need(Engine.Farm.Gold > gold && farm.Session.GetPig(selected) == null && !farm.PigViews.ContainsKey(selected) &&
                        farm.Session.HasEvidence("sale"), "sale-state-and-view", "Gold=" + gold + "→" + Engine.Farm.Gold + "; previous count=" + pigs);
                    yield return Snapshot("market-gameplay");
                }
            }
        }
        private IEnumerator VerifyRaid()
        {
            raid = FindAnyObjectByType<PlayableFarmRaidView>();
            Need(raid != null && raid.Model != null && raid.ThiefPrefab != null && raid.CrawlerPrefab != null,
                "authored-raid-bindings", "Real enemy prefabs and runtime raid model.");
            Need(route.DefenseStop >= 0 && route.DefenseStop < route.RaidRoute.Length, "defense-route", "Authored route required.");
            for (int i = 0; i <= route.DefenseStop; i++) { Need(route.RaidRoute[i] != null, "raid-waypoint", i.ToString()); yield return Walk(route.RaidRoute[i].position); }
            var tower = Engine.Defense.Buildings.Single(b => b.InstanceId == DemoScenarioFactory.OwnedTowerId);
            int inventory = Engine.Inventory.CountId("TenThep"), ammo = tower.AmmoCurrent;
            Action(farm.InteractNearest(), "preload-real-inventory");
            Need(tower.AmmoCurrent > ammo && inventory - Engine.Inventory.CountId("TenThep") == tower.AmmoCurrent - ammo,
                "ammo-stock-transfer", "Inventory=" + inventory + "→" + Engine.Inventory.CountId("TenThep") + "; tower=" + ammo + "→" + tower.AmmoCurrent);
            startSubscription = Engine.EventBus.Subscribe<WaveStarted>(e => result.waveStarted++);
            endSubscription = Engine.EventBus.Subscribe<WaveEnded>(e => { result.waveEnded++; result.reportVictory = e.Victory; });
            raid.Model.TowerFired += id => result.towerShots++;
            Need(Engine.Clock.HoursUntilNextMinorWave <= 2 && !raid.Model.Active, "scheduled-warning", Engine.Clock.HoursUntilNextMinorWave.ToString());
            farm.Session.AdvanceSimulationMinutes(120);
            Need(farm.Session.State.PendingRaid || Engine.Clock.IsMinorWaveTriggered, "real-arrival-due", "Actual engine tick reached its existing schedule.");
            float until = Time.realtimeSinceStartup + 12f;
            while (!raid.Model.Active && Time.realtimeSinceStartup < until) yield return null;
            Need(raid.Model.Active && result.waveStarted == 1, "event-backed-start", "Spawn must pass real camera/proximity/collider eligibility.");
            result.enemiesSpawned = raid.Model.Enemies.Count;
            var originalHp = raid.Model.Enemies.ToDictionary(e => e.Model.Id, e => e.Model.CurrentHp);
            yield return null;
            foreach (var enemy in raid.Model.Enemies)
                Need(raid.EnemyViews.TryGetValue(enemy.Model.Id, out var view) && HasMesh(view) &&
                    view.GetComponent<PlayableFarmActor>()?.CoreEnemyId == enemy.Model.Id && view.GetComponentInChildren<Collider>() != null,
                    "enemy-model-id:" + enemy.Model.Id, "Actual live enemy ID, mesh and shot collider.");
            yield return Snapshot("raid-start-gameplay");
            until = Time.realtimeSinceStartup + 180f;
            while (raid.Model.Active && Time.realtimeSinceStartup < until)
            {
                var target = raid.Model.Enemies.Where(e => !e.Resolved).OrderBy(e => Dist(e.X,e.Y,farm.Session.PlayerX,farm.Session.PlayerY)).FirstOrDefault();
                if (target != null)
                {
                    if (Dist(target.X,target.Y,farm.Session.PlayerX,farm.Session.PlayerY) > 1.3f) Aim(new Vector3(target.X,0f,target.Y));
                    else { farm.SetMoveInput(Vector2.zero,false); raid.AttackNearest(false); }
                }
                yield return null;
            }
            farm.SetMoveInput(Vector2.zero,false);
            result.hpChanges = raid.Model.Enemies.Count(e => originalHp.TryGetValue(e.Model.Id,out float hp) && e.Model.CurrentHp < hp);
            Need(!raid.Model.Active && result.waveEnded == 1 && raid.Model.LastReport != null, "event-backed-end", "No aggregate resolver or forced victory.");
            result.reportAmmoSpent = raid.Model.LastReport.AmmoSpent;
            Need(result.enemiesSpawned == 2 && result.hpChanges > 0 && result.towerShots > 0 && result.reportAmmoSpent > 0 &&
                result.reportVictory == raid.Model.LastReport.Victory, "raid-observed-outcome", "Shots=" + result.towerShots + "; HP changes=" + result.hpChanges + "; ammo=" + result.reportAmmoSpent + "; victory=" + result.reportVictory);
            int minute = farm.Session.AbsoluteGameMinute; farm.Session.AdvanceSimulationMinutes(1);
            Need(farm.Session.AbsoluteGameMinute == minute + 1 && !Engine.Clock.IsDuringCombatWave, "post-raid-clock-resumed", minute + "→" + farm.Session.AbsoluteGameMinute);
            yield return Snapshot("raid-end-gameplay");
        }
        private IEnumerator VerifySave()
        {
            farm.SetMoveInput(Vector2.zero,false); farm.SetPaused(true);
            var ids = Engine.Pigs.Where(p => p.IsAlive).Select(p => p.Id).OrderBy(id => id).ToArray();
            long gold = Engine.Farm.Gold; float feed = Engine.Farm.FeedStockKg, stamina = Engine.Character.Stamina.CurrentStamina;
            int minute = farm.Session.AbsoluteGameMinute, actions = farm.Session.State.Actions.Count;
            Action(farm.TrySave(), "save-to-isolated-profile");
            result.savePath = Path.Combine(Path.GetFullPath(Argument("-pigTycoonSaveRoot")), bootstrap.CreatedScenario.SaveFileName);
            Need(File.Exists(result.savePath), "isolated-file-exists", result.savePath);
            File.Copy(result.savePath, reportPath + ".save.json", true);
            File.WriteAllText(result.savePath + ".acceptance-expected.json", JsonUtility.ToJson(CurrentState(), true));
            Action(farm.TryLoad(), "restore-real-save-file");
            Need(ids.SequenceEqual(Engine.Pigs.Where(p => p.IsAlive).Select(p => p.Id).OrderBy(id => id)) && Engine.Farm.Gold == gold &&
                Math.Abs(Engine.Farm.FeedStockKg-feed)<0.001f && Math.Abs(Engine.Character.Stamina.CurrentStamina-stamina)<0.001f &&
                farm.Session.AbsoluteGameMinute == minute && farm.Session.State.Actions.Count == actions,
                "save-state-preserved", "Pig IDs, gold, feed, stamina, clock and actual action history.");
            farm.SetPaused(false); yield return Snapshot("save-reload-gameplay");
        }
        private StateReceipt CurrentState() => new StateReceipt
        {
            profile=farm.Session.State.ScenarioProfileId,
            pigs=Engine.Pigs.Where(p=>p.IsAlive).Select(p=>p.Id).OrderBy(id=>id).ToList(),
            gold=Engine.Farm.Gold, feed=Engine.Farm.FeedStockKg, stamina=Engine.Character.Stamina.CurrentStamina,
            playerX=farm.Session.PlayerX, playerY=farm.Session.PlayerY, minute=farm.Session.AbsoluteGameMinute,
            actions=farm.Session.State.Actions.Count, steelArrows=Engine.Inventory.CountId("TenThep"), hp=Engine.Character.Hp,
            towerAmmo=Engine.Defense.Buildings.Where(b=>b.InstanceId==DemoScenarioFactory.OwnedTowerId).Sum(b=>b.AmmoCurrent)
        };
        private static bool Matches(StateReceipt a, StateReceipt b) => a != null && a.pigs != null &&
            a.profile==b.profile && a.pigs.SequenceEqual(b.pigs) && a.gold==b.gold &&
            Math.Abs(a.feed-b.feed)<0.001f && Math.Abs(a.stamina-b.stamina)<0.001f &&
            Math.Abs(a.playerX-b.playerX)<0.001f && Math.Abs(a.playerY-b.playerY)<0.001f &&
            a.minute==b.minute && a.actions==b.actions && a.steelArrows==b.steelArrows && a.towerAmmo==b.towerAmmo && a.hp==b.hp;
        private PlayableFarmStation Station(FarmInteractionKind kind)
        {
            var station = farm.Stations.Where(s => s != null && s.Kind == kind && farm.Session.CanInteract(s.Point))
                .OrderBy(s => Dist(s.Point.X,s.Point.Y,farm.Session.PlayerX,farm.Session.PlayerY)).FirstOrDefault();
            Need(station != null, "reachable-station:"+kind, "Authored route must reach actual station.");
            return station;
        }
        private IEnumerator Walk(Vector3 target)
        {
            Vector3 before = farm.PlayerRoot.position; float until = Time.realtimeSinceStartup + 25f;
            while (Flat(target-farm.PlayerRoot.position).magnitude > 0.3f && Time.realtimeSinceStartup < until)
            { Aim(target); yield return null; }
            farm.SetMoveInput(Vector2.zero,false);
            Need(Flat(target-farm.PlayerRoot.position).magnitude <= 0.4f, "motor-waypoint", "from="+before+" target="+target+" actual="+farm.PlayerRoot.position);
        }
        private void Aim(Vector3 target)
        {
            // Walk with the actual guided pig instead of sprinting the driver ahead at
            // player speed. Waiting uses the same public input hook; no pig state is moved.
            if (farm.Session.FollowingPigIds.Any(id => farm.Session.GetPig(id) != null &&
                Dist(farm.Session.GetPig(id).PositionX,farm.Session.GetPig(id).PositionY,farm.Session.PlayerX,farm.Session.PlayerY)>3f))
            { farm.SetMoveInput(Vector2.zero,false); return; }
            Vector3 forward = farm.GameplayCamera == null ? Vector3.forward : Flat(farm.GameplayCamera.transform.forward).normalized;
            Vector3 right = new Vector3(forward.z,0f,-forward.x), delta = Flat(target-farm.PlayerRoot.position).normalized;
            farm.SetMoveInput(new Vector2(Vector3.Dot(delta,right),Vector3.Dot(delta,forward)),false);
        }
        private IEnumerator Snapshot(string name)
        {
            if (!result.graphical) yield break;
            yield return new WaitForEndOfFrame();
            string directory=reportPath+".captures"; Directory.CreateDirectory(directory);
            string path = Path.Combine(directory, name+".png");
            var texture = ScreenCapture.CaptureScreenshotAsTexture();
            Need(texture != null && texture.width >= 640 && texture.height >= 360, "gameplay-capture:"+name, "Actual player screen, current gameplay camera and HUD.");
            File.WriteAllBytes(path, texture.EncodeToPNG()); Destroy(texture); result.screenshots.Add(path);
        }
        private static Vector3 Flat(Vector3 value) => new Vector3(value.x,0f,value.z);
        private static float Dist(float x,float z,float bx,float bz) => Vector2.Distance(new Vector2(x,z),new Vector2(bx,bz));
        private static bool HasMesh(Transform root) => root != null && root.GetComponentsInChildren<Renderer>().Any(r => r.enabled &&
            (r is SkinnedMeshRenderer skin && skin.sharedMesh != null && skin.sharedMesh.vertexCount > 100 || r is MeshRenderer && r.GetComponent<MeshFilter>()?.sharedMesh != null && r.GetComponent<MeshFilter>().sharedMesh.vertexCount > 100));
        private void Action(FarmActionResult action,string name) => Need(action.Success,name,action.Message);
        private void Need(bool pass,string name,string detail)
        {
            result.checks.Add(new Check { name=name,passed=pass,detail=detail });
            File.AppendAllText(reportPath+".trace.txt", (pass?"PASS ":"FAIL ")+name+" "+detail+"\n");
            if (!pass) throw new InvalidOperationException(name+": "+detail);
        }
        private void Finish(bool pass,string error)
        {
            if (finished) return; finished=true;
            if (farm != null) farm.SetMoveInput(Vector2.zero,false);
            startSubscription?.Dispose(); endSubscription?.Dispose();
            result.passed=pass; result.error=error;
            File.WriteAllText(reportPath,JsonUtility.ToJson(result,true));
            Application.logMessageReceived-=Log;
            Application.Quit(pass?0:1);
        }
    }
}
