using System;
using System.Collections.Generic;
using NUnit.Framework;

namespace NeuroVida.Games.Radar.Tests
{
    public class RadarContractTests
    {
        [Test]
        public void Exposure_ShrinksFrom600To80()
        {
            Assert.AreEqual(600, RadarContract.ExposureMs(1));
            Assert.AreEqual(80, RadarContract.ExposureMs(RadarContract.MaxLevel));
            for (int l = 2; l <= RadarContract.MaxLevel; l++)
                Assert.Less(RadarContract.ExposureMs(l), RadarContract.ExposureMs(l - 1));
            // Fuera de rango se acota.
            Assert.AreEqual(600, RadarContract.ExposureMs(0));
            Assert.AreEqual(80, RadarContract.ExposureMs(99));
        }

        [Test]
        public void Difficulty_AddsAstronautsAndRobots()
        {
            Assert.AreEqual(2, RadarContract.TargetCount(1));
            Assert.AreEqual(3, RadarContract.TargetCount(3));
            Assert.AreEqual(4, RadarContract.TargetCount(6));
            Assert.AreEqual(5, RadarContract.TargetCount(12));
            Assert.AreEqual(0, RadarContract.RobotCount(4));
            Assert.AreEqual(1, RadarContract.RobotCount(5));
            Assert.AreEqual(2, RadarContract.RobotCount(12));
            for (int l = 2; l <= RadarContract.MaxLevel; l++)
                Assert.GreaterOrEqual(RadarContract.TargetCount(l), RadarContract.TargetCount(l - 1));
        }

        [Test]
        public void Trials_AreConsistentAtEveryLevel()
        {
            var rng = new Random(7);
            for (int level = 1; level <= RadarContract.MaxLevel; level++)
            {
                for (int i = 0; i < 200; i++)
                {
                    var t = RadarContract.NextTrial(level, rng);
                    Assert.IsFalse(t.Rain);
                    Assert.AreEqual(RadarContract.ExposureMs(level), t.ExposureMs);
                    Assert.AreEqual(RadarContract.TargetCount(level), t.Targets.Length);
                    Assert.AreEqual(RadarContract.RobotCount(level), t.Robots.Length);
                    // Sin repetir, dentro del radar, y nunca un robot sobre un astronauta.
                    var seen = new HashSet<int>();
                    foreach (int s in t.Targets) Assert.IsTrue(seen.Add(s));
                    foreach (int s in t.Robots) Assert.IsTrue(seen.Add(s));
                    foreach (int s in seen) Assert.That(s, Is.InRange(0, RadarContract.Slots - 1));
                }
            }
        }

        [Test]
        public void Trials_CoverEveryDirectionAndBothRings()
        {
            var rng = new Random(3);
            var dirs = new HashSet<int>();
            var rings = new HashSet<int>();
            for (int i = 0; i < 300; i++)
                foreach (int s in RadarContract.NextTrial(6, rng).Targets)
                {
                    dirs.Add(RadarContract.DirectionOf(s));
                    rings.Add(RadarContract.RingOf(s));
                }
            Assert.AreEqual(RadarContract.Directions, dirs.Count);
            Assert.AreEqual(RadarContract.Rings, rings.Count);
        }

        [Test]
        public void Rain_ComesEveryFifthRoundWithSixAndALongFlash()
        {
            Assert.IsFalse(RadarContract.IsRainTrial(0));
            Assert.IsFalse(RadarContract.IsRainTrial(3));
            Assert.IsTrue(RadarContract.IsRainTrial(4));
            Assert.IsTrue(RadarContract.IsRainTrial(9));
            Assert.IsFalse(RadarContract.IsRainTrial(10));
            var t = RadarContract.RainTrial(9, new Random(1));
            Assert.IsTrue(t.Rain);
            Assert.AreEqual(RadarContract.RainTargets, t.Targets.Length);
            Assert.AreEqual(0, t.Robots.Length);
            Assert.AreEqual(RadarContract.RainExposureMs, t.ExposureMs);
            Assert.AreEqual(RadarContract.RainTargets, new HashSet<int>(t.Targets).Count);
        }

        [Test]
        public void NearestSlot_RoundTripsAndIgnoresCenterAndOutside()
        {
            for (int s = 0; s < RadarContract.Slots; s++)
            {
                RadarContract.Position(s, out float x, out float y);
                Assert.AreEqual(s, RadarContract.NearestSlot(x, y));
                // Un toque un poco corrido también elige ese lugar.
                Assert.AreEqual(s, RadarContract.NearestSlot(x * 1.08f + 0.02f, y * 1.08f));
            }
            Assert.AreEqual(RadarContract.SlotOf(0, 1), RadarContract.NearestSlot(0f, 0.95f)); // arriba, lejos
            Assert.AreEqual(RadarContract.SlotOf(2, 0), RadarContract.NearestSlot(0.4f, 0f));  // derecha, cerca
            Assert.AreEqual(-1, RadarContract.NearestSlot(0.05f, 0.05f)); // mira del centro
            Assert.AreEqual(-1, RadarContract.NearestSlot(2f, 0f));       // lejos del radar
        }

        [Test]
        public void Evaluate_CountsHitsExtrasAndRobots()
        {
            var t = new RadarTrial { Targets = new[] { 1, 5, 9, 12 }, Robots = new[] { 3 } };
            var a = RadarContract.Evaluate(t, new[] { 1, 5, 3, 7 });
            Assert.AreEqual(2, a.Hits);
            Assert.AreEqual(2, a.Extras);
            Assert.AreEqual(1, a.RobotsTouched);
            // Repetir una baliza no suma dos veces.
            Assert.AreEqual(1, RadarContract.Evaluate(t, new[] { 9, 9 }).Hits);
            Assert.AreEqual(0, RadarContract.Evaluate(t, new int[0]).Hits);
        }

        [Test]
        public void Success_NeedsAllUpToThreeAndAllButOneAfter()
        {
            Assert.AreEqual(2, RadarContract.Needed(2));
            Assert.AreEqual(3, RadarContract.Needed(3));
            Assert.AreEqual(3, RadarContract.Needed(4));
            Assert.AreEqual(4, RadarContract.Needed(5));
            var three = new RadarTrial { Targets = new[] { 1, 2, 3 }, Robots = new int[0] };
            Assert.IsTrue(RadarContract.Success(three, new RadarAnswer { Hits = 3 }));
            Assert.IsFalse(RadarContract.Success(three, new RadarAnswer { Hits = 2, Extras = 1 }));
            var four = new RadarTrial { Targets = new[] { 1, 2, 3, 4 }, Robots = new int[0] };
            Assert.IsTrue(RadarContract.Success(four, new RadarAnswer { Hits = 3, Extras = 1 }));
        }

        [Test]
        public void Points_RewardRescuesSuccessAndStreaks()
        {
            Assert.AreEqual(0, RadarContract.Points(0, false, 1, 0));
            Assert.AreEqual(80, RadarContract.Points(2, false, 1, 0));
            Assert.AreEqual(140, RadarContract.Points(2, true, 1, 1));
            Assert.Greater(RadarContract.Points(3, true, 8, 1), RadarContract.Points(3, true, 1, 1));
            Assert.Greater(RadarContract.Points(3, true, 1, 4), RadarContract.Points(3, true, 1, 1));
            Assert.AreEqual(RadarContract.Points(3, true, 1, 11), RadarContract.Points(3, true, 1, 40)); // bono acotado
        }

        [Test]
        public void Glance_NeedsEnoughRoundsAndIgnoresTheStart()
        {
            Assert.AreEqual(-1, RadarContract.GlanceMs(null));
            Assert.AreEqual(-1, RadarContract.GlanceMs(new List<float> { 500, 400, 300, 200, 150, 120, 100 }));
            Assert.AreEqual(100, RadarContract.GlanceMs(new List<float> { 500, 500, 500, 500, 100, 100, 100, 100 }));
            Assert.AreEqual(100, RadarContract.GlanceMs(new List<float> { 500, 500, 500, 500, 50, 200, 50, 200 }));
            var longRun = new List<float>();
            for (int i = 0; i < 20; i++) longRun.Add(400);
            for (int i = 0; i < 12; i++) longRun.Add(80);
            Assert.AreEqual(80, RadarContract.GlanceMs(longRun));
            // La carga del vistazo usa las mismas rondas.
            Assert.AreEqual(-1f, RadarContract.GlanceLoad(new List<int> { 2, 2, 3 }));
            Assert.AreEqual(3.5f, RadarContract.GlanceLoad(new List<int> { 2, 2, 2, 2, 3, 4, 3, 4 }), 1e-4f);
        }

        [Test]
        public void Capture_AveragesRainsAndDiscountsGuesses()
        {
            Assert.AreEqual(4, RadarContract.RainScore(new RadarAnswer { Hits = 4 }));
            Assert.AreEqual(2, RadarContract.RainScore(new RadarAnswer { Hits = 4, Extras = 2 }));
            Assert.AreEqual(0, RadarContract.RainScore(new RadarAnswer { Hits = 1, Extras = 3 }));
            Assert.AreEqual(-1f, RadarContract.Capture(new List<int> { 4 }));
            Assert.AreEqual(3.5f, RadarContract.Capture(new List<int> { 4, 3 }), 1e-4f);
        }

        [Test]
        public void Score_GrowsWithAccuracyAndSpeed()
        {
            Assert.AreEqual(0, RadarContract.Score(0f, 600));
            Assert.AreEqual(100, RadarContract.Score(1f, 80));
            Assert.AreEqual(100, RadarContract.Score(1f, 40)); // más rápido que el piso también es 100
            Assert.Greater(RadarContract.Score(0.8f, 120), RadarContract.Score(0.8f, 300));
            Assert.Greater(RadarContract.Score(0.9f, 200), RadarContract.Score(0.6f, 200));
            Assert.AreEqual(70, RadarContract.Score(0.7f, -1)); // sin vistazo: solo rondas logradas
        }

        [Test]
        public void DirectionNames_WrapAround()
        {
            Assert.AreEqual("arriba", RadarContract.DirectionName(0));
            Assert.AreEqual("a la izquierda", RadarContract.DirectionName(6));
            Assert.AreEqual("arriba", RadarContract.DirectionName(8));
            Assert.AreEqual("arriba a la izquierda", RadarContract.DirectionName(-1));
        }
    }
}
