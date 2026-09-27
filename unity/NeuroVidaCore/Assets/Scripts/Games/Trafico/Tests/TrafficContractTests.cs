using System;
using System.Collections.Generic;
using NUnit.Framework;

namespace NeuroVida.Games.Trafico.Tests
{
    public class TrafficContractTests
    {
        [Test]
        public void Networks_AreTreesWithoutCrossingsAtEverySize()
        {
            var rng = new Random(4);
            // Teléfonos más bajos o más altos (la separación se mide con la proporción real del campo), y rutas suaves
            // (nivel 1) o en serpentina (nivel 12).
            foreach (float aspect in new[] { 1.3f, 1.6f, 2.0f })
            foreach (float twist in new[] { 0f, 1f })
            for (int ports = 2; ports <= TrafficContract.MaxPorts; ports++)
            {
                for (int i = 0; i < 12; i++)
                {
                    var net = TrafficContract.BuildNetwork(ports, rng, aspect, twist);
                    int portCount = 0, switches = 0;
                    var colors = new HashSet<int>();
                    for (int n = 0; n < net.Count; n++)
                    {
                        Assert.That(net.X[n], Is.InRange(0f, 1f));
                        Assert.That(net.Y[n], Is.InRange(0f, 1f));
                        if (net.IsPort(n)) { portCount++; colors.Add(net.PortColor[n]); }
                        if (net.IsSwitch(n)) switches++;
                        if (n > 0) Assert.AreEqual(n, Array.IndexOf(net.Children[net.Parent[n]], n) >= 0 ? n : -1);
                    }
                    Assert.AreEqual(ports, portCount);
                    Assert.AreEqual(ports - 1, switches);             // árbol binario: un desvío menos que puertos
                    Assert.AreEqual(ports, colors.Count);              // cada puerto su color
                    Assert.IsTrue(TrafficContract.IsPlanar(net), $"{ports} puertos: tramos cruzados");
                    Assert.GreaterOrEqual(TrafficContract.MinGap(net, aspect), TrafficContract.MinNodeGap, $"{ports} puertos, proporción {aspect}: nodos muy juntos");
                    Assert.GreaterOrEqual(TrafficContract.MinNodeToRail(net, aspect), TrafficContract.MinRailGap, $"{ports} puertos, proporción {aspect}: una ruta pasa rozando un nodo ajeno");
                    Assert.GreaterOrEqual(TrafficContract.MinRouteGap(net), TrafficContract.MinRouteGapWidth, $"{ports} puertos, proporción {aspect}: dos rutas van pegadas");
                    for (int n = 1; n < net.Count; n++)
                    {
                        // Cada recorrido une de verdad al padre con el nodo, y sin saltos.
                        var xs = net.PathX[n];
                        var ys = net.PathY[n];
                        int p = net.Parent[n];
                        Assert.AreEqual(net.X[p], xs[0], 1e-4f);
                        Assert.AreEqual(net.Y[p], ys[0], 1e-4f);
                        Assert.AreEqual(net.X[n], xs[xs.Length - 1], 1e-4f);
                        Assert.AreEqual(net.Y[n], ys[ys.Length - 1], 1e-4f);
                        for (int k = 1; k < xs.Length; k++) Assert.Less(Math.Abs(xs[k] - xs[k - 1]) + Math.Abs(ys[k] - ys[k - 1]), 0.1f);
                    }
                    Assert.AreEqual(1, net.Children[0].Length);        // el portal tiene una sola salida
                    for (int c = 0; c < ports; c++) Assert.GreaterOrEqual(net.PortOfColor(c), 0);
                }
            }
        }

        [Test]
        public void Routes_CurveAndWindMoreAtHighLevels()
        {
            // Las rutas son más largas que las rectas (curvas), y en nivel alto la del portal serpentea: su largo crece
            // y cambia de sentido a lo ancho varias veces.
            float Stretch(float twist, out int turns)
            {
                var rng = new Random(9);
                float ratio = 0f;
                turns = 0;
                const int nets = 20;
                for (int i = 0; i < nets; i++)
                {
                    var net = TrafficContract.BuildNetwork(5, rng, 1.6f, twist);
                    int trunk = net.Children[0][0];
                    float straight = (float)Math.Sqrt(Math.Pow((net.X[trunk] - net.X[0]) / 1.6f, 2) + Math.Pow(net.Y[trunk] - net.Y[0], 2));
                    ratio += net.PathLength(trunk) / straight / nets;
                    var xs = net.PathX[trunk];
                    int dir = 0;
                    for (int k = 1; k < xs.Length; k++)
                    {
                        float d = xs[k] - xs[k - 1];
                        if (Math.Abs(d) < 1e-4f) continue;
                        int sgn = d > 0 ? 1 : -1;
                        if (dir != 0 && sgn != dir) turns++;
                        dir = sgn;
                    }
                }
                return ratio;
            }
            float soft = Stretch(0f, out int softTurns);
            float wound = Stretch(1f, out int woundTurns);
            Assert.Greater(soft, 1.02f);
            Assert.Greater(wound, soft * 1.5f);
            Assert.Greater(woundTurns, softTurns);
            Assert.Less(TrafficContract.Twist(1), TrafficContract.Twist(12));
        }

        [Test]
        public void Pods_MoveAlongTheCurveAtConstantSpeed()
        {
            var net = ThreePorts();
            // Tramo 0 → 1 en forma de arco (medio círculo aplanado).
            var xs = new float[21];
            var ys = new float[21];
            for (int i = 0; i <= 20; i++)
            {
                double t = Math.PI * i / 20.0;
                xs[i] = 0.5f + 0.2f * (float)Math.Sin(t);
                ys[i] = 0.3f * i / 20f;
            }
            net.EnsurePaths();
            net.SetPath(1, xs, ys);
            Assert.Greater(net.PathLength(1), 0.3f);   // más largo que la recta
            var sim = new TrafficSim(net) { Speed = 0.1f };
            var pod = sim.Spawn(0);
            sim.Step(net.PathLength(1) * 0.5f / 0.1f, null);
            sim.Position(pod, out float x, out float y);
            Assert.AreEqual(0.7f, x, 0.01f);             // a mitad de camino está en lo más ancho del arco
            Assert.AreEqual(0.15f, y, 0.01f);
        }

        private static TrafficNetwork ThreePorts()
        {
            // Portal (0) → desvío (1) → [puerto 2 (color 0), desvío 3 → [puerto 4 (color 1), puerto 5 (color 2)]]
            var net = new TrafficNetwork();
            net.X.AddRange(new[] { 0.5f, 0.5f, 0.2f, 0.7f, 0.6f, 0.8f });
            net.Y.AddRange(new[] { 0f, 0.3f, 0.8f, 0.5f, 0.8f, 0.8f });
            net.Children.AddRange(new[] { new[] { 1 }, new[] { 2, 3 }, new int[0], new[] { 4, 5 }, new int[0], new int[0] });
            net.Parent.AddRange(new[] { -1, 0, 1, 1, 3, 3 });
            net.PortColor.AddRange(new[] { -1, -1, 0, -1, 1, 2 });
            return net;
        }

        [Test]
        public void Pods_FollowTheSwitchesActiveWhenTheyArrive()
        {
            var sim = new TrafficSim(ThreePorts()) { Speed = 0.5f };
            var ev = new List<TrafficEvent>();
            var pod = sim.Spawn(2);
            // El desvío 1 apunta al puerto 2 (mal); lo corrijo antes de que llegue.
            sim.Step(0.2f, ev);
            sim.Toggle(1);
            sim.Toggle(3); // hacia el puerto 5 (color 2)
            for (int i = 0; i < 100 && !pod.Done; i++) sim.Step(0.05f, ev);
            Assert.IsTrue(pod.Done);
            var arrival = ev.Find(e => e.Arrived);
            Assert.IsTrue(arrival.Correct);
            Assert.AreEqual(5, arrival.Node);
            // Pasos por desvíos: los dos bien y con anticipación medida (se movieron para esta cápsula).
            var passes = ev.FindAll(e => !e.Arrived);
            Assert.AreEqual(2, passes.Count);
            Assert.IsTrue(passes.TrueForAll(e => e.Correct && e.Lead > 0f));
        }

        [Test]
        public void MovingASwitchAfterThePodPassedDoesNotChangeItsRoute()
        {
            var sim = new TrafficSim(ThreePorts()) { Speed = 1f };
            var ev = new List<TrafficEvent>();
            var pod = sim.Spawn(0);
            sim.Step(0.35f, ev);               // ya pasó el desvío 1 (a 0,3) camino al puerto 2
            Assert.AreEqual(2, pod.To);
            sim.Toggle(1);                     // tarde: no la cambia
            for (int i = 0; i < 50 && !pod.Done; i++) sim.Step(0.05f, ev);
            Assert.IsTrue(ev.Find(e => e.Arrived).Correct);
            // No movió el desvío para ella antes de pasar: no hay anticipación registrada.
            Assert.AreEqual(-1f, ev.Find(e => !e.Arrived).Lead);
        }

        [Test]
        public void WrongPortCountsAsError()
        {
            var sim = new TrafficSim(ThreePorts()) { Speed = 1f };
            var ev = new List<TrafficEvent>();
            var pod = sim.Spawn(1);            // va al puerto 2 (color 0): error
            for (int i = 0; i < 50 && !pod.Done; i++) sim.Step(0.05f, ev);
            var a = ev.Find(e => e.Arrived);
            Assert.IsFalse(a.Correct);
            Assert.AreEqual(2, a.Node);
            Assert.IsFalse(ev.Find(e => !e.Arrived).Correct);
            Assert.AreEqual(0, sim.InFlight);
        }

        [Test]
        public void Ladder_Colors_AndPoints()
        {
            Assert.AreEqual(2, TrafficContract.Ports(1));
            Assert.AreEqual(8, TrafficContract.Ports(12));
            for (int l = 2; l <= TrafficContract.MaxLevel; l++)
            {
                Assert.GreaterOrEqual(TrafficContract.Ports(l), TrafficContract.Ports(l - 1));
                Assert.Greater(TrafficContract.Speed(l, false), TrafficContract.Speed(l - 1, false));
                Assert.Less(TrafficContract.SpawnInterval(l, false), TrafficContract.SpawnInterval(l - 1, false));
            }
            Assert.Less(TrafficContract.Speed(5, true), TrafficContract.Speed(5, false));
            var rng = new Random(1);
            int prev = -1;
            for (int i = 0; i < 300; i++)
            {
                int c = TrafficContract.NextColor(5, prev, rng);
                Assert.That(c, Is.InRange(0, 4));
                Assert.AreNotEqual(prev, c);
                prev = c;
            }
            Assert.AreEqual(0, TrafficContract.Points(false, 5, 3));
            Assert.Greater(TrafficContract.Points(true, 6, 1), TrafficContract.Points(true, 1, 1));
            Assert.Greater(TrafficContract.Points(true, 1, 5), TrafficContract.Points(true, 1, 1));
        }

        [Test]
        public void Anticipation_MedianAndProactiveShare()
        {
            Assert.AreEqual(-1f, TrafficContract.MedianLead(new List<float> { 1f, 2f }));
            var leads = new List<float> { 0.2f, 0.4f, 1.5f, 2.0f, 9f };
            Assert.AreEqual(1.5f, TrafficContract.MedianLead(leads), 1e-5f);
            Assert.AreEqual(0.6f, TrafficContract.ProactiveShare(leads), 1e-5f);
            Assert.AreEqual(100, TrafficContract.Score(1f, TrafficContract.MaxLevel));
            Assert.Greater(TrafficContract.Score(0.9f, 6), TrafficContract.Score(0.6f, 6));
        }
    }
}
