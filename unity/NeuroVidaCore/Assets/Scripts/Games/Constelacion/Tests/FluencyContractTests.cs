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
            FluencyContract.Accept(c, said, "Perro y gato, un caballo, eh... estrellas de mar no, estrella de mar", 3f);
            // "estrellas de mar": el plural de la primera palabra de una entrada larga también se entiende (y la segunda repite).
            CollectionAssert.AreEqual(new[] { "perro", "gato", "caballo", "estrellas de mar", "estrella de mar" }, said.Select(w => w.Display).ToArray());
            CollectionAssert.AreEqual(new[] { "perro", "gato", "caballo", "estrella de mar", "estrella de mar" }, said.Select(w => w.Canonical).ToArray());
            Assert.IsTrue(said[4].Repeat);
            said.RemoveAt(4);

            // Plural, diminutivo, femenino y variantes regionales cuentan como la misma palabra.
            FluencyContract.Accept(c, said, "perritos leona chancho cerdo palomas", 9f);
            var last = said.Skip(4).ToList();
            CollectionAssert.AreEqual(new[] { "perritos", "leona", "chancho", "cerdo", "palomas" }, last.Select(w => w.Display).ToArray());
            CollectionAssert.AreEqual(new[] { "perro", "león", "cerdo", "cerdo", "paloma" }, last.Select(w => w.Canonical).ToArray());
            CollectionAssert.AreEqual(new[] { true, false, false, true, false }, last.Select(w => w.Repeat).ToArray());
            Assert.AreEqual(9f, last[0].Time);
            Assert.AreEqual(7, FluencyContract.ValidCount(said)); // 4 + león, cerdo y paloma (perro y cerdo repetidos no suman)

            // Si la lectura principal no ubica nada, se usa la otra lectura del reconocedor.
            var said2 = new List<FluencyWord>();
            FluencyContract.Accept(c, said2, "dato", 1f, new[] { "gato" });
            Assert.AreEqual("gato", said2.Single().Display);
        }

        [Test]
        public void Types_AndTokenPositions_FromRicardosTest()
        {
            // Frases reales de la prueba de voz de Ricardo (28-sep).
            var c = FluencyContract.Get("animales");
            var said = new List<FluencyWord>();
            FluencyContract.Accept(c, said, "perro gato oso koala León Puma tigre tigre de bengala", 12f);
            FluencyContract.Accept(c, said, "León del Atlas serpiente Cascabel boa boa constructor", 23f);
            FluencyContract.Accept(c, said, "tiburón tiburón blanco tiburón ballena", 38f);
            CollectionAssert.AreEqual(new[]
            {
                "perro", "gato", "koala", "león", "puma", "tigre", "tigre de bengala", // "oso koala" es el koala
                "león del atlas", "serpiente", "cascabel", "boa", "boa constrictor",
                "tiburón", "tiburón blanco", "tiburón ballena",
            }, said.Select(w => w.Canonical).ToArray());
            Assert.IsTrue(said.All(w => !w.Repeat));
            Assert.AreEqual("boa constrictor", said[11].Display); // error típico del reconocedor: se muestra bien escrito
            Assert.AreEqual("oso koala", said[2].Display);
            // Un tipo queda en los grupos de lo que precisa.
            CollectionAssert.Contains(said[6].Groups, "felinos");

            // "es muy común" no se pega a "elefante" (hay palabras en medio); "común" no suma.
            var s2 = new List<FluencyWord>();
            var unk = FluencyContract.Accept(c, s2, "elefante es muy común erizo escorpión cerdo cerdo hormiguero gallina de Guinea", 0f);
            CollectionAssert.AreEqual(new[] { "elefante", "erizo", "escorpión", "cerdo", "cerdo hormiguero", "gallina de guinea" },
                s2.Select(w => w.Display).ToArray());
            Assert.AreEqual("alacrán", s2[2].Canonical);
            CollectionAssert.Contains(unk, "comun");

            // Posición de cada palabra en la frase (para ponerle la hora en que apareció).
            var read = FluencyContract.Read(c, null, "el perro y un gato montés", null, out _);
            CollectionAssert.AreEqual(new[] { 1, 4 }, read.Select(w => w.TokenIndex).ToArray());
            Assert.AreEqual("gato montés", read[1].Display);
            Assert.AreEqual("gato montés", read[1].Canonical);
            Assert.AreEqual(6, FluencyContract.Tokenize("el perro y un gato montés").Count);
        }

        [Test]
        public void SoundsLike_ForWhatTheRecognizerMisspells()
        {
            var c = FluencyContract.Get("animales");
            Assert.AreEqual(FluencyContract.Phonetic("avoceta"), FluencyContract.Phonetic("avosetta"));
            Assert.AreEqual(FluencyContract.Phonetic("oso koala"), FluencyContract.Phonetic("ozocoala"));
            Assert.AreEqual(1, FluencyContract.EditDistance("fregata", "fragata", 2));

            // Errores reales del reconocedor en las pruebas de Ricardo (28-sep): se entienden y se muestra bien escrito.
            var said = new List<FluencyWord>();
            var unknown = FluencyContract.Accept(c, said, "fregata avosetta boa constructor ozocoala gallinera", 0f);
            CollectionAssert.AreEqual(new[] { "fragata", "avoceta", "boa constrictor", "koala", "gallina de guinea" },
                said.Select(w => w.Display).ToArray());
            Assert.IsEmpty(unknown);

            // Palabras cortas no se "corrigen" (serían adivinanzas): "dato" no es "gato". Ni lo que no se parece a nada.
            var s2 = new List<FluencyWord>();
            var unk2 = FluencyContract.Accept(c, s2, "dato silueta", 0f);
            Assert.IsEmpty(s2);
            CollectionAssert.AreEqual(new[] { "dato", "silueta" }, unk2);

            // La lista de la ronda va al reconocedor, bien escrita y sin los errores típicos.
            CollectionAssert.Contains(c.BiasPhrases, "boa constrictor");
            CollectionAssert.DoesNotContain(c.BiasPhrases, "boa constructor");
            Assert.Greater(c.BiasPhrases.Count, 300);
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
            int animals = 0;
            for (int seed = 0; seed < 200; seed++)
            {
                var plan = FluencyContract.Plan(seed);
                Assert.AreEqual(2, plan.Length);
                Assert.AreEqual(FluencyKind.Semantic, FluencyContract.Get(plan[0]).Kind);
                Assert.AreEqual(FluencyKind.Letter, FluencyContract.Get(plan[1]).Kind);
                if (plan[0] == "animales") animals++;
            }
            Assert.That(animals, Is.InRange(70, 130)); // Animales, la mitad de las veces
        }
    }
}
