using System;
using System.Collections.Generic;
using UnityEngine;

namespace NeuroVida.Games.Constelacion
{
    /// <summary>
    /// Sonido de Constelación de Palabras (identidad de la app: campanas de cristal y destellos, en la pentatónica de do,
    /// nada de maquinita). La idea musical dibuja la medida: cada palabra que SIGUE una constelación suena una nota más
    /// arriba (una constelación larga es una melodía que sube); un SALTO a otro grupo vuelve a la nota de partida. Así se
    /// oye cómo se agrupa y cómo se salta.
    /// <list type="bullet">
    /// <item><see cref="Star"/>: nace una estrella (nota según su lugar en la constelación).</item>
    /// <item><see cref="Formed"/>: dos palabras forman una constelación (destello que se abre).</item>
    /// <item><see cref="Repeat"/>: esa palabra ya estaba (toque suave de madera, sin castigo).</item>
    /// <item><see cref="Listen"/> / <see cref="Stop"/>: el micrófono se enciende / se apaga.</item>
    /// <item><see cref="RoundEnd"/>: el cielo de la ronda (lluvia de campanas).</item>
    /// </list>
    /// </summary>
    public static class ConstellationSounds
    {
        private const int Rate = 44100;
        private static readonly float[] Penta = { 523.25f, 587.33f, 659.25f, 783.99f, 880.00f, 1046.50f, 1174.66f, 1318.51f, 1567.98f, 1760.00f };
        private static readonly Dictionary<string, AudioClip> Cache = new Dictionary<string, AudioClip>();

        public static AudioClip Star(int step) => Get("star" + Mathf.Clamp(step, 0, Penta.Length - 1), () => Make("star", 0.8f, t =>
        {
            float hz = Penta[Mathf.Clamp(step, 0, Penta.Length - 1)];
            return 0.32f * Bell(hz, t, 0.32f) + 0.3f * Glitter(t, 5 + step);
        }, echo: 1));

        public static AudioClip Formed() => Get("formed", () => Make("formed", 1.0f, t =>
            0.2f * Bell(1567.98f, t - 0.02f, 0.3f) + 0.2f * Bell(2093f, t - 0.1f, 0.35f) + 0.5f * Glitter(t - 0.05f, 31), echo: 2));

        public static AudioClip Repeat() => Get("repeat", () => Make("repeat", 0.2f, t =>
            0.4f * Sin(659.25f, t) * Env(t, 0.002f, 0.03f) + 0.2f * Sin(659.25f * 2.3f, t) * Env(t, 0.001f, 0.01f)));

        public static AudioClip Listen() => Get("listen", () => Make("listen", 0.7f, t =>
            0.26f * Bell(783.99f, t, 0.25f) + 0.26f * Bell(1046.50f, t - 0.1f, 0.35f), echo: 1));

        public static AudioClip Stop() => Get("stop", () => Make("stop", 0.7f, t =>
            0.26f * Bell(1046.50f, t, 0.25f) + 0.26f * Bell(783.99f, t - 0.1f, 0.35f), echo: 1));

        public static AudioClip RoundEnd() => Get("roundend", () => Make("roundend", 1.6f, t =>
        {
            float[] notes = { 1046.50f, 1174.66f, 1318.51f, 1567.98f, 1760.00f, 2093.00f, 2349.32f };
            float s = 0f;
            for (int i = 0; i < notes.Length; i++) s += 0.16f * Bell(notes[i], t - 0.08f * i, i == notes.Length - 1 ? 0.7f : 0.35f);
            return s + 0.4f * Glitter(t - 0.35f, 41) + 0.3f * Glitter(t - 0.6f, 43);
        }, echo: 2));

        // ------------------------------------------------------------------ síntesis

        private static float Bell(float hz, float t, float tau) => t < 0f ? 0f :
            Sin(hz, t) * Env(t, 0.003f, tau) + 0.28f * Sin(hz * 2.76f, t) * Env(t, 0.002f, tau * 0.3f)
            + 0.1f * Sin(hz * 5.4f, t) * Env(t, 0.001f, tau * 0.1f);

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

        private static AudioClip Get(string key, Func<AudioClip> build)
        {
            if (!Cache.TryGetValue(key, out var clip) || clip == null)
            {
                clip = build();
                Cache[key] = clip;
            }
            return clip;
        }

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
