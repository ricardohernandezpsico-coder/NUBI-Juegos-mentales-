using System;
using System.Collections.Generic;
using UnityEngine;

namespace NeuroVida.Games.Anagramas
{
    /// <summary>
    /// Sonido de «En la punta de la lengua» (la identidad de la app: campanas en la pentatónica de do, nada arcade): cada ficha que se coloca suena con una
    /// nota de la escala (la casilla k suena la nota k); al completar bien, cada letra se enciende con la nota siguiente y cierra un acorde; el error es un
    /// golpe grave y blando (sin chicharra); la ayuda, un soplo que sube y una campana; las fichas aparecen con un barrido suave. Sintetizado por código.
    /// </summary>
    public static class PuntaSounds
    {
        private const int Rate = 44100;
        /// <summary>La escala del juego (do mayor pentatónica, dos octavas): casilla k → nota k.</summary>
        public static readonly float[] Penta = { 523.25f, 587.33f, 659.25f, 783.99f, 880f, 1046.5f, 1174.66f, 1318.5f, 1567.98f, 1760f };
        private static readonly Dictionary<string, AudioClip> Cache = new Dictionary<string, AudioClip>();

        /// <summary>La nota <paramref name="k"/> de la escala (0 = do5; sigue hasta la nota 9).</summary>
        public static AudioClip Note(int k)
        {
            k = Mathf.Clamp(k, 0, Penta.Length - 1);
            return Get("note" + k, () => Make("note", 1.2f, t => 0.34f * Bell(Penta[k], t, 0.22f)));
        }

        /// <summary>Una ficha vuelve al banco: un soplo corto, bajo y suave.</summary>
        public static AudioClip Back() => Get("back", () => Make("back", 0.2f, t =>
            0.12f * Noise(t, 1400f * Mathf.Pow(0.5f, t / 0.18f)) * Mathf.Sin(Mathf.Clamp01(t / 0.18f) * Mathf.PI)));

        /// <summary>Las fichas aparecen (o la señal se aclara): un barrido que sube.</summary>
        public static AudioClip Appear() => Get("appear", () => Make("appear", 0.55f, t =>
            0.13f * Noise(t, 600f * Mathf.Pow(2200f / 600f, Mathf.Clamp01(t / 0.5f))) * Mathf.Sin(Mathf.Clamp01(t / 0.5f) * Mathf.PI)));

        /// <summary>Una ayuda: un soplo que sube y una campana grave.</summary>
        public static AudioClip Help() => Get("help", () => Make("help", 1.0f, t =>
            0.15f * Noise(t, 300f * Mathf.Pow(1800f / 300f, Mathf.Clamp01(t / 0.7f))) * Mathf.Sin(Mathf.Clamp01(t / 0.7f) * Mathf.PI) +
            0.26f * Bell(392f, t, 0.18f)));

        /// <summary>Casi: un golpe grave y blando que baja (sin chicharra ni nada que regañe).</summary>
        public static AudioClip Thud() => Get("thud", () => Make("thud", 0.34f, t =>
        {
            float hz = 220f * Mathf.Pow(130f / 220f, Mathf.Clamp01(t / 0.25f));
            return 0.3f * Mathf.Sin(2f * Mathf.PI * hz * t) * Mathf.Exp(-t / 0.09f);
        }));

        /// <summary>El acorde de cierre cuando la palabra salió (tres campanas que suben).</summary>
        public static AudioClip Chord() => Get("chord", () => Make("chord", 1.6f, t =>
            0.2f * (Bell(1046.5f, t, 0.35f) + Bell(1318.5f, t - 0.08f, 0.35f) + Bell(1567.98f, t - 0.16f, 0.4f)), echo: true));

        /// <summary>El lucero llega al cielo: dos campanitas.</summary>
        public static AudioClip Chime() => Get("chime", () => Make("chime", 1.1f, t =>
            0.18f * (Bell(659.25f, t, 0.2f) + Bell(880f, t - 0.09f, 0.24f))));

        /// <summary>Fin de la partida: tres campanas que suben, lentas.</summary>
        public static AudioClip Finale() => Get("finale", () => Make("finale", 2.0f, t =>
            0.18f * (Bell(784f, t, 0.3f) + Bell(1046.5f, t - 0.15f, 0.34f) + Bell(1318.5f, t - 0.3f, 0.4f)), echo: true));

        // ------------------------------------------------------------------ síntesis

        /// <summary>Campana: el tono y dos armónicos inarmónicos (×2,76 y ×5,4), con un ataque de 10 ms y caída exponencial.</summary>
        private static float Bell(float hz, float t, float tau)
        {
            if (t < 0f) return 0f;
            float env = (t < 0.01f ? t / 0.01f : 1f) * Mathf.Exp(-Mathf.Max(0f, t - 0.01f) / tau);
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
            for (int i = 0; i < n; i++) data[i] = Mathf.Clamp(data[i], -1f, 1f);
            int f = Mathf.Min(n, Rate / 100);
            for (int i = 0; i < f; i++) data[n - 1 - i] *= i / (float)f;
            var clip = AudioClip.Create(name, n, 1, Rate, false);
            clip.SetData(data, 0);
            return clip;
        }
    }
}
