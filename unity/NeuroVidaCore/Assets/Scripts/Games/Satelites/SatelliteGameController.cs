using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using NeuroVida.Bridge;
using NeuroVida.Contracts;
using NeuroVida.Games.Shared;
using static NeuroVida.Games.Shared.UiKit;
using Motion = NeuroVida.Games.Shared.Motion; // UnityEngine.Motion también existe

namespace NeuroVida.Games.Satelites
{
    /// <summary>
    /// «Satélites: enciende tu planeta» (id <c>satelites</c>, renovado el 9-oct; ver <see cref="SatelliteContract"/> y docs/diseno-satelites.md; el boceto aprobado es docs/previews/satelites-orbitas-boceto.html, versión 2).
    /// SEGUIMIENTO DE MÚLTIPLES OBJETOS en órbitas planas: tu planeta está a oscuras; algunos satélites traen un mensaje (aro dorado y sobre), se apagan y todos giran en tres anillos; cuando se detienen se tocan los que traían
    /// mensaje y cada uno entregado vuela al planeta y enciende una luz. Una ronda dura de 12 a 16 s: aviso de sorpresa si hay (cambio de órbita, nube de polvo, órbitas rápidas: SIEMPRE antes de la presentación), presentación de
    /// 1,8 s, seguimiento SIN ninguna respuesta posible, respuesta (se puede corregir hasta 0,55 s después del último), revelación. El motor común recibe un ensayo por ronda (acierto = todos los k). Todo el tiempo va con
    /// <see cref="GameClock"/> y lo que se mueve con <see cref="Motion"/>: con «quitar animaciones» los satélites siguen girando (es la tarea) pero sin pulso del aro, sin partículas, sin vuelo de los sobres y con luces fijas.
    /// Reglas por patentes: plano (sin profundidad simulada), ninguna respuesta durante el seguimiento y sin «firma» por series.
    /// </summary>
    public sealed partial class SatelliteGameController : GameControllerBase
    {
        public const string GameId = SatelliteContract.GameId;

        private const float UnitsPerDp = 3f;
        private const float MarginU = 60f;
        /// <summary>Si en la respuesta pasan tantos segundos sin completar los marcados, se evalúa lo que haya (nadie se queda atascado ni el Reto sin terminar).</summary>
        private const float AnswerTimeoutSeconds = 25f;
        private const float SampleEvery = 0.1f;

        private enum Phase { Idle, Surprise, Ready, Cue, Track, Answer, Reveal, Done }

        /// <summary>SOLO EN EL EDITOR, para las capturas de pantalla (<c>verificar-todo.sh --capturas Satelites</c>): con esta bandera el juego NO se juega solo y un guion (<see cref="EditorShotScript"/>) lo lleva por los
        /// momentos que se fotografían. En el teléfono es siempre false.</summary>
        public static bool EditorShotMode;

        // ------------------------------------------------------------------ estado

        private System.Random _rng;
        private AdaptiveDifficulty _dda;
        private SatelliteRun _run;
        private OrbitSwarm _swarm;
        private LevelSpec _spec;
        private Surprise _surprise, _lastSurprise;
        private Phase _phase = Phase.Idle;
        private bool _loopOn, _baked, _guided, _inputOn, _tapped, _planetBig;
        private int _roundNo, _level, _bestRecord, _debugStage, _debugSurprise, _lightsShown, _lastTickSecond;
        private float _endsAt, _evalAt, _answerStartedAt, _phaseAt;
        private int _previousFrameRate;
        private Vector2? _press;
        private readonly bool[] _marked = new bool[SatPool];
        private readonly List<float[]> _framesX = new List<float[]>(), _framesY = new List<float[]>();
        private readonly List<(float x, float y)> _lightSpots = new List<(float x, float y)>();

        private ExitButton _exit;
        private GameHud _hud;
        private CountdownScreen _countdown;

        private bool Endless => _config != null && _config.config.timed;
        private float Now => GameClock.Time;

        // ------------------------------------------------------------------ sesión

        public void StartSession(SequenceInitConfig config)
        {
            _config = config;
            _rng = new System.Random();
#if UNITY_EDITOR
            // el smoke arranca en el nivel 6 (con las tres sorpresas, la media vuelta y los de frente) para pasar por todo lo importante en pocas rondas
            if (GuidedTutorial.EditorAutoPlayGame && !EditorShotMode && config.config.sat_stage <= 0) config.config.sat_stage = 6;
#endif
            _dda = SatelliteMetrics.CreateEngine(config.config);
            _bestRecord = Mathf.Max(0, config.config.sat_best);
            _debugStage = Mathf.Max(0, config.config.sat_stage);
            _debugSurprise = Mathf.Max(0, config.config.sat_surprise);
            _run = new SatelliteRun();
            _swarm = null;
            _surprise = _lastSurprise = Surprise.None;
            _phase = Phase.Idle;
            _loopOn = _guided = _inputOn = _planetBig = false;
            _roundNo = 0;
            _lightsShown = 0;
            _lastTickSecond = -1;
            _endsAt = 0f;
            _press = null;
            _lightSpots.Clear();

            // Movimiento continuo: a 60 cuadros por segundo se ve fluido (Android da 30 por defecto).
            _previousFrameRate = Application.targetFrameRate;
            Application.targetFrameRate = 60;

            ResetRoundViews();
            ResetPlanet();
            _exit.Hide();
            _hud.SetStreak(0);
            _tutorial.Hide();
            StopAllCoroutines();
            StartCoroutine(GameLoop());
        }

        private void OnDisable()
        {
            if (_previousFrameRate != 0) Application.targetFrameRate = _previousFrameRate;
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
                ResetRoundViews();
                ResetPlanet();
            }
            _safe.gameObject.SetActive(false);
            yield return StartCoroutine(_countdown.Play(SatelliteContract.Title, Assessment.Subtitle(SatelliteContract.CountdownSub), () => _safe.gameObject.SetActive(true)));
            while (!_baked) yield return null;
            _safe.gameObject.SetActive(true);
            yield return null;
            ApplySafeArea(_safe);
            Canvas.ForceUpdateCanvases();
            Layout();
            _endsAt = GameClock.Time + SatelliteContract.RetoSeconds;
            _timerBg.gameObject.SetActive(Endless);
            _loopOn = true;
            yield return StartCoroutine(MainLoop());
        }

        /// <summary>El bucle de la partida: una ronda tras otra hasta que se cumplan las rondas (Precisión) o el tiempo (Reto). «Cómo se juega» lo retoma desde acá.</summary>
        private IEnumerator MainLoop()
        {
            while (!Finished()) yield return StartCoroutine(PlayRound());
            _loopOn = false;
            yield return StartCoroutine(FinishGame());
        }

        private bool Finished() => !Endless ? _run.RoundsPlayed >= SatelliteContract.PrecisionRounds : GameClock.Time >= _endsAt;

        /// <summary>Hornea los sprites y sintetiza los sonidos durante la cuenta regresiva, de a poco por cuadro (así nada se traba).</summary>
        private IEnumerator Prewarm()
        {
            if (!_baked)
            {
                var sprites = SatelliteSprites.Prewarm();
                while (sprites.MoveNext()) yield return null;
                AssignSprites();
                _baked = true;
            }
            var sounds = SatelliteSounds.Prewarm();
            while (sounds.MoveNext()) yield return null;
        }

        // ------------------------------------------------------------------ una ronda

        private IEnumerator PlayRound()
        {
            _roundNo = _run.RoundsPlayed + 1;
            _level = _dda.PresentedLevel;
            var baseSpec = SatelliteContract.Level(_level);
            _surprise = ChooseSurprise(baseSpec);
            if (_surprise != Surprise.None) _lastSurprise = _surprise;
            _spec = baseSpec.With(_surprise);
#if UNITY_EDITOR
            if (GuidedTutorial.EditorAutoPlayGame) _spec = _spec.Scaled(0.55f);          // el smoke juega más rápido para pasar por varias rondas
#endif
            BeginRoundObjects(_spec, _surprise);
            SetPrompt("", "", TextColor);                       // sin el resultado de la ronda anterior detrás del aviso
            UpdateHud();
            if (_surprise != Surprise.None) yield return StartCoroutine(ShowSurprise(_surprise));
            yield return StartCoroutine(DoReady());
            yield return StartCoroutine(DoCue());
            yield return StartCoroutine(DoTrack());
            yield return StartCoroutine(DoAnswer());
            yield return StartCoroutine(DoReveal());
            ClearRoundViews();
        }

        private Surprise ChooseSurprise(LevelSpec spec)
        {
#if UNITY_EDITOR
            if (GuidedTutorial.EditorAutoPlayGame && _debugSurprise == 0 && !EditorShotMode)
            {
                // el smoke pasa por las tres en las tres primeras rondas (y deja la primera sin sorpresa para mirar lo básico)
                Surprise[] cycle = { Surprise.None, Surprise.Orbit, Surprise.Cloud, Surprise.Fast };
                var s = cycle[(_roundNo - 1) % cycle.Length];
                return s == Surprise.None || (spec.Surprises & s) != 0 ? s : Surprise.None;
            }
#endif
            if (_debugSurprise != 0) return (Surprise)_debugSurprise;
            return SatelliteContract.PickSurprise(spec.Surprises, _roundNo, _lastSurprise, _rng);
        }

        private IEnumerator DoReady()
        {
            _phase = Phase.Ready;
            _phaseAt = Now;
            SetPrompt(SatelliteContract.CueTitle(_spec.K), SatelliteContract.CueSub, TextColor);
            yield return Wait(SatelliteContract.ReadySeconds);
        }

        /// <summary>La presentación (1,8 s): los que traen mensaje con su aro dorado y su sobre; en los últimos 300 ms la señal se desvanece.</summary>
        private IEnumerator DoCue()
        {
            _phase = Phase.Cue;
            var targets = new List<int>();
            for (int i = 0; i < _swarm.Count; i++) if (_swarm.Target[i]) targets.Add(i);
            foreach (int i in targets) SetCue(i, 1f, 1f);
            SetPrompt(SatelliteContract.CueTitle(_spec.K), SatelliteContract.CueSub, TextColor);
            float t = 0f;
            int rung = 0;
            while (t < SatelliteContract.CueSeconds)
            {
                t += GameClock.DeltaTime;
                if (rung < targets.Count && t >= rung * 0.12f) { PlayClip(SatelliteSounds.CueBell(rung), 0.7f); rung++; }
                float fade = Mathf.Clamp01((SatelliteContract.CueSeconds - t) / SatelliteContract.CueFadeSeconds);
                float pulse = Motion.Decorative ? 0.75f + 0.25f * Mathf.Sin(t * 7.1f) : 1f;          // sin «quitar animaciones» la señal queda fija
                foreach (int i in targets) SetCue(i, fade, pulse);
                yield return null;
            }
            foreach (int i in targets) HideCue(i);
        }

        /// <summary>El seguimiento: giran en sus anillos sin que se pueda tocar nada. Termina cuando se cumple el tiempo Y nadie está a menos de 38 dp de otro ni bajo la nube (hasta 0,9 s más).</summary>
        private IEnumerator DoTrack()
        {
            _phase = Phase.Track;
            SetPrompt(SatelliteContract.TrackTitle, SatelliteContract.TrackHint(_surprise), TextColor);
            if (_surprise == Surprise.Cloud)
            {
                _swarm.StartCloud(ScreenPlan.Width);
                _cloud.gameObject.SetActive(true);
            }
            StartHum();
            _framesX.Clear();
            _framesY.Clear();
            float sampleAt = 0f;
            Sample();
            while (true)
            {
                float dt = GameClock.DeltaTime;
                if (dt > 0f)
                {
                    _swarm.Step(Mathf.Min(dt, 0.05f));
                    if (_swarm.Time >= sampleAt) { sampleAt += SampleEvery; Sample(); }
                }
                if (_swarm.ReadyToStop()) break;
                yield return null;
            }
            _swarm.EndCloud();
            _cloud.gameObject.SetActive(false);
            StopHum();
            PlayClip(SatelliteSounds.Stop(), 0.8f);
            // si todavía hay dos casi tocándose, se apartan lo mínimo en 0,35 s (así nunca quedan encimados al responder)
            if (_swarm.PlanSettle())
            {
                while (_swarm.Settling)
                {
                    _swarm.SettleStep(Mathf.Min(GameClock.DeltaTime, 0.05f));
                    yield return null;
                }
            }
            Sample();
        }

        private void Sample()
        {
            _framesX.Add((float[])_swarm.X.Clone());
            _framesY.Add((float[])_swarm.Y.Clone());
        }

        /// <summary>La respuesta: tocar marca o desmarca. Al marcar el k-ésimo se evalúa 0,55 s después (un margen para corregir el último).</summary>
        private IEnumerator DoAnswer()
        {
            _phase = Phase.Answer;
            for (int i = 0; i < _marked.Length; i++) _marked[i] = false;
            _evalAt = 0f;
            _answerStartedAt = Now;
            _inputOn = true;
            _press = null;
#if UNITY_EDITOR
            _botPlan = null;
            _botT = 0f;
#endif
            UpdateAnswerPrompt();
            while (true)
            {
                if (_evalAt > 0f && Now >= _evalAt && MarkedCount() == _spec.K) break;
                if (Now - _answerStartedAt > AnswerTimeoutSeconds) break;
                yield return null;
            }
            _inputOn = false;
            _evalAt = 0f;
        }

        private int MarkedCount()
        {
            int n = 0;
            for (int i = 0; i < _swarm.Count; i++) if (_marked[i]) n++;
            return n;
        }

        private void UpdateAnswerPrompt()
        {
            int n = MarkedCount();
            SetPrompt(SatelliteContract.AnswerTitle(_spec.K), SatelliteContract.AnswerSub(n, _spec.K), Cyan);
        }

        // ------------------------------------------------------------------ la revelación

        private IEnumerator DoReveal()
        {
            _phase = Phase.Reveal;
            int k = _spec.K, n = _swarm.Count;
            int hits = 0, missedIdx = -1, wrongIdx = -1;
            for (int i = 0; i < n; i++)
            {
                if (_marked[i] && _swarm.Target[i]) hits++;
                else if (!_marked[i] && _swarm.Target[i] && missedIdx < 0) missedIdx = i;
                else if (_marked[i] && !_swarm.Target[i] && wrongIdx < 0) wrongIdx = i;
            }
            bool perfect = SatelliteContract.IsPerfect(hits, k);
            if (!_guided)
            {
                _run.Add(hits, k, n, _level);            // la ronda guiada del tutorial no cuenta: ni puntos, ni motor, ni racha
                _dda.Register(perfect);
                _hud.SetStreak(_run.Streak);
            }
            int streak = _guided ? 0 : _run.Streak;
#if UNITY_EDITOR
            if (!_guided) Debug.Log("[SmokeTest] Satelites: ronda " + _roundNo + " nivel " + _level + (_surprise != Surprise.None ? " sorpresa " + SatelliteContract.SurpriseName(_surprise) : "") + ": " + hits + " de " + k + " (de " + n + "), luces " + _run.Lights + ", racha " + _run.Streak);
#endif
            // las marcas: ✓ en los entregados, aspa diagonal coral en los marcados sin mensaje, aro punteado en los que faltaron
            for (int i = 0; i < n; i++)
            {
                var v = _sats[i];
                bool target = _swarm.Target[i], marked = _marked[i];
                v.MarkRing.gameObject.SetActive(false);
                if (target) SetCue(i, 1f, 1f, full: false);
                if (marked && target) ShowBadge(v, true);
                else if (marked) { ShowBadge(v, false); v.MarkRing.color = Bad; v.MarkRing.gameObject.SetActive(true); }
                else if (target) { v.Dashed.color = Bad; v.Dashed.gameObject.SetActive(true); }
                if (!marked && !target) v.Body.color = new Color(1f, 1f, 1f, 0.55f);
            }
            // el sonido y la sensación de la ronda
            if (perfect)
            {
                GameFeel.Haptic(GameFeel.HapticKind.Firm);
                PlayClip(SatelliteSounds.Perfect(), 0.8f);          // el sonido es el propio del juego (un arpegio), no el pling común
            }
            else
            {
                GameFeel.Haptic(GameFeel.HapticKind.Double);
                PlayClip(SatelliteSounds.Partial(), 0.7f);          // nunca un castigo: un tono suave que baja
            }
            SetPrompt(SatelliteContract.ResultTitle(hits, k), SatelliteContract.ResultSub(perfect, streak), perfect ? Good : Gold);
            StartCoroutine(PopRect(_promptA.rectTransform, 1.12f, 0.25f));
            // los sobres de los entregados vuelan al planeta y encienden luces
            int j = 0;
            for (int i = 0; i < n; i++)
            {
                if (!(_marked[i] && _swarm.Target[i])) continue;
                StartFlight(i, Now + 0.25f + j * 0.17f);
                j++;
            }
            // ¿dónde se confundió? Donde el que faltó y el que se marcó por error pasaron más cerca
            float hold = SatelliteContract.RevealSeconds;
            if (missedIdx >= 0 && wrongIdx >= 0 && SatelliteContract.TryCrossPoint(_framesX, _framesY, missedIdx, wrongIdx, out float cx, out float cy))
            {
                ShowCross(cx, cy);
                hold += SatelliteContract.CrossExtraSeconds;
            }
            if (!_guided) UpdateHud();
            UpdateFooter();
            yield return Wait(hold);
            _phase = Phase.Idle;
        }

        // ------------------------------------------------------------------ fin

        private IEnumerator FinishGame()
        {
            _phase = Phase.Done;
            _inputOn = false;
            int record = SatelliteContract.NewRecord(_bestRecord, _run.Lights);
            bool broke = SatelliteContract.BrokeRecord(_bestRecord, _run.Lights);
#if UNITY_EDITOR
            Debug.Log("[SmokeTest] Satelites: partida terminada: " + _run.Lights + " luces, " + _run.Perfect + " rondas perfectas de " + _run.RoundsPlayed + ", seguimiento " + _run.Capacity.ToString("0.0") + ", nivel más alto " + _run.BestCleared);
#endif
            ClearRoundViews();
            ShowEnd(record, broke);
            _exit.Show();
            PlayClip(SatelliteSounds.Finale(), 0.8f);
            var telemetry = new StroopTelemetry
            {
                user_id = _config.user_id,
                game_id = _config.game_id,
                session_metrics = SatelliteMetrics.Build(_dda, _run, record, broke, _config.config)
            };
            NativeBridge.ForwardTelemetryToPlatform(JsonUtility.ToJson(telemetry));
            yield break;
        }

        // ------------------------------------------------------------------ marcador

        private void UpdateHud()
        {
            _hud.SetLevel(_dda.PresentedLevel);
            // la ronda que se juega (también durante su revelación: el número no salta hasta que empieza la siguiente)
            int round = Mathf.Max(1, _roundNo);
            _hud.SetInfo(Endless ? SatelliteContract.RoundHudReto(round)
                                 : SatelliteContract.RoundHud(Mathf.Min(round, SatelliteContract.PrecisionRounds), SatelliteContract.PrecisionRounds));
        }

        private void UpdateClock()
        {
            if (!Endless || _endsAt <= 0f || _phase == Phase.Idle || _phase == Phase.Done || _guided) return;
            float left = _endsAt - GameClock.Time;
            float fraction = Mathf.Clamp01(left / SatelliteContract.RetoSeconds);
            _timerFill.anchorMax = new Vector2(fraction, 1f);
            _timerFill.offsetMin = _timerFill.offsetMax = Vector2.zero;
            _timerFillImage.color = fraction > 0.5f ? Color.Lerp(Gold, Good, (fraction - 0.5f) * 2f) : Color.Lerp(Bad, Gold, fraction * 2f);
            int whole = Mathf.CeilToInt(left);
            if (whole <= 5 && whole >= 1 && whole != _lastTickSecond)
            {
                _lastTickSecond = whole;
                GameFeel.Tick();
            }
        }

        // ------------------------------------------------------------------ el aviso de sorpresa («NUEVO» una sola vez por instalación cada una)

        private const string IntroKey = "sat_intros";

        private static bool IntroSeen(Surprise s)
        {
            try { return (PlayerPrefs.GetInt(IntroKey, 0) & (int)s) != 0; }
            catch (System.Exception) { return false; }
        }

        private static void MarkIntroSeen(Surprise s)
        {
            try { PlayerPrefs.SetInt(IntroKey, PlayerPrefs.GetInt(IntroKey, 0) | (int)s); PlayerPrefs.Save(); }
            catch (System.Exception) { }
        }

        /// <summary>El aviso se muestra ANTES de la presentación (nunca durante el seguimiento): 1,7 s, y 2,6 s con la etiqueta «NUEVO» la primera vez que sale cada sorpresa.</summary>
        private IEnumerator ShowSurprise(Surprise s)
        {
            _phase = Phase.Surprise;
            bool isNew = !IntroSeen(s);
            MarkIntroSeen(s);
            _surpriseTag.text = isNew ? SatelliteContract.NewTag : SatelliteContract.SurpriseTag;
            _surpriseName.text = SatelliteContract.SurpriseName(s);
            _surpriseLine.text = SatelliteContract.SurpriseLine(s);
            _surpriseLayer.gameObject.SetActive(true);
            _surpriseGroup.alpha = 0f;
            PlayClip(SatelliteSounds.Chime(), 0.7f);
            float total = isNew ? SatelliteContract.SurpriseFirstSeconds : SatelliteContract.SurpriseSeconds;
            float t = 0f;
            while (t < total)
            {
                t += GameClock.DeltaTime;
                _surpriseGroup.alpha = Motion.Decorative ? Mathf.Min(1f, t / 0.2f, (total - t) / 0.25f) : 1f;
                yield return null;
            }
            _surpriseLayer.gameObject.SetActive(false);
        }

        // ------------------------------------------------------------------ pieza del tutorial

        /// <summary>El tutorial guiado común sobre el área segura (se llama al final de <c>BuildUi</c>, para que quede encima de todo).</summary>
        private void SetUpTutorial() =>
            BuildTutorial(_safe, GameHud.Height + 10f, SatelliteContract.Title, "Sigue los satélites que traen un mensaje y enciende las luces de tu planeta.", badgeAtBottom: true);

        // ------------------------------------------------------------------ «Cómo se juega» desde la pausa

        protected override bool HowToReady => _loopOn && _phase != Phase.Done;

        protected override void HowToSuspend()
        {
            // la ronda a medias no cuenta: se vuelve a jugar entera
            _phase = Phase.Idle;
            _inputOn = false;
            _roundNo = _run != null ? _run.RoundsPlayed : 0;
            _swarm = null;
            StopHum();
            ResetRoundViews();
        }

        protected override void HowToResume(float spentSeconds)
        {
            if (Endless) _endsAt += spentSeconds;                // el tiempo del tutorial no se le descuenta al Reto
            _timerBg.gameObject.SetActive(Endless);
            Layout();
            UpdateHud();
            UpdateFooter();
            StartCoroutine(MainLoop());
        }

        // ------------------------------------------------------------------ entrada

        private void Update()
        {
            if (PollTutorialSkip()) return;             // un toque en «Saltar tutorial» no es un toque al juego
            float now = GameClock.Time;
            UpdateClock();
            AnimateLights(now);
            AnimateFlights(now);
            if (_swarm != null) PlaceSwarm();
            if (_inputOn)
            {
                ReadPress();
                HandlePress();
            }
#if UNITY_EDITOR
            if (_phase == Phase.Answer && !_guided && GameClock.DeltaTime > 0f) AutoPlay(GameClock.DeltaTime);
#endif
        }

        private void ReadPress()
        {
            if (!GuidedTutorial.TryPress(out Vector2 pos)) return;
            if (_tutorial != null && _tutorial.Coach != null && _tutorial.Coach.Blocks(pos)) return;
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(_play, pos, null, out var local)) return;
            _press = ToLogical(local);
        }

        private void HandlePress()
        {
            if (!_press.HasValue) return;
            var p = _press.Value;
            _press = null;
            if (_phase == Phase.Answer && _swarm != null) OnAnswerTap(p);
        }

        /// <summary>Un toque en la respuesta: el satélite más cercano (a menos de 30 dp) se marca o se desmarca.</summary>
        private void OnAnswerTap(Vector2 p)
        {
            int i = _swarm.Nearest(p.x, p.y);
            if (i >= 0) ToggleMark(i);
        }

        private void ToggleMark(int i)
        {
            int n = MarkedCount();
            if (_marked[i])
            {
                _marked[i] = false;
                n--;
                _sats[i].MarkRing.gameObject.SetActive(false);
                PlayClip(SatelliteSounds.Unmark(), 0.7f);
            }
            else if (n >= _spec.K)
            {
                PlayClip(SatelliteSounds.Blocked(), 0.6f);          // ya están todos: para cambiar uno, primero se quita otro
                return;
            }
            else
            {
                _marked[i] = true;
                n++;
                var ring = _sats[i].MarkRing;
                ring.color = Cyan;
                ring.gameObject.SetActive(true);
                StartCoroutine(PopRect(_sats[i].Root, 1.12f, 0.18f));
                PlayClip(SatelliteSounds.Mark(n), 0.8f);
            }
            GameFeel.Haptic(GameFeel.HapticKind.Light);
            _evalAt = n == _spec.K ? Now + SatelliteContract.EvalDelaySeconds : 0f;
            UpdateAnswerPrompt();
        }

        private void PlayClip(AudioClip clip, float volume)
        {
            if (clip == null || !GameFeel.SoundOn) return;
            if (_config != null && _config.config != null && !_config.config.sound_enabled) return;
            _audioSource.PlayOneShot(clip, volume);
        }

        private void StartHum()
        {
            if (!GameFeel.SoundOn || (_config != null && _config.config != null && !_config.config.sound_enabled)) return;
            _humSource.clip = SatelliteSounds.Hum();
            _humSource.volume = 0.12f;
            _humSource.Play();
        }

        private void StopHum()
        {
            if (_humSource != null && _humSource.isPlaying) _humSource.Stop();
        }

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
        private List<int> _botPlan;

        /// <summary>SOLO EN EL EDITOR (smoke): juega solo. Espera un momento tras la parada y marca los que traían mensaje; una de cada cuatro rondas se «equivoca» con el satélite vecino de uno (así el arranque de prueba pasa por los aciertos,
        /// los errores y «¿Aquí se cruzaron?»). En el teléfono no hace nada.</summary>
        private void AutoPlay(float dt)
        {
            if (EditorShotMode || !GuidedTutorial.AutoPlayGame(10f) || _swarm == null) return;
            if (_botPlan == null)
            {
                _botPlan = new List<int>();
                for (int i = 0; i < _swarm.Count; i++) if (_swarm.Target[i]) _botPlan.Add(i);
                if (_roundNo % 4 == 3 && _botPlan.Count > 0)
                {
                    int wrong = -1;
                    float best = float.MaxValue;
                    int from = _botPlan[_botPlan.Count - 1];
                    for (int i = 0; i < _swarm.Count; i++)
                    {
                        if (_swarm.Target[i]) continue;
                        float d = Mathf.Abs(_swarm.X[i] - _swarm.X[from]) + Mathf.Abs(_swarm.Y[i] - _swarm.Y[from]);
                        if (d < best) { best = d; wrong = i; }
                    }
                    if (wrong >= 0) _botPlan[_botPlan.Count - 1] = wrong;
                }
                _botT = 0f;
            }
            _botT += dt;
            if (_botT < 0.5f || _botPlan.Count == 0) return;
            _botT = 0.3f;
            int next = _botPlan[0];
            _botPlan.RemoveAt(0);
            if (!_marked[next]) ToggleMark(next);
        }
#endif
    }
}
