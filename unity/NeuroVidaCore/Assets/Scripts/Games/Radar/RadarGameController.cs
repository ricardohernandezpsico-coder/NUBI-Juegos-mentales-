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

namespace NeuroVida.Games.Radar
{
    /// <summary>
    /// "Radar": juego estrella de velocidad de procesamiento (ver <see cref="RadarContract"/>). Eres quien opera el
    /// radar de rescate: en un destello aparece una nave en la pantalla central y un astronauta perdido en algún lugar
    /// del radar (desde el nivel 4, entre asteroides). Una interferencia borra la imagen y hay que responder:
    /// <list type="number">
    /// <item>¿Qué nave pasó por el centro? (dos opciones)</item>
    /// <item>¿Dónde estaba el astronauta? (tocar su dirección en el radar)</item>
    /// </list>
    /// Las dos bien = astronauta rescatado (vuela a la fila de rescatados). El destello se acorta con el DDA común.
    /// Medidas propias: "tu vistazo" (ms en que se asentó la escalera) y el mapa de aciertos por dirección, que la app
    /// dibuja como "tu radar". Reto = 90 s; Precisión = 20 destellos sin reloj.
    /// </summary>
    public class RadarGameController : GameControllerBase
    {
        public const string GameId = RadarContract.GameId;

        private const float UnitsPerDp = 3f;
        private const float MarginU = 60f;
        private const int CrewIcons = 10;

        private static readonly Color GoodColor = NeuroStyle.Lime;
        private static readonly Color BadColor = NeuroStyle.Coral;
        private static readonly Color AmberColor = NeuroStyle.Sun;
        private static readonly Color PhosphorColor = NeuroStyle.Lime;

        private enum Phase { Idle, Watch, AnswerCenter, AnswerDirection, Feedback, Done }

        private System.Random _rng;
        private AdaptiveDifficulty _dda;
        private Phase _phase = Phase.Idle;
        private bool Endless => _config != null && _config.config.timed;
        private bool Precision => !Endless;
        private int _previousFrameRate;

        // ensayo en curso
        private RadarTrial _trial;
        private int _trialSeed; // corrimiento visual fijo del ensayo (el astronauta no se mueve al mostrar la respuesta)
        private bool _optionAIsCenter;
        private int _centerAnswer = -1; // 0 = opción izquierda, 1 = derecha
        private int _directionAnswer = -1;
        private float _answerStart;

        // sesión
        private readonly List<float> _exposures = new List<float>();
        private readonly int[] _sectorHits = new int[RadarContract.Directions];
        private readonly int[] _sectorTrials = new int[RadarContract.Directions];
        private int _trials, _full, _centerOk, _rescued;
        private int _streak, _bestStreak, _points;
        private long _rtSum;
        private int _rtCount;
        private float _startedAt, _endsAt;
        private int _lastTickSecond = -1;
        private float _sweepAngle;

        // UI
        private RectTransform _safe, _fxRect, _scopeRect, _stimRoot, _timerBg, _timerFill, _crewRow;
        private Image _scope, _sweep, _mask, _centerIcon, _centerMark, _helmet, _helmetGlow, _fixation;
        private Text _prompt, _hint, _crewLabel;
        private readonly List<Image> _asteroids = new List<Image>();
        private readonly List<Image> _pads = new List<Image>();
        private readonly List<Image> _padMarks = new List<Image>();
        private readonly List<Image> _crew = new List<Image>();
        private readonly RectTransform[] _optionRect = new RectTransform[2];
        private readonly Image[] _optionIcon = new Image[2];
        private readonly Image[] _optionMark = new Image[2];
        private readonly Button[] _optionButton = new Button[2];
        private Toast _toast;
        private ExitButton _exit;
        private GameHud _hud;
        private CountdownScreen _countdown;
        private float _glassR, _scopeSize;

        // ------------------------------------------------------------------ sesión

        public void StartSession(SequenceInitConfig config)
        {
            _config = config;
            _rng = new System.Random();
            var age = DdaUserProfileConfig.ParseAgeBand(config.config.age_band);
            float start = AdaptiveDifficulty.StartRating(config.config, RadarContract.MaxLevel);
            // Pocos ensayos por partida (~25): pasos más grandes. Sin tiempo de reacción: acá cuenta lo que se ve, no
            // lo rápido que se responde.
            _dda = new AdaptiveDifficulty(RadarContract.MaxLevel, age, start, stepUp: 0.3f, useReaction: false);

            // Los destellos son de decenas de milisegundos: a 30 cuadros por segundo (lo que Android da por defecto)
            // el más breve duraría 33 ms como mínimo y los saltos serían gruesos. Se pide 60 mientras dura el juego.
            _previousFrameRate = Application.targetFrameRate;
            Application.targetFrameRate = 60;

            _phase = Phase.Idle;
            _exposures.Clear();
            for (int d = 0; d < RadarContract.Directions; d++) _sectorHits[d] = _sectorTrials[d] = 0;
            _trials = _full = _centerOk = _rescued = 0;
            _streak = _bestStreak = _points = 0;
            _rtSum = 0;
            _rtCount = 0;
            _lastTickSecond = -1;
            _endsAt = 0f;

            _resultRoot.gameObject.SetActive(false);
            _exit.Hide();
            _timerBg.gameObject.SetActive(Endless);
            _hud.SetStreak(0);
            foreach (var c in _crew) c.gameObject.SetActive(false);
            _crewLabel.text = "Rescatados: 0";
            HideStimuli();
            HideAnswers();
            _mask.gameObject.SetActive(false);

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
            yield return StartCoroutine(_countdown.Play("Radar", Assessment.Subtitle("Prepárate"), () => _safe.gameObject.SetActive(true)));
            _safe.gameObject.SetActive(true);
            yield return null;
            ApplySafeArea(_safe);
            Canvas.ForceUpdateCanvases();
            Layout();
            UpdateHud();

            // Primera vez: qué va a pasar, antes del primer destello (lento).
            SetPrompt("Un destello: mira el centro", Color.white);
            _hint.text = "Recuerda la nave del centro y dónde aparece el astronauta.";
            _hint.gameObject.SetActive(true);
            yield return StartCoroutine(Wait(2.4f));
            _hint.gameObject.SetActive(false);

            _startedAt = GameClock.Time;
            _endsAt = _startedAt + RadarContract.RetoSeconds;
            while (!Finished())
                yield return StartCoroutine(RunTrial());
            yield return StartCoroutine(FinishGame());
        }

        private bool Finished() => Precision ? _trials >= RadarContract.PrecisionTrials : GameClock.Time >= _endsAt;

        // ------------------------------------------------------------------ un ensayo

        private IEnumerator RunTrial()
        {
            _trial = RadarContract.NextTrial(_dda.PresentedLevel, _rng);
            _trialSeed = _rng.Next(100000);
            _centerAnswer = _directionAnswer = -1;

            // 1. Preparados: la mira del centro late (dónde poner la vista).
            _phase = Phase.Watch;
            HideAnswers();
            SetPrompt("Mira el centro", Color.white);
            _fixation.gameObject.SetActive(true);
            float t = 0f;
            float ready = 0.75f + 0.25f * (float)_rng.NextDouble(); // un poco variable: que no se pueda anticipar
            while (t < ready)
            {
                t += GameClock.DeltaTime;
                float k = Mathf.Clamp01(t / ready);
                _fixation.rectTransform.localScale = Vector3.one * (1.25f - 0.25f * UiFx.EaseOutCubic(k));
                _fixation.color = NeuroStyle.WithAlpha(AmberColor, 0.5f + 0.5f * k);
                yield return null;
            }
            _fixation.gameObject.SetActive(false);

            // 2. Destello: centro + astronauta (+ asteroides). El haz se apaga para no tapar nada.
            ShowStimuli(_trial);
            _sweep.gameObject.SetActive(false);
            PlayTone(1318f, 0.06f, 0.07f);
            float shownAt = Time.unscaledTime;
            float startClock = GameClock.Time;
            float exposure = _trial.ExposureMs / 1000f;
            bool paused = false;
            // Se apaga en el cuadro más cercano a la duración pedida (a 60 cuadros por segundo, pasos de ~17 ms).
            while (GameClock.Time - startClock + Time.unscaledDeltaTime * 0.5f < exposure)
            {
                if (GameClock.Paused) paused = true;
                yield return null;
            }
            float actualMs = (Time.unscaledTime - shownAt) * 1000f;
            HideStimuli();

            // 3. Interferencia (máscara): borra la imagen que queda en la retina.
            _mask.sprite = RadarSprites.Mask(_trials);
            _mask.rectTransform.localRotation = Quaternion.Euler(0f, 0f, (float)_rng.NextDouble() * 360f);
            _mask.color = Color.white;
            _mask.gameObject.SetActive(true);
            PlayTone(233f, 0.12f, 0.05f);
            yield return StartCoroutine(Wait(0.35f));
            yield return StartCoroutine(FadeMask(0.15f));

            if (paused || GameClock.Paused)
            {
                // Una pausa en pleno destello lo invalida: se repite sin contar.
                _sweep.gameObject.SetActive(true);
                _toast.Show("Otra vez", "La pausa cortó el destello", AmberColor, 0.9f);
                yield return StartCoroutine(Wait(0.6f));
                yield break;
            }

            // 4. ¿Qué pasó por el centro?
            _phase = Phase.AnswerCenter;
            _sweep.gameObject.SetActive(true);
            _optionAIsCenter = _rng.NextDouble() < 0.5;
            SetOption(0, _optionAIsCenter ? _trial.CenterShape : _trial.DecoyShape, _optionAIsCenter ? _trial.CenterVariant : _trial.DecoyVariant);
            SetOption(1, _optionAIsCenter ? _trial.DecoyShape : _trial.CenterShape, _optionAIsCenter ? _trial.DecoyVariant : _trial.CenterVariant);
            SetPrompt("¿Qué pasó por el centro?", Color.white);
            ShowOptions();
            _answerStart = GameClock.Time;
            while (_centerAnswer < 0) yield return null;
            _rtSum += (long)((GameClock.Time - _answerStart) * 1000f);
            _rtCount++;
            bool centerOk = (_centerAnswer == 0) == _optionAIsCenter;

            // 5. ¿Dónde estaba el astronauta?
            _phase = Phase.AnswerDirection;
            SetPrompt("¿Dónde estaba el astronauta?", Color.white);
            _hint.text = "Toca su dirección en el radar";
            _hint.gameObject.SetActive(true);
            for (int i = 0; i < _optionRect.Length; i++) _optionButton[i].interactable = false;
            yield return StartCoroutine(ShowPads());
            while (_directionAnswer < 0) yield return null;
            _hint.gameObject.SetActive(false);
            bool locationOk = _directionAnswer == _trial.Direction;

            // 6. Resultado del ensayo.
            _phase = Phase.Feedback;
            yield return StartCoroutine(Resolve(centerOk, locationOk, actualMs));
        }

        private IEnumerator Resolve(bool centerOk, bool locationOk, float actualMs)
        {
            bool full = centerOk && locationOk;
            _trials++;
            _exposures.Add(actualMs);
            _sectorTrials[_trial.Direction]++;
            if (locationOk) _sectorHits[_trial.Direction]++;
            if (centerOk) _centerOk++;
            if (full) _full++;
            _streak = full ? _streak + 1 : 0;
            _bestStreak = Mathf.Max(_bestStreak, _streak);
            int pts = RadarContract.Points(centerOk, locationOk, _trial.Level, _streak);
            _points += pts;
            _hud.SetStreak(_streak);

            // Marcas: la opción elegida (✓/✗), la dirección tocada (✓/✗) y la verdad a la vista (nave en el centro,
            // astronauta en su lugar).
            int chosen = _centerAnswer;
            int right = _optionAIsCenter ? 0 : 1;
            _optionMark[chosen].sprite = centerOk ? AnswerMarkSprite.Check() : AnswerMarkSprite.Cross();
            _optionMark[chosen].gameObject.SetActive(true);
            if (!centerOk)
            {
                // La correcta vuelve a verse entera y salta: así queda claro cuál era.
                _optionRect[right].GetComponent<CanvasGroup>().alpha = 1f;
                StartCoroutine(PopRect(_optionRect[right], 1.1f, 0.3f));
            }

            for (int d = 0; d < _pads.Count; d++)
                _pads[d].gameObject.SetActive(d == _directionAnswer);
            var pm = _padMarks[_directionAnswer];
            pm.sprite = locationOk ? AnswerMarkSprite.Check() : AnswerMarkSprite.Cross();
            pm.gameObject.SetActive(true);
            _pads[_directionAnswer].color = locationOk ? Color.white : new Color(1f, 0.62f, 0.56f, 1f);

            _centerIcon.sprite = SymbolSprite.Get((ShapeKind)_trial.CenterShape, _trial.CenterVariant);
            _centerIcon.color = NeuroStyle.WithAlpha(Color.white, 0.9f);
            _centerIcon.gameObject.SetActive(true);
            _centerMark.sprite = centerOk ? AnswerMarkSprite.Check() : AnswerMarkSprite.Cross();
            _centerMark.gameObject.SetActive(true);
            PlaceHelmet(_trial);
            _helmet.gameObject.SetActive(true);
            _helmetGlow.gameObject.SetActive(true);
            _helmetGlow.color = NeuroStyle.WithAlpha(locationOk ? GoodColor : AmberColor, 0.55f);
            StartCoroutine(UiFx.RingBurst(_stimRoot, _helmet.rectTransform.anchoredPosition, locationOk ? GoodColor : AmberColor, 90f, 260f, 0.5f));

            var change = _dda.Register(full);
            if (full)
            {
                GameFeel.Correct(_streak);
                _rescued++;
                SetPrompt(_streak >= 3 ? $"¡Rescatado! · racha {_streak}" : "¡Rescatado!", GoodColor);
                StartCoroutine(FloatText(_helmet.rectTransform, "+" + pts, NeuroStyle.Sun));
                StartCoroutine(UiFx.SparkBurst(_stimRoot, _helmet.rectTransform.anchoredPosition, NeuroStyle.Sun, 14, 200f, 36f, 0.5f));
            }
            else
            {
                GameFeel.Wrong();
                SetPrompt(!centerOk && !locationOk ? "Se escapó esta vez" : !centerOk ? "La nave era otra" : "Estaba " + RadarContract.DirectionName(_trial.Direction), AmberColor);
                if (pts > 0) StartCoroutine(FloatText(_helmet.rectTransform, "+" + pts, NeuroStyle.Sun));
            }

            if (change == DdaChange.Up)
            {
                _toast.Show("Destello más breve", LevelNews(_dda.Level), GoodColor, 1.0f);
                GameFeel.LevelUp();
            }
            else if (change == DdaChange.Down || _dda.Struggling)
                _toast.Show("Con calma", "Destello más largo", AmberColor, 0.9f);
            UpdateHud();

            yield return StartCoroutine(Wait(full ? 0.55f : 0.95f));
            if (full) yield return StartCoroutine(FlyToCrew());
            HideStimuli();
            HideAnswers();
        }

        /// <summary>Qué trae el nivel nuevo (solo cuando cambia algo además del destello).</summary>
        private static string LevelNews(int level)
        {
            if (RadarContract.DistractorCount(level) > RadarContract.DistractorCount(level - 1)) return "Llegan asteroides";
            if (RadarContract.MaxRing(level) > RadarContract.MaxRing(level - 1)) return "El astronauta se aleja";
            if (RadarContract.CenterTier(level) > RadarContract.CenterTier(level - 1)) return "Naves más parecidas";
            return $"{RadarContract.ExposureMs(level)} ms";
        }

        // ------------------------------------------------------------------ entrada

        private void OnOption(int index)
        {
            if (_phase != Phase.AnswerCenter || _centerAnswer >= 0) return;
            _centerAnswer = index;
            GameFeel.Haptic(GameFeel.HapticKind.Light);
            // La otra opción se apaga: queda a la vista lo que se eligió.
            _optionRect[1 - index].GetComponent<CanvasGroup>().alpha = 0.35f;
        }

        private void Update()
        {
            // Haz del radar: gira siempre (salvo en el destello, donde está apagado).
            float dt = GameClock.DeltaTime;
            if (_sweep != null && _sweep.gameObject.activeSelf && dt > 0f)
            {
                _sweepAngle = Mathf.Repeat(_sweepAngle - 140f * dt, 360f);
                _sweep.rectTransform.localRotation = Quaternion.Euler(0f, 0f, _sweepAngle);
            }
            UpdateClock();

            if (_phase != Phase.AnswerDirection || _directionAnswer >= 0 || dt <= 0f) return;
            // Tocar en cualquier parte del radar elige la dirección más cercana (no hace falta acertarle al botón).
            if (!Input.GetMouseButtonDown(0)) return;
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(_scopeRect, Input.mousePosition, null, out var local)) return;
            float x = local.x / _glassR, y = local.y / _glassR;
            if (!RadarContract.IsDirectionTap(x, y)) return;
            _directionAnswer = RadarContract.DirectionFromOffset(x, y);
            GameFeel.Haptic(GameFeel.HapticKind.Light);
            StartCoroutine(PopRect(_pads[_directionAnswer].rectTransform, 1.2f, 0.2f));
        }

        private void UpdateClock()
        {
            if (!Endless || _phase == Phase.Idle || _phase == Phase.Done || _endsAt <= 0f) return;
            float left = _endsAt - GameClock.Time;
            SetTimerFraction(Mathf.Clamp01(left / RadarContract.RetoSeconds));
            int whole = Mathf.CeilToInt(left);
            if (whole <= 5 && whole >= 1 && whole != _lastTickSecond)
            {
                _lastTickSecond = whole;
                GameFeel.Tick();
            }
        }

        // ------------------------------------------------------------------ estímulos

        private void ShowStimuli(RadarTrial t)
        {
            _centerIcon.sprite = SymbolSprite.Get((ShapeKind)t.CenterShape, t.CenterVariant);
            _centerIcon.color = Color.white;
            _centerIcon.gameObject.SetActive(true);
            PlaceHelmet(t);
            _helmet.gameObject.SetActive(true);
            for (int i = 0; i < _asteroids.Count; i++)
            {
                var a = _asteroids[i];
                if (i >= t.Distractors.Length)
                {
                    a.gameObject.SetActive(false);
                    continue;
                }
                int slot = t.Distractors[i];
                a.rectTransform.anchoredPosition = SlotPosition(slot % RadarContract.Directions, slot / RadarContract.Directions, jitterSeed: slot + _trialSeed);
                a.rectTransform.localRotation = Quaternion.Euler(0f, 0f, _rng.Next(360));
                a.sprite = SymbolSprite.Get(ShapeKind.Asteroid, _rng.Next(3));
                a.gameObject.SetActive(true);
            }
        }

        private void PlaceHelmet(RadarTrial t)
        {
            _helmet.rectTransform.anchoredPosition = SlotPosition(t.Direction, t.Ring, jitterSeed: t.Ring * RadarContract.Directions + t.Direction + _trialSeed);
            _helmetGlow.rectTransform.anchoredPosition = _helmet.rectTransform.anchoredPosition;
        }

        /// <summary>Centro de una casilla en el radar, con un pequeño corrimiento (fijo por ensayo) para que no se vea
        /// como una grilla.</summary>
        private Vector2 SlotPosition(int direction, int ring, int jitterSeed)
        {
            RadarContract.Position(direction, ring, out float x, out float y);
            var j = new System.Random(jitterSeed);
            float jx = ((float)j.NextDouble() - 0.5f) * 0.05f, jy = ((float)j.NextDouble() - 0.5f) * 0.05f;
            return new Vector2((x + jx) * _glassR, (y + jy) * _glassR);
        }

        private void HideStimuli()
        {
            _centerIcon.gameObject.SetActive(false);
            _centerMark.gameObject.SetActive(false);
            _helmet.gameObject.SetActive(false);
            _helmetGlow.gameObject.SetActive(false);
            foreach (var a in _asteroids) a.gameObject.SetActive(false);
        }

        private IEnumerator FadeMask(float seconds)
        {
            float t = 0f;
            while (t < seconds)
            {
                t += GameClock.DeltaTime;
                _mask.color = NeuroStyle.WithAlpha(Color.white, 1f - Mathf.Clamp01(t / seconds));
                yield return null;
            }
            _mask.gameObject.SetActive(false);
        }

        // ------------------------------------------------------------------ respuestas

        private void SetOption(int i, int shape, int variant)
        {
            _optionIcon[i].sprite = SymbolSprite.Get((ShapeKind)shape, variant);
            _optionMark[i].gameObject.SetActive(false);
            _optionRect[i].GetComponent<CanvasGroup>().alpha = 1f;
            _optionButton[i].interactable = true;
        }

        private void ShowOptions()
        {
            for (int i = 0; i < 2; i++)
            {
                _optionRect[i].gameObject.SetActive(true);
                StartCoroutine(PopIn(_optionRect[i], 0.22f));
            }
        }

        private IEnumerator ShowPads()
        {
            for (int d = 0; d < _pads.Count; d++)
            {
                var p = _pads[d];
                p.color = Color.white;
                _padMarks[d].gameObject.SetActive(false);
                p.gameObject.SetActive(true);
                StartCoroutine(PopIn(p.rectTransform, 0.18f));
            }
            yield return null;
        }

        private void HideAnswers()
        {
            for (int i = 0; i < 2; i++)
                if (_optionRect[i] != null) _optionRect[i].gameObject.SetActive(false);
            foreach (var p in _pads) p.gameObject.SetActive(false);
            foreach (var m in _padMarks) m.gameObject.SetActive(false);
            if (_hint != null) _hint.gameObject.SetActive(false);
        }

        private void SetPrompt(string text, Color color)
        {
            _prompt.text = text;
            _prompt.color = color;
        }

        // ------------------------------------------------------------------ efectos

        /// <summary>El astronauta rescatado vuela a la fila de rescatados de abajo.</summary>
        private IEnumerator FlyToCrew()
        {
            int slot = Mathf.Min(_rescued, CrewIcons) - 1;
            var target = _crew[Mathf.Max(0, slot)].rectTransform;
            var from = LocalIn(_fxRect, _helmet.rectTransform);
            var to = LocalIn(_fxRect, target);
            _helmet.gameObject.SetActive(false);
            _helmetGlow.gameObject.SetActive(false);

            var go = new GameObject("Flying");
            go.transform.SetParent(_fxRect, false);
            var r = go.AddComponent<RectTransform>();
            r.anchorMin = r.anchorMax = new Vector2(0.5f, 0.5f);
            var img = go.AddComponent<Image>();
            img.sprite = SymbolSprite.Get(ShapeKind.Helmet, 1);
            img.raycastTarget = false;
            float t = 0f;
            const float seconds = 0.45f;
            float fromSize = _helmet.rectTransform.sizeDelta.x, toSize = target.sizeDelta.x;
            while (t < seconds)
            {
                t += GameClock.DeltaTime;
                float k = UiFx.EaseOutCubic(Mathf.Clamp01(t / seconds));
                // Arco hacia arriba en el camino, como si lo levantara el rayo de rescate.
                r.anchoredPosition = Vector2.Lerp(from, to, k) + new Vector2(0f, Mathf.Sin(k * Mathf.PI) * 120f);
                float s = Mathf.Lerp(fromSize, toSize, k);
                r.sizeDelta = new Vector2(s, s);
                yield return null;
            }
            Destroy(go);
            _crewLabel.text = $"Rescatados: {_rescued}";
            if (slot >= 0)
            {
                _crew[slot].gameObject.SetActive(true);
                StartCoroutine(PopRect(_crew[slot].rectTransform, 1.3f, 0.25f));
            }
            if (_rescued % 10 == 0)
            {
                _toast.Show($"¡{_rescued} rescatados!", "Escuadrón completo", NeuroStyle.Sun, 1.0f);
                GameFeel.LevelUp();
            }
        }

        private IEnumerator FloatText(RectTransform anchor, string text, Color color)
        {
            var t = MakeText(_fxRect, "Float", 58, TextAnchor.MiddleCenter, color, 0f, 0f);
            NeuroStyle.ClayText(t, 4f, 6f);
            var r = t.rectTransform;
            r.anchorMin = r.anchorMax = new Vector2(0.5f, 0.5f);
            r.sizeDelta = new Vector2(300f, 90f);
            t.text = text;
            var pos = LocalIn(_fxRect, anchor);
            float e = 0f;
            const float seconds = 0.7f;
            while (e < seconds)
            {
                e += GameClock.DeltaTime;
                float k = Mathf.Clamp01(e / seconds);
                r.anchoredPosition = pos + new Vector2(0f, 70f + 80f * UiFx.EaseOutCubic(k));
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
            HideStimuli();
            HideAnswers();

            float accuracy = _trials > 0 ? (float)_full / _trials : 0f;
            int glance = RadarContract.GlanceMs(_exposures);
            int score = RadarContract.Score(accuracy, glance);
            int avgMs = _rtCount > 0 ? (int)(_rtSum / _rtCount) : 0;

            SetPrompt("Fin del turno", GoodColor);
            ShowResult(score, glance);

            var telemetry = new StroopTelemetry
            {
                user_id = _config.user_id,
                game_id = _config.game_id,
                session_metrics = new StroopSessionMetrics
                {
                    correct_trials = _full,
                    total_trials = _trials,
                    calculated_score = score,
                    average_response_time_ms = avgMs,
                    level = _config.config.level,
                    timed = _config.config.timed,
                    end_rating = _dda.RatingNormalized,
                    mode_trials = _dda.ScoredTrials,
                    mode_hits = _dda.ScoredCorrect,
                    peak_level = _dda.PeakLevel,
                    glance_ms = glance,
                    sector_hits = (int[])_sectorHits.Clone(),
                    sector_trials = (int[])_sectorTrials.Clone()
                }
            };
            NativeBridge.ForwardTelemetryToPlatform(JsonUtility.ToJson(telemetry));
            yield break;
        }

        private void ShowResult(int score, int glance)
        {
            _exit.Show();
            _resultRoot.Find("Title").GetComponent<Text>().text = score >= 85 ? "¡Ojo de radar!" : score >= 65 ? "¡Buen turno!" : "Turno completado";
            _resultRoot.Find("Detail").GetComponent<Text>().text = $"{_rescued} rescatados de {_trials}";
            _resultRoot.Find("Extra").GetComponent<Text>().text = glance > 0 ? $"Tu vistazo: {glance} ms" : $"Mejor racha {_bestStreak}";
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

            var canvasGo = new GameObject("RadarCanvas");
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
            WorldBackdrop.Build(bgRect, GameWorld.RadarStation);

            var safeGo = new GameObject("SafeAreaContent");
            safeGo.transform.SetParent(canvasGo.transform, false);
            _safe = safeGo.AddComponent<RectTransform>();
            ApplySafeArea(_safe);

            _hud = new GameHud(_safe, "Radar", MarginU, this);
            BuildTimer();

            _prompt = MakeText(_safe, "Prompt", 64, TextAnchor.MiddleCenter, Color.white, 0f, 0f);
            NeuroStyle.ClayText(_prompt, 3.5f, 5f);
            var pr = _prompt.rectTransform;
            pr.anchorMin = pr.anchorMax = new Vector2(0.5f, 1f);
            pr.pivot = new Vector2(0.5f, 0.5f);
            BestFit(_prompt, 40);

            BuildScope();
            BuildOptions();
            BuildCrewRow();

            _hint = MakeText(_safe, "Hint", 42, TextAnchor.MiddleCenter, new Color(1f, 1f, 1f, 0.8f), 0f, 0f);
            var hr = _hint.rectTransform;
            hr.anchorMin = hr.anchorMax = new Vector2(0.5f, 1f);
            hr.pivot = new Vector2(0.5f, 0.5f);
            BestFit(_hint, 28);
            _hint.gameObject.SetActive(false);

            var fx = new GameObject("Fx");
            fx.transform.SetParent(_safe, false);
            _fxRect = fx.AddComponent<RectTransform>();
            Stretch(_fxRect);

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

        private void BuildScope()
        {
            var go = new GameObject("Scope");
            go.transform.SetParent(_safe, false);
            _scopeRect = go.AddComponent<RectTransform>();
            _scopeRect.anchorMin = _scopeRect.anchorMax = new Vector2(0.5f, 1f);
            _scopeRect.pivot = new Vector2(0.5f, 0.5f);
            _scope = go.AddComponent<Image>();
            _scope.sprite = RadarSprites.Scope();
            _scope.raycastTarget = false;

            // Haz que barre (debajo de los estímulos).
            _sweep = NewImage(_scopeRect, "Sweep", RadarSprites.Sweep());
            _sweep.color = NeuroStyle.WithAlpha(PhosphorColor, 0.32f);
            _sweep.gameObject.SetActive(true);

            _stimRoot = Layer(_scopeRect, "Stimuli");

            // Mira del centro: un anillo que se cierra antes del destello.
            _fixation = NewImage(_stimRoot, "Fixation", RingSprite.Get());

            for (int i = 0; i < 23; i++) _asteroids.Add(NewImage(_stimRoot, "Asteroid", null));

            _helmetGlow = NewImage(_stimRoot, "HelmetGlow", RadialGlowSprite.Get());
            _helmet = NewImage(_stimRoot, "Helmet", SymbolSprite.Get(ShapeKind.Helmet, 1));
            _helmet.preserveAspect = true;

            _centerIcon = NewImage(_stimRoot, "CenterIcon", null);
            _centerIcon.preserveAspect = true;
            _centerMark = NewImage(_stimRoot, "CenterMark", null);

            // Botones de dirección (visuales: tocar cualquier parte del sector también vale).
            for (int d = 0; d < RadarContract.Directions; d++)
            {
                var pad = NewImage(_stimRoot, "Pad" + d, RadarSprites.Pad());
                pad.rectTransform.localRotation = Quaternion.Euler(0f, 0f, -45f * d);
                _pads.Add(pad);
                var mark = NewImage(_stimRoot, "PadMark" + d, null);
                _padMarks.Add(mark);
            }

            // Interferencia: cubre todo el vidrio.
            _mask = NewImage(_scopeRect, "Mask", RadarSprites.Mask(0));
            Stretch(_mask.rectTransform);
        }

        private void BuildOptions()
        {
            for (int i = 0; i < 2; i++)
            {
                int index = i;
                var go = new GameObject("Option" + i);
                go.transform.SetParent(_safe, false);
                var r = go.AddComponent<RectTransform>();
                r.anchorMin = r.anchorMax = new Vector2(0.5f, 1f);
                r.pivot = new Vector2(0.5f, 0.5f);
                go.AddComponent<CanvasGroup>();
                var img = go.AddComponent<Image>();
                img.sprite = RoundedRectSprite.Get(64);
                img.type = Image.Type.Sliced;
                img.color = ClayRaster.Cream;
                NeuroStyle.ClayFrame(img, 5f, 12f);
                var button = go.AddComponent<Button>();
                button.transition = Selectable.Transition.None;
                button.onClick.AddListener(() => OnOption(index));
                go.AddComponent<PressScale>();

                var icon = NewImage(r, "Icon", null);
                icon.preserveAspect = true;
                var ir = icon.rectTransform;
                ir.anchorMin = new Vector2(0.12f, 0.12f);
                ir.anchorMax = new Vector2(0.88f, 0.88f);
                ir.offsetMin = ir.offsetMax = Vector2.zero;
                icon.gameObject.SetActive(true);

                var mark = NewImage(r, "Mark", null);
                var mr = mark.rectTransform;
                mr.anchorMin = mr.anchorMax = new Vector2(0.9f, 0.9f);
                mr.sizeDelta = new Vector2(110f, 110f);

                _optionRect[i] = r;
                _optionIcon[i] = icon;
                _optionMark[i] = mark;
                _optionButton[i] = button;
                go.SetActive(false);
            }
        }

        private void BuildCrewRow()
        {
            var go = new GameObject("Crew");
            go.transform.SetParent(_safe, false);
            _crewRow = go.AddComponent<RectTransform>();
            _crewRow.anchorMin = _crewRow.anchorMax = new Vector2(0.5f, 0f);
            _crewRow.pivot = new Vector2(0.5f, 0f);
            _crewRow.sizeDelta = new Vector2(960f, 170f);
            _crewRow.anchoredPosition = new Vector2(0f, 40f);

            _crewLabel = MakeText(_crewRow, "Label", 40, TextAnchor.MiddleCenter, new Color(1f, 1f, 1f, 0.75f), 0f, 0f);
            var lr = _crewLabel.rectTransform;
            lr.anchorMin = new Vector2(0f, 1f);
            lr.anchorMax = new Vector2(1f, 1f);
            lr.pivot = new Vector2(0.5f, 1f);
            lr.sizeDelta = new Vector2(0f, 56f);
            lr.anchoredPosition = Vector2.zero;

            const float icon = 80f, gap = 12f;
            float total = CrewIcons * icon + (CrewIcons - 1) * gap;
            for (int i = 0; i < CrewIcons; i++)
            {
                var slot = NewImage(_crewRow, "Slot", DiscSprite.Get());
                var sr = slot.rectTransform;
                sr.anchorMin = sr.anchorMax = new Vector2(0.5f, 0f);
                sr.sizeDelta = new Vector2(icon * 0.5f, icon * 0.5f);
                sr.anchoredPosition = new Vector2(-total / 2f + icon / 2f + i * (icon + gap), icon / 2f + 8f);
                slot.color = new Color(1f, 1f, 1f, 0.10f);
                slot.gameObject.SetActive(true);

                var h = NewImage(_crewRow, "Rescued", SymbolSprite.Get(ShapeKind.Helmet, 1));
                h.preserveAspect = true;
                var hr = h.rectTransform;
                hr.anchorMin = hr.anchorMax = new Vector2(0.5f, 0f);
                hr.sizeDelta = new Vector2(icon, icon);
                hr.anchoredPosition = sr.anchoredPosition;
                _crew.Add(h);
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
            y += promptH + 6f;

            // Radar: lo más grande posible (más grande = más periferia), dejando lugar a las respuestas y la fila.
            const float answersH = 330f, crewH = 210f;
            _scopeSize = Mathf.Min(contentW + 40f, sh - y - answersH - crewH);
            _scopeSize = Mathf.Max(_scopeSize, 560f);
            _scopeRect.sizeDelta = new Vector2(_scopeSize, _scopeSize);
            _scopeRect.anchoredPosition = new Vector2(0f, -(y + _scopeSize / 2f));
            y += _scopeSize + 16f;
            _glassR = _scopeSize * 0.5f * RadarSprites.GlassFraction;

            float optionSize = Mathf.Min(290f, answersH - 30f);
            for (int i = 0; i < 2; i++)
            {
                _optionRect[i].sizeDelta = new Vector2(optionSize, optionSize);
                _optionRect[i].anchoredPosition = new Vector2((i == 0 ? -1f : 1f) * (optionSize / 2f + 40f), -(y + answersH / 2f));
            }
            _hint.rectTransform.sizeDelta = new Vector2(contentW, 90f);
            _hint.rectTransform.anchoredPosition = new Vector2(0f, -(y + answersH / 2f));

            // Tamaños en el radar (proporcionales al vidrio).
            float cw = RadarContract.CenterWindow * _glassR;
            _sweep.rectTransform.sizeDelta = new Vector2(_glassR * 2f, _glassR * 2f);
            _fixation.rectTransform.sizeDelta = new Vector2(cw * 1.1f, cw * 1.1f);
            _centerIcon.rectTransform.sizeDelta = new Vector2(cw * 1.75f, cw * 1.75f);
            _centerMark.rectTransform.sizeDelta = new Vector2(cw * 0.9f, cw * 0.9f);
            _centerMark.rectTransform.anchoredPosition = new Vector2(cw * 0.85f, cw * 0.85f);
            float item = _glassR * 0.25f;
            foreach (var a in _asteroids) a.rectTransform.sizeDelta = new Vector2(item * 0.92f, item * 0.92f);
            _helmet.rectTransform.sizeDelta = new Vector2(item, item);
            _helmetGlow.rectTransform.sizeDelta = new Vector2(item * 2.4f, item * 2.4f);
            float pad = Mathf.Max(item * 1.15f, 132f);
            for (int d = 0; d < _pads.Count; d++)
            {
                RadarContract.Position(d, RadarContract.Rings - 1, out float px, out float py);
                var p = new Vector2(px * _glassR, py * _glassR);
                _pads[d].rectTransform.sizeDelta = new Vector2(pad, pad);
                _pads[d].rectTransform.anchoredPosition = p;
                _padMarks[d].rectTransform.sizeDelta = new Vector2(pad * 0.55f, pad * 0.55f);
                _padMarks[d].rectTransform.anchoredPosition = p + new Vector2(pad * 0.36f, pad * 0.36f);
            }
            _mask.rectTransform.sizeDelta = Vector2.zero;
        }

        private void UpdateHud()
        {
            _hud.SetLevel(_dda.PresentedLevel);
            if (Endless) _hud.SetPoints(_points);
            else _hud.SetInfo($"{Mathf.Min(_trials + 1, RadarContract.PrecisionTrials)} de {RadarContract.PrecisionTrials}");
        }
    }
}
