using System;
using System.IO;
using NUnit.Framework;
using UnityEngine;

namespace NeuroVida.Games.Piloto.Tests
{
    /// <summary>
    /// El motor «Cohete» de Piloto Estelar (Tarea 66): el C# suena igual que el laboratorio del motor que Ricardo aprobó (muestras de REFERENCIA generadas con node a partir del propio laboratorio: <c>node tools/sonido/referencia-motor.js</c>), los bucles empalman sin costura,
    /// la mezcla de las tres capas conserva la potencia y el motor queda claramente por debajo de las campanas (es un fondo).
    /// </summary>
    public class PilotEngineTests
    {
        [Serializable]
        private class Summary
        {
            public int length;
            public double sumAbs, peak;
            public float[] head, mid, tail;
        }

        [Serializable]
        private class Reference
        {
            public double[] rng7;
            public Summary cruise, fast, boost;
        }

        private static Reference _reference;

        private static Reference Ref()
        {
            if (_reference != null) return _reference;
            string path = Path.Combine(Application.dataPath, "Scripts", "Games", "Piloto", "Tests", "PilotEngineReference.json");
            Assert.IsTrue(File.Exists(path), "falta " + path + " (se genera con: node tools/sonido/referencia-motor.js)");
            return _reference = JsonUtility.FromJson<Reference>(File.ReadAllText(path));
        }

        private const float Tolerance = 1e-4f;

        private static void CheckAgainst(string label, Summary expected, PilotSounds.EngineLayer layer)
        {
            var x = PilotSounds.MakeEngineLoop(layer);
            Assert.AreEqual(expected.length, x.Length, label + ": 4 s exactos a 44.100 Hz");
            double sum = 0, peak = 0;
            foreach (float v in x) { sum += Math.Abs((double)v); peak = Math.Max(peak, Math.Abs((double)v)); }
            Assert.AreEqual(expected.peak, peak, 1e-6, label + ": el pico (el nivel del laboratorio)");
            Assert.AreEqual(expected.sumAbs, sum, expected.sumAbs * 1e-4, label + ": toda la señal (suma de valores absolutos)");
            for (int i = 0; i < expected.head.Length; i++) Assert.AreEqual(expected.head[i], x[i], Tolerance, label + ": las primeras " + expected.head.Length + " muestras, la " + i);
            for (int i = 0; i < expected.mid.Length; i++) Assert.AreEqual(expected.mid[i], x[88200 + i], Tolerance, label + ": el medio, muestra " + i);
            for (int i = 0; i < expected.tail.Length; i++) Assert.AreEqual(expected.tail[i], x[x.Length - 10 + i], Tolerance, label + ": el final, muestra " + i);
        }

        [Test]
        public void TheRandomGeneratorGivesTheLaboratorySeries()
        {
            var r = new NeuroVida.Games.Shared.SoundKit.Rng(7);
            var expected = Ref().rng7;
            for (int i = 0; i < expected.Length; i++) Assert.AreEqual(expected[i], r.Next(), 1e-12, "mulberry32 con la semilla 7, valor " + i);
        }

        [Test] public void TheCruiseLayer_SoundsLikeTheLaboratory() => CheckAgainst("crucero", Ref().cruise, PilotSounds.EngineLayer.Cruise);
        [Test] public void TheFastLayer_SoundsLikeTheLaboratory() => CheckAgainst("rápido", Ref().fast, PilotSounds.EngineLayer.Fast);
        [Test] public void TheBoostLayer_SoundsLikeTheLaboratory() => CheckAgainst("hiperimpulso", Ref().boost, PilotSounds.EngineLayer.Boost);

        [Test]
        public void TheLoopsAreSeamless_FourSecondsMonoWithThePeaksOfTheLaboratory()
        {
            var peaks = new[] { 0.46, 0.50, 0.32 };
            int index = 0;
            foreach (PilotSounds.EngineLayer layer in Enum.GetValues(typeof(PilotSounds.EngineLayer)))
            {
                var clip = PilotSounds.EngineLoop(layer);
                Assert.AreEqual(1, clip.channels, layer + ": mono");
                Assert.AreEqual(44100, clip.frequency);
                Assert.AreEqual(4 * 44100, clip.samples, layer + ": 4 s exactos");
                var x = PilotSounds.MakeEngineLoop(layer);
                double peak = 0, steps = 0;
                for (int i = 0; i < x.Length; i++)
                {
                    peak = Math.Max(peak, Math.Abs((double)x[i]));
                    if (i > 0) steps += ((double)x[i] - x[i - 1]) * ((double)x[i] - x[i - 1]);
                }
                Assert.AreEqual(peaks[index++], peak, 1e-4, layer + ": pico");
                double rmsStep = Math.Sqrt(steps / (x.Length - 1)), seam = Math.Abs((double)x[0] - x[x.Length - 1]);
                Assert.Less(seam, 5 * rmsStep + 1e-6, layer + ": el final empalma con el principio (el salto es como cualquier otro paso entre muestras)");
            }
        }

        [Test]
        public void TheThreeLayersAreDifferentSounds()
        {
            var a = PilotSounds.MakeEngineLoop(PilotSounds.EngineLayer.Cruise);
            var b = PilotSounds.MakeEngineLoop(PilotSounds.EngineLayer.Fast);
            var c = PilotSounds.MakeEngineLoop(PilotSounds.EngineLayer.Boost);
            double dab = 0, dac = 0;
            for (int i = 0; i < a.Length; i++) { dab += Math.Abs(a[i] - b[i]); dac += Math.Abs(a[i] - c[i]); }
            Assert.Greater(dab / a.Length, 0.02);
            Assert.Greater(dac / a.Length, 0.02);
        }

        [Test]
        public void TheMixKeepsConstantPowerBetweenCruiseAndFast_AndTheBoostAddsOnTop()
        {
            for (float k = 0f; k <= 1.0001f; k += 0.05f)
            {
                var m = PilotEngine.TargetMix(k, false);
                Assert.AreEqual(0.55f * 0.55f, m.Cruise * m.Cruise + m.Fast * m.Fast, 1e-5f, "potencia constante con k = " + k);
                Assert.AreEqual(0f, m.Boost);
                Assert.AreEqual(0.96f + 0.10f * k, m.Pitch, 1e-5f);
            }
            var slow = PilotEngine.TargetMix(0f, false);
            Assert.AreEqual(0.55f, slow.Cruise, 1e-5f);
            Assert.AreEqual(0f, slow.Fast, 1e-5f);
            var fast = PilotEngine.TargetMix(1f, false);
            Assert.AreEqual(0f, fast.Cruise, 1e-5f);
            Assert.AreEqual(0.55f, fast.Fast, 1e-5f);
            var hyper = PilotEngine.TargetMix(0.5f, true);
            Assert.AreEqual(0.5f, hyper.Boost, 1e-5f, "el chorro sube a 0,5");
            Assert.AreEqual(0.96f + 0.05f + 0.04f, hyper.Pitch, 1e-5f, "y el tono sube 0,04 más");
            Assert.AreEqual(PilotEngine.TargetMix(1f, false).Pitch, PilotEngine.TargetMix(5f, false).Pitch, 1e-6f, "k pasa de 1: se queda en 1");
            Assert.AreEqual(PilotEngine.TargetMix(0f, false).Pitch, PilotEngine.TargetMix(-3f, false).Pitch, 1e-6f);
        }

        [Test]
        public void EverythingApproachesItsTargetWithAnExponentialAndNothingJumps()
        {
            Assert.AreEqual(0f, PilotEngine.Approach(0f, 1f, 0.25f, 0f), 1e-6f, "sin tiempo no se mueve");
            Assert.AreEqual(1f - Mathf.Exp(-1f), PilotEngine.Approach(0f, 1f, 0.25f, 0.25f), 1e-5f, "en una constante recorre el 63 %");
            Assert.Greater(PilotEngine.Approach(0f, 1f, 0.25f, 5f * 0.25f), 0.99f);
            float v = 0f;
            for (int i = 0; i < 60; i++)
            {
                float next = PilotEngine.Approach(v, 0.5f, 0.12f, 1f / 60f);
                Assert.GreaterOrEqual(next, v, "sube sin retroceder");
                Assert.LessOrEqual(next, 0.5f);
                v = next;
            }
            Assert.AreEqual(0.5f, v, 0.01f);
            Assert.Less(PilotEngine.TauBoostIn, PilotEngine.TauBoostOut, "el chorro entra rápido (0,12 s) y se va despacio (0,35 s)");
            Assert.AreEqual(0.25f, PilotEngine.TauMix);
            CollectionAssert.AreEqual(new[] { 0f, 1.3f, 2.7f }, PilotEngine.StartSeconds, "las tres capas arrancan en fases distintas");
        }

        private static double Rms(float[] data, int from, int count, double gain)
        {
            double s = 0;
            for (int i = 0; i < count; i++) { double v = data[from + i] * gain; s += v * v; }
            return Math.Sqrt(s / count);
        }

        [Test]
        public void TheEngineIsABackground_ClearlyBelowTheCatchBellAndTheNewMissionNotice()
        {
            var cruise = PilotSounds.MakeEngineLoop(PilotSounds.EngineLayer.Cruise);
            var fast = PilotSounds.MakeEngineLoop(PilotSounds.EngineLayer.Fast);
            var boost = PilotSounds.MakeEngineLoop(PilotSounds.EngineLayer.Boost);
            int n = cruise.Length;
            double worstRms = 0, worstPeak = 0;
            string table = "";
            foreach (var (k, hyper) in new[] { (0f, false), (0.5f, false), (1f, false), (0.5f, true), (1f, true) })
            {
                var m = PilotEngine.TargetMix(k, hyper);
                double sum = 0, peak = 0;
                for (int i = 0; i < n; i++)
                {
                    // las tres capas desde sus puntos de arranque (0, 1,3 y 2,7 s), con el nivel general del motor
                    double v = (cruise[i] * m.Cruise + fast[(i + (int)(1.3 * 44100)) % n] * m.Fast + boost[(i + (int)(2.7 * 44100)) % n] * m.Boost) * PilotEngine.MasterLevel;
                    sum += v * v;
                    peak = Math.Max(peak, Math.Abs(v));
                }
                double rms = Math.Sqrt(sum / n);
                worstRms = Math.Max(worstRms, rms);
                worstPeak = Math.Max(worstPeak, peak);
                table += "k=" + k + (hyper ? " hiper" : "") + ": RMS " + rms.ToString("0.000") + " pico " + peak.ToString("0.000") + "; ";
            }
            // las campanas: se mide lo que suena de verdad (clip × volumen con que el juego las toca) en su parte más fuerte
            var catchClip = PilotSounds.Catch(0);
            var mission = PilotSounds.MissionChange();
            var catchData = new float[catchClip.samples];
            catchClip.GetData(catchData, 0);
            var missionData = new float[mission.samples];
            mission.GetData(missionData, 0);
            double catchRms = Rms(catchData, 0, (int)(0.3 * 44100), 0.8), missionRms = Rms(missionData, 0, (int)(0.8 * 44100), 0.85);
            double catchPeak = 0, missionPeak = 0;
            foreach (float v in catchData) catchPeak = Math.Max(catchPeak, Math.Abs(v * 0.8));
            foreach (float v in missionData) missionPeak = Math.Max(missionPeak, Math.Abs(v * 0.85));
            Debug.Log("[PilotEngine] nivel general " + PilotEngine.MasterLevel + " · motor: " + table + "· campana de atrapar: RMS " + catchRms.ToString("0.000") + " pico " + catchPeak.ToString("0.000") + " · aviso de misión: RMS " + missionRms.ToString("0.000") + " pico " + missionPeak.ToString("0.000"));
            Assert.AreEqual(0.35f, PilotEngine.MasterLevel, 1e-6f, "el nivel general que se eligió al medir (con 0,7 el motor llegaba a la fuerza del aviso de misión)");
            Assert.Less(worstRms, 0.5 * catchRms, "el motor (RMS, el peor caso: hiperimpulso) queda al menos 6 dB por debajo de la campana de atrapar");
            Assert.Less(worstRms, 0.51 * missionRms, "y del aviso de misión nueva");
            Assert.Less(worstPeak, 0.65 * catchPeak, "también en el pico");
            Assert.Less(worstPeak, 0.65 * missionPeak);
        }

        [Test]
        public void TheOldEngineIsGone()
        {
            Assert.IsNull(typeof(PilotSounds).GetMethod("Engine"), "el motor viejo (diente de sierra de 55 Hz) ya no existe");
        }
    }
}
