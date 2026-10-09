using System;
using NeuroVida.Contracts;

namespace NeuroVida.Games.Piloto
{
    /// <summary>
    /// Los dos motores comunes de dificultad de Piloto Estelar (pilotaje y señales; los dos arrancan del rating guardado de la persona) y la telemetría de la partida. Separado del contrato puro porque usa los tipos de la app.
    /// Todo lo que se informa se midió con las dos tareas juntas: no hay una medida de una tarea sola (regla permanente 1 de Ricardo, 9-oct).
    /// </summary>
    public static class PilotMetrics
    {
        public static float StartRating(SequenceConfigDetails config) =>
            config.pil_stage > 0 ? Math.Max(1f, Math.Min(PilotContract.MaxLevel, config.pil_stage)) : AdaptiveDifficulty.StartRating(config, PilotContract.MaxLevel);

        /// <summary>Pilotaje: un ensayo por ventana de 1,5 s, objetivo 85 %, sin tiempo de reacción.</summary>
        public static AdaptiveDifficulty CreateDriveEngine(SequenceConfigDetails config) =>
            new AdaptiveDifficulty(PilotContract.MaxLevel, DdaUserProfileConfig.ParseAgeBand(config.age_band), StartRating(config), PilotContract.DriveStepUp, PilotContract.DriveTarget, useReaction: false);

        /// <summary>Señales: un ensayo por señal resuelta, objetivo 80 %, sin tiempo de reacción (el tiempo lo fija la exposición de la señal, no la prisa de la persona).</summary>
        public static AdaptiveDifficulty CreateSignalEngine(SequenceConfigDetails config) =>
            new AdaptiveDifficulty(PilotContract.MaxLevel, DdaUserProfileConfig.ParseAgeBand(config.age_band), StartRating(config), PilotContract.SignalStepUp, PilotContract.SignalTarget, useReaction: false);

        public static StroopSessionMetrics Build(AdaptiveDifficulty drive, AdaptiveDifficulty signal, PilotRun run, SequenceConfigDetails config)
        {
            // Desafío superado en Piloto = las dos tareas: se informa la de menos aciertos (pilotaje o señales)
            var weaker = PilotContract.WeakerOf(drive.ScoredCorrect, drive.ScoredTrials, signal.ScoredCorrect, signal.ScoredTrials);
            float? score = run.SignalScore;
            return new StroopSessionMetrics
            {
                correct_trials = run.WindowsPassed + run.Hits + (run.NonTargets - run.FalseAlarms),
                total_trials = run.Windows + run.Resolved,
                calculated_score = run.Score,
                average_response_time_ms = 0,
                level = config.level,
                timed = config.timed,
                end_rating = (drive.RatingNormalized + signal.RatingNormalized) * 0.5f,
                mode_trials = weaker.trials,
                mode_hits = weaker.hits,
                peak_level = Math.Max(drive.PeakLevel, signal.PeakLevel),
                pil_lane_pct = (int)Math.Round(run.InLaneFraction * 100f),
                pil_hits = run.Hits,
                pil_targets = run.Targets,
                pil_false = run.FalseAlarms,
                pil_signal_pct = score.HasValue ? (int)Math.Round(score.Value * 100f) : -1,
                pil_signal_level = signal.Level,
                pil_drive_level = drive.Level,
                pil_best_streak = run.BestStreak,
                pil_points = run.Points,
                pil_hyper = run.HyperCount
            };
        }
    }
}
