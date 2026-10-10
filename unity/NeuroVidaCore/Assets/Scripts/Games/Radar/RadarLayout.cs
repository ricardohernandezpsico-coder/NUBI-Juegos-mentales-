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
    /// y los avisos: NUNCA sobre el radar, la nave ni el tablero), el radar, la nave de rescate, el tablero de 6 botones (3 × 2) y «¡Rescatar!».
    /// V4 (Tarea 65, 9-oct): la referencia es una pantalla de 360 × 780 dp (20:9) donde el juego usa TODO el alto (radar de 146 dp de radio, nave protagonista de 200 dp de ancho, tablero de botones de 74 dp de alto en la zona del pulgar). En 16:9 (640 dp) queda el layout de la
    /// Tarea 62 (radar de 128 dp, tablero de 62 dp de alto); entre los dos (ya descontados los 96 dp que el tutorial reserva abajo) se interpola: la v4 entra de a poco desde que su escala iguala el radar de 128 dp (unos 690 dp) hasta que mide 1 (unos 770 dp), así que el radar nunca se achica al crecer
    /// la pantalla; por encima de 780 el espacio que sobra se reparte hacia abajo (el tablero y «¡Rescatar!» bajan al pulgar). Con pantallas bajas todo se achica parejo sin que el texto baje de 14 dp. La pantalla NO depende del nivel: el radio de la zona del destello es el mismo (<see cref="RadarGeometry"/>) en los 12 niveles.
    /// </summary>
    public readonly struct RadarPlan
    {
        public const float Width = 360f;
        public const float HudBottom = 190f / 3f;
        public const float TutorialControls = 96f;
        /// <summary>La franja de mensajes: de 78 a 114 dp desde arriba, a todo lo ancho menos 12 dp de cada lado.</summary>
        public const float StripTop = 78f, StripBottom = 114f, StripLineA = 90f, StripLineB = 108f;
        /// <summary>El alto del layout compacto (16:9, el de la Tarea 62) y el de la referencia (20:9, v4).</summary>
        public const float ShortHeight = 640f, TallHeight = 780f;

        // la v4, medida con la referencia de 780 dp (diseno-rescate-v4.md §1)
        private const float TopFixed = StripBottom + 4f;
        private const float V4GlassR = 146f, V4RadarCy = 290f, V4ShipY = 494f, V4CellH = 74f, V4Gap = 8f, V4BoardY0 = 552f, V4GoCy = 740f, V4GoH = 44f, V4Bottom = 762f;
        /// <summary>El botón del tablero: 104 × 74 dp con la referencia (el ancho sale del alto).</summary>
        public const float CellAspect = 104f / 74f;

        // la Tarea 62 (16:9)
        private const float GlassR1 = RadarContract.RadarRadius, BezelR1 = GlassR1 + 13f, ShipH1 = 32f, CellH1 = 62f, Gap1 = 8f, GoH1 = 44f;
        private const float Fixed = StripBottom + 4f;                       // lo de arriba (marcador y franja): fijo
        private const float Content1 = 2f * BezelR1 + 9f + ShipH1 - 6f + 2f * CellH1 + Gap1 + 10f + GoH1;      // lo que mide todo lo demás con la escala 1 (503)

        private struct Parts
        {
            public float RadarCy, GlassR, BezelR, ShipY, ShipScale, CellH, CellGap, BoardY0, GoCy, GoH, GoW;
        }

        public readonly float Height, EffectiveHeight, T;
        /// <summary>El radar con relación al de referencia de 128 dp (el de la v3).</summary>
        public readonly float Scale;
        public readonly float RadarCx, RadarCy, GlassR, BezelR;
        public readonly float ShipY, ShipScale;
        public readonly float BoardX0, BoardY0, CellW, CellH, CellGap;
        public readonly float GoCx, GoCy, GoW, GoH;

        public RadarPlan(float height, bool guided)
        {
            Height = height;
            float eff = height - (guided ? TutorialControls : 0f);
            EffectiveHeight = eff;
            float sMin = GlassR1 / V4GlassR;                                    // con esta escala el radar de la v4 mide lo mismo que el de 16:9 (128 dp)
            T = Math.Max(0f, Math.Min(1f, (TallScale(eff) - sMin) / (1f - sMin)));      // la v4 entra de a poco, SIN achicar nunca el radar por debajo de 128 dp
            Parts a = Compact(height, guided), b = Tall(eff);
            GlassR = Lerp(a.GlassR, b.GlassR, T);
            BezelR = Lerp(a.BezelR, b.BezelR, T);
            Scale = GlassR / GlassR1;
            RadarCx = Width * 0.5f;
            RadarCy = Lerp(a.RadarCy, b.RadarCy, T);
            ShipY = Lerp(a.ShipY, b.ShipY, T);
            ShipScale = Lerp(a.ShipScale, b.ShipScale, T);
            CellH = Lerp(a.CellH, b.CellH, T);
            CellW = CellH * CellAspect;
            CellGap = Lerp(a.CellGap, b.CellGap, T);
            BoardY0 = Lerp(a.BoardY0, b.BoardY0, T);
            BoardX0 = Width * 0.5f - (3f * CellW + 2f * CellGap) * 0.5f;
            GoCx = Width * 0.5f;
            GoCy = Lerp(a.GoCy, b.GoCy, T);
            GoW = Lerp(a.GoW, b.GoW, T);
            GoH = Lerp(a.GoH, b.GoH, T);
        }

        private static float Lerp(float a, float b, float t) => a + (b - a) * t;

        /// <summary>El layout de la Tarea 62: todo se achica parejo (mínimo 0,75) en pantallas cortas y el espacio que sobra se reparte entre las piezas. La nave nueva (200 dp de ancho) se dibuja a la mitad para caber en la franja de 40 dp que deja.</summary>
        private static Parts Compact(float height, bool guided)
        {
            float avail = height - (guided ? TutorialControls : 0f) - 8f - Fixed;
            float k = Math.Max(0.75f, Math.Min(1f, avail / Content1));
            float extra = Math.Max(0f, avail - Content1 * k);
            float g1 = extra * 0.25f, g2 = extra * 0.15f, g3 = extra * 0.30f, g4 = extra * 0.10f;
            var p = new Parts
            {
                GlassR = GlassR1 * k,
                BezelR = BezelR1 * k,
                ShipScale = 0.5f * k,
                CellH = CellH1 * k,
                CellGap = Gap1 * k,
                GoW = 172f,
                GoH = GoH1 * k,
            };
            p.RadarCy = Fixed + g1 + p.BezelR;
            p.ShipY = p.RadarCy + p.BezelR + (9f + g2) * k + ShipH1 * k * 0.5f;
            p.BoardY0 = p.ShipY + ShipH1 * k * 0.5f - 6f * k + g3;
            p.GoCy = p.BoardY0 + 2f * p.CellH + p.CellGap + (10f + g4) * k + p.GoH * 0.5f;
            return p;
        }

        /// <summary>El layout v4: la referencia de 780 dp escalada parejo a lo que cabe (como mucho 1) y, si sobra alto, repartido hacia abajo.</summary>
        private static float TallScale(float eff) => Math.Max(0.6f, Math.Min(1f, (eff - (TopFixed + 4f)) / (V4Bottom - TopFixed)));

        private static Parts Tall(float eff)
        {
            float s = TallScale(eff);
            float Down(float y) => TopFixed + (y - TopFixed) * s;
            var p = new Parts
            {
                GlassR = V4GlassR * s,
                BezelR = V4GlassR * (BezelR1 / GlassR1) * s,                 // el sprite del radar guarda siempre la misma proporción bisel/vidrio (141/128): así la pantalla mide justo 146 dp
                ShipScale = s,
                CellH = V4CellH * s,
                CellGap = V4Gap * s,
                GoW = 172f * Math.Max(0.9f, s),
                GoH = V4GoH * s,
                RadarCy = Down(V4RadarCy),
                ShipY = Down(V4ShipY),
                BoardY0 = Down(V4BoardY0),
                GoCy = Down(V4GoCy),
            };
            float extra = Math.Max(0f, eff - TallHeight);
            p.RadarCy += extra * 0.15f;
            p.ShipY += extra * 0.40f;
            p.BoardY0 += extra * 0.75f;
            p.GoCy += extra * 0.90f;
            return p;
        }

        /// <summary>true con las pantallas altas (la referencia de 20:9): ahí entra la línea «N de 10 a bordo» bajo la nave; en la compacta no hay lugar.</summary>
        public bool ShowShipCount => T >= 0.5f;

        /// <summary>La franja de mensajes.</summary>
        public RadarBox StripBox => new RadarBox(12f, StripTop, Width - 12f, StripBottom);

        /// <summary>El radar con su bisel: zona prohibida para los avisos.</summary>
        public RadarBox RadarBox => new RadarBox(RadarCx - BezelR, RadarCy - BezelR, RadarCx + BezelR, RadarCy + BezelR);

        /// <summary>La nave de rescate (casco de 200 dp con aletas y propulsores, a su escala), con la cabina arriba y las llamas abajo.</summary>
        public RadarBox ShipBox => new RadarBox(RadarCx - 114f * ShipScale, ShipY - 40f * ShipScale, RadarCx + 114f * ShipScale, ShowShipCount ? ShipCountY + 11f : ShipY + 20f * ShipScale);

        /// <summary>Donde va el texto «N de 10 a bordo» (centro, dp): 42 dp bajo el centro de la nave.</summary>
        public float ShipCountY => ShipY + 42f * ShipScale;

        /// <summary>El tablero (los seis botones) y «¡Rescatar!».</summary>
        public RadarBox BoardBox => new RadarBox(BoardX0, BoardY0, BoardX0 + 3f * CellW + 2f * CellGap, BoardY0 + 2f * CellH + CellGap);

        public RadarBox GoBox => new RadarBox(GoCx - GoW * 0.5f, GoCy - GoH * 0.5f, GoCx + GoW * 0.5f, GoCy + GoH * 0.5f);

        /// <summary>Centro del botón <paramref name="i"/> (0-5) del tablero, de izquierda a derecha y de arriba abajo.</summary>
        public (float x, float y) CellCenter(int i) =>
            (BoardX0 + (i % 3) * (CellW + CellGap) + CellW * 0.5f, BoardY0 + (i / 3) * (CellH + CellGap) + CellH * 0.5f);
    }
}
