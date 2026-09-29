using UnityEngine;
using NeuroVida.Games.Shared;
using static NeuroVida.Games.Shared.ClayRaster;

namespace NeuroVida.Games.Radar
{
    /// <summary>
    /// Arte procedural de Radar (sin assets), con el pincel de arcilla común (<see cref="ClayRaster"/>):
    /// <list type="bullet">
    /// <item><see cref="Scope"/>: la pantalla del radar. Aro de arcilla con borde tinta y sombra dura, vidrio azul
    /// noche, tres anillos, ocho radios y la pantalla central.</item>
    /// <item><see cref="Sweep"/>: el haz que barre (blanco con estela que se apaga; se tiñe con <c>Image.color</c>).</item>
    /// <item><see cref="Mask"/>: la "interferencia" que tapa el destello (máscara visual de la tarea UFOV): manchas y
    /// trazos de la paleta sobre el vidrio, opaca dentro del radar.</item>
    /// <item><see cref="Robot"/>: robot de rescate apagado, el distractor de Rescate relámpago: casco CUADRADO gris con
    /// visera recta y antena (el astronauta es redondo y crema: se distinguen por forma, no solo por color).</item>
    /// <item><see cref="Beacon"/>: la baliza que se pone al tocar un lugar (disco celeste con luz crema).</item>
    /// <item><see cref="Slot"/>: un lugar vacío donde se puede poner baliza (aro tenue).</item>
    /// </list>
    /// Las funciones <c>Render*</c> devuelven los píxeles (fila 0 = abajo) para poder previsualizarlos fuera de Unity.
    /// </summary>
    public static class RadarSprites
    {
        private const int ScopePx = 768;
        private const float ScopeZoom = 1.06f;
        /// <summary>Radio del vidrio (donde van los anillos) en coordenadas del dibujo.</summary>
        public const float GlassR = 0.86f;
        private const float BezelR = 0.95f;
        /// <summary>Radio del vidrio como fracción de la mitad del sprite: el controlador lo usa para ubicar las casillas.</summary>
        public const float GlassFraction = GlassR / ScopeZoom;

        public static readonly Color Glass = Hex(0x0C1648);
        public static readonly Color GlassCenter = Hex(0x17226A);
        public static readonly Color Bezel = Hex(0x2A3590);

        private static Sprite _scope, _sweep, _robot, _beacon, _slot;
        private static readonly Sprite[] _masks = new Sprite[2];

        public static Sprite Scope() => _scope != null ? _scope : _scope = ToSprite(RenderScope(ScopePx), ScopePx, ScopePx);
        public static Sprite Sweep() => _sweep != null ? _sweep : _sweep = ToSprite(RenderSweep(256), 256, 256);
        public static Sprite Robot() => _robot != null ? _robot : _robot = ToSprite(RenderRobot(192), 192, 192);
        public static Sprite Beacon() => _beacon != null ? _beacon : _beacon = ToSprite(RenderBeacon(160), 160, 160);
        public static Sprite Slot() => _slot != null ? _slot : _slot = ToSprite(RenderSlot(160), 160, 160);

        public static Sprite Mask(int i)
        {
            i = Mathf.Abs(i) % _masks.Length;
            return _masks[i] != null ? _masks[i] : _masks[i] = ToSprite(RenderMask(512, 11 + i * 37), 512, 512);
        }

        // ------------------------------------------------------------------ radar

        private static float ScopeBody(float x, float y) => Circle(x, y, 0f, 0f, BezelR);

        public static Color32[] RenderScope(int size) => RenderClay(size, ScopeZoom, 0.03f, 0.05f, 0.012f, ScopeBody, (ref Px p, float x, float y) =>
        {
            const float aa = 0.006f;
            float r = Mathf.Sqrt(x * x + y * y);
            // Aro de arcilla y vidrio.
            p.Over(Bezel, Cover(r - BezelR, aa));
            p.Over(Ink, Cover(r - GlassR - 0.018f, aa));
            p.Over(Color.Lerp(GlassCenter, Glass, Mathf.Clamp01(r / GlassR)), Cover(r - GlassR, aa));
            // Brillo nítido del aro arriba a la izquierda.
            p.Over(new Color(1f, 1f, 1f, 0.22f), Cover(Ellipse(x, y, -0.5f, 0.72f, 0.16f, 0.045f), aa) * Cover(-(r - GlassR - 0.025f), aa));
            // Marcas del aro en las 8 direcciones.
            for (int d = 0; d < RadarContract.Directions; d++)
            {
                float a = d * Mathf.PI / 4f;
                float sx = Mathf.Sin(a), sy = Mathf.Cos(a);
                float tick = Capsule(x, y, sx * (GlassR + 0.035f), sy * (GlassR + 0.035f), sx * (BezelR - 0.03f), sy * (BezelR - 0.03f), 0.012f);
                p.Over(new Color(1f, 1f, 1f, 0.55f), Cover(tick, aa));
            }
            if (r > GlassR) return;

            // Anillos y radios, tenues.
            var line = new Color(Sky.r, Sky.g, Sky.b, 0.20f);
            foreach (float rr in RadarContract.RingRadius)
                p.Over(line, Cover(Mathf.Abs(r - rr * GlassR) - 0.0035f, aa));
            for (int d = 0; d < RadarContract.Directions; d++)
            {
                float a = d * Mathf.PI / 4f;
                float spoke = Capsule(x, y, Mathf.Sin(a) * RadarContract.CenterWindow * GlassR, Mathf.Cos(a) * RadarContract.CenterWindow * GlassR,
                    Mathf.Sin(a) * GlassR, Mathf.Cos(a) * GlassR, 0.0025f);
                p.Over(new Color(Sky.r, Sky.g, Sky.b, 0.10f), Cover(spoke, aa));
            }
            // Pantalla central: un vidrio un poco más claro con borde crema.
            float cw = RadarContract.CenterWindow * GlassR;
            p.Over(GlassCenter, Cover(r - cw, aa));
            p.Over(new Color(1f, 1f, 1f, 0.35f), Cover(Mathf.Abs(r - cw) - 0.006f, aa));
        });

        /// <summary>Haz del radar: cuña con el borde delantero brillante (apunta hacia arriba) y una estela que se apaga
        /// hacia atrás (sentido antihorario, porque el haz gira en sentido horario).</summary>
        public static Color32[] RenderSweep(int size)
        {
            var pixels = new Color32[size * size];
            const float trail = Mathf.PI * 0.45f;
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
                        // Ángulo "detrás" del borde delantero (que está en +y), medido en sentido antihorario.
                        float behind = Mathf.Atan2(-x, y);
                        if (behind < 0f) behind += Mathf.PI * 2f;
                        float fade = behind <= trail ? 1f - behind / trail : 0f;
                        float edge = Mathf.Clamp01(1f - Mathf.Abs(x) * size * 0.25f) * (y > 0f ? 1f : 0f);
                        float radial = Mathf.Clamp01((1f - r) * 12f) * Mathf.Clamp01(r * 6f);
                        a = Mathf.Max(fade * fade * 0.55f, edge * 0.9f) * radial;
                    }
                    pixels[py * size + px] = new Color32(255, 255, 255, (byte)(Mathf.Clamp01(a) * 255f));
                }
            }
            return pixels;
        }

        /// <summary>Interferencia: vidrio opaco cubierto de manchas redondas y trazos de la paleta, al azar (semilla fija).</summary>
        public static Color32[] RenderMask(int size, int seed)
        {
            var rng = new System.Random(seed);
            var colors = new[] { Sky, Grape, Cream, Sun, Coral, Lime, Bezel, Hex(0x9C94D6) };
            var acc = new Px[size * size];
            float glass = GlassR / ScopeZoom;
            // Fondo: el vidrio, opaco.
            for (int i = 0; i < acc.Length; i++)
            {
                float x = (i % size + 0.5f) / size * 2f - 1f, y = (i / size + 0.5f) / size * 2f - 1f;
                float r = Mathf.Sqrt(x * x + y * y);
                acc[i].Over(Glass, Mathf.Clamp01((glass - r) * size * 0.5f));
            }
            // Manchas y trazos: muchos, de tamaños parecidos a los del juego, para "borrar" lo que se vio.
            for (int k = 0; k < 520; k++)
            {
                var c = colors[rng.Next(colors.Length)];
                c.a = 0.55f + 0.4f * (float)rng.NextDouble();
                float cx = (float)rng.NextDouble() * 2f - 1f, cy = (float)rng.NextDouble() * 2f - 1f;
                bool stroke = rng.NextDouble() < 0.4;
                float rad = 0.015f + 0.045f * (float)rng.NextDouble();
                float ang = (float)(rng.NextDouble() * Mathf.PI * 2f), len = 0.04f + 0.08f * (float)rng.NextDouble();
                float ex = cx + Mathf.Cos(ang) * len, ey = cy + Mathf.Sin(ang) * len;
                int x0 = Mathf.Max(0, (int)((Mathf.Min(cx, ex) - rad - 0.1f + 1f) * 0.5f * size));
                int x1 = Mathf.Min(size - 1, (int)((Mathf.Max(cx, ex) + rad + 0.1f + 1f) * 0.5f * size));
                int y0 = Mathf.Max(0, (int)((Mathf.Min(cy, ey) - rad - 0.1f + 1f) * 0.5f * size));
                int y1 = Mathf.Min(size - 1, (int)((Mathf.Max(cy, ey) + rad + 0.1f + 1f) * 0.5f * size));
                for (int py = y0; py <= y1; py++)
                {
                    for (int px = x0; px <= x1; px++)
                    {
                        float x = (px + 0.5f) / size * 2f - 1f, y = (py + 0.5f) / size * 2f - 1f;
                        if (Mathf.Sqrt(x * x + y * y) > glass) continue;
                        float sdf = stroke ? Capsule(x, y, cx, cy, ex, ey, rad * 0.45f) : Circle(x, y, cx, cy, rad);
                        acc[py * size + px].Over(c, Cover(sdf, 2f / size));
                    }
                }
            }
            var pixels = new Color32[size * size];
            for (int i = 0; i < acc.Length; i++) pixels[i] = acc[i].ToColor32();
            return pixels;
        }

        // ------------------------------------------------------------------ piezas de Rescate relámpago

        public static readonly Color RobotGray = Hex(0xB4BCD6);

        private static float RobotBody(float x, float y) =>
            Union(RoundBox(x, y, 0f, -0.1f, 0.72f, 0.6f, 0.24f), Circle(x, y, 0f, 0.8f, 0.12f));

        public static Color32[] RenderRobot(int size) => RenderClay(size, 1.1f, 0.07f, 0.09f, 0.025f, RobotBody, (ref Px p, float x, float y) =>
        {
            const float aa = 0.025f;
            float head = RoundBox(x, y, 0f, -0.1f, 0.72f, 0.6f, 0.24f);
            p.Over(RobotGray, Cover(head, aa));
            // Antena con luz coral (apagada: el robot "duerme").
            p.Over(Ink, Cover(Capsule(x, y, 0f, 0.5f, 0f, 0.72f, 0.04f), aa));
            p.Over(Coral, Cover(Circle(x, y, 0f, 0.8f, 0.12f) + 0.035f, aa));
            // Visera recta (el astronauta la tiene redonda) con dos luces.
            float visor = RoundBox(x, y, 0f, 0.04f, 0.5f, 0.22f, 0.06f);
            p.Over(Ink, Cover(visor, aa));
            p.Over(Hex(0x3A4486), Cover(visor + 0.04f, aa));
            p.Over(Sky, Cover(Circle(x, y, -0.22f, 0.04f, 0.08f), aa));
            p.Over(Sky, Cover(Circle(x, y, 0.22f, 0.04f, 0.08f), aa));
            // Rejilla de la boca y brillo.
            for (int i = -1; i <= 1; i++)
                p.Over(Shade(RobotGray, 0.7f), Cover(Capsule(x, y, i * 0.18f, -0.46f, i * 0.18f, -0.32f, 0.035f), aa));
            p.Over(new Color(1f, 1f, 1f, 0.35f), Cover(Ellipse(x, y, -0.4f, 0.36f, 0.14f, 0.06f), aa) * Cover(head + 0.04f, aa));
        });

        private static float BeaconBody(float x, float y) => Circle(x, y, 0f, 0f, 0.7f);

        public static Color32[] RenderBeacon(int size) => RenderClay(size, 1.1f, 0.09f, 0.1f, 0.03f, BeaconBody, (ref Px p, float x, float y) =>
        {
            float disc = BeaconBody(x, y);
            p.Over(Sky, Cover(disc, 0.03f));
            p.Over(new Color(1f, 1f, 1f, 0.3f), Cover(Ellipse(x, y, -0.24f, 0.36f, 0.18f, 0.07f), 0.03f) * Cover(disc + 0.03f, 0.03f));
            p.Over(Ink, Cover(Circle(x, y, 0f, 0f, 0.27f), 0.03f));
            p.Over(Cream, Cover(Circle(x, y, 0f, 0f, 0.21f), 0.03f));
        });

        /// <summary>Lugar vacío: aro tenue (blanco, se tiñe con <c>Image.color</c>), sin relleno.</summary>
        public static Color32[] RenderSlot(int size)
        {
            var pixels = new Color32[size * size];
            for (int py = 0; py < size; py++)
                for (int px = 0; px < size; px++)
                {
                    float x = (px + 0.5f) / size * 2f - 1f, y = (py + 0.5f) / size * 2f - 1f;
                    float r = Mathf.Sqrt(x * x + y * y);
                    float a = Cover(Mathf.Abs(r - 0.78f) - 0.05f, 2.5f / size);
                    pixels[py * size + px] = new Color32(255, 255, 255, (byte)(a * 255f));
                }
            return pixels;
        }
    }
}
