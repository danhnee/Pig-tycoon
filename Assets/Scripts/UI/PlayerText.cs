using System.Text.RegularExpressions;

namespace PigTycoon.Presentation
{
    /// <summary>
    /// Strips design-document references from text a player reads ("(GDD 13.2)", "— GDD 10.4", "Bảng 1.5-D GDD",
    /// "ADR-0017", "Sổ tay v3.0 §10", "[TẠM]", "P2.04"). Core messages cite the GDD for us; the player must never see
    /// spec jargon (3 Nov user review: "kèm text lung tung, text của bản thiết kế").
    /// </summary>
    public static class PlayerText
    {
        static readonly Regex Paren = new Regex(@"\s*[\(\[][^\(\)\[\]]*(GDD|ADR-\d|Sổ tay v|HANDBOOK|Bảng \d+\.\d+|TẠM\b|\bP\d\.\d{2}\b)[^\(\)\[\]]*[\)\]]", RegexOptions.Compiled);
        static readonly Regex Inline = new Regex(@"\s*(—|-|–|·)?\s*(GDD\s*v?\d+(\.\d+)*(\s*[–-]\s*\d+(\.\d+)*)?(\s*&\s*\d+(\.\d+)*)*|ADR-\d+|BẤT BIẾN GDD\s*\d*:?)", RegexOptions.Compiled);
        static readonly Regex Spaces = new Regex(@"[ \t]{2,}", RegexOptions.Compiled);

        public static string Clean(string text)
        {
            if (string.IsNullOrEmpty(text) || (text.IndexOf("GDD") < 0 && text.IndexOf("ADR-") < 0 && text.IndexOf("Sổ tay v") < 0
                && text.IndexOf("TẠM") < 0 && text.IndexOf("GTTC") < 0 && text.IndexOf("Paper-Doll") < 0
                && text.IndexOf("HANDBOOK") < 0 && text.IndexOf("Bảng ") < 0 && !Regex.IsMatch(text, @"\bP\d\.\d{2}\b")))
                return text;
            string s = Paren.Replace(text, "");
            s = Inline.Replace(s, "");
            s = Regex.Replace(s, @"\bTẠM:?\s*", "", RegexOptions.IgnoreCase);
            s = Regex.Replace(s, @"\bGTTC\b", "quy đổi", RegexOptions.IgnoreCase);
            s = Regex.Replace(s, @"\bPaper-Doll\b", "ngoại trang", RegexOptions.IgnoreCase);
            s = Spaces.Replace(s, " ").Replace(" .", ".").Replace(" !", "!").Replace(" ,", ",").Replace(" :", ":");
            return s.Trim();
        }
    }
}
