using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using NeuroVida.Bridge;
using NeuroVida.Contracts;
using NeuroVida.Games.Shared;
using static NeuroVida.Games.Shared.UiKit;
using Motion = NeuroVida.Games.Shared.Motion; // UnityEngine.Motion también existe

namespace NeuroVida.Games.Parejas
{
    /// <summary>
    /// «Constelaciones» (id <c>parejas</c>, antes «Parejas Ocultas»; ver <see cref="ConstelacionContract"/> y docs/diseno-constelaciones.md; el boceto aprobado es docs/previews/constelaciones-boceto.html, versión 2).
    /// Un cielo nocturno: cada luz esconde un objeto del espacio; se dan vuelta dos (o tres) y si son iguales quedan unidas por una línea de luz, DORADA continua si fuiste directo a una compañera YA VISTA (de memoria) o
    /// CELESTE punteada si la luz era nueva (a la primera vista: suerte). Las reglas están en <see cref="ConstelacionSky"/> (copiadas del boceto): oportunidad de memoria, acierto, «se te escapó» (la compañera brilla sin
    /// darse vuelta), racha de memoria. NADA bloquea el toque: las abiertas se cierran a los 850 ms o al tocar otra luz, y cada luz se puede tocar apenas aparece. El tablero está siempre completo a la vez (regla de patentes).
    /// Precisión = 6 cielos; Reto = hasta que se acaben los 180 s (las tarjetas «NUEVO» no gastan tiempo). Todo lo que se mueve va con resortes (<see cref="ConstelacionMotion"/>); con «quitar animaciones»: sin rebotes,
    /// partículas ni estela, las líneas aparecen completas, el brillo de pista es fijo y la etiqueta no sube.
    /// </summary>
    public sealed partial class ConstelacionGameController : GameControllerBase
    {
        public const string GameId = ConstelacionContract.GameId;

        private const float UnitsPerDp = 3f;
        private const float Su = 3f;                 // unidades del cielo por dp (el cielo se escala entero sobre esto)
        private const float MarginU = 60f;

        private enum Phase { Idle, Intro, Play, BoardEnd, Leaving, Done }

        // ------------------------------------------------------------------ estado

        private System.Random _rng;
        private AdaptiveDifficulty _dda;
        private ConTally _tally;
        private ConstelacionSky _sky;
        private Phase _phase = Phase.Idle;
        private bool Endless => _config != null && _config.config.timed;
        private bool _senior, _loopOn, _guided, _inputOn, _baked, _aborted, _recordNow, _recordBroken;
        private int _boardNo, _level, _streak, _bestStreak, _bestRecord, _debugStage;
        private float _endsAt, _introAt, _leaveAt;
        private int _lastTickSecond = -1;
        private bool _introTapped;
        private Vector2? _press;
        private ConIntro _introKind;
        private float _hintDueAt = -1f;
        private List<ConLight> _hintLights = new List<ConLight>();
        private float _msgAt = -10f;

        // escala y disposición
        private float _s = 3f, _playW = 1080f, _playH = 1920f, _logicalH = 640f;
        private ConstelacionLayout.Metrics _lay;

        private ExitButton _exit;
        private GameHud _hud;
        private CountdownScreen _countdown;

        private float NowMs => GameClock.Time * 1000f;

        // ------------------------------------------------------------------ sesión

        public void StartSession(SequenceInitConfig config)
        {
            _config = config;
            _rng = new System.Random();
            var age = DdaUserProfileConfig.ParseAgeBand(config.config.age_band);
            _senior = age == AgeBand.Senior;
            _dda = ConstelacionContract.CreateEngine(config.config);
            _tally = new ConTally();
            _bestRecord = Mathf.Max(0, config.config.con_best);
            _debugStage = Mathf.Max(0, config.config.con_stage);

            _phase = Phase.Idle;
            _loopOn = _guided = _inputOn = _aborted = false;
            _press = null;
            _boardNo = _streak = _bestStreak = 0;
            _endsAt = 0f;
            _lastTickSecond = -1;
            _recordNow = _recordBroken = false;
            _hintDueAt = -1f;
            _hintLights.Clear();
            _sky = null;
            ResetBoardViews();

            _resultRoot.gameObject.SetActive(false);
            _exit.Hide();
            _timerTrack.gameObject.SetActive(Endless);
            _hud.SetStreak(0);
            _tutorial.Hide();
            _introLayer.gameObject.SetActive(false);

            StopAllCoroutines();
            StartCoroutine(GameLoop());
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
                ResetBoardViews();
            }
            _safe.gameObject.SetActive(false);
            yield return StartCoroutine(_countdown.Play("Constelaciones", Assessment.Subtitle("Encuentra las parejas y únelas con luz"), () => _safe.gameObject.SetActive(true)));
            while (!_baked) yield return null;
            _safe.gameObject.SetActive(true);
            yield return null;
            ApplySafeArea(_safe);
            Canvas.ForceUpdateCanvases();
            Layout();
            UpdateHud();

            _endsAt = GameClock.Time + ConstelacionContract.RetoSeconds;
            _loopOn = true;
            yield return StartCoroutine(MainLoop());
        }

        /// <summary>El bucle de la partida: un cielo tras otro hasta que se acabe el tiempo (Reto) o los 6 (Precisión). «Cómo se juega» lo retoma desde acá.</summary>
        private IEnumerator MainLoop()
        {
            while (!Finished()) yield return StartCoroutine(PlayBoard());
            _loopOn = false;
            yield return StartCoroutine(FinishGame());
        }

        private bool Finished() => Endless ? GameClock.Time >= _endsAt : _tally.Boards >= ConstelacionContract.Boards;

        /// <summary>El Reto se acabó (solo cuenta con la partida en marcha).</summary>
        private bool Over => Endless && _loopOn && GameClock.Time >= _endsAt;

        /// <summary>Hornea los sprites y sintetiza los sonidos durante la cuenta regresiva, de a poco por cuadro (así nada se traba).</summary>
        private IEnumerator Prewarm()
        {
            if (!_baked)
            {
                var sprites = ConstelacionSprites.Prewarm();
                while (sprites.MoveNext()) yield return null;
                AssignSprites();
                _baked = true;
            }
            var sounds = ConstelacionSounds.Prewarm();
            while (sounds.MoveNext()) yield return null;
        }

        // ------------------------------------------------------------------ un cielo

        /// <summary>Un cielo: se arma, salen las tarjetas «NUEVO» que toquen, las luces aparecen escalonadas y se juega hasta que todas quedan unidas. Si se acaba el tiempo del Reto a mitad, lo ya hecho cuenta.</summary>
        private IEnumerator PlayBoard()
        {
            _boardNo++;
            _level = _boardNo == 1 && _debugStage > 0 ? Mathf.Clamp(_debugStage, 1, ConstelacionContract.MaxLevel) : _dda.PresentedLevel;
            var stage = ConstelacionContract.Stage(_level);
            _sky = ConstelacionSky.Create(stage, _rng, ConstelacionLayout.SkyW, _lay.SkyH, NowMs, _streak, _bestStreak, _senior ? 1.35f : 1f);
            BindBoard(_sky);
            UpdateHud();
            _phase = Phase.Idle;
            foreach (var intro in ConstelacionContract.IntrosFor(_level))
            {
                if (IntroSeen(intro)) continue;
                float before = GameClock.Time;
                yield return StartCoroutine(ShowIntro(intro));
                _endsAt = HowToClock.Shift(_endsAt, GameClock.Time - before);       // la tarjeta «NUEVO» no gasta tiempo del Reto
                MarkIntroSeen(intro);
            }
            _aborted = false;
            BeginSky();
            float waitedTap = 0f;
            while (!_sky.Complete)
            {
                if (Over) { _aborted = true; break; }
                waitedTap += GameClock.RealDeltaTime;
                if (_phase == Phase.Play && GuidedTutorial.AutoPlayGame(waitedTap)) { waitedTap = 1.15f; AutoPlayTap(); }          // solo en el smoke del Editor
                yield return null;
            }
            _inputOn = false;
            if (_aborted)
            {
                _tally.AddBoard(_sky, _level);
                _streak = _sky.Streak;
                _bestStreak = _sky.BestStreak;
                _phase = Phase.Idle;
                yield break;
            }
            yield return Motion.Hold(0.65f);                    // la última línea termina de trazarse
            yield return StartCoroutine(EndBoard());
        }

        /// <summary>Las luces aparecen escalonadas (45 ms entre una y otra) y desde ese momento se puede tocar.</summary>
        private void BeginSky()
        {
            _sky.RestartAppearance(NowMs);
            _phase = Phase.Play;
            _inputOn = true;
            _press = null;
            _msgAt = -10f;
            PlayClip(ConstelacionSounds.Hum(), 0.5f);
        }

        /// <summary>Fin de un cielo: «¡Constelación completa!», el arpegio, las líneas se iluminan una por una (queda a la vista qué parte armaste con la memoria) y las luces se encogen.</summary>
        private IEnumerator EndBoard()
        {
            _phase = Phase.BoardEnd;
            _inputOn = false;
            _tally.AddBoard(_sky, _level);
            _streak = _sky.Streak;
            _bestStreak = _sky.BestStreak;
            _recordNow = false;
            if (_bestStreak > _bestRecord && _sky.BestStreak > 0)
            {
                _bestRecord = _bestStreak;
                _recordNow = true;
                _recordBroken = true;
            }
            _boardDoneAt = GameClock.Time;
            PlayClip(ConstelacionSounds.BoardEnd(), 0.8f);
            float now = NowMs;
            for (int i = 0; i < _sky.Links.Count; i++) _linkGlowAt[_sky.Links[i]] = now + 300f + i * 90f;
            UpdateHud();
            float slow = _senior ? 1.35f : 1f;
            yield return Motion.Hold((Motion.Decorative ? 2.6f : 1.8f) * slow);
            _phase = Phase.Leaving;
            _leaveAt = GameClock.Time;
            yield return Motion.Hold(Motion.Decorative ? ConstelacionMotion.LeaveSeconds + 0.06f : 0.05f);
            _phase = Phase.Idle;
        }

        private float _boardDoneAt = -10f;

        // ------------------------------------------------------------------ un toque

        /// <summary>Un toque en la luz <paramref name="id"/>: las reglas son de <see cref="ConstelacionSky.Tap"/>; acá se muestra lo que pasó, se suena y se cuenta para el motor común.</summary>
        private ConTapResult TapLight(int id)
        {
            if (_sky == null) return new ConTapResult();
            float nowMs = NowMs, now = GameClock.Time;
            var res = _sky.Tap(id, nowMs);
            if (!res.Accepted) return res;
            if (res.ClosedNow.Count > 0) PlayClip(ConstelacionSounds.FlipDown(), 0.5f);
            var v = _lv[id];
            v.PressAt = now;
            PlayClip(ConstelacionSounds.FlipUp(), 0.5f);
            PlayClip(ConstelacionSounds.Note(_sky.Lights[id].Group), 0.35f);
            if (res.Opportunity && !_guided)
            {
                _dda.Register(res.Hit);                           // cada oportunidad de memoria es un ensayo
                _streak = _sky.Streak;
                _hud.SetStreak(_sky.Streak);
                UpdateHud();
            }
            else if (res.Opportunity) _hud.SetStreak(_sky.Streak);
            if (res.Matched)
            {
                if (res.Completed.Count > 0) OnGroupDone(res);
                else if (res.ThirdMissing)
                {
                    var a = _sky.Face[0];
                    AddFloat(MidOf(a, _sky.Face[1]), ConstelacionContract.FloatThird, new Color(1f, 231f / 255f, 168f / 255f));
                    PlayClip(ConstelacionSounds.Note(a.Group), 0.3f);
                }
            }
            else if (res.Pending.Count > 0)
            {
                // no son iguales: quedan abiertas un momento (o hasta el próximo toque)
                PlayClip(ConstelacionSounds.Soft(), 0.5f);
                if (res.Miss)
                {
                    _hintLights = res.Kin;
                    _hintDueAt = now + ConstelacionContract.HintDelayMs / 1000f;
                    _msgAt = now;
                }
            }
            return res;
        }

        private void OnGroupDone(ConTapResult res)
        {
            var f = res.Completed;
            foreach (var l in res.NewLinks) AddLinkView(l);
            var mid = MidOf(f);
            if (res.CompletedByMemory)
            {
                AddFloat(mid, ConstelacionContract.FloatMemory, new Color(1f, 201f / 255f, 74f / 255f));
                Emit(mid, 22, 140f, 0.7f, new Color(1f, 201f / 255f, 74f / 255f), 20f);
                PlayClip(ConstelacionSounds.FoundMemory(f[0].Group), 0.8f);
                PlayClip(ConstelacionSounds.Spark(Mathf.Max(1, _sky.Streak)), 0.6f);
            }
            else
            {
                AddFloat(mid, f.Count == 3 ? ConstelacionContract.FloatTrio : ConstelacionContract.FloatFound, new Color(191f / 255f, 233f / 255f, 1f));
                Emit(mid, 10, 80f, 0.5f, new Color(191f / 255f, 233f / 255f, 1f), 20f);
                PlayClip(ConstelacionSounds.FoundNew(f[0].Group), 0.7f);
            }
        }

        /// <summary>El punto medio de las luces (en dp lógicos de pantalla): donde flota la etiqueta.</summary>
        private Vector2 MidOf(params ConLight[] lights) => MidOf((IList<ConLight>)lights);

        private Vector2 MidOf(IList<ConLight> lights)
        {
            float x = 0f, y = 0f;
            foreach (var l in lights) { x += l.Pos.X; y += l.Pos.Y; }
            return SkyToLogical(x / lights.Count, y / lights.Count - 10f);
        }

        /// <summary>SOLO EN EL EDITOR (smoke): juega solo, un toque cada 0,35 s: explora luces nuevas y, si ya vio una compañera, va directo a ella (de vez en cuando se «olvida» y toca otra: así el arranque pasa por los
        /// aciertos, los «se te escapó» y las líneas de los dos tipos). En el teléfono no hace nada.</summary>
        private void AutoPlayTap()
        {
#if UNITY_EDITOR
            var down = _sky.Lights.Where(l => l.State == ConState.Down && CanPick(l)).ToList();
            if (down.Count == 0) return;
            ConLight pick;
            if (_sky.Face.Count == 0)
            {
                var fresh = down.Where(l => !l.Seen).ToList();
                pick = fresh.Count > 0 ? fresh[_rng.Next(fresh.Count)] : down[_rng.Next(down.Count)];
            }
            else
            {
                var first = _sky.Face[0];
                var kin = down.Where(l => l.Key == first.Key && l.Seen).ToList();
                _botTurns++;
                if (kin.Count > 0 && _botTurns % 4 != 0) pick = kin[0];
                else
                {
                    var other = down.Where(l => l.Key != first.Key).ToList();
                    pick = other.Count > 0 ? other[_rng.Next(other.Count)] : down[0];
                }
            }
            Debug.Log("[SmokeTest] Constelaciones: cielo " + _boardNo + " (etapa " + _level + "), toca la luz " + pick.Id + (pick.Seen ? " (ya vista)" : " (nueva)"));
            TapLight(pick.Id);
#endif
        }

#if UNITY_EDITOR
        private int _botTurns;
        private bool CanPick(ConLight l) => _sky.CanTap(l.Id, NowMs);
#endif

        // ------------------------------------------------------------------ resultado

        private IEnumerator FinishGame()
        {
            _phase = Phase.Done;
            _inputOn = false;
            ResetBoardViews();
            int score = ConstelacionContract.Score(_tally.Hits, _tally.Opportunities);
            ShowResult(score);
            PlayClip(ConstelacionSounds.Finale(), 0.8f);
            int record = ConstelacionContract.NewRecord(_bestRecord, _tally.BestStreak);
            var telemetry = new CardsTelemetry
            {
                user_id = _config.user_id,
                game_id = _config.game_id,
                session_metrics = ConstelacionContract.BuildMetrics(_dda, _tally, record, _recordBroken, score, _config.config)
            };
            NativeBridge.ForwardTelemetryToPlatform(JsonUtility.ToJson(telemetry));
            yield break;
        }

        private void ShowResult(int score)
        {
            _exit.Show();
            _boardRoot.gameObject.SetActive(false);
            _cardLayer.gameObject.SetActive(false);
            _rowLayer.gameObject.SetActive(false);
            _fxLayer.gameObject.SetActive(false);
            _resultRoot.Find("Title").GetComponent<Text>().text = "¡Buen trabajo!";
            _resultRoot.Find("Detail").GetComponent<Text>().text = _tally.Opportunities > 0
                ? $"{_tally.Hits} de {_tally.Opportunities} veces fuiste directo a una pareja que ya habías visto"
                : ConstelacionContract.LuckyLine;
            _resultRoot.Find("Extra").GetComponent<Text>().text = _tally.BestStreak > 0
                ? $"Racha de memoria más larga: {_tally.BestStreak}" + (_bestRecord > 0 ? $" · tu mejor racha: {Mathf.Max(_bestRecord, _tally.BestStreak)}" : "")
                : "Lo encontrado por suerte no cuenta";
            _resultRoot.gameObject.SetActive(true);
            StartCoroutine(AnimateResult(score));
        }

        private void UpdateHud()
        {
            _hud.SetLevel(_dda != null ? _dda.PresentedLevel : 1);
            _hud.SetInfo(Endless ? "Cielo " + Mathf.Max(1, _boardNo) : ConstelacionContract.SkyOf(Mathf.Clamp(_boardNo, 1, ConstelacionContract.Boards), ConstelacionContract.Boards));
        }

        // ------------------------------------------------------------------ tarjetas «NUEVO» (una sola vez por instalación cada una: se guardan en las preferencias de Unity)

        private const string IntroKey = "con_intros";

        private static bool IntroSeen(ConIntro intro)
        {
            try { return (PlayerPrefs.GetInt(IntroKey, 0) & (1 << (int)intro)) != 0; }
            catch (System.Exception) { return false; }
        }

        private static void MarkIntroSeen(ConIntro intro)
        {
            try { PlayerPrefs.SetInt(IntroKey, PlayerPrefs.GetInt(IntroKey, 0) | (1 << (int)intro)); PlayerPrefs.Save(); }
            catch (System.Exception) { }
        }

        private IEnumerator ShowIntro(ConIntro intro)
        {
            _phase = Phase.Intro;
            _introTapped = false;
            _introKind = intro;
            _introTag.text = ConstelacionContract.IntroTag(intro);
            _introLayer.gameObject.SetActive(true);
            LayoutIntro(ConstelacionContract.IntroLines(intro));
            _introAt = GameClock.Time;
            PlayClip(ConstelacionSounds.Chime(), 0.7f);
            _inputOn = true;
            _press = null;
            float waited = 0f;
            while (!_introTapped)
            {
                waited += GameClock.RealDeltaTime;
                if (GuidedTutorial.AutoPlayGame(waited)) _introTapped = true;          // solo en el smoke del Editor
                yield return null;
            }
            _inputOn = false;
            _introLayer.gameObject.SetActive(false);
        }

        /// <summary>El tutorial guiado común sobre el área segura (se llama al final de <c>BuildUi</c>, para que quede encima de todo).</summary>
        private void SetUpTutorial() =>
            BuildTutorial(_safe, GameHud.Height + 10f, "Constelaciones", "Cada luz esconde un objeto. Da vuelta dos: si son iguales, quedan unidas con luz.", badgeAtBottom: true);

        // ------------------------------------------------------------------ «Cómo se juega» desde la pausa

        protected override bool HowToReady => _loopOn && _phase != Phase.Done;

        protected override void HowToSuspend()
        {
            if (_sky != null && !_sky.Complete && _phase != Phase.BoardEnd && _phase != Phase.Leaving) _boardNo--;      // el cielo a medias se vuelve a jugar entero
            _phase = Phase.Idle;
            _inputOn = false;
            _introLayer.gameObject.SetActive(false);
            _sky = null;
            ResetBoardViews();
        }

        protected override void HowToResume(float spentSeconds)
        {
            _endsAt = HowToClock.Shift(_endsAt, spentSeconds);       // el tiempo que duró «Cómo se juega» no se le descuenta al Reto
            StartCoroutine(MainLoop());
        }

        // ------------------------------------------------------------------ entrada

        private void Update()
        {
            if (PollTutorialSkip()) return;             // un toque en «Saltar tutorial» no es un toque al juego
            float dt = GameClock.DeltaTime;
            float now = GameClock.Time;
            UpdateClock();
            if (_sky != null)
            {
                var closed = _sky.Tick(NowMs);                          // las abiertas que no coinciden se cierran solas a los 850 ms
                if (closed.Count > 0) PlayClip(ConstelacionSounds.FlipDown(), 0.5f);
                if (_hintDueAt > 0f && now >= _hintDueAt)
                {
                    _hintDueAt = -1f;
                    foreach (var k in _hintLights) if (k.State == ConState.Down) { _lv[k.Id].HintAt = now; }
                    PlayClip(ConstelacionSounds.Chime(), 0.25f);
                }
            }
            AnimateBoard(now, dt);
            AnimateCard(now);
            AnimateRow(now);
            AnimateFloats(now);
            AnimateSparks(dt);
            AnimateIntro(now);
            if (_inputOn && dt > 0f)
            {
                ReadPress();
                HandlePress();
            }
        }

        private void ReadPress()
        {
            if (!GuidedTutorial.TryPress(out Vector2 pos)) return;
            if (_tutorial != null && _tutorial.Coach != null && _tutorial.Coach.Blocks(pos)) return;
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(_play, pos, null, out var local)) return;
            _press = ToLogical(local);
        }

        private void HandlePress()
        {
            if (!_press.HasValue) return;
            var p = _press.Value;
            _press = null;
            if (_phase == Phase.Intro)
            {
                if (GameClock.Time - _introAt > 0.5f) _introTapped = true;
                return;
            }
            if (_phase != Phase.Play || _sky == null) return;
            var s = LogicalToSky(p);
            int id = ConstelacionLayout.Nearest(_sky.Placement.Points, _sky.Placement.R, s.x, s.y);
            if (id >= 0) OnLightPressed(id);
        }

        /// <summary>Un toque en una luz. En la ronda guiada puede estar acotado a la luz que se pide.</summary>
        private void OnLightPressed(int id)
        {
            if (_guided && _guidedOnly >= 0 && id != _guidedOnly)
            {
                _guidedMisses++;
                return;
            }
            var res = TapLight(id);
            if (_guided && res.Accepted && id == _guidedOnly) _guidedHit = true;
        }

        private int _guidedOnly = -1, _guidedMisses;
        private bool _guidedHit;

        private void UpdateClock()
        {
            if (!Endless || !_loopOn || _endsAt <= 0f || _phase == Phase.Done) return;
            float left = _endsAt - GameClock.Time;
            float frac = Mathf.Clamp01(left / ConstelacionContract.RetoSeconds);
            _timerFill.rectTransform.anchorMax = new Vector2(frac, 1f);
            _timerHead.anchorMin = _timerHead.anchorMax = new Vector2(frac, 0.5f);
            int whole = Mathf.CeilToInt(left);
            if (whole <= 5 && whole >= 1 && whole != _lastTickSecond)
            {
                _lastTickSecond = whole;
                GameFeel.Tick();
            }
        }

        private void PlayClip(AudioClip clip, float volume)
        {
            if (clip == null || !GameFeel.SoundOn) return;
            if (_config != null && _config.config != null && !_config.config.sound_enabled) return;
            _audioSource.PlayOneShot(clip, volume);
        }
    }
}
