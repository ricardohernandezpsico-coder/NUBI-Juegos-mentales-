using System;
using System.Collections.Generic;
using NUnit.Framework;

namespace NeuroVida.Games.Radar.Tests
{
    public class RadarContractTests
    {
        [Test]
        public void Exposure_ShrinksFrom500To40()
        {
            Assert.AreEqual(500, RadarContract.ExposureMs(1));
            Assert.AreEqual(40, RadarContract.ExposureMs(RadarContract.MaxLevel));
            for (int l = 2; l <= RadarContract.MaxLevel; l++)
                Assert.Less(RadarContract.ExposureMs(l), RadarContract.ExposureMs(l - 1));
            // Fuera de rango se acota.
            Assert.AreEqual(500, RadarContract.ExposureMs(0));
            Assert.AreEqual(40, RadarContract.ExposureMs(99));
        }

        [Test]
        public void Difficulty_AddsDistractorsDistanceAndSimilarity()
        {
            Assert.AreEqual(0, RadarContract.DistractorCount(1));
            Assert.AreEqual(0, RadarContract.DistractorCount(3));
            Assert.AreEqual(7, RadarContract.DistractorCount(4));
            Assert.AreEqual(15, RadarContract.DistractorCount(6));
            Assert.AreEqual(23, RadarContract.DistractorCount(12));
            Assert.AreEqual(0, RadarContract.MaxRing(1));
            Assert.AreEqual(2, RadarContract.MaxRing(12));
            Assert.AreEqual(0, RadarContract.CenterTier(1));
            Assert.AreEqual(2, RadarContract.CenterTier(12));
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
                    Assert.That(t.Direction, Is.InRange(0, RadarContract.Directions - 1));
                    Assert.That(t.Ring, Is.InRange(0, RadarContract.MaxRing(level)));
                    Assert.AreEqual(RadarContract.ExposureMs(level), t.ExposureMs);
                    // El centro y su parecido nunca son el mismo ícono.
                    Assert.IsFalse(t.CenterShape == t.DecoyShape && t.CenterVariant == t.DecoyVariant);
                    // Asteroides: la cantidad del nivel, sin repetir y nunca en la casilla del astronauta.
                    Assert.AreEqual(RadarContract.DistractorCount(level), t.Distractors.Length);
                    var seen = new HashSet<int>(t.Distractors);
                    Assert.AreEqual(t.Distractors.Length, seen.Count);
                    Assert.IsFalse(seen.Contains(t.Ring * RadarContract.Directions + t.Direction));
                    foreach (int s in t.Distractors) Assert.That(s, Is.InRange(0, RadarContract.Rings * RadarContract.Directions - 1));
                }
            }
        }

        [Test]
        public void Trials_CoverAllDirectionsAndBothCenterOptions()
        {
            var rng = new Random(3);
            var dirs = new HashSet<int>();
            int shownA = 0;
            for (int i = 0; i < 400; i++)
            {
                var t = RadarContract.NextTrial(5, rng);
                dirs.Add(t.Direction);
                var pairs = RadarContract.CenterPairs[RadarContract.CenterTier(5)];
                foreach (var p in pairs)
                    if (p[0] == t.CenterShape && p[1] == t.CenterVariant) shownA++;
            }
            Assert.AreEqual(RadarContract.Directions, dirs.Count);
            Assert.That(shownA, Is.InRange(120, 280)); // ~mitad y mitad
        }

        [Test]
        public void CenterPairs_NeverUseHelmetOrAsteroid()
        {
            foreach (var tier in RadarContract.CenterPairs)
                foreach (var p in tier)
                {
                    Assert.AreNotEqual(RadarContract.HelmetShape, p[0]);
                    Assert.AreNotEqual(RadarContract.HelmetShape, p[2]);
                    Assert.AreNotEqual(RadarContract.AsteroidShape, p[0]);
                    Assert.AreNotEqual(RadarContract.AsteroidShape, p[2]);
                    Assert.IsFalse(p[0] == p[2] && p[1] == p[3]);
                }
        }

        [Test]
        public void Direction_RoundTripsFromEveryPosition()
        {
            for (int ring = 0; ring < RadarContract.Rings; ring++)
                for (int d = 0; d < RadarContract.Directions; d++)
                {
                    RadarContract.Position(d, ring, out float x, out float y);
                    Assert.AreEqual(d, RadarContract.DirectionFromOffset(x, y));
                    Assert.IsTrue(RadarContract.IsDirectionTap(x, y));
                }
            Assert.AreEqual(0, RadarContract.DirectionFromOffset(0f, 1f));
            Assert.AreEqual(2, RadarContract.DirectionFromOffset(1f, 0f));
            Assert.AreEqual(4, RadarContract.DirectionFromOffset(0f, -1f));
            Assert.AreEqual(6, RadarContract.DirectionFromOffset(-1f, 0f));
            Assert.AreEqual(0, RadarContract.DirectionFromOffset(-0.1f, 1f)); // un poco a la izquierda de arriba
            Assert.AreEqual(7, RadarContract.DirectionFromOffset(-0.7f, 0.7f));
            Assert.IsFalse(RadarContract.IsDirectionTap(0.05f, 0.05f)); // pantalla central
            Assert.IsFalse(RadarContract.IsDirectionTap(2f, 0f));       // lejos del radar
        }

        [Test]
        public void Points_RewardFullAnswersAndStreaks()
        {
            Assert.AreEqual(100, RadarContract.Points(true, true, 1, 1));
            Assert.AreEqual(25, RadarContract.Points(true, false, 5, 3));
            Assert.AreEqual(25, RadarContract.Points(false, true, 5, 3));
            Assert.AreEqual(0, RadarContract.Points(false, false, 5, 3));
            Assert.Greater(RadarContract.Points(true, true, 8, 1), RadarContract.Points(true, true, 1, 1));
            Assert.Greater(RadarContract.Points(true, true, 1, 4), RadarContract.Points(true, true, 1, 1));
            Assert.AreEqual(RadarContract.Points(true, true, 1, 11), RadarContract.Points(true, true, 1, 40)); // bono acotado
        }

        [Test]
        public void Glance_NeedsEnoughTrialsAndIgnoresTheStart()
        {
            Assert.AreEqual(-1, RadarContract.GlanceMs(null));
            Assert.AreEqual(-1, RadarContract.GlanceMs(new List<float> { 500, 400, 300, 200, 150, 120, 100 }));
            // Los primeros 4 (lentos) no cuentan: el resto es 100 ms.
            var list = new List<float> { 500, 500, 500, 500, 100, 100, 100, 100 };
            Assert.AreEqual(100, RadarContract.GlanceMs(list));
            // Media geométrica de la ventana final.
            var mixed = new List<float> { 500, 500, 500, 500, 50, 200, 50, 200 };
            Assert.AreEqual(100, RadarContract.GlanceMs(mixed));
            // Solo los últimos 12.
            var longRun = new List<float>();
            for (int i = 0; i < 20; i++) longRun.Add(400);
            for (int i = 0; i < 12; i++) longRun.Add(80);
            Assert.AreEqual(80, RadarContract.GlanceMs(longRun));
        }

        [Test]
        public void Score_GrowsWithAccuracyAndSpeed()
        {
            Assert.AreEqual(0, RadarContract.Score(0f, 500));
            Assert.AreEqual(100, RadarContract.Score(1f, 40));
            Assert.AreEqual(100, RadarContract.Score(1f, 20)); // más rápido que el piso también es 100
            Assert.Greater(RadarContract.Score(0.8f, 100), RadarContract.Score(0.8f, 300));
            Assert.Greater(RadarContract.Score(0.9f, 200), RadarContract.Score(0.6f, 200));
            Assert.AreEqual(70, RadarContract.Score(0.7f, -1)); // sin vistazo: solo aciertos
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
