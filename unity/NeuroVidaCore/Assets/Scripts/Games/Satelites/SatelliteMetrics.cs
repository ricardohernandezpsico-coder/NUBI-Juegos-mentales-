using System;
using NeuroVida.Contracts;

namespace NeuroVida.Games.Satelites
{
    /// <summary>El motor común de dificultad y la telemetría de Satélites (separados del contrato puro porque usan los tipos de la app).</summary>
    public static class SatelliteMetrics
    {
        /// <summary>Un ensayo por ronda (acierto = todos los k); sin tiempo de reacción: el tiempo es el de la tarea, no una medida.</summary>
        public static AdaptiveDifficulty CreateEngine(SequenceConfigDetails config) =>
            new AdaptiveDifficulty(SatelliteContract.MaxLevel, DdaUserProfileConfig.ParseAgeBand(config.age_band), StartRating(config), SatelliteContract.StepUp, useReaction: false);

        public static float StartRating(SequenceConfigDetails config) =>
            config.sat_stage > 0 ? Math.Max(1f, Math.Min(SatelliteContract.MaxLevel, config.sat_stage)) : AdaptiveDifficulty.StartRating(config, SatelliteContract.MaxLevel);

        public static StroopSessionMetrics Build(AdaptiveDifficulty dda, SatelliteRun run, int record, bool newRecord, SequenceConfigDetails config) =>
            new StroopSessionMetrics
            {
                correct_trials = run.Perfect,
                total_trials = run.RoundsPlayed,
                calculated_score = run.Score,
                average_response_time_ms = 0,
                level = config.level,
                timed = config.timed,
                end_rating = dda.RatingNormalized,
                mode_trials = dda.ScoredTrials,
                mode_hits = dda.ScoredCorrect,
                peak_level = Math.Max(dda.PeakLevel, run.BestCleared),
                tracking_capacity = run.Capacity,
                tracking_targets = run.MeanTargets,
                tracking_speed = run.SpeedReached,
                sat_lights = run.Lights,
                sat_perfect = run.Perfect,
                sat_best_streak = run.BestStreak,
                sat_best = record,
                sat_new = newRecord ? 1 : 0
            };
    }
}
