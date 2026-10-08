using System;

namespace NeuroVida.Games.Parejas
{
    /// <summary>
    /// El movimiento de «Constelaciones» (docs/diseno-constelaciones.md §6): el volteo de cada luz sigue su objetivo con un RESORTE amortiguado integrado con el <c>dt</c> real (tope 0,05 s). Abrir es más lento y con un
    /// pequeño rebote (frecuencia 2,6, amortiguación 0,72); cerrar es más rápido y sin rebote (3,6 y 0,95): la salida dura menos que la entrada. Funciones puras con pruebas.
    /// </summary>
    public static class ConstelacionMotion
    {
        public const float MaxDt = 0.05f;
        private const float SubStep = 1f / 90f;

        public const float OpenFreq = 2.6f, OpenZeta = 0.72f, CloseFreq = 3.6f, CloseZeta = 0.95f;
        /// <summary>Al tocar la luz se achica a 0,94 durante 140 ms (la respuesta llega en menos de 150 ms).</summary>
        public const float PressScale = 0.94f, PressSeconds = 0.14f;
        /// <summary>La línea se traza en 380 ms desacelerando; el brillo del final dura 900 ms.</summary>
        public const float TraceSeconds = 0.38f, GlowSeconds = 0.9f;
        /// <summary>Cuánto sube la etiqueta flotante (dp) y cuánto dura (s).</summary>
        public const float FloatRise = 22f, FloatSeconds = 1f;
        /// <summary>Al terminar el cielo las luces se encogen en 360 ms.</summary>
        public const float LeaveSeconds = 0.36f;

        public static float ClampDt(float dt) => dt < 0f ? 0f : Math.Min(MaxDt, dt);

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

        /// <summary>Un paso del volteo de una luz: abre con <see cref="OpenFreq"/> y cierra con <see cref="CloseFreq"/>.</summary>
        public static void Flip(ref float f, ref float vf, float target, float dt)
        {
            bool opening = target > f;
            Spring(ref f, ref vf, target, dt, opening ? OpenFreq : CloseFreq, opening ? OpenZeta : CloseZeta);
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

        /// <summary>El ancho aparente de la luz al voltearse (|cos| con piso 0,02) y cuánto se levanta (hasta 7 %).</summary>
        public static float FlipWidth(float f) => Math.Max(0.02f, Math.Abs((float)Math.Cos(Math.PI * Clamp01(f))));
        public static float FlipLift(float f) => 1f + 0.07f * (float)Math.Sin(Math.PI * Clamp01(f));
    }
}
