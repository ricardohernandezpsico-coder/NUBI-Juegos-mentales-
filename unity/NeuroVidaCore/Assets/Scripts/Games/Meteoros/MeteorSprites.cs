using System.Collections.Generic;
using UnityEngine;
using NeuroVida.Games.Shared;
using static NeuroVida.Games.Shared.ClayRaster;

namespace NeuroVida.Games.Meteoros
{
    /// <summary>Color de la roca: lila, celeste y coral (los meteoros normales), sol (el dorado) y gris polvo (la roca
    /// agrietada de una inventada tocada).</summary>
    public enum RockTint { Grape = 0, Sky = 1, Coral = 2, Gold = 3, Dust = 4 }

    /// <summary>
    /// Arte procedural de Lluvia de meteoros, con el pincel de arcilla común (<see cref="ClayRaster"/>):
    /// <list type="bullet">
    /// <item><see cref="Rock"/>: roca de arcilla CON RELIEVE (luz arriba a la izquierda: canto claro arriba, canto oscuro
    /// abajo, brillo, 3-4 cráteres con su sombra), borde tinta y sombra dura hacia abajo. 4 formas y 5 colores; la de
    /// polvo lleva grietas. Se estira a lo ancho según la palabra.</item>
    /// <item><see cref="StarLit"/> / <see cref="StarDim"/>: estrellas de la constelación (encendida y apagada).</item>
    /// </list>
    /// Colores horneados: <c>Image.color</c> en blanco. Las formas se hornean al primer uso (no todas juntas).
    /// </summary>
    public static class MeteorSprites
    {
        public const int ShapeCount = 4;
        private const int SizePx = 192;
        private const float Zoom = 1.08f;

        private static readonly Dictionary<int, Sprite> Rocks = new Dictionary<int, Sprite>();
        private static Sprite _starLit, _starDim;

        public static Sprite Rock(int shape, RockTint tint)
        {
            int key = (shape % ShapeCount) * 8 + (int)tint;
            if (!Rocks.TryGetValue(key, out var s) || s == null)
                Rocks[key] = s = ToSprite(RenderRock(SizePx, shape % ShapeCount, tint), SizePx, SizePx);
            return s;
        }

        public static Sprite StarLit() => _starLit != null ? _starLit : _starLit = ToSprite(RenderStar(96, true), 96, 96);
        public static Sprite StarDim() => _starDim != null ? _starDim : _starDim = ToSprite(RenderStar(96, false), 96, 96);

        public static Color TintColor(RockTint t)
        {
            switch (t)
            {
                case RockTint.Sky: return Sky;
                case RockTint.Coral: return Coral;
                case RockTint.Gold: return Sun;
                case RockTint.Dust: return Hex(0xA29FBE);
                default: return Grape;
            }
        }

        // ------------------------------------------------------------------ roca

        private static readonly float[][] Phase =
        {
            new[] { 0.3f, 1.9f, 4.1f }, new[] { 2.2f, 0.4f, 1.3f }, new[] { 4.6f, 3.1f, 0.7f }, new[] { 1.1f, 5.2f, 2.6f }
        };

        // (x, y, radio) de los cráteres de cada forma: en los bordes, para no quedar bajo la placa de la palabra.
        private static readonly float[][] Craters =
        {
            new[] { -0.62f, 0.34f, 0.15f, 0.60f, -0.36f, 0.12f, 0.06f, 0.44f, 0.09f, 0.80f, 0.08f, 0.07f },
            new[] { -0.74f, -0.20f, 0.12f, -0.30f, 0.43f, 0.11f, 0.66f, 0.30f, 0.14f, 0.34f, -0.45f, 0.09f },
            new[] { -0.52f, -0.40f, 0.13f, 0.14f, 0.45f, 0.10f, 0.74f, -0.10f, 0.12f, -0.78f, 0.22f, 0.08f },
            new[] { 0.52f, 0.40f, 0.12f, -0.66f, 0.26f, 0.14f, -0.12f, -0.46f, 0.10f, 0.78f, -0.26f, 0.09f },
        };

        private static float RockSdf(float x, float y, int shape)
        {
            var ph = Phase[shape];
            float a = Mathf.Atan2(y, x);
            float wobble = 1f + 0.06f * Mathf.Sin(3f * a + ph[0]) + 0.04f * Mathf.Sin(5f * a + ph[1]) + 0.022f * Mathf.Sin(9f * a + ph[2]);
            float d = Mathf.Sqrt(x * x / (0.9f * 0.9f) + y * y / (0.62f * 0.62f));
            return (d - wobble) * 0.5f;
        }

        public static Color32[] RenderRock(int size, int shape, RockTint tint)
        {
            var baseColor = TintColor(tint);
            bool cracked = tint == RockTint.Dust;
            var cr = Craters[shape];
            const float aa = 0.02f;
            // grietas: dos líneas en zigzag a los lados de la placa (el centro lo tapa la palabra)
            float Cracks(float x, float y) =>
                Mathf.Min(Mathf.Min(Capsule(x, y, -0.62f, 0.36f, -0.46f, 0.12f, 0.022f), Capsule(x, y, -0.46f, 0.12f, -0.58f, -0.1f, 0.022f)),
                    Mathf.Min(Mathf.Min(Capsule(x, y, -0.58f, -0.1f, -0.42f, -0.34f, 0.022f), Capsule(x, y, 0.5f, 0.4f, 0.64f, 0.14f, 0.022f)),
                        Mathf.Min(Capsule(x, y, 0.64f, 0.14f, 0.5f, -0.08f, 0.022f), Capsule(x, y, 0.5f, -0.08f, 0.66f, -0.34f, 0.022f))));

            return RenderClay(size, Zoom, 0.05f, 0.06f, aa, (x, y) => RockSdf(x, y, shape), (ref Px p, float x, float y) =>
            {
                float d = RockSdf(x, y, shape);
                float inside = Cover(d, aa);
                const float e = 0.012f;
                float nx = RockSdf(x + e, y, shape) - RockSdf(x - e, y, shape);
                float ny = RockSdf(x, y + e, shape) - RockSdf(x, y - e, shape);
                float nl = Mathf.Max(0.0001f, Mathf.Sqrt(nx * nx + ny * ny));
                nx /= nl;
                ny /= nl;
                float ndotl = nx * -0.55f + ny * 0.83f;           // luz arriba a la izquierda
                float edge = Mathf.Clamp01(1f - (-d) / 0.2f);       // 1 en el borde, 0 adentro
                // cuerpo: degradado suave (arriba izquierda más claro) y canto con volumen
                float grad = (-x * 0.45f + y * 0.7f) * 0.11f;
                var body = Shade(baseColor, Mathf.Clamp(1f + grad, 0.8f, 1.2f));
                p.Over(body, inside);
                p.Over(WithAlpha(Tint(baseColor, 0.7f), 0.85f), inside * edge * Mathf.Clamp01(ndotl) * 0.9f);     // canto claro (arriba)
                p.Over(WithAlpha(Shade(baseColor, 0.55f), 0.9f), inside * edge * Mathf.Clamp01(-ndotl) * 0.9f);   // canto oscuro (abajo)
                p.Over(new Color(1f, 1f, 1f, 0.38f), Cover(Ellipse(x, y, -0.34f, 0.24f, 0.3f, 0.1f), aa) * inside);   // brillo
                // cráteres con su sombra: la pared de arriba a la izquierda queda en sombra, la de abajo a la derecha, iluminada
                for (int i = 0; i < cr.Length; i += 3)
                {
                    float cx = cr[i], cy = cr[i + 1], r = cr[i + 2];
                    float disc = Circle(x, y, cx, cy, r);
                    p.Over(WithAlpha(Shade(baseColor, 0.8f), 1f), Cover(disc, aa) * inside);
                    float shadow = Subtract(disc, Circle(x, y, cx + r * 0.22f, cy - r * 0.22f, r * 0.9f));
                    p.Over(WithAlpha(Shade(baseColor, 0.5f), 0.9f), Cover(shadow, aa) * inside);
                    float lit = Subtract(Circle(x, y, cx - r * 0.12f, cy + r * 0.12f, r * 1.12f), Circle(x, y, cx, cy, r * 1.0f));
                    p.Over(WithAlpha(Tint(baseColor, 0.75f), 0.8f), Cover(lit, aa) * inside * 0.7f);
                }
                if (cracked) p.Over(Ink, Cover(Cracks(x, y), aa) * inside);
            });
        }

        // ------------------------------------------------------------------ estrella de la constelación

        private static float StarSdf(float x, float y) => Star(x, y, 0f, 0f, 5, 0.68f, 2.8f, 0.12f);

        public static Color32[] RenderStar(int size, bool lit) => RenderClay(size, 1.12f, lit ? 0.06f : 0.05f, lit ? 0.07f : 0.0f, 0.04f, StarSdf, (ref Px p, float x, float y) =>
        {
            float s = StarSdf(x, y);
            if (lit)
            {
                p.Over(Sun, Cover(s, 0.04f));
                p.Over(new Color(1f, 1f, 1f, 0.55f), Cover(Ellipse(x, y, -0.16f, 0.2f, 0.16f, 0.07f), 0.04f) * Cover(s + 0.04f, 0.04f));
            }
            else p.Over(new Color(0.16f, 0.19f, 0.43f, 1f), Cover(s, 0.04f));
        });
    }
}
