using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using NeuroVida.Bridge;
using NeuroVida.Contracts;
using NeuroVida.Games.Secuencia; // RoundedRectSprite / RadialGlowSprite
using NeuroVida.Games.Shared;
using static NeuroVida.Games.Shared.UiKit;

namespace NeuroVida.Games.Acoplamiento
{
    /// <summary>
    /// "Acoplamiento": juego estrella de rotación mental (ver <see cref="DockingContract"/>). Abajo, el puerto de la
    /// estación con el hueco de una pieza; arriba llega un módulo girado. ¿ENCAJA (es la misma pieza, girada) o es su
    /// ESPEJO (su reflejo: no entra por más que se gire)?
    /// <list type="bullet">
    /// <item>Siempre se ve la respuesta: el módulo gira hasta quedar derecho y baja al puerto. Si era el reflejo, se da
    /// vuelta como un espejo (y recién ahí entra): se ve por qué.</item>
    /// <item>Cada módulo acoplado se suma a tu estación (arriba): crece con la partida.</item>
    /// <item>Medida propia: "tu giro mental" (grados por segundo) y "tu curva de giro" (cuánto más tardas cuanto más
    /// girado viene), la huella clásica de Shepard y Metzler.</item>
    /// </list>
    /// Reto = 2 minutos, con combustible limitado por módulo (6 → 2,5 s); Precisión = 24 módulos sin apuro.
    /// </summary>
    public class DockingGameController : GameControllerBase
    {
        public const string GameId = DockingContract.GameId;

        private const float UnitsPerDp = 3f;
        private const float MarginU = 60f;
        private const int PxPerCell = 64;
        private const int StationIcons = 14;
        private const float ShadowDrop = 10f;

        private static readonly Color GoodColor = NeuroStyle.Lime;
        private static readonly Color BadColor = NeuroStyle.Coral;
        private static readonly Color AmberColor = NeuroStyle.Sun;

        private enum Phase { Idle, Arrive, Decide, Resolve, Done }

        private System.Random _rng;
        private AdaptiveDifficulty _dda;
        private Phase _phase = Phase.Idle;
        private bool Endless => _config != null && _config.config.timed;
        private bool Precision => !Endless;
        private int _previousFrameRate;

        // módulo en curso
        private DockingTrial _trial;
        private int _answer = -1; // 0 = encaja, 1 = espejo
        private float _answerAt;
        private float _cell;

        // sesión
        private readonly List<int> _disparities = new List<int>();
        private readonly List<float> _rts = new List<float>();
        private readonly List<bool> _corrects = new List<bool>();
        private int _trials, _correct, _docked, _streak, _bestStreak, _points;
        private float _endsAt;
        private int _lastTickSecond = -1;

        // UI
        private RectTransform _safe, _play, _fxRect, _holder, _bodyRoot, _shadowRoot, _portRect, _socketRoot, _fuelBg, _fuelFill,
            _stationRow, _timerBg, _timerFill;
        private Image _body, _shadow, _socket;
        private readonly List<Image> _stationIcons = new List<Image>();
        private readonly RectTransform[] _buttons = new RectTransform[2];
        private Image _portGlow;
        private Text _prompt, _stationLabel;
        private Toast _toast;
        private ExitButton _exit;
        private GameHud _hud;
        private CountdownScreen _countdown;
        private float _playW, _playH, _arriveY, _portY;

        // ------------------------------------------------------------------ sesión

        public void StartSession(SequenceInitConfig config)
        {
            _config = config;
            _rng = new System.Random();
            var age = DdaUserProfileConfig.ParseAgeBand(config.config.age_band);
            float start = AdaptiveDifficulty.StartRating(config.config, DockingContract.MaxLevel);
            // ~25 módulos por partida. Sin modulación por tiempo: el tiempo ES la medida (no debe mover la dificultad).
            _dda = new AdaptiveDifficulty(DockingContract.MaxLevel, age, start, stepUp: 0.3f, useReaction: false);

            _previousFrameRate = Application.targetFrameRate;
            Application.targetFrameRate = 60;

            _phase = Phase.Idle;
            _disparities.Clear();
            _rts.Clear();
            _corrects.Clear();
            _trials = _correct = _docked = _streak = _bestStreak = _points = 0;
            _endsAt = 0f;
            _lastTickSecond = -1;

            _resultRoot.gameObject.SetActive(false);
            _exit.Hide();
            _timerBg.gameObject.SetActive(Endless);
            _hud.SetStreak(0);
            _holder.gameObject.SetActive(false);
            foreach (var s in _stationIcons) s.gameObject.SetActive(false);
            _stationLabel.text = "Tu estación";
            SetButtonsActive(false);

            StopAllCoroutines();
            StartCoroutine(GameLoop());
        }

        private void OnDisable()
        {
            if (_previousFrameRate != 0) Application.targetFrameRate = _previousFrameRate;
        }

        /// <summary>Las piezas se hornean en cada intento: al cerrar el juego se liberan sus texturas.</summary>
        private void OnDestroy()
        {
            foreach (var img in new[] { _body, _shadow, _socket })
                if (img != null) Replace(img, null);
        }

        private IEnumerator GameLoop()
        {
            _safe.gameObject.SetActive(false);
            yield return StartCoroutine(_countdown.Play("Acoplamiento", Assessment.Subtitle("Prepárate"), () => _safe.gameObject.SetActive(true)));
            _safe.gameObject.SetActive(true);
            yield return null;
            ApplySafeArea(_safe);
            Canvas.ForceUpdateCanvases();
            Layout();
            UpdateHud();

            _endsAt = GameClock.Time + DockingContract.RetoSeconds;
            while (!Finished())
                yield return StartCoroutine(RunTrial());
            yield return StartCoroutine(FinishGame());
        }

        private bool Finished() => Precision ? _trials >= DockingContract.PrecisionTrials : GameClock.Time >= _endsAt;

        // ------------------------------------------------------------------ un módulo

        private IEnumerator RunTrial()
        {
            int level = _dda.PresentedLevel;
            _trial = DockingContract.NextTrial(level, _rng);
            _answer = -1;
            BuildPiece(_trial);
            SetPrompt(_trials < 2 ? "¿Encaja en el puerto o es su espejo?" : "", Color.white);

            // 1. Llega el módulo (desde arriba, ya girado).
            _phase = Phase.Arrive;
            _holder.gameObject.SetActive(true);
            _holder.localScale = Vector3.one;
            float t = 0f;
            const float arrive = 0.3f;
            var from = new Vector2(0f, _arriveY + 500f);
            var to = new Vector2(0f, _arriveY);
            while (t < arrive)
            {
                t += GameClock.DeltaTime;
                float k = UiFx.EaseOutCubic(Mathf.Clamp01(t / arrive));
                _holder.anchoredPosition = Vector2.LerpUnclamped(from, to, k);
                yield return null;
            }
            _holder.anchoredPosition = to;
            PlayTone(523f, 0.05f, 0.05f);

            // 2. Decidir (con combustible limitado en Reto).
            _phase = Phase.Decide;
            SetButtonsActive(true);
            float shownAt = GameClock.Time;
            float deadline = DockingContract.DeadlineMs(level, Precision) / 1000f;
            _fuelBg.gameObject.SetActive(Endless);
            while (_answer < 0 && GameClock.Time - shownAt < deadline)
            {
                float left = 1f - (GameClock.Time - shownAt) / deadline;
                SetFuel(left);
                // Flota apenas (solo sube y baja: girar cambiaría el ángulo que hay que evaluar).
                _holder.anchoredPosition = to + new Vector2(0f, 6f * Mathf.Sin((GameClock.Time - shownAt) * 3f));
                yield return null;
            }
            _fuelBg.gameObject.SetActive(false);
            SetButtonsActive(false);
            _phase = Phase.Resolve;
            bool timedOut = _answer < 0;
            float rtMs = timedOut ? deadline * 1000f : (_answerAt - shownAt) * 1000f;
            bool saidFits = _answer == 0;
            bool correct = !timedOut && saidFits == !_trial.Mirrored;

            _trials++;
            _disparities.Add(_trial.Disparity);
            _rts.Add(rtMs);
            _corrects.Add(correct);
            if (correct) _correct++;
            _streak = correct ? _streak + 1 : 0;
            _bestStreak = Mathf.Max(_bestStreak, _streak);
            int pts = DockingContract.Points(correct, rtMs, (int)(deadline * 1000f), level, _streak);
            _points += pts;
            _hud.SetStreak(_streak);
            var change = _dda.Register(correct);

            // 3. Mostrar la verdad: girar hasta quedar derecho, darse vuelta si era el reflejo y acoplar.
            if (timedOut)
            {
                SetPrompt("¡Sin combustible!", AmberColor);
                GameFeel.Wrong();
                yield return StartCoroutine(DriftAway());
            }
            else if (correct)
            {
                SetPrompt(_trial.Mirrored ? "¡Bien visto! Era su reflejo" : (_streak >= 3 ? $"¡Acoplado! · racha {_streak}" : "¡Acoplado!"), GoodColor);
                GameFeel.Correct(_streak);
                yield return StartCoroutine(Straighten());
                if (_trial.Mirrored) yield return StartCoroutine(Flip());
                yield return StartCoroutine(Dock(true));
                StartCoroutine(FloatText(new Vector2(0f, _portY + 160f), "+" + pts, NeuroStyle.Sun));
                AddToStation();
            }
            else if (_trial.Mirrored)
            {
                // Dijo "encaja" y era el reflejo: se intenta, no entra, y se ve el espejo.
                SetPrompt("No encaja: era su reflejo", BadColor);
                GameFeel.Wrong();
                yield return StartCoroutine(Straighten());
                yield return StartCoroutine(Bump());
                yield return StartCoroutine(Flip());
                yield return StartCoroutine(Wait(0.35f));
                yield return StartCoroutine(DriftAway());
            }
            else
            {
                // Dijo "espejo" y era la misma pieza: se ve que sí entraba.
                SetPrompt("Sí encajaba: era la misma pieza girada", AmberColor);
                GameFeel.Wrong();
                yield return StartCoroutine(Straighten());
                yield return StartCoroutine(Dock(false));
            }

            if (change == DdaChange.Up)
            {
                _toast.Show("¡Subes de nivel!", LevelNews(_dda.Level), GoodColor, 1.0f);
                GameFeel.LevelUp();
            }
            else if (change == DdaChange.Down || _dda.Struggling)
                _toast.Show("Con calma", "Gira la pieza en tu mente", AmberColor, 0.9f);
            UpdateHud();
            yield return StartCoroutine(Wait(0.45f));
            _holder.gameObject.SetActive(false);
            _portGlow.color = new Color(1f, 1f, 1f, 0f);
        }

        private static string LevelNews(int level)
        {
            if (DockingContract.Cells(level) > DockingContract.Cells(level - 1)) return $"Piezas de {DockingContract.Cells(level)} bloques";
            if (DockingContract.MaxAngle(level) > DockingContract.MaxAngle(level - 1)) return $"Giros de hasta {DockingContract.MaxAngle(level)}°";
            if (DockingContract.AngleStep(level) < DockingContract.AngleStep(level - 1)) return "Giros en cualquier ángulo";
            return "Menos combustible";
        }

        // ------------------------------------------------------------------ pieza

        /// <summary>
        /// Arma el módulo (girado y, si toca, reflejado) y el hueco del puerto (la pieza derecha). Cada pieza se hornea
        /// entera como una sola arcilla (un contorno, juntas tenues entre bloques); las texturas del intento anterior se
        /// liberan.
        /// </summary>
        private void BuildPiece(DockingTrial t)
        {
            int w = 0, h = 0;
            foreach (var c in t.Shape) { w = Mathf.Max(w, c.X + 1); h = Mathf.Max(h, c.Y + 1); }
            _cell = Mathf.Min(96f, 400f / Mathf.Max(w, h));
            float side = DockingSprites.PieceSide(t.Shape);
            int px = Mathf.CeilToInt(side * PxPerCell);

            Replace(_body, DockingSprites.PieceSprite(t.Shape, t.Mirrored, DockingSprites.PieceLayer.Body, px));
            Replace(_shadow, DockingSprites.PieceSprite(t.Shape, t.Mirrored, DockingSprites.PieceLayer.Silhouette, px));
            Replace(_socket, DockingSprites.PieceSprite(t.Shape, false, DockingSprites.PieceLayer.Socket, px));
            var size = new Vector2(side * _cell, side * _cell);
            _body.rectTransform.sizeDelta = _shadow.rectTransform.sizeDelta = _socket.rectTransform.sizeDelta = size;
            _body.gameObject.SetActive(true);
            _shadow.gameObject.SetActive(true);
            _socket.gameObject.SetActive(true);
            _body.color = Color.white;
            _shadow.color = NeuroStyle.Ink;

            var rot = Quaternion.Euler(0f, 0f, t.AngleDeg);
            _bodyRoot.localRotation = _shadowRoot.localRotation = rot;
            _bodyRoot.localScale = _shadowRoot.localScale = Vector3.one;
        }

        private static void Replace(Image img, Sprite sprite)
        {
            var old = img.sprite;
            img.sprite = sprite;
            if (old != null)
            {
                Destroy(old.texture);
                Destroy(old);
            }
        }

        private IEnumerator Straighten()
        {
            float a0 = _trial.AngleDeg;
            float t = 0f;
            float seconds = 0.2f + 0.3f * Mathf.Abs(a0) / 180f;
            while (t < seconds)
            {
                t += GameClock.DeltaTime;
                float k = UiFx.EaseOutCubic(Mathf.Clamp01(t / seconds));
                var rot = Quaternion.Euler(0f, 0f, Mathf.Lerp(a0, 0f, k));
                _bodyRoot.localRotation = _shadowRoot.localRotation = rot;
                yield return null;
            }
            _bodyRoot.localRotation = _shadowRoot.localRotation = Quaternion.identity;
        }

        /// <summary>El reflejo se da vuelta como en un espejo (escala x de 1 a −1).</summary>
        private IEnumerator Flip()
        {
            PlayTone(880f, 0.08f, 0.05f);
            float t = 0f;
            const float seconds = 0.4f;
            while (t < seconds)
            {
                t += GameClock.DeltaTime;
                float k = Mathf.Clamp01(t / seconds);
                float sx = Mathf.Cos(k * Mathf.PI); // 1 → −1 pasando por 0 (de canto)
                _bodyRoot.localScale = _shadowRoot.localScale = new Vector3(sx, 1f, 1f);
                yield return null;
            }
            _bodyRoot.localScale = _shadowRoot.localScale = new Vector3(-1f, 1f, 1f);
        }

        private IEnumerator Dock(bool celebrate)
        {
            var from = _holder.anchoredPosition;
            var to = new Vector2(0f, _portY + _socketRoot.anchoredPosition.y);
            float t = 0f;
            const float seconds = 0.35f;
            while (t < seconds)
            {
                t += GameClock.DeltaTime;
                float k = Mathf.Clamp01(t / seconds);
                _holder.anchoredPosition = Vector2.Lerp(from, to, k * k);
                yield return null;
            }
            _holder.anchoredPosition = to;
            // ¡Clunk!: el puerto se ilumina.
            PlayTone(147f, 0.16f, 0.08f);
            GameFeel.Haptic(GameFeel.HapticKind.Firm);
            StartCoroutine(UiFx.Shake(8f, 0.15f, _portRect));
            _portGlow.color = NeuroStyle.WithAlpha(celebrate ? GoodColor : AmberColor, 0.5f);
            if (celebrate)
            {
                _docked++;
                StartCoroutine(UiFx.SparkBurst(_fxRect, new Vector2(0f, _portY), NeuroStyle.Sun, 16, 260f, 36f, 0.55f));
                StartCoroutine(UiFx.RingBurst(_fxRect, new Vector2(0f, _portY), GoodColor, 200f, 640f, 0.45f));
            }
        }

        /// <summary>Intento de entrar que no calza: baja, choca y rebota.</summary>
        private IEnumerator Bump()
        {
            var from = _holder.anchoredPosition;
            var hit = new Vector2(0f, _portY + _cell * 1.2f);
            float t = 0f;
            const float seconds = 0.25f;
            while (t < seconds)
            {
                t += GameClock.DeltaTime;
                _holder.anchoredPosition = Vector2.Lerp(from, hit, Mathf.Clamp01(t / seconds));
                yield return null;
            }
            PlayTone(110f, 0.12f, 0.07f);
            GameFeel.Haptic(GameFeel.HapticKind.Double);
            _portGlow.color = NeuroStyle.WithAlpha(BadColor, 0.45f);
            yield return StartCoroutine(UiFx.Shake(18f, 0.25f, _holder));
            t = 0f;
            while (t < 0.2f)
            {
                t += GameClock.DeltaTime;
                _holder.anchoredPosition = Vector2.Lerp(hit, from, Mathf.Clamp01(t / 0.2f));
                yield return null;
            }
        }

        private IEnumerator DriftAway()
        {
            var from = _holder.anchoredPosition;
            float t = 0f;
            const float seconds = 0.5f;
            while (t < seconds)
            {
                t += GameClock.DeltaTime;
                float k = Mathf.Clamp01(t / seconds);
                _holder.anchoredPosition = from + new Vector2(_playW * 0.8f * k * k, 120f * k);
                _body.color = new Color(1f, 1f, 1f, 1f - k);
                _shadow.color = NeuroStyle.WithAlpha(NeuroStyle.Ink, 1f - k);
                yield return null;
            }
        }

        private void AddToStation()
        {
            int i = Mathf.Min(_docked, StationIcons) - 1;
            _stationLabel.text = _docked == 1 ? "Tu estación · 1 módulo" : $"Tu estación · {_docked} módulos";
            if (i < 0 || i >= _stationIcons.Count) return;
            var icon = _stationIcons[i];
            icon.gameObject.SetActive(true);
            StartCoroutine(PopIn(icon.rectTransform, 0.25f));
            if (_docked % 10 == 0)
            {
                _toast.Show($"¡{_docked} módulos!", "Tu estación crece", NeuroStyle.Sun, 1.0f);
                GameFeel.LevelUp();
            }
        }

        // ------------------------------------------------------------------ entrada

        private void Update()
        {
            UpdateClock();
            if (_phase != Phase.Decide || _answer >= 0 || GameClock.DeltaTime <= 0f) return;
            if (!Input.GetMouseButtonDown(0)) return;
            // Al presionar (no al soltar): el tiempo de respuesta es la medida.
            for (int i = 0; i < 2; i++)
            {
                if (!RectTransformUtility.RectangleContainsScreenPoint(_buttons[i], Input.mousePosition, null)) continue;
                _answer = i;
                _answerAt = GameClock.Time;
                GameFeel.Haptic(GameFeel.HapticKind.Light);
                StartCoroutine(PopRect(_buttons[i], 0.92f, 0.15f));
                return;
            }
        }

        private void UpdateClock()
        {
            if (!Endless || _phase == Phase.Idle || _phase == Phase.Done || _endsAt <= 0f) return;
            float left = _endsAt - GameClock.Time;
            SetTimerFraction(Mathf.Clamp01(left / DockingContract.RetoSeconds));
            int whole = Mathf.CeilToInt(left);
            if (whole <= 5 && whole >= 1 && whole != _lastTickSecond)
            {
                _lastTickSecond = whole;
                GameFeel.Tick();
            }
        }

        private void SetButtonsActive(bool on)
        {
            foreach (var b in _buttons)
            {
                if (b == null) continue;
                b.GetComponent<CanvasGroup>().alpha = on ? 1f : 0.45f;
            }
        }

        private void SetFuel(float fraction)
        {
            _fuelFill.anchorMax = new Vector2(Mathf.Clamp01(fraction), 1f);
            _fuelFill.offsetMin = _fuelFill.offsetMax = Vector2.zero;
            _fuelFill.GetComponent<Image>().color = fraction > 0.35f ? NeuroStyle.Sky : AmberColor;
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
                r.anchoredPosition = pos + new Vector2(0f, 70f * UiFx.EaseOutCubic(k));
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
            _holder.gameObject.SetActive(false);

            float accuracy = _trials > 0 ? (float)_correct / _trials : 0f;
            int speed = DockingContract.RotationSpeed(_disparities, _rts, _corrects);
            int[] curve = DockingContract.Curve(_disparities, _rts, _corrects);
            int score = DockingContract.Score(accuracy, _dda.PeakLevel);
            float rtSum = 0f;
            int rtN = 0;
            for (int i = 0; i < _rts.Count; i++) if (_corrects[i]) { rtSum += _rts[i]; rtN++; }

            SetPrompt("Fin de los acoplamientos", GoodColor);
            ShowResult(score, speed);

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
                    peak_level = _dda.PeakLevel,
                    rotation_speed_dps = speed,
                    rotation_curve_ms = curve
                }
            };
            NativeBridge.ForwardTelemetryToPlatform(JsonUtility.ToJson(telemetry));
            yield break;
        }

        private void ShowResult(int score, int speed)
        {
            _exit.Show();
            _resultRoot.Find("Title").GetComponent<Text>().text = score >= 85 ? "¡Ingeniería espacial!" : score >= 65 ? "¡Buena estación!" : "Acoplamientos completados";
            _resultRoot.Find("Detail").GetComponent<Text>().text = $"{_correct} de {_trials} bien · {_docked} módulos";
            _resultRoot.Find("Extra").GetComponent<Text>().text = speed > 0 ? $"Tu giro mental: {speed}° por segundo" : $"Mejor racha {_bestStreak}";
            _resultRoot.gameObject.SetActive(true);
            StartCoroutine(AnimateResult(score));
        }

        // ------------------------------------------------------------------ construcción de UI

        protected override void BuildUi()
        {
            if (FindObjectOfType<UnityEngine.EventSystems.EventSystem>() == null)
            {
                var es = new GameObject("EventSystem");
                es.AddComponent<UnityEngine.EventSystems.EventSystem>();
                es.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
            }

            var canvasGo = new GameObject("DockingCanvas");
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
            WorldBackdrop.Build(bgRect, GameWorld.DockingBay);

            var safeGo = new GameObject("SafeAreaContent");
            safeGo.transform.SetParent(canvasGo.transform, false);
            _safe = safeGo.AddComponent<RectTransform>();
            ApplySafeArea(_safe);

            _hud = new GameHud(_safe, "Acoplamiento", MarginU, this);
            BuildTimer();

            var playGo = new GameObject("Play");
            playGo.transform.SetParent(_safe, false);
            _play = playGo.AddComponent<RectTransform>();
            Stretch(_play);

            BuildStation();

            _prompt = MakeText(_play, "Prompt", 58, TextAnchor.MiddleCenter, Color.white, 0f, 0f);
            NeuroStyle.ClayText(_prompt, 3.5f, 5f);
            _prompt.rectTransform.anchorMin = _prompt.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            BestFit(_prompt, 36);

            BuildPort();
            BuildModule();
            BuildFuel();
            BuildButtons();

            _fxRect = Layer(_play, "Fx");

            BuildResultPanel();
            _exit = new ExitButton(_safe, this, UnitsPerDp);
            _toast = new Toast(_safe, this, UnitsPerDp);
            _toast.SetTopOffset(0f);

            var flashGo = new GameObject("Flash");
            flashGo.transform.SetParent(canvasGo.transform, false);
            Stretch(flashGo.AddComponent<RectTransform>());
            _flash = flashGo.AddComponent<Image>();
            _flash.raycastTarget = false;
            _flash.color = new Color(0f, 0f, 0f, 0f);

            _countdown = new CountdownScreen(canvasGo.transform, UnitsPerDp);
        }

        private void BuildStation()
        {
            var go = new GameObject("Station");
            go.transform.SetParent(_play, false);
            _stationRow = go.AddComponent<RectTransform>();
            _stationRow.anchorMin = _stationRow.anchorMax = new Vector2(0.5f, 0.5f);
            _stationRow.sizeDelta = new Vector2(960f, 110f);
            _stationLabel = MakeText(_stationRow, "Label", 34, TextAnchor.MiddleLeft, new Color(1f, 1f, 1f, 0.8f), 0f, 0f);
            var lr = _stationLabel.rectTransform;
            lr.anchorMin = new Vector2(0f, 1f);
            lr.anchorMax = new Vector2(1f, 1f);
            lr.pivot = new Vector2(0.5f, 1f);
            lr.sizeDelta = new Vector2(0f, 46f);
            lr.anchoredPosition = Vector2.zero;
            // Riel de la estación y los módulos acoplados en fila.
            var rail = NewImage(_stationRow, "Rail", RoundedRectSprite.Get(8));
            rail.type = Image.Type.Sliced;
            rail.color = new Color(1f, 1f, 1f, 0.16f);
            rail.rectTransform.sizeDelta = new Vector2(960f, 10f);
            rail.rectTransform.anchoredPosition = new Vector2(0f, -28f);
            rail.gameObject.SetActive(true);
            const float icon = 52f, gap = 16f;
            for (int i = 0; i < StationIcons; i++)
            {
                var m = NewImage(_stationRow, "Module", DockingSprites.ModuleCell());
                m.rectTransform.sizeDelta = new Vector2(icon, icon);
                m.rectTransform.anchoredPosition = new Vector2(-480f + icon * 0.5f + i * (icon + gap), -28f);
                _stationIcons.Add(m);
            }
        }

        private void BuildPort()
        {
            var go = new GameObject("Port");
            go.transform.SetParent(_play, false);
            _portRect = go.AddComponent<RectTransform>();
            _portRect.anchorMin = _portRect.anchorMax = new Vector2(0.5f, 0.5f);
            _portRect.sizeDelta = new Vector2(620f, 560f);
            var panel = go.AddComponent<Image>();
            panel.sprite = RoundedRectSprite.Get(64);
            panel.type = Image.Type.Sliced;
            panel.color = NeuroStyle.Surface;
            panel.raycastTarget = false;
            NeuroStyle.ClayFrame(panel, 6f, 14f);

            _portGlow = NewImage(_portRect, "Glow", RadialGlowSprite.Get());
            _portGlow.rectTransform.sizeDelta = new Vector2(760f, 760f);
            _portGlow.color = new Color(1f, 1f, 1f, 0f);
            _portGlow.gameObject.SetActive(true);

            var label = MakeText(_portRect, "Label", 32, TextAnchor.MiddleCenter, new Color(1f, 1f, 1f, 0.6f), 0f, 0f);
            label.text = "PUERTO";
            var lr = label.rectTransform;
            lr.anchorMin = lr.anchorMax = new Vector2(0.5f, 1f);
            lr.pivot = new Vector2(0.5f, 1f);
            lr.sizeDelta = new Vector2(400f, 50f);
            lr.anchoredPosition = new Vector2(0f, -12f);

            var sr = new GameObject("Socket");
            sr.transform.SetParent(_portRect, false);
            _socketRoot = sr.AddComponent<RectTransform>();
            _socketRoot.anchorMin = _socketRoot.anchorMax = new Vector2(0.5f, 0.5f);
            _socketRoot.anchoredPosition = new Vector2(0f, -10f);
            _socket = NewImage(_socketRoot, "Hole", null);
        }

        private void BuildModule()
        {
            var go = new GameObject("Module");
            go.transform.SetParent(_play, false);
            _holder = go.AddComponent<RectTransform>();
            _holder.anchorMin = _holder.anchorMax = new Vector2(0.5f, 0.5f);

            // Sombra: la misma pieza en tinta, corrida hacia abajo EN PANTALLA (el contenedor no gira; la pieza sí).
            var sg = new GameObject("ShadowRoot");
            sg.transform.SetParent(_holder, false);
            _shadowRoot = sg.AddComponent<RectTransform>();
            _shadowRoot.anchorMin = _shadowRoot.anchorMax = new Vector2(0.5f, 0.5f);
            _shadowRoot.anchoredPosition = new Vector2(0f, -ShadowDrop);
            var bgo = new GameObject("BodyRoot");
            bgo.transform.SetParent(_holder, false);
            _bodyRoot = bgo.AddComponent<RectTransform>();
            _bodyRoot.anchorMin = _bodyRoot.anchorMax = new Vector2(0.5f, 0.5f);
            _shadow = NewImage(_shadowRoot, "Shadow", null);
            _body = NewImage(_bodyRoot, "Body", null);
            go.SetActive(false);
        }

        private void BuildFuel()
        {
            var bg = new GameObject("Fuel");
            bg.transform.SetParent(_play, false);
            _fuelBg = bg.AddComponent<RectTransform>();
            _fuelBg.anchorMin = _fuelBg.anchorMax = new Vector2(0.5f, 0.5f);
            _fuelBg.sizeDelta = new Vector2(420f, 16f);
            var bgImg = bg.AddComponent<Image>();
            bgImg.sprite = RoundedRectSprite.Get(10);
            bgImg.type = Image.Type.Sliced;
            bgImg.color = new Color(1f, 1f, 1f, 0.12f);
            bgImg.raycastTarget = false;
            var fillGo = new GameObject("Fill");
            fillGo.transform.SetParent(bg.transform, false);
            _fuelFill = fillGo.AddComponent<RectTransform>();
            _fuelFill.anchorMin = Vector2.zero;
            _fuelFill.anchorMax = Vector2.one;
            _fuelFill.offsetMin = _fuelFill.offsetMax = Vector2.zero;
            var img = fillGo.AddComponent<Image>();
            img.sprite = RoundedRectSprite.Get(10);
            img.type = Image.Type.Sliced;
            img.raycastTarget = false;
            img.color = NeuroStyle.Sky;
            bg.SetActive(false);
        }

        private void BuildButtons()
        {
            string[] labels = { "ENCAJA", "ESPEJO" };
            Color[] colors = { NeuroStyle.Lime, NeuroStyle.Grape };
            Sprite[] icons = { DockingSprites.FitIcon(), DockingSprites.MirrorIcon() };
            for (int i = 0; i < 2; i++)
            {
                var go = new GameObject("Button" + labels[i]);
                go.transform.SetParent(_play, false);
                var r = go.AddComponent<RectTransform>();
                r.anchorMin = r.anchorMax = new Vector2(0.5f, 0.5f);
                r.sizeDelta = new Vector2(440f, 190f);
                go.AddComponent<CanvasGroup>();
                var img = go.AddComponent<Image>();
                img.sprite = RoundedRectSprite.Get(64);
                img.type = Image.Type.Sliced;
                img.color = colors[i];
                img.raycastTarget = false;
                NeuroStyle.ClayFrame(img, 6f, 14f);

                var icon = NewImage(r, "Icon", icons[i]);
                icon.rectTransform.anchorMin = icon.rectTransform.anchorMax = new Vector2(0f, 0.5f);
                icon.rectTransform.sizeDelta = new Vector2(120f, 120f);
                icon.rectTransform.anchoredPosition = new Vector2(88f, 0f);
                icon.gameObject.SetActive(true);

                var t = MakeText(r, "Label", 60, TextAnchor.MiddleCenter, NeuroStyle.Ink, 0f, 0f);
                t.text = labels[i];
                var tr = t.rectTransform;
                tr.anchorMin = new Vector2(0f, 0f);
                tr.anchorMax = new Vector2(1f, 1f);
                tr.offsetMin = new Vector2(150f, 0f);
                tr.offsetMax = new Vector2(-18f, 0f);
                BestFit(t, 36);
                _buttons[i] = r;
            }
        }

        private void BuildTimer()
        {
            var bg = new GameObject("TimerBar");
            bg.transform.SetParent(_safe, false);
            _timerBg = bg.AddComponent<RectTransform>();
            _timerBg.anchorMin = _timerBg.anchorMax = new Vector2(0.5f, 1f);
            _timerBg.pivot = new Vector2(0.5f, 0.5f);
            _timerBg.sizeDelta = new Vector2(960f, 16f);
            _timerBg.anchoredPosition = new Vector2(0f, -(GameHud.Height + 14f));
            var bgImg = bg.AddComponent<Image>();
            bgImg.sprite = RoundedRectSprite.Get(10);
            bgImg.type = Image.Type.Sliced;
            bgImg.color = new Color(1f, 1f, 1f, 0.12f);
            bgImg.raycastTarget = false;

            var fillGo = new GameObject("Fill");
            fillGo.transform.SetParent(bg.transform, false);
            _timerFill = fillGo.AddComponent<RectTransform>();
            _timerFill.anchorMin = Vector2.zero;
            _timerFill.anchorMax = Vector2.one;
            _timerFill.offsetMin = _timerFill.offsetMax = Vector2.zero;
            var img = fillGo.AddComponent<Image>();
            img.sprite = RoundedRectSprite.Get(10);
            img.type = Image.Type.Sliced;
            img.raycastTarget = false;
            img.color = GoodColor;
        }

        private void SetTimerFraction(float fraction)
        {
            _timerFill.anchorMax = new Vector2(Mathf.Clamp01(fraction), 1f);
            _timerFill.offsetMin = _timerFill.offsetMax = Vector2.zero;
            _timerFill.GetComponent<Image>().color = fraction > 0.5f ? Color.Lerp(AmberColor, GoodColor, (fraction - 0.5f) * 2f)
                                                                      : Color.Lerp(BadColor, AmberColor, fraction * 2f);
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

        private static RectTransform Layer(Transform parent, string name)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var r = go.AddComponent<RectTransform>();
            Stretch(r);
            return r;
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
            Canvas.ForceUpdateCanvases();
            _playW = _play.rect.width;
            _playH = _play.rect.height;
            float top = _playH * 0.5f, bottom = -_playH * 0.5f;

            _stationRow.anchoredPosition = new Vector2(0f, top - (GameHud.Height + 90f));
            _prompt.rectTransform.sizeDelta = new Vector2(_playW - MarginU * 2f, 90f);
            _prompt.rectTransform.anchoredPosition = new Vector2(0f, top - (GameHud.Height + 200f));

            // Botones abajo; el puerto encima; el módulo llega arriba, entre el aviso y el puerto.
            float buttonsY = bottom + 60f + 95f;
            _buttons[0].anchoredPosition = new Vector2(-240f, buttonsY);
            _buttons[1].anchoredPosition = new Vector2(240f, buttonsY);
            _portY = buttonsY + 95f + 40f + 280f;
            _portRect.anchoredPosition = new Vector2(0f, _portY);
            float promptBottom = top - (GameHud.Height + 245f);
            _arriveY = (promptBottom + (_portY + 280f)) * 0.5f;
            _fuelBg.anchoredPosition = new Vector2(0f, _portY + 300f);
        }

        private void UpdateHud()
        {
            _hud.SetLevel(_dda.PresentedLevel);
            if (Endless) _hud.SetPoints(_points);
            else _hud.SetInfo($"{Mathf.Min(_trials + 1, DockingContract.PrecisionTrials)} de {DockingContract.PrecisionTrials}");
        }
    }
}
