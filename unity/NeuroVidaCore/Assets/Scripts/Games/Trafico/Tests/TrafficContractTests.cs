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
            // Teléfonos más bajos o más altos: la separación se mide con la proporción real del campo.
            foreach (float aspect in new[] { 1.3f, 1.6f, 2.0f })
            for (int ports = 2; ports <= TrafficContract.MaxPorts; ports++)
            {
                for (int i = 0; i < 25; i++)
                {
                    var net = TrafficContract.BuildNetwork(ports, rng, aspect);
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
                    Assert.AreEqual(1, net.Children[0].Length);        // el portal tiene una sola salida
                    for (int c = 0; c < ports; c++) Assert.GreaterOrEqual(net.PortOfColor(c), 0);
                }
            }
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
