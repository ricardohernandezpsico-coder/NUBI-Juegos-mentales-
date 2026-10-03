using NUnit.Framework;
using NeuroVida.Contracts;

namespace NeuroVida.Games.Parejas.Tests
{
    /// <summary>
    /// Parejas Ocultas sobre el DDA común (docs/DDA-comun.md §6): la escalera de 10 niveles, lo que fija cada nivel
    /// (grilla, símbolos) y lo que sigue el rating continuo (vista previa, distractores), y que el motor converja al
    /// objetivo, respete techo y piso del modo, parta del rating guardado y salga en la telemetría.
    /// </summary>
    public class CardsDifficultyTests
    {
        private static SequenceConfigDetails Config(string age = "ADULT", bool timed = true, int level = 3, float? saved = null) =>
            new SequenceConfigDetails
            {
                level = level, age_band = age, timed = timed,
                has_dda_rating = saved.HasValue, dda_rating = saved ?? 0f
            };

        /// <summary>Usuario simulado: cada pareja intentada acierta con probabilidad logística según la distancia entre
        /// su habilidad y el nivel del tablero en curso (el nivel solo cambia entre tableros: se simula con tableros
        /// de <paramref name="boardSize"/> intentos).</summary>
        private static (float accuracy, float meanLevel) Simulate(AdaptiveDifficulty dda, float ability, int boards, int seed, int boardSize = 8)
        {
            var rng = new System.Random(seed);
            int correct = 0, counted = 0;
            double levelSum = 0;
            for (int b = 0; b < boards; b++)
            {
                int level = dda.PresentedLevel; // se fija al armar el tablero
                double p = 1.0 / (1.0 + System.Math.Exp(-1.6 * (ability - level)));
                for (int i = 0; i < boardSize; i++)
                {
                    bool ok = rng.NextDouble() < p;
                    dda.Register(ok, 1500f + (float)rng.NextDouble() * 800f);
                    if (b >= boards / 2) { counted++; if (ok) correct++; levelSum += level; }
                }
            }
            return ((float)correct / counted, (float)(levelSum / counted));
        }

        [Test]
        public void TheLadder_HasTenLevelsWithStrictlyMorePairsEachTime()
        {
            Assert.AreEqual(10, CardsGameContract.MaxLevel);
            for (int level = 2; level <= CardsGameContract.MaxLevel; level++)
                Assert.Greater(CardsGameContract.PairCountForStage(level), CardsGameContract.PairCountForStage(level - 1));
        }

        [Test]
        public void InterferenceFollowsTheLevel_FromTheEasiestBankToTheHardest()
        {
            Assert.AreEqual(0, CardsGameContract.InterferenceForLevel(1));
            Assert.AreEqual(0, CardsGameContract.InterferenceForLevel(3));
            Assert.AreEqual(1, CardsGameContract.InterferenceForLevel(4));
            Assert.AreEqual(2, CardsGameContract.InterferenceForLevel(7));
            Assert.AreEqual(3, CardsGameContract.InterferenceForLevel(10));
            int previous = 0;
            for (int level = 1; level <= CardsGameContract.MaxLevel; level++)
            {
                int tier = CardsGameContract.InterferenceForLevel(level);
                Assert.GreaterOrEqual(tier, previous, "el banco de símbolos nunca se vuelve más fácil al subir de nivel");
                previous = tier;
            }
        }

        [Test]
        public void GridDimensions_AreResolvedCorrectlyAndAlwaysFitTheDeck()
        {
            Assert.AreEqual((2, 2), CardsBoardProfile.GridDimensionsFor(2));
            Assert.AreEqual((3, 4), CardsBoardProfile.GridDimensionsFor(6));
            Assert.AreEqual((4, 5), CardsBoardProfile.GridDimensionsFor(9));
            Assert.AreEqual((4, 6), CardsBoardProfile.GridDimensionsFor(12));
            for (int level = 1; level <= CardsGameContract.MaxLevel; level++)
            {
                var p = CardsBoardProfile.Build(level, 0.5f, 500f);
                Assert.AreEqual(CardsGameContract.PairCountForStage(level), p.PairCount);
                Assert.GreaterOrEqual(p.GridColumns * p.GridRows, p.PairCount * 2);
            }
        }

        [Test]
        public void PreviewExposure_IsMaxAtZeroAndHitsTheAgeFloorAtTheTop()
        {
            Assert.AreEqual(3000L, CardsBoardProfile.Build(5, 0f, 500f).PreviewExposureMs);
            Assert.AreEqual(3000L, CardsBoardProfile.Build(5, 0f, 1500f).PreviewExposureMs);
            Assert.AreEqual(500L, CardsBoardProfile.Build(5, 1f, 500f).PreviewExposureMs);
            Assert.AreEqual(1500L, CardsBoardProfile.Build(5, 1f, 1500f).PreviewExposureMs);
            Assert.AreEqual(500L, CardsBoardProfile.Build(5, 7f, 500f).PreviewExposureMs, "el índice se acota a 0..1");
        }

        [Test]
        public void Distractors_AreAtTheirMinimumAtZeroAndTheirCapAtTheTop()
        {
            var low = CardsBoardProfile.Build(1, 0f, 500f);
            Assert.AreEqual(0, low.DistractorCount);
            Assert.AreEqual(0.03f, low.DistractorOpacity, 1e-4f);
            var high = CardsBoardProfile.Build(10, 1f, 500f);
            Assert.AreEqual(6, high.DistractorCount);
            Assert.AreEqual(0.12f, high.DistractorOpacity, 1e-4f);
        }

        [Test]
        public void Engine_UsesTheLadderTheAgeProfileAndTheReactionOnlyInChallenge()
        {
            var adult = CardsGameContract.CreateEngine(Config("ADULT"));
            var senior = CardsGameContract.CreateEngine(Config("SENIOR"));
            Assert.AreEqual(CardsGameContract.MaxLevel, adult.MaxLevel);
            Assert.AreEqual(0.80f, adult.TargetAccuracy, 1e-4f);
            Assert.AreEqual(0.85f, senior.TargetAccuracy, 1e-4f);

            // Con reloj un acierto rápido sube más que uno lento; sin reloj (Precisión) el tiempo no cuenta.
            float Gain(bool timed, float reactionMs)
            {
                var d = CardsGameContract.CreateEngine(Config(timed: timed));
                for (int i = 0; i < 8; i++) d.Register(i % 2 == 0, 1500f);
                float before = d.Rating;
                d.Register(true, reactionMs);
                return d.Rating - before;
            }
            Assert.Greater(Gain(true, 150f), Gain(true, 6000f));
            Assert.AreEqual(Gain(false, 150f), Gain(false, 6000f), 1e-5f);
        }

        [Test]
        public void Simulation_ConvergesToTheTargetAccuracy_AndStrongerPlayersEndHigher()
        {
            foreach (float ability in new[] { 3f, 5f, 7f })
            {
                var (acc, level) = Simulate(CardsGameContract.CreateEngine(Config(level: 1)), ability, 600, 17);
                Assert.That(acc, Is.InRange(0.70f, 0.90f), $"habilidad {ability}: acierto {acc:0.00}");
                Assert.That(level, Is.InRange(ability - 2f, ability + 0.6f), $"habilidad {ability}: nivel {level:0.0}");
            }
            var (_, low) = Simulate(CardsGameContract.CreateEngine(Config(level: 1)), 3f, 400, 5);
            var (_, high) = Simulate(CardsGameContract.CreateEngine(Config(level: 1)), 7f, 400, 5);
            Assert.Greater(high, low + 2f);
        }

        [Test]
        public void ATimeoutCountsAsAnError()
        {
            var dda = CardsGameContract.CreateEngine(Config(saved: 0.5f));
            for (int i = 0; i < 10; i++) dda.Register(true, 1500f);
            float before = dda.Rating;
            dda.Register(false); // se acabó el tiempo: error sin tiempo de reacción
            Assert.Less(dda.Rating, before);
        }

        [Test]
        public void StartsFromTheSavedRating_OrFromTheChosenLevel()
        {
            var saved = CardsGameContract.CreateEngine(Config(saved: 0.5f));
            Assert.AreEqual(6f, saved.Rating, 1e-4f); // 1 + 0,5 × 10
            Assert.AreEqual(5, saved.PresentedLevel, "el calentamiento presenta un nivel por debajo");
            Assert.AreEqual(0.5f, saved.RatingNormalized, 1e-4f);
            var chosen = CardsGameContract.CreateEngine(Config(level: 1));
            Assert.AreEqual(1f, chosen.Rating, 1e-4f);
        }

        [Test]
        public void ModeFloorAndCeiling_BoundTheLevelOfTheBoards()
        {
            try
            {
                AdaptiveDifficulty.ConfigureMode(new SequenceConfigDetails { mode_floor = 0.5f }); // Desafío
                var hard = CardsGameContract.CreateEngine(Config(level: 1));
                for (int i = 0; i < 40; i++) hard.Register(false, 3000f);
                Assert.GreaterOrEqual(hard.PresentedLevel, 6, "con piso 0,5 de 10 niveles el tablero nunca baja del 6");

                AdaptiveDifficulty.ConfigureMode(new SequenceConfigDetails { mode_ceiling = 0.3f }); // Suave
                var soft = CardsGameContract.CreateEngine(Config(saved: 0.9f));
                for (int i = 0; i < 80; i++) soft.Register(true, 800f);
                Assert.LessOrEqual(soft.Level, 4, "con techo 0,3 de 10 niveles el tablero nunca pasa del 4");
            }
            finally
            {
                AdaptiveDifficulty.ConfigureMode(null);
            }
        }

        [Test]
        public void TheTelemetryCarriesEndRatingPeakAndModeCounts()
        {
            var cfg = Config(saved: 0.4f);
            var dda = CardsGameContract.CreateEngine(cfg);
            for (int i = 0; i < 20; i++) dda.Register(i % 5 != 0, 1500f);
            var m = CardsGameContract.BuildMetrics(dda, 16, 20, 80, cfg);
            Assert.AreEqual(dda.RatingNormalized, m.end_rating, 1e-6f);
            Assert.AreEqual(dda.PeakLevel, m.peak_level);
            Assert.AreEqual(dda.ScoredTrials, m.mode_trials);
            Assert.AreEqual(dda.ScoredCorrect, m.mode_hits);
            Assert.AreEqual(18, m.mode_trials); // 20 intentos menos los 2 del calentamiento
            string json = UnityEngine.JsonUtility.ToJson(new CardsTelemetry { user_id = "u", game_id = "parejas", session_metrics = m });
            StringAssert.Contains("\"end_rating\":", json);
            var back = UnityEngine.JsonUtility.FromJson<CardsTelemetry>(json);
            Assert.AreEqual(m.end_rating, back.session_metrics.end_rating, 1e-6f);
        }
    }
}
