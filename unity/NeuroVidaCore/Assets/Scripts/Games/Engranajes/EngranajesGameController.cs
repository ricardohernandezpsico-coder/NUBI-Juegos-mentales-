using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using NeuroVida.Bridge;
using NeuroVida.Contracts;
using NeuroVida.Games.Shared;
using static NeuroVida.Games.Shared.UiKit;
using Motion = NeuroVida.Games.Shared.Motion; // UnityEngine.Motion también existe

namespace NeuroVida.Games.Engranajes
{
    /// <summary>
    /// «Engranajes» (Razonamiento; ver <see cref="EngranajesContract"/> y docs/diseno-engranajes.md; el boceto aprobado es docs/previews/engranajes-boceto.html).
    /// A la izquierda la sala de máquinas con el motor; a la derecha el cohete con sus cuatro piezas (antena, compuerta, carga y turbina). Una cadena de engranajes lleva la fuerza
    /// del motor a cada pieza; se mira la máquina y se decide qué hará la que brilla cuando arranque. Al responder la máquina arranca: todo gira junto (los dientes ENCAJAN y nunca se pisan),
    /// un pulso de luz recorre cada unión con su clic y su nota, y cada pieza reacciona con su sonido. Al fallar, Nubi explica y marca el camino con flechas.
    /// Cada acierto enciende una de las 10 luces del cohete (que se guardan entre partidas: <c>engr_lights</c>/<c>engr_orbit</c> de la config y la telemetría) y con la décima el cohete despega ahí mismo.
    /// Precisión = 10 máquinas; Reto = 120 s (las tarjetas «NUEVO» y el despegue no gastan tiempo). Con «quitar animaciones»: sin giro, pulso, chispas, llamas ni temblor; quedan las flechas
    /// de sentido en cada engranaje, la barra arriba o abajo y los mismos tiempos.
    /// </summary>
    public sealed partial class EngranajesGameController : GameControllerBase
    {
        public const string GameId = EngranajesContract.GameId;

        private const float UnitsPerDp = 3f;
        private const float Su = 3f;                 // unidades de la escena por dp del boceto (la escena se escala entera sobre esto)
        private const float MarginU = 60f;
        private const float Accel = 0.45f;           // segundos que tarda la máquina en llegar a su velocidad
        private const float Spin = 2.4f;             // radianes por segundo del motor a velocidad plena

        private enum Phase { Idle, Intro, Play, Reveal, Launch, Done }

        // ------------------------------------------------------------------ estado

        private System.Random _rng;
        private AdaptiveDifficulty _dda;
        private EngranajesTally _tally;
        private RocketState _rocket;
        private int _shownLights, _shownOrbit;
        private Machine _mach;
        private Solution _sol;
        private Piece _placed = Piece.None;
        private Phase _phase = Phase.Idle;
        private bool Endless => _config != null && _config.config.timed;
        private bool Precision => !Endless;
        private bool _senior, _loopOn, _guided, _inputOn, _baked, _okAnswer, _jamRun;
        private int _streak, _bestStreak, _points, _resolved, _okCount;
        private float _endsAt, _t0, _runAt = float.MaxValue, _trickAt = -10f, _introAt, _launchAt, _lastLightAt = -10f, _pressAt = -10f;
        private int _lastTickSecond = -1, _pressIndex = -1;
        private Answer _pending = Answer.None, _answered = Answer.None;
        private bool _introTapped;
        private float[] _baseA = new float[GearPool + 2];
        private float _slotBaseA;
        private Vector2? _press;
        private readonly Dictionary<Station, float> _rackOff = new Dictionary<Station, float>();
        private System.Collections.Generic.List<int> _trickPath = new List<int>();

        // escala y disposición
        private float _s = 3f, _playW = 1080f, _playH = 1920f, _logicalH = 640f;
        private EngranajesLayout.Metrics _lay;

        private ExitButton _exit;
        private GameHud _hud;
        private CountdownScreen _countdown;

        // ------------------------------------------------------------------ sesión

        public void StartSession(SequenceInitConfig config)
        {
            _config = config;
            _rng = new System.Random();
            var age = DdaUserProfileConfig.ParseAgeBand(config.config.age_band);
            _senior = age == AgeBand.Senior;
            float start = AdaptiveDifficulty.StartRating(config.config, EngranajesContract.MaxLevel);
            // 10 máquinas por partida: pasos grandes (dos aciertos seguidos suben una etapa), sin tiempo de reacción: mirar con calma no se penaliza
            _dda = new AdaptiveDifficulty(EngranajesContract.MaxLevel, age, start, stepUp: 0.5f, useReaction: false);
            _tally = new EngranajesTally();
            _rocket = new RocketState(config.config.engr_lights, config.config.engr_orbit);
            _shownLights = _rocket.Lights;
            _shownOrbit = _rocket.Orbit;

            _phase = Phase.Idle;
            _loopOn = _guided = _inputOn = false;
            _press = null;
            _streak = _bestStreak = _points = _resolved = _okCount = 0;
            _endsAt = 0f;
            _lastTickSecond = -1;
            _pending = _answered = Answer.None;
            _trickAt = -10f;
            ClearMachine();

            _resultRoot.gameObject.SetActive(false);
            _exit.Hide();
            _timerTrack.gameObject.SetActive(Endless);
            _hud.SetStreak(0);
            _tutorial.Hide();
            _launchLayer.gameObject.SetActive(false);
            _introLayer.gameObject.SetActive(false);
            RefreshStrip();

            StopAllCoroutines();
            StartCoroutine(GameLoop());
        }

        private IEnumerator GameLoop()
        {
            StartCoroutine(Prewarm());
            if (TutorialWanted)
            {
                // la ronda guiada se juega sobre la sala ya armada, antes de la cuenta regresiva
                _safe.gameObject.SetActive(true);
                yield return null;
                ApplySafeArea(_safe);
                Canvas.ForceUpdateCanvases();
                Layout();
                UpdateHud();
                yield return StartCoroutine(RunTutorialIfNeeded());
                ClearMachine();
            }
            _safe.gameObject.SetActive(false);
            yield return StartCoroutine(_countdown.Play("Engranajes", Assessment.Subtitle("Lleva la fuerza a las piezas"), () => _safe.gameObject.SetActive(true)));
            while (!_baked) yield return null;
            _safe.gameObject.SetActive(true);
            yield return null;
            ApplySafeArea(_safe);
            Canvas.ForceUpdateCanvases();
            Layout();
            UpdateHud();

            _endsAt = GameClock.Time + EngranajesContract.RetoSeconds;
            _loopOn = true;
            yield return StartCoroutine(MainLoop());
        }

        /// <summary>El bucle de la partida: una máquina tras otra hasta que se acabe el tiempo (Reto) o las 10 (Precisión). «Cómo se juega» lo retoma desde acá.</summary>
        private IEnumerator MainLoop()
        {
            while (!Finished()) yield return StartCoroutine(PlayMachine());
            _loopOn = false;
            yield return StartCoroutine(FinishGame());
        }

        private bool Finished()
        {
            if (Endless) return GameClock.Time >= _endsAt;
            return _resolved >= EngranajesContract.PrecisionMachines;
        }

        /// <summary>Hornea los sprites y sintetiza los sonidos durante la cuenta regresiva, de a poco por cuadro (así nada se traba).</summary>
        private IEnumerator Prewarm()
        {
            if (!_baked)
            {
                var sprites = EngranajesSprites.Prewarm();
                while (sprites.MoveNext()) yield return null;
                AssignSprites();
                _baked = true;
            }
            var sounds = EngranajesSounds.Prewarm();
            while (sounds.MoveNext()) yield return null;
        }

        /// <summary>Una máquina: se arma, se mira, se responde, arranca y se cuenta. Si se acaba el tiempo del Reto mientras se mira, no cuenta (ni a favor ni en contra).</summary>
        private IEnumerator PlayMachine()
        {
            var m = EngranajesContract.Generate(_dda.PresentedLevel, _rng);
            ShowMachine(m);
            UpdateHud();
            if (m.Stage.Intro != Intro.None && !IntroSeen(m.Stage.Intro))
            {
                float before = GameClock.Time;
                yield return StartCoroutine(ShowIntro(m.Stage.Intro));
                _endsAt = HowToClock.Shift(_endsAt, GameClock.Time - before);       // la tarjeta «NUEVO» no gasta tiempo del Reto
                MarkIntroSeen(m.Stage.Intro);
            }
            _phase = Phase.Play;
            _inputOn = true;
            _press = null;
            _pending = Answer.None;
            _t0 = GameClock.Time;
            PlayClip(EngranajesSounds.MotorStart(), 0.6f);
            while (_pending == Answer.None)
            {
                if (Endless && GameClock.Time >= _endsAt)
                {
                    _inputOn = false;
                    _phase = Phase.Idle;
                    ClearMachine();
                    yield break;
                }
                yield return null;
            }
            _inputOn = false;
            var chosen = _pending;
            int ms = Mathf.RoundToInt((GameClock.Time - _t0) * 1000f);
            bool launched = RecordMachine(m, chosen == m.Truth, ms);
            yield return StartCoroutine(RevealRoutine(m, chosen, true));
            if (launched) yield return StartCoroutine(LaunchRoutine());
            _phase = Phase.Idle;
        }

        /// <summary>Anota una máquina de la partida: cuentas, DDA, racha y puntos; si acertó suma una luz al cohete. Devuelve true si con esa luz el cohete se completó (despega).</summary>
        private bool RecordMachine(Machine m, bool ok, int ms)
        {
            _tally.Record(ok, ms, m.Level);
            _dda.Register(ok);
            _resolved++;
            bool launched = false;
            if (ok)
            {
                _okCount++;
                _streak++;
                _bestStreak = Mathf.Max(_bestStreak, _streak);
                _points += 10;
                _rocket = _rocket.AddLight(out launched);
                if (launched) _tally.Launched();
            }
            else _streak = 0;
            _hud.SetStreak(_streak);
            UpdateHud();
            return launched;
        }

        private IEnumerator FinishGame()
        {
            _phase = Phase.Done;
            _inputOn = false;
            ClearMachine();
            int score = EngranajesContract.Score(_tally.Correct, Endless);
            ShowResult(score);
            PlayClip(EngranajesSounds.Finale(), 0.8f);

            var telemetry = new StroopTelemetry
            {
                user_id = _config.user_id,
                game_id = _config.game_id,
                session_metrics = new StroopSessionMetrics
                {
                    correct_trials = _tally.Correct,
                    total_trials = _tally.Total,
                    calculated_score = score,
                    average_response_time_ms = _tally.MeanMs < 0 ? 0 : _tally.MeanMs,
                    level = _config.config.level,
                    timed = _config.config.timed,
                    end_rating = _dda.RatingNormalized,
                    mode_trials = _dda.ScoredTrials,
                    mode_hits = _dda.ScoredCorrect,
                    peak_level = _dda.PeakLevel,
                    engr_etapa = _tally.PeakEtapa,
                    engr_lights = _rocket.Lights,
                    engr_orbit = _rocket.Orbit,
                    engr_launches = _tally.Launches,
                    engr_ms = _tally.MeanMs
                }
            };
            NativeBridge.ForwardTelemetryToPlatform(JsonUtility.ToJson(telemetry));
            yield break;
        }

        private void ShowResult(int score)
        {
            _exit.Show();
            _sceneRoot.gameObject.SetActive(false);
            _uiLayer.gameObject.SetActive(false);
            _questionBar.gameObject.SetActive(false);
            _resultRoot.Find("Title").GetComponent<Text>().text = "¡Buen trabajo!";
            _resultRoot.Find("Detail").GetComponent<Text>().text = $"{_tally.Correct} de {_tally.Total} máquinas · etapa {_tally.PeakEtapa} de 5";
            string rocket = _rocket.Lights > 0
                ? $"Faltan {EngranajesContract.RocketLights - _rocket.Lights} luces: tu cohete espera en el hangar"
                : _tally.Launches > 0 ? "¡Tu cohete va a la órbita!" : "Cada acierto enciende una luz del cohete";
            _resultRoot.Find("Extra").GetComponent<Text>().text = rocket;
            _resultRoot.gameObject.SetActive(true);
            StartCoroutine(AnimateResult(score));
        }

        private void UpdateHud()
        {
            _hud.SetLevel(_dda != null ? _dda.PresentedLevel : 1);
            if (Endless) _hud.SetPoints(_points);
            else _hud.SetInfo($"{Mathf.Min(_resolved + 1, EngranajesContract.PrecisionMachines)} de {EngranajesContract.PrecisionMachines}");
        }

        // ------------------------------------------------------------------ tarjetas «NUEVO» (una sola vez por instalación: se guardan en las preferencias de Unity)

        private static bool IntroSeen(Intro intro)
        {
            try { return (PlayerPrefs.GetInt("engr_intros", 0) & (1 << (int)intro)) != 0; }
            catch (System.Exception) { return false; }
        }

        private static void MarkIntroSeen(Intro intro)
        {
            try { PlayerPrefs.SetInt("engr_intros", PlayerPrefs.GetInt("engr_intros", 0) | (1 << (int)intro)); PlayerPrefs.Save(); }
            catch (System.Exception) { }
        }

        private IEnumerator ShowIntro(Intro intro)
        {
            _phase = Phase.Intro;
            _introTapped = false;
            var text = EngranajesContract.IntroText(intro);
            _introTitle.text = "NUEVO";
            _introLine1.text = text.Length > 0 ? text[0] : "";
            _introLine2.text = text.Length > 1 ? text[1] : "";
            _introLayer.gameObject.SetActive(true);
            _introAt = GameClock.Time;
            PlayClip(EngranajesSounds.Chime(), 0.7f);
            _inputOn = true;
            _press = null;
            while (!_introTapped) yield return null;
            _inputOn = false;
            _introLayer.gameObject.SetActive(false);
        }

        // ------------------------------------------------------------------ al responder: arranca la máquina

        /// <summary>La máquina arranca con la respuesta elegida: se ve y se oye por qué (el pulso recorre cada unión, cada pieza hace lo suyo) y se anuncia el resultado. Con <paramref name="count"/> un
        /// acierto suma su luz al cohete (la ronda guiada del tutorial no cuenta).</summary>
        private IEnumerator RevealRoutine(Machine m, Answer v, bool count)
        {
            _phase = Phase.Reveal;
            _answered = v;
            bool ok = v == m.Truth;
            _okAnswer = ok;
            _jamRun = m.Jam;
            var piece = Piece.None;
            float delay = 0f;
            if (m.Q == Question.Build)
            {
                piece = v == Answer.Gear ? Piece.Gear : Piece.Crossed;
                _placed = piece;
                if (piece == Piece.Gear) EngranajesContract.Phase(m, Piece.Gear, _rng);   // los dientes del engranaje nuevo encajan con sus dos vecinos
                PlayClip(EngranajesSounds.Place(), 0.8f);
                delay = Motion.Decorative ? 0.45f : 0f;
            }
            _sol = EngranajesContract.Solve(m, piece);
            CaptureAngles();
            float step = EngranajesContract.StepSeconds(_senior);
            int maxD = 0;
            for (int i = 0; i < _sol.Depth.Length; i++) maxD = Mathf.Max(maxD, _sol.Depth[i]);
            float arrive = delay + maxD * step + 0.2f;
            float hold = ok ? 2.3f : 3.8f;
            if (_senior) hold *= 1.25f;
            float t0 = GameClock.Time;
            _runAt = t0 + delay;
            int nextStep = 0;
            bool jamSounded = false, partSounded = false, resultShown = false;
            while (true)
            {
                float t = GameClock.Time - t0;
                while (nextStep <= maxD && t >= delay + nextStep * step)
                {
                    if (!m.Jam || nextStep == 0) PlayClip(EngranajesSounds.Step(nextStep), 0.9f);
                    nextStep++;
                }
                if (m.Jam && !jamSounded && t >= delay) { jamSounded = true; PlayClip(EngranajesSounds.Jam(), 0.9f); }
                if (!m.Jam && !partSounded && t >= arrive) { partSounded = true; PlayPartSound(m.TargetStation); }
                if (!resultShown && t >= arrive + 0.5f)
                {
                    resultShown = true;
                    if (ok)
                    {
                        PlayClip(EngranajesSounds.Success(), 0.8f);
                        Tell(new[] { "¡Exacto!", "¡Eso es!", "¡Muy bien visto!" }[_okCount % 3], null, true);
                        var at = PartPosition(m.TargetStation);
                        Emit(L(at.x, at.y), 26, 150f, 0.8f, Gold, 20f);
                        if (count) StartFlyer(L(at.x, at.y));
                    }
                    else
                    {
                        PlayClip(EngranajesSounds.Thud(), 0.8f);
                        Tell(EngranajesContract.Explain(m), EngranajesContract.Trick(m), false);
                        _trickAt = GameClock.Time;
                        _trickPath = EngranajesContract.PathTo(m, m.Target, piece != Piece.None);
                    }
                }
                if (t >= arrive + hold) break;
                yield return null;
            }
        }

        private void PlayPartSound(Station s)
        {
            switch (s)
            {
                case Station.Antena: PlayClip(EngranajesSounds.Antenna(), 0.8f); break;
                case Station.Turbina: PlayClip(EngranajesSounds.Turbine(), 0.8f); break;
                default: PlayClip(EngranajesSounds.Rack(), 0.8f); break;
            }
        }

        /// <summary>El despegue: el cohete de la escena tiembla, sube con su llama y su humo y se va a la órbita; después empieza otro y la partida sigue.</summary>
        private IEnumerator LaunchRoutine()
        {
            ClearMachine();                                            // la sala queda vacía: solo el cohete se va
            _phase = Phase.Launch;
            float before = GameClock.Time;
            _launchAt = before;
            _launchNumber = _shownOrbit + 1;
            _launchTitle.text = "¡Despegue!";
            _launchLine1.text = "Tu cohete n.º " + _launchNumber;
            _launchLine2.text = "va a la órbita";
            _launchLayer.gameObject.SetActive(true);
            PlayClip(EngranajesSounds.Launch(), 0.9f);
            float dur = Motion.Decorative ? 3.2f : 1.2f;
            while (GameClock.Time - before < dur) yield return null;
            _launchLayer.gameObject.SetActive(false);
            _shownLights = 0;
            _shownOrbit = _rocket.Orbit;
            RefreshStrip();
            _endsAt = HowToClock.Shift(_endsAt, GameClock.Time - before);      // el despegue tampoco gasta tiempo del Reto
        }

        /// <summary>El tutorial guiado común sobre el área segura (se llama al final de <c>BuildUi</c>, para que quede encima de todo).</summary>
        private void SetUpTutorial() =>
            BuildTutorial(_safe, GameHud.Height + 10f, "Engranajes", "Mira la máquina y decide qué hará la pieza del cohete cuando arranque el motor.", badgeAtBottom: true);

        // ------------------------------------------------------------------ «Cómo se juega» desde la pausa

        protected override bool HowToReady => _loopOn && _phase != Phase.Done;

        protected override void HowToSuspend()
        {
            _phase = Phase.Idle;
            _inputOn = false;
            _introLayer.gameObject.SetActive(false);
            _launchLayer.gameObject.SetActive(false);
            ClearMachine();
        }

        protected override void HowToResume(float spentSeconds)
        {
            _endsAt = HowToClock.Shift(_endsAt, spentSeconds);       // el tiempo que duró «Cómo se juega» no se le descuenta al Reto
            ClearMachine();
            StartCoroutine(MainLoop());
        }

        // ------------------------------------------------------------------ ronda guiada del tutorial (pieza común)

        // <guided>
        protected override IEnumerator GuidedRound(GuidedTutorial t)
        {
            t.BeginPractice();
            _guided = true;
            while (!_baked) yield return null;                         // el arte se hornea mientras Nubi se presenta
            var coach = t.Coach;
            // «Nubi entrenadora»: una máquina fácil (el motor, una cadena corta y una pieza) con tres focos. Cada foco congela el juego; el toque en el hueco es el de verdad:
            // 1) mirar la flecha del motor, 2) mirar la pieza de la pregunta, 3) tocar la respuesta correcta (y se ve por qué).
            var script = new GuidedScript(1);                          // una máquina; «Saltar tutorial» la termina
            var easy = EngranajesContract.Generate(1, _rng);
            ShowMachine(easy);
            _phase = Phase.Play;
            _inputOn = false;
            yield return Motion.Hold(0.6f);
            bool ok = !t.Skipped;
            if (ok)
            {
                yield return StartCoroutine(coach.Touch(() => coach.AroundOf(_motorArrowRect, Vector2.one * SceneUnits(100f)), "El motor gira así", circle: true));
                ok = !t.Skipped;
            }
            if (ok)
            {
                yield return StartCoroutine(coach.Touch(() => coach.AroundOf(_partRoots[(int)easy.TargetStation], Vector2.one * SceneUnits(78f)), "Esta es la pieza de la pregunta", circle: true));
                ok = !t.Skipped;
            }
            int right = IndexOfButton(easy.Truth);
            if (ok)
            {
                yield return StartCoroutine(coach.Touch(() => coach.RectOf(_buttons[right].Root), "Cada engranaje que toca gira al revés"));
                ok = !t.Skipped;
            }
            if (ok)
            {
                _pressIndex = right;
                _pressAt = GameClock.Time;
                yield return StartCoroutine(RevealRoutine(easy, easy.Truth, false));
                coach.Hide();
                yield return StartCoroutine(coach.Notice("¡Eso es! Así se juega", 1.8f));
                yield return StartCoroutine(coach.Notice("¡Listo! Ahora va en serio", 1.5f));
            }
            coach.Hide();
            _inputOn = false;
            _phase = Phase.Idle;
            ClearMachine();
            _guided = false;
            t.EndPractice();
        }
        // </guided>

        // ------------------------------------------------------------------ entrada y animación por cuadro

        private void Update()
        {
            if (PollTutorialSkip()) return;             // un toque en «Saltar tutorial» no es un toque al juego
            float dt = GameClock.DeltaTime;
            float now = GameClock.Time;
            UpdateClock();
            AnimateScene(now, dt);
            AnimateStrip(now);
            AnimateButtons(now);
            AnimateSay(now);
            AnimateFlyer(now);
            AnimateSparks(dt);
            AnimateIntro(now);
            AnimateLaunch(now);
            if (_inputOn && dt > 0f)
            {
                ReadPress();
                HandlePress();
            }
        }

        private void ReadPress()
        {
            bool down;
            Vector2 pos;
            if (Input.touchCount > 0)
            {
                var t = Input.GetTouch(0);
                down = t.phase == TouchPhase.Began;
                pos = t.position;
            }
            else
            {
                down = Input.GetMouseButtonDown(0);
                pos = Input.mousePosition;
            }
            if (!down) return;
            if (_tutorial != null && _tutorial.Coach != null && _tutorial.Coach.Blocks(pos)) return;
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(_play, pos, null, out var local)) return;
            _press = ToLogical(local);
        }

        private void HandlePress()
        {
            if (!_press.HasValue) return;
            var p = _press.Value;
            _press = null;
            if (_phase == Phase.Intro)
            {
                if (GameClock.Time - _introAt > 0.5f) _introTapped = true;
                return;
            }
            if (_phase != Phase.Play || _mach == null) return;
            for (int i = 0; i < _buttonCount; i++)
            {
                if (!_buttons[i].Rect.Contains(p)) continue;
                _pressIndex = i;
                _pressAt = GameClock.Time;
                PlayClip(EngranajesSounds.Press(), 0.6f);
                _pending = _buttons[i].Def.Value;
                return;
            }
        }

        private void UpdateClock()
        {
            if (!Endless || !_loopOn || _endsAt <= 0f || _phase == Phase.Done) return;
            float left = _endsAt - GameClock.Time;
            float frac = Mathf.Clamp01(left / EngranajesContract.RetoSeconds);
            _timerFill.rectTransform.anchorMax = new Vector2(frac, 1f);
            _timerHead.anchorMin = _timerHead.anchorMax = new Vector2(frac, 0.5f);
            int whole = Mathf.CeilToInt(left);
            if (whole <= 5 && whole >= 1 && whole != _lastTickSecond)
            {
                _lastTickSecond = whole;
                GameFeel.Tick();
            }
        }

        private int IndexOfButton(Answer a)
        {
            for (int i = 0; i < _buttonCount; i++) if (_buttons[i].Def.Value == a) return i;
            return 0;
        }

        private float SceneUnits(float dp) => dp * _s * (_lay.SceneScale <= 0f ? 1f : _lay.SceneScale);

        private void PlayClip(AudioClip clip, float volume)
        {
            if (clip == null || !GameFeel.SoundOn) return;
            if (_config != null && _config.config != null && !_config.config.sound_enabled) return;
            _audioSource.PlayOneShot(clip, volume);
        }
    }
}
