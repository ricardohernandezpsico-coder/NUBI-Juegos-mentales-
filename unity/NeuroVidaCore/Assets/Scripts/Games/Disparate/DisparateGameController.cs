using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using NeuroVida.Bridge;
using NeuroVida.Contracts;
using NeuroVida.Games;
using NeuroVida.Games.Secuencia; // RoundedRectSprite / RadialGlowSprite / RingSprite
using NeuroVida.Games.Shared;
using static NeuroVida.Games.Shared.UiKit;

namespace NeuroVida.Games.Disparate
{
    /// <summary>
    /// "¿Verdad o disparate?": juego estrella de Lenguaje (verificación de frases, ver <see cref="DisparateContract"/> y
    /// docs/diseno-verdad-o-disparate.md). Desde una sala de radio llegan transmisiones con una frase corta: se decide si es
    /// verdad o un disparate, tocando los botones o deslizando la placa. Todo está pensado para sentirse CONTINUO:
    /// la frase siguiente se prepara mientras se muestra la actual y la transición entre frases dura menos de 300 ms.
    /// <list type="bullet">
    /// <item>Llegada: una cinta de luz sale de la antena y la frase "sintoniza" (de estática a letras nítidas en ~200 ms); el reloj
    /// de respuesta arranca recién cuando está nítida.</item>
    /// <item>Señal: la barra se vacía; en el último 25% la placa tiembla un poco y aparece estática en sus bordes (no sobre las letras).</item>
    /// <item>Acierto: la placa late, sale una onda de la antena, chispas viajan por la cinta hasta la antena y se enciende una barra
    /// de señal; con racha ≥ 5 las ondas son de color y con 10 seguidas una aurora cruza el cielo (antena encendida, × 1,5).</item>
    /// <item>Error: chasquido suave, la placa se sacude poco, ✗ y la corrección en su lugar, con un fundido. Se agotó la señal: la frase se
    /// disuelve en estática.</item>
    /// <item>Deslizar la placa: sigue al dedo con una leve inclinación; derecha = VERDAD, izquierda = DISPARATE.</item>
    /// <item>Mantener presionada la placa ~0,8 s: "Esta frase no está clara" (no cuenta para nada; la app guarda el id).</item>
    /// <item>Ráfaga cada 12 frases: 5 frases cortas muy rápidas, estrellas veloces, fuera de la escalera y de las medidas.</item>
    /// </list>
    /// Con "quitar animaciones": sin estática animada, sin temblor, sin aurora y con fundidos simples.
    /// Reto = 2 minutos; Precisión = 30 frases sin señal que se apaga.
    /// </summary>
    public class DisparateGameController : GameControllerBase
    {
        public const string GameId = DisparateContract.GameId;
        private const string RecentKey = "disparate_recent";

        private const float UnitsPerDp = 3f;
        private const float MarginU = 60f;
        private const float DragThresholdFrac = 0.22f;   // fracción del ancho jugable para soltar y responder
        private const float HoldSeconds = 0.8f;
        private const float MoveSlopU = 28f;

        private static readonly Color GoodColor = NeuroStyle.Lime;
        private static readonly Color BadColor = NeuroStyle.Coral;
        private static readonly Color AmberColor = NeuroStyle.Sun;
        private static readonly Color PlateColor = new Color(1f, 0.973f, 0.925f, 1f);
        private const string GlyphPool = "#%&@*+=?/~<>";

        private enum Phase { Idle, Playing, Done }

        private struct PlateLayout { public float W, H, TextW, TextH; }

        private System.Random _rng;
        private AdaptiveDifficulty _dda;
        private SentenceBank _bank;
        private DisparateDirector _director;
        private DisparateTally _tally;
        private Phase _phase = Phase.Idle;
        private bool Endless => _config != null && _config.config.timed;
        private bool Precision => !Endless;
        private bool Senior;
        private int _previousFrameRate;

        private SentenceSpec _cur, _next;
        private PlateLayout _nextLayout;
        private bool _answerable, _answerGiven, _answerIsTruth, _timedOut, _unclearDone, _wasBurst;
        private float _legibleAt, _answerAt, _signalSeconds;
        private int _streak, _points, _resolved;
        private float _endsAt;
        private int _lastTickSecond = -1;
        private bool _hintDone;
        private bool _perfectShown;

        // entrada
        private bool _dragging, _moved, _holdFired;
        private float _dragStartX, _dragStartTime, _plateX;
        private bool _ignoreUntilUp;

        // UI
        private RectTransform _safe, _play, _fxRect, _timerBg, _timerFill, _antenna, _plate, _auroraLayer;
        private RectTransform _btnTrue, _btnFalse, _sigBar, _sigFillRect;
        private CanvasGroup _plateGroup;
        private Image _plateImg, _plateGlow, _tipGlow, _ribbon, _sigFill, _mark;
        private Text _label, _fix, _measure, _hint, _swipeTrue, _swipeFalse, _sigLabel, _burstTag;
        private readonly List<Image> _rings = new List<Image>();
        private readonly List<Image> _meter = new List<Image>();
        private readonly List<Image> _aurora = new List<Image>();
        private readonly List<RawImage> _edgeStatic = new List<RawImage>();
        private Toast _toast;
        private ExitButton _exit;
        private GameHud _hud;
        private CountdownScreen _countdown;
        private StarfieldFx _stars;
        private float _playW, _playH, _plateY, _sigY;
        private Vector2 _dishPos, _tipPos, _btnTrueCenter, _btnFalseCenter;
        private Vector2 _btnSize;
        private float[] _decodeThr = new float[0];
        private float _tremble, _staticAlpha;
        private float _warpTarget;
        private int _lit;

        // ------------------------------------------------------------------ sesión

        public void StartSession(SequenceInitConfig config)
        {
            _config = config;
            _rng = new System.Random();
            var age = DdaUserProfileConfig.ParseAgeBand(config.config.age_band);
            Senior = age == AgeBand.Senior;
            float start = AdaptiveDifficulty.StartRating(config.config, DisparateContract.MaxLevel);
            // ~60 decisiones en el Reto: pasos de tamaño común. Sin tiempo de reacción: el tiempo es la medida, no el nivel.
            _dda = new AdaptiveDifficulty(DisparateContract.MaxLevel, age, start, stepUp: 0.15f, useReaction: false);
            _bank = SentenceBank.Load();
            string recent = "";
            try { recent = PlayerPrefs.GetString(RecentKey, ""); } catch (System.Exception) { }
            _director = new DisparateDirector(_bank, _rng, DisparateContract.RecentIds(recent));
            _tally = new DisparateTally();

            _previousFrameRate = Application.targetFrameRate;
            Application.targetFrameRate = 60;

            _phase = Phase.Idle;
            _cur = _next = null;
            _answerable = _answerGiven = _timedOut = _dragging = _moved = _holdFired = _ignoreUntilUp = false;
            _streak = _points = _resolved = 0;
            _endsAt = 0f;
            _lastTickSecond = -1;
            _hintDone = _perfectShown = false;
            _warpTarget = 0f;
            _lit = 0;

            _resultRoot.gameObject.SetActive(false);
            _exit.Hide();
            _timerBg.gameObject.SetActive(Endless);
            _sigBar.gameObject.SetActive(!Precision);
            _sigLabel.gameObject.SetActive(!Precision);
            _hud.SetStreak(0);
            _hint.gameObject.SetActive(false);
            _plate.gameObject.SetActive(false);
            _mark.gameObject.SetActive(false);
            _swipeTrue.gameObject.SetActive(false);
            _swipeFalse.gameObject.SetActive(false);
            SetMeter(0, false);
            HideAurora();

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
            StartCoroutine(PrewarmSprites());
            yield return StartCoroutine(_countdown.Play("¿Verdad o disparate?", Assessment.Subtitle("Lee y decide rápido"), () => _safe.gameObject.SetActive(true)));
            _safe.gameObject.SetActive(true);
            yield return null;
            ApplySafeArea(_safe);
            Canvas.ForceUpdateCanvases();
            Layout();
            UpdateHud();

            _endsAt = GameClock.Time + DisparateContract.RetoSeconds;
            _phase = Phase.Playing;
            _hint.gameObject.SetActive(true);
            _hint.text = "¿Es verdad o es un disparate?\nResponde rápido";

            while (!Finished())
            {
                var spec = TakeNext();
                if (spec == null) break;
                yield return StartCoroutine(ShowSentence(spec));
                yield return StartCoroutine(WaitForAnswer());
                if (Finished() && !_answerGiven && !_timedOut) break;     // se acabó el tiempo a mitad de una frase: no cuenta
                yield return StartCoroutine(Resolve());
            }
            yield return StartCoroutine(FinishGame());
        }

        private bool Finished()
        {
            if (Endless) return GameClock.Time >= _endsAt;
            return _resolved >= DisparateContract.PrecisionSentences && !_director.InBurst;
        }

        /// <summary>Hornea la antena, los íconos y la estática durante la cuenta regresiva (así la primera frase no traba).</summary>
        private IEnumerator PrewarmSprites()
        {
            DisparateSprites.Antenna();
            yield return null;
            DisparateSprites.CheckIcon();
            DisparateSprites.BrokenWaveIcon();
            DisparateSprites.Band();
            DisparateSprites.NoiseTexture();
            yield return null;
            DisparateSounds.Tune();
            DisparateSounds.StaticClick();
            yield return null;
            DisparateSounds.WaveRing();
            DisparateSounds.Lost();
        }

        // ------------------------------------------------------------------ preparar y mostrar la frase

        private SentenceSpec TakeNext()
        {
            int level = _dda.PresentedLevel;
            var spec = _next;
            _next = null;
            // si el nivel cambió de tipo mientras se preparaba la siguiente, se elige de nuevo (la ráfaga no depende del nivel)
            if (spec != null && !spec.Burst && (int)DisparateContract.TypeForLevel(level) != spec.S.t) spec = null;
            return spec ?? _director.Next(level, Precision, Senior);
        }

        private void PrefetchNext()
        {
            _next = _director.Next(_dda.PresentedLevel, Precision, Senior);
            if (_next != null) _nextLayout = Measure(_next.S.f);
        }

        /// <summary>Tamaño de la placa y del texto para una frase (1 renglón si cabe, si no 2; en mayores hasta 3).</summary>
        private PlateLayout Measure(string text)
        {
            int fontPx = Mathf.RoundToInt(DisparateContract.PhraseSizeSp(Senior) * UnitsPerDp);
            _measure.fontSize = fontPx;
            float contentW = _playW - MarginU * 2f;
            const float padX = 64f, padY = 46f;
            _measure.horizontalOverflow = HorizontalWrapMode.Overflow;
            _measure.rectTransform.sizeDelta = new Vector2(4000f, 400f);
            _measure.text = text;
            float w1 = _measure.preferredWidth;
            var l = new PlateLayout();
            if (w1 + padX * 2f <= contentW)
            {
                l.W = Mathf.Max(w1 + padX * 2f, 420f);
                l.TextW = l.W - padX * 2f + 6f;
                l.TextH = fontPx * 1.3f;
            }
            else
            {
                l.W = contentW;
                l.TextW = l.W - padX * 2f;
                _measure.horizontalOverflow = HorizontalWrapMode.Wrap;
                _measure.rectTransform.sizeDelta = new Vector2(l.TextW, 400f);
                l.TextH = Mathf.Max(fontPx * 2.5f, _measure.preferredHeight);
            }
            l.H = l.TextH + padY * 2f;
            return l;
        }

        private IEnumerator ShowSentence(SentenceSpec spec)
        {
            _cur = spec;
            _answerGiven = _timedOut = _unclearDone = _answerable = false;
            _ignoreUntilUp = _dragging;      // si el dedo sigue apoyado de la frase anterior, no cuenta
            var layout = _next == spec ? _nextLayout : Measure(spec.S.f);
            ApplyLayout(layout, spec.S.f);

            // ráfaga: estrellas veloces y un cartelito
            if (spec.Burst)
            {
                if (!_wasBurst) { _toast.Show("¡Ráfaga!", "Cinco frases rápidas; no cuentan para tu medida", AmberColor, 1.2f); GameFeel.LevelUp(); }
                _warpTarget = GameFeel.ReduceMotion ? 0f : 2.2f;
            }
            else _warpTarget = 0f;
            _wasBurst = spec.Burst;

            _plateX = 0f;
            _plate.anchoredPosition = new Vector2(0f, _plateY);
            _plate.localRotation = Quaternion.identity;
            _plate.gameObject.SetActive(true);
            _mark.gameObject.SetActive(false);
            _fix.text = "";
            _label.color = NeuroStyle.Ink;
            _plateGlow.color = new Color(1f, 1f, 1f, 0f);
            SetEdgeStatic(0f);
            _signalSeconds = spec.SignalSeconds;
            SetSignal(1f);

            PlayClip(DisparateSounds.Tune(), 0.5f);
            float dur = GameFeel.ReduceMotion ? 0.1f : (spec.Burst ? 0.14f : 0.2f);
            StartCoroutine(RibbonSweep(dur));
            float t = 0f;
            BuildDecode(spec.S.f);
            while (t < dur)
            {
                t += GameClock.DeltaTime;
                float k = Mathf.Clamp01(t / dur);
                _plateGroup.alpha = Mathf.Clamp01(k * 3f);
                float s = GameFeel.ReduceMotion ? 1f : Mathf.LerpUnclamped(0.93f, 1f, UiFx.EaseOutBack(k));
                _plate.localScale = Vector3.one * s;
                _label.text = GameFeel.ReduceMotion ? spec.S.f : Decode(spec.S.f, Mathf.Clamp01((k - 0.15f) / 0.8f));
                yield return null;
            }
            _plateGroup.alpha = 1f;
            _plate.localScale = Vector3.one;
            _label.text = spec.S.f;
            _legibleAt = GameClock.Time;              // el reloj de respuesta arranca recién ahora: la frase ya es nítida
            _answerable = true;
            // la siguiente se prepara mientras esta se lee
            PrefetchNext();
        }

        private void ApplyLayout(PlateLayout l, string text)
        {
            int fontPx = Mathf.RoundToInt(DisparateContract.PhraseSizeSp(Senior) * UnitsPerDp);
            _plate.sizeDelta = new Vector2(l.W, l.H);
            _label.fontSize = fontPx;
            _fix.fontSize = fontPx;
            _label.rectTransform.sizeDelta = new Vector2(l.TextW, l.TextH);
            _fix.rectTransform.sizeDelta = new Vector2(l.TextW, l.TextH);
            _label.horizontalOverflow = HorizontalWrapMode.Wrap;
            _fix.horizontalOverflow = HorizontalWrapMode.Wrap;
            // estática de los bordes: tiras alrededor de la placa (nunca sobre las letras)
            const float strip = 26f, gap = 8f;
            SetStrip(0, new Vector2(0f, l.H * 0.5f + gap + strip * 0.5f), new Vector2(l.W + 40f, strip));
            SetStrip(1, new Vector2(0f, -l.H * 0.5f - gap - strip * 0.5f), new Vector2(l.W + 40f, strip));
            SetStrip(2, new Vector2(-l.W * 0.5f - gap - strip * 0.5f, 0f), new Vector2(strip, l.H + 40f));
            SetStrip(3, new Vector2(l.W * 0.5f + gap + strip * 0.5f, 0f), new Vector2(strip, l.H + 40f));
            _plateGlow.rectTransform.sizeDelta = new Vector2(l.W * 1.5f + 200f, l.H * 2f + 200f);
            _mark.rectTransform.anchoredPosition = new Vector2(l.W * 0.5f - 18f, l.H * 0.5f - 6f);
            // rótulos del deslizar, sobre la placa
            float ly = _plateY + l.H * 0.5f + 110f;
            _swipeTrue.rectTransform.anchoredPosition = new Vector2(_playW * 0.26f, ly);
            _swipeFalse.rectTransform.anchoredPosition = new Vector2(-_playW * 0.26f, ly);
        }

        private void SetStrip(int i, Vector2 pos, Vector2 size)
        {
            var r = _edgeStatic[i].rectTransform;
            r.anchoredPosition = pos;
            r.sizeDelta = size;
            _edgeStatic[i].uvRect = new Rect(0f, 0f, size.x / 96f, size.y / 96f);
        }

        // ------------------------------------------------------------------ texto que sintoniza (estática → letras)

        private void BuildDecode(string text)
        {
            _decodeThr = new float[text.Length];
            for (int i = 0; i < text.Length; i++) _decodeThr[i] = (float)_rng.NextDouble();
        }

        /// <summary>Cada letra pasa de un símbolo al azar a su letra cuando el avance [k] supera su turno (las letras no se mueven).</summary>
        private string Decode(string text, float k)
        {
            var sb = new System.Text.StringBuilder(text.Length);
            for (int i = 0; i < text.Length; i++)
            {
                char c = text[i];
                if (c == ' ' || k >= 1f || (i < _decodeThr.Length && _decodeThr[i] < k)) sb.Append(c);
                else sb.Append(GlyphPool[_rng.Next(GlyphPool.Length)]);
            }
            return sb.ToString();
        }

        private string Scramble(string text, float k)
        {
            // k = 0 nítida … 1 pura estática (las letras se van disolviendo en el orden contrario)
            return Decode(text, 1f - k);
        }

        // ------------------------------------------------------------------ esperar la respuesta

        private IEnumerator WaitForAnswer()
        {
            _tremble = 0f;
            float last = -1f;
            while (!_answerGiven)
            {
                if (Finished() && Endless) yield break;
                if (_signalSeconds > 0f)
                {
                    float elapsed = GameClock.Time - _legibleAt;
                    float left = Mathf.Clamp01(1f - elapsed / _signalSeconds);
                    if (Mathf.Abs(left - last) > 0.0005f) { SetSignal(left); last = left; }
                    // último 25%: la placa tiembla un poco y aparece estática en los bordes
                    float urgency = Mathf.Clamp01((0.25f - left) / 0.25f);
                    _staticAlpha = GameFeel.ReduceMotion ? 0f : urgency * 0.9f;
                    _tremble = GameFeel.ReduceMotion ? 0f : urgency * 5f;
                    if (left <= 0f) { _timedOut = true; break; }
                }
                yield return null;
            }
            _answerable = false;
            _tremble = 0f;
            _staticAlpha = 0f;
            SetEdgeStatic(0f);
        }

        // ------------------------------------------------------------------ entrada (botones, deslizar y mantener)

        private void Update()
        {
            UpdateClock();
            AnimateAmbient();
            if (_phase != Phase.Playing) return;
            UpdatePlateMotion();
            HandleInput();
        }

        private void HandleInput()
        {
            bool down = false, up = false, held = false;
            Vector2 pos = Vector2.zero;
            if (Input.touchCount > 0)
            {
                var t = Input.GetTouch(0);
                pos = t.position;
                down = t.phase == TouchPhase.Began;
                up = t.phase == TouchPhase.Ended || t.phase == TouchPhase.Canceled;
                held = !up;
            }
            else
            {
                pos = Input.mousePosition;
                down = Input.GetMouseButtonDown(0);
                up = Input.GetMouseButtonUp(0);
                held = Input.GetMouseButton(0);
            }
            if (!down && !held && !up) return;
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(_play, pos, null, out var local)) return;

            if (down)
            {
                if (!_answerable || _answerGiven) { _ignoreUntilUp = true; return; }
                _ignoreUntilUp = false;
                if (InRect(local, _btnTrueCenter, _btnSize)) { PressButton(_btnTrue); RequestAnswer(true); _ignoreUntilUp = true; return; }
                if (InRect(local, _btnFalseCenter, _btnSize)) { PressButton(_btnFalse); RequestAnswer(false); _ignoreUntilUp = true; return; }
                if (InRect(local, new Vector2(0f, _plateY), _plate.sizeDelta * 1.08f))
                {
                    _dragging = true; _moved = false; _holdFired = false;
                    _dragStartX = local.x; _dragStartTime = GameClock.Time;
                }
                return;
            }
            if (_ignoreUntilUp) { if (up) _dragging = false; return; }
            if (!_dragging) return;

            float dx = local.x - _dragStartX;
            if (!_moved && Mathf.Abs(dx) > MoveSlopU) _moved = true;
            if (_moved) _plateX = dx * 0.9f;
            if (!_moved && !_holdFired && held && GameClock.Time - _dragStartTime >= HoldSeconds && _answerable && !_answerGiven)
            {
                _holdFired = true;
                ReportUnclear();
            }
            if (up)
            {
                _dragging = false;
                if (_moved && !_holdFired && _answerable && !_answerGiven && Mathf.Abs(dx) > _playW * DragThresholdFrac)
                    RequestAnswer(dx > 0f);
                _moved = false;
            }
        }

        private static bool InRect(Vector2 p, Vector2 center, Vector2 size) =>
            Mathf.Abs(p.x - center.x) <= size.x * 0.5f && Mathf.Abs(p.y - center.y) <= size.y * 0.5f;

        private void PressButton(RectTransform rect) => StartCoroutine(PopRect(rect, 0.94f, 0.14f));

        private void RequestAnswer(bool saysTruth)
        {
            if (!_answerable || _answerGiven) return;
            _answerGiven = true;
            _answerIsTruth = saysTruth;
            _answerAt = GameClock.Time;
            GameFeel.Haptic(GameFeel.HapticKind.Light);
        }

        /// <summary>Mantener presionada la placa: "Esta frase no está clara". No cuenta para nada; la app guarda el id.</summary>
        private void ReportUnclear()
        {
            if (_cur == null || _unclearDone) return;
            _unclearDone = true;
            _tally.ReportUnclear(_cur.S.i);
            _toast.Show("Gracias, la revisaremos", "Marcaste esta frase como poco clara", AmberColor, 1.0f);
            PlayClip(DisparateSounds.Unclear(), 0.5f);
            GameFeel.Haptic(GameFeel.HapticKind.Double);
            StartCoroutine(UiFx.RingBurst(_fxRect, new Vector2(0f, _plateY), AmberColor, 200f, 700f, 0.45f));
        }

        // movimiento de la placa: sigue al dedo con inclinación y vuelve con resorte; temblor de la señal baja
        private void UpdatePlateMotion()
        {
            if (!_plate.gameObject.activeSelf) return;
            float dt = GameClock.DeltaTime;
            if (!_shaking && (!_dragging || !_moved)) _plateX = Mathf.Lerp(_plateX, 0f, Mathf.Min(1f, dt * 14f));
            float shake = _tremble > 0f ? Mathf.Sin(GameClock.Time * 70f) * _tremble : 0f;
            if (_shaking) shake = 0f;
            _plate.anchoredPosition = new Vector2(_plateX + shake, _plateY);
            _plate.localRotation = Quaternion.Euler(0f, 0f, GameFeel.ReduceMotion ? 0f : -_plateX / _playW * 26f);
            // rótulos VERDAD / DISPARATE según hacia dónde se desliza
            float f = Mathf.Clamp01(Mathf.Abs(_plateX) / (_playW * DragThresholdFrac));
            bool right = _plateX > 0f;
            SetSwipeLabel(_swipeTrue, right ? f : 0f);
            SetSwipeLabel(_swipeFalse, right ? 0f : f);
        }

        private bool _shaking;

        private static void SetSwipeLabel(Text t, float a)
        {
            bool on = a > 0.02f;
            if (t.gameObject.activeSelf != on) t.gameObject.SetActive(on);
            if (on) t.canvasRenderer.SetAlpha(a);
        }

        // ------------------------------------------------------------------ resolver

        private IEnumerator Resolve()
        {
            var spec = _cur;
            bool timeout = _timedOut && !_answerGiven;
            bool correct = !timeout && _answerIsTruth == spec.S.v;
            int ms = timeout ? -1 : Mathf.RoundToInt((_answerAt - _legibleAt) * 1000f);
            if (spec.Burst) _tally.AddBurst(correct);
            else
            {
                _tally.Add(spec.S, correct, correct ? ms : -1);
                _resolved++;
            }
            if (!_hintDone) { _hintDone = true; _hint.gameObject.SetActive(false); }
            if (!spec.Burst) Register(correct);

            if (correct) yield return StartCoroutine(CorrectFeedback(spec));
            else if (timeout) yield return StartCoroutine(LostFeedback(spec));
            else yield return StartCoroutine(WrongFeedback(spec));
            UpdateHud();
        }

        private void Register(bool correct)
        {
            var change = _dda.Register(correct);
            if (change == DdaChange.Up) _toast.Show("¡Subes de nivel!", DisparateContract.LevelNews(_dda.Level), GoodColor, 1.1f);
            else if (change == DdaChange.Down || _dda.Struggling) _toast.Show("Con calma", "Lee la frase hasta el final", AmberColor, 0.9f);
        }

        private IEnumerator CorrectFeedback(SentenceSpec spec)
        {
            _streak++;
            int pts = DisparateContract.Points(_dda.PresentedLevel, _streak, spec.Burst);
            _points += pts;
            GameFeel.Correct(_streak);
            PlayClip(DisparateSounds.WaveRing(), 0.45f);
            _hud.SetStreak(_streak);
            SetMeter(Mathf.Min(5, _streak), true);
            _plateGlow.color = new Color(GoodColor.r, GoodColor.g, GoodColor.b, 0.5f);
            _mark.sprite = AnswerMarkSprite.Check();
            _mark.gameObject.SetActive(true);
            StartCoroutine(PopIn(_mark.rectTransform, 0.2f));
            StartCoroutine(PlateBeat());
            StartCoroutine(UiFx.RingBurst(_fxRect, _dishPos, StreakColor(), 120f, 760f, 0.55f));
            if (!GameFeel.ReduceMotion) StartCoroutine(SparkTravel(new Vector2(_plateX, _plateY), _tipPos));
            StartCoroutine(FloatText(new Vector2(0f, _plateY + _plate.sizeDelta.y * 0.5f + 40f), "+" + pts, NeuroStyle.Sun));
            if (_streak == DisparateContract.PerfectStreak && !_perfectShown)
            {
                _perfectShown = true;
                StartCoroutine(PerfectTransmission());
            }
            // breve: la frase sigue llegando; la placa se despide en ~120 ms
            float t = 0f;
            const float seconds = 0.12f;
            while (t < seconds)
            {
                t += GameClock.DeltaTime;
                float k = Mathf.Clamp01(t / seconds);
                _plateGroup.alpha = 1f - k;
                _plate.localScale = Vector3.one * Mathf.Lerp(1.04f, 1.1f, k);
                yield return null;
            }
            _plateGroup.alpha = 0f;
            _plate.gameObject.SetActive(false);
        }

        private IEnumerator WrongFeedback(SentenceSpec spec)
        {
            BreakStreak();
            GameFeel.Wrong();
            PlayClip(DisparateSounds.StaticClick(), 0.45f);
            _plateGlow.color = new Color(BadColor.r, BadColor.g, BadColor.b, 0.5f);
            _mark.sprite = AnswerMarkSprite.Cross();
            _mark.gameObject.SetActive(true);
            StartCoroutine(PopIn(_mark.rectTransform, 0.2f));
            if (!GameFeel.ReduceMotion) { _shaking = true; StartCoroutine(PlateShake()); }
            // la corrección aparece en su lugar con un fundido
            string fix = string.IsNullOrEmpty(spec.S.r) ? spec.S.f : spec.S.r;
            var fl = Measure(fix);
            if (fl.H > _plate.sizeDelta.y) { _plate.sizeDelta = new Vector2(_plate.sizeDelta.x, fl.H); }
            _fix.rectTransform.sizeDelta = new Vector2(fl.TextW, fl.TextH);
            _fix.text = fix;
            _fix.canvasRenderer.SetAlpha(0f);
            float t = 0f;
            float fade = GameFeel.ReduceMotion ? 0.12f : 0.3f;
            while (t < fade)
            {
                t += GameClock.DeltaTime;
                float k = Mathf.Clamp01(t / fade);
                _label.canvasRenderer.SetAlpha(1f - k);
                _fix.canvasRenderer.SetAlpha(k);
                yield return null;
            }
            _label.canvasRenderer.SetAlpha(1f);
            _label.text = "";
            _fix.canvasRenderer.SetAlpha(1f);
            _shaking = false;
            float hold = Senior ? 1.9f : 1.4f;
            t = 0f;
            while (t < hold) { t += GameClock.DeltaTime; yield return null; }
            // despedida
            t = 0f;
            while (t < 0.12f)
            {
                t += GameClock.DeltaTime;
                _plateGroup.alpha = 1f - Mathf.Clamp01(t / 0.12f);
                yield return null;
            }
            _plateGroup.alpha = 0f;
            _fix.text = "";
            _plate.gameObject.SetActive(false);
        }

        private IEnumerator LostFeedback(SentenceSpec spec)
        {
            BreakStreak();
            PlayClip(DisparateSounds.Lost(), 0.5f);
            _toast.Show("Se perdió la señal", "Sigue la próxima frase", AmberColor, 0.9f);
            // la frase se disuelve en estática (sin castigo extra)
            BuildDecode(spec.S.f);
            float dur = GameFeel.ReduceMotion ? 0.15f : 0.45f;
            float t = 0f;
            while (t < dur)
            {
                t += GameClock.DeltaTime;
                float k = Mathf.Clamp01(t / dur);
                if (!GameFeel.ReduceMotion) _label.text = Scramble(spec.S.f, k);
                _plateGroup.alpha = 1f - Mathf.Clamp01((k - 0.4f) / 0.6f);
                SetEdgeStatic(GameFeel.ReduceMotion ? 0f : (1f - k) * 0.9f);
                yield return null;
            }
            _plateGroup.alpha = 0f;
            _plate.gameObject.SetActive(false);
            SetEdgeStatic(0f);
        }

        private void BreakStreak()
        {
            _streak = 0;
            _hud.SetStreak(0);
            SetMeter(0, false);
            _perfectShown = false;
        }

        private Color StreakColor()
        {
            if (_streak >= DisparateContract.PerfectStreak) return NeuroStyle.Sun;
            if (_streak >= DisparateContract.ColorWavesStreak) return NeuroStyle.Lime;
            return NeuroStyle.Sky;
        }

        // ------------------------------------------------------------------ efectos

        private IEnumerator PlateBeat()
        {
            float t = 0f;
            const float seconds = 0.18f;
            while (t < seconds)
            {
                t += GameClock.DeltaTime;
                float k = Mathf.Clamp01(t / seconds);
                _plate.localScale = Vector3.one * (1f + 0.07f * Mathf.Sin(k * Mathf.PI));
                yield return null;
            }
        }

        private IEnumerator PlateShake()
        {
            float t = 0f;
            const float seconds = 0.3f;
            while (t < seconds)
            {
                t += GameClock.DeltaTime;
                float k = Mathf.Clamp01(t / seconds);
                _plateX = Mathf.Sin(k * Mathf.PI * 6f) * 11f * (1f - k);   // amplitud chica
                yield return null;
            }
            _plateX = 0f;
        }

        private IEnumerator RibbonSweep(float seconds)
        {
            // la cinta de luz sale de la antena y recorre la pantalla hasta la placa, donde la frase sintoniza
            if (GameFeel.ReduceMotion) { _ribbon.gameObject.SetActive(false); yield break; }
            _ribbon.gameObject.SetActive(true);
            var r = _ribbon.rectTransform;
            Vector2 a = _dishPos, b = new Vector2(0f, _plateY);
            float t = 0f;
            while (t < seconds)
            {
                t += GameClock.DeltaTime;
                float k = Mathf.Clamp01(t / seconds);
                float e = UiFx.EaseOutCubic(k);
                Vector2 p = Vector2.Lerp(a, b, e);
                Vector2 d = b - a;
                float len = Mathf.Max(60f, d.magnitude * Mathf.Sin(Mathf.Clamp01(k) * Mathf.PI) * 0.9f + 90f);
                r.anchoredPosition = Vector2.Lerp(a, b, e * 0.5f) ;
                r.sizeDelta = new Vector2(len, 46f);
                r.localRotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(d.y, d.x) * Mathf.Rad2Deg);
                _ribbon.color = new Color(0.55f, 0.88f, 1f, 0.85f * (1f - k * 0.6f));
                yield return null;
            }
            _ribbon.gameObject.SetActive(false);
        }

        private IEnumerator SparkTravel(Vector2 from, Vector2 to)
        {
            // chispas que viajan desde la placa hasta la antena
            const int n = 7;
            var imgs = new Image[n];
            for (int i = 0; i < n; i++)
            {
                imgs[i] = NewImage(_fxRect, "Spark", DiscSprite.Get());
                imgs[i].color = i % 2 == 0 ? NeuroStyle.Sun : NeuroStyle.Sky;
                imgs[i].gameObject.SetActive(true);
            }
            float t = 0f;
            const float seconds = 0.5f;
            Vector2 mid = (from + to) * 0.5f + new Vector2(120f, 0f);
            while (t < seconds + 0.2f)
            {
                t += GameClock.DeltaTime;
                for (int i = 0; i < n; i++)
                {
                    if (imgs[i] == null) continue;
                    float k = Mathf.Clamp01((t - i * 0.03f) / seconds);
                    float e = UiFx.EaseOutCubic(k);
                    Vector2 p = (1 - e) * (1 - e) * from + 2f * (1 - e) * e * mid + e * e * to;
                    imgs[i].rectTransform.anchoredPosition = p;
                    float s = Mathf.Lerp(34f, 12f, e);
                    imgs[i].rectTransform.sizeDelta = new Vector2(s, s);
                    imgs[i].color = NeuroStyle.WithAlpha(imgs[i].color, k >= 1f ? 0f : 1f);
                }
                yield return null;
            }
            for (int i = 0; i < n; i++) if (imgs[i] != null) Destroy(imgs[i].gameObject);
        }

        private IEnumerator PerfectTransmission()
        {
            // 10 seguidas: la antena se enciende (× 1,5) y una aurora suave cruza el cielo
            _toast.Show("¡Transmisión perfecta!", "Antena encendida: puntos × 1,5", AmberColor, 1.4f);
            GameFeel.LevelUp();
            PlayClip(DisparateSounds.Aurora(), 0.55f);
            if (GameFeel.ReduceMotion) yield break;
            float t = 0f;
            const float seconds = 4f;
            while (t < seconds)
            {
                t += GameClock.DeltaTime;
                float k = t / seconds;
                float env = Mathf.Sin(Mathf.Clamp01(k) * Mathf.PI);
                for (int i = 0; i < _aurora.Count; i++)
                {
                    var r = _aurora[i].rectTransform;
                    r.anchoredPosition = new Vector2(Mathf.Lerp(-_playW * 0.55f, _playW * 0.55f, k) * (i % 2 == 0 ? 1f : -1f) * 0.6f, r.anchoredPosition.y);
                    var c = _aurora[i].color;
                    _aurora[i].color = new Color(c.r, c.g, c.b, 0.2f * env);
                    _aurora[i].gameObject.SetActive(true);
                }
                yield return null;
            }
            HideAurora();
        }

        private void HideAurora()
        {
            foreach (var a in _aurora) if (a != null) a.gameObject.SetActive(false);
        }

        private IEnumerator FloatText(Vector2 pos, string text, Color color)
        {
            var t = MakeText(_fxRect, "Float", 58, TextAnchor.MiddleCenter, color, 0f, 0f);
            NeuroStyle.ClayText(t, 4f, 6f);
            var r = t.rectTransform;
            r.anchorMin = r.anchorMax = new Vector2(0.5f, 0.5f);
            r.sizeDelta = new Vector2(500f, 100f);
            t.horizontalOverflow = HorizontalWrapMode.Overflow;
            t.text = text;
            float e = 0f;
            const float seconds = 0.7f;
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

        // ambiente: ondas de la antena (siempre), luz de la punta y estática de los bordes (urgencia)
        private void AnimateAmbient()
        {
            if (_rings.Count == 0 || _play == null) return;
            float time = GameClock.Time;
            bool calm = GameFeel.ReduceMotion;
            float speed = _streak >= DisparateContract.PerfectStreak ? 1.3f : (_streak >= DisparateContract.ColorWavesStreak ? 1.0f : 0.7f);
            var colors = new[] { NeuroStyle.Sky, NeuroStyle.Lime, NeuroStyle.Sun };
            for (int i = 0; i < _rings.Count; i++)
            {
                float ph = calm ? 0.25f + 0.25f * i : Mathf.Repeat(time * speed * 0.7f + i / (float)_rings.Count, 1f);
                float size = Mathf.Lerp(150f, 640f, ph);
                _rings[i].rectTransform.sizeDelta = new Vector2(size, size);
                _rings[i].rectTransform.anchoredPosition = _dishPos;
                Color c = _streak >= DisparateContract.ColorWavesStreak ? colors[(i + (int)(time * speed)) % colors.Length] : NeuroStyle.Sky;
                float a = (1f - ph) * (calm ? 0.28f : 0.5f) * (_phase == Phase.Idle ? 0.5f : 1f);
                _rings[i].color = NeuroStyle.WithAlpha(c, a);
            }
            bool perfect = _streak >= DisparateContract.PerfectStreak;
            float pulse = calm ? 0.6f : 0.55f + 0.25f * Mathf.Sin(time * 3f);
            _tipGlow.color = NeuroStyle.WithAlpha(NeuroStyle.Sun, (perfect ? 0.9f : 0.35f) * pulse);
            _tipGlow.rectTransform.anchoredPosition = _tipPos;
            if (_stars != null) _stars.Warp = Mathf.Lerp(_stars.Warp, _warpTarget, Mathf.Min(1f, GameClock.DeltaTime * 6f));
            // estática de los bordes: se mueve al azar solo cuando se ve
            if (_staticAlpha > 0.01f && !calm && Time.frameCount % 2 == 0) SetEdgeStatic(_staticAlpha, true);
            else if (_staticAlpha <= 0.01f && _edgeStaticOn) SetEdgeStatic(0f);
        }

        private bool _edgeStaticOn;

        private void SetEdgeStatic(float alpha, bool jitter = false)
        {
            _edgeStaticOn = alpha > 0.01f;
            foreach (var e in _edgeStatic)
            {
                if (e == null) continue;
                if (e.gameObject.activeSelf != _edgeStaticOn) e.gameObject.SetActive(_edgeStaticOn);
                if (!_edgeStaticOn) continue;
                e.color = new Color(1f, 1f, 1f, alpha);
                if (jitter)
                {
                    var r = e.uvRect;
                    e.uvRect = new Rect((float)_rng.NextDouble(), (float)_rng.NextDouble(), r.width, r.height);
                }
            }
        }

        private void SetMeter(int lit, bool pop)
        {
            _lit = lit;
            for (int i = 0; i < _meter.Count; i++)
            {
                bool on = i < lit;
                _meter[i].color = on ? NeuroStyle.Sun : new Color(0.22f, 0.26f, 0.56f, 1f);
                if (pop && i == lit - 1) StartCoroutine(PopRect(_meter[i].rectTransform, 1.4f, 0.25f));
            }
        }

        private void SetSignal(float fraction)
        {
            if (_sigFillRect == null) return;
            _sigFillRect.anchorMax = new Vector2(Mathf.Clamp01(fraction), 1f);
            _sigFillRect.offsetMin = _sigFillRect.offsetMax = Vector2.zero;
            _sigFill.color = fraction > 0.25f ? Color.Lerp(AmberColor, NeuroStyle.Sky, Mathf.Clamp01((fraction - 0.25f) / 0.5f)) : Color.Lerp(BadColor, AmberColor, fraction / 0.25f);
        }

        private void PlayClip(AudioClip clip, float volume)
        {
            if (clip == null || !GameFeel.SoundOn) return;
            _audioSource.PlayOneShot(clip, volume);
        }

        private void UpdateClock()
        {
            if (!Endless || _phase != Phase.Playing || _endsAt <= 0f) return;
            float left = _endsAt - GameClock.Time;
            SetTimerFraction(Mathf.Clamp01(left / DisparateContract.RetoSeconds));
            int whole = Mathf.CeilToInt(left);
            if (whole <= 5 && whole >= 1 && whole != _lastTickSecond)
            {
                _lastTickSecond = whole;
                GameFeel.Tick();
            }
        }

        private void UpdateHud()
        {
            _hud.SetLevel(_dda.PresentedLevel);
            if (Endless) _hud.SetPoints(_points);
            else _hud.SetInfo($"{Mathf.Min(_resolved + 1, DisparateContract.PrecisionSentences)} de {DisparateContract.PrecisionSentences}");
        }

        // ------------------------------------------------------------------ fin

        private IEnumerator FinishGame()
        {
            _phase = Phase.Done;
            _answerable = false;
            _plate.gameObject.SetActive(false);
            _warpTarget = 0f;
            SetEdgeStatic(0f);
            try { PlayerPrefs.SetString(RecentKey, DisparateContract.PushRecent(PlayerPrefs.GetString(RecentKey, ""), _director.UsedInOrder)); PlayerPrefs.Save(); }
            catch (System.Exception) { }

            int total = _tally.Total, correct = _tally.Correct;
            float acc = total > 0 ? (float)correct / total : 0f;
            int score = DisparateContract.Score(acc, _dda.PeakLevel);
            int avgMs = 0;
            if (_tally.HitWordsMs.Count > 0) { long s = 0; foreach (int v in _tally.HitWordsMs) s += v; avgMs = (int)(s / _tally.HitWordsMs.Count); }

            ShowResult(score);

            var telemetry = new StroopTelemetry
            {
                user_id = _config.user_id,
                game_id = _config.game_id,
                session_metrics = new StroopSessionMetrics
                {
                    correct_trials = correct,
                    total_trials = total,
                    calculated_score = score,
                    average_response_time_ms = avgMs,
                    level = _config.config.level,
                    timed = _config.config.timed,
                    end_rating = _dda.RatingNormalized,
                    mode_trials = _dda.ScoredTrials,
                    mode_hits = _dda.ScoredCorrect,
                    peak_level = _dda.PeakLevel,
                    sv_wpm = _tally.Wpm,
                    sv_rt_type = _tally.TypeMeanMs(),
                    sv_hits_type = (int[])_tally.Hits.Clone(),
                    sv_seen_type = (int[])_tally.Seen.Clone(),
                    sv_evident_hits = _tally.EvidentHits,
                    sv_evident_seen = _tally.EvidentSeen,
                    sv_subtle_hits = _tally.SubtleHits,
                    sv_subtle_seen = _tally.SubtleSeen,
                    sv_best_streak = _tally.BestStreak,
                    sv_unclear = _tally.UnclearCsv
                }
            };
            NativeBridge.ForwardTelemetryToPlatform(JsonUtility.ToJson(telemetry));
            yield break;
        }

        private void ShowResult(int score)
        {
            _exit.Show();
            _resultRoot.Find("Title").GetComponent<Text>().text = score >= 85 ? "¡Gran lectura!" : score >= 65 ? "¡Buena transmisión!" : "Misión completada";
            _resultRoot.Find("Detail").GetComponent<Text>().text = $"{_tally.Correct} de {_tally.Total} bien · mejor racha {_tally.BestStreak}";
            int wpm = _tally.Wpm;
            _resultRoot.Find("Extra").GetComponent<Text>().text = wpm >= 0 ? $"{wpm} palabras por minuto leyendo y decidiendo" : "Juega un poco más para medir tu lectura";
            _resultRoot.gameObject.SetActive(true);
            StartCoroutine(AnimateResult(score));
        }

        // ------------------------------------------------------------------ construcción de UI

        protected override void BuildUi()
        {
            var canvasGo = new GameObject("DisparateCanvas");
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
            WorldBackdrop.Build(bgRect, GameWorld.Radio);
            _stars = bgRect.GetComponentInChildren<StarfieldFx>();

            var safeGo = new GameObject("SafeAreaContent");
            safeGo.transform.SetParent(canvasGo.transform, false);
            _safe = safeGo.AddComponent<RectTransform>();
            ApplySafeArea(_safe);

            _hud = new GameHud(_safe, "¿Verdad o disparate?", MarginU, this);
            BuildTimer();

            var playGo = new GameObject("Play");
            playGo.transform.SetParent(_safe, false);
            _play = playGo.AddComponent<RectTransform>();
            Stretch(_play);

            BuildAurora();
            BuildAntenna();
            BuildPlate();
            BuildSignalBar();
            BuildButtons();

            _hint = CenteredText(_play, "Hint", 56, new Color(1f, 1f, 1f, 0.95f));
            NeuroStyle.ClayText(_hint, 3f, 4f);
            _hint.gameObject.SetActive(false);

            _measure = MakeText(_play, "Measure", 72, TextAnchor.UpperLeft, Color.white, 0f, 0f);
            _measure.font = UiFonts.Word;
            _measure.color = new Color(0f, 0f, 0f, 0f);

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

        private void BuildAurora()
        {
            _auroraLayer = Layer(_play, "Aurora");
            var cols = new[] { NeuroStyle.Lime, NeuroStyle.Sky, NeuroStyle.Grape };
            for (int i = 0; i < 3; i++)
            {
                var img = NewImage(_auroraLayer, "Band", RadialGlowSprite.Get());
                img.color = NeuroStyle.WithAlpha(cols[i], 0f);
                img.rectTransform.sizeDelta = new Vector2(1500f, 360f);
                img.rectTransform.anchoredPosition = new Vector2(0f, 300f - i * 190f);
                _aurora.Add(img);
            }
        }

        private void BuildAntenna()
        {
            _antenna = Layer(_play, "AntennaGroup");
            for (int i = 0; i < 3; i++)
            {
                var ring = NewImage(_antenna, "Wave", RingSprite.Get());
                ring.color = NeuroStyle.WithAlpha(NeuroStyle.Sky, 0f);
                ring.gameObject.SetActive(true);
                _rings.Add(ring);
            }
            _tipGlow = NewImage(_antenna, "TipGlow", RadialGlowSprite.Get());
            _tipGlow.rectTransform.sizeDelta = new Vector2(240f, 240f);
            _tipGlow.gameObject.SetActive(true);
            var art = NewImage(_antenna, "Antenna", DisparateSprites.Antenna());
            art.rectTransform.sizeDelta = new Vector2(420f, 420f);
            art.gameObject.SetActive(true);
            _antennaArt = art.rectTransform;
            // el medidor de señal (la racha): 5 barras que se encienden
            for (int i = 0; i < 5; i++)
            {
                var bar = NewImage(_antenna, "Meter", RoundedRectSprite.Get(12));
                bar.type = Image.Type.Sliced;
                bar.rectTransform.sizeDelta = new Vector2(34f, 50f + 26f * i);
                bar.rectTransform.pivot = new Vector2(0.5f, 0f);
                NeuroStyle.ClayFrame(bar, 3f, 5f);
                bar.gameObject.SetActive(true);
                _meter.Add(bar);
            }
            _ribbon = NewImage(_play, "Ribbon", DisparateSprites.Band());
        }

        private RectTransform _antennaArt;

        private void BuildPlate()
        {
            var plateGo = new GameObject("Plate");
            plateGo.transform.SetParent(_play, false);
            _plate = plateGo.AddComponent<RectTransform>();
            _plate.anchorMin = _plate.anchorMax = new Vector2(0.5f, 0.5f);
            _plate.pivot = new Vector2(0.5f, 0.5f);
            _plateGroup = plateGo.AddComponent<CanvasGroup>();
            _plateGroup.blocksRaycasts = false;
            _plateGroup.interactable = false;

            _plateGlow = NewImage(_plate, "Glow", RadialGlowSprite.Get());
            _plateGlow.color = new Color(1f, 1f, 1f, 0f);
            _plateGlow.gameObject.SetActive(true);

            _plateImg = plateGo.AddComponent<Image>();
            _plateImg.sprite = RoundedRectSprite.Get(28);
            _plateImg.type = Image.Type.Sliced;
            _plateImg.color = PlateColor;
            _plateImg.raycastTarget = false;
            NeuroStyle.ClayFrame(_plateImg, 5f, 9f);

            _label = MakeText(_plate, "Phrase", 72, TextAnchor.MiddleCenter, NeuroStyle.Ink, 0f, 0f);
            _label.font = UiFonts.Word;
            _label.horizontalOverflow = HorizontalWrapMode.Wrap;
            _label.verticalOverflow = VerticalWrapMode.Overflow;
            _label.lineSpacing = 1.05f;
            CenterRect(_label.rectTransform);
            _fix = MakeText(_plate, "Fix", 72, TextAnchor.MiddleCenter, NeuroStyle.Ink, 0f, 0f);
            _fix.font = UiFonts.Word;
            _fix.horizontalOverflow = HorizontalWrapMode.Wrap;
            _fix.verticalOverflow = VerticalWrapMode.Overflow;
            _fix.lineSpacing = 1.05f;
            CenterRect(_fix.rectTransform);

            _mark = NewImage(_plate, "Mark", AnswerMarkSprite.Check());
            _mark.rectTransform.sizeDelta = new Vector2(104f, 104f);

            // estática de los bordes (4 tiras): fuera de las letras
            for (int i = 0; i < 4; i++)
            {
                var go = new GameObject("EdgeStatic");
                go.transform.SetParent(_plate, false);
                var r = go.AddComponent<RectTransform>();
                r.anchorMin = r.anchorMax = new Vector2(0.5f, 0.5f);
                r.pivot = new Vector2(0.5f, 0.5f);
                var raw = go.AddComponent<RawImage>();
                raw.texture = DisparateSprites.NoiseTexture();
                raw.raycastTarget = false;
                raw.color = new Color(1f, 1f, 1f, 0f);
                go.SetActive(false);
                _edgeStatic.Add(raw);
            }

            // rótulos del deslizar
            _swipeTrue = SwipeLabel("VERDAD", GoodColor);
            _swipeFalse = SwipeLabel("DISPARATE", BadColor);
        }

        private Text SwipeLabel(string text, Color color)
        {
            var t = MakeText(_play, "Swipe" + text, 66, TextAnchor.MiddleCenter, color, 0f, 0f);
            NeuroStyle.ClayText(t, 4f, 6f);
            var r = t.rectTransform;
            r.anchorMin = r.anchorMax = new Vector2(0.5f, 0.5f);
            r.sizeDelta = new Vector2(420f, 100f);
            t.horizontalOverflow = HorizontalWrapMode.Overflow;
            t.text = text;
            t.gameObject.SetActive(false);
            return t;
        }

        private static void CenterRect(RectTransform r)
        {
            r.anchorMin = r.anchorMax = new Vector2(0.5f, 0.5f);
            r.pivot = new Vector2(0.5f, 0.5f);
            r.anchoredPosition = Vector2.zero;
        }

        private void BuildSignalBar()
        {
            var go = new GameObject("SignalBar");
            go.transform.SetParent(_play, false);
            _sigBar = go.AddComponent<RectTransform>();
            _sigBar.anchorMin = _sigBar.anchorMax = new Vector2(0.5f, 0.5f);
            _sigBar.pivot = new Vector2(0.5f, 0.5f);
            _sigBar.sizeDelta = new Vector2(760f, 22f);
            var bgImg = go.AddComponent<Image>();
            bgImg.sprite = RoundedRectSprite.Get(11);
            bgImg.type = Image.Type.Sliced;
            bgImg.color = new Color(1f, 1f, 1f, 0.14f);
            bgImg.raycastTarget = false;

            var fillGo = new GameObject("Fill");
            fillGo.transform.SetParent(go.transform, false);
            _sigFillRect = fillGo.AddComponent<RectTransform>();
            _sigFillRect.anchorMin = Vector2.zero;
            _sigFillRect.anchorMax = Vector2.one;
            _sigFillRect.offsetMin = _sigFillRect.offsetMax = Vector2.zero;
            _sigFill = fillGo.AddComponent<Image>();
            _sigFill.sprite = RoundedRectSprite.Get(11);
            _sigFill.type = Image.Type.Sliced;
            _sigFill.raycastTarget = false;
            _sigFill.color = NeuroStyle.Sky;

            _sigLabel = MakeText(_play, "SignalLabel", 44, TextAnchor.MiddleRight, new Color(0.84f, 0.82f, 0.96f, 1f), 0f, 0f);
            _sigLabel.font = UiFonts.Regular;
            _sigLabel.text = "señal";
            var lr = _sigLabel.rectTransform;
            lr.anchorMin = lr.anchorMax = new Vector2(0.5f, 0.5f);
            lr.sizeDelta = new Vector2(180f, 60f);
            lr.pivot = new Vector2(1f, 0.5f);
            _sigLabel.horizontalOverflow = HorizontalWrapMode.Overflow;
        }

        private void BuildButtons()
        {
            _btnTrue = MakeButton("VERDAD", GoodColor, DisparateSprites.CheckIcon());
            _btnFalse = MakeButton("DISPARATE", BadColor, DisparateSprites.BrokenWaveIcon());
        }

        private RectTransform MakeButton(string label, Color color, Sprite icon)
        {
            var go = new GameObject("Button" + label);
            go.transform.SetParent(_play, false);
            var r = go.AddComponent<RectTransform>();
            r.anchorMin = r.anchorMax = new Vector2(0.5f, 0.5f);
            r.pivot = new Vector2(0.5f, 0.5f);
            var img = go.AddComponent<Image>();
            img.sprite = RoundedRectSprite.Get(64);
            img.type = Image.Type.Sliced;
            img.color = color;
            img.raycastTarget = false;
            NeuroStyle.ClayFrame(img, 6f, 14f);
            var ic = NewImage(r, "Icon", icon);
            ic.rectTransform.sizeDelta = new Vector2(120f, 120f);
            ic.rectTransform.anchoredPosition = new Vector2(0f, 46f);
            ic.gameObject.SetActive(true);
            var t = MakeText(r, "Label", 62, TextAnchor.MiddleCenter, NeuroStyle.Ink, 0f, 0f);
            var tr = t.rectTransform;
            tr.anchorMin = tr.anchorMax = new Vector2(0.5f, 0.5f);
            tr.sizeDelta = new Vector2(480f, 90f);
            tr.anchoredPosition = new Vector2(0f, -58f);
            t.horizontalOverflow = HorizontalWrapMode.Overflow;
            t.text = label;
            return r;
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
            AddResultText("Detail", 44, new Vector2(0f, -150f), new Color(1f, 1f, 1f, 0.85f));
            var extra = AddResultText("Extra", 40, new Vector2(0f, -250f), new Color(1f, 1f, 1f, 0.65f));
            extra.horizontalOverflow = HorizontalWrapMode.Wrap;
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
            float top = _playH * 0.5f;
            float bottom = -_playH * 0.5f;

            // antena arriba, a la izquierda del centro; el medidor de señal a su derecha
            const float antSize = 420f;
            var antCenter = new Vector2(-150f, top - (GameHud.Height + 40f + antSize * 0.5f));
            _antennaArt.anchoredPosition = antCenter;
            _dishPos = antCenter + new Vector2((DisparateSprites.AntennaDish.x - 0.5f) * antSize, (DisparateSprites.AntennaDish.y - 0.5f) * antSize);
            _tipPos = antCenter + new Vector2((DisparateSprites.AntennaTip.x - 0.5f) * antSize, (DisparateSprites.AntennaTip.y - 0.5f) * antSize);
            float floorY = antCenter.y + (DisparateSprites.AntennaFloor - 0.5f) * antSize;
            for (int i = 0; i < _meter.Count; i++)
                _meter[i].rectTransform.anchoredPosition = new Vector2(antCenter.x + 150f + i * 46f, floorY + 70f);

            // la placa va al medio; la barra de señal, el aviso y los botones, abajo
            _plateY = antCenter.y - antSize * 0.5f - 130f;
            _sigY = _plateY - 270f;
            _sigBar.anchoredPosition = new Vector2(80f, _sigY);
            _sigLabel.rectTransform.anchoredPosition = new Vector2(-300f, _sigY);
            _hint.rectTransform.sizeDelta = new Vector2(_playW - MarginU * 2f, 160f);
            _hint.rectTransform.anchoredPosition = new Vector2(0f, _sigY - 190f);

            float btnH = Mathf.Max(280f, DisparateContract.MinButtonDp * UnitsPerDp * 1.6f);
            float btnW = (_playW - MarginU * 2f - 36f) * 0.5f;
            _btnSize = new Vector2(btnW, btnH);
            float by = bottom + 70f + btnH * 0.5f;
            _btnTrueCenter = new Vector2(-(btnW * 0.5f + 18f), by);
            _btnFalseCenter = new Vector2(btnW * 0.5f + 18f, by);
            _btnTrue.sizeDelta = _btnFalse.sizeDelta = _btnSize;
            _btnTrue.anchoredPosition = _btnTrueCenter;
            _btnFalse.anchoredPosition = _btnFalseCenter;

            _plate.anchoredPosition = new Vector2(0f, _plateY);
            _ribbon.rectTransform.anchoredPosition = _dishPos;
            AnimateAmbient();
        }
    }
}
