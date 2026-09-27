using System;
using System.Collections.Generic;
using UnityEngine;

namespace NeuroVida.Games.Rumbo
{
    /// <summary>
    /// Sonido de "Rumbo a Casa", con la identidad de NeuroVida (campanas de cristal, marimba, soplos suaves; nada de
    /// maquinita) y en la pentatónica de do de la app, así se mezcla afinado con <see cref="Shared.GameFeel"/>. La idea
    /// musical: la ida sube nota a nota (cada cristal es una nota más alta) y la llegada "vuelve a do", la nota de casa.
    /// <list type="bullet">
    /// <item><see cref="Ping"/>: aparece la señal del próximo cristal (un "ping" de sonar con eco).</item>
    /// <item><see cref="Crystal"/>: cristal recogido; cada parada, una nota más alta.</item>
    /// <item><see cref="Turn"/>: la nave gira (un soplo que se abre y se cierra).</item>
    /// <item><see cref="Lock"/>: rumbo fijado (dos toques de madera).</item>
    /// <item><see cref="Arrival"/>: la llegada. Justo en casa, el acorde de do completo con destellos; cerca, do y sol;
    /// lejos, un acorde "en suspenso" (re y sol) que no termina de resolver.</item>
    /// <item><see cref="MapReveal"/>: la vista desde arriba (el aire sube y brilla).</item>
    /// <item><see cref="FuelOut"/>: se acabó el combustible de la vuelta (dos notas que bajan).</item>
    /// </list>
    /// El vuelo usa el colchón de <see cref="Trafico.TrafficSounds.EngineLoop"/> (mismo sonido de nave en toda la app).
    /// </summary>
    public static class HomingSounds
    {
        private const int Rate = 44100;
        private static readonly float[] Penta = { 523.25f, 587.33f, 659.25f, 783.99f, 880.00f, 1046.50f, 1174.66f, 1318.51f, 1567.98f, 1760.00f };
        private static readonly Dictionary<string, AudioClip> Cache = new Dictionary<string, AudioClip>();

        public static AudioClip Ping() => Get("ping", () => Make("ping", 1.0f, t =>
            0.3f * Bell(1318.51f, t, 0.12f) + 0.12f * Bell(2637f, t, 0.03f), echo: 3));

        public static AudioClip Crystal(int stop) => Get("crystal" + stop, () => Make("crystal", 0.9f, t =>
        {
            float hz = Penta[Mathf.Clamp(stop + 2, 0, Penta.Length - 1)];
            return 0.34f * Bell(hz, t, 0.35f) + 0.3f * Marimba(hz * 0.5f, t) + 0.45f * Glitter(t, 17 + stop);
        }, echo: 1));

        public static AudioClip Turn() => Get("turn", () =>
        {
            const float seconds = 0.7f;
            float[] air = Breath(Mathf.CeilToInt(seconds * Rate), 5, 400f, 1600f, 0.35f);
            return Make("turn", seconds, t =>
            {
                int i = Mathf.Min(air.Length - 1, (int)(t * Rate));
                float k = Mathf.Clamp01(t / seconds);
                return 0.5f * air[i] * Mathf.Sin(Mathf.PI * k);
            });
        });

        public static AudioClip Lock() => Get("lock", () => Make("lock", 0.35f, t =>
            Wood(659.25f, t) + Wood(783.99f, t - 0.09f)));

        /// <summary>0 = justo en casa, 1 = cerca, 2 = lejos.</summary>
        public static AudioClip Arrival(int quality) => Get("arrival" + quality, () => Make("arrival", 1.8f, t =>
        {
            float[] notes = quality == 0 ? new[] { 523.25f, 659.25f, 783.99f, 1046.50f }
                          : quality == 1 ? new[] { 523.25f, 783.99f, 1046.50f }
                          : new[] { 587.33f, 783.99f, 1174.66f };
            float s = 0f;
            for (int i = 0; i < notes.Length; i++)
                s += (quality == 2 ? 0.16f : 0.2f) * Bell(notes[i], t - 0.08f * i, quality == 2 ? 0.45f : 0.7f);
            s += 0.25f * Marimba(notes[0] * 0.5f, t);
            if (quality == 0) s += 0.5f * Glitter(t - 0.3f, 41) + 0.35f * Glitter(t - 0.55f, 43);
            return s;
        }, echo: quality == 2 ? 1 : 2));

        public static AudioClip MapReveal() => Get("reveal", () =>
        {
            const float seconds = 1.4f;
            float[] air = Breath(Mathf.CeilToInt(seconds * Rate), 9, 500f, 3200f, 0.9f);
            return Make("reveal", seconds, t =>
            {
                int i = Mathf.Min(air.Length - 1, (int)(t * Rate));
                float swell = Mathf.Clamp01(t / 0.9f);
                float air2 = 0.35f * air[i] * swell * swell * Mathf.Exp(-Mathf.Max(0f, t - 0.9f) / 0.2f);
                return air2 + 0.18f * Bell(1567.98f, t - 0.75f, 0.4f) + 0.4f * Glitter(t - 0.8f, 29);
            }, echo: 1);
        });

        public static AudioClip FuelOut() => Get("fuel", () => Make("fuel", 0.6f, t =>
            0.4f * Marimba(392f, t) + 0.4f * Marimba(329.63f, t - 0.16f)));

        // ------------------------------------------------------------------ síntesis

        private static float Bell(float hz, float t, float tau) => t < 0f ? 0f :
            Sin(hz, t) * Env(t, 0.003f, tau) + 0.28f * Sin(hz * 2.76f, t) * Env(t, 0.002f, tau * 0.3f)
            + 0.1f * Sin(hz * 5.4f, t) * Env(t, 0.001f, tau * 0.1f);

        private static float Marimba(float hz, float t) => t < 0f ? 0f :
            0.8f * Sin(hz, t) * Env(t, 0.004f, 0.11f) + 0.22f * Sin(hz * 4f, t) * Env(t, 0.002f, 0.025f);

        private static float Wood(float hz, float t) => t < 0f ? 0f :
            0.5f * Sin(hz, t) * Env(t, 0.002f, 0.03f) + 0.28f * Sin(hz * 2.3f, t) * Env(t, 0.001f, 0.01f);

        /// <summary>Destello: parciales muy agudos que entran escalonados y se apagan en milésimas.</summary>
        private static float Glitter(float t, int seed)
        {
            if (t < 0f) return 0f;
            float s = 0f;
            uint h = (uint)(seed * 2654435761u);
            for (int i = 0; i < 6; i++)
            {
                h ^= h << 13; h ^= h >> 17; h ^= h << 5;
                float hz = 3500f + (h % 4500u);
                float start = 0.025f * i + (h % 17u) * 0.001f;
                if (t >= start) s += 0.12f * Sin(hz, t) * Env(t - start, 0.001f, 0.035f);
            }
            return s;
        }

        /// <summary>Soplo: ruido con semilla por un filtro que se abre de <paramref name="from"/> a <paramref name="to"/> Hz.</summary>
        private static float[] Breath(int n, int seed, float from, float to, float openSeconds)
        {
            var rng = new System.Random(seed);
            var data = new float[n];
            float y = 0f;
            for (int i = 0; i < n; i++)
            {
                float t = i / (float)Rate;
                float fc = Mathf.Lerp(from, to, Mathf.Clamp01(t / openSeconds));
                float a = 1f - Mathf.Exp(-2f * Mathf.PI * fc / Rate);
                y += a * ((float)rng.NextDouble() * 2f - 1f - y);
                data[i] = y;
            }
            return data;
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

        /// <summary>Arma el clip. <paramref name="echo"/>: rebotes suaves a 160 ms (da aire y espacio).</summary>
        private static AudioClip Make(string name, float seconds, Func<float, float> sample, int echo = 0)
        {
            int n = Mathf.CeilToInt(seconds * Rate);
            var data = new float[n];
            for (int i = 0; i < n; i++) data[i] = sample(i / (float)Rate);
            if (echo > 0)
            {
                int d = (int)(0.16f * Rate);
                var dry = (float[])data.Clone();
                float gain = 0.34f;
                for (int e = 1; e <= echo; e++, gain *= 0.45f)
                    for (int i = n - 1; i >= e * d; i--) data[i] += gain * dry[i - e * d];
            }
            for (int i = 0; i < n; i++) data[i] = Mathf.Clamp(data[i], -1f, 1f);
            int f = Mathf.Min(n, Rate / 100); // 10 ms de salida: sin "clic" al cortar
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
