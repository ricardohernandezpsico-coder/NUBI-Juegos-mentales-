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
    /// Dónde va cada cosa en la pantalla de Acoplamiento (v2, 10-oct; docs/diseno-acoplamiento.md §5): dp lógicos de un campo de 360 de ancho, origen arriba a la izquierda. Lógica pura, probada en cuatro formas de teléfono. La referencia es una pantalla de 360 × 780 dp (20:9):
    /// el marcador común (63 dp), la estación de anillos (base en y 200), la FRANJA DE AVISOS (y 246-278: nunca sobre el módulo, el puerto ni los botones), el módulo (centro y 360, bloques de 26 dp), la barra de tiempo (y 438), el puerto (centro y 534, 168 × 146 dp, con el hueco a la
    /// MISMA escala), los dos botones (y 622, 158 × 76 dp) y la pista (y 734). En pantallas más bajas (16:9, 640 dp; o con el tutorial, que reserva 96 dp abajo) todo lo que va bajo la franja se achica parejo (como mínimo 0,6; el texto nunca baja de 14 dp) y en pantallas más altas lo que
    /// sobra se reparte hacia abajo, al pulgar.
    /// </summary>
    public readonly struct DockingPlan
    {
        public const float Width = 360f;
        /// <summary>Lo que ocupa el marcador común de arriba (190 unidades de 1080 → 63,3 dp).</summary>
        public const float HudBottom = 190f / 3f;
        public const float TutorialControls = 96f;
        public const float StripTop = 246f, StripBottom = 278f, StripCenter = 262f;
        public const float ReferenceHeight = 780f;
        /// <summary>El tamaño de un bloque de la pieza, en dp (el módulo y el hueco, a la misma escala).</summary>
        public const float ReferenceBlock = 26f;
        /// <summary>Cuántos anillos de la estación se dibujan a la vez (los más nuevos; los viejos se van hacia abajo).</summary>
        public const int VisibleRings = 5;

        // la estación (lo de arriba: no se achica)
        public const float StationCx = 180f, StationBase = 200f, StationRx = 128f, StationRy = 30f, StationStep = 24f;

        // la referencia de 780 dp (centros, en dp); lo de abajo de la franja se mide desde su borde (StripBottom)
        private const float RefModuleCy = 360f, RefFuelCy = 444f, RefPortCy = 534f, RefButtonCy = 660f, RefHintCy = 734f, NeededBelowStrip = 741f - StripBottom;

        public readonly float Height, EffectiveHeight, K, Extra;
        public readonly float Block;
        public readonly float ModuleCx, ModuleCy;
        public readonly float FuelCy, FuelW, FuelH;
        public readonly float PortCx, PortCy, PortW, PortH;
        public readonly float ButtonCy, ButtonW, ButtonH, ButtonGap;
        public readonly float HintCy;

        public DockingPlan(float height, bool guided)
        {
            Height = height;
            float eff = height - (guided ? TutorialControls : 0f);
            EffectiveHeight = eff;
            float avail = eff - StripBottom - 6f;
            K = Math.Max(0.6f, Math.Min(1f, avail / NeededBelowStrip));
            Extra = Math.Max(0f, eff - ReferenceHeight);
            Block = ReferenceBlock * K;
            ModuleCx = PortCx = Width * 0.5f;
            ModuleCy = Down(RefModuleCy, 0.30f, K, Extra);
            FuelCy = Down(RefFuelCy, 0.35f, K, Extra);
            FuelW = 200f * K;
            FuelH = 12f * K;
            PortCy = Down(RefPortCy, 0.50f, K, Extra);
            PortW = 168f * K;
            PortH = 146f * K;
            ButtonCy = Down(RefButtonCy, 0.85f, K, Extra);
            ButtonW = 158f * K;
            ButtonH = 76f * K;
            ButtonGap = 12f * K;
            HintCy = Down(RefHintCy, 0.90f, K, Extra);
        }

        private static float Down(float refY, float weight, float k, float extra) => StripBottom + (refY - StripBottom) * k + extra * weight;

        /// <summary>El centro del botón <paramref name="i"/> (0 = «Encaja», izquierda; 1 = «Espejo», derecha).</summary>
        public (float x, float y) ButtonCenter(int i) => (Width * 0.5f + (i == 0 ? -1f : 1f) * (ButtonGap + ButtonW) * 0.5f, ButtonCy);

        /// <summary>La franja de avisos.</summary>
        public DockBox StripBox => new DockBox(12f, StripTop, Width - 12f, StripBottom);

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

        // ------------------------------------------------------------------ la estación

        /// <summary>El centro (y) del anillo visible <paramref name="visibleRing"/> (0 = el de abajo).</summary>
        public static float RingY(int visibleRing) => StationBase - visibleRing * StationStep;

        /// <summary>El casillero <paramref name="slot"/> (0 a 7) de un anillo a la altura <paramref name="ringY"/>: su centro y su profundidad (−1 = atrás, +1 = adelante).</summary>
        public static (float x, float y, float depth) SlotPoint(float ringY, int slot)
        {
            double a = Math.PI * 0.5 + slot * Math.PI * 2.0 / DockingContract.Slots;
            return (StationCx + StationRx * (float)Math.Cos(a), ringY + StationRy * (float)Math.Sin(a), (float)Math.Sin(a));
        }

        /// <summary>
        /// Qué anillos se dibujan y dónde: con <paramref name="docked"/> módulos acoplados, el anillo en curso es <c>docked / 8</c> y se dibujan como mucho <see cref="VisibleRings"/> (los últimos). Devuelve el primer anillo que se dibuja y cuántos son.
        /// </summary>
        public static (int first, int count) VisibleRange(int docked)
        {
            int current = Math.Max(0, docked) / DockingContract.Slots;
            int total = current + 1;
            int count = Math.Min(VisibleRings, total);
            return (total - count, count);
        }
    }
}
