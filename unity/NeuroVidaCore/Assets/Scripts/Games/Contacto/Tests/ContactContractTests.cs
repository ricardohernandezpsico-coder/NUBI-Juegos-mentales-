using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;

namespace NeuroVida.Games.Contacto.Tests
{
    public class ContactContractTests
    {
        [Test]
        public void Language_IsWellFormed()
        {
            Assert.AreEqual(ContactContract.WordCount, ContactContract.Words.Length);
            Assert.AreEqual(ContactContract.WordCount, ContactContract.Meanings.Length);
            CollectionAssert.AllItemsAreUnique(ContactContract.Words);
            // El orden de aprendizaje recorre todo el idioma, una vez cada palabra.
            CollectionAssert.AreEquivalent(Enumerable.Range(0, ContactContract.WordCount), ContactContract.Curriculum);

            int nouns = 0, colors = 0, counts = 0;
            for (int w = 0; w < ContactContract.WordCount; w++)
            {
                int v = ContactContract.ValueOf(w);
                switch (ContactContract.KindOf(w))
                {
                    case WordKind.Noun: nouns++; Assert.AreEqual(w, ContactContract.NounWord(v)); break;
                    case WordKind.Color: colors++; Assert.AreEqual(w, ContactContract.ColorWord(v)); break;
                    case WordKind.Count: counts++; Assert.AreEqual(w, ContactContract.CountWord(v)); break;
                }
            }
            Assert.AreEqual(ContactContract.ObjectCount, nouns);
            Assert.AreEqual(ContactContract.ColorCount, colors);
            Assert.AreEqual(ContactContract.MaxCount, counts);
            // Los colores se aprenden después de tener cosas del cielo con nombre.
            int firstColor = Array.FindIndex(ContactContract.Curriculum, w => ContactContract.KindOf(w) == WordKind.Color);
            Assert.Greater(firstColor, 3);
        }

        [Test]
        public void Lesson_FollowsCurriculum_FadedFirst_AndDependencies()
        {
            var fresh = ContactContract.BuildLesson(new int[0], new int[0], 1);
            CollectionAssert.AreEqual(new[] { 0, 1, 2 }, fresh.Targets);
            Assert.IsFalse(fresh.ColorOn);
            Assert.IsFalse(fresh.CountOn);

            // Con las 8 primeras cosas del cielo sabidas, llegan los colores: las frases llevan color.
            var colors = ContactContract.BuildLesson(Enumerable.Range(0, 8), new int[0], 3);
            CollectionAssert.AreEqual(new[] { 12, 13, 14, 8 }, colors.Targets);
            Assert.IsTrue(colors.ColorOn);

            // Lo olvidado en el repaso va primero y deja de estar sabido.
            var faded = ContactContract.BuildLesson(Enumerable.Range(0, 8), new[] { 5 }, 1);
            Assert.AreEqual(5, faded.Targets[0]);
            Assert.IsFalse(faded.Known.Contains(5));

            // Un color olvidado sin ninguna cosa del cielo con nombre: primero se aprende una cosa del cielo.
            var dep = ContactContract.BuildLesson(new int[0], new[] { 12 }, 1);
            Assert.Less(dep.Targets.IndexOf(0), dep.Targets.IndexOf(12));

            // Todo sabido: conversación (práctica), sin palabras nuevas.
            var all = ContactContract.BuildLesson(Enumerable.Range(0, ContactContract.WordCount), new int[0], 5);
            Assert.IsTrue(all.Practice);
            Assert.IsTrue(all.ColorOn && all.CountOn);
        }

        [Test]
        public void Scenes_HaveExactlyOneAnswer_AndOnlySayWordsThatCanBeLearned()
        {
            var rng = new Random(7);
            for (int round = 0; round < 400; round++)
            {
                int level = 1 + rng.Next(ContactContract.MaxLevel);
                var known = Enumerable.Range(0, ContactContract.WordCount).Where(_ => rng.Next(3) == 0).ToList();
                var lesson = ContactContract.BuildLesson(known, new int[0], level);
                int focus = lesson.Practice ? -1 : lesson.Targets[rng.Next(lesson.Targets.Count)];
                var s = ContactContract.MakeScene(lesson, focus, ContactContract.ObjectsPerScene(level), rng);

                Assert.AreEqual(ContactContract.ObjectsPerScene(level), s.Things.Length);
                CollectionAssert.AllItemsAreUnique(s.Things);
                if (focus >= 0) CollectionAssert.Contains(s.Phrase, focus);
                foreach (int w in s.Phrase) Assert.IsTrue(lesson.Speakable(w), $"palabra {w} no se puede aprender");
                for (int i = 0; i < s.Things.Length; i++)
                    Assert.AreEqual(i == s.Target, ContactContract.MatchesPhrase(s.Phrase, s.Things[i]));
                foreach (var t in s.Things)
                {
                    Assert.IsTrue(t.Color == -1 || ContactContract.Colorable(t.Obj), "los hallazgos tienen sus colores");
                    Assert.That(t.Count, Is.InRange(1, ContactContract.MaxCount));
                }
            }
        }

        [Test]
        public void Review_HasOneAnswer_PerKindOfWord()
        {
            var rng = new Random(3);
            var lesson = ContactContract.BuildLesson(Enumerable.Range(0, ContactContract.WordCount), new int[0], 5);
            for (int w = 0; w < ContactContract.WordCount; w++)
            {
                var s = ContactContract.ReviewScene(lesson, w, rng);
                Assert.GreaterOrEqual(s.Things.Length, 3);
                CollectionAssert.AllItemsAreUnique(s.Things);
                for (int i = 0; i < s.Things.Length; i++)
                    Assert.AreEqual(i == s.Target, ContactContract.Matches(w, s.Things[i]), $"{ContactContract.Words[w]}");
            }
        }

        [Test]
        public void Decoding_IgnoresTheFirstGuess_AndNeedsTwoInARow()
        {
            var lesson = ContactContract.BuildLesson(new int[0], new int[0], 1);
            int w = lesson.Targets[0];
            var scene = new ContactScene { Things = new[] { new Thing(ContactContract.ValueOf(w)), new Thing(20) }, Target = 0, Phrase = new[] { w }, Focus = w };

            Assert.IsEmpty(ContactContract.Answer(lesson, scene, 0, 0));   // 1ª vez: adivinar, no cuenta
            Assert.IsEmpty(ContactContract.Answer(lesson, scene, 0, 1));   // 1 acierto
            Assert.IsEmpty(ContactContract.Answer(lesson, scene, 1, 2));   // falla: vuelve a cero
            Assert.IsEmpty(ContactContract.Answer(lesson, scene, 0, 3));
            CollectionAssert.AreEqual(new[] { w }, ContactContract.Answer(lesson, scene, 0, 4));
            Assert.AreEqual(5, lesson.Progress[w].HearingsToDecode);
            Assert.IsTrue(lesson.Known.Contains(w));
            Assert.AreEqual(5f, ContactContract.MeanHearings(lesson));
            Assert.IsEmpty(ContactContract.Answer(lesson, scene, 0, 5));   // ya descifrada: no se cuenta más

            // Si no sale en 7 veces, queda para otro día y la partida no insiste.
            int other = lesson.Targets[1];
            var scene2 = new ContactScene { Things = new[] { new Thing(ContactContract.ValueOf(other)), new Thing(20) }, Target = 0, Phrase = new[] { other }, Focus = other };
            for (int i = 0; i < ContactContract.MaxHearings; i++) ContactContract.Answer(lesson, scene2, i % 2, 10 + i);
            Assert.IsTrue(lesson.Progress[other].Parked);
            Assert.IsFalse(lesson.Progress[other].Decoded);
            Assert.AreNotEqual(other, ContactContract.NextFocus(lesson, -1, new Random(1)));
        }

        [Test]
        public void Exclusion_CountsOnlyNewNouns_WithANamedAlternative()
        {
            var lesson = ContactContract.BuildLesson(new[] { 0 }, new int[0], 1);   // ZOBA (planeta) ya se sabe
            int w = lesson.Targets[0];                                             // KITU (cohete), nueva
            Assert.AreEqual(1, w);
            var scene = new ContactScene { Things = new[] { new Thing(1), new Thing(0) }, Target = 0, Phrase = new[] { w }, Focus = w };
            Assert.IsTrue(ContactContract.IsExclusionChance(lesson, scene));
            Assert.IsTrue(ContactContract.Excluded(lesson, scene, 0));
            Assert.IsFalse(ContactContract.Excluded(lesson, scene, 1));   // eligió el planeta, que ya tenía nombre

            var noNamed = new ContactScene { Things = new[] { new Thing(1), new Thing(5) }, Target = 0, Phrase = new[] { w }, Focus = w };
            Assert.IsFalse(ContactContract.IsExclusionChance(lesson, noNamed));
        }

        /// <summary>
        /// Quien deduce (se queda con lo que se repite cada vez que suena la palabra y descarta lo que ya tiene nombre)
        /// descifra todo en pocas escenas, en todos los niveles; quien toca al azar descifra poco. Si no, el juego mediría
        /// suerte.
        /// </summary>
        [Test]
        public void Deducing_Decodes_ButRandomTapping_DoesNot()
        {
            for (int level = 1; level <= ContactContract.MaxLevel; level++)
            {
                float smartHearings = 0f, smartRate = 0f, randomRate = 0f;
                const int games = 40;
                for (int g = 0; g < games; g++)
                {
                    var known = Enumerable.Range(0, 4 * (g % 8)).Select(i => ContactContract.Curriculum[i]).ToList();
                    var smart = Play(known, level, new Random(g * 31 + level), deduce: true);
                    var rand = Play(known, level, new Random(g * 31 + level), deduce: false);
                    smartRate += (float)smart.DecodedCount / smart.Targets.Count;
                    smartHearings += ContactContract.MeanHearings(smart);
                    randomRate += (float)rand.DecodedCount / rand.Targets.Count;
                }
                smartRate /= games;
                smartHearings /= games;
                randomRate /= games;
                Assert.Greater(smartRate, 0.97f, $"nivel {level}: quien deduce descifra casi todo");
                Assert.Less(smartHearings, 3.6f, $"nivel {level}: en pocas escenas");
                if (ContactContract.ObjectsPerScene(level) >= 3)
                    Assert.Less(randomRate, 0.45f, $"nivel {level}: al azar se descifra poco ({randomRate:0.00})");
            }
        }

        private static ContactLesson Play(List<int> known, int level, Random rng, bool deduce)
        {
            var lesson = ContactContract.BuildLesson(known, new int[0], level);
            var candidates = new Dictionary<int, HashSet<int>>();
            int focus = -1;
            for (int s = 0; s < ContactContract.MaxScenes(lesson.Targets.Count) && !lesson.Finished; s++)
            {
                focus = ContactContract.NextFocus(lesson, focus, rng);
                var scene = ContactContract.MakeScene(lesson, focus, ContactContract.ObjectsPerScene(level), rng);
                int tap = deduce ? Deduce(lesson, scene, candidates, rng) : rng.Next(scene.Things.Length);
                ContactContract.Answer(lesson, scene, tap, s);
            }
            return lesson;
        }

        /// <summary>El que deduce: por cada palabra, los valores que estuvieron en TODAS las escenas donde sonó, menos lo
        /// que ya tiene nombre (descarte); toca la cosa que más calza.</summary>
        private static int Deduce(ContactLesson lesson, ContactScene scene, Dictionary<int, HashSet<int>> cand, Random rng)
        {
            foreach (int w in scene.Phrase)
            {
                if (lesson.Known.Contains(w)) continue;
                var here = new HashSet<int>(scene.Things.Select(t => Feature(w, t)));
                if (cand.TryGetValue(w, out var c)) c.IntersectWith(here);
                else cand[w] = here;
                // Descarte: lo que ya tiene nombre (de la misma clase: cosas, colores o números) no es esta palabra.
                var kind = ContactContract.KindOf(w);
                foreach (int k in lesson.Known)
                    if (ContactContract.KindOf(k) == kind) cand[w].Remove(ContactContract.ValueOf(k));
            }
            var best = new List<int>();
            int bestScore = -1;
            for (int i = 0; i < scene.Things.Length; i++)
            {
                int score = 0;
                foreach (int w in scene.Phrase)
                {
                    int f = Feature(w, scene.Things[i]);
                    if (lesson.Known.Contains(w)) score += f == ContactContract.ValueOf(w) ? 10 : -10;
                    else if (cand[w].Contains(f)) score += 1;
                }
                if (score > bestScore) { bestScore = score; best.Clear(); }
                if (score == bestScore) best.Add(i);
            }
            return best[rng.Next(best.Count)];
        }

        private static int Feature(int w, Thing t)
        {
            switch (ContactContract.KindOf(w))
            {
                case WordKind.Color: return t.Color;
                case WordKind.Count: return t.Count;
                default: return t.Obj;
            }
        }

        [Test]
        public void Score_RewardsDecoding_Speed_AndLevel()
        {
            Assert.AreEqual(100, ContactContract.Score(5, 5, 3f, ContactContract.MaxLevel));
            Assert.AreEqual(0, ContactContract.Score(0, 5, -1f, 1));
            Assert.Greater(ContactContract.Score(4, 5, 3.5f, 3), ContactContract.Score(2, 5, 5f, 3));
        }
    }
}
