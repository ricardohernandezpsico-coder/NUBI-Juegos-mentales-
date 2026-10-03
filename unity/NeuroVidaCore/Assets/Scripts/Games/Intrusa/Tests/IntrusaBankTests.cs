using System;
using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using UnityEngine;
using Random = System.Random;

namespace NeuroVida.Games.Intrusa.Tests
{
    /// <summary>El banco real (Resources/Lexico/intrusa_es.json y intrusa_figuras.json): lo que escriben tools/intrusa/grupos.py y figuras.py.</summary>
    public class IntrusaBankTests
    {
        private static IntrusaBank Real()
        {
            string dir = Path.Combine(Application.dataPath, "Resources/Lexico");
            string g = Path.Combine(dir, "intrusa_es.json"), f = Path.Combine(dir, "intrusa_figuras.json");
            Assert.IsTrue(File.Exists(g), g);
            Assert.IsTrue(File.Exists(f), f);
            return IntrusaBank.FromJson(File.ReadAllText(g), File.ReadAllText(f));
        }

        [Test]
        public void ArchivoReal_960Grupos_160PorTipo_YUnaFiguraPorRegla()
        {
            var bank = Real();
            Assert.AreEqual(960, bank.Count);
            for (int t = 1; t <= 6; t++) Assert.AreEqual(160, bank.OfType(t).Count, "tipo " + t);
            Assert.AreEqual(77, bank.FigureCount);
            var rules = new List<string>(bank.RuleKeys);
            Assert.AreEqual(77, rules.Count);
            foreach (var k in rules) Assert.IsNotNull(bank.FigureOf(k), "sin figura: " + k);
        }

        [Test]
        public void ArchivoReal_CadaFiguraTieneLoQueElJuegoNecesita()
        {
            var bank = Real();
            foreach (var k in bank.RuleKeys)
            {
                var f = bank.FigureOf(k);
                Assert.IsTrue(IntrusaBank.Valid(f), k);
                Assert.GreaterOrEqual(f.PointCount, 7, k);
                Assert.LessOrEqual(f.PointCount, 12, k);
                Assert.IsTrue(f.HoleCount == 2 || f.HoleCount == 3, k);
                foreach (int a in f.anclas) Assert.IsTrue(a >= 0 && a < f.PointCount, k);
                for (int e = 0; e < f.EdgeCount; e++)
                {
                    Assert.IsTrue(f.aristas[e * 2] >= 0 && f.aristas[e * 2] < f.PointCount, k);
                    Assert.IsTrue(f.aristas[e * 2 + 1] >= 0 && f.aristas[e * 2 + 1] < f.PointCount, k);
                }
                for (int i = 0; i < f.puntos.Length; i++) Assert.IsTrue(f.puntos[i] >= 0f && f.puntos[i] <= 1f, k);
                Assert.IsNotNull(f.contorno, k);
                Assert.IsNotNull(f.detalles, k);
            }
        }

        [Test]
        public void ArchivoReal_ElGrupoEstaBienFormado()
        {
            var bank = Real();
            for (int t = 1; t <= 6; t++)
                foreach (var g in bank.OfType(t))
                {
                    Assert.AreEqual(4, g.p.Length, g.i);
                    Assert.AreEqual(3, g.o.Length, g.i);
                    Assert.IsTrue(g.c >= 0 && g.c <= 2, g.i);
                    Assert.AreEqual(g.n, g.o[g.c], g.i);
                    Assert.IsTrue(g.e.Contains(g.x), g.i);
                    Assert.AreEqual(t >= 5, g.IsTrap && !string.IsNullOrEmpty(g.a), g.i);
                }
        }

        [Test]
        public void ArchivoReal_ElDirectorArmaPartidasCompletasEnTodosLosNiveles()
        {
            var bank = Real();
            var rng = new Random(11);
            var dir = new IntrusaDirector(bank, rng);
            for (int level = 1; level <= 12; level++)
                for (int k = 0; k < 8; k++)
                {
                    var next = dir.Next(level);
                    Assert.IsTrue(next.HasValue, $"nivel {level} ronda {k}");
                    var g = next.Value.group;
                    Assert.AreEqual(IntrusaContract.TypeForLevel(level), g.t);
                    var spec = IntrusaLayout.Arrange(g, bank.FigureOf(g.k), rng, w => w.Length * 9.9f);
                    Assert.AreEqual(4, spec.AnchorWords.Length);
                }
        }

        [Test]
        public void SinArchivos_ElRespaldoSirve()
        {
            var bank = IntrusaBank.Fallback();
            Assert.AreEqual(1, bank.Count);
            var dir = new IntrusaDirector(bank, new Random(1));
            Assert.IsTrue(dir.Next(1).HasValue);
        }
    }
}
