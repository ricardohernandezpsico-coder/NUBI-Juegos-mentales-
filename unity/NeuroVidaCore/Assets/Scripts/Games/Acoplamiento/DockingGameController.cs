using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using NeuroVida.Bridge;
using NeuroVida.Contracts;
using NeuroVida.Games.Shared;
using static NeuroVida.Games.Shared.UiKit;
using Motion = NeuroVida.Games.Shared.Motion; // UnityEngine.Motion también existe

namespace NeuroVida.Games.Acoplamiento
{
    /// <summary>
    /// «Acoplamiento: muelle de acoplamiento» (id <c>acoplamiento</c>, renovado el 10-oct; ver <see cref="DockingContract"/> y docs/diseno-acoplamiento.md; el boceto aprobado es docs/previews/acoplamiento-boceto.html): ROTACIÓN MENTAL (Shepard y Metzler, 1971). Estás armando una estación espacial: llega un módulo girado y hay
    /// que decidir si ENCAJA en el hueco del puerto (es la misma pieza, girada) o es su ESPEJO (dado vuelta). Los dos aciertos suman un módulo a la estación (si era espejo y lo dijiste, el muelle lo da vuelta y también se acopla); cada 8 módulos se completa un anillo. SIEMPRE se ve la verdad:
    /// el módulo gira hasta quedar derecho, se da vuelta si era espejo y baja al puerto; si acertaste vuela a su casillero de la estación y, si no, se aleja sin castigo. Un módulo: llega (420 ms) → decidir (Reto: de 6 a 2,5 s según el nivel, con su barra de «Tiempo»; Precisión sin apuro) → revelación.
    /// Reto de 120 s; Precisión de 24 módulos. La pieza cabe en 4 × 4 bloques; la luz y la sombra quedan fijas en la pantalla (no giran con la pieza, para no delatar el giro) y nada gira en el fondo. Los avisos van en una franja fija (y 246-278) y NUNCA sobre el módulo, el puerto ni los botones (el smoke lo comprueba).
    /// Todo el tiempo va con <see cref="GameClock"/> y lo que se mueve con <see cref="Motion"/>: con «quitar animaciones» el módulo aparece de una vez y cada paso de la revelación (giro, vuelta, bajada, vuelo) cambia de golpe, y los chevrones no parpadean.
    /// </summary>
    public sealed partial class DockingGameController : GameControllerBase
    {
        public const string GameId = DockingContract.GameId;

        private const float UnitsPerDp = 3f;
        private const float MarginU = 60f;
        private const int NoAnswer = -2;

        /// <summary>SOLO EN EL EDITOR, para las capturas de pantalla (<c>verificar-todo.sh --capturas Acoplamiento</c>): con esta bandera el juego NO se juega solo y un guion (<see cref="EditorShotScript"/>) lo lleva por los momentos que se fotografían. En el teléfono es siempre false.</summary>
        public static bool EditorShotMode;

        private enum Phase { Idle, Arrive, Decide, Reveal, Done }

        // ------------------------------------------------------------------ estado

        private System.Random _rng;
        private AdaptiveDifficulty _dda;
        private Phase _phase = Phase.Idle;
        /// <summary>true mientras corre <see cref="RevealRoutine"/> (la fase sigue siendo «Reveal» hasta que alguien pasa a la siguiente: el juego en <c>EndTrial</c>, la práctica del tutorial por su cuenta).</summary>
        private bool _revealing;
        private bool _loopOn, _baked, _guided, _inputOn, _landingPending, _revealDemo;
        private int _previousFrameRate, _lastTickSecond;
        private float _endsAt;

        // el módulo en curso
        private DockingTrial _trial;
        private int _answer = NoAnswer, _shownAnswer = NoAnswer;
        private float _decideAt, _answerAt, _deadline, _arriveAt, _revealAt = -10f;
        private bool _shownCorrect;
        private readonly float[] _pressAt = { -10f, -10f };

        // la partida
        private readonly List<int> _disparities = new List<int>();
        private readonly List<float> _rts = new List<float>();
        private readonly List<bool> _corrects = new List<bool>();
        private int _trials, _correct, _docked, _rings, _streak, _bestStreak, _bestRecord, _stationShown;

        // los tiempos y el largo de la partida (el arranque de prueba del Editor los acorta para llegar al final)
        private int _trialsTotal = DockingContract.PrecisionTrials;
        private float _retoSeconds = DockingContract.RetoSeconds;

        private ExitButton _exit;
        private GameHud _hud;
        private CountdownScreen _countdown;

        private bool Endless => _config != null && _config.config.timed;
        private bool Precision => !Endless;
        private float Now => GameClock.Time;

        // ------------------------------------------------------------------ sesión

        public void StartSession(SequenceInitConfig config)
        {
            _config = config;
            _rng = new System.Random();
            var age = DdaUserProfileConfig.ParseAgeBand(config.config.age_band);
            float start = AdaptiveDifficulty.StartRating(config.config, DockingContract.MaxLevel);
            // ~25 módulos por partida. Sin modulación por tiempo: el tiempo ES la medida (no debe mover la dificultad).
            _dda = new AdaptiveDifficulty(DockingContract.MaxLevel, age, start, stepUp: 0.3f, useReaction: false);
            _bestRecord = Mathf.Max(0, config.config.dock_best);
            _trialsTotal = DockingContract.PrecisionTrials;
            _retoSeconds = DockingContract.RetoSeconds;
#if UNITY_EDITOR
            // el smoke juega una partida corta (6 módulos) desde el nivel 5 (piezas de 5 bloques, giros de a 15°)
            if (GuidedTutorial.EditorAutoPlayGame && !EditorShotMode)
            {
                _trialsTotal = 6;
                _retoSeconds = 16f;
            }
#endif
            _previousFrameRate = Application.targetFrameRate;
            Application.targetFrameRate = 60;

            _phase = Phase.Idle;
            _loopOn = _guided = _inputOn = _landingPending = false;
            _disparities.Clear();
            _rts.Clear();
            _corrects.Clear();
            _trials = _correct = _docked = _rings = _streak = _bestStreak = _stationShown = 0;
            _endsAt = 0f;
            _lastTickSecond = -1;
            _trial = null;
            _answer = _shownAnswer = NoAnswer;

            ResetViews();
            _exit.Hide();
            _tutorial.Hide();
            StopAllCoroutines();
            StartCoroutine(GameLoop());
        }

        private void OnDisable()
        {
            if (_previousFrameRate != 0) Application.targetFrameRate = _previousFrameRate;
        }

        /// <summary>Las piezas se hornean en cada módulo: al cerrar el juego se liberan sus texturas.</summary>
        private void OnDestroy() => ReleasePiece();

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
            yield return StartCoroutine(_countdown.Play(DockingContract.Title, Assessment.Subtitle(DockingContract.CountdownSub), () => _safe.gameObject.SetActive(true)));
            while (!_baked) yield return null;
            _safe.gameObject.SetActive(true);
            yield return null;
            ApplySafeArea(_safe);
            Canvas.ForceUpdateCanvases();
            Layout();
            _endsAt = Now + _retoSeconds;
            _timerTrack.gameObject.SetActive(Endless);
            _timerFill.gameObject.SetActive(Endless);
            UpdateHud();
            _loopOn = true;
            yield return StartCoroutine(MainLoop());
        }

        /// <summary>El bucle de la partida: un módulo tras otro hasta que se cumplan los módulos (Precisión) o el tiempo (Reto). «Cómo se juega» lo retoma desde acá.</summary>
        private IEnumerator MainLoop()
        {
            while (!Finished()) yield return StartCoroutine(PlayTrial());
            _loopOn = false;
            yield return StartCoroutine(FinishGame());
        }

        private bool Finished() => Precision ? _trials >= _trialsTotal : Now >= _endsAt;

        /// <summary>Hornea los sprites y sintetiza los sonidos durante la cuenta regresiva, de a poco por cuadro (así nada se traba).</summary>
        private IEnumerator Prewarm()
        {
            if (_baked) yield break;
            var sprites = DockingSprites.Prewarm();
            while (sprites.MoveNext()) yield return null;
            _baked = true;
            AssignSprites();
            if (SoundWanted)                                                                  // con «Efectos de sonido» apagado no se calcula ningún clip
            {
                var sounds = DockingSounds.Prewarm();
                while (sounds.MoveNext()) yield return null;
            }
            Layout();
        }

        // ------------------------------------------------------------------ un módulo

        private IEnumerator PlayTrial()
        {
            int level = _dda.PresentedLevel;
            var trial = DockingContract.NextTrial(level, _rng);
#if UNITY_EDITOR
            if (_editorMirror >= 0) trial.Mirrored = _editorMirror == 1;                         // las capturas fuerzan el módulo (espejo o igual, y su giro)
            if (_editorAngle != NoAngle) trial.AngleDeg = _editorAngle;
#endif
            BeginTrial(trial);
            yield return null;                                                                // el horneado de la pieza ya se hizo: el módulo llega en un cuadro limpio
            UpdateHud();
            yield return StartCoroutine(DoArrive());
            yield return StartCoroutine(DoDecide(level));
            EvaluateAnswer(level, out bool correct, out int answer);
            yield return StartCoroutine(RevealRoutine(correct, answer));
            EndTrial();
        }

        /// <summary>Llega el módulo (420 ms, desde arriba y ya girado). Con «quitar animaciones» aparece de una vez.</summary>
        private IEnumerator DoArrive()
        {
            _phase = Phase.Arrive;
            _arriveAt = Now;
            _moduleRoot.gameObject.SetActive(true);
            Play(DockSfx.Arrive);
            float dur = Motion.Decorative ? DockingContract.ArriveMs / 1000f : 0f;
            while (Now - _arriveAt < dur) yield return null;
        }

        /// <summary>Decidir: «Encaja» o «Espejo». La respuesta cuenta al PRESIONAR (el tiempo de respuesta es la medida). En el Reto hay una barra de «Tiempo» (de 6 a 2,5 s según el nivel); en Precisión no hay apuro.</summary>
        private IEnumerator DoDecide(int level)
        {
            _phase = Phase.Decide;
            _answer = NoAnswer;
            _decideAt = Now;
            _deadline = DockingContract.DeadlineMs(level, Precision) / 1000f;
#if UNITY_EDITOR
            if (GuidedTutorial.EditorAutoPlayGame && !EditorShotMode) _deadline = Mathf.Min(_deadline, 1.6f);                  // el smoke no espera 6 s para ver «Sin tiempo»
            if (_editorDeadline > 0f) _deadline = _editorDeadline;                                                         // y las capturas, tampoco
#endif
            _inputOn = true;
            SetFuelVisible(Endless);
            _hint.gameObject.SetActive(_trials < DockingContract.HintModules);
            while (_answer == NoAnswer && Now - _decideAt < _deadline)
            {
                SetFuel(1f - (Now - _decideAt) / _deadline);
                yield return null;
            }
            _inputOn = false;
            _hint.gameObject.SetActive(false);
            SetFuelVisible(false);
        }

        /// <summary>Lo que cuenta la respuesta (o su falta): aciertos, racha, motor, medidas. Sin tiempo es un error («Sin tiempo») y la verdad se muestra igual.</summary>
        private void EvaluateAnswer(int level, out bool correct, out int answer)
        {
            bool timedOut = _answer == NoAnswer;
            answer = timedOut ? DockingContract.AnswerTimeout : _answer;
            correct = DockingContract.IsCorrect(_trial.Mirrored, answer);
            float rtMs = timedOut ? _deadline * 1000f : (_answerAt - _decideAt) * 1000f;
            _trials++;
            _disparities.Add(_trial.Disparity);
            _rts.Add(rtMs);
            _corrects.Add(correct);
            if (correct) _correct++;
            _streak = correct ? _streak + 1 : 0;
            _bestStreak = Mathf.Max(_bestStreak, _streak);
            _dda.Register(correct);
            UpdateHud();
#if UNITY_EDITOR
            Debug.Log("[SmokeTest] Acoplamiento: módulo " + _trials + " nivel " + level + (_trial.Mirrored ? " espejo" : " igual") + " " + _trial.AngleDeg + "°: " + (timedOut ? "sin tiempo" : answer == DockingContract.AnswerFits ? "dijo «Encaja»" : "dijo «Espejo»") + " → " + (correct ? "acierto" : "error") + ", " + rtMs.ToString("0") + " ms, racha " + _streak);
#endif
        }

        private void EndTrial()
        {
            _phase = Phase.Idle;
            HideModule();
            SetNotice("", Lime, false);
        }

        // ------------------------------------------------------------------ la revelación (siempre se ve la verdad)

        /// <summary>
        /// La revelación: el módulo gira hasta quedar derecho (320 ms), se da vuelta si era espejo (300 ms), baja al puerto (380 ms) y, si acertaste, el borde y los chevrones se ponen lima, 220 ms después vuela a su casillero de la estación (520 ms) y se enciende; cada 8 módulos, «¡Anillo completo!».
        /// Si no acertaste, el borde se pone celeste («mostrar») y el módulo se aleja hacia la derecha mientras se apaga (600 ms), sin castigo. El movimiento sale del TIEMPO de la revelación (<see cref="RevealT"/>), así las capturas pueden detenerlo a mitad de un paso.
        /// </summary>
        private IEnumerator RevealRoutine(bool correct, int answer, bool demo = false)
        {
            _phase = Phase.Reveal;
            _revealing = true;
            _revealAt = Now;
            _revealDemo = demo;
            _shownAnswer = demo ? NoAnswer : answer;
            _shownCorrect = correct;
            _landingPending = correct && !demo;
            if (!demo) SetNotice(DockingContract.Notice(_trial.Mirrored, answer, _streak), correct ? Lime : Gold, false);
            RefreshButtons(Now);
            float tA = RevealUp + (_trial.Mirrored ? RevealFlip : 0f);
            float end = demo ? tA + RevealDown + 0.9f : correct ? tA + RevealDown + RevealHold + RevealFly + RevealAfter : tA + RevealDown + RevealGone + 0.2f;
            bool soundFlip = false, soundDock = false, soundMiss = false;
            while (true)
            {
                float t = RevealT();
                if (_trial.Mirrored && !soundFlip && t >= RevealUp) { soundFlip = true; Play(DockSfx.Flip); }
                if (correct || demo)
                {
                    if (!soundDock && t >= tA + RevealDown)
                    {
                        soundDock = true;
                        Play(DockSfx.Dock);
                        GameFeel.Haptic(GameFeel.HapticKind.Firm);
                    }
                    if (_landingPending && t >= tA + RevealDown + RevealHold + RevealFly) Land();
                }
                else if (!soundMiss && t >= tA + 0.12f)
                {
                    soundMiss = true;
                    Play(DockSfx.Miss);
                    GameFeel.Haptic(GameFeel.HapticKind.Double);
                }
                if (t >= end) break;
                yield return null;
            }
            if (_landingPending) Land();
            _shownAnswer = NoAnswer;
            _revealDemo = false;
            _revealing = false;
            RefreshButtons(Now);
        }

        private const float RevealUp = DockingContract.UpMs / 1000f, RevealFlip = DockingContract.FlipMs / 1000f, RevealDown = DockingContract.DownMs / 1000f, RevealHold = DockingContract.HoldMs / 1000f,
            RevealFly = DockingContract.FlyMs / 1000f, RevealGone = DockingContract.GoneMs / 1000f, RevealAfter = DockingContract.AfterDockMs / 1000f;

        /// <summary>El tiempo de la revelación, en segundos (las capturas lo detienen con <c>_editorRevealT</c>).</summary>
        private float RevealT()
        {
#if UNITY_EDITOR
            if (_editorRevealT >= 0f) return _editorRevealT;
#endif
            return Now - _revealAt;
        }

        /// <summary>El módulo llegó a su casillero: se suma a la estación (y a la partida, salvo en la práctica del tutorial), se enciende con un aro blanco y suena la kalimba que sube con la racha; cada 8, «¡Anillo completo!».</summary>
        private void Land()
        {
            _landingPending = false;
            int index = _stationShown;
            _stationShown++;
            if (!_guided) _docked++;
            _slotFlashAt = Now;
            _slotFlashIndex = index;
            RefreshStation();
            UpdateHudExtras();
            Play(DockSfx.Slot, _guided ? 1 : _streak);
            GameFeel.Haptic(GameFeel.HapticKind.Light);
            if (DockingContract.CompletesRing(_stationShown))
            {
                if (!_guided) _rings++;
                SetNotice(DockingContract.RingComplete, Cyan, true);
                StartCoroutine(PlayLater(DockSfx.Ring, 0.26f));
            }
        }

        private IEnumerator PlayLater(DockSfx sfx, float seconds)
        {
            float t = 0f;
            while (t < seconds) { t += GameClock.DeltaTime; yield return null; }
            Play(sfx);
        }

        // ------------------------------------------------------------------ fin

        private IEnumerator FinishGame()
        {
            _phase = Phase.Done;
            int record = Mathf.Max(_bestRecord, _docked);
            bool broke = _docked > _bestRecord;
            float accuracy = _trials > 0 ? (float)_correct / _trials : 0f;
            int speed = DockingContract.RotationSpeed(_disparities, _rts, _corrects);
            int[] curve = DockingContract.Curve(_disparities, _rts, _corrects);
            int score = DockingContract.Score(accuracy, _dda.PeakLevel);
            float rtSum = 0f;
            int rtN = 0;
            for (int i = 0; i < _rts.Count; i++) if (_corrects[i]) { rtSum += _rts[i]; rtN++; }
#if UNITY_EDITOR
            // el contador de arriba, la estación y el título «N módulos acoplados» tienen que decir lo mismo (cada acierto se acopla una sola vez): si no, el smoke falla
            if (!EditorShotMode && (_docked != _correct || _stationShown != _docked))
                Debug.LogError("[SmokeTest] Acoplamiento: los acoplados (" + _docked + "), la estación (" + _stationShown + ") y los aciertos (" + _correct + ") no coinciden");
            Debug.Log("[SmokeTest] Acoplamiento: partida terminada: " + _docked + " módulos, " + _rings + " anillos, " + _correct + " de " + _trials + ", racha mayor " + _bestStreak + ", giro " + speed + " °/s, nivel " + _dda.Level);
#endif
            HideModule();
            ShowEnd(record, broke, speed, curve);
            _exit.Show();
            Play(DockSfx.Finale);
            var telemetry = new StroopTelemetry
            {
                user_id = _config.user_id,
                game_id = _config.game_id,
                session_metrics = new StroopSessionMetrics
                {
                    correct_trials = _correct,
                    total_trials = _trials,
                    calculated_score = score,
                    average_response_time_ms = rtN > 0 ? (int)(rtSum / rtN) : 0,
                    level = _config.config.level,
                    timed = _config.config.timed,
                    end_rating = _dda.RatingNormalized,
                    mode_trials = _dda.ScoredTrials,
                    mode_hits = _dda.ScoredCorrect,
                    peak_level = _dda.PeakLevel,
                    rotation_speed_dps = speed,
                    rotation_curve_ms = curve,
                    docked = _docked,
                    rings = _rings,
                    dock_best = record,
                    dock_new = broke ? 1 : 0,
                }
            };
            NativeBridge.ForwardTelemetryToPlatform(JsonUtility.ToJson(telemetry));
            yield break;
        }

        // ------------------------------------------------------------------ pieza del tutorial

        /// <summary>El tutorial guiado común sobre el área segura (se llama al final de <c>BuildUi</c>, para que quede encima de todo).</summary>
        private void SetUpTutorial() =>
            BuildTutorial(_safe, GameHud.Height + 10f, DockingContract.Title, "Llegan módulos girados: decide si encajan en el puerto o son su espejo.", badgeAtBottom: true);

        // ------------------------------------------------------------------ «Cómo se juega» desde la pausa

        protected override bool HowToReady => _loopOn && _phase != Phase.Done && _phase != Phase.Idle;

        protected override void HowToSuspend()
        {
            // el módulo a medias no cuenta: se vuelve a jugar entero (lo ganado hasta ahora se conserva; si ya había acertado, el módulo se acopla)
            if (_landingPending) Land();
            _phase = Phase.Idle;
            _inputOn = false;
            _shownAnswer = NoAnswer;
            HideModule();
            SetNotice("", Lime, false);
            SetFuelVisible(false);
            _hint.gameObject.SetActive(false);
            RefreshButtons(Now);
        }

        protected override void HowToResume(float spentSeconds)
        {
            if (Endless) _endsAt += spentSeconds;                // el tiempo del tutorial no se le descuenta al Reto
            Layout();
            UpdateHud();
            StartCoroutine(MainLoop());
        }

        // ------------------------------------------------------------------ entrada

        private void Update()
        {
            if (PollTutorialSkip()) return;             // un toque en «Saltar tutorial» no es un toque al juego
            float now = Now, dt = GameClock.DeltaTime;
            UpdateClock();
            ReadInput();
            Animate(now, dt);
#if UNITY_EDITOR
            if (_phase == Phase.Decide && !_guided && dt > 0f) AutoPlay(dt);
            GuardNotices();
#endif
        }

        private void UpdateClock()
        {
            if (!Endless || _guided || !_loopOn || _phase == Phase.Done || _endsAt <= 0f) return;
            float left = _endsAt - Now;
            SetTimer(left / _retoSeconds);
            int whole = Mathf.CeilToInt(left);
            if (whole <= 5 && whole >= 1 && whole != _lastTickSecond)
            {
                _lastTickSecond = whole;
                GameFeel.Tick();
            }
        }

        private void ReadInput()
        {
            if (!_inputOn || _phase != Phase.Decide || _answer != NoAnswer) return;
            if (!GuidedTutorial.TryPress(out Vector2 pos)) return;
            if (_tutorial != null && _tutorial.Coach != null && _tutorial.Coach.Blocks(pos)) return;
            if (GameClock.DeltaTime <= 0f) return;                         // en pausa los toques los recibe el paso o el menú, no el juego
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(_play, pos, null, out var local)) return;
            var p = ToLogical(local);
            for (int i = 0; i < 2; i++)
            {
                var b = _plan.ButtonBox(i);
                if (p.x >= b.X0 && p.x <= b.X1 && p.y >= b.Y0 && p.y <= b.Y1) { PressButton(i); return; }
            }
        }

        /// <summary>Un toque en un botón (0 = «Encaja», 1 = «Espejo»): la respuesta cuenta al presionar.</summary>
        private void PressButton(int i)
        {
            if (_answer != NoAnswer) return;
            _answer = i;
            _answerAt = Now;
            _pressAt[i] = Now;
            Play(DockSfx.Tap);
            GameFeel.Haptic(GameFeel.HapticKind.Light);
        }

        /// <summary>Un sonido de «Madera cálida» (<see cref="DockingSounds"/>): con «Efectos de sonido» apagado no se calcula ni suena nada. Volumen 0,8: el del laboratorio donde Ricardo lo eligió.</summary>
        private void Play(DockSfx sfx, int arg = 0)
        {
            if (!SoundWanted) return;
            var clip = DockingSounds.Get(sfx, arg);
            if (clip != null) _audioSource.PlayOneShot(clip, 0.8f);
        }

        private bool SoundWanted => GameFeel.SoundOn && (_config == null || _config.config == null || _config.config.sound_enabled);

        private static IEnumerator Wait(float seconds)
        {
            float t = 0f;
            while (t < seconds)
            {
                t += GameClock.DeltaTime;
                yield return null;
            }
        }

#if UNITY_EDITOR
        private float _botT;
        private int _botPick = NoAnswer;
        private int _guardReports;

        /// <summary>SOLO EN EL EDITOR (smoke): juega solo. Contesta bien casi siempre, una vez de cada tres se equivoca a propósito (para ver «Era su espejo» y «Sí encajaba») y una de cada cinco no contesta (para ver «Sin tiempo»). En el teléfono no hace nada.</summary>
        private void AutoPlay(float dt)
        {
            if (EditorShotMode || !GuidedTutorial.EditorAutoPlayGame || _trial == null || _answer != NoAnswer) return;
            if (_botPick == NoAnswer)
            {
                bool silent = _trials % 5 == 4;
                bool wrong = _trials % 3 == 1;
                _botPick = silent ? -1 : ((_trial.Mirrored ? 1 : 0) ^ (wrong ? 1 : 0));
                _botT = 0f;
            }
            _botT += dt;
            if (_botT < 0.5f || _botPick < 0) return;
            int pick = _botPick;
            _botPick = NoAnswer;
            PressButton(pick);
        }

        /// <summary>SOLO EN EL EDITOR: el pedido de Ricardo hecho prueba. El aviso (con su píldora y su texto) nunca toca el módulo, el puerto ni los botones: si se cruzan, el smoke falla (cualquier error de consola lo hace).</summary>
        private void GuardNotices()
        {
            if (_guardReports >= 3 || _s <= 0f || _phase == Phase.Done || !_noticePill.gameObject.activeSelf) return;
            var rt = _noticePill.rectTransform;
            var c = ToLogical(rt.anchoredPosition);
            var box = new DockBox(c.x - rt.sizeDelta.x / _s * 0.5f, c.y - rt.sizeDelta.y / _s * 0.5f, c.x + rt.sizeDelta.x / _s * 0.5f, c.y + rt.sizeDelta.y / _s * 0.5f);
            var zones = new[] { ("el módulo", _plan.ModuleBox), ("el puerto", _plan.PortBox), ("«Encaja»", _plan.ButtonBox(0)), ("«Espejo»", _plan.ButtonBox(1)), ("la barra de tiempo", _plan.FuelBox) };
            foreach (var z in zones)
                if (box.Intersects(z.Item2))
                {
                    _guardReports++;
                    Debug.LogError("[SmokeTest] Acoplamiento: un aviso tapa " + z.Item1 + " (aviso de " + Mathf.RoundToInt(box.X0) + "," + Mathf.RoundToInt(box.Y0) + " a " + Mathf.RoundToInt(box.X1) + "," + Mathf.RoundToInt(box.Y1) + ")");
                    return;
                }
        }
#endif
    }
}
