using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using NeuroVida.Bridge;
using NeuroVida.Contracts;
using NeuroVida.Games.Secuencia; // RoundedRectSprite / RadialGlowSprite / TileSprites / HarmonicTone
using NeuroVida.Games.Shared;
using static NeuroVida.Games.Shared.UiKit;
using Motion = NeuroVida.Games.Shared.Motion; // UnityEngine.Motion también existe

namespace NeuroVida.Games.Stroop
{
    /// <summary>
    /// «Tinta o Palabra» — modelo «Dos orillas» (aprobado el 3-oct; ficha: <c>docs/diseno-tinta-o-palabra.md</c>). Una palabra de color llega deslizándose desde una de dos orillas:
    /// la de la TINTA (izquierda, violeta, gotas) o la de la PALABRA (derecha, turquesa, libros). La orilla manda: desde la TINTA se toca el color con que está escrita; desde la
    /// PALABRA, lo que dice. Al acertar, la tarjeta sale hacia la orilla contraria; al fallar, Nubi explica sin culpa («La tinta era azul») y un aro marca el botón correcto.
    /// Las orillas son HACES DE LUZ (compuertas de una estación): estética espacial, nada de papel. La regla se ve en tres señales a la vez (orilla, ícono y cinta fija),
    /// que es lo que más baja el costo de cambiar de regla (Monsell, 2003). Reglas puras y medidas en <see cref="StroopContract"/>.
    /// La UI se arma por código; todo dentro del Safe Area. Con «quitar animaciones»: fundido en vez de deslizamiento, sin temblor ni chispas, y los mismos tiempos.
    /// </summary>
    public class StroopGameController : GameControllerBase
    {
        public const string GameId = StroopContract.GameId;

        private const float UnitsPerDp = 3f;
        private const float MarginU = 60f;
        private const float ShoreW = 120f;
        private const float NoAnswer = -2f;
        private const int TimeUp = -3;
        /// <summary>Lugar que se reserva abajo durante la ronda guiada para «Práctica: no cuenta» y «Saltar tutorial» (los botones suben).</summary>
        private const float GuidedReserveU = 270f;

        /// <summary>Tinta de arcilla (<c>Ink</c> 1A1240): el nombre sobre AZUL, AMARILLO y BLANCO (contrastes 4,9 · 11,2 · 15,9 : 1); sobre ROJO va blanco puro (5,0 : 1).</summary>
        private static readonly Color InkLabel = new Color(0x1A / 255f, 0x12 / 255f, 0x40 / 255f);
        private static readonly Color GoodColor = new Color(0x22 / 255f, 0xC5 / 255f, 0x5E / 255f);
        private static readonly Color BadColor = new Color(0xEF / 255f, 0x44 / 255f, 0x44 / 255f);
        private static readonly Color AmberColor = new Color(0xF5 / 255f, 0x9E / 255f, 0x0B / 255f);
        /// <summary>El aro por FUERA del botón correcto: celeste 7FD8FF con borde tinta, que se distingue alrededor de las cuatro tintas (también del botón blanco).</summary>
        private static readonly Color RingColor = new Color(0x7F / 255f, 0xD8 / 255f, 0xFF / 255f);
        private const float RingPad = 22f;

        private System.Random _rng;
        private StroopContract.Sequencer _seq;

        // Estado de partida
        private int _trialIndex;
        private int _correct;
        private int _streak;
        private int _bestStreak;
        private long _responseMsSum;
        private int _responseCount;
        private int _answerIndex = (int)NoAnswer;
        private float _answerAt;
        private bool _acceptInput;
        private bool _ended;
        private bool _loopOn;
        private bool _taughtWord;
        private StroopTrial _trial;
        private int _totalAnswered;
        private AdaptiveDifficulty _dda; // DDA común (ver docs/DDA-comun.md)
        private int _effLevel = 1;
        private int _points;
        private float _roundEndsAt;
        private int _lastTickSecond = -1;
        private readonly List<StroopResponse> _responses = new List<StroopResponse>();
        private bool Senior => _config != null && _config.config != null && DdaUserProfileConfig.ParseAgeBand(_config.config.age_band) == AgeBand.Senior;

        /// <summary>Modo Reto = ronda contra el reloj SIN límite de palabras: se responde todo lo posible en <see cref="StroopContract.EndlessSeconds"/>.
        /// Modo Precisión (sin reloj) = <see cref="StroopContract.TotalTrials"/> palabras.</summary>
        private bool Endless => _config != null && _config.config.timed;

        // UI
        private RectTransform _safe;
        private Text _wordText, _ribbonText;
        private Image _ribbonBg, _ribbonIcon, _wordGlow, _cardBorder, _cardCorner, _cardCornerIcon;
        private RectTransform _ribbonRect, _timerBg, _timerFill, _cardRect, _fxRect, _stageRect;
        private CanvasGroup _cardGroup;
        private readonly CanvasGroup[] _shoreGroup = new CanvasGroup[2];
        private readonly RectTransform[] _shoreRect = new RectTransform[2];
        private readonly List<RectTransform> _buttonRects = new List<RectTransform>();
        private readonly List<Image> _buttonImages = new List<Image>();
        private readonly List<CanvasGroup> _buttonGroups = new List<CanvasGroup>();
        private readonly List<RectTransform> _ringRects = new List<RectTransform>();
        private ProgressDots _dots;
        private PhasePill _pill;
        private Toast _toast;
        private ExitButton _exit;
        private GameHud _hud;
        private CountdownScreen _countdown;
        // «Ahora: PALABRA»
        private RectTransform _ruleCardRoot;
        private CanvasGroup _ruleCardGroup;
        private float _bottomReserve;
        private Vector2 _cardRestPos;
        private float _cardW;

        // ------------------------------------------------------------------ sesión

        public void StartSession(SequenceInitConfig config)
        {
            _config = config;
            _rng = new System.Random();
            _seq = new StroopContract.Sequencer(_rng);
            _trialIndex = 0;
            _correct = 0;
            _streak = 0;
            _bestStreak = 0;
            _responseMsSum = 0;
            _responseCount = 0;
            _ended = false;
            _loopOn = false;
            _guided = false;
            _taughtWord = false;
            _acceptInput = false;
            _totalAnswered = 0;
            _points = 0;
            _responses.Clear();
            _bottomReserve = 0f;
            _dda = new AdaptiveDifficulty(StroopContract.MaxLevel, DdaUserProfileConfig.ParseAgeBand(config.config.age_band),
                AdaptiveDifficulty.StartRating(config.config, StroopContract.MaxLevel),
                stepUp: config.config.timed ? 0.12f : 0.2f, useReaction: config.config.timed);
            _effLevel = _dda.PresentedLevel;
            _lastTickSecond = -1;

            _dots.Reset();
            _resultRoot.gameObject.SetActive(false);
            _exit.Hide();
            _ribbonRect.gameObject.SetActive(true);
            _stageRect.gameObject.SetActive(true);
            _pill.Rect.gameObject.SetActive(true);
            foreach (var r in _buttonRects) r.gameObject.SetActive(true);
            _cardRect.gameObject.SetActive(true);
            _cardGroup.alpha = 0f;
            _ruleCardRoot.gameObject.SetActive(false);
            ClearRings();
            SetStreak(0);
            ApplyRule(StroopRule.Ink, false);
            _timerBg.gameObject.SetActive(config.config.timed);
            _dots.Rect.gameObject.SetActive(!config.config.timed);

            StopAllCoroutines();
            StartCoroutine(GameLoop());
        }

        private IEnumerator GameLoop()
        {
            _safe.gameObject.SetActive(false);
            if (TutorialWanted)
            {
                // la ronda guiada se juega sobre el cielo ya armado, antes de la cuenta regresiva
                _safe.gameObject.SetActive(true);
                yield return null;
                ApplySafeArea(_safe);
                Canvas.ForceUpdateCanvases();
                Layout();
                UpdateHudText();
                yield return StartCoroutine(RunTutorialIfNeeded());
            }
            _safe.gameObject.SetActive(false);
            yield return StartCoroutine(_countdown.Play("Tinta o Palabra", Assessment.Subtitle("Prepárate"), () => _safe.gameObject.SetActive(true)));
            _safe.gameObject.SetActive(true);
            yield return null; // deja que el Canvas calcule el rect del Safe Area antes de medir
            ApplySafeArea(_safe);
            Canvas.ForceUpdateCanvases();
            Layout();

            _roundEndsAt = GameClock.Time + StroopContract.EndlessSeconds;
            _trialIndex = 0;
            _loopOn = true;
            yield return StartCoroutine(MainLoop());
        }

        /// <summary>El bucle de la partida: una palabra tras otra hasta que se acaben (Precisión) o el tiempo (Reto). «Cómo se juega» lo retoma desde acá.</summary>
        private IEnumerator MainLoop()
        {
            while (Endless ? GameClock.Time < _roundEndsAt : _trialIndex < StroopContract.TotalTrials)
            {
                yield return StartCoroutine(PresentTrial());

                float startedAt = GameClock.Time;
                while (_answerIndex == (int)NoAnswer)
                {
                    if (Endless && UpdateRoundClock()) _answerIndex = TimeUp;
                    yield return null;
                }
                _acceptInput = false;

                if (_answerIndex == TimeUp)
                {
                    yield return StartCoroutine(FadeCardOut());
                    break;
                }

                bool correct = _answerIndex == _trial.CorrectIndex;
                float ms = (_answerAt - startedAt) * 1000f;
                _totalAnswered++;
                _responseMsSum += (long)ms;
                _responseCount++;
                _responses.Add(new StroopResponse(correct, ms, _trial.Clash, _trial.Switched));
                var change = _dda.Register(correct, ms);
                yield return StartCoroutine(ResolveTrial(correct));
                if (change == DdaChange.Up)
                {
                    _toast.Show($"Nivel {_dda.Level}", _dda.Level == 3 ? "Ahora llegan por las dos orillas" : "Más difícil", GoodColor, 1.0f);
                    GameFeel.LevelUp();
                }
                else if (change == DdaChange.Down || _dda.Struggling) _toast.Show("Con calma", "Ajustamos la dificultad", AmberColor, 1.0f);
                _trialIndex++;
            }

            _loopOn = false;
            yield return StartCoroutine(FinishGame());
        }

        // ------------------------------------------------------------------ palabra

        private IEnumerator PresentTrial()
        {
            _answerIndex = (int)NoAnswer;
            _effLevel = _dda.PresentedLevel;
            _trial = _seq.Next(_effLevel);
            if (!Endless) _dots.MarkCurrent(_trialIndex);
            UpdateHudText();
            if (!Endless) SetTimerFraction(1f);
            ClearRings();
            ResetButtons();

            // la primera vez que una palabra llega por la orilla de la PALABRA, una tarjeta lo explica (no le gasta tiempo al Reto)
            if (_trial.Rule == StroopRule.Word && !_taughtWord)
            {
                _taughtWord = true;
                yield return StartCoroutine(ShowRuleCard());
            }
            yield return StartCoroutine(SlideIn(_trial, StroopContract.ArrivalSeconds(_effLevel, Senior), _trial.Switched));
            _acceptInput = true;
        }

        /// <summary>Pone la palabra en la tarjeta, enciende la orilla de la que llega y la hace llegar deslizándose desde ella (con «quitar animaciones»: aparece con un fundido, mismo tiempo).</summary>
        private IEnumerator SlideIn(StroopTrial trial, float seconds, bool pop)
        {
            _wordText.text = StroopContract.Names[trial.WordIndex];
            var ink = StroopContract.Colors[trial.InkIndex];
            _wordText.color = ink;
            _wordGlow.color = new Color(ink.r, ink.g, ink.b, 0.18f);
            ApplyRule(trial.Rule, pop);
            if (_shownCardSide != (trial.Rule == StroopRule.Ink ? -1 : 1)) _shownCardSide = trial.Rule == StroopRule.Ink ? -1 : 1;
            float side = _shownCardSide;
            float startX = side * (_stageRect.rect.width * 0.5f - ShoreW - 10f);
            _cardRect.localScale = Vector3.one;
            float t = 0f;
            while (t < seconds)
            {
                t += GameClock.DeltaTime;
                float k = Mathf.Clamp01(t / seconds);
                float e = UiFx.EaseOutCubic(k);
                _cardRect.anchoredPosition = _cardRestPos + new Vector2(Motion.Decorative ? startX * (1f - e) : 0f, 0f);
                _cardGroup.alpha = Mathf.Clamp01(k * 2.4f);
                yield return null;
            }
            _cardRect.anchoredPosition = _cardRestPos;
            _cardGroup.alpha = 1f;
        }

        private int _shownCardSide;

        private IEnumerator ResolveTrial(bool correct)
        {
            if (!Endless) _dots.Mark(_trialIndex, correct);
            var ink = StroopContract.Colors[_trial.InkIndex];

            if (correct)
            {
                _correct++;
                _streak++;
                _bestStreak = Mathf.Max(_bestStreak, _streak);
                _points += 100 + 25 * Mathf.Min(_streak - 1, 8);
                UpdateHudText();
                SetStreak(_streak);
                _pill.Set("¡Bien!", GoodColor);
                GameFeel.Correct(_streak);
                StartCoroutine(Flash(GoodColor, 0.08f, 0.24f));
                if (Motion.Decorative)
                {
                    StartCoroutine(UiFx.SparkBurst(_fxRect, LocalIn(_fxRect, _cardRect), ink, 14, 280f, 42f, 0.55f));
                    StartCoroutine(UiFx.RingBurst(_fxRect, LocalIn(_fxRect, _buttonRects[_answerIndex]), Color.white, 120f, 420f, 0.45f));
                }
                if (_streak == 4 || _streak == 8 || _streak == 12)
                    _toast.Show($"Racha de {_streak}", "Sigue así", GoodColor, 0.9f);

                // la tarjeta sale hacia la orilla CONTRARIA a la de la que llegó
                yield return StartCoroutine(SlideOut(Endless ? 0.17f : 0.24f, -_shownCardSide));
                if (!Endless) yield return Motion.Hold(0.08f);
            }
            else
            {
                _streak = 0;
                SetStreak(0);
                int correctIndex = _trial.CorrectIndex;
                _pill.Set(StroopContract.Explain(_trial), AmberColor);
                GameFeel.Wrong();
                _cardBorder.color = new Color(BadColor.r, BadColor.g, BadColor.b, 0.9f);
                StartCoroutine(Flash(BadColor, 0.12f, 0.3f));
                if (Motion.Decorative) StartCoroutine(UiFx.Shake(20f, 0.35f, _cardRect));
                ShowRing(correctIndex);
                if (Motion.Decorative) StartCoroutine(UiFx.RingBurst(_fxRect, LocalIn(_fxRect, _buttonRects[correctIndex]), Color.white, 140f, 460f, 0.55f));
                // una pausa para leer la explicación; no le gasta tiempo al Reto
                yield return Motion.Hold(StroopContract.ErrorPauseSeconds);
                if (Endless) _roundEndsAt += StroopContract.ErrorPauseSeconds;
                yield return StartCoroutine(FadeCardOut());
            }
            _cardGroup.alpha = 0f;
            ClearRings();
        }

        /// <summary>La tarjeta sale hacia <paramref name="toSide"/> (−1 izquierda, +1 derecha) mientras se desvanece; sin movimiento con «quitar animaciones».</summary>
        private IEnumerator SlideOut(float seconds, int toSide)
        {
            float t = 0f;
            float dist = toSide * (_stageRect.rect.width * 0.5f - ShoreW - 10f);
            while (t < seconds)
            {
                t += GameClock.DeltaTime;
                float k = UiFx.EaseOutCubic(Mathf.Clamp01(t / seconds));
                _cardRect.anchoredPosition = _cardRestPos + new Vector2(Motion.Decorative ? dist * k : 0f, 0f);
                _cardGroup.alpha = 1f - k;
                yield return null;
            }
            _cardGroup.alpha = 0f;
            _cardRect.anchoredPosition = _cardRestPos;
        }

        private IEnumerator FadeCardOut()
        {
            float t = 0f;
            const float seconds = 0.16f;
            float from = _cardGroup.alpha;
            while (t < seconds)
            {
                t += GameClock.DeltaTime;
                _cardGroup.alpha = from * (1f - Mathf.Clamp01(t / seconds));
                yield return null;
            }
            _cardGroup.alpha = 0f;
        }

        /// <summary>Actualiza la barra del reloj global; devuelve true cuando se acabó el tiempo.
        /// En los últimos 5 segundos suena un tic por segundo.</summary>
        private bool UpdateRoundClock()
        {
            float left = _roundEndsAt - GameClock.Time;
            SetTimerFraction(Mathf.Clamp01(left / StroopContract.EndlessSeconds));
            int whole = Mathf.CeilToInt(left);
            if (whole <= 5 && whole >= 1 && whole != _lastTickSecond)
            {
                _lastTickSecond = whole;
                GameFeel.Tick();
            }
            return left <= 0f;
        }

        // ------------------------------------------------------------------ «Ahora: PALABRA»

        /// <summary>La tarjeta «Ahora: PALABRA — toca lo que DICE la palabra, no su color»: solo la primera vez en cada partida; dura unos segundos o hasta un toque.</summary>
        private IEnumerator ShowRuleCard()
        {
            float began = GameClock.Time;
            _acceptInput = false;
            _ruleCardRoot.gameObject.SetActive(true);
            PlayTone(392f, 0.14f, 0.1f);
            yield return StartCoroutine(Motion.Fade(_ruleCardGroup, 0f, 1f, Motion.FadeSeconds));
            float t = 0f;
            while (t < 2.4f)
            {
                t += GameClock.DeltaTime;
                if (t > 0.4f && GuidedTutorial.TryPress(out _)) break;     // un toque sigue
                yield return null;
            }
            yield return StartCoroutine(Motion.Fade(_ruleCardGroup, 1f, 0f, Motion.FadeSeconds));
            _ruleCardRoot.gameObject.SetActive(false);
            if (Endless) _roundEndsAt += GameClock.Time - began;            // la tarjeta no le gasta tiempo al Reto
        }

        // ------------------------------------------------------------------ fin

        private IEnumerator FinishGame()
        {
            _ended = true;
            int total = Endless ? _totalAnswered : StroopContract.TotalTrials;
            int score = Endless ? StroopContract.EndlessScore(_correct, _totalAnswered) : StroopContract.Score(_correct, total);
            int avgMs = _responseCount > 0 ? (int)(_responseMsSum / _responseCount) : 0;

            _pill.Set("Partida terminada", GoodColor);
            ShowResult(score, avgMs, total);

            var telemetry = new StroopTelemetry
            {
                user_id = _config.user_id,
                game_id = _config.game_id,
                session_metrics = new StroopSessionMetrics
                {
                    correct_trials = _correct,
                    total_trials = total,
                    calculated_score = score,
                    average_response_time_ms = avgMs,
                    level = _config.config.level,
                    timed = _config.config.timed,
                    end_rating = _dda.RatingNormalized,
                    mode_trials = _dda.ScoredTrials,
                    mode_hits = _dda.ScoredCorrect,
                    peak_level = _dda.PeakLevel,
                    interference_ms = StroopContract.InterferenceMs(_responses),
                    switch_cost_ms = StroopContract.SwitchCostMs(_responses)
                }
            };
            NativeBridge.ForwardTelemetryToPlatform(JsonUtility.ToJson(telemetry));
            yield break;
        }

        // ------------------------------------------------------------------ entrada

        private void OnColorTapped(int index)
        {
            if (!_acceptInput || _ended) return;
            _acceptInput = false;
            _answerAt = GameClock.Time;
            _answerIndex = index;
        }

        private void Update()
        {
            if (PollTutorialSkip()) return;                              // un toque en «Saltar tutorial» no es un toque a un botón
        }

        // ------------------------------------------------------------------ construcción de UI

        protected override void BuildUi()
        {

            var canvasGo = new GameObject("StroopCanvas");
            canvasGo.transform.SetParent(transform, false);
            var canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvasGo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080f, 1920f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0f; // ancho fijo (1080)
            canvasGo.AddComponent<GraphicRaycaster>();

            var bg = new GameObject("Background");
            bg.transform.SetParent(canvasGo.transform, false);
            var bgRect = bg.AddComponent<RectTransform>();
            Stretch(bgRect);
            // Mundo "Neon": cielo nocturno de la app + su elemento propio (ver Shared/WorldBackdrop.cs).
            WorldBackdrop.Build(bgRect, GameWorld.Neon);

            var safeGo = new GameObject("SafeAreaContent");
            safeGo.transform.SetParent(canvasGo.transform, false);
            _safe = safeGo.AddComponent<RectTransform>();
            ApplySafeArea(_safe);

            _hud = new GameHud(_safe, "Tinta o Palabra", MarginU, this);
            _dots = new ProgressDots(_safe, this, UnitsPerDp, StroopContract.TotalTrials);
            BuildRibbon();
            BuildTimer();
            BuildStage();
            _pill = new PhasePill(_safe, this, UnitsPerDp, 22);
            BuildButtons();
            BuildRuleCard();

            var fxGo = new GameObject("FxLayer");
            fxGo.transform.SetParent(_safe, false);
            _fxRect = fxGo.AddComponent<RectTransform>();
            Stretch(_fxRect);

            BuildResultPanel();
            _exit = new ExitButton(_safe, this, UnitsPerDp);

            _toast = new Toast(_safe, this, UnitsPerDp);
            _toast.SetTopOffset(0f);
            BuildTutorial(_safe, GameHud.Height + 10f, "Tinta o Palabra", "La palabra llega por una orilla: de la TINTA, toca su color; de la PALABRA, lo que dice.",
                badgeAtBottom: true);

            var flashGo = new GameObject("Flash");
            flashGo.transform.SetParent(canvasGo.transform, false);
            Stretch(flashGo.AddComponent<RectTransform>());
            _flash = flashGo.AddComponent<Image>();
            _flash.raycastTarget = false;
            _flash.color = new Color(0f, 0f, 0f, 0f);

            _countdown = new CountdownScreen(canvasGo.transform, UnitsPerDp);
        }

        /// <summary>La cinta fija «Responde: TINTA / PALABRA» con su ícono (gota / libro): la regla se lee sin mirar la orilla.</summary>
        private void BuildRibbon()
        {
            var go = new GameObject("RuleRibbon");
            go.transform.SetParent(_safe, false);
            _ribbonRect = go.AddComponent<RectTransform>();
            _ribbonRect.anchorMin = _ribbonRect.anchorMax = new Vector2(0.5f, 1f);
            _ribbonRect.pivot = new Vector2(0.5f, 0.5f);
            _ribbonBg = go.AddComponent<Image>();
            _ribbonBg.sprite = RoundedRectSprite.Get(64);
            _ribbonBg.type = Image.Type.Sliced;
            _ribbonBg.raycastTarget = false;
            NeuroStyle.ClayFrame(_ribbonBg, 4f, 8f);

            var iconGo = new GameObject("Icon");
            iconGo.transform.SetParent(go.transform, false);
            var ir = iconGo.AddComponent<RectTransform>();
            ir.anchorMin = ir.anchorMax = new Vector2(0f, 0.5f);
            ir.pivot = new Vector2(0.5f, 0.5f);
            ir.sizeDelta = new Vector2(84f, 84f);
            ir.anchoredPosition = new Vector2(66f, 0f);
            _ribbonIcon = iconGo.AddComponent<Image>();
            _ribbonIcon.raycastTarget = false;

            _ribbonText = MakeText(go.transform, "Text", 64, TextAnchor.MiddleLeft, Color.white, 3f, 0.4f);
            var tr = _ribbonText.rectTransform;
            tr.anchorMin = Vector2.zero;
            tr.anchorMax = Vector2.one;
            tr.offsetMin = new Vector2(130f, 4f);
            tr.offsetMax = new Vector2(-30f, -4f);
            BestFit(_ribbonText, 40);
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
            _timerFill.anchorMin = new Vector2(0f, 0f);
            _timerFill.anchorMax = new Vector2(1f, 1f);
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
            var img = _timerFill.GetComponent<Image>();
            img.color = fraction > 0.5f ? Color.Lerp(AmberColor, GoodColor, (fraction - 0.5f) * 2f)
                                        : Color.Lerp(BadColor, AmberColor, fraction * 2f);
        }

        /// <summary>El escenario: las dos orillas (haces de luz a los costados) y la tarjeta de la palabra en el centro.</summary>
        private void BuildStage()
        {
            var stage = new GameObject("Stage");
            stage.transform.SetParent(_safe, false);
            _stageRect = stage.AddComponent<RectTransform>();
            _stageRect.anchorMin = _stageRect.anchorMax = new Vector2(0.5f, 1f);
            _stageRect.pivot = new Vector2(0.5f, 1f);

            BuildShore(0, StroopRule.Ink);
            BuildShore(1, StroopRule.Word);
            BuildCard(stage.transform);
        }

        /// <summary>Una orilla: un haz de luz del color de la regla pegado al borde, una columna de gotas (TINTA) o libros (PALABRA) y el nombre en vertical.</summary>
        private void BuildShore(int index, StroopRule rule)
        {
            bool ink = rule == StroopRule.Ink;
            var color = ink ? StroopContract.InkShore : StroopContract.WordShore;
            var go = new GameObject(ink ? "ShoreInk" : "ShoreWord");
            go.transform.SetParent(_stageRect, false);
            var rect = go.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(ink ? 0f : 1f, 0f);
            rect.anchorMax = new Vector2(ink ? 0f : 1f, 1f);
            rect.pivot = new Vector2(ink ? 0f : 1f, 0.5f);
            rect.sizeDelta = new Vector2(ShoreW, 0f);
            rect.anchoredPosition = Vector2.zero;
            _shoreRect[index] = rect;
            _shoreGroup[index] = go.AddComponent<CanvasGroup>();
            _shoreGroup[index].blocksRaycasts = false;

            var beamGo = new GameObject("Beam");
            beamGo.transform.SetParent(go.transform, false);
            var br = beamGo.AddComponent<RectTransform>();
            Stretch(br);
            var beam = beamGo.AddComponent<Image>();
            beam.sprite = StroopSprites.Beam();
            beam.color = new Color(color.r, color.g, color.b, 0.95f);
            beam.raycastTarget = false;
            if (!ink) br.localScale = new Vector3(-1f, 1f, 1f);   // la orilla de la derecha: el haz mira hacia adentro

            // una línea fina de luz pegada al borde: la «compuerta»
            var edgeGo = new GameObject("Edge");
            edgeGo.transform.SetParent(go.transform, false);
            var er = edgeGo.AddComponent<RectTransform>();
            er.anchorMin = new Vector2(ink ? 0f : 1f, 0f);
            er.anchorMax = new Vector2(ink ? 0f : 1f, 1f);
            er.pivot = new Vector2(ink ? 0f : 1f, 0.5f);
            er.sizeDelta = new Vector2(8f, 0f);
            er.anchoredPosition = Vector2.zero;
            var edge = edgeGo.AddComponent<Image>();
            edge.color = Color.Lerp(color, Color.white, 0.55f);
            edge.raycastTarget = false;

            // columna de marcas: gotas o libros, siempre con la forma además del color
            for (int k = 0; k < 4; k++)
            {
                var markGo = new GameObject("Mark");
                markGo.transform.SetParent(go.transform, false);
                var mr = markGo.AddComponent<RectTransform>();
                mr.anchorMin = mr.anchorMax = new Vector2(ink ? 0.36f : 0.64f, 0.12f + k * 0.25f);
                mr.pivot = new Vector2(0.5f, 0.5f);
                mr.sizeDelta = new Vector2(54f, 54f);
                var mark = markGo.AddComponent<Image>();
                mark.sprite = ink ? StroopSprites.Drop() : StroopSprites.Book();
                mark.color = new Color(1f, 1f, 1f, 0.6f);
                mark.raycastTarget = false;
            }

            var label = MakeText(go.transform, "Name", 44, TextAnchor.MiddleCenter, Color.white, 2f, 0.4f);
            var lr = label.rectTransform;
            lr.anchorMin = lr.anchorMax = new Vector2(ink ? 0.84f : 0.16f, 0.5f);
            lr.pivot = new Vector2(0.5f, 0.5f);
            lr.sizeDelta = new Vector2(420f, 60f);
            lr.localRotation = Quaternion.Euler(0f, 0f, ink ? 90f : -90f);
            label.text = ink ? "TINTA" : "PALABRA";
        }

        private void BuildCard(Transform parent)
        {
            var root = new GameObject("Card");
            root.transform.SetParent(parent, false);
            _cardRect = root.AddComponent<RectTransform>();
            _cardRect.anchorMin = _cardRect.anchorMax = new Vector2(0.5f, 1f);
            _cardRect.pivot = new Vector2(0.5f, 0.5f);
            _cardGroup = root.AddComponent<CanvasGroup>();
            _cardGroup.blocksRaycasts = false;

            var shadowGo = new GameObject("Shadow");
            shadowGo.transform.SetParent(root.transform, false);
            var sr = shadowGo.AddComponent<RectTransform>();
            sr.anchorMin = new Vector2(-0.06f, -0.3f);
            sr.anchorMax = new Vector2(1.06f, 0.96f);
            sr.offsetMin = sr.offsetMax = Vector2.zero;
            var shImg = shadowGo.AddComponent<Image>();
            shImg.sprite = RadialGlowSprite.Get();
            shImg.color = new Color(0f, 0f, 0f, 0.35f);
            shImg.raycastTarget = false;

            // borde del color de la orilla de la que llegó la tarjeta
            var borderGo = new GameObject("Border");
            borderGo.transform.SetParent(root.transform, false);
            Stretch(borderGo.AddComponent<RectTransform>());
            _cardBorder = borderGo.AddComponent<Image>();
            _cardBorder.sprite = RoundedRectSprite.Get(56);
            _cardBorder.type = Image.Type.Sliced;
            _cardBorder.raycastTarget = false;
            NeuroStyle.ClayFrame(_cardBorder, 5f, 12f);

            var fillGo = new GameObject("Fill");
            fillGo.transform.SetParent(root.transform, false);
            var fr = fillGo.AddComponent<RectTransform>();
            fr.anchorMin = Vector2.zero;
            fr.anchorMax = Vector2.one;
            fr.offsetMin = new Vector2(9f, 9f);
            fr.offsetMax = new Vector2(-9f, -9f);
            var fillImg = fillGo.AddComponent<Image>();
            fillImg.sprite = RoundedRectSprite.Get(52);
            fillImg.type = Image.Type.Sliced;
            fillImg.color = StroopContract.CardFill;          // #141B36: sobre este fondo se midieron los contrastes de las 4 tintas
            fillImg.raycastTarget = false;

            // brillo suave detrás de la palabra
            var glowGo = new GameObject("WordGlow");
            glowGo.transform.SetParent(root.transform, false);
            var gr = glowGo.AddComponent<RectTransform>();
            gr.anchorMin = new Vector2(0.02f, -0.1f);
            gr.anchorMax = new Vector2(0.98f, 1.1f);
            gr.offsetMin = gr.offsetMax = Vector2.zero;
            _wordGlow = glowGo.AddComponent<Image>();
            _wordGlow.sprite = RadialGlowSprite.Get();
            _wordGlow.raycastTarget = false;

            _wordText = MakeText(root.transform, "Word", 190, TextAnchor.MiddleCenter, Color.white, 4f, 0.5f);
            var wr = _wordText.rectTransform;
            wr.offsetMin = new Vector2(40f, 10f);
            wr.offsetMax = new Vector2(-40f, -10f);
            BestFit(_wordText, 80);
            _wordText.horizontalOverflow = HorizontalWrapMode.Wrap;

            // esquina con el ícono de su orilla: gota (TINTA, arriba a la izquierda) o libro (PALABRA, arriba a la derecha)
            var cornerGo = new GameObject("Corner");
            cornerGo.transform.SetParent(root.transform, false);
            var cr = cornerGo.AddComponent<RectTransform>();
            cr.anchorMin = cr.anchorMax = new Vector2(0f, 1f);
            cr.pivot = new Vector2(0.5f, 0.5f);
            cr.sizeDelta = new Vector2(86f, 86f);
            cr.anchoredPosition = new Vector2(58f, -58f);
            _cardCorner = cornerGo.AddComponent<Image>();
            _cardCorner.sprite = RoundedRectSprite.Get(64);
            _cardCorner.type = Image.Type.Sliced;
            _cardCorner.raycastTarget = false;
            var cornerIconGo = new GameObject("CornerIcon");
            cornerIconGo.transform.SetParent(cornerGo.transform, false);
            var cir = cornerIconGo.AddComponent<RectTransform>();
            cir.anchorMin = new Vector2(0.14f, 0.14f);
            cir.anchorMax = new Vector2(0.86f, 0.86f);
            cir.offsetMin = cir.offsetMax = Vector2.zero;
            _cardCornerIcon = cornerIconGo.AddComponent<Image>();
            _cardCornerIcon.raycastTarget = false;
        }

        /// <summary>
        /// Los 4 botones de respuesta (2 × 2): cada uno se rellena ENTERO con el color de su tinta, en arcilla (la ficha común de la app: borde tinta grueso y sombra dura hacia
        /// abajo, y se hunde al tocarla) con el nombre centrado en Fredoka seminegrita. Detrás de cada uno va el aro (oculto) que marca el correcto al fallar y en la ronda guiada.
        /// </summary>
        private void BuildButtons()
        {
            for (int i = 0; i < StroopContract.Names.Length; i++)
            {
                var ringGo = new GameObject("Ring_" + StroopContract.Names[i]);
                ringGo.transform.SetParent(_safe, false);
                var ringRect = ringGo.AddComponent<RectTransform>();
                ringRect.anchorMin = ringRect.anchorMax = new Vector2(0.5f, 0f);
                ringRect.pivot = new Vector2(0.5f, 0.5f);
                var ring = ringGo.AddComponent<Image>();
                ring.sprite = RoundedRectSprite.Get(64);
                ring.type = Image.Type.Sliced;
                ring.color = RingColor;
                ring.raycastTarget = false;
                NeuroStyle.ClayFrame(ring, 4f, 5f);
                ringGo.SetActive(false);
                _ringRects.Add(ringRect);
            }
            for (int i = 0; i < StroopContract.Names.Length; i++)
            {
                int index = i;
                var color = StroopContract.Colors[i];
                var go = new GameObject("Btn_" + StroopContract.Names[i]);
                go.transform.SetParent(_safe, false);
                var rect = go.AddComponent<RectTransform>();
                rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0f);
                rect.pivot = new Vector2(0.5f, 0.5f);
                var img = go.AddComponent<Image>();
                img.sprite = TileSprites.Get();                    // ficha de arcilla en grises: el color de la tinta la tiñe entera
                img.color = color;
                var group = go.AddComponent<CanvasGroup>();
                var button = go.AddComponent<Button>();
                button.transition = Selectable.Transition.None;
                button.onClick.AddListener(() => OnColorTapped(index));
                go.AddComponent<PressScale>();

                // el nombre, centrado sobre el color: blanco sobre ROJO, tinta oscura sobre las otras tres
                var label = MakeText(go.transform, "Label", 72, TextAnchor.MiddleCenter, i == 0 ? Color.white : InkLabel, 0f, 0f);
                label.font = UiFonts.Regular;                      // Fredoka seminegrita
                var lr = label.rectTransform;
                lr.anchorMin = new Vector2(0.1f, 0.14f);
                lr.anchorMax = new Vector2(0.9f, 0.9f);
                lr.offsetMin = lr.offsetMax = Vector2.zero;
                BestFit(label, 60);                                // 20 dp o más: «AMARILLO» cabe sin achicarse mucho
                label.text = StroopContract.Names[i];

                _buttonRects.Add(rect);
                _buttonImages.Add(img);
                _buttonGroups.Add(group);
            }
        }

        /// <summary>La tarjeta «Ahora: PALABRA» (primera vez por partida).</summary>
        private void BuildRuleCard()
        {
            var go = new GameObject("RuleCard");
            go.transform.SetParent(_safe, false);
            _ruleCardRoot = go.AddComponent<RectTransform>();
            _ruleCardRoot.anchorMin = _ruleCardRoot.anchorMax = new Vector2(0.5f, 1f);
            _ruleCardRoot.pivot = new Vector2(0.5f, 0.5f);
            _ruleCardGroup = go.AddComponent<CanvasGroup>();
            _ruleCardGroup.blocksRaycasts = false;
            var bg = go.AddComponent<Image>();
            bg.sprite = RoundedRectSprite.Get(64);
            bg.type = Image.Type.Sliced;
            bg.color = StroopContract.WordShore;
            bg.raycastTarget = false;
            NeuroStyle.ClayFrame(bg, 5f, 12f);
            var fillGo = new GameObject("Fill");
            fillGo.transform.SetParent(go.transform, false);
            var fr = fillGo.AddComponent<RectTransform>();
            Stretch(fr);
            fr.offsetMin = new Vector2(9f, 9f);
            fr.offsetMax = new Vector2(-9f, -9f);
            var fill = fillGo.AddComponent<Image>();
            fill.sprite = RoundedRectSprite.Get(56);
            fill.type = Image.Type.Sliced;
            fill.color = StroopContract.CardFill;
            fill.raycastTarget = false;

            var iconGo = new GameObject("Icon");
            iconGo.transform.SetParent(go.transform, false);
            var ir = iconGo.AddComponent<RectTransform>();
            ir.anchorMin = ir.anchorMax = new Vector2(0.5f, 0.78f);
            ir.pivot = new Vector2(0.5f, 0.5f);
            ir.sizeDelta = new Vector2(120f, 120f);
            var icon = iconGo.AddComponent<Image>();
            icon.sprite = StroopSprites.Book();
            icon.color = Color.Lerp(StroopContract.WordShore, Color.white, 0.55f);
            icon.raycastTarget = false;

            var title = MakeText(go.transform, "Title", 96, TextAnchor.MiddleCenter, Color.white, 3f, 0.4f);
            var tr = title.rectTransform;
            tr.anchorMin = new Vector2(0.04f, 0.38f);
            tr.anchorMax = new Vector2(0.96f, 0.64f);
            tr.offsetMin = tr.offsetMax = Vector2.zero;
            BestFit(title, 50);
            title.text = "Ahora: PALABRA";

            var line = MakeText(go.transform, "Line", 48, TextAnchor.MiddleCenter, new Color(1f, 1f, 1f, 0.9f), 2f, 0.3f);
            var lr = line.rectTransform;
            lr.anchorMin = new Vector2(0.06f, 0.06f);
            lr.anchorMax = new Vector2(0.94f, 0.38f);
            lr.offsetMin = lr.offsetMax = Vector2.zero;
            BestFit(line, 28);
            line.text = "Toca lo que DICE la palabra, no su color";
            go.SetActive(false);
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
            var outline = go.AddComponent<Outline>();
            outline.effectColor = new Color(1f, 1f, 1f, 0.14f);
            outline.effectDistance = new Vector2(3f, -3f);

            AddResultText("Title", 84, new Vector2(0f, 250f), Color.white);
            AddResultText("Score", 260, new Vector2(0f, 60f), Color.white);
            AddResultText("Detail", 56, new Vector2(0f, -150f), new Color(1f, 1f, 1f, 0.85f));
            AddResultText("Extra", 44, new Vector2(0f, -250f), new Color(1f, 1f, 1f, 0.65f));
            go.SetActive(false);
        }

        private void ShowResult(int score, int avgMs, int total)
        {
            _exit.Show();
            _acceptInput = false;
            foreach (var r in _buttonRects) r.gameObject.SetActive(false);
            _stageRect.gameObject.SetActive(false);
            _ribbonRect.gameObject.SetActive(false);
            _timerBg.gameObject.SetActive(false);
            _dots.Rect.gameObject.SetActive(false);
            _pill.Rect.gameObject.SetActive(false);

            string title = score >= 90 ? "¡Extraordinario!" : score >= 70 ? "¡Muy bien!" : score >= 50 ? "Buen trabajo" : "Sigue practicando";
            _resultRoot.Find("Title").GetComponent<Text>().text = title;
            _resultRoot.Find("Detail").GetComponent<Text>().text = Endless ? $"{_correct} aciertos de {total} · {_points} puntos" : $"{_correct} de {total} aciertos";
            _resultRoot.Find("Extra").GetComponent<Text>().text = _responseCount > 0
                ? $"Respuesta media {avgMs / 1000f:0.0} s · mejor racha {_bestStreak}"
                : $"Mejor racha {_bestStreak}";
            _resultRoot.gameObject.SetActive(true);
            StartCoroutine(AnimateResult(score));
            if (Motion.Decorative) StartCoroutine(UiFx.SparkBurst(_fxRect, Vector2.zero, StroopContract.WordShore, 24, 420f, 56f, 0.9f));
        }

        // ------------------------------------------------------------------ layout

        private void Layout()
        {
            Rect safe = _safe.rect;
            float sw = Mathf.Max(safe.width, 400f);
            float sh = Mathf.Max(safe.height, 800f);
            float contentW = sw - MarginU * 2f;

            float y = 190f; // debajo del HUD
            _dots.Rect.anchoredPosition = new Vector2(0f, -y);
            y += 60f;

            float ribbonH = 104f;
            _ribbonRect.sizeDelta = new Vector2(contentW * 0.82f, ribbonH);
            _ribbonRect.anchoredPosition = new Vector2(0f, -(y + ribbonH / 2f));
            y += ribbonH + 14f;

            _timerBg.sizeDelta = new Vector2(contentW, 16f);
            _timerBg.anchoredPosition = new Vector2(0f, -(y + 8f));
            y += 16f + 22f;

            // El espacio que queda se reparte entre el escenario (las dos orillas y la tarjeta), el hueco de la píldora y los botones 2 × 2, que quedan cerca del pulgar.
            // Durante la ronda guiada los botones suben (_bottomReserve) para dejar lugar a «Práctica: no cuenta» y «Saltar tutorial».
            float gap = 22f;
            float avail = sh - y - _bottomReserve;
            float cellW = (contentW - gap) / 2f;
            float cellH = Mathf.Clamp((avail * 0.36f - gap) / 2f, 130f, 300f);
            float block = cellH * 2f + gap;
            const float pillZone = 120f, bottomMin = 50f;
            float stageH = Mathf.Clamp(avail * 0.40f, 330f, 640f);
            if (stageH + pillZone + block + bottomMin > avail) stageH = Mathf.Max(300f, avail - pillZone - block - bottomMin);
            float extra = Mathf.Max(0f, avail - (stageH + pillZone + block + bottomMin));
            float topPad = extra * 0.35f;
            float pillGap = pillZone + extra * 0.45f;
            float bottom = bottomMin + extra * 0.20f + _bottomReserve;
            y += topPad;

            float cardH = stageH - 50f;
            _stageRect.sizeDelta = new Vector2(sw, stageH);
            _stageRect.anchoredPosition = new Vector2(0f, -y);
            _cardW = sw - 2f * (ShoreW + 24f);
            _cardRect.sizeDelta = new Vector2(_cardW, cardH);
            _cardRestPos = new Vector2(0f, -stageH / 2f);
            _cardRect.anchoredPosition = _cardRestPos;
            y += stageH;
            _ruleCardRoot.sizeDelta = new Vector2(contentW * 0.92f, Mathf.Min(cardH + 60f, 440f));
            _ruleCardRoot.anchoredPosition = new Vector2(0f, -(y - stageH / 2f));

            float rowBottomY = bottom + cellH / 2f;
            float rowTopY = rowBottomY + cellH + gap;
            for (int i = 0; i < _buttonRects.Count; i++)
            {
                var r = _buttonRects[i];
                r.sizeDelta = new Vector2(cellW, cellH);
                float x = (i % 2 == 0 ? -1f : 1f) * (cellW + gap) / 2f;
                r.anchoredPosition = new Vector2(x, i < 2 ? rowTopY : rowBottomY);
                // el aro rodea la ficha de arcilla visible (que ocupa ~86 % de su espacio)
                var ring = _ringRects[i];
                ring.sizeDelta = new Vector2(cellW * 0.86f + 2f * RingPad, cellH * 0.86f + 2f * RingPad);
                ring.anchoredPosition = r.anchoredPosition + new Vector2(0f, 4f);
            }

            // píldora de estado (y explicación del error) en el hueco entre la tarjeta y los botones; el y se mide desde el centro del Safe Area
            _pill.SetPosition(new Vector2(0f, sh / 2f - (y + pillGap / 2f)));
            _toast.SetTopOffset(0f); // avisos arriba (zona del título), nunca sobre la tarjeta
            _lastBlock = block;
            _lastBottom = bottom;
        }

        private float _lastBlock, _lastBottom;

        // ------------------------------------------------------------------ helpers de UI

        /// <summary>Enciende la orilla de la que llega la palabra (la otra baja al 35 %), pone la cinta, el borde y el ícono de la esquina de la tarjeta. <paramref name="pop"/> = un
        /// pequeño golpe en la cinta al cambiar de regla (sin animaciones, no hay).</summary>
        private void ApplyRule(StroopRule rule, bool pop)
        {
            bool ink = rule == StroopRule.Ink;
            var shore = ink ? StroopContract.InkShore : StroopContract.WordShore;
            _shoreGroup[0].alpha = ink ? 1f : 0.35f;
            _shoreGroup[1].alpha = ink ? 0.35f : 1f;
            _ribbonBg.color = Color.Lerp(StroopContract.CardFill, shore, 0.55f);
            _ribbonIcon.sprite = ink ? StroopSprites.Drop() : StroopSprites.Book();
            _ribbonIcon.color = Color.Lerp(shore, Color.white, 0.7f);
            _ribbonText.text = StroopContract.Ribbon(rule);
            _cardBorder.color = new Color(shore.r, shore.g, shore.b, 0.98f);
            _cardCorner.color = shore;
            _cardCornerIcon.sprite = ink ? StroopSprites.Drop() : StroopSprites.Book();
            _cardCornerIcon.color = Color.white;
            var cr = _cardCorner.rectTransform;
            cr.anchorMin = cr.anchorMax = new Vector2(ink ? 0f : 1f, 1f);
            cr.anchoredPosition = new Vector2(ink ? 58f : -58f, -58f);
            if (pop && Motion.Decorative) StartCoroutine(PopRect(_ribbonRect, 1.06f, 0.2f));
            if (pop) PlayTone(ink ? 330f : 440f, 0.1f, 0.1f);
        }

        private void UpdateHudText()
        {
            // Marcador común (GameHud): nivel + puntos que cuentan (Reto) o avance "3 de 16" (Precisión).
            if (Endless)
            {
                _hud.SetLevel(_effLevel);
                _hud.SetPoints(_points);
            }
            else
            {
                _hud.SetLevel(_config.config.level);
                _hud.SetInfo($"{Mathf.Min(_trialIndex + 1, StroopContract.TotalTrials)} de {StroopContract.TotalTrials}");
            }
        }

        private void SetStreak(int streak)
        {
            _hud.SetStreak(streak);
        }

        private void ResetButtons()
        {
            for (int i = 0; i < _buttonGroups.Count; i++) _buttonGroups[i].alpha = 1f;
        }

        /// <summary>El aro (por fuera del botón) sobre el correcto; con <paramref name="dimOthers"/> los demás se apagan (al fallar), y sin eso solo se marca (ronda guiada).</summary>
        private void ShowRing(int keep, bool dimOthers = true)
        {
            for (int i = 0; i < _ringRects.Count; i++)
            {
                _ringRects[i].gameObject.SetActive(i == keep);
                _buttonGroups[i].alpha = i == keep || !dimOthers ? 1f : 0.4f;
            }
            if (dimOthers && Motion.Decorative) StartCoroutine(PopRect(_buttonRects[keep], 1.08f, 0.3f));
        }

        private void ClearRings()
        {
            for (int i = 0; i < _ringRects.Count; i++) _ringRects[i].gameObject.SetActive(false);
            ResetButtons();
        }

        // ------------------------------------------------------------------ «Cómo se juega» desde la pausa

        protected override bool HowToReady => _loopOn && !_ended;

        protected override void HowToSuspend()
        {
            _acceptInput = false;
            _answerIndex = (int)NoAnswer;
            _cardGroup.alpha = 0f;
            _toast.Hide();
            ClearTransient();
        }

        protected override void HowToResume(float spentSeconds)
        {
            _roundEndsAt = HowToClock.Shift(_roundEndsAt, spentSeconds);   // el tiempo que duró «Cómo se juega» no se le descuenta al Reto
            ClearTransient();
            StartCoroutine(MainLoop());                                    // la palabra que estaba en curso no contó: sigue otra con el mismo estado
        }

        /// <summary>Quita lo que quedó a medias (efectos, aros, la tarjeta «Ahora: PALABRA») sin tocar ninguna cuenta.</summary>
        private void ClearTransient()
        {
            foreach (Transform c in _fxRect) Destroy(c.gameObject);
            _ruleCardRoot.gameObject.SetActive(false);
            _cardGroup.alpha = 0f;
            ClearRings();
        }

        // ------------------------------------------------------------------ ronda guiada del tutorial (pieza común)

        private bool _guided;

        // <guided>
        protected override IEnumerator GuidedRound(GuidedTutorial t)
        {
            t.BeginPractice();
            _guided = true;
            ClearTransient();
            _acceptInput = false;
            _answerIndex = (int)NoAnswer;
            _bottomReserve = GuidedReserveU;
            _timerBg.gameObject.SetActive(false);
            _dots.Rect.gameObject.SetActive(false);
            Layout();
            t.PlaceControls(GameHud.Height + 10f, false, _lastBottom + _lastBlock + 24f, badgeAtBottom: true);
            _pill.Rect.gameObject.SetActive(false);                      // durante la práctica habla Nubi (abajo de la tarjeta), no la píldora
            ApplyRule(StroopRule.Ink, false);
            yield return Motion.Hold(0.4f);

            var script = new GuidedScript(StroopContract.GuidedTrials.Length);
            while (!script.Finished)
            {
                if (t.Skipped) { script.Skip(); break; }
                var trial = StroopContract.GuidedTrials[script.Index];
                bool hinted = script.Index < StroopContract.GuidedHinted;
                string instruction = script.Index == 0 ? "Llega por la orilla de la TINTA: toca el COLOR con que está escrita"
                    : script.Index == 1 ? "Ahora llega por la orilla de la PALABRA: toca lo que DICE"
                    : "Mira de qué orilla llega y elige tú";
                // la persona lee y empieza ella (nada avanza por tiempo); la explicación queda puesta mientras llega la tarjeta y se responde
                yield return StartCoroutine(t.WaitForContinue(instruction, "Toca para empezar"));
                if (t.Skipped) { script.Skip(); break; }
                t.Say(instruction);
                _trial = trial;
                _shownCardSide = trial.Rule == StroopRule.Ink ? -1 : 1;
                ClearRings();
                yield return StartCoroutine(SlideIn(trial, StroopContract.GuidedArrivalSeconds, true));
                int correctIndex = trial.CorrectIndex;
                if (hinted) ShowRing(correctIndex, dimOthers: false);   // el mismo aro celeste por fuera del botón que se toca: se ve sobre las cuatro tintas
                _acceptInput = true;
                _answerIndex = (int)NoAnswer;
                while (_answerIndex == (int)NoAnswer && !t.Skipped) yield return null;
                _acceptInput = false;
                ClearRings();
                if (t.Skipped) { script.Skip(); break; }

                if (_answerIndex == correctIndex)
                {
                    GameFeel.Correct(1);
                    script.Success();
                    yield return StartCoroutine(t.WaitForContinue("¡Bien! " + StroopContract.Explain(trial)));
                    yield return StartCoroutine(SlideOut(0.28f, -_shownCardSide));
                }
                else
                {
                    GameFeel.Wrong();
                    ShowRing(correctIndex);
                    script.Failure();
                    yield return StartCoroutine(t.WaitForContinue(trial.Rule == StroopRule.Ink
                        ? "Casi: desde esta orilla se toca el COLOR de la tinta. Mira otra vez"
                        : "Casi: desde esta orilla se toca lo que DICE la palabra. Mira otra vez"));
                    yield return StartCoroutine(FadeCardOut());
                    ClearRings();
                }
                yield return Motion.Hold(0.25f);
            }
            ClearRings();
            _cardGroup.alpha = 0f;
            if (!script.Skipped)
            {
                PlayTone(523f, 0.4f, 0.08f);
                yield return StartCoroutine(t.WaitForContinue("¡Así se juega! Ahora sin ayuda", "Toca para empezar"));
            }
            _guided = false;
            _bottomReserve = 0f;
            _pill.Rect.gameObject.SetActive(true);
            _timerBg.gameObject.SetActive(Endless);
            _dots.Rect.gameObject.SetActive(!Endless);
            Layout();
            ApplyRule(StroopRule.Ink, false);
            t.EndPractice();
        }
        // </guided>
    }
}
