using System;
using System.Collections.Generic;
using System.Linq;

namespace PigTycoon.Core
{
    /// <summary>One speaking character: display name, nameplate colour, the portrait ids the art set must draw, voice blip.</summary>
    public sealed class CastMember
    {
        public string Id = "";
        public string DisplayName = "";
        public string Role = "";
        public string NameplateColor = "#E8C27A";
        /// <summary>Portrait ids this character needs (UI falls back to neutral when the art is missing).</summary>
        public HashSet<string> Portraits = new HashSet<string>(StringComparer.Ordinal);
        /// <summary>Text "blip" pitch while the line types out (1 = default).</summary>
        public float BlipPitch = 1f;
        public string VoiceNote = "";
    }

    /// <summary>
    /// Speaking cast of Season 1 (Docs/COT_TRUYEN_PIG_TYCOON.md). This list is also the portrait art request for the UI:
    /// <see cref="PortraitManifest"/> prints every (character, portrait) pair the dialogue can show.
    /// </summary>
    public static class NarrativeCast
    {
        static readonly Dictionary<string, CastMember> byId = new Dictionary<string, CastMember>(StringComparer.OrdinalIgnoreCase);
        public static IEnumerable<CastMember> All => byId.Values;

        public static CastMember Get(string id) => string.IsNullOrEmpty(id) ? null : byId.TryGetValue(id, out var c) ? c : null;

        static readonly string[] Standard = { Portraits.Neutral, Portraits.Happy, Portraits.Sad, Portraits.Angry, Portraits.Surprised, Portraits.Thinking, Portraits.Worried };

        static void Add(string id, string name, string role, string color, float pitch, string voice, params string[] portraits)
        {
            var c = new CastMember { Id = id, DisplayName = name, Role = role, NameplateColor = color, BlipPitch = pitch, VoiceNote = voice };
            foreach (var p in portraits.Length == 0 ? Standard : portraits) c.Portraits.Add(p);
            c.Portraits.Add(Portraits.Neutral);
            byId[id] = c;
        }

        static NarrativeCast()
        {
            // Protagonists
            Add(CastIds.An, "An", "Người chơi", "#F2B8A0", 1.15f,
                "Ấm áp, lễ phép, bình thường quá mức; vết lệch rất khẽ (cười hơi lâu, nhìn hơi lâu).",
                Portraits.Neutral, Portraits.Happy, Portraits.Laugh, Portraits.Sad, Portraits.Surprised, Portraits.Thinking, Portraits.Worried,
                Portraits.AnTooBright, Portraits.AnStare, Portraits.AnSilentTilt);
            Add(CastIds.Khoa, "Khoa", "Người chơi", "#9FB7C9", 0.9f,
                "Vô hồn, câu ngắn, giọng biên bản. Không có biểu cảm; cảm xúc chỉ hiện trong bóng phản chiếu.",
                Portraits.Neutral, Portraits.KhoaReflectFrown, Portraits.KhoaReflectSmile, Portraits.KhoaReflectCry);
            NarrativeCastContent.Register(Add);
        }

        /// <summary>Every (characterId, portraitId) pair, for the art request in the UI contract.</summary>
        public static IEnumerable<(string character, string portrait)> PortraitManifest()
            => All.OrderBy(c => c.Id, StringComparer.Ordinal).SelectMany(c => c.Portraits.OrderBy(p => p, StringComparer.Ordinal).Select(p => (c.Id, p)));

        internal delegate void AddFn(string id, string name, string role, string color, float pitch, string voice, params string[] portraits);
    }

    public static class CastIds
    {
        public const string An = "an", Khoa = "khoa";
        public const string BaSau = "ba_sau", Man = "man", TamVa = "tam_va", Dung = "dung", Cuc = "cuc", Le = "le",
            AnhCa = "anh_ca", OngTon = "ong_ton", AnhTu = "anh_tu", BiNhan = "bi_nhan";
        public const string Do = "do", Mien = "mien", Chot = "chot", TuTroi = "tu_troi", Van = "van";
    }
}
