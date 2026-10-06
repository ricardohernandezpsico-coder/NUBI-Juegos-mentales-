using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using NeuroVida.Bridge;
using NeuroVida.Contracts;
using NeuroVida.Games.Shared;
using static NeuroVida.Games.Shared.UiKit;
using Motion = NeuroVida.Games.Shared.Motion; // UnityEngine.Motion también existe

namespace NeuroVida.Games.Bodega
{
    /// <summary>
    /// «Bodega de carga» (Memoria; ver <see cref="BodegaContract"/> y docs/diseno-bodega-de-carga.md; el boceto aprobado es docs/previews/bodega-de-carga-boceto.html, versión 4).
    /// Una bodega redonda con la ESCLUSA de carga (posición 0 del anillo, gira con la bodega), escotillas alrededor y un robot al centro. La carga llega por la esclusa y el robot la toma con su haz y la lleva
    /// por dentro del anillo hasta su escotilla (que se abre antes de que llegue; suena SU nota); a veces cambia una caja cerrada de lugar y a veces la bodega gira 2 o 3 posiciones. Después piden los
    /// objetos uno por uno y se toca la escotilla que lo tiene: si no era, la escotilla muestra qué había (otro objeto o «vacía»), la correcta brilla y se abre sola (aprendizaje sin errores); el objeto
    /// encontrado sale por la esclusa y baja al carro de reparto. El robot NO reacciona al desempeño (nada de caras ni negar con la cabeza: regla de patentes de Akili): trabaja, mira hacia donde trabaja, flota y parpadea.
    /// Todo lo que se mueve sigue a su objetivo con un resorte amortiguado y el dt real (<see cref="BodegaMotion"/>): ese es el «flow» que pidió Ricardo. Precisión = 6 pedidos; Reto = 120 s (las tarjetas «NUEVO» no
    /// gastan tiempo). Con «quitar animaciones»: sin resortes, flotación, chispas, inclinación, mecerse ni temblor; todo llega al instante, las escotillas se abren y cierran, la caja se ve en su
    /// escotilla nueva y la bodega aparece girada, con los mismos tiempos de mirada (<c>Motion.Hold</c>).
    /// </summary>
    public sealed partial class BodegaGameController : GameControllerBase
    {
        public const string GameId = BodegaContract.GameId;

        private const float UnitsPerDp = 3f;
        private const float Su = 3f;                 // unidades de la bodega por dp (la bodega se escala entera sobre esto)
        private const float MarginU = 60f;

        private enum Phase { Idle, Intro, Load, Move, Spin, Ask, Busy, Done }
        private enum CardKind { None, Watch, Store, Move, Spin, Ask, Done }
        private enum CrateMode { None, Pull, Carry, Push }

        // ------------------------------------------------------------------ estado

        private System.Random _rng;
        private AdaptiveDifficulty _dda;
        private BodegaTally _tally;
        private Order _order;
        private Phase _phase = Phase.Idle;
        private bool Endless => _config != null && _config.config.timed;
        private bool _senior, _loopOn, _guided, _inputOn, _baked, _askResolved, _aborted, _storeDone, _recordNow, _recordBroken;
        private int _ordersDone, _errs, _askI, _points, _bestRecord;
        private bool _first;
        private float _endsAt, _t0, _introAt;
        private int _lastTickSecond = -1;
        private bool _introTapped;
        private Vector2? _press;
        private Intro _introKind;

        // escala y disposición
        private float _s = 3f, _playW = 1080f, _playH = 1920f, _logicalH = 640f;
        private BodegaLayout.Metrics _lay;

        private ExitButton _exit;
        private GameHud _hud;
        private CountdownScreen _countdown;

        // ------------------------------------------------------------------ sesión

        public void StartSession(SequenceInitConfig config)
        {
            _config = config;
            _rng = new System.Random();
            var age = DdaUserProfileConfig.ParseAgeBand(config.config.age_band);
            _senior = age == AgeBand.Senior;
            float start = AdaptiveDifficulty.StartRating(config.config, BodegaContract.MaxLevel);
            // un pedido perfecto de 5 objetos sube una etapa, uno con dos errores baja una; sin tiempo de reacción: mirar con calma no se penaliza
            _dda = new AdaptiveDifficulty(BodegaContract.MaxLevel, age, start, stepUp: BodegaContract.DdaStepUp, useReaction: false);
            _tally = new BodegaTally();
            _bestRecord = Mathf.Max(0, config.config.bod_best);

            _phase = Phase.Idle;
            _loopOn = _guided = _inputOn = _aborted = false;
            _press = null;
            _ordersDone = _errs = _askI = _points = 0;
            _endsAt = 0f;
            _lastTickSecond = -1;
            _recordNow = _recordBroken = false;
            _order = null;
            ResetBoard();

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
                // la ronda guiada se juega sobre la bodega ya armada, antes de la cuenta regresiva
                _safe.gameObject.SetActive(true);
                yield return null;
                ApplySafeArea(_safe);
                Canvas.ForceUpdateCanvases();
                Layout();
                UpdateHud();
                yield return StartCoroutine(RunTutorialIfNeeded());
                ResetBoard();
            }
            _safe.gameObject.SetActive(false);
            yield return StartCoroutine(_countdown.Play("Bodega de carga", Assessment.Subtitle("Recuerda dónde guarda cada cosa el robot"), () => _safe.gameObject.SetActive(true)));
            while (!_baked) yield return null;
            _safe.gameObject.SetActive(true);
            yield return null;
            ApplySafeArea(_safe);
            Canvas.ForceUpdateCanvases();
            Layout();
            UpdateHud();

            _endsAt = GameClock.Time + BodegaContract.RetoSeconds;
            _loopOn = true;
            yield return StartCoroutine(MainLoop());
        }

        /// <summary>El bucle de la partida: un pedido tras otro hasta que se acabe el tiempo (Reto) o los 6 (Precisión). «Cómo se juega» lo retoma desde acá.</summary>
        private IEnumerator MainLoop()
        {
            while (!Finished()) yield return StartCoroutine(PlayOrder());
            _loopOn = false;
            yield return StartCoroutine(FinishGame());
        }

        private bool Finished() => Endless ? GameClock.Time >= _endsAt : _ordersDone >= BodegaContract.PrecisionOrders;

        /// <summary>El Reto se acabó (solo cuenta con la partida en marcha).</summary>
        private bool Over => Endless && _loopOn && GameClock.Time >= _endsAt;

        /// <summary>Hornea los sprites y sintetiza los sonidos durante la cuenta regresiva, de a poco por cuadro (así nada se traba).</summary>
        private IEnumerator Prewarm()
        {
            if (!_baked)
            {
                var sprites = BodegaSprites.Prewarm();
                while (sprites.MoveNext()) yield return null;
                AssignSprites();
                _baked = true;
            }
            var sounds = BodegaSounds.Prewarm();
            while (sounds.MoveNext()) yield return null;
        }

        // ------------------------------------------------------------------ un pedido

        /// <summary>Un pedido: la carga llega y se guarda, a veces cambian cajas y gira la bodega, y se piden los objetos. Si se acaba el tiempo del Reto a mitad, no cuenta (ni a favor ni en contra).</summary>
        private IEnumerator PlayOrder()
        {
            var order = BodegaContract.Generate(_dda.PresentedLevel, _rng);
            SetUpOrder(order);
            UpdateHud();
            foreach (var intro in BodegaContract.IntrosFor(order.Level))
            {
                if (IntroSeen(intro)) continue;
                float before = GameClock.Time;
                yield return StartCoroutine(ShowIntro(intro));
                _endsAt = HowToClock.Shift(_endsAt, GameClock.Time - before);       // la tarjeta «NUEVO» no gasta tiempo del Reto
                MarkIntroSeen(intro);
            }
            _aborted = false;
            yield return StartCoroutine(StorePhase(order));
            if (!_aborted) yield return StartCoroutine(MovePhase(order));
            if (!_aborted) yield return StartCoroutine(SpinPhase(order));
            if (!_aborted) yield return StartCoroutine(AskPhase(order));
            if (_aborted)
            {
                _phase = Phase.Idle;
                ResetBoard();
                yield break;
            }
            yield return StartCoroutine(EndOrder(order));
        }

        /// <summary>La carga llega por la esclusa, el robot la toma con su haz y la lleva por dentro del anillo hasta su escotilla, que se abre antes de que llegue: entra, suena SU nota, se ve y se cierra.</summary>
        private IEnumerator StorePhase(Order o)
        {
            _phase = Phase.Load;
            _inputOn = false;
            _card = CardKind.Watch;
            _cardObj_ = -1;
            PlayClip(BodegaSounds.Hum(), 0.5f);
            yield return Motion.Hold(0.5f);
            float slow = BodegaContract.Slow(_senior);
            foreach (int hi in o.StoreOrder)
            {
                if (Over) { _aborted = true; _storeDone = true; yield break; }
                var h = _hv[hi];
                float now = GameClock.Time;
                // llega por la esclusa
                _card = CardKind.Store;
                _cardObj_ = h.Obj_;
                _cardAt = now;
                _lockInAt = now;
                PlayClip(BodegaSounds.Arrive(), 0.6f);
                LookAtLock();
                _flyActive = true;
                _flyObjId = h.Obj_;
                _flyTo = hi;
                _flyStartsAt = now + 0.33f;
                _flyDur = 0.75f * slow;
                yield return Motion.Hold(0.33f);
                // el robot lo toma con su haz y lo lleva por dentro del anillo hasta su escotilla
                BeamStart(-1);
                _robot.Sq = BodegaMotion.SquashOnBeam;
                PlayClip(BodegaSounds.Beam(), 0.6f);
                yield return Motion.Hold(0.33f * slow);
                h.Target = 1f;
                PlayClip(BodegaSounds.Open(), 0.6f);
                yield return Motion.Hold(0.42f * slow);
                _flyActive = false;
                RobotHome();
                h.ShowAt = GameClock.Time;
                PlayClip(BodegaSounds.Note(hi), 0.9f);
                var p = HatchPos(hi);
                Emit(L(p.x, p.y), 10, 60f, 0.5f, Gold, 20f);
                BeamStop();
                yield return Motion.Hold(0.95f * slow);
                h.Target = 0f;
                PlayClip(BodegaSounds.Close(), 0.5f);
                _card = CardKind.Watch;
                yield return Motion.Hold(0.2f);
            }
            RobotHome();
            _card = CardKind.Watch;
            yield return Motion.Hold(0.35f);
            _storeDone = true;
        }

        /// <summary>El robot cambia cajas de lugar: vuela a una escotilla, saca la caja CERRADA con su haz, la lleva colgando por el anillo y la deja en una escotilla vacía (hay que acordarse qué había adentro).</summary>
        private IEnumerator MovePhase(Order o)
        {
            if (o.Moves.Length == 0) yield break;
            _phase = Phase.Move;
            float slow = BodegaContract.Slow(_senior);
            foreach (var m in o.Moves)
            {
                if (Over) { _aborted = true; yield break; }
                _card = CardKind.Move;
                yield return Motion.Hold(0.4f);
                var A = NearHatch(m.From, 58f);
                var B = NearHatch(m.To, 58f);
                _robot.Tx = A.x; _robot.Ty = A.y; _robot.Ta = A.a;
                yield return Motion.Hold(0.65f);
                BeamStart(m.From);
                PlayClip(BodegaSounds.Beam(), 0.6f);
                _robot.Sq = BodegaMotion.SquashOnBeam;
                _hv[m.From].GlowAt = GameClock.Time;
                yield return Motion.Hold(0.22f);
                _hv[m.From].Obj_ = -1;
                CrateStart(CrateMode.Pull, m.From, 0.45f);
                PlayClip(BodegaSounds.Clank(), 0.6f);
                yield return Motion.Hold(0.45f);
                _crateMode = CrateMode.Carry;
                BeamStop();
                PlayClip(BodegaSounds.Whoosh(), 0.6f);
                // el vuelo por el anillo, por el camino más corto; el resorte del robot le da la estela suave
                float dAng = BodegaMotion.AngleDelta(A.a, B.a);
                float dur = 1.3f * slow;
                float t = 0f;
                float ringR = BodegaLayout.Ring - 58f;
                while (t < dur)
                {
                    t += GameClock.DeltaTime;
                    float e = BodegaMotion.EaseInOut(t / dur);
                    float ang = A.a + dAng * e;
                    _robot.Tx = BodegaLayout.CenterX + Mathf.Cos(ang) * ringR;
                    _robot.Ty = BodegaLayout.CenterY + Mathf.Sin(ang) * ringR;
                    _robot.Ta = ang + Mathf.Sign(dAng) * Mathf.PI / 2f;
                    yield return null;
                }
                _robot.Tx = B.x; _robot.Ty = B.y;
                _robot.Ta = B.a;
                yield return Motion.Hold(0.45f);
                BeamStart(m.To);
                PlayClip(BodegaSounds.Beam(), 0.6f);
                CrateStart(CrateMode.Push, m.To, 0.42f);
                yield return Motion.Hold(0.42f);
                _hv[m.To].Obj_ = m.Obj;
                _crateMode = CrateMode.None;
                _hv[m.To].GlowAt = GameClock.Time;
                PlayClip(BodegaSounds.Clank(), 0.6f);
                BeamStop();
                yield return Motion.Hold(0.3f);
                RobotHome();
                yield return Motion.Hold(0.5f);
            }
        }

        /// <summary>La bodega gira 2 o 3 posiciones: un pequeño impulso hacia atrás y un asentarse suave; la esclusa gira con ella y el robot la sigue con la mirada.</summary>
        private IEnumerator SpinPhase(Order o)
        {
            if (o.Spin == 0) yield break;
            if (Over) { _aborted = true; yield break; }
            _phase = Phase.Spin;
            _card = CardKind.Spin;
            yield return Motion.Hold(0.6f);
            float slow = BodegaContract.Slow(_senior);
            float from = _rot, to = from + o.Spin * 2f * Mathf.PI / (o.Hatches + 1);
            float dur = BodegaMotion.SpinSeconds * slow;
            PlayClip(BodegaSounds.Ratchet(), 0.6f);
            float t = 0f;
            while (t < dur)
            {
                t += GameClock.DeltaTime;
                _rot = Motion.Decorative ? from + (to - from) * BodegaMotion.EaseSpin(t / dur) : from;      // «quitar animaciones»: aparece ya girada al final
                LookAtLock();
                yield return null;
            }
            _rot = to;
            yield return Motion.Hold(0.25f);
            RobotHome();
            yield return Motion.Hold(0.35f);
        }

        /// <summary>Los pedidos: uno por objeto guardado y en orden al azar. Se toca la escotilla que lo tiene.</summary>
        private IEnumerator AskPhase(Order o)
        {
            for (_askI = 0; _askI < o.Asks.Length;)
            {
                if (Over) { _aborted = true; yield break; }
                BeginAsk(o.Asks[_askI]);
                _askResolved = false;
                float waited = 0f;
                while (!_askResolved)
                {
                    waited += GameClock.RealDeltaTime;
                    if (Over && _phase == Phase.Ask) { _aborted = true; _inputOn = false; yield break; }
                    if (_phase == Phase.Ask && GuidedTutorial.AutoPlayGame(waited)) AutoPlayAsk(o.Asks[_askI]);          // solo en el smoke del Editor
                    yield return null;
                }
            }
        }

        /// <summary>Muestra el pedido (tarjeta con el objeto y borde dorado) y abre la entrada a las escotillas.</summary>
        private void BeginAsk(int obj)
        {
            _card = CardKind.Ask;
            _cardObj_ = obj;
            _cardAt = GameClock.Time;
            _first = true;
            _t0 = GameClock.Time;
            _hint = -1;
            _phase = Phase.Ask;
            _inputOn = true;
            _press = null;
            PlayClip(BodegaSounds.Ping(), 0.6f);
        }

        /// <summary>Un toque en una escotilla (cuando se está pidiendo un objeto).</summary>
        private void TapHatch(int i)
        {
            if (_phase != Phase.Ask || _order == null) return;
            _phase = Phase.Busy;
            _inputOn = false;
            StartCoroutine(ResolveTap(i));
        }

        /// <summary>Correcta: se abre, suena su nota y el objeto sale por la esclusa al carro. Equivocada: se abre y MUESTRA qué había (otro objeto o «vacía»), tiembla, suena un golpe suave y después la
        /// correcta brilla en dorado y se abre sola: siempre se aprende dónde estaba.</summary>
        private IEnumerator ResolveTap(int i)
        {
            var h = _hv[i];
            int want = _order.Asks[_askI];
            float now = GameClock.Time;
            h.PressAt = now;
            if (h.Obj_ == want)
            {
                yield return StartCoroutine(FoundRoutine(i, _first));
            }
            else
            {
                _first = false;
                RecordMiss();
                h.Target = 1f;
                h.ShakeAt = h.RevealAt = now;
                PlayClip(BodegaSounds.Open(), 0.6f);
                yield return Motion.Hold(0.16f);
                PlayClip(BodegaSounds.Thud(), 0.7f);
                int right = RightHatch(want);
                yield return Motion.Hold(0.75f);
                h.Target = 0f;
                PlayClip(BodegaSounds.Close(), 0.5f);
                yield return Motion.Hold(0.25f);
                _hv[right].GlowAt = GameClock.Time;
                _hint = right;
                PlayClip(BodegaSounds.Ping(), 0.6f);
                yield return Motion.Hold(0.7f);
                _hint = -1;
                yield return StartCoroutine(FoundRoutine(right, false));
            }
        }

        private IEnumerator FoundRoutine(int i, bool first)
        {
            var h = _hv[i];
            int obj = h.Obj_;
            float now = GameClock.Time;
            h.Target = 1f;
            PlayClip(BodegaSounds.Open(), 0.6f);
            yield return Motion.Hold(0.16f);
            PlayClip(BodegaSounds.Note(i), 0.9f);
            int ms = Mathf.RoundToInt((GameClock.Time - _t0) * 1000f);
            if (!_guided) RecordFound(first, ms);
            if (first && !_guided) PlayClip(BodegaSounds.Spark(_tally.Streak), 0.6f);
            int slot = _found.Count;
            _found.Add(new Found { Obj = obj, First = first });
            h.Obj_ = -1;
            var p = HatchPos(i);
            Emit(L(p.x, p.y), first ? 22 : 10, first ? 150f : 80f, 0.7f, first ? Gold : Lavender, 20f);
            LaunchFlyer(slot, obj, i, first);
            yield return Motion.Hold(0.65f);
            h.Target = 0f;
            PlayClip(BodegaSounds.Close(), 0.5f);
            _askI++;
            yield return Motion.Hold(0.3f);
            _phase = Phase.Idle;
            _askResolved = true;
        }

        /// <summary>La escotilla que de verdad tiene el objeto ahora (lo que se ve, después de las cajas y el giro).</summary>
        private int RightHatch(int obj)
        {
            for (int k = 0; k < _order.Hatches; k++) if (_hv[k].Obj_ == obj) return k;
            return _order.HatchOf(obj);
        }

        /// <summary>Anota un objeto encontrado: cuentas, DDA, racha y puntos.</summary>
        private void RecordFound(bool first, int ms)
        {
            _tally.Found(first, ms);
            _dda.Register(first);
            _points += first ? 10 : 5;
            _hud.SetStreak(_tally.Streak);
            UpdateHud();
        }

        private void RecordMiss()
        {
            if (_guided) return;
            _errs++;
            _tally.BreakStreak();
            _hud.SetStreak(0);
        }

        /// <summary>Fin de un pedido: perfecto (arpegio y, si es el mayor, récord nuevo) o con un aviso y un truco. Sube o baja de etapa por el DDA.</summary>
        private IEnumerator EndOrder(Order o)
        {
            _phase = Phase.Done;
            bool perfect = _errs == 0;
            _recordNow = false;
            _tally.EndOrder(o.Objects, _errs, o.Level);
            if (perfect && o.Objects > _bestRecord)
            {
                _bestRecord = o.Objects;
                _recordNow = true;
                _recordBroken = true;
            }
            _ordersDone++;
            _card = CardKind.Done;
            _cardPerfect = perfect;
            _cardFirst = _found.FindAll(f => f.First).Count;
            _cardAt = GameClock.Time;
            if (perfect) PlayClip(BodegaSounds.Perfect(), 0.8f);
            else Tell(BodegaContract.ToastTitle(_errs), BodegaContract.ToastTip(_ordersDone, _errs), true, 2.8f);
            UpdateHud();
            yield return Motion.Hold(perfect ? 2f : 3f);
            _phase = Phase.Idle;
        }

        private IEnumerator FinishGame()
        {
            _phase = Phase.Done;
            _inputOn = false;
            ResetBoard();
            int score = BodegaContract.Score(_tally.FirstTry, _tally.Total);
            ShowResult(score);
            PlayClip(BodegaSounds.Finale(), 0.8f);

            var telemetry = new StroopTelemetry
            {
                user_id = _config.user_id,
                game_id = _config.game_id,
                session_metrics = new StroopSessionMetrics
                {
                    correct_trials = _tally.FirstTry,
                    total_trials = _tally.Total,
                    calculated_score = score,
                    average_response_time_ms = _tally.MeanMs < 0 ? 0 : _tally.MeanMs,
                    level = _config.config.level,
                    timed = _config.config.timed,
                    end_rating = _dda.RatingNormalized,
                    mode_trials = _dda.ScoredTrials,
                    mode_hits = _dda.ScoredCorrect,
                    peak_level = _dda.PeakLevel,
                    bod_group = _tally.Total > 0 ? _tally.PeakGroup : -1,
                    bod_best_streak = _tally.BestStreak,
                    bod_biggest = _tally.BiggestPerfect,
                    bod_best = BodegaContract.NewRecord(_bestRecord, _tally.BiggestPerfect),
                    bod_ms = _tally.MeanMs,
                    bod_new = _recordBroken ? 1 : 0
                }
            };
            NativeBridge.ForwardTelemetryToPlatform(JsonUtility.ToJson(telemetry));
            yield break;
        }

        private void ShowResult(int score)
        {
            _exit.Show();
            _boardRoot.gameObject.SetActive(false);
            _cardLayer.gameObject.SetActive(false);
            _trayLayer.gameObject.SetActive(false);
            _toastLayer.gameObject.SetActive(false);
            _resultRoot.Find("Title").GetComponent<Text>().text = "¡Buen trabajo!";
            _resultRoot.Find("Detail").GetComponent<Text>().text = $"{_tally.FirstTry} de {_tally.Total} objetos al primer intento";
            int record = BodegaContract.NewRecord(_bestRecord, _tally.BiggestPerfect);
            _resultRoot.Find("Extra").GetComponent<Text>().text = _tally.BiggestPerfect > 0
                ? $"Tu bodega más grande hoy: {_tally.BiggestPerfect} objetos" + (record > 0 ? $" · tu récord: {record}" : "")
                : "Cada objeto que encuentras al primer intento cuenta";
            _resultRoot.gameObject.SetActive(true);
            StartCoroutine(AnimateResult(score));
        }

        private void UpdateHud()
        {
            _hud.SetLevel(_dda != null ? _dda.PresentedLevel : 1);
            if (Endless) _hud.SetPoints(_points);
            else _hud.SetInfo($"Pedido {Mathf.Min(_ordersDone + 1, BodegaContract.PrecisionOrders)} de {BodegaContract.PrecisionOrders}");
        }

        // ------------------------------------------------------------------ tarjetas «NUEVO» (una sola vez por instalación: se guardan en las preferencias de Unity)

        private const string IntroKey = "bod_intros";

        private static bool IntroSeen(Intro intro)
        {
            try { return (PlayerPrefs.GetInt(IntroKey, 0) & (1 << (int)intro)) != 0; }
            catch (System.Exception) { return false; }
        }

        private static void MarkIntroSeen(Intro intro)
        {
            try { PlayerPrefs.SetInt(IntroKey, PlayerPrefs.GetInt(IntroKey, 0) | (1 << (int)intro)); PlayerPrefs.Save(); }
            catch (System.Exception) { }
        }

        private IEnumerator ShowIntro(Intro intro)
        {
            _phase = Phase.Intro;
            _introTapped = false;
            _introKind = intro;
            _introTitle.text = intro == Intro.Bodega ? "BODEGA DE CARGA" : "NUEVO";
            _introLayer.gameObject.SetActive(true);
            LayoutIntro(BodegaContract.IntroText(intro));
            _introAt = GameClock.Time;
            PlayClip(BodegaSounds.Chime(), 0.7f);
            _inputOn = true;
            _press = null;
            float waitedIntro = 0f;
            while (!_introTapped)
            {
                waitedIntro += GameClock.RealDeltaTime;
                if (GuidedTutorial.AutoPlayGame(waitedIntro)) _introTapped = true;          // solo en el smoke del Editor
                yield return null;
            }
            _inputOn = false;
            _introLayer.gameObject.SetActive(false);
        }

        /// <summary>El tutorial guiado común sobre el área segura (se llama al final de <c>BuildUi</c>, para que quede encima de todo).</summary>
        private void SetUpTutorial() =>
            BuildTutorial(_safe, GameHud.Height + 10f, "Bodega de carga", "Mira dónde guarda cada cosa el robot y toca la escotilla que te piden.", badgeAtBottom: true);

        // ------------------------------------------------------------------ «Cómo se juega» desde la pausa

        protected override bool HowToReady => _loopOn && _phase != Phase.Done;

        protected override void HowToSuspend()
        {
            _phase = Phase.Idle;
            _inputOn = false;
            _introLayer.gameObject.SetActive(false);
            ResetBoard();
        }

        protected override void HowToResume(float spentSeconds)
        {
            _endsAt = HowToClock.Shift(_endsAt, spentSeconds);       // el tiempo que duró «Cómo se juega» no se le descuenta al Reto
            ResetBoard();
            StartCoroutine(MainLoop());
        }

        // ------------------------------------------------------------------ ronda guiada del tutorial (pieza común)

        // <guided>
        protected override IEnumerator GuidedRound(GuidedTutorial t)
        {
            t.BeginPractice();
            _guided = true;
            while (!_baked) yield return null;                         // el arte se hornea mientras Nubi se presenta
            var coach = t.Coach;
            // «Nubi entrenadora»: un pedido de la etapa 1 (2 objetos). 1) la carga entra por la esclusa y el robot la guarda (la bodega entera es el hueco; la tarjeta queda protegida);
            // 2) toque de verdad en la escotilla pedida; 3) un error de muestra: se abre la que tocaron, y la correcta brilla y se abre sola.
            var script = new GuidedScript(1);                          // un pedido; «Saltar tutorial» lo termina
            var easy = BodegaContract.Generate(1, _rng);
            SetUpOrder(easy);
            _phase = Phase.Load;
            _inputOn = false;
            yield return Motion.Hold(0.4f);
            bool ok = !t.Skipped;
            var card = new[] { coach.Zone(() => coach.RectOf(_cardBg.rectTransform)) };
            Coroutine store = null, demo = null;
            if (ok)
            {
                _storeDone = false;
                store = StartCoroutine(StorePhase(easy));
                yield return StartCoroutine(coach.Watch(() => coach.AroundOf(_hull.rectTransform, Vector2.one * BoardUnits(316f)), CoachTexts.Bodega.Watch, () => _storeDone, 16f, keep: card));
                ok = !t.Skipped;
            }
            if (ok)
            {
                BeginAsk(easy.Asks[0]);
                _inputOn = false;
                int target = easy.HatchOf(easy.Asks[0]);
                yield return StartCoroutine(coach.Touch(() => coach.AroundOf(_hv[target].Frame.rectTransform, Vector2.one * BoardUnits(76f)), CoachTexts.Bodega.Ask(BodegaContract.ObjectWithArticle[easy.Asks[0]]), circle: true, keep: card));
                ok = !t.Skipped;
                if (ok)
                {
                    _askI = 0;
                    _phase = Phase.Busy;
                    yield return StartCoroutine(ResolveTap(target));       // el toque en el hueco ES la respuesta
                }
            }
            if (ok)
            {
                BeginAsk(easy.Asks[1]);
                _inputOn = false;
                int right = easy.HatchOf(easy.Asks[1]);
                int wrong = WrongHatchFor(easy, right);
                _askI = 1;
                _askResolved = false;
                _phase = Phase.Busy;
                demo = StartCoroutine(ResolveTap(wrong));
                yield return StartCoroutine(coach.Watch(() => coach.AroundOf(_hull.rectTransform, Vector2.one * BoardUnits(316f)), CoachTexts.Bodega.Wrong, () => _askResolved, 10f, keep: card));
                ok = !t.Skipped;
                if (ok) yield return StartCoroutine(coach.Notice(CoachTexts.Ready, 1.5f));
            }
            if (store != null) StopCoroutine(store);
            if (demo != null) StopCoroutine(demo);
            coach.Hide();
            _inputOn = false;
            _phase = Phase.Idle;
            ResetBoard();
            _guided = false;
            t.EndPractice();
        }
        // </guided>

        /// <summary>Una escotilla que NO tiene el objeto pedido (para mostrar el error de muestra): la primera con otra cosa, o si no una vacía.</summary>
        private static int WrongHatchFor(Order o, int right)
        {
            for (int k = 0; k < o.Hatches; k++) if (k != right && o.Final[k] >= 0) return k;
            for (int k = 0; k < o.Hatches; k++) if (k != right) return k;
            return right;
        }

        private float BoardUnits(float dp) => dp * _s * (_lay.Scale <= 0f ? 1f : _lay.Scale);

        // ------------------------------------------------------------------ entrada

        private void Update()
        {
            if (PollTutorialSkip()) return;             // un toque en «Saltar tutorial» no es un toque al juego
            float dt = GameClock.DeltaTime;
            float now = GameClock.Time;
            UpdateClock();
            if (_boardRoot.gameObject.activeSelf) StepMotion(dt);
            AnimateBoard(now);
            AnimateCard(now);
            AnimateTray(now);
            AnimateFlyers(now);
            AnimateToast(now);
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
            if (_phase == Phase.Intro)
            {
                if (GameClock.Time - _introAt > 0.5f) _introTapped = true;
                return;
            }
            if (_phase != Phase.Ask || _order == null) return;
            int i = HatchAt(p);
            if (i >= 0) TapHatch(i);
        }

        /// <summary>La escotilla más cercana a un toque (dp lógicos), si cae dentro de su radio de toque.</summary>
        private int HatchAt(Vector2 logical)
        {
            BodegaLayout.LogicalToBoard(_lay, logical.x, logical.y, out float bx, out float by);
            int best = -1;
            float bd = float.MaxValue;
            for (int i = 0; i < _order.Hatches; i++)
            {
                var p = HatchPos(i);
                float d = Mathf.Sqrt((bx - p.x) * (bx - p.x) + (by - p.y) * (by - p.y));
                if (d < bd) { bd = d; best = i; }
            }
            return best >= 0 && bd <= BodegaLayout.TouchR ? best : -1;
        }

        /// <summary>SOLO EN EL EDITOR (smoke): los pedidos pares los acierta y los impares se equivocan primero (así pasa también por el error y su aviso). En el teléfono no hace nada.</summary>
        private void AutoPlayAsk(int obj)
        {
#if UNITY_EDITOR
            int right = RightHatch(obj);
            int pick = right;
            if (_ordersDone % 2 == 1) pick = WrongHatchFor(_order, right);
            Debug.Log($"[SmokeTest] Bodega: pedido {_ordersDone + 1}, objeto {obj}, {(pick == right ? "acierta" : "se equivoca primero")}");
            TapHatch(pick);
#endif
        }

        private void UpdateClock()
        {
            if (!Endless || !_loopOn || _endsAt <= 0f || _phase == Phase.Done) return;
            float left = _endsAt - GameClock.Time;
            float frac = Mathf.Clamp01(left / BodegaContract.RetoSeconds);
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
