using System;
using System.Collections.Generic;
using NUnit.Framework;

namespace NeuroVida.Games.Rumbo.Tests
{
    public class HomingContractTests
    {
        [Test]
        public void Trips_AreWellFormed_AtEveryLevel()
        {
            var rng = new Random(7);
            for (int level = 1; level <= HomingContract.MaxLevel; level++)
                for (int k = 0; k < 150; k++)
                {
                    var t = HomingContract.NextTrip(level, k % 2 == 0, rng);
                    Assert.AreEqual(HomingContract.Legs(level), t.Legs);
                    // La vuelta siempre empieza con la base en la niebla, a una distancia razonable.
                    Assert.That(t.HomeDistance, Is.InRange(HomingContract.MinHomeDistance, HomingContract.MaxHomeDistance));
                    // El primer tramo sale derecho hacia arriba.
                    Assert.AreEqual(0f, t.CrystalX[0], 1e-3f);
                    Assert.Greater(t.CrystalY[0], 0f);
                    // Después de la primera parada, la ruta no vuelve a acercarse a la base.
                    for (int i = 1; i < t.Legs; i++)
                        Assert.GreaterOrEqual(HomingContract.SegmentDistance(0f, 0f, t.CrystalX[i - 1], t.CrystalY[i - 1], t.CrystalX[i], t.CrystalY[i]),
                            HomingContract.HomeClearance - 1e-2f);
                    // Los giros quedan dentro del rango del nivel.
                    HomingContract.TurnRange(level, out float tMin, out float tMax);
                    float prev = 0f, px = 0f, py = 0f;
                    for (int i = 0; i < t.Legs; i++)
                    {
                        float h = HomingContract.HeadingOf(t.CrystalX[i] - px, t.CrystalY[i] - py);
                        if (i > 0) Assert.That(Math.Abs(HomingContract.Wrap(h - prev)), Is.InRange(tMin - 0.1f, tMax + 0.1f));
                        prev = h;
                        px = t.CrystalX[i];
                        py = t.CrystalY[i];
                    }
                }
        }

        [Test]
        public void Ladder_GrowsWithLevel()
        {
            Assert.AreEqual(2, HomingContract.Legs(1));
            Assert.AreEqual(5, HomingContract.Legs(HomingContract.MaxLevel));
            for (int l = 2; l <= HomingContract.MaxLevel; l++)
            {
                Assert.GreaterOrEqual(HomingContract.Legs(l), HomingContract.Legs(l - 1));
                Assert.Greater(HomingContract.TurnRate(l), HomingContract.TurnRate(l - 1));
                Assert.IsNotEmpty(HomingContract.LevelNews(l));
            }
        }

        [Test]
        public void Geometry_HeadingsAndWrap()
        {
            Assert.AreEqual(0f, HomingContract.HeadingOf(0f, 1f), 1e-4f);
            Assert.AreEqual(90f, HomingContract.HeadingOf(1f, 0f), 1e-4f);
            Assert.AreEqual(-90f, HomingContract.HeadingOf(-1f, 0f), 1e-4f);
            Assert.AreEqual(180f, HomingContract.HeadingOf(0f, -1f), 1e-4f);
            Assert.AreEqual(-170f, HomingContract.Wrap(190f), 1e-4f);
            Assert.AreEqual(170f, HomingContract.Wrap(-190f), 1e-4f);
            Assert.AreEqual(180f, HomingContract.Wrap(-180f), 1e-4f);
        }

        /// <summary>Triángulo de 3-4-5: arriba 400 y a la derecha 300 (la vuelta mide 500).</summary>
        private static HomingTrip Triangle() => new HomingTrip
        {
            Level = 1, CrystalX = new[] { 0f, 300f }, CrystalY = new[] { 400f, 400f },
        };

        [Test]
        public void Evaluate_PerfectReturn_IsZeroError()
        {
            var t = Triangle();
            Assert.AreEqual(90f, t.ArrivalHeading, 1e-3f);
            Assert.AreEqual(500f, t.HomeDistance, 1e-3f);
            float turn = HomingContract.CorrectTurn(t);
            // Hacia la base: (-300, -400) → rumbo -143,13°; desde 90° hay que girar -233,13 = +126,87 (a la derecha).
            Assert.AreEqual(126.87f, turn, 0.01f);
            var o = HomingContract.Evaluate(t, turn, 500f);
            Assert.AreEqual(0f, o.AngleError, 1e-3f);
            Assert.AreEqual(1f, o.DistanceRatio, 1e-4f);
            Assert.AreEqual(0f, o.ErrorRatio, 1e-3f);
            Assert.AreEqual(1f, o.Along, 1e-4f);
            Assert.AreEqual(0f, o.Lateral, 1e-4f);
            Assert.IsTrue(o.Hit);
            Assert.IsTrue(o.Perfect);
        }

        [Test]
        public void Evaluate_SeparatesHeadingFromDistance()
        {
            var t = Triangle();
            float turn = HomingContract.CorrectTurn(t);

            // Buen rumbo, se queda corto a la mitad: error de distancia puro.
            var shortO = HomingContract.Evaluate(t, turn, 250f);
            Assert.AreEqual(0f, shortO.AngleError, 1e-3f);
            Assert.AreEqual(0.5f, shortO.DistanceRatio, 1e-4f);
            Assert.AreEqual(0.5f, shortO.ErrorRatio, 1e-3f);
            Assert.AreEqual(0.5f, shortO.Along, 1e-4f);
            Assert.AreEqual(0f, shortO.Lateral, 1e-4f);
            Assert.IsFalse(shortO.Hit);

            // Distancia justa, 20° a la derecha: el desvío queda a la derecha (lateral +) y a 2·sen(10°) ≈ 0,347 de casa.
            var right = HomingContract.Evaluate(t, turn + 20f, 500f);
            Assert.AreEqual(20f, right.AngleError, 1e-3f);
            Assert.AreEqual(1f, right.DistanceRatio, 1e-4f);
            Assert.AreEqual(2f * Math.Sin(10.0 * Math.PI / 180.0), right.ErrorRatio, 1e-3f);
            Assert.Greater(right.Lateral, 0.3f);
            Assert.AreEqual(Math.Cos(20.0 * Math.PI / 180.0), right.Along, 1e-3f);
            Assert.IsTrue(right.Hit);
            Assert.IsFalse(right.Perfect);

            // A la izquierda: lateral negativo.
            var left = HomingContract.Evaluate(t, turn - 20f, 500f);
            Assert.AreEqual(-20f, left.AngleError, 1e-3f);
            Assert.Less(left.Lateral, -0.3f);
        }

        [Test]
        public void Points_AndScore_RewardCloseness()
        {
            var t = Triangle();
            float turn = HomingContract.CorrectTurn(t);
            var perfect = HomingContract.Evaluate(t, turn, 500f);
            var near = HomingContract.Evaluate(t, turn + 15f, 500f);
            var far = HomingContract.Evaluate(t, turn + 90f, 500f);
            Assert.Greater(HomingContract.Points(perfect, 1, 1), HomingContract.Points(near, 1, 1));
            Assert.Greater(HomingContract.Points(near, 1, 1), HomingContract.Points(far, 1, 0));
            Assert.AreEqual(0, HomingContract.Points(far, 1, 0));
            Assert.Greater(HomingContract.Points(near, 5, 4), HomingContract.Points(near, 1, 1));

            Assert.AreEqual(-1f, HomingContract.MeanErrorPct(new List<HomingOutcome>()));
            var list = new List<HomingOutcome> { perfect, HomingContract.Evaluate(t, turn, 250f) };
            Assert.AreEqual(25f, HomingContract.MeanErrorPct(list), 0.01f);
            Assert.AreEqual(100, HomingContract.Score(0f, HomingContract.MaxLevel));
            Assert.AreEqual(0, HomingContract.Score(-1f, 1));
            Assert.Greater(HomingContract.Score(10f, 3), HomingContract.Score(30f, 3));
        }

        [Test]
        public void Beacon_ComesInBalancedPairs_AndAnglesSplitByIt()
        {
            var rng = new Random(3);
            bool first = false;
            int with = 0, firstOfPair = 0;
            bool previous = false;
            for (int i = 0; i < 40; i++)
            {
                bool a = HomingContract.BeaconFor(i, rng, ref first);
                if (a) with++;
                if (i % 2 == 1) Assert.AreNotEqual(previous, a); // cada par: uno con faro y otro sin
                else if (a) firstOfPair++;
                previous = a;
            }
            Assert.AreEqual(20, with);
            Assert.That(firstOfPair, Is.InRange(4, 16)); // el orden dentro del par cambia (sin patrón fijo)

            var t = Triangle();
            float turn = HomingContract.CorrectTurn(t);
            var trips = new List<HomingOutcome>
            {
                HomingContract.Evaluate(t, turn + 10f, 500f),
                HomingContract.Evaluate(t, turn - 30f, 500f),
                HomingContract.Evaluate(t, turn + 20f, 500f),
            };
            var beacons = new List<bool> { true, false, true };
            Assert.AreEqual(20f, HomingContract.MeanAbsAngle(trips), 1e-3f);
            Assert.AreEqual(15f, HomingContract.MeanAbsAngle(trips, 1, beacons), 1e-3f);
            Assert.AreEqual(30f, HomingContract.MeanAbsAngle(trips, 0, beacons), 1e-3f);
        }
    }
}
