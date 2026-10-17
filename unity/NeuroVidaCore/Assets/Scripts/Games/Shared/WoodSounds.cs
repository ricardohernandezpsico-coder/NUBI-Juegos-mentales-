using System;
using System.Collections.Generic;
using UnityEngine;
using NeuroVida.Games.Radar;
using static NeuroVida.Games.Shared.SoundKit;

namespace NeuroVida.Games.Shared
{
    /// <summary>Los sonidos de la paleta «Madera cálida» de TODA la app (Tarea 119): cada uno es un EVENTO con el mismo sentido en cualquier juego.</summary>
    public enum WoodSfx
    {
        // micro: pasan todo el tiempo y casi no se notan
        Tap, Unselect, Tick, Count,
        // respuesta: lo que el juego te dice de lo que acabas de hacer
        Select, Correct, Wrong, Knock, Hint, New, Notice, Spark,
        // hito y cierre: lo que se celebra
        Streak, LevelUp, Perfect, Record, Finish, Go,
        // interfaz
        PauseIn, PauseOut,
        // texturas: soplos, golpes y zumbidos de los objetos del juego
        SweepUp, SweepDown, Hum, Clack,
    }

    /// <summary>La familia de un sonido de la paleta: manda el nivel (pico y sonoridad).</summary>
    public enum WoodTier { Micro, Respuesta, Hito, Cierre, Textura }

    /// <summary>El instrumento de una nota que lleva información (la altura NO cambia: solo el timbre).</summary>
    public enum WoodVoice { Kalimba, Marimba }

    /// <summary>
    /// PALETA SEMÁNTICA «MADERA CÁLIDA» (Tarea 119, Fase A, 17-oct): lo que Ricardo aprobó el 10-oct en Rescate relámpago (kalimba, marimba y bloque de madera con una sala pequeña y suave: <see cref="RadarSounds"/>, <see cref="SoundKit"/>) ordenado como un vocabulario para toda la app. Cada evento
    /// (tocar, elegir, acertar, errar, ayuda, nuevo, hito, fin…) tiene UN sonido, siempre con el mismo instrumento y la misma altura, y un nivel según su familia (<see cref="WoodTier"/>): lo que pasa todo el tiempo es lo más suave y lo que se celebra es lo más fuerte; así todo queda parejo.
    /// Reusa tal cual las recetas aprobadas (marcar, desmarcar, ronda perfecta, «no era», final) y arma con las mismas piezas lo que faltaba. TODAVÍA NO LO USA NINGÚN JUEGO: en esta fase solo lo renderiza el exportador de la página de escucha (Bootstrap/Editor/SoundListeningExporter.cs) y lo miden sus pruebas
    /// (WoodSoundsTests). La Fase B lo conecta a <c>GameFeel</c> y al kit; docs/sonido-madera-plan.md tiene el mapa evento por evento, los niveles medidos y el plan por fases.
    /// Cada sonido es un clip ESTÉREO a 44.100 Hz (la sala es estéreo); la cola de sala de cada uno es la justa para que no se corte (los más cortos llevan menos cola: menos memoria). Todo es determinista (mismas semillas): el mismo evento suena siempre igual.
    /// </summary>
    public static class WoodSounds
    {
        /// <summary>El volumen con el que se reproduce cada clip (el del laboratorio donde Ricardo eligió «Madera cálida»; el que ya usan Rescate, Acoplamiento y Aterrizaje).</summary>
        public const float PlayVolume = 0.8f;

        /// <summary>La pentatónica de do mayor desde do5 (la de todos los juegos): do5, re5, mi5, sol5, la5, do6, re6, mi6, sol6, la6, do7, re7, mi7.</summary>
        private static readonly double[] P = RadarSounds.P;

        /// <summary>La cola de sala que se calcula (como máximo): lo que queda por debajo de lo audible se recorta después (<see cref="Render"/>), así la sala nunca se corta de golpe y los sonidos suaves no gastan memoria en una cola que no se oye.</summary>
        public const double MaxTail = 1.5;

        /// <summary>La receta de un sonido: las muestras mono antes de la sala, el nivel del pico final y la mezcla de sala (la sala es la de Rescate).</summary>
        public readonly struct Recipe
        {
            public readonly float[] X;
            public readonly double Level;
            public readonly ReverbSettings Verb;

            public Recipe(float[] x, double level, double wet)
            {
                X = x;
                Level = level;
                Verb = new ReverbSettings(RadarSounds.Room.Room, RadarSounds.Room.Damp, wet, MaxTail, RadarSounds.Room.Width);
            }
        }

        /// <summary>La familia de un evento.</summary>
        public static WoodTier TierOf(WoodSfx sfx)
        {
            switch (sfx)
            {
                case WoodSfx.Tap: case WoodSfx.Unselect: case WoodSfx.Tick: case WoodSfx.Count: case WoodSfx.PauseIn: case WoodSfx.PauseOut: return WoodTier.Micro;
                case WoodSfx.Select: case WoodSfx.Correct: case WoodSfx.Wrong: case WoodSfx.Knock: case WoodSfx.Hint: case WoodSfx.New: case WoodSfx.Notice: case WoodSfx.Spark: return WoodTier.Respuesta;
                case WoodSfx.Streak: case WoodSfx.LevelUp: case WoodSfx.Perfect: case WoodSfx.Go: return WoodTier.Hito;
                case WoodSfx.Record: case WoodSfx.Finish: return WoodTier.Cierre;
                default: return WoodTier.Textura;
            }
        }

        /// <summary>Las variantes de un sonido: el argumento con el que se pide cada una (la racha, el número de marca, la cuenta). Los demás tienen una sola (0).</summary>
        public static int[] Variants(WoodSfx sfx)
        {
            switch (sfx)
            {
                case WoodSfx.Correct: case WoodSfx.Spark: return new[] { 1, 2, 3, 4, 5, 6, 7, 8 };
                case WoodSfx.Select: return new[] { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9 };
                case WoodSfx.Count: return new[] { 3, 2, 1 };
                default: return new[] { 0 };
            }
        }

        private static int Clamp(int v, int lo, int hi) => Math.Max(lo, Math.Min(hi, v));

        /// <summary>Un rodado de marimba (cada nota <paramref name="gap"/> s después de la anterior).</summary>
        private static float[] Marimbas(double[] notes, double gap, double tau, double total) => Seq(notes, gap, (f, i) => RadarSounds.Marimba(f, tau), total);

        /// <summary>La receta de un evento (<paramref name="arg"/> es la racha, la marca o la cuenta, según el evento).</summary>
        public static Recipe MakeRecipe(WoodSfx sfx, int arg = 0)
        {
            switch (sfx)
            {
                case WoodSfx.Tap:                                                                      // un toque o un botón presionado: un toque de madera apagado (el de Acoplamiento)
                    return new Recipe(RadarSounds.Marimba(392, 0.06), 0.07, 0.18);
                case WoodSfx.Select:                                                                   // elegir o marcar: una kalimba; la altura sube con la cantidad (do5, re5, mi5…): el de Rescate
                    return new Recipe(RadarSounds.Kalimba(P[Clamp(arg, 0, 9)]), 0.14, 0.18);
                case WoodSfx.Unselect:                                                                 // quitar o deshacer: una kalimba apagada
                    return new Recipe(RadarSounds.Kalimba(392, 0.07), 0.16, 0.18);
                case WoodSfx.Tick:                                                                     // el último tramo del reloj: un bloque de madera
                    return new Recipe(RadarSounds.Woodblock(1250), 0.22, 0.25);
                case WoodSfx.Count:                                                                    // la cuenta regresiva 3, 2, 1: una marimba que sube un escalón por número
                    return new Recipe(RadarSounds.Marimba(P[Clamp(3 - arg, 0, 2)], 0.12), 0.14, 0.18);

                case WoodSfx.Correct:                                                                  // acertar: una kalimba de DOS notas que suben; la pareja sube por la escala con la racha
                {
                    int s = Clamp(arg - 1, 0, 6);
                    var x = Blank(1.5);
                    Add(x, RadarSounds.Kalimba(P[s], 0.3), 0, 0.85);
                    Add(x, RadarSounds.Kalimba(P[s + 2], 0.42), 0.09, 1.0);
                    return new Recipe(x, 0.24, 0.18);
                }
                case WoodSfx.Wrong:                                                                    // errar: dos notas graves y suaves de marimba que bajan (el «no era» de Rescate, Acoplamiento y Aterrizaje)
                    return new Recipe(Seq(new[] { 196.0, 164.81 }, 0.13, (f, i) => RadarSounds.Marimba(f, 0.25), 0.9), 0.22, 0.18);
                case WoodSfx.Knock:                                                                    // «eso no se puede»: UN toc de madera grave y hueco
                {
                    var x = RadarSounds.Thump(0.25, 120, 70, 0.08);
                    Add(x, RadarSounds.Marimba(185, 0.12), 0, 0.7);
                    return new Recipe(x, 0.28, 0.12);
                }
                case WoodSfx.Hint:                                                                     // la ayuda de Nubi: un soplo que sube y una kalimba grave
                {
                    var x = Blank(1.4);
                    Add(x, RadarSounds.NoiseSweep(0.5, 400, 1600, FilterType.BandPass, 0.8, 0.02, 0.2, 51), 0, 0.5);
                    Add(x, RadarSounds.Kalimba(392, 0.5), 0.32, 0.8);
                    return new Recipe(x, 0.18, 0.18);
                }
                case WoodSfx.New:                                                                      // «NUEVO», sorpresa, algo que se desbloquea: dos kalimbas agudas que suben
                {
                    var x = Blank(1.6);
                    Add(x, RadarSounds.Kalimba(P[5], 0.4));
                    Add(x, RadarSounds.Kalimba(P[7], 0.5), 0.12);
                    return new Recipe(x, 0.26, 0.25);
                }
                case WoodSfx.Notice:                                                                   // un aviso en pantalla: una marimba sola
                    return new Recipe(RadarSounds.Marimba(P[5], 0.2), 0.16, 0.18);
                case WoodSfx.Spark:                                                                    // el destello de la racha: una kalimba aguda que sube con cada acierto seguido
                    return new Recipe(RadarSounds.Kalimba(P[Clamp(4 + arg, 5, 12)], 0.25), 0.14, 0.18);

                case WoodSfx.Streak:                                                                   // un hito de racha: un arpegio corto de kalimba sobre una marimba grave
                {
                    var x = Seq(new[] { P[2], P[4], P[5] }, 0.07, (f, i) => RadarSounds.Kalimba(f, 0.35), 1.6);
                    Add(x, RadarSounds.Marimba(P[0], 0.3), 0, 0.5);
                    return new Recipe(x, 0.30, 0.18);
                }
                case WoodSfx.LevelUp:                                                                  // subir de nivel: el arpegio do-mi-sol-do de kalimba (las mismas notas de siempre)
                    return new Recipe(Seq(new[] { P[0], P[2], P[3], P[5] }, 0.065, (f, i) => RadarSounds.Kalimba(f, 0.35), 1.5), 0.32, 0.18);
                case WoodSfx.Perfect:                                                                  // una ronda perfecta: la de Rescate (do-mi-sol-do de marimba y dos kalimbas)
                {
                    var x = Marimbas(new[] { P[0], P[2], P[3], P[5] }, 0.08, 0.35, 1.6);
                    Add(x, RadarSounds.Kalimba(P[5]), 0.34, 0.6);
                    Add(x, RadarSounds.Kalimba(P[7]), 0.42, 0.6);
                    return new Recipe(x, 0.34, 0.18);
                }
                case WoodSfx.Go:                                                                       // «¡Ya!»: dos marimbas y una kalimba aguda
                {
                    var x = Marimbas(new[] { P[0], P[3] }, 0.05, 0.25, 1.4);
                    Add(x, RadarSounds.Kalimba(P[5], 0.4), 0.1, 0.8);
                    return new Recipe(x, 0.30, 0.18);
                }

                case WoodSfx.Record:                                                                   // un récord: una escalera de kalimba hasta lo agudo y el acorde de marimba
                {
                    var x = Seq(new[] { P[0], P[2], P[3], P[5], P[7], P[9] }, 0.06, (f, i) => RadarSounds.Kalimba(f, 0.35), 2.2);
                    double[] chord = { 261.63, 392, 523.25 };
                    for (int i = 0; i < chord.Length; i++) Add(x, RadarSounds.Marimba(chord[i], 0.5), 0.42 + i * 0.05, 0.75);
                    return new Recipe(x, 0.40, 0.18);
                }
                case WoodSfx.Finish:                                                                   // el fin de la partida: el final de Rescate (acorde de marimba y una kalimba larga)
                {
                    var f = RadarSounds.MakeRecipe(RadarSfx.Finale);
                    return new Recipe(f.X, f.Level, f.Verb.Wet);
                }

                case WoodSfx.PauseIn:                                                                  // abrir la pausa: dos marimbas suaves que bajan
                    return new Recipe(Marimbas(new[] { P[2], P[0] }, 0.1, 0.15, 0.8), 0.12, 0.25);
                case WoodSfx.PauseOut:                                                                 // volver de la pausa: las mismas dos que suben
                    return new Recipe(Marimbas(new[] { P[0], P[2] }, 0.1, 0.15, 0.8), 0.12, 0.25);

                case WoodSfx.SweepUp:                                                                  // un soplo que sube (algo que aparece o se abre)
                    return new Recipe(RadarSounds.NoiseSweep(0.5, 500, 2200, FilterType.BandPass, 0.8, 0.01, 0.15, 61), 0.12, 0.1);
                case WoodSfx.SweepDown:                                                                // un soplo que baja (algo que se suelta, se cierra o vuelve): el de Aterrizaje
                    return new Recipe(RadarSounds.NoiseSweep(0.5, 1400, 300, FilterType.LowPass, 0.7, 0.004, 0.14, 41), 0.12, 0.18);
                case WoodSfx.Hum:                                                                      // un zumbido que arranca: dos marimbas graves
                {
                    var x = Blank(1.6);
                    Add(x, RadarSounds.Marimba(196, 0.5));
                    Add(x, RadarSounds.Marimba(293.66, 0.5), 0.1);
                    return new Recipe(x, 0.12, 0.18);
                }
                default:                                                                               // Clack: un golpe seco de un objeto (el de Aterrizaje al posarse)
                {
                    var x = RadarSounds.Thump(0.3, 110, 55, 0.09);
                    Add(x, RadarSounds.Woodblock(520), 0, 0.25);
                    return new Recipe(x, 0.26, 0.18);
                }
            }
        }

        /// <summary>
        /// La receta de UNA NOTA que lleva información (la altura del juego: la del lucero, del grupo, de la escotilla, del buzón…): el mismo Hz de siempre con el timbre de la paleta. Kalimba: el timbre de Rescate pero con una caída de 0,3 s (la de Rescate, de 0,45 s, sigue sonando más de un segundo y se encimaría con la nota siguiente de una secuencia: la
        /// caída de hoy de estas notas es de unos 0,2 a 0,4 s); marimba: más «afinada» (sus parciales son múltiplos). Las dos caen a −20 dB en cerca de 0,7 s.
        /// </summary>
        public static Recipe MakeNote(double hz, WoodVoice voice)
        {
            if (voice == WoodVoice.Kalimba) return new Recipe(RadarSounds.Kalimba(hz, 0.3), 0.30, 0.18);
            return new Recipe(RadarSounds.Marimba(hz, 0.3), 0.30, 0.18);
        }

        /// <summary>
        /// Una SEÑAL CORTA con altura (el tono de la regla de Tinta o Palabra, la señal de salida de Freno, el «blip» de Piloto…): la misma altura y un solo golpe seco de marimba que dura lo mismo que el tono de hoy (<paramref name="seconds"/>: 0,06 s la salida de Freno, 0,1 s las reglas de Tinta o Palabra), casi sin sala:
        /// empieza igual de rápido y no se le encima al estímulo siguiente.
        /// </summary>
        public static Recipe MakeCue(double hz, double seconds = 0.1) => new Recipe(RadarSounds.Marimba(hz, seconds / 3.0), 0.20, 0.05);

        /// <summary>Lo que se guarda de un sonido: se corta lo que queda 50 dB bajo su pico o por debajo de −54 dBFS (el final de la cola de la sala: con el teléfono a volumen alto sigue muy por debajo del ruido de cualquier pieza), con un fundido de 30 ms, para no gastar memoria en silencio.</summary>
        public const double TrimDb = 50.0, TrimFloorDbfs = -54.0;

        /// <summary>Las muestras estéreo ya con la sala, recortadas al final audible (<see cref="TrimDb"/>) y con el cierre (el pico queda en el nivel de la receta).</summary>
        public static void Render(Recipe recipe, out float[] left, out float[] right)
        {
            Reverb(recipe.X, recipe.Verb, out var l, out var r);
            int fade = (int)Math.Round(0.03 * Rate);
            int n = Math.Min(l.Length, (int)Math.Ceiling(SoundMeter.DurationMs(l, r, TrimDb, TrimFloorDbfs) * Rate / 1000.0) + fade);
            left = new float[n];
            right = new float[n];
            Array.Copy(l, left, n);
            Array.Copy(r, right, n);
            Finish(left, right, recipe.Level);
        }

        public static void RenderStereo(WoodSfx sfx, int arg, out float[] left, out float[] right) => Render(MakeRecipe(sfx, arg), out left, out right);

        /// <summary>Los clips de la paleta se guardan a 22.050 Hz (la mitad de memoria; la kalimba, la marimba y la sala no pasan de los 10 kHz).</summary>
        public const int ClipRate = SoundKit.HalfRate;

        /// <summary>Los eventos MICRO (tocar, tic, cuenta, pausa, quitar) llevan tan poca sala que se guardan en mono: otra mitad de memoria.</summary>
        public static bool IsMono(WoodSfx sfx) => TierOf(sfx) == WoodTier.Micro;

        /// <summary>Lo que se guarda de un evento: las muestras a <see cref="ClipRate"/>, estéreo (en mono, <paramref name="right"/> es null).</summary>
        public static void RenderForClip(WoodSfx sfx, int arg, out float[] left, out float[] right)
        {
            RenderStereo(sfx, arg, out var l, out var r);
            if (IsMono(sfx))
            {
                var mix = new float[l.Length];
                for (int i = 0; i < mix.Length; i++) mix[i] = 0.5f * (l[i] + r[i]);
                left = SoundKit.HalveRate(mix);
                right = null;
                return;
            }
            left = SoundKit.HalveRate(l);
            right = SoundKit.HalveRate(r);
        }

        /// <summary>La clave de un sonido y su variante («correct3»).</summary>
        public static string KeyOf(WoodSfx sfx, int arg) => Variants(sfx).Length > 1 ? sfx.ToString().ToLowerInvariant() + arg : sfx.ToString().ToLowerInvariant();

        /// <summary>El clip de un evento (a <see cref="ClipRate"/>; mono en los micro). Se calcula en el momento; la Fase B los precarga de a poco, como los demás juegos.</summary>
        public static AudioClip ToClip(WoodSfx sfx, int arg = 0)
        {
            RenderForClip(sfx, arg, out var l, out var r);
            int n = l.Length, channels = r == null ? 1 : 2;
            var data = new float[n * channels];
            for (int i = 0; i < n; i++)
            {
                data[i * channels] = l[i];
                if (channels == 2) data[i * 2 + 1] = r[i];
            }
            var clip = AudioClip.Create(KeyOf(sfx, arg), n, channels, ClipRate, false);
            clip.SetData(data, 0);
            return clip;
        }
    }
}
