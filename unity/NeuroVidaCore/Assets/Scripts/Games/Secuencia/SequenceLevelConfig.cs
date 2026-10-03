using System.Collections.Generic;
using NeuroVida.Contracts;
using UnityEngine;

namespace NeuroVida.Games.Secuencia
{
    /// <summary>
    /// Cuarta pasada de rediseño (22-sep, spec detallado de Ricardo: "esquema extendido
    /// de progresión multinivel", 16 niveles en 5 fases): el motor de dificultad de este juego (reemplazó al
    /// ajuste continuo por ventana deslizante del motor anterior, que se borró el 26-sep).
    ///
    /// Cada nivel fija cuadrícula (rows x cols, no necesariamente cuadrada), longitud de
    /// secuencia (span) y velocidad de presentación (ISI) -- valores tomados literal del
    /// esquema que compartió Ricardo. Los niveles 17+ (no definidos explícitamente en el
    /// spec, que dice "16+") se generan por extrapolación en <see cref="SequenceLevelDatabase.Get"/>.
    /// </summary>
    public readonly struct SequenceLevelConfig
    {
        public readonly int LevelIndex;
        public readonly int GridRows;
        public readonly int GridCols;
        public readonly int SequenceLength;
        public readonly float IsiMs;
        public readonly bool HasAudioDistractor;
        public readonly bool HasVisualDistractor;

        public int TotalTiles => GridRows * GridCols;

        public SequenceLevelConfig(int levelIndex, int gridRows, int gridCols, int sequenceLength, float isiMs, bool hasAudioDistractor = false, bool hasVisualDistractor = false)
        {
            LevelIndex = levelIndex;
            GridRows = gridRows;
            GridCols = gridCols;
            SequenceLength = sequenceLength;
            IsiMs = isiMs;
            HasAudioDistractor = hasAudioDistractor;
            HasVisualDistractor = hasVisualDistractor;
        }
    }

    public static class SequenceLevelDatabase
    {
        /// <summary>Último nivel definido literal en la tabla del spec -- de acá para
        /// arriba, <see cref="Get"/> extrapola (ver comentario ahí).</summary>
        public const int MaxDefinedLevel = 16;

        private static readonly SequenceLevelConfig[] Levels =
        {
            // Fase 1: Calentamiento y ritmo base
            new SequenceLevelConfig(1, 2, 2, 3, 1000f),
            new SequenceLevelConfig(2, 2, 2, 3, 800f),
            new SequenceLevelConfig(3, 2, 2, 4, 900f),
            // Fase 2: Consolidación y expansión del campo visual
            new SequenceLevelConfig(4, 2, 3, 4, 800f),
            new SequenceLevelConfig(5, 2, 3, 5, 850f),
            new SequenceLevelConfig(6, 2, 3, 5, 700f),
            // Fase 3: Desafío visoespacial intermedio
            new SequenceLevelConfig(7, 3, 3, 5, 750f),
            new SequenceLevelConfig(8, 3, 3, 6, 800f),
            new SequenceLevelConfig(9, 3, 3, 6, 650f),
            // Fase 4: Carga alta de memoria de trabajo
            new SequenceLevelConfig(10, 3, 4, 6, 700f),
            new SequenceLevelConfig(11, 3, 4, 7, 650f),
            new SequenceLevelConfig(12, 3, 4, 7, 550f, hasAudioDistractor: true), // tono de fondo
            // Fase 5: Nivel experto / gran maestro
            new SequenceLevelConfig(13, 4, 4, 7, 500f, hasAudioDistractor: true), // tono de fondo
            new SequenceLevelConfig(14, 4, 4, 8, 450f, hasAudioDistractor: true), // interferencia auditiva
            new SequenceLevelConfig(15, 4, 4, 8, 400f, hasVisualDistractor: true), // destello ambiental
            new SequenceLevelConfig(16, 4, 4, 9, 350f, hasAudioDistractor: true, hasVisualDistractor: true), // múltiples distractores
        };

        private const int ExtrapolatedIsiFloorMs = 350; // "velocidad límite de procesamiento perceptivo" (spec)
        private const int ExtrapolatedMaxSequenceLength = 16; // techo propio (no pedido), evita secuencias absurdas

        /// <summary>Nivel 1-16 literal de la tabla; 17+ extrapola manteniendo la 4x4 y el
        /// piso de ISI del nivel 16, sumando 1 luz cada 2 niveles (mismo espíritu que
        /// "Nivel 16+: Secuencia de 9+ luces" del spec, que no define una tabla exacta
        /// más allá de ahí).</summary>
        public static SequenceLevelConfig Get(int levelIndex)
        {
            if (levelIndex <= MaxDefinedLevel) return Levels[Mathf.Clamp(levelIndex, 1, MaxDefinedLevel) - 1];

            int stepsBeyond = levelIndex - MaxDefinedLevel;
            int sequenceLength = Mathf.Min(9 + stepsBeyond / 2, ExtrapolatedMaxSequenceLength);
            return new SequenceLevelConfig(levelIndex, 4, 4, sequenceLength, ExtrapolatedIsiFloorMs, hasAudioDistractor: true, hasVisualDistractor: true);
        }
    }

    /// <summary>
    /// Conexión de Secuencia Lumínica con el DDA común (<see cref="AdaptiveDifficulty"/>, docs/DDA-comun.md §6):
    /// los 16 niveles de <see cref="SequenceLevelDatabase"/> son la escalera (el motor elige el nivel de cada
    /// secuencia; el nivel fija cuadrícula, largo, velocidad y distractores). Cada secuencia completa es UN ensayo:
    /// acierto si la repite entera bien. NO se usa el tiempo de reacción: es una tarea de capacidad de memoria, no
    /// de velocidad (como Anagramas o Ruta del Tesoro), y el tiempo entre toques depende del largo de la secuencia
    /// y de recordar el primer toque más que de qué tan bien se sabe; el promedio entre toques sigue yendo al
    /// puntaje y a <c>average_response_time_ms</c>. Las 3 vidas ya no tocan la dificultad: solo terminan la partida.
    /// </summary>
    public static class SequenceDifficulty
    {
        public const int MaxLevel = SequenceLevelDatabase.MaxDefinedLevel;

        /// <summary>Paso de subida por secuencia acertada. Con objetivo 0,80 la bajada por error es 4 veces más,
        /// justo el tope de 1 nivel por error (0,25 × 4 = 1,0): el equilibrio queda en el objetivo. Antes subía un
        /// nivel cada 2 aciertos seguidos (0,5 por acierto, ≈71 %).</summary>
        public const float StepUp = 0.25f;

        public static AdaptiveDifficulty CreateEngine(SequenceConfigDetails config) =>
            new AdaptiveDifficulty(MaxLevel, DdaUserProfileConfig.ParseAgeBand(config.age_band),
                AdaptiveDifficulty.StartRating(config, MaxLevel), StepUp, useReaction: false);

        /// <summary>Telemetría de salida: <c>end_rating</c> es el rating del motor común (0..1) que la app guarda;
        /// <c>peak_level</c> se mantiene por compatibilidad.</summary>
        public static SequenceSessionMetrics BuildMetrics(AdaptiveDifficulty dda, int correctRounds, int totalRounds, int score,
            double averageResponseMs, SequenceConfigDetails config) => new SequenceSessionMetrics
        {
            correct_rounds = correctRounds,
            total_rounds = totalRounds,
            calculated_score = score,
            average_response_time_ms = averageResponseMs,
            final_span_length = SequenceLevelDatabase.Get(dda.PeakLevel).SequenceLength,
            level = config.level,
            timed = config.timed,
            peak_level = dda.PeakLevel,
            end_rating = dda.RatingNormalized,
            mode_trials = dda.ScoredTrials,
            mode_hits = dda.ScoredCorrect
        };
    }
}
