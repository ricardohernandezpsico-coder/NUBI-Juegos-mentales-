using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using NeuroVida.Bridge;
using NeuroVida.Contracts;
using NeuroVida.Games.Shared;
using static NeuroVida.Games.Shared.UiKit;
using Motion = NeuroVida.Games.Shared.Motion; // UnityEngine.Motion también existe

namespace NeuroVida.Games.Piloto
{
    /// <summary>
    /// «Piloto Estelar: la ruta de las balizas» (id <c>piloto</c>, renovado el 9-oct; ver <see cref="PilotContract"/> y docs/diseno-piloto.md; el boceto aprobado es docs/previews/piloto-balizas-boceto.html).
    /// DOBLE TAREA SIEMPRE JUNTA (regla permanente 1 de Ricardo): un dedo, en la franja de abajo, guía la nave por una ruta de balizas; con el otro se atrapan SOLO las señales de la misión (forma y detalle: «hexágono con punto»). El vuelo de 90 s
    /// cruza tres sectores con nombre, y en cada uno cambia la misión; nada se detiene. No hay piloto automático ni «costo de multitarea»: nada se mide con una tarea sola (los primeros 10 s son suaves, con las dos tareas a la vez).
    /// Dos motores comunes de dificultad (pilotaje por ventanas de 1,5 s y señales por señal). Todo el tiempo va con <see cref="GameClock"/> y lo que se mueve con <see cref="Motion"/>.
    /// Pedido explícito de Ricardo (9-oct): NINGÚN aviso, globo ni cartel tapa una señal visible ni la zona donde nacen. Los avisos (sector, hiperimpulso) van en una franja fija sobre el cielo de señales y esperan a que no haya
    /// ninguna señal bajo ellos; mientras uno está a la vista o en espera, las señales nuevas nacen fuera de su rectángulo (<see cref="PilotSpawn"/>); los textos flotantes se ponen junto a su señal sin tapar otra.
    /// </summary>
    public sealed partial class PilotGameController : GameControllerBase
    {
        public const string GameId = PilotContract.GameId;

        private const float UnitsPerDp = 3f;
        private const float MarginU = 60f;
        /// <summary>Antes del cambio de sector no nacen señales durante este tiempo (más que la exposición más larga): así la misión nueva no encuentra señales viejas.</summary>
        private const float SpawnPauseSeconds = 2.2f;
        private const float NoticeSeconds = 2.2f;

        /// <summary>SOLO EN EL EDITOR, para las capturas de pantalla (<c>verificar-todo.sh --capturas Piloto</c>): con esta bandera el juego NO se juega solo y un guion (<see cref="EditorShotScript"/>) lo lleva por los
        /// momentos que se fotografían. En el teléfono es siempre false.</summary>
        public static bool EditorShotMode;

        private enum Phase { Idle, Fly, Done }
        private enum Outcome { None, Gone, Caught, Wrong }

        /// <summary>Una señal viva (o que se está desvaneciendo): qué es, dónde está y cuándo nació.</summary>
        private sealed class Signal
        {
            public PilotSignalKind Kind;
            public float X, Y, At, Expo, DoneAt;
            public Outcome Outcome;
            public int View = -1;
            public bool BotSeen;
            public bool Alive => Outcome == Outcome.None;
        }

        /// <summary>El arco de un sector: a qué distancia de la ruta (dp) lo cruza la nave y a qué sector entra.</summary>
        private sealed class Gate
        {
            public float P;
            public int Sector;
        }

        // ------------------------------------------------------------------ estado

        private System.Random _rng;
        private AdaptiveDifficulty _driveDda, _signalDda;
        private PilotRun _run = new PilotRun();
        private PilotRoute _route = new PilotRoute(1);
        private PilotMission _mission;
        private Phase _phase = Phase.Idle;
        private bool _loopOn, _baked, _guided, _gateScheduled;
        private int _sector, _spawned, _dropped, _debugStage;
        private float _t, _dist, _shipX = PilotPlan.Width * 0.5f, _steerX = PilotPlan.Width * 0.5f;
        private float _nextSignalIn, _hyperLeft, _wobbleAt = -10f, _missionFreshAt = -10f, _beamAt = -10f, _beamX, _beamY, _steeredSeconds;
        private float _flightSeconds = PilotContract.FlightSeconds, _sectorSeconds = PilotContract.SectorSeconds;
        private int _steerFinger = -1, _previousFrameRate;
        /// <summary>Cuántas señales dura Precisión (24) y cada cuántas hay un sector nuevo (un tercio). El arranque de prueba del Editor las acorta para llegar al final.</summary>
        private int _precisionTotal = PilotContract.PrecisionSignals;
        private int PrecisionEvery => _precisionTotal / PilotContract.Sectors;
        private bool _mouseSteer, _insideNow = true;
        private StarfieldFx _stars;
        private readonly List<Signal> _signals = new List<Signal>();
        private readonly List<Gate> _gates = new List<Gate>();
        private readonly List<Box> _forbidden = new List<Box>();
        private readonly List<(float x, float y)> _liveSpots = new List<(float x, float y)>();

        private ExitButton _exit;
        private GameHud _hud;
        private CountdownScreen _countdown;

        private bool Endless => _config != null && _config.config.timed;
        private float Now => GameClock.Time;
        private bool Hyper => _hyperLeft > 0f;
        private bool Gentle => !_guided && _t < PilotContract.GentleSeconds;
        private bool Precision => !_guided && !Endless;
        private int DriveLevel => _guided ? 1 : PilotContract.DriveLevel(_driveDda.Level, Gentle);
        private int SignalLevel => _guided ? 1 : _signalDda.Level;
        private float Speed => PilotContract.Speed(DriveLevel, Precision) * (Hyper ? PilotContract.HyperSpeedFactor : 1f) * (_guided ? PracticeSpeedFactor : 1f);

        // ------------------------------------------------------------------ sesión

        public void StartSession(SequenceInitConfig config)
        {
            _config = config;
            _rng = new System.Random();
#if UNITY_EDITOR
            // el smoke arranca en el nivel 5 (señales en los bordes y parecidas) y con sectores de 6 s: pasa por los tres sectores y el final en pocos segundos
            if (GuidedTutorial.EditorAutoPlayGame && !EditorShotMode)
            {
                if (config.config.pil_stage <= 0) config.config.pil_stage = 5;
                if (config.config.pil_sector_s <= 0) config.config.pil_sector_s = 6;
            }
            _precisionTotal = GuidedTutorial.EditorAutoPlayGame && !EditorShotMode ? 6 : PilotContract.PrecisionSignals;
#endif
            _driveDda = PilotMetrics.CreateDriveEngine(config.config);
            _signalDda = PilotMetrics.CreateSignalEngine(config.config);
            _debugStage = Mathf.Max(0, config.config.pil_stage);
            _sectorSeconds = config.config.pil_sector_s > 0 ? config.config.pil_sector_s : PilotContract.SectorSeconds;
            _flightSeconds = _sectorSeconds * PilotContract.Sectors;
            _run = new PilotRun();
            _phase = Phase.Idle;
            _loopOn = _guided = false;
            _steerFinger = -1;
            _mouseSteer = false;

            // Movimiento continuo: a 60 cuadros por segundo se ve fluido (Android da 30 por defecto).
            _previousFrameRate = Application.targetFrameRate;
            Application.targetFrameRate = 60;

            ResetViews();
            _exit.Hide();
            _hud.SetStreak(0);
            _tutorial.Hide();
            StopAllCoroutines();
            StartCoroutine(GameLoop());
        }

        private void OnDisable()
        {
            if (_previousFrameRate != 0) Application.targetFrameRate = _previousFrameRate;
            StopEngine();
        }

        private IEnumerator GameLoop()
        {
            StartCoroutine(Prewarm());
            if (TutorialWanted)
            {
                // la ronda guiada se juega sobre la pantalla ya armada, antes de la cuenta regresiva
                _safe.gameObject.SetActive(true);
                yield return null;
                ApplySafeArea(_safe);
                Canvas.ForceUpdateCanvases();
                Layout();
                yield return StartCoroutine(RunTutorialIfNeeded());
                ResetViews();
            }
            _safe.gameObject.SetActive(false);
            yield return StartCoroutine(_countdown.Play(PilotContract.Title, Assessment.Subtitle(PilotContract.CountdownSub), () => _safe.gameObject.SetActive(true)));
            while (!_baked) yield return null;
            _safe.gameObject.SetActive(true);
            yield return null;
            ApplySafeArea(_safe);
            Canvas.ForceUpdateCanvases();
            Layout();
            BeginFlight();
            _loopOn = true;
        }

        /// <summary>Hornea los sprites y sintetiza los sonidos durante la cuenta regresiva, de a poco por cuadro (así nada se traba).</summary>
        private IEnumerator Prewarm()
        {
            if (_baked) yield break;
            var sprites = PilotSignalSprites.Prewarm();
            while (sprites.MoveNext()) yield return null;
            PilotShipSprite.Get();
            yield return null;
            var sounds = PilotSounds.Prewarm();
            while (sounds.MoveNext()) yield return null;
            _baked = true;
        }

        /// <summary>Empieza el vuelo de verdad: la ruta, la primera misión y el primer sector (con su aviso).</summary>
        private void BeginFlight()
        {
            _guided = false;
            _run = new PilotRun();
            _t = _dist = 0f;
            _sector = _spawned = _dropped = 0;
            _gateScheduled = false;
            _hyperLeft = 0f;
            _steeredSeconds = 0f;
            _shipX = _steerX = PilotPlan.Width * 0.5f;
            _wobbleAt = _beamAt = -10f;
            _route = new PilotRoute(PilotContract.DriveLevel(_driveDda.Level, true));
            ExtendRoute();
            _mission = PilotContract.PickMission(_rng, null);
            _missionFreshAt = -10f;
            ClearSignals();
            _gates.Clear();
            ClearNotices();
            _nextSignalIn = 1.2f;
            ShowFlightLayers(true);
            _endLayer.gameObject.SetActive(false);
            SetMissionCard();
            SetJourney(0f);
            _tintFrom = _tintTo = 0;
            _tintMix = 1f;
            _hud.SetLevelText(PilotContract.SectorChip(0));
            UpdateHud();
            RequestNotice(PilotContract.SectorBannerTag(0), PilotContract.SectorName(0), NoticeSeconds, Gold);
            _phase = Phase.Fly;
            StartEngine();
            LayoutStrip();
        }

        private void ExtendRoute()
        {
            _route.Extend(_dist + (_plan.ShipY - _plan.RouteTop) + 160f, DriveLevel);
            _route.Prune(_dist - 90f);
        }

        // ------------------------------------------------------------------ un cuadro

        private void Update()
        {
            if (PollTutorialSkip()) return;             // un toque en «Saltar tutorial» no es un toque al juego
            float dt = GameClock.DeltaTime;
            ReadInput(dt);
            if (_phase == Phase.Fly && dt > 0f) Step(Mathf.Min(dt, 0.05f));
            Animate(Now, dt);
#if UNITY_EDITOR
            if (_guided && _phase == Phase.Fly && GuidedTutorial.EditorAutoContinue && dt > 0f)
            {
                _guidedBotT += dt;                         // el smoke «desliza» solo en el paso 2 del tutorial
                if (_guidedBotT > 2f) _steeredSeconds = Mathf.Max(_steeredSeconds, 1.6f);
            }
            if (_phase == Phase.Fly && !_guided && dt > 0f) AutoPlay();
            GuardNotices();
#endif
        }

        /// <summary>Un paso de vuelo: avanza la ruta, mueve la nave, pasa las balizas, cierra las ventanas de pilotaje, hace nacer y vencer las señales y cambia de sector.</summary>
        private void Step(float dt)
        {
            float now = Now;
            _t += dt;
            _dist += Speed * dt;
            ExtendRoute();
            bool wasHyper = Hyper;
            if (_hyperLeft > 0f) _hyperLeft = Mathf.Max(0f, _hyperLeft - dt);
            if (wasHyper && !Hyper) UpdateHud();                 // el chip de puntos deja de decir «×2»

            // la nave sigue al dedo con inercia
            _shipX = Mathf.Lerp(_shipX, _steerX, Mathf.Min(1f, dt * PilotContract.ShipFollow));
            _route.At(_dist, out float center, out float half);
            _insideNow = PilotContract.Inside(_shipX, center, half);
            if (_run.Fly(dt, _insideNow, out bool passed) && !_guided)
            {
                _driveDda.Register(passed);
                if (passed) AfterResolve();                  // tres ventanas limpias pueden dar el hiperimpulso a una racha que ya iba en cinco
            }

            // las balizas que pasan: se encienden si la nave iba dentro; si iba fuera, parpadean, la nave tiembla y suena un zumbido
            var beacons = _route.Beacons;
            for (int i = 0; i < beacons.Count; i++)
            {
                var b = beacons[i];
                if (b.Passed || b.P > _dist) continue;
                b.Passed = true;
                if (_insideNow)
                {
                    b.LitAt = now;
                    PlayClip(PilotSounds.Beacon(Mathf.RoundToInt(b.P / PilotContract.BeaconGap)), 0.6f);
                }
                else
                {
                    b.BadAt = now;
                    _wobbleAt = now;
                    PlayClip(PilotSounds.Buzz(), 0.7f);
                    GameFeel.Haptic(GameFeel.HapticKind.Light);
                }
            }

            StepSignals(now, dt);
            if (!_guided) StepSectors(now);
            if (!_guided && Endless) SetJourney(_t / _flightSeconds);
            if (!_guided && Endless && _t >= _flightSeconds) EndFlight();
        }

        // ------------------------------------------------------------------ las señales

        private void StepSignals(float now, float dt)
        {
            // vencen: una de la misión que se fue es una omisión; una que no era de la misión y se dejó pasar es un acierto en silencio
            for (int i = 0; i < _signals.Count; i++)
            {
                var s = _signals[i];
                if (!s.Alive || now - s.At <= s.Expo) continue;
                s.Outcome = Outcome.Gone;
                s.DoneAt = now;
                if (s.Kind.IsTarget)
                {
                    _run.Miss();
                    if (!_guided) _signalDda.Register(false);
                    ShowFloat(PilotContract.GoneText, Bad, s.X, s.Y, s);
                }
                else
                {
                    _run.Ignore();
                    if (!_guided) _signalDda.Register(true);
                }
                UpdateHud();
                AfterResolve();
            }
            for (int i = _signals.Count - 1; i >= 0; i--)
            {
                var s = _signals[i];
                if (s.Alive || now - s.DoneAt < SignalFadeSeconds) continue;
                ReleaseView(s);
                _signals.RemoveAt(i);
            }

            // nacen
            if (_guided ? !_practiceSpawn : SpawnPaused) return;
            _nextSignalIn -= dt;
            if (_nextSignalIn > 0f) return;
            if (TrySpawn(now))
            {
                float gap = PilotContract.Gap(SignalLevel, Gentle) * (_guided ? PracticeGapFactor : 1f);
                _nextSignalIn = gap * (0.85f + (float)_rng.NextDouble() * 0.3f);
            }
            else _nextSignalIn = 0.15f;                       // no hubo lugar libre (un aviso a la vista o muchas señales): se vuelve a probar enseguida
        }

        private const float SignalFadeSeconds = 0.6f;

        /// <summary>true mientras no deben nacer señales: justo antes de un cambio de sector (que la misión nueva no encuentre señales viejas) o cuando ya nacieron las 24 de Precisión.</summary>
        private bool SpawnPaused
        {
            get
            {
                if (_gateScheduled) return true;
                int next = _sector + 1;
                if (Endless) return next < PilotContract.Sectors && _t >= next * _sectorSeconds - SpawnPauseSeconds;
                if (_spawned >= _precisionTotal) return true;
                return next < PilotContract.Sectors && _spawned >= next * PrecisionEvery;
            }
        }

        private bool TrySpawn(float now)
        {
            int view = FreeView();
            if (view < 0) return false;
            var kind = PilotContract.NextSignal(SignalLevel, _mission, _rng);
            return SpawnKind(kind, view, now, SignalLevel);
        }

        private bool SpawnKind(PilotSignalKind kind, int view, float now, int level)
        {
            BuildForbidden();
            _liveSpots.Clear();
            foreach (var s in _signals) if (s.Alive) _liveSpots.Add((s.X, s.Y));
            if (!PilotSpawn.TryPlace(_rng, kind.Peripheral, _plan.SkyTop, _plan.SkyBottom, _forbidden, _liveSpots, RouteAtY, out float x, out float y)) return false;
            float expo = _guided ? PracticeExposure : PilotContract.ExposureMs(level, Precision, Gentle) / 1000f;
            AddSignal(kind, x, y, expo, view, now);
            return true;
        }

        private Signal AddSignal(PilotSignalKind kind, float x, float y, float expo, int view, float now)
        {
            var s = new Signal { Kind = kind, X = x, Y = y, At = now, Expo = expo, View = view };
            _signals.Add(s);
            _views[view].InUse = true;
            var v = _views[view];
            v.Body.sprite = PilotSignalSprites.Get(kind.Shape, kind.Detail);
            v.Mark.gameObject.SetActive(false);
            v.Root.gameObject.SetActive(true);
            v.Root.localScale = Vector3.one;
            v.Root.anchoredPosition = P(x, y);
            if (!_guided) _spawned++;
            PlayClip(PilotSounds.Blip(), 0.5f);
            return s;
        }

        private (float center, float half) RouteAtY(float y)
        {
            _route.At(_dist + (_plan.ShipY - y), out float c, out float h);
            return (c, h);
        }

        private int FreeView()
        {
            for (int i = 0; i < _views.Length; i++) if (!_views[i].InUse) return i;
            return -1;
        }

        private void ReleaseView(Signal s)
        {
            if (s.View < 0) return;
            _views[s.View].InUse = false;
            _views[s.View].Root.gameObject.SetActive(false);
            s.View = -1;
        }

        private void ClearSignals()
        {
            foreach (var s in _signals) ReleaseView(s);
            _signals.Clear();
            foreach (var v in _views) { v.InUse = false; v.Root.gameObject.SetActive(false); }
        }

        /// <summary>Los rectángulos donde NO nacen señales: el aviso a la vista o en espera y los textos flotantes que están a la vista.</summary>
        private void BuildForbidden()
        {
            _forbidden.Clear();
            if (NoticeClaimsZone) _forbidden.Add(_plan.BannerBox);
            foreach (var f in _floatViews) if (f.Active) _forbidden.Add(f.Area);
        }

        // ------------------------------------------------------------------ atrapar

        /// <summary>Un toque en el cielo: la señal viva más cercana dentro de 40 dp (un toque de 80 dp de diámetro). Si no hay ninguna, no pasa nada.</summary>
        private bool TapAt(float x, float y)
        {
            Signal best = null;
            float bestD = PilotContract.TouchRadius;
            foreach (var s in _signals)
            {
                if (!s.Alive) continue;
                float d = Mathf.Sqrt((s.X - x) * (s.X - x) + (s.Y - y) * (s.Y - y));
                if (d < bestD) { bestD = d; best = s; }
            }
            if (best == null) return false;
            if (best.Kind.IsTarget) CatchSignal(best); else WrongTap(best);
            return true;
        }

        private void CatchSignal(Signal s)
        {
            float now = Now;
            s.Outcome = Outcome.Caught;
            s.DoneAt = now;
            int pts = _run.Catch(Hyper);
            if (!_guided) _signalDda.Register(true);
            _beamAt = now;
            _beamX = s.X;
            _beamY = s.Y;
            ShowFloat("+" + pts, Good, s.X, s.Y, s);
            if (Motion.Decorative) StartCoroutine(UiFx.SparkBurst(_fxLayer, P(s.X, s.Y), Gold, 8, 60f * _s, 5f * _s, 0.45f));
            PlayClip(PilotSounds.Catch(_run.Streak), 0.8f);
            GameFeel.Haptic(GameFeel.HapticKind.Light);
            UpdateHud();
            AfterResolve();
        }

        private void WrongTap(Signal s)
        {
            s.Outcome = Outcome.Wrong;
            s.DoneAt = Now;
            _run.FalseAlarm();
            if (!_guided) _signalDda.Register(false);
            ShowFloat(PilotContract.WrongText, Bad, s.X, s.Y, s);
            PlayClip(PilotSounds.Thud(), 0.7f);
            GameFeel.Haptic(GameFeel.HapticKind.Double);
            UpdateHud();
            AfterResolve();
        }

        /// <summary>Después de resolverse una señal (o de cerrarse una ventana limpia): ¿hiperimpulso? ¿se acabó la partida de Precisión?</summary>
        private void AfterResolve()
        {
            if (_guided) return;
            if (_run.ShouldHyper(Hyper)) StartHyper();
            if (!Endless)
            {
                int done = _run.Resolved + _dropped;
                SetJourney((float)done / _precisionTotal);
                if (done >= _precisionTotal && _phase == Phase.Fly) EndFlight();
            }
        }

        private void StartHyper()
        {
            _hyperLeft = PilotContract.HyperSeconds;
            _run.CountHyper();
            RequestNotice(PilotContract.HyperTitle, PilotContract.HyperSub, 1.8f, Cyan);
            PlayClip(PilotSounds.Hyper(), 0.8f);
            GameFeel.Haptic(GameFeel.HapticKind.Firm);
            if (Motion.Decorative) StartCoroutine(UiFx.RingBurst(_fxLayer, P(_shipX, _plan.ShipY), Gold, 40f * _s, 230f * _s, 0.55f));
            UpdateHud();
        }

        // ------------------------------------------------------------------ sectores

        /// <summary>Reto: el arco sale arriba a tiempo para que la nave lo cruce en el límite del sector. Precisión: sale cuando ya nacieron las 8 señales del sector. La misión cambia al cruzar el arco; nada se detiene.</summary>
        private void StepSectors(float now)
        {
            int next = _sector + 1;
            if (next < PilotContract.Sectors && !_gateScheduled)
            {
                float ahead = GateAhead;
                bool due = Endless ? _t >= next * _sectorSeconds - ahead / Mathf.Max(1f, Speed)
                                   : _spawned >= next * PrecisionEvery;
                if (due)
                {
                    _gates.Add(new Gate { P = _dist + ahead, Sector = next });
                    _gateScheduled = true;
                }
            }
            for (int i = _gates.Count - 1; i >= 0; i--)
            {
                var g = _gates[i];
                if (_dist >= g.P && g.Sector > _sector) EnterSector(g.Sector, now);
                if (_plan.ShipY - (g.P - _dist) > _plan.ShipY + 90f) _gates.RemoveAt(i);
            }
        }

        /// <summary>A qué distancia por delante de la nave (dp) sale el arco de un sector: arriba del todo, bajo la tarjeta de misión. El arranque de prueba del Editor lo acerca para llegar al final en pocos segundos.</summary>
        private float GateAhead
        {
            get
            {
                float full = _plan.ShipY - _plan.RouteTop - 14f;
#if UNITY_EDITOR
                if (GuidedTutorial.EditorAutoPlayGame && !EditorShotMode) return Mathf.Min(full, 200f);
#endif
                return full;
            }
        }

        private void EnterSector(int sector, float now)
        {
            _sector = sector;
            _gateScheduled = false;
#if UNITY_EDITOR
            Debug.Log("[SmokeTest] Piloto: sector " + (sector + 1) + " a los " + _t.ToString("0.0") + " s (" + (Endless ? "Reto" : "Precisión") + "), nacieron " + _spawned + ", resueltas " + _run.Resolved);
#endif
            // las señales que quedaban eran de la misión vieja: se retiran sin contar
            foreach (var s in _signals) if (s.Alive) { s.Outcome = Outcome.Gone; s.DoneAt = now - SignalFadeSeconds + 0.12f; _dropped++; }
            _mission = PilotContract.PickMission(_rng, _mission);
            _missionFreshAt = now;
            SetMissionCard();
            _tintFrom = _tintTo;
            _tintTo = sector;
            _tintMix = 0f;
            _hud.SetLevelText(PilotContract.SectorChip(sector), highlight: true);
            RequestNotice(PilotContract.SectorBannerTag(sector), PilotContract.SectorName(sector), NoticeSeconds, Gold);
            PlayClip(PilotSounds.Whoosh(), 0.8f);
            GameFeel.Haptic(GameFeel.HapticKind.Firm);
        }

        // ------------------------------------------------------------------ marcador

        private void UpdateHud()
        {
            if (_guided) return;
            _hud.SetStreak(_run.Streak);
            if (Endless)
            {
                if (Hyper) _hud.SetInfo(PilotContract.PointsText(_run.Points) + PilotContract.HyperChip);
                else _hud.SetPoints(_run.Points);
            }
            else _hud.SetInfo(PilotContract.PrecisionChip(_run.Resolved + _dropped, _precisionTotal));
        }

        // ------------------------------------------------------------------ fin

        private void EndFlight()
        {
            if (_phase == Phase.Done) return;
            _phase = Phase.Done;
            StartCoroutine(FinishGame());
        }

        private IEnumerator FinishGame()
        {
            _loopOn = false;
            StopEngine();
            ClearSignals();
            ClearNotices();
            foreach (var f in _floatViews) { f.Active = false; f.Root.gameObject.SetActive(false); }
            _hyperLeft = 0f;
            UpdateHud();                                       // el chip de puntos deja de decir «×2»
            if (_stars != null) _stars.Warp = 0.12f;           // las estrellas vuelven a su paso de siempre detrás de la pantalla final
            float? score = _run.SignalScore;
#if UNITY_EDITOR
            Debug.Log("[SmokeTest] Piloto: partida terminada: " + _run.Points + " puntos, en la ruta " + Mathf.RoundToInt(_run.InLaneFraction * 100f) + " %, señales " + _run.Hits + " de " + _run.Targets + ", toques equivocados " + _run.FalseAlarms
                      + ", racha mayor " + _run.BestStreak + ", hiperimpulsos " + _run.HyperCount + ", sector " + (_sector + 1) + ", nivel de señales " + _signalDda.Level + ", pilotaje " + _driveDda.Level);
#endif
            ShowFlightLayers(false);
            ShowEnd(score);
            _exit.Show();
            PlayClip(PilotSounds.Finale(), 0.8f);
            var telemetry = new StroopTelemetry
            {
                user_id = _config.user_id,
                game_id = _config.game_id,
                session_metrics = PilotMetrics.Build(_driveDda, _signalDda, _run, _config.config)
            };
            NativeBridge.ForwardTelemetryToPlatform(JsonUtility.ToJson(telemetry));
            yield break;
        }

        // ------------------------------------------------------------------ pieza del tutorial

        /// <summary>El tutorial guiado común sobre el área segura (se llama al final de <c>BuildUi</c>, para que quede encima de todo).</summary>
        private void SetUpTutorial() =>
            BuildTutorial(_safe, GameHud.Height + 10f, PilotContract.Title, "Guía la nave con un dedo y, con el otro, atrapa solo las señales de tu misión.", badgeAtBottom: true);

        // ------------------------------------------------------------------ «Cómo se juega» desde la pausa

        protected override bool HowToReady => _loopOn && _phase == Phase.Fly;

        protected override void HowToSuspend()
        {
            // el vuelo queda quieto (el tiempo y lo ganado se conservan); las señales y avisos a la vista se retiran
            _phase = Phase.Idle;
            StopEngine();
            ClearSignals();
            ClearNotices();
            _gates.Clear();
            _gateScheduled = false;
            foreach (var f in _floatViews) { f.Active = false; f.Root.gameObject.SetActive(false); }
            _steerFinger = -1;
            _mouseSteer = false;
        }

        protected override void HowToResume(float spentSeconds)
        {
            Layout();
            _guided = false;
            _route = new PilotRoute(PilotContract.DriveLevel(_driveDda.Level, false));
            _dist = 0f;
            _shipX = _steerX = PilotPlan.Width * 0.5f;
            ExtendRoute();
            ShowFlightLayers(true);
            SetMissionCard();
            _hud.SetLevelText(PilotContract.SectorChip(_sector));            // la práctica dejó el chip en «Sector 1 de 3»
            SetJourney(Endless ? _t / _flightSeconds : (float)(_run.Resolved + _dropped) / _precisionTotal);
            _nextSignalIn = 1.4f;
            UpdateHud();
            _phase = Phase.Fly;
            StartEngine();
            LayoutStrip();
        }

        // ------------------------------------------------------------------ entrada

        /// <summary>El dedo que empieza en la franja de abajo guía la nave hasta que se levanta; cualquier otro toque intenta atrapar una señal. Se lee <c>Input</c> directo (varios dedos a la vez).</summary>
        private void ReadInput(float dt)
        {
            if (_phase != Phase.Fly) { _steerFinger = -1; _mouseSteer = false; return; }
            bool coachBlocks = false;
            if (Input.touchCount > 0)
            {
                bool steering = false;
                for (int i = 0; i < Input.touchCount; i++)
                {
                    var touch = Input.GetTouch(i);
                    if (!ScreenToLogical(touch.position, out Vector2 p)) continue;
                    if (touch.phase == TouchPhase.Began)
                    {
                        coachBlocks = _tutorial != null && _tutorial.Coach != null && _tutorial.Coach.Blocks(touch.position);
                        if (p.y >= _plan.StripTop) { if (_steerFinger < 0) _steerFinger = touch.fingerId; }
                        else if (!coachBlocks && dt > 0f) TapAt(p.x, p.y);
                    }
                    if (touch.fingerId == _steerFinger)
                    {
                        if (touch.phase == TouchPhase.Ended || touch.phase == TouchPhase.Canceled) _steerFinger = -1;
                        else { SteerTo(p.x, dt); steering = true; }
                    }
                }
                if (!steering && _steerFinger >= 0 && !FingerAlive(_steerFinger)) _steerFinger = -1;
                return;
            }
            _steerFinger = -1;

            // Mouse (Editor): arrastrar en la franja de abajo guía; clic arriba atrapa.
            if (!ScreenToLogical(Input.mousePosition, out Vector2 m)) return;
            if (Input.GetMouseButtonDown(0))
            {
                coachBlocks = _tutorial != null && _tutorial.Coach != null && _tutorial.Coach.Blocks(Input.mousePosition);
                if (m.y >= _plan.StripTop) _mouseSteer = true;
                else if (!coachBlocks && dt > 0f) TapAt(m.x, m.y);
            }
            if (Input.GetMouseButtonUp(0)) _mouseSteer = false;
            if (_mouseSteer && Input.GetMouseButton(0)) SteerTo(m.x, dt);
        }

        private bool ScreenToLogical(Vector2 screen, out Vector2 logical)
        {
            logical = Vector2.zero;
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(_play, screen, null, out var local)) return false;
            logical = ToLogical(local);
            return true;
        }

        private static bool FingerAlive(int fingerId)
        {
            for (int i = 0; i < Input.touchCount; i++)
                if (Input.GetTouch(i).fingerId == fingerId) return true;
            return false;
        }

        private void SteerTo(float x, float dt)
        {
            _steerX = Mathf.Clamp(x, 20f, PilotPlan.Width - 20f);
            if (dt > 0f) _steeredSeconds += dt;
        }

        private void PlayClip(AudioClip clip, float volume)
        {
            if (clip == null || !SoundWanted) return;
            _audioSource.PlayOneShot(clip, volume);
        }

        private bool SoundWanted => GameFeel.SoundOn && (_config == null || _config.config == null || _config.config.sound_enabled);

        private void StartEngine()
        {
            if (!SoundWanted || _engineSource == null) return;
            _engineSource.clip = PilotSounds.Engine();
            _engineSource.volume = 0.5f;
            _engineSource.pitch = 0.9f;
            _engineSource.mute = false;
            _engineSource.Play();
        }

        private void StopEngine()
        {
            if (_engineSource != null && _engineSource.isPlaying) _engineSource.Stop();
        }

#if UNITY_EDITOR
        private float _botFlipAt;
        private int _botWrong;

        /// <summary>SOLO EN EL EDITOR (smoke): juega solo. Guía la nave por el centro de la ruta (y de vez en cuando se sale a propósito, para ver las balizas en coral), atrapa casi todas las señales de la misión y, de vez en cuando,
        /// toca una que no era (así el arranque de prueba pasa por los aciertos, los errores y el hiperimpulso). En el teléfono no hace nada.</summary>
        private void AutoPlay()
        {
            if (EditorShotMode || !GuidedTutorial.AutoPlayGame(_t)) return;
            _route.At(_dist, out float center, out float half);
            float off = ((int)(_t / 5f)) % 4 == 3 ? (center < PilotPlan.Width * 0.5f ? 1f : -1f) * (half + 20f) : Mathf.Sin(_t * 0.9f) * half * 0.25f;
            _steerX = Mathf.Clamp(center + off, 20f, PilotPlan.Width - 20f);
            foreach (var s in _signals)
            {
                if (!s.Alive || s.BotSeen || Now - s.At < 0.4f) continue;
                s.BotSeen = true;
                if (s.Kind.IsTarget) { if (_botWrong++ % 9 != 4) TapAt(s.X, s.Y); }
                else if (_botWrong++ % 7 == 3) TapAt(s.X, s.Y);
                break;
            }
        }

        private int _guardReports;

        /// <summary>SOLO EN EL EDITOR: el pedido de Ricardo hecho prueba. Si un aviso (el de sector o hiperimpulso, o un texto flotante) se dibuja sobre una señal viva, el smoke falla (cualquier error de consola lo hace).</summary>
        private void GuardNotices()
        {
            if (_guardReports >= 3 || _signals.Count == 0) return;
            var boxes = new List<Box>();
            if (_notice != null && _bannerGroup.alpha > 0.05f) boxes.Add(_plan.BannerBox);
            foreach (var f in _floatViews) if (f.Active) boxes.Add(f.Area);
            foreach (var s in _signals)
            {
                if (!s.Alive) continue;
                var sb = Box.Around(s.X, s.Y, PilotContract.SignalSize * 0.5f);
                foreach (var b in boxes)
                    if (sb.Intersects(b, 0f))
                    {
                        _guardReports++;
                        Debug.LogError("[SmokeTest] Piloto: un aviso tapa una señal activa (señal en " + Mathf.RoundToInt(s.X) + "," + Mathf.RoundToInt(s.Y) + ", aviso de " + Mathf.RoundToInt(b.X0) + "," + Mathf.RoundToInt(b.Y0) + " a " + Mathf.RoundToInt(b.X1) + "," + Mathf.RoundToInt(b.Y1) + ")");
                        return;
                    }
            }
        }
#endif
    }
}
