using UnityEngine;
using static NeuroVida.Games.Shared.ClayRaster;

namespace NeuroVida.Games.Calculo
{
    /// <summary>
    /// Arte propio de Cálculo Sereno: nenúfares de arcilla que flotan en el estanque (uno con flor de loto),
    /// para que el estanque sea un lugar tranquilo y no un rectángulo de agua. Vistos en perspectiva (achatados).
    /// Colores horneados: <c>Image.color</c> en blanco.
    /// </summary>
    public static class PondSprites
    {
        private const int SizePx = 192;
        private const float Zoom = 1.08f;
        private const float Aa = 0.025f;
        private const float Line = 0.07f;
        private const float Thin = 0.05f;
        private const float Drop = 0.07f;
        private static readonly Color PadColor = Hex(0x5FD68A);
        private static readonly Sprite[] Cache = new Sprite[2];

        /// <summary>0 = nenúfar solo, 1 = nenúfar con flor de loto.</summary>
        public static Sprite LilyPad(int variant)
        {
            variant = Mathf.Clamp(variant, 0, 1);
            if (Cache[variant] != null) return Cache[variant];
            return Cache[variant] = ToSprite(Render(variant, SizePx), SizePx, SizePx);
        }

        private static float Pad(float x, float y)
        {
            // Elipse achatada con la muesca en V del nenúfar.
            float pad = Ellipse(x, y, 0f, -0.42f, 0.9f, 0.4f);
            float notch = Polygon(x, y, Notch);
            return Mathf.Max(pad, -notch);
        }

        private static readonly float[] Notch = { 0f, -0.44f, 0.30f, 0.05f, 0.52f, -0.02f };

        private static float Petal(float x, float y, float cx, float cy, float angle, float len)
        {
            Rotate(x, y, cx, cy, -angle, out float u, out float v);
            return Intersect(Circle(u, v, -0.13f, len * 0.5f, 0.2f), Circle(u, v, 0.13f, len * 0.5f, 0.2f));
        }

        private static float Flower(float x, float y)
        {
            float f = 1e6f;
            for (int i = -2; i <= 2; i++) f = Mathf.Min(f, Petal(x, y, -0.18f, -0.34f, i * 0.42f, 0.62f));
            return f;
        }

        private static float Body(int variant, float x, float y) => variant == 1 ? Mathf.Min(Pad(x, y), Flower(x, y)) : Pad(x, y);

        /// <summary>Píxeles del nenúfar (fila 0 = abajo). Separado para previsualizar fuera de Unity.</summary>
        public static Color32[] Render(int variant, int size) =>
            RenderClay(size, Zoom, Line, Drop, Aa, (x, y) => Body(variant, x, y), (ref Px p, float x, float y) =>
            {
                float pad = Pad(x, y);
                p.Over(PadColor, Cover(pad, Aa));
                // Nervaduras del nenúfar.
                float veins = Mathf.Min(Mathf.Min(Capsule(x, y, 0f, -0.44f, -0.62f, -0.38f, 0.02f), Capsule(x, y, 0f, -0.44f, -0.40f, -0.70f, 0.02f)),
                    Capsule(x, y, 0f, -0.44f, 0.44f, -0.72f, 0.02f));
                p.Over(Shade(PadColor, 0.72f), Cover(veins, Aa) * Cover(pad + 0.05f, Aa));
                p.Over(new Color(1f, 1f, 1f, 0.45f), Cover(Ellipse(x, y, -0.44f, -0.24f, 0.2f, 0.05f), Aa) * Cover(pad + 0.03f, Aa));
                if (variant == 1)
                {
                    for (int i = -2; i <= 2; i++)
                    {
                        if (i == 0) continue;
                        float pe = Petal(x, y, -0.18f, -0.34f, i * 0.42f, 0.62f);
                        p.Over(Ink, Cover(pe - Thin, Aa));
                        p.Over(Hex(0xFFB3D6), Cover(pe, Aa));
                    }
                    float mid = Petal(x, y, -0.18f, -0.34f, 0f, 0.62f);
                    p.Over(Ink, Cover(mid - Thin, Aa));
                    p.Over(Pink, Cover(mid, Aa));
                    p.Over(Ink, Cover(Circle(x, y, -0.18f, -0.30f, 0.07f) - Thin, Aa));
                    p.Over(Sun, Cover(Circle(x, y, -0.18f, -0.30f, 0.07f), Aa));
                }
            });
    }
}
