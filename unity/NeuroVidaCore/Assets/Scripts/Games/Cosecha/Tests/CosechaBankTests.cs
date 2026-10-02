using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using UnityEngine;
using Random = System.Random;

namespace NeuroVida.Games.Cosecha.Tests
{
    /// <summary>El banco de rondas: elegir por nivel sin repetir, y el archivo real (Resources/Lexico/cosecha_es.json).</summary>
    public class CosechaBankTests
    {
        private static HarvestRound Make(string id, int level, int commons)
        {
            var words = new List<HarvestWord> { new HarvestWord { p = "asociar", b = 2, comun = true, estrella = true } };
            for (int i = 0; i < commons; i++) words.Add(new HarvestWord { p = "pal" + i, b = 1, comun = true });
            return new HarvestRound { id = id, nivel = level, letras = "iraoasc", palabras = words.ToArray() };
        }

        private static CosechaBank StubBank()
        {
            var rounds = new List<HarvestRound>();
            for (int l = 1; l <= 10; l++)
                for (int k = 0; k < 6; k++) rounds.Add(Make($"L{l}_{k}", l, 12 + k * 4));
            return new CosechaBank(rounds);
        }

        [Test]
        public void Pick_DaRondasDelNivelPedido()
        {
            var bank = StubBank();
            var rng = new Random(1);
            for (int l = 1; l <= 10; l++)
                Assert.AreEqual(l, bank.Pick(l, false, rng, null, null).nivel);
        }

        [Test]
        public void Pick_NoRepiteLasUsadasNiLasRecientes()
        {
            var bank = StubBank();
            var rng = new Random(2);
            var used = new HashSet<string>();
            var avoid = new HashSet<string> { "L3_0", "L3_1" };
            for (int i = 0; i < 4; i++)
            {
                var r = bank.Pick(3, false, rng, used, avoid);
                Assert.IsFalse(used.Contains(r.id));
                Assert.IsFalse(avoid.Contains(r.id));
                used.Add(r.id);
            }
            // con todas usadas o evitadas, relaja y aun así da una ronda
            used.UnionWith(new[] { "L3_2", "L3_3", "L3_4", "L3_5" });
            Assert.IsNotNull(bank.Pick(3, false, rng, used, avoid));
        }

        [Test]
        public void Pick_EnMayoresPrefiereLaMitadConMasComunes()
        {
            var bank = StubBank();
            var rng = new Random(3);
            for (int i = 0; i < 40; i++)
            {
                var r = bank.Pick(5, true, rng, null, null);
                Assert.GreaterOrEqual(CosechaContract.CommonTotal(r), 12 + 3 * 4, "solo las 3 rondas con más comunes");
            }
        }

        [Test]
        public void Pick_UnNivelSinRondasUsaElMasCercano()
        {
            var rounds = new List<HarvestRound> { Make("a", 2, 15), Make("b", 8, 15) };
            var bank = new CosechaBank(rounds);
            Assert.AreEqual(2, bank.Pick(3, false, new Random(1), null, null).nivel);
            Assert.AreEqual(8, bank.Pick(10, false, new Random(1), null, null).nivel);
        }

        [Test]
        public void Fallback_TieneRondaYEstrella()
        {
            var bank = CosechaBank.Fallback();
            Assert.AreEqual(1, bank.Count);
            var r = bank.Pick(7, false, new Random(1), null, null);
            Assert.GreaterOrEqual(CosechaContract.CommonTotal(r), 12);
            var star = false;
            foreach (var w in r.palabras) star |= w.estrella && w.IsCommon;
            Assert.IsTrue(star);
        }

        [Test]
        public void FromJson_LeeElFormatoDeJsonUtility()
        {
            string json = "{\"version\":1,\"idioma\":\"es\",\"rondas\":[{\"id\":\"x1\",\"nivel\":4,\"letras\":\"iraoasc\",\"palabras\":[" +
                          "{\"p\":\"asociar\",\"b\":2,\"comun\":true,\"estrella\":true},{\"p\":\"ácaros\",\"b\":4},{\"p\":\"aso\",\"b\":1,\"oculta\":true}]}]}";
            var bank = CosechaBank.FromJson(json);
            Assert.AreEqual(1, bank.Count);
            var r = bank.Pick(4, false, new Random(1), null, null);
            Assert.AreEqual("x1", r.id);
            Assert.AreEqual(3, r.palabras.Length);
            Assert.IsTrue(r.palabras[0].IsCommon && r.palabras[0].estrella);
            Assert.IsFalse(r.palabras[1].comun);
            Assert.IsTrue(r.palabras[1].IsRare);
            Assert.IsTrue(r.palabras[2].oculta);
            Assert.IsFalse(r.palabras[2].IsCommon);
        }

        [Test]
        public void ArchivoReal_600Rondas_Con12ComunesYEstrellaCada()
        {
            string path = Path.Combine(Application.dataPath, "Resources/Lexico/cosecha_es.json");
            Assert.IsTrue(File.Exists(path), path);
            var bank = CosechaBank.FromJson(File.ReadAllText(path));
            Assert.AreEqual(600, bank.Count);
            for (int l = 1; l <= 10; l++) Assert.AreEqual(60, bank.CountOf(l), "rondas del nivel " + l);
            var rng = new Random(5);
            var seen = new HashSet<string>();
            for (int l = 1; l <= 10; l++)
                for (int k = 0; k < 20; k++)
                {
                    var r = bank.Pick(l, k % 2 == 0, rng, null, null);
                    Assert.IsNotNull(r);
                    seen.Add(r.id);
                    Assert.GreaterOrEqual(CosechaContract.CommonTotal(r), 12, r.id);
                    bool star = false;
                    foreach (var w in r.palabras)
                    {
                        star |= w.IsCommon && w.estrella;
                        Assert.IsFalse(w.oculta && w.comun, "una oculta nunca es común: " + w.p);
                    }
                    Assert.IsTrue(star, "estrella común en " + r.id);
                }
            Assert.Greater(seen.Count, 60);
        }
    }
}
