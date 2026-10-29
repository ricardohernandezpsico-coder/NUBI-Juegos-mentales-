using UnityEngine;

namespace NeuroVida.Games.Shared
{
    /// <summary>
    /// "Sensación" común de todos los juegos: el mismo sonido y la misma vibración para acertar, fallar, subir de nivel, el tic del reloj y el final de la partida, y (desde la Fase B, Tarea 120) para la cuenta regresiva, «¡Ya!», abrir y seguir la pausa, los avisos y el toque de un botón. Antes cada juego tocaba sus
    /// propios tonos sueltos y ninguno vibraba; ahora acertar se siente igual de bien en todos (criterio de ui-ux-pro-max: "motion/feedback consistency").
    ///
    /// Los sonidos son los de la paleta «Madera cálida» (<see cref="WoodSounds"/>: kalimba, marimba y bloque de madera con una sala pequeña; ver docs/sonido-madera-plan.md), sintetizados por código y guardados por <see cref="WoodCore"/> (se calculan UNA vez, de a poco, durante la cuenta regresiva):
    /// - Acierto: kalimba de dos notas que suben; el par sube por la pentatónica con la racha (1 a 7): la racha se OYE crecer.
    /// - Error: dos marimbas graves y suaves que bajan (sin chicharra ni castigo). Subir de nivel: arpegio de kalimba. Tic: bloque de madera. Final: acorde de marimba y kalimba larga, UNO por partida.
    /// - Cuenta regresiva: una marimba que sube un escalón por número (3, 2, 1) y «¡Ya!». Pausa: dos marimbas que bajan al abrir y suben al seguir. Aviso: una marimba sola. Toque: un golpecito de madera apagado.
    ///
    /// Vibración (regla haptic-feedback: solo confirmaciones importantes, nunca en cada toque): error (doble toque leve), racha múltiplo de 5, subir de nivel y final (golpe firme). Respeta los ajustes de sonido y
    /// vibración de la app (<see cref="SoundOn"/>/<see cref="HapticsOn"/>, los fija GameEntryPoint con la config); con el sonido apagado no se calcula ni suena nada. Los sonidos del juego se callan con la pausa (<c>AudioListener.pause</c>); los de la interfaz (toque de botón, abrir y seguir la pausa) no.
    /// </summary>
    public static class GameFeel
    {
        public static bool SoundOn = true;
        public static bool HapticsOn = true;
        /// <summary>"Quitar animaciones" del teléfono (lo manda la app): sin adornos que se mueven solos
        /// (respiración, pulsos, destellos). La mecánica del juego no cambia.</summary>
        public static bool ReduceMotion;

        /// <summary>Lo más corto que puede pasar entre dos toques de botón con sonido (s, reloj real): un golpeteo rápido no se vuelve una ametralladora.</summary>
        public const float TapGap = 0.05f;

        private static float _lastTap = -100f;

        /// <summary>SOLO PARA LAS PRUEBAS: olvida el último toque de botón (para que el límite de <see cref="TapGap"/> no pase de una prueba a otra).</summary>
        public static void ResetTap() => _lastTap = -100f;

        // ------------------------------------------------------------------ API

        /// <summary>
        /// Prepara el sonido de la partida (<see cref="WoodCore.Warm"/>): calcula lo de la cuenta regresiva y deja el resto del núcleo común para que se calcule de a poco. Lo llama <c>GameEntryPoint</c> en cuanto llega la configuración (ya con <see cref="SoundOn"/>).
        /// </summary>
        public static void Warm(string gameId = null) => WoodCore.Warm(gameId);

        /// <summary>Respuesta correcta. <paramref name="streak"/> = racha actual (1 = primer acierto).</summary>
        public static void Correct(int streak)
        {
            WoodCore.Play(WoodSfx.Correct, streak);
            if (streak > 0 && streak % 5 == 0) Haptic(HapticKind.Light);
        }

        /// <summary>Respuesta incorrecta: suave, sin castigo.</summary>
        public static void Wrong()
        {
            WoodCore.Play(WoodSfx.Wrong);
            Haptic(HapticKind.Double);
        }

        /// <summary>Sube el nivel dentro de la partida.</summary>
        public static void LevelUp()
        {
            WoodCore.Play(WoodSfx.LevelUp);
            Haptic(HapticKind.Firm);
        }

        /// <summary>Último tramo del reloj (los 5 segundos finales).</summary>
        public static void Tick() => WoodCore.Play(WoodSfx.Tick);

        /// <summary>Fin de la partida: el ÚNICO sonido de final de toda la app (lo suena <c>ExitButton</c> al abrir el cierre; ningún juego suena uno propio).</summary>
        public static void Finish()
        {
            if (WoodCore.Play(WoodSfx.Finish)) WoodCore.CountFinish();
            Haptic(HapticKind.Firm);
        }

        /// <summary>Un aviso (por ejemplo, el «¡Listo!» de una práctica): una marimba sola. El aviso en pantalla (<see cref="Toast"/>) la suena solo si nada más sonó con él.</summary>
        public static void Notice() => WoodCore.Play(WoodSfx.Notice);

        /// <summary>El toque de un botón del kit: un golpecito de madera apagado, por el altavoz de la interfaz (suena aunque el audio esté en pausa). No más de uno cada <see cref="TapGap"/> s.</summary>
        public static void Tap()
        {
            float now = Time.realtimeSinceStartup;
            if (now - _lastTap < TapGap) return;
            _lastTap = now;
            WoodCore.Play(WoodSfx.Tap, 0, ui: true);
            Haptic(HapticKind.Light);                                                  // el tic del toque (Tarea 123): con el mismo ajuste de vibración de la app, suene o no el golpecito (Fable §3.5)
        }

        /// <summary>El número de la cuenta regresiva (3, 2 o 1): una marimba que sube un escalón por número.</summary>

        /// <summary>«¡Ya!»: la salida de la cuenta regresiva.</summary>
        public static void Go() => WoodCore.Play(WoodSfx.Go);

        /// <summary>Se abre la pausa (por el altavoz de la interfaz: suena aunque el audio se congele).</summary>
        public static void PauseIn() => WoodCore.Play(WoodSfx.PauseIn, 0, ui: true);

        /// <summary>Se sigue jugando después de la pausa.</summary>
        public static void PauseOut() => WoodCore.Play(WoodSfx.PauseOut, 0, ui: true);

        // ------------------------------------------------------------------ vibración

        public enum HapticKind { Light, Double, Firm }

        public static void Haptic(HapticKind kind)
        {
            if (!HapticsOn) return;
#if UNITY_ANDROID && !UNITY_EDITOR
            try
            {
                EnsureVibrator();
                if (_vibrator == null) return;
                // Efectos predefinidos del sistema (API 29+): se sienten "nativos" y los ajusta el fabricante.
                // EFFECT_TICK = 2, EFFECT_DOUBLE_CLICK = 1, EFFECT_HEAVY_CLICK = 5.
                if (_sdk >= 29)
                {
                    int effect = kind == HapticKind.Light ? 2 : kind == HapticKind.Double ? 1 : 5;
                    using (var ve = new AndroidJavaClass("android.os.VibrationEffect"))
                    using (var e = ve.CallStatic<AndroidJavaObject>("createPredefined", effect))
                        _vibrator.Call("vibrate", e);
                }
                else if (_sdk >= 26)
                {
                    long ms = kind == HapticKind.Light ? 12 : kind == HapticKind.Double ? 22 : 35;
                    int amp = kind == HapticKind.Light ? 60 : kind == HapticKind.Double ? 110 : 200;
                    using (var ve = new AndroidJavaClass("android.os.VibrationEffect"))
                    using (var e = ve.CallStatic<AndroidJavaObject>("createOneShot", ms, amp))
                        _vibrator.Call("vibrate", e);
                }
                else
                {
                    _vibrator.Call("vibrate", kind == HapticKind.Light ? 12L : 30L);
                }
            }
            catch (System.Exception e)
            {
                Debug.LogWarning("[GameFeel] Sin vibración: " + e.Message);
                HapticsOn = false; // no insistir en cada acierto si el equipo no la soporta
            }
#endif
        }

#if UNITY_ANDROID && !UNITY_EDITOR
        private static AndroidJavaObject _vibrator;
        private static int _sdk;

        private static void EnsureVibrator()
        {
            if (_vibrator != null) return;
            using (var version = new AndroidJavaClass("android.os.Build$VERSION"))
                _sdk = version.GetStatic<int>("SDK_INT");
            using (var player = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
            using (var activity = player.GetStatic<AndroidJavaObject>("currentActivity"))
                _vibrator = activity.Call<AndroidJavaObject>("getSystemService", "vibrator");
        }
#endif
    }
}
