using System;
using System.IO;
using PigTycoon.Core;
using PigTycoon.Presentation;
using UnityEngine;

namespace PigTycoon.PlayableDemo3D
{
    public sealed class PlayableDemoLaunchOptions
    {
        public string SaveDirectory { get; private set; } = "";
        public bool LoadExistingSave { get; private set; }
        public bool OwnSimulationClock { get; private set; }
        public bool IsAcceptance { get; private set; }
        public static PlayableDemoLaunchOptions Resolve(string[] arguments, string defaultDirectory, bool loadProfileSave)
        {
            bool acceptance = false, fresh = false, reload = false;
            string saveRoot = "";
            for (int i = 0; i < arguments.Length; i++)
            {
                string argument = arguments[i];
                if (argument.Equals("-pigTycoonAcceptance", StringComparison.OrdinalIgnoreCase)) acceptance = true;
                else if (argument.Equals("-pigTycoonFreshAcceptance", StringComparison.OrdinalIgnoreCase)) fresh = true;
                else if (argument.Equals("-pigTycoonAcceptanceReload", StringComparison.OrdinalIgnoreCase)) reload = true;
                else if (argument.Equals("-pigTycoonSaveRoot", StringComparison.OrdinalIgnoreCase))
                {
                    if (saveRoot.Length != 0 || ++i >= arguments.Length)
                        throw new ArgumentException("Provide exactly one absolute -pigTycoonSaveRoot directory.");
                    saveRoot = RequireAbsoluteDirectory(arguments[i]);
                }
            }
            if (acceptance && saveRoot.Length == 0) throw new ArgumentException("Acceptance requires an explicit isolated -pigTycoonSaveRoot.");
            if (acceptance && fresh && reload) throw new ArgumentException("Fresh acceptance and acceptance reload are mutually exclusive.");
            return new PlayableDemoLaunchOptions
            {
                SaveDirectory = saveRoot.Length == 0 ? RequireAbsoluteDirectory(defaultDirectory) : saveRoot,
                IsAcceptance = acceptance, OwnSimulationClock = !acceptance,
                LoadExistingSave = acceptance ? !fresh : loadProfileSave
            };
        }
        public static string RequireAbsoluteDirectory(string directory)
        {
            if (string.IsNullOrWhiteSpace(directory) || !Path.IsPathFullyQualified(directory))
                throw new ArgumentException("Save directory must be absolute.");
            return Path.GetFullPath(directory);
        }
    }

    /// <summary>One explicit populated scenario owner for the new3D scene; no legacy save owner.</summary>
    [DefaultExecutionOrder(-200)]
    public sealed class PlayableDemoBootstrap : MonoBehaviour
    {
        public PlayableFarmController Farm;
        public DemoScenarioKind Scenario = DemoScenarioKind.Care;
        public DemoScenarioLayout Layout = DemoScenarioFactory.DefaultLayout();
        public PlayableFarmStation DefenseStation;
        public GameObject DefenseVisualRoot;
        public bool LoadProfileSave = true;
        public DemoScenario CreatedScenario { get; private set; }
        public bool Ready { get; private set; }
        public string ManifestPath { get; private set; } = "";
        public PlayableDemoLaunchOptions LaunchOptions { get; private set; }

        private void Awake()
        {
            if (Farm == null) Farm = GetComponent<PlayableFarmController>();
            if (Farm == null) Farm = FindAnyObjectByType<PlayableFarmController>();
            if (Farm == null) { Debug.LogError("[DemoBootstrap] Assign the3D farm controller."); return; }
            if (FindAnyObjectByType<MobileGameController>() != null)
            { Fail("Remove the legacy MobileGameController from this isolated scene; it owns a separate save/engine."); return; }
            if (Farm.Session != null) { Fail("The farm was initialized before its scenario bootstrap."); return; }
            if (Farm.KhoaPrefab == null || Farm.PigPrefab == null)
            { Fail("Assign the actual Khoa and pig prefabs before starting the scene."); return; }
            var residents = FindObjectsByType<PlayableFarmNpcView>(FindObjectsSortMode.None);
            bool hasMan = false;
            foreach (var npc in residents) if (npc.NpcId == "Man") { npc.Farm = Farm; hasMan = true; }
            if (!hasMan) { Fail("Add the authored black-umbrella Mận model with PlayableFarmNpcView."); return; }
            Scenario = ReadRequestedScenario(Scenario);
            bool preRaid = Scenario == DemoScenarioKind.PreRaidFixture;
            if (preRaid && (DefenseStation == null || DefenseVisualRoot == null))
            { Fail("The pre-raid fixture needs its actual authored Nỏ visual and interaction station."); return; }
            try
            {
                LaunchOptions = PlayableDemoLaunchOptions.Resolve(Environment.GetCommandLineArgs(), Application.persistentDataPath, LoadProfileSave);
                CreatedScenario = DemoScenarioFactory.Create(Scenario, Layout);
                if (!ValidateAuthoredBindings(residents)) return;
                if (DefenseVisualRoot != null) DefenseVisualRoot.SetActive(preRaid);
                if (DefenseStation != null) DefenseStation.gameObject.SetActive(preRaid);
                if (preRaid)
                {
                    DefenseStation.Kind = FarmInteractionKind.Defense;
                    DefenseStation.CoreInstanceId = CreatedScenario.TowerInstanceId;
                }
                Farm.UseExistingCore = false; Farm.CoreController = null;
                Farm.OwnSimulationClock = LaunchOptions.OwnSimulationClock; Farm.UseConfiguredMap = false;
                Farm.LoadDemoSaveOnStart = LaunchOptions.LoadExistingSave;
                Farm.FailOnInvalidExistingSave = true;
                Farm.SaveFileName = CreatedScenario.SaveFileName;
                Farm.ConfigureSaveDirectory(LaunchOptions.SaveDirectory);
                Farm.ScenarioProfileId = CreatedScenario.ProfileId;
                Farm.RequiredTowerInstanceId = CreatedScenario.TowerInstanceId;
                Farm.ScenarioLabel = preRaid ? "Thử phòng thủ · Kịch bản riêng cấp 3, Nỏ rỗng và 50 tên" : "Chăm trại · Phiên demo độc lập";
                Farm.PlayerSpawn = new Vector3(Layout.PlayerX, Farm.PlayerSpawn.y, Layout.PlayerY);
                Farm.SaveRestored += OnSaveRestored;
                Farm.Initialize(CreatedScenario.Engine);
                if (preRaid && !BindExactTower()) return;
                if (Farm.PigViews.Count != Farm.Session.Engine.Pigs.FindAll(p => p.IsAlive).Count)
                { Fail("The authored pig views do not cover the actual live Core IDs."); return; }
                Ready = true;
                WriteLaunchManifest();
            }
            catch (Exception ex) when (ex is ArgumentException || ex is InvalidOperationException)
            { Fail(ex.Message); }
        }

        public bool BindExactTower()
        {
            if (CreatedScenario == null || CreatedScenario.TowerInstanceId.Length == 0) return true;
            string id = CreatedScenario.TowerInstanceId;
            var actual = Farm.Session.Engine.Defense.Buildings.Find(b => b.InstanceId == id);
            if (actual == null || actual.BlueprintId != "NoXuyenVan")
            { Fail("The saved fixture does not contain its declared exact Nỏ ID; no replacement/fallback was created."); return false; }
            DefenseStation.CoreInstanceId = actual.InstanceId;
            return true;
        }

        private bool ValidateAuthoredBindings(PlayableFarmNpcView[] residents)
        {
            var stations = FindObjectsByType<PlayableFarmStation>(FindObjectsSortMode.None);
            foreach (var service in CreatedScenario.Layout.Map.ServicePoints)
            {
                int matches = 0;
                foreach (var station in stations)
                {
                    if (station.Point.Id != service.Id || !At(station.transform.position, service.X, service.Y)) continue;
                    if (service.Feed != (station.Kind == FarmInteractionKind.Feed) || service.Water != (station.Kind == FarmInteractionKind.Water) ||
                        service.Shelter != station.ProvidesHerdShelter || service.Cooling != station.ProvidesCooling) continue;
                    matches++;
                }
                if (matches != 1)
                { Fail("Bind exactly one authored service station at its declared Core coordinates: " + service.Id); return false; }
            }
            int manCount = 0, positionedManCount = 0;
            foreach (var npc in residents) if (npc.NpcId == "Man")
            { manCount++; if (At(npc.transform.position, Layout.ManX, Layout.ManY)) positionedManCount++; }
            if (manCount != 1 || positionedManCount != 1) { Fail("Bind exactly one Mận model at the declared NPC position."); return false; }
            if (Scenario == DemoScenarioKind.PreRaidFixture && !At(DefenseStation.transform.position, Layout.DefenseX, Layout.DefenseY))
            { Fail("Place the authored Nỏ station at the fixture's declared coordinates."); return false; }
            return true;
        }
        private static bool At(Vector3 position, float x, float y)
            => Mathf.Abs(position.x - x) < 0.05f && Mathf.Abs(position.z - y) < 0.05f;

        private void OnSaveRestored()
        {
            if (!BindExactTower()) return;
            WriteLaunchManifest();
        }
        private void OnDestroy()
        { if (Farm != null) Farm.SaveRestored -= OnSaveRestored; }

        private void WriteLaunchManifest()
        {
            ManifestPath = Farm.SavePath + ".manifest.json";
            try
            {
                Directory.CreateDirectory(Farm.SaveDirectory);
                File.WriteAllText(ManifestPath, DemoScenarioFactory.SerializeLaunch(CreatedScenario, Farm.Session, Farm.LoadedExistingSave, Farm.PigViews.Count));
                Debug.Log("[DemoBootstrap] " + CreatedScenario.ProfileId + " ready; actual pigs=" + Farm.PigViews.Count + "; manifest=" + ManifestPath);
            }
            catch (Exception ex) when (ex is IOException || ex is UnauthorizedAccessException)
            { Debug.LogError("[DemoBootstrap] Cannot write scenario manifest: " + ex.Message); }
        }
        private void Fail(string message)
        {
            Ready = false;
            if (Farm != null) { Farm.SetPaused(true); Farm.enabled = false; }
            Debug.LogError("[DemoBootstrap] " + message);
        }
        private static DemoScenarioKind ReadRequestedScenario(DemoScenarioKind fallback)
        {
            foreach (string argument in Environment.GetCommandLineArgs())
            {
                if (argument.Equals("-pigTycoonScenario=preraid", StringComparison.OrdinalIgnoreCase)) return DemoScenarioKind.PreRaidFixture;
                if (argument.Equals("-pigTycoonScenario=care", StringComparison.OrdinalIgnoreCase)) return DemoScenarioKind.Care;
            }
            return fallback;
        }
    }
}
