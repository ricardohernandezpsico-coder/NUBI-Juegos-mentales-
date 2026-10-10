using System;
using System.IO;
using NUnit.Framework;
using UnityEngine;

namespace NeuroVida.Games.Aterrizaje.Tests
{
    /// <summary>
    /// El sonido «Madera cálida» de Aterrizaje Lunar (Tarea 70): el C# suena igual que la versión 4 del boceto que Ricardo aprobó (muestras de REFERENCIA generadas con node a partir del propio boceto: <c>node tools/sonido/referencia-aterrizaje.js</c>), el propulsor es un bucle de 4 s sin costura, los clips se hornean de a poco,
    /// caben en memoria (menos de 10 MB) y nada suena fuerte: cada pico es el «nivel» de la tabla del diseño, y los más cortos son los más suaves.
    /// </summary>
    public class LandingSoundTests
    {
        [Serializable]
        private class Summary
        {
            public int length;
            public double sumAbsL, sumAbsR, peak;
            public float[] tailL, headL, headR;
        }

        [Serializable]
        private class LoopSummary
        {
            public int length;
            public double sumAbs, peak;
            public float[] head, mid, tail;
        }

        [Serializable]
        private class Reference
        {
            public double[] rng7;
            public Summary release, touch, flag, hit, bull, miss, dome, finale;
            public LoopSummary hover;
        }

        private static Reference _reference;

        private static Reference Ref()
        {
            if (_reference != null) return _reference;
            string path = Path.Combine(Application.dataPath, "Scripts", "Games", "Aterrizaje", "Tests", "LandingSoundReference.json");
            Assert.IsTrue(File.Exists(path), "falta " + path + " (se genera con: node tools/sonido/referencia-aterrizaje.js)");
            return _reference = JsonUtility.FromJson<Reference>(File.ReadAllText(path));
        }

        private const float Tolerance = 1e-4f;

        private static void CheckAgainst(string label, Summary expected, LandSfx sfx)
        {
            LandingSounds.RenderStereo(sfx, out var left, out var right);
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

        [Test] public void TheReleaseBreeze_SoundsLikeTheSketch() => CheckAgainst("soltar", Ref().release, LandSfx.Release);
        [Test] public void TheTouchdown_SoundsLikeTheSketch() => CheckAgainst("posarse", Ref().touch, LandSfx.Touch);
        [Test] public void TheFlagKalimba_SoundsLikeTheSketch() => CheckAgainst("sube la bandera", Ref().flag, LandSfx.Flag);
        [Test] public void TheHit_SoundsLikeTheSketch() => CheckAgainst("acierto", Ref().hit, LandSfx.Hit);
        [Test] public void TheBullseye_SoundsLikeTheSketch() => CheckAgainst("diana", Ref().bull, LandSfx.Bull);
        [Test] public void TheMiss_SoundsLikeTheSketch() => CheckAgainst("lejos", Ref().miss, LandSfx.Miss);
        [Test] public void TheDome_SoundsLikeTheSketch() => CheckAgainst("cúpula", Ref().dome, LandSfx.Dome);
        [Test] public void TheFinale_SoundsLikeTheSketch() => CheckAgainst("final", Ref().finale, LandSfx.Finale);

        [Test]
        public void TheHoverLoop_IsTheSketchLoop_AndSeamless()
        {
            var expected = Ref().hover;
            var data = LandingSounds.MakeHoverLoop();
            Assert.AreEqual(expected.length, data.Length, "4 s a 44.100 Hz");
            Assert.AreEqual(176400, data.Length);
            double sum = 0, pk = 0;
            for (int i = 0; i < data.Length; i++) { sum += Math.Abs((double)data[i]); pk = Math.Max(pk, Math.Abs((double)data[i])); }
            Assert.AreEqual(expected.peak, pk, 1e-6, "el pico queda en 0,5");
            Assert.AreEqual(0.5, pk, 1e-6);
            Assert.AreEqual(expected.sumAbs, sum, expected.sumAbs * 1e-4, "toda la señal");
            for (int i = 0; i < expected.head.Length; i++) Assert.AreEqual(expected.head[i], data[i], Tolerance, "el comienzo, muestra " + i);
            for (int i = 0; i < expected.mid.Length; i++) Assert.AreEqual(expected.mid[i], data[88200 + i], Tolerance, "el medio, muestra " + i);
            for (int i = 0; i < expected.tail.Length; i++) Assert.AreEqual(expected.tail[i], data[data.Length - 40 + i], Tolerance, "el final, muestra " + i);
            // sin costura: el salto del final al comienzo no es mayor que el paso más grande dentro del bucle
            double maxStep = 0;
            for (int i = 1; i < data.Length; i++) maxStep = Math.Max(maxStep, Math.Abs((double)data[i] - data[i - 1]));
            Assert.LessOrEqual(Math.Abs((double)data[0] - data[data.Length - 1]), maxStep * 1.01, "el final empalma con el comienzo");
        }

        [Test]
        public void TheShortSoundsAreTheSoftestAndNothingIsLoud_TheTableOfTheDesign()
        {
            // los niveles (picos) de la tabla del diseño: soltar 0,08 · posarse 0,26 · bandera 0,20 · acierto 0,26 · diana 0,34 · lejos 0,20 · cúpula 0,36 · final 0,40
            var peaks = new (LandSfx sfx, double level)[]
            {
                (LandSfx.Release, 0.08), (LandSfx.Touch, 0.26), (LandSfx.Flag, 0.20), (LandSfx.Hit, 0.26), (LandSfx.Bull, 0.34), (LandSfx.Miss, 0.20), (LandSfx.Dome, 0.36), (LandSfx.Finale, 0.40),
            };
            foreach (var (sfx, level) in peaks)
            {
                Assert.AreEqual(level, LandingSounds.MakeRecipe(sfx).Level, 1e-9, sfx + ": nivel");
                Assert.LessOrEqual(level, 0.40 + 1e-9, sfx + " no pasa de 0,40");
            }
            Assert.Less(LandingSounds.MakeRecipe(LandSfx.Release).Level, LandingSounds.MakeRecipe(LandSfx.Touch).Level, "el soplo es más suave que el golpe");
            Assert.Less(LandingSounds.HoverVolume, 0.1f, "el propulsor es un colchón muy suave");
        }

        [Test]
        public void TheClipsAreBakedOnceInSlices_AndTakeLessThan10Mb_AndNothingIsLoud()
        {
            LandingSounds.ClearCache();
            var warm = LandingSounds.Prewarm();
            int frames = 0;
            var clock = System.Diagnostics.Stopwatch.StartNew();
            while (warm.MoveNext()) frames++;
            clock.Stop();
            Assert.AreEqual(LandingSounds.BakeOrder.Length, LandingSounds.CachedCount, "los sonidos del juego (sin contar el propulsor)");
            Assert.Greater(frames, LandingSounds.BakeOrder.Length * 2, "se calculan de a poco (varios cuadros por clip), no de golpe");
            long bytes = LandingSounds.TotalSamplesPerChannel() * sizeof(float) * 2;                            // el propulsor es mono: se cuenta de más, no de menos
            Debug.Log("[LandingSounds] clips: " + LandingSounds.CachedCount + " estéreo y el bucle del propulsor, " + LandingSounds.TotalSamplesPerChannel() + " muestras por canal ≈ " + (bytes / 1048576.0).ToString("0.0") + " MB, " + frames + " cuadros de precarga, " + clock.ElapsedMilliseconds + " ms de cálculo en total (en el PC)");
            Assert.Less(bytes, 10L * 1024 * 1024, "los clips pesan menos de 10 MB");
            int before = LandingSounds.CachedCount;
            Assert.AreSame(LandingSounds.Get(LandSfx.Touch), LandingSounds.Get(LandSfx.Touch), "un clip por sonido");
            Assert.AreEqual(before, LandingSounds.CachedCount);
            foreach (var sfx in LandingSounds.BakeOrder)
            {
                var clip = LandingSounds.Get(sfx);
                Assert.AreEqual(2, clip.channels, clip.name + " es estéreo");
                Assert.AreEqual(44100, clip.frequency, clip.name + " a 44 100 Hz");
                var data = new float[clip.samples * clip.channels];
                clip.GetData(data, 0);
                float peak = 0f;
                foreach (float v in data) peak = Math.Max(peak, Math.Abs(v));
                Assert.Greater(peak, 0.04f, clip.name + " suena");
                Assert.LessOrEqual(peak, 0.4001f, clip.name + " no pasa de 0,40");
            }
            var hover = LandingSounds.HoverLoop();
            Assert.AreEqual(1, hover.channels);
            Assert.AreEqual(176400, hover.samples);
        }

        [Test]
        public void EverySoundOfTheGameHasAClip()
        {
            foreach (LandSfx sfx in Enum.GetValues(typeof(LandSfx))) Assert.IsNotNull(LandingSounds.Get(sfx), sfx.ToString());
        }
    }
}
