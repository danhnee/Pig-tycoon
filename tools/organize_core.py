"""Group Assets/Scripts/Core into domain folders (3 Nov 2026).

Moves each .cs with its .meta (Unity GUIDs are kept, so no scene/prefab reference breaks). Namespaces stay
`PigTycoon.Core`; the asmdef stays at Core/. Tests compile Core/**/*.cs, so nothing else changes.

    python tools/organize_core.py            # dry run
    python tools/organize_core.py --apply
"""
import os
import shutil
import sys

CORE = "Assets/Scripts/Core"
LAYOUT = {
    "Engine": ["GameEngine", "GameEngineEconomyIntel", "GameEngineHerdLifecycle", "GameEngineNarrative", "Enums",
               "BalanceBook", "DataCatalog", "PigTycoonContracts", "StardewGradeArchitectureSpine", "NewGameSetup"],
    "Save": ["GameSaveData", "SaveSystem"],
    "Time": ["GameClock", "WeatherScheduler", "PlayerWeatherStatus"],
    "Farm": ["Farm", "FarmGridAndZones", "FarmMainLayout", "PlayableMapConfiguration", "PastureBoundary", "TroughCommands",
             "FarmLaborStamina", "SystemicEcosystemSimulation"],
    "Herd": ["Pig", "PigCareCommands", "HerdCare", "HerdFlowField", "FlowField8Dir", "HerdLifecycleRules", "HerdManager",
             "StarterHerd", "BreedingLedger", "GeneticsAndBreedingSystem", "DiseaseBoard"],
    "Character": ["Character", "PlayerRoster", "PlayerCarry", "CarryCapacity", "PlayerLocomotion", "PlayerRestCommands",
                  "ProtagonistTraits", "SpaceBag", "QuickBag", "EquipmentAppearance", "AnimationClips", "CardinalFacing"],
    "Combat": ["MartialProgression", "CombatFormulas", "CombatStrike", "WeaponData", "EnemyData", "MonsterMoves", "LootData",
               "MercenarySpecs", "EnhancementTransactions", "SpecialtyPaperActions", "SpecialtyLockTracker"],
    "Defense": ["DefenseManager", "DefenseRepairs", "DefenseStructureRules", "ConstructionQueue", "WaveApproachPlanner"],
    "Economy": ["EconomyManager", "MarketCycleThemes", "NightAuction", "NightMarketBarter", "VillageEconomy", "PetMarket",
                "IntelBureau", "MemberACDBackendSystems"],
    "Npc": ["NpcContracts", "RecruitmentRules", "PetNpcAndOccultManager"],
    "World": ["PixelWorldRoutes", "OccultEncounterDirector", "OccultExpedition"],
    "Story": ["StoryAndDeepSystems", "StoryQuestBook", "CanonicalSagaAndGdd6Systems"],
    "Narrative": ["NarrativeRuntime", "NarrativeLibrary", "NarrativeBackdrops", "NarrativeCast", "NarrativeCastContent",
                  "NarrativeContentChapter1", "NarrativeContentPrologueAn", "NarrativeContentPrologueKhoa",
                  "NarrativeContentStart", "NarrativeContentVillage"],
}


def main(apply):
    present = {f[:-3] for f in os.listdir(CORE) if f.endswith(".cs")}
    planned = {n for names in LAYOUT.values() for n in names}
    unplaced = sorted(present - planned)
    missing = sorted(planned - present)
    if missing:
        print("not found (skipped):", ", ".join(missing))
    if unplaced:
        print("NOT PLACED (stay in Core/):", ", ".join(unplaced))
    for folder, names in LAYOUT.items():
        dst = os.path.join(CORE, folder)
        for n in names:
            if n not in present:
                continue
            print(f"{n}.cs -> {folder}/")
            if apply:
                os.makedirs(dst, exist_ok=True)
                for ext in (".cs", ".cs.meta"):
                    src = os.path.join(CORE, n + ext)
                    if os.path.exists(src):
                        shutil.move(src, os.path.join(dst, n + ext))
    if apply:
        # Unity needs a .meta per new folder; create stable ones so the editor does not regenerate them later
        import hashlib
        for folder in LAYOUT:
            meta = os.path.join(CORE, folder + ".meta")
            if not os.path.exists(meta) and os.path.isdir(os.path.join(CORE, folder)):
                guid = hashlib.md5(("PigTycoon.Core/" + folder).encode()).hexdigest()
                open(meta, "w", newline="\n").write(f"fileFormatVersion: 2\nguid: {guid}\nfolderAsset: yes\nDefaultImporter:\n  externalObjects: {{}}\n  userData: \n  assetBundleName: \n  assetBundleVariant: \n")


if __name__ == "__main__":
    main("--apply" in sys.argv)
