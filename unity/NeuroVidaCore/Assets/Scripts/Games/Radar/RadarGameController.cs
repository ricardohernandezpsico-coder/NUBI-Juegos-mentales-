using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using NeuroVida.Bridge;
using NeuroVida.Contracts;
using NeuroVida.Games.Shared;
using static NeuroVida.Games.Shared.UiKit;
using Motion = NeuroVida.Games.Shared.Motion; // UnityEngine.Motion también existe

namespace NeuroVida.Games.Radar
{
    /// <summary>
    /// «Rescate relámpago: qué cápsulas viste» (id <c>radar</c>, renovado el 9-oct; ver <see cref="RadarContract"/> y docs/diseno-rescate.md; el boceto aprobado es docs/previews/rescate-boceto.html). VELOCIDAD DE PROCESAMIENTO VISUAL con informe total: el radar gira y,
    /// sin aviso, un relámpago ilumina unas cápsulas de escape (y, desde el nivel 6, rocas grises que no se rescatan) en posiciones continuas al azar; la estática borra la imagen y en el tablero se elige QUÉ cápsulas se vieron (nunca DÓNDE) y se toca «¡Rescatar!».
    /// Las acertadas suben a la nave por un rayo tractor. Una ronda: atento (1,5-3,5 s) → destello (la duración REAL se mide a 60 cuadros por segundo; una pausa en pleno destello lo anula y la ronda se repite sin contar) → estática (350 ms) → respuesta (tope de 25 s) →
    /// revelación (2,3 s). Cada 4 rondas, una «lluvia de cápsulas» (4 cápsulas, destello FIJO de 300 ms) que queda fuera del motor y mide «Tu captura». Los mensajes van en una franja fija de arriba, NUNCA sobre el radar ni el tablero (el smoke lo comprueba).
    /// Todo el tiempo va con <see cref="GameClock"/> y lo que se mueve con <see cref="Motion"/>: con «quitar animaciones» no hay estela, ecos, resplandor, onda ni luz en toda la pantalla, el polvo queda quieto y las cápsulas no vuelan (la estática se mantiene, quieta).
    /// </summary>
    public sealed partial class RadarGameController : GameControllerBase
    {
        public const string GameId = RadarContract.GameId;

        private const float UnitsPerDp = 3f;
        private const float MarginU = 60f;

        /// <summary>SOLO EN EL EDITOR, para las capturas de pantalla (<c>verificar-todo.sh --capturas Radar</c>): con esta bandera el juego NO se juega solo y un guion (<see cref="EditorShotScript"/>) lo lleva por los momentos que se fotografían. En el teléfono es siempre false.</summary>
        public static bool EditorShotMode;

        private enum Phase { Idle, Watch, Flash, Mask, Answer, Reveal, Done }

        // ------------------------------------------------------------------ estado

        private System.Random _rng;
        private AdaptiveDifficulty _dda;
        private RadarRun _run = new RadarRun();
        private RadarRound _round;
        private Phase _phase = Phase.Idle;
        private bool _loopOn, _baked, _guided, _rescueTapped, _flashPaused;
        private int _bestRecord, _debugStage, _lastTickSecond, _previousFrameRate;
        private float _endsAt, _answerStartedAt, _actualMs;
        private readonly HashSet<CapsuleType> _picks = new HashSet<CapsuleType>();

        // los tiempos y el largo de la partida (el arranque de prueba del Editor los acorta para llegar al final)
        private int _roundsTotal = RadarContract.PrecisionRounds;
        private float _watchMin = RadarContract.WatchMinSeconds, _watchMax = RadarContract.WatchMaxSeconds, _revealSeconds = RadarContract.RevealSeconds, _retoSeconds = RadarContract.RetoSeconds;

        private ExitButton _exit;
        private GameHud _hud;
        private CountdownScreen _countdown;

        private bool Endless => _config != null && _config.config.timed;
        private float Now => GameClock.Time;
        private int RoundsTotal => _roundsTotal;

        // ------------------------------------------------------------------ sesión

        public void StartSession(SequenceInitConfig config)
        {
            _config = config;
            _rng = new System.Random();
#if UNITY_EDITOR
            // el smoke juega una partida corta (4 rondas, la última una lluvia) desde el nivel 7 (cuatro cápsulas y una roca)
            _roundsTotal = RadarContract.PrecisionRounds;
            if (GuidedTutorial.EditorAutoPlayGame && !EditorShotMode)
            {
                if (config.config.resc_stage <= 0) config.config.resc_stage = 7;
                _roundsTotal = 4;
                _watchMin = 0.5f;
                _watchMax = 1.0f;
                _revealSeconds = 1.0f;
                _retoSeconds = 16f;
            }
            else
            {
                _watchMin = RadarContract.WatchMinSeconds;
                _watchMax = RadarContract.WatchMaxSeconds;
                _revealSeconds = RadarContract.RevealSeconds;
                _retoSeconds = RadarContract.RetoSeconds;
            }
#endif
            _dda = RadarMetrics.CreateEngine(config.config);
            _bestRecord = Mathf.Max(0, config.config.resc_best);
            _debugStage = Mathf.Max(0, config.config.resc_stage);
            _run = new RadarRun();
            _phase = Phase.Idle;
            _loopOn = _guided = false;
            _picks.Clear();
            _lastTickSecond = -1;
            _endsAt = 0f;

            // Los destellos son de decenas de milisegundos: a 30 cuadros por segundo (lo que Android da por defecto) los saltos serían gruesos. Se piden 60 mientras dura el juego.
            _previousFrameRate = Application.targetFrameRate;
            Application.targetFrameRate = 60;

            ResetViews();
            _exit.Hide();
            _tutorial.Hide();
            SetTimer(1f);
            _timerTrack.gameObject.SetActive(false);
            _timerFill.gameObject.SetActive(false);
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
                // la ronda guiada se juega sobre la pantalla ya armada, antes de la cuenta regresiva
                _safe.gameObject.SetActive(true);
                yield return null;
                ApplySafeArea(_safe);
                Canvas.ForceUpdateCanvases();
                Layout();
                yield return StartCoroutine(RunTutorialIfNeeded());
                ResetViews();
            }
            _safe.gameObject.SetActive(false);
            yield return StartCoroutine(_countdown.Play(RadarContract.Title, Assessment.Subtitle(RadarContract.CountdownSub), () => _safe.gameObject.SetActive(true)));
            while (!_baked) yield return null;
            _safe.gameObject.SetActive(true);
            yield return null;
            ApplySafeArea(_safe);
            Canvas.ForceUpdateCanvases();
            Layout();
            _endsAt = GameClock.Time + _retoSeconds;
            _timerTrack.gameObject.SetActive(Endless);
            _timerFill.gameObject.SetActive(Endless);
            UpdateHud();
            _loopOn = true;
            yield return StartCoroutine(MainLoop());
        }

        /// <summary>El bucle de la partida: una ronda tras otra hasta que se cumplan las rondas (Precisión) o el tiempo (Reto). «Cómo se juega» lo retoma desde acá.</summary>
        private IEnumerator MainLoop()
        {
            while (!Finished()) yield return StartCoroutine(PlayRound());
            _loopOn = false;
            yield return StartCoroutine(FinishGame());
        }

        private bool Finished() => Endless ? GameClock.Time >= _endsAt : _run.RoundsPlayed >= _roundsTotal;

        /// <summary>Hornea los sprites y sintetiza los sonidos durante la cuenta regresiva, de a poco por cuadro (así nada se traba).</summary>
        private IEnumerator Prewarm()
        {
            if (_baked) yield break;
            var sprites = RadarSprites.Prewarm();
            while (sprites.MoveNext()) yield return null;
            AssignSprites();
            var sounds = RadarSounds.Prewarm();
            while (sounds.MoveNext()) yield return null;
            _baked = true;
            Layout();
        }

        // ------------------------------------------------------------------ una ronda

        private IEnumerator PlayRound()
        {
            bool rain = RadarContract.IsRainRound(_run.RoundsPlayed);
            int level = _dda.PresentedLevel;
            var round = rain ? RadarContract.RainRound(level, _rng) : RadarContract.NextRound(level, _rng);
            BeginRoundObjects(round);
            UpdateHud();
            yield return StartCoroutine(DoWatch(_watchMin + (_watchMax - _watchMin) * (float)_rng.NextDouble(), rain));
            yield return StartCoroutine(DoFlash());
            yield return StartCoroutine(DoMask());
            if (_flashPaused)
            {
                // una pausa en pleno destello lo anula: la ronda se repite sin contar
                SetMessage(RadarContract.RedoMessage, RadarContract.RedoHint, Gold, pill: true);
                yield return Wait(1.0f);
                _phase = Phase.Idle;
                HideRoundObjects();
                SetMessage("", "");
                yield break;
            }
            yield return StartCoroutine(DoAnswer());
            yield return StartCoroutine(DoReveal());
            HideRoundObjects();
        }

        /// <summary>Atento: el haz del radar gira y suena un «ping» por vuelta; el relámpago llega sin aviso.</summary>
        private IEnumerator DoWatch(float seconds, bool rain)
        {
            _phase = Phase.Watch;
            _beamOn = true;
            _picks.Clear();
            SetBoard(BoardMode.Dim);
            float t = 0f;
            if (rain)
            {
                SetMessage(RadarContract.RainMessage, "", Gold, pill: true);
                PlayClip(RadarSounds.Rain(), 0.7f);
                GameFeel.Haptic(GameFeel.HapticKind.Light);
                while (t < 1.4f) { t += GameClock.DeltaTime; yield return null; }
            }
            SetMessage(RadarContract.WatchMessage, "", Soft);
            while (t < seconds) { t += GameClock.DeltaTime; yield return null; }
        }

        /// <summary>El destello: se ven las cápsulas (y las rocas) durante los ms del nivel. Se mide la duración REAL (a 60 cuadros por segundo); una pausa en pleno destello lo anula.</summary>
        private IEnumerator DoFlash()
        {
            _phase = Phase.Flash;
            _beamOn = false;
            ShowObjects(1.06f);
            _flashAt = Now;
            _flashMs = _round.ExposureMs;
            PlayClip(RadarSounds.Flash(), 0.7f);
            if (Motion.Decorative) StartCoroutine(Flash(new Color(230f / 255f, 244f / 255f, 1f), 0.16f, 0.12f));          // 120 ms de luz suave en toda la pantalla; un solo destello por ronda
            SetMessage("", "");
            float shownAt = Time.unscaledTime;
            float startClock = GameClock.Time;
            float exposure = _round.ExposureMs / 1000f;
#if UNITY_EDITOR
            exposure = Mathf.Max(exposure, _editorFlashHold);
#endif
            bool paused = false;
            // Se apaga en el cuadro más cercano a la duración pedida (a 60 cuadros por segundo, pasos de ~17 ms).
            while (GameClock.Time - startClock + Time.unscaledDeltaTime * 0.5f < exposure)
            {
                if (GameClock.Paused) paused = true;
                yield return null;
            }
            _actualMs = (Time.unscaledTime - shownAt) * 1000f;
            _flashPaused = paused || GameClock.Paused;
            HideRoundObjects();
        }

        /// <summary>La estática (350 ms) dentro del disco: borra la imagen que queda en la retina. Con «quitar animaciones» se mantiene, quieta.</summary>
        private IEnumerator DoMask()
        {
            _phase = Phase.Mask;
            _mask.sprite = RadarSprites.Mask(_maskCount++);
            _mask.rectTransform.localRotation = Quaternion.Euler(0f, 0f, (float)_rng.NextDouble() * 360f);
            _mask.color = Color.white;
            _mask.gameObject.SetActive(true);
            PlayClip(RadarSounds.Static(), 0.5f);
            float t = 0f;
            while (t < RadarContract.MaskMs / 1000f) { t += GameClock.DeltaTime; yield return null; }
            _mask.gameObject.SetActive(false);
        }

        /// <summary>La respuesta: tocar un botón del tablero lo marca (de nuevo, lo desmarca; como máximo las que eran); «¡Rescatar!» se activa con 1 o más. Tope de espera: 25 s; después se evalúa lo que haya.</summary>
        private IEnumerator DoAnswer()
        {
            _phase = Phase.Answer;
            _picks.Clear();
            _rescueTapped = false;
            _answerStartedAt = Now;
#if UNITY_EDITOR
            _botPlan = null;
            _botT = 0f;
#endif
            SetBoard(BoardMode.Live);
            SetMessage(RadarContract.AskMessage(_round.Count), RadarContract.AskHint, Cyan);
            while (!_rescueTapped && Now - _answerStartedAt < RadarContract.AnswerTimeoutSeconds) yield return null;
        }

        /// <summary>La revelación (2,3 s): lo que pasó, con el rayo tractor, los avisos del tablero y el mensaje de la franja.</summary>
        private IEnumerator DoReveal()
        {
            _phase = Phase.Reveal;
            var answer = RadarContract.Evaluate(_round, _picks);
            var types = _round.Types();
            _hitSet.Clear();
            _wrongSet.Clear();
            _missedSet.Clear();
            foreach (var p in _picks) { if (types.Contains(p)) _hitSet.Add(p); else _wrongSet.Add(p); }
            foreach (var t in types) if (!_picks.Contains(t)) _missedSet.Add(t);
            bool perfect = RadarContract.Perfect(_round.Count, answer.Hits, answer.Extras);
            if (!_guided)
            {
                var outcome = _run.Add(_round, answer.Hits, answer.Extras, _actualMs);
                if (!_round.Rain) _dda.Register(outcome.Success);          // las lluvias quedan fuera del motor
#if UNITY_EDITOR
                Debug.Log("[SmokeTest] Radar: ronda " + _run.RoundsPlayed + (_round.Rain ? " (lluvia)" : " nivel " + _round.Level) + ": " + answer.Hits + " de " + _round.Count + ", " + answer.Extras + " de más, destello real " + _actualMs.ToString("0") + " ms de " + _round.ExposureMs + ", racha " + _run.Streak);
#endif
            }
            UpdateHud();
            _revealAt = Now;
            _lifts.Clear();
            _drifting.Clear();
            ShowObjects(1f);
            int slot = 0;
            for (int i = 0; i < _round.Capsules.Length; i++)
            {
                var o = _round.Capsules[i];
                if (_hitSet.Contains(o.Type))
                {
                    _lifts.Add(new Lift { Index = i, Slot = slot, Type = o.Type, FromX = o.X, FromY = o.Y, Tilt = o.Tilt, At = Now + 0.2f + slot * 0.16f });
                    _flying[slot].sprite = RadarSprites.CapsuleSprite(o.Type);
                    slot++;
                }
                else
                {
                    _drifting.Add(i);
                    _capRings[i].gameObject.SetActive(true);
                }
            }
            SetBoard(BoardMode.Reveal);
            int streak = _run.Streak;
            if (perfect)
            {
                SetMessage(RadarContract.AllSafe(streak), "", Lime, pill: true);
                PlayClip(RadarSounds.Chime(), 0.8f);
                GameFeel.Haptic(GameFeel.HapticKind.Firm);
            }
            else
            {
                SetMessage(RadarContract.Partial(answer.Hits, _round.Count, answer.Extras), "", Gold, pill: true);
                PlayClip(RadarSounds.Thud(), 0.7f);
                GameFeel.Haptic(GameFeel.HapticKind.Double);
            }
            yield return Wait(_revealSeconds);
            foreach (var b in _beams) b.gameObject.SetActive(false);
            foreach (var f in _flying) f.gameObject.SetActive(false);
            foreach (var sp in _sparks) sp.gameObject.SetActive(false);
            foreach (var l in _lifts) if (!l.Landed) Land(l, Now);          // si la revelación se cortó antes de que subieran todas, igual quedan a salvo
            _lifts.Clear();
            _drifting.Clear();
            SetBoard(BoardMode.Dim);
            SetMessage("", "");
            _phase = Phase.Idle;
        }

        // ------------------------------------------------------------------ fin

        private IEnumerator FinishGame()
        {
            _phase = Phase.Done;
            int record = Mathf.Max(_bestRecord, _run.Rescued);
            bool broke = _run.Rescued > _bestRecord;
#if UNITY_EDITOR
            Debug.Log("[SmokeTest] Radar: partida terminada: " + _run.Rescued + " cápsulas, " + _run.Perfect + " rondas perfectas de " + _run.RoundsPlayed + ", racha mayor " + _run.BestStreak + ", vistazo " + _run.GlanceMs + " ms, captura " + _run.Capture.ToString("0.0") + ", nivel " + _dda.Level);
#endif
            HideRoundObjects();
            ShowEnd(record, broke);
            _exit.Show();
            PlayClip(RadarSounds.Finale(), 0.8f);
            var telemetry = new StroopTelemetry
            {
                user_id = _config.user_id,
                game_id = _config.game_id,
                session_metrics = RadarMetrics.Build(_dda, _run, record, broke, _config.config)
            };
            NativeBridge.ForwardTelemetryToPlatform(JsonUtility.ToJson(telemetry));
            yield break;
        }

        // ------------------------------------------------------------------ pieza del tutorial

        /// <summary>El tutorial guiado común sobre el área segura (se llama al final de <c>BuildUi</c>, para que quede encima de todo).</summary>
        private void SetUpTutorial() =>
            BuildTutorial(_safe, GameHud.Height + 10f, RadarContract.Title, "Un relámpago muestra unas cápsulas de escape: elige en el tablero cuáles viste y rescátalas.", badgeAtBottom: true);

        // ------------------------------------------------------------------ «Cómo se juega» desde la pausa

        protected override bool HowToReady => _loopOn && _phase != Phase.Done && _phase != Phase.Idle;

        protected override void HowToSuspend()
        {
            // la ronda a medias no cuenta: se vuelve a jugar entera (lo ganado hasta ahora se conserva)
            _phase = Phase.Idle;
            _beamOn = true;
            _picks.Clear();
            HideRoundObjects();
            _mask.gameObject.SetActive(false);
            _lifts.Clear();
            _drifting.Clear();
            foreach (var b in _beams) b.gameObject.SetActive(false);
            foreach (var f in _flying) f.gameObject.SetActive(false);
            foreach (var sp in _sparks) sp.gameObject.SetActive(false);
            SetBoard(BoardMode.Dim);
            SetMessage("", "");
        }

        protected override void HowToResume(float spentSeconds)
        {
            if (Endless) _endsAt += spentSeconds;                // el tiempo del tutorial no se le descuenta al Reto
            _timerTrack.gameObject.SetActive(Endless);
            _timerFill.gameObject.SetActive(Endless);
            Layout();
            UpdateHud();
            RefreshSeats();
            StartCoroutine(MainLoop());
        }

        // ------------------------------------------------------------------ entrada

        private void Update()
        {
            if (PollTutorialSkip()) return;             // un toque en «Saltar tutorial» no es un toque al juego
            float now = GameClock.Time, dt = GameClock.DeltaTime;
            UpdateClock();
            ReadInput();
            Animate(now, dt);
#if UNITY_EDITOR
            if (_phase == Phase.Answer && !_guided && dt > 0f) AutoPlay(dt);
            GuardNotices();
#endif
        }

        private void UpdateClock()
        {
            if (!Endless || _endsAt <= 0f || _guided || !_loopOn || _phase == Phase.Done) return;
            float left = _endsAt - GameClock.Time;
            SetTimer(left / _retoSeconds);
            int whole = Mathf.CeilToInt(left);
            if (whole <= 5 && whole >= 1 && whole != _lastTickSecond)
            {
                _lastTickSecond = whole;
                GameFeel.Tick();
            }
        }

        private void ReadInput()
        {
            if (!GuidedTutorial.TryPress(out Vector2 pos)) return;
            if (_tutorial != null && _tutorial.Coach != null && _tutorial.Coach.Blocks(pos)) return;
            if (GameClock.DeltaTime <= 0f) return;                         // en pausa (o con un foco del tutorial) los toques los recibe el paso, no el juego
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(_play, pos, null, out var local)) return;
            var p = ToLogical(local);
            HandlePress(p.x, p.y);
        }

        /// <summary>Un toque en la respuesta: un botón del tablero lo marca o desmarca; «¡Rescatar!» entrega lo elegido (con 1 o más).</summary>
        private void HandlePress(float x, float y)
        {
            if (_phase != Phase.Answer || _boardMode != BoardMode.Live) return;
            for (int i = 0; i < _buttons.Length; i++)
            {
                var (cx, cy) = _plan.CellCenter(i);
                if (Mathf.Abs(x - cx) <= _plan.CellW * 0.5f && Mathf.Abs(y - cy) <= _plan.CellH * 0.5f) { ToggleType(RadarContract.BoardOrder[i], i); return; }
            }
            if (_picks.Count > 0 && _plan.GoBox.Intersects(new RadarBox(x - 1f, y - 1f, x + 1f, y + 1f))) PressGo();
        }

        private void ToggleType(CapsuleType type, int cell)
        {
            _pressAt[cell] = Now;
            if (_picks.Contains(type))
            {
                _picks.Remove(type);
                PlayClip(RadarSounds.Pop(), 0.7f);
            }
            else if (_picks.Count < _round.Count)
            {
                _picks.Add(type);
                PlayClip(RadarSounds.Mark(_picks.Count), 0.8f);
                GameFeel.Haptic(GameFeel.HapticKind.Light);
            }
            else return;                                                   // ya están todas las que eran: para cambiar una, primero se quita otra
            RefreshBoard(Now);
        }

        private void PressGo()
        {
            if (_picks.Count == 0) return;
            _goPressAt = Now;
            _rescueTapped = true;
        }

        private void PlayClip(AudioClip clip, float volume)
        {
            if (clip == null || !GameFeel.SoundOn) return;
            if (_config != null && _config.config != null && !_config.config.sound_enabled) return;
            _audioSource.PlayOneShot(clip, volume);
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

#if UNITY_EDITOR
        private float _editorFlashHold, _botT;
        private List<CapsuleType> _botPlan;
        private int _guardReports;

        /// <summary>SOLO EN EL EDITOR (smoke): juega solo. En la respuesta marca las cápsulas que eran (una ronda de cada tres cambia la última por una que no estaba, para pasar por el aspa y el «de más») y toca «¡Rescatar!». En el teléfono no hace nada.</summary>
        private void AutoPlay(float dt)
        {
            if (EditorShotMode || !GuidedTutorial.EditorAutoPlayGame || _round == null) return;
            if (_botPlan == null)
            {
                _botPlan = new List<CapsuleType>(_round.Types());
                if (_run.RoundsPlayed % 3 == 1 && _botPlan.Count > 0)
                    foreach (var t in RadarContract.BoardOrder)
                        if (!_round.Types().Contains(t)) { _botPlan[_botPlan.Count - 1] = t; break; }
                _botT = 0f;
            }
            _botT += dt;
            if (_botT < 0.3f) return;
            _botT = 0f;
            if (_botPlan.Count > 0)
            {
                var t = _botPlan[0];
                _botPlan.RemoveAt(0);
                int cell = System.Array.IndexOf(RadarContract.BoardOrder, t);
                ToggleType(t, cell);
            }
            else if (_picks.Count > 0) PressGo();
        }

        /// <summary>SOLO EN EL EDITOR: el pedido de Ricardo hecho prueba. La franja de mensajes (con sus textos y su píldora) nunca toca el radar, la nave, el tablero ni «¡Rescatar!»: si se cruzan, el smoke falla (cualquier error de consola lo hace).</summary>
        private void GuardNotices()
        {
            if (_guardReports >= 3 || _s <= 0f || _phase == Phase.Done) return;
            var texts = new List<RectTransform>();
            if (!string.IsNullOrEmpty(_msgA.text)) texts.Add(_msgA.rectTransform);
            if (!string.IsNullOrEmpty(_msgB.text)) texts.Add(_msgB.rectTransform);
            if (_msgPill.gameObject.activeSelf) texts.Add(_msgPill.rectTransform);
            var zones = new[] { ("el radar", _plan.RadarBox), ("la nave", _plan.ShipBox), ("el tablero", _plan.BoardBox), ("«¡Rescatar!»", _plan.GoBox) };
            foreach (var rt in texts)
            {
                var c = ToLogical(rt.anchoredPosition);
                var box = new RadarBox(c.x - rt.sizeDelta.x / _s * 0.5f, c.y - rt.sizeDelta.y / _s * 0.5f, c.x + rt.sizeDelta.x / _s * 0.5f, c.y + rt.sizeDelta.y / _s * 0.5f);
                foreach (var z in zones)
                    if (box.Intersects(z.Item2))
                    {
                        _guardReports++;
                        Debug.LogError("[SmokeTest] Radar: un aviso tapa " + z.Item1 + " (aviso de " + Mathf.RoundToInt(box.X0) + "," + Mathf.RoundToInt(box.Y0) + " a " + Mathf.RoundToInt(box.X1) + "," + Mathf.RoundToInt(box.Y1) + ")");
                        return;
                    }
            }
        }
#endif
    }
}
