namespace PigTycoon.Core
{
    /// <summary>
    /// HANDBOOK P1.02 / P1.21: on a new game the player picks An or Khoa. Both share one controller and differ only in
    /// sprite, base stats (<see cref="PlayerRoster"/>, GDD 8.1) and basic trait. Choosing keeps the story protagonist,
    /// the six-lock owner and the protagonist bonuses in sync through <see cref="GameEngine.SwitchActiveProtagonist"/>.
    /// </summary>
    public static class NewGameSetup
    {
        /// <summary>The start screen shows only on a fresh game that no launcher preset and no scripted run (video
        /// autopilot, smoke test) has already decided.</summary>
        public static bool ShouldOfferCharacterSelect(bool restoredSave, bool presetByLauncher, bool scriptedRun) =>
            !restoredSave && !presetByLauncher && !scriptedRun;

        public static void ChooseProtagonist(GameEngine engine, PlayerId id)
        {
            if (engine == null) throw new System.ArgumentNullException(nameof(engine));
            engine.SwitchActiveProtagonist(id);
        }

        /// <summary>GDD 8.1 title of each protagonist.</summary>
        public static string Title(PlayerId id) => id == PlayerId.Khoa ? "Khoa — Người làm trại" : "An — Người đọc chợ";

        /// <summary>GDD 8.1 basic trait (unlocked at once).</summary>
        public static string BasicTraitName(PlayerId id) => id == PlayerId.Khoa ? "Tiếng Huýt Chăn Đàn" : "Mắt Nhà Nghề";

        /// <summary>One Vietnamese line with the base stats, read from the roster so no number is duplicated here.</summary>
        public static string StatLine(PlayerId id)
        {
            var c = new CharacterData(id);
            PlayerRoster.Apply(c, id);
            var s = c.BaseStats;
            return $"STR {s.Str} • AGI {s.Agi} • CTRL {s.Ctrl} • RES {s.Res}";
        }
    }
}
