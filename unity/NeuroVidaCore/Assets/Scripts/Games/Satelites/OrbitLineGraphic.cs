using UnityEngine;
using UnityEngine.UI;

namespace NeuroVida.Games.Satelites
{
    /// <summary>
    /// Una órbita: una elipse punteada y fina, centrada en el centro de su RectTransform (malla de la interfaz, sin sprites: el grosor del trazo no se deforma al estirar la elipse). Medidas en unidades del lienzo.
    /// </summary>
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class OrbitLineGraphic : MaskableGraphic
    {
        public float Rx = 100f, Ry = 140f, Thickness = 3.6f, Dash = 9f, Gap = 21f;

        public void Set(float rx, float ry, float thickness, float dash, float gap, Color c)
        {
            Rx = rx; Ry = ry; Thickness = thickness; Dash = dash; Gap = gap;
            color = c;
            SetVerticesDirty();
        }

        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();
            if (Rx <= 1f || Ry <= 1f) return;
            const int steps = 360;
            float period = Dash + Gap, acc = 0f, half = Thickness * 0.5f;
            Vector2 prev = new Vector2(Rx, 0f);
            int v = 0;
            for (int i = 1; i <= steps; i++)
            {
                float t = i / (float)steps * Mathf.PI * 2f;
                Vector2 cur = new Vector2(Rx * Mathf.Cos(t), Ry * Mathf.Sin(t));
                Vector2 d = cur - prev;
                float len = d.magnitude;
                if (len > 1e-4f && Mathf.Repeat(acc, period) < Dash)
                {
                    Vector2 n = new Vector2(-d.y, d.x) / len * half;
                    vh.AddVert(prev - n, color, Vector2.zero);
                    vh.AddVert(prev + n, color, Vector2.zero);
                    vh.AddVert(cur + n, color, Vector2.zero);
                    vh.AddVert(cur - n, color, Vector2.zero);
                    vh.AddTriangle(v, v + 1, v + 2);
                    vh.AddTriangle(v, v + 2, v + 3);
                    v += 4;
                }
                acc += len;
                prev = cur;
            }
        }
    }
}
