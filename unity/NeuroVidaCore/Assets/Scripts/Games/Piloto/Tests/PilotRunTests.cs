using System;
using NUnit.Framework;
using NeuroVida.Contracts;
using UnityEngine;

namespace NeuroVida.Games.Piloto.Tests
{
    /// <summary>Lo que suma un vuelo (ventanas de 1,5 s, señales, racha, hiperimpulso), la disposición de la pantalla en cuatro formas de teléfono, los dos motores comunes, la telemetría y el arte y el sonido que se hornean.</summary>
    public class PilotRunTests
    {
        private static void Fly(PilotRun run, float seconds, bool inside)
        {
            for (float t = 0f; t < seconds - 1e-4f; t += 0.05f) run.Fly(0.05f, inside, out _);
        }

        [Test]
        public void AWindowIsOneAndAHalfSecondsAndPassesWithEightyFivePercentInside()
        {
            var run = new PilotRun();
            bool closed = false, passed = false;
            for (int i = 0; i < 31; i++) if (run.Fly(0.05f, true, out bool p)) { closed = true; passed = p; }
            Assert.IsTrue(closed && passed);
            Assert.AreEqual(1, run.Windows);
            Assert.AreEqual(1, run.CleanWindows);
            // 20 de 31 cuadros dentro: 65 % → falla y corta la serie de ventanas limpias
            for (int i = 0; i < 31; i++) run.Fly(0.05f, i < 20, out _);
            Assert.AreEqual(2, run.Windows);
            Assert.AreEqual(1, run.WindowsPassed);
            Assert.AreEqual(0, run.CleanWindows);
        }

        [Test]
        public void TheLaneFractionIsTimeInsideOverFlightTime()
        {
            var run = new PilotRun();
            Fly(run, 3f, true);
            Fly(run, 1f, false);
            Assert.AreEqual(0.75f, run.InLaneFraction, 0.02f);
        }

        [Test]
        public void CatchingAMissionSignalScoresTenAndDoubleInHyperdrive()
        {
            var run = new PilotRun();
            Assert.AreEqual(10, run.Catch(false));
            Assert.AreEqual(20, run.Catch(true));
            Assert.AreEqual(30, run.Points);
            Assert.AreEqual(2, run.Hits);
            Assert.AreEqual(2, run.Targets);
        }

        [Test]
        public void AMissionSignalThatLeavesIsAnOmission_AWrongTapIsAFalseAlarm_BothCutTheStreak()
        {
            var run = new PilotRun();
            run.Catch(false); run.Catch(false);
            Assert.AreEqual(2, run.Streak);
            run.Miss();
            Assert.AreEqual(0, run.Streak);
            Assert.AreEqual(3, run.Targets);
            Assert.AreEqual(2, run.Hits);
            run.Catch(false);
            run.FalseAlarm();
            Assert.AreEqual(0, run.Streak);
            Assert.AreEqual(1, run.FalseAlarms);
            Assert.AreEqual(1, run.NonTargets);
            Assert.AreEqual(2, run.BestStreak);
        }

        [Test]
        public void ALetPassedSignalThatWasNotTheMissionIsASilentHit_ForTheStreak()
        {
            var run = new PilotRun();
            for (int i = 0; i < 4; i++) run.Ignore();
            Assert.AreEqual(4, run.Streak);
            Assert.AreEqual(0, run.Hits);
            Assert.AreEqual(0, run.Targets);
            Assert.AreEqual(4, run.NonTargets);
            Assert.AreEqual(4, run.Resolved);
        }

        [Test]
        public void HyperdriveNeedsFiveInARowAndThreeCleanWindows_OnlyOneAtATime()
        {
            var run = new PilotRun();
            for (int i = 0; i < 5; i++) run.Ignore();
            Assert.IsFalse(run.ShouldHyper(false), "faltan ventanas limpias");
            Fly(run, 5f, true);                                   // tres ventanas de 1,5 s
            Assert.GreaterOrEqual(run.CleanWindows, 3);
            Assert.IsTrue(run.ShouldHyper(false));
            Assert.IsFalse(run.ShouldHyper(true), "ya hay uno en curso");
            run.Ignore();
            Assert.IsFalse(run.ShouldHyper(false), "a la sexta ya no toca");
            for (int i = 0; i < 4; i++) run.Ignore();
            Assert.IsTrue(run.ShouldHyper(false), "a la décima vuelve a tocar");
            run.CountHyper();
            Assert.AreEqual(1, run.HyperCount);
        }

        [Test]
        public void TheRunScoreUsesTheMeasureOnlyWithEnoughMissionSignals()
        {
            var run = new PilotRun();
            Fly(run, 3f, true);
            for (int i = 0; i < 6; i++) run.Catch(false);
            Assert.IsNull(run.SignalScore);
            Assert.AreEqual(100, run.Score, "sin medida de señales cuenta solo la ruta");
            run.Catch(false); run.Catch(false);
            run.FalseAlarm();
            Assert.AreEqual((8 - 1) / 8f, run.SignalScore.Value, 1e-5f);
        }

        // ------------------------------------------------------------------ la pantalla

        [Test]
        public void TheScreenFitsInFourPhoneShapes_WithAFingerStripOfAtLeast100Dp()
        {
            foreach (float h in new[] { 640f, 720f, 800f, 900f })
                foreach (bool guided in new[] { false, true })
                {
                    var p = new PilotPlan(h, guided);
                    string at = " (alto " + h + ", guiada " + guided + ")";
                    Assert.GreaterOrEqual(p.StripBottom - p.StripVisualTop, 100f - 1e-3f, "franja" + at);
                    Assert.GreaterOrEqual(p.SkyBottom - p.SkyTop, 100f, "cielo de señales" + at);
                    Assert.Greater(p.ShipY, p.SkyBottom + 40f, "nave bajo el cielo" + at);
                    Assert.Less(p.ShipY, p.StripTop - 30f, "nave sobre la franja" + at);
                    Assert.LessOrEqual(p.StripBottom, guided ? h - PilotPlan.TutorialControls : h, "franja dentro" + at);
                    Assert.Greater(p.MissionTop, PilotPlan.HudBottom, "tarjeta de misión bajo el marcador" + at);
                    Assert.Greater(p.RouteTop, p.MissionBottom, "la ruta no pasa bajo la tarjeta" + at);
                }
        }

        [Test]
        public void TheNoticeZoneSitsBetweenTheMissionCardAndTheSky()
        {
            var p = new PilotPlan(800f, false);
            var b = p.BannerBox;
            Assert.Greater(b.Y0, p.MissionBottom);
            Assert.Less(b.Y1, p.SkyBottom);
            Assert.Greater(b.X0, 0f);
            Assert.Less(b.X1, PilotContract.FieldWidth);
        }

        [Test]
        public void TheMissionNoticeHasRoomForTheTitleTheDrawnShapeAndTheSector_AndNeverReachesTheShip()
        {
            Assert.GreaterOrEqual(PilotPlan.NoticeShapeDp, 48f, "la forma se dibuja a 48 dp o más");
            // título (26 dp desde 3) + forma (52 dp, centrada a 56) + sector (20 dp, centrado a 98): todo cabe en el alto
            Assert.GreaterOrEqual(PilotPlan.NoticeHeight, 4f + 26f + PilotPlan.NoticeShapeDp + 20f);
            foreach (float h in new[] { 640f, 720f, 800f, 900f })
            {
                var p = new PilotPlan(h, false);
                var n = p.NoticeBox;
                string at = " (alto " + h + ")";
                Assert.Greater(n.Y0, p.MissionBottom, "bajo la tarjeta de misión" + at);
                Assert.AreEqual(p.BannerTop, n.Y0, 1e-3f, "mismo lugar de arriba que el aviso corto" + at);
                Assert.Greater(n.Y1 - n.Y0, 100f, "alto para tres renglones y la forma" + at);
                Assert.Less(n.Y1, p.ShipY - 30f, "el aviso nunca llega a la nave" + at);
                Assert.Less(n.Y1, p.SkyBottom, "queda sobre el final del cielo" + at);
                Assert.GreaterOrEqual(n.X0, 0f); Assert.LessOrEqual(n.X1, PilotContract.FieldWidth);
                Assert.LessOrEqual(n.X0, p.BannerBox.X0, "contiene al aviso corto" + at);
                Assert.GreaterOrEqual(n.X1, p.BannerBox.X1);
                Assert.GreaterOrEqual(n.Y1, p.BannerBox.Y1);
                Assert.GreaterOrEqual(p.SkyBottom - p.SkyTop, 100f, "el cielo de señales no se achica por el aviso nuevo" + at);
            }
        }

        [Test]
        public void TheMissionNoticeTextsAreTheSketchOnes_AndTheFirstSectorOnlyAnnouncesTheMission()
        {
            Assert.AreEqual("¡Tu misión!", PilotContract.MissionNoticeTitle(0));
            Assert.AreEqual("¡Nueva misión!", PilotContract.MissionNoticeTitle(1));
            Assert.AreEqual("¡Nueva misión!", PilotContract.MissionNoticeTitle(2));
            Assert.AreEqual("Sector 2 · Cinturón de hielo", PilotContract.MissionNoticeFoot(1));
            Assert.AreEqual("círculo con punto", PilotContract.MissionName(new PilotMission(SignalShape.Circle, SignalDetail.Dot)));
            foreach (var t in new[] { PilotContract.MissionNoticeTitle(0), PilotContract.MissionNoticeTitle(1), PilotContract.MissionNoticeFoot(0), PilotContract.MissionNoticeFoot(2) })
                foreach (var bad in new[] { "cognitiv", "entrenamiento", "cerebro" })
                    Assert.IsFalse(t.ToLowerInvariant().Contains(bad), t);
        }

        [Test]
        public void TheMissionCardPulsesTwiceInAboutEightTenthsOfASecond()
        {
            Assert.AreEqual(0.8f, PilotContract.PulseBeats * PilotContract.PulseBeatSeconds, 1e-5f);
            Assert.AreEqual(1f, PilotContract.MissionPulseScale(-0.1f), 1e-5f, "antes del cambio, en reposo");
            Assert.AreEqual(1f, PilotContract.MissionPulseScale(0f), 1e-4f);
            Assert.AreEqual(1.06f, PilotContract.MissionPulseScale(0.2f), 1e-4f, "primer latido: 1,06 a la mitad");
            Assert.AreEqual(1f, PilotContract.MissionPulseScale(0.4f), 1e-4f, "entre un latido y otro vuelve a 1");
            Assert.AreEqual(1.06f, PilotContract.MissionPulseScale(0.6f), 1e-4f, "segundo latido");
            Assert.LessOrEqual(PilotPlan.MissionCardWidth * PilotContract.PulsePeak, PilotPlan.Width - 8f, "con el latido la tarjeta queda a más de 4 dp de cada borde de la pantalla");
            Assert.AreEqual(1f, PilotContract.MissionPulseScale(0.8f), 1e-5f);
            Assert.AreEqual(1f, PilotContract.MissionPulseScale(5f), 1e-5f, "después, en reposo");
            for (float t = 0f; t < 0.8f; t += 0.01f)
            {
                float k = PilotContract.MissionPulseScale(t);
                Assert.GreaterOrEqual(k, 1f - 1e-5f);
                Assert.LessOrEqual(k, 1.06f + 1e-5f);
            }
        }

        // ------------------------------------------------------------------ los dos motores comunes

        private static SequenceConfigDetails Config(int stage = 5) => new SequenceConfigDetails { age_band = "ADULT", pil_stage = stage, level = 3, timed = true };

        [SetUp]
        public void NoModeLimits() => AdaptiveDifficulty.ConfigureMode(null);

        [Test]
        public void TheDriveEngineAimsAtEightyFivePercent_AndTheSignalEngineAtEighty()
        {
            var drive = PilotMetrics.CreateDriveEngine(Config());
            var signal = PilotMetrics.CreateSignalEngine(Config());
            Assert.AreEqual(0.85f, drive.TargetAccuracy, 1e-5f);
            Assert.AreEqual(0.80f, signal.TargetAccuracy, 1e-5f);
            Assert.AreEqual(5, drive.Level);
            Assert.AreEqual(5, signal.Level);
        }

        [Test]
        public void EachEngineStepsAsInTheSketch()
        {
            var drive = PilotMetrics.CreateDriveEngine(Config());
            for (int i = 0; i < 7; i++) drive.Register(true);
            float before = drive.Rating;
            drive.Register(true);
            Assert.AreEqual(0.15f, drive.Rating - before, 1e-4f, "ventana limpia: +0,15");
            before = drive.Rating;
            drive.Register(false);
            Assert.AreEqual(-0.85f, drive.Rating - before, 1e-4f, "ventana fallada: −0,85");

            var signal = PilotMetrics.CreateSignalEngine(Config());
            for (int i = 0; i < 7; i++) signal.Register(true);
            before = signal.Rating;
            signal.Register(true);
            Assert.AreEqual(0.25f, signal.Rating - before, 1e-4f, "señal bien resuelta: +0,25");
            before = signal.Rating;
            signal.Register(false);
            Assert.AreEqual(-1f, signal.Rating - before, 1e-4f, "señal fallada: −1");
        }

        [Test]
        public void TheDebugStageSetsBothEngines()
        {
            Assert.AreEqual(3f, PilotMetrics.StartRating(Config(3)), 1e-5f);
            Assert.AreEqual(9f, PilotMetrics.StartRating(Config(99)), 1e-5f);
            Assert.AreEqual(3, PilotMetrics.CreateSignalEngine(Config(3)).Level);
            Assert.AreEqual(3, PilotMetrics.CreateDriveEngine(Config(3)).Level);
        }

        [Test]
        public void TheTelemetryReportsEverythingMeasuredWithBothTasks()
        {
            var drive = PilotMetrics.CreateDriveEngine(Config());
            var signal = PilotMetrics.CreateSignalEngine(Config());
            var run = new PilotRun();
            Fly(run, 6f, true);
            for (int i = 0; i < 9; i++) { run.Catch(false); signal.Register(true); }
            run.FalseAlarm(); signal.Register(false);
            run.Miss();
            drive.Register(true); drive.Register(false);
            var m = PilotMetrics.Build(drive, signal, run, Config());
            Assert.AreEqual(run.Score, m.calculated_score);
            Assert.AreEqual(100, m.pil_lane_pct);
            Assert.AreEqual(9, m.pil_hits);
            Assert.AreEqual(10, m.pil_targets);
            Assert.AreEqual(1, m.pil_false);
            Assert.AreEqual((int)Math.Round((9 - 1) / 10f * 100f), m.pil_signal_pct);
            Assert.AreEqual(signal.Level, m.pil_signal_level);
            Assert.AreEqual(drive.Level, m.pil_drive_level);
            Assert.AreEqual(90, m.pil_points);
            Assert.AreEqual(9, m.pil_best_streak);
            Assert.AreEqual(Math.Max(drive.PeakLevel, signal.PeakLevel), m.peak_level);
            Assert.AreEqual((drive.RatingNormalized + signal.RatingNormalized) * 0.5f, m.end_rating, 1e-5f);
            Assert.AreEqual(run.WindowsPassed + run.Hits + (run.NonTargets - run.FalseAlarms), m.correct_trials);
            Assert.AreEqual(run.Windows + run.Resolved, m.total_trials);
            // sin 8 señales de la misión no hay proporción
            var few = new PilotRun();
            few.Catch(false);
            Assert.AreEqual(-1, PilotMetrics.Build(drive, signal, few, Config()).pil_signal_pct);
        }

        // ------------------------------------------------------------------ el arte y el sonido

        [Test]
        public void FifteenSignalsAreDrawn_AllDifferent_AndLookalikesDifferOnlyByTheirDetail()
        {
            const int px = 64;
            var all = new System.Collections.Generic.List<(string name, Color32[] pixels, SignalShape shape)>();
            foreach (SignalShape shape in Enum.GetValues(typeof(SignalShape)))
                foreach (SignalDetail detail in Enum.GetValues(typeof(SignalDetail)))
                {
                    var pixels = PilotSignalSprites.Render(shape, detail, px);
                    Assert.AreEqual(px * px, pixels.Length);
                    Assert.Greater(pixels[(px / 2) * px + px / 2].a, 200, "opaca al centro: " + shape + " " + detail);
                    all.Add((shape + " " + detail, pixels, shape));
                }
            Assert.AreEqual(15, all.Count);
            for (int i = 0; i < all.Count; i++)
                for (int j = i + 1; j < all.Count; j++)
                {
                    int diff = 0;
                    for (int k = 0; k < px * px; k++) if (Math.Abs(all[i].pixels[k].r - all[j].pixels[k].r) + Math.Abs(all[i].pixels[k].g - all[j].pixels[k].g) + Math.Abs(all[i].pixels[k].b - all[j].pixels[k].b) + Math.Abs(all[i].pixels[k].a - all[j].pixels[k].a) > 60) diff++;
                    Assert.Greater(diff, px * px / 40, all[i].name + " y " + all[j].name + " casi no se distinguen");
                }
            // el color de cada forma es el suyo en las tres versiones (el detalle es lo único que cambia)
            foreach (SignalShape shape in Enum.GetValues(typeof(SignalShape)))
            {
                var c = PilotSignalSprites.ShapeColors[(int)shape];
                foreach (var entry in all)
                {
                    if (entry.shape != shape) continue;
                    int opaque = 0, close = 0;
                    foreach (var p in entry.pixels)
                    {
                        if (p.a < 200) continue;
                        opaque++;
                        if (Math.Abs(p.r - c.r * 255f) < 14f && Math.Abs(p.g - c.g * 255f) < 14f && Math.Abs(p.b - c.b * 255f) < 14f) close++;
                    }
                    Assert.Greater(close / (float)opaque, 0.25f, entry.name + ": el color de la forma domina");
                }
            }
        }

        [Test]
        public void TheSoundsAreSynthesizedOnceAndNothingIsLoud()
        {
            var warm = PilotSounds.Prewarm();
            while (warm.MoveNext()) { }
            Assert.GreaterOrEqual(PilotSounds.CachedCount, 14);
            int before = PilotSounds.CachedCount;
            Assert.AreSame(PilotSounds.Engine(), PilotSounds.Engine(), "un clip por sonido");
            Assert.AreEqual(before, PilotSounds.CachedCount);
            foreach (var clip in new[] { PilotSounds.Engine(), PilotSounds.Beacon(2), PilotSounds.Buzz(), PilotSounds.Blip(), PilotSounds.Catch(3), PilotSounds.Thud(), PilotSounds.Whoosh(), PilotSounds.MissionChange(), PilotSounds.Hyper(), PilotSounds.Finale() })
            {
                var data = new float[clip.samples * clip.channels];
                clip.GetData(data, 0);
                float peak = 0f;
                foreach (float v in data) peak = Math.Max(peak, Math.Abs(v));
                Assert.Greater(peak, 0.02f, clip.name + " suena");
                Assert.LessOrEqual(peak, 0.5001f, clip.name + " no pasa de 0,5");
            }
        }

        [Test]
        public void TheEngineLoopHasNoJumpAtTheSeam()
        {
            var clip = PilotSounds.Engine();
            var data = new float[clip.samples];
            clip.GetData(data, 0);
            Assert.AreEqual(44100, data.Length, "un segundo exacto");
            Assert.Less(Math.Abs(data[0] - data[data.Length - 1]), 0.06f, "el final empalma con el principio");
        }
    }
}
