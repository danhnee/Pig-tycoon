using UnityEngine;

namespace PigTycoon.Presentation
{
    /// <summary>Short-lived tint-only effects for structures and monsters (no new art: lines and existing sprites).</summary>
    public static class DefenseFx
    {
        private static Material lineMaterial;

        public static void Line(Vector2 from, Vector2 to, Color color, float seconds = 0.12f, float width = 0.08f)
        {
            var go = new GameObject("DefenseFx_Line");
            var lr = go.AddComponent<LineRenderer>();
            if (lineMaterial == null) lineMaterial = new Material(Shader.Find("Sprites/Default"));
            lr.material = lineMaterial;
            lr.positionCount = 2;
            lr.useWorldSpace = true;
            lr.SetPosition(0, from);
            lr.SetPosition(1, to);
            lr.startWidth = width;
            lr.endWidth = width * 0.6f;
            lr.startColor = color;
            lr.endColor = color;
            lr.sortingOrder = 650;
            Object.Destroy(go, seconds);
        }

        public static void Ring(Vector2 center, float radius, Color color, float seconds = 0.4f)
        {
            var go = new GameObject("DefenseFx_Ring");
            var lr = go.AddComponent<LineRenderer>();
            if (lineMaterial == null) lineMaterial = new Material(Shader.Find("Sprites/Default"));
            lr.material = lineMaterial;
            lr.loop = true;
            lr.useWorldSpace = true;
            const int n = 32;
            lr.positionCount = n;
            for (int i = 0; i < n; i++)
            {
                float a = i / (float)n * Mathf.PI * 2f;
                lr.SetPosition(i, center + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * radius);
            }
            lr.startWidth = lr.endWidth = 0.07f;
            lr.startColor = lr.endColor = color;
            lr.sortingOrder = 640;
            Object.Destroy(go, seconds);
        }

        public static void Feedback(string message)
        {
            PlayerInteractionController.Instance?.ShowFeedback(message);
        }
    }
}
