using System;
using NeuroVida.Contracts;

namespace NeuroVida.Games.Radar
{
    /// <summary>El motor común de dificultad y la telemetría de Rescate relámpago (separados del contrato puro porque usan los tipos de la app).</summary>
    public static class RadarMetrics
    {
        /// <summary>Pocas rondas por partida (12 a 20): pasos grandes (0,3) y objetivo del motor común (~80 %); sin tiempo de reacción: acá cuenta lo que se ve, no lo rápido que se responde. Las lluvias no pasan por el motor.</summary>
        public const float StepUp = 0.3f;

        public static AdaptiveDifficulty CreateEngine(SequenceConfigDetails config) =>
            new AdaptiveDifficulty(RadarContract.MaxLevel, DdaUserProfileConfig.ParseAgeBand(config.age_band), StartRating(config), StepUp, useReaction: false);

        public static float StartRating(SequenceConfigDetails config) =>
            config.resc_stage > 0 ? Math.Max(1f, Math.Min(RadarContract.MaxLevel, config.resc_stage)) : AdaptiveDifficulty.StartRating(config, RadarContract.MaxLevel);

        public static StroopSessionMetrics Build(AdaptiveDifficulty dda, RadarRun run, int record, bool newRecord, SequenceConfigDetails config)
        {
            int glance = run.GlanceMs;
            return new StroopSessionMetrics
            {
                correct_trials = run.Successes,
                total_trials = run.NormalRounds,
                calculated_score = run.Score,
                average_response_time_ms = 0,
                level = config.level,
                timed = config.timed,
                end_rating = dda.RatingNormalized,
                mode_trials = dda.ScoredTrials,
                mode_hits = dda.ScoredCorrect,
                peak_level = dda.PeakLevel,
                glance_ms = glance,
                glance_load = glance > 0 ? run.GlanceLoad : -1f,
                capture = run.Capture,
                resc_rescued = run.Rescued,
                resc_perfect = run.Perfect,
                resc_rounds = run.RoundsPlayed,
                resc_best_streak = run.BestStreak,
                resc_shortest_ms = run.ShortestPerfectMs > 0 ? run.ShortestPerfectMs : -1,
                resc_best = record,
                resc_new = newRecord ? 1 : 0,
                resc_trips = run.Trips
            };
        }
    }
}
