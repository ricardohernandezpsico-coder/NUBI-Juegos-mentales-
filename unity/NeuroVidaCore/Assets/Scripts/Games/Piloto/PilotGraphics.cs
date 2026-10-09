using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace NeuroVida.Games.Piloto
{
    /// <summary>
    /// La ruta de balizas como una malla de la interfaz (sin sprites: el grosor de los bordes no se deforma): un canal tenue entre las dos filas de balizas y, en cada borde, una línea ancha y suave y otra fina y clara. Los puntos van de la nave hacia adelante (de abajo hacia arriba),
    /// en unidades del lienzo y relativos al centro del rectángulo; el canal se desvanece hacia lejos.
    /// </summary>
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class RouteGraphic : MaskableGraphic
    {
        private readonly List<Vector2> _left = new List<Vector2>(), _right = new List<Vector2>();
        private float _wide, _thin, _channelAlpha = 0.07f, _wideAlpha = 0.16f, _thinAlpha = 0.28f;
        private Color _tint = new Color(127f / 255f, 216f / 255f, 1f);

        public void Clear()
        {
            _left.Clear();
            _right.Clear();
            SetVerticesDirty();
        }

        public void Add(Vector2 left, Vector2 right)
        {
            _left.Add(left);
            _right.Add(right);
        }

        public void Apply(float wideUnits, float thinUnits, Color tint, float channelAlpha, float wideAlpha, float thinAlpha)
        {
            _wide = wideUnits; _thin = thinUnits; _tint = tint;
            _channelAlpha = channelAlpha; _wideAlpha = wideAlpha; _thinAlpha = thinAlpha;
            SetVerticesDirty();
        }

        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();
            int n = _left.Count;
            if (n < 2) return;
            int v = 0;
            for (int i = 0; i < n - 1; i++)
            {
                float fade0 = 1f - 0.7f * i / (n - 1), fade1 = 1f - 0.7f * (i + 1) / (n - 1);
                Quad(vh, ref v, _left[i], _right[i], _right[i + 1], _left[i + 1], Col(_channelAlpha * fade0), Col(_channelAlpha * fade0), Col(_channelAlpha * fade1), Col(_channelAlpha * fade1));
                Strip(vh, ref v, _left[i], _left[i + 1], _wide, Col(_wideAlpha * fade0), Col(_wideAlpha * fade1));
                Strip(vh, ref v, _right[i], _right[i + 1], _wide, Col(_wideAlpha * fade0), Col(_wideAlpha * fade1));
                Strip(vh, ref v, _left[i], _left[i + 1], _thin, Col(_thinAlpha * fade0), Col(_thinAlpha * fade1));
                Strip(vh, ref v, _right[i], _right[i + 1], _thin, Col(_thinAlpha * fade0), Col(_thinAlpha * fade1));
            }
        }

        private Color32 Col(float a) => new Color(_tint.r, _tint.g, _tint.b, Mathf.Clamp01(a) * color.a);

        private static void Strip(VertexHelper vh, ref int v, Vector2 a, Vector2 b, float width, Color32 ca, Color32 cb)
        {
            Vector2 d = b - a;
            float len = d.magnitude;
            if (len < 1e-4f) return;
            Vector2 nrm = new Vector2(-d.y, d.x) / len * (width * 0.5f);
            Quad(vh, ref v, a - nrm, a + nrm, b + nrm, b - nrm, ca, ca, cb, cb);
        }

        private static void Quad(VertexHelper vh, ref int v, Vector2 p0, Vector2 p1, Vector2 p2, Vector2 p3, Color32 c0, Color32 c1, Color32 c2, Color32 c3)
        {
            vh.AddVert(p0, c0, Vector2.zero);
            vh.AddVert(p1, c1, Vector2.zero);
            vh.AddVert(p2, c2, Vector2.zero);
            vh.AddVert(p3, c3, Vector2.zero);
            vh.AddTriangle(v, v + 1, v + 2);
            vh.AddTriangle(v, v + 2, v + 3);
            v += 4;
        }
    }

    /// <summary>
    /// El arco de un sector: un portal dorado (media elipse sobre la ruta) con un brillo ancho debajo, que la nave cruza al pasar de un sector al siguiente. Medidas en unidades del lienzo; el centro es el del rectángulo (que el juego mueve).
    /// </summary>
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class GateGraphic : MaskableGraphic
    {
        public float Rx = 200f, Ry = 66f, Core = 15f, Glow = 36f;
        private Color _tint = new Color(1f, 201f / 255f, 74f / 255f, 0.9f);

        public void Set(float rx, float ry, float core, float glow, Color c)
        {
            Rx = rx; Ry = ry; Core = core; Glow = glow;
            _tint = c;                                   // el color va en los vértices (el del gráfico queda blanco: así no se multiplica dos veces)
            SetVerticesDirty();
        }

        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();
            const int steps = 40;
            // dos cintas continuas (sin costuras entre tramos): el brillo ancho, que se desvanece hacia los bordes, y el filo dorado, parejo
            var pts = new Vector2[steps + 1];
            var nrm = new Vector2[steps + 1];
            for (int i = 0; i <= steps; i++)
            {
                float t = i / (float)steps * Mathf.PI;
                pts[i] = new Vector2(Rx * Mathf.Cos(t), Ry * Mathf.Sin(t));
                // la normal de la elipse en ese punto (apunta hacia afuera)
                var n = new Vector2(Mathf.Cos(t) / Rx, Mathf.Sin(t) / Ry);
                nrm[i] = n.sqrMagnitude > 1e-9f ? n.normalized : Vector2.up;
            }
            Ribbon(vh, pts, nrm, Glow, new Color(_tint.r, _tint.g, _tint.b, 0f), new Color(_tint.r, _tint.g, _tint.b, _tint.a * 0.4f));
            Ribbon(vh, pts, nrm, Core, new Color(_tint.r, _tint.g, _tint.b, _tint.a), new Color(_tint.r, _tint.g, _tint.b, _tint.a));
        }

        private static void Ribbon(VertexHelper vh, Vector2[] pts, Vector2[] nrm, float width, Color32 edge, Color32 mid)
        {
            int start = vh.currentVertCount;
            for (int i = 0; i < pts.Length; i++)
            {
                vh.AddVert(pts[i] - nrm[i] * (width * 0.5f), edge, Vector2.zero);
                vh.AddVert(pts[i], mid, Vector2.zero);
                vh.AddVert(pts[i] + nrm[i] * (width * 0.5f), edge, Vector2.zero);
            }
            for (int i = 0; i < pts.Length - 1; i++)
            {
                int a = start + i * 3, b = start + (i + 1) * 3;
                vh.AddTriangle(a, a + 1, b + 1);
                vh.AddTriangle(a, b + 1, b);
                vh.AddTriangle(a + 1, a + 2, b + 2);
                vh.AddTriangle(a + 1, b + 2, b + 1);
            }
        }
    }
}
