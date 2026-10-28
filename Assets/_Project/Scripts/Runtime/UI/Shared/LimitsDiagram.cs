using UnityEngine;
using UnityEngine.UI;

namespace CalculusLabEscape
{
    // Canvas-native geometry; coordinates stay local at every CanvasScaler resolution.
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class LimitsDiagram : MaskableGraphic
    {
        public bool portal;
        [Range(0, 5)] public int charge;
        public bool filledPoint;
        [Range(0, 4)] public float probe = 1;
        [Range(0, 8)] public int marker = 1;
        public bool showProbe = true;
        static readonly Color Accent = new Color(.2f, .82f, .88f);
        static readonly Color Ink = new Color(.73f, .88f, .92f);
        static readonly Color Grid = new Color(.15f, .31f, .37f);
        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear(); Rect r = rectTransform.rect;
            if (portal)
            {
                Vector2 center = r.center; float radius = Mathf.Min(r.width, r.height) * .35f;
                Circle(vh, center, radius, Ink, 3); Circle(vh, center, radius - 12, Grid, 2);
                for (int i = 0; i < 5; i++)
                {
                    float angle = (90 - i * 72) * Mathf.Deg2Rad;
                    Disc(vh, center + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * (radius + 20), 7, i < charge ? Accent : Grid);
                    if (i < charge) for (int j = 0; j < 12; j++)
                    {
                        float a = (90 - i * 72 - j * 6) * Mathf.Deg2Rad, b = a - 6 * Mathf.Deg2Rad;
                        Segment(vh, center + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * (radius - 24), center + new Vector2(Mathf.Cos(b), Mathf.Sin(b)) * (radius - 24), Accent, 7);
                    }
                }
                return;
            }
            float x0 = r.xMin + 30, y0 = r.yMin + 28, w = r.width - 48, h = r.height - 42;
            for (int y = 0; y <= 8; y++) Segment(vh, new Vector2(x0, y0 + y * h / 8), new Vector2(x0 + w, y0 + y * h / 8), Grid, 1);
            for (int x = 0; x <= 4; x++) Segment(vh, new Vector2(x0 + x * w / 4, y0), new Vector2(x0 + x * w / 4, y0 + h), Grid, 1);
            Segment(vh, new Vector2(x0, y0), new Vector2(x0 + w, y0), Ink, 2);
            Segment(vh, new Vector2(x0, y0), new Vector2(x0, y0 + h), Ink, 2);
            Vector2 previous = default;
            for (int i = 0; i <= 80; i++)
            {
                float x = i / 20f, y = 4 + .8f * (x - 2) * (x - 2);
                var point = new Vector2(x0 + x * w / 4, y0 + y * h / 8);
                if (i > 0) Segment(vh, previous, point, Ink, 3); previous = point;
            }
            var open = new Vector2(x0 + w / 2, y0 + h / 2);
            Disc(vh, open, 7, new Color(.025f,.045f,.065f)); Circle(vh, open, 7, Ink, 2);
            if (filledPoint) Disc(vh, new Vector2(x0 + w / 2, y0 + 7 * h / 8), 6, Accent);
            if (showProbe)
            {
                Segment(vh, new Vector2(x0, y0 + marker * h / 8), new Vector2(x0 + w, y0 + marker * h / 8), Accent, 2);
                // f(2) is undefined here; the probe must not visually fill the open point.
                if (Mathf.Abs(probe - 2) > .015f) Disc(vh, new Vector2(x0 + probe * w / 4, y0 + (4 + .8f * (probe - 2) * (probe - 2)) * h / 8), 5, Accent);
            }
        }
        static void Segment(VertexHelper vh, Vector2 a, Vector2 b, Color c, float width)
        {
            Vector2 d = (b - a).normalized, n = new Vector2(-d.y, d.x) * width * .5f;
            int i = vh.currentVertCount;
            vh.AddVert(a - n, c, Vector2.zero); vh.AddVert(a + n, c, Vector2.zero);
            vh.AddVert(b + n, c, Vector2.zero); vh.AddVert(b - n, c, Vector2.zero);
            vh.AddTriangle(i, i + 1, i + 2); vh.AddTriangle(i, i + 2, i + 3);
        }
        static void Circle(VertexHelper vh, Vector2 center, float radius, Color c, float width)
        {
            for (int i = 0; i < 80; i++) { float a = i * Mathf.PI / 40, b = (i + 1) * Mathf.PI / 40; Segment(vh, center + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * radius, center + new Vector2(Mathf.Cos(b), Mathf.Sin(b)) * radius, c, width); }
        }
        static void Disc(VertexHelper vh, Vector2 center, float radius, Color c)
        {
            int start = vh.currentVertCount; vh.AddVert(center, c, Vector2.zero);
            for (int i = 0; i <= 32; i++) { float a = i * Mathf.PI / 16; vh.AddVert(center + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * radius, c, Vector2.zero); if (i > 0) vh.AddTriangle(start, start + i, start + i + 1); }
        }
    }
}
