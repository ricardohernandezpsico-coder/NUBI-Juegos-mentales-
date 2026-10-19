using System.Collections.Generic;
using NeuroVida.Games.Shared;

namespace NeuroVida.Games.Freno
{
    /// <summary>
    /// «Madera cálida» en Freno de emergencia (Fase E; spec `auditoria-fable/spec-sonido-CDE.md` §4.3; cableada el 19-oct, Tarea 139). La SEÑAL de salida (el cohete que se enciende) es el ESTÍMULO: el tiempo de reacción se mide desde que se enciende la luz y suena este tono, así que pasa a <c>PlayCue</c> (marimba seca, casi sin sala)
    /// con la MISMA altura (784 Hz, ±1 %), la misma duración (60 ms) y el mismo nivel de hoy (<see cref="ExitGainDb"/> = −6 dB), y el inicio es más rápido (0,5 ms contra 15 ms de ataque del tono de hoy). Por eso el juego la pide PRIMERO en <see cref="Request"/>: nunca puede esperar a calcularse.
    /// «¡Más rápido!» es <c>Knock</c> (el toc hueco de «eso no se puede»; hoy casi no se oía). Se QUITARON el rugido del despegue y el resoplido del freno perfecto (ya suena el acierto: dos sonidos por una respuesta es lo que la paleta evita). <b>La sirena de «¡ALTO!» se queda tal cual</b> (988-740-988 Hz con
    /// <c>PlayTone</c>, decisión de Ricardo): es el único uso vivo de <c>PlayTone</c>/<c>HarmonicTone</c> y no tiene nada que ver con este archivo. Lo demás (acierto, error, nivel, tic, «¡Listo!» de la práctica) es de <c>GameFeel</c>, la paleta común.
    /// </summary>
    public static class BrakeSounds
    {
        /// <summary>La señal de salida: 784 Hz, 60 ms (la de hoy).</summary>
        public const double ExitHz = 784.0, ExitSeconds = 0.06;

        /// <summary>Mismo nivel que hoy: −6 dB respecto de la señal normal.</summary>
        public const float ExitGainDb = -6f;

        /// <summary>Suena la señal de salida (el estímulo del tiempo de reacción): nunca espera a nada.</summary>
        public static bool PlayExit() => WoodCore.PlayCue(ExitHz, ExitSeconds, ExitGainDb);

        /// <summary>Lo que Freno pide antes de la cuenta regresiva, en el orden en que suena: la señal de salida (el estímulo: PRIMERO) y el «¡Más rápido!» (<c>Knock</c>). Lo demás va con el núcleo (<c>GameFeel</c>).</summary>
        public static void Request()
        {
            WoodCore.RequestCue(ExitHz, ExitSeconds);
            WoodCore.Request(WoodSfx.Knock);
        }

        /// <summary>Los sonidos PROPIOS de Freno: ninguno (la señal es <c>PlayCue</c> de la paleta; la sirena sigue siendo <c>PlayTone</c>). La prueba de niveles recorre esta lista.</summary>
        public static IEnumerable<(string Key, WoodSounds.Recipe Recipe, bool Mono)> All() { yield break; }
    }
}
