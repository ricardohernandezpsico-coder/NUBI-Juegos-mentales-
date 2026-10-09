using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using NeuroVida.Bridge;
using NeuroVida.Contracts;
using NeuroVida.Games.Secuencia; // RoundedRectSprite / RadialGlowSprite / RingSprite
using NeuroVida.Games.Parejas;   // SymbolSprite
using NeuroVida.Games.Shared;
using static NeuroVida.Games.Shared.UiKit;
using Motion = NeuroVida.Games.Shared.Motion; // UnityEngine.Motion también existe

namespace NeuroVida.Games.Satelites
{
    /// <summary>
    /// "Satélites": juego estrella de seguimiento de múltiples objetos (ver <see cref="SatelliteContract"/>). Eres
    /// control de misión: algunos satélites encienden su señal un momento, se apagan, y todos se mueven y se cruzan.
    /// Al detenerse, tocas los que llevaban la señal.
    /// <list type="bullet">
    /// <item>Al revelar se dibuja el camino que hicieron los de la señal; si confundiste uno, se marca dónde pasó más
    /// cerca del que elegiste ("¿aquí se cruzaron?"): se ve el momento exacto de la confusión.</item>
    /// <item>Medida propia: "tu seguimiento", cuántos satélites sigues de verdad a la vez (descontando la suerte), y la
    /// velocidad máxima superada.</item>
    /// </list>
    /// Reto = 2 minutos de rondas; Precisión = 8 rondas sin reloj.
    /// </summary>
    public class SatelliteGameController : GameControllerBase
    {
        public const string GameId = SatelliteContract.GameId;

        private const float UnitsPerDp = 3f;
        private const float MarginU = 60f;
        private const float SampleEvery = 0.1f;
        private const int TrailPool = 260;
        private const int MaxSatellites = 12;

        private static readonly Color GoodColor = NeuroStyle.Lime;
        private static readonly Color BadColor = NeuroStyle.Coral;
        private static readonly Color SignalColor = NeuroStyle.Sun;
        private static readonly Color PickColor = NeuroStyle.Sky;

        private enum Phase { Idle, Cue, Track, Answer, Feedback, Done }

        private sealed class Sat
        {
            public RectTransform Rect;
            public Image Body, Halo, Glow, Ring, Mark;
            public bool Target, Picked;
            public readonly List<float> PathX = new List<float>();
            public readonly List<float> PathY = new List<float>();
        }

        private System.Random _rng;
        private AdaptiveDifficulty _dda;
        private Phase _phase = Phase.Idle;
        private bool Endless => _config != null && _config.config.timed;
        private bool Precision => !Endless;
        private int _previousFrameRate;

        // ronda en curso
        private SatelliteSwarm _swarm;
        private int _level, _targets, _picked;
        private float _sampleAt;

        // sesión
        private readonly List<(int hits, int targets, int total)> _rounds = new List<(int hits, int targets, int total)>();
        private int _hitsTotal, _targetsTotal, _cleared, _bestCleared;
        private int _streak, _bestStreak, _points;
        private float _endsAt;
        private int _lastTickSecond = -1;

        // UI
        private RectTransform _safe, _fxRect, _arena, _pickRow, _crossMarker, _timerBg, _timerFill;
        private Image _arenaImage;
        private Text _prompt, _crossLabel;
        private readonly List<Sat> _sats = new List<Sat>();
        private readonly List<Image> _trail = new List<Image>();
        private readonly List<Image> _pickDots = new List<Image>();
        private Toast _toast;
        private ExitButton _exit;
        private GameHud _hud;
        private CountdownScreen _countdown;
        private float _arenaW, _arenaH, _fieldH, _satSize;

        // ------------------------------------------------------------------ sesión

        public void StartSession(SequenceInitConfig config)
        {
            _config = config;
            _rng = new System.Random();
            var age = DdaUserProfileConfig.ParseAgeBand(config.config.age_band);
            float start = AdaptiveDifficulty.StartRating(config.config, SatelliteContract.MaxLevel);
            // Pocas rondas por partida (~8-10): pasos grandes. Sin tiempo de reacción: cuenta lo que se siguió.
            _dda = new AdaptiveDifficulty(SatelliteContract.MaxLevel, age, start, stepUp: 0.5f, useReaction: false);

            // Movimiento continuo: a 60 cuadros por segundo se ve fluido (Android da 30 por defecto).
            _previousFrameRate = Application.targetFrameRate;
            Application.targetFrameRate = 60;

            _phase = Phase.Idle;
            _rounds.Clear();
            _hitsTotal = _targetsTotal = _cleared = _bestCleared = 0;
            _streak = _bestStreak = _points = 0;
            _lastTickSecond = -1;
            _endsAt = 0f;
            _swarm = null;

            _resultRoot.gameObject.SetActive(false);
            _exit.Hide();
            _timerBg.gameObject.SetActive(Endless);
            _hud.SetStreak(0);
            HideAll();

            StopAllCoroutines();
            StartCoroutine(GameLoop());
        }

        private void OnDisable()
        {
            if (_previousFrameRate != 0) Application.targetFrameRate = _previousFrameRate;
        }

        private IEnumerator GameLoop()
        {
            _safe.gameObject.SetActive(false);
            yield return StartCoroutine(_countdown.Play("Satélites", Assessment.Subtitle("Prepárate"), () => _safe.gameObject.SetActive(true)));
            _safe.gameObject.SetActive(true);
            yield return null;
            ApplySafeArea(_safe);
            Canvas.ForceUpdateCanvases();
            Layout();
            UpdateHud();

            _endsAt = GameClock.Time + SatelliteContract.RetoSeconds;
            while (!Finished())
                yield return StartCoroutine(RunRound());
            yield return StartCoroutine(FinishGame());
        }

        private bool Finished() => Precision ? _rounds.Count >= SatelliteContract.PrecisionRounds : GameClock.Time >= _endsAt;

        // ------------------------------------------------------------------ una ronda

        private IEnumerator RunRound()
        {
            _level = _dda.PresentedLevel;
            _targets = SatelliteContract.Targets(_level);
            int total = SatelliteContract.Total(_level);
            _swarm = new SatelliteSwarm(total, _fieldH, SatelliteContract.Speed(_level), _rng);
            _picked = 0;

            // Quiénes llevan la señal: k al azar.
            var order = new List<int>();
            for (int i = 0; i < total; i++) order.Add(i);
            for (int i = order.Count - 1; i > 0; i--)
            {
                int j = _rng.Next(i + 1);
                (order[i], order[j]) = (order[j], order[i]);
            }
            for (int i = 0; i < _sats.Count; i++)
            {
                var s = _sats[i];
                s.Target = false;
                s.Picked = false;
                s.PathX.Clear();
                s.PathY.Clear();
                s.Mark.gameObject.SetActive(false);
                s.Ring.gameObject.SetActive(false);
                s.Glow.gameObject.SetActive(false);
                s.Body.color = Color.white;
                s.Rect.gameObject.SetActive(i < total);
            }
            for (int i = 0; i < _targets; i++) _sats[order[i]].Target = true;
            HideTrails();
            _crossMarker.gameObject.SetActive(false);
            SetPickRow(0);
            PlaceAll();

            // 1. Aparecen (uno tras otro, rápido).
            SetPrompt($"Memoriza los {_targets} que brillan", Color.white);
            for (int i = 0; i < total; i++) StartCoroutine(PopIn(_sats[i].Rect, 0.22f));
            yield return StartCoroutine(Wait(0.3f));

            // 2. Señal: los elegidos brillan y laten.
            _phase = Phase.Cue;
            foreach (var s in _sats)
            {
                if (!s.Target || !s.Rect.gameObject.activeSelf) continue;
                s.Glow.gameObject.SetActive(true);
                s.Ring.gameObject.SetActive(true);
                s.Ring.color = SignalColor;
            }
            float t = 0f;
            int beeps = 0;
            while (t < SatelliteContract.CueSeconds)
            {
                t += GameClock.DeltaTime;
                float pulse = Motion.Decorative ? 0.5f + 0.5f * Mathf.Sin(t * Mathf.PI * 2f * 1.4f) : 1f; // sin ReduceMotion: la señal queda fija (brillante), sin pulso
                foreach (var s in _sats)
                {
                    if (!s.Target) continue;
                    s.Glow.color = NeuroStyle.WithAlpha(SignalColor, 0.45f + 0.35f * pulse);
                    s.Ring.rectTransform.localScale = Vector3.one * (1.05f + 0.08f * pulse);
                }
                if (t > beeps * 0.7f && beeps < 3)
                {
                    beeps++;
                    PlayTone(988f, 0.07f, 0.05f);
                }
                yield return null;
            }

            // 3. Se apagan: todos iguales. Arrancan de a poco.
            t = 0f;
            while (t < 0.3f)
            {
                t += GameClock.DeltaTime;
                float k = 1f - Mathf.Clamp01(t / 0.3f);
                foreach (var s in _sats)
                {
                    if (!s.Target) continue;
                    s.Glow.color = NeuroStyle.WithAlpha(SignalColor, 0.8f * k);
                    s.Ring.color = NeuroStyle.WithAlpha(SignalColor, k);
                }
                yield return null;
            }
            foreach (var s in _sats) { s.Glow.gameObject.SetActive(false); s.Ring.gameObject.SetActive(false); }

            _phase = Phase.Track;
            SetPrompt("¡Síguelos con la vista!", SignalColor);
            PlayTone(660f, 0.1f, 0.06f);
            float speed = SatelliteContract.Speed(_level);
            float track = SatelliteContract.TrackSeconds(_level);
            _sampleAt = 0f;
            t = 0f;
            while (t < track)
            {
                float dt = GameClock.DeltaTime;
                t += dt;
                // Arranque y frenado suaves (0,5 s).
                float ease = Mathf.Clamp01(t / 0.5f) * Mathf.Clamp01((track - t) / 0.5f + 0.15f);
                _swarm.Speed = speed * ease;
                _swarm.Step(dt);
                if (t >= _sampleAt)
                {
                    _sampleAt += SampleEvery;
                    for (int i = 0; i < _swarm.Count; i++)
                    {
                        _sats[i].PathX.Add(_swarm.X[i]);
                        _sats[i].PathY.Add(_swarm.Y[i]);
                    }
                }
                PlaceAll();
                if (t > track * 0.5f && t - dt <= track * 0.5f) SetPrompt("¡Síguelos con la vista!", Color.white);
                yield return null;
            }

            // 4. Respuesta: tocar los que llevaban la señal.
            _phase = Phase.Answer;
            PlayTone(523f, 0.12f, 0.06f);
            SetPrompt($"Toca los {_targets} que brillaban", Color.white);
            while (_picked < _targets) yield return null;
            _phase = Phase.Feedback; // ya no se puede desmarcar
            yield return StartCoroutine(Wait(0.3f));

            // 5. Revelar.
            yield return StartCoroutine(Reveal());
        }

        private IEnumerator Reveal()
        {
            int hits = 0;
            Sat missed = null, wrong = null;
            foreach (var s in _sats)
            {
                if (!s.Rect.gameObject.activeSelf) continue;
                if (s.Target && s.Picked) hits++;
                if (s.Target && !s.Picked && missed == null) missed = s;
                if (!s.Target && s.Picked && wrong == null) wrong = s;
            }
            int total = _swarm.Count;
            bool all = hits >= _targets;
            _rounds.Add((hits, _targets, total));
            _hitsTotal += hits;
            _targetsTotal += _targets;
            if (all)
            {
                _cleared++;
                _bestCleared = Mathf.Max(_bestCleared, _level);
            }
            _streak = all ? _streak + 1 : 0;
            _bestStreak = Mathf.Max(_bestStreak, _streak);
            int pts = SatelliteContract.Points(hits, _targets, _level, _streak);
            _points += pts;
            _hud.SetStreak(_streak);

            // Marcas: ✓ en los bien encontrados, ✗ en los equivocados; los que faltaron vuelven a brillar ("era este").
            foreach (var s in _sats)
            {
                if (!s.Rect.gameObject.activeSelf) continue;
                if (s.Picked)
                {
                    s.Mark.sprite = s.Target ? AnswerMarkSprite.Check() : AnswerMarkSprite.Cross();
                    s.Mark.gameObject.SetActive(true);
                    s.Ring.gameObject.SetActive(true);
                    s.Ring.color = s.Target ? GoodColor : BadColor;
                    s.Ring.rectTransform.localScale = Vector3.one * 1.05f;
                    if (!s.Target) s.Body.color = new Color(1f, 1f, 1f, 0.55f);
                }
                else if (s.Target)
                {
                    s.Glow.gameObject.SetActive(true);
                    s.Glow.color = NeuroStyle.WithAlpha(SignalColor, 0.7f);
                    s.Ring.gameObject.SetActive(true);
                    s.Ring.color = SignalColor;
                    StartCoroutine(PopRect(s.Rect, 1.2f, 0.35f));
                }
                else s.Body.color = new Color(1f, 1f, 1f, 0.45f);
            }

            var change = _dda.Register(all);
            if (all)
            {
                GameFeel.Correct(_streak);
                SetPrompt(_streak >= 3 ? $"¡Todos! · racha {_streak}" : "¡Todos!", GoodColor);
                foreach (var s in _sats)
                    if (s.Target) StartCoroutine(UiFx.SparkBurst(_fxRect, LocalIn(_fxRect, s.Rect), SignalColor, 10, 160f, 30f, 0.45f));
            }
            else
            {
                GameFeel.Wrong();
                SetPrompt($"{hits} de {_targets}", SignalColor);
            }
            StartCoroutine(FloatText(LocalIn(_fxRect, _pickRow) + new Vector2(0f, 40f), "+" + pts, NeuroStyle.Sun));

            // El camino que hicieron los de la señal.
            yield return StartCoroutine(DrawTrails());

            // ¿Dónde se confundió? El punto en que el que faltó pasó más cerca del que se eligió por error.
            float hold = all ? 1.0f : 1.4f;
            if (missed != null && wrong != null)
            {
                int k = SatelliteContract.ClosestApproach(missed.PathX, missed.PathY, wrong.PathX, wrong.PathY);
                if (k >= 0)
                {
                    float mx = (missed.PathX[k] + wrong.PathX[k]) * 0.5f, my = (missed.PathY[k] + wrong.PathY[k]) * 0.5f;
                    _crossMarker.anchoredPosition = ToUi(mx, my);
                    _crossMarker.gameObject.SetActive(true);
                    _crossLabel.text = "¿Aquí se cruzaron?";
                    StartCoroutine(PopIn(_crossMarker, 0.3f));
                    hold = 2.2f;
                }
            }

            if (change == DdaChange.Up)
            {
                _toast.Show("Sube la dificultad", LevelNews(_dda.Level), GoodColor, 1.0f);
                GameFeel.LevelUp();
            }
            else if (change == DdaChange.Down || _dda.Struggling)
                _toast.Show("Con calma", "Un poco más fácil", SignalColor, 0.9f);
            UpdateHud();

            yield return StartCoroutine(Wait(hold));
            HideAll();
        }

        private static string LevelNews(int level)
        {
            if (SatelliteContract.Targets(level) > SatelliteContract.Targets(level - 1)) return $"Ahora son {SatelliteContract.Targets(level)} con señal";
            if (SatelliteContract.Total(level) > SatelliteContract.Total(level - 1)) return "Un satélite más en órbita";
            return $"Más rápido · {SatelliteContract.SpeedFactor(level):0.0}×".Replace('.', ',');
        }

        // ------------------------------------------------------------------ entrada

        private void Update()
        {
            UpdateClock();
            if (_phase == Phase.Cue || _phase == Phase.Track || _phase == Phase.Answer) Wobble();
            if (_phase != Phase.Answer || GameClock.DeltaTime <= 0f || _swarm == null) return;
            if (!Input.GetMouseButtonDown(0)) return;
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(_arena, Input.mousePosition, null, out var local)) return;
            float x = local.x / _arenaW + 0.5f, y = local.y / _arenaW + _fieldH * 0.5f;
            int i = _swarm.Nearest(x, y, SatelliteSwarm.Radius * 1.9f);
            if (i < 0) return;
            var s = _sats[i];
            if (s.Picked)
            {
                s.Picked = false;
                _picked--;
                s.Ring.gameObject.SetActive(false);
            }
            else if (_picked < _targets)
            {
                s.Picked = true;
                _picked++;
                s.Ring.gameObject.SetActive(true);
                s.Ring.color = PickColor;
                s.Ring.rectTransform.localScale = Vector3.one;
                StartCoroutine(PopRect(s.Rect, 1.15f, 0.18f));
            }
            GameFeel.Haptic(GameFeel.HapticKind.Light);
            PlayTone(s.Picked ? 784f : 523f, 0.05f, 0.05f);
            SetPickRow(_picked);
        }

        /// <summary>Todos los satélites se balancean IGUAL (vida sin dar pistas de cuál es cuál).</summary>
        private void Wobble()
        {
            float a = Motion.Decorative ? 7f * Mathf.Sin(GameClock.Time * 1.7f) : 0f; // sin ReduceMotion: quietos
            foreach (var s in _sats) s.Body.rectTransform.localRotation = Quaternion.Euler(0f, 0f, a);
        }

        private void UpdateClock()
        {
            if (!Endless || _phase == Phase.Idle || _phase == Phase.Done || _endsAt <= 0f) return;
            float left = _endsAt - GameClock.Time;
            SetTimerFraction(Mathf.Clamp01(left / SatelliteContract.RetoSeconds));
            int whole = Mathf.CeilToInt(left);
            if (whole <= 5 && whole >= 1 && whole != _lastTickSecond)
            {
                _lastTickSecond = whole;
                GameFeel.Tick();
            }
        }

        // ------------------------------------------------------------------ dibujo

        private Vector2 ToUi(float x, float y) => new Vector2((x - 0.5f) * _arenaW, (y - _fieldH * 0.5f) * _arenaW);

        private void PlaceAll()
        {
            if (_swarm == null) return;
            for (int i = 0; i < _swarm.Count; i++) _sats[i].Rect.anchoredPosition = ToUi(_swarm.X[i], _swarm.Y[i]);
        }

        /// <summary>Estela del recorrido de cada satélite con señal: puntos que se encienden del principio al final.</summary>
        private IEnumerator DrawTrails()
        {
            var dots = new List<(Vector2 pos, float age)>();
            foreach (var s in _sats)
            {
                if (!s.Target || !s.Rect.gameObject.activeSelf) continue;
                int n = s.PathX.Count;
                for (int k = 0; k < n; k += 2) dots.Add((ToUi(s.PathX[k], s.PathY[k]), n > 1 ? (float)k / (n - 1) : 1f));
            }
            int used = Mathf.Min(dots.Count, _trail.Count);
            // Si hay más puntos que imágenes, se reparten parejo.
            float step = dots.Count > 0 ? (float)dots.Count / Mathf.Max(1, used) : 1f;
            for (int i = 0; i < used; i++)
            {
                var d = dots[Mathf.Min(dots.Count - 1, Mathf.FloorToInt(i * step))];
                var img = _trail[i];
                img.rectTransform.anchoredPosition = d.pos;
                float size = Mathf.Lerp(6f, 13f, d.age);
                img.rectTransform.sizeDelta = new Vector2(size, size);
                img.color = NeuroStyle.WithAlpha(SignalColor, 0f);
                img.gameObject.SetActive(true);
            }
            float t = 0f;
            const float seconds = 0.6f;
            while (t < seconds)
            {
                t += GameClock.DeltaTime;
                float k = Mathf.Clamp01(t / seconds);
                for (int i = 0; i < used; i++)
                {
                    var d = dots[Mathf.Min(dots.Count - 1, Mathf.FloorToInt(i * step))];
                    float on = Mathf.Clamp01((k - d.age * 0.8f) / 0.2f);
                    _trail[i].color = NeuroStyle.WithAlpha(SignalColor, on * Mathf.Lerp(0.18f, 0.6f, d.age));
                }
                yield return null;
            }
        }

        private void HideTrails()
        {
            foreach (var d in _trail) d.gameObject.SetActive(false);
        }

        private void HideAll()
        {
            foreach (var s in _sats) s.Rect.gameObject.SetActive(false);
            HideTrails();
            if (_crossMarker != null) _crossMarker.gameObject.SetActive(false);
        }

        private void SetPickRow(int picked)
        {
            for (int i = 0; i < _pickDots.Count; i++)
            {
                var d = _pickDots[i];
                d.gameObject.SetActive(i < _targets);
                d.color = i < picked ? PickColor : new Color(1f, 1f, 1f, 0.16f);
            }
            float gap = 58f;
            for (int i = 0; i < _targets && i < _pickDots.Count; i++)
                _pickDots[i].rectTransform.anchoredPosition = new Vector2((i - (_targets - 1) * 0.5f) * gap, 0f);
        }

        private void SetPrompt(string text, Color color)
        {
            _prompt.text = text;
            _prompt.color = color;
        }

        private IEnumerator FloatText(Vector2 pos, string text, Color color)
        {
            var t = MakeText(_fxRect, "Float", 58, TextAnchor.MiddleCenter, color, 0f, 0f);
            NeuroStyle.ClayText(t, 4f, 6f);
            var r = t.rectTransform;
            r.anchorMin = r.anchorMax = new Vector2(0.5f, 0.5f);
            r.sizeDelta = new Vector2(300f, 90f);
            t.text = text;
            float e = 0f;
            const float seconds = 0.8f;
            while (e < seconds)
            {
                e += GameClock.DeltaTime;
                float k = Mathf.Clamp01(e / seconds);
                r.anchoredPosition = pos + new Vector2(0f, 60f * (Motion.Decorative ? UiFx.EaseOutCubic(k) : 0f));
                t.color = NeuroStyle.WithAlpha(color, 1f - k * k);
                yield return null;
            }
            Destroy(t.gameObject);
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

        // ------------------------------------------------------------------ fin

        private IEnumerator FinishGame()
        {
            _phase = Phase.Done;
            HideAll();

            float hitRate = _targetsTotal > 0 ? (float)_hitsTotal / _targetsTotal : 0f;
            float capacity = SatelliteContract.Capacity(_rounds);
            float meanTargets = 0f;
            foreach (var r in _rounds) meanTargets += r.targets;
            meanTargets = _rounds.Count > 0 ? meanTargets / _rounds.Count : -1f;
            float speedReached = _bestCleared > 0 ? SatelliteContract.SpeedFactor(_bestCleared) : -1f;
            int score = SatelliteContract.Score(hitRate, _bestCleared);

            SetPrompt("Fin de la misión", GoodColor);
            ShowResult(score, capacity, meanTargets);

            var telemetry = new StroopTelemetry
            {
                user_id = _config.user_id,
                game_id = _config.game_id,
                session_metrics = new StroopSessionMetrics
                {
                    correct_trials = _cleared,
                    total_trials = _rounds.Count,
                    calculated_score = score,
                    average_response_time_ms = 0,
                    level = _config.config.level,
                    timed = _config.config.timed,
                    end_rating = _dda.RatingNormalized,
                    mode_trials = _dda.ScoredTrials,
                    mode_hits = _dda.ScoredCorrect,
                    peak_level = _dda.PeakLevel,
                    tracking_capacity = capacity,
                    tracking_targets = meanTargets,
                    tracking_speed = speedReached
                }
            };
            NativeBridge.ForwardTelemetryToPlatform(JsonUtility.ToJson(telemetry));
            yield break;
        }

        private void ShowResult(int score, float capacity, float meanTargets)
        {
            _exit.Show();
            _resultRoot.Find("Title").GetComponent<Text>().text = score >= 85 ? "¡Control de misión experto!" : score >= 65 ? "¡Buena misión!" : "Misión completada";
            _resultRoot.Find("Detail").GetComponent<Text>().text = $"{_hitsTotal} de {_targetsTotal} satélites encontrados";
            // "de N": cuántos había que seguir. El juego sube esa cantidad al acertar: el número no es un techo personal.
            _resultRoot.Find("Extra").GetComponent<Text>().text = capacity >= 0f
                ? $"Seguiste {capacity:0.0} de {meanTargets:0.#} a la vez".Replace('.', ',') : $"Mejor racha {_bestStreak}";
            _resultRoot.gameObject.SetActive(true);
            StartCoroutine(AnimateResult(score));
        }

        // ------------------------------------------------------------------ construcción de UI

        protected override void BuildUi()
        {

            var canvasGo = new GameObject("SatelliteCanvas");
            canvasGo.transform.SetParent(transform, false);
            var canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvasGo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080f, 1920f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0f;
            canvasGo.AddComponent<GraphicRaycaster>();

            var bg = new GameObject("Background");
            bg.transform.SetParent(canvasGo.transform, false);
            var bgRect = bg.AddComponent<RectTransform>();
            Stretch(bgRect);
            WorldBackdrop.Build(bgRect, GameWorld.MissionControl);

            var safeGo = new GameObject("SafeAreaContent");
            safeGo.transform.SetParent(canvasGo.transform, false);
            _safe = safeGo.AddComponent<RectTransform>();
            ApplySafeArea(_safe);

            _hud = new GameHud(_safe, "Satélites", MarginU, this);
            BuildTimer();

            _prompt = MakeText(_safe, "Prompt", 64, TextAnchor.MiddleCenter, Color.white, 0f, 0f);
            NeuroStyle.ClayText(_prompt, 3.5f, 5f);
            var pr = _prompt.rectTransform;
            pr.anchorMin = pr.anchorMax = new Vector2(0.5f, 1f);
            pr.pivot = new Vector2(0.5f, 0.5f);
            BestFit(_prompt, 42);

            BuildArena();
            BuildPickRow();

            var fx = new GameObject("Fx");
            fx.transform.SetParent(_safe, false);
            _fxRect = fx.AddComponent<RectTransform>();
            Stretch(_fxRect);

            BuildResultPanel();
            _exit = new ExitButton(_safe, this, UnitsPerDp);
            _toast = new Toast(_safe, this, UnitsPerDp);
            _toast.SetBelowHud();
            _toast.KeepOut(_arena);                         // el campo donde se mueven los satélites
            _toast.KeepOut(_prompt.rectTransform);          // la consigna, aunque en este momento esté vacía

            var flashGo = new GameObject("Flash");
            flashGo.transform.SetParent(canvasGo.transform, false);
            Stretch(flashGo.AddComponent<RectTransform>());
            _flash = flashGo.AddComponent<Image>();
            _flash.raycastTarget = false;
            _flash.color = new Color(0f, 0f, 0f, 0f);

            _countdown = new CountdownScreen(canvasGo.transform, UnitsPerDp);
        }

        private void BuildArena()
        {
            var go = new GameObject("Arena");
            go.transform.SetParent(_safe, false);
            _arena = go.AddComponent<RectTransform>();
            _arena.anchorMin = _arena.anchorMax = new Vector2(0.5f, 1f);
            _arena.pivot = new Vector2(0.5f, 0.5f);
            // El campo: un vidrio apenas visible con borde fino (marca el límite donde rebotan; no es una tarjeta).
            _arenaImage = go.AddComponent<Image>();
            _arenaImage.sprite = RoundedRectSprite.Get(64);
            _arenaImage.type = Image.Type.Sliced;
            _arenaImage.color = new Color(1f, 1f, 1f, 0.035f);
            _arenaImage.raycastTarget = false;
            var edge = go.AddComponent<Outline>();
            edge.effectColor = NeuroStyle.WithAlpha(NeuroStyle.Sky, 0.25f);
            edge.effectDistance = new Vector2(2f, -2f);

            var trailRoot = new GameObject("Trails");
            trailRoot.transform.SetParent(_arena, false);
            var tr = trailRoot.AddComponent<RectTransform>();
            Stretch(tr);
            for (int i = 0; i < TrailPool; i++) _trail.Add(NewImage(tr, "Dot", DiscSprite.Get()));

            for (int i = 0; i < MaxSatellites; i++) _sats.Add(BuildSat(i));

            // Marcador de la confusión: anillo coral + texto.
            var cm = new GameObject("CrossMarker");
            cm.transform.SetParent(_arena, false);
            _crossMarker = cm.AddComponent<RectTransform>();
            _crossMarker.anchorMin = _crossMarker.anchorMax = new Vector2(0.5f, 0.5f);
            _crossMarker.sizeDelta = new Vector2(150f, 150f);
            var ring = cm.AddComponent<Image>();
            ring.sprite = RingSprite.Get();
            ring.color = BadColor;
            ring.raycastTarget = false;
            _crossLabel = MakeText(_crossMarker, "Label", 42, TextAnchor.MiddleCenter, Color.white, 0f, 0f);
            NeuroStyle.ClayText(_crossLabel, 3f, 4f);
            var lr = _crossLabel.rectTransform;
            lr.anchorMin = lr.anchorMax = new Vector2(0.5f, 0f);
            lr.pivot = new Vector2(0.5f, 1f);
            lr.sizeDelta = new Vector2(460f, 60f);
            lr.anchoredPosition = new Vector2(0f, -6f);
            cm.SetActive(false);
        }

        private Sat BuildSat(int i)
        {
            var go = new GameObject("Satellite" + i);
            go.transform.SetParent(_arena, false);
            var rect = go.AddComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);

            var glow = NewImage(rect, "Glow", RadialGlowSprite.Get());
            // Disco tenue detrás: el satélite es delgado (paneles); así se ve como una ficha y se sigue mejor.
            var halo = NewImage(rect, "Halo", DiscSprite.Get());
            halo.color = NeuroStyle.WithAlpha(NeuroStyle.Sky, 0.13f);
            halo.gameObject.SetActive(true);
            var ring = NewImage(rect, "Ring", RingSprite.Get());
            var body = NewImage(rect, "Body", SymbolSprite.Get(ShapeKind.Satellite, 0));
            body.preserveAspect = true;
            body.gameObject.SetActive(true);
            var mark = NewImage(rect, "Mark", null);
            go.SetActive(false);
            return new Sat { Rect = rect, Body = body, Halo = halo, Glow = glow, Ring = ring, Mark = mark };
        }

        private void BuildPickRow()
        {
            var go = new GameObject("PickRow");
            go.transform.SetParent(_safe, false);
            _pickRow = go.AddComponent<RectTransform>();
            _pickRow.anchorMin = _pickRow.anchorMax = new Vector2(0.5f, 0f);
            _pickRow.pivot = new Vector2(0.5f, 0.5f);
            _pickRow.sizeDelta = new Vector2(600f, 60f);
            for (int i = 0; i < 5; i++)
            {
                var d = NewImage(_pickRow, "Pick", DiscSprite.Get());
                d.rectTransform.sizeDelta = new Vector2(38f, 38f);
                _pickDots.Add(d);
            }
        }

        private void BuildTimer()
        {
            var bg = new GameObject("TimerBar");
            bg.transform.SetParent(_safe, false);
            _timerBg = bg.AddComponent<RectTransform>();
            _timerBg.anchorMin = _timerBg.anchorMax = new Vector2(0.5f, 1f);
            _timerBg.pivot = new Vector2(0.5f, 0.5f);
            var bgImg = bg.AddComponent<Image>();
            bgImg.sprite = RoundedRectSprite.Get(10);
            bgImg.type = Image.Type.Sliced;
            bgImg.color = new Color(1f, 1f, 1f, 0.12f);
            bgImg.raycastTarget = false;

            var fill = new GameObject("Fill");
            fill.transform.SetParent(bg.transform, false);
            _timerFill = fill.AddComponent<RectTransform>();
            _timerFill.anchorMin = Vector2.zero;
            _timerFill.anchorMax = Vector2.one;
            _timerFill.offsetMin = _timerFill.offsetMax = Vector2.zero;
            var img = fill.AddComponent<Image>();
            img.sprite = RoundedRectSprite.Get(10);
            img.type = Image.Type.Sliced;
            img.raycastTarget = false;
            img.color = GoodColor;
        }

        private void SetTimerFraction(float fraction)
        {
            _timerFill.anchorMax = new Vector2(Mathf.Clamp01(fraction), 1f);
            _timerFill.offsetMin = _timerFill.offsetMax = Vector2.zero;
            _timerFill.GetComponent<Image>().color = fraction > 0.5f ? Color.Lerp(SignalColor, GoodColor, (fraction - 0.5f) * 2f)
                                                                      : Color.Lerp(BadColor, SignalColor, fraction * 2f);
        }

        private void BuildResultPanel()
        {
            var go = new GameObject("Result");
            go.transform.SetParent(_safe, false);
            _resultRoot = go.AddComponent<RectTransform>();
            _resultRoot.anchorMin = _resultRoot.anchorMax = new Vector2(0.5f, 0.5f);
            _resultRoot.pivot = new Vector2(0.5f, 0.5f);
            _resultRoot.sizeDelta = new Vector2(880f, 760f);
            var img = go.AddComponent<Image>();
            img.sprite = RoundedRectSprite.Get(64);
            img.type = Image.Type.Sliced;
            img.color = new Color(0.08f, 0.12f, 0.22f, 0.96f);

            AddResultText("Title", 84, new Vector2(0f, 250f), Color.white);
            AddResultText("Score", 260, new Vector2(0f, 60f), Color.white);
            AddResultText("Detail", 48, new Vector2(0f, -150f), new Color(1f, 1f, 1f, 0.85f));
            AddResultText("Extra", 44, new Vector2(0f, -250f), new Color(1f, 1f, 1f, 0.65f));
            go.SetActive(false);
        }

        private static Image NewImage(Transform parent, string name, Sprite sprite)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var r = go.AddComponent<RectTransform>();
            r.anchorMin = r.anchorMax = new Vector2(0.5f, 0.5f);
            r.pivot = new Vector2(0.5f, 0.5f);
            var img = go.AddComponent<Image>();
            img.sprite = sprite;
            img.raycastTarget = false;
            go.SetActive(false);
            return img;
        }

        // ------------------------------------------------------------------ layout

        private void Layout()
        {
            Rect safe = _safe.rect;
            float sw = Mathf.Max(safe.width, 400f);
            float sh = Mathf.Max(safe.height, 800f);
            float contentW = sw - MarginU * 2f;

            float y = GameHud.Height + 6f;
            _timerBg.sizeDelta = new Vector2(contentW, 16f);
            _timerBg.anchoredPosition = new Vector2(0f, -(y + 8f));
            y += 16f + 14f;

            const float promptH = 100f;
            _prompt.rectTransform.sizeDelta = new Vector2(contentW, promptH);
            _prompt.rectTransform.anchoredPosition = new Vector2(0f, -(y + promptH / 2f));
            y += promptH + 10f;

            // Campo: todo el ancho y lo que queda de alto, menos la fila de elegidos.
            const float bottomH = 150f;
            _arenaW = contentW + 20f;
            _arenaH = Mathf.Max(600f, sh - y - bottomH);
            _arenaH = Mathf.Min(_arenaH, _arenaW * 1.6f);
            _fieldH = _arenaH / _arenaW;
            _arena.sizeDelta = new Vector2(_arenaW, _arenaH);
            _arena.anchoredPosition = new Vector2(0f, -(y + _arenaH / 2f));
            _pickRow.anchoredPosition = new Vector2(0f, Mathf.Max(60f, (sh - y - _arenaH) * 0.5f));

            // Tamaños proporcionales al campo (el radio del contrato es en anchos de campo).
            float diameter = SatelliteSwarm.Radius * 2f * _arenaW;
            _satSize = diameter * 1.25f; // el ícono trae margen para el borde y la sombra
            foreach (var s in _sats)
            {
                s.Rect.sizeDelta = new Vector2(_satSize, _satSize);
                Stretch(s.Body.rectTransform);
                s.Halo.rectTransform.sizeDelta = new Vector2(diameter * 1.1f, diameter * 1.1f);
                s.Glow.rectTransform.sizeDelta = new Vector2(_satSize * 2.3f, _satSize * 2.3f);
                s.Ring.rectTransform.sizeDelta = new Vector2(diameter * 1.35f, diameter * 1.35f);
                s.Mark.rectTransform.sizeDelta = new Vector2(_satSize * 0.5f, _satSize * 0.5f);
                s.Mark.rectTransform.anchoredPosition = new Vector2(_satSize * 0.38f, _satSize * 0.38f);
            }
        }

        private void UpdateHud()
        {
            _hud.SetLevel(_dda.PresentedLevel);
            if (Endless) _hud.SetPoints(_points);
            else _hud.SetInfo($"{Mathf.Min(_rounds.Count + 1, SatelliteContract.PrecisionRounds)} de {SatelliteContract.PrecisionRounds}");
        }
    }
}
