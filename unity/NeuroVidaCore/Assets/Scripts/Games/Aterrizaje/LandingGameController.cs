using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using NeuroVida.Bridge;
using NeuroVida.Contracts;
using NeuroVida.Games.Shared;
using static NeuroVida.Games.Shared.UiKit;
using Motion = NeuroVida.Games.Shared.Motion; // UnityEngine.Motion también existe

namespace NeuroVida.Games.Aterrizaje
{
    /// <summary>
    /// «Aterrizaje Lunar: la misma tarea, más grande, con más vida y un final con sentido» (id <c>aterrizaje</c>, renovado el 10-oct; ver <see cref="LandingContract"/> y docs/diseno-aterrizaje.md; el boceto aprobado es docs/previews/aterrizaje-boceto.html): ESTIMACIÓN EN LA LÍNEA NUMÉRICA (Siegler y Opfer, 2003). Arriba, la misión («Aterriza en» y el número en grande);
    /// abajo, la luna con una regla de arcilla que solo marca sus extremos. La nave baja sola (de 6 a 3,5 s según el nivel; Precisión 10 s): el dedo, en CUALQUIER parte del cielo, la mueve a lo ancho (con una guía de puntos hasta la regla) y al soltar cae en 320 ms; si la bajada termina sin soltar, se posa donde está.
    /// Al posarse: polvo, el tramo entre la nave y el lugar justo (lima con ✓ o coral con aspa: nunca solo color), una bandera de 80 dp en el lugar justo con su número (en las sumas, el RESULTADO) y un aviso en el cielo vacío («¡Diana lunar!», «¡Justo! a 3 del blanco», «Cerca: a 3 del blanco»). Cada 5 aterrizajes justos se arma una cúpula de tu base (SOLO en el marcador:
    /// nada fijo cerca de la regla, serviría de pista). Las dos cordilleras lunares se desplazan despacio y con «quitar animaciones» NO se dibujan. Reto de 120 s o Precisión de 15 aterrizajes (8 en la versión corta del inicio). Todo el tiempo va con <see cref="GameClock"/> y lo que se mueve con <see cref="Motion"/>.
    /// Los avisos van en el cielo vacío, DESPUÉS de posarse, y NUNCA sobre la nave, la regla, la bandera ni la misión (el smoke lo comprueba). El final de la app (<c>GameResultScreen</c>) ordena lo que hiciste, tu avance contigo, un truco y, abajo y en chico, por qué importa.
    /// </summary>
    public sealed partial class LandingGameController : GameControllerBase
    {
        public const string GameId = LandingContract.GameId;

        private const float UnitsPerDp = 3f;
        private const float MarginU = 60f;

        /// <summary>SOLO EN EL EDITOR, para las capturas de pantalla (<c>verificar-todo.sh --capturas Aterrizaje</c>): con esta bandera el juego NO se juega solo y un guion (<see cref="EditorShotScript"/>) lo lleva por los momentos que se fotografían. En el teléfono es siempre false.</summary>
        public static bool EditorShotMode;

        private enum Phase { Idle, Fall, Drop, Land, Done }

        // ------------------------------------------------------------------ estado

        private System.Random _rng;
        private AdaptiveDifficulty _dda;
        private Phase _phase = Phase.Idle;
        private bool _loopOn, _baked, _guided;
        private int _previousFrameRate, _lastTickSecond;
        private float _endsAt;
        private AudioSource _hoverSource;

        private ExitButton _exit;
        private GameHud _hud;
        private CountdownScreen _countdown;

        private bool Endless => _config != null && _config.config.timed && !Assessment.Active;
        private bool Precision => !Endless;
        private float Now => GameClock.Time;

        // el aterrizaje en curso
        private LandingTrial _trial;
        private float _x, _tx, _y, _y0, _fallAt, _dropAt, _descent, _landAt, _holdT = 2.3f, _popAt = -10f;
        private bool _released, _touching, _everTouched;

        /// <summary>Cómo salió el aterrizaje (se calcula al tocar la regla).</summary>
        private sealed class Landing
        {
            public float Frac, Value, Err, TargetFrac;
            public LandingContract.Outcome Outcome;
            public bool Hit => Outcome != LandingContract.Outcome.Far;
            public bool Bull => Outcome == LandingContract.Outcome.Bull;
            public string Notice;
            public bool DomeEarned;
            public int Streak;
        }

        private Landing _land;

        // la partida
        private readonly List<float> _errors = new List<float>(), _trueFractions = new List<float>(), _givenFractions = new List<float>();
        private int _trials, _hits, _bulls, _streak, _bestStreak, _domes, _shownHits, _shownDomes;
        private int _trialsTotal = LandingContract.PrecisionTrials;
        private float _retoSeconds = LandingContract.RetoSeconds;

        // ------------------------------------------------------------------ sesión

        public void StartSession(SequenceInitConfig config)
        {
            _config = config;
            _rng = new System.Random();
            // ~18 aterrizajes por partida: pasos medianos. Sin tiempo de reacción: cuenta la precisión.
            _dda = LandingContract.CreateEngine(config.config);
            _trialsTotal = LandingContract.TotalTrials(Assessment.Active);
            _retoSeconds = LandingContract.RetoSeconds;
#if UNITY_EDITOR
            // el smoke juega una partida corta: 7 aterrizajes (los suficientes para llegar a la primera cúpula) o 40 s de Reto
            if (GuidedTutorial.EditorAutoPlayGame && !EditorShotMode && !Assessment.Active)
            {
                _trialsTotal = 7;
                _retoSeconds = 40f;
            }
#endif
            // Vuelo continuo y fluido: 60 cuadros por segundo (Android da 30).
            _previousFrameRate = Application.targetFrameRate;
            Application.targetFrameRate = 60;

            _phase = Phase.Idle;
            _loopOn = _guided = false;
            _errors.Clear();
            _trueFractions.Clear();
            _givenFractions.Clear();
            _trials = _hits = _bulls = _streak = _bestStreak = _domes = _shownHits = _shownDomes = 0;
            _endsAt = 0f;
            _lastTickSecond = -1;
            _trial = null;
            _land = null;

            ResetViews();
            _exit.Hide();
            _tutorial.Hide();
            StopAllCoroutines();
            StartCoroutine(GameLoop());
        }

        private void OnDisable()
        {
            if (_previousFrameRate != 0) Application.targetFrameRate = _previousFrameRate;
            if (_hoverSource != null) _hoverSource.Stop();
        }

        private IEnumerator GameLoop()
        {
            StartCoroutine(Prewarm());
            if (TutorialWanted)
            {
                // la ronda guiada se juega sobre la luna ya armada, antes de la cuenta regresiva
                _safe.gameObject.SetActive(true);
                yield return null;
                ApplySafeArea(_safe);
                Canvas.ForceUpdateCanvases();
                Layout();
                yield return StartCoroutine(RunTutorialIfNeeded());
                ResetViews();
            }
            _safe.gameObject.SetActive(false);
            yield return StartCoroutine(_countdown.Play(LandingContract.Title, Assessment.Subtitle(LandingContract.CountdownSub), () => _safe.gameObject.SetActive(true)));
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

        /// <summary>El bucle de la partida: un aterrizaje tras otro. «Cómo se juega» lo retoma desde acá.</summary>
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
            var sprites = LandingSprites.Prewarm();
            while (sprites.MoveNext()) yield return null;
            _baked = true;
            AssignSprites();
            if (SoundWanted)                                                                  // con «Efectos de sonido» apagado no se calcula ningún clip
            {
                var sounds = LandingSounds.Prewarm();
                while (sounds.MoveNext()) yield return null;
                _hoverSource.clip = LandingSounds.HoverLoop();
            }
            Layout();
        }

        // ------------------------------------------------------------------ un aterrizaje

        private float Descent(int level) => LandingContract.DescentSeconds(level, Precision && !Assessment.Active);

        private IEnumerator PlayTrial()
        {
            int level = _dda.PresentedLevel;
            var trial = LandingContract.NextTrial(level, _rng);
#if UNITY_EDITOR
            if (_editorTrial != null)                                                         // las capturas fuerzan el aterrizaje (la regla y el blanco)
            {
                trial = _editorTrial;
                _editorTrial = null;
            }
#endif
            BeginTrial(trial, Descent(level));
            yield return StartCoroutine(WaitForLanding());
            yield return StartCoroutine(ResolveRoutine());
            EndTrial();
        }

        /// <summary>La nave aparece arriba, en un lugar al azar (no siempre en el centro: no regala el medio) y empieza a bajar.</summary>
        private void BeginTrial(LandingTrial trial, float descentSeconds)
        {
            _trial = trial;
            _missionNumber.text = trial.Label;
            _minLabel.text = trial.MinLabel;
            _maxLabel.text = trial.MaxLabel;
            _tickMid.gameObject.SetActive(trial.MidTick);
            ClearResultViews();
            float startFrac = Mathf.Lerp(0.1f, 0.9f, (float)_rng.NextDouble());
            _x = _tx = LandingPlan.FracX(startFrac);
            _y = LandingPlan.StartY;
            _released = _touching = _everTouched = false;
            _fallAt = Now;
            _popAt = Now;
            _descent = Mathf.Max(0.5f, descentSeconds);
            _land = null;
            _phase = Phase.Fall;
            _landerRoot.gameObject.SetActive(true);
            _landerGroup.alpha = 1f;
            _hint.gameObject.SetActive(!_guided && _trials < LandingContract.HintTrials);
            UpdateHud();
#if UNITY_EDITOR
            _botT = 0f;
            _botReleased = false;
#endif
        }

        private IEnumerator WaitForLanding()
        {
            while (_phase == Phase.Fall || _phase == Phase.Drop) yield return null;
        }

        /// <summary>Un cuadro de vuelo: baja sola (de y 196 a 50 dp sobre la regla, en lo que dice el nivel), el dedo la mueve a lo ancho con suavizado y, al soltar, cae en 320 ms. Termina cuando toca la regla.</summary>
        private void StepFlight(float now, float dt)
        {
            if (_phase != Phase.Fall && _phase != Phase.Drop) return;
            _x += (_tx - _x) * Mathf.Min(1f, dt * 14f);
            if (_phase == Phase.Fall)
            {
                float t = now - _fallAt;
                _y = LandingPlan.StartY + (_plan.RestY - LandingPlan.StartY) * Mathf.Clamp01(t / _descent);
                if (t >= _descent) TouchDown();
            }
            else
            {
                float k = Mathf.Clamp01((now - _dropAt) / (LandingContract.DropMs / 1000f));
                _y = _y0 + (_plan.RestY - _y0) * k * k;
                if (k >= 1f) TouchDown();
            }
        }

        /// <summary>Soltar: la nave cae rápido.</summary>
        private void Release()
        {
            if (_phase != Phase.Fall || _released) return;
            _released = true;
            _phase = Phase.Drop;
            _dropAt = Now;
            _y0 = _y;
            Play(LandSfx.Release);
        }

        /// <summary>La nave toca la regla: se mide dónde quedó, se cuenta (salvo en la práctica del tutorial) y empieza el resultado.</summary>
        private void TouchDown()
        {
            _phase = Phase.Land;
            _landAt = Now;
            _x = _tx = Mathf.Clamp(_x, LandingPlan.RulerX0, LandingPlan.RulerX1);
            _y = _plan.RestY;
            float f = LandingPlan.FracOf(_x);
            float value = LandingContract.ValueAt(_trial, f);
            float err = LandingContract.Error(_trial, value);
            var land = new Landing { Frac = f, Value = value, Err = err, TargetFrac = _trial.TargetFraction, Outcome = LandingContract.OutcomeOf(err) };
            if (!_guided)
            {
                _trials++;
                _errors.Add(err);
                _trueFractions.Add(land.TargetFrac);
                _givenFractions.Add(f);
                if (land.Hit) _hits++;
                if (land.Bull) _bulls++;
                _streak = land.Hit ? _streak + 1 : 0;
                _bestStreak = Mathf.Max(_bestStreak, _streak);
                land.DomeEarned = land.Hit && LandingContract.EarnsDome(_hits);
                _domes = LandingContract.Domes(_hits);
                _dda.Register(land.Hit);
            }
            land.Streak = _streak;
            land.Notice = LandingContract.Notice(_trial, value, _streak);
            _land = land;
            _holdT = land.DomeEarned ? Mathf.Max(LandingContract.ResultHoldMs / 1000f, (LandingContract.NoticeDelayMs + LandingContract.DomeNoticeDelayMs) / 1000f + DomeNoticeLife + 0.08f) : LandingContract.ResultHoldMs / 1000f;
            Play(LandSfx.Touch);
            GameFeel.Haptic(GameFeel.HapticKind.Light);
            SpawnDust();
#if UNITY_EDITOR
            Debug.Log("[SmokeTest] Aterrizaje: aterrizaje " + _trials + " nivel " + _trial.Level + " regla " + _trial.MinLabel + "–" + _trial.MaxLabel + " blanco " + _trial.Label + ": quedó a " + (err * 100f).ToString("0.0") + " % → " + (land.Bull ? "diana" : land.Hit ? "justo" : "lejos") + ", racha " + _streak + ", aviso «" + land.Notice + "»");
#endif
        }

        private const float NoticeLife = 1.4f, BullNoticeLife = 1.7f, DomeNoticeLife = 1.7f;

        /// <summary>El tiempo del resultado, en segundos desde que la nave tocó la regla (las capturas lo detienen con <c>_editorResultT</c>).</summary>
        private float ResultT()
        {
#if UNITY_EDITOR
            if (_editorResultT >= 0f) return _editorResultT;
#endif
            return Now - _landAt;
        }

        /// <summary>
        /// Lo que pasa después de posarse, atado al tiempo del resultado: a los 380 ms suena la kalimba de la bandera, a los 620 el aviso (con su sonido) y el marcador (puntos y cúpulas) y, si se armó una cúpula, 900 ms después el aviso de la cúpula con su acorde; la siguiente nave llega a los 2300 ms
        /// (más tarde si hubo cúpula, para que su aviso no se pise con la nave que baja).
        /// </summary>
        private IEnumerator ResolveRoutine()
        {
            var land = _land;
            bool flag = false, notice = false, dome = false;
            float domeAt = (LandingContract.NoticeDelayMs + LandingContract.DomeNoticeDelayMs) / 1000f;
            while (true)
            {
                float t = ResultT();
                if (!flag && t >= LandingContract.FlagDelayMs / 1000f)
                {
                    flag = true;
                    Play(LandSfx.Flag);
                }
                if (!notice && t >= LandingContract.NoticeDelayMs / 1000f)
                {
                    notice = true;
                    ShowResultNotice(land);
                }
                if (land.DomeEarned && !dome && t >= domeAt)
                {
                    dome = true;
                    ShowDomeNotice(land);
                }
                if (t >= _holdT) break;
                yield return null;
            }
        }

        private void ShowResultNotice(Landing land)
        {
            Color color = land.Outcome == LandingContract.Outcome.Far ? Gold : Lime;
            SetNotice(0, land.Notice, color, land.Bull, land.Bull ? BullNoticeLife : NoticeLife, LandingContract.NoticeDelayMs / 1000f);
            if (!_guided)
            {
                _shownHits = _hits;
                _shownDomes = _domes;
                RefreshHudExtras();
            }
            Play(land.Bull ? LandSfx.Bull : land.Hit ? LandSfx.Hit : LandSfx.Miss);
            GameFeel.Haptic(land.Bull ? GameFeel.HapticKind.Firm : land.Hit ? GameFeel.HapticKind.Light : GameFeel.HapticKind.Double);
            if (land.Bull && Motion.Decorative) StartCoroutine(UiFx.SparkBurst(_fxLayer, P(LandingPlan.FracX(land.TargetFrac), _plan.RulerY - 50f), Gold, 22, 300f, 42f, 0.7f));
        }

        private void ShowDomeNotice(Landing land)
        {
            SetNotice(1, LandingContract.DomeNotice, Cyan, !land.Bull, DomeNoticeLife, (LandingContract.NoticeDelayMs + LandingContract.DomeNoticeDelayMs) / 1000f);          // en la fila que no usa el aviso de la diana
            _domePopAt = Now;
            Play(LandSfx.Dome);
            GameFeel.Haptic(GameFeel.HapticKind.Firm);
        }

        private void EndTrial()
        {
            _phase = Phase.Idle;
            _landerRoot.gameObject.SetActive(false);
            ClearResultViews();
        }

        // ------------------------------------------------------------------ fin

        private IEnumerator FinishGame()
        {
            _phase = Phase.Done;
            _landerRoot.gameObject.SetActive(false);
            ClearResultViews();
            StopHover();
            float meanErr = LandingContract.MeanErrorPct(_errors);
            int score = LandingContract.Score(meanErr, _dda.PeakLevel);
#if UNITY_EDITOR
            Debug.Log("[SmokeTest] Aterrizaje: partida terminada: " + _hits + " justos de " + _trials + ", " + _bulls + " dianas, " + _domes + " cúpulas, racha mayor " + _bestStreak + ", a " + meanErr.ToString("0.0") + " %, nivel " + _dda.Level);
            if (!EditorShotMode && _domes != LandingContract.Domes(_hits))
                Debug.LogError("[SmokeTest] Aterrizaje: las cúpulas (" + _domes + ") no son las de los aterrizajes justos (" + _hits + ")");
#endif
            ShowEnd(meanErr);
            _exit.Show();
            Play(LandSfx.Finale);
            var telemetry = new StroopTelemetry
            {
                user_id = _config.user_id,
                game_id = _config.game_id,
                session_metrics = new StroopSessionMetrics
                {
                    correct_trials = _hits,
                    total_trials = _errors.Count,
                    calculated_score = score,
                    average_response_time_ms = 0,
                    level = _config.config.level,
                    timed = _config.config.timed,
                    end_rating = _dda.RatingNormalized,
                    mode_trials = _dda.ScoredTrials,
                    mode_hits = _dda.ScoredCorrect,
                    peak_level = _dda.PeakLevel,
                    numline_error_pct = meanErr,
                    numline_true = _trueFractions.ToArray(),
                    numline_given = _givenFractions.ToArray(),
                    numline_bullseyes = _bulls,
                    domes = _domes,
                    land_streak = _bestStreak,
                }
            };
            NativeBridge.ForwardTelemetryToPlatform(JsonUtility.ToJson(telemetry));
            yield break;
        }

        // ------------------------------------------------------------------ pieza del tutorial

        /// <summary>El tutorial guiado común sobre el área segura (se llama al final de <c>BuildUi</c>, para que quede encima de todo).</summary>
        private void SetUpTutorial() =>
            BuildTutorial(_safe, GameHud.Height + 10f, LandingContract.Title, "Aterriza en el número que te piden. La regla solo marca sus dos extremos.", badgeAtBottom: true);

        // ------------------------------------------------------------------ «Cómo se juega» desde la pausa

        protected override bool HowToReady => _loopOn && _phase != Phase.Done && _phase != Phase.Idle;

        protected override void HowToSuspend()
        {
            // el aterrizaje a medias no cuenta: sigue otro con el mismo estado (lo ganado hasta ahora se conserva)
            _phase = Phase.Idle;
            _touching = false;
            StopHover();
            _landerRoot.gameObject.SetActive(false);
            ClearResultViews();
            _hint.gameObject.SetActive(false);
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
            if (PollTutorialSkip()) return;             // un toque en «Saltar tutorial» no mueve la nave
            float now = Now, dt = GameClock.DeltaTime;
            UpdateClock();
            ReadInput();
            StepFlight(now, dt);
            Animate(now, dt);
#if UNITY_EDITOR
            if (_phase == Phase.Fall && !_guided && dt > 0f) AutoPlay(dt);
            GuardNotices();
            GuardWorld();
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

        /// <summary>El dedo (o el ratón): en CUALQUIER parte del cielo (de y 160 hacia abajo) mueve la nave a lo ancho; al soltar, cae. El que empezó en el marcador no cuenta.</summary>
        private void ReadInput()
        {
            if (_phase != Phase.Fall || GameClock.DeltaTime <= 0f) return;                   // en pausa los toques los recibe el paso o el menú, no el juego
            bool down = Input.touchCount > 0 || Input.GetMouseButton(0);
            if (down)
            {
                Vector2 pos = Input.touchCount > 0 ? Input.GetTouch(0).position : (Vector2)Input.mousePosition;
                if (_tutorial != null && _tutorial.Coach != null && !_touching && _tutorial.Coach.Blocks(pos)) return;
                if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(_play, pos, null, out var local)) return;
                var p = ToLogical(local);
                if (!_touching && p.y <= LandingPlan.TouchTopY) return;
                _tx = Mathf.Clamp(p.x, LandingPlan.RulerX0, LandingPlan.RulerX1);
                _touching = true;
                _everTouched = true;
            }
            else if (_touching)
            {
                _touching = false;
                Release();
            }
        }

        /// <summary>Un sonido de «Madera cálida» (<see cref="LandingSounds"/>): con «Efectos de sonido» apagado no se calcula ni suena nada. Volumen 0,8: el del laboratorio donde Ricardo lo eligió.</summary>
        private void Play(LandSfx sfx)
        {
            if (!SoundWanted) return;
            var clip = LandingSounds.Get(sfx);
            if (clip != null) _audioSource.PlayOneShot(clip, 0.8f);
        }

        private bool SoundWanted => GameFeel.SoundOn && (_config == null || _config.config == null || _config.config.sound_enabled);

#if UNITY_EDITOR
        private float _botT;
        private bool _botReleased;
        private int _guardReports;

        /// <summary>
        /// SOLO EN EL EDITOR (smoke): juega solo, una conducta por aterrizaje: 0 y 4 = justo en el blanco (diana), 1, 3 y 5 = cerca (justo), 2 = lejos (casi a un tercio de la regla) y 6 = no toca nada (se posa donde empezó). Suelta a los 1,2 s. En el teléfono no hace nada.
        /// </summary>
        private void AutoPlay(float dt)
        {
            if (EditorShotMode || !GuidedTutorial.EditorAutoPlayGame || _trial == null || _botReleased) return;
            int kind = _trials % 7;
            _botT += dt;
            float tf = _trial.TargetFraction;
            float f = kind == 0 || kind == 4 ? tf : kind == 1 || kind == 3 || kind == 5 ? tf + (tf < 0.5f ? 0.03f : -0.03f) : kind == 2 ? (tf < 0.5f ? Mathf.Min(1f, tf + 0.34f) : Mathf.Max(0f, tf - 0.34f)) : -1f;
            if (_botT < 0.5f) return;
            if (f >= 0f) _tx = LandingPlan.FracX(Mathf.Clamp01(f));
            if (_botT >= 1.2f)
            {
                _botReleased = true;
                if (f >= 0f) Release();                                                       // el que no toca nada se posa donde está (cuando termina la bajada)
            }
        }

        private float _guardRangeX = float.NaN, _guardRangeAt;
        private bool _guardWorldReported;

        /// <summary>
        /// SOLO EN EL EDITOR: «nada fijo junto a la regla» (docs/diseno-aterrizaje.md §4) hecho prueba. Con animaciones las dos cordilleras se dibujan Y SE MUEVEN (si se quedan quietas cerca de la regla servirían de marca para estimar); con «quitar animaciones» NO se dibujan.
        /// Si no se cumple, el smoke falla (cualquier error de consola lo hace). Los cráteres tenues y solo lejos de la regla los vigila <c>LandingV2Tests</c>; las cúpulas van SOLO en el marcador.
        /// </summary>
        private void GuardWorld()
        {
            if (_guardWorldReported || _phase == Phase.Idle || _phase == Phase.Done || GameClock.DeltaTime <= 0f || _rangeNear == null) return;
            bool drawn = _rangeFar.gameObject.activeSelf || _rangeNear.gameObject.activeSelf;
            if (!Motion.Decorative)
            {
                if (drawn) { _guardWorldReported = true; Debug.LogError("[SmokeTest] Aterrizaje: con «quitar animaciones» se dibuja una cordillera (quieta junto a la regla sería una marca para estimar)"); }
                return;
            }
            if (!drawn) { _guardWorldReported = true; Debug.LogError("[SmokeTest] Aterrizaje: con animaciones las cordilleras no se dibujan"); return; }
            float x = _rangeNear.rectTransform.anchoredPosition.x;
            if (float.IsNaN(_guardRangeX)) { _guardRangeX = x; _guardRangeAt = Now; return; }
            if (Now - _guardRangeAt < 2f) return;
            if (Mathf.Abs(x - _guardRangeX) < 0.01f * _s) { _guardWorldReported = true; Debug.LogError("[SmokeTest] Aterrizaje: las cordilleras están quietas (se tienen que desplazar despacio: quietas junto a la regla serían una marca)"); }
            _guardRangeX = x;
            _guardRangeAt = Now;
        }

        /// <summary>SOLO EN EL EDITOR: el pedido de Ricardo hecho prueba. El aviso (con su píldora y su texto) nunca toca la nave, la regla, la bandera ni la misión: si se cruzan, el smoke falla (cualquier error de consola lo hace).</summary>
        private void GuardNotices()
        {
            if (_guardReports >= 3 || _s <= 0f || _phase == Phase.Done || _land == null) return;
            foreach (var n in _notices)
            {
                if (!n.Live) continue;
                var box = _plan.NoticeBox(n.Big, n.Width);
                var zones = new[] { ("la nave", _plan.LanderBox(_x)), ("la regla", _plan.RulerBox), ("la bandera", _plan.FlagBox(LandingPlan.FracX(_land.TargetFrac))), ("la misión", _plan.MissionBox) };
                foreach (var z in zones)
                    if (box.Intersects(z.Item2))
                    {
                        _guardReports++;
                        Debug.LogError("[SmokeTest] Aterrizaje: un aviso tapa " + z.Item1 + " (aviso de " + Mathf.RoundToInt(box.X0) + "," + Mathf.RoundToInt(box.Y0) + " a " + Mathf.RoundToInt(box.X1) + "," + Mathf.RoundToInt(box.Y1) + ")");
                        return;
                    }
            }
        }
#endif
    }
}
