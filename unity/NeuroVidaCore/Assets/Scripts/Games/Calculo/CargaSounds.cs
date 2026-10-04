using System;
using System.Collections.Generic;
using UnityEngine;

namespace NeuroVida.Games.Calculo
{
    /// <summary>
    /// Sonido de «Carga exacta» (la identidad de la app: campanas en la pentatónica de do, nada arcade), sintetizado por código como el del boceto aprobado:
    /// elegir una celda o una operación es una campanita suave; juntar dos celdas hace sonar una nota que SUBE con cada paso (la nota 4, 5, 6… de la escala) con un
    /// soplo de luz; lo imposible es un golpe grave y blando (sin chicharra); la pista de Nubi son dos campanas; al lograr la carga, un arpegio que sube.
    /// </summary>
    public static class CargaSounds
    {
        private const int Rate = 44100;
        public static readonly float[] Penta = { 523.25f, 587.33f, 659.25f, 783.99f, 880f, 1046.5f, 1174.66f, 1318.5f, 1567.98f, 1760f };
        private static readonly Dictionary<string, AudioClip> Cache = new Dictionary<string, AudioClip>();

        /// <summary>Se elige una celda: campanita suave y corta (la nota 3 de la escala).</summary>
        public static AudioClip Select() => Get("select", () => Make("select", 0.5f, t => 0.2f * Bell(Penta[2], t, 0.09f)));

        /// <summary>Se elige una operación: otra campanita, un poco más aguda.</summary>
        public static AudioClip Operation() => Get("op", () => Make("op", 0.5f, t => 0.22f * Bell(Penta[5], t, 0.09f)));

        /// <summary>Se juntan dos celdas en el paso <paramref name="step"/> (1, 2, 3…): una nota que sube con cada paso y un soplo de luz.</summary>
        public static AudioClip Merge(int step)
        {
            int k = Mathf.Clamp(3 + step, 3, Penta.Length - 1);
            return Get("merge" + k, () => Make("merge", 1.1f, t =>
                0.3f * Bell(Penta[k], t, 0.22f) +
                0.12f * Noise(t, 900f * Mathf.Pow(2600f / 900f, Mathf.Clamp01(t / 0.3f))) * Mathf.Sin(Mathf.Clamp01(t / 0.3f) * Mathf.PI)));
        }

        /// <summary>Una jugada imposible: un golpe grave y blando que baja (sin chicharra ni nada que regañe).</summary>
        public static AudioClip Thud() => Get("thud", () => Make("thud", 0.34f, t =>
        {
            float hz = 220f * Mathf.Pow(130f / 220f, Mathf.Clamp01(t / 0.25f));
            return 0.3f * Mathf.Sin(2f * Mathf.PI * hz * t) * Mathf.Exp(-t / 0.09f);
        }));

        /// <summary>Deshacer o empezar de nuevo: un soplo corto que baja.</summary>
        public static AudioClip Back() => Get("back", () => Make("back", 0.3f, t =>
            0.12f * Noise(t, 1400f * Mathf.Pow(600f / 1400f, Mathf.Clamp01(t / 0.2f))) * Mathf.Sin(Mathf.Clamp01(t / 0.2f) * Mathf.PI)));

        /// <summary>La pista de Nubi: dos campanas (mi y la).</summary>
        public static AudioClip Hint() => Get("hint", () => Make("hint", 1.1f, t =>
            0.18f * (Bell(659.25f, t, 0.2f) + Bell(880f, t - 0.09f, 0.26f))));

        /// <summary>La carga está exacta: un arpegio que sube (cuatro campanas de la escala) con eco.</summary>
        public static AudioClip Arpeggio() => Get("arpeggio", () => Make("arpeggio", 2.0f, t =>
            0.2f * (Bell(Penta[4], t, 0.4f) + Bell(Penta[6], t - 0.09f, 0.4f) + Bell(Penta[8], t - 0.18f, 0.44f) + Bell(Penta[9], t - 0.27f, 0.5f)), echo: true));

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
