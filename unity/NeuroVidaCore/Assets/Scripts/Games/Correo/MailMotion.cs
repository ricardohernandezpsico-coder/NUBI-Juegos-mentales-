using System;

namespace NeuroVida.Games.Correo
{
    /// <summary>Movimiento de la estación de correo (docs/diseno-correo-estacion.md §5 y §11): resortes y suavizados del boceto. Funciones puras con pruebas.</summary>
    public static class MailMotion
    {
        public const float MaxDt = 0.05f;
        private const float SubStep = 1f / 90f;
        /// <summary>El resorte con que las cartas avanzan por la cinta (frecuencia 2,2 y amortiguación 0,82).</summary>
        public const float BeltFreq = 2.2f, BeltZeta = 0.82f;
        /// <summary>El vuelo de una carta al buzón (380 ms) o a la caja fuerte (420 ms), en arco; la caída de una carta atrasada, 600 ms.</summary>
        public const float FlyBoxSeconds = 0.38f, FlySafeSeconds = 0.42f, FallSeconds = 0.6f;
        /// <summary>Lo que dura el momento del faro: la nave entra en 0,5-1,7 s, se detiene hasta 2,4 s, deja caer el saco desde 1,8 s (en 0,4 s) y se va por la derecha.</summary>
        public const float ShowSeconds = 3.4f;

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

        public static float Clamp01(float t) => t < 0f ? 0f : t > 1f ? 1f : t;
        public static float EaseOut(float t) => 1f - (float)Math.Pow(1f - Clamp01(t), 3.0);
        public static float EaseInOut(float t) { t = Clamp01(t); return t < 0.5f ? 4f * t * t * t : 1f - (float)Math.Pow(-2f * t + 2f, 3.0) / 2f; }
        public static float EaseBack(float t)
        {
            const float c = 1.4f;
            t = Clamp01(t);
            return 1f + (c + 1f) * (float)Math.Pow(t - 1f, 3.0) + c * (float)Math.Pow(t - 1f, 2.0);
        }

        // ------------------------------------------------------------------ el faro encendido (docs §11)

        /// <summary>La envolvente de la luz del faro: sube en 0,25 s, se mantiene y baja en los últimos 0,8 s.</summary>
        public static float ShowEnvelope(float t) => Math.Max(0f, Math.Min(1f, Math.Min(t / 0.25f, (ShowSeconds - t) / 0.8f)));

        /// <summary>El ángulo del haz que barre el cielo (−π/2 es hacia arriba en el boceto, con y hacia abajo): oscila ±1,05 rad; con «quitar animaciones», fijo hacia arriba.</summary>
        public static float BeamAngle(float t, bool reduced) => reduced ? -(float)Math.PI / 2f : -(float)Math.PI / 2f + (float)Math.Sin(t * 2.2f) * 1.05f;

        /// <summary>Dónde está la nave del correo a los <paramref name="t"/> s del faro (x, y en dp del escenario de 360 de ancho, y hacia abajo; <paramref name="baseY"/> es la altura de la entrada): entra por la izquierda
        /// siguiendo la luz, se detiene sobre la cinta y se va por la derecha. Con «quitar animaciones» queda quieta sobre la cinta.</summary>
        public static void ShipAt(float t, bool reduced, float baseY, out float x, out float y)
        {
            if (reduced) { x = 180f; y = baseY + 30f; return; }
            if (t < 0.5f) { x = -80f; y = baseY; }
            else if (t < 1.7f) { float e = EaseOut((t - 0.5f) / 1.2f); x = -60f + (180f + 60f) * e; y = baseY + 30f * e - (float)Math.Sin(Math.PI * e) * 30f; }
            else if (t < 2.4f) { x = 180f; y = baseY + 30f + (float)Math.Sin((t - 1.7f) * 6f) * 2f; }
            else { float e = (t - 2.4f) / 1.0f; x = 180f + (180f + 90f) * e * e; y = baseY + 30f - 60f * e * e; }
        }

        /// <summary>El saco dorado que cae de la nave (desde 1,8 s en 0,4 s): 0 = todavía no, 1 = ya cayó (estalla en chispas).</summary>
        public static float SackDrop(float t) => t <= 1.8f ? 0f : Math.Min(1f, (t - 1.8f) / 0.4f);
    }
}
