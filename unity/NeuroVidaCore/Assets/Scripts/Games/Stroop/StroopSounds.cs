using System.Collections.Generic;
using NeuroVida.Games.Shared;

namespace NeuroVida.Games.Stroop
{
    /// <summary>
    /// «Madera cálida» en Tinta o Palabra (Fase E; spec `auditoria-fable/spec-sonido-CDE.md` §4.1; cableada el 19-oct, Tarea 139). Tinta no tiene sonidos propios: lo único que sonaba por su cuenta eran dos señales con altura que ahora son <c>PlayCue</c> (marimba seca, casi sin sala, que empieza
    /// de inmediato) con la MISMA altura y la misma duración de hoy: la tarjeta de la regla (392 Hz, 0,14 s) y el cambio de regla (330 Hz «el color de la tinta», 440 Hz «lo que dice la palabra», 0,1 s). <b>La altura ES la regla</b> (es información: el jugador la oye antes de leerla) y el juego mide tiempos de
    /// reacción: solo cambia el timbre (el inicio es más rápido que el del tono de hoy, 0,5 contra 15 ms de ataque: la instrucción llega antes, no después). Lo demás (acierto, error, nivel, tic y el «¡Listo!» de la práctica) es de <c>GameFeel</c>, la paleta común.
    /// </summary>
    public static class StroopSounds
    {
        /// <summary>La señal de la tarjeta de la regla: 392 Hz, 0,14 s (la de hoy).</summary>
        public const double CardHz = 392.0, CardSeconds = 0.14;

        /// <summary>Las señales del cambio de regla: 330 Hz (el color de la tinta) y 440 Hz (lo que dice la palabra), 0,1 s cada una (las de hoy).</summary>
        public const double InkHz = 330.0, WordHz = 440.0, RuleSeconds = 0.1;

        /// <summary>La altura de la señal de la regla [ink]: «tinta» 330 Hz, «palabra» 440 Hz.</summary>
        public static double RuleHz(bool ink) => ink ? InkHz : WordHz;

        /// <summary>Suena la señal de la tarjeta de la regla.</summary>
        public static bool PlayCard() => WoodCore.PlayCue(CardHz, CardSeconds);

        /// <summary>Suena la señal del cambio de regla (la altura dice cuál es).</summary>
        public static bool PlayRule(bool ink) => WoodCore.PlayCue(RuleHz(ink), RuleSeconds);

        /// <summary>Lo que Tinta pide antes de la cuenta regresiva: la señal de la tarjeta (suena primero) y las dos del cambio de regla. Lo demás va con el núcleo (<c>GameFeel</c>).</summary>
        public static void Request()
        {
            WoodCore.RequestCue(CardHz, CardSeconds);
            WoodCore.RequestCue(InkHz, RuleSeconds);
            WoodCore.RequestCue(WordHz, RuleSeconds);
        }

        /// <summary>Los sonidos PROPIOS de Tinta: ninguno (las tres señales son <c>PlayCue</c> de la paleta). La prueba de niveles recorre esta lista.</summary>
        public static IEnumerable<(string Key, WoodSounds.Recipe Recipe, bool Mono)> All() { yield break; }
    }
}
