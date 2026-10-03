using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using NeuroVida.Bridge;
using NeuroVida.Contracts;
using NeuroVida.Games.Secuencia; // RoundedRectSprite / RadialGlowSprite / RingSprite
using NeuroVida.Games.Shared;
using NeuroVida.Games.Trafico;   // RailLine, TrafficSounds.EngineLoop
using static NeuroVida.Games.Shared.UiKit;
using Motion = NeuroVida.Games.Shared.Motion; // UnityEngine.Motion también existe

namespace NeuroVida.Games.Rumbo
{
    /// <summary>
    /// "Rumbo a Casa": juego estrella de orientación (integración de trayecto; ver <see cref="HomingContract"/>).
    /// <para>Vista de cabina: la nave queda fija mirando hacia arriba y es el espacio el que gira y pasa (polvo de
    /// estrellas cerca, niebla lejos). La ida: aparece la señal de un cristal en el borde de la vista, se toca, la nave
    /// gira hacia él y vuela; así 2 a 5 tramos. La base queda en la niebla. La vuelta: "¿Hacia dónde está casa?" se
    /// apunta con el dedo (flecha sobre un dial que gira con la nave), se fija el rumbo, la nave avanza y se toca
    /// "¡AQUÍ!" donde se cree que está la base.</para>
    /// <para>La revelación: la cámara se aleja hasta la vista desde arriba (el mapa gira hasta el norte), aparece la ruta
    /// de ida (lima), la vuelta (sol) y la vuelta justa (crema). Suena el acorde de llegada: más resuelto cuanto más cerca
    /// de casa. En la mitad de los viajes hay FARO (una estrella lejanísima que solo cambia de lugar al girar): así el
    /// final puede decir cuánto ayuda un punto de referencia.</para>
    /// Reto = 150 s (se termina el viaje en curso); Precisión = 8 viajes.
    /// </summary>
    public class HomingGameController : GameControllerBase
    {
        public const string GameId = HomingContract.GameId;

        private const float UnitsPerDp = 3f;
        private const float MarginU = 60f;
        private const float ShipSize = 170f;
        private const float BaseSize = 270f;
        private const float CrystalSize = 124f;
        private const float BeaconSize = 120f;
        private const float SignalRadius = 420f;
        private const float BeaconRadius = 465f;
        private const float DialSize = 500f;
        private const int DustCount = 150;
        private const float DustTile = 2600f;
        private const float AimDeadZone = 60f;
        /// <summary>Si nadie toca la señal en este tiempo, la nave va sola (el juego no se traba).</summary>
        private const float SignalAutoGo = 8f;

        private static readonly Color GoodColor = NeuroStyle.Lime;
        private static readonly Color BadColor = NeuroStyle.Coral;
        private static readonly Color AmberColor = NeuroStyle.Sun;

        private enum Phase { Idle, Outbound, Aim, Advance, Reveal, Done }

        private System.Random _rng;
        private AdaptiveDifficulty _dda;
        private Phase _phase = Phase.Idle;
        private bool Endless => _config != null && _config.config.timed;
        private bool Precision => !Endless;
        private int _previousFrameRate;

        // viaje en curso
        private HomingTrip _trip;
        private int _tripIndex;
        private bool _pairFirst;
        private float _px, _py, _heading;
        private bool _moving;
        private int _nextCrystal;
        private bool _signalTapped, _aimed, _lockPressed, _stopPressed;
        private float _aimAngle;

        // cámara: centro (mapa), giro, escala y dónde queda ese centro en la pantalla
        private float _camX, _camY, _camRot, _camScale = 1f;
        private Vector2 _camPivot;
        private bool _mapMode;

        // sesión
        private readonly List<HomingOutcome> _outcomes = new List<HomingOutcome>();
        private readonly List<bool> _beacons = new List<bool>();
        private int _hits, _perfects, _streak, _bestStreak, _points, _crystalsTotal;
        private float _endsAt;
        private int _lastTickSecond = -1;
        private bool _signalHintShown, _aimHintShown, _beaconHintShown, _stopHintShown, _introShown;

        // UI
        private RectTransform _safe, _play, _view, _pivot, _content, _fxRect, _shipRect, _baseRect, _signal, _beacon;
        private RectTransform _aimRoot, _aimArrow, _lockButton, _stopButton, _mapShip, _endMark, _timerBg, _timerFill;
        private Image _fog, _flame, _baseImg, _baseGlow, _signalRing, _signalGem, _beaconGlow, _dial, _endMarkImg;
        private readonly List<RectTransform> _crystals = new List<RectTransform>();
        private readonly List<Image> _crystalImgs = new List<Image>();
        private readonly List<Image> _crystalGlows = new List<Image>();
        private readonly List<Image> _dust = new List<Image>();
        private readonly List<Vector2> _dustPos = new List<Vector2>();
        private readonly List<float> _dustAlpha = new List<float>();
        private RailLine _outGlow, _outLine, _idealLine, _backLine, _gapLine;
        private Text _prompt, _detail, _hint;
        private Toast _toast;
        private ExitButton _exit;
        private GameHud _hud;
        private CountdownScreen _countdown;
        private AudioSource _engine;
        private float _playW, _playH;
        private Vector2 _shipScreen;
        private float _dustFade = 1f, _fogFade = 1f;

        // ------------------------------------------------------------------ sesión

        public void StartSession(SequenceInitConfig config)
        {
            _config = config;
            _rng = new System.Random();
            var age = DdaUserProfileConfig.ParseAgeBand(config.config.age_band);
            float start = AdaptiveDifficulty.StartRating(config.config, HomingContract.MaxLevel);
            // Pocos viajes por partida (6-8): pasos grandes. Sin tiempo de reacción: cuenta llegar a casa.
            _dda = new AdaptiveDifficulty(HomingContract.MaxLevel, age, start, stepUp: 0.5f, useReaction: false);

            // Giros y vuelo fluidos: 60 cuadros por segundo (Android da 30).
            _previousFrameRate = Application.targetFrameRate;
            Application.targetFrameRate = 60;

            _phase = Phase.Idle;
            _outcomes.Clear();
            _beacons.Clear();
            _tripIndex = 0;
            _hits = _perfects = _streak = _bestStreak = _points = _crystalsTotal = 0;
            _endsAt = 0f;
            _lastTickSecond = -1;
            _signalHintShown = _aimHintShown = _beaconHintShown = _stopHintShown = _introShown = false;

            _resultRoot.gameObject.SetActive(false);
            _exit.Hide();
            _timerBg.gameObject.SetActive(Endless);
            _hud.SetStreak(0);

            StopAllCoroutines();
            StartCoroutine(GameLoop());
        }

        private void OnDisable()
        {
            if (_previousFrameRate != 0) Application.targetFrameRate = _previousFrameRate;
            if (_engine != null) _engine.Stop();
        }

        private IEnumerator GameLoop()
        {
            _safe.gameObject.SetActive(false);
            yield return StartCoroutine(_countdown.Play("Rumbo a Casa", Assessment.Subtitle("Prepárate"), () => _safe.gameObject.SetActive(true)));
            _safe.gameObject.SetActive(true);
            yield return null;
            ApplySafeArea(_safe);
            Canvas.ForceUpdateCanvases();
            Layout();
            UpdateHud();

            _endsAt = GameClock.Time + HomingContract.RetoSeconds;
            while (!Finished())
            {
                yield return StartCoroutine(RunTrip());
                _tripIndex++;
            }
            yield return StartCoroutine(FinishGame());
        }

        private bool Finished() => Precision ? _outcomes.Count >= HomingContract.PrecisionTrips : GameClock.Time >= _endsAt;

        // ------------------------------------------------------------------ un viaje

        private IEnumerator RunTrip()
        {
            int level = _dda.PresentedLevel;
            bool beacon = HomingContract.BeaconFor(_tripIndex, _rng, ref _pairFirst);
            _trip = HomingContract.NextTrip(level, beacon, _rng);
            ResetForTrip();

            // Vista de cabina, en la base.
            _phase = Phase.Outbound;
            yield return StartCoroutine(FadeView(0f, 1f, 0.35f));
            if (!_introShown)
            {
                _introShown = true;
                _toast.Show("Recuerda cada giro", "Al final vuelves a casa sin mapa", NeuroStyle.Sky, 2.2f);
            }
            if (beacon && !_beaconHintShown)
            {
                _beaconHintShown = true;
                StartCoroutine(DelayedToast(_tripIndex == 0 ? 2.6f : 0.2f, "Hoy hay faro", "Está lejísimos: solo te dice hacia dónde miras"));
            }
            SetPrompt("Sigue las señales", Color.white);
            SetDetail("");

            for (int i = 0; i < _trip.Legs; i++)
                yield return StartCoroutine(Leg(i));

            // La vuelta.
            _phase = Phase.Aim;
            _signal.gameObject.SetActive(false);
            yield return StartCoroutine(Aim());
            yield return StartCoroutine(TurnBy(_aimAngle));
            _phase = Phase.Advance;
            float traveled = 0f;
            yield return StartCoroutine(Advance(t => traveled = t));
            var outcome = HomingContract.Evaluate(_trip, _aimAngle, traveled);
            yield return StartCoroutine(Reveal(outcome, level, traveled));
            yield return StartCoroutine(FadeView(1f, 0f, 0.35f));
        }

        private IEnumerator DelayedToast(float delay, string title, string subtitle)
        {
            yield return StartCoroutine(Wait(delay));
            _toast.Show(title, subtitle, AmberColor, 2.4f);
        }

        private IEnumerator Leg(int i)
        {
            _nextCrystal = i;
            float cx = _trip.CrystalX[i], cy = _trip.CrystalY[i];
            var crystal = _crystals[i];
            crystal.gameObject.SetActive(true);
            crystal.anchoredPosition = new Vector2(cx, cy);
            _crystalImgs[i].color = Color.white;
            _crystalGlows[i].gameObject.SetActive(true);

            // Señal en el borde de la vista, hacia el cristal.
            _signal.gameObject.SetActive(true);
            ApplyCamera(); // la señal aparece ya en su lugar (no un cuadro en el centro)
            _signalGem.sprite = HomingSprites.Crystal(i);
            _signalRing.color = NeuroStyle.WithAlpha(HomingSprites.CrystalColors[i % HomingSprites.CrystalColors.Length], 0.9f);
            StartCoroutine(PopIn(_signal, 0.25f));
            Sfx(HomingSounds.Ping(), 0.45f);
            _signalTapped = false;
            if (!_signalHintShown) ShowHint("Toca la señal para ir al cristal");
            float waited = 0f;
            while (!_signalTapped && waited < SignalAutoGo)
            {
                waited += GameClock.DeltaTime;
                yield return null;
            }
            if (_signalTapped)
            {
                _signalHintShown = true;
                HideHint();
            }
            StartCoroutine(PopRect(_signal, 1.2f, 0.15f));

            // Gira hacia el cristal (en el primer tramo ya lo tiene delante) y vuela.
            float turn = HomingContract.Wrap(HomingContract.HeadingOf(cx - _px, cy - _py) - _heading);
            if (Mathf.Abs(turn) > 0.5f) yield return StartCoroutine(TurnBy(turn));
            _signal.gameObject.SetActive(false);
            yield return StartCoroutine(FlyTo(cx, cy));
            HideHint();

            // Recogido.
            _crystalsTotal++;
            _points += 20;
            UpdateHud();
            Sfx(HomingSounds.Crystal(i), 0.55f);
            GameFeel.Haptic(GameFeel.HapticKind.Light);
            var col = HomingSprites.CrystalColors[i % HomingSprites.CrystalColors.Length];
            StartCoroutine(UiFx.SparkBurst(_fxRect, _shipScreen, col, 14, 200f, 30f, 0.55f));
            StartCoroutine(UiFx.RingBurst(_fxRect, _shipScreen, col, 90f, 380f, 0.45f));
            StartCoroutine(FloatText(_shipScreen + new Vector2(0f, ShipSize * 0.8f), "+20", AmberColor));
            crystal.gameObject.SetActive(false);
            _crystalGlows[i].gameObject.SetActive(false);
            yield return StartCoroutine(Wait(0.35f));
        }

        private IEnumerator Aim()
        {
            _toast.Show("¡Hora de volver!", "Combustible justo para llegar a casa", AmberColor, 1.8f);
            SetPrompt("¿Hacia dónde está casa?", AmberColor);
            SetDetail("");
            _aimRoot.gameObject.SetActive(true);
            _aimArrow.gameObject.SetActive(false);
            StartCoroutine(PopIn(_aimRoot, 0.3f));
            _lockButton.gameObject.SetActive(false);
            _aimed = _lockPressed = false;
            _aimAngle = 0f;
            if (!_aimHintShown) ShowHint("Toca o arrastra para apuntar");
            while (!_lockPressed) yield return null;
            _aimHintShown = true;
            HideHint();
            Sfx(HomingSounds.Lock(), 0.5f);
            StartCoroutine(PopRect(_lockButton, 0.92f, 0.12f));
            yield return StartCoroutine(Wait(0.12f));
            _lockButton.gameObject.SetActive(false);
            _aimRoot.gameObject.SetActive(false);
        }

        private IEnumerator Advance(System.Action<float> done)
        {
            SetPrompt("Avanza hacia casa", AmberColor);
            SetDetail("Toca ¡AQUÍ! donde creas que está la base");
            _stopButton.gameObject.SetActive(true);
            StartCoroutine(PopIn(_stopButton, 0.25f));
            _stopPressed = false;
            if (!_stopHintShown) ShowHint("La base sigue en la niebla: calcula la distancia");
            float max = _trip.HomeDistance * HomingContract.MaxReturnFactor;
            float traveled = 0f;
            _moving = true;
            Sfx(HomingSounds.Turn(), 0.25f);
            while (!_stopPressed && traveled < max)
            {
                float step = HomingContract.Speed * GameClock.DeltaTime;
                traveled = Mathf.Min(max, traveled + step);
                HomingContract.Dir(_heading, out float dx, out float dy);
                _px = _trip.StartX + dx * traveled;
                _py = _trip.StartY + dy * traveled;
                yield return null;
            }
            _moving = false;
            _stopHintShown = true;
            HideHint();
            if (!_stopPressed)
            {
                Sfx(HomingSounds.FuelOut(), 0.5f);
                _toast.Show("Sin combustible", "La nave se detuvo sola", AmberColor, 1.2f);
            }
            else StartCoroutine(PopRect(_stopButton, 0.92f, 0.12f));
            yield return StartCoroutine(Wait(0.15f));
            _stopButton.gameObject.SetActive(false);
            done(traveled);
        }

        // ------------------------------------------------------------------ revelación

        private IEnumerator Reveal(HomingOutcome o, int level, float traveled)
        {
            _phase = Phase.Reveal;
            _outcomes.Add(o);
            _beacons.Add(_trip.Beacon);
            if (o.Hit) _hits++;
            if (o.Perfect) _perfects++;
            _streak = o.Hit ? _streak + 1 : 0;
            _bestStreak = Mathf.Max(_bestStreak, _streak);
            int pts = HomingContract.Points(o, level, _streak);
            _points += pts;
            _hud.SetStreak(_streak);

            SetPrompt("Vista desde arriba", Color.white);
            SetDetail("");
            Sfx(HomingSounds.MapReveal(), 0.5f);
            _beacon.gameObject.SetActive(false);

            // Mapa: ruta de ida, vuelta y vuelta justa, en coordenadas del mapa (giran y se achican con la cámara).
            float ex = _px, ey = _py, sx = _trip.StartX, sy = _trip.StartY;
            var outPts = new List<Vector2> { Vector2.zero };
            for (int i = 0; i < _trip.Legs; i++) outPts.Add(new Vector2(_trip.CrystalX[i], _trip.CrystalY[i]));

            // Encuadre: todo el viaje (base, cristales y dónde quedó) dentro del área del mapa.
            float minX = Mathf.Min(0f, ex), maxX = Mathf.Max(0f, ex), minY = Mathf.Min(0f, ey), maxY = Mathf.Max(0f, ey);
            foreach (var p in outPts)
            {
                minX = Mathf.Min(minX, p.x); maxX = Mathf.Max(maxX, p.x);
                minY = Mathf.Min(minY, p.y); maxY = Mathf.Max(maxY, p.y);
            }
            float mapW = _playW - 2f * (MarginU + 70f), mapH = _playH * 0.5f;
            float fit = Mathf.Min(mapW / Mathf.Max(1f, maxX - minX), mapH / Mathf.Max(1f, maxY - minY));
            fit = Mathf.Min(fit, 0.6f);
            var mapCenter = new Vector2(0f, -_playH * 0.02f);

            // La nave fija se vuelve la del mapa (misma posición y rumbo en pantalla al empezar: no hay salto).
            _shipRect.gameObject.SetActive(false);
            _mapShip.gameObject.SetActive(true);
            _mapShip.anchoredPosition = new Vector2(ex, ey);
            _mapShip.localRotation = Quaternion.Euler(0f, 0f, -_heading);

            float fromX = _camX, fromY = _camY, fromRot = _camRot, fromScale = _camScale;
            Vector2 fromPivot = _camPivot;
            float toX = (minX + maxX) * 0.5f, toY = (minY + maxY) * 0.5f;
            float rotDelta = HomingContract.Wrap(0f - fromRot);
            _mapMode = true;
            float t = 0f;
            const float zoom = 1.5f;
            while (t < zoom)
            {
                t += GameClock.DeltaTime;
                float k = Mathf.Clamp01(t / zoom);
                float e = k * k * (3f - 2f * k);
                _camX = Mathf.Lerp(fromX, toX, e);
                _camY = Mathf.Lerp(fromY, toY, e);
                _camRot = fromRot + rotDelta * e;
                _camScale = Mathf.Exp(Mathf.Lerp(Mathf.Log(fromScale), Mathf.Log(fit), e));
                _camPivot = Vector2.Lerp(fromPivot, mapCenter, e);
                _dustFade = 1f - e;
                _fogFade = 1f - e;
                DrawPartial(_outLine, outPts, e);
                DrawPartial(_outGlow, outPts, e);
                yield return null;
            }
            _dustFade = _fogFade = 0f;
            DrawPartial(_outLine, outPts, 1f);
            DrawPartial(_outGlow, outPts, 1f);
            foreach (var c in _crystals) c.gameObject.SetActive(false);
            for (int i = 0; i < _trip.Legs; i++)
            {
                _crystals[i].gameObject.SetActive(true);
                _crystalImgs[i].color = new Color(1f, 1f, 1f, 0.9f);
            }

            // La vuelta que hiciste (sol) y la vuelta justa (crema, tenue).
            var back = new List<Vector2> { new Vector2(sx, sy), new Vector2(ex, ey) };
            var ideal = new List<Vector2> { new Vector2(sx, sy), Vector2.zero };
            _backLine.gameObject.SetActive(true);
            _idealLine.gameObject.SetActive(true);
            t = 0f;
            while (t < 0.6f)
            {
                t += GameClock.DeltaTime;
                float k = UiFx.EaseOutCubic(Mathf.Clamp01(t / 0.6f));
                DrawPartial(_idealLine, ideal, k);
                DrawPartial(_backLine, back, k);
                yield return null;
            }

            // ¿Llegó a casa? Acorde de llegada, marca ✓/✗ con texto, y el tramo que faltó.
            _gapLine.gameObject.SetActive(!o.Perfect);
            _gapLine.color = o.Hit ? GoodColor : BadColor;
            if (!o.Perfect) DrawPartial(_gapLine, new List<Vector2> { new Vector2(ex, ey), Vector2.zero }, 1f);
            _endMark.gameObject.SetActive(true);
            _endMark.anchoredPosition = new Vector2(ex, ey);
            _endMarkImg.sprite = o.Hit ? AnswerMarkSprite.Check() : AnswerMarkSprite.Cross();
            StartCoroutine(PopIn(_endMark, 0.3f));
            var home = ToView(0f, 0f);
            if (o.Perfect)
            {
                Sfx(HomingSounds.Arrival(0), 0.6f);
                GameFeel.Correct(_streak);
                SetPrompt(_streak >= 3 ? $"¡Llegada perfecta! · racha {_streak}" : "¡Llegada perfecta!", AmberColor);
                StartCoroutine(UiFx.SparkBurst(_fxRect, home, NeuroStyle.Sun, 24, 260f, 40f, 0.7f));
                StartCoroutine(UiFx.RingBurst(_fxRect, home, NeuroStyle.Sun, 120f, 520f, 0.5f));
            }
            else if (o.Hit)
            {
                Sfx(HomingSounds.Arrival(1), 0.55f);
                GameFeel.Correct(_streak);
                SetPrompt(_streak >= 3 ? $"¡Llegaste a casa! · racha {_streak}" : "¡Llegaste a casa!", GoodColor);
                StartCoroutine(UiFx.SparkBurst(_fxRect, home, GoodColor, 14, 200f, 32f, 0.5f));
            }
            else
            {
                Sfx(HomingSounds.Arrival(2), 0.5f);
                SetPrompt(o.ErrorRatio < 0.6f ? "¡Casi! Quedaste cerca" : "Esta vez quedaste lejos", AmberColor);
            }
            SetDetail(DetailFor(o));
            StartCoroutine(PulseHome());
            if (pts > 0) StartCoroutine(FloatText(ToView(ex, ey) + new Vector2(0f, 90f), "+" + pts, NeuroStyle.Sun));

            var change = _dda.Register(o.Hit);
            if (change == DdaChange.Up)
                _toast.Show("¡Subes de nivel!", HomingContract.LevelNews(_dda.Level), GoodColor, 1.3f);
            else if (change == DdaChange.Down || _dda.Struggling)
                _toast.Show("Con calma", "En cada giro, fíjate cuánto gira el polvo", AmberColor, 1.3f);
            UpdateHud();

            yield return StartCoroutine(Wait(o.Hit ? 3.0f : 3.6f));
        }

        /// <summary>Qué pasó en esta vuelta, separado en sus dos partes (rumbo y distancia), en palabras.</summary>
        private static string DetailFor(HomingOutcome o)
        {
            float a = Mathf.Abs(o.AngleError);
            string rumbo = a < 5f ? "Rumbo justo" : $"Rumbo: {Mathf.RoundToInt(a)}° a la {(o.AngleError > 0f ? "derecha" : "izquierda")}";
            string dist = o.DistanceRatio < 0.9f ? "te quedaste corto" : o.DistanceRatio > 1.1f ? "te pasaste" : "distancia justa";
            return rumbo + " · " + dist;
        }

        private IEnumerator PulseHome()
        {
            if (!Motion.Decorative)
            {
                // Tinte fijo del hogar un momento (misma duración) y vuelve con fundido.
                _baseGlow.color = NeuroStyle.WithAlpha(NeuroStyle.Sun, 0.75f);
                yield return Motion.Hold(0.9f);
                _baseGlow.color = NeuroStyle.WithAlpha(NeuroStyle.Sun, 0.35f);
                yield break;
            }
            float t = 0f;
            while (t < 0.9f)
            {
                t += GameClock.DeltaTime;
                float k = Mathf.Clamp01(t / 0.9f);
                _baseGlow.color = NeuroStyle.WithAlpha(NeuroStyle.Sun, 0.35f + 0.4f * Mathf.Sin(k * Mathf.PI));
                yield return null;
            }
        }

        // ------------------------------------------------------------------ movimiento

        private IEnumerator TurnBy(float delta)
        {
            if (Mathf.Abs(delta) < 0.5f) yield break;
            if (Mathf.Abs(delta) > 12f) Sfx(HomingSounds.Turn(), 0.35f);
            float from = _heading;
            float seconds = Mathf.Max(0.4f, Mathf.Abs(delta) / HomingContract.TurnRate(_trip.Level));
            float t = 0f;
            while (t < seconds)
            {
                t += GameClock.DeltaTime;
                float k = Mathf.Clamp01(t / seconds);
                _heading = HomingContract.Wrap(from + delta * (k * k * (3f - 2f * k)));
                yield return null;
            }
            _heading = HomingContract.Wrap(from + delta);
        }

        private IEnumerator FlyTo(float x, float y)
        {
            float fx = _px, fy = _py;
            float len = Mathf.Sqrt((x - fx) * (x - fx) + (y - fy) * (y - fy));
            float d = 0f;
            _moving = true;
            while (d < len)
            {
                d = Mathf.Min(len, d + HomingContract.Speed * GameClock.DeltaTime);
                float k = d / len;
                _px = Mathf.Lerp(fx, x, k);
                _py = Mathf.Lerp(fy, y, k);
                yield return null;
            }
            _moving = false;
        }

        private void ResetForTrip()
        {
            _px = _py = _heading = 0f;
            _mapMode = false;
            _camScale = 1f;
            _dustFade = _fogFade = 1f;
            _shipRect.gameObject.SetActive(true);
            _mapShip.gameObject.SetActive(false);
            _endMark.gameObject.SetActive(false);
            _aimRoot.gameObject.SetActive(false);
            _lockButton.gameObject.SetActive(false);
            _stopButton.gameObject.SetActive(false);
            _signal.gameObject.SetActive(false);
            foreach (var c in _crystals) c.gameObject.SetActive(false);
            foreach (var g in _crystalGlows) g.gameObject.SetActive(false);
            _outLine.SetPoints(new List<Vector2>(), Vector2.zero);
            _outGlow.SetPoints(new List<Vector2>(), Vector2.zero);
            _idealLine.gameObject.SetActive(false);
            _backLine.gameObject.SetActive(false);
            _gapLine.gameObject.SetActive(false);
            _beacon.gameObject.SetActive(_trip != null && _trip.Beacon);
            _baseGlow.color = NeuroStyle.WithAlpha(NeuroStyle.Sun, 0.35f);
            // El polvo se reparte de nuevo alrededor de la base.
            for (int i = 0; i < _dustPos.Count; i++)
                _dustPos[i] = new Vector2(((float)_rng.NextDouble() - 0.5f) * DustTile, ((float)_rng.NextDouble() - 0.5f) * DustTile);
        }

        private IEnumerator FadeView(float from, float to, float seconds)
        {
            var group = _view.GetComponent<CanvasGroup>();
            float t = 0f;
            while (t < seconds)
            {
                t += GameClock.DeltaTime;
                group.alpha = Mathf.Lerp(from, to, Mathf.Clamp01(t / seconds));
                yield return null;
            }
            group.alpha = to;
        }

        // ------------------------------------------------------------------ cuadro a cuadro

        private void Update()
        {
            UpdateClock();
            if (_phase == Phase.Idle || _phase == Phase.Done || _view == null) return;
            if (!_mapMode)
            {
                _camX = _px;
                _camY = _py;
                _camRot = _heading;
                _camScale = 1f;
                _camPivot = _shipScreen;
            }
            ApplyCamera();
            UpdateEngine();
            HandleInput();
        }

        private void ApplyCamera()
        {
            _pivot.anchoredPosition = _camPivot;
            _pivot.localRotation = Quaternion.Euler(0f, 0f, _camRot);
            _pivot.localScale = new Vector3(_camScale, _camScale, 1f);
            _content.anchoredPosition = new Vector2(-_camX, -_camY);
            // Lo del mapa se "des-gira": las sombras duras siempre caen hacia abajo. En el mapa, los íconos no se achican
            // tanto como el mapa (se siguen leyendo).
            var counter = Quaternion.Euler(0f, 0f, -_camRot);
            _baseRect.localRotation = counter;
            _baseRect.localScale = Vector3.one * IconScale(BaseSize, 170f);
            _endMark.localRotation = counter;
            _endMark.localScale = Vector3.one * IconScale(110f, 72f);
            _mapShip.localScale = Vector3.one * IconScale(ShipSize * 0.8f, 110f);
            for (int i = 0; i < _crystals.Count; i++)
            {
                var c = _crystals[i];
                if (!c.gameObject.activeSelf) continue;
                c.localRotation = counter;
                c.localScale = Vector3.one * IconScale(CrystalSize, 120f);
                float a = _mapMode ? 1f : Visibility(c.anchoredPosition.x, c.anchoredPosition.y);
                _crystalImgs[i].color = new Color(1f, 1f, 1f, _mapMode ? 0.9f : a);
                var g = _crystalGlows[i];
                if (g.gameObject.activeSelf)
                {
                    g.color = NeuroStyle.WithAlpha(HomingSprites.CrystalColors[i % HomingSprites.CrystalColors.Length],
                        a * (Motion.Decorative ? 0.45f + 0.15f * Mathf.Sin(GameClock.Time * 4f + i) : 0.45f)); // sin ReduceMotion: brillo fijo
                    g.rectTransform.anchoredPosition = c.anchoredPosition;
                }
            }
            // La base se ve entre la niebla solo de cerca (en el mapa, siempre).
            float baseA = _mapMode ? 1f : Visibility(0f, 0f);
            _baseImg.color = new Color(1f, 1f, 1f, baseA);
            if (!_mapMode) _baseGlow.color = NeuroStyle.WithAlpha(NeuroStyle.Sun, 0.35f * baseA);

            // Líneas del mapa (solo existen en la vista desde arriba): el grosor se mantiene en pantalla aunque el mapa
            // se achique.
            if (_mapMode)
            {
                float inv = 1f / Mathf.Max(0.05f, _camScale);
                _outGlow.Setup(RailLine.Style.Soft, 44f * inv);
                _outLine.Setup(RailLine.Style.Solid, 12f * inv);
                _idealLine.Setup(RailLine.Style.Solid, 7f * inv);
                _backLine.Setup(RailLine.Style.Solid, 12f * inv);
                _gapLine.Setup(RailLine.Style.Solid, 7f * inv);
            }

            UpdateDust();
            _fog.color = new Color(1f, 1f, 1f, _fogFade);
            _fog.rectTransform.anchoredPosition = _shipScreen;

            // Faro: en el infinito, solo cambia de lugar al girar.
            if (_beacon.gameObject.activeSelf && _trip != null)
            {
                float rel = _trip.BeaconBearing - _heading;
                HomingContract.Dir(rel, out float bx, out float by);
                _beacon.anchoredPosition = _shipScreen + new Vector2(bx, by) * BeaconRadius;
                _beaconGlow.color = NeuroStyle.WithAlpha(NeuroStyle.Sun, Motion.Decorative ? 0.4f + 0.15f * Mathf.Sin(GameClock.Time * 2.2f) : 0.4f);
            }

            // Señal del cristal en el borde de la vista.
            if (_signal.gameObject.activeSelf && _trip != null && _nextCrystal < _trip.Legs)
            {
                float cx = _trip.CrystalX[_nextCrystal], cy = _trip.CrystalY[_nextCrystal];
                float rel = HomingContract.HeadingOf(cx - _px, cy - _py) - _heading;
                HomingContract.Dir(rel, out float sx, out float sy);
                _signal.anchoredPosition = _shipScreen + new Vector2(sx, sy) * SignalRadius;
                float pulse = Motion.Decorative ? 1f + 0.08f * Mathf.Sin(GameClock.Time * 6f) : 1f;
                _signalRing.rectTransform.localScale = Vector3.one * pulse;
            }

            // Llama de la nave: encendida mientras avanza.
            float flame = _moving ? (Motion.Decorative ? 0.85f + 0.15f * Mathf.Sin(GameClock.Time * 40f) : 0.85f) : 0f; // encendida mientras avanza; sin titileo con ReduceMotion
            _flame.color = NeuroStyle.WithAlpha(NeuroStyle.Sun, 0.75f * flame);
            _flame.rectTransform.sizeDelta = new Vector2(ShipSize * 0.45f, ShipSize * (0.5f + 0.4f * flame));
        }

        /// <summary>En el mapa, los íconos no se achican tanto como el mapa: nunca quedan por debajo de
        /// <paramref name="minScreen"/> unidades en pantalla (en la cabina, tamaño normal).</summary>
        private float IconScale(float size, float minScreen) =>
            _mapMode ? Mathf.Max(1f, minScreen / (size * Mathf.Max(0.05f, _camScale))) : 1f;

        /// <summary>Cuánto se ve algo entre la niebla según su distancia a la nave (1 cerca, 0 lejos).</summary>
        private float Visibility(float x, float y)
        {
            float dx = x - _px, dy = y - _py;
            float d = Mathf.Sqrt(dx * dx + dy * dy);
            float k = Mathf.Clamp01((d - HomingContract.FogClear) / (HomingContract.FogGone - HomingContract.FogClear));
            return 1f - k * k * (3f - 2f * k);
        }

        private void UpdateDust()
        {
            float half = DustTile * 0.5f;
            for (int i = 0; i < _dust.Count; i++)
            {
                var p = _dustPos[i];
                // Envuelve alrededor de la cámara: el polvo nunca se acaba.
                float dx = p.x - _camX, dy = p.y - _camY;
                if (dx > half) p.x -= DustTile; else if (dx < -half) p.x += DustTile;
                if (dy > half) p.y -= DustTile; else if (dy < -half) p.y += DustTile;
                _dustPos[i] = p;
                var img = _dust[i];
                img.rectTransform.anchoredPosition = p;
                img.color = new Color(0.85f, 0.88f, 1f, _dustAlpha[i] * _dustFade);
            }
        }

        private void UpdateEngine()
        {
            if (_engine == null) return;
            float dt = GameClock.DeltaTime;
            if (dt <= 0f)
            {
                if (_engine.isPlaying) _engine.Pause();
                return;
            }
            float target = SoundAllowed && _moving ? 0.16f : 0f;
            _engine.volume = Mathf.MoveTowards(_engine.volume, target, dt * 0.5f);
            if (_engine.volume > 0.001f && !_engine.isPlaying) _engine.Play();
            else if (_engine.volume <= 0.001f && target <= 0f && _engine.isPlaying) _engine.Stop();
        }

        private bool SoundAllowed => GameFeel.SoundOn && (_config == null || _config.config == null || _config.config.sound_enabled);

        private void Sfx(AudioClip clip, float volume)
        {
            if (SoundAllowed) _audioSource.PlayOneShot(clip, volume);
        }

        // ------------------------------------------------------------------ entrada

        private void HandleInput()
        {
            if (GameClock.DeltaTime <= 0f) return;
            bool down = Input.GetMouseButton(0);
            bool began = Input.GetMouseButtonDown(0);
            if (!down && !began) return;
            Vector2 screen = Input.mousePosition;
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(_view, screen, null, out var local)) return;

            switch (_phase)
            {
                case Phase.Outbound:
                    if (!began || !_signal.gameObject.activeSelf || _signalTapped) break;
                    // Vale tocar la señal o el cristal si ya se ve (con margen generoso).
                    bool nearSignal = (local - _signal.anchoredPosition).magnitude <= 190f;
                    var c = ToView(_trip.CrystalX[_nextCrystal], _trip.CrystalY[_nextCrystal]);
                    bool nearCrystal = (local - c).magnitude <= 170f;
                    if (nearSignal || nearCrystal) _signalTapped = true;
                    break;

                case Phase.Aim:
                    if (_lockButton.gameObject.activeSelf && Hit(_lockButton, screen))
                    {
                        if (began && _aimed) _lockPressed = true;
                        break;
                    }
                    var rel = local - _shipScreen;
                    if (rel.magnitude < AimDeadZone) break;
                    _aimAngle = HomingContract.HeadingOf(rel.x, rel.y);
                    _aimArrow.localRotation = Quaternion.Euler(0f, 0f, -_aimAngle);
                    if (!_aimed)
                    {
                        _aimed = true;
                        _aimArrow.gameObject.SetActive(true);
                        _lockButton.gameObject.SetActive(true);
                        StartCoroutine(PopIn(_lockButton, 0.25f));
                        HideHint();
                    }
                    break;

                case Phase.Advance:
                    if (began && Hit(_stopButton, screen)) _stopPressed = true;
                    break;
            }
        }

        private static bool Hit(RectTransform r, Vector2 screen) =>
            RectTransformUtility.RectangleContainsScreenPoint(r, screen, null);

        private void UpdateClock()
        {
            if (!Endless || _phase == Phase.Idle || _phase == Phase.Done || _endsAt <= 0f) return;
            float left = _endsAt - GameClock.Time;
            SetTimerFraction(Mathf.Clamp01(left / HomingContract.RetoSeconds));
            int whole = Mathf.CeilToInt(left);
            if (whole <= 5 && whole >= 1 && whole != _lastTickSecond)
            {
                _lastTickSecond = whole;
                GameFeel.Tick();
            }
        }

        // ------------------------------------------------------------------ utilidades

        /// <summary>De coordenadas del mapa a la pantalla (espacio de <see cref="_view"/>).</summary>
        private Vector2 ToView(float x, float y)
        {
            float dx = (x - _camX) * _camScale, dy = (y - _camY) * _camScale;
            float r = _camRot * Mathf.Deg2Rad;
            float c = Mathf.Cos(r), s = Mathf.Sin(r);
            return _camPivot + new Vector2(dx * c - dy * s, dx * s + dy * c);
        }

        private static void DrawPartial(RailLine line, List<Vector2> pts, float fraction)
        {
            var outPts = new List<Vector2>();
            if (pts.Count < 2 || fraction <= 0f)
            {
                line.SetPoints(outPts, Vector2.zero);
                return;
            }
            float total = 0f;
            for (int i = 1; i < pts.Count; i++) total += (pts[i] - pts[i - 1]).magnitude;
            float want = total * Mathf.Clamp01(fraction), acc = 0f;
            outPts.Add(pts[0]);
            for (int i = 1; i < pts.Count; i++)
            {
                float seg = (pts[i] - pts[i - 1]).magnitude;
                if (acc + seg >= want)
                {
                    float k = seg <= 0f ? 1f : (want - acc) / seg;
                    outPts.Add(Vector2.Lerp(pts[i - 1], pts[i], k));
                    break;
                }
                acc += seg;
                outPts.Add(pts[i]);
            }
            if (outPts.Count < 2) outPts.Add(pts[0] + (pts[1] - pts[0]).normalized * 0.5f);
            line.SetPoints(outPts, Vector2.zero);
        }

        private void SetPrompt(string text, Color color)
        {
            _prompt.text = text;
            _prompt.color = color;
        }

        private void SetDetail(string text) => _detail.text = text;

        private void ShowHint(string text)
        {
            _hint.text = text;
            _hint.gameObject.SetActive(true);
        }

        private void HideHint() => _hint.gameObject.SetActive(false);

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
                r.anchoredPosition = pos + new Vector2(0f, 70f * (Motion.Decorative ? UiFx.EaseOutCubic(k) : 0f));
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
            _moving = false;
            if (_engine != null) _engine.Stop();

            float meanErr = HomingContract.MeanErrorPct(_outcomes);
            int score = HomingContract.Score(meanErr, _dda.PeakLevel);
            ShowResult(score, meanErr);

            var along = new float[_outcomes.Count];
            var lateral = new float[_outcomes.Count];
            var beacon = new int[_outcomes.Count];
            for (int i = 0; i < _outcomes.Count; i++)
            {
                along[i] = _outcomes[i].Along;
                lateral[i] = _outcomes[i].Lateral;
                beacon[i] = _beacons[i] ? 1 : 0;
            }
            var telemetry = new StroopTelemetry
            {
                user_id = _config.user_id,
                game_id = _config.game_id,
                session_metrics = new StroopSessionMetrics
                {
                    correct_trials = _hits,
                    total_trials = _outcomes.Count,
                    calculated_score = score,
                    average_response_time_ms = 0,
                    level = _config.config.level,
                    timed = _config.config.timed,
                    end_rating = _dda.RatingNormalized,
                    mode_trials = _dda.ScoredTrials,
                    mode_hits = _dda.ScoredCorrect,
                    peak_level = _dda.PeakLevel,
                    homing_error_pct = meanErr,
                    homing_along = along,
                    homing_lateral = lateral,
                    homing_beacon = beacon,
                    homing_perfect = _perfects
                }
            };
            NativeBridge.ForwardTelemetryToPlatform(JsonUtility.ToJson(telemetry));
            yield break;
        }

        private void ShowResult(int score, float meanErr)
        {
            _exit.Show();
            SetPrompt("Fin de la exploración", GoodColor);
            SetDetail("");
            _resultRoot.Find("Title").GetComponent<Text>().text = score >= 85 ? "¡Brújula de oro!" : score >= 65 ? "¡Buen regreso!" : "Exploración completa";
            _resultRoot.Find("Detail").GetComponent<Text>().text = $"{_hits} de {_outcomes.Count} llegadas a casa · {_perfects} perfectas";
            _resultRoot.Find("Extra").GetComponent<Text>().text = meanErr >= 0f ? $"A {meanErr:0}% de casa, en promedio" : $"{_crystalsTotal} cristales";
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

            var engineGo = new GameObject("Engine");
            engineGo.transform.SetParent(transform, false);
            _engine = engineGo.AddComponent<AudioSource>();
            _engine.playOnAwake = false;
            _engine.loop = true;
            _engine.spatialBlend = 0f;
            _engine.volume = 0f;
            _engine.clip = TrafficSounds.EngineLoop();

            var canvasGo = new GameObject("HomingCanvas");
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
            WorldBackdrop.Build(bgRect, GameWorld.DeepSpace);

            var safeGo = new GameObject("SafeAreaContent");
            safeGo.transform.SetParent(canvasGo.transform, false);
            _safe = safeGo.AddComponent<RectTransform>();
            ApplySafeArea(_safe);

            var playGo = new GameObject("Play");
            playGo.transform.SetParent(_safe, false);
            _play = playGo.AddComponent<RectTransform>();
            Stretch(_play);

            BuildView();

            _hud = new GameHud(_safe, "Rumbo a Casa", MarginU, this);
            BuildTimer();

            _prompt = CenteredText(_play, "Prompt", 62, Color.white);
            NeuroStyle.ClayText(_prompt, 3.5f, 5f);
            _detail = CenteredText(_play, "Detail", 42, new Color(1f, 1f, 1f, 0.9f));
            NeuroStyle.ClayText(_detail, 2.5f, 3f);
            _hint = CenteredText(_play, "Hint", 40, new Color(1f, 1f, 1f, 0.9f));
            NeuroStyle.ClayText(_hint, 2.5f, 3f);
            _hint.gameObject.SetActive(false);

            _lockButton = ClayButton("Lock", "FIJAR RUMBO", NeuroStyle.Lime);
            _stopButton = ClayButton("Stop", "¡AQUÍ!", NeuroStyle.Sun);

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

        private void BuildView()
        {
            _view = Layer(_play, "View");
            var group = _view.gameObject.AddComponent<CanvasGroup>();
            group.alpha = 0f;
            group.blocksRaycasts = false;
            group.interactable = false;

            // Cámara: el pivote gira y escala; el contenido lleva las coordenadas del mapa.
            var pivotGo = new GameObject("Pivot");
            pivotGo.transform.SetParent(_view, false);
            _pivot = pivotGo.AddComponent<RectTransform>();
            _pivot.anchorMin = _pivot.anchorMax = new Vector2(0.5f, 0.5f);
            _pivot.sizeDelta = Vector2.zero;
            var contentGo = new GameObject("Content");
            contentGo.transform.SetParent(_pivot, false);
            _content = contentGo.AddComponent<RectTransform>();
            _content.anchorMin = _content.anchorMax = new Vector2(0.5f, 0.5f);
            _content.sizeDelta = Vector2.zero;

            // Polvo de estrellas (el paso del polvo es lo que se "siente" al girar y al avanzar).
            var dustRng = new System.Random(11);
            for (int i = 0; i < DustCount; i++)
            {
                var d = NewImage(_content, "Dust", DiscSprite.Get());
                float s = 5f + (float)dustRng.NextDouble() * 9f;
                d.rectTransform.sizeDelta = new Vector2(s, s);
                d.gameObject.SetActive(true);
                _dust.Add(d);
                _dustPos.Add(Vector2.zero);
                _dustAlpha.Add(0.25f + 0.45f * (float)dustRng.NextDouble());
            }

            // Líneas del mapa (debajo de los íconos).
            _outGlow = Line("OutGlow", RailLine.Style.Soft, NeuroStyle.WithAlpha(NeuroStyle.Lime, 0.35f));
            _outLine = Line("OutLine", RailLine.Style.Solid, NeuroStyle.Lime);
            _idealLine = Line("Ideal", RailLine.Style.Solid, NeuroStyle.WithAlpha(NeuroStyle.Cream, 0.55f));
            _backLine = Line("Back", RailLine.Style.Solid, NeuroStyle.Sun);
            _gapLine = Line("Gap", RailLine.Style.Solid, NeuroStyle.Coral);

            // Base (casa) con su resplandor cálido.
            _baseGlow = NewImage(_content, "BaseGlow", RadialGlowSprite.Get());
            _baseGlow.rectTransform.sizeDelta = new Vector2(BaseSize * 2.2f, BaseSize * 2.2f);
            _baseGlow.gameObject.SetActive(true);
            _baseImg = NewImage(_content, "Base", HomingSprites.Base());
            _baseRect = _baseImg.rectTransform;
            _baseRect.sizeDelta = new Vector2(BaseSize, BaseSize);
            _baseImg.gameObject.SetActive(true);

            for (int i = 0; i < 5; i++)
            {
                var glow = NewImage(_content, "CrystalGlow", RadialGlowSprite.Get());
                glow.rectTransform.sizeDelta = new Vector2(CrystalSize * 2.6f, CrystalSize * 2.6f);
                _crystalGlows.Add(glow);
                var c = NewImage(_content, "Crystal", HomingSprites.Crystal(i));
                c.rectTransform.sizeDelta = new Vector2(CrystalSize, CrystalSize);
                _crystals.Add(c.rectTransform);
                _crystalImgs.Add(c);
            }

            var mapShip = NewImage(_content, "MapShip", HomingSprites.Ship(shadow: false));
            _mapShip = mapShip.rectTransform;
            _mapShip.sizeDelta = new Vector2(ShipSize * 0.8f, ShipSize * 0.8f);
            _endMarkImg = NewImage(_content, "EndMark", AnswerMarkSprite.Check());
            _endMark = _endMarkImg.rectTransform;
            _endMark.sizeDelta = new Vector2(110f, 110f);

            // Niebla alrededor de la nave (no gira: es redonda).
            _fog = NewImage(_view, "Fog", HomingSprites.Fog());
            _fog.rectTransform.sizeDelta = new Vector2(HomingSprites.FogRadius * 2f, HomingSprites.FogRadius * 2f);
            _fog.gameObject.SetActive(true);

            // Faro (en el infinito) y la señal del próximo cristal.
            var beaconGo = new GameObject("Beacon");
            beaconGo.transform.SetParent(_view, false);
            _beacon = beaconGo.AddComponent<RectTransform>();
            _beacon.anchorMin = _beacon.anchorMax = new Vector2(0.5f, 0.5f);
            _beacon.sizeDelta = new Vector2(BeaconSize, BeaconSize);
            _beaconGlow = NewImage(_beacon, "Glow", RadialGlowSprite.Get());
            _beaconGlow.rectTransform.sizeDelta = new Vector2(BeaconSize * 3f, BeaconSize * 3f);
            _beaconGlow.gameObject.SetActive(true);
            var star = NewImage(_beacon, "Star", HomingSprites.Beacon());
            Stretch(star.rectTransform);
            star.gameObject.SetActive(true);
            var beaconLabel = MakeText(_beacon, "Label", 36, TextAnchor.MiddleCenter, NeuroStyle.Sun, 0f, 0f);
            NeuroStyle.ClayText(beaconLabel, 2.5f, 3f);
            beaconLabel.text = "faro";
            var bl = beaconLabel.rectTransform;
            bl.anchorMin = bl.anchorMax = new Vector2(0.5f, 0f);
            bl.pivot = new Vector2(0.5f, 1f);
            bl.sizeDelta = new Vector2(200f, 50f);
            bl.anchoredPosition = new Vector2(0f, -2f);
            beaconGo.SetActive(false);

            var signalGo = new GameObject("Signal");
            signalGo.transform.SetParent(_view, false);
            _signal = signalGo.AddComponent<RectTransform>();
            _signal.anchorMin = _signal.anchorMax = new Vector2(0.5f, 0.5f);
            _signal.sizeDelta = new Vector2(140f, 140f);
            _signalRing = NewImage(_signal, "Ring", RingSprite.Get());
            _signalRing.rectTransform.sizeDelta = new Vector2(150f, 150f);
            _signalRing.gameObject.SetActive(true);
            _signalGem = NewImage(_signal, "Gem", HomingSprites.Crystal(0));
            _signalGem.rectTransform.sizeDelta = new Vector2(84f, 84f);
            _signalGem.gameObject.SetActive(true);
            signalGo.SetActive(false);

            // Dial y flecha para apuntar a casa (giran con la nave, no con el mapa).
            var aimGo = new GameObject("Aim");
            aimGo.transform.SetParent(_view, false);
            _aimRoot = aimGo.AddComponent<RectTransform>();
            _aimRoot.anchorMin = _aimRoot.anchorMax = new Vector2(0.5f, 0.5f);
            _aimRoot.sizeDelta = new Vector2(DialSize, DialSize);
            _dial = NewImage(_aimRoot, "Dial", HomingSprites.Dial());
            Stretch(_dial.rectTransform);
            _dial.color = NeuroStyle.WithAlpha(NeuroStyle.Cream, 0.55f);
            _dial.gameObject.SetActive(true);
            var arrow = NewImage(_aimRoot, "Arrow", HomingSprites.AimArrow());
            _aimArrow = arrow.rectTransform;
            _aimArrow.pivot = new Vector2(0.5f, HomingSprites.ArrowTailFraction);
            _aimArrow.sizeDelta = new Vector2(300f, 300f);
            _aimArrow.anchoredPosition = Vector2.zero;
            aimGo.SetActive(false);

            // La nave, fija en la pantalla (el espacio gira a su alrededor).
            var shipGo = new GameObject("Ship");
            shipGo.transform.SetParent(_view, false);
            _shipRect = shipGo.AddComponent<RectTransform>();
            _shipRect.anchorMin = _shipRect.anchorMax = new Vector2(0.5f, 0.5f);
            _shipRect.sizeDelta = new Vector2(ShipSize, ShipSize);
            _flame = NewImage(_shipRect, "Flame", RadialGlowSprite.Get());
            _flame.rectTransform.anchoredPosition = new Vector2(0f, -ShipSize * 0.5f);
            _flame.gameObject.SetActive(true);
            var ship = NewImage(_shipRect, "Body", HomingSprites.Ship());
            Stretch(ship.rectTransform);
            ship.gameObject.SetActive(true);

            _fxRect = Layer(_view, "Fx");
        }

        private RailLine Line(string name, RailLine.Style style, Color color)
        {
            var go = new GameObject(name);
            go.transform.SetParent(_content, false);
            var r = go.AddComponent<RectTransform>();
            r.anchorMin = r.anchorMax = new Vector2(0.5f, 0.5f);
            r.sizeDelta = Vector2.zero;
            var line = go.AddComponent<RailLine>();
            line.Setup(style, 12f);
            line.color = color;
            return line;
        }

        private RectTransform ClayButton(string name, string label, Color color)
        {
            var go = new GameObject("Button" + name);
            go.transform.SetParent(_play, false);
            var r = go.AddComponent<RectTransform>();
            r.anchorMin = r.anchorMax = new Vector2(0.5f, 0.5f);
            r.sizeDelta = new Vector2(600f, 170f);
            var img = go.AddComponent<Image>();
            img.sprite = RoundedRectSprite.Get(64);
            img.type = Image.Type.Sliced;
            img.color = color;
            img.raycastTarget = false;
            NeuroStyle.ClayFrame(img, 6f, 14f);
            var t = MakeText(r, "Label", 64, TextAnchor.MiddleCenter, NeuroStyle.Ink, 0f, 0f);
            t.text = label;
            Stretch(t.rectTransform);
            BestFit(t, 36);
            go.SetActive(false);
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

            // Aviso y detalle arriba (texto suelto, debajo del marcador); la nave un poco más abajo del centro.
            float y = top - (GameHud.Height + 50f);
            _prompt.rectTransform.sizeDelta = new Vector2(contentW, 90f);
            _prompt.rectTransform.anchoredPosition = new Vector2(0f, y - 45f);
            _detail.rectTransform.sizeDelta = new Vector2(contentW, 64f);
            _detail.rectTransform.anchoredPosition = new Vector2(0f, y - 120f);

            _shipScreen = new Vector2(0f, -_playH * 0.04f);
            _shipRect.anchoredPosition = _shipScreen;
            _aimRoot.anchoredPosition = _shipScreen;
            _camPivot = _shipScreen;

            float buttonY = bottom + 150f;
            _lockButton.anchoredPosition = new Vector2(0f, buttonY);
            _stopButton.anchoredPosition = new Vector2(0f, buttonY);
            _hint.rectTransform.sizeDelta = new Vector2(contentW, 64f);
            _hint.rectTransform.anchoredPosition = new Vector2(0f, buttonY + 150f);
        }

        private void UpdateHud()
        {
            _hud.SetLevel(_dda.PresentedLevel);
            if (Endless) _hud.SetPoints(_points);
            else _hud.SetInfo($"Viaje {Mathf.Min(_outcomes.Count + 1, HomingContract.PrecisionTrips)} de {HomingContract.PrecisionTrips}");
        }
    }
}
