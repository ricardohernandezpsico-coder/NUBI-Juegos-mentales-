using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using NeuroVida.Bridge;
using NeuroVida.Contracts;
using NeuroVida.Games.Shared;
using static NeuroVida.Games.Shared.UiKit;
using Motion = NeuroVida.Games.Shared.Motion; // UnityEngine.Motion también existe

namespace NeuroVida.Games.Secuencia
{
    /// <summary>
    /// «Rastro de luz» (id interno <c>secuencia</c>): Secuencia Lumínica rehecha desde cero; juego estrella de Memoria (docs/diseno-rastro-de-luz.md).
    /// Una chispa de luz vuela entre nueve luceros de cristal y cada lucero suena con su nota: el camino se ve y se oye como una melodía. La persona lo
    /// repite deslizando el dedo (su trazo deja una cinta de luz que se engancha a cada lucero) o tocando uno por uno. Cuatro familias que se desbloquean
    /// (con «¡NUEVO!», una sola vez en la vida): el rastro, al revés, el cielo gira y en marcha. Las reglas viven en <see cref="RastroSession"/> (el motor
    /// común de 16 niveles, el director de modos, las vidas) y la geometría en <see cref="RastroBoard"/>; este controlador solo dibuja y lee el dedo.
    /// Reto = 90 s; Precisión = 14 rondas; en los dos, 3 vidas. Con «quitar animaciones»: el giro se queda (es la tarea) pero lineal, sin partículas, sin
    /// respiración y sin vuelo de chispas al contador; el rastro y la cinta pasan a tramos rectos con fundido de 300 ms y las esperas duran lo mismo.
    /// </summary>
    public class RastroGameController : GameControllerBase
    {
        public const string GameId = RastroContract.GameId;
        private const string UnlockKey = "rastro_modes";

        private const float UnitsPerDp = 3f;
        private const float LogicalW = 360f;

        // tinte del cielo y color del rastro de cada modo (docs §3)
        private static readonly Color[] SkyTint =
        {
            Rgb(76, 201, 240), Rgb(184, 164, 255), Rgb(120, 240, 200), Rgb(255, 159, 122)
        };
        private static readonly Color[] TrailTint =
        {
            Rgb(255, 226, 122), Rgb(200, 170, 255), Rgb(160, 250, 210), Rgb(255, 190, 150)
        };
        /// <summary>Alfa del tinte del modo en los bordes (viñeta): nunca más claro que el fondo de los demás juegos.</summary>
        private const float TintAlpha = 0.05f;
        private static readonly Color Dim = Rgb(171, 165, 210);
        private static readonly Color Cream = Rgb(255, 246, 224);
        private static readonly Color Sun = Rgb(255, 201, 74);
        private static readonly Color Coral = Rgb(255, 122, 92);
        private const float TrailFadeSeconds = 0.3f;      // «quitar animaciones»: fundido de los tramos

        private enum Phase { Idle, Show, Turn, Input, Done }

        private static Color Rgb(int r, int g, int b) => new Color(r / 255f, g / 255f, b / 255f, 1f);

        // ------------------------------------------------------------------ estado

        private System.Random _rng;
        private RastroSession _session;
        private Phase _phase = Phase.Idle;
        private bool _senior;
        private int _previousFrameRate;
        private float _angle;                     // giro actual del tablero (grados)
        private RastroMode _tintMode = RastroMode.Rastro;
        private float _clockStart, _clockComp;
        private bool _clockOn;
        private int _lastTickSecond = -1;
        private int _counter;                     // «luces recordadas»
        private int _hintOrb = -1;                // ronda guiada: el lucero que sigue
        private readonly List<int> _input = new List<int>();
        private float _inputMs;
        private int _inputLights;

        // escala y disposición
        private float _s = 3f, _playW, _playH, _yShift, _logicalH;

        // UI
        private RectTransform _safe, _play, _hud, _timerTrack;
        private RectTransform _timerFill;
        private Image _disc, _tint, _sparkHalo, _sparkCore;
        private LightMesh _fixedMesh, _trailMesh, _ribbonMesh;
        private Text _rule, _sub;
        private RectTransform _modePill, _countPill;
        private Image _modeIcon;
        private Text _modeText, _countText, _roundText, _counterNum, _counterLabel;
        private readonly Image[] _lifeDots = new Image[RastroContract.Lives];
        private readonly OrbView[] _orbs = new OrbView[RastroBoard.Orbs];
        private readonly List<Particle> _particles = new List<Particle>();
        private readonly List<Bloom> _blooms = new List<Bloom>();
        private readonly List<Flyer> _flyers = new List<Flyer>();
        private RectTransform _unlockRoot;
        private CanvasGroup _unlockGroup;
        private Image _unlockGlow, _unlockIcon;
        private Text _unlockNew, _unlockName, _unlockLine;
        private RectTransform _cueRoot;
        private Image _cueIcon;
        private Text _cueText;
        private ExitButton _exit;
        private CountdownScreen _countdown;

        private sealed class OrbView
        {
            public RectTransform Root;
            public Image Halo, Body, Aro, Hint, X;
            public float HitAt = -10f;
            public float Phase;
        }

        private struct Pt { public Vector2 Pos; public float T; }
        private struct Link { public int A, B; public float Born; }
        private sealed class Particle { public Image Img; public Vector2 Pos, Vel; public float Age, Life, Size; public Color Col; public bool Alive; }
        private sealed class Bloom { public Image Img; public Vector2 Pos; public float Born; public Color Col; public bool Alive; }
        private sealed class Flyer { public Image Img; public Vector2 From; public float Start; public bool Alive; }

        private readonly List<Pt> _trail = new List<Pt>();            // rastro de la chispa (movimiento normal)
        private readonly List<Link> _trailLinks = new List<Link>();   // rastro como tramos rectos («quitar animaciones»)
        private readonly List<Pt> _ribbon = new List<Pt>();           // cinta del dedo
        private readonly List<Link> _fixed = new List<Link>();        // tramos acertados
        private float _trailFade = 1f;
        private bool _trailFading;
        private float _winAt = -10f;
        private bool _sparkOn;
        private Vector2 _sparkPos;                                    // en unidades del lienzo
        private float _fadeSparkAt = -10f;

        private readonly struct PointerEvent
        {
            public readonly Vector2 Logical;
            public PointerEvent(Vector2 l) { Logical = l; }
        }
        private readonly Queue<PointerEvent> _events = new Queue<PointerEvent>();

        private sealed class InputResult
        {
            public bool Complete, Abandoned, Skipped, Confusion;
            public int Wrong = -1, Expected = -1;
        }

        // ------------------------------------------------------------------ sesión

        public void StartSession(SequenceInitConfig config)
        {
            _config = config;
            _rng = new System.Random();
            _senior = DdaUserProfileConfig.ParseAgeBand(config.config.age_band) == AgeBand.Senior;
            int unlocked = 0;
            try { unlocked = PlayerPrefs.GetInt(UnlockKey, 0); } catch (System.Exception) { }
            _session = new RastroSession(config.config, _rng, unlocked);

            _previousFrameRate = Application.targetFrameRate;
            Application.targetFrameRate = 60;

            _phase = Phase.Idle;
            _angle = 0f;
            _counter = 0;
            _hintOrb = -1;
            _clockOn = false;
            _clockComp = 0f;
            _lastTickSecond = -1;
            _inputMs = 0f;
            _inputLights = 0;
            _tintMode = RastroMode.Rastro;
            _events.Clear();

            _resultRoot.gameObject.SetActive(false);
            _exit.Hide();
            _timerTrack.gameObject.SetActive(_session.Timed && !_session.Assessment);
            _unlockRoot.gameObject.SetActive(false);
            _tutorial.Hide();
            ClearRound();
            ClearFx();
            _tint.color = NeuroStyle.WithAlpha(SkyTint[0], TintAlpha);
            _cueRoot.gameObject.SetActive(false);

            StopAllCoroutines();
            StartCoroutine(GameLoop());
        }

        private void OnDisable()
        {
            if (_previousFrameRate != 0) Application.targetFrameRate = _previousFrameRate;
        }

        private float Elapsed => _clockOn ? GameClock.Time - _clockStart - _clockComp : 0f;
        // En la versión corta del inicio («Tu punto de partida») el tiempo NO corta una ronda empezada (se veía «¡Listo!» a mitad de un ejercicio): solo impide EMPEZAR otra (MainLoop).
        private bool TimeIsUp => _clockOn && !_session.Assessment && _session.IsOver(Elapsed);

        private IEnumerator GameLoop()
        {
            StartCoroutine(Prewarm());
            if (TutorialWanted)
            {
                // el tutorial se juega sobre el cielo ya armado, antes de la cuenta regresiva
                _safe.gameObject.SetActive(true);
                yield return null;
                ApplySafeArea(_safe);
                Canvas.ForceUpdateCanvases();
                Layout();
                ResetHud(RastroMode.Rastro, RastroContract.GuidedLength);
                yield return StartCoroutine(RunTutorialIfNeeded());
            }
            _safe.gameObject.SetActive(false);
            yield return StartCoroutine(_countdown.Play("Rastro de luz", Assessment.Subtitle("Mira el camino y repítelo"), () => _safe.gameObject.SetActive(true)));
            _safe.gameObject.SetActive(true);
            yield return null;
            ApplySafeArea(_safe);
            Canvas.ForceUpdateCanvases();
            Layout();
            ResetHud(RastroMode.Rastro, 0);
            ClearRound();

            _clockStart = GameClock.Time;
            _clockOn = true;
            yield return StartCoroutine(MainLoop());
        }

        /// <summary>El bucle de la partida: una ronda tras otra hasta que se acaben las vidas, el tiempo o las rondas. «Cómo se juega» lo retoma desde acá.</summary>
        private IEnumerator MainLoop()
        {
            while (!_session.IsOver(Elapsed))
            {
                var round = _session.NextRound();
                yield return StartCoroutine(PlayRound(round));
            }
            yield return StartCoroutine(FinishGame());
        }

        // ------------------------------------------------------------------ «Cómo se juega» desde la pausa

        private bool _clockWasOn;

        protected override bool HowToReady => _clockOn && _phase != Phase.Done;

        protected override void HowToSuspend()
        {
            _clockWasOn = _clockOn;
            _clockOn = false;                       // el reloj del Reto no corre mientras se ve «Cómo se juega»
            _phase = Phase.Idle;
            _events.Clear();
            _unlockRoot.gameObject.SetActive(false);
            ClearRound();
            ClearFx();
            _hintOrb = -1;
        }

        protected override void HowToResume(float spentSeconds)
        {
            _clockComp += spentSeconds;             // lo que duró no se le descuenta al Reto
            _clockOn = _clockWasOn;
            _phase = Phase.Idle;
            ClearRound();
            ResetHud(RastroMode.Rastro, 0);
            StartCoroutine(MainLoop());             // la ronda que estaba en curso no contó: sigue otra con el mismo estado
        }

        /// <summary>Hornea sprites y sintetiza sonidos durante la tarjeta y la cuenta regresiva (así la primera ronda no traba).</summary>
        private IEnumerator Prewarm()
        {
            RastroSprites.XMark();
            RastroSprites.DashedRing();
            yield return null;
            foreach (var m in RastroModes.All) { RastroSprites.Icon(m); yield return null; }
            for (int i = 0; i < RastroBoard.Orbs; i++)
            {
                RastroSounds.Orb(i);
                if (i % 3 == 2) yield return null;
            }
            RastroSounds.Chord();
            yield return null;
            RastroSounds.Wrong();
            RastroSounds.Air();
            RastroSounds.Unlock();
            foreach (var m in RastroModes.All) { RastroSounds.ModeCue(m); yield return null; }
        }

        // ------------------------------------------------------------------ una ronda

        private IEnumerator PlayRound(RastroRound r)
        {
            SetMode(r);
            // el «¡NUEVO!» de por vida va primero; si coincide con un cambio de modo, sale solo el «¡NUEVO!»
            if (r.Notice != RastroNotice.None) yield return StartCoroutine(ModeScreen(r));
            InputResult res;
            while (true)
            {
                yield return StartCoroutine(Show(r));
                if (!TimeIsUp && r.Mode == RastroMode.Gira) yield return StartCoroutine(Turn(r));
                res = new InputResult();
                if (!TimeIsUp) yield return StartCoroutine(InputPhase(r, res));
                else res.Abandoned = true;
                _events.Clear();
                if (!res.Confusion) break;
                // «confusión de modo»: no cuenta (ni error, ni vida, ni DDA); Nubi lo explica y se repite la MISMA muestra
                yield return StartCoroutine(ConfusionMessage(r));
                ClearRound();
            }
            if (res.Abandoned)
            {
                // se acabó el tiempo a mitad de ronda: no cuenta
                ClearRound();
                yield break;
            }

            _session.Complete(r, res.Complete);
            UpdateHud();
            if (res.Complete) yield return StartCoroutine(Win(r));
            else yield return StartCoroutine(Fail(r, res));
            ClearRound();
            UpdateHud();
        }

        private IEnumerator ConfusionMessage(RastroRound r)
        {
            _hintOrb = -1;
            _cueRoot.gameObject.SetActive(false);
            PlayClip(RastroSounds.ModeCue(r.Mode), 0.8f);
            string what = r.Mode == RastroMode.Reves ? "Esta era al revés" : "Esta era en marcha: solo las últimas " + r.Asked;
            Rule("Nubi: ¡Ojo! " + what + ". Mírala de nuevo", "Esta vez no cuenta");
            yield return StartCoroutine(Motion.Hold(2.4f));
        }

        private void SetMode(RastroRound r)
        {
            StartCoroutine(Motion.ColorTo(_tint, NeuroStyle.WithAlpha(SkyTint[(int)r.Mode], TintAlpha), Motion.Decorative ? 0.6f : Motion.FadeSeconds));
            _tintMode = r.Mode;
            ResetHud(r.Mode, r.Asked);
            _hintOrb = -1;
        }

        // ------------------------------------------------------------------ la chispa muestra el camino

        private IEnumerator Show(RastroRound r)
        {
            _phase = Phase.Show;
            ClearRound();
            Rule(RastroModes.WatchRule(r.Mode, r.Asked), RastroModes.Name(r.Mode));
            yield return StartCoroutine(Motion.Hold(0.45f));

            Color trail = TrailTint[(int)r.Mode];
            _trailFade = 1f;
            _trailFading = false;
            _sparkOn = true;
            _sparkHalo.color = NeuroStyle.WithAlpha(trail, 0.9f);
            _sparkHalo.gameObject.SetActive(true);
            _sparkCore.gameObject.SetActive(true);
            for (int k = 0; k < r.Shown.Length; k++)
            {
                if (TimeIsUp) break;
                int orb = r.Shown[k];
                if (k == 0) _sparkPos = OrbPlay(orb);
                else yield return StartCoroutine(Fly(r.Shown[k - 1], orb, k, r.SparkSpeed, trail));
                Arrive(orb);
                yield return StartCoroutine(Motion.Hold(r.WaitMs / 1000f));
            }
            yield return StartCoroutine(Motion.Hold(0.25f));
            _sparkOn = false;
            _sparkHalo.gameObject.SetActive(false);
            _sparkCore.gameObject.SetActive(false);
            _trailFading = true;               // el rastro se apaga antes de girar o de pedir el camino
        }

        private IEnumerator Fly(int from, int to, int leg, float speed, Color trail)
        {
            Vector2 a = RastroBoard.Position(from, _angle), b = RastroBoard.Position(to, _angle);
            Vector2 c = RastroBoard.CurveControl(a, b, leg);
            float dur = Mathf.Max(0.26f, Vector2.Distance(a, b) / speed);
            float t = 0f;
            while (t < dur)
            {
                t += GameClock.DeltaTime;
                float k = Mathf.Clamp01(t / dur);
                // la curva y las partículas son adorno; el vuelo de orden a orden es la tarea y se queda
                Vector2 logical = Motion.Decorative ? RastroBoard.Bezier(a, c, b, k) : Vector2.Lerp(a, b, k);
                _sparkPos = LogicalToPlay(logical);
                if (Motion.Decorative)
                {
                    _trail.Add(new Pt { Pos = _sparkPos, T = GameClock.Time });
                    Emit(_sparkPos, 2, 30f, 0.5f, trail);
                }
                yield return null;
            }
            if (!Motion.Decorative) _trailLinks.Add(new Link { A = from, B = to, Born = GameClock.Time });
        }

        /// <summary>La chispa llega a un lucero: suena su nota, florece y salen chispas de su color.</summary>
        private void Arrive(int orb)
        {
            PlayClip(RastroSounds.Orb(orb), 1f);
            Flash(orb, 10, 90f);
        }

        private void Flash(int orb, int sparks, float spread)
        {
            _orbs[orb].HitAt = GameClock.Time;
            AddBloom(OrbPlay(orb), RastroBoard.Colors[orb]);
            Emit(OrbPlay(orb), sparks, spread, 0.65f, OrbColor(orb));
        }

        // ------------------------------------------------------------------ el cielo gira

        private IEnumerator Turn(RastroRound r)
        {
            _phase = Phase.Turn;
            Rule("¡El cielo gira!", RastroModes.Name(r.Mode));
            PlayClip(RastroSounds.Air(), 1f);
            float from = _angle, to = _angle + r.TurnDegrees;
            float dur = Motion.Decorative ? 1.4f : 1.5f;     // el giro SE QUEDA con «quitar animaciones» (es la tarea), pero lineal
            float t = 0f;
            while (t < dur)
            {
                t += GameClock.DeltaTime;
                float k = Mathf.Clamp01(t / dur);
                _angle = Mathf.Lerp(from, to, Motion.Decorative ? EaseInOut(k) : k);
                yield return null;
            }
            _angle = Mathf.Repeat(to, 360f);
        }

        private static float EaseInOut(float t) => t < 0.5f ? 2f * t * t : 1f - Mathf.Pow(-2f * t + 2f, 2f) / 2f;

        // ------------------------------------------------------------------ la persona repite

        private IEnumerator InputPhase(RastroRound r, InputResult res)
        {
            _phase = Phase.Input;
            _input.Clear();
            _fixed.Clear();
            _ribbon.Clear();
            _events.Clear();
            Rule(RastroModes.RepeatRule(r.Mode, r.Asked), RastroModes.Name(r.Mode));
            if (r.Guided) _hintOrb = r.Target[0];
            else ShowCue(r);
            float startedAt = GameClock.Time;
            float radius = _senior ? RastroBoard.HitRadiusSenior : RastroBoard.HitRadius;
            while (true)
            {
                if (r.Guided && GuidedTutorial.AutoPlay(GameClock.Time - startedAt)) { res.Complete = true; _phase = Phase.Idle; yield break; }   // solo en el smoke del Editor
                if (r.Guided ? _tutorial.Skipped : TimeIsUp)
                {
                    res.Abandoned = !r.Guided;
                    res.Skipped = r.Guided;
                    _phase = Phase.Idle;
                    yield break;
                }
                while (_events.Count > 0)
                {
                    var ev = _events.Dequeue();
                    if (Motion.Decorative) { _ribbon.Add(new Pt { Pos = LogicalToPlay(ev.Logical), T = GameClock.Time }); Emit(LogicalToPlay(ev.Logical), 1, 20f, 0.38f, TrailTint[(int)r.Mode]); }
                    int orb = RastroBoard.OrbAt(ev.Logical, _angle, radius);
                    if (orb < 0 || (_input.Count > 0 && orb == _input[_input.Count - 1])) continue;   // nunca el mismo lucero dos veces seguidas
                    int k = _input.Count;
                    _input.Add(orb);
                    if (r.Target[k] != orb)
                    {
                        if (k == 0 && _session.TryModeConfusion(r, orb))
                        {
                            res.Confusion = true;
                            _phase = Phase.Idle;
                            yield break;
                        }
                        res.Wrong = orb;
                        res.Expected = r.Target[k];
                        _phase = Phase.Idle;
                        yield break;
                    }
                    PlayClip(RastroSounds.Orb(orb), 0.88f);
                    Flash(orb, 12, 110f);
                    if (k > 0) _fixed.Add(new Link { A = _input[k - 1], B = orb, Born = GameClock.Time });
                    if (_input.Count == r.Asked)
                    {
                        res.Complete = true;
                        if (!r.Guided) { _inputMs += (GameClock.Time - startedAt) * 1000f; _inputLights += r.Asked; }
                        _phase = Phase.Idle;
                        yield break;
                    }
                    if (r.Guided) _hintOrb = r.Target[k + 1];
                }
                yield return null;
            }
        }

        // ------------------------------------------------------------------ acierto y error

        private IEnumerator Win(RastroRound r)
        {
            Rule("¡Exacto!", "");
            _hintOrb = -1;
            PlayClip(RastroSounds.Chord(), 1f);
            GameFeel.Haptic(GameFeel.HapticKind.Light);
            _winAt = GameClock.Time;
            if (!r.Guided)
            {
                int add = r.Asked;
                if (Motion.Decorative)
                {
                    float now = GameClock.Time;
                    for (int j = 0; j < _fixed.Count; j++) LaunchFlyer(OrbPlay(_fixed[j].B), now + j * 0.06f);
                    LaunchFlyer(OrbPlay(_input[0]), now);
                    StartCoroutine(CountAfter(0.75f + 0.06f * _fixed.Count, add));
                }
                else
                {
                    _counter += add;
                    UpdateHud();
                }
            }
            yield return StartCoroutine(Motion.Hold(1.5f));
        }

        private IEnumerator CountAfter(float seconds, int add)
        {
            yield return StartCoroutine(Motion.Hold(seconds));
            _counter += add;
            UpdateHud();
            StartCoroutine(PopRect(_counterNum.rectTransform, 1.18f, 0.25f));
        }

        private IEnumerator Fail(RastroRound r, InputResult res)
        {
            _hintOrb = -1;
            var wrong = _orbs[res.Wrong];
            var right = _orbs[res.Expected];
            wrong.Aro.color = NeuroStyle.WithAlpha(Coral, 1f);
            wrong.Aro.gameObject.SetActive(true);
            wrong.X.color = Coral;
            wrong.X.gameObject.SetActive(true);
            right.Aro.color = NeuroStyle.WithAlpha(Sun, 1f);
            right.Aro.gameObject.SetActive(true);
            PlayClip(RastroSounds.Wrong(), 1f);
            GameFeel.Haptic(GameFeel.HapticKind.Double);
            Rule("Esa no era", "La correcta tiene el aro amarillo");
            yield return StartCoroutine(Motion.Hold(1.8f));
        }

        // ------------------------------------------------------------------ ronda guiada del tutorial (pieza común)

        // <guided>
        protected override IEnumerator GuidedRound(GuidedTutorial tutorial)
        {
            tutorial.BeginPractice();
            var coach = tutorial.Coach;
            var r = _session.GuidedRound();
            SetMode(r);
            // zona protegida: el contador «luces recordadas» (lo que suma cada ronda) queda iluminado junto al tablero
            var counter = LightsZone(coach);
            // «Nubi entrenadora»: 1) mirar cómo vuela la chispa, 2) tocar el primer lucero (el toque es el de verdad), 3) un aviso breve. Nada se queda esperando en silencio.
            while (!tutorial.Skipped)
            {
                StartCoroutine(coach.Watch(BoardHole, CoachTexts.Rastro.Watch, () => _phase != Phase.Show, 15f, keep: counter));
                yield return StartCoroutine(Show(r));
                if (tutorial.Skipped) break;
                var res = new InputResult();
                var input = StartCoroutine(InputPhase(r, res));
                int first = r.Target[0];
                yield return StartCoroutine(coach.Touch(() => OrbHole(first), CoachTexts.Rastro.Repeat, circle: true, keep: counter));
                yield return input;
                _events.Clear();
                if (tutorial.Skipped) break;
                if (res.Complete)
                {
                    PlayClip(RastroSounds.Chord(), 1f);
                    _winAt = GameClock.Time;
                    _hintOrb = -1;
                    yield return StartCoroutine(coach.Notice(CoachTexts.Rastro.Good, 1.8f, counter));
                    break;
                }
                // error: sin culpa, se explica y se repite el mismo camino
                _orbs[res.Wrong].Aro.color = NeuroStyle.WithAlpha(Coral, 1f);
                _orbs[res.Wrong].Aro.gameObject.SetActive(true);
                _orbs[res.Wrong].X.color = Coral;
                _orbs[res.Wrong].X.gameObject.SetActive(true);
                _orbs[res.Expected].Aro.color = NeuroStyle.WithAlpha(Sun, 1f);
                _orbs[res.Expected].Aro.gameObject.SetActive(true);
                PlayClip(RastroSounds.Wrong(), 1f);
                yield return StartCoroutine(coach.Notice(CoachTexts.Rastro.Missed, 2.4f, counter));
                ClearRound();
            }
            _hintOrb = -1;
            ClearRound();
            if (!tutorial.Skipped) yield return StartCoroutine(coach.Notice(CoachTexts.Ready, 1.5f));
            tutorial.EndPractice();
            _phase = Phase.Idle;
        }
        // </guided>

        /// <summary>El contador «luces recordadas» como zona protegida del tutorial.</summary>
        private Func<Rect>[] LightsZone(NubiCoach coach) => new[] { coach.ZoneOfTexts(_counterNum, _counterLabel) };

        /// <summary>El tablero entero de luceros (para el foco de «mirar»).</summary>
        private Rect BoardHole()
        {
            var coach = _tutorial.Coach;
            Rect bounds = Rect.zero;
            bool first = true;
            for (int i = 0; i < RastroBoard.Orbs; i++)
            {
                var o = coach.AroundOf(_orbs[i].Root, new Vector2(230f, 230f));
                if (first) { bounds = o; first = false; }
                else bounds = Rect.MinMaxRect(Mathf.Min(bounds.xMin, o.xMin), Mathf.Min(bounds.yMin, o.yMin), Mathf.Max(bounds.xMax, o.xMax), Mathf.Max(bounds.yMax, o.yMax));
            }
            return bounds;
        }

        /// <summary>Un lucero (para el foco de «tocar»).</summary>
        private Rect OrbHole(int orb) => _tutorial.Coach.AroundOf(_orbs[orb].Root, new Vector2(230f, 230f));

        // ------------------------------------------------------------------ «¡NUEVO!»

        /// <summary>El cartel antes de la muestra: «¡NUEVO!» la primera vez de por vida que se llega a un modo, o «Ahora: AL REVÉS» al cambiar de modo.</summary>
        private IEnumerator ModeScreen(RastroRound r)
        {
            bool isNew = r.Notice == RastroNotice.NewMode;
            var mode = r.Mode;
            if (isNew) { try { PlayerPrefs.SetInt(UnlockKey, _session.Director.Unlocked); PlayerPrefs.Save(); } catch (System.Exception) { } }
            float began = GameClock.Time;
            Color sky = SkyTint[(int)mode];
            _unlockGlow.color = NeuroStyle.WithAlpha(sky, 0.5f);
            _unlockIcon.sprite = RastroSprites.Icon(mode);
            _unlockNew.text = isNew ? "¡NUEVO!" : "Ahora:";
            _unlockName.text = isNew ? RastroModes.Name(mode) : RastroModes.NoticeTitle(mode);
            _unlockLine.text = isNew ? RastroModes.UnlockLine(mode) : RastroModes.NoticeLine(mode, r.Asked);
            _unlockRoot.gameObject.SetActive(true);
            PlayClip(isNew ? RastroSounds.Unlock() : RastroSounds.ModeCue(mode), 1f);
            if (isNew) GameFeel.Haptic(GameFeel.HapticKind.Firm);
            yield return StartCoroutine(Motion.Fade(_unlockGroup, 0f, 1f, Motion.Decorative && isNew ? 0.4f : Motion.FadeSeconds));
            float seconds = isNew ? RastroContract.UnlockSeconds : RastroContract.NoticeSeconds;
            float t = 0f;
            while (t < seconds)
            {
                t += GameClock.DeltaTime;
                if (t > (isNew ? 0.5f : 0.3f) && GuidedTutorial.TryPress(out _)) break;     // un toque sigue
                yield return null;
            }
            yield return StartCoroutine(Motion.Fade(_unlockGroup, 1f, 0f, Motion.FadeSeconds));
            _unlockRoot.gameObject.SetActive(false);
            _clockComp += GameClock.Time - began;                          // el cartel no le gasta tiempo al Reto
        }

        // ------------------------------------------------------------------ fin

        private IEnumerator FinishGame()
        {
            _phase = Phase.Done;
            ClearRound();
            double avgMs = _inputLights > 0 ? _inputMs / _inputLights : 0.0;
            var metrics = RastroContract.BuildMetrics(_session, avgMs, _config.config);
            ShowResult(metrics);
            var telemetry = new SequenceTelemetry { user_id = _config.user_id, game_id = _config.game_id, session_metrics = metrics };
            NativeBridge.ForwardTelemetryToPlatform(JsonUtility.ToJson(telemetry));
            yield break;
        }

        private void ShowResult(SequenceSessionMetrics m)
        {
            _exit.Show();
            int score = m.calculated_score;
            _resultRoot.Find("Title").GetComponent<Text>().text = score >= 85 ? "¡Qué buen rastro!" : score >= 65 ? "¡Buen camino!" : "Misión completada";
            int best = _session.Tally.BestLength[(int)RastroMode.Rastro];
            _resultRoot.Find("Detail").GetComponent<Text>().text = $"{m.correct_rounds} de {m.total_rounds} bien" + (best > 0 ? $" · tu rastro {best} luces" : "");
            _resultRoot.Find("Extra").GetComponent<Text>().text = _session.NewModes != 0 ? "¡Desbloqueaste un modo nuevo!" : "Sigue encendiendo luces";
            _resultRoot.gameObject.SetActive(true);
            StartCoroutine(AnimateResult(score));
        }

        // ------------------------------------------------------------------ entrada del dedo y cuadro a cuadro

        private void Update()
        {
            float dt = GameClock.DeltaTime;
            ReadPointer();
            if (_safe == null || !_safe.gameObject.activeInHierarchy) return;
            PlaceOrbs(dt);
            UpdateFx(dt);
            UpdateTimer();
        }

        private void ReadPointer()
        {
            bool down, held;
            Vector2 pos;
            if (Input.touchCount > 0)
            {
                var t = Input.GetTouch(0);
                down = t.phase == TouchPhase.Began;
                held = t.phase != TouchPhase.Ended && t.phase != TouchPhase.Canceled;
                pos = t.position;
            }
            else
            {
                down = Input.GetMouseButtonDown(0);
                held = Input.GetMouseButton(0);
                pos = Input.mousePosition;
            }
            if (!down && !held) return;
            if (down && _tutorial != null && _tutorial.TrySkip(pos)) return;       // «Saltar tutorial»
            if (_tutorial != null && _tutorial.Coach.Blocks(pos)) return;          // el foco de Nubi: solo vale el toque dentro del hueco
            if (_phase != Phase.Input) return;
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(_play, pos, null, out var local)) return;
            _events.Enqueue(new PointerEvent(PlayToLogical(local)));
        }

        private void UpdateTimer()
        {
            if (!_timerTrack.gameObject.activeSelf || !_clockOn || _phase == Phase.Done) return;
            float left = Mathf.Max(0f, RastroContract.RetoSeconds - Elapsed);
            float frac = left / RastroContract.RetoSeconds;
            _timerFill.anchorMax = new Vector2(Mathf.Clamp01(frac), 1f);
            _timerFill.GetComponent<Image>().color = left > 15f ? SkyTint[0] : Sun;
            int whole = Mathf.CeilToInt(left);
            if (whole <= 5 && whole >= 1 && whole != _lastTickSecond)
            {
                _lastTickSecond = whole;
                GameFeel.Tick();
            }
        }

        private void PlaceOrbs(float dt)
        {
            float now = GameClock.Time;
            float diameter = _senior ? RastroBoard.OrbDiameterSenior : RastroBoard.OrbDiameter;
            for (int i = 0; i < RastroBoard.Orbs; i++)
            {
                var o = _orbs[i];
                o.Root.anchoredPosition = OrbPlay(i);
                float f = Mathf.Max(0f, 1f - (now - o.HitAt) / 0.7f);
                float breathe = Motion.Decorative ? 1f + 0.05f * Mathf.Sin(now / 0.9f + i) : 1f;
                float haloD = diameter * 1.9f * breathe + 28f * f * (Motion.Decorative ? 1f : 0f);
                o.Halo.rectTransform.sizeDelta = new Vector2(haloD * _s, haloD * _s);
                o.Halo.color = NeuroStyle.WithAlpha(OrbColor(i), 0.35f + 0.65f * f);
                o.Body.rectTransform.localScale = Vector3.one * (Motion.Decorative ? 1f + 0.12f * f : 1f);
                if (o.Aro.gameObject.activeSelf)
                {
                    float pulse = Motion.Decorative ? 1f + 0.06f * Mathf.Sin(now * 8f) : 1f;
                    o.Aro.rectTransform.localScale = Vector3.one * pulse;
                }
                bool hint = i == _hintOrb;
                if (o.Hint.gameObject.activeSelf != hint) o.Hint.gameObject.SetActive(hint);
                if (hint) o.Hint.rectTransform.localEulerAngles = new Vector3(0f, 0f, Motion.Decorative ? -now * 25f : 0f);
            }
            _disc.rectTransform.localEulerAngles = new Vector3(0f, 0f, -_angle);
        }

        private void UpdateFx(float dt)
        {
            float now = GameClock.Time;

            // rastro de la chispa
            if (_trailFading)
            {
                _trailFade -= dt / TrailFadeSeconds;
                if (_trailFade <= 0f) { _trailFade = 0f; _trailFading = false; _trail.Clear(); _trailLinks.Clear(); }
            }
            _trailMesh.Clear();
            Color tc = TrailTint[(int)_tintMode];
            if (_trailFade > 0f)
            {
                float life = _tintMode == RastroMode.Marcha ? 0.9f : 1.5f;
                for (int i = _trail.Count - 1; i >= 0; i--) if (now - _trail[i].T >= life) { _trail.RemoveRange(0, i + 1); break; }
                for (int j = 1; j < _trail.Count; j++)
                {
                    float a = (1f - (now - _trail[j].T) / life) * _trailFade;
                    float aq = (1f - (now - _trail[j - 1].T) / life) * _trailFade;
                    _trailMesh.Add(_trail[j - 1].Pos, _trail[j].Pos, (1f + 5f * aq) * _s, (1f + 5f * a) * _s,
                        NeuroStyle.WithAlpha(tc, 0.85f * aq), NeuroStyle.WithAlpha(tc, 0.85f * a));
                }
                // «quitar animaciones»: tramos rectos que aparecen con un fundido de 300 ms
                foreach (var l in _trailLinks)
                {
                    float a = Mathf.Clamp01((now - l.Born) / TrailFadeSeconds) * _trailFade * 0.75f;
                    _trailMesh.Add(OrbPlay(l.A), OrbPlay(l.B), 4f * _s, NeuroStyle.WithAlpha(tc, a));
                }
            }
            _trailMesh.Commit();

            // cinta del dedo
            _ribbonMesh.Clear();
            for (int i = _ribbon.Count - 1; i >= 0; i--) if (now - _ribbon[i].T >= 0.7f) { _ribbon.RemoveRange(0, i + 1); break; }
            for (int j = 1; j < _ribbon.Count; j++)
            {
                float a = 1f - (now - _ribbon[j].T) / 0.7f, aq = 1f - (now - _ribbon[j - 1].T) / 0.7f;
                _ribbonMesh.Add(_ribbon[j - 1].Pos, _ribbon[j].Pos, (2f + 7f * aq) * _s, (2f + 7f * a) * _s,
                    NeuroStyle.WithAlpha(tc, 0.8f * aq), NeuroStyle.WithAlpha(tc, 0.8f * a));
            }
            _ribbonMesh.Commit();

            // tramos acertados: del color del modo; con el acierto de la ronda brillan más y se ensanchan
            _fixedMesh.Clear();
            float winK = _phase == Phase.Idle || _phase == Phase.Input ? Mathf.Clamp01((now - _winAt) / 0.5f) : 0f;
            if (now - _winAt > 4f) winK = 0f;
            foreach (var l in _fixed)
            {
                float fade = Motion.Decorative ? 1f : Mathf.Clamp01((now - l.Born) / TrailFadeSeconds);
                _fixedMesh.Add(OrbPlay(l.A), OrbPlay(l.B), (3f + 3f * winK) * _s, NeuroStyle.WithAlpha(tc, (0.55f + 0.45f * winK) * fade));
            }
            _fixedMesh.Commit();

            // chispa
            if (_sparkOn)
            {
                _sparkHalo.rectTransform.anchoredPosition = _sparkPos;
                _sparkCore.rectTransform.anchoredPosition = _sparkPos;
            }

            // partículas, florecimientos y chispas que vuelan al contador (nada de esto existe con «quitar animaciones»)
            foreach (var p in _particles)
            {
                if (!p.Alive) continue;
                p.Age += dt;
                if (p.Age >= p.Life) { p.Alive = false; p.Img.gameObject.SetActive(false); continue; }
                p.Pos += p.Vel * dt;
                float k = p.Age / p.Life;
                float sz = p.Size * (1f - k * 0.5f) * _s;
                p.Img.rectTransform.anchoredPosition = p.Pos;
                p.Img.rectTransform.sizeDelta = new Vector2(sz, sz);
                p.Img.color = NeuroStyle.WithAlpha(p.Col, 0.9f * (1f - k));
            }
            foreach (var b in _blooms)
            {
                if (!b.Alive) continue;
                float k = (now - b.Born) / 0.65f;
                if (k >= 1f) { b.Alive = false; b.Img.gameObject.SetActive(false); continue; }
                float eased = Motion.Decorative ? 1f - Mathf.Pow(1f - k, 3f) : 0f;      // sin animaciones: un aro fijo que se apaga
                float d = (40f + 60f * eased) * _s;
                b.Img.rectTransform.anchoredPosition = b.Pos;
                b.Img.rectTransform.sizeDelta = new Vector2(d, d);
                b.Img.color = NeuroStyle.WithAlpha(b.Col, 0.8f * (1f - k));
            }
            Vector2 target = CounterPlay();
            foreach (var f in _flyers)
            {
                if (!f.Alive) continue;
                float k = (now - f.Start) / 0.7f;
                if (k < 0f) continue;
                if (k >= 1f) { f.Alive = false; f.Img.gameObject.SetActive(false); continue; }
                f.Img.gameObject.SetActive(true);
                float e = 1f - Mathf.Pow(1f - k, 3f);
                Vector2 pos = Vector2.Lerp(f.From, target, e) + new Vector2(0f, Mathf.Sin(k * Mathf.PI) * 60f * _s);
                f.Img.rectTransform.anchoredPosition = pos;
                f.Img.rectTransform.sizeDelta = new Vector2(24f * _s, 24f * _s);
                f.Img.color = NeuroStyle.WithAlpha(tc, 0.9f);
            }
        }

        // ------------------------------------------------------------------ efectos

        private void Emit(Vector2 at, int n, float spread, float life, Color col)
        {
            if (!Motion.Decorative) return;
            for (int i = 0; i < n; i++)
            {
                Particle p = null;
                foreach (var q in _particles) if (!q.Alive) { p = q; break; }
                if (p == null) return;
                p.Alive = true;
                p.Age = 0f;
                p.Life = life * (0.6f + (float)_rng.NextDouble() * 0.7f);
                p.Pos = at;
                p.Vel = new Vector2(((float)_rng.NextDouble() - 0.5f) * spread, ((float)_rng.NextDouble() - 0.5f) * spread) * _s;
                p.Size = 1.5f + (float)_rng.NextDouble() * 3.2f;
                p.Col = col;
                p.Img.gameObject.SetActive(true);
            }
        }

        private void AddBloom(Vector2 at, int hex)
        {
            Bloom b = null;
            foreach (var q in _blooms) if (!q.Alive) { b = q; break; }
            if (b == null) return;
            b.Alive = true;
            b.Pos = at;
            b.Born = GameClock.Time;
            b.Col = ColorOf(hex);
            b.Img.gameObject.SetActive(true);
        }

        private static Color ColorOf(int hex) => new Color(((hex >> 16) & 0xFF) / 255f, ((hex >> 8) & 0xFF) / 255f, (hex & 0xFF) / 255f, 1f);
        private static Color OrbColor(int orb) => ColorOf(RastroBoard.Colors[orb]);

        private void LaunchFlyer(Vector2 from, float start)
        {
            foreach (var f in _flyers)
            {
                if (f.Alive) continue;
                f.Alive = true;
                f.From = from;
                f.Start = start;
                return;
            }
        }

        private void PlayClip(AudioClip clip, float volume)
        {
            if (clip == null || !GameFeel.SoundOn) return;
            _audioSource.PlayOneShot(clip, volume);
        }

        private void ClearFx()
        {
            foreach (var p in _particles) { p.Alive = false; p.Img.gameObject.SetActive(false); }
            foreach (var b in _blooms) { b.Alive = false; b.Img.gameObject.SetActive(false); }
            foreach (var f in _flyers) { f.Alive = false; f.Img.gameObject.SetActive(false); }
            _trail.Clear(); _trailLinks.Clear(); _ribbon.Clear();
            _trailFade = 1f; _trailFading = false;
        }

        private void ClearRound()
        {
            _fixed.Clear();
            _input.Clear();
            _trail.Clear();
            _trailLinks.Clear();
            _ribbon.Clear();
            _sparkOn = false;
            if (_sparkHalo != null) { _sparkHalo.gameObject.SetActive(false); _sparkCore.gameObject.SetActive(false); }
            _winAt = -10f;
            _hintOrb = -1;
            if (_cueRoot != null) _cueRoot.gameObject.SetActive(false);
            foreach (var o in _orbs)
            {
                if (o == null) continue;
                o.Aro.gameObject.SetActive(false);
                o.X.gameObject.SetActive(false);
                o.Hint.gameObject.SetActive(false);
            }
            Rule("", "");
        }

        // ------------------------------------------------------------------ marcador y textos

        /// <summary>Rótulo de abajo y su línea de apoyo. Durante la ronda guiada los textos los dice Nubi (<see cref="GuidedTutorial.Say"/>).</summary>
        private void Rule(string rule, string sub)
        {
            bool practice = _tutorial != null && _tutorial.Practicing;
            _rule.text = practice ? "" : rule;
            _sub.text = practice ? "" : sub;
        }

        private void ResetHud(RastroMode mode, int asked)
        {
            _modeText.text = RastroModes.Name(mode);
            _modeIcon.sprite = RastroSprites.Icon(mode);
            _countText.text = asked > 0 ? RastroModes.CountLabel(mode, asked) : "";
            _countPill.gameObject.SetActive(asked > 0);
            LayoutPills();
            UpdateHud();
        }

        private void UpdateHud()
        {
            for (int i = 0; i < _lifeDots.Length; i++)
                _lifeDots[i].color = i < _session.Lives ? Cream : new Color(1f, 1f, 1f, 0.18f);
            _counterNum.text = _counter.ToString();
            bool precision = !_session.Timed && !_session.Assessment;
            _roundText.gameObject.SetActive(precision);
            if (precision) _roundText.text = $"Ronda {Mathf.Min(_session.RoundsPlayed + 1, RastroContract.PrecisionRounds)} de {RastroContract.PrecisionRounds}";
        }

        // ------------------------------------------------------------------ coordenadas

        private Vector2 LogicalToPlay(Vector2 l) => new Vector2((l.x - LogicalW * 0.5f) * _s, _playH * 0.5f - (l.y + _yShift) * _s);
        private Vector2 PlayToLogical(Vector2 u) => new Vector2(u.x / _s + LogicalW * 0.5f, (_playH * 0.5f - u.y) / _s - _yShift);
        private Vector2 OrbPlay(int i) => LogicalToPlay(RastroBoard.Position(i, _angle));
        private Vector2 CounterPlay() => new Vector2((LogicalW - 16f - 28f - LogicalW * 0.5f) * _s, _playH * 0.5f - 74f * _s);

        // ------------------------------------------------------------------ construcción de la UI

        protected override void BuildUi()
        {
            var canvasGo = new GameObject("RastroCanvas");
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
            WorldBackdrop.Build(bgRect, GameWorld.CieloDeCristal);
            // el fondo es el común (degradé + dos nebulosas más tenues que las de Cielo profundo); el tinte del modo va solo en los bordes (viñeta)
            var tintGo = new GameObject("ModeTint");
            tintGo.transform.SetParent(bgRect, false);
            Stretch(tintGo.AddComponent<RectTransform>());
            _tint = tintGo.AddComponent<Image>();
            _tint.sprite = RastroSprites.Vignette();
            _tint.raycastTarget = false;
            _tint.color = NeuroStyle.WithAlpha(SkyTint[0], TintAlpha);

            var safeGo = new GameObject("SafeAreaContent");
            safeGo.transform.SetParent(canvasGo.transform, false);
            _safe = safeGo.AddComponent<RectTransform>();
            ApplySafeArea(_safe);

            var playGo = new GameObject("Play");
            playGo.transform.SetParent(_safe, false);
            _play = playGo.AddComponent<RectTransform>();
            Stretch(_play);

            // disco punteado del tablero (gira con el cielo), tramos de luz, luceros, chispa y efectos
            _disc = Center(_play, "Disc", RastroSprites.DottedDisc(), 2f * RastroBoard.DiscRadius * UnitsPerDp);
            _disc.color = new Color(120f / 255f, 110f / 255f, 200f / 255f, 0.55f);
            _fixedMesh = Mesh(_play, "FixedMesh");
            _trailMesh = Mesh(_play, "TrailMesh");
            _ribbonMesh = Mesh(_play, "RibbonMesh");
            BuildOrbs();
            _sparkHalo = Center(_play, "SparkHalo", RadialGlowSprite.Get(), 52f * UnitsPerDp);
            _sparkCore = Center(_play, "SparkCore", DiscSprite.Get(), 18f * UnitsPerDp * 0.5f);
            _sparkCore.color = Color.white;
            _sparkHalo.gameObject.SetActive(false);
            _sparkCore.gameObject.SetActive(false);
            for (int i = 0; i < 70; i++) _particles.Add(new Particle { Img = Hidden(_play, "Particle", DiscSprite.Get()) });
            for (int i = 0; i < 10; i++) _blooms.Add(new Bloom { Img = Hidden(_play, "Bloom", RingSprite.Get()) });
            for (int i = 0; i < 12; i++) _flyers.Add(new Flyer { Img = Hidden(_play, "Flyer", RadialGlowSprite.Get()) });

            BuildHud();
            BuildTexts();
            BuildUnlock();
            BuildCue();
            BuildResultPanel();
            _exit = new ExitButton(_safe, this, UnitsPerDp);
            BuildTutorial(_safe, 112f * UnitsPerDp, "Rastro de luz", "Mira el camino de la chispa y repítelo con el dedo");

            var flashGo = new GameObject("Flash");
            flashGo.transform.SetParent(canvasGo.transform, false);
            Stretch(flashGo.AddComponent<RectTransform>());
            _flash = flashGo.AddComponent<Image>();
            _flash.raycastTarget = false;
            _flash.color = new Color(0f, 0f, 0f, 0f);

            _countdown = new CountdownScreen(canvasGo.transform, UnitsPerDp);
        }

        private void BuildOrbs()
        {
            for (int i = 0; i < RastroBoard.Orbs; i++)
            {
                var v = new OrbView { Phase = i * 1.3f };
                var root = new GameObject("Orb" + i);
                root.transform.SetParent(_play, false);
                v.Root = root.AddComponent<RectTransform>();
                v.Root.anchorMin = v.Root.anchorMax = v.Root.pivot = new Vector2(0.5f, 0.5f);
                v.Halo = Center(v.Root, "Halo", RadialGlowSprite.Get(), 100f);
                v.Body = Center(v.Root, "Body", RastroSprites.Orb(i), RastroBoard.OrbDiameter * UnitsPerDp / 0.94f);
                v.Aro = Center(v.Root, "Aro", RingSprite.Get(), (RastroBoard.OrbDiameter + 16f) * UnitsPerDp);
                v.Hint = Center(v.Root, "Hint", RastroSprites.DashedRing(), (RastroBoard.OrbDiameter + 22f) * UnitsPerDp);
                v.Hint.color = Sun;
                v.X = Center(v.Root, "X", RastroSprites.XMark(), 26f * UnitsPerDp);
                v.Aro.gameObject.SetActive(false);
                v.Hint.gameObject.SetActive(false);
                v.X.gameObject.SetActive(false);
                _orbs[i] = v;
            }
        }

        private void BuildHud()
        {
            var go = new GameObject("Hud");
            go.transform.SetParent(_safe, false);
            _hud = go.AddComponent<RectTransform>();
            Stretch(_hud);

            _modePill = Pill("ModePill", out _modeText, out _modeIcon, Sun, true);
            _countPill = Pill("CountPill", out _countText, out _, Rgb(127, 216, 255), false);
            _roundText = MakeText(_hud, "Round", 45, TextAnchor.MiddleLeft, Dim, 0f, 0f);
            _roundText.font = UiFonts.Regular;
            _roundText.horizontalOverflow = HorizontalWrapMode.Overflow;
            AnchorTopLeft(_roundText.rectTransform);

            for (int i = 0; i < _lifeDots.Length; i++)
            {
                var d = Hidden(_hud, "Life" + i, DiscSprite.Get());
                AnchorTopLeft(d.rectTransform);
                d.gameObject.SetActive(true);
                _lifeDots[i] = d;
            }
            _counterNum = MakeText(_hud, "Counter", 96, TextAnchor.MiddleRight, Color.white, 0f, 0f);
            NeuroStyle.ClayText(_counterNum, 3f, 5f);
            _counterNum.horizontalOverflow = HorizontalWrapMode.Overflow;
            AnchorTopLeft(_counterNum.rectTransform);
            _counterLabel = MakeText(_hud, "CounterLabel", 42, TextAnchor.MiddleRight, Dim, 0f, 0f);
            _counterLabel.font = UiFonts.Regular;
            _counterLabel.text = "luces recordadas";
            _counterLabel.horizontalOverflow = HorizontalWrapMode.Overflow;
            AnchorTopLeft(_counterLabel.rectTransform);

            // reloj del Reto: una línea fina arriba que se consume
            var tgo = new GameObject("Timer");
            tgo.transform.SetParent(_safe, false);
            _timerTrack = tgo.AddComponent<RectTransform>();
            _timerTrack.anchorMin = new Vector2(0f, 1f);
            _timerTrack.anchorMax = new Vector2(1f, 1f);
            _timerTrack.pivot = new Vector2(0.5f, 1f);
            _timerTrack.offsetMin = new Vector2(16f * UnitsPerDp, -6f * UnitsPerDp);
            _timerTrack.offsetMax = new Vector2(-16f * UnitsPerDp, -2f * UnitsPerDp);
            var track = tgo.AddComponent<Image>();
            track.sprite = RoundedRectSprite.Get(3);
            track.type = Image.Type.Sliced;
            track.color = new Color(1f, 1f, 1f, 0.12f);
            track.raycastTarget = false;
            var fgo = new GameObject("Fill");
            fgo.transform.SetParent(tgo.transform, false);
            _timerFill = fgo.AddComponent<RectTransform>();
            _timerFill.anchorMin = Vector2.zero;
            _timerFill.anchorMax = Vector2.one;
            _timerFill.offsetMin = _timerFill.offsetMax = Vector2.zero;
            var fill = fgo.AddComponent<Image>();
            fill.sprite = RoundedRectSprite.Get(3);
            fill.type = Image.Type.Sliced;
            fill.raycastTarget = false;
        }

        /// <summary>Píldora de arcilla del marcador: un ícono dibujado (opcional) y un texto de 15 sp.</summary>
        private RectTransform Pill(string name, out Text text, out Image icon, Color textColor, bool withIcon)
        {
            var go = new GameObject(name);
            go.transform.SetParent(_hud, false);
            var rect = go.AddComponent<RectTransform>();
            AnchorTopLeft(rect);
            var bg = go.AddComponent<Image>();
            bg.sprite = RoundedRectSprite.Get(64);
            bg.type = Image.Type.Sliced;
            bg.color = NeuroStyle.WithAlpha(NeuroStyle.Surface, 0.9f);
            bg.raycastTarget = false;
            NeuroStyle.ClayFrame(bg, 3f, 6f);
            icon = null;
            if (withIcon)
            {
                var ig = new GameObject("Icon");
                ig.transform.SetParent(rect, false);
                var ir = ig.AddComponent<RectTransform>();
                ir.anchorMin = ir.anchorMax = ir.pivot = new Vector2(0f, 0.5f);
                icon = ig.AddComponent<Image>();
                icon.raycastTarget = false;
                icon.color = textColor;
            }
            text = MakeText(rect, "Text", 45, TextAnchor.MiddleLeft, textColor, 0f, 0f);
            text.horizontalOverflow = HorizontalWrapMode.Overflow;
            var tr = text.rectTransform;
            tr.anchorMin = tr.anchorMax = new Vector2(0f, 0.5f);
            tr.pivot = new Vector2(0f, 0.5f);
            return rect;
        }

        private void BuildTexts()
        {
            _rule = MakeText(_play, "Rule", 60, TextAnchor.MiddleCenter, Color.white, 3f, 0.4f);
            NeuroStyle.ClayText(_rule, 3f, 4f);
            BestFit(_rule, 48);                       // se ajusta al ancho (ajuste de línea; si no cabe, achica hasta 16 sp): nunca se corta
            _sub = MakeText(_play, "Sub", 48, TextAnchor.MiddleCenter, Dim, 0f, 0f);
            _sub.font = UiFonts.Regular;
            BestFit(_sub, 45);
            foreach (var t in new[] { _rule, _sub })
            {
                var r = t.rectTransform;
                r.anchorMin = r.anchorMax = new Vector2(0.5f, 0.5f);
                r.pivot = new Vector2(0.5f, 0.5f);
            }
        }

        private void BuildUnlock()
        {
            var go = new GameObject("Unlock");
            go.transform.SetParent(_safe, false);
            _unlockRoot = go.AddComponent<RectTransform>();
            Stretch(_unlockRoot);
            _unlockGroup = go.AddComponent<CanvasGroup>();
            var dim = go.AddComponent<Image>();
            dim.color = new Color(0.008f, 0.012f, 0.06f, 0.82f);
            dim.raycastTarget = false;
            _unlockGlow = Center(_unlockRoot, "Glow", RadialGlowSprite.Get(), 260f * UnitsPerDp);
            _unlockGlow.rectTransform.anchoredPosition = new Vector2(0f, 40f * UnitsPerDp);
            _unlockNew = TextAt(_unlockRoot, "New", 66, Sun, 0f, 118f);
            _unlockNew.text = "¡NUEVO!";
            _unlockIcon = Center(_unlockRoot, "Icon", RastroSprites.Icon(RastroMode.Reves), 120f * UnitsPerDp);
            _unlockIcon.rectTransform.anchoredPosition = new Vector2(0f, 40f * UnitsPerDp);
            _unlockIcon.color = Color.white;
            _unlockName = TextAt(_unlockRoot, "Name", 96, Color.white, 0f, -50f);
            BestFit(_unlockName, 60);
            _unlockLine = TextAt(_unlockRoot, "Line", 57, Rgb(217, 212, 245), 0f, -100f);
            _unlockLine.font = UiFonts.Regular;
            _unlockLine.rectTransform.sizeDelta = new Vector2(900f, 150f);      // hasta 2 líneas
            BestFit(_unlockLine, 48);
            _unlockRoot.gameObject.SetActive(false);
        }

        /// <summary>El rótulo del modo que se queda arriba del tablero mientras se responde: ícono dibujado + texto grande del color del modo.</summary>
        private void BuildCue()
        {
            var go = new GameObject("ModeCue");
            go.transform.SetParent(_safe, false);
            _cueRoot = go.AddComponent<RectTransform>();
            _cueRoot.anchorMin = _cueRoot.anchorMax = new Vector2(0.5f, 1f);
            _cueRoot.pivot = new Vector2(0.5f, 1f);
            var ig = new GameObject("Icon");
            ig.transform.SetParent(_cueRoot, false);
            var ir = ig.AddComponent<RectTransform>();
            ir.anchorMin = ir.anchorMax = ir.pivot = new Vector2(0f, 0.5f);
            _cueIcon = ig.AddComponent<Image>();
            _cueIcon.raycastTarget = false;
            _cueText = MakeText(_cueRoot, "Text", 54, TextAnchor.MiddleCenter, Color.white, 3f, 0.4f);
            BestFit(_cueText, 54);                    // ≥ 18 sp: ajuste de línea (hasta 2 líneas); nunca se corta
            go.SetActive(false);
        }

        /// <summary>Muestra el recordatorio del modo (empieza la respuesta): del color del modo; el rastro simple, más discreto.</summary>
        private void ShowCue(RastroRound r)
        {
            Color c = r.Mode == RastroMode.Rastro ? Dim : SkyTint[(int)r.Mode];
            _cueIcon.sprite = RastroSprites.Icon(r.Mode);
            _cueIcon.color = c;
            _cueText.color = c;
            _cueText.text = RastroModes.CueText(r.Mode, r.Asked);
            _cueRoot.gameObject.SetActive(true);
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

        // ---- ayudas de construcción

        private static Image Center(Transform parent, string name, Sprite sprite, float sizeU)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var r = go.AddComponent<RectTransform>();
            r.anchorMin = r.anchorMax = r.pivot = new Vector2(0.5f, 0.5f);
            r.sizeDelta = new Vector2(sizeU, sizeU);
            var img = go.AddComponent<Image>();
            img.sprite = sprite;
            img.raycastTarget = false;
            return img;
        }

        private static Image Img(Transform parent, string name, Sprite sprite, Vector2 anchor, float sizeU)
        {
            var img = Center(parent, name, sprite, sizeU);
            img.rectTransform.anchorMin = img.rectTransform.anchorMax = anchor;
            return img;
        }

        private static Image Hidden(Transform parent, string name, Sprite sprite)
        {
            var img = Center(parent, name, sprite, 10f);
            img.gameObject.SetActive(false);
            return img;
        }

        private static LightMesh Mesh(Transform parent, string name)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var m = go.AddComponent<LightMesh>();
            Stretch(m.rectTransform);
            return m;
        }

        private static Text TextAt(RectTransform parent, string name, int fontPx, Color color, float x, float y)
        {
            var t = MakeText(parent, name, fontPx, TextAnchor.MiddleCenter, color, 0f, 0f);
            NeuroStyle.ClayText(t, 3f, 5f);
            var r = t.rectTransform;
            r.anchorMin = r.anchorMax = new Vector2(0.5f, 0.5f);
            r.sizeDelta = new Vector2(940f, fontPx * 1.5f);
            r.anchoredPosition = new Vector2(x, y * UnitsPerDp);
            t.horizontalOverflow = HorizontalWrapMode.Wrap;
            t.verticalOverflow = VerticalWrapMode.Overflow;
            return t;
        }

        private static void AnchorTopLeft(RectTransform r)
        {
            r.anchorMin = r.anchorMax = new Vector2(0f, 1f);
            r.pivot = new Vector2(0f, 1f);
        }

        // ------------------------------------------------------------------ disposición

        private void Layout()
        {
            Canvas.ForceUpdateCanvases();
            _playW = _play.rect.width;
            _playH = _play.rect.height;
            _s = _playW / LogicalW;
            _logicalH = _playH / _s;
            // en pantallas altas el tablero baja un poco (el aire sobra abajo); en las bajitas, sube un poco
            _yShift = (_logicalH - 640f) * 0.22f;

            float diameter = _senior ? RastroBoard.OrbDiameterSenior : RastroBoard.OrbDiameter;
            foreach (var o in _orbs)
            {
                o.Body.rectTransform.sizeDelta = new Vector2(diameter * _s / 0.94f, diameter * _s / 0.94f);
                float ring = (diameter + 16f) * _s;
                o.Aro.rectTransform.sizeDelta = new Vector2(ring, ring);
                float hint = (diameter + 22f) * _s;
                o.Hint.rectTransform.sizeDelta = new Vector2(hint, hint);
                float x = 26f * _s;
                o.X.rectTransform.sizeDelta = new Vector2(x, x);
            }
            _disc.rectTransform.anchoredPosition = LogicalToPlay(RastroBoard.Center);
            _disc.rectTransform.sizeDelta = Vector2.one * (2f * RastroBoard.DiscRadius * _s);
            _sparkHalo.rectTransform.sizeDelta = Vector2.one * (52f * _s);
            _sparkCore.rectTransform.sizeDelta = Vector2.one * (9f * _s);

            // rótulos de abajo: instrucción (20 sp) y línea de apoyo (15 sp)
            _rule.rectTransform.sizeDelta = new Vector2(340f * _s, 58f * _s);
            _rule.rectTransform.anchoredPosition = LogicalToPlay(new Vector2(180f, 562f));
            _sub.rectTransform.sizeDelta = new Vector2(340f * _s, 30f * _s);
            _sub.rectTransform.anchoredPosition = LogicalToPlay(new Vector2(180f, 606f));
            _rule.rectTransform.localScale = Vector3.one;

            _cueRoot.sizeDelta = new Vector2(344f * _s, 50f * _s);
            _cueRoot.anchoredPosition = new Vector2(0f, -116f * _s);
            _cueIcon.rectTransform.sizeDelta = new Vector2(30f * _s, 30f * _s);
            _cueIcon.rectTransform.anchoredPosition = new Vector2(4f * _s, 0f);
            _cueText.rectTransform.offsetMin = new Vector2(40f * _s, 0f);
            _cueText.rectTransform.offsetMax = new Vector2(-4f * _s, 0f);
            LayoutHud();
            LayoutPills();
            _unlockGlow.rectTransform.sizeDelta = Vector2.one * (260f * _s);
        }

        private void LayoutHud()
        {
            float m = 16f;
            float dot = 12f, gap = 18f;
            for (int i = 0; i < _lifeDots.Length; i++)
            {
                var r = _lifeDots[i].rectTransform;
                r.sizeDelta = new Vector2(dot * _s, dot * _s);
                r.anchoredPosition = new Vector2((LogicalW - m - dot - (_lifeDots.Length - 1 - i) * gap) * _s, -(28f - dot * 0.5f) * _s);
            }
            _counterNum.rectTransform.sizeDelta = new Vector2(120f * _s, 40f * _s);
            _counterNum.rectTransform.anchoredPosition = new Vector2((LogicalW - m - 120f) * _s, -50f * _s);
            _counterLabel.rectTransform.sizeDelta = new Vector2(150f * _s, 20f * _s);
            _counterLabel.rectTransform.anchoredPosition = new Vector2((LogicalW - m - 150f) * _s, -90f * _s);
        }

        /// <summary>Ajusta el ancho de cada píldora a su texto y las pone en sus dos filas.</summary>
        private void LayoutPills()
        {
            float m = 16f, h = 36f, pad = 12f, iconD = 22f, iconGap = 6f;
            // fila 1: ícono + modo
            float tw = _modeText.preferredWidth / _s;
            _modeIcon.rectTransform.sizeDelta = new Vector2(iconD * _s, iconD * _s);
            _modeIcon.rectTransform.anchoredPosition = new Vector2(pad * _s, 0f);
            _modeText.rectTransform.sizeDelta = new Vector2((tw + 4f) * _s, h * _s);
            _modeText.rectTransform.anchoredPosition = new Vector2((pad + iconD + iconGap) * _s, 0f);
            _modePill.sizeDelta = new Vector2((pad * 2f + iconD + iconGap + tw) * _s, h * _s);
            _modePill.anchoredPosition = new Vector2(m * _s, -10f * _s);
            // fila 2: cuántas luces
            float cw = _countText.preferredWidth / _s;
            _countText.rectTransform.sizeDelta = new Vector2((cw + 4f) * _s, h * _s);
            _countText.rectTransform.anchoredPosition = new Vector2(pad * _s, 0f);
            _countPill.sizeDelta = new Vector2((pad * 2f + cw) * _s, h * _s);
            _countPill.anchoredPosition = new Vector2(m * _s, -52f * _s);
            // la ronda de Precisión, a la derecha de la píldora
            _roundText.rectTransform.sizeDelta = new Vector2(110f * _s, h * _s);
            _roundText.rectTransform.anchoredPosition = new Vector2((m + pad * 2f + cw + 8f) * _s, -52f * _s);
            if (!_countPill.gameObject.activeSelf) _roundText.rectTransform.anchoredPosition = new Vector2(m * _s, -52f * _s);
        }
    }
}
