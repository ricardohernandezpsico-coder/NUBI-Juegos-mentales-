using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using NeuroVida.Bridge;
using NeuroVida.Contracts;
using NeuroVida.Games.Parejas;   // SymbolSprite (cometas de la patrulla)
using NeuroVida.Games.Secuencia; // RoundedRectSprite / RadialGlowSprite / RingSprite
using NeuroVida.Games.Shared;
using NeuroVida.Games.Trafico;   // TrafficSprites.Port (planetas de colores con símbolo)
using static NeuroVida.Games.Shared.UiKit;

namespace NeuroVida.Games.Bitacora
{
    /// <summary>
    /// "Bitácora de Misión": juego estrella de memoria episódica con recuerdo DIFERIDO (ver <see cref="BitacoraContract"/>).
    /// <list type="bullet">
    /// <item><b>Transmisión</b>: una sonda recorre planetas del mapa y en cada uno aparece un hallazgo (con destello, su
    /// nota y su nombre). La persona lo toca para guardarlo en la bitácora (hacerlo uno mismo ayuda a recordarlo).</item>
    /// <item><b>Primer repaso</b>: planeta por planeta, elegir qué había (con respuesta al instante: se aprende).</item>
    /// <item><b>Espera</b>: en la sesión diaria, los otros juegos; al jugar suelto, una patrulla de 45 s (atrapar
    /// cometas) que ocupa la mente sin repasar.</item>
    /// <item><b>Informe</b>: qué había en cada planeta (sin ayuda) y en qué orden pasó la sonda; al final, la misión se
    /// revela y lo recordado se archiva.</item>
    /// </list>
    /// Fases por configuración (<c>memory_phase</c>): "" = todo; "encode" = transmisión y repaso; "recall" = informe.
    /// </summary>
    public class BitacoraGameController : GameControllerBase
    {
        public const string GameId = BitacoraContract.GameId;

        private const float UnitsPerDp = 3f;
        private const float MarginU = 40f;
        private const float PlanetSize = 150f, FindOnPlanet = 124f, ProbeSize = 96f, SlotSize = 92f, OptionSize = 150f;
        private const float FindTapWait = 5f;

        private static readonly Color GoodColor = NeuroStyle.Lime;
        private static readonly Color BadColor = NeuroStyle.Coral;
        private static readonly Color Deep = NeuroStyle.Hex(0x1B2466);

        private enum Wait { None, Find, Option, Planet, Comet }

        private sealed class PlanetView
        {
            public RectTransform Rect;
            public Image Body, Glow, Ring, Find, Mark;
            public Text Label;
            public RectTransform Badge;
            public Text BadgeText;
        }

        private sealed class OptionView
        {
            public RectTransform Rect;
            public Image Bg, Img;
            public int Find;
        }

        private System.Random _rng;
        private AdaptiveDifficulty _dda;
        private string _phaseMode = "";
        private Mission _m;
        private bool[] _learned, _recalled;
        private int[] _chosen;
        private readonly List<int> _routeTaps = new List<int>();
        private int _points, _streak, _bestStreak, _caught, _previousFrameRate;
        private float _startedAt;
        private bool _done;

        // entrada pendiente
        private Wait _wait = Wait.None;
        private int _picked = -1;

        // UI
        private RectTransform _safe, _play, _map, _fx, _logRoot, _drawerRoot, _probe, _trailRoot, _patrolRoot;
        private float _mapW, _mapH;
        private Text _caption, _sub, _logLabel;
        private readonly List<PlanetView> _planets = new List<PlanetView>();
        private readonly List<Image> _slotBgs = new List<Image>();
        private readonly List<Image> _slotFinds = new List<Image>();
        private readonly List<OptionView> _options = new List<OptionView>();
        private readonly List<RectTransform> _trail = new List<RectTransform>();
        private readonly List<RectTransform> _comets = new List<RectTransform>();
        private readonly List<Vector2> _cometVel = new List<Vector2>();
        private Image _findHint;
        private Toast _toast;
        private ExitButton _exit;
        private GameHud _hud;
        private CountdownScreen _countdown;

        // ------------------------------------------------------------------ sesión

        public void StartSession(SequenceInitConfig config)
        {
            _config = config;
            _rng = new System.Random();
            var c = config.config;
            _phaseMode = c.memory_phase ?? "";
            var age = DdaUserProfileConfig.ParseAgeBand(c.age_band);
            float start = AdaptiveDifficulty.StartRating(c, BitacoraContract.MaxLevel);
            // Un ensayo por parada del informe (pocos por partida): pasos grandes.
            _dda = new AdaptiveDifficulty(BitacoraContract.MaxLevel, age, start, stepUp: 0.5f, useReaction: false);
            int level = _phaseMode == "recall" && c.memory_level > 0 ? c.memory_level : _dda.Level;
            int seed = c.memory_seed != 0 ? c.memory_seed : _rng.Next(1, int.MaxValue);
            _m = BitacoraContract.Generate(seed, level);
            _learned = new bool[_m.Items];
            _recalled = new bool[_m.Items];
            _chosen = new int[_m.Items];
            for (int i = 0; i < _chosen.Length; i++) _chosen[i] = -1;
            _routeTaps.Clear();
            _points = _streak = _bestStreak = _caught = 0;
            _done = false;
            _wait = Wait.None;

            _previousFrameRate = Application.targetFrameRate;
            Application.targetFrameRate = 60;

            _resultRoot.gameObject.SetActive(false);
            _exit.Hide();
            _hud.SetStreak(0);
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
            string subtitle = _phaseMode == "encode" ? "Transmisión del día" : _phaseMode == "recall" ? "Informe de la bitácora" : "Prepárate";
            yield return StartCoroutine(_countdown.Play("Bitácora de Misión", Assessment.Subtitle(subtitle), () => _safe.gameObject.SetActive(true)));
            _safe.gameObject.SetActive(true);
            yield return null;
            ApplySafeArea(_safe);
            Canvas.ForceUpdateCanvases();
            Layout();
            _startedAt = GameClock.Time;
            _hud.SetLevel(_m.Level);
            UpdateHud("");

            yield return StartCoroutine(ShowMap());
            if (_phaseMode != "recall")
            {
                yield return StartCoroutine(Transmission());
                yield return StartCoroutine(Review());
                if (_phaseMode == "encode")
                {
                    FinishEncode();
                    yield break;
                }
                yield return StartCoroutine(Patrol());
            }
            yield return StartCoroutine(Report());
            yield return StartCoroutine(RouteOrder());
            yield return StartCoroutine(Reveal());
            FinishReport();
        }

        // ------------------------------------------------------------------ fases

        private IEnumerator ShowMap()
        {
            for (int p = 0; p < _planets.Count; p++)
            {
                var v = _planets[p];
                bool on = p < _m.Planets;
                v.Rect.gameObject.SetActive(on);
                if (!on) continue;
                v.Rect.anchoredPosition = MapPos(_m.PlanetX[p], _m.PlanetY[p]);
                v.Body.sprite = TrafficSprites.Port(_m.PlanetColors[p]);
                v.Body.color = Color.white;
                v.Label.text = BitacoraContract.PlanetNames[_m.PlanetColors[p]];
                v.Glow.color = NeuroStyle.WithAlpha(TrafficSprites.Colors[_m.PlanetColors[p]], 0f);
                v.Find.gameObject.SetActive(false);
                v.Mark.gameObject.SetActive(false);
                v.Ring.gameObject.SetActive(false);
                v.Badge.gameObject.SetActive(false);
                StartCoroutine(PopInDelayed(v.Rect, 0.07f * p));
            }
            _probe.anchoredPosition = BasePos();
            _probe.gameObject.SetActive(false);
            ClearTrail();
            for (int i = 0; i < _slotBgs.Count; i++)
            {
                bool on = i < _m.Items;
                _slotBgs[i].gameObject.SetActive(on);
                _slotFinds[i].gameObject.SetActive(false);
            }
            _logRoot.gameObject.SetActive(_phaseMode != "recall");
            _drawerRoot.gameObject.SetActive(false);
            LayoutSlots();
            yield return StartCoroutine(Pause(0.3f + 0.07f * _m.Planets));
        }

        private IEnumerator Transmission()
        {
            SetCaption("Llega una transmisión", "Toca cada hallazgo para guardarlo en la bitácora");
            _toast.Show("Llega una transmisión", "Guarda cada hallazgo en la bitácora", NeuroStyle.Sky, 2f);
            Sfx(BitacoraSounds.Incoming(), 0.5f);
            yield return StartCoroutine(Pause(2.2f));
            _toast.Show("Truco", "Imagina cada hallazgo EN su planeta, como una escena", NeuroStyle.Sun, 2.6f);
            yield return StartCoroutine(Pause(2.8f));

            _probe.gameObject.SetActive(true);
            StartCoroutine(PopIn(_probe, 0.25f));
            for (int i = 0; i < _m.Items; i++)
            {
                int p = _m.Route[i];
                var v = _planets[p];
                UpdateHud($"Parada {i + 1} de {_m.Items}");
                yield return StartCoroutine(FlyProbe(v.Rect.anchoredPosition + new Vector2(0f, PlanetSize * 0.62f)));

                // Llega: el planeta se enciende y el hallazgo aparece con un destello y su nota.
                SetGlow(v, 0.55f);
                StartCoroutine(UiFx.RingBurst(_fx, LocalIn(_fx, v.Rect), TrafficSprites.Colors[_m.PlanetColors[p]], 110f, 300f, 0.45f));
                v.Find.sprite = BitacoraSprites.Find(_m.Finds[i]);
                v.Find.color = Color.white;
                v.Find.rectTransform.anchoredPosition = new Vector2(0f, PlanetSize * 0.62f);
                v.Find.rectTransform.localScale = Vector3.one;
                v.Find.gameObject.SetActive(true);
                StartCoroutine(PopIn(v.Find.rectTransform, 0.3f));
                StartCoroutine(UiFx.SparkBurst(_fx, LocalIn(_fx, v.Find.rectTransform), NeuroStyle.Sun, 10, 120f, 26f));
                Sfx(BitacoraSounds.Reveal(i), 0.55f);
                SetCaption($"Planeta {PlanetName(p)} · {BitacoraContract.FindNames[_m.Finds[i]]}", "Tócalo para guardarlo");

                // Hay que tocarlo (guardarlo uno mismo ayuda a recordarlo); si no, se guarda solo al rato.
                _findHint.gameObject.SetActive(true);
                _findHint.rectTransform.anchoredPosition = LocalIn(_fx, v.Find.rectTransform);
                _picked = -1;
                _wait = Wait.Find;
                _targetFind = v.Find.rectTransform;
                float until = GameClock.Time + FindTapWait;
                while (_picked < 0 && GameClock.Time < until) yield return null;
                _wait = Wait.None;
                _findHint.gameObject.SetActive(false);

                Sfx(BitacoraSounds.Save(), 0.45f);
                yield return StartCoroutine(FlyFindToSlot(v.Find, i));
                SetGlow(v, 0f);
            }
            yield return StartCoroutine(FlyProbe(BasePos()));
            _probe.gameObject.SetActive(false);
            SetCaption("Transmisión completa", "Ahora, un primer repaso");
            yield return StartCoroutine(Pause(1.2f));
        }

        private RectTransform _targetFind;

        /// <summary>Primer repaso (con respuesta al instante): lo que se aprende acá es la base de la retención.</summary>
        private IEnumerator Review()
        {
            yield return StartCoroutine(FadeLog(false));
            ShowDrawer();
            foreach (int i in ShuffledStops())
            {
                int p = _m.Route[i];
                var v = _planets[p];
                UpdateHud("Primer repaso");
                SetCaption($"¿Qué había en el planeta {PlanetName(p)}?", "Elige el hallazgo");
                yield return StartCoroutine(AskOption(v));
                int f = _picked;
                bool ok = f == _m.Finds[i];
                _learned[i] = ok;
                yield return StartCoroutine(PlaceOnPlanet(v, f));
                ShowMark(v, ok);
                if (ok)
                {
                    _streak++;
                    _bestStreak = Mathf.Max(_bestStreak, _streak);
                    _points += 40;
                    GameFeel.Correct(_streak);
                    StartCoroutine(UiFx.SparkBurst(_fx, LocalIn(_fx, v.Rect), GoodColor, 8, 110f, 22f));
                    SetCaption($"¡Sí! {Cap(BitacoraContract.FindNames[f])}", "");
                    yield return StartCoroutine(Pause(0.7f));
                }
                else
                {
                    _streak = 0;
                    GameFeel.Wrong();
                    yield return StartCoroutine(Pause(0.45f));
                    // Se muestra lo que era: el repaso también enseña.
                    v.Find.sprite = BitacoraSprites.Find(_m.Finds[i]);
                    StartCoroutine(PopIn(v.Find.rectTransform, 0.25f));
                    SetCaption($"Era {BitacoraContract.FindNames[_m.Finds[i]]}", "Imagínalo en ese planeta");
                    yield return StartCoroutine(Pause(1.4f));
                }
                _hud.SetStreak(_streak);
                v.Find.gameObject.SetActive(false);
                v.Mark.gameObject.SetActive(false);
                SetGlow(v, 0f);
            }
            HideDrawer();
            SetCaption("Bitácora guardada", BitacoraContract.Count(_learned) == _m.Items ? "Aprendiste la misión completa" : "");
            yield return StartCoroutine(Pause(1.2f));
        }

        /// <summary>Patrulla (solo al jugar suelto): una tarea liviana que ocupa la atención para que no se repase.</summary>
        private IEnumerator Patrol()
        {
            _toast.Show("Patrulla", "Atrapa cometas mientras la base procesa la bitácora", NeuroStyle.Sky, 2.2f);
            SetCaption("Patrulla", "Toca los cometas");
            foreach (var v in _planets) v.Body.color = new Color(1f, 1f, 1f, 0.35f);
            _patrolRoot.gameObject.SetActive(true);
            float end = GameClock.Time + BitacoraContract.PatrolSeconds;
            float nextComet = GameClock.Time + 0.6f;
            _wait = Wait.Comet;
            while (GameClock.Time < end)
            {
                UpdateHud($"Patrulla · {Mathf.CeilToInt(end - GameClock.Time)} s · {_caught} cometas");
                if (GameClock.Time >= nextComet)
                {
                    SpawnComet();
                    nextComet = GameClock.Time + 0.8f + 0.6f * (float)_rng.NextDouble();
                }
                yield return null;
            }
            _wait = Wait.None;
            foreach (var cm in _comets) cm.gameObject.SetActive(false);
            _patrolRoot.gameObject.SetActive(false);
            foreach (var v in _planets) v.Body.color = Color.white;
            SetCaption("Patrulla terminada", _caught > 0 ? $"{_caught} cometas atrapados" : "");
            yield return StartCoroutine(Pause(1f));
        }

        /// <summary>Informe: qué había en cada planeta, sin ayuda (la respuesta se ve al final).</summary>
        private IEnumerator Report()
        {
            if (_phaseMode == "recall")
            {
                int min = Mathf.Max(1, Mathf.RoundToInt(_config.config.memory_elapsed_s / 60f));
                _toast.Show("Informe de la bitácora", $"La transmisión llegó hace {min} {(min == 1 ? "minuto" : "minutos")}", NeuroStyle.Grape, 2.2f);
                yield return StartCoroutine(Pause(0.3f));
            }
            Sfx(BitacoraSounds.Report(), 0.5f);
            SetCaption("Informe", "¿Qué había en cada planeta? Sin ayuda: la verdad se ve al final");
            yield return StartCoroutine(Pause(1.6f));
            ShowDrawer();
            int n = 0;
            foreach (int i in ShuffledStops())
            {
                int p = _m.Route[i];
                var v = _planets[p];
                UpdateHud($"Informe · {++n} de {_m.Items}");
                SetCaption($"¿Qué había en el planeta {PlanetName(p)}?", "");
                yield return StartCoroutine(AskOption(v));
                _chosen[i] = _picked;
                _recalled[i] = _picked == _m.Finds[i];
                _dda.Register(_recalled[i]);
                Sfx(BitacoraSounds.Save(), 0.35f);
                yield return StartCoroutine(PlaceOnPlanet(v, _picked));
                v.Find.rectTransform.localScale = Vector3.one * 0.8f;
                SetGlow(v, 0f);
            }
            HideDrawer();
        }

        /// <summary>La ruta: tocar los planetas en el orden en que pasó la sonda (tocar el último de nuevo lo deshace).</summary>
        private IEnumerator RouteOrder()
        {
            SetCaption("La ruta", "Toca los planetas en el orden en que pasó la sonda");
            UpdateHud("La ruta");
            _routeTaps.Clear();
            while (_routeTaps.Count < _m.Items)
            {
                _picked = -1;
                _wait = Wait.Planet;
                while (_picked < 0) yield return null;
                _wait = Wait.None;
                int p = _picked;
                var v = _planets[p];
                int already = _routeTaps.IndexOf(p);
                if (already >= 0)
                {
                    if (already == _routeTaps.Count - 1)
                    {
                        _routeTaps.RemoveAt(already);
                        v.Badge.gameObject.SetActive(false);
                        PlayTone(523f, 0.05f, 0.04f);
                    }
                    continue;
                }
                _routeTaps.Add(p);
                v.Badge.gameObject.SetActive(true);
                v.BadgeText.text = _routeTaps.Count.ToString();
                SetBadge(v, NeuroStyle.Sun);
                StartCoroutine(PopIn(v.Badge, 0.2f));
                Sfx(BitacoraSounds.Catch(_routeTaps.Count - 1), 0.35f);
            }
            yield return StartCoroutine(Pause(0.5f));
        }

        /// <summary>La misión se revela: la sonda repite la ruta y cada planeta muestra lo que era; lo recordado se archiva.</summary>
        private IEnumerator Reveal()
        {
            SetCaption("Así fue la misión", "");
            // Los números que puso la persona se apagan: ahora se ven los de la ruta real (lima = acertó el orden).
            foreach (var pv in _planets) pv.Badge.gameObject.SetActive(false);
            _logRoot.gameObject.SetActive(true);
            foreach (var s in _slotFinds) s.gameObject.SetActive(false);
            _logLabel.text = "bitácora";
            yield return StartCoroutine(FadeLog(true));
            _probe.anchoredPosition = BasePos();
            _probe.gameObject.SetActive(true);
            StartCoroutine(PopIn(_probe, 0.2f));
            for (int i = 0; i < _m.Items; i++)
            {
                int p = _m.Route[i];
                var v = _planets[p];
                yield return StartCoroutine(FlyProbe(v.Rect.anchoredPosition + new Vector2(0f, PlanetSize * 0.62f)));
                bool orderOk = i < _routeTaps.Count && _routeTaps[i] == p;
                v.Badge.gameObject.SetActive(true);
                v.BadgeText.text = (i + 1).ToString();
                SetBadge(v, orderOk ? GoodColor : NeuroStyle.Cream);
                StartCoroutine(PopRect(v.Badge, 1.3f, 0.2f));
                if (_recalled[i])
                {
                    ShowMark(v, true);
                    v.Find.rectTransform.localScale = Vector3.one;
                    StartCoroutine(UiFx.SparkBurst(_fx, LocalIn(_fx, v.Find.rectTransform), NeuroStyle.Sun, 12, 130f, 26f));
                    Sfx(BitacoraSounds.Reveal(i), 0.45f);
                    _points += BitacoraContract.Points(true, _m.Level, i + 1);
                    yield return StartCoroutine(Pause(0.25f));
                    yield return StartCoroutine(FlyFindToSlot(v.Find, i));
                }
                else
                {
                    ShowMark(v, false);
                    v.Find.sprite = BitacoraSprites.Find(_m.Finds[i]);
                    v.Find.color = new Color(1f, 1f, 1f, 0.55f);
                    v.Find.rectTransform.localScale = Vector3.one;
                    v.Find.gameObject.SetActive(true);
                    StartCoroutine(PopIn(v.Find.rectTransform, 0.25f));
                    SetCaption($"En {PlanetName(p)} había {BitacoraContract.FindNames[_m.Finds[i]]}", "");
                    Sfx(BitacoraSounds.Reveal(i), 0.25f);
                    yield return StartCoroutine(Pause(0.9f));
                }
                _hud.SetPoints(_points);
            }
            yield return StartCoroutine(FlyProbe(BasePos()));
            _probe.gameObject.SetActive(false);
            int kept = BitacoraContract.Count(_recalled);
            if (kept > 0)
            {
                Sfx(BitacoraSounds.Archive(), 0.55f);
                _toast.Show(kept == _m.Items ? "¡Misión completa!" : "¡A tu bitácora!", kept == 1 ? "1 hallazgo archivado" : $"{kept} hallazgos archivados", NeuroStyle.Sun, 1.8f);
                GameFeel.LevelUp();
            }
            SetCaption(kept == _m.Items ? "Recordaste toda la misión" : $"Recordaste {kept} de {_m.Items}", "");
            yield return StartCoroutine(Pause(2f));
        }

        // ------------------------------------------------------------------ fin

        private void FinishEncode()
        {
            _done = true;
            int learned = BitacoraContract.Count(_learned);
            _exit.Show();
            _resultRoot.Find("Title").GetComponent<Text>().text = "Transmisión guardada";
            _resultRoot.Find("Score").GetComponent<Text>().text = $"{learned}/{_m.Items}";
            _resultRoot.Find("Detail").GetComponent<Text>().text = $"Aprendiste {learned} de {_m.Items} en el primer repaso";
            _resultRoot.Find("Extra").GetComponent<Text>().text = "El informe llega al final de tu sesión";
            _resultRoot.gameObject.SetActive(true);
            SendTelemetry(learned > 0 ? Mathf.RoundToInt(100f * learned / _m.Items) : 0, learned, -1f, -1);
        }

        private void FinishReport()
        {
            _done = true;
            int kept = BitacoraContract.Count(_recalled);
            int order = BitacoraContract.OrderCorrect(_m, _routeTaps);
            int score = BitacoraContract.Score((float)kept / _m.Items, (float)order / _m.Items, _m.Level);
            _exit.Show();
            _resultRoot.Find("Title").GetComponent<Text>().text = kept == _m.Items ? "¡Bitácora impecable!" : kept * 2 >= _m.Items ? "¡Buen informe!" : "Informe entregado";
            _resultRoot.Find("Detail").GetComponent<Text>().text = $"Recordaste {kept} de {_m.Items} en su lugar";
            _resultRoot.Find("Extra").GetComponent<Text>().text = $"La ruta: {order} de {_m.Items} en orden";
            _resultRoot.gameObject.SetActive(true);
            StartCoroutine(AnimateResult(score));
            SendTelemetry(score, kept, _dda.RatingNormalized, order);
        }

        private void SendTelemetry(int score, int correct, float endRating, int order)
        {
            bool report = _phaseMode != "encode";
            int delay = _phaseMode == "recall" ? _config.config.memory_elapsed_s : Mathf.RoundToInt(GameClock.Time - _startedAt);
            var metrics = new StroopSessionMetrics
            {
                correct_trials = correct,
                total_trials = _m.Items,
                calculated_score = score,
                average_response_time_ms = 0,
                level = _config.config.level,
                timed = _config.config.timed,
                end_rating = endRating >= 0f ? endRating : -1f,
                peak_level = report ? _dda.PeakLevel : _m.Level,
                mem_phase = _phaseMode,
                mem_seed = _m.Seed,
                mem_level = _m.Level,
                mem_items = _m.Items,
                mem_learned = _phaseMode == "recall" ? -1 : BitacoraContract.Count(_learned),
                mem_learned_mask = _phaseMode == "recall" ? -1 : BitacoraContract.Mask(_learned),
                mem_recalled = report ? BitacoraContract.Count(_recalled) : -1,
                mem_recalled_mask = report ? BitacoraContract.Mask(_recalled) : -1,
                mem_intrusions = report ? BitacoraContract.Intrusions(_m, _chosen) : -1,
                mem_order_ok = report ? order : -1,
                mem_delay_s = report ? delay : -1
            };
            var telemetry = new StroopTelemetry { user_id = _config.user_id, game_id = _config.game_id, session_metrics = metrics };
            NativeBridge.ForwardTelemetryToPlatform(JsonUtility.ToJson(telemetry));
        }

        // ------------------------------------------------------------------ entrada

        private void Update()
        {
            UpdateComets();
            if (_findHint != null && _findHint.gameObject.activeSelf)
                _findHint.rectTransform.localScale = Vector3.one * (1f + 0.08f * Mathf.Sin(GameClock.Time * 7f));
            if (_done || _wait == Wait.None || GameClock.DeltaTime <= 0f) return;
            if (!Input.GetMouseButtonDown(0)) return;
            Vector2 screen = Input.mousePosition;
            switch (_wait)
            {
                case Wait.Find:
                    if (_targetFind != null && Hit(_targetFind, screen, 120f)) _picked = 1;
                    break;
                case Wait.Option:
                    foreach (var o in _options)
                    {
                        if (!o.Rect.gameObject.activeSelf || !Hit(o.Rect, screen, OptionSize * 0.55f)) continue;
                        _picked = o.Find;
                        StartCoroutine(PopRect(o.Rect, 1.15f, 0.15f));
                        break;
                    }
                    break;
                case Wait.Planet:
                    for (int p = 0; p < _m.Planets; p++)
                        if (Hit(_planets[p].Rect, screen, PlanetSize * 0.6f)) { _picked = p; break; }
                    break;
                case Wait.Comet:
                    for (int k = 0; k < _comets.Count; k++)
                    {
                        var cm = _comets[k];
                        if (!cm.gameObject.activeSelf || !Hit(cm, screen, 90f)) continue;
                        cm.gameObject.SetActive(false);
                        _caught++;
                        _points += 10;
                        Sfx(BitacoraSounds.Catch(_caught), 0.4f);
                        StartCoroutine(UiFx.SparkBurst(_fx, LocalIn(_fx, cm), NeuroStyle.Sky, 8, 90f, 20f));
                        break;
                    }
                    break;
            }
        }

        private static bool Hit(RectTransform r, Vector2 screen, float radius) =>
            RectTransformUtility.ScreenPointToLocalPointInRectangle(r, screen, null, out var local) && local.magnitude <= radius;

        private IEnumerator AskOption(PlanetView v)
        {
            SetGlow(v, 0.6f);
            v.Ring.gameObject.SetActive(true);
            StartCoroutine(PopIn(v.Ring.rectTransform, 0.2f));
            _picked = -1;
            _wait = Wait.Option;
            while (_picked < 0) yield return null;
            _wait = Wait.None;
            v.Ring.gameObject.SetActive(false);
        }

        // ------------------------------------------------------------------ patrulla

        private void SpawnComet()
        {
            RectTransform cm = null;
            int idx = -1;
            for (int k = 0; k < _comets.Count; k++) if (!_comets[k].gameObject.activeSelf) { cm = _comets[k]; idx = k; break; }
            if (cm == null) return;
            bool fromLeft = _rng.Next(2) == 0;
            float y = (0.1f + 0.8f * (float)_rng.NextDouble() - 0.5f) * _mapH;
            float x = (fromLeft ? -0.5f : 0.5f) * _mapW + (fromLeft ? -80f : 80f);
            cm.anchoredPosition = new Vector2(x, y);
            float speed = _mapW / (2.2f + 0.8f * (float)_rng.NextDouble());
            _cometVel[idx] = new Vector2(fromLeft ? speed : -speed, (float)(_rng.NextDouble() - 0.5) * 120f);
            cm.localRotation = Quaternion.Euler(0f, 0f, fromLeft ? 180f : 0f);
            cm.gameObject.SetActive(true);
        }

        private void UpdateComets()
        {
            float dt = GameClock.DeltaTime;
            if (dt <= 0f) return;
            for (int k = 0; k < _comets.Count; k++)
            {
                var cm = _comets[k];
                if (!cm.gameObject.activeSelf) continue;
                cm.anchoredPosition += _cometVel[k] * dt;
                if (Mathf.Abs(cm.anchoredPosition.x) > _mapW * 0.5f + 120f) cm.gameObject.SetActive(false);
            }
        }

        // ------------------------------------------------------------------ animación

        private IEnumerator FlyProbe(Vector2 to)
        {
            Vector2 from = _probe.anchoredPosition;
            float dist = Vector2.Distance(from, to);
            if (dist < 4f) yield break;
            Sfx(BitacoraSounds.Travel(), 0.3f);
            float seconds = Mathf.Clamp(dist / 900f, 0.45f, 0.95f);
            // Arco suave: la sonda no va en línea recta, planea.
            Vector2 d = (to - from).normalized;
            Vector2 n = new Vector2(-d.y, d.x) * Mathf.Min(120f, dist * 0.25f) * (_rng.Next(2) == 0 ? 1f : -1f);
            float t = 0f, nextDot = 0f;
            while (t < seconds)
            {
                t += GameClock.DeltaTime;
                float k = UiFx.EaseOutCubic(Mathf.Clamp01(t / seconds));
                Vector2 pos = Vector2.Lerp(from, to, k) + n * Mathf.Sin(Mathf.PI * k);
                _probe.anchoredPosition = pos;
                if (t >= nextDot)
                {
                    DropTrail(pos);
                    nextDot = t + 0.045f;
                }
                yield return null;
            }
            _probe.anchoredPosition = to;
        }

        /// <summary>Estela de luz de la sonda: puntitos que se apagan despacio (la ruta se ve un rato y se desvanece).</summary>
        private void DropTrail(Vector2 pos)
        {
            RectTransform dot = null;
            foreach (var t in _trail) if (!t.gameObject.activeSelf) { dot = t; break; }
            if (dot == null) return;
            dot.anchoredPosition = pos;
            dot.gameObject.SetActive(true);
            StartCoroutine(FadeDot(dot));
        }

        private IEnumerator FadeDot(RectTransform dot)
        {
            var img = dot.GetComponent<Image>();
            float t = 0f;
            const float seconds = 1.6f;
            while (t < seconds)
            {
                t += GameClock.DeltaTime;
                float k = Mathf.Clamp01(t / seconds);
                img.color = NeuroStyle.WithAlpha(NeuroStyle.Cream, 0.85f * (1f - k));
                dot.localScale = Vector3.one * (1f - 0.5f * k);
                yield return null;
            }
            dot.gameObject.SetActive(false);
        }

        private void ClearTrail()
        {
            foreach (var t in _trail) t.gameObject.SetActive(false);
        }

        private IEnumerator FlyFindToSlot(Image find, int slot)
        {
            // Una copia vuela del planeta a su lugar en la bitácora (abajo); el original se apaga.
            var fly = NewImage(_fx, "Fly", find.sprite);
            fly.gameObject.SetActive(true);
            var r = fly.rectTransform;
            r.sizeDelta = Vector2.one * FindOnPlanet;
            Vector2 from = LocalIn(_fx, find.rectTransform);
            Vector2 to = LocalIn(_fx, _slotBgs[slot].rectTransform);
            find.gameObject.SetActive(false);
            float t = 0f;
            const float seconds = 0.5f;
            while (t < seconds)
            {
                t += GameClock.DeltaTime;
                float k = UiFx.EaseOutCubic(Mathf.Clamp01(t / seconds));
                r.anchoredPosition = Vector2.Lerp(from, to, k) + new Vector2(0f, 90f * Mathf.Sin(Mathf.PI * k));
                r.localScale = Vector3.one * Mathf.Lerp(1f, SlotSize * 0.85f / FindOnPlanet, k);
                yield return null;
            }
            Destroy(fly.gameObject);
            _slotFinds[slot].sprite = find.sprite;
            _slotFinds[slot].gameObject.SetActive(true);
            StartCoroutine(PopRect(_slotFinds[slot].rectTransform, 1.25f, 0.2f));
            StartCoroutine(UiFx.SparkBurst(_fx, to, NeuroStyle.Sun, 6, 70f, 16f));
        }

        private IEnumerator PlaceOnPlanet(PlanetView v, int find)
        {
            var opt = _options.Find(o => o.Find == find);
            v.Find.sprite = BitacoraSprites.Find(find);
            v.Find.color = Color.white;
            v.Find.rectTransform.anchoredPosition = new Vector2(0f, PlanetSize * 0.62f);
            if (opt == null)
            {
                v.Find.gameObject.SetActive(true);
                yield break;
            }
            var fly = NewImage(_fx, "Fly", v.Find.sprite);
            fly.gameObject.SetActive(true);
            var r = fly.rectTransform;
            r.sizeDelta = Vector2.one * OptionSize * 0.8f;
            Vector2 from = LocalIn(_fx, opt.Rect);
            Vector2 to = LocalIn(_fx, v.Find.rectTransform);
            float t = 0f;
            const float seconds = 0.35f;
            while (t < seconds)
            {
                t += GameClock.DeltaTime;
                float k = UiFx.EaseOutCubic(Mathf.Clamp01(t / seconds));
                r.anchoredPosition = Vector2.Lerp(from, to, k);
                yield return null;
            }
            Destroy(fly.gameObject);
            v.Find.rectTransform.localScale = Vector3.one;
            v.Find.gameObject.SetActive(true);
        }

        private IEnumerator FadeLog(bool show)
        {
            var group = _logRoot.GetComponent<CanvasGroup>();
            if (show) _logRoot.gameObject.SetActive(true);
            float t = 0f;
            const float seconds = 0.3f;
            while (t < seconds)
            {
                t += GameClock.DeltaTime;
                float k = Mathf.Clamp01(t / seconds);
                group.alpha = show ? k : 1f - k;
                yield return null;
            }
            group.alpha = show ? 1f : 0f;
            if (!show) _logRoot.gameObject.SetActive(false);
        }

        private IEnumerator PopInDelayed(RectTransform r, float delay)
        {
            r.localScale = Vector3.zero;
            yield return StartCoroutine(Pause(delay));
            yield return StartCoroutine(PopIn(r, 0.28f));
        }

        private static IEnumerator Pause(float seconds)
        {
            float t = 0f;
            while (t < seconds)
            {
                t += GameClock.DeltaTime;
                yield return null;
            }
        }

        // ------------------------------------------------------------------ ayudas de UI

        private IEnumerable<int> ShuffledStops()
        {
            var order = new List<int>();
            for (int i = 0; i < _m.Items; i++) order.Add(i);
            for (int i = order.Count - 1; i > 0; i--)
            {
                int j = _rng.Next(i + 1);
                (order[i], order[j]) = (order[j], order[i]);
            }
            return order;
        }

        private string PlanetName(int mapPlanet)
        {
            int c = _m.PlanetColors[mapPlanet];
            var col = TrafficSprites.Colors[c];
            return $"<color=#{ColorUtility.ToHtmlStringRGB(col)}>{BitacoraContract.PlanetNames[c]}</color>";
        }

        private static string Cap(string s) => string.IsNullOrEmpty(s) ? s : char.ToUpper(s[0]) + s.Substring(1);

        private void SetCaption(string main, string sub)
        {
            _caption.text = main;
            _sub.text = sub;
        }

        /// <summary>El segundo chip del marcador dice en qué parte va la misión; los puntos se muestran en la revelación.</summary>
        private void UpdateHud(string info)
        {
            if (!string.IsNullOrEmpty(info)) _hud.SetInfo(info);
        }

        private void SetGlow(PlanetView v, float alpha)
        {
            var c = v.Glow.color;
            v.Glow.color = new Color(c.r, c.g, c.b, alpha);
        }

        private void ShowMark(PlanetView v, bool ok)
        {
            v.Mark.sprite = ok ? AnswerMarkSprite.Check() : AnswerMarkSprite.Cross();
            v.Mark.gameObject.SetActive(true);
            StartCoroutine(PopIn(v.Mark.rectTransform, 0.2f));
        }

        private static void SetBadge(PlanetView v, Color c) => v.Badge.GetComponent<Image>().color = c;

        private void ShowDrawer()
        {
            _drawerRoot.gameObject.SetActive(true);
            int n = _m.Drawer.Length;
            int cols = n <= 8 ? 4 : 5;
            int rows = (n + cols - 1) / cols;
            float gap = OptionSize + 22f;
            for (int i = 0; i < _options.Count; i++)
            {
                var o = _options[i];
                bool on = i < n;
                o.Rect.gameObject.SetActive(on);
                if (!on) continue;
                o.Find = _m.Drawer[i];
                o.Img.sprite = BitacoraSprites.Find(o.Find);
                int row = i / cols, col = i % cols;
                int inRow = Mathf.Min(cols, n - row * cols);
                o.Rect.anchoredPosition = new Vector2((col - (inRow - 1) * 0.5f) * gap, ((rows - 1) * 0.5f - row) * gap);
                StartCoroutine(PopInDelayed(o.Rect, 0.03f * i));
            }
        }

        private void HideDrawer() => _drawerRoot.gameObject.SetActive(false);

        private void Sfx(AudioClip clip, float volume)
        {
            if (GameFeel.SoundOn && (_config == null || _config.config == null || _config.config.sound_enabled))
                _audioSource.PlayOneShot(clip, volume);
        }

        private Vector2 MapPos(float x, float y) => new Vector2((x - 0.5f) * _mapW, (0.5f - y) * _mapH);

        /// <summary>De dónde sale y a dónde vuelve la sonda: abajo al centro del mapa.</summary>
        private Vector2 BasePos() => new Vector2(0f, -_mapH * 0.5f - 30f);

        // ------------------------------------------------------------------ construcción de UI

        protected override void BuildUi()
        {
            if (FindObjectOfType<UnityEngine.EventSystems.EventSystem>() == null)
            {
                var es = new GameObject("EventSystem");
                es.AddComponent<UnityEngine.EventSystems.EventSystem>();
                es.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
            }

            var canvasGo = new GameObject("BitacoraCanvas");
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
            WorldBackdrop.Build(bgRect, GameWorld.Logbook);

            var safeGo = new GameObject("SafeAreaContent");
            safeGo.transform.SetParent(canvasGo.transform, false);
            _safe = safeGo.AddComponent<RectTransform>();
            ApplySafeArea(_safe);

            _hud = new GameHud(_safe, "Bitácora de Misión", MarginU + 20f, this);

            var playGo = new GameObject("Play");
            playGo.transform.SetParent(_safe, false);
            _play = playGo.AddComponent<RectTransform>();
            Stretch(_play);

            _map = Layer(_play, "Map");
            _map.anchorMin = _map.anchorMax = new Vector2(0.5f, 0.5f);
            _trailRoot = Layer(_map, "Trail");
            for (int i = 0; i < 90; i++)
            {
                var dot = NewImage(_trailRoot, "Trail", DiscSprite.Get());
                dot.rectTransform.sizeDelta = new Vector2(14f, 14f);
                _trail.Add(dot.rectTransform);
            }
            var planetRoot = Layer(_map, "Planets");
            for (int p = 0; p < BitacoraContract.PlanetCount; p++) _planets.Add(BuildPlanet(planetRoot));
            _patrolRoot = Layer(_map, "Patrol");
            for (int k = 0; k < 6; k++)
            {
                var cm = NewImage(_patrolRoot, "Comet", SymbolSprite.Get(ShapeKind.Comet, k % 2));
                cm.rectTransform.sizeDelta = new Vector2(110f, 110f);
                _comets.Add(cm.rectTransform);
                _cometVel.Add(Vector2.zero);
            }
            _patrolRoot.gameObject.SetActive(false);
            var probe = NewImage(_map, "Probe", BitacoraSprites.Probe());
            probe.rectTransform.sizeDelta = new Vector2(ProbeSize, ProbeSize);
            _probe = probe.rectTransform;

            _caption = MakeText(_play, "Caption", 50, TextAnchor.MiddleCenter, Color.white, 0f, 0f);
            NeuroStyle.ClayText(_caption, 3.5f, 5f);
            _caption.supportRichText = true;
            _caption.rectTransform.anchorMin = _caption.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            BestFit(_caption, 32);
            _sub = MakeText(_play, "Sub", 34, TextAnchor.MiddleCenter, NeuroStyle.WithAlpha(NeuroStyle.Cream, 0.8f), 0f, 0f);
            _sub.rectTransform.anchorMin = _sub.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            BestFit(_sub, 24);

            // La bitácora: una fila de huecos donde se guardan los hallazgos (rótulo suelto, sin recuadro).
            _logRoot = Layer(_play, "Log");
            _logRoot.anchorMin = _logRoot.anchorMax = new Vector2(0.5f, 0.5f);
            _logRoot.gameObject.AddComponent<CanvasGroup>();
            _logLabel = MakeText(_logRoot, "LogLabel", 30, TextAnchor.MiddleCenter, NeuroStyle.WithAlpha(NeuroStyle.Cream, 0.7f), 0f, 0f);
            _logLabel.rectTransform.anchorMin = _logLabel.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            _logLabel.rectTransform.sizeDelta = new Vector2(400f, 44f);
            _logLabel.rectTransform.anchoredPosition = new Vector2(0f, SlotSize * 0.5f + 34f);
            _logLabel.text = "bitácora";
            _logLabel.gameObject.SetActive(true);
            for (int i = 0; i < 8; i++)
            {
                var slot = NewImage(_logRoot, "Slot", DiscSprite.Get());
                slot.rectTransform.sizeDelta = new Vector2(SlotSize, SlotSize);
                slot.color = Deep;
                var ring = NewImage(slot.rectTransform, "Ring", RingSprite.Get());
                Stretch(ring.rectTransform);
                ring.color = NeuroStyle.WithAlpha(NeuroStyle.Cream, 0.25f);
                ring.gameObject.SetActive(true);
                var f = NewImage(slot.rectTransform, "Find", null);
                f.rectTransform.sizeDelta = new Vector2(SlotSize * 0.85f, SlotSize * 0.85f);
                _slotBgs.Add(slot);
                _slotFinds.Add(f);
            }

            // Cajón de hallazgos para responder.
            _drawerRoot = Layer(_play, "Drawer");
            _drawerRoot.anchorMin = _drawerRoot.anchorMax = new Vector2(0.5f, 0.5f);
            for (int i = 0; i < 13; i++)
            {
                var go = new GameObject("Option");
                go.transform.SetParent(_drawerRoot, false);
                var r = go.AddComponent<RectTransform>();
                r.anchorMin = r.anchorMax = new Vector2(0.5f, 0.5f);
                r.sizeDelta = new Vector2(OptionSize, OptionSize);
                var b = NewImage(r, "Bg", DiscSprite.Get());
                Stretch(b.rectTransform);
                b.color = Deep;
                b.gameObject.SetActive(true);
                var ring = NewImage(r, "Ring", RingSprite.Get());
                Stretch(ring.rectTransform);
                ring.color = NeuroStyle.WithAlpha(NeuroStyle.Sky, 0.45f);
                ring.gameObject.SetActive(true);
                var img = NewImage(r, "Img", null);
                img.rectTransform.sizeDelta = new Vector2(OptionSize * 0.8f, OptionSize * 0.8f);
                img.gameObject.SetActive(true);
                go.SetActive(false);
                _options.Add(new OptionView { Rect = r, Bg = b, Img = img });
            }
            _drawerRoot.gameObject.SetActive(false);

            _fx = Layer(_play, "Fx");
            _findHint = NewImage(_fx, "Hint", RingSprite.Get());
            _findHint.rectTransform.sizeDelta = new Vector2(170f, 170f);
            _findHint.color = NeuroStyle.WithAlpha(NeuroStyle.Sun, 0.9f);

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

        private PlanetView BuildPlanet(Transform parent)
        {
            var go = new GameObject("Planet");
            go.transform.SetParent(parent, false);
            var r = go.AddComponent<RectTransform>();
            r.anchorMin = r.anchorMax = new Vector2(0.5f, 0.5f);
            r.sizeDelta = new Vector2(PlanetSize, PlanetSize);
            var v = new PlanetView { Rect = r };
            v.Glow = NewImage(r, "Glow", RadialGlowSprite.Get());
            v.Glow.rectTransform.sizeDelta = Vector2.one * PlanetSize * 2.6f;
            v.Glow.gameObject.SetActive(true);
            v.Ring = NewImage(r, "Ask", RingSprite.Get());
            v.Ring.rectTransform.sizeDelta = Vector2.one * PlanetSize * 1.45f;
            v.Ring.color = NeuroStyle.Sun;
            v.Body = NewImage(r, "Body", null);
            Stretch(v.Body.rectTransform);
            v.Body.gameObject.SetActive(true);
            v.Label = MakeText(r, "Name", 30, TextAnchor.MiddleCenter, NeuroStyle.WithAlpha(NeuroStyle.Cream, 0.85f), 0f, 0f);
            v.Label.rectTransform.anchorMin = v.Label.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            v.Label.rectTransform.sizeDelta = new Vector2(220f, 40f);
            v.Label.rectTransform.anchoredPosition = new Vector2(0f, -PlanetSize * 0.62f);
            v.Label.gameObject.SetActive(true);
            v.Find = NewImage(r, "Find", null);
            v.Find.rectTransform.sizeDelta = new Vector2(FindOnPlanet, FindOnPlanet);
            v.Mark = NewImage(r, "Mark", null);
            v.Mark.rectTransform.sizeDelta = new Vector2(62f, 62f);
            v.Mark.rectTransform.anchoredPosition = new Vector2(PlanetSize * 0.52f, PlanetSize * 0.52f);
            var badge = NewImage(r, "Badge", DiscSprite.Get());
            badge.rectTransform.sizeDelta = new Vector2(58f, 58f);
            badge.rectTransform.anchoredPosition = new Vector2(PlanetSize * 0.5f, -PlanetSize * 0.32f);
            v.Badge = badge.rectTransform;
            var ink = NewImage(badge.rectTransform, "Ink", RingSprite.Get());
            Stretch(ink.rectTransform);
            ink.color = NeuroStyle.Ink;
            ink.gameObject.SetActive(true);
            v.BadgeText = MakeText(badge.rectTransform, "N", 36, TextAnchor.MiddleCenter, NeuroStyle.Ink, 0f, 0f);
            Stretch(v.BadgeText.rectTransform);
            v.BadgeText.gameObject.SetActive(true);
            return v;
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
            AddResultText("Score", 220, new Vector2(0f, 60f), Color.white);
            AddResultText("Detail", 46, new Vector2(0f, -150f), new Color(1f, 1f, 1f, 0.85f));
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
            float w = _play.rect.width, h = _play.rect.height;
            float top = h * 0.5f - (GameHud.Height + 40f);
            _mapW = w - MarginU * 2f - PlanetSize * 0.6f;
            _mapH = Mathf.Min(h * 0.42f, _mapW * 0.95f);
            _map.sizeDelta = new Vector2(_mapW, _mapH);
            float mapCenter = top - 60f - _mapH * 0.5f;
            _map.anchoredPosition = new Vector2(0f, mapCenter);
            float below = mapCenter - _mapH * 0.5f - 150f;
            _caption.rectTransform.sizeDelta = new Vector2(w - 2f * MarginU, 80f);
            _caption.rectTransform.anchoredPosition = new Vector2(0f, below);
            _sub.rectTransform.sizeDelta = new Vector2(w - 2f * MarginU, 56f);
            _sub.rectTransform.anchoredPosition = new Vector2(0f, below - 62f);
            float rest = below - 62f - 40f;              // de acá hacia abajo: bitácora o cajón
            float bottom = -h * 0.5f + 40f;
            float mid = (rest + bottom) * 0.5f;
            _logRoot.anchoredPosition = new Vector2(0f, mid);
            _drawerRoot.anchoredPosition = new Vector2(0f, mid);
        }

        private void LayoutSlots()
        {
            int n = _m.Items;
            float gap = SlotSize + 16f;
            for (int i = 0; i < _slotBgs.Count; i++)
                _slotBgs[i].rectTransform.anchoredPosition = new Vector2((i - (n - 1) * 0.5f) * gap, 0f);
            _logLabel.text = "bitácora";
            _logRoot.GetComponent<CanvasGroup>().alpha = 1f;
        }
    }
}
