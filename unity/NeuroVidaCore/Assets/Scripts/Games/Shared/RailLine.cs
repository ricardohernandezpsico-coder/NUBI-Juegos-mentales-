using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace NeuroVida.Games.Shared
{
    /// <summary>
    /// Una ruta curva dibujada como una cinta (malla propia de la UI) que sigue una lista de puntos. La sección de la cinta
    /// sale de una textura chica con los bordes suavizados, así las curvas no se ven dentadas:
    /// <list type="bullet">
    /// <item><see cref="Style.Clay"/>: riel de arcilla (borde tinta a los dos lados y cuerpo del color de la cinta).</item>
    /// <item><see cref="Style.Solid"/>: cinta pareja (sombra dura, línea de luz).</item>
    /// <item><see cref="Style.Soft"/>: resplandor que se desvanece hacia los bordes.</item>
    /// </list>
    /// El color se da con <c>Graphic.color</c>.
    /// </summary>
    public class RailLine : MaskableGraphic
    {
        public enum Style { Clay, Solid, Soft }

        private const int TexH = 64;
        private static Texture2D _clay, _solid, _soft;

        private readonly List<Vector2> _points = new List<Vector2>();
        private Style _style = Style.Solid;
        private float _thickness = 16f;

        public override Texture mainTexture
        {
            get
            {
                switch (_style)
                {
                    case Style.Clay: return _clay != null ? _clay : _clay = Profile(Style.Clay);
                    case Style.Soft: return _soft != null ? _soft : _soft = Profile(Style.Soft);
                    default: return _solid != null ? _solid : _solid = Profile(Style.Solid);
                }
            }
        }

        public void Setup(Style style, float thickness)
        {
            _style = style;
            _thickness = thickness;
            raycastTarget = false;
            SetMaterialDirty();
            SetVerticesDirty();
        }

        public void SetPoints(IList<Vector2> points, Vector2 offset)
        {
            _points.Clear();
            foreach (var p in points) _points.Add(p + offset);
            SetVerticesDirty();
        }

        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();
            int n = _points.Count;
            if (n < 2) return;
            float half = _thickness * 0.5f;
            Color32 c = color;
            // Los extremos se alargan medio grosor: las uniones quedan tapadas bajo desvíos y planetas, sin huecos.
            for (int i = -1; i <= n; i++)
            {
                Vector2 p, t;
                if (i < 0)
                {
                    t = (_points[1] - _points[0]).normalized;
                    p = _points[0] - t * half;
                }
                else if (i >= n)
                {
                    t = (_points[n - 1] - _points[n - 2]).normalized;
                    p = _points[n - 1] + t * half;
                }
                else
                {
                    p = _points[i];
                    t = (_points[Mathf.Min(n - 1, i + 1)] - _points[Mathf.Max(0, i - 1)]).normalized;
                }
                var normal = new Vector2(-t.y, t.x) * half;
                vh.AddVert(p + normal, c, new Vector2(0.5f, 1f));
                vh.AddVert(p - normal, c, new Vector2(0.5f, 0f));
            }
            int verts = (n + 2) * 2;
            for (int i = 0; i + 3 < verts; i += 2)
            {
                vh.AddTriangle(i, i + 1, i + 3);
                vh.AddTriangle(i, i + 3, i + 2);
            }
        }

        /// <summary>Sección transversal de la cinta (v = 0 a 1 de un borde al otro).</summary>
        private static Texture2D Profile(Style style)
        {
            var tex = new Texture2D(2, TexH, TextureFormat.RGBA32, false)
            {
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Bilinear
            };
            var px = new Color32[2 * TexH];
            var ink = new Color(0.102f, 0.071f, 0.251f); // Ink 0x1A1240
            for (int y = 0; y < TexH; y++)
            {
                float v = (y + 0.5f) / TexH;
                float edge = Mathf.Min(v, 1f - v) * TexH;   // texeles hasta el borde más cercano
                float aa = Mathf.Clamp01((edge - 0.5f) / 2.5f);
                Color c;
                switch (style)
                {
                    case Style.Clay:
                        // Borde tinta (22% a cada lado) y cuerpo blanco (se tiñe con el color de la cinta).
                        float body = Mathf.Clamp01((Mathf.Min(v, 1f - v) - 0.22f) * TexH / 1.5f + 0.5f);
                        c = Color.Lerp(ink, Color.white, body);
                        c.a = aa;
                        break;
                    case Style.Soft:
                        float k = 1f - Mathf.Abs(2f * v - 1f);
                        c = new Color(1f, 1f, 1f, k * k);
                        break;
                    default:
                        c = new Color(1f, 1f, 1f, aa);
                        break;
                }
                px[y * 2] = px[y * 2 + 1] = c;
            }
            tex.SetPixels32(px);
            tex.Apply();
            return tex;
        }
    }
}
