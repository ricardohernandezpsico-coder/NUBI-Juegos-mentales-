using System.Collections.Generic;
using NeuroVida.Games.Shared;
using static NeuroVida.Games.Shared.SoundKit;

namespace NeuroVida.Games.Bodega
{
    /// <summary>Los sonidos PROPIOS de Bodega de carga en «Madera cálida» (Fase C): lo que no es de la paleta ni una nota de escotilla.</summary>
    public enum BodegaSfx { Arrive, Beam, Open, Fly, Ratchet }

    /// <summary>
    /// «Madera cálida» en Bodega de carga (Fase C; spec `auditoria-fable/spec-sonido-CDE.md` §2.4): cada escotilla suena SU nota (12, kalimba: <see cref="NoteHz"/>, la altura de hoy NO cambia); el robot arranca <c>Hum</c>, cerrar una puerta <c>Clack</c> 6 dB más bajo, tomar o dejar la caja <c>Clack</c>,
    /// la pregunta de un pedido <c>Notice</c>, el error <c>Wrong</c>, el destello de racha <c>Spark(1 a 8)</c> junto a la nota (pareja permitida), el pedido perfecto <c>Perfect</c> y «NUEVO» <c>New</c>; y CINCO propios hechos con las piezas de <see cref="WoodKit"/>: la carga que llega por la esclusa
    /// (<see cref="BodegaSfx.Arrive"/>), el haz del robot (<see cref="BodegaSfx.Beam"/>: tres marimbas que suben, una kalimba no puede deslizarse), abrir una puerta (<see cref="BodegaSfx.Open"/>, mono), el vuelo con la caja (<see cref="BodegaSfx.Fly"/>: el <c>SweepUp</c> de la paleta dura 0,5 s, corto para el vuelo)
    /// y la matraca del giro (<see cref="BodegaSfx.Ratchet"/>: un bloque de madera cada 110 ms durante <see cref="BodegaMotion.SpinSeconds"/> y dos marimbas graves). Esta parte de la clase solo dice las recetas y qué pide el juego ANTES de la cuenta regresiva; el cableado del controlador es la tarea siguiente.
    /// </summary>
    public static partial class BodegaSounds
    {
        /// <summary>La receta de un sonido propio: el nivel (pico) y la sala de cada una son los de la spec; la familia declara a qué banda de sonoridad pertenece.</summary>
        public static WoodSounds.Recipe MakeRecipe(BodegaSfx sfx)
        {
            switch (sfx)
            {
                case BodegaSfx.Arrive:                                             // llega la carga por la esclusa: un soplido que baja y una campanita de kalimba aguda (sol6)
                {
                    var x = Blank(1.2);
                    Add(x, WoodKit.NoiseSweep(0.5, 1800, 500, FilterType.LowPass, 0.7, 0.004, 0.14, 21));
                    Add(x, WoodKit.Kalimba(1567.98, 0.15), 0.05, 0.6);
                    return new WoodSounds.Recipe(x, 0.14, 0.18, WoodTier.Respuesta);
                }
                case BodegaSfx.Beam:                                               // el haz del robot: tres marimbas que suben (440, 659 y 880 Hz)
                    return new WoodSounds.Recipe(Seq(new[] { 440.0, 659.25, 880.0 }, 0.05, (f, i) => WoodKit.Marimba(f, 0.10), 0.8), 0.12, 0.10, WoodTier.Textura);
                case BodegaSfx.Open:                                               // abrir una puerta: un siseo corto que sube (mono, casi sin sala)
                    return new WoodSounds.Recipe(WoodKit.NoiseSweep(0.2, 900, 2600, FilterType.BandPass, 0.8, 0.005, 0.06, 61), 0.08, 0.10, WoodTier.Micro);
                case BodegaSfx.Fly:                                                // el robot vuela con la caja: un soplido de 1 s de 300 a 1500 Hz
                    return new WoodSounds.Recipe(WoodKit.NoiseSweep(1.0, 300, 1500, FilterType.BandPass, 0.8, 0.02, 0.30, 61), 0.10, 0.10, WoodTier.Textura);
                default:                                                           // Ratchet: la matraca del giro: 14 bloques de madera cada 110 ms y dos marimbas graves debajo
                {
                    var x = Blank(BodegaMotion.SpinSeconds + 0.3);
                    for (int i = 0; i < 14; i++) Add(x, WoodKit.Woodblock(i % 2 == 0 ? 1250 : 1000), i * 0.11, 0.5);
                    Add(x, WoodKit.Marimba(196, 0.5), 0, 0.5);
                    Add(x, WoodKit.Marimba(146.83, 0.5), 0.75, 0.4);
                    return new WoodSounds.Recipe(x, 0.14, 0.12, WoodTier.Textura);
                }
            }
        }

        /// <summary>La clave de un sonido propio («bodega.ratchet»).</summary>
        public static string Key(BodegaSfx sfx) => "bodega." + sfx.ToString().ToLowerInvariant();

        /// <summary>Los micro sin sala van en mono.</summary>
        public static bool Mono(BodegaSfx sfx) => sfx == BodegaSfx.Open;

        /// <summary>Los sonidos propios con su clave y su receta: los lee la prueba de niveles y la página de escucha.</summary>
        public static IEnumerable<(string Key, WoodSounds.Recipe Recipe, bool Mono)> All()
        {
            foreach (BodegaSfx sfx in System.Enum.GetValues(typeof(BodegaSfx))) yield return (Key(sfx), MakeRecipe(sfx), Mono(sfx));
        }

        /// <summary>Lo que Bodega pide antes de la cuenta regresiva, en el orden en que suena: las 12 notas (la presentación las toca de a una), el zumbido y los propios de la presentación (<c>arrive</c>, <c>beam</c>, <c>open</c>), el destello de racha (1 a 8), lo demás de la paleta y el vuelo y la matraca.</summary>
        public static void Request()
        {
            for (int h = 0; h < 12; h++) WoodCore.RequestNote(NoteHz(h), WoodVoice.Kalimba);
            WoodCore.Request(WoodSfx.Hum);
            RequestOwn(BodegaSfx.Arrive);
            RequestOwn(BodegaSfx.Beam);
            RequestOwn(BodegaSfx.Open);
            for (int n = 1; n <= 8; n++) WoodCore.Request(WoodSfx.Spark, n);
            WoodCore.Request(WoodSfx.Clack);
            RequestOwn(BodegaSfx.Fly);
            RequestOwn(BodegaSfx.Ratchet);
            WoodCore.Request(WoodSfx.Perfect);
            WoodCore.Request(WoodSfx.New);
        }

        private static void RequestOwn(BodegaSfx sfx) => WoodCore.Request(Key(sfx), () => MakeRecipe(sfx), Mono(sfx));

        /// <summary>Suena un sonido propio (si no estaba listo no suena y queda contado).</summary>
        public static void Play(BodegaSfx sfx, float gainDb = 0f) => WoodCore.Play(Key(sfx), gainDb);
    }
}
