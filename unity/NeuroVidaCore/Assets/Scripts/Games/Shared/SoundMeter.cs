using System;

namespace NeuroVida.Games.Shared
{
    /// <summary>
    /// Medidas de un sonido (Tarea 119): el pico en dBFS, la sonoridad aproximada (LUFS del momento más fuerte) y la afinación de una nota. Lógica pura, sin Unity; la usan las pruebas de la paleta (<see cref="WoodSounds"/>) y el exportador de la página de escucha para que todos los sonidos de la app queden PAREJOS.
    /// La sonoridad es la de la recomendación UIT-R BS.1770 (filtro K de dos etapas, con los coeficientes calculados por la transformación bilineal para la frecuencia de muestreo) sobre ventanas de 400 ms con salto de 100 ms; un clip más corto que 400 ms se rellena con silencio (así un toque corto suena menos fuerte que un acorde,
    /// como pasa al oído). Un clip mono se cuenta en los dos canales (así lo reproduce el teléfono). No es un medidor certificado: sirve para comparar sonidos entre sí.
    /// </summary>
    public static class SoundMeter
    {
        private const double Rate = SoundKit.Rate;

        private sealed class Biquad
        {
            private readonly double[] _b, _a;
            private double _x1, _x2, _y1, _y2;

            public Biquad(double[] b, double[] a) { _b = b; _a = a; }

            public double Run(double x)
            {
                double y = _b[0] * x + _b[1] * _x1 + _b[2] * _x2 - _a[1] * _y1 - _a[2] * _y2;
                _x2 = _x1; _x1 = x; _y2 = _y1; _y1 = y;
                return y;
            }
        }

        private static void KWeighting(out double[] b1, out double[] a1, out double[] b2, out double[] a2)
        {
            double f0 = 1681.974450955533, g = 3.999843853973347, q = 0.7071752369554196;
            double k = Math.Tan(Math.PI * f0 / Rate), vh = Math.Pow(10.0, g / 20.0), vb = Math.Pow(vh, 0.4996667741545416), d = 1.0 + k / q + k * k;
            b1 = new[] { (vh + vb * k / q + k * k) / d, 2.0 * (k * k - vh) / d, (vh - vb * k / q + k * k) / d };
            a1 = new[] { 1.0, 2.0 * (k * k - 1.0) / d, (1.0 - k / q + k * k) / d };
            f0 = 38.13547087602444; q = 0.5003270373238773;
            k = Math.Tan(Math.PI * f0 / Rate); d = 1.0 + k / q + k * k;
            b2 = new[] { 1.0, -2.0, 1.0 };
            a2 = new[] { 1.0, 2.0 * (k * k - 1.0) / d, (1.0 - k / q + k * k) / d };
        }

        /// <summary>El pico (valor absoluto más alto de los dos canales, de 0 a 1).</summary>
        public static double Peak(float[] left, float[] right = null)
        {
            double peak = 0;
            for (int i = 0; i < left.Length; i++)
            {
                peak = Math.Max(peak, Math.Abs((double)left[i]));
                if (right != null) peak = Math.Max(peak, Math.Abs((double)right[i]));
            }
            return peak;
        }

        public static double ToDb(double linear) => 20.0 * Math.Log10(Math.Max(linear, 1e-9));

        /// <summary>El LUFS del momento más fuerte (ventana de 400 ms). <paramref name="right"/> = null: mono contado en los dos canales.</summary>
        public static double MomentaryLufs(float[] left, float[] right = null)
        {
            KWeighting(out var b1, out var a1, out var b2, out var a2);
            int n = left.Length, win = (int)(0.4 * Rate), hop = (int)(0.1 * Rate);
            var channels = new[] { left, right ?? left };
            var energy = new double[2][];
            for (int ch = 0; ch < 2; ch++)
            {
                var s1 = new Biquad(b1, a1);
                var s2 = new Biquad(b2, a2);
                var sq = new double[n];
                for (int i = 0; i < n; i++)
                {
                    double y = s2.Run(s1.Run(channels[ch][i]));
                    sq[i] = y * y;
                }
                energy[ch] = sq;
            }
            double best = -1e9;
            int start = 0;
            while (true)
            {
                double sum = 0;
                for (int ch = 0; ch < 2; ch++)
                {
                    double s = 0;
                    int end = Math.Min(n, start + win);
                    for (int i = start; i < end; i++) s += energy[ch][i];
                    sum += s / win;
                }
                best = Math.Max(best, -0.691 + 10.0 * Math.Log10(Math.Max(sum, 1e-12)));
                start += hop;
                if (start >= n) break;
            }
            return best;
        }

        /// <summary>Los milisegundos hasta la primera muestra que llega a <paramref name="fraction"/> del pico: cuánto tarda en oírse el inicio del sonido.</summary>
        public static double OnsetMs(float[] left, float[] right = null, double fraction = 0.1)
        {
            double peak = Peak(left, right);
            if (peak <= 0) return 0;
            for (int i = 0; i < left.Length; i++)
            {
                double a = Math.Abs((double)left[i]);
                if (right != null) a = Math.Max(a, Math.Abs((double)right[i]));
                if (a >= fraction * peak) return i * 1000.0 / Rate;
            }
            return 0;
        }

        /// <summary>
        /// Los inicios (en ms) de un clip con VARIAS notas o golpes (la matraca de Bodega, la bocina de Correo, una cascada): cada vez que la envolvente de la DIFERENCIA SEGUNDA de las muestras (un «filtro pasa-altos»: un ataque trae agudos que enseguida se apagan, así una nota grave
        /// que todavía suena no esconde la que entra; el máximo de cada 2 ms) sube de golpe (más de 1,8 veces la ventana anterior) y llega al menos a <paramref name="thresholdDb"/> dB del pico de esa envolvente, y a lo menos <paramref name="minGapMs"/> ms después del inicio anterior. Las pruebas de los sonidos propios con secuencia fijan así sus tiempos (paso 0 de las Fases C a E).
        /// </summary>
        public static System.Collections.Generic.List<double> OnsetsMs(float[] left, float[] right = null, double thresholdDb = -30.0, double minGapMs = 40.0)
        {
            var onsets = new System.Collections.Generic.List<double>();
            int n = left.Length;
            var d = new double[n];                                              // la diferencia segunda de las muestras (la mayor de los dos canales)
            double top = 0;
            for (int i = 2; i < n; i++)
            {
                d[i] = Math.Abs((double)left[i] - 2.0 * left[i - 1] + left[i - 2]);
                if (right != null) d[i] = Math.Max(d[i], Math.Abs((double)right[i] - 2.0 * right[i - 1] + right[i - 2]));
                top = Math.Max(top, d[i]);
            }
            if (top <= 0) return onsets;
            double threshold = top * Math.Pow(10.0, thresholdDb / 20.0);
            int window = Math.Max(1, (int)Math.Round(0.002 * Rate)), gap = (int)Math.Round(minGapMs * Rate / 1000.0), last = -gap;
            double previous = 0;
            for (int s = 0; s + window <= n; s += window)
            {
                double e = 0;
                for (int i = s; i < s + window; i++) e = Math.Max(e, d[i]);
                if (e >= threshold && e > 1.8 * previous && s - last >= gap)
                {
                    onsets.Add(s * 1000.0 / Rate);
                    last = s;
                }
                previous = e;
            }
            return onsets;
        }

        /// <summary>
        /// Los milisegundos hasta la última muestra que supera <paramref name="belowPeakDb"/> dB por debajo del pico: cuánto dura de verdad el sonido. Si se da <paramref name="floorDbfs"/>, lo que queda bajo ese nivel ABSOLUTO (en dBFS) tampoco cuenta: una cola de sala de −70 dBFS no se oye aunque
        /// sea fuerte para un sonido muy suave.
        /// </summary>
        public static double DurationMs(float[] left, float[] right = null, double belowPeakDb = 50.0, double floorDbfs = -200.0)
        {
            double limit = Math.Max(Peak(left, right) * Math.Pow(10.0, -belowPeakDb / 20.0), Math.Pow(10.0, floorDbfs / 20.0));
            for (int i = left.Length - 1; i >= 0; i--)
            {
                if (Math.Abs((double)left[i]) >= limit || (right != null && Math.Abs((double)right[i]) >= limit)) return (i + 1) * 1000.0 / Rate;
            }
            return 0;
        }

        /// <summary>
        /// Qué tan «limpia» es la altura de una nota: la fracción de la energía de sus primeros 300 ms que cae en su frecuencia (Goertzel con ventana de Hann), de 0 a 1. Si baja mucho al cambiar de timbre, la nota se oye menos afinada (la altura es información del juego).
        /// </summary>
        public static double PitchClarity(float[] left, float[] right, double hz, int startMs = 5, int lengthMs = 300)
        {
            int s0 = startMs * (int)Rate / 1000, n = Math.Min(lengthMs * (int)Rate / 1000, left.Length - s0);
            if (n < 64) return 0;
            double re = 0, im = 0, sumW = 0, sumW2 = 0, sumWX2 = 0;
            for (int i = 0; i < n; i++)
            {
                double w = 0.5 - 0.5 * Math.Cos(2.0 * Math.PI * i / (n - 1));
                double x = left[s0 + i];
                if (right != null) x = 0.5 * (x + right[s0 + i]);
                double wx = w * x, ph = 2.0 * Math.PI * hz * i / Rate;
                re += wx * Math.Cos(ph);
                im -= wx * Math.Sin(ph);
                sumW += w; sumW2 += w * w; sumWX2 += wx * wx;
            }
            double power = re * re + im * im, denom = 0.5 * sumWX2 * (sumW * sumW) / sumW2;
            return denom <= 0 ? 0 : Math.Min(1.0, power / denom);
        }

        /// <summary>Los milisegundos desde el pico hasta que la envolvente (ventanas de 10 ms) cae <paramref name="db"/> dB: cuánto tarda en apagarse (y en encimarse con la nota siguiente).</summary>
        public static double DecayMs(float[] left, float[] right, double db)
        {
            int w = (int)Rate / 100, blocks = left.Length / w;
            if (blocks < 2) return 0;
            var env = new double[blocks];
            double top = 1e-12;
            int topAt = 0;
            for (int b = 0; b < blocks; b++)
            {
                double s = 0;
                for (int i = b * w; i < (b + 1) * w; i++)
                {
                    s += (double)left[i] * left[i];
                    if (right != null) s += (double)right[i] * right[i];
                }
                env[b] = Math.Sqrt(s / (w * (right == null ? 1 : 2)));
                if (env[b] > top) { top = env[b]; topAt = b; }
            }
            double limit = top * Math.Pow(10.0, -db / 20.0);
            for (int b = topAt; b < blocks; b++) if (env[b] < limit) return (b - topAt) * 10.0;
            return (blocks - topAt) * 10.0;
        }
    }
}
