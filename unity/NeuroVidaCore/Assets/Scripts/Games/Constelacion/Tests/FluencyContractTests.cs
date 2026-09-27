using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;

namespace NeuroVida.Games.Constelacion.Tests
{
    public class FluencyContractTests
    {
        [Test]
        public void Lexicon_IsBigAndWellFormed()
        {
            var sizes = new Dictionary<string, int> { { "animales", 250 }, { "frutas", 150 }, { "casa", 200 } };
            foreach (var id in FluencyContract.SemanticIds)
            {
                var c = FluencyContract.Get(id);
                Assert.GreaterOrEqual(c.Words.Count, sizes[id], id + ": pocas palabras");
                Assert.GreaterOrEqual(c.GroupNames.Count, 8, id + ": pocos grupos");
                for (int w = 0; w < c.Words.Count; w++)
                {
                    Assert.IsNotEmpty(c.WordGroups[w], c.Words[w]);
                    // Cada palabra se encuentra por su propia forma.
                    Assert.AreEqual(w, c.Lookup[FluencyContract.Normalize(c.Words[w])], c.Words[w]);
                }
                // Cada grupo tiene al menos 6 palabras (si no, no hay constelaciones que armar).
                for (int g = 0; g < c.GroupNames.Count; g++)
                    Assert.GreaterOrEqual(Enumerable.Range(0, c.Words.Count).Count(w => c.WordGroups[w].Contains(g)), 6, c.GroupNames[g]);
            }
        }

        [Test]
        public void Lexicon_NoVariantPointsToTwoWords()
        {
            // Se relee cada línea: una misma variante no puede ser de dos palabras distintas en una categoría.
            foreach (var src in new[] { FluencyLexicon.Animals, FluencyLexicon.Produce, FluencyLexicon.House })
            {
                var owner = new Dictionary<string, string>();
                foreach (var line in src.Split('\n').Where(l => l.Contains(':')))
                    foreach (var item in line.Substring(line.IndexOf(':') + 1).Split(','))
                    {
                        var forms = item.Split('|').Select(f => FluencyContract.Normalize(f)).Where(f => f.Length > 0).ToArray();
                        if (forms.Length == 0) continue;
                        foreach (var f in forms)
                        {
                            if (owner.TryGetValue(f, out var o)) Assert.AreEqual(o, forms[0], $"«{f}» es de «{o}» y de «{forms[0]}»");
                            else owner[f] = forms[0];
                        }
                    }
            }
        }

        [Test]
        public void Normalize_AndVariants()
        {
            Assert.AreEqual("leon", FluencyContract.Normalize("León"));
            Assert.AreEqual("pinguino", FluencyContract.Normalize("pingüino"));
            Assert.AreEqual("ñandu", FluencyContract.Normalize("Ñandú"));
            Assert.AreEqual("estrella de mar", FluencyContract.Normalize("  Estrella, de   mar!"));
            CollectionAssert.Contains(FluencyContract.Variants("peces"), "pez");
            CollectionAssert.Contains(FluencyContract.Variants("leones"), "leon");
            CollectionAssert.Contains(FluencyContract.Variants("perritos"), "perro");
            CollectionAssert.Contains(FluencyContract.Variants("ratoncito"), "raton");
            CollectionAssert.Contains(FluencyContract.Variants("mona"), "mono");
        }

        [Test]
        public void Accept_FindsWordsInSpeech()
        {
            var c = FluencyContract.Get("animales");
            var said = new List<FluencyWord>();
            var unknown = FluencyContract.Accept(c, said, "Perro y gato, un caballo, eh... estrellas de mar no, estrella de mar", 3f);
            CollectionAssert.AreEqual(new[] { "perro", "gato", "caballo", "estrella de mar" }, said.Select(w => w.Display).ToArray());
            // "estrellas" (plural de la primera palabra de una entrada larga) no se ubica: queda como no reconocida.
            CollectionAssert.Contains(unknown, "estrellas");

            // Plural, diminutivo, femenino y variantes regionales cuentan como la misma palabra.
            FluencyContract.Accept(c, said, "perritos leona chancho cerdo palomas", 9f);
            var last = said.Skip(4).ToList();
            CollectionAssert.AreEqual(new[] { "perro", "león", "cerdo", "cerdo", "paloma" }, last.Select(w => w.Display).ToArray());
            CollectionAssert.AreEqual(new[] { true, false, false, true, false }, last.Select(w => w.Repeat).ToArray());
            Assert.AreEqual(9f, last[0].Time);
            Assert.AreEqual(7, FluencyContract.ValidCount(said)); // 4 + león, cerdo y paloma (perro y cerdo repetidos no suman)

            // Si la lectura principal no ubica nada, se usa la otra lectura del reconocedor.
            var said2 = new List<FluencyWord>();
            FluencyContract.Accept(c, said2, "dato", 1f, new[] { "gato" });
            Assert.AreEqual("gato", said2.Single().Display);
        }

        [Test]
        public void Clusters_FollowTroyer()
        {
            var c = FluencyContract.Get("animales");
            var said = new List<FluencyWord>();
            // Granja (3) → mar (2) → suelta → aves que también son de granja…
            FluencyContract.Accept(c, said, "vaca oveja cabra tiburón pulpo canguro", 0f);
            var cl = FluencyContract.Clusters(said);
            CollectionAssert.AreEqual(new[] { 3, 2, 1 }, cl.Select(x => x.Count).ToArray());
            Assert.AreEqual("de la granja", cl[0].Group);
            Assert.AreEqual("del mar", cl[1].Group);
            Assert.IsNull(cl[2].Group);
            // Tamaños desde la segunda palabra: 2, 1, 0 → media 1; dos saltos.
            Assert.AreEqual(1f, FluencyContract.MeanClusterSize(cl), 1e-5f);
            Assert.AreEqual(2, FluencyContract.Switches(cl));

            // Una palabra de varios grupos sigue la constelación por el grupo común: pato (granja + aves + río).
            var s2 = new List<FluencyWord>();
            FluencyContract.Accept(c, s2, "gallina pato cisne", 0f);
            var cl2 = FluencyContract.Clusters(s2);
            Assert.AreEqual(1, cl2.Count);
            Assert.AreEqual("aves", cl2[0].Group);
            Assert.AreEqual(-1f, FluencyContract.MeanClusterSize(new List<FluencyCluster>()));
        }

        [Test]
        public void LetterRound_RulesAndSoundClusters()
        {
            var c = FluencyContract.Get("letra_p");
            Assert.AreEqual(FluencyKind.Letter, c.Kind);
            var said = new List<FluencyWord>();
            var unknown = FluencyContract.Accept(c, said, "pato pala pan pero perrito Pedro mesa pez peces", 2f);
            // "pero" es relleno, "Pedro" es nombre propio, "mesa" no empieza con P, "peces" repite "pez".
            CollectionAssert.AreEqual(new[] { "pato", "pala", "pan", "perrito", "pez", "peces" }, said.Select(w => w.Display).ToArray());
            Assert.IsTrue(said[5].Repeat);
            CollectionAssert.Contains(unknown, "pedro");
            CollectionAssert.Contains(unknown, "mesa");
            // pato, pala, pan empiezan con «pa»: una constelación de 3.
            var cl = FluencyContract.Clusters(said);
            Assert.AreEqual(3, cl[0].Count);
            Assert.AreEqual("empiezan con «pa»", cl[0].Group);
            // Rima: "ruleta" y "receta" terminan igual.
            var rim = new List<FluencyWord>();
            FluencyContract.Accept(FluencyContract.Get("letra_r"), rim, "ruleta receta", 0f);
            Assert.AreEqual(1, FluencyContract.Clusters(rim).Count);
        }

        [Test]
        public void Quarters_Score_AndPlan()
        {
            var words = new List<FluencyWord>
            {
                new FluencyWord { Time = 2f }, new FluencyWord { Time = 10f }, new FluencyWord { Time = 14f, Repeat = true },
                new FluencyWord { Time = 20f }, new FluencyWord { Time = 59f }, new FluencyWord { Time = 61f },
            };
            CollectionAssert.AreEqual(new[] { 2, 1, 0, 2 }, FluencyContract.Quarters(words));
            Assert.AreEqual(100, FluencyContract.Score(new[] { 30, 20 }, new[] { FluencyKind.Semantic, FluencyKind.Letter }));
            Assert.AreEqual(50, FluencyContract.Score(new[] { 11 }, new[] { FluencyKind.Semantic }));
            for (int seed = 0; seed < 30; seed++)
            {
                var plan = FluencyContract.Plan(seed);
                Assert.AreEqual(3, plan.Length);
                Assert.AreEqual("animales", plan[0]);
                Assert.AreNotEqual("animales", plan[1]);
                Assert.AreEqual(FluencyKind.Letter, FluencyContract.Get(plan[2]).Kind);
            }
        }
    }
}
