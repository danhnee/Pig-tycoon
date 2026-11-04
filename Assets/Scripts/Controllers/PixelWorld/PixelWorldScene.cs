using UnityEngine;

namespace PigTycoon.Presentation
{
    /// <summary>Marks scenes participating in the persistent 2D world.</summary>
    public sealed class PixelWorldScene : MonoBehaviour
    {
        public string LocationId = "Farm";
        public Vector2 DefaultArrival;
    }
}
