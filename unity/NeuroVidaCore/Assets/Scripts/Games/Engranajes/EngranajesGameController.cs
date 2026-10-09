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
    /// «Engranajes: Taller de reparación» (Razonamiento; ver <see cref="EngranajesContract"/> y docs/diseno-engranajes.md; el boceto aprobado es docs/previews/engranajes-taller-boceto.html).
    /// A la izquierda la sala de máquinas con el motor; a la derecha el cohete con sus piezas (antena, compuerta, carga y turbina), cada una con un CARTEL de lo que debe hacer. La máquina viene
    /// mal armada: con una llave (o dos, en las etapas altas) la persona hace cambios tocando el motor (su flecha se da vuelta) o una correa (se cruza o se descruza), y después toca «Arrancar».
    /// La fuerza recorre la máquina (todo gira junto: los dientes ENCAJAN y nunca se pisan; un pulso de luz recorre cada unión con su clic y su nota) y cada cartel se pone verde si la pieza
    /// cumplió o coral si no. Al fallar se marca en celeste lo que movió tu cambio y en dorado lo que había que tocar. Se juzga el RESULTADO, no el camino.
    /// Cada acierto enciende una de las 10 luces del cohete (que se guardan entre partidas: <c>engr_lights</c>/<c>engr_orbit</c> de la config y la telemetría) y con la décima el cohete despega ahí mismo.
    /// Precisión = 10 máquinas; Reto = 120 s (las tarjetas «NUEVO» y el despegue no gastan tiempo). Con «quitar animaciones»: sin giro, pulso, chispas, llamas, temblor ni latidos; la flecha y la correa
    /// cambian al instante, la barra queda arriba o abajo, los carteles con su veredicto y los mismos tiempos.
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
        private PartResult[] _results;
        private readonly List<int> _changes = new List<int>();                // los cambios hechos (ids de interruptor), el más antiguo primero
        private readonly List<int> _beltLinks = new List<int>();              // las uniones que son correa, en el orden de sus vistas
        private readonly Dictionary<int, float> _beltFlipAt = new Dictionary<int, float>();
        private Phase _phase = Phase.Idle;
        private bool Endless => _config != null && _config.config.timed;
        private bool Precision => !Endless;
        private bool _senior, _loopOn, _guided, _inputOn, _baked, _okAnswer, _startRequested;
        private int _streak, _bestStreak, _points, _resolved, _okCount;
        private float _endsAt, _t0, _runAt = float.MaxValue, _verdictAt = float.MaxValue, _showFixAt = -10f, _motorFlipAt, _hintAt, _startPressAt = -10f;
        private float _introAt, _launchAt, _lastLightAt = -10f;
        private int _lastTickSecond = -1, _motorArrowDir;
        private bool _introTapped;
        private readonly float[] _baseA = new float[GearPool];
        private Vector2? _press;
        private readonly Dictionary<Station, float> _rackOff = new Dictionary<Station, float>();

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
            _startRequested = false;
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
            yield return StartCoroutine(_countdown.Play("Engranajes", Assessment.Subtitle("Arregla la máquina del cohete"), () => _safe.gameObject.SetActive(true)));
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

        /// <summary>Una máquina: se arma, se mira, se hacen los cambios, se arranca y se cuenta. Si se acaba el tiempo del Reto mientras se mira, no cuenta (ni a favor ni en contra).</summary>
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
            _startRequested = false;
            _t0 = GameClock.Time;
            _hintAt = _t0;
            PlayClip(EngranajesSounds.MotorStart(), 0.6f);
            float waitedPlay = 0f;
            while (!_startRequested)
            {
                waitedPlay += GameClock.RealDeltaTime;
                if (GuidedTutorial.AutoPlayGame(waitedPlay)) AutoPlayMachine(m);          // solo en el smoke del Editor
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
            var changes = new List<int>(_changes);
            bool ok = EngranajesContract.AllOk(EngranajesContract.Results(m, changes));
            int ms = Mathf.RoundToInt((GameClock.Time - _t0) * 1000f);
            bool launched = RecordMachine(m, ok, ms);
            yield return StartCoroutine(RevealRoutine(m, changes, true));
            if (launched) yield return StartCoroutine(LaunchRoutine());
            _phase = Phase.Idle;
        }

        /// <summary>Anota una máquina de la partida: cuentas, DDA, racha y puntos; si se arregló suma una luz al cohete. Devuelve true si con esa luz el cohete se completó (despega).</summary>
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
            _resultRoot.Find("Detail").GetComponent<Text>().text = $"{_tally.Correct} de {_tally.Total} máquinas arregladas · etapa {_tally.PeakEtapa} de 5";
            string rocket = _rocket.Lights > 0
                ? $"Faltan {EngranajesContract.RocketLights - _rocket.Lights} luces: tu cohete espera en el hangar"
                : _tally.Launches > 0 ? "¡Tu cohete va a la órbita!" : "Cada máquina arreglada enciende una luz del cohete";
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

        // La clave es nueva con el rediseño «Taller de reparación»: las tarjetas de la versión anterior eran otras (y sus bits no valen para estas)
        private const string IntroKey = "engr_intros2";

        private static bool IntroSeen(Intro intro)
        {
            try { return (PlayerPrefs.GetInt(IntroKey, 0) & (1 << (int)intro)) != 0; }
            catch (System.Exception) { return false; }
        }

        private static void MarkIntroSeen(Intro intro)
        {
            try { PlayerPrefs.SetInt(IntroKey, PlayerPrefs.GetInt(IntroKey, 0) | (1 << (int)intro)); PlayerPrefs.Save(); }
            catch (System.Exception) { }
        }

        private IEnumerator ShowIntro(Intro intro)
        {
            _phase = Phase.Intro;
            _introTapped = false;
            _introTitle.text = "NUEVO";
            _introLayer.gameObject.SetActive(true);
            LayoutIntro(EngranajesContract.IntroText(intro));
            _introAt = GameClock.Time;
            PlayClip(EngranajesSounds.Chime(), 0.7f);
            _inputOn = true;
            _press = null;
            float waitedIntro = 0f;
            while (!_introTapped)
            {
                waitedIntro += GameClock.RealDeltaTime;
                if (GuidedTutorial.AutoPlayGame(waitedIntro)) _introTapped = true;          // solo en el smoke del Editor
                yield return null;
            }
            _inputOn = false;
            _introLayer.gameObject.SetActive(false);
        }

        // ------------------------------------------------------------------ al arrancar

        /// <summary>
        /// La máquina arranca con los <paramref name="changes"/> hechos: se ve y se oye cómo (el pulso recorre cada unión, cada pieza hace lo suyo), cada cartel se pone verde o coral y se anuncia
        /// el resultado. Con <paramref name="count"/> un acierto suma su luz al cohete (la ronda guiada del tutorial no cuenta). La siguiente máquina llega 1,9 s después del veredicto si se acertó y
        /// 4,8 s si no (lo que se marca al equivocarse necesita tiempo para mirarse).
        /// </summary>
        private IEnumerator RevealRoutine(Machine m, List<int> changes, bool count)
        {
            _phase = Phase.Reveal;
            _results = EngranajesContract.Results(m, changes);
            bool ok = EngranajesContract.AllOk(_results);
            _okAnswer = ok;
            _sol = EngranajesContract.Solve(m, changes);
            CaptureAngles();
            float step = EngranajesContract.StepSeconds(_senior);
            int maxD = 0;
            for (int i = 0; i < _sol.Depth.Length; i++) maxD = Mathf.Max(maxD, _sol.Depth[i]);
            float arrive = 0.12f + maxD * step + 0.25f;
            float hold = ok ? 1.9f : 4.8f;
            if (_senior) hold *= 1.25f;
            float t0 = GameClock.Time;
            _runAt = t0 + 0.12f;
            _verdictAt = t0 + arrive;
            int nextStep = 0;
            var soundedPart = new bool[m.Targets.Length];
            bool resultShown = false;
            while (true)
            {
                float t = GameClock.Time - t0;
                while (nextStep <= maxD && t >= 0.12f + nextStep * step)
                {
                    PlayClip(EngranajesSounds.Step(nextStep), 0.9f);
                    nextStep++;
                }
                for (int k = 0; k < soundedPart.Length; k++)
                {
                    if (soundedPart[k] || t < arrive + k * 0.12f) continue;
                    soundedPart[k] = true;
                    PlayPartSound(m.Targets[k]);
                    if (!_results[k].Ok) PlayClip(EngranajesSounds.Thud(), 0.8f);
                }
                if (!resultShown && t >= arrive + 0.6f)
                {
                    resultShown = true;
                    if (ok)
                    {
                        PlayClip(EngranajesSounds.Success(), 0.8f);
                        if (!_guided) Tell(EngranajesContract.SuccessText(m, changes, _okCount), null, true);      // en el tutorial habla Nubi
                        foreach (var target in m.Targets)
                        {
                            var at = PartPosition(target);
                            Emit(L(at.x, at.y), 18, 140f, 0.8f, Gold, 20f);
                        }
                        if (count)
                        {
                            var first = PartPosition(m.Targets[0]);
                            StartFlyer(L(first.x, first.y));
                        }
                    }
                    else
                    {
                        if (!_guided) Tell(EngranajesContract.FailText(_results), EngranajesContract.HintText(m, _results, changes), false);
                        _showFixAt = GameClock.Time;
                    }
                }
                if (t >= arrive + 0.6f + hold) break;
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
            BuildTutorial(_safe, GameHud.Height + 10f, "Engranajes", "Arregla la máquina: toca el motor o una correa y luego Arrancar.", badgeAtBottom: true);

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
            // «Nubi entrenadora»: una máquina de la etapa 1 en la que la antena viene fallando, con tres focos. Los dos de «tocar» congelan el juego y el toque en el hueco es el de verdad:
            // 1) mirar el cartel de la antena (la antena queda iluminada), 2) tocar el motor (el cambio real), 3) tocar «Arrancar» (arranca de verdad y el cartel se pone verde).
            var script = new GuidedScript(1);                          // una máquina; «Saltar tutorial» la termina
            var easy = EngranajesContract.Generate(1, _rng, Station.Antena);
            ShowMachine(easy);
            _phase = Phase.Play;
            _inputOn = false;
            yield return Motion.Hold(0.6f);
            bool ok = !t.Skipped;
            var antena = new[] { coach.Zone(() => coach.AroundOf(_partRoots[(int)Station.Antena], new Vector2(SceneUnits(60f), SceneUnits(34f)))) };
            if (ok)
            {
                yield return StartCoroutine(coach.Watch(() => coach.RectOf(_cartels[(int)Station.Antena].Root), CoachTexts.Engranajes.Cartel, () => false, 3f, keep: antena));
                ok = !t.Skipped;
            }
            if (ok)
            {
                yield return StartCoroutine(coach.Touch(() => coach.AroundOf(_motorArrowRect, Vector2.one * SceneUnits(100f)), CoachTexts.Engranajes.Motor, circle: true, keep: antena));
                ok = !t.Skipped;
                if (ok) ToggleSwitch(EngranajesContract.MotorId);      // el toque en el hueco ES el cambio
            }
            if (ok)
            {
                var cartel = new[] { coach.Zone(() => coach.RectOf(_cartels[(int)Station.Antena].Root)) };
                yield return StartCoroutine(coach.Touch(() => coach.RectOf(_start.Root), CoachTexts.Engranajes.Start, keep: cartel));
                ok = !t.Skipped;
                if (ok)
                {
                    PressStart();                                      // el toque en el hueco ES «Arrancar»
                    coach.Hide();
                    yield return StartCoroutine(RevealRoutine(easy, new List<int>(_changes), false));
                    yield return StartCoroutine(coach.Notice(CoachTexts.Ready, 1.5f));
                }
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
            AnimateBottom(now);
            AnimateSay(now);
            AnimateFlyer(now);
            AnimateSparks(dt);
            AnimateIntro(now);
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
            if (_start.Rect.Contains(p))
            {
                PressStart();
                return;
            }
            var scene = EngranajesLayout.LogicalToScene(_lay, p.x, p.y);
            int id = HitSwitch(scene);
            if (id != int.MinValue) ToggleSwitch(id);
        }

        /// <summary>SOLO EN EL EDITOR (smoke): juega la máquina sola; las pares con la solución guardada y las impares sin tocar nada (así pasa también por el error y su aviso). En el teléfono no hace nada.</summary>
        private void AutoPlayMachine(Machine m)
        {
#if UNITY_EDITOR
            bool solve = _resolved % 2 == 0;
            if (solve) foreach (int id in m.Solution) if (!_changes.Contains(id)) ToggleSwitch(id);
            Debug.Log($"[SmokeTest] Engranajes: máquina {_resolved + 1} (nivel {m.Level}), {(solve ? "con la solución guardada" : "sin tocar nada")}");
            PressStart();
#endif
        }

        /// <summary>«Arrancar»: está siempre activo (a veces la máquina ya está bien).</summary>
        private void PressStart()
        {
            _startPressAt = GameClock.Time;
            PlayClip(EngranajesSounds.Press(), 0.6f);
            _startRequested = true;
        }

        /// <summary>Hace (o deshace) un cambio. Tocar algo ya cambiado lo deshace; con todas las llaves usadas, tocar otro interruptor MUEVE la llave: se deshace el cambio más antiguo, sin error.</summary>
        private void ToggleSwitch(int id)
        {
            float now = GameClock.Time;
            if (_changes.Contains(id)) _changes.Remove(id);
            else
            {
                if (_changes.Count >= _mach.Stage.Keys)
                {
                    int old = _changes[0];
                    _changes.RemoveAt(0);
                    FlipSwitch(old, now);
                }
                _changes.Add(id);
            }
            FlipSwitch(id, now);
            PlayClip(EngranajesSounds.Clank(), 0.7f);
        }

        private void FlipSwitch(int id, float now)
        {
            if (id == EngranajesContract.MotorId) _motorFlipAt = now;
            else _beltFlipAt[id] = now;
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

        private float SceneUnits(float dp) => dp * _s * (_lay.SceneScale <= 0f ? 1f : _lay.SceneScale);

        private void PlayClip(AudioClip clip, float volume)
        {
            if (clip == null || !GameFeel.SoundOn) return;
            if (_config != null && _config.config != null && !_config.config.sound_enabled) return;
            _audioSource.PlayOneShot(clip, volume);
        }
    }
}
