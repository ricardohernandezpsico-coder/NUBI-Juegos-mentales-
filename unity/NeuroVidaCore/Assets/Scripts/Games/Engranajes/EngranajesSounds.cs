using System.Collections.Generic;
using UnityEngine;
using NeuroVida.Games.Shared;
using static NeuroVida.Games.Shared.SoundKit;

namespace NeuroVida.Games.Engranajes
{
    /// <summary>
    /// Los sonidos PROPIOS de Engranajes en «Madera cálida» (Fase E): los diez pasos de la cascada (clic de dientes y una nota que sube), la antena, la turbina, la compuerta y el elevador, y el despegue.
    /// </summary>
    public enum EngranajesSfx { Step0, Step1, Step2, Step3, Step4, Step5, Step6, Step7, Step8, Step9, Antenna, Turbine, Rack, Launch }

    /// <summary>
    /// «Madera cálida» en Engranajes: Taller de reparación (Fase E; spec `auditoria-fable/spec-sonido-CDE.md` §4.5; cableada el 19-oct, Tarea 139). La cascada es la secuencia más densa de la app (hasta 10 notas en 1,5 s): cada paso (cada 0,15 s, 0,21 en mayores: <see cref="EngranajesContract.StepSeconds"/>, que NO cambia)
    /// suena un clic de dientes y la nota de la escala que sube con la profundidad (<see cref="StepNoteHz"/>: <c>Penta[profundidad + 1]</c>, la altura de hoy) en MARIMBA con caída de 0,15 s (<see cref="StepTau"/>; 20 dB en ≈ 0,35 s: la pila de notas es la de hoy), un propio por paso (<see cref="EngranajesSfx.Step0"/> a <see cref="EngranajesSfx.Step9"/>).
    /// Cada pieza del cohete tiene el suyo (<see cref="EngranajesSfx.Antenna"/>, <see cref="EngranajesSfx.Turbine"/> y <see cref="EngranajesSfx.Rack"/> con <b>tres golpes graves a 0,27 s = el motor y un golpe seco al parar</b>) y el despegue (<see cref="EngranajesSfx.Launch"/>: es un HITO, no el <c>Record</c> de la app, que no puede sonar en un despegue).
    /// El resto es de la paleta: el motor que arranca <c>Hum</c>, un cambio <c>Clack</c>, un cartel que no cumplió <c>Knock</c> (y una jugada que no era <c>Knock</c> a −6 dB), el acierto de la máquina <c>Perfect</c>, la luz del cohete <c>Spark</c> (1 a 8), «NUEVO» <c>New</c>; y «Arrancar» es el toque del kit (<c>PlaysTap</c>), sin nota propia.
    /// </summary>
    public static class EngranajesSounds
    {
        /// <summary>La escala de la cascada y de las luces (pentatónica de do desde do5): las alturas de hoy NO cambian.</summary>
        public static readonly float[] Penta = { 523.25f, 587.33f, 659.25f, 783.99f, 880f, 1046.5f, 1174.66f, 1318.5f, 1567.98f, 1760f };

        /// <summary>La caída de la marimba de cada paso: 0,15 s (a 0,15 s entre pasos, la pila de notas es la de hoy).</summary>
        public const double StepTau = 0.15;

        /// <summary>El número de nota (0 a 9) del paso de profundidad [depth] (0 = el motor): <c>profundidad + 1</c>, como hoy.</summary>
        public static int StepIndex(int depth) => Mathf.Clamp(depth + 1, 0, Penta.Length - 1);

        /// <summary>La frecuencia de la nota del paso [depth].</summary>
        public static double StepNoteHz(int depth) => Penta[StepIndex(depth)];

        /// <summary>El propio del paso de profundidad [depth].</summary>
        public static EngranajesSfx StepSfx(int depth) => (EngranajesSfx)StepIndex(depth);

        /// <summary>La receta de un sonido propio: el nivel (pico) y la sala de cada una son los de la spec; la familia declara su banda de sonoridad.</summary>
        public static WoodSounds.Recipe MakeRecipe(EngranajesSfx sfx)
        {
            if (sfx <= EngranajesSfx.Step9)                                 // un paso: el clic de dientes (bloque de madera agudo) y la nota de marimba que sube
            {
                var x = Blank(0.9);
                Add(x, WoodKit.Woodblock(1800), 0, 0.5);
                Add(x, WoodKit.Marimba(Penta[(int)sfx], StepTau), 0.01);
                return new WoodSounds.Recipe(x, 0.16, 0.10, WoodTier.Respuesta);
            }
            switch (sfx)
            {
                case EngranajesSfx.Antenna:                                 // la antena: un silbido de viento que sube y una campana aguda (sol6)
                {
                    var x = Blank(1.6);
                    Add(x, WoodKit.NoiseSweep(0.8, 600, 2600, FilterType.BandPass, 0.8, 0.02, 0.25, 61));
                    Add(x, WoodKit.Kalimba(1567.98, 0.35), 0.3, 0.6);
                    return new WoodSounds.Recipe(x, 0.16, 0.18, WoodTier.Textura);
                }
                case EngranajesSfx.Turbine:                                 // la turbina: un rugido que sube de grave a medio; el golpe grave que SUBE hace de retumbo
                {
                    var x = Blank(1.6);
                    Add(x, WoodKit.NoiseSweep(1.1, 180, 900, FilterType.BandPass, 0.7, 0.05, 0.35, 23));
                    Add(x, WoodKit.Thump(1.1, 55, 110, 0.5), 0, 0.5);
                    return new WoodSounds.Recipe(x, 0.18, 0.12, WoodTier.Textura);
                }
                case EngranajesSfx.Rack:                                    // la compuerta y el elevador: tres golpes graves (el motor) y, al parar, un golpe seco
                {
                    var x = Blank(1.4);
                    for (int h = 0; h < 3; h++)
                    {
                        Add(x, WoodKit.Marimba(98, 0.25), h * RackBeat, 0.7);
                        Add(x, WoodKit.Marimba(146.83, 0.25), h * RackBeat + 0.03, 0.4);
                    }
                    Add(x, WoodKit.Thump(0.3, 110, 55, 0.09), RackStop);
                    Add(x, WoodKit.Woodblock(520), RackStop, 0.25);
                    return new WoodSounds.Recipe(x, 0.18, 0.12, WoodTier.Textura);
                }
                default:                                                    // Launch: el despegue: un rugido que sube 2,6 s, cinco kalimbas que ascienden desde 1,0 s cada 0,14 s y un acorde de marimba grave (do, sol, do) que lo corona
                {
                    var x = Blank(4.2);
                    Add(x, WoodKit.NoiseSweep(2.6, 90, 420, FilterType.BandPass, 0.7, 0.3, 0.6, 23), 0, 0.8);
                    Add(x, WoodKit.Thump(2.6, 50, 120, 1.0), 0, 0.4);
                    for (int i = 0; i < 5; i++) Add(x, WoodKit.Kalimba(Penta[2 + (int)(i * 1.5)], 0.5), LaunchBellsFrom + i * LaunchBellGap, 0.7);
                    double[] chord = { 261.63, 392, 523.25 };
                    for (int j = 0; j < chord.Length; j++) Add(x, WoodKit.Marimba(chord[j], 0.5), 1.8 + j * 0.12, 0.6);
                    return new WoodSounds.Recipe(x, 0.30, 0.25, WoodTier.Hito);
                }
            }
        }

        /// <summary>Los tres golpes graves del motor de la compuerta (cada 0,27 s) y el golpe seco al parar (a 0,8 s): los instantes de la receta.</summary>
        public const double RackBeat = 0.27, RackStop = 0.8;

        /// <summary>Las cinco kalimbas del despegue: desde 1,0 s, una cada 0,14 s (como las cinco campanas de hoy).</summary>
        public const double LaunchBellsFrom = 1.0, LaunchBellGap = 0.14;

        /// <summary>La clave de un sonido propio («engranajes.step3», «engranajes.rack»).</summary>
        public static string Key(EngranajesSfx sfx) => "engranajes." + sfx.ToString().ToLowerInvariant();

        /// <summary>Los pasos y las piezas llevan sala (estéreo, el uso de memoria cabe holgado en los 10 MB del juego).</summary>
        public static bool Mono(EngranajesSfx sfx) => false;

        /// <summary>Los sonidos propios con su clave y su receta: los lee la prueba de niveles y la página de escucha.</summary>
        public static IEnumerable<(string Key, WoodSounds.Recipe Recipe, bool Mono)> All()
        {
            foreach (EngranajesSfx sfx in System.Enum.GetValues(typeof(EngranajesSfx))) yield return (Key(sfx), MakeRecipe(sfx), Mono(sfx));
        }

        /// <summary>Suena un sonido propio.</summary>
        public static bool Play(EngranajesSfx sfx) => WoodCore.Play(Key(sfx));

        /// <summary>Suena el paso de la cascada de profundidad [depth] (clic de dientes y su nota).</summary>
        public static bool PlayStep(int depth) => Play(StepSfx(depth));

        /// <summary>
        /// Lo que Engranajes pide antes de la cuenta regresiva, en el orden en que suena: el zumbido del motor (arranca cada máquina), los diez pasos de la cascada, el cambio (<c>Clack</c>), el cartel que no cumplió (<c>Knock</c>), las tres piezas del cohete, el acierto (<c>Perfect</c>), las ocho luces (<c>Spark</c>),
        /// «NUEVO» y, al final, el despegue (el más largo y el último en usarse). Lo demás va con el núcleo.
        /// </summary>
        public static void Request()
        {
            WoodCore.Request(WoodSfx.Hum);
            for (EngranajesSfx s = EngranajesSfx.Step0; s <= EngranajesSfx.Step9; s++) RequestOwn(s);
            WoodCore.Request(WoodSfx.Clack);
            WoodCore.Request(WoodSfx.Knock);
            RequestOwn(EngranajesSfx.Antenna);
            RequestOwn(EngranajesSfx.Turbine);
            RequestOwn(EngranajesSfx.Rack);
            WoodCore.Request(WoodSfx.Perfect);
            for (int k = 1; k <= 8; k++) WoodCore.Request(WoodSfx.Spark, k);
            WoodCore.Request(WoodSfx.New);
            RequestOwn(EngranajesSfx.Launch);
        }

        private static void RequestOwn(EngranajesSfx sfx) => WoodCore.Request(Key(sfx), () => MakeRecipe(sfx), Mono(sfx));
    }
}
