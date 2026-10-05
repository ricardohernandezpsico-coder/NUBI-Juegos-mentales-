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

namespace NeuroVida.Games.Freno
{
    /// <summary>
    /// "Freno de Emergencia": juego estrella de control inhibitorio (ver <see cref="BrakeContract"/>). Base de
    /// lanzamiento con 2 a 4 plataformas: se enciende un cohete y hay que lanzarlo tocando su carril, lo más rápido
    /// posible. A veces, un instante después, suena la alarma y aparece "¡ALTO!": hay que no tocar.
    /// <list type="bullet">
    /// <item>Cada lanzamiento despega de verdad (llamas, humo, temblor) y deja una estrella en el cielo: el cielo se
    /// llena con la partida.</item>
    /// <item>Frenar a tiempo da más puntos que lanzar y empuja el "límite del freno" (el alto llega más tarde); se ve en
    /// un medidor arriba con la marca del récord.</item>
    /// <item>Si la persona empieza a esperar el alto (lanza cada vez más lento), se le avisa: "¡No esperes al ALTO!".</item>
    /// </list>
    /// Medida propia: "tu freno" (SSRT, ms). Reto = 2 minutos; Precisión = 40 lanzamientos con más tiempo para lanzar.
    /// </summary>
    public class BrakeGameController : GameControllerBase
    {
        public const string GameId = BrakeContract.GameId;

        private const float UnitsPerDp = 3f;
        private const float MarginU = 50f;
        private const int MaxLanes = 4;
        private const int SkyStars = 60;
        private const float GaugeScaleMs = 900f;

        private static readonly Color GoodColor = NeuroStyle.Lime;
        private static readonly Color BadColor = NeuroStyle.Coral;
        private static readonly Color AmberColor = NeuroStyle.Sun;

        private enum Phase { Idle, Foreperiod, Respond, Feedback, Done }

        private sealed class Lane
        {
            public RectTransform Root, RocketRect;
            public Image Pad, Rocket, Glow, Beacon, Button, Mark, Hint;
        }

        private System.Random _rng;
        private AdaptiveDifficulty _dda;
        private Phase _phase = Phase.Idle;
        // la versión corta del inicio (Assessment) es de lanzamientos fijos y sin reloj, aunque la config traiga timed
        private bool Endless => _config != null && _config.config.timed && !Assessment.Active;
        private bool Precision => !Endless;
        private int _previousFrameRate;

        // ensayo en curso
        private int _tapLane = -1;
        private bool _tapped;
        private float _tapAt;

        // sesión
        private readonly List<float> _goRts = new List<float>();
        private readonly List<int> _stopSsds = new List<int>();
        private readonly List<bool> _stopResponded = new List<bool>();
        private int _trials, _goTrials, _goCorrect, _stopsOk, _bestSsd, _stopsInARow, _ssd;
        private int _streak, _bestStreak, _points, _launched, _lanesShown;
        private long _rtSum;
        private int _rtCount, _warnedAt = -99;
        private float _endsAt;
        private int _lastTickSecond = -1;

        // UI
        private RectTransform _safe, _play, _fxRect, _lanesRoot, _skyRoot, _stopRect, _timerBg, _timerFill, _gaugeFill, _gaugeBest;
        private Image _stopImage;
        private Text _prompt, _stopText, _gaugeValue;
        private readonly List<Lane> _lanes = new List<Lane>();
        private readonly List<Image> _stars = new List<Image>();
        private Toast _toast;
        private ExitButton _exit;
        private GameHud _hud;
        private CountdownScreen _countdown;
        private float _playW, _playH, _laneTop, _gaugeW;

        // ------------------------------------------------------------------ sesión

        public void StartSession(SequenceInitConfig config)
        {
            _config = config;
            _rng = new System.Random();
            // El DDA mueve la tarea de ir (plataformas y tiempo para lanzar); el alto tiene su propia escalera.
            _dda = BrakeContract.CreateEngine(config.config);

            // Tiempos de respuesta y retrasos del alto en decenas de ms: 60 cuadros por segundo (Android da 30).
            _previousFrameRate = Application.targetFrameRate;
            Application.targetFrameRate = 60;

            _phase = Phase.Idle;
            _goRts.Clear();
            _stopSsds.Clear();
            _stopResponded.Clear();
            _trials = _goTrials = _goCorrect = _stopsOk = _bestSsd = _stopsInARow = 0;
            _ssd = BrakeContract.SsdStartMs;
            _streak = _bestStreak = _points = _launched = 0;
            _lanesShown = 0;
            _rtSum = 0;
            _rtCount = 0;
            _warnedAt = -99;
            _endsAt = 0f;
            _lastTickSecond = -1;

            _resultRoot.gameObject.SetActive(false);
            _exit.Hide();
            _timerBg.gameObject.SetActive(Endless);
            _hud.SetStreak(0);
            _loopOn = false;
            _tutorial.Hide();
            _stopRect.gameObject.SetActive(false);
            foreach (var s in _stars) s.gameObject.SetActive(false);

            StopAllCoroutines();
            StartCoroutine(GameLoop());
        }

        private void OnDisable()
        {
            if (_previousFrameRate != 0) Application.targetFrameRate = _previousFrameRate;
        }

        private IEnumerator GameLoop()
        {
            bool tutorialPlayed = false;
            if (TutorialWanted)
            {
                // la ronda guiada se juega sobre la base ya armada, antes de la cuenta regresiva
                _safe.gameObject.SetActive(true);
                yield return null;
                ApplySafeArea(_safe);
                Canvas.ForceUpdateCanvases();
                Layout();
                SetupLanes(2);
                UpdateGauge(false);
                UpdateHud();
                yield return StartCoroutine(RunTutorialIfNeeded());
                tutorialPlayed = !_tutorial.Skipped;
            }
            _safe.gameObject.SetActive(false);
            yield return StartCoroutine(_countdown.Play("Freno de Emergencia", Assessment.Subtitle("Prepárate"), () => _safe.gameObject.SetActive(true)));
            _safe.gameObject.SetActive(true);
            yield return null;
            ApplySafeArea(_safe);
            Canvas.ForceUpdateCanvases();
            Layout();
            SetupLanes(BrakeContract.Pads(_dda.PresentedLevel));
            UpdateGauge(false);
            UpdateHud();

            // Primera vez sin tutorial (o si lo saltó): las dos reglas, antes del primer lanzamiento.
            SetPrompt("", Color.white);
            if (!tutorialPlayed)
            {
                SetPrompt("Lanza el cohete que se enciende", Color.white);
                yield return StartCoroutine(Wait(1.6f));
                SetPrompt("Si aparece ¡ALTO!, no toques", BadColor);
                yield return StartCoroutine(ShowStopPreview());
            }

            _endsAt = GameClock.Time + BrakeContract.RetoSeconds;
            _loopOn = true;
            yield return StartCoroutine(MainLoop());
        }

        /// <summary>El bucle de la partida: un lanzamiento tras otro. «Cómo se juega» lo retoma desde acá.</summary>
        private IEnumerator MainLoop()
        {
            while (!Finished())
                yield return StartCoroutine(RunTrial());
            _loopOn = false;
            yield return StartCoroutine(FinishGame());
        }

        private bool Finished() => Precision ? _trials >= BrakeContract.TotalTrials(Assessment.Active) : GameClock.Time >= _endsAt;

        /// <summary>Muestra la señal de alto un momento (sin alarma fuerte) para que se reconozca después.</summary>
        private IEnumerator ShowStopPreview()
        {
            _stopRect.gameObject.SetActive(true);
            _stopImage.color = Color.white;
            _stopText.color = Color.white;
            yield return StartCoroutine(PopIn(_stopRect, 0.25f));
            yield return StartCoroutine(Wait(1.3f));
            yield return StartCoroutine(FadeStop(0.25f));
        }

        // ------------------------------------------------------------------ un lanzamiento

        private IEnumerator RunTrial()
        {
            int level = _dda.PresentedLevel;
            int pads = BrakeContract.Pads(level);
            if (pads != _lanesShown)
            {
                SetupLanes(pads);
                if (_trials > 0) _toast.Show("¡Nueva plataforma!", "Ahora son " + pads, GoodColor, 1.0f);
                yield return StartCoroutine(Wait(0.4f));
            }
            int deadline = BrakeContract.DeadlineMs(level, Precision);
            bool stop = Assessment.Active ? BrakeContract.AssessmentStop(_trials) : BrakeContract.NextIsStop(_trials, _stopsInARow, _rng);
            int lane = _rng.Next(pads);

            // 1. Espera variable (no se puede anticipar). Tocar antes = "¡Espera la luz!" y se repite.
            _phase = Phase.Foreperiod;
            SetPrompt(_trials < 2 ? "Espera la luz..." : "", Color.white);
            _tapped = false;
            float fore = BrakeContract.ForeperiodMs(_rng) / 1000f;
            float t = 0f;
            while (t < fore)
            {
                t += GameClock.DeltaTime;
                if (_tapped)
                {
                    _tapped = false;
                    SetPrompt("¡Espera la luz!", AmberColor);
                    GameFeel.Haptic(GameFeel.HapticKind.Double);
                    yield return StartCoroutine(Wait(0.7f));
                    yield break; // no cuenta: se repite
                }
                yield return null;
            }

            // 2. Salida: se enciende un cohete.
            var L = _lanes[lane];
            SetLit(L, true);
            SetPrompt("", Color.white); // la luz es la señal: nada de texto que distraiga
            PlayTone(784f, 0.06f, 0.06f);
            _phase = Phase.Respond;
            _tapped = false;
            _tapLane = -1;
            float goAt = GameClock.Time;
            bool stopShown = false;
            float ssdS = _ssd / 1000f;
            float deadlineS = deadline / 1000f;
            while (!_tapped && GameClock.Time - goAt < deadlineS)
            {
                if (stop && !stopShown && GameClock.Time - goAt + Time.unscaledDeltaTime * 0.5f >= ssdS)
                {
                    stopShown = true;
                    ShowStop();
                }
                yield return null;
            }
            _phase = Phase.Feedback;
            float rtMs = _tapped ? (_tapAt - goAt) * 1000f : deadline;
            _trials++;
            SetLit(L, false);

            // 3. Resultado.
            if (stop)
            {
                _stopsInARow++;
                bool responded = _tapped;
                _stopSsds.Add(_ssd);
                _stopResponded.Add(responded);
                if (!responded) yield return StartCoroutine(StopSuccess(L, _ssd));
                else yield return StartCoroutine(StopFail(_tapLane >= 0 ? _lanes[_tapLane] : L, stopShown));
                _ssd = BrakeContract.NextSsd(_ssd, !responded, level, Precision);
                UpdateGauge(!responded);
            }
            else
            {
                _stopsInARow = 0;
                _goTrials++;
                _goRts.Add(Mathf.Min(rtMs, deadline));
                bool ok = _tapped && _tapLane == lane;
                if (_tapped)
                {
                    _rtSum += (long)rtMs;
                    _rtCount++;
                }
                var change = _dda.Register(ok, _tapped ? rtMs : -1f);
                if (ok) yield return StartCoroutine(Launch(L, rtMs, deadline, level));
                else if (_tapped) yield return StartCoroutine(WrongLane(_lanes[_tapLane], L));
                else yield return StartCoroutine(TooSlow(L));
                if (change == DdaChange.Up)
                {
                    _toast.Show("¡Más rápido!", "Menos tiempo para lanzar", GoodColor, 0.9f);
                    GameFeel.LevelUp();
                }
                else if (_dda.Struggling) _toast.Show("Con calma", "Mira bien cuál se enciende", AmberColor, 0.9f);

                // ¿Empezó a esperar el alto? Eso arruina la medida: aviso amable (como mucho cada 10 lanzamientos).
                if (BrakeContract.IsWaiting(_goRts) && _trials - _warnedAt >= 10)
                {
                    _warnedAt = _trials;
                    _toast.Show("¡No esperes al ALTO!", "Lanza apenas se encienda", AmberColor, 1.4f);
                }
            }
            _hud.SetStreak(_streak);
            UpdateHud();
        }

        // ------------------------------------------------------------------ desenlaces

        private IEnumerator Launch(Lane L, float rtMs, int deadline, int level)
        {
            _streak++;
            _bestStreak = Mathf.Max(_bestStreak, _streak);
            _goCorrect++;
            _launched++;
            int pts = BrakeContract.GoPoints(rtMs, deadline, level, _streak);
            _points += pts;
            GameFeel.Correct(_streak);
            GameFeel.Haptic(GameFeel.HapticKind.Light);
            PlayTone(196f, 0.25f, 0.05f); // rugido grave del motor
            SetPrompt(rtMs < deadline * 0.45f ? "¡Despegue relámpago!" : "¡Despegue!", GoodColor);
            StartCoroutine(FloatText(L.RocketRect, "+" + pts, NeuroStyle.Sun));
            StartCoroutine(FlyAway(L));
            StartCoroutine(UiFx.Shake(6f, 0.18f, _lanesRoot));
            if (_launched % 10 == 0)
            {
                _toast.Show($"¡{_launched} lanzamientos!", "Tu cielo se llena de estrellas", NeuroStyle.Sun, 1.0f);
                GameFeel.LevelUp();
            }
            yield return StartCoroutine(Wait(0.35f));
        }

        private IEnumerator WrongLane(Lane tapped, Lane lit)
        {
            _streak = 0;
            GameFeel.Wrong();
            SetPrompt("Ese no era", AmberColor);
            ShowMark(tapped, false);
            StartCoroutine(UiFx.Shake(14f, 0.3f, tapped.RocketRect));
            StartCoroutine(Blink(lit.Beacon, GoodColor));
            yield return StartCoroutine(Wait(0.6f));
            HideMark(tapped);
        }

        private IEnumerator TooSlow(Lane lit)
        {
            _streak = 0;
            SetPrompt("¡Más rápido!", AmberColor);
            PlayTone(262f, 0.12f, 0.05f);
            StartCoroutine(Blink(lit.Beacon, AmberColor));
            yield return StartCoroutine(Wait(0.55f));
        }

        private IEnumerator StopSuccess(Lane lit, int ssd)
        {
            _streak++;
            _bestStreak = Mathf.Max(_bestStreak, _streak);
            _stopsOk++;
            int pts = BrakeContract.StopPoints(ssd, _streak);
            _points += pts;
            bool record = ssd > _bestSsd && _stopsOk >= 2;
            _bestSsd = Mathf.Max(_bestSsd, ssd);
            GameFeel.Correct(_streak);
            PlayTone(147f, 0.3f, 0.05f); // resoplido de los frenos
            SetPrompt("¡FRENO PERFECTO!", GoodColor);
            ShowMark(lit, true);
            StartCoroutine(Steam(lit));
            StartCoroutine(FloatText(_stopRect, "+" + pts, NeuroStyle.Sun));
            StartCoroutine(UiFx.RingBurst(_fxRect, LocalIn(_fxRect, _stopRect), GoodColor, 200f, 520f, 0.45f));
            if (record)
            {
                _toast.Show("¡Nuevo límite!", $"Frenaste con el alto a {ssd} ms", NeuroStyle.Sun, 1.1f);
                GameFeel.LevelUp();
            }
            yield return StartCoroutine(Wait(0.5f));
            yield return StartCoroutine(FadeStop(0.2f));
            HideMark(lit);
        }

        private IEnumerator StopFail(Lane tapped, bool stopWasShown)
        {
            _streak = 0;
            if (!stopWasShown) ShowStop(); // tocó antes de que apareciera: igual se muestra, para que se entienda
            GameFeel.Wrong();
            GameFeel.Haptic(GameFeel.HapticKind.Double);
            SetPrompt("¡Se escapó!", BadColor);
            ShowMark(tapped, false);
            StartCoroutine(UiFx.Shake(16f, 0.3f, _stopRect));
            yield return StartCoroutine(Hop(tapped));
            yield return StartCoroutine(Wait(0.25f));
            yield return StartCoroutine(FadeStop(0.2f));
            HideMark(tapped);
        }

        // ------------------------------------------------------------------ entrada

        private void Update()
        {
            if (PollTutorialSkip()) return;             // un toque en «Saltar tutorial» no es un lanzamiento
            GuidedTutorial.SpinHint(_hintOf(_tutorialLane));
            UpdateClock();
            if (GameClock.DeltaTime <= 0f) return;
            if (_phase != Phase.Foreperiod && _phase != Phase.Respond) return;
            if (!Input.GetMouseButtonDown(0) || _tapped) return;
            if (_tutorial != null && _tutorial.Coach.Blocks(Input.mousePosition)) return;   // el foco de Nubi: solo vale el toque dentro del hueco
            // Toques al presionar (no al soltar): el tiempo de respuesta no carga la demora del dedo al levantarse.
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(_play, Input.mousePosition, null, out var local)) return;
            if (local.y > _laneTop) return; // arriba (medidor, marcador): no es un lanzamiento
            int n = Mathf.Max(1, _lanesShown);
            int lane = Mathf.Clamp(Mathf.FloorToInt((local.x / _playW + 0.5f) * n), 0, n - 1);
            _tapped = true;
            _tapLane = lane;
            _tapAt = GameClock.Time;
            if (_phase == Phase.Respond) StartCoroutine(PressButton(_lanes[lane]));
        }

        private void UpdateClock()
        {
            if (!Endless || _phase == Phase.Idle || _phase == Phase.Done || _endsAt <= 0f) return;
            float left = _endsAt - GameClock.Time;
            SetTimerFraction(Mathf.Clamp01(left / BrakeContract.RetoSeconds));
            int whole = Mathf.CeilToInt(left);
            if (whole <= 5 && whole >= 1 && whole != _lastTickSecond)
            {
                _lastTickSecond = whole;
                GameFeel.Tick();
            }
        }

        // ------------------------------------------------------------------ efectos

        private void SetLit(Lane L, bool lit)
        {
            L.Beacon.color = lit ? GoodColor : new Color(1f, 1f, 1f, 0.18f);
            L.Glow.gameObject.SetActive(lit);
            L.Glow.color = NeuroStyle.WithAlpha(GoodColor, 0.55f);
            L.Button.sprite = BrakeSprites.LaunchButton(lit);
            if (lit) StartCoroutine(PopRect(L.Beacon.rectTransform, 1.4f, 0.16f));
        }

        private void ShowStop()
        {
            _stopRect.gameObject.SetActive(true);
            _stopRect.localScale = Vector3.one; // aparece YA (sin animación de entrada: el retraso es la medida)
            _stopImage.color = Color.white;
            _stopText.color = Color.white;
            foreach (var L in _lanes) L.Beacon.color = BadColor;
            // Sirena: dos tonos alternados.
            PlayTone(988f, 0.09f, 0.08f);
            StartCoroutine(SirenTail());
        }

        private IEnumerator SirenTail()
        {
            yield return StartCoroutine(Wait(0.1f));
            PlayTone(740f, 0.09f, 0.08f);
            yield return StartCoroutine(Wait(0.1f));
            PlayTone(988f, 0.09f, 0.07f);
        }

        private IEnumerator FadeStop(float seconds)
        {
            float t = 0f;
            while (t < seconds)
            {
                t += GameClock.DeltaTime;
                float a = 1f - Mathf.Clamp01(t / seconds);
                _stopImage.color = new Color(1f, 1f, 1f, a);
                _stopText.color = new Color(1f, 1f, 1f, a);
                yield return null;
            }
            _stopRect.gameObject.SetActive(false);
            foreach (var L in _lanes) L.Beacon.color = new Color(1f, 1f, 1f, 0.18f);
        }

        /// <summary>El cohete despega: acelera hacia arriba con llama, deja humo, sale de la pantalla y se convierte
        /// en una estrella del cielo; en su plataforma aparece uno nuevo.</summary>
        private IEnumerator FlyAway(Lane L, bool star = true)
        {
            var from = LocalIn(_fxRect, L.RocketRect);
            float size = L.RocketRect.sizeDelta.x;
            L.Rocket.gameObject.SetActive(false);
            if (!Motion.Decorative)
            {
                // Sin despegue: el cohete se va y vuelve el nuevo tras el mismo tiempo (0,75 s), sin llama ni humo.
                yield return Motion.Hold(0.75f);
                if (star) AddSkyStar();
                L.Rocket.gameObject.SetActive(true);
                yield return StartCoroutine(PopIn(L.RocketRect, 0.2f));
                yield break;
            }

            var go = new GameObject("Flying");
            go.transform.SetParent(_fxRect, false);
            var r = go.AddComponent<RectTransform>();
            r.anchorMin = r.anchorMax = new Vector2(0.5f, 0.5f);
            r.sizeDelta = new Vector2(size, size);
            var flame = NewImage(r, "Flame", RadialGlowSprite.Get());
            flame.rectTransform.anchoredPosition = new Vector2(0f, -size * 0.55f);
            flame.gameObject.SetActive(true);
            var body = NewImage(r, "Body", L.Rocket.sprite);
            Stretch(body.rectTransform);
            body.rectTransform.localRotation = Quaternion.Euler(0f, 0f, 45f);
            body.gameObject.SetActive(true);
            for (int i = 0; i < 6; i++) StartCoroutine(Puff(from + new Vector2(0f, -size * 0.45f), i));

            float t = 0f;
            const float seconds = 0.75f;
            float travel = _playH + size * 2f;
            while (t < seconds)
            {
                t += GameClock.DeltaTime;
                float k = Mathf.Clamp01(t / seconds);
                r.anchoredPosition = from + new Vector2(Mathf.Sin(k * 18f) * 4f * (1f - k), travel * k * k);
                float f = 0.8f + 0.25f * Mathf.Sin(t * 60f);
                flame.rectTransform.sizeDelta = new Vector2(size * 0.8f * f, size * 1.3f * f);
                flame.color = NeuroStyle.WithAlpha(k < 0.5f ? NeuroStyle.Sun : NeuroStyle.Coral, 0.9f);
                yield return null;
            }
            Destroy(go);
            if (star) AddSkyStar();

            // Cohete nuevo en la plataforma.
            L.Rocket.gameObject.SetActive(true);
            yield return StartCoroutine(PopIn(L.RocketRect, 0.2f));
        }

        private IEnumerator Puff(Vector2 at, int i)
        {
            if (!Motion.Decorative) yield break; // sin ReduceMotion: sin humo
            var img = NewImage(_fxRect, "Smoke", DiscSprite.Get());
            img.gameObject.SetActive(true);
            var r = img.rectTransform;
            float dir = i % 2 == 0 ? -1f : 1f;
            float spread = 60f + 40f * (i / 2);
            float t = 0f;
            const float seconds = 0.6f;
            while (t < seconds)
            {
                t += GameClock.DeltaTime;
                float k = Mathf.Clamp01(t / seconds);
                r.anchoredPosition = at + new Vector2(dir * spread * UiFx.EaseOutCubic(k), 20f * k);
                float s = Mathf.Lerp(40f, 110f, k);
                r.sizeDelta = new Vector2(s, s);
                img.color = new Color(1f, 1f, 1f, 0.5f * (1f - k));
                yield return null;
            }
            Destroy(img.gameObject);
        }

        /// <summary>Frenos hidráulicos: vapor a los dos lados de la plataforma.</summary>
        private IEnumerator Steam(Lane L)
        {
            var at = LocalIn(_fxRect, L.RocketRect) + new Vector2(0f, -L.RocketRect.sizeDelta.y * 0.4f);
            for (int i = 0; i < 4; i++) StartCoroutine(Puff(at, i));
            yield return StartCoroutine(PopRect(L.RocketRect, 0.9f, 0.2f));
        }

        /// <summary>No frenó: el cohete da un salto, tambalea y vuelve a su plataforma (sin choques ni explosiones).</summary>
        private IEnumerator Hop(Lane L)
        {
            var r = L.RocketRect;
            var basePos = r.anchoredPosition;
            float t = 0f;
            const float seconds = 0.45f;
            if (!Motion.Decorative) { yield return Motion.Hold(seconds); yield break; } // sin salto ni vaivén: el cohete queda en su plataforma (misma duración)
            while (t < seconds)
            {
                t += GameClock.DeltaTime;
                float k = Mathf.Clamp01(t / seconds);
                r.anchoredPosition = basePos + new Vector2(Mathf.Sin(k * 30f) * 6f, 90f * Mathf.Sin(k * Mathf.PI));
                yield return null;
            }
            r.anchoredPosition = basePos;
            StartCoroutine(Puff(LocalIn(_fxRect, r) + new Vector2(0f, -r.sizeDelta.y * 0.45f), 0));
            StartCoroutine(Puff(LocalIn(_fxRect, r) + new Vector2(0f, -r.sizeDelta.y * 0.45f), 1));
        }

        private IEnumerator PressButton(Lane L)
        {
            var r = L.Button.rectTransform;
            if (!Motion.Decorative)
            {
                // Sin encogerse: el botón se oscurece un instante (como PressScale).
                var c0 = L.Button.color;
                L.Button.color = new Color(c0.r * 0.8f, c0.g * 0.8f, c0.b * 0.8f, c0.a);
                yield return StartCoroutine(Wait(0.1f));
                L.Button.color = c0;
                yield break;
            }
            r.localScale = Vector3.one * 0.9f;
            yield return StartCoroutine(Wait(0.1f));
            r.localScale = Vector3.one;
        }

        private IEnumerator Blink(Image img, Color color)
        {
            if (!Motion.Decorative)
            {
                // Sin parpadeo: la baliza queda prendida el mismo tiempo (0,48 s) y vuelve a su reposo.
                img.color = color;
                yield return StartCoroutine(Wait(0.48f));
                img.color = new Color(1f, 1f, 1f, 0.18f);
                yield break;
            }
            // Regla 5: el brillo oscila solo entre 1 y ~0,6 (antes 1 ↔ 0,18 a 6 Hz).
            var dim = Color.Lerp(color, new Color(1f, 1f, 1f, 0.18f), 0.4f);
            for (int i = 0; i < 3; i++)
            {
                img.color = color;
                yield return StartCoroutine(Wait(0.08f));
                img.color = dim;
                yield return StartCoroutine(Wait(0.08f));
            }
            img.color = new Color(1f, 1f, 1f, 0.18f);
        }

        private void ShowMark(Lane L, bool ok)
        {
            L.Mark.sprite = ok ? AnswerMarkSprite.Check() : AnswerMarkSprite.Cross();
            L.Mark.gameObject.SetActive(true);
            StartCoroutine(PopIn(L.Mark.rectTransform, 0.18f));
        }

        private static void HideMark(Lane L) => L.Mark.gameObject.SetActive(false);

        private void AddSkyStar()
        {
            var free = _stars.Find(s => !s.gameObject.activeSelf);
            if (free == null) return;
            var r = free.rectTransform;
            float top = _laneTop - 40f, bottom = -_playH * 0.5f + 660f; // cielo: entre las plataformas y el medidor
            r.anchoredPosition = new Vector2(((float)_rng.NextDouble() - 0.5f) * (_playW - 80f), Mathf.Lerp(bottom, top, (float)_rng.NextDouble()));
            float s = 30f + (float)_rng.NextDouble() * 26f;
            r.sizeDelta = new Vector2(s, s);
            free.color = NeuroStyle.WithAlpha(_rng.NextDouble() < 0.5 ? NeuroStyle.Sun : Color.white, 0.9f);
            free.gameObject.SetActive(true);
            StartCoroutine(PopIn(r, 0.3f));
            StartCoroutine(UiFx.SparkBurst(_skyRoot, r.anchoredPosition, NeuroStyle.Sun, 6, 60f, 12f, 0.35f));
        }

        private void UpdateGauge(bool stopped)
        {
            float f = Mathf.Clamp01(_ssd / GaugeScaleMs);
            _gaugeFill.anchorMax = new Vector2(f, 1f);
            _gaugeFill.offsetMin = _gaugeFill.offsetMax = Vector2.zero;
            _gaugeValue.text = $"{_ssd} ms";
            if (_bestSsd > 0)
            {
                _gaugeBest.gameObject.SetActive(true);
                _gaugeBest.anchoredPosition = new Vector2(_gaugeW * Mathf.Clamp01(_bestSsd / GaugeScaleMs), 0f);
            }
            if (stopped) StartCoroutine(PopRect(_gaugeValue.rectTransform, 1.25f, 0.25f));
        }

        private void SetPrompt(string text, Color color)
        {
            _prompt.text = text;
            _prompt.color = color;
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
                r.anchoredPosition = pos + new Vector2(0f, 120f + 80f * (Motion.Decorative ? UiFx.EaseOutCubic(k) : 0f));
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
            _stopRect.gameObject.SetActive(false);

            float goAcc = _goTrials > 0 ? (float)_goCorrect / _goTrials : 0f;
            int ssrt = BrakeContract.Ssrt(_goRts, _stopSsds, _stopResponded);
            int score = BrakeContract.Score(goAcc, ssrt);
            int avgMs = _rtCount > 0 ? (int)(_rtSum / _rtCount) : 0;

            SetPrompt("Fin de los lanzamientos", GoodColor);
            ShowResult(score, ssrt);

            var telemetry = new StroopTelemetry
            {
                user_id = _config.user_id,
                game_id = _config.game_id,
                session_metrics = new StroopSessionMetrics
                {
                    correct_trials = _goCorrect + _stopsOk,
                    total_trials = _trials,
                    calculated_score = score,
                    average_response_time_ms = avgMs,
                    level = _config.config.level,
                    timed = _config.config.timed,
                    end_rating = _dda.RatingNormalized,
                    mode_trials = _dda.ScoredTrials,
                    mode_hits = _dda.ScoredCorrect,
                    peak_level = _dda.PeakLevel,
                    brake_ms = ssrt,
                    stops_ok = _stopsOk,
                    stops_total = _stopSsds.Count,
                    brake_best_ssd_ms = _bestSsd > 0 ? _bestSsd : -1
                }
            };
            NativeBridge.ForwardTelemetryToPlatform(JsonUtility.ToJson(telemetry));
            yield break;
        }

        private void ShowResult(int score, int ssrt)
        {
            _exit.Show();
            _resultRoot.Find("Title").GetComponent<Text>().text = score >= 85 ? "¡Frenos de acero!" : score >= 65 ? "¡Buena base!" : "Lanzamientos completados";
            _resultRoot.Find("Detail").GetComponent<Text>().text = $"{_launched} despegues · {_stopsOk} de {_stopSsds.Count} frenos";
            _resultRoot.Find("Extra").GetComponent<Text>().text = ssrt > 0 ? $"Tu freno: {ssrt} ms" : $"Mejor racha {_bestStreak}";
            _resultRoot.gameObject.SetActive(true);
            StartCoroutine(AnimateResult(score));
        }

        // ------------------------------------------------------------------ construcción de UI

        protected override void BuildUi()
        {

            var canvasGo = new GameObject("BrakeCanvas");
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
            WorldBackdrop.Build(bgRect, GameWorld.LaunchBase);

            var safeGo = new GameObject("SafeAreaContent");
            safeGo.transform.SetParent(canvasGo.transform, false);
            _safe = safeGo.AddComponent<RectTransform>();
            ApplySafeArea(_safe);

            _hud = new GameHud(_safe, "Freno de Emergencia", MarginU, this);
            BuildTimer();
            BuildGauge();

            var playGo = new GameObject("Play");
            playGo.transform.SetParent(_safe, false);
            _play = playGo.AddComponent<RectTransform>();
            Stretch(_play);

            _skyRoot = Layer(_play, "Sky");
            for (int i = 0; i < SkyStars; i++) _stars.Add(NewImage(_skyRoot, "Star", SparkleSprite.Get()));

            _prompt = MakeText(_play, "Prompt", 66, TextAnchor.MiddleCenter, Color.white, 0f, 0f);
            NeuroStyle.ClayText(_prompt, 3.5f, 5f);
            _prompt.rectTransform.anchorMin = _prompt.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            BestFit(_prompt, 40);

            _lanesRoot = Layer(_play, "Lanes");
            for (int i = 0; i < MaxLanes; i++) _lanes.Add(BuildLane(i));

            BuildStopSign();

            _fxRect = Layer(_play, "Fx");

            BuildResultPanel();
            _exit = new ExitButton(_safe, this, UnitsPerDp);
            _toast = new Toast(_safe, this, UnitsPerDp);
            _toast.SetTopOffset(0f);
            BuildTutorial(_safe, GameHud.Height + 125f, "Freno de Emergencia", "Lanza el cohete que se enciende. Si aparece ¡ALTO!, no toques.",
                skipAtTop: true, captionFromBottomU: 700f);

            var flashGo = new GameObject("Flash");
            flashGo.transform.SetParent(canvasGo.transform, false);
            Stretch(flashGo.AddComponent<RectTransform>());
            _flash = flashGo.AddComponent<Image>();
            _flash.raycastTarget = false;
            _flash.color = new Color(0f, 0f, 0f, 0f);

            _countdown = new CountdownScreen(canvasGo.transform, UnitsPerDp);
        }

        private Lane BuildLane(int i)
        {
            var go = new GameObject("Lane" + i);
            go.transform.SetParent(_lanesRoot, false);
            var root = go.AddComponent<RectTransform>();
            root.anchorMin = root.anchorMax = new Vector2(0.5f, 0.5f);
            root.pivot = new Vector2(0.5f, 0.5f);

            var pad = NewImage(root, "Pad", BrakeSprites.LaunchPad());
            pad.preserveAspect = true;
            pad.gameObject.SetActive(true);

            var glow = NewImage(root, "Glow", RadialGlowSprite.Get());

            var rocketGo = new GameObject("RocketHolder");
            rocketGo.transform.SetParent(root, false);
            var rr = rocketGo.AddComponent<RectTransform>();
            rr.anchorMin = rr.anchorMax = new Vector2(0.5f, 0.5f);
            var rocket = NewImage(rr, "Rocket", SymbolSprite.Get(ShapeKind.Rocket, i % 3));
            Stretch(rocket.rectTransform);
            rocket.rectTransform.localRotation = Quaternion.Euler(0f, 0f, 45f); // el ícono viene inclinado: apunta arriba
            rocket.preserveAspect = true;
            rocket.gameObject.SetActive(true);

            var beacon = NewImage(root, "Beacon", DiscSprite.Get());
            beacon.color = new Color(1f, 1f, 1f, 0.18f);
            beacon.gameObject.SetActive(true);

            var button = NewImage(root, "Button", BrakeSprites.LaunchButton(false));
            button.preserveAspect = true;
            button.gameObject.SetActive(true);

            var mark = NewImage(rr, "Mark", null);
            mark.rectTransform.anchorMin = mark.rectTransform.anchorMax = new Vector2(0.9f, 0.9f);
            var hint = GuidedTutorial.CreateHintRing(root, 10f);     // el aro de ayuda de la ronda guiada: «toca aquí»

            go.SetActive(false);
            return new Lane { Root = root, RocketRect = rr, Pad = pad, Rocket = rocket, Glow = glow, Beacon = beacon, Button = button, Mark = mark, Hint = hint };
        }

        private void BuildStopSign()
        {
            var go = new GameObject("Stop");
            go.transform.SetParent(_play, false);
            _stopRect = go.AddComponent<RectTransform>();
            _stopRect.anchorMin = _stopRect.anchorMax = new Vector2(0.5f, 0.5f);
            _stopRect.sizeDelta = new Vector2(420f, 420f);
            _stopImage = go.AddComponent<Image>();
            _stopImage.sprite = BrakeSprites.StopSign();
            _stopImage.raycastTarget = false;
            _stopText = MakeText(_stopRect, "Label", 120, TextAnchor.MiddleCenter, Color.white, 0f, 0f);
            NeuroStyle.ClayText(_stopText, 5f, 7f);
            Stretch(_stopText.rectTransform);
            _stopText.rectTransform.offsetMin = new Vector2(0f, 14f);
            _stopText.text = "ALTO";
            go.SetActive(false);
        }

        private void BuildGauge()
        {
            // "Límite del freno": qué tan tarde llega el alto ahora (sube al frenar) y la marca del récord.
            var go = new GameObject("Gauge");
            go.transform.SetParent(_safe, false);
            var root = go.AddComponent<RectTransform>();
            root.anchorMin = root.anchorMax = new Vector2(0.5f, 1f);
            root.pivot = new Vector2(0.5f, 1f);
            root.sizeDelta = new Vector2(960f, 96f);
            root.anchoredPosition = new Vector2(0f, -(GameHud.Height + 36f));

            var label = MakeText(root, "Label", 36, TextAnchor.MiddleLeft, new Color(1f, 1f, 1f, 0.8f), 0f, 0f);
            label.text = "Límite del freno";
            var lr = label.rectTransform;
            lr.anchorMin = new Vector2(0f, 1f);
            lr.anchorMax = new Vector2(0.7f, 1f);
            lr.pivot = new Vector2(0f, 1f);
            lr.sizeDelta = new Vector2(0f, 50f);
            lr.anchoredPosition = Vector2.zero;

            _gaugeValue = MakeText(root, "Value", 40, TextAnchor.MiddleRight, NeuroStyle.Sky, 0f, 0f);
            NeuroStyle.ClayText(_gaugeValue, 2.5f, 3f);
            var vr = _gaugeValue.rectTransform;
            vr.anchorMin = new Vector2(0.6f, 1f);
            vr.anchorMax = new Vector2(1f, 1f);
            vr.pivot = new Vector2(1f, 1f);
            vr.sizeDelta = new Vector2(0f, 50f);
            vr.anchoredPosition = Vector2.zero;

            var barGo = new GameObject("Bar");
            barGo.transform.SetParent(root, false);
            var bar = barGo.AddComponent<RectTransform>();
            bar.anchorMin = new Vector2(0f, 0f);
            bar.anchorMax = new Vector2(1f, 0f);
            bar.pivot = new Vector2(0f, 0f);
            bar.sizeDelta = new Vector2(0f, 20f);
            bar.anchoredPosition = new Vector2(0f, 6f);
            var barImg = barGo.AddComponent<Image>();
            barImg.sprite = RoundedRectSprite.Get(10);
            barImg.type = Image.Type.Sliced;
            barImg.color = new Color(1f, 1f, 1f, 0.12f);
            barImg.raycastTarget = false;

            var fillGo = new GameObject("Fill");
            fillGo.transform.SetParent(bar, false);
            _gaugeFill = fillGo.AddComponent<RectTransform>();
            _gaugeFill.anchorMin = Vector2.zero;
            _gaugeFill.anchorMax = new Vector2(0.3f, 1f);
            _gaugeFill.offsetMin = _gaugeFill.offsetMax = Vector2.zero;
            var fill = fillGo.AddComponent<Image>();
            fill.sprite = RoundedRectSprite.Get(10);
            fill.type = Image.Type.Sliced;
            fill.color = NeuroStyle.Sky;
            fill.raycastTarget = false;

            var bestGo = new GameObject("Best");
            bestGo.transform.SetParent(bar, false);
            _gaugeBest = bestGo.AddComponent<RectTransform>();
            _gaugeBest.anchorMin = _gaugeBest.anchorMax = new Vector2(0f, 0.5f);
            _gaugeBest.sizeDelta = new Vector2(10f, 40f);
            var best = bestGo.AddComponent<Image>();
            best.sprite = RoundedRectSprite.Get(10);
            best.type = Image.Type.Sliced;
            best.color = NeuroStyle.Sun;
            best.raycastTarget = false;
            NeuroStyle.ClayFrame(best, 2f, 3f);
            bestGo.SetActive(false);
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
            float bottom = -_playH * 0.5f;
            // Zona de carriles: de abajo hasta aquí se toca para lanzar (el medidor y el marcador quedan arriba).
            _laneTop = _playH * 0.5f - (GameHud.Height + 150f);
            _gaugeW = Mathf.Min(960f, _playW - MarginU * 2f);
            _prompt.rectTransform.sizeDelta = new Vector2(_playW - MarginU * 2f, 110f);
            _prompt.rectTransform.anchoredPosition = new Vector2(0f, bottom + 980f);
            _stopRect.anchoredPosition = new Vector2(0f, bottom + 1180f);
            _stopRect.sizeDelta = new Vector2(400f, 400f);
        }

        /// <summary>Muestra <paramref name="n"/> carriles repartidos a lo ancho (plataforma, cohete, baliza, botón).</summary>
        private void SetupLanes(int n)
        {
            _lanesShown = n;
            float bottom = -_playH * 0.5f;
            float laneW = (_playW - 20f) / n;
            float rocket = Mathf.Min(230f, laneW * 0.8f);
            float button = Mathf.Min(210f, laneW * 0.78f);
            for (int i = 0; i < _lanes.Count; i++)
            {
                var L = _lanes[i];
                bool on = i < n;
                L.Root.gameObject.SetActive(on);
                if (!on) continue;
                L.Root.anchoredPosition = new Vector2(-(_playW - 20f) * 0.5f + laneW * (i + 0.5f), 0f);
                L.Root.sizeDelta = new Vector2(laneW, _playH);

                float buttonY = bottom + 60f + button * 0.5f;
                L.Button.rectTransform.sizeDelta = new Vector2(button, button);
                L.Button.rectTransform.anchoredPosition = new Vector2(0f, buttonY);
                L.Button.rectTransform.localScale = Vector3.one;
                L.Hint.rectTransform.sizeDelta = new Vector2(button * 1.4f, button * 1.4f);
                L.Hint.rectTransform.anchoredPosition = new Vector2(0f, buttonY);
                L.Hint.gameObject.SetActive(false);

                float padY = buttonY + button * 0.5f + 90f;
                L.Pad.rectTransform.sizeDelta = new Vector2(Mathf.Min(300f, laneW * 0.98f), Mathf.Min(300f, laneW * 0.98f));
                L.Pad.rectTransform.anchoredPosition = new Vector2(0f, padY);

                float rocketY = padY + rocket * 0.05f; // la cola apoyada en la losa, entre las torres
                L.RocketRect.sizeDelta = new Vector2(rocket, rocket);
                L.RocketRect.anchoredPosition = new Vector2(0f, rocketY);
                L.RocketRect.localScale = Vector3.one;
                L.Rocket.gameObject.SetActive(true);
                L.Glow.rectTransform.sizeDelta = new Vector2(rocket * 2f, rocket * 2f);
                L.Glow.rectTransform.anchoredPosition = new Vector2(0f, rocketY);
                L.Glow.gameObject.SetActive(false);
                L.Beacon.rectTransform.sizeDelta = new Vector2(46f, 46f);
                L.Beacon.rectTransform.anchoredPosition = new Vector2(0f, rocketY + rocket * 0.62f);
                L.Beacon.color = new Color(1f, 1f, 1f, 0.18f);
                L.Mark.rectTransform.sizeDelta = new Vector2(rocket * 0.45f, rocket * 0.45f);
                L.Mark.gameObject.SetActive(false);
                L.Button.sprite = BrakeSprites.LaunchButton(false);
                StartCoroutine(PopIn(L.Root, 0.25f));
            }
        }

        private void UpdateHud()
        {
            _hud.SetLevel(_dda.PresentedLevel);
            int total = BrakeContract.TotalTrials(Assessment.Active);
            if (Endless) _hud.SetPoints(_points);
            else _hud.SetInfo($"{Mathf.Min(_trials + 1, total)} de {total}");
        }

        // ------------------------------------------------------------------ «Cómo se juega» desde la pausa

        private bool _loopOn;
        private int _lanesBefore;

        protected override bool HowToReady => _loopOn && _phase != Phase.Done;

        protected override void HowToSuspend()
        {
            _lanesBefore = _lanesShown;
            _phase = Phase.Idle;
            _tapped = false;
            SetPrompt("", Color.white);
            _toast.Hide();
            ClearTransient();
        }

        protected override void HowToResume(float spentSeconds)
        {
            _endsAt = HowToClock.Shift(_endsAt, spentSeconds);       // el tiempo que duró «Cómo se juega» no se le descuenta al Reto
            _phase = Phase.Idle;
            ClearTransient();
            SetupLanes(Mathf.Max(2, _lanesBefore));
            UpdateGauge(false);
            UpdateHud();
            StartCoroutine(MainLoop());                              // el lanzamiento que estaba en curso no contó: sigue otro con el mismo estado
        }

        /// <summary>Quita lo que quedó a medias (efectos sueltos, la señal de alto, balizas encendidas) sin tocar ninguna cuenta.</summary>
        private void ClearTransient()
        {
            foreach (Transform c in _fxRect) Destroy(c.gameObject);
            _stopRect.gameObject.SetActive(false);
            foreach (var L in _lanes)
            {
                L.Beacon.color = new Color(1f, 1f, 1f, 0.18f);
                L.Glow.gameObject.SetActive(false);
                L.Button.sprite = BrakeSprites.LaunchButton(false);
                L.Button.rectTransform.localScale = Vector3.one;
                L.Rocket.gameObject.SetActive(true);
                L.RocketRect.localScale = Vector3.one;
                L.Mark.gameObject.SetActive(false);
                L.Hint.gameObject.SetActive(false);
            }
        }

        // ------------------------------------------------------------------ ronda guiada del tutorial (pieza común)

        private int _tutorialLane = -1;
        private Image _hintOf(int lane) => lane >= 0 && lane < _lanes.Count ? _lanes[lane].Hint : null;

        // <guided>
        protected override IEnumerator GuidedRound(GuidedTutorial t)
        {
            t.BeginPractice();
            _phase = Phase.Idle;
            SetPrompt("", Color.white);
            SetupLanes(2);
            var coach = t.Coach;
            yield return StartCoroutine(Wait(0.4f));
            // «Nubi entrenadora»: 1) tocar el cohete que se enciende (el juego se congela y ese toque es el real), 2) el primer ¡ALTO! se ve con el foco un instante y
            // se deja quieto, 3) un aviso breve al lograrlo. Nada se queda esperando en silencio.
            var script = new GuidedScript(BrakeContract.GuidedPlan.Length);
            while (!script.Finished)
            {
                if (t.Skipped) { script.Skip(); break; }
                bool isStop = BrakeContract.GuidedPlan[script.Index] == BrakeContract.GuidedStep.Stop;
                int lane = (script.Index + script.Failures) % 2;
                var L = _lanes[lane];

                SetLit(L, true);                      // el cohete se enciende y se queda encendido hasta que se toque
                PlayTone(784f, 0.06f, 0.06f);
                _tutorialLane = lane;
                L.Hint.gameObject.SetActive(!isStop); // el aro marca el que se toca; el del ¡ALTO! no lleva aro
                _tapped = false;
                _tapLane = -1;
                _phase = Phase.Respond;
                float litAt = GameClock.Time;
                bool stopShown = false;
                float limit = isStop ? 3.2f : 600f;
                if (!isStop) StartCoroutine(coach.Touch(() => LaneHole(L), CoachTexts.Freno.Launch));
                while (!_tapped && !t.Skipped && GameClock.Time - litAt < limit)
                {
                    if (!isStop && GuidedTutorial.AutoPlay(GameClock.Time - litAt)) { _tapped = true; _tapLane = lane; }   // solo en el smoke del Editor
                    if (isStop && !stopShown && GameClock.Time - litAt >= 0.7f)
                    {
                        stopShown = true;
                        ShowStop();
                        StartCoroutine(coach.Watch(() => coach.RectOf(_stopRect), CoachTexts.Freno.Stop, () => false, 1.6f));
                    }
                    yield return null;
                }
                _phase = Phase.Idle;
                L.Hint.gameObject.SetActive(false);
                _tutorialLane = -1;
                SetLit(L, false);
                coach.Hide();
                if (t.Skipped) { script.Skip(); break; }

                if (!isStop)
                {
                    if (_tapped && _tapLane == lane)
                    {
                        PlayTone(196f, 0.25f, 0.05f);
                        StartCoroutine(FlyAway(L, false));
                        GameFeel.Haptic(GameFeel.HapticKind.Light);
                        script.Success();
                        yield return StartCoroutine(Wait(0.6f));
                    }
                    else if (_tapped)
                    {
                        ShowMark(_lanes[_tapLane], false);
                        script.Failure();
                        yield return StartCoroutine(coach.Notice(CoachTexts.Freno.Missed, 2.2f));
                        HideMark(_lanes[_tapLane]);
                    }
                }
                else if (!_tapped)
                {
                    PlayTone(147f, 0.3f, 0.05f);
                    ShowMark(L, true);
                    StartCoroutine(Steam(L));
                    script.Success();
                    yield return StartCoroutine(coach.Notice(CoachTexts.Freno.Braked, 1.8f));
                    HideMark(L);
                    yield return StartCoroutine(FadeStop(0.2f));
                }
                else
                {
                    if (!stopShown) ShowStop();         // tocó antes de que apareciera: igual se muestra, para que se entienda
                    ShowMark(_tapLane >= 0 ? _lanes[_tapLane] : L, false);
                    script.Failure();
                    yield return StartCoroutine(Hop(L));
                    yield return StartCoroutine(coach.Notice(CoachTexts.Freno.Tapped, 2.4f));
                    HideMark(_tapLane >= 0 ? _lanes[_tapLane] : L);
                    yield return StartCoroutine(FadeStop(0.2f));
                }
                yield return StartCoroutine(Wait(0.3f));
            }
            _stopRect.gameObject.SetActive(false);
            foreach (var l in _lanes) { l.Beacon.color = new Color(1f, 1f, 1f, 0.18f); l.Mark.gameObject.SetActive(false); l.Hint.gameObject.SetActive(false); }
            if (!script.Skipped)
            {
                PlayTone(523f, 0.4f, 0.08f);
                yield return StartCoroutine(coach.Notice(CoachTexts.Ready, 1.5f));
            }
            t.EndPractice();
            SetPrompt("", Color.white);
        }
        // </guided>

        /// <summary>El cohete, su botón y su baliza (para el foco de «tocar»).</summary>
        private Rect LaneHole(Lane L)
        {
            var coach = _tutorial.Coach;
            var a = coach.RectOf(L.RocketRect);
            var b = coach.RectOf(L.Button.rectTransform);
            var c = coach.RectOf(L.Beacon.rectTransform);
            return Rect.MinMaxRect(Mathf.Min(a.xMin, Mathf.Min(b.xMin, c.xMin)), Mathf.Min(a.yMin, Mathf.Min(b.yMin, c.yMin)),
                Mathf.Max(a.xMax, Mathf.Max(b.xMax, c.xMax)), Mathf.Max(a.yMax, Mathf.Max(b.yMax, c.yMax)));
        }
    }
}
