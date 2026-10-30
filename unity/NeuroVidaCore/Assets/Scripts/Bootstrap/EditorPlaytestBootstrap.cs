using UnityEngine;
using NeuroVida.Contracts;
using NeuroVida.Games.Secuencia;

namespace NeuroVida.Bridge
{
    /// <summary>
    /// Simula la llamada que en el dispositivo real hace la app nativa vía
    /// <c>UnityPlayer.UnitySendMessage</c> — para poder jugar el piloto de "Secuencia
    /// Lumínica" apretando Play en el Editor, sin necesitar todavía la app Android
    /// embebiendo Unity (eso es un paso posterior de la Fase 1, ver roadmap en
    /// NeuroVida/CLAUDE.md).
    ///
    /// Colocar en un GameObject vacío de la escena de prueba junto con
    /// <see cref="GameEntryPoint"/>. Ajustar los valores del Inspector para probar
    /// distintos niveles/perfiles de edad sin tocar código.
    /// </summary>
    public class EditorPlaytestBootstrap : MonoBehaviour
    {
        [SerializeField] private GameEntryPoint gameEntryPoint;
        [SerializeField] private int level = 1;
        [SerializeField] private int baseIntensity = 0;
        [SerializeField] private bool timed = false;
        [SerializeField] private string ageBand = "ADULT"; // SENIOR / ADULT / UNDER_18

        /// <summary>Solo para el smoke test headless: fuerza otro juego sin tocar la escena.</summary>
        public static string GameIdOverride;

        /// <summary>Solo para el smoke test headless: simula "quitar animaciones" del teléfono (<c>reduce_motion</c> de la config).</summary>
        public static bool ReduceMotionOverride;

        /// <summary>Solo para el smoke test headless: arranca el juego con su tutorial guiado (<c>show_tutorial</c> de la config).</summary>
        public static bool ShowTutorialOverride;
        /// <summary>«Primera*» del smoke (Tarea 191): la primera partida de un juego sin historial: tutorial, subida de etapa desde la 1 hasta el nivel sembrado (0,45) y tarjetas «NUEVO» como alguien nuevo (<c>fresh_start</c>: no lee ni guarda las marcas).</summary>
        public static bool FirstGameOverride;

        /// <summary>Solo para el smoke test headless: arranca la versión corta del inicio («Tu punto de partida», <c>assessment</c> de la config).</summary>
        public static bool AssessmentOverride;

        /// <summary>Solo para las capturas (<c>--capturas</c>): la etapa (1..10) con que arranca «La estación de correo» (<c>mail_stage</c> de la config; 0 = la que corresponde).</summary>
        public static int MailStageOverride;

        /// <summary>Solo para el smoke (corridas «Tarjetas…», Tarea 190d): la etapa (1..18) con que arranca «Constelaciones» (<c>con_stage</c>; 0 = la que corresponde) y el nivel (1..5) de la partida (0 = el de la escena), para ver TODAS las tarjetas «NUEVO».</summary>
        public static int ConStageOverride, LevelOverride;

        /// <summary>Solo para las capturas (<c>--capturas</c>): el nivel (1..12) con que arranca «Satélites» (<c>sat_stage</c> de la config; 0 = el que corresponde).</summary>
        public static int SatStageOverride;

        /// <summary>Solo para las capturas: la sorpresa que sale en todas las rondas de «Satélites» (<c>sat_surprise</c>: 1 órbita, 2 nube, 4 rápidas; 0 = las de siempre).</summary>
        public static int SatSurpriseOverride;

        /// <summary>Solo para las capturas (<c>--capturas</c>): el nivel (1..9) con que arrancan los dos motores de «Piloto Estelar» (<c>pil_stage</c> de la config; 0 = el que corresponde) y cada cuántos segundos cambia el sector (<c>pil_sector_s</c>; 0 = 30).</summary>
        public static int PilStageOverride, PilSectorOverride;

        /// <summary>Solo para las capturas: el nivel (1..12) con que arranca «Rescate relámpago» (<c>resc_stage</c> de la config; 0 = el que corresponde).</summary>
        public static int RescStageOverride;

        /// <summary>Solo para el smoke (corridas «…Mudo»): la partida arranca con <c>sound_enabled = false</c>, como si la persona hubiera apagado «Efectos de sonido» en Opciones.</summary>
        public static bool SoundOffOverride;

        /// <summary>Solo para el smoke y las capturas: arranca la partida en modo Reto (con reloj) aunque el campo <c>timed</c> de la escena esté apagado.</summary>
        public static bool TimedOverride;

        /// <summary>Solo para el smoke y las capturas: arranca la partida como CONTRARRELOJ (<c>time_trial</c> y <c>track_seed</c> de la config; Tarea 176), con la fecha de la pista que se pida (por omisión, la del día de la prueba).</summary>
        public static bool TimeTrialOverride;
        public static int TrackSeedOverride = 20261026;

        private void Start()
        {
#if UNITY_EDITOR
            // Solo en el Editor -- en un build real (dispositivo), la config llega vía
            // LaunchIntentConfigReader (Intent extra desde UnityGameLauncher.kt del lado
            // nativo). Ambos componentes pueden convivir en la misma escena sin pisarse.
            if (gameEntryPoint == null) gameEntryPoint = FindObjectOfType<GameEntryPoint>();
            if (gameEntryPoint == null)
            {
                Debug.LogError("[EditorPlaytestBootstrap] No se encontró un GameEntryPoint en la escena.");
                return;
            }

            if (int.TryParse(System.Environment.GetEnvironmentVariable("NUBI_EDITOR_LEVEL"), out int envLevel) && envLevel >= 1) level = Mathf.Min(5, envLevel);          // diagnóstico: el nivel (1 a 5) con que arranca la partida, para ver las etapas altas de un juego
            if (LevelOverride > 0) level = Mathf.Min(5, LevelOverride);
            int envStage = int.TryParse(System.Environment.GetEnvironmentVariable("NUBI_EDITOR_STAGE"), out int es) ? es : 0;          // diagnóstico: la etapa con que arrancan las escaleras con tarjetas «NUEVO» (Correo 1 a 10, Constelaciones 1 a 18, Satélites 1 a 12)
            var config = new SequenceInitConfig
            {
                user_id = "editor_playtest",
                game_id = string.IsNullOrEmpty(GameIdOverride) ? RastroGameController.GameId : GameIdOverride,
                config = new SequenceConfigDetails
                {
                    level = level,
                    base_intensity = baseIntensity,
                    timed = timed || TimedOverride,
                    age_band = ageBand,
                    sound_enabled = !SoundOffOverride,
                    reduce_motion = ReduceMotionOverride,
                    show_tutorial = ShowTutorialOverride || FirstGameOverride,
                    first_game = FirstGameOverride,
                    fresh_start = FirstGameOverride,
                    has_dda_rating = FirstGameOverride,
                    dda_rating = FirstGameOverride ? 0.45f : 0f,
                    assessment = AssessmentOverride,
                    assessment_step = AssessmentOverride ? 1 : 0,
                    assessment_total = AssessmentOverride ? 4 : 0,
                    mail_stage = envStage > 0 ? Mathf.Min(10, envStage) : MailStageOverride,
                    con_stage = envStage > 0 ? Mathf.Min(18, envStage) : ConStageOverride,
                    sat_stage = envStage > 0 ? Mathf.Min(12, envStage) : SatStageOverride,
                    sat_surprise = SatSurpriseOverride,
                    pil_stage = PilStageOverride,
                    pil_sector_s = PilSectorOverride,
                    resc_stage = RescStageOverride,
                    time_trial = TimeTrialOverride,
                    track_seed = TimeTrialOverride ? TrackSeedOverride : 0
                }
            };

            gameEntryPoint.InitializeGameConfig(JsonUtility.ToJson(config));
#endif
        }
    }
}
