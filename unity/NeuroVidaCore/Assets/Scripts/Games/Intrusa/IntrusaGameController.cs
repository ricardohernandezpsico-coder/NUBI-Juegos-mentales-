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
using Motion = NeuroVida.Games.Shared.Motion; // UnityEngine.Motion también existe

namespace NeuroVida.Games.Intrusa
{
    /// <summary>
    /// "La estrella intrusa" (Atlas celeste): juego estrella de Lenguaje (organizar significados y resistir asociaciones engañosas;
    /// ver <see cref="IntrusaContract"/> y docs/diseno-estrella-intrusa.md). Cinco estrellas-palabra sobre un cielo quieto: una no
    /// pertenece. NO hay líneas antes de responder (la figura no regala nada); las estrellas menores de la figura se ven tenues.
    /// <list type="bullet">
    /// <item>Acierto: la intrusa cae como estrella fugaz (900 ms); desde el nivel 3, «¿Qué las une?» (3 opciones, 4 s; mayores 6 s) con
    /// las cuatro estrellas latiendo; después una chispa traza la figura de la regla (una nota de la pentatónica en cada estrella, un
    /// acorde al cerrar) y aparece el grabado con el nombre de la figura y la regla. En los niveles 1-2 no hay bonus y la chispa sale ya.</item>
    /// <item>Error: la estrella tocada con aro coral y una X dibujada, la intrusa con aro sol que late, la explicación («Todos viven en el
    /// agua; el loro no.»), si era trampa una línea punteada «va con perro, pero no es un animal»; con «Seguir» la intrusa cae y la
    /// figura se traza al doble de velocidad, sin bonus ni notas fuertes. La regla queda «por repasar» y no suma lámina.</item>
    /// <item>Cada ronda la figura se refleja al azar y se gira ±12°, y la intrusa ocupa uno de los huecos: no se puede memorizar.</item>
    /// </list>
    /// Con "quitar animaciones": las líneas aparecen con un fundido de 300 ms, sin partículas ni recorrido de caída, y el grabado de una
    /// vez. Respeta sonido y vibración apagados (vibración solo al acertar y al completar la figura).
    /// Reto = 2 minutos (una cometa cruza arriba); Precisión = 20 rondas sin tiempo.
    /// </summary>
    public class IntrusaGameController : GameControllerBase
    {
        public const string GameId = IntrusaContract.GameId;
        private const string RecentKey = "intrusa_recent";
        private const string ReviewKey = "intrusa_review";
        private const string PlatesKey = "intrusa_plates";

        private const float UnitsPerDp = 3f;
        private const float MarginU = 60f;
        private const int LabelSp = 17;

        private static readonly Color LineGold = new Color(255f / 255f, 217f / 255f, 138f / 255f, 1f);
        private static readonly Color SparkCore = new Color(1f, 0.973f, 0.902f, 1f);
        private static readonly Color StarTint = new Color(1f, 0.925f, 0.784f, 1f);
        private static readonly Color PlateColor = new Color(1f, 0.973f, 0.925f, 1f);
        private static readonly Color NameColor = new Color(1f, 0.89f, 0.64f, 1f);          // #FFE3A3
        private static readonly Color RuleColor = new Color(0.851f, 0.831f, 0.961f, 1f);    // #D9D4F5
        private static readonly Color NamedColor = new Color(1f, 0.788f, 0.29f, 1f);        // #FFC94A
        private static readonly Color GoodColor = NeuroStyle.Lime;
        private static readonly Color BadColor = NeuroStyle.Coral;
        private static readonly Color AmberColor = NeuroStyle.Sun;

        private static readonly float[] LineWidth = { 9f, 4f, 1.5f };
        private static readonly float[] LineAlpha = { 0.10f, 0.25f, 0.95f };

        private enum Phase { Idle, Playing, Done }

        // ------------------------------------------------------------------ estado

        private System.Random _rng;
        private AdaptiveDifficulty _dda;
        private IntrusaBank _bank;
        private IntrusaDirector _director;
        private IntrusaTally _tally;
        private Phase _phase = Phase.Idle;
        private bool Endless => _config != null && _config.config.timed;
        private bool Precision => !Endless;
        private bool Senior;
        private int _previousFrameRate;
        private float _endsAt;
        private int _lastTickSecond = -1;
        private int _streak, _points, _resolved;
        private int _today;
        private bool _clearShown;
        private Dictionary<string, int> _review;
        private HashSet<string> _plates;

        // ronda
        private IntrusaSpec _spec, _nextSpec;
        private Engraving _engraving;
        private Vector2? _press;                 // último toque (coordenadas lógicas), lo consume quien lo espera
        private bool _roundLive;
        private float _legibleAt;
        private int _revealStep;

        // escala y disposición
        private float _s = 3f, _playW, _playH, _yShift, _logicalH;
        private bool _stacked;

        // UI
        private RectTransform _safe, _play, _fig, _fxLayer, _textLayer, _timerTrack, _comet;
        private Image _cometGlow, _cometTail, _clearGlow, _dimFade;
        private Text _measure, _name, _rule, _named, _explain, _trapText, _reviewTag, _hint, _seguirLabel;
        private RectTransform _seguir, _optionsBar, _optionsFill;
        private Image _seguirImg;
        private readonly Slot[] _slots = new Slot[5];
        private readonly List<LineViz> _lines = new List<LineViz>();
        private readonly List<Image> _minor = new List<Image>();
        private readonly List<Image> _dots = new List<Image>();
        private readonly List<OptionButton> _options = new List<OptionButton>();
        private Image _headHalo, _headCore, _outlineImg, _shadeImg;
        private readonly List<Particle> _particles = new List<Particle>();
        private readonly List<RingFx> _rings = new List<RingFx>();
        private readonly List<Image> _trail = new List<Image>();
        private Toast _toast;
        private ExitButton _exit;
        private GameHud _hud;
        private CountdownScreen _countdown;
        private bool _pulsing;
        private readonly Dictionary<string, float> _widthCache = new Dictionary<string, float>();

        private sealed class Slot
        {
            public RectTransform Root, Plate;
            public Image Halo, Core, Flare, Aro, XMark, PlateBg;
            public Text Label;
            public CanvasGroup Group, PlateGroup;
            public Vector2 PosU, BaseSizeU;
            public LRect Plaque, Touch;
            public string Word;
            public bool Intruder;
            public float PulsePhase;
        }

        private sealed class LineViz { public Image[] Img = new Image[3]; public Vector2 A, B; }
        private sealed class Particle { public Image Img; public Vector2 Pos, Vel; public float Age, Life; public bool Alive; }
        private sealed class RingFx { public Image Img; public Vector2 Pos; public float Age, Dur, Size; public bool Alive; }
        private sealed class OptionButton { public RectTransform Rect; public Image Bg, Mark; public Text Label; public Vector2 CenterU, SizeU; }

        // ------------------------------------------------------------------ sesión

        public void StartSession(SequenceInitConfig config)
        {
            _config = config;
            _rng = new System.Random();
            var age = DdaUserProfileConfig.ParseAgeBand(config.config.age_band);
            Senior = age == AgeBand.Senior;
            float start = AdaptiveDifficulty.StartRating(config.config, IntrusaContract.MaxLevel);
            // ~10 rondas en el Reto: pasos grandes (como Satélites y Cosecha). Sin tiempo de reacción: el tiempo no es la medida.
            _dda = new AdaptiveDifficulty(IntrusaContract.MaxLevel, age, start, stepUp: 0.4f, useReaction: false);
            _bank = IntrusaBank.Load();
            _today = IntrusaContract.DayNumber(System.DateTime.Now);
            string recent = "", review = "", plates = "";
            try { recent = PlayerPrefs.GetString(RecentKey, ""); review = PlayerPrefs.GetString(ReviewKey, ""); plates = PlayerPrefs.GetString(PlatesKey, ""); }
            catch (System.Exception) { }
            _review = IntrusaContract.ParseReview(review);
            _plates = IntrusaContract.ParsePlates(plates);
            _director = new IntrusaDirector(_bank, _rng, IntrusaContract.RecentIds(recent), IntrusaContract.ReviewDue(_review, _today));
            _tally = new IntrusaTally();

            _previousFrameRate = Application.targetFrameRate;
            Application.targetFrameRate = 60;

            _phase = Phase.Idle;
            _spec = _nextSpec = null;
            _press = null;
            _roundLive = false;
            _streak = _points = _resolved = 0;
            _endsAt = 0f;
            _lastTickSecond = -1;
            _clearShown = false;
            _pulsing = false;
            _loopOn = _guided = false;

            _resultRoot.gameObject.SetActive(false);
            _exit.Hide();
            _timerTrack.gameObject.SetActive(Endless);
            _hud.SetStreak(0);
            HideRound();
            _hint.gameObject.SetActive(false);
            _clearGlow.color = NeuroStyle.WithAlpha(NeuroStyle.Grape, 0f);

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
            _baked = false;
            StartCoroutine(PrewarmSprites());
            if (TutorialWanted)
            {
                // la ronda guiada se juega sobre el cielo ya armado, antes de la cuenta regresiva
                _safe.gameObject.SetActive(true);
                yield return null;
                ApplySafeArea(_safe);
                Canvas.ForceUpdateCanvases();
                Layout();
                UpdateHud();
                yield return StartCoroutine(RunTutorialIfNeeded());
                HideRound();
                _safe.gameObject.SetActive(false);
            }
            yield return StartCoroutine(_countdown.Play("La estrella intrusa", Assessment.Subtitle("Toca la que no pertenece"), () => _safe.gameObject.SetActive(true)));
            _safe.gameObject.SetActive(true);
            yield return null;
            ApplySafeArea(_safe);
            Canvas.ForceUpdateCanvases();
            Layout();
            UpdateHud();

            _endsAt = GameClock.Time + IntrusaContract.RetoSeconds;
            _phase = Phase.Playing;
            _hint.gameObject.SetActive(true);
            _hint.text = "Una estrella no pertenece.\nToca la intrusa";
            _nextSpec = PrepareRound();
            _loopOn = true;
            yield return StartCoroutine(MainLoop());
        }

        /// <summary>Una ronda tras otra hasta que se acabe el tiempo (Reto) o las 20 (Precisión), y el final. «Cómo se juega» la retoma desde aquí.</summary>
        private IEnumerator MainLoop()
        {
            while (!Finished())
            {
                var spec = _nextSpec ?? PrepareRound();
                _nextSpec = null;
                if (spec == null) break;
                yield return StartCoroutine(PlayRound(spec));
                if (!Finished()) _nextSpec = PrepareRound();
            }
            yield return StartCoroutine(FinishGame());
        }

        private bool Finished()
        {
            if (Endless) return GameClock.Time >= _endsAt;
            return _resolved >= IntrusaContract.PrecisionRounds;
        }

        /// <summary>Hornea los sprites y sintetiza los sonidos durante la cuenta regresiva (así la primera ronda no traba).</summary>
        private IEnumerator PrewarmSprites()
        {
            IntrusaSprites.Line();
            IntrusaSprites.Flare();
            yield return null;
            IntrusaSounds.Note(0, true);
            IntrusaSounds.Fall();
            IntrusaSounds.Chord();
            yield return null;
            for (int i = 1; i < 6; i++) IntrusaSounds.Note(i, i % 2 == 0);
            IntrusaSounds.Glass();
            yield return null;
            IntrusaSounds.Engrave();
            IntrusaSounds.Named();
            IntrusaSounds.Pulse();
            IntrusaSounds.Dull();
            _baked = true;
        }

        // ------------------------------------------------------------------ preparar la ronda

        private IntrusaSpec PrepareRound()
        {
            var next = _director.Next(_dda.PresentedLevel);
            if (!next.HasValue) return null;
            var g = next.Value.group;
            var shape = _bank.FigureOf(g.k);
            if (shape == null) return null;
            return IntrusaLayout.Arrange(g, shape, _rng, TextWidthLogical, next.Value.review);
        }

        /// <summary>Ancho de la palabra en dp lógicos (Atkinson Bold 17 sp), medido con el texto invisible del juego.</summary>
        private float TextWidthLogical(string word)
        {
            if (_widthCache.TryGetValue(word, out float w)) return w;
            _measure.fontSize = Mathf.RoundToInt(LabelSp * _s);
            _measure.horizontalOverflow = HorizontalWrapMode.Overflow;
            _measure.text = word;
            w = _measure.preferredWidth / _s;
            _widthCache[word] = w;
            return w;
        }

        // ------------------------------------------------------------------ una ronda

        private IEnumerator PlayRound(IntrusaSpec spec)
        {
            _spec = spec;
            var g = spec.G;
            _press = null;
            ShowRound(spec);
            yield return StartCoroutine(AppearStars());

            // esperar el toque: no hay reloj por ronda (el tiempo de la partida es la cometa)
            _legibleAt = GameClock.Time;
            _roundLive = true;
            int choice = -1;
            while (choice < 0)
            {
                if (Endless && Finished()) { _roundLive = false; yield break; }
                if (_press.HasValue)
                {
                    var p = _press.Value;
                    _press = null;
                    choice = PickSlot(p);
                }
                yield return null;
            }
            _roundLive = false;
            GameFeel.Haptic(GameFeel.HapticKind.Light);
            int ms = Mathf.RoundToInt((GameClock.Time - _legibleAt) * 1000f);
            if (_hint.gameObject.activeSelf) _hint.gameObject.SetActive(false);
            _resolved++;
            bool correct = _slots[choice].Intruder;
            _tally.Add(g, correct, correct ? ms : -1);
            Register(correct);
            _hud.SetLevel(_dda.PresentedLevel);

            if (correct) yield return StartCoroutine(CorrectFlow(spec, choice));
            else yield return StartCoroutine(WrongFlow(spec, choice));
            UpdateHud();
            yield return StartCoroutine(FadeOutRound());
        }

        private void Register(bool correct)
        {
            var change = _dda.Register(correct);
            if (change == DdaChange.Up) _toast.Show("¡Subes de nivel!", IntrusaContract.LevelNews(_dda.Level), GoodColor, 1.1f);
            else if (change == DdaChange.Down || _dda.Struggling) _toast.Show("Con calma", "Piensa qué tienen en común las otras", AmberColor, 0.9f);
        }

        private int PickSlot(Vector2 logical)
        {
            var areas = new List<LRect>(5);
            for (int i = 0; i < 5; i++) areas.Add(_slots[i].Touch);
            return IntrusaLayout.Pick(logical.x, logical.y, areas);
        }

        // ------------------------------------------------------------------ acierto

        private IEnumerator CorrectFlow(IntrusaSpec spec, int choice)
        {
            var g = spec.G;
            int level = _dda.PresentedLevel;
            _streak++;
            _hud.SetStreak(_streak);
            bool hasBonus = IntrusaContract.HasBonus(level);
            bool named = false;
            Slot intruder = _slots[4];

            if (!hasBonus)
            {
                // niveles 1-2: la intrusa cae y la chispa sale ya
                StartCoroutine(FallStar(intruder));
                yield return StartCoroutine(Trace(spec, 1f, false));
            }
            else
            {
                yield return StartCoroutine(FallStar(intruder));
                named = false;
                var bonus = new BonusResult();
                yield return StartCoroutine(Bonus(spec, bonus));
                named = bonus.Named;
                if (bonus.Played) _tally.AddBonus(named, g.k);
                yield return StartCoroutine(Trace(spec, 1f, false));
            }

            int pts = IntrusaContract.Points(level, _streak, named);
            _points += pts;
            UpdateHud();
            bool newPlate = _plates.Add(g.k);
            if (newPlate) _tally.NewPlates.Add(g.k);
            if (_review.Remove(g.k)) { _tally.Mastered.Add(g.k); }
            StartCoroutine(FloatText(Center(), "+" + pts, NeuroStyle.Sun));
            CheckClearSky();
            yield return StartCoroutine(Reveal(spec, named, false, newPlate));
            // un toque (o la pausa) cierra la ronda
            float hold = Senior ? 1.1f : 0.7f;
            float t = 0f;
            _press = null;
            while (t < hold)
            {
                t += GameClock.DeltaTime;
                if (_press.HasValue && t > 0.2f) break;
                yield return null;
            }
            _press = null;
        }

        private sealed class BonusResult { public bool Played, Named; }

        /// <summary>«¿Qué las une?»: tres opciones, 4 s (mayores 6 s); las cuatro estrellas laten suave. Ignorarlo no castiga.</summary>
        private IEnumerator Bonus(IntrusaSpec spec, BonusResult result)
        {
            var g = spec.G;
            float window = IntrusaContract.BonusWindow(Senior);
            _pulsing = true;
            PlayClip(IntrusaSounds.Pulse(), 0.5f);
            for (int i = 0; i < 3; i++)
            {
                var o = _options[i];
                o.Label.text = g.o[i];
                o.Bg.color = PlateColor;
                o.Mark.gameObject.SetActive(false);
                o.Rect.gameObject.SetActive(true);
                o.Rect.localScale = Vector3.one * (Motion.Decorative ? 0.9f : 1f);
            }
            _optionsBar.gameObject.SetActive(true);
            _name.text = "";
            _hint.gameObject.SetActive(false);
            _press = null;
            float t = 0f;
            int chosen = -1;
            while (t < window && chosen < 0)
            {
                t += GameClock.DeltaTime;
                float pop = Mathf.Clamp01(t / 0.2f);
                for (int i = 0; i < 3; i++) _options[i].Rect.localScale = Vector3.one * (Motion.Decorative ? Mathf.Lerp(0.9f, 1f, UiFx.EaseOutBack(pop)) : 1f); // sin ReduceMotion: aparecen quietas
                SetBar(1f - t / window);
                if (_press.HasValue)
                {
                    var p = _press.Value; _press = null;
                    chosen = PickOption(p);
                }
                yield return null;
            }
            _pulsing = false;
            ResetPulse();
            if (chosen >= 0)
            {
                result.Played = true;
                result.Named = chosen == g.c;
                var ob = _options[chosen];
                ob.Bg.color = result.Named ? GoodColor : BadColor;
                ob.Mark.sprite = result.Named ? AnswerMarkSprite.Check() : AnswerMarkSprite.Cross();
                ob.Mark.gameObject.SetActive(true);
                if (!result.Named) { var ok = _options[g.c]; ok.Bg.color = GoodColor; PlayClip(IntrusaSounds.Dull(), 0.5f); }
                else PlayClip(IntrusaSounds.Named(), 0.6f);
                StartCoroutine(PopRect(ob.Rect, 1.08f, 0.2f));
                float e = 0f;
                while (e < 0.55f) { e += GameClock.DeltaTime; yield return null; }
            }
            for (int i = 0; i < 3; i++) _options[i].Rect.gameObject.SetActive(false);
            _optionsBar.gameObject.SetActive(false);
        }

        private int PickOption(Vector2 logical)
        {
            // las opciones se miden en unidades del lienzo: se convierte el toque lógico de vuelta
            Vector2 u = LogicalToPlay(logical);
            int best = -1;
            float bestD = float.MaxValue;
            for (int i = 0; i < 3; i++)
            {
                var o = _options[i];
                if (Mathf.Abs(u.x - o.CenterU.x) > o.SizeU.x * 0.5f || Mathf.Abs(u.y - o.CenterU.y) > o.SizeU.y * 0.5f) continue;
                float d = (u - o.CenterU).sqrMagnitude;
                if (d < bestD) { bestD = d; best = i; }
            }
            return best;
        }

        private void SetBar(float fraction)
        {
            _optionsFill.anchorMax = new Vector2(Mathf.Clamp01(fraction), 1f);
            _optionsFill.offsetMin = _optionsFill.offsetMax = Vector2.zero;
            _optionsFill.GetComponent<Image>().color = fraction > 0.3f ? NeuroStyle.Sky : AmberColor;
        }

        // ------------------------------------------------------------------ error

        private IEnumerator WrongFlow(IntrusaSpec spec, int choice)
        {
            var g = spec.G;
            _streak = 0;
            _hud.SetStreak(0);
            CheckClearSky();
            PlayClip(IntrusaSounds.Glass(), 0.5f);
            Slot wrong = _slots[choice], intruder = _slots[4];

            // la tocada: aro coral y una X dibujada (nunca solo color); la verdadera intrusa: aro sol que late
            wrong.Aro.color = NeuroStyle.WithAlpha(BadColor, 0.95f);
            wrong.Aro.gameObject.SetActive(true);
            wrong.XMark.gameObject.SetActive(true);
            StartCoroutine(PopIn(wrong.XMark.rectTransform, 0.2f));
            intruder.Aro.color = NeuroStyle.WithAlpha(AmberColor, 0.95f);
            intruder.Aro.gameObject.SetActive(true);
            var pulse = StartCoroutine(PulseAro(intruder));

            // explicación («Todos viven en el agua; el loro no.») y, si es trampa, la línea punteada
            _explain.text = g.e;
            _explain.gameObject.SetActive(true);
            _explain.canvasRenderer.SetAlpha(0f);
            if (g.IsTrap && !string.IsNullOrEmpty(g.a)) ShowTrapLine(g);
            _seguir.gameObject.SetActive(true);
            _seguirImg.color = PlateColor;
            _press = null;
            float t = 0f;
            const float fade = 0.3f;
            bool go = false;
            while (!go)
            {
                t += GameClock.DeltaTime;
                float k = Mathf.Clamp01(t / fade);
                _explain.canvasRenderer.SetAlpha(k);
                if (_trapText.gameObject.activeSelf) _trapText.canvasRenderer.SetAlpha(k);
                if (_press.HasValue)
                {
                    var p = _press.Value; _press = null;
                    Vector2 u = LogicalToPlay(p);
                    Vector2 sc = _seguir.anchoredPosition, ss = _seguir.sizeDelta;
                    if (Mathf.Abs(u.x - sc.x) <= ss.x * 0.5f && Mathf.Abs(u.y - sc.y) <= ss.y * 0.5f) go = true;
                }
                if (Endless && Finished() && t > 1f) go = true;     // se acabó el tiempo mientras leía: no se queda esperando
                yield return null;
            }
            StopCoroutine(pulse);
            _seguir.gameObject.SetActive(false);
            _explain.gameObject.SetActive(false);
            HideTrapLine();
            wrong.Aro.gameObject.SetActive(false);
            wrong.XMark.gameObject.SetActive(false);
            intruder.Aro.gameObject.SetActive(false);

            // la regla queda por repasar (no suma lámina)
            if (!_review.ContainsKey(g.k)) { _review[g.k] = _today; _tally.NewReview.Add(g.k); }

            // con «Seguir»: la intrusa cae y la figura se traza al doble de velocidad, sin bonus ni notas fuertes
            StartCoroutine(FallStar(intruder));
            yield return StartCoroutine(Trace(spec, 2f, true));
            yield return StartCoroutine(Reveal(spec, false, true, false));
            float hold = 0.5f;
            float e = 0f;
            while (e < hold) { e += GameClock.DeltaTime; yield return null; }
        }

        private IEnumerator PulseAro(Slot s)
        {
            float t = 0f;
            while (true)
            {
                t += GameClock.DeltaTime;
                float k = GameFeel.ReduceMotion ? 1f : 1f + 0.12f * Mathf.Sin(t * 5f);
                s.Aro.rectTransform.localScale = Vector3.one * k;
                yield return null;
            }
        }

        private void ShowTrapLine(IntrusaGroup g)
        {
            int idx = -1;
            for (int i = 0; i < 4; i++) if (_slots[i].Word == g.a) { idx = i; break; }
            if (idx < 0) return;
            Vector2 a = _slots[4].PosU, b = _slots[idx].PosU;
            float dist = Vector2.Distance(a, b);
            int n = Mathf.Clamp(Mathf.RoundToInt(dist / (16f * _s / 3f)), 3, _dots.Count);
            for (int i = 0; i < _dots.Count; i++)
            {
                bool on = i < n;
                _dots[i].gameObject.SetActive(on);
                if (!on) continue;
                float k = (i + 0.5f) / n;
                _dots[i].rectTransform.anchoredPosition = Vector2.Lerp(a, b, k);
                _dots[i].rectTransform.sizeDelta = new Vector2(7f * _s, 7f * _s);
                _dots[i].color = NeuroStyle.WithAlpha(AmberColor, 0.95f);
            }
            // el rótulo («va con perro, pero no es un animal») justo debajo de la explicación
            _trapText.text = g.x + " " + g.r;
            _trapText.gameObject.SetActive(true);
            _trapText.canvasRenderer.SetAlpha(0f);
        }

        private void HideTrapLine()
        {
            foreach (var d in _dots) d.gameObject.SetActive(false);
            _trapText.gameObject.SetActive(false);
        }

        // ------------------------------------------------------------------ mostrar la ronda

        private void HideRound()
        {
            foreach (var s in _slots)
            {
                if (s == null) continue;
                s.Root.gameObject.SetActive(false);
                s.Plate.gameObject.SetActive(false);
                s.Aro.gameObject.SetActive(false);
                s.XMark.gameObject.SetActive(false);
            }
            foreach (var l in _lines) foreach (var im in l.Img) im.gameObject.SetActive(false);
            foreach (var m in _minor) m.gameObject.SetActive(false);
            foreach (var d in _dots) d.gameObject.SetActive(false);
            foreach (var o in _options) o.Rect.gameObject.SetActive(false);
            _optionsBar.gameObject.SetActive(false);
            _outlineImg.gameObject.SetActive(false);
            _shadeImg.gameObject.SetActive(false);
            _headHalo.gameObject.SetActive(false);
            _headCore.gameObject.SetActive(false);
            _name.gameObject.SetActive(false);
            _rule.gameObject.SetActive(false);
            _named.gameObject.SetActive(false);
            _explain.gameObject.SetActive(false);
            _trapText.gameObject.SetActive(false);
            _reviewTag.gameObject.SetActive(false);
            _seguir.gameObject.SetActive(false);
            foreach (var t in _trail) t.gameObject.SetActive(false);
            foreach (var p in _particles) { p.Alive = false; p.Img.gameObject.SetActive(false); }
            foreach (var r in _rings) { r.Alive = false; r.Img.gameObject.SetActive(false); }
            if (_engraving != null) { _engraving.Release(); _engraving = null; }
            _pulsing = false;
        }

        private Vector2 LogicalToPlay(Vector2 l) => new Vector2((l.x - IntrusaLayout.ScreenW * 0.5f) * _s, _playH * 0.5f - (l.y + _yShift) * _s);
        private Vector2 LogicalToPlay(Vec2 l) => LogicalToPlay(new Vector2(l.X, l.Y));
        private Vector2 PlayToLogical(Vector2 u) => new Vector2(u.x / _s + IntrusaLayout.ScreenW * 0.5f, (_playH * 0.5f - u.y) / _s - _yShift);
        private Vector2 Center() => LogicalToPlay(new Vector2(IntrusaLayout.BoxX + IntrusaLayout.BoxW * 0.5f, IntrusaLayout.BoxY + IntrusaLayout.BoxH * 0.5f));

        private void ShowRound(IntrusaSpec spec)
        {
            HideRound();
            var f = spec.Figure;
            var g = spec.G;

            // las 5 estrellas con palabra: 4 en las anclas (en el reparto elegido) y la intrusa en su hueco
            for (int i = 0; i < 5; i++)
            {
                var s = _slots[i];
                bool intruder = i == 4;
                Vec2 pt = intruder ? f.Hole(spec.Hole) : f.Point(f.anclas[i]);
                Vec2 sp = IntrusaLayout.ToScreen(pt, spec.Mirror, spec.RotationDeg);
                s.Word = intruder ? g.x : spec.AnchorWords[i];
                s.Intruder = intruder;
                s.Label.text = s.Word;
                s.Label.fontSize = Mathf.RoundToInt(LabelSp * _s);
                s.Plaque = IntrusaLayout.Plaque(sp, TextWidthLogical(s.Word));
                s.Touch = IntrusaLayout.TouchArea(sp, s.Plaque);
                s.PosU = LogicalToPlay(sp);
                s.Root.anchoredPosition = s.PosU;
                s.Root.localScale = Vector3.one;
                s.Root.localRotation = Quaternion.identity;
                s.Group.alpha = 0f;
                s.PlateGroup.alpha = 0f;
                s.Plate.sizeDelta = new Vector2(s.Plaque.W * _s, s.Plaque.H * _s);
                s.Plate.anchoredPosition = LogicalToPlay(new Vector2(s.Plaque.CenterX, s.Plaque.CenterY));
                s.Halo.rectTransform.sizeDelta = new Vector2(44f * _s, 44f * _s);
                s.Core.rectTransform.sizeDelta = new Vector2(7f * _s, 7f * _s);
                s.Flare.rectTransform.sizeDelta = new Vector2(24f * _s, 24f * _s);
                s.Aro.rectTransform.sizeDelta = new Vector2(34f * _s, 34f * _s);
                s.XMark.rectTransform.sizeDelta = new Vector2(30f * _s, 30f * _s);
                s.Root.gameObject.SetActive(true);
                s.Plate.gameObject.SetActive(true);
                s.Aro.gameObject.SetActive(false);
                s.XMark.gameObject.SetActive(false);
            }

            // estrellas menores de la figura: tenues desde el inicio (radio ~1,25 dp, alfa 0,5)
            var isAnchor = new HashSet<int>(f.anclas);
            int m = 0;
            for (int i = 0; i < f.PointCount; i++)
            {
                if (isAnchor.Contains(i)) continue;
                var img = Minor(m++);
                var sp = IntrusaLayout.ToScreen(f.Point(i), spec.Mirror, spec.RotationDeg);
                img.rectTransform.anchoredPosition = LogicalToPlay(sp);
                img.rectTransform.sizeDelta = new Vector2(2.5f * _s, 2.5f * _s);
                img.color = new Color(StarTint.r, StarTint.g, StarTint.b, 0f);
                img.gameObject.SetActive(true);
            }

            _reviewTag.gameObject.SetActive(spec.Review);
            if (spec.Review) _reviewTag.text = "Por repasar";

            // el grabado de esta ronda se rasteriza en segundo plano (a pedazos) mientras se piensa
            StartCoroutine(BakeEngraving(spec));
        }

        private IEnumerator BakeEngraving(IntrusaSpec spec)
        {
            var e = new Engraving();
            var it = IntrusaSprites.BakeCoroutine(spec.Figure, spec.Mirror, spec.RotationDeg, e, 90);
            while (it.MoveNext()) yield return null;
            if (_spec != spec) { e.Release(); yield break; }
            if (_engraving != null) _engraving.Release();
            _engraving = e;
        }

        private Image Minor(int i)
        {
            while (_minor.Count <= i) _minor.Add(MakeImage(_fig, "MinorStar", DiscSprite.Get()));
            return _minor[i];
        }

        private IEnumerator AppearStars()
        {
            float dur = GameFeel.ReduceMotion ? 0.12f : 0.4f;
            float t = 0f;
            while (t < dur)
            {
                t += GameClock.DeltaTime;
                float k = Mathf.Clamp01(t / dur);
                float pop = GameFeel.ReduceMotion ? 1f : Mathf.LerpUnclamped(0.6f, 1f, UiFx.EaseOutBack(k));
                foreach (var s in _slots)
                {
                    s.Group.alpha = k;
                    s.PlateGroup.alpha = k;
                    s.Root.localScale = Vector3.one * pop;
                }
                for (int i = 0; i < _minor.Count; i++)
                    if (_minor[i].gameObject.activeSelf) _minor[i].color = new Color(StarTint.r, StarTint.g, StarTint.b, 0.5f * k);
                yield return null;
            }
            foreach (var s in _slots) { s.Group.alpha = 1f; s.PlateGroup.alpha = 1f; s.Root.localScale = Vector3.one; }
        }

        private IEnumerator FadeOutRound()
        {
            float dur = GameFeel.ReduceMotion ? 0.12f : 0.25f;
            float t = 0f;
            var imgs = new List<Image>();
            foreach (var l in _lines) foreach (var im in l.Img) if (im.gameObject.activeSelf) imgs.Add(im);
            var alphas = new List<float>();
            foreach (var im in imgs) alphas.Add(im.color.a);
            var texts = new List<Text> { _name, _rule, _named, _reviewTag };
            while (t < dur)
            {
                t += GameClock.DeltaTime;
                float k = 1f - Mathf.Clamp01(t / dur);
                foreach (var s in _slots) { s.Group.alpha = Mathf.Min(s.Group.alpha, k); s.PlateGroup.alpha = Mathf.Min(s.PlateGroup.alpha, k); }
                for (int i = 0; i < imgs.Count; i++) imgs[i].color = NeuroStyle.WithAlpha(imgs[i].color, alphas[i] * k);
                for (int i = 0; i < _minor.Count; i++) if (_minor[i].gameObject.activeSelf) _minor[i].color = NeuroStyle.WithAlpha(_minor[i].color, 0.5f * k);
                if (_outlineImg.gameObject.activeSelf) _outlineImg.color = NeuroStyle.WithAlpha(_outlineImg.color, k);
                if (_shadeImg.gameObject.activeSelf) _shadeImg.color = NeuroStyle.WithAlpha(_shadeImg.color, k);
                foreach (var tx in texts) if (tx.gameObject.activeSelf) tx.canvasRenderer.SetAlpha(k);
                yield return null;
            }
            HideRound();
        }

        // ------------------------------------------------------------------ la estrella fugaz

        private IEnumerator FallStar(Slot s)
        {
            PlayClip(IntrusaSounds.Fall(), 0.55f);
            s.PlateGroup.alpha = 0f;
            if (GameFeel.ReduceMotion)
            {
                float t0 = 0f;
                while (t0 < 0.2f) { t0 += GameClock.DeltaTime; s.Group.alpha = 1f - t0 / 0.2f; yield return null; }
                s.Group.alpha = 0f;
                yield break;
            }
            Vector2 from = s.PosU;
            float dir = from.x >= 0f ? 1f : -1f;
            float t = 0f;
            float dur = IntrusaContract.FallSeconds;
            while (t < dur)
            {
                t += GameClock.DeltaTime;
                float k = Mathf.Clamp01(t / dur);
                s.Root.anchoredPosition = FallPos(from, dir, t);
                s.Root.localRotation = Quaternion.Euler(0f, 0f, -dir * 180f * k);
                s.Group.alpha = 1f - Mathf.Clamp01((k - 0.6f) / 0.4f);
                // estela: copias que siguen el camino un poco antes
                for (int i = 0; i < _trail.Count; i++)
                {
                    float ti = t - (i + 1) * 0.035f;
                    var im = _trail[i];
                    if (ti < 0f) { im.gameObject.SetActive(false); continue; }
                    im.gameObject.SetActive(true);
                    im.rectTransform.anchoredPosition = FallPos(from, dir, ti);
                    float sz = Mathf.Lerp(26f, 6f, (i + 1f) / _trail.Count) * _s;
                    im.rectTransform.sizeDelta = new Vector2(sz, sz);
                    im.color = NeuroStyle.WithAlpha(LineGold, (1f - (i + 1f) / _trail.Count) * 0.7f * s.Group.alpha);
                }
                yield return null;
            }
            s.Group.alpha = 0f;
            foreach (var im in _trail) im.gameObject.SetActive(false);
        }

        private Vector2 FallPos(Vector2 from, float dir, float t)
        {
            float k = t / IntrusaContract.FallSeconds;
            return from + new Vector2(dir * 90f * _s * k, -(420f * _s) * k * k);
        }

        // ------------------------------------------------------------------ la chispa traza la figura

        private IEnumerator Trace(IntrusaSpec spec, float speed, bool quiet)
        {
            var f = spec.Figure;
            var segs = IntrusaSpark.Plan(f, spec.Mirror, spec.RotationDeg, speed, out float total);
            while (_lines.Count < segs.Count) _lines.Add(MakeLine());
            var pos = new Vector2[f.PointCount];
            for (int i = 0; i < pos.Length; i++) pos[i] = LogicalToPlay(IntrusaLayout.ToScreen(f.Point(i), spec.Mirror, spec.RotationDeg));
            var isAnchor = new HashSet<int>(f.anclas);
            var anchorSlot = new Dictionary<int, Slot>();
            for (int i = 0; i < 4; i++) anchorSlot[f.anclas[i]] = _slots[i];
            for (int i = 0; i < segs.Count; i++)
            {
                _lines[i].A = pos[segs[i].From];
                _lines[i].B = pos[segs[i].To];
                SetLine(_lines[i], 0f, 0.4f);
            }

            if (GameFeel.ReduceMotion)
            {
                // sin recorrido: todas las líneas aparecen con un fundido de 300 ms
                float t0 = 0f;
                while (t0 < 0.3f)
                {
                    t0 += GameClock.DeltaTime;
                    for (int i = 0; i < segs.Count; i++) SetLine(_lines[i], 1f, Mathf.Lerp(0.4f, 1f, t0 / 0.3f), t0 / 0.3f);
                    yield return null;
                }
                for (int i = 0; i < segs.Count; i++) SetLine(_lines[i], 1f, 1f);
                PlayClip(IntrusaSounds.Chord(), quiet ? 0.35f : 0.6f);
                GameFeel.Haptic(GameFeel.HapticKind.Firm);
                yield break;
            }

            _headHalo.gameObject.SetActive(true);
            _headCore.gameObject.SetActive(true);
            int step = 0;
            var arrived = new bool[segs.Count];
            ArriveAt(segs[0].From, pos, isAnchor, anchorSlot, ref step, quiet);
            float t = 0f;
            while (t < total + 0.05f)
            {
                t += GameClock.DeltaTime;
                Vector2 head = pos[segs[0].From];
                for (int i = 0; i < segs.Count; i++)
                {
                    var sg = segs[i];
                    if (t < sg.Start) continue;
                    float p = sg.End > sg.Start ? Mathf.Clamp01((t - sg.Start) / (sg.End - sg.Start)) : 1f;
                    float after = Mathf.Clamp01((t - sg.End) / 0.38f);
                    SetLine(_lines[i], p, p >= 1f ? Mathf.Lerp(0.4f, 1f, after) : 0.4f);
                    if (p < 1f) head = Vector2.Lerp(_lines[i].A, _lines[i].B, p);
                    else if (!arrived[i])
                    {
                        arrived[i] = true;
                        head = _lines[i].B;
                        ArriveAt(sg.To, pos, isAnchor, anchorSlot, ref step, quiet);
                    }
                    else if (i == segs.Count - 1 || t < segs[i + 1].Start) head = _lines[i].B;
                }
                _headHalo.rectTransform.anchoredPosition = head;
                _headCore.rectTransform.anchoredPosition = head;
                SpawnParticles(head, 3);
                yield return null;
            }
            for (int i = 0; i < segs.Count; i++) SetLine(_lines[i], 1f, 1f);
            _headHalo.gameObject.SetActive(false);
            _headCore.gameObject.SetActive(false);
            PlayClip(IntrusaSounds.Chord(), quiet ? 0.35f : 0.65f);
            GameFeel.Haptic(GameFeel.HapticKind.Firm);
            float settle = 0f;
            while (settle < 0.38f)
            {
                settle += GameClock.DeltaTime;
                yield return null;
            }
        }

        private void ArriveAt(int point, Vector2[] pos, HashSet<int> isAnchor, Dictionary<int, Slot> anchorSlot, ref int step, bool quiet)
        {
            bool word = isAnchor.Contains(point);
            FlashRing(pos[point], word && !quiet);
            PlayClip(IntrusaSounds.Note(step, word && !quiet), 0.55f);
            step = Mathf.Min(step + 1, IntrusaSounds.NoteCount - 1);
            if (word && anchorSlot.TryGetValue(point, out var slot)) StartCoroutine(PopRect(slot.Core.rectTransform, 1.7f, 0.3f));
        }

        private void SetLine(LineViz l, float progress, float bright, float fade = 1f)
        {
            Vector2 d = l.B - l.A;
            float len = d.magnitude * Mathf.Clamp01(progress);
            bool on = len > 0.5f;
            float ang = Mathf.Atan2(d.y, d.x) * Mathf.Rad2Deg;
            Vector2 mid = l.A + d.normalized * (len * 0.5f);
            for (int i = 0; i < 3; i++)
            {
                var im = l.Img[i];
                if (im.gameObject.activeSelf != on) im.gameObject.SetActive(on);
                if (!on) continue;
                var r = im.rectTransform;
                r.anchoredPosition = mid;
                r.sizeDelta = new Vector2(len, LineWidth[i] * _s);
                r.localRotation = Quaternion.Euler(0f, 0f, ang);
                im.color = new Color(LineGold.r, LineGold.g, LineGold.b, LineAlpha[i] * bright * fade);
            }
        }

        private LineViz MakeLine()
        {
            var l = new LineViz();
            for (int i = 0; i < 3; i++) l.Img[i] = MakeImage(_fig, "Line" + i, IntrusaSprites.Line());
            return l;
        }

        // ------------------------------------------------------------------ partículas y destellos

        private void SpawnParticles(Vector2 at, int n)
        {
            for (int k = 0; k < n; k++)
            {
                Particle p = null;
                foreach (var q in _particles) if (!q.Alive) { p = q; break; }
                if (p == null) return;
                p.Alive = true;
                p.Age = 0f;
                p.Life = (0.6f + (float)_rng.NextDouble() * 0.6f) * 0.6f;
                float a = (float)_rng.NextDouble() * Mathf.PI * 2f;
                p.Vel = new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * (10f + (float)_rng.NextDouble() * 26f) * _s;
                p.Pos = at;
                p.Img.gameObject.SetActive(true);
            }
        }

        private void FlashRing(Vector2 at, bool strong)
        {
            RingFx r = null;
            foreach (var q in _rings) if (!q.Alive) { r = q; break; }
            if (r == null) return;
            r.Alive = true; r.Age = 0f; r.Dur = 0.65f; r.Pos = at; r.Size = strong ? 1f : 0.7f;
            r.Img.gameObject.SetActive(true);
        }

        private void UpdateFx(float dt)
        {
            foreach (var p in _particles)
            {
                if (!p.Alive) continue;
                p.Age += dt;
                if (p.Age >= p.Life) { p.Alive = false; p.Img.gameObject.SetActive(false); continue; }
                p.Pos += p.Vel * dt;
                float k = p.Age / p.Life;
                float sz = Mathf.Lerp(5f, 1.5f, k) * _s;
                p.Img.rectTransform.anchoredPosition = p.Pos;
                p.Img.rectTransform.sizeDelta = new Vector2(sz, sz);
                p.Img.color = new Color(1f, 217f / 255f, 138f / 255f, 0.9f * (1f - k));
            }
            foreach (var r in _rings)
            {
                if (!r.Alive) continue;
                r.Age += dt;
                if (r.Age >= r.Dur) { r.Alive = false; r.Img.gameObject.SetActive(false); continue; }
                float k = UiFx.EaseOutCubic(r.Age / r.Dur);
                float radius = Mathf.Lerp(5f, 27f, k) * r.Size;
                r.Img.rectTransform.anchoredPosition = r.Pos;
                r.Img.rectTransform.sizeDelta = new Vector2(radius * 2f * _s, radius * 2f * _s);
                r.Img.color = new Color(1f, 0.93f, 0.78f, 0.85f * (1f - k) * r.Size);
            }
        }

        // ------------------------------------------------------------------ el grabado

        private IEnumerator Reveal(IntrusaSpec spec, bool named, bool review, bool newPlate)
        {
            // el grabado se rasteriza durante la ronda; si por algo no está listo, se espera un poco
            float wait = 0f;
            while (_engraving == null && wait < 2f) { wait += GameClock.DeltaTime; yield return null; }
            var f = spec.Figure;
            float speed = review ? 2.2f : 1f;
            bool calm = GameFeel.ReduceMotion;
            PlayClip(IntrusaSounds.Engrave(), 0.4f);

            if (_engraving != null)
            {
                Vector2 c = LogicalToPlay(_engraving.Center);
                Vector2 size = _engraving.Size * _s;
                _outlineImg.sprite = _engraving.OutlineSprite();
                _shadeImg.sprite = _engraving.ShadeSprite();
                foreach (var im in new[] { _outlineImg, _shadeImg })
                {
                    im.rectTransform.anchoredPosition = c;
                    im.rectTransform.sizeDelta = size;
                }
                _outlineImg.fillAmount = calm ? 1f : 0f;
                _outlineImg.color = Color.white;
                _shadeImg.color = new Color(1f, 1f, 1f, 0f);
                _outlineImg.gameObject.SetActive(true);
                _shadeImg.gameObject.SetActive(true);
            }

            // textos: el nombre (Fraunces Italic) y la regla
            _name.text = f.nombre;
            _name.gameObject.SetActive(true);
            _name.canvasRenderer.SetAlpha(0f);
            _rule.text = review ? "Por repasar: " + spec.G.n : spec.G.n;
            _rule.gameObject.SetActive(true);
            _rule.canvasRenderer.SetAlpha(0f);
            _named.gameObject.SetActive(named);
            _named.text = "La nombraste tú · puntos ×2";
            _named.canvasRenderer.SetAlpha(0f);

            float t = 0f;
            float total = calm ? 0.35f : 1.7f / speed;
            while (t < total)
            {
                t += GameClock.DeltaTime;
                float ms = t * 1000f * speed;
                if (calm)
                {
                    float k = Mathf.Clamp01(t / 0.3f);
                    _shadeImg.color = new Color(1f, 1f, 1f, k);
                    SetTextAlpha(k);
                }
                else
                {
                    _outlineImg.fillAmount = UiFx.EaseOutCubic(Mathf.Clamp01(ms / 1500f));
                    _shadeImg.color = new Color(1f, 1f, 1f, Mathf.Clamp01((ms - 700f) / 900f));
                    SetTextAlpha(Mathf.Clamp01((ms - 900f) / 600f));
                }
                yield return null;
            }
            _outlineImg.fillAmount = 1f;
            _shadeImg.color = Color.white;
            SetTextAlpha(1f);
            if (named) StartCoroutine(UiFx.RingBurst(_fxLayer, Center(), NamedColor, 120f, 900f, 0.7f));
        }

        private void SetTextAlpha(float k)
        {
            _name.canvasRenderer.SetAlpha(k);
            _rule.canvasRenderer.SetAlpha(k);
            if (_named.gameObject.activeSelf) _named.canvasRenderer.SetAlpha(k);
        }

        // ------------------------------------------------------------------ efectos varios

        private void CheckClearSky()
        {
            bool clear = _streak >= IntrusaContract.ClearStreak;
            if (clear && !_clearShown)
            {
                _clearShown = true;
                _toast.Show("¡Cielo despejado!", "Puntos × 1,5", AmberColor, 1.2f);
                GameFeel.LevelUp();
            }
            else if (!clear) _clearShown = false;
        }

        private void PlayClip(AudioClip clip, float volume)
        {
            if (clip == null || !GameFeel.SoundOn) return;
            _audioSource.PlayOneShot(clip, volume);
        }

        private IEnumerator FloatText(Vector2 pos, string text, Color color)
        {
            var t = MakeText(_fxLayer, "Float", 58, TextAnchor.MiddleCenter, color, 0f, 0f);
            NeuroStyle.ClayText(t, 4f, 6f);
            var r = t.rectTransform;
            r.anchorMin = r.anchorMax = new Vector2(0.5f, 0.5f);
            r.sizeDelta = new Vector2(500f, 100f);
            t.horizontalOverflow = HorizontalWrapMode.Overflow;
            t.text = text;
            float e = 0f;
            const float seconds = 0.8f;
            while (e < seconds)
            {
                e += GameClock.DeltaTime;
                float k = Mathf.Clamp01(e / seconds);
                r.anchoredPosition = pos + new Vector2(0f, 60f * _s * (Motion.Decorative ? UiFx.EaseOutCubic(k) : 0f) * 0.4f + 110f);
                t.color = NeuroStyle.WithAlpha(color, 1f - k * k);
                yield return null;
            }
            Destroy(t.gameObject);
        }

        private void ResetPulse()
        {
            for (int i = 0; i < 4; i++) { _slots[i].Core.rectTransform.localScale = Vector3.one; _slots[i].Halo.rectTransform.localScale = Vector3.one; }
        }

        // ------------------------------------------------------------------ entrada y ambiente

        private void Update()
        {
            if (PollTutorialSkip()) return;             // un toque en «Saltar tutorial» no es un toque al juego
            float dt = GameClock.DeltaTime;
            UpdateClock();
            if (_clearGlow != null && _phase == Phase.Playing)
            {
                float target = _streak >= IntrusaContract.ClearStreak ? 0.13f : 0f;
                var c = _clearGlow.color;
                _clearGlow.color = new Color(c.r, c.g, c.b, Mathf.Lerp(c.a, target, Mathf.Min(1f, dt * 2f)));
            }
            if (_phase != Phase.Playing) return;
            UpdateFx(dt);
            AnimateAmbient();
            ReadPress();
        }

        private void ReadPress()
        {
            bool down = false;
            Vector2 pos = Vector2.zero;
            if (Input.touchCount > 0)
            {
                var t = Input.GetTouch(0);
                down = t.phase == TouchPhase.Began;
                pos = t.position;
            }
            else
            {
                down = Input.GetMouseButtonDown(0);
                pos = Input.mousePosition;
            }
#if UNITY_EDITOR
            if (_guided && !down && GuidedTutorial.TryPress(out var injected)) { down = true; pos = injected; }       // el smoke da un toque «de verdad» en el hueco
#endif
            if (!down) return;
            if (_tutorial != null && _tutorial.Coach != null && _tutorial.Coach.Blocks(pos)) return;                  // el foco de Nubi: solo vale el toque dentro del hueco
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(_play, pos, null, out var local)) return;
            _press = PlayToLogical(local);
        }

        /// <summary>Titileo suave de las estrellas con palabra y latido de las cuatro mientras se piensa «¿qué las une?».</summary>
        private void AnimateAmbient()
        {
            float time = GameClock.Time;
            bool calm = GameFeel.ReduceMotion;
            foreach (var s in _slots)
            {
                if (!s.Root.gameObject.activeSelf) continue;
                float tw = calm ? 0.8f : 0.72f + 0.28f * Mathf.Sin(time * 1.3f + s.PulsePhase);
                s.Halo.color = NeuroStyle.WithAlpha(StarTint, 0.42f * tw);
                s.Flare.color = NeuroStyle.WithAlpha(StarTint, 0.9f * tw);
            }
            if (_pulsing && !calm)
            {
                float k = 1f + 0.28f * (0.5f + 0.5f * Mathf.Sin(time * 5.2f));
                for (int i = 0; i < 4; i++)
                {
                    _slots[i].Core.rectTransform.localScale = Vector3.one * k;
                    _slots[i].Halo.rectTransform.localScale = Vector3.one * (1f + (k - 1f) * 0.8f);
                }
            }
        }

        private void UpdateClock()
        {
            if (_guided || !Endless || _phase != Phase.Playing || _endsAt <= 0f) return;
            float left = _endsAt - GameClock.Time;
            float frac = Mathf.Clamp01(1f - left / IntrusaContract.RetoSeconds);
            // la cometa cruza de izquierda a derecha: sin reloj por ronda
            float half = _timerTrack.rect.width * 0.5f;
            _comet.anchoredPosition = new Vector2(Mathf.Lerp(-half, half, frac), 0f);
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
            else _hud.SetInfo($"{Mathf.Min(_resolved + 1, IntrusaContract.PrecisionRounds)} de {IntrusaContract.PrecisionRounds}");
        }

        // ------------------------------------------------------------------ tutorial con Nubi (ronda guiada) y «Cómo se juega» desde la pausa

        private bool _guided, _loopOn, _baked, _practiceHit;
        private RectTransform _holeRect;                        // una caja invisible que sigue a lo que el foco del tutorial ilumina

        /// <summary>El tutorial guiado común sobre el área segura (se llama al final de <c>BuildUi</c>, para que quede encima de todo).</summary>
        private void SetUpTutorial()
        {
            var hole = new GameObject("TutorialHole", typeof(RectTransform));
            hole.transform.SetParent(_play, false);
            _holeRect = (RectTransform)hole.transform;
            _holeRect.anchorMin = _holeRect.anchorMax = new Vector2(0.5f, 0.5f);
            BuildTutorial(_safe, GameHud.Height + 10f, "La estrella intrusa", "Cuatro estrellas comparten algo y una no. Toca la intrusa.");
        }

        /// <summary>Pone la caja invisible sobre un rectángulo lógico (dp de una pantalla de 360 de ancho) y la devuelve, para que el foco la ilumine.</summary>
        private RectTransform HoleOver(LRect r)
        {
            _holeRect.anchoredPosition = LogicalToPlay(new Vector2(r.CenterX, r.CenterY));
            _holeRect.sizeDelta = new Vector2(r.W * _s, r.H * _s);
            return _holeRect;
        }

        /// <summary>La caja de la figura más las placas de las cuatro palabras (algunas sobresalen: la de arriba va sobre su estrella y la de abajo, debajo), para iluminarlo todo junto.</summary>
        private LRect FigureHole()
        {
            float x0 = IntrusaLayout.BoxX, y0 = IntrusaLayout.BoxY, x1 = x0 + IntrusaLayout.BoxW, y1 = y0 + IntrusaLayout.BoxH;
            for (int i = 0; i < 4; i++)
            {
                var q = _slots[i].Plaque;
                x0 = Mathf.Min(x0, q.X); y0 = Mathf.Min(y0, q.Y); x1 = Mathf.Max(x1, q.X + q.W); y1 = Mathf.Max(y1, q.Y + q.H);
            }
            const float pad = 8f;
            return new LRect(x0 - pad, y0 - pad, x1 - x0 + 2f * pad, y1 - y0 + 2f * pad);
        }

        /// <summary>La caída de la intrusa, la chispa que dibuja la figura y el grabado con su nombre; después avisa. No anota nada: ni racha, ni puntos, ni lámina del atlas.</summary>
        private IEnumerator PracticeReveal(IntrusaSpec spec, System.Action done)
        {
            yield return Trace(spec, 1f, false);
            yield return Reveal(spec, false, false, false);
            done();
        }

        // <guided>
        protected override IEnumerator GuidedRound(GuidedTutorial t)
        {
            t.BeginPractice();
            _guided = true;
            while (!_baked) yield return null;                         // el arte se hornea mientras Nubi se presenta
            var coach = t.Coach;
            // Pasos: 1) «Cuatro estrellas comparten algo; una no» (aviso); 2) «Toca la intrusa» (hueco: la palabra intrusa); 3) la intrusa cae, la chispa dibuja la figura de las otras cuatro y aparece su nombre (mirar);
            // 4) la figura se guarda en el atlas (aviso); 5) «¡Listo!». Ejemplo clarísimo: cuatro frutas y un zapato. No pasa por el DDA, el puntaje, el atlas ni el repaso.
            var group = IntrusaContract.PracticeGroup();
            var shape = _bank.FigureOf(group.k) ?? IntrusaBank.Fallback().FigureOf(group.k);
            var spec = IntrusaLayout.Arrange(group, shape, new System.Random(IntrusaContract.PracticeSeed), TextWidthLogical);
            _hint.gameObject.SetActive(false);
            _timerTrack.gameObject.SetActive(false);
            _phase = Phase.Playing;
            _press = null;
            _roundLive = false;
            _spec = spec;
            ShowRound(spec);
            yield return StartCoroutine(AppearStars());
            bool ok = !t.Skipped;

            // 1) cuatro comparten algo; una no
            if (ok)
            {
                yield return StartCoroutine(coach.Notice(CoachTexts.Intrusa.Look, 3.4f));
                ok = !t.Skipped;
            }

            // 2) tocar la intrusa (solo vale esa: si el toque cae en otra estrella, se vuelve a pedir)
            int choice = -1;
            int tries = 0;
            _practiceHit = false;
            while (ok && choice != 4)
            {
                _press = null;
#if UNITY_EDITOR
                StartCoroutine(ProbeTap(coach));
#endif
                yield return StartCoroutine(coach.Touch(() => coach.RectOf(HoleOver(_slots[4].Touch)), CoachTexts.Intrusa.Tap));
                ok = !t.Skipped;
                if (ok && _press.HasValue) choice = PickSlot(_press.Value);
                _press = null;
                if (ok && choice != 4 && (coach.ClosedBySafetyNet || ++tries >= 2)) choice = 4;          // el toque no llegó (red de seguridad) o ya se pidió dos veces: la ronda toca la intrusa y sigue (Tarea 58)
#if UNITY_EDITOR
                if (ok && GuidedTutorial.EditorAutoContinue && choice != 4) choice = 4;          // el smoke no toca: lo hace por la persona
#endif
            }
            _practiceHit = choice == 4;

            // 3) mirar: la intrusa cae, la chispa une las otras cuatro y aparece la figura con su nombre
            Coroutine fall = null, reveal = null;
            if (ok)
            {
                bool revealed = false;
                float revealedAt = -1f;
                fall = StartCoroutine(FallStar(_slots[4]));
                reveal = StartCoroutine(PracticeReveal(spec, () => revealed = true));
                yield return StartCoroutine(coach.Watch(() => coach.RectOf(HoleOver(FigureHole())), CoachTexts.Intrusa.Spark,
                    () =>
                    {
                        if (!revealed) return false;
                        if (revealedAt < 0f) revealedAt = GameClock.Time;
                        return GameClock.Time - revealedAt >= 1.4f;
                    }, 25f));
                ok = !t.Skipped;
            }

            // 4) la figura va al atlas
            if (ok)
            {
                yield return StartCoroutine(coach.Notice(CoachTexts.Intrusa.Atlas, 3.4f));
                ok = !t.Skipped;
            }
            if (ok) yield return StartCoroutine(coach.Notice(CoachTexts.Ready, 1.5f));

            if (fall != null) StopCoroutine(fall);
            if (reveal != null) StopCoroutine(reveal);
            coach.Hide();
            HideRound();
            _press = null;
            _roundLive = false;
            _timerTrack.gameObject.SetActive(Endless);
            _guided = false;
            _phase = Phase.Idle;                                       // la partida (o «Cómo se juega») sigue desde su bucle
            t.EndPractice();
        }
        // </guided>

#if UNITY_EDITOR
        /// <summary>SOLO EN EL EDITOR (smoke del tutorial): da un toque «de verdad» en el centro de la palabra intrusa que el paso ilumina y comprueba que el juego la reconoce. Si el hueco no coincidiera con la estrella dibujada,
        /// el tutorial se «pegaría» sin dejar tocar lo que pide (Ricardo, 6-oct); así se vería antes de llegar al teléfono.</summary>
        private IEnumerator ProbeTap(NubiCoach coach)
        {
            if (!GuidedTutorial.EditorAutoContinue) yield break;
            float w = 0f;
            while (!coach.Active && w < 3f) { w += GameClock.RealDeltaTime; yield return null; }
            w = 0f;
            while (coach.Active && w < 0.7f) { w += GameClock.RealDeltaTime; yield return null; }
            if (!coach.Active) yield break;
            GuidedTutorial.EditorPressPos = RectTransformUtility.WorldToScreenPoint(null, _holeRect.position);
            GuidedTutorial.EditorPressFrame = Time.frameCount + 1;
            for (int i = 0; i < 4; i++) yield return null;
            if (!_practiceHit) Debug.LogError("[SmokeTest] Intrusa: un toque en el centro de la palabra intrusa NO llegó al juego (el hueco no coincide con la estrella)");
            else Debug.Log("[SmokeTest] Intrusa: un toque en el centro de la palabra intrusa llegó al juego");
        }
#endif

        // ------------------------------------------------------------------ «Cómo se juega» desde la pausa

        protected override bool HowToReady => _loopOn && _phase == Phase.Playing && !_guided;

        protected override void HowToSuspend()
        {
            // la ronda que estaba en pantalla se descarta (si todavía no se había contestado, no cuenta); después sigue la partida con una nueva
            _roundLive = false;
            _press = null;
            HideRound();
            _phase = Phase.Idle;
        }

        protected override void HowToResume(float spentSeconds)
        {
            _endsAt = HowToClock.Shift(_endsAt, spentSeconds);       // el tiempo que duró «Cómo se juega» no se le descuenta al Reto
            _lastTickSecond = -1;
            _nextSpec = null;
            _phase = Phase.Playing;
            StartCoroutine(MainLoop());
        }

        // ------------------------------------------------------------------ fin

        private IEnumerator FinishGame()
        {
            _phase = Phase.Done;
            _roundLive = false;
            HideRound();
            try
            {
                PlayerPrefs.SetString(RecentKey, IntrusaContract.PushRecent(PlayerPrefs.GetString(RecentKey, ""), _director.UsedInOrder));
                PlayerPrefs.SetString(ReviewKey, IntrusaContract.FormatReview(_review));
                PlayerPrefs.SetString(PlatesKey, IntrusaContract.FormatPlates(_plates));
                PlayerPrefs.Save();
            }
            catch (System.Exception) { }

            int total = _tally.Total, correct = _tally.Correct;
            float acc = total > 0 ? (float)correct / total : 0f;
            int score = IntrusaContract.Score(acc, _dda.PeakLevel);
            int rt = _tally.MedianMs;
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
                    average_response_time_ms = rt < 0 ? 0 : rt,
                    level = _config.config.level,
                    timed = _config.config.timed,
                    end_rating = _dda.RatingNormalized,
                    mode_trials = _dda.ScoredTrials,
                    mode_hits = _dda.ScoredCorrect,
                    peak_level = _dda.PeakLevel,
                    intr_seen_type = (int[])_tally.Seen.Clone(),
                    intr_hits_type = (int[])_tally.Hits.Clone(),
                    intr_rt_ms = rt,
                    intr_best_streak = _tally.BestStreak,
                    intr_bonus_seen = _tally.BonusSeen,
                    intr_bonus_hits = _tally.BonusHits,
                    intr_new_plates = string.Join(";", _tally.NewPlates),
                    intr_review_new = string.Join(";", _tally.NewReview),
                    intr_review_done = string.Join(";", _tally.Mastered),
                    intr_named = string.Join(";", _tally.Named)
                }
            };
            NativeBridge.ForwardTelemetryToPlatform(JsonUtility.ToJson(telemetry));
            yield break;
        }

        private void ShowResult(int score)
        {
            _exit.Show();
            _resultRoot.Find("Title").GetComponent<Text>().text = score >= 85 ? "¡Qué buen cielo!" : score >= 65 ? "¡Buen atlas!" : "Misión completada";
            _resultRoot.Find("Detail").GetComponent<Text>().text = $"{_tally.Correct} de {_tally.Total} bien · mejor racha {_tally.BestStreak}";
            int n = _tally.NewPlates.Count;
            _resultRoot.Find("Extra").GetComponent<Text>().text = n > 0 ? (n == 1 ? "1 lámina nueva en tu atlas" : $"{n} láminas nuevas en tu atlas") : "Sigue completando tu atlas";
            _resultRoot.gameObject.SetActive(true);
            StartCoroutine(AnimateResult(score));
        }

        // ------------------------------------------------------------------ construcción de UI

        protected override void BuildUi()
        {
            var canvasGo = new GameObject("IntrusaCanvas");
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
            WorldBackdrop.Build(bgRect, GameWorld.CieloProfundo);
            // al encadenar rachas de 5 el cielo se aclara un poco (una nebulosa lila más viva, sobre el fondo y debajo del juego)
            _clearGlow = MakeImage(bgRect, "ClearGlow", RadialGlowSprite.Get());
            _clearGlow.rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
            _clearGlow.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            _clearGlow.rectTransform.sizeDelta = new Vector2(1900f, 1900f);
            _clearGlow.color = NeuroStyle.WithAlpha(NeuroStyle.Grape, 0f);
            _clearGlow.gameObject.SetActive(true);

            var safeGo = new GameObject("SafeAreaContent");
            safeGo.transform.SetParent(canvasGo.transform, false);
            _safe = safeGo.AddComponent<RectTransform>();
            ApplySafeArea(_safe);

            _hud = new GameHud(_safe, "La estrella intrusa", MarginU, this);
            BuildTimer();

            var playGo = new GameObject("Play");
            playGo.transform.SetParent(_safe, false);
            _play = playGo.AddComponent<RectTransform>();
            Stretch(_play);

            _fig = Layer(_play, "Figure");
            BuildFigurePieces();
            _textLayer = Layer(_play, "Texts");
            BuildTexts();
            BuildOptions();
            BuildSlots();
            _fxLayer = Layer(_play, "Fx");

            _hint = MakeText(_play, "Hint", 54, TextAnchor.MiddleCenter, new Color(1f, 1f, 1f, 0.95f), 3f, 0.4f);
            NeuroStyle.ClayText(_hint, 3f, 4f);
            var hr = _hint.rectTransform;
            hr.anchorMin = hr.anchorMax = new Vector2(0.5f, 0.5f);
            hr.sizeDelta = new Vector2(900f, 180f);
            _hint.gameObject.SetActive(false);

            _measure = MakeText(_play, "Measure", 51, TextAnchor.UpperLeft, Color.white, 0f, 0f);
            _measure.font = UiFonts.Word;
            _measure.color = new Color(0f, 0f, 0f, 0f);
            _measure.rectTransform.sizeDelta = new Vector2(4000f, 200f);

            BuildResultPanel();
            _exit = new ExitButton(_safe, this, UnitsPerDp);
            _toast = new Toast(_safe, this, UnitsPerDp);
            _toast.SetBelowHud();
            SetUpTutorial();

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
            var go = new GameObject("CometTrack");
            go.transform.SetParent(_safe, false);
            _timerTrack = go.AddComponent<RectTransform>();
            _timerTrack.anchorMin = _timerTrack.anchorMax = new Vector2(0.5f, 1f);
            _timerTrack.pivot = new Vector2(0.5f, 0.5f);
            _timerTrack.sizeDelta = new Vector2(960f, 6f);
            _timerTrack.anchoredPosition = new Vector2(0f, -(GameHud.Height + 14f));
            var img = go.AddComponent<Image>();
            img.sprite = RoundedRectSprite.Get(3);
            img.type = Image.Type.Sliced;
            img.color = new Color(1f, 1f, 1f, 0.1f);
            img.raycastTarget = false;

            var cometGo = new GameObject("Comet");
            cometGo.transform.SetParent(go.transform, false);
            _comet = cometGo.AddComponent<RectTransform>();
            _comet.anchorMin = _comet.anchorMax = new Vector2(0.5f, 0.5f);
            _comet.sizeDelta = new Vector2(10f, 10f);
            _cometTail = MakeImage(_comet, "Tail", IntrusaSprites.Line());
            _cometTail.rectTransform.pivot = new Vector2(1f, 0.5f);
            _cometTail.rectTransform.sizeDelta = new Vector2(220f, 12f);
            _cometTail.rectTransform.anchoredPosition = new Vector2(-8f, 0f);
            _cometTail.color = new Color(1f, 0.85f, 0.55f, 0.55f);
            _cometTail.gameObject.SetActive(true);
            _cometGlow = MakeImage(_comet, "Glow", RadialGlowSprite.Get());
            _cometGlow.rectTransform.sizeDelta = new Vector2(110f, 110f);
            _cometGlow.color = new Color(1f, 0.85f, 0.55f, 0.7f);
            _cometGlow.gameObject.SetActive(true);
            var core = MakeImage(_comet, "Core", DiscSprite.Get());
            core.rectTransform.sizeDelta = new Vector2(24f, 24f);
            core.color = SparkCore;
            core.gameObject.SetActive(true);
        }

        private void BuildFigurePieces()
        {
            for (int i = 0; i < 10; i++) _trail.Add(MakeImage(_fig, "Trail", DiscSprite.Get()));
            // el grabado va debajo de las líneas y las estrellas
            _outlineImg = MakeImage(_fig, "EngravingOutline", null);
            _outlineImg.type = Image.Type.Filled;
            _outlineImg.fillMethod = Image.FillMethod.Radial360;
            _outlineImg.fillOrigin = (int)Image.Origin360.Top;
            _outlineImg.fillClockwise = true;
            _shadeImg = MakeImage(_fig, "EngravingShade", null);
            for (int i = 0; i < 14; i++) _lines.Add(MakeLine());
            for (int i = 0; i < 16; i++) _dots.Add(MakeImage(_fig, "Dot", DiscSprite.Get()));
            foreach (var l in _lines) foreach (var im in l.Img) im.gameObject.SetActive(false);
            for (int i = 0; i < 12; i++) _minor.Add(MakeImage(_fig, "MinorStar", DiscSprite.Get()));
            // la chispa: halo dorado de 44 dp y núcleo de 2,8 dp de radio
            _headHalo = MakeImage(_fig, "SparkHalo", RadialGlowSprite.Get());
            _headHalo.rectTransform.sizeDelta = new Vector2(44f * UnitsPerDp, 44f * UnitsPerDp);
            _headHalo.color = new Color(1f, 217f / 255f, 138f / 255f, 0.75f);
            _headCore = MakeImage(_fig, "SparkCore", DiscSprite.Get());
            _headCore.rectTransform.sizeDelta = new Vector2(5.6f * UnitsPerDp, 5.6f * UnitsPerDp);
            _headCore.color = SparkCore;
            for (int i = 0; i < 60; i++) _particles.Add(new Particle { Img = MakeImage(_fig, "Particle", DiscSprite.Get()) });
            for (int i = 0; i < 8; i++) _rings.Add(new RingFx { Img = MakeImage(_fig, "Ring", RingSprite.Get()) });
        }

        private void BuildSlots()
        {
            for (int i = 0; i < 5; i++)
            {
                var s = new Slot { PulsePhase = i * 1.3f };
                var root = new GameObject("Star" + i);
                root.transform.SetParent(_play, false);
                s.Root = root.AddComponent<RectTransform>();
                s.Root.anchorMin = s.Root.anchorMax = new Vector2(0.5f, 0.5f);
                s.Root.pivot = new Vector2(0.5f, 0.5f);
                s.Group = root.AddComponent<CanvasGroup>();
                s.Group.blocksRaycasts = false;
                s.Halo = MakeImage(s.Root, "Halo", RadialGlowSprite.Get());
                s.Core = MakeImage(s.Root, "Core", DiscSprite.Get());
                s.Core.color = new Color(1f, 0.973f, 0.902f, 1f);
                s.Flare = MakeImage(s.Root, "Flare", IntrusaSprites.Flare());
                s.Aro = MakeImage(s.Root, "Aro", RingSprite.Get());
                s.XMark = MakeImage(s.Root, "X", AnswerMarkSprite.Cross());
                foreach (var im in new[] { s.Halo, s.Core, s.Flare }) im.gameObject.SetActive(true);

                var plate = new GameObject("Plate" + i);
                plate.transform.SetParent(_play, false);
                s.Plate = plate.AddComponent<RectTransform>();
                s.Plate.anchorMin = s.Plate.anchorMax = new Vector2(0.5f, 0.5f);
                s.Plate.pivot = new Vector2(0.5f, 0.5f);
                s.PlateGroup = plate.AddComponent<CanvasGroup>();
                s.PlateGroup.blocksRaycasts = false;
                s.PlateBg = plate.AddComponent<Image>();
                s.PlateBg.sprite = RoundedRectSprite.Get(28);
                s.PlateBg.type = Image.Type.Sliced;
                s.PlateBg.color = PlateColor;
                s.PlateBg.raycastTarget = false;
                NeuroStyle.ClayFrame(s.PlateBg, 3f, 5f);
                s.Label = MakeText(s.Plate, "Word", 51, TextAnchor.MiddleCenter, NeuroStyle.Ink, 0f, 0f);
                s.Label.font = UiFonts.Word;
                s.Label.horizontalOverflow = HorizontalWrapMode.Overflow;
                s.Label.verticalOverflow = VerticalWrapMode.Overflow;
                _slots[i] = s;
            }
        }

        private void BuildTexts()
        {
            _name = TextBlock("Name", 96, UiFonts.Name, NameColor);
            _rule = TextBlock("Rule", 48, UiFonts.Word, RuleColor);
            _named = TextBlock("Named", 48, UiFonts.Word, NamedColor);
            _reviewTag = TextBlock("ReviewTag", 42, UiFonts.Word, NeuroStyle.Grape);
            _explain = TextBlock("Explain", 51, UiFonts.Word, new Color(0.95f, 0.93f, 1f, 1f));
            _explain.horizontalOverflow = HorizontalWrapMode.Wrap;
            _explain.verticalOverflow = VerticalWrapMode.Truncate;
            _trapText = TextBlock("TrapText", 48, UiFonts.Word, AmberColor);
            _trapText.horizontalOverflow = HorizontalWrapMode.Wrap;
            // «Seguir»
            var go = new GameObject("Seguir");
            go.transform.SetParent(_play, false);
            _seguir = go.AddComponent<RectTransform>();
            _seguir.anchorMin = _seguir.anchorMax = new Vector2(0.5f, 0.5f);
            _seguir.pivot = new Vector2(0.5f, 0.5f);
            _seguirImg = go.AddComponent<Image>();
            _seguirImg.sprite = RoundedRectSprite.Get(40);
            _seguirImg.type = Image.Type.Sliced;
            _seguirImg.color = PlateColor;
            _seguirImg.raycastTarget = false;
            NeuroStyle.ClayFrame(_seguirImg, 5f, 9f);
            _seguirLabel = MakeText(_seguir, "Label", 58, TextAnchor.MiddleCenter, NeuroStyle.Ink, 0f, 0f);
            _seguirLabel.font = UiFonts.Word;
            _seguirLabel.text = "Seguir";
            go.SetActive(false);
        }

        private Text TextBlock(string name, int fontPx, Font font, Color color)
        {
            var t = MakeText(_textLayer, name, fontPx, TextAnchor.MiddleCenter, color, 0f, 0f);
            t.font = font;
            t.horizontalOverflow = HorizontalWrapMode.Overflow;
            t.verticalOverflow = VerticalWrapMode.Overflow;
            var r = t.rectTransform;
            r.anchorMin = r.anchorMax = new Vector2(0.5f, 0.5f);
            r.pivot = new Vector2(0.5f, 0.5f);
            r.sizeDelta = new Vector2(980f, fontPx * 1.5f);
            t.gameObject.SetActive(false);
            return t;
        }

        private void BuildOptions()
        {
            var barGo = new GameObject("OptionsBar");
            barGo.transform.SetParent(_play, false);
            _optionsBar = barGo.AddComponent<RectTransform>();
            _optionsBar.anchorMin = _optionsBar.anchorMax = new Vector2(0.5f, 0.5f);
            _optionsBar.pivot = new Vector2(0.5f, 0.5f);
            _optionsBar.sizeDelta = new Vector2(760f, 14f);
            var bgi = barGo.AddComponent<Image>();
            bgi.sprite = RoundedRectSprite.Get(7);
            bgi.type = Image.Type.Sliced;
            bgi.color = new Color(1f, 1f, 1f, 0.14f);
            bgi.raycastTarget = false;
            var fillGo = new GameObject("Fill");
            fillGo.transform.SetParent(barGo.transform, false);
            _optionsFill = fillGo.AddComponent<RectTransform>();
            _optionsFill.anchorMin = Vector2.zero; _optionsFill.anchorMax = Vector2.one;
            _optionsFill.offsetMin = _optionsFill.offsetMax = Vector2.zero;
            var fi = fillGo.AddComponent<Image>();
            fi.sprite = RoundedRectSprite.Get(7);
            fi.type = Image.Type.Sliced;
            fi.raycastTarget = false;
            barGo.SetActive(false);

            for (int i = 0; i < 3; i++)
            {
                var go = new GameObject("Option" + i);
                go.transform.SetParent(_play, false);
                var r = go.AddComponent<RectTransform>();
                r.anchorMin = r.anchorMax = new Vector2(0.5f, 0.5f);
                r.pivot = new Vector2(0.5f, 0.5f);
                var img = go.AddComponent<Image>();
                img.sprite = RoundedRectSprite.Get(40);
                img.type = Image.Type.Sliced;
                img.color = PlateColor;
                img.raycastTarget = false;
                NeuroStyle.ClayFrame(img, 5f, 9f);
                var label = MakeText(r, "Label", 51, TextAnchor.MiddleCenter, NeuroStyle.Ink, 0f, 0f);
                label.font = UiFonts.Word;
                BestFit(label, 42);
                var mark = MakeImage(r, "Mark", AnswerMarkSprite.Check());
                mark.rectTransform.anchorMin = mark.rectTransform.anchorMax = new Vector2(1f, 0.5f);
                mark.rectTransform.anchoredPosition = new Vector2(-54f, 0f);
                mark.rectTransform.sizeDelta = new Vector2(72f, 72f);
                go.SetActive(false);
                _options.Add(new OptionButton { Rect = r, Bg = img, Mark = mark, Label = label });
            }
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
            var extra = AddResultText("Extra", 42, new Vector2(0f, -250f), new Color(1f, 1f, 1f, 0.65f));
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

        private static Image MakeImage(Transform parent, string name, Sprite sprite)
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

        // ------------------------------------------------------------------ disposición

        private void Layout()
        {
            Canvas.ForceUpdateCanvases();
            _playW = _play.rect.width;
            _playH = _play.rect.height;
            _s = _playW / IntrusaLayout.ScreenW;
            _logicalH = _playH / _s;
            // en pantallas altas, la escena baja un poco (el aire sobra abajo); en las bajitas, sube un poco
            _yShift = (_logicalH - 640f) * 0.22f;
            _widthCache.Clear();

            // textos de abajo: nombre, regla, «la nombraste tú» y el aviso de repaso, bajo la caja de la figura
            float zoneTop = 520f;
            PlaceText(_name, 548f, 56f);
            PlaceText(_rule, 584f, 30f);
            PlaceText(_named, 608f, 26f);
            PlaceText(_reviewTag, 150f, 26f);

            // opciones de «¿Qué las une?» y «Seguir»: en columna si hay lugar; si no, en tres columnas (siempre ≥ 64 dp de alto)
            float zoneH = _logicalH - zoneTop - _yShift - 20f;
            _stacked = zoneH >= 3f * 64f + 2f * 8f + 26f;
            float optH = 64f;
            float baseY = Mathf.Max(zoneTop + 22f, _logicalH - _yShift - 20f - (_stacked ? 3f * optH + 16f : optH));
            float barY = baseY - 14f;
            _optionsBar.anchoredPosition = LogicalToPlay(new Vector2(180f, barY));
            _optionsBar.sizeDelta = new Vector2(250f * _s, 5f * _s);
            for (int i = 0; i < 3; i++)
            {
                var o = _options[i];
                if (_stacked)
                {
                    o.SizeU = new Vector2(300f * _s, optH * _s);
                    o.CenterU = LogicalToPlay(new Vector2(180f, baseY + optH * 0.5f + i * (optH + 8f)));
                }
                else
                {
                    float w = (ScreenInner() - 16f) / 3f;
                    o.SizeU = new Vector2(w * _s, optH * _s);
                    o.CenterU = LogicalToPlay(new Vector2(IntrusaLayout.SideMargin + w * 0.5f + i * (w + 8f), baseY + optH * 0.5f));
                }
                o.Rect.sizeDelta = o.SizeU;
                o.Rect.anchoredPosition = o.CenterU;
                o.Label.rectTransform.offsetMin = new Vector2(16f, 4f);
                o.Label.rectTransform.offsetMax = new Vector2(-(_stacked ? 90f : 16f), -4f);
            }
            _seguir.sizeDelta = new Vector2(220f * _s, 64f * _s);
            _seguir.anchoredPosition = LogicalToPlay(new Vector2(180f, _logicalH - _yShift - 20f - 32f));

            // explicación y rótulo de la trampa, arriba del «Seguir»
            float expY = Mathf.Max(zoneTop + 8f, _logicalH - _yShift - 20f - 64f - 12f - 70f);
            _explain.rectTransform.sizeDelta = new Vector2(330f * _s, 44f * _s);
            _explain.rectTransform.anchoredPosition = LogicalToPlay(new Vector2(180f, expY + 22f));
            BestFit(_explain, Mathf.RoundToInt(14f * _s));
            _explain.fontSize = Mathf.RoundToInt(17f * _s);
            _explain.resizeTextMaxSize = Mathf.RoundToInt(17f * _s);
            _trapText.rectTransform.sizeDelta = new Vector2(330f * _s, 28f * _s);
            _trapText.rectTransform.anchoredPosition = LogicalToPlay(new Vector2(180f, expY + 58f));
            BestFit(_trapText, Mathf.RoundToInt(14f * _s));
            _trapText.fontSize = Mathf.RoundToInt(16f * _s);
            _trapText.resizeTextMaxSize = Mathf.RoundToInt(16f * _s);

            _hint.rectTransform.anchoredPosition = LogicalToPlay(new Vector2(180f, 540f));
        }

        private float ScreenInner() => IntrusaLayout.ScreenW - 2f * IntrusaLayout.SideMargin;

        private void PlaceText(Text t, float yLogical, float heightLogical)
        {
            t.rectTransform.sizeDelta = new Vector2(ScreenInner() * _s, heightLogical * _s);
            t.rectTransform.anchoredPosition = LogicalToPlay(new Vector2(180f, yLogical));
        }
    }
}
