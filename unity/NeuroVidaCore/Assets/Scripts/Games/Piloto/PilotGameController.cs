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

namespace NeuroVida.Games.Piloto
{
    /// <summary>
    /// "Piloto Estelar": juego estrella de multitarea (ver <see cref="PilotContract"/>). La nave vuela por una ruta de
    /// balizas que serpentea; con el pulgar (tocar o arrastrar en la franja de abajo) se la mantiene dentro, y con el
    /// otro dedo se atrapan solo las señales de la misión, que aparecen un instante en el espacio.
    /// <list type="bullet">
    /// <item>Piloto automático (15 s): solo señales. Mide la tarea sola para el costo de multitarea.</item>
    /// <item>"¡A los mandos!": las dos tareas a la vez, cada una con su dificultad adaptativa (DDA común x2).</item>
    /// <item>Hiperimpulso: racha de señales + ruta limpia = puntos x2 y las estrellas pasan a hiperespacio.</item>
    /// </list>
    /// Reto = 90 s; Precisión = vuelo más tranquilo que termina tras 24 señales. Telemetría: <see cref="StroopTelemetry"/>
    /// con <c>multitask_cost</c>.
    /// </summary>
    public class PilotGameController : GameControllerBase
    {
        public const string GameId = PilotContract.GameId;

        private const float UnitsPerDp = 3f;
        private const float MarginU = 60f;
        private const float Spacing = 46f;          // distancia entre balizas de la ruta
        private const float PathScale = 1900f;      // unidades de lienzo por "altura de pantalla" en CenterAt
        private const float ShipSize = 176f;
        private const float SignalSize = 160f;
        private const int SignalPool = 3;
        private const int TrailPool = 12;

        private static readonly Color GoodColor = NeuroStyle.Lime;
        private static readonly Color BadColor = NeuroStyle.Coral;
        private static readonly Color AmberColor = NeuroStyle.Sun;
        private static readonly Color LaneColor = NeuroStyle.Sky;

        private enum Phase { Idle, Autopilot, Manual, Done }

        private struct Segment
        {
            public float D, Center, Half;
        }

        private sealed class ActiveSignal
        {
            public RectTransform Rect;
            public Image Icon, Ring, Mark;
            public CanvasGroup Group;
            public PilotSignal Data;
            public float SpawnAt, ExpiresAt;
            public bool Live, Manual;
            /// <summary>Terminando su animación de salida: todavía no se puede reusar.</summary>
            public bool Busy;
        }

        private System.Random _rng;
        private AdaptiveDifficulty _driveDda, _signalDda;
        private Phase _phase = Phase.Idle;
        private bool Endless => _config != null && _config.config.timed;
        private bool Precision => !Endless;

        // misión
        private int _missionShape, _missionVariant;

        // pilotaje
        private readonly List<Segment> _segments = new List<Segment>();
        private float _traveled, _speed, _amp, _half;
        private float _shipX = 0.5f, _shipTargetX = 0.5f, _shipVx;
        private int _steerFinger = -1;
        private bool _mouseSteer;
        private bool _inLane = true;
        private float _windowT, _windowIn;
        private float _manualTime, _manualInLane;
        private int _laneStreak;
        private float _laneAccum;
        private bool _labelFading;

        // señales
        private readonly List<ActiveSignal> _signals = new List<ActiveSignal>();
        private float _nextSignalAt;
        private int _autoHits, _autoTargets, _autoFa, _autoNonTargets;
        private int _hits, _targets, _fa, _nonTargets, _correctRejections, _manualResolved;
        private int _streak, _bestStreak, _points;
        private long _rtSum;
        private int _rtCount;

        // tiempo
        private float _flightStart, _manualStart, _endsAt;
        private float _boostUntil;
        private int _lastTickSecond = -1;
        private bool _ended;

        // UI
        private RectTransform _safe, _play, _fxRect, _shipRect, _controlRect, _bannerRect, _timerBg, _timerFill, _laneRoot, _trailRoot;
        private Image _shipImage, _shipGlow, _vignetteL, _vignetteR, _controlImage, _missionIcon;
        private Text _controlLabel, _bannerTitle, _bannerSub, _bigText;
        private StarfieldFx _stars;
        private readonly List<Image> _beaconsL = new List<Image>();
        private readonly List<Image> _beaconsR = new List<Image>();
        private readonly List<Image> _bands = new List<Image>();
        private readonly List<Image> _trail = new List<Image>();
        private readonly List<float> _trailAge = new List<float>();
        private int _trailNext;
        private float _trailEmitAt;
        private PhasePill _pill;
        private Toast _toast;
        private ExitButton _exit;
        private GameHud _hud;
        private CountdownScreen _countdown;
        private float _playW, _playH, _shipY, _controlTop, _signalBottom, _signalTop;

        // ------------------------------------------------------------------ sesión

        public void StartSession(SequenceInitConfig config)
        {
            _config = config;
            _rng = new System.Random();
            var age = DdaUserProfileConfig.ParseAgeBand(config.config.age_band);
            float start = AdaptiveDifficulty.StartRating(config.config, PilotContract.MaxLevel);
            _driveDda = new AdaptiveDifficulty(PilotContract.MaxLevel, age, start, stepUp: 0.14f, useReaction: false);
            _signalDda = new AdaptiveDifficulty(PilotContract.MaxLevel, age, start, stepUp: 0.14f, useReaction: config.config.timed);
            (_missionShape, _missionVariant) = PilotContract.PickMission(_rng);

            _phase = Phase.Idle;
            _ended = false;
            _segments.Clear();
            _traveled = 0f;
            _speed = PilotContract.ScrollSpeed(_driveDda.PresentedLevel, Precision);
            _amp = PilotContract.Curviness(_driveDda.PresentedLevel) * 0.6f;
            _half = PilotContract.LaneHalfWidth(_driveDda.PresentedLevel);
            _shipX = _shipTargetX = 0.5f;
            _shipVx = 0f;
            _steerFinger = -1;
            _mouseSteer = false;
            _inLane = true;
            _windowT = _windowIn = _manualTime = _manualInLane = 0f;
            _laneStreak = 0;
            _laneAccum = 0f;
            _labelFading = false;
            _controlLabel.color = new Color(1f, 1f, 1f, 0.7f);
            _autoHits = _autoTargets = _autoFa = _autoNonTargets = 0;
            _hits = _targets = _fa = _nonTargets = _correctRejections = _manualResolved = 0;
            _streak = _bestStreak = _points = 0;
            _rtSum = 0;
            _rtCount = 0;
            _boostUntil = 0f;
            _lastTickSecond = -1;
            foreach (var s in _signals) Retire(s);

            _resultRoot.gameObject.SetActive(false);
            _bigText.gameObject.SetActive(false);
            _exit.Hide();
            _missionIcon.sprite = SymbolSprite.Get((ShapeKind)PilotContract.Shapes[_missionShape], _missionVariant);
            _timerBg.gameObject.SetActive(Endless);
            _hud.SetStreak(0);

            StopAllCoroutines();
            StartCoroutine(GameLoop());
        }

        private IEnumerator GameLoop()
        {
            _safe.gameObject.SetActive(false);
            yield return StartCoroutine(_countdown.Play("Piloto Estelar", Assessment.Subtitle("Prepárate"), () => _safe.gameObject.SetActive(true)));
            _safe.gameObject.SetActive(true);
            yield return null;
            ApplySafeArea(_safe);
            Canvas.ForceUpdateCanvases();
            Layout();
            FillPath();

            _flightStart = GameClock.Time;
            _endsAt = _flightStart + PilotContract.FlightSeconds;
            _nextSignalAt = _flightStart + 1.2f;
            _phase = Phase.Autopilot;
            UpdateHud();
            _pill.Set("Piloto automático · solo atrapa tus señales", LaneColor);
            StartCoroutine(PulseBanner());

            while (_phase == Phase.Autopilot && GameClock.Time < _flightStart + PilotContract.AutopilotSeconds) yield return null;
            yield return StartCoroutine(TakeControl());

            while (!_ended) yield return null;
            yield return StartCoroutine(FinishGame());
        }

        /// <summary>Paso del piloto automático a los mandos: aviso grande, la franja de control late.</summary>
        private IEnumerator TakeControl()
        {
            _phase = Phase.Manual;
            _manualStart = GameClock.Time;
            _pill.Set("¡A los mandos! Guía la nave por la ruta", AmberColor);
            GameFeel.LevelUp();
            GameFeel.Haptic(GameFeel.HapticKind.Firm);
            _bigText.text = "¡A LOS MANDOS!";
            _bigText.gameObject.SetActive(true);
            StartCoroutine(UiFx.RingBurst(_fxRect, new Vector2(0f, _shipY), AmberColor, 160f, 900f, 0.6f));
            float t = 0f;
            const float seconds = 1.1f;
            while (t < seconds)
            {
                t += GameClock.DeltaTime;
                float k = Mathf.Clamp01(t / seconds);
                _bigText.rectTransform.localScale = Vector3.one * (Motion.Decorative ? Mathf.LerpUnclamped(0.6f, 1f, UiFx.EaseOutBack(Mathf.Clamp01(k * 3f))) : 1f); // sin ReduceMotion: sin rebote
                _bigText.color = new Color(1f, 1f, 1f, k < 0.7f ? 1f : 1f - (k - 0.7f) / 0.3f);
                float pulse = Motion.Decorative ? 0.10f + 0.12f * Mathf.Abs(Mathf.Sin(k * Mathf.PI * 3f)) : 0.16f; // sin ReduceMotion: tinte fijo
                _controlImage.color = NeuroStyle.WithAlpha(AmberColor, pulse);
                yield return null;
            }
            _bigText.gameObject.SetActive(false);
            _controlImage.color = new Color(1f, 1f, 1f, 0.06f);
        }

        // ------------------------------------------------------------------ bucle por cuadro

        private void Update()
        {
            if (_phase != Phase.Autopilot && _phase != Phase.Manual) return;
            float dt = GameClock.DeltaTime;
            if (dt <= 0f) return; // en pausa
            float now = GameClock.Time;

            HandleInput();
            UpdateFlight(dt);
            UpdateSignals(now);
            UpdateEffects(dt, now);
            UpdateClock(now);
        }

        private void HandleInput()
        {
            // Dedos: el que empieza en la franja de control guía la nave hasta que se levanta; un toque que empieza
            // arriba intenta atrapar una señal. Se lee Input directo (varios dedos a la vez).
            if (Input.touchCount > 0)
            {
                bool steering = false;
                for (int i = 0; i < Input.touchCount; i++)
                {
                    var touch = Input.GetTouch(i);
                    if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(_play, touch.position, null, out var local)) continue;
                    if (touch.phase == TouchPhase.Began)
                    {
                        if (local.y <= _controlTop) _steerFinger = touch.fingerId;
                        else TryCatch(local);
                    }
                    if (touch.fingerId == _steerFinger)
                    {
                        if (touch.phase == TouchPhase.Ended || touch.phase == TouchPhase.Canceled) _steerFinger = -1;
                        else
                        {
                            SteerTo(local.x);
                            steering = true;
                        }
                    }
                }
                if (!steering && _steerFinger >= 0 && !FingerAlive(_steerFinger)) _steerFinger = -1;
                return;
            }

            // Mouse (Editor): arrastrar en la franja de control guía; clic arriba atrapa.
            if (RectTransformUtility.ScreenPointToLocalPointInRectangle(_play, Input.mousePosition, null, out var m))
            {
                if (Input.GetMouseButtonDown(0))
                {
                    if (m.y <= _controlTop) _mouseSteer = true;
                    else TryCatch(m);
                }
                if (Input.GetMouseButtonUp(0)) _mouseSteer = false;
                if (_mouseSteer && Input.GetMouseButton(0)) SteerTo(m.x);
            }
        }

        private static bool FingerAlive(int fingerId)
        {
            for (int i = 0; i < Input.touchCount; i++)
                if (Input.GetTouch(i).fingerId == fingerId) return true;
            return false;
        }

        private void SteerTo(float localX)
        {
            if (_phase != Phase.Manual) return;
            _shipTargetX = Mathf.Clamp01(localX / _playW + 0.5f);
            if (!_labelFading && GameClock.Time - _manualStart > 3f)
            {
                _labelFading = true;
                StartCoroutine(FadeControlLabel());
            }
        }

        private void UpdateFlight(float dt)
        {
            int dl = _driveDda.PresentedLevel;
            float targetSpeed = PilotContract.ScrollSpeed(dl, Precision) * (Boosted ? 1.12f : 1f);
            _speed = Mathf.MoveTowards(_speed, targetSpeed, 120f * dt);
            _traveled += _speed * dt;
            FillPath();

            float center = CenterAtShip(out float half);
            if (_phase == Phase.Autopilot)
                _shipTargetX = center + (Motion.Decorative ? 0.04f * Mathf.Sin(GameClock.Time * 1.3f) : 0f); // sin ReduceMotion: no se mece

            // La nave sigue al dedo con un resorte suave (rápida, sin teletransportarse).
            float before = _shipX;
            float follow = 1f - Mathf.Exp(-dt * 14f);
            float step = (_shipTargetX - _shipX) * follow;
            float maxStep = 2.6f * dt;
            _shipX += Mathf.Clamp(step, -maxStep, maxStep);
            _shipVx = (_shipX - before) / dt;

            bool inLane = PilotContract.InLane(_shipX, center, half);
            if (_phase == Phase.Manual)
            {
                _manualTime += dt;
                _windowT += dt;
                if (inLane)
                {
                    _manualInLane += dt;
                    _windowIn += dt;
                    // 5 puntos por segundo dentro de la ruta (el doble en hiperimpulso).
                    _laneAccum += dt;
                    while (_laneAccum >= 0.2f)
                    {
                        _laneAccum -= 0.2f;
                        _points += Boosted ? 2 : 1;
                    }
                }
                if (_inLane && !inLane)
                {
                    PlayTone(196f, 0.16f, 0.12f);
                    _pill.Set("¡Vuelve a la ruta!", BadColor);
                }
                else if (!_inLane && inLane) _pill.Set("En la ruta", GoodColor);

                if (_windowT >= PilotContract.DriveWindowSeconds)
                {
                    bool pass = _windowIn / _windowT >= PilotContract.DriveWindowPass;
                    _laneStreak = pass ? _laneStreak + 1 : 0;
                    var change = _driveDda.Register(pass);
                    if (change == DdaChange.Up)
                    {
                        _toast.Show("Más velocidad", "La ruta se estrecha", GoodColor, 0.9f);
                        GameFeel.LevelUp();
                    }
                    else if (change == DdaChange.Down) _toast.Show("Con calma", "Ruta más ancha", AmberColor, 0.9f);
                    _windowT = _windowIn = 0f;
                    UpdateHud();
                    TryBoost();
                }
            }
            _inLane = inLane;

            // Nave: posición, alabeo según la velocidad lateral, tinte si está fuera de la ruta.
            _shipRect.anchoredPosition = new Vector2((_shipX - 0.5f) * _playW, _shipY);
            _shipRect.localRotation = Quaternion.Euler(0f, 0f, Motion.Decorative ? Mathf.Clamp(-_shipVx * 22f, -16f, 16f) : 0f); // sin ReduceMotion: sin alabeo
            _shipImage.color = inLane || _phase == Phase.Autopilot ? Color.white : new Color(1f, 0.78f, 0.74f, 1f);

            LayoutPath(center);
        }

        /// <summary>Crea tramos de ruta por delante y descarta los que ya quedaron atrás.</summary>
        private void FillPath()
        {
            float ahead = _playH + 200f;
            float lastD = _segments.Count > 0 ? _segments[_segments.Count - 1].D : _traveled - _shipY - _playH;
            int dl = _driveDda.PresentedLevel;
            float targetAmp = PilotContract.Curviness(dl);
            float targetHalf = PilotContract.LaneHalfWidth(dl);
            while (lastD < _traveled + ahead)
            {
                lastD += Spacing;
                // Cambios de dificultad suaves: la ruta ya dibujada no se deforma, se afina de a poco.
                _amp = Mathf.MoveTowards(_amp, targetAmp, 0.004f);
                _half = Mathf.MoveTowards(_half, targetHalf, 0.0025f);
                _segments.Add(new Segment { D = lastD, Half = _half, Center = PilotContract.CenterAt(lastD / PathScale, _amp, _half) });
            }
            float behind = _traveled - (_playH * 0.5f + _shipY) - 200f;
            while (_segments.Count > 0 && _segments[0].D < behind) _segments.RemoveAt(0);
        }

        /// <summary>Centro y medio ancho de la ruta a la altura de la nave (interpolados entre tramos).</summary>
        private float CenterAtShip(out float half)
        {
            for (int i = 1; i < _segments.Count; i++)
            {
                if (_segments[i].D < _traveled) continue;
                var a = _segments[i - 1];
                var b = _segments[i];
                float k = Mathf.InverseLerp(a.D, b.D, _traveled);
                half = Mathf.Lerp(a.Half, b.Half, k);
                return Mathf.Lerp(a.Center, b.Center, k);
            }
            half = _half;
            return 0.5f;
        }

        private void LayoutPath(float centerAtShip)
        {
            int used = 0;
            float top = _playH * 0.5f + 80f;
            float bottom = -_playH * 0.5f - 80f;
            foreach (var seg in _segments)
            {
                float y = _shipY + (seg.D - _traveled);
                if (y < bottom || y > top || used >= _beaconsL.Count) continue;
                // Profundidad: las balizas lejanas (arriba) son más chicas y tenues.
                float depth = Mathf.InverseLerp(bottom, top, y);
                float scale = Mathf.Lerp(1.25f, 0.6f, depth);
                float alpha = Mathf.Lerp(1f, 0.45f, depth);
                bool nearShip = Mathf.Abs(y - _shipY) < 160f;
                Color beacon = nearShip && !_inLane && _phase == Phase.Manual ? BadColor : (Boosted ? NeuroStyle.Sun : LaneColor);
                bool big = Mathf.RoundToInt(seg.D / Spacing) % 3 == 0;
                float size = (big ? 26f : 16f) * scale;

                float lx = (seg.Center - seg.Half - 0.5f) * _playW;
                float rx = (seg.Center + seg.Half - 0.5f) * _playW;
                Place(_beaconsL[used], new Vector2(lx, y), size, NeuroStyle.WithAlpha(beacon, alpha));
                Place(_beaconsR[used], new Vector2(rx, y), size, NeuroStyle.WithAlpha(beacon, alpha));

                var band = _bands[used];
                band.gameObject.SetActive(true);
                var br = band.rectTransform;
                br.anchoredPosition = new Vector2((seg.Center - 0.5f) * _playW, y);
                br.sizeDelta = new Vector2(Mathf.Max(0f, rx - lx), Spacing + 1f);
                band.color = NeuroStyle.WithAlpha(Boosted ? NeuroStyle.Sun : LaneColor, 0.055f * alpha);
                used++;
            }
            for (int i = used; i < _beaconsL.Count; i++)
            {
                _beaconsL[i].gameObject.SetActive(false);
                _beaconsR[i].gameObject.SetActive(false);
                _bands[i].gameObject.SetActive(false);
            }
        }

        private static void Place(Image img, Vector2 pos, float size, Color color)
        {
            img.gameObject.SetActive(true);
            var r = img.rectTransform;
            r.anchoredPosition = pos;
            r.sizeDelta = new Vector2(size, size);
            img.color = color;
        }

        // ------------------------------------------------------------------ señales

        private void UpdateSignals(float now)
        {
            bool manual = _phase == Phase.Manual;
            if (now >= _nextSignalAt && !(Precision && manual && _manualResolved + LiveCount() >= PilotContract.PrecisionSignals))
            {
                var free = _signals.Find(s => !s.Live && !s.Busy);
                if (free != null) Spawn(free, now, manual);
                _nextSignalAt = now + PilotContract.SignalGap(_signalDda.PresentedLevel, Precision) * (0.8f + 0.4f * (float)_rng.NextDouble());
            }

            foreach (var s in _signals)
            {
                if (!s.Live) continue;
                float life = Mathf.Clamp01((now - s.SpawnAt) / (s.ExpiresAt - s.SpawnAt));
                s.Ring.fillAmount = 1f - life;
                float pop = Mathf.Clamp01((now - s.SpawnAt) / 0.14f);
                s.Rect.localScale = Vector3.one * (Motion.Decorative ? Mathf.LerpUnclamped(0.4f, 1f, UiFx.EaseOutBack(pop)) : 1f); // aparece a su tamaño
                if (now >= s.ExpiresAt) Expire(s);
            }

            if (Precision && manual && _manualResolved >= PilotContract.PrecisionSignals && LiveCount() == 0) _ended = true;
        }

        private int LiveCount()
        {
            int n = 0;
            foreach (var s in _signals) if (s.Live) n++;
            return n;
        }

        private void Spawn(ActiveSignal s, float now, bool manual)
        {
            s.Data = PilotContract.NextSignal(_signalDda.PresentedLevel, _missionShape, _missionVariant, _rng);
            // Que no caiga encima de otra señal viva.
            for (int tries = 0; tries < 4 && Overlaps(s.Data); tries++)
                s.Data = PilotContract.NextSignal(_signalDda.PresentedLevel, _missionShape, _missionVariant, _rng);
            s.SpawnAt = now;
            s.ExpiresAt = now + PilotContract.ExposureMs(_signalDda.PresentedLevel, Precision) / 1000f;
            s.Live = true;
            s.Manual = manual;
            s.Icon.sprite = SymbolSprite.Get((ShapeKind)PilotContract.Shapes[s.Data.Shape], s.Data.Variant);
            s.Icon.color = Color.white;
            s.Ring.color = NeuroStyle.WithAlpha(Color.white, 0.75f);
            s.Mark.gameObject.SetActive(false);
            s.Rect.anchoredPosition = SignalPosition(s.Data);
            s.Rect.localScale = Vector3.one * 0.4f;
            s.Group.alpha = 1f;
            s.Rect.gameObject.SetActive(true);
            PlayTone(s.Data.Peripheral ? 1046f : 880f, 0.05f, 0.05f); // mismo "blip" para todas: no delata cuál es
        }

        private bool Overlaps(PilotSignal d)
        {
            var p = SignalPosition(d);
            foreach (var o in _signals)
                if (o.Live && Vector2.Distance(o.Rect.anchoredPosition, p) < SignalSize * 1.2f) return true;
            return false;
        }

        private Vector2 SignalPosition(PilotSignal d)
        {
            float x = (d.X - 0.5f) * (_playW - SignalSize);
            float y = Mathf.Lerp(_signalBottom, _signalTop, d.Y);
            return new Vector2(x, y);
        }

        private void TryCatch(Vector2 local)
        {
            ActiveSignal best = null;
            float bestDist = SignalSize * 0.66f; // área de toque generosa (≥ 48 dp)
            foreach (var s in _signals)
            {
                if (!s.Live) continue;
                float d = Vector2.Distance(local, s.Rect.anchoredPosition);
                if (d < bestDist)
                {
                    bestDist = d;
                    best = s;
                }
            }
            if (best == null) return;
            float rtMs = (GameClock.Time - best.SpawnAt) * 1000f;
            if (best.Data.IsTarget) Caught(best, rtMs);
            else FalseAlarm(best);
        }

        private void Caught(ActiveSignal s, float rtMs)
        {
            s.Live = false;
            Count(s, hit: true);
            _streak++;
            _bestStreak = Mathf.Max(_bestStreak, _streak);
            int pts = PilotContract.PointsForCatch(_streak, Boosted);
            _points += pts;
            _rtSum += (long)rtMs;
            _rtCount++;
            _hud.SetStreak(_streak);
            RegisterSignal(true, rtMs);
            GameFeel.Correct(_streak);
            s.Mark.sprite = AnswerMarkSprite.Check();
            s.Mark.gameObject.SetActive(true);
            StartCoroutine(UiFx.SparkBurst(_fxRect, s.Rect.anchoredPosition, NeuroStyle.Sun, 14, 240f, 40f, 0.5f));
            StartCoroutine(UiFx.RingBurst(_fxRect, s.Rect.anchoredPosition, Color.white, 120f, 360f, 0.4f));
            StartCoroutine(FloatText(s.Rect.anchoredPosition, "+" + pts, NeuroStyle.Sun));
            StartCoroutine(Vanish(s, 0.28f, 1.35f));
            TryBoost();
        }

        private void FalseAlarm(ActiveSignal s)
        {
            s.Live = false;
            Count(s, hit: false, falseAlarm: true);
            _streak = 0;
            _hud.SetStreak(0);
            RegisterSignal(false, -1f);
            GameFeel.Wrong();
            s.Icon.color = new Color(1f, 1f, 1f, 0.55f);
            s.Mark.sprite = AnswerMarkSprite.Cross();
            s.Mark.gameObject.SetActive(true);
            StartCoroutine(UiFx.Shake(18f, 0.3f, s.Rect));
            StartCoroutine(Flash(BadColor, 0.10f, 0.25f));
            StartCoroutine(Vanish(s, 0.45f, 0.9f));
        }

        private void Expire(ActiveSignal s)
        {
            s.Live = false;
            if (s.Data.IsTarget)
            {
                // Se escapó una señal de la misión: se marca y la racha se corta (sin sonido de error: fue una omisión).
                Count(s, hit: false);
                _streak = 0;
                _hud.SetStreak(0);
                RegisterSignal(false, -1f);
                s.Icon.color = new Color(1f, 1f, 1f, 0.5f);
                s.Mark.sprite = AnswerMarkSprite.Cross();
                s.Mark.gameObject.SetActive(true);
                StartCoroutine(Vanish(s, 0.4f, 0.85f));
            }
            else
            {
                Count(s, hit: false);
                _correctRejections++;
                RegisterSignal(true, -1f);
                StartCoroutine(Vanish(s, 0.18f, 0.8f));
            }
        }

        private void Count(ActiveSignal s, bool hit, bool falseAlarm = false)
        {
            if (s.Manual)
            {
                _manualResolved++;
                if (s.Data.IsTarget) { _targets++; if (hit) _hits++; }
                else { _nonTargets++; if (falseAlarm) _fa++; }
            }
            else
            {
                if (s.Data.IsTarget) { _autoTargets++; if (hit) _autoHits++; }
                else { _autoNonTargets++; if (falseAlarm) _autoFa++; }
            }
            if (Precision && s.Manual) UpdateHud();
        }

        private void RegisterSignal(bool correct, float rtMs)
        {
            var change = _signalDda.Register(correct, rtMs);
            if (change == DdaChange.Up)
            {
                _toast.Show("Señales más rápidas", _signalDda.Level >= 5 ? "Ahora también a los costados" : "Atento a los colores", GoodColor, 0.9f);
                GameFeel.LevelUp();
            }
            else if (_signalDda.Struggling) _toast.Show("Con calma", "Solo las de tu misión", AmberColor, 0.9f);
            UpdateHud();
        }

        private IEnumerator Vanish(ActiveSignal s, float hold, float endScale)
        {
            s.Busy = true;
            float t = 0f;
            while (t < hold)
            {
                t += GameClock.DeltaTime;
                float k = Mathf.Clamp01(t / hold);
                s.Group.alpha = 1f - k * k;
                s.Rect.localScale = Vector3.one * (Motion.Decorative ? Mathf.Lerp(1f, endScale, k) : 1f); // sin ReduceMotion: solo se desvanece
                yield return null;
            }
            s.Rect.gameObject.SetActive(false);
            s.Busy = false;
        }

        private void Retire(ActiveSignal s)
        {
            s.Live = false;
            s.Busy = false;
            if (s.Rect != null) s.Rect.gameObject.SetActive(false);
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
            const float seconds = 0.7f;
            while (e < seconds)
            {
                e += GameClock.DeltaTime;
                float k = Mathf.Clamp01(e / seconds);
                r.anchoredPosition = pos + new Vector2(0f, 60f + 90f * (Motion.Decorative ? UiFx.EaseOutCubic(k) : 0f));
                t.color = NeuroStyle.WithAlpha(color, 1f - k * k);
                yield return null;
            }
            Destroy(t.gameObject);
        }

        // ------------------------------------------------------------------ hiperimpulso y efectos

        private bool Boosted => GameClock.Time < _boostUntil;

        /// <summary>Racha de señales y ruta limpia a la vez = hiperimpulso (puntos x2 durante 6 s).</summary>
        private void TryBoost()
        {
            if (_phase != Phase.Manual || Boosted) return;
            if (_streak >= 5 && _laneStreak >= 4)
            {
                _boostUntil = GameClock.Time + 6f;
                _laneStreak = 0;
                _toast.Show("¡Hiperimpulso!", "Puntos x2", NeuroStyle.Sun, 1.1f);
                GameFeel.LevelUp();
                GameFeel.Haptic(GameFeel.HapticKind.Firm);
                StartCoroutine(UiFx.RingBurst(_fxRect, new Vector2(0f, _shipY), NeuroStyle.Sun, 200f, 1400f, 0.7f));
            }
        }

        private void UpdateEffects(float dt, float now)
        {
            // Estrellas: más rápido cuanto más rápido se vuela; hiperespacio durante el hiperimpulso.
            float speedK = Mathf.InverseLerp(PilotContract.ScrollSpeed(1, false), PilotContract.ScrollSpeed(PilotContract.MaxLevel, false), _speed);
            float warp = Boosted ? 0.85f : 0.12f + 0.3f * speedK;
            if (_stars != null) _stars.Warp = Mathf.MoveTowards(_stars.Warp, warp, dt * 1.5f);

            // Viñeta coral a los costados cuando la nave está fuera de la ruta.
            float target = !_inLane && _phase == Phase.Manual ? 0.55f : 0f;
            float a = Mathf.MoveTowards(_vignetteL.color.a, target, dt * 3f);
            _vignetteL.color = _vignetteR.color = NeuroStyle.WithAlpha(BadColor, a);

            // Brillo bajo la nave que respira; estela de motor.
            _shipGlow.color = NeuroStyle.WithAlpha(Boosted ? NeuroStyle.Sun : LaneColor, Motion.Decorative ? 0.30f + 0.08f * Mathf.Sin(now * 5f) : 0.30f);
            if (now >= _trailEmitAt && Motion.Decorative) // sin ReduceMotion: sin estela de motor
            {
                _trailEmitAt = now + 0.045f;
                var img = _trail[_trailNext];
                _trailAge[_trailNext] = 0f;
                img.gameObject.SetActive(true);
                img.rectTransform.anchoredPosition = _shipRect.anchoredPosition + new Vector2(0f, -ShipSize * 0.42f);
                _trailNext = (_trailNext + 1) % _trail.Count;
            }
            for (int i = 0; i < _trail.Count; i++)
            {
                if (!_trail[i].gameObject.activeSelf) continue;
                _trailAge[i] += dt;
                float k = _trailAge[i] / 0.4f;
                if (k >= 1f)
                {
                    _trail[i].gameObject.SetActive(false);
                    continue;
                }
                var r = _trail[i].rectTransform;
                r.anchoredPosition += new Vector2(0f, -_speed * dt);
                float size = Mathf.Lerp(60f, 18f, k);
                r.sizeDelta = new Vector2(size, size);
                _trail[i].color = NeuroStyle.WithAlpha(Boosted ? NeuroStyle.Sun : NeuroStyle.Coral, 0.55f * (1f - k));
            }
        }

        private void UpdateClock(float now)
        {
            if (!Endless) return;
            float left = _endsAt - now;
            SetTimerFraction(Mathf.Clamp01(left / PilotContract.FlightSeconds));
            int whole = Mathf.CeilToInt(left);
            if (whole <= 5 && whole >= 1 && whole != _lastTickSecond)
            {
                _lastTickSecond = whole;
                GameFeel.Tick();
            }
            if (left <= 0f) _ended = true;
        }

        private IEnumerator FadeControlLabel()
        {
            var c = _controlLabel.color;
            float t = 0f;
            while (t < 0.6f)
            {
                t += GameClock.DeltaTime;
                _controlLabel.color = NeuroStyle.WithAlpha(c, c.a * (1f - Mathf.Clamp01(t / 0.6f)));
                yield return null;
            }
            _controlLabel.color = NeuroStyle.WithAlpha(c, 0f);
        }

        private IEnumerator PulseBanner()
        {
            for (int i = 0; i < 2; i++) yield return StartCoroutine(PopRect(_bannerRect, 1.06f, 0.35f));
        }

        // ------------------------------------------------------------------ fin

        private IEnumerator FinishGame()
        {
            _phase = Phase.Done;
            foreach (var s in _signals) Retire(s);

            float? single = PilotContract.SignalAccuracy(_autoHits, _autoTargets, _autoFa, _autoNonTargets);
            float? dual = PilotContract.SignalAccuracy(_hits, _targets, _fa, _nonTargets);
            float lane = _manualTime > 0f ? _manualInLane / _manualTime : 0f;
            int score = PilotContract.Score(dual ?? 0f, lane);
            int cost = PilotContract.MultitaskCost(single, dual);
            int total = _autoTargets + _autoNonTargets + _targets + _nonTargets;
            int correct = _autoHits + (_autoNonTargets - _autoFa) + _hits + (_nonTargets - _fa);
            int avgMs = _rtCount > 0 ? (int)(_rtSum / _rtCount) : 0;

            _pill.Set("Aterrizaje", GoodColor);
            ShowResult(score, lane, cost);

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
                    end_rating = (_driveDda.RatingNormalized + _signalDda.RatingNormalized) * 0.5f,
                    // Desafío superado en Piloto = las dos tareas: se informa la de menos aciertos (pilotaje o señales).
                    mode_trials = PilotContract.WeakerOf(_driveDda.ScoredCorrect, _driveDda.ScoredTrials, _signalDda.ScoredCorrect, _signalDda.ScoredTrials).trials,
                    mode_hits = PilotContract.WeakerOf(_driveDda.ScoredCorrect, _driveDda.ScoredTrials, _signalDda.ScoredCorrect, _signalDda.ScoredTrials).hits,
                    peak_level = Mathf.Max(_driveDda.PeakLevel, _signalDda.PeakLevel),
                    multitask_cost = cost
                }
            };
            NativeBridge.ForwardTelemetryToPlatform(JsonUtility.ToJson(telemetry));
            yield break;
        }

        private void ShowResult(int score, float lane, int cost)
        {
            _exit.Show();
            _resultRoot.Find("Title").GetComponent<Text>().text = score >= 85 ? "¡Vuelo impecable!" : score >= 65 ? "¡Buen vuelo!" : "Vuelo completado";
            _resultRoot.Find("Detail").GetComponent<Text>().text = $"{_hits} de {_targets} señales · {Mathf.RoundToInt(lane * 100f)}% en la ruta";
            _resultRoot.Find("Extra").GetComponent<Text>().text = cost >= 0 ? $"Costo de multitarea {cost}%" : $"Mejor racha {_bestStreak}";
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

            var canvasGo = new GameObject("PilotCanvas");
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
            // Mundo "Hiperespacio": las estrellas nacen arriba y pasan junto a la nave (sensación de avance).
            WorldBackdrop.Build(bgRect, GameWorld.Hyperspace);
            _stars = bgRect.GetComponentInChildren<StarfieldFx>();

            var safeGo = new GameObject("SafeAreaContent");
            safeGo.transform.SetParent(canvasGo.transform, false);
            _safe = safeGo.AddComponent<RectTransform>();
            ApplySafeArea(_safe);

            _hud = new GameHud(_safe, "Piloto Estelar", MarginU, this);
            BuildBanner();
            BuildTimer();
            BuildPlayField();
            _pill = new PhasePill(_safe, this, UnitsPerDp, 21);

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

        /// <summary>Cartel de la misión: el ícono a atrapar, bien grande (forma + color, sin depender de leer).</summary>
        private void BuildBanner()
        {
            var go = new GameObject("MissionBanner");
            go.transform.SetParent(_safe, false);
            _bannerRect = go.AddComponent<RectTransform>();
            _bannerRect.anchorMin = _bannerRect.anchorMax = new Vector2(0.5f, 1f);
            _bannerRect.pivot = new Vector2(0.5f, 0.5f);
            var bg = go.AddComponent<Image>();
            bg.sprite = RoundedRectSprite.Get(64);
            bg.type = Image.Type.Sliced;
            bg.color = NeuroStyle.Surface;
            bg.raycastTarget = false;
            NeuroStyle.ClayFrame(bg, 4f, 8f);

            var iconBg = new GameObject("IconDisc");
            iconBg.transform.SetParent(go.transform, false);
            var ir = iconBg.AddComponent<RectTransform>();
            ir.anchorMin = ir.anchorMax = new Vector2(0f, 0.5f);
            ir.pivot = new Vector2(0.5f, 0.5f);
            ir.sizeDelta = new Vector2(124f, 124f);
            ir.anchoredPosition = new Vector2(84f, 0f);
            var disc = iconBg.AddComponent<Image>();
            disc.sprite = DiscSprite.Get();
            disc.color = NeuroStyle.WithAlpha(NeuroStyle.Cream, 0.14f);
            disc.raycastTarget = false;

            var icon = new GameObject("MissionIcon");
            icon.transform.SetParent(iconBg.transform, false);
            var mr = icon.AddComponent<RectTransform>();
            mr.anchorMin = new Vector2(0.02f, 0.02f);
            mr.anchorMax = new Vector2(0.98f, 0.98f);
            mr.offsetMin = mr.offsetMax = Vector2.zero;
            _missionIcon = icon.AddComponent<Image>();
            _missionIcon.raycastTarget = false;
            _missionIcon.preserveAspect = true;

            _bannerTitle = MakeText(go.transform, "Title", 50, TextAnchor.MiddleLeft, Color.white, 0f, 0f);
            NeuroStyle.ClayText(_bannerTitle, 3f, 4f);
            _bannerTitle.text = "MISIÓN: atrapa solo esta";
            var tr = _bannerTitle.rectTransform;
            tr.anchorMin = new Vector2(0f, 0.42f);
            tr.anchorMax = new Vector2(1f, 1f);
            tr.offsetMin = new Vector2(170f, 0f);
            tr.offsetMax = new Vector2(-24f, -6f);
            BestFit(_bannerTitle, 30);

            _bannerSub = MakeText(go.transform, "Sub", 36, TextAnchor.MiddleLeft, new Color(1f, 1f, 1f, 0.8f), 0f, 0f);
            _bannerSub.text = "Misma forma y mismo color. Ignora las demás.";
            var sr = _bannerSub.rectTransform;
            sr.anchorMin = new Vector2(0f, 0f);
            sr.anchorMax = new Vector2(1f, 0.46f);
            sr.offsetMin = new Vector2(170f, 8f);
            sr.offsetMax = new Vector2(-24f, 0f);
            BestFit(_bannerSub, 24);
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

        private void BuildPlayField()
        {
            var go = new GameObject("PlayField");
            go.transform.SetParent(_safe, false);
            _play = go.AddComponent<RectTransform>();
            _play.anchorMin = Vector2.zero;
            _play.anchorMax = Vector2.one;
            _play.pivot = new Vector2(0.5f, 0.5f);

            // Franja de control (abajo): donde va el pulgar que guía la nave.
            var ctl = new GameObject("ControlBand");
            ctl.transform.SetParent(_play, false);
            _controlRect = ctl.AddComponent<RectTransform>();
            _controlRect.anchorMin = new Vector2(0f, 0f);
            _controlRect.anchorMax = new Vector2(1f, 0f);
            _controlRect.pivot = new Vector2(0.5f, 0f);
            _controlImage = ctl.AddComponent<Image>();
            _controlImage.sprite = RoundedRectSprite.Get(64);
            _controlImage.type = Image.Type.Sliced;
            _controlImage.color = new Color(1f, 1f, 1f, 0.06f);
            _controlImage.raycastTarget = false;
            _controlLabel = MakeText(ctl.transform, "Label", 40, TextAnchor.MiddleCenter, new Color(1f, 1f, 1f, 0.7f), 0f, 0f);
            _controlLabel.text = "‹  Desliza aquí para guiar la nave  ›";
            Stretch(_controlLabel.rectTransform);
            BestFit(_controlLabel, 26);

            _laneRoot = Layer(_play, "Lane");
            int pool = 64;
            for (int i = 0; i < pool; i++)
            {
                _bands.Add(NewImage(_laneRoot, "Band", null));
            }
            for (int i = 0; i < pool; i++)
            {
                _beaconsL.Add(NewImage(_laneRoot, "BeaconL", DiscSprite.Get()));
                _beaconsR.Add(NewImage(_laneRoot, "BeaconR", DiscSprite.Get()));
            }

            // Viñetas de "fuera de la ruta" a los costados.
            _vignetteL = NewImage(_play, "VignetteL", RadialGlowSprite.Get());
            _vignetteR = NewImage(_play, "VignetteR", RadialGlowSprite.Get());
            foreach (var v in new[] { _vignetteL, _vignetteR })
            {
                v.color = new Color(0f, 0f, 0f, 0f);
                v.gameObject.SetActive(true);
            }

            _trailRoot = Layer(_play, "Trail");
            for (int i = 0; i < TrailPool; i++)
            {
                var img = NewImage(_trailRoot, "Trail", RadialGlowSprite.Get());
                _trail.Add(img);
                _trailAge.Add(1f);
            }

            var shipGo = new GameObject("Ship");
            shipGo.transform.SetParent(_play, false);
            _shipRect = shipGo.AddComponent<RectTransform>();
            _shipRect.anchorMin = _shipRect.anchorMax = new Vector2(0.5f, 0.5f);
            _shipRect.pivot = new Vector2(0.5f, 0.5f);
            _shipRect.sizeDelta = new Vector2(ShipSize, ShipSize);
            var glowGo = new GameObject("Glow");
            glowGo.transform.SetParent(shipGo.transform, false);
            var gr = glowGo.AddComponent<RectTransform>();
            gr.anchorMin = gr.anchorMax = new Vector2(0.5f, 0.5f);
            gr.sizeDelta = new Vector2(ShipSize * 2.2f, ShipSize * 2.2f);
            _shipGlow = glowGo.AddComponent<Image>();
            _shipGlow.sprite = RadialGlowSprite.Get();
            _shipGlow.raycastTarget = false;
            var bodyGo = new GameObject("Body");
            bodyGo.transform.SetParent(shipGo.transform, false);
            Stretch(bodyGo.AddComponent<RectTransform>());
            _shipImage = bodyGo.AddComponent<Image>();
            _shipImage.sprite = PilotShipSprite.Get();
            _shipImage.raycastTarget = false;

            for (int i = 0; i < SignalPool; i++) _signals.Add(BuildSignal(i));

            _fxRect = Layer(_play, "Fx");

            _bigText = MakeText(_play, "BigText", 110, TextAnchor.MiddleCenter, Color.white, 0f, 0f);
            NeuroStyle.ClayText(_bigText, 6f, 10f);
            var br = _bigText.rectTransform;
            br.anchorMin = br.anchorMax = new Vector2(0.5f, 0.5f);
            br.sizeDelta = new Vector2(1000f, 180f);
            BestFit(_bigText, 60);
            _bigText.gameObject.SetActive(false);
        }

        private ActiveSignal BuildSignal(int i)
        {
            var go = new GameObject("Signal" + i);
            go.transform.SetParent(_play, false);
            var rect = go.AddComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = new Vector2(SignalSize, SignalSize);
            var group = go.AddComponent<CanvasGroup>();
            group.blocksRaycasts = false;

            var halo = NewImage(rect, "Halo", RadialGlowSprite.Get());
            halo.rectTransform.anchorMin = halo.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            halo.rectTransform.sizeDelta = new Vector2(SignalSize * 1.9f, SignalSize * 1.9f);
            halo.color = NeuroStyle.WithAlpha(NeuroStyle.Cream, 0.22f);
            halo.gameObject.SetActive(true);

            // Anillo que se vacía: cuánto le queda a la señal.
            var ring = NewImage(rect, "Ring", RingSprite.Get());
            ring.rectTransform.anchorMin = ring.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            ring.rectTransform.sizeDelta = new Vector2(SignalSize * 1.18f, SignalSize * 1.18f);
            ring.type = Image.Type.Filled;
            ring.fillMethod = Image.FillMethod.Radial360;
            ring.fillOrigin = (int)Image.Origin360.Top;
            ring.fillClockwise = false;
            ring.gameObject.SetActive(true);

            var icon = NewImage(rect, "Icon", null);
            Stretch(icon.rectTransform);
            icon.preserveAspect = true;
            icon.gameObject.SetActive(true);

            var mark = NewImage(rect, "Mark", null);
            var mr = mark.rectTransform;
            mr.anchorMin = mr.anchorMax = new Vector2(0.82f, 0.82f);
            mr.sizeDelta = new Vector2(SignalSize * 0.5f, SignalSize * 0.5f);
            mark.color = Color.white;

            go.SetActive(false);
            return new ActiveSignal { Rect = rect, Icon = icon, Ring = ring, Mark = mark, Group = group };
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

        // ------------------------------------------------------------------ layout

        private void Layout()
        {
            Rect safe = _safe.rect;
            float sw = Mathf.Max(safe.width, 400f);
            float sh = Mathf.Max(safe.height, 800f);
            float contentW = sw - MarginU * 2f;

            float y = GameHud.Height + 10f;
            float bannerH = 150f;
            _bannerRect.sizeDelta = new Vector2(contentW, bannerH);
            _bannerRect.anchoredPosition = new Vector2(0f, -(y + bannerH / 2f));
            y += bannerH + 22f;
            _timerBg.sizeDelta = new Vector2(contentW, 16f);
            _timerBg.anchoredPosition = new Vector2(0f, -(y + 8f));
            y += 16f + 12f;

            // Zona de vuelo: todo lo que queda debajo.
            _play.offsetMin = Vector2.zero;
            _play.offsetMax = new Vector2(0f, -y);
            Canvas.ForceUpdateCanvases();
            _playW = _play.rect.width;
            _playH = _play.rect.height;

            float controlH = Mathf.Clamp(_playH * 0.24f, 240f, 380f);
            _controlRect.offsetMin = new Vector2(MarginU * 0.5f, 18f);
            _controlRect.offsetMax = new Vector2(-MarginU * 0.5f, 18f + controlH);
            _controlTop = -_playH * 0.5f + 18f + controlH;
            _shipY = _controlTop + ShipSize * 0.75f;
            _signalBottom = _shipY + ShipSize * 1.1f;
            _signalTop = _playH * 0.5f - SignalSize * 0.7f;

            float vh = _playH * 0.9f;
            _vignetteL.rectTransform.sizeDelta = _vignetteR.rectTransform.sizeDelta = new Vector2(520f, vh);
            _vignetteL.rectTransform.anchoredPosition = new Vector2(-_playW * 0.5f, 0f);
            _vignetteR.rectTransform.anchoredPosition = new Vector2(_playW * 0.5f, 0f);

            // Píldora de estado arriba dentro de la franja de control (la nave queda libre, justo encima).
            _pill.SetPosition(new Vector2(0f, -sh / 2f + 18f + controlH - 56f));
            _pill.Rect.SetAsLastSibling();
        }

        private void UpdateHud()
        {
            int level = Mathf.RoundToInt((_driveDda.PresentedLevel + _signalDda.PresentedLevel) * 0.5f);
            _hud.SetLevel(level);
            if (Endless) _hud.SetPoints(_points);
            else _hud.SetInfo(_phase == Phase.Manual ? $"{Mathf.Min(_manualResolved, PilotContract.PrecisionSignals)} de {PilotContract.PrecisionSignals}" : "Despegue");
        }
    }
}
