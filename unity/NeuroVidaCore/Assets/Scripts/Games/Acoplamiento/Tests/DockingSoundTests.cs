using System;
using System.IO;
using NUnit.Framework;
using UnityEngine;

namespace NeuroVida.Games.Acoplamiento.Tests
{
    /// <summary>
    /// El sonido «Madera cálida» de Acoplamiento (Tarea 68): el C# suena igual que la versión 2 del boceto que Ricardo aprobó (muestras de REFERENCIA generadas con node a partir del propio boceto: <c>node tools/sonido/referencia-acoplamiento.js</c>), los clips se hornean de a poco, caben en memoria (menos de 20 MB) y
    /// nada suena fuerte: cada pico es el «nivel» de la tabla del diseño, y los más cortos son los más suaves.
    /// </summary>
    public class DockingSoundTests
    {
        [Serializable]
        private class Summary
        {
            public int length;
            public double sumAbsL, sumAbsR, peak;
            public float[] tailL, headL, headR;
        }

        [Serializable]
        private class Reference
        {
            public double[] rng7;
            public Summary arrive, tap, flip, dock, slot1, slot8, ring, miss, finale;
        }

        private static Reference _reference;

        private static Reference Ref()
        {
            if (_reference != null) return _reference;
            string path = Path.Combine(Application.dataPath, "Scripts", "Games", "Acoplamiento", "Tests", "DockingSoundReference.json");
            Assert.IsTrue(File.Exists(path), "falta " + path + " (se genera con: node tools/sonido/referencia-acoplamiento.js)");
            return _reference = JsonUtility.FromJson<Reference>(File.ReadAllText(path));
        }

        private const float Tolerance = 1e-4f;

        private static void CheckAgainst(string label, Summary expected, DockSfx sfx, int arg = 0)
        {
            DockingSounds.RenderStereo(sfx, arg, out var left, out var right);
            Assert.AreEqual(expected.length, left.Length, label + ": el largo es el mismo (hasta el redondeo de los segundos)");
            Assert.AreEqual(left.Length, right.Length);
            double sl = 0, sr = 0, pk = 0;
            for (int i = 0; i < left.Length; i++)
            {
                sl += Math.Abs((double)left[i]);
                sr += Math.Abs((double)right[i]);
                pk = Math.Max(pk, Math.Max(Math.Abs((double)left[i]), Math.Abs((double)right[i])));
            }
            Assert.AreEqual(expected.peak, pk, 1e-6, label + ": el pico (el nivel de la tabla del diseño)");
            Assert.AreEqual(expected.sumAbsL, sl, expected.sumAbsL * 1e-4, label + ": toda la señal izquierda (suma de valores absolutos)");
            Assert.AreEqual(expected.sumAbsR, sr, expected.sumAbsR * 1e-4, label + ": toda la señal derecha");
            for (int i = 0; i < expected.tailL.Length; i++) Assert.AreEqual(expected.tailL[i], left[left.Length - 40 + i], Tolerance, label + ": la cola, muestra " + i);
            if (expected.headL != null && expected.headL.Length > 0)
                for (int i = 0; i < expected.headL.Length; i++) Assert.AreEqual(expected.headL[i], left[i], Tolerance, label + ": las primeras " + expected.headL.Length + " muestras, la " + i);
            if (expected.headR != null && expected.headR.Length > 0)
                for (int i = 0; i < expected.headR.Length; i++) Assert.AreEqual(expected.headR[i], right[i], Tolerance, label + ": canal derecho, muestra " + i);
        }

        [Test]
        public void TheRandomGeneratorGivesTheSketchSeries()
        {
            var r = new NeuroVida.Games.Shared.SoundKit.Rng(7);
            var expected = Ref().rng7;
            for (int i = 0; i < expected.Length; i++) Assert.AreEqual(expected[i], r.Next(), 1e-12, "mulberry32 con la semilla 7, valor " + i);
        }

        [Test] public void TheApproachBreeze_SoundsLikeTheSketch() => CheckAgainst("se acerca", Ref().arrive, DockSfx.Arrive);
        [Test] public void TheTap_SoundsLikeTheSketch() => CheckAgainst("tocar", Ref().tap, DockSfx.Tap);
        [Test] public void TheMirrorFlip_SoundsLikeTheSketch() => CheckAgainst("se da vuelta", Ref().flip, DockSfx.Flip);
        [Test] public void TheDock_SoundsLikeTheSketch() => CheckAgainst("encaja", Ref().dock, DockSfx.Dock);
        [Test] public void TheSlotNoteWithStreakOne_SoundsLikeTheSketch() => CheckAgainst("se suma (racha 1)", Ref().slot1, DockSfx.Slot, 1);
        [Test] public void TheSlotNoteWithStreakEight_SoundsLikeTheSketch() => CheckAgainst("se suma (racha 8)", Ref().slot8, DockSfx.Slot, 8);
        [Test] public void TheRingComplete_SoundsLikeTheSketch() => CheckAgainst("anillo completo", Ref().ring, DockSfx.Ring);
        [Test] public void TheMiss_SoundsLikeTheSketch() => CheckAgainst("no era", Ref().miss, DockSfx.Miss);
        [Test] public void TheFinale_SoundsLikeTheSketch() => CheckAgainst("final", Ref().finale, DockSfx.Finale);

        [Test]
        public void TheSlotNoteRisesWithTheStreak_AndStopsRisingAtEight()
        {
            var x1 = DockingSounds.MakeRecipe(DockSfx.Slot, 1).X;
            var x2 = DockingSounds.MakeRecipe(DockSfx.Slot, 2).X;
            Assert.AreNotEqual(x1[200], x2[200], "cada racha suena distinto");
            var x8 = DockingSounds.MakeRecipe(DockSfx.Slot, 8).X;
            var x9 = DockingSounds.MakeRecipe(DockSfx.Slot, 20).X;
            CollectionAssert.AreEqual(x8, x9, "pasada la racha 8 la nota ya no sube");
            Assert.Greater(NeuroVida.Games.Radar.RadarSounds.P[1 + 8], NeuroVida.Games.Radar.RadarSounds.P[1 + 1], "la nota sube con la racha (la pentatónica sube)");
        }

        [Test]
        public void TheShortSoundsAreTheSoftestAndNothingIsLoud_TheTableOfTheDesign()
        {
            // los niveles (picos) de la tabla del diseño: los cortos son los más suaves
            var peaks = new (DockSfx sfx, double level)[]
            {
                (DockSfx.Arrive, 0.06), (DockSfx.Tap, 0.07), (DockSfx.Flip, 0.16), (DockSfx.Dock, 0.30), (DockSfx.Slot, 0.26), (DockSfx.Ring, 0.36), (DockSfx.Miss, 0.22), (DockSfx.Finale, 0.40),
            };
            foreach (var (sfx, level) in peaks)
            {
                Assert.AreEqual(level, DockingSounds.MakeRecipe(sfx, 1).Level, 1e-9, sfx + ": nivel");
                Assert.LessOrEqual(level, 0.40 + 1e-9, sfx + " no pasa de 0,40");
            }
            Assert.Less(DockingSounds.MakeRecipe(DockSfx.Arrive).Level, DockingSounds.MakeRecipe(DockSfx.Dock).Level, "el soplo es más suave que el golpe");
            Assert.Less(DockingSounds.MakeRecipe(DockSfx.Tap).Level, DockingSounds.MakeRecipe(DockSfx.Slot, 1).Level, "el toque es más suave que la nota de la estación");
        }

        [Test]
        public void TheClipsAreBakedOnceInSlices_AndTakeLessThan20Mb_AndNothingIsLoud()
        {
            DockingSounds.ClearCache();
            var warm = DockingSounds.Prewarm();
            int frames = 0;
            var clock = System.Diagnostics.Stopwatch.StartNew();
            while (warm.MoveNext()) frames++;
            clock.Stop();
            Assert.AreEqual(DockingSounds.BakeOrder.Length, DockingSounds.CachedCount, "los sonidos del juego");
            Assert.Greater(frames, DockingSounds.BakeOrder.Length * 2, "se calculan de a poco (varios cuadros por clip), no de golpe");
            long bytes = DockingSounds.TotalSamplesPerChannel() * 2 * sizeof(float);
            Debug.Log("[DockingSounds] clips estéreo: " + DockingSounds.CachedCount + " clips, " + DockingSounds.TotalSamplesPerChannel() + " muestras por canal = " + (bytes / 1048576.0).ToString("0.0") + " MB, " + frames + " cuadros de precarga, " + clock.ElapsedMilliseconds + " ms de cálculo en total (en el PC)");
            Assert.Less(bytes, 20L * 1024 * 1024, "los clips estéreo pesan menos de 20 MB");
            int before = DockingSounds.CachedCount;
            Assert.AreSame(DockingSounds.Get(DockSfx.Tap), DockingSounds.Get(DockSfx.Tap), "un clip por sonido");
            Assert.AreEqual(before, DockingSounds.CachedCount);
            foreach (var (sfx, arg) in DockingSounds.BakeOrder)
            {
                var clip = DockingSounds.Get(sfx, arg);
                Assert.AreEqual(2, clip.channels, clip.name + " es estéreo");
                Assert.AreEqual(44100, clip.frequency, clip.name + " a 44 100 Hz");
                var data = new float[clip.samples * clip.channels];
                clip.GetData(data, 0);
                float peak = 0f;
                foreach (float v in data) peak = Math.Max(peak, Math.Abs(v));
                Assert.Greater(peak, 0.04f, clip.name + " suena");
                Assert.LessOrEqual(peak, 0.4001f, clip.name + " no pasa de 0,40");
            }
        }

        [Test]
        public void EverySoundOfTheGameHasAClip()
        {
            foreach (DockSfx sfx in Enum.GetValues(typeof(DockSfx))) Assert.IsNotNull(DockingSounds.Get(sfx, 1), sfx.ToString());
            Assert.AreNotSame(DockingSounds.Get(DockSfx.Slot, 1), DockingSounds.Get(DockSfx.Slot, 5));
        }
    }
}
