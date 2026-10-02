using System.Collections.Generic;
using UnityEngine;
using NeuroVida.Games.Shared;
using static NeuroVida.Games.Shared.ClayRaster;

namespace NeuroVida.Games.Cosecha
{
    /// <summary>
    /// Arte de "Cosecha de palabras" con el pincel de arcilla común (<see cref="ClayRaster"/>): el planeta-huerto de tierra (marrón
    /// cálido con vetas, relieve y cráteres), su pasto, las plantas (brote, flor, tulipán, arbusto, girasol, hongo y flor dorada),
    /// el árbol dorado de la palabra estrella, la ficha-luna (blanca: se tiñe con <c>Image.color</c>) y los íconos de los botones.
    /// Todo se hornea una vez (durante la cuenta regresiva) y se reutiliza.
    /// </summary>
    public static class CosechaSprites
    {
        private const float Aa = 0.02f;
        public static readonly Color Earth = new Color(0.62f, 0.40f, 0.26f, 1f);
        public static readonly Color EarthDark = new Color(0.48f, 0.30f, 0.20f, 1f);
        public static readonly Color EarthLight = new Color(0.75f, 0.53f, 0.35f, 1f);
        public static readonly Color EarthShade = new Color(0.27f, 0.16f, 0.17f, 1f);
        public static readonly Color Grass = new Color(0.44f, 0.78f, 0.33f, 1f);
        public static readonly Color GrassDark = new Color(0.27f, 0.59f, 0.27f, 1f);
        public static readonly Color Bush = new Color(0.34f, 0.69f, 0.38f, 1f);
        public static readonly Color Gold = new Color(1f, 0.84f, 0.36f, 1f);
        public static readonly Color Sunflower = new Color(1f, 0.77f, 0.21f, 1f);
        public static readonly Color Shroom = new Color(0.93f, 0.38f, 0.35f, 1f);
        public static readonly Color Soil = new Color(0.48f, 0.30f, 0.20f, 1f);

        private const int PlanetPx = 256, PlanetZoom100 = 114;      // zoom 1,14
        private const float PlanetZoom = 1.14f, PlanetRadius = 0.9f;
        private const int PlantPx = 128, TreePx = 192, MoonPx = 128;

        private static Sprite _planet, _grassCap, _tufts, _moon, _seed, _tree, _seedIcon, _backIcon;
        private static readonly Dictionary<PlantKind, Sprite> Plants = new Dictionary<PlantKind, Sprite>();

        /// <summary>Qué fracción del alto del dibujo de una planta queda bajo su base (para fijar el pivote: la base apoya en el suelo).</summary>
        public const float PlantBase = 0.09f;
        /// <summary>Del radio del planeta al ancho del sprite: el dibujo ocupa (radio × 2 / 0,9 × 1,14) del lado.</summary>
        public const float PlanetSpriteToRadius = PlanetZoom / PlanetRadius;

        // ------------------------------------------------------------------ API

        public static Sprite Planet() => _planet != null ? _planet : _planet = ToSprite(RenderPlanet(PlanetPx), PlanetPx, PlanetPx);
        public static Sprite GrassCap() => _grassCap != null ? _grassCap : _grassCap = ToSprite(RenderGrassCap(PlanetPx), PlanetPx, PlanetPx);
        public static Sprite Tufts() => _tufts != null ? _tufts : _tufts = ToSprite(RenderTufts(PlanetPx), PlanetPx, PlanetPx);
        public static Sprite Moon() => _moon != null ? _moon : _moon = ToSprite(RenderMoon(MoonPx), MoonPx, MoonPx);
        public static Sprite Seed() => _seed != null ? _seed : _seed = ToSprite(RenderSeed(64), 64, 64);
        public static Sprite GoldenTree() => _tree != null ? _tree : _tree = ToSprite(RenderTree(TreePx), TreePx, TreePx);
        public static Sprite SeedIcon() => _seedIcon != null ? _seedIcon : _seedIcon = ToSprite(RenderFlat(96, SeedIconShape, Ink), 96, 96);
        public static Sprite BackIcon() => _backIcon != null ? _backIcon : _backIcon = ToSprite(RenderFlat(96, BackIconShape, Ink), 96, 96);

        public static Sprite Plant(PlantKind kind)
        {
            if (Plants.TryGetValue(kind, out var s) && s != null) return s;
            s = ToSprite(RenderPlant(kind, PlantPx), PlantPx, PlantPx);
            Plants[kind] = s;
            return s;
        }

        // ------------------------------------------------------------------ pintura por partes (silueta = unión de las partes)

        /// <summary>Escalón suave entre [a] y [b] (como smoothstep de los shaders).</summary>
        private static float Sm(float a, float b, float x)
        {
            float t = Mathf.Clamp01((x - a) / (b - a));
            return t * t * (3f - 2f * t);
        }

        private struct Part
        {
            public ShapeFn Shape;
            public Color Fill;
            public Part(ShapeFn shape, Color fill) { Shape = shape; Fill = fill; }
        }

        private static Color32[] RenderParts(int size, float zoom, List<Part> parts, PaintFn extra = null)
        {
            ShapeFn body = (x, y) =>
            {
                float d = float.MaxValue;
                for (int i = 0; i < parts.Count; i++) d = Mathf.Min(d, parts[i].Shape(x, y));
                return d;
            };
            return RenderClay(size, zoom, 0.05f, 0.05f, Aa, body, (ref Px p, float x, float y) =>
            {
                for (int i = 0; i < parts.Count; i++)
                {
                    float d = parts[i].Shape(x, y);
                    float c = Cover(d, Aa);
                    if (c <= 0f) continue;
                    // relieve: claro arriba a la izquierda, algo más oscuro abajo a la derecha
                    float lit = Mathf.Clamp(1.06f + (-x * 0.25f + y * 0.3f) * 0.2f, 0.84f, 1.14f);
                    p.Over(Shade(parts[i].Fill, lit), c);
                }
                extra?.Invoke(ref p, x, y);
            });
        }

        // ------------------------------------------------------------------ el planeta

        private static float PlanetBody(float x, float y) => Circle(x, y, 0f, 0f, PlanetRadius);

        private static Color32[] RenderPlanet(int size) =>
            RenderClay(size, PlanetZoom, 0.04f, 0.07f, Aa, PlanetBody, (ref Px p, float x, float y) =>
            {
                float inside = Cover(PlanetBody(x, y), Aa);
                if (inside <= 0f) return;
                // base de tierra
                p.Over(Earth, inside);
                // vetas onduladas oscuras
                float v = Mathf.Sin((y * 6.2f + Mathf.Sin(x * 3.1f + 0.7f) * 0.45f + Mathf.Sin(x * 7.3f) * 0.12f) * Mathf.PI);
                p.Over(WithAlpha(EarthDark, 0.95f), Sm(0.5f, 0.72f, v) * inside);
                // manchas claras de relieve
                float n = Mathf.Sin(x * 5.3f + 1.1f) * Mathf.Sin(y * 4.7f + 0.3f) + 0.35f * Mathf.Sin(x * 11f + y * 9f);
                p.Over(WithAlpha(EarthLight, 0.85f), Sm(0.45f, 0.75f, n) * inside);
                // cráteres: sombra abajo y borde claro arriba
                float[] cr = { -0.35f, -0.30f, 0.15f, 0.40f, -0.55f, 0.12f, 0.12f, -0.66f, 0.10f, -0.52f, 0.05f, 0.09f, 0.62f, -0.15f, 0.11f };
                for (int i = 0; i < cr.Length; i += 3)
                {
                    float c = Ellipse(x, y, cr[i], cr[i + 1], cr[i + 2], cr[i + 2] * 0.55f);
                    float rim = Ellipse(x, y - 0.012f, cr[i], cr[i + 1], cr[i + 2], cr[i + 2] * 0.55f);
                    p.Over(WithAlpha(EarthLight, 0.9f), Cover(rim, Aa) * (1f - Cover(c, Aa)) * 0.9f);
                    p.Over(EarthDark, Cover(c, Aa) * inside);
                }
                // lado en sombra (abajo a la derecha)
                float shade = Mathf.Clamp01(x * 0.55f - y * 0.75f + 0.35f);
                p.Over(WithAlpha(EarthShade, 0.55f), shade * shade * inside);
                // brillo de arcilla arriba a la izquierda
                float ring = Mathf.Abs(Circle(x, y, 0f, 0f, 0.77f)) - 0.018f;
                p.Over(new Color(1f, 0.93f, 0.78f, 0.85f), Cover(ring, Aa) * Sm(0.35f, 0.7f, -x * 0.7f + y * 0.7f) * inside);
            });

        /// <summary>Casquete de pasto sobre la mitad de arriba del planeta (mismo tamaño y encuadre que el planeta, para superponerlos).</summary>
        private static Color32[] RenderGrassCap(int size)
        {
            var px = new Color32[size * size];
            for (int py = 0; py < size; py++)
                for (int pxl = 0; pxl < size; pxl++)
                {
                    float x = ((pxl + 0.5f) / size * 2f - 1f) * PlanetZoom;
                    float y = ((py + 0.5f) / size * 2f - 1f) * PlanetZoom;
                    float line = 0.30f + 0.06f * Mathf.Sin(x * 9f + 0.4f) + 0.04f * Mathf.Sin(x * 17f);
                    float inside = Cover(PlanetBody(x, y) + 0.015f, Aa);
                    float cap = Cover(line - y, Aa * 2f) * inside;
                    float edge = Cover(Mathf.Abs(y - line) - 0.03f, Aa) * inside;
                    var c = new Px();
                    c.Over(Grass, cap);
                    c.Over(GrassDark, edge * 0.9f);
                    c.Over(new Color(1f, 1f, 1f, 0.2f), Cover(Mathf.Abs(Circle(x, y, 0f, 0f, 0.82f)) - 0.02f, Aa) * cap * Mathf.Clamp01(-x + y));
                    px[py * size + pxl] = c.ToColor32();
                }
            return px;
        }

        /// <summary>Matitas de pasto sueltas en la cara de arriba (el pasto ralo del comienzo).</summary>
        private static Color32[] RenderTufts(int size)
        {
            var px = new Color32[size * size];
            float[] t = { -0.52f, 0.62f, -0.30f, 0.52f, -0.05f, 0.74f, 0.22f, 0.66f, 0.46f, 0.50f, -0.62f, 0.38f, 0.12f, 0.44f, 0.66f, 0.30f };
            for (int py = 0; py < size; py++)
                for (int pxl = 0; pxl < size; pxl++)
                {
                    float x = ((pxl + 0.5f) / size * 2f - 1f) * PlanetZoom;
                    float y = ((py + 0.5f) / size * 2f - 1f) * PlanetZoom;
                    float inside = Cover(PlanetBody(x, y) + 0.02f, Aa);
                    float cov = 0f;
                    for (int i = 0; i < t.Length; i += 2)
                        for (int b = -1; b <= 1; b++)
                            cov = Mathf.Max(cov, Cover(Capsule(x, y, t[i] + b * 0.035f, t[i + 1], t[i] + b * 0.065f, t[i + 1] + 0.1f + 0.015f * (b + 1), 0.016f), Aa));
                    var c = new Px();
                    c.Over(Grass, cov * inside);
                    px[py * size + pxl] = c.ToColor32();
                }
            return px;
        }

        // ------------------------------------------------------------------ plantas

        private static float StemTo(float x, float y, float top) => Capsule(x, y, 0f, -0.88f, 0f, top, 0.065f);
        private static float LeafL(float x, float y, float h) => Capsule(x, y, -0.02f, h, -0.36f, h + 0.2f, 0.12f);
        private static float LeafR(float x, float y, float h) => Capsule(x, y, 0.02f, h, 0.36f, h + 0.2f, 0.12f);

        private static Color32[] RenderPlant(PlantKind kind, int size)
        {
            var parts = new List<Part>();
            switch (kind)
            {
                case PlantKind.Sprout:
                    parts.Add(new Part((x, y) => StemTo(x, y, -0.35f), GrassDark));
                    parts.Add(new Part((x, y) => Capsule(x, y, 0f, -0.4f, -0.46f, -0.08f, 0.15f), Grass));
                    parts.Add(new Part((x, y) => Capsule(x, y, 0f, -0.4f, 0.46f, -0.08f, 0.15f), Grass));
                    break;
                case PlantKind.Flower:
                case PlantKind.GoldenFlower:
                {
                    bool gold = kind == PlantKind.GoldenFlower;
                    parts.Add(new Part((x, y) => StemTo(x, y, 0.2f), GrassDark));
                    parts.Add(new Part((x, y) => LeafL(x, y, -0.45f), Grass));
                    parts.Add(new Part((x, y) => LeafR(x, y, -0.45f), Grass));
                    for (int k = 0; k < 6; k++)
                    {
                        float a = k * Mathf.PI / 3f + 0.3f;
                        float cx = 0.27f * Mathf.Cos(a), cy = 0.5f + 0.27f * Mathf.Sin(a);
                        parts.Add(new Part((x, y) => Circle(x, y, cx, cy, 0.17f), gold ? Gold : Coral));
                    }
                    parts.Add(new Part((x, y) => Circle(x, y, 0f, 0.5f, 0.15f), gold ? Cream : Sun));
                    break;
                }
                case PlantKind.Tulip:
                    parts.Add(new Part((x, y) => StemTo(x, y, 0.0f), GrassDark));
                    parts.Add(new Part((x, y) => LeafL(x, y, -0.5f), Grass));
                    parts.Add(new Part((x, y) => LeafR(x, y, -0.5f), Grass));
                    parts.Add(new Part((x, y) => Union(Polygon(x, y, new[] { -0.32f, 0.78f, -0.17f, 0.42f, 0f, 0.8f, 0.17f, 0.42f, 0.32f, 0.78f, 0.28f, 0.2f, -0.28f, 0.2f }),
                                                          Circle(x, y, 0f, 0.26f, 0.29f)), Sky));
                    break;
                case PlantKind.Bush:
                    parts.Add(new Part((x, y) => Circle(x, y, -0.4f, -0.52f, 0.4f), Bush));
                    parts.Add(new Part((x, y) => Circle(x, y, 0.4f, -0.52f, 0.4f), Bush));
                    parts.Add(new Part((x, y) => Circle(x, y, 0f, -0.2f, 0.5f), Bush));
                    parts.Add(new Part((x, y) => Circle(x, y, -0.24f, -0.3f, 0.07f), Coral));
                    parts.Add(new Part((x, y) => Circle(x, y, 0.28f, -0.1f, 0.07f), Coral));
                    parts.Add(new Part((x, y) => Circle(x, y, 0.08f, -0.55f, 0.07f), Coral));
                    break;
                case PlantKind.Sunflower:
                    parts.Add(new Part((x, y) => StemTo(x, y, 0.1f), GrassDark));
                    parts.Add(new Part((x, y) => LeafL(x, y, -0.5f), Grass));
                    parts.Add(new Part((x, y) => LeafR(x, y, -0.5f), Grass));
                    for (int k = 0; k < 12; k++)
                    {
                        float a = k * Mathf.PI / 6f;
                        float c = Mathf.Cos(a), s = Mathf.Sin(a);
                        parts.Add(new Part((x, y) => Capsule(x, y, c * 0.2f, 0.48f + s * 0.2f, c * 0.5f, 0.48f + s * 0.5f, 0.09f), Sunflower));
                    }
                    parts.Add(new Part((x, y) => Circle(x, y, 0f, 0.48f, 0.24f), new Color(0.48f, 0.30f, 0.2f, 1f)));
                    break;
                case PlantKind.Mushroom:
                    parts.Add(new Part((x, y) => RoundBox(x, y, 0f, -0.58f, 0.15f, 0.3f, 0.06f), Cream));
                    parts.Add(new Part((x, y) => Intersect(Ellipse(x, y, 0f, -0.22f, 0.58f, 0.44f), -(y + 0.24f)), Shroom));
                    parts.Add(new Part((x, y) => Circle(x, y, -0.26f, -0.02f, 0.08f), Cream));
                    parts.Add(new Part((x, y) => Circle(x, y, 0.1f, 0.06f, 0.09f), Cream));
                    parts.Add(new Part((x, y) => Circle(x, y, 0.34f, -0.1f, 0.07f), Cream));
                    break;
            }
            return RenderParts(size, 1.1f, parts);
        }

        private static Color32[] RenderTree(int size)
        {
            var parts = new List<Part>
            {
                new Part((x, y) => RoundBox(x, y, 0f, -0.62f, 0.12f, 0.34f, 0.05f), new Color(0.69f, 0.47f, 0.30f, 1f)),
                new Part((x, y) => Circle(x, y, -0.36f, 0.08f, 0.38f), Gold),
                new Part((x, y) => Circle(x, y, 0.36f, 0.08f, 0.38f), Gold),
                new Part((x, y) => Circle(x, y, 0f, 0.38f, 0.44f), Gold),
                new Part((x, y) => Circle(x, y, -0.16f, -0.22f, 0.3f), Gold),
                new Part((x, y) => Circle(x, y, 0.16f, -0.22f, 0.3f), Gold),
            };
            return RenderParts(size, 1.1f, parts, (ref Px p, float x, float y) =>
            {
                // brillo blanco arriba a la izquierda de la copa
                float ring = Mathf.Abs(Circle(x, y, 0f, 0.38f, 0.32f)) - 0.025f;
                p.Over(new Color(1f, 1f, 1f, 0.8f), Cover(ring, Aa) * Sm(0.2f, 0.7f, -x + y) * Cover(Circle(x, y, 0f, 0.38f, 0.44f) + 0.05f, Aa));
            });
        }

        // ------------------------------------------------------------------ ficha-luna, semilla e íconos

        private static float MoonBody(float x, float y) => Circle(x, y, 0f, 0f, 0.92f);

        /// <summary>La ficha-luna: círculo blanco con cráteres tenues y brillo (la tiñe Image.color); la letra va encima, en el texto.</summary>
        private static Color32[] RenderMoon(int size) =>
            RenderClay(size, 1.1f, 0.06f, 0.08f, Aa, MoonBody, (ref Px p, float x, float y) =>
            {
                float inside = Cover(MoonBody(x, y), Aa);
                p.Over(Color.white, inside);
                float shade = Mathf.Clamp01(x * 0.5f - y * 0.6f + 0.2f);
                p.Over(new Color(0.55f, 0.52f, 0.7f, 0.5f), shade * shade * inside);
                float[] cr = { -0.52f, 0.38f, 0.12f, 0.5f, -0.46f, 0.1f, 0.18f, 0.7f, 0.06f };
                for (int i = 0; i < cr.Length; i += 3)
                    p.Over(new Color(0.4f, 0.36f, 0.6f, 0.25f), Cover(Ellipse(x, y, cr[i], cr[i + 1], cr[i + 2], cr[i + 2] * 0.8f), Aa) * inside);
                float ring = Mathf.Abs(Circle(x, y, 0f, 0f, 0.74f)) - 0.025f;
                p.Over(new Color(1f, 1f, 1f, 0.9f), Cover(ring, Aa) * Sm(0.3f, 0.8f, -x * 0.7f + y * 0.7f) * inside);
            });

        private static Color32[] RenderSeed(int size) =>
            RenderClay(size, 1.1f, 0.07f, 0.07f, Aa, (x, y) => Ellipse(x, y, 0f, 0f, 0.48f, 0.72f), (ref Px p, float x, float y) =>
            {
                float body = Cover(Ellipse(x, y, 0f, 0f, 0.48f, 0.72f), Aa);
                p.Over(new Color(0.71f, 0.49f, 0.29f, 1f), body);
                p.Over(new Color(1f, 0.9f, 0.7f, 0.8f), Cover(Ellipse(x, y, -0.16f, 0.22f, 0.1f, 0.24f), Aa) * body);
            });

        private static float SeedIconShape(float x, float y)
        {
            float stem = Capsule(x, y, 0f, -0.7f, 0f, -0.05f, 0.1f);
            float l = Capsule(x, y, -0.02f, -0.1f, -0.58f, 0.3f, 0.2f);
            float r = Capsule(x, y, 0.02f, -0.1f, 0.58f, 0.3f, 0.2f);
            return Union(stem, Union(l, r));
        }

        private static float BackIconShape(float x, float y)
        {
            float arrow = Polygon(x, y, new[] { -0.85f, 0f, -0.3f, 0.5f, 0.85f, 0.5f, 0.85f, -0.5f, -0.3f, -0.5f });
            // la equis adentro se recorta para que se lea como "borrar"
            float cross = Union(Capsule(x, y, 0.0f, -0.22f, 0.5f, 0.22f, 0.07f), Capsule(x, y, 0.0f, 0.22f, 0.5f, -0.22f, 0.07f));
            return Subtract(arrow, cross);
        }

        public static Color32[] RenderFlat(int size, ShapeFn shape, Color color)
        {
            var px = new Color32[size * size];
            for (int py = 0; py < size; py++)
                for (int pxl = 0; pxl < size; pxl++)
                {
                    float x = (pxl + 0.5f) / size * 2f - 1f;
                    float y = (py + 0.5f) / size * 2f - 1f;
                    float a = Cover(shape(x, y), 0.04f);
                    px[py * size + pxl] = new Color32((byte)(color.r * 255f), (byte)(color.g * 255f), (byte)(color.b * 255f), (byte)(a * 255f));
                }
            return px;
        }
    }
}
