using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using NeuroVida.Bridge;
using NeuroVida.Contracts;
using NeuroVida.Games.Secuencia; // RoundedRectSprite / RadialGlowSprite
using NeuroVida.Games.Shared;
using static NeuroVida.Games.Shared.UiKit;

namespace NeuroVida.Games.Aterrizaje
{
    /// <summary>
    /// "Aterrizaje Lunar": juego estrella de sentido numérico (ver <see cref="LandingContract"/>). Arriba, la misión
    /// ("Aterriza en 37"); abajo, la superficie de la luna es una regla con solo los extremos marcados. El módulo lunar
    /// baja solo: se arrastra con el dedo a lo ancho (un haz de luz marca dónde se posará) y al soltar cae rápido. Al
    /// posarse aparece la bandera en el lugar exacto y se ve a cuánto quedó.
    /// <list type="bullet">
    /// <item>"¡Diana lunar!" cuando queda justo en el blanco: destellos y bono.</item>
    /// <item>Medida propia: "tu estimación" (distancia media al blanco en % de la regla) y "tu línea" (cada objetivo y
    /// dónde aterrizaste), que la app dibuja y lee por tramos de la regla (dónde más se aleja del blanco).</item>
    /// </list>
    /// Reto = 2 minutos (la nave baja más rápido al subir de nivel); Precisión = 15 aterrizajes, bajada lenta.
    /// </summary>
    public class LandingGameController : GameControllerBase
    {
        public const string GameId = LandingContract.GameId;

        private const float UnitsPerDp = 3f;
        private const float MarginU = 60f;
        private const float LanderSize = 230f;
        private const int BeamDots = 24;

        private static readonly Color GoodColor = NeuroStyle.Lime;
        private static readonly Color BadColor = NeuroStyle.Coral;
        private static readonly Color AmberColor = NeuroStyle.Sun;

        private enum Phase { Idle, Flying, Reveal, Done }

        private System.Random _rng;
        private AdaptiveDifficulty _dda;
        private Phase _phase = Phase.Idle;
        private bool Endless => _config != null && _config.config.timed;
        private bool Precision => !Endless;
        private int _previousFrameRate;

        // aterrizaje en curso
        private LandingTrial _trial;
        private float _landerX, _landerTargetX, _landerY;
        private bool _touching, _released, _everTouched;

        // sesión
        private readonly List<float> _errors = new List<float>();
        private readonly List<float> _trueFractions = new List<float>();
        private readonly List<float> _givenFractions = new List<float>();
        private int _hits, _bullseyes, _streak, _bestStreak, _points;
        private float _endsAt;
        private int _lastTickSecond = -1;
        private bool _hintShown;

        // UI
        private RectTransform _safe, _play, _fxRect, _landerRect, _rulerRect, _flagRect, _gapRect, _midTick, _timerBg, _timerFill;
        private Image _lander, _flame, _gapLine;
        private Text _missionSmall, _mission, _prompt, _minLabel, _maxLabel, _flagLabel, _gapLabel, _hint;
        private readonly List<Image> _beam = new List<Image>();
        private Toast _toast;
        private ExitButton _exit;
        private GameHud _hud;
        private CountdownScreen _countdown;
        private float _playW, _playH, _rulerY, _rulerW, _skyTop;

        // ------------------------------------------------------------------ sesión

        public void StartSession(SequenceInitConfig config)
        {
            _config = config;
            _rng = new System.Random();
            var age = DdaUserProfileConfig.ParseAgeBand(config.config.age_band);
            float start = AdaptiveDifficulty.StartRating(config.config, LandingContract.MaxLevel);
            // ~18 aterrizajes por partida: pasos medianos. Sin tiempo de reacción: cuenta la precisión.
            _dda = new AdaptiveDifficulty(LandingContract.MaxLevel, age, start, stepUp: 0.3f, useReaction: false);

            // Vuelo continuo y fluido: 60 cuadros por segundo (Android da 30).
            _previousFrameRate = Application.targetFrameRate;
            Application.targetFrameRate = 60;

            _phase = Phase.Idle;
            _errors.Clear();
            _trueFractions.Clear();
            _givenFractions.Clear();
            _hits = _bullseyes = _streak = _bestStreak = _points = 0;
            _endsAt = 0f;
            _lastTickSecond = -1;
            _hintShown = false;

            _resultRoot.gameObject.SetActive(false);
            _exit.Hide();
            _timerBg.gameObject.SetActive(Endless);
            _hud.SetStreak(0);
            HideReveal();
            _landerRect.gameObject.SetActive(false);

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
            yield return StartCoroutine(_countdown.Play("Aterrizaje Lunar", Assessment.Subtitle("Prepárate"), () => _safe.gameObject.SetActive(true)));
            _safe.gameObject.SetActive(true);
            yield return null;
            ApplySafeArea(_safe);
            Canvas.ForceUpdateCanvases();
            Layout();
            UpdateHud();

            _endsAt = GameClock.Time + LandingContract.RetoSeconds;
            while (!Finished())
                yield return StartCoroutine(RunTrial());
            yield return StartCoroutine(FinishGame());
        }

        private bool Finished() => Precision ? _errors.Count >= LandingContract.PrecisionTrials : GameClock.Time >= _endsAt;

        // ------------------------------------------------------------------ un aterrizaje

        private IEnumerator RunTrial()
        {
            int level = _dda.PresentedLevel;
            _trial = LandingContract.NextTrial(level, _rng);
            HideReveal();
            SetRuler(_trial);
            _missionSmall.text = "Aterriza en";
            _mission.text = _trial.Label;
            StartCoroutine(PopRect(_mission.rectTransform, 1.15f, 0.3f));
            SetPrompt("", Color.white);

            // La nave aparece arriba, en una posición al azar (no siempre en el centro: no regala el medio).
            _landerX = _landerTargetX = Mathf.Lerp(-_rulerW * 0.4f, _rulerW * 0.4f, (float)_rng.NextDouble());
            _landerY = _skyTop;
            _touching = _released = _everTouched = false;
            _landerRect.gameObject.SetActive(true);
            _lander.color = Color.white;
            PlaceLander();
            StartCoroutine(PopIn(_landerRect, 0.25f));
            if (!_hintShown)
            {
                _hint.gameObject.SetActive(true);
                _hint.text = "Arrastra para mover la nave · suelta para aterrizar";
            }

            // Vuelo: baja sola; el dedo la mueve a lo ancho; al soltar, cae rápido.
            _phase = Phase.Flying;
            float descent = LandingContract.DescentSeconds(level, Precision);
            float slow = (_skyTop - GroundY()) / descent;
            float speed = 0f;
            while (_landerY > GroundY())
            {
                float dt = GameClock.DeltaTime;
                float target = _released ? slow * 5f : slow;
                speed = Mathf.MoveTowards(speed, target, slow * 12f * dt);
                _landerY = Mathf.Max(GroundY(), _landerY - speed * dt);
                float follow = 1f - Mathf.Exp(-dt * 16f);
                _landerX += (_landerTargetX - _landerX) * follow;
                PlaceLander();
                // Retrocohetes: la llama crece cerca del suelo (frenando) y titila.
                float near = 1f - Mathf.Clamp01((_landerY - GroundY()) / 500f);
                float f = (0.45f + 0.55f * near) * (0.85f + 0.15f * Mathf.Sin(GameClock.Time * 50f));
                _flame.rectTransform.sizeDelta = new Vector2(LanderSize * 0.5f * f, LanderSize * 0.9f * f);
                _flame.color = NeuroStyle.WithAlpha(NeuroStyle.Sun, 0.55f + 0.35f * near);
                yield return null;
            }
            _phase = Phase.Reveal;
            _hint.gameObject.SetActive(false);
            _flame.color = new Color(1f, 1f, 1f, 0f);
            foreach (var d in _beam) d.gameObject.SetActive(false);

            // Toque de suelo: polvo y un pequeño rebote.
            PlayTone(110f, 0.18f, 0.07f);
            GameFeel.Haptic(GameFeel.HapticKind.Light);
            for (int i = 0; i < 6; i++) StartCoroutine(Dust(new Vector2(_landerX, _rulerY + 10f), i));
            StartCoroutine(PopRect(_landerRect, 0.92f, 0.18f));

            yield return StartCoroutine(Reveal(level));
        }

        private IEnumerator Reveal(int level)
        {
            float given = Mathf.Clamp01(_landerX / _rulerW + 0.5f);
            float value = LandingContract.ValueAt(_trial, given);
            float err = LandingContract.Error(_trial, value);
            bool hit = LandingContract.IsHit(err), bull = LandingContract.IsBullseye(err);
            _errors.Add(err);
            _trueFractions.Add(_trial.TargetFraction);
            _givenFractions.Add(given);
            if (hit) _hits++;
            if (bull) _bullseyes++;
            _streak = hit ? _streak + 1 : 0;
            _bestStreak = Mathf.Max(_bestStreak, _streak);
            int pts = LandingContract.Points(err, level, _streak);
            _points += pts;
            _hud.SetStreak(_streak);

            // Bandera en el lugar exacto, con el número.
            float tx = (_trial.TargetFraction - 0.5f) * _rulerW;
            _flagRect.anchoredPosition = new Vector2(tx, _rulerY);
            _flagRect.gameObject.SetActive(true);
            _flagLabel.text = _trial.Label;
            yield return StartCoroutine(PlantFlag());

            // Tramo entre donde aterrizó y el blanco (si no fue diana).
            if (!bull)
            {
                float lx = _landerX;
                float left = Mathf.Min(lx, tx), right = Mathf.Max(lx, tx);
                _gapRect.gameObject.SetActive(true);
                _gapRect.anchoredPosition = new Vector2((left + right) * 0.5f, _rulerY - 40f);
                _gapRect.sizeDelta = new Vector2(Mathf.Max(8f, right - left), 12f);
                _gapLine.color = hit ? GoodColor : BadColor;
                _gapLabel.text = "a " + LandingContract.DistanceLabel(_trial, value);
                _gapLabel.color = hit ? GoodColor : NeuroStyle.Cream;
            }

            if (bull)
            {
                SetPrompt(_streak >= 3 ? $"¡DIANA LUNAR! · racha {_streak}" : "¡DIANA LUNAR!", AmberColor);
                GameFeel.Correct(_streak);
                GameFeel.LevelUp();
                StartCoroutine(UiFx.SparkBurst(_fxRect, new Vector2(tx, _rulerY + 60f), NeuroStyle.Sun, 22, 300f, 42f, 0.7f));
                StartCoroutine(UiFx.RingBurst(_fxRect, new Vector2(tx, _rulerY + 40f), NeuroStyle.Sun, 120f, 520f, 0.5f));
            }
            else if (hit)
            {
                SetPrompt(_streak >= 3 ? $"¡Buen aterrizaje! · racha {_streak}" : "¡Buen aterrizaje!", GoodColor);
                GameFeel.Correct(_streak);
                StartCoroutine(UiFx.SparkBurst(_fxRect, new Vector2(_landerX, _rulerY + 60f), GoodColor, 12, 180f, 32f, 0.5f));
            }
            else
            {
                SetPrompt(err < 0.1f ? "¡Casi!" : "Esta vez quedó lejos", AmberColor);
                GameFeel.Wrong();
            }
            if (pts > 0) StartCoroutine(FloatText(new Vector2(_landerX, _rulerY + LanderSize * 0.9f), "+" + pts, NeuroStyle.Sun));

            var change = _dda.Register(hit);
            if (change == DdaChange.Up)
            {
                _toast.Show("¡Subes de nivel!", LandingContract.LevelNews(_dda.Level), GoodColor, 1.1f);
            }
            else if (change == DdaChange.Down || _dda.Struggling)
                _toast.Show("Con calma", "Mira dónde quedaría la mitad", AmberColor, 0.9f);
            UpdateHud();

            yield return StartCoroutine(Wait(bull ? 1.1f : 1.4f));
            // La nave despega para el siguiente.
            yield return StartCoroutine(TakeOff());
        }

        // ------------------------------------------------------------------ entrada

        private void Update()
        {
            UpdateClock();
            if (_phase != Phase.Flying || GameClock.DeltaTime <= 0f) return;
            bool down = Input.touchCount > 0 || Input.GetMouseButton(0);
            if (down)
            {
                Vector2 pos = Input.touchCount > 0 ? Input.GetTouch(0).position : (Vector2)Input.mousePosition;
                if (RectTransformUtility.ScreenPointToLocalPointInRectangle(_play, pos, null, out var local))
                {
                    _landerTargetX = Mathf.Clamp(local.x, -_rulerW * 0.5f, _rulerW * 0.5f);
                    if (!_everTouched)
                    {
                        _everTouched = true;
                        _hintShown = true;
                        _hint.gameObject.SetActive(false);
                    }
                }
                _touching = true;
            }
            else if (_touching)
            {
                // Soltó: aterrizar ya.
                _touching = false;
                if (!_released)
                {
                    _released = true;
                    PlayTone(392f, 0.08f, 0.05f);
                }
            }
        }

        private void UpdateClock()
        {
            if (!Endless || _phase == Phase.Idle || _phase == Phase.Done || _endsAt <= 0f) return;
            float left = _endsAt - GameClock.Time;
            SetTimerFraction(Mathf.Clamp01(left / LandingContract.RetoSeconds));
            int whole = Mathf.CeilToInt(left);
            if (whole <= 5 && whole >= 1 && whole != _lastTickSecond)
            {
                _lastTickSecond = whole;
                GameFeel.Tick();
            }
        }

        // ------------------------------------------------------------------ dibujo

        private float GroundY() => _rulerY + LanderSize * (0.5f - LandingSprites.LanderFootFraction) + 6f;

        private void PlaceLander()
        {
            _landerRect.anchoredPosition = new Vector2(_landerX, _landerY);
            // Inclinación leve según hacia dónde se mueve.
            float tilt = Mathf.Clamp((_landerTargetX - _landerX) * -0.05f, -10f, 10f);
            _lander.rectTransform.localRotation = Quaternion.Euler(0f, 0f, tilt);
            // Haz de aterrizaje: puntos desde la nave hasta la regla, justo donde se posaría.
            float top = _landerY - LanderSize * 0.4f, bottom = _rulerY + 16f;
            for (int i = 0; i < _beam.Count; i++)
            {
                float k = (i + 0.5f) / _beam.Count;
                float y = Mathf.Lerp(top, bottom, k);
                var d = _beam[i];
                bool on = y > bottom && y < top;
                d.gameObject.SetActive(on);
                if (!on) continue;
                d.rectTransform.anchoredPosition = new Vector2(_landerX, y);
                d.color = NeuroStyle.WithAlpha(GoodColor, 0.25f + 0.5f * k);
            }
        }

        private void SetRuler(LandingTrial t)
        {
            _minLabel.text = t.MinLabel;
            _maxLabel.text = t.MaxLabel;
            _midTick.gameObject.SetActive(t.MidTick);
        }

        private IEnumerator PlantFlag()
        {
            PlayTone(659f, 0.08f, 0.06f);
            var r = _flagRect;
            float t = 0f;
            const float seconds = 0.28f;
            while (t < seconds)
            {
                t += GameClock.DeltaTime;
                float k = Mathf.Clamp01(t / seconds);
                r.localScale = new Vector3(1f, Mathf.LerpUnclamped(0f, 1f, UiFx.EaseOutBack(k)), 1f);
                yield return null;
            }
            r.localScale = Vector3.one;
        }

        private IEnumerator TakeOff()
        {
            HideReveal();
            float t = 0f;
            const float seconds = 0.45f;
            float from = _landerY;
            while (t < seconds)
            {
                t += GameClock.DeltaTime;
                float k = Mathf.Clamp01(t / seconds);
                _landerY = from + (_skyTop + 400f - from) * k * k;
                _landerRect.anchoredPosition = new Vector2(_landerX, _landerY);
                _flame.color = NeuroStyle.WithAlpha(NeuroStyle.Sun, 0.8f);
                _flame.rectTransform.sizeDelta = new Vector2(LanderSize * 0.5f, LanderSize);
                _lander.color = new Color(1f, 1f, 1f, 1f - k);
                yield return null;
            }
            _landerRect.gameObject.SetActive(false);
            _flame.color = new Color(1f, 1f, 1f, 0f);
        }

        private IEnumerator Dust(Vector2 at, int i)
        {
            var img = NewImage(_fxRect, "Dust", DiscSprite.Get());
            img.gameObject.SetActive(true);
            var r = img.rectTransform;
            float dir = i % 2 == 0 ? -1f : 1f;
            float spread = 50f + 35f * (i / 2);
            float t = 0f;
            const float seconds = 0.55f;
            while (t < seconds)
            {
                t += GameClock.DeltaTime;
                float k = Mathf.Clamp01(t / seconds);
                r.anchoredPosition = at + new Vector2(dir * spread * UiFx.EaseOutCubic(k), 30f * k);
                float s = Mathf.Lerp(30f, 90f, k);
                r.sizeDelta = new Vector2(s, s);
                img.color = new Color(0.8f, 0.78f, 0.95f, 0.5f * (1f - k));
                yield return null;
            }
            Destroy(img.gameObject);
        }

        private void HideReveal()
        {
            if (_flagRect != null) _flagRect.gameObject.SetActive(false);
            if (_gapRect != null) _gapRect.gameObject.SetActive(false);
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
            HideReveal();
            _landerRect.gameObject.SetActive(false);

            float meanErr = LandingContract.MeanErrorPct(_errors);
            int score = LandingContract.Score(meanErr, _dda.PeakLevel);

            SetPrompt("Fin de la misión", GoodColor);
            ShowResult(score, meanErr);

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
                    peak_level = _dda.PeakLevel,
                    numline_error_pct = meanErr,
                    numline_true = _trueFractions.ToArray(),
                    numline_given = _givenFractions.ToArray(),
                    numline_bullseyes = _bullseyes
                }
            };
            NativeBridge.ForwardTelemetryToPlatform(JsonUtility.ToJson(telemetry));
            yield break;
        }

        private void ShowResult(int score, float meanErr)
        {
            _exit.Show();
            _resultRoot.Find("Title").GetComponent<Text>().text = score >= 85 ? "¡Piloto de precisión!" : score >= 65 ? "¡Buenos aterrizajes!" : "Misión completada";
            _resultRoot.Find("Detail").GetComponent<Text>().text = $"{_hits} de {_errors.Count} en el blanco · {_bullseyes} dianas";
            _resultRoot.Find("Extra").GetComponent<Text>().text = meanErr >= 0f ? $"A {meanErr:0.0}% del blanco".Replace('.', ',') : $"Mejor racha {_bestStreak}";
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

            var canvasGo = new GameObject("LandingCanvas");
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
            WorldBackdrop.Build(bgRect, GameWorld.LunarRange);

            var safeGo = new GameObject("SafeAreaContent");
            safeGo.transform.SetParent(canvasGo.transform, false);
            _safe = safeGo.AddComponent<RectTransform>();
            ApplySafeArea(_safe);

            _hud = new GameHud(_safe, "Aterrizaje Lunar", MarginU, this);
            BuildTimer();

            var playGo = new GameObject("Play");
            playGo.transform.SetParent(_safe, false);
            _play = playGo.AddComponent<RectTransform>();
            Stretch(_play);

            // Misión: texto suelto (sin tarjeta), el número grande en sol.
            _missionSmall = CenteredText(_play, "MissionSmall", 44, new Color(1f, 1f, 1f, 0.8f));
            _mission = CenteredText(_play, "Mission", 150, NeuroStyle.Sun);
            NeuroStyle.ClayText(_mission, 6f, 10f);
            _prompt = CenteredText(_play, "Prompt", 62, Color.white);
            NeuroStyle.ClayText(_prompt, 3.5f, 5f);
            _hint = CenteredText(_play, "Hint", 40, new Color(1f, 1f, 1f, 0.85f));
            _hint.gameObject.SetActive(false);

            BuildRuler();

            var beamRoot = Layer(_play, "Beam");
            for (int i = 0; i < BeamDots; i++)
            {
                var d = NewImage(beamRoot, "Dot", DiscSprite.Get());
                d.rectTransform.sizeDelta = new Vector2(12f, 12f);
                _beam.Add(d);
            }

            BuildLander();
            BuildFlag(); // encima de la nave: el blanco siempre se ve, aunque la nave quede justo al lado

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

        private static Text CenteredText(Transform parent, string name, int size, Color color)
        {
            var t = MakeText(parent, name, size, TextAnchor.MiddleCenter, color, 0f, 0f);
            var r = t.rectTransform;
            r.anchorMin = r.anchorMax = new Vector2(0.5f, 0.5f);
            r.pivot = new Vector2(0.5f, 0.5f);
            BestFit(t, Mathf.Max(24, size / 2));
            return t;
        }

        private void BuildRuler()
        {
            // La regla: una tira de arcilla crema sobre el borde de la superficie; marcas en los extremos (y el medio
            // en los niveles bajos). Los números de los extremos van debajo, sobre la luna.
            var go = new GameObject("Ruler");
            go.transform.SetParent(_play, false);
            _rulerRect = go.AddComponent<RectTransform>();
            _rulerRect.anchorMin = _rulerRect.anchorMax = new Vector2(0.5f, 0.5f);
            var img = go.AddComponent<Image>();
            img.sprite = RoundedRectSprite.Get(16);
            img.type = Image.Type.Sliced;
            img.color = ClayRaster.Cream;
            img.raycastTarget = false;
            NeuroStyle.ClayFrame(img, 4f, 7f);

            foreach (float x in new[] { 0f, 1f })
            {
                var tick = NewImage(_rulerRect, "EndTick", RoundedRectSprite.Get(8));
                tick.type = Image.Type.Sliced;
                tick.color = NeuroStyle.Ink;
                var tr = tick.rectTransform;
                tr.anchorMin = tr.anchorMax = new Vector2(x, 0.5f);
                tr.sizeDelta = new Vector2(12f, 76f);
                tick.gameObject.SetActive(true);
            }
            var mid = NewImage(_rulerRect, "MidTick", RoundedRectSprite.Get(8));
            mid.type = Image.Type.Sliced;
            mid.color = NeuroStyle.WithAlpha(NeuroStyle.Ink, 0.7f);
            _midTick = mid.rectTransform;
            _midTick.anchorMin = _midTick.anchorMax = new Vector2(0.5f, 0.5f);
            _midTick.sizeDelta = new Vector2(8f, 48f);

            _minLabel = EndLabel("Min", 0f);
            _maxLabel = EndLabel("Max", 1f);

            // Tramo entre el aterrizaje y el blanco.
            var gapGo = new GameObject("Gap");
            gapGo.transform.SetParent(_play, false);
            _gapRect = gapGo.AddComponent<RectTransform>();
            _gapRect.anchorMin = _gapRect.anchorMax = new Vector2(0.5f, 0.5f);
            _gapLine = gapGo.AddComponent<Image>();
            _gapLine.sprite = RoundedRectSprite.Get(8);
            _gapLine.type = Image.Type.Sliced;
            _gapLine.raycastTarget = false;
            _gapLabel = MakeText(_gapRect, "Label", 44, TextAnchor.MiddleCenter, Color.white, 0f, 0f);
            NeuroStyle.ClayText(_gapLabel, 3f, 4f);
            var gl = _gapLabel.rectTransform;
            gl.anchorMin = gl.anchorMax = new Vector2(0.5f, 0f);
            gl.pivot = new Vector2(0.5f, 1f);
            gl.sizeDelta = new Vector2(300f, 60f);
            gl.anchoredPosition = new Vector2(0f, -4f);
            gapGo.SetActive(false);
        }

        private Text EndLabel(string name, float x)
        {
            var t = MakeText(_rulerRect, name, 56, TextAnchor.MiddleCenter, Color.white, 0f, 0f);
            NeuroStyle.ClayText(t, 3.5f, 5f);
            var r = t.rectTransform;
            r.anchorMin = r.anchorMax = new Vector2(x, 0f);
            r.pivot = new Vector2(0.5f, 1f);
            r.sizeDelta = new Vector2(240f, 80f);
            r.anchoredPosition = new Vector2(0f, -34f);
            return t;
        }

        private void BuildFlag()
        {
            var go = new GameObject("Flag");
            go.transform.SetParent(_play, false);
            _flagRect = go.AddComponent<RectTransform>();
            _flagRect.anchorMin = _flagRect.anchorMax = new Vector2(0.5f, 0.5f);
            // El pivote es el pie del asta: la bandera "crece" desde el suelo, justo en el blanco.
            _flagRect.pivot = new Vector2((-0.5f / 1.05f + 1f) * 0.5f, (-0.95f / 1.05f + 1f) * 0.5f);
            _flagRect.sizeDelta = new Vector2(190f, 190f);
            var img = go.AddComponent<Image>();
            img.sprite = LandingSprites.Flag();
            img.raycastTarget = false;
            _flagLabel = MakeText(_flagRect, "Label", 58, TextAnchor.MiddleCenter, NeuroStyle.Sun, 0f, 0f);
            NeuroStyle.ClayText(_flagLabel, 4f, 6f);
            var lr = _flagLabel.rectTransform;
            lr.anchorMin = lr.anchorMax = new Vector2(0.26f, 1f);
            lr.pivot = new Vector2(0.5f, 0f);
            lr.sizeDelta = new Vector2(320f, 80f);
            lr.anchoredPosition = new Vector2(0f, 4f);
            go.SetActive(false);
        }

        private void BuildLander()
        {
            var go = new GameObject("Lander");
            go.transform.SetParent(_play, false);
            _landerRect = go.AddComponent<RectTransform>();
            _landerRect.anchorMin = _landerRect.anchorMax = new Vector2(0.5f, 0.5f);
            _landerRect.sizeDelta = new Vector2(LanderSize, LanderSize);
            _flame = NewImage(_landerRect, "Flame", RadialGlowSprite.Get());
            _flame.rectTransform.anchoredPosition = new Vector2(0f, -LanderSize * 0.42f);
            _flame.color = new Color(1f, 1f, 1f, 0f);
            _flame.gameObject.SetActive(true);
            _lander = NewImage(_landerRect, "Body", LandingSprites.Lander());
            Stretch(_lander.rectTransform);
            _lander.gameObject.SetActive(true);
            go.SetActive(false);
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
            float contentW = _playW - MarginU * 2f;

            // Misión arriba (debajo del marcador), el aviso del resultado bajo la misión.
            float y = top - (GameHud.Height + 40f);
            _missionSmall.rectTransform.sizeDelta = new Vector2(contentW, 60f);
            _missionSmall.rectTransform.anchoredPosition = new Vector2(0f, y - 30f);
            _mission.rectTransform.sizeDelta = new Vector2(contentW, 180f);
            _mission.rectTransform.anchoredPosition = new Vector2(0f, y - 150f);
            _prompt.rectTransform.sizeDelta = new Vector2(contentW, 90f);
            _prompt.rectTransform.anchoredPosition = new Vector2(0f, y - 290f);
            _skyTop = y - 330f - LanderSize * 0.5f;

            // Regla sobre el borde de la superficie lunar (el mundo la dibuja en el 26% de abajo).
            _rulerW = contentW - 60f;
            _rulerY = bottom + _playH * 0.26f;
            _rulerRect.sizeDelta = new Vector2(_rulerW, 28f);
            _rulerRect.anchoredPosition = new Vector2(0f, _rulerY);
            _hint.rectTransform.sizeDelta = new Vector2(contentW, 70f);
            _hint.rectTransform.anchoredPosition = new Vector2(0f, bottom + _playH * 0.12f);
        }

        private void UpdateHud()
        {
            _hud.SetLevel(_dda.PresentedLevel);
            if (Endless) _hud.SetPoints(_points);
            else _hud.SetInfo($"{Mathf.Min(_errors.Count + 1, LandingContract.PrecisionTrials)} de {LandingContract.PrecisionTrials}");
        }
    }
}
