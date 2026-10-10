using System.Collections.Generic;
using UnityEngine;
using NeuroVida.Games.Shared;
using static NeuroVida.Games.Shared.ClayRaster;

namespace NeuroVida.Games.Acoplamiento
{
    /// <summary>
    /// Arte procedural de Acoplamiento (v2, 10-oct; sin assets), con el pincel de arcilla común (<see cref="ClayRaster"/>) y las medidas y colores del boceto aprobado (docs/previews/acoplamiento-boceto.html):
    /// <list type="bullet">
    /// <item><see cref="BakePiece"/>: el MÓDULO (bloques de arcilla celeste, planos, con juntas finas de tinta y 4 remaches simétricos por bloque; SIN luz ni sombra horneadas, porque la luz y la sombra quedan FIJAS en la pantalla y no giran con la pieza: las pone el controlador con
    /// <see cref="LightGradient"/> y <see cref="LightSpot"/> recortadas por la silueta) y el HUECO del puerto (el borde y el fondo oscuro), ambos de la misma pieza, a la misma escala.</item>
    /// <item><see cref="PortPanel"/>, <see cref="FitButton"/>, <see cref="MirrorButton"/>: el puerto y los dos botones de arcilla, con sus íconos (<see cref="FitIcon"/>, <see cref="MirrorIcon"/>).</item>
    /// <item><see cref="EllipseInk"/> y <see cref="EllipseColor"/> (los anillos de la estación), <see cref="SlotSky"/>, <see cref="SlotLime"/>, <see cref="SlotEmpty"/> (sus casilleros) y <see cref="Chevron"/> (las luces del puerto).</item>
    /// </list>
    /// Las funciones <c>Render*</c> devuelven los píxeles (fila 0 = abajo) para poder previsualizarlos fuera de Unity.
    /// </summary>
    public static class DockingSprites
    {
        public static readonly Color ModuleColor = Hex(0x7FD8FF), LimeColor = Hex(0xA6E36B), GrapeColor = Hex(0xB79BFF), PortColor = Hex(0x2C2A6E), HoleColor = Hex(0x070818), RingBase = Hex(0x5B4FC0), CreamIcon = Hex(0xE9E6FF);

        private const float Aa = 0.03f;
        private static readonly Dictionary<int, Sprite> Cache = new Dictionary<int, Sprite>();

        // ------------------------------------------------------------------ la pieza (módulo y hueco)

        /// <summary>El lado del cuadro de la pieza en bloques (4 de la pieza más margen para el borde): la pieza queda centrada en su caja.</summary>
        public const float PieceSideCells = 4.8f;
        public const int PiecePx = 384;
        /// <summary>El borde de tinta del módulo y del hueco, en bloques (3 dp con bloques de 26 dp).</summary>
        public const float InkBorder = 3f / 26f, HoleInset = 0.6f / 26f;

        public enum PieceLayer { Body, Edge, Hole }

        /// <summary>La caja de la pieza: el centro de su cuadro delimitador, en coordenadas de bloque.</summary>
        public static void PieceCenter(IReadOnlyList<Cell> cells, out float cx, out float cy, out int w, out int h)
        {
            w = 0;
            h = 0;
            foreach (var c in cells) { w = Mathf.Max(w, c.X + 1); h = Mathf.Max(h, c.Y + 1); }
            cx = (w - 1) * 0.5f;
            cy = (h - 1) * 0.5f;
        }

        /// <summary>
        /// Distancia con signo a la pieza (en bloques): UNA sola silueta. Además de cada bloque se suma un «puente» que cubre cada par de bloques vecinos; sin eso, la unión de cuadrados que se tocan deja la costura entre ellos con distancia 0 (se dibujaría como borde).
        /// </summary>
        private static float PieceSdf(Cell[] cells, HashSet<Cell> set, float gx, float gy)
        {
            const float r = 0.16f;
            float d = 1e5f;
            foreach (var c in cells)
            {
                float cx = c.X, cy = c.Y;
                d = Mathf.Min(d, RoundBox(gx, gy, cx, cy, 0.5f, 0.5f, r));
                if (set.Contains(new Cell(c.X + 1, c.Y))) d = Mathf.Min(d, RoundBox(gx, gy, cx + 0.5f, cy, 1f, 0.5f, r));
                if (set.Contains(new Cell(c.X, c.Y + 1))) d = Mathf.Min(d, RoundBox(gx, gy, cx, cy + 0.5f, 0.5f, 1f, r));
            }
            return d;
        }

        /// <summary>
        /// Los píxeles de una capa de la pieza (fila 0 = abajo), de <paramref name="size"/> × <paramref name="size"/>: <see cref="PieceLayer.Body"/> es el módulo (borde de tinta, celeste plano, juntas de 1 dp entre bloques y 4 remaches simétricos por bloque);
        /// <see cref="PieceLayer.Edge"/> es la silueta blanca del borde del hueco (se tiñe con el estado del puerto) y <see cref="PieceLayer.Hole"/> el fondo del hueco (blanco: se tiñe de oscuro).
        /// </summary>
        public static Color32[] RenderPiece(Cell[] cells, PieceLayer layer, int size)
        {
            var body = layer == PieceLayer.Body ? new Color32[size * size] : null;
            var edge = layer == PieceLayer.Edge ? new Color32[size * size] : null;
            var hole = layer == PieceLayer.Hole ? new Color32[size * size] : null;
            RenderPieceLayers(cells, size, body, edge, hole);
            return body ?? edge ?? hole;
        }

        /// <summary>Calcula las capas pedidas (las que no son null) en UNA sola pasada: la distancia a la pieza se evalúa una vez por píxel.</summary>
        public static void RenderPieceLayers(Cell[] cells, int size, Color32[] body, Color32[] edge, Color32[] hole)
        {
            var set = new HashSet<Cell>(cells);
            PieceCenter(cells, out float ccx, out float ccy, out int w, out int h);
            float aa = PieceSideCells / size * 1.5f;
            // los píxeles lejos de la caja de la pieza (con su borde) quedan vacíos sin evaluar nada
            float reach = Mathf.Max(w, h) * 0.5f + 1f;
            for (int py = 0; py < size; py++)
            {
                float gy = ccy + ((py + 0.5f) / size - 0.5f) * PieceSideCells;
                for (int x = 0; x < size; x++)
                {
                    float gx = ccx + ((x + 0.5f) / size - 0.5f) * PieceSideCells;
                    if (Mathf.Abs(gx - ccx) > reach || Mathf.Abs(gy - ccy) > reach) continue;
                    float d = PieceSdf(cells, set, gx, gy);
                    int i = py * size + x;
                    if (edge != null) { var p = new Px(); p.Over(Color.white, Cover(d - InkBorder, aa)); edge[i] = p.ToColor32(); }
                    if (hole != null) { var p = new Px(); p.Over(Color.white, Cover(d + HoleInset, aa)); hole[i] = p.ToColor32(); }
                    if (body == null) continue;
                    var q = new Px();
                    q.Over(Ink, Cover(d - InkBorder, aa));
                    q.Over(ModuleColor, Cover(d + 0.02f, aa));
                    int ix = Mathf.RoundToInt(gx), iy = Mathf.RoundToInt(gy);
                    if (set.Contains(new Cell(ix, iy)))
                    {
                        float lx = gx - ix, ly = gy - iy, inside = Cover(d + 0.02f, aa);
                        // las juntas: una línea fina de tinta (1 dp) en cada lado que comparte con un vecino
                        float joint = 1e5f;
                        if (Mathf.Abs(ly) <= 0.5f)
                        {
                            if (set.Contains(new Cell(ix + 1, iy))) joint = Mathf.Min(joint, Mathf.Abs(lx - 0.5f));
                            if (set.Contains(new Cell(ix - 1, iy))) joint = Mathf.Min(joint, Mathf.Abs(lx + 0.5f));
                        }
                        if (Mathf.Abs(lx) <= 0.5f)
                        {
                            if (set.Contains(new Cell(ix, iy + 1))) joint = Mathf.Min(joint, Mathf.Abs(ly - 0.5f));
                            if (set.Contains(new Cell(ix, iy - 1))) joint = Mathf.Min(joint, Mathf.Abs(ly + 0.5f));
                        }
                        q.Over(new Color(Ink.r, Ink.g, Ink.b, 0.55f), Cover(joint - 0.02f, aa) * inside);
                        // los 4 remaches de cada bloque: simétricos, no delatan la orientación
                        foreach (var (rx, ry) in Rivets)
                            q.Over(new Color(26f / 255f, 18f / 255f, 64f / 255f, 0.35f), Cover(Circle(lx, ly, rx, ry, 0.06f), aa) * inside);
                    }
                    body[i] = q.ToColor32();
                }
            }
        }

        private static readonly (float, float)[] Rivets = { (-0.27f, -0.27f), (0.27f, -0.27f), (-0.27f, 0.27f), (0.27f, 0.27f) };

        /// <summary>Hornea el módulo, el borde del hueco y el fondo del hueco de una pieza. TEXTURAS NUEVAS en cada llamada: quien las pide libera las anteriores (<see cref="Release"/>).</summary>
        public static void BakePiece(Cell[] cells, out Sprite body, out Sprite edge, out Sprite hole)
        {
            var b = new Color32[PiecePx * PiecePx];
            var e = new Color32[PiecePx * PiecePx];
            var o = new Color32[PiecePx * PiecePx];
            RenderPieceLayers(cells, PiecePx, b, e, o);
            body = ToSprite(b, PiecePx, PiecePx);
            edge = ToSprite(e, PiecePx, PiecePx);
            hole = ToSprite(o, PiecePx, PiecePx);
        }

        /// <summary>Libera la textura de un sprite horneado por pieza.</summary>
        public static void Release(Sprite sprite)
        {
            if (sprite == null) return;
            var tex = sprite.texture;
            if (Application.isPlaying)
            {
                Object.Destroy(sprite);
                if (tex != null) Object.Destroy(tex);
            }
            else
            {
                Object.DestroyImmediate(sprite);                                  // en las pruebas EditMode Destroy avisa con un error
                if (tex != null) Object.DestroyImmediate(tex);
            }
        }

        // ------------------------------------------------------------------ la luz fija (no gira con la pieza)

        /// <summary>El degradado de luz y sombra sobre el módulo, en la pantalla: arriba un velo blanco (35 %), en el medio nada y abajo un velo oscuro (15 %), como en el boceto (luz de arriba). Va recortado por la silueta del módulo y NO gira con él.</summary>
        public static Sprite LightGradient() => Get(200, () =>
        {
            const int n = 64;
            var px = new Color32[n * n];
            for (int y = 0; y < n; y++)
            {
                float t = 1f - (y + 0.5f) / n;                         // 0 arriba, 1 abajo
                Color c = t < 0.55f ? new Color(1f, 1f, 1f, 0.35f * (1f - t / 0.55f)) : new Color(8f / 255f, 6f / 255f, 30f / 255f, 0.18f * ((t - 0.55f) / 0.45f));
                for (int x = 0; x < n; x++) px[y * n + x] = (Color32)c;
            }
            return ToSprite(px, n, n);
        });

        /// <summary>El brillo de arriba a la izquierda (una elipse blanca; se pinta al 30 %): también fijo en la pantalla.</summary>
        public static Sprite LightSpot() => Get(201, () => ToSprite(RenderSpot(128), 128, 128));

        public static Color32[] RenderSpot(int size)
        {
            var px = new Color32[size * size];
            for (int py = 0; py < size; py++)
                for (int x = 0; x < size; x++)
                {
                    float u = ((x + 0.5f) / size * 2f - 1f), v = ((py + 0.5f) / size * 2f - 1f);
                    var p = new Px();
                    p.Over(Color.white, Cover(Ellipse(u, v, 0f, 0f, 0.9f, 0.9f), 0.04f));
                    px[py * size + x] = p.ToColor32();
                }
            return px;
        }

        // ------------------------------------------------------------------ el puerto y los botones (arcilla)

        /// <summary>1 unidad = la mitad del ancho del puerto (84 dp); el sprite es cuadrado de lado <see cref="PanelSide"/> dp.</summary>
        public const float PanelUnit = 84f, PanelZoom = 1.15f, PanelSide = 2f * PanelZoom * PanelUnit;
        /// <summary>1 unidad = la mitad del ancho de un botón (79 dp).</summary>
        public const float ButtonUnit = 79f, ButtonZoom = 1.12f, ButtonSide = 2f * ButtonZoom * ButtonUnit;

        private static Color ClayFill(Color c, float y, float half) =>
            y > 0.1f * half ? Color.Lerp(c, Tint(c, 0.32f), Mathf.Clamp01((y - 0.1f * half) / (0.9f * half))) : Color.Lerp(c, Shade(c, 0.88f), Mathf.Clamp01((0.1f * half - y) / (1.1f * half)));

        private static Color32[] RenderClayBox(int size, float unit, float zoom, float hx, float hy, float radiusDp, float lineDp, float dropDp, Color fill)
        {
            float r = radiusDp / unit, line = lineDp / unit, drop = dropDp / unit;
            return RenderClay(size, zoom, line, drop, Aa, (x, y) => RoundBox(x, y, 0f, 0f, hx, hy, r), (ref Px p, float x, float y) =>
                p.Over(ClayFill(fill, y, hy), Cover(RoundBox(x, y, 0f, 0f, hx, hy, r) + 0.02f, Aa)));
        }

        /// <summary>El panel del puerto: 168 × 146 dp, radio 22, borde de tinta de 3,4 dp y sombra dura de 6 dp hacia abajo.</summary>
        public static Sprite PortPanel() => Get(210, () => ToSprite(RenderPortPanel(512), 512, 512));

        public static Color32[] RenderPortPanel(int size) => RenderClayBox(size, PanelUnit, PanelZoom, 1f, 73f / PanelUnit, 22f, 3.4f, 6f, PortColor);

        /// <summary>«Encaja» (lima) y «Espejo» (uva): 158 × 76 dp, radio 22, borde de tinta de 3,4 dp y sombra dura de 6 dp hacia abajo.</summary>
        public static Sprite FitButton() => Get(211, () => ToSprite(RenderButton(512, LimeColor), 512, 512));
        public static Sprite MirrorButton() => Get(212, () => ToSprite(RenderButton(512, GrapeColor), 512, 512));

        public static Color32[] RenderButton(int size, Color color) => RenderClayBox(size, ButtonUnit, ButtonZoom, 1f, 38f / ButtonUnit, 22f, 3.4f, 6f, color);

        // ------------------------------------------------------------------ íconos de los botones (±18 dp)

        public const float IconHalf = 18f;

        private static (float x, float y) ToDp(int px, int py, int size) => (((px + 0.5f) / size * 2f - 1f) * IconHalf, -((py + 0.5f) / size * 2f - 1f) * IconHalf);   // el sprite va con la fila 0 abajo; el dibujo del boceto, con y hacia abajo

        private static float BlockDp(float x, float y, float cx, float cy, float w, float h) => RoundBox(x, y, cx + w * 0.5f, cy + h * 0.5f, w * 0.5f, h * 0.5f, 2f);

        /// <summary>«Encaja»: tres bloques (una L) que bajan a su soporte.</summary>
        public static Sprite FitIcon() => Get(213, () => ToSprite(RenderFitIcon(128), 128, 128));

        public static Color32[] RenderFitIcon(int size)
        {
            var px = new Color32[size * size];
            float aa = IconHalf * 2f / size * 1.5f;
            for (int py = 0; py < size; py++)
                for (int x = 0; x < size; x++)
                {
                    var (u, v) = ToDp(x, py, size);
                    float blocks = Mathf.Min(BlockDp(u, v, -9f, -14f, 8f, 8f), Mathf.Min(BlockDp(u, v, -1f, -14f, 8f, 8f), BlockDp(u, v, -1f, -6f, 8f, 8f)));
                    // el soporte: una U (-13,6) → (-13,13) → (13,13) → (13,6)
                    float support = Mathf.Min(Capsule(u, v, -13f, 6f, -13f, 13f, 1.5f), Mathf.Min(Capsule(u, v, -13f, 13f, 13f, 13f, 1.5f), Capsule(u, v, 13f, 13f, 13f, 6f, 1.5f)));
                    var p = new Px();
                    p.Over(Ink, Cover(Mathf.Min(blocks - 1.5f, support), aa));
                    p.Over(CreamIcon, Cover(blocks + 1.5f, aa));
                    px[py * size + x] = p.ToColor32();
                }
            return px;
        }

        /// <summary>«Espejo»: dos figuras espejadas con un eje punteado en el medio.</summary>
        public static Sprite MirrorIcon() => Get(214, () => ToSprite(RenderMirrorIcon(128), 128, 128));

        public static Color32[] RenderMirrorIcon(int size)
        {
            var px = new Color32[size * size];
            float aa = IconHalf * 2f / size * 1.5f;
            for (int py = 0; py < size; py++)
                for (int x = 0; x < size; x++)
                {
                    var (u, v) = ToDp(x, py, size);
                    float left = Mathf.Min(BlockDp(u, v, -15f, -10f, 6f, 6f), Mathf.Min(BlockDp(u, v, -15f, -4f, 6f, 6f), BlockDp(u, v, -9f, 2f, 6f, 6f)));
                    float right = Mathf.Min(BlockDp(u, v, 9f, -10f, 6f, 6f), Mathf.Min(BlockDp(u, v, 9f, -4f, 6f, 6f), BlockDp(u, v, 3f, 2f, 6f, 6f)));
                    float figures = Mathf.Min(left, right);
                    // el eje punteado: tramos de 3 dp cada 6 dp, de -14 a 14
                    float axis = 1e5f;
                    for (float y0 = -14f; y0 < 14f; y0 += 6f) axis = Mathf.Min(axis, Capsule(u, v, 0f, y0, 0f, Mathf.Min(14f, y0 + 3f), 1.5f));
                    var p = new Px();
                    p.Over(Ink, Cover(Mathf.Min(figures - 1.5f, axis), aa));
                    p.Over(CreamIcon, Cover(figures + 1.5f, aa));
                    px[py * size + x] = p.ToColor32();
                }
            return px;
        }

        // ------------------------------------------------------------------ la estación (anillos y casilleros)

        /// <summary>La elipse de un anillo (128 × 30 dp de radios) con su trazo: el de tinta (9 dp) va debajo del de color (5 dp). Textura rectangular, 2 píxeles por dp.</summary>
        public const float RingRx = DockingPlan.StationRx, RingRy = DockingPlan.StationRy, RingPad = 8f, RingPxPerDp = 2f;
        public const float RingSpriteW = (2f * RingRx + 2f * RingPad), RingSpriteH = (2f * RingRy + 2f * RingPad);

        public static Sprite EllipseInk() => Get(220, () => ToSpriteRect(RenderEllipseRing(9f), (int)(RingSpriteW * RingPxPerDp), (int)(RingSpriteH * RingPxPerDp)));
        public static Sprite EllipseColor() => Get(221, () => ToSpriteRect(RenderEllipseRing(5f), (int)(RingSpriteW * RingPxPerDp), (int)(RingSpriteH * RingPxPerDp)));

        /// <summary>El trazo blanco (de <paramref name="strokeDp"/> dp) de la elipse del anillo, en una textura de <see cref="RingSpriteW"/> × <see cref="RingSpriteH"/> dp.</summary>
        public static Color32[] RenderEllipseRing(float strokeDp)
        {
            int w = (int)(RingSpriteW * RingPxPerDp), h = (int)(RingSpriteH * RingPxPerDp);
            var px = new Color32[w * h];
            float half = strokeDp * 0.5f, aa = 1.5f / RingPxPerDp;
            for (int py = 0; py < h; py++)
            {
                float v = -(((py + 0.5f) / h * 2f - 1f) * (RingSpriteH * 0.5f));              // y hacia arriba, en dp
                for (int x = 0; x < w; x++)
                {
                    float u = ((x + 0.5f) / w * 2f - 1f) * (RingSpriteW * 0.5f);
                    float d = Mathf.Abs(EllipseDistance(u, v, RingRx, RingRy)) - half;
                    byte a = (byte)(Cover(d, aa) * 255f + 0.5f);
                    if (a > 0) px[py * w + x] = new Color32(255, 255, 255, a);
                }
            }
            return px;
        }

        /// <summary>Distancia (con signo, aproximada con unas vueltas de Newton) de un punto a una elipse de radios (a, b) centrada en el origen.</summary>
        public static float EllipseDistance(float px, float py, float a, float b)
        {
            float ax = Mathf.Abs(px), ay = Mathf.Abs(py);
            float t = Mathf.Atan2(ay * a, ax * b);                      // un buen arranque
            for (int i = 0; i < 4; i++)
            {
                float c = Mathf.Cos(t), s = Mathf.Sin(t);
                float ex = a * c, ey = b * s;                           // el punto de la elipse
                float dx = -a * s, dy = b * c;                          // su tangente
                float ddx = -a * c, ddy = -b * s;
                float rx = ex - ax, ry = ey - ay;
                float f = rx * dx + ry * dy, fp = dx * dx + dy * dy + rx * ddx + ry * ddy;
                if (Mathf.Abs(fp) < 1e-6f) break;
                t = Mathf.Clamp(t - f / fp, 0f, Mathf.PI * 0.5f);
            }
            float qx = a * Mathf.Cos(t) - ax, qy = b * Mathf.Sin(t) - ay;
            float dist = Mathf.Sqrt(qx * qx + qy * qy);
            bool inside = (ax * ax) / (a * a) + (ay * ay) / (b * b) < 1f;
            return inside ? -dist : dist;
        }

        /// <summary>Un casillero ocupado: 26 × 18 dp (radio 6, borde de tinta de 2,6 dp). Celeste, o lima cuando su anillo está completo.</summary>
        public const float SlotW = 26f, SlotH = 18f;

        public static Sprite SlotSky() => Get(222, () => ToSprite(RenderSlot(128, ModuleColor), 128, 128));
        public static Sprite SlotLime() => Get(223, () => ToSprite(RenderSlot(128, LimeColor), 128, 128));
        /// <summary>El lado del sprite de un casillero en dp (un poco más que él: borde y sombra).</summary>
        public const float SlotSide = 36f;

        public static Color32[] RenderSlot(int size, Color fill)
        {
            float unit = SlotSide * 0.5f, hx = (SlotW * 0.5f) / unit, hy = (SlotH * 0.5f) / unit, r = 6f / unit;
            return RenderClay(size, 1f, 2.6f / unit, 3f / unit, Aa, (x, y) => RoundBox(x, y, 0f, 0f, hx, hy, r), (ref Px p, float x, float y) =>
                p.Over(ClayFill(fill, y, hy), Cover(RoundBox(x, y, 0f, 0f, hx, hy, r) + 0.02f, Aa)));
        }

        /// <summary>Un casillero libre: un contorno punteado violeta apagado (24 × 16 dp).</summary>
        public static Sprite SlotEmpty() => Get(224, () => ToSprite(RenderSlotEmpty(128), 128, 128));

        public static Color32[] RenderSlotEmpty(int size)
        {
            var px = new Color32[size * size];
            float unit = SlotSide * 0.5f, hx = 12f / unit, hy = 8f / unit, r = 6f / unit, aa = 1.5f / size * 2f;
            var col = new Color(184f / 255f, 176f / 255f, 1f, 0.35f);
            for (int py = 0; py < size; py++)
                for (int x = 0; x < size; x++)
                {
                    float u = ((x + 0.5f) / size * 2f - 1f), v = ((py + 0.5f) / size * 2f - 1f);
                    float d = Mathf.Abs(RoundBox(u, v, 0f, 0f, hx, hy, r)) - 1f / unit;
                    // el punteado: tramos de 4 dp cada 8 dp a lo largo del contorno (ángulo)
                    float dash = Mathf.Sin(Mathf.Atan2(v / hy, u / hx) * 7f) > -0.1f ? 1f : 0f;
                    var p = new Px();
                    p.Over(col, Cover(d, aa) * dash);
                    px[py * size + x] = p.ToColor32();
                }
            return px;
        }

        /// <summary>Una luz del puerto: una «V» de 12 × 6 dp (trazo de 3 dp). Blanca: se tiñe de lima.</summary>
        public static Sprite Chevron() => Get(225, () => ToSprite(RenderChevron(64), 64, 64));
        public const float ChevronSide = 18f;

        public static Color32[] RenderChevron(int size)
        {
            var px = new Color32[size * size];
            float half = ChevronSide * 0.5f, aa = ChevronSide / size * 1.5f;
            for (int py = 0; py < size; py++)
                for (int x = 0; x < size; x++)
                {
                    float u = ((x + 0.5f) / size * 2f - 1f) * half, v = ((py + 0.5f) / size * 2f - 1f) * half;
                    float d = Mathf.Min(Capsule(u, v, -6f, 3f, 0f, -3f, 1.5f), Capsule(u, v, 0f, -3f, 6f, 3f, 1.5f));
                    var p = new Px();
                    p.Over(Color.white, Cover(d, aa));
                    px[py * size + x] = p.ToColor32();
                }
            return px;
        }

        // ------------------------------------------------------------------ sprites rectangulares y caché

        /// <summary>Un sprite de una textura rectangular (1 unidad = el ancho).</summary>
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

        /// <summary>Hornea todo lo fijo de a poco (durante la cuenta regresiva): una pieza de arte por cuadro.</summary>
        public static System.Collections.IEnumerator Prewarm()
        {
            LightGradient(); LightSpot(); yield return null;
            PortPanel(); yield return null;
            FitButton(); MirrorButton(); yield return null;
            FitIcon(); MirrorIcon(); Chevron(); yield return null;
            EllipseInk(); yield return null;
            EllipseColor(); yield return null;
            SlotSky(); SlotLime(); SlotEmpty();
        }
    }
}
