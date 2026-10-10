using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using NeuroVida.Games.Shared;
using static NeuroVida.Games.Shared.SoundKit;

namespace NeuroVida.Games.Radar
{
    /// <summary>Los sonidos de «Rescate relámpago».</summary>
    public enum RadarSfx { Ping, Flash, Static, Mark, Unmark, Beam, Dock, Perfect, Miss, Rain, Finale, Trip }

    /// <summary>
    /// Sonido «MADERA CÁLIDA» de «Rescate relámpago» (v4, 9-oct; docs/diseno-rescate.md §16): kalimba, marimba y bloque de madera con una sala pequeña, como un juguete fino. Es un PORT 1:1 del estilo <c>STYLES.madera</c> del laboratorio de sonido que Ricardo eligió
    /// (docs/previews/sonido-laboratorio.html): las mismas recetas, semillas, niveles y tiempos; las piezas de síntesis comunes (generador, filtro, reverberación) están en <see cref="SoundKit"/>. Cada sonido es un clip ESTÉREO a 44 100 Hz calculado una sola vez, de a poco,
    /// durante la cuenta regresiva (<see cref="Prewarm"/>: la reverberación se calcula por pedazos, sin trabar el arranque). Son 18 clips (≈ 13 MB de muestras, medidos en la prueba <c>TheClipsTakeLessThan20Mb</c>). Nada suena fuerte: el pico de cada clip es el «nivel» del laboratorio (0,16 a 0,40).
    /// </summary>
    public static class RadarSounds
    {
        /// <summary>La pentatónica de do mayor desde do5, ampliada (la del laboratorio).</summary>
        public static readonly double[] P = { 523.25, 587.33, 659.25, 783.99, 880, 1046.5, 1174.66, 1318.51, 1567.98, 1760, 2093, 2349.32, 2637.02 };

        /// <summary>La sala de «Madera cálida»: pequeña, amortiguada y apenas abierta.</summary>
        public static readonly ReverbSettings Room = new ReverbSettings(0.7, 0.5, 0.18, 0.9, 0.8);

        /// <summary>Muestras de reverberación que se calculan por cuadro al precargar.</summary>
        private const int SamplesPerFrame = 24000;

        private static readonly double[] BeamNotes = { 261.63, 293.66, 329.63, 392, 440, 523.25, 587.33, 659.25 };

        private static readonly Dictionary<string, AudioClip> Cache = new Dictionary<string, AudioClip>();
        private static readonly Dictionary<double, float[]> MarimbaCache = new Dictionary<double, float[]>();

        // ------------------------------------------------------------------ instrumentos (recetas del laboratorio)

        /// <summary>Kalimba: seno con un leve «golpe» de afinación al principio, un armónico metálico (×5,95), la octava y un clic de ruido de 4 ms.</summary>
        public static float[] Kalimba(double f, double tau = 0.45, uint seed = 7)
        {
            var r = new Rng(seed);
            return Render(tau * 3, t =>
            {
                double bend = 1 + 0.012 * Math.Exp(-t / 0.012);
                return Math.Sin(Tau * f * bend * t) * Env(t, 0.002, tau) + 0.28 * Math.Sin(Tau * f * 5.95 * t) * Env(t, 0.001, 0.035)
                     + 0.12 * Math.Sin(Tau * f * 2 * t) * Env(t, 0.002, tau * 0.4) + (t < 0.004 ? (r.Next() * 2 - 1) * 0.3 * (1 - t / 0.004) : 0.0);
            });
        }

        /// <summary>Marimba: el fundamental más dos armónicos que se apagan rápido (×4 y ×9,8).</summary>
        public static float[] Marimba(double f, double tau = 0.3) =>
            Render(tau * 3.2, t => 0.9 * Math.Sin(Tau * f * t) * Env(t, 0.003, tau) + 0.25 * Math.Sin(Tau * f * 4 * t) * Env(t, 0.002, 0.03) + 0.06 * Math.Sin(Tau * f * 9.8 * t) * Env(t, 0.001, 0.012));

        /// <summary>Bloque de madera: un seno corto y su parcial inarmónico (×2,71).</summary>
        public static float[] Woodblock(double f) =>
            Render(0.25, t => Math.Sin(Tau * f * t) * Env(t, 0.001, 0.035) + 0.5 * Math.Sin(Tau * f * 2.71 * t) * Env(t, 0.001, 0.02));

        /// <summary>Ruido filtrado con un barrido de frecuencia exponencial de <paramref name="f0"/> a <paramref name="f1"/>.</summary>
        public static float[] NoiseSweep(double sec, double f0, double f1, FilterType type = FilterType.BandPass, double q = 0.8, double a = 0.002, double tau = 0.08, uint seed = 1)
        {
            var r = new Rng(seed);
            var flt = new Biquad(type, f0, q);
            return Render(sec, t =>
            {
                flt.Set(f0 * Math.Pow(f1 / f0, Math.Min(1.0, t / sec)));
                return flt.Run(r.Next() * 2 - 1) * Env(t, a, tau);
            });
        }

        /// <summary>Estática granulada: ruido de banda con granos al azar (como un sonajero suave).</summary>
        public static float[] StaticNoise(double sec, double center = 1800, double q = 0.7, uint seed = 3, double grain = 0.003, double flutter = 0)
        {
            var r = new Rng(seed);
            var bp = new Biquad(FilterType.BandPass, center, q);
            double g = 0, next = 0;
            return Render(sec, t =>
            {
                if (t >= next)
                {
                    g = r.Next() < 0.35 ? r.Next() : r.Next() * 0.25;
                    next = t + grain * (0.5 + r.Next());
                }
                double fl = flutter != 0 ? 0.6 + 0.4 * Math.Sin(Tau * flutter * t) : 1;
                return bp.Run(r.Next() * 2 - 1) * g * fl * Math.Min(1.0, t / 0.01) * Math.Min(1.0, (sec - t) / 0.08);
            });
        }

        /// <summary>Golpe grave: un seno que baja de <paramref name="f0"/> a <paramref name="f1"/> Hz.</summary>
        public static float[] Thump(double sec, double f0 = 90, double f1 = 50, double tau = 0.09)
        {
            double ph = 0;
            return Render(sec, t =>
            {
                ph += f0 * Math.Pow(f1 / f0, Math.Min(1.0, t / sec)) / Rate;
                return Math.Sin(Tau * ph) * Env(t, 0.002, tau);
            });
        }

        // ------------------------------------------------------------------ los sonidos del juego (antes de la sala)

        /// <summary>Una marimba por nota y por duración, calculada una sola vez (el rayo, el viaje y el final repiten las mismas).</summary>
        private static float[] MarimbaOnce(double f, double tau)
        {
            double key = f * 1000.0 + tau;
            if (!MarimbaCache.TryGetValue(key, out var m)) MarimbaCache[key] = m = Marimba(f, tau);
            return m;
        }

        /// <summary>La receta de un sonido: las muestras mono antes de la sala, cuánta sala lleva (mezcla) y el nivel del pico final.</summary>
        public readonly struct Recipe
        {
            public readonly float[] X;
            public readonly ReverbSettings Verb;
            public readonly double Level;

            public Recipe(float[] x, double level, double? wet = null)
            {
                X = x;
                Level = level;
                Verb = new ReverbSettings(Room.Room, Room.Damp, wet ?? Room.Wet, Room.Tail, Room.Width);
            }
        }

        public static Recipe MakeRecipe(RadarSfx sfx, int arg = 0)
        {
            switch (sfx)
            {
                case RadarSfx.Ping:                                                                  // una vuelta del haz: un bloque de madera
                    return new Recipe(Woodblock(1250), 0.16, 0.25);
                case RadarSfx.Flash:                                                                 // el relámpago: ruido que se cierra y un golpe grave
                {
                    var x = NoiseSweep(0.4, 3500, 300, FilterType.LowPass, 0.7, 0.002, 0.09, 12);
                    Add(x, Thump(0.35, 140, 60, 0.13), 0, 0.9);
                    return new Recipe(x, 0.4);
                }
                case RadarSfx.Static:                                                                // la estática: un sonajero suave
                    return new Recipe(StaticNoise(0.3, 5000, 0.6, 6, 0.002), 0.18, 0.08);
                case RadarSfx.Mark:                                                                  // marcar la 1.ª, 2.ª, 3.ª, 4.ª: kalimba do5, re5, mi5, sol5
                    return new Recipe(Kalimba(P[Math.Max(0, Math.Min(4, arg - 1))]), 0.34);
                case RadarSfx.Unmark:                                                                // desmarcar: una kalimba apagada
                    return new Recipe(Kalimba(392, 0.07), 0.22);
                case RadarSfx.Beam:                                                                  // el rayo tractor: una marimba que sube
                    return new Recipe(Seq(BeamNotes, 0.07, (f, i) => MarimbaOnce(f, 0.18), 1.2), 0.18);
                case RadarSfx.Dock:                                                                  // entra una cápsula en la nave
                {
                    var x = Add(Marimba(P[2 + arg % 4]), Woodblock(1800), 0, 0.25);
                    return new Recipe(x, 0.3);
                }
                case RadarSfx.Perfect:                                                               // ronda perfecta
                {
                    var x = Seq(new[] { P[0], P[2], P[3], P[5] }, 0.08, (f, i) => Marimba(f, 0.35), 1.6);
                    Add(x, Kalimba(P[5]), 0.34, 0.6);
                    Add(x, Kalimba(P[7]), 0.42, 0.6);
                    return new Recipe(x, 0.34);
                }
                case RadarSfx.Miss:                                                                  // ronda con un error: dos notas graves que bajan
                    return new Recipe(Seq(new[] { 196.0, 164.81 }, 0.13, (f, i) => Marimba(f, 0.25)), 0.24);
                case RadarSfx.Rain:                                                                  // lluvia de cápsulas: ocho notas que bajan
                {
                    var notes = new List<double>();
                    foreach (int k in new[] { 9, 8, 7, 6, 5, 4, 3, 2 }) notes.Add(P[k]);
                    return new Recipe(Seq(notes, 0.06, (f, i) => Kalimba(f, 0.3), 1.6), 0.3);
                }
                case RadarSfx.Finale:                                                                // el final: un acorde de marimba y una kalimba larga
                {
                    var x = Blank(3);
                    double[] chord = { 261.63, 329.63, 392, 523.25, 659.25, 783.99, 1046.5 };
                    for (int i = 0; i < chord.Length; i++)
                    {
                        var m = MarimbaOnce(chord[i], 0.4);
                        for (int h = 0; h < 3; h++) Add(x, m, i * 0.07 + h * 0.05, h != 0 ? 0.35 : 0.8);
                    }
                    Add(x, Kalimba(P[7], 0.6), 0.55, 0.7);
                    return new Recipe(x, 0.4);
                }
                default:                                                                             // el viaje a la estación: el rayo más rápido y una kalimba do6
                {
                    var x = Seq(BeamNotes, 0.05, (f, i) => MarimbaOnce(f, 0.18), 1.2);
                    Add(x, Kalimba(P[5]), 0.45, 0.6);
                    return new Recipe(x, 0.3);
                }
            }
        }

        /// <summary>Las muestras estéreo ya con la sala y el cierre (para las pruebas y para armar el clip).</summary>
        public static void RenderStereo(RadarSfx sfx, int arg, out float[] left, out float[] right)
        {
            var recipe = MakeRecipe(sfx, arg);
            Reverb(recipe.X, recipe.Verb, out left, out right);
            Finish(left, right, recipe.Level);
        }

        // ------------------------------------------------------------------ clips

        private static string KeyOf(RadarSfx sfx, int arg) => sfx == RadarSfx.Mark ? "mark" + Math.Max(1, Math.Min(4, arg)) : sfx == RadarSfx.Dock ? "dock" + (Math.Abs(arg) % 4) : sfx.ToString();

        /// <summary>El clip de un sonido (se calcula la primera vez si todavía no se precargó; en el juego se precarga con <see cref="Prewarm"/>).</summary>
        public static AudioClip Get(RadarSfx sfx, int arg = 0)
        {
            string key = KeyOf(sfx, arg);
            if (Cache.TryGetValue(key, out var clip) && clip != null) return clip;
            RenderStereo(sfx, sfx == RadarSfx.Dock ? Math.Abs(arg) % 4 : arg, out var l, out var r);
            return Cache[key] = ToClip(key, l, r);
        }

        /// <summary>Lo que se precarga, en este orden (el lugar de cada marca y de cada entrada a la nave son distintos).</summary>
        public static readonly (RadarSfx Sfx, int Arg)[] BakeOrder =
        {
            (RadarSfx.Ping, 0), (RadarSfx.Flash, 0), (RadarSfx.Static, 0), (RadarSfx.Mark, 1), (RadarSfx.Mark, 2), (RadarSfx.Mark, 3), (RadarSfx.Beam, 0), (RadarSfx.Perfect, 0),
            (RadarSfx.Dock, 0), (RadarSfx.Dock, 1), (RadarSfx.Dock, 2), (RadarSfx.Unmark, 0), (RadarSfx.Miss, 0), (RadarSfx.Mark, 4), (RadarSfx.Dock, 3), (RadarSfx.Rain, 0), (RadarSfx.Finale, 0), (RadarSfx.Trip, 0),
        };

        /// <summary>Calcula todos los clips de a poco: la síntesis de cada uno en un cuadro y su reverberación en pedazos de <see cref="SamplesPerFrame"/> muestras por cuadro, para que el arranque no se trabe.</summary>
        public static IEnumerator Prewarm()
        {
            foreach (var (sfx, arg) in BakeOrder)
            {
                string key = KeyOf(sfx, arg);
                if (Cache.TryGetValue(key, out var have) && have != null) continue;
                var recipe = MakeRecipe(sfx, arg);
                yield return null;
                var job = new ReverbJob(recipe.X, recipe.Verb);
                while (!job.Done)
                {
                    job.Run(SamplesPerFrame);
                    yield return null;
                }
                job.Result(out var l, out var r);
                Finish(l, r, recipe.Level);
                Cache[key] = ToClip(key, l, r);
                yield return null;
            }
            MarimbaCache.Clear();
        }

        public static int CachedCount => Cache.Count;

        /// <summary>Suelta los clips precargados (SOLO para las pruebas: así miden la precarga desde cero sin depender de qué prueba corrió antes).</summary>
        public static void ClearCache()
        {
            foreach (var c in Cache.Values) if (c != null) UnityEngine.Object.DestroyImmediate(c);
            Cache.Clear();
            MarimbaCache.Clear();
        }

        /// <summary>Cuántas muestras (por canal) suman todos los clips precargados: para medir la memoria.</summary>
        public static long TotalSamplesPerChannel()
        {
            long n = 0;
            foreach (var c in Cache.Values) if (c != null) n += c.samples;
            return n;
        }
    }
}
