using System;
using System.Collections.Generic;
using UnityEngine;

namespace NeuroVida.Games.Radar
{
    /// <summary>
    /// Sonido PROPIO de «Rescate relámpago», sintetizado por código (docs/diseno-rescate.md §11): un «ping» agudo por vuelta del haz, un golpe de ruido filtrado en el relámpago, una nota pentatónica que SUBE con cada marca (y un «pop» al desmarcar),
    /// campanas al entrar cada cápsula en la nave, un acorde breve si la ronda es perfecta, un golpe sordo si no, la lluvia y un acorde al final. Nada suena fuerte: todo ≤ 0,5.
    /// </summary>
    public static class RadarSounds
    {
        private const int Rate = 44100;
        private static readonly Dictionary<string, AudioClip> Cache = new Dictionary<string, AudioClip>();

        /// <summary>La escala pentatónica de do mayor desde do5: la de los otros juegos.</summary>
        public static readonly float[] Penta = { 523.25f, 587.33f, 659.25f, 783.99f, 880f, 1046.5f, 1174.66f, 1318.51f, 1567.98f, 1760f };

        /// <summary>Un «ping» agudo por cada vuelta del haz.</summary>
        public static AudioClip Ping() => Get("ping", () => Make("ping", 0.4f, t => 0.22f * Bell(1567.98f, t, 0.1f)));

        /// <summary>El relámpago: un golpe de ruido filtrado que baja de 3000 a 1200 Hz.</summary>
        public static AudioClip Flash() => Get("flash", () => Make("flash", 0.18f, t =>
        {
            float k = Mathf.Clamp01(t / 0.12f);
            return 0.3f * Mathf.Exp(-t / 0.05f) * Noise(t, Mathf.Lerp(3000f, 1200f, k));
        }));

        /// <summary>La estática que borra la imagen: un ruido corto y grave.</summary>
        public static AudioClip Static() => Get("static", () => Make("static", 0.3f, t => 0.12f * Mathf.Min(1f, t / 0.02f) * Mathf.Exp(-t / 0.2f) * Noise(t, 520f)));

        /// <summary>Se marca una cápsula en el tablero: una nota de la pentatónica que sube con cada marca (la 1.ª, la 2.ª…).</summary>
        public static AudioClip Mark(int count) => Get("mark" + Mathf.Clamp(count, 1, 5), () => Make("mark", 0.5f, t => 0.26f * Bell(Penta[2 + Mathf.Clamp(count, 1, 5)], t, 0.14f)));

        /// <summary>Se desmarca: un «pop» breve que baja.</summary>
        public static AudioClip Pop() => Get("pop", () => Make("pop", 0.12f, t => 0.25f * Mathf.Sin(2f * Mathf.PI * Mathf.Lerp(520f, 260f, Mathf.Clamp01(t / 0.08f)) * t) * Mathf.Exp(-t / 0.035f)));

        /// <summary>Entra una cápsula rescatada en la nave: una campana que sube con el orden de entrada.</summary>
        public static AudioClip Rescue(int index) => Get("rescue" + (Mathf.Abs(index) % 5), () => Make("rescue", 0.9f, t => 0.24f * Bell(Penta[4 + Mathf.Abs(index) % 5], t, 0.28f)));

        /// <summary>Ronda perfecta: un acorde breve (do, mi y sol agudos).</summary>
        public static AudioClip Chime() => Get("chime", () => Make("chime", 1.2f, t => 0.16f * (Bell(1046.5f, t, 0.35f) + Bell(1318.51f, t - 0.07f, 0.35f) + Bell(1567.98f, t - 0.14f, 0.4f)), echo: true));

        /// <summary>Ronda que no salió perfecta: un golpe sordo y corto (nunca un castigo).</summary>
        public static AudioClip Thud() => Get("thud", () => Make("thud", 0.3f, t => 0.3f * Mathf.Sin(2f * Mathf.PI * Mathf.Lerp(220f, 130f, Mathf.Clamp01(t / 0.2f)) * t) * Mathf.Exp(-t / 0.08f)));

        /// <summary>«¡Lluvia de cápsulas!»: cuatro notas que bajan, ligeras.</summary>
        public static AudioClip Rain() => Get("rain", () => Make("rain", 1.0f, t => 0.14f * (Bell(Penta[8], t, 0.18f) + Bell(Penta[7], t - 0.09f, 0.18f) + Bell(Penta[6], t - 0.18f, 0.2f) + Bell(Penta[5], t - 0.27f, 0.25f))));

        /// <summary>Fin de la partida: un acorde lento.</summary>
        public static AudioClip Finale() => Get("finale", () => Make("finale", 2.0f, t => 0.18f * (Bell(784f, t, 0.5f) + Bell(1046.5f, t - 0.12f, 0.5f) + Bell(1318.51f, t - 0.24f, 0.55f)), echo: true));

        // ------------------------------------------------------------------ síntesis

        private static float Bell(float hz, float t, float tau)
        {
            if (t < 0f) return 0f;
            float env = (t < 0.008f ? t / 0.008f : 1f) * Mathf.Exp(-Mathf.Max(0f, t - 0.008f) / tau);
            return env * (Sin(hz, t) + 0.16f * Sin(hz * 2.76f, t) + 0.05f * Sin(hz * 5.4f, t));
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

        /// <summary>Sintetiza todos los clips (cada uno una sola vez), de a poco.</summary>
        public static System.Collections.IEnumerator Prewarm()
        {
            Ping(); Flash(); Static(); Pop();
            yield return null;
            for (int i = 1; i <= 5; i++) Mark(i);
            yield return null;
            for (int i = 0; i < 5; i++) Rescue(i);
            yield return null;
            Chime(); Thud(); Rain(); Finale();
        }

        public static int CachedCount => Cache.Count;
    }
}
