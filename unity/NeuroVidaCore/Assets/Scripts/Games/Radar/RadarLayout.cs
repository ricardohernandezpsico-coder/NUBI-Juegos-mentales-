using System;

namespace NeuroVida.Games.Radar
{
    /// <summary>Un rectángulo en dp (x a la derecha, y hacia abajo): una zona prohibida para los avisos (el radar, la nave, el tablero).</summary>
    public readonly struct RadarBox
    {
        public readonly float X0, Y0, X1, Y1;

        public RadarBox(float x0, float y0, float x1, float y1)
        {
            X0 = Math.Min(x0, x1);
            Y0 = Math.Min(y0, y1);
            X1 = Math.Max(x0, x1);
            Y1 = Math.Max(y0, y1);
        }

        public bool Intersects(RadarBox o) => X0 < o.X1 && X1 > o.X0 && Y0 < o.Y1 && Y1 > o.Y0;
        public float Width => X1 - X0;
        public float Height => Y1 - Y0;
    }

    /// <summary>
    /// Dónde va cada cosa en la pantalla de Rescate relámpago (dp lógicos de un campo de 360 de ancho, origen arriba a la izquierda; lógica pura, probada en cuatro formas de teléfono): el marcador común (63 dp), la FRANJA DE MENSAJES (donde salen la espera, la pregunta
    /// y los avisos: NUNCA sobre el radar ni el tablero), el radar (radio de 128 dp), la nave de rescate, el tablero de 6 botones (3 × 2) y «¡Rescatar!». En una pantalla corta (o con los controles del tutorial, que ocupan 96 dp abajo) todo se achica junto con la escala
    /// <see cref="Scale"/> (mínimo 0,75) sin que el texto baje de 14 dp; en una alta, el espacio que sobra se reparte entre las piezas. La escala es solo de PANTALLA: las posiciones y el radio de la zona del destello son los mismos (en dp) en todos los niveles.
    /// </summary>
    public readonly struct RadarPlan
    {
        public const float Width = 360f;
        public const float HudBottom = 190f / 3f;
        public const float TutorialControls = 96f;
        /// <summary>La franja de mensajes: de 80 a 114 dp desde arriba, a todo lo ancho menos 12 dp de cada lado.</summary>
        public const float StripTop = 78f, StripBottom = 114f, StripLineA = 90f, StripLineB = 108f;

        // las medidas con la escala 1 (dp)
        private const float GlassR1 = RadarContract.RadarRadius, BezelR1 = GlassR1 + 13f, ShipH1 = 32f, CellW1 = 104f, CellH1 = 62f, Gap1 = 8f, GoH1 = 44f;
        private const float Fixed = StripBottom + 4f;                       // lo de arriba (marcador y franja): fijo
        private const float Content1 = 2f * BezelR1 + 9f + ShipH1 - 6f + 2f * CellH1 + Gap1 + 10f + GoH1;      // lo que mide todo lo demás con la escala 1 (503)

        public readonly float Height, Scale;
        public readonly float RadarCx, RadarCy, GlassR, BezelR;
        public readonly float ShipY, ShipScale;
        public readonly float BoardX0, BoardY0, CellW, CellH, CellGap;
        public readonly float GoCx, GoCy, GoW, GoH;

        public RadarPlan(float height, bool guided)
        {
            Height = height;
            float avail = height - (guided ? TutorialControls : 0f) - 8f - Fixed;
            float k = Math.Max(0.75f, Math.Min(1f, avail / Content1));
            Scale = k;
            float extra = Math.Max(0f, avail - Content1 * k);
            float g1 = extra * 0.25f, g2 = extra * 0.15f, g3 = extra * 0.30f, g4 = extra * 0.10f;
            GlassR = GlassR1 * k;
            BezelR = BezelR1 * k;
            RadarCx = Width * 0.5f;
            RadarCy = Fixed + g1 + BezelR;
            ShipScale = k;
            ShipY = RadarCy + BezelR + (9f + g2) * k + ShipH1 * k * 0.5f;
            CellW = CellW1 * k;
            CellH = CellH1 * k;
            CellGap = Gap1 * k;
            BoardY0 = ShipY + ShipH1 * k * 0.5f - 6f * k + g3;
            BoardX0 = Width * 0.5f - (3f * CellW + 2f * CellGap) * 0.5f;
            GoW = 172f;
            GoH = GoH1 * k;
            GoCx = Width * 0.5f;
            GoCy = BoardY0 + 2f * CellH + CellGap + (10f + g4) * k + GoH * 0.5f;
        }

        /// <summary>La franja de mensajes.</summary>
        public RadarBox StripBox => new RadarBox(12f, StripTop, Width - 12f, StripBottom);

        /// <summary>El radar con su bisel: zona prohibida para los avisos.</summary>
        public RadarBox RadarBox => new RadarBox(RadarCx - BezelR, RadarCy - BezelR, RadarCx + BezelR, RadarCy + BezelR);

        /// <summary>La nave de rescate (casco de 160 dp de ancho, con los propulsores).</summary>
        public RadarBox ShipBox => new RadarBox(RadarCx - 84f * ShipScale, ShipY - 26f * ShipScale, RadarCx + 84f * ShipScale, ShipY + 26f * ShipScale);

        /// <summary>El tablero (los seis botones) y «¡Rescatar!».</summary>
        public RadarBox BoardBox => new RadarBox(BoardX0, BoardY0, BoardX0 + 3f * CellW + 2f * CellGap, BoardY0 + 2f * CellH + CellGap);

        public RadarBox GoBox => new RadarBox(GoCx - GoW * 0.5f, GoCy - GoH * 0.5f, GoCx + GoW * 0.5f, GoCy + GoH * 0.5f);

        /// <summary>Centro del botón <paramref name="i"/> (0-5) del tablero, de izquierda a derecha y de arriba abajo.</summary>
        public (float x, float y) CellCenter(int i) =>
            (BoardX0 + (i % 3) * (CellW + CellGap) + CellW * 0.5f, BoardY0 + (i / 3) * (CellH + CellGap) + CellH * 0.5f);
    }
}
