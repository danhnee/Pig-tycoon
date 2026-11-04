namespace PigTycoon.Core
{
    /// <summary>
    /// Player-facing names for pig data. Every UI uses these instead of printing enum ids ("HongDien", "TruongThanh"),
    /// and the gene line follows GDD 5.1/5.4: Thường and Khá show their name at once; Hiếm and above stay "?" until
    /// identified.
    /// </summary>
    public static class PigText
    {
        public static string Stage(PigStage s) => s switch
        {
            PigStage.HeoNon => "Heo non",
            PigStage.DangLon => "Đang lớn",
            PigStage.TruongThanh => "Trưởng thành",
            _ => "Heo già",
        };

        public static string Rarity(GeneRarity r) => r switch
        {
            GeneRarity.Thuong => "Thường",
            GeneRarity.Kha => "Khá",
            GeneRarity.Hiem => "Hiếm",
            GeneRarity.Quy => "Quý",
            GeneRarity.HuyenThoai => "Huyền Thoại",
            _ => "Dị Biến",
        };

        /// <summary>GDD 5.1: Thường/Khá are recognised at a glance; rarer lines need an appraisal.</summary>
        public static bool LineVisible(GeneRarity rarity, bool identified) => identified || rarity == GeneRarity.Thuong || rarity == GeneRarity.Kha;

        public static string Line(Pig pig) => pig == null ? "?" : Line(pig.GeneLine, pig.Rarity, pig.IsIdentified);

        public static string Line(GeneLineId line, GeneRarity rarity, bool identified) =>
            LineVisible(rarity, identified) ? GeneticsAndBreedingSystem.GetGeneProfile(line).Name : "?";

        public static string Gender(string g) => g == "Duc" ? "Đực" : "Cái";
    }
}
