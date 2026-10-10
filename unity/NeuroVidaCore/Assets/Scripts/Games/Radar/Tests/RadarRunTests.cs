using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using NeuroVida.Contracts;
using UnityEngine;

namespace NeuroVida.Games.Radar.Tests
{
    /// <summary>Lo que suma una partida (rescatadas, rondas perfectas, rachas, «Tu vistazo», «Tu captura», lluvias fuera del motor), la disposición de la pantalla en cuatro formas de teléfono, el motor común, la telemetría y el arte y el sonido que se hornean.</summary>
    public class RadarRunTests
    {
        private static RadarRound Round(int level = 5, bool rain = false, int seed = 1) =>
            rain ? RadarContract.RainRound(level, new System.Random(seed)) : RadarContract.NextRound(level, new System.Random(seed));

        [SetUp]
        public void NoModeLimits() => AdaptiveDifficulty.ConfigureMode(null);

        // ------------------------------------------------------------------ la partida

        [Test]
        public void ARoundCountsRescuedCapsules_AndAPerfectOneRaisesTheStreak()
        {
            var run = new RadarRun();
            var r = Round(5);                                           // 3 cápsulas
            var o1 = run.Add(r, 3, 0, 350f);
            Assert.IsTrue(o1.Perfect && o1.Success);
            Assert.AreEqual(1, run.Streak);
            var o2 = run.Add(r, 2, 1, 350f);
            Assert.IsFalse(o2.Perfect);
            Assert.IsFalse(o2.Success, "2 − 1 < 3");
            Assert.AreEqual(0, run.Streak);
            Assert.AreEqual(5, run.Rescued);
            Assert.AreEqual(1, run.WrongPicks);
            Assert.AreEqual(1, run.Perfect);
            Assert.AreEqual(1, run.BestStreak);
            run.Add(r, 3, 0, 350f); run.Add(r, 3, 0, 350f); run.Add(r, 3, 0, 350f);
            Assert.AreEqual(3, run.Streak);
            Assert.AreEqual(3, run.BestStreak);
            Assert.AreEqual(5, run.RoundsPlayed);
        }

        [Test]
        public void TheRainsStayOutOfTheEngineTheAccuracyAndTheGlance()
        {
            var run = new RadarRun();
            for (int i = 0; i < 3; i++) run.Add(Round(5, false, i), 3, 0, 350f);
            run.Add(Round(5, true, 9), 4, 0, 300f);
            Assert.AreEqual(4, run.RoundsPlayed);
            Assert.AreEqual(3, run.NormalRounds, "la lluvia no es una ronda normal");
            Assert.AreEqual(3, run.Successes);
            Assert.AreEqual(1, run.Rains);
            Assert.AreEqual(3, run.RealMs.Count, "su destello de 300 ms no entra a «Tu vistazo»");
            Assert.AreEqual(1f, run.Accuracy, 1e-5f);
            Assert.AreEqual(13, run.Rescued, "pero las cápsulas de la lluvia sí se rescatan (el premio)");
        }

        [Test]
        public void TheCaptureComesFromTheRains_AndNeedsTwo()
        {
            var run = new RadarRun();
            run.Add(Round(5, true, 1), 4, 0, 300f);
            Assert.AreEqual(-1f, run.Capture);
            run.Add(Round(5, true, 2), 3, 1, 300f);                                 // 3 − 2 = 1
            Assert.AreEqual((4 + 1) / 2f, run.Capture, 1e-5f);
            run.Add(Round(5, true, 3), 0, 3, 300f);                                 // 0 − 6 = −6: cada lluvia NO se corta en 0
            Assert.AreEqual((4 + 1 - 6) / 3f > 0 ? (4 + 1 - 6) / 3f : 0f, run.Capture, 1e-5f);
            Assert.AreEqual(0f, run.Capture, "el total tiene mínimo 0");
        }

        [Test]
        public void TheShortestFlashSolvedIsTheRealOneOfAPerfectNormalRound()
        {
            var run = new RadarRun();
            Assert.AreEqual(0, run.ShortestPerfectMs);
            run.Add(Round(7, false, 1), 4, 0, 283f);
            run.Add(Round(7, false, 2), 2, 2, 120f);                                // no perfecta: no cuenta
            run.Add(Round(7, true, 3), 4, 0, 100f);                                 // lluvia: no cuenta
            run.Add(Round(7, false, 4), 4, 0, 216f);
            Assert.AreEqual(216, run.ShortestPerfectMs);
        }

        [Test]
        public void TheGlanceAndTheScoreComeFromTheRealDurations()
        {
            var run = new RadarRun();
            var real = new float[] { 800, 600, 600, 350, 350, 280, 280, 280 };
            foreach (float ms in real) run.Add(Round(5), 3, 0, ms);
            Assert.AreEqual(run.GlanceMs, RadarContract.GlanceMs(real));
            Assert.Greater(run.GlanceMs, 0);
            Assert.AreEqual(run.GlanceLoad, RadarContract.GlanceLoad(Enumerable.Repeat(3, 8).ToArray()), 1e-5f);
            Assert.AreEqual(RadarContract.Score(1f, run.GlanceMs), run.Score);
            Assert.AreEqual(-1f, new RadarRun().GlanceLoad);
        }

        // ------------------------------------------------------------------ la pantalla

        [Test]
        public void TheScreenFitsInFourPhoneShapes_AndTheStripNeverTouchesTheRadarTheShipOrTheBoard()
        {
            foreach (float h in new[] { 640f, 720f, 800f, 900f })
                foreach (bool guided in new[] { false, true })
                {
                    var p = new RadarPlan(h, guided);
                    string at = " (alto " + h + ", guiada " + guided + ", escala " + p.Scale.ToString("0.00") + ")";
                    var strip = p.StripBox;
                    Assert.IsFalse(strip.Intersects(p.RadarBox), "franja y radar" + at);
                    Assert.IsFalse(strip.Intersects(p.ShipBox), "franja y nave" + at);
                    Assert.IsFalse(strip.Intersects(p.BoardBox), "franja y tablero" + at);
                    Assert.IsFalse(strip.Intersects(p.GoBox), "franja y «¡Rescatar!»" + at);
                    Assert.IsFalse(p.RadarBox.Intersects(p.BoardBox), "radar y tablero" + at);
                    Assert.IsFalse(p.ShipBox.Intersects(p.RadarBox), "la nave no pisa el radar" + at);
                    Assert.IsFalse(p.ShipBox.Intersects(p.BoardBox), "la nave no pisa el tablero" + at);
                    Assert.IsFalse(p.ShipBox.Intersects(p.GoBox), "la nave no pisa «¡Rescatar!»" + at);
                    Assert.GreaterOrEqual(p.ShipBox.X0, 0f, "la nave entra a lo ancho" + at);
                    Assert.LessOrEqual(p.ShipBox.X1, RadarPlan.Width, "la nave entra a lo ancho" + at);
                    Assert.IsFalse(p.GoBox.Intersects(p.BoardBox), "«¡Rescatar!» y tablero" + at);
                    Assert.LessOrEqual(p.GoBox.Y1, h - (guided ? RadarPlan.TutorialControls : 0f) + 0.01f, "todo entra" + at);
                    Assert.GreaterOrEqual(p.RadarBox.X0, 0f, "radar dentro" + at);
                    Assert.LessOrEqual(p.RadarBox.X1, RadarPlan.Width, "radar dentro" + at);
                    Assert.LessOrEqual(p.BoardBox.X1, RadarPlan.Width, "tablero dentro" + at);
                    Assert.GreaterOrEqual(p.Scale, 0.75f, "la escala no baja de 0,75" + at);
                    Assert.AreEqual(128f * p.Scale, p.GlassR, 1e-3f, "el radio del radar es el fijo por la escala de pantalla" + at);
                }
        }

        [Test]
        public void TheBoardIs3By2WithCellsOf104By74Dp_OnTheTallReferenceScreen()
        {
            var p = new RadarPlan(780f, false);
            Assert.AreEqual(146f / 128f, p.Scale, 1e-4f);
            Assert.AreEqual(104f, p.CellW, 1e-2f);
            Assert.AreEqual(74f, p.CellH, 1e-2f);
            Assert.AreEqual(8f, p.CellGap, 1e-2f);
            Assert.AreEqual(172f, p.GoW, 1e-2f);
            Assert.AreEqual(44f, p.GoH, 1e-2f);
            var (x0, y0) = p.CellCenter(0);
            var (x2, y2) = p.CellCenter(2);
            var (x3, y3) = p.CellCenter(3);
            Assert.AreEqual(y0, y2, 1e-3f);
            Assert.AreEqual(p.CellW + p.CellGap, x2 - x0 - (p.CellW + p.CellGap), 1e-3f);
            Assert.AreEqual(x0, x3, 1e-3f);
            Assert.AreEqual(p.CellH + p.CellGap, y3 - y0, 1e-3f);
            // el tablero nunca depende de la ronda: su geometría sale solo de la pantalla
            Assert.AreEqual(new RadarPlan(780f, false).BoardBox.X0, p.BoardBox.X0);
        }

        // ------------------------------------------------------------------ el motor común y la telemetría

        private static SequenceConfigDetails Config(int stage = 6) => new SequenceConfigDetails { age_band = "ADULT", resc_stage = stage, level = 3, timed = false };

        [Test]
        public void TheEngineIsTheCommonOne_AndTheDebugStageSetsTheLevel()
        {
            var dda = RadarMetrics.CreateEngine(Config(6));
            Assert.AreEqual(6, dda.Level);
            Assert.AreEqual(12, dda.MaxLevel);
            Assert.AreEqual(3, RadarMetrics.CreateEngine(Config(3)).Level);
            Assert.AreEqual(12f, RadarMetrics.StartRating(Config(99)), 1e-5f);
            Assert.AreEqual(0.8f, dda.TargetAccuracy, 1e-5f, "adulto: objetivo ~80 %");
        }

        [Test]
        public void TheEngineStepsBy0Point3WithoutReactionTime()
        {
            var dda = RadarMetrics.CreateEngine(Config(5));
            for (int i = 0; i < 7; i++) dda.Register(true);
            float before = dda.Rating;
            dda.Register(true);
            Assert.AreEqual(RadarMetrics.StepUp, dda.Rating - before, 1e-4f);
        }

        [Test]
        public void TheTelemetryCarriesWhatWasMeasured_AndNoPlacesAtAll()
        {
            var dda = RadarMetrics.CreateEngine(Config());
            var run = new RadarRun();
            var real = new float[] { 800, 600, 600, 350, 350, 280, 280, 280 };
            foreach (float ms in real) { var o = run.Add(Round(5), 3, 0, ms); dda.Register(o.Success); }
            run.Add(Round(5, true, 5), 4, 0, 300f);
            run.Add(Round(5, true, 6), 3, 0, 300f);
            var m = RadarMetrics.Build(dda, run, 30, true, Config());
            Assert.AreEqual(8, m.correct_trials);
            Assert.AreEqual(8, m.total_trials, "solo las rondas normales");
            Assert.AreEqual(run.Score, m.calculated_score);
            Assert.AreEqual(run.GlanceMs, m.glance_ms);
            Assert.AreEqual(3f, m.glance_load, 1e-5f);
            Assert.AreEqual(3.5f, m.capture, 1e-5f);
            Assert.AreEqual(run.Rescued, m.resc_rescued);
            Assert.AreEqual(10, m.resc_rounds);
            Assert.AreEqual(run.Perfect, m.resc_perfect);
            Assert.AreEqual(run.BestStreak, m.resc_best_streak);
            Assert.AreEqual(30, m.resc_best);
            Assert.AreEqual(1, m.resc_new);
            Assert.AreEqual(280, m.resc_shortest_ms);
            Assert.AreEqual(dda.RatingNormalized, m.end_rating, 1e-5f);
            // lo que se borró: ninguna medida por lugar (sector/aro) ni robots
            var fields = typeof(StroopSessionMetrics).GetFields().Select(f => f.Name).ToArray();
            foreach (var gone in new[] { "sector_hits", "sector_trials", "ring_hits", "ring_trials", "robots_shown", "robots_touched" })
                CollectionAssert.DoesNotContain(fields, gone, gone);
        }

        // ------------------------------------------------------------------ el arte y el sonido

        [Test]
        public void SixCapsulesAreDrawn_WithFixedColorsAndNameableShapes_AndTheSameAtEveryLevel()
        {
            const int px = 64;
            var colors = RadarSprites.Colors;
            Assert.AreEqual(6, colors.Length);
            for (int i = 0; i < colors.Length; i++)
                for (int j = i + 1; j < colors.Length; j++)
                {
                    float d = Math.Abs(colors[i].r - colors[j].r) + Math.Abs(colors[i].g - colors[j].g) + Math.Abs(colors[i].b - colors[j].b);
                    Assert.Greater(d, 0.2f, RadarContract.TypeNames[i] + " y " + RadarContract.TypeNames[j] + " se parecen en color");
                }
            var all = new List<Color32[]>();
            foreach (CapsuleType t in Enum.GetValues(typeof(CapsuleType)))
            {
                var pixels = RadarSprites.RenderCapsule(t, px);
                Assert.AreEqual(px * px, pixels.Length);
                Assert.Greater(pixels[(px / 2) * px + px / 2].a, 200, "opaca al centro: " + t);
                // el color de la forma domina (la cápsula ES el símbolo: forma + color grandes)
                var c = colors[(int)t];
                int opaque = 0, close = 0;
                foreach (var p in pixels)
                {
                    if (p.a < 200) continue;
                    opaque++;
                    if (Math.Abs(p.r - c.r * 255f) < 60f && Math.Abs(p.g - c.g * 255f) < 60f && Math.Abs(p.b - c.b * 255f) < 60f) close++;
                }
                Assert.Greater(close / (float)opaque, 0.3f, t + ": el color de la forma domina");
                all.Add(pixels);
                Assert.AreEqual(px * px, RadarSprites.RenderEmblem(t, px).Length);
            }
            for (int i = 0; i < all.Count; i++)
                for (int j = i + 1; j < all.Count; j++)
                {
                    int diff = 0;
                    for (int k = 0; k < px * px; k++) if (Math.Abs(all[i][k].a - all[j][k].a) > 120 || Math.Abs(all[i][k].r - all[j][k].r) + Math.Abs(all[i][k].g - all[j][k].g) + Math.Abs(all[i][k].b - all[j][k].b) > 150) diff++;
                    Assert.Greater(diff, px * px / 40, "las cápsulas " + i + " y " + j + " casi no se distinguen");
                }
            // regla de patentes 5: ningún color ni brillo depende del nivel (no hay función de nivel que devuelva colores)
            foreach (var m in typeof(RadarSprites).GetMethods(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static))
                foreach (var prm in m.GetParameters())
                    Assert.IsFalse(prm.Name.ToLowerInvariant().Contains("level") || prm.Name.ToLowerInvariant().Contains("nivel"), m.Name + " recibe el nivel");
        }

        [Test]
        public void TheRockIsGrayAndDoesNotLookLikeACapsule_AndTheRestOfTheArtIsDrawn()
        {
            var rock = RadarSprites.RenderRock(64);
            int opaque = 0, gray = 0;
            foreach (var p in rock)
            {
                if (p.a < 200) continue;
                opaque++;
                if (Math.Abs(p.r - p.g) < 40 && Math.Abs(p.g - p.b) < 50) gray++;
            }
            Assert.Greater(gray / (float)opaque, 0.8f, "la roca es gris");
            Assert.AreEqual(64 * 64, RadarSprites.RenderAxis(64).Length);
            Assert.AreEqual(128 * 128, RadarSprites.RenderDashedRing(128).Length);
            Assert.AreEqual(256 * 256, RadarSprites.RenderShip(256).Length);
            Assert.AreEqual(256 * 256, RadarSprites.RenderButton(256).Length);
            Assert.AreEqual(256 * 256, RadarSprites.RenderGo(256).Length);
            Assert.AreEqual(256 * 256, RadarSprites.RenderButtonRing(256, true).Length);
            Assert.AreEqual(256 * 256, RadarSprites.RenderPlanet(256).Length);
            var mask = RadarSprites.RenderMask(128, 11);
            Assert.Greater(mask[64 * 128 + 64].a, 200, "la estática es opaca dentro del disco");
            Assert.AreEqual(0, mask[0].a, "y transparente fuera");
            var scope = RadarSprites.RenderScope(128);
            Assert.Greater(scope[64 * 128 + 64].a, 200);
        }
    }
}
