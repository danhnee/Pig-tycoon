using System.Collections.Generic;
using UnityEngine;

namespace PigTycoon.Presentation
{
    [RequireComponent(typeof(Collider2D))]
    public class Shelter2DView : MonoBehaviour
    {
        [Header("Shelter Attributes")]
        public int Capacity = 12;
        public float ComfortRating = 1.2f;

        [Header("Visual")]
        public SpriteRenderer RoofSprite;

        private readonly HashSet<PigAgentView> shelteredPigs = new HashSet<PigAgentView>();

        public int CurrentOccupants => shelteredPigs.Count;
        public bool HasCapacity => CurrentOccupants < Capacity;

        private void OnTriggerEnter2D(Collider2D other)
        {
            var pig = other.GetComponent<PigAgentView>();
            if (pig != null)
            {
                shelteredPigs.Add(pig);
                pig.SetInShelter(true);
            }
        }

        private void OnTriggerExit2D(Collider2D other)
        {
            var pig = other.GetComponent<PigAgentView>();
            if (pig != null)
            {
                shelteredPigs.Remove(pig);
                pig.SetInShelter(false);
            }
        }
    }
}
