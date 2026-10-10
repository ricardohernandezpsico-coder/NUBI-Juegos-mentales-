using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using NeuroVida.Games.Radar;
using NeuroVida.Games.Shared;
using static NeuroVida.Games.Shared.SoundKit;

namespace NeuroVida.Games.Acoplamiento
{
    /// <summary>Los sonidos de «Acoplamiento»: cada uno es lo que pasa en pantalla.</summary>
    public enum DockSfx { Arrive, Tap, Flip, Dock, Slot, Ring, Miss, Finale }

    /// <summary>
    /// Sonido «MADERA CÁLIDA» de «Acoplamiento» (v2, 10-oct; docs/diseno-acoplamiento.md §7), ATADO a lo que pasa en pantalla: un soplo suave cuando el módulo se acerca, un toque de madera apagado al tocar un botón, dos notas que bajan cuando se da vuelta, un golpe hueco con una marimba cuando
    /// encaja, una kalimba que SUBE con la racha cuando se suma a la estación, un arpegio de marimba al completar un anillo, dos notas graves y suaves cuando no era y un rodado de marimba al final. Los cortos son los más suaves (Ricardo notó que los «breves y un poco más fuertes» no se entendían).
    /// Es un PORT 1:1 de <c>SFX</c> de la versión 2 del boceto (docs/previews/acoplamiento-boceto.html): mismas recetas, semillas y niveles, con las piezas comunes de <see cref="SoundKit"/> y los instrumentos y la sala de <see cref="RadarSounds"/> (los mismos del laboratorio de sonido).
    /// Cada sonido es un clip ESTÉREO a 44.100 Hz calculado una sola vez, de a poco, durante la cuenta regresiva (<see cref="Prewarm"/>).
    /// </summary>
    public static class DockingSounds
    {
        /// <summary>Muestras de reverberación que se calculan por cuadro al precargar.</summary>
        private const int SamplesPerFrame = 24000;

        private static readonly Dictionary<string, AudioClip> Cache = new Dictionary<string, AudioClip>();

        /// <summary>La cantidad de notas que sube el sonido de «se suma a la estación» (la nota número 1 + min(racha, 8)).</summary>
        public const int MaxSlotStreak = 8;

        private static string KeyOf(DockSfx sfx, int arg) => sfx == DockSfx.Slot ? "slot" + Math.Max(0, Math.Min(MaxSlotStreak, arg)) : sfx.ToString();

        /// <summary>La receta de un sonido: las muestras mono antes de la sala, cuánta sala lleva y el nivel del pico final (el «nivel» de la tabla del diseño).</summary>
        public static RadarSounds.Recipe MakeRecipe(DockSfx sfx, int arg = 0)
        {
            var P = RadarSounds.P;
            switch (sfx)
            {
                case DockSfx.Arrive:                                                                // el módulo se acerca: un soplo suave de paso bajo (900 → 300 Hz)
                    return new RadarSounds.Recipe(RadarSounds.NoiseSweep(0.6, 900, 300, FilterType.LowPass, 0.7, 0.004, 0.2, 31), 0.06);
                case DockSfx.Tap:                                                                   // tocaste un botón: un toque de madera apagado
                    return new RadarSounds.Recipe(RadarSounds.Marimba(392, 0.06), 0.07);
                case DockSfx.Flip:                                                                  // se da vuelta: dos notas que bajan
                {
                    var x = Blank(0.9);
                    Add(x, RadarSounds.Kalimba(P[4], 0.18));
                    Add(x, RadarSounds.Kalimba(P[2], 0.22), 0.09);
                    return new RadarSounds.Recipe(x, 0.16);
                }
                case DockSfx.Dock:                                                                  // encaja: golpe hueco + marimba + un bloque de madera
                {
                    var x = RadarSounds.Thump(0.35, 130, 60, 0.1);
                    Add(x, RadarSounds.Marimba(P[0], 0.3), 0.01, 0.6);
                    Add(x, RadarSounds.Woodblock(700), 0, 0.2);
                    return new RadarSounds.Recipe(x, 0.3);
                }
                case DockSfx.Slot:                                                                  // se suma a la estación: una kalimba cuya nota sube con la racha
                    return new RadarSounds.Recipe(RadarSounds.Kalimba(P[1 + Math.Max(0, Math.Min(MaxSlotStreak, arg))], 0.4), 0.26);
                case DockSfx.Ring:                                                                  // anillo completo: do-mi-sol-do de marimba y una kalimba
                {
                    var x = Seq(new[] { P[0], P[2], P[3], P[5] }, 0.08, (f, i) => RadarSounds.Marimba(f, 0.35), 1.6);
                    Add(x, RadarSounds.Kalimba(P[7]), 0.4, 0.6);
                    return new RadarSounds.Recipe(x, 0.36);
                }
                case DockSfx.Miss:                                                                  // no era: dos notas graves y suaves
                    return new RadarSounds.Recipe(Seq(new[] { 196.0, 164.81 }, 0.13, (f, i) => RadarSounds.Marimba(f, 0.25), 0.9), 0.22);
                default:                                                                            // el final: un rodado de marimba
                    return RadarSounds.MakeRecipe(RadarSfx.Finale);                                 // la misma receta del laboratorio (acorde de marimba y kalimba larga, nivel 0,40)
            }
        }

        /// <summary>Las muestras estéreo ya con la sala y el cierre (para las pruebas y para armar el clip).</summary>
        public static void RenderStereo(DockSfx sfx, int arg, out float[] left, out float[] right)
        {
            var recipe = MakeRecipe(sfx, arg);
            Reverb(recipe.X, recipe.Verb, out left, out right);
            Finish(left, right, recipe.Level);
        }

        /// <summary>El clip de un sonido (se calcula la primera vez si todavía no se precargó).</summary>
        public static AudioClip Get(DockSfx sfx, int arg = 0)
        {
            string key = KeyOf(sfx, arg);
            if (Cache.TryGetValue(key, out var clip) && clip != null) return clip;
            RenderStereo(sfx, sfx == DockSfx.Slot ? Math.Max(0, Math.Min(MaxSlotStreak, arg)) : 0, out var l, out var r);
            return Cache[key] = ToClip(key, l, r);
        }

        /// <summary>Lo que se precarga, en este orden (una nota de «se suma» por cada racha de 1 a 8; la de racha 0 solo se calcula si se pide).</summary>
        public static readonly (DockSfx Sfx, int Arg)[] BakeOrder =
        {
            (DockSfx.Arrive, 0), (DockSfx.Tap, 0), (DockSfx.Dock, 0), (DockSfx.Slot, 1), (DockSfx.Slot, 2), (DockSfx.Flip, 0), (DockSfx.Miss, 0), (DockSfx.Slot, 3), (DockSfx.Slot, 4),
            (DockSfx.Ring, 0), (DockSfx.Slot, 5), (DockSfx.Slot, 6), (DockSfx.Slot, 7), (DockSfx.Slot, 8), (DockSfx.Finale, 0),
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
        }

        public static int CachedCount => Cache.Count;

        /// <summary>Cuántas muestras (por canal) suman todos los clips precargados: para medir la memoria.</summary>
        public static long TotalSamplesPerChannel()
        {
            long n = 0;
            foreach (var c in Cache.Values) if (c != null) n += c.samples;
            return n;
        }

        /// <summary>Suelta los clips precargados (SOLO para las pruebas: así miden la precarga desde cero sin depender de qué prueba corrió antes).</summary>
        public static void ClearCache()
        {
            foreach (var c in Cache.Values) if (c != null) UnityEngine.Object.DestroyImmediate(c);
            Cache.Clear();
        }
    }
}
