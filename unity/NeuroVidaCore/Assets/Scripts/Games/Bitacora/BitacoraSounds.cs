using System;
using System.Collections.Generic;
using UnityEngine;

namespace NeuroVida.Games.Bitacora
{
    /// <summary>
    /// Sonido de "Bitácora de Misión": moderno y con la identidad de NeuroVida (nada de maquinita). Campanas de cristal y
    /// "destellos" (brillo de parciales muy agudos que se apagan al instante), con un eco suave que les da aire; todo en
    /// la pentatónica de do de la app, así se mezcla afinado con los sonidos comunes de <see cref="Shared.GameFeel"/>.
    /// <list type="bullet">
    /// <item><see cref="Incoming"/>: llega la transmisión (soplo que sube + tres campanas).</item>
    /// <item><see cref="Reveal"/>: aparece un hallazgo; cada parada tiene su nota, así la misión suena como una melodía
    /// (el orden también se oye).</item>
    /// <item><see cref="Save"/>: se guarda en la bitácora (destello que baja + toque de madera).</item>
    /// <item><see cref="Travel"/>: la sonda viaja al planeta siguiente (soplo corto).</item>
    /// <item><see cref="Catch"/>: patrulla, cometa atrapado (marimba). <see cref="Report"/>: se abre el informe.</item>
    /// <item><see cref="Archive"/>: lo recordado entra a la bitácora (lluvia de campanas con destellos).</item>
    /// </list>
    /// </summary>
    public static class BitacoraSounds
    {
        private const int Rate = 44100;
        private static readonly float[] Penta = { 523.25f, 587.33f, 659.25f, 783.99f, 880.00f, 1046.50f, 1174.66f, 1318.51f, 1567.98f, 1760.00f };
        private static readonly Dictionary<string, AudioClip> Cache = new Dictionary<string, AudioClip>();

        public static AudioClip Incoming() => Get("incoming", () => Make("incoming", 1.3f, t =>
        {
            float air = 0.25f * Swell(t, 0.5f) * Noise(t, 900f + 2400f * Mathf.Clamp01(t / 0.5f));
            float s = 0f;
            float[] notes = { 783.99f, 1046.50f, 1318.51f };
            for (int i = 0; i < notes.Length; i++) s += 0.22f * Bell(notes[i], t - 0.18f - 0.11f * i, 0.45f);
            return air + s + 0.5f * Glitter(t - 0.5f, 7);
        }, echo: true));

        public static AudioClip Reveal(int slot) => Get("reveal" + slot, () => Make("reveal", 0.9f, t =>
            0.34f * Bell(Penta[Mathf.Clamp(slot, 0, Penta.Length - 1)], t, 0.4f) + 0.5f * Glitter(t, 11 + slot), echo: true));

        public static AudioClip Save() => Get("save", () => Make("save", 0.5f, t =>
        {
            // Destello que "baja" hacia la bitácora + el toque de madera de la app.
            float sweep = 0.2f * Mathf.Sin(2f * Mathf.PI * (2600f * t - 2400f * t * t)) * Env(t, 0.004f, 0.08f);
            return sweep + 0.3f * (Sin(1046.5f, t) * Env(t, 0.002f, 0.05f) + 0.3f * Sin(2406f, t) * Env(t, 0.001f, 0.012f));
        }, echo: true));

        public static AudioClip Travel() => Get("travel", () => Make("travel", 0.45f, t =>
            0.3f * Swell(t, 0.18f) * Env(Mathf.Max(0f, t - 0.18f), 0.001f, 0.1f) * Noise(t, 500f + 1800f * Mathf.Clamp01(t / 0.25f))));

        public static AudioClip Catch(int k) => Get("catch" + (k % 5), () => Make("catch", 0.35f, t =>
        {
            float hz = Penta[k % 5];
            return 0.5f * (0.8f * Sin(hz, t) * Env(t, 0.004f, 0.11f) + 0.22f * Sin(hz * 4f, t) * Env(t, 0.002f, 0.025f)) + 0.25f * Glitter(t, 3 + k % 5);
        }));

        public static AudioClip Report() => Get("report", () => Make("report", 1.2f, t =>
        {
            float s = 0f;
            float[] notes = { 1318.51f, 1046.50f, 783.99f };
            for (int i = 0; i < notes.Length; i++) s += 0.22f * Bell(notes[i], t - 0.1f * i, 0.5f);
            return s + 0.18f * Swell(t, 0.35f) * Env(Mathf.Max(0f, t - 0.35f), 0.001f, 0.3f) * Noise(t, 1200f);
        }, echo: true));

        public static AudioClip Archive() => Get("archive", () => Make("archive", 1.4f, t =>
        {
            float[] notes = { 1046.50f, 1174.66f, 1318.51f, 1567.98f, 1760.00f, 2093.00f };
            float s = 0f;
            for (int i = 0; i < notes.Length; i++) s += 0.18f * Bell(notes[i], t - 0.07f * i, i == notes.Length - 1 ? 0.6f : 0.35f);
            return s + 0.45f * Glitter(t - 0.3f, 23);
        }, echo: true));

        // ------------------------------------------------------------------ síntesis

        /// <summary>Campana de cristal: fundamental + parciales inarmónicos que se apagan antes.</summary>
        private static float Bell(float hz, float t, float tau) => t < 0f ? 0f :
            Sin(hz, t) * Env(t, 0.003f, tau) + 0.28f * Sin(hz * 2.76f, t) * Env(t, 0.002f, tau * 0.3f)
            + 0.1f * Sin(hz * 5.4f, t) * Env(t, 0.001f, tau * 0.1f);

        /// <summary>
        /// Destello: seis parciales muy agudos (3,5-8 kHz) que entran escalonados y se apagan en milésimas, como un
        /// brillo. Cada semilla da un destello distinto (siempre el mismo para la misma semilla).
        /// </summary>
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
                s += 0.12f * Sin(hz, t) * Env(t - start, 0.001f, 0.035f) * (t >= start ? 1f : 0f);
            }
            return s;
        }

        /// <summary>Ruido suave pseudoaleatorio (suma de senos inarmónicos alrededor de un centro): soplo sin siseo.</summary>
        private static float Noise(float t, float center)
        {
            float s = 0f;
            float[] ratios = { 0.71f, 0.93f, 1.07f, 1.29f, 1.51f, 0.83f };
            for (int i = 0; i < ratios.Length; i++) s += Mathf.Sin(2f * Mathf.PI * center * ratios[i] * t + i * 1.7f);
            return s / ratios.Length;
        }

        /// <summary>Entrada suave hasta <paramref name="peak"/> segundos (envolvente sen²).</summary>
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
                // Eco suave (dos rebotes a 140 ms): da aire y sensación de espacio, sin reverberación pesada.
                int d = (int)(0.14f * Rate);
                for (int i = n - 1; i >= d; i--) data[i] += 0.32f * data[i - d];
                for (int i = n - 1; i >= 2 * d; i--) data[i] += 0.12f * data[i - 2 * d];
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
