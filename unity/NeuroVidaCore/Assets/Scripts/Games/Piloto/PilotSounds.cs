using System;
using System.Collections.Generic;
using UnityEngine;

namespace NeuroVida.Games.Piloto
{
    /// <summary>
    /// Sonido PROPIO de «Piloto Estelar: la ruta de las balizas», sintetizado por código (docs/diseno-piloto.md §8): el motor (un diente de sierra y un triángulo graves con un filtro; un segundo exacto que se repite sin cortes: la velocidad le sube el tono y el hiperimpulso le abre el
    /// filtro), una nota para cada baliza que se pasa DENTRO de la ruta (la escala pentatónica) y un zumbido para la que se pasa fuera, un «blip» igual para todas las señales (no delata cuál es la de la misión), una campana al atrapar, un golpe sordo al equivocarse, un soplido al cruzar el arco de un sector, DOS NOTAS QUE SUBEN (con timbre de triángulo, no de campana) al cambiar la misión
    /// y un destello al entrar en hiperimpulso. Nada suena fuerte: todo ≤ 0,5.
    /// </summary>
    public static class PilotSounds
    {
        private const int Rate = 44100;
        private static readonly Dictionary<string, AudioClip> Cache = new Dictionary<string, AudioClip>();

        /// <summary>La escala pentatónica de do mayor desde do5: la de los otros juegos.</summary>
        public static readonly float[] Penta = { 523.25f, 587.33f, 659.25f, 783.99f, 880f, 1046.5f, 1174.66f, 1318.51f, 1567.98f, 1760f };

        /// <summary>El motor: dos senos graves con armónicos (55 y 82,5 Hz: 55 y 82 ciclos exactos en un segundo) filtrados; se toca en bucle y su tono sube con la velocidad (<c>AudioSource.pitch</c>).</summary>
        public static AudioClip Engine() => Get("engine", () =>
        {
            int n = Rate;
            var data = new float[n];
            // se calcula dos vueltas y se guarda la segunda: el filtro llega al régimen y el bucle no tiene salto
            var two = new float[2 * n];
            float lp = 0f;
            const float a = 0.055f;                                    // pasa-bajos de un polo (≈ 380 Hz)
            for (int i = 0; i < two.Length; i++)
            {
                float t = (i % n) / (float)Rate;
                float saw = 2f * (t * 55f - Mathf.Floor(t * 55f + 0.5f));
                float tri = 2f / Mathf.PI * Mathf.Asin(Mathf.Sin(2f * Mathf.PI * 82f * t));
                float x = 0.6f * saw + 0.4f * tri;
                lp += a * (x - lp);
                two[i] = lp;
            }
            float peak = 0f;
            for (int i = 0; i < n; i++) peak = Mathf.Max(peak, Mathf.Abs(two[n + i]));
            float gain = peak > 0.001f ? 0.45f / peak : 1f;               // la mitad del volumen máximo: nada pasa de 0,5
            for (int i = 0; i < n; i++) data[i] = two[n + i] * gain;
            var clip = AudioClip.Create("engine", n, 1, Rate, false);
            clip.SetData(data, 0);
            return clip;
        });

        /// <summary>Una baliza que se pasa dentro de la ruta: una nota corta y suave de la pentatónica (la baliza número <paramref name="index"/>).</summary>
        public static AudioClip Beacon(int index) => Get("beacon" + (Mathf.Abs(index) % 4), () => Make("beacon", 0.3f, t => 0.22f * Bell(Penta[1 + Mathf.Abs(index) % 4], t, 0.1f)));

        /// <summary>Una baliza que se pasa fuera de la ruta: un zumbido grave y corto (no solo color).</summary>
        public static AudioClip Buzz() => Get("buzz", () => Make("buzz", 0.22f, t =>
        {
            float env = Mathf.Exp(-t / 0.09f);
            return 0.3f * env * (Mathf.Sin(2f * Mathf.PI * 130f * t) + 0.5f * Mathf.Sin(2f * Mathf.PI * 138f * t) + 0.25f * Noise(t, 260f));
        }));

        /// <summary>Aparece una señal: el MISMO blip para todas (no delata cuál es de la misión).</summary>
        public static AudioClip Blip() => Get("blip", () => Make("blip", 0.12f, t => 0.2f * Sin(880f, t) * Mathf.Exp(-t / 0.035f)));

        /// <summary>Una señal de la misión atrapada: una campana que sube por la escala con la racha.</summary>
        public static AudioClip Catch(int streak) => Get("catch" + (Mathf.Abs(streak) % 5), () => Make("catch", 0.8f, t => 0.3f * Bell(Penta[3 + Mathf.Abs(streak) % 5], t, 0.25f)));

        /// <summary>Un toque equivocado (no era de la misión): un golpe sordo y corto que baja (no es un castigo).</summary>
        public static AudioClip Thud() => Get("thud", () => Make("thud", 0.3f, t =>
        {
            float hz = 220f * Mathf.Pow(130f / 220f, Mathf.Clamp01(t / 0.2f));
            return 0.3f * Sin(hz, t) * Mathf.Exp(-t / 0.08f);
        }));

        /// <summary>Se cruza el arco de un sector: un soplido que sube (las notas de la misión nueva van aparte: <see cref="MissionChange"/>).</summary>
        public static AudioClip Whoosh() => Get("whoosh", () => Make("whoosh", 0.8f, t =>
        {
            float k = Mathf.Clamp01(t / 0.7f);
            return t < 0.7f ? 0.2f * Noise(t, 300f * Mathf.Pow(2400f / 300f, k)) * Mathf.Sin(k * Mathf.PI) : 0f;
        }));

        /// <summary>
        /// La misión cambió (Tarea 63): dos notas que suben (mi y si agudos), una por cada latido de la tarjeta (0,4 s entre una y otra), con timbre de triángulo y caída rápida: se distingue de la campana de atrapar, que es larga y con armónicos.
        /// </summary>
        public static AudioClip MissionChange() => Get("mission", () => Make("mission", 1.0f, t => 0.3f * (Pluck(659.25f, t, 0.16f) + Pluck(987.77f, t - PilotContract.PulseBeatSeconds, 0.24f)), echo: true));

        /// <summary>El hiperimpulso: un destello que se abre y cuatro campanas que suben.</summary>
        public static AudioClip Hyper() => Get("hyper", () => Make("hyper", 1.6f, t =>
        {
            float k = Mathf.Clamp01(t / 0.9f);
            float n = t < 0.9f ? 0.12f * Noise(t, 200f * Mathf.Pow(3800f / 200f, k)) * Mathf.Sin(k * Mathf.PI) : 0f;
            return n + 0.14f * (Bell(Penta[4], t, 0.3f) + Bell(Penta[5], t - 0.08f, 0.3f) + Bell(Penta[6], t - 0.16f, 0.32f) + Bell(Penta[7], t - 0.24f, 0.4f));
        }, echo: true));

        /// <summary>Fin del vuelo (llegada al puerto): dos campanas lentas (sol y do agudos).</summary>
        public static AudioClip Finale() => Get("finale", () => Make("finale", 2.0f, t => 0.2f * (Bell(784f, t, 0.5f) + Bell(1046.5f, t - 0.15f, 0.55f)), echo: true));

        // ------------------------------------------------------------------ síntesis

        private static float Bell(float hz, float t, float tau)
        {
            if (t < 0f) return 0f;
            float env = (t < 0.01f ? t / 0.01f : 1f) * Mathf.Exp(-Mathf.Max(0f, t - 0.01f) / tau);
            return env * (Sin(hz, t) + 0.16f * Sin(hz * 2.76f, t) + 0.05f * Sin(hz * 5.4f, t));
        }

        private static float Pluck(float hz, float t, float tau)
        {
            if (t < 0f) return 0f;
            float env = (t < 0.008f ? t / 0.008f : 1f) * Mathf.Exp(-Mathf.Max(0f, t - 0.008f) / tau);
            float tri = 2f / Mathf.PI * Mathf.Asin(Mathf.Sin(2f * Mathf.PI * hz * t));
            return env * (0.85f * tri + 0.15f * Sin(hz * 2f, t));
        }

        private static float Noise(float t, float center)
        {
            float s = 0f;
            float[] ratios = { 0.71f, 0.93f, 1.07f, 1.29f, 1.51f, 0.83f, 1.17f };
            for (int i = 0; i < ratios.Length; i++) s += Mathf.Sin(2f * Mathf.PI * center * ratios[i] * t + i * 1.7f);
            return s / ratios.Length;
        }

        private static float Sin(float hz, float t) => Mathf.Sin(2f * Mathf.PI * hz * t);

        private static AudioClip Get(string key, Func<AudioClip> build)
        {
            if (!Cache.TryGetValue(key, out var clip) || clip == null)
            {
                clip = build();
                Cache[key] = clip;
            }
            return clip;
        }

        private static AudioClip Make(string name, float seconds, Func<float, float> sample, bool echo = false)
        {
            int n = Mathf.CeilToInt(seconds * Rate);
            var data = new float[n];
            for (int i = 0; i < n; i++) data[i] = sample(i / (float)Rate);
            if (echo)
            {
                int d = (int)(0.16f * Rate);
                for (int i = n - 1; i >= d; i--) data[i] += 0.28f * data[i - d];
            }
            float peak = 0f;
            for (int i = 0; i < n; i++) peak = Mathf.Max(peak, Mathf.Abs(data[i]));
            float gain = peak > 0.5f ? 0.5f / peak : 1f;            // nada pasa de 0,5
            for (int i = 0; i < n; i++) data[i] = Mathf.Clamp(data[i] * gain, -1f, 1f);
            int f = Mathf.Min(n, Rate / 100);
            for (int i = 0; i < f; i++) data[n - 1 - i] *= i / (float)f;
            var clip = AudioClip.Create(name, n, 1, Rate, false);
            clip.SetData(data, 0);
            return clip;
        }

        /// <summary>Para las pruebas y el calentamiento: sintetiza todos los clips (cada uno una sola vez), de a poco.</summary>
        public static System.Collections.IEnumerator Prewarm()
        {
            Engine();
            yield return null;
            for (int i = 0; i < 4; i++) Beacon(i);
            Buzz(); Blip();
            yield return null;
            for (int i = 0; i < 5; i++) Catch(i);
            Thud();
            yield return null;
            Whoosh(); MissionChange(); Hyper(); Finale();
        }

        public static int CachedCount => Cache.Count;
    }
}
