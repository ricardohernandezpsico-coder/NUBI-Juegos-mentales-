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
    /// Arte procedural de Lluvia de meteoros (1-oct: opción "B · brasa y estela de calor" elegida por Ricardo en la maqueta
    /// <c>docs/previews/meteoros-rocas.png</c>), con el pincel de arcilla común (<see cref="ClayRaster"/>):
    /// <list type="bullet">
    /// <item><see cref="Rock"/>: roca de arcilla con VOLUMEN (campo de alturas con domo, cráteres hundidos de borde levantado y
    /// grano, luz arriba a la izquierda), con el borde que va por delante AL ROJO VIVO (brasa naranja-coral que se ve también
    /// fuera del contorno), borde tinta y sombra dura hacia abajo. 4 formas y 5 colores; la de polvo (inventada tocada) va sin
    /// brasa y con grietas. Se estira a lo ancho según la palabra; el sprite lleva más margen (<see cref="Zoom"/>).</item>
    /// <item><see cref="HeatTrail"/>: estela de calor, con chispas o sin ellas ("quitar animaciones").</item>
    /// <item><see cref="StarLit"/> / <see cref="StarDim"/>: estrellas de la constelación (encendida y apagada).</item>
    /// </list>
    /// Colores horneados: <c>Image.color</c> en blanco. Las formas se hornean al primer uso; <see cref="Prewarm"/> las hornea
    /// de a una por cuadro mientras corre la cuenta regresiva.
    /// </summary>
    public static class MeteorSprites
    {
        public const int ShapeCount = 4;
        private const int SizePx = 176;
        /// <summary>Zoom del sprite de la roca: deja margen para el resplandor de la brasa. El cuerpo mide 1,08 de
        /// (sin brasa) y el Image se agranda <see cref="ImageScale"/> veces.</summary>
        public const float Zoom = 1.3f;
        private const float BaseZoom = 1.08f;
        /// <summary>Cuánto hay que agrandar el Image sobre el tamaño deseado de la roca para que el CUERPO mida eso.</summary>
        public const float ImageScale = Zoom / BaseZoom;

        private static readonly Dictionary<int, Sprite> Rocks = new Dictionary<int, Sprite>();
        private static Sprite _starLit, _starDim, _trailSparks, _trailPlain;

        public static Sprite Rock(int shape, RockTint tint)
        {
            int key = (shape % ShapeCount) * 8 + (int)tint;
            if (!Rocks.TryGetValue(key, out var s) || s == null)
                Rocks[key] = s = ToSprite(RenderRock(SizePx, shape % ShapeCount, tint), SizePx, SizePx);
            return s;
        }

        /// <summary>Hornea de a una las rocas de uso común (4 formas × lila, celeste, coral y sol): devuelve false cuando ya
        /// no queda ninguna. La de polvo se hornea al primer error.</summary>
        public static bool Prewarm(ref int step)
        {
            const int total = ShapeCount * 4;
            if (step >= total) return false;
            Rock(step / 4, (RockTint)(step % 4));
            step++;
            return step < total;
        }

        public static Sprite StarLit() => _starLit != null ? _starLit : _starLit = ToSprite(RenderStar(96, true), 96, 96);
        public static Sprite StarDim() => _starDim != null ? _starDim : _starDim = ToSprite(RenderStar(96, false), 96, 96);

        /// <summary>Estela de calor, fila 0 = extremo caliente (pegado a la roca; pivote abajo). Con [sparks] lleva chispas.</summary>
        public static Sprite HeatTrail(bool sparks)
        {
            if (sparks) return _trailSparks != null ? _trailSparks : _trailSparks = TrailSprite(true);
            return _trailPlain != null ? _trailPlain : _trailPlain = TrailSprite(false);
        }

        private static Sprite TrailSprite(bool sparks)
        {
            const int w = 96, h = 288;
            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Bilinear };
            tex.SetPixels32(RenderHeatTrail(w, h, sparks));
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0f), 100f);
        }

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

        private static readonly float Lx = -0.55f, Ly = 0.62f, Lz = 0.56f;      // luz: arriba a la izquierda
        private static readonly float EmberX = 0.55f, EmberY = -0.83f;           // hacia donde cae el meteoro: abajo a la derecha

        private static readonly float[][] Phase =
        {
            new[] { 0.3f, 1.9f, 4.1f, 2.2f }, new[] { 2.2f, 0.4f, 1.3f, 5.1f }, new[] { 4.6f, 3.1f, 0.7f, 3.3f }, new[] { 1.1f, 5.2f, 2.6f, 0.9f }
        };

        // (x, y, radio) de los cráteres de cada forma: grandes y en los bordes, para que se vean sin tapar la placa.
        private static readonly float[][] Craters =
        {
            new[] { -0.62f, 0.34f, 0.17f, 0.60f, -0.36f, 0.14f, 0.06f, 0.46f, 0.10f, 0.80f, 0.10f, 0.08f, -0.74f, -0.22f, 0.08f },
            new[] { -0.74f, -0.20f, 0.14f, -0.30f, 0.45f, 0.12f, 0.66f, 0.30f, 0.16f, 0.34f, -0.46f, 0.10f, 0.0f, 0.5f, 0.07f },
            new[] { -0.52f, -0.40f, 0.15f, 0.14f, 0.47f, 0.11f, 0.74f, -0.10f, 0.13f, -0.78f, 0.22f, 0.09f, 0.45f, -0.45f, 0.07f },
            new[] { 0.52f, 0.40f, 0.14f, -0.66f, 0.26f, 0.16f, -0.12f, -0.48f, 0.11f, 0.78f, -0.26f, 0.10f, -0.4f, 0.5f, 0.07f },
        };

        private static float Hash(float x, float y)
        {
            float h = Mathf.Sin(x * 127.1f + y * 311.7f) * 43758.5453f;
            return h - Mathf.Floor(h);
        }

        private static float Noise(float x, float y)
        {
            float ix = Mathf.Floor(x), iy = Mathf.Floor(y), fx = x - ix, fy = y - iy;
            fx = fx * fx * (3f - 2f * fx);
            fy = fy * fy * (3f - 2f * fy);
            float a = Hash(ix, iy), b = Hash(ix + 1f, iy), c = Hash(ix, iy + 1f), d = Hash(ix + 1f, iy + 1f);
            return Mathf.Lerp(Mathf.Lerp(a, b, fx), Mathf.Lerp(c, d, fx), fy);
        }

        private static float Grain(float x, float y) => 0.6f * Noise(x * 9f, y * 9f) + 0.3f * Noise(x * 21f, y * 21f) + 0.1f * Noise(x * 47f, y * 47f);

        private static float RockSdf(float x, float y, int shape)
        {
            var ph = Phase[shape];
            float a = Mathf.Atan2(y, x);
            float wobble = 1f + 0.07f * Mathf.Sin(3f * a + ph[0]) + 0.045f * Mathf.Sin(5f * a + ph[1]) + 0.03f * Mathf.Sin(9f * a + ph[2]) + 0.015f * Mathf.Sin(14f * a + ph[3]);
            float d = Mathf.Sqrt(x * x / (0.9f * 0.9f) + y * y / (0.62f * 0.62f));
            return (d - wobble) * 0.5f;
        }

        /// <summary>Altura de la roca (0 en el borde, hasta ~0,55 en el centro) con cráteres hundidos de borde levantado y grano.</summary>
        private static float Height(float x, float y, int shape)
        {
            float d = RockSdf(x, y, shape);
            float h = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(-d / 0.42f)) * 0.55f;
            var cr = Craters[shape];
            for (int i = 0; i < cr.Length; i += 3)
            {
                float dd = Circle(x, y, cr[i], cr[i + 1], cr[i + 2]) / cr[i + 2]; // < 0 adentro
                h -= 0.24f * Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(-dd) * 1.7f);
                h += 0.08f * Mathf.Exp(-dd * dd * 16f);
            }
            h += (Grain(x + shape * 3.1f, y) - 0.5f) * 0.08f;
            return h;
        }

        private static Color Mul(Color c, float k) => new Color(Mathf.Clamp01(c.r * k), Mathf.Clamp01(c.g * k), Mathf.Clamp01(c.b * k), c.a);
        private static Color Add(Color c, float k) => new Color(Mathf.Clamp01(c.r + k), Mathf.Clamp01(c.g + k), Mathf.Clamp01(c.b + k), c.a);

        // grietas de la roca de polvo: a los lados de la placa (el centro lo tapa la palabra)
        private static float Cracks(float x, float y) =>
            Mathf.Min(Mathf.Min(Capsule(x, y, -0.62f, 0.36f, -0.46f, 0.12f, 0.022f), Capsule(x, y, -0.46f, 0.12f, -0.58f, -0.1f, 0.022f)),
                Mathf.Min(Mathf.Min(Capsule(x, y, -0.58f, -0.1f, -0.42f, -0.34f, 0.022f), Capsule(x, y, 0.5f, 0.4f, 0.64f, 0.14f, 0.022f)),
                    Mathf.Min(Capsule(x, y, 0.64f, 0.14f, 0.5f, -0.08f, 0.022f), Capsule(x, y, 0.5f, -0.08f, 0.66f, -0.34f, 0.022f))));

        public static Color32[] RenderRock(int size, int shape, RockTint tint)
        {
            shape = ((shape % ShapeCount) + ShapeCount) % ShapeCount;
            bool dust = tint == RockTint.Dust;
            var baseColor = TintColor(tint);
            const float aa = 0.02f;
            float ll = Mathf.Sqrt(Lx * Lx + Ly * Ly + Lz * Lz);
            float lx = Lx / ll, ly = Ly / ll, lz = Lz / ll;
            return RenderClay(size, Zoom, 0.05f, 0.06f, aa, (x, y) => RockSdf(x, y, shape), (ref Px p, float x, float y) =>
            {
                float d = RockSdf(x, y, shape);
                float inside = Cover(d, aa);
                const float e = 0.012f;
                float gx = (Height(x + e, y, shape) - Height(x - e, y, shape)) / (2f * e);
                float gy = (Height(x, y + e, shape) - Height(x, y - e, shape)) / (2f * e);
                float nx = -gx * 0.9f, ny = -gy * 0.9f, nz = 1f;
                float nl = Mathf.Sqrt(nx * nx + ny * ny + nz * nz);
                nx /= nl; ny /= nl; nz /= nl;
                float diff = Mathf.Max(0f, nx * lx + ny * ly + nz * lz);
                float grain = (Grain(x * 1.3f + 7f, y * 1.3f) - 0.5f) * 0.16f;
                // oclusión: más oscuro abajo a la derecha y en el pie de la roca
                float edge = Mathf.Clamp01(1f - (-d) / 0.22f);
                float ao = 1f - 0.22f * edge * Mathf.Clamp01(0.5f - (nx * lx + ny * ly) * 0.8f);
                float k = (0.42f + 0.95f * diff) * ao + grain;
                var c = Mul(baseColor, k);
                c = Add(c, 0.28f * Mathf.Pow(diff, 14f));                               // brillo de la arcilla
                p.Over(c, inside);
                p.Over(WithAlpha(Tint(baseColor, 0.8f), 0.55f), inside * Mathf.Clamp01(edge * (ny * 0.7f - nx * 0.7f)) * 0.8f); // canto claro (arriba a la izquierda)

                if (dust)
                {
                    p.Over(Ink, Cover(Cracks(x, y), aa) * inside);
                    return;
                }
                // brasa en el borde que va por delante
                float gdx = RockSdf(x + e, y, shape) - RockSdf(x - e, y, shape);
                float gdy = RockSdf(x, y + e, shape) - RockSdf(x, y - e, shape);
                float gl = Mathf.Max(0.0001f, Mathf.Sqrt(gdx * gdx + gdy * gdy));
                float facing = Mathf.Clamp01((gdx / gl) * EmberX + (gdy / gl) * EmberY);
                facing = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(facing * 1.25f - 0.05f));
                float rim = Mathf.Clamp01(1f - (-d) / 0.2f);
                float heat = facing * rim * (0.75f + 0.5f * Grain(x * 2f, y * 2f));
                var hot = Color.Lerp(Coral, Sun, Mathf.Clamp01(heat * 1.3f - 0.25f));
                p.Over(WithAlpha(hot, 1f), inside * Mathf.Clamp01(heat * 1.25f));
                // la brasa también se ve fuera del contorno (resplandor), apagado antes del borde del sprite
                float outside = 1f - Cover(d - 0.012f, aa * 2f);
                float edgeFade = Mathf.Clamp01((Zoom * 0.96f - Mathf.Max(Mathf.Abs(x), Mathf.Abs(y))) / 0.14f);
                float glow = Mathf.Exp(-Mathf.Max(0f, d) * 16f) * facing * 0.6f * edgeFade;
                p.Over(WithAlpha(Color.Lerp(Orange, Coral, 0.4f), glow), (1f - inside) * outside);
            });
        }

        // ------------------------------------------------------------------ estela de calor

        /// <summary>Estela de calor: fila 0 = extremo caliente (pegado a la roca); arriba se enfría hacia lila y se apaga.
        /// Se estira y se gira según el rumbo. Con [withSparks] lleva chispas que se alejan.</summary>
        public static Color32[] RenderHeatTrail(int w, int h, bool withSparks)
        {
            var px = new Color32[w * h];
            const int sparks = 26;
            for (int py = 0; py < h; py++)
            {
                float t = (py + 0.5f) / h;
                for (int pxl = 0; pxl < w; pxl++)
                {
                    float u = ((pxl + 0.5f) / w) * 2f - 1f;
                    float half = Mathf.Pow(1f - t, 0.75f) * 0.85f + 0.03f;
                    float body = Mathf.SmoothStep(1f, 0f, Mathf.Clamp01((Mathf.Abs(u) - half * 0.55f) / (half * 0.55f + 0.001f)));
                    float inten = Mathf.Pow(1f - t, 1.5f);
                    var core = t < 0.3f ? Color.Lerp(Sun, Orange, t / 0.3f) : Color.Lerp(Orange, Coral, Mathf.Clamp01((t - 0.3f) / 0.4f));
                    core = Color.Lerp(core, Grape, Mathf.Clamp01((t - 0.55f) / 0.45f));
                    float wob = 0.85f + 0.3f * Grain(u * 1.5f + 3f, t * 4f);
                    var acc = new Px();
                    acc.Over(WithAlpha(core, 1f), Mathf.Clamp01(body * inten * 1.1f * wob));
                    for (int s = 0; withSparks && s < sparks; s++)
                    {
                        float sy = Hash(s * 1.7f, 3.3f) * 0.92f;
                        float sx = (Hash(s * 2.9f, 8.1f) * 2f - 1f) * (0.15f + sy * 0.9f) * 0.9f;
                        float r = 0.018f + Hash(s * 4.3f, 1.1f) * 0.02f;
                        float dd = Mathf.Sqrt((u - sx) * (u - sx) + ((t - sy) * h / w * 2f) * ((t - sy) * h / w * 2f));
                        float a = Mathf.Clamp01(1f - dd / r) * (1f - sy);
                        if (a > 0f) acc.Over(WithAlpha(Color.Lerp(Sun, Color.white, 0.5f), 1f), a);
                    }
                    px[py * w + pxl] = acc.ToColor32();
                }
            }
            return px;
        }

        // ------------------------------------------------------------------ estrella de la constelación

        private static float StarSdf(float x, float y) => Circle(x, y, 0f, 0f, 0.56f);   // lucero REDONDO (nada de estrella con puntas)

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
