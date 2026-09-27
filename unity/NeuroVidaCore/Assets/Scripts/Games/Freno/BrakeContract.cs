using System;
using System.Collections.Generic;

namespace NeuroVida.Games.Freno
{
    /// <summary>
    /// Reglas puras de "Freno de Emergencia": tarea de SEÑAL DE ALTO (stop-signal; Logan y Cowan, 1984), siguiendo la
    /// guía de consenso de Verbruggen et al. (eLife, 2019). Tarea de ir: lanzar el cohete que se enciende (elección entre
    /// 2 a 4 plataformas, para que no se pueda responder "a ciegas"). En ~30% de los lanzamientos aparece "¡ALTO!" un
    /// instante después de la señal de salida (el retraso, SSD) y hay que no tocar. El SSD sigue una escalera: si frena,
    /// el alto llega 50 ms más tarde (más difícil); si no, 50 ms antes. Así se acerca al 50% de frenos, el punto en que
    /// se puede estimar el tiempo de frenado (SSRT) por el método de integración.
    /// La dificultad común (DDA) solo mueve la tarea de ir: más plataformas y menos tiempo para lanzar.
    /// Sin dependencias de UnityEngine: testeable con NUnit.
    /// </summary>
    public static class BrakeContract
    {
        public const string GameId = "freno";
        public const int MaxLevel = 12;

        /// <summary>Reto: 2 minutos de lanzamientos.</summary>
        public const int RetoSeconds = 120;
        /// <summary>Precisión (sin reloj): cantidad de lanzamientos.</summary>
        public const int PrecisionTrials = 40;

        /// <summary>Proporción de lanzamientos con alto (la guía recomienda ~25-30%).</summary>
        public const float StopChance = 0.3f;
        /// <summary>Los primeros lanzamientos son siempre de ir (para aprender la tarea antes del primer alto).</summary>
        public const int WarmupGoTrials = 3;

        public const int SsdStartMs = 250;
        public const int SsdStepMs = 50;
        public const int SsdMinMs = 50;

        // ------------------------------------------------------------------ dificultad (tarea de ir)

        /// <summary>Plataformas entre las que se elige: 2 (niveles 1-4), 3 (5-8), 4 (9-12).</summary>
        public static int Pads(int level)
        {
            int l = Clamp(level);
            return l <= 4 ? 2 : l <= 8 ? 3 : 4;
        }

        /// <summary>Tiempo máximo para lanzar (ms): 1400 en el nivel 1 → 800 en el 12. Precisión da 300 ms más.</summary>
        public static int DeadlineMs(int level, bool precision)
        {
            int ms = 1400 - (int)Math.Round(600.0 * (Clamp(level) - 1) / (MaxLevel - 1));
            return precision ? ms + 300 : ms;
        }

        /// <summary>El alto nunca llega tan tarde que no quede tiempo para frenar.</summary>
        public static int SsdMaxMs(int level, bool precision) => DeadlineMs(level, precision) - 150;

        /// <summary>Espera antes de la señal de salida (variable: que no se pueda anticipar).</summary>
        public static int ForeperiodMs(Random rng) => 600 + rng.Next(601);

        /// <summary>¿El próximo lanzamiento lleva alto? Nunca en el calentamiento ni más de 2 seguidos.</summary>
        public static bool NextIsStop(int trialIndex, int stopsInARow, Random rng)
        {
            if (trialIndex < WarmupGoTrials || stopsInARow >= 2) return false;
            return rng.NextDouble() < StopChance;
        }

        /// <summary>Escalera del alto: frenó → +50 ms (más difícil); no frenó → −50 ms.</summary>
        public static int NextSsd(int ssdMs, bool stopped, int level, bool precision) =>
            Math.Max(SsdMinMs, Math.Min(SsdMaxMs(level, precision), ssdMs + (stopped ? SsdStepMs : -SsdStepMs)));

        // ------------------------------------------------------------------ medidas

        /// <summary>
        /// Tiempo de frenado (SSRT, ms) por el método de integración con reemplazo de omisiones (Verbruggen et al.,
        /// 2019): se ordenan los tiempos de TODOS los lanzamientos de ir (las omisiones cuentan como el tiempo máximo),
        /// se toma el que está en la posición p(responder | alto) y se le resta el retraso medio del alto.
        /// -1 si hay menos de 6 altos o si p(responder | alto) queda fuera de 0,25-0,75: el consenso recomienda no
        /// estimar el SSRT individual en ese caso (Verbruggen et al., 2019), porque la estimación deja de ser confiable.
        /// </summary>
        /// <param name="goRtsMs">Tiempos de los lanzamientos de ir (omisiones = tiempo máximo).</param>
        /// <param name="stopSsdsMs">Retraso del alto en cada ensayo con alto.</param>
        /// <param name="stopResponded">Si se tocó en cada ensayo con alto (no frenó).</param>
        public static int Ssrt(IReadOnlyList<float> goRtsMs, IReadOnlyList<int> stopSsdsMs, IReadOnlyList<bool> stopResponded)
        {
            if (goRtsMs == null || stopSsdsMs == null || stopResponded == null) return -1;
            int stops = Math.Min(stopSsdsMs.Count, stopResponded.Count);
            if (stops < 6 || goRtsMs.Count < 6) return -1;
            int responded = 0;
            double ssdSum = 0;
            for (int i = 0; i < stops; i++)
            {
                if (stopResponded[i]) responded++;
                ssdSum += stopSsdsMs[i];
            }
            double p = (double)responded / stops;
            if (p < 0.25 || p > 0.75) return -1;
            var sorted = new List<float>(goRtsMs);
            sorted.Sort();
            int n = (int)Math.Ceiling(p * sorted.Count);
            n = Math.Max(1, Math.Min(sorted.Count, n));
            double ssrt = sorted[n - 1] - ssdSum / stops;
            return (int)Math.Round(Math.Max(50.0, Math.Min(900.0, ssrt)));
        }

        /// <summary>
        /// ¿La persona empezó a esperar el alto (lanzar cada vez más lento para frenar mejor)? Eso invalida la medida:
        /// se compara el promedio de los 6 primeros lanzamientos de ir con el de los 6 últimos.
        /// </summary>
        public static bool IsWaiting(IReadOnlyList<float> goRtsMs)
        {
            if (goRtsMs == null || goRtsMs.Count < 12) return false;
            double first = 0, last = 0;
            for (int i = 0; i < 6; i++) first += goRtsMs[i];
            for (int i = goRtsMs.Count - 6; i < goRtsMs.Count; i++) last += goRtsMs[i];
            first /= 6;
            last /= 6;
            return last > first * 1.3 && last > 550;
        }

        // ------------------------------------------------------------------ puntaje

        /// <summary>Puntos de un lanzamiento bien hecho: base + rapidez + nivel + racha.</summary>
        public static int GoPoints(float rtMs, int deadlineMs, int level, int streak)
        {
            float speed = Math.Max(0f, Math.Min(1f, (deadlineMs - rtMs) / deadlineMs));
            return 50 + (int)Math.Round(50f * speed) + 10 * (Clamp(level) - 1) + 10 * Math.Min(Math.Max(streak - 1, 0), 10);
        }

        /// <summary>Puntos por frenar a tiempo: más cuanto más tarde llegó el alto.</summary>
        public static int StopPoints(int ssdMs, int streak) =>
            150 + Math.Max(0, ssdMs) / 5 + 10 * Math.Min(Math.Max(streak - 1, 0), 10);

        /// <summary>Puntaje 0-100: lanzamientos correctos a tiempo (50%) y tiempo de frenado (50%, 450 ms → 0,
        /// 150 ms → 1). Sin SSRT confiable, solo los lanzamientos.</summary>
        public static int Score(float goAccuracy, int ssrtMs)
        {
            float acc = Math.Max(0f, Math.Min(1f, goAccuracy));
            if (ssrtMs <= 0) return (int)Math.Round(acc * 100f);
            float brake = Math.Max(0f, Math.Min(1f, (450f - ssrtMs) / 300f));
            return Math.Max(0, Math.Min(100, (int)Math.Round((0.5f * acc + 0.5f * brake) * 100f)));
        }

        private static int Clamp(int level) => Math.Max(1, Math.Min(MaxLevel, level));
    }
}
