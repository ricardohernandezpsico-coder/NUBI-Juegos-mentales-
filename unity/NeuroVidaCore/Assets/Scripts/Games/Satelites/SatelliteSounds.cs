using System;
using System.Collections.Generic;
using UnityEngine;

namespace NeuroVida.Games.Satelites
{
    /// <summary>
    /// Sonido PROPIO de «Satélites: enciende tu planeta», sintetizado por código (docs/diseno-satelites.md §8): campanas pentatónicas, una por satélite con mensaje, al presentarlos; un zumbido grave y suave mientras giran (se repite sin
    /// cortes); un «clac» al detenerse; una nota al marcar (más aguda con cada marca) y un golpecito al quitarla; un arpegio al acertar y un tono suave que baja si faltó alguno (nunca un castigo); un destello cuando cada luz se enciende;
    /// dos campanas para el aviso de sorpresa y para el final. Nada suena fuerte: todo ≤ 0,5.
    /// </summary>
    public static class SatelliteSounds
    {
        private const int Rate = 44100;
        private static readonly Dictionary<string, AudioClip> Cache = new Dictionary<string, AudioClip>();

        /// <summary>La escala pentatónica de do mayor desde do5 (523 Hz): la de los otros juegos.</summary>
        public static readonly float[] Penta = { 523.25f, 587.33f, 659.25f, 783.99f, 880f, 1046.5f, 1174.66f, 1318.51f, 1567.98f, 1760f };

        /// <summary>La campana de la presentación del satélite número <paramref name="index"/> (0 a 4).</summary>
        public static AudioClip CueBell(int index) => Get("cue" + Mathf.Clamp(index, 0, 4), () => Make("cue", 1.0f, t => 0.26f * Bell(Penta[2 + Mathf.Clamp(index, 0, 4)], t, 0.3f)));

        /// <summary>El zumbido del seguimiento: dos senos graves (110 y 165 Hz) de un segundo exacto (ciclos enteros: se repite sin chasquido).</summary>
        public static AudioClip Hum() => Get("hum", () =>
        {
            int n = Rate;
            var data = new float[n];
            for (int i = 0; i < n; i++)
            {
                float t = i / (float)Rate;
                data[i] = 0.5f * (0.6f * Sin(110f, t) + 0.4f * Sin(165f, t));
            }
            var clip = AudioClip.Create("hum", n, 1, Rate, false);
            clip.SetData(data, 0);
            return clip;
        });

        /// <summary>«Clac» al detenerse: un golpe corto y seco.</summary>
        public static AudioClip Stop() => Get("stop", () => Make("stop", 0.3f, t =>
        {
            float hz = 260f * Mathf.Pow(150f / 260f, Mathf.Clamp01(t / 0.12f));
            return 0.28f * Sin(hz, t) * Mathf.Exp(-t / 0.05f) + 0.12f * Noise(t, 1800f) * Mathf.Exp(-t / 0.02f);
        }));

        /// <summary>Marcar: una nota por marca (más aguda con cada una).</summary>
        public static AudioClip Mark(int n) => Get("mark" + Mathf.Clamp(n, 1, 7), () => Make("mark", 0.5f, t => 0.22f * Bell(Penta[Mathf.Clamp(n, 1, 7) + 1], t, 0.12f)));

        /// <summary>Quitar una marca: un golpecito suave que baja.</summary>
        public static AudioClip Unmark() => Get("unmark", () => Make("unmark", 0.2f, t =>
        {
            float hz = 440f * Mathf.Pow(330f / 440f, Mathf.Clamp01(t / 0.1f));
            return 0.16f * Sin(hz, t) * Mathf.Exp(-t / 0.05f);
        }));

        /// <summary>Un toque que no cuenta (ya están todos marcados): un tono corto y bajo.</summary>
        public static AudioClip Blocked() => Get("blocked", () => Make("blocked", 0.2f, t => 0.12f * Sin(300f, t) * Mathf.Exp(-t / 0.05f)));

        /// <summary>Acertar todo: un arpegio de cuatro campanas que sube (con más notas, más brillo).</summary>
        public static AudioClip Perfect() => Get("perfect", () => Make("perfect", 1.5f, t =>
            0.2f * (Bell(Penta[2], t, 0.3f) + Bell(Penta[4], t - 0.09f, 0.3f) + Bell(Penta[5], t - 0.18f, 0.32f) + Bell(Penta[7], t - 0.27f, 0.4f)), echo: true));

        /// <summary>Faltó alguno: un tono suave que baja (392 → 330 Hz); no es un castigo.</summary>
        public static AudioClip Partial() => Get("partial", () => Make("partial", 0.5f, t =>
        {
            float hz = 392f * Mathf.Pow(330f / 392f, Mathf.Clamp01(t / 0.25f));
            return 0.2f * Sin(hz, t) * Mathf.Exp(-t / 0.14f);
        }));

        /// <summary>Una luz que se enciende en el planeta: una campanita aguda; la <paramref name="index"/> sube por la escala.</summary>
        public static AudioClip LightOn(int index) => Get("light" + (Mathf.Abs(index) % 5), () => Make("light", 0.9f, t => 0.14f * Bell(Penta[4 + Mathf.Abs(index) % 5], t, 0.22f)));

        /// <summary>El aviso de sorpresa: dos campanas (mi y la).</summary>
        public static AudioClip Chime() => Get("chime", () => Make("chime", 1.2f, t => 0.16f * (Bell(659.25f, t, 0.2f) + Bell(880f, t - 0.09f, 0.26f))));

        /// <summary>Fin de la partida: dos campanas lentas (sol y do agudos).</summary>
        public static AudioClip Finale() => Get("finale", () => Make("finale", 2.0f, t => 0.2f * (Bell(784f, t, 0.5f) + Bell(1046.5f, t - 0.15f, 0.55f)), echo: true));

        // ------------------------------------------------------------------ síntesis

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
            float peak = 0f;
            for (int i = 0; i < n; i++) peak = Mathf.Max(peak, Mathf.Abs(data[i]));
            float gain = peak > 0.5f ? 0.5f / peak : 1f;            // nada pasa de 0,5
            for (int i = 0; i < n; i++) data[i] = Mathf.Clamp(data[i] * gain, -1f, 1f);
            int f = Mathf.Min(n, Rate / 100);
            for (int i = 0; i < f; i++) data[n - 1 - i] *= i / (float)f;
            var clip = AudioClip.Create(name, n, 1, Rate, false);
            clip.SetData(data, 0);
            return clip;
        }

        /// <summary>Para las pruebas y el calentamiento: sintetiza todos los clips (cada uno una sola vez), de a poco.</summary>
        public static System.Collections.IEnumerator Prewarm()
        {
            for (int i = 0; i < 5; i++) CueBell(i);
            Hum(); Stop();
            yield return null;
            for (int i = 1; i <= 7; i++) Mark(i);
            Unmark(); Blocked();
            yield return null;
            Perfect(); Partial();
            for (int i = 0; i < 5; i++) LightOn(i);
            yield return null;
            Chime(); Finale();
        }

        public static int CachedCount => Cache.Count;
    }
}
