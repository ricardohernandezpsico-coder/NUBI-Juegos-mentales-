using UnityEngine;

namespace NeuroVida.Games.Shared
{
    /// <summary>
    /// Reloj de juego que se puede PAUSAR. Los juegos medían todo con el reloj real de Unity
    /// (<c>Time.unscaledTime</c>/<c>unscaledDeltaTime</c>), que no se detiene nunca: pausar la partida no
    /// congelaba los relojes de ronda ni los tiempos de respuesta. Ahora usan <see cref="Time"/> y
    /// <see cref="DeltaTime"/>: en pausa el tiempo no avanza (y al reanudar se descuenta lo que duró la pausa),
    /// <c>Time.timeScale</c> = 0 congela los <c>WaitForSeconds</c> y el audio se detiene (la pausa del MENÚ). La congelación que pone «Nubi entrenadora» en un paso de toque (Tarea 64) detiene el reloj pero NO calla el audio:
    /// un sonido que estaba sonando cuando aparece Nubi termina completo; los sonidos en bucle bajan a <see cref="FrozenLoopVolume"/> mientras dura y vuelven al soltar.
    /// El fondo animado (estrellas, mundos) sigue usando el reloj real a propósito: la pausa no se ve "colgada".
    /// </summary>
    public static class GameClock
    {
        private static float s_pausedTotal;
        private static float s_pausedAt = -1f;

        private static bool s_audioSilenced;

        public static bool Paused => s_pausedAt >= 0f;

        /// <summary>true mientras el juego está detenido Y el audio callado (la pausa del menú). Una congelación de Nubi es <see cref="Paused"/> pero no silenciada.</summary>
        public static bool AudioSilenced => Paused && s_audioSilenced;

        /// <summary>Cuánto bajan los sonidos en bucle (el motor de Piloto, por ejemplo) mientras Nubi congela el juego: no se cortan, quedan al 30 %.</summary>
        public const float FrozenLoopVolume = 0.3f;

        /// <summary>El factor de volumen de un sonido en bucle: 1 normalmente y 0,3 con el juego congelado solo por Nubi. (Con la pausa del menú el audio ya está callado.)</summary>
        public static float LoopVolume => Paused && !s_audioSilenced ? FrozenLoopVolume : 1f;

        /// <summary>Segundos de juego (sin contar las pausas).</summary>
        public static float Time => (Paused ? s_pausedAt : UnityEngine.Time.unscaledTime) - s_pausedTotal;

        /// <summary>SOLO PARA PRUEBAS EditMode (donde no hay cuadros reales): si es mayor que 0, es la duración de cada cuadro.
        /// Dejarlo en 0 al terminar la prueba.</summary>
        public static float SimulatedDeltaTime;

        /// <summary>Duración del cuadro con el reloj REAL (no se detiene con la pausa): para cierres y fondos que siguen vivos.</summary>
        public static float RealDeltaTime => SimulatedDeltaTime > 0f ? SimulatedDeltaTime : UnityEngine.Time.unscaledDeltaTime;

        /// <summary>Duración del cuadro en segundos de juego (0 en pausa).</summary>
        public static float DeltaTime => Paused ? 0f : (SimulatedDeltaTime > 0f ? SimulatedDeltaTime : UnityEngine.Time.unscaledDeltaTime);

        /// <summary>
        /// Detiene el reloj de juego. <paramref name="silenceAudio"/> true (la pausa del menú, por omisión): además calla todo el audio. false («Nubi entrenadora»): el reloj se detiene pero los sonidos que estaban sonando siguen hasta el final.
        /// Si el juego ya estaba detenido sin silencio (Nubi) y llega una pausa del menú, el audio se calla igual.
        /// </summary>
        public static void Pause(bool silenceAudio = true)
        {
            if (Paused)
            {
                if (silenceAudio && !s_audioSilenced)
                {
                    s_audioSilenced = true;
                    AudioListener.pause = true;
                }
                return;
            }
            s_pausedAt = UnityEngine.Time.unscaledTime;
            UnityEngine.Time.timeScale = 0f;
            s_audioSilenced = silenceAudio;
            AudioListener.pause = silenceAudio;
        }

        public static void Resume()
        {
            if (!Paused) return;
            s_pausedTotal += UnityEngine.Time.unscaledTime - s_pausedAt;
            s_pausedAt = -1f;
            s_audioSilenced = false;
            UnityEngine.Time.timeScale = 1f;
            AudioListener.pause = false;
        }

        /// <summary>Estado limpio para una partida nueva (cada carga de escena).</summary>
        public static void Reset()
        {
            s_pausedTotal = 0f;
            s_pausedAt = -1f;
            s_audioSilenced = false;
            UnityEngine.Time.timeScale = 1f;
            AudioListener.pause = false;
        }
    }
}
