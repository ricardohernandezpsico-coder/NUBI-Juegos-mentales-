using System;
using System.Collections.Generic;
using UnityEngine;

namespace NeuroVida.Games.Intrusa
{
    /// <summary>
    /// Sonido de "La estrella intrusa", con la identidad de la app (marimba y campana en la pentatónica de do; nada arcade): la chispa
    /// que traza la figura toca una nota de la escala ascendente en cada estrella (más fuerte en las estrellas con palabra); al cerrar
    /// suena un acorde de cuatro notas. Aquí también: <see cref="Fall"/> (la estrella fugaz: un soplo brillante que baja),
    /// <see cref="Glass"/> (error: un cristal suave y apagado), <see cref="Engrave"/> (el grabado), <see cref="Named"/> (campanas del
    /// «¿Qué las une?») y <see cref="Pulse"/> (latido de las cuatro estrellas mientras se piensa).
    /// </summary>
    public static class IntrusaSounds
    {
        private const int Rate = 44100;
        // Pentatónica mayor desde do5, igual que GameFeel: todo suena afinado con lo demás.
        private static readonly float[] Penta = { 523.25f, 587.33f, 659.25f, 783.99f, 880.00f, 1046.50f, 1174.66f, 1318.51f, 1567.98f };
        private static readonly Dictionary<string, AudioClip> Cache = new Dictionary<string, AudioClip>();

        public static int NoteCount => Penta.Length;

        /// <summary>La nota [step] (0 = do5, sube por la pentatónica); [strong] = estrella con palabra: más fuerte y con brillo.</summary>
        public static AudioClip Note(int step, bool strong)
        {
            step = Mathf.Clamp(step, 0, Penta.Length - 1);
            return Get("note" + step + (strong ? "s" : "w"), () => Make("note", strong ? 0.55f : 0.4f, t =>
            {
                float hz = Penta[step];
                float amp = strong ? 0.5f : 0.3f;
                return amp * (Marimba(hz, t) * 0.8f + Bell(hz * 2f, t, 0.25f) * (strong ? 0.25f : 0.08f));
            }));
        }

        /// <summary>Acorde de cierre de cuatro notas (do-mi-sol-do) con campana y un brillo que entra después.</summary>
        public static AudioClip Chord() => Get("chord", () => Make("chord", 1.3f, t =>
        {
            float[] n = { 523.25f, 659.25f, 783.99f, 1046.5f };
            float s = 0f;
            for (int i = 0; i < n.Length; i++) s += 0.2f * Bell(n[i], t - i * 0.03f, 0.55f);
            s += 0.07f * Bell(2093f, t - 0.14f, 0.3f);
            return s;
        }, echo: true));

        /// <summary>Estrella fugaz: un soplo filtrado que baja de lo agudo a lo grave, con una campanita al empezar.</summary>
        public static AudioClip Fall() => Get("fall", () => Make("fall", 0.95f, t =>
        {
            float k = Mathf.Clamp01(t / 0.9f);
            float center = 4200f * Mathf.Exp(-k * 1.8f) + 500f;
            float env = Mathf.Sin(Mathf.Clamp01(t / 0.9f) * Mathf.PI) * 0.9f;
            return 0.16f * Noise(t, center) * env + 0.14f * Bell(1567.98f, t, 0.18f);
        }));

        /// <summary>Error: un cristal suave y apagado (dos notas graves que bajan, con un poco de tintineo), sin chicharra.</summary>
        public static AudioClip Glass() => Get("glass", () => Make("glass", 0.6f, t =>
            0.26f * Bell(392f, t, 0.28f) + 0.2f * Bell(329.63f, t - 0.1f, 0.3f) + 0.05f * Bell(1174.66f, t, 0.1f)));

        /// <summary>El grabado: un barrido suave y cálido que sube mientras el dibujo se completa.</summary>
        public static AudioClip Engrave() => Get("engrave", () => Make("engrave", 1.2f, t =>
        {
            float k = Mathf.Clamp01(t / 1.1f);
            float env = Mathf.Sin(k * Mathf.PI);
            return 0.1f * Noise(t, 700f + 1800f * k) * env + 0.08f * Sin(261.63f * (1f + 0.5f * k), t) * env;
        }, echo: true));

        /// <summary>«¿Qué las une?» acertada: tres campanas que suben.</summary>
        public static AudioClip Named() => Get("named", () => Make("named", 1.0f, t =>
            0.16f * (Bell(783.99f, t, 0.4f) + Bell(1046.5f, t - 0.12f, 0.4f) + Bell(1318.51f, t - 0.24f, 0.5f)), echo: true));

        /// <summary>Latido de las cuatro estrellas mientras se elige qué las une: un golpecito grave y suave.</summary>
        public static AudioClip Pulse() => Get("pulse", () => Make("pulse", 0.35f, t =>
            0.2f * Sin(196f, t) * Env(t, 0.01f, 0.12f) + 0.06f * Bell(784f, t, 0.1f)));

        /// <summary>La opción de «¿Qué las une?» no era: un golpecito de madera sordo.</summary>
        public static AudioClip Dull() => Get("dull", () => Make("dull", 0.25f, t =>
            0.3f * Sin(180f, t) * Env(t, 0.003f, 0.06f)));

        // ------------------------------------------------------------------ síntesis

        private static float Marimba(float hz, float t) => t < 0f ? 0f :
            Sin(hz, t) * Env(t, 0.004f, 0.14f) + 0.28f * Sin(hz * 4f, t) * Env(t, 0.002f, 0.03f);

        private static float Bell(float hz, float t, float tau) => t < 0f ? 0f :
            Sin(hz, t) * Env(t, 0.003f, tau) + 0.28f * Sin(hz * 2.76f, t) * Env(t, 0.002f, tau * 0.3f);

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
                int d = (int)(0.16f * Rate);
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
