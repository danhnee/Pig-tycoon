using UnityEngine;

namespace PigTycoon.PlayableDemo3D
{
    /// <summary>Add to an authored prop or building doorway. This component creates no art or fallback mesh.</summary>
    public sealed class PlayableFarmStation : MonoBehaviour
    {
        public string StationId = "";
        public string DisplayName = "";
        public FarmInteractionKind Kind;
        public float ReachMeters = 2.5f;
        public string CoreInstanceId = "";
        public GameObject CompletedFenceVisual;
        public Collider CompletedFenceCollider;
        public bool InitiallyBuilt;
        public bool ProvidesHerdShelter;
        public bool ProvidesCooling;
        public Transform ProjectileOrigin;

        [Header("Interactive Building Entry & Inspection Camera")]
        public bool HasInteriorView;
        public Vector3 InspectionCameraPosition;
        public Vector3 InspectionCameraLookAt;
        public string BuildingSummary = "";

        public FarmInteractionPoint Point => new FarmInteractionPoint(
            string.IsNullOrWhiteSpace(StationId) ? gameObject.name : StationId, Kind, transform.position.x, transform.position.z)
        { ReachMeters = ReachMeters, CoreInstanceId = CoreInstanceId };

        public bool ApplyState(PlayableFarmState state)
        {
            if (Kind != FarmInteractionKind.Construction) return false;
            var fence = state.Fences.Find(f => f.Id == Point.Id);
            bool built = fence != null ? fence.Hp > 0 : InitiallyBuilt || state.TemporaryFenceIds.Contains(Point.Id);
            bool changed = CompletedFenceCollider != null && CompletedFenceCollider.enabled != built;
            if (CompletedFenceVisual != null) CompletedFenceVisual.SetActive(built);
            if (CompletedFenceCollider != null) CompletedFenceCollider.enabled = built;
            return changed;
        }
    }
}
