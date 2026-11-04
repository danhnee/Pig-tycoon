namespace PigTycoon.Core
{
    /// <summary>
    /// Đúng 2 nhân vật người chơi. GDD 8.1. Không thêm người thứ ba.
    /// </summary>
    public enum PlayerId
    {
        An,
        Khoa
    }

    public static class PlayerRoster
    {
        public const string AnDataId = "player.an";
        public const string KhoaDataId = "player.khoa";
        public const string AnBasicTraitId = "mat_nha_nghe";
        public const string KhoaBasicTraitId = "tieng_huyt_chan_dan";

        public static void Apply(CharacterData character, PlayerId identity)
        {
            if (identity == PlayerId.An)
            {
                // GDD 8.1 — An, "Người đọc chợ".
                character.ApplyIdentity(PlayerId.An, "An", AnDataId, AnBasicTraitId, 17, 23, 24, 18);
                return;
            }

            // GDD 8.1 — Khoa, "Người làm trại".
            character.ApplyIdentity(PlayerId.Khoa, "Khoa", KhoaDataId, KhoaBasicTraitId, 24, 18, 18, 22);
        }
    }
}
