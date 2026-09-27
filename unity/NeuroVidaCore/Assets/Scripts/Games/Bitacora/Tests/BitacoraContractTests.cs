using System;
using System.Collections.Generic;
using NUnit.Framework;

namespace NeuroVida.Games.Bitacora.Tests
{
    public class BitacoraContractTests
    {
        [Test]
        public void SameSeed_SameMission_AndItIsWellFormed()
        {
            for (int level = 1; level <= BitacoraContract.MaxLevel; level++)
                for (int seed = 1; seed < 60; seed++)
                {
                    var a = BitacoraContract.Generate(seed, level);
                    var b = BitacoraContract.Generate(seed, level);
                    // La transmisión y el informe son partidas distintas: la semilla debe rearmar exactamente lo mismo.
                    CollectionAssert.AreEqual(a.Route, b.Route);
                    CollectionAssert.AreEqual(a.Finds, b.Finds);
                    CollectionAssert.AreEqual(a.Drawer, b.Drawer);
                    CollectionAssert.AreEqual(a.PlanetColors, b.PlanetColors);
                    CollectionAssert.AreEqual(a.PlanetX, b.PlanetX);

                    int items = BitacoraContract.Items(level);
                    Assert.AreEqual(items, a.Items);
                    Assert.AreEqual(items + BitacoraContract.ExtraPlanets(level), a.Planets);
                    Assert.LessOrEqual(a.Planets, BitacoraContract.PlanetCount);
                    CollectionAssert.AllItemsAreUnique(a.Route);
                    CollectionAssert.AllItemsAreUnique(a.Finds);
                    CollectionAssert.AllItemsAreUnique(a.PlanetColors);
                    CollectionAssert.AllItemsAreUnique(a.Drawer);
                    foreach (int f in a.Finds) CollectionAssert.Contains(a.Drawer, f);   // todo lo de la misión se puede elegir
                    Assert.AreEqual(items + BitacoraContract.Lures(items), a.Drawer.Length);
                    foreach (int p in a.Route) Assert.That(p, Is.InRange(0, a.Planets - 1));
                    // Planetas separados: nunca dos en el mismo lugar del mapa.
                    for (int i = 0; i < a.Planets; i++)
                        for (int j = i + 1; j < a.Planets; j++)
                            Assert.Greater(Math.Abs(a.PlanetX[i] - a.PlanetX[j]) + Math.Abs(a.PlanetY[i] - a.PlanetY[j]), 0.2f);
                }
            // Semillas distintas, misiones distintas.
            var m1 = BitacoraContract.Generate(1, 6);
            var m2 = BitacoraContract.Generate(2, 6);
            Assert.IsFalse(string.Join(",", m1.Finds) == string.Join(",", m2.Finds) && string.Join(",", m1.Route) == string.Join(",", m2.Route));
        }

        [Test]
        public void Ladder_GrowsWithLevel()
        {
            Assert.AreEqual(3, BitacoraContract.Items(1));
            Assert.AreEqual(8, BitacoraContract.Items(BitacoraContract.MaxLevel));
            for (int l = 2; l <= BitacoraContract.MaxLevel; l++)
                Assert.GreaterOrEqual(BitacoraContract.Items(l), BitacoraContract.Items(l - 1));
            Assert.AreEqual(0, BitacoraContract.ExtraPlanets(1));
            Assert.AreEqual(2, BitacoraContract.ExtraPlanets(6));
            Assert.AreEqual(2, BitacoraContract.Lures(3));
            Assert.AreEqual(5, BitacoraContract.Lures(8));
            Assert.AreEqual(BitacoraContract.FindCount, BitacoraContract.FindNames.Length);
            Assert.AreEqual(BitacoraContract.PlanetCount, BitacoraContract.PlanetNames.Length);
        }

        [Test]
        public void Answers_Intrusions_AndOrder()
        {
            var m = BitacoraContract.Generate(7, 5);
            int lure = -1;
            foreach (int d in m.Drawer) if (!m.IsInMission(d)) { lure = d; break; }
            Assert.GreaterOrEqual(lure, 0);

            var chosen = new List<int>(m.Finds);
            chosen[0] = m.Finds[1];   // confundió el lugar de uno (no es intrusión: estuvo en la misión)
            chosen[1] = lure;         // eligió uno que nunca estuvo: intrusión
            chosen[2] = -1;           // no respondió
            var ok = BitacoraContract.Check(m, chosen);
            Assert.AreEqual(m.Items - 3, BitacoraContract.Count(ok));
            Assert.IsFalse(ok[0]);
            Assert.AreEqual(1, BitacoraContract.Intrusions(m, chosen));

            var route = new List<int>(m.Route);
            Assert.AreEqual(m.Items, BitacoraContract.OrderCorrect(m, route));
            (route[0], route[1]) = (route[1], route[0]);
            Assert.AreEqual(m.Items - 2, BitacoraContract.OrderCorrect(m, route));
        }

        [Test]
        public void Retention_IsWhatWasKeptOfWhatWasLearned()
        {
            var learned = new[] { true, true, true, true, false };
            var recalled = new[] { true, true, false, true, true };  // el 5.º se recordó sin haberlo aprendido: no cuenta
            Assert.AreEqual(0.75f, BitacoraContract.Retention(learned, recalled), 1e-5f);
            Assert.AreEqual(-1f, BitacoraContract.Retention(new[] { false, false }, new[] { true, true }));
            // La máscara viaja a la app y vuelve igual.
            int mask = BitacoraContract.Mask(learned);
            CollectionAssert.AreEqual(learned, BitacoraContract.FromMask(mask, learned.Length));
            Assert.AreEqual(0b01111, mask);
        }

        [Test]
        public void Score_AndPoints()
        {
            Assert.AreEqual(100, BitacoraContract.Score(1f, 1f, BitacoraContract.MaxLevel));
            Assert.Greater(BitacoraContract.Score(0.9f, 0.5f, 4), BitacoraContract.Score(0.5f, 0.5f, 4));
            Assert.Greater(BitacoraContract.Score(0.8f, 1f, 4), BitacoraContract.Score(0.8f, 0f, 4));
            Assert.AreEqual(0, BitacoraContract.Points(false, 5, 3));
            Assert.Greater(BitacoraContract.Points(true, 5, 3), BitacoraContract.Points(true, 1, 1));
        }
    }
}
