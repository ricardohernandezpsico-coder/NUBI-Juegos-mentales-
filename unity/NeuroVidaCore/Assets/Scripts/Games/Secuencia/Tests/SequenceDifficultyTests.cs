using NUnit.Framework;
using NeuroVida.Contracts;

namespace NeuroVida.Games.Secuencia.Tests
{
    /// <summary>
    /// Secuencia Lumínica sobre el DDA común (docs/DDA-comun.md §6): los 16 niveles son la escalera, cada secuencia
    /// completa es un ensayo, el motor converge al objetivo, respeta techo y piso del modo, parte del rating guardado
    /// y deja <c>end_rating</c> en la telemetría. Las vidas no intervienen: no hay nada de ellas en el motor.
    /// </summary>
    public class SequenceDifficultyTests
    {
        private static SequenceConfigDetails Config(string age = "ADULT", int level = 3, float? saved = null, bool assessment = false) =>
            new SequenceConfigDetails
            {
                level = level, age_band = age, assessment = assessment,
                has_dda_rating = saved.HasValue, dda_rating = saved ?? 0f
            };

        /// <summary>Usuario simulado: cada secuencia se acierta con probabilidad logística según la distancia entre su
        /// habilidad y el nivel presentado. Sin tiempo de reacción (el motor de Secuencia no lo usa).</summary>
        private static (float accuracy, float meanLevel) Simulate(AdaptiveDifficulty dda, float ability, int trials, int seed)
        {
            var rng = new System.Random(seed);
            int correct = 0, counted = 0;
            double levelSum = 0;
            for (int i = 0; i < trials; i++)
            {
                int level = dda.PresentedLevel;
                double p = 1.0 / (1.0 + System.Math.Exp(-1.2 * (ability - level)));
                bool ok = rng.NextDouble() < p;
                dda.Register(ok);
                if (i >= trials / 2) { counted++; if (ok) correct++; levelSum += dda.Level; }
            }
            return ((float)correct / counted, (float)(levelSum / counted));
        }

        [Test]
        public void TheLadder_Is16LevelsThatNeverGetEasier()
        {
            Assert.AreEqual(16, SequenceDifficulty.MaxLevel);
            for (int level = 2; level <= SequenceDifficulty.MaxLevel; level++)
            {
                var prev = SequenceLevelDatabase.Get(level - 1);
                var cur = SequenceLevelDatabase.Get(level);
                Assert.GreaterOrEqual(cur.SequenceLength, prev.SequenceLength, $"nivel {level}: secuencia más corta que la del anterior");
                Assert.GreaterOrEqual(cur.TotalTiles, prev.TotalTiles, $"nivel {level}: menos fichas que el anterior");
            }
        }

        [Test]
        public void Engine_UsesThe16LevelLadderAndTheCommonAgeTargets()
        {
            var adult = SequenceDifficulty.CreateEngine(Config("ADULT"));
            var senior = SequenceDifficulty.CreateEngine(Config("SENIOR"));
            Assert.AreEqual(16, adult.MaxLevel);
            Assert.AreEqual(0.80f, adult.TargetAccuracy, 1e-4f, "ya no hay objetivo propio de 0,707 (la regla vieja era 2 seguidos)");
            Assert.AreEqual(0.85f, senior.TargetAccuracy, 1e-4f);
        }

        [Test]
        public void ReactionTimeIsNotUsed()
        {
            // Mismo resultado aunque el tiempo sea muy distinto: la memoria no se mide por rapidez.
            var a = SequenceDifficulty.CreateEngine(Config(saved: 0.5f));
            var b = SequenceDifficulty.CreateEngine(Config(saved: 0.5f));
            for (int i = 0; i < 12; i++) { a.Register(i % 3 != 0, 300f); b.Register(i % 3 != 0, 9000f); }
            Assert.AreEqual(a.Rating, b.Rating, 1e-6f);
        }

        [Test]
        public void Simulation_ConvergesToTheTargetAccuracy_AndStrongerPlayersEndHigher()
        {
            foreach (float ability in new[] { 4f, 8f, 12f })
            {
                var (acc, level) = Simulate(SequenceDifficulty.CreateEngine(Config(level: 1)), ability, 2000, 23);
                Assert.That(acc, Is.InRange(0.70f, 0.90f), $"habilidad {ability}: acierto {acc:0.00}");
                Assert.That(level, Is.InRange(ability - 3f, ability + 0.6f), $"habilidad {ability}: nivel {level:0.0}");
            }
            var (_, low) = Simulate(SequenceDifficulty.CreateEngine(Config(level: 1)), 4f, 1500, 9);
            var (_, high) = Simulate(SequenceDifficulty.CreateEngine(Config(level: 1)), 12f, 1500, 9);
            Assert.Greater(high, low + 4f);
        }

        [Test]
        public void StartsFromTheSavedRating_OrFromTheChosenLevel()
        {
            var saved = SequenceDifficulty.CreateEngine(Config(saved: 0.5f));
            Assert.AreEqual(9f, saved.Rating, 1e-4f); // 1 + 0,5 × 16
            Assert.AreEqual(8, saved.PresentedLevel, "el calentamiento presenta un nivel por debajo");
            Assert.AreEqual(0.5f, saved.RatingNormalized, 1e-4f);
            var chosen = SequenceDifficulty.CreateEngine(Config(level: 1));
            Assert.AreEqual(1f, chosen.Rating, 1e-4f);
        }

        [Test]
        public void AnOldSavedRating_KeepsItsMeaning()
        {
            // El rating viejo ya era 0..1 sobre los mismos 16 niveles ((pico-1)/15): se continúa desde ahí sin convertir.
            float oldRating = (9 - 1) / 15f;
            float rating = SequenceDifficulty.CreateEngine(Config(saved: oldRating)).Rating;
            Assert.That(rating, Is.InRange(9f, 10f)); // cae en el nivel del pico (9), a menos de un nivel
        }

        [Test]
        public void AnErrorLowersTheRating_ButTheLevelOnlyChangesWhenItCrossesAWholeNumber()
        {
            var dda = SequenceDifficulty.CreateEngine(Config(saved: 0.5f));
            for (int i = 0; i < 10; i++) dda.Register(true);
            int before = dda.Level;
            float r = dda.Rating;
            var change = dda.Register(false);
            Assert.Less(dda.Rating, r);
            Assert.AreEqual(change == DdaChange.Down, dda.Level < before);
        }

        [Test]
        public void ModeFloorAndCeiling_BoundTheLevel()
        {
            try
            {
                AdaptiveDifficulty.ConfigureMode(new SequenceConfigDetails { mode_floor = 0.5f }); // Desafío
                var hard = SequenceDifficulty.CreateEngine(Config(level: 1));
                for (int i = 0; i < 40; i++) hard.Register(false);
                Assert.GreaterOrEqual(hard.PresentedLevel, 9, "con piso 0,5 de 16 niveles nunca baja del 9");

                AdaptiveDifficulty.ConfigureMode(new SequenceConfigDetails { mode_ceiling = 0.25f }); // Suave
                var soft = SequenceDifficulty.CreateEngine(Config(saved: 0.9f));
                for (int i = 0; i < 120; i++) soft.Register(true);
                Assert.LessOrEqual(soft.Level, 5, "con techo 0,25 de 16 niveles nunca pasa del 5");
            }
            finally
            {
                AdaptiveDifficulty.ConfigureMode(null);
            }
        }

        [Test]
        public void TheEvaluation_StartsInTheMiddleAndCalibratesFast()
        {
            try
            {
                AdaptiveDifficulty.FastCalibration = true; // lo fija Assessment.Configure
                var cfg = Config(level: 3, assessment: true);
                var fast = SequenceDifficulty.CreateEngine(cfg);
                Assert.AreEqual(8.5f, fast.Rating, 1e-4f); // 1 + 2 × 15/4: el medio de la escala
                float start = fast.Rating;
                fast.Register(true);
                AdaptiveDifficulty.FastCalibration = false;
                var normal = SequenceDifficulty.CreateEngine(cfg);
                normal.Register(true);
                Assert.Greater(fast.Rating - start, normal.Rating - start, "la evaluación da pasos más grandes");
            }
            finally
            {
                AdaptiveDifficulty.FastCalibration = false;
            }
        }

        [Test]
        public void TheTelemetryCarriesEndRatingPeakAndModeCounts_AndStillThePeakLevel()
        {
            var cfg = Config(saved: 0.4f);
            var dda = SequenceDifficulty.CreateEngine(cfg);
            for (int i = 0; i < 12; i++) dda.Register(i % 4 != 3);
            var m = SequenceDifficulty.BuildMetrics(dda, 9, 12, 70, 640.0, cfg);
            Assert.AreEqual(dda.RatingNormalized, m.end_rating, 1e-6f);
            Assert.AreEqual(dda.PeakLevel, m.peak_level, "peak_level se mantiene por compatibilidad");
            Assert.AreEqual(SequenceLevelDatabase.Get(dda.PeakLevel).SequenceLength, m.final_span_length);
            Assert.AreEqual(10, m.mode_trials); // 12 secuencias menos las 2 del calentamiento
            string json = UnityEngine.JsonUtility.ToJson(new SequenceTelemetry { user_id = "u", game_id = "secuencia", session_metrics = m });
            StringAssert.Contains("\"end_rating\":", json);
            StringAssert.Contains("\"peak_level\":", json);
            var back = UnityEngine.JsonUtility.FromJson<SequenceTelemetry>(json);
            Assert.AreEqual(m.end_rating, back.session_metrics.end_rating, 1e-6f);
        }
    }
}
