using System;
using NUnit.Framework;
using NeuroVida.Contracts;

namespace NeuroVida.Games.Secuencia.Tests
{
    /// <summary>
    /// El director de modos y el estado de la partida de «Rastro de luz»: no más de 2 rondas seguidas del mismo modo nuevo, el rastro simple siempre
    /// en la mezcla, el desbloqueo «¡NUEVO!» una sola vez, la ronda guiada del tutorial sin dejar rastro en el DDA, las vidas y cuándo termina cada partida.
    /// </summary>
    public class RastroSessionTests
    {
        private static SequenceConfigDetails Config(string age = "ADULT", float saved = 0.5f, bool timed = false, bool assessment = false) =>
            new SequenceConfigDetails { age_band = age, has_dda_rating = !assessment, dda_rating = saved, timed = timed, assessment = assessment };

        private const int AllModes = 15;

        // ------------------------------------------------------------------ director

        [Test]
        public void Director_NeverRepeatsANewModeMoreThanTwiceInARow_AndTheSimpleTrailAlwaysAppears()
        {
            foreach (int level in new[] { 5, 7, 9, 12, 16 })
            {
                var d = new RastroDirector(new Random(level), AllModes, simpleOnly: false);
                var seq = new RastroMode[3000];
                for (int i = 0; i < seq.Length; i++) seq[i] = d.Next(level, out _);
                Assert.AreEqual(RastroMode.Rastro, seq[0], "la primera ronda es el rastro simple");
                int run = 1;
                for (int i = 1; i < seq.Length; i++)
                {
                    run = seq[i] == seq[i - 1] ? run + 1 : 1;
                    if (seq[i] != RastroMode.Rastro) Assert.LessOrEqual(run, 2, $"nivel {level}, ronda {i}: {seq[i]} {run} veces seguidas");
                    if (i >= 2) Assert.IsTrue(seq[i] == RastroMode.Rastro || seq[i - 1] == RastroMode.Rastro || seq[i - 2] == RastroMode.Rastro,
                        $"nivel {level}, ronda {i}: tres rondas seguidas sin el rastro simple");
                }
                // y solo aparecen las familias del nivel, todas ellas
                var seen = new System.Collections.Generic.HashSet<RastroMode>(seq);
                CollectionAssert.AreEquivalent(RastroLadder.FamiliesAt(level), seen);
            }
        }

        [Test]
        public void Director_TheEvaluationOnlyPlaysTheSimpleTrail()
        {
            var d = new RastroDirector(new Random(1), 0, simpleOnly: true);
            for (int i = 0; i < 200; i++) Assert.AreEqual(RastroMode.Rastro, d.Next(16, out bool isNew));
            Assert.AreEqual(0, d.NewlyUnlocked);
            var s = new RastroSession(Config(assessment: true), new Random(2), 0);
            for (int i = 0; i < 30; i++)
            {
                var r = s.NextRound();
                Assert.AreEqual(RastroMode.Rastro, r.Mode);
                Assert.IsFalse(r.IsNewMode);
                s.Complete(r, true);
            }
        }

        [Test]
        public void Director_EachModeIsNewOnlyTheFirstTimeInALifetime()
        {
            var d = new RastroDirector(new Random(9), 0, simpleOnly: false);
            var firstSeen = new System.Collections.Generic.Dictionary<RastroMode, int>();
            int news = 0;
            for (int i = 0; i < 60; i++)
            {
                var m = d.Next(12, out bool isNew);
                if (isNew) { news++; Assert.IsFalse(firstSeen.ContainsKey(m), $"{m} salió como nuevo dos veces"); }
                if (!firstSeen.ContainsKey(m))
                {
                    firstSeen[m] = i;
                    if (m != RastroMode.Rastro) Assert.IsTrue(isNew, $"{m}: la primera vez que sale lleva su «¡NUEVO!»");
                }
            }
            Assert.AreEqual(3, news, "reves, gira y marcha, una vez cada uno");
            Assert.AreEqual(RastroModes.Bit(RastroMode.Reves) | RastroModes.Bit(RastroMode.Gira) | RastroModes.Bit(RastroMode.Marcha), d.NewlyUnlocked);
            Assert.AreEqual(AllModes, d.Unlocked);

            // quien ya los conoce (guardado de por vida) no vuelve a ver «¡NUEVO!»
            var known = new RastroDirector(new Random(9), AllModes, simpleOnly: false);
            for (int i = 0; i < 100; i++) { known.Next(12, out bool isNew); Assert.IsFalse(isNew); }
            Assert.AreEqual(0, known.NewlyUnlocked);
        }

        [Test]
        public void Director_ANewModeComesRightAfterItBecomesAvailable()
        {
            // al nivel 5 se abre «al revés»: en cuanto el nivel lo permite sale en la ronda siguiente (con «¡NUEVO!»), no cuando el azar quiera
            var d = new RastroDirector(new Random(3), 0, simpleOnly: false);
            for (int i = 0; i < 31; i++) Assert.AreEqual(RastroMode.Rastro, d.Next(4, out _), "en el nivel 4 solo hay rastro simple");
            var m = d.Next(5, out bool isNew);
            Assert.AreEqual(RastroMode.Reves, m);
            Assert.IsTrue(isNew);
            Assert.AreEqual(RastroModes.Bit(RastroMode.Reves), d.NewlyUnlocked);
        }

        // ------------------------------------------------------------------ ronda guiada

        [Test]
        public void TheGuidedRound_LeavesNoTraceInTheDdaTheTallyOrTheLives()
        {
            var s = new RastroSession(Config(saved: 0.5f), new Random(6), 0);
            float rating = s.Dda.Rating;
            var g = s.GuidedRound();
            Assert.IsTrue(g.Guided);
            Assert.AreEqual(RastroContract.GuidedLength, g.Asked, "dos luces");
            Assert.AreEqual(RastroMode.Rastro, g.Mode);
            CollectionAssert.AreEqual(g.Shown, g.Target);
            Assert.IsFalse(g.IsNewMode);
            foreach (bool ok in new[] { false, true, false, true })
            {
                var outcome = s.Complete(g, ok);
                Assert.IsFalse(outcome.Counted);
                Assert.AreEqual(DdaChange.None, outcome.Change);
            }
            Assert.AreEqual(0, s.Dda.Trials, "el DDA no vio ningún ensayo");
            Assert.AreEqual(0, s.Dda.ScoredTrials);
            Assert.AreEqual(rating, s.Dda.Rating, 1e-6f, "el rating no se movió");
            Assert.AreEqual(0, s.Tally.TotalRounds);
            Assert.AreEqual(0, s.Tally.TotalHits);
            Assert.AreEqual(0, s.Tally.ModesSeen);
            Assert.AreEqual(RastroContract.Lives, s.Lives);
            Assert.AreEqual(0, s.Errors);
            Assert.AreEqual(0, s.NewModes);
            Assert.AreEqual(0, RastroContract.BuildMetrics(s, 0, Config()).total_rounds, "y no sale en la telemetría");
            // la partida que sigue arranca igual que sin tutorial
            var first = s.NextRound();
            Assert.IsFalse(first.Guided);
            Assert.AreEqual(RastroMode.Rastro, first.Mode);
        }

        [Test]
        public void TheGuidedRound_IsAShortCleanPath_AtTheSpeedOfLevel2()
        {
            var s = new RastroSession(Config("SENIOR"), new Random(7), 0);
            for (int k = 0; k < 50; k++)
            {
                var g = s.GuidedRound();
                Assert.AreEqual(2, g.Shown.Length);
                Assert.AreNotEqual(g.Shown[0], g.Shown[1]);
                Assert.IsTrue(RastroBoard.SegmentClear(g.Shown[0], g.Shown[1]));
                Assert.AreEqual(RastroLadder.Get(2).SparkSpeed * 0.85f, g.SparkSpeed, 1e-3f);
            }
        }

        // ------------------------------------------------------------------ rondas reales, vidas y final

        [Test]
        public void ARealRound_CountsInTheDdaTheTallyAndTheLives()
        {
            var s = new RastroSession(Config(), new Random(10), AllModes);
            var r1 = s.NextRound();
            var o1 = s.Complete(r1, true);
            Assert.IsTrue(o1.Counted);
            var r2 = s.NextRound();
            s.Complete(r2, false);
            Assert.AreEqual(2, s.Dda.Trials);
            Assert.AreEqual(2, s.Tally.TotalRounds);
            Assert.AreEqual(1, s.Tally.TotalHits);
            Assert.AreEqual(RastroContract.Lives - 1, s.Lives);
            Assert.AreEqual(1, s.Errors);
            Assert.AreEqual(r1.Asked, s.Tally.BestLength[(int)r1.Mode], "el mejor largo se anota solo con acierto");
            Assert.AreEqual(RastroModes.Bit(r1.Mode) | RastroModes.Bit(r2.Mode), s.Tally.ModesSeen);
        }

        [Test]
        public void TheTally_KeepsTheBestLengthPerFamily_AndHitsNeverExceedRounds()
        {
            var s = new RastroSession(Config(saved: 0.8f), new Random(12), AllModes);
            var rng = new Random(13);
            for (int i = 0; i < 300; i++)
            {
                var r = s.NextRound();
                s.Complete(r, rng.NextDouble() < 0.8);
            }
            for (int m = 0; m < RastroModes.Count; m++)
            {
                Assert.LessOrEqual(s.Tally.Hits[m], s.Tally.Rounds[m]);
                Assert.GreaterOrEqual(s.Tally.BestLength[m], s.Tally.Hits[m] > 0 ? 2 : 0);
                Assert.LessOrEqual(s.Tally.BestLength[m], 8);
            }
        }

        [Test]
        public void ThreeLives_EndTheGame_InEveryMode()
        {
            foreach (var cfg in new[] { Config(timed: true), Config(timed: false) })
            {
                var s = new RastroSession(cfg, new Random(14), AllModes);
                for (int i = 0; i < 2; i++) { s.Complete(s.NextRound(), false); Assert.IsFalse(s.IsOver(1f)); }
                s.Complete(s.NextRound(), false);
                Assert.AreEqual(0, s.Lives);
                Assert.IsTrue(s.IsOver(1f));
            }
        }

        [Test]
        public void TheRetoEndsAt90Seconds_AndPrecisionAfter14Rounds()
        {
            var reto = new RastroSession(Config(timed: true), new Random(15), AllModes);
            Assert.IsFalse(reto.IsOver(89.9f));
            Assert.IsTrue(reto.IsOver(90f));
            var precision = new RastroSession(Config(timed: false), new Random(16), AllModes);
            for (int i = 0; i < 13; i++) { precision.Complete(precision.NextRound(), true); Assert.IsFalse(precision.IsOver(1000f), "Precisión no tiene reloj"); }
            precision.Complete(precision.NextRound(), true);
            Assert.IsTrue(precision.IsOver(0f));
            Assert.AreEqual(14, RastroContract.PrecisionRounds);
        }

        [Test]
        public void TheEvaluationEndsWithTwoErrorsOrAfter75Seconds()
        {
            var a = new RastroSession(Config(assessment: true), new Random(17), 0);
            a.Complete(a.NextRound(), false);
            Assert.IsFalse(a.IsOver(10f));
            a.Complete(a.NextRound(), true);
            a.Complete(a.NextRound(), false);
            Assert.IsTrue(a.IsOver(10f), "2 errores");
            var b = new RastroSession(Config(assessment: true), new Random(18), 0);
            Assert.IsFalse(b.IsOver(74f));
            Assert.IsTrue(b.IsOver(75f), "~75 s");
            Assert.AreEqual(2, RastroContract.AssessmentMaxErrors);
        }

        [Test]
        public void TheEvaluation_StartsOnLevel2OrLevel1ForSeniors_WithoutUsingTheSavedRating()
        {
            var adult = new RastroSession(Config("ADULT", saved: 0.95f, assessment: true), new Random(19), 0);
            Assert.AreEqual(2, adult.Dda.Level);
            var senior = new RastroSession(Config("SENIOR", saved: 0.95f, assessment: true), new Random(20), 0);
            Assert.AreEqual(1, senior.Dda.Level);
            // y la medida sale en end_rating, que es lo que lee Baseline.kt
            for (int i = 0; i < 4; i++) adult.Complete(adult.NextRound(), true);
            var m = RastroContract.BuildMetrics(adult, 0, Config(assessment: true));
            Assert.Greater(m.end_rating, 0f);
            Assert.AreEqual(adult.Dda.RatingNormalized, m.end_rating, 1e-6f);
        }

        [Test]
        public void Simulation_AfterAMixedGame_TheLadderStaysInsideItsLimits()
        {
            // 40 partidas simuladas completas: ninguna ronda pide algo fuera de la escalera ni repite un lucero
            for (int game = 0; game < 40; game++)
            {
                var rng = new Random(100 + game);
                var s = new RastroSession(Config(game % 2 == 0 ? "ADULT" : "SENIOR", saved: (game % 10) / 10f, timed: game % 3 == 0), rng, game % 4 == 0 ? 0 : AllModes);
                int guard = 0;
                while (!s.IsOver(guard * 5f) && guard++ < 100)
                {
                    var r = s.NextRound();
                    Assert.That(r.Level, Is.InRange(1, 16));
                    Assert.IsTrue(r.Params.Has(r.Mode), "una familia que el nivel no tiene");
                    CollectionAssert.AllItemsAreUnique(r.Shown);
                    Assert.AreEqual(r.Params.Length(r.Mode), r.Asked);
                    s.Complete(r, rng.NextDouble() < 0.85);
                }
            }
        }
    }
}
