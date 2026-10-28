using System;
using UnityEngine;

namespace PigTycoon.PlayableDemo3D.Verification
{
    /// <summary>Authored test paths only; creates no geometry and has no normal-play behavior.</summary>
    public sealed class AcceptanceRoute : MonoBehaviour
    {
        public Transform[] CareRoute = Array.Empty<Transform>();
        public int FeedStop = -1, WaterStop = -1, NpcStop = -1, MarketStop = -1;
        public Transform[] RaidRoute = Array.Empty<Transform>();
        public int DefenseStop = -1;
        public Transform[] WallRoute = Array.Empty<Transform>();
        public Collider WallCollider;
        public Transform WallApproach, WallBeyond;
    }
}
