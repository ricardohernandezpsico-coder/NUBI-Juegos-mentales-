using System.Collections.Generic;
using NeuroVida.Games.Shared;

namespace NeuroVida.Games.Calculo
{
    /// <summary>
    /// «Madera cálida» en Carga exacta (Fase C; spec `auditoria-fable/spec-sonido-CDE.md` §2.1): Carga no tiene sonidos PROPIOS ni notas con información, todo es de la paleta (elegir una celda <c>Select 2</c>, elegir una operación <c>Select 5</c>, juntar <c>Correct(paso)</c>, jugada imposible <c>Knock</c>,
    /// deshacer <c>Unselect</c>, la pista <c>Hint</c> y «la carga está exacta» <c>Perfect</c>). Esta parte de la clase solo dice qué pide el juego ANTES de la cuenta regresiva; el cableado del controlador (borrar las campanas de arriba) es la tarea siguiente, con la página de escucha aprobada.
    /// </summary>
    public static partial class CargaSounds
    {
        /// <summary>Lo que Carga pide antes de la cuenta regresiva, en el orden en que suena: elegir, operar, juntar (Correct 1 a 7 ya va con el núcleo), la jugada imposible, deshacer, la pista y la carga exacta.</summary>
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
