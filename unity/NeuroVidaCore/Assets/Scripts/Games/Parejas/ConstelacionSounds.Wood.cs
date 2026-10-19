using System.Collections.Generic;
using NeuroVida.Games.Shared;

namespace NeuroVida.Games.Parejas
{
    /// <summary>
    /// «Madera cálida» en Constelaciones (Fase C; spec `auditoria-fable/spec-sonido-CDE.md` §2.3): cada grupo suena SU nota (11, la 11.ª una octava arriba: <see cref="ConstelacionContract.NoteHz"/>) con kalimba; dar vuelta una luz <c>Tap</c> (por el altavoz del juego), cerrarla <c>Unselect</c>, «no son iguales» <c>Knock</c>,
    /// grupo encontrado <c>Correct(racha)</c> (sin el destello <c>Spark</c>: el par de <c>Correct</c> ya sube con la racha; decisión 1 de §8, Ricardo la aprueba de oído), cielo completo <c>Perfect</c>, empieza el cielo <c>Hum</c>, tarjeta «NUEVO» <c>New</c> y las luces hermanas (la pista) <c>Hint</c>.
    /// Esta parte de la clase solo dice qué pide el juego ANTES de la cuenta regresiva; el cableado es la tarea siguiente.
    /// </summary>
    public static partial class ConstelacionSounds
    {
        /// <summary>Lo que Constelaciones pide antes de la cuenta regresiva: las 11 notas (suenan al abrir la primera luz), después el zumbido, cerrar, «no son iguales», la pista, el cielo completo y «NUEVO». Tap y Correct 1 a 7 ya van con el núcleo.</summary>
        public static void Request()
        {
            for (int g = 0; g < 11; g++) WoodCore.RequestNote(ConstelacionContract.NoteHz(g), WoodVoice.Kalimba);
            WoodCore.Request(WoodSfx.Hum);
            WoodCore.Request(WoodSfx.Unselect);
            WoodCore.Request(WoodSfx.Knock);
            WoodCore.Request(WoodSfx.Hint);
            WoodCore.Request(WoodSfx.Perfect);
            WoodCore.Request(WoodSfx.New);
        }

        /// <summary>Los sonidos PROPIOS de Constelaciones (ninguno): la prueba de niveles recorre esta lista.</summary>
        public static IEnumerable<(string Key, WoodSounds.Recipe Recipe, bool Mono)> All() { yield break; }
    }
}
