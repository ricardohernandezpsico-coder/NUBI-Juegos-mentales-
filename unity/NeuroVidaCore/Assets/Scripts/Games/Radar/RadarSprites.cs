using System.Collections.Generic;
using UnityEngine;
using NeuroVida.Games.Shared;
using static NeuroVida.Games.Shared.ClayRaster;

namespace NeuroVida.Games.Radar
{
    /// <summary>
    /// Arte procedural de «Rescate relámpago: qué cápsulas viste» (sin assets), con el pincel de arcilla común (<see cref="ClayRaster"/>) y las medidas y colores del boceto aprobado (docs/previews/rescate-boceto.html):
    /// <list type="bullet">
    /// <item><see cref="CapsuleSprite"/> / <see cref="Emblem"/>: las seis cápsulas (forma + color fijos; la cápsula ES el símbolo, de unos 55 dp; el astronauta va en una ventanita, solo de adorno) y la misma forma sola, para el tablero.</item>
    /// <item><see cref="Rock"/>: la roca gris, irregular, sin forma de cápsula ni emblema.</item>
    /// <item><see cref="Scope"/>: el radar, un instrumento de arcilla (bisel con 48 marcas, pantalla azul noche, dos aros punteados y una cruz, solo de adorno); <see cref="Sweep"/>: el haz con estela; <see cref="Mask"/>: la estática.</item>
    /// <item><see cref="Ship"/>, <see cref="Window"/>: la nave de rescate (casco claro, cabina celeste, dos propulsores coral) y sus diez ventanas.</item>
    /// <item><see cref="Button"/>, <see cref="GoButton"/>, <see cref="ButtonRing"/>: los botones de arcilla del tablero y «¡Rescatar!».</item>
    /// </list>
    /// El color y el brillo de cápsulas, rocas y fondo son IGUALES en todos los niveles (regla de patentes 5: la dificultad no ajusta el contraste figura-fondo). Las funciones <c>Render*</c> devuelven los píxeles (fila 0 = abajo) para poder previsualizarlos fuera de Unity.
    /// </summary>
    public static class RadarSprites
    {
        /// <summary>El color FIJO de cada tipo de cápsula.</summary>
        public static readonly Color[] Colors =
        {
            Hex(0xA6E36B),      // hexágono
            Hex(0x7FD8FF),      // gota
            Hex(0xFF8A6B),      // círculo
            Hex(0xFFC94A),      // cuadrado
            Hex(0xB79BFF),      // triángulo
            Hex(0xFFB3D1),      // rombo
        };

        public static readonly Color RockColor = Hex(0x76718F), RockSpot = Hex(0x5C5875), WindowWhite = Hex(0xF4F2FF), Visor = Hex(0x284A8E), Bezel = Hex(0x3A3486), Glass = Hex(0x070E2C), GlassCenter = Hex(0x13265E);

        private const float Aa = 0.03f;
        private static readonly Dictionary<int, Sprite> Cache = new Dictionary<int, Sprite>();

        // ------------------------------------------------------------------ cápsulas

        private const int CapsulePx = 128;
        private const float CapsuleZoom = 1.18f;
        /// <summary>De la cápsula (dp de diámetro de su forma) al lado del sprite: la forma ocupa 0,92 de los 1,18 de «zoom», así que lado = diámetro × 1,18 / 0,92.</summary>
        public const float CapsuleSideInSizes = CapsuleZoom / 0.92f;

        private static readonly float[] HexPts = BuildPolygon(6, 0.9f, Mathf.PI / 6f);
        private static readonly float[] TriPts = { 0f, 0.98f, 0.98f, -0.74f, -0.98f, -0.74f };
        private static readonly float[] DiamondPts = { 0f, 1.0f, 0.84f, 0f, 0f, -1.0f, -0.84f, 0f };

        private static float[] BuildPolygon(int n, float r, float start)
        {
            var p = new float[n * 2];
            for (int i = 0; i < n; i++)
            {
                float a = start + i * 2f * Mathf.PI / n;
                p[i * 2] = Mathf.Cos(a) * r;
                p[i * 2 + 1] = Mathf.Sin(a) * r;
            }
            return p;
        }

        /// <summary>La silueta de cada tipo (y arriba, como en Unity).</summary>
        public static float ShapeSdf(CapsuleType t, float x, float y)
        {
            switch (t)
            {
                case CapsuleType.Hexagon: return Polygon(x, y, HexPts) - 0.05f;
                case CapsuleType.Drop: return UnevenCapsule(x, y + 0.34f, 0.66f, 0.07f, 1.3f);
                case CapsuleType.Circle: return Circle(x, y, 0f, 0f, 0.92f);
                case CapsuleType.Square: return RoundBox(x, y, 0f, 0f, 0.74f, 0.74f, 0.22f);
                case CapsuleType.Triangle: return Polygon(x, y, TriPts) - 0.08f;
                default: return Polygon(x, y, DiamondPts) - 0.06f;
            }
        }

        /// <summary>Dónde va la ventanita del astronauta dentro de cada forma (y arriba).</summary>
        private static float WindowY(CapsuleType t) => t == CapsuleType.Drop ? -0.34f : t == CapsuleType.Triangle ? -0.28f : 0f;

        /// <summary>Relleno de arcilla: luz arriba (+32 %) y sombra abajo (−12 %), con el brillo elíptico arriba a la izquierda.</summary>
        private static Color Gradient(Color c, float y, float half) => Color.Lerp(Shade(Tint(c, 0.32f), 1f), Shade(c, 0.88f), Mathf.Clamp01((half - y) / (2f * half)));

        private static void Fill(ref Px p, Color c, float sdf, float x, float y, float half, bool highlight)
        {
            p.Over(Gradient(c, y, half), Cover(sdf + 0.02f, Aa));
            if (highlight) p.Over(new Color(1f, 1f, 1f, 0.38f), Cover(Ellipse(x, y, -0.3f, 0.42f, 0.2f, 0.09f), Aa) * Cover(sdf + 0.08f, Aa));
        }

        public static Sprite CapsuleSprite(CapsuleType t) => Get(100 + (int)t, () => ToSprite(RenderCapsule(t, CapsulePx), CapsulePx, CapsulePx));

        public static Sprite Emblem(CapsuleType t) => Get(110 + (int)t, () => ToSprite(RenderEmblem(t, CapsulePx), CapsulePx, CapsulePx));

        public static Color32[] RenderCapsule(CapsuleType t, int size)
        {
            Color c = Colors[(int)t];
            float wy = WindowY(t);
            return RenderClay(size, CapsuleZoom, 0.075f, 0.06f, Aa, (x, y) => ShapeSdf(t, x, y), (ref Px p, float x, float y) =>
            {
                float sdf = ShapeSdf(t, x, y);
                Fill(ref p, c, sdf, x, y, 0.9f, true);
                // ventanita con el astronauta: adorno, la identidad es forma + color
                p.Over(Ink, Cover(Circle(x, y, 0f, wy, 0.36f), Aa));
                p.Over(WindowWhite, Cover(Circle(x, y, 0f, wy, 0.3f), Aa));
                p.Over(Visor, Cover(Ellipse(x, y, 0f, wy - 0.02f, 0.22f, 0.18f), Aa));
                p.Over(new Color(1f, 1f, 1f, 0.7f), Cover(Ellipse(x, y, -0.08f, wy + 0.05f, 0.07f, 0.04f), Aa));
            });
        }

        public static Color32[] RenderEmblem(CapsuleType t, int size)
        {
            Color c = Colors[(int)t];
            return RenderClay(size, CapsuleZoom, 0.1f, 0.08f, Aa, (x, y) => ShapeSdf(t, x, y), (ref Px p, float x, float y) => Fill(ref p, c, ShapeSdf(t, x, y), x, y, 0.9f, true));
        }

        // ------------------------------------------------------------------ roca

        private const int RockPx = 128;
        private const float RockZoom = 1.18f;
        /// <summary>El radio de la roca (dp) como fracción del lado del sprite: lado = 23 dp × 2 × 1,18 / 0,98.</summary>
        public const float RockSideInRadii = 2f * RockZoom / 0.98f;

        private static readonly float[] RockK = { 1f, 0.8f, 0.95f, 0.76f, 1f, 0.84f, 0.93f, 0.79f };

        private static float[] RockPts()
        {
            var p = new float[16];
            for (int i = 0; i < 8; i++)
            {
                float a = i * Mathf.PI / 4f + 0.2f;
                p[i * 2] = Mathf.Cos(a) * 0.98f * RockK[i];
                p[i * 2 + 1] = -Mathf.Sin(a) * 0.98f * RockK[i];       // el boceto dibuja con y hacia abajo
            }
            return p;
        }

        private static readonly float[] RockPoly = RockPts();

        public static Sprite Rock() => Get(120, () => ToSprite(RenderRock(RockPx), RockPx, RockPx));

        public static Color32[] RenderRock(int size) => RenderClay(size, RockZoom, 0.1f, 0.1f, Aa, (x, y) => Polygon(x, y, RockPoly) - 0.04f, (ref Px p, float x, float y) =>
        {
            float sdf = Polygon(x, y, RockPoly) - 0.04f;
            Fill(ref p, RockColor, sdf, x, y, 0.9f, false);
            foreach (var s in new[] { (-0.30f, 0.22f, 0.22f), (0.35f, -0.26f, 0.16f), (0.17f, 0.43f, 0.11f) })
                p.Over(RockSpot, Cover(Circle(x, y, s.Item1, s.Item2, s.Item3), Aa) * Cover(sdf + 0.1f, Aa));
        });

        // ------------------------------------------------------------------ el radar

        private const int ScopePx = 768;
        private const float ScopeZoom = 1.06f, BezelR = 0.95f;
        /// <summary>Radio del vidrio (donde va la imagen) en coordenadas del dibujo: 128 dp de vidrio dentro de 141 dp de bisel.</summary>
        public const float GlassR = BezelR * RadarContract.RadarRadius / (RadarContract.RadarRadius + 13f);
        /// <summary>Del radio del bisel (dp) al lado del sprite del radar y de la estática: lado = 2 × radio × 1,06 / 0,95.</summary>
        public const float ScopeSideInBezelRadii = 2f * ScopeZoom / BezelR;
        /// <summary>El vidrio como fracción de la mitad del sprite (para la estática y el haz).</summary>
        public const float GlassFraction = GlassR / ScopeZoom;

        public static Sprite Scope() => Get(130, () => ToSprite(RenderScope(ScopePx), ScopePx, ScopePx));
        public static Sprite Sweep() => Get(131, () => ToSprite(RenderSweep(256), 256, 256));
        public static Sprite Mask(int i) { i = Mathf.Abs(i) % 2; return Get(132 + i, () => ToSprite(RenderMask(512, 11 + i * 37), 512, 512)); }
        public static Sprite DashedRing() => Get(160, () => ToSprite(RenderDashedRing(128), 128, 128));
        public static Sprite Axis() => Get(134, () => ToSprite(RenderAxis(64), 64, 64));
        public static Sprite Planet() => Get(135, () => ToSprite(RenderPlanet(256), 256, 256));

        public static Color32[] RenderScope(int size) => RenderClay(size, ScopeZoom, 0.027f, 0.05f, 0.006f, (x, y) => Circle(x, y, 0f, 0f, BezelR), (ref Px p, float x, float y) =>
        {
            const float aa = 0.006f;
            float r = Mathf.Sqrt(x * x + y * y);
            // el bisel de arcilla, con luz arriba
            p.Over(Gradient(Bezel, y, BezelR), Cover(r - BezelR, aa));
            p.Over(new Color(1f, 1f, 1f, 0.2f), Cover(Ellipse(x, y, -0.5f, 0.72f, 0.16f, 0.045f), aa) * Cover(-(r - GlassR - 0.03f), aa));
            // las 48 marcas del bisel (una larga cada cuatro)
            float k = BezelR / (RadarContract.RadarRadius + 13f);
            for (int i = 0; i < 48; i++)
            {
                float a = i * Mathf.PI / 24f;
                bool longTick = i % 4 == 0;
                float r1 = BezelR - 3f * k, r0 = r1 - (longTick ? 8f : 4f) * k;
                float tick = Capsule(x, y, Mathf.Cos(a) * r0, Mathf.Sin(a) * r0, Mathf.Cos(a) * r1, Mathf.Sin(a) * r1, (longTick ? 1.2f : 0.7f) * k);
                p.Over(longTick ? Hex(0xB8B0FF) : new Color(184f / 255f, 176f / 255f, 1f, 0.5f), Cover(tick, aa));
            }
            // la pantalla: borde de tinta y vidrio azul noche con luz arriba
            p.Over(Ink, Cover(r - GlassR - 3.5f * k, aa));
            float gy = y - GlassR * 0.23f;
            p.Over(Color.Lerp(GlassCenter, Glass, Mathf.Clamp01(Mathf.Sqrt(x * x + gy * gy) / GlassR)), Cover(r - GlassR, aa));
            if (r > GlassR) return;
            // adorno: dos aros punteados y una cruz (NUNCA lugares de respuesta)
            var line = new Color(127f / 255f, 216f / 255f, 1f, 0.16f);
            float ang = Mathf.Atan2(y, x);
            foreach (float rr in new[] { 0.36f, 0.7f })
            {
                float dash = Mathf.Sin(ang * rr * 60f) > -0.1f ? 1f : 0f;
                p.Over(line, Cover(Mathf.Abs(r - rr * GlassR) - 1.0f * k, aa) * dash);
            }
            p.Over(line, Cover(Mathf.Abs(y) - 0.9f * k, aa));
            p.Over(line, Cover(Mathf.Abs(x) - 0.9f * k, aa));
        });

        /// <summary>Haz del radar: cuña con el borde delantero brillante (apunta hacia arriba) y una estela que se apaga hacia atrás (sentido antihorario: el haz gira en sentido horario).</summary>
        public static Color32[] RenderSweep(int size)
        {
            var pixels = new Color32[size * size];
            const float trail = Mathf.PI * 0.42f;
            for (int py = 0; py < size; py++)
            {
                for (int px = 0; px < size; px++)
                {
                    float x = (px + 0.5f) / size * 2f - 1f;
                    float y = (py + 0.5f) / size * 2f - 1f;
                    float r = Mathf.Sqrt(x * x + y * y);
                    float a = 0f;
                    if (r <= 1f)
                    {
                        float behind = Mathf.Atan2(-x, y);
                        if (behind < 0f) behind += Mathf.PI * 2f;
                        float fade = behind <= trail ? 1f - behind / trail : 0f;
                        float edge = Mathf.Clamp01(1f - Mathf.Abs(x) * size * 0.25f) * (y > 0f ? 1f : 0f);
                        float radial = Mathf.Clamp01((1f - r) * 12f) * Mathf.Clamp01(r * 6f);
                        a = Mathf.Max(fade * fade * 0.3f, edge * 0.95f) * radial;
                    }
                    pixels[py * size + px] = new Color32(255, 255, 255, (byte)(Mathf.Clamp01(a) * 255f));
                }
            }
            return pixels;
        }

        /// <summary>La estática: vidrio opaco cubierto de barras de color al azar (celeste, uva, blanco) y líneas de barrido, solo dentro del disco. Semilla fija.</summary>
        public static Color32[] RenderMask(int size, int seed)
        {
            var rng = new System.Random(seed);
            var acc = new Px[size * size];
            float glass = GlassFraction;
            for (int i = 0; i < acc.Length; i++)
            {
                float x = (i % size + 0.5f) / size * 2f - 1f, y = (i / size + 0.5f) / size * 2f - 1f;
                float r = Mathf.Sqrt(x * x + y * y);
                acc[i].Over(new Color(20f / 255f, 30f / 255f, 80f / 255f, 1f), Mathf.Clamp01((glass - r) * size * 0.5f));
            }
            for (int k = 0; k < 340; k++)
            {
                float pick = (float)rng.NextDouble();
                Color c = pick < 0.33f ? new Color(127f / 255f, 216f / 255f, 1f) : pick < 0.66f ? new Color(183f / 255f, 155f / 255f, 1f) : new Color(237f / 255f, 234f / 255f, 251f / 255f);
                c.a = 0.35f + 0.5f * (float)rng.NextDouble();
                float cx = (float)rng.NextDouble() * 2f - 1f, cy = (float)rng.NextDouble() * 2f - 1f;
                float w = (3f + 16f * (float)rng.NextDouble()) / 128f * 0.5f / 0.8f, h = (2f + 5f * (float)rng.NextDouble()) / 128f * 0.5f / 0.8f;
                int x0 = Mathf.Max(0, (int)((cx - w + 1f) * 0.5f * size) - 1), x1 = Mathf.Min(size - 1, (int)((cx + w + 1f) * 0.5f * size) + 1);
                int y0 = Mathf.Max(0, (int)((cy - h + 1f) * 0.5f * size) - 1), y1 = Mathf.Min(size - 1, (int)((cy + h + 1f) * 0.5f * size) + 1);
                for (int py = y0; py <= y1; py++)
                    for (int px = x0; px <= x1; px++)
                    {
                        float x = (px + 0.5f) / size * 2f - 1f, y = (py + 0.5f) / size * 2f - 1f;
                        if (Mathf.Sqrt(x * x + y * y) > glass) continue;
                        acc[py * size + px].Over(c, Cover(RoundBox(x, y, cx, cy, w, h, 0f), 2f / size));
                    }
            }
            var pixels = new Color32[size * size];
            for (int py = 0; py < size; py++)
                for (int px = 0; px < size; px++)
                {
                    var p = acc[py * size + px];
                    if (py % 7 < 2 && p.a > 0.5f) p.Over(new Color(0f, 0f, 0f, 0.18f), 1f);       // líneas de barrido (solo dentro del disco)
                    pixels[py * size + px] = p.ToColor32();
                }
            return pixels;
        }

        /// <summary>Un aro punteado blanco (se tiñe con <c>Image.color</c>): lo que marca «estaba y no la elegiste» en la revelación (de 80 dp de diámetro).</summary>
        public static Color32[] RenderDashedRing(int size)
        {
            var pixels = new Color32[size * size];
            for (int py = 0; py < size; py++)
                for (int px = 0; px < size; px++)
                {
                    float x = (px + 0.5f) / size * 2f - 1f, y = (py + 0.5f) / size * 2f - 1f;
                    float r = Mathf.Sqrt(x * x + y * y);
                    float a = Cover(Mathf.Abs(r - 0.82f) - 0.05f, 2.5f / size) * (Mathf.Sin(Mathf.Atan2(y, x) * 10f) > -0.15f ? 1f : 0f);
                    pixels[py * size + px] = new Color32(255, 255, 255, (byte)(Mathf.Clamp01(a) * 255f));
                }
            return pixels;
        }

        /// <summary>El eje lima del centro del radar, con borde de tinta.</summary>
        public static Color32[] RenderAxis(int size) => RenderClay(size, 1.2f, 0.18f, 0f, Aa, (x, y) => Circle(x, y, 0f, 0f, 0.8f), (ref Px p, float x, float y) => p.Over(Hex(0xA6E36B), Cover(Circle(x, y, 0f, 0f, 0.8f), Aa)));

        /// <summary>El planeta lejano de arriba a la derecha (se dibuja a media luz): esfera violeta con un anillo que pasa por delante de la mitad de arriba y por detrás de la de abajo.</summary>
        public static Color32[] RenderPlanet(int size)
        {
            var pixels = new Color32[size * size];
            for (int py = 0; py < size; py++)
                for (int px = 0; px < size; px++)
                {
                    float x = ((px + 0.5f) / size * 2f - 1f) * 1.6f, y = ((py + 0.5f) / size * 2f - 1f) * 1.6f;
                    var p = new Px();
                    float r = Mathf.Sqrt(x * x + y * y);
                    float ring = (Mathf.Sqrt((x / 1.5f) * (x / 1.5f) + (y / 0.26f) * (y / 0.26f)) - 1f) * 0.26f;
                    var ringColor = new Color(183f / 255f, 155f / 255f, 1f, 0.55f);
                    if (y < 0f) p.Over(ringColor, Cover(Mathf.Abs(ring) - 0.025f, 0.02f));
                    float lx = x + 0.34f, ly = y - 0.37f;
                    p.Over(Color.Lerp(Hex(0x5D4FB3), Hex(0x231B5C), Mathf.Clamp01(Mathf.Sqrt(lx * lx + ly * ly) / 1.15f)), Cover(r - 1f, 0.02f));
                    if (y >= 0f) p.Over(ringColor, Cover(Mathf.Abs(ring) - 0.025f, 0.02f));
                    pixels[py * size + px] = p.ToColor32();
                }
            return pixels;
        }

        // ------------------------------------------------------------------ la nave de rescate

        private const int ShipPx = 256;
        private const float ShipZoom = 1.18f, ShipUnit = 80f;
        /// <summary>El lado del sprite de la nave (dp): 1 unidad = 80 dp.</summary>
        public const float ShipSide = 2f * ShipZoom * ShipUnit;

        private static float ShipBody(float x, float y)
        {
            float hull = RoundBox(x, y, 0f, 0f, 1f, 0.2f, 0.2f);
            float cabin = Intersect(Circle(x, y, 0f, 0.2f, 0.19f), -(y - 0.2f));
            float th = Union(RoundBox(x, y, -0.925f, -0.02f, 0.125f, 0.113f, 0.088f), RoundBox(x, y, 0.925f, -0.02f, 0.125f, 0.113f, 0.088f));
            return Union(Union(hull, cabin), th);
        }

        public static Sprite Ship() => Get(140, () => ToSprite(RenderShip(ShipPx), ShipPx, ShipPx));
        public static Sprite Window() => Get(141, () => ToSprite(RenderWindow(64), 64, 64));

        public static Color32[] RenderShip(int size) => RenderClay(size, ShipZoom, 0.04f, 0.06f, 0.012f, ShipBody, (ref Px p, float x, float y) =>
        {
            const float aa = 0.012f;
            // propulsores (detrás del casco)
            foreach (float sx in new[] { -0.925f, 0.925f })
            {
                float th = RoundBox(x, y, sx, -0.02f, 0.125f, 0.113f, 0.088f);
                p.Over(Ink, Cover(th - 0.035f, aa));
                p.Over(Gradient(Hex(0xFF8A6B), y, 0.12f), Cover(th, aa));
            }
            float hull = RoundBox(x, y, 0f, 0f, 1f, 0.2f, 0.2f);
            float cabin = Intersect(Circle(x, y, 0f, 0.2f, 0.19f), -(y - 0.2f));
            p.Over(Ink, Cover(cabin - 0.035f, aa));
            p.Over(Gradient(Hex(0x7FD8FF), y, 0.4f), Cover(cabin, aa));
            p.Over(Ink, Cover(hull - 0.04f, aa));
            p.Over(Gradient(Hex(0xE9E6FF), y, 0.2f), Cover(hull, aa));
            p.Over(new Color(1f, 1f, 1f, 0.38f), Cover(Ellipse(x, y, -0.55f, 0.1f, 0.2f, 0.04f), aa) * Cover(hull + 0.03f, aa));
        });

        /// <summary>Una ventana de la nave: disco claro con borde de tinta (se tiñe con <c>Image.color</c> cuando una cápsula rescatada ocupa su lugar).</summary>
        public static Color32[] RenderWindow(int size) => RenderClay(size, 1.15f, 0.2f, 0f, 0.03f, (x, y) => Circle(x, y, 0f, 0f, 0.8f), (ref Px p, float x, float y) => p.Over(Color.white, Cover(Circle(x, y, 0f, 0f, 0.8f), 0.03f)));

        // ------------------------------------------------------------------ el tablero

        private const int ButtonPx = 256;
        private const float ButtonZoom = 1.12f;
        /// <summary>El botón del tablero: 104 × 62 dp en un sprite cuadrado de lado 116,5 dp (1 unidad = 52 dp).</summary>
        public const float ButtonUnit = 52f, ButtonSide = 2f * ButtonZoom * ButtonUnit;

        public static Sprite Button() => Get(150, () => ToSprite(RenderButton(ButtonPx), ButtonPx, ButtonPx));
        public static Sprite ButtonRing(bool dashed) => Get(151 + (dashed ? 1 : 0), () => ToSprite(RenderButtonRing(ButtonPx, dashed), ButtonPx, ButtonPx));
        public static Sprite GoButton() => Get(153, () => ToSprite(RenderGo(ButtonPx), ButtonPx, ButtonPx));

        private static float ButtonShape(float x, float y) => RoundBox(x, y, 0f, 0f, 1f, 0.596f, 0.346f);

        /// <summary>El botón del tablero en blanco con luz arriba (se tiñe con <c>Image.color</c>: el borde de tinta y la sombra dura quedan oscuros).</summary>
        public static Color32[] RenderButton(int size) => RenderClay(size, ButtonZoom, 0.06f, 0.1f, Aa, ButtonShape, (ref Px p, float x, float y) =>
            p.Over(Gradient(Color.white, y, 0.6f), Cover(ButtonShape(x, y) + 0.02f, Aa)));

        public static Color32[] RenderButtonRing(int size, bool dashed)
        {
            var pixels = new Color32[size * size];
            for (int py = 0; py < size; py++)
                for (int px = 0; px < size; px++)
                {
                    float x = ((px + 0.5f) / size * 2f - 1f) * ButtonZoom, y = ((py + 0.5f) / size * 2f - 1f) * ButtonZoom;
                    float sdf = RoundBox(x, y, 0f, 0f, 0.9f, 0.496f, 0.27f);
                    float a = Cover(Mathf.Abs(sdf) - 0.04f, Aa);
                    if (dashed) a *= Mathf.Sin(Mathf.Atan2(y / 0.6f, x) * 11f) > -0.15f ? 1f : 0f;
                    pixels[py * size + px] = new Color32(255, 255, 255, (byte)(Mathf.Clamp01(a) * 255f));
                }
            return pixels;
        }

        private static float GoShape(float x, float y) => RoundBox(x, y, 0f, 0f, 1f, 0.256f, 0.256f);

        /// <summary>«¡Rescatar!»: 172 × 44 dp en un sprite cuadrado de lado 185,8 dp (1 unidad = 86 dp).</summary>
        public const float GoUnit = 86f, GoSide = 2f * 1.08f * GoUnit;

        public static Color32[] RenderGo(int size) => RenderClay(size, 1.08f, 0.037f, 0.07f, Aa, GoShape, (ref Px p, float x, float y) =>
        {
            p.Over(Gradient(Color.white, y, 0.26f), Cover(GoShape(x, y) + 0.01f, Aa));
            p.Over(new Color(1f, 1f, 1f, 0.3f), Cover(Ellipse(x, y, -0.55f, 0.12f, 0.2f, 0.045f), Aa) * Cover(GoShape(x, y) + 0.04f, Aa));
        });

        // ------------------------------------------------------------------ caché

        private static Sprite Get(int key, System.Func<Sprite> build)
        {
            if (!Cache.TryGetValue(key, out var s) || s == null)
            {
                s = build();
                Cache[key] = s;
            }
            return s;
        }

        /// <summary>Hornea todo de a poco (durante la cuenta regresiva).</summary>
        public static System.Collections.IEnumerator Prewarm()
        {
            for (int t = 0; t < RadarContract.TypeCount; t++) { CapsuleSprite((CapsuleType)t); Emblem((CapsuleType)t); yield return null; }
            Rock(); Axis(); Window(); yield return null;
            Scope(); yield return null;
            Sweep(); Mask(0); Mask(1); yield return null;
            Ship(); Button(); ButtonRing(false); ButtonRing(true); GoButton(); Planet();
        }
    }
}
