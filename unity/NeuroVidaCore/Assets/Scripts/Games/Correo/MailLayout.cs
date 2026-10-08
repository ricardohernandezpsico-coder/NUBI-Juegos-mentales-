using System;

namespace NeuroVida.Games.Correo
{
    /// <summary>
    /// La disposición de la estación en la pantalla (docs/diseno-correo-estacion.md §5; es la del boceto de 360×640 dp con el marcador de arriba): el encabezado con el reloj tapado, la franja de avisos (radio, saco,
    /// barra del día), la cinta, los buzones, la caja fuerte y el faro y la frase de abajo. Todo en dp lógicos (x hacia la derecha, y hacia abajo, ancho 360), puro y con pruebas: ninguna pieza se encima, ningún toque
    /// mide menos de 56 dp y todo cabe de 592 dp de alto hacia arriba.
    /// </summary>
    public static class MailLayout
    {
        public const float W = 360f, HudDp = 66f;
        /// <summary>La carta de la cinta (112×76) y su marco punteado (132×92); la carta que espera se queda a 150 dp de la izquierda.</summary>
        public const float LetterW = 112f, LetterH = 76f, FrameW = 132f, FrameH = 92f, BeltXA = 150f, BeltBandH = 100f;
        public const float BoxH = 112f, BtnH = 88f, BtnW = 158f, ClockR = 28f;
        /// <summary>De la altura mínima de la pantalla (incluye el marcador) en que todo cabe sin apretarse.</summary>
        public const float MinHeight = 592f;

        public struct Rect2
        {
            public float X, Y, W, H;
            public float Cx => X + W / 2f;
            public float Cy => Y + H / 2f;
            public Rect2(float x, float y, float w, float h) { X = x; Y = y; W = w; H = h; }
            public bool Contains(float px, float py) => px >= X && px <= X + W && py >= Y && py <= Y + H;
        }

        public struct Metrics
        {
            public float ClockCx, ClockCy, LabelY, HeaderY1;
            /// <summary>La franja de avisos (radio, saco, barra del día).</summary>
            public Rect2 Banner;
            public float BeltY, BoxY0, BtnY0, HintY, Bottom;
            public Rect2 Safe, Beacon;
        }

        public static Metrics Compute(float height)
        {
            var m = new Metrics();
            float s0 = HudDp + 4f;                              // debajo del marcador
            float extra = Math.Max(0f, height - MinHeight);       // el aire que sobra se reparte entre el cielo (donde pasa la nave), la cinta y los botones
            m.ClockCx = W - 44f;
            m.ClockCy = s0 + 30f;
            m.LabelY = m.ClockCy + ClockR + 14f;
            m.HeaderY1 = s0 + 14f;
            float beltTop = s0 + 126f + Math.Min(extra * 0.45f, 80f);
            m.Banner = new Rect2(14f, beltTop - 78f, W - 28f, 70f);
            m.BeltY = beltTop + BeltBandH / 2f;
            m.BoxY0 = beltTop + BeltBandH + 26f + Math.Min(extra * 0.15f, 16f);
            m.BtnY0 = m.BoxY0 + BoxH + 30f + Math.Min(extra * 0.15f, 16f);
            m.Safe = new Rect2(16f, m.BtnY0, BtnW, BtnH);
            m.Beacon = new Rect2(186f, m.BtnY0, BtnW, BtnH);
            m.HintY = m.BtnY0 + BtnH + 26f;
            m.Bottom = m.HintY + 14f;
            return m;
        }

        /// <summary>El buzón <paramref name="i"/> de <paramref name="n"/> (2 a 4): 150, 102 o 78 dp de ancho con 10 de aire, centrados.</summary>
        public static Rect2 BoxRect(int i, int n, float y0)
        {
            float gap = 10f, w = n == 2 ? 150f : n == 3 ? 102f : 78f, tot = n * w + (n - 1) * gap;
            return new Rect2(W / 2f - tot / 2f + i * (w + gap), y0, w, BoxH);
        }

        /// <summary>El lugar (x) de la carta número <paramref name="slot"/> de la cinta (0 = la que espera): las demás van detrás, más chicas.</summary>
        public static float SlotX(int slot) => slot == 0 ? BeltXA : BeltXA + 110f + (slot - 1) * 70f;
    }
}
