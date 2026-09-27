using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

namespace NeuroVida.Games.Contacto
{
    /// <summary>
    /// Sonido de "Primer Contacto", con la identidad de NeuroVida (campanas y cristal en la pentatónica de do de la app,
    /// nada de maquinita):
    /// <list type="bullet">
    /// <item><see cref="Voice"/>: la voz de los nuri (<see cref="NuriVoice"/>), una por frase. Se prepara en otro hilo
    /// (<see cref="Prepare"/>) mientras se arma la escena: en el teléfono tarda unas décimas.</item>
    /// <item><see cref="Materialize"/>: aparecen las cosas (brillo que sube). <see cref="Pick"/>: se elige una (madera).</item>
    /// <item><see cref="Ack"/>: el nuri escucha la respuesta ("mm", dos notas suaves, sin decir si está bien).</item>
    /// <item><see cref="Decoded"/>: ¡palabra descifrada! (arpegio con destellos). <see cref="Parked"/>: una palabra queda
    /// para otro día (dos notas que bajan, suaves, sin culpa).</item>
    /// <item><see cref="Arrival"/>: llega la transmisión al empezar.</item>
    /// </list>
    /// </summary>
    public static class ContactSounds
    {
        private const int Rate = 44100;
        private static readonly Dictionary<string, AudioClip> Cache = new Dictionary<string, AudioClip>();
        private static readonly Dictionary<string, Task<float[]>> Pending = new Dictionary<string, Task<float[]>>();

        // ------------------------------------------------------------------ voz

        /// <summary>Empieza a sintetizar la frase en otro hilo (si no está ya).</summary>
        public static void Prepare(IReadOnlyList<int> phrase)
        {
            string key = ContactContract.PhraseText(phrase);
            if (Cache.ContainsKey(key) || Pending.ContainsKey(key)) return;
            var words = Split(key);
            Pending[key] = Task.Run(() => NuriVoice.Speak(words));
        }

        /// <summary>¿Ya está lista la voz de esa frase?</summary>
        public static bool Ready(IReadOnlyList<int> phrase)
        {
            string key = ContactContract.PhraseText(phrase);
            return Cache.ContainsKey(key) || (Pending.TryGetValue(key, out var t) && t.IsCompleted);
        }

        /// <summary>La voz de la frase (si no se preparó, se hace ahora).</summary>
        public static AudioClip Voice(IReadOnlyList<int> phrase)
        {
            string key = ContactContract.PhraseText(phrase);
            if (Cache.TryGetValue(key, out var clip) && clip != null) return clip;
            float[] data;
            if (Pending.TryGetValue(key, out var task))
            {
                data = task.Result;
                Pending.Remove(key);
            }
            else data = NuriVoice.Speak(Split(key));
            clip = AudioClip.Create("nuri " + key, data.Length, 1, NuriVoice.Rate, false);
            clip.SetData(data, 0);
            if (Cache.Count > 120) Cache.Clear();
            Cache[key] = clip;
            return clip;
        }

        /// <summary>Tiempos de la frase (boca y palabra que suena), iguales a los de la voz.</summary>
        public static NuriVoice.Plan PlanFor(IReadOnlyList<int> phrase) => NuriVoice.MakePlan(Split(ContactContract.PhraseText(phrase)));

        private static string[] Split(string text) => text.Split(' ');

        // ------------------------------------------------------------------ efectos

        public static AudioClip Materialize() => Get("materialize", () => Make("materialize", 0.55f, t =>
        {
            float s = 0f;
            float[] notes = { 783.99f, 1046.50f, 1318.51f, 1567.98f };
            for (int i = 0; i < notes.Length; i++) s += 0.12f * Bell(notes[i], t - 0.045f * i, 0.18f);
            return s + 0.35f * Glitter(t, 5);
        }, echo: true));

        public static AudioClip Pick() => Get("pick", () => Make("pick", 0.25f, t =>
            0.45f * (Sin(880f, t) * Env(t, 0.002f, 0.045f) + 0.3f * Sin(2406f, t) * Env(t, 0.001f, 0.01f))));

        public static AudioClip Ack() => Get("ack", () => Make("ack", 0.5f, t =>
        {
            // "Mm-hm" de dos notas con vibrato: el nuri escuchó (no dice si está bien).
            float a = Hum(392f, t, 0.02f, 0.13f), b = Hum(523.25f, t - 0.15f, 0.02f, 0.16f);
            return 0.22f * (a + b);
        }, echo: true));

        public static AudioClip Decoded() => Get("decoded", () => Make("decoded", 1.2f, t =>
        {
            float[] notes = { 523.25f, 659.25f, 783.99f, 1046.50f, 1318.51f };
            float s = 0f;
            for (int i = 0; i < notes.Length; i++) s += 0.2f * Bell(notes[i], t - 0.075f * i, i == notes.Length - 1 ? 0.55f : 0.3f);
            return s + 0.45f * Glitter(t - 0.3f, 17);
        }, echo: true));

        public static AudioClip Parked() => Get("parked", () => Make("parked", 0.7f, t =>
            0.2f * (Bell(783.99f, t, 0.25f) + 0.8f * Bell(659.25f, t - 0.16f, 0.35f)), echo: true));

        public static AudioClip Arrival() => Get("arrival", () => Make("arrival", 1.4f, t =>
        {
            float air = 0.2f * Swell(t, 0.6f) * Env(Mathf.Max(0f, t - 0.6f), 0.001f, 0.25f) * Noise(t, 700f + 2600f * Mathf.Clamp01(t / 0.6f));
            float s = 0f;
            float[] notes = { 659.25f, 783.99f, 1046.50f };
            for (int i = 0; i < notes.Length; i++) s += 0.2f * Bell(notes[i], t - 0.45f - 0.12f * i, 0.45f);
            return air + s + 0.4f * Glitter(t - 0.7f, 29);
        }, echo: true));

        // ------------------------------------------------------------------ síntesis (misma familia que la Bitácora)

        private static float Hum(float hz, float t, float attack, float tau)
        {
            if (t < 0f) return 0f;
            float vib = 1f + 0.02f * Mathf.Sin(2f * Mathf.PI * 6f * t);
            float ph = 2f * Mathf.PI * hz * vib * t;
            return (Mathf.Sin(ph) + 0.3f * Mathf.Sin(2f * ph) + 0.12f * Mathf.Sin(3f * ph)) * Env(t, attack, tau);
        }

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
                s += 0.12f * Sin(hz, t) * Env(t - start, 0.001f, 0.035f) * (t >= start ? 1f : 0f);
            }
            return s;
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

        /// <summary>Muestras de un efecto (para la vista previa fuera de Unity).</summary>
        public static float[] Samples(string name, float seconds, Func<float, float> sample, bool echo)
        {
            int n = Mathf.CeilToInt(seconds * Rate);
            var data = new float[n];
            for (int i = 0; i < n; i++) data[i] = sample(i / (float)Rate);
            if (echo)
            {
                int d = (int)(0.14f * Rate);
                for (int i = n - 1; i >= d; i--) data[i] += 0.32f * data[i - d];
                for (int i = n - 1; i >= 2 * d; i--) data[i] += 0.12f * data[i - 2 * d];
            }
            for (int i = 0; i < n; i++) data[i] = Mathf.Clamp(data[i], -1f, 1f);
            int f = Mathf.Min(n, Rate / 100);
            for (int i = 0; i < f; i++) data[n - 1 - i] *= i / (float)f;
            return data;
        }

        private static AudioClip Make(string name, float seconds, Func<float, float> sample, bool echo = false)
        {
            var data = Samples(name, seconds, sample, echo);
            var clip = AudioClip.Create(name, data.Length, 1, Rate, false);
            clip.SetData(data, 0);
            return clip;
        }

        private static float Sin(float hz, float t) => Mathf.Sin(2f * Mathf.PI * hz * t);

        private static float Env(float t, float attack, float tau) =>
            t < 0f ? 0f : (t < attack ? t / attack : 1f) * Mathf.Exp(-Mathf.Max(0f, t - attack) / tau);
    }
}
