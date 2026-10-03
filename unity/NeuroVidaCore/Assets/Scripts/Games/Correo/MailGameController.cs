using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using NeuroVida.Bridge;
using NeuroVida.Contracts;
using NeuroVida.Games.Secuencia;  // RoundedRectSprite / RadialGlowSprite / RingSprite
using NeuroVida.Games.CambioChip; // ChipShipSprite (la nave de Piloto Estelar)
using NeuroVida.Games.Parejas;    // SymbolSprite (asteroides)
using NeuroVida.Games.Piloto;     // PilotContract: la ruta (ancho, curvas, velocidad)
using NeuroVida.Games.Shared;
using NeuroVida.Games.Trafico;    // TrafficSprites.Port: planetas de color con símbolo
using static NeuroVida.Games.Shared.UiKit;
using Motion = NeuroVida.Games.Shared.Motion; // UnityEngine.Motion también existe

namespace NeuroVida.Games.Correo
{
    /// <summary>
    /// "Correo Estelar": juego estrella de MEMORIA PARA LO PENDIENTE (memoria prospectiva; ver <see cref="MailContract"/>).
    /// Es el vuelo de Piloto Estelar (un pulgar guía la nave por la ruta y se recogen sobres) con encargos que hay que
    /// recordar en el momento justo, sin que nada los muestre durante el vuelo:
    /// <list type="bullet">
    /// <item><b>Hoja de ruta</b> antes de salir: "cuando pases un planeta coral, tócalo" (por lugar) y "cada 30 segundos,
    /// toca la radio" (por hora; el reloj va tapado y se destapa un momento al tocarlo).</item>
    /// <item><b>Vuelo</b> de 150 s: planetas que pasan a los costados (pocos son del encargo; desde el nivel 3, algunos de
    /// color parecido), la radio abajo a la derecha y el reloj arriba a la derecha.</item>
    /// </list>
    /// La nave lleva un ESCUDO de 3 segmentos (<see cref="ShipShield"/>): cada asteroide rompe uno y la nave se ve dañada;
    /// volar limpio lo repara; sin escudo, reparación de emergencia (lenta unos segundos). El vuelo nunca termina antes.
    /// Telemetría: <see cref="StroopTelemetry"/> con los campos <c>mail_*</c>.
    /// </summary>
    public class MailGameController : GameControllerBase
    {
        public const string GameId = MailContract.GameId;

        private const float UnitsPerDp = 3f;
        private const float MarginU = 60f;
        private const float Spacing = 46f;
        private const float PathScale = 1900f;
        private const float ShipSize = 176f;
        private const float PlanetSize = 170f;
        private const float EnvelopeSize = 96f;
        private const float RadioSize = 190f;
        private const float ClockSize = 140f;
        private const int TrailPool = 12;
        private const int PlanetPool = 6;
        private const int AsteroidPool = 6;
        private const float AsteroidSize = 130f;
        private const int EnvelopePool = 8;

        private static readonly Color GoodColor = NeuroStyle.Lime;
        private static readonly Color BadColor = NeuroStyle.Coral;
        private static readonly Color AmberColor = NeuroStyle.Sun;
        private static readonly Color LaneColor = NeuroStyle.Sky;

        private enum Phase { Idle, Brief, Flight, Done }

        private struct Segment
        {
            public float D, Center, Half;
        }

        private sealed class PlanetView
        {
            public RectTransform Rect;
            public Image Body, Glow, Mark;
            public MailPlanet Data;
            public float D, X;
            public bool Live, Delivered, Wrong;
        }

        private sealed class EnvelopeView
        {
            public RectTransform Rect;
            public Image Img;
            public float D, X;
            public bool Live;
        }

        private System.Random _rng;
        /// <summary>Dos dificultades, como en Piloto: la de los encargos (<see cref="_dda"/>: colores, parecidos, ritmo de
        /// planetas) y la del pilotaje (<see cref="_driveDda"/>: velocidad, curvas, ancho y asteroides).</summary>
        private AdaptiveDifficulty _dda, _driveDda;
        private Phase _phase = Phase.Idle;
        private bool Precision => _config != null && !_config.config.timed;
        private int _startLevel;

        // encargos del vuelo
        private int[] _targets;
        private float _period;
        private List<float> _radioTargets = new List<float>();
        private readonly HashSet<int> _answered = new HashSet<int>();
        private int _radioChecked;   // horas ya evaluadas (a tiempo o pasadas)

        // pilotaje
        private readonly List<Segment> _segments = new List<Segment>();
        private float _traveled, _speed, _amp, _half;
        private float _shipX = 0.5f, _shipTargetX = 0.5f, _shipVx;
        private int _steerFinger = -1;
        private bool _mouseSteer, _inLane = true, _labelFading;
        private float _flightTime, _inLaneTime;
        private float _windowT, _windowIn;
        private bool _windowHit;
        private int _stage;

        // asteroides (hay que esquivarlos)
        private sealed class AsteroidView
        {
            public RectTransform Rect;
            public Image Img;
            public float D, X, Spin;
            public bool Live, Hit;
        }
        private readonly List<AsteroidView> _asteroids = new List<AsteroidView>();
        private float _nextAsteroidAt, _hitUntil;
        private int _asteroidHits, _asteroidsPassed;

        // escudo de la nave (cuidarla: cada choque rompe un segmento; volar limpio lo repara)
        private const int SmokePool = 8;
        private const float PipSize = 78f;
        private readonly ShipShield _shield = new ShipShield();
        private RectTransform _shieldRoot;
        private readonly Image[] _pips = new Image[ShipShield.Max];
        private Image _damageImage;
        private readonly List<Image> _smoke = new List<Image>();
        private readonly List<float> _smokeAge = new List<float>();
        private int _smokeNext;
        private float _smokeEmitAt;
        private bool _wasEmergency;

        // planetas y sobres
        private readonly List<PlanetView> _planets = new List<PlanetView>();
        private readonly List<EnvelopeView> _envelopes = new List<EnvelopeView>();
        private float _nextPlanetAt, _nextEnvelopeD;
        private bool _lastWasTarget;
        private int _eventHits, _eventTotal, _commissions, _lureCommissions, _deliveryStreak;
        private int _radioHits, _radioOfftime;
        private readonly List<float> _clockChecks = new List<float>();
        private int _envelopesGot, _envelopesTotal, _envelopeStreak, _points;

        // tiempo
        private float _flightStart;
        private float _clockShownUntil, _lastPeek = -10f;

        // UI
        private RectTransform _safe, _play, _fxRect, _shipRect, _controlRect, _laneRoot, _trailRoot, _briefRoot, _goButton, _radioRect, _clockRect;
        private Image _shipImage, _shipGlow, _vignetteL, _vignetteR, _controlImage, _radioGlow, _clockImage;
        private Text _controlLabel, _bigText, _clockText;
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
        private float _playW, _playH, _shipY, _controlTop;

        // ------------------------------------------------------------------ sesión

        public void StartSession(SequenceInitConfig config)
        {
            _config = config;
            _rng = new System.Random();
            var age = DdaUserProfileConfig.ParseAgeBand(config.config.age_band);
            float start = AdaptiveDifficulty.StartRating(config.config, MailContract.MaxLevel);
            // Pocos registros por vuelo (un encargo cada ~10 s): pasos grandes.
            _dda = new AdaptiveDifficulty(MailContract.MaxLevel, age, start, stepUp: 0.5f, useReaction: false);
            // Pilotaje: ventanas de 1,5 s (muchas por vuelo), pasos chicos como en Piloto Estelar.
            _driveDda = new AdaptiveDifficulty(9, age, AdaptiveDifficulty.StartRating(config.config, 9), stepUp: 0.14f, useReaction: false);
            _startLevel = _dda.PresentedLevel;
            _targets = MailContract.PickTargets(_startLevel, _rng);
            _period = MailContract.RadioPeriod(_startLevel);
            _radioTargets = MailContract.RadioTargets(_period, MailContract.FlightSeconds);
            _answered.Clear();
            _radioChecked = 0;

            _phase = Phase.Idle;
            _segments.Clear();
            int fl = _driveDda.PresentedLevel;
            _traveled = 0f;
            _speed = PilotContract.ScrollSpeed(fl, Precision);
            _amp = PilotContract.Curviness(fl) * 0.6f;
            _half = PilotContract.LaneHalfWidth(fl);
            _shipX = _shipTargetX = 0.5f;
            _shipVx = 0f;
            _steerFinger = -1;
            _mouseSteer = false;
            _inLane = true;
            _labelFading = false;
            _controlLabel.color = new Color(1f, 1f, 1f, 0.7f);
            _flightTime = _inLaneTime = _windowT = _windowIn = 0f;
            _windowHit = false;
            _stage = 0;
            _hitUntil = 0f;
            _asteroidHits = _asteroidsPassed = 0;
            foreach (var a in _asteroids) RetireAsteroid(a);
            _shield.Start(0f);
            _wasEmergency = false;
            RefreshShield();
            _shieldRoot.gameObject.SetActive(false);
            foreach (var sm in _smoke) sm.gameObject.SetActive(false);
            _eventHits = _eventTotal = _commissions = _lureCommissions = _deliveryStreak = 0;
            _radioHits = _radioOfftime = 0;
            _clockChecks.Clear();
            _envelopesGot = _envelopesTotal = _envelopeStreak = _points = 0;
            _lastWasTarget = false;
            _clockShownUntil = 0f;
            _lastPeek = -10f;
            foreach (var p in _planets) RetirePlanet(p);
            foreach (var e in _envelopes) RetireEnvelope(e);

            _resultRoot.gameObject.SetActive(false);
            _bigText.gameObject.SetActive(false);
            _exit.Hide();
            bool radio = _period > 0f;
            _radioRect.gameObject.SetActive(false);
            _clockRect.gameObject.SetActive(false);
            _radioRect.gameObject.SetActive(radio);
            _clockRect.gameObject.SetActive(radio);
            ShowClock(false);
            // La píldora de estado ("En la ruta") aparece al despegar: vacía se veía como un punto blanco sobre "¡A volar!".
            _pill.Rect.gameObject.SetActive(false);

            StopAllCoroutines();
            StartCoroutine(GameLoop());
        }

        private IEnumerator GameLoop()
        {
            _safe.gameObject.SetActive(false);
            yield return StartCoroutine(_countdown.Play("Correo Estelar", Assessment.Subtitle("Tus encargos"), () => _safe.gameObject.SetActive(true)));
            _safe.gameObject.SetActive(true);
            yield return null;
            ApplySafeArea(_safe);
            Canvas.ForceUpdateCanvases();
            Layout();
            FillPath();
            LayoutPath(CenterAtShip(out _));
            _hud.SetLevel(_startLevel);
            _hud.SetInfo("Hoja de ruta");

            // Hoja de ruta: los encargos, hasta que se toque "¡A volar!". Mientras, se hornean los planetas.
            BuildBrief();
            _phase = Phase.Brief;
            StartCoroutine(Prewarm());
            while (_phase == Phase.Brief) yield return null;

            _flightStart = GameClock.Time;
            _nextPlanetAt = _flightStart + 2.5f;
            _nextAsteroidAt = _flightStart + 4f;
            _nextEnvelopeD = _traveled + _playH * 0.5f;
            _hud.SetInfo("Entregas 0");
            _pill.Rect.gameObject.SetActive(true);
            _pill.Set("En la ruta", GoodColor);
            _shield.Start(_flightStart);
            RefreshShield();
            _shieldRoot.gameObject.SetActive(true);
            GameFeel.LevelUp();

            while (_phase == Phase.Flight && GameClock.Time - _flightStart < MailContract.FlightSeconds) yield return null;
            yield return StartCoroutine(FinishGame());
        }

        private IEnumerator Prewarm()
        {
            for (int c = 0; c < MailContract.PlanetColors; c++)
            {
                TrafficSprites.Port(c);
                yield return null;
            }
            MailSprites.Package();
            MailSprites.Envelope();
        }

        // ------------------------------------------------------------------ bucle por cuadro

        private void Update()
        {
            float dt = GameClock.DeltaTime;
            if (dt <= 0f) return; // en pausa
            if (_phase == Phase.Brief)
            {
                if (TappedAt(out var local) && Hit(_goButton, local, 1.2f)) StartFlight();
                return;
            }
            if (_phase != Phase.Flight) return;
            float now = GameClock.Time;
            HandleInput();
            UpdateFlight(dt);
            UpdateEnvelopes();
            UpdateAsteroids(now);
            UpdateShield(now);
            UpdatePlanets(now);
            UpdateStage();
            UpdateRadio(now);
            UpdateEffects(dt, now);
            if (_clockShownUntil > 0f && now >= _clockShownUntil) ShowClock(false);
        }

        private void StartFlight()
        {
            _phase = Phase.Flight;
            StartCoroutine(HideBrief());
        }

        /// <summary>¿Hubo un toque nuevo este cuadro? (en coordenadas de la zona de juego).</summary>
        private bool TappedAt(out Vector2 local)
        {
            local = Vector2.zero;
            if (Input.touchCount > 0)
            {
                for (int i = 0; i < Input.touchCount; i++)
                {
                    var t = Input.GetTouch(i);
                    if (t.phase == TouchPhase.Began && RectTransformUtility.ScreenPointToLocalPointInRectangle(_play, t.position, null, out local)) return true;
                }
                return false;
            }
            return Input.GetMouseButtonDown(0) && RectTransformUtility.ScreenPointToLocalPointInRectangle(_play, Input.mousePosition, null, out local);
        }

        private void HandleInput()
        {
            // El dedo que empieza en la franja de abajo guía la nave; un toque arriba es la radio, el reloj o un planeta.
            if (Input.touchCount > 0)
            {
                bool steering = false;
                for (int i = 0; i < Input.touchCount; i++)
                {
                    var touch = Input.GetTouch(i);
                    if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(_play, touch.position, null, out var local)) continue;
                    if (touch.phase == TouchPhase.Began)
                    {
                        if (local.y <= _controlTop && !Hit(_radioRect, local, 0.6f)) _steerFinger = touch.fingerId;
                        else Tap(local);
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

            if (RectTransformUtility.ScreenPointToLocalPointInRectangle(_play, Input.mousePosition, null, out var m))
            {
                if (Input.GetMouseButtonDown(0))
                {
                    if (m.y <= _controlTop && !Hit(_radioRect, m, 0.6f)) _mouseSteer = true;
                    else Tap(m);
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

        /// <summary>¿El punto (en la zona de juego) cae dentro del círculo del objeto? (<paramref name="k"/> × su medio ancho).</summary>
        private bool Hit(RectTransform r, Vector2 playLocal, float k)
        {
            if (r == null || !r.gameObject.activeInHierarchy) return false;
            Vector2 c = LocalIn(_play, r);
            return Vector2.Distance(c, playLocal) <= r.rect.width * 0.5f * k;
        }

        private void Tap(Vector2 local)
        {
            if (Hit(_radioRect, local, 0.6f)) { Radio(); return; }
            if (Hit(_clockRect, local, 0.75f)) { PeekClock(); return; }
            foreach (var p in _planets)
            {
                if (!p.Live || p.Delivered || p.Wrong) continue;
                if (Vector2.Distance(p.Rect.anchoredPosition, local) <= PlanetSize * 0.62f)
                {
                    TapPlanet(p);
                    return;
                }
            }
        }

        private void SteerTo(float localX)
        {
            _shipTargetX = Mathf.Clamp01(localX / _playW + 0.5f);
            if (!_labelFading && GameClock.Time - _flightStart > 4f)
            {
                _labelFading = true;
                StartCoroutine(FadeControlLabel());
            }
        }

        // ------------------------------------------------------------------ vuelo (el de Piloto Estelar)

        private void UpdateFlight(float dt)
        {
            int fl = _driveDda.PresentedLevel;
            bool emergency = _shield.InEmergency(GameClock.Time);
            float targetSpeed = PilotContract.ScrollSpeed(fl, Precision) * MailContract.SpeedRamp(Progress) * _shield.SpeedFactor(GameClock.Time);
            // Al quedar sin escudo frena de golpe; al salir de la reparación acelera de a poco.
            _speed = Mathf.MoveTowards(_speed, targetSpeed, (emergency ? 900f : 120f) * dt);
            _traveled += _speed * dt;
            FillPath();

            float center = CenterAtShip(out float half);
            float before = _shipX;
            float follow = 1f - Mathf.Exp(-dt * 14f);
            float step = (_shipTargetX - _shipX) * follow;
            float maxStep = 2.6f * dt;
            _shipX += Mathf.Clamp(step, -maxStep, maxStep);
            _shipVx = (_shipX - before) / dt;

            bool inLane = PilotContract.InLane(_shipX, center, half);
            _flightTime += dt;
            if (inLane) _inLaneTime += dt;
            // Dificultad del pilotaje: cada 1,5 s, "bien" = 85% del tiempo en la ruta y sin chocar un asteroide.
            _windowT += dt;
            if (inLane) _windowIn += dt;
            if (emergency)
            {
                // Mientras se repara, la dificultad del pilotaje no cuenta (la nave va lenta y sin escudo).
                _windowT = _windowIn = 0f;
                _windowHit = false;
            }
            else if (_windowT >= PilotContract.DriveWindowSeconds)
            {
                bool pass = _windowIn / _windowT >= PilotContract.DriveWindowPass && !_windowHit;
                _driveDda.Register(pass);
                _windowT = _windowIn = 0f;
                _windowHit = false;
                UpdateHudLevel();
            }
            if (_inLane && !inLane)
            {
                PlayTone(196f, 0.16f, 0.12f);
                _pill.Set("¡Vuelve a la ruta!", BadColor);
            }
            else if (!_inLane && inLane && !emergency) _pill.Set("En la ruta", GoodColor);
            _inLane = inLane;

            _shipRect.anchoredPosition = new Vector2((_shipX - 0.5f) * _playW, _shipY);
            float wobble = Motion.Decorative && GameClock.Time < _hitUntil ? 14f * Mathf.Sin(GameClock.Time * 40f) : 0f; // sin ReduceMotion: sin tambaleo ni inclinación (el tinte rojo del golpe sí)
            _shipRect.localRotation = Quaternion.Euler(0f, 0f, (Motion.Decorative ? Mathf.Clamp(-_shipVx * 22f, -16f, 16f) : 0f) + wobble);
            _shipImage.color = inLane && GameClock.Time >= _hitUntil ? Color.white : new Color(1f, 0.78f, 0.74f, 1f);
            LayoutPath(center);
        }

        private void FillPath()
        {
            float ahead = _playH + 200f;
            float lastD = _segments.Count > 0 ? _segments[_segments.Count - 1].D : _traveled - _shipY - _playH;
            int fl = _driveDda.PresentedLevel;
            // En cada tramo del vuelo la ruta se vuelve más sinuosa y algo más angosta.
            float targetAmp = Mathf.Min(0.36f, PilotContract.Curviness(fl) * MailContract.CurveRamp(Progress));
            float targetHalf = PilotContract.LaneHalfWidth(fl) * MailContract.LaneRamp(Progress);
            while (lastD < _traveled + ahead)
            {
                lastD += Spacing;
                _amp = Mathf.MoveTowards(_amp, targetAmp, 0.004f);
                _half = Mathf.MoveTowards(_half, targetHalf, 0.0025f);
                _segments.Add(new Segment { D = lastD, Half = _half, Center = PilotContract.CenterAt(lastD / PathScale, _amp, _half) });
            }
            float behind = _traveled - (_playH * 0.5f + _shipY) - 200f;
            while (_segments.Count > 0 && _segments[0].D < behind) _segments.RemoveAt(0);
        }

        private float CenterAtShip(out float half) => CenterAt(_traveled, out half);

        /// <summary>Centro y medio ancho de la ruta a la distancia <paramref name="d"/> (interpolados entre tramos).</summary>
        private float CenterAt(float d, out float half)
        {
            for (int i = 1; i < _segments.Count; i++)
            {
                if (_segments[i].D < d) continue;
                var a = _segments[i - 1];
                var b = _segments[i];
                float k = Mathf.InverseLerp(a.D, b.D, d);
                half = Mathf.Lerp(a.Half, b.Half, k);
                return Mathf.Lerp(a.Center, b.Center, k);
            }
            half = _half;
            return 0.5f;
        }

        private float YOf(float d) => _shipY + (d - _traveled);

        private void LayoutPath(float centerAtShip)
        {
            int used = 0;
            float top = _playH * 0.5f + 80f;
            float bottom = -_playH * 0.5f - 80f;
            foreach (var seg in _segments)
            {
                float y = YOf(seg.D);
                if (y < bottom || y > top || used >= _beaconsL.Count) continue;
                float depth = Mathf.InverseLerp(bottom, top, y);
                float scale = Mathf.Lerp(1.25f, 0.6f, depth);
                float alpha = Mathf.Lerp(1f, 0.45f, depth);
                bool nearShip = Mathf.Abs(y - _shipY) < 160f;
                Color beacon = nearShip && !_inLane && _phase == Phase.Flight ? BadColor : LaneColor;
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
                band.color = NeuroStyle.WithAlpha(LaneColor, 0.055f * alpha);
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

        // ------------------------------------------------------------------ sobres (la tarea en curso)

        private void UpdateEnvelopes()
        {
            float top = _playH * 0.5f + 120f;
            // Un sobre cada ~1,4 s de vuelo, sobre la ruta (a veces hacia un borde: hay que ir a buscarlo).
            while (_nextEnvelopeD < _traveled + (top - _shipY))
            {
                var free = _envelopes.Find(e => !e.Live);
                if (free == null) break;
                float c = CenterAt(_nextEnvelopeD, out float half);
                float off = ((float)_rng.NextDouble() * 2f - 1f) * half * 0.6f;
                free.D = _nextEnvelopeD;
                free.X = (c + off - 0.5f) * _playW;
                free.Live = true;
                free.Rect.gameObject.SetActive(true);
                free.Rect.localScale = Vector3.one;
                free.Img.color = Color.white;
                _nextEnvelopeD += _speed * 1.4f;
            }
            foreach (var e in _envelopes)
            {
                if (!e.Live) continue;
                float y = YOf(e.D);
                e.Rect.anchoredPosition = new Vector2(e.X, y);
                e.Rect.localRotation = Quaternion.Euler(0f, 0f, Motion.Decorative ? 8f * Mathf.Sin(GameClock.Time * 3f + e.D * 0.01f) : 0f);
                Vector2 ship = _shipRect.anchoredPosition;
                if (Mathf.Abs(y - ship.y) < 70f && Mathf.Abs(e.X - ship.x) < 80f && !_shield.InEmergency(GameClock.Time))
                {
                    _envelopesGot++;
                    _envelopesTotal++;
                    _envelopeStreak++;
                    _points += 10;
                    Sfx(MailSounds.Pickup(), 0.3f);
                    StartCoroutine(UiFx.SparkBurst(_fxRect, new Vector2(e.X, y), NeuroStyle.Cream, 6, 70f, 14f));
                    RetireEnvelope(e);
                }
                else if (y < ship.y - 120f)
                {
                    _envelopesTotal++;
                    _envelopeStreak = 0;
                    RetireEnvelope(e);
                }
            }
        }

        private static void RetireEnvelope(EnvelopeView e)
        {
            e.Live = false;
            if (e.Rect != null) e.Rect.gameObject.SetActive(false);
        }

        // ------------------------------------------------------------------ planetas (encargo por lugar)

        private void UpdatePlanets(float now)
        {
            if (now >= _nextPlanetAt)
            {
                var free = _planets.Find(p => !p.Live);
                if (free != null)
                {
                    var data = MailContract.NextPlanet(_dda.PresentedLevel, _targets, _lastWasTarget, _rng);
                    _lastWasTarget = data.IsTarget;
                    SpawnPlanet(free, data);
                }
                _nextPlanetAt = now + MailContract.PlanetGap(_dda.PresentedLevel, Progress) * (0.85f + 0.3f * (float)_rng.NextDouble());
            }
            float bottom = -_playH * 0.5f - PlanetSize;
            foreach (var p in _planets)
            {
                if (!p.Live) continue;
                float y = YOf(p.D);
                p.Rect.anchoredPosition = new Vector2(p.X, y);
                p.Glow.color = NeuroStyle.WithAlpha(TrafficSprites.Colors[p.Data.Color], Motion.Decorative ? 0.25f + 0.08f * Mathf.Sin(now * 4f) : 0.25f);
                if (y < bottom)
                {
                    if (p.Data.IsTarget && !p.Delivered)
                    {
                        // Se fue sin su paquete: aviso suave (sin culpa) y cuenta para la dificultad.
                        _eventTotal++;
                        _deliveryStreak = 0;
                        _dda.Register(false);
                        Sfx(MailSounds.Missed(), 0.45f);
                        StartCoroutine(FloatText(new Vector2(p.X * 0.6f, -_playH * 0.5f + 330f), "Se fue sin su paquete", NeuroStyle.Grape));
                        UpdateHudLevel();
                    }
                    RetirePlanet(p);
                }
            }
        }

        private void SpawnPlanet(PlanetView p, MailPlanet data)
        {
            p.Data = data;
            p.Live = true;
            p.Delivered = p.Wrong = false;
            p.D = _traveled + (_playH * 0.5f - _shipY) + PlanetSize;
            // Al costado de la ruta; si no cabe de ese lado, del otro.
            float c = CenterAt(p.D, out float half);
            float edge = 0.5f - (PlanetSize * 0.5f + 20f) / _playW;
            float x = c - 0.5f + data.Side * (half + 0.17f);
            if (Mathf.Abs(x) > edge) x = c - 0.5f - data.Side * (half + 0.17f);
            p.X = Mathf.Clamp(x, -edge, edge) * _playW;
            p.Body.sprite = TrafficSprites.Port(data.Color);
            p.Body.color = Color.white;
            p.Mark.gameObject.SetActive(false);
            p.Rect.localScale = Vector3.one;
            p.Rect.gameObject.SetActive(true);
            StartCoroutine(PopIn(p.Rect, 0.3f));
        }

        private void TapPlanet(PlanetView p)
        {
            if (p.Data.IsTarget)
            {
                p.Delivered = true;
                _eventHits++;
                _eventTotal++;
                _deliveryStreak++;
                int pts = MailContract.DeliveryPoints(_dda.PresentedLevel, _deliveryStreak);
                _points += pts;
                _dda.Register(true);
                GameFeel.Correct(_deliveryStreak);
                Sfx(MailSounds.Deliver(), 0.55f);
                StartCoroutine(FlyPackage(p));
                _hud.SetInfo($"Entregas {_eventHits + _radioHits}");
                UpdateHudLevel();
            }
            else
            {
                p.Wrong = true;
                _commissions++;
                if (p.Data.IsLure) _lureCommissions++;
                _deliveryStreak = 0;
                _dda.Register(false);
                Sfx(MailSounds.WrongPlanet(), 0.5f);
                p.Mark.sprite = AnswerMarkSprite.Cross();
                p.Mark.gameObject.SetActive(true);
                StartCoroutine(PopIn(p.Mark.rectTransform, 0.2f));
                p.Body.color = new Color(1f, 1f, 1f, 0.55f);
                StartCoroutine(UiFx.Shake(6f, 0.25f, p.Rect));
                StartCoroutine(FloatText(p.Rect.anchoredPosition, "No es de tu encargo", BadColor));
                UpdateHudLevel();
            }
        }

        /// <summary>El paquete sale de la nave en arco hasta el planeta; al llegar, ✓ y destellos.</summary>
        private IEnumerator FlyPackage(PlanetView p)
        {
            var img = NewImage(_fxRect, "Package", MailSprites.Package());
            img.gameObject.SetActive(Motion.Decorative); // sin ReduceMotion: el paquete no vuela (aparece la marca en el planeta tras el mismo tiempo)
            var r = img.rectTransform;
            r.sizeDelta = new Vector2(90f, 90f);
            Vector2 from = _shipRect.anchoredPosition + new Vector2(0f, ShipSize * 0.3f);
            float t = 0f;
            const float seconds = 0.45f;
            while (t < seconds && p.Live && Motion.Decorative)
            {
                t += GameClock.DeltaTime;
                float k = UiFx.EaseOutCubic(Mathf.Clamp01(t / seconds));
                Vector2 to = p.Rect.anchoredPosition;
                r.anchoredPosition = Vector2.Lerp(from, to, k) + new Vector2(0f, 120f * Mathf.Sin(Mathf.PI * k));
                r.localRotation = Quaternion.Euler(0f, 0f, 360f * k);
                r.localScale = Vector3.one * Mathf.Lerp(1f, 0.6f, k);
                yield return null;
            }
            if (!Motion.Decorative) yield return Motion.Hold(seconds);
            Destroy(img.gameObject);
            if (!p.Live) yield break;
            p.Mark.sprite = AnswerMarkSprite.Check();
            p.Mark.gameObject.SetActive(true);
            StartCoroutine(PopIn(p.Mark.rectTransform, 0.2f));
            StartCoroutine(PopRect(p.Rect, 1.2f, 0.25f));
            StartCoroutine(UiFx.SparkBurst(_fxRect, p.Rect.anchoredPosition, NeuroStyle.Sun, 12, 140f, 24f));
            StartCoroutine(UiFx.RingBurst(_fxRect, p.Rect.anchoredPosition, TrafficSprites.Colors[p.Data.Color], 120f, 360f, 0.5f));
            StartCoroutine(FloatText(p.Rect.anchoredPosition, "¡Entregado!", GoodColor));
        }

        private static void RetirePlanet(PlanetView p)
        {
            p.Live = false;
            if (p.Rect != null) p.Rect.gameObject.SetActive(false);
        }

        // ------------------------------------------------------------------ radio y reloj (encargo por hora)

        private float FlightClock => GameClock.Time - _flightStart;

        private void Radio()
        {
            if (_period <= 0f) return;
            var j = MailContract.JudgeRadio(FlightClock, _radioTargets, _answered, out int index);
            StartCoroutine(PopRect(_radioRect, 1.15f, 0.2f));
            if (j == RadioJudgement.OnTime)
            {
                _answered.Add(index);
                _radioHits++;
                _points += MailContract.DeliveryPoints(_dda.PresentedLevel, 1);
                _dda.Register(true);
                Sfx(MailSounds.RadioOk(), 0.55f);
                GameFeel.Haptic(GameFeel.HapticKind.Light);
                StartCoroutine(UiFx.RingBurst(_fxRect, _radioRect.anchoredPosition, NeuroStyle.Grape, 120f, 380f, 0.5f));
                StartCoroutine(FloatText(_radioRect.anchoredPosition + new Vector2(-120f, 40f), "¡Aviso recibido!", GoodColor));
                _hud.SetInfo($"Entregas {_eventHits + _radioHits}");
                UpdateHudLevel();
            }
            else
            {
                _radioOfftime++;
                Sfx(MailSounds.RadioOff(), 0.45f);
                StartCoroutine(FloatText(_radioRect.anchoredPosition + new Vector2(-120f, 40f),
                    j == RadioJudgement.Early ? "Aún no es la hora" : "Ya pasó la hora", AmberColor));
            }
        }

        /// <summary>Horas de radio que pasaron sin aviso: se cuentan (y se dice, suave) cuando se cierra su ventana.</summary>
        private void UpdateRadio(float now)
        {
            float t = FlightClock;
            while (_radioChecked < _radioTargets.Count && t > _radioTargets[_radioChecked] + MailContract.RadioWindow)
            {
                if (!_answered.Contains(_radioChecked))
                {
                    _dda.Register(false);
                    Sfx(MailSounds.Missed(), 0.4f);
                    StartCoroutine(FloatText(_radioRect.anchoredPosition + new Vector2(-150f, 40f), "Se pasó la hora del aviso", NeuroStyle.Grape));
                    StartCoroutine(BlinkRadio());
                    UpdateHudLevel();
                }
                _radioChecked++;
            }
            if (_radioGlow != null) _radioGlow.color = NeuroStyle.WithAlpha(NeuroStyle.Grape, Motion.Decorative ? 0.18f + 0.06f * Mathf.Sin(now * 3f) : 0.18f);
        }

        private IEnumerator BlinkRadio()
        {
            if (!Motion.Decorative)
            {
                // Tinte fijo del aviso perdido durante el mismo tiempo (0,72 s), sin parpadeo.
                _radioGlow.color = NeuroStyle.WithAlpha(BadColor, 0.6f);
                yield return StartCoroutine(Wait(0.72f));
                _radioGlow.color = NeuroStyle.WithAlpha(NeuroStyle.Grape, 0.2f);
                yield break;
            }
            for (int i = 0; i < 2; i++)
            {
                _radioGlow.color = NeuroStyle.WithAlpha(BadColor, 0.6f);
                yield return StartCoroutine(Wait(0.18f));
                _radioGlow.color = NeuroStyle.WithAlpha(NeuroStyle.Grape, 0.2f);
                yield return StartCoroutine(Wait(0.18f));
            }
        }

        /// <summary>Se destapa el reloj un momento (1,6 s) y se anota cuándo se miró.</summary>
        private void PeekClock()
        {
            if (_period <= 0f || GameClock.Time - _lastPeek < 0.3f) return;
            _lastPeek = GameClock.Time;
            _clockChecks.Add(FlightClock);
            Sfx(MailSounds.Peek(), 0.45f);
            ShowClock(true);
            _clockShownUntil = GameClock.Time + 1.6f;
            StartCoroutine(PopRect(_clockRect, 1.15f, 0.2f));
        }

        private void ShowClock(bool open)
        {
            if (_clockImage == null) return;
            _clockImage.sprite = open ? MailSprites.ClockFace() : MailSprites.ClockCover();
            int s = Mathf.Max(0, Mathf.FloorToInt(FlightClock));
            _clockText.text = open ? $"{s / 60}:{s % 60:00}" : "?";
            _clockText.color = open ? AmberColor : NeuroStyle.WithAlpha(NeuroStyle.Cream, 0.7f);
            _clockText.rectTransform.anchoredPosition = open ? new Vector2(0f, -ClockSize * 0.72f) : Vector2.zero;
            if (!open) _clockShownUntil = 0f;
        }

        // ------------------------------------------------------------------ efectos

        private void UpdateEffects(float dt, float now)
        {
            float speedK = Mathf.InverseLerp(PilotContract.ScrollSpeed(1, false), PilotContract.ScrollSpeed(9, false), _speed);
            if (_stars != null) _stars.Warp = Mathf.MoveTowards(_stars.Warp, 0.1f + 0.25f * speedK, dt * 1.5f);
            float target = !_inLane ? 0.55f : 0f;
            float a = Mathf.MoveTowards(_vignetteL.color.a, target, dt * 3f);
            _vignetteL.color = _vignetteR.color = NeuroStyle.WithAlpha(BadColor, a);
            _shipGlow.color = _shield.InEmergency(now)
                ? NeuroStyle.WithAlpha(AmberColor, Motion.Decorative ? 0.35f + 0.2f * Mathf.Sin(now * 12f) : 0.45f)
                : NeuroStyle.WithAlpha(LaneColor, Motion.Decorative ? 0.30f + 0.08f * Mathf.Sin(now * 5f) : 0.30f);
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
                _trail[i].color = NeuroStyle.WithAlpha(NeuroStyle.Coral, 0.55f * (1f - k));
            }
        }

        private void UpdateHudLevel() => _hud.SetLevel(Mathf.RoundToInt((_dda.PresentedLevel + _driveDda.PresentedLevel) * 0.5f));

        private float Progress => _phase == Phase.Flight || _phase == Phase.Done
            ? Mathf.Clamp01((GameClock.Time - _flightStart) / MailContract.FlightSeconds) : 0f;

        /// <summary>Cambio de tramo: aviso, destello y un empujón de hiperespacio (se siente la aceleración).</summary>
        private void UpdateStage()
        {
            int stage = MailContract.Stage(Progress);
            if (stage <= _stage) return;
            _stage = stage;
            _toast.Show($"Tramo {stage + 1} de 3", stage == 1 ? "¡La ruta se acelera!" : "¡Último tramo: a toda velocidad!", AmberColor, 1.6f);
            GameFeel.LevelUp();
            GameFeel.Haptic(GameFeel.HapticKind.Firm);
            if (_stars != null) _stars.Warp = 0.9f;
            StartCoroutine(UiFx.RingBurst(_fxRect, new Vector2(0f, _shipY), AmberColor, 160f, 1100f, 0.6f));
        }

        // ------------------------------------------------------------------ asteroides (hay que esquivarlos)

        private void UpdateAsteroids(float now)
        {
            if (now >= _nextAsteroidAt)
            {
                var free = _asteroids.Find(a => !a.Live);
                if (free != null) SpawnAsteroid(free);
                _nextAsteroidAt = now + MailContract.AsteroidGap(_driveDda.PresentedLevel, Progress) * (0.8f + 0.4f * (float)_rng.NextDouble());
            }
            Vector2 ship = _shipRect.anchoredPosition;
            float dt = GameClock.DeltaTime;
            foreach (var a in _asteroids)
            {
                if (!a.Live) continue;
                float y = YOf(a.D);
                a.Rect.anchoredPosition = new Vector2(a.X, y);
                a.Rect.localRotation = Quaternion.Euler(0f, 0f, a.Rect.localEulerAngles.z + (Motion.Decorative ? a.Spin * dt : 0f));
                bool touching = Mathf.Abs(y - ship.y) < AsteroidSize * 0.5f && Mathf.Abs(a.X - ship.x) < AsteroidSize * 0.55f;
                if (!a.Hit && touching && _shield.InEmergency(now))
                {
                    // En la reparación de emergencia los asteroides la atraviesan: no cuentan ni como choque ni como esquivado.
                    a.Hit = true;
                    a.Img.color = new Color(1f, 1f, 1f, 0.45f);
                }
                else if (!a.Hit && touching)
                {
                    a.Hit = true;
                    BreakShield(now);
                    _asteroidHits++;
                    _windowHit = true;
                    _envelopeStreak = 0;
                    _hitUntil = now + 0.45f;
                    Sfx(MailSounds.Bump(), 0.55f);
                    GameFeel.Haptic(GameFeel.HapticKind.Double);
                    a.Img.color = new Color(1f, 1f, 1f, 0.45f);
                    StartCoroutine(UiFx.SparkBurst(_fxRect, new Vector2(a.X, y), NeuroStyle.Coral, 10, 120f, 22f));
                    StartCoroutine(FloatText(new Vector2(ship.x, ship.y + 40f), "¡Asteroide!", BadColor));
                    _vignetteL.color = _vignetteR.color = NeuroStyle.WithAlpha(BadColor, 0.7f);
                }
                if (y < ship.y - ShipSize)
                {
                    if (!a.Hit) _asteroidsPassed++;
                    RetireAsteroid(a);
                }
            }
        }

        /// <summary>Un asteroide sobre la ruta, cargado a un lado (siempre queda por dónde pasar).</summary>
        private void SpawnAsteroid(AsteroidView a)
        {
            a.D = _traveled + (_playH * 0.5f - _shipY) + AsteroidSize;
            float c = CenterAt(a.D, out float half);
            float side = _rng.Next(2) == 0 ? -1f : 1f;
            float off = side * half * (0.35f + 0.4f * (float)_rng.NextDouble());
            a.X = (c + off - 0.5f) * _playW;
            a.Spin = (_rng.Next(2) == 0 ? -1f : 1f) * (30f + 50f * (float)_rng.NextDouble());
            a.Live = true;
            a.Hit = false;
            a.Img.sprite = SymbolSprite.Get(ShapeKind.Asteroid, _rng.Next(3));
            a.Img.color = Color.white;
            a.Rect.gameObject.SetActive(true);
        }

        private static void RetireAsteroid(AsteroidView a)
        {
            a.Live = false;
            if (a.Rect != null) a.Rect.gameObject.SetActive(false);
        }

        // ------------------------------------------------------------------ escudo de la nave

        /// <summary>Un choque: se rompe un segmento (y si era el último, reparación de emergencia).</summary>
        private void BreakShield(float now)
        {
            bool empty = _shield.Hit(now);
            RefreshShield();
            var pip = _pips[Mathf.Clamp(_shield.Segments, 0, ShipShield.Max - 1)];
            Sfx(MailSounds.ShieldCrack(), 0.4f);
            StartCoroutine(PopRect(pip.rectTransform, 1.35f, 0.25f));
            StartCoroutine(UiFx.SparkBurst(_shieldRoot, pip.rectTransform.anchoredPosition, NeuroStyle.Sky, 8, 70f, 14f));
            if (!empty) return;
            _toast.Show("¡Reparación de emergencia!", "Sin escudo: la nave va lenta unos segundos", AmberColor, 1.8f);
            _pill.Set("Reparando la nave…", AmberColor);
            Sfx(MailSounds.Emergency(), 0.5f);
            GameFeel.Haptic(GameFeel.HapticKind.Firm);
            StartCoroutine(UiFx.RingBurst(_fxRect, _shipRect.anchoredPosition, AmberColor, 140f, 520f, 0.5f));
        }

        private void UpdateShield(float now)
        {
            bool fromEmergency = _wasEmergency;
            if (_shield.Advance(now)) OnShieldRepaired(fromEmergency);
            bool emergency = _shield.InEmergency(now);
            _wasEmergency = emergency;
            if (emergency)
            {
                // Parpadea mientras se repara.
                float k = Motion.Decorative ? 0.55f + 0.45f * Mathf.Abs(Mathf.Sin(now * 5.5f)) : 0.7f; // ≤ 2 Hz en todos los modos (antes ≈ 2,9 Hz); sin ReduceMotion queda fija (semitransparente)
                _shipImage.color = NeuroStyle.WithAlpha(_shipImage.color, k);
                _damageImage.color = new Color(1f, 1f, 1f, k);
            }
            else _damageImage.color = Color.white;

            // Humo: con un segmento o sin escudo, la nave va echando humo (más cuanto peor está).
            if (_shield.Segments <= 1 && now >= _smokeEmitAt)
            {
                _smokeEmitAt = now + (_shield.Segments == 0 ? 0.08f : 0.16f);
                var img = _smoke[_smokeNext];
                _smokeAge[_smokeNext] = 0f;
                img.gameObject.SetActive(true);
                float jitter = ((float)_rng.NextDouble() * 2f - 1f) * ShipSize * 0.15f;
                img.rectTransform.anchoredPosition = _shipRect.anchoredPosition + new Vector2(jitter, -ShipSize * 0.1f);
                _smokeNext = (_smokeNext + 1) % _smoke.Count;
            }
            float dt = GameClock.DeltaTime;
            for (int i = 0; i < _smoke.Count; i++)
            {
                if (!_smoke[i].gameObject.activeSelf) continue;
                _smokeAge[i] += dt;
                float k = _smokeAge[i] / 0.9f;
                if (k >= 1f)
                {
                    _smoke[i].gameObject.SetActive(false);
                    continue;
                }
                var r = _smoke[i].rectTransform;
                r.anchoredPosition += new Vector2(0f, -_speed * 0.55f * dt);
                float size = Mathf.Lerp(40f, 120f, k);
                r.sizeDelta = new Vector2(size, size);
                _smoke[i].color = new Color(0.62f, 0.6f, 0.74f, 0.45f * (1f - k));
            }
        }

        private void OnShieldRepaired(bool afterEmergency)
        {
            RefreshShield();
            var pip = _pips[Mathf.Clamp(_shield.Segments - 1, 0, ShipShield.Max - 1)];
            Sfx(MailSounds.Repair(), 0.4f);
            StartCoroutine(PopRect(pip.rectTransform, 1.3f, 0.3f));
            StartCoroutine(UiFx.SparkBurst(_shieldRoot, pip.rectTransform.anchoredPosition, GoodColor, 8, 80f, 14f));
            if (afterEmergency)
            {
                StartCoroutine(FloatText(_shipRect.anchoredPosition + new Vector2(0f, 40f), "¡Nave reparada!", GoodColor));
                if (_inLane) _pill.Set("En la ruta", GoodColor);
            }
            else StartCoroutine(FloatText(_shieldRoot.anchoredPosition + new Vector2(0f, -150f), "Escudo reparado", GoodColor));
        }

        /// <summary>Segmentos llenos o rotos y el daño visible de la nave (grietas según lo que falta).</summary>
        private void RefreshShield()
        {
            for (int i = 0; i < _pips.Length; i++) _pips[i].sprite = MailSprites.ShieldPip(i < _shield.Segments);
            int lost = ShipShield.Max - _shield.Segments;
            _damageImage.gameObject.SetActive(lost > 0);
            if (lost > 0) _damageImage.sprite = MailSprites.ShipDamage(Mathf.Min(lost, 2));
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

        private IEnumerator FloatText(Vector2 pos, string text, Color color)
        {
            var t = MakeText(_fxRect, "Float", 50, TextAnchor.MiddleCenter, color, 0f, 0f);
            NeuroStyle.ClayText(t, 4f, 6f);
            var r = t.rectTransform;
            r.anchorMin = r.anchorMax = new Vector2(0.5f, 0.5f);
            r.sizeDelta = new Vector2(620f, 80f);
            t.text = text;
            float half = 310f;
            pos.x = Mathf.Clamp(pos.x, -_playW * 0.5f + half, _playW * 0.5f - half);
            float e = 0f;
            const float seconds = 1f;
            while (e < seconds)
            {
                e += GameClock.DeltaTime;
                float k = Mathf.Clamp01(e / seconds);
                r.anchoredPosition = pos + new Vector2(0f, 70f + 80f * (Motion.Decorative ? UiFx.EaseOutCubic(k) : 0f));
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

        private void Sfx(AudioClip clip, float volume)
        {
            if (clip != null && GameFeel.SoundOn && (_config == null || _config.config == null || _config.config.sound_enabled))
                _audioSource.PlayOneShot(clip, volume);
        }

        // ------------------------------------------------------------------ hoja de ruta

        private string ColorWord(int color) =>
            $"<color=#{ColorUtility.ToHtmlStringRGB(TrafficSprites.Colors[color])}>{MailContract.ColorNames[color]}</color>";

        /// <summary>La hoja de ruta: los encargos con su dibujo (sin recuadros), un truco y el botón "¡A volar!".</summary>
        private void BuildBrief()
        {
            foreach (Transform child in _briefRoot) Destroy(child.gameObject);
            _briefRoot.gameObject.SetActive(true);
            _briefRoot.GetComponent<CanvasGroup>().alpha = 1f;
            float w = _play.rect.width;
            float y = _playH * 0.5f - 90f;

            var title = BriefText("Tu hoja de ruta", 76, Color.white, new Vector2(0f, y), w);
            NeuroStyle.ClayText(title, 4f, 6f);
            y -= 80f;
            BriefText("Recuerda tus encargos: durante el vuelo no los verás", 36, NeuroStyle.WithAlpha(NeuroStyle.Cream, 0.85f), new Vector2(0f, y), w - 80f);
            y -= 170f;

            // Por lugar: los planetas del encargo.
            float iconX = -w * 0.5f + 150f;
            for (int i = 0; i < _targets.Length; i++)
            {
                var planet = NewImage(_briefRoot, "Planet", TrafficSprites.Port(_targets[i]));
                planet.rectTransform.sizeDelta = new Vector2(150f, 150f);
                planet.rectTransform.anchoredPosition = new Vector2(iconX + (_targets.Length > 1 ? (i == 0 ? -40f : 40f) : 0f), y + (i == 0 ? 0f : -30f));
                planet.gameObject.SetActive(true);
            }
            string colors = _targets.Length == 1 ? ColorWord(_targets[0]) : $"{ColorWord(_targets[0])} o {ColorWord(_targets[1])}";
            BriefRow(y, $"Cuando pases un planeta {colors}, tócalo", "le entregas su paquete", w);
            y -= 220f;

            if (_period > 0f)
            {
                var radio = NewImage(_briefRoot, "Radio", MailSprites.Radio());
                radio.rectTransform.sizeDelta = new Vector2(150f, 150f);
                radio.rectTransform.anchoredPosition = new Vector2(iconX, y);
                radio.gameObject.SetActive(true);
                BriefRow(y, $"Cada {Mathf.RoundToInt(_period)} segundos, toca la radio", "avisas a la base que vas bien", w);
                y -= 200f;
                var clock = NewImage(_briefRoot, "Clock", MailSprites.ClockCover());
                clock.rectTransform.sizeDelta = new Vector2(120f, 120f);
                clock.rectTransform.anchoredPosition = new Vector2(iconX, y);
                clock.gameObject.SetActive(true);
                BriefRow(y, "El reloj va tapado", "tócalo cuando quieras mirar la hora", w);
                y -= 190f;
            }

            BriefText("Truco: dilo por dentro, «cuando vea el planeta, lo toco»", 34, NeuroStyle.WithAlpha(NeuroStyle.Sun, 0.95f), new Vector2(0f, y), w - 80f);

            // Botón "¡A volar!" (arcilla lima).
            var go = new GameObject("Go");
            go.transform.SetParent(_briefRoot, false);
            _goButton = go.AddComponent<RectTransform>();
            _goButton.anchorMin = _goButton.anchorMax = new Vector2(0.5f, 0.5f);
            _goButton.sizeDelta = new Vector2(520f, 150f);
            _goButton.anchoredPosition = new Vector2(0f, _controlTop - 20f);
            var bg = go.AddComponent<Image>();
            bg.sprite = RoundedRectSprite.Get(64);
            bg.type = Image.Type.Sliced;
            bg.color = GoodColor;
            bg.raycastTarget = false;
            NeuroStyle.ClayFrame(bg, 5f, 10f);
            var label = MakeText(_goButton, "Label", 66, TextAnchor.MiddleCenter, NeuroStyle.Ink, 0f, 0f);
            Stretch(label.rectTransform);
            label.text = "¡A volar!";
            StartCoroutine(PulseGo());
        }

        private IEnumerator PulseGo()
        {
            while (_phase == Phase.Brief || _phase == Phase.Idle)
            {
                if (_goButton != null) _goButton.localScale = Vector3.one * (Motion.Decorative ? 1f + 0.04f * Mathf.Sin(GameClock.Time * 4f) : 1f);
                yield return null;
            }
        }

        private void BriefRow(float y, string main, string sub, float w)
        {
            float left = -w * 0.5f + 270f;
            float width = w * 0.5f - left - 40f;
            var t = MakeText(_briefRoot, "Main", 46, TextAnchor.MiddleLeft, Color.white, 0f, 0f);
            t.supportRichText = true;
            t.rectTransform.anchorMin = t.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            t.rectTransform.pivot = new Vector2(0f, 0.5f);
            t.rectTransform.sizeDelta = new Vector2(width, 110f);
            t.rectTransform.anchoredPosition = new Vector2(left, y + 22f);
            t.horizontalOverflow = HorizontalWrapMode.Wrap;
            t.text = main;
            BestFit(t, 32);
            var s = MakeText(_briefRoot, "Sub", 32, TextAnchor.MiddleLeft, NeuroStyle.WithAlpha(NeuroStyle.Cream, 0.75f), 0f, 0f);
            s.rectTransform.anchorMin = s.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            s.rectTransform.pivot = new Vector2(0f, 0.5f);
            s.rectTransform.sizeDelta = new Vector2(width, 50f);
            s.rectTransform.anchoredPosition = new Vector2(left, y - 50f);
            s.text = sub;
            BestFit(s, 24);
        }

        private Text BriefText(string text, int size, Color color, Vector2 pos, float width)
        {
            var t = MakeText(_briefRoot, "Text", size, TextAnchor.MiddleCenter, color, 0f, 0f);
            t.rectTransform.anchorMin = t.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            t.rectTransform.sizeDelta = new Vector2(width, size * 1.6f);
            t.rectTransform.anchoredPosition = pos;
            t.text = text;
            BestFit(t, Mathf.RoundToInt(size * 0.7f));
            return t;
        }

        private IEnumerator HideBrief()
        {
            var g = _briefRoot.GetComponent<CanvasGroup>();
            float t = 0f;
            while (t < 0.35f)
            {
                t += GameClock.DeltaTime;
                g.alpha = 1f - Mathf.Clamp01(t / 0.35f);
                yield return null;
            }
            _briefRoot.gameObject.SetActive(false);
        }

        // ------------------------------------------------------------------ fin

        private IEnumerator FinishGame()
        {
            _phase = Phase.Done;
            // Los planetas que todavía se veían no cuentan (aún se podían tocar).
            foreach (var p in _planets) RetirePlanet(p);
            foreach (var e in _envelopes) RetireEnvelope(e);
            foreach (var a in _asteroids) RetireAsteroid(a);
            // Horas de radio: las que cerraron su ventana, más las que ya tenían aviso aunque su ventana siga abierta.
            UpdateRadio(GameClock.Time);
            int radioTotal = _radioChecked;
            foreach (int i in _answered) if (i >= _radioChecked) radioTotal++;

            float lane = _flightTime > 0f ? _inLaneTime / _flightTime : 0f;
            var clock = MailContract.Monitoring(_clockChecks, _radioTargets.GetRange(0, Mathf.Min(radioTotal, _radioTargets.Count)), _period);
            int score = MailContract.Score(_eventHits, _eventTotal, _commissions, _radioHits, radioTotal, lane, _dda.PeakLevel);
            int asteroids = _asteroidHits + _asteroidsPassed;
            float flown = Mathf.Min(MailContract.FlightSeconds, GameClock.Time - _flightStart);
            _shield.Advance(_flightStart + flown);
            int intactPct = Mathf.RoundToInt(_shield.IntactShare(flown) * 100f);
            foreach (var sm in _smoke) sm.gameObject.SetActive(false);
            _shieldRoot.gameObject.SetActive(false);

            _pill.Set("Ruta completa", GoodColor);
            _bigText.text = "¡RUTA COMPLETA!";
            _bigText.gameObject.SetActive(true);
            StartCoroutine(PopIn(_bigText.rectTransform, 0.35f));
            GameFeel.Finish();
            yield return StartCoroutine(Wait(1.4f));
            _bigText.gameObject.SetActive(false);

            _exit.Show();
            _resultRoot.Find("Title").GetComponent<Text>().text = score >= 85 ? "¡Correo impecable!" : score >= 65 ? "¡Buen reparto!" : "Ruta completa";
            _resultRoot.Find("Detail").GetComponent<Text>().text = $"Planetas: {_eventHits} de {_eventTotal}" + (radioTotal > 0 ? $" · Radio: {_radioHits} de {radioTotal}" : "");
            _resultRoot.Find("Extra").GetComponent<Text>().text = $"Esquivaste {_asteroidsPassed} de {asteroids} asteroides · nave intacta {intactPct}%";
            _resultRoot.gameObject.SetActive(true);
            StartCoroutine(AnimateResult(score));

            var telemetry = new StroopTelemetry
            {
                user_id = _config.user_id,
                game_id = _config.game_id,
                session_metrics = new StroopSessionMetrics
                {
                    correct_trials = _eventHits + _radioHits,
                    total_trials = _eventTotal + radioTotal,
                    calculated_score = score,
                    average_response_time_ms = 0,
                    level = _config.config.level,
                    timed = _config.config.timed,
                    end_rating = _dda.RatingNormalized,
                    mode_trials = _dda.ScoredTrials,
                    mode_hits = _dda.ScoredCorrect,
                    peak_level = Mathf.Max(_dda.PeakLevel, _driveDda.PeakLevel),
                    mail_event_hits = _eventHits,
                    mail_event_total = _eventTotal,
                    mail_commissions = _commissions,
                    mail_lure_commissions = _lureCommissions,
                    mail_radio_hits = _radioHits,
                    mail_radio_total = radioTotal,
                    mail_radio_offtime = _radioOfftime,
                    mail_radio_period_s = Mathf.RoundToInt(_period),
                    mail_clock_checks = clock.Checks,
                    mail_clock_late = clock.LateChecks,
                    mail_lane_pct = Mathf.RoundToInt(lane * 100f),
                    mail_envelopes = _envelopesGot,
                    mail_envelopes_total = _envelopesTotal,
                    mail_asteroid_hits = _asteroidHits,
                    mail_asteroids = asteroids,
                    mail_hull_intact_pct = intactPct,
                    mail_emergencies = _shield.Emergencies
                }
            };
            NativeBridge.ForwardTelemetryToPlatform(JsonUtility.ToJson(telemetry));
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

            var canvasGo = new GameObject("MailCanvas");
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
            WorldBackdrop.Build(bgRect, GameWorld.Hyperspace);
            _stars = bgRect.GetComponentInChildren<StarfieldFx>();

            var safeGo = new GameObject("SafeAreaContent");
            safeGo.transform.SetParent(canvasGo.transform, false);
            _safe = safeGo.AddComponent<RectTransform>();
            ApplySafeArea(_safe);

            _hud = new GameHud(_safe, "Correo Estelar", MarginU, this, withStreak: false, rightReserve: ClockSize + 40f);
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

        private void BuildPlayField()
        {
            var go = new GameObject("PlayField");
            go.transform.SetParent(_safe, false);
            _play = go.AddComponent<RectTransform>();
            _play.anchorMin = Vector2.zero;
            _play.anchorMax = Vector2.one;
            _play.pivot = new Vector2(0.5f, 0.5f);

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
            for (int i = 0; i < 64; i++) _bands.Add(NewImage(_laneRoot, "Band", null));
            for (int i = 0; i < 64; i++)
            {
                _beaconsL.Add(NewImage(_laneRoot, "BeaconL", DiscSprite.Get()));
                _beaconsR.Add(NewImage(_laneRoot, "BeaconR", DiscSprite.Get()));
            }

            // Sobres sobre la ruta y planetas a los costados.
            var envRoot = Layer(_play, "Envelopes");
            for (int i = 0; i < EnvelopePool; i++)
            {
                var img = NewImage(envRoot, "Envelope", MailSprites.Envelope());
                img.rectTransform.sizeDelta = new Vector2(EnvelopeSize, EnvelopeSize);
                _envelopes.Add(new EnvelopeView { Rect = img.rectTransform, Img = img });
            }
            var asteroidRoot = Layer(_play, "Asteroids");
            for (int i = 0; i < AsteroidPool; i++)
            {
                var img = NewImage(asteroidRoot, "Asteroid", null);
                img.rectTransform.sizeDelta = new Vector2(AsteroidSize, AsteroidSize);
                _asteroids.Add(new AsteroidView { Rect = img.rectTransform, Img = img });
            }
            var planetRoot = Layer(_play, "Planets");
            for (int i = 0; i < PlanetPool; i++) _planets.Add(BuildPlanet(planetRoot));

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
                _trail.Add(NewImage(_trailRoot, "Trail", RadialGlowSprite.Get()));
                _trailAge.Add(1f);
            }

            var smokeRoot = Layer(_play, "Smoke");
            for (int i = 0; i < SmokePool; i++)
            {
                _smoke.Add(NewImage(smokeRoot, "Smoke", RadialGlowSprite.Get()));
                _smokeAge.Add(1f);
            }

            var shipGo = new GameObject("Ship");
            shipGo.transform.SetParent(_play, false);
            _shipRect = shipGo.AddComponent<RectTransform>();
            _shipRect.anchorMin = _shipRect.anchorMax = new Vector2(0.5f, 0.5f);
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
            _shipImage.sprite = ChipShipSprite.Get(ChipDirection.Up);
            _shipImage.raycastTarget = false;
            // Grietas encima de la nave (mismo encuadre que su sprite).
            _damageImage = NewImage(shipGo.transform, "Damage", null);
            Stretch(_damageImage.rectTransform);

            // Escudo: 3 segmentos arriba a la izquierda, con su rótulo.
            var shieldGo = new GameObject("Shield");
            shieldGo.transform.SetParent(_play, false);
            _shieldRoot = shieldGo.AddComponent<RectTransform>();
            _shieldRoot.anchorMin = _shieldRoot.anchorMax = new Vector2(0.5f, 0.5f);
            _shieldRoot.sizeDelta = new Vector2(PipSize * 3.4f, PipSize * 1.6f);
            for (int i = 0; i < _pips.Length; i++)
            {
                _pips[i] = NewImage(_shieldRoot, "Pip", MailSprites.ShieldPip(true));
                _pips[i].rectTransform.sizeDelta = new Vector2(PipSize, PipSize);
                _pips[i].rectTransform.anchoredPosition = new Vector2((i - 1) * (PipSize + 8f), 12f);
                _pips[i].gameObject.SetActive(true);
            }
            var shieldLabel = MakeText(_shieldRoot, "Label", 30, TextAnchor.MiddleCenter, NeuroStyle.WithAlpha(NeuroStyle.Cream, 0.75f), 0f, 0f);
            shieldLabel.rectTransform.anchorMin = shieldLabel.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            shieldLabel.rectTransform.sizeDelta = new Vector2(PipSize * 3.4f, 40f);
            shieldLabel.rectTransform.anchoredPosition = new Vector2(0f, -PipSize * 0.5f - 12f);
            shieldLabel.text = "escudo";
            _shieldRoot.gameObject.SetActive(false);

            // Radio (abajo a la derecha, sobre la franja de control) y reloj tapado (arriba a la derecha).
            var radioGo = new GameObject("Radio");
            radioGo.transform.SetParent(_play, false);
            _radioRect = radioGo.AddComponent<RectTransform>();
            _radioRect.anchorMin = _radioRect.anchorMax = new Vector2(0.5f, 0.5f);
            _radioRect.sizeDelta = new Vector2(RadioSize, RadioSize);
            _radioGlow = NewImage(_radioRect, "Glow", RadialGlowSprite.Get());
            _radioGlow.rectTransform.sizeDelta = Vector2.one * RadioSize * 1.9f;
            _radioGlow.gameObject.SetActive(true);
            var radioImg = NewImage(_radioRect, "Body", MailSprites.Radio());
            Stretch(radioImg.rectTransform);
            radioImg.gameObject.SetActive(true);
            var radioLabel = MakeText(_radioRect, "Label", 32, TextAnchor.MiddleCenter, NeuroStyle.WithAlpha(NeuroStyle.Cream, 0.8f), 0f, 0f);
            radioLabel.rectTransform.anchorMin = radioLabel.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            radioLabel.rectTransform.sizeDelta = new Vector2(200f, 40f);
            radioLabel.rectTransform.anchoredPosition = new Vector2(0f, -RadioSize * 0.6f);
            radioLabel.text = "radio";

            var clockGo = new GameObject("Clock");
            clockGo.transform.SetParent(_safe, false);
            _clockRect = clockGo.AddComponent<RectTransform>();
            _clockRect.anchorMin = _clockRect.anchorMax = new Vector2(1f, 1f);
            _clockRect.sizeDelta = new Vector2(ClockSize, ClockSize);
            _clockRect.anchoredPosition = new Vector2(-MarginU - ClockSize * 0.5f, -GameHud.Height * 0.5f - 10f);
            _clockImage = NewImage(_clockRect, "Face", MailSprites.ClockCover());
            Stretch(_clockImage.rectTransform);
            _clockImage.gameObject.SetActive(true);
            _clockText = MakeText(_clockRect, "Time", 44, TextAnchor.MiddleCenter, NeuroStyle.Cream, 0f, 0f);
            NeuroStyle.ClayText(_clockText, 3f, 4f);
            _clockText.rectTransform.anchorMin = _clockText.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            _clockText.rectTransform.sizeDelta = new Vector2(220f, 60f);

            _fxRect = Layer(_play, "Fx");

            // Hoja de ruta (encima de todo mientras se lee).
            _briefRoot = Layer(_play, "Brief");
            var dim = _briefRoot.gameObject.AddComponent<Image>();
            dim.color = new Color(0.03f, 0.05f, 0.16f, 0.9f);
            dim.raycastTarget = false;
            _briefRoot.gameObject.AddComponent<CanvasGroup>();
            _briefRoot.gameObject.SetActive(false);

            _bigText = MakeText(_play, "BigText", 110, TextAnchor.MiddleCenter, Color.white, 0f, 0f);
            NeuroStyle.ClayText(_bigText, 6f, 10f);
            var br = _bigText.rectTransform;
            br.anchorMin = br.anchorMax = new Vector2(0.5f, 0.5f);
            br.sizeDelta = new Vector2(1000f, 180f);
            BestFit(_bigText, 60);
            _bigText.gameObject.SetActive(false);
        }

        private PlanetView BuildPlanet(Transform parent)
        {
            var go = new GameObject("Planet");
            go.transform.SetParent(parent, false);
            var r = go.AddComponent<RectTransform>();
            r.anchorMin = r.anchorMax = new Vector2(0.5f, 0.5f);
            r.sizeDelta = new Vector2(PlanetSize, PlanetSize);
            var v = new PlanetView { Rect = r };
            v.Glow = NewImage(r, "Glow", RadialGlowSprite.Get());
            v.Glow.rectTransform.sizeDelta = Vector2.one * PlanetSize * 2f;
            v.Glow.gameObject.SetActive(true);
            v.Body = NewImage(r, "Body", null);
            Stretch(v.Body.rectTransform);
            v.Body.gameObject.SetActive(true);
            v.Mark = NewImage(r, "Mark", null);
            v.Mark.rectTransform.sizeDelta = new Vector2(70f, 70f);
            v.Mark.rectTransform.anchoredPosition = new Vector2(PlanetSize * 0.4f, PlanetSize * 0.4f);
            go.SetActive(false);
            return v;
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
            AddResultText("Detail", 44, new Vector2(0f, -150f), new Color(1f, 1f, 1f, 0.85f));
            AddResultText("Extra", 40, new Vector2(0f, -250f), new Color(1f, 1f, 1f, 0.65f));
            go.SetActive(false);
        }

        // ------------------------------------------------------------------ layout

        private void Layout()
        {
            Rect safe = _safe.rect;
            float sh = Mathf.Max(safe.height, 800f);
            float y = GameHud.Height + 20f;
            _play.offsetMin = Vector2.zero;
            _play.offsetMax = new Vector2(0f, -y);
            Canvas.ForceUpdateCanvases();
            _playW = _play.rect.width;
            _playH = _play.rect.height;

            float controlH = Mathf.Clamp(_playH * 0.22f, 240f, 360f);
            _controlRect.offsetMin = new Vector2(MarginU * 0.5f, 18f);
            _controlRect.offsetMax = new Vector2(-MarginU * 0.5f, 18f + controlH);
            _controlTop = -_playH * 0.5f + 18f + controlH;
            _shipY = _controlTop + ShipSize * 0.75f;
            _radioRect.anchoredPosition = new Vector2(_playW * 0.5f - MarginU - RadioSize * 0.5f, _controlTop + RadioSize * 0.62f);
            _shieldRoot.anchoredPosition = new Vector2(-_playW * 0.5f + MarginU * 0.5f + PipSize * 1.7f, _playH * 0.5f - PipSize * 0.8f);

            float vh = _playH * 0.9f;
            _vignetteL.rectTransform.sizeDelta = _vignetteR.rectTransform.sizeDelta = new Vector2(520f, vh);
            _vignetteL.rectTransform.anchoredPosition = new Vector2(-_playW * 0.5f, 0f);
            _vignetteR.rectTransform.anchoredPosition = new Vector2(_playW * 0.5f, 0f);

            _pill.SetPosition(new Vector2(0f, -sh / 2f + 18f + controlH - 56f));
            _pill.Rect.SetAsLastSibling();
            _clockRect.SetAsLastSibling();
        }
    }
}
