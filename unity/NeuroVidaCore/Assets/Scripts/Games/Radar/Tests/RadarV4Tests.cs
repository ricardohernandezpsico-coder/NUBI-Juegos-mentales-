using System;
using NUnit.Framework;

namespace NeuroVida.Games.Radar.Tests
{
    /// <summary>
    /// Rescate relámpago v4 (Tarea 65, 9-oct): la pantalla de 20:9 con todo el alto (y la de 16:9 igual que en la Tarea 62), la geometría de la zona del destello (radio FIJO en los 12 niveles), la nave con sus viajes a la estación y el reloj del Reto que no corre durante el viaje.
    /// </summary>
    public class RadarV4Tests
    {
        // ------------------------------------------------------------------ la pantalla

        [Test]
        public void On16By9TheLayoutIsTheOneOfTask62()
        {
            var p = new RadarPlan(640f, false);
            Assert.AreEqual(0f, p.T, "16:9 no mezcla nada del layout alto");
            Assert.AreEqual(128f, p.GlassR, 1e-3f, "el radar de 128 dp");
            Assert.AreEqual(141f, p.BezelR, 1e-3f);
            Assert.AreEqual(1f, p.Scale, 1e-4f);
            Assert.AreEqual(261.75f, p.RadarCy, 0.01f);
            Assert.AreEqual(429.4f, p.ShipY, 0.01f);
            Assert.AreEqual(442.7f, p.BoardY0, 0.01f);
            Assert.AreEqual(607.8f, p.GoCy, 0.01f);
            Assert.AreEqual(62f, p.CellH, 1e-3f, "el tablero de 62 dp de alto");
            Assert.AreEqual(8f, p.CellGap, 1e-3f);
            Assert.AreEqual(172f, p.GoW, 1e-3f);
            Assert.AreEqual(44f, p.GoH, 1e-3f);
            Assert.IsFalse(p.ShowShipCount, "en 16:9 no hay lugar para «N de 10 a bordo»");
        }

        [Test]
        public void On20By9TheLayoutIsTheV4Reference()
        {
            var p = new RadarPlan(780f, false);
            Assert.AreEqual(1f, p.T, 1e-5f);
            Assert.AreEqual(180f, p.RadarCx, 1e-3f);
            Assert.AreEqual(290f, p.RadarCy, 1e-2f, "radar centrado en (180, 290)");
            Assert.AreEqual(146f, p.GlassR, 1e-2f, "radio de pantalla de 146 dp");
            Assert.AreEqual(146f * 141f / 128f, p.BezelR, 1e-2f, "el bisel guarda la proporción del arte (141/128)");
            Assert.AreEqual(494f, p.ShipY, 1e-2f, "nave en y 494");
            Assert.AreEqual(1f, p.ShipScale, 1e-4f);
            Assert.AreEqual(552f, p.BoardY0, 1e-2f, "tablero en y 552");
            Assert.AreEqual(740f, p.GoCy, 1e-2f, "«¡Rescatar!» bajo el tablero (172 × 44)");
            Assert.IsTrue(p.ShowShipCount, "«N de 10 a bordo» a +42 de la nave");
            Assert.AreEqual(p.ShipY + 42f, p.ShipCountY, 1e-3f);
            Assert.LessOrEqual(p.GoBox.Y1, 780f, "todo entra en 780 dp");
        }

        [Test]
        public void BetweenThe16By9AndThe20By9TheLayoutInterpolatesWithoutJumps()
        {
            float prevCy = 0f, prevBoard = 0f, prevGlass = 0f;
            for (float h = 640f; h <= 780f; h += 10f)
            {
                var p = new RadarPlan(h, false);
                string at = " (alto " + h + ")";
                Assert.GreaterOrEqual(p.T, 0f);
                Assert.LessOrEqual(p.T, 1f);
                Assert.GreaterOrEqual(p.GlassR, prevGlass - 1e-3f, "el radar nunca se achica al crecer la pantalla" + at);
                Assert.GreaterOrEqual(p.BoardY0, prevBoard, "el tablero baja o se queda" + at);
                Assert.GreaterOrEqual(p.RadarCy, prevCy - 1e-3f, "el radar no sube" + at);
                if (prevGlass > 0f) Assert.Less(p.GlassR - prevGlass, 5f, "sin saltos de más de 5 dp por cada 10 dp de pantalla" + at);
                prevGlass = p.GlassR; prevBoard = p.BoardY0; prevCy = p.RadarCy;
            }
        }

        [Test]
        public void TallerThan780TheExtraSpaceGoesDown_AndTheBoardFollowsTheThumb()
        {
            var a = new RadarPlan(780f, false);
            var b = new RadarPlan(900f, false);
            Assert.AreEqual(a.GlassR, b.GlassR, 1e-3f, "el radar no crece más");
            Assert.Greater(b.BoardY0, a.BoardY0 + 60f, "el tablero baja");
            Assert.Greater(b.GoCy, a.GoCy + 80f);
            Assert.LessOrEqual(b.GoBox.Y1, 900f);
        }

        [Test]
        public void TheGlassIsGreenWaterAndTheSameForEveryLevel()
        {
            // RadarSprites.RenderScope no recibe el nivel: el color y el contraste del radar son iguales siempre
            var a = RadarSprites.RenderScope(128);
            var b = RadarSprites.RenderScope(128);
            for (int i = 0; i < a.Length; i++) Assert.AreEqual(a[i], b[i]);
            var c = a[80 * 128 + 80];
            Assert.AreEqual(255, c.a, "el vidrio es opaco");
            Assert.Greater(c.g, c.r + 20, "el vidrio es verde agua (más verde y azul que rojo)");
            Assert.Greater(c.b, c.r + 20);
        }

        // ------------------------------------------------------------------ la geometría de la zona del destello

        [Test]
        public void TheGeometryScalesWithTheRadar_AndTheSketchNumbersAre112And72And58At146()
        {
            var g = RadarGeometry.For(146f);
            Assert.AreEqual(112f, g.SpawnRadius, 1e-3f, "radio de la zona = RR − 34");
            Assert.AreEqual(72f, g.MinSeparation, 1e-3f);
            Assert.AreEqual(58f, g.CapsuleSize, 1e-3f);
            var r = RadarGeometry.Reference;
            Assert.AreEqual(94f, r.SpawnRadius, 1e-3f);
            Assert.AreEqual(66f, r.MinSeparation, 1e-3f);
            Assert.AreEqual(55f, r.CapsuleSize, 1e-3f);
        }

        [Test]
        public void TheZoneRadiusIsFixedAtEveryLevel_ForEveryScreenSize()
        {
            foreach (float glass in new[] { 120f, 128f, 137f, 146f })
            {
                var geo = RadarGeometry.For(glass);
                float limit = geo.SpawnRadius + 1.3f;
                for (int level = 1; level <= RadarContract.MaxLevel; level++)
                {
                    float far = 0f;
                    for (int seed = 0; seed < 200; seed++)
                    {
                        var round = RadarContract.NextRound(level, new Random(seed * 31 + level), geo);
                        foreach (var o in round.Capsules) far = Math.Max(far, (float)Math.Sqrt(o.X * o.X + o.Y * o.Y));
                        foreach (var o in round.Rocks) far = Math.Max(far, (float)Math.Sqrt(o.X * o.X + o.Y * o.Y));
                    }
                    Assert.LessOrEqual(far, limit, "radio de la zona con el radar de " + glass + " dp y el nivel " + level + ": nunca crece con el nivel");
                    Assert.Greater(far, geo.SpawnRadius * 0.6f, "y se usa toda la zona, no se achica con el nivel (radar " + glass + ", nivel " + level + ")");
                }
            }
        }

        [Test]
        public void SixObjectsAlwaysFitWithTheTallRadar_With10000Seeds()
        {
            foreach (float glass in new[] { 146f, 137f })
            {
                var geo = RadarGeometry.For(glass);
                for (int seed = 0; seed < 10000; seed++)
                {
                    var pts = RadarContract.Place(6, new Random(seed), geo);
                    Assert.AreEqual(6, pts.Count, "seis lugares con la semilla " + seed);
                    for (int i = 0; i < pts.Count; i++)
                    {
                        Assert.LessOrEqual(Math.Sqrt(pts[i].x * pts[i].x + pts[i].y * pts[i].y), geo.SpawnRadius + 1.3f, "dentro de la zona (semilla " + seed + ")");
                        for (int j = i + 1; j < pts.Count; j++)
                        {
                            float dx = pts[i].x - pts[j].x, dy = pts[i].y - pts[j].y;
                            Assert.GreaterOrEqual(Math.Sqrt(dx * dx + dy * dy), geo.MinSeparation - 3.5f, "separados (semilla " + seed + ")");
                        }
                    }
                }
            }
        }

        // ------------------------------------------------------------------ la nave y sus viajes

        [Test]
        public void TheTripHappensExactlyWhenTenAreAboardAfterAReveal()
        {
            for (int onBoard = 0; onBoard <= 9; onBoard++) Assert.IsFalse(RadarCargo.TripDue(onBoard), onBoard + " a bordo: todavía no");
            Assert.IsTrue(RadarCargo.TripDue(10), "con 10 a bordo, viaja");
            Assert.IsTrue(RadarCargo.TripDue(13), "con más de 10 (entraron varias a la vez), también");
            Assert.AreEqual(2.0f, RadarCargo.TripSeconds, 1e-6f);
            Assert.AreEqual(10, RadarCargo.Capacity);
        }

        [Test]
        public void TheCapsulesThatDidNotFitMoveToTheNewShip()
        {
            // la nave llevaba 9 y entraron 4: son 13 rescatadas, 10 viajan y 3 pasan a la nave nueva
            int rescued = 13, trips = 0;
            int onBoard = RadarCargo.OnBoard(rescued, trips);
            Assert.AreEqual(13, onBoard);
            Assert.AreEqual(10, RadarCargo.Shown(onBoard), "nunca más de 10 ventanas");
            Assert.AreEqual(3, RadarCargo.CarriedOver(onBoard));
            trips++;
            Assert.AreEqual(3, RadarCargo.OnBoard(rescued, trips), "al volver, la nave nueva trae las 3");
            Assert.AreEqual(3, RadarCargo.Shown(RadarCargo.OnBoard(rescued, trips)));
            // 7 rescatadas más: 10 a bordo otra vez y un segundo viaje
            rescued += 7;
            Assert.IsTrue(RadarCargo.TripDue(RadarCargo.OnBoard(rescued, trips)));
            Assert.AreEqual(0, RadarCargo.OnBoard(rescued, trips + 1));
            Assert.AreEqual(0, RadarCargo.OnBoard(0, 0));
            Assert.AreEqual(0, RadarCargo.CarriedOver(7));
        }

        [Test]
        public void TheRetoDoesNotLoseTimeDuringTheTrip()
        {
            var clock = new RetoClock(100f, 120f);
            Assert.AreEqual(120f, clock.Remaining(100f), 1e-4f);
            Assert.AreEqual(100f, clock.Remaining(120f), 1e-4f);
            // un viaje de 2 s: el reloj del juego avanza 2 s y el final se corre 2 s
            float now = 120f;
            clock.Shift(RadarCargo.TripSeconds);
            now += RadarCargo.TripSeconds;
            Assert.AreEqual(100f, clock.Remaining(now), 1e-4f, "después del viaje queda lo mismo que antes");
            Assert.IsFalse(clock.Finished(now));
            Assert.IsTrue(clock.Finished(now + 100.01f));
            // sin viaje el tiempo sí corre
            var plain = new RetoClock(0f, 10f);
            Assert.IsTrue(plain.Finished(10f));
            Assert.IsFalse(plain.Finished(9.99f));
        }

        [Test]
        public void TheRunCountsTheTrips()
        {
            var run = new RadarRun();
            Assert.AreEqual(0, run.Trips);
            run.AddTrip();
            run.AddTrip();
            Assert.AreEqual(2, run.Trips);
        }

        [Test]
        public void TheWorldArtIsDrawn_StationDebrisWindowsAndTheNewShip()
        {
            var ship = RadarSprites.RenderShip(256);
            Assert.AreEqual(256 * 256, ship.Length);
            Assert.Greater(ship[128 * 256 + 128].a, 200, "el casco es opaco al centro");
            Assert.Greater(RadarSprites.ShipSide, 200f, "la nave mide más de 200 dp");
            var station = RadarSprites.RenderStation(192);
            Assert.AreEqual(192 * 192, station.Length);
            int opaque = 0;
            foreach (var p in station) if (p.a > 200) opaque++;
            Assert.Greater(opaque, 192 * 192 / 20, "la estación se dibuja");
            var debris = RadarSprites.RenderDebris(64);
            Assert.Greater(debris[32 * 64 + 32].a, 200);
            Assert.AreEqual(0, debris[0].a);
            var win = RadarSprites.RenderWindow(64);
            Assert.Greater(win[32 * 64 + 32].a, 200);
            Assert.GreaterOrEqual(RadarSprites.WindowDisc, 14f, "las ventanas miden al menos 14 dp");
        }

        [Test]
        public void TheTripAndTheFinalTextsAreTheDesignOnes()
        {
            Assert.AreEqual("¡Nave llena! Viaje a la estación", RadarContract.TripNotice);
            Assert.AreEqual("Viajes a la estación", RadarContract.EndTrips);
            Assert.AreEqual("3", RadarContract.TripsValue(3));
            foreach (var t in new[] { RadarContract.TripNotice, RadarContract.EndTrips })
                foreach (var bad in new[] { "cognitiv", "entrenamiento", "cerebro" }) Assert.IsFalse(t.ToLowerInvariant().Contains(bad), t);
        }
    }
}
