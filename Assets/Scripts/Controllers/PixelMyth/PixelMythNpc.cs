using System;
using System.Collections.Generic;
using PigTycoon.Core;
using TMPro;
using UnityEngine;

namespace PigTycoon.Presentation
{
    /// <summary>
    /// Living resident of a pixel map (spawned by PixelMythWorldBuilder from the layout "npcs" array).
    /// Walks a looping route at ~1.2 m/s with 2-6 s pauses and faces its direction of travel; at night
    /// (20:00-05:00 game clock) it goes to the first waypoint (home) and idles. Kinematic Rigidbody2D + small
    /// capsule so the player cannot walk through it; if blocked for more than 2 s it skips to the next waypoint.
    /// Y-sorts every frame (sortingOrder = round(-y*100)). Sprite: library with family == Id, else the "man"
    /// library with a per-NPC tint. Interaction: the player within 1.6 m pressing E (or PixelMythNpc.TalkTo from
    /// the demo autopilot) stops the NPC, turns it to the player, plays "interact" if present and shows a
    /// speech bubble for ~3 s. Greetings are VERBATIM story lines from .collab/claude-2d/story.txt (table below);
    /// residents without a sourced line only show an icon bubble ("...").
    /// Public API: PixelMythNpc.All, NearestTo(pos, range), TalkTo(PlayerMobileController).
    /// </summary>
    [RequireComponent(typeof(SpriteRenderer))]
    public sealed class PixelMythNpc : MonoBehaviour
    {
        public string Id;
        public string DisplayName;
        public Vector2[] Route = Array.Empty<Vector2>();
        public string HomeRoleId;
        public string GreetingKey;

        public const float InteractRange = 1.6f;
        private const float Speed = 1.2f;
        private const float BubbleSeconds = 3f;
        private const float StuckSeconds = 2f;

        // Fallback bubble only (no engine). Story talk comes from NarrativeContentVillage via NarrativeStageController.Talk.
        // Chapter-8 farewell lines (S763–S771) are reserved for the final conversations and must not be used here.
        private static readonly Dictionary<string, string> Greetings = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            { "ba_sau",        "Ăn thêm miếng nữa đi con." },                                                       // S436 / S763
            { "tam_va",        "Còn tốt." },                                                                        // S207
            { "ong_ton",       "Tôi ghi hết là hết. Mực trong sổ chưa bắn hộ ai bao giờ." },                        // S1135 (also S767: "Đã bàn giao. Đủ.")
            { "le",            "Nguồn thì ở ngoài kia. Phí vận chuyển thì ở trong này. Chị nói cái gì tính được trước." }, // S1138 (also S768: "Món này chị không bán. Chị giữ.")
            { "bac_si_dung",   "Cứ đổ cám xuống. Nó sống lại thì mai vẫn phải đi cân." },                           // S1139 (also S769: "Đàn khỏe. Cậu cũng vậy. Đừng để mai mới đi khám.")
            { "dung",          "Cứ đổ cám xuống. Nó sống lại thì mai vẫn phải đi cân." },                           // S1139 (alias id in Hub_Village)
            { "anh_tu",        "Ở đây chưa biết cũng là một ngày trong tuần." },                                    // S243 (also S771: "Xe về chạy lúc nào cũng được. Lần này theo giờ của cậu.")
            { "bi_nhan",       "Bên tôi không nhận hàng đã qua sử dụng." },                                         // S1115
            { "merc_tu_troi",  "Tôi hỏi loại. Trời thì tôi biết rồi." },                                            // S1140
            { "merc_chot",     "Đồng nghiệp đang nghỉ phép." },                                                     // S463
            { "nguoi_ban_dem", "Chỉ nhận heo biết hát." },                                                          // S1058
            { "nguoi_ban_tin", "“Lúc nào” thì tôi bán được. “Tại sao” thì tôi chưa định giá nổi." },                // S1067
            { "nguoi_ao_mua",  "Còn sớm." },                                                                        // S31 / S273
        };

        public static readonly List<PixelMythNpc> All = new List<PixelMythNpc>();

        private SpriteRenderer sr;
        private Rigidbody2D rb;
        private PixelMythPlayer anim;
        private CharacterFacing facing = CharacterFacing.Down;
        private int wp;
        private float pause;
        private float blocked;
        private bool atHome;
        private float talkUntil;
        private GameObject bubble;
        private readonly RaycastHit2D[] hits = new RaycastHit2D[8];
        private ContactFilter2D filter;
        private PlayerMobileController[] players = Array.Empty<PlayerMobileController>();
        private float playerScan;

        public bool IsTalking => Time.time < talkUntil || InScene;
        /// <summary>Held by a story scene (ADR-0027): the NPC stops its routine and only walks to <see cref="ScriptedTarget"/>.</summary>
        public bool InScene { get; set; }
        public Vector2? ScriptedTarget { get; set; }
        public bool ScriptedArrived => !ScriptedTarget.HasValue;
        /// <summary>Cast id in the narrative data (map ids "bac_si_dung" → "dung", "merc_chot" → "chot").</summary>
        public string CastId => Id == "bac_si_dung" ? CastIds.Dung : Id != null && Id.StartsWith("merc_") ? Id.Substring(5) : Id;

        public void FaceToward(CharacterFacing f) => facing = f;
        public void Warp(Vector2 p)
        {
            if (rb != null) rb.position = p;
            transform.position = new Vector3(p.x, p.y, transform.position.z);
        }

        private void OnEnable() { if (!All.Contains(this)) All.Add(this); }
        private void OnDisable() { All.Remove(this); }

        private void Awake()
        {
            sr = GetComponent<SpriteRenderer>();
            rb = GetComponent<Rigidbody2D>();
            if (rb != null) { rb.bodyType = RigidbodyType2D.Kinematic; rb.gravityScale = 0f; }
            filter = new ContactFilter2D { useTriggers = false };
            var lib = PixelMythRegistry.Find(Id);
            if (lib == null)
            {
                lib = PixelMythRegistry.Find("man");
                sr.color = TintFor(Id);
            }
            if (lib != null)
            {
                anim = new PixelMythPlayer(lib, sr);
                anim.SetBase("idle");
            }
            pause = UnityEngine.Random.Range(0.5f, 3f);
        }

        private static Color TintFor(string id)
        {
            int h = 17;
            foreach (var c in id ?? "") h = h * 31 + c;
            float hue = (Mathf.Abs(h) % 360) / 360f;
            return Color.Lerp(Color.white, Color.HSVToRGB(hue, 0.55f, 1f), 0.55f);
        }

        private static int Hour()
        {
            var g = MobileGameController.Instance;
            return g != null && g.Engine != null ? g.Engine.Clock.CurrentHour : 12;
        }

        private void Update()
        {
            sr.sortingOrder = Mathf.RoundToInt(-transform.position.y * 100f);
            if (Time.unscaledTime >= playerScan) { playerScan = Time.unscaledTime + 1f; players = FindObjectsByType<PlayerMobileController>(FindObjectsSortMode.None); }
            if (Input.GetKeyDown(KeyCode.E) && !IsTalking)
            {
                var p = ClosestPlayer();
                if (p != null && IsClosestNpc(p)) Talk(p.transform.position);
            }
            if (bubble != null && !IsTalking) { Destroy(bubble); bubble = null; }
            if (anim != null) anim.Tick(Time.deltaTime, facing);
        }

        private PlayerMobileController ClosestPlayer()
        {
            PlayerMobileController best = null;
            float bd = InteractRange * InteractRange;
            foreach (var p in players)
            {
                if (p == null || !p.isActiveAndEnabled) continue;
                float d = ((Vector2)p.transform.position - (Vector2)transform.position).sqrMagnitude;
                if (d <= bd) { bd = d; best = p; }
            }
            return best;
        }

        private bool IsClosestNpc(PlayerMobileController p)
        {
            float mine = ((Vector2)p.transform.position - (Vector2)transform.position).sqrMagnitude;
            foreach (var n in All)
            {
                if (n == this || n == null) continue;
                if (((Vector2)p.transform.position - (Vector2)n.transform.position).sqrMagnitude < mine) return false;
            }
            return true;
        }

        public static PixelMythNpc NearestTo(Vector2 pos, float range = InteractRange)
        {
            PixelMythNpc best = null;
            float bd = range * range;
            foreach (var n in All)
            {
                if (n == null) continue;
                float d = ((Vector2)n.transform.position - pos).sqrMagnitude;
                if (d <= bd) { bd = d; best = n; }
            }
            return best;
        }

        /// <summary>Greets the player with the nearest NPC in range. Returns that NPC (null if none).</summary>
        public static PixelMythNpc TalkTo(PlayerMobileController player)
        {
            if (player == null) return null;
            var npc = NearestTo(player.transform.position);
            if (npc != null) npc.Talk(player.transform.position);
            return npc;
        }

        /// <summary>Talk: the story runtime picks the scene (first meeting, quest talk, today's line). Without a dialogue
        /// view on screen the first line of that scene is shown as the bubble instead.</summary>
        public void Talk(Vector2 playerPos)
        {
            string line = NarrativeStageController.Talk(this);
            if (line == null) { Greet(playerPos); return; }
            var d = playerPos - (Vector2)transform.position;
            if (d.sqrMagnitude > 0.0001f) facing = CharacterSpriteAnimator.ResolveFacing(d, facing, 1f);
            if (line.Length == 0) return; // the dialogue view shows the scene
            talkUntil = Time.time + Mathf.Clamp(line.Length * 0.06f, BubbleSeconds, 6f);
            anim?.SetBase("idle");
            anim?.PlayOnce("interact");
            if (bubble != null) Destroy(bubble);
            bubble = BuildBubble(line);
        }

        public void Greet(Vector2 playerPos)
        {
            var d = playerPos - (Vector2)transform.position;
            if (d.sqrMagnitude > 0.0001f) facing = FacingOf(d);
            talkUntil = Time.time + BubbleSeconds;
            anim?.SetBase("idle");
            anim?.PlayOnce("interact");
            if (bubble != null) Destroy(bubble);
            bubble = BuildBubble(Greetings.TryGetValue(Id ?? "", out var line) ? line : null);
        }

        private void FixedUpdate()
        {
            if (rb == null) return;
            if (ScriptedTarget.HasValue)
            {
                if (MoveToward(ScriptedTarget.Value) || blocked > StuckSeconds * 2f) { if (blocked > StuckSeconds * 2f) Warp(ScriptedTarget.Value); ScriptedTarget = null; blocked = 0f; }
                return;
            }
            if (IsTalking) { anim?.SetBase("idle"); return; }
            var route = Route;
            if (route == null || route.Length == 0) { anim?.SetBase("idle"); return; }

            int hr = Hour();
            bool night = hr >= 20 || hr < 5;
            if (night)
            {
                wp = 0;
                if (!atHome && MoveToward(route[0])) atHome = true;
                if (atHome) anim?.SetBase("idle");
                return;
            }
            atHome = false;

            if (pause > 0f) { pause -= Time.fixedDeltaTime; anim?.SetBase("idle"); return; }
            if (route.Length == 1) { anim?.SetBase("idle"); return; }
            if (MoveToward(route[wp % route.Length]) || blocked > StuckSeconds)
            {
                wp = (wp + 1) % route.Length;
                pause = UnityEngine.Random.Range(2f, 6f);
                blocked = 0f;
            }
        }

        /// <summary>Returns true when the target is reached.</summary>
        private bool MoveToward(Vector2 target)
        {
            Vector2 pos = rb.position;
            Vector2 delta = target - pos;
            float dist = delta.magnitude;
            if (dist < 0.08f) { anim?.SetBase("idle"); return true; }
            Vector2 dir = delta / dist;
            float step = Mathf.Min(Speed * Time.fixedDeltaTime, dist);
            int n = rb.Cast(dir, filter, hits, step + 0.02f);
            bool block = false;
            for (int i = 0; i < n; i++)
            {
                var c = hits[i].collider;
                if (c != null && !c.isTrigger && c.attachedRigidbody != rb) { block = true; break; }
            }
            if (block)
            {
                blocked += Time.fixedDeltaTime;
                anim?.SetBase("idle");
                return false;
            }
            blocked = 0f;
            facing = CharacterSpriteAnimator.ResolveFacing(dir, facing, 1.3f);
            anim?.SetBase("walk");
            rb.MovePosition(pos + dir * step);
            return false;
        }

        private static CharacterFacing FacingOf(Vector2 d)
        {
            if (Mathf.Abs(d.x) > Mathf.Abs(d.y)) return d.x < 0f ? CharacterFacing.Left : CharacterFacing.Right;
            return d.y < 0f ? CharacterFacing.Down : CharacterFacing.Up;
        }

        private GameObject BuildBubble(string text)
        {
            int order = sr.sortingOrder + 500;
            var root = new GameObject("Bubble");
            root.transform.SetParent(transform, false);
            float top = sr.sprite != null ? sr.sprite.bounds.max.y : 1f;
            root.transform.localPosition = new Vector3(0f, top + 0.25f, 0f);

            string shown = string.IsNullOrEmpty(text) ? "…" : text;
            var tmpGo = new GameObject("Text");
            tmpGo.transform.SetParent(root.transform, false);
            var tmp = tmpGo.AddComponent<TextMeshPro>();
            tmp.text = shown;
            tmp.fontSize = 2.2f;
            tmp.color = new Color(0.24f, 0.15f, 0.08f, 1f);
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.textWrappingMode = TextWrappingModes.Normal;
            tmp.rectTransform.sizeDelta = new Vector2(3.2f, 1f);
            tmp.sortingOrder = order + 1;
            tmp.ForceMeshUpdate();
            var bounds = tmp.textBounds.size;
            float w = Mathf.Max(0.8f, bounds.x + 0.4f), h = Mathf.Max(0.5f, bounds.y + 0.3f);

            var bg = new GameObject("Bg");
            bg.transform.SetParent(root.transform, false);
            var bsr = bg.AddComponent<SpriteRenderer>();
            var frame = UISpriteLoader.GetFrameParchment();
            if (frame != null)
            {
                bsr.sprite = frame;
                bsr.drawMode = SpriteDrawMode.Sliced;
                bsr.size = new Vector2(w, h);
            }
            bsr.color = new Color(1f, 0.93f, 0.78f, 0.97f);
            bsr.sortingOrder = order;
            root.transform.localPosition += new Vector3(0f, h * 0.5f, 0f);
            return root;
        }
    }
}
