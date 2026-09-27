using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;

namespace NeuroVida.Games.Correo.Tests
{
    public class MailContractTests
    {
        [Test]
        public void Levels_GrowInEncargos_Lures_AndPace()
        {
            Assert.AreEqual(1, MailContract.EventColors(1));
            Assert.AreEqual(2, MailContract.EventColors(MailContract.MaxLevel));
            Assert.AreEqual(30f, MailContract.RadioPeriod(1));          // los dos tipos de encargo desde el primer vuelo
            Assert.AreEqual(20f, MailContract.RadioPeriod(MailContract.MaxLevel));
            Assert.AreEqual(0f, MailContract.LureChance(1));
            Assert.Greater(MailContract.LureChance(MailContract.MaxLevel), MailContract.LureChance(2));
            Assert.Greater(MailContract.PlanetGap(1, 0f), MailContract.PlanetGap(MailContract.MaxLevel, 0f));
            Assert.LessOrEqual(MailContract.FlightLevel(MailContract.MaxLevel), 9);
        }

        [Test]
        public void Flight_Accelerates_InThreeStages()
        {
            Assert.AreEqual(0, MailContract.Stage(0.1f));
            Assert.AreEqual(1, MailContract.Stage(0.5f));
            Assert.AreEqual(2, MailContract.Stage(0.9f));
            Assert.AreEqual(1f, MailContract.SpeedRamp(0f));
            Assert.AreEqual(1.5f, MailContract.SpeedRamp(1f), 1e-5f);
            Assert.Greater(MailContract.CurveRamp(1f), MailContract.CurveRamp(0f));
            Assert.Less(MailContract.LaneRamp(1f), MailContract.LaneRamp(0f));
            // Al final del vuelo, más planetas y más asteroides (pero nunca uno encima del otro).
            Assert.Less(MailContract.PlanetGap(3, 1f), MailContract.PlanetGap(3, 0f));
            Assert.Less(MailContract.AsteroidGap(3, 1f), MailContract.AsteroidGap(3, 0f));
            Assert.Less(MailContract.AsteroidGap(9, 0f), MailContract.AsteroidGap(1, 0f));
            Assert.GreaterOrEqual(MailContract.AsteroidGap(9, 1f), 1.1f);
        }

        [Test]
        public void Planets_TargetsAreRare_NeverTwoInARow_AndLuresAreNotTargets()
        {
            var rng = new Random(5);
            for (int level = 1; level <= MailContract.MaxLevel; level++)
            {
                var targets = MailContract.PickTargets(level, rng);
                Assert.AreEqual(MailContract.EventColors(level), targets.Length);
                CollectionAssert.AllItemsAreUnique(targets);
                int n = 0, hits = 0, lures = 0;
                bool prev = false;
                for (int i = 0; i < 4000; i++)
                {
                    var p = MailContract.NextPlanet(level, targets, prev, rng);
                    Assert.AreEqual(p.IsTarget, targets.Contains(p.Color), "solo los del encargo son del encargo");
                    Assert.IsFalse(prev && p.IsTarget, "nunca dos del encargo seguidos");
                    Assert.IsTrue(p.Side == -1 || p.Side == 1);
                    if (p.IsTarget) hits++;
                    if (p.IsLure) lures++;
                    prev = p.IsTarget;
                    n++;
                }
                float share = (float)hits / n;
                Assert.That(share, Is.InRange(0.15f, 0.3f), $"nivel {level}: {share:0.00}");
                if (level <= 1) Assert.AreEqual(0, lures);
                else Assert.Greater(lures, 0);
            }
        }

        [Test]
        public void Radio_OnTimeWithinWindow_OncePerHour_ElseEarlyOrLate()
        {
            var targets = MailContract.RadioTargets(30f, MailContract.FlightSeconds);
            CollectionAssert.AreEqual(new[] { 30f, 60f, 90f, 120f }, targets);
            var answered = new HashSet<int>();
            Assert.AreEqual(RadioJudgement.Early, MailContract.JudgeRadio(20f, targets, answered, out _));
            Assert.AreEqual(RadioJudgement.OnTime, MailContract.JudgeRadio(27f, targets, answered, out int i));
            Assert.AreEqual(0, i);
            answered.Add(i);
            // Dos avisos para la misma hora: el segundo ya no cuenta.
            Assert.AreEqual(RadioJudgement.Late, MailContract.JudgeRadio(31f, targets, answered, out _));
            Assert.AreEqual(RadioJudgement.Late, MailContract.JudgeRadio(40f, targets, answered, out _));
            Assert.AreEqual(RadioJudgement.OnTime, MailContract.JudgeRadio(64.9f, targets, answered, out i));
            Assert.AreEqual(1, i);
            Assert.IsEmpty(MailContract.RadioTargets(0f, MailContract.FlightSeconds));   // sin período, sin horas
        }

        [Test]
        public void Clock_CountsChecksJustBeforeTheHour()
        {
            var targets = MailContract.RadioTargets(30f, 100f);   // 30, 60, 90
            // Estratégico: casi todo en el último 30% (21-30, 51-60, 81-90).
            var strategic = MailContract.Monitoring(new[] { 22f, 26f, 55f, 58f, 84f, 12f }, targets, 30f);
            Assert.AreEqual(6, strategic.Checks);
            Assert.AreEqual(5, strategic.LateChecks);
            Assert.AreEqual(3, strategic.Intervals);
            // Parejo: mirando todo el rato.
            var even = MailContract.Monitoring(Enumerable.Range(0, 30).Select(k => k * 3f + 1f).ToArray(), targets, 30f);
            Assert.Less(even.LateShare, 0.4f);
            // Después de la última hora (más la ventana) no cuenta.
            Assert.AreEqual(0, MailContract.Monitoring(new[] { 99f }, targets, 30f).Checks);
            Assert.AreEqual(-1f, MailContract.Monitoring(new float[0], targets, 30f).LateShare);
        }

        [Test]
        public void Score_RewardsEncargos_PenalizesWrongPlanets()
        {
            int perfect = MailContract.Score(6, 6, 0, 4, 4, 1f, MailContract.MaxLevel);
            Assert.AreEqual(100, perfect);
            Assert.Greater(MailContract.Score(6, 6, 0, 4, 4, 0.9f, 3), MailContract.Score(6, 6, 4, 4, 4, 0.9f, 3));
            Assert.Greater(MailContract.Score(5, 6, 0, 3, 4, 0.9f, 3), MailContract.Score(2, 6, 0, 1, 4, 0.9f, 3));
            // Sin encargo por hora (nivel 1): cuenta solo el de lugar.
            Assert.Greater(MailContract.Score(6, 6, 0, 0, 0, 1f, 1), MailContract.Score(3, 6, 0, 0, 0, 1f, 1));
            Assert.Greater(MailContract.DeliveryPoints(3, 4), MailContract.DeliveryPoints(3, 1));
        }
    }
}
