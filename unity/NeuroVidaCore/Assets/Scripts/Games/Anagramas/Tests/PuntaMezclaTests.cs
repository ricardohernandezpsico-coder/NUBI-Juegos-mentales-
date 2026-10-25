using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using NeuroVida.Games.Shared;
using NUnit.Framework;
using UnityEngine;

namespace NeuroVida.Games.Anagramas.Tests
{
    /// <summary>
    /// La mezcla de lo que suena al resolver una palabra en «En la punta de la lengua» (Tarea 170, 25-oct): se calcula OFFLINE con los clips reales de la paleta (<see cref="WoodSounds"/>, 22.050 Hz, con el volumen maestro) y el volumen con que se reproducen
    /// (<see cref="WoodKit.PlayVolume"/> = 0,8 por <c>PlayOneShot</c>), puestos en el tiempo en que los pide <c>PuntaGameController</c>: la nota de la última ficha (−0,26 s: la validación espera 0,26 s), la cascada (<c>LightUp</c>: la nota k+1 de la
    /// pentatónica con retardo k · 0,09 s y <see cref="PuntaSounds.CascadeGainDb"/>), el acorde (<c>ChordAfter</c>: <see cref="WoodSfx.Perfect"/> a n · 0,09 + 0,3 s), el «Nuevo» del lucero (<see cref="WoodSfx.New"/> a −3 dB, 0,7 s después de que sale)
    /// y, al subir de etapa, el arpegio <see cref="WoodSfx.LevelUp"/> con <see cref="PuntaSounds.LevelUpGainDb"/> a la vez que arranca la cascada.
    /// El diagnóstico de Ricardo («el sonido me da la sensación de que se satura») midió −2,5 dBFS: 4,4 dB más fuerte que el final de la partida (−6,9 dBFS, el sonido más fuerte de la paleta). Estas pruebas fijan que la mezcla NO pase del pico del final.
    /// <c>NUBI_PUNTA_MEZCLA_INFORME=1</c> deja además un informe y los WAV en <c>unity/test-results/punta-mezcla/</c> (fuera de git).
    /// </summary>
    public class PuntaMezclaTests
    {
        private const int Rate = WoodSounds.ClipRate;
        private static readonly float[] Penta = PuntaSounds.Penta;

        private sealed class Voice
        {
            public string Name;
            public double At;
            public float[] L, R;
            public float Vol;
            public float Peak;          // el pico del clip ya con el volumen de reproducción
        }

        // cada clip se calcula UNA vez (son ~14 distintos): la reverberación de cada uno tarda
        private static readonly Dictionary<string, KeyValuePair<float[], float[]>> Cache = new Dictionary<string, KeyValuePair<float[], float[]>>();

        private static KeyValuePair<float[], float[]> Clip(string key, Func<WoodSounds.Recipe> recipe, WoodSfx? sfx)
        {
            if (Cache.TryGetValue(key, out var c)) return c;
            float[] l, r;
            if (sfx.HasValue) WoodSounds.RenderForClip(sfx.Value, 0, out l, out r); else WoodSounds.RenderForClip(recipe(), false, out l, out r);
            return Cache[key] = new KeyValuePair<float[], float[]>(l, r ?? l);
        }

        /// <summary>La nota de la casilla [index] de la pentatónica: kalimba, caída de 0,3 s (la de <c>WoodCore.PlayNote</c> por omisión: el mismo clip que la cascada).</summary>
        private static Voice Note(string name, double at, int index, float gainDb = 0f)
        {
            var c = Clip("nota" + index, () => WoodSounds.MakeNote(Penta[index], WoodVoice.Kalimba, 0.3), null);
            return Wrap(name, at, c.Key, c.Value, gainDb);
        }

        private static Voice Make(string name, double at, WoodSfx sfx, float gainDb = 0f)
        {
            var c = Clip("sfx" + sfx, null, sfx);
            return Wrap(name, at, c.Key, c.Value, gainDb);
        }

        private static Voice Wrap(string name, double at, float[] l, float[] r, float gainDb)
        {
            float vol = WoodKit.PlayVolume * Mathf.Pow(10f, gainDb / 20f);
            float peak = 0f;
            for (int i = 0; i < l.Length; i++) peak = Mathf.Max(peak, Mathf.Abs(l[i]), Mathf.Abs(r[i]));
            return new Voice { Name = name, At = at, L = l, R = r, Vol = vol, Peak = peak * vol };
        }

        private static float Db(float linear) => linear <= 1e-9f ? -180f : 20f * Mathf.Log10(linear);

        private sealed class Result
        {
            public float Peak;            // el pico de la suma (1,0 = 0 dBFS)
            public int Clipped;           // muestras (de los dos canales) que pasan de ±1
            public int MaxVoices;         // cuántos clips suenan a la vez en lo más cargado (con más de −40 dBFS)
            public float RmsPeakDb;       // el RMS más alto en ventanas de 50 ms (dBFS)
            public float[] L, R;
        }

        private static Result Mix(List<Voice> voices)
        {
            double t0 = double.MaxValue, tEnd = 0;
            foreach (var v in voices) { t0 = Math.Min(t0, v.At); tEnd = Math.Max(tEnd, v.At + (double)v.L.Length / Rate); }
            int n = (int)Math.Ceiling((tEnd - t0) * Rate) + 1;
            var l = new float[n]; var r = new float[n];
            var starts = new int[voices.Count];
            for (int k = 0; k < voices.Count; k++)
            {
                var v = voices[k];
                int s = (int)Math.Round((v.At - t0) * Rate); starts[k] = s;
                for (int i = 0; i < v.L.Length && s + i < n; i++) { l[s + i] += v.L[i] * v.Vol; r[s + i] += v.R[i] * v.Vol; }
            }
            var res = new Result { L = l, R = r };
            for (int i = 0; i < n; i++)
            {
                res.Peak = Mathf.Max(res.Peak, Mathf.Abs(l[i]), Mathf.Abs(r[i]));
                if (Mathf.Abs(l[i]) > 1f) res.Clipped++;
                if (Mathf.Abs(r[i]) > 1f) res.Clipped++;
            }
            int win = Rate / 20; double best = 0;
            for (int s = 0; s + win <= n; s += win / 2)
            {
                double acc = 0;
                for (int i = s; i < s + win; i++) acc += 0.5 * (l[i] * l[i] + r[i] * r[i]);
                best = Math.Max(best, acc / win);
            }
            res.RmsPeakDb = best <= 1e-12 ? -120f : (float)(10.0 * Math.Log10(best));
            for (int i = 0; i < n; i += Rate / 100)
            {
                int on = 0;
                for (int k = 0; k < voices.Count; k++)
                {
                    var v = voices[k]; int idx = i - starts[k];
                    if (idx < 0 || idx >= v.L.Length) continue;
                    if (Mathf.Max(Mathf.Abs(v.L[idx]), Mathf.Abs(v.R[idx])) * v.Vol > 0.01f) on++;
                }
                res.MaxVoices = Math.Max(res.MaxVoices, on);
            }
            return res;
        }

        /// <summary>Lo que suena al resolver una palabra de [n] letras con movimiento (con «quitar animaciones» el «Nuevo» suena al instante y no hay más estímulos: no es la peor mezcla).</summary>
        private static List<Voice> Word(int n, bool levelUp, bool vista, float cascadeDb, float levelUpDb)
        {
            var vs = new List<Voice>
            {
                Note("última ficha (nota de su casilla)", -0.26, Mathf.Min(Penta.Length - 1, n - 1)),
            };
            if (levelUp) vs.Add(Make("subir de etapa (arpegio)", 0, WoodSfx.LevelUp, levelUpDb));
            for (int k = 0; k < n; k++) vs.Add(Note("cascada " + (k + 1), k * 0.09, Mathf.Min(Penta.Length - 1, k + 1), cascadeDb));
            double tail = n * 0.09 + 0.5;
            if (!vista) vs.Add(Make("acorde (Perfect)", Math.Max(0.0, tail - 0.2), WoodSfx.Perfect));
            vs.Add(Make("Nuevo (lucero llega)", tail + 0.7, WoodSfx.New, -3f));
            return vs;
        }

        private static float FinishPeak() => Make("final de la partida", 0, WoodSfx.Finish).Peak;

        [Test]
        public void Las_ganancias_de_Punta_son_de_las_permitidas_y_ya_no_son_cero()
        {
            foreach (float g in new[] { PuntaSounds.CascadeGainDb, PuntaSounds.LevelUpGainDb }) Assert.That(WoodCore.AllowedGainsDb, Does.Contain(g), "WoodCore solo admite 0, -3, -6 o -9 dB: " + g);
            Assert.AreEqual(-6f, PuntaSounds.CascadeGainDb, "la cascada suena 6 dB más baja (Ricardo, 25-oct)");
            Assert.AreEqual(-3f, PuntaSounds.LevelUpGainDb, "y el arpegio de subir de etapa, 3 dB más bajo");
        }

        [Test]
        public void La_mezcla_de_una_palabra_nunca_es_mas_fuerte_que_el_final_de_la_partida_de_4_a_10_letras()
        {
            float ceiling = FinishPeak() * 1.007f;                    // el pico del final de la partida (−6,9 dBFS) con 0,06 dB de tolerancia
            var worst = new StringBuilder();
            for (int n = 4; n <= 10; n++)
                foreach (bool levelUp in new[] { false, true })
                    foreach (bool vista in new[] { false, true })
                    {
                        var m = Mix(Word(n, levelUp, vista, PuntaSounds.CascadeGainDb, PuntaSounds.LevelUpGainDb));
                        worst.AppendLine(string.Format("{0} letras{1}{2}: pico {3:0.0} dBFS, {4} voces, RMS 50 ms {5:0.0} dBFS", n, levelUp ? " + etapa" : "", vista ? " (Vista)" : "", Db(m.Peak), m.MaxVoices, m.RmsPeakDb));
                        Assert.AreEqual(0, m.Clipped, n + " letras" + (levelUp ? " + etapa" : "") + ": la mezcla no puede cortarse");
                        Assert.LessOrEqual(m.Peak, ceiling, n + " letras" + (levelUp ? " + etapa" : "") + (vista ? " (Vista)" : "") + ": la mezcla pica en " + Db(m.Peak).ToString("0.0") + " dBFS, más fuerte que el final de la partida (" + Db(ceiling).ToString("0.0") + ")");
                    }
            Debug.Log("[PuntaMezcla] la peor de cada caso:\n" + worst);
        }

        /// <summary>La prueba no es vacía: SIN las ganancias (como sonaba antes de la Tarea 170) la mezcla de 7 letras al subir de etapa pasa del pico del final por más de 3 dB.</summary>
        [Test]
        public void Sin_las_ganancias_la_mezcla_de_antes_pasaba_del_pico_del_final()
        {
            float ceiling = FinishPeak();
            var before = Mix(Word(7, true, false, 0f, 0f));
            Debug.Log("[PuntaMezcla] antes: pico " + Db(before.Peak).ToString("0.0") + " dBFS; final de la partida " + Db(ceiling).ToString("0.0") + " dBFS");
            Assert.Greater(before.Peak, ceiling * 1.41f, "antes de la Tarea 170 la mezcla pasaba del final por más de 3 dB");
            Assert.AreEqual(0, before.Clipped, "pero nunca se cortaba: «se satura» era densidad y volumen sumado, no recorte");
        }

        /// <summary>Con <c>NUBI_PUNTA_MEZCLA_INFORME=1</c>: el informe y los WAV (lo que sonaba y lo que suena) en unity/test-results/punta-mezcla/.</summary>
        [Test]
        public void Deja_el_informe_y_los_WAV_si_se_pide()
        {
            if (Environment.GetEnvironmentVariable("NUBI_PUNTA_MEZCLA_INFORME") != "1") return;          // solo con NUBI_PUNTA_MEZCLA_INFORME=1 (no Assert.Ignore: un resultado «Skipped:Ignored» confunde a verificar-todo.sh)
            string dir = Path.GetFullPath(Path.Combine(Application.dataPath, "..", "..", "test-results", "punta-mezcla"));
            Directory.CreateDirectory(dir);
            var sb = new StringBuilder();
            sb.AppendLine("Final de la partida (el techo): " + Db(FinishPeak()).ToString("0.0") + " dBFS");
            foreach (int n in new[] { 4, 5, 7, 10 })
                foreach (bool lu in new[] { false, true })
                {
                    var a = Mix(Word(n, lu, false, 0f, 0f)); var b = Mix(Word(n, lu, false, PuntaSounds.CascadeGainDb, PuntaSounds.LevelUpGainDb));
                    sb.AppendLine(string.Format("{0} letras{1}: antes pico {2:0.0} dBFS, RMS {3:0.0}, {4} voces | ahora pico {5:0.0} dBFS, RMS {6:0.0}, {7} voces", n, lu ? " + etapa" : "", Db(a.Peak), a.RmsPeakDb, a.MaxVoices, Db(b.Peak), b.RmsPeakDb, b.MaxVoices));
                    if (n == 7) { WriteWav(Path.Combine(dir, "antes-7" + (lu ? "-etapa" : "") + ".wav"), a); WriteWav(Path.Combine(dir, "ahora-7" + (lu ? "-etapa" : "") + ".wav"), b); }
                }
            File.WriteAllText(Path.Combine(dir, "informe.txt"), sb.ToString(), new UTF8Encoding(false));
        }

        private static void WriteWav(string path, Result m)
        {
            using (var fs = new FileStream(path, FileMode.Create)) using (var w = new BinaryWriter(fs))
            {
                int n = m.L.Length, bytes = n * 4;
                w.Write(Encoding.ASCII.GetBytes("RIFF")); w.Write(36 + bytes); w.Write(Encoding.ASCII.GetBytes("WAVEfmt "));
                w.Write(16); w.Write((short)1); w.Write((short)2); w.Write(Rate); w.Write(Rate * 4); w.Write((short)4); w.Write((short)16);
                w.Write(Encoding.ASCII.GetBytes("data")); w.Write(bytes);
                for (int i = 0; i < n; i++)
                {
                    w.Write((short)Mathf.RoundToInt(Mathf.Clamp(m.L[i], -1f, 1f) * 32767f));      // el altavoz corta en ±1: así se oiría
                    w.Write((short)Mathf.RoundToInt(Mathf.Clamp(m.R[i], -1f, 1f) * 32767f));
                }
            }
        }
    }
}
