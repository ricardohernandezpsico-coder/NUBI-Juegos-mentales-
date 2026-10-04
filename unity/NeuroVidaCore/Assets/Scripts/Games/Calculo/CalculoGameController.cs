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

namespace NeuroVida.Games.Calculo
{
    /// <summary>
    /// «Carga exacta» (Razonamiento; reemplaza a Cálculo Sereno y conserva su id <c>calculo</c>; ver <see cref="CalculoContract"/> y docs/diseno-carga-exacta.md).
    /// El reactor de la nave pide una CARGA exacta (p. ej. 24) y hay celdas de energía con números: se juntan de a dos con + − × ÷ hasta que una celda valga la carga.
    /// Cualquier camino vale. Se toca una celda (sube y brilla), una operación (se pone dorada) y otra celda: un arco de luz las junta y queda una con el resultado.
    /// Lo imposible (resta negativa, división inexacta) no se hace y se explica con cariño; «Deshacer» y «Empezar de nuevo» no cuestan nada. Si pasan 25 s (20 s en
    /// mayores) Nubi da una pista: vuelve a las celdas originales y dice el primer paso de una solución (las dos celdas laten). Al lograrla el reactor se pone dorado.
    /// Precisión = 8 cargas; Reto = 120 s. Nada cae ni apura dentro de una carga. Con «quitar animaciones»: sin rebotes, chispas, arco ni giro del aro; quedan los
    /// cambios de número, de color y los fundidos cortos.
    /// </summary>
    public class CalculoGameController : GameControllerBase
    {
        public const string GameId = CalculoContract.GameId;

        private const float UnitsPerDp = 3f;
        private const float MarginU = 60f;
        private const int MaxCells = 5;
        private const int ArcDots = 14;

        // ------------------------------------------------------------------ colores (los del boceto aprobado)

        private static readonly Color Cyan = new Color(127f / 255f, 216f / 255f, 255f / 255f);
        private static readonly Color Gold = new Color(255f / 255f, 201f / 255f, 74f / 255f);
        private static readonly Color Lavender = new Color(171f / 255f, 165f / 255f, 210f / 255f);
        private static readonly Color Dim = new Color(110f / 255f, 106f / 255f, 154f / 255f);
        private static readonly Color Silver = new Color(214f / 255f, 222f / 255f, 240f / 255f);
        private static readonly Color GoodText = new Color(159f / 255f, 245f / 255f, 214f / 255f);
        private static readonly Color WarnText = new Color(255f / 255f, 214f / 255f, 160f / 255f);
        private static readonly Color ButtonText = new Color(237f / 255f, 234f / 255f, 251f / 255f);
        private static readonly Color RingBody = new Color(35f / 255f, 43f / 255f, 87f / 255f);
        private static readonly Color CoreDark = new Color(13f / 255f, 20f / 255f, 53f / 255f);
        private static readonly Color CoreLight = new Color(27f / 255f, 42f / 255f, 94f / 255f);
        private static readonly Color CoreGold = new Color(233f / 255f, 169f / 255f, 58f / 255f);
        private static readonly Color CoreGoldLight = new Color(255f / 255f, 243f / 255f, 196f / 255f);
        private static readonly Color CellNormal = new Color(233f / 255f, 229f / 255f, 255f / 255f);
        private static readonly Color CellSelected = new Color(191f / 255f, 233f / 255f, 255f / 255f);
        private static readonly Color CellWin = new Color(255f / 255f, 224f / 255f, 138f / 255f);
        private static readonly Color CellWindow = new Color(26f / 255f, 18f / 255f, 64f / 255f, 0.2f);
        private static readonly Color OpIdle = new Color(42f / 255f, 51f / 255f, 102f / 255f);
        private static readonly Color ToolFill = new Color(22f / 255f, 29f / 255f, 60f / 255f);

        private enum Phase { Idle, Play, Solved, Done }

        // ------------------------------------------------------------------ piezas

        private sealed class CellView
        {
            public int Id;
            public RectTransform Root;
            public CanvasGroup Group;
            public Image Halo, Glow, Face, Window, Shine;
            public Text Label;
            public bool Shown, Flying;
            public int Value;
            public Vector2 Pos, Target;
            public float BornAt, PopAt = -10f, FlyAt, FlyFade;
            public int FlyToId = -1;
            public Vector2 FlyFrom;
        }

        private sealed class OpView
        {
            public RectTransform Root;
            public Image Shadow, Bg;
            public Text Label;
            public CargaOp Op;
            public Rect Rect;           // lógico (dp)
            public bool Visible;
            public CanvasGroup Group;
        }

        private sealed class ToolView
        {
            public RectTransform Root;
            public Image Bg;
            public Text Label;
            public Rect Rect;
            public bool Undo, Visible;
            public CanvasGroup Group;
        }

        private sealed class Spark { public Image Img; public Vector2 Pos, Vel; public float Age, Life, Size; public bool Alive; public Color Color; }

        private sealed class Arc
        {
            public Image[] Dots;
            public Vector2 A, B;
            public float At = -10f;
            public bool Alive;
        }

        // ------------------------------------------------------------------ estado

        private System.Random _rng;
        private AdaptiveDifficulty _dda;
        private CargaDirector _director;
        private CargaCredit _credit;
        private CargaTally _tally;
        private CargaPuzzle _puzzle;
        private CargaBoard _board;
        private Phase _phase = Phase.Idle;
        private bool Endless => _config != null && _config.config.timed;
        private bool Precision => !Endless;
        private bool _senior, _loopOn, _guided, _inputOn;
        private int _streak, _bestStreak, _points, _resolved;
        private float _endsAt, _loadStart, _hintAt, _solvedAt, _hintShownUntil;
        private int _lastTickSecond = -1;
        private int _sel = -1, _op = -1;
        private bool _hinted;
        private int _hintIdA = -1, _hintIdB = -1, _winId = -1;
        private Vector2? _press;
        private float _solvedK;
        private readonly CargaOp[] _shownOps = new CargaOp[4];
        private int _shownOpCount;

        // escala y disposición
        private float _s = 3f, _playW = 1080f, _playH = 1920f, _logicalH = 640f;
        private CargaLayout.Metrics _m;

        // UI
        private RectTransform _safe, _play, _reactorLayer, _cellLayer, _uiLayer, _fxLayer, _timerTrack, _timerHead;
        private Image _timerFill;
        private Image _reactorGlow, _reactorShadow, _reactorRim, _reactorBody, _core, _coreHi;
        private readonly Image[] _lights = new Image[12];
        private Text _reactorLabel, _targetText, _allText, _sayText, _guideText;
        private readonly CellView[] _views = new CellView[MaxCells];
        private readonly OpView[] _opViews = new OpView[4];
        private readonly ToolView[] _toolViews = new ToolView[2];
        private readonly List<Image> _dots = new List<Image>();
        private readonly int[] _dotState = new int[CalculoContract.PrecisionLoads];
        private readonly List<Spark> _sparks = new List<Spark>();
        private readonly Arc[] _arcs = new Arc[3];
        private ExitButton _exit;
        private GameHud _hud;
        private CountdownScreen _countdown;
        private readonly List<KeyValuePair<Text, float>> _fonts = new List<KeyValuePair<Text, float>>();
        private string _sayFull = "";
        private float _sayAt = -10f;
        private Color _sayColor = GoodText;

        // ------------------------------------------------------------------ sesión

        public void StartSession(SequenceInitConfig config)
        {
            _config = config;
            _rng = new System.Random();
            var age = DdaUserProfileConfig.ParseAgeBand(config.config.age_band);
            _senior = age == AgeBand.Senior;
            float start = AdaptiveDifficulty.StartRating(config.config, CalculoContract.MaxLevel);
            // 8 cargas por partida: pasos grandes (como Satélites y La estrella intrusa). Sin tiempo de reacción: pensar con calma no se penaliza.
            _dda = new AdaptiveDifficulty(CalculoContract.MaxLevel, age, start, stepUp: 0.4f, useReaction: false);
            _director = new CargaDirector(_rng);
            _credit = new CargaCredit();
            _tally = new CargaTally();

            _phase = Phase.Idle;
            _loopOn = _guided = false;
            _inputOn = false;
            _press = null;
            _streak = _bestStreak = _points = _resolved = 0;
            _endsAt = 0f;
            _lastTickSecond = -1;
            _sel = _op = -1;
            _solvedK = 0f;
            for (int i = 0; i < _dotState.Length; i++) _dotState[i] = 0;
            ClearLoad();

            _resultRoot.gameObject.SetActive(false);
            _exit.Hide();
            _timerTrack.gameObject.SetActive(Endless);
            _hud.SetStreak(0);
            _tutorial.Hide();
            SetToolsVisible(true);

            StopAllCoroutines();
            StartCoroutine(GameLoop());
        }

        private IEnumerator GameLoop()
        {
            StartCoroutine(Prewarm());
            if (TutorialWanted)
            {
                // la ronda guiada se juega sobre el reactor ya armado, antes de la cuenta regresiva
                _safe.gameObject.SetActive(true);
                yield return null;
                ApplySafeArea(_safe);
                Canvas.ForceUpdateCanvases();
                Layout();
                UpdateHud();
                yield return StartCoroutine(RunTutorialIfNeeded());
                ClearLoad();
            }
            _safe.gameObject.SetActive(false);
            yield return StartCoroutine(_countdown.Play("Carga exacta", Assessment.Subtitle("Combina las celdas"), () => _safe.gameObject.SetActive(true)));
            _safe.gameObject.SetActive(true);
            yield return null;
            ApplySafeArea(_safe);
            Canvas.ForceUpdateCanvases();
            Layout();
            UpdateHud();

            _endsAt = GameClock.Time + CalculoContract.RetoSeconds;
            _loopOn = true;
            yield return StartCoroutine(MainLoop());
        }

        /// <summary>El bucle de la partida: una carga tras otra hasta que se acabe el tiempo (Reto) o las 8 (Precisión). «Cómo se juega» lo retoma desde acá.</summary>
        private IEnumerator MainLoop()
        {
            while (!Finished()) yield return StartCoroutine(PlayLoad());
            _loopOn = false;
            yield return StartCoroutine(FinishGame());
        }

        private bool Finished()
        {
            if (Endless) return GameClock.Time >= _endsAt;
            return _resolved >= CalculoContract.PrecisionLoads;
        }

        /// <summary>Sintetiza los sonidos y hornea los sprites durante la cuenta regresiva (así la primera carga no traba).</summary>
        private IEnumerator Prewarm()
        {
            DiscSprite.Get();
            RingSprite.Get();
            RadialGlowSprite.Get();
            yield return null;
            CargaSounds.Select();
            CargaSounds.Operation();
            CargaSounds.Thud();
            CargaSounds.Back();
            yield return null;
            for (int step = 1; step <= 6; step++) CargaSounds.Merge(step);
            CargaSounds.Hint();
            yield return null;
            CargaSounds.Arpeggio();
            CargaSounds.Finale();
        }

        /// <summary>Una carga: se arma, se juega hasta lograrla (o hasta la pista, o hasta que se acabe el tiempo del Reto) y se cuenta.</summary>
        private IEnumerator PlayLoad()
        {
            var puzzle = _director.Next(_dda.PresentedLevel);
            BeginLoad(puzzle);
            UpdateHud();
            bool timeUp = false, gaveUp = false;
            while (_phase == Phase.Play)
            {
                float now = GameClock.Time;
                if (Endless && now >= _endsAt) { timeUp = true; break; }
                if (!_hinted && now - _loadStart >= CalculoContract.HintAfter(_senior)) DoHint();
                else if (_hinted && now - _hintAt >= CalculoContract.GiveUpAfterHintSeconds) { gaveUp = true; break; }
                yield return null;
            }
            if (timeUp)
            {
                // se acabó el reloj en medio de una carga: no cuenta (ni a favor ni en contra)
                _phase = Phase.Idle;
                _inputOn = false;
                ClearLoad();
                yield break;
            }
            _inputOn = false;
            var outcome = gaveUp ? CargaOutcome.Missed : _hinted ? CargaOutcome.Hinted : CargaOutcome.Alone;
            int ms = Mathf.RoundToInt(((gaveUp ? GameClock.Time : _solvedAt) - _loadStart) * 1000f);
            RecordLoad(outcome, _board.Steps, puzzle.ShortestSteps, ms);
            if (gaveUp)
            {
                _phase = Phase.Idle;
                Tell("Esta carga es difícil: la dejamos para otra vez", false);
                yield return Motion.Hold(1.6f);
            }
            else
            {
                string shortPath = outcome == CargaOutcome.Alone && _board.Steps <= puzzle.ShortestSteps ? " Camino corto" : "";
                Tell(outcome == CargaOutcome.Hinted ? "¡Carga exacta! (con pista)" : "¡Carga exacta!" + shortPath, true);
                yield return Motion.Hold(1.7f);
            }
            _phase = Phase.Idle;
            ClearLoad();
            yield return Motion.Hold(0.15f);
        }

        /// <summary>Anota una carga de la partida: cuentas, DDA (con pista cuenta como acierto una vez sí y otra no), racha y puntos.</summary>
        private void RecordLoad(CargaOutcome outcome, int stepsUsed, int shortestSteps, int ms)
        {
            _tally.Record(outcome, stepsUsed, shortestSteps, ms);
            _dda.Register(_credit.CountsAsHit(outcome));
            if (_resolved < _dotState.Length) _dotState[_resolved] = outcome == CargaOutcome.Alone ? 2 : outcome == CargaOutcome.Hinted ? 3 : 4;
            _resolved++;
            if (outcome == CargaOutcome.Alone)
            {
                _streak++;
                _bestStreak = Mathf.Max(_bestStreak, _streak);
                _points += 10;
            }
            else
            {
                _streak = 0;
                if (outcome == CargaOutcome.Hinted) _points += 5;
            }
            _hud.SetStreak(_streak);
            UpdateHud();
        }

        private IEnumerator FinishGame()
        {
            _phase = Phase.Done;
            _inputOn = false;
            ClearLoad();
            int score = CalculoContract.Score(_tally, Endless);
            ShowResult(score);
            PlayClip(CargaSounds.Finale(), 0.8f);

            var telemetry = new StroopTelemetry
            {
                user_id = _config.user_id,
                game_id = _config.game_id,
                session_metrics = new StroopSessionMetrics
                {
                    correct_trials = _tally.Solved,
                    total_trials = _tally.Total,
                    calculated_score = score,
                    average_response_time_ms = _tally.MeanAloneMs < 0 ? 0 : _tally.MeanAloneMs,
                    level = _config.config.level,
                    timed = _config.config.timed,
                    end_rating = _dda.RatingNormalized,
                    mode_trials = _dda.ScoredTrials,
                    mode_hits = _dda.ScoredCorrect,
                    peak_level = _dda.PeakLevel,
                    carga_alone = _tally.SolvedAlone,
                    carga_hinted = _tally.Hinted,
                    carga_short = _tally.ShortPaths,
                    carga_ms = _tally.MeanAloneMs
                }
            };
            NativeBridge.ForwardTelemetryToPlatform(JsonUtility.ToJson(telemetry));
            yield break;
        }

        private void ShowResult(int score)
        {
            _exit.Show();
            SetToolsVisible(false);
            for (int i = 0; i < _shownOpCount; i++) _opViews[i].Root.gameObject.SetActive(false);
            _reactorLayer.gameObject.SetActive(false);
            _cellLayer.gameObject.SetActive(false);
            _resultRoot.Find("Title").GetComponent<Text>().text = "¡Reactor cargado!";
            _resultRoot.Find("Detail").GetComponent<Text>().text = $"{_tally.Solved} de {_tally.Total} cargas · {_tally.SolvedAlone} sin pista";
            _resultRoot.Find("Extra").GetComponent<Text>().text = _tally.MeanAloneMs >= 0
                ? $"Tiempo medio {_tally.MeanAloneMs / 1000f:0} s por carga sin pista"
                : "Cada carga logra su camino";
            _resultRoot.gameObject.SetActive(true);
            StartCoroutine(AnimateResult(score));
        }

        private void UpdateHud()
        {
            _hud.SetLevel(_dda != null ? _dda.PresentedLevel : 1);
            if (Endless) _hud.SetPoints(_points);
            else _hud.SetInfo($"{Mathf.Min(_resolved + 1, CalculoContract.PrecisionLoads)} de {CalculoContract.PrecisionLoads}");
        }

        // ------------------------------------------------------------------ una carga

        /// <summary>Arma la carga en la pantalla (sin contar nada): el reactor con su número, las celdas que entran una tras otra y las operaciones del nivel.</summary>
        private void BeginLoad(CargaPuzzle p)
        {
            _puzzle = p;
            _board = new CargaBoard(p.Cells);
            _sel = _op = -1;
            _hinted = false;
            _hintIdA = _hintIdB = _winId = -1;
            _hintShownUntil = 0f;
            _solvedK = 0f;
            _solvedAt = 0f;
            _targetText.text = p.Target.ToString();
            _allText.gameObject.SetActive(p.RequireAll);
            foreach (var v in _views) { v.Shown = false; v.Flying = false; v.Root.gameObject.SetActive(false); SizeCell(v, p.Cells.Length); }
            SetOps(p.Ops);
            SyncViews(reborn: true);
            _sayFull = "";
            _loadStart = GameClock.Time;
            _phase = Phase.Play;
            _inputOn = true;
            _press = null;
        }

        private void ClearLoad()
        {
            _sel = _op = -1;
            _hinted = false;
            _hintIdA = _hintIdB = _winId = -1;
            _solvedK = 0f;
            _sayFull = "";
            if (_views[0] != null)
                foreach (var v in _views) { v.Shown = false; v.Flying = false; v.Root.gameObject.SetActive(false); }
            if (_sayText != null) _sayText.gameObject.SetActive(false);
            for (int i = 0; i < _arcs.Length; i++) if (_arcs[i] != null) { _arcs[i].Alive = false; foreach (var d in _arcs[i].Dots) d.gameObject.SetActive(false); }
            foreach (var s in _sparks) { s.Alive = false; s.Img.gameObject.SetActive(false); }
        }

        /// <summary>Pone en pantalla las celdas del tablero: las que ya no están se van, las nuevas entran (con rebote, una tras otra) y las que cambiaron de valor dan un «pop».</summary>
        private void SyncViews(bool reborn)
        {
            float now = GameClock.Time;
            int n = _board.Count;
            for (int k = 0; k < n; k++)
            {
                var cell = _board.Cells[k];
                var v = _views[cell.Id];
                v.Target = CargaLayout.CellPos(_m, k, n);
                if (!v.Shown || v.Flying)
                {
                    bool wasFlying = v.Flying;
                    v.Shown = true;
                    v.Flying = false;
                    v.Root.gameObject.SetActive(true);
                    v.Group.alpha = 0f;
                    v.BornAt = now + k * 0.07f;
                    v.Pos = reborn || !wasFlying ? v.Target + new Vector2(0f, Motion.Decorative ? 40f : 0f) : v.Pos;
                    v.PopAt = -10f;
                    SetValue(v, cell.Value);
                }
                else if (v.Value != cell.Value)
                {
                    SetValue(v, cell.Value);
                    v.PopAt = now;
                }
            }
            foreach (var v in _views)
            {
                if (!v.Shown || v.Flying) continue;
                if (_board.IndexOfId(v.Id) < 0) { v.Shown = false; v.Root.gameObject.SetActive(false); }
            }
        }

        private void SetValue(CellView v, int value)
        {
            v.Value = value;
            v.Label.text = value.ToString();
            int len = v.Label.text.Length;
            float dp = len <= 2 ? 30f : len == 3 ? 24f : len == 4 ? 19f : 16f;
            v.Label.fontSize = Mathf.RoundToInt(dp * _s);
        }

        private void SetOps(CargaOp[] ops)
        {
            _shownOpCount = Mathf.Min(ops.Length, _opViews.Length);
            for (int i = 0; i < _opViews.Length; i++)
            {
                var o = _opViews[i];
                o.Visible = i < _shownOpCount;
                o.Root.gameObject.SetActive(o.Visible && _phase != Phase.Done);
                if (!o.Visible) continue;
                o.Op = ops[i];
                _shownOps[i] = ops[i];
                o.Label.text = CargaRules.Symbol(ops[i]);
                var pos = CargaLayout.OpPos(_m, i, _shownOpCount);
                o.Rect = new Rect(pos.x - CargaLayout.OpW / 2f, pos.y - CargaLayout.OpH / 2f, CargaLayout.OpW, CargaLayout.OpH);
                o.Root.sizeDelta = new Vector2(CargaLayout.OpW * _s, CargaLayout.OpH * _s);
                o.Root.anchoredPosition = P(pos);
            }
        }

        private void SetToolsVisible(bool on)
        {
            foreach (var t in _toolViews)
            {
                if (t == null) continue;
                t.Visible = on;
                t.Root.gameObject.SetActive(on);
            }
        }

        // ------------------------------------------------------------------ jugadas

        private void TapCell(int id)
        {
            if (_phase != Phase.Play) return;
            if (_sel < 0 || _sel == id)
            {
                _sel = _sel == id ? -1 : id;
                _op = -1;
                PlayClip(CargaSounds.Select(), 0.7f);
                return;
            }
            if (_op < 0)
            {
                _sel = id;                                  // cambia de celda elegida
                PlayClip(CargaSounds.Select(), 0.7f);
                return;
            }
            Merge(_sel, (CargaOp)_op, id);
        }

        private void TapOp(CargaOp op)
        {
            if (_phase != Phase.Play) return;
            if (_sel < 0)
            {
                Tell("Primero toca una celda", false);
                return;
            }
            _op = _op == (int)op ? -1 : (int)op;
            PlayClip(CargaSounds.Operation(), 0.7f);
        }

        private void Merge(int idA, CargaOp op, int idB)
        {
            int ia = _board.IndexOfId(idA), ib = _board.IndexOfId(idB);
            var va = _views[idA];
            var vb = _views[idB];
            if (!_board.TryMerge(ia, op, ib, out var move, out var reject))
            {
                PlayClip(CargaSounds.Thud(), 0.8f);
                GameFeel.Haptic(GameFeel.HapticKind.Double);
                Tell(CargaRules.Explain(_board.Cells[ia].Value, op, _board.Cells[ib].Value, reject), false);
                _op = -1;
                return;
            }
            float now = GameClock.Time;
            // la primera celda vuela a la segunda por un arco de luz; la segunda queda con el resultado
            LaunchArc(va.Pos, vb.Target, now);
            va.Flying = true;
            va.FlyAt = now;
            va.FlyFrom = va.Pos;
            va.FlyToId = idB;
            PlayClip(CargaSounds.Merge(_board.Steps), 0.8f);
            GameFeel.Haptic(GameFeel.HapticKind.Light);
            _sel = _op = -1;
            _hintShownUntil = 0f;
            SyncViews(reborn: false);
            vb.PopAt = now;
            Emit(vb.Target, 14, 110f, 0.6f, Gold, 20f);

            int win = _board.Winning(_puzzle.Target, _puzzle.RequireAll);
            if (win >= 0)
            {
                Solve(_board.Cells[win].Id);
                return;
            }
            if (_puzzle.RequireAll && _board.HasTarget(_puzzle.Target)) Tell("¡Llegaste! Ahora usa también las demás", true);
            else if (_board.Stuck(_puzzle.Target))
            {
                Tell($"Quedó {_board.Cells[0].Value}: deshaz y prueba otro camino", false);
                PlayClip(CargaSounds.Thud(), 0.6f);
            }
        }

        /// <summary>La carga quedó exacta: el reactor se pone dorado y la celda ganadora brilla (el recuento y lo que se dice, en <see cref="PlayLoad"/>).</summary>
        private void Solve(int winningId)
        {
            _phase = Phase.Solved;
            _winId = winningId;
            _solvedAt = GameClock.Time;
            PlayClip(CargaSounds.Arpeggio(), 0.9f);
            GameFeel.Haptic(GameFeel.HapticKind.Firm);
            Emit(new Vector2(CargaLayout.W / 2f, _m.ReactorY), 40, 220f, 0.9f, Cyan, 20f);
            Emit(new Vector2(CargaLayout.W / 2f, _m.ReactorY), 30, 160f, 0.8f, Gold, 20f);
        }

        private void Undo()
        {
            if (_phase != Phase.Play || !_board.CanUndo) return;
            _board.Undo();
            _sel = _op = -1;
            SyncViews(reborn: false);
            PlayClip(CargaSounds.Back(), 0.7f);
        }

        private void Restart()
        {
            if (_phase != Phase.Play || !_board.CanUndo) return;
            _board.Reset();
            _sel = _op = -1;
            foreach (var v in _views) { v.Flying = false; }
            SyncViews(reborn: true);
            PlayClip(CargaSounds.Back(), 0.7f);
        }

        /// <summary>La pista de Nubi: vuelve a las celdas originales y dice el primer paso de una solución; esas dos celdas laten. Una vez por carga.</summary>
        private void DoHint()
        {
            _hinted = true;
            _hintAt = GameClock.Time;
            _board.Reset();
            _sel = _op = -1;
            foreach (var v in _views) v.Flying = false;
            SyncViews(reborn: false);
            var step = _puzzle.Solution[0];
            int ia = _board.IndexOfValue(step.A);
            int ib = _board.IndexOfValue(step.B, ia);
            _hintIdA = ia >= 0 ? _board.Cells[ia].Id : -1;
            _hintIdB = ib >= 0 ? _board.Cells[ib].Id : -1;
            string text = "Pista de Nubi: prueba " + step.A + " " + CargaRules.Symbol(step.Op) + " " + step.B;
            _hintShownUntil = float.MaxValue;
            _hintText = text;
            Tell(text, null);
            PlayClip(CargaSounds.Hint(), 0.8f);
        }

        private string _hintText = "";

        // ------------------------------------------------------------------ «Cómo se juega» desde la pausa

        protected override bool HowToReady => _loopOn && _phase != Phase.Done;

        protected override void HowToSuspend()
        {
            _phase = Phase.Idle;
            _inputOn = false;
            ClearLoad();
        }

        protected override void HowToResume(float spentSeconds)
        {
            _endsAt = HowToClock.Shift(_endsAt, spentSeconds);       // el tiempo que duró «Cómo se juega» no se le descuenta al Reto
            ClearLoad();
            StartCoroutine(MainLoop());
        }

        // ------------------------------------------------------------------ ronda guiada del tutorial (pieza común)

        // <guided>
        protected override IEnumerator GuidedRound(GuidedTutorial t)
        {
            t.BeginPractice();
            _guided = true;
            SetToolsVisible(false);
            var coach = t.Coach;
            var script = new GuidedScript(1);                         // una carga fácil; «Saltar tutorial» la termina
            // «Nubi entrenadora»: una carga de 3 celdas y solo sumas. Cada foco congela el juego y el toque en el hueco es la jugada de verdad:
            // 1) tocar la primera celda, 2) tocar «+», 3) tocar la otra celda (se juntan y queda la carga exacta).
            var easy = CargaGenerator.Generate(1, _rng);
            BeginLoad(easy);
            _inputOn = false;
            var first = easy.Solution[0];
            int ia = _board.IndexOfValue(first.A);
            int ib = _board.IndexOfValue(first.B, ia);
            int idA = _board.Cells[ia].Id, idB = _board.Cells[ib].Id;
            yield return Motion.Hold(0.8f);                           // las celdas terminan de entrar
            bool ok = !t.Skipped;
            if (ok)
            {
                yield return StartCoroutine(coach.Touch(() => coach.RectOf(_views[idA].Root), "Toca una celda"));
                ok = !t.Skipped;
                if (ok) TapCell(idA);                                  // el toque en el hueco ES el toque en la celda
            }
            if (ok)
            {
                yield return StartCoroutine(coach.Touch(() => coach.RectOf(_opViews[0].Root), "Elige una operación"));
                ok = !t.Skipped;
                if (ok) TapOp(CargaOp.Add);
            }
            if (ok)
            {
                yield return StartCoroutine(coach.Touch(() => coach.RectOf(_views[idB].Root), "Toca otra celda: se juntan"));
                ok = !t.Skipped;
                if (ok) TapCell(idB);
            }
            if (ok && _phase == Phase.Solved)
            {
                Tell("¡Carga exacta!", true);
                yield return StartCoroutine(coach.Notice("¡Carga exacta! Así se juega", 2.2f));
            }
            coach.Hide();
            if (ok) yield return StartCoroutine(coach.Notice("¡Listo! Ahora va en serio", 1.5f));
            _inputOn = false;
            _phase = Phase.Idle;
            ClearLoad();
            _guided = false;
            SetToolsVisible(true);
            t.EndPractice();
        }
        // </guided>

        // ------------------------------------------------------------------ entrada y animación por cuadro

        private void Update()
        {
            if (PollTutorialSkip()) return;             // un toque en «Saltar tutorial» no es un toque al juego
            float dt = GameClock.DeltaTime;
            float now = GameClock.Time;
            UpdateClock();
            AnimateReactor(now);
            AnimateCells(dt, now);
            AnimateOps();
            AnimateTools();
            AnimateArcs(now);
            AnimateSparks(dt);
            AnimateTexts(now);
            AnimateDots(now);
            if (_inputOn && dt > 0f)
            {
                ReadPress();
                HandlePress();
            }
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
            if (_tutorial != null && _tutorial.Coach != null && _tutorial.Coach.Blocks(pos)) return;
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(_play, pos, null, out var local)) return;
            _press = ToLogical(local);
        }

        private void HandlePress()
        {
            if (!_press.HasValue) return;
            var p = _press.Value;
            _press = null;
            if (_phase != Phase.Play) return;
            float hw = CargaLayout.CellW(_board.Count) / 2f + 4f, hh = CargaLayout.CellH / 2f + 4f;
            foreach (var v in _views)
            {
                if (!v.Shown || v.Flying || GameClock.Time < v.BornAt) continue;
                if (Mathf.Abs(p.x - v.Target.x) < hw && Mathf.Abs(p.y - v.Target.y) < hh) { TapCell(v.Id); return; }
            }
            for (int i = 0; i < _shownOpCount; i++)
            {
                var o = _opViews[i];
                if (o.Rect.Contains(p)) { TapOp(o.Op); return; }
            }
            foreach (var tl in _toolViews)
            {
                if (!tl.Visible || !tl.Rect.Contains(p)) continue;
                if (tl.Undo) Undo(); else Restart();
                return;
            }
        }

        private void UpdateClock()
        {
            if (!Endless || !_loopOn || _endsAt <= 0f || _phase == Phase.Done) return;
            float left = _endsAt - GameClock.Time;
            float frac = Mathf.Clamp01(left / CalculoContract.RetoSeconds);
            _timerFill.rectTransform.anchorMax = new Vector2(frac, 1f);
            _timerHead.anchorMin = _timerHead.anchorMax = new Vector2(frac, 0.5f);
            int whole = Mathf.CeilToInt(left);
            if (whole <= 5 && whole >= 1 && whole != _lastTickSecond)
            {
                _lastTickSecond = whole;
                GameFeel.Tick();
            }
        }

        /// <summary>El reactor: el aro de luces gira despacio; al lograr la carga todo se pone dorado (con «quitar animaciones» no gira ni brilla, pero el color dorado se queda).</summary>
        private void AnimateReactor(float now)
        {
            _solvedK = _phase == Phase.Solved ? (Motion.Decorative ? Mathf.Clamp01((now - _solvedAt) / 0.7f) : 1f) : 0f;
            float k = _solvedK;
            _reactorGlow.gameObject.SetActive(Motion.Decorative);
            _reactorGlow.color = Color.Lerp(NeuroStyle.WithAlpha(Cyan, 0.30f), NeuroStyle.WithAlpha(Gold, 0.5f), k);
            float spin = Motion.Decorative ? now / 1.6f : 0f;
            var center = P(CargaLayout.W / 2f, _m.ReactorY);
            for (int i = 0; i < _lights.Length; i++)
            {
                float a = spin + i * Mathf.PI / 6f;
                float r = (CargaLayout.CoreR + 3f) * _s;
                _lights[i].rectTransform.anchoredPosition = center + new Vector2(Mathf.Cos(a), -Mathf.Sin(a)) * r;
                _lights[i].color = Color.Lerp(NeuroStyle.WithAlpha(Cyan, 0.35f), NeuroStyle.WithAlpha(Gold, 1f), k);
            }
            _core.color = Color.Lerp(CoreDark, CoreGold, k);
            _coreHi.color = Color.Lerp(NeuroStyle.WithAlpha(CoreLight, 0.9f), NeuroStyle.WithAlpha(CoreGoldLight, 0.95f), k);
            _reactorLabel.color = Color.Lerp(Lavender, NeuroStyle.Ink, k);
            _targetText.color = Color.Lerp(Color.white, NeuroStyle.Ink, k);
            _targetText.rectTransform.localScale = Vector3.one * (1f + 0.11f * k * (Motion.Decorative ? 1f : 0f));
            _allText.color = Color.Lerp(Gold, NeuroStyle.Ink, k);
        }

        private void AnimateCells(float dt, float now)
        {
            float follow = 1f - Mathf.Exp(-14f * dt);
            foreach (var v in _views)
            {
                if (!v.Shown) continue;
                if (v.Flying)
                {
                    var to = v.FlyToId >= 0 && _views[v.FlyToId].Shown ? _views[v.FlyToId].Pos : v.Pos;
                    float fk = Mathf.Clamp01((now - v.FlyAt) / (Motion.Decorative ? 0.2f : Motion.FadeSeconds));
                    if (Motion.Decorative) v.Pos = Vector2.Lerp(v.FlyFrom, to, UiFx.EaseOutCubic(fk));
                    v.Group.alpha = 1f - fk;
                    ApplyView(v, now, 1f, 1f - fk, false, false, false);
                    if (fk >= 1f) { v.Shown = false; v.Flying = false; v.Root.gameObject.SetActive(false); }
                    continue;
                }
                v.Pos = Motion.Decorative ? Vector2.Lerp(v.Pos, v.Target, follow) : v.Target;
                float born = Mathf.Clamp01((now - v.BornAt) / (Motion.Decorative ? 0.26f : Motion.FadeSeconds));
                if (now < v.BornAt) born = 0f;
                float pop = Motion.Decorative ? Mathf.Max(0f, 1f - (now - v.PopAt) / 0.4f) : 0f;
                bool sel = _sel == v.Id;
                bool win = _winId == v.Id && _phase != Phase.Idle;
                bool hint = _hinted && _board != null && _board.Steps == 0 && (v.Id == _hintIdA || v.Id == _hintIdB) && _phase == Phase.Play;
                float scale = (1f + 0.18f * pop) * (sel && Motion.Decorative ? 1.08f : 1f) * (Motion.Decorative ? UiFx.EaseOutBack(born) : 1f);
                ApplyView(v, now, scale, born, sel, win, hint);
            }
        }

        private void ApplyView(CellView v, float now, float scale, float alpha, bool sel, bool win, bool hint)
        {
            float lift = sel && Motion.Decorative ? 6f : 0f;
            v.Root.anchoredPosition = P(v.Pos + new Vector2(0f, -lift));
            v.Root.localScale = Vector3.one * Mathf.Max(0.01f, scale);
            v.Group.alpha = alpha;
            v.Face.color = win ? CellWin : sel ? CellSelected : CellNormal;
            bool glow = (sel || win) && Motion.Decorative;
            v.Glow.gameObject.SetActive(glow);
            if (glow) v.Glow.color = win ? NeuroStyle.WithAlpha(Gold, 1f) : NeuroStyle.WithAlpha(Cyan, 1f);
            v.Halo.gameObject.SetActive(hint);
            if (hint) v.Halo.color = NeuroStyle.WithAlpha(Gold, Motion.Decorative ? 0.5f + 0.5f * Mathf.Sin(now * 5f) : 1f);
            if (sel || win) v.Root.SetAsLastSibling();
        }

        private void AnimateOps()
        {
            bool canPick = _phase == Phase.Play && _sel >= 0;
            for (int i = 0; i < _shownOpCount; i++)
            {
                var o = _opViews[i];
                bool on = _op == (int)o.Op;
                o.Bg.color = on ? Gold : OpIdle;
                o.Label.color = on ? NeuroStyle.Ink : ButtonText;
                o.Group.alpha = canPick ? 1f : 0.45f;
            }
        }

        private void AnimateTools()
        {
            bool can = _phase == Phase.Play && _board != null && _board.CanUndo;
            foreach (var t in _toolViews) if (t.Visible) t.Group.alpha = can ? 1f : 0.45f;
        }

        private void AnimateTexts(float now)
        {
            // el mensaje de Nubi (como en el boceto): aparece, se queda ~2 s y se va; la pista se queda hasta la primera jugada
            if (_sayFull.Length > 0)
            {
                float k = (now - _sayAt) / 2.2f;
                bool sticky = _sayFull == _hintText && _hintShownUntil > now && _board != null && _board.Steps == 0 && _phase == Phase.Play;
                if (k >= 1f && !sticky) { _sayFull = ""; _sayText.gameObject.SetActive(false); }
                else
                {
                    _sayText.gameObject.SetActive(true);
                    _sayText.text = _sayFull;
                    _sayText.color = new Color(_sayColor.r, _sayColor.g, _sayColor.b, sticky ? 1f : Mathf.Min(1f, k * 8f, (1f - k) * 4f));
                }
            }
            else _sayText.gameObject.SetActive(false);

            if (_phase == Phase.Play && _board != null)
            {
                string guide;
                if (_sel < 0) guide = "Toca una celda";
                else if (_op < 0) guide = "Ahora toca una operación";
                else guide = _views[_sel].Value + " " + CargaRules.Symbol((CargaOp)_op) + " … toca otra celda";
                _guideText.text = guide;
                _guideText.gameObject.SetActive(true);
            }
            else _guideText.gameObject.SetActive(false);
        }

        private void AnimateDots(float now)
        {
            bool show = Precision && !_guided && _phase != Phase.Done;
            for (int i = 0; i < _dots.Count; i++)
            {
                var d = _dots[i];
                d.gameObject.SetActive(show);
                if (!show) continue;
                int st = _dotState[i];
                bool current = st == 0 && i == _resolved;
                d.color = st == 2 ? Gold : st == 3 ? Silver : st == 4 ? Dim : current ? NeuroStyle.WithAlpha(Cyan, 0.9f) : new Color(1f, 1f, 1f, 0.16f);
                float size = (st == 2 || st == 3 ? 10f : st == 4 ? 7f : current ? 9f : 8f) * _s;
                d.rectTransform.sizeDelta = Vector2.one * size;
            }
        }

        // ---- arcos y chispas

        private void LaunchArc(Vector2 from, Vector2 to, float now)
        {
            if (!Motion.Decorative) return;
            foreach (var a in _arcs)
            {
                if (a.Alive) continue;
                a.Alive = true;
                a.A = from;
                a.B = to;
                a.At = now;
                foreach (var d in a.Dots) d.gameObject.SetActive(true);
                return;
            }
        }

        private void AnimateArcs(float now)
        {
            foreach (var a in _arcs)
            {
                if (!a.Alive) continue;
                float k = (now - a.At) / 0.45f;
                if (k >= 1f) { a.Alive = false; foreach (var d in a.Dots) d.gameObject.SetActive(false); continue; }
                var mid = new Vector2((a.A.x + a.B.x) / 2f, Mathf.Min(a.A.y, a.B.y) - 50f);
                for (int i = 0; i < a.Dots.Length; i++)
                {
                    float t = i / (float)(a.Dots.Length - 1);
                    var pos = (1 - t) * (1 - t) * a.A + 2 * (1 - t) * t * mid + t * t * a.B;
                    a.Dots[i].rectTransform.anchoredPosition = P(pos);
                    a.Dots[i].rectTransform.sizeDelta = Vector2.one * ((3.5f * (1f - k) + 2f) * 2f * _s);
                    a.Dots[i].color = NeuroStyle.WithAlpha(Gold, 1f - k);
                }
            }
        }

        private void Emit(Vector2 at, int n, float spread, float life, Color color, float up)
        {
            if (!Motion.Decorative) return;
            for (int i = 0; i < n; i++)
            {
                var p = _sparks.Find(x => !x.Alive);
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

        private void AnimateSparks(float dt)
        {
            foreach (var p in _sparks)
            {
                if (!p.Alive) continue;
                p.Age += dt;
                if (p.Age >= p.Life) { p.Alive = false; p.Img.gameObject.SetActive(false); continue; }
                p.Vel.y += 60f * dt;
                p.Pos += p.Vel * dt;
                float k = p.Age / p.Life;
                p.Img.rectTransform.anchoredPosition = P(p.Pos);
                p.Img.rectTransform.sizeDelta = Vector2.one * (p.Size * 2f * (1f - k * 0.5f) * _s);
                p.Img.color = NeuroStyle.WithAlpha(p.Color, 0.9f * (1f - k));
            }
        }

        /// <summary>Lo que dice la línea de Nubi (bajo el reactor). <paramref name="good"/>: true = verde, false = ámbar, null = dorado (pista).</summary>
        private void Tell(string text, bool? good)
        {
            _sayFull = text;
            _sayAt = GameClock.Time;
            _sayColor = good == true ? GoodText : good == false ? WarnText : Gold;
        }

        private void PlayClip(AudioClip clip, float volume)
        {
            if (clip == null || !GameFeel.SoundOn) return;
            if (_config != null && _config.config != null && !_config.config.sound_enabled) return;
            _audioSource.PlayOneShot(clip, volume);
        }

        // ------------------------------------------------------------------ construcción de UI

        protected override void BuildUi()
        {
            var canvasGo = new GameObject("CargaCanvas");
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
            // el cielo quieto de Rastro de luz (sin agua): aquí lo que se mueve es la tarea
            WorldBackdrop.Build(bgRect, GameWorld.CieloDeCristal);

            var safeGo = new GameObject("SafeAreaContent");
            safeGo.transform.SetParent(canvasGo.transform, false);
            _safe = safeGo.AddComponent<RectTransform>();
            ApplySafeArea(_safe);

            _hud = new GameHud(_safe, "Carga exacta", MarginU, this);
            BuildTimer();

            var playGo = new GameObject("Play");
            playGo.transform.SetParent(_safe, false);
            _play = playGo.AddComponent<RectTransform>();
            Stretch(_play);

            _reactorLayer = Layer(_play, "Reactor");
            _cellLayer = Layer(_play, "Cells");
            _uiLayer = Layer(_play, "Ui");
            _fxLayer = Layer(_play, "Fx");
            BuildReactor();
            BuildCells();
            BuildUiPieces();
            BuildFx();

            BuildResultPanel();
            _exit = new ExitButton(_safe, this, UnitsPerDp);

            var flashGo = new GameObject("Flash");
            flashGo.transform.SetParent(canvasGo.transform, false);
            Stretch(flashGo.AddComponent<RectTransform>());
            _flash = flashGo.AddComponent<Image>();
            _flash.raycastTarget = false;
            _flash.color = new Color(0f, 0f, 0f, 0f);

            _countdown = new CountdownScreen(canvasGo.transform, UnitsPerDp);
            BuildTutorial(_safe, GameHud.Height + 10f, "Carga exacta", "Junta las celdas de dos en dos hasta que una valga la carga que pide el reactor.", badgeAtBottom: true);
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
        }

        private void BuildReactor()
        {
            _reactorGlow = MakeImage(_reactorLayer, "Glow", RadialGlowSprite.Get());
            _reactorShadow = MakeImage(_reactorLayer, "Shadow", DiscSprite.Get());
            _reactorShadow.color = new Color(0f, 0f, 0f, 0.45f);
            _reactorRim = MakeImage(_reactorLayer, "Rim", DiscSprite.Get());
            _reactorRim.color = NeuroStyle.Ink;
            _reactorBody = MakeImage(_reactorLayer, "Body", DiscSprite.Get());
            _reactorBody.color = RingBody;
            for (int i = 0; i < _lights.Length; i++) _lights[i] = MakeImage(_reactorLayer, "Light" + i, DiscSprite.Get());
            _core = MakeImage(_reactorLayer, "Core", DiscSprite.Get());
            _coreHi = MakeImage(_reactorLayer, "CoreHi", RadialGlowSprite.Get());
            _reactorLabel = MakeLabel(_reactorLayer, "Label", 14f, UiFonts.Regular, Lavender, TextAnchor.MiddleCenter);
            _reactorLabel.text = "carga exacta";
            _targetText = MakeLabel(_reactorLayer, "Target", 52f, UiFonts.Bold, Color.white, TextAnchor.MiddleCenter);
            _targetText.text = "0";
            _allText = MakeLabel(_reactorLayer, "All", 14f, UiFonts.Bold, Gold, TextAnchor.MiddleCenter);
            _allText.text = "usa todas las celdas";
            _allText.gameObject.SetActive(false);
        }

        private void BuildCells()
        {
            for (int i = 0; i < MaxCells; i++)
            {
                var v = new CellView { Id = i };
                var go = new GameObject("Cell" + i);
                go.transform.SetParent(_cellLayer, false);
                v.Root = go.AddComponent<RectTransform>();
                v.Root.anchorMin = v.Root.anchorMax = v.Root.pivot = new Vector2(0.5f, 0.5f);
                v.Group = go.AddComponent<CanvasGroup>();
                v.Group.blocksRaycasts = false;
                v.Halo = MakeImage(v.Root, "Halo", RoundedRectSprite.Get(64));
                v.Halo.type = Image.Type.Sliced;
                v.Halo.color = Gold;
                v.Glow = MakeImage(v.Root, "Glow", RadialGlowSprite.Get());
                v.Face = MakeImage(v.Root, "Face", RoundedRectSprite.Get(60), true);
                v.Face.type = Image.Type.Sliced;
                v.Face.color = CellNormal;
                NeuroStyle.ClayFrame(v.Face, 4f, 12f);             // celda de arcilla: borde tinta y sombra dura
                v.Window = MakeImage(v.Root, "Window", RoundedRectSprite.Get(16), true);   // la ventanita de energía
                v.Window.type = Image.Type.Sliced;
                v.Window.color = CellWindow;
                v.Shine = MakeImage(v.Root, "Shine", RadialGlowSprite.Get(), true);
                v.Shine.color = new Color(1f, 1f, 1f, 0.55f);
                v.Label = MakeLabel(v.Root, "Value", 30f, UiFonts.Bold, NeuroStyle.Ink, TextAnchor.MiddleCenter);
                v.Label.horizontalOverflow = HorizontalWrapMode.Overflow;
                _fonts.RemoveAt(_fonts.Count - 1);                 // el tamaño lo fija SetValue según las cifras
                go.SetActive(false);
                _views[i] = v;
            }
        }

        private void BuildUiPieces()
        {
            _sayText = MakeLabel(_uiLayer, "Say", 18f, UiFonts.Bold, GoodText, TextAnchor.MiddleCenter);
            _sayText.gameObject.SetActive(false);
            BestFit(_sayText, Mathf.RoundToInt(14f * UnitsPerDp));
            _guideText = MakeLabel(_uiLayer, "Guide", 16f, UiFonts.Regular, Lavender, TextAnchor.MiddleCenter);
            for (int i = 0; i < _opViews.Length; i++)
            {
                var o = new OpView();
                var go = new GameObject("Op" + i);
                go.transform.SetParent(_uiLayer, false);
                o.Root = go.AddComponent<RectTransform>();
                o.Root.anchorMin = o.Root.anchorMax = o.Root.pivot = new Vector2(0.5f, 0.5f);
                o.Group = go.AddComponent<CanvasGroup>();
                o.Group.blocksRaycasts = false;
                o.Bg = go.AddComponent<Image>();
                o.Bg.sprite = RoundedRectSprite.Get(48);
                o.Bg.type = Image.Type.Sliced;
                o.Bg.color = OpIdle;
                o.Bg.raycastTarget = false;
                NeuroStyle.ClayFrame(o.Bg, 4f, 8f);
                o.Label = MakeText(o.Root, "Label", Mathf.RoundToInt(30f * UnitsPerDp), TextAnchor.MiddleCenter, ButtonText, 0f, 0f);
                o.Label.horizontalOverflow = HorizontalWrapMode.Overflow;
                _fonts.Add(new KeyValuePair<Text, float>(o.Label, 30f));
                go.SetActive(false);
                _opViews[i] = o;
            }
            for (int i = 0; i < 2; i++)
            {
                var t = new ToolView { Undo = i == 0 };
                var go = new GameObject(i == 0 ? "Undo" : "Restart");
                go.transform.SetParent(_uiLayer, false);
                t.Root = go.AddComponent<RectTransform>();
                t.Root.anchorMin = t.Root.anchorMax = t.Root.pivot = new Vector2(0.5f, 0.5f);
                t.Group = go.AddComponent<CanvasGroup>();
                t.Group.blocksRaycasts = false;
                t.Bg = go.AddComponent<Image>();
                t.Bg.sprite = RoundedRectSprite.Get(48);
                t.Bg.type = Image.Type.Sliced;
                t.Bg.color = ToolFill;
                t.Bg.raycastTarget = false;
                NeuroStyle.ClayFrame(t.Bg, 3f, 6f);
                t.Label = MakeText(t.Root, "Label", Mathf.RoundToInt(16f * UnitsPerDp), TextAnchor.MiddleCenter, ButtonText, 0f, 0f);
                t.Label.font = UiFonts.Regular;
                t.Label.text = i == 0 ? "Deshacer" : "Empezar de nuevo";
                BestFit(t.Label, Mathf.RoundToInt(14f * UnitsPerDp));
                _fonts.Add(new KeyValuePair<Text, float>(t.Label, 16f));
                _toolViews[i] = t;
            }
            for (int i = 0; i < CalculoContract.PrecisionLoads; i++)
            {
                var d = MakeImage(_uiLayer, "Dot" + i, DiscSprite.Get());
                d.gameObject.SetActive(false);
                _dots.Add(d);
            }
        }

        private void BuildFx()
        {
            for (int a = 0; a < _arcs.Length; a++)
            {
                var arc = new Arc { Dots = new Image[ArcDots] };
                for (int i = 0; i < ArcDots; i++)
                {
                    var d = MakeImage(_fxLayer, "Arc" + a + "_" + i, DiscSprite.Get());
                    d.gameObject.SetActive(false);
                    arc.Dots[i] = d;
                }
                _arcs[a] = arc;
            }
            for (int i = 0; i < 70; i++)
            {
                var img = MakeImage(_fxLayer, "Spark", DiscSprite.Get());
                img.gameObject.SetActive(false);
                _sparks.Add(new Spark { Img = img });
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

        /// <summary>Una imagen centrada en su padre. Nace ENCENDIDA (<paramref name="active"/>): las piezas que se prenden y apagan a propósito lo dicen aparte.</summary>
        private static Image MakeImage(Transform parent, string name, Sprite sprite, bool active = true)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var r = go.AddComponent<RectTransform>();
            r.anchorMin = r.anchorMax = new Vector2(0.5f, 0.5f);
            r.pivot = new Vector2(0.5f, 0.5f);
            var img = go.AddComponent<Image>();
            img.sprite = sprite;
            img.raycastTarget = false;
            go.SetActive(active);
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

        /// <summary>Para las pruebas: devuelve lo que NO se vería (pieza apagada o sin opacidad) del reactor, una celda, una operación, los botones y las chispas. Una celda con
        /// la cara apagada deja solo el número tinta sobre el cielo oscuro y el juego queda injugable (lo que pasó con las fichas de Punta el 3-oct).</summary>
        public List<string> AuditVisibility()
        {
            var problems = new List<string>();
            _s = 3f; _playW = 1080f; _playH = 1920f; _logicalH = 640f;
            _m = CargaLayout.Compute(_logicalH);
            if (_rng == null) _rng = new System.Random(1);
            void Check(string what, Graphic g, bool needsAlpha = true)
            {
                if (g == null) { problems.Add(what + ": no existe"); return; }
                if (!g.gameObject.activeInHierarchy) problems.Add(what + ": apagada");
                else if (needsAlpha && g.color.a <= 0.01f) problems.Add(what + ": transparente");
            }
            Check("reactor.Shadow", _reactorShadow);
            Check("reactor.Rim", _reactorRim);
            Check("reactor.Body", _reactorBody);
            Check("reactor.Core", _core, false);          // el color lo fija AnimateReactor
            Check("reactor.CoreHi", _coreHi, false);
            foreach (var l in _lights) Check("reactor.Luz", l, false);
            Check("reactor.Carga", _targetText);
            Check("reactor.Rótulo", _reactorLabel);
            var cell = _views[0];
            cell.Root.gameObject.SetActive(true);
            Check("celda.Face", cell.Face);
            Check("celda.Window", cell.Window);
            Check("celda.Shine", cell.Shine);
            Check("celda.Label", cell.Label);
            foreach (var o in _opViews)
            {
                o.Root.gameObject.SetActive(true);
                Check("operación.fondo", o.Bg);
                Check("operación.texto", o.Label);
            }
            foreach (var t in _toolViews)
            {
                t.Root.gameObject.SetActive(true);
                Check("botón " + t.Label.text + ".fondo", t.Bg);
                Check("botón " + t.Label.text + ".texto", t.Label);
            }
            foreach (var a in _arcs) foreach (var d in a.Dots) if (d == null) problems.Add("arco: punto no existe");
            if (_sparks.Count == 0) problems.Add("chispas: no hay");
            return problems;   // nada se destruye aquí (en modo edición no se puede): quien llama destruye el objeto entero
        }

        // ------------------------------------------------------------------ disposición

        private Vector2 P(Vector2 l) => new Vector2((l.x - CargaLayout.W * 0.5f) * _s, _playH * 0.5f - l.y * _s);
        private Vector2 P(float x, float y) => P(new Vector2(x, y));
        private Vector2 ToLogical(Vector2 u) => new Vector2(u.x / _s + CargaLayout.W * 0.5f, (_playH * 0.5f - u.y) / _s);

        private void SetRect(RectTransform r, float cx, float cy, float w, float h)
        {
            r.sizeDelta = new Vector2(w * _s, h * _s);
            r.anchoredPosition = P(cx, cy);
        }

        private void Layout()
        {
            Canvas.ForceUpdateCanvases();
            _playW = _play.rect.width;
            _playH = _play.rect.height;
            _s = _playW / CargaLayout.W;
            _logicalH = _playH / _s;
            foreach (var kv in _fonts)
            {
                int px = Mathf.RoundToInt(kv.Value * _s);
                kv.Key.fontSize = px;
                if (kv.Key.resizeTextForBestFit) kv.Key.resizeTextMaxSize = px;
            }
            _sayText.resizeTextMinSize = Mathf.RoundToInt(14f * _s);
            foreach (var t in _toolViews) t.Label.resizeTextMinSize = Mathf.RoundToInt(14f * _s);
            _m = CargaLayout.Compute(_logicalH);

            // reactor
            float cx = CargaLayout.W / 2f, cy = _m.ReactorY;
            float R = CargaLayout.CoreR, outer = CargaLayout.RingR;
            SetRect(_reactorGlow.rectTransform, cx, cy, R * 4.4f, R * 4.4f);
            SetRect(_reactorShadow.rectTransform, cx, cy + 6f, outer * 2f, outer * 2f);
            SetRect(_reactorRim.rectTransform, cx, cy, outer * 2f + 4f, outer * 2f + 4f);
            SetRect(_reactorBody.rectTransform, cx, cy, outer * 2f, outer * 2f);
            foreach (var l in _lights) l.rectTransform.sizeDelta = Vector2.one * (6.4f * _s);
            SetRect(_core.rectTransform, cx, cy, R * 2f, R * 2f);
            SetRect(_coreHi.rectTransform, cx, cy - 10f, R * 1.7f, R * 1.7f);
            SetRect(_reactorLabel.rectTransform, cx, cy - 30f, 150f, 20f);
            SetRect(_targetText.rectTransform, cx, cy + 8f, 140f, 64f);
            SetRect(_allText.rectTransform, cx, cy + 46f, 150f, 20f);
            SetRect(_sayText.rectTransform, cx, _m.SayY, 336f, 30f);
            SetRect(_guideText.rectTransform, cx, _m.GuideY, 336f, 24f);

            // celdas (tamaño de la cara y de las piezas; la posición la pone SyncViews)
            foreach (var v in _views) SizeCell(v, _board != null ? _board.Count : 4);
            if (_board != null)
                for (int k = 0; k < _board.Count; k++) _views[_board.Cells[k].Id].Target = CargaLayout.CellPos(_m, k, _board.Count);

            // operaciones, botones y puntos
            if (_puzzle != null) SetOps(_puzzle.Ops);
            else SetOps(CalculoContract.Levels[0].Ops);
            for (int i = 0; i < _toolViews.Length; i++)
            {
                var t = _toolViews[i];
                var pos = CargaLayout.ToolPos(_m, i);
                t.Rect = new Rect(pos.x - CargaLayout.ToolW / 2f, pos.y - CargaLayout.ToolH / 2f, CargaLayout.ToolW, CargaLayout.ToolH);
                SetRect(t.Root, pos.x, pos.y, CargaLayout.ToolW, CargaLayout.ToolH);
                t.Label.rectTransform.offsetMin = new Vector2(8f * _s, 0f);
                t.Label.rectTransform.offsetMax = new Vector2(-8f * _s, 0f);
            }
            float spacing = 14f, total = (_dots.Count - 1) * spacing;
            for (int i = 0; i < _dots.Count; i++) _dots[i].rectTransform.anchoredPosition = P(cx - total / 2f + i * spacing, _m.DotsY);
        }

        private void SizeCell(CellView v, int n)
        {
            float w = CargaLayout.CellW(n), h = CargaLayout.CellH;
            v.Root.sizeDelta = new Vector2(w * _s, h * _s);
            SetChild(v.Halo.rectTransform, 0f, 0f, w + 12f, h + 12f);
            SetChild(v.Glow.rectTransform, 0f, 0f, 120f, 120f);
            SetChild(v.Face.rectTransform, 0f, 0f, w, h);
            SetChild(v.Window.rectTransform, 0f, h / 2f - 12f, w - 16f, 8f);
            SetChild(v.Shine.rectTransform, -w * 0.22f, -h * 0.3f, w * 0.4f, h * 0.2f);
            var l = v.Label.rectTransform;
            l.sizeDelta = new Vector2((w - 4f) * _s, h * 0.7f * _s);
            l.anchoredPosition = new Vector2(0f, 4f * _s);
        }

        /// <summary>Coloca una pieza respecto del centro de su celda (dp; y hacia abajo).</summary>
        private void SetChild(RectTransform r, float dx, float dy, float w, float h)
        {
            r.sizeDelta = new Vector2(w * _s, h * _s);
            r.anchoredPosition = new Vector2(dx * _s, -dy * _s);
        }
    }
}
