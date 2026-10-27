using System;

namespace NeuroVida.Games.Acoplamiento
{
    /// <summary>Un rectángulo en dp (x a la derecha, y hacia abajo): una zona prohibida para los avisos (el módulo, el puerto, los botones).</summary>
    public readonly struct DockBox
    {
        public readonly float X0, Y0, X1, Y1;

        public DockBox(float x0, float y0, float x1, float y1)
        {
            X0 = Math.Min(x0, x1);
            Y0 = Math.Min(y0, y1);
            X1 = Math.Max(x0, x1);
            Y1 = Math.Max(y0, y1);
        }

        public bool Intersects(DockBox o) => X0 < o.X1 && X1 > o.X0 && Y0 < o.Y1 && Y1 > o.Y0;
        public float Width => X1 - X0;
        public float Height => Y1 - Y0;
    }

    /// <summary>
    /// Dónde va cada cosa en la pantalla de Acoplamiento (v3, 17-oct; docs/diseno-acoplamiento.md §5 y docs/previews/plasticina/propuesta-acoplamiento-v3.png): dp lógicos de un campo de 360 de ancho, origen arriba a la izquierda. Lógica pura, probada en ocho formas de teléfono.
    /// SIN la estación de anillos: la pieza y el puerto usan su lugar. La referencia es una pantalla de 360 × 780 dp (20:9): el marcador común (63 dp), la FILA DE 8 LUCES de avance (y 100), la FRANJA DE AVISOS (y 118-150: nunca sobre el módulo, el puerto ni los botones), el módulo (bloques de 38 dp; gira
    /// dentro de un círculo de 215 dp), la barra de tiempo, el puerto (245 × 214 dp, con el hueco a la MISMA escala), los dos botones (158 × 76 dp) y la pista. En pantallas más bajas (16:9, 640 dp; o con el tutorial, que reserva 96 dp abajo) todo lo que va bajo la franja se achica parejo (como mínimo 0,6; el
    /// texto nunca baja de 14 dp y los botones nunca de 56 dp) y en pantallas más altas lo que sobra se reparte hacia abajo, al pulgar.
    /// </summary>
    public readonly struct DockingPlan
    {
        public const float Width = 360f;
        /// <summary>Lo que ocupa el marcador común de arriba (190 unidades de 1080 → 63,3 dp).</summary>
        public const float HudBottom = 190f / 3f;
        /// <summary>Lo que se reserva ABAJO en la ronda guiada (dp) para Nubi y su globo; «Saltar tutorial» ya no va ahí sino en la fila fija de arriba (Tarea 127), así que bajó de 96 a 72 y, en la Tarea 128, a 60: con la fila fija de arriba la escala bajaba y los botones de la práctica medían 46 dp en 16:9; con 60 miden 48 o más (el globo de Nubi se acomoda donde no tapa).</summary>
        public const float TutorialControls = 60f;
        public const float StripTop = 118f, StripBottom = 150f;
        public const float ReferenceHeight = 780f;
        /// <summary>El tamaño de un bloque de la pieza, en dp (el módulo y el hueco, a la misma escala): 38 en la referencia (hoy: 26; 31 en 16:9).</summary>
        public const float ReferenceBlock = 38f;

        // la fila de luces de avance: 8 luces de 22 dp (su sprite mide 30) cada 30 dp, centradas, a y 100; la medalla de los anillos va a la derecha
        public const int Lights = 8;
        public const float LightsCy = 100f, LightsStep = 30f, LightSprite = 30f, MedalSprite = 40f, MedalDx = 4.7f * LightsStep;

        /// <summary>Lo que va bajo la franja en la referencia (de y 150 a y 737: el módulo, la barra, el puerto, los botones y la pista).</summary>
        private const float NeededBelowStrip = 737f - StripBottom;

        public readonly float Height, EffectiveHeight, K, Extra;
        /// <summary>En la ronda guiada, lo que baja TODO (la fila de luces, la franja y lo de abajo) para dejar la fila fija del tutorial bajo el marcador (<c>GuidedTutorial.RowInsetDp</c>); 0 en la partida.</summary>
        public readonly float Dy;
        public readonly float Block;
        public readonly float ModuleCx, ModuleCy;
        public readonly float FuelCy, FuelW, FuelH;
        public readonly float PortCx, PortCy, PortW, PortH;
        public readonly float ButtonCy, ButtonW, ButtonH, ButtonGap;
        public readonly float HintCy;

        public DockingPlan(float height, bool guided)
        {
            Height = height;
            Dy = guided ? NeuroVida.Games.Shared.GuidedTutorial.RowInsetDp : 0f;
            float eff = height - (guided ? TutorialControls : 0f);
            EffectiveHeight = eff;
            float stripBottom = StripBottom + Dy;
            float avail = eff - stripBottom - 6f;
            K = Math.Max(0.6f, Math.Min(1f, avail / NeededBelowStrip));
            Extra = Math.Max(0f, eff - Dy - ReferenceHeight);
            Block = ReferenceBlock * K;
            ModuleCx = PortCx = Width * 0.5f;
            float r = (float)Math.Sqrt(32.0) * 0.5f * Block;
            ModuleCy = stripBottom + 8f * K + r + Extra * 0.20f;
            FuelCy = ModuleCy + r + 12f * K + Extra * 0.08f;
            FuelW = 200f * K;
            FuelH = 12f * K;
            PortW = 6.46f * Block;
            PortH = 5.62f * Block;
            PortCy = FuelCy + 14f * K + PortH * 0.5f + Extra * 0.20f;
            ButtonW = 158f * K;
            ButtonH = 76f * K;
            ButtonGap = 12f * K;
            ButtonCy = PortCy + PortH * 0.5f + 14f * K + ButtonH * 0.5f + Extra * 0.30f;
            HintCy = ButtonCy + ButtonH * 0.5f + 20f * K + Extra * 0.20f;
        }

        /// <summary>El centro del hueco: 3 dp (a la escala de la pantalla) más arriba que el centro del panel, para dejar lugar al rótulo «Puerto» y a los chevrones.</summary>
        public float HoleCy => PortCy - 3f * K;

        /// <summary>El centro del botón <paramref name="i"/> (0 = «Encaja», izquierda; 1 = «Espejo», derecha).</summary>
        public (float x, float y) ButtonCenter(int i) => (Width * 0.5f + (i == 0 ? -1f : 1f) * (ButtonGap + ButtonW) * 0.5f, ButtonCy);

        /// <summary>El centro (y) de la fila de luces y de la franja de avisos de ESTA pantalla (con la bajada de la ronda guiada).</summary>
        public float LightsY => LightsCy + Dy;

        /// <summary>La franja de avisos.</summary>
        public DockBox StripBox => new DockBox(12f, StripTop + Dy, Width - 12f, StripBottom + Dy);

        /// <summary>La fila de luces de avance (con la medalla de los anillos a su derecha).</summary>
        public DockBox LightsBox => new DockBox(Width * 0.5f - 4f * LightsStep, LightsY - LightSprite * 0.5f, Width * 0.5f + MedalDx + MedalSprite * 0.5f, LightsY + LightSprite * 0.5f);

        /// <summary>El módulo cuando espera, con todos sus giros: el círculo que lo envuelve (una pieza cabe en 4 × 4 bloques).</summary>
        public DockBox ModuleBox
        {
            get
            {
                float r = (float)Math.Sqrt(32.0) * 0.5f * Block;
                return new DockBox(ModuleCx - r, ModuleCy - r, ModuleCx + r, ModuleCy + r);
            }
        }

        public DockBox PortBox => new DockBox(PortCx - PortW * 0.5f, PortCy - PortH * 0.5f, PortCx + PortW * 0.5f, PortCy + PortH * 0.5f);

        public DockBox ButtonBox(int i)
        {
            var (cx, cy) = ButtonCenter(i);
            return new DockBox(cx - ButtonW * 0.5f, cy - ButtonH * 0.5f, cx + ButtonW * 0.5f, cy + ButtonH * 0.5f);
        }

        /// <summary>Los dos botones juntos.</summary>
        public DockBox ButtonsBox => new DockBox(ButtonBox(0).X0, ButtonBox(0).Y0, ButtonBox(1).X1, ButtonBox(1).Y1);

        /// <summary>La barra de tiempo (con su rótulo a la izquierda).</summary>
        public DockBox FuelBox => new DockBox(Width * 0.5f - FuelW * 0.5f - 60f, FuelCy - FuelH * 0.5f, Width * 0.5f + FuelW * 0.5f, FuelCy + FuelH * 0.5f);

        /// <summary>La pista de los primeros módulos (una línea de 14 dp).</summary>
        public DockBox HintBox => new DockBox(12f, HintCy - 8f, Width - 12f, HintCy + 8f);

        /// <summary>El centro (x) de la luz <paramref name="i"/> (0 a 7) de la fila de avance.</summary>
        public static float LightX(int i) => Width * 0.5f + (i - 3.5f) * LightsStep;

        // ------------------------------------------------------------------ la pantalla final: «tu estación» como escultura

        /// <summary>La escultura: núcleo central y hasta 3 anillos de 8 piezas (los más nuevos), de abajo hacia arriba. Centros de los anillos (y, en dp), radios de la elipse y bloque de las piezas chicas.</summary>
        public const int SculptureRings = 3;
        public const float SculptureCx = Width * 0.5f, SculptureRx = 104f, SculptureRy = 20f, SculptureBlock = 10f;
        public static readonly float[] SculptureRingY = { 219f, 179f, 139f };

        /// <summary>El casillero <paramref name="slot"/> (0 a 7) de un anillo a la altura <paramref name="ringY"/>: su centro y su profundidad (−1 = atrás, +1 = adelante).</summary>
        public static (float x, float y, float depth) SlotPoint(float ringY, int slot)
        {
            double a = Math.PI * 0.5 + slot * Math.PI * 2.0 / DockingContract.Slots;
            return (SculptureCx + SculptureRx * (float)Math.Cos(a), ringY + SculptureRy * (float)Math.Sin(a), (float)Math.Sin(a));
        }

        /// <summary>true si el casillero va delante del núcleo (los costados cuentan como delante: así el orden de dibujo no depende del ruido de una suma de flotantes).</summary>
        public static bool IsFront(int slot) => SlotPoint(0f, slot).depth > -0.01f;

        /// <summary>
        /// Qué anillos se dibujan en la escultura: con <paramref name="docked"/> módulos acoplados, el anillo en curso es <c>docked / 8</c> y se dibujan como mucho <see cref="SculptureRings"/> (los últimos). Devuelve el primer anillo que se dibuja y cuántos son.
        /// </summary>
        public static (int first, int count) VisibleRange(int docked)
        {
            int current = Math.Max(0, docked) / DockingContract.Slots;
            int total = current + (Math.Max(0, docked) % DockingContract.Slots > 0 ? 1 : 0);
            total = Math.Max(1, total);
            int count = Math.Min(SculptureRings, total);
            return (total - count, count);
        }
    }
}
