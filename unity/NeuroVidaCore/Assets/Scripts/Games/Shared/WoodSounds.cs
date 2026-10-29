using System;
using System.Collections.Generic;
using UnityEngine;
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
    /// PALETA SEMÁNTICA «MADERA CÁLIDA» (Tarea 119, Fase A, 17-oct; conectada en la Fase B, Tarea 120): lo que Ricardo aprobó el 10-oct en Rescate relámpago (kalimba, marimba y bloque de madera con una sala pequeña y suave: <see cref="WoodKit"/>, <see cref="SoundKit"/>) ordenado como un vocabulario para toda la app. Cada evento
    /// (tocar, elegir, acertar, errar, ayuda, nuevo, hito, fin…) tiene UN sonido, siempre con el mismo instrumento y la misma altura, y un nivel según su familia (<see cref="WoodTier"/>): lo que pasa todo el tiempo es lo más suave y lo que se celebra es lo más fuerte; así todo queda parejo.
    /// Reusa tal cual las recetas aprobadas (marcar, desmarcar, ronda perfecta, «no era», final) y arma con las mismas piezas lo que faltaba. Desde la Fase B lo suena <c>GameFeel</c> (acierto, error, nivel, tic, fin, cuenta, «¡Ya!», pausa, aviso y toque del kit) a través de <see cref="WoodCore"/>, que lo calcula una vez y lo guarda;
    /// los sonidos propios de cada juego pasan a la paleta en las Fases C a E. docs/sonido-madera-plan.md tiene el mapa evento por evento, los niveles medidos y el plan por fases. Aquí, <see cref="Render"/> y <see cref="RenderStereo"/> dan las muestras como se CALCULAN (estéreo, a 44.100 Hz, con el pico en el nivel de la receta: lo que escucha la página de escucha) y
    /// <see cref="RenderForClip"/> y <see cref="BakeJob"/> como se GUARDAN (22.050 Hz, los micro en mono y con el volumen maestro de <see cref="WoodKit.MasterGainDb"/>). Todo es determinista (mismas semillas): el mismo evento suena siempre igual.
    /// </summary>
    public static class WoodSounds
    {
        /// <summary>El volumen con el que se reproduce cada clip (el del laboratorio donde Ricardo eligió «Madera cálida»; el que ya usan Rescate, Acoplamiento y Aterrizaje): vive en <see cref="WoodKit.PlayVolume"/>.</summary>
        public const float PlayVolume = WoodKit.PlayVolume;

        /// <summary>La pentatónica de do mayor desde do5 (la de todos los juegos): do5, re5, mi5, sol5, la5, do6, re6, mi6, sol6, la6, do7, re7, mi7.</summary>
        private static readonly double[] P = WoodKit.P;

        /// <summary>La cola de sala que se calcula (como máximo): lo que queda por debajo de lo audible se recorta después (<see cref="Render"/>), así la sala nunca se corta de golpe y los sonidos suaves no gastan memoria en una cola que no se oye.</summary>
        public const double MaxTail = 1.5;

        /// <summary>
        /// La receta de un sonido: las muestras mono antes de la sala, el nivel del pico final y la mezcla de sala (la sala es la de Rescate). Desde el paso 0 de las Fases C a E lleva también su FAMILIA (<see cref="Tier"/>): los sonidos propios de cada juego declaran a cuál pertenecen y la prueba de niveles
        /// (<c>GameSoundsTierTests</c>) los mide contra la banda de esa familia. Los de la paleta no la usan (su familia sale de <see cref="TierOf"/>).
        /// </summary>
        public readonly struct Recipe
        {
            public readonly float[] X;
            public readonly double Level;
            public readonly ReverbSettings Verb;
            public readonly WoodTier Tier;

            public Recipe(float[] x, double level, double wet, WoodTier tier = WoodTier.Textura)
            {
                X = x;
                Level = level;
                Verb = new ReverbSettings(WoodKit.Room.Room, WoodKit.Room.Damp, wet, MaxTail, WoodKit.Room.Width);
                Tier = tier;
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
                case WoodSfx.Correct: return new[] { 1, 2, 3, 4, 5, 6, 7 };                       // la racha 7 en adelante suena igual que la 7
                case WoodSfx.Spark: return new[] { 1, 2, 3, 4, 5, 6, 7, 8 };
                case WoodSfx.Select: return new[] { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9 };
                case WoodSfx.Count: return new[] { 3, 2, 1 };
                default: return new[] { 0 };
            }
        }

        private static int Clamp(int v, int lo, int hi) => Math.Max(lo, Math.Min(hi, v));

        /// <summary>Un rodado de marimba (cada nota <paramref name="gap"/> s después de la anterior).</summary>
        private static float[] Marimbas(double[] notes, double gap, double tau, double total) => Seq(notes, gap, (f, i) => WoodKit.Marimba(f, tau), total);

        /// <summary>La receta de un evento (<paramref name="arg"/> es la racha, la marca o la cuenta, según el evento).</summary>
        public static Recipe MakeRecipe(WoodSfx sfx, int arg = 0)
        {
            switch (sfx)
            {
                case WoodSfx.Tap:                                                                      // un toque o un botón presionado: un toque de madera apagado (el de Acoplamiento)
                    return new Recipe(WoodKit.Marimba(392, 0.06), 0.07, 0.18);
                case WoodSfx.Select:                                                                   // elegir o marcar: una kalimba; la altura sube con la cantidad (do5, re5, mi5…): el de Rescate
                    return new Recipe(WoodKit.Kalimba(P[Clamp(arg, 0, 9)]), 0.14, 0.18);
                case WoodSfx.Unselect:                                                                 // quitar o deshacer: una kalimba apagada
                    return new Recipe(WoodKit.Kalimba(392, 0.07), 0.16, 0.18);
                case WoodSfx.Tick:                                                                     // el último tramo del reloj: un bloque de madera
                    return new Recipe(WoodKit.Woodblock(1250), 0.22, 0.25);
                case WoodSfx.Count:                                                                    // la cuenta regresiva 3, 2, 1: una marimba que sube un escalón por número
                    return new Recipe(WoodKit.Marimba(P[Clamp(3 - arg, 0, 2)], 0.12), 0.14, 0.18);

                case WoodSfx.Correct:                                                                  // acertar: una kalimba de DOS notas que suben; la pareja sube por la escala con la racha
                {
                    int s = Clamp(arg - 1, 0, 6);
                    var x = Blank(1.5);
                    Add(x, WoodKit.Kalimba(P[s], 0.3), 0, 0.85);
                    Add(x, WoodKit.Kalimba(P[s + 2], 0.42), 0.09, 1.0);
                    return new Recipe(x, 0.24, 0.18);
                }
                case WoodSfx.Wrong:                                                                    // errar: dos notas graves y suaves de marimba que bajan (el «no era» de Rescate, Acoplamiento y Aterrizaje)
                    return new Recipe(Seq(new[] { 196.0, 164.81 }, 0.13, (f, i) => WoodKit.Marimba(f, 0.25), 0.9), 0.22, 0.18);
                case WoodSfx.Knock:                                                                    // «eso no se puede»: UN toc de madera grave y hueco
                {
                    var x = WoodKit.Thump(0.25, 120, 70, 0.08);
                    Add(x, WoodKit.Marimba(185, 0.12), 0, 0.7);
                    return new Recipe(x, 0.28, 0.12);
                }
                case WoodSfx.Hint:                                                                     // la ayuda de Nubi: un soplo que sube y una kalimba grave
                {
                    var x = Blank(1.4);
                    Add(x, WoodKit.NoiseSweep(0.5, 400, 1600, FilterType.BandPass, 0.8, 0.02, 0.2, 51), 0, 0.5);
                    Add(x, WoodKit.Kalimba(392, 0.5), 0.32, 0.8);
                    return new Recipe(x, 0.18, 0.18);
                }
                case WoodSfx.New:                                                                      // «NUEVO», sorpresa, algo que se desbloquea: dos kalimbas agudas que suben
                {
                    var x = Blank(1.6);
                    Add(x, WoodKit.Kalimba(P[5], 0.4));
                    Add(x, WoodKit.Kalimba(P[7], 0.5), 0.12);
                    return new Recipe(x, 0.26, 0.25);
                }
                case WoodSfx.Notice:                                                                   // un aviso en pantalla: una marimba sola
                    return new Recipe(WoodKit.Marimba(P[5], 0.2), 0.16, 0.18);
                case WoodSfx.Spark:                                                                    // el destello de la racha: una kalimba aguda que sube con cada acierto seguido
                    return new Recipe(WoodKit.Kalimba(P[Clamp(4 + arg, 5, 12)], 0.25), 0.14, 0.18);

                case WoodSfx.Streak:                                                                   // un hito de racha: un arpegio corto de kalimba sobre una marimba grave
                {
                    var x = Seq(new[] { P[2], P[4], P[5] }, 0.07, (f, i) => WoodKit.Kalimba(f, 0.35), 1.6);
                    Add(x, WoodKit.Marimba(P[0], 0.3), 0, 0.5);
                    return new Recipe(x, 0.30, 0.18);
                }
                case WoodSfx.LevelUp:                                                                  // subir de nivel: el arpegio do-mi-sol-do de kalimba (las mismas notas de siempre)
                    return new Recipe(Seq(new[] { P[0], P[2], P[3], P[5] }, 0.065, (f, i) => WoodKit.Kalimba(f, 0.35), 1.5), 0.32, 0.18);
                case WoodSfx.Perfect:                                                                  // una ronda perfecta: la de Rescate (do-mi-sol-do de marimba y dos kalimbas)
                {
                    var x = Marimbas(new[] { P[0], P[2], P[3], P[5] }, 0.08, 0.35, 1.6);
                    Add(x, WoodKit.Kalimba(P[5]), 0.34, 0.6);
                    Add(x, WoodKit.Kalimba(P[7]), 0.42, 0.6);
                    return new Recipe(x, 0.34, 0.18);
                }
                case WoodSfx.Go:                                                                       // «¡Ya!»: dos marimbas y una kalimba aguda
                {
                    var x = Marimbas(new[] { P[0], P[3] }, 0.05, 0.25, 1.4);
                    Add(x, WoodKit.Kalimba(P[5], 0.4), 0.1, 0.8);
                    return new Recipe(x, 0.30, 0.18);
                }

                case WoodSfx.Record:                                                                   // un récord: una escalera de kalimba hasta lo agudo y el acorde de marimba
                {
                    var x = Seq(new[] { P[0], P[2], P[3], P[5], P[7], P[9] }, 0.06, (f, i) => WoodKit.Kalimba(f, 0.35), 2.2);
                    double[] chord = { 261.63, 392, 523.25 };
                    for (int i = 0; i < chord.Length; i++) Add(x, WoodKit.Marimba(chord[i], 0.5), 0.42 + i * 0.05, 0.75);
                    return new Recipe(x, 0.40, 0.18);
                }
                case WoodSfx.Finish:                                                                   // el fin de la partida: el final de Rescate (acorde de marimba y una kalimba larga)
                {
                    var x = Blank(3);                                                                  // la receta de Rescate (RadarSfx.Finale), muestra por muestra: una prueba lo comprueba
                    double[] chord = { 261.63, 329.63, 392, 523.25, 659.25, 783.99, 1046.5 };
                    for (int i = 0; i < chord.Length; i++)
                    {
                        var m = WoodKit.Marimba(chord[i], 0.4);
                        for (int h = 0; h < 3; h++) Add(x, m, i * 0.07 + h * 0.05, h != 0 ? 0.35 : 0.8);
                    }
                    Add(x, WoodKit.Kalimba(P[7], 0.6), 0.55, 0.7);
                    return new Recipe(x, 0.4, WoodKit.Room.Wet);
                }

                case WoodSfx.PauseIn:                                                                  // abrir la pausa: dos marimbas suaves que bajan
                    return new Recipe(Marimbas(new[] { P[2], P[0] }, 0.1, 0.15, 0.8), 0.12, 0.25);
                case WoodSfx.PauseOut:                                                                 // volver de la pausa: las mismas dos que suben
                    return new Recipe(Marimbas(new[] { P[0], P[2] }, 0.1, 0.15, 0.8), 0.12, 0.25);

                case WoodSfx.SweepUp:                                                                  // un soplo que sube (algo que aparece o se abre)
                    return new Recipe(WoodKit.NoiseSweep(0.5, 500, 2200, FilterType.BandPass, 0.8, 0.01, 0.15, 61), 0.12, 0.1);
                case WoodSfx.SweepDown:                                                                // un soplo que baja (algo que se suelta, se cierra o vuelve): el de Aterrizaje
                    return new Recipe(WoodKit.NoiseSweep(0.5, 1400, 300, FilterType.LowPass, 0.7, 0.004, 0.14, 41), 0.12, 0.18);
                case WoodSfx.Hum:                                                                      // un zumbido que arranca: dos marimbas graves
                {
                    var x = Blank(1.6);
                    Add(x, WoodKit.Marimba(196, 0.5));
                    Add(x, WoodKit.Marimba(293.66, 0.5), 0.1);
                    return new Recipe(x, 0.12, 0.18);
                }
                default:                                                                               // Clack: un golpe seco de un objeto (el de Aterrizaje al posarse)
                {
                    var x = WoodKit.Thump(0.3, 110, 55, 0.09);
                    Add(x, WoodKit.Woodblock(520), 0, 0.25);
                    return new Recipe(x, 0.26, 0.18);
                }
            }
        }

        /// <summary>
        /// La receta de UNA NOTA que lleva información (la altura del juego: la del lucero, del grupo, de la escotilla, del buzón…): el mismo Hz de siempre con el timbre de la paleta. Kalimba: el timbre de Rescate pero con una caída de 0,3 s (la de Rescate, de 0,45 s, sigue sonando más de un segundo y se encimaría con la nota siguiente de una secuencia: la
        /// caída de hoy de estas notas es de unos 0,2 a 0,4 s); marimba: más «afinada» (sus parciales son múltiplos). Las dos caen a −20 dB en cerca de 0,7 s con la caída de siempre (<paramref name="tau"/> 0,3 s).
        /// <para><paramref name="tau"/> (paso 0 de las Fases C a E): donde el juego marca el ritmo y las notas se encimarían, la caída es más corta: 0,2 s en Rastro de luz y 0,15 s en la cascada de Engranajes (nivel 0,30 y sala 0,18 no cambian).</para>
        /// </summary>
        public static Recipe MakeNote(double hz, WoodVoice voice, double tau = 0.3)
        {
            if (voice == WoodVoice.Kalimba) return new Recipe(WoodKit.Kalimba(hz, tau), 0.30, 0.18, WoodTier.Respuesta);
            return new Recipe(WoodKit.Marimba(hz, tau), 0.30, 0.18, WoodTier.Respuesta);
        }

        /// <summary>
        /// Una SEÑAL CORTA con altura (el tono de la regla de Tinta o Palabra, la señal de salida de Freno, el «blip» de Piloto…): la misma altura y un solo golpe seco de marimba que dura lo mismo que el tono de hoy (<paramref name="seconds"/>: 0,06 s la salida de Freno, 0,1 s las reglas de Tinta o Palabra), casi sin sala:
        /// empieza igual de rápido y no se le encima al estímulo siguiente.
        /// </summary>
        public static Recipe MakeCue(double hz, double seconds = 0.1) => new Recipe(WoodKit.Marimba(hz, seconds / 3.0), 0.20, 0.05, WoodTier.Respuesta);

        /// <summary>Lo que se guarda de un sonido: se corta lo que queda 50 dB bajo su pico o por debajo de −54 dBFS (el final de la cola de la sala: con el teléfono a volumen alto sigue muy por debajo del ruido de cualquier pieza), con un fundido de 30 ms, para no gastar memoria en silencio.</summary>
        public const double TrimDb = 50.0, TrimFloorDbfs = -54.0;

        /// <summary>Recorta la sala al final audible (<see cref="TrimDb"/>) y cierra: el pico queda en el nivel de la receta y el último tramo se funde (sin clic de corte).</summary>
        private static void Close(float[] l, float[] r, Recipe recipe, out float[] left, out float[] right)
        {
            int fade = (int)Math.Round(0.03 * Rate);
            int n = Math.Min(l.Length, (int)Math.Ceiling(SoundMeter.DurationMs(l, r, TrimDb, TrimFloorDbfs) * Rate / 1000.0) + fade);
            left = new float[n];
            right = new float[n];
            Array.Copy(l, left, n);
            Array.Copy(r, right, n);
            Finish(left, right, recipe.Level);
        }

        /// <summary>Las muestras estéreo ya con la sala, recortadas al final audible (<see cref="TrimDb"/>) y con el cierre (el pico queda en el nivel de la receta; sin el volumen maestro: así las mide la página de escucha).</summary>
        public static void Render(Recipe recipe, out float[] left, out float[] right)
        {
            Reverb(recipe.X, recipe.Verb, out var l, out var r);
            Close(l, r, recipe, out left, out right);
        }

        public static void RenderStereo(WoodSfx sfx, int arg, out float[] left, out float[] right) => Render(MakeRecipe(sfx, arg), out left, out right);

        /// <summary>Los clips de la paleta se guardan a 22.050 Hz (la mitad de memoria; la kalimba, la marimba y la sala no pasan de los 10 kHz).</summary>
        public const int ClipRate = SoundKit.HalfRate;

        /// <summary>Los eventos MICRO (tocar, tic, cuenta, pausa, quitar) llevan tan poca sala que se guardan en mono: otra mitad de memoria.</summary>
        public static bool IsMono(WoodSfx sfx) => TierOf(sfx) == WoodTier.Micro;

        /// <summary>De las muestras calculadas (estéreo, 44.100 Hz) a las que se guardan: mono si <paramref name="mono"/> (los micro de la paleta y los propios cortos sin sala), a <see cref="ClipRate"/> y con el volumen maestro (<see cref="WoodKit.ApplyMaster"/>).</summary>
        private static void Store(bool mono, float[] l, float[] r, out float[] left, out float[] right)
        {
            if (mono)
            {
                var mix = new float[l.Length];
                for (int i = 0; i < mix.Length; i++) mix[i] = 0.5f * (l[i] + r[i]);
                left = SoundKit.HalveRate(mix);
                right = null;
            }
            else
            {
                left = SoundKit.HalveRate(l);
                right = SoundKit.HalveRate(r);
            }
            WoodKit.ApplyMaster(left, right);
        }

        /// <summary>Lo que se guarda de un evento: las muestras a <see cref="ClipRate"/>, estéreo (en mono, <paramref name="right"/> es null) y con el volumen maestro.</summary>
        public static void RenderForClip(WoodSfx sfx, int arg, out float[] left, out float[] right)
        {
            RenderStereo(sfx, arg, out var l, out var r);
            Store(IsMono(sfx), l, r, out left, out right);
        }

        /// <summary>Lo que se guarda de una receta suelta (una nota, una señal o un sonido propio de un juego): como <see cref="RenderForClip(WoodSfx,int,out float[],out float[])"/>, con <paramref name="mono"/> a elección.</summary>
        public static void RenderForClip(Recipe recipe, bool mono, out float[] left, out float[] right)
        {
            Render(recipe, out var l, out var r);
            Store(mono, l, r, out left, out right);
        }

        /// <summary>Un clip a partir de muestras guardadas (<paramref name="right"/> null = mono).</summary>
        public static AudioClip MakeClip(string name, float[] left, float[] right)
        {
            int n = left.Length, channels = right == null ? 1 : 2;
            var data = new float[n * channels];
            for (int i = 0; i < n; i++)
            {
                data[i * channels] = left[i];
                if (channels == 2) data[i * 2 + 1] = right[i];
            }
            var clip = AudioClip.Create(name, n, channels, ClipRate, false);
            clip.SetData(data, 0);
            return clip;
        }

        /// <summary>
        /// El horneado de UN evento DE A POCO (cada <see cref="Step"/> es la síntesis, un pedazo de la sala o el cierre): así <see cref="WoodCore"/> calcula todo el núcleo común durante la cuenta regresiva sin trabar ningún cuadro. Da el mismo clip que <see cref="ToClip"/>.
        /// </summary>
        public sealed class BakeJob
        {
            private readonly WoodSfx _sfx;
            private readonly int _arg;
            private readonly Func<Recipe> _make;                         // un horneado GENÉRICO (nota, señal o propio de un juego): la receta se arma en el primer paso, no al crear el trabajo
            private readonly string _key;
            private readonly bool _mono;
            private Recipe _recipe;
            private ReverbJob _reverb;
            private int _stage;

            public BakeJob(WoodSfx sfx, int arg)
            {
                _sfx = sfx;
                _arg = arg;
                _mono = IsMono(sfx);
            }

            /// <summary>Un horneado genérico (paso 0 de las Fases C a E): una receta suelta con su clave y si se guarda en mono. Da el mismo clip que <see cref="ToClip(Recipe,string,bool)"/>; con una receta de la paleta y <c>mono = IsMono</c>, el mismo que <see cref="ToClip(WoodSfx,int)"/>.</summary>
            public BakeJob(Func<Recipe> make, string key, bool mono)
            {
                _make = make;
                _key = key;
                _mono = mono;
            }

            public BakeJob(Recipe recipe, string key, bool mono) : this(() => recipe, key, mono) { }

            public bool Done => _stage >= 3;

            /// <summary>El nombre con que se crea el clip (la clave del sonido).</summary>
            public string ClipName => _make != null ? _key : KeyOf(_sfx, _arg);

            /// <summary>
            /// Calcula las muestras del clip DE UNA VEZ —la receta, la sala, el cierre y el guardado— sin tocar ninguna API de Unity (solo aritmética): es lo que corre en un HILO DE TRABAJO (Tarea 190, <see cref="WoodCore"/>); el hilo principal solo crea el clip con <see cref="MakeClip"/>.
            /// Da exactamente las mismas muestras que los <see cref="Step"/> sucesivos y que <see cref="RenderForClip(WoodSfx,int,out float[],out float[])"/> (la prueba lo mide).
            /// </summary>
            public void ComputeSamples(out float[] left, out float[] right)
            {
                var recipe = _make != null ? _make() : MakeRecipe(_sfx, _arg);
                var reverb = new ReverbJob(recipe.X, recipe.Verb);
                reverb.Run(int.MaxValue);
                reverb.Result(out var l, out var r);
                Close(l, r, recipe, out var cl, out var cr);
                Store(_mono, cl, cr, out left, out right);
            }

            /// <summary>El clip, una vez que <see cref="Done"/>.</summary>
            public AudioClip Clip { get; private set; }

            /// <summary>Avanza un poco: la síntesis, hasta <paramref name="reverbSamples"/> muestras de sala o el cierre. Devuelve true cuando el clip ya está listo.</summary>
            public bool Step(int reverbSamples = 24000)
            {
                switch (_stage)
                {
                    case 0:
                        _recipe = _make != null ? _make() : MakeRecipe(_sfx, _arg);
                        _reverb = new ReverbJob(_recipe.X, _recipe.Verb);
                        _stage = 1;
                        return false;
                    case 1:
                        _reverb.Run(reverbSamples);
                        if (_reverb.Done) _stage = 2;
                        return false;
                    case 2:
                        _reverb.Result(out var l, out var r);
                        Close(l, r, _recipe, out var cl, out var cr);
                        Store(_mono, cl, cr, out var left, out var right);
                        Clip = MakeClip(_make != null ? _key : KeyOf(_sfx, _arg), left, right);
                        _stage = 3;
                        return true;
                    default:
                        return true;
                }
            }
        }

        /// <summary>La clave de un sonido y su variante («correct3»).</summary>
        public static string KeyOf(WoodSfx sfx, int arg) => Variants(sfx).Length > 1 ? sfx.ToString().ToLowerInvariant() + arg : sfx.ToString().ToLowerInvariant();

        /// <summary>El clip de un evento (a <see cref="ClipRate"/>; mono en los micro; con el volumen maestro), calculado de una vez. Para pruebas y recetas sueltas: el juego los hornea de a poco (<see cref="BakeJob"/>).</summary>
        public static AudioClip ToClip(WoodSfx sfx, int arg = 0)
        {
            RenderForClip(sfx, arg, out var l, out var r);
            return MakeClip(KeyOf(sfx, arg), l, r);
        }

        /// <summary>El clip de una receta suelta (de una vez): para pruebas; el juego lo hornea de a poco con <see cref="BakeJob"/>.</summary>
        public static AudioClip ToClip(Recipe recipe, string key, bool mono)
        {
            RenderForClip(recipe, mono, out var l, out var r);
            return MakeClip(key, l, r);
        }
    }
}
