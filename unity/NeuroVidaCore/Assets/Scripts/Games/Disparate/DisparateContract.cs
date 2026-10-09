using System;
using System.Collections.Generic;

namespace NeuroVida.Games.Disparate
{
    /// <summary>Tipo de frase, en el orden de la tabla de dificultad (y de las listas de telemetría <c>sv_*_type</c>, 0 a 5):
    /// corta, con complemento o adjetivo, con negación, con pausa («, que …,»), todos / algunos / ningún, comparaciones.</summary>
    public enum SentenceType { Short = 1, Complement = 2, Negation = 3, Pause = 4, Quantifier = 5, Comparison = 6 }

    /// <summary>Cómo es una frase del juego (sale de Resources/Frases/disparate_es.json: lo escribe tools/frases/disparate.py).</summary>
    [Serializable]
    public sealed class SentenceItem
    {
        public string f;      // la frase
        public bool v;        // true = verdad, false = disparate
        public int t;         // tipo 1 a 6
        public string c;      // clase del disparate: "evidente" / "sutil" ("" si es verdad)
        public int n;         // palabras
        public string r;      // corrección corta (solo disparates)
        public string i;      // id estable (8 hex)

        public bool IsSubtle => c == "sutil";
    }

    /// <summary>Qué se muestra ahora: la frase, si es de una Ráfaga y los segundos de señal (0 = sin límite).</summary>
    public sealed class SentenceSpec
    {
        public SentenceItem S;
        public bool Burst;
        public float SignalSeconds;
    }

    /// <summary>
    /// Reglas puras de "¿Verdad o disparate?" (juego estrella de Lenguaje; ver docs/diseno-verdad-o-disparate.md). Llegan
    /// frases cortas: se decide si son verdad o un disparate. Es la VERIFICACIÓN DE FRASES (Collins y Quillian, 1969; usada
    /// en neuropsicología para la memoria semántica, p. ej. Wilson y Baddeley, 1988) con frases generadas por programa, sin
    /// un banco fijo que se memorice (Crossland, Legge y Dakin, 2008). La dificultad viene de la FORMA de la frase (negación,
    /// cláusula entre comas, cuantificadores, comparaciones: las negaciones cuestan más, Clark y Chase, 1972), no del
    /// conocimiento. NO se calcula ningún perfil de sesgo (tender a decir verdad o disparate): solo aciertos por tipo de frase.
    /// Sin dependencias de UnityEngine: testeable con NUnit.
    /// </summary>
    public static class DisparateContract
    {
        public const string GameId = "disparate";
        public const int MaxLevel = 12;

        /// <summary>Reto: 2 minutos. Precisión (sin señal que se apaga): frases por partida.</summary>
        public const int RetoSeconds = 120;
        public const int PrecisionSentences = 30;
        /// <summary>Nunca más de tantas respuestas iguales seguidas (verdad o disparate).</summary>
        public const int MaxSameAnswerRun = 3;
        /// <summary>Ráfaga: cada tantas frases llegan <see cref="BurstSize"/> frases de 3 palabras muy rápidas, fuera de la escalera y de las medidas.</summary>
        public const int BurstEvery = 12;
        public const int BurstSize = 5;
        public const float BurstSignalSeconds = 4f;
        /// <summary>Transmisión perfecta: racha desde la que la antena se enciende (puntos × 1,5).</summary>
        public const int PerfectStreak = 10;
        /// <summary>Rachas desde las que la antena emite ondas de color.</summary>
        public const int ColorWavesStreak = 5;
        /// <summary>Aciertos mínimos para estimar las palabras por minuto, y por tipo de frase para dar su tiempo.</summary>
        public const int MinWpmHits = 10;
        public const int MinTypeHits = 4;
        /// <summary>Segundos de la señal de las frases que siguen sin responder en mayores: × 1,3.</summary>
        public const float SeniorSignalFactor = 1.3f;
        /// <summary>Cuántas partidas anteriores se evitan al elegir frases.</summary>
        public const int RecentGames = 3;

        // Tipo de frase por nivel: 1-2 cortas … 11-12 comparaciones.
        public static SentenceType TypeForLevel(int level) => (SentenceType)((Clamp(level) - 1) / 2 + 1);

        private static readonly float[] Signal = { 9f, 9f, 8f, 8f, 7f, 7f, 6.5f, 6.5f, 6f, 6f, 5f, 5f };

        /// <summary>Segundos de señal del nivel (9 → 5); mayores × 1,3. En Precisión no hay señal: 0.</summary>
        public static float SignalSeconds(int level, bool precision, bool senior) =>
            precision ? 0f : Signal[Clamp(level) - 1] * (senior ? SeniorSignalFactor : 1f);

        public static float BurstSeconds(bool precision, bool senior) =>
            precision ? 0f : BurstSignalSeconds * (senior ? SeniorSignalFactor : 1f);

        /// <summary>Qué trae cada nivel (para el aviso al subir).</summary>
        public static string LevelNews(int level)
        {
            switch (Clamp(level))
            {
                case 3: return "Frases con complemento";
                case 5: return "Ahora con «no»";
                case 7: return "Frases con una pausa en medio";
                case 9: return "Todos, algunos, ningún";
                case 11: return "Comparaciones";
                default: return "";
            }
        }

        /// <summary>Tamaño de la letra de la frase, en sp: 24 y, en mayores, 28 (Atkinson Hyperlegible Bold).</summary>
        public static int PhraseSizeSp(bool senior) => senior ? 28 : 24;

        /// <summary>Alto mínimo de los botones, en dp.</summary>
        public const float MinButtonDp = 64f;

        // ------------------------------------------------------------------ frases de la práctica (tutorial con Nubi)

        /// <summary>Segundos de señal de la tercera frase de la práctica: solo se ve bajar la barra un rato (en Precisión no hay señal).</summary>
        public const float PracticeSignalSeconds = 8f;

        /// <summary>Las tres frases de la práctica: una verdad clarísima, un disparate clarísimo (con su corrección) y otra verdad corta donde se ve la señal. No salen del banco ni cuentan para nada.</summary>
        public static SentenceSpec[] PracticeSpecs(bool precision)
        {
            SentenceSpec Spec(string f, bool truth, int type, string kind, string fix, string id, float signal) => new SentenceSpec
            {
                S = new SentenceItem { f = f, v = truth, t = type, c = kind, n = f.Split(' ').Length, r = fix, i = id },
                Burst = false,
                SignalSeconds = signal
            };
            return new[]
            {
                Spec("Los peces nadan", true, 1, "", "", "practica-1", 0f),
                Spec("Las piedras cantan", false, 1, "evidente", "Las piedras no cantan", "practica-2", 0f),
                Spec("El sol calienta la tierra", true, 2, "", "", "practica-3", precision ? 0f : PracticeSignalSeconds)
            };
        }

        // ------------------------------------------------------------------ qué sale

        /// <summary>Quién sale: 50% verdad y 50% disparate, y NUNCA más de <see cref="MaxSameAnswerRun"/> iguales seguidas.</summary>
        public sealed class AnswerSequencer
        {
            private bool _last;
            private int _run;

            public int Run => _run;

            /// <summary>La próxima respuesta correcta: true = verdad, false = disparate.</summary>
            public bool Next(Random rng)
            {
                if (_run >= MaxSameAnswerRun) return !_last;
                return rng.NextDouble() < 0.5;
            }

            public void Record(bool answer)
            {
                _run = _run > 0 && answer == _last ? _run + 1 : 1;
                _last = answer;
            }
        }

        /// <summary>¿Toca Ráfaga? Después de cada <see cref="BurstEvery"/> frases (sin contar las de ráfaga) y solo una vez por cada tanda.</summary>
        public static bool BurstDue(int normalSpawned, int burstsDone) =>
            normalSpawned > 0 && normalSpawned / BurstEvery > burstsDone;

        // ------------------------------------------------------------------ puntaje

        /// <summary>Puntos de un acierto: 100 + 10 por nivel; con la antena encendida (racha ≥ 10) × 1,5; una frase de ráfaga vale 50.</summary>
        public static int Points(int level, int streak, bool burst)
        {
            if (burst) return 50;
            float pts = 100f + 10f * (Clamp(level) - 1);
            if (streak >= PerfectStreak) pts *= 1.5f;
            return (int)Math.Round(pts);
        }

        /// <summary>Puntaje 0-100: aciertos / frases (70%) y nivel más alto alcanzado (30%).</summary>
        public static int Score(float accuracy, int peakLevel)
        {
            float lv = (float)(Clamp(peakLevel) - 1) / (MaxLevel - 1);
            float a = Math.Max(0f, Math.Min(1f, accuracy));
            return Math.Max(0, Math.Min(100, (int)Math.Round((0.7f * a + 0.3f * lv) * 100f)));
        }

        // ------------------------------------------------------------------ medidas del final

        /// <summary>
        /// "Tu lectura con comprensión": mediana, en las frases bien respondidas, de palabras ÷ tiempo de respuesta, en palabras
        /// por minuto. Incluye la decisión (se rotula "leyendo y decidiendo"). -1 con menos de <see cref="MinWpmHits"/> aciertos.
        /// </summary>
        public static int WordsPerMinute(IReadOnlyList<int> words, IReadOnlyList<int> ms)
        {
            if (words == null || ms == null || words.Count != ms.Count || words.Count < MinWpmHits) return -1;
            var wpm = new List<double>();
            for (int i = 0; i < words.Count; i++)
                if (ms[i] > 0) wpm.Add(words[i] * 60000.0 / ms[i]);
            if (wpm.Count < MinWpmHits) return -1;
            wpm.Sort();
            int n = wpm.Count;
            double med = n % 2 == 1 ? wpm[n / 2] : (wpm[n / 2 - 1] + wpm[n / 2]) / 2.0;
            return (int)Math.Round(med);
        }

        /// <summary>Tiempo medio (ms) de los aciertos de un tipo; -1 si hay menos de <see cref="MinTypeHits"/>.</summary>
        public static int MeanMs(IReadOnlyList<int> values)
        {
            if (values == null || values.Count < MinTypeHits) return -1;
            long sum = 0;
            foreach (int v in values) sum += v;
            return (int)Math.Round(sum / (double)values.Count);
        }

        // ------------------------------------------------------------------ frases de las últimas partidas

        /// <summary>Ids de las frases de las últimas partidas: partidas separadas por ';' e ids por ','. Devuelve las partidas más recientes al final.</summary>
        public static List<List<string>> ParseRecent(string stored)
        {
            var games = new List<List<string>>();
            if (string.IsNullOrEmpty(stored)) return games;
            foreach (var g in stored.Split(';'))
            {
                var ids = new List<string>();
                foreach (var id in g.Split(','))
                    if (!string.IsNullOrWhiteSpace(id)) ids.Add(id.Trim());
                if (ids.Count > 0) games.Add(ids);
            }
            return games;
        }

        /// <summary>Agrega las frases de esta partida y se queda con las últimas <see cref="RecentGames"/>.</summary>
        public static string PushRecent(string stored, IEnumerable<string> thisGame)
        {
            var games = ParseRecent(stored);
            var mine = new List<string>(thisGame);
            if (mine.Count > 0) games.Add(mine);
            while (games.Count > RecentGames) games.RemoveAt(0);
            var parts = new List<string>();
            foreach (var g in games) parts.Add(string.Join(",", g));
            return string.Join(";", parts);
        }

        public static HashSet<string> RecentIds(string stored)
        {
            var set = new HashSet<string>();
            foreach (var g in ParseRecent(stored)) foreach (var id in g) set.Add(id);
            return set;
        }

        private static int Clamp(int level) => Math.Max(1, Math.Min(MaxLevel, level));
    }
}
