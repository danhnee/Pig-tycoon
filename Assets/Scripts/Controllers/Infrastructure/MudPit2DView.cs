using System.Collections.Generic;
using UnityEngine;

namespace PigTycoon.Presentation
{
    [RequireComponent(typeof(Collider2D))]
    public class MudPit2DView : MonoBehaviour
    {
        [Header("Mud Bath Attributes")]
        public float CoolingEffectiveness = 2.0f;
        public float MudHygienePenaltyPerSec = 0.1f;

        [Header("Occupants")]
        private readonly HashSet<PigAgentView> bathingPigs = new HashSet<PigAgentView>();

        public int OccupantCount => bathingPigs.Count;

        private void OnTriggerEnter2D(Collider2D other)
        {
            var pig = other.GetComponent<PigAgentView>();
            if (pig != null)
            {
                bathingPigs.Add(pig);
                pig.SetInMudPit(true);
            }
        }

        private void OnTriggerExit2D(Collider2D other)
        {
            var pig = other.GetComponent<PigAgentView>();
            if (pig != null)
            {
                bathingPigs.Remove(pig);
                pig.SetInMudPit(false);
            }
        }
    }
}
