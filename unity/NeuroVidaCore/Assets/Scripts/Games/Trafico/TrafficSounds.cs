using System;
using System.Collections.Generic;
using UnityEngine;

namespace NeuroVida.Games.Trafico
{
    /// <summary>
    /// Sonido de "Tráfico Estelar" con la identidad sonora de NeuroVida (la misma de <see cref="Shared.GameFeel"/>):
    /// tonos cálidos de marimba y campana, todos en la pentatónica de do (la escala de toda la app), así nada desafina con
    /// nada y el tráfico, al pasar, arma una melodía tranquila. Nada de chicharras ni timbre de maquinita (Ricardo pidió
    /// explícitamente que no sonara "arcade").
    /// <list type="bullet">
    /// <item><see cref="EngineLoop"/>: vuelo de las cápsulas. Un colchón suave (do + sol, quinta abierta) con aire
    /// (soplo filtrado) que respira despacio; se repite sin cortes (2 s: todas las frecuencias son múltiplos de 0,5 Hz y
    /// el soplo se funde con su propio comienzo).</item>
    /// <item><see cref="Launch"/>: soplo que se abre + una nota grave de marimba: la cápsula sale de la compuerta.</item>
    /// <item><see cref="Switch"/>: golpecito de madera afinado (mi o sol según el lado al que quedó el desvío).</item>
    /// <item><see cref="PassChime"/>: campanita afinada con el COLOR de la cápsula cuando pasa por un desvío (cada color
    /// tiene su nota: color + símbolo + nota).</item>
    /// <item><see cref="Landing"/>: marimba grave del color al posarse en su planeta (bajo el "pling" de la racha).</item>
    /// <item><see cref="Cascade"/>: lluvia de campanas que sube (oleada perfecta).</item>
    /// </list>
    /// </summary>
    public static class TrafficSounds
    {
        private const int Rate = 44100;

        /// <summary>Nota de cada color: la pentatónica de do de la app (do5 … mi6).</summary>
        private static readonly float[] ColorNote = { 523.25f, 587.33f, 659.25f, 783.99f, 880.00f, 1046.50f, 1174.66f, 1318.51f };

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

        public static AudioClip Launch() => Get("launch", () =>
        {
            const float seconds = 0.5f;
            int n = Mathf.CeilToInt(seconds * Rate);
            var rng = new System.Random(3);
            var whoosh = new float[n];
            float y = 0f;
            for (int i = 0; i < n; i++)
            {
                float t = i / (float)Rate;
                // Soplo que se abre: el filtro deja pasar cada vez más agudos (300 → 2400 Hz).
                float fc = Mathf.Lerp(300f, 2400f, Mathf.Clamp01(t / 0.3f));
                float a = 1f - Mathf.Exp(-2f * Mathf.PI * fc / Rate);
                y += a * ((float)rng.NextDouble() * 2f - 1f - y);
                whoosh[i] = y;
            }
            return Make("launch", seconds, t =>
            {
                int i = Mathf.Min(n - 1, (int)(t * Rate));
                return 0.9f * whoosh[i] * Env(t, 0.09f, 0.1f) + 0.35f * Marimba(392f, t);
            });
        });

        public static AudioClip Switch(bool second) => Get(second ? "switch_b" : "switch_a", () => Make("switch", 0.12f, t =>
        {
            float hz = second ? 783.99f : 659.25f;
            // Madera: la nota corta más un "toc" inarmónico que se apaga enseguida.
            return 0.55f * Sin(hz, t) * Env(t, 0.002f, 0.03f) + 0.3f * Sin(hz * 2.3f, t) * Env(t, 0.001f, 0.01f);
        }));

        public static AudioClip PassChime(int color) => Get("chime" + color, () => Make("chime", 0.5f, t =>
            0.5f * Bell(ColorNote[Mathf.Clamp(color, 0, ColorNote.Length - 1)], t, 0.2f)));

        public static AudioClip Landing(int color) => Get("landing" + color, () => Make("landing", 0.45f, t =>
            0.5f * Marimba(ColorNote[Mathf.Clamp(color, 0, ColorNote.Length - 1)] * 0.5f, t)));

        public static AudioClip Cascade() => Get("cascade", () => Make("cascade", 1.1f, t =>
        {
            float[] notes = { 1046.50f, 1174.66f, 1318.51f, 1567.98f, 1760.00f, 2093.00f };
            float s = 0f;
            for (int i = 0; i < notes.Length; i++)
            {
                float start = i * 0.06f;
                if (t < start) break;
                s += 0.2f * Bell(notes[i], t - start, i == notes.Length - 1 ? 0.5f : 0.3f);
            }
            return s;
        }));

        // ------------------------------------------------------------------ síntesis

        /// <summary>Campana: fundamental + parciales inarmónicos (2,76 y 5,4) que se apagan antes: brillo de cristal.</summary>
        private static float Bell(float hz, float t, float tau) =>
            Sin(hz, t) * Env(t, 0.003f, tau) + 0.25f * Sin(hz * 2.76f, t) * Env(t, 0.002f, tau * 0.3f)
            + 0.08f * Sin(hz * 5.4f, t) * Env(t, 0.001f, tau * 0.1f);

        /// <summary>Marimba (la misma receta de la app): fundamental + parcial 4x corto, el "tok" de madera.</summary>
        private static float Marimba(float hz, float t) =>
            0.8f * Sin(hz, t) * Env(t, 0.004f, 0.11f) + 0.22f * Sin(hz * 4f, t) * Env(t, 0.002f, 0.025f);

        /// <summary>
        /// Soplo (ruido con dos filtros suaves, sin siseo) de <paramref name="n"/> muestras, normalizado a pico ~1, que
        /// empalma consigo mismo: el comienzo se funde con el tramo que seguiría al final.
        /// </summary>
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

        private static float Env(float t, float attack, float tau) =>
            t < 0f ? 0f : (t < attack ? t / attack : 1f) * Mathf.Exp(-Mathf.Max(0f, t - attack) / tau);
    }
}
