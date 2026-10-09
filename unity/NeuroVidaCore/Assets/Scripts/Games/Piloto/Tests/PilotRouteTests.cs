using System;
using System.Collections.Generic;
using NUnit.Framework;

namespace NeuroVida.Games.Piloto.Tests
{
    /// <summary>La ruta de balizas (una cada 64 dp, cada una con su centro y su ancho) y el lugar donde nacen las señales (pedido de Ricardo: nunca bajo un aviso).</summary>
    public class PilotRouteTests
    {
        [Test]
        public void ABeaconEvery64Dp_AndEachKeepsItsOwnCenterAndWidth()
        {
            var route = new PilotRoute(3);
            route.Extend(1000f, 3);
            var b = route.Beacons;
            Assert.GreaterOrEqual(b.Count, 15);
            for (int i = 1; i < b.Count; i++) Assert.AreEqual(64f, b[i].P - b[i - 1].P, 1e-3f);
            foreach (var beacon in b)
            {
                Assert.GreaterOrEqual(beacon.Cx - beacon.Half, PilotContract.RouteEdgeMargin - 1e-3f);
                Assert.LessOrEqual(beacon.Cx + beacon.Half, PilotContract.FieldWidth - PilotContract.RouteEdgeMargin + 1e-3f);
            }
        }

        [Test]
        public void TheRouteIsContinuousBetweenBeacons()
        {
            var route = new PilotRoute(9);
            route.Extend(3000f, 9);
            float last = -1f, worst = 0f;
            for (float p = 0f; p < 2900f; p += 2f)
            {
                route.At(p, out float c, out float h);
                if (last >= 0f) worst = Math.Max(worst, Math.Abs(c - last));
                last = c;
                Assert.Greater(h, 40f);
            }
            Assert.Less(worst, 8f, "salto máximo de la línea central en 2 dp");
        }

        [Test]
        public void ALevelChangeNeverDeformsWhatIsAlreadyVisible()
        {
            var route = new PilotRoute(3);
            route.Extend(2000f, 3);
            route.At(500f, out float c1, out float h1);
            route.Extend(5000f, 9);                        // el nivel de pilotaje sube a 9
            route.At(500f, out float c2, out float h2);
            Assert.AreEqual(c1, c2, 1e-4f);
            Assert.AreEqual(h1, h2, 1e-4f);
            route.At(4900f, out _, out float far);
            Assert.Less(far, PilotContract.HalfWidth(3), "el ancho se acerca despacio al del nivel nuevo");
            Assert.GreaterOrEqual(far, PilotContract.HalfWidth(9) - 1f);
        }

        [Test]
        public void TheBeaconsLeftBehindAreDiscarded()
        {
            var route = new PilotRoute(1);
            route.Extend(5000f, 1);
            route.Prune(4000f);
            Assert.Less(route.Beacons.Count, 25);
            route.At(4500f, out float c, out float h);
            Assert.Greater(h, 0f);
            Assert.GreaterOrEqual(c, 0f);
        }
    }

    public class PilotSpawnTests
    {
        private static readonly PilotPlan Plan = new PilotPlan(800f, false);

        private static (float center, float half) Straight(float y) => (180f, 80f);

        [Test]
        public void ASignalNeverBornUnderANotice()
        {
            var rng = new Random(4);
            var banner = new List<Box> { Plan.BannerBox };
            var live = new List<(float x, float y)>();
            int placed = 0;
            for (int i = 0; i < 20000; i++)
            {
                if (!PilotSpawn.TryPlace(rng, i % 3 == 0, Plan.SkyTop, Plan.SkyBottom, banner, live, Straight, out float x, out float y)) continue;
                placed++;
                Assert.IsFalse(Box.Around(x, y, PilotContract.SignalRingSize * 0.5f).Intersects(Plan.BannerBox, 0f), "nace bajo el aviso");
                Assert.GreaterOrEqual(x, PilotSpawn.SideMargin - 1e-3f);
                Assert.LessOrEqual(x, PilotContract.FieldWidth - PilotSpawn.SideMargin + 1e-3f);
                Assert.GreaterOrEqual(y, Plan.SkyTop - 1e-3f);
                Assert.LessOrEqual(y, Plan.SkyBottom + 1e-3f);
                live.Add((x, y));
                if (live.Count > 3) live.RemoveAt(0);
            }
            Assert.Greater(placed, 15000);
        }

        [Test]
        public void ASignalNeverBornOnAnotherOrOnTheBeaconLine()
        {
            var rng = new Random(9);
            var live = new List<(float x, float y)> { (180f, 300f) };
            for (int i = 0; i < 5000; i++)
            {
                if (!PilotSpawn.TryPlace(rng, false, Plan.SkyTop, Plan.SkyBottom, null, live, Straight, out float x, out float y)) continue;
                Assert.GreaterOrEqual(Math.Sqrt((x - 180f) * (x - 180f) + (y - 300f) * (y - 300f)), PilotSpawn.SignalClear - 1e-3f);
                Assert.Greater(Math.Abs(Math.Abs(x - 180f) - 80f), PilotSpawn.RouteClear - 1e-3f);
            }
        }

        [Test]
        public void WithTheWholeSkyCovered_NothingIsBorn()
        {
            var all = new List<Box> { Plan.SkyBox };
            Assert.IsFalse(PilotSpawn.TryPlace(new Random(1), false, Plan.SkyTop, Plan.SkyBottom, all, null, null, out _, out _));
            Assert.IsFalse(PilotSpawn.TryPlace(new Random(1), true, Plan.SkyTop, Plan.SkyBottom, all, null, null, out _, out _));
        }

        [Test]
        public void BoxesIntersectWithTheirPadding()
        {
            var a = new Box(0f, 0f, 10f, 10f);
            Assert.IsFalse(a.Intersects(new Box(10f, 0f, 20f, 10f)));
            Assert.IsTrue(a.Intersects(new Box(10f, 0f, 20f, 10f), 1f));
            Assert.IsTrue(a.Intersects(new Box(5f, 5f, 6f, 6f)));
        }
    }
}
