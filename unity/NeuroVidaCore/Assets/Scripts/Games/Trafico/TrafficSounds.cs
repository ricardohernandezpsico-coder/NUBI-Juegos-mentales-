using System;
using System.Collections.Generic;
using UnityEngine;

namespace NeuroVida.Games.Trafico
{
    /// <summary>
    /// Sonidos arcade de "Tráfico Estelar", sintetizados por código (sin archivos) con timbre de consola retro pero
    /// suave: ondas "cuadradas" armadas con pocos armónicos (sin la aspereza de una cuadrada pura).
    /// <list type="bullet">
    /// <item><see cref="EngineLoop"/>: zumbido de motor que se repite sin cortes (1 s; todas las frecuencias son enteras,
    /// así el final empalma con el principio). El juego lo sube y lo afina según cuántas cápsulas viajan.</item>
    /// <item><see cref="Launch"/>: "fiu" que sube (sale una cápsula de la compuerta).</item>
    /// <item><see cref="Switch"/>: "blip" de dos notas (sube o baja según el lado al que quedó el desvío).</item>
    /// <item><see cref="Clack"/>: "clac" leve de riel cuando una cápsula pasa por un desvío.</item>
    /// <item><see cref="Coin"/>: "ba-ding" de moneda; la nota sube con la racha (pentatónica, nunca desafina).</item>
    /// <item><see cref="Miss"/>: "buuu-om" que baja, sin chicharra ni castigo.</item>
    /// <item><see cref="Fanfare"/>: arpeggio de oleada perfecta. <see cref="PowerUp"/>: barrido que sube (más tráfico).</item>
    /// </list>
    /// </summary>
    public static class TrafficSounds
    {
        private const int Rate = 44100;
        private static readonly float[] Pentatonic = { 659.25f, 739.99f, 830.61f, 987.77f, 1108.73f, 1318.51f, 1479.98f, 1661.22f };
        private static readonly Dictionary<string, AudioClip> Cache = new Dictionary<string, AudioClip>();

        public static AudioClip EngineLoop() => Get("engine", () => Make("engine", 1f, t =>
        {
            // Dos voces casi iguales (220 y 221 Hz: un "uuaa" lento de 1 Hz), el armónico de 440 para que se oiga en
            // el parlante del teléfono, un pulso de 8 Hz de turbina y un silbido muy bajo con vibrato.
            float trem = 0.82f + 0.18f * Sin(8f, t);
            float body = 0.34f * Soft(220f, t) + 0.26f * Soft(221f, t) + 0.12f * Sin(440f, t);
            float whine = 0.035f * Mathf.Sin(2f * Mathf.PI * 1320f * t + 2.5f * Sin(5f, t));
            return trem * body + whine;
        }, fade: false));

        public static AudioClip Launch() => Get("launch", () => Make("launch", 0.26f, t =>
        {
            // Barrido exponencial 320 → 1100 Hz (fase integrada, sin saltos) con caída rápida.
            const float f0 = 320f, f1 = 1100f, T = 0.2f;
            float r = Mathf.Log(f1 / f0);
            float u = Mathf.Min(t, T);
            float phase = 2f * Mathf.PI * f0 * T / r * (Mathf.Exp(r * u / T) - 1f) + (t > T ? 2f * Mathf.PI * f1 * (t - T) : 0f);
            return 0.55f * SquareSoft(phase) * Env(t, 0.004f, 0.07f);
        }));

        public static AudioClip Switch(bool up) => Get(up ? "switch_up" : "switch_down", () => Make("switch", 0.1f, t =>
        {
            float a = up ? 1046.5f : 1568f, b = up ? 1568f : 1046.5f;
            float hz = t < 0.035f ? a : b;
            float local = t < 0.035f ? t : t - 0.035f;
            return 0.5f * SquareSoft(2f * Mathf.PI * hz * t) * Env(local, 0.002f, 0.025f);
        }));

        public static AudioClip Clack() => Get("clack", () =>
        {
            var noise = new System.Random(7);
            return Make("clack", 0.06f, t =>
                (0.5f * Sin(520f, t) + 0.35f * ((float)noise.NextDouble() * 2f - 1f)) * Env(t, 0.001f, 0.012f)
                + 0.3f * Sin(1180f, t) * Env(t, 0.001f, 0.006f));
        });

        public static AudioClip Coin(int streak)
        {
            int step = Mathf.Clamp(streak - 1, 0, Pentatonic.Length - 1);
            return Get("coin" + step, () => Make("coin", 0.32f, t =>
            {
                float n1 = Pentatonic[step], n2 = n1 * 4f / 3f;
                if (t < 0.07f) return 0.6f * SquareSoft(2f * Mathf.PI * n1 * t) * Env(t, 0.002f, 0.05f);
                return 0.6f * SquareSoft(2f * Mathf.PI * n2 * t) * Env(t - 0.07f, 0.002f, 0.09f);
            }));
        }

        public static AudioClip Miss() => Get("miss", () => Make("miss", 0.42f, t =>
        {
            // 440 → 150 Hz en 0,36 s (fase integrada, sin saltos) con un vibrato que "tambalea": el clásico "buuu-om".
            const float f0 = 440f, f1 = 150f, T = 0.36f;
            float u = Mathf.Min(t, T);
            float cycles = f0 * u - (f0 - f1) * u * u / (2f * T) + (t > T ? f1 * (t - T) : 0f);
            float phase = 2f * Mathf.PI * cycles + 0.6f * Sin(11f, t);
            return 0.5f * (Mathf.Sin(phase) + 0.2f * Mathf.Sin(3f * phase)) * Env(t, 0.006f, 0.16f);
        }));

        public static AudioClip Fanfare() => Get("fanfare", () => Make("fanfare", 0.7f, t =>
        {
            float[] notes = { 783.99f, 987.77f, 1174.66f, 1567.98f };
            float s = 0f;
            for (int i = 0; i < notes.Length; i++)
            {
                float start = i * 0.07f;
                if (t < start) break;
                float hold = i == notes.Length - 1 ? 0.22f : 0.05f;
                s += 0.3f * SquareSoft(2f * Mathf.PI * notes[i] * t) * Env(t - start, 0.003f, hold);
            }
            return s;
        }));

        public static AudioClip PowerUp() => Get("powerup", () => Make("powerup", 0.36f, t =>
        {
            // Escalera rápida que sube (8 pasos de semitono doble) con trémolo: "¡más tráfico!".
            int stepIndex = Mathf.Min(7, (int)(t / 0.035f));
            float hz = 523.25f * Mathf.Pow(2f, stepIndex * 2f / 12f);
            return 0.36f * SquareSoft(2f * Mathf.PI * hz * t) * (0.75f + 0.25f * Sin(30f, t)) * Env(t, 0.003f, 0.2f);
        }));

        // ------------------------------------------------------------------ síntesis

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
            int n = Mathf.CeilToInt(seconds * Rate);
            if (!fade) n = Mathf.RoundToInt(seconds * Rate); // bucle: largo exacto para empalmar
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

        /// <summary>Casi triangular (armónicos impares que caen rápido): cuerpo cálido.</summary>
        private static float Soft(float hz, float t)
        {
            float x = 2f * Mathf.PI * hz * t;
            return Mathf.Sin(x) - Mathf.Sin(3f * x) / 9f + Mathf.Sin(5f * x) / 25f;
        }

        /// <summary>"Cuadrada" de consola retro con solo 3 armónicos: brillante pero sin aspereza.</summary>
        private static float SquareSoft(float phase) =>
            0.8f * (Mathf.Sin(phase) + Mathf.Sin(3f * phase) / 3f * 0.7f + Mathf.Sin(5f * phase) / 5f * 0.45f);

        private static float Env(float t, float attack, float tau) =>
            t < 0f ? 0f : (t < attack ? t / attack : 1f) * Mathf.Exp(-Mathf.Max(0f, t - attack) / tau);
    }
}
