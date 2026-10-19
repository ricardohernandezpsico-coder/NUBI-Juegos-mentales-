using System.Collections.Generic;
using UnityEngine;
using NeuroVida.Games.Shared;
using static NeuroVida.Games.Shared.SoundKit;

namespace NeuroVida.Games.Intrusa
{
    /// <summary>Los sonidos PROPIOS de La estrella intrusa en «Madera cálida» (Fase D): el grabado del dibujo y la estrella fugaz que cae.</summary>
    public enum IntrusaSfx { Engrave, Fall }

    /// <summary>
    /// «Madera cálida» en La estrella intrusa (Fase D; spec `auditoria-fable/spec-sonido-CDE.md` §3.4; cableada el 19-oct, Tarea 138). La chispa que traza la figura toca una nota de la escala ascendente en cada estrella (9, do5 a sol6: <see cref="NoteHz"/>, la altura de hoy NO cambia) en MARIMBA; la estrella
    /// CON palabra suena 6 dB más fuerte que la que no la tiene: esa diferencia es la información y sale de dos pasos de ganancia de la paleta (<see cref="PlayNote"/>: −6 dB sin palabra, 0 con ella; una sola receta de cada nota). Al cerrar la figura <c>Perfect</c> (con −6 dB en la variante tranquila), el error <c>Wrong</c>,
    /// «¿Qué las une?» acertada <c>Correct 3</c>, la opción que no era <c>Knock</c> y el latido de las cuatro estrellas mientras se piensa <c>Hum</c> a −3 dB (dos marimbas graves que arrancan: un latido grave; el plan decía <c>Tap</c>, que es un toque agudo de 60 ms). Y DOS propios (<see cref="IntrusaSfx"/>): el grabado
    /// (un barrido de ruido y una marimba grave, tan largo como el dibujo) y la estrella fugaz que cae (un soplo que baja y una kalimba aguda: con <c>SweepDown</c> de la paleta quedaba 9 LU más baja que hoy y casi no se oía; es el momento en que la intrusa se va, y merece un sonido propio del juego).
    /// </summary>
    public static class IntrusaSounds
    {
        // Pentatónica mayor desde do5, igual que GameFeel: todo suena afinado con lo demás.
        private static readonly float[] Penta = { 523.25f, 587.33f, 659.25f, 783.99f, 880.00f, 1046.50f, 1174.66f, 1318.51f, 1567.98f };

        public static int NoteCount => Penta.Length;

        /// <summary>La frecuencia de la nota [step] (0 = do5, sube por la pentatónica): la de hoy.</summary>
        public static float NoteHz(int step) => Penta[Mathf.Clamp(step, 0, Penta.Length - 1)];

        /// <summary>Cuánto más suave suena la estrella SIN palabra: −6 dB (dos pasos de 3 dB). La estrella con palabra suena sin atenuación: esos 6 dB son la información.</summary>
        public const float WithoutWordGainDb = -6f;

        /// <summary>Suena la nota [step] (marimba, la altura de hoy): [strong] = estrella con palabra, 6 dB más fuerte que sin ella (la MISMA receta, dos pasos de ganancia).</summary>
        public static bool PlayNote(int step, bool strong) => WoodCore.PlayNote(NoteHz(step), WoodVoice.Marimba, gainDb: strong ? 0f : WithoutWordGainDb);

        /// <summary>La receta de un sonido propio: el nivel (pico) y la sala de cada una son los de la spec; la familia declara su banda de sonoridad.</summary>
        public static WoodSounds.Recipe MakeRecipe(IntrusaSfx sfx)
        {
            var x = Blank(1.6);
            if (sfx == IntrusaSfx.Engrave)                                  // el grabado: un barrido cálido que sube mientras el dibujo se completa y una marimba grave debajo
            {
                Add(x, WoodKit.NoiseSweep(1.1, 700, 2500, FilterType.BandPass, 0.8, 0.05, 0.35, 61));
                Add(x, WoodKit.Marimba(261.63, 0.6), 0.1, 0.4);
                return new WoodSounds.Recipe(x, 0.10, 0.18, WoodTier.Textura);
            }
            x = Blank(1.2);                                                  // la estrella fugaz: un soplo que baja de lo agudo a lo grave y una kalimba aguda (sol6) al empezar
            Add(x, WoodKit.NoiseSweep(0.9, 4200, 500, FilterType.LowPass, 0.8, 0.02, 0.35, 71));
            Add(x, WoodKit.Kalimba(1567.98, 0.18), 0.0, 0.5);
            return new WoodSounds.Recipe(x, 0.14, 0.18, WoodTier.Textura);
        }

        /// <summary>La clave de un sonido propio («intrusa.engrave»).</summary>
        public static string Key(IntrusaSfx sfx) => "intrusa." + sfx.ToString().ToLowerInvariant();

        /// <summary>Texturas con sala: estéreo.</summary>
        public static bool Mono(IntrusaSfx sfx) => false;

        /// <summary>Los sonidos propios con su clave y su receta: los lee la prueba de niveles y la página de escucha.</summary>
        public static IEnumerable<(string Key, WoodSounds.Recipe Recipe, bool Mono)> All()
        {
            foreach (IntrusaSfx sfx in System.Enum.GetValues(typeof(IntrusaSfx))) yield return (Key(sfx), MakeRecipe(sfx), Mono(sfx));
        }

        /// <summary>Suena un sonido propio.</summary>
        public static bool Play(IntrusaSfx sfx) => WoodCore.Play(Key(sfx));

        /// <summary>Lo que Intrusa pide antes de la cuenta regresiva, en el orden en que suena: las 9 notas de la escala (la primera figura se traza enseguida), el cierre de la figura, la estrella fugaz, el latido, el error, «¿Qué las une?» y el grabado. <c>Correct 3</c> y <c>Wrong</c> ya van con el núcleo.</summary>
        public static void Request()
        {
            for (int i = 0; i < Penta.Length; i++) WoodCore.RequestNote(Penta[i], WoodVoice.Marimba);
            WoodCore.Request(WoodSfx.Perfect);
            WoodCore.Request(Key(IntrusaSfx.Fall), () => MakeRecipe(IntrusaSfx.Fall), Mono(IntrusaSfx.Fall));
            WoodCore.Request(WoodSfx.Hum);
            WoodCore.Request(WoodSfx.Knock);
            WoodCore.Request(Key(IntrusaSfx.Engrave), () => MakeRecipe(IntrusaSfx.Engrave), Mono(IntrusaSfx.Engrave));
        }
    }
}
