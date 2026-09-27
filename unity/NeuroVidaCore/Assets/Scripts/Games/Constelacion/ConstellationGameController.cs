using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using NeuroVida.Bridge;
using NeuroVida.Contracts;
using NeuroVida.Games.Secuencia; // RoundedRectSprite / RadialGlowSprite / RingSprite / DiscSprite
using NeuroVida.Games.Shared;
using NeuroVida.Games.Trafico;   // RailLine
using static NeuroVida.Games.Shared.UiKit;

namespace NeuroVida.Games.Constelacion
{
    /// <summary>
    /// "Constelación de Palabras": juego estrella de lenguaje, FLUIDEZ VERBAL (ver <see cref="FluencyContract"/>). Dos
    /// rondas de un minuto: una categoría ("Animales") y una letra ("Palabras con P"). Se toca el micrófono y se dicen
    /// palabras en voz alta; cada una nace como una estrella en el cielo MIENTRAS se habla (resultados parciales del
    /// reconocedor; se confirman al terminar la frase). Las palabras seguidas del mismo grupo se unen con líneas y forman
    /// una constelación con su nombre ("del mar", "felinos"); un salto a otro grupo empieza otra en otro lugar del cielo.
    /// <para>Sonido: cada palabra que sigue una constelación suena una nota más arriba; al saltar, vuelve a la nota de
    /// partida (se oye cómo se agrupa y cómo se salta). Lo repetido no castiga: la estrella que ya estaba late y suena un
    /// toque suave. Lo que el juego no reconoce aparece un momento en gris y no suma.</para>
    /// <para>Sin voz (sin reconocedor o sin permiso de micrófono) o si la persona lo prefiere: teclado.</para>
    /// Medidas (Troyer et al., 1997): palabras, tamaño medio de las constelaciones, saltos y ritmo por cuartos; la app
    /// las lee al final.
    /// </summary>
    public class ConstellationGameController : GameControllerBase
    {
        public const string GameId = FluencyContract.GameId;

        private const float UnitsPerDp = 3f;
        private const float MarginU = 60f;
        private const float StarSize = 70f;
        private const float MicSize = 200f;
        /// <summary>Después de "¡tiempo!", cuánto se espera la última frase del reconocedor.</summary>
        private const float CloseGrace = 1.6f;

        private enum Phase { Idle, Intro, Listening, Closing, Reveal, Done }

        private sealed class StarView
        {
            public FluencyWord Word;
            public Vector2 Pos;
            public RectTransform Rect;
            public Image Img;
            public Text Label;
            public bool Live;
        }

        private sealed class ClusterView
        {
            public RailLine Glow, Line;
            public Text Name;
        }

        // partida
        private string[] _plan;
        private int _round;
        private FluencyCategory _cat;
        private Phase _phase = Phase.Idle;
        private bool _keyboard, _speechOk, _pausedSpeech, _startPressed;
        private float _roundStart, _endsAt;
        private int _lastTickSecond = -1;
        private int _points, _pointsBefore;
        private bool _offline;

        // ronda
        private readonly List<FluencyWord> _said = new List<FluencyWord>();
        private readonly List<StarView> _visible = new List<StarView>();     // estrellas en orden (confirmadas y en vivo)
        private readonly List<StarView> _live = new List<StarView>();
        private List<FluencyWord> _liveWords = new List<FluencyWord>();
        private readonly List<(string word, float time)> _tokenTimes = new List<(string, float)>();
        private int _lastClusterCount;

        // resultados por ronda
        private readonly List<string> _roundIds = new List<string>();
        private readonly List<int> _valid = new List<int>();
        private readonly List<FluencyKind> _kinds = new List<FluencyKind>();
        private readonly List<int> _repeats = new List<int>();
        private readonly List<int> _unknowns = new List<int>();
        private readonly List<float> _meanClusters = new List<float>();
        private readonly List<int> _switches = new List<int>();
        private readonly List<int> _quarters = new List<int>();
        private readonly List<string> _tops = new List<string>();
        private readonly List<string> _words = new List<string>();
        private int _roundUnknown;

        // UI
        private RectTransform _safe, _play, _sky, _fxRect, _micRect, _micRings, _keyboardRoot, _addButton, _writeButton, _timerBg, _timerFill;
        private Image _mic;
        private Text _roundLabel, _title, _prompt, _caption, _hint, _summary;
        private InputField _input;
        private readonly List<ClusterView> _clusterViews = new List<ClusterView>();
        private Toast _toast;
        private ExitButton _exit;
        private GameHud _hud;
        private CountdownScreen _countdown;
        private float _playW, _playH, _skyTop, _skyBottom;
        private System.Random _rng;

        // ------------------------------------------------------------------ sesión

        public void StartSession(SequenceInitConfig config)
        {
            _config = config;
            _rng = new System.Random();
            _plan = FluencyContract.Plan(_rng.Next());
            _round = 0;
            _points = 0;
            _phase = Phase.Idle;
            _roundIds.Clear(); _valid.Clear(); _kinds.Clear(); _repeats.Clear(); _unknowns.Clear();
            _meanClusters.Clear(); _switches.Clear(); _quarters.Clear(); _tops.Clear(); _words.Clear();
            _speechOk = SpeechClient.Available;
            _keyboard = !_speechOk;

            _resultRoot.gameObject.SetActive(false);
            _exit.Hide();
            _hud.SetStreak(0);

            StopAllCoroutines();
            StartCoroutine(GameLoop());
        }

        private void OnDisable()
        {
            SpeechClient.Stop();
        }

        private IEnumerator GameLoop()
        {
            _safe.gameObject.SetActive(false);
            yield return StartCoroutine(_countdown.Play("Constelación de Palabras", Assessment.Subtitle("Prepárate"), () => _safe.gameObject.SetActive(true)));
            _safe.gameObject.SetActive(true);
            yield return null;
            ApplySafeArea(_safe);
            Canvas.ForceUpdateCanvases();
            Layout();
            UpdateHud();

            for (_round = 0; _round < _plan.Length; _round++)
                yield return StartCoroutine(RunRound());
            yield return StartCoroutine(FinishGame());
        }

        // ------------------------------------------------------------------ una ronda

        private IEnumerator RunRound()
        {
            _cat = FluencyContract.Get(_plan[_round]);
            ClearSky();
            _pointsBefore = _points;
            _roundUnknown = 0;
            _roundLabel.text = $"Ronda {_round + 1} de {_plan.Length}";
            _title.text = _cat.Title;
            _prompt.text = _cat.Prompt;
            StartCoroutine(PopRect(_title.rectTransform, 1.15f, 0.3f));
            _summary.gameObject.SetActive(false);
            SetTimerFraction(1f);

            // Intro: se empieza cuando la persona toca (el micrófono o, con teclado, "Empezar").
            _phase = Phase.Intro;
            _startPressed = false;
            ShowInputMode();
            SetHint(_keyboard ? "Toca Empezar y escribe todas las que puedas en un minuto"
                              : "Toca el micrófono y di en voz alta todas las que puedas");
            if (_round == 0) _toast.Show("Un minuto", "No es un examen: di lo que se te ocurra", NeuroStyle.Sky, 2.2f);
            while (!_startPressed) yield return null;

            // Voz: pedir permiso la primera vez; si no hay, teclado.
            if (!_keyboard && !SpeechClient.HasPermission)
            {
                bool? granted = null;
                SpeechClient.RequestPermission(g => granted = g);
                float waited = 0f;
                while (granted == null && waited < 30f)
                {
                    waited += Time.unscaledDeltaTime; // el diálogo del sistema no pausa el juego
                    yield return null;
                }
                if (granted != true)
                {
                    _keyboard = true;
                    ShowInputMode();
                    _toast.Show("Sin micrófono", "Puedes escribir las palabras", NeuroStyle.Sun, 2f);
                }
            }

            // ¡A hablar! Un minuto.
            _phase = Phase.Listening;
            SetHint("");
            _tokenTimes.Clear();
            _liveWords = new List<FluencyWord>();
            _roundStart = GameClock.Time;
            _endsAt = _roundStart + FluencyContract.RoundSeconds;
            _lastTickSecond = -1;
            Sfx(ConstellationSounds.Listen(), 0.45f);
            if (!_keyboard)
            {
                SpeechClient.Start(offline: true);
                _offline = SpeechClient.Offline;
            }
            else FocusInput();
            while (GameClock.Time < _endsAt) yield return null;

            // ¡Tiempo! Se espera la última frase y se confirma lo que quedó a medio decir.
            _phase = Phase.Closing;
            if (!_keyboard) SpeechClient.Stop();
            Sfx(ConstellationSounds.Stop(), 0.4f);
            SetCaption("");
            SetHint("¡Tiempo!");
            float grace = 0f;
            while (grace < CloseGrace && !_keyboard)
            {
                grace += GameClock.DeltaTime;
                PumpSpeech();
                yield return null;
            }
            if (_liveWords.Count > 0) Commit(new List<string>());
            if (_keyboard && _input != null && !string.IsNullOrWhiteSpace(_input.text)) SubmitTyped();

            yield return StartCoroutine(RevealRound());
        }

        private IEnumerator RevealRound()
        {
            _phase = Phase.Reveal;
            HideInputs();
            var valid = ValidWords();
            var clusters = FluencyContract.Clusters(valid);
            int n = valid.Count;
            int constellations = 0, biggest = 0;
            string bigName = null;
            foreach (var c in clusters)
            {
                if (c.Count < 2) continue;
                constellations++;
                if (c.Count > biggest) { biggest = c.Count; bigName = c.Group; }
            }
            int switches = FluencyContract.Switches(clusters);
            Sfx(ConstellationSounds.RoundEnd(), 0.5f);
            SetHint("");
            _summary.gameObject.SetActive(true);
            _summary.text = n == 0 ? "Esta vez el cielo quedó vacío" :
                $"{n} {(n == 1 ? "estrella" : "estrellas")} · {constellations} {(constellations == 1 ? "constelación" : "constelaciones")} · {switches} {(switches == 1 ? "salto" : "saltos")}";
            StartCoroutine(PopRect(_summary.rectTransform, 1.12f, 0.3f));
            // Las estrellas titilan una vez, en orden.
            for (int i = 0; i < _visible.Count; i++)
            {
                StartCoroutine(PopRect(_visible[i].Rect, 1.3f, 0.25f));
                if (i % 3 == 0) yield return StartCoroutine(Wait(0.04f));
            }

            // Resultados de la ronda.
            _roundIds.Add(_cat.Id);
            _valid.Add(n);
            _kinds.Add(_cat.Kind);
            int rep = 0;
            foreach (var w in _said) if (w.Repeat) rep++;
            _repeats.Add(rep);
            _unknowns.Add(_roundUnknown);
            _meanClusters.Add(FluencyContract.MeanClusterSize(clusters));
            _switches.Add(switches);
            _quarters.AddRange(FluencyContract.Quarters(_said));
            var tops = new List<string>();
            foreach (var c in clusters) if (c.Count >= 2 && c.Group != null) tops.Add(c.Group + ":" + c.Count);
            _tops.Add(string.Join(";", tops));
            var names = new List<string>();
            foreach (var w in valid) names.Add(w.Canonical ?? w.Display);
            _words.Add(string.Join("|", names));
            if (bigName != null && biggest >= 3)
                _toast.Show("¡Tu constelación más grande!", $"{bigName}: {biggest} estrellas", NeuroStyle.Sun, 1.8f);

            yield return StartCoroutine(Wait(3.6f));
        }

        // ------------------------------------------------------------------ voz y teclado

        private void Update()
        {
            if (_phase == Phase.Idle || _phase == Phase.Done) return;
            UpdateTimer();
            UpdateMicPulse();

            // Pausa (Atrás): se deja de escuchar y se retoma al volver.
            if (!_keyboard && _phase == Phase.Listening)
            {
                if (GameClock.Paused && !_pausedSpeech) { SpeechClient.Stop(); _pausedSpeech = true; }
                else if (!GameClock.Paused && _pausedSpeech) { SpeechClient.Start(offline: true); _pausedSpeech = false; }
            }
            if (GameClock.DeltaTime <= 0f) return;
            if (!_keyboard && _phase == Phase.Listening) PumpSpeech();
            HandleTaps();
        }

        private void PumpSpeech()
        {
            foreach (var e in SpeechClient.Poll())
            {
                switch (e.type)
                {
                    case "partial": ApplyUtterance(e.text, null, false); break;
                    case "final": ApplyUtterance(e.text, e.alts, true); break;
                    case "error":
                        if (e.code == 9) // sin permiso
                        {
                            _keyboard = true;
                            ShowInputMode();
                            FocusInput();
                            _toast.Show("Sin micrófono", "Sigue escribiendo las palabras", NeuroStyle.Sun, 2f);
                        }
                        break;
                }
            }
        }

        /// <summary>
        /// Una frase (a medio decir o terminada): se leen sus palabras, cada una con la hora en que APARECIÓ en los
        /// parciales (el reconocedor junta varias palabras en una frase; si no, el ritmo saldría amontonado al final de
        /// cada pausa), y las estrellas de la frase se concilian con las que ya estaban en vivo.
        /// </summary>
        private void ApplyUtterance(string text, string[] alts, bool final)
        {
            float now = GameClock.Time - _roundStart;
            var toks = FluencyContract.Tokenize(text);
            int keep = 0;
            while (keep < toks.Count && keep < _tokenTimes.Count && _tokenTimes[keep].word == toks[keep].word) keep++;
            if (keep < _tokenTimes.Count) _tokenTimes.RemoveRange(keep, _tokenTimes.Count - keep);
            for (int i = keep; i < toks.Count; i++) _tokenTimes.Add((toks[i].word, Mathf.Min(now, FluencyContract.RoundSeconds)));

            var words = FluencyContract.Read(_cat, _said, text, final ? alts : null, out var unknown);
            foreach (var w in words)
                w.Time = w.TokenIndex < _tokenTimes.Count ? _tokenTimes[w.TokenIndex].time : Mathf.Min(now, FluencyContract.RoundSeconds);

            // Conciliar: se conservan las estrellas en vivo que siguen iguales; el resto se rehace.
            int same = 0;
            while (same < words.Count && same < _liveWords.Count && words[same].Key == _liveWords[same].Key && words[same].Repeat == _liveWords[same].Repeat) same++;
            for (int i = _live.Count - 1; i >= 0; i--)
            {
                int wi = _liveWords.IndexOf(_live[i].Word);
                if (wi >= same) RemoveStar(_live[i]);
            }
            for (int i = 0; i < same; i++) words[i] = _liveWords[i];
            for (int i = same; i < words.Count; i++)
            {
                if (words[i].Repeat) PulseRepeat(words[i]);
                else AddStar(words[i], live: true);
            }
            _liveWords = words;
            RefreshClusters();
            SetCaption(final ? "" : text);
            if (final) Commit(unknown);
        }

        /// <summary>La frase terminó: sus estrellas quedan fijas en el cielo.</summary>
        private void Commit(List<string> unknown)
        {
            foreach (var w in _liveWords) _said.Add(w);
            foreach (var s in _live)
            {
                s.Live = false;
                s.Img.color = Color.white;
                s.Label.color = NeuroStyle.Cream;
                StartCoroutine(PopRect(s.Rect, 1.2f, 0.18f));
            }
            _live.Clear();
            _liveWords = new List<FluencyWord>();
            _tokenTimes.Clear();
            _points = _pointsBefore + RoundPoints();
            UpdateHud();
            // Lo no reconocido: aparece un momento en gris, sin castigo (puede faltar en la lista).
            int shown = 0;
            foreach (var u in unknown)
            {
                _roundUnknown++;
                if (shown++ < 2) StartCoroutine(FloatText(new Vector2((shown - 1.5f) * 260f, _caption.rectTransform.anchoredPosition.y + 40f), u + "?", new Color(1f, 1f, 1f, 0.45f), 38));
            }
        }

        /// <summary>
        /// Puntos de la ronda (solo lo confirmado): 10 por palabra; formar una constelación, 15; cada palabra más que la
        /// sigue, 5 por su lugar (la tercera 10, la cuarta 15...): agrupar se premia, y saltar también suma palabras.
        /// </summary>
        private int RoundPoints()
        {
            var committed = new List<FluencyWord>();
            foreach (var s in _visible) if (!s.Live) committed.Add(s.Word);
            int pts = 10 * committed.Count;
            foreach (var c in FluencyContract.Clusters(committed))
                for (int step = 1; step < c.Count; step++) pts += step == 1 ? 15 : 5 * step;
            return pts;
        }

        private void SubmitTyped()
        {
            if (_input == null) return;
            string text = _input.text;
            _input.text = "";
            if (string.IsNullOrWhiteSpace(text)) return;
            _tokenTimes.Clear();
            ApplyUtterance(text, null, true);
            if (_phase == Phase.Listening) FocusInput();
        }

        private void HandleTaps()
        {
            if (!Input.GetMouseButtonDown(0)) return;
            Vector2 screen = Input.mousePosition;
            if (_phase == Phase.Intro)
            {
                if (!_keyboard && _micRect.gameObject.activeSelf && Hit(_micRect, screen, 1.3f)) _startPressed = true;
                else if (_keyboard && _addButton.gameObject.activeSelf && Hit(_addButton, screen, 1.1f)) _startPressed = true;
                else if (_writeButton.gameObject.activeSelf && Hit(_writeButton, screen, 1.1f))
                {
                    _keyboard = !_keyboard;
                    if (!_speechOk) _keyboard = true;
                    ShowInputMode();
                    SetHint(_keyboard ? "Toca Empezar y escribe todas las que puedas en un minuto"
                                      : "Toca el micrófono y di en voz alta todas las que puedas");
                }
            }
            else if (_phase == Phase.Listening && _keyboard && _addButton.gameObject.activeSelf && Hit(_addButton, screen, 1.1f))
            {
                SubmitTyped();
            }
        }

        private static bool Hit(RectTransform r, Vector2 screen, float scale)
        {
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(r, screen, null, out var local)) return false;
            var size = r.rect.size * 0.5f * scale;
            return Mathf.Abs(local.x) <= size.x && Mathf.Abs(local.y) <= size.y;
        }

        // ------------------------------------------------------------------ estrellas y constelaciones

        private List<FluencyWord> ValidWords()
        {
            var list = new List<FluencyWord>();
            foreach (var s in _visible) list.Add(s.Word);
            return list;
        }

        private void AddStar(FluencyWord w, bool live)
        {
            // Dónde nace: junto a la última estrella si sigue su constelación; si no, en un lugar libre del cielo.
            var words = ValidWords();
            words.Add(w);
            var clusters = FluencyContract.Clusters(words);
            var last = clusters[clusters.Count - 1];
            int step = last.Count - 1;
            Vector2 pos = step > 0 ? PlaceNear(_visible[_visible.Count - 1].Pos) : PlaceFree();

            var view = new StarView { Word = w, Pos = pos, Live = live };
            var go = new GameObject("Star");
            go.transform.SetParent(_sky, false);
            view.Rect = go.AddComponent<RectTransform>();
            view.Rect.anchorMin = view.Rect.anchorMax = new Vector2(0.5f, 0.5f);
            view.Rect.sizeDelta = new Vector2(StarSize, StarSize);
            view.Rect.anchoredPosition = pos;
            view.Img = go.AddComponent<Image>();
            view.Img.sprite = ConstellationSprites.Neutral();
            view.Img.raycastTarget = false;
            view.Img.color = new Color(1f, 1f, 1f, live ? 0.7f : 1f);
            view.Label = MakeText(view.Rect, "Label", 34, TextAnchor.UpperCenter, live ? new Color(1f, 1f, 1f, 0.7f) : NeuroStyle.Cream, 0f, 0f);
            NeuroStyle.ClayText(view.Label, 2.5f, 3f);
            var lr = view.Label.rectTransform;
            lr.anchorMin = lr.anchorMax = new Vector2(0.5f, 0f);
            lr.pivot = new Vector2(0.5f, 1f);
            lr.sizeDelta = new Vector2(300f, 50f);
            lr.anchoredPosition = new Vector2(0f, -2f);
            view.Label.text = w.Display;
            view.Label.horizontalOverflow = HorizontalWrapMode.Overflow;

            _visible.Add(view);
            if (live) _live.Add(view);
            StartCoroutine(PopIn(view.Rect, 0.25f));
            StartCoroutine(UiFx.SparkBurst(_fxRect, pos, NeuroStyle.Cream, 6, 70f, 14f, 0.35f));
            Sfx(ConstellationSounds.Star(step), 0.4f);
            if (step == 1) Sfx(ConstellationSounds.Formed(), 0.35f);
        }

        private void RemoveStar(StarView s)
        {
            _visible.Remove(s);
            _live.Remove(s);
            if (s.Rect != null) Destroy(s.Rect.gameObject);
        }

        private void PulseRepeat(FluencyWord w)
        {
            Sfx(ConstellationSounds.Repeat(), 0.35f);
            foreach (var s in _visible)
                if (s.Word.Key == w.Key)
                {
                    StartCoroutine(PopRect(s.Rect, 1.35f, 0.25f));
                    break;
                }
        }

        /// <summary>Colores, líneas y nombres de las constelaciones, rehechos desde las estrellas (en su orden).</summary>
        private void RefreshClusters()
        {
            var clusters = FluencyContract.Clusters(ValidWords());
            int ordinal = 0;
            foreach (var c in clusters)
            {
                if (c.Count < 2)
                {
                    _visible[c.Start].Img.sprite = ConstellationSprites.Neutral();
                    continue;
                }
                var view = ClusterViewAt(ordinal);
                var color = ConstellationSprites.Colors[ordinal % ConstellationSprites.Colors.Length];
                var pts = new List<Vector2>();
                for (int i = c.Start; i < c.Start + c.Count; i++)
                {
                    _visible[i].Img.sprite = ConstellationSprites.Star(ordinal);
                    pts.Add(_visible[i].Pos);
                }
                view.Glow.gameObject.SetActive(true);
                view.Line.gameObject.SetActive(true);
                view.Glow.color = NeuroStyle.WithAlpha(color, 0.28f);
                view.Line.color = NeuroStyle.WithAlpha(color, 0.75f);
                view.Glow.SetPoints(pts, Vector2.zero);
                view.Line.SetPoints(pts, Vector2.zero);
                // El nombre, sobre la estrella más alta de la constelación (así no tapa las palabras de abajo).
                var topStar = _visible[c.Start].Pos;
                for (int i = c.Start + 1; i < c.Start + c.Count; i++)
                    if (_visible[i].Pos.y > topStar.y) topStar = _visible[i].Pos;
                view.Name.gameObject.SetActive(true);
                view.Name.text = c.Group ?? "";
                view.Name.color = color;
                view.Name.rectTransform.anchoredPosition = topStar + new Vector2(0f, StarSize * 0.5f + 26f);
                ordinal++;
            }
            for (int i = ordinal; i < _clusterViews.Count; i++)
            {
                _clusterViews[i].Glow.gameObject.SetActive(false);
                _clusterViews[i].Line.gameObject.SetActive(false);
                _clusterViews[i].Name.gameObject.SetActive(false);
            }
            if (ordinal > _lastClusterCount && ordinal > 0)
            {
                var nv = _clusterViews[ordinal - 1];
                StartCoroutine(PopRect(nv.Name.rectTransform, 1.25f, 0.3f));
            }
            _lastClusterCount = ordinal;
        }

        private ClusterView ClusterViewAt(int i)
        {
            while (_clusterViews.Count <= i)
            {
                var v = new ClusterView();
                v.Glow = LineIn(_sky, RailLine.Style.Soft, 30f);
                v.Line = LineIn(_sky, RailLine.Style.Solid, 6f);
                // Las líneas van detrás de las estrellas.
                v.Glow.transform.SetSiblingIndex(0);
                v.Line.transform.SetSiblingIndex(1);
                v.Name = MakeText(_sky, "ClusterName", 32, TextAnchor.MiddleCenter, Color.white, 0f, 0f);
                NeuroStyle.ClayText(v.Name, 2.5f, 3f);
                var r = v.Name.rectTransform;
                r.anchorMin = r.anchorMax = new Vector2(0.5f, 0.5f);
                r.sizeDelta = new Vector2(420f, 48f);
                v.Name.horizontalOverflow = HorizontalWrapMode.Overflow;
                _clusterViews.Add(v);
            }
            return _clusterViews[i];
        }

        private static RailLine LineIn(Transform parent, RailLine.Style style, float thickness)
        {
            var go = new GameObject("ConstellationLine");
            go.transform.SetParent(parent, false);
            var r = go.AddComponent<RectTransform>();
            r.anchorMin = r.anchorMax = new Vector2(0.5f, 0.5f);
            r.sizeDelta = Vector2.zero;
            var line = go.AddComponent<RailLine>();
            line.Setup(style, thickness);
            return line;
        }

        private void ClearSky()
        {
            foreach (var s in _visible) if (s.Rect != null) Destroy(s.Rect.gameObject);
            _visible.Clear();
            _live.Clear();
            _liveWords = new List<FluencyWord>();
            _said.Clear();
            _tokenTimes.Clear();
            _lastClusterCount = 0;
            foreach (var v in _clusterViews)
            {
                v.Glow.gameObject.SetActive(false);
                v.Line.gameObject.SetActive(false);
                v.Name.gameObject.SetActive(false);
            }
        }

        // ------------------------------------------------------------------ dónde nace cada estrella

        private float Room(Vector2 p)
        {
            // Distancia a lo más cercano (las etiquetas van debajo: la distancia vertical pesa más).
            float best = float.MaxValue;
            foreach (var s in _visible)
            {
                var d = p - s.Pos;
                best = Mathf.Min(best, new Vector2(d.x * 0.8f, d.y * 1.5f).magnitude);
            }
            foreach (var v in _clusterViews)
                if (v.Name.gameObject.activeSelf)
                {
                    var d = p - v.Name.rectTransform.anchoredPosition;
                    best = Mathf.Min(best, new Vector2(d.x * 0.6f, d.y * 1.6f).magnitude);
                }
            return best;
        }

        private bool Inside(Vector2 p)
        {
            float halfW = _sky.rect.width * 0.5f - 110f;
            return Mathf.Abs(p.x) <= halfW && p.y <= _skyTop - 70f && p.y >= _skyBottom + 50f;
        }

        private Vector2 PlaceFree()
        {
            Vector2 best = Vector2.zero;
            float bestRoom = -1f;
            float halfW = _sky.rect.width * 0.5f - 110f;
            for (int i = 0; i < 48; i++)
            {
                var p = new Vector2(Mathf.Lerp(-halfW, halfW, (float)_rng.NextDouble()),
                    Mathf.Lerp(_skyBottom + 50f, _skyTop - 70f, (float)_rng.NextDouble()));
                float room = _visible.Count == 0 ? 1000f - p.magnitude * 0.3f : Room(p);
                if (room > bestRoom) { bestRoom = room; best = p; }
            }
            return best;
        }

        private Vector2 PlaceNear(Vector2 from)
        {
            Vector2 best = PlaceFree();
            float bestRoom = -1f;
            for (int i = 0; i < 24; i++)
            {
                float a = (float)_rng.NextDouble() * Mathf.PI * 2f;
                float r = Mathf.Lerp(135f, 185f, (float)_rng.NextDouble());
                var p = from + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * r;
                if (!Inside(p)) continue;
                float room = Mathf.Min(Room(p), 170f); // basta con que no choque: se prefiere cerca
                if (room > bestRoom) { bestRoom = room; best = p; }
            }
            return bestRoom >= 70f ? best : PlaceFree();
        }

        // ------------------------------------------------------------------ interfaz

        private void ShowInputMode()
        {
            bool intro = _phase == Phase.Intro;
            _micRect.gameObject.SetActive(!_keyboard);
            _keyboardRoot.gameObject.SetActive(_keyboard);
            _addButton.gameObject.SetActive(_keyboard);
            _addButton.Find("Label").GetComponent<Text>().text = intro ? "Empezar" : "Agregar";
            _writeButton.gameObject.SetActive(intro && _speechOk);
            _writeButton.Find("Label").GetComponent<Text>().text = _keyboard ? "Prefiero hablar" : "Prefiero escribir";
            _caption.gameObject.SetActive(!_keyboard);
        }

        private void HideInputs()
        {
            _micRect.gameObject.SetActive(false);
            _keyboardRoot.gameObject.SetActive(false);
            _addButton.gameObject.SetActive(false);
            _writeButton.gameObject.SetActive(false);
            _caption.gameObject.SetActive(false);
        }

        private void FocusInput()
        {
            if (_input == null) return;
            _addButton.Find("Label").GetComponent<Text>().text = "Agregar";
            _input.ActivateInputField();
            _input.Select();
        }

        private void UpdateMicPulse()
        {
            bool on = _phase == Phase.Listening && !_keyboard && !GameClock.Paused;
            _micRings.gameObject.SetActive(on || _phase == Phase.Intro);
            float t = GameClock.Time;
            for (int i = 0; i < _micRings.childCount; i++)
            {
                var ring = _micRings.GetChild(i) as RectTransform;
                float k = on ? Mathf.Repeat(t * 0.7f + i / (float)_micRings.childCount, 1f) : 0.2f + 0.1f * Mathf.Sin(t * 3f + i);
                ring.sizeDelta = Vector2.one * Mathf.Lerp(MicSize * 1.05f, MicSize * 1.9f, k);
                ring.GetComponent<Image>().color = NeuroStyle.WithAlpha(on ? NeuroStyle.Coral : NeuroStyle.Sky, (1f - k) * (on ? 0.55f : 0.35f));
            }
            if (_phase == Phase.Intro) _micRect.localScale = Vector3.one * (1f + 0.04f * Mathf.Sin(t * 4f));
            else _micRect.localScale = Vector3.one;
        }

        private void UpdateTimer()
        {
            if (_phase != Phase.Listening) return;
            float left = _endsAt - GameClock.Time;
            SetTimerFraction(Mathf.Clamp01(left / FluencyContract.RoundSeconds));
            int whole = Mathf.CeilToInt(left);
            if (whole <= 5 && whole >= 1 && whole != _lastTickSecond)
            {
                _lastTickSecond = whole;
                GameFeel.Tick();
            }
        }

        private void SetCaption(string text)
        {
            _caption.text = string.IsNullOrWhiteSpace(text) ? "" : "…" + text;
        }

        private void SetHint(string text) => _hint.text = text;

        private bool SoundAllowed => GameFeel.SoundOn && (_config == null || _config.config == null || _config.config.sound_enabled);

        private void Sfx(AudioClip clip, float volume)
        {
            if (SoundAllowed) _audioSource.PlayOneShot(clip, volume);
        }

        private IEnumerator FloatText(Vector2 pos, string text, Color color, int size)
        {
            var t = MakeText(_fxRect, "Float", size, TextAnchor.MiddleCenter, color, 0f, 0f);
            var r = t.rectTransform;
            r.anchorMin = r.anchorMax = new Vector2(0.5f, 0.5f);
            r.sizeDelta = new Vector2(400f, 70f);
            t.text = text;
            float e = 0f;
            const float seconds = 1.3f;
            while (e < seconds)
            {
                e += GameClock.DeltaTime;
                float k = Mathf.Clamp01(e / seconds);
                r.anchoredPosition = pos + new Vector2(0f, 60f * UiFx.EaseOutCubic(k));
                t.color = NeuroStyle.WithAlpha(color, color.a * (1f - k * k));
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
            SpeechClient.Stop();
            HideInputs();
            int score = FluencyContract.Score(_valid, _kinds);
            int total = 0;
            foreach (int v in _valid) total += v;
            ShowResult(score, total);

            var telemetry = new StroopTelemetry
            {
                user_id = _config.user_id,
                game_id = _config.game_id,
                session_metrics = new StroopSessionMetrics
                {
                    correct_trials = total,
                    total_trials = total,
                    calculated_score = score,
                    average_response_time_ms = 0,
                    level = _config.config.level,
                    timed = _config.config.timed,
                    end_rating = score / 100f,
                    peak_level = _config.config.level,
                    fluency_categories = _roundIds.ToArray(),
                    fluency_valid = _valid.ToArray(),
                    fluency_repeats = _repeats.ToArray(),
                    fluency_unknown = _unknowns.ToArray(),
                    fluency_cluster = _meanClusters.ToArray(),
                    fluency_switches = _switches.ToArray(),
                    fluency_quarters = _quarters.ToArray(),
                    fluency_top = _tops.ToArray(),
                    fluency_words = _words.ToArray(),
                    fluency_input = _keyboard ? "teclado" : (_offline ? "voz_telefono" : "voz"),
                }
            };
            NativeBridge.ForwardTelemetryToPlatform(JsonUtility.ToJson(telemetry));
            yield break;
        }

        private void ShowResult(int score, int total)
        {
            _exit.Show();
            _title.text = "¡Tu cielo!";
            _prompt.text = "";
            _roundLabel.text = "";
            _summary.gameObject.SetActive(false);
            _resultRoot.Find("Title").GetComponent<Text>().text = total >= 35 ? "¡Cielo lleno de estrellas!" : total >= 20 ? "¡Buenas constelaciones!" : "Cielo encendido";
            _resultRoot.Find("Detail").GetComponent<Text>().text = $"{total} palabras en {_valid.Count} rondas";
            string best = "";
            int bestN = 0;
            foreach (var t in _tops)
                foreach (var part in t.Split(';'))
                {
                    int colon = part.LastIndexOf(':');
                    if (colon <= 0) continue;
                    if (int.TryParse(part.Substring(colon + 1), out int k) && k > bestN) { bestN = k; best = part.Substring(0, colon); }
                }
            _resultRoot.Find("Extra").GetComponent<Text>().text = bestN >= 2 ? $"Constelación más grande: {best} ({bestN})" : "";
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

            var canvasGo = new GameObject("ConstellationCanvas");
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
            WorldBackdrop.Build(bgRect, GameWorld.WordSky);

            var safeGo = new GameObject("SafeAreaContent");
            safeGo.transform.SetParent(canvasGo.transform, false);
            _safe = safeGo.AddComponent<RectTransform>();
            ApplySafeArea(_safe);

            var playGo = new GameObject("Play");
            playGo.transform.SetParent(_safe, false);
            _play = playGo.AddComponent<RectTransform>();
            Stretch(_play);

            _sky = Layer(_play, "Sky");
            _fxRect = Layer(_play, "Fx");

            _hud = new GameHud(_safe, "Constelación de Palabras", MarginU, this);
            BuildTimer();

            _roundLabel = CenteredText(_play, "Round", 36, new Color(1f, 1f, 1f, 0.75f));
            _title = CenteredText(_play, "Title", 92, NeuroStyle.Sun);
            NeuroStyle.ClayText(_title, 6f, 10f);
            _prompt = CenteredText(_play, "Prompt", 40, NeuroStyle.Cream);
            NeuroStyle.ClayText(_prompt, 2.5f, 3f);
            _summary = CenteredText(_play, "Summary", 50, NeuroStyle.Sun);
            NeuroStyle.ClayText(_summary, 3.5f, 5f);
            _summary.gameObject.SetActive(false);
            _caption = CenteredText(_play, "Caption", 40, new Color(1f, 1f, 1f, 0.8f));
            _hint = CenteredText(_play, "Hint", 40, new Color(1f, 1f, 1f, 0.9f));
            NeuroStyle.ClayText(_hint, 2.5f, 3f);

            BuildMic();
            BuildKeyboard();
            _writeButton = TextButton("Write", "Prefiero escribir");

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

        private void BuildMic()
        {
            var go = new GameObject("Mic");
            go.transform.SetParent(_play, false);
            _micRect = go.AddComponent<RectTransform>();
            _micRect.anchorMin = _micRect.anchorMax = new Vector2(0.5f, 0.5f);
            _micRect.sizeDelta = new Vector2(MicSize, MicSize);

            var ringsGo = new GameObject("Rings");
            ringsGo.transform.SetParent(_micRect, false);
            _micRings = ringsGo.AddComponent<RectTransform>();
            _micRings.anchorMin = _micRings.anchorMax = new Vector2(0.5f, 0.5f);
            for (int i = 0; i < 3; i++)
            {
                var ring = NewImage(_micRings, "Ring", RingSprite.Get());
                ring.gameObject.SetActive(true);
            }
            var disc = NewImage(_micRect, "Disc", DiscSprite.Get());
            disc.color = NeuroStyle.Surface;
            Stretch(disc.rectTransform);
            disc.gameObject.SetActive(true);
            _mic = NewImage(_micRect, "Icon", ConstellationSprites.Mic());
            _mic.rectTransform.sizeDelta = new Vector2(MicSize * 0.78f, MicSize * 0.78f);
            _mic.gameObject.SetActive(true);
        }

        private void BuildKeyboard()
        {
            var go = new GameObject("Keyboard");
            go.transform.SetParent(_play, false);
            _keyboardRoot = go.AddComponent<RectTransform>();
            _keyboardRoot.anchorMin = _keyboardRoot.anchorMax = new Vector2(0.5f, 0.5f);
            _keyboardRoot.sizeDelta = new Vector2(620f, 130f);
            var bg = go.AddComponent<Image>();
            bg.sprite = RoundedRectSprite.Get(48);
            bg.type = Image.Type.Sliced;
            bg.color = ClayRaster.Cream;
            NeuroStyle.ClayFrame(bg, 5f, 10f);

            var text = MakeText(_keyboardRoot, "Text", 50, TextAnchor.MiddleLeft, NeuroStyle.Ink, 0f, 0f);
            text.supportRichText = false;
            text.rectTransform.offsetMin = new Vector2(34f, 0f);
            text.rectTransform.offsetMax = new Vector2(-24f, 0f);
            var placeholder = MakeText(_keyboardRoot, "Placeholder", 46, TextAnchor.MiddleLeft, NeuroStyle.WithAlpha(NeuroStyle.Ink, 0.45f), 0f, 0f);
            placeholder.text = "escribe una palabra…";
            placeholder.rectTransform.offsetMin = new Vector2(34f, 0f);
            placeholder.rectTransform.offsetMax = new Vector2(-24f, 0f);

            _input = go.AddComponent<InputField>();
            _input.textComponent = text;
            _input.placeholder = placeholder;
            _input.lineType = InputField.LineType.SingleLine;
            _input.characterLimit = 60;
            _input.onEndEdit.AddListener(_ =>
            {
                if (_phase == Phase.Listening) SubmitTyped();
            });
            bg.raycastTarget = true;
            go.SetActive(false);

            _addButton = ClayButton("Add", "Agregar", NeuroStyle.Lime, new Vector2(300f, 130f));
        }

        private RectTransform ClayButton(string name, string label, Color color, Vector2 size)
        {
            var go = new GameObject("Button" + name);
            go.transform.SetParent(_play, false);
            var r = go.AddComponent<RectTransform>();
            r.anchorMin = r.anchorMax = new Vector2(0.5f, 0.5f);
            r.sizeDelta = size;
            var img = go.AddComponent<Image>();
            img.sprite = RoundedRectSprite.Get(48);
            img.type = Image.Type.Sliced;
            img.color = color;
            img.raycastTarget = false;
            NeuroStyle.ClayFrame(img, 5f, 12f);
            var t = MakeText(r, "Label", 54, TextAnchor.MiddleCenter, NeuroStyle.Ink, 0f, 0f);
            t.text = label;
            BestFit(t, 30);
            go.SetActive(false);
            return r;
        }

        /// <summary>Botón de solo texto (subrayado con una línea fina), con un área de toque grande.</summary>
        private RectTransform TextButton(string name, string label)
        {
            var go = new GameObject("Button" + name);
            go.transform.SetParent(_play, false);
            var r = go.AddComponent<RectTransform>();
            r.anchorMin = r.anchorMax = new Vector2(0.5f, 0.5f);
            r.sizeDelta = new Vector2(520f, 120f);
            var hit = go.AddComponent<Image>();
            hit.color = new Color(0f, 0f, 0f, 0f);
            hit.raycastTarget = false;
            var t = MakeText(r, "Label", 42, TextAnchor.MiddleCenter, NeuroStyle.Sky, 0f, 0f);
            t.text = label;
            NeuroStyle.ClayText(t, 2f, 3f);
            var line = NewImage(r, "Underline", RoundedRectSprite.Get(6));
            line.type = Image.Type.Sliced;
            line.color = NeuroStyle.WithAlpha(NeuroStyle.Sky, 0.7f);
            line.rectTransform.sizeDelta = new Vector2(330f, 5f);
            line.rectTransform.anchoredPosition = new Vector2(0f, -30f);
            line.gameObject.SetActive(true);
            go.SetActive(false);
            return r;
        }

        private static Text CenteredText(Transform parent, string name, int size, Color color)
        {
            var t = MakeText(parent, name, size, TextAnchor.MiddleCenter, color, 0f, 0f);
            var r = t.rectTransform;
            r.anchorMin = r.anchorMax = new Vector2(0.5f, 0.5f);
            r.pivot = new Vector2(0.5f, 0.5f);
            BestFit(t, Mathf.Max(24, size / 2));
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
            img.color = NeuroStyle.Lime;
        }

        private void SetTimerFraction(float fraction)
        {
            _timerFill.anchorMax = new Vector2(Mathf.Clamp01(fraction), 1f);
            _timerFill.offsetMin = _timerFill.offsetMax = Vector2.zero;
            _timerFill.GetComponent<Image>().color = fraction > 0.5f ? Color.Lerp(NeuroStyle.Sun, NeuroStyle.Lime, (fraction - 0.5f) * 2f)
                                                                      : Color.Lerp(NeuroStyle.Coral, NeuroStyle.Sun, fraction * 2f);
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
            AddResultText("Detail", 48, new Vector2(0f, -150f), new Color(1f, 1f, 1f, 0.85f));
            AddResultText("Extra", 42, new Vector2(0f, -250f), new Color(1f, 1f, 1f, 0.7f));
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
            float top = _playH * 0.5f, bottom = -_playH * 0.5f;
            float contentW = _playW - MarginU * 2f;

            // Encabezado: ronda, categoría (grande, sol) y la consigna. Texto suelto, sin tarjeta.
            float y = top - (GameHud.Height + 40f);
            Place(_roundLabel, contentW, 50f, y - 25f);
            Place(_title, contentW, 110f, y - 100f);
            Place(_prompt, contentW, 56f, y - 180f);

            // El cielo, entre el encabezado y los controles de abajo.
            _skyTop = y - 230f;
            _skyBottom = bottom + 470f;
            float micY = bottom + 230f;
            _micRect.anchoredPosition = new Vector2(0f, micY);
            _keyboardRoot.anchoredPosition = new Vector2(-150f, micY);
            _addButton.anchoredPosition = new Vector2(330f, micY);
            _writeButton.anchoredPosition = new Vector2(0f, bottom + 70f);
            Place(_caption, contentW, 56f, micY + 170f);
            Place(_hint, contentW, 56f, micY + 170f);
            Place(_summary, contentW, 70f, micY + 110f); // en la revelación el micrófono ya no está
        }

        private static void Place(Text t, float w, float h, float y)
        {
            t.rectTransform.sizeDelta = new Vector2(w, h);
            t.rectTransform.anchoredPosition = new Vector2(0f, y);
        }

        private void UpdateHud()
        {
            _hud.SetLevel(_config != null ? Mathf.Max(1, _config.config.level) : 1);
            _hud.SetPoints(_points);
        }
    }
}
