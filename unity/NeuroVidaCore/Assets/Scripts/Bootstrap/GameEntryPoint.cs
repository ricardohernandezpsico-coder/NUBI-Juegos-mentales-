using UnityEngine;
using NeuroVida.Contracts;
using NeuroVida.Games.Secuencia;
using NeuroVida.Games.Parejas;
using NeuroVida.Games.Stroop;
using NeuroVida.Games.Calculo;
using NeuroVida.Games.Anagramas;
using NeuroVida.Games.Piloto;
using NeuroVida.Games.Radar;
using NeuroVida.Games.Satelites;
using NeuroVida.Games.Freno;
using NeuroVida.Games.Aterrizaje;
using NeuroVida.Games.Meteoros;
using NeuroVida.Games.Disparate;
using NeuroVida.Games.Cosecha;
using NeuroVida.Games.Intrusa;
using NeuroVida.Games.Acoplamiento;
using NeuroVida.Games.Bitacora;
using NeuroVida.Games.Rumbo;
using NeuroVida.Games.Correo;
using NeuroVida.Games.Engranajes;

namespace NeuroVida.Bridge
{
    /// <summary>
    /// Punto de entrada Nativo -> Unity. El lado Kotlin invoca
    /// <c>UnityPlayer.UnitySendMessage("GameBridge", "InitializeGameConfig", json)</c> —
    /// ver plantilla en
    /// <c>unity/NeuroVidaCore/NativeBridgeTemplates/android/UnityBridgeController.kt</c>.
    ///
    /// Convención fijada acá (documentar en el lado nativo cuando se conecte de verdad):
    /// GameObject en la escena llamado EXACTAMENTE "GameBridge", con este componente.
    ///
    /// <paramref name="json"/> siempre se parsea como <see cref="SequenceInitConfig"/> --
    /// el contrato de entrada (user_id/game_id/config con level/base_intensity/timed/
    /// age_band/sound_enabled) es genérico entre juegos a propósito desde el día 1 del
    /// roadmap (ver NeuroVida/CLAUDE.md), así que "Parejas Ocultas" (Fase 2) lo reusa tal
    /// cual en vez de tener su propia clase de config duplicada.
    /// </summary>
    public class GameEntryPoint : MonoBehaviour
    {
        [SerializeField] private RastroGameController rastroGameController;
        [SerializeField] private CardsGameController cardsGameController;
        [SerializeField] private StroopGameController stroopGameController;
        [SerializeField] private CalculoGameController calculoGameController;
        [SerializeField] private PuntaGameController anagramGameController;
        [SerializeField] private PilotGameController pilotGameController;
        [SerializeField] private RadarGameController radarGameController;
        [SerializeField] private SatelliteGameController satelliteGameController;
        [SerializeField] private BrakeGameController brakeGameController;
        [SerializeField] private LandingGameController landingGameController;
        [SerializeField] private MeteorGameController meteorGameController;
        [SerializeField] private DisparateGameController disparateGameController;
        [SerializeField] private CosechaGameController cosechaGameController;
        [SerializeField] private IntrusaGameController intrusaGameController;
        [SerializeField] private DockingGameController dockingGameController;
        [SerializeField] private BitacoraGameController bitacoraGameController;
        [SerializeField] private HomingGameController homingGameController;
        [SerializeField] private MailGameController mailGameController;
        [SerializeField] private EngranajesGameController engranajesGameController;

        /// <summary>Los 9 juegos arman su interfaz una sola vez para 1080x1920 vertical: al girar el teléfono se
        /// desarmaban. Se fija la orientación antes de cargar la escena, además de Player Settings (Portrait) y del
        /// manifest de la app, para que no dependa de que el export esté al día.</summary>
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void LockPortrait()
        {
            Screen.autorotateToPortrait = true;
            Screen.autorotateToPortraitUpsideDown = false;
            Screen.autorotateToLandscapeLeft = false;
            Screen.autorotateToLandscapeRight = false;
            Screen.orientation = ScreenOrientation.Portrait;
        }

        /// <summary>Unity queda vivo entre partidas y la escena se recarga "en reposo" (ver NativeBridge.CloseGameScreen):
        /// la cámara pinta el azul noche de la app en vez del cielo por defecto de Unity, así el instante entre que
        /// Unity vuelve al frente y arranca la partida nueva no muestra un fondo gris azulado ajeno a la app.</summary>
        private void Awake()
        {
            NeuroVida.Games.Shared.GameClock.Reset(); // cada carga de escena = partida nueva o reposo
            var cam = Camera.main;
            if (cam == null) return;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = NeuroVida.Games.Shared.NeuroStyle.NightBottom;
        }

        private NeuroVida.Games.Shared.PauseMenu _pause;
        private bool _running; // en esta escena arrancó una partida

        private bool InProgress => _running && !NativeBridge.GameFinished;

        /// <summary>El juego activo de esta escena (solo hay uno): su controlador común, para preguntarle si ofrece «Cómo se juega».</summary>
        private NeuroVida.Games.Shared.GameControllerBase ActiveGame => GetComponentInChildren<NeuroVida.Games.Shared.GameControllerBase>(false);

        /// <summary>Botón Atrás de Android (llega como Escape). A mitad de partida PAUSA (menú Continuar /
        /// Reiniciar / Salir) en vez de abandonarla; con el menú abierto, Atrás = Continuar; con la partida
        /// terminada (o sin partida), vuelve a la app.</summary>
        private void Update()
        {
            if (!Input.GetKeyDown(KeyCode.Escape)) return;
            if (_pause != null && _pause.IsShown)
            {
                _pause.ResumeFromBack();
                return;
            }
            if (InProgress)
            {
                ShowPause();
                return;
            }
            NativeBridge.CloseGameScreen();
        }

        /// <summary>La app pasa a segundo plano (Inicio, llamada, otra app) a mitad de partida: queda en pausa, y al
        /// volver se ve el menú en vez de encontrar la partida corriendo o con el reloj vencido.</summary>
        private void OnApplicationPause(bool paused)
        {
            if (paused && InProgress) ShowPause();
        }

        private void ShowPause()
        {
            if (_pause == null)
            {
                _pause = NeuroVida.Games.Shared.PauseMenu.Create(
                    transform,
                    onResume: null,
                    onRestart: LaunchIntentConfigReader.RestartCurrentGame,
                    onExit: ExitPaused,
                    onHowTo: () => { var g = ActiveGame; if (g != null) g.ShowHowTo(); },
                    canHowTo: () => { var g = ActiveGame; return g != null && g.CanShowHowTo; });
            }
            _pause.Show();
        }

        /// <summary>"Salir" del menú de pausa: vuelve a la app con la partida en pausa (el menú queda visible para
        /// cuando se vuelva a abrir el juego). Si no se puede, se abandona como antes.</summary>
        private void ExitPaused()
        {
            if (NativeBridge.ReturnToAppPaused()) return;
            NeuroVida.Games.Shared.GameClock.Reset();
            NativeBridge.CloseGameScreen();
        }

        /// <summary>Invocado por <c>UnityPlayer.UnitySendMessage</c> desde el lado nativo.
        /// <paramref name="json"/> es un <see cref="SequenceInitConfig"/> serializado.</summary>
        public void InitializeGameConfig(string json)
        {
            var config = JsonUtility.FromJson<SequenceInitConfig>(json);
            if (config?.config == null)
            {
                Debug.LogError("[GameEntryPoint] JSON de configuración inválido o incompleto: " + json);
                return;
            }

            _running = true;
            // Sonido y vibración comunes de los 9 juegos (ajustes de la app).
            NeuroVida.Games.Shared.GameFeel.SoundOn = config.config.sound_enabled;
            NeuroVida.Games.Shared.GameFeel.HapticsOn = config.config.haptics_enabled;
            NeuroVida.Games.Shared.GameFeel.ReduceMotion = config.config.reduce_motion;
            // Evaluación inicial ("Tu punto de partida"): subtítulo de la cuenta regresiva y calibración rápida.
            NeuroVida.Games.Shared.Assessment.Configure(config.config);
            // Modo elegido antes de jugar (Suave / Desafío / Experto): techo o piso de todos los DDA de la partida.
            NeuroVida.Games.AdaptiveDifficulty.ConfigureMode(config.config);
            switch (config.game_id)
            {
                case RastroGameController.GameId:
                    if (rastroGameController == null)
                    {
                        // Sin wiring en el Editor: el controlador se crea por código, como el resto de los juegos.
                        var go = new GameObject("RastroGameController");
                        go.transform.SetParent(transform, false);
                        rastroGameController = go.AddComponent<RastroGameController>();
                    }
                    rastroGameController.gameObject.SetActive(true);
                    rastroGameController.StartSession(config);
                    break;
                case CardsGameController.GameId:
                    if (cardsGameController == null)
                    {
                        var go = new GameObject("CardsGameController");
                        go.transform.SetParent(transform, false);
                        cardsGameController = go.AddComponent<CardsGameController>();
                    }
                    cardsGameController.gameObject.SetActive(true);
                    cardsGameController.StartSession(config);
                    break;
                case StroopGameController.GameId:
                    if (stroopGameController == null)
                    {
                        var go = new GameObject("StroopGameController");
                        go.transform.SetParent(transform, false);
                        stroopGameController = go.AddComponent<StroopGameController>();
                    }
                    stroopGameController.gameObject.SetActive(true);
                    stroopGameController.StartSession(config);
                    break;
                case CalculoGameController.GameId:
                    if (calculoGameController == null)
                    {
                        var go = new GameObject("CalculoGameController");
                        go.transform.SetParent(transform, false);
                        calculoGameController = go.AddComponent<CalculoGameController>();
                    }
                    calculoGameController.gameObject.SetActive(true);
                    calculoGameController.StartSession(config);
                    break;
                case EngranajesGameController.GameId:
                    if (engranajesGameController == null)
                    {
                        var go = new GameObject("EngranajesGameController");
                        go.transform.SetParent(transform, false);
                        engranajesGameController = go.AddComponent<EngranajesGameController>();
                    }
                    engranajesGameController.gameObject.SetActive(true);
                    engranajesGameController.StartSession(config);
                    break;
                case PuntaGameController.GameId:
                    if (anagramGameController == null)
                    {
                        var go = new GameObject("PuntaGameController");
                        go.transform.SetParent(transform, false);
                        anagramGameController = go.AddComponent<PuntaGameController>();
                    }
                    anagramGameController.gameObject.SetActive(true);
                    anagramGameController.StartSession(config);
                    break;
                case PilotGameController.GameId:
                    if (pilotGameController == null)
                    {
                        var go = new GameObject("PilotGameController");
                        go.transform.SetParent(transform, false);
                        pilotGameController = go.AddComponent<PilotGameController>();
                    }
                    pilotGameController.gameObject.SetActive(true);
                    pilotGameController.StartSession(config);
                    break;
                case RadarGameController.GameId:
                    if (radarGameController == null)
                    {
                        var go = new GameObject("RadarGameController");
                        go.transform.SetParent(transform, false);
                        radarGameController = go.AddComponent<RadarGameController>();
                    }
                    radarGameController.gameObject.SetActive(true);
                    radarGameController.StartSession(config);
                    break;
                case SatelliteGameController.GameId:
                    if (satelliteGameController == null)
                    {
                        var go = new GameObject("SatelliteGameController");
                        go.transform.SetParent(transform, false);
                        satelliteGameController = go.AddComponent<SatelliteGameController>();
                    }
                    satelliteGameController.gameObject.SetActive(true);
                    satelliteGameController.StartSession(config);
                    break;
                case BrakeGameController.GameId:
                    if (brakeGameController == null)
                    {
                        var go = new GameObject("BrakeGameController");
                        go.transform.SetParent(transform, false);
                        brakeGameController = go.AddComponent<BrakeGameController>();
                    }
                    brakeGameController.gameObject.SetActive(true);
                    brakeGameController.StartSession(config);
                    break;
                case LandingGameController.GameId:
                    if (landingGameController == null)
                    {
                        var go = new GameObject("LandingGameController");
                        go.transform.SetParent(transform, false);
                        landingGameController = go.AddComponent<LandingGameController>();
                    }
                    landingGameController.gameObject.SetActive(true);
                    landingGameController.StartSession(config);
                    break;
                case DockingGameController.GameId:
                    if (dockingGameController == null)
                    {
                        var go = new GameObject("DockingGameController");
                        go.transform.SetParent(transform, false);
                        dockingGameController = go.AddComponent<DockingGameController>();
                    }
                    dockingGameController.gameObject.SetActive(true);
                    dockingGameController.StartSession(config);
                    break;
                case BitacoraGameController.GameId:
                    if (bitacoraGameController == null)
                    {
                        var go = new GameObject("BitacoraGameController");
                        go.transform.SetParent(transform, false);
                        bitacoraGameController = go.AddComponent<BitacoraGameController>();
                    }
                    bitacoraGameController.gameObject.SetActive(true);
                    bitacoraGameController.StartSession(config);
                    break;
                case HomingGameController.GameId:
                    if (homingGameController == null)
                    {
                        var go = new GameObject("HomingGameController");
                        go.transform.SetParent(transform, false);
                        homingGameController = go.AddComponent<HomingGameController>();
                    }
                    homingGameController.gameObject.SetActive(true);
                    homingGameController.StartSession(config);
                    break;
                case MailGameController.GameId:
                    if (mailGameController == null)
                    {
                        var go = new GameObject("MailGameController");
                        go.transform.SetParent(transform, false);
                        mailGameController = go.AddComponent<MailGameController>();
                    }
                    mailGameController.gameObject.SetActive(true);
                    mailGameController.StartSession(config);
                    break;
                case MeteorGameController.GameId:
                    if (meteorGameController == null)
                    {
                        var go = new GameObject("MeteorGameController");
                        go.transform.SetParent(transform, false);
                        meteorGameController = go.AddComponent<MeteorGameController>();
                    }
                    meteorGameController.gameObject.SetActive(true);
                    meteorGameController.StartSession(config);
                    break;
                case DisparateGameController.GameId:
                    if (disparateGameController == null)
                    {
                        var go = new GameObject("DisparateGameController");
                        go.transform.SetParent(transform, false);
                        disparateGameController = go.AddComponent<DisparateGameController>();
                    }
                    disparateGameController.gameObject.SetActive(true);
                    disparateGameController.StartSession(config);
                    break;
                case CosechaGameController.GameId:
                    if (cosechaGameController == null)
                    {
                        var go = new GameObject("CosechaGameController");
                        go.transform.SetParent(transform, false);
                        cosechaGameController = go.AddComponent<CosechaGameController>();
                    }
                    cosechaGameController.gameObject.SetActive(true);
                    cosechaGameController.StartSession(config);
                    break;
                case IntrusaGameController.GameId:
                    if (intrusaGameController == null)
                    {
                        var go = new GameObject("IntrusaGameController");
                        go.transform.SetParent(transform, false);
                        intrusaGameController = go.AddComponent<IntrusaGameController>();
                    }
                    intrusaGameController.gameObject.SetActive(true);
                    intrusaGameController.StartSession(config);
                    break;
                default:
                    Debug.LogError($"[GameEntryPoint] game_id {config.game_id} no tiene un controlador registrado todavía.");
                    break;
            }
        }
    }
}
