using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using NeuroVida.Games.Radar;
using NeuroVida.Games.Shared;
using static NeuroVida.Games.Shared.SoundKit;

namespace NeuroVida.Games.Aterrizaje
{
    /// <summary>Los sonidos de «Aterrizaje Lunar»: cada uno es lo que pasa en pantalla.</summary>
    public enum LandSfx { Release, Touch, Flag, Hit, Bull, Miss, Dome, Finale }

    /// <summary>
    /// Sonido «MADERA CÁLIDA» de «Aterrizaje Lunar» (10-oct; docs/diseno-aterrizaje.md §5), ATADO a lo que pasa en pantalla: un bucle suave de ruido grave mientras la nave baja (el propulsor), un soplo que baja al soltar, un golpe suave con un bloque de madera al posarse, una kalimba cuando sube la bandera,
    /// dos notas de marimba en un aterrizaje justo, do-mi-sol-do en una diana, dos notas graves y suaves si quedó lejos, un acorde cuando se arma una cúpula y un rodado de marimba al final. Es un PORT 1:1 de <c>SFX</c> y <c>hoverOn</c> del boceto aprobado (docs/previews/aterrizaje-boceto.html): mismas recetas, semillas y niveles,
    /// con las piezas comunes de <see cref="SoundKit"/> y los instrumentos y la sala de <see cref="RadarSounds"/>. Cada sonido es un clip ESTÉREO a 44.100 Hz calculado una sola vez, de a poco, durante la cuenta regresiva (<see cref="Prewarm"/>); el propulsor es un bucle MONO de 4 s.
    /// </summary>
    public static class LandingSounds
    {
        private const int SamplesPerFrame = 24000;

        private static readonly Dictionary<string, AudioClip> Cache = new Dictionary<string, AudioClip>();
        private static AudioClip _hover;

        /// <summary>La receta de un sonido: las muestras mono antes de la sala, cuánta sala lleva y el nivel del pico final (el «nivel» de la tabla del diseño).</summary>
        public static RadarSounds.Recipe MakeRecipe(LandSfx sfx)
        {
            var P = RadarSounds.P;
            switch (sfx)
            {
                case LandSfx.Release:                                                               // soltar: un soplo de paso bajo que baja (1400 → 300 Hz)
                    return new RadarSounds.Recipe(RadarSounds.NoiseSweep(0.5, 1400, 300, FilterType.LowPass, 0.7, 0.004, 0.14, 41), 0.08);
                case LandSfx.Touch:                                                                 // posarse: golpe grave suave y un bloque de madera
                {
                    var x = RadarSounds.Thump(0.3, 110, 55, 0.09);
                    Add(x, RadarSounds.Woodblock(520), 0, 0.25);
                    return new RadarSounds.Recipe(x, 0.26);
                }
                case LandSfx.Flag:                                                                  // sube la bandera: una kalimba
                    return new RadarSounds.Recipe(RadarSounds.Kalimba(P[3], 0.35), 0.2);
                case LandSfx.Hit:                                                                   // aterrizaje justo: marimba de 659 y 880 Hz
                {
                    var x = Blank(1.2);
                    Add(x, RadarSounds.Marimba(P[2], 0.3));
                    Add(x, RadarSounds.Marimba(P[4], 0.35), 0.1);
                    return new RadarSounds.Recipe(x, 0.26);
                }
                case LandSfx.Bull:                                                                  // diana: do-mi-sol-do de marimba y una kalimba
                {
                    var x = Seq(new[] { P[0], P[2], P[3], P[5] }, 0.07, (f, i) => RadarSounds.Marimba(f, 0.35), 1.6);
                    Add(x, RadarSounds.Kalimba(P[7]), 0.32, 0.6);
                    return new RadarSounds.Recipe(x, 0.34);
                }
                case LandSfx.Miss:                                                                  // lejos: dos notas graves y suaves
                    return new RadarSounds.Recipe(Seq(new[] { 196.0, 164.81 }, 0.13, (f, i) => RadarSounds.Marimba(f, 0.25), 0.9), 0.2);
                case LandSfx.Dome:                                                                  // cúpula nueva: un acorde de marimba y una kalimba
                {
                    var x = Blank(2.4);
                    double[] chord = { 261.63, 329.63, 392, 523.25 };
                    for (int i = 0; i < chord.Length; i++) Add(x, RadarSounds.Marimba(chord[i], 0.5), i * 0.06, 0.8);
                    Add(x, RadarSounds.Kalimba(P[5], 0.6), 0.3, 0.6);
                    return new RadarSounds.Recipe(x, 0.36);
                }
                default:                                                                            // el final: un rodado de marimba
                    return RadarSounds.MakeRecipe(RadarSfx.Finale);                                 // la misma receta del laboratorio (acorde de marimba y kalimba larga, nivel 0,40)
            }
        }

        /// <summary>Las muestras estéreo ya con la sala y el cierre (para las pruebas y para armar el clip).</summary>
        public static void RenderStereo(LandSfx sfx, out float[] left, out float[] right)
        {
            var recipe = MakeRecipe(sfx);
            Reverb(recipe.X, recipe.Verb, out left, out right);
            Finish(left, right, recipe.Level);
        }

        /// <summary>El clip de un sonido (se calcula la primera vez si todavía no se precargó).</summary>
        public static AudioClip Get(LandSfx sfx)
        {
            string key = sfx.ToString();
            if (Cache.TryGetValue(key, out var clip) && clip != null) return clip;
            RenderStereo(sfx, out var l, out var r);
            return Cache[key] = ToClip(key, l, r);
        }

        // ------------------------------------------------------------------ el propulsor mientras baja

        /// <summary>El volumen del propulsor cuando suena (el boceto sube la ganancia a 0,09 con una constante de 0,15 s y la baja a 0 con una de 0,08 s al soltar).</summary>
        public const float HoverVolume = 0.09f;

        /// <summary>
        /// El bucle del propulsor: ruido café (blanco integrado con un poco de fuga) por un paso bajo de 420 Hz (Q 0,7, semilla 43). Se calculan 6 s y los 2 últimos se funden sobre los 2 primeros → 4 s sin costura (mono, 176.400 muestras); el pico queda en 0,5.
        /// </summary>
        public static float[] MakeHoverLoop()
        {
            var lp = new Biquad(FilterType.LowPass, 420.0, 0.7);
            var r = new Rng(43);
            double b = 0.0;
            var x = Render(6.0, t =>
            {
                b = (b + 0.02 * (r.Next() * 2.0 - 1.0)) / 1.02;
                return lp.Run(b * 3.5);
            });
            int n = 4 * Rate, f = 2 * Rate;
            var output = new float[n];
            Array.Copy(x, output, n);
            for (int i = 0; i < f; i++)
            {
                double k = i / (double)f;
                output[i] = (float)(x[i] * k + x[i + n] * (1.0 - k));
            }
            double peak = 1e-9;
            for (int i = 0; i < n; i++) peak = Math.Max(peak, Math.Abs((double)output[i]));
            for (int i = 0; i < n; i++) output[i] = (float)(output[i] * (0.5 / peak));
            return output;
        }

        /// <summary>El clip del propulsor (se calcula la primera vez).</summary>
        public static AudioClip HoverLoop()
        {
            if (_hover != null) return _hover;
            var data = MakeHoverLoop();
            _hover = AudioClip.Create("hover", data.Length, 1, Rate, false);
            _hover.SetData(data, 0);
            return _hover;
        }

        // ------------------------------------------------------------------ precarga

        /// <summary>Lo que se precarga, en este orden.</summary>
        public static readonly LandSfx[] BakeOrder = { LandSfx.Release, LandSfx.Touch, LandSfx.Flag, LandSfx.Hit, LandSfx.Miss, LandSfx.Bull, LandSfx.Dome, LandSfx.Finale };

        /// <summary>Calcula todos los clips de a poco: la síntesis de cada uno en un cuadro y su reverberación en pedazos de <see cref="SamplesPerFrame"/> muestras por cuadro, para que el arranque no se trabe; el bucle del propulsor, en su propio cuadro.</summary>
        public static IEnumerator Prewarm()
        {
            HoverLoop();
            yield return null;
            foreach (var sfx in BakeOrder)
            {
                string key = sfx.ToString();
                if (Cache.TryGetValue(key, out var have) && have != null) continue;
                var recipe = MakeRecipe(sfx);
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
        }

        public static int CachedCount => Cache.Count;

        /// <summary>Cuántas muestras (por canal) suman todos los clips precargados: para medir la memoria.</summary>
        public static long TotalSamplesPerChannel()
        {
            long n = 0;
            foreach (var c in Cache.Values) if (c != null) n += c.samples;
            if (_hover != null) n += _hover.samples;
            return n;
        }

        /// <summary>Suelta los clips precargados (SOLO para las pruebas: así miden la precarga desde cero sin depender de qué prueba corrió antes).</summary>
        public static void ClearCache()
        {
            foreach (var c in Cache.Values) if (c != null) UnityEngine.Object.DestroyImmediate(c);
            Cache.Clear();
            if (_hover != null) UnityEngine.Object.DestroyImmediate(_hover);
            _hover = null;
        }
    }
}
