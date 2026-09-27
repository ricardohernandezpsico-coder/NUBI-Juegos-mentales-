using System;
using System.Collections.Generic;

namespace NeuroVida.Games.Contacto
{
    /// <summary>
    /// La voz de los nuri, sintetizada (sin grabaciones): pronuncia las palabras del idioma con vocales y consonantes de
    /// verdad y una entonación cantada, un poco aguda y con un eco de octava arriba que la hace "de otro mundo" sin
    /// volverla robótica.
    /// <list type="bullet">
    /// <item>Vocales y sonoras (m, n, l, r): síntesis aditiva por formantes. Cada armónico de la voz pesa según qué tan
    /// cerca queda de las tres resonancias de la vocal (a, e, i, o, u del español); entre sonidos las resonancias se
    /// deslizan, como en el habla.</item>
    /// <item>Consonantes: p, t, k, b, d, g con cierre y explosión (ruido corto de su color); s, z, f con soplo; la
    /// resonancia de la vocal que sigue arranca desde el "lugar" de la consonante.</item>
    /// <item>Entonación: cada palabra tiene su melodía propia en la pentatónica de la app (así, además, se reconoce de
    /// oído) y la frase baja un poco al final, como una frase dicha.</item>
    /// </list>
    /// C# puro (sin UnityEngine): <see cref="ContactSounds"/> lo convierte en AudioClip y ArtPreview lo vuelca a WAV.
    /// </summary>
    public static class NuriVoice
    {
        public const int Rate = 22050;
        private const int Block = 32;
        private const float BaseHz = 262f;   // do central: la voz queda en la tonalidad de la app

        /// <summary>Un tramo del plan de una frase: qué suena, cuándo y con qué resonancias.</summary>
        public struct Segment
        {
            public char Phone;
            public float Start, Duration;
            public int Word;           // índice de la palabra dentro de la frase
            public bool Vowel;
        }

        /// <summary>El plan de una frase: tramos y duración total (para animar la boca y marcar la palabra que suena).</summary>
        public sealed class Plan
        {
            public readonly List<Segment> Segments = new List<Segment>();
            public float Duration;
            public float[] WordStart, WordEnd;

            /// <summary>¿Boca abierta en ese instante? (durante las vocales).</summary>
            public bool MouthOpen(float t)
            {
                foreach (var s in Segments) if (s.Vowel && t >= s.Start && t < s.Start + s.Duration * 0.9f) return true;
                return false;
            }

            /// <summary>Palabra que suena en ese instante (-1 = ninguna).</summary>
            public int WordAt(float t)
            {
                for (int i = 0; i < WordStart.Length; i++) if (t >= WordStart[i] && t < WordEnd[i] + 0.06f) return i;
                return -1;
            }
        }

        // Vocales: F1, F2, F3 (Hz), un poco más agudas que las de una voz adulta (voz pequeña).
        private static readonly Dictionary<char, float[]> Vowels = new Dictionary<char, float[]>
        {
            { 'A', new[] { 850f, 1400f, 2750f } },
            { 'E', new[] { 520f, 2050f, 2850f } },
            { 'I', new[] { 330f, 2550f, 3300f } },
            { 'O', new[] { 540f, 980f, 2700f } },
            { 'U', new[] { 360f, 880f, 2600f } },
        };

        // Sonoras: resonancias y fuerza relativa.
        private static readonly Dictionary<char, float[]> Sonorants = new Dictionary<char, float[]>
        {
            { 'M', new[] { 280f, 1100f, 2400f, 0.45f } },
            { 'N', new[] { 290f, 1600f, 2500f, 0.45f } },
            { 'L', new[] { 380f, 1300f, 2800f, 0.6f } },
            { 'R', new[] { 420f, 1350f, 1900f, 0.35f } },
        };

        /// <summary>"Lugar" de cada consonante: desde dónde arranca F2 de la vocal siguiente.</summary>
        private static float Locus(char c)
        {
            switch (c)
            {
                case 'P': case 'B': case 'M': case 'F': case 'V': return 900f;
                case 'T': case 'D': case 'N': case 'S': case 'Z': case 'L': case 'R': return 1800f;
                case 'K': case 'G': return 2300f;
                default: return -1f;
            }
        }

        /// <summary>Melodías de palabra (semitonos sobre do). Cada palabra toma una según su letras.</summary>
        private static readonly int[][] Tunes =
        {
            new[] { 4, 0 }, new[] { 7, 4 }, new[] { 0, 4 }, new[] { 2, 7 }, new[] { 9, 4 }, new[] { 4, 9 }, new[] { 7, 2 },
            new[] { 0, 7 },
        };

        private static bool IsVowel(char c) => Vowels.ContainsKey(c);

        public static Plan MakePlan(IReadOnlyList<string> words)
        {
            var plan = new Plan { WordStart = new float[words.Count], WordEnd = new float[words.Count] };
            float t = 0.06f;
            for (int w = 0; w < words.Count; w++)
            {
                string word = words[w].ToUpperInvariant();
                plan.WordStart[w] = t;
                for (int i = 0; i < word.Length; i++)
                {
                    char c = word[i];
                    bool last = i == word.Length - 1 || (i == word.Length - 2 && !IsVowel(word[word.Length - 1]) && IsVowel(c));
                    float d;
                    if (IsVowel(c)) d = last ? 0.2f : 0.14f;
                    else if (c == 'P' || c == 'T' || c == 'K') d = 0.075f;
                    else if (c == 'B' || c == 'D' || c == 'G' || c == 'V') d = 0.06f;
                    else if (c == 'S' || c == 'Z' || c == 'F') d = 0.1f;
                    else if (c == 'R') d = 0.03f;
                    else d = 0.065f;
                    if (i == word.Length - 1 && !IsVowel(c)) d += 0.03f;   // consonante final (TUN, NAS, BEL)
                    plan.Segments.Add(new Segment { Phone = c, Start = t, Duration = d, Word = w, Vowel = IsVowel(c) });
                    t += d;
                }
                plan.WordEnd[w] = t;
                t += 0.13f;   // pausa entre palabras
            }
            plan.Duration = t + 0.35f;   // cola (eco)
            return plan;
        }

        public static float[] Speak(IReadOnlyList<string> words) => Render(MakePlan(words), words);

        public static float[] Render(Plan plan, IReadOnlyList<string> words)
        {
            int n = (int)Math.Ceiling(plan.Duration * Rate);
            int blocks = n / Block + 1;
            // Pistas por bloque: f0, formantes, sonoridad, ruido (fuerza, centro, ancho).
            var f0 = new float[blocks];
            var f1 = new float[blocks]; var f2 = new float[blocks]; var f3 = new float[blocks];
            var voice = new float[blocks];
            var noise = new float[blocks]; var nCenter = new float[blocks]; var nWidth = new float[blocks];

            // 1) Objetivos por tramo.
            var tunes = new int[words.Count][];
            for (int w = 0; w < words.Count; w++) tunes[w] = Tunes[Hash(words[w]) % Tunes.Length];
            var vowelsInWord = new int[words.Count];
            foreach (var s in plan.Segments) if (s.Vowel) vowelsInWord[s.Word]++;
            var seenInWord = new int[words.Count];
            float lastF1 = 500f, lastF2 = 1500f, lastF3 = 2700f, lastPitch = BaseHz;
            for (int si = 0; si < plan.Segments.Count; si++)
            {
                var s = plan.Segments[si];
                int b0 = (int)(s.Start * Rate / Block), b1 = (int)((s.Start + s.Duration) * Rate / Block);
                char c = s.Phone;
                float tf1 = lastF1, tf2 = lastF2, tf3 = lastF3, tv = 0f, tn = 0f, nc = 0f, nw = 0f;
                if (s.Vowel)
                {
                    var v = Vowels[c];
                    tf1 = v[0]; tf2 = v[1]; tf3 = v[2]; tv = 1f;
                    int k = seenInWord[s.Word]++;
                    var tune = tunes[s.Word];
                    int semis = tune[Math.Min(k, tune.Length - 1)];
                    if (vowelsInWord[s.Word] == 1) semis = tune[0];
                    // La frase baja un poco hacia el final (declinación), sin perder la melodía de cada palabra.
                    float decl = -1.5f * s.Word / Math.Max(1, words.Count - 1);
                    lastPitch = BaseHz * (float)Math.Pow(2.0, (semis + decl) / 12.0);
                }
                else if (Sonorants.TryGetValue(c, out var so))
                {
                    tf1 = so[0]; tf2 = so[1]; tf3 = so[2]; tv = so[3];
                }
                else
                {
                    switch (c)
                    {
                        case 'S': case 'Z': tn = 0.24f; nc = 6000f; nw = 2600f; break;
                        case 'F': tn = 0.16f; nc = 4200f; nw = 4500f; break;
                    }
                }
                for (int b = Math.Max(0, b0); b <= b1 && b < blocks; b++)
                {
                    f0[b] = lastPitch; f1[b] = tf1; f2[b] = tf2; f3[b] = tf3; voice[b] = tv; noise[b] = tn; nCenter[b] = nc; nWidth[b] = nw;
                }
                // Explosión de las oclusivas: al final del cierre, ruido cortito de su color.
                if (c == 'P' || c == 'T' || c == 'K' || c == 'B' || c == 'D' || c == 'G' || c == 'V')
                {
                    bool voiced = c == 'B' || c == 'D' || c == 'G' || c == 'V';
                    int burst = (int)(0.018f * Rate / Block) + 1;
                    for (int b = Math.Max(0, b1 - burst); b <= b1 && b < blocks; b++)
                    {
                        noise[b] = voiced ? 0.22f : 0.4f;
                        nCenter[b] = c == 'P' || c == 'B' || c == 'V' ? 900f : c == 'T' || c == 'D' ? 4500f : 2400f;
                        nWidth[b] = c == 'T' || c == 'D' ? 3000f : 1400f;
                    }
                    if (voiced) for (int b = Math.Max(0, b0); b < b1 - burst && b < blocks; b++) voice[b] = 0.12f;   // murmullo del cierre
                }
                // La vocal que sigue a una consonante arranca con F2 en el lugar de la consonante.
                float locus = Locus(c);
                if (locus > 0f && si + 1 < plan.Segments.Count && plan.Segments[si + 1].Vowel)
                    for (int b = Math.Max(0, b0); b <= b1 && b < blocks; b++) f2[b] = locus;
                lastF1 = tf1; lastF2 = tf2; lastF3 = tf3;
            }
            // Antes de la primera y después de la última palabra: silencio con la misma altura (sin saltos).
            for (int b = 1; b < blocks; b++) if (f0[b] <= 0f) f0[b] = f0[b - 1] > 0f ? f0[b - 1] : BaseHz;
            for (int b = blocks - 2; b >= 0; b--) if (f0[b] <= 0f) f0[b] = f0[b + 1];

            // 2) Suavizado: resonancias y altura se deslizan (habla), la sonoridad entra y sale sin clics.
            Smooth(f0, 0.035f); Smooth(f1, 0.018f); Smooth(f2, 0.03f); Smooth(f3, 0.02f); Smooth(voice, 0.008f); Smooth(noise, 0.003f);

            // 3) Síntesis.
            var outp = new float[n];
            double phase = 0.0;
            var rnd = new Random(Hash(string.Join(" ", words)));
            var bp = new Bandpass();
            float octavePhase = 0f;
            for (int b = 0; b < blocks; b++)
            {
                int start = b * Block;
                if (start >= n) break;
                float hz = f0[b] * (1f + 0.018f * (float)Math.Sin(2.0 * Math.PI * 5.2 * start / Rate));   // vibrato
                int harmonics = Math.Max(1, (int)(5200f / hz));
                var amp = new float[harmonics + 1];
                if (voice[b] > 0.001f)
                    for (int k = 1; k <= harmonics; k++)
                    {
                        float fk = k * hz;
                        float g = Res(fk, f1[b], 110f) + 0.7f * Res(fk, f2[b], 140f) + 0.35f * Res(fk, f3[b], 200f);
                        amp[k] = voice[b] * g / (float)Math.Pow(k, 0.55);
                    }
                bp.Set(nCenter[b] > 0f ? nCenter[b] : 3000f, nWidth[b] > 0f ? nWidth[b] : 2000f);
                for (int i = start; i < start + Block && i < n; i++)
                {
                    phase += 2.0 * Math.PI * hz / Rate;
                    if (phase > 2.0 * Math.PI * 1000.0) phase -= 2.0 * Math.PI * 1000.0;
                    float s = 0f;
                    if (voice[b] > 0.001f)
                        for (int k = 1; k <= harmonics; k++) s += amp[k] * (float)Math.Sin(k * phase);
                    // Eco de octava arriba, muy suave: el brillo "de otro mundo".
                    octavePhase += 2f * (float)Math.PI * hz * 2.003f / Rate;
                    s += 0.07f * voice[b] * (float)Math.Sin(octavePhase);
                    if (noise[b] > 0.001f) s += noise[b] * bp.Tick((float)(rnd.NextDouble() * 2.0 - 1.0)) * 2.2f;
                    outp[i] = s;
                }
            }

            // 4) Eco suave (espacio), normalizado.
            int d = (int)(0.16f * Rate);
            for (int i = n - 1; i >= d; i--) outp[i] += 0.22f * outp[i - d];
            float peak = 0f;
            foreach (float v in outp) peak = Math.Max(peak, Math.Abs(v));
            if (peak > 0f) for (int i = 0; i < n; i++) outp[i] *= 0.82f / peak;
            int fade = Math.Min(n, Rate / 100);
            for (int i = 0; i < fade; i++) outp[n - 1 - i] *= i / (float)fade;
            return outp;
        }

        /// <summary>Resonancia: cuánto pasa una frecuencia cerca de un formante (curva de campana suave).</summary>
        private static float Res(float f, float center, float bandwidth)
        {
            float x = (f - center) / (bandwidth * 0.5f);
            return 1f / (1f + x * x);
        }

        /// <summary>Suavizado de un polo, en los dos sentidos (sin retraso).</summary>
        private static void Smooth(float[] a, float seconds)
        {
            float k = (float)Math.Exp(-Block / (seconds * Rate));
            for (int i = 1; i < a.Length; i++) a[i] = a[i] + (a[i - 1] - a[i]) * k;
            for (int i = a.Length - 2; i >= 0; i--) a[i] = a[i] + (a[i + 1] - a[i]) * k;
        }

        public static int Hash(string s)
        {
            unchecked
            {
                int h = 17;
                foreach (char c in s) h = h * 31 + c;
                return h & 0x7fffffff;
            }
        }

        /// <summary>Filtro pasabanda de segundo orden (para el ruido de las consonantes).</summary>
        private sealed class Bandpass
        {
            private float _b0, _b2, _a1, _a2, _x1, _x2, _y1, _y2;

            public void Set(float center, float width)
            {
                double w0 = 2.0 * Math.PI * center / Rate;
                double q = Math.Max(0.4, center / width);
                double alpha = Math.Sin(w0) / (2.0 * q);
                double a0 = 1.0 + alpha;
                _b0 = (float)(alpha / a0);
                _b2 = (float)(-alpha / a0);
                _a1 = (float)(-2.0 * Math.Cos(w0) / a0);
                _a2 = (float)((1.0 - alpha) / a0);
            }

            public float Tick(float x)
            {
                float y = _b0 * x + _b2 * _x2 - _a1 * _y1 - _a2 * _y2;
                _x2 = _x1; _x1 = x; _y2 = _y1; _y1 = y;
                return y;
            }
        }
    }
}
