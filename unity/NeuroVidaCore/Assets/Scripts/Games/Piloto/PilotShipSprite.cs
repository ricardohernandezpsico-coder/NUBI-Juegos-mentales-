using UnityEngine;
using static NeuroVida.Games.Shared.ClayRaster;

namespace NeuroVida.Games.Piloto
{
    /// <summary>
    /// La nave de arcilla de Piloto Estelar (y de Correo Estelar): cuerpo crema, punta y aletas coral, ventanilla celeste y llama sol, mirando hacia arriba. Era
    /// <c>ChipShipSprite</c> de Cambio de Chip (retirado el 3-oct-2026), que la dibujaba en las 4 direcciones; se mudó acá y quedó solo la de arriba.
    /// Se hornea una sola vez (la sombra dura siempre abajo). <c>Image.color</c> en blanco.
    /// </summary>
    public static class PilotShipSprite
    {
        private const int SizePx = 192;
        private const float Zoom = 1.1f;
        private const float Aa = 0.025f;
        private const float Line = 0.075f;
        private const float Thin = 0.05f;
        private const float Drop = 0.08f;
        private static Sprite _cache;

        private static readonly float[] Fin = { 0.20f, -0.10f, 0.52f, -0.46f, 0.52f, -0.68f, 0.20f, -0.52f };

        public static Sprite Get() => _cache != null ? _cache : (_cache = ToSprite(Render(SizePx), SizePx, SizePx));

        private static float Hull(float u, float v) =>
            Mathf.Max(Mathf.Max(Circle(u, v, -0.62f, -0.06f, 0.92f), Circle(u, v, 0.62f, -0.06f, 0.92f)), -0.58f - v);

        private static float Flame(float u, float v) => UnevenCapsule(u, -(v + 0.56f), 0.17f, 0.04f, 0.34f);

        private static float Body(float x, float y) =>
            Mathf.Min(Mathf.Min(Hull(x, y), Polygon(Mathf.Abs(x), y, Fin)), Flame(x, y));

        private static void Part(ref Px p, float sdf, Color fill, float line = Line)
        {
            p.Over(Ink, Cover(sdf - line, Aa));
            p.Over(fill, Cover(sdf, Aa));
        }

        /// <summary>Píxeles de la nave (fila 0 = abajo). Separado de <see cref="Get"/> para previsualizar fuera de Unity.</summary>
        public static Color32[] Render(int size) =>
            RenderClay(size, Zoom, Line, Drop, Aa, Body, (ref Px p, float u, float v) =>
            {
                p.Over(Sun, Cover(Flame(u, v), Aa));
                p.Over(Coral, Cover(UnevenCapsule(u, -(v + 0.56f), 0.08f, 0.02f, 0.2f), Aa));
                Part(ref p, Polygon(Mathf.Abs(u), v, Fin), Coral);
                float hull = Hull(u, v);
                Part(ref p, hull, Cream);
                Part(ref p, Mathf.Max(hull, 0.26f - v), Coral);                    // punta
                p.Over(new Color(1f, 1f, 1f, 0.55f), Cover(Ellipse(u, v, -0.14f, -0.04f, 0.05f, 0.24f), Aa) * Cover(hull + 0.03f, Aa));
                float window = Circle(u, v, 0f, 0.02f, 0.15f);
                Part(ref p, window, Sky, Thin);
                p.Over(new Color(1f, 1f, 1f, 0.75f), Cover(Circle(u, v, -0.05f, 0.07f, 0.045f), Aa));
            });
    }
}
