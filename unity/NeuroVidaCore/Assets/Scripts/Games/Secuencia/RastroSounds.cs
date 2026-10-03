using System;
using System.Collections.Generic;
using UnityEngine;

namespace NeuroVida.Games.Secuencia
{
    /// <summary>
    /// Sonido de «Rastro de luz» (docs/diseno-rastro-de-luz.md §7), sintetizado por código como el boceto aprobado: cada lucero suena con su nota
    /// de la pentatónica (una campana cristalina: fundamental + parcial 2,76× + 5,4×), así el camino se ve y se OYE como una melodía; el acierto
    /// es un acorde de cuatro notas; el error, una campana grave y suave; al girar, un soplo de aire; y «¡NUEVO!», dos campanas. Nada arcade.
    /// </summary>
    public static class RastroSounds
    {
        private const int Rate = 44100;
        /// <summary>Las campanas del boceto suenan a 0,05-0,08 de amplitud en el navegador; en el teléfono se suben para que se oigan igual.</summary>
        private const float Gain = 3.2f;
        private static readonly Dictionary<string, AudioClip> Cache = new Dictionary<string, AudioClip>();

        /// <summary>La nota del lucero <paramref name="orb"/>: siempre la misma.</summary>
        public static AudioClip Orb(int orb) => Get("orb" + orb, () => Make("orb", 1.3f, t => Bell(RastroBoard.Notes[orb], t, 0.08f, 1.3f)));

        /// <summary>Acierto de ronda: do-mi-sol-do con las campanas escalonadas 70 ms.</summary>
        public static AudioClip Chord() => Get("chord", () => Make("chord", 2.0f, t =>
        {
            float[] n = { 523.25f, 659.25f, 783.99f, 1046.5f };
            float s = 0f;
            for (int i = 0; i < n.Length; i++) s += Bell(n[i], t - i * 0.07f, 0.05f, 1.6f);
            return s;
        }));

        /// <summary>Error: una campana grave y suave (196 Hz).</summary>
        public static AudioClip Wrong() => Get("wrong", () => Make("wrong", 0.7f, t => Bell(196f, t, 0.06f, 0.6f)));

        /// <summary>«¡NUEVO!»: dos campanas, la segunda 120 ms después.</summary>
        public static AudioClip Unlock() => Get("unlock", () => Make("unlock", 1.7f, t =>
            Bell(1046.5f, t, 0.05f, 1.4f) + Bell(1318.5f, t - 0.12f, 0.05f, 1.4f)));

        /// <summary>El soplo de aire del cielo al girar: ruido que pasa por un filtro de banda que sube de 900 a 2400 Hz en medio segundo.</summary>
        public static AudioClip Air() => Get("air", () =>
        {
            int n = Rate / 2;
            var data = new float[n];
            var rng = new System.Random(77);
            float y1 = 0f, y2 = 0f, x1 = 0f, x2 = 0f, peak = 1e-6f;
            for (int i = 0; i < n; i++)
            {
                float k = i / (float)n;
                float f = 900f * Mathf.Pow(2400f / 900f, k);
                float w0 = 2f * Mathf.PI * f / Rate, alpha = Mathf.Sin(w0) / (2f * 6f);
                float b0 = alpha, b2 = -alpha, a0 = 1f + alpha, a1 = -2f * Mathf.Cos(w0), a2 = 1f - alpha;
                float x0 = ((float)rng.NextDouble() * 2f - 1f) * Mathf.Sin(Mathf.PI * k);
                float y0 = (b0 * x0 + b2 * x2 - a1 * y1 - a2 * y2) / a0;
                x2 = x1; x1 = x0; y2 = y1; y1 = y0;
                data[i] = y0;
                peak = Mathf.Max(peak, Mathf.Abs(y0));
            }
            float scale = 0.28f / peak;
            for (int i = 0; i < n; i++) data[i] *= scale;
            var clip = AudioClip.Create("air", n, 1, Rate, false);
            clip.SetData(data, 0);
            return clip;
        });

        // ------------------------------------------------------------------ síntesis

        /// <summary>Una campana de frecuencia <paramref name="hz"/>: ataque de 8 ms hasta <paramref name="vol"/> y caída exponencial hasta el silencio en <paramref name="dur"/> s.</summary>
        private static float Bell(float hz, float t, float vol, float dur)
        {
            if (t < 0f || t >= dur) return 0f;
            const float floor = 0.0001f, attack = 0.008f;
            float amp = t < attack ? floor * Mathf.Pow(vol / floor, t / attack) : vol * Mathf.Pow(floor / vol, (t - attack) / (dur - attack));
            float s = Sin(hz, t) + 0.18f * Sin(hz * 2.76f, t) + 0.06f * Sin(hz * 5.4f, t);
            return amp * s * Gain;
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

        private static AudioClip Make(string name, float seconds, Func<float, float> sample)
        {
            int n = Mathf.CeilToInt(seconds * Rate);
            var data = new float[n];
            for (int i = 0; i < n; i++) data[i] = Mathf.Clamp(sample(i / (float)Rate), -1f, 1f);
            int f = Mathf.Min(n, Rate / 100);
            for (int i = 0; i < f; i++) data[n - 1 - i] *= i / (float)f;
            var clip = AudioClip.Create(name, n, 1, Rate, false);
            clip.SetData(data, 0);
            return clip;
        }
    }
}
