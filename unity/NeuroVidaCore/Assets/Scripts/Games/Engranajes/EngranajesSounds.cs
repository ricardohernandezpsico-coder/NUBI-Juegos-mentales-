using System;
using System.Collections.Generic;
using UnityEngine;

namespace NeuroVida.Games.Engranajes
{
    /// <summary>
    /// Sonido PROPIO de «Engranajes», sintetizado por código como el del boceto aprobado: la identidad de la app (campanas en la pentatónica de do) para lo que celebra, y para la
    /// máquina sonidos de taller: el clic de los dientes y una nota que sube con cada paso de la cascada, el zumbido del motor al arrancar, y UN sonido por pieza del cohete
    /// (la antena silba y tintinea, la turbina ruge, la compuerta y el elevador zumban con su motor eléctrico y paran con un golpe). Al hacer un cambio suena un «clank» de llave; un
    /// cartel que no cumplió es un golpe grave y blando (sin chicharra). Despegue: un rugido que sube y cinco campanas. Nada suena fuerte: todo ≤ 0,5.
    /// </summary>
    public static class EngranajesSounds
    {
        private const int Rate = 44100;
        public static readonly float[] Penta = { 523.25f, 587.33f, 659.25f, 783.99f, 880f, 1046.5f, 1174.66f, 1318.5f, 1567.98f, 1760f };
        private static readonly Dictionary<string, AudioClip> Cache = new Dictionary<string, AudioClip>();

        /// <summary>El clic de los dientes de un paso de la cascada <paramref name="depth"/> (0 = el motor) y la nota de la escala que sube con él.</summary>
        public static AudioClip Step(int depth)
        {
            int k = Mathf.Clamp(depth + 1, 0, Penta.Length - 1);
            return Get("step" + k, () => Make("step", 0.6f, t =>
                0.17f * Clack(t) + 0.1f * Bell(Penta[k], t, 0.13f)));
        }

        /// <summary>Arranca el motor: un zumbido grave que sube un poco y se apaga (triángulo de 70 a 95 Hz).</summary>
        public static AudioClip MotorStart() => Get("motor", () => Make("motor", 0.7f, t =>
        {
            float hz = 70f + 25f * Mathf.Clamp01(t / 0.4f);
            float env = Mathf.Clamp01(t / 0.1f) * Mathf.Exp(-Mathf.Max(0f, t - 0.1f) / 0.22f);
            return 0.2f * env * Triangle(hz, t);
        }));

        /// <summary>La antena: un silbido de viento que sube y, a poco, una campana aguda.</summary>
        public static AudioClip Antenna() => Get("antena", () => Make("antena", 1.2f, t =>
            0.13f * Noise(t, 600f * Mathf.Pow(2600f / 600f, Mathf.Clamp01(t / 0.8f))) * Mathf.Sin(Mathf.Clamp01(t / 0.8f) * Mathf.PI) + 0.1f * Bell(1567.98f, t - 0.3f, 0.35f)));

        /// <summary>La turbina: un rugido que sube de grave a medio, con un retumbo debajo.</summary>
        public static AudioClip Turbine() => Get("turbina", () => Make("turbina", 1.3f, t =>
        {
            float k = Mathf.Clamp01(t / 1.1f);
            float env = Mathf.Sin(k * Mathf.PI) * 0.9f + 0.1f;
            return 0.2f * env * Noise(t, 180f * Mathf.Pow(900f / 180f, k)) + 0.12f * env * Mathf.Sin(2f * Mathf.PI * (55f + 55f * k) * t);
        }));

        /// <summary>La compuerta o el elevador: el zumbido de un motor eléctrico (diente de sierra suave) y, al parar, un golpe seco.</summary>
        public static AudioClip Rack() => Get("rack", () => Make("rack", 1.1f, t =>
        {
            float hz = 90f + 50f * Mathf.Clamp01(t / 0.7f);
            float env = t < 0.8f ? 0.12f * Mathf.Exp(-t / 0.8f) + 0.02f : 0f;
            float hum = env * (Saw(hz, t) * 2.2f);
            float knock = t >= 0.8f ? 0.2f * Noise(t, 500f * Mathf.Pow(200f / 500f, Mathf.Clamp01((t - 0.8f) / 0.12f))) * Mathf.Sin(Mathf.Clamp01((t - 0.8f) / 0.12f) * Mathf.PI) : 0f;
            return hum + knock;
        }));

        /// <summary>Una jugada que no era: un golpe grave y blando que baja (sin chicharra ni nada que regañe).</summary>
        public static AudioClip Thud() => Get("thud", () => Make("thud", 0.4f, t =>
        {
            float hz = 220f * Mathf.Pow(130f / 220f, Mathf.Clamp01(t / 0.25f));
            return 0.3f * Mathf.Sin(2f * Mathf.PI * hz * t) * Mathf.Exp(-t / 0.09f);
        }));

        /// <summary>El «clank» de un cambio (tocar el motor o una correa, o deshacerlo): un golpe metálico corto que baja y una campanita aguda (la del boceto: ruido 2400→900 Hz y un la sostenido).</summary>
        public static AudioClip Clank() => Get("clank", () => Make("clank", 0.45f, t =>
        {
            float k = Mathf.Clamp01(t / 0.09f);
            float hit = t < 0.09f ? 0.2f * Noise(t, 2400f * Mathf.Pow(900f / 2400f, k)) * Mathf.Sin(k * Mathf.PI) : 0f;
            return hit + 0.1f * Bell(1174.66f, t, 0.12f);
        }));

        /// <summary>Se toca «Arrancar»: una campanita suave (la nota 3 de la escala).</summary>
        public static AudioClip Press() => Get("press", () => Make("press", 0.4f, t => 0.2f * Bell(Penta[2], t, 0.08f)));

        /// <summary>«NUEVO»: dos campanas (mi y la).</summary>
        public static AudioClip Chime() => Get("chime", () => Make("chime", 1.2f, t => 0.16f * (Bell(659.25f, t, 0.2f) + Bell(880f, t - 0.09f, 0.26f))));

        /// <summary>Acierto: tres campanas que suben (do, mi, sol de la octava siguiente).</summary>
        public static AudioClip Success() => Get("success", () => Make("success", 2.2f, t =>
            0.2f * (Bell(1046.5f, t, 0.4f) + 0.8f * Bell(1318.5f, t - 0.08f, 0.45f) + 0.65f * Bell(1567.98f, t - 0.16f, 0.5f)), echo: true));

        /// <summary>La luz del cohete que se enciende: una campanita de la escala (la nota sube con cada luz).</summary>
        public static AudioClip Light(int k) => Get("light" + Mathf.Clamp(k, 0, 9), () => Make("light", 0.9f, t => 0.22f * Bell(Penta[Mathf.Clamp(k, 0, 9)], t, 0.22f)));

        /// <summary>El despegue: un rugido que sube durante 2,6 s y cinco campanas que ascienden.</summary>
        public static AudioClip Launch() => Get("launch", () => Make("launch", 3.8f, t =>
        {
            float k = Mathf.Clamp01(t / 2.6f);
            float roar = 0.22f * Mathf.Sin(k * Mathf.PI) * Noise(t, 90f * Mathf.Pow(420f / 90f, k)) + 0.1f * Mathf.Sin(k * Mathf.PI) * Mathf.Sin(2f * Mathf.PI * (50f + 70f * k) * t);
            float bells = 0f;
            for (int i = 0; i < 5; i++) bells += 0.16f * Bell(Penta[2 + (int)(i * 1.5f)], t - (1f + i * 0.14f), 0.5f);
            return roar + bells;
        }, echo: true));

        /// <summary>Fin de la partida: tres campanas que suben, lentas.</summary>
        public static AudioClip Finale() => Get("finale", () => Make("finale", 2.0f, t =>
            0.18f * (Bell(784f, t, 0.3f) + Bell(1046.5f, t - 0.15f, 0.34f) + Bell(1318.5f, t - 0.3f, 0.4f)), echo: true));

        // ------------------------------------------------------------------ síntesis

        private static float Clack(float t)
        {
            // diente contra diente: un chasquido de 50 ms con un tono agudo que baja
            if (t < 0f || t > 0.06f) return 0f;
            float env = Mathf.Sin(t / 0.06f * Mathf.PI);
            return env * Noise(t, 3200f - 1400f * (t / 0.06f));
        }

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

        /// <summary>Diente de sierra con pocos armónicos (como pasado por un filtro grave: sin chirrido).</summary>
        private static float Saw(float hz, float t)
        {
            float s = 0f;
            for (int h = 1; h <= 5; h++) s += Mathf.Sin(2f * Mathf.PI * hz * h * t) / h;
            return s * 0.55f;
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
            Press(); MotorStart(); Thud(); Clank();
            yield return null;
            for (int d = 0; d <= 9; d++) Step(d);
            yield return null;
            Antenna(); Turbine(); Rack();
            yield return null;
            Chime(); Success(); Finale();
            for (int k = 0; k < 10; k++) Light(k);
            yield return null;
            Launch();
        }

        public static int CachedCount => Cache.Count;
    }
}
