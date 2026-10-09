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

namespace NeuroVida.Games.Meteoros
{
    /// <summary>
    /// "Lluvia de meteoros": juego estrella de Lenguaje (decisión léxica "ir / no ir", ver <see cref="MeteorContract"/> y
    /// docs/diseno-lluvia-de-meteoros.md). Caen meteoros de arcilla con una palabra en una placa crema: se TOCAN las que
    /// existen y se DEJAN PASAR las inventadas. Una palabra tocada estalla en estrellas que van a la constelación de arriba;
    /// una inventada tocada se agrieta en polvo con ✗ y la palabra tachada; una inventada que pasa se deshace en chispas
    /// suaves al llegar a la atmósfera (acierto, sin castigo) y una palabra que se va queda anotada ("se fue: brújula").
    /// <list type="bullet">
    /// <item>Meteoro dorado (1 de cada 12): una palabra más rara, con aro sol y el doble de puntos.</item>
    /// <item>Lluvia de estrellas (cada 25): 6 palabras comunes y rápidas, fuera de la escalera de dificultad.</item>
    /// <item>Racha de 8 o más: la estela se enciende (× 1,5) hasta el próximo error.</item>
    /// <item>Medidas propias: "tu vocabulario" por banda, "tu reconocimiento", "tu filtro" y "tu colección" (la app las lee).</item>
    /// </list>
    /// Reto = 2 minutos; Precisión = 40 meteoros, caída más lenta y máximo 2 a la vez.
    /// </summary>
    public class MeteorGameController : GameControllerBase
    {
        public const string GameId = MeteorContract.GameId;

        private const float UnitsPerDp = 3f;
        private const float MarginU = 60f;

        private static readonly Color GoodColor = NeuroStyle.Lime;
        private static readonly Color BadColor = NeuroStyle.Coral;
        private static readonly Color AmberColor = NeuroStyle.Sun;

        private enum Phase { Idle, Playing, Done }

        private sealed class Meteor
        {
            public MeteorSpec Spec;
            public RectTransform Root, Rock, Plaque, TrailRect, AuraRect;
            public Image RockImg, TrailImg, AuraImg, GlowImg;
            public Text Label;
            public Vector2 Pos, Vel;
            public float Spawned, Rx, Ry, Wobble, TrailBase;
            public int Shape;
            public RockTint Tint;
            public bool Done;
        }

        private System.Random _rng;
        private AdaptiveDifficulty _dda;
        private MeteorLexicon _lexicon;
        private MeteorDirector _director;
        private MeteorTally _tally;
        private Phase _phase = Phase.Idle;
        // la versión corta del inicio (Assessment) corre con reloj de 60 s, aunque la config traiga timed = false
        private bool Endless => _config != null && (_config.config.timed || Assessment.Active);
        private int RunSeconds => MeteorContract.RunSeconds(Assessment.Active);
        private bool Precision => !Endless;
        private bool Senior;
        private int _previousFrameRate;

        private readonly List<Meteor> _active = new List<Meteor>();
        private readonly List<int> _allMs = new List<int>();
        private int _streak, _bestStreak, _points, _rescued, _resolved;
        private float _endsAt, _lastSpawn;
        private int _lastTickSecond = -1;
        private bool _hintDone, _wasShower;

        // UI
        private RectTransform _safe, _play, _fxRect, _meteorLayer, _timerBg, _timerFill, _atmosphere;
        private Text _hint, _prompt;
        private readonly List<Image> _conStars = new List<Image>();
        private Toast _toast;
        private ExitButton _exit;
        private GameHud _hud;
        private CountdownScreen _countdown;
        private float _playW, _playH, _spawnY, _atmosphereY, _bottom, _hudBottomY;
        private RectTransform _toastKeepOut;
        private Vector2 _starTarget;

        // ------------------------------------------------------------------ sesión

        public void StartSession(SequenceInitConfig config)
        {
            _config = config;
            _rng = new System.Random();
            var age = DdaUserProfileConfig.ParseAgeBand(config.config.age_band);
            Senior = age == AgeBand.Senior;
            // ~60 decisiones en el Reto: pasos de tamaño común. Sin tiempo de reacción: cuenta acertar, no la prisa.
            _dda = MeteorContract.CreateEngine(config.config);
            _lexicon = MeteorLexicon.Load();
            _director = new MeteorDirector(_lexicon, _rng);
            _tally = new MeteorTally();

            _previousFrameRate = Application.targetFrameRate;
            Application.targetFrameRate = 60;

            _phase = Phase.Idle;
            ClearMeteors();
            _allMs.Clear();
            _streak = _bestStreak = _points = _rescued = _resolved = 0;
            _endsAt = _lastSpawn = 0f;
            _lastTickSecond = -1;
            _hintDone = false;
            _wasShower = false;
            _pending = null;
            _shrunkWords.Clear();

            _resultRoot.gameObject.SetActive(false);
            _exit.Hide();
            _timerBg.gameObject.SetActive(Endless);
            _hud.SetStreak(0);
            _hint.gameObject.SetActive(false);
            SetPrompt("", Color.white);
            SetConstellation(0);
            _loopOn = false;
            _guided = false;
            _tutorial.Hide();

            StopAllCoroutines();
            StartCoroutine(GameLoop());
        }

        private void OnDisable()
        {
            if (_previousFrameRate != 0) Application.targetFrameRate = _previousFrameRate;
        }

        private IEnumerator GameLoop()
        {
            StartCoroutine(PrewarmSprites());
            bool tutorialPlayed = false;
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
                tutorialPlayed = !_tutorial.Skipped;
            }
            _safe.gameObject.SetActive(false);
            yield return StartCoroutine(_countdown.Play("Lluvia de meteoros", Assessment.Subtitle("Prepárate"), () => _safe.gameObject.SetActive(true)));
            _safe.gameObject.SetActive(true);
            yield return null;
            ApplySafeArea(_safe);
            Canvas.ForceUpdateCanvases();
            Layout();
            UpdateHud();

            _endsAt = GameClock.Time + RunSeconds;
            _lastSpawn = GameClock.Time - 2f;
            _phase = Phase.Playing;
            _hint.gameObject.SetActive(!tutorialPlayed);       // con el tutorial hecho, la regla ya se vio
            _hint.text = "Toca las palabras que existen\nLas inventadas, déjalas pasar";
            _loopOn = true;
            yield return StartCoroutine(MainLoop());
        }

        /// <summary>El bucle de la partida: va soltando meteoros hasta que se acabe el tiempo (o los meteoros). «Cómo se juega» lo retoma desde acá.</summary>
        private IEnumerator MainLoop()
        {
            while (!Finished())
            {
                int level = _dda.PresentedLevel;
                bool shower = _director.InShower;
                bool more = Endless ? GameClock.Time < _endsAt : (_director.Spawned < MeteorContract.PrecisionMeteors || shower);
                int cap = shower ? 4 : Assessment.Active ? MeteorContract.AssessmentConcurrent(level) : MeteorContract.Concurrent(level, Precision);
                // versión corta: nunca menos de 2 en pantalla (el siguiente sale enseguida); la partida normal usa las pausas de siempre
                float gap = MeteorContract.SpawnGap(shower, Assessment.Active, _active.Count);
                if (more && _active.Count < cap && GameClock.Time - _lastSpawn >= gap)
                {
                    bool placed = Spawn();
                    _lastSpawn = placed ? GameClock.Time : GameClock.Time - gap + 0.15f; // sin lugar libre: se reintenta enseguida
                    if (placed && !_wasShower && _director.InShower)
                    {
                        _toast.Show("¡Lluvia de estrellas!", "Toca sin pensar: son palabras comunes", AmberColor, 1.4f);
                        GameFeel.LevelUp();
                    }
                    if (placed) _wasShower = _director.InShower;
                }
                yield return null;
            }
            _loopOn = false;
            if (Assessment.Active)
            {
                // versión corta del inicio: el tiempo solo impide soltar OTRO meteoro; los que ya caen se resuelven (antes se cortaban a mitad y salía «¡Listo!»)
                float limit = GameClock.Time + 14f;
                while (_active.Count > 0 && GameClock.Time < limit) yield return null;
            }
            if (Endless) ClearMeteors(); // se acabó el tiempo: lo que quedaba en el cielo no cuenta
            yield return StartCoroutine(FinishGame());
        }

        /// <summary>Hornea las rocas, la estela y las estrellas de a una por cuadro mientras corre la cuenta regresiva
        /// (así el primer meteoro no traba el juego).</summary>
        private IEnumerator PrewarmSprites()
        {
            int step = 0;
            MeteorSprites.StarLit();
            yield return null;
            MeteorSprites.StarDim();
            yield return null;
            MeteorSprites.HeatTrail(!GameFeel.ReduceMotion);
            yield return null;
            while (MeteorSprites.Prewarm(ref step)) yield return null;
            MeteorSprites.Rock(0, RockTint.Dust);
        }

        private bool Finished()
        {
            if (Endless) return GameClock.Time >= _endsAt;
            return _director.Spawned >= MeteorContract.PrecisionMeteors && !_director.InShower && _active.Count == 0;
        }

        // ------------------------------------------------------------------ meteoros

        private MeteorSpec _pending;
        private const float SeparationU = 28f;

        /// <summary>Palabras que se achicaron para caber ("murciélago 24"), para anotarlas en las pruebas.</summary>
        private readonly List<string> _shrunkWords = new List<string>();

        /// <summary>Saca el próximo meteoro. Devuelve false si por ahora no hay lugar donde no se pise con otro (se reintenta en
        /// unos cuadros; la palabra elegida queda pendiente).</summary>
        private bool Spawn()
        {
            int level = _dda.PresentedLevel;
            if (_pending == null) _pending = _director.Next(level, Precision, Senior);
            var spec = _pending;
            var m = new Meteor { Spec = spec, Spawned = GameClock.Time, Wobble = (float)_rng.NextDouble() * 6.28f, Shape = _rng.Next(MeteorSprites.ShapeCount) };
            m.Tint = spec.Golden ? RockTint.Gold : (RockTint)_rng.Next(3);

            var root = new GameObject("Meteor");
            root.transform.SetParent(_meteorLayer, false);
            m.Root = root.AddComponent<RectTransform>();
            m.Root.anchorMin = m.Root.anchorMax = new Vector2(0.5f, 0.5f);
            m.Root.sizeDelta = Vector2.zero;

            int baseDp = MeteorContract.WordSizeDp(Senior);
            int fontPx = baseDp * (int)UnitsPerDp;
            // estela de calor (detrás de todo)
            m.TrailImg = NewImage(m.Root, "Trail", MeteorSprites.HeatTrail(!GameFeel.ReduceMotion));
            m.TrailRect = m.TrailImg.rectTransform;
            m.TrailRect.pivot = new Vector2(0.5f, 0f);
            m.TrailImg.gameObject.SetActive(true);
            if (spec.Golden)
            {
                m.GlowImg = NewImage(m.Root, "Glow", RadialGlowSprite.Get());
                m.GlowImg.color = NeuroStyle.WithAlpha(NeuroStyle.Sun, 0.55f);
                m.GlowImg.gameObject.SetActive(true);
            }
            m.RockImg = NewImage(m.Root, "Rock", MeteorSprites.Rock(m.Shape, m.Tint));
            m.Rock = m.RockImg.rectTransform;
            m.RockImg.gameObject.SetActive(true);
            if (spec.Golden)
            {
                m.AuraImg = NewImage(m.Root, "Aura", RingSprite.Get());
                m.AuraImg.color = NeuroStyle.Sun;
                m.AuraRect = m.AuraImg.rectTransform;
                m.AuraImg.gameObject.SetActive(true);
            }

            // placa crema con la palabra (siempre horizontal, aunque la roca se mueva)
            var plaqueGo = new GameObject("Plaque");
            plaqueGo.transform.SetParent(m.Root, false);
            m.Plaque = plaqueGo.AddComponent<RectTransform>();
            m.Plaque.anchorMin = m.Plaque.anchorMax = new Vector2(0.5f, 0.5f);
            var pimg = plaqueGo.AddComponent<Image>();
            pimg.sprite = RoundedRectSprite.Get(24);
            pimg.type = Image.Type.Sliced;
            pimg.color = new Color(1f, 0.973f, 0.925f, 1f);
            pimg.raycastTarget = false;
            NeuroStyle.ClayFrame(pimg, 4f, 7f);
            m.Label = MakeText(m.Plaque, "Word", fontPx, TextAnchor.MiddleCenter, NeuroStyle.Ink, 0f, 0f);
            m.Label.font = UiFonts.Word;
            m.Label.horizontalOverflow = HorizontalWrapMode.Overflow;
            m.Label.verticalOverflow = VerticalWrapMode.Overflow;
            m.Label.text = spec.Word;
            Stretch(m.Label.rectTransform);
            float textW = m.Label.preferredWidth;
            // una palabra larga que no cabe a lo ancho achica su letra de a 1 dp (nunca bajo 22 dp); lo normal: 26 dp (30 en mayores)
            int dp = MeteorContract.FitWordDp(baseDp, textW, _playW - 2f * MarginU);
            if (dp != baseDp)
            {
                fontPx = dp * (int)UnitsPerDp;
                m.Label.fontSize = fontPx;
                textW = m.Label.preferredWidth;
                _shrunkWords.Add(spec.Word + " " + dp);
            }
            m.Plaque.sizeDelta = new Vector2(textW + MeteorContract.PlaquePadU, fontPx * 1.5f);

            // la roca es grande: la placa ocupa ~1/3 de su alto
            var size = MeteorContract.RockSize(m.Plaque.sizeDelta.x, m.Plaque.sizeDelta.y);
            float rockW = size.w, rockH = size.h;
            m.Rock.sizeDelta = new Vector2(rockW, rockH) * MeteorSprites.ImageScale;
            m.Rx = rockW * 0.5f;
            m.Ry = rockH * 0.5f;
            if (m.GlowImg != null) m.GlowImg.rectTransform.sizeDelta = new Vector2(rockW * 1.5f, rockH * 1.5f);
            if (m.AuraRect != null) m.AuraRect.sizeDelta = new Vector2(rockW + 70f, rockH + 70f);
            m.TrailBase = rockW * 0.55f;

            // recorrido: arriba, en un lugar donde no se pise con otro meteoro, hasta un punto de la atmósfera (diagonal suave)
            float half = Mathf.Max(40f, _playW * 0.5f - MarginU - m.Rx);
            float startY = SpawnCenterY(m);
            float dropH = startY - _atmosphereY;
            bool placed = false;
            float sx = 0f, tx = 0f;
            for (int tries = 0; tries < 14 && !placed; tries++)
            {
                sx = Mathf.Lerp(-half, half, (float)_rng.NextDouble());
                tx = Mathf.Lerp(-half, half, (float)_rng.NextDouble());
                // la diagonal no pasa de un tercio del recorrido hacia el lado: se ve como caída, no como vuelo
                tx = Mathf.Clamp(tx, sx - dropH * 0.35f, sx + dropH * 0.35f);
                tx = Mathf.Clamp(tx, -half, half);
                var v = new Vector2((tx - sx) / spec.FallSeconds, -dropH / spec.FallSeconds);
                placed = !CollidesWithActive(m, new Vector2(sx, startY), v);
            }
            if (!placed)
            {
                Destroy(m.Root.gameObject);
                return false;
            }
            _pending = null;
            m.Pos = new Vector2(sx, startY);
            m.Vel = new Vector2((tx - sx) / spec.FallSeconds, -dropH / spec.FallSeconds);
            m.Root.anchoredPosition = m.Pos;
            ApplyTrail(m);
            _active.Add(m);
            StartCoroutine(PopIn(m.Root, 0.2f));
            return true;
        }

        /// <summary>El centro de la roca al nacer: su borde de arriba queda justo DEBAJO del marcador (antes nacían con la roca asomando por encima de él y tapaban el título y los rótulos).</summary>
        private float SpawnCenterY(Meteor m) => Mathf.Min(_spawnY + m.Ry, _hudBottomY - 20f - m.Ry);

        /// <summary>¿El recorrido de [m] (de [pos] con [vel]) pisaría la roca de otro meteoro en algún momento de la caída de
        /// ambos? Se mira cada 0,2 s con la caja de las dos rocas más un poco de aire.</summary>
        private bool CollidesWithActive(Meteor m, Vector2 pos, Vector2 vel)
        {
            foreach (var o in _active)
            {
                if (o.Done) continue;
                float oLeft = Mathf.Max(0f, (o.Pos.y - _atmosphereY) / Mathf.Max(1f, -o.Vel.y));
                float horizon = Mathf.Min(m.Spec.FallSeconds, oLeft + 0.4f);
                for (float t = 0f; t <= horizon; t += 0.2f)
                {
                    var a = pos + vel * t;
                    var b = o.Pos + o.Vel * t;
                    if (MeteorContract.Overlaps(a.x, a.y, m.Rx, m.Ry, b.x, b.y, o.Rx, o.Ry, SeparationU)) return true;
                }
            }
            return false;
        }

        /// <summary>Estela de calor: más larga y ancha con la racha encendida; corta, sin chispas y quieta con "quitar animaciones".</summary>
        private void ApplyTrail(Meteor m)
        {
            bool glow = _streak >= MeteorContract.StreakGlow;
            float len = (glow ? 620f : 430f) * (GameFeel.ReduceMotion ? 0.5f : 1f);
            Vector2 back = m.Vel.sqrMagnitude > 0.01f ? -m.Vel.normalized : Vector2.up;
            float width = m.TrailBase * (glow ? 1.35f : 1f);
            // la estela crece al caer: nunca sube más arriba del borde de abajo del marcador. Cuenta también el ancho: con la estela girada (la roca cae en diagonal) sus esquinas suben más que su punta, y la caja de la estela llegaba a
            // tapar el «1 de 40» del marcador (lo vio el smoke cuando el toque de verdad dejó la roca recién nacida un instante más)
            if (_hudBottomY != 0f) len = Mathf.Min(len, Mathf.Max(0f, (_hudBottomY - 8f - m.Pos.y - width * 0.5f * Mathf.Abs(back.x)) / Mathf.Max(0.3f, back.y)));
            m.TrailRect.sizeDelta = new Vector2(width, len);
            m.TrailImg.color = new Color(1f, 1f, 1f, glow ? 1f : 0.85f);
            m.TrailRect.localRotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(-back.x, back.y) * Mathf.Rad2Deg);
        }

        private void RefreshTrails()
        {
            foreach (var m in _active) if (!m.Done) ApplyTrail(m);
        }

        private void MoveMeteors()
        {
            float dt = GameClock.DeltaTime;
            float t = GameClock.Time;
            for (int i = _active.Count - 1; i >= 0; i--)
            {
                var m = _active[i];
                if (m.Done) continue;
                m.Pos += m.Vel * dt;
                m.Root.anchoredPosition = m.Pos;
                ApplyTrail(m);                                                  // el largo de la estela depende de qué tan abajo del marcador va la roca
                if (!GameFeel.ReduceMotion) m.Rock.localRotation = Quaternion.Euler(0f, 0f, 7f * Mathf.Sin(t * 0.9f + m.Wobble));
                if (m.Pos.y <= _atmosphereY + m.Ry * 0.35f) ResolvePass(m);
            }
        }

        // ------------------------------------------------------------------ entrada

        private void Update()
        {
            if (PollTutorialSkip()) return;             // un toque en «Saltar tutorial» no es un toque a un meteoro
            GuidedTutorial.SpinHint(_guidedRing);
            UpdateClock();
            if (_phase != Phase.Playing || GameClock.DeltaTime <= 0f) return;
            MoveMeteors();
            bool pressed = false;
            Vector2 pos = Vector2.zero;
            for (int i = 0; i < Input.touchCount; i++)
            {
                var t = Input.GetTouch(i);
                if (t.phase == TouchPhase.Began) { pressed = true; pos = t.position; if (!TapBlocked(pos)) TryTap(pos); }
            }
            if (!pressed && Input.touchCount == 0 && Input.GetMouseButtonDown(0) && !TapBlocked(Input.mousePosition)) TryTap(Input.mousePosition);
        }

        private void TryTap(Vector2 screenPos)
        {
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(_play, screenPos, null, out var local)) return;
            // La zona de toque es la roca + 12 dp (mínimo 56 dp de diámetro; 64 en mayores): el toque cuenta al PRESIONAR.
            float pad = 12f * UnitsPerDp;
            float minR = MeteorContract.MinTouchDp(Senior) * UnitsPerDp * 0.5f;
            Meteor best = null;
            float bestV = float.MaxValue;
            foreach (var m in _active)
            {
                if (m.Done) continue;
                float rx = Mathf.Max(m.Rx + pad, minR), ry = Mathf.Max(m.Ry + pad, minR);
                float dx = (local.x - m.Pos.x) / rx, dy = (local.y - m.Pos.y) / ry;
                float v = dx * dx + dy * dy;
                if (v <= 1f && v < bestV) { bestV = v; best = m; }
            }
            if (best != null) ResolveTap(best);
        }

        private void UpdateClock()
        {
            if (!Endless || _phase != Phase.Playing || _endsAt <= 0f) return;
            float left = _endsAt - GameClock.Time;
            SetTimerFraction(Mathf.Clamp01(left / RunSeconds));
            int whole = Mathf.CeilToInt(left);
            if (whole <= 5 && whole >= 1 && whole != _lastTickSecond)
            {
                _lastTickSecond = whole;
                GameFeel.Tick();
            }
        }

        // ------------------------------------------------------------------ resolver

        private void ResolveTap(Meteor m)
        {
            if (_guided) { GuidedResolve(m, true); return; }   // la ronda guiada no cuenta: nada de lo de abajo
            m.Done = true;
            _active.Remove(m);
            var spec = m.Spec;
            int ms = Mathf.RoundToInt((GameClock.Time - m.Spawned) * 1000f);
            bool hit = spec.IsWord;
            if (spec.IsWord) _tally.AddWord(spec, true, ms); else _tally.AddDecoy(spec, true);
            if (!spec.Shower && spec.IsWord) _allMs.Add(ms);
            if (!spec.Shower) _resolved++;
            Register(hit, spec);
            if (hit)
            {
                _streak++;
                _bestStreak = Mathf.Max(_bestStreak, _streak);
                int pts = MeteorContract.Points(true, _dda.PresentedLevel, _streak, spec.Golden, spec.Shower);
                _points += pts;
                _rescued++;
                GameFeel.Correct(spec.Golden ? Mathf.Max(_streak, 6) : _streak);
                StartCoroutine(RescueEffect(m, pts));
                if (_streak == MeteorContract.StreakGlow) RefreshTrails();
            }
            else
            {
                BreakStreak();
                GameFeel.Wrong();
                StartCoroutine(CrackEffect(m));
            }
            AfterResolve();
        }

        private void ResolvePass(Meteor m)
        {
            if (_guided) { GuidedResolve(m, false); return; }
            m.Done = true;
            _active.Remove(m);
            var spec = m.Spec;
            if (!spec.Shower) _resolved++;
            if (!spec.IsWord)
            {
                // inventada que se deja pasar: acierto, sin ruido
                _tally.AddDecoy(spec, false);
                Register(true, spec);
                _streak++;
                _bestStreak = Mathf.Max(_bestStreak, _streak);
                _points += MeteorContract.Points(false, _dda.PresentedLevel, _streak, false, false);
                PlayTone(330f, 0.12f, 0.03f);
                StartCoroutine(DissolveEffect(m));
                if (_streak == MeteorContract.StreakGlow) RefreshTrails();
            }
            else
            {
                // palabra que se fue: queda anotada, sin culpa
                _tally.AddWord(spec, false, -1);
                Register(false, spec);
                if (!spec.Shower) BreakStreak();
                StartCoroutine(MissedEffect(m));
            }
            AfterResolve();
        }

        private void Register(bool correct, MeteorSpec spec)
        {
            if (spec.Shower) return; // la lluvia de estrellas va fuera de la escalera
            var change = _dda.Register(correct);
            if (change == DdaChange.Up) _toast.Show("¡Subes de nivel!", MeteorContract.LevelNews(_dda.Level), GoodColor, 1.1f);
            else if (change == DdaChange.Down || _dda.Struggling) _toast.Show("Con calma", "Mira la palabra entera, de principio a fin", AmberColor, 0.9f);
        }

        private void BreakStreak()
        {
            bool wasGlowing = _streak >= MeteorContract.StreakGlow;
            _streak = 0;
            if (wasGlowing) RefreshTrails();
        }

        private void AfterResolve()
        {
            _hud.SetStreak(_streak);
            if (!_hintDone)
            {
                _hintDone = true;
                _hint.gameObject.SetActive(false);
            }
            UpdateHud();
        }

        // ------------------------------------------------------------------ efectos

        private IEnumerator RescueEffect(Meteor m, int pts)
        {
            var at = m.Pos;
            var col = m.Spec.Golden ? NeuroStyle.Sun : MeteorSprites.TintColor(m.Tint);
            StartCoroutine(UiFx.SparkBurst(_fxRect, at, col, m.Spec.Golden ? 20 : 12, 220f, 36f, 0.6f));
            if (m.Spec.Golden) StartCoroutine(UiFx.RingBurst(_fxRect, at, NeuroStyle.Sun, 160f, 640f, 0.55f));
            StartCoroutine(MarkPop(at + new Vector2(m.Rx * 0.62f, m.Ry * 0.5f), true));
            StartCoroutine(FloatText(at + new Vector2(0f, m.Ry + 20f), "+" + pts, NeuroStyle.Sun));
            // tres estrellas vuelan a la constelación
            for (int i = 0; i < 3; i++) StartCoroutine(StarFlight(at, i));
            // la roca estalla: se achica y se apaga
            float t = 0f;
            const float seconds = 0.22f;
            while (t < seconds)
            {
                t += GameClock.DeltaTime;
                float k = Mathf.Clamp01(t / seconds);
                m.Root.localScale = Vector3.one * (Motion.Decorative ? Mathf.Lerp(1f, 1.25f, k) : 1f); // sin ReduceMotion: solo se apaga
                SetAlpha(m, 1f - k);
                yield return null;
            }
            Destroy(m.Root.gameObject);
        }

        private IEnumerator StarFlight(Vector2 from, int i)
        {
            if (!Motion.Decorative)
            {
                // Sin estrellitas que vuelan: la estrella de la constelación se enciende al mismo tiempo (0,6 s + retraso).
                yield return Motion.Hold(0.6f + i * 0.07f);
                if (i == 2) LightConstellation();
                yield break;
            }
            var img = NewImage(_fxRect, "FlyStar", MeteorSprites.StarLit());
            img.gameObject.SetActive(true);
            var r = img.rectTransform;
            r.sizeDelta = new Vector2(52f, 52f);
            Vector2 mid = (from + _starTarget) * 0.5f + new Vector2((i - 1) * 140f, 120f);
            float delay = i * 0.07f;
            float t = -delay;
            const float seconds = 0.6f;
            while (t < seconds)
            {
                t += GameClock.DeltaTime;
                float k = Mathf.Clamp01(t / seconds);
                float e = UiFx.EaseOutCubic(k);
                Vector2 p = (1 - e) * (1 - e) * from + 2 * (1 - e) * e * mid + e * e * _starTarget;
                r.anchoredPosition = p;
                r.localScale = Vector3.one * Mathf.Lerp(1f, 0.55f, e);
                img.color = new Color(1f, 1f, 1f, t < 0f ? 0f : 1f);
                yield return null;
            }
            Destroy(img.gameObject);
            if (i == 2) LightConstellation();
        }

        private void LightConstellation()
        {
            int lit = _rescued % _conStars.Count;
            bool complete = lit == 0 && _rescued > 0;
            SetConstellation(complete ? _conStars.Count : lit);
            var pos = _conStars[Mathf.Max(0, (complete ? _conStars.Count : lit) - 1)].rectTransform.anchoredPosition;
            StartCoroutine(UiFx.SparkBurst(_fxRect, pos, NeuroStyle.Sun, 8, 110f, 26f, 0.45f));
            if (complete)
            {
                StartCoroutine(UiFx.RingBurst(_fxRect, pos, NeuroStyle.Sun, 100f, 420f, 0.5f));
                _toast.Show("¡Constelación completa!", "Cada palabra rescatada suma una estrella", GoodColor, 1.1f);
                StartCoroutine(ResetConstellationSoon());
            }
        }

        private IEnumerator ResetConstellationSoon()
        {
            float t = 0f;
            while (t < 0.9f) { t += GameClock.DeltaTime; yield return null; }
            SetConstellation(_rescued % _conStars.Count);
        }

        private void SetConstellation(int lit)
        {
            for (int i = 0; i < _conStars.Count; i++)
                _conStars[i].sprite = i < lit ? MeteorSprites.StarLit() : MeteorSprites.StarDim();
        }

        private IEnumerator CrackEffect(Meteor m)
        {
            // la roca se vuelve polvo gris con grietas; la palabra queda tachada y sale un ✗
            m.RockImg.sprite = MeteorSprites.Rock(m.Shape, RockTint.Dust);
            if (m.TrailImg != null) m.TrailImg.color = new Color(1f, 1f, 1f, 0f); // la roca se apagó: sin calor
            if (m.GlowImg != null) m.GlowImg.gameObject.SetActive(false);
            if (m.AuraImg != null) m.AuraImg.gameObject.SetActive(false);
            var strike = NewImage(m.Plaque, "Strike", null);
            strike.sprite = RoundedRectSprite.Get(8);
            strike.type = Image.Type.Sliced;
            strike.color = BadColor;
            strike.rectTransform.sizeDelta = new Vector2(m.Plaque.sizeDelta.x - 24f, 10f);
            strike.gameObject.SetActive(true);
            StartCoroutine(MarkPop(m.Pos + new Vector2(m.Rx * 0.62f, m.Ry * 0.5f), false));
            for (int i = 0; i < 8; i++) StartCoroutine(Dust(m.Pos + new Vector2((i - 3.5f) * m.Rx * 0.22f, -m.Ry * 0.3f), i));
            StartCoroutine(FloatText(m.Pos + new Vector2(0f, -m.Ry - 30f), "esa no existía", BadColor, 44, 1.1f));
            GameFeel.Haptic(GameFeel.HapticKind.Double);
            // sacudida corta y luego se apaga hacia abajo
            float t = 0f;
            const float seconds = 1.0f;
            Vector2 home = m.Pos;
            while (t < seconds)
            {
                t += GameClock.DeltaTime;
                float k = Mathf.Clamp01(t / seconds);
                float shake = k < 0.25f && !GameFeel.ReduceMotion ? Mathf.Sin(t * 70f) * 14f * (1f - k * 4f) : 0f;
                m.Root.anchoredPosition = home + new Vector2(shake, Motion.Decorative ? -90f * UiFx.EaseOutCubic(k) : 0f); // sin ReduceMotion: se apaga en su lugar
                SetAlpha(m, k < 0.6f ? 1f : 1f - (k - 0.6f) / 0.4f);
                yield return null;
            }
            Destroy(m.Root.gameObject);
        }

        private IEnumerator DissolveEffect(Meteor m)
        {
            Vector2 at = new Vector2(m.Pos.x, _atmosphereY + 20f);
            StartCoroutine(UiFx.SparkBurst(_fxRect, at, NeuroStyle.Sky, 9, 130f, 24f, 0.7f));
            StartCoroutine(UiFx.SparkBurst(_fxRect, at + new Vector2(0f, 20f), NeuroStyle.Grape, 6, 90f, 20f, 0.6f));
            float t = 0f;
            const float seconds = 0.5f;
            while (t < seconds)
            {
                t += GameClock.DeltaTime;
                float k = Mathf.Clamp01(t / seconds);
                m.Root.localScale = Vector3.one * (Motion.Decorative ? Mathf.Lerp(1f, 0.7f, k) : 1f); // sin ReduceMotion: se desvanece en su lugar
                m.Root.anchoredPosition = m.Pos + new Vector2(0f, Motion.Decorative ? -40f * k : 0f);
                SetAlpha(m, 1f - k);
                yield return null;
            }
            Destroy(m.Root.gameObject);
        }

        private IEnumerator MissedEffect(Meteor m)
        {
            // una palabra real se fue: se apaga y queda su nombre, pequeño
            if (!m.Spec.Shower) StartCoroutine(FloatText(new Vector2(m.Pos.x, _atmosphereY + 90f), "se fue: " + m.Spec.Word, new Color(1f, 1f, 1f, 0.9f), 46, 1.6f, UiFonts.Word));
            float t = 0f;
            const float seconds = 0.6f;
            while (t < seconds)
            {
                t += GameClock.DeltaTime;
                float k = Mathf.Clamp01(t / seconds);
                m.Root.anchoredPosition = m.Pos + new Vector2(0f, Motion.Decorative ? -70f * k : 0f);
                SetAlpha(m, 1f - k);
                yield return null;
            }
            Destroy(m.Root.gameObject);
        }

        private IEnumerator MarkPop(Vector2 pos, bool ok)
        {
            var img = NewImage(_fxRect, ok ? "Check" : "Cross", ok ? AnswerMarkSprite.Check() : AnswerMarkSprite.Cross());
            img.gameObject.SetActive(true);
            var r = img.rectTransform;
            r.sizeDelta = new Vector2(120f, 120f);
            r.anchoredPosition = pos;
            float t = 0f;
            const float seconds = 0.9f;
            while (t < seconds)
            {
                t += GameClock.DeltaTime;
                float k = Mathf.Clamp01(t / seconds);
                r.localScale = Vector3.one * (Motion.Decorative ? Mathf.LerpUnclamped(0.3f, 1f, UiFx.EaseOutBack(Mathf.Clamp01(k * 3f))) : 1f); // la marca aparece quieta
                img.color = new Color(1f, 1f, 1f, k < 0.7f ? 1f : 1f - (k - 0.7f) / 0.3f);
                yield return null;
            }
            Destroy(img.gameObject);
        }

        private IEnumerator Dust(Vector2 at, int i)
        {
            if (!Motion.Decorative) yield break; // sin ReduceMotion: sin polvo
            var img = NewImage(_fxRect, "Dust", DiscSprite.Get());
            img.gameObject.SetActive(true);
            var r = img.rectTransform;
            float dir = (i % 2 == 0 ? -1f : 1f) * (0.5f + 0.18f * i);
            float t = 0f;
            const float seconds = 0.9f;
            while (t < seconds)
            {
                t += GameClock.DeltaTime;
                float k = Mathf.Clamp01(t / seconds);
                r.anchoredPosition = at + new Vector2(dir * 90f * UiFx.EaseOutCubic(k), -150f * k * k + 30f * k);
                float s = Mathf.Lerp(24f, 60f, k);
                r.sizeDelta = new Vector2(s, s);
                img.color = new Color(0.62f, 0.62f, 0.72f, 0.7f * (1f - k));
                yield return null;
            }
            Destroy(img.gameObject);
        }

        private static void SetAlpha(Meteor m, float a)
        {
            // el alfa del renderizador se multiplica con el de cada color: la estela y el brillo conservan su transparencia
            foreach (var g in m.Root.GetComponentsInChildren<Graphic>()) g.canvasRenderer.SetAlpha(a);
        }

        private IEnumerator FloatText(Vector2 pos, string text, Color color, int size = 58, float seconds = 0.8f, Font font = null)
        {
            var t = MakeText(_fxRect, "Float", size, TextAnchor.MiddleCenter, color, 0f, 0f);
            if (font != null) t.font = font;
            NeuroStyle.ClayText(t, size >= 56 ? 4f : 3f, size >= 56 ? 6f : 4f);
            var r = t.rectTransform;
            r.anchorMin = r.anchorMax = new Vector2(0.5f, 0.5f);
            r.sizeDelta = new Vector2(760f, size * 1.6f);
            t.horizontalOverflow = HorizontalWrapMode.Overflow;
            t.text = text;
            float e = 0f;
            while (e < seconds)
            {
                e += GameClock.DeltaTime;
                float k = Mathf.Clamp01(e / seconds);
                r.anchoredPosition = pos + new Vector2(0f, 60f * (Motion.Decorative ? UiFx.EaseOutCubic(k) : 0f));
                t.color = NeuroStyle.WithAlpha(color, 1f - k * k);
                yield return null;
            }
            Destroy(t.gameObject);
        }

        private void ClearMeteors()
        {
            foreach (var m in _active) if (m.Root != null) Destroy(m.Root.gameObject);
            _active.Clear();
        }

        private void SetPrompt(string text, Color color)
        {
            if (_prompt == null) return;
            _prompt.text = text;
            _prompt.color = color;
        }

        // ------------------------------------------------------------------ fin

        private IEnumerator FinishGame()
        {
            _phase = Phase.Done;
            ClearMeteors();

            float acc = _tally.BalancedAccuracy;
            int score = MeteorContract.Score(acc, _dda.PeakLevel);
            int total = _tally.WordsSeen + _tally.DecoysSeen;
            int correct = _tally.WordsHit + _tally.DecoysPassed;
            int avgMs = 0;
            if (_allMs.Count > 0) { long s = 0; foreach (int v in _allMs) s += v; avgMs = (int)(s / _allMs.Count); }

            SetPrompt("Fin de la lluvia", GoodColor);
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
                    average_response_time_ms = avgMs,
                    level = _config.config.level,
                    timed = _config.config.timed,
                    end_rating = _dda.RatingNormalized,
                    mode_trials = _dda.ScoredTrials,
                    mode_hits = _dda.ScoredCorrect,
                    peak_level = _dda.PeakLevel,
                    lex_band_seen = (int[])_tally.BandSeen.Clone(),
                    lex_band_hits = (int[])_tally.BandHits.Clone(),
                    lex_fa_seen = (int[])_tally.DecoySeen.Clone(),
                    lex_fa_hits = (int[])_tally.DecoyTapped.Clone(),
                    lex_rt_common_ms = _tally.CommonMedianMs,
                    lex_rt_rare_ms = _tally.RareMedianMs,
                    lex_rare_words = _tally.RareWords
                }
            };
            NativeBridge.ForwardTelemetryToPlatform(JsonUtility.ToJson(telemetry));
            yield break;
        }

        private void ShowResult(int score)
        {
            _exit.Show();
            _resultRoot.Find("Title").GetComponent<Text>().text = score >= 85 ? "¡Gran vocabulario!" : score >= 65 ? "¡Buena lluvia!" : "Misión completada";
            _resultRoot.Find("Detail").GetComponent<Text>().text = $"{_tally.WordsHit} palabras tocadas · {_tally.DecoysPassed} inventadas dejadas pasar";
            int fa = _tally.DecoysTappedTotal;
            _resultRoot.Find("Extra").GetComponent<Text>().text = fa > 0 ? $"Tocaste {fa} inventada{(fa == 1 ? "" : "s")} · mejor racha {_bestStreak}" : $"Ninguna inventada tocada · mejor racha {_bestStreak}";
            _resultRoot.gameObject.SetActive(true);
            StartCoroutine(AnimateResult(score));
        }

        // ------------------------------------------------------------------ construcción de UI

        protected override void BuildUi()
        {

            var canvasGo = new GameObject("MeteorCanvas");
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
            WorldBackdrop.Build(bgRect, GameWorld.Observatory);

            var safeGo = new GameObject("SafeAreaContent");
            safeGo.transform.SetParent(canvasGo.transform, false);
            _safe = safeGo.AddComponent<RectTransform>();
            ApplySafeArea(_safe);

            _hud = new GameHud(_safe, "Lluvia de meteoros", MarginU, this);
            BuildTimer();

            var playGo = new GameObject("Play");
            playGo.transform.SetParent(_safe, false);
            _play = playGo.AddComponent<RectTransform>();
            Stretch(_play);

            // atmósfera: un resplandor fino sobre la cúpula, donde se apagan los meteoros
            var atm = NewImage(_play, "Atmosphere", RadialGlowSprite.Get());
            atm.color = NeuroStyle.WithAlpha(NeuroStyle.Sky, 0.22f);
            atm.rectTransform.sizeDelta = new Vector2(1500f, 280f);
            atm.gameObject.SetActive(true);
            _atmosphere = atm.rectTransform;

            BuildConstellation();

            _hint = CenteredText(_play, "Hint", 54, new Color(1f, 1f, 1f, 0.92f));
            NeuroStyle.ClayText(_hint, 3f, 4f);
            _hint.gameObject.SetActive(false);
            _prompt = CenteredText(_play, "Prompt", 60, Color.white);
            NeuroStyle.ClayText(_prompt, 3.5f, 5f);

            _meteorLayer = Layer(_play, "Meteors");
            _fxRect = Layer(_play, "Fx");

            BuildResultPanel();
            _exit = new ExitButton(_safe, this, UnitsPerDp);
            _toast = new Toast(_safe, this, UnitsPerDp);
            _toast.SetBelowHud();
            // el campo donde se mueve el estímulo: el aviso nunca va ahí (se mide en Layout)
            var keepOut = new GameObject("ToastKeepOut", typeof(RectTransform));
            keepOut.transform.SetParent(_play, false);
            _toastKeepOut = (RectTransform)keepOut.transform;
            _toastKeepOut.anchorMin = _toastKeepOut.anchorMax = new Vector2(0.5f, 0.5f);
            _toastKeepOut.pivot = new Vector2(0.5f, 1f);
            _toast.KeepOut(_toastKeepOut);
            BuildTutorial(_safe, GameHud.Height + 10f, "Lluvia de meteoros", "Toca las palabras que existen. Las inventadas, déjalas caer.",
                captionFromBottomU: 430f, badgeAtBottom: true);

            var flashGo = new GameObject("Flash");
            flashGo.transform.SetParent(canvasGo.transform, false);
            Stretch(flashGo.AddComponent<RectTransform>());
            _flash = flashGo.AddComponent<Image>();
            _flash.raycastTarget = false;
            _flash.color = new Color(0f, 0f, 0f, 0f);

            _countdown = new CountdownScreen(canvasGo.transform, UnitsPerDp);
        }

        private void BuildConstellation()
        {
            for (int i = 0; i < 5; i++)
            {
                var img = NewImage(_play, "ConstellationStar", MeteorSprites.StarDim());
                img.rectTransform.sizeDelta = new Vector2(78f, 78f);
                img.gameObject.SetActive(true);
                _conStars.Add(img);
            }
        }

        private static Text CenteredText(Transform parent, string name, int size, Color color)
        {
            var t = MakeText(parent, name, size, TextAnchor.MiddleCenter, color, 0f, 0f);
            var r = t.rectTransform;
            r.anchorMin = r.anchorMax = new Vector2(0.5f, 0.5f);
            r.pivot = new Vector2(0.5f, 0.5f);
            BestFit(t, 42);                                  // nunca bajo 14 dp: si no cabe en un renglón pasa a dos
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
            AddResultText("Extra", 42, new Vector2(0f, -250f), new Color(1f, 1f, 1f, 0.65f));
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
            _bottom = -_playH * 0.5f;
            float contentW = _playW - MarginU * 2f;

            // constelación bajo el marcador
            float cy = top - (GameHud.Height + 90f);
            for (int i = 0; i < _conStars.Count; i++)
                _conStars[i].rectTransform.anchoredPosition = new Vector2((i - 2) * 130f, cy + (i % 2 == 0 ? 0f : -26f));
            _starTarget = _conStars[2].rectTransform.anchoredPosition;

            _hint.rectTransform.sizeDelta = new Vector2(contentW, 150f);
            _hint.rectTransform.anchoredPosition = new Vector2(0f, top - (GameHud.Height + 330f));
            _prompt.rectTransform.sizeDelta = new Vector2(contentW, 90f);
            _prompt.rectTransform.anchoredPosition = new Vector2(0f, top - (GameHud.Height + 200f));

            _spawnY = top - (GameHud.Height + 190f);
            _hudBottomY = top - GameHud.Height;
            // la atmósfera queda sobre la cúpula del observatorio (la superficie ocupa el 16% de abajo)
            _atmosphereY = _bottom + _playH * 0.30f;
            _atmosphere.anchoredPosition = new Vector2(0f, _atmosphereY - 40f);
            // zona prohibida del aviso: el cielo por donde caen los meteoros, de debajo del marcador a la atmósfera; el aviso va abajo, en la cúpula y la superficie
            _toastKeepOut.sizeDelta = new Vector2(_playW, Mathf.Max(0f, (top - GameHud.Height) - _atmosphereY));
            _toastKeepOut.anchoredPosition = new Vector2(0f, top - GameHud.Height);
        }

        private void UpdateHud()
        {
            _hud.SetLevel(_dda.PresentedLevel);
            if (Endless) _hud.SetPoints(_points);
            else _hud.SetInfo($"{Mathf.Min(_resolved + 1, MeteorContract.PrecisionMeteors)} de {MeteorContract.PrecisionMeteors}");
        }

        // ------------------------------------------------------------------ «Cómo se juega» desde la pausa

        private bool _loopOn;

        protected override bool HowToReady => _loopOn && _phase != Phase.Done;

        protected override void HowToSuspend()
        {
            _phase = Phase.Idle;
            _toast.Hide();
            ClearTransient();
        }

        protected override void HowToResume(float spentSeconds)
        {
            _endsAt = HowToClock.Shift(_endsAt, spentSeconds);       // el tiempo que duró «Cómo se juega» no se le descuenta al Reto
            _lastSpawn = GameClock.Time - 1f;
            ClearTransient();
            _phase = Phase.Playing;
            StartCoroutine(MainLoop());                              // los meteoros que estaban en el cielo no cuentan: sigue la lluvia con el mismo estado
        }

        /// <summary>Quita lo que quedó a medias (meteoros, efectos, avisos) sin tocar ninguna cuenta.</summary>
        private void ClearTransient()
        {
            ClearMeteors();
            foreach (Transform c in _fxRect) Destroy(c.gameObject);
            _pending = null;
            _hint.gameObject.SetActive(false);
            SetPrompt("", Color.white);
            SetConstellation(_rescued % _conStars.Count);
        }

        // ------------------------------------------------------------------ ronda guiada del tutorial (pieza común)

        private bool _guided;
        private int _guidedOutcome;          // 0 = esperando, 1 = tocó una palabra, 2 = tocó una inventada, 3 = se fue una palabra, 4 = pasó una inventada
        private Image _guidedRing;

        // <guided>
        protected override IEnumerator GuidedRound(GuidedTutorial t)
        {
            t.BeginPractice();
            _guided = true;
            ClearMeteors();
            _pending = null;
            _hint.gameObject.SetActive(false);
            SetPrompt("", Color.white);
            _phase = Phase.Playing;
            var coach = t.Coach;
            var script = new GuidedScript(MeteorContract.GuidedPlan.Length);
            // «Nubi entrenadora»: el primer meteoro (una palabra) se congela arriba con el foco y se toca; el segundo (inventado) cae solo y Nubi avisa DESPUÉS de responder.
            // Si una palabra llega abajo sin tocarse, Nubi lo dice y la vuelve a soltar con el foco.
            bool focusNext = true;
            while (!script.Finished)
            {
                if (t.Skipped) { script.Skip(); break; }
                bool isWord = MeteorContract.GuidedPlan[script.Index];
                MeteorSpec spec;
                if (isWord)
                {
                    _lexicon.PickWord(1, 1, 4, 6, _rng, out var w, out int band);
                    spec = new MeteorSpec { Word = w, IsWord = true, Band = band, FallSeconds = MeteorContract.GuidedFall(Senior) };
                }
                else
                {
                    _lexicon.PickDecoy(new[] { DecoyKind.Obvious }, 4, 6, _rng, out var d, out var kind, out int band);
                    spec = new MeteorSpec { Word = d, IsWord = false, Band = band, Decoy = kind, FallSeconds = MeteorContract.GuidedFall(Senior) };
                }
                _pending = spec;
                int tries = 0;
                while (!Spawn() && tries++ < 20) yield return null;
                var meteor = _active.Count > 0 ? _active[_active.Count - 1] : null;
                if (meteor != null && isWord)
                {
                    // el aro sol punteado marca la palabra que se toca
                    _guidedRing = GuidedTutorial.CreateHintRing(meteor.Root, Mathf.Max(meteor.Rx, meteor.Ry) * 2.7f);
                    _guidedRing.gameObject.SetActive(true);
                }
                _guidedOutcome = 0;
                if (isWord && meteor != null && focusNext)
                {
                    focusNext = false;
                    yield return null;                                     // el meteoro termina de aparecer arriba
                    var m = meteor;
                    yield return StartCoroutine(coach.Touch(() => coach.AroundOf(m.Root, new Vector2(Mathf.Max(m.Rx * 2.6f, 340f), Mathf.Max(m.Ry * 2.6f, 260f))),
                        CoachTexts.Meteoros.Exists));
                }
                float outcomeWait = 0f;
                while (_guidedOutcome == 0 && !t.Skipped)
                {
                    outcomeWait += GameClock.RealDeltaTime;
                    if (isWord && GuidedTutorial.AutoPlay(outcomeWait)) _guidedOutcome = 1;     // solo en el smoke del Editor
                    yield return null;
                }
                _guidedRing = null;
                if (t.Skipped) { script.Skip(); break; }
                if (_guidedOutcome == 1 || _guidedOutcome == 4)
                {
                    script.Success();
                    if (_guidedOutcome == 4) yield return StartCoroutine(coach.Notice(CoachTexts.Meteoros.LetItFall, 2.2f));
                }
                else
                {
                    script.Failure();
                    focusNext = _guidedOutcome == 3;
                    yield return StartCoroutine(coach.Notice(_guidedOutcome == 3 ? CoachTexts.Meteoros.MissedWord : CoachTexts.Meteoros.MissedFake, 2.4f));
                }
            }
            _guidedRing = null;
            ClearMeteors();
            foreach (Transform c in _fxRect) Destroy(c.gameObject);
            if (!script.Skipped)
            {
                PlayTone(523f, 0.4f, 0.08f);
                yield return StartCoroutine(coach.Notice(CoachTexts.Ready, 1.5f));
            }
            t.EndPractice();
            _guided = false;
            _phase = Phase.Idle;
            _pending = null;
        }
        // </guided>

        /// <summary>Un meteoro de la ronda guiada se resolvió (se tocó o llegó abajo): solo se ve el efecto y se avisa a la ronda; no hay puntos, rachas, constelación ni DDA.</summary>
        private void GuidedResolve(Meteor m, bool tapped)
        {
            m.Done = true;
            _active.Remove(m);
            bool word = m.Spec.IsWord;
            if (tapped && word)
            {
                GameFeel.Correct(1);
                StartCoroutine(UiFx.SparkBurst(_fxRect, m.Pos, MeteorSprites.TintColor(m.Tint), 12, 220f, 36f, 0.6f));
                StartCoroutine(MarkPop(m.Pos + new Vector2(m.Rx * 0.62f, m.Ry * 0.5f), true));
                StartCoroutine(GuidedFade(m));
                _guidedOutcome = 1;
            }
            else if (tapped)
            {
                GameFeel.Wrong();
                StartCoroutine(CrackEffect(m));
                _guidedOutcome = 2;
            }
            else if (word)
            {
                StartCoroutine(MissedEffect(m));
                _guidedOutcome = 3;
            }
            else
            {
                PlayTone(330f, 0.12f, 0.03f);
                StartCoroutine(DissolveEffect(m));
                _guidedOutcome = 4;
            }
        }

        private static IEnumerator Wait(float seconds) => Motion.Hold(seconds);

        private IEnumerator GuidedFade(Meteor m)
        {
            float t = 0f;
            const float seconds = 0.22f;
            while (t < seconds)
            {
                t += GameClock.DeltaTime;
                float k = Mathf.Clamp01(t / seconds);
                SetAlpha(m, 1f - k);
                yield return null;
            }
            Destroy(m.Root.gameObject);
        }

        /// <summary>true si el foco de Nubi está abierto y el toque cae fuera de su hueco (no llega al juego).</summary>
        private bool TapBlocked(Vector2 screenPos) => _tutorial != null && _tutorial.Coach.Blocks(screenPos);
    }
}
