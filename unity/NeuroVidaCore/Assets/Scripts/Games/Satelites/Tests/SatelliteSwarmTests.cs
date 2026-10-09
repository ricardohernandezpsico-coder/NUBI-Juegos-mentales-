using System;
using System.Collections.Generic;
using NUnit.Framework;

namespace NeuroVida.Games.Satelites.Tests
{
    /// <summary>El movimiento de «Satélites: enciende tu planeta» (docs/diseno-satelites.md §5 y §7), sin Unity: los anillos, el reparto, las velocidades y los sentidos, la media vuelta, el cambio de órbita, la nube, la parada y el asentamiento.</summary>
    public class SatelliteSwarmTests
    {
        private static OrbitLayout Tall() => new ScreenPlan(762f, false).Orbits;     // un teléfono 20:9
        private static OrbitLayout Short() => new ScreenPlan(640f, false).Orbits;    // uno 16:9

        private static float Dist(OrbitSwarm s, int i, int j)
        {
            float dx = s.X[i] - s.X[j], dy = s.Y[i] - s.Y[j];
            return (float)Math.Sqrt(dx * dx + dy * dy);
        }

        // ------------------------------------------------------------------ los anillos (§5)

        [Test]
        public void TheOuterRingNeverLeavesTheField()
        {
            foreach (var lay in new[] { Tall(), Short() })
            {
                float widest = lay.Rx[2] * (1f + SatelliteContract.LaneSpread);
                Assert.AreEqual(180f - (SatelliteContract.SatRadius + SatelliteContract.PanelReach) - SatelliteContract.EdgeMargin, widest, 0.01f,
                    "ancho del más externo = campo/2 − (radio + paneles) − 8 dp, contando el carril más ancho");
                Assert.Less(lay.Rx[0], lay.Rx[1]);
                Assert.Less(lay.Rx[1], lay.Rx[2]);
                // el anillo de adentro deja pasar los paneles sin pisar el planeta
                Assert.Greater(lay.Rx[0] * (1f - SatelliteContract.LaneSpread) - SatelliteContract.SatRadius - SatelliteContract.PanelReach, SatelliteContract.PlanetRadius);
            }
        }

        [Test]
        public void TheRingsAreTallerThanWide_UpToOnePointFortyTwo_AndUseWhatTheScreenGives()
        {
            Assert.AreEqual(1.42f, Tall().RyK, 1e-4f, "en 20:9 se estira todo lo que se pide");
            Assert.That(Short().RyK, Is.InRange(1f, 1.42f), "en 16:9 se achata para que quepa");
            var plan = new ScreenPlan(762f, false);
            var o = plan.Orbits;
            float reach = o.Rx[2] * (1f + SatelliteContract.LaneSpread) * o.RyK + SatelliteContract.SatRadius;
            Assert.LessOrEqual(plan.Cy + reach, plan.BandBottom + 0.5f, "el anillo externo no baja del campo");
            Assert.GreaterOrEqual(plan.Cy - reach - SatelliteContract.EnvelopeRise, plan.BandTop - 0.5f, "ni sube del campo (con el sobre de la señal)");
            var tiny = new ScreenPlan(420f, false).Orbits;
            Assert.GreaterOrEqual(tiny.RyK, 1f, "nunca más ancho que alto");
        }

        // ------------------------------------------------------------------ el reparto, las velocidades y los sentidos

        [Test]
        public void TheSwarmHasTheLevelsCountsAndStartsApart_InEveryLevel()
        {
            for (int level = 1; level <= 12; level++)
            {
                var spec = SatelliteContract.Level(level);
                for (int seed = 0; seed < 25; seed++)
                {
                    var sw = new OrbitSwarm(spec, Surprise.None, Tall(), new Random(seed));
                    Assert.AreEqual(spec.N, sw.Count);
                    int targets = 0;
                    foreach (bool t in sw.Target) if (t) targets++;
                    Assert.AreEqual(spec.K, targets, "nivel " + level);
                    Assert.GreaterOrEqual(sw.MinGap(), SatelliteContract.SpawnGap - 0.01f, "reparto inicial a 46 dp o más (nivel " + level + ", semilla " + seed + ")");
                }
            }
        }

        [Test]
        public void EverySatelliteHasItsOwnSpeed_ZeroPointSeventyFiveToOneTwentyFiveTimesTheLevels()
        {
            foreach (int level in new[] { 1, 6, 12 })
            {
                var spec = SatelliteContract.Level(level);
                var lay = Tall();
                var seen = new HashSet<int>();
                for (int seed = 0; seed < 20; seed++)
                {
                    var sw = new OrbitSwarm(spec, Surprise.None, lay, new Random(seed));
                    for (int i = 0; i < sw.Count; i++)
                    {
                        float v = Math.Abs(sw.Omega[i]) * lay.MeanRadius(sw.Ring[i]);
                        Assert.That(v, Is.InRange(spec.Speed * 0.75f - 0.01f, spec.Speed * 1.25f + 0.01f));
                        seen.Add((int)(v * 10f));
                    }
                }
                Assert.Greater(seen.Count, 20, "no van todos a la misma velocidad: se adelantan");
            }
        }

        [Test]
        public void InTheFirstTwoLevelsEachRingTurnsOneWay_AndFromTheThirdTheyCrossHeadOn()
        {
            var lay = Tall();
            for (int seed = 0; seed < 30; seed++)
                foreach (int level in new[] { 1, 2 })
                {
                    var sw = new OrbitSwarm(SatelliteContract.Level(level), Surprise.None, lay, new Random(seed));
                    for (int a = 0; a < sw.Count; a++)
                        for (int b = a + 1; b < sw.Count; b++)
                            if (sw.Ring[a] == sw.Ring[b]) Assert.AreEqual(Math.Sign(sw.Omega[a]), Math.Sign(sw.Omega[b]), "mismo anillo, mismo sentido (nivel " + level + ")");
                }
            int mixed = 0;
            for (int seed = 0; seed < 30; seed++)
            {
                var sw = new OrbitSwarm(SatelliteContract.Level(5), Surprise.None, lay, new Random(seed));
                for (int a = 0; a < sw.Count; a++)
                    for (int b = a + 1; b < sw.Count; b++)
                        if (sw.Ring[a] == sw.Ring[b] && Math.Sign(sw.Omega[a]) != Math.Sign(sw.Omega[b])) { mixed++; a = b = sw.Count; }
            }
            Assert.Greater(mixed, 20, "desde el nivel 3 casi siempre hay dos de un mismo anillo que giran al revés");
        }

        // ------------------------------------------------------------------ el movimiento es puro y reproducible

        [Test]
        public void SameSeedSameMovement()
        {
            var a = new OrbitSwarm(SatelliteContract.Level(7), Surprise.Orbit, Tall(), new Random(5));
            var b = new OrbitSwarm(SatelliteContract.Level(7), Surprise.Orbit, Tall(), new Random(5));
            for (int i = 0; i < 400; i++) { a.Step(0.016f); b.Step(0.016f); }
            for (int i = 0; i < a.Count; i++)
            {
                Assert.AreEqual(a.X[i], b.X[i]);
                Assert.AreEqual(a.Y[i], b.Y[i]);
                Assert.AreEqual(a.Target[i], b.Target[i]);
            }
        }

        [Test]
        public void EverySatelliteStaysInsideTheField_ThroughTheWholeFollowing_EvenWithJumpsAndTurns()
        {
            var lay = Tall();
            float edge = SatelliteContract.SatRadius + SatelliteContract.PanelReach;
            for (int level = 1; level <= 12; level++)
                for (int seed = 0; seed < 12; seed++)
                {
                    var spec = SatelliteContract.Level(level);
                    var surprise = SatelliteContract.PickSurprise(spec.Surprises, 2, Surprise.None, new Random(seed));
                    var eff = spec.With(surprise);
                    var sw = new OrbitSwarm(eff, surprise, lay, new Random(seed));
                    if (surprise == Surprise.Cloud) sw.StartCloud(360f);
                    while (sw.Time < eff.TrackSeconds + SatelliteContract.MaxExtraTrackSeconds)
                    {
                        sw.Step(1f / 60f);
                        for (int i = 0; i < sw.Count; i++)
                        {
                            Assert.GreaterOrEqual(sw.X[i] - edge, SatelliteContract.EdgeMargin - 0.5f, "se sale por la izquierda (nivel " + level + ")");
                            Assert.GreaterOrEqual(360f - sw.X[i] - edge, SatelliteContract.EdgeMargin - 0.5f, "se sale por la derecha (nivel " + level + ")");
                            Assert.GreaterOrEqual(Math.Sqrt((sw.X[i] - lay.Cx) * (sw.X[i] - lay.Cx) + (sw.Y[i] - lay.Cy) * (sw.Y[i] - lay.Cy)), SatelliteContract.PlanetRadius + SatelliteContract.SatRadius, "pisa el planeta");
                        }
                    }
                }
        }

        // ------------------------------------------------------------------ media vuelta (§5)

        [Test]
        public void FromLevelSixAboutAThirdTurnBackOnce_SmoothlyAndNeverBefore()
        {
            var lay = Tall();
            int total = 0, turned = 0;
            for (int seed = 0; seed < 30; seed++)
            {
                var sw = new OrbitSwarm(SatelliteContract.Level(8), Surprise.None, lay, new Random(seed));
                var before = (float[])sw.Omega.Clone();
                var last = (float[])sw.Omega.Clone();
                float maxJump = 0f;
                for (int f = 0; f < 60 * 7; f++)
                {
                    sw.Step(1f / 60f);
                    for (int i = 0; i < sw.Count; i++) { maxJump = Math.Max(maxJump, Math.Abs(sw.Omega[i] - last[i])); last[i] = sw.Omega[i]; }
                }
                for (int i = 0; i < sw.Count; i++) { total++; if (Math.Sign(sw.Omega[i]) != Math.Sign(before[i])) turned++; }
                Assert.Less(maxJump, 0.2f, "el cambio de sentido es un frenado suave, sin saltos");
            }
            float share = turned / (float)total;
            Assert.That(share, Is.InRange(0.2f, 0.5f), "cerca de un tercio da media vuelta (" + share + ")");

            for (int seed = 0; seed < 20; seed++)
            {
                var sw = new OrbitSwarm(SatelliteContract.Level(5), Surprise.None, lay, new Random(seed));
                var before = (float[])sw.Omega.Clone();
                for (int f = 0; f < 60 * 7; f++) sw.Step(1f / 60f);
                for (int i = 0; i < sw.Count; i++) Assert.AreEqual(Math.Sign(before[i]), Math.Sign(sw.Omega[i]), "antes del nivel 6 nadie da media vuelta");
            }
        }

        // ------------------------------------------------------------------ cambio de órbita (§7)

        [Test]
        public void OrbitChange_TwoToFourJump_OneCarriesAMessage_AndLandOnTheNeighbourRing()
        {
            var lay = Tall();
            for (int seed = 0; seed < 40; seed++)
            {
                var spec = SatelliteContract.Level(5);
                var sw = new OrbitSwarm(spec, Surprise.Orbit, lay, new Random(seed));
                Assert.That(sw.Jumps.Count, Is.InRange(2, 4));
                bool anyTarget = false;
                foreach (var j in sw.Jumps)
                {
                    if (sw.Target[j.Sat]) anyTarget = true;
                    Assert.AreEqual(1, Math.Abs(j.To - sw.Ring[j.Sat]), "salta al anillo vecino");
                    Assert.AreEqual(SatelliteContract.JumpSeconds, j.Duration, 1e-5f);
                    Assert.GreaterOrEqual(j.At, 0.7f - 1e-3f);
                    Assert.LessOrEqual(j.At + j.Duration, spec.TrackSeconds + 1e-3f, "el salto termina antes de que acabe el seguimiento");
                }
                Assert.IsTrue(anyTarget, "al menos uno de los que saltan trae mensaje");
                var distinct = new HashSet<int>();
                foreach (var j in sw.Jumps) Assert.IsTrue(distinct.Add(j.Sat), "cada uno salta una vez");
                while (sw.Time < spec.TrackSeconds) sw.Step(1f / 60f);
                foreach (var j in sw.Jumps) Assert.AreEqual(j.To, sw.Ring[j.Sat], "después del salto queda en el anillo nuevo");
            }
            Assert.AreEqual(0, new OrbitSwarm(SatelliteContract.Level(5), Surprise.Cloud, lay, new Random(1)).Jumps.Count, "sin la sorpresa, nadie salta");
        }

        // ------------------------------------------------------------------ nube de polvo (§7)

        [Test]
        public void TheCloudCrossesTheFieldAndLeavesBeforeTheEnd()
        {
            var lay = Tall();
            for (int level = 4; level <= 12; level++)
                for (int seed = 0; seed < 10; seed++)
                {
                    var spec = SatelliteContract.Level(level);
                    var sw = new OrbitSwarm(spec, Surprise.Cloud, lay, new Random(seed));
                    sw.StartCloud(360f);
                    var c = sw.Cloud;
                    Assert.IsTrue(c.X < -50f || c.X > 410f, "entra desde un costado, fuera del campo");
                    bool crossedMiddle = false;
                    while (sw.Time < spec.TrackSeconds)
                    {
                        sw.Step(1f / 60f);
                        if (Math.Abs(sw.Cloud.X - 180f) < 30f) crossedMiddle = true;
                    }
                    Assert.IsTrue(crossedMiddle, "cruza el campo de lado a lado");
                    Assert.IsTrue(sw.Cloud.X < -100f || sw.Cloud.X > 460f, "sale antes de que termine el seguimiento (nivel " + level + ")");
                    Assert.IsFalse(sw.CloudCoversAny());
                    Assert.AreEqual(160f, sw.Cloud.Rx * 2f, 8f, "unos 160 dp de ancho");
                    Assert.AreEqual(125f, sw.Cloud.Ry * 2f, 8f, "y 125 de alto");
                }
        }

        [Test]
        public void ADustCloudHidesWhatIsUnderIt()
        {
            var c = new DustCloud(100f, 100f, 0f, 78f, 62f);
            Assert.IsTrue(c.Covers(100f, 100f));
            Assert.IsTrue(c.Covers(150f, 110f));
            Assert.IsFalse(c.Covers(100f + 78f, 100f));
            Assert.IsFalse(c.Covers(300f, 300f));
        }

        // ------------------------------------------------------------------ la parada (§4)

        [Test]
        public void ItNeverStopsBeforeTheTime_AndNeverLaterThanNineTenthsOfASecondMore()
        {
            var lay = Tall();
            for (int level = 1; level <= 12; level++)
                for (int seed = 0; seed < 20; seed++)
                {
                    var spec = SatelliteContract.Level(level);
                    var sw = new OrbitSwarm(spec, Surprise.None, lay, new Random(seed));
                    while (!sw.ReadyToStop()) sw.Step(1f / 60f);
                    Assert.GreaterOrEqual(sw.Time, spec.TrackSeconds - 1e-3f, "no se detiene antes de tiempo");
                    Assert.LessOrEqual(sw.Time, spec.TrackSeconds + SatelliteContract.MaxExtraTrackSeconds + 1f / 60f + 1e-3f, "ni más de 0,9 s después");
                    if (sw.Time < spec.TrackSeconds + SatelliteContract.MaxExtraTrackSeconds - 1f / 30f)
                        Assert.GreaterOrEqual(sw.MinGap(), SatelliteContract.StopGap, "si se detuvo antes del límite, estaban separados por 38 dp o más");
                }
        }

        [Test]
        public void ItWaitsWhileAnyoneIsUnderTheCloud()
        {
            var lay = Tall();
            var spec = SatelliteContract.Level(4);
            var sw = new OrbitSwarm(spec, Surprise.Cloud, lay, new Random(2));
            sw.StartCloud(360f);
            while (sw.Time < spec.TrackSeconds - 0.01f) sw.Step(1f / 60f);
            sw.Cloud.X = sw.X[0];
            sw.Cloud.Y = sw.Y[0];                                      // justo encima de uno
            Assert.IsTrue(sw.CloudCoversAny());
            sw.Step(1f / 60f);
            Assert.IsFalse(sw.ReadyToStop() && sw.Time < spec.TrackSeconds + SatelliteContract.MaxExtraTrackSeconds, "bajo la nube no se detiene");
        }

        // ------------------------------------------------------------------ el asentamiento al detenerse

        [Test]
        public void IfTwoStopTooClose_TheyDriftApartALittle_AndNeverEndUpOverlapping()
        {
            foreach (var lay in new[] { Tall(), Short() })
            {
                int needed = 0, total = 0;
                for (int level = 1; level <= 12; level++)
                    for (int seed = 0; seed < 25; seed++)
                    {
                        var surprise = level >= 4 && seed % 3 == 0 ? Surprise.Cloud : seed % 3 == 1 ? Surprise.Orbit : Surprise.None;
                        var sw = new OrbitSwarm(SatelliteContract.Level(level), surprise, lay, new Random(seed * 13 + level));
                        if (surprise == Surprise.Cloud) sw.StartCloud(360f);
                        while (!sw.ReadyToStop()) sw.Step(1f / 60f);
                        var x0 = (float[])sw.X.Clone();
                        var y0 = (float[])sw.Y.Clone();
                        float gapBefore = sw.MinGap();
                        bool moves = sw.PlanSettle();
                        total++;
                        if (moves) needed++;
                        float t = 0f;
                        while (sw.Settling) { sw.SettleStep(1f / 60f); t += 1f / 60f; }
                        Assert.LessOrEqual(t, SatelliteContract.SettleSeconds + 0.05f, "dura 0,35 s");
                        if (!moves) Assert.GreaterOrEqual(gapBefore, SatelliteContract.SettleGap - 2f, "si no se movió (menos de 1 dp), ya estaban separados o a menos de 2 dp de estarlo");
                        Assert.GreaterOrEqual(sw.MinGap(), 30f, "después de asentarse nadie queda encimado (nivel " + level + ", semilla " + seed + ")");
                        for (int i = 0; i < sw.Count; i++)
                        {
                            float d = (float)Math.Sqrt((sw.X[i] - x0[i]) * (sw.X[i] - x0[i]) + (sw.Y[i] - y0[i]) * (sw.Y[i] - y0[i]));
                            Assert.LessOrEqual(d, SatelliteContract.SettleMax + 0.01f, "se aparta poco");
                            Assert.GreaterOrEqual(sw.X[i] - (SatelliteContract.SatRadius + SatelliteContract.PanelReach), SatelliteContract.EdgeMargin - 0.5f);
                            Assert.GreaterOrEqual(360f - sw.X[i] - (SatelliteContract.SatRadius + SatelliteContract.PanelReach), SatelliteContract.EdgeMargin - 0.5f);
                            double toPlanet = Math.Sqrt((sw.X[i] - lay.Cx) * (sw.X[i] - lay.Cx) + (sw.Y[i] - lay.Cy) * (sw.Y[i] - lay.Cy));
                            Assert.GreaterOrEqual(toPlanet, SatelliteContract.PlanetRadius + SatelliteContract.SatRadius - 0.5, "no pisa el planeta");
                        }
                    }
                Assert.Greater(needed, 0, "a veces hace falta");
                Assert.Less(needed, total, "y a veces no");
            }
        }

        [Test]
        public void TwoExactlyOnTopOfEachOtherStillSeparate()
        {
            var lay = Tall();
            var sw = new OrbitSwarm(SatelliteContract.Level(1), Surprise.None, lay, new Random(1));
            sw.X[1] = sw.X[0];
            sw.Y[1] = sw.Y[0];
            Assert.IsTrue(sw.PlanSettle());
            while (sw.Settling) sw.SettleStep(0.05f);
            Assert.Greater(Dist(sw, 0, 1), 30f);
        }

        // ------------------------------------------------------------------ tocar

        [Test]
        public void Nearest_FindsTheTappedSatelliteWithinThirtyDp()
        {
            var sw = new OrbitSwarm(SatelliteContract.Level(1), Surprise.None, Tall(), new Random(1));
            sw.X[0] = 100f; sw.Y[0] = 100f;
            sw.X[1] = 160f; sw.Y[1] = 100f;
            Assert.AreEqual(0, sw.Nearest(105f, 110f));
            Assert.AreEqual(1, sw.Nearest(150f, 100f));
            Assert.AreEqual(-1, sw.Nearest(130f, 400f), "lejos de todos no marca nada");
            Assert.AreEqual(SatelliteContract.TouchRadius, 30f);
        }
    }
}
