using System;
using System.Collections.Generic;
using UnityEngine;

namespace NeuroVida.Games.Disparate
{
    /// <summary>
    /// Sonido de "¿Verdad o disparate?", con la identidad de la app (marimba y campanas en la pentatónica de do; nada arcade):
    /// una sala de radio suave. El acierto suena con la campana común (<c>GameFeel.Correct</c>, que sube con la racha); aquí
    /// están los sonidos propios: <see cref="Tune"/> (la sintonía al llegar la frase: un barrido de ruido filtrado),
    /// <see cref="StaticClick"/> (el chasquido de estática suave al errar), <see cref="Lost"/> (se perdió la señal),
    /// <see cref="WaveRing"/> (la onda que sale de la antena al acertar), <see cref="Aurora"/> (transmisión perfecta) y
    /// <see cref="Unclear"/> (gracias por avisar).
    /// </summary>
    public static class DisparateSounds
    {
        private const int Rate = 44100;
        private static readonly Dictionary<string, AudioClip> Cache = new Dictionary<string, AudioClip>();

        /// <summary>Barrido de ruido filtrado que sube y se aclara (la frase "sintoniza").</summary>
        public static AudioClip Tune() => Get("tune", () => Make("tune", 0.3f, t =>
        {
            float k = Mathf.Clamp01(t / 0.26f);
            float center = 600f + 3200f * k * k;
            float env = Mathf.Sin(k * Mathf.PI) * (t < 0.26f ? 1f : Mathf.Exp(-(t - 0.26f) / 0.02f));
            return 0.2f * Noise(t, center) * env + 0.1f * Sin(783.99f, t) * Env(t - 0.17f, 0.01f, 0.07f);
        }));

        /// <summary>Chasquido de estática corto, con un golpe grave y suave debajo (no es estridente).</summary>
        public static AudioClip StaticClick() => Get("static", () => Make("static", 0.22f, t =>
            0.26f * Noise(t, 2600f) * Env(t, 0.001f, 0.035f) + 0.3f * Sin(110f + 50f * Mathf.Exp(-t / 0.05f), t) * Env(t, 0.002f, 0.07f)));

        /// <summary>Se perdió la señal: ruido que se apaga y baja, y una nota grave suave.</summary>
        public static AudioClip Lost() => Get("lost", () => Make("lost", 0.7f, t =>
            0.22f * Noise(t, 3000f * Mathf.Exp(-t / 0.3f) + 500f) * Env(t, 0.003f, 0.22f) + 0.2f * Bell(392f, t - 0.08f, 0.3f)));

        /// <summary>La onda que sale de la antena al acertar: un soplo que sube y una campanita.</summary>
        public static AudioClip WaveRing() => Get("wave", () => Make("wave", 0.45f, t =>
        {
            float k = Mathf.Clamp01(t / 0.3f);
            return 0.1f * Noise(t, 1200f + 2500f * k) * Mathf.Sin(k * Mathf.PI) + 0.12f * Bell(1567.98f, t - 0.05f, 0.18f);
        }, echo: true));

        /// <summary>Transmisión perfecta (10 seguidas): una cascada suave de campanas altas con eco.</summary>
        public static AudioClip Aurora() => Get("aurora", () => Make("aurora", 1.8f, t =>
        {
            float[] notes = { 1046.5f, 1318.51f, 1567.98f, 1318.51f, 1760f, 2093f };
            float s = 0f;
            for (int i = 0; i < notes.Length; i++) s += 0.13f * Bell(notes[i], t - 0.14f * i, 0.5f);
            return s + 0.07f * Noise(t, 2200f) * Mathf.Sin(Mathf.Clamp01(t / 1.2f) * Mathf.PI) * 0.4f;
        }, echo: true));

        /// <summary>Se avisó que una frase no está clara: dos notas suaves que suben.</summary>
        public static AudioClip Unclear() => Get("unclear", () => Make("unclear", 0.5f, t =>
            0.26f * (Marimba(659.25f, t) + Marimba(880f, t - 0.1f))));

        // ------------------------------------------------------------------ síntesis

        private static float Marimba(float hz, float t) => t < 0f ? 0f :
            0.8f * Sin(hz, t) * Env(t, 0.004f, 0.12f) + 0.22f * Sin(hz * 4f, t) * Env(t, 0.002f, 0.028f);

        private static float Bell(float hz, float t, float tau) => t < 0f ? 0f :
            Sin(hz, t) * Env(t, 0.003f, tau) + 0.28f * Sin(hz * 2.76f, t) * Env(t, 0.002f, tau * 0.3f);

        /// <summary>Ruido filtrado en una banda alrededor de [center] Hz: suma de senos con razones fijas (determinista, sin azar).</summary>
        private static float Noise(float t, float center)
        {
            float s = 0f;
            float[] ratios = { 0.71f, 0.93f, 1.07f, 1.29f, 1.51f, 0.83f, 1.17f };
            for (int i = 0; i < ratios.Length; i++) s += Mathf.Sin(2f * Mathf.PI * center * ratios[i] * t + i * 1.7f);
            return s / ratios.Length;
        }

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
                int d = (int)(0.14f * Rate);
                for (int i = n - 1; i >= d; i--) data[i] += 0.3f * data[i - d];
            }
            for (int i = 0; i < n; i++) data[i] = Mathf.Clamp(data[i], -1f, 1f);
            int f = Mathf.Min(n, Rate / 100);
            for (int i = 0; i < f; i++) data[n - 1 - i] *= i / (float)f;
            var clip = AudioClip.Create(name, n, 1, Rate, false);
            clip.SetData(data, 0);
            return clip;
        }

        private static float Sin(float hz, float t) => Mathf.Sin(2f * Mathf.PI * hz * t);

        private static float Env(float t, float attack, float tau) =>
            t < 0f ? 0f : (t < attack ? t / attack : 1f) * Mathf.Exp(-Mathf.Max(0f, t - attack) / tau);
    }
}
