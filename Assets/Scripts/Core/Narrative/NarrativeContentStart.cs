using System.Collections.Generic;

namespace PigTycoon.Core
{
    /// <summary>What the character-select screen shows for each protagonist (story bible 2.7, GDD 8.1).</summary>
    public sealed class ProtagonistCard
    {
        public PlayerId Id;
        public string Name = "";
        public string Title = "";
        /// <summary>The narrator's first line (story 1.0 / 1B.0).</summary>
        public string OpeningLine = "";
        public string Tagline = "";
        public string Blurb = "";
        public string TraitName = "";
        public string TraitText = "";
        public string Catchphrase = "";
        /// <summary>Portrait id for the select screen (the hidden expressions are never used here).</summary>
        public string Portrait = Portraits.Neutral;
    }

    public static class NarrativeContentStart
    {
        public static ProtagonistCard Card(PlayerId id) => id == PlayerId.Khoa
            ? new ProtagonistCard
            {
                Id = PlayerId.Khoa,
                Name = "Khoa",
                Title = NewGameSetup.Title(PlayerId.Khoa),
                OpeningLine = "Tôi tên Khoa. Tôi điên.",
                Tagline = "Lấy cương — hỏi thẳng, ghi sổ, đặt vật chứng lên bàn.",
                Blurb = "Gương mặt không đổi, cái bóng trong gương cảm thay anh. Người ta dè chừng anh trước, rồi tin anh như tin một cái cân.",
                TraitName = NewGameSetup.BasicTraitName(PlayerId.Khoa),
                TraitText = "Một tiếng huýt: tối đa 12 heo trong 20 m đi theo Khoa suốt 60 giây.",
                Catchphrase = "“Để rồi tính.”",
                Portrait = Portraits.Neutral,
            }
            : new ProtagonistCard
            {
                Id = PlayerId.An,
                Name = "An",
                Title = NewGameSetup.Title(PlayerId.An),
                OpeningLine = "Tôi tên An. Tôi điên. Có giấy hẳn hoi, nhưng không ai tin, vì tôi cười nhiều quá.",
                Tagline = "Lấy nhu — hỏi một câu ngây ngô, giúp một việc nhỏ, rồi lắng nghe.",
                Blurb = "Ai ở Bờ Rìa cũng thương An ngay từ đầu. Rất lâu sau họ mới thấy lạ, vì chị nhớ hết.",
                TraitName = NewGameSetup.BasicTraitName(PlayerId.An),
                TraitText = "Đứng quan sát một con heo 5 giây là lộ ra một dấu hiệu sức khoẻ hoặc tính chất đang ẩn.",
                Catchphrase = "“Ơ, thật hả?”",
                Portrait = Portraits.Happy,
            };

        /// <summary>No scenes of its own: the prologue files start on NarrativeTrigger.NewGame per protagonist.</summary>
        public static IEnumerable<SceneDefinition> Scenes()
        {
            yield break;
        }
    }
}
