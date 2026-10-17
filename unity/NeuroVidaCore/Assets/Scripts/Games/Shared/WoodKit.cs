using System;
using static NeuroVida.Games.Shared.SoundKit;

namespace NeuroVida.Games.Shared
{
    /// <summary>
    /// Los INSTRUMENTOS de «Madera cálida» y los números que valen para todo el sonido de la app (Tarea 120, Fase B): la kalimba, la marimba, el bloque de madera, el golpe grave, el soplo de ruido y la estática de Rescate relámpago (recetas del laboratorio de sonido que Ricardo aprobó el 9-oct: docs/previews/sonido-laboratorio.html),
    /// la pentatónica de do, la sala pequeña y el VOLUMEN. Vivían en <c>RadarSounds</c>; allá quedan como envolturas (mismas muestras, bit por bit: las pruebas de referencia de Rescate, Acoplamiento y Aterrizaje lo comprueban) y los usan <see cref="WoodSounds"/> (la paleta) y los tres juegos que ya suenan en Madera.
    /// </summary>
    public static class WoodKit
    {
        // ------------------------------------------------------------------ el volumen (UN solo lugar)

        /// <summary>
        /// Cuánto más fuerte que lo que Ricardo aprobó el 10-oct suena todo «Madera cálida»: <b>+3 dB</b> (pidió «un poco más alto» el 17-oct, después de escuchar la página de escucha). Cambiar ESTE número sube o baja a la vez la paleta, lo común (<c>GameFeel</c>) y los sonidos propios de Rescate, Acoplamiento y Aterrizaje; los escalones entre
        /// familias (micro, respuesta, hito, cierre) no cambian porque todo se multiplica por lo mismo. Con 3 dB el pico más alto (el final) queda en −6,9 dBFS ya con el volumen de reproducción (<see cref="PlayVolume"/>); las pruebas exigen ≤ −6 dBFS y que dos sonidos fuertes a la vez no saturen.
        /// </summary>
        public const double MasterGainDb = 3.0;

        /// <summary>La ganancia lineal de <see cref="MasterGainDb"/> (1,41 con 3 dB).</summary>
        public static readonly double MasterGain = Math.Pow(10.0, MasterGainDb / 20.0);

        /// <summary>El volumen con el que se reproduce cada clip (<c>PlayOneShot</c>): el del laboratorio donde Ricardo eligió «Madera cálida» (0,8). No pasa de 1: el aumento va dentro de las muestras (<see cref="ApplyMaster"/>).</summary>
        public const float PlayVolume = 0.8f;

        /// <summary>Aplica <see cref="MasterGain"/> a las muestras ya cerradas (con <c>Finish</c>), en el mismo arreglo. Es el ÚNICO paso que sube el volumen: lo llaman el horneado de la paleta y el de Rescate, Acoplamiento y Aterrizaje justo antes de crear el clip.</summary>
        public static void ApplyMaster(float[] left, float[] right = null)
        {
            float g = (float)MasterGain;
            for (int i = 0; i < left.Length; i++) left[i] *= g;
            if (right != null) for (int i = 0; i < right.Length; i++) right[i] *= g;
        }

        // ------------------------------------------------------------------ la escala y la sala

        /// <summary>La pentatónica de do mayor desde do5, ampliada (la del laboratorio).</summary>
        public static readonly double[] P = { 523.25, 587.33, 659.25, 783.99, 880, 1046.5, 1174.66, 1318.51, 1567.98, 1760, 2093, 2349.32, 2637.02 };

        /// <summary>La sala de «Madera cálida»: pequeña, amortiguada y apenas abierta.</summary>
        public static readonly ReverbSettings Room = new ReverbSettings(0.7, 0.5, 0.18, 0.9, 0.8);

        // ------------------------------------------------------------------ instrumentos (recetas del laboratorio)

        /// <summary>Kalimba: seno con un leve «golpe» de afinación al principio, un armónico metálico (×5,95), la octava y un clic de ruido de 4 ms.</summary>
        public static float[] Kalimba(double f, double tau = 0.45, uint seed = 7)
        {
            var r = new Rng(seed);
            return Render(tau * 3, t =>
            {
                double bend = 1 + 0.012 * Math.Exp(-t / 0.012);
                return Math.Sin(Tau * f * bend * t) * Env(t, 0.002, tau) + 0.28 * Math.Sin(Tau * f * 5.95 * t) * Env(t, 0.001, 0.035)
                     + 0.12 * Math.Sin(Tau * f * 2 * t) * Env(t, 0.002, tau * 0.4) + (t < 0.004 ? (r.Next() * 2 - 1) * 0.3 * (1 - t / 0.004) : 0.0);
            });
        }

        /// <summary>Marimba: el fundamental más dos armónicos que se apagan rápido (×4 y ×9,8).</summary>
        public static float[] Marimba(double f, double tau = 0.3) =>
            Render(tau * 3.2, t => 0.9 * Math.Sin(Tau * f * t) * Env(t, 0.003, tau) + 0.25 * Math.Sin(Tau * f * 4 * t) * Env(t, 0.002, 0.03) + 0.06 * Math.Sin(Tau * f * 9.8 * t) * Env(t, 0.001, 0.012));

        /// <summary>Bloque de madera: un seno corto y su parcial inarmónico (×2,71).</summary>
        public static float[] Woodblock(double f) =>
            Render(0.25, t => Math.Sin(Tau * f * t) * Env(t, 0.001, 0.035) + 0.5 * Math.Sin(Tau * f * 2.71 * t) * Env(t, 0.001, 0.02));

        /// <summary>Ruido filtrado con un barrido de frecuencia exponencial de <paramref name="f0"/> a <paramref name="f1"/>.</summary>
        public static float[] NoiseSweep(double sec, double f0, double f1, FilterType type = FilterType.BandPass, double q = 0.8, double a = 0.002, double tau = 0.08, uint seed = 1)
        {
            var r = new Rng(seed);
            var flt = new Biquad(type, f0, q);
            return Render(sec, t =>
            {
                flt.Set(f0 * Math.Pow(f1 / f0, Math.Min(1.0, t / sec)));
                return flt.Run(r.Next() * 2 - 1) * Env(t, a, tau);
            });
        }

        /// <summary>Estática granulada: ruido de banda con granos al azar (como un sonajero suave).</summary>
        public static float[] StaticNoise(double sec, double center = 1800, double q = 0.7, uint seed = 3, double grain = 0.003, double flutter = 0)
        {
            var r = new Rng(seed);
            var bp = new Biquad(FilterType.BandPass, center, q);
            double g = 0, next = 0;
            return Render(sec, t =>
            {
                if (t >= next)
                {
                    g = r.Next() < 0.35 ? r.Next() : r.Next() * 0.25;
                    next = t + grain * (0.5 + r.Next());
                }
                double fl = flutter != 0 ? 0.6 + 0.4 * Math.Sin(Tau * flutter * t) : 1;
                return bp.Run(r.Next() * 2 - 1) * g * fl * Math.Min(1.0, t / 0.01) * Math.Min(1.0, (sec - t) / 0.08);
            });
        }

        /// <summary>Golpe grave: un seno que baja de <paramref name="f0"/> a <paramref name="f1"/> Hz.</summary>
        public static float[] Thump(double sec, double f0 = 90, double f1 = 50, double tau = 0.09)
        {
            double ph = 0;
            return Render(sec, t =>
            {
                ph += f0 * Math.Pow(f1 / f0, Math.Min(1.0, t / sec)) / Rate;
                return Math.Sin(Tau * ph) * Env(t, 0.002, tau);
            });
        }
    }
}
