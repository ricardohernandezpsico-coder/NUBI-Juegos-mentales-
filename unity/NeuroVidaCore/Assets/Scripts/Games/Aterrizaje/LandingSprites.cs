using System.Collections.Generic;
using UnityEngine;
using NeuroVida.Games.Shared;
using static NeuroVida.Games.Shared.ClayRaster;

namespace NeuroVida.Games.Aterrizaje
{
    /// <summary>
    /// Arte procedural de Aterrizaje Lunar (renovación del 10-oct, sin assets), con el pincel de arcilla común (<see cref="ClayRaster"/>) y las medidas y colores del boceto aprobado (docs/previews/aterrizaje-boceto.html): la nave (<see cref="Lander"/>), el gallardete de la bandera, la Tierra,
    /// la luna con sus cráteres, las dos cordilleras (que se desplazan despacio: nada fijo junto a la regla), la nebulosa, el satélite, la estrella fugaz, la cúpula del marcador y las insignias ✓ / aspa. Las funciones <c>Render*</c> devuelven los píxeles (fila 0 = abajo) para poder verlos fuera de Unity.
    /// Colores horneados: <c>Image.color</c> en blanco (salvo donde se dice).
    /// </summary>
    public static class LandingSprites
    {
        public static readonly Color Gold = Hex(0xFFC94A), Cyan = Hex(0x7FD8FF), LimeColor = Hex(0xA6E36B), CoralColor = Hex(0xFF8A6B), CabinColor = Hex(0xE9E6FF), RulerColor = Hex(0xF3EBD3), NoticeFill = Hex(0x1F1B4E), DomeOff = Hex(0x3A3486);

        private const float Aa = 0.03f;
        private static readonly Dictionary<int, Sprite> Cache = new Dictionary<int, Sprite>();

        // ------------------------------------------------------------------ la nave

        /// <summary>La nave se dibuja en un cuadro de 72 × 72 dp del boceto (media-lado 36), que en pantalla es ×1,25.</summary>
        public const float LanderUnit = 36f;

        public static Sprite Lander() => Get(1, () => ToSprite(RenderLander(256), 256, 256));

        private static float U(float dp) => dp / LanderUnit;

        // formas en el espacio del cuadro (-1..1, y hacia arriba); las coordenadas del boceto (dp, y hacia abajo) pasan por U() y se les cambia el signo a la y
        private static float Base(float x, float y) => RoundBox(x, y, 0f, -U(6f), U(22f), U(8f), U(6f));

        /// <summary>La cabina: media circunferencia arriba (radio 17 dp) y un tramo recto hasta la base.</summary>
        private static float Cabin(float x, float y)
        {
            float top = Mathf.Max(Circle(x, y, 0f, U(6f), U(17f)), U(6f) - y);
            float stem = RoundBox(x, y, 0f, U(3f), U(17f), U(3f), 0.001f);
            return Mathf.Min(top, stem);
        }

        private static float LanderBody(float x, float y) => Mathf.Min(Base(x, y), Cabin(x, y));

        private static float Legs(float x, float y)
        {
            float d = 1e5f;
            foreach (float s in new[] { -1f, 1f })
            {
                d = Mathf.Min(d, Capsule(x, y, s * U(12f), -U(8f), s * U(24f), -U(24f), U(1.6f)));
                d = Mathf.Min(d, Capsule(x, y, s * U(30f), -U(24f), s * U(18f), -U(24f), U(1.6f)));
            }
            return d;
        }

        /// <summary>Relleno de arcilla: arriba más claro y abajo más oscuro (t = 0 arriba, 1 abajo), como el <c>clay()</c> del boceto.</summary>
        private static Color ClayFill(Color c, float t) => t < 0.55f ? Color.Lerp(Tint(c, 0.32f), c, t / 0.55f) : Color.Lerp(c, Shade(c, 0.88f), Mathf.Clamp01((t - 0.55f) / 0.45f));

        public static Color32[] RenderLander(int size) => RenderClay(size, 1f, U(3f), U(3f), 0.02f, LanderBody, (ref Px p, float x, float y) =>
        {
            const float aa = 0.02f;
            p.Over(Ink, Cover(Legs(x, y), aa));
            // la base (sol): de -2 a +14 dp por debajo del centro
            float b = Base(x, y);
            p.Over(ClayFill(Gold, Mathf.Clamp01((U(-2f) * -1f - y) / U(16f))), Cover(b, aa));
            // la cabina (crema) con su borde y su brillo arriba a la izquierda
            float c = Cabin(x, y);
            p.Over(Ink, Cover(c - U(3f), aa));
            p.Over(ClayFill(CabinColor, Mathf.Clamp01((U(23f) - y) / U(23f))), Cover(c, aa));
            p.Over(new Color(1f, 1f, 1f, 0.38f), Cover(Ellipse(x, y, -U(6.1f), U(8.8f), U(8f), U(4.2f)), aa) * Cover(c, aa));
            // la ventanilla: celeste con su aro de tinta
            float w = Circle(x, y, 0f, U(9f), U(7f));
            p.Over(Ink, Cover(w - U(1.2f), aa));
            p.Over(Cyan, Cover(w + U(1.2f), aa));
        });

        // ------------------------------------------------------------------ la bandera (gallardete)

        /// <summary>El gallardete de la bandera: un triángulo sol de 26 × 15 dp con borde de tinta. El sprite cubre 36 × 24 dp con el vértice de arriba a la izquierda en (3, 3).</summary>
        public const float PennantW = 36f, PennantH = 24f, PennantPxPerDp = 6f;

        public static Sprite Pennant() => Get(2, () => ToSpriteRect(RenderPennant(), (int)(PennantW * PennantPxPerDp), (int)(PennantH * PennantPxPerDp)));

        public static Color32[] RenderPennant()
        {
            int w = (int)(PennantW * PennantPxPerDp), h = (int)(PennantH * PennantPxPerDp);
            var px = new Color32[w * h];
            float aa = 1.5f / PennantPxPerDp;
            float[] tri = { 3f, 3f, 29f, 10f, 3f, 18f };                                   // x, y (hacia abajo) en dp del sprite
            for (int py = 0; py < h; py++)
                for (int x = 0; x < w; x++)
                {
                    float dx = (x + 0.5f) / PennantPxPerDp, dy = PennantH - (py + 0.5f) / PennantPxPerDp;
                    float d = Polygon(dx, dy, tri);
                    var p = new Px();
                    p.Over(Ink, Cover(Polygon(dx, dy - 2f, tri) - 2.4f, aa));                // sombra dura de 2 dp
                    p.Over(Ink, Cover(d - 2.4f * 0.5f, aa));
                    p.Over(ClayFill(Gold, Mathf.Clamp01((dy - 3f) / 15f)), Cover(d + 0.6f, aa));
                    px[py * w + x] = p.ToColor32();
                }
            return px;
        }

        // ------------------------------------------------------------------ el mundo

        /// <summary>La Tierra: 116 dp de diámetro (radio 58) dentro de un cuadro de 128 dp, a 4 píxeles por dp.</summary>
        public const float EarthSide = 128f, EarthRadius = 58f, EarthPxPerDp = 4f;

        public static Sprite Earth() => Get(3, () => ToSprite(RenderEarth((int)(EarthSide * EarthPxPerDp)), (int)(EarthSide * EarthPxPerDp), EarthPxPerDp));

        /// <summary>La Tierra redibujada (versión 3 del boceto): océano en degradado, tierras suaves lima, nubes en trazos, la noche como degradado, borde de atmósfera y borde de tinta.</summary>
        public static Color32[] RenderEarth(int size)
        {
            var px = new Color32[size * size];
            float r = EarthRadius, aa = 1.4f / EarthPxPerDp;
            for (int py = 0; py < size; py++)
                for (int x = 0; x < size; x++)
                {
                    // dp desde el centro; yd hacia ABAJO como en el boceto
                    float cx = ((x + 0.5f) / size - 0.5f) * EarthSide, yd = -(((py + 0.5f) / size - 0.5f) * EarthSide);
                    float dist = Mathf.Sqrt(cx * cx + yd * yd);
                    var p = new Px();
                    float inside = Cover(dist - r, aa);
                    if (inside > 0f)
                    {
                        // océano: degradado desde un punto arriba a la izquierda
                        float sx = cx + r * 0.4f, sy = yd + r * 0.4f;
                        float t = Mathf.Clamp01(Mathf.Sqrt(sx * sx + sy * sy) / (r * 1.45f));
                        Color sea = t < 0.55f ? Color.Lerp(Hex(0x9EDCFF), Hex(0x3F8AD8), t / 0.55f) : Color.Lerp(Hex(0x3F8AD8), Hex(0x22508F), (t - 0.55f) / 0.45f);
                        p.Over(sea, inside);
                        // tierras suaves
                        float land = Mathf.Min(Mathf.Min(Ellipse(cx, yd, -0.40f * r, -0.30f * r, 0.30f * r, 0.20f * r), Ellipse(cx, yd, -0.24f * r, 0.02f * r, 0.15f * r, 0.20f * r)),
                            Mathf.Min(Ellipse(cx, yd, -0.52f * r, 0.14f * r, 0.17f * r, 0.20f * r), Ellipse(cx, yd, 0.36f * r, 0.30f * r, 0.24f * r, 0.22f * r)));
                        p.Over(new Color(150f / 255f, 214f / 255f, 140f / 255f, 0.9f), Cover(land, 0.05f * r * 0.6f) * inside);
                        // nubes en trazos
                        float cloud = 1e5f;
                        foreach (var c in Clouds) cloud = Mathf.Min(cloud, Capsule(cx, yd, c[0] * r, c[1] * r, c[2] * r, c[3] * r, c[4] * 0.5f));
                        p.Over(new Color(1f, 1f, 1f, 0.7f), Cover(cloud, aa) * inside);
                        // la noche (suave) y el borde de atmósfera
                        float night = Mathf.Clamp01(((cx + 0.1f * r) + (yd + 0.1f * r)) / (2.2f * r));
                        p.Over(new Color(6f / 255f, 8f / 255f, 34f / 255f, 0.6f * night), inside);
                        float atm = Mathf.Clamp01((dist - 0.8f * r) / (0.2f * r));
                        p.Over(new Color(170f / 255f, 225f / 255f, 1f, 0.5f * atm), inside);
                    }
                    p.Over(Ink, Cover(Mathf.Abs(dist - r) - 1.75f, aa));
                    px[py * size + x] = p.ToColor32();
                }
            return px;
        }

        private static readonly float[][] Clouds =
        {
            new[] { -0.7f, 0.45f, -0.1f, 0.38f, 4.5f }, new[] { 0.05f, -0.62f, 0.55f, -0.5f, 4f }, new[] { 0.3f, -0.05f, 0.75f, 0.02f, 3.5f }, new[] { -0.35f, -0.78f, -0.05f, -0.82f, 3f },
        };

        /// <summary>
        /// La luna: el suelo. Un círculo enorme (centro a 1240 dp, radio 652 en el boceto) de lavanda a violeta con borde de tinta de 4 dp y cráteres tenues, SOLO lejos de la regla (más de 86 dp debajo de ella). El sprite cubre 400 × 300 dp (de x −20 a 380) y su borde de arriba coincide con el punto más alto del arco:
        /// el controlador lo pone 16 dp sobre la regla.
        /// </summary>
        public const float MoonW = 400f, MoonH = 300f, MoonPxPerDp = 2f, MoonCenterY = 652f, MoonRadius = 652f;

        public static Sprite Moon() => Get(4, () => ToSpriteRect(RenderMoon(), (int)(MoonW * MoonPxPerDp), (int)(MoonH * MoonPxPerDp)));

        public static Color32[] RenderMoon()
        {
            int w = (int)(MoonW * MoonPxPerDp), h = (int)(MoonH * MoonPxPerDp);
            var px = new Color32[w * h];
            float aa = 1.4f / MoonPxPerDp;
            // los cráteres: (x, y hacia abajo desde el borde de arriba de la luna, radio); en el boceto y 720, 742, 768 y 700 con la luna arriba en 588
            float[][] craters = { new[] { 60f, 132f, 22f }, new[] { 250f, 154f, 30f }, new[] { 150f, 180f, 16f }, new[] { 320f, 112f, 12f } };
            for (int py = 0; py < h; py++)
            {
                float sy = h - 1 - py;
                float y = (sy + 0.5f) / MoonPxPerDp;                                       // dp desde el borde de arriba
                for (int x = 0; x < w; x++)
                {
                    float sx = (x + 0.5f) / MoonPxPerDp - 20f;                                // dp en el campo (x de 0 a 360)
                    float dist = Mathf.Sqrt((sx - 180f) * (sx - 180f) + (y - MoonCenterY) * (y - MoonCenterY));
                    var p = new Px();
                    float inside = Cover(dist - MoonRadius, aa);
                    if (inside > 0f)
                    {
                        float t = Mathf.Clamp01((y - 2f) / 190f);
                        p.Over(Color.Lerp(Hex(0x9A93D8), Hex(0x4C4596), t), inside);
                        float crater = 1e5f;
                        foreach (var c in craters) crater = Mathf.Min(crater, Ellipse(sx, y, c[0], c[1], c[2], c[2] * 0.4f));
                        p.Over(new Color(58f / 255f, 52f / 255f, 134f / 255f, 0.35f), Cover(crater, aa) * inside);
                    }
                    p.Over(Ink, Cover(Mathf.Abs(dist - MoonRadius) - 2f, aa));
                    px[py * w + x] = p.ToColor32();
                }
            }
            return px;
        }

        /// <summary>Una cordillera lunar: el sprite tiene el ancho de DOS campos (720 dp: el dibujo se repite) y 60 dp de alto, con la base abajo; <paramref name="far"/> es la de atrás (más alta y más oscura).</summary>
        public const float RangeW = 720f, RangeH = 60f, RangePxPerDp = 2f;

        public static Sprite Range(bool far) => Get(far ? 5 : 6, () => ToSpriteRect(RenderRange(far), (int)(RangeW * RangePxPerDp), (int)(RangeH * RangePxPerDp)));

        public static Color32[] RenderRange(bool far)
        {
            int n = far ? 9 : 7;
            float lo = far ? 18f : 10f, hi = far ? 52f : 34f;
            var rng = new SoundKit.Rng(far ? 5u : 9u);
            var hs = new float[n + 1];
            for (int i = 0; i <= n; i++) hs[i] = lo + (float)rng.Next() * (hi - lo);
            Color fill = far ? Hex(0x2C2766) : Hex(0x3D3784);
            int w = (int)(RangeW * RangePxPerDp), h = (int)(RangeH * RangePxPerDp);
            var px = new Color32[w * h];
            float step = 360f / n, aa = 1.4f / RangePxPerDp;
            for (int py = 0; py < h; py++)
            {
                float y = (py + 0.5f) / RangePxPerDp;                                       // dp sobre la base
                for (int x = 0; x < w; x++)
                {
                    float xd = (x + 0.5f) / RangePxPerDp;
                    float xm = xd % 360f;
                    int i = Mathf.Min(n - 1, (int)(xm / step));
                    float f = (xm - i * step) / step;
                    float top = Mathf.Lerp(hs[i], hs[i + 1], f);
                    float slope = (hs[i + 1] - hs[i]) / step;
                    float d = (y - top) / Mathf.Sqrt(1f + slope * slope);
                    var p = new Px();
                    p.Over(fill, Cover(d, aa));
                    p.Over(new Color(26f / 255f, 18f / 255f, 64f / 255f, 0.6f), Cover(Mathf.Abs(d) - 1.25f, aa));
                    px[py * w + x] = p.ToColor32();
                }
            }
            return px;
        }

        /// <summary>La franja de nebulosa en diagonal (muy suave): un degradado de 90 × 150 px que se estira sobre 360 × 600 dp.</summary>
        public static Sprite Nebula() => Get(7, () => ToSpriteRect(RenderNebula(), 90, 150));

        public static Color32[] RenderNebula()
        {
            const int w = 90, h = 150;
            var px = new Color32[w * h];
            for (int py = 0; py < h; py++)
                for (int x = 0; x < w; x++)
                {
                    float dx = (x + 0.5f) / w * 360f, dy = (h - 1 - py + 0.5f) / h * 600f;
                    // el degradado va de (0, 120) a (360, 480): t es la proyección del punto sobre esa recta
                    float vx = 360f, vy = 360f;
                    float t = ((dx) * vx + (dy - 120f) * vy) / (vx * vx + vy * vy);
                    Color c;
                    if (t <= 0f) c = new Color(183f / 255f, 100f / 255f, 1f, 0f);
                    else if (t < 0.45f) c = new Color(183f / 255f, 100f / 255f, 1f, 0.13f * (t / 0.45f));
                    else if (t < 0.55f) c = Color.Lerp(new Color(183f / 255f, 100f / 255f, 1f, 0.13f), new Color(127f / 255f, 216f / 255f, 1f, 0.10f), (t - 0.45f) / 0.1f);
                    else if (t < 1f) c = new Color(127f / 255f, 216f / 255f, 1f, 0.10f * (1f - (t - 0.55f) / 0.45f));
                    else c = new Color(127f / 255f, 216f / 255f, 1f, 0f);
                    px[py * w + x] = (Color32)c;
                }
            return px;
        }

        /// <summary>El satélite que cruza el cielo: dos paneles azules y un cuerpo crema (32 × 12 dp).</summary>
        public const float SatelliteW = 32f, SatelliteH = 12f, SatellitePxPerDp = 6f;

        public static Sprite Satellite() => Get(8, () => ToSpriteRect(RenderSatellite(), (int)(SatelliteW * SatellitePxPerDp), (int)(SatelliteH * SatellitePxPerDp)));

        public static Color32[] RenderSatellite()
        {
            int w = (int)(SatelliteW * SatellitePxPerDp), h = (int)(SatelliteH * SatellitePxPerDp);
            var px = new Color32[w * h];
            float aa = 1.2f / SatellitePxPerDp;
            for (int py = 0; py < h; py++)
                for (int x = 0; x < w; x++)
                {
                    float dx = (x + 0.5f) / SatellitePxPerDp - SatelliteW * 0.5f, dy = (py + 0.5f) / SatellitePxPerDp - SatelliteH * 0.5f;
                    var p = new Px();
                    float panels = Mathf.Min(RoundBox(dx, dy, -9.5f, 0f, 4.5f, 3f, 0.4f), RoundBox(dx, dy, 9.5f, 0f, 4.5f, 3f, 0.4f));
                    p.Over(Hex(0x3E5BA8), Cover(panels, aa));
                    p.Over(CabinColor, Cover(RoundBox(dx, dy, 0f, 0f, 4f, 4f, 0.5f), aa));
                    px[py * w + x] = p.ToColor32();
                }
            return px;
        }

        /// <summary>La estela de una estrella fugaz: una línea blanca de 53,5 dp que se apaga hacia atrás (128 × 8 px). El controlador la gira 20,8° y la mueve.</summary>
        public static Sprite ShootingStar() => Get(9, () => ToSpriteRect(RenderShootingStar(), 128, 8));

        public static Color32[] RenderShootingStar()
        {
            const int w = 128, h = 8;
            var px = new Color32[w * h];
            for (int py = 0; py < h; py++)
                for (int x = 0; x < w; x++)
                {
                    float t = (x + 0.5f) / w;
                    float dy = Mathf.Abs((py + 0.5f) / h - 0.5f) * 2f;                       // 0 al centro, 1 en el borde
                    float a = t * Mathf.Clamp01((1f - dy) * 1.6f);
                    px[py * w + x] = (Color32)new Color(1f, 1f, 1f, a);
                }
            return px;
        }

        // ------------------------------------------------------------------ el marcador y el resultado

        /// <summary>La cúpula de tu base (marcador): media esfera sobre su base, celeste cuando ya hay alguna y apagada si no. 28 × 20 dp.</summary>
        public const float DomeW = 28f, DomeH = 20f, DomePxPerDp = 6f;

        public static Sprite Dome(bool on) => Get(on ? 10 : 11, () => ToSpriteRect(RenderDome(on), (int)(DomeW * DomePxPerDp), (int)(DomeH * DomePxPerDp)));

        public static Color32[] RenderDome(bool on)
        {
            int w = (int)(DomeW * DomePxPerDp), h = (int)(DomeH * DomePxPerDp);
            var px = new Color32[w * h];
            float aa = 1.4f / DomePxPerDp;
            Color fill = on ? Cyan : DomeOff;
            for (int py = 0; py < h; py++)
                for (int x = 0; x < w; x++)
                {
                    float dx = (x + 0.5f) / DomePxPerDp - DomeW * 0.5f, dy = DomeH * 0.5f - (py + 0.5f) / DomePxPerDp;     // dy hacia ABAJO desde el centro, como el boceto
                    // arco (centro (0, 4), radio 11) arriba y un tramo recto hasta y = 6
                    float dome = Mathf.Min(Mathf.Max(Circle(dx, dy, 0f, 4f, 11f), dy - 4f), RoundBox(dx, dy, 0f, 5f, 11f, 1f, 0.001f));
                    var p = new Px();
                    p.Over(Ink, Cover(dome - 2.4f * 0.5f - 1.2f, aa));
                    p.Over(ClayFill(fill, Mathf.Clamp01((dy + 8f) / 14f)), Cover(dome + 0.01f, aa));
                    px[py * w + x] = p.ToColor32();
                }
            return px;
        }

        /// <summary>La insignia del tramo: un círculo de 24 dp con ✓ (lima) o aspa (coral). Nunca solo color.</summary>
        public const float BadgeSide = 28f, BadgePxPerDp = 6f;

        public static Sprite Badge(bool ok) => Get(ok ? 12 : 13, () => ToSprite(RenderBadge(ok, (int)(BadgeSide * BadgePxPerDp)), (int)(BadgeSide * BadgePxPerDp), BadgePxPerDp));

        public static Color32[] RenderBadge(bool ok, int size)
        {
            var px = new Color32[size * size];
            float aa = 1.4f / BadgePxPerDp;
            for (int py = 0; py < size; py++)
                for (int x = 0; x < size; x++)
                {
                    float dx = ((x + 0.5f) / size - 0.5f) * BadgeSide, dy = ((py + 0.5f) / size - 0.5f) * BadgeSide;      // y hacia ARRIBA
                    var p = new Px();
                    float d = Circle(dx, dy, 0f, 0f, 12f);
                    p.Over(Ink, Cover(d - 1.25f, aa));
                    p.Over(ok ? LimeColor : CoralColor, Cover(d, aa));
                    float glyph = ok
                        ? Mathf.Min(Capsule(dx, dy, -5f, 0f, -1f, -4f, 1.5f), Capsule(dx, dy, -1f, -4f, 5f, 4f, 1.5f))
                        : Mathf.Min(Capsule(dx, dy, -4f, 4f, 4f, -4f, 1.5f), Capsule(dx, dy, 4f, 4f, -4f, -4f, 1.5f));
                    p.Over(Ink, Cover(glyph, aa));
                    px[py * size + x] = p.ToColor32();
                }
            return px;
        }

        /// <summary>La flecha de la guía, sobre la regla: un triángulo lima con borde de tinta (16 × 12 dp).</summary>
        public const float ArrowW = 16f, ArrowH = 12f, ArrowPxPerDp = 8f;

        public static Sprite Arrow() => Get(14, () => ToSpriteRect(RenderArrow(), (int)(ArrowW * ArrowPxPerDp), (int)(ArrowH * ArrowPxPerDp)));

        public static Color32[] RenderArrow()
        {
            int w = (int)(ArrowW * ArrowPxPerDp), h = (int)(ArrowH * ArrowPxPerDp);
            var px = new Color32[w * h];
            float aa = 1.4f / ArrowPxPerDp;
            float[] tri = { 1.5f, 10.5f, 14.5f, 10.5f, 8f, 1.5f };                          // x, y (hacia arriba): la punta abajo
            for (int py = 0; py < h; py++)
                for (int x = 0; x < w; x++)
                {
                    float dx = (x + 0.5f) / ArrowPxPerDp, dy = (py + 0.5f) / ArrowPxPerDp;
                    float d = Polygon(dx, dy, tri);
                    var p = new Px();
                    p.Over(Ink, Cover(d - 1f, aa));
                    p.Over(LimeColor, Cover(d, aa));
                    px[py * w + x] = p.ToColor32();
                }
            return px;
        }

        // ------------------------------------------------------------------ piezas sueltas y caché

        /// <summary>Un sprite de una textura rectangular (1 unidad = 1 píxel / <paramref name="pxPerDp"/> si se pasa; si no, el ancho).</summary>
        public static Sprite ToSpriteRect(Color32[] pixels, int w, int h)
        {
            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Bilinear };
            tex.SetPixels32(pixels);
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f), w);
        }

        private static Sprite Get(int key, System.Func<Sprite> build)
        {
            if (!Cache.TryGetValue(key, out var s) || s == null)
            {
                s = build();
                Cache[key] = s;
            }
            return s;
        }

        /// <summary>Hornea todo lo fijo de a poco (durante la cuenta regresiva): una pieza por cuadro.</summary>
        public static System.Collections.IEnumerator Prewarm()
        {
            Lander(); yield return null;
            Earth(); yield return null;
            Moon(); yield return null;
            Range(true); Range(false); yield return null;
            Pennant(); Nebula(); Satellite(); ShootingStar(); yield return null;
            Dome(true); Dome(false); Badge(true); Badge(false); Arrow();
        }
    }
}
