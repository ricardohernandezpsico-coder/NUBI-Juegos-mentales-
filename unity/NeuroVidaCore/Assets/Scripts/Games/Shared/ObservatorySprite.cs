using UnityEngine;
using static NeuroVida.Games.Shared.ClayRaster;

namespace NeuroVida.Games.Shared
{
    /// <summary>
    /// Cúpula de un observatorio en arcilla (la de "Lluvia de meteoros", sobre el borde del planeta): cúpula crema con
    /// relieve, franja abierta por donde asoma un telescopio celeste, y zócalo lila. Colores horneados.
    /// </summary>
    public static class ObservatorySprite
    {
        private const int SizePx = 256;
        private const float Zoom = 1.1f;
        private const float Aa = 0.02f;
        private static Sprite _cached;

        /// <summary>Fracción del alto del dibujo (desde abajo) donde termina el zócalo: se alinea con el borde del planeta.</summary>
        public static float BaseFraction => ((-0.9f) / Zoom + 1f) * 0.5f;

        public static Sprite Get() => _cached != null ? _cached : _cached = ToSprite(Render(SizePx), SizePx, SizePx);

        private static float Dome(float x, float y) => Intersect(Circle(x, y, 0f, -0.52f, 0.8f), -(y + 0.52f));
        private static float Plinth(float x, float y) => RoundBox(x, y, 0f, -0.7f, 0.86f, 0.17f, 0.09f);
        private static float Barrel(float x, float y) => Capsule(x, y, 0.1f, 0.05f, 0.66f, 0.62f, 0.1f);
        private static float Slit(float x, float y) => RoundBox(x, y, 0.1f, 0.0f, 0.1f, 0.58f, 0.08f);

        private static float Body(float x, float y) => Union(Union(Dome(x, y), Plinth(x, y)), Barrel(x, y));

        public static Color32[] Render(int size) => RenderClay(size, Zoom, 0.05f, 0.06f, Aa, Body, (ref Px p, float x, float y) =>
        {
            float dome = Dome(x, y);
            p.Over(Grape, Cover(Plinth(x, y), Aa));
            // cúpula con relieve: claro arriba a la izquierda, oscuro abajo a la derecha
            float shade = Mathf.Clamp(1f + (-x * 0.35f + y * 0.45f) * 0.2f, 0.82f, 1.15f);
            p.Over(Shade(Cream, shade), Cover(dome, Aa));
            p.Over(WithAlpha(Shade(Grape, 0.9f), 0.5f), Cover(dome, Aa) * Mathf.Clamp01((x * 0.5f - y * 0.2f) * 0.8f - 0.25f));
            p.Over(new Color(1f, 1f, 1f, 0.55f), Cover(Ellipse(x, y, -0.34f, 0.18f, 0.2f, 0.08f), Aa) * Cover(dome + 0.04f, Aa));
            // costillas de la cúpula
            p.Over(WithAlpha(Ink, 0.35f), Cover(Mathf.Abs(Capsule(x, y, -0.35f, -0.48f, -0.3f, 0.12f, 0.01f)), Aa) * Cover(dome + 0.03f, Aa));
            // franja abierta con el cielo detrás
            float slit = Slit(x, y);
            p.Over(Ink, Cover(slit + 0.03f, Aa) * Cover(dome + 0.03f, Aa));
            p.Over(Hex(0x0A0F33), Cover(slit, Aa) * Cover(dome, Aa));
            p.Over(WithAlpha(Sky, 0.5f), Cover(Capsule(x, y, 0.1f, -0.3f, 0.1f, 0.4f, 0.025f), Aa) * Cover(slit + 0.03f, Aa));
            // telescopio
            float barrel = Barrel(x, y);
            p.Over(Sky, Cover(barrel, Aa));
            p.Over(new Color(1f, 1f, 1f, 0.5f), Cover(Capsule(x, y, 0.14f, 0.14f, 0.5f, 0.52f, 0.025f), Aa) * Cover(barrel + 0.03f, Aa));
            p.Over(Cream, Cover(Circle(x, y, 0.66f, 0.62f, 0.115f), Aa));
            p.Over(Ink, Cover(Mathf.Abs(Circle(x, y, 0.66f, 0.62f, 0.115f)) - 0.02f, Aa));
            // luz de aviso
            p.Over(Sun, Cover(Circle(x, y, -0.5f, -0.72f, 0.05f), Aa));
            p.Over(Sun, Cover(Circle(x, y, 0.5f, -0.72f, 0.05f), Aa));
        });
    }
}
