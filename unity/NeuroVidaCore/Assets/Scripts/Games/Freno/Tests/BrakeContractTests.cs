using System;
using System.Collections.Generic;
using NUnit.Framework;

namespace NeuroVida.Games.Freno.Tests
{
    public class BrakeContractTests
    {
        [Test]
        public void GoTask_GetsHarderWithLevel()
        {
            Assert.AreEqual(2, BrakeContract.Pads(1));
            Assert.AreEqual(3, BrakeContract.Pads(5));
            Assert.AreEqual(4, BrakeContract.Pads(12));
            Assert.AreEqual(1400, BrakeContract.DeadlineMs(1, false));
            Assert.AreEqual(800, BrakeContract.DeadlineMs(12, false));
            Assert.AreEqual(1100, BrakeContract.DeadlineMs(12, true));
            for (int l = 2; l <= BrakeContract.MaxLevel; l++)
                Assert.LessOrEqual(BrakeContract.DeadlineMs(l, false), BrakeContract.DeadlineMs(l - 1, false));
        }

        [Test]
        public void Staircase_MovesTheAlarmAndStaysInRange()
        {
            Assert.AreEqual(300, BrakeContract.NextSsd(250, true, 1, false));
            Assert.AreEqual(200, BrakeContract.NextSsd(250, false, 1, false));
            Assert.AreEqual(BrakeContract.SsdMinMs, BrakeContract.NextSsd(BrakeContract.SsdMinMs, false, 1, false));
            int ssd = 250;
            for (int i = 0; i < 40; i++) ssd = BrakeContract.NextSsd(ssd, true, 12, false);
            Assert.AreEqual(BrakeContract.SsdMaxMs(12, false), ssd);
            Assert.Less(BrakeContract.SsdMaxMs(12, false), BrakeContract.DeadlineMs(12, false));
        }

        [Test]
        public void StopTrials_RespectWarmupStreaksAndProportion()
        {
            var rng = new Random(11);
            int stops = 0, inARow = 0, maxRow = 0;
            const int n = 4000;
            for (int i = 0; i < n; i++)
            {
                bool stop = BrakeContract.NextIsStop(i, inARow, rng);
                if (i < BrakeContract.WarmupGoTrials) Assert.IsFalse(stop);
                inARow = stop ? inARow + 1 : 0;
                maxRow = Math.Max(maxRow, inARow);
                if (stop) stops++;
            }
            Assert.LessOrEqual(maxRow, 2);
            Assert.That((float)stops / n, Is.InRange(0.22f, 0.32f));
            for (int i = 0; i < 50; i++) Assert.That(BrakeContract.ForeperiodMs(rng), Is.InRange(600, 1200));
        }

        [Test]
        public void Ssrt_IntegrationMethod()
        {
            // 10 tiempos de ir: 300, 350, ..., 750. Respondió en la mitad de los altos → percentil 50 = 5.º = 500 ms.
            var go = new List<float>();
            for (int i = 0; i < 10; i++) go.Add(300 + 50 * i);
            var ssd = new List<int> { 200, 250, 300, 250, 300, 250 };        // media 258,3
            var responded = new List<bool> { false, false, true, false, true, true };
            Assert.AreEqual(242, BrakeContract.Ssrt(go, ssd, responded)); // 500 − 258,3

            // Pocas señales o probabilidades extremas: sin estimación.
            Assert.AreEqual(-1, BrakeContract.Ssrt(go, new List<int> { 200, 250 }, new List<bool> { true, false }));
            var allStopped = new List<bool> { false, false, false, false, false, false };
            Assert.AreEqual(-1, BrakeContract.Ssrt(go, ssd, allStopped));
            Assert.AreEqual(-1, BrakeContract.Ssrt(null, ssd, responded));
        }

        [Test]
        public void Ssrt_FasterGoMeansFasterBrakeAtTheSameAlarm()
        {
            var ssd = new List<int> { 250, 250, 250, 250, 250, 250 };
            var responded = new List<bool> { true, false, true, false, true, false };
            var slow = new List<float> { 500, 550, 600, 650, 700, 750, 800, 850 };
            var fast = new List<float> { 350, 400, 450, 500, 550, 600, 650, 700 };
            Assert.Less(BrakeContract.Ssrt(fast, ssd, responded), BrakeContract.Ssrt(slow, ssd, responded));
        }

        [Test]
        public void Waiting_IsDetected()
        {
            var steady = new List<float>();
            for (int i = 0; i < 14; i++) steady.Add(480);
            Assert.IsFalse(BrakeContract.IsWaiting(steady));
            var slowing = new List<float>();
            for (int i = 0; i < 7; i++) slowing.Add(450);
            for (int i = 0; i < 7; i++) slowing.Add(700);
            Assert.IsTrue(BrakeContract.IsWaiting(slowing));
            Assert.IsFalse(BrakeContract.IsWaiting(new List<float> { 400, 900 }));
        }

        [Test]
        public void Points_AndScore()
        {
            Assert.Greater(BrakeContract.GoPoints(300, 1400, 1, 1), BrakeContract.GoPoints(1200, 1400, 1, 1));
            Assert.Greater(BrakeContract.GoPoints(500, 1400, 6, 1), BrakeContract.GoPoints(500, 1400, 1, 1));
            Assert.AreEqual(BrakeContract.GoPoints(500, 1400, 1, 11), BrakeContract.GoPoints(500, 1400, 1, 50));
            Assert.Greater(BrakeContract.StopPoints(400, 1), BrakeContract.StopPoints(150, 1));
            Assert.AreEqual(80, BrakeContract.Score(0.8f, -1));
            Assert.AreEqual(100, BrakeContract.Score(1f, 150));
            Assert.AreEqual(50, BrakeContract.Score(1f, 450));
            Assert.Greater(BrakeContract.Score(0.9f, 220), BrakeContract.Score(0.9f, 320));
        }
    }
}
