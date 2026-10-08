using UnityEngine;
using UnityEngine.UI;

namespace NeuroVida.Games.Correo
{
    /// <summary>Un haz de luz: un triángulo con la punta en el centro de su RectTransform, que se abre hacia <see cref="Angle"/> (en radianes, y hacia ARRIBA como en UGUI) hasta <see cref="Length"/> con medio ancho
    /// <see cref="Half"/> (en radianes) y se desvanece hacia el final (la punta con <see cref="NearAlpha"/>, a 0,6 del largo con <see cref="MidAlpha"/>, el final transparente). Es una malla de la interfaz, sin sprites;
    /// sirve para el haz del faro y su halo.</summary>
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class WedgeGraphic : MaskableGraphic
    {
        public float Angle = Mathf.PI / 2f, Half = 0.15f, Length = 780f, NearAlpha = 0.85f, MidAlpha = 0.25f;

        public void Set(float angle, float half, float length, Color c, float nearAlpha, float midAlpha)
        {
            Angle = angle;
            Half = half;
            Length = length;
            color = c;
            NearAlpha = nearAlpha;
            MidAlpha = midAlpha;
            SetVerticesDirty();
        }

        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();
            if (Length <= 0f) return;
            Vector2 l = new Vector2(Mathf.Cos(Angle + Half), Mathf.Sin(Angle + Half));
            Vector2 r = new Vector2(Mathf.Cos(Angle - Half), Mathf.Sin(Angle - Half));
            Color a0 = new Color(color.r, color.g, color.b, color.a * NearAlpha);
            Color a1 = new Color(color.r, color.g, color.b, color.a * MidAlpha);
            Color a2 = new Color(color.r, color.g, color.b, 0f);
            const float mid = 0.6f;
            vh.AddVert(Vector2.zero, a0, Vector2.zero);
            vh.AddVert(l * (Length * mid), a1, Vector2.zero);
            vh.AddVert(r * (Length * mid), a1, Vector2.zero);
            vh.AddVert(l * Length, a2, Vector2.zero);
            vh.AddVert(r * Length, a2, Vector2.zero);
            vh.AddTriangle(0, 1, 2);
            vh.AddTriangle(1, 3, 4);
            vh.AddTriangle(1, 4, 2);
        }
    }
}
