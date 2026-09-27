using System;
using System.Collections.Generic;

namespace NeuroVida.Games.Satelites
{
    /// <summary>
    /// Movimiento de los satélites (lógica pura, reproducible con semilla). Coordenadas normalizadas: el campo mide 1 de
    /// ancho y <see cref="Height"/> de alto; la velocidad va en anchos de campo por segundo. Cada satélite avanza con
    /// velocidad constante y rumbo que deriva al azar (trayectorias suaves e impredecibles, como en las tareas de
    /// seguimiento múltiple), rebota en los bordes y en los otros satélites (nunca se superponen del todo: seguir uno no
    /// depende de adivinar en un cruce exacto).
    /// </summary>
    public sealed class SatelliteSwarm
    {
        /// <summary>Radio de un satélite (fracción del ancho del campo).</summary>
        public const float Radius = 0.06f;
        /// <summary>Distancia mínima entre centros (un poco más que dos radios: se rozan, no se tapan).</summary>
        public const float MinGap = Radius * 2.1f;
        /// <summary>Cuánto puede girar el rumbo por segundo (radianes, deriva al azar).</summary>
        public const float Wander = 1.8f;

        public readonly int Count;
        public readonly float Height;
        public readonly float[] X, Y, Heading;
        public float Speed;
        private readonly Random _rng;

        public SatelliteSwarm(int count, float height, float speed, Random rng)
        {
            Count = count;
            Height = height;
            Speed = speed;
            _rng = rng;
            X = new float[count];
            Y = new float[count];
            Heading = new float[count];
            for (int i = 0; i < count; i++)
            {
                // Posición inicial sin superposición (con aire extra, así la señal inicial se ve clara).
                for (int tries = 0; tries < 400; tries++)
                {
                    X[i] = Radius + (float)rng.NextDouble() * (1f - 2f * Radius);
                    Y[i] = Radius + (float)rng.NextDouble() * (height - 2f * Radius);
                    if (FarFromOthers(i, MinGap * 1.5f)) break;
                }
                Heading[i] = (float)(rng.NextDouble() * Math.PI * 2.0);
            }
        }

        private bool FarFromOthers(int i, float gap)
        {
            for (int j = 0; j < i; j++)
            {
                float dx = X[i] - X[j], dy = Y[i] - Y[j];
                if (dx * dx + dy * dy < gap * gap) return false;
            }
            return true;
        }

        public void Step(float dt)
        {
            if (dt <= 0f) return;
            for (int i = 0; i < Count; i++)
            {
                Heading[i] += ((float)_rng.NextDouble() - 0.5f) * 2f * Wander * dt;
                X[i] += (float)Math.Cos(Heading[i]) * Speed * dt;
                Y[i] += (float)Math.Sin(Heading[i]) * Speed * dt;
                // Bordes: rebote espejo.
                if (X[i] < Radius) { X[i] = Radius; Heading[i] = (float)Math.PI - Heading[i]; }
                else if (X[i] > 1f - Radius) { X[i] = 1f - Radius; Heading[i] = (float)Math.PI - Heading[i]; }
                if (Y[i] < Radius) { Y[i] = Radius; Heading[i] = -Heading[i]; }
                else if (Y[i] > Height - Radius) { Y[i] = Height - Radius; Heading[i] = -Heading[i]; }
            }
            // Choques: se separan hasta la distancia mínima y cada uno refleja su rumbo sobre la línea que los une.
            for (int pass = 0; pass < 3; pass++)
            {
                for (int i = 0; i < Count; i++)
                {
                    for (int j = i + 1; j < Count; j++)
                    {
                        float dx = X[j] - X[i], dy = Y[j] - Y[i];
                        float d2 = dx * dx + dy * dy;
                        if (d2 >= MinGap * MinGap) continue;
                        float d = (float)Math.Sqrt(d2);
                        float nx, ny;
                        if (d < 1e-5f) { nx = 1f; ny = 0f; d = 0f; }
                        else { nx = dx / d; ny = dy / d; }
                        float push = (MinGap - d) * 0.5f;
                        X[i] -= nx * push; Y[i] -= ny * push;
                        X[j] += nx * push; Y[j] += ny * push;
                        if (pass == 0)
                        {
                            Heading[i] = Reflect(Heading[i], nx, ny, towards: true);
                            Heading[j] = Reflect(Heading[j], -nx, -ny, towards: true);
                        }
                    }
                }
                ClampAll();
            }
        }

        /// <summary>Si el rumbo va hacia el otro (componente positiva sobre la normal), lo refleja.</summary>
        private static float Reflect(float heading, float nx, float ny, bool towards)
        {
            float vx = (float)Math.Cos(heading), vy = (float)Math.Sin(heading);
            float dot = vx * nx + vy * ny;
            if (towards && dot <= 0f) return heading;
            vx -= 2f * dot * nx;
            vy -= 2f * dot * ny;
            return (float)Math.Atan2(vy, vx);
        }

        private void ClampAll()
        {
            for (int i = 0; i < Count; i++)
            {
                X[i] = Math.Max(Radius, Math.Min(1f - Radius, X[i]));
                Y[i] = Math.Max(Radius, Math.Min(Height - Radius, Y[i]));
            }
        }

        /// <summary>Índice del satélite más cercano a (x, y) dentro de <paramref name="reach"/>; -1 si ninguno.</summary>
        public int Nearest(float x, float y, float reach)
        {
            int best = -1;
            float bestD = reach * reach;
            for (int i = 0; i < Count; i++)
            {
                float dx = X[i] - x, dy = Y[i] - y, d2 = dx * dx + dy * dy;
                if (d2 < bestD) { bestD = d2; best = i; }
            }
            return best;
        }
    }

    /// <summary>
    /// Reglas puras de "Satélites": SEGUIMIENTO DE MÚLTIPLES OBJETOS (Pylyshyn y Storm, 1988; en 3D, NeuroTracker:
    /// Faubert y Sidebottom, 2012). Varios satélites iguales; algunos se encienden (llevan la señal), se apagan y todos
    /// se mueven y se cruzan; al detenerse hay que tocar los que llevaban la señal. La dificultad suma satélites a
    /// seguir y en total, y sube la velocidad. Sin dependencias de UnityEngine: testeable con NUnit.
    /// </summary>
    public static class SatelliteContract
    {
        public const string GameId = "satelites";
        public const int MaxLevel = 12;

        /// <summary>Reto: 2 minutos de rondas.</summary>
        public const int RetoSeconds = 120;
        /// <summary>Precisión (sin reloj): cantidad de rondas.</summary>
        public const int PrecisionRounds = 8;
        /// <summary>Segundos con la señal encendida antes de que empiecen a moverse.</summary>
        public const float CueSeconds = 2.2f;

        private static readonly int[] TargetsByLevel = { 2, 2, 3, 3, 3, 4, 4, 4, 4, 5, 5, 5 };
        private static readonly int[] TotalByLevel = { 6, 6, 7, 7, 8, 8, 8, 9, 10, 10, 10, 11 };
        private static readonly float[] SpeedByLevel = { 0.16f, 0.21f, 0.19f, 0.24f, 0.29f, 0.25f, 0.30f, 0.35f, 0.40f, 0.38f, 0.45f, 0.52f };

        public static int Targets(int level) => TargetsByLevel[Clamp(level) - 1];
        public static int Total(int level) => TotalByLevel[Clamp(level) - 1];
        /// <summary>Velocidad (anchos de campo por segundo).</summary>
        public static float Speed(int level) => SpeedByLevel[Clamp(level) - 1];
        /// <summary>Velocidad como múltiplo de la del nivel 1 (lo que se le muestra a la persona: "1,8×").</summary>
        public static float SpeedFactor(int level) => Speed(level) / SpeedByLevel[0];
        /// <summary>Segundos de movimiento: 5 en el nivel 1 → 8 en el 12.</summary>
        public static float TrackSeconds(int level) => 5f + 3f * (Clamp(level) - 1) / (MaxLevel - 1);

        // ------------------------------------------------------------------ puntaje y medidas

        /// <summary>Puntos de la ronda: cada satélite bien encontrado suma; la ronda completa, un bono con nivel y racha.</summary>
        public static int Points(int hits, int targets, int level, int streak)
        {
            int pts = 40 * Math.Max(0, hits);
            if (targets > 0 && hits >= targets) pts += 100 + 15 * (Clamp(level) - 1) + 20 * Math.Min(Math.Max(streak - 1, 0), 10);
            return pts;
        }

        /// <summary>
        /// Cuántos satélites se siguieron de verdad en una ronda, descontando los aciertos por suerte (modelo clásico
        /// de seguimiento: se siguen m de los k; los k − m que faltan se eligen al azar entre los n − m restantes, así que
        /// los aciertos esperados son m + (k − m)² / (n − m)). Se despeja m a partir de los aciertos observados.
        /// </summary>
        public static float TrackedEstimate(int hits, int targets, int total)
        {
            if (targets <= 0 || total <= targets) return 0f;
            hits = Math.Max(0, Math.Min(targets, hits));
            if (hits >= targets) return targets;
            float lo = 0f, hi = targets;
            for (int it = 0; it < 40; it++)
            {
                float m = (lo + hi) * 0.5f;
                if (ExpectedHits(m, targets, total) < hits) lo = m; else hi = m;
            }
            return (lo + hi) * 0.5f;
        }

        private static float ExpectedHits(float m, int k, int n) => m + (k - m) * (k - m) / Math.Max(1e-4f, n - m);

        /// <summary>"Tu seguimiento": promedio de <see cref="TrackedEstimate"/> de las rondas (satélites a la vez).
        /// -1 si no hay rondas.</summary>
        public static float Capacity(IReadOnlyList<(int hits, int targets, int total)> rounds)
        {
            if (rounds == null || rounds.Count == 0) return -1f;
            float sum = 0f;
            foreach (var r in rounds) sum += TrackedEstimate(r.hits, r.targets, r.total);
            return sum / rounds.Count;
        }

        /// <summary>Puntaje 0-100: satélites encontrados (60%) y nivel más alto superado completo (40%).</summary>
        public static int Score(float hitRate, int bestClearedLevel)
        {
            float h = Math.Max(0f, Math.Min(1f, hitRate));
            float lv = bestClearedLevel <= 0 ? 0f : (float)(Clamp(bestClearedLevel) - 1) / (MaxLevel - 1);
            return Math.Max(0, Math.Min(100, (int)Math.Round((0.6f * h + 0.4f * lv) * 100f)));
        }

        /// <summary>
        /// Momento en que dos satélites pasaron más cerca (para mostrar "¿aquí se cruzaron?" cuando se confundió uno por
        /// otro). Recibe las trayectorias muestreadas (x, y) de ambos; devuelve el índice de muestra, o -1.
        /// </summary>
        public static int ClosestApproach(IReadOnlyList<float> ax, IReadOnlyList<float> ay, IReadOnlyList<float> bx, IReadOnlyList<float> by)
        {
            int n = Math.Min(Math.Min(ax.Count, ay.Count), Math.Min(bx.Count, by.Count));
            int best = -1;
            float bestD = float.MaxValue;
            for (int i = 0; i < n; i++)
            {
                float dx = ax[i] - bx[i], dy = ay[i] - by[i], d2 = dx * dx + dy * dy;
                if (d2 < bestD) { bestD = d2; best = i; }
            }
            return best;
        }

        private static int Clamp(int level) => Math.Max(1, Math.Min(MaxLevel, level));
    }
}
