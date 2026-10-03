using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace NeuroVida.Games.Stroop.Tests
{
    /// <summary>Reglas puras de «Tinta o Palabra» (modelo «Dos orillas», docs/diseno-tinta-o-palabra.md): paleta, niveles, generación y medidas.</summary>
    public class StroopContractTests
    {
        private static Color32 Hex(int rgb) => new Color32((byte)(rgb >> 16), (byte)((rgb >> 8) & 255), (byte)(rgb & 255), 255);

        // ------------------------------------------------------------------ paleta

        [Test]
        public void Palette_IsTheFourInksForEveryone_WithoutGreenOrPurple()
        {
            CollectionAssert.AreEqual(new[] { "ROJO", "AZUL", "AMARILLO", "BLANCO" }, StroopContract.Names);
            Assert.AreEqual(4, StroopContract.Colors.Length);
            Assert.AreEqual(4, StroopContract.LowerNames.Length);
            CollectionAssert.DoesNotContain(StroopContract.Names, "VERDE");
            CollectionAssert.DoesNotContain(StroopContract.Names, "MORADO");
        }

        [Test]
        public void Palette_IsExactlyTheOneVerifiedForColorBlindness()
        {
            // python tools/paleta_daltonismo.py C93C3C 4A86E8 F2CC1D F4F4F4 --fondo 141B36
            Assert.AreEqual(Hex(0xC93C3C), (Color32)StroopContract.Colors[0]);
            Assert.AreEqual(Hex(0x4A86E8), (Color32)StroopContract.Colors[1]);
            Assert.AreEqual(Hex(0xF2CC1D), (Color32)StroopContract.Colors[2]);
            Assert.AreEqual(Hex(0xF4F4F4), (Color32)StroopContract.Colors[3]);
            Assert.AreEqual(Hex(0x141B36), (Color32)StroopContract.CardFill);
            Assert.AreEqual(Hex(0x7C5CE0), (Color32)StroopContract.InkShore);
            Assert.AreEqual(Hex(0x159A8C), (Color32)StroopContract.WordShore);
        }

        [Test]
        public void Palette_EveryInkStandsOutFromTheCard()
        {
            // contraste de luminancia sobre #141B36 (la regla de la ficha: el rojo, el más bajo, 3,4:1 en palabra grande y con brillo)
            for (int i = 0; i < 4; i++) Assert.GreaterOrEqual(Contrast(StroopContract.Colors[i], StroopContract.CardFill), 3.3f, StroopContract.Names[i]);
        }

        private static float Lum(Color c)
        {
            float f(float v) => v <= 0.03928f ? v / 12.92f : Mathf.Pow((v + 0.055f) / 1.055f, 2.4f);
            return 0.2126f * f(c.r) + 0.7152f * f(c.g) + 0.0722f * f(c.b);
        }

        private static float Contrast(Color a, Color b)
        {
            float la = Lum(a), lb = Lum(b);
            return (Mathf.Max(la, lb) + 0.05f) / (Mathf.Min(la, lb) + 0.05f);
        }

        // ------------------------------------------------------------------ niveles

        [Test]
        public void Levels_FollowTheTableOfTheFiche()
        {
            // nivel: reglas · % que chocan · llegada
            AssertLevel(1, false, 0.60f, 360);
            AssertLevel(2, false, 0.75f, 320);
            AssertLevel(3, true, 0.75f, 300);
            AssertLevel(4, true, 0.80f, 280);
            AssertLevel(5, true, 0.85f, 240);
            Assert.IsTrue(StroopContract.Spec(3).Blocks);
            Assert.IsFalse(StroopContract.Spec(4).Blocks);
            Assert.AreEqual(0.40f, StroopContract.Spec(4).SwitchChance, 1e-5f);
            Assert.AreEqual(0.50f, StroopContract.Spec(5).SwitchChance, 1e-5f);
            Assert.AreEqual(5, StroopContract.MaxLevel);
        }

        private static void AssertLevel(int level, bool both, float clash, int arrivalMs)
        {
            var s = StroopContract.Spec(level);
            Assert.AreEqual(both, s.BothRules, "reglas nivel " + level);
            Assert.AreEqual(clash, s.ClashShare, 1e-5f, "choque nivel " + level);
            Assert.AreEqual(arrivalMs, s.ArrivalMs, "llegada nivel " + level);
        }

        [Test]
        public void Levels_OutOfRangeAreClamped()
        {
            Assert.AreEqual(StroopContract.Spec(1).ArrivalMs, StroopContract.Spec(0).ArrivalMs);
            Assert.AreEqual(StroopContract.Spec(5).ArrivalMs, StroopContract.Spec(9).ArrivalMs);
        }

        [Test]
        public void Arrival_GetsFasterWithTheLevel_AndSeniorsGetThirtyPercentMore()
        {
            float previous = float.MaxValue;
            for (int level = 1; level <= 5; level++)
            {
                float adult = StroopContract.ArrivalSeconds(level, false);
                Assert.Less(adult, previous);
                previous = adult;
                Assert.AreEqual(adult * 1.3f, StroopContract.ArrivalSeconds(level, true), 1e-5f);
            }
            Assert.AreEqual(0.36f, StroopContract.ArrivalSeconds(1, false), 1e-5f);
            Assert.AreEqual(0.24f, StroopContract.ArrivalSeconds(5, false), 1e-5f);
        }

        [Test]
        public void Modes_PrecisionIsSixteenWords_RetoIsSixtySeconds()
        {
            Assert.AreEqual(16, StroopContract.TotalTrials);
            Assert.AreEqual(60, StroopContract.EndlessSeconds);
        }

        // ------------------------------------------------------------------ generación

        private static List<StroopTrial> Generate(int level, int count, int seed)
        {
            var seq = new StroopContract.Sequencer(new System.Random(seed));
            var list = new List<StroopTrial>();
            for (int i = 0; i < count; i++) list.Add(seq.Next(level));
            return list;
        }

        [Test]
        public void Levels1And2_AreAlwaysTheInkShore()
        {
            foreach (int level in new[] { 1, 2 })
            {
                foreach (var t in Generate(level, 300, 4 + level))
                {
                    Assert.AreEqual(StroopRule.Ink, t.Rule);
                    Assert.IsFalse(t.Switched);
                }
            }
        }

        [Test]
        public void EveryGame_OpensWithThreeInkWords_AtAnyLevel()
        {
            for (int level = 1; level <= 5; level++)
                for (int seed = 0; seed < 40; seed++)
                {
                    var trials = Generate(level, StroopContract.OpeningInkTrials, seed);
                    foreach (var t in trials) Assert.AreEqual(StroopRule.Ink, t.Rule, "nivel " + level + " semilla " + seed);
                }
        }

        [Test]
        public void Level3_ChangesTheRuleInBlocksOfFourToSix()
        {
            for (int seed = 0; seed < 30; seed++)
            {
                var trials = Generate(3, 80, seed);
                // tramos: largo de cada racha de la misma regla (menos el primero, que es la apertura de TINTA y la primera racha de PALABRA, que arranca al 4.º)
                var runs = new List<int>();
                int run = 1;
                for (int i = 1; i < trials.Count; i++)
                {
                    if (trials[i].Rule == trials[i - 1].Rule) run++;
                    else { runs.Add(run); run = 1; }
                }
                Assert.GreaterOrEqual(runs.Count, 4, "debe haber varios cambios");
                Assert.AreEqual(StroopContract.OpeningInkTrials, runs[0], "la apertura son 3 de TINTA");
                for (int k = 1; k < runs.Count; k++) { Assert.GreaterOrEqual(runs[k], 4); Assert.LessOrEqual(runs[k], 6); }
                // los cambios están marcados
                for (int i = 1; i < trials.Count; i++) Assert.AreEqual(trials[i].Rule != trials[i - 1].Rule, trials[i].Switched);
            }
        }

        [Test]
        public void Levels4And5_SwitchAboutFortyAndFiftyPercent()
        {
            AssertSwitchRate(4, 0.40f);
            AssertSwitchRate(5, 0.50f);
        }

        private static void AssertSwitchRate(int level, float expected)
        {
            int switches = 0, eligible = 0;
            for (int seed = 0; seed < 50; seed++)
            {
                var trials = Generate(level, 100, 100 + seed);
                for (int i = StroopContract.OpeningInkTrials; i < trials.Count; i++) { eligible++; if (trials[i].Switched) switches++; }
            }
            Assert.AreEqual(expected, switches / (float)eligible, 0.03f, "nivel " + level);
        }

        [Test]
        public void ClashShare_MatchesTheLevel()
        {
            for (int level = 1; level <= 5; level++)
            {
                int clash = 0, n = 0;
                for (int seed = 0; seed < 30; seed++)
                    foreach (var t in Generate(level, 100, 500 + seed)) { n++; if (t.Clash) clash++; }
                Assert.AreEqual(StroopContract.Spec(level).ClashShare, clash / (float)n, 0.03f, "nivel " + level);
            }
        }

        [Test]
        public void TheRestOfTheWordsCoincideWithTheirInk_SoTheInterferenceCanBeMeasured()
        {
            var trials = Generate(2, 400, 77);
            Assert.Greater(trials.FindAll(t => !t.Clash).Count, 60);
            foreach (var t in trials) Assert.AreEqual(!t.Clash, t.WordIndex == t.InkIndex);
        }

        [Test]
        public void Indexes_StayInsideTheFourInks()
        {
            foreach (var t in Generate(5, 500, 9))
            {
                Assert.That(t.WordIndex, Is.InRange(0, 3));
                Assert.That(t.InkIndex, Is.InRange(0, 3));
            }
        }

        [Test]
        public void CorrectIndex_FollowsTheShoreTheWordCameFrom()
        {
            Assert.AreEqual(1, new StroopTrial(0, 1, StroopRule.Ink).CorrectIndex);
            Assert.AreEqual(0, new StroopTrial(0, 1, StroopRule.Word).CorrectIndex);
        }

        [Test]
        public void SameSeed_GivesTheSameSequence()
        {
            var a = Generate(4, 60, 123);
            var b = Generate(4, 60, 123);
            for (int i = 0; i < a.Count; i++)
            {
                Assert.AreEqual(a[i].WordIndex, b[i].WordIndex);
                Assert.AreEqual(a[i].InkIndex, b[i].InkIndex);
                Assert.AreEqual(a[i].Rule, b[i].Rule);
            }
        }

        [Test]
        public void ClimbingMidGame_KeepsAValidSequence()
        {
            // el DDA sube de nivel a mitad de partida: del 1 al 3 el generador sigue sin saltos raros
            var seq = new StroopContract.Sequencer(new System.Random(3));
            for (int i = 0; i < 10; i++) Assert.AreEqual(StroopRule.Ink, seq.Next(1).Rule);
            bool sawWord = false;
            for (int i = 0; i < 20; i++) sawWord |= seq.Next(3).Rule == StroopRule.Word;
            Assert.IsTrue(sawWord);
            Assert.AreEqual(30, seq.Count);
        }

        // ------------------------------------------------------------------ medidas

        private static StroopResponse Ok(float ms, bool clash = false, bool switched = false) => new StroopResponse(true, ms, clash, switched);

        [Test]
        public void Interference_IsTheAverageOfClashingMinusCoinciding()
        {
            var r = new List<StroopResponse>
            {
                Ok(900, true), Ok(1000, true), Ok(1100, true), Ok(1000, true),
                Ok(600, false), Ok(700, false), Ok(800, false),
            };
            Assert.AreEqual(300, StroopContract.InterferenceMs(r)); // 1000 − 700
        }

        [Test]
        public void Interference_NeedsThreeCorrectOfEachKind_AndMinusOneOtherwise()
        {
            var few = new List<StroopResponse> { Ok(900, true), Ok(1000, true), Ok(1100, true), Ok(600), Ok(700) };
            Assert.AreEqual(-1, StroopContract.InterferenceMs(few));
            Assert.AreEqual(-1, StroopContract.InterferenceMs(new List<StroopResponse>()));
            Assert.AreEqual(-1, StroopContract.InterferenceMs(null));
        }

        [Test]
        public void Interference_IgnoresTheWrongAnswers()
        {
            var r = new List<StroopResponse>
            {
                Ok(1000, true), Ok(1000, true), Ok(1000, true), Ok(700), Ok(700), Ok(700),
                new StroopResponse(false, 5000f, true, false), new StroopResponse(false, 10f, false, false),
            };
            Assert.AreEqual(300, StroopContract.InterferenceMs(r));
        }

        [Test]
        public void Interference_IsNeverNegative()
        {
            var r = new List<StroopResponse> { Ok(500, true), Ok(500, true), Ok(500, true), Ok(800), Ok(800), Ok(800) };
            Assert.AreEqual(0, StroopContract.InterferenceMs(r)); // más rápido con choque: «casi nada», nunca -1 (eso quiere decir «sin datos»)
        }

        [Test]
        public void SwitchCost_IsTheAverageAfterASwitchMinusRepeating()
        {
            var r = new List<StroopResponse>
            {
                Ok(1100, switched: true), Ok(1300, switched: true), Ok(1200, switched: true),
                Ok(800), Ok(900), Ok(1000), Ok(900),
            };
            Assert.AreEqual(300, StroopContract.SwitchCostMs(r)); // 1200 − 900
        }

        [Test]
        public void SwitchCost_IsMinusOneWithoutSwitches_LikeLevels1And2()
        {
            var seq = new StroopContract.Sequencer(new System.Random(1));
            var r = new List<StroopResponse>();
            for (int i = 0; i < 40; i++) { var t = seq.Next(2); r.Add(Ok(800, t.Clash, t.Switched)); }
            Assert.AreEqual(-1, StroopContract.SwitchCostMs(r));
            Assert.AreEqual(-1, StroopContract.SwitchCostMs(null));
        }

        [Test]
        public void SwitchCost_NeedsThreeOfEachKind()
        {
            var r = new List<StroopResponse> { Ok(1100, switched: true), Ok(1300, switched: true), Ok(800), Ok(900), Ok(1000) };
            Assert.AreEqual(-1, StroopContract.SwitchCostMs(r));
        }

        // ------------------------------------------------------------------ puntaje y textos

        [Test]
        public void Score_IsPercentageOfTrialsClamped()
        {
            Assert.AreEqual(75, StroopContract.Score(12, 16));
            Assert.AreEqual(100, StroopContract.Score(16, 16));
            Assert.AreEqual(0, StroopContract.Score(0, 0));
        }

        [Test]
        public void EndlessScore_CombinesAccuracyAndPace()
        {
            Assert.AreEqual(100, StroopContract.EndlessScore(24, 24));
            Assert.AreEqual(100, StroopContract.EndlessScore(40, 40));
            Assert.AreEqual(25, StroopContract.EndlessScore(6, 12)); // 50% de precisión x 50% de ritmo
            Assert.AreEqual(0, StroopContract.EndlessScore(0, 0));
            Assert.Less(StroopContract.EndlessScore(5, 10), StroopContract.EndlessScore(20, 24));
        }

        [Test]
        public void Explain_SaysWhatTheInkWasOrWhatTheWordSaid_WithoutBlame()
        {
            Assert.AreEqual("La tinta era azul", StroopContract.Explain(new StroopTrial(0, 1, StroopRule.Ink)));
            Assert.AreEqual("La palabra decía rojo", StroopContract.Explain(new StroopTrial(0, 1, StroopRule.Word)));
            Assert.AreEqual("La palabra decía amarillo", StroopContract.Explain(new StroopTrial(2, 3, StroopRule.Word)));
            Assert.AreEqual("Responde: TINTA", StroopContract.Ribbon(StroopRule.Ink));
            Assert.AreEqual("Responde: PALABRA", StroopContract.Ribbon(StroopRule.Word));
        }

        // ------------------------------------------------------------------ tutorial

        [Test]
        public void TheGuidedRound_IsFourClashingWords_InkThenWordThenTheSameWithoutHelp()
        {
            Assert.AreEqual(4, StroopContract.GuidedTrials.Length);
            Assert.AreEqual(StroopRule.Ink, StroopContract.GuidedTrials[0].Rule);
            Assert.AreEqual(StroopRule.Word, StroopContract.GuidedTrials[1].Rule);
            Assert.AreEqual(StroopRule.Ink, StroopContract.GuidedTrials[2].Rule);
            Assert.AreEqual(StroopRule.Word, StroopContract.GuidedTrials[3].Rule);
            foreach (var t in StroopContract.GuidedTrials)
            {
                Assert.IsTrue(t.Clash, "todas chocan: así se ve por qué manda la orilla");
                Assert.AreNotEqual(t.InkIndex, t.WordIndex);
                Assert.AreEqual(t.Rule == StroopRule.Ink ? t.InkIndex : t.WordIndex, t.CorrectIndex);
            }
            Assert.AreEqual(2, StroopContract.GuidedHinted);
            Assert.GreaterOrEqual(StroopContract.GuidedArrivalSeconds, StroopContract.ArrivalSeconds(1, false), "llega más despacio que el nivel 1");
            Assert.GreaterOrEqual(StroopContract.GuidedErrorPauseSeconds, 1.5f, "tras un error se alcanza a leer");
        }
    }
}
