using System;
using NUnit.Framework;

namespace NeuroVida.Games.Piloto.Tests
{
    public class PilotContractTests
    {
        [Test]
        public void Difficulty_GetsHarderWithLevel()
        {
            Assert.Greater(PilotContract.ScrollSpeed(9, false), PilotContract.ScrollSpeed(1, false));
            Assert.Less(PilotContract.LaneHalfWidth(9), PilotContract.LaneHalfWidth(1));
            Assert.Greater(PilotContract.Curviness(9), PilotContract.Curviness(1));
            Assert.Less(PilotContract.SignalGap(9, false), PilotContract.SignalGap(1, false));
            Assert.Less(PilotContract.ExposureMs(9, false), PilotContract.ExposureMs(1, false));
            Assert.AreEqual(1600, PilotContract.ExposureMs(1, false));
            Assert.AreEqual(650, PilotContract.ExposureMs(9, false));
        }

        [Test]
        public void Precision_IsGentlerThanReto()
        {
            Assert.Less(PilotContract.ScrollSpeed(5, true), PilotContract.ScrollSpeed(5, false));
            Assert.Greater(PilotContract.ExposureMs(5, true), PilotContract.ExposureMs(5, false));
            Assert.Greater(PilotContract.SignalGap(5, true), PilotContract.SignalGap(5, false));
        }

        [Test]
        public void Lane_NeverLeavesTheScreen()
        {
            for (int level = 1; level <= PilotContract.MaxLevel; level++)
            {
                float half = PilotContract.LaneHalfWidth(level), amp = PilotContract.Curviness(level);
                for (float d = 0f; d < 60f; d += 0.05f)
                {
                    float c = PilotContract.CenterAt(d, amp, half);
                    Assert.GreaterOrEqual(c - half, 0.03f);
                    Assert.LessOrEqual(c + half, 0.97f);
                }
            }
        }

        [Test]
        public void Lane_IsContinuous()
        {
            float amp = PilotContract.Curviness(9), half = PilotContract.LaneHalfWidth(9);
            float step = 46f / 1900f; // una baliza
            for (float d = 0f; d < 30f; d += step)
                Assert.Less(Math.Abs(PilotContract.CenterAt(d + step, amp, half) - PilotContract.CenterAt(d, amp, half)), 0.05f);
        }

        [Test]
        public void InLane_UsesHalfWidth()
        {
            Assert.IsTrue(PilotContract.InLane(0.5f, 0.5f, 0.2f));
            Assert.IsTrue(PilotContract.InLane(0.69f, 0.5f, 0.2f));
            Assert.IsFalse(PilotContract.InLane(0.71f, 0.5f, 0.2f));
        }

        [Test]
        public void Signals_TargetsMatchTheMissionAndDistractorsDoNot()
        {
            var rng = new Random(7);
            int targets = 0;
            for (int i = 0; i < 2000; i++)
            {
                var s = PilotContract.NextSignal(6, 2, 1, rng);
                bool isMission = s.Shape == 2 && s.Variant == 1;
                Assert.AreEqual(s.IsTarget, isMission);
                Assert.That(s.Variant, Is.InRange(0, PilotContract.Variants - 1));
                Assert.That(s.X, Is.InRange(0f, 1f));
                Assert.That(s.Y, Is.InRange(0f, 1f));
                if (s.IsTarget) targets++;
            }
            Assert.That(targets / 2000f, Is.InRange(0.35f, 0.45f));
        }

        [Test]
        public void Signals_LookalikesAndPeripheryOnlyAtHigherLevels()
        {
            var rng = new Random(3);
            for (int i = 0; i < 500; i++)
            {
                var s = PilotContract.NextSignal(1, 0, 0, rng);
                if (!s.IsTarget) Assert.AreNotEqual(0, s.Shape); // nivel 1: los distractores tienen otra forma
                Assert.IsFalse(s.Peripheral);
            }
            int lookalikes = 0, peripheral = 0;
            for (int i = 0; i < 2000; i++)
            {
                var s = PilotContract.NextSignal(8, 0, 0, rng);
                if (!s.IsTarget && s.Shape == 0) lookalikes++;
                if (s.Peripheral)
                {
                    peripheral++;
                    Assert.IsTrue(s.X < 0.2f || s.X > 0.8f);
                }
            }
            Assert.Greater(lookalikes, 300);
            Assert.Greater(peripheral, 600);
        }

        [Test]
        public void SignalAccuracy_IsHitsMinusFalseAlarms()
        {
            Assert.AreEqual(0.8f, PilotContract.SignalAccuracy(8, 10, 0, 10).Value, 1e-4f);
            Assert.AreEqual(0.6f, PilotContract.SignalAccuracy(8, 10, 2, 10).Value, 1e-4f);
            Assert.AreEqual(0f, PilotContract.SignalAccuracy(1, 10, 9, 10).Value, 1e-4f);
            Assert.IsNull(PilotContract.SignalAccuracy(3, 3, 0, 0));
            Assert.IsNull(PilotContract.SignalAccuracy(0, 0, 0, 4));
        }

        [Test]
        public void MultitaskCost_IsTheRelativeDrop()
        {
            Assert.AreEqual(25, PilotContract.MultitaskCost(0.8f, 0.6f));
            Assert.AreEqual(0, PilotContract.MultitaskCost(0.6f, 0.8f)); // mejor en doble tarea: costo 0
            Assert.AreEqual(-1, PilotContract.MultitaskCost(null, 0.5f));
            Assert.AreEqual(-1, PilotContract.MultitaskCost(0.1f, 0.05f)); // tarea sola demasiado baja: no confiable
        }

        [Test]
        public void Score_CombinesSignalsAndLane()
        {
            Assert.AreEqual(100, PilotContract.Score(1f, 1f));
            Assert.AreEqual(0, PilotContract.Score(0f, 0f));
            Assert.AreEqual(55, PilotContract.Score(1f, 0f));
            Assert.AreEqual(45, PilotContract.Score(0f, 1f));
        }

        [Test]
        public void Points_GrowWithStreakAndDoubleInBoost()
        {
            Assert.AreEqual(100, PilotContract.PointsForCatch(1, false));
            Assert.AreEqual(140, PilotContract.PointsForCatch(3, false));
            Assert.AreEqual(300, PilotContract.PointsForCatch(50, false));
            Assert.AreEqual(280, PilotContract.PointsForCatch(3, true));
        }
    }
}
