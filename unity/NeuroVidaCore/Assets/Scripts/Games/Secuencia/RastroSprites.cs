using UnityEngine;
using NeuroVida.Games.Shared;
using static NeuroVida.Games.Shared.ClayRaster;

namespace NeuroVida.Games.Secuencia
{
    /// <summary>
    /// El arte de «Rastro de luz», horneado por código (sin assets): los luceros de cristal (degradé radial blanco → color → translúcido, borde
    /// blanco fino y brillo arriba a la izquierda; redondos, SIN puntas ni símbolos), el disco punteado del tablero y los íconos de los cuatro
    /// modos DIBUJADOS con trazos (no glifos de la fuente: cada teléfono los dibuja distinto o no los tiene). Todo en blanco o ya coloreado:
    /// los íconos y la ✗ se tiñen con <c>Image.color</c>.
    /// </summary>
    public static class RastroSprites
    {
        private const int OrbPx = 192;
        private static readonly Sprite[] Orbs = new Sprite[RastroBoard.Orbs];
        private static readonly Sprite[] Icons = new Sprite[RastroModes.Count];
        private static Sprite _disc, _cross, _dashed, _vignette;

        public static Sprite Orb(int i) => Orbs[i] != null ? Orbs[i] : Orbs[i] = ToSprite(OrbPixels(Hex(RastroBoard.Colors[i]), OrbPx), OrbPx, 100f);
        public static Sprite DottedDisc() => _disc != null ? _disc : _disc = ToSprite(DiscPixels(512), 512, 100f);
        public static Sprite DashedRing() => _dashed != null ? _dashed : _dashed = ToSprite(DashedRingPixels(128), 128, 100f);
        /// <summary>Viñeta: transparente en el centro y teñida hacia los bordes (se estira a toda la pantalla y se tiñe con <c>Image.color</c>): el tinte del modo.</summary>
        public static Sprite Vignette() => _vignette != null ? _vignette : _vignette = ToSprite(VignettePixels(128), 128, 100f);
        public static Sprite XMark() => _cross != null ? _cross : _cross = ToSprite(Bake(128, CrossSdf), 128, 100f);
        public static Sprite Icon(RastroMode m) => Icons[(int)m] != null ? Icons[(int)m] : Icons[(int)m] = ToSprite(Bake(256, IconSdf(m)), 256, 100f);

        // ------------------------------------------------------------------ lucero de cristal

        // Degradé del boceto: círculo chico de luz (centro arriba a la izquierda, radio 0,105) → círculo del lucero (radio 1).
        private const float C0x = -0.316f, C0y = 0.368f, R0 = 0.105f;

        /// <summary>Posición en el degradé de dos círculos (0 = luz, 1 = borde) del punto (x, y), con el lucero de radio 1.</summary>
        public static float GradientT(float x, float y)
        {
            float dcx = -C0x, dcy = -C0y, dr = 1f - R0;
            float dpx = x - C0x, dpy = y - C0y;
            float a = dcx * dcx + dcy * dcy - dr * dr;
            float b = dpx * dcx + dpy * dcy + R0 * dr;
            float c = dpx * dpx + dpy * dpy - R0 * R0;
            // a < 0: la ecuación a·t² − 2b·t + c = 0 tiene una raíz positiva (la del círculo que pasa por el punto)
            float disc = Mathf.Max(0f, b * b - a * c);
            float t = (b - Mathf.Sqrt(disc)) / a;
            return Mathf.Clamp(t, 0f, 1.2f);
        }

        /// <summary>Píxeles del lucero de color <paramref name="hue"/> (fila 0 = abajo). Separado para previsualizar fuera de Unity.</summary>
        public static Color32[] OrbPixels(Color hue, int size)
        {
            var pixels = new Color32[size * size];
            const float margin = 0.94f;           // el lucero ocupa el 94 % del lienzo: aire para el borde y el antialias
            float pxPerUnit = size * 0.5f * margin;
            for (int py = 0; py < size; py++)
            {
                for (int px = 0; px < size; px++)
                {
                    float x = ((px + 0.5f) / size * 2f - 1f) / margin;
                    float y = ((py + 0.5f) / size * 2f - 1f) / margin;
                    float r = Mathf.Sqrt(x * x + y * y);
                    var p = new Px();
                    float cover = Mathf.Clamp01(0.5f - (r - 1f) * pxPerUnit / 1.2f);
                    if (cover > 0f)
                    {
                        float t = GradientT(x, y);
                        Color col;
                        if (t < 0.35f) col = Color.Lerp(Color.white, hue, t / 0.35f);
                        else { col = hue; col.a = Mathf.Lerp(1f, 0.35f, Mathf.Clamp01((t - 0.35f) / 0.65f)); }
                        p.Over(col, cover);
                        // borde blanco fino
                        float ring = Mathf.Clamp01(1f - Mathf.Abs(r - 0.955f) * pxPerUnit / 2.2f);
                        p.Over(new Color(1f, 1f, 1f, 0.55f), ring * cover);
                        // brillo especular
                        float spec = Cover(Ellipse(Rot(x, y, -0.33f, 0.44f, 0.6f, out float ry), ry, 0f, 0f, 0.28f, 0.17f) * pxPerUnit, 1.4f);
                        p.Over(new Color(1f, 1f, 1f, 0.75f), spec * cover);
                    }
                    pixels[py * size + px] = p.ToColor32();
                }
            }
            return pixels;
        }

        /// <summary>x de (x − cx, y − cy) girado <paramref name="ang"/> radianes; <paramref name="ry"/> es la y girada.</summary>
        private static float Rot(float x, float y, float cx, float cy, float ang, out float ry)
        {
            float dx = x - cx, dy = y - cy, c = Mathf.Cos(ang), s = Mathf.Sin(ang);
            ry = dx * s + dy * c;
            return dx * c - dy * s;
        }

        // ------------------------------------------------------------------ disco punteado del tablero

        /// <summary>Anillo de rayitas (3 dp de largo, 9 de hueco, 1,2 de grosor en el boceto) en blanco: se tiñe y se baja el alfa con <c>Image.color</c>.</summary>
        public static Color32[] DiscPixels(int size)
        {
            var pixels = new Color32[size * size];
            float radius = size * 0.495f;
            float thick = size / 300f * 1.2f * 0.5f; // medio grosor en px: el lienzo mide 300 dp
            float period = size / 300f * 12f, dash = size / 300f * 3f;
            for (int py = 0; py < size; py++)
            {
                for (int px = 0; px < size; px++)
                {
                    float x = px + 0.5f - size * 0.5f, y = py + 0.5f - size * 0.5f;
                    float r = Mathf.Sqrt(x * x + y * y);
                    float ring = Mathf.Clamp01(thick + 0.5f - Mathf.Abs(r - radius));
                    float s = (Mathf.Atan2(y, x) + Mathf.PI) * radius;
                    float inDash = Mathf.Clamp01(dash * 0.5f + 0.5f - Mathf.Abs(Mathf.Repeat(s, period) - dash * 0.5f));
                    pixels[py * size + px] = new Color32(255, 255, 255, (byte)(ring * inDash * 255f));
                }
            }
            return pixels;
        }

        /// <summary>Alfa 0 hasta el 45 % del radio y subiendo con el cuadrado hasta 1 en las esquinas (se estira, así que es una elipse).</summary>
        public static Color32[] VignettePixels(int size)
        {
            var pixels = new Color32[size * size];
            for (int py = 0; py < size; py++)
                for (int px = 0; px < size; px++)
                {
                    float x = (px + 0.5f) / size * 2f - 1f, y = (py + 0.5f) / size * 2f - 1f;
                    float d = Mathf.Sqrt(x * x + y * y) / 1.41421356f;
                    float a = Mathf.Clamp01((d - 0.45f) / 0.55f);
                    pixels[py * size + px] = new Color32(255, 255, 255, (byte)(a * a * 255f + 0.5f));
                }
            return pixels;
        }

        /// <summary>Aro de 14 rayitas (la ayuda de la ronda guiada: «el que sigue»), blanco; se tiñe de sol con <c>Image.color</c>.</summary>
        public static Color32[] DashedRingPixels(int size)
        {
            var pixels = new Color32[size * size];
            float radius = size * 0.46f, half = size * 0.035f;
            for (int py = 0; py < size; py++)
                for (int px = 0; px < size; px++)
                {
                    float x = px + 0.5f - size * 0.5f, y = py + 0.5f - size * 0.5f;
                    float ring = Mathf.Clamp01(half + 0.5f - Mathf.Abs(Mathf.Sqrt(x * x + y * y) - radius));
                    float turn = (Mathf.Atan2(y, x) + Mathf.PI) / (Mathf.PI * 2f) * 14f;
                    float dash = Mathf.Clamp01((0.25f - Mathf.Abs(Mathf.Repeat(turn, 1f) - 0.5f)) * radius * 0.45f + 0.5f);
                    pixels[py * size + px] = new Color32(255, 255, 255, (byte)(ring * dash * 255f));
                }
            return pixels;
        }

        // ------------------------------------------------------------------ íconos y cruz (trazos blancos)

        private static float Cap(float x, float y, float ax, float ay, float bx, float by, float r) => Capsule(x, y, ax, ay, bx, by, r);

        private static float CrossSdf(float x, float y) =>
            Mathf.Min(Cap(x, y, -0.52f, -0.52f, 0.52f, 0.52f, 0.12f), Cap(x, y, -0.52f, 0.52f, 0.52f, -0.52f, 0.12f));

        public delegate float Sdf(float x, float y);

        private static Sdf IconSdf(RastroMode m)
        {
            switch (m)
            {
                case RastroMode.Reves: return (x, y) => CircularArrow(x, y, ccw: true);
                case RastroMode.Gira: return (x, y) => CircularArrow(-x, y, ccw: true); // espejo: ↻
                case RastroMode.Marcha: return Chevrons;
                default: return RightArrow;
            }
        }

        private static float RightArrow(float x, float y)
        {
            const float r = 0.09f;
            float d = Cap(x, y, -0.7f, 0f, 0.62f, 0f, r);
            d = Mathf.Min(d, Cap(x, y, 0.68f, 0f, 0.18f, 0.5f, r));
            return Mathf.Min(d, Cap(x, y, 0.68f, 0f, 0.18f, -0.5f, r));
        }

        private static float Chevrons(float x, float y)
        {
            const float r = 0.09f;
            float d = float.MaxValue;
            foreach (float cx in new[] { -0.38f, 0.22f })
            {
                d = Mathf.Min(d, Cap(x, y, cx - 0.22f, 0.5f, cx + 0.3f, 0f, r));
                d = Mathf.Min(d, Cap(x, y, cx + 0.3f, 0f, cx - 0.22f, -0.5f, r));
            }
            return d;
        }

        /// <summary>Flecha circular (⟲): un arco de ~255° con punta en el extremo que gira en contra del reloj.</summary>
        private static float CircularArrow(float x, float y, bool ccw)
        {
            const float rad = 0.58f, w = 0.09f;
            float a0 = 35f * Mathf.Deg2Rad, a1 = 290f * Mathf.Deg2Rad;
            float ang = Mathf.Atan2(y, x);
            if (ang < 0f) ang += Mathf.PI * 2f;
            float dist;
            if (ang >= a0 && ang <= a1) dist = Mathf.Abs(Mathf.Sqrt(x * x + y * y) - rad);
            else
            {
                float ex0 = Mathf.Cos(a0) * rad, ey0 = Mathf.Sin(a0) * rad, ex1 = Mathf.Cos(a1) * rad, ey1 = Mathf.Sin(a1) * rad;
                dist = Mathf.Min(Mathf.Sqrt((x - ex0) * (x - ex0) + (y - ey0) * (y - ey0)), Mathf.Sqrt((x - ex1) * (x - ex1) + (y - ey1) * (y - ey1)));
            }
            float d = dist - w;
            // punta en el extremo final (a1), apuntando en el sentido de giro (tangente antihoraria): dos alas hacia atrás, ±35°
            float tx = -Mathf.Sin(a1), ty = Mathf.Cos(a1);
            float tipX = Mathf.Cos(a1) * rad + tx * 0.08f, tipY = Mathf.Sin(a1) * rad + ty * 0.08f;
            foreach (float side in new[] { 1f, -1f })
            {
                float cs = Mathf.Cos(0.62f * side), sn = Mathf.Sin(0.62f * side);
                float dx = -tx * cs + ty * sn, dy = -tx * sn - ty * cs;
                d = Mathf.Min(d, Cap(x, y, tipX, tipY, tipX + dx * 0.36f, tipY + dy * 0.36f, w));
            }
            return d;
        }

        /// <summary>Hornea un trazo SDF (coordenadas -1..1, fila 0 = abajo) en blanco con antialias.</summary>
        public static Color32[] Bake(int size, Sdf sdf)
        {
            var px = new Color32[size * size];
            float aa = 2.2f / size; // ~1,1 px en unidades de lienzo (de -1 a 1)
            for (int py = 0; py < size; py++)
                for (int pxl = 0; pxl < size; pxl++)
                {
                    float x = (pxl + 0.5f) / size * 2f - 1f, y = (py + 0.5f) / size * 2f - 1f;
                    float a = Cover(sdf(x, y), aa);
                    px[py * size + pxl] = new Color32(255, 255, 255, (byte)(a * 255f + 0.5f));
                }
            return px;
        }
    }
}
