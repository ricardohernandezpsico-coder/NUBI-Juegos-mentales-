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

namespace NeuroVida.Games.Cosecha
{
    /// <summary>
    /// "Cosecha de palabras": juego estrella de Lenguaje (fluidez verbal, ver <see cref="CosechaContract"/> y
    /// docs/diseno-cosecha-de-palabras.md). Siete fichas-luna giran en una órbita elíptica alrededor de un planeta de tierra: se
    /// tocan letras en orden (se encienden con su número y pasan a la bandeja) y "Sembrar" las convierte en una semilla que cae en
    /// arco y brota como una planta del huerto. Tocar la última letra de la bandeja la devuelve; "Borrar" la vacía.
    /// <list type="bullet">
    /// <item>3 cosechas por partida (Reto 60 s, Precisión 90 s), cada una con letras nuevas y un planeta nuevo; entre cosechas, 3 s de
    /// "¡Cosecha lista!" con el huerto completo.</item>
    /// <item>Palabra válida: puntos por largo, planta según el largo (flor dorada si es rara) y, si usa las 7 letras, un árbol dorado
    /// con destello y × 3. Repetida: "Ya la tienes" (late). No válida: "No está en el diccionario de Nubi" (se sacude suave). Sin castigo.</item>
    /// <item>Pista: tras 15 s sin sembrar (10 en mayores) Nubi ilumina la primera letra de una palabra común que falte.</item>
    /// <item>Sonido: cada letra sube una nota de la pentatónica (la palabra suena como melodía); sembrar = plop + campanita; brotar =
    /// cuerda suave; repetida/no válida = madera sorda. Racha de 3 palabras en menos de 10 s: brillo de la órbita.</item>
    /// </list>
    /// Con "quitar animaciones": la órbita queda quieta y la semilla aparece sin vuelo ni rebote. 60 cuadros por segundo.
    /// </summary>
    public class CosechaGameController : GameControllerBase
    {
        public const string GameId = CosechaContract.GameId;
        private const string RecentKey = "cosecha_recent";

        private const float UnitsPerDp = 3f;
        private const float MarginU = 60f;
        private const float OrbitSeconds = 40f;
        private const float ReadySeconds = 3f;
        /// <summary>Diámetro de la ficha-luna en unidades (64 dp de toque mínimo = 192 u); las de atrás un poco menores.</summary>
        private const float TileUnits = UnitsPerDp * CosechaContract.MinTouchDp * 1.04f;

        private static readonly Color GoodColor = NeuroStyle.Lime;
        private static readonly Color BadColor = NeuroStyle.Coral;
        private static readonly Color AmberColor = NeuroStyle.Sun;
        private static readonly Color PlateColor = new Color(1f, 0.973f, 0.925f, 1f);
        private static readonly Color[] TileTints =
        {
            new Color(1f, 0.973f, 0.925f, 1f), new Color(0.81f, 0.91f, 1f, 1f), new Color(0.87f, 0.82f, 1f, 1f), new Color(0.84f, 0.94f, 0.75f, 1f),
            new Color(1f, 0.89f, 0.67f, 1f), new Color(1f, 0.84f, 0.80f, 1f), new Color(0.78f, 0.93f, 0.96f, 1f)
        };
        private static readonly Color[] PlanetTints =
        {
            Color.white, new Color(1f, 0.9f, 0.88f, 1f), new Color(1f, 0.96f, 0.84f, 1f)
        };

        private enum Phase { Idle, Playing, Between, Done }

        private sealed class Tile
        {
            public RectTransform Rect;
            public Image Img;
            public Text Letter;
            public Image Badge;
            public Text BadgeText;
            public Image HintRing;
            public bool Front;
            public Vector2 Pos;
            public float Size;
        }

        private System.Random _rng;
        private AdaptiveDifficulty _dda;
        private CosechaBank _bank;
        private CosechaTally _tally;
        private CosechaSession _session;
        private Garden _garden;
        private Phase _phase = Phase.Idle;
        private bool Precision => _config != null && !_config.config.timed;
        private bool Senior;
        private int _previousFrameRate;

        private int _cosecha, _points, _level = 1, _lastTickSecond = -1;
        private float _endsAt, _cosechaSeconds, _lastActionAt;
        private bool _hintOn, _treeDone;
        private int _hintTile = -1;
        private readonly List<string> _usedRounds = new List<string>();
        private readonly HashSet<string> _usedSet = new HashSet<string>();
        private HashSet<string> _avoid = new HashSet<string>();
        private readonly List<float> _recentTimes = new List<float>();
        private float _shineUntil;
        private float _msgUntil;

        // UI
        private RectTransform _safe, _play, _backLayer, _frontLayer, _plantsLayer, _fxRect, _planetRect, _trayRect, _timerBg, _timerFill;
        private RectTransform _btnSow, _btnClear;
        private Image _planetImg, _grassImg, _tuftsImg, _atmosImg, _orbitRing, _trayGlow, _readyDim;
        private Text _cosechaLabel, _listTitle, _listWords, _msg, _readyTitle, _readySub;
        private readonly List<Tile> _tiles = new List<Tile>();
        private readonly List<Text> _slots = new List<Text>();
        private readonly List<Image> _slotLines = new List<Image>();
        private readonly List<RectTransform> _plants = new List<RectTransform>();
        private readonly List<float> _plantY = new List<float>();
        private RectTransform _treeRect;
        private Toast _toast;
        private ExitButton _exit;
        private GameHud _hud;
        private CountdownScreen _countdown;
        private StarfieldFx _stars;
        private float _playW, _playH, _planetR, _trayY, _orbitCx, _orbitCy, _rx, _ry, _slotStep;
        private Vector2 _btnSowCenter, _btnClearCenter, _btnSowSize, _btnClearSize;
        private float _orbitPhase;

        // ------------------------------------------------------------------ sesión

        public void StartSession(SequenceInitConfig config)
        {
            _config = config;
            _rng = new System.Random();
            var age = DdaUserProfileConfig.ParseAgeBand(config.config.age_band);
            Senior = age == AgeBand.Senior;
            float start = AdaptiveDifficulty.StartRating(config.config, CosechaContract.MaxLevel);
            // 3 decisiones por partida (una por cosecha): pasos grandes, sin tiempo de reacción
            _dda = new AdaptiveDifficulty(CosechaContract.MaxLevel, age, start, stepUp: 0.5f, useReaction: false);
            _bank = CosechaBank.Load();
            string recent = "";
            try { recent = PlayerPrefs.GetString(RecentKey, ""); } catch (System.Exception) { }
            _avoid = CosechaContract.RecentIds(recent);
            _usedRounds.Clear();
            _usedSet.Clear();
            _tally = new CosechaTally();

            _previousFrameRate = Application.targetFrameRate;
            Application.targetFrameRate = 60;

            _phase = Phase.Idle;
            _loopOn = false;
            _guided = false;
            _allow = Allow.All;
            _rail = null;
            _flying.Clear();
            _session = null;
            _cosecha = 0;
            _points = 0;
            _level = 1;
            _lastTickSecond = -1;
            _hintOn = false;
            _hintTile = -1;
            _msgUntil = 0f;
            _shineUntil = 0f;
            _recentTimes.Clear();
            _orbitPhase = (float)_rng.NextDouble() * Mathf.PI * 2f;

            HideBoard();

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
            StartCoroutine(Prewarm());
            if (TutorialWanted)
            {
                // la ronda guiada se juega sobre el planeta ya armado, antes de la cuenta regresiva
                _safe.gameObject.SetActive(true);
                yield return null;
                ApplySafeArea(_safe);
                Canvas.ForceUpdateCanvases();
                Layout();
                yield return StartCoroutine(RunTutorialIfNeeded());
                HideBoard();
                _session = null;
                _safe.gameObject.SetActive(false);
            }
            yield return StartCoroutine(_countdown.Play("Cosecha de palabras", Assessment.Subtitle("Forma palabras y haz crecer tu huerto"), () => _safe.gameObject.SetActive(true)));
            _safe.gameObject.SetActive(true);
            yield return null;
            ApplySafeArea(_safe);
            Canvas.ForceUpdateCanvases();
            Layout();

            _loopOn = true;
            _cosecha = 0;
            yield return StartCoroutine(MainLoop(false));
        }

        /// <summary>Las tres cosechas, una tras otra, y el final. «Cómo se juega» la retoma desde aquí, en la cosecha donde estaba (<paramref name="resume"/>: sigue la que quedó a medias, sin armarla de nuevo).</summary>
        private IEnumerator MainLoop(bool resume)
        {
            for (; _cosecha < CosechaContract.Cosechas; _cosecha++)
            {
                yield return StartCoroutine(resume ? PlayCosecha() : RunCosecha());
                resume = false;
                if (_cosecha < CosechaContract.Cosechas - 1) yield return StartCoroutine(ReadyBetween());
            }
            yield return StartCoroutine(FinishGame());
        }

        /// <summary>Hornea el arte y los sonidos durante la cuenta regresiva, repartido en cuadros (así la primera cosecha no traba).</summary>
        private IEnumerator Prewarm()
        {
            CosechaSprites.Planet();
            yield return null;
            CosechaSprites.GrassCap();
            CosechaSprites.Tufts();
            yield return null;
            CosechaSprites.Moon();
            CosechaSprites.Seed();
            CosechaSprites.SeedIcon();
            CosechaSprites.BackIcon();
            yield return null;
            foreach (PlantKind k in System.Enum.GetValues(typeof(PlantKind)))
            {
                CosechaSprites.Plant(k);
                yield return null;
            }
            CosechaSprites.GoldenTree();
            yield return null;
            for (int i = 0; i < 7; i++) CosechaSounds.Letter(i);
            yield return null;
            CosechaSounds.Plop();
            CosechaSounds.Sprout();
            CosechaSounds.Wood();
            yield return null;
            CosechaSounds.Star();
            CosechaSounds.Ready();
            CosechaSounds.Hint();
            CosechaSounds.Shine();
            _baked = true;
        }

        // ------------------------------------------------------------------ una cosecha

        private IEnumerator RunCosecha()
        {
            _level = _dda.PresentedLevel;
            var round = _bank.Pick(_level, Senior, _rng, _usedSet, _avoid);
            if (round == null) round = CosechaBank.Fallback().Pick(1, false, _rng, null, null);
            _usedSet.Add(round.id);
            _usedRounds.Add(round.id);
            _session = new CosechaSession(round);
            _cosechaSeconds = CosechaContract.CosechaSeconds(Precision);
            _hud.SetLevel(_level);
            _hud.SetPoints(_points);

            SetupRound(round);
            yield return StartCoroutine(PopIn(_planetRect, 0.45f));

            _phase = Phase.Playing;
            _endsAt = GameClock.Time + _cosechaSeconds;
            _lastActionAt = GameClock.Time;
            _hintOn = false;
            _lastTickSecond = -1;
            yield return StartCoroutine(PlayCosecha());
        }

        /// <summary>El resto de una cosecha: corre el reloj y, al acabar, la cuenta y la registra. «Cómo se juega» retoma la cosecha desde aquí.</summary>
        private IEnumerator PlayCosecha()
        {
            while (GameClock.Time < _endsAt) yield return null;

            _phase = Phase.Between;
            _session.Clear();
            RefreshTray();
            ClearHint();
            _tally.AddCosecha(_session, (int)_cosechaSeconds, _level);
            bool won = _session.Success;
            var change = _dda.Register(won);
            if (_cosecha < CosechaContract.Cosechas - 1)
            {
                if (change == DdaChange.Up) _toast.Show("¡Subes de nivel!", "Las próximas cosechas traen letras más difíciles", GoodColor, 1.4f);
                else if (change == DdaChange.Down) _toast.Show("Con calma", "Las próximas cosechas son un poco más fáciles", AmberColor, 1.4f);
            }
        }

        /// <summary>Planeta nuevo, letras nuevas, fichas en órbita y los 3 brotes del comienzo.</summary>
        private void SetupRound(HarvestRound round)
        {
            ClearGarden();
            _garden = new Garden(_planetR, _rng.Next());
            _treeDone = false;
            _planetImg.color = PlanetTints[_cosecha % PlanetTints.Length];
            _planetRect.gameObject.SetActive(true);
            _planetRect.localScale = Vector3.one;
            _trayRect.gameObject.SetActive(true);
            _btnSow.gameObject.SetActive(true);
            _btnClear.gameObject.SetActive(true);
            _cosechaLabel.gameObject.SetActive(true);
            _listTitle.gameObject.SetActive(true);
            _listWords.gameObject.SetActive(true);
            _orbitRing.gameObject.SetActive(true);
            for (int i = 0; i < _tiles.Count; i++)
            {
                var t = _tiles[i];
                t.Rect.gameObject.SetActive(true);
                t.Letter.text = char.ToUpperInvariant(round.letras[i]).ToString();
                t.Rect.localScale = Vector3.one;
            }
            for (int i = 0; i < 3; i++) SpawnPlantImmediate(_garden.Add(3, false, PlantKind.Sprout), 0.8f);
            UpdateGrass();
            RefreshTray();
            RefreshList();
            PositionTiles(true);
        }

        private void ClearGarden()
        {
            foreach (var p in _plants) if (p != null) Destroy(p.gameObject);
            _plants.Clear();
            _plantY.Clear();
            if (_treeRect != null) { Destroy(_treeRect.gameObject); _treeRect = null; }
            _grassImg.color = new Color(1f, 1f, 1f, 0f);
            _tuftsImg.color = Color.white;
        }

        private IEnumerator ReadyBetween()
        {
            _phase = Phase.Between;
            GameFeel.Finish();
            PlayClip(CosechaSounds.Ready(), 0.6f);
            _readyTitle.text = "¡Cosecha lista!";
            _readySub.text = $"{_session.CountedWords} palabras · {_session.CommonFound} de las {CosechaContract.CommonTotal(_session.Round)} comunes";
            _readyDim.gameObject.SetActive(true);
            _readyTitle.gameObject.SetActive(true);
            _readySub.gameObject.SetActive(true);
            _readyDim.color = new Color(0f, 0f, 0.05f, 0f);
            float e = 0f;
            while (e < ReadySeconds)
            {
                e += GameClock.DeltaTime;
                float k = Mathf.Clamp01(e / 0.4f);
                _readyDim.color = new Color(0f, 0f, 0.05f, 0.35f * k);
                _readyTitle.rectTransform.localScale = Vector3.one * (Motion.Decorative ? Mathf.LerpUnclamped(0.7f, 1f, UiFx.EaseOutBack(k)) : 1f); // sin ReduceMotion: sin rebote
                yield return null;
            }
            _readyDim.gameObject.SetActive(false);
            _readyTitle.gameObject.SetActive(false);
            _readySub.gameObject.SetActive(false);
        }

        // ------------------------------------------------------------------ entrada

        private void Update()
        {
            if (PollTutorialSkip()) return;             // un toque en «Saltar tutorial» no es un toque al juego
            if (_phase == Phase.Idle) return;
            UpdateClock();
            AnimateOrbit();
            AnimateAmbient();
            if (_phase != Phase.Playing) return;
            HandleInput();
            CheckHint();
            if (_msg.gameObject.activeSelf && GameClock.Time > _msgUntil) _msg.gameObject.SetActive(false);
        }

        private void HandleInput()
        {
            if (_guided)
            {
                // la práctica lee el toque igual que Nubi (así el smoke puede dar un toque «de verdad» en el hueco)
                if (GuidedTutorial.TryPress(out var pressed)) Press(pressed);
                return;
            }
            if (Input.touchCount > 0)
            {
                for (int i = 0; i < Input.touchCount; i++)
                {
                    var t = Input.GetTouch(i);
                    if (t.phase == TouchPhase.Began) Press(t.position);
                }
            }
            else if (Input.GetMouseButtonDown(0)) Press(Input.mousePosition);
        }

        private void Press(Vector2 screen)
        {
            if (_tutorial != null && _tutorial.Coach != null && _tutorial.Coach.Blocks(screen)) return;       // el foco de Nubi: solo vale el toque dentro del hueco
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(_play, screen, null, out var local)) return;
            if (InRect(local, _btnSowCenter, _btnSowSize))
            {
                if (!Allows(Allow.Sow)) return;
                StartCoroutine(PopRect(_btnSow, 0.94f, 0.14f));
                Plant();
                return;
            }
            if (InRect(local, _btnClearCenter, _btnClearSize))
            {
                if (!Allows(Allow.Clear)) return;
                StartCoroutine(PopRect(_btnClear, 0.9f, 0.14f));
                ClearTray();
                return;
            }
            // la última letra de la bandeja la devuelve
            int n = _session.Tray.Count;
            if (n > 0)
            {
                var slot = new Vector2(SlotX(n - 1), _trayY);
                if (InRect(local, slot, new Vector2(_slotStep, 200f))) { if (Allows(Allow.Back)) BackOne(); return; }
            }
            // una ficha de la órbita: la más cercana al toque (el radio de la ficha + un margen)
            Tile best = null;
            float bestD = float.MaxValue;
            foreach (var t in _tiles)
            {
                float d = Vector2.Distance(local, t.Pos);
                if (d <= t.Size * 0.5f + 14f && d < bestD) { best = t; bestD = d; }
            }
            if (best != null && Allows(Allow.Tile)) TapTile(_tiles.IndexOf(best));
        }

        private static bool InRect(Vector2 p, Vector2 center, Vector2 size) =>
            Mathf.Abs(p.x - center.x) <= size.x * 0.5f && Mathf.Abs(p.y - center.y) <= size.y * 0.5f;

        private void TapTile(int index)
        {
            if (_session == null) return;
            if (_guided && _rail != null)
            {
                int next = _session.Tray.Count;                        // la práctica acepta las fichas solo en el orden pedido
                if (next >= _rail.Length || index != _rail[next]) return;
            }
            int before = _session.Tray.Count;
            bool wasLast = before > 0 && _session.Tray[before - 1] == index;
            if (!_session.TapTile(index)) return;
            _lastActionAt = GameClock.Time;
            ClearHint();
            var tile = _tiles[index];
            StartCoroutine(PopRect(tile.Rect, 0.88f, 0.14f));
            if (!wasLast) PlayClip(CosechaSounds.Letter(before), 0.55f);
            GameFeel.Haptic(GameFeel.HapticKind.Light);
            RefreshTray();
        }

        private void BackOne()
        {
            if (_session.RemoveLast())
            {
                _lastActionAt = GameClock.Time;
                ClearHint();
                GameFeel.Haptic(GameFeel.HapticKind.Light);
                RefreshTray();
            }
        }

        private void ClearTray()
        {
            if (_session.Tray.Count == 0) return;
            _session.Clear();
            _lastActionAt = GameClock.Time;
            ClearHint();
            RefreshTray();
        }

        // ------------------------------------------------------------------ sembrar

        private void Plant()
        {
            if (_session == null) return;
            int count = _session.Tray.Count;
            var slotPositions = new List<Vector2>();
            var letters = new List<string>();
            for (int i = 0; i < count; i++)
            {
                slotPositions.Add(new Vector2(SlotX(i), _trayY));
                letters.Add(char.ToUpperInvariant(_session.Round.letras[_session.Tray[i]]).ToString());
            }
            float time = GameClock.Time - (_endsAt - _cosechaSeconds);
            var r = _session.Submit(time);
            _lastActionAt = GameClock.Time;
            ClearHint();
            switch (r.Kind)
            {
                case SubmitKind.TooShort:
                    ShowMessage($"Usa {CosechaContract.MinLetters} letras o más", AmberColor);
                    PlayClip(CosechaSounds.Wood(), 0.4f);
                    return;
                case SubmitKind.Repeat:
                    ShowMessage("Ya la tienes", NeuroStyle.Sky);
                    PlayClip(CosechaSounds.Wood(), 0.4f);
                    GameFeel.Haptic(GameFeel.HapticKind.Light);
                    StartCoroutine(PopRect(_trayRect, 1.06f, 0.25f));
                    RefreshTray();
                    return;
                case SubmitKind.Invalid:
                    ShowMessage("No está en el diccionario de Nubi", BadColor);
                    PlayClip(CosechaSounds.Wood(), 0.45f);
                    GameFeel.Haptic(GameFeel.HapticKind.Light);
                    if (!GameFeel.ReduceMotion) StartCoroutine(UiFx.Shake(18f, 0.35f, _trayRect));
                    RefreshTray();
                    return;
            }
            // palabra válida: puntos, medidas y la planta
            _points += r.Points;
            _hud.SetPoints(_points);
            RefreshTray();
            RefreshList();
            NoteFastWord();
            PlantSpot spot;
            bool tree = false;
            if (r.Star && !_treeDone) { spot = _garden.TreeSpot(); tree = true; _treeDone = true; }
            else spot = _garden.Add(CosechaContract.Normalize(r.Word.p).Length, r.Word.IsRare || (r.Star && _treeDone));
            var flight = new Flight { Spot = spot, Tree = tree };
            _flying.Add(flight);
            _lastSowAt = GameClock.Time;
            StartCoroutine(SowRoutine(letters, slotPositions, spot, tree, r, flight));
        }

        private void NoteFastWord()
        {
            float now = GameClock.Time;
            _recentTimes.Add(now);
            int n = _recentTimes.Count;
            if (n >= CosechaContract.FastWords && now - _recentTimes[n - CosechaContract.FastWords] < CosechaContract.FastSeconds)
            {
                _recentTimes.Clear();
                if (!GameFeel.ReduceMotion) _shineUntil = now + 1.4f;
                PlayClip(CosechaSounds.Shine(), 0.45f);
            }
        }

        /// <summary>Las letras vuelan hacia una semilla, la semilla cae en arco al lugar de su planta y brota con rebote.</summary>
        private IEnumerator SowRoutine(List<string> letters, List<Vector2> from, PlantSpot spot, bool tree, SubmitResult r, Flight pending)
        {
            Vector2 center = new Vector2(0f, _trayY);
            Vector2 target = PlantPosition(spot);
            bool calm = GameFeel.ReduceMotion;
            var ghosts = new List<RectTransform>();
            if (!calm)
            {
                for (int i = 0; i < letters.Count; i++)
                {
                    var t = MakeText(_fxRect, "Ghost", (int)(CosechaContract.LetterSizeSp(Senior) * UnitsPerDp), TextAnchor.MiddleCenter, NeuroStyle.Ink, 0f, 0f);
                    t.font = UiFonts.Word;
                    t.text = letters[i];
                    var gr = t.rectTransform;
                    gr.anchorMin = gr.anchorMax = new Vector2(0.5f, 0.5f);
                    gr.sizeDelta = new Vector2(120f, 120f);
                    gr.anchoredPosition = from[i];
                    ghosts.Add(gr);
                }
                float e = 0f;
                const float gather = 0.16f;
                while (e < gather)
                {
                    e += GameClock.DeltaTime;
                    float k = UiFx.EaseOutCubic(Mathf.Clamp01(e / gather));
                    for (int i = 0; i < ghosts.Count; i++)
                    {
                        ghosts[i].anchoredPosition = Vector2.Lerp(from[i], center, k);
                        ghosts[i].localScale = Vector3.one * (1f - 0.7f * k);
                    }
                    yield return null;
                }
                foreach (var g in ghosts) Destroy(g.gameObject);
            }

            // la semilla
            var seed = NewImage(_fxRect, "Seed", CosechaSprites.Seed());
            seed.rectTransform.sizeDelta = new Vector2(84f, 84f);
            seed.gameObject.SetActive(true);
            if (!calm)
            {
                const float flight = 0.46f;
                float e2 = 0f;
                while (e2 < flight)
                {
                    e2 += GameClock.DeltaTime;
                    float k = Mathf.Clamp01(e2 / flight);
                    Vector2 p = Vector2.Lerp(center, target, k);
                    p.y += Mathf.Sin(k * Mathf.PI) * 220f;                     // arco
                    seed.rectTransform.anchoredPosition = p;
                    seed.rectTransform.localRotation = Quaternion.Euler(0f, 0f, k * 330f);
                    seed.rectTransform.localScale = Vector3.one * (1f - 0.3f * k);
                    yield return null;
                }
            }
            else seed.rectTransform.anchoredPosition = target;
            Destroy(seed.gameObject);

            // brota
            PlayClip(CosechaSounds.Plop(), 0.5f);
            if (r.Star) PlayClip(CosechaSounds.Star(), 0.55f);
            else PlayClip(CosechaSounds.Sprout(), 0.4f);
            GameFeel.Correct(Mathf.Min(7, _session.Found.Count));
            if (tree) SpawnTree(spot); else SpawnPlant(spot);
            _flying.Remove(pending);
            UpdateGrass();
            StartCoroutine(UiFx.RingBurst(_fxRect, target, r.Star ? NeuroStyle.Sun : NeuroStyle.Lime, 60f, 340f, 0.5f));
            if (!calm) StartCoroutine(UiFx.SparkBurst(_fxRect, target, r.Star ? NeuroStyle.Sun : NeuroStyle.Lime, r.Star ? 14 : 8, r.Star ? 300f : 190f, 22f, 0.6f));
            StartCoroutine(FloatText(target + new Vector2(0f, 120f), (r.Star ? "¡Palabra estrella! × 3  +" : "+") + r.Points, r.Star ? AmberColor : GoodColor, r.Star ? 66 : 54));
            if (r.Star) GameFeel.Haptic(GameFeel.HapticKind.Firm);
            else if (r.Word.IsRare) GameFeel.Haptic(GameFeel.HapticKind.Double);
        }

        private Vector2 PlantPosition(PlantSpot s) => _planetRect.anchoredPosition + new Vector2(s.X, s.Y);

        private void SpawnPlantImmediate(PlantSpot s, float scale)
        {
            SpawnPlant(s, false, scale);
        }

        private void SpawnPlant(PlantSpot s, bool animate = true, float scale = 1f)
        {
            float side = s.Size * _planetR / 0.72f * scale;
            var img = NewImage(_plantsLayer, "Plant", CosechaSprites.Plant(s.Kind));
            var rt = img.rectTransform;
            rt.sizeDelta = new Vector2(side, side);
            rt.pivot = new Vector2(0.5f, CosechaSprites.PlantBase);
            rt.anchoredPosition = PlantPosition(s);
            rt.localRotation = Quaternion.Euler(0f, 0f, s.Tilt);
            img.gameObject.SetActive(true);
            _plants.Add(rt);
            _plantY.Add(s.Y);
            SortPlants();
            if (animate && !GameFeel.ReduceMotion) StartCoroutine(Sprout(rt));
        }

        private void SpawnTree(PlantSpot s)
        {
            float side = s.Size * _planetR / 0.81f;
            var img = NewImage(_plantsLayer, "GoldenTree", CosechaSprites.GoldenTree());
            var rt = img.rectTransform;
            rt.sizeDelta = new Vector2(side, side);
            rt.pivot = new Vector2(0.5f, 0.064f);
            rt.anchoredPosition = PlantPosition(s);
            img.gameObject.SetActive(true);
            rt.SetAsLastSibling();
            _treeRect = rt;
            var glow = NewImage(rt, "TreeGlow", RadialGlowSprite.Get());
            glow.color = new Color(1f, 0.84f, 0.36f, 0.5f);
            glow.rectTransform.sizeDelta = new Vector2(side * 1.5f, side * 1.5f);
            glow.rectTransform.anchoredPosition = new Vector2(0f, side * 0.45f);
            glow.gameObject.SetActive(true);
            glow.transform.SetAsFirstSibling();
            if (!GameFeel.ReduceMotion) StartCoroutine(Sprout(rt));
        }

        /// <summary>Brota con rebote desde la base: crece de 0 con un pequeño sobrepaso.</summary>
        private IEnumerator Sprout(RectTransform rt)
        {
            float t = 0f;
            const float seconds = 0.45f;
            while (t < seconds)
            {
                if (rt == null) yield break;
                t += GameClock.DeltaTime;
                float k = Mathf.Clamp01(t / seconds);
                rt.localScale = new Vector3(Mathf.LerpUnclamped(0.3f, 1f, UiFx.EaseOutBack(k)), Mathf.LerpUnclamped(0f, 1f, UiFx.EaseOutBack(k)), 1f);
                yield return null;
            }
            if (rt != null) rt.localScale = Vector3.one;
        }

        private void SortPlants()
        {
            // de atrás (arriba del planeta) hacia adelante: las que están más arriba se dibujan primero
            var order = new List<int>();
            for (int i = 0; i < _plants.Count; i++) order.Add(i);
            order.Sort((a, b) => _plantY[b].CompareTo(_plantY[a]));
            for (int k = 0; k < order.Count; k++) _plants[order[k]].SetSiblingIndex(k);
            if (_treeRect != null) _treeRect.SetAsLastSibling();
        }

        private void UpdateGrass()
        {
            int n = _garden != null ? _garden.Count - 3 : 0;
            float cap = Mathf.Clamp01(n / 22f);
            _grassImg.color = new Color(1f, 1f, 1f, cap);
            _tuftsImg.color = new Color(1f, 1f, 1f, 1f - cap * 0.8f);
        }

        // ------------------------------------------------------------------ pista y mensajes

        private void CheckHint()
        {
            if (_guided || _hintOn || _session == null) return;
            if (GameClock.Time - _lastActionAt < CosechaContract.HintAfter(Senior)) return;
            var word = CosechaContract.PickHint(_session.Round, _session.FoundNormalized);
            if (word == null) return;
            char first = CosechaContract.Normalize(word.p)[0];
            int tile = -1;
            for (int i = 0; i < _session.Round.letras.Length; i++)
                if (_session.Round.letras[i] == first && !_session.InTray(i)) { tile = i; break; }
            if (tile < 0) { _lastActionAt = GameClock.Time; return; }
            _hintOn = true;
            _hintTile = tile;
            _session.AddHint();
            _tiles[tile].HintRing.gameObject.SetActive(true);
            PlayClip(CosechaSounds.Hint(), 0.5f);
            ShowMessage("Nubi te da una pista: empieza con la letra que brilla", AmberColor, 4f);
            _lastActionAt = GameClock.Time;
        }

        private void ClearHint()
        {
            if (!_hintOn) return;
            _hintOn = false;
            if (_hintTile >= 0 && _hintTile < _tiles.Count) _tiles[_hintTile].HintRing.gameObject.SetActive(false);
            _hintTile = -1;
        }

        private void ShowMessage(string text, Color color, float seconds = 1.1f)
        {
            _msg.text = text;
            _msg.color = color;
            _msg.gameObject.SetActive(true);
            _msgUntil = GameClock.Time + seconds;
        }

        private IEnumerator FloatText(Vector2 pos, string text, Color color, int size)
        {
            var t = MakeText(_fxRect, "Float", size, TextAnchor.MiddleCenter, color, 0f, 0f);
            NeuroStyle.ClayText(t, 4f, 6f);
            var r = t.rectTransform;
            r.anchorMin = r.anchorMax = new Vector2(0.5f, 0.5f);
            r.sizeDelta = new Vector2(900f, 110f);
            t.horizontalOverflow = HorizontalWrapMode.Overflow;
            t.text = text;
            float e = 0f;
            const float seconds = 0.9f;
            while (e < seconds)
            {
                e += GameClock.DeltaTime;
                float k = Mathf.Clamp01(e / seconds);
                r.anchoredPosition = pos + new Vector2(0f, 90f * (Motion.Decorative ? UiFx.EaseOutCubic(k) : 0f));
                t.color = NeuroStyle.WithAlpha(color, 1f - k * k);
                yield return null;
            }
            Destroy(t.gameObject);
        }

        // ------------------------------------------------------------------ bandeja, lista y órbita

        private float SlotX(int i) => (i - 3) * _slotStep;

        private void RefreshTray()
        {
            string text = _session != null ? _session.TrayText : "";
            for (int i = 0; i < _slots.Count; i++)
            {
                bool filled = i < text.Length;
                _slots[i].text = filled ? char.ToUpperInvariant(text[i]).ToString() : "";
                _slotLines[i].gameObject.SetActive(!filled);
            }
            for (int i = 0; i < _tiles.Count; i++)
            {
                var t = _tiles[i];
                int order = _session != null ? _session.Tray.IndexOf(i) : -1;
                bool used = order >= 0;
                t.Img.color = used ? Color.Lerp(TileTints[i % TileTints.Length], new Color(0.45f, 0.42f, 0.6f, 1f), 0.55f) : TileTints[i % TileTints.Length];
                t.Letter.color = used ? new Color(0.24f, 0.2f, 0.38f, 1f) : NeuroStyle.Ink;
                t.Badge.gameObject.SetActive(used);
                if (used) t.BadgeText.text = (order + 1).ToString();
            }
        }

        private void RefreshList()
        {
            int n = _session != null ? _session.CountedWords : 0;
            _listTitle.text = $"Tu cosecha · {n} {(n == 1 ? "palabra" : "palabras")}";
            var shown = new List<string>();
            if (_session != null)
                for (int i = _session.Found.Count - 1; i >= 0 && shown.Count < 6; i--)
                    if (!_session.Found[i].Word.oculta) shown.Add(_session.Found[i].Word.p);
            _listWords.text = shown.Count > 0 ? string.Join("   ", shown) : "toca letras para empezar";
        }

        private void AnimateOrbit()
        {
            if (_phase == Phase.Idle || _session == null) return;
            if (!GameFeel.ReduceMotion && !_guided && (_phase == Phase.Playing || _phase == Phase.Between)) _orbitPhase += GameClock.DeltaTime * Mathf.PI * 2f / OrbitSeconds;
            PositionTiles(false);
        }

        private void PositionTiles(bool force)
        {
            for (int i = 0; i < _tiles.Count; i++)
            {
                var t = _tiles[i];
                float a = _orbitPhase + i * Mathf.PI * 2f / _tiles.Count;
                float sin = Mathf.Sin(a);
                t.Pos = new Vector2(_orbitCx + _rx * Mathf.Cos(a), _orbitCy + _ry * sin);
                bool front = sin <= 0f;                                  // abajo = delante del planeta
                t.Size = TileUnits * (front ? 1f : 0.9f);
                t.Rect.anchoredPosition = t.Pos;
                t.Rect.sizeDelta = new Vector2(t.Size, t.Size);
                if (force || front != t.Front)
                {
                    t.Front = front;
                    t.Rect.SetParent(front ? _frontLayer : _backLayer, false);
                    t.Rect.anchoredPosition = t.Pos;
                }
            }
        }

        private void AnimateAmbient()
        {
            if (_orbitRing == null || !_orbitRing.gameObject.activeSelf) return;
            bool shine = GameClock.Time < _shineUntil;
            float pulse = shine ? 0.55f + 0.3f * Mathf.Sin(GameClock.Time * 14f) : 0.22f;
            _orbitRing.color = shine ? NeuroStyle.WithAlpha(NeuroStyle.Sun, pulse) : NeuroStyle.WithAlpha(NeuroStyle.Sky, pulse);
            float t = GameFeel.ReduceMotion ? 0.5f : 0.5f + 0.5f * Mathf.Sin(GameClock.Time * 1.6f);
            _atmosImg.color = new Color(0.59f, 0.89f, 0.75f, 0.2f + 0.12f * t);
            if (_hintOn && _hintTile >= 0)
            {
                var ring = _tiles[_hintTile].HintRing;
                ring.color = NeuroStyle.WithAlpha(NeuroStyle.Sun, GameFeel.ReduceMotion ? 1f : 0.6f + 0.4f * Mathf.Sin(GameClock.Time * 7f));
            }
        }

        private void PlayClip(AudioClip clip, float volume)
        {
            if (clip == null || !GameFeel.SoundOn) return;
            _audioSource.PlayOneShot(clip, volume);
        }

        private void UpdateClock()
        {
            if (_guided || _phase != Phase.Playing || _endsAt <= 0f) return;
            float left = Mathf.Max(0f, _endsAt - GameClock.Time);
            SetTimerFraction(Mathf.Clamp01(left / _cosechaSeconds));
            int whole = Mathf.CeilToInt(left);
            _cosechaLabel.text = $"Cosecha {_cosecha + 1} de {CosechaContract.Cosechas} · {whole / 60}:{whole % 60:00}";
            if (whole <= 5 && whole >= 1 && whole != _lastTickSecond)
            {
                _lastTickSecond = whole;
                GameFeel.Tick();
            }
        }

        // ------------------------------------------------------------------ tutorial con Nubi (ronda guiada) y «Cómo se juega» desde la pausa

        /// <summary>Qué se puede tocar durante la ronda guiada (fuera de ella, todo).</summary>
        [System.Flags]
        private enum Allow { None = 0, Tile = 1, Sow = 2, Clear = 4, Back = 8, All = 15 }

        /// <summary>Una semilla en vuelo: si la partida se corta antes de que caiga («Cómo se juega»), su planta se pone igual.</summary>
        private sealed class Flight { public PlantSpot Spot; public bool Tree; }

        /// <summary>La cosecha en curso, apartada mientras «Cómo se juega» usa el mismo planeta para la práctica.</summary>
        private sealed class KeptBoard
        {
            public CosechaSession Session;
            public Garden Garden;
            public List<RectTransform> Plants;
            public List<float> PlantY;
            public RectTransform Tree;
            public bool TreeDone;
            public int Points;
            public float Orbit;
        }

        /// <summary>La órbita queda QUIETA durante la práctica y en esta fase (radianes): la C a la izquierda y las demás fichas repartidas, para que Nubi y su globo quepan entre las letras en cualquier forma de pantalla.</summary>
        private const float PracticeOrbit = 3.9f;

        private bool _guided, _loopOn, _baked;
        private Allow _allow = Allow.All;
        private int[] _rail;                                  // las fichas que la práctica pide tocar, en orden (null = cualquiera)
        private float _lastSowAt = -10f;
        private readonly List<Flight> _flying = new List<Flight>();

        /// <summary>El tutorial guiado común sobre el área segura (se llama al final de <c>BuildUi</c>, para que quede encima de todo). «Saltar tutorial» va arriba: abajo están Sembrar y Borrar.</summary>
        private void SetUpTutorial() =>
            BuildTutorial(_safe, GameHud.Height + 10f, "Cosecha de palabras", "Forma palabras de 3 letras o más con las letras que giran y haz crecer tu huerto.", skipAtTop: true);

        private bool Allows(Allow what) => !_guided || (_allow & what) != 0;

        /// <summary>Deja el tablero como al empezar una partida: nada del huerto a la vista (lo usan <c>StartSession</c> y el final de la práctica).</summary>
        private void HideBoard()
        {
            _resultRoot.gameObject.SetActive(false);
            _exit.Hide();
            _timerBg.gameObject.SetActive(true);
            _msg.gameObject.SetActive(false);
            _readyDim.gameObject.SetActive(false);
            _readyTitle.gameObject.SetActive(false);
            _readySub.gameObject.SetActive(false);
            _trayRect.gameObject.SetActive(false);
            _planetRect.gameObject.SetActive(false);
            foreach (var t in _tiles) t.Rect.gameObject.SetActive(false);
            _btnSow.gameObject.SetActive(false);
            _btnClear.gameObject.SetActive(false);
            _cosechaLabel.gameObject.SetActive(false);
            _listTitle.gameObject.SetActive(false);
            _listWords.gameObject.SetActive(false);
            _orbitRing.gameObject.SetActive(false);
            ClearGarden();
        }

        /// <summary>Durante la práctica no hay reloj ni lista de palabras (en su lugar van «Práctica: no cuenta» y «Saltar tutorial»).</summary>
        private void ShowPracticeChrome(bool practice)
        {
            _cosechaLabel.gameObject.SetActive(!practice);
            _timerBg.gameObject.SetActive(!practice);
            _listTitle.gameObject.SetActive(!practice);
            _listWords.gameObject.SetActive(!practice);
        }

        /// <summary>Lo que una corrutina cortada dejó a medias (fichas, botones y bandeja a medio rebote).</summary>
        private void ResetPieces()
        {
            _planetRect.localScale = Vector3.one;
            _trayRect.localScale = Vector3.one;
            _trayRect.anchoredPosition = new Vector2(0f, _trayY);
            _btnSow.localScale = Vector3.one;
            _btnClear.localScale = Vector3.one;
            foreach (var t in _tiles) t.Rect.localScale = Vector3.one;
        }

        private KeptBoard SetAsideBoard()
        {
            if (_session == null) return null;
            var kept = new KeptBoard
            {
                Session = _session, Garden = _garden, Plants = new List<RectTransform>(_plants), PlantY = new List<float>(_plantY),
                Tree = _treeRect, TreeDone = _treeDone, Points = _points, Orbit = _orbitPhase
            };
            foreach (var p in kept.Plants) if (p != null) p.gameObject.SetActive(false);
            if (kept.Tree != null) kept.Tree.gameObject.SetActive(false);
            _plants.Clear();
            _plantY.Clear();
            _treeRect = null;
            return kept;
        }

        private void RestoreBoard(KeptBoard kept)
        {
            ClearGarden();                                    // las plantas de la práctica
            _session = kept.Session;
            _garden = kept.Garden;
            _treeDone = kept.TreeDone;
            _points = kept.Points;
            _orbitPhase = kept.Orbit;
            _plants.AddRange(kept.Plants);
            _plantY.AddRange(kept.PlantY);
            _treeRect = kept.Tree;
            foreach (var p in kept.Plants) if (p != null) p.gameObject.SetActive(true);
            if (_treeRect != null) _treeRect.gameObject.SetActive(true);
            for (int i = 0; i < _tiles.Count; i++) _tiles[i].Letter.text = char.ToUpperInvariant(_session.Round.letras[i]).ToString();
            SortPlants();
            UpdateGrass();
            _hud.SetPoints(_points);
            RefreshTray();
            RefreshList();
        }

        private Rect TileHole(NubiCoach coach, int tile) => coach.AroundOf(_tiles[tile].Rect, Vector2.one * (TileUnits + 36f));

        private int RailTile() => _rail[Mathf.Min(_session.Tray.Count, _rail.Length - 1)];

        // <guided>
        protected override IEnumerator GuidedRound(GuidedTutorial t)
        {
            t.BeginPractice();
            _guided = true;
            while (!_baked) yield return null;                         // el arte se hornea mientras Nubi se presenta
            var coach = t.Coach;
            // Pasos (explicar → mirar → hacer): 1) la primera letra de CASA; 2) las otras tres, con el foco siguiendo a la ficha que toca; 3) Sembrar; 4) mirar cómo brota; 5) un error de muestra (dos
            // letras que no forman palabra) y Borrar; 6) «¡Listo!». Las fichas se aceptan solo en el orden pedido: nadie se queda con una palabra que no existe sin saber cómo salir.
            var kept = SetAsideBoard();
            var round = CosechaContract.PracticeRound();
            _session = new CosechaSession(round);
            _points = 0;
            _hud.SetPoints(0);
            _rail = CosechaContract.TilesFor(round.letras, CosechaContract.PracticeWord);
            _allow = Allow.None;
            _orbitPhase = PracticeOrbit;
            SetupRound(round);
            ShowPracticeChrome(true);
            _msg.gameObject.SetActive(false);
            _phase = Phase.Playing;
            yield return StartCoroutine(PopIn(_planetRect, 0.45f));

            var tray = new[] { coach.Zone(() => coach.RectOf(_trayRect)) };
            bool ok = !t.Skipped;

            // 1) la primera letra (toque de verdad en la ficha C)
            _allow = Allow.Tile;
            while (ok && _session.Tray.Count == 0)
            {
#if UNITY_EDITOR
                StartCoroutine(ProbeTap(coach, _tiles[_rail[0]].Rect, "la primera letra", () => _session.Tray.Count > 0));
#endif
                yield return StartCoroutine(coach.Touch(() => TileHole(coach, _rail[0]), CoachTexts.Cosecha.First, circle: true));
                ok = !t.Skipped;
#if UNITY_EDITOR
                if (ok && GuidedTutorial.EditorAutoContinue && _session.Tray.Count == 0) TapTile(_rail[0]);          // el smoke no toca: lo hace por la persona
#endif
            }

            // 2) las otras tres: el foco sigue a la ficha que toca (las que ya están en la bandeja no se vuelven a pedir)
            while (ok && _session.Tray.Count < _rail.Length)
            {
#if UNITY_EDITOR
                if (GuidedTutorial.EditorAutoContinue) StartCoroutine(EditorTapRail());
#endif
                yield return StartCoroutine(coach.Watch(() => TileHole(coach, RailTile()), CoachTexts.Cosecha.Next, () => _session.Tray.Count >= _rail.Length, 40f, circle: true, keep: tray));
                ok = !t.Skipped;
            }

            // 3) Sembrar
            _allow = Allow.Sow;
            while (ok && _session.Found.Count == 0)
            {
#if UNITY_EDITOR
                StartCoroutine(ProbeTap(coach, _btnSow, "Sembrar", () => _session.Found.Count > 0));
#endif
                yield return StartCoroutine(coach.Touch(() => coach.RectOf(_btnSow), CoachTexts.Cosecha.Sow, keep: tray));
                ok = !t.Skipped;
#if UNITY_EDITOR
                if (ok && GuidedTutorial.EditorAutoContinue && _session.Found.Count == 0) Plant();
#endif
            }

            // 4) mirar: la semilla vuela, brota y el huerto crece
            _allow = Allow.None;
            if (ok)
            {
                float landedAt = -1f;
                System.Func<bool> sprouted = () =>
                {
                    if (_flying.Count > 0) return false;
                    if (landedAt < 0f) landedAt = GameClock.Time;
                    return GameClock.Time - landedAt >= 1.8f;
                };
                yield return StartCoroutine(coach.Watch(() => coach.AroundOf(_planetRect, Vector2.one * (_planetR * 2.7f)), CoachTexts.Cosecha.Sprout, sprouted, 10f, circle: true));
                ok = !t.Skipped;
            }

            // 5) un error de muestra: dos letras que no forman palabra, y Borrar las quita
            if (ok)
            {
                _rail = null;
                foreach (int tile in CosechaContract.TilesFor(round.letras, CosechaContract.PracticeMistake)) TapTile(tile);
                _allow = Allow.Clear;
                yield return Motion.Hold(0.5f);
                while (ok && _session.Tray.Count > 0)
                {
#if UNITY_EDITOR
                    StartCoroutine(ProbeTap(coach, _btnClear, "Borrar", () => _session.Tray.Count == 0));
#endif
                    yield return StartCoroutine(coach.Touch(() => coach.RectOf(_btnClear), CoachTexts.Cosecha.Erase, keep: tray));
                    ok = !t.Skipped;
#if UNITY_EDITOR
                    if (ok && GuidedTutorial.EditorAutoContinue && _session.Tray.Count > 0) ClearTray();
#endif
                }
                _allow = Allow.None;
                if (ok) yield return Motion.Hold(0.4f);
            }
            if (ok) yield return StartCoroutine(coach.Notice(CoachTexts.Ready, 1.5f));

            // si se saltó con una semilla en el aire, que termine de brotar y se apaguen sus destellos antes de limpiar
            while (GameClock.Time - _lastSowAt < 1.4f) yield return null;
            coach.Hide();
            _allow = Allow.All;
            _rail = null;
            _flying.Clear();
            foreach (Transform c in _fxRect) Destroy(c.gameObject);
            ClearHint();
            ShowPracticeChrome(false);
            if (kept != null) RestoreBoard(kept);
            else
            {
                _session = null;
                _points = 0;
                _hud.SetPoints(0);
            }
            ResetPieces();
            _guided = false;
            _phase = Phase.Idle;                                       // la partida (o «Cómo se juega») sigue desde su bucle
            t.EndPractice();
        }
        // </guided>

#if UNITY_EDITOR
        /// <summary>SOLO EN EL EDITOR (smoke del tutorial): da un toque «de verdad» en el centro de lo que el paso ilumina y comprueba que el paso se cierra. Si el hueco no coincidiera con lo dibujado, el tutorial se
        /// «pegaría» sin dejar tocar lo que pide (Ricardo, 6-oct); así se vería antes de llegar al teléfono.</summary>
        private IEnumerator ProbeTap(NubiCoach coach, RectTransform target, string what, System.Func<bool> reached)
        {
            if (!GuidedTutorial.EditorAutoContinue) yield break;
            float w = 0f;
            while (!coach.Active && w < 3f) { w += GameClock.RealDeltaTime; yield return null; }
            w = 0f;
            while (coach.Active && w < 0.7f) { w += GameClock.RealDeltaTime; yield return null; }
            if (!coach.Active) yield break;
            GuidedTutorial.EditorPressPos = RectTransformUtility.WorldToScreenPoint(null, target.position);
            GuidedTutorial.EditorPressFrame = Time.frameCount + 1;
            for (int i = 0; i < 4; i++) yield return null;
            if (!reached()) Debug.LogError("[SmokeTest] Cosecha: un toque en el centro de " + what + " NO llegó al juego (el hueco no coincide con lo que se toca)");
            else Debug.Log("[SmokeTest] Cosecha: un toque en el centro de " + what + " llegó al juego");
        }

        /// <summary>SOLO EN EL EDITOR (smoke): mientras Nubi mira, toca las fichas que faltan de CASA, una cada medio segundo.</summary>
        private IEnumerator EditorTapRail()
        {
            float w = 0f;
            while (w < 2.2f) { w += GameClock.RealDeltaTime; yield return null; }
            while (_guided && _rail != null && _session != null && _session.Tray.Count < _rail.Length)
            {
                TapTile(_rail[_session.Tray.Count]);
                float g = 0f;
                while (g < 0.45f) { g += GameClock.RealDeltaTime; yield return null; }
            }
        }
#endif

        // ------------------------------------------------------------------ «Cómo se juega» desde la pausa

        protected override bool HowToReady => _loopOn && _phase == Phase.Playing && !_guided;

        protected override void HowToSuspend()
        {
            // las semillas en vuelo brotan ya (su corrutina se va a detener) y lo que flotaba en el aire se barre
            foreach (var f in _flying) { if (f.Tree) SpawnTree(f.Spot); else SpawnPlant(f.Spot, false); }
            _flying.Clear();
            foreach (Transform c in _fxRect) Destroy(c.gameObject);
            UpdateGrass();
            ClearHint();
            _msg.gameObject.SetActive(false);
            ResetPieces();
            _phase = Phase.Idle;
        }

        protected override void HowToResume(float spentSeconds)
        {
            _endsAt = HowToClock.Shift(_endsAt, spentSeconds);       // el tiempo que duró «Cómo se juega» no se le descuenta a la cosecha
            _lastActionAt = GameClock.Time;
            _lastTickSecond = -1;
            _phase = Phase.Playing;
            StartCoroutine(MainLoop(true));
        }

        // ------------------------------------------------------------------ fin

        private IEnumerator FinishGame()
        {
            _phase = Phase.Done;
            ClearHint();
            _trayRect.gameObject.SetActive(false);
            _btnSow.gameObject.SetActive(false);
            _btnClear.gameObject.SetActive(false);
            try { PlayerPrefs.SetString(RecentKey, CosechaContract.PushRecent(PlayerPrefs.GetString(RecentKey, ""), _usedRounds)); PlayerPrefs.Save(); }
            catch (System.Exception) { }

            float share = _tally.CommonShare;
            int score = CosechaContract.Score(share, _dda.PeakLevel);
            ShowResult(score);

            var telemetry = new StroopTelemetry
            {
                user_id = _config.user_id,
                game_id = _config.game_id,
                session_metrics = new StroopSessionMetrics
                {
                    correct_trials = _tally.CosechasWon,
                    total_trials = _tally.Cosechas,
                    calculated_score = score,
                    average_response_time_ms = 0,
                    level = _config.config.level,
                    timed = _config.config.timed,
                    end_rating = _dda.RatingNormalized,
                    mode_trials = _dda.ScoredTrials,
                    mode_hits = _dda.ScoredCorrect,
                    peak_level = _dda.PeakLevel,
                    harv_words = _tally.Words,
                    harv_common_found = _tally.CommonFound,
                    harv_common_total = _tally.CommonTotal,
                    harv_cluster_pct = _tally.ClusterPercent,
                    harv_first20 = _tally.First20,
                    harv_last20 = _tally.Last20,
                    harv_star = _tally.Star,
                    harv_best = _tally.Best,
                    harv_missed = string.Join(",", _tally.Missed),
                    harv_hints = _tally.Hints
                }
            };
            NativeBridge.ForwardTelemetryToPlatform(JsonUtility.ToJson(telemetry));
            yield break;
        }

        private void ShowResult(int score)
        {
            _exit.Show();
            _resultRoot.Find("Title").GetComponent<Text>().text = score >= 80 ? "¡Gran cosecha!" : score >= 55 ? "¡Buena cosecha!" : "Cosecha completa";
            _resultRoot.Find("Detail").GetComponent<Text>().text = $"{_tally.Words} palabras · de las comunes, {_tally.CommonFound} de {_tally.CommonTotal}";
            _resultRoot.Find("Extra").GetComponent<Text>().text = _tally.Star.Length > 0 ? $"Palabra estrella: {_tally.Star}" : (_tally.Best.Length > 0 ? $"Tu palabra más larga: {_tally.Best}" : "");
            _resultRoot.gameObject.SetActive(true);
            StartCoroutine(AnimateResult(score));
        }

        // ------------------------------------------------------------------ construcción de UI

        protected override void BuildUi()
        {
            var canvasGo = new GameObject("CosechaCanvas");
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
            WorldBackdrop.Build(bgRect, GameWorld.Huerto);
            _stars = bgRect.GetComponentInChildren<StarfieldFx>();

            var safeGo = new GameObject("SafeAreaContent");
            safeGo.transform.SetParent(canvasGo.transform, false);
            _safe = safeGo.AddComponent<RectTransform>();
            ApplySafeArea(_safe);

            _hud = new GameHud(_safe, "Cosecha de palabras", MarginU, this, withStreak: false);
            BuildTimer();

            var playGo = new GameObject("Play");
            playGo.transform.SetParent(_safe, false);
            _play = playGo.AddComponent<RectTransform>();
            Stretch(_play);

            _orbitRing = NewImage(_play, "OrbitRing", RingSprite.Get());
            _backLayer = Layer(_play, "BackTiles");
            BuildPlanet();
            _plantsLayer = Layer(_play, "Plants");
            _frontLayer = Layer(_play, "FrontTiles");
            BuildTiles();
            BuildLabels();
            BuildTray();
            BuildButtons();

            _fxRect = Layer(_play, "Fx");
            BuildReady();
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

        private void BuildPlanet()
        {
            var go = new GameObject("Planet");
            go.transform.SetParent(_play, false);
            _planetRect = go.AddComponent<RectTransform>();
            _planetRect.anchorMin = _planetRect.anchorMax = new Vector2(0.5f, 0.5f);
            _planetRect.pivot = new Vector2(0.5f, 0.5f);
            _atmosImg = NewImage(_planetRect, "Atmosphere", RadialGlowSprite.Get());
            _atmosImg.color = new Color(0.59f, 0.89f, 0.75f, 0.28f);
            _atmosImg.gameObject.SetActive(true);
            _planetImg = NewImage(_planetRect, "Earth", CosechaSprites.Planet());
            _planetImg.gameObject.SetActive(true);
            _tuftsImg = NewImage(_planetRect, "Tufts", CosechaSprites.Tufts());
            _tuftsImg.gameObject.SetActive(true);
            _grassImg = NewImage(_planetRect, "Grass", CosechaSprites.GrassCap());
            _grassImg.color = new Color(1f, 1f, 1f, 0f);
            _grassImg.gameObject.SetActive(true);
            go.SetActive(false);
        }

        private void BuildTiles()
        {
            for (int i = 0; i < CosechaContract.LetterCount; i++)
            {
                var img = NewImage(_frontLayer, "Tile" + i, CosechaSprites.Moon());
                img.gameObject.SetActive(true);
                var letter = MakeText(img.rectTransform, "Letter", (int)(CosechaContract.LetterSizeSp(Senior) * UnitsPerDp), TextAnchor.MiddleCenter, NeuroStyle.Ink, 0f, 0f);
                letter.font = UiFonts.Word;
                letter.horizontalOverflow = HorizontalWrapMode.Overflow;
                letter.verticalOverflow = VerticalWrapMode.Overflow;
                var badge = NewImage(img.rectTransform, "Badge", DiscSprite.Get());
                badge.color = NeuroStyle.Sky;
                badge.rectTransform.sizeDelta = new Vector2(72f, 72f);
                badge.rectTransform.anchorMin = badge.rectTransform.anchorMax = new Vector2(0.82f, 0.9f);
                badge.rectTransform.anchoredPosition = Vector2.zero;
                var bt = MakeText(badge.rectTransform, "N", 48, TextAnchor.MiddleCenter, NeuroStyle.Ink, 0f, 0f);
                bt.horizontalOverflow = HorizontalWrapMode.Overflow;
                var ring = NewImage(img.rectTransform, "HintRing", RingSprite.Get());
                ring.color = NeuroStyle.Sun;
                ring.rectTransform.anchorMin = Vector2.zero;
                ring.rectTransform.anchorMax = Vector2.one;
                ring.rectTransform.offsetMin = new Vector2(-24f, -24f);
                ring.rectTransform.offsetMax = new Vector2(24f, 24f);
                _tiles.Add(new Tile { Rect = img.rectTransform, Img = img, Letter = letter, Badge = badge, BadgeText = bt, HintRing = ring });
            }
        }

        private void BuildLabels()
        {
            _cosechaLabel = CenteredText(_play, "CosechaLabel", 52, NeuroStyle.Sky);
            _cosechaLabel.font = UiFonts.Regular;
            _listTitle = CenteredText(_play, "ListTitle", 56, NeuroStyle.Sun);
            _listWords = CenteredText(_play, "ListWords", 56, Color.white);
            _listWords.font = UiFonts.Regular;
            _msg = CenteredText(_play, "Msg", 56, Color.white);
            NeuroStyle.ClayText(_msg, 3f, 4f);
            _msg.gameObject.SetActive(false);
        }

        private void BuildTray()
        {
            var go = new GameObject("Tray");
            go.transform.SetParent(_play, false);
            _trayRect = go.AddComponent<RectTransform>();
            _trayRect.anchorMin = _trayRect.anchorMax = new Vector2(0.5f, 0.5f);
            _trayRect.pivot = new Vector2(0.5f, 0.5f);
            var img = go.AddComponent<Image>();
            img.sprite = RoundedRectSprite.Get(48);
            img.type = Image.Type.Sliced;
            img.color = PlateColor;
            img.raycastTarget = false;
            NeuroStyle.ClayFrame(img, 5f, 9f);
            for (int i = 0; i < CosechaContract.LetterCount; i++)
            {
                var t = MakeText(_trayRect, "Slot" + i, (int)(CosechaContract.LetterSizeSp(Senior) * UnitsPerDp), TextAnchor.MiddleCenter, NeuroStyle.Ink, 0f, 0f);
                t.font = UiFonts.Word;
                t.horizontalOverflow = HorizontalWrapMode.Overflow;
                t.verticalOverflow = VerticalWrapMode.Overflow;
                var r = t.rectTransform;
                r.anchorMin = r.anchorMax = new Vector2(0.5f, 0.5f);
                r.pivot = new Vector2(0.5f, 0.5f);
                r.sizeDelta = new Vector2(120f, 130f);
                _slots.Add(t);
                var line = NewImage(_trayRect, "Line" + i, RoundedRectSprite.Get(6));
                line.type = Image.Type.Sliced;
                line.color = new Color(0.74f, 0.71f, 0.83f, 1f);
                line.rectTransform.sizeDelta = new Vector2(70f, 12f);
                line.gameObject.SetActive(true);
                _slotLines.Add(line);
            }
            go.SetActive(false);
        }

        private void BuildButtons()
        {
            _btnSow = MakeButton("Sembrar", GoodColor, 78, CosechaSprites.SeedIcon());
            _btnClear = MakeButton("Borrar", Color.white, 46, CosechaSprites.BackIcon());
            _btnSow.gameObject.SetActive(false);
            _btnClear.gameObject.SetActive(false);
        }

        private RectTransform MakeButton(string label, Color color, int fontPx, Sprite icon)
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
            ic.gameObject.SetActive(true);
            var t = MakeText(r, "Label", fontPx, TextAnchor.MiddleCenter, NeuroStyle.Ink, 0f, 0f);
            t.horizontalOverflow = HorizontalWrapMode.Overflow;
            t.text = label;
            return r;
        }

        private void BuildReady()
        {
            _readyDim = NewImage(_safe, "ReadyDim", null);
            Stretch(_readyDim.rectTransform);
            _readyDim.color = new Color(0f, 0f, 0.05f, 0f);
            _readyTitle = CenteredText(_safe, "ReadyTitle", 130, NeuroStyle.Sun);
            NeuroStyle.ClayText(_readyTitle, 6f, 10f);
            _readyTitle.rectTransform.sizeDelta = new Vector2(980f, 190f);
            _readyTitle.rectTransform.anchoredPosition = new Vector2(0f, 130f);
            _readySub = CenteredText(_safe, "ReadySub", 60, Color.white);
            _readySub.rectTransform.sizeDelta = new Vector2(980f, 130f);
            _readySub.rectTransform.anchoredPosition = new Vector2(0f, -30f);
            _readyDim.gameObject.SetActive(false);
            _readyTitle.gameObject.SetActive(false);
            _readySub.gameObject.SetActive(false);
        }

        private static Text CenteredText(Transform parent, string name, int size, Color color)
        {
            var t = MakeText(parent, name, size, TextAnchor.MiddleCenter, color, 0f, 0f);
            var r = t.rectTransform;
            r.anchorMin = r.anchorMax = new Vector2(0.5f, 0.5f);
            r.pivot = new Vector2(0.5f, 0.5f);
            t.horizontalOverflow = HorizontalWrapMode.Overflow;
            t.verticalOverflow = VerticalWrapMode.Overflow;
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
            float w = _playW - MarginU * 2f;

            // arriba: cosecha y tiempo, la lista de palabras y la bandeja
            _cosechaLabel.rectTransform.sizeDelta = new Vector2(w, 80f);
            _cosechaLabel.rectTransform.anchoredPosition = new Vector2(0f, top - (GameHud.Height + 78f));
            _listTitle.rectTransform.sizeDelta = new Vector2(w, 80f);
            _listTitle.rectTransform.anchoredPosition = new Vector2(0f, top - (GameHud.Height + 170f));
            _listWords.rectTransform.sizeDelta = new Vector2(w, 80f);
            _listWords.rectTransform.anchoredPosition = new Vector2(0f, top - (GameHud.Height + 245f));

            float trayH = 190f;
            _trayY = top - (GameHud.Height + 405f);
            _trayRect.sizeDelta = new Vector2(w, trayH);
            _trayRect.anchoredPosition = new Vector2(0f, _trayY);
            _slotStep = (w - 70f) / CosechaContract.LetterCount;
            for (int i = 0; i < CosechaContract.LetterCount; i++)
            {
                _slots[i].rectTransform.anchoredPosition = new Vector2(SlotX(i), 4f);
                _slotLines[i].rectTransform.anchoredPosition = new Vector2(SlotX(i), -42f);
            }
            _msg.rectTransform.sizeDelta = new Vector2(w, 100f);
            _msg.rectTransform.anchoredPosition = new Vector2(0f, _trayY - trayH * 0.5f - 52f);
            BestFit(_msg, 42);

            // abajo: los botones
            float btnH = 190f;
            float by = bottom + 70f + btnH * 0.5f;
            float sowW = (w - 24f) * 0.62f, clrW = (w - 24f) - sowW;
            _btnSowSize = new Vector2(sowW, btnH);
            _btnClearSize = new Vector2(clrW, btnH * 0.82f);
            _btnSowCenter = new Vector2(-(w * 0.5f) + sowW * 0.5f, by);
            _btnClearCenter = new Vector2(w * 0.5f - clrW * 0.5f, by);
            _btnSow.sizeDelta = _btnSowSize;
            _btnClear.sizeDelta = _btnClearSize;
            _btnSow.anchoredPosition = _btnSowCenter;
            _btnClear.anchoredPosition = _btnClearCenter;
            LayoutButton(_btnSow, _btnSowSize, 120f, -34f);
            LayoutButton(_btnClear, _btnClearSize, 70f, 0f, stacked: true);

            // en medio: el planeta y la órbita
            float trayBottom = _trayY - trayH * 0.5f - 60f;
            float buttonsTop = by + btnH * 0.5f + 20f;
            _orbitCy = (trayBottom + buttonsTop) * 0.5f;
            float tile = TileUnits;
            _ry = Mathf.Min(250f, (trayBottom - buttonsTop) * 0.5f - tile * 0.5f);
            _rx = w * 0.5f - tile * 0.5f;
            _orbitCx = 0f;
            _planetR = Mathf.Min(205f, _ry * 0.82f);
            _planetRect.anchoredPosition = new Vector2(_orbitCx, _orbitCy);
            float side = 2f * _planetR * CosechaSprites.PlanetSpriteToRadius;
            _planetRect.sizeDelta = new Vector2(side, side);
            foreach (var img in new[] { _planetImg, _tuftsImg, _grassImg })
            {
                Stretch(img.rectTransform);
            }
            _atmosImg.rectTransform.sizeDelta = new Vector2(_planetR * 3.1f, _planetR * 3.1f);
            _orbitRing.rectTransform.sizeDelta = new Vector2(_rx * 2f, _ry * 2f);
            _orbitRing.rectTransform.anchoredPosition = new Vector2(_orbitCx, _orbitCy);
            _orbitRing.color = NeuroStyle.WithAlpha(NeuroStyle.Sky, 0.22f);
            _planetRect.pivot = new Vector2(0.5f, 0.5f);
            PositionTiles(true);
            AnimateAmbient();
        }

        /// <summary>Ícono y rótulo del botón: en fila (Sembrar) o uno sobre el otro (Borrar, que es angosto).</summary>
        private static void LayoutButton(RectTransform r, Vector2 size, float iconSize, float labelShift, bool stacked = false)
        {
            var icon = r.Find("Icon").GetComponent<RectTransform>();
            icon.sizeDelta = new Vector2(iconSize, iconSize);
            var label = r.Find("Label").GetComponent<RectTransform>();
            label.anchorMin = label.anchorMax = new Vector2(0.5f, 0.5f);
            if (stacked)
            {
                icon.anchoredPosition = new Vector2(0f, size.y * 0.5f - iconSize * 0.5f - 22f);
                label.sizeDelta = new Vector2(size.x - 20f, 60f);
                label.anchoredPosition = new Vector2(0f, -size.y * 0.5f + 44f);
                return;
            }
            icon.anchoredPosition = new Vector2(-size.x * 0.5f + iconSize * 0.5f + 40f, 0f);
            label.sizeDelta = new Vector2(size.x - iconSize - 60f, size.y);
            label.anchoredPosition = new Vector2(iconSize * 0.5f + labelShift * 0.0f, 0f);
        }
    }
}
