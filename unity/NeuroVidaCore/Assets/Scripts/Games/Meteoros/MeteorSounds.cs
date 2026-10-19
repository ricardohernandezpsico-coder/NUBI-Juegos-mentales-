using System.Collections.Generic;
using NeuroVida.Games.Shared;

namespace NeuroVida.Games.Meteoros
{
    /// <summary>
    /// «Madera cálida» en Lluvia de meteoros (Fase E; spec `auditoria-fable/spec-sonido-CDE.md` §4.2; cableada el 19-oct, Tarea 139). Meteoros no tiene sonidos propios: lo único que sonaba por su cuenta era el tono de 330 Hz (0,12 s, muy suave) de «dejar pasar una palabra inventada» (el acierto, sin ruido),
    /// que NO se suma a ningún acierto común (es el único sonido de ese acierto: el plan lo creía doblado y proponía quitarlo; no lo está, se queda). Ahora es <c>PlayCue</c> con la misma altura y duración y <see cref="LetPassGainDb"/> = −9 dB, igual de discreto. Lo demás (rescatar una palabra, error, nivel, tic,
    /// el «¡Listo!» de la práctica) es de <c>GameFeel</c>, la paleta común.
    /// </summary>
    public static class MeteorSounds
    {
        /// <summary>La señal de «dejar pasar una inventada»: 330 Hz, 0,12 s (la de hoy).</summary>
        public const double LetPassHz = 330.0, LetPassSeconds = 0.12;

        /// <summary>Cuánto más suave que la señal normal: −9 dB (hoy sonaba a amplitud 0,03: casi un roce).</summary>
        public const float LetPassGainDb = -9f;

        /// <summary>Suena el acierto sin ruido de dejar pasar una inventada (suave).</summary>
        public static bool PlayLetPass() => WoodCore.PlayCue(LetPassHz, LetPassSeconds, LetPassGainDb);

        /// <summary>Lo que Meteoros pide antes de la cuenta regresiva: la señal de dejar pasar una inventada. Lo demás va con el núcleo (<c>GameFeel</c>).</summary>
        public static void Request() => WoodCore.RequestCue(LetPassHz, LetPassSeconds);

        /// <summary>Los sonidos PROPIOS de Meteoros: ninguno (la señal es <c>PlayCue</c> de la paleta). La prueba de niveles recorre esta lista.</summary>
        public static IEnumerable<(string Key, WoodSounds.Recipe Recipe, bool Mono)> All() { yield break; }
    }
}
