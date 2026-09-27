using System;
using System.Collections.Generic;
using NUnit.Framework;

namespace NeuroVida.Games.Aterrizaje.Tests
{
    public class LandingContractTests
    {
        [Test]
        public void Trials_StayOnTheRulerAtEveryLevel()
        {
            var rng = new Random(5);
            for (int level = 1; level <= LandingContract.MaxLevel; level++)
            {
                for (int i = 0; i < 300; i++)
                {
                    var t = LandingContract.NextTrial(level, rng);
                    Assert.Less(t.Min, t.Max);
                    Assert.That(t.TargetFraction, Is.InRange(0.03f, 0.97f), $"nivel {level}: {t.Label}");
                    Assert.IsFalse(string.IsNullOrEmpty(t.Label));
                    Assert.IsFalse(string.IsNullOrEmpty(t.MinLabel));
                    Assert.IsFalse(string.IsNullOrEmpty(t.MaxLabel));
                    if (t.MidTick) Assert.Greater(Math.Abs(t.TargetFraction - 0.5f), 1e-4f); // con marca del medio, el medio sería regalado
                }
            }
        }

        [Test]
        public void Ladder_ScalesAndChangesFormat()
        {
            var rng = new Random(2);
            var t1 = LandingContract.NextTrial(1, rng);
            Assert.AreEqual(0f, t1.Min);
            Assert.AreEqual(10f, t1.Max);
            Assert.IsTrue(t1.MidTick);
            Assert.AreEqual(1000f, LandingContract.NextTrial(6, rng).Max);
            Assert.IsFalse(LandingContract.NextTrial(6, rng).MidTick);
            StringAssert.Contains("/", LandingContract.NextTrial(7, rng).Label);
            StringAssert.Contains("+", LandingContract.NextTrial(10, rng).Label);
            var t11 = LandingContract.NextTrial(11, rng);
            Assert.AreEqual(2f, t11.Max);
            var t12 = LandingContract.NextTrial(12, rng);
            Assert.AreEqual(-50f, t12.Min);
            var t9 = LandingContract.NextTrial(9, rng);
            Assert.Greater(t9.Min, 0f);
            Assert.AreEqual(500f, t9.Max - t9.Min);
        }

        [Test]
        public void Labels_MatchTheTarget()
        {
            var rng = new Random(9);
            for (int i = 0; i < 200; i++)
            {
                var f = LandingContract.NextTrial(7, rng);
                var parts = f.Label.Split('/');
                Assert.AreEqual(float.Parse(parts[0]) / float.Parse(parts[1]), f.Target, 1e-5f);
                var s = LandingContract.NextTrial(10, rng);
                var add = s.Label.Split('+');
                Assert.AreEqual(float.Parse(add[0].Trim()) + float.Parse(add[1].Trim()), s.Target, 1e-3f);
                var d = LandingContract.NextTrial(8, rng);
                if (d.Label.EndsWith("%")) Assert.AreEqual(float.Parse(d.Label.TrimEnd('%')), d.Target, 1e-3f);
                else
                {
                    StringAssert.StartsWith("0,", d.Label); // coma decimal
                    Assert.AreEqual(float.Parse(d.Label.Replace(',', '.'), System.Globalization.CultureInfo.InvariantCulture), d.Target, 1e-4f);
                }
            }
        }

        [Test]
        public void Error_HitsAndBullseyes()
        {
            var t = new LandingTrial { Min = 0, Max = 100, Target = 40 };
            Assert.AreEqual(0.03f, LandingContract.Error(t, 43f), 1e-5f);
            Assert.IsTrue(LandingContract.IsHit(LandingContract.Error(t, 44f)));
            Assert.IsFalse(LandingContract.IsHit(LandingContract.Error(t, 46f)));
            Assert.IsTrue(LandingContract.IsBullseye(LandingContract.Error(t, 41f)));
            Assert.AreEqual(25f, LandingContract.ValueAt(t, 0.25f), 1e-5f);
            Assert.AreEqual(100f, LandingContract.ValueAt(t, 3f), 1e-5f);
            var neg = new LandingTrial { Min = -50, Max = 50, Target = -20 };
            Assert.AreEqual(0.1f, LandingContract.Error(neg, -10f), 1e-5f);
            Assert.AreEqual("7", LandingContract.DistanceLabel(t, 47f));
            var frac = new LandingTrial { Min = 0, Max = 1, Target = 0.75f };
            Assert.AreEqual("0,10", LandingContract.DistanceLabel(frac, 0.65f));
        }

        [Test]
        public void Points_RewardCloseness()
        {
            Assert.Greater(LandingContract.Points(0.005f, 1, 1), LandingContract.Points(0.04f, 1, 1));
            Assert.Greater(LandingContract.Points(0.04f, 1, 1), LandingContract.Points(0.09f, 1, 1));
            Assert.AreEqual(0, LandingContract.Points(0.2f, 5, 3));
            Assert.Greater(LandingContract.Points(0.02f, 8, 1), LandingContract.Points(0.02f, 1, 1));
            Assert.Greater(LandingContract.Points(0.02f, 1, 5), LandingContract.Points(0.02f, 1, 1));
        }

        [Test]
        public void MeasuresAndScore()
        {
            Assert.AreEqual(-1f, LandingContract.MeanErrorPct(new List<float>()));
            Assert.AreEqual(4f, LandingContract.MeanErrorPct(new List<float> { 0.02f, 0.06f }), 1e-4f);
            Assert.AreEqual(100, LandingContract.Score(0f, LandingContract.MaxLevel));
            Assert.AreEqual(0, LandingContract.Score(20f, 1));
            Assert.Greater(LandingContract.Score(3f, 5), LandingContract.Score(8f, 5));
            Assert.Less(LandingContract.DescentSeconds(12, false), LandingContract.DescentSeconds(1, false));
            Assert.Greater(LandingContract.DescentSeconds(12, true), LandingContract.DescentSeconds(1, false));
            for (int l = 2; l <= LandingContract.MaxLevel; l++) Assert.IsNotEmpty(LandingContract.LevelNews(l));
        }
    }
}
