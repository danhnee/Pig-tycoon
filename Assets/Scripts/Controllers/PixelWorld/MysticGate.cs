using System.Collections;
using PigTycoon.Core;
using UnityEngine;

namespace PigTycoon.Presentation
{
    /// <summary>
    /// The occult-event gate (story: a hastily patched gate that pushes up out of the ground wherever the event
    /// strikes, then sinks away). Nothing stands on the map while no event is open: on the Farm the gate picks a
    /// random free spot inside one of the gate-event zones each time Core opens an offer, rises out of the earth,
    /// loops its active mist, and sinks when the offer/expedition ends. In the forest the same gate stands at the
    /// arrival point for as long as the expedition lasts. Owns the portal's visibility (PixelWorldPortal defers).
    /// </summary>
    [RequireComponent(typeof(PixelWorldPortal))]
    public sealed class MysticGate : MonoBehaviour
    {
        public Rect[] Zones = new Rect[0];          // Farm only; empty = stay where the layout put the portal
        public float RiseSeconds = 1.4f;
        public float SinkSeconds = 1.0f;
        public float ClearRadius = 1.8f;
        public float BuildingClearance = 6f;        // GDD G589/G637/G1221: >= 6 m from buildings, off herd lanes

        public bool IsUp { get; private set; }
        public static MysticGate FarmGate { get; private set; }

        private SpriteRenderer body;
        private PixelMythSpriteLibrary library;
        private PixelMythSpriteLibrary.Clip active;
        private Coroutine motion;
        private float animTime;
        private bool wasOpen;
        private PixelWorldPortal portal;

        private void Awake()
        {
            portal = GetComponent<PixelWorldPortal>();
            if (Zones.Length > 0) FarmGate = this;
            var go = new GameObject("GateBody");
            go.transform.SetParent(transform, false);
            body = go.AddComponent<SpriteRenderer>();
            body.enabled = false;
            library = PixelMythRegistry.Find("mystic_gate");
            active = library != null ? (library.Get("active") ?? library.Get("idle")) : null;
        }

        private void OnDestroy() { if (FarmGate == this) FarmGate = null; }

        private static bool EventOpen()
        {
            var engine = MobileGameController.Instance?.Engine;
            return engine != null && (engine.ForestAndEvents.Expedition.IsActive || engine.ForestAndEvents.Director.HasOpenGate(engine));
        }

        private void Update()
        {
            bool open = EventOpen();
            if (open && !wasOpen) Raise();
            else if (!open && wasOpen) Lower();
            wasOpen = open;

            if (IsUp && motion == null && active != null && active.Down.Length > 0)
            {
                animTime += Time.deltaTime;
                int i = Mathf.FloorToInt(animTime * Mathf.Max(1f, active.Fps)) % active.Down.Length;
                body.sprite = active.Down[i];
            }
            body.sortingOrder = Mathf.RoundToInt(-transform.position.y * 100f);
        }

        /// <summary>Raise the gate now (also used when the scene loads with an event already open).</summary>
        public void Raise(Vector2? near = null, float spread = 0.35f)
        {
            wasOpen = true;
            if (Zones.Length > 0) transform.position = PickSpot(near, spread);
            if (motion != null) StopCoroutine(motion);
            motion = StartCoroutine(RiseRoutine());
        }

        public void Lower()
        {
            wasOpen = false;
            if (!IsUp) return;
            if (motion != null) StopCoroutine(motion);
            motion = StartCoroutine(SinkRoutine());
        }

        /// <summary>Random free spot inside a random gate zone (or the zone nearest <paramref name="near"/>).</summary>
        private Vector3 PickSpot(Vector2? near, float spread)
        {
            var rng = new System.Random(unchecked(System.Environment.TickCount ^ GetHashCode()));
            int zi = rng.Next(Zones.Length);
            if (near.HasValue)
            {
                float best = float.MaxValue;
                for (int i = 0; i < Zones.Length; i++)
                {
                    float d = (Zones[i].center - near.Value).sqrMagnitude;
                    if (d < best) { best = d; zi = i; }
                }
            }
            var z = Zones[zi];
            for (int attempt = 0; attempt < 24; attempt++)
            {
                var p = new Vector2(z.xMin + (float)rng.NextDouble() * z.width, z.yMin + (float)rng.NextDouble() * z.height);
                if (near.HasValue && attempt < 12) p = Vector2.Lerp(near.Value, p, spread);   // still random, but within view
                bool blocked = false;
                foreach (var hit in Physics2D.OverlapCircleAll(p, ClearRadius))
                    if (!hit.isTrigger && hit.GetComponentInParent<PlayerMobileController>() == null && hit.GetComponentInParent<PigAgentView>() == null)
                    { blocked = true; break; }
                if (!blocked)
                    foreach (var hit in Physics2D.OverlapCircleAll(p, BuildingClearance))
                        if (hit.name.StartsWith("building:")) { blocked = true; break; }
                if (!blocked) return new Vector3(p.x, p.y, 0f);
            }
            return new Vector3(z.center.x, z.center.y, 0f);
        }

        private IEnumerator RiseRoutine()
        {
            IsUp = true;
            animTime = 0f;
            body.enabled = true;
            if (active != null && active.Down.Length > 0) body.sprite = active.Down[0];
            float t = 0f;
            while (t < RiseSeconds)
            {
                t += Time.deltaTime;
                float k = Mathf.Clamp01(t / RiseSeconds);
                float e = 1f - Mathf.Pow(1f - k, 3f);                 // ease-out: pushes up fast, settles slowly
                float shake = (1f - k) * 0.06f * Mathf.Sin(t * 70f);
                body.transform.localScale = new Vector3(1f + (1f - e) * 0.15f, e, 1f);
                body.transform.localPosition = new Vector3(shake, 0f, 0f);
                body.color = new Color(1f, 1f, 1f, Mathf.Clamp01(k * 2.5f));
                yield return null;
            }
            body.transform.localScale = Vector3.one;
            body.transform.localPosition = Vector3.zero;
            body.color = Color.white;
            motion = null;
        }

        private IEnumerator SinkRoutine()
        {
            float t = 0f;
            while (t < SinkSeconds)
            {
                t += Time.deltaTime;
                float k = Mathf.Clamp01(t / SinkSeconds);
                body.transform.localScale = new Vector3(1f + k * 0.1f, 1f - k, 1f);
                body.color = new Color(1f, 1f, 1f, 1f - k * k);
                yield return null;
            }
            body.enabled = false;
            IsUp = false;
            motion = null;
        }
    }
}
