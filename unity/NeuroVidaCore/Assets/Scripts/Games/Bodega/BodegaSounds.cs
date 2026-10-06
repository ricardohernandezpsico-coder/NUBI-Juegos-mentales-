using System;
using System.Collections.Generic;
using UnityEngine;

namespace NeuroVida.Games.Bodega
{
    /// <summary>
    /// Sonido PROPIO de «Bodega de carga», sintetizado por código como el del boceto aprobado (docs/diseno-bodega-de-carga.md §5). Cada escotilla tiene su NOTA (pentatónica de la app, una octava arriba
    /// desde la 11): al guardar la bodega suena como una melodía y al abrir la correcta vuelve su nota, así se recuerda con la vista y con el oído. Puertas: abrir es un siseo agudo y cerrar un golpe grave
    /// suave. Robot: un zumbido al empezar, un «piu» ascendente con cada haz y un clank al tomar o dejar la caja. Movimientos: un soplido al volar con la caja y una matraca de engranes (un clic cada
    /// 110 ms) mientras la bodega gira. Respuestas: acierto = la nota de la escotilla más un destello que sube de tono con la racha; error = un golpe sordo (sin chicharra); pedido perfecto = arpegio.
    /// Nada suena fuerte: todo ≤ 0,5.
    /// </summary>
    public static class BodegaSounds
    {
        private const int Rate = 44100;
        public static readonly float[] Penta = { 523.25f, 587.33f, 659.25f, 783.99f, 880f, 1046.5f, 1174.66f, 1318.5f, 1567.98f, 1760f };
        private static readonly Dictionary<string, AudioClip> Cache = new Dictionary<string, AudioClip>();

        /// <summary>La frecuencia de la nota de una escotilla (0..9 sube por la escala; de la 10 en adelante repite una octava arriba).</summary>
        public static float NoteHz(int hatch) => Penta[BodegaContract.NoteIndex(hatch)] * (BodegaContract.NoteOctave(hatch) == 2 ? 2f : 1f);

        /// <summary>La nota propia de la escotilla: una campana (se oye al guardar y al abrir).</summary>
        public static AudioClip Note(int hatch) => Get("note" + hatch, () => Make("note", 1.3f, t => 0.3f * Bell(NoteHz(hatch), t, 0.42f)));

        /// <summary>Abrir una puerta: un siseo agudo que baja.</summary>
        public static AudioClip Open() => Get("open", () => Make("open", 0.2f, t =>
        {
            float k = Mathf.Clamp01(t / 0.16f);
            return 0.2f * Noise(t, 2600f * Mathf.Pow(900f / 2600f, k)) * Mathf.Sin(k * Mathf.PI);
        }));

        /// <summary>Cerrar una puerta: un golpe grave y suave.</summary>
        public static AudioClip Close() => Get("close", () => Make("close", 0.2f, t =>
        {
            float k = Mathf.Clamp01(t / 0.1f);
            return 0.24f * Noise(t, 700f * Mathf.Pow(250f / 700f, k)) * Mathf.Sin(k * Mathf.PI);
        }));

        /// <summary>Un error: un golpe sordo y grave que baja (sin chicharra ni nada que regañe).</summary>
        public static AudioClip Thud() => Get("thud", () => Make("thud", 0.4f, t =>
        {
            float hz = 220f * Mathf.Pow(130f / 220f, Mathf.Clamp01(t / 0.25f));
            return 0.3f * Mathf.Sin(2f * Mathf.PI * hz * t) * Mathf.Exp(-t / 0.09f);
        }));

        /// <summary>El zumbido del robot al empezar: grave, sube un poco y se apaga.</summary>
        public static AudioClip Hum() => Get("hum", () => Make("hum", 0.85f, t =>
        {
            float hz = 65f + 25f * Mathf.Clamp01(t / 0.5f);
            float env = Mathf.Clamp01(t / 0.1f) * Mathf.Exp(-Mathf.Max(0f, t - 0.1f) / 0.28f);
            return 0.2f * env * Triangle(hz, t);
        }));

        /// <summary>El «piu» del haz: un tono que sube de 420 a 880 Hz.</summary>
        public static AudioClip Beam() => Get("beam", () => Make("beam", 0.34f, t =>
        {
            float k = Mathf.Clamp01(t / 0.25f);
            float hz = 420f * Mathf.Pow(880f / 420f, k);
            return 0.2f * Mathf.Sin(2f * Mathf.PI * hz * t) * Mathf.Exp(-t / 0.12f);
        }));

        /// <summary>Tomar o dejar la caja: un golpe metálico corto y una campanita aguda.</summary>
        public static AudioClip Clank() => Get("clank", () => Make("clank", 0.4f, t =>
        {
            float k = Mathf.Clamp01(t / 0.09f);
            float hit = t < 0.09f ? 0.2f * Noise(t, 2400f * Mathf.Pow(900f / 2400f, k)) * Mathf.Sin(k * Mathf.PI) : 0f;
            return hit + 0.1f * Bell(1174.66f, t, 0.1f);
        }));

        /// <summary>El soplido del robot al volar con la caja por el anillo.</summary>
        public static AudioClip Whoosh() => Get("whoosh", () => Make("whoosh", 1.1f, t =>
        {
            float k = Mathf.Clamp01(t / 0.9f);
            return 0.16f * Noise(t, 300f * Mathf.Pow(1500f / 300f, k)) * Mathf.Sin(k * Mathf.PI);
        }));

        /// <summary>La llegada de la carga por la esclusa: un soplido que baja y una campanita aguda.</summary>
        public static AudioClip Arrive() => Get("arrive", () => Make("arrive", 0.6f, t =>
        {
            float k = Mathf.Clamp01(t / 0.5f);
            return 0.14f * Noise(t, 1800f * Mathf.Pow(500f / 1800f, k)) * Mathf.Sin(k * Mathf.PI) + 0.12f * Bell(1567.98f, t, 0.15f);
        }));

        /// <summary>La matraca de engranes de la bodega que gira: un clic cada 110 ms durante el giro (1,5 s) y un zumbido grave.</summary>
        public static AudioClip Ratchet() => Get("ratchet", () => Make("ratchet", BodegaMotion.SpinSeconds + 0.1f, t =>
        {
            float s = 0f;
            int n = Mathf.RoundToInt(BodegaMotion.SpinSeconds / 0.11f);
            for (int i = 0; i < n; i++)
            {
                float u = t - i * 0.11f;
                if (u < 0f || u > 0.04f) continue;
                s += 0.12f * Noise(u, 3000f - 1200f * (u / 0.04f)) * Mathf.Sin(u / 0.04f * Mathf.PI);
            }
            float env = Mathf.Clamp01(t / 0.1f) * Mathf.Exp(-Mathf.Max(0f, t - 0.1f) / 0.5f);
            return s + 0.12f * env * Triangle(70f + 20f * Mathf.Clamp01(t / 1.4f), t);
        }));

        /// <summary>La pregunta de un pedido: un ping corto.</summary>
        public static AudioClip Ping() => Get("ping", () => Make("ping", 0.6f, t => 0.12f * Bell(1318.5f, t, 0.12f)));

        /// <summary>El destello de la racha: una campanita aguda que sube de tono con cada objeto seguido al primer intento.</summary>
        public static AudioClip Spark(int streak) => Get("spark" + Mathf.Clamp(streak, 1, 8), () => Make("spark", 1.0f, t => 0.14f * Bell(Penta[Mathf.Min(9, 2 + Mathf.Clamp(streak, 1, 8))] * 2f, t, 0.2f)));

        /// <summary>Un pedido perfecto: un arpegio de cuatro campanas.</summary>
        public static AudioClip Perfect() => Get("perfect", () => Make("perfect", 1.8f, t =>
        {
            float s = 0f;
            for (int k = 0; k < 4; k++) s += 0.2f * Bell(Penta[Mathf.Min(9, 3 + k * 2)], t - k * 0.09f, 0.3f);
            return s;
        }, echo: true));

        /// <summary>«NUEVO»: dos campanas (mi y la).</summary>
        public static AudioClip Chime() => Get("chime", () => Make("chime", 1.2f, t => 0.16f * (Bell(659.25f, t, 0.2f) + Bell(880f, t - 0.09f, 0.26f))));

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

        private static float Triangle(float hz, float t)
        {
            float ph = t * hz - Mathf.Floor(t * hz);
            return 4f * Mathf.Abs(ph - 0.5f) - 1f;
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

        /// <summary>Para las pruebas y el calentamiento: sintetiza todos los clips (cada uno una sola vez).</summary>
        public static System.Collections.IEnumerator Prewarm()
        {
            Open(); Close(); Thud(); Hum(); Beam(); Clank();
            yield return null;
            Whoosh(); Arrive(); Ratchet(); Ping(); Chime();
            yield return null;
            for (int h = 0; h < 10; h++) Note(h);
            yield return null;
            for (int s = 1; s <= 8; s++) Spark(s);
            Perfect(); Finale();
        }

        public static int CachedCount => Cache.Count;
    }
}
