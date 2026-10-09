using System;
using System.Collections.Generic;
using NUnit.Framework;

namespace NeuroVida.Games.Disparate.Tests
{
    public class DisparateContractTests
    {
        /// <summary>Banco falso: cada tipo y respuesta con N frases "t{tipo}{V|D}_{n}" de 3 palabras.</summary>
        private static SentenceBank StubBank(int perKind = 60)
        {
            var items = new List<SentenceItem>();
            for (int t = 1; t <= 6; t++)
                for (int k = 0; k < 2; k++)
                    for (int n = 0; n < perKind; n++)
                        items.Add(new SentenceItem
                        {
                            f = $"a b c{t}{k}{n}", v = k == 1, t = t, c = k == 1 ? "" : (n % 2 == 0 ? "evidente" : "sutil"),
                            n = 3 + t, r = k == 1 ? "" : "corrige", i = $"t{t}{k}_{n}"
                        });
            return new SentenceBank(items);
        }

        [Test]
        public void Practica_TresFrases_VerdadDisparateYVerdad_SinSerDelBanco()
        {
            var specs = DisparateContract.PracticeSpecs(false);
            Assert.AreEqual(3, specs.Length);
            Assert.IsTrue(specs[0].S.v);
            Assert.IsFalse(specs[1].S.v);
            Assert.IsTrue(specs[2].S.v);
            Assert.AreEqual("evidente", specs[1].S.c);
            Assert.IsFalse(string.IsNullOrEmpty(specs[1].S.r), "el disparate muestra su corrección");
            var ids = new HashSet<string>();
            foreach (var s in specs)
            {
                Assert.IsFalse(s.Burst);
                Assert.LessOrEqual(s.S.t, 2, "frases cortas: la práctica no enseña ni negaciones ni comparaciones");
                Assert.LessOrEqual(s.S.n, 5, s.S.f);
                Assert.IsTrue(ids.Add(s.S.i), "ids distintos");
                Assert.IsTrue(s.S.i.StartsWith("practica-"), "no se confunde con el id de una frase del banco");
            }
        }

        [Test]
        public void Practica_LaSenalSoloSeVeEnElReto()
        {
            Assert.AreEqual(0f, DisparateContract.PracticeSpecs(true)[2].SignalSeconds, "Precisión: sin señal");
            Assert.AreEqual(DisparateContract.PracticeSignalSeconds, DisparateContract.PracticeSpecs(false)[2].SignalSeconds);
            Assert.Greater(DisparateContract.PracticeSignalSeconds, 3.6f, "la barra no llega a cero mientras Nubi habla");
            foreach (var s in DisparateContract.PracticeSpecs(false)) Assert.IsFalse(s.S.IsSubtle, s.S.f);
        }

        [Test]
        public void LevelTable_TypeAndSignal()
        {
            Assert.AreEqual(SentenceType.Short, DisparateContract.TypeForLevel(1));
            Assert.AreEqual(SentenceType.Short, DisparateContract.TypeForLevel(2));
            Assert.AreEqual(SentenceType.Complement, DisparateContract.TypeForLevel(3));
            Assert.AreEqual(SentenceType.Negation, DisparateContract.TypeForLevel(6));
            Assert.AreEqual(SentenceType.Pause, DisparateContract.TypeForLevel(7));
            Assert.AreEqual(SentenceType.Quantifier, DisparateContract.TypeForLevel(10));
            Assert.AreEqual(SentenceType.Comparison, DisparateContract.TypeForLevel(12));
            Assert.AreEqual(9f, DisparateContract.SignalSeconds(1, false, false));
            Assert.AreEqual(5f, DisparateContract.SignalSeconds(12, false, false));
            float prev = 100f;
            for (int l = 1; l <= DisparateContract.MaxLevel; l++)
            {
                float s = DisparateContract.SignalSeconds(l, false, false);
                Assert.LessOrEqual(s, prev, $"la señal no sube, nivel {l}");
                prev = s;
            }
        }

        [Test]
        public void Precision_HasNoSignal_AndSeniorsGetMoreTime()
        {
            Assert.AreEqual(0f, DisparateContract.SignalSeconds(5, true, false));
            Assert.AreEqual(0f, DisparateContract.BurstSeconds(true, false));
            Assert.AreEqual(9f * 1.3f, DisparateContract.SignalSeconds(1, false, true), 1e-4f);
            Assert.AreEqual(4f * 1.3f, DisparateContract.BurstSeconds(false, true), 1e-4f);
            Assert.AreEqual(28, DisparateContract.PhraseSizeSp(true));
            Assert.AreEqual(24, DisparateContract.PhraseSizeSp(false));
        }

        [Test]
        public void Sequencer_NeverMoreThanThreeEqualInARow_AndIsBalanced()
        {
            var rng = new Random(7);
            var seq = new DisparateContract.AnswerSequencer();
            int run = 0, best = 0, trues = 0;
            bool last = false;
            const int n = 20000;
            for (int i = 0; i < n; i++)
            {
                bool a = seq.Next(rng);
                seq.Record(a);
                run = i > 0 && a == last ? run + 1 : 1;
                last = a;
                best = Math.Max(best, run);
                if (a) trues++;
            }
            Assert.LessOrEqual(best, DisparateContract.MaxSameAnswerRun);
            Assert.AreEqual(0.5, trues / (double)n, 0.03);
        }

        [Test]
        public void Director_NeverMoreThanThreeEqualInARow_NoRepeats_AndTypeFollowsLevel()
        {
            var dir = new DisparateDirector(StubBank(), new Random(3));
            var seen = new HashSet<string>();
            int run = 0, best = 0;
            bool last = false;
            for (int i = 0; i < 100; i++)
            {
                int level = 1 + (i / 9) % 12;
                var spec = dir.Next(level, false, false);
                Assert.IsNotNull(spec);
                Assert.IsTrue(seen.Add(spec.S.i), "frase repetida " + spec.S.i);
                if (!spec.Burst) Assert.AreEqual((int)DisparateContract.TypeForLevel(level), spec.S.t, $"tipo, nivel {level}");
                run = i > 0 && spec.S.v == last ? run + 1 : 1;
                last = spec.S.v;
                best = Math.Max(best, run);
            }
            Assert.LessOrEqual(best, 3);
        }

        [Test]
        public void Director_AvoidsRecentGames_WhenThereAreOthers()
        {
            var bank = StubBank(40);
            var recent = new List<string>();
            for (int n = 0; n < 30; n++) recent.Add($"t10_{n}");                     // 30 de 40 disparates del tipo 1 ya salieron
            var dir = new DisparateDirector(bank, new Random(5), recent);
            var avoidSet = new HashSet<string>(recent);
            int fromAvoid = 0;
            for (int i = 0; i < 8; i++)
            {
                var spec = dir.Next(1, false, false);
                if (avoidSet.Contains(spec.S.i)) fromAvoid++;
            }
            Assert.AreEqual(0, fromAvoid, "hay otras frases: no debe repetir las de las últimas partidas");
        }

        [Test]
        public void Director_ReusesWhenTheBankIsExhausted()
        {
            var dir = new DisparateDirector(StubBank(3), new Random(1));
            for (int i = 0; i < 30; i++) Assert.IsNotNull(dir.Next(1, false, false));   // solo hay 6 frases del tipo 1: no se cae
        }

        [Test]
        public void Burst_ComesEvery12_Brings5ShortSentences_AndDoesNotCount()
        {
            var dir = new DisparateDirector(StubBank(), new Random(11));
            var bursts = new List<int>();
            int normal = 0, burstRun = 0;
            for (int i = 0; i < 40; i++)
            {
                var spec = dir.Next(8, false, false);
                if (spec.Burst)
                {
                    burstRun++;
                    Assert.AreEqual(1, spec.S.t, "la ráfaga son frases cortas");
                    Assert.LessOrEqual(spec.S.n, 3 + 1, "de 3 palabras");
                    Assert.AreEqual(DisparateContract.BurstSignalSeconds, spec.SignalSeconds);
                }
                else
                {
                    if (burstRun > 0) { bursts.Add(burstRun); burstRun = 0; }
                    normal++;
                    Assert.AreEqual((int)DisparateContract.TypeForLevel(8), spec.S.t);
                }
            }
            Assert.IsTrue(bursts.Count >= 2, "al menos dos ráfagas en 40 frases");
            foreach (int b in bursts) Assert.AreEqual(DisparateContract.BurstSize, b);
            Assert.AreEqual(normal, dir.Spawned, "las frases de ráfaga no cuentan como normales");
        }

        [Test]
        public void BurstDue_OnlyOncePerBatch()
        {
            Assert.IsFalse(DisparateContract.BurstDue(0, 0));
            Assert.IsFalse(DisparateContract.BurstDue(11, 0));
            Assert.IsTrue(DisparateContract.BurstDue(12, 0));
            Assert.IsFalse(DisparateContract.BurstDue(12, 1));
            Assert.IsFalse(DisparateContract.BurstDue(23, 1));
            Assert.IsTrue(DisparateContract.BurstDue(24, 1));
        }

        [Test]
        public void Points_PerfectStreakMultiplies_BurstIsFlat()
        {
            Assert.AreEqual(100, DisparateContract.Points(1, 0, false));
            Assert.AreEqual(210, DisparateContract.Points(12, 0, false));
            Assert.AreEqual(150, DisparateContract.Points(1, DisparateContract.PerfectStreak, false));
            Assert.AreEqual(50, DisparateContract.Points(9, 20, true));
        }

        [Test]
        public void Score_UsesAccuracyAndPeakLevel()
        {
            Assert.AreEqual(0, DisparateContract.Score(0f, 1));
            Assert.AreEqual(100, DisparateContract.Score(1f, 12));
            Assert.AreEqual(70, DisparateContract.Score(1f, 1));
            Assert.AreEqual(35, DisparateContract.Score(0.5f, 1));
        }

        [Test]
        public void Wpm_NeedsTenHits_AndUsesTheMedian()
        {
            var words = new List<int>(); var ms = new List<int>();
            for (int i = 0; i < 9; i++) { words.Add(3); ms.Add(1500); }
            Assert.AreEqual(-1, DisparateContract.WordsPerMinute(words, ms), "con 9 aciertos no se mide");
            words.Add(3); ms.Add(1500);
            Assert.AreEqual(120, DisparateContract.WordsPerMinute(words, ms));            // 3 palabras en 1,5 s = 120 por minuto
            // un acierto muy lento no cambia la mediana
            words[0] = 3; ms[0] = 30000;
            Assert.AreEqual(120, DisparateContract.WordsPerMinute(words, ms));
            Assert.AreEqual(-1, DisparateContract.WordsPerMinute(null, null));
        }

        [Test]
        public void MeanMs_NeedsFourHits()
        {
            Assert.AreEqual(-1, DisparateContract.MeanMs(new List<int> { 900, 1100, 1000 }));
            Assert.AreEqual(1000, DisparateContract.MeanMs(new List<int> { 900, 1100, 1000, 1000 }));
        }

        [Test]
        public void Tally_CountsPerTypeClassStreak_AndIgnoresBurst()
        {
            var tally = new DisparateTally();
            var truth = new SentenceItem { f = "x y z", v = true, t = 1, n = 3, i = "a" };
            var evident = new SentenceItem { f = "x y z", v = false, t = 3, c = "evidente", n = 4, i = "b" };
            var subtle = new SentenceItem { f = "x y z", v = false, t = 3, c = "sutil", n = 4, i = "c" };
            tally.Add(truth, true, 900);
            tally.AddBurst(true);
            tally.Add(evident, true, 1200);
            tally.Add(subtle, false, 2000);
            tally.Add(subtle, true, 1800);
            tally.Add(truth, false, -1);     // se agotó la señal
            Assert.AreEqual(5, tally.Total);
            Assert.AreEqual(3, tally.Correct);
            Assert.AreEqual(2, tally.Seen[0]);
            Assert.AreEqual(1, tally.Hits[0]);
            Assert.AreEqual(3, tally.Seen[2]);
            Assert.AreEqual(1, tally.EvidentSeen); Assert.AreEqual(1, tally.EvidentHits);
            Assert.AreEqual(2, tally.SubtleSeen); Assert.AreEqual(1, tally.SubtleHits);
            Assert.AreEqual(3, tally.BestStreak, "acierto, ráfaga y acierto: 3 seguidos; el error la corta");
            Assert.AreEqual(900, tally.HitMs[0][0]);
            Assert.AreEqual(1, tally.HitMs[0].Count, "la señal agotada no suma tiempo");
        }

        [Test]
        public void Tally_TypeMeansOnlyWithFourHits()
        {
            var tally = new DisparateTally();
            var s = new SentenceItem { f = "x y z", v = true, t = 3, n = 4, i = "a" };
            for (int i = 0; i < 4; i++) tally.Add(s, true, 1000 + i * 100);
            var t1 = new SentenceItem { f = "x y z", v = true, t = 1, n = 3, i = "b" };
            for (int i = 0; i < 3; i++) tally.Add(t1, true, 800);
            var means = tally.TypeMeanMs();
            Assert.AreEqual(1150, means[2]);
            Assert.AreEqual(-1, means[0]);
            Assert.AreEqual(-1, means[5]);
        }

        [Test]
        public void Unclear_NoDuplicates_AsCsv()
        {
            var tally = new DisparateTally();
            tally.ReportUnclear("a1b2"); tally.ReportUnclear("a1b2"); tally.ReportUnclear("c3d4"); tally.ReportUnclear("");
            Assert.AreEqual("a1b2,c3d4", tally.UnclearCsv);
        }

        [Test]
        public void RecentIds_KeepsTheLastThreeGames()
        {
            string s = "";
            s = DisparateContract.PushRecent(s, new[] { "a", "b" });
            s = DisparateContract.PushRecent(s, new[] { "c" });
            s = DisparateContract.PushRecent(s, new[] { "d", "e" });
            s = DisparateContract.PushRecent(s, new[] { "f" });
            Assert.AreEqual("c;d,e;f", s);
            var ids = DisparateContract.RecentIds(s);
            Assert.IsTrue(ids.Contains("d") && ids.Contains("f") && !ids.Contains("a") && !ids.Contains("b"));
            Assert.AreEqual(0, DisparateContract.ParseRecent("").Count);
            Assert.AreEqual(0, DisparateContract.ParseRecent(null).Count);
        }

        [Test]
        public void NoBiasProfile_InTheTally()
        {
            // regla de patentes (US 11,839,472): no se calcula ningún perfil "tiende a verdad / a disparate"
            foreach (var m in typeof(DisparateTally).GetMembers())
            {
                string name = m.Name.ToLowerInvariant();
                Assert.IsFalse(name.Contains("bias") || name.Contains("sesgo") || name.Contains("impuls") || name.Contains("conserv"), m.Name);
            }
        }

        [Test]
        public void Bank_RealFile_HasEnoughOfEveryTypeAndAnswer()
        {
            var bank = SentenceBank.Load();
            Assert.GreaterOrEqual(bank.Count, 1500, "falta o está incompleto Resources/Frases/disparate_es.json");
            for (int t = 1; t <= 6; t++)
            {
                Assert.GreaterOrEqual(bank.CountOf(t, true), 120, $"verdades del tipo {t}");
                Assert.GreaterOrEqual(bank.CountOf(t, false), 120, $"disparates del tipo {t}");
            }
        }

        [Test]
        public void Bank_RealFile_PlaysAFullRetoWithoutRepeats()
        {
            var dir = new DisparateDirector(SentenceBank.Load(), new Random(21));
            var seen = new HashSet<string>();
            for (int i = 0; i < 120; i++)
            {
                var spec = dir.Next(1 + i / 10, false, i % 2 == 0);
                Assert.IsNotNull(spec);
                Assert.IsTrue(seen.Add(spec.S.i), "repetida: " + spec.S.f);
                Assert.IsFalse(string.IsNullOrEmpty(spec.S.f));
                if (!spec.S.v) Assert.IsFalse(string.IsNullOrEmpty(spec.S.r), "un disparate sin corrección: " + spec.S.f);
            }
        }

        [Test]
        public void Bank_Fallback_Works()
        {
            var bank = SentenceBank.Fallback();
            for (int t = 1; t <= 6; t++) { Assert.Greater(bank.CountOf(t, true), 0); Assert.Greater(bank.CountOf(t, false), 0); }
            var dir = new DisparateDirector(bank, new Random(2));
            for (int i = 0; i < 40; i++) Assert.IsNotNull(dir.Next(1 + i % 12, false, false));
        }
    }
}
