using System;
using System.Collections.Generic;
using UnityEngine;

namespace NeuroVida.Games.Correo
{
    /// <summary>
    /// Sonido PROPIO de «La estación de correo», sintetizado por código como el del boceto aprobado (docs/diseno-correo-estacion.md §6 y §11): cada buzón tiene su nota pentatónica con un brillo que sube con la racha; fanfarria de
    /// tres notas al cumplir un encargo; un tono grave y corto al equivocarse (nunca un castigo); el tic del reloj; dos tonos con ruido para la radio; la bocina grave de tres notas de la nave del correo; el sonido del saco;
    /// un zumbido al empezar el día y campanas al cerrarlo. Nada suena fuerte: todo ≤ 0,5.
    /// </summary>
    public static class MailSounds
    {
        private const int Rate = 44100;
        private static readonly Dictionary<string, AudioClip> Cache = new Dictionary<string, AudioClip>();

        /// <summary>La nota del buzón <paramref name="planet"/>: una campana suave.</summary>
        public static AudioClip BoxNote(int planet) => Get("box" + planet, () => Make("box", 0.7f, t => 0.3f * Bell(MailContract.PlanetNotes[Mathf.Clamp(planet, 0, 3)], t, 0.2f)));

        /// <summary>El brillo de la racha (desde ×3): una campanita aguda que sube por la escala con la racha.</summary>
        public static AudioClip ComboBell(int combo) => Get("combo" + (combo % 10), () => Make("combo", 0.6f, t => 0.12f * Bell(MailContract.Penta[Mathf.Clamp(combo % 10, 0, 9)] * 2f, t - 0.06f, 0.16f)));

        /// <summary>Una carta atrasada que cae, o un buzón equivocado: un golpe sordo y corto que baja (no es un castigo).</summary>
        public static AudioClip Thud() => Get("thud", () => Make("thud", 0.34f, t =>
        {
            float hz = 220f * Mathf.Pow(130f / 220f, Mathf.Clamp01(t / 0.25f));
            return 0.3f * Mathf.Sin(2f * Mathf.PI * hz * t) * Mathf.Exp(-t / 0.09f);
        }));

        /// <summary>«Esa carta no» / «aún no es la hora»: un tono suave que baja (392 → 330 Hz).</summary>
        public static AudioClip Soft() => Get("soft", () => Make("soft", 0.28f, t =>
        {
            float hz = 392f * Mathf.Pow(330f / 392f, Mathf.Clamp01(t / 0.2f));
            return 0.2f * Mathf.Sin(2f * Mathf.PI * hz * t) * Mathf.Exp(-t / 0.08f);
        }));

        /// <summary>El tic del reloj al mirarlo.</summary>
        public static AudioClip Tick() => Get("tick", () => Make("tick", 0.4f, t =>
        {
            float k = Mathf.Clamp01(t / 0.05f);
            float noise = t < 0.05f ? 0.18f * Noise(t, 3200f * Mathf.Pow(2400f / 3200f, k)) * Mathf.Sin(k * Mathf.PI) : 0f;
            return noise + 0.1f * Bell(1318.5f, t - 0.05f, 0.07f);
        }));

        /// <summary>Un encargo cumplido: fanfarria de tres notas (sol, si y re agudos).</summary>
        public static AudioClip Fanfare() => Get("fanfare", () => Make("fanfare", 1.6f, t =>
            0.22f * (Bell(783.99f, t, 0.35f) + 0.9f * Bell(987.77f, t - 0.08f, 0.35f) + 0.9f * Bell(1174.66f, t - 0.16f, 0.4f))));

        /// <summary>La radio: dos tonos y un siseo.</summary>
        public static AudioClip Radio() => Get("radio", () => Make("radio", 0.7f, t =>
        {
            float n = t < 0.35f ? 0.12f * Noise(t, 1500f * Mathf.Pow(900f / 1500f, t / 0.35f)) * Mathf.Sin(Mathf.PI * t / 0.35f) : 0f;
            return 0.2f * (Bell(880f, t, 0.07f) + Bell(1174.66f, t - 0.14f, 0.07f)) + n;
        }));

        /// <summary>La bocina grave de tres notas (146, 220 y 293 Hz) de la nave del correo.</summary>
        public static AudioClip Horn() => Get("horn", () => Make("horn", 2.1f, t =>
        {
            float s = 0f;
            float[] hz = { 146.83f, 220f, 293.66f };
            for (int i = 0; i < 3; i++)
            {
                float tt = t - i * 0.05f;
                if (tt < 0f) continue;
                float env = tt < 0.35f ? tt / 0.35f : Mathf.Exp(-(tt - 0.35f) / 0.5f);
                s += (0.3f - i * 0.06f) * env * Triangle(hz[i], tt);
            }
            return s;
        }));

        /// <summary>«¡Llega un saco!»: un siseo que sube, un golpe y dos campanitas.</summary>
        public static AudioClip Sack() => Get("sack", () => Make("sack", 0.9f, t =>
        {
            float k = Mathf.Clamp01(t / 0.45f);
            float n = t < 0.45f ? 0.14f * Noise(t, 400f * Mathf.Pow(1400f / 400f, k)) * Mathf.Sin(k * Mathf.PI) : 0f;
            float hz = 220f * Mathf.Pow(130f / 220f, Mathf.Clamp01(t / 0.25f));
            float thud = 0.2f * Mathf.Sin(2f * Mathf.PI * hz * t) * Mathf.Exp(-t / 0.09f);
            return n + thud + 0.12f * (Bell(659.25f, t - 0.15f, 0.12f) + Bell(880f, t - 0.25f, 0.12f));
        }));

        /// <summary>Al empezar el día: un zumbido (sol y re graves) que sube y se apaga.</summary>
        public static AudioClip Hum() => Get("hum", () => Make("hum", 1.6f, t =>
        {
            float env = Mathf.Clamp01(t / 0.3f) * Mathf.Exp(-Mathf.Max(0f, t - 0.3f) / 0.45f);
            return 0.1f * env * (Mathf.Sin(2f * Mathf.PI * 196f * t) + Mathf.Sin(2f * Mathf.PI * 293.66f * Mathf.Max(0f, t - 0.1f)));
        }));

        /// <summary>La hoja de la mañana: una campana (sol).</summary>
        public static AudioClip BriefBell() => Get("brief", () => Make("brief", 0.9f, t => 0.2f * Bell(784f, t, 0.3f)));

        /// <summary>«NUEVO»: dos campanas (mi y la).</summary>
        public static AudioClip Chime() => Get("chime", () => Make("chime", 1.2f, t => 0.16f * (Bell(659.25f, t, 0.2f) + Bell(880f, t - 0.09f, 0.26f))));

        /// <summary>El cierre del día: tres campanas que suben (mi, sol y si agudos).</summary>
        public static AudioClip DayEnd() => Get("dayend", () => Make("dayend", 1.6f, t =>
            0.2f * (Bell(MailContract.Penta[2], t, 0.35f) + Bell(MailContract.Penta[4], t - 0.11f, 0.35f) + Bell(MailContract.Penta[6], t - 0.22f, 0.4f)), echo: true));

        /// <summary>Fin de la partida: dos campanas lentas (sol y do agudos).</summary>
        public static AudioClip Finale() => Get("finale", () => Make("finale", 2.0f, t =>
            0.2f * (Bell(784f, t, 0.5f) + Bell(1046.5f, t - 0.15f, 0.55f)), echo: true));

        /// <summary>El destello del escalón de la racha (×10, ×20, ×30): dos campanas y, más arriba, una más.</summary>
        public static AudioClip Tier(int tier) => Get("tier" + Mathf.Clamp(tier, 1, 3), () => Make("tier", 1.4f, t =>
        {
            float s = Bell(659.25f, t, 0.18f) + Bell(880f, t - 0.09f, 0.22f);
            if (tier >= 2) s += Bell(1567.98f, t - 0.12f, 0.3f);
            if (tier >= 3) s += Bell(2093f, t - 0.2f, 0.35f);
            return 0.14f * s;
        }));

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
        private static float Triangle(float hz, float t) => 2f / Mathf.PI * (float)System.Math.Asin(Mathf.Sin(2f * Mathf.PI * hz * t));

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
            for (int i = 0; i < MailContract.PlanetCount; i++) BoxNote(i);
            Thud(); Soft(); Tick();
            yield return null;
            for (int c = 0; c < 10; c++) ComboBell(c);
            Fanfare(); Radio();
            yield return null;
            Horn(); Sack(); Hum();
            yield return null;
            BriefBell(); Chime(); DayEnd(); Finale();
            for (int tier = 1; tier <= 3; tier++) Tier(tier);
        }

        public static int CachedCount => Cache.Count;
    }
}
