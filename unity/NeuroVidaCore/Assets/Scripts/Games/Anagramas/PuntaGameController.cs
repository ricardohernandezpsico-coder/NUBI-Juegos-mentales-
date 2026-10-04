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

namespace NeuroVida.Games.Anagramas
{
    /// <summary>
    /// «En la punta de la lengua» (Lenguaje; reemplaza a Anagramas y conserva su id <c>anagramas</c>; ver <see cref="PuntaContract"/> y
    /// docs/diseno-punta-de-la-lengua.md). Nubi capta la DEFINICIÓN de una palabra y la tarjeta de transmisión la escribe sola. Se busca en la memoria:
    /// si sale, «¡La tengo!» trae las fichas (con letras de más) y se arma tocándolas en orden (cada ficha vuela a su casilla y suena con una nota de la
    /// pentatónica). Si no sale, «Una ayuda» sube una escalera de tres peldaños (cuántas letras, la primera letra, solo sus letras) y después dice «Ver la
    /// palabra»: nunca se queda trabado. Al acertar, las letras se encienden en orden y la palabra se vuelve un LUCERO redondo (nunca una estrella con
    /// puntas) que vuela al cielo de arriba: dorado (sola), plateado (1-2 ayudas), cobre (las letras justas) o azul (la mostró Nubi). Las azules vuelven en
    /// otra partida (las guarda la app). Reto = 120 s; Precisión = 8 palabras.
    /// Con «quitar animaciones»: el texto aparece entero, sin ondas, flotación, temblor ni chispas; el vuelo de la ficha a su casilla se queda (forma corta)
    /// y los tiempos se esperan con <see cref="Motion.Hold"/>.
    /// </summary>
    public class PuntaGameController : GameControllerBase
    {
        public const string GameId = PuntaContract.GameId;
        private const string RecentKey = "punta_recent";

        private const float UnitsPerDp = 3f;
        private const float MarginU = 60f;
        private const float TutorialBottomReserve = 40f;
        /// <summary>dp de aire entre la tarjeta y las casillas durante el tutorial: ahí va el mensaje de Nubi.</summary>
        private const float TutorialCaptionGap = 40f;

        // ------------------------------------------------------------------ colores (los del boceto aprobado)

        private static readonly Color Cyan = new Color(127f / 255f, 216f / 255f, 255f / 255f);
        private static readonly Color CardFill = new Color(20f / 255f, 27f / 255f, 58f / 255f);
        private static readonly Color SlotFill = new Color(14f / 255f, 20f / 255f, 48f / 255f);
        private static readonly Color Lavender = new Color(171f / 255f, 165f / 255f, 210f / 255f);
        private static readonly Color Dim = new Color(110f / 255f, 106f / 255f, 154f / 255f);
        private static readonly Color ButtonSecondary = new Color(35f / 255f, 43f / 255f, 87f / 255f);
        private static readonly Color ButtonText = new Color(237f / 255f, 234f / 255f, 251f / 255f);
        private static readonly Color GoodText = new Color(159f / 255f, 245f / 255f, 214f / 255f);
        private static readonly Color WarnText = new Color(255f / 255f, 214f / 255f, 160f / 255f);
        private static readonly Color TileFace = new Color(244f / 255f, 241f / 255f, 255f / 255f);
        private static readonly Color TileLocked = new Color(247f / 255f, 221f / 255f, 148f / 255f);
        private static readonly Color Nebula = new Color(140f / 255f, 120f / 255f, 255f / 255f);
        private static readonly Color Gold = new Color(255f / 255f, 201f / 255f, 74f / 255f);
        private static readonly Color Silver = new Color(214f / 255f, 222f / 255f, 240f / 255f);
        private static readonly Color Copper = new Color(230f / 255f, 150f / 255f, 90f / 255f);
        private static readonly Color Blue = new Color(127f / 255f, 170f / 255f, 255f / 255f);

        public static Color TierColor(PuntaTier tier)
        {
            switch (tier)
            {
                case PuntaTier.Solo: return Gold;
                case PuntaTier.Pista: return Silver;
                case PuntaTier.Letras: return Copper;
                default: return Blue;
            }
        }

        private enum Phase { Idle, Think, Build, Solved, Done }

        private enum IntentKind { None, Have, Help, Clear, Tile, Next }

        private struct Intent
        {
            public IntentKind Kind;
            public Tile Tile;
        }

        // ------------------------------------------------------------------ piezas

        private sealed class Tile
        {
            public RectTransform Root;
            public Image Shadow, Glow, Face, Shine, Rim;
            public Text Label;
            public char Char;
            public Vector2 Home, Pos, Target;
            public int Slot = -1;
            public bool Locked, Gone, Lit;
            public float Born, Phase, Fade, ScaleNow;
            public Color LitColor;
        }

        private sealed class Lucero
        {
            public RectTransform Root;
            public Image Ring, Glow, Core, Shine;
            public bool Filled;
            public Vector2 Pos;
            public PuntaTier Tier;
        }

        private sealed class Flyer
        {
            public RectTransform Root;
            public Image Glow, Core, Shine;
            public Vector2 From;
            public float Born;
            public int Index;
            public PuntaTier Tier;
            public bool Alive;
        }

        private sealed class Particle { public Image Img; public Vector2 Pos, Vel; public float Age, Life, Size; public bool Alive; public Color Color; }

        private sealed class ButtonView
        {
            public RectTransform Root;
            public Image Bg;
            public Text Label;
            public Rect Rect;           // lógico (dp)
            public bool Visible, Primary;
            public IntentKind Kind;
            public float PressedAt = -10f;
            public CanvasGroup Group;
        }

        // ------------------------------------------------------------------ estado

        private System.Random _rng;
        private AdaptiveDifficulty _dda;
        private PuntaBank _bank;
        private PuntaDirector _director;
        private PuntaCredit _credit;
        private PuntaTally _tally;
        private List<string> _pending = new List<string>();
        private Phase _phase = Phase.Idle;
        private bool Endless => _config != null && _config.config.timed;
        private bool Precision => !Endless;
        private bool _senior;
        private int _previousFrameRate;
        private float _endsAt;
        private int _lastTickSecond = -1;
        private int _streak, _bestStreak, _points, _resolved;
        private bool _loopOn, _guided;
        // Tutorial «aprender haciendo»: en cada paso solo responde LO QUE SE PIDE (un botón, la tarjeta o una ficha) y lo demás queda atenuado
        private bool _strict;
        private IntentKind _allowKind = IntentKind.None;
        private Tile _allowTile, _hintTile;

        // palabra en curso
        private PuntaWord _word;
        private bool _isBlue;
        private int _helps, _tries;
        private bool _built;
        private int _maxTiles, _extra;
        private float _cardAt, _typedDoneAt, _helpAt, _shakeAt, _validateAt, _haveAt;
        private PuntaTier _outcome;
        private bool _inputOn;
        private Vector2? _press;
        private int _typedCount = -1;
        private readonly List<Tile> _tiles = new List<Tile>();
        private Tile[] _slots = new Tile[0];
        private Tile _ghost;
        private PuntaLayout.Metrics _m;
        private float _reserve, _guidedGap;

        // escala y disposición
        private float _s = 3f, _playW, _playH, _logicalH;

        // UI
        private RectTransform _safe, _play, _nebulaLayer, _skyLayer, _cardLayer, _slotLayer, _tileLayer, _fxLayer, _uiLayer, _timerTrack, _timerHead;
        private Image _timerFill;
        private RectTransform _card, _cardShadow, _cardBorder, _cardFill;
        private CanvasGroup _cardGroup;
        private Image _antennaDot;
        private readonly Image[] _waves = new Image[3];
        private Text _cardLabel, _defText, _askText, _sayText;
        private readonly List<Image> _nebulaBlobs = new List<Image>();
        private readonly List<Image> _slotBorders = new List<Image>();
        private readonly List<Image> _slotFills = new List<Image>();
        private readonly List<Lucero> _luceros = new List<Lucero>();
        private readonly List<Flyer> _flyers = new List<Flyer>();
        private readonly List<Particle> _particles = new List<Particle>();
        private readonly ButtonView[] _buttons = new ButtonView[2];
        private Text _ladderLabel;
        private readonly Image[] _ladderDots = new Image[3];
        private Image _hintRing;
        private Toast _toast;
        private ExitButton _exit;
        private GameHud _hud;
        private CountdownScreen _countdown;
        private float _sayAt = -10f;
        private bool _sayGood;
        private readonly List<KeyValuePair<Text, float>> _fonts = new List<KeyValuePair<Text, float>>();

        // ------------------------------------------------------------------ sesión

        public void StartSession(SequenceInitConfig config)
        {
            _config = config;
            _rng = new System.Random();
            var age = DdaUserProfileConfig.ParseAgeBand(config.config.age_band);
            _senior = age == AgeBand.Senior;
            float start = AdaptiveDifficulty.StartRating(config.config, PuntaContract.MaxLevel);
            // 8 palabras por partida: pasos grandes (como Satélites, Cosecha y La estrella intrusa). Sin tiempo de reacción: pensar con calma no se penaliza.
            _dda = new AdaptiveDifficulty(PuntaContract.MaxLevel, age, start, stepUp: 0.4f, useReaction: false);
            _bank = PuntaBank.Load();
            string recent = "";
            try { recent = PlayerPrefs.GetString(RecentKey, ""); } catch (System.Exception) { }
            _pending = PuntaContract.ParseWords(config.config.punta_pending);
            _director = new PuntaDirector(_bank, _rng, PuntaContract.ParseWords(recent), _pending, Precision);
            _credit = new PuntaCredit();
            _tally = new PuntaTally(_pending);

            _previousFrameRate = Application.targetFrameRate;
            Application.targetFrameRate = 60;

            _phase = Phase.Idle;
            _loopOn = _guided = false;
            _inputOn = false;
            _press = null;
            _strict = false;
            _allowKind = IntentKind.None;
            _allowTile = _hintTile = null;
            _streak = _bestStreak = _points = _resolved = 0;
            _endsAt = 0f;
            _lastTickSecond = -1;
            _reserve = _guidedGap = 0f;
            _validateAt = 0f;
            ClearWordVisuals();
            ResetSky();

            _resultRoot.gameObject.SetActive(false);
            _exit.Hide();
            _timerTrack.gameObject.SetActive(Endless);
            _hud.SetStreak(0);
            _tutorial.Hide();
            HideHint();
            HideButtons();

            StopAllCoroutines();
            StartCoroutine(GameLoop());
        }

        private void OnDisable()
        {
            if (_previousFrameRate != 0) Application.targetFrameRate = _previousFrameRate;
        }

        private IEnumerator GameLoop()
        {
            StartCoroutine(Prewarm());
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
                ClearWordVisuals();
                _reserve = 0f;
            }
            _safe.gameObject.SetActive(false);
            yield return StartCoroutine(_countdown.Play("En la punta de la lengua", Assessment.Subtitle("Encuentra la palabra"), () => _safe.gameObject.SetActive(true)));
            _safe.gameObject.SetActive(true);
            yield return null;
            ApplySafeArea(_safe);
            Canvas.ForceUpdateCanvases();
            Layout();
            UpdateHud();

            _endsAt = GameClock.Time + PuntaContract.RetoSeconds;
            _loopOn = true;
            yield return StartCoroutine(MainLoop());
        }

        /// <summary>El bucle de la partida: una palabra tras otra hasta que se acabe el tiempo (Reto) o las 8 (Precisión). «Cómo se juega» lo retoma desde acá.</summary>
        private IEnumerator MainLoop()
        {
            while (!Finished()) yield return StartCoroutine(PlayWord());
            _loopOn = false;
            yield return StartCoroutine(FinishGame());
        }

        private bool Finished()
        {
            if (Endless) return GameClock.Time >= _endsAt;
            return _resolved >= PuntaContract.PrecisionWords;
        }

        /// <summary>Sintetiza los sonidos y hornea los sprites durante la cuenta regresiva (así la primera palabra no traba).</summary>
        private IEnumerator Prewarm()
        {
            DiscSprite.Get();
            RingSprite.Get();
            RadialGlowSprite.Get();
            yield return null;
            for (int k = 0; k < 6; k++) PuntaSounds.Note(k);
            PuntaSounds.Appear();
            PuntaSounds.Help();
            yield return null;
            for (int k = 6; k < PuntaSounds.Penta.Length; k++) PuntaSounds.Note(k);
            PuntaSounds.Back();
            PuntaSounds.Thud();
            yield return null;
            PuntaSounds.Chord();
            PuntaSounds.Chime();
            PuntaSounds.Finale();
        }

        // ------------------------------------------------------------------ una palabra

        private IEnumerator PlayWord()
        {
            int level = _dda.PresentedLevel;
            _word = _director.Next(level, _resolved, out _isBlue);
            BeginWord(_word, level);
            yield return StartCoroutine(WordLoop(abortAtTimeUp: Endless));
            if (_phase != Phase.Solved)
            {
                // se acabó el tiempo a mitad de palabra: no cuenta
                yield return StartCoroutine(FadeOutWord());
                _phase = Phase.Idle;
                yield break;
            }

            var tier = _outcome;
            int thinkMs = tier == PuntaTier.Solo && _haveAt > 0f ? Mathf.Max(0, Mathf.RoundToInt((_haveAt - _typedDoneAt) * 1000f)) : -1;
            _tally.Add(_word.Word, tier, thinkMs);
            _resolved++;
            bool hit = _credit.CountsAsHit(tier);
            var change = _dda.Register(hit);
            bool streakHit = tier == PuntaTier.Solo || tier == PuntaTier.Pista;
            _streak = streakHit ? _streak + 1 : 0;
            _bestStreak = Mathf.Max(_bestStreak, _streak);
            _points += PuntaContract.PointsFor(tier, _streak);
            _hud.SetStreak(_streak);
            UpdateHud();
            if (change == DdaChange.Up)
            {
                _toast.Show("¡Subes de nivel!", "Palabras menos comunes y más largas", NeuroStyle.Lime, 1.1f);
                GameFeel.LevelUp();
            }
            else if (change == DdaChange.Down || _dda.Struggling)
                _toast.Show("Con calma", "Pide una ayuda cuando la necesites", NeuroStyle.Sun, 0.9f);

            yield return StartCoroutine(PlaySolved(tier, _word, _tally.Total - 1, count: true));
        }

        /// <summary>Prepara la tarjeta y los lugares para una palabra nueva.</summary>
        private void BeginWord(PuntaWord word, int level)
        {
            ClearWordVisuals();
            _word = word;
            _helps = _tries = 0;
            _built = false;
            _haveAt = 0f;
            _validateAt = 0f;
            _outcome = PuntaTier.Solo;
            int n = word.Tiles.Length;
            _extra = PuntaContract.ExtraLetters(level, _senior);
            _maxTiles = n + _extra;
            _m = PuntaLayout.Compute(_logicalH - _reserve, _maxTiles, _guidedGap);
            _slots = new Tile[n];
            ApplyMetrics(n);
            _cardAt = GameClock.Time;
            _typedDoneAt = _cardAt + PuntaContract.TypeSeconds(word.Clue.Length, GameFeel.ReduceMotion || _guided);
            _typedCount = -1;
            _defText.text = "";
            _helpAt = 0f;
            _phase = Phase.Think;
            SetButtonsFor(Phase.Think, true);
            _inputOn = true;
            _press = null;
            UpdateHud();
        }

        /// <summary>Lee toques y los resuelve hasta que la palabra queda resuelta (<see cref="Phase.Solved"/>) o, si se pide, se acaba el tiempo.</summary>
        private IEnumerator WordLoop(bool abortAtTimeUp)
        {
            while (_phase == Phase.Think || _phase == Phase.Build)
            {
                if (abortAtTimeUp && GameClock.Time >= _endsAt) { _inputOn = false; yield break; }
                if (_tutorial != null && _guided && _tutorial.Skipped) { _inputOn = false; yield break; }
                if (_validateAt > 0f && GameClock.Time >= _validateAt)
                {
                    _validateAt = 0f;
                    if (AllFilled()) yield return StartCoroutine(Validate());
                }
                else
                {
                    var intent = ReadIntent();
                    if (intent.Kind != IntentKind.None) yield return StartCoroutine(Act(intent));
                }
                yield return null;
            }
            _inputOn = false;
        }

        private IEnumerator Act(Intent intent)
        {
            switch (intent.Kind)
            {
                case IntentKind.Have:
                    if (_phase != Phase.Think) break;
                    _haveAt = GameClock.Time;
                    ShowTiles(withExtra: true);
                    break;
                case IntentKind.Help:
                    if (_phase == Phase.Build && _helps >= PuntaContract.HelpSteps) yield return StartCoroutine(Reveal());
                    else DoHelp();
                    break;
                case IntentKind.Clear:
                    if (_phase == Phase.Build) ClearAll(true);
                    break;
                case IntentKind.Tile:
                    TapTile(intent.Tile);
                    break;
            }
        }

        // ------------------------------------------------------------------ fichas

        private void ShowTiles(bool withExtra)
        {
            var chars = withExtra ? PuntaContract.BuildTiles(_word.Tiles, _extra, _rng) : PuntaContract.OnlyLetters(_word.Tiles, _rng);
            float now = GameClock.Time;
            Vector2 from = new Vector2(PuntaLayout.W / 2f, _m.SlotY);
            for (int k = 0; k < chars.Length; k++)
            {
                var t = MakeTile(chars[k]);
                t.Home = PuntaLayout.BankHome(_m, k, chars.Length);
                t.Pos = Motion.Decorative ? from : t.Home;
                t.Target = t.Home;
                t.Born = now + (Motion.Decorative ? k * 0.045f : 0f);
                t.Phase = (float)_rng.NextDouble() * 6.28f;
                _tiles.Add(t);
            }
            if (_ghost != null) { Destroy(_ghost.Root.gameObject); _ghost = null; }
            _built = true;
            _phase = Phase.Build;
            // la primera letra que dio la ayuda queda fija, en su casilla
            if (_helps >= 2)
            {
                var f = _tiles.Find(t => t.Char == _word.Tiles[0]);
                if (f != null) { f.Pos = PuntaLayout.SlotPos(_m, 0, _word.Tiles.Length); f.Born = now; PlaceTile(f, 0, true); }
            }
            SetButtonsFor(Phase.Build, true);
            PlayClip(PuntaSounds.Appear(), 0.6f);
        }

        private bool AllFilled()
        {
            foreach (var s in _slots) if (s == null) return false;
            return _slots.Length > 0;
        }

        private int FirstEmptySlot()
        {
            for (int k = 0; k < _slots.Length; k++) if (_slots[k] == null) return k;
            return -1;
        }

        private void PlaceTile(Tile t, int k, bool locked = false)
        {
            t.Slot = k;
            t.Locked = locked;
            t.Target = PuntaLayout.SlotPos(_m, k, _slots.Length);
            _slots[k] = t;
            RefreshTileLook(t);
        }

        private void Unplace(Tile t)
        {
            if (t.Locked || t.Slot < 0) return;
            _slots[t.Slot] = null;
            t.Slot = -1;
            t.Target = t.Home;
            RefreshTileLook(t);
        }

        private void TapTile(Tile t)
        {
            if (t == null || _phase != Phase.Build || t.Gone) return;
            if (t.Slot >= 0)
            {
                if (t.Locked) { StartCoroutine(PopRect(t.Root, 1.12f, 0.18f)); return; }
                Unplace(t);
                _validateAt = 0f;
                PlayClip(PuntaSounds.Back(), 0.5f);
                return;
            }
            int k = FirstEmptySlot();
            if (k < 0) return;
            PlaceTile(t, k);
            PlayClip(PuntaSounds.Note(Mathf.Min(PuntaSounds.Penta.Length - 1, k)), 0.8f);
            if (AllFilled()) _validateAt = GameClock.Time + 0.26f;
        }

        private void ClearAll(bool sound)
        {
            bool any = false;
            foreach (var t in _tiles) if (t.Slot >= 0 && !t.Locked) { Unplace(t); any = true; }
            _validateAt = 0f;
            if (any && sound) PlayClip(PuntaSounds.Back(), 0.5f);
        }

        private string FormedTiles()
        {
            var sb = new System.Text.StringBuilder();
            foreach (var s in _slots) sb.Append(s != null ? s.Char : '?');
            return sb.ToString();
        }

        private IEnumerator Validate()
        {
            _inputOn = false;
            if (PuntaContract.IsAccepted(FormedTiles(), _word.Tiles))
            {
                _outcome = PuntaContract.TierOf(_helps, false);
                _phase = Phase.Solved;
                yield break;
            }
            _tries++;
            PlayClip(PuntaSounds.Thud(), 0.8f);
            GameFeel.Haptic(GameFeel.HapticKind.Light);
            _shakeAt = GameClock.Time;
            if (_tries >= 2)
            {
                Tell("Esta se escondió bien. No pasa nada: Nubi te la muestra.", false);
                yield return Motion.Hold(0.65f);
                yield return StartCoroutine(Reveal());
                yield break;
            }
            Tell("Casi. Las letras vuelven: prueba otro orden.", false);
            yield return Motion.Hold(0.52f);
            ClearAll(true);
            _inputOn = true;
            _press = null;
        }

        /// <summary>Las letras van solas a su lugar: la palabra queda mostrada (lucero azul). Si aún no habían salido las fichas, salen solo las de la palabra.</summary>
        private IEnumerator Reveal()
        {
            _inputOn = false;
            _validateAt = 0f;
            if (!_built) ShowTiles(withExtra: false);
            foreach (var t in _tiles) if (t.Slot >= 0 && !t.Locked) { _slots[t.Slot] = null; t.Slot = -1; RefreshTileLook(t); }
            var used = new HashSet<Tile>();
            foreach (var s in _slots) if (s != null) used.Add(s);
            for (int k = 0; k < _word.Tiles.Length; k++)
            {
                if (_slots[k] != null) continue;
                var pick = _tiles.Find(t => t.Char == _word.Tiles[k] && !used.Contains(t));
                if (pick == null) continue;
                used.Add(pick);
                PlaceTile(pick, k);
            }
            foreach (var t in _tiles) if (t.Slot < 0) t.Gone = true;
            _outcome = PuntaTier.Vista;
            _phase = Phase.Solved;
            yield break;
        }

        // ------------------------------------------------------------------ ayudas

        private void DoHelp()
        {
            if (_phase != Phase.Think && _phase != Phase.Build) return;
            if (_helps >= PuntaContract.HelpSteps) return;
            _helps++;
            _helpAt = GameClock.Time;
            PlayClip(PuntaSounds.Help(), 0.7f);
            int n = _word.Tiles.Length;
            if (_helps == 1)
            {
                Say("Esta palabra tiene " + n + " letras.", true);
            }
            else if (_helps == 2)
            {
                Tell("Empieza con «" + _word.Tiles[0] + "».", true);
                if (_built)
                {
                    var current = _slots.Length > 0 ? _slots[0] : null;
                    if (current != null && !current.Locked) Unplace(current);
                    var f = _tiles.Find(t => t.Char == _word.Tiles[0] && t.Slot < 0 && !t.Gone) ?? _tiles.Find(t => t.Char == _word.Tiles[0] && !t.Gone && !t.Locked);
                    if (f != null)
                    {
                        if (f.Slot >= 0) Unplace(f);
                        PlaceTile(f, 0, true);
                    }
                    _validateAt = 0f;
                }
                else MakeGhost();
            }
            else
            {
                Tell("Solo sus letras: ordénalas.", true);
                if (_built)
                {
                    ClearAll(false);
                    var need = new List<char>(_word.Tiles.ToCharArray());
                    foreach (var t in _tiles)
                    {
                        int idx = need.IndexOf(t.Char);
                        if (idx >= 0 && !t.Gone) need.RemoveAt(idx);
                        else if (t.Slot < 0) t.Gone = true;
                    }
                }
                else ShowTiles(withExtra: false);
            }
            if (_phase == Phase.Think || _phase == Phase.Build) SetButtonsFor(_phase, false);
        }

        private void MakeGhost()
        {
            if (_ghost != null) return;
            _ghost = MakeTile(_word.Tiles[0]);
            _ghost.Locked = true;
            _ghost.Pos = _ghost.Target = PuntaLayout.SlotPos(_m, 0, _word.Tiles.Length);
            _ghost.Born = GameClock.Time;
            _ghost.Slot = 0;
            RefreshTileLook(_ghost);
        }

        // ------------------------------------------------------------------ resolver una palabra

        /// <summary>Cada letra se enciende en orden con su nota, suena un acorde (salvo si la mostró Nubi), y la palabra se vuelve un lucero que vuela al cielo.</summary>
        private IEnumerator PlaySolved(PuntaTier tier, PuntaWord word, int skyIndex, bool count)
        {
            _phase = Phase.Solved;
            _inputOn = false;
            SetButtonsFor(Phase.Solved, false);
            var color = TierColor(tier);
            int n = word.Tiles.Length;
            bool calm = !Motion.Decorative;
            for (int k = 0; k < n; k++)
            {
                var t = _slots.Length > k ? _slots[k] : null;
                if (t != null)
                {
                    StartCoroutine(LightUp(t, k, color, calm ? 0f : k * 0.09f));
                }
            }
            float tail = calm ? 0.3f : n * 0.09f + 0.5f;
            if (tier != PuntaTier.Vista) StartCoroutine(ChordAfter(Mathf.Max(0f, tail - 0.2f)));
            if (tier != PuntaTier.Vista) GameFeel.Haptic(GameFeel.HapticKind.Light);
            Say(tier == PuntaTier.Solo ? "¡La encontraste sola!" : tier == PuntaTier.Vista ? "Era «" + word.Word + "». Volverá otro día." : "¡Estaba en la punta de la lengua!", tier != PuntaTier.Vista);
            yield return Motion.Hold(tail);
            if (count) LaunchFlyer(skyIndex, tier);
            yield return Motion.Hold(1.1f);
            yield return StartCoroutine(FadeOutWord());
            _phase = Phase.Idle;
        }

        private IEnumerator LightUp(Tile t, int k, Color color, float delay)
        {
            if (delay > 0f) yield return Motion.Hold(delay);
            if (t == null || t.Root == null) yield break;
            t.Lit = true;
            t.LitColor = color;
            RefreshTileLook(t);
            PlayClip(PuntaSounds.Note(Mathf.Min(PuntaSounds.Penta.Length - 1, k + 1)), 0.8f);
            Emit(t.Pos, 8, 70f, 0.6f, color, 10f);
        }

        private IEnumerator ChordAfter(float delay)
        {
            if (delay > 0f) yield return Motion.Hold(delay);
            PlayClip(PuntaSounds.Chord(), 0.8f);
        }

        private IEnumerator FadeOutWord()
        {
            float t = 0f;
            const float seconds = 0.2f;
            while (t < seconds)
            {
                t += GameClock.DeltaTime;
                float k = Mathf.Clamp01(t / seconds);
                foreach (var tile in _tiles) tile.Fade = k;
                yield return null;
            }
            ClearWordVisuals();
        }

        // ------------------------------------------------------------------ fin

        private IEnumerator FinishGame()
        {
            _phase = Phase.Done;
            _inputOn = false;
            ClearWordVisuals();
            try
            {
                PlayerPrefs.SetString(RecentKey, PuntaContract.PushRecent(PlayerPrefs.GetString(RecentKey, ""), _director.UsedInOrder));
                PlayerPrefs.Save();
            }
            catch (System.Exception) { }

            int score = PuntaContract.Score(_tally, Endless);
            ShowResult(score);
            PlayClip(PuntaSounds.Finale(), 0.8f);

            var telemetry = new StroopTelemetry
            {
                user_id = _config.user_id,
                game_id = _config.game_id,
                session_metrics = new StroopSessionMetrics
                {
                    correct_trials = _tally.Found,
                    total_trials = _tally.Total,
                    calculated_score = score,
                    average_response_time_ms = _tally.MeanSoloMs < 0 ? 0 : _tally.MeanSoloMs,
                    level = _config.config.level,
                    timed = _config.config.timed,
                    end_rating = _dda.RatingNormalized,
                    mode_trials = _dda.ScoredTrials,
                    mode_hits = _dda.ScoredCorrect,
                    peak_level = _dda.PeakLevel,
                    punta_solo = _tally.Solo,
                    punta_pista = _tally.Pista,
                    punta_letras = _tally.Letras,
                    punta_vista = _tally.Vista,
                    punta_ms = _tally.MeanSoloMs,
                    punta_words = _tally.WordsCsv(),
                    punta_blue = _tally.BlueCsv(),
                    punta_cleared = _tally.ClearedCsv()
                }
            };
            NativeBridge.ForwardTelemetryToPlatform(JsonUtility.ToJson(telemetry));
            yield break;
        }

        private void ShowResult(int score)
        {
            _exit.Show();
            SetButtonsFor(Phase.Done, false);
            _resultRoot.Find("Title").GetComponent<Text>().text = "Tu cielo de palabras";
            _resultRoot.Find("Detail").GetComponent<Text>().text = $"Encontraste {_tally.Solo} de {_tally.Total} por tu cuenta";
            _resultRoot.Find("Extra").GetComponent<Text>().text = _tally.Vista > 0 ? "Las azules vuelven en otra partida." : "Cada palabra es un lucero de tu cielo.";
            _resultRoot.gameObject.SetActive(true);
            StartCoroutine(AnimateResult(score));
        }

        private void UpdateHud()
        {
            _hud.SetLevel(_dda != null ? _dda.PresentedLevel : 1);
            if (Endless) _hud.SetPoints(_points);
            else _hud.SetInfo($"{Mathf.Min(_resolved + 1, PuntaContract.PrecisionWords)} de {PuntaContract.PrecisionWords}");
        }

        // ------------------------------------------------------------------ «Cómo se juega» desde la pausa

        protected override bool HowToReady => _loopOn && _phase != Phase.Done;

        protected override void HowToSuspend()
        {
            _phase = Phase.Idle;
            _inputOn = false;
            _strict = false;
            _allowKind = IntentKind.None;
            _allowTile = _hintTile = null;
            _toast.Hide();
            ClearWordVisuals();
        }

        protected override void HowToResume(float spentSeconds)
        {
            _endsAt = HowToClock.Shift(_endsAt, spentSeconds);       // el tiempo que duró «Cómo se juega» no se le descuenta al Reto
            ClearWordVisuals();
            _reserve = 0f;
            StartCoroutine(MainLoop());
        }

        // ------------------------------------------------------------------ ronda guiada del tutorial (pieza común)

        // <guided>
        protected override IEnumerator GuidedRound(GuidedTutorial t)
        {
            t.BeginPractice();
            _guided = true;
            _reserve = TutorialBottomReserve;
            _guidedGap = TutorialCaptionGap;
            SetSkyVisible(false);
            var script = new GuidedScript(2);
            // el mensaje de Nubi va en el aire que se deja entre la tarjeta y las casillas: no tapa la tarjeta, las fichas ni los botones, y queda puesto hasta el siguiente
            float captionY = _m.CardBottom + 25f + _guidedGap / 2f;
            t.PlaceControls(GameHud.Height + 10f, false, (_logicalH - captionY) * _s);

            // Aprender haciendo: Nubi NO juega sola. Cada paso espera que la persona toque lo que se le pide (el aro marca solo eso, lo demás queda atenuado y no
            // responde) y nada avanza por tiempo. La definición aparece entera desde el principio para poder leerla.

            // ---- PALABRA 1: sola
            var w1 = GuidedWord(1, 5);
            BeginWord(w1, 1);
            _strict = true;
            // 1) leer la definición
            ConfigureButton(_buttons[0], "Ya la leí", new Rect(90f, _m.ButtonsTop, 180f, PuntaLayout.ButtonH), true, IntentKind.Next);
            _buttons[1].Visible = false;
            _buttons[1].Root.gameObject.SetActive(false);
            t.Say("Esta es una definición. Léela con calma.");
            _allowKind = IntentKind.Next;
            PlaceHintOn(0);
            yield return StartCoroutine(WaitIntent(t, IntentKind.Next));
            // 2) «¡La tengo!»
            if (!t.Skipped)
            {
                SetButtonsFor(Phase.Think, false);
                t.Say("La palabra es fácil: ¿la sabes? Toca «¡La tengo!».");
                _allowKind = IntentKind.Have;
                PlaceHintOn(0);
                yield return StartCoroutine(WaitIntent(t, IntentKind.Have));
                HideHint();
            }
            // 3) las letras, en orden
            if (!t.Skipped)
            {
                _haveAt = GameClock.Time;
                ShowTiles(withExtra: true);
                _allowKind = IntentKind.None;
                yield return Motion.Hold(0.6f);                       // las fichas terminan de aparecer
                for (int k = 0; k < w1.Tiles.Length && !t.Skipped; k++)
                {
                    var tile = _tiles.Find(x => x.Char == w1.Tiles[k] && x.Slot < 0 && !x.Gone);
                    if (tile == null) continue;
                    t.Say(k == 0 ? "Ahora toca las letras en orden. Empieza por la «" + w1.Tiles[k] + "»." : "Sigue con la «" + w1.Tiles[k] + "».");
                    yield return StartCoroutine(WaitTile(t, tile));
                }
            }
            // 4) celebración y «lucero dorado»
            if (!t.Skipped)
            {
                _allowTile = null;
                HideHint();
                yield return Motion.Hold(0.3f);
                yield return StartCoroutine(Validate());
                t.Say("¡Muy bien!");
                yield return StartCoroutine(PlaySolved(PuntaTier.Solo, w1, 0, count: false));
                t.Say("Cuando la encuentras sola, el lucero es dorado. Toca para seguir.");
                yield return StartCoroutine(WaitTap(t));
                script.Success();
            }
            else script.Skip();

            // ---- PALABRA 2: con ayuda
            if (!script.Finished)
            {
                var w2 = GuidedWord(1, 5, w1.Word);
                BeginWord(w2, 1);
                _strict = true;
                _allowKind = IntentKind.Help;
                t.Say("Si una palabra no te sale, pide ayuda. Toca «Una ayuda».");
                PlaceHintOn(1);
                yield return StartCoroutine(WaitIntent(t, IntentKind.Help));
                if (!t.Skipped)
                {
                    DoHelp();                                              // 5) cuántas letras tiene
                    t.Say("Ahora sabes cuántas letras tiene. Toca otra vez «Una ayuda».");
                    PlaceHintOn(1);
                    yield return StartCoroutine(WaitIntent(t, IntentKind.Help));
                }
                if (!t.Skipped)
                {
                    DoHelp();                                              // 6) la primera letra
                    t.Say("Con eso sale. Toca «¡La tengo!» y arma la palabra.");
                    _allowKind = IntentKind.Have;
                    PlaceHintOn(0);
                    yield return StartCoroutine(WaitIntent(t, IntentKind.Have));
                    HideHint();
                }
                if (!t.Skipped)
                {
                    // 7) armarla: libre; el aro solo aparece en la ficha que toca si se queda quieta 4 s
                    _haveAt = GameClock.Time;
                    ShowTiles(withExtra: true);
                    _strict = false;
                    _allowKind = IntentKind.None;
                    float lastAct = GameClock.Time;
                    while ((_phase == Phase.Build) && !t.Skipped)
                    {
                        if (_validateAt > 0f && GameClock.Time >= _validateAt)
                        {
                            _validateAt = 0f;
                            if (AllFilled()) yield return StartCoroutine(Validate());
                            lastAct = GameClock.Time;
                        }
                        else
                        {
                            var intent = ReadIntent();
                            if (intent.Kind != IntentKind.None)
                            {
                                lastAct = GameClock.Time;
                                HideHint();
                                yield return StartCoroutine(Act(intent));
                            }
                        }
                        if (GameClock.Time - lastAct > 4f && _hintTile == null && _phase == Phase.Build)
                        {
                            int k = FirstEmptySlot();
                            var next = k < 0 ? null : _tiles.Find(x => x.Char == w2.Tiles[k] && x.Slot < 0 && !x.Gone);
                            if (next != null) PlaceHintOnTile(next);
                        }
                        yield return null;
                    }
                    HideHint();
                }
                // 8) cierre
                if (!t.Skipped && _phase == Phase.Solved)
                {
                    t.Say("¡Muy bien!");
                    yield return StartCoroutine(PlaySolved(_outcome, w2, 0, count: false));
                    t.Say(_outcome == PuntaTier.Vista
                        ? "No pasa nada: Nubi te la muestra y la palabra vuelve otro día. Toca para terminar."
                        : "Con ayuda, el lucero es plateado. Siempre hay salida: si no sale, Nubi te la muestra. Toca para terminar.");
                    yield return StartCoroutine(WaitTap(t));
                    script.Success();
                }
                else script.Skip();
            }
            HideHint();
            _inputOn = false;
            _strict = false;
            _allowKind = IntentKind.None;
            _allowTile = null;
            ClearWordVisuals();
            _guided = false;
            _reserve = _guidedGap = 0f;
            SetSkyVisible(true);
            _phase = Phase.Idle;
            _toast.Hide();
            t.EndPractice();
        }
        // </guided>

        /// <summary>Espera EXACTAMENTE ese toque (un botón o, con <see cref="IntentKind.Next"/>, la tarjeta): lo demás está atenuado y no responde.</summary>
        private IEnumerator WaitIntent(GuidedTutorial t, IntentKind kind)
        {
            _press = null;
            _inputOn = true;
            while (!t.Skipped)
            {
                var intent = ReadIntent();
                if (intent.Kind == kind) break;
                yield return null;
            }
            _inputOn = false;
        }

        /// <summary>Espera que se toque ESA ficha (el aro la marca y las demás quedan atenuadas). Si se toca otra, rebota suave y vuelve: el aro sigue en la correcta.</summary>
        private IEnumerator WaitTile(GuidedTutorial t, Tile target)
        {
            _allowTile = target;
            PlaceHintOnTile(target);
            _press = null;
            _inputOn = true;
            while (!t.Skipped)
            {
                var intent = ReadIntent();
                if (intent.Kind == IntentKind.Tile)
                {
                    if (intent.Tile == target)
                    {
                        TapTile(target);
                        break;
                    }
                    PlayClip(PuntaSounds.Back(), 0.5f);
                    yield return StartCoroutine(PopRect(intent.Tile.Root, 1.18f, 0.25f));   // rebota suave: no era esa
                    _press = null;
                }
                yield return null;
            }
            _inputOn = false;
            HideHint();
        }

        /// <summary>Espera un toque en cualquier parte de la pantalla (el de «Saltar tutorial» lo atiende <see cref="GameControllerBase.PollTutorialSkip"/>): los pasos avanzan al ritmo de la persona.</summary>
        private IEnumerator WaitTap(GuidedTutorial t)
        {
            _press = null;
            _inputOn = true;
            float guard = 0f;
            while (!t.Skipped)
            {
                guard += GameClock.RealDeltaTime;
                if (_press.HasValue && guard > 0.3f) break;
                if (guard <= 0.3f) _press = null;
                yield return null;
            }
            _press = null;
            _inputOn = false;
        }

        /// <summary>Una palabra fácil para la ronda guiada: del nivel dado y de pocas letras.</summary>
        private PuntaWord GuidedWord(int level, int maxLetters, string not = null)
        {
            var pool = new List<PuntaWord>();
            foreach (var w in _bank.OfLevel(level)) if (w.Tiles.Length <= maxLetters && w.Tiles.Length >= 4 && w.Word != not) pool.Add(w);
            if (pool.Count == 0) foreach (var w in _bank.OfLevel(level)) if (w.Word != not) pool.Add(w);
            if (pool.Count == 0) pool.AddRange(_bank.All);
            return pool[_rng.Next(pool.Count)];
        }

        /// <summary>El aro sol punteado marca el botón que se toca (0 = el de la izquierda, 1 = el de la derecha).</summary>
        private void PlaceHintOn(int button)
        {
            var b = _buttons[button];
            _hintRing.rectTransform.sizeDelta = new Vector2((b.Rect.width + 14f) * _s, (b.Rect.height + 14f) * _s);
            _hintRing.rectTransform.anchoredPosition = P(b.Rect.center);
            _hintTile = null;
            _hintRing.gameObject.SetActive(true);
        }

        private void HideHint()
        {
            _hintTile = null;
            if (_hintRing != null) _hintRing.gameObject.SetActive(false);
        }

        /// <summary>El aro marca una ficha (la sigue mientras se mueve).</summary>
        private void PlaceHintOnTile(Tile tile)
        {
            _hintTile = tile;
            _hintRing.gameObject.SetActive(true);
        }

        // ------------------------------------------------------------------ entrada y animación por cuadro

        private void Update()
        {
            if (PollTutorialSkip()) return;             // un toque en «Saltar tutorial» no es un toque al juego
            float dt = GameClock.DeltaTime;
            UpdateClock();
            GuidedTutorial.SpinHint(_hintRing);
            AnimateSky(dt);
            AnimateCard();
            AnimateSlots();
            AnimateTiles(dt);
            AnimateFlyers();
            AnimateParticles(dt);
            AnimateUi();
            if (_inputOn && dt > 0f) ReadPress();
        }

        private void ReadPress()
        {
            bool down;
            Vector2 pos;
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
            if (!down) return;
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(_play, pos, null, out var local)) return;
            _press = ToLogical(local);
        }

        /// <summary>Qué quiso hacer el último toque: un botón, o una ficha (la más cercana dentro de su radio de toque).</summary>
        private Intent ReadIntent()
        {
            var none = new Intent { Kind = IntentKind.None };
            if (!_inputOn || !_press.HasValue) return none;
            var p = _press.Value;
            _press = null;
            foreach (var b in _buttons)
            {
                if (!b.Visible) continue;
                if (_strict && b.Kind != _allowKind) continue;       // atenuado: no responde
                var r = new Rect(b.Rect.x - 4f, b.Rect.y - 4f, b.Rect.width + 8f, b.Rect.height + 8f);
                if (r.Contains(p))
                {
                    b.PressedAt = GameClock.Time;
                    return new Intent { Kind = b.Kind };
                }
            }
            // «Ya la leí» también se toca sobre la tarjeta
            if (_strict && _allowKind == IntentKind.Next && p.y >= _m.CardTop && p.y <= _m.CardBottom && p.x >= PuntaLayout.SideMargin && p.x <= PuntaLayout.W - PuntaLayout.SideMargin)
                return new Intent { Kind = IntentKind.Next };
            if (_phase != Phase.Build) return none;
            if (_strict && _allowTile == null) return none;
            Tile best = null;
            float bestD = float.MaxValue;
            foreach (var t in _tiles)
            {
                if (t.Gone || GameClock.Time < t.Born) continue;
                float radius = t.Slot >= 0 ? Mathf.Max(22f, PuntaLayout.SlotSpacing(_slots.Length) / 2f + 2f) : PuntaLayout.TileHitRadius;
                float d = Vector2.Distance(p, t.Pos);
                if (d <= radius && d < bestD) { best = t; bestD = d; }
            }
            return best != null ? new Intent { Kind = IntentKind.Tile, Tile = best } : none;
        }

        private void PressFx(int button) => _buttons[button].PressedAt = GameClock.Time;

        private void UpdateClock()
        {
            if (!Endless || !_loopOn || _endsAt <= 0f || _phase == Phase.Done) return;
            float left = _endsAt - GameClock.Time;
            float frac = Mathf.Clamp01(left / PuntaContract.RetoSeconds);
            _timerFill.rectTransform.anchorMax = new Vector2(frac, 1f);
            _timerHead.anchorMin = _timerHead.anchorMax = new Vector2(frac, 0.5f);
            int whole = Mathf.CeilToInt(left);
            if (whole <= 5 && whole >= 1 && whole != _lastTickSecond)
            {
                _lastTickSecond = whole;
                GameFeel.Tick();
            }
        }

        // ---- el cielo de luceros

        private int SkySlots => Precision ? PuntaContract.PrecisionWords : Mathf.Clamp(_tally != null ? _tally.Total + 1 : 1, 1, _luceros.Count);

        private void AnimateSky(float dt)
        {
            int slots = SkySlots;
            float k = 1f - Mathf.Exp(-10f * dt);
            int current = _tally != null ? _tally.Total : 0;
            for (int i = 0; i < _luceros.Count; i++)
            {
                var l = _luceros[i];
                bool on = i < slots && _skyVisible;
                if (l.Root.gameObject.activeSelf != on) l.Root.gameObject.SetActive(on);
                if (!on) continue;
                var target = PuntaLayout.SkyPos(_m, i, slots);
                l.Pos = l.Pos == Vector2.zero ? target : Vector2.Lerp(l.Pos, target, k);
                l.Root.anchoredPosition = P(l.Pos);
                l.Ring.gameObject.SetActive(!l.Filled);
                if (!l.Filled)
                    l.Ring.color = i == current && _phase != Phase.Done ? NeuroStyle.WithAlpha(Cyan, 0.8f) : new Color(1f, 1f, 1f, 0.18f);
            }
        }

        private bool _skyVisible = true;

        private void SetSkyVisible(bool on) { _skyVisible = on; }

        private void ResetSky()
        {
            foreach (var l in _luceros)
            {
                l.Filled = false;
                l.Pos = Vector2.zero;
                l.Glow.gameObject.SetActive(false);
                l.Core.gameObject.SetActive(false);
                l.Shine.gameObject.SetActive(false);
            }
            foreach (var f in _flyers) { f.Alive = false; f.Root.gameObject.SetActive(false); }
            _skyVisible = true;
        }

        private void FillLucero(int index, PuntaTier tier)
        {
            if (index < 0 || index >= _luceros.Count) return;
            var l = _luceros[index];
            l.Filled = true;
            l.Tier = tier;
            var c = TierColor(tier);
            l.Glow.color = NeuroStyle.WithAlpha(c, 0.6f);
            l.Glow.gameObject.SetActive(Motion.Decorative);
            l.Core.color = c;
            l.Core.gameObject.SetActive(true);
            l.Shine.gameObject.SetActive(true);
        }

        // ---- la tarjeta de transmisión

        private void AnimateCard()
        {
            bool show = _phase == Phase.Think || _phase == Phase.Build || _phase == Phase.Solved;
            if (_card.gameObject.activeSelf != show) _card.gameObject.SetActive(show);
            if (!show || _word == null) return;
            float now = GameClock.Time;
            float k = Motion.Decorative ? UiFx.EaseOutCubic(Mathf.Clamp01((now - _cardAt) / 0.42f)) : 1f;
            _cardGroup.alpha = k;
            _card.anchoredPosition = new Vector2(0f, -(1f - k) * 12f * _s);

            // ondas de la señal: más nítidas con cada ayuda (sin ondas con «quitar animaciones»)
            float clarity = 0.35f + _helps * 0.2f;
            for (int j = 0; j < 3; j++)
            {
                bool on = Motion.Decorative;
                if (_waves[j].gameObject.activeSelf != on) _waves[j].gameObject.SetActive(on);
                if (!on) continue;
                float ph = (now / 0.9f + j / 3f) % 1f;
                float r = 6f + ph * 16f;
                _waves[j].rectTransform.sizeDelta = Vector2.one * (2f * r * _s);
                _waves[j].color = NeuroStyle.WithAlpha(Cyan, (1f - ph) * clarity);
            }

            // la definición se escribe sola; el resto del texto queda invisible para que las líneas no se corran al escribir
            string full = _word.Clue;
            int shown = PuntaContract.TypedCount(now - _cardAt, full.Length, GameFeel.ReduceMotion || _guided);  // en el tutorial el texto aparece entero: hay que poder leerlo
            if (shown != _typedCount)
            {
                _typedCount = shown;
                _defText.text = shown >= full.Length ? full : full.Substring(0, shown) + "<color=#00000000>" + full.Substring(shown) + "</color>";
            }
        }

        // ---- casillas, nebulosa y pregunta

        private void AnimateSlots()
        {
            bool inWord = _phase == Phase.Think || _phase == Phase.Build || _phase == Phase.Solved;
            bool showSlots = inWord && (_helps >= 1 || _built);
            bool showAsk = inWord && !showSlots;
            if (_askText.gameObject.activeSelf != showAsk) _askText.gameObject.SetActive(showAsk);
            float now = GameClock.Time;
            bool nebula = showAsk && Motion.Decorative;
            for (int j = 0; j < _nebulaBlobs.Count; j++)
            {
                var b = _nebulaBlobs[j];
                if (b.gameObject.activeSelf != nebula) b.gameObject.SetActive(nebula);
                if (!nebula) continue;
                float x = PuntaLayout.W / 2f + Mathf.Sin(now / 1.3f + j * 1.7f) * 70f;
                float y = _m.SlotY + Mathf.Cos(now / 1.7f + j) * 10f;
                b.rectTransform.anchoredPosition = P(x, y);
            }
            int n = _word != null ? _word.Tiles.Length : 0;
            float appear = 1f;
            if (_helpAt > 0f && _helps == 1 && !_built) appear = Motion.Decorative ? UiFx.EaseOutCubic(Mathf.Clamp01((now - _helpAt) / 0.6f)) : 1f;
            float shake = Shake(now);
            for (int k = 0; k < _slotBorders.Count; k++)
            {
                bool on = showSlots && k < n;
                if (_slotBorders[k].gameObject.activeSelf != on) { _slotBorders[k].gameObject.SetActive(on); _slotFills[k].gameObject.SetActive(on); }
                if (!on) continue;
                float a = Mathf.Clamp01(appear * n - k);
                var pos = PuntaLayout.SlotPos(_m, k, n);
                float d = Mathf.Min(32f, PuntaLayout.SlotSpacing(n) - 4f);
                var rb = _slotBorders[k].rectTransform;
                rb.anchoredPosition = P(pos.x + shake, pos.y);
                rb.sizeDelta = Vector2.one * ((d + 3f) * _s);
                var rf = _slotFills[k].rectTransform;
                rf.anchoredPosition = rb.anchoredPosition;
                rf.sizeDelta = Vector2.one * (d * _s);
                _slotBorders[k].color = NeuroStyle.WithAlpha(Cyan, 0.35f * a);
                _slotFills[k].color = NeuroStyle.WithAlpha(SlotFill, a);
            }
        }

        private float Shake(float now)
        {
            if (_shakeAt <= 0f || !Motion.Decorative) return 0f;
            float e = now - _shakeAt;
            return e > 0.4f ? 0f : Mathf.Sin(e / 0.03f) * Mathf.Max(0f, 1f - e / 0.4f) * 6f;
        }

        // ---- fichas

        private void AnimateTiles(float dt)
        {
            float now = GameClock.Time;
            float spring = 1f - Mathf.Exp(-(Motion.Decorative ? 13f : 32f) * dt);
            float shake = Shake(now);
            int n = _slots.Length;
            float placedScale = n > 0 ? PuntaLayout.PlacedScale(_m, n) : 1f;
            foreach (var t in _tiles) AnimateTile(t, now, spring, shake, placedScale, true);
            if (_ghost != null) AnimateTile(_ghost, now, spring, shake, placedScale, false);
        }

        private void AnimateTile(Tile t, float now, float spring, float shake, float placedScale, bool bob)
        {
            if (t.Root == null) return;
            bool visible = now >= t.Born && t.Fade < 1f;
            if (t.Root.gameObject.activeSelf != visible) t.Root.gameObject.SetActive(visible);
            if (!visible) return;
            float wob = bob && t.Slot < 0 && Motion.Decorative ? Mathf.Sin(now / 0.8f + t.Phase) * 3f : 0f;
            t.Pos = Vector2.Lerp(t.Pos, t.Target + new Vector2(0f, wob), spring);
            var shown = t.Pos + new Vector2(t.Slot >= 0 ? shake : 0f, 0f);
            t.Root.anchoredPosition = P(shown);
            float born = Motion.Decorative ? UiFx.EaseOutBack(Mathf.Clamp01((now - t.Born) / 0.25f)) : 1f;
            float target = (t.Slot >= 0 ? placedScale : 1f) * born * (t.Gone ? 1f - t.Fade : 1f);
            t.ScaleNow = Mathf.Lerp(t.ScaleNow <= 0f ? target : t.ScaleNow, target, spring);
            if (t.Gone) { t.Fade = Mathf.Min(1f, t.Fade + 0.06f); }
            t.Root.localScale = Vector3.one * Mathf.Max(0.0001f, t.ScaleNow);
            float alpha = Mathf.Clamp01((now - t.Born) / 0.25f) * (1f - t.Fade);
            float dim = _strict && t.Slot < 0 && t != _allowTile ? 0.35f : 1f;     // tutorial: solo la ficha pedida se ve completa
            SetTileAlpha(t, (Motion.Decorative ? alpha : (1f - t.Fade)) * dim);
            if (t.Lit && t.Glow != null)
            {
                bool glow = Motion.Decorative;
                if (t.Glow.gameObject.activeSelf != glow) t.Glow.gameObject.SetActive(glow);
            }
        }

        private static void SetTileAlpha(Tile t, float a)
        {
            var g = t.Root.GetComponent<CanvasGroup>();
            if (g != null) g.alpha = a;
        }

        private Tile MakeTile(char c)
        {
            float d = _m.TileD;
            var t = new Tile { Char = c, ScaleNow = 0f };
            var root = new GameObject("Tile_" + c);
            root.transform.SetParent(_tileLayer, false);
            t.Root = root.AddComponent<RectTransform>();
            t.Root.anchorMin = t.Root.anchorMax = new Vector2(0.5f, 0.5f);
            t.Root.pivot = new Vector2(0.5f, 0.5f);
            t.Root.sizeDelta = new Vector2(d * _s, d * _s);
            root.AddComponent<CanvasGroup>().blocksRaycasts = false;
            t.Glow = MakeImage(t.Root, "Glow", RadialGlowSprite.Get());
            t.Glow.rectTransform.sizeDelta = new Vector2(d * 1.9f * _s, d * 1.9f * _s);
            t.Glow.gameObject.SetActive(false);
            t.Shadow = MakeImage(t.Root, "Shadow", DiscSprite.Get());
            t.Shadow.gameObject.SetActive(true);
            t.Shadow.color = new Color(0f, 0f, 0f, 0.45f);
            t.Shadow.rectTransform.sizeDelta = new Vector2(d * 1.02f * _s, d * 1.02f * _s);
            t.Shadow.rectTransform.anchoredPosition = new Vector2(0f, -3f * _s);
            t.Face = MakeImage(t.Root, "Face", DiscSprite.Get());
            t.Face.gameObject.SetActive(true);
            t.Face.rectTransform.sizeDelta = new Vector2(d * _s, d * _s);
            t.Shine = MakeImage(t.Root, "Shine", RadialGlowSprite.Get());
            t.Shine.gameObject.SetActive(true);
            t.Shine.color = new Color(1f, 1f, 1f, 0.7f);
            t.Shine.rectTransform.sizeDelta = new Vector2(d * 0.62f * _s, d * 0.46f * _s);
            t.Shine.rectTransform.anchoredPosition = new Vector2(-d * 0.13f * _s, d * 0.2f * _s);
            t.Rim = MakeImage(t.Root, "Rim", RingSprite.Get());
            t.Rim.gameObject.SetActive(true);
            t.Rim.color = NeuroStyle.Ink;
            t.Rim.rectTransform.sizeDelta = new Vector2(d * 1.02f * _s, d * 1.02f * _s);
            t.Label = MakeText(t.Root, "Char", Mathf.RoundToInt(d * 0.55f * _s), TextAnchor.MiddleCenter, NeuroStyle.Ink, 0f, 0f);
            t.Label.horizontalOverflow = HorizontalWrapMode.Overflow;
            t.Label.verticalOverflow = VerticalWrapMode.Overflow;
            t.Label.text = c.ToString();
            t.Pos = t.Target = Vector2.zero;
            RefreshTileLook(t);
            return t;
        }

        private void RefreshTileLook(Tile t)
        {
            if (t.Face == null) return;
            t.Face.color = t.Lit ? t.LitColor : (t.Locked ? TileLocked : TileFace);
            if (t.Glow != null) t.Glow.color = NeuroStyle.WithAlpha(t.LitColor, 0.6f);
            if (t.Lit && t.Glow != null) t.Glow.gameObject.SetActive(Motion.Decorative);
        }

        private void ClearWordVisuals()
        {
            foreach (var t in _tiles) if (t.Root != null) Destroy(t.Root.gameObject);
            _tiles.Clear();
            if (_ghost != null && _ghost.Root != null) Destroy(_ghost.Root.gameObject);
            _ghost = null;
            _slots = new Tile[0];
            _built = false;
            _validateAt = 0f;
            _helps = 0;
            _helpAt = 0f;
            _shakeAt = 0f;
            _word = null;
            if (_card != null) _card.gameObject.SetActive(false);
            if (_sayText != null) _sayText.gameObject.SetActive(false);
            if (_askText != null) _askText.gameObject.SetActive(false);
            HideButtons();
            if (_ladderLabel != null) SetLadderVisible(false);
        }

        // ---- lo que se dice, la escalera y los botones

        /// <summary>Un aviso del juego; en la ronda guiada lo dice Nubi (su mensaje queda puesto hasta el siguiente), fuera de ella sale bajo la tarjeta.</summary>
        private void Tell(string text, bool good)
        {
            if (_guided && _tutorial != null && _tutorial.Practicing) _tutorial.Say(text);
            else Say(text, good);
        }

        private void Say(string text, bool good)
        {
            _sayText.text = text;
            _sayGood = good;
            _sayAt = GameClock.Time;
            _sayText.gameObject.SetActive(true);
            // en la ronda guiada, lo que dice Nubi va en su propio mensaje; el aviso del juego se queda quieto
            if (_guided && _tutorial != null && _tutorial.Practicing) _sayText.gameObject.SetActive(false);
        }

        private void AnimateUi()
        {
            float now = GameClock.Time;
            if (_sayText.gameObject.activeSelf)
            {
                float k = (now - _sayAt) / 2.2f;
                if (k >= 1f) _sayText.gameObject.SetActive(false);
                else
                {
                    float a = Mathf.Min(1f, k * 8f, (1f - k) * 4f);
                    _sayText.color = NeuroStyle.WithAlpha(_sayGood ? GoodText : WarnText, Mathf.Clamp01(a));
                }
            }
            bool ladder = _phase == Phase.Think || _phase == Phase.Build;
            SetLadderVisible(ladder);
            if (ladder)
            {
                for (int k = 0; k < 3; k++) _ladderDots[k].color = k < _helps ? Cyan : new Color(1f, 1f, 1f, 0.14f);
            }
            if (_hintTile != null && _hintRing != null && _hintRing.gameObject.activeSelf)
            {
                _hintRing.rectTransform.sizeDelta = Vector2.one * (_m.TileD * 1.7f * _s);
                _hintRing.rectTransform.anchoredPosition = P(_hintTile.Pos);
            }
            foreach (var b in _buttons)
            {
                if (!b.Visible) continue;
                if (b.Group != null) b.Group.alpha = _strict && b.Kind != _allowKind ? 0.35f : 1f;
                float pr = Mathf.Max(0f, 1f - (now - b.PressedAt) / 0.2f);
                b.Root.anchoredPosition = P(b.Rect.center) + new Vector2(0f, -pr * 4f * _s);
            }
        }

        private void SetLadderVisible(bool on)
        {
            if (_ladderLabel.gameObject.activeSelf == on) return;
            _ladderLabel.gameObject.SetActive(on);
            foreach (var d in _ladderDots) d.gameObject.SetActive(on);
        }

        private void HideButtons()
        {
            foreach (var b in _buttons) if (b != null) { b.Visible = false; b.Root.gameObject.SetActive(false); }
        }

        /// <summary>Los botones de cada momento (boceto): pensando = «¡La tengo!» y «Una ayuda»; armando = «Borrar» y «Una ayuda» (después de 3 ayudas, «Ver la palabra»).</summary>
        private void SetButtonsFor(Phase phase, bool pop)
        {
            if (phase != Phase.Think && phase != Phase.Build) { HideButtons(); return; }
            float y = _m.ButtonsTop;
            var b0 = _buttons[0];
            var b1 = _buttons[1];
            if (phase == Phase.Think)
            {
                ConfigureButton(b0, "¡La tengo!", new Rect(24f, y, 184f, PuntaLayout.ButtonH), true, IntentKind.Have);
                ConfigureButton(b1, "Una ayuda", new Rect(220f, y, 116f, PuntaLayout.ButtonH), false, IntentKind.Help);
            }
            else
            {
                ConfigureButton(b0, "Borrar", new Rect(24f, y, 100f, PuntaLayout.ButtonH), false, IntentKind.Clear);
                ConfigureButton(b1, _helps >= PuntaContract.HelpSteps ? "Ver la palabra" : "Una ayuda", new Rect(136f, y, 200f, PuntaLayout.ButtonH), false, IntentKind.Help);
            }
            if (pop)
                foreach (var b in _buttons) StartCoroutine(PopRect(b.Root, 1.06f, 0.2f));
        }

        private void ConfigureButton(ButtonView b, string label, Rect rect, bool primary, IntentKind kind)
        {
            b.Visible = true;
            b.Rect = rect;
            b.Primary = primary;
            b.Kind = kind;
            b.Root.gameObject.SetActive(true);
            b.Root.sizeDelta = new Vector2(rect.width * _s, rect.height * _s);
            b.Root.anchoredPosition = P(rect.center);
            b.Label.text = label;
            b.Label.color = primary ? NeuroStyle.Ink : ButtonText;
            b.Bg.color = primary ? Cyan : ButtonSecondary;
            b.Label.rectTransform.offsetMin = new Vector2(10f, 4f);
            b.Label.rectTransform.offsetMax = new Vector2(-10f, -4f);
        }

        // ---- el lucero vuela al cielo

        private void LaunchFlyer(int skyIndex, PuntaTier tier)
        {
            if (!Motion.Decorative)
            {
                FillLucero(skyIndex, tier);
                PlayClip(PuntaSounds.Chime(), 0.5f);
                return;
            }
            Flyer f = _flyers.Find(x => !x.Alive);
            if (f == null) { FillLucero(skyIndex, tier); return; }
            f.Alive = true;
            f.Index = skyIndex;
            f.Tier = tier;
            f.Born = GameClock.Time;
            f.From = new Vector2(PuntaLayout.W / 2f, _m.SlotY);
            var c = TierColor(tier);
            f.Glow.color = NeuroStyle.WithAlpha(c, 0.6f);
            f.Core.color = c;
            f.Root.gameObject.SetActive(true);
        }

        private void AnimateFlyers()
        {
            float now = GameClock.Time;
            foreach (var f in _flyers)
            {
                if (!f.Alive) continue;
                float k = (now - f.Born) / 0.7f;
                int slots = SkySlots;
                Vector2 to = PuntaLayout.SkyPos(_m, f.Index, Mathf.Max(slots, f.Index + 1));
                if (k >= 1f)
                {
                    f.Alive = false;
                    f.Root.gameObject.SetActive(false);
                    FillLucero(f.Index, f.Tier);
                    PlayClip(PuntaSounds.Chime(), 0.5f);
                    Emit(to, 14, 80f, 0.7f, TierColor(f.Tier), 0f);
                    continue;
                }
                float e = UiFx.EaseOutCubic(Mathf.Clamp01(k));
                var pos = Vector2.Lerp(f.From, to, e) + new Vector2(0f, -Mathf.Sin(Mathf.PI * e) * 40f);
                f.Root.anchoredPosition = P(pos);
                float r = Mathf.Lerp(10f, 7f, e);
                f.Root.sizeDelta = Vector2.one * (2f * r * _s);
                f.Glow.rectTransform.sizeDelta = Vector2.one * (6f * r * _s);
            }
        }

        // ---- chispas

        private void Emit(Vector2 at, int n, float spread, float life, Color color, float up)
        {
            if (!Motion.Decorative) return;
            for (int i = 0; i < n; i++)
            {
                var p = _particles.Find(x => !x.Alive);
                if (p == null) return;
                p.Alive = true;
                p.Pos = at;
                p.Vel = new Vector2(((float)_rng.NextDouble() - 0.5f) * spread, ((float)_rng.NextDouble() - 0.5f) * spread - up);
                p.Age = 0f;
                p.Life = life * (0.6f + (float)_rng.NextDouble() * 0.7f);
                p.Size = 0.8f + (float)_rng.NextDouble() * 1.8f;
                p.Color = color;
                p.Img.gameObject.SetActive(true);
            }
        }

        private void AnimateParticles(float dt)
        {
            foreach (var p in _particles)
            {
                if (!p.Alive) continue;
                p.Age += dt;
                if (p.Age >= p.Life) { p.Alive = false; p.Img.gameObject.SetActive(false); continue; }
                p.Pos += p.Vel * dt;
                float k = p.Age / p.Life;
                p.Img.rectTransform.anchoredPosition = P(p.Pos);
                p.Img.rectTransform.sizeDelta = Vector2.one * (p.Size * 2f * _s);
                p.Img.color = NeuroStyle.WithAlpha(p.Color, 1f - k);
            }
        }

        private void PlayClip(AudioClip clip, float volume)
        {
            if (clip == null || !GameFeel.SoundOn) return;
            _audioSource.PlayOneShot(clip, volume);
        }

        // ------------------------------------------------------------------ construcción de UI

        protected override void BuildUi()
        {
            var canvasGo = new GameObject("PuntaCanvas");
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
            // el cielo quieto de Rastro de luz: aquí lo que se mueve es la tarea
            WorldBackdrop.Build(bgRect, GameWorld.CieloDeCristal);

            var safeGo = new GameObject("SafeAreaContent");
            safeGo.transform.SetParent(canvasGo.transform, false);
            _safe = safeGo.AddComponent<RectTransform>();
            ApplySafeArea(_safe);

            _hud = new GameHud(_safe, "En la punta de la lengua", MarginU, this);
            BuildTimer();

            var playGo = new GameObject("Play");
            playGo.transform.SetParent(_safe, false);
            _play = playGo.AddComponent<RectTransform>();
            Stretch(_play);

            _nebulaLayer = Layer(_play, "Nebula");
            _skyLayer = Layer(_play, "Sky");
            _cardLayer = Layer(_play, "Card");
            _slotLayer = Layer(_play, "Slots");
            _tileLayer = Layer(_play, "Tiles");
            _fxLayer = Layer(_play, "Fx");
            _uiLayer = Layer(_play, "Ui");
            BuildNebula();
            BuildSky();
            BuildCard();
            BuildSlots();
            BuildFx();
            BuildUiPieces();

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
            BuildTutorial(_safe, GameHud.Height + 10f, "En la punta de la lengua", "Lee la definición y encuentra la palabra. Si no sale, pide una ayuda: siempre hay una.");
        }

        private void BuildTimer()
        {
            var go = new GameObject("TimeTrack");
            go.transform.SetParent(_safe, false);
            _timerTrack = go.AddComponent<RectTransform>();
            _timerTrack.anchorMin = _timerTrack.anchorMax = new Vector2(0.5f, 1f);
            _timerTrack.pivot = new Vector2(0.5f, 0.5f);
            _timerTrack.sizeDelta = new Vector2(960f, 8f);
            _timerTrack.anchoredPosition = new Vector2(0f, -(GameHud.Height + 6f));
            var img = go.AddComponent<Image>();
            img.sprite = RoundedRectSprite.Get(4);
            img.type = Image.Type.Sliced;
            img.color = new Color(1f, 1f, 1f, 0.1f);
            img.raycastTarget = false;
            var fillGo = new GameObject("Fill");
            fillGo.transform.SetParent(go.transform, false);
            var fr = fillGo.AddComponent<RectTransform>();
            fr.anchorMin = Vector2.zero;
            fr.anchorMax = Vector2.one;
            fr.offsetMin = fr.offsetMax = Vector2.zero;
            _timerFill = fillGo.AddComponent<Image>();
            _timerFill.sprite = RoundedRectSprite.Get(4);
            _timerFill.type = Image.Type.Sliced;
            _timerFill.color = NeuroStyle.WithAlpha(Cyan, 0.8f);
            _timerFill.raycastTarget = false;
            var head = MakeImage(go.transform, "Head", RadialGlowSprite.Get());
            _timerHead = head.rectTransform;
            _timerHead.anchorMin = _timerHead.anchorMax = new Vector2(1f, 0.5f);
            _timerHead.sizeDelta = new Vector2(70f, 70f);
            head.color = NeuroStyle.WithAlpha(Cyan, 0.7f);
            head.gameObject.SetActive(true);
        }

        private void BuildNebula()
        {
            for (int j = 0; j < 5; j++)
            {
                var b = MakeImage(_nebulaLayer, "Blob" + j, RadialGlowSprite.Get());
                b.color = NeuroStyle.WithAlpha(Nebula, 0.16f);
                b.gameObject.SetActive(false);
                _nebulaBlobs.Add(b);
            }
        }

        private void BuildSky()
        {
            for (int i = 0; i < 16; i++)
            {
                var l = new Lucero();
                var go = new GameObject("Lucero" + i);
                go.transform.SetParent(_skyLayer, false);
                l.Root = go.AddComponent<RectTransform>();
                l.Root.anchorMin = l.Root.anchorMax = l.Root.pivot = new Vector2(0.5f, 0.5f);
                l.Root.sizeDelta = new Vector2(14f * UnitsPerDp, 14f * UnitsPerDp);
                l.Ring = MakeImage(l.Root, "Ring", RingSprite.Get());
                l.Ring.rectTransform.sizeDelta = new Vector2(12f * UnitsPerDp, 12f * UnitsPerDp);
                l.Ring.gameObject.SetActive(true);
                l.Glow = MakeImage(l.Root, "Glow", RadialGlowSprite.Get());
                l.Glow.rectTransform.sizeDelta = new Vector2(42f * UnitsPerDp, 42f * UnitsPerDp);
                l.Core = MakeImage(l.Root, "Core", DiscSprite.Get());
                l.Core.rectTransform.sizeDelta = new Vector2(14f * UnitsPerDp, 14f * UnitsPerDp);
                l.Shine = MakeImage(l.Root, "Shine", RadialGlowSprite.Get());
                l.Shine.rectTransform.sizeDelta = new Vector2(7f * UnitsPerDp, 7f * UnitsPerDp);
                l.Shine.rectTransform.anchoredPosition = new Vector2(-2.1f * UnitsPerDp, 2.4f * UnitsPerDp);
                l.Shine.color = new Color(1f, 1f, 1f, 0.9f);
                go.SetActive(false);
                _luceros.Add(l);
            }
        }

        private void BuildCard()
        {
            var go = new GameObject("CardRoot");
            go.transform.SetParent(_cardLayer, false);
            _card = go.AddComponent<RectTransform>();
            Stretch(_card);
            _cardGroup = go.AddComponent<CanvasGroup>();
            _cardGroup.blocksRaycasts = false;

            _cardShadow = RoundedPanel(_card, "Shadow", new Color(0f, 0f, 0f, 0.4f), 26);
            _cardBorder = RoundedPanel(_card, "Border", NeuroStyle.WithAlpha(Cyan, 0.35f), 26);
            _cardFill = RoundedPanel(_card, "Fill", CardFill, 24);

            _antennaDot = MakeImage(_card, "Dot", DiscSprite.Get());
            _antennaDot.color = Cyan;
            _antennaDot.gameObject.SetActive(true);
            for (int j = 0; j < 3; j++)
            {
                var w = MakeImage(_card, "Wave" + j, RingSprite.Get());
                w.type = Image.Type.Filled;
                w.fillMethod = Image.FillMethod.Radial360;
                w.fillOrigin = (int)Image.Origin360.Top;
                w.fillClockwise = true;
                w.fillAmount = 0.3f;
                w.rectTransform.localEulerAngles = new Vector3(0f, 0f, 54f);
                w.gameObject.SetActive(true);
                _waves[j] = w;
            }
            _cardLabel = MakeLabel(_card, "Label", 14f, UiFonts.Regular, Lavender, TextAnchor.MiddleLeft);
            _cardLabel.text = "Nubi capta una definición";
            _defText = MakeLabel(_card, "Definition", 20f, UiFonts.Word, Color.white, TextAnchor.UpperLeft);
            _defText.supportRichText = true;
            BestFit(_defText, Mathf.RoundToInt(16f * UnitsPerDp));
            _card.gameObject.SetActive(false);
        }

        private RectTransform RoundedPanel(Transform parent, string name, Color color, int radiusPx)
        {
            var img = MakeImage(parent, name, RoundedRectSprite.Get(radiusPx));
            img.type = Image.Type.Sliced;
            img.color = color;
            img.gameObject.SetActive(true);
            return img.rectTransform;
        }

        private void BuildSlots()
        {
            for (int k = 0; k < 10; k++)
            {
                var border = MakeImage(_slotLayer, "SlotBorder" + k, DiscSprite.Get());
                var fill = MakeImage(_slotLayer, "SlotFill" + k, DiscSprite.Get());
                _slotBorders.Add(border);
                _slotFills.Add(fill);
            }
            _askText = MakeLabel(_slotLayer, "Ask", 17f, UiFonts.Regular, Lavender, TextAnchor.MiddleCenter);
            _askText.text = "¿Cuál es la palabra?";
            _askText.gameObject.SetActive(false);
        }

        private void BuildFx()
        {
            for (int i = 0; i < 4; i++)
            {
                var f = new Flyer();
                var go = new GameObject("Flyer" + i);
                go.transform.SetParent(_fxLayer, false);
                f.Root = go.AddComponent<RectTransform>();
                f.Root.anchorMin = f.Root.anchorMax = f.Root.pivot = new Vector2(0.5f, 0.5f);
                f.Glow = MakeImage(f.Root, "Glow", RadialGlowSprite.Get());
                f.Glow.gameObject.SetActive(true);
                f.Core = MakeImage(f.Root, "Core", DiscSprite.Get());
                f.Core.rectTransform.anchorMin = Vector2.zero;
                f.Core.rectTransform.anchorMax = Vector2.one;
                f.Core.rectTransform.offsetMin = f.Core.rectTransform.offsetMax = Vector2.zero;
                f.Core.gameObject.SetActive(true);
                f.Shine = MakeImage(f.Root, "Shine", RadialGlowSprite.Get());
                f.Shine.color = new Color(1f, 1f, 1f, 0.9f);
                f.Shine.rectTransform.anchorMin = new Vector2(0.15f, 0.45f);
                f.Shine.rectTransform.anchorMax = new Vector2(0.55f, 0.85f);
                f.Shine.rectTransform.offsetMin = f.Shine.rectTransform.offsetMax = Vector2.zero;
                f.Shine.gameObject.SetActive(true);
                go.SetActive(false);
                _flyers.Add(f);
            }
            for (int i = 0; i < 60; i++)
            {
                var img = MakeImage(_fxLayer, "Particle", DiscSprite.Get());
                _particles.Add(new Particle { Img = img });
            }
        }

        private void BuildUiPieces()
        {
            _sayText = MakeLabel(_uiLayer, "Say", 18f, UiFonts.Bold, GoodText, TextAnchor.MiddleCenter);
            _sayText.gameObject.SetActive(false);
            _ladderLabel = MakeLabel(_uiLayer, "LadderLabel", 14f, UiFonts.Regular, Dim, TextAnchor.MiddleCenter);
            _ladderLabel.text = "Ayudas";
            for (int k = 0; k < 3; k++)
            {
                var d = MakeImage(_uiLayer, "LadderDot" + k, DiscSprite.Get());
                d.gameObject.SetActive(true);
                _ladderDots[k] = d;
            }
            _ladderLabel.gameObject.SetActive(false);
            foreach (var d in _ladderDots) d.gameObject.SetActive(false);
            for (int i = 0; i < 2; i++)
            {
                var b = new ButtonView();
                var go = new GameObject("Button" + i);
                go.transform.SetParent(_uiLayer, false);
                b.Root = go.AddComponent<RectTransform>();
                b.Root.anchorMin = b.Root.anchorMax = b.Root.pivot = new Vector2(0.5f, 0.5f);
                b.Group = go.AddComponent<CanvasGroup>();
                b.Group.blocksRaycasts = false;
                b.Bg = go.AddComponent<Image>();
                b.Bg.sprite = RoundedRectSprite.Get(48);
                b.Bg.type = Image.Type.Sliced;
                b.Bg.raycastTarget = false;
                NeuroStyle.ClayFrame(b.Bg, 4f, 8f);
                b.Label = MakeText(b.Root, "Label", Mathf.RoundToInt(19f * UnitsPerDp), TextAnchor.MiddleCenter, NeuroStyle.Ink, 0f, 0f);
                BestFit(b.Label, Mathf.RoundToInt(15f * UnitsPerDp));
                go.SetActive(false);
                _buttons[i] = b;
            }
            _hintRing = GuidedTutorial.CreateHintRing(_uiLayer, 100f);
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

            AddResultText("Title", 76, new Vector2(0f, 250f), Color.white);
            AddResultText("Score", 260, new Vector2(0f, 60f), Color.white);
            AddResultText("Detail", 46, new Vector2(0f, -150f), new Color(1f, 1f, 1f, 0.85f));
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

        /// <summary>Un texto centrado en su propio rect; el tamaño en dp se aplica en <see cref="Layout"/> (depende de la escala de la pantalla).</summary>
        private Text MakeLabel(Transform parent, string name, float dp, Font font, Color color, TextAnchor anchor)
        {
            var t = MakeText(parent, name, Mathf.RoundToInt(dp * UnitsPerDp), anchor, color, 0f, 0f);
            t.font = font;
            t.horizontalOverflow = HorizontalWrapMode.Wrap;
            t.verticalOverflow = VerticalWrapMode.Overflow;
            var r = t.rectTransform;
            r.anchorMin = r.anchorMax = new Vector2(0.5f, 0.5f);
            r.pivot = new Vector2(0.5f, 0.5f);
            _fonts.Add(new KeyValuePair<Text, float>(t, dp));
            return t;
        }

        // ------------------------------------------------------------------ verificación de que lo dibujado se ve

        /// <summary>Para las pruebas: arma una ficha, la ficha fija de la ayuda, los botones, un lucero, el que vuela y la escalera, y devuelve lo que NO se vería (pieza
        /// apagada o sin opacidad). Una ficha con la cara apagada deja solo la letra tinta sobre el cielo oscuro: el juego queda injugable (3-oct).</summary>
        public List<string> AuditVisibility()
        {
            var problems = new List<string>();
            _s = 3f; _playW = 1080f; _playH = 1920f; _logicalH = 640f;
            if (_bank == null) _bank = PuntaBank.Fallback();
            _word = _bank.All[0];
            _m = PuntaLayout.Compute(_logicalH, 8);
            _slots = new Tile[_word.Tiles.Length];
            if (_rng == null) _rng = new System.Random(1);
            void Check(string what, Graphic g, bool needsAlpha = true)
            {
                if (g == null) { problems.Add(what + ": no existe"); return; }
                if (!g.gameObject.activeInHierarchy) problems.Add(what + ": apagada");
                else if (needsAlpha && g.color.a <= 0.01f) problems.Add(what + ": transparente");
            }
            var tile = MakeTile('A');
            tile.Root.gameObject.SetActive(true);
            Check("ficha.Face", tile.Face);
            Check("ficha.Rim", tile.Rim);
            Check("ficha.Shadow", tile.Shadow);
            Check("ficha.Shine", tile.Shine);
            Check("ficha.Label", tile.Label);
            MakeGhost();
            _ghost.Root.gameObject.SetActive(true);
            Check("fija.Face", _ghost.Face);
            Check("fija.Rim", _ghost.Rim);
            SetButtonsFor(Phase.Think, false);
            foreach (var b in _buttons)
            {
                if (!b.Root.gameObject.activeInHierarchy) problems.Add("botón " + b.Kind + ": apagado");
                Check("botón " + b.Kind + ".fondo", b.Bg);
                Check("botón " + b.Kind + ".texto", b.Label);
            }
            FillLucero(0, PuntaTier.Solo);
            _luceros[0].Root.gameObject.SetActive(true);
            Check("lucero.Core", _luceros[0].Core);
            Check("lucero.Shine", _luceros[0].Shine);
            SetLadderVisible(true);
            foreach (var d in _ladderDots) Check("escalera.punto", d);
            Check("escalera.rótulo", _ladderLabel);
            _card.gameObject.SetActive(true);
            foreach (var w in _waves) { w.gameObject.SetActive(true); Check("onda", w, false); }
            Check("tarjeta.fondo", _cardFill.GetComponent<Image>());
            Check("tarjeta.borde", _cardBorder.GetComponent<Image>());
            Check("tarjeta.texto", _defText);
            foreach (var f in _flyers) { f.Root.gameObject.SetActive(true); Check("volador.Core", f.Core); Check("volador.Glow", f.Glow); f.Root.gameObject.SetActive(false); }
            return problems;   // nada se destruye aquí (en modo edición no se puede): quien llama destruye el objeto entero
        }

        // ------------------------------------------------------------------ disposición

        private Vector2 P(Vector2 l) => new Vector2((l.x - PuntaLayout.W * 0.5f) * _s, _playH * 0.5f - l.y * _s);
        private Vector2 P(float x, float y) => P(new Vector2(x, y));
        private Vector2 ToLogical(Vector2 u) => new Vector2(u.x / _s + PuntaLayout.W * 0.5f, (_playH * 0.5f - u.y) / _s);

        private void Layout()
        {
            Canvas.ForceUpdateCanvases();
            _playW = _play.rect.width;
            _playH = _play.rect.height;
            _s = _playW / PuntaLayout.W;
            _logicalH = _playH / _s;
            foreach (var kv in _fonts)
            {
                int px = Mathf.RoundToInt(kv.Value * _s);
                kv.Key.fontSize = px;
                if (kv.Key.resizeTextForBestFit) kv.Key.resizeTextMaxSize = px;
            }
            if (_defText != null) _defText.resizeTextMinSize = Mathf.RoundToInt(16f * _s);
            foreach (var b in _buttons)
            {
                b.Label.fontSize = Mathf.RoundToInt(19f * _s);
                b.Label.resizeTextMaxSize = b.Label.fontSize;
                b.Label.resizeTextMinSize = Mathf.RoundToInt(15f * _s);
            }
            _m = PuntaLayout.Compute(_logicalH - _reserve, 7);
            ApplyMetrics(5);
        }

        /// <summary>Coloca lo que depende de la altura (cielo, tarjeta, pregunta, mensajes, escalera) para la palabra de <paramref name="letters"/> letras.</summary>
        private void ApplyMetrics(int letters)
        {
            float w = PuntaLayout.W;
            // tarjeta: sombra, borde, relleno, antena, rótulo y texto
            float cx = w / 2f;
            float cardW = w - 2f * PuntaLayout.SideMargin;
            float cy = _m.CardTop + PuntaLayout.CardH / 2f;
            SetRect(_cardShadow, cx, cy + 6f, cardW, PuntaLayout.CardH);
            SetRect(_cardBorder, cx, cy, cardW + 3f, PuntaLayout.CardH + 3f);
            SetRect(_cardFill, cx, cy, cardW, PuntaLayout.CardH);
            SetRect(_antennaDot.rectTransform, 46f, _m.CardTop + 30f, 8f, 8f);
            foreach (var wv in _waves) wv.rectTransform.anchoredPosition = P(46f, _m.CardTop + 30f);
            SetRect(_cardLabel.rectTransform, 64f + 100f, _m.CardTop + 31f, 200f, 20f);
            SetRect(_defText.rectTransform, cx, _m.CardTop + 54f + (PuntaLayout.CardH - 54f - 12f) / 2f, cardW - 40f, PuntaLayout.CardH - 54f - 12f);
            SetRect(_askText.rectTransform, cx, _m.SlotY, 300f, 28f);
            foreach (var b in _nebulaBlobs) b.rectTransform.sizeDelta = new Vector2(120f * _s, 80f * _s);
            SetRect(_sayText.rectTransform, cx, _m.SayY, 336f, 28f);
            SetRect(_ladderLabel.rectTransform, w / 2f - 40f, _m.LadderY, 80f, 20f);
            for (int k = 0; k < 3; k++) SetRect(_ladderDots[k].rectTransform, w / 2f + 4f + k * 20f + 8f, _m.LadderY, 11f, 11f);
        }

        private void SetRect(RectTransform r, float cx, float cy, float w, float h)
        {
            r.sizeDelta = new Vector2(w * _s, h * _s);
            r.anchoredPosition = P(cx, cy);
        }
    }
}
