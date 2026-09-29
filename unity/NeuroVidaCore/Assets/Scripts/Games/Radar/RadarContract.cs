using System;
using System.Collections.Generic;

namespace NeuroVida.Games.Radar
{
    /// <summary>Una ronda de Radar: dónde aparecen los astronautas y los robots, y cuánto dura el destello.</summary>
    public sealed class RadarTrial
    {
        /// <summary>Lugares con astronautas (lugar = anillo * 8 + dirección), sin repetir.</summary>
        public int[] Targets;
        /// <summary>Lugares con robots (se ven, pero no se rescatan). Nunca coinciden con un astronauta.</summary>
        public int[] Robots;
        public int ExposureMs;
        public int Level;
        /// <summary>"¡Lluvia de astronautas!": destello largo con muchos, para medir cuántos se captan de un vistazo.
        /// No mueve la dificultad ni cuenta para "tu vistazo".</summary>
        public bool Rain;
    }

    /// <summary>Cómo le fue a una respuesta (las balizas que puso la persona).</summary>
    public struct RadarAnswer
    {
        /// <summary>Balizas sobre un astronauta.</summary>
        public int Hits;
        /// <summary>Balizas donde no había astronauta (vacío o robot).</summary>
        public int Extras;
        /// <summary>De esas, las que cayeron sobre un robot.</summary>
        public int RobotsTouched;
    }

    /// <summary>
    /// Reglas puras de "Radar" / Rescate relámpago (28-sep, rediseño aprobado por Ricardo): VELOCIDAD DE PROCESAMIENTO
    /// VISUAL con informe total ("whole report": Sperling, 1960; teoría de la atención visual, TVA: Bundesen, 1990;
    /// Habekost, 2015). En un destello breve aparecen VARIOS astronautas repartidos por el radar (8 direcciones x 2
    /// anillos = 16 lugares, cerca y lejos del centro); una interferencia borra la imagen y se tocan todos los lugares
    /// donde se vieron (se sabe cuántos eran). El destello llega en un momento imprevisible (alerta propia: Penning et
    /// al., 2021). Desde el nivel 5 hay robots que no se rescatan (informe parcial: seleccionar lo importante).
    /// No hay imagen central que identificar ni opciones entre las que elegir (ver docs/nombre-marca-y-riesgos.md).
    /// Sin dependencias de UnityEngine: testeable con NUnit.
    /// </summary>
    public static class RadarContract
    {
        public const string GameId = "radar";
        public const int MaxLevel = 12;
        public const int Directions = 8;
        public const int Rings = 2;
        public const int Slots = Directions * Rings;

        /// <summary>Reto: 120 s de radar (cada ronda dura más que en la versión de un astronauta: ~8 s).</summary>
        public const int RetoSeconds = 120;
        /// <summary>Precisión (sin reloj): cantidad de destellos (incluidas las lluvias).</summary>
        public const int PrecisionTrials = 20;

        /// <summary>Radio de cada anillo como fracción del radio del radar (cerca / lejos del centro).</summary>
        public static readonly float[] RingRadius = { 0.46f, 0.80f };

        /// <summary>Radio de la mira del centro (fracción del radio del radar): tocar adentro no elige lugar.</summary>
        public const float CenterWindow = 0.16f;

        /// <summary>Cada cuántas rondas llega una lluvia de astronautas, con cuántos y con qué destello.</summary>
        public const int RainEvery = 5, RainTargets = 6, RainExposureMs = 300;

        // ------------------------------------------------------------------ dificultad

        /// <summary>Duración del destello (ms): 600 en el nivel 1 → 80 en el 12, en pasos proporcionales (~17% menos
        /// por nivel). Más larga que en la versión de un solo astronauta: acá se captan varios a la vez.</summary>
        public static int ExposureMs(int level)
        {
            double k = (Clamp(level) - 1) / (double)(MaxLevel - 1);
            return (int)Math.Round(600.0 * Math.Pow(80.0 / 600.0, k));
        }

        /// <summary>Astronautas por destello: 2 → 5.</summary>
        public static int TargetCount(int level)
        {
            int l = Clamp(level);
            return l <= 2 ? 2 : l <= 5 ? 3 : l <= 8 ? 4 : 5;
        }

        /// <summary>Robots que distraen: ninguno hasta el nivel 4, uno del 5 al 8, dos desde el 9.</summary>
        public static int RobotCount(int level)
        {
            int l = Clamp(level);
            return l <= 4 ? 0 : l <= 8 ? 1 : 2;
        }

        /// <summary>¿La ronda número <paramref name="index"/> (0 = la primera) es una lluvia de astronautas?</summary>
        public static bool IsRainTrial(int index) => index >= 0 && (index + 1) % RainEvery == 0;

        public static RadarTrial NextTrial(int level, Random rng)
        {
            level = Clamp(level);
            var picked = PickSlots(TargetCount(level) + RobotCount(level), rng);
            int n = TargetCount(level);
            return new RadarTrial
            {
                Targets = picked.GetRange(0, n).ToArray(),
                Robots = picked.GetRange(n, picked.Count - n).ToArray(),
                ExposureMs = ExposureMs(level),
                Level = level,
            };
        }

        public static RadarTrial RainTrial(int level, Random rng) => new RadarTrial
        {
            Targets = PickSlots(RainTargets, rng).ToArray(),
            Robots = new int[0],
            ExposureMs = RainExposureMs,
            Level = Clamp(level),
            Rain = true,
        };

        /// <summary>Lugares distintos al azar (los primeros serán los astronautas, el resto robots).</summary>
        private static List<int> PickSlots(int count, Random rng)
        {
            var all = new List<int>();
            for (int s = 0; s < Slots; s++) all.Add(s);
            for (int i = all.Count - 1; i > 0; i--)
            {
                int j = rng.Next(i + 1);
                (all[i], all[j]) = (all[j], all[i]);
            }
            return all.GetRange(0, Math.Min(count, Slots));
        }

        // ------------------------------------------------------------------ geometría

        public static int SlotOf(int direction, int ring) => ring * Directions + direction;
        public static int DirectionOf(int slot) => slot % Directions;
        public static int RingOf(int slot) => slot / Directions;

        /// <summary>Posición de un lugar en coordenadas del radar (radio 1, y hacia arriba).</summary>
        public static void Position(int slot, out float x, out float y)
        {
            double a = DirectionOf(slot) * Math.PI / 4.0;
            float r = RingRadius[Math.Max(0, Math.Min(Rings - 1, RingOf(slot)))];
            x = (float)(r * Math.Sin(a));
            y = (float)(r * Math.Cos(a));
        }

        /// <summary>El lugar más cercano a un toque en (x, y) (en radios del radar), o -1 si el toque cae en la mira del
        /// centro o lejos del radar. Toda la zona alrededor de cada lugar cuenta: no hace falta acertarle al círculo.</summary>
        public static int NearestSlot(float x, float y)
        {
            double r = Math.Sqrt(x * x + y * y);
            if (r < CenterWindow || r > 1.2) return -1;
            int best = -1;
            double bestD = double.MaxValue;
            for (int s = 0; s < Slots; s++)
            {
                Position(s, out float sx, out float sy);
                double d = (sx - x) * (sx - x) + (sy - y) * (sy - y);
                if (d < bestD)
                {
                    bestD = d;
                    best = s;
                }
            }
            return best;
        }

        private static readonly string[] DirectionNames =
            { "arriba", "arriba a la derecha", "a la derecha", "abajo a la derecha", "abajo", "abajo a la izquierda", "a la izquierda", "arriba a la izquierda" };

        public static string DirectionName(int direction) => DirectionNames[((direction % Directions) + Directions) % Directions];

        // ------------------------------------------------------------------ respuesta

        /// <summary>Compara las balizas con lo que había. Las balizas repetidas cuentan una vez.</summary>
        public static RadarAnswer Evaluate(RadarTrial trial, IEnumerable<int> beacons)
        {
            var targets = new HashSet<int>(trial.Targets);
            var robots = new HashSet<int>(trial.Robots ?? new int[0]);
            var a = new RadarAnswer();
            foreach (int b in new HashSet<int>(beacons))
            {
                if (targets.Contains(b)) a.Hits++;
                else
                {
                    a.Extras++;
                    if (robots.Contains(b)) a.RobotsTouched++;
                }
            }
            return a;
        }

        /// <summary>Astronautas que hay que rescatar para que la ronda cuente como lograda: todos hasta 3; con 4 o más,
        /// todos menos uno. Así la escalera apunta a "rescatar casi todos".</summary>
        public static int Needed(int targets) => targets <= 3 ? targets : targets - 1;

        public static bool Success(RadarTrial trial, RadarAnswer answer) => answer.Hits >= Needed(trial.Targets.Length);

        /// <summary>Puntos: 40 por rescatado (con bono de nivel), más un bono si se logró la ronda, que crece con la racha.</summary>
        public static int Points(int hits, bool success, int level, int streak)
        {
            int pts = hits * (40 + 4 * (Clamp(level) - 1));
            if (success) pts += 60 + 20 * Math.Min(Math.Max(streak - 1, 0), 10);
            return pts;
        }

        // ------------------------------------------------------------------ medidas

        /// <summary>Rondas iniciales que no cuentan para <see cref="GlanceMs"/> (la escalera todavía está buscando).</summary>
        public const int GlanceSkip = 4;
        /// <summary>Rondas finales que se promedian.</summary>
        public const int GlanceWindow = 12;

        /// <summary>
        /// "Tu vistazo" (ms): la duración de destello en la que la escalera se asentó, o sea, la más breve con la que
        /// la persona rescata casi todos cerca de 8 de cada 10 veces. Media geométrica de las duraciones reales de las
        /// últimas rondas normales (sin lluvias), sin las primeras. -1 si hay muy pocas.
        /// </summary>
        public static int GlanceMs(IReadOnlyList<float> exposuresMs)
        {
            if (exposuresMs == null || exposuresMs.Count < GlanceSkip + 4) return -1;
            int from = Math.Max(GlanceSkip, exposuresMs.Count - GlanceWindow);
            double logSum = 0;
            int n = 0;
            for (int i = from; i < exposuresMs.Count; i++)
            {
                logSum += Math.Log(Math.Max(1.0, exposuresMs[i]));
                n++;
            }
            return (int)Math.Round(Math.Exp(logSum / n));
        }

        /// <summary>Cuántos astronautas había en promedio en las mismas rondas que usa <see cref="GlanceMs"/> (el
        /// vistazo se lee "con N a la vez"). -1 si no hay vistazo.</summary>
        public static float GlanceLoad(IReadOnlyList<int> targetCounts)
        {
            if (targetCounts == null || targetCounts.Count < GlanceSkip + 4) return -1f;
            int from = Math.Max(GlanceSkip, targetCounts.Count - GlanceWindow);
            float sum = 0f;
            for (int i = from; i < targetCounts.Count; i++) sum += targetCounts[i];
            return sum / (targetCounts.Count - from);
        }

        /// <summary>Puntaje de una lluvia: rescatados menos balizas de más (poner balizas al azar no suma).</summary>
        public static int RainScore(RadarAnswer a) => Math.Max(0, a.Hits - a.Extras);

        /// <summary>
        /// "Tu captura": cuántos astronautas se captan de un vistazo cuando el tiempo no es el límite (promedio de las
        /// lluvias: 6 astronautas, 300 ms). En TVA es la capacidad de la memoria visual de corto plazo, que en adultos
        /// ronda 3 a 4 elementos. -1 con menos de 2 lluvias.
        /// </summary>
        public static float Capture(IReadOnlyList<int> rainScores)
        {
            if (rainScores == null || rainScores.Count < 2) return -1f;
            float sum = 0f;
            foreach (int s in rainScores) sum += s;
            return sum / rainScores.Count;
        }

        /// <summary>Puntaje 0-100: rondas logradas (50%) y rapidez del vistazo (50%, escala logarítmica entre 600 y
        /// 80 ms). Sin vistazo medido, solo las rondas logradas.</summary>
        public static int Score(float accuracy, int glanceMs)
        {
            float acc = Math.Max(0f, Math.Min(1f, accuracy));
            if (glanceMs <= 0) return (int)Math.Round(acc * 100f);
            double speed = Math.Log(600.0 / Math.Max(1, glanceMs)) / Math.Log(600.0 / 80.0);
            speed = Math.Max(0.0, Math.Min(1.0, speed));
            return Math.Max(0, Math.Min(100, (int)Math.Round((0.5 * acc + 0.5 * speed) * 100.0)));
        }

        private static int Clamp(int level) => Math.Max(1, Math.Min(MaxLevel, level));
    }
}
