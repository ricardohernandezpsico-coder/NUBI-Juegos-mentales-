using System.Collections.Generic;
using NeuroVida.Games.Shared;

namespace NeuroVida.Games.Calculo
{
    /// <summary>
    /// «Madera cálida» en Carga exacta (Fase C; spec `auditoria-fable/spec-sonido-CDE.md` §2.1; cableada el 19-oct, Tarea 135): Carga no tiene sonidos PROPIOS ni notas con información, todo es de la paleta: elegir una celda <c>Select 2</c>, elegir una operación <c>Select 5</c>,
    /// juntar <c>Correct(paso)</c> (el par de kalimbas sube con el paso), la jugada imposible <c>Knock</c> (y «quedó X», 3 dB más bajo), deshacer <c>Unselect</c> (el primer toque de «empezar de nuevo», 6 dB más bajo), la pista de Nubi <c>Hint</c> y «la carga está exacta» <c>Perfect</c>.
    /// El juego solo pide por adelantado lo que no va ya con el núcleo común (<see cref="Request"/>); las campanas de antes (la escala <c>Penta</c> y su <c>Bell</c>) se fueron: «hoy» de la página de escucha sale CONGELADO del 17-oct.
    /// </summary>
    public static class CargaSounds
    {
        /// <summary>Lo que Carga pide antes de la cuenta regresiva, en el orden en que suena: elegir, operar, la jugada imposible, deshacer, la pista y la carga exacta (juntar, <c>Correct</c> 1 a 7, ya va con el núcleo).</summary>
        public static void Request()
        {
            WoodCore.Request(WoodSfx.Select, 2);
            WoodCore.Request(WoodSfx.Select, 5);
            WoodCore.Request(WoodSfx.Knock);
            WoodCore.Request(WoodSfx.Unselect);
            WoodCore.Request(WoodSfx.Hint);
            WoodCore.Request(WoodSfx.Perfect);
        }

        /// <summary>Los sonidos PROPIOS de Carga (ninguno): la prueba de niveles recorre esta lista.</summary>
        public static IEnumerable<(string Key, WoodSounds.Recipe Recipe, bool Mono)> All() { yield break; }
    }
}
