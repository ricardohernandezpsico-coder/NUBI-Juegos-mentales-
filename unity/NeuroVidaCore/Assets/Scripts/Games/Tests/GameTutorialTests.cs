using System;
using NUnit.Framework;
using NeuroVida.Contracts;
using NeuroVida.Games.Aterrizaje;
using NeuroVida.Games.Freno;
using NeuroVida.Games.Meteoros;

namespace NeuroVida.Games.Tests
{
    /// <summary>Lo común a las versiones cortas del inicio de los tres juegos: parten suave, el motor no usa tiempo de reacción en Aterrizaje/Meteoros, y el rating final sale normalizado.</summary>
    internal static class ShortVersionHelper
    {
        public static SequenceConfigDetails Config(string ageBand, bool assessment, bool timed = false) =>
            new SequenceConfigDetails { age_band = ageBand, assessment = assessment, timed = timed, level = 3 };

        /// <summary>Simula una versión corta con la tasa de acierto que se pida y devuelve el motor al final.</summary>
        public static AdaptiveDifficulty Simulate(AdaptiveDifficulty engine, int trials, double accuracy, int seed)
        {
            var rng = new Random(seed);
            for (int i = 0; i < trials; i++) engine.Register(rng.NextDouble() < accuracy);
            return engine;
        }
    }

    public class FrenoTutorialTests
    {
        [Test]
        public void TheGuidedRound_IsThreeGoesAndOneStop_InThatOrder()
        {
            CollectionAssert.AreEqual(new[] { BrakeContract.GuidedStep.Go, BrakeContract.GuidedStep.Go, BrakeContract.GuidedStep.Go, BrakeContract.GuidedStep.Stop },
                BrakeContract.GuidedPlan);
        }

        [Test]
        public void TheShortVersion_HasAFixedNumberOfLaunches_AndFewerThanPrecision()
        {
            Assert.AreEqual(24, BrakeContract.TotalTrials(true));
            Assert.AreEqual(BrakeContract.PrecisionTrials, BrakeContract.TotalTrials(false));
            Assert.Less(BrakeContract.AssessmentTrials, BrakeContract.PrecisionTrials);
        }

        [Test]
        public void TheShortVersionStops_AreEnoughForTheStopTime_AndNeverBunchedUp()
        {
            int stops = 0, run = 0, maxRun = 0;
            for (int i = 0; i < BrakeContract.AssessmentTrials; i++)
            {
                bool stop = BrakeContract.AssessmentStop(i);
                if (stop) { stops++; run++; maxRun = Math.Max(maxRun, run); } else run = 0;
                if (i < BrakeContract.WarmupGoTrials) Assert.IsFalse(stop, "los primeros lanzamientos son de ir: " + i);
            }
            Assert.GreaterOrEqual(stops, 6, "alcanza el mínimo de altos del tiempo de frenado");
            Assert.LessOrEqual(maxRun, 2, "nunca más de 2 altos seguidos");
            Assert.IsFalse(BrakeContract.AssessmentStop(-1));
            Assert.IsFalse(BrakeContract.AssessmentStop(BrakeContract.AssessmentTrials));
        }

        [Test]
        public void TheShortVersion_StartsSoft_SeniorsAtLevelOne_OthersAtThree()
        {
            Assert.AreEqual(1, BrakeContract.CreateEngine(ShortVersionHelper.Config("senior", true)).Level);
            Assert.AreEqual(3, BrakeContract.CreateEngine(ShortVersionHelper.Config("adult", true)).Level);
        }

        [Test]
        public void TheShortVersion_EndsWithANormalizedRating()
        {
            var e = ShortVersionHelper.Simulate(BrakeContract.CreateEngine(ShortVersionHelper.Config("adult", true)), BrakeContract.AssessmentTrials, 0.9, 7);
            Assert.GreaterOrEqual(e.RatingNormalized, 0f);
            Assert.LessOrEqual(e.RatingNormalized, 1f);
            Assert.Greater(e.RatingNormalized, 0.1f, "con 90 % de aciertos el nivel sube del arranque suave");
        }
    }

    public class AterrizajeTutorialTests
    {
        [Test]
        public void TheGuidedTrial_IsAnEasyOne_NeverTheMiddle_NeverTheSameTwice()
        {
            var rng = new Random(1);
            int last = -1;
            for (int i = 0; i < 300; i++)
            {
                var t = LandingContract.GuidedTrial(rng, last);
                Assert.AreEqual(0f, t.Min);
                Assert.AreEqual(10f, t.Max);
                Assert.IsTrue(t.MidTick, "la regla lleva la marca del medio");
                Assert.GreaterOrEqual(t.Target, 2f);
                Assert.LessOrEqual(t.Target, 8f);
                Assert.AreNotEqual(5f, t.Target, "el medio ya está marcado: no se pide");
                Assert.AreNotEqual((float)last, t.Target, "no se repite el número");
                Assert.AreEqual(((int)t.Target).ToString(), t.Label);
                last = (int)t.Target;
            }
        }

        [Test]
        public void TheGuidedZones_GetNarrower_AndAreWiderThanAHit()
        {
            Assert.AreEqual(2, LandingContract.GuidedZones.Length);
            Assert.Greater(LandingContract.GuidedZones[0], LandingContract.GuidedZones[1]);
            Assert.GreaterOrEqual(LandingContract.GuidedZones[1], LandingContract.HitError, "la zona más chica todavía es un buen aterrizaje");
        }

        [Test]
        public void TheShortVersion_IsEightFixedLandings()
        {
            Assert.AreEqual(8, LandingContract.TotalTrials(true));
            Assert.AreEqual(LandingContract.PrecisionTrials, LandingContract.TotalTrials(false));
        }

        [Test]
        public void TheShortVersion_StartsSoft_SeniorsAtLevelOne_OthersAtThree()
        {
            Assert.AreEqual(1, LandingContract.CreateEngine(ShortVersionHelper.Config("senior", true)).Level);
            Assert.AreEqual(3, LandingContract.CreateEngine(ShortVersionHelper.Config("adult", true)).Level);
        }

        [Test]
        public void TheShortVersion_EndsWithANormalizedRating_AndTheFirstDataIsInWords()
        {
            var e = ShortVersionHelper.Simulate(LandingContract.CreateEngine(ShortVersionHelper.Config("senior", true)), LandingContract.AssessmentTrials, 0.8, 3);
            Assert.GreaterOrEqual(e.RatingNormalized, 0f);
            Assert.LessOrEqual(e.RatingNormalized, 1f);
            // «distancia media al lugar justo»: la misma cuenta que usa la partida normal
            Assert.AreEqual(4f, LandingContract.MeanErrorPct(new[] { 0.02f, 0.06f }), 1e-3f);
        }
    }

    public class MeteorosTutorialTests
    {
        [Test]
        public void TheGuidedRound_IsTwoWordsAndOneInvented_InThatOrder()
        {
            CollectionAssert.AreEqual(new[] { true, true, false }, MeteorContract.GuidedPlan);
            Assert.GreaterOrEqual(MeteorContract.GuidedFallSeconds, 4f, "cae despacio: hay tiempo de sobra para leer");
            Assert.LessOrEqual(MeteorContract.GuidedFallSeconds, 6f, "pero no 11 s: la inventada se deja caer entera y era tiempo muerto");
        }

        [Test]
        public void TheGuidedRound_FallsInSixSeconds_SevenAndAHalfForSeniors()
        {
            Assert.AreEqual(6f, MeteorContract.GuidedFall(false), 1e-4f);
            Assert.AreEqual(7.5f, MeteorContract.GuidedFall(true), 1e-4f);
        }

        [Test]
        public void TheGuidedRound_NextMeteorAppearsAtOnce_AfterAnError_ThereIsTimeToRead()
        {
            Assert.LessOrEqual(MeteorContract.GuidedGapSeconds, 0.4f, "apenas se resuelve uno aparece el siguiente");
            Assert.GreaterOrEqual(MeteorContract.GuidedRetryGapSeconds, 1.5f, "tras un error se alcanza a leer el porqué");
        }

        [Test]
        public void TheGuidedRound_IdleTimeDropsFromNineteenToEightSeconds()
        {
            // antes: 3 × 2,2 + 1,8 + 11 = 19,4 s sin hacer nada
            Assert.AreEqual(8.4f, MeteorContract.GuidedIdleSeconds(false), 1e-3f);
            Assert.AreEqual(9.9f, MeteorContract.GuidedIdleSeconds(true), 1e-3f);
            Assert.Less(MeteorContract.GuidedIdleSeconds(false), 19.4f / 2f, "menos de la mitad que antes");
            Assert.Less(MeteorContract.GuidedIdleSeconds(true), 19.4f * 0.55f, "también en mayores");
        }

        [Test]
        public void TheShortVersion_FirstMeteorWithinHalfASecond_AndNeverFewerThanTwoOnScreen()
        {
            Assert.LessOrEqual(MeteorContract.AssessmentFirstSpawnSeconds, 0.5f);
            Assert.AreEqual(2, MeteorContract.AssessmentMinOnScreen);
            // con menos de 2 en pantalla el siguiente sale enseguida; con 2 o más, la pausa de siempre
            Assert.AreEqual(MeteorContract.AssessmentRefillGapSeconds, MeteorContract.SpawnGap(false, true, 0), 1e-4f);
            Assert.AreEqual(MeteorContract.AssessmentRefillGapSeconds, MeteorContract.SpawnGap(false, true, 1), 1e-4f);
            Assert.Less(MeteorContract.AssessmentRefillGapSeconds, 0.5f);
            Assert.AreEqual(MeteorContract.NormalGapSeconds, MeteorContract.SpawnGap(false, true, 2), 1e-4f);
            Assert.AreEqual(MeteorContract.ShowerGapSeconds, MeteorContract.SpawnGap(true, true, 0), 1e-4f);
        }

        [Test]
        public void TheNormalGame_KeepsItsSpawnGaps()
        {
            foreach (int active in new[] { 0, 1, 2, 3 })
            {
                Assert.AreEqual(1.1f, MeteorContract.SpawnGap(false, false, active), 1e-4f, "activos " + active);
                Assert.AreEqual(0.45f, MeteorContract.SpawnGap(true, false, active), 1e-4f);
            }
        }

        [Test]
        public void TheGuidedRound_HasEasyWordsAndAnObviousInventedOne()
        {
            var lex = MeteorLexicon.Fallback();
            var rng = new Random(5);
            for (int i = 0; i < 50; i++)
            {
                Assert.IsTrue(lex.PickWord(1, 1, 4, 6, rng, out string w, out int band), "hay palabras de la banda 1 de 4 a 6 letras");
                Assert.AreEqual(1, band);
                Assert.GreaterOrEqual(w.Length, 4);
                Assert.LessOrEqual(w.Length, 6);
                Assert.IsTrue(lex.PickDecoy(new[] { DecoyKind.Obvious }, 4, 6, rng, out string d, out DecoyKind kind, out _), "hay inventadas obvias de 4 a 6 letras");
                Assert.AreEqual(DecoyKind.Obvious, kind);
                Assert.GreaterOrEqual(d.Length, 4);
                Assert.LessOrEqual(d.Length, 6);
            }
        }

        [Test]
        public void TheShortVersion_IsSixtySeconds_AndTheRetoStaysTwoMinutes()
        {
            Assert.AreEqual(60, MeteorContract.RunSeconds(true));
            Assert.AreEqual(MeteorContract.RetoSeconds, MeteorContract.RunSeconds(false));
            Assert.AreEqual(120, MeteorContract.RetoSeconds);
        }

        [Test]
        public void TheShortVersion_AlwaysHasAtLeastTwoMeteorsAtOnce()
        {
            for (int level = 1; level <= MeteorContract.MaxLevel; level++)
            {
                Assert.GreaterOrEqual(MeteorContract.AssessmentConcurrent(level), 2, "nivel " + level);
                Assert.GreaterOrEqual(MeteorContract.AssessmentConcurrent(level), MeteorContract.Spec(level).Concurrent);
            }
        }

        [Test]
        public void TheShortVersion_StartsSoft_SeniorsAtLevelOne_OthersAtThree()
        {
            Assert.AreEqual(1, MeteorContract.CreateEngine(ShortVersionHelper.Config("senior", true)).Level);
            Assert.AreEqual(3, MeteorContract.CreateEngine(ShortVersionHelper.Config("adult", true, timed: true)).Level);
        }

        [Test]
        public void TheShortVersion_EndsWithANormalizedRating()
        {
            var e = ShortVersionHelper.Simulate(MeteorContract.CreateEngine(ShortVersionHelper.Config("adult", true, timed: true)), 20, 0.85, 11);
            Assert.GreaterOrEqual(e.RatingNormalized, 0f);
            Assert.LessOrEqual(e.RatingNormalized, 1f);
        }
    }
}
