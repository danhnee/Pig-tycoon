using PigTycoon.Core;
using UnityEngine;

namespace PigTycoon.Presentation
{
    /// <summary>
    /// Dummy trong CombatSandbox. Máu và đòn đi qua CombatStrike, không tự trừ máu.
    /// </summary>
    public class CombatDummyView : MonoBehaviour
    {
        public int MaxHp = 40;
        public int Hp = 40;
        public int Defense;
        public int Resilience;

        public bool ApplyLightStrike(float stamina, out float staminaAfter, out float dailyLoadAdded)
        {
            var dummy = new CombatDummy(MaxHp, Defense, Resilience);
            dummy.Hp = Hp;
            StrikeResult result = CombatStrike.Light(stamina, dummy);
            if (!result.Landed)
            {
                staminaAfter = stamina;
                dailyLoadAdded = 0f;
                return false;
            }

            Hp = dummy.Hp;
            staminaAfter = stamina - result.StaminaSpent;
            dailyLoadAdded = result.DailyLoadAdded;
            return true;
        }
    }
}
