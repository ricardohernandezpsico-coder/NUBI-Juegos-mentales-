using System.Collections.Generic;
using NeuroVida.Games.Shared;
using static NeuroVida.Games.Shared.SoundKit;

namespace NeuroVida.Games.Secuencia
{
    /// <summary>Los sonidos PROPIOS de Rastro de luz en «Madera cálida» (Fase D): el aviso de cada modo (el patrón de notas que lo distingue).</summary>
    public enum RastroSfx { Cue0, Cue1, Cue2, Cue3 }

    /// <summary>
    /// «Madera cálida» en Rastro de luz (Fase D; spec `auditoria-fable/spec-sonido-CDE.md` §3.1; cableada el 19-oct, Tarea 138). El camino se aprende con el oído: cada lucero suena SU nota (9, <see cref="RastroBoard.Notes"/>, 392 a 1175 Hz) en MARIMBA con una caída de
    /// <see cref="OrbTau"/> = 0,2 s (la campana de hoy caía 20 dB en 450 ms; la marimba de 0,3 s caería en 700 y en un camino rápido las notas se encimarían más que hoy): MISMA altura, mismo orden y mismo momento, solo cambia el timbre. La nota suena igual cuando la chispa llega
    /// a un lucero y cuando el jugador lo toca (un solo nivel). El resto es de la paleta: la ronda lograda <c>Correct</c> que SUBE con el largo del camino (<see cref="RoundStep"/>; decisión de Ricardo: respuesta, no hito en cada ronda), el error <c>Wrong</c>, «¡NUEVO!» <c>New</c> y el soplo
    /// del cielo que gira <c>SweepUp</c>. Y CUATRO propios (<see cref="RastroSfx"/>): el aviso de cada modo, marimbas de la misma caída con las alturas y los instantes de hoy (cuántas notas y hacia dónde van es la información: <see cref="CueHz"/> y <see cref="CueAt"/> NO cambian).
    /// </summary>
    public static class RastroSounds
    {
        /// <summary>La caída de las notas de los luceros y de los avisos: 0,2 s (20 dB en ≈ 0,45 s, lo mismo que la campana de hoy).</summary>
        public const double OrbTau = 0.2;

        /// <summary>La frecuencia de la nota del lucero [orb] (la de hoy: NO cambia).</summary>
        public static double OrbHz(int orb) => RastroBoard.Notes[orb];

        /// <summary>Suena la nota del lucero [orb] (marimba, misma altura de siempre): cuando la chispa llega y cuando el jugador lo toca.</summary>
        public static bool PlayOrb(int orb) => WoodCore.PlayNote(OrbHz(orb), WoodVoice.Marimba, OrbTau);

        /// <summary>Qué escalón sube la ronda lograda: el par de <c>Correct</c> sube con el largo del camino (Rastro no tiene racha en el marcador). Un camino de 3 luceros es el escalón 1; de 9, el 7 (el tope de la paleta).</summary>
        public static int RoundStep(int asked) => System.Math.Max(1, System.Math.Min(7, asked - 2));

        /// <summary>Qué propio avisa cada modo (el rastro, al revés, el cielo gira y en marcha).</summary>
        public static RastroSfx CueOf(RastroMode mode)
        {
            switch (mode)
            {
                case RastroMode.Reves: return RastroSfx.Cue1;
                case RastroMode.Gira: return RastroSfx.Cue2;
                case RastroMode.Marcha: return RastroSfx.Cue3;
                default: return RastroSfx.Cue0;
            }
        }

        /// <summary>Las alturas de cada aviso (las de hoy): el rastro una nota, al revés dos que bajan, el cielo gira tres (sube y baja: un arco) y en marcha tres que suben rápido.</summary>
        public static readonly double[][] CueHz =
        {
            new[] { 659.25 },
            new[] { 880.0, 659.25 },
            new[] { 659.25, 880.0, 659.25 },
            new[] { 523.25, 659.25, 783.99 },
        };

        /// <summary>Los instantes de cada nota de cada aviso, en segundos (los de hoy: 0,11 / 0,12 / 0,09 s entre una y otra).</summary>
        public static readonly double[][] CueAt =
        {
            new[] { 0.0 },
            new[] { 0.0, 0.11 },
            new[] { 0.0, 0.12, 0.24 },
            new[] { 0.0, 0.09, 0.18 },
        };

        /// <summary>La receta de un aviso de modo: marimbas de <see cref="OrbTau"/> con las alturas y los instantes de <see cref="CueHz"/> y <see cref="CueAt"/>; nivel 0,20, sala 0,18, familia «respuesta».</summary>
        public static WoodSounds.Recipe MakeRecipe(RastroSfx sfx)
        {
            int k = (int)sfx;
            var x = Blank(1.0);
            for (int i = 0; i < CueHz[k].Length; i++) Add(x, WoodKit.Marimba(CueHz[k][i], OrbTau), CueAt[k][i]);
            return new WoodSounds.Recipe(x, 0.20, 0.18, WoodTier.Respuesta);
        }

        /// <summary>La clave de un sonido propio («rastro.cue2»).</summary>
        public static string Key(RastroSfx sfx) => "rastro." + sfx.ToString().ToLowerInvariant();

        /// <summary>Los avisos son señales cortas con altura, pero van con la sala de la familia (no son micro): estéreo.</summary>
        public static bool Mono(RastroSfx sfx) => false;

        /// <summary>Los sonidos propios con su clave y su receta: los lee la prueba de niveles y la página de escucha.</summary>
        public static IEnumerable<(string Key, WoodSounds.Recipe Recipe, bool Mono)> All()
        {
            foreach (RastroSfx sfx in System.Enum.GetValues(typeof(RastroSfx))) yield return (Key(sfx), MakeRecipe(sfx), Mono(sfx));
        }

        /// <summary>Suena el aviso del modo [mode].</summary>
        public static bool PlayCue(RastroMode mode) => WoodCore.Play(Key(CueOf(mode)));

        /// <summary>Lo que Rastro pide antes de la cuenta regresiva, en el orden en que suena: las 9 notas de los luceros (la muestra del primer camino), los avisos de modo, el soplo del giro y «NUEVO». <c>Correct 1 a 7</c> y <c>Wrong</c> ya van con el núcleo.</summary>
        public static void Request()
        {
            for (int i = 0; i < RastroBoard.Notes.Length; i++) WoodCore.RequestNote(OrbHz(i), WoodVoice.Marimba, OrbTau);
            foreach (RastroSfx sfx in System.Enum.GetValues(typeof(RastroSfx))) { var s = sfx; WoodCore.Request(Key(s), () => MakeRecipe(s), Mono(s)); }
            WoodCore.Request(WoodSfx.SweepUp);
            WoodCore.Request(WoodSfx.New);
        }
    }
}
