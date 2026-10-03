using System;
using NeuroVida.Contracts;

namespace NeuroVida.Games.Secuencia
{
    /// <summary>Las cuatro familias de «Rastro de luz» (docs/diseno-rastro-de-luz.md §3). El número es el índice de las listas
    /// de telemetría <c>ras_*</c>: 0 el rastro, 1 al revés, 2 el cielo gira, 3 en marcha.</summary>
    public enum RastroMode { Rastro = 0, Reves = 1, Gira = 2, Marcha = 3 }

    /// <summary>Textos y bits de cada familia. Los textos son los del diseño aprobado por Ricardo.</summary>
    public static class RastroModes
    {
        public const int Count = 4;
        public static readonly RastroMode[] All = { RastroMode.Rastro, RastroMode.Reves, RastroMode.Gira, RastroMode.Marcha };

        public static int Bit(RastroMode m) => 1 << (int)m;

        public static string Name(RastroMode m)
        {
            switch (m)
            {
                case RastroMode.Reves: return "Al revés";
                case RastroMode.Gira: return "El cielo gira";
                case RastroMode.Marcha: return "En marcha";
                default: return "El rastro";
            }
        }

        /// <summary>Rótulo de abajo mientras se mira el camino.</summary>
        public static string WatchRule(RastroMode m, int asked)
        {
            switch (m)
            {
                case RastroMode.Reves: return "Mira el rastro: lo repetirás al revés";
                case RastroMode.Marcha: return "Mira… al final, las últimas " + asked;
                default: return "Mira el rastro";
            }
        }

        /// <summary>Rótulo de abajo mientras se repite el camino.</summary>
        public static string RepeatRule(RastroMode m, int asked)
        {
            switch (m)
            {
                case RastroMode.Reves: return "Al revés: de la última a la primera";
                case RastroMode.Marcha: return "Repite las últimas " + asked;
                default: return "Repítelo con el dedo";
            }
        }

        /// <summary>La píldora de cuántas luces: «3 luces», «Últimas 3 luces».</summary>
        public static string CountLabel(RastroMode m, int asked) => (m == RastroMode.Marcha ? "Últimas " : "") + asked + " luces";

        /// <summary>La línea de la pantalla «¡NUEVO!» al llegar por primera vez a un modo.</summary>
        public static string UnlockLine(RastroMode m)
        {
            switch (m)
            {
                case RastroMode.Reves: return "De la última luz a la primera";
                case RastroMode.Gira: return "El cielo da un giro: sigue a tus luceros";
                case RastroMode.Marcha: return "Repite solo las últimas luces";
                default: return "Repite el camino de la chispa";
            }
        }
    }

    /// <summary>Un nivel de la escalera de 16 (docs/diseno-rastro-de-luz.md §4): qué familias hay, cuántas luces pide cada una, la
    /// velocidad de la chispa (dp/s) y su espera en cada lucero (ms), el giro (grados) y cuántos cruces permite el camino.</summary>
    public readonly struct RastroLevel
    {
        public readonly int Index;
        public readonly int Families;        // bits de RastroModes.Bit
        public readonly int LenRastro, LenReves, LenGira, LenMarcha;
        public readonly int ShownMin, ShownMax; // en marcha: cuántas luces recorre la chispa en total (de ahí se piden las últimas LenMarcha)
        public readonly float SparkSpeed;
        public readonly float WaitMs;
        public readonly float TurnMin, TurnMax;
        public readonly int CrossMax;        // cruces permitidos entre tramos no vecinos (0 = ninguno; <see cref="RastroLadder.CrossAny"/> = sin tope)

        public RastroLevel(int index, int families, int lenRastro, int lenReves, int lenGira, int lenMarcha, int shownMin, int shownMax,
            float sparkSpeed, float waitMs, float turnMin, float turnMax, int crossMax)
        {
            Index = index; Families = families;
            LenRastro = lenRastro; LenReves = lenReves; LenGira = lenGira; LenMarcha = lenMarcha;
            ShownMin = shownMin; ShownMax = shownMax;
            SparkSpeed = sparkSpeed; WaitMs = waitMs; TurnMin = turnMin; TurnMax = turnMax; CrossMax = crossMax;
        }

        public bool Has(RastroMode m) => (Families & RastroModes.Bit(m)) != 0;

        /// <summary>Luces que se piden repetir en esa familia (0 si el nivel no la tiene).</summary>
        public int Length(RastroMode m)
        {
            switch (m)
            {
                case RastroMode.Reves: return LenReves;
                case RastroMode.Gira: return LenGira;
                case RastroMode.Marcha: return LenMarcha;
                default: return LenRastro;
            }
        }

        /// <summary>Cruces que el generador busca producir (los permite el nivel; no hace falta llegar al tope).</summary>
        public int CrossTarget => CrossMax >= RastroLadder.CrossAny ? 3 : CrossMax;
    }

    /// <summary>La escalera de 16 niveles. Misma escala que la Secuencia anterior (el rating guardado de 0 a 1 conserva su sentido).</summary>
    public static class RastroLadder
    {
        public const int MaxLevel = 16;
        public const int CrossAny = 99;
        private const int R = 1, V = 2, G = 4, M = 8;

        private static readonly RastroLevel[] Levels =
        {
            //          nivel  familias         rastro revés gira marcha  de      chispa  espera  giro        cruces
            new RastroLevel(1,  R,               2, 0, 0, 0,  0, 0,   200f, 320f,   0f,   0f, 0),
            new RastroLevel(2,  R,               3, 0, 0, 0,  0, 0,   220f, 300f,   0f,   0f, 0),
            new RastroLevel(3,  R,               3, 0, 0, 0,  0, 0,   260f, 260f,   0f,   0f, 0),
            new RastroLevel(4,  R,               4, 0, 0, 0,  0, 0,   260f, 260f,   0f,   0f, 0),
            new RastroLevel(5,  R | V,           4, 3, 0, 0,  0, 0,   280f, 240f,   0f,   0f, 0),
            new RastroLevel(6,  R | V,           4, 4, 0, 0,  0, 0,   280f, 240f,   0f,   0f, 0),
            new RastroLevel(7,  R | V | G,       5, 4, 3, 0,  0, 0,   300f, 220f,  60f,  90f, 0),
            new RastroLevel(8,  R | V | G,       5, 4, 4, 0,  0, 0,   300f, 220f,  60f, 120f, 0),
            new RastroLevel(9,  R | V | G | M,   5, 4, 4, 2,  3, 5,   300f, 220f,  90f, 150f, 0),
            new RastroLevel(10, R | V | G | M,   5, 5, 4, 3,  4, 6,   320f, 200f,  90f, 150f, 1),
            new RastroLevel(11, R | V | G | M,   6, 5, 5, 3,  4, 6,   320f, 200f,  90f, 180f, 1),
            new RastroLevel(12, R | V | G | M,   6, 5, 5, 3,  5, 7,   340f, 190f,  90f, 180f, 2),
            new RastroLevel(13, R | V | G | M,   6, 6, 5, 4,  5, 7,   340f, 190f,  45f, 180f, 2),
            new RastroLevel(14, R | V | G | M,   7, 6, 6, 4,  5, 7,   360f, 180f,  45f, 180f, 2),
            new RastroLevel(15, R | V | G | M,   7, 6, 6, 4,  6, 8,   380f, 170f,  45f, 180f, CrossAny),
            new RastroLevel(16, R | V | G | M,   8, 7, 6, 5,  7, 9,   400f, 160f,  45f, 180f, CrossAny),
        };

        public static RastroLevel Get(int level) => Levels[Math.Max(1, Math.Min(MaxLevel, level)) - 1];

        /// <summary>Familias disponibles en el nivel, en orden (el rastro simple siempre está).</summary>
        public static RastroMode[] FamiliesAt(int level)
        {
            var l = Get(level);
            var list = new System.Collections.Generic.List<RastroMode>();
            foreach (var m in RastroModes.All) if (l.Has(m)) list.Add(m);
            return list.ToArray();
        }
    }

    /// <summary>Reglas puras de «Rastro de luz»: constantes, motor de dificultad común, puntaje y la telemetría de salida.</summary>
    public static class RastroContract
    {
        /// <summary>Mismo id de texto que <c>GameRegistry</c> del lado Kotlin: se mantiene «secuencia» para no perder avance ni historial.</summary>
        public const string GameId = "secuencia";
        public const int MaxLevel = RastroLadder.MaxLevel;

        public const int Lives = 3;
        public const float RetoSeconds = 90f;
        public const int PrecisionRounds = 14;
        /// <summary>Evaluación inicial: solo el rastro simple, hasta 2 errores o ~75 s.</summary>
        public const int AssessmentMaxErrors = 2;
        public const float AssessmentSeconds = 75f;
        /// <summary>Luces de la ronda guiada del tutorial.</summary>
        public const int GuidedLength = 2;
        /// <summary>Segundos que dura la pantalla «¡NUEVO!» (o hasta un toque).</summary>
        public const float UnlockSeconds = 2.3f;

        // Pasos de subida del motor común por edad. El motor multiplica por 0,85 en mayores y 1,1 en menores de 18, y baja
        // `paso · p / (1 − p)` por error con tope MaxDropPerError = 1: para que ese tope no se active (y el objetivo se cumpla),
        // el paso efectivo tiene que ser ≤ (1 − p) / p. Adultos: 0,25 × 4 = 1,0. Mayores: 0,17 × 0,85 × 5,67 = 0,82 → converge a 0,85
        // (con el 0,25 de la Secuencia anterior bajaba 1,0 por error y quedaba en ~0,82). Menores de 18: 0,22 × 1,1 × 4 = 0,97.
        public const float StepUpAdult = 0.25f;
        public const float StepUpSenior = 0.17f;
        public const float StepUpUnder18 = 0.22f;

        public static float StepUp(AgeBand age) => age == AgeBand.Senior ? StepUpSenior : age == AgeBand.Under18 ? StepUpUnder18 : StepUpAdult;

        /// <summary>Nivel (1..16) donde parte quien nunca jugó ni tiene rating guardado, y la evaluación inicial: 2 (1 en mayores).</summary>
        public static int FirstLevel(AgeBand age) => age == AgeBand.Senior ? 1 : 2;

        public static float StartRating(SequenceConfigDetails config, AgeBand age)
        {
            if (config.assessment || !config.has_dda_rating) return FirstLevel(age);
            return AdaptiveDifficulty.StartRating(config, MaxLevel);
        }

        /// <summary>El motor común sobre la escalera de 16 niveles. Cada ronda completa es UN ensayo (acierto si se repite entera
        /// bien). Sin tiempo de reacción: es capacidad de memoria, no velocidad. Las 3 vidas ya no deciden la dificultad.</summary>
        public static AdaptiveDifficulty CreateEngine(SequenceConfigDetails config)
        {
            var age = DdaUserProfileConfig.ParseAgeBand(config.age_band);
            return new AdaptiveDifficulty(MaxLevel, age, StartRating(config, age), StepUp(age), useReaction: false);
        }

        /// <summary>Puntaje 0-100 (el que ve la app): aciertos / rondas (70 %) y nivel más alto alcanzado (30 %), como los otros juegos estrella.</summary>
        public static int Score(int correctRounds, int totalRounds, int peakLevel)
        {
            float acc = totalRounds > 0 ? Math.Max(0f, Math.Min(1f, (float)correctRounds / totalRounds)) : 0f;
            float lv = (float)(Math.Max(1, Math.Min(MaxLevel, peakLevel)) - 1) / (MaxLevel - 1);
            return Math.Max(0, Math.Min(100, (int)Math.Round((0.7f * acc + 0.3f * lv) * 100f)));
        }

        /// <summary>Telemetría de salida: conserva <c>end_rating</c>, <c>peak_level</c>, <c>mode_trials</c> y <c>mode_hits</c> y agrega <c>ras_*</c>.</summary>
        public static SequenceSessionMetrics BuildMetrics(RastroSession s, double averageResponseMs, SequenceConfigDetails config)
        {
            var dda = s.Dda;
            var t = s.Tally;
            return new SequenceSessionMetrics
            {
                correct_rounds = t.TotalHits,
                total_rounds = t.TotalRounds,
                calculated_score = Score(t.TotalHits, t.TotalRounds, dda.PeakLevel),
                average_response_time_ms = averageResponseMs,
                final_span_length = RastroLadder.Get(dda.PeakLevel).LenRastro,
                level = config.level,
                timed = config.timed,
                peak_level = dda.PeakLevel,
                end_rating = dda.RatingNormalized,
                mode_trials = dda.ScoredTrials,
                mode_hits = dda.ScoredCorrect,
                ras_best_len = (int[])t.BestLength.Clone(),
                ras_rounds = (int[])t.Rounds.Clone(),
                ras_hits = (int[])t.Hits.Clone(),
                ras_modes_seen = t.ModesSeen,
                ras_new_modes = s.NewModes
            };
        }
    }
}
