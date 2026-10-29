using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using NeuroVida.Games.Shared;
using NeuroVida.Games.Acoplamiento;
using NeuroVida.Games.Aterrizaje;
using NeuroVida.Games.Bodega;
using NeuroVida.Games.Correo;
using NeuroVida.Games.Engranajes;
using NeuroVida.Games.Radar;
using static NeuroVida.Games.Shared.ClayRaster;

namespace NeuroVida.Games.Tests
{
    /// <summary>
    /// El horneo de sprites en hilos de trabajo (Tarea 190): los pixeles tienen que quedar IDÉNTICOS a los de un solo hilo, en todos los juegos que se repartieron (Correo, Engranajes, Rescate, Acoplamiento, Aterrizaje y Bodega), y el repartidor tiene que repartir de verdad
    /// (todas las filas una sola vez, con varios hilos) y no perder un error. La Texture2D y el Sprite siguen creándose en el hilo principal (acá corren los dos modos en el hilo de la prueba).
    /// </summary>
    public class SpriteThreadsTests
    {
        [TearDown]
        public void TearDown() => Background.Parallel = true;

        // ------------------------------------------------------------------ el repartidor

        [Test]
        public void ForRows_RunsEveryRowExactlyOnce_AndUsesMoreThanOneThread()
        {
            Background.Parallel = true;
            const int rows = 4000;
            var hits = new int[rows];
            var threads = new HashSet<int>();
            Background.ForRows(rows, i =>
            {
                System.Threading.Interlocked.Increment(ref hits[i]);
                lock (threads) threads.Add(System.Threading.Thread.CurrentThread.ManagedThreadId);
                double x = 0; for (int k = 0; k < 20000; k++) x += Math.Sqrt(k + i);      // un poco de trabajo para que los hilos se repartan
                if (x < 0) hits[i] = -99;
            }, int.MaxValue);
            for (int i = 0; i < rows; i++) Assert.AreEqual(1, hits[i], "fila " + i);
            if (Background.Workers > 1) Assert.Greater(threads.Count, 1, "con " + Background.Workers + " hilos de trabajo, las filas se reparten de verdad");
        }

        [Test]
        public void ForRows_DoesNotSwallowAnError()
        {
            Background.Parallel = true;
            Assert.Throws<AggregateException>(() => Background.ForRows(500, i => { if (i == 321) throw new InvalidOperationException("fila mala"); }, int.MaxValue));
        }

        [Test]
        public void ARunInTheBackground_FinishesOnItsOwnAndReportsItsError()
        {
            Background.Parallel = true;
            int mainId = System.Threading.Thread.CurrentThread.ManagedThreadId, workerId = -1;
            var ok = Background.Run(() => workerId = System.Threading.Thread.CurrentThread.ManagedThreadId);
            var bad = Background.Run(() => throw new InvalidOperationException("mal"));
            var watch = System.Diagnostics.Stopwatch.StartNew();
            while ((!ok.IsDone || !bad.IsDone) && watch.ElapsedMilliseconds < 10000) System.Threading.Thread.Sleep(1);
            Assert.IsTrue(ok.IsDone && bad.IsDone);
            Assert.IsNull(ok.Error);
            Assert.AreNotEqual(mainId, workerId, "corrió en un hilo de trabajo");
            Assert.IsInstanceOf<InvalidOperationException>(bad.Error);
        }

        // ------------------------------------------------------------------ pixeles idénticos

        [Test]
        public void RenderClay_GivesTheSamePixelsInParallelAsInASingleThread()
        {
            ShapeFn body = (x, y) => RoundBox(x, y, 0.1f, -0.05f, 0.62f, 0.4f, 0.15f);
            PaintFn paint = (ref Px p, float x, float y) =>
            {
                p.Over(Hex(0x7FD8FF), Cover(RoundBox(x, y, 0.1f, -0.05f, 0.62f, 0.4f, 0.15f), 0.01f));
                p.Over(Hex(0xFFC94A), Cover(Circle(x, y, -0.2f, 0.1f, 0.18f), 0.01f));
            };
            Background.Parallel = false;
            var serial = RenderClay(384, 1.2f, 0.05f, 0.04f, 0.01f, body, paint);
            Background.Parallel = true;
            var parallel = RenderClay(384, 1.2f, 0.05f, 0.04f, 0.01f, body, paint);
            AssertSame("RenderClay 384", serial, parallel);
        }

        private static readonly (Type Type, string Name)[] Sprites =
        {
            (typeof(MailSprites), "Correo"), (typeof(EngranajesSprites), "Engranajes"), (typeof(RadarSprites), "Rescate"),
            (typeof(DockingSprites), "Acoplamiento"), (typeof(LandingSprites), "Aterrizaje"), (typeof(BodegaSprites), "Bodega"),
        };

        [Test]
        public void EverySpriteOfTheSixGamesHasTheSamePixelsInParallelAsInASingleThread()
        {
            foreach (var (type, name) in Sprites)
            {
                var cache = CacheOf(type);
                Background.Parallel = false;
                ClearCache(cache);
                Drain(Prewarm(type));
                var serial = Snapshot(cache);
                Background.Parallel = true;
                ClearCache(cache);
                Drain(Prewarm(type));
                var parallel = Snapshot(cache);
                ClearCache(cache);
                Assert.GreaterOrEqual(serial.Count, 1, name + ": el Prewarm hornea sprites (los que salen del atlas de plasticina no se hornean: no pasan por la caché)");
                Debug.Log("[SpriteThreads] " + name + ": " + serial.Count + " sprites procedurales idénticos");
                CollectionAssert.AreEquivalent(serial.Keys, parallel.Keys, name + ": los mismos sprites");
                foreach (var kv in serial) AssertSame(name + " «" + kv.Key + "»", kv.Value, parallel[kv.Key]);
            }
        }

        [Test]
        public void TheDockingPieceBakedByAWorkerEqualsTheOneBakedInSteps()
        {
            var cells = new List<Cell> { new Cell(0, 0), new Cell(1, 0), new Cell(2, 0), new Cell(0, 1), new Cell(1, 1) };
            var g = new DockingArt.PieceGeo(cells);
            int n = 96;
            var serial = new Color32[n * n];
            DockingArt.Drain(DockingArt.FaceRoutine(g, n, DockingArt.Bp, 0f, false, DockingArt.RaisedStyle(), serial));
            var threaded = new Color32[n * n];
            var work = Background.RunEnumerator(DockingArt.FaceRoutine(g, n, DockingArt.Bp, 0f, false, DockingArt.RaisedStyle(), threaded));
            var watch = System.Diagnostics.Stopwatch.StartNew();
            while (!work.IsDone && watch.ElapsedMilliseconds < 20000) System.Threading.Thread.Sleep(1);
            Assert.IsTrue(work.IsDone);
            Assert.IsNull(work.Error);
            AssertSame("la cara de la pieza de Acoplamiento en un hilo", serial, threaded);
            var cav1 = new Color32[n * n]; var cor1 = new Color32[n * n];
            DockingArt.Drain(DockingArt.PortRoutine(g, n, DockingArt.Bp, 31, cav1, cor1));
            var cav2 = new Color32[n * n]; var cor2 = new Color32[n * n];
            var port = Background.RunEnumerator(DockingArt.PortRoutine(g, n, DockingArt.Bp, 31, cav2, cor2));
            watch.Restart();
            while (!port.IsDone && watch.ElapsedMilliseconds < 20000) System.Threading.Thread.Sleep(1);
            Assert.IsTrue(port.IsDone && port.Error == null);
            AssertSame("el hueco en un hilo", cav1, cav2);
            AssertSame("el cordón en un hilo", cor1, cor2);
        }

        [Test]
        public void TheDashFrameOfCorreoWithPrunedStrokesEqualsTheOriginalPainterPixelByPixel()
        {
            var cache = CacheOf(typeof(MailSprites));
            ClearCache(cache);
            var sprite = MailSprites.DashFrame();
            var tex = sprite.texture;
            var got = tex.GetPixels32();
            var dashes = (List<float>)typeof(MailSprites).GetMethod("FrameDashes", BindingFlags.NonPublic | BindingFlags.Static).Invoke(null, null);
            int nx = tex.width, ny = tex.height;
            float boxW = MailSprites.FrameBoxW, boxH = MailSprites.FrameBoxH, aa = 1f / MailSprites.SmallPpd;
            for (int iy = 0; iy < ny; iy++)
            {
                float yUp = ((iy + 0.5f) / ny - 0.5f) * boxH;
                for (int ix = 0; ix < nx; ix++)
                {
                    float x = ((ix + 0.5f) / nx - 0.5f) * boxW, y = -yUp;
                    float d = float.MaxValue;
                    for (int i = 0; i < dashes.Count; i += 4) d = Mathf.Min(d, Capsule(x, y, dashes[i], dashes[i + 1], dashes[i + 2], dashes[i + 3], 1.5f));   // el painter ORIGINAL: todos los trazos
                    var p = new Px();
                    p.Over(Color.white, Cover(d, aa));
                    var want = p.ToColor32();
                    var g = got[iy * nx + ix];
                    if (want.r != g.r || want.g != g.g || want.b != g.b || want.a != g.a) Assert.Fail("el pixel (" + ix + ", " + iy + ") del marco punteado difiere del painter original: " + want + " contra " + g);
                }
            }
            ClearCache(cache);
        }

        // ------------------------------------------------------------------ ayudas

        private static IEnumerator Prewarm(Type t) => (IEnumerator)t.GetMethod("Prewarm", BindingFlags.Public | BindingFlags.Static).Invoke(null, null);

        private static IDictionary CacheOf(Type t)
        {
            var f = t.GetField("Cache", BindingFlags.NonPublic | BindingFlags.Static);
            Assert.IsNotNull(f, t.Name + ": no tiene la caché «Cache»");
            return (IDictionary)f.GetValue(null);
        }

        private static void Drain(IEnumerator it) { while (it.MoveNext()) { } }

        private static Dictionary<string, Color32[]> Snapshot(IDictionary cache)
        {
            var d = new Dictionary<string, Color32[]>();
            foreach (DictionaryEntry e in cache)
            {
                var s = e.Value as Sprite;
                if (s == null) continue;
                d[e.Key.ToString()] = s.texture.GetPixels32();
            }
            return d;
        }

        private static void ClearCache(IDictionary cache)
        {
            foreach (DictionaryEntry e in cache)
            {
                var s = e.Value as Sprite;
                if (s == null) continue;
                var tex = s.texture;
                UnityEngine.Object.DestroyImmediate(s);
                if (tex != null) UnityEngine.Object.DestroyImmediate(tex);
            }
            cache.Clear();
        }

        private static void AssertSame(string what, Color32[] a, Color32[] b)
        {
            Assert.AreEqual(a.Length, b.Length, what + ": el mismo tamaño");
            for (int i = 0; i < a.Length; i++)
                if (a[i].r != b[i].r || a[i].g != b[i].g || a[i].b != b[i].b || a[i].a != b[i].a)
                    Assert.Fail(what + ": el pixel " + i + " (fila " + i / Math.Max(1, (int)Math.Sqrt(a.Length)) + ") difiere: " + a[i] + " contra " + b[i]);
        }
    }
}
