using System;
using System.Collections.Generic;
using NUnit.Framework;

namespace NeuroVida.Games.Meteoros.Tests
{
    public class MeteorContractTests
    {
        /// <summary>Léxico falso y determinista: la palabra lleva su banda y un número ("p3_17"), las inventadas su tipo.</summary>
        private sealed class StubLexicon : ILexicon
        {
            private int _n;
            public bool PickWord(int minBand, int maxBand, int minLen, int maxLen, Random rng, out string word, out int band)
            {
                band = minBand + rng.Next(maxBand - minBand + 1);
                word = $"p{band}_{_n++}";
                return true;
            }

            public bool PickDecoy(DecoyKind[] kinds, int minLen, int maxLen, Random rng, out string word, out DecoyKind kind, out int band)
            {
                kind = kinds[rng.Next(kinds.Length)];
                band = kind == DecoyKind.Obvious ? 0 : 3;
                word = $"x{(int)kind}_{_n++}";
                return true;
            }
        }

        [Test]
        public void LevelTable_IsMonotonic()
        {
            for (int l = 2; l <= MeteorContract.MaxLevel; l++)
            {
                var a = MeteorContract.Spec(l - 1);
                var b = MeteorContract.Spec(l);
                Assert.GreaterOrEqual(b.MinBand, a.MinBand, $"banda mínima, nivel {l}");
                Assert.GreaterOrEqual(b.MaxBand, a.MaxBand, $"banda máxima, nivel {l}");
                Assert.LessOrEqual(b.FallSeconds, a.FallSeconds, $"caída, nivel {l}");
                Assert.GreaterOrEqual(b.Concurrent, a.Concurrent, $"a la vez, nivel {l}");
                Assert.GreaterOrEqual(b.MaxLen, a.MaxLen, $"largo máximo, nivel {l}");
            }
            for (int l = 1; l <= MeteorContract.MaxLevel; l++)
            {
                var s = MeteorContract.Spec(l);
                Assert.IsNotEmpty(s.Decoys);
                Assert.LessOrEqual(s.MinBand, s.MaxBand);
                Assert.LessOrEqual(s.MinLen, s.MaxLen);
                Assert.That(s.Concurrent, Is.InRange(1, 3));
            }
            Assert.AreEqual(1, MeteorContract.Spec(1).MaxBand);
            Assert.AreEqual(6, MeteorContract.Spec(12).MaxBand);
            Assert.AreEqual(7f, MeteorContract.Spec(1).FallSeconds);
            Assert.AreEqual(4f, MeteorContract.Spec(12).FallSeconds);
            Assert.AreEqual(new[] { DecoyKind.Transposed }, MeteorContract.Spec(12).Decoys);
        }

        [Test]
        public void ModeAndAgeAdjustments()
        {
            float basic = MeteorContract.FallSeconds(5, false, false);
            Assert.AreEqual(basic * 1.5f, MeteorContract.FallSeconds(5, true, false), 1e-4f);
            Assert.AreEqual(basic * 1.25f, MeteorContract.FallSeconds(5, false, true), 1e-4f);
            Assert.AreEqual(2, MeteorContract.Concurrent(12, true));     // Precisión: máximo 2
            Assert.AreEqual(3, MeteorContract.Concurrent(12, false));
            Assert.AreEqual(1, MeteorContract.Concurrent(1, true));
            Assert.Greater(MeteorContract.WordSizeDp(true), MeteorContract.WordSizeDp(false));
            Assert.GreaterOrEqual(MeteorContract.WordSizeDp(false), 24);
            Assert.AreEqual(64f, MeteorContract.MinTouchDp(true));
            Assert.AreEqual(56f, MeteorContract.MinTouchDp(false));
        }

        [Test]
        public void Director_HalfWordsAndNeverMoreThanThreeDecoysInARow()
        {
            var d = new MeteorDirector(new StubLexicon(), new Random(7));
            int words = 0, decoys = 0, run = 0, maxRun = 0, normal = 0;
            for (int i = 0; i < 4000; i++)
            {
                var m = d.Next(6, false, false);
                if (m.Shower) { run = 0; continue; } // la lluvia son palabras: corta la racha de inventadas y no entra en la proporción
                normal++;
                if (m.IsWord) { words++; run = 0; }
                else { decoys++; run++; maxRun = Math.Max(maxRun, run); }
            }
            Assert.LessOrEqual(maxRun, MeteorContract.MaxDecoyRun);
            float decoyShare = decoys / (float)normal;
            Assert.That(decoyShare, Is.InRange(0.38f, 0.5f), $"inventadas {decoys} de {normal}");
            Assert.Greater(words, 0);
        }

        [Test]
        public void Director_DecoysAndWordsFollowTheLevel()
        {
            var d = new MeteorDirector(new StubLexicon(), new Random(3));
            for (int i = 0; i < 60; i++)
            {
                var m = d.Next(1, false, false);
                if (m.Shower || m.Golden) continue;
                if (m.IsWord) Assert.AreEqual(1, m.Band, "nivel 1: solo la banda 1");
                else Assert.AreEqual(DecoyKind.Obvious, m.Decoy, "nivel 1: solo señuelos obvios");
            }
            var hard = new MeteorDirector(new StubLexicon(), new Random(4));
            for (int i = 0; i < 60; i++)
            {
                var m = hard.Next(12, false, false);
                if (m.Shower || m.Golden) continue;
                if (m.IsWord) Assert.That(m.Band, Is.InRange(5, 6));
                else Assert.AreEqual(DecoyKind.Transposed, m.Decoy);
            }
        }

        [Test]
        public void Golden_OneInTwelve_TwoBandsRarer_DoublePoints()
        {
            Assert.IsTrue(MeteorContract.IsGolden(11));
            Assert.IsTrue(MeteorContract.IsGolden(23));
            Assert.IsFalse(MeteorContract.IsGolden(0));
            Assert.IsFalse(MeteorContract.IsGolden(12));
            Assert.AreEqual(3, MeteorContract.GoldenBand(1));   // banda máxima 1 + 2
            Assert.AreEqual(6, MeteorContract.GoldenBand(12));  // tope 6
            var d = new MeteorDirector(new StubLexicon(), new Random(1));
            int golden = 0;
            for (int i = 0; i < 24; i++)
            {
                var m = d.Next(1, false, false);
                if (!m.Golden) continue;
                golden++;
                Assert.IsTrue(m.IsWord);
                Assert.AreEqual(3, m.Band);
            }
            Assert.AreEqual(2, golden);
            Assert.AreEqual(2 * MeteorContract.Points(true, 5, 1, false, false), MeteorContract.Points(true, 5, 1, true, false));
        }

        [Test]
        public void Shower_EveryTwentyFive_SixFastCommonWords()
        {
            var d = new MeteorDirector(new StubLexicon(), new Random(9));
            for (int i = 0; i < 25; i++)
            {
                Assert.IsFalse(d.InShower);
                Assert.IsFalse(d.Next(8, false, false).Shower);
            }
            Assert.IsTrue(d.InShower);
            float normalFall = MeteorContract.FallSeconds(8, false, false);
            for (int i = 0; i < MeteorContract.ShowerSize; i++)
            {
                var m = d.Next(8, false, false);
                Assert.IsTrue(m.Shower);
                Assert.IsTrue(m.IsWord);
                Assert.AreEqual(1, m.Band);
                Assert.Less(m.FallSeconds, normalFall);
            }
            Assert.IsFalse(d.InShower);
            Assert.AreEqual(25, d.Spawned); // la lluvia no cuenta como meteoros de la escalera
            Assert.IsFalse(d.Next(8, false, false).Shower);
        }

        [Test]
        public void Points_StreakGoldenAndSilentPass()
        {
            int basePts = MeteorContract.Points(true, 1, 1, false, false);
            Assert.AreEqual(100, basePts);
            Assert.AreEqual((int)Math.Round(basePts * 1.5f), MeteorContract.Points(true, 1, MeteorContract.StreakGlow, false, false));
            Assert.AreEqual(2 * basePts, MeteorContract.Points(true, 1, 1, false, true)); // lluvia de estrellas × 2
            Assert.Greater(MeteorContract.Points(true, 9, 1, false, false), basePts);
            int pass = MeteorContract.Points(false, 1, 1, false, false);
            Assert.Greater(pass, 0);
            Assert.Less(pass, basePts);
        }

        [Test]
        public void ScoreAndBalancedAccuracy()
        {
            Assert.AreEqual(1f, MeteorContract.BalancedAccuracy(10, 10, 10, 10), 1e-5f);
            Assert.AreEqual(0.5f, MeteorContract.BalancedAccuracy(10, 10, 10, 0), 1e-5f); // tocar todo no es saber
            Assert.AreEqual(0.5f, MeteorContract.BalancedAccuracy(10, 0, 10, 10), 1e-5f); // no tocar nada tampoco
            Assert.AreEqual(0f, MeteorContract.BalancedAccuracy(0, 0, 0, 0));
            Assert.AreEqual(100, MeteorContract.Score(1f, MeteorContract.MaxLevel));
            Assert.AreEqual(0, MeteorContract.Score(0f, 1));
            Assert.Greater(MeteorContract.Score(0.9f, 5), MeteorContract.Score(0.6f, 5));
            Assert.Greater(MeteorContract.Score(0.8f, 9), MeteorContract.Score(0.8f, 3));
        }

        [Test]
        public void BandPercents_SubtractFalseAlarms_AndRequireEnoughWords()
        {
            var seen = new[] { 10, 10, 10, 10, 10, 3 };
            var hits = new[] { 10, 9, 8, 7, 5, 3 };
            // 20 inventadas vistas y 2 tocadas = 10% de falsas alarmas
            var r = MeteorContract.BandPercents(seen, hits, 20, 2);
            Assert.AreEqual(new[] { 90, 80, 70, 60, 40, -1 }, r);
            // sin falsas alarmas se ve el % tal cual
            Assert.AreEqual(100, MeteorContract.BandPercents(seen, hits, 20, 0)[0]);
            // nunca bajo 0
            Assert.AreEqual(0, MeteorContract.BandPercents(new[] { 10, 0, 0, 0, 0, 0 }, new[] { 1, 0, 0, 0, 0, 0 }, 10, 5)[0]);
            // sin datos
            Assert.AreEqual(new[] { -1, -1, -1, -1, -1, -1 }, MeteorContract.BandPercents(new int[6], new int[6], 0, 0));
        }

        [Test]
        public void MedianMs_NeedsFiveSamples()
        {
            Assert.AreEqual(-1, MeteorContract.MedianMs(new List<int> { 700, 800, 900, 1000 }));
            Assert.AreEqual(-1, MeteorContract.MedianMs(null));
            Assert.AreEqual(900, MeteorContract.MedianMs(new List<int> { 1200, 600, 900, 700, 1100 }));
            Assert.AreEqual(850, MeteorContract.MedianMs(new List<int> { 600, 700, 800, 900, 1000, 1100 }));
            Assert.AreEqual(500, MeteorContract.MedianMs(new List<int> { 400, 500, 600 }, 3));
        }

        [Test]
        public void Tally_CountsBandsDecoysTimesAndRareWords()
        {
            var t = new MeteorTally();
            MeteorSpec W(string w, int band, bool shower = false) => new MeteorSpec { Word = w, IsWord = true, Band = band, Shower = shower };
            MeteorSpec D(DecoyKind k) => new MeteorSpec { Word = "x", IsWord = false, Decoy = k };
            for (int i = 0; i < 6; i++) t.AddWord(W("com" + i, 1), true, 600 + 20 * i);
            for (int i = 0; i < 6; i++) t.AddWord(W("rara" + i, 6), i < 4, i < 4 ? 1000 + 30 * i : -1);
            t.AddWord(W("rara0", 6), true, 900);      // repetida: la colección no la duplica
            t.AddWord(W("lluvia", 1, shower: true), true, 300); // la lluvia no entra en las medidas
            t.AddDecoy(D(DecoyKind.Transposed), true);
            t.AddDecoy(D(DecoyKind.Transposed), false);
            t.AddDecoy(D(DecoyKind.Obvious), false);
            Assert.AreEqual(13, t.WordsSeen);
            Assert.AreEqual(11, t.WordsHit);
            Assert.AreEqual(6, t.BandSeen[0]);
            Assert.AreEqual(7, t.BandSeen[5]);
            Assert.AreEqual(5, t.BandHits[5]);
            Assert.AreEqual(3, t.DecoysSeen);
            Assert.AreEqual(2, t.DecoysPassed);
            Assert.AreEqual(1, t.DecoyTapped[(int)DecoyKind.Transposed]);
            Assert.AreEqual(2, t.DecoySeen[(int)DecoyKind.Transposed]);
            Assert.AreEqual(1, t.DecoysTappedTotal);
            Assert.AreEqual(650, t.CommonMedianMs);        // 600, 620, ... 700: la mediana de 6 valores es (640 + 660) / 2
            Assert.AreEqual(1030, t.RareMedianMs);         // 900, 1000, 1030, 1060, 1090
            Assert.AreEqual("rara0,rara1,rara2,rara3", t.RareWords);
            var bands = t.Bands;
            Assert.AreEqual(-1, bands[1]);                  // banda 2 sin palabras
            Assert.AreEqual(67, bands[0]);                  // 100% de la banda 1 menos 33% de falsas alarmas (1 de 3)
        }

        [Test]
        public void Lexicon_FromJson_PicksByBandAndRelaxes()
        {
            const string json = "{\"version\":1,\"idioma\":\"es\",\"fuente\":\"prueba\",\"palabras\":[" +
                "{\"p\":\"luna\",\"b\":1},{\"p\":\"mesa\",\"b\":1},{\"p\":\"umbral\",\"b\":4},{\"p\":\"efímero\",\"b\":6}]," +
                "\"inventadas\":[{\"p\":\"bolpa\",\"t\":\"obvia\",\"de\":\"\",\"b\":0},{\"p\":\"tormenat\",\"t\":\"traspuesta\",\"de\":\"tormenta\",\"b\":3}]}";
            var lex = MeteorLexicon.FromJson(json);
            Assert.AreEqual(4, lex.WordCount);
            Assert.AreEqual(2, lex.DecoyCount);
            var rng = new Random(1);
            Assert.IsTrue(lex.PickWord(4, 4, 4, 12, rng, out var w, out int b));
            Assert.AreEqual("umbral", w);
            Assert.AreEqual(4, b);
            // sin palabra de esa banda y ese largo: relaja y trae la más cercana
            Assert.IsTrue(lex.PickWord(5, 5, 4, 12, rng, out w, out b));
            Assert.That(b, Is.InRange(4, 6));
            // no repite mientras quedan otras
            var seen = new HashSet<string>();
            for (int i = 0; i < 2; i++)
            {
                lex.PickWord(1, 1, 4, 12, rng, out w, out b);
                seen.Add(w);
            }
            Assert.AreEqual(2, seen.Count);
            // un señuelo de un tipo que no hay: cae a otro tipo
            Assert.IsTrue(lex.PickDecoy(new[] { DecoyKind.OneLetter }, 4, 12, rng, out var d, out var kind, out int db));
            Assert.AreNotEqual(DecoyKind.OneLetter, kind);
            Assert.IsNotEmpty(d);
            Assert.Throws<FormatException>(() => MeteorLexicon.FromJson("{\"version\":1,\"palabras\":[]}"));
        }

        [Test]
        public void Lexicon_RealFile_HasWordsInEveryBandAndDecoysOfEveryKind()
        {
            var lex = MeteorLexicon.Load();
            Assert.GreaterOrEqual(lex.WordCount, 1000, "falta o está incompleto Resources/Lexico/meteoros_es.json");
            Assert.GreaterOrEqual(lex.DecoyCount, 1200);
            var rng = new Random(2);
            for (int band = 1; band <= 6; band++)
            {
                Assert.IsTrue(lex.PickWord(band, band, 4, 12, rng, out var w, out int b));
                Assert.AreEqual(band, b, $"banda {band}");
            }
            foreach (DecoyKind k in Enum.GetValues(typeof(DecoyKind)))
            {
                Assert.IsTrue(lex.PickDecoy(new[] { k }, 4, 12, rng, out var d, out var got, out int _));
                Assert.AreEqual(k, got);
            }
        }

        [Test]
        public void Lexicon_Fallback_Works()
        {
            var lex = MeteorLexicon.Fallback();
            Assert.Greater(lex.WordCount, 20);
            Assert.IsTrue(lex.PickWord(1, 1, 4, 8, new Random(1), out var w, out int b));
            Assert.AreEqual(1, b);
            Assert.IsNotEmpty(w);
        }

        [Test]
        public void RockSize_PlaqueIsAboutAThirdOfRockHeight()
        {
            var (w, h) = MeteorContract.RockSize(300f, 117f);
            Assert.AreEqual(117f / h, 1f / 3.2f, 0.001f);
            Assert.Greater(w, 300f * 1.4f);
        }

        [Test]
        public void FitWordDp_KeepsBaseWhenItFitsAndShrinksOnlyWhenNeeded()
        {
            // palabra corta: cabe a 26 dp
            Assert.AreEqual(26, MeteorContract.FitWordDp(26, 300f, 1000f));
            // palabra de 12 letras que no cabe a 26: baja, pero no de 22
            int dp = MeteorContract.FitWordDp(26, 1000f, 1500f);
            Assert.Less(dp, 26);
            Assert.GreaterOrEqual(dp, MeteorContract.MinWordDp);
            var fit = MeteorContract.RockSize(1000f * dp / 26f + MeteorContract.PlaquePadU, 0f).w;
            Assert.LessOrEqual(fit, 1500f);
            // no cabe ni a 22: se queda en 22 (nunca menos)
            Assert.AreEqual(MeteorContract.MinWordDp, MeteorContract.FitWordDp(26, 3000f, 1000f));
            // mayores: base 30
            Assert.AreEqual(30, MeteorContract.FitWordDp(30, 300f, 1000f));
        }

        [Test]
        public void Overlaps_DetectsBoxesAndHonorsGap()
        {
            Assert.IsTrue(MeteorContract.Overlaps(0, 0, 100, 150, 120, 100, 100, 150, 0));
            Assert.IsFalse(MeteorContract.Overlaps(0, 0, 100, 150, 250, 0, 100, 150, 0));      // separados a lo ancho
            Assert.IsFalse(MeteorContract.Overlaps(0, 0, 100, 150, 0, 400, 100, 150, 0));      // separados a lo alto
            Assert.IsTrue(MeteorContract.Overlaps(0, 0, 100, 150, 210, 0, 100, 150, 20));      // el aire de sobra los junta
        }
    }
}
