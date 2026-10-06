using System;

namespace NeuroVida.Games.Bodega
{
    /// <summary>
    /// Disposición de «Bodega de carga» en dp lógicos (360 de ancho; y desde arriba del área de juego, que incluye el marcador). Es la del boceto aprobado (docs/previews/bodega-de-carga-boceto.html:
    /// la tarjeta de 2 líneas, la bodega redonda con su esclusa de carga y el robot, el carro de reparto y el aviso) adaptada a la altura de cada teléfono: la bodega se achica, entera y pareja, si la
    /// pantalla es baja, y si sobra aire se reparte entre los bloques. Pura, con pruebas. Ningún texto baja de 14 dp y las escotillas se tocan en un círculo de al menos 48 dp.
    /// </summary>
    public static class BodegaLayout
    {
        public const float W = 360f;
        /// <summary>Lo que ocupa el marcador común de los juegos (GameHud, 190 unidades = 63 dp) más un respiro.</summary>
        public const float HudDp = 66f;

        // la bodega, en coordenadas del boceto (360 × 640): centro, radio del anillo de escotillas y radio de cada escotilla (con su marco de 5)
        public const float CenterX = 180f, CenterY = 335f, Ring = 125f, HatchR = 27f, FrameW = 5f;
        public const float HullR = Ring + 44f;
        /// <summary>Radio del toque de cada escotilla en el boceto (un poco más que el dibujo).</summary>
        public const float TouchR = HatchR + 10f;

        public const float CardH = 66f, TrayCircle = 38f, TrayGap = 8f, TrayLabelH = 20f, ToastH = 58f, MinScale = 0.8f, MaxScale = 1f, MinFree = 36f;
        private const float Gap = 4f, TrayBlockH = TrayLabelH + 6f + TrayCircle;

        public struct Metrics
        {
            public float CardTop, BoardCenterY, Scale, TrayLabelY, TrayCenterY, ToastTop, Bottom;
        }

        /// <summary>La altura que pide la pantalla con la bodega a tamaño completo.</summary>
        public static float FullHeight => HudDp + 2f + CardH + Gap + 2f * HullR + 6f + TrayBlockH + 6f + ToastH + 6f;

        public static Metrics Compute(float height)
        {
            var m = new Metrics();
            float fixedH = FullHeight - 2f * HullR;
            float scale = Math.Max(MinScale, Math.Min(MaxScale, (height - fixedH - MinFree) / (2f * HullR)));      // siempre queda un respiro (36 dp) para lo que se pone encima (Nubi y su globo en el tutorial)
            float boardH = 2f * HullR * scale;
            float free = Math.Max(0f, height - fixedH - boardH);
            m.Scale = scale;
            m.CardTop = HudDp + 2f;
            float boardTop = m.CardTop + CardH + Gap + free * 0.4f;
            m.BoardCenterY = boardTop + boardH / 2f;
            float trayTop = boardTop + boardH + 6f + free * 0.3f;
            m.TrayLabelY = trayTop + TrayLabelH / 2f;
            m.TrayCenterY = trayTop + TrayLabelH + 6f + TrayCircle / 2f;
            m.ToastTop = trayTop + TrayBlockH + 6f + free * 0.3f;
            m.Bottom = m.ToastTop + ToastH;
            return m;
        }

        /// <summary>De un punto del boceto (dentro de la bodega) a dp lógicos.</summary>
        public static void BoardToLogical(Metrics m, float bx, float by, out float lx, out float ly)
        {
            lx = W / 2f + (bx - CenterX) * m.Scale;
            ly = m.BoardCenterY + (by - CenterY) * m.Scale;
        }

        /// <summary>De dp lógicos a un punto de la bodega (lo contrario: para saber qué escotilla se tocó).</summary>
        public static void LogicalToBoard(Metrics m, float lx, float ly, out float bx, out float by)
        {
            bx = CenterX + (lx - W / 2f) / m.Scale;
            by = CenterY + (ly - m.BoardCenterY) / m.Scale;
        }

        /// <summary>El centro (en el boceto) del lugar <paramref name="position"/> del anillo de <paramref name="places"/> lugares (la esclusa es el 0), con el anillo girado <paramref name="rot"/> radianes.</summary>
        public static void RingPoint(int position, int places, float rot, float radius, out float x, out float y, out float angle)
        {
            angle = -(float)Math.PI / 2f + position * 2f * (float)Math.PI / places + rot;
            x = CenterX + (float)Math.Cos(angle) * radius;
            y = CenterY + (float)Math.Sin(angle) * radius;
        }

        /// <summary>El centro del carro de reparto: el círculo <paramref name="i"/> de <paramref name="n"/> (dp lógicos; x desde la izquierda).</summary>
        public static float TrayX(int i, int n)
        {
            float w = n * TrayCircle + (n - 1) * TrayGap;
            return W / 2f - w / 2f + i * (TrayCircle + TrayGap) + TrayCircle / 2f;
        }
    }
}
