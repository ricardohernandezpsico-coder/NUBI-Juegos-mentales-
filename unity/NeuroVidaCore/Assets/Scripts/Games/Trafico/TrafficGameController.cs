using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using NeuroVida.Bridge;
using NeuroVida.Contracts;
using NeuroVida.Games.Secuencia; // RoundedRectSprite / RadialGlowSprite / RingSprite
using NeuroVida.Games.Shared;
using static NeuroVida.Games.Shared.UiKit;
using Motion = NeuroVida.Games.Shared.Motion; // UnityEngine.Motion también existe

namespace NeuroVida.Games.Trafico
{
    /// <summary>
    /// "Tráfico Estelar": juego estrella de atención dividida y planificación (ver <see cref="TrafficContract"/>). Eres
    /// control de tráfico: de la estación de carga salen cápsulas de colores que viajan por rutas curvas; tocando los
    /// desvíos haces que cada una llegue al planeta-puerto de su color (y su símbolo). Cada vez salen más seguido, más
    /// rápido, hay más puertos y las rutas se enroscan más (cornisas con cambios de sentido).
    /// <list type="bullet">
    /// <item>Lento y lleno: las cápsulas van tranquilas pero salen seguidas, así hay varias en viaje a la vez (2-3 al
    /// principio, 7-8 en nivel alto) y hay que coordinar desvíos que dos cápsulas necesitan en sentidos distintos.
    /// Flujo continuo (la pantalla no se vacía); 10 entregas seguidas sin error = "serie perfecta".</item>
    /// <item>"Próximas": las 3 que vienen esperan a la vista en la estación (se puede preparar la ruta antes de que
    /// salgan). Desde el nivel 5, a veces sale una cápsula URGENTE (aro sol, más rápida, vale el doble).</item>
    /// <item>Si cambia la cantidad de puertos, se terminan de entregar las que van en camino y la red se rearma.</item>
    /// <item>Medida propia: "tu anticipación" (cuánto antes preparas los desvíos) y cuánto planificas vs reaccionas a
    /// último momento (control proactivo / reactivo, Braver 2012), y cuántas cápsulas manejaste a la vez.</item>
    /// </list>
    /// Reto = 2 minutos; Precisión = 30 cápsulas, más lentas y espaciadas.
    /// </summary>
    public class TrafficGameController : GameControllerBase
    {
        public const string GameId = TrafficContract.GameId;

        private const float UnitsPerDp = 3f;
        private const float MarginU = 40f;
        private const int PodPool = 16;
        private const int EdgePool = 2 * TrafficContract.MaxPorts;
        private const float PortSize = 150f, PodSize = 86f, KnobSize = 128f, TapReach = 100f, StationSize = 220f;
        private const int FlowPool = 110, TrailDots = 3, TrailHistory = 12;
        /// <summary>Luces que corren por las rutas activas: separación y velocidad (unidades del canvas).</summary>
        private const float FlowSpacing = 80f, FlowSpeed = 150f;
        /// <summary>Fila de "próximas" a la derecha de la estación: dónde empieza, separación y tamaño de cada una.</summary>
        private const float QueueX0 = 175f, QueueSpacing = 82f;
        private static readonly float[] QueueSizes = { 80f, 70f, 60f };
        private static readonly Color RailIdle = NeuroStyle.Hex(0x4B4F9A);

        private static readonly Color GoodColor = NeuroStyle.Lime;
        private static readonly Color BadColor = NeuroStyle.Coral;
        private static readonly Color AmberColor = NeuroStyle.Sun;

        private enum Phase { Idle, Playing, Rebuild, Done }

        private sealed class EdgeView
        {
            public RailLine Halo, Shadow, Rail, Core;
            public int From, To;
            public bool Active;
            public readonly List<Vector2> Points = new List<Vector2>();
            public float[] Cum = new float[0];
            public float Length;

            public RailLine[] All => new[] { Halo, Shadow, Rail, Core };

            /// <summary>Punto de la ruta a una distancia (unidades del canvas) desde su inicio.</summary>
            public Vector2 At(float d)
            {
                int n = Points.Count;
                if (n == 0) return Vector2.zero;
                d = Mathf.Clamp(d, 0f, Length);
                int i = 1;
                while (i < n - 1 && Cum[i] < d) i++;
                float seg = Mathf.Max(1e-4f, Cum[i] - Cum[i - 1]);
                return Vector2.Lerp(Points[i - 1], Points[i], Mathf.Clamp01((d - Cum[i - 1]) / seg));
            }
        }

        private sealed class PodView
        {
            public RectTransform Rect;
            public Image Body, Glow, Ring;
            public Image[] Trail;
            public readonly Vector2[] History = new Vector2[TrailHistory];
            public int Head;
            public int PodId = -1;
        }

        private System.Random _rng;
        private AdaptiveDifficulty _dda;
        private Phase _phase = Phase.Idle;
        private bool Endless => _config != null && _config.config.timed;
        private bool Precision => !Endless;
        private int _previousFrameRate;

        private TrafficNetwork _net;
        private TrafficSim _sim;
        private readonly List<TrafficEvent> _events = new List<TrafficEvent>();

        // sesión
        private readonly List<float> _leads = new List<float>();
        private int _spawned, _delivered, _correct, _streak, _bestStreak, _points;
        private float _nextSpawnAt, _endsAt, _builtAt, _meanRoute;
        private bool _urgentExplained;

        // "próximas": las que esperan en la estación (color, urgente, desde cuándo se ven)
        private struct Upcoming { public int Color; public bool Urgent; public float AnnouncedSim, AnnouncedPlay; }
        private readonly List<Upcoming> _queue = new List<Upcoming>();
        private int _lastQueuedColor = -1;
        private bool _lastQueuedUrgent;

        // "tu carga": muestras de cuántas van en viaje y tramos ensuciados por errores (en tiempo de juego)
        private float _playTime, _nextSample;
        private readonly List<float> _loadTimes = new List<float>();
        private readonly List<int> _loadCounts = new List<int>();
        private readonly List<float> _errFrom = new List<float>();
        private readonly List<float> _errTo = new List<float>();
        private readonly Dictionary<int, float> _podSeenAt = new Dictionary<int, float>();
        private bool _spawningClosed;
        private int _lastTickSecond = -1;

        // UI
        private RectTransform _safe, _play, _field, _fxRect, _station, _timerBg, _timerFill;
        private Image _beacon, _doorGlow;
        private AudioSource _engine;
        private float _lastChime;
        private readonly List<RectTransform> _flow = new List<RectTransform>();
        private readonly List<Image> _queuePods = new List<Image>();
        private readonly List<Image> _queueRings = new List<Image>();
        private readonly List<Vector2> _queueBase = new List<Vector2>();
        private Text _queueLabel;
        private float _queueShift;
        private Text _prompt;
        private readonly List<EdgeView> _edges = new List<EdgeView>();
        private int _edgeCount;
        private readonly Dictionary<int, RectTransform> _knobs = new Dictionary<int, RectTransform>();
        private readonly Dictionary<int, float> _knobAngle = new Dictionary<int, float>();
        private readonly Dictionary<int, RectTransform> _ports = new Dictionary<int, RectTransform>();
        private readonly List<RectTransform> _knobPool = new List<RectTransform>();
        private readonly List<RectTransform> _portPool = new List<RectTransform>();
        private readonly List<PodView> _pods = new List<PodView>();
        private Toast _toast;
        private ExitButton _exit;
        private GameHud _hud;
        private CountdownScreen _countdown;
        private float _fieldW, _fieldH;

        // ------------------------------------------------------------------ sesión

        public void StartSession(SequenceInitConfig config)
        {
            _config = config;
            _rng = new System.Random();
            var age = DdaUserProfileConfig.ParseAgeBand(config.config.age_band);
            float start = AdaptiveDifficulty.StartRating(config.config, TrafficContract.MaxLevel);
            // Cada cápsula es un ensayo (~60 por partida). Sin tiempo de reacción: cuenta llegar bien.
            _dda = new AdaptiveDifficulty(TrafficContract.MaxLevel, age, start, stepUp: 0.2f, useReaction: false);

            _previousFrameRate = Application.targetFrameRate;
            Application.targetFrameRate = 60;

            _phase = Phase.Idle;
            _leads.Clear();
            _spawned = _delivered = _correct = _streak = _bestStreak = _points = 0;
            _queue.Clear();
            _lastQueuedColor = -1;
            _lastQueuedUrgent = false;
            _urgentExplained = false;
            _playTime = _nextSample = 0f;
            _loadTimes.Clear();
            _loadCounts.Clear();
            _errFrom.Clear();
            _errTo.Clear();
            _podSeenAt.Clear();
            _endsAt = 0f;
            _spawningClosed = false;
            _lastTickSecond = -1;
            _net = null;
            _sim = null;

            _resultRoot.gameObject.SetActive(false);
            _exit.Hide();
            _timerBg.gameObject.SetActive(Endless);
            _hud.SetStreak(0);
            HideNetwork();

            StopAllCoroutines();
            StartCoroutine(GameLoop());
        }

        private void OnDisable()
        {
            if (_engine != null) _engine.Stop();
            if (_previousFrameRate != 0) Application.targetFrameRate = _previousFrameRate;
        }

        private IEnumerator GameLoop()
        {
            _safe.gameObject.SetActive(false);
            yield return StartCoroutine(_countdown.Play("Tráfico Estelar", Assessment.Subtitle("Prepárate"), () => _safe.gameObject.SetActive(true)));
            _safe.gameObject.SetActive(true);
            yield return null;
            ApplySafeArea(_safe);
            Canvas.ForceUpdateCanvases();
            Layout();
            UpdateHud();

            yield return StartCoroutine(BuildNetwork(TrafficContract.Ports(_dda.PresentedLevel)));
            for (int i = 0; i < TrafficContract.QueueSize; i++) EnqueueUpcoming(_dda.PresentedLevel);
            RefreshQueue();
            // Instrucciones como aviso arriba (un texto en el medio taparía la ruta de la estación).
            _toast.Show("Lleva cada cápsula a su planeta", "Toca los desvíos · a la derecha, las próximas", NeuroStyle.Sky, 2.8f);
            yield return StartCoroutine(Wait(3f));

            _endsAt = GameClock.Time + TrafficContract.RetoSeconds;
            _nextSpawnAt = GameClock.Time;
            _phase = Phase.Playing;
            while (true)
            {
                if (_phase == Phase.Playing && NeedsRebuild()) yield return StartCoroutine(Rebuild());
                if (_spawningClosed && _sim.InFlight == 0) break;
                yield return null;
            }
            yield return StartCoroutine(FinishGame());
        }

        private bool Finished() => Precision ? _spawned >= TrafficContract.PrecisionPods : GameClock.Time >= _endsAt;

        /// <summary>¿Hay que rearmar la red? Solo si cambió la cantidad de puertos y la actual lleva un rato en juego.</summary>
        private bool NeedsRebuild() =>
            !Finished() && TrafficContract.Ports(_dda.PresentedLevel) != CountPorts() && GameClock.Time - _builtAt >= 20f;

        /// <summary>Deja de sacar cápsulas, espera que lleguen las que van en camino y arma la red nueva.</summary>
        private IEnumerator Rebuild()
        {
            _phase = Phase.Rebuild;
            bool more = TrafficContract.Ports(_dda.PresentedLevel) > CountPorts();
            _toast.Show(more ? "¡Se suma un planeta!" : "Una ruta menos", "Termina de entregar las que van en camino", GoodColor, 1.8f);
            float giveUp = GameClock.Time + 20f;
            while (_sim.InFlight > 0 && GameClock.Time < giveUp) yield return null;
            if (Finished())
            {
                _phase = Phase.Playing;
                yield break;
            }
            yield return StartCoroutine(Wait(0.3f));
            yield return StartCoroutine(BuildNetwork(TrafficContract.Ports(_dda.PresentedLevel)));
            // La simulación nueva empieza su reloj en 0; las que esperan en "próximas" se ven desde ahora, y si alguna
            // tenía un color que ya no tiene puerto, cambia.
            int ports = CountPorts();
            int prev = -1;
            for (int i = 0; i < _queue.Count; i++)
            {
                var q = _queue[i];
                q.AnnouncedSim = 0f;
                if (q.Color >= ports || q.Color == prev) q.Color = TrafficContract.NextColor(ports, prev, _rng);
                prev = q.Color;
                _queue[i] = q;
            }
            _lastQueuedColor = prev;
            _podSeenAt.Clear();
            RefreshQueue();
            _nextSpawnAt = GameClock.Time + 0.6f;
            _phase = Phase.Playing;
        }

        private void EnqueueUpcoming(int level)
        {
            int color = TrafficContract.NextColor(CountPorts(), _lastQueuedColor, _rng);
            bool urgent = TrafficContract.NextIsUrgent(level, _lastQueuedUrgent, _rng);
            _lastQueuedColor = color;
            _lastQueuedUrgent = urgent;
            _queue.Add(new Upcoming { Color = color, Urgent = urgent, AnnouncedSim = _sim != null ? _sim.Time : 0f, AnnouncedPlay = _playTime });
        }

        /// <summary>Sale la primera de "próximas"; entra otra al final de la fila.</summary>
        private void SpawnNext(int level)
        {
            if (_queue.Count == 0) EnqueueUpcoming(level);
            var next = _queue[0];
            _queue.RemoveAt(0);
            var pod = _sim.Spawn(next.Color, next.Urgent, next.AnnouncedSim);
            _podSeenAt[pod.Id] = next.AnnouncedPlay;
            _spawned++;
            EnqueueUpcoming(level);
            float travel = _meanRoute / Mathf.Max(0.01f, _sim.Speed);
            _nextSpawnAt = GameClock.Time + TrafficContract.SpawnInterval(level, Precision, travel) * (0.85f + 0.3f * (float)_rng.NextDouble());
            Sfx(next.Urgent ? TrafficSounds.UrgentLaunch() : TrafficSounds.Launch(), next.Urgent ? 0.4f : 0.3f);
            StartCoroutine(StationLaunch());
            _queueShift = QueueSpacing;
            RefreshQueue();
            if (next.Urgent && !_urgentExplained)
            {
                _urgentExplained = true;
                _toast.Show("¡Cápsula urgente!", "Va más rápido y vale el doble", NeuroStyle.Sun, 1.6f);
            }
        }

        private int CountPorts()
        {
            int n = 0;
            for (int i = 0; i < _net.Count; i++) if (_net.IsPort(i)) n++;
            return n;
        }

        // ------------------------------------------------------------------ cada cuadro

        private void Update()
        {
            UpdateClock();
            UpdateEngine();
            if (_sim != null && _phase != Phase.Done && GameClock.DeltaTime > 0f)
            {
                // Vida de la red también durante las instrucciones: luces que corren y la baliza de la antena.
                UpdateFlow();
                _beacon.color = NeuroStyle.WithAlpha(NeuroStyle.Coral, Motion.Decorative ? 0.35f + 0.3f * Mathf.Sin(GameClock.Time * 4f) : 0.5f); // sin ReduceMotion: baliza fija
            }
            if (_phase != Phase.Playing && _phase != Phase.Rebuild) return;
            float dt = GameClock.DeltaTime;
            if (dt <= 0f || _sim == null) return;

            HandleTap();
            _playTime += dt;

            // Salida de cápsulas: flujo continuo (salvo mientras se rearma la red).
            int level = _dda.PresentedLevel;
            _sim.Speed = TrafficContract.Speed(level, Precision);
            if (Finished() && !_spawningClosed)
            {
                _spawningClosed = true;
                RefreshQueue();
            }
            if (_phase == Phase.Playing && !_spawningClosed && GameClock.Time >= _nextSpawnAt) SpawnNext(level);

            // "Tu carga": cuántas van en viaje, dos veces por segundo.
            if (_playTime >= _nextSample)
            {
                _loadTimes.Add(_playTime);
                _loadCounts.Add(_sim.InFlight);
                _nextSample = _playTime + 0.5f;
            }

            _events.Clear();
            _sim.Step(dt, _events);
            foreach (var e in _events) OnEvent(e);

            DrawPods();
            UpdateKnobs(dt);
            UpdateQueue(dt);
        }

        /// <summary>La compuerta se enciende y la estación da un saltito cuando sale una cápsula.</summary>
        private IEnumerator StationLaunch()
        {
            var r = _doorGlow.rectTransform;
            float e = 0f;
            const float seconds = 0.45f;
            while (e < seconds)
            {
                e += GameClock.DeltaTime;
                float k = Mathf.Clamp01(e / seconds);
                r.localScale = Vector3.one * (Motion.Decorative ? 0.8f + 0.6f * k : 1f); // sin ReduceMotion: el resplandor solo se apaga (sin crecer)
                _doorGlow.color = NeuroStyle.WithAlpha(NeuroStyle.Sun, 0.85f * (1f - k));
                _station.localScale = Vector3.one * (Motion.Decorative ? 1f + 0.05f * Mathf.Sin(k * Mathf.PI) : 1f);
                yield return null;
            }
            _doorGlow.color = NeuroStyle.WithAlpha(NeuroStyle.Sun, 0f);
            _station.localScale = Vector3.one;
        }

        // ------------------------------------------------------------------ sonido

        private bool SoundAllowed => GameFeel.SoundOn && (_config == null || _config.config == null || _config.config.sound_enabled);

        private void Sfx(AudioClip clip, float volume)
        {
            if (SoundAllowed) _audioSource.PlayOneShot(clip, volume);
        }

        /// <summary>
        /// Vuelo del tráfico: un colchón suave que suena mientras hay cápsulas viajando. Con más cápsulas sube un poco el
        /// volumen (el tono no cambia: queda afinado con las campanas y la marimba); se corre a la izquierda o a la
        /// derecha según por dónde van. Sin cápsulas, se apaga despacio.
        /// </summary>
        private void UpdateEngine()
        {
            if (_engine == null || _dda == null) return;
            float dt = GameClock.DeltaTime;
            if (dt <= 0f) return;
            int flying = _sim != null && _phase != Phase.Done ? _sim.InFlight : 0;
            float target = SoundAllowed && flying > 0 ? 0.12f + 0.025f * Mathf.Min(flying - 1, 6) : 0f;
            _engine.volume = Mathf.MoveTowards(_engine.volume, target, dt * 0.4f);
            if (flying > 0)
            {
                float sum = 0f;
                int n = 0;
                foreach (var pod in _sim.Pods)
                {
                    if (pod.Done) continue;
                    _sim.Position(pod, out float x, out _);
                    sum += x;
                    n++;
                }
                float pan = n > 0 ? Mathf.Clamp((sum / n - 0.5f) * 0.8f, -0.4f, 0.4f) : 0f;
                _engine.panStereo = Mathf.MoveTowards(_engine.panStereo, pan, dt * 1.2f);
            }
            if (_engine.volume > 0.001f && !_engine.isPlaying) _engine.Play();
            else if (_engine.volume <= 0.001f && target <= 0f && _engine.isPlaying) _engine.Stop();
        }

        private void HandleTap()
        {
            if (!Input.GetMouseButtonDown(0)) return;
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(_field, Input.mousePosition, null, out var local)) return;
            int best = -1;
            float bestD = TapReach;
            foreach (var kv in _knobs)
            {
                float d = Vector2.Distance(local, kv.Value.anchoredPosition);
                if (d < bestD) { bestD = d; best = kv.Key; }
            }
            if (best < 0) return;
            _sim.Toggle(best);
            GameFeel.Haptic(GameFeel.HapticKind.Light);
            Sfx(TrafficSounds.Switch(_sim.SwitchState[best] == 1), 0.4f);
            StartCoroutine(PopRect(_knobs[best], 1.15f, 0.15f));
            RefreshEdges();
        }

        private void OnEvent(TrafficEvent e)
        {
            if (!e.Arrived)
            {
                if (e.Correct && e.Lead >= 0f) _leads.Add(e.Lead);
                // Campanita con la nota del color de la cápsula al pasar por el desvío: el tráfico arma una melodía (sin
                // amontonar si pasan varias juntas).
                if (GameClock.Time - _lastChime > 0.07f)
                {
                    _lastChime = GameClock.Time;
                    Sfx(TrafficSounds.PassChime(e.Color), 0.2f);
                }
                return;
            }
            _delivered++;
            var port = _ports[e.Node];
            var pod = _sim.Pods.Find(q => q.Id == e.PodId);
            bool urgent = pod != null && pod.Urgent;
            var change = _dda.Register(e.Correct);
            if (e.Correct)
            {
                _correct++;
                _streak++;
                _bestStreak = Mathf.Max(_bestStreak, _streak);
                int pts = TrafficContract.Points(true, _dda.PresentedLevel, _streak, urgent);
                _points += pts;
                if (_streak % TrafficContract.SeriesSize == 0)
                {
                    _points += 200;
                    _toast.Show("¡Serie perfecta!", $"{_streak} seguidas sin error · +200", NeuroStyle.Sun, 1.3f);
                    Sfx(TrafficSounds.Cascade(), 0.5f);
                    GameFeel.Haptic(GameFeel.HapticKind.Firm);
                }
                // El "pling" de la racha (igual en toda la app) y, debajo, la marimba grave del color al posarse.
                GameFeel.Correct(_streak);
                Sfx(TrafficSounds.Landing(e.Color), 0.3f);
                StartCoroutine(PopRect(port, 1.25f, 0.25f));
                StartCoroutine(UiFx.RingBurst(_fxRect, port.anchoredPosition, TrafficSprites.Colors[e.Color], 120f, 320f, 0.4f));
                StartCoroutine(FloatText(port.anchoredPosition + new Vector2(0f, 60f), "+" + pts, NeuroStyle.Sun));
            }
            else
            {
                _streak = 0;
                // El error ensucia "tu carga" desde que esa cápsula se vio hasta ahora.
                _errFrom.Add(_podSeenAt.TryGetValue(e.PodId, out float seen) ? seen : _playTime - 10f);
                _errTo.Add(_playTime);
                GameFeel.Wrong();
                StartCoroutine(UiFx.Shake(14f, 0.3f, port));
                StartCoroutine(MarkAt(port.anchoredPosition, false));
                StartCoroutine(Flash(BadColor, 0.08f, 0.2f));
            }
            _hud.SetStreak(_streak);
            if (change == DdaChange.Up)
            {
                _toast.Show("¡Más tráfico!", "Más rápido y más seguido", GoodColor, 0.9f);
                GameFeel.LevelUp();
            }
            else if (change == DdaChange.Down || _dda.Struggling)
                _toast.Show("Con calma", "Mira el color y el símbolo", AmberColor, 0.9f);
            UpdateHud();
        }

        private void UpdateClock()
        {
            if (!Endless || _phase == Phase.Idle || _phase == Phase.Done || _endsAt <= 0f) return;
            float left = _endsAt - GameClock.Time;
            SetTimerFraction(Mathf.Clamp01(left / TrafficContract.RetoSeconds));
            int whole = Mathf.CeilToInt(left);
            if (whole <= 5 && whole >= 1 && whole != _lastTickSecond)
            {
                _lastTickSecond = whole;
                GameFeel.Tick();
            }
        }

        // ------------------------------------------------------------------ red

        private Vector2 ToUi(float x, float y) => new Vector2((x - 0.5f) * _fieldW, (0.5f - y) * _fieldH);

        private IEnumerator BuildNetwork(int ports)
        {
            // Si había una red, se desvanece; después aparece la nueva (rutas, desvíos y puertos en cascada).
            HideNetwork();
            // Rutas más enroscadas en niveles altos (curvas suaves al principio; cornisas con cambios de sentido después).
            _net = TrafficContract.BuildNetwork(ports, _rng, _fieldH / Mathf.Max(1f, _fieldW), TrafficContract.Twist(_dda.PresentedLevel));
            _sim = new TrafficSim(_net) { Speed = TrafficContract.Speed(_dda.PresentedLevel, Precision) };
            _knobs.Clear();
            _knobAngle.Clear();
            _ports.Clear();

            var door = ToUi(_net.X[0], _net.Y[0]);
            _station.anchoredPosition = door + new Vector2(0f, 0.2f * StationSize);
            _builtAt = GameClock.Time;
            _meanRoute = _net.MeanRouteLength();
            // "Próximas": en fila a la derecha de la compuerta (la primera, la más cercana y grande).
            _queueBase.Clear();
            for (int i = 0; i < _queuePods.Count; i++)
            {
                _queueBase.Add(door + new Vector2(QueueX0 + i * QueueSpacing, 40f));
                _queuePods[i].rectTransform.anchoredPosition = _queueBase[i];
                _queueRings[i].rectTransform.anchoredPosition = _queueBase[i];
            }
            _queueLabel.rectTransform.anchoredPosition = door + new Vector2(QueueX0 + QueueSpacing, 102f);
            int e = 0, k = 0, p = 0;
            for (int n = 0; n < _net.Count; n++)
            {
                foreach (int c in _net.Children[n])
                {
                    var ev = _edges[e++];
                    _edgeCount = e;
                    ev.From = n;
                    ev.To = c;
                    ev.Points.Clear();
                    var xs = _net.PathX[c];
                    var ys = _net.PathY[c];
                    for (int i = 0; i < xs.Length; i++) ev.Points.Add(ToUi(xs[i], ys[i]));
                    ev.Cum = new float[ev.Points.Count];
                    for (int i = 1; i < ev.Points.Count; i++) ev.Cum[i] = ev.Cum[i - 1] + Vector2.Distance(ev.Points[i - 1], ev.Points[i]);
                    ev.Length = ev.Cum[ev.Cum.Length - 1];
                    ev.Halo.SetPoints(ev.Points, Vector2.zero);
                    ev.Shadow.SetPoints(ev.Points, new Vector2(0f, -7f));
                    ev.Rail.SetPoints(ev.Points, Vector2.zero);
                    ev.Core.SetPoints(ev.Points, Vector2.zero);
                }
                if (_net.IsSwitch(n))
                {
                    var knob = _knobPool[k++];
                    knob.anchoredPosition = ToUi(_net.X[n], _net.Y[n]);
                    knob.gameObject.SetActive(true);
                    _knobs[n] = knob;
                    _knobAngle[n] = TargetAngle(n);
                    knob.localRotation = Quaternion.Euler(0f, 0f, _knobAngle[n]);
                }
                else if (_net.IsPort(n))
                {
                    var port = _portPool[p++];
                    port.anchoredPosition = ToUi(_net.X[n], _net.Y[n]);
                    port.GetComponent<Image>().sprite = TrafficSprites.Port(_net.PortColor[n]);
                    port.gameObject.SetActive(true);
                    _ports[n] = port;
                }
            }
            for (int i = 0; i < _edges.Count; i++)
            {
                bool on = i < e;
                foreach (var line in _edges[i].All) line.gameObject.SetActive(on);
            }
            RefreshEdges();
            _station.gameObject.SetActive(true);
            PlayTone(523f, 0.1f, 0.05f);
            StartCoroutine(PopIn(_station, 0.25f));
            foreach (var kv in _knobs) StartCoroutine(PopIn(kv.Value, 0.25f));
            int order = 0;
            foreach (var kv in _ports)
            {
                StartCoroutine(PopInDelayed(kv.Value, 0.08f * order++));
            }
            yield return StartCoroutine(Wait(0.3f + 0.08f * _ports.Count));
        }

        private IEnumerator PopInDelayed(RectTransform r, float delay)
        {
            r.localScale = Motion.Decorative ? Vector3.zero : Vector3.one; // sin ReduceMotion: aparece quieto (la espera y el PopIn conservan su duración)
            yield return StartCoroutine(Wait(delay));
            yield return StartCoroutine(PopIn(r, 0.25f));
        }

        private void HideNetwork()
        {
            _edgeCount = 0;
            foreach (var ev in _edges) { ev.Active = false; foreach (var line in ev.All) line.gameObject.SetActive(false); }
            foreach (var r in _knobPool) r.gameObject.SetActive(false);
            foreach (var r in _portPool) r.gameObject.SetActive(false);
            foreach (var r in _flow) r.gameObject.SetActive(false);
            foreach (var img in _queuePods) img.gameObject.SetActive(false);
            foreach (var img in _queueRings) img.gameObject.SetActive(false);
            if (_queueLabel != null) _queueLabel.gameObject.SetActive(false);
            foreach (var pv in _pods) HidePod(pv);
            if (_station != null) _station.gameObject.SetActive(false);
        }

        /// <summary>
        /// Rutas: las activas (por donde irá una cápsula que llegue ahora) son rieles celestes con resplandor y luces que
        /// corren; las otras quedan como rieles de arcilla apagados, visibles para planificar.
        /// </summary>
        private void RefreshEdges()
        {
            foreach (var ev in _edges)
            {
                if (!ev.Rail.gameObject.activeSelf) continue;
                ev.Active = !_net.IsSwitch(ev.From) || _net.Children[ev.From][_sim.SwitchState[ev.From]] == ev.To;
                ev.Halo.color = NeuroStyle.WithAlpha(NeuroStyle.Sky, ev.Active ? 0.3f : 0f);
                ev.Shadow.color = NeuroStyle.WithAlpha(NeuroStyle.Ink, ev.Active ? 0.9f : 0.55f);
                ev.Rail.color = ev.Active ? NeuroStyle.Sky : RailIdle;
                ev.Core.color = new Color(1f, 1f, 1f, ev.Active ? 0.5f : 0f);
            }
        }

        /// <summary>Luces que corren por las rutas activas, en el sentido del viaje (se mueven solo donde irá la carga).</summary>
        private void UpdateFlow()
        {
            float phase = Mathf.Repeat(GameClock.Time * FlowSpeed, FlowSpacing);
            int used = 0;
            foreach (var ev in _edges)
            {
                if (!ev.Active || !ev.Rail.gameObject.activeSelf) continue;
                for (float d = phase; d < ev.Length && used < _flow.Count; d += FlowSpacing)
                {
                    var dot = _flow[used++];
                    dot.anchoredPosition = ev.At(d);
                    if (!dot.gameObject.activeSelf) dot.gameObject.SetActive(true);
                }
            }
            for (int i = used; i < _flow.Count; i++)
                if (_flow[i].gameObject.activeSelf) _flow[i].gameObject.SetActive(false);
        }

        private EdgeView EdgeTo(int child)
        {
            for (int i = 0; i < _edgeCount; i++) if (_edges[i].To == child) return _edges[i];
            return null;
        }

        private float TargetAngle(int sw)
        {
            int child = _net.Children[sw][_sim.SwitchState[sw]];
            // La flecha apunta hacia donde SALE la ruta activa (su primer tramo), no hacia el nodo lejano.
            var a = ToUi(_net.X[sw], _net.Y[sw]);
            var ev = EdgeTo(child);
            var b = ev != null ? ev.At(Mathf.Min(45f, ev.Length * 0.5f)) : ToUi(_net.X[child], _net.Y[child]);
            // La flecha del sprite apunta hacia arriba.
            return Mathf.Atan2(b.y - a.y, b.x - a.x) * Mathf.Rad2Deg - 90f;
        }

        private void UpdateKnobs(float dt)
        {
            foreach (var kv in _knobs)
            {
                float target = TargetAngle(kv.Key);
                float cur = _knobAngle[kv.Key];
                cur = Mathf.MoveTowardsAngle(cur, target, 900f * dt);
                _knobAngle[kv.Key] = cur;
                kv.Value.localRotation = Quaternion.Euler(0f, 0f, cur);
            }
        }

        private void DrawPods()
        {
            // Una vista por cápsula en viaje (se reciclan las que terminaron).
            foreach (var pv in _pods)
            {
                if (pv.PodId < 0) continue;
                var pod = _sim.Pods.Find(q => q.Id == pv.PodId);
                if (pod == null || pod.Done) HidePod(pv);
            }
            foreach (var pod in _sim.Pods)
            {
                if (pod.Done) continue;
                _sim.Position(pod, out float x, out float y);
                var pos = ToUi(x, y);
                var pv = _pods.Find(v => v.PodId == pod.Id);
                if (pv == null)
                {
                    pv = _pods.Find(v => v.PodId < 0);
                    if (pv == null) continue;
                    pv.PodId = pod.Id;
                    pv.Body.sprite = TrafficSprites.Pod(pod.Color);
                    var tint = TrafficSprites.Colors[pod.Color];
                    pv.Glow.color = pod.Urgent ? NeuroStyle.WithAlpha(NeuroStyle.Sun, 0.6f) : NeuroStyle.WithAlpha(tint, 0.45f);
                    pv.Ring.gameObject.SetActive(pod.Urgent);
                    for (int i = 0; i < pv.Trail.Length; i++)
                    {
                        pv.Trail[i].color = NeuroStyle.WithAlpha(tint, 0.5f - 0.14f * i);
                        pv.Trail[i].gameObject.SetActive(true);
                    }
                    for (int i = 0; i < pv.History.Length; i++) pv.History[i] = pos;
                    pv.Rect.gameObject.SetActive(true);
                    StartCoroutine(PopIn(pv.Rect, 0.2f));
                }
                pv.Rect.anchoredPosition = pos;
                if (pod.Urgent)
                {
                    // Urgente: aro sol que late (no solo color: forma y movimiento propios) y un poco más grande.
                    float beat = Motion.Decorative ? 0.5f + 0.5f * Mathf.Sin(GameClock.Time * 9f) : 1f; // sin ReduceMotion: el aro urgente queda fijo (la forma sigue distinguiéndolo)
                    pv.Ring.rectTransform.localScale = Vector3.one * (1f + 0.12f * beat);
                    pv.Ring.color = NeuroStyle.WithAlpha(NeuroStyle.Sun, 0.75f + 0.25f * beat);
                    pv.Body.rectTransform.localScale = Vector3.one * 1.1f;
                }
                else pv.Body.rectTransform.localScale = Vector3.one;
                // Estela: puntos del color de la cápsula donde estuvo hace unos cuadros.
                pv.Head = (pv.Head + 1) % pv.History.Length;
                pv.History[pv.Head] = pos;
                for (int i = 0; i < pv.Trail.Length; i++)
                {
                    int back = (pv.Head - 3 * (i + 1) + pv.History.Length * 4) % pv.History.Length;
                    pv.Trail[i].rectTransform.anchoredPosition = pv.History[back];
                }
            }
            // Limpieza: las cápsulas entregadas ya no hacen falta en la simulación.
            _sim.Pods.RemoveAll(q => q.Done && _pods.TrueForAll(v => v.PodId != q.Id));
        }

        /// <summary>Muestra la fila de "próximas" (se oculta cuando ya no saldrán más).</summary>
        private void RefreshQueue()
        {
            bool any = false;
            for (int i = 0; i < _queuePods.Count; i++)
            {
                bool show = !_spawningClosed && i < _queue.Count && (!Precision || _spawned + i < TrafficContract.PrecisionPods);
                _queuePods[i].gameObject.SetActive(show);
                _queueRings[i].gameObject.SetActive(show && _queue[i].Urgent);
                if (show) _queuePods[i].sprite = TrafficSprites.Pod(_queue[i].Color);
                any |= show;
            }
            _queueLabel.gameObject.SetActive(any);
        }

        /// <summary>La fila avanza hacia la compuerta cuando sale una (se corre a la izquierda con suavidad).</summary>
        private void UpdateQueue(float dt)
        {
            _queueShift = Mathf.MoveTowards(_queueShift, 0f, dt * 360f);
            float beat = Motion.Decorative ? 0.5f + 0.5f * Mathf.Sin(GameClock.Time * 9f) : 1f; // sin ReduceMotion: el aro urgente queda fijo (la forma sigue distinguiéndolo)
            for (int i = 0; i < _queuePods.Count && i < _queueBase.Count; i++)
            {
                var p = _queueBase[i] + new Vector2(_queueShift, 0f);
                _queuePods[i].rectTransform.anchoredPosition = p;
                _queueRings[i].rectTransform.anchoredPosition = p;
                _queueRings[i].rectTransform.localScale = Vector3.one * (1f + 0.1f * beat);
            }
        }

        private static void HidePod(PodView pv)
        {
            pv.PodId = -1;
            pv.Rect.gameObject.SetActive(false);
            pv.Ring.gameObject.SetActive(false);
            foreach (var t in pv.Trail) t.gameObject.SetActive(false);
        }

        // ------------------------------------------------------------------ efectos

        private IEnumerator MarkAt(Vector2 pos, bool ok)
        {
            var img = NewImage(_fxRect, "Mark", ok ? AnswerMarkSprite.Check() : AnswerMarkSprite.Cross());
            img.gameObject.SetActive(true);
            var r = img.rectTransform;
            r.anchoredPosition = pos + new Vector2(PortSize * 0.35f, PortSize * 0.35f);
            r.sizeDelta = new Vector2(80f, 80f);
            yield return StartCoroutine(PopIn(r, 0.18f));
            yield return StartCoroutine(Wait(0.7f));
            Destroy(img.gameObject);
        }

        private void SetPrompt(string text, Color color)
        {
            _prompt.text = text;
            _prompt.color = color;
        }

        private IEnumerator FloatText(Vector2 pos, string text, Color color)
        {
            var t = MakeText(_fxRect, "Float", 52, TextAnchor.MiddleCenter, color, 0f, 0f);
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
            float accuracy = _delivered > 0 ? (float)_correct / _delivered : 0f;
            float median = TrafficContract.MedianLead(_leads);
            float share = TrafficContract.ProactiveShare(_leads);
            int score = TrafficContract.Score(accuracy, _dda.PeakLevel);
            int load = TrafficContract.CleanPeakLoad(_loadTimes, _loadCounts, _errFrom, _errTo);

            SetPrompt("Fin del turno", GoodColor);
            ShowResult(score, median, load);

            var telemetry = new StroopTelemetry
            {
                user_id = _config.user_id,
                game_id = _config.game_id,
                session_metrics = new StroopSessionMetrics
                {
                    correct_trials = _correct,
                    total_trials = _delivered,
                    calculated_score = score,
                    average_response_time_ms = 0,
                    level = _config.config.level,
                    timed = _config.config.timed,
                    end_rating = _dda.RatingNormalized,
                    mode_trials = _dda.ScoredTrials,
                    mode_hits = _dda.ScoredCorrect,
                    peak_level = _dda.PeakLevel,
                    traffic_lead_ms = median >= 0f ? Mathf.RoundToInt(median * 1000f) : -1,
                    traffic_proactive_pct = share >= 0f ? Mathf.RoundToInt(share * 100f) : -1,
                    traffic_peak_pods = load
                }
            };
            NativeBridge.ForwardTelemetryToPlatform(JsonUtility.ToJson(telemetry));
            yield break;
        }

        private void ShowResult(int score, float median, int load)
        {
            _exit.Show();
            _resultRoot.Find("Title").GetComponent<Text>().text = score >= 85 ? "¡Tráfico impecable!" : score >= 65 ? "¡Buen turno!" : "Turno completado";
            _resultRoot.Find("Detail").GetComponent<Text>().text = $"{_correct} de {_delivered} cápsulas a su planeta";
            _resultRoot.Find("Extra").GetComponent<Text>().text = load > 0 ? $"Tu carga: {load} a la vez sin errores"
                : median >= 0f ? $"Tu anticipación: {median:0.0} s".Replace('.', ',') : $"Mejor racha {_bestStreak}";
            _resultRoot.gameObject.SetActive(true);
            StartCoroutine(AnimateResult(score));
        }

        // ------------------------------------------------------------------ construcción de UI

        protected override void BuildUi()
        {

            var engineGo = new GameObject("Engine");
            engineGo.transform.SetParent(transform, false);
            _engine = engineGo.AddComponent<AudioSource>();
            _engine.playOnAwake = false;
            _engine.loop = true;
            _engine.spatialBlend = 0f;
            _engine.volume = 0f;
            _engine.clip = TrafficSounds.EngineLoop();

            var canvasGo = new GameObject("TrafficCanvas");
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
            WorldBackdrop.Build(bgRect, GameWorld.TrafficHub);

            var safeGo = new GameObject("SafeAreaContent");
            safeGo.transform.SetParent(canvasGo.transform, false);
            _safe = safeGo.AddComponent<RectTransform>();
            ApplySafeArea(_safe);

            _hud = new GameHud(_safe, "Tráfico Estelar", MarginU + 20f, this);
            BuildTimer();

            var playGo = new GameObject("Play");
            playGo.transform.SetParent(_safe, false);
            _play = playGo.AddComponent<RectTransform>();
            Stretch(_play);

            var fieldGo = new GameObject("Field");
            fieldGo.transform.SetParent(_play, false);
            _field = fieldGo.AddComponent<RectTransform>();
            _field.anchorMin = _field.anchorMax = new Vector2(0.5f, 0.5f);

            // Capas de las rutas: resplandor, sombras, rieles y línea de luz (cada capa entera sobre la anterior, así
            // ninguna sombra pisa un riel vecino).
            var haloRoot = Layer(_field, "RailHalos");
            var shadowRoot = Layer(_field, "RailShadows");
            var railRoot = Layer(_field, "Rails");
            var coreRoot = Layer(_field, "RailCores");
            for (int i = 0; i < EdgePool; i++)
            {
                _edges.Add(new EdgeView
                {
                    Halo = NewRail(haloRoot, RailLine.Style.Soft, 70f),
                    Shadow = NewRail(shadowRoot, RailLine.Style.Solid, 30f),
                    Rail = NewRail(railRoot, RailLine.Style.Clay, 30f),
                    Core = NewRail(coreRoot, RailLine.Style.Solid, 6f),
                });
            }
            var flowRoot = Layer(_field, "Flow");
            for (int i = 0; i < FlowPool; i++)
            {
                var dot = NewImage(flowRoot, "Light", DiscSprite.Get());
                dot.rectTransform.sizeDelta = new Vector2(12f, 12f);
                dot.color = NeuroStyle.WithAlpha(NeuroStyle.Cream, 0.95f);
                _flow.Add(dot.rectTransform);
            }

            // Estación de carga: de su compuerta salen las cápsulas.
            var stationGo = new GameObject("Station");
            stationGo.transform.SetParent(_field, false);
            _station = stationGo.AddComponent<RectTransform>();
            _station.anchorMin = _station.anchorMax = new Vector2(0.5f, 0.5f);
            _station.sizeDelta = new Vector2(StationSize, StationSize);
            var sg = NewImage(_station, "Glow", RadialGlowSprite.Get());
            sg.rectTransform.sizeDelta = new Vector2(StationSize * 1.9f, StationSize * 1.5f);
            sg.color = NeuroStyle.WithAlpha(NeuroStyle.Grape, 0.35f);
            sg.gameObject.SetActive(true);
            _beacon = NewImage(_station, "Beacon", RadialGlowSprite.Get());
            _beacon.rectTransform.sizeDelta = new Vector2(90f, 90f);
            _beacon.rectTransform.anchoredPosition = new Vector2(0f, 0.72f / 2.24f * StationSize);
            _beacon.gameObject.SetActive(true);
            var hull = NewImage(_station, "Body", TrafficSprites.Station());
            Stretch(hull.rectTransform);
            hull.gameObject.SetActive(true);
            _doorGlow = NewImage(_station, "DoorGlow", RadialGlowSprite.Get());
            _doorGlow.rectTransform.sizeDelta = new Vector2(150f, 150f);
            _doorGlow.rectTransform.anchoredPosition = new Vector2(0f, TrafficSprites.StationDoorY * StationSize + 20f);
            _doorGlow.color = NeuroStyle.WithAlpha(NeuroStyle.Sun, 0f);
            _doorGlow.gameObject.SetActive(true);
            stationGo.SetActive(false);

            // "Próximas": las que esperan en la estación, con un rótulo suelto (sin recuadro).
            var queueRoot = Layer(_field, "Queue");
            _queueLabel = MakeText(queueRoot, "QueueLabel", 30, TextAnchor.MiddleCenter, NeuroStyle.WithAlpha(NeuroStyle.Cream, 0.8f), 0f, 0f);
            _queueLabel.rectTransform.anchorMin = _queueLabel.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            _queueLabel.rectTransform.sizeDelta = new Vector2(260f, 44f);
            _queueLabel.text = "próximas";
            _queueLabel.gameObject.SetActive(false);
            for (int i = 0; i < TrafficContract.QueueSize; i++)
            {
                var ring = NewImage(queueRoot, "UrgentRing", RingSprite.Get());
                ring.rectTransform.sizeDelta = Vector2.one * QueueSizes[i] * 1.5f;
                ring.color = NeuroStyle.Sun;
                _queueRings.Add(ring);
                var qp = NewImage(queueRoot, "Next", null);
                qp.rectTransform.sizeDelta = Vector2.one * QueueSizes[i];
                qp.color = new Color(1f, 1f, 1f, 1f - 0.18f * i);
                _queuePods.Add(qp);
            }

            var portRoot = Layer(_field, "Ports");
            for (int i = 0; i < TrafficContract.MaxPorts; i++)
            {
                var img = NewImage(portRoot, "Port", null);
                img.rectTransform.sizeDelta = new Vector2(PortSize, PortSize);
                _portPool.Add(img.rectTransform);
            }
            var knobRoot = Layer(_field, "Switches");
            for (int i = 0; i < TrafficContract.MaxPorts; i++)
            {
                var img = NewImage(knobRoot, "Switch", TrafficSprites.SwitchKnob());
                img.rectTransform.sizeDelta = new Vector2(KnobSize, KnobSize);
                _knobPool.Add(img.rectTransform);
            }
            var trailRoot = Layer(_field, "Trails");
            var podRoot = Layer(_field, "Pods");
            for (int i = 0; i < PodPool; i++)
            {
                var trail = new Image[TrailDots];
                for (int t = 0; t < TrailDots; t++)
                {
                    trail[t] = NewImage(trailRoot, "Trail", DiscSprite.Get());
                    trail[t].rectTransform.sizeDelta = Vector2.one * (30f - 7f * t);
                }
                var go = new GameObject("Pod");
                go.transform.SetParent(podRoot, false);
                var r = go.AddComponent<RectTransform>();
                r.anchorMin = r.anchorMax = new Vector2(0.5f, 0.5f);
                r.sizeDelta = new Vector2(PodSize, PodSize);
                var glow = NewImage(r, "Glow", RadialGlowSprite.Get());
                glow.rectTransform.sizeDelta = new Vector2(PodSize * 2f, PodSize * 2f);
                glow.gameObject.SetActive(true);
                var ringImg = NewImage(r, "UrgentRing", RingSprite.Get());
                ringImg.rectTransform.sizeDelta = new Vector2(PodSize * 1.55f, PodSize * 1.55f);
                var body = NewImage(r, "Body", null);
                Stretch(body.rectTransform);
                body.gameObject.SetActive(true);
                go.SetActive(false);
                _pods.Add(new PodView { Rect = r, Body = body, Glow = glow, Ring = ringImg, Trail = trail });
            }
            _fxRect = Layer(_field, "Fx");

            _prompt = MakeText(_play, "Prompt", 54, TextAnchor.MiddleCenter, Color.white, 0f, 0f);
            NeuroStyle.ClayText(_prompt, 3.5f, 5f);
            _prompt.rectTransform.anchorMin = _prompt.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            BestFit(_prompt, 34);

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

        private static RailLine NewRail(Transform parent, RailLine.Style style, float thickness)
        {
            var go = new GameObject("Route");
            go.transform.SetParent(parent, false);
            var r = go.AddComponent<RectTransform>();
            r.anchorMin = r.anchorMax = new Vector2(0.5f, 0.5f);
            r.pivot = new Vector2(0.5f, 0.5f);
            r.sizeDelta = Vector2.zero;
            var line = go.AddComponent<RailLine>();
            line.Setup(style, thickness);
            go.SetActive(false);
            return line;
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
            float w = _play.rect.width, h = _play.rect.height;
            float top = h * 0.5f - (GameHud.Height + 40f), bottom = -h * 0.5f + 40f;
            _fieldW = w - MarginU * 2f;
            _fieldH = top - bottom;
            _field.sizeDelta = new Vector2(_fieldW, _fieldH);
            _field.anchoredPosition = new Vector2(0f, (top + bottom) * 0.5f);
            // Aviso en el hueco entre el portal y los puertos de los lados.
            _prompt.rectTransform.sizeDelta = new Vector2(w - 2f * MarginU, 90f);
            _prompt.rectTransform.anchoredPosition = new Vector2(0f, _field.anchoredPosition.y + (0.5f - 0.24f) * _fieldH);
        }

        private void UpdateHud()
        {
            _hud.SetLevel(_dda.PresentedLevel);
            if (Endless) _hud.SetPoints(_points);
            else _hud.SetInfo($"{Mathf.Min(_spawned, TrafficContract.PrecisionPods)} de {TrafficContract.PrecisionPods}");
        }
    }
}
