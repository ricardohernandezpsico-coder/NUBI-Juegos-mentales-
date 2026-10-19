using System.Collections.Generic;
using NeuroVida.Games.Shared;

namespace NeuroVida.Games.Anagramas
{
    /// <summary>
    /// «Madera cálida» en En la punta de la lengua (Fase C; spec `auditoria-fable/spec-sonido-CDE.md` §2.2): cada ficha que se coloca suena la nota de su casilla (las 10 de <see cref="Penta"/>, do5 a la6, con información: el orden en que se arma la palabra) con kalimba y la caída de siempre;
    /// el resto es de la paleta (aparecen las fichas <c>SweepUp</c>, una ficha vuelve al banco <c>Unselect</c>, «casi» <c>Wrong</c>, una ayuda <c>Hint</c>, la palabra salió <c>Perfect</c>, el lucero llega al cielo <c>New</c> 3 dB más bajo). Los retardos de la cascada final (k · 0,09 s) no se tocan.
    /// Esta parte de la clase solo dice qué pide el juego ANTES de la cuenta regresiva y su lista de propios (ninguno); el cableado es la tarea siguiente.
    /// </summary>
    public static partial class PuntaSounds
    {
        /// <summary>Lo que Punta pide antes de la cuenta regresiva: primero las notas de las casillas (suenan desde la primera ficha), después lo demás en el orden en que aparece.</summary>
        public static void Request()
        {
            for (int k = 0; k < Penta.Length; k++) WoodCore.RequestNote(Penta[k], WoodVoice.Kalimba);
            WoodCore.Request(WoodSfx.SweepUp);
            WoodCore.Request(WoodSfx.Unselect);
            WoodCore.Request(WoodSfx.Hint);
            WoodCore.Request(WoodSfx.Perfect);
            WoodCore.Request(WoodSfx.New);
        }

        /// <summary>Los sonidos PROPIOS de Punta (ninguno): la prueba de niveles recorre esta lista.</summary>
        public static IEnumerable<(string Key, WoodSounds.Recipe Recipe, bool Mono)> All() { yield break; }
    }
}
