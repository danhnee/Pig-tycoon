using UnityEngine;
using PigTycoon.Core;

namespace PigTycoon.Presentation
{
    public sealed class PixelWorldPortal : MonoBehaviour
    {
        public string PortalId;
        public string TargetLocationId;
        public string TargetPortalId;
        public string DisplayName;
        // Set only after the player explicitly selects an owned event-exit item.
        [System.NonSerialized] public string RequestedExitItemId;
        public float InteractionRadius = 2.2f;
        public Vector2 ArrivalOffset = new Vector2(0, -2.5f);

        Renderer[] renderers;
        bool[] originalRendererStates;
        Canvas[] canvases;
        bool[] originalCanvasStates;
        void Awake()
        {
            renderers = GetComponentsInChildren<Renderer>(true);
            originalRendererStates = System.Array.ConvertAll(renderers, r => r.enabled);
            canvases = GetComponentsInChildren<Canvas>(true);
            originalCanvasStates = System.Array.ConvertAll(canvases, c => c.enabled);
        }
        void LateUpdate()
        {
            // A forest entrance exists visually only while its actual encounter exists.
            if (TargetLocationId != "Forest_Primeval") return;
            if (GetComponent<MysticGate>() != null) return;   // the gate rises/sinks itself
            var engine = MobileGameController.Instance?.Engine;
            bool visible = engine != null && (engine.ForestAndEvents.Expedition.IsActive || engine.ForestAndEvents.Director.HasOpenGate(engine));
            for (int i = 0; i < renderers.Length; i++)
                if (renderers[i] != null) renderers[i].enabled = visible && originalRendererStates[i];
            for (int i = 0; i < canvases.Length; i++)
                if (canvases[i] != null) canvases[i].enabled = visible && originalCanvasStates[i];
        }

    }
}
