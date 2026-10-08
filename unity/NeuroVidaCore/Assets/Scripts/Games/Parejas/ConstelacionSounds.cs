using System;
using System.Collections.Generic;
using UnityEngine;

namespace NeuroVida.Games.Parejas
{
    /// <summary>
    /// Sonido PROPIO de «Constelaciones», sintetizado por código como el del boceto aprobado (docs/diseno-constelaciones.md §6). Cada grupo tiene su NOTA (pentatónica de la app, una octava arriba desde el 11.º) y la luz suena
    /// suave al abrirse: las dos de una pareja suenan igual, así se recuerda con la vista y con el oído. Al encontrar un grupo suena un acorde de quinta (de memoria) o de tercera (a la primera vista), y un acierto de
    /// memoria suma un brillo que sube con la racha. El «no son iguales» es un tono grave y corto, nunca un castigo. Al empezar el cielo suena un zumbido de fondo. Nada suena fuerte: todo ≤ 0,5.
    /// </summary>
    public static class ConstelacionSounds
    {
        private const int Rate = 44100;
        private static readonly Dictionary<string, AudioClip> Cache = new Dictionary<string, AudioClip>();
        private static readonly float[] Penta = ConstelacionContract.Penta;

        /// <summary>La nota propia del grupo: una campana suave (se oye al abrir cualquiera de sus luces).</summary>
        public static AudioClip Note(int group) => Get("note" + group, () => Make("note", 0.9f, t => 0.3f * Bell(ConstelacionContract.NoteHz(group), t, 0.22f)));

        /// <summary>Dar vuelta una luz: un siseo corto y agudo que sube.</summary>
        public static AudioClip FlipUp() => Get("flipup", () => Make("flipup", 0.12f, t =>
        {
            float k = Mathf.Clamp01(t / 0.09f);
            return 0.2f * Noise(t, 2200f + 1200f * k) * Mathf.Sin(k * Mathf.PI);
        }));

        /// <summary>Cerrar una luz: un siseo más corto y más grave que baja.</summary>
        public static AudioClip FlipDown() => Get("flipdown", () => Make("flipdown", 0.1f, t =>
        {
            float k = Mathf.Clamp01(t / 0.07f);
            return 0.16f * Noise(t, 1600f * Mathf.Pow(900f / 1600f, k)) * Mathf.Sin(k * Mathf.PI);
        }));

        /// <summary>No son iguales: un tono grave y corto que baja (sin chicharra ni nada que regañe).</summary>
        public static AudioClip Soft() => Get("soft", () => Make("soft", 0.32f, t =>
        {
            float hz = 330f * Mathf.Pow(247f / 330f, Mathf.Clamp01(t / 0.22f));
            return 0.18f * Mathf.Sin(2f * Mathf.PI * hz * t) * Mathf.Exp(-t / 0.1f);
        }));

        /// <summary>Al empezar el cielo: un zumbido de fondo (sol y re graves) que sube y se apaga.</summary>
        public static AudioClip Hum() => Get("hum", () => Make("hum", 1.7f, t =>
        {
            float env = Mathf.Clamp01(t / 0.4f) * Mathf.Exp(-Mathf.Max(0f, t - 0.4f) / 0.5f);
            return 0.1f * env * (Mathf.Sin(2f * Mathf.PI * 196f * t) + Mathf.Sin(2f * Mathf.PI * 293.66f * Mathf.Max(0f, t - 0.12f)));
        }));

        /// <summary>Encontrar un grupo de memoria: un acorde de quinta (la nota del grupo y su quinta).</summary>
        public static AudioClip FoundMemory(int group) => Get("fm" + group, () => Make("fm", 1.6f, t =>
        {
            float hz = ConstelacionContract.NoteHz(group);
            return 0.2f * (Bell(hz, t, 0.4f) + 0.7f * Bell(hz * 1.5f, t - 0.08f, 0.4f));
        }));

        /// <summary>El destello de la racha: una campanita aguda, un poco después del acorde, que sube de tono con cada acierto de memoria seguido.</summary>
        public static AudioClip Spark(int streak) => Get("spark" + Mathf.Clamp(streak, 1, 8), () => Make("spark", 1.2f, t => 0.12f * Bell(Penta[Mathf.Min(9, 2 + Mathf.Clamp(streak, 1, 8))] * 2f, t - 0.18f, 0.28f)));

        /// <summary>Encontrar un grupo a la primera vista: un acorde de tercera, más callado (fue suerte, no memoria).</summary>
        public static AudioClip FoundNew(int group) => Get("fn" + group, () => Make("fn", 1.4f, t =>
        {
            float hz = ConstelacionContract.NoteHz(group);
            return 0.17f * (Bell(hz, t, 0.35f) + 0.7f * Bell(hz * 1.25f, t - 0.08f, 0.32f));
        }));

        /// <summary>«NUEVO»: dos campanas (mi y la).</summary>
        public static AudioClip Chime() => Get("chime", () => Make("chime", 1.2f, t => 0.16f * (Bell(659.25f, t, 0.2f) + Bell(880f, t - 0.09f, 0.26f))));

        /// <summary>Un cielo completo: un arpegio de cuatro campanas.</summary>
        public static AudioClip BoardEnd() => Get("boardend", () => Make("boardend", 1.8f, t =>
        {
            float s = 0f;
            for (int k = 0; k < 4; k++) s += 0.2f * Bell(Penta[Mathf.Min(9, 3 + k * 2)], t - k * 0.1f, 0.3f);
            return s;
        }, echo: true));

        /// <summary>Fin de la partida: tres campanas que suben, lentas.</summary>
        public static AudioClip Finale() => Get("finale", () => Make("finale", 2.0f, t =>
            0.18f * (Bell(784f, t, 0.3f) + Bell(1046.5f, t - 0.15f, 0.34f) + Bell(1318.5f, t - 0.3f, 0.4f)), echo: true));

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
            FlipUp(); FlipDown(); Soft(); Hum(); Chime();
            yield return null;
            for (int g = 0; g < 11; g++) Note(g);
            yield return null;
            for (int g = 0; g < 11; g++) { FoundNew(g); FoundMemory(g); }
            yield return null;
            for (int s = 1; s <= 8; s++) Spark(s);
            BoardEnd(); Finale();
        }

        public static int CachedCount => Cache.Count;
    }
}
