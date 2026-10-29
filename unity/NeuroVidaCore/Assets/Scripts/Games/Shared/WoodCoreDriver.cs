using UnityEngine;

namespace NeuroVida.Games.Shared
{
    /// <summary>El controlador oculto de <see cref="WoodCore"/>: en cada cuadro avanza unos milisegundos el calculo del núcleo común de sonido y de lo extra del juego (<see cref="WoodCore.CurrentBudgetMs"/>): reparte el trabajo a los hilos y crea los clips que terminaron (Tarea 190: 2 ms por cuadro; 30 ms mientras la pantalla de carga tapa el juego). Vive en el objeto «WoodCore», que no se destruye al recargar la escena.</summary>
    public sealed class WoodCoreDriver : MonoBehaviour
    {
        private void Update()
        {
            if (!WoodCore.Idle) WoodCore.Step(WoodCore.CurrentBudgetMs);
        }
    }
}
