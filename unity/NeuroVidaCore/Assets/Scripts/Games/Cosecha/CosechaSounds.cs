using System;
using System.Collections.Generic;
using UnityEngine;

namespace NeuroVida.Games.Cosecha
{
    /// <summary>
    /// Sonido de "Cosecha de palabras", con la identidad de la app (marimba y campanas en la pentatónica de do; nada arcade): cada
    /// letra tocada sube una nota (la palabra suena como melodía, <see cref="Letter"/>); sembrar = un "plop" de tierra con una
    /// campanita (<see cref="Plop"/>); brotar = una cuerda suave (<see cref="Sprout"/>); repetida o no válida = madera sorda
    /// suave (<see cref="Wood"/>); la palabra estrella, una cascada de campanas (<see cref="Star"/>); y <see cref="Ready"/> al
    /// cerrar la cosecha, <see cref="Hint"/> para la pista y <see cref="Shine"/> con la racha de palabras rápidas.
    /// </summary>
    public static class CosechaSounds
    {
        private const int Rate = 44100;
        // pentatónica mayor desde do5 (la misma de GameFeel): suena bien en cualquier orden
        private static readonly float[] Pentatonic = { 523.25f, 587.33f, 659.25f, 783.99f, 880.00f, 1046.50f, 1174.66f };
        private static readonly Dictionary<string, AudioClip> Cache = new Dictionary<string, AudioClip>();

        /// <summary>La nota de la letra número [i] de la palabra (0 = primera): sube por la pentatónica.</summary>
        public static AudioClip Letter(int i)
        {
            int step = Mathf.Clamp(i, 0, Pentatonic.Length - 1);
            return Get("letter" + step, () => Make("letter", 0.3f, t => 0.8f * Marimba(Pentatonic[step], t)));
        }

        /// <summary>La semilla cae: un "plop" grave de tierra y una campanita.</summary>
        public static AudioClip Plop() => Get("plop", () => Make("plop", 0.5f, t =>
            0.5f * Sin(150f * Mathf.Exp(-t / 0.09f) + 70f, t) * Env(t, 0.002f, 0.07f) + 0.1f * Noise(t, 700f) * Env(t, 0.001f, 0.025f)
            + 0.14f * Bell(1318.51f, t - 0.07f, 0.2f)));

        /// <summary>Brota: una cuerda suave que sube (ataque lento, sin golpe).</summary>
        public static AudioClip Sprout() => Get("sprout", () => Make("sprout", 0.7f, t =>
        {
            float k = Mathf.Clamp01(t / 0.35f);
            float hz = 392f * Mathf.Pow(2f, k * 0.5f);
            float env = Mathf.Min(1f, t / 0.05f) * Mathf.Exp(-Mathf.Max(0f, t - 0.18f) / 0.18f);
            return 0.22f * env * (Sin(hz, t) + 0.35f * Sin(hz * 2f, t) + 0.12f * Sin(hz * 3f, t));
        }, echo: true));

        /// <summary>Repetida o no válida: dos golpecitos sordos de madera (suave, sin castigo).</summary>
        public static AudioClip Wood() => Get("wood", () => Make("wood", 0.3f, t =>
            0.45f * (Sin(196f, t) + 0.3f * Sin(392f, t)) * Env(t, 0.002f, 0.045f) + 0.4f * (Sin(165f, t - 0.09f) + 0.3f * Sin(330f, t - 0.09f)) * Env(t - 0.09f, 0.002f, 0.05f)));

        /// <summary>Palabra estrella: una cascada suave de campanas altas con eco.</summary>
        public static AudioClip Star() => Get("star", () => Make("star", 1.9f, t =>
        {
            float[] notes = { 1046.5f, 1318.51f, 1567.98f, 2093f, 1567.98f, 2637f };
            float s = 0f;
            for (int i = 0; i < notes.Length; i++) s += 0.14f * Bell(notes[i], t - 0.13f * i, 0.55f);
            return s + 0.3f * Marimba(523.25f, t);
        }, echo: true));

        /// <summary>Cosecha lista: un acorde abierto (do-sol-mi) con brillo.</summary>
        public static AudioClip Ready() => Get("ready", () => Make("ready", 1.4f, t =>
        {
            float[] chord = { 523.25f, 783.99f, 1046.5f, 1318.51f };
            float s = 0f;
            for (int i = 0; i < chord.Length; i++) s += 0.2f * Bell(chord[i], t - 0.09f * i, 0.5f);
            return s;
        }, echo: true));

        /// <summary>La pista: dos notas suaves que suben (Nubi señala una letra).</summary>
        public static AudioClip Hint() => Get("hint", () => Make("hint", 0.6f, t =>
            0.24f * (Marimba(783.99f, t) + Marimba(1046.5f, t - 0.12f))));

        /// <summary>Brillo de la órbita con 3 palabras rápidas: un arpegio corto y alto.</summary>
        public static AudioClip Shine() => Get("shine", () => Make("shine", 0.7f, t =>
        {
            float[] n = { 1046.5f, 1318.51f, 1567.98f };
            float s = 0f;
            for (int i = 0; i < n.Length; i++) s += 0.14f * Bell(n[i], t - 0.07f * i, 0.25f);
            return s;
        }, echo: true));

        // ------------------------------------------------------------------ síntesis

        private static float Marimba(float hz, float t) => t < 0f ? 0f :
            Sin(hz, t) * Env(t, 0.004f, 0.12f) + 0.28f * Sin(hz * 4f, t) * Env(t, 0.002f, 0.028f);

        private static float Bell(float hz, float t, float tau) => t < 0f ? 0f :
            Sin(hz, t) * Env(t, 0.003f, tau) + 0.28f * Sin(hz * 2.76f, t) * Env(t, 0.002f, tau * 0.3f);

        /// <summary>Ruido filtrado alrededor de [center] Hz: suma de senos con razones fijas (determinista, sin azar).</summary>
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
                int d = (int)(0.13f * Rate);
                for (int i = n - 1; i >= d; i--) data[i] += 0.28f * data[i - d];
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
