using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace NeuroVida.Games.Parejas
{
    /// <summary>
    /// Una línea gruesa que se dibuja SOBRE las luces (las de «Constelaciones»: continua o punteada), con los bordes suaves (una franja de transparencia de 1,2 unidades). Los puntos van en unidades del lienzo (locales a su
    /// RectTransform, centrada en su padre). Sin sprites: es una malla de la interfaz.
    /// </summary>
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class LineGraphic : MaskableGraphic
    {
        public List<Vector2> Points = new List<Vector2>();
        public float Width = 4f;
        /// <summary>Largos del trazo y del hueco; 0 = continua.</summary>
        public float DashOn, DashOff;
        public float Feather = 1.2f;

        public void Set(List<Vector2> pts, float width, Color c, float dashOn = 0f, float dashOff = 0f)
        {
            Points = pts;
            Width = width;
            DashOn = dashOn;
            DashOff = dashOff;
            color = c;
            SetVerticesDirty();
        }

        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();
            if (Points == null || Points.Count < 2 || Width <= 0f) return;
            float half = Width * 0.5f;
            float acc = 0f;                  // en qué punto del patrón de trazos va
            bool on = true;
            for (int i = 0; i + 1 < Points.Count; i++)
            {
                Vector2 a = Points[i], b = Points[i + 1];
                float len = Vector2.Distance(a, b);
                if (len < 0.0001f) continue;
                if (DashOn <= 0f) { Segment(vh, a, b, half, i == 0, i == Points.Count - 2); continue; }
                float pos = 0f;
                while (pos < len)
                {
                    float limit = (on ? DashOn : DashOff) - acc;
                    float step = Mathf.Min(limit, len - pos);
                    if (on) Segment(vh, Vector2.Lerp(a, b, pos / len), Vector2.Lerp(a, b, (pos + step) / len), half, false, false);
                    pos += step;
                    acc += step;
                    if (acc >= (on ? DashOn : DashOff) - 1e-4f) { on = !on; acc = 0f; }
                }
            }
        }

        private void Segment(VertexHelper vh, Vector2 a, Vector2 b, float half, bool capA, bool capB)
        {
            Vector2 d = (b - a).normalized, n = new Vector2(-d.y, d.x);
            float cap = half * 0.55f;
            a -= d * cap;                      // las puntas se pasan un poco: terminan redondeadas a la vista
            b += d * cap;
            Color32 c = color, z = new Color(color.r, color.g, color.b, 0f);
            int s = vh.currentVertCount;
            float f = half + Feather;
            vh.AddVert(a + n * f, z, Vector2.zero);
            vh.AddVert(a + n * half, c, Vector2.zero);
            vh.AddVert(a - n * half, c, Vector2.zero);
            vh.AddVert(a - n * f, z, Vector2.zero);
            vh.AddVert(b + n * f, z, Vector2.zero);
            vh.AddVert(b + n * half, c, Vector2.zero);
            vh.AddVert(b - n * half, c, Vector2.zero);
            vh.AddVert(b - n * f, z, Vector2.zero);
            for (int k = 0; k < 3; k++)
            {
                vh.AddTriangle(s + k, s + k + 1, s + k + 5);
                vh.AddTriangle(s + k, s + k + 5, s + k + 4);
            }
        }
    }
}
