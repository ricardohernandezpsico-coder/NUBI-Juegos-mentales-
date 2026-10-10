using System;
using NUnit.Framework;
using NeuroVida.Games.Shared;
using static NeuroVida.Games.Shared.SoundKit;

namespace NeuroVida.Games.Tests
{
    /// <summary>Las piezas comunes de síntesis (Games/Shared/SoundKit.cs, Tarea 65): el generador, el filtro, el armado de muestras, la reverberación Freeverb, el cierre y el bucle sin costura.</summary>
    public class SoundKitTests
    {
        [Test]
        public void Mulberry32_GivesTheSameSeriesAsTheLaboratory_AndIsReproducible()
        {
            var r = new Rng(7);
            Assert.AreEqual(0.011704753153026104, r.Next(), 1e-12);
            Assert.AreEqual(0.06195825757458806, r.Next(), 1e-12);
            Assert.AreEqual(0.97690763277933, r.Next(), 1e-12);
            var a = new Rng(123456789u);
            var b = new Rng(123456789u);
            for (int i = 0; i < 100; i++)
            {
                double x = a.Next();
                Assert.AreEqual(x, b.Next());
                Assert.GreaterOrEqual(x, 0.0);
                Assert.Less(x, 1.0);
            }
        }

        [Test]
        public void TheEnvelopeAttacksLinearlyAndDecaysExponentially()
        {
            Assert.AreEqual(0.0, Env(-0.1, 0.01, 0.2));
            Assert.AreEqual(0.0, Env(0.0, 0.01, 0.2), 1e-12);
            Assert.AreEqual(0.5, Env(0.005, 0.01, 0.2), 1e-12);
            Assert.AreEqual(1.0, Env(0.01, 0.01, 0.2), 1e-12);
            Assert.AreEqual(Math.Exp(-1.0), Env(0.21, 0.01, 0.2), 1e-12);
        }

        private static double Amplitude(FilterType type, double filterHz, double sineHz)
        {
            var f = new Biquad(type, filterHz, 0.7071);
            double peak = 0;
            for (int i = 0; i < Rate; i++)
            {
                double y = f.Run(Math.Sin(Tau * sineHz * i / Rate));
                if (i > Rate / 2) peak = Math.Max(peak, Math.Abs(y));            // ya en régimen
            }
            return peak;
        }

        [Test]
        public void TheBiquad_PassesWhatItShould()
        {
            Assert.Greater(Amplitude(FilterType.LowPass, 1000, 100), 0.95, "el paso bajo deja pasar lo grave");
            Assert.Less(Amplitude(FilterType.LowPass, 1000, 8000), 0.05, "y frena lo agudo");
            Assert.Greater(Amplitude(FilterType.HighPass, 1000, 8000), 0.95, "el paso alto deja pasar lo agudo");
            Assert.Less(Amplitude(FilterType.HighPass, 1000, 100), 0.05, "y frena lo grave");
            Assert.Greater(Amplitude(FilterType.BandPass, 1000, 1000), Amplitude(FilterType.BandPass, 1000, 100) * 5, "el de banda prefiere su frecuencia");
        }

        [Test]
        public void TheBiquadFrequencyCanChangeSampleBySample_AndIsClampedToTheAudibleRange()
        {
            var f = new Biquad(FilterType.LowPass, 100, 0.7);
            for (int i = 0; i < 1000; i++)
            {
                f.Set(100 + i * 10);
                Assert.IsFalse(double.IsNaN(f.Run(0.5)));
            }
            f.Set(1e9);                                  // más allá de lo audible: se limita (0,45 de la frecuencia de muestreo)
            f.Set(-5);                                   // y por debajo de 20 Hz
            Assert.IsFalse(double.IsNaN(f.Run(0.5)));
        }

        [Test]
        public void BlankRenderAddAndSeq_BuildSamplesAsTheLaboratoryDoes()
        {
            Assert.AreEqual((int)Math.Ceiling(0.25 * Rate), Blank(0.25).Length);
            var s = Render(0.01, t => t);
            Assert.AreEqual((int)Math.Ceiling(0.01 * Rate), s.Length);
            Assert.AreEqual(1.0 / Rate, s[1], 1e-9);
            var dst = Blank(0.01);
            var src = new float[] { 1f, 2f, 3f };
            Add(dst, src, 0.001, 0.5);
            int o = (int)Math.Floor(0.001 * Rate + 0.5);
            Assert.AreEqual(0.5f, dst[o]);
            Assert.AreEqual(1.5f, dst[o + 2]);
            Assert.AreEqual(0f, dst[o + 3]);
            Add(dst, new float[10000], 0.0, 1.0);        // lo que no cabe se descarta, sin romper nada
            var x = Seq(new double[] { 100, 200 }, 0.1, (f, i) => new float[] { (float)f }, 0.5);
            Assert.AreEqual(100f, x[0]);
            Assert.AreEqual(200f, x[(int)Math.Floor(0.1 * Rate + 0.5)]);
            Assert.AreEqual((int)Math.Ceiling((2 * 0.1 + 1.4) * Rate), Seq(new double[] { 1, 2 }, 0.1, (f, i) => new float[1]).Length, "sin total: notas × gap + 1,4 s");
        }

        private static float[] Click() { var x = new float[2000]; x[0] = 1f; return x; }

        [Test]
        public void TheReverb_AddsATailInStereo_AndIsTheSameInSlices()
        {
            var settings = new ReverbSettings(0.7, 0.5, 0.18, 0.9, 0.8);
            var x = Click();
            Reverb(x, settings, out var l, out var r);
            Assert.AreEqual(x.Length + (int)Math.Floor(0.9 * Rate + 0.5), l.Length, "la entrada más la cola");
            Assert.AreEqual(l.Length, r.Length);
            double tail = 0;
            for (int i = x.Length; i < l.Length; i++) tail += Math.Abs(l[i]) + Math.Abs(r[i]);
            Assert.Greater(tail, 0.01, "después de la entrada todavía suena la sala");
            bool differ = false;
            for (int i = 0; i < l.Length; i++) if (l[i] != r[i]) { differ = true; break; }
            Assert.IsTrue(differ, "los dos canales no son iguales (estéreo)");
            // calculada por pedazos da exactamente lo mismo
            var job = new ReverbJob(x, settings);
            int steps = 0;
            while (!job.Done) { job.Run(777); steps++; }
            job.Result(out var l2, out var r2);
            Assert.Greater(steps, 10);
            for (int i = 0; i < l.Length; i++) { Assert.AreEqual(l[i], l2[i]); Assert.AreEqual(r[i], r2[i]); }
        }

        [Test]
        public void WithoutWet_TheReverbOnlyCopiesTheSignalToBothChannels()
        {
            var x = new float[] { 0.1f, -0.2f, 0.3f };
            Reverb(x, new ReverbSettings(0.7, 0.5, 0, 0.5, 1), out var l, out var r);
            Assert.AreEqual(3 + (int)Math.Floor(0.5 * Rate + 0.5), l.Length);
            for (int i = 0; i < 3; i++) { Assert.AreEqual(x[i], l[i]); Assert.AreEqual(x[i], r[i]); }
            Assert.AreEqual(0f, l[10]);
        }

        [Test]
        public void Finish_PutsThePeakAtTheLevel_AndFadesTheLast30Ms()
        {
            var l = Render(0.2, t => 0.2 * Math.Sin(Tau * 440 * t));
            var r = Render(0.2, t => 0.05 * Math.Sin(Tau * 330 * t));
            Finish(l, r, 0.34);
            double peak = 0;
            for (int i = 0; i < l.Length; i++) peak = Math.Max(peak, Math.Max(Math.Abs(l[i]), Math.Abs(r[i])));
            Assert.AreEqual(0.34, peak, 0.01, "el pico queda en el nivel (el fundido recorta algo del final)");
            Assert.AreEqual(0f, l[l.Length - 1], 1e-6f, "el último valor es 0: sin clic de corte");
            Assert.AreEqual(0f, r[r.Length - 1], 1e-6f);
        }

        [Test]
        public void Loopify_JoinsTheEndWithTheStart_WithoutASeam()
        {
            var l = Render(6.0, t => Math.Sin(Tau * 100 * t));
            var r = Render(6.0, t => Math.Sin(Tau * 150 * t));
            Loopify(l, r, out var ol, out var or, 2.0);
            Assert.AreEqual(264600 - 88200, ol.Length, "6 s con 2 s de cruce dan 4 s");
            Assert.AreEqual(4.0, ol.Length / (double)Rate, 0.001);
            Assert.AreEqual(ol.Length, or.Length);
            // el final empalma con el principio (dos muestras consecutivas de la señal original)
            Assert.Less(Math.Abs(ol[0] - ol[ol.Length - 1]), 0.1);
            Assert.Less(Math.Abs(or[0] - or[or.Length - 1]), 0.1);
        }

        [Test]
        public void ToClip_MakesAStereoClipAt44100()
        {
            var l = new float[] { 0.1f, 0.2f, 0.3f };
            var r = new float[] { -0.1f, -0.2f, -0.3f };
            var clip = ToClip("prueba", l, r);
            Assert.AreEqual(2, clip.channels);
            Assert.AreEqual(44100, clip.frequency);
            Assert.AreEqual(3, clip.samples);
            var data = new float[6];
            clip.GetData(data, 0);
            Assert.AreEqual(0.2f, data[2], 1e-4f);
            Assert.AreEqual(-0.2f, data[3], 1e-4f);
        }
    }
}
