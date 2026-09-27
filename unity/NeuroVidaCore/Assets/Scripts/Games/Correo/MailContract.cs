using System;
using System.Collections.Generic;

namespace NeuroVida.Games.Correo
{
    /// <summary>Un planeta que pasa junto a la ruta: su color (0..7, los de Tráfico Estelar), si es de un encargo y si es
    /// un "parecido" (color cercano al de un encargo, para confundir).</summary>
    public readonly struct MailPlanet
    {
        public readonly int Color;
        public readonly bool IsTarget, IsLure;
        /// <summary>Lado de la ruta: -1 izquierda, +1 derecha.</summary>
        public readonly int Side;

        public MailPlanet(int color, bool isTarget, bool isLure, int side)
        {
            Color = color;
            IsTarget = isTarget;
            IsLure = isLure;
            Side = side;
        }
    }

    public enum RadioJudgement { OnTime, Early, Late }

    /// <summary>Cómo se usó el reloj: cuántas veces se miró y cuántas justo antes de la hora (último 30% del intervalo).</summary>
    public readonly struct ClockProfile
    {
        public readonly int Checks, LateChecks, Intervals;
        public ClockProfile(int checks, int lateChecks, int intervals)
        {
            Checks = checks;
            LateChecks = lateChecks;
            Intervals = intervals;
        }

        /// <summary>Parte de las miradas que fue justo antes de la hora (-1 = sin miradas).</summary>
        public float LateShare => Checks == 0 ? -1f : (float)LateChecks / Checks;
    }

    /// <summary>
    /// Reglas puras de "Correo Estelar": el vuelo de Piloto Estelar (mantenerse en la ruta y recoger sobres es la tarea en
    /// curso) con ENCARGOS que hay que recordar en el momento justo, sin que nadie avise: memoria prospectiva (Rummel y
    /// Kvavilashvili, 2023, revisión en Nature Reviews Psychology).
    /// <list type="bullet">
    /// <item><b>Por lugar</b> (evento): "cuando pases un planeta coral, tócalo" (le entregas su paquete). Los planetas pasan
    /// a los costados; la mayoría no son del encargo y algunos tienen un color parecido.</item>
    /// <item><b>Por hora</b> (tiempo): "cada 30 segundos, toca la radio". El reloj va tapado: tocarlo lo muestra un momento.
    /// Cuándo se mira mide la estrategia: lo eficaz es mirar poco al principio y más a medida que se acerca la hora.</item>
    /// </list>
    /// Sin dependencias de UnityEngine: testeable con NUnit.
    /// </summary>
    public static class MailContract
    {
        public const string GameId = "correo";
        public const int MaxLevel = 10;
        public const int PlanetColors = 8;

        /// <summary>Duración del vuelo (Reto y Precisión: Precisión vuela más tranquilo).</summary>
        public const float FlightSeconds = 150f;

        /// <summary>Ventana para el aviso por radio: ± segundos alrededor de la hora.</summary>
        public const float RadioWindow = 5f;

        /// <summary>"Justo antes de la hora" = el último 30% de cada intervalo.</summary>
        public const float LateFraction = 0.3f;

        /// <summary>
        /// Color parecido de cada color (para confundir): coral → naranjo, amarillo → naranjo, celeste → menta, lila → rosado
        /// (y al revés para los demás). Los parecidos de los colores de encargo nunca son colores de encargo.
        /// </summary>
        private static readonly int[] LureOf = { 7, 7, 6, 6, 5, 0, 3, 0 };

        /// <summary>Nombres de los colores (mismo orden que <c>TrafficSprites.Colors</c>).</summary>
        public static readonly string[] ColorNames = { "coral", "amarillo", "celeste", "verde", "lila", "rosado", "menta", "naranjo" };

        /// <summary>Colores que sirven para encargos (bien distintos entre sí): coral, celeste, amarillo, lila.</summary>
        private static readonly int[] TargetPalette = { 0, 2, 1, 4 };

        // ------------------------------------------------------------------ niveles

        /// <summary>Colores de planeta con encargo: 1 (niveles 1-3) o 2 (desde el 4).</summary>
        public static int EventColors(int level) => Clamp(level) >= 4 ? 2 : 1;

        /// <summary>Cada cuántos segundos hay que avisar por radio: 30 (niveles 1-4), 25 (5-7), 20 (8-10). Desde el nivel 1:
        /// los dos tipos de encargo desde el primer vuelo (Ricardo, 28-sep: con uno solo "no tuve que trabajar la memoria").</summary>
        public static float RadioPeriod(int level)
        {
            level = Clamp(level);
            return level <= 4 ? 30f : level <= 7 ? 25f : 20f;
        }

        /// <summary>Probabilidad de que un planeta que no es del encargo tenga un color parecido: 0 (nivel 1), 0,15 → 0,45.</summary>
        public static float LureChance(int level)
        {
            level = Clamp(level);
            return level <= 1 ? 0f : 0.15f + 0.3f * (level - 2) / (MaxLevel - 2);
        }

        /// <summary>
        /// Segundos entre un planeta y el siguiente: 3,6 (nivel 1) → 2,4 (nivel 10), y un 30% menos al final del vuelo
        /// (<paramref name="progress"/> 0..1). Muchos planetas: casi todos hay que dejarlos pasar.
        /// </summary>
        public static float PlanetGap(int level, float progress) =>
            (3.6f - 1.2f * (Clamp(level) - 1) / (MaxLevel - 1)) * (1f - 0.3f * Clamp01(progress));

        /// <summary>Proporción de planetas que son del encargo (pocos: si fueran muchos, no habría que recordar nada).</summary>
        public const float TargetChance = 0.3f;

        /// <summary>Nivel de la ruta (ancho, curvas y velocidad: los de Piloto Estelar, que tiene 9).</summary>
        public static int FlightLevel(int level) => Math.Max(1, Math.Min(9, Clamp(level)));

        // ------------------------------------------------------------------ el vuelo se acelera

        /// <summary>El vuelo tiene tres tramos (0, 1, 2); en cada uno la ruta va más rápida, más curva y más angosta.</summary>
        public static int Stage(float progress) => progress < 1f / 3f ? 0 : progress < 2f / 3f ? 1 : 2;

        /// <summary>Velocidad: × 1 al salir → × 1,5 al final del vuelo (sube de a poco, se nota en cada tramo).</summary>
        public static float SpeedRamp(float progress) => 1f + 0.5f * Clamp01(progress);

        /// <summary>Curvas: × 1 → × 1,5 (la ruta se vuelve más sinuosa).</summary>
        public static float CurveRamp(float progress) => 1f + 0.5f * Clamp01(progress);

        /// <summary>Ancho de la ruta: × 1 → × 0,85.</summary>
        public static float LaneRamp(float progress) => 1f - 0.15f * Clamp01(progress);

        /// <summary>
        /// Segundos entre un asteroide y el siguiente en la ruta (hay que esquivarlos): 3,4 (pilotaje nivel 1) → 1,8 (9), y
        /// un 35% menos al final del vuelo; nunca menos de 1,1.
        /// </summary>
        public static float AsteroidGap(int driveLevel, float progress)
        {
            float g = 3.4f - 1.6f * (Math.Max(1, Math.Min(9, driveLevel)) - 1) / 8f;
            return Math.Max(1.1f, g * (1f - 0.35f * Clamp01(progress)));
        }

        private static float Clamp01(float x) => Math.Max(0f, Math.Min(1f, x));

        // ------------------------------------------------------------------ encargos

        /// <summary>Colores de los encargos por lugar del vuelo (distintos entre sí).</summary>
        public static int[] PickTargets(int level, Random rng)
        {
            var pool = new List<int>(TargetPalette);
            for (int i = pool.Count - 1; i > 0; i--)
            {
                int j = rng.Next(i + 1);
                (pool[i], pool[j]) = (pool[j], pool[i]);
            }
            return pool.GetRange(0, EventColors(level)).ToArray();
        }

        /// <summary>
        /// El próximo planeta. Nunca dos del encargo seguidos (el encargo debe "sorprender"); los demás, a veces del color
        /// parecido (desde el nivel 3) y si no, de un color que no es de ningún encargo ni parecido.
        /// </summary>
        public static MailPlanet NextPlanet(int level, IReadOnlyList<int> targets, bool previousWasTarget, Random rng)
        {
            int side = rng.Next(2) == 0 ? -1 : 1;
            if (!previousWasTarget && rng.NextDouble() < TargetChance)
                return new MailPlanet(targets[rng.Next(targets.Count)], true, false, side);
            if (rng.NextDouble() < LureChance(level))
            {
                int lure = LureOf[targets[rng.Next(targets.Count)]];
                if (!Contains(targets, lure)) return new MailPlanet(lure, false, true, side);
            }
            var others = new List<int>();
            for (int c = 0; c < PlanetColors; c++)
            {
                if (Contains(targets, c)) continue;
                bool isLure = false;
                foreach (int t in targets) if (LureOf[t] == c) isLure = true;
                if (!isLure) others.Add(c);
            }
            if (others.Count == 0) for (int c = 0; c < PlanetColors; c++) if (!Contains(targets, c)) others.Add(c);
            return new MailPlanet(others[rng.Next(others.Count)], false, false, side);
        }

        private static bool Contains(IReadOnlyList<int> list, int v)
        {
            foreach (int x in list) if (x == v) return true;
            return false;
        }

        // ------------------------------------------------------------------ radio

        /// <summary>Horas de aviso por radio dentro del vuelo: período, 2 × período... (sin pasarse del final).</summary>
        public static List<float> RadioTargets(float period, float flightSeconds)
        {
            var list = new List<float>();
            if (period <= 0f) return list;
            for (float t = period; t <= flightSeconds - 2f; t += period) list.Add(t);
            return list;
        }

        /// <summary>
        /// Juzga un aviso por radio a los <paramref name="t"/> segundos. A tiempo = dentro de ± <see cref="RadioWindow"/> de
        /// una hora que todavía no tenía aviso (devuelve cuál en <paramref name="index"/>). Si no, temprano o tarde respecto
        /// de la hora más cercana.
        /// </summary>
        public static RadioJudgement JudgeRadio(float t, IReadOnlyList<float> targets, ISet<int> answered, out int index)
        {
            index = -1;
            float best = float.MaxValue;
            int nearest = -1;
            for (int i = 0; i < targets.Count; i++)
            {
                float d = Math.Abs(t - targets[i]);
                if (d <= RadioWindow && !answered.Contains(i))
                {
                    index = i;
                    return RadioJudgement.OnTime;
                }
                if (d < best)
                {
                    best = d;
                    nearest = i;
                }
            }
            if (nearest < 0) return RadioJudgement.Late;
            return t < targets[nearest] ? RadioJudgement.Early : RadioJudgement.Late;
        }

        /// <summary>
        /// Cómo se usó el reloj: miradas durante los intervalos de radio (desde el inicio hasta la última hora + ventana), y
        /// cuántas cayeron en el último 30% del intervalo, antes de su hora.
        /// </summary>
        public static ClockProfile Monitoring(IReadOnlyList<float> checks, IReadOnlyList<float> targets, float period)
        {
            if (targets.Count == 0 || period <= 0f) return new ClockProfile(0, 0, 0);
            float end = targets[targets.Count - 1] + RadioWindow;
            int n = 0, late = 0;
            foreach (float c in checks)
            {
                if (c < 0f || c > end) continue;
                n++;
                float phase = c % period;
                if (phase >= period * (1f - LateFraction)) late++;
            }
            return new ClockProfile(n, late, targets.Count);
        }

        // ------------------------------------------------------------------ puntaje

        public static float Rate(int hits, int total) => total <= 0 ? -1f : (float)hits / total;

        /// <summary>
        /// Puntaje 0-100: encargos cumplidos (60%: lugar y hora por igual si hay de los dos; se restan los planetas tocados
        /// por error), la ruta (20%) y el nivel (20%).
        /// </summary>
        public static int Score(int eventHits, int eventTotal, int commissions, int radioHits, int radioTotal, float lanePct, int level)
        {
            float ev = eventTotal > 0 ? Math.Max(0f, (eventHits - 0.5f * commissions) / eventTotal) : -1f;
            float ti = radioTotal > 0 ? (float)radioHits / radioTotal : -1f;
            float pm = ev >= 0f && ti >= 0f ? (ev + ti) * 0.5f : ev >= 0f ? ev : Math.Max(0f, ti);
            float lane = Math.Max(0f, Math.Min(1f, lanePct));
            float lv = (float)(Clamp(level) - 1) / (MaxLevel - 1);
            return Math.Max(0, Math.Min(100, (int)Math.Round((0.6f * Math.Min(1f, pm) + 0.2f * lane + 0.2f * lv) * 100f)));
        }

        public static int DeliveryPoints(int level, int streak) => 100 + 15 * (Clamp(level) - 1) + 25 * Math.Min(Math.Max(streak - 1, 0), 6);

        private static int Clamp(int level) => Math.Max(1, Math.Min(MaxLevel, level));
    }
}
