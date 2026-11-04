using System;
using System.Collections.Generic;
using UnityEngine;

namespace PigTycoon.Presentation
{
    /// <summary>
    /// All animation clips of one sprite family (khoa, pig, crawler...), produced by the 3D→pixel pipeline
    /// (tools/pixelart) and assembled by PixelMythLibraryBuilder. Directions follow the render rows:
    /// down (facing the camera), left, up, right. Referencing sprites as assets keeps them in builds
    /// without the Resources/*.bytes packaging the older single-PNG loader needs.
    /// </summary>
    [CreateAssetMenu(menuName = "Pig Tycoon/Pixel Myth Sprite Library")]
    public sealed class PixelMythSpriteLibrary : ScriptableObject
    {
        [Serializable]
        public sealed class Clip
        {
            public string Name;
            public float Fps = 10f;
            /// <summary>Ground covered by one loop of a locomotion clip, in metres (sheet json "stride_m_per_cycle"; 0 = unknown).</summary>
            public float StrideMetersPerCycle;
            /// <summary>Speed (m/s) at which the feet do not slide when played at <see cref="Fps"/>; 0 = unknown.</summary>
            public float NaturalSpeed => StrideMetersPerCycle > 0f && Fps > 0f && Down.Length > 0 ? StrideMetersPerCycle * Fps / Down.Length : 0f;
            public bool Loop = true;
            [Tooltip("Frame index on which a one-shot clip lands its effect (hit, pour, pick-up).")]
            public int EffectFrame = -1;
            public Sprite[] Down = Array.Empty<Sprite>();
            public Sprite[] Left = Array.Empty<Sprite>();
            public Sprite[] Up = Array.Empty<Sprite>();
            public Sprite[] Right = Array.Empty<Sprite>();

            public Sprite[] For(CharacterFacing facing)
            {
                switch (facing)
                {
                    case CharacterFacing.Up: return Up;
                    case CharacterFacing.Left: return Left;
                    case CharacterFacing.Right:
                    case CharacterFacing.Side: return Right;
                    default: return Down;
                }
            }

            public float Duration => Fps > 0f && Down.Length > 0 ? Down.Length / Fps : 0f;
        }

        public string Family;
        public List<Clip> Clips = new List<Clip>();

        private Dictionary<string, Clip> byName;

        public Clip Get(string name)
        {
            if (byName == null || byName.Count != Clips.Count)
            {
                byName = new Dictionary<string, Clip>(StringComparer.OrdinalIgnoreCase);
                foreach (var c in Clips)
                {
                    if (c != null && !string.IsNullOrEmpty(c.Name)) byName[c.Name] = c;
                }
            }
            return byName.TryGetValue(name, out var clip) ? clip : null;
        }

        public bool Has(string name) => Get(name) != null;
    }

    /// <summary>
    /// Frame stepper shared by player, NPC, pig and enemy views. Loops a base clip (idle/walk/run) and plays
    /// one-shot clips (attack, interact, whistle, eat, hit, death) on top, firing a callback on the effect frame.
    /// </summary>
    public sealed class PixelMythPlayer
    {
        private readonly PixelMythSpriteLibrary library;
        private readonly SpriteRenderer renderer;
        private PixelMythSpriteLibrary.Clip baseClip;
        private PixelMythSpriteLibrary.Clip oneShot;
        private float baseTime;
        private float oneShotTime;
        private int lastOneShotFrame = -1;
        private Action onEffect;
        private Action onDone;
        private float oneShotSpeed = 1f;

        public PixelMythPlayer(PixelMythSpriteLibrary library, SpriteRenderer renderer)
        {
            this.library = library;
            this.renderer = renderer;
        }

        public bool IsPlayingOneShot => oneShot != null;
        public PixelMythSpriteLibrary Library => library;

        public void SetBase(string clipName)
        {
            var clip = library.Get(clipName) ?? library.Get("idle");
            if (clip == baseClip) return;
            // walk <-> run keeps the stride phase (same foot forward), so the switch does not hitch
            bool locomotion = baseClip != null && clip != null && IsLocomotion(baseClip.Name) && IsLocomotion(clip.Name);
            float phase = locomotion && baseClip.Fps > 0f && baseClip.Down.Length > 0 ? baseTime * baseClip.Fps / baseClip.Down.Length : 0f;
            baseClip = clip;
            baseTime = locomotion && clip.Fps > 0f && clip.Down.Length > 0 ? (phase - Mathf.Floor(phase)) * clip.Down.Length / clip.Fps : 0f;
        }

        static bool IsLocomotion(string name) => name == "walk" || name == "run" || name == "move";

        /// <summary>Base-clip playback rate (1 = authored fps). Locomotion sets it from real speed so feet do not slide.</summary>
        public float BaseRate { get; set; } = 1f;
        public PixelMythSpriteLibrary.Clip BaseClip => baseClip;
        /// <summary>The clip and frame index on screen now (paper-doll gear layers draw the same frame of their own sheet).</summary>
        public string ShownClip { get; private set; } = "idle";
        public int ShownFrame { get; private set; }

        /// <summary>The chain the current one-shot was picked from (gear layers fall back along it).</summary>
        public IReadOnlyList<string> ShownChain { get; private set; } = Array.Empty<string>();

        /// <summary>Plays the first clip of <paramref name="chain"/> this family has (Core AnimationClips fallbacks).</summary>
        public bool PlayFirst(IReadOnlyList<string> chain, Action effect = null, Action done = null, float speed = 1f)
        {
            if (chain != null)
                foreach (var name in chain)
                    if (library.Has(name))
                    {
                        bool ok = PlayOnce(name, effect, done, speed);
                        ShownChain = chain;
                        return ok;
                    }
            effect?.Invoke();
            done?.Invoke();
            return false;
        }

        public bool PlayOnce(string clipName, Action effect = null, Action done = null, float speed = 1f)
        {
            var clip = library.Get(clipName);
            if (clip == null)
            {
                effect?.Invoke();
                done?.Invoke();
                return false;
            }
            onDone?.Invoke();
            ShownChain = Array.Empty<string>();
            oneShot = clip;
            oneShotTime = 0f;
            lastOneShotFrame = -1;
            onEffect = effect;
            onDone = done;
            oneShotSpeed = Mathf.Max(0.2f, speed);
            return true;
        }

        public void Tick(float dt, CharacterFacing facing)
        {
            if (renderer == null) return;
            renderer.flipX = false;
            if (oneShot != null)
            {
                oneShotTime += dt * oneShotSpeed;
                var frames = oneShot.For(facing);
                int n = frames.Length;
                int i = n == 0 ? 0 : Mathf.FloorToInt(oneShotTime * oneShot.Fps);
                int effectAt = oneShot.EffectFrame >= 0 ? oneShot.EffectFrame : n / 2;
                for (int f = lastOneShotFrame + 1; f <= Mathf.Min(i, n - 1); f++)
                {
                    if (f == effectAt)
                    {
                        var e = onEffect;
                        onEffect = null;
                        e?.Invoke();
                    }
                }
                lastOneShotFrame = Mathf.Max(lastOneShotFrame, Mathf.Min(i, n - 1));
                if (i >= n)
                {
                    var e = onEffect;
                    onEffect = null;
                    e?.Invoke();
                    var d = onDone;
                    onDone = null;
                    oneShot = null;
                    ShownChain = Array.Empty<string>();
                    d?.Invoke();
                }
                else
                {
                    if (frames[i] != null) renderer.sprite = frames[i];
                    ShownClip = oneShot.Name;
                    ShownFrame = i;
                    return;
                }
            }
            if (baseClip == null) SetBase("idle");
            if (baseClip == null) return;
            baseTime += dt * Mathf.Max(0f, BaseRate);
            var loop = baseClip.For(facing);
            if (loop.Length == 0) return;
            int k = Mathf.FloorToInt(baseTime * baseClip.Fps);
            k = baseClip.Loop ? k % loop.Length : Mathf.Min(k, loop.Length - 1);
            if (loop[k] != null) renderer.sprite = loop[k];
            ShownClip = baseClip.Name;
            ShownFrame = k;
        }
    }
}
