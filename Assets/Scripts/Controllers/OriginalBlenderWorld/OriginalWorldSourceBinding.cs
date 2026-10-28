using UnityEngine;

namespace PigTycoon.Candidates.EvaluatedExport
{
    /// <summary>Explicit source/run identity and remaining verification scope on the imported world.</summary>
    public sealed class OriginalWorldSourceBinding : MonoBehaviour
    {
        public string SourceBlendPath;
        public string SourceSha256;
        public string ExportRunId;
        public string ManifestSha256;
        public long ExpectedInstanceRecords;
        public string GeometryScope = "ACTIVE_VIEW_LAYER_VIEWPORT_EVALUATED_STATIC_FRAME";
        public string MaterialMappingStatus;
        public string PhysicsStatus = "SOURCE_SURFACE_COLLIDER_PLAN_NOT_ATTACHED";
        public string ServiceAnchorStatus = "SOURCE_SURFACE_SEMANTIC_ANCHORS_NOT_ATTACHED";
        public EvaluatedMapViewer Viewer;
    }
}
