using NUnit.Framework;
using UnityEngine;

namespace NeuroVida.Games.Satelites.Tests
{
    /// <summary>Lo que suma una partida (luces, rondas perfectas, rachas, «Tu seguimiento»), la disposición de la pantalla en tres formas de teléfono y el arte y el sonido que se hornean.</summary>
    public class SatelliteRunTests
    {
        [Test]
        public void EveryDeliveredMessageIsALight_AndAPerfectRoundIsAllOfThem()
        {
            var run = new SatelliteRun();
            Assert.IsTrue(run.Add(3, 3, 7, 3));
            Assert.IsFalse(run.Add(2, 3, 7, 3));
            Assert.IsTrue(run.Add(4, 4, 8, 5));
            Assert.AreEqual(9, run.Lights);
            Assert.AreEqual(10, run.TargetsTotal);
            Assert.AreEqual(2, run.Perfect);
            Assert.AreEqual(3, run.RoundsPlayed);
            Assert.AreEqual(0.9f, run.HitRate, 1e-5f);
        }

        [Test]
        public void TheStreakCountsPerfectRoundsInARow_AndTheBestIsKept()
        {
            var run = new SatelliteRun();
            run.Add(3, 3, 7, 3);
            run.Add(3, 3, 7, 3);
            Assert.AreEqual(2, run.Streak);
            run.Add(1, 3, 7, 3);
            Assert.AreEqual(0, run.Streak);
            run.Add(3, 3, 7, 3);
            Assert.AreEqual(1, run.Streak);
            Assert.AreEqual(2, run.BestStreak);
        }

        [Test]
        public void HitsAreClampedToTheMessagesOfTheRound()
        {
            var run = new SatelliteRun();
            run.Add(7, 3, 7, 3);
            run.Add(-2, 3, 7, 3);
            Assert.AreEqual(3, run.Lights);
        }

        [Test]
        public void YourTrackingIsTheLuckCorrectedAverage_AndTheSpeedIsTheHighestClearedLevel()
        {
            var run = new SatelliteRun();
            Assert.AreEqual(-1f, run.Capacity);
            Assert.AreEqual(-1f, run.MeanTargets);
            Assert.AreEqual(-1f, run.SpeedReached);
            run.Add(3, 3, 7, 4);
            run.Add(4, 4, 8, 6);
            run.Add(1, 4, 8, 8);
            Assert.AreEqual(6, run.BestCleared, "el nivel más alto superado completo");
            Assert.AreEqual(SatelliteContract.SpeedFactor(6), run.SpeedReached, 1e-5f);
            Assert.AreEqual(11f / 3f, run.MeanTargets, 1e-4f);
            Assert.Less(run.Capacity, run.MeanTargets, "descuenta la suerte");
            Assert.AreEqual(SatelliteContract.Capacity(run.Rounds), run.Capacity, 1e-6f);
            Assert.AreEqual(SatelliteContract.Score(run.HitRate, 6), run.Score);
        }

        // ------------------------------------------------------------------ la pantalla (docs §8)

        private static readonly float[] Heights = { 592f, 640f, 700f, 762f, 800f, 900f };

        [Test]
        public void EverythingFitsInThreePhoneShapes_AndNothingOverlaps()
        {
            foreach (float h in Heights)
            {
                foreach (bool guided in new[] { false, true })
                {
                    var p = new ScreenPlan(h, guided);
                    string at = " (alto " + h + (guided ? ", tutorial" : "") + ")";
                    Assert.Greater(p.TimerY, ScreenPlan.HudBottom, "la barra del Reto va bajo el marcador" + at);
                    Assert.Greater(p.PromptAY - 14f, p.TimerY, "la instrucción va bajo la barra" + at);
                    Assert.Greater(p.PromptBY - 11f, p.PromptAY + 14f, "las dos líneas no se tocan" + at);
                    Assert.Greater(p.BandTop, p.PromptBY + 11f, "el campo va bajo la instrucción" + at);
                    Assert.Greater(p.BandBottom, p.BandTop + 250f, "al campo le queda alto para los anillos" + at);
                    if (!guided)
                    {
                        Assert.Greater(p.DiscsY - 10f, p.BandBottom, "los discos de rondas van bajo el campo" + at);
                        Assert.Greater(p.LightsY - 11f, p.DiscsY + 10f, "y «Luces encendidas» bajo los discos" + at);
                        Assert.LessOrEqual(p.LightsY + 11f, h, "todo cabe" + at);
                    }
                    else Assert.LessOrEqual(p.BandBottom, h - ScreenPlan.TutorialControls, "el campo no pisa los controles del tutorial" + at);
                    var o = p.Orbits;
                    float reach = o.Rx[2] * (1f + SatelliteContract.LaneSpread) * o.RyK + SatelliteContract.SatRadius;
                    Assert.LessOrEqual(p.Cy + reach, p.BandBottom + 0.5f, "los anillos caben abajo" + at);
                    Assert.GreaterOrEqual(p.Cy - reach - SatelliteContract.EnvelopeRise, p.BandTop - 0.5f, "y arriba, con el sobre de la señal incluido" + at);
                }
            }
        }

        [Test]
        public void ATallPhoneUsesTheWholeHeight_NotAThirdOfItEmpty()
        {
            var tall = new ScreenPlan(762f, false);
            var o = tall.Orbits;
            float used = 2f * (o.Rx[2] * (1f + SatelliteContract.LaneSpread) * o.RyK + SatelliteContract.SatRadius);
            float given = tall.BandBottom - tall.BandTop;
            Assert.Greater(used / given, 0.75f, "los anillos ocupan casi todo el campo (" + used + " de " + given + ")");
        }

        // ------------------------------------------------------------------ el arte y el sonido

        [Test]
        public void TheSpritesAreDrawnAndTransparentAtTheCorners()
        {
            foreach (var (name, px, size) in new[]
            {
                ("satélite", SatelliteSprites.RenderBody(64), 64), ("planeta", SatelliteSprites.RenderPlanet(64), 64), ("sobre", SatelliteSprites.RenderEnvelope(48), 48),
                ("nube", SatelliteSprites.RenderCloud(64), 64), ("aro punteado", SatelliteSprites.RenderDashed(64), 64), ("medio disco", SatelliteSprites.RenderHalf(48), 48),
            })
            {
                Assert.AreEqual(size * size, px.Length, name);
                Assert.AreEqual(0, px[0].a, name + ": la esquina es transparente");
                int opaque = 0;
                foreach (var c in px) if (c.a > 200) opaque++;
                Assert.Greater(opaque, size * size / 40, name + ": se dibuja algo");
            }
            Assert.AreEqual(255, SatelliteSprites.RenderPlanet(64)[32 * 64 + 32].a, "el planeta es opaco al centro");
            Assert.Greater(SatelliteSprites.RenderCloud(64)[32 * 64 + 32].a, 200, "la nube es opaca al centro");
        }

        [Test]
        public void TheHalfDiscIsTheLeftHalf()
        {
            var px = SatelliteSprites.RenderHalf(48);
            Assert.Greater(px[24 * 48 + 12].a, 200, "a la izquierda hay disco");
            Assert.AreEqual(0, px[24 * 48 + 36].a, "a la derecha no");
        }

        [Test]
        public void EverySoundIsSynthesizedOnce_AndNothingIsLoud()
        {
            var warm = SatelliteSounds.Prewarm();
            while (warm.MoveNext()) { }
            Assert.GreaterOrEqual(SatelliteSounds.CachedCount, 20);
            int before = SatelliteSounds.CachedCount;
            Assert.AreSame(SatelliteSounds.Perfect(), SatelliteSounds.Perfect(), "un clip por sonido");
            Assert.AreEqual(before, SatelliteSounds.CachedCount);
            foreach (var clip in new[] { SatelliteSounds.CueBell(0), SatelliteSounds.Stop(), SatelliteSounds.Mark(3), SatelliteSounds.Perfect(), SatelliteSounds.Partial(), SatelliteSounds.LightOn(2), SatelliteSounds.Finale(), SatelliteSounds.Hum() })
            {
                var data = new float[clip.samples];
                clip.GetData(data, 0);
                float peak = 0f;
                foreach (float s in data) peak = Mathf.Max(peak, Mathf.Abs(s));
                Assert.Greater(peak, 0.05f, clip.name + " suena");
                Assert.LessOrEqual(peak, 0.5001f, clip.name + " no pasa de 0,5");
            }
            Assert.AreEqual(44100, SatelliteSounds.Hum().samples, "el zumbido dura un segundo exacto: se repite sin chasquido");
        }
    }
}
