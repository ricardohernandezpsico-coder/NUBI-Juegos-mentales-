using System.Diagnostics;
using Debug = UnityEngine.Debug;

namespace NeuroVida.Games.Shared
{
    /// <summary>
    /// Guardias de ESTADO de los tutoriales guiados (Tarea 67, 10-oct): el smoke de un tutorial no solo mira que los pasos avancen, mira que el juego quede como el paso lo pide (marcado, encendido, colocado) y que la práctica termine como la de quien siguió las instrucciones. Si no, escribe un
    /// error de consola y el smoke falla. Nació de Rescate relámpago, donde el toque de un paso llegaba al juego (que marcaba la cápsula) y después la práctica la ALTERNABA y la desmarcaba: «Rescataste 0 de 2» con las instrucciones seguidas al pie de la letra. Dos reglas (docs/tutoriales-con-nubi.md):
    /// la práctica ASEGURA un estado (si ya está, no hace nada), nunca lo alterna; y el smoke mira el estado. Las llamadas desaparecen del código fuera del Editor (<see cref="ConditionalAttribute"/>: ni siquiera se evalúan los argumentos).
    /// </summary>
    public static class TutorialGuards
    {
        /// <summary>Si <paramref name="ok"/> es false, un error de consola con el juego y lo que no quedó como el paso pedía (solo en el Editor).</summary>
        [Conditional("UNITY_EDITOR")]
        public static void Expect(bool ok, string game, string what)
        {
            if (!ok) Debug.LogError("[SmokeTest] " + game + ", tutorial: " + what);
        }
    }
}
