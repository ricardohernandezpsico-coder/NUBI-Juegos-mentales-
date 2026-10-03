using System;
using NUnit.Framework;
using NeuroVida.Contracts;

namespace NeuroVida.Games.Secuencia.Tests
{
    /// <summary>
    /// La escalera de 16 niveles de «Rastro de luz» (docs/diseno-rastro-de-luz.md §4) y su conexión con el motor común: cada nivel es válido, nunca se
    /// vuelve más fácil, el motor converge al objetivo (0,80; 0,85 en mayores sin chocar con el tope de bajada por error), respeta techo y piso del
    /// modo, parte del rating guardado o del nivel 2 (1 en mayores) y deja <c>end_rating</c> y <c>ras_*</c> en la telemetría.
    /// </summary>
    public class RastroLadderTests
    {
        private static SequenceConfigDetails Config(string age = "ADULT", int level = 3, float? saved = null, bool assessment = false, bool timed = false) =>
            new SequenceConfigDetails
            {
                level = level, age_band = age, assessment = assessment, timed = timed,
                has_dda_rating = saved.HasValue, dda_rating = saved ?? 0f
            };

        /// <summary>Usuario simulado: cada ronda se acierta con probabilidad logística según la distancia entre su habilidad y el nivel presentado.</summary>
        private static (float accuracy, float meanLevel) Simulate(AdaptiveDifficulty dda, float ability, int trials, int seed)
        {
            var rng = new Random(seed);
            int correct = 0, counted = 0;
            double levelSum = 0;
            for (int i = 0; i < trials; i++)
            {
                int level = dda.PresentedLevel;
                double p = 1.0 / (1.0 + Math.Exp(-1.2 * (ability - level)));
                bool ok = rng.NextDouble() < p;
                dda.Register(ok);
                if (i >= trials / 2) { counted++; if (ok) correct++; levelSum += dda.Level; }
            }
            return ((float)correct / counted, (float)(levelSum / counted));
        }

        // ------------------------------------------------------------------ la escalera

        [Test]
        public void TheLadder_Has16Levels_AndEveryLevelIsValid()
        {
            Assert.AreEqual(16, RastroLadder.MaxLevel);
            for (int level = 1; level <= RastroLadder.MaxLevel; level++)
            {
                var l = RastroLadder.Get(level);
                Assert.AreEqual(level, l.Index);
                Assert.IsTrue(l.Has(RastroMode.Rastro), $"nivel {level}: el rastro simple siempre está");
                foreach (var m in RastroModes.All)
                {
                    int len = l.Length(m);
                    if (l.Has(m)) Assert.That(len, Is.InRange(2, 8), $"nivel {level} {m}: largo {len}");
                    else Assert.AreEqual(0, len, $"nivel {level} {m}: sin familia, sin largo");
                }
                Assert.Greater(l.SparkSpeed, 0f);
                Assert.Greater(l.WaitMs, 0f);
                Assert.That(l.CrossMax, Is.GreaterThanOrEqualTo(0));
                if (l.Has(RastroMode.Gira)) Assert.That(l.TurnMin, Is.InRange(1f, l.TurnMax), $"nivel {level}: giro {l.TurnMin}-{l.TurnMax}");
                else Assert.AreEqual(0f, l.TurnMax, $"nivel {level}: sin giro");
                if (l.Has(RastroMode.Marcha))
                {
                    Assert.Greater(l.ShownMin, l.LenMarcha, $"nivel {level}: la chispa recorre más luces de las que se piden");
                    Assert.LessOrEqual(l.ShownMin, l.ShownMax);
                    Assert.LessOrEqual(l.ShownMax, RastroPaths.MaxLength, $"nivel {level}: hay solo 9 luceros");
                }
                else Assert.AreEqual(0, l.ShownMax);
            }
        }

        [Test]
        public void TheLadder_NeverGetsEasier()
        {
            for (int level = 2; level <= RastroLadder.MaxLevel; level++)
            {
                var prev = RastroLadder.Get(level - 1);
                var cur = RastroLadder.Get(level);
                Assert.AreEqual(prev.Families, prev.Families & cur.Families, $"nivel {level}: pierde una familia");
                foreach (var m in RastroModes.All)
                    if (prev.Has(m)) Assert.GreaterOrEqual(cur.Length(m), prev.Length(m), $"nivel {level} {m}: pide menos luces que el anterior");
                Assert.GreaterOrEqual(cur.SparkSpeed, prev.SparkSpeed, $"nivel {level}: chispa más lenta");
                Assert.LessOrEqual(cur.WaitMs, prev.WaitMs, $"nivel {level}: espera más larga");
                Assert.GreaterOrEqual(cur.CrossMax, prev.CrossMax, $"nivel {level}: menos cruces");
                Assert.GreaterOrEqual(cur.TurnMax, prev.TurnMax, $"nivel {level}: giro más chico");
            }
        }

        [Test]
        public void FamiliesAppearAtTheLevelsOfTheDesign()
        {
            for (int level = 1; level <= 4; level++) CollectionAssert.AreEqual(new[] { RastroMode.Rastro }, RastroLadder.FamiliesAt(level));
            for (int level = 5; level <= 6; level++) CollectionAssert.AreEqual(new[] { RastroMode.Rastro, RastroMode.Reves }, RastroLadder.FamiliesAt(level));
            for (int level = 7; level <= 8; level++) CollectionAssert.AreEqual(new[] { RastroMode.Rastro, RastroMode.Reves, RastroMode.Gira }, RastroLadder.FamiliesAt(level));
            for (int level = 9; level <= 16; level++) Assert.AreEqual(4, RastroLadder.FamiliesAt(level).Length, $"nivel {level}");
        }

        [Test]
        public void TheTableMatchesTheDesign_AtTheCornerLevels()
        {
            var l1 = RastroLadder.Get(1);
            Assert.AreEqual(2, l1.LenRastro);
            Assert.AreEqual(200f, l1.SparkSpeed);
            Assert.AreEqual(320f, l1.WaitMs);
            var l9 = RastroLadder.Get(9);
            Assert.AreEqual(new[] { 5, 4, 4, 2 }, new[] { l9.LenRastro, l9.LenReves, l9.LenGira, l9.LenMarcha });
            Assert.AreEqual(new[] { 3, 5 }, new[] { l9.ShownMin, l9.ShownMax });
            Assert.AreEqual(new[] { 90f, 150f }, new[] { l9.TurnMin, l9.TurnMax });
            var l12 = RastroLadder.Get(12);
            Assert.AreEqual(new[] { 6, 5, 5, 3 }, new[] { l12.LenRastro, l12.LenReves, l12.LenGira, l12.LenMarcha });
            Assert.AreEqual(new[] { 5, 7 }, new[] { l12.ShownMin, l12.ShownMax });
            var l16 = RastroLadder.Get(16);
            Assert.AreEqual(new[] { 8, 7, 6, 5 }, new[] { l16.LenRastro, l16.LenReves, l16.LenGira, l16.LenMarcha });
            Assert.AreEqual(400f, l16.SparkSpeed);
            Assert.AreEqual(160f, l16.WaitMs);
            // el tope del largo es 8 luces (Cowan: el límite real ronda los 4 elementos; las escaleras no pasan de 8)
            for (int level = 1; level <= 16; level++) Assert.LessOrEqual(RastroLadder.Get(level).LenRastro, 8);
        }

        [Test]
        public void CrossingsOnlyWhereTheLevelAllowsThem()
        {
            for (int level = 1; level <= 9; level++) Assert.AreEqual(0, RastroLadder.Get(level).CrossMax, $"nivel {level}");
            Assert.AreEqual(1, RastroLadder.Get(10).CrossMax);
            Assert.AreEqual(1, RastroLadder.Get(11).CrossMax);
            for (int level = 12; level <= 14; level++) Assert.AreEqual(2, RastroLadder.Get(level).CrossMax, $"nivel {level}");
            Assert.AreEqual(RastroLadder.CrossAny, RastroLadder.Get(15).CrossMax);
            Assert.AreEqual(RastroLadder.CrossAny, RastroLadder.Get(16).CrossMax);
        }

        // ------------------------------------------------------------------ el motor común

        [Test]
        public void Engine_UsesThe16LevelLadderAndTheCommonAgeTargets()
        {
            var adult = RastroContract.CreateEngine(Config("ADULT"));
            var senior = RastroContract.CreateEngine(Config("SENIOR"));
            Assert.AreEqual(16, adult.MaxLevel);
            Assert.AreEqual(0.80f, adult.TargetAccuracy, 1e-4f);
            Assert.AreEqual(0.85f, senior.TargetAccuracy, 1e-4f);
        }

        [Test]
        public void StepUp_NeverHitsTheDropCap_SoTheTargetIsReached()
        {
            // El motor baja paso·p/(1−p) por error (tope 1,0) y multiplica el paso por 0,85 en mayores y 1,1 en menores de 18.
            // Si el tope se activa, el equilibrio queda por debajo del objetivo (la Secuencia anterior en mayores: ~0,82).
            foreach (var (age, mult) in new[] { (AgeBand.Adult, 1f), (AgeBand.Senior, 0.85f), (AgeBand.Under18, 1.1f) })
            {
                float p = AdaptiveDifficulty.TargetFor(age);
                float drop = RastroContract.StepUp(age) * mult * p / (1f - p);
                Assert.LessOrEqual(drop, 1.0001f, $"{age}: baja {drop:0.000} por error y el tope es 1");
                Assert.GreaterOrEqual(drop, 0.78f, $"{age}: el paso quedó demasiado chico ({drop:0.000})");
            }
            Assert.AreEqual(0.17f, RastroContract.StepUp(AgeBand.Senior), 1e-6f, "el valor documentado para mayores");
        }

        [Test]
        public void ReactionTimeIsNotUsed()
        {
            var a = RastroContract.CreateEngine(Config(saved: 0.5f));
            var b = RastroContract.CreateEngine(Config(saved: 0.5f));
            for (int i = 0; i < 12; i++) { a.Register(i % 3 != 0, 300f); b.Register(i % 3 != 0, 9000f); }
            Assert.AreEqual(a.Rating, b.Rating, 1e-6f);
        }

        [Test]
        public void Simulation_ConvergesToTheTargetAccuracy_AndStrongerPlayersEndHigher()
        {
            foreach (float ability in new[] { 4f, 8f, 12f })
            {
                var (acc, level) = Simulate(RastroContract.CreateEngine(Config(level: 1)), ability, 2500, 23);
                Assert.That(acc, Is.InRange(0.72f, 0.88f), $"habilidad {ability}: acierto {acc:0.00}");
                Assert.That(level, Is.InRange(ability - 3f, ability + 0.6f), $"habilidad {ability}: nivel {level:0.0}");
            }
            var (_, low) = Simulate(RastroContract.CreateEngine(Config(level: 1)), 4f, 1500, 9);
            var (_, high) = Simulate(RastroContract.CreateEngine(Config(level: 1)), 12f, 1500, 9);
            Assert.Greater(high, low + 4f);
        }

        [Test]
        public void Simulation_Senior_ConvergesTowardTheTarget0_85()
        {
            // Con el paso de la Secuencia anterior (0,25) el equilibrio de mayores quedaba en ~0,82; con 0,17 llega a 0,85.
            float sum = 0f;
            int n = 0;
            foreach (int seed in new[] { 3, 17, 29, 41 })
            {
                var (acc, _) = Simulate(RastroContract.CreateEngine(Config("SENIOR", level: 1)), 8f, 4000, seed);
                sum += acc; n++;
            }
            float mean = sum / n;
            Assert.That(mean, Is.InRange(0.82f, 0.89f), $"mayores: acierto medio {mean:0.000} (objetivo 0,85)");
            // y el adulto, 0,80
            sum = 0f; n = 0;
            foreach (int seed in new[] { 3, 17, 29, 41 })
            {
                var (acc, _) = Simulate(RastroContract.CreateEngine(Config("ADULT", level: 1)), 8f, 4000, seed);
                sum += acc; n++;
            }
            Assert.That(sum / n, Is.InRange(0.77f, 0.84f), $"adultos: acierto medio {sum / n:0.000} (objetivo 0,80)");
        }

        [Test]
        public void StartsFromTheSavedRating_OrFromLevel2_Or1ForSeniors()
        {
            var saved = RastroContract.CreateEngine(Config(saved: 0.5f));
            Assert.AreEqual(9f, saved.Rating, 1e-4f);               // 1 + 0,5 × 16
            Assert.AreEqual(8, saved.PresentedLevel, "el calentamiento presenta un nivel por debajo");
            Assert.AreEqual(0.5f, saved.RatingNormalized, 1e-4f);
            // primera vez (sin rating guardado): nivel 2 en adultos, 1 en mayores, aunque la app mande otro nivel elegido
            Assert.AreEqual(2f, RastroContract.CreateEngine(Config(level: 5)).Rating, 1e-4f);
            Assert.AreEqual(1f, RastroContract.CreateEngine(Config("SENIOR", level: 5)).Rating, 1e-4f);
            Assert.AreEqual(2f, RastroContract.CreateEngine(Config("UNDER_18", level: 1)).Rating, 1e-4f);
        }

        [Test]
        public void TheEvaluation_StartsAtLevel2_IgnoresTheSavedRating_AndCalibratesFast()
        {
            try
            {
                AdaptiveDifficulty.FastCalibration = true; // lo fija Assessment.Configure
                var cfg = Config(level: 3, saved: 0.9f, assessment: true);
                var fast = RastroContract.CreateEngine(cfg);
                Assert.AreEqual(2f, fast.Rating, 1e-4f);
                Assert.AreEqual(1f, RastroContract.CreateEngine(Config("SENIOR", assessment: true)).Rating, 1e-4f);
                float start = fast.Rating;
                fast.Register(true);
                AdaptiveDifficulty.FastCalibration = false;
                var normal = RastroContract.CreateEngine(cfg);
                normal.Register(true);
                Assert.Greater(fast.Rating - start, normal.Rating - start, "la evaluación da pasos más grandes");
            }
            finally
            {
                AdaptiveDifficulty.FastCalibration = false;
            }
        }

        [Test]
        public void AnOldSavedRating_KeepsItsMeaning()
        {
            // El rating viejo ya era 0..1 sobre los mismos 16 niveles ((pico-1)/15): se continúa desde ahí sin convertir.
            float oldRating = (9 - 1) / 15f;
            float rating = RastroContract.CreateEngine(Config(saved: oldRating)).Rating;
            Assert.That(rating, Is.InRange(9f, 10f));
        }

        [Test]
        public void ModeFloorAndCeiling_BoundTheLevel()
        {
            try
            {
                AdaptiveDifficulty.ConfigureMode(new SequenceConfigDetails { mode_floor = 0.5f }); // Desafío
                var hard = RastroContract.CreateEngine(Config(level: 1));
                for (int i = 0; i < 40; i++) hard.Register(false);
                Assert.GreaterOrEqual(hard.PresentedLevel, 9, "con piso 0,5 de 16 niveles nunca baja del 9");

                AdaptiveDifficulty.ConfigureMode(new SequenceConfigDetails { mode_ceiling = 0.25f }); // Suave
                var soft = RastroContract.CreateEngine(Config(saved: 0.9f));
                for (int i = 0; i < 120; i++) soft.Register(true);
                Assert.LessOrEqual(soft.Level, 5, "con techo 0,25 de 16 niveles nunca pasa del 5");
            }
            finally
            {
                AdaptiveDifficulty.ConfigureMode(null);
            }
        }

        // ------------------------------------------------------------------ puntaje y telemetría

        [Test]
        public void Score_IsAccuracyAndPeakLevel_Within0To100()
        {
            Assert.AreEqual(0, RastroContract.Score(0, 0, 1));
            Assert.AreEqual(100, RastroContract.Score(10, 10, 16));
            Assert.AreEqual(70, RastroContract.Score(10, 10, 1));
            Assert.That(RastroContract.Score(8, 10, 8), Is.InRange(60, 80));
            Assert.AreEqual(RastroContract.Score(5, 10, 16), RastroContract.Score(5, 10, 99), "el nivel se recorta a la escalera");
        }

        [Test]
        public void TheTelemetryCarriesEndRatingPeakModeCounts_AndTheRasFields()
        {
            var cfg = Config(saved: 0.5f);
            var s = new RastroSession(cfg, new Random(5), 0);
            for (int i = 0; i < 6; i++) s.Complete(s.NextRound(), i % 3 != 2);
            var m = RastroContract.BuildMetrics(s, 640.0, cfg);
            Assert.AreEqual(s.Dda.RatingNormalized, m.end_rating, 1e-6f);
            Assert.AreEqual(s.Dda.PeakLevel, m.peak_level, "peak_level se mantiene");
            Assert.AreEqual(4, m.mode_trials); // 6 rondas menos las 2 del calentamiento
            Assert.AreEqual(6, m.total_rounds);
            Assert.AreEqual(4, m.correct_rounds);
            Assert.AreEqual(4, m.ras_rounds.Length);
            Assert.AreEqual(6, Sum(m.ras_rounds));
            Assert.AreEqual(4, Sum(m.ras_hits));
            Assert.AreEqual(s.Tally.ModesSeen, m.ras_modes_seen);
            Assert.AreEqual(s.NewModes, m.ras_new_modes);
            for (int i = 0; i < 4; i++) Assert.LessOrEqual(m.ras_hits[i], m.ras_rounds[i]);
            string json = UnityEngine.JsonUtility.ToJson(new SequenceTelemetry { user_id = "u", game_id = "secuencia", session_metrics = m });
            StringAssert.Contains("\"end_rating\":", json);
            StringAssert.Contains("\"peak_level\":", json);
            StringAssert.Contains("\"mode_trials\":", json);
            StringAssert.Contains("\"mode_hits\":", json);
            StringAssert.Contains("\"ras_best_len\":[", json);
            var back = UnityEngine.JsonUtility.FromJson<SequenceTelemetry>(json);
            Assert.AreEqual(m.end_rating, back.session_metrics.end_rating, 1e-6f);
            CollectionAssert.AreEqual(m.ras_rounds, back.session_metrics.ras_rounds);
            CollectionAssert.AreEqual(m.ras_best_len, back.session_metrics.ras_best_len);
        }

        private static int Sum(int[] a) { int n = 0; foreach (int v in a) n += v; return n; }
    }
}
