using UnityEngine;
using UnityEngine.EventSystems;

namespace NeuroVida.Games.Shared
{
    /// <summary>
    /// Asegura que haya un <see cref="EventSystem"/> en la escena. Los botones de arcilla (el menú de pausa, «¡Listo!» y los de cada juego) usan
    /// <c>GraphicRaycaster</c> y NO responden sin él. Antes cada controlador lo creaba por su cuenta y cuatro (En la punta de la lengua, Cosecha, ¿Verdad o disparate? y La
    /// estrella intrusa, que leen <c>Input</c> directo) no: si uno de esos era el primero tras recargar la escena, la pausa y «¡Listo!» quedaban muertos (3-oct).
    /// Lo llaman <see cref="GameControllerBase"/> al nacer (cubre a todos los juegos y a sus botones) y <see cref="PauseMenu.Create"/> (el que más lo necesita, venga de donde venga).
    /// </summary>
    public static class EventSystemGuard
    {
        public static void Ensure()
        {
            if (EventSystem.current != null || Object.FindObjectOfType<EventSystem>() != null) return;
            var go = new GameObject("EventSystem");
            go.AddComponent<EventSystem>();
            go.AddComponent<StandaloneInputModule>();
        }
    }
}
