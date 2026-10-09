using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using NeuroVida.Bridge;
using NeuroVida.Contracts;
using NeuroVida.Games.Secuencia; // RoundedRectSprite / RadialGlowSprite / RingSprite
using NeuroVida.Games.Parejas;   // SymbolSprite
using NeuroVida.Games.Shared;
using static NeuroVida.Games.Shared.UiKit;
using Motion = NeuroVida.Games.Shared.Motion; // UnityEngine.Motion también existe

namespace NeuroVida.Games.Radar
{
    /// <summary>
    /// "Radar" / Rescate relámpago: juego estrella de velocidad de procesamiento visual (ver <see cref="RadarContract"/>).
    /// Eres quien opera el radar de rescate:
    /// <list type="number">
    /// <item>Atento: el haz gira y el destello llega en un momento imprevisible.</item>
    /// <item>Destello: varios astronautas a la vez, cerca y lejos del centro (desde el nivel 5, también robots).</item>
    /// <item>Interferencia: borra la imagen.</item>
    /// <item>¿Dónde estaban?: se pone una baliza en cada lugar donde se vio un astronauta (se sabe cuántos eran) y
    /// "¡RESCATAR!".</item>
    /// <item>Revelación: ✓ rescatado (vuela a la fila), aro sol = se escapó, ✗ = baliza de más.</item>
    /// </list>
    /// Cada 5 rondas, "¡Lluvia de astronautas!" (6, destello largo) para medir "tu captura". El destello se acorta con
    /// el DDA común. Medidas: tu vistazo, tu captura, tu filtro y tu radar (por dirección y cerca/lejos).
    /// Reto = 120 s; Precisión = 20 destellos sin reloj.
    /// </summary>
    public class RadarGameController : GameControllerBase
    {
        public const string GameId = RadarContract.GameId;

        private const float UnitsPerDp = 3f;
        private const float MarginU = 60f;
        private const int CrewIcons = 10;

        private static readonly Color GoodColor = NeuroStyle.Lime;
        private static readonly Color BadColor = NeuroStyle.Coral;
        private static readonly Color AmberColor = NeuroStyle.Sun;
        private static readonly Color PhosphorColor = NeuroStyle.Lime;

        private enum Phase { Idle, Watch, Answer, Feedback, Done }

        private System.Random _rng;
        private AdaptiveDifficulty _dda;
        private Phase _phase = Phase.Idle;
        private bool Endless => _config != null && _config.config.timed;
        private bool Precision => !Endless;
        private int _previousFrameRate;

        // ronda en curso
        private RadarTrial _trial;
        private int _trialSeed; // corrimiento visual fijo de la ronda (lo que se ve no se mueve al revelar)
        private readonly HashSet<int> _beacons = new HashSet<int>();
        private bool _submitted;
        private float _answerStart;

        // sesión
        private readonly List<float> _exposures = new List<float>();
        private readonly List<int> _loads = new List<int>();
        private readonly List<int> _rainScores = new List<int>();
        private readonly int[] _sectorHits = new int[RadarContract.Directions];
        private readonly int[] _sectorTrials = new int[RadarContract.Directions];
        private readonly int[] _ringHits = new int[RadarContract.Rings];
        private readonly int[] _ringTrials = new int[RadarContract.Rings];
        private int _trials, _normal, _success, _rescued, _robotsShown, _robotsTouched;
        private int _streak, _bestStreak, _points;
        private long _rtSum;
        private int _rtCount;
        private float _endsAt;
        private int _lastTickSecond = -1;
        private float _sweepAngle;
        private bool _robotsExplained;

        // UI
        private RectTransform _safe, _fxRect, _scopeRect, _stimRoot, _timerBg, _timerFill, _crewRow, _rescueRect;
        private Image _scope, _sweep, _mask, _fixation;
        private Text _prompt, _hint, _crewLabel;
        private Button _rescueButton;
        private readonly List<Image> _astros = new List<Image>();
        private readonly List<Image> _astroGlows = new List<Image>();
        private readonly List<Image> _robots = new List<Image>();
        private readonly List<Image> _slots = new List<Image>();
        private readonly List<Image> _beaconImgs = new List<Image>();
        private readonly List<Image> _marks = new List<Image>();
        private readonly List<Image> _crew = new List<Image>();
        private Toast _toast;
        private ExitButton _exit;
        private GameHud _hud;
        private CountdownScreen _countdown;
        private float _glassR, _scopeSize, _itemSize;

        // ------------------------------------------------------------------ sesión

        public void StartSession(SequenceInitConfig config)
        {
            _config = config;
            _rng = new System.Random();
            var age = DdaUserProfileConfig.ParseAgeBand(config.config.age_band);
            float start = AdaptiveDifficulty.StartRating(config.config, RadarContract.MaxLevel);
            // Pocas rondas por partida (~15-20): pasos más grandes. Sin tiempo de reacción: acá cuenta lo que se ve, no
            // lo rápido que se responde.
            _dda = new AdaptiveDifficulty(RadarContract.MaxLevel, age, start, stepUp: 0.3f, useReaction: false);

            // Los destellos son de decenas de milisegundos: a 30 cuadros por segundo (lo que Android da por defecto)
            // los saltos serían gruesos. Se pide 60 mientras dura el juego.
            _previousFrameRate = Application.targetFrameRate;
            Application.targetFrameRate = 60;

            _phase = Phase.Idle;
            _exposures.Clear();
            _loads.Clear();
            _rainScores.Clear();
            for (int d = 0; d < RadarContract.Directions; d++) _sectorHits[d] = _sectorTrials[d] = 0;
            for (int r = 0; r < RadarContract.Rings; r++) _ringHits[r] = _ringTrials[r] = 0;
            _trials = _normal = _success = _rescued = _robotsShown = _robotsTouched = 0;
            _streak = _bestStreak = _points = 0;
            _rtSum = 0;
            _rtCount = 0;
            _lastTickSecond = -1;
            _endsAt = 0f;
            _robotsExplained = false;

            _resultRoot.gameObject.SetActive(false);
            _exit.Hide();
            _timerBg.gameObject.SetActive(Endless);
            _hud.SetStreak(0);
            foreach (var c in _crew) c.gameObject.SetActive(false);
            _crewLabel.text = "Rescatados: 0";
            HideStimuli();
            HideAnswers();
            _mask.gameObject.SetActive(false);

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
            yield return StartCoroutine(_countdown.Play("Radar", Assessment.Subtitle("Prepárate"), () => _safe.gameObject.SetActive(true)));
            _safe.gameObject.SetActive(true);
            yield return null;
            ApplySafeArea(_safe);
            Canvas.ForceUpdateCanvases();
            Layout();
            UpdateHud();

            // Primera vez: qué va a pasar, antes del primer destello.
            SetPrompt("Rescate relámpago", Color.white);
            SetHint("Varios astronautas aparecen un instante: recuerda DÓNDE estaban.");
            yield return StartCoroutine(Wait(2.6f));

            _endsAt = GameClock.Time + RadarContract.RetoSeconds;
            while (!Finished())
                yield return StartCoroutine(RunTrial());
            yield return StartCoroutine(FinishGame());
        }

        private bool Finished() => Precision ? _trials >= RadarContract.PrecisionTrials : GameClock.Time >= _endsAt;

        // ------------------------------------------------------------------ una ronda

        private IEnumerator RunTrial()
        {
            bool rain = RadarContract.IsRainTrial(_trials);
            _trial = rain ? RadarContract.RainTrial(_dda.PresentedLevel, _rng) : RadarContract.NextTrial(_dda.PresentedLevel, _rng);
            _trialSeed = _rng.Next(100000);
            _beacons.Clear();
            _submitted = false;

            // 1. Atento: el haz gira; el destello llega sin aviso (espera imprevisible = alerta propia).
            _phase = Phase.Watch;
            HideAnswers();
            _sweep.gameObject.SetActive(true);
            _fixation.gameObject.SetActive(true);
            if (rain)
            {
                SetPrompt("¡Lluvia de astronautas!", AmberColor);
                SetHint($"Vienen {RadarContract.RainTargets} a la vez, con un destello más largo.");
                GameFeel.LevelUp();
            }
            else
            {
                SetPrompt("Atento al radar...", Color.white);
                if (_trials < 2) SetHint("El destello llega en cualquier momento.");
            }
            if (!rain && !_robotsExplained && _trial.Robots.Length > 0)
            {
                _robotsExplained = true;
                _toast.Show("¡Llegan robots!", "Los robots no se rescatan: toca solo astronautas", AmberColor, 1.6f);
                yield return StartCoroutine(Wait(1.2f));
            }
            yield return StartCoroutine(Wait(1.5f + 2.0f * (float)_rng.NextDouble()));
            _hint.gameObject.SetActive(false);

            // 2. Destello: astronautas (+ robots). El haz y la mira se apagan para no tapar nada.
            ShowStimuli(_trial);
            _sweep.gameObject.SetActive(false);
            _fixation.gameObject.SetActive(false);
            PlayTone(1318f, 0.06f, 0.07f);
            float shownAt = Time.unscaledTime;
            float startClock = GameClock.Time;
            float exposure = _trial.ExposureMs / 1000f;
            bool paused = false;
            // Se apaga en el cuadro más cercano a la duración pedida (a 60 cuadros por segundo, pasos de ~17 ms).
            while (GameClock.Time - startClock + Time.unscaledDeltaTime * 0.5f < exposure)
            {
                if (GameClock.Paused) paused = true;
                yield return null;
            }
            float actualMs = (Time.unscaledTime - shownAt) * 1000f;
            HideStimuli();

            // 3. Interferencia (máscara): borra la imagen que queda en la retina.
            _mask.sprite = RadarSprites.Mask(_trials);
            _mask.rectTransform.localRotation = Quaternion.Euler(0f, 0f, (float)_rng.NextDouble() * 360f);
            _mask.color = Color.white;
            _mask.gameObject.SetActive(true);
            PlayTone(233f, 0.12f, 0.05f);
            yield return StartCoroutine(Wait(0.35f));
            yield return StartCoroutine(FadeMask(0.15f));

            if (paused || GameClock.Paused)
            {
                // Una pausa en pleno destello lo invalida: se repite sin contar.
                _toast.Show("Otra vez", "La pausa cortó el destello", AmberColor, 0.9f);
                yield return StartCoroutine(Wait(0.6f));
                yield break;
            }

            // 4. ¿Dónde estaban? Balizas sobre los lugares (se sabe cuántos eran).
            _phase = Phase.Answer;
            int n = _trial.Targets.Length;
            SetPrompt($"¿Dónde estaban los {n}?", Color.white);
            ShowSlots();
            UpdateBeaconHint();
            _rescueRect.gameObject.SetActive(true);
            StartCoroutine(PopIn(_rescueRect, 0.2f));
            _answerStart = GameClock.Time;
            while (!_submitted) yield return null;
            _rtSum += (long)((GameClock.Time - _answerStart) * 1000f);
            _rtCount++;

            // 5. Revelación.
            _phase = Phase.Feedback;
            yield return StartCoroutine(Resolve(actualMs));
        }

        private IEnumerator Resolve(float actualMs)
        {
            _rescueRect.gameObject.SetActive(false);
            _hint.gameObject.SetActive(false);
            var answer = RadarContract.Evaluate(_trial, _beacons);
            bool rain = _trial.Rain;
            bool ok = RadarContract.Success(_trial, answer);
            _trials++;

            var targets = new HashSet<int>(_trial.Targets);
            foreach (int s in _trial.Targets)
            {
                int d = RadarContract.DirectionOf(s), r = RadarContract.RingOf(s);
                _sectorTrials[d]++;
                _ringTrials[r]++;
                if (_beacons.Contains(s))
                {
                    _sectorHits[d]++;
                    _ringHits[r]++;
                }
            }

            if (rain) _rainScores.Add(RadarContract.RainScore(answer));
            else
            {
                _normal++;
                _exposures.Add(actualMs);
                _loads.Add(_trial.Targets.Length);
                _robotsShown += _trial.Robots.Length;
                _robotsTouched += answer.RobotsTouched;
                if (ok) _success++;
                _streak = ok ? _streak + 1 : 0;
                _bestStreak = Mathf.Max(_bestStreak, _streak);
                _hud.SetStreak(_streak);
            }
            int pts = RadarContract.Points(answer.Hits, ok && !rain, _trial.Level, _streak);
            _points += pts;
            _rescued += answer.Hits;

            // Lo que había, a la vista: rescatados (✓ + resplandor lima), los que se escaparon (aro sol, un poco
            // transparentes), balizas de más (✗) y los robots.
            for (int i = 0; i < _slots.Count; i++) _slots[i].gameObject.SetActive(false);
            int ai = 0;
            foreach (int s in _trial.Targets)
            {
                var pos = SlotPosition(s);
                var a = _astros[ai];
                var g = _astroGlows[ai];
                ai++;
                a.rectTransform.anchoredPosition = pos;
                g.rectTransform.anchoredPosition = pos;
                bool hit = _beacons.Contains(s);
                a.color = hit ? Color.white : NeuroStyle.WithAlpha(Color.white, 0.6f);
                a.gameObject.SetActive(true);
                g.color = NeuroStyle.WithAlpha(hit ? GoodColor : AmberColor, hit ? 0.55f : 0.4f);
                g.gameObject.SetActive(true);
                _beaconImgs[s].gameObject.SetActive(false);
                if (hit) ShowMark(s, true);
                else
                {
                    _slots[s].color = AmberColor;
                    _slots[s].rectTransform.sizeDelta = Vector2.one * _itemSize * 1.45f;
                    _slots[s].gameObject.SetActive(true);
                }
            }
            for (int i = 0; i < _trial.Robots.Length; i++)
            {
                var r = _robots[i];
                r.rectTransform.anchoredPosition = SlotPosition(_trial.Robots[i]);
                r.color = NeuroStyle.WithAlpha(Color.white, 0.85f);
                r.gameObject.SetActive(true);
            }
            foreach (int b in _beacons)
                if (!targets.Contains(b)) ShowMark(b, false);

            int n = _trial.Targets.Length;
            var change = rain ? DdaChange.None : _dda.Register(ok);
            if (answer.Hits > 0)
            {
                GameFeel.Correct(Mathf.Max(1, _streak));
                SetPrompt(answer.Hits == n ? RescueWord(answer.Hits) : $"Rescataste {answer.Hits} de {n}", answer.Hits == n || ok ? GoodColor : AmberColor);
            }
            else
            {
                GameFeel.Wrong();
                SetPrompt("Se escaparon esta vez", AmberColor);
            }
            if (answer.Hits < n && answer.Hits > 0) SetHint(answer.Hits == n - 1 ? "Uno se escapó (aro sol)" : $"{n - answer.Hits} se escaparon (aro sol)");
            if (pts > 0) StartCoroutine(FloatText(_scopeRect, "+" + pts, NeuroStyle.Sun));

            if (change == DdaChange.Up)
            {
                _toast.Show("¡Subes!", LevelNews(_dda.Level), GoodColor, 1.0f);
                GameFeel.LevelUp();
            }
            else if (change == DdaChange.Down || (!rain && _dda.Struggling))
                _toast.Show("Con calma", "Destello más largo", AmberColor, 0.9f);
            UpdateHud();

            yield return StartCoroutine(Wait(answer.Hits == n ? 0.7f : 1.2f));
            if (answer.Hits > 0) yield return StartCoroutine(FlyToCrew(answer.Hits));
            HideStimuli();
            HideAnswers();
        }

        private static string RescueWord(int hits)
        {
            switch (hits)
            {
                case 2: return "¡Rescate doble!";
                case 3: return "¡Rescate triple!";
                case 4: return "¡Rescate cuádruple!";
                case 5: return "¡Rescate quíntuple!";
                case 6: return "¡Rescate total!";
                default: return "¡Rescatado!";
            }
        }

        /// <summary>Qué trae el nivel nuevo.</summary>
        private static string LevelNews(int level)
        {
            if (RadarContract.TargetCount(level) > RadarContract.TargetCount(level - 1)) return "Un astronauta más";
            if (RadarContract.RobotCount(level) > RadarContract.RobotCount(level - 1)) return "Llegan robots: no se rescatan";
            return $"Destello de {RadarContract.ExposureMs(level)} ms";
        }

        // ------------------------------------------------------------------ entrada

        private void OnRescue()
        {
            if (_phase != Phase.Answer || _submitted) return;
            _submitted = true;
            GameFeel.Haptic(GameFeel.HapticKind.Light);
        }

        private void Update()
        {
            // Haz del radar: gira mientras se espera (quieto con "quitar animaciones").
            float dt = GameClock.DeltaTime;
            if (_sweep != null && _sweep.gameObject.activeSelf && dt > 0f && !GameFeel.ReduceMotion)
            {
                _sweepAngle = Mathf.Repeat(_sweepAngle - 140f * dt, 360f);
                _sweep.rectTransform.localRotation = Quaternion.Euler(0f, 0f, _sweepAngle);
            }
            UpdateClock();

            if (_phase != Phase.Answer || _submitted || dt <= 0f) return;
            // Tocar en cualquier parte del radar elige el lugar más cercano (no hace falta acertarle al aro).
            if (!Input.GetMouseButtonDown(0)) return;
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(_scopeRect, Input.mousePosition, null, out var local)) return;
            int slot = RadarContract.NearestSlot(local.x / _glassR, local.y / _glassR);
            if (slot < 0) return;
            ToggleBeacon(slot);
        }

        private void ToggleBeacon(int slot)
        {
            int n = _trial.Targets.Length;
            if (_beacons.Remove(slot))
            {
                _beaconImgs[slot].gameObject.SetActive(false);
                _slots[slot].gameObject.SetActive(true);
                PlayTone(523f, 0.05f, 0.04f);
            }
            else if (_beacons.Count < n)
            {
                _beacons.Add(slot);
                _slots[slot].gameObject.SetActive(false);
                _beaconImgs[slot].gameObject.SetActive(true);
                StartCoroutine(PopRect(_beaconImgs[slot].rectTransform, 1.2f, 0.18f));
                PlayTone(784f, 0.05f, 0.05f);
            }
            else
            {
                SetHint($"Ya pusiste {n}: toca una baliza para sacarla");
                return;
            }
            GameFeel.Haptic(GameFeel.HapticKind.Light);
            UpdateBeaconHint();
        }

        private void UpdateBeaconHint()
        {
            int n = _trial.Targets.Length;
            SetHint(_beacons.Count == 0 ? "Toca los lugares donde viste astronautas" : $"{_beacons.Count} de {n} balizas · toca de nuevo para sacar");
        }

        private void UpdateClock()
        {
            if (!Endless || _phase == Phase.Idle || _phase == Phase.Done || _endsAt <= 0f) return;
            float left = _endsAt - GameClock.Time;
            SetTimerFraction(Mathf.Clamp01(left / RadarContract.RetoSeconds));
            int whole = Mathf.CeilToInt(left);
            if (whole <= 5 && whole >= 1 && whole != _lastTickSecond)
            {
                _lastTickSecond = whole;
                GameFeel.Tick();
            }
        }

        // ------------------------------------------------------------------ estímulos

        private void ShowStimuli(RadarTrial t)
        {
            for (int i = 0; i < _astros.Count; i++)
            {
                bool on = i < t.Targets.Length;
                _astros[i].gameObject.SetActive(on);
                _astroGlows[i].gameObject.SetActive(false);
                if (!on) continue;
                _astros[i].color = Color.white;
                _astros[i].rectTransform.anchoredPosition = SlotPosition(t.Targets[i]);
            }
            for (int i = 0; i < _robots.Count; i++)
            {
                bool on = i < t.Robots.Length;
                _robots[i].gameObject.SetActive(on);
                if (!on) continue;
                _robots[i].color = Color.white;
                _robots[i].rectTransform.anchoredPosition = SlotPosition(t.Robots[i]);
            }
        }

        /// <summary>Centro de un lugar en el radar, con un pequeño corrimiento (fijo por ronda) para que no se vea como
        /// una grilla.</summary>
        private Vector2 SlotPosition(int slot)
        {
            RadarContract.Position(slot, out float x, out float y);
            var j = new System.Random(slot * 7919 + _trialSeed);
            float jx = ((float)j.NextDouble() - 0.5f) * 0.05f, jy = ((float)j.NextDouble() - 0.5f) * 0.05f;
            return new Vector2((x + jx) * _glassR, (y + jy) * _glassR);
        }

        /// <summary>Centro exacto del lugar (las balizas y aros van ahí, sin corrimiento).</summary>
        private Vector2 SlotCenter(int slot)
        {
            RadarContract.Position(slot, out float x, out float y);
            return new Vector2(x * _glassR, y * _glassR);
        }

        private void ShowSlots()
        {
            for (int s = 0; s < _slots.Count; s++)
            {
                _slots[s].color = NeuroStyle.WithAlpha(NeuroStyle.Cream, 0.5f);
                _slots[s].rectTransform.sizeDelta = Vector2.one * _itemSize * 1.2f;
                _slots[s].gameObject.SetActive(true);
                _beaconImgs[s].gameObject.SetActive(false);
                _marks[s].gameObject.SetActive(false);
            }
        }

        private void ShowMark(int slot, bool good)
        {
            var m = _marks[slot];
            m.sprite = good ? AnswerMarkSprite.Check() : AnswerMarkSprite.Cross();
            m.gameObject.SetActive(true);
            StartCoroutine(PopRect(m.rectTransform, 1.25f, 0.2f));
        }

        private void HideStimuli()
        {
            foreach (var a in _astros) a.gameObject.SetActive(false);
            foreach (var g in _astroGlows) g.gameObject.SetActive(false);
            foreach (var r in _robots) r.gameObject.SetActive(false);
        }

        private IEnumerator FadeMask(float seconds)
        {
            float t = 0f;
            while (t < seconds)
            {
                t += GameClock.DeltaTime;
                _mask.color = NeuroStyle.WithAlpha(Color.white, 1f - Mathf.Clamp01(t / seconds));
                yield return null;
            }
            _mask.gameObject.SetActive(false);
        }

        private void HideAnswers()
        {
            foreach (var s in _slots) s.gameObject.SetActive(false);
            foreach (var b in _beaconImgs) b.gameObject.SetActive(false);
            foreach (var m in _marks) m.gameObject.SetActive(false);
            if (_rescueRect != null) _rescueRect.gameObject.SetActive(false);
            if (_hint != null) _hint.gameObject.SetActive(false);
        }

        private void SetPrompt(string text, Color color)
        {
            _prompt.text = text;
            _prompt.color = color;
        }

        private void SetHint(string text)
        {
            _hint.text = text;
            _hint.gameObject.SetActive(true);
        }

        // ------------------------------------------------------------------ efectos

        /// <summary>Los rescatados vuelan a la fila de abajo (con "quitar animaciones", la fila se actualiza sin vuelo).</summary>
        private IEnumerator FlyToCrew(int count)
        {
            if (!GameFeel.ReduceMotion)
            {
                var flying = new List<(RectTransform r, Vector2 from, Vector2 to, float delay)>();
                int k = 0;
                for (int i = 0; i < _trial.Targets.Length; i++)
                {
                    if (!_beacons.Contains(_trial.Targets[i])) continue;
                    var src = _astros[i].rectTransform;
                    int slot = (_rescued - count + k) % CrewIcons;
                    var go = new GameObject("Flying");
                    go.transform.SetParent(_fxRect, false);
                    var r = go.AddComponent<RectTransform>();
                    r.anchorMin = r.anchorMax = new Vector2(0.5f, 0.5f);
                    r.sizeDelta = src.sizeDelta;
                    var img = go.AddComponent<Image>();
                    img.sprite = SymbolSprite.Get(ShapeKind.Helmet, 1);
                    img.raycastTarget = false;
                    flying.Add((r, LocalIn(_fxRect, src), LocalIn(_fxRect, _crew[slot].rectTransform), k * 0.08f));
                    _astros[i].gameObject.SetActive(false);
                    _astroGlows[i].gameObject.SetActive(false);
                    k++;
                }
                float t = 0f;
                const float seconds = 0.45f;
                float end = seconds + (flying.Count - 1) * 0.08f;
                while (t < end)
                {
                    t += GameClock.DeltaTime;
                    foreach (var f in flying)
                    {
                        float e = UiFx.EaseOutCubic(Mathf.Clamp01((t - f.delay) / seconds));
                        // Arco hacia arriba, como si los levantara el rayo de rescate.
                        f.r.anchoredPosition = Vector2.Lerp(f.from, f.to, e) + new Vector2(0f, Mathf.Sin(e * Mathf.PI) * 120f);
                        float s = Mathf.Lerp(_itemSize, 80f, e);
                        f.r.sizeDelta = new Vector2(s, s);
                    }
                    yield return null;
                }
                foreach (var f in flying) Destroy(f.r.gameObject);
            }

            _crewLabel.text = $"Rescatados: {_rescued}";
            int before = _rescued - count;
            if (before / CrewIcons != _rescued / CrewIcons)
            {
                _toast.Show($"¡{_rescued / CrewIcons * CrewIcons} rescatados!", "Escuadrón completo", NeuroStyle.Sun, 1.0f);
                GameFeel.LevelUp();
            }
            int filled = _rescued % CrewIcons;
            if (filled == 0 && _rescued > 0) filled = CrewIcons;
            for (int i = 0; i < _crew.Count; i++)
            {
                bool on = i < filled;
                bool fresh = on && !_crew[i].gameObject.activeSelf;
                _crew[i].gameObject.SetActive(on);
                if (fresh) StartCoroutine(PopRect(_crew[i].rectTransform, 1.3f, 0.25f));
            }
        }

        private IEnumerator FloatText(RectTransform anchor, string text, Color color)
        {
            var t = MakeText(_fxRect, "Float", 58, TextAnchor.MiddleCenter, color, 0f, 0f);
            NeuroStyle.ClayText(t, 4f, 6f);
            var r = t.rectTransform;
            r.anchorMin = r.anchorMax = new Vector2(0.5f, 0.5f);
            r.sizeDelta = new Vector2(300f, 90f);
            t.text = text;
            var pos = LocalIn(_fxRect, anchor);
            float e = 0f;
            const float seconds = 0.8f;
            while (e < seconds)
            {
                e += GameClock.DeltaTime;
                float k = Mathf.Clamp01(e / seconds);
                r.anchoredPosition = pos + new Vector2(0f, 40f * (Motion.Decorative ? UiFx.EaseOutCubic(k) : 0f));
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
            HideStimuli();
            HideAnswers();
            _sweep.gameObject.SetActive(true);

            float accuracy = _normal > 0 ? (float)_success / _normal : 0f;
            int glance = RadarContract.GlanceMs(_exposures);
            float load = glance > 0 ? RadarContract.GlanceLoad(_loads) : -1f;
            float capture = RadarContract.Capture(_rainScores);
            int score = RadarContract.Score(accuracy, glance);
            int avgMs = _rtCount > 0 ? (int)(_rtSum / _rtCount) : 0;

            SetPrompt("Fin del turno", GoodColor);
            ShowResult(score, glance);

            var telemetry = new StroopTelemetry
            {
                user_id = _config.user_id,
                game_id = _config.game_id,
                session_metrics = new StroopSessionMetrics
                {
                    correct_trials = _success,
                    total_trials = _normal,
                    calculated_score = score,
                    average_response_time_ms = avgMs,
                    level = _config.config.level,
                    timed = _config.config.timed,
                    end_rating = _dda.RatingNormalized,
                    mode_trials = _dda.ScoredTrials,
                    mode_hits = _dda.ScoredCorrect,
                    peak_level = _dda.PeakLevel,
                    glance_ms = glance,
                    glance_load = load,
                    sector_hits = (int[])_sectorHits.Clone(),
                    sector_trials = (int[])_sectorTrials.Clone(),
                    ring_hits = (int[])_ringHits.Clone(),
                    ring_trials = (int[])_ringTrials.Clone(),
                    capture = capture,
                    robots_shown = _robotsShown,
                    robots_touched = _robotsTouched
                }
            };
            NativeBridge.ForwardTelemetryToPlatform(JsonUtility.ToJson(telemetry));
            yield break;
        }

        private void ShowResult(int score, int glance)
        {
            _exit.Show();
            _resultRoot.Find("Title").GetComponent<Text>().text = score >= 85 ? "¡Ojo de radar!" : score >= 65 ? "¡Buen turno!" : "Turno completado";
            _resultRoot.Find("Detail").GetComponent<Text>().text = $"{_rescued} astronautas rescatados";
            _resultRoot.Find("Extra").GetComponent<Text>().text = glance > 0 ? $"Tu vistazo: {glance} ms" : $"Mejor racha {_bestStreak}";
            _resultRoot.gameObject.SetActive(true);
            StartCoroutine(AnimateResult(score));
        }

        // ------------------------------------------------------------------ construcción de UI

        protected override void BuildUi()
        {

            var canvasGo = new GameObject("RadarCanvas");
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
            WorldBackdrop.Build(bgRect, GameWorld.RadarStation);

            var safeGo = new GameObject("SafeAreaContent");
            safeGo.transform.SetParent(canvasGo.transform, false);
            _safe = safeGo.AddComponent<RectTransform>();
            ApplySafeArea(_safe);

            _hud = new GameHud(_safe, "Radar", MarginU, this);
            BuildTimer();

            _prompt = MakeText(_safe, "Prompt", 64, TextAnchor.MiddleCenter, Color.white, 0f, 0f);
            NeuroStyle.ClayText(_prompt, 3.5f, 5f);
            var pr = _prompt.rectTransform;
            pr.anchorMin = pr.anchorMax = new Vector2(0.5f, 1f);
            pr.pivot = new Vector2(0.5f, 0.5f);
            BestFit(_prompt, 42);

            BuildScope();
            BuildRescueButton();
            BuildCrewRow();

            _hint = MakeText(_safe, "Hint", 42, TextAnchor.MiddleCenter, new Color(1f, 1f, 1f, 0.8f), 0f, 0f);
            var hr = _hint.rectTransform;
            hr.anchorMin = hr.anchorMax = new Vector2(0.5f, 1f);
            hr.pivot = new Vector2(0.5f, 0.5f);
            BestFit(_hint, 42);                      // la letra no baja de 14 dp: si no cabe en un renglón pasa a dos
            _hint.gameObject.SetActive(false);

            var fx = new GameObject("Fx");
            fx.transform.SetParent(_safe, false);
            _fxRect = fx.AddComponent<RectTransform>();
            Stretch(_fxRect);

            BuildResultPanel();
            _exit = new ExitButton(_safe, this, UnitsPerDp);
            _toast = new Toast(_safe, this, UnitsPerDp);
            _toast.SetBelowHud();

            var flashGo = new GameObject("Flash");
            flashGo.transform.SetParent(canvasGo.transform, false);
            Stretch(flashGo.AddComponent<RectTransform>());
            _flash = flashGo.AddComponent<Image>();
            _flash.raycastTarget = false;
            _flash.color = new Color(0f, 0f, 0f, 0f);

            _countdown = new CountdownScreen(canvasGo.transform, UnitsPerDp);
        }

        private void BuildScope()
        {
            var go = new GameObject("Scope");
            go.transform.SetParent(_safe, false);
            _scopeRect = go.AddComponent<RectTransform>();
            _scopeRect.anchorMin = _scopeRect.anchorMax = new Vector2(0.5f, 1f);
            _scopeRect.pivot = new Vector2(0.5f, 0.5f);
            _scope = go.AddComponent<Image>();
            _scope.sprite = RadarSprites.Scope();
            _scope.raycastTarget = false;

            // Haz que barre (debajo de todo lo demás).
            _sweep = NewImage(_scopeRect, "Sweep", RadarSprites.Sweep());
            _sweep.color = NeuroStyle.WithAlpha(PhosphorColor, 0.32f);
            _sweep.gameObject.SetActive(true);

            _stimRoot = Layer(_scopeRect, "Stimuli");

            // Mira del centro: un aro quieto (sin aviso antes del destello).
            _fixation = NewImage(_stimRoot, "Fixation", RingSprite.Get());
            _fixation.color = NeuroStyle.WithAlpha(AmberColor, 0.7f);

            for (int s = 0; s < RadarContract.Slots; s++)
                _slots.Add(NewImage(_stimRoot, "Slot" + s, RadarSprites.Slot()));
            for (int i = 0; i < RadarContract.RainTargets; i++)
            {
                _astroGlows.Add(NewImage(_stimRoot, "AstroGlow" + i, RadialGlowSprite.Get()));
                var a = NewImage(_stimRoot, "Astro" + i, SymbolSprite.Get(ShapeKind.Helmet, 1));
                a.preserveAspect = true;
                _astros.Add(a);
            }
            for (int i = 0; i < 2; i++)
            {
                var r = NewImage(_stimRoot, "Robot" + i, RadarSprites.Robot());
                r.preserveAspect = true;
                _robots.Add(r);
            }
            for (int s = 0; s < RadarContract.Slots; s++)
                _beaconImgs.Add(NewImage(_stimRoot, "Beacon" + s, RadarSprites.Beacon()));
            for (int s = 0; s < RadarContract.Slots; s++)
                _marks.Add(NewImage(_stimRoot, "Mark" + s, null));

            // Interferencia: cubre todo el vidrio.
            _mask = NewImage(_scopeRect, "Mask", RadarSprites.Mask(0));
            Stretch(_mask.rectTransform);
        }

        private void BuildRescueButton()
        {
            var go = new GameObject("Rescue");
            go.transform.SetParent(_safe, false);
            _rescueRect = go.AddComponent<RectTransform>();
            _rescueRect.anchorMin = _rescueRect.anchorMax = new Vector2(0.5f, 1f);
            _rescueRect.pivot = new Vector2(0.5f, 0.5f);
            var bg = go.AddComponent<Image>();
            bg.sprite = RoundedRectSprite.Get(64);
            bg.type = Image.Type.Sliced;
            bg.color = AmberColor;
            NeuroStyle.ClayFrame(bg, 5f, 10f);
            _rescueButton = go.AddComponent<Button>();
            _rescueButton.transition = Selectable.Transition.None;
            _rescueButton.onClick.AddListener(OnRescue);
            go.AddComponent<PressScale>();
            var label = MakeText(_rescueRect, "Label", 60, TextAnchor.MiddleCenter, NeuroStyle.Ink, 0f, 0f);
            Stretch(label.rectTransform);
            label.text = "¡RESCATAR!";
            label.raycastTarget = false;
            go.SetActive(false);
        }

        private void BuildCrewRow()
        {
            var go = new GameObject("Crew");
            go.transform.SetParent(_safe, false);
            _crewRow = go.AddComponent<RectTransform>();
            _crewRow.anchorMin = _crewRow.anchorMax = new Vector2(0.5f, 0f);
            _crewRow.pivot = new Vector2(0.5f, 0f);
            _crewRow.sizeDelta = new Vector2(960f, 170f);
            _crewRow.anchoredPosition = new Vector2(0f, 40f);

            _crewLabel = MakeText(_crewRow, "Label", 42, TextAnchor.MiddleCenter, new Color(1f, 1f, 1f, 0.75f), 0f, 0f);
            var lr = _crewLabel.rectTransform;
            lr.anchorMin = new Vector2(0f, 1f);
            lr.anchorMax = new Vector2(1f, 1f);
            lr.pivot = new Vector2(0.5f, 1f);
            lr.sizeDelta = new Vector2(0f, 56f);
            lr.anchoredPosition = Vector2.zero;

            const float icon = 80f, gap = 12f;
            float total = CrewIcons * icon + (CrewIcons - 1) * gap;
            for (int i = 0; i < CrewIcons; i++)
            {
                var slot = NewImage(_crewRow, "Slot", DiscSprite.Get());
                var sr = slot.rectTransform;
                sr.anchorMin = sr.anchorMax = new Vector2(0.5f, 0f);
                sr.sizeDelta = new Vector2(icon * 0.5f, icon * 0.5f);
                sr.anchoredPosition = new Vector2(-total / 2f + icon / 2f + i * (icon + gap), icon / 2f + 8f);
                slot.color = new Color(1f, 1f, 1f, 0.10f);
                slot.gameObject.SetActive(true);

                var h = NewImage(_crewRow, "Rescued", SymbolSprite.Get(ShapeKind.Helmet, 1));
                h.preserveAspect = true;
                var hr = h.rectTransform;
                hr.anchorMin = hr.anchorMax = new Vector2(0.5f, 0f);
                hr.sizeDelta = new Vector2(icon, icon);
                hr.anchoredPosition = sr.anchoredPosition;
                _crew.Add(h);
            }
        }

        private void BuildTimer()
        {
            var bg = new GameObject("TimerBar");
            bg.transform.SetParent(_safe, false);
            _timerBg = bg.AddComponent<RectTransform>();
            _timerBg.anchorMin = _timerBg.anchorMax = new Vector2(0.5f, 1f);
            _timerBg.pivot = new Vector2(0.5f, 0.5f);
            var bgImg = bg.AddComponent<Image>();
            bgImg.sprite = RoundedRectSprite.Get(10);
            bgImg.type = Image.Type.Sliced;
            bgImg.color = new Color(1f, 1f, 1f, 0.12f);
            bgImg.raycastTarget = false;

            var fill = new GameObject("Fill");
            fill.transform.SetParent(bg.transform, false);
            _timerFill = fill.AddComponent<RectTransform>();
            _timerFill.anchorMin = Vector2.zero;
            _timerFill.anchorMax = Vector2.one;
            _timerFill.offsetMin = _timerFill.offsetMax = Vector2.zero;
            var img = fill.AddComponent<Image>();
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
            Rect safe = _safe.rect;
            float sw = Mathf.Max(safe.width, 400f);
            float sh = Mathf.Max(safe.height, 800f);
            float contentW = sw - MarginU * 2f;

            float y = GameHud.Height + 6f;
            _timerBg.sizeDelta = new Vector2(contentW, 16f);
            _timerBg.anchoredPosition = new Vector2(0f, -(y + 8f));
            y += 16f + 14f;

            const float promptH = 100f;
            _prompt.rectTransform.sizeDelta = new Vector2(contentW, promptH);
            _prompt.rectTransform.anchoredPosition = new Vector2(0f, -(y + promptH / 2f));
            y += promptH + 6f;

            // Radar: lo más grande posible (más grande = más periferia y lugares más fáciles de tocar), dejando lugar
            // para la línea de ayuda, el botón y la fila de rescatados.
            const float hintH = 116f, buttonH = 170f, crewH = 220f;
            _scopeSize = Mathf.Min(contentW + 40f, sh - y - hintH - buttonH - crewH);
            _scopeSize = Mathf.Max(_scopeSize, 620f);
            _scopeRect.sizeDelta = new Vector2(_scopeSize, _scopeSize);
            _scopeRect.anchoredPosition = new Vector2(0f, -(y + _scopeSize / 2f));
            y += _scopeSize + 8f;
            _glassR = _scopeSize * 0.5f * RadarSprites.GlassFraction;

            _hint.rectTransform.sizeDelta = new Vector2(contentW, hintH);
            _hint.rectTransform.anchoredPosition = new Vector2(0f, -(y + hintH / 2f));
            y += hintH;
            _rescueRect.sizeDelta = new Vector2(Mathf.Min(560f, contentW), 140f);
            _rescueRect.anchoredPosition = new Vector2(0f, -(y + buttonH / 2f));

            // Tamaños en el radar (proporcionales al vidrio). Los lugares de adentro están a ~0,35 radios entre sí:
            // el aro visible es algo menor, pero tocar cerca ya elige el lugar (el más cercano).
            _itemSize = _glassR * 0.26f;
            float cw = RadarContract.CenterWindow * _glassR;
            _sweep.rectTransform.sizeDelta = new Vector2(_glassR * 2f, _glassR * 2f);
            _fixation.rectTransform.sizeDelta = new Vector2(cw * 0.9f, cw * 0.9f);
            foreach (var a in _astros) a.rectTransform.sizeDelta = new Vector2(_itemSize, _itemSize);
            foreach (var g in _astroGlows) g.rectTransform.sizeDelta = new Vector2(_itemSize * 2.3f, _itemSize * 2.3f);
            foreach (var r in _robots) r.rectTransform.sizeDelta = new Vector2(_itemSize * 1.25f, _itemSize * 1.25f); // el dibujo del robot deja más margen
            for (int s = 0; s < RadarContract.Slots; s++)
            {
                var c = SlotCenter(s);
                _slots[s].rectTransform.anchoredPosition = c;
                _slots[s].rectTransform.sizeDelta = new Vector2(_itemSize * 1.2f, _itemSize * 1.2f);
                _beaconImgs[s].rectTransform.anchoredPosition = c;
                _beaconImgs[s].rectTransform.sizeDelta = new Vector2(_itemSize * 0.95f, _itemSize * 0.95f);
                _marks[s].rectTransform.anchoredPosition = c + new Vector2(_itemSize * 0.45f, _itemSize * 0.45f);
                _marks[s].rectTransform.sizeDelta = new Vector2(_itemSize * 0.6f, _itemSize * 0.6f);
            }
            _mask.rectTransform.sizeDelta = Vector2.zero;
        }

        private void UpdateHud()
        {
            _hud.SetLevel(_dda.PresentedLevel);
            if (Endless) _hud.SetPoints(_points);
            else _hud.SetInfo($"{Mathf.Min(_trials + 1, RadarContract.PrecisionTrials)} de {RadarContract.PrecisionTrials}");
        }
    }
}
