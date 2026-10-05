using System;
using System.Collections.Generic;
using NeuroVida.Contracts;

namespace NeuroVida.Games.Parejas
{
    /// <summary>Puerto 1:1 de <c>CountdownReason</c> (Kotlin) -- distingue arranque/avance
    /// normal de una bajada o repetición de nivel por fallas, para que el cambio de nivel
    /// hacia atrás se lea como mecánica del juego, no como un bug.</summary>
    public enum CountdownReason
    {
        Start,
        Advance,
        Demoted,
        Retry
    }

    /// <summary>
    /// La escalera de Parejas Ocultas y su conexión con el DDA común (<see cref="AdaptiveDifficulty"/>, ver
    /// docs/DDA-comun.md §6). Niveles 1..<see cref="MaxLevel"/>: cada nivel fija la cantidad de parejas
    /// (<see cref="PairCountForStage"/>), la grilla y el banco de símbolos (<see cref="InterferenceForLevel"/>).
    /// El motor común decide en qué nivel se arma el SIGUIENTE tablero; dentro de un tablero el nivel no cambia.
    /// La cantidad de parejas nunca se afina "a ojo": sube con la escalera, así el nivel 2 nunca es más fácil que
    /// el 1 (Ricardo lo notó jugando la versión Kotlin, cuando todavía salía de D(t)).
    /// </summary>
    public static class CardsGameContract
    {
        /// <summary>Ritmo de referencia para el bono de velocidad: 4s por pareja.</summary>
        public const long ExpectedMsPerPair = 4000L;

        /// <summary>Niveles de la escalera (el rating del DDA común va de 1 a este número).</summary>
        public const int MaxLevel = 10;

        /// <summary>Cuántos tableros arma una partida antes de mostrar el resultado final (cuentan también los
        /// tableros cortados por 3 parejas erradas). Antes la partida terminaba al superar el nivel 10; con el
        /// nivel decidido por el motor hace falta otro límite para que no sea infinita.</summary>
        public const int BoardsPerSession = 10;

        /// <summary>Paso de subida del DDA por pareja acertada (≈7 aciertos por nivel). Con objetivo 0,80 la bajada
        /// por error es 4 veces más (0,60 niveles).</summary>
        public const float StepUp = 0.15f;

        private static readonly int[] PairCountsByStage = { 2, 3, 4, 5, 6, 7, 8, 9, 10, 12 };

        public static int PairCountForStage(int stage)
        {
            int index = stage - 1;
            if (index < 0) index = 0;
            if (index > PairCountsByStage.Length - 1) index = PairCountsByStage.Length - 1;
            return PairCountsByStage[index];
        }

        /// <summary>Banco de símbolos del nivel (0..3, ver <see cref="SymbolBank"/>): 1-3 → 0, 4-6 → 1, 7-9 → 2, 10 → 3.</summary>
        public static int InterferenceForLevel(int level) => Math.Max(0, Math.Min(3, (level - 1) / 3));

        /// <summary>Motor común de la partida: escalera 1..<see cref="MaxLevel"/>, parte del rating guardado o del
        /// nivel elegido, usa el perfil de edad y respeta techo y piso del modo (ya fijados con
        /// <see cref="AdaptiveDifficulty.ConfigureMode"/>). El tiempo de reacción (primera a segunda carta de cada
        /// intento) solo pesa en Reto.</summary>
        public static AdaptiveDifficulty CreateEngine(SequenceConfigDetails config) =>
            new AdaptiveDifficulty(MaxLevel, DdaUserProfileConfig.ParseAgeBand(config.age_band),
                AdaptiveDifficulty.StartRating(config, MaxLevel), StepUp, useReaction: config.timed);

        /// <summary>Sistema de "3 fallas": fallar <see cref="MismatchesToFailStage"/>
        /// parejas en el tablero actual corta ese tablero ahí mismo y cuenta como una
        /// falla de nivel. Tres fallas seguidas (sin completar un tablero entre medio) terminan la partida; qué nivel
        /// sigue lo decide el motor común, no la falla.</summary>
        public const int MismatchesToFailStage = 3;
        public const int StageFailuresBeforeGameOver = 3;

        /// <summary>Telemetría de salida: <c>end_rating</c> es el rating del motor común (0..1) que la app guarda.</summary>
        public static CardsSessionMetrics BuildMetrics(AdaptiveDifficulty dda, int matchedPairs, int attempts, int score,
            SequenceConfigDetails config) => new CardsSessionMetrics
        {
            matched_pairs = matchedPairs,
            attempts = attempts,
            calculated_score = score,
            level = config.level,
            timed = config.timed,
            end_rating = dda.RatingNormalized,
            peak_level = dda.PeakLevel,
            mode_trials = dda.ScoredTrials,
            mode_hits = dda.ScoredCorrect
        };
    }

    /// <summary>Lo que fija el nivel del tablero (<see cref="Build"/>): grilla, símbolos y, con el rating continuo
    /// del motor (0..1), la vista previa, los distractores de fondo y su opacidad. Son las mismas fórmulas del motor
    /// anterior (<c>VisualWorkingMemoryDDA</c>, borrado): solo cambia de dónde sale el índice.</summary>
    public readonly struct CardsBoardProfile
    {
        public readonly int Level;
        public readonly int PairCount;
        public readonly int GridColumns;
        public readonly int GridRows;
        public readonly int InterferenceLevel;
        public readonly long PreviewExposureMs;
        public readonly int DistractorCount;
        public readonly float DistractorOpacity;

        private const float PreviewMaxMs = 3000f;
        private const int MaxDistractors = 6;
        private const float MinDistractorAlpha = 0.03f;
        private const float MaxDistractorAlpha = 0.12f;

        private CardsBoardProfile(int level, int pairCount, int columns, int rows, int interference, long previewMs, int distractors, float opacity)
        {
            Level = level;
            PairCount = pairCount;
            GridColumns = columns;
            GridRows = rows;
            InterferenceLevel = interference;
            PreviewExposureMs = previewMs;
            DistractorCount = distractors;
            DistractorOpacity = opacity;
        }

        /// <param name="level">Nivel del tablero (1..MaxLevel).</param>
        /// <param name="fineIndex">Rating normalizado del motor (0..1), para lo que varía de forma continua.</param>
        /// <param name="previewFloorMs">Vista previa mínima del perfil de edad.</param>
        public static CardsBoardProfile Build(int level, float fineIndex, float previewFloorMs)
        {
            float d = fineIndex < 0f ? 0f : fineIndex > 1f ? 1f : fineIndex;
            int pairs = CardsGameContract.PairCountForStage(level);
            var (columns, rows) = GridDimensionsFor(pairs);
            return new CardsBoardProfile(level, pairs, columns, rows, CardsGameContract.InterferenceForLevel(level),
                (long)(PreviewMaxMs - d * (PreviewMaxMs - previewFloorMs)),
                (int)(d * MaxDistractors),
                MinDistractorAlpha + d * (MaxDistractorAlpha - MinDistractorAlpha));
        }

        private static int ColumnsFor(int pairCount)
        {
            if (pairCount <= 4) return 2;
            if (pairCount <= 6) return 3;
            if (pairCount <= 12) return 4;
            if (pairCount <= 15) return 5;
            return 6;
        }

        public static (int columns, int rows) GridDimensionsFor(int pairCount)
        {
            int columns = ColumnsFor(pairCount);
            int rows = (int)Math.Ceiling((pairCount * 2) / (double)columns);
            return (columns, rows);
        }
    }

    /// <summary>Un símbolo de carta: ícono ilustrado + variante de color (ver
    /// <see cref="SymbolSprite"/>). <see cref="Key"/> es el identificador estable usado
    /// para detectar pares (equivalente al emoji original, que servía como su propia
    /// clave por ser un string).</summary>
    public readonly struct SymbolDef
    {
        public readonly ShapeKind Shape;
        public readonly int Variant;
        public readonly string Key;

        public SymbolDef(ShapeKind shape, int variant)
        {
            Shape = shape;
            Variant = variant;
            Key = $"{shape}:{variant}";
        }
    }

    /// <summary>Puerto de <c>SymbolBank</c> (Kotlin) -- bancos de símbolos graduados por
    /// similitud perceptual (0 = muy distintos entre sí, 3 = máxima interferencia),
    /// impulsado por <c>DifficultyProfile.InterferenceLevel</c>.
    ///
    /// La versión Kotlin usa emojis de color (el font system de Android los renderiza
    /// nativo). Acá son íconos ilustrados procedurales (<see cref="SymbolSprite"/>): la
    /// fuente legacy de Unity (<c>LegacyRuntime.ttf</c>) no tiene glifos de emoji, así que
    /// las cartas se veían vacías (bug reportado por Ricardo jugando en el dispositivo,
    /// 23-sep).
    ///
    /// Los objetos son del cielo nocturno (planeta, cohete, cometa...; ver <see cref="ShapeKind"/>).
    ///
    /// Gradiente de dificultad, siempre con colores vívidos (Ricardo: "los símbolos son
    /// muy opacos, oscuros, difíciles de recordar"): tier 0 = objetos y colores muy
    /// distintos; tier 1 = más variedad de objetos; tier 2 = mismo objeto en dos colores
    /// distintos (hay que recordar objeto Y color); tier 3 = mismo objeto en dos tonos
    /// análogos (variantes 0 y 3 de cada ícono).</summary>
    public static class SymbolBank
    {
        private static readonly SymbolDef[][] Tiers =
        {
            // Tier 0: 8 objetos de siluetas muy distintas, de colores distintos.
            new[]
            {
                new SymbolDef(ShapeKind.Planet, 0),
                new SymbolDef(ShapeKind.Rocket, 0),
                new SymbolDef(ShapeKind.Galaxy, 0),
                new SymbolDef(ShapeKind.Comet, 0),
                new SymbolDef(ShapeKind.Ufo, 0),
                new SymbolDef(ShapeKind.Crystal, 1),
                new SymbolDef(ShapeKind.Helmet, 0),
                new SymbolDef(ShapeKind.FullMoon, 2),
            },
            // Tier 1: más objetos, algunos repiten forma con otro color.
            new[]
            {
                new SymbolDef(ShapeKind.Sun, 0),
                new SymbolDef(ShapeKind.Satellite, 0),
                new SymbolDef(ShapeKind.Asteroid, 0),
                new SymbolDef(ShapeKind.Telescope, 1),
                new SymbolDef(ShapeKind.Constellation, 0),
                new SymbolDef(ShapeKind.FullMoon, 1),
                new SymbolDef(ShapeKind.Planet, 1),
                new SymbolDef(ShapeKind.Rocket, 1),
                new SymbolDef(ShapeKind.Comet, 1),
                new SymbolDef(ShapeKind.Ufo, 1),
            },
            // Tier 2: pares del mismo objeto en dos colores claramente distintos.
            new[]
            {
                new SymbolDef(ShapeKind.Planet, 0),
                new SymbolDef(ShapeKind.Planet, 2),
                new SymbolDef(ShapeKind.Rocket, 0),
                new SymbolDef(ShapeKind.Rocket, 2),
                new SymbolDef(ShapeKind.Crystal, 0),
                new SymbolDef(ShapeKind.Crystal, 1),
                new SymbolDef(ShapeKind.Ufo, 0),
                new SymbolDef(ShapeKind.Ufo, 2),
            },
            // Tier 3 (máxima interferencia): pares del mismo objeto en tonos análogos.
            new[]
            {
                new SymbolDef(ShapeKind.Galaxy, 0),
                new SymbolDef(ShapeKind.Galaxy, 3),
                new SymbolDef(ShapeKind.Planet, 0),
                new SymbolDef(ShapeKind.Planet, 3),
                new SymbolDef(ShapeKind.Comet, 0),
                new SymbolDef(ShapeKind.Comet, 3),
                new SymbolDef(ShapeKind.Crystal, 0),
                new SymbolDef(ShapeKind.Crystal, 3),
            },
        };

        public static List<SymbolDef> Select(int pairCount, int interferenceLevel, System.Random random)
        {
            int level = interferenceLevel;
            if (level < 0) level = 0;
            if (level > Tiers.Length - 1) level = Tiers.Length - 1;

            var pool = new List<SymbolDef>();
            var seenKeys = new HashSet<string>();
            void AddFromTier(int tier)
            {
                foreach (var symbol in Shuffled(Tiers[tier], random))
                {
                    if (pool.Count >= pairCount) return;
                    if (!seenKeys.Add(symbol.Key)) continue;
                    pool.Add(symbol);
                }
            }

            for (int tier = level; tier >= 0 && pool.Count < pairCount; tier--) AddFromTier(tier);
            // La escalera fija de niveles (CardsGameContract.PairCountForStage) llega a
            // pairCount=12, pero con interferenceLevel bajo (0 o 1) los tiers 0..level solos
            // no siempre alcanzan esa cantidad (tier 0 solo tiene 8 símbolos distintos).
            // Antes esto dejaba el mazo con MENOS pares de los que _totalPairs esperaba --
            // el tablero nunca podía completarse (partida "pegada", Ricardo lo vio cerca del
            // nivel 8) y sobraban casillas de grilla sin carta asignada. Completar con los
            // tiers restantes (aunque sean "más difíciles") garantiza siempre pairCount
            // símbolos -- más de 25 distintos entre los 4 tiers, contra un máximo de 12.
            for (int tier = level + 1; tier < Tiers.Length && pool.Count < pairCount; tier++) AddFromTier(tier);

            return pool;
        }

        private static IEnumerable<SymbolDef> Shuffled(SymbolDef[] source, System.Random random)
        {
            var list = new List<SymbolDef>(source);
            for (int i = list.Count - 1; i > 0; i--)
            {
                int j = random.Next(i + 1);
                (list[i], list[j]) = (list[j], list[i]);
            }
            return list;
        }
    }
}
