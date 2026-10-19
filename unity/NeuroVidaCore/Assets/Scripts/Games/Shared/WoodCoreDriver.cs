using UnityEngine;

namespace NeuroVida.Games.Shared
{
    /// <summary>El controlador oculto de <see cref="WoodCore"/>: en cada cuadro avanza unos milisegundos el calculo del núcleo común de sonido y de lo extra del juego (<see cref="WoodCore.CurrentBudgetMs"/>: 5 ms, 8 mientras corre la cuenta regresiva), así corre durante la cuenta regresiva sin trabar nada. Vive en el objeto «WoodCore», que no se destruye al recargar la escena.</summary>
    public sealed class WoodCoreDriver : MonoBehaviour
    {
        private void Update()
        {
            if (!WoodCore.Idle) WoodCore.Step(WoodCore.CurrentBudgetMs);
        }
    }
}
