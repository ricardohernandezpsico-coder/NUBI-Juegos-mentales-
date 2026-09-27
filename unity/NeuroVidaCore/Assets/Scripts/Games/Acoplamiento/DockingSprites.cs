using UnityEngine;
using NeuroVida.Games.Shared;
using static NeuroVida.Games.Shared.ClayRaster;

namespace NeuroVida.Games.Acoplamiento
{
    /// <summary>
    /// Arte procedural de Acoplamiento, con el pincel de arcilla común (<see cref="ClayRaster"/>):
    /// <list type="bullet">
    /// <item><see cref="ModuleCell"/>: bloque del módulo (celeste, borde tinta, brillo y dos remaches). SIN sombra
    /// horneada: el módulo gira y la sombra dura debe caer siempre hacia abajo, así que el controlador la dibuja aparte
    /// con <see cref="CellSilhouette"/> en tinta, corrida hacia abajo en pantalla.</item>
    /// <item><see cref="SocketCell"/>: hueco del puerto (hundido, con borde crema).</item>
    /// <item><see cref="FitIcon"/> y <see cref="MirrorIcon"/>: íconos de los botones "ENCAJA" y "ESPEJO" (van con texto).</item>
    /// </list>
    /// </summary>
    public static class DockingSprites
    {
        private const int CellPx = 128;
        private static Sprite _cell, _socket, _silhouette, _fit, _mirror;

        public static Sprite ModuleCell() => _cell != null ? _cell : _cell = ToSprite(RenderCell(CellPx), CellPx, CellPx);
        public static Sprite SocketCell() => _socket != null ? _socket : _socket = ToSprite(RenderSocket(CellPx), CellPx, CellPx);
        public static Sprite CellSilhouette() => _silhouette != null ? _silhouette : _silhouette = ToSprite(RenderSilhouette(CellPx), CellPx, CellPx);
        public static Sprite FitIcon() => _fit != null ? _fit : _fit = ToSprite(RenderFit(128), 128, 128);
        public static Sprite MirrorIcon() => _mirror != null ? _mirror : _mirror = ToSprite(RenderMirror(128), 128, 128);

        // Las celdas ocupan todo el cuadro (los bloques vecinos se tocan borde con borde).
        private static float Block(float x, float y) => RoundBox(x, y, 0f, 0f, 0.93f, 0.93f, 0.2f);

        public static Color32[] RenderCell(int size) => RenderClay(size, 1f, 0.12f, 0f, 0.03f, (x, y) => Block(x, y) + 0.12f, (ref Px p, float x, float y) =>
        {
            float b = Block(x, y) + 0.12f;
            p.Over(Sky, Cover(b, 0.03f));
            // Cara superior más clara (volumen de arcilla) y brillo nítido.
            p.Over(Tint(Sky, 0.25f), Cover(RoundBox(x, y, 0f, 0.28f, 0.66f, 0.4f, 0.16f), 0.03f));
            p.Over(new Color(1f, 1f, 1f, 0.55f), Cover(Ellipse(x, y, -0.3f, 0.5f, 0.24f, 0.08f), 0.03f));
            // Remaches (simétricos: no delatan la orientación).
            foreach (var (cx, cy) in new[] { (-0.5f, -0.5f), (0.5f, -0.5f), (-0.5f, 0.5f), (0.5f, 0.5f) })
            {
                p.Over(Ink, Cover(Circle(x, y, cx, cy, 0.1f), 0.03f));
                p.Over(Tint(Sky, 0.6f), Cover(Circle(x, y, cx - 0.02f, cy + 0.02f, 0.05f), 0.03f));
            }
        });

        public static Color32[] RenderSocket(int size)
        {
            var px = new Color32[size * size];
            for (int py = 0; py < size; py++)
            {
                for (int x = 0; x < size; x++)
                {
                    float u = (x + 0.5f) / size * 2f - 1f, v = (py + 0.5f) / size * 2f - 1f;
                    float b = Block(u, v);
                    var p = new Px();
                    p.Over(new Color(Ink.r, Ink.g, Ink.b, 0.75f), Cover(b, 0.03f));
                    // Borde interno crema (el contorno del hueco) y sombra interior arriba (se ve hundido).
                    p.Over(new Color(1f, 1f, 1f, 0.55f), Cover(Mathf.Abs(b + 0.06f) - 0.035f, 0.03f));
                    p.Over(new Color(0f, 0f, 0f, 0.35f), Cover(RoundBox(u, v, 0f, 0.72f, 0.8f, 0.14f, 0.1f), 0.03f) * Cover(b + 0.1f, 0.03f));
                    px[py * size + x] = p.ToColor32();
                }
            }
            return px;
        }

        public static Color32[] RenderSilhouette(int size)
        {
            var px = new Color32[size * size];
            for (int py = 0; py < size; py++)
            {
                for (int x = 0; x < size; x++)
                {
                    float u = (x + 0.5f) / size * 2f - 1f, v = (py + 0.5f) / size * 2f - 1f;
                    float a = Cover(Block(u, v) + 0.12f - 0.12f, 0.03f);
                    px[py * size + x] = new Color32(255, 255, 255, (byte)(a * 255f));
                }
            }
            return px;
        }

        // ------------------------------------------------------------------ pieza entera (una sola arcilla)

        /// <summary>Margen alrededor de la pieza, en celdas (para el borde).</summary>
        public const float PieceMargin = 0.25f;

        /// <summary>Lado del cuadro de la pieza, en celdas (la pieza queda centrada en su caja).</summary>
        public static float PieceSide(Cell[] cells)
        {
            int w = 0, h = 0;
            foreach (var c in cells) { w = Mathf.Max(w, c.X + 1); h = Mathf.Max(h, c.Y + 1); }
            return Mathf.Max(w, h) + PieceMargin * 2f;
        }

        /// <summary>
        /// Distancia con signo a la pieza (en celdas): una sola silueta. Además de cada bloque se suma un "puente" que
        /// cubre cada par de bloques vecinos; sin eso, la unión de cuadrados que se tocan deja la costura entre ellos
        /// con distancia 0 (se dibujaría como borde).
        /// </summary>
        private static float PieceSdf(Cell[] cells, System.Collections.Generic.HashSet<Cell> set, bool mirror, float gx, float gy)
        {
            const float r = 0.16f;
            float s = mirror ? -1f : 1f;
            float d = 1e5f;
            foreach (var c in cells)
            {
                float cx = s * c.X, cy = c.Y;
                d = Mathf.Min(d, RoundBox(gx, gy, cx, cy, 0.5f, 0.5f, r));
                if (set.Contains(new Cell(c.X + 1, c.Y))) d = Mathf.Min(d, RoundBox(gx, gy, cx + s * 0.5f, cy, 1f, 0.5f, r));
                if (set.Contains(new Cell(c.X, c.Y + 1))) d = Mathf.Min(d, RoundBox(gx, gy, cx, cy + 0.5f, 0.5f, 1f, r));
            }
            return d;
        }

        private static void PieceFrame(Cell[] cells, bool mirror, out float cx, out float cy, out float side)
        {
            int w = 0, h = 0;
            foreach (var c in cells) { w = Mathf.Max(w, c.X + 1); h = Mathf.Max(h, c.Y + 1); }
            cx = mirror ? -(w - 1) * 0.5f : (w - 1) * 0.5f;
            cy = (h - 1) * 0.5f;
            side = Mathf.Max(w, h) + PieceMargin * 2f;
        }

        public enum PieceLayer { Body, Silhouette, Socket }

        /// <summary>
        /// Píxeles de la pieza (fila 0 = abajo), cuadrados de <paramref name="size"/>: el módulo de arcilla (celeste,
        /// borde tinta, junta tenue entre bloques, brillo y remaches), su silueta blanca (para la sombra) o el hueco del
        /// puerto. <paramref name="mirror"/>: el reflejo (x → −x).
        /// </summary>
        public static Color32[] RenderPiece(Cell[] cells, bool mirror, PieceLayer layer, int size)
        {
            PieceFrame(cells, mirror, out float ccx, out float ccy, out float side);
            float aa = side / size * 1.5f;
            var set = new System.Collections.Generic.HashSet<Cell>(cells);
            var px = new Color32[size * size];
            for (int py = 0; py < size; py++)
            {
                for (int x = 0; x < size; x++)
                {
                    float gx = ccx + ((x + 0.5f) / size - 0.5f) * side;
                    float gy = ccy + ((py + 0.5f) / size - 0.5f) * side;
                    float d = PieceSdf(cells, set, mirror, gx, gy);
                    var p = new Px();
                    if (layer == PieceLayer.Silhouette)
                    {
                        p.Over(Color.white, Cover(d - 0.07f, aa));
                    }
                    else if (layer == PieceLayer.Socket)
                    {
                        p.Over(new Color(Ink.r, Ink.g, Ink.b, 0.8f), Cover(d, aa));
                        p.Over(new Color(1f, 1f, 1f, 0.6f), Cover(Mathf.Abs(d + 0.05f) - 0.03f, aa));
                    }
                    else
                    {
                        p.Over(Ink, Cover(d - 0.07f, aa));
                        p.Over(Sky, Cover(d, aa));
                        // Bloque de esta celda: junta tenue, brillo y remaches (simétricos: no delatan orientación).
                        int ix = Mathf.RoundToInt(mirror ? -gx : gx), iy = Mathf.RoundToInt(gy);
                        if (set.Contains(new Cell(ix, iy)))
                        {
                            float lx = gx - (mirror ? -ix : ix), ly = gy - iy;
                            float inside = Cover(d, aa);
                            float seam = Mathf.Max(Mathf.Abs(lx), Mathf.Abs(ly));
                            p.Over(new Color(Ink.r, Ink.g, Ink.b, 0.28f), Cover(Mathf.Abs(seam - 0.5f) - 0.02f, aa) * inside);
                            p.Over(new Color(1f, 1f, 1f, 0.28f), Cover(RoundBox(lx, ly, 0f, 0.12f, 0.32f, 0.2f, 0.08f), aa) * inside);
                            p.Over(new Color(1f, 1f, 1f, 0.55f), Cover(Ellipse(lx, ly, -0.14f, 0.26f, 0.12f, 0.04f), aa) * inside);
                            foreach (var (rx, ry) in new[] { (-0.27f, -0.27f), (0.27f, -0.27f), (-0.27f, 0.27f), (0.27f, 0.27f) })
                                p.Over(Ink, Cover(Circle(lx, ly, rx, ry, 0.05f), aa) * inside);
                        }
                    }
                    px[py * size + x] = p.ToColor32();
                }
            }
            return px;
        }

        /// <summary>Sprite de la pieza (textura nueva: quien lo pide destruye la anterior).</summary>
        public static Sprite PieceSprite(Cell[] cells, bool mirror, PieceLayer layer, int size) =>
            ToSprite(RenderPiece(cells, mirror, layer, size), size, size);

        private static float FitBody(float x, float y) =>
            Mathf.Min(RoundBox(x, y, 0f, -0.55f, 0.75f, 0.14f, 0.07f),                              // base del puerto
                Mathf.Min(RoundBox(x, y, 0f, 0.25f, 0.28f, 0.28f, 0.08f),                           // bloque que baja
                    Mathf.Min(Capsule(x, y, -0.2f, -0.12f, 0f, -0.3f, 0.07f), Capsule(x, y, 0.2f, -0.12f, 0f, -0.3f, 0.07f))));

        public static Color32[] RenderFit(int size) => RenderClay(size, 1.1f, 0.08f, 0.08f, 0.03f, FitBody, (ref Px p, float x, float y) =>
        {
            p.Over(Cream, Cover(RoundBox(x, y, 0f, -0.55f, 0.75f, 0.14f, 0.07f), 0.03f));
            p.Over(Sky, Cover(RoundBox(x, y, 0f, 0.25f, 0.28f, 0.28f, 0.08f), 0.03f));
            p.Over(Lime, Cover(Mathf.Min(Capsule(x, y, -0.2f, -0.12f, 0f, -0.3f, 0.07f), Capsule(x, y, 0.2f, -0.12f, 0f, -0.3f, 0.07f)), 0.03f));
        });

        private static float Tri(float x, float y, float sign) =>
            Polygon(x, y, new[] { sign * 0.12f, -0.4f, sign * 0.12f, 0.4f, sign * 0.72f, 0f });

        private static float MirrorBody(float x, float y) =>
            Mathf.Min(Mathf.Min(Tri(x, y, 1f), Tri(x, y, -1f)), RoundBox(x, y, 0f, 0f, 0.035f, 0.72f, 0.03f));

        public static Color32[] RenderMirror(int size) => RenderClay(size, 1.1f, 0.08f, 0.08f, 0.03f, MirrorBody, (ref Px p, float x, float y) =>
        {
            p.Over(Grape, Cover(Tri(x, y, -1f), 0.03f));
            p.Over(Tint(Grape, 0.45f), Cover(Tri(x, y, 1f), 0.03f));
            p.Over(Cream, Cover(RoundBox(x, y, 0f, 0f, 0.035f, 0.72f, 0.03f), 0.03f));
        });
    }
}
