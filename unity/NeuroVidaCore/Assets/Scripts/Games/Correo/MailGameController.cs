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

namespace NeuroVida.Games.Correo
{
    /// <summary>
    /// «La estación de correo» (id <c>correo</c>, antes «Correo Estelar», el vuelo; ver <see cref="MailContract"/> y docs/diseno-correo-estacion.md; el boceto aprobado es docs/previews/correo-estacion-boceto.html, versión 2).
    /// Las cartas llegan por una cinta y se tocan en el buzón del planeta de su sello (la tarea de fondo); los ENCARGOS del día se dan en la hoja de la mañana y no se ven durante el día: por evento («si llega una carta con
    /// sello dorado o con lazo, a la caja fuerte») y por hora («al mediodía, enciende el faro», con el reloj tapado que se mira 1,6 s). Un día dura 50 s; una partida, 4 días, cada uno con su resumen. Las reglas están en
    /// <see cref="MailDay"/> y <see cref="MailRun"/> (copiadas del boceto). El faro encendido a tiempo guía la nave del correo (luz DETRÁS de la estación, nave y saco DELANTE); «¡Llega un saco!» trae 5 cartas seguidas.
    /// Todo el tiempo va con <see cref="GameClock"/> y lo que se mueve con <see cref="Motion"/>: con «quitar animaciones», sin vuelos, sin brillos latentes y sin partículas.
    /// </summary>
    public sealed partial class MailGameController : GameControllerBase
    {
        public const string GameId = MailContract.GameId;

        private const float UnitsPerDp = 3f;
        private const float MarginU = 60f;

        private enum Phase { Idle, Intro, Brief, Play, Recap, Done }

        // ------------------------------------------------------------------ estado

        private System.Random _rng;
        private AdaptiveDifficulty _dda;
        private MailRun _run;
        private MailDay _day;
        private MailRecap _recap;
        private Phase _phase = Phase.Idle;
        private bool _senior, _loopOn, _guided, _inputOn, _baked, _aborted, _recordBroken, _tapped;
        private int _bestRecord, _debugStage;
        private float _phaseAt;
        private MailIntro _introKind;
        private Vector2? _press;
        /// <summary>Solo la ronda guiada: el día no avanza mientras un paso explica algo, y mientras corre libre solo se aceptan cartas (buzones y caja fuerte).</summary>
        private bool _guidedFreeze, _guidedAllowFree;

        // escala y disposición
        private float _s = 3f, _playW = 1080f, _playH = 1920f, _logicalH = 640f;
        private MailLayout.Metrics _lay;

        private ExitButton _exit;
        private GameHud _hud;
        private CountdownScreen _countdown;

        private float Now => GameClock.Time;

        // ------------------------------------------------------------------ sesión

        public void StartSession(SequenceInitConfig config)
        {
            _config = config;
            _rng = new System.Random();
            var age = DdaUserProfileConfig.ParseAgeBand(config.config.age_band);
            _senior = age == AgeBand.Senior;
            _dda = MailContract.CreateEngine(config.config);
            _bestRecord = Mathf.Max(0, config.config.mail_best);
            _debugStage = Mathf.Max(0, config.config.mail_stage);
            _run = new MailRun(MailContract.StartLevel(_dda), _bestRecord, _rng)
            {
                Floor = Mathf.FloorToInt(_dda.MinRating),
                Ceiling = Mathf.Min(MailContract.MaxLevel, Mathf.FloorToInt(_dda.MaxRating)),
            };
#if UNITY_EDITOR
            // el smoke arranca en la etapa 4 (una hora, lo de todos los días, la radio que cancela) y con días cortos, para pasar por todo lo importante
            if (GuidedTutorial.EditorAutoPlayGame && _debugStage <= 0) _run.Level = _run.MaxLevelReached = 4;
#endif
            _phase = Phase.Idle;
            _loopOn = _guided = _inputOn = _aborted = _recordBroken = false;
            _press = null;
            _day = null;
            _recap = null;
            ResetViews();
            ShowOnly(null);
            _exit.Hide();
            _hud.SetStreak(0);
            _tutorial.Hide();
            StopAllCoroutines();
            StartCoroutine(GameLoop());
        }

        private IEnumerator GameLoop()
        {
            StartCoroutine(Prewarm());
            if (TutorialWanted)
            {
                // la ronda guiada se juega sobre la estación ya armada, antes de la cuenta regresiva
                _safe.gameObject.SetActive(true);
                yield return null;
                ApplySafeArea(_safe);
                Canvas.ForceUpdateCanvases();
                Layout();
                yield return StartCoroutine(RunTutorialIfNeeded());
                ResetViews();
                ShowOnly(null);
            }
            _safe.gameObject.SetActive(false);
            yield return StartCoroutine(_countdown.Play(MailContract.TitleRun, Assessment.Subtitle(MailContract.CountdownSub), () => _safe.gameObject.SetActive(true)));
            while (!_baked) yield return null;
            _safe.gameObject.SetActive(true);
            yield return null;
            ApplySafeArea(_safe);
            Canvas.ForceUpdateCanvases();
            Layout();
            _loopOn = true;
            yield return StartCoroutine(MainLoop());
        }

        /// <summary>El bucle de la partida: un día tras otro hasta los 4. «Cómo se juega» lo retoma desde acá.</summary>
        private IEnumerator MainLoop()
        {
            while (!_run.Finished) yield return StartCoroutine(PlayDay());
            _loopOn = false;
            yield return StartCoroutine(FinishGame());
        }

        /// <summary>Hornea los sprites y sintetiza los sonidos durante la cuenta regresiva, de a poco por cuadro (así nada se traba).</summary>
        private IEnumerator Prewarm()
        {
            if (!_baked)
            {
                var sprites = MailSprites.Prewarm();
                while (sprites.MoveNext()) yield return null;
                AssignSprites();
                _baked = true;
            }
            var sounds = MailSounds.Prewarm();
            while (sounds.MoveNext()) yield return null;
        }

        // ------------------------------------------------------------------ un día

        /// <summary>El largo del día: 50 s (en el smoke del Editor, un día corto para que el arranque de prueba pase por el resumen).</summary>
        private float DaySeconds()
        {
#if UNITY_EDITOR
            if (GuidedTutorial.EditorAutoPlayGame) return 24f;
#endif
            return MailContract.DaySeconds;
        }

        private IEnumerator PlayDay()
        {
            _aborted = false;
            _day = _run.NextDay(DaySeconds());
            BindDay(_day);
            UpdateHud();
            _phase = Phase.Idle;
            foreach (var intro in MailContract.IntrosFor(_run.Level))
            {
                if (IntroSeen(intro)) continue;
                yield return StartCoroutine(ShowIntro(intro));
                MarkIntroSeen(intro);
            }
            yield return StartCoroutine(ShowBrief());
            BeginPlay();
            while (!_day.Ended && !_aborted) yield return null;
            if (_aborted) yield break;
            _inputOn = false;
            yield return Motion.Hold(0.35f);
            yield return StartCoroutine(EndDay());
        }

        private void BeginPlay()
        {
            _phase = Phase.Play;
            _inputOn = true;
            _press = null;
            ShowOnly(null);
            _day.Begin();
            PlayClip(MailSounds.Hum(), 0.5f);
        }

        /// <summary>El fin del día: el resumen (cada encargo con su marca), el motor común (cada encargo es un ensayo), la etapa de mañana y el botón de seguir.</summary>
        private IEnumerator EndDay()
        {
            _phase = Phase.Recap;
            _recap = _day.Summarize();
            if (!_guided)
            {
                foreach (var it in _recap.Items) _dda.Register(it.Ok);
                _run.Complete(_day, _recap);
                if (_run.NewRecordToday) _recordBroken = true;
            }
            ClearBelt();
            PlayClip(MailSounds.DayEnd(), 0.8f);
            BindRecap();
#if UNITY_EDITOR
            Debug.Log("[SmokeTest] Correo: fin del día " + _day.DayNo + " (etapa " + _day.Level + "): encargos " + _recap.Stat.PmOk + " de " + _recap.Stat.PmAll + ", cartas " + _recap.Stat.Right + " de " + (_recap.Stat.Sorted + _recap.Stat.Late) + ", miradas al reloj " + _recap.Stat.Peeks);
#endif
            ShowOnly(_recapLayer);
            _phaseAt = Now;
            _tapped = false;
            _inputOn = true;
            _press = null;
            float waited = 0f;
            while (!_tapped)
            {
                waited += GameClock.RealDeltaTime;
                if (GuidedTutorial.AutoPlayGame(waited)) _tapped = true;          // solo en el smoke del Editor
                yield return null;
            }
            _inputOn = false;
            ShowOnly(null);
            UpdateHud();
        }

        private IEnumerator ShowBrief()
        {
            _phase = Phase.Brief;
            BindBrief();
            ShowOnly(_briefLayer);
            PlayClip(MailSounds.BriefBell(), 0.7f);
            _phaseAt = Now;
            _tapped = false;
            _inputOn = true;
            _press = null;
            float waited = 0f;
            while (!_tapped)
            {
                waited += GameClock.RealDeltaTime;
                if (GuidedTutorial.AutoPlayGame(waited)) _tapped = true;
                yield return null;
            }
            _inputOn = false;
        }

        private void UpdateHud()
        {
            _hud.SetLevel(_run != null ? _run.Level : 1);
            _hud.SetInfo(_day != null ? MailContract.DayHud(_day.DayNo) : MailContract.DayHud(1));
        }

        // ------------------------------------------------------------------ resultado

        private IEnumerator FinishGame()
        {
            _phase = Phase.Done;
            _inputOn = false;
            var t = _run.Tally;
#if UNITY_EDITOR
            Debug.Log("[SmokeTest] Correo: partida terminada: " + t.Percent + " % (" + t.MeasureOk + " de " + t.MeasureAll + "), etapa más alta " + _run.MaxLevelReached);
#endif
            int record = MailContract.NewRecord(_bestRecord, _run.BestDayRecord);
            ClearBelt();
            ShowOnly(_endLayer);
            _stageLayer.gameObject.SetActive(false);
            BindEnd();
            _exit.Show();
            PlayClip(MailSounds.Finale(), 0.8f);
            var telemetry = new StroopTelemetry
            {
                user_id = _config.user_id,
                game_id = _config.game_id,
                session_metrics = MailContract.BuildMetrics(_dda, t, record, _recordBroken, _run.MaxLevelReached, _config.config)
            };
            NativeBridge.ForwardTelemetryToPlatform(JsonUtility.ToJson(telemetry));
            yield break;
        }

        // ------------------------------------------------------------------ tarjetas «NUEVO» (una sola vez por instalación cada una: se guardan en las preferencias de Unity)

        private const string IntroKey = "mail_intros";

        private static bool IntroSeen(MailIntro intro)
        {
            try { return (PlayerPrefs.GetInt(IntroKey, 0) & (1 << (int)intro)) != 0; }
            catch (System.Exception) { return false; }
        }

        private static void MarkIntroSeen(MailIntro intro)
        {
            try { PlayerPrefs.SetInt(IntroKey, PlayerPrefs.GetInt(IntroKey, 0) | (1 << (int)intro)); PlayerPrefs.Save(); }
            catch (System.Exception) { }
        }

        private IEnumerator ShowIntro(MailIntro intro)
        {
            _phase = Phase.Intro;
            _tapped = false;
            _introKind = intro;
            _introTag.text = MailContract.IntroTag(intro);
            LayoutIntro(MailContract.IntroLines(intro));
            ShowOnly(_introLayer);
            _phaseAt = Now;
            PlayClip(MailSounds.Chime(), 0.7f);
            _inputOn = true;
            _press = null;
            float waited = 0f;
            while (!_tapped)
            {
                waited += GameClock.RealDeltaTime;
                if (GuidedTutorial.AutoPlayGame(waited)) _tapped = true;
                yield return null;
            }
            _inputOn = false;
            ShowOnly(null);
        }

        /// <summary>El tutorial guiado común sobre el área segura (se llama al final de <c>BuildUi</c>, para que quede encima de todo).</summary>
        private void SetUpTutorial() =>
            BuildTutorial(_safe, GameHud.Height + 10f, MailContract.TitleRun, "Toca el buzón del sello de cada carta y cumple los encargos del día: nadie te los recuerda.", badgeAtBottom: true);

        // ------------------------------------------------------------------ «Cómo se juega» desde la pausa

        protected override bool HowToReady => _loopOn && _phase != Phase.Done;

        protected override void HowToSuspend()
        {
            if (_run != null && _day != null && _phase != Phase.Recap && !(_day.Ended && _recap != null)) _run.DayNo = Mathf.Max(0, _run.DayNo - 1);     // el día a medias se vuelve a jugar entero
            _phase = Phase.Idle;
            _inputOn = false;
            _day = null;
            ResetViews();
            ShowOnly(null);
        }

        protected override void HowToResume(float spentSeconds)
        {
            StartCoroutine(MainLoop());
        }

        // ------------------------------------------------------------------ entrada

        private void Update()
        {
            if (PollTutorialSkip()) return;             // un toque en «Saltar tutorial» no es un toque al juego
            float dt = GameClock.DeltaTime;
            float now = GameClock.Time;
            if (_day != null && _phase == Phase.Play && !_day.Ended && !_guidedFreeze && dt > 0f)
            {
                _day.Step(MailMotion.ClampDt(dt));
                ProcessEvents();
            }
            AnimateAll(now, dt);
            if (_inputOn)
            {
                ReadPress();
                HandlePress();
            }
#if UNITY_EDITOR
            if (_phase == Phase.Play && !_guided && dt > 0f) AutoPlay(dt);
#endif
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
            switch (_phase)
            {
                case Phase.Intro:
                    if (Now - _phaseAt > 0.5f) _tapped = true;
                    return;
                case Phase.Brief:
                    if (Now - _phaseAt > 0.6f && InButton(p)) _tapped = true;
                    return;
                case Phase.Recap:
                    if (Now - _phaseAt > 0.9f && InButton(p)) _tapped = true;
                    return;
                case Phase.Play:
                    PressInPlay(p);
                    return;
            }
        }

        /// <summary>El botón de abajo de la hoja y del resumen (y un margen de 8 dp: es de 56 dp o más).</summary>
        private bool InButton(Vector2 p) => new MailLayout.Rect2(MailLayout.W / 2f - 98f, _logicalH - 118f, 196f, 80f).Contains(p.x, p.y);

        private void PressInPlay(Vector2 p)
        {
            if (_day == null || _day.Ended) return;
            var m = _lay;
            if (Vector2.Distance(p, new Vector2(m.ClockCx, m.ClockCy)) <= MailLayout.ClockR + 10f) { OnPressed(Target.Clock, 0); return; }
            if (m.Safe.Contains(p.x, p.y)) { OnPressed(Target.Safe, 0); return; }
            if (m.Beacon.Contains(p.x, p.y)) { OnPressed(Target.Beacon, 0); return; }
            for (int i = 0; i < _day.Stage.Boxes; i++)
                if (MailLayout.BoxRect(i, _day.Stage.Boxes, m.BoxY0).Contains(p.x, p.y)) { OnPressed(Target.Box, i); return; }
        }

        private enum Target { Box, Safe, Beacon, Clock }

        /// <summary>Un toque en una pieza. En la ronda guiada puede estar acotado a lo que se pide.</summary>
        private void OnPressed(Target t, int box)
        {
            if (_guided && !GuidedAllows(t, box)) return;
            switch (t)
            {
                case Target.Box: DoBox(box); break;
                case Target.Safe: DoSafe(); break;
                case Target.Beacon: DoBeacon(); break;
                default: DoClock(); break;
            }
        }

        private void PlayClip(AudioClip clip, float volume)
        {
            if (clip == null || !GameFeel.SoundOn) return;
            if (_config != null && _config.config != null && !_config.config.sound_enabled) return;
            _audioSource.PlayOneShot(clip, volume);
        }

#if UNITY_EDITOR
        private float _botT;
        private int _botPeeks;

        /// <summary>SOLO EN EL EDITOR (smoke): juega solo. Clasifica cada carta (las señal, a la caja fuerte), mira el reloj un par de veces y enciende el faro dentro de la ventana de cada encargo por hora; de vez en cuando se
        /// «equivoca» de buzón (así el arranque de prueba pasa por los aciertos, los errores y los resúmenes). En el teléfono no hace nada.</summary>
        private void AutoPlay(float dt)
        {
            if (!GuidedTutorial.AutoPlayGame(10f) || _day == null || _day.Ended) return;
            _botT += dt;
            if (_botT < 0.5f) return;
            _botT = 0f;
            float f = _day.Fraction;
            if (_day.Peeks.Count < 2 && f > 0.4f && _botPeeks++ % 2 == 0) DoClock();
            foreach (var t in _day.Todos)
                if (t.IsTime && t.State == MailState.Open && !t.Cancelled && f >= t.W0 && f <= t.W1) { DoBeacon(); return; }
            if (_day.Belt.Count > 0 && FrontReady())
            {
                var l = _day.Belt[0];
                if (l.IsCue) DoSafe();
                else DoBox(_rng.Next(10) == 0 ? (l.Planet + 1) % _day.Stage.Boxes : l.Planet);
            }
        }
#endif
    }
}
