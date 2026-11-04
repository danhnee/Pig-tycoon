using System.Collections.Generic;
using UnityEngine;

namespace PigTycoon.Presentation
{
    /// <summary>What a player action looks like in the world, so it reads without text (Stardew-style feedback).</summary>
    public enum ActionCue
    {
        ScoopFeed,   // bran from the silo into the sack
        PourFeed,    // sack into the trough
        DrawWater,   // well into the bucket
        PourWater,   // bucket into the basin
        Repair,      // hammer on a fence / structure
        Dismantle,   // hammer takes something down
        Place,       // a fence / trough / building set down
        PetPig,      // brushing / petting a pig
        Clean,       // sweeping / lime on a plot
        PickUp,      // something went into hand / bag
        Deny         // could not do it (empty, full, too far, wrong tool)
    }

    /// <summary>
    /// One call per player action: plays the matching body clip, a short world-space effect at the target and, for items,
    /// the item's own icon rising from the target (the "you got this" pop). Success needs no words; Deny shows a red cross
    /// on the target plus a short toast that says why.
    /// Art: if Argon ships <c>Art/PixelMyth/FX/fx_&lt;cue&gt;.png</c> (horizontal strip of square frames, 32 PPU) it is
    /// used; until then a small procedural burst in the cue's colour is drawn, so the system works before the art lands.
    /// </summary>
    public sealed class ActionFx : MonoBehaviour
    {
        static ActionFx instance;
        readonly List<Fx> live = new List<Fx>();
        static readonly Dictionary<ActionCue, Sprite[]> stripCache = new Dictionary<ActionCue, Sprite[]>();

        sealed class Fx
        {
            public Transform T; public SpriteRenderer R; public Vector3 Start, Vel; public float Age, Life, Spin;
            public Sprite[] Frames; public bool Fade = true; public float Scale0 = 1f; public bool Shake;
        }

        /// <summary>Body clip for the cue ("interact_feed" etc. with fallback to "interact"), or null for none.</summary>
        public static string WorkClip(ActionCue cue) => cue switch
        {
            ActionCue.ScoopFeed or ActionCue.PourFeed => "feed",
            ActionCue.DrawWater or ActionCue.PourWater => "water",
            ActionCue.Repair or ActionCue.Dismantle or ActionCue.Place => "repair",
            ActionCue.PetPig => "observe",
            ActionCue.Clean => "clean",
            ActionCue.PickUp => "gather",
            _ => null
        };

        static Color Tint(ActionCue cue) => cue switch
        {
            ActionCue.ScoopFeed or ActionCue.PourFeed => new Color(0.86f, 0.70f, 0.38f),
            ActionCue.DrawWater or ActionCue.PourWater => new Color(0.45f, 0.75f, 1f),
            ActionCue.Repair or ActionCue.Place => new Color(1f, 0.86f, 0.35f),
            ActionCue.Dismantle => new Color(0.72f, 0.55f, 0.36f),
            ActionCue.Clean => new Color(0.95f, 0.98f, 1f),
            ActionCue.PetPig => new Color(1f, 0.45f, 0.55f),
            _ => Color.white
        };

        /// <summary>Plays the cue at <paramref name="target"/>. <paramref name="actor"/> (the player) gets the body clip.
        /// <paramref name="itemKey"/>: the item that moved (its icon pops up); <paramref name="denyReason"/>: toast for Deny.</summary>
        public static void Play(ActionCue cue, Vector3 target, GameObject actor = null, string itemKey = null, string denyReason = null)
        {
            if (instance == null) instance = new GameObject("ActionFx").AddComponent<ActionFx>();
            if (actor != null && WorkClip(cue) is string clip) actor.GetComponent<CharacterSpriteAnimator>()?.TriggerWork(clip);
            if (cue == ActionCue.Deny)
            {
                Deny(target, !string.IsNullOrEmpty(itemKey) ? UISpriteLoader.GetLootSprite(itemKey) : null, denyReason);
                return;
            }
            var strip = Strip(cue);
            if (strip != null && strip.Length > 0)
                instance.SpawnStrip(strip, target + new Vector3(0f, 0.3f, 0f), 0.45f);
            else
                instance.Burst(cue, target);
            if (cue == ActionCue.PetPig)
                instance.Spawn(UISpriteLoader.GetIconHeartFull(), target + new Vector3(0f, 1.0f, 0f), Vector3.up * 0.9f, 0.9f, 0.45f);
            if (!string.IsNullOrEmpty(itemKey) && UISpriteLoader.GetLootSprite(itemKey) is Sprite icon)
                instance.Spawn(icon, target + new Vector3(0f, 0.8f, 0f), Vector3.up * 1.1f, 0.8f, 0.6f);
        }

        /// <summary>
        /// "Can't": the thing that is missing or in the way (bran, water, the full trough, the tool) flashes red and shakes
        /// over the target, with a 2–4 word caption under it ("Hết cám", "Máng đầy"). No X mark, no toast (4 Nov user rule).
        /// </summary>
        public static void Deny(Vector3 target, Sprite icon, string shortText)
        {
            if (instance == null) instance = new GameObject("ActionFx").AddComponent<ActionFx>();
            var at = target + new Vector3(0f, 1.0f, 0f);
            if (icon != null)
            {
                var fx = instance.Spawn(icon, at, Vector3.zero, 0.9f, 1.2f);
                fx.R.color = new Color(1f, 0.32f, 0.28f);
                fx.Shake = true; fx.Fade = true;
            }
            // research 4 Nov (Docs/research/2026-11-04-tieu-chuan-game-pixel-du-tot.md §2): icon + shake first, words only
            // when the player tries the same thing again (or there is no icon to say it)
            bool retry = shortText == lastDenyText && Time.unscaledTime - lastDenyAt < 6f;
            lastDenyText = shortText; lastDenyAt = Time.unscaledTime;
            if (!string.IsNullOrEmpty(shortText) && (retry || icon == null))
                instance.Caption(at + new Vector3(0f, icon != null ? -0.55f : 0f, 0f), shortText, 1.6f);
        }

        static string lastDenyText;
        static float lastDenyAt = -99f;

        void Caption(Vector3 at, string text, float life)
        {
            var go = new GameObject("fx_caption");
            go.transform.SetParent(transform, false);
            go.transform.position = at;
            var t = go.AddComponent<TMPro.TextMeshPro>();
            t.text = text; t.fontSize = 2.2f; t.alignment = TMPro.TextAlignmentOptions.Center;
            t.color = new Color(1f, 0.93f, 0.85f); t.outlineWidth = 0.25f; t.outlineColor = new Color32(40, 12, 8, 255);
            t.sortingOrder = 32001; t.textWrappingMode = TMPro.TextWrappingModes.NoWrap;
            captions.Add((t, 0f, life));
        }

        readonly List<(TMPro.TextMeshPro t, float age, float life)> captions = new List<(TMPro.TextMeshPro, float, float)>();

        static Sprite[] Strip(ActionCue cue)
        {
            if (stripCache.TryGetValue(cue, out var s)) return s;
            Sprite[] frames = null;
            var sheet = UISpriteLoader.LoadSprite($"Art/PixelMyth/FX/fx_{cue.ToString().ToLowerInvariant()}.png", 32);
            if (sheet != null)
            {
                var tex = sheet.texture; int size = tex.height, n = Mathf.Max(1, tex.width / Mathf.Max(1, size));
                frames = new Sprite[n];
                for (int i = 0; i < n; i++)
                    frames[i] = Sprite.Create(tex, new Rect(i * size, 0, size, size), new Vector2(0.5f, 0.5f), 32f);
            }
            stripCache[cue] = frames;
            return frames;
        }

        static Sprite pixel;
        static Sprite Pixel()
        {
            if (pixel != null) return pixel;
            var t = new Texture2D(2, 2) { filterMode = FilterMode.Point };
            t.SetPixels(new[] { Color.white, Color.white, Color.white, Color.white }); t.Apply();
            return pixel = Sprite.Create(t, new Rect(0, 0, 2, 2), new Vector2(0.5f, 0.5f), 32f);
        }

        void Burst(ActionCue cue, Vector3 at)
        {
            var c = Tint(cue);
            int n = cue == ActionCue.Clean ? 10 : 8;
            bool falls = cue is ActionCue.PourFeed or ActionCue.PourWater;
            for (int i = 0; i < n; i++)
            {
                float a = Random.Range(0f, Mathf.PI * 2f);
                var v = falls ? new Vector3(Random.Range(-0.6f, 0.6f), Random.Range(1.4f, 2.2f), 0f)
                              : new Vector3(Mathf.Cos(a), Mathf.Sin(a) * 0.6f + 0.6f, 0f) * Random.Range(0.8f, 1.6f);
                var fx = Spawn(Pixel(), at + new Vector3(0f, falls ? 0.6f : 0.3f, 0f), v, Random.Range(0.35f, 0.6f), Random.Range(1.5f, 3f));
                fx.R.color = Color.Lerp(c, Color.white, Random.Range(0f, 0.25f));
                fx.Spin = falls ? 0f : Random.Range(-360f, 360f);
            }
        }

        Fx Spawn(Sprite sprite, Vector3 at, Vector3 velocity, float life, float scale)
        {
            var go = new GameObject("fx");
            go.transform.SetParent(transform, false);
            go.transform.position = at;
            go.transform.localScale = Vector3.one * scale;
            var r = go.AddComponent<SpriteRenderer>();
            r.sprite = sprite; r.sortingOrder = 32000;
            var fx = new Fx { T = go.transform, R = r, Start = at, Vel = velocity, Life = life, Scale0 = scale };
            live.Add(fx);
            return fx;
        }

        void SpawnStrip(Sprite[] frames, Vector3 at, float life)
        {
            var fx = Spawn(frames[0], at, Vector3.zero, life, 1f);
            fx.Frames = frames; fx.Fade = false;
        }

        void Update()
        {
            float dt = Time.deltaTime;
            for (int i = live.Count - 1; i >= 0; i--)
            {
                var f = live[i];
                f.Age += dt;
                float k = Mathf.Clamp01(f.Age / f.Life);
                if (f.Shake)
                {
                    // a quick horizontal jolt that settles, then a fade
                    float amp = 0.12f * (1f - Mathf.Clamp01(f.Age / 0.45f));
                    f.T.position = f.Start + new Vector3(Mathf.Sin(f.Age * 60f) * amp, 0f, 0f);
                    if (f.Age > 0.6f) { var c = f.R.color; c.a = 1f - Mathf.Clamp01((f.Age - 0.6f) / 0.3f); f.R.color = c; }
                }
                else if (f.Frames != null) f.R.sprite = f.Frames[Mathf.Min(f.Frames.Length - 1, (int)(k * f.Frames.Length))];
                else if (!f.Shake)
                {
                    f.Vel += Vector3.down * 3.5f * dt * (f.R.sprite == pixel ? 1f : 0f); // particles fall, icons float
                    f.T.position += f.Vel * dt;
                    if (f.Spin != 0f) f.T.Rotate(0f, 0f, f.Spin * dt);
                    if (f.Fade) { var c = f.R.color; c.a = 1f - k * k; f.R.color = c; }
                }
                if (f.Age >= f.Life) { Destroy(f.T.gameObject); live.RemoveAt(i); }
            }
            for (int i = captions.Count - 1; i >= 0; i--)
            {
                var (t, age, life) = captions[i];
                age += dt;
                if (t == null || age >= life) { if (t != null) Destroy(t.gameObject); captions.RemoveAt(i); continue; }
                var c = t.color; c.a = age > life - 0.3f ? (life - age) / 0.3f : 1f; t.color = c;
                captions[i] = (t, age, life);
            }
        }
    }
}
