using System;
using System.Collections.Generic;
using NUnit.Framework;

namespace NeuroVida.Games.Satelites.Tests
{
    public class SatelliteContractTests
    {
        [Test]
        public void Ladder_NeverGetsEasier()
        {
            for (int l = 2; l <= SatelliteContract.MaxLevel; l++)
            {
                Assert.GreaterOrEqual(SatelliteContract.Targets(l), SatelliteContract.Targets(l - 1));
                Assert.GreaterOrEqual(SatelliteContract.Total(l), SatelliteContract.Total(l - 1));
                Assert.Greater(SatelliteContract.TrackSeconds(l), SatelliteContract.TrackSeconds(l - 1));
                // Si no cambia la cantidad a seguir, sube la velocidad.
                if (SatelliteContract.Targets(l) == SatelliteContract.Targets(l - 1))
                    Assert.Greater(SatelliteContract.Speed(l), SatelliteContract.Speed(l - 1));
                // Siempre hay más satélites que los que se siguen (si no, no hay nada que confundir).
                Assert.Greater(SatelliteContract.Total(l), SatelliteContract.Targets(l));
            }
            Assert.AreEqual(1f, SatelliteContract.SpeedFactor(1), 1e-5f);
            Assert.AreEqual(2, SatelliteContract.Targets(0));
            Assert.AreEqual(5, SatelliteContract.Targets(99));
            Assert.AreEqual(5f, SatelliteContract.TrackSeconds(1), 1e-5f);
            Assert.AreEqual(8f, SatelliteContract.TrackSeconds(12), 1e-5f);
        }

        [Test]
        public void Swarm_SpawnsApartAndInsideTheField()
        {
            for (int seed = 0; seed < 30; seed++)
            {
                var s = new SatelliteSwarm(11, 1.3f, 0.3f, new Random(seed));
                for (int i = 0; i < s.Count; i++)
                {
                    Assert.That(s.X[i], Is.InRange(SatelliteSwarm.Radius - 1e-4f, 1f - SatelliteSwarm.Radius + 1e-4f));
                    Assert.That(s.Y[i], Is.InRange(SatelliteSwarm.Radius - 1e-4f, 1.3f - SatelliteSwarm.Radius + 1e-4f));
                    for (int j = 0; j < i; j++)
                        Assert.Greater(Dist(s, i, j), SatelliteSwarm.MinGap);
                }
            }
        }

        [Test]
        public void Swarm_StaysInsideAndRarelyOverlaps()
        {
            var s = new SatelliteSwarm(11, 1.3f, 0.52f, new Random(4));
            int overlaps = 0, checks = 0;
            for (int step = 0; step < 60 * 30; step++) // 30 s a 60 cuadros por segundo
            {
                s.Step(1f / 60f);
                for (int i = 0; i < s.Count; i++)
                {
                    Assert.That(s.X[i], Is.InRange(SatelliteSwarm.Radius - 1e-4f, 1f - SatelliteSwarm.Radius + 1e-4f));
                    Assert.That(s.Y[i], Is.InRange(SatelliteSwarm.Radius - 1e-4f, 1.3f - SatelliteSwarm.Radius + 1e-4f));
                    for (int j = 0; j < i; j++)
                    {
                        checks++;
                        // Nunca se tapan del todo (a lo más se rozan un poco en un amontonamiento contra un borde).
                        Assert.Greater(Dist(s, i, j), SatelliteSwarm.Radius * 1.5f);
                        if (Dist(s, i, j) < SatelliteSwarm.MinGap * 0.98f) overlaps++;
                    }
                }
            }
            Assert.Less(overlaps, checks / 200);
        }

        [Test]
        public void Swarm_MovesAtTheRequestedSpeedAndIsReproducible()
        {
            var a = new SatelliteSwarm(2, 1.3f, 0.25f, new Random(9));
            // Un satélite lejos de todo avanza exactamente speed * dt.
            a.X[0] = 0.5f; a.Y[0] = 0.65f; a.X[1] = 0.1f; a.Y[1] = 0.1f;
            float x0 = a.X[0], y0 = a.Y[0];
            a.Step(0.1f);
            float moved = (float)Math.Sqrt((a.X[0] - x0) * (a.X[0] - x0) + (a.Y[0] - y0) * (a.Y[0] - y0));
            Assert.AreEqual(0.025f, moved, 1e-4f);

            // Misma semilla, mismo recorrido.
            var c = new SatelliteSwarm(8, 1.3f, 0.3f, new Random(21));
            var d = new SatelliteSwarm(8, 1.3f, 0.3f, new Random(21));
            for (int i = 0; i < 300; i++) { c.Step(1f / 60f); d.Step(1f / 60f); }
            for (int i = 0; i < 8; i++)
            {
                Assert.AreEqual(c.X[i], d.X[i], 1e-6f);
                Assert.AreEqual(c.Y[i], d.Y[i], 1e-6f);
            }
        }

        [Test]
        public void Nearest_FindsTheTappedSatellite()
        {
            var s = new SatelliteSwarm(3, 1.3f, 0.2f, new Random(1));
            s.X[0] = 0.2f; s.Y[0] = 0.2f;
            s.X[1] = 0.8f; s.Y[1] = 0.2f;
            s.X[2] = 0.5f; s.Y[2] = 1.0f;
            Assert.AreEqual(1, s.Nearest(0.78f, 0.23f, 0.1f));
            Assert.AreEqual(2, s.Nearest(0.5f, 1.05f, 0.1f));
            Assert.AreEqual(-1, s.Nearest(0.5f, 0.5f, 0.1f));
        }

        [Test]
        public void TrackedEstimate_DiscountsLuckyGuesses()
        {
            // Todo bien: se siguieron todos.
            Assert.AreEqual(4f, SatelliteContract.TrackedEstimate(4, 4, 8), 1e-4f);
            // Nada: cero.
            Assert.AreEqual(0f, SatelliteContract.TrackedEstimate(0, 4, 8), 1e-3f);
            // Lo que se acierta al azar (k²/n = 2 de 4 entre 8) equivale a no haber seguido ninguno.
            Assert.AreEqual(0f, SatelliteContract.TrackedEstimate(2, 4, 8), 1e-3f);
            // 3 de 4 entre 8: se siguieron de verdad entre 2 y 3 (uno de los 3 pudo ser suerte).
            float m = SatelliteContract.TrackedEstimate(3, 4, 8);
            Assert.That(m, Is.InRange(2f, 3f));
            // Más aciertos, más seguimiento.
            Assert.Greater(SatelliteContract.TrackedEstimate(4, 5, 10), SatelliteContract.TrackedEstimate(3, 5, 10));
            // Casos raros no rompen.
            Assert.AreEqual(0f, SatelliteContract.TrackedEstimate(2, 3, 3));
            Assert.AreEqual(0f, SatelliteContract.TrackedEstimate(1, 0, 6));
        }

        [Test]
        public void Capacity_AveragesRounds()
        {
            Assert.AreEqual(-1f, SatelliteContract.Capacity(new List<(int, int, int)>()));
            var rounds = new List<(int, int, int)> { (3, 3, 7), (4, 4, 8) };
            Assert.AreEqual(3.5f, SatelliteContract.Capacity(rounds), 1e-4f);
        }

        [Test]
        public void Points_AndScore()
        {
            Assert.AreEqual(80, SatelliteContract.Points(2, 3, 1, 0));
            Assert.AreEqual(3 * 40 + 100, SatelliteContract.Points(3, 3, 1, 1));
            Assert.Greater(SatelliteContract.Points(3, 3, 6, 1), SatelliteContract.Points(3, 3, 1, 1));
            Assert.Greater(SatelliteContract.Points(3, 3, 1, 4), SatelliteContract.Points(3, 3, 1, 1));
            Assert.AreEqual(0, SatelliteContract.Score(0f, 0));
            Assert.AreEqual(100, SatelliteContract.Score(1f, SatelliteContract.MaxLevel));
            Assert.AreEqual(60, SatelliteContract.Score(1f, 0));
            Assert.Greater(SatelliteContract.Score(0.8f, 6), SatelliteContract.Score(0.8f, 3));
        }

        [Test]
        public void ClosestApproach_FindsTheCrossing()
        {
            var ax = new List<float> { 0f, 0.2f, 0.4f, 0.6f };
            var ay = new List<float> { 0f, 0f, 0f, 0f };
            var bx = new List<float> { 0.6f, 0.45f, 0.42f, 0.2f };
            var by = new List<float> { 0.5f, 0.2f, 0.01f, 0.3f };
            Assert.AreEqual(2, SatelliteContract.ClosestApproach(ax, ay, bx, by));
            Assert.AreEqual(-1, SatelliteContract.ClosestApproach(new List<float>(), ay, bx, by));
        }

        private static float Dist(SatelliteSwarm s, int i, int j)
        {
            float dx = s.X[i] - s.X[j], dy = s.Y[i] - s.Y[j];
            return (float)Math.Sqrt(dx * dx + dy * dy);
        }
    }
}
