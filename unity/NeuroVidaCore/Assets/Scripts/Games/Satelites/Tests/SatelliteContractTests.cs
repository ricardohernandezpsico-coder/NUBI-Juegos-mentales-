using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using NeuroVida.Contracts;

namespace NeuroVida.Games.Satelites.Tests
{
    /// <summary>«Satélites: enciende tu planeta» (docs/diseno-satelites.md): los 12 niveles, las sorpresas, las medidas, las luces, el récord, el motor común, la telemetría y los textos.</summary>
    public class SatelliteContractTests
    {
        // ------------------------------------------------------------------ los 12 niveles (§6)

        [Test]
        public void TheLadderHasTwelveLevels_AsInTheTable()
        {
            Assert.AreEqual(12, SatelliteContract.MaxLevel);
            var expected = new (int k, int n, float speed, float track, Surprise surprises, bool head, bool turn)[]
            {
                (2, 6, 55f, 3.5f, Surprise.None, false, false),
                (3, 6, 58f, 3.8f, Surprise.Orbit, false, false),
                (3, 7, 64f, 4.0f, Surprise.Orbit, true, false),
                (3, 8, 70f, 4.2f, Surprise.Orbit | Surprise.Cloud, true, false),
                (4, 8, 74f, 4.4f, Surprise.Orbit | Surprise.Cloud, true, false),
                (4, 9, 80f, 4.6f, Surprise.Orbit | Surprise.Cloud | Surprise.Fast, true, true),
                (4, 10, 88f, 4.8f, Surprise.Orbit | Surprise.Cloud | Surprise.Fast, true, true),
                (4, 10, 96f, 5.0f, Surprise.Orbit | Surprise.Cloud | Surprise.Fast, true, true),
                (5, 11, 100f, 5.2f, Surprise.Orbit | Surprise.Cloud | Surprise.Fast, true, true),
                (5, 11, 108f, 5.4f, Surprise.Orbit | Surprise.Cloud | Surprise.Fast, true, true),
                (5, 12, 116f, 5.7f, Surprise.Orbit | Surprise.Cloud | Surprise.Fast, true, true),
                (5, 13, 126f, 6.0f, Surprise.Orbit | Surprise.Cloud | Surprise.Fast, true, true),
            };
            for (int i = 0; i < 12; i++)
            {
                var s = SatelliteContract.Level(i + 1);
                var e = expected[i];
                string at = " (nivel " + (i + 1) + ")";
                Assert.AreEqual(e.k, s.K, "seguir" + at);
                Assert.AreEqual(e.n, s.N, "de" + at);
                Assert.AreEqual(e.speed, s.Speed, 1e-4f, "velocidad" + at);
                Assert.AreEqual(e.track, s.TrackSeconds, 1e-4f, "seguimiento" + at);
                Assert.AreEqual(e.surprises, s.Surprises, "sorpresas" + at);
                Assert.AreEqual(e.head, s.Head, "de frente" + at);
                Assert.AreEqual(e.turn, s.Turn, "media vuelta" + at);
            }
            Assert.AreEqual(SatelliteContract.Level(1).K, SatelliteContract.Level(0).K, "por debajo del 1 se queda en el 1");
            Assert.AreEqual(SatelliteContract.Level(12).N, SatelliteContract.Level(99).N, "por encima del 12 se queda en el 12");
        }

        [Test]
        public void TheLadderNeverGetsEasier_AndAlwaysHasMoreSatellitesThanMessages()
        {
            for (int l = 2; l <= 12; l++)
            {
                var a = SatelliteContract.Level(l - 1);
                var b = SatelliteContract.Level(l);
                Assert.GreaterOrEqual(b.K, a.K);
                Assert.GreaterOrEqual(b.N, a.N);
                Assert.Greater(b.Speed, a.Speed);
                Assert.Greater(b.TrackSeconds, a.TrackSeconds);
                Assert.IsTrue((b.Surprises & a.Surprises) == a.Surprises, "las sorpresas solo se suman");
                Assert.IsTrue(!a.Head || b.Head);
                Assert.IsTrue(!a.Turn || b.Turn);
            }
            for (int l = 1; l <= 12; l++) Assert.Greater(SatelliteContract.Level(l).N, SatelliteContract.Level(l).K);
            Assert.AreEqual(1f, SatelliteContract.SpeedFactor(1), 1e-5f);
            Assert.AreEqual(126f / 55f, SatelliteContract.SpeedFactor(12), 1e-4f);
        }

        [Test]
        public void TheNewMechanicsArriveGradually()
        {
            Assert.AreEqual(Surprise.None, SatelliteContract.Level(1).Surprises, "el nivel 1 no trae sorpresas");
            Assert.IsFalse(SatelliteContract.Level(2).Head, "hasta el 2 cada anillo gira para un solo lado");
            Assert.IsTrue(SatelliteContract.Level(3).Head);
            Assert.IsFalse(SatelliteContract.Level(3).Surprises.HasFlag(Surprise.Cloud), "la nube desde el 4");
            Assert.IsTrue(SatelliteContract.Level(4).Surprises.HasFlag(Surprise.Cloud));
            Assert.IsFalse(SatelliteContract.Level(5).Turn, "la media vuelta desde el 6");
            Assert.IsTrue(SatelliteContract.Level(6).Turn);
            Assert.IsFalse(SatelliteContract.Level(5).Surprises.HasFlag(Surprise.Fast), "las órbitas rápidas desde el 6");
            Assert.IsTrue(SatelliteContract.Level(6).Surprises.HasFlag(Surprise.Fast));
        }

        [Test]
        public void FastOrbits_AreAQuarterFasterAndAFifthShorter()
        {
            var normal = SatelliteContract.Level(6);
            var fast = normal.With(Surprise.Fast);
            Assert.AreEqual(normal.Speed * 1.25f, fast.Speed, 1e-4f);
            Assert.AreEqual(normal.TrackSeconds * 0.8f, fast.TrackSeconds, 1e-4f);
            Assert.AreEqual(normal.K, fast.K);
            Assert.AreEqual(normal.N, fast.N);
            var other = normal.With(Surprise.Cloud);
            Assert.AreEqual(normal.Speed, other.Speed, 1e-4f, "las otras sorpresas no cambian la velocidad");
        }

        [Test]
        public void ThePracticeRoundIsSlowTwoOfFiveWithNoSurprises()
        {
            var p = SatelliteContract.Practice;
            Assert.AreEqual(2, p.K);
            Assert.AreEqual(5, p.N);
            Assert.AreEqual(Surprise.None, p.Surprises);
            Assert.IsFalse(p.Head);
            Assert.IsFalse(p.Turn);
            Assert.Less(p.Speed, SatelliteContract.Level(1).Speed, "más lenta que el nivel 1");
        }

        // ------------------------------------------------------------------ sorpresas (§7)

        [Test]
        public void SurprisesComeEveryOtherRoundFromTheSecond_AndNeverTwiceInARow()
        {
            for (int seed = 0; seed < 40; seed++)
            {
                var rng = new Random(seed);
                var last = Surprise.None;
                int count = 0;
                for (int round = 1; round <= 8; round++)
                {
                    var s = SatelliteContract.PickSurprise(SatelliteContract.Level(8).Surprises, round, last, rng);
                    if (round == 1 || round % 2 == 1) { Assert.AreEqual(Surprise.None, s, "ronda " + round + " sin sorpresa"); continue; }
                    Assert.AreNotEqual(Surprise.None, s, "ronda " + round + " con sorpresa");
                    Assert.AreNotEqual(last, s, "no repite la anterior");
                    Assert.IsTrue(SatelliteContract.Level(8).Surprises.HasFlag(s));
                    last = s;
                    count++;
                }
                Assert.AreEqual(4, count, "cuatro sorpresas en ocho rondas");
            }
        }

        [Test]
        public void WhenOnlyOneSurpriseIsPossibleItIsThatOne_AndLevelOneNeverHasAny()
        {
            var rng = new Random(3);
            Assert.AreEqual(Surprise.Orbit, SatelliteContract.PickSurprise(Surprise.Orbit, 2, Surprise.Orbit, rng));
            Assert.AreEqual(Surprise.Orbit, SatelliteContract.PickSurprise(Surprise.Orbit, 4, Surprise.None, rng));
            Assert.AreEqual(Surprise.None, SatelliteContract.PickSurprise(Surprise.None, 2, Surprise.None, rng));
        }

        [Test]
        public void EverySurpriseHasANameAndALine_ThatFitTheCard()
        {
            foreach (var s in new[] { Surprise.Orbit, Surprise.Cloud, Surprise.Fast })
            {
                Assert.IsNotEmpty(SatelliteContract.SurpriseName(s));
                Assert.IsNotEmpty(SatelliteContract.SurpriseLine(s));
                Assert.LessOrEqual(SatelliteContract.SurpriseName(s).Length, 24);
                Assert.LessOrEqual(SatelliteContract.SurpriseLine(s).Length, 60, "dos renglones como mucho");
                Assert.IsNotEmpty(SatelliteContract.TrackHint(s));
                Assert.LessOrEqual(SatelliteContract.TrackHint(s).Length, 36);
            }
            Assert.AreEqual("", SatelliteContract.SurpriseName(Surprise.None));
            Assert.AreEqual("", SatelliteContract.TrackHint(Surprise.None));
        }

        // ------------------------------------------------------------------ medidas (§8)

        [Test]
        public void TrackedEstimate_DiscountsLuckyGuesses()
        {
            Assert.AreEqual(4f, SatelliteContract.TrackedEstimate(4, 4, 8), 1e-4f, "todo bien: se siguieron todos");
            Assert.AreEqual(0f, SatelliteContract.TrackedEstimate(0, 4, 8), 1e-3f);
            Assert.AreEqual(0f, SatelliteContract.TrackedEstimate(2, 4, 8), 1e-3f, "lo que se acierta al azar (k²/n) equivale a no haber seguido ninguno");
            Assert.That(SatelliteContract.TrackedEstimate(3, 4, 8), Is.InRange(2f, 3f));
            Assert.Greater(SatelliteContract.TrackedEstimate(4, 5, 10), SatelliteContract.TrackedEstimate(3, 5, 10));
            Assert.AreEqual(0f, SatelliteContract.TrackedEstimate(2, 3, 3), "casos raros no rompen");
            Assert.AreEqual(0f, SatelliteContract.TrackedEstimate(1, 0, 6));
        }

        [Test]
        public void Capacity_AveragesRounds_AndAPerfectRoundIsWorthAllItsMessages()
        {
            Assert.AreEqual(-1f, SatelliteContract.Capacity(new List<(int, int, int)>()));
            var rounds = new List<(int, int, int)> { (3, 3, 7), (4, 4, 8) };
            Assert.AreEqual(3.5f, SatelliteContract.Capacity(rounds), 1e-4f);
            Assert.IsTrue(SatelliteContract.IsPerfect(3, 3));
            Assert.IsFalse(SatelliteContract.IsPerfect(2, 3));
            Assert.IsFalse(SatelliteContract.IsPerfect(0, 0));
        }

        [Test]
        public void Score_IsHitRateAndHighestClearedLevel()
        {
            Assert.AreEqual(0, SatelliteContract.Score(0f, 0));
            Assert.AreEqual(100, SatelliteContract.Score(1f, SatelliteContract.MaxLevel));
            Assert.AreEqual(60, SatelliteContract.Score(1f, 0));
            Assert.Greater(SatelliteContract.Score(0.8f, 6), SatelliteContract.Score(0.8f, 3));
        }

        [Test]
        public void ClosestApproach_FindsTheCrossing()
        {
            var ax = new List<float> { 0f, 0.2f, 0.4f, 0.6f };
            var ay = new List<float> { 0f, 0f, 0f, 0f };
            var bx = new List<float> { 0.6f, 0.45f, 0.42f, 0.2f };
            var by = new List<float> { 0.5f, 0.2f, 0.01f, 0.3f };
            Assert.AreEqual(2, SatelliteContract.ClosestApproach(ax, ay, bx, by));
            Assert.AreEqual(-1, SatelliteContract.ClosestApproach(new List<float>(), ay, bx, by));
        }

        [Test]
        public void TryCrossPoint_IsTheMidpointWhereTheyPassedClosest_OrNothingIfTheyNeverMet()
        {
            var fx = new List<float[]> { new[] { 0f, 100f }, new[] { 50f, 80f }, new[] { 120f, 125f } };
            var fy = new List<float[]> { new[] { 0f, 100f }, new[] { 50f, 80f }, new[] { 120f, 125f } };
            Assert.IsTrue(SatelliteContract.TryCrossPoint(fx, fy, 0, 1, out float x, out float y));
            Assert.AreEqual(122.5f, x, 1e-3f);
            Assert.AreEqual(122.5f, y, 1e-3f);
            var far = new List<float[]> { new[] { 0f, 200f }, new[] { 10f, 210f } };
            Assert.IsFalse(SatelliteContract.TryCrossPoint(far, far, 0, 1, out _, out _), "si nunca pasaron cerca, la confusión no vino de un cruce");
            Assert.IsFalse(SatelliteContract.TryCrossPoint(new List<float[]>(), new List<float[]>(), 0, 1, out _, out _));
        }

        // ------------------------------------------------------------------ las luces y el récord

        [Test]
        public void LightSpots_StayInsideThePlanet_AndDoNotPileUp()
        {
            for (int seed = 0; seed < 20; seed++)
            {
                var rng = new Random(seed);
                var placed = new List<(float x, float y)>();
                for (int i = 0; i < 40; i++)
                {
                    var p = SatelliteContract.NextLightSpot(placed, rng);
                    float d = (float)Math.Sqrt(p.x * p.x + p.y * p.y);
                    Assert.LessOrEqual(d, SatelliteContract.PlanetRadius - 9f + 1e-3f, "dentro del planeta, a 9 dp del borde");
                    foreach (var q in placed)
                        Assert.Greater(Math.Sqrt((q.x - p.x) * (q.x - p.x) + (q.y - p.y) * (q.y - p.y)), SatelliteContract.LightGap, "no se amontonan");
                    placed.Add(p);
                }
            }
        }

        [Test]
        public void LightSpots_AreReproducibleAndAlwaysFindARoom()
        {
            var a = SatelliteContract.NextLightSpot(new List<(float, float)>(), new Random(5));
            var b = SatelliteContract.NextLightSpot(new List<(float, float)>(), new Random(5));
            Assert.AreEqual(a, b);
            // aunque el planeta ya esté lleno, devuelve algo sin romper
            var rng = new Random(1);
            var full = new List<(float x, float y)>();
            for (int i = 0; i < 400; i++) full.Add(SatelliteContract.NextLightSpot(full, rng));
            Assert.AreEqual(400, full.Count);
        }

        [Test]
        public void TheRecordIsTheBestNumberOfLights_AndOnlyBrokenWithAtLeastOne()
        {
            Assert.AreEqual(12, SatelliteContract.NewRecord(0, 12));
            Assert.AreEqual(30, SatelliteContract.NewRecord(30, 12));
            Assert.AreEqual(0, SatelliteContract.NewRecord(-5, -1));
            Assert.IsTrue(SatelliteContract.BrokeRecord(10, 11));
            Assert.IsFalse(SatelliteContract.BrokeRecord(10, 10));
            Assert.IsFalse(SatelliteContract.BrokeRecord(0, 0));
            Assert.IsTrue(SatelliteContract.BrokeRecord(0, 1), "la primera luz ya es un récord");
        }

        // ------------------------------------------------------------------ textos (español cercano, sin jerga)

        [Test]
        public void TheTextsReadWell()
        {
            Assert.AreEqual("Estos 3 traen un mensaje", SatelliteContract.CueTitle(3));
            Assert.AreEqual("Toca los 4 que traían mensaje", SatelliteContract.AnswerTitle(4));
            Assert.AreEqual("2 de 3 marcados · otro toque lo quita", SatelliteContract.AnswerSub(2, 3));
            Assert.AreEqual("¡3 de 3!", SatelliteContract.ResultTitle(3, 3));
            Assert.AreEqual("2 de 3", SatelliteContract.ResultTitle(2, 3));
            Assert.AreEqual("Racha ×3", SatelliteContract.ResultSub(true, 3));
            Assert.AreEqual("Cada mensaje enciende una luz", SatelliteContract.ResultSub(true, 2));
            StringAssert.Contains("punteado", SatelliteContract.ResultSub(false, 0));
            Assert.AreEqual("Ronda 3 de 8", SatelliteContract.RoundHud(3, 8));
            Assert.AreEqual("Ronda 3", SatelliteContract.RoundHudReto(3));
            Assert.AreEqual("Luces encendidas: 7", SatelliteContract.LightsFooter(7));
            Assert.AreEqual("¿Aquí se cruzaron?", SatelliteContract.CrossText);
            Assert.AreEqual("Encendiste 1 luz", SatelliteContract.LightsTitle(1));
            Assert.AreEqual("Encendiste 12 luces", SatelliteContract.LightsTitle(12));
            Assert.AreEqual("No se encendió ninguna luz", SatelliteContract.LightsTitle(0));
        }

        [Test]
        public void TheEndLinesUseTheDecimalCommaAndOnlyShowWhatWasMeasured()
        {
            Assert.AreEqual("2,6 de 3,5 a la vez", SatelliteContract.TrackedLine(2.6f, 3.5f));
            Assert.AreEqual("3,0 de 3 a la vez", SatelliteContract.TrackedLine(3f, 3f));
            Assert.AreEqual("Rondas perfectas: 3 de 8", SatelliteContract.PerfectLine(3, 8, 1));
            Assert.AreEqual("Rondas perfectas: 5 de 8 · racha mayor ×3", SatelliteContract.PerfectLine(5, 8, 3));
            Assert.AreEqual("¡Récord nuevo: 22 luces!", SatelliteContract.RecordLine(22, true));
            Assert.AreEqual("Tu récord: 22 luces en una partida", SatelliteContract.RecordLine(22, false));
            Assert.AreEqual("", SatelliteContract.RecordLine(0, false));
        }

        [Test]
        public void NoTextUsesTheForbiddenWords()
        {
            var all = new List<string>
            {
                SatelliteContract.Title, SatelliteContract.CountdownSub, SatelliteContract.NewTag, SatelliteContract.SurpriseTag, SatelliteContract.CrossText,
                SatelliteContract.TrackTitle, SatelliteContract.CueSub, SatelliteContract.CueTitle(3), SatelliteContract.AnswerTitle(3), SatelliteContract.AnswerSub(1, 3),
                SatelliteContract.ResultTitle(2, 3), SatelliteContract.ResultSub(true, 4), SatelliteContract.ResultSub(false, 0), SatelliteContract.LightsTitle(5),
                SatelliteContract.LightsFooter(5), SatelliteContract.TrackedLine(2f, 3f), SatelliteContract.PerfectLine(2, 8, 2), SatelliteContract.RecordLine(5, true),
            };
            foreach (var s in new[] { Surprise.Orbit, Surprise.Cloud, Surprise.Fast }) { all.Add(SatelliteContract.SurpriseName(s)); all.Add(SatelliteContract.SurpriseLine(s)); all.Add(SatelliteContract.TrackHint(s)); }
            foreach (string text in all)
            {
                string t = text.ToLowerInvariant();
                foreach (string bad in new[] { "cognitiv", "entrenamiento", "cerebro", "diagnóstico" })
                    Assert.IsFalse(t.Contains(bad), "«" + text + "» usa «" + bad + "»");
            }
        }

        // ------------------------------------------------------------------ motor común y telemetría

        [Test]
        public void TheCommonEngine_APerfectRoundClimbsALevel_AndAFailureDropsAtMostOne()
        {
            var config = new SequenceConfigDetails { level = 1, age_band = "ADULT" };
            var dda = SatelliteMetrics.CreateEngine(config);
            for (int i = 0; i < 6; i++) dda.Register(true);                  // los seis primeros ensayos pesan 1,5 (calibración)
            float before = dda.Rating;
            dda.Register(true);
            Assert.AreEqual(1f, dda.Rating - before, 1e-4f, "una ronda perfecta, después de la calibración, sube un nivel entero");
            before = dda.Rating;
            dda.Register(false);
            Assert.GreaterOrEqual(dda.Rating, before - 1.0001f, "un error baja como mucho un nivel");
            Assert.Less(dda.Rating, before);
        }

        [Test]
        public void TheDebugStageStartsTheGameAtThatLevel()
        {
            var config = new SequenceConfigDetails { level = 1, sat_stage = 6 };
            var dda = SatelliteMetrics.CreateEngine(config);
            Assert.AreEqual(6, dda.Level);
            Assert.AreEqual(1, SatelliteMetrics.CreateEngine(new SequenceConfigDetails { level = 1 }).Level, "sin nivel de prueba, el de siempre");
            Assert.AreEqual(12f, SatelliteMetrics.StartRating(new SequenceConfigDetails { sat_stage = 99 }), 0.01f, "el tope es 12");
        }

        [Test]
        public void TheTelemetryCarriesTheOldMeasuresAndTheNewOnes()
        {
            var config = new SequenceConfigDetails { level = 1, timed = true };
            var dda = SatelliteMetrics.CreateEngine(config);
            var run = new SatelliteRun();
            run.Add(3, 3, 7, 2);
            run.Add(2, 3, 7, 2);
            run.Add(4, 4, 8, 5);
            dda.Register(true); dda.Register(false); dda.Register(true);
            var m = SatelliteMetrics.Build(dda, run, 9, true, config);
            Assert.AreEqual(2, m.correct_trials, "rondas perfectas");
            Assert.AreEqual(3, m.total_trials);
            Assert.AreEqual(9, m.sat_lights);
            Assert.AreEqual(2, m.sat_perfect);
            Assert.AreEqual(1, m.sat_best_streak, "la racha de rondas perfectas: la del medio la cortó");
            Assert.AreEqual(9, m.sat_best);
            Assert.AreEqual(1, m.sat_new);
            Assert.AreEqual(run.Capacity, m.tracking_capacity, 1e-5f);
            Assert.AreEqual((3 + 3 + 4) / 3f, m.tracking_targets, 1e-5f);
            Assert.AreEqual(SatelliteContract.SpeedFactor(5), m.tracking_speed, 1e-5f);
            Assert.AreEqual(run.Score, m.calculated_score);
            Assert.AreEqual(0, m.average_response_time_ms, "sin tiempo de reacción: el tiempo es el de la tarea");
            Assert.IsTrue(m.timed);
        }
    }
}
