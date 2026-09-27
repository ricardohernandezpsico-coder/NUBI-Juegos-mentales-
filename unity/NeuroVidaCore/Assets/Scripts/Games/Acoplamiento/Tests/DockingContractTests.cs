using System;
using System.Collections.Generic;
using NUnit.Framework;

namespace NeuroVida.Games.Acoplamiento.Tests
{
    public class DockingContractTests
    {
        private static Cell[] Cells(params int[] xy)
        {
            var o = new Cell[xy.Length / 2];
            for (int i = 0; i < o.Length; i++) o[i] = new Cell(xy[i * 2], xy[i * 2 + 1]);
            return o;
        }

        [Test]
        public void Chirality_KnownPieces()
        {
            Assert.IsTrue(DockingContract.IsChiral(Cells(0, 0, 0, 1, 0, 2, 1, 0)));        // L: distinta de su reflejo (J)
            Assert.IsTrue(DockingContract.IsChiral(Cells(0, 0, 1, 0, 1, 1, 2, 1)));        // S / Z
            Assert.IsFalse(DockingContract.IsChiral(Cells(0, 0, 1, 0, 2, 0, 1, 1)));       // T: igual a su reflejo
            Assert.IsFalse(DockingContract.IsChiral(Cells(0, 0, 0, 1, 0, 2, 0, 3)));       // I
            Assert.IsFalse(DockingContract.IsChiral(Cells(0, 0, 1, 0, 0, 1, 1, 1)));       // cuadrado
        }

        [Test]
        public void RandomShapes_AreConnectedChiralAndTheRightSize()
        {
            var rng = new Random(3);
            for (int n = 4; n <= 7; n++)
            {
                for (int i = 0; i < 150; i++)
                {
                    var s = DockingContract.RandomChiralShape(n, rng);
                    Assert.AreEqual(n, s.Length);
                    Assert.AreEqual(n, new HashSet<Cell>(s).Count);
                    Assert.IsTrue(DockingContract.IsConnected(s));
                    Assert.IsTrue(DockingContract.IsChiral(s));
                    foreach (var c in s) { Assert.GreaterOrEqual(c.X, 0); Assert.GreaterOrEqual(c.Y, 0); }
                }
            }
        }

        [Test]
        public void Ladder_AndTrials()
        {
            Assert.AreEqual(4, DockingContract.Cells(1));
            Assert.AreEqual(7, DockingContract.Cells(12));
            Assert.AreEqual(90, DockingContract.MaxAngle(1));
            Assert.AreEqual(180, DockingContract.MaxAngle(3));
            Assert.AreEqual(6000, DockingContract.DeadlineMs(1, false));
            Assert.AreEqual(2500, DockingContract.DeadlineMs(12, false));
            var rng = new Random(8);
            int mirrored = 0;
            var disparities = new HashSet<int>();
            for (int i = 0; i < 600; i++)
            {
                var t = DockingContract.NextTrial(6, rng);
                Assert.That(t.Disparity, Is.InRange(0, 180));
                Assert.AreEqual(0, t.AngleDeg % DockingContract.AngleStep(6));
                Assert.AreEqual(DockingContract.Cells(6), t.Shape.Length);
                if (t.Mirrored) mirrored++;
                disparities.Add(t.Disparity);
            }
            Assert.That(mirrored, Is.InRange(240, 360)); // ~mitad
            Assert.GreaterOrEqual(disparities.Count, 10); // de a 15°: muchos ángulos distintos
            for (int i = 0; i < 100; i++) Assert.LessOrEqual(DockingContract.NextTrial(1, rng).Disparity, 90);
        }

        [Test]
        public void Disparity_WrapsAround()
        {
            Assert.AreEqual(0, DockingContract.Disparity(0));
            Assert.AreEqual(90, DockingContract.Disparity(-90));
            Assert.AreEqual(180, DockingContract.Disparity(180));
            Assert.AreEqual(90, DockingContract.Disparity(270));
            Assert.AreEqual(45, DockingContract.Disparity(-315));
        }

        [Test]
        public void RotationSpeed_FromTheSlope()
        {
            // TR = 600 + 4 ms por grado → 250 grados por segundo.
            var d = new List<int>();
            var rt = new List<float>();
            var ok = new List<bool>();
            foreach (int a in new[] { 0, 45, 90, 135, 180, 0, 45, 90, 135, 180 })
            {
                d.Add(a);
                rt.Add(600 + 4 * a);
                ok.Add(true);
            }
            Assert.AreEqual(250, DockingContract.RotationSpeed(d, rt, ok));

            // Los errores no cuentan.
            d.Add(180); rt.Add(100); ok.Add(false);
            Assert.AreEqual(250, DockingContract.RotationSpeed(d, rt, ok));

            // Sin efecto del ángulo (recta plana) o con pocos datos: sin medida.
            var flat = new List<float>();
            foreach (int a in d) flat.Add(900);
            Assert.AreEqual(-1, DockingContract.RotationSpeed(d, flat, ok));
            Assert.AreEqual(-1, DockingContract.RotationSpeed(new List<int> { 0, 90, 180 }, new List<float> { 500, 800, 1100 }, new List<bool> { true, true, true }));
        }

        [Test]
        public void Curve_AveragesByAngle()
        {
            var d = new List<int> { 0, 10, 90, 95, 180 };
            var rt = new List<float> { 500, 700, 1000, 1200, 2000 };
            var ok = new List<bool> { true, true, true, false, true };
            var c = DockingContract.Curve(d, rt, ok);
            Assert.AreEqual(600, c[0]);   // 0° y 10° → columna 0°
            Assert.AreEqual(-1, c[1]);
            Assert.AreEqual(1000, c[2]);  // el error de 95° no cuenta
            Assert.AreEqual(-1, c[3]);
            Assert.AreEqual(2000, c[4]);
        }

        [Test]
        public void Points_AndScore()
        {
            Assert.AreEqual(0, DockingContract.Points(false, 500, 6000, 5, 3));
            Assert.Greater(DockingContract.Points(true, 800, 6000, 1, 1), DockingContract.Points(true, 5000, 6000, 1, 1));
            Assert.Greater(DockingContract.Points(true, 800, 6000, 6, 1), DockingContract.Points(true, 800, 6000, 1, 1));
            Assert.AreEqual(100, DockingContract.Score(1f, DockingContract.MaxLevel));
            Assert.AreEqual(60, DockingContract.Score(1f, 1));
            Assert.Greater(DockingContract.Score(0.8f, 6), DockingContract.Score(0.6f, 6));
        }
    }
}
