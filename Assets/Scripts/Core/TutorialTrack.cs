namespace PigTycoon.Core
{
    public sealed class TutorialFacts
    {
        public bool PlayedAsAn;
        public bool TookWalkStep;
        public bool StoodAtGate;
        public bool PlacedHorizontalFence;
        public bool PigAte;
        public bool ReadCareBars;
        public bool TreatedOrIsolated;
        public bool MarketGlance;
    }

    /// <summary>
    /// Tám bước tutorial. Bước sau chỉ tính khi các bước trước đã xong.
    /// </summary>
    public static class TutorialTrack
    {
        public const int StepCount = 8;

        public static int CompletedPrefix(TutorialFacts facts)
        {
            bool[] gates =
            {
                facts.PlayedAsAn,
                facts.TookWalkStep,
                facts.StoodAtGate,
                facts.PlacedHorizontalFence,
                facts.PigAte,
                facts.ReadCareBars,
                facts.TreatedOrIsolated,
                facts.MarketGlance
            };

            int done = 0;
            for (int i = 0; i < gates.Length; i++)
            {
                if (!gates[i])
                {
                    break;
                }

                done += 1;
            }

            return done;
        }
    }
}
