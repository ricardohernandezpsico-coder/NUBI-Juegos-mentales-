using System;
using System.Collections.Generic;

namespace NeuroVida.Games.Satelites
{
    /// <summary>
    /// Reglas puras de «Satélites: enciende tu planeta» (renovado el 9-oct; ver docs/diseno-satelites.md): SEGUIMIENTO DE MÚLTIPLES OBJETOS (Pylyshyn y Storm, 1988). Tu planeta está a oscuras; algunos satélites traen un mensaje (aro dorado y
    /// sobre), se apagan y todos giran en órbitas planas alrededor del planeta; cuando se detienen se tocan los que traían mensaje y cada mensaje entregado enciende una luz. Siempre plano (sin profundidad simulada) y SIN ninguna respuesta
    /// mientras se sigue. Sin dependencias de UnityEngine: testeable con NUnit.
    /// </summary>
    public static class SatelliteContract
    {
        public const string GameId = "satelites";
        public const string Title = "Satélites", CountdownSub = "Enciende tu planeta";
        public const int MaxLevel = 12;

        /// <summary>Reto: 2 minutos de rondas.</summary>
        public const int RetoSeconds = 120;
        /// <summary>Precisión (sin reloj): cantidad de rondas.</summary>
        public const int PrecisionRounds = 8;

        // ------------------------------------------------------------------ tiempos de una ronda (segundos)

        /// <summary>Aviso de sorpresa (antes de la presentación) y, la primera vez que sale cada una, con la etiqueta «NUEVO».</summary>
        public const float SurpriseSeconds = 1.7f, SurpriseFirstSeconds = 2.6f;
        public const float ReadySeconds = 0.45f;
        /// <summary>Presentación: los que traen mensaje con su aro dorado y su sobre; la señal se desvanece en los últimos 300 ms.</summary>
        public const float CueSeconds = 1.8f, CueFadeSeconds = 0.3f;
        /// <summary>Un satélite se detiene a lo más 0,9 s más tarde si todavía hay dos muy juntos o alguno bajo la nube.</summary>
        public const float MaxExtraTrackSeconds = 0.9f;
        /// <summary>Al marcar el k-ésimo se evalúa 0,55 s después: un margen para corregir el último.</summary>
        public const float EvalDelaySeconds = 0.55f;
        public const float RevealSeconds = 1.9f, CrossExtraSeconds = 0.5f;
        /// <summary>Media vuelta (frenado suave), salto de órbita y vuelo del sobre al planeta.</summary>
        public const float TurnSeconds = 0.5f, JumpSeconds = 0.9f, FlySeconds = 0.6f;

        // ------------------------------------------------------------------ geometría (dp lógicos de un campo de 360 de ancho)

        public const float FieldWidth = 360f;
        /// <summary>Un satélite mide 28 dp de diámetro (14 de radio) más 9 dp de paneles a cada lado; se toca con un círculo de 60 dp de diámetro.</summary>
        public const float SatRadius = 14f, PanelReach = 9f, TouchRadius = 30f;
        /// <summary>Distancia mínima para detenerse (la nube aparte) y para repartirlos al empezar.</summary>
        public const float StopGap = 38f, SpawnGap = 46f;
        public const float PlanetRadius = 44f, EdgeMargin = 8f;
        /// <summary>Cuánto sube el sobre de la señal por encima del borde del satélite (dp): el anillo de arriba deja ese aire.</summary>
        public const float EnvelopeRise = 22f;
        /// <summary>Al detenerse, si quedan dos a menos de <see cref="SettleGap"/> se apartan lo mínimo (hasta <see cref="SettleMax"/> dp cada uno) en <see cref="SettleSeconds"/> s.</summary>
        public const float SettleGap = 36f, SettleMax = 22f, SettleSeconds = 0.35f;
        /// <summary>Cada satélite lleva su carril: el radio de su anillo × (1 ± 6 %).</summary>
        public const float LaneSpread = 0.06f;
        /// <summary>Los anillos son más altos que anchos: alto = 1,42 × ancho (o lo que quepa).</summary>
        public const float RingHeightK = 1.42f;
        /// <summary>«Órbitas rápidas»: velocidad × 1,25 y seguimiento × 0,8.</summary>
        public const float FastSpeedFactor = 1.25f, FastTrackFactor = 0.8f;

        /// <summary>
        /// Paso del DDA común: un ensayo por ronda (acierto = todos los k). Con 1,0 una ronda perfecta sube cerca de un nivel y una fallada baja hasta uno (el motor limita la bajada a un nivel por error), como en el boceto aprobado.
        /// Sin tiempo de reacción: el tiempo es el de la tarea, no una medida.
        /// </summary>
        public const float StepUp = 1.0f;

        // ------------------------------------------------------------------ los 12 niveles

        private static readonly LevelSpec[] Levels =
        {
            new LevelSpec(2, 6,  55f, 3.5f, Surprise.None,                                  head: false, turn: false),
            new LevelSpec(3, 6,  58f, 3.8f, Surprise.Orbit,                                 head: false, turn: false),
            new LevelSpec(3, 7,  64f, 4.0f, Surprise.Orbit,                                 head: true,  turn: false),
            new LevelSpec(3, 8,  70f, 4.2f, Surprise.Orbit | Surprise.Cloud,                head: true,  turn: false),
            new LevelSpec(4, 8,  74f, 4.4f, Surprise.Orbit | Surprise.Cloud,                head: true,  turn: false),
            new LevelSpec(4, 9,  80f, 4.6f, Surprise.Orbit | Surprise.Cloud | Surprise.Fast, head: true, turn: true),
            new LevelSpec(4, 10, 88f, 4.8f, Surprise.Orbit | Surprise.Cloud | Surprise.Fast, head: true, turn: true),
            new LevelSpec(4, 10, 96f, 5.0f, Surprise.Orbit | Surprise.Cloud | Surprise.Fast, head: true, turn: true),
            new LevelSpec(5, 11, 100f, 5.2f, Surprise.Orbit | Surprise.Cloud | Surprise.Fast, head: true, turn: true),
            new LevelSpec(5, 11, 108f, 5.4f, Surprise.Orbit | Surprise.Cloud | Surprise.Fast, head: true, turn: true),
            new LevelSpec(5, 12, 116f, 5.7f, Surprise.Orbit | Surprise.Cloud | Surprise.Fast, head: true, turn: true),
            new LevelSpec(5, 13, 126f, 6.0f, Surprise.Orbit | Surprise.Cloud | Surprise.Fast, head: true, turn: true),
        };

        public static LevelSpec Level(int level) => Levels[Clamp(level) - 1];

        /// <summary>Cuánto más rápido es un nivel que el 1 (para «la velocidad que alcanzaste» de la telemetría).</summary>
        public static float SpeedFactor(int level) => Level(level).Speed / Levels[0].Speed;

        // ------------------------------------------------------------------ sorpresas

        /// <summary>
        /// Qué sorpresa trae la ronda <paramref name="round"/> (desde 1): una de cada dos rondas, desde la 2.ª (la 1.ª y las impares, ninguna), de las que el nivel permite, y nunca la misma que la anterior mientras haya otra (<paramref name="last"/>).
        /// Con una sola posible (niveles 2 y 3) sale esa. <see cref="Surprise.None"/> = ronda sin sorpresa.
        /// </summary>
        public static Surprise PickSurprise(Surprise allowed, int round, Surprise last, Random rng)
        {
            if (allowed == Surprise.None || round < 2 || round % 2 == 1) return Surprise.None;
            var pool = new List<Surprise>();
            foreach (var s in new[] { Surprise.Orbit, Surprise.Cloud, Surprise.Fast })
                if ((allowed & s) != 0 && s != last) pool.Add(s);
            if (pool.Count == 0)
                foreach (var s in new[] { Surprise.Orbit, Surprise.Cloud, Surprise.Fast })
                    if ((allowed & s) != 0) pool.Add(s);
            return pool[rng.Next(pool.Count)];
        }

        public static string SurpriseName(Surprise s)
        {
            switch (s)
            {
                case Surprise.Orbit: return "Cambio de órbita";
                case Surprise.Cloud: return "Nube de polvo";
                case Surprise.Fast: return "Órbitas rápidas";
                default: return "";
            }
        }

        /// <summary>La frase del aviso de sorpresa (≤ 2 renglones).</summary>
        public static string SurpriseLine(Surprise s)
        {
            switch (s)
            {
                case Surprise.Orbit: return "Algunos saltan a otro anillo. No los pierdas.";
                case Surprise.Cloud: return "Una nube pasa por encima. Lo que entra, sale.";
                case Surprise.Fast: return "Ronda más corta, pero más rápida.";
                default: return "";
            }
        }

        // ------------------------------------------------------------------ textos de la pantalla (español cercano, sin jerga)

        public const string NewTag = "NUEVO", SurpriseTag = "SORPRESA", CrossText = "¿Aquí se cruzaron?", TrackTitle = "Síguelos con la vista", CueSub = "Síguelos con la vista";

        public static string CueTitle(int k) => "Estos " + k + " traen un mensaje";

        /// <summary>La pista de la sorpresa mientras se sigue (una línea corta; vacía si la ronda no trae sorpresa).</summary>
        public static string TrackHint(Surprise s)
        {
            switch (s)
            {
                case Surprise.Cloud: return "Lo que entra a la nube, sale";
                case Surprise.Orbit: return "Ojo: algunos cambian de anillo";
                case Surprise.Fast: return "Más rápido, pero más corto";
                default: return "";
            }
        }

        public static string AnswerTitle(int k) => "Toca los " + k + " que traían mensaje";
        public static string AnswerSub(int marked, int k) => marked + " de " + k + " marcados · otro toque lo quita";
        public static string ResultTitle(int hits, int k) => hits >= k ? "¡" + k + " de " + k + "!" : hits + " de " + k;

        /// <summary>Bajo el resultado: «Racha ×N» desde la tercera ronda perfecta seguida; si faltó alguno, qué marca el aro punteado.</summary>
        public static string ResultSub(bool perfect, int streak) =>
            perfect ? (streak >= 3 ? "Racha ×" + streak : "Cada mensaje enciende una luz") : "El aro punteado marca el que faltó";

        public static string RoundHud(int round, int total) => "Ronda " + round + " de " + total;
        public static string RoundHudReto(int round) => "Ronda " + round;
        public static string LightsFooter(int lights) => "Luces encendidas: " + lights;

        /// <summary>«2,6 de 3,5 a la vez» (coma decimal; el segundo número sin decimales si es entero).</summary>
        public static string TrackedLine(float capacity, float meanTargets) => Decimal(capacity, "0.0") + " de " + Decimal(meanTargets, "0.#") + " a la vez";

        public static string PerfectLine(int perfect, int rounds, int bestStreak) =>
            "Rondas perfectas: " + perfect + " de " + rounds + (bestStreak >= 2 ? " · racha mayor ×" + bestStreak : "");

        public static string RecordLine(int record, bool broke) =>
            broke ? "¡Récord nuevo: " + record + " luces!" : record > 0 ? "Tu récord: " + record + " luces en una partida" : "";

        private static string Decimal(float v, string format) => v.ToString(format, System.Globalization.CultureInfo.InvariantCulture).Replace('.', ',');

        // ------------------------------------------------------------------ puntaje y medidas

        public static bool IsPerfect(int hits, int targets) => targets > 0 && hits >= targets;

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

        /// <summary>"Tu seguimiento": promedio de <see cref="TrackedEstimate"/> de las rondas (satélites a la vez). -1 si no hay rondas.</summary>
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

        /// <summary>
        /// Dónde mostrar «¿Aquí se cruzaron?»: el punto medio entre el satélite que faltó y el marcado por error en el momento en que pasaron más cerca, si fue a menos de <see cref="CrossMaxGap"/> dp; si no, no hubo cruce
        /// (la confusión no vino de un cruce). Los cuadros son las posiciones muestreadas de TODOS los satélites durante el seguimiento.
        /// </summary>
        public static bool TryCrossPoint(IReadOnlyList<float[]> framesX, IReadOnlyList<float[]> framesY, int missed, int wrong, out float x, out float y)
        {
            x = y = 0f;
            int n = Math.Min(framesX.Count, framesY.Count);
            if (n == 0) return false;
            int best = -1;
            float bestD = float.MaxValue;
            for (int f = 0; f < n; f++)
            {
                float dx = framesX[f][missed] - framesX[f][wrong], dy = framesY[f][missed] - framesY[f][wrong], d2 = dx * dx + dy * dy;
                if (d2 < bestD) { bestD = d2; best = f; }
            }
            if (best < 0 || Math.Sqrt(bestD) >= CrossMaxGap) return false;
            x = (framesX[best][missed] + framesX[best][wrong]) * 0.5f;
            y = (framesY[best][missed] + framesY[best][wrong]) * 0.5f;
            return true;
        }

        /// <summary>El cruce más lejano que todavía se señala (dp).</summary>
        public const float CrossMaxGap = 70f;

        // ------------------------------------------------------------------ la práctica del tutorial, las luces y el récord

        /// <summary>La ronda guiada del tutorial: 2 de 5, lenta y sin sorpresas ni «de frente» ni media vuelta; no cuenta.</summary>
        public static readonly LevelSpec Practice = new LevelSpec(2, 5, 36f, 3.2f, Surprise.None, head: false, turn: false);

        /// <summary>Las luces del planeta no se amontonan: a más de esto una de otra (dp); y se dibujan hasta <see cref="MaxLightDots"/> (las demás solo suman al contador y al halo).</summary>
        public const float LightGap = 7f;
        public const int MaxLightDots = 80;

        /// <summary>
        /// Dónde enciende la luz que llega: un punto del planeta (relativo a su centro, en dp), a 9 dp del borde y a más de <see cref="LightGap"/> de las que ya hay; si no halla sitio en 60 intentos, uno cerca del centro.
        /// </summary>
        public static (float x, float y) NextLightSpot(IReadOnlyList<(float x, float y)> placed, Random rng)
        {
            for (int t = 0; t < 60; t++)
            {
                double a = rng.NextDouble() * Math.PI * 2.0, r = Math.Sqrt(rng.NextDouble()) * (PlanetRadius - 9f);
                float x = (float)(r * Math.Cos(a)), y = (float)(r * Math.Sin(a));
                bool free = true;
                for (int i = 0; i < placed.Count && free; i++)
                {
                    float dx = placed[i].x - x, dy = placed[i].y - y;
                    free = Math.Sqrt(dx * dx + dy * dy) > LightGap;
                }
                if (free) return (x, y);
            }
            return (((float)rng.NextDouble() - 0.5f) * 50f, ((float)rng.NextDouble() - 0.5f) * 50f);
        }

        /// <summary>El récord de luces en una partida: el mayor entre el guardado y el de esta partida.</summary>
        public static int NewRecord(int best, int lights) => Math.Max(Math.Max(0, best), Math.Max(0, lights));

        /// <summary>true si esta partida superó el récord (con al menos una luz).</summary>
        public static bool BrokeRecord(int best, int lights) => lights > 0 && lights > Math.Max(0, best);

        /// <summary>«Encendiste N luces» (una sola: «Encendiste 1 luz»; ninguna: «No se encendió ninguna luz»).</summary>
        public static string LightsTitle(int lights) => lights <= 0 ? "No se encendió ninguna luz" : lights == 1 ? "Encendiste 1 luz" : "Encendiste " + lights + " luces";

        private static int Clamp(int level) => Math.Max(1, Math.Min(MaxLevel, level));
    }
}
