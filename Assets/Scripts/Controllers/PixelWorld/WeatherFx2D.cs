using System.Collections.Generic;
using PigTycoon.Core;
using UnityEngine;

namespace PigTycoon.Presentation
{
    /// <summary>
    /// GDD 7.2 weather on screen, following the camera: rain streaks (Mưa, Giông Bão, Mưa Bức Xạ tinted green),
    /// drifting fog banks (Sương Mù, lighter for Nồm Ẩm), lightning flashes (Giông Bão), dust (Gió Khô). The light
    /// tint of each weather lives in DayNight2DController; this draws only the moving parts. Pixel sprites are made at
    /// runtime (no art asset), drawn above every actor.
    /// </summary>
    public sealed class WeatherFx2D : MonoBehaviour
    {
        private const int OverlaySort = 30000;
        private const int MaxStreaks = 160;
        private readonly List<SpriteRenderer> streaks = new List<SpriteRenderer>();
        private readonly List<SpriteRenderer> fogBanks = new List<SpriteRenderer>();
        private SpriteRenderer flash;
        private Camera cam;
        private WeatherType shown = (WeatherType)(-1);
        private float flashTimer, nextFlash = 6f;
        private Vector2 wind = new Vector2(-0.18f, -1f);

        private static Sprite streakSprite, fogSprite, flashSprite;

        private void Start()
        {
            cam = GetComponent<Camera>() ?? Camera.main;
            streakSprite ??= MakeSprite(1, 6, (x, y) => new Color(1, 1, 1, 0.35f + 0.1f * y));
            fogSprite ??= MakeSprite(64, 64, (x, y) =>
            {
                float dx = (x - 31.5f) / 32f, dy = (y - 31.5f) / 32f;
                float a = Mathf.Clamp01(1f - Mathf.Sqrt(dx * dx + dy * dy));
                return new Color(1, 1, 1, a * a);
            });
            flashSprite ??= MakeSprite(4, 4, (x, y) => Color.white);
            for (int i = 0; i < MaxStreaks; i++)
            {
                var sr = NewRenderer("Rain", streakSprite, OverlaySort);
                sr.enabled = false;
                streaks.Add(sr);
            }
            for (int i = 0; i < 5; i++)
            {
                var sr = NewRenderer("Fog", fogSprite, OverlaySort - 1);
                sr.enabled = false;
                fogBanks.Add(sr);
            }
            flash = NewRenderer("Lightning", flashSprite, OverlaySort + 1);
            flash.enabled = false;
        }

        private SpriteRenderer NewRenderer(string name, Sprite sprite, int order)
        {
            var go = new GameObject(name);
            go.transform.SetParent(transform, false);
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            sr.sortingOrder = order;
            return sr;
        }

        private static Sprite MakeSprite(int w, int h, System.Func<int, int, Color> px)
        {
            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point, wrapMode = TextureWrapMode.Clamp };
            for (int y = 0; y < h; y++) for (int x = 0; x < w; x++) tex.SetPixel(x, y, px(x, y));
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f), 32f);
        }

        private void Update()
        {
            var engine = MobileGameController.Instance != null ? MobileGameController.Instance.Engine : null;
            if (engine == null || cam == null) return;
            var w = engine.CurrentWeather;
            if (w != shown) { shown = w; Apply(w); }

            float halfH = cam.orthographicSize, halfW = halfH * cam.aspect;
            Vector3 c = cam.transform.position;
            bool rain = w == WeatherType.Mua || w == WeatherType.GiongBao || w == WeatherType.MuaBucXa;
            if (rain)
            {
                float speed = w == WeatherType.GiongBao ? 22f : 15f;
                foreach (var s in streaks)
                {
                    if (!s.enabled) continue;
                    var p = s.transform.position + (Vector3)(wind * speed * Time.deltaTime);
                    if (p.y < c.y - halfH - 1f || p.x < c.x - halfW - 2f)
                        p = new Vector3(c.x + Random.Range(-halfW, halfW + 3f), c.y + halfH + Random.Range(0f, 2f), 0f);
                    s.transform.position = new Vector3(p.x, p.y, 0f);
                }
            }
            foreach (var f in fogBanks)
            {
                if (!f.enabled) continue;
                var p = f.transform.position + new Vector3(0.6f * Time.deltaTime, 0.08f * Mathf.Sin(Time.time * 0.3f) * Time.deltaTime, 0f);
                if (p.x > c.x + halfW + 8f) p = new Vector3(c.x - halfW - 8f, c.y + Random.Range(-halfH, halfH), 0f);
                if (Mathf.Abs(p.y - c.y) > halfH + 6f) p.y = c.y + Random.Range(-halfH, halfH);
                f.transform.position = new Vector3(p.x, p.y, 0f);
            }
            if (w == WeatherType.GiongBao)
            {
                nextFlash -= Time.deltaTime;
                if (nextFlash <= 0f) { flashTimer = 0.35f; nextFlash = Random.Range(6f, 14f); }
            }
            if (flashTimer > 0f)
            {
                flashTimer -= Time.deltaTime;
                flash.enabled = true;
                flash.transform.position = new Vector3(c.x, c.y, 0f);
                flash.transform.localScale = new Vector3(halfW * 2f * 8f + 4f, halfH * 2f * 8f + 4f, 1f);
                float a = flashTimer > 0.25f ? 0.55f : flashTimer / 0.25f * 0.35f;
                flash.color = new Color(0.92f, 0.95f, 1f, a);
            }
            else if (flash.enabled) flash.enabled = false;
        }

        private void Apply(WeatherType w)
        {
            bool rain = w == WeatherType.Mua || w == WeatherType.GiongBao || w == WeatherType.MuaBucXa;
            int count = w == WeatherType.GiongBao ? MaxStreaks : w == WeatherType.Mua ? 110 : w == WeatherType.MuaBucXa ? 90 : 0;
            Color rainTint = w == WeatherType.MuaBucXa ? new Color(0.6f, 1f, 0.55f, 0.9f) : new Color(0.8f, 0.88f, 1f, 0.75f);
            float halfH = cam.orthographicSize, halfW = halfH * cam.aspect;
            Vector3 c = cam.transform.position;
            for (int i = 0; i < streaks.Count; i++)
            {
                var s = streaks[i];
                s.enabled = rain && i < count;
                s.color = rainTint;
                s.transform.position = new Vector3(c.x + Random.Range(-halfW, halfW + 3f), c.y + Random.Range(-halfH, halfH), 0f);
                s.transform.rotation = Quaternion.Euler(0, 0, -10f);
            }
            int fogCount = w == WeatherType.SuongMu ? 5 : w == WeatherType.NomAm ? 3 : w == WeatherType.GioKho ? 2 : 0;
            Color fogTint = w == WeatherType.SuongMu ? new Color(0.86f, 0.88f, 0.92f, 0.62f)
                : w == WeatherType.NomAm ? new Color(0.85f, 0.9f, 0.86f, 0.38f) : new Color(0.92f, 0.82f, 0.6f, 0.28f);
            for (int i = 0; i < fogBanks.Count; i++)
            {
                var f = fogBanks[i];
                f.enabled = i < fogCount;
                f.color = fogTint;
                f.transform.localScale = new Vector3(Random.Range(9f, 14f), Random.Range(5f, 8f), 1f);   // 64 px sprite = 2 m
                f.transform.position = new Vector3(c.x + Random.Range(-halfW - 4f, halfW + 4f), c.y + Random.Range(-halfH, halfH), 0f);
            }
            if (w != WeatherType.GiongBao) { flashTimer = 0f; flash.enabled = false; }
            Debug.Log($"[Weather] {w}");
        }
    }
}
