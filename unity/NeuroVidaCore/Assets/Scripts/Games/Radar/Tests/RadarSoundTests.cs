using System;
using System.IO;
using NUnit.Framework;
using UnityEngine;

namespace NeuroVida.Games.Radar.Tests
{
    /// <summary>
    /// El sonido «Madera cálida» de Rescate relámpago (Tarea 65): el C# suena igual que el laboratorio que Ricardo aprobó (muestras de REFERENCIA generadas con node a partir del propio laboratorio: <c>node tools/sonido/referencia.js</c>), los 18 clips se calculan de a poco,
    /// caben en memoria (menos de 20 MB) y nada suena fuerte.
    /// </summary>
    public class RadarSoundTests
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
            public Summary mark1, flash, dock0, ping, beam, perfect, finale, trip;
        }

        private static Reference _reference;

        private static Reference Ref()
        {
            if (_reference != null) return _reference;
            string path = Path.Combine(Application.dataPath, "Scripts", "Games", "Radar", "Tests", "RadarSoundReference.json");
            Assert.IsTrue(File.Exists(path), "falta " + path + " (se genera con: node tools/sonido/referencia.js)");
            return _reference = JsonUtility.FromJson<Reference>(File.ReadAllText(path));
        }

        private const float Tolerance = 1e-4f;

        private static void CheckAgainst(string label, Summary expected, RadarSfx sfx, int arg = 0, bool lab = true)
        {
            RadarSounds.RenderStereo(sfx, arg, out var left, out var right);
            Assert.AreEqual(expected.length, left.Length, label + ": el largo es el mismo (hasta el redondeo de los segundos)");
            Assert.AreEqual(left.Length, right.Length);
            double sl = 0, sr = 0, pk = 0;
            for (int i = 0; i < left.Length; i++)
            {
                sl += Math.Abs((double)left[i]);
                sr += Math.Abs((double)right[i]);
                pk = Math.Max(pk, Math.Max(Math.Abs((double)left[i]), Math.Abs((double)right[i])));
            }
            Assert.AreEqual(expected.peak, pk, 1e-6, label + ": el pico (el nivel del laboratorio)");
            Assert.AreEqual(expected.sumAbsL, sl, expected.sumAbsL * 1e-4, label + ": toda la señal izquierda (suma de valores absolutos)");
            Assert.AreEqual(expected.sumAbsR, sr, expected.sumAbsR * 1e-4, label + ": toda la señal derecha");
            for (int i = 0; i < expected.tailL.Length; i++) Assert.AreEqual(expected.tailL[i], left[left.Length - 40 + i], Tolerance, label + ": la cola, muestra " + i);
            if (expected.headL != null && expected.headL.Length > 0)
                for (int i = 0; i < expected.headL.Length; i++) Assert.AreEqual(expected.headL[i], left[i], Tolerance, label + ": las primeras " + expected.headL.Length + " muestras, la " + i);
            if (expected.headR != null && expected.headR.Length > 0)
                for (int i = 0; i < expected.headR.Length; i++) Assert.AreEqual(expected.headR[i], right[i], Tolerance, label + ": canal derecho, muestra " + i);
        }

        [Test]
        public void TheRandomGeneratorGivesTheLaboratorySeries()
        {
            var r = new NeuroVida.Games.Shared.SoundKit.Rng(7);
            var expected = Ref().rng7;
            for (int i = 0; i < expected.Length; i++) Assert.AreEqual(expected[i], r.Next(), 1e-12, "mulberry32 con la semilla 7, valor " + i);
        }

        [Test] public void MarkOne_SoundsLikeTheLaboratory() => CheckAgainst("mark(1)", Ref().mark1, RadarSfx.Mark, 1);
        [Test] public void TheFlash_SoundsLikeTheLaboratory() => CheckAgainst("flash", Ref().flash, RadarSfx.Flash);
        [Test] public void DockZero_SoundsLikeTheLaboratory() => CheckAgainst("dock(0)", Ref().dock0, RadarSfx.Dock, 0);
        [Test] public void ThePing_SoundsLikeTheLaboratory() => CheckAgainst("ping", Ref().ping, RadarSfx.Ping);
        [Test] public void TheBeam_SoundsLikeTheLaboratory() => CheckAgainst("beam", Ref().beam, RadarSfx.Beam);
        [Test] public void ThePerfectRound_SoundsLikeTheLaboratory() => CheckAgainst("perfect", Ref().perfect, RadarSfx.Perfect);
        [Test] public void TheFinale_SoundsLikeTheLaboratory() => CheckAgainst("finale", Ref().finale, RadarSfx.Finale);
        [Test] public void TheTrip_IsTheBeamEveryFiftyMillisecondsAndAKalimbaAtFortyFiveHundredths() => CheckAgainst("viaje", Ref().trip, RadarSfx.Trip);

        [Test]
        public void TheClipsAreBakedOnceInSlices_AndTakeLessThan20Mb_AndNothingIsLoud()
        {
            RadarSounds.ClearCache();
            var warm = RadarSounds.Prewarm();
            int frames = 0;
            var clock = System.Diagnostics.Stopwatch.StartNew();
            while (warm.MoveNext()) frames++;
            clock.Stop();
            Assert.AreEqual(RadarSounds.BakeOrder.Length, RadarSounds.CachedCount, "los 18 sonidos del juego");
            Assert.AreEqual(18, RadarSounds.CachedCount);
            Assert.Greater(frames, 18 * 3, "se calculan de a poco (varios cuadros por clip), no de golpe");
            long bytes = RadarSounds.TotalSamplesPerChannel() * 2 * sizeof(float);
            Debug.Log("[RadarSounds] clips estéreo: " + RadarSounds.TotalSamplesPerChannel() + " muestras por canal = " + (bytes / 1048576.0).ToString("0.0") + " MB, " + frames + " cuadros de precarga, " + clock.ElapsedMilliseconds + " ms de cálculo en total (en el PC)");
            Assert.Less(bytes, 20L * 1024 * 1024, "los clips estéreo pesan menos de 20 MB");
            int before = RadarSounds.CachedCount;
            Assert.AreSame(RadarSounds.Get(RadarSfx.Ping), RadarSounds.Get(RadarSfx.Ping), "un clip por sonido");
            Assert.AreEqual(before, RadarSounds.CachedCount);
            foreach (var (sfx, arg) in RadarSounds.BakeOrder)
            {
                var clip = RadarSounds.Get(sfx, arg);
                Assert.AreEqual(2, clip.channels, clip.name + " es estéreo");
                Assert.AreEqual(44100, clip.frequency, clip.name + " a 44 100 Hz");
                var data = new float[clip.samples * clip.channels];
                clip.GetData(data, 0);
                float peak = 0f;
                foreach (float v in data) peak = Math.Max(peak, Math.Abs(v));
                Assert.Greater(peak, 0.1f, clip.name + " suena");
                Assert.LessOrEqual(peak, 0.4001f, clip.name + " no pasa de 0,40 (el nivel más alto del laboratorio)");
            }
        }

        [Test]
        public void EverySoundOfTheGameHasAClip_AndTheMarksRise()
        {
            foreach (RadarSfx sfx in Enum.GetValues(typeof(RadarSfx))) Assert.IsNotNull(RadarSounds.Get(sfx, 1), sfx.ToString());
            // marcar la 1.ª, 2.ª, 3.ª y 4.ª: kalimba do5, re5, mi5, sol5 (cada una suena distinto y la pentatónica sube)
            Assert.Less(RadarSounds.P[0], RadarSounds.P[1]);
            Assert.Less(RadarSounds.P[1], RadarSounds.P[2]);
            Assert.Less(RadarSounds.P[2], RadarSounds.P[3]);
            Assert.AreNotSame(RadarSounds.Get(RadarSfx.Mark, 1), RadarSounds.Get(RadarSfx.Mark, 3));
            Assert.AreNotSame(RadarSounds.Get(RadarSfx.Beam), RadarSounds.Get(RadarSfx.Trip), "el rayo y el viaje son sonidos distintos");
        }
    }
}
