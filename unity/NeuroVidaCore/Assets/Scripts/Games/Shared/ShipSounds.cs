using System;
using System.Collections.Generic;
using UnityEngine;

namespace NeuroVida.Games.Shared
{
    /// <summary>
    /// El sonido de nave de toda la app: un colchón suave (do + sol, quinta abierta) con aire (soplo filtrado) que respira despacio y se repite sin cortes (2 s: todas las
    /// frecuencias son múltiplos de 0,5 Hz y el soplo se funde con su propio comienzo). Lo usa el vuelo de Rumbo a Casa. Vivía en <c>TrafficSounds</c> (Tráfico Estelar,
    /// retirado el 4-oct-2026).
    /// </summary>
    public static class ShipSounds
    {
        private const int Rate = 44100;
        private static readonly Dictionary<string, AudioClip> Cache = new Dictionary<string, AudioClip>();

        public static AudioClip EngineLoop() => Get("engine", () =>
        {
            const float seconds = 2f;
            int n = Mathf.RoundToInt(seconds * Rate);
            float[] air = Breath(n, 700f, 11);
            return Make("engine", seconds, t =>
            {
                int i = Mathf.Min(n - 1, (int)(t * Rate));
                // Respira una vez por vuelta (0,5 Hz); do4 con su gemelo apenas desafinado (un "uaa" lento) y sol4.
                float breathe = 0.8f + 0.2f * Sin(0.5f, t);
                float pad = 0.3f * Sin(261.5f, t) + 0.18f * Sin(262f, t) + 0.2f * Sin(392f, t) + 0.05f * Sin(523f, t);
                float shimmer = 0.035f * Sin(1046.5f, t) * (0.5f + 0.5f * Sin(0.5f, t + 0.5f));
                return breathe * (pad + shimmer) + 0.18f * air[i] * (0.7f + 0.3f * Sin(1f, t));
            }, fade: false);
        });

        private static float[] Breath(int n, float cutoff, int seed)
        {
            int blend = Rate / 5;
            var raw = new float[n + blend];
            var rng = new System.Random(seed);
            float a = 1f - Mathf.Exp(-2f * Mathf.PI * cutoff / Rate), y = 0f, y2 = 0f, peak = 1e-6f;
            for (int i = 0; i < raw.Length; i++)
            {
                y += a * ((float)rng.NextDouble() * 2f - 1f - y);
                y2 += a * (y - y2);
                raw[i] = y2;
                peak = Mathf.Max(peak, Mathf.Abs(y2));
            }
            var outp = new float[n];
            for (int i = 0; i < n; i++) outp[i] = raw[i] / peak;
            for (int i = 0; i < blend; i++)
            {
                float k = i / (float)blend;
                outp[i] = (raw[i] * k + raw[n + i] * (1f - k)) / peak;
            }
            return outp;
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

        private static AudioClip Make(string name, float seconds, Func<float, float> sample, bool fade = true)
        {
            int n = fade ? Mathf.CeilToInt(seconds * Rate) : Mathf.RoundToInt(seconds * Rate); // bucle: largo exacto
            var data = new float[n];
            for (int i = 0; i < n; i++) data[i] = Mathf.Clamp(sample(i / (float)Rate), -1f, 1f);
            if (fade)
            {
                int f = Mathf.Min(n, Rate / 250); // 4 ms: sin "clic" al cortar
                for (int i = 0; i < f; i++) data[n - 1 - i] *= i / (float)f;
            }
            var clip = AudioClip.Create(name, n, 1, Rate, false);
            clip.SetData(data, 0);
            return clip;
        }

        private static float Sin(float hz, float t) => Mathf.Sin(2f * Mathf.PI * hz * t);
    }
}
