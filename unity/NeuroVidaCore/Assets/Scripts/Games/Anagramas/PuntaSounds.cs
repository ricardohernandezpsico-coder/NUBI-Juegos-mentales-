using System.Collections.Generic;
using NeuroVida.Games.Shared;

namespace NeuroVida.Games.Anagramas
{
    /// <summary>
    /// «Madera cálida» en En la punta de la lengua (Fase C; spec `auditoria-fable/spec-sonido-CDE.md` §2.2; cableada el 19-oct, Tarea 135): cada ficha que se coloca suena la nota de su casilla (las 10 de <see cref="Penta"/>, do5 a la6, con información: el orden en que se arma la palabra) con kalimba
    /// y la caída de siempre (<c>WoodCore.PlayNote</c>); el resto es de la paleta (aparecen las fichas <c>SweepUp</c>, una ficha vuelve al banco <c>Unselect</c>, «casi» <c>Wrong</c>, una ayuda <c>Hint</c>, la palabra salió <c>Perfect</c>, el lucero llega al cielo <c>New</c> 3 dB más bajo).
    /// Los retardos de la cascada final (k · 0,09 s: <c>LightUp</c> y <c>ChordAfter</c>) no se tocan. Ricardo escuchó la cascada de 7 notas (18-oct) y quedó como está, sin bajarla. Las campanas de antes se fueron: «hoy» de la página de escucha sale congelado del 17-oct.
    /// </summary>
    public static class PuntaSounds
    {
        /// <summary>Las 10 alturas de las casillas (pentatónica de do, do5 a la6): <c>Penta[k]</c> es la nota de la casilla k. También es la escala de referencia de las familias de la página de escucha (La estrella intrusa usa la misma).</summary>
        public static readonly float[] Penta = { 523.25f, 587.33f, 659.25f, 783.99f, 880f, 1046.5f, 1174.66f, 1318.5f, 1567.98f, 1760f };

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
