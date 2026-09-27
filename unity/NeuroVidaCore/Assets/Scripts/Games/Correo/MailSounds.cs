using System;
using System.Collections.Generic;
using UnityEngine;

namespace NeuroVida.Games.Correo
{
    /// <summary>
    /// Sonido de "Correo Estelar", con la identidad de la app (marimba y campanas en la pentatónica de do, como Tráfico y la
    /// Bitácora; nada de maquinita):
    /// <list type="bullet">
    /// <item><see cref="Pickup"/>: se recoge un sobre (una nota suave, siempre la misma). <see cref="Bump"/>: choque con un asteroide.</item>
    /// <item><see cref="Deliver"/>: paquete entregado (soplo + campanas do-mi-sol).</item>
    /// <item><see cref="WrongPlanet"/>: planeta equivocado (dos notas suaves que bajan, sin castigo).</item>
    /// <item><see cref="Missed"/>: un planeta del encargo se fue sin su paquete (campana grave, suave).</item>
    /// <item><see cref="RadioOk"/> / <see cref="RadioOff"/>: aviso por radio a tiempo (dos pitidos claros) o a destiempo.</item>
    /// <item><see cref="Peek"/>: se mira el reloj (tic-tac de madera).</item>
    /// </list>
    /// </summary>
    public static class MailSounds
    {
        private const int Rate = 44100;
        private static readonly Dictionary<string, AudioClip> Cache = new Dictionary<string, AudioClip>();

        /// <summary>Siempre la misma nota corta y suave (Ricardo, 28-sep: la escala que subía y bajaba terminaba irritando).</summary>
        public static AudioClip Pickup() => Get("pickup", () => Make("pickup", 0.22f, t =>
            0.4f * (0.8f * Sin(783.99f, t) * Env(t, 0.003f, 0.06f) + 0.15f * Sin(783.99f * 3f, t) * Env(t, 0.001f, 0.015f))));

        /// <summary>Choque con un asteroide: golpe sordo y grave (sin estridencia).</summary>
        public static AudioClip Bump() => Get("bump", () => Make("bump", 0.4f, t =>
            0.55f * Sin(98f + 60f * Mathf.Exp(-t / 0.05f), t) * Env(t, 0.002f, 0.12f) + 0.2f * Noise(t, 400f) * Env(t, 0.001f, 0.05f)));

        public static AudioClip Deliver() => Get("deliver", () => Make("deliver", 1.1f, t =>
        {
            float air = 0.22f * Swell(t, 0.18f) * Env(Mathf.Max(0f, t - 0.18f), 0.001f, 0.08f) * Noise(t, 900f + 2200f * Mathf.Clamp01(t / 0.2f));
            float[] notes = { 523.25f, 659.25f, 783.99f, 1046.50f };
            float s = 0f;
            for (int i = 0; i < notes.Length; i++) s += 0.2f * Bell(notes[i], t - 0.16f - 0.06f * i, i == notes.Length - 1 ? 0.5f : 0.3f);
            return air + s;
        }, echo: true));

        public static AudioClip WrongPlanet() => Get("wrong", () => Make("wrong", 0.5f, t =>
            0.3f * (Marimba(392f, t) + 0.8f * Marimba(329.63f, t - 0.12f))));

        public static AudioClip Missed() => Get("missed", () => Make("missed", 0.9f, t =>
            0.22f * (Bell(659.25f, t, 0.3f) + 0.8f * Bell(523.25f, t - 0.18f, 0.45f)), echo: true));

        public static AudioClip RadioOk() => Get("radiook", () => Make("radiook", 0.7f, t =>
        {
            float beep1 = Sin(1174.66f, t) * Gate(t, 0.02f, 0.11f);
            float beep2 = Sin(1567.98f, t) * Gate(t, 0.16f, 0.26f);
            float crackle = 0.08f * Noise(t, 3000f) * Env(t, 0.002f, 0.05f);
            return 0.28f * (beep1 + beep2) + crackle + 0.16f * Bell(1046.5f, t - 0.3f, 0.3f);
        }, echo: true));

        public static AudioClip RadioOff() => Get("radiooff", () => Make("radiooff", 0.4f, t =>
            0.22f * Sin(587.33f, t) * Gate(t, 0.01f, 0.18f) + 0.06f * Noise(t, 2500f) * Env(t, 0.002f, 0.06f)));

        public static AudioClip Peek() => Get("peek", () => Make("peek", 0.35f, t =>
            0.4f * (Wood(2400f, t) + 0.8f * Wood(1800f, t - 0.16f))));

        // ------------------------------------------------------------------ síntesis

        private static float Marimba(float hz, float t) => t < 0f ? 0f :
            0.8f * Sin(hz, t) * Env(t, 0.004f, 0.14f) + 0.22f * Sin(hz * 4f, t) * Env(t, 0.002f, 0.03f);

        private static float Wood(float hz, float t) => t < 0f ? 0f : Sin(hz, t) * Env(t, 0.001f, 0.018f) + 0.4f * Sin(hz * 0.5f, t) * Env(t, 0.001f, 0.03f);

        private static float Bell(float hz, float t, float tau) => t < 0f ? 0f :
            Sin(hz, t) * Env(t, 0.003f, tau) + 0.28f * Sin(hz * 2.76f, t) * Env(t, 0.002f, tau * 0.3f);

        /// <summary>Tono con entrada y salida cortas entre <paramref name="a"/> y <paramref name="b"/> segundos.</summary>
        private static float Gate(float t, float a, float b)
        {
            if (t < a || t > b) return 0f;
            return Mathf.Min(1f, Mathf.Min((t - a) / 0.006f, (b - t) / 0.006f));
        }

        private static float Noise(float t, float center)
        {
            float s = 0f;
            float[] ratios = { 0.71f, 0.93f, 1.07f, 1.29f, 1.51f, 0.83f };
            for (int i = 0; i < ratios.Length; i++) s += Mathf.Sin(2f * Mathf.PI * center * ratios[i] * t + i * 1.7f);
            return s / ratios.Length;
        }

        private static float Swell(float t, float peak)
        {
            float k = Mathf.Clamp01(t / peak);
            float s = Mathf.Sin(k * Mathf.PI * 0.5f);
            return s * s;
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
