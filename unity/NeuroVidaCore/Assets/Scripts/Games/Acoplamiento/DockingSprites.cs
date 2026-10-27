using System.Collections.Generic;
using UnityEngine;
using NeuroVida.Games.Shared;
using static NeuroVida.Games.Shared.ClayRaster;

namespace NeuroVida.Games.Acoplamiento
{
    /// <summary>
    /// Arte procedural de Acoplamiento (sin assets), con el pincel de arcilla común (<see cref="ClayRaster"/>): lo que queda de la v2 y sirve de RESPALDO cuando no está el atlas de plasticina (<c>PlasticArt</c>, grupo «acoplamiento» y losas del kit): el panel del puerto, los botones «Encaja» y «Espejo»
    /// con sus íconos y los chevrones. La pieza y el hueco (la v3, con la lámpara fija en la pantalla) se hornean por módulo en <see cref="DockingArt"/>; la estación de anillos ya no existe en la pantalla de juego.
    /// Las funciones <c>Render*</c> devuelven los píxeles (fila 0 = abajo) para poder previsualizarlos fuera de Unity.
    /// </summary>
    public static class DockingSprites
    {
        public static readonly Color LimeColor = Hex(0xA6E36B), GrapeColor = Hex(0xB79BFF), PortColor = Hex(0x2C2A6E), CreamIcon = Hex(0xE9E6FF);

        private const float Aa = 0.03f;
        private static readonly Dictionary<int, Sprite> Cache = new Dictionary<int, Sprite>();

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

        // ------------------------------------------------------------------ la elipse (la pista de los anillos de la escultura final)

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

        private static Sprite Get(int key, System.Func<Sprite> build)
        {
            if (!Cache.TryGetValue(key, out var s) || s == null)
            {
                s = build();
                Cache[key] = s;
            }
            return s;
        }

        /// <summary>Hornea todo lo fijo de a poco (durante la cuenta regresiva): una pieza de arte por cuadro. Con el atlas de plasticina solo hace falta el respaldo de lo que no está en él.</summary>
        public static System.Collections.IEnumerator Prewarm()
        {
            PortPanel(); yield return null;
            FitButton(); MirrorButton(); yield return null;
            FitIcon(); MirrorIcon(); Chevron(); yield return null;
        }
    }
}
