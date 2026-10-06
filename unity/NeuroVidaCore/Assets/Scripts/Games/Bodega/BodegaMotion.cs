using System;

namespace NeuroVida.Games.Bodega
{
    /// <summary>
    /// El «flow» de «Bodega de carga» (pedido de Ricardo: el robot y las puertas se veían rígidos): TODO lo que se mueve sigue a su objetivo con un RESORTE AMORTIGUADO, integrado con el
    /// <c>dt</c> real (tope 0,05 s), nunca en línea recta ni con velocidad fija. Funciones puras con pruebas; el controlador solo les da objetivo. Ver docs/diseno-bodega-de-carga.md §6.
    /// </summary>
    public static class BodegaMotion
    {
        /// <summary>El <c>dt</c> más grande que se integra de una vez (un tirón del teléfono no hace saltar nada).</summary>
        public const float MaxDt = 0.05f;
        private const float SubStep = 1f / 90f;

        // los resortes del boceto aprobado (frecuencia en Hz, amortiguación)
        public const float RobotFreq = 1.5f, RobotZeta = 0.8f;               // se desliza hacia la escotilla en la que trabaja
        public const float TurnFreq = 2.2f, TurnZeta = 0.75f;                // mira hacia donde trabaja
        public const float AntennaFreq = 3.2f, AntennaZeta = 0.3f;           // la antena se queda atrás y rebota
        public const float SwayFreq = 1.3f, SwayZeta = 0.22f;                // la caja colgando se mece
        public const float DoorOpenFreq = 2.6f, DoorOpenZeta = 0.55f;        // abre y se pasa apenas
        public const float DoorCloseZeta = 0.85f;                            // cierra firme
        public const float MaxLean = 0.22f;                                  // inclinación máxima del robot (rad)
        public const float SquashOnBeam = 0.12f;                             // se aplasta un poco al lanzar el haz
        public const float BeamGrow = 0.26f, BeamFade = 0.22f;               // el haz crece con suavidad y se apaga sin cortarse
        public const float ObjectPopSeconds = 0.42f;                         // el objeto aparece con un rebote (hasta 1,08)
        public const float SpinSeconds = 1.5f;                               // el giro de la bodega
        public const float RobotBobA = 520f, RobotBobB = 1370f;              // las dos ondas de flotación (ms), que no coinciden

        public static float ClampDt(float dt) => dt < 0f ? 0f : Math.Min(MaxDt, dt);

        /// <summary>Un paso de resorte amortiguado (Euler semi-implícito en pasos chicos, así no depende de cuánto dure el cuadro): acelera hacia <paramref name="target"/> con frecuencia
        /// <paramref name="freq"/> (Hz) y amortiguación <paramref name="zeta"/> (1 = sin pasarse).</summary>
        public static void Spring(ref float x, ref float v, float target, float dt, float freq, float zeta)
        {
            dt = ClampDt(dt);
            if (dt <= 0f) return;
            float w = 2f * (float)Math.PI * freq;
            int steps = Math.Max(1, (int)Math.Ceiling(dt / SubStep));
            float h = dt / steps;
            for (int i = 0; i < steps; i++)
            {
                v += (w * w * (target - x) - 2f * zeta * w * v) * h;
                x += v * h;
            }
        }

        /// <summary>La diferencia de ángulos más corta (−π..π): para que el robot gire por el lado corto.</summary>
        public static float AngleDelta(float from, float to)
        {
            double d = (to - from + Math.PI) % (2 * Math.PI);
            if (d < 0) d += 2 * Math.PI;
            return (float)(d - Math.PI);
        }

        public static float Clamp01(float t) => t < 0f ? 0f : t > 1f ? 1f : t;

        public static float EaseOut(float t) => 1f - (float)Math.Pow(1f - Clamp01(t), 3.0);

        /// <summary>Aparición con rebote: sube hasta ~1,08 y vuelve a 1.</summary>
        public static float EaseBack(float t)
        {
            const float c = 1.4f;
            t = Clamp01(t);
            return 1f + (c + 1f) * (float)Math.Pow(t - 1f, 3.0) + c * (float)Math.Pow(t - 1f, 2.0);
        }

        public static float EaseInOut(float t)
        {
            t = Clamp01(t);
            return t < 0.5f ? 4f * t * t * t : 1f - (float)Math.Pow(-2f * t + 2f, 3.0) / 2f;
        }

        /// <summary>El giro de la bodega: un pequeño impulso hacia atrás (3,5 % en el primer 12 %) y después un giro con un asentarse suave al final. Va de 0 a 1.</summary>
        public static float EaseSpin(float t)
        {
            t = Clamp01(t);
            if (t < 0.12f) return -0.035f * (float)Math.Sin(Math.PI * t / 0.12f);
            float u = (t - 0.12f) / 0.88f;
            const float c1 = 1.15f, c3 = c1 + 1f;
            return 1f + c3 * (float)Math.Pow(u - 1f, 3.0) + c1 * (float)Math.Pow(u - 1f, 2.0);
        }

        /// <summary>La flotación del robot: dos ondas que no coinciden (520 y 1370 ms), nunca se ve mecánico. Con «quitar animaciones» no flota.</summary>
        public static float Bob(float timeSeconds, bool decorative) =>
            !decorative ? 0f : (float)(Math.Sin(timeSeconds * 1000.0 / RobotBobA) * 2.2 + Math.Sin(timeSeconds * 1000.0 / RobotBobB) * 1.2);
    }
}
