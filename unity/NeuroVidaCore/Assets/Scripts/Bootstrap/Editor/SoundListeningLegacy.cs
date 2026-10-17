using System;
using UnityEngine;

namespace NeuroVida.Bridge.EditorTools
{
    /// <summary>
    /// Los sonidos comunes de ANTES de la Fase B (Tarea 120): el «pling» de marimba que sube con la racha, el «bu-bum» del error, el arpegio de nivel, el tic y la campana final que <c>GameFeel</c> sintetizaba hasta el 17-oct. Desde la Fase B <c>GameFeel</c> suena la paleta «Madera cálida» y estas recetas ya no existen en el juego;
    /// se guardan AQUÍ, tal cual, para que la página de escucha (<see cref="SoundListeningExporter"/>) pueda seguir mostrando «cómo sonaba hoy» al lado de Madera. Solo corre en el Editor.
    /// </summary>
    internal static class GameFeelLegacy
    {
        private const int Rate = 44100;

        /// <summary>Pentatónica mayor desde do5: la de antes.</summary>
        public static readonly float[] Pentatonic = { 523.25f, 587.33f, 659.25f, 783.99f, 880.00f, 1046.50f, 1174.66f, 1318.51f };

        private static AudioClip Make(string name, float seconds, Func<float, float> sample)
        {
            int n = Mathf.CeilToInt(seconds * Rate);
            var data = new float[n];
            for (int i = 0; i < n; i++) data[i] = Mathf.Clamp(sample(i / (float)Rate), -1f, 1f);
            int fade = Mathf.Min(n, Rate / 250);
            for (int i = 0; i < fade; i++) data[n - 1 - i] *= i / (float)fade;
            var clip = AudioClip.Create(name, n, 1, Rate, false);
            clip.SetData(data, 0);
            return clip;
        }

        private static float Sin(float hz, float t) => Mathf.Sin(2f * Mathf.PI * hz * t);

        private static float Env(float t, float attack, float tau) =>
            (t < attack ? t / attack : 1f) * Mathf.Exp(-Mathf.Max(0f, t - attack) / tau);

        /// <summary>El «pling» de la racha: marimba (fundamental + parcial ×4) por la pentatónica; la racha 1 es do5.</summary>
        public static AudioClip Marimba(float hz, float seconds) => Make("marimba", seconds, t =>
            0.8f * Sin(hz, t) * Env(t, 0.004f, 0.11f) + 0.22f * Sin(hz * 4f, t) * Env(t, 0.002f, 0.025f));

        public static AudioClip Wrong() => Make("wrong", 0.34f, t =>
        {
            float hz = t < 0.11f ? 220f : 185f;
            float local = t < 0.11f ? t : t - 0.11f;
            float env = Env(local, 0.006f, 0.09f) * (t < 0.11f ? 0.75f : 1f);
            return 0.7f * env * (Sin(hz, t) + 0.18f * Sin(hz * 3f, t));
        });

        public static AudioClip LevelUp() => Make("arp", 0.5f, t =>
        {
            float[] notes = { 523.25f, 659.25f, 783.99f, 1046.50f };
            float s = 0f;
            for (int i = 0; i < notes.Length; i++)
            {
                float start = i * 0.065f;
                if (t < start) break;
                float lt = t - start;
                s += 0.42f * Sin(notes[i], t) * Env(lt, 0.004f, 0.14f) + 0.1f * Sin(notes[i] * 4f, t) * Env(lt, 0.002f, 0.03f);
            }
            return s;
        });

        public static AudioClip Finish() => Make("finish", 1.1f, t =>
        {
            float s = 0f;
            float[] chord = { 523.25f, 659.25f, 783.99f, 1046.50f };
            for (int i = 0; i < chord.Length; i++)
                s += 0.26f * Sin(chord[i], t) * Env(t, 0.006f, 0.42f) + 0.06f * Sin(chord[i] * 2.76f, t) * Env(t, 0.003f, 0.12f);
            if (t > 0.12f) s += 0.12f * Sin(2093f, t) * Env(t - 0.12f, 0.004f, 0.2f);
            return s;
        });

        public static AudioClip Tick() => Make("tick", 0.06f, t =>
            0.7f * (Sin(1250f, t) + 0.4f * Sin(2500f, t)) * Env(t, 0.001f, 0.012f));
    }
}
