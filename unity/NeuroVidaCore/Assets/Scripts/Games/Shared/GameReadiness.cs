using System;
using UnityEngine;
using NeuroVida.Bridge;

namespace NeuroVida.Games.Shared
{
    /// <summary>
    /// «El tablero está completo» (Tarea 190, 28-oct). La pantalla de carga de la app (planeta, nombre y etapa) tapa Unity desde que la partida se pide hasta que Unity avisa que lo que se va a ver YA está terminado: la escena armada, los atlas y los sprites horneados, los sonidos de la partida calculados y
    /// el primer ensayo a punto. Antes la capa se quitaba 3 cuadros después de recibir la configuración y lo que faltaba se terminaba detrás de la cuenta regresiva (cuadros de 100 ms a 3 s); ahora la persona ve el primer cuadro con el tablero entero, y de ahí en adelante nada pesado corre en el hilo principal.
    /// <para><see cref="Report"/> lo llama <see cref="GameControllerBase.ShowBoardWhenReady"/> una sola vez por partida: avisa a Android (<see cref="NativeBridge.NotifyGameShown"/>) y deja las huellas que leen las pruebas y el smoke (<see cref="Shown"/>, <see cref="ShownAt"/>).</para>
    /// </summary>
    public static class GameReadiness
    {
        /// <summary>Lo que tarda la pantalla de carga de Android en irse (fundido de 250 ms; <c>UnityLoadingOverlay</c>) con un margen: el «¡Ya!» o el tutorial no empiezan debajo de ella.</summary>
        public const float CoverFadeSeconds = 0.3f;

        /// <summary>El tope de lo que se espera a que el sonido termine de calcularse antes de mostrar el tablero (si algo nunca termina, el juego arranca igual).</summary>
        public const float MaxWaitSeconds = 30f;

        /// <summary>true desde que el tablero de ESTA partida se mostró (se apaga al llegar la configuración de una partida nueva).</summary>
        public static bool Shown { get; private set; }

        /// <summary>El reloj real (<see cref="Time.realtimeSinceStartup"/>) en que se mostró; -1 si todavía no.</summary>
        public static float ShownAt { get; private set; } = -1f;

        /// <summary>Cuántas veces se mostró un tablero desde que arrancó el proceso (las pruebas comprueban que sea una por partida).</summary>
        public static int ShownCount { get; private set; }

        /// <summary>Lo que corre justo antes de avisar a Android (las pruebas toman aquí la foto del tablero: «el primer cuadro visible ya está completo»).</summary>
        public static event Action BeforeReport;

        /// <summary>Una partida nueva: el tablero todavía no se mostró (lo llama <c>GameEntryPoint.InitializeGameConfig</c>).</summary>
        public static void Reset()
        {
            Shown = false;
            ShownAt = -1f;
        }

        /// <summary>El tablero ya está completo: avisa a Android para que quite la pantalla de carga (con un fundido) y deja la huella.</summary>
        public static void Report()
        {
            BeforeReport?.Invoke();
            Shown = true;
            ShownAt = Time.realtimeSinceStartup;
            ShownCount++;
            EntryProbe.Shown();
            NativeBridge.NotifyGameShown();
        }
    }
}
