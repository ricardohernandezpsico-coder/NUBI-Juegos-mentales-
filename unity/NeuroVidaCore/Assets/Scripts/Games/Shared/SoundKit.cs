using System;
using System.Collections.Generic;
using UnityEngine;

namespace NeuroVida.Games.Shared
{
    /// <summary>
    /// Piezas COMUNES de síntesis de sonido (Tarea 65, 9-oct; las usan «Rescate relámpago» y, después, el motor de Piloto): el generador al azar <see cref="Rng"/> (mulberry32), el filtro <see cref="Biquad"/> (recetas de R. Bristow-Johnson: paso bajo, paso alto y de banda,
    /// con la frecuencia cambiable muestra a muestra), el armado de muestras (<see cref="Render"/>, <see cref="Add"/>, <see cref="Seq"/>, <see cref="Env"/>), la reverberación estéreo Freeverb de Jezar con las afinaciones estándar (<see cref="ReverbJob"/>), el cierre
    /// <see cref="Finish"/> (pico al nivel pedido y fundido de 30 ms) y el bucle sin costura <see cref="Loopify"/>. Es un PORT 1:1 de las piezas del laboratorio de sonido (docs/previews/sonido-laboratorio.html): mismas fórmulas, mismas semillas, mismos redondeos (las muestras
    /// se guardan en <c>float</c> como el <c>Float32Array</c> del laboratorio y se calculan en <c>double</c>), de modo que los clips suenan igual que lo que Ricardo aprobó; una prueba lo comprueba contra muestras de referencia generadas con node (tools/sonido/referencia.js).
    /// Las RECETAS de cada juego (kalimba, marimba, el rayo tractor…) viven en el juego. Lógica pura salvo <see cref="ToClip"/>.
    /// </summary>
    public static class SoundKit
    {
        public const int Rate = 44100;
        public const double Tau = Math.PI * 2.0;

        // ------------------------------------------------------------------ generador al azar (mulberry32)

        /// <summary>mulberry32: el generador del laboratorio. Misma semilla, misma serie (de 0 a 1, sin llegar a 1).</summary>
        public sealed class Rng
        {
            private int _a;

            public Rng(uint seed) { _a = unchecked((int)seed); }

            public double Next()
            {
                unchecked
                {
                    _a += 0x6D2B79F5;
                    int t = (_a ^ (int)((uint)_a >> 15)) * (1 | _a);
                    t = (t + (t ^ (int)((uint)t >> 7)) * (61 | t)) ^ t;
                    return (uint)(t ^ (int)((uint)t >> 14)) / 4294967296.0;
                }
            }
        }

        // ------------------------------------------------------------------ filtro biquad (RBJ)

        public enum FilterType { LowPass, HighPass, BandPass }

        /// <summary>Un filtro de segundo orden. <see cref="Set"/> cambia la frecuencia (y, si se pide, la Q) entre muestra y muestra: así se hacen los barridos.</summary>
        public sealed class Biquad
        {
            private readonly FilterType _type;
            private readonly double _q;
            private double _b0, _b1, _b2, _a1, _a2, _x1, _x2, _y1, _y2;

            public Biquad(FilterType type, double frequency, double q)
            {
                _type = type;
                _q = q;
                Set(frequency, q);
            }

            public void Set(double frequency) => Set(frequency, _q);

            public void Set(double frequency, double q)
            {
                double w = Tau * Math.Min(Math.Max(frequency, 20.0), Rate * 0.45) / Rate, c = Math.Cos(w), s = Math.Sin(w), al = s / (2.0 * q);
                double n0, n1, n2;
                if (_type == FilterType.LowPass) { n0 = (1.0 - c) / 2.0; n1 = 1.0 - c; n2 = (1.0 - c) / 2.0; }
                else if (_type == FilterType.HighPass) { n0 = (1.0 + c) / 2.0; n1 = -(1.0 + c); n2 = (1.0 + c) / 2.0; }
                else { n0 = al; n1 = 0.0; n2 = -al; }
                double d0 = 1.0 + al;
                _b0 = n0 / d0;
                _b1 = n1 / d0;
                _b2 = n2 / d0;
                _a1 = -2.0 * c / d0;
                _a2 = (1.0 - al) / d0;
            }

            public double Run(double x)
            {
                double y = _b0 * x + _b1 * _x1 + _b2 * _x2 - _a1 * _y1 - _a2 * _y2;
                _x2 = _x1;
                _x1 = x;
                _y2 = _y1;
                _y1 = y;
                return y;
            }
        }

        // ------------------------------------------------------------------ armado de muestras

        /// <summary>La envolvente de ataque lineal (a s) y caída exponencial (constante tau): 0 antes de t = 0.</summary>
        public static double Env(double t, double attack, double tau) => t < 0.0 ? 0.0 : (t < attack ? t / attack : Math.Exp(-(t - attack) / tau));

        /// <summary>Un arreglo en silencio de <paramref name="seconds"/> s (redondeado hacia arriba, como el laboratorio).</summary>
        public static float[] Blank(double seconds) => new float[(int)Math.Ceiling(seconds * Rate)];

        /// <summary>Calcula <paramref name="seconds"/> s de muestras llamando a <paramref name="fn"/>(t) en cada una (t en segundos).</summary>
        public static float[] Render(double seconds, Func<double, double> fn)
        {
            int n = (int)Math.Ceiling(seconds * Rate);
            var output = new float[n];
            for (int i = 0; i < n; i++) output[i] = (float)fn(i / (double)Rate);
            return output;
        }

        /// <summary>Suma <paramref name="src"/> sobre <paramref name="dst"/> desde <paramref name="at"/> s, con ganancia <paramref name="gain"/> (lo que no cabe se descarta). Devuelve <paramref name="dst"/>.</summary>
        public static float[] Add(float[] dst, float[] src, double at = 0.0, double gain = 1.0)
        {
            int o = (int)Math.Floor(at * Rate + 0.5);
            for (int i = 0; i < src.Length && o + i < dst.Length; i++) dst[o + i] = (float)(dst[o + i] + src[i] * gain);
            return dst;
        }

        /// <summary>Una nota tras otra cada <paramref name="gap"/> s; <paramref name="make"/> recibe la frecuencia y su lugar. Sin <paramref name="total"/>, el largo es notas × gap + 1,4 s.</summary>
        public static float[] Seq(IList<double> notes, double gap, Func<double, int, float[]> make, double total = double.NaN)
        {
            var x = Blank(double.IsNaN(total) ? notes.Count * gap + 1.4 : total);
            for (int i = 0; i < notes.Count; i++) Add(x, make(notes[i], i), i * gap);
            return x;
        }

        // ------------------------------------------------------------------ reverberación estéreo (Freeverb de Jezar, afinaciones estándar)

        /// <summary>room: tamaño de la sala (0-1) · damp: amortiguación (0-1) · wet: mezcla (0 = sin reverberación) · tail: segundos que se agregan al final para la cola · width: apertura estéreo (0-1).</summary>
        public struct ReverbSettings
        {
            public double Room, Damp, Wet, Tail, Width;

            public ReverbSettings(double room, double damp, double wet, double tail, double width)
            {
                Room = room;
                Damp = damp;
                Wet = wet;
                Tail = tail;
                Width = width;
            }
        }

        private static readonly int[] CombSizes = { 1116, 1188, 1277, 1356, 1422, 1491, 1557, 1617 }, AllPassSizes = { 556, 441, 341, 225 };

        private sealed class Comb
        {
            private readonly float[] _buffer;
            private int _index;
            private double _store;

            public Comb(int size) { _buffer = new float[size]; }

            public double Process(double x, double feedback, double damping)
            {
                double o = _buffer[_index];
                _store = o * (1.0 - damping) + _store * damping;
                _buffer[_index] = (float)(x + _store * feedback);
                if (++_index >= _buffer.Length) _index = 0;
                return o;
            }
        }

        private sealed class AllPass
        {
            private readonly float[] _buffer;
            private int _index;

            public AllPass(int size) { _buffer = new float[size]; }

            public double Process(double x)
            {
                double bo = _buffer[_index], o = -x + bo;
                _buffer[_index] = (float)(x + bo * 0.5);
                if (++_index >= _buffer.Length) _index = 0;
                return o;
            }
        }

        /// <summary>
        /// La reverberación calculada POR PEDAZOS: <see cref="Run"/> avanza un número de muestras (así los clips se calculan de a poco, en varios cuadros, sin trabar el arranque) y <see cref="Done"/> avisa cuándo está lista. <see cref="Reverb"/> la calcula de una vez.
        /// El resultado tiene la entrada más <see cref="ReverbSettings.Tail"/> s de cola, en estéreo.
        /// </summary>
        public sealed class ReverbJob
        {
            private readonly float[] _x, _left, _right;
            private readonly Comb[] _cl, _cr;
            private readonly AllPass[] _al, _ar;
            private readonly double _feedback, _damping, _w1, _w2;
            private readonly bool _dry;
            private int _i;

            public ReverbJob(float[] x, ReverbSettings s)
            {
                _x = x;
                int n = x.Length + (int)Math.Floor(s.Tail * Rate + 0.5);
                _left = new float[n];
                _right = new float[n];
                if (s.Wet <= 0.0)
                {
                    Array.Copy(x, _left, x.Length);
                    Array.Copy(x, _right, x.Length);
                    _dry = true;
                    return;
                }
                _cl = new Comb[8];
                _cr = new Comb[8];
                for (int c = 0; c < 8; c++) { _cl[c] = new Comb(CombSizes[c]); _cr[c] = new Comb(CombSizes[c] + 23); }
                _al = new AllPass[4];
                _ar = new AllPass[4];
                for (int a = 0; a < 4; a++) { _al[a] = new AllPass(AllPassSizes[a]); _ar[a] = new AllPass(AllPassSizes[a] + 23); }
                _feedback = s.Room * 0.28 + 0.7;
                _damping = s.Damp * 0.4;
                double w = s.Wet * 3.0;
                _w1 = w * (s.Width / 2.0 + 0.5);
                _w2 = w * ((1.0 - s.Width) / 2.0);
            }

            public bool Done => _dry || _i >= _left.Length;

            public int Length => _left.Length;

            /// <summary>Calcula hasta <paramref name="samples"/> muestras más.</summary>
            public void Run(int samples)
            {
                if (_dry) return;
                int end = (int)Math.Min((long)_left.Length, (long)_i + samples);
                for (; _i < end; _i++)
                {
                    double dry = _i < _x.Length ? _x[_i] : 0.0, input = dry * 0.015, l = 0.0, r = 0.0;
                    for (int c = 0; c < 8; c++) { l += _cl[c].Process(input, _feedback, _damping); r += _cr[c].Process(input, _feedback, _damping); }
                    for (int a = 0; a < 4; a++) { l = _al[a].Process(l); r = _ar[a].Process(r); }
                    _left[_i] = (float)(dry + l * _w1 + r * _w2);
                    _right[_i] = (float)(dry + r * _w1 + l * _w2);
                }
            }

            public void Result(out float[] left, out float[] right)
            {
                left = _left;
                right = _right;
            }
        }

        /// <summary>La reverberación de una vez (para pruebas y recetas cortas).</summary>
        public static void Reverb(float[] x, ReverbSettings s, out float[] left, out float[] right)
        {
            var job = new ReverbJob(x, s);
            job.Run(int.MaxValue);
            job.Result(out left, out right);
        }

        // ------------------------------------------------------------------ cierre y bucle

        /// <summary>Ajusta el pico a <paramref name="level"/> y funde los últimos 30 ms (sin «clic» de corte). Trabaja sobre los mismos arreglos.</summary>
        public static void Finish(float[] left, float[] right, double level)
        {
            double peak = 1e-9;
            for (int i = 0; i < left.Length; i++) peak = Math.Max(peak, Math.Max(Math.Abs((double)left[i]), Math.Abs((double)right[i])));
            double g = level / peak;
            int f = Math.Min(left.Length, (int)Math.Floor(0.03 * Rate + 0.5));
            for (int i = 0; i < left.Length; i++)
            {
                double k = i >= left.Length - f ? (left.Length - 1 - i) / (double)f : 1.0;
                left[i] = (float)(left[i] * (g * k));
                right[i] = (float)(right[i] * (g * k));
            }
        }

        /// <summary>
        /// Bucle sin costura: se calcula el largo completo (por ejemplo 6 s) y los últimos <paramref name="crossSeconds"/> s se funden sobre los primeros; el resultado es más corto en esos segundos (6 s → 4 s con 2 s de cruce) y su final empalma con su principio.
        /// </summary>
        public static void Loopify(float[] left, float[] right, out float[] loopLeft, out float[] loopRight, double crossSeconds = 2.0)
        {
            int f = (int)Math.Floor(crossSeconds * Rate + 0.5), n = left.Length - f;
            loopLeft = new float[n];
            loopRight = new float[n];
            Array.Copy(left, loopLeft, n);
            Array.Copy(right, loopRight, n);
            for (int i = 0; i < f && i < n; i++)
            {
                double k = i / (double)f;
                loopLeft[i] = (float)(left[i] * k + left[i + n] * (1.0 - k));
                loopRight[i] = (float)(right[i] * k + right[i + n] * (1.0 - k));
            }
        }

        /// <summary>Un clip estéreo a 44 100 Hz con las dos muestras (izquierda y derecha) entrelazadas.</summary>
        public static AudioClip ToClip(string name, float[] left, float[] right)
        {
            int n = left.Length;
            var data = new float[n * 2];
            for (int i = 0; i < n; i++)
            {
                data[i * 2] = left[i];
                data[i * 2 + 1] = right[i];
            }
            var clip = AudioClip.Create(name, n, 2, Rate, false);
            clip.SetData(data, 0);
            return clip;
        }
    }
}
