using System;
using System.Collections;
using UnityEngine;
using NeuroVida.Games.Shared;

namespace NeuroVida.Games.Piloto
{
    public sealed partial class PilotGameController
    {
        /// <summary>La práctica del tutorial: ruta ancha (nivel 1), nave a tres cuartos de la velocidad, señales espaciadas y que duran 4 s.</summary>
        private const float PracticeSpeedFactor = 0.75f, PracticeGapFactor = 1.7f, PracticeExposure = 4f;

        private bool _practiceSpawn;

        // ------------------------------------------------------------------ ronda guiada del tutorial (pieza común)

        // <guided>
        protected override IEnumerator GuidedRound(GuidedTutorial t)
        {
            t.BeginPractice();
            _guided = true;
            while (!_baked) yield return null;
            // «Nubi entrenadora» (docs/diseno-piloto.md §10), sobre una práctica que no cuenta (ruta ancha, nave lenta, misión «hexágono con punto»; no alimenta motores ni medidas). LAS DOS TAREAS VAN JUNTAS desde el paso 2 (regla permanente 1): nunca
            // una pantalla de señales sola ni una ruta sin señales. 1) «Esta es tu misión» (foco en la tarjeta, con una señal de ejemplo a la vista); 2) «Desliza aquí para guiar la nave» (señales ya apareciendo despacio); 3) tocar una señal de la misión
            // (hueco = esa señal; el toque en el hueco es el de verdad y la práctica hace la jugada); 4) «Las parecidas tienen otro detalle»; 5) «En cada sector cambia la misión»; 6) «¡Listo!».
            var savedRun = _run;
            var savedMission = _mission;
            float savedFresh = _missionFreshAt, savedT = _t, savedDist = _dist, savedHyper = _hyperLeft;
            int savedSector = _sector, savedTintFrom = _tintFrom, savedTintTo = _tintTo;
            float savedMix = _tintMix;
            _run = new PilotRun();
            _mission = new PilotMission(SignalShape.Hexagon, SignalDetail.Dot);
            _missionFreshAt = -10f;
            _hyperLeft = 0f;
            _t = _dist = 0f;
            _steeredSeconds = 0f;
            _tintFrom = _tintTo = 0;
            _tintMix = 1f;
            ClearSignals();
            ClearNotices();
            _gates.Clear();
            _gateScheduled = false;
            Layout();                                                       // en la práctica el campo es más bajo: los controles del tutorial van abajo
            _route = new PilotRoute(1);
            _shipX = _steerX = PilotPlan.Width * 0.5f;
            ExtendRoute();
            ShowFlightLayers(true);
            _endLayer.gameObject.SetActive(false);
            SetMissionCard();
            _hud.SetLevelText(PilotContract.SectorChip(0));
            _practiceSpawn = false;
            _phase = Phase.Idle;                                           // el paso 1 es un cuadro quieto
            var coach = t.Coach;
            var script = new GuidedScript(6);
            bool ok = !t.Skipped;

            // 1) esta es tu misión: la tarjeta, y una señal de la misión a la vista para compararla
            Signal example = null;
            if (ok)
            {
                example = SpawnForced(new PilotSignalKind(_mission.Shape, _mission.Detail, true, false), 999f);
                var keepExample = new Func<Rect>[] { SignalsZone(coach) };
                float started = Time.unscaledTime;
                yield return StartCoroutine(coach.Watch(() => coach.RectOf(_missionFill.rectTransform), CoachTexts.Piloto.Mission, () => Time.unscaledTime - started > 3.2f, 3.6f, keep: keepExample));
                ok = !t.Skipped;
                if (ok) script.Success();
            }

            // 2) las dos tareas a la vez: se desliza en la franja con las señales ya apareciendo despacio
            if (ok)
            {
                ClearSignals();
                _phase = Phase.Fly;
                _practiceSpawn = true;
                _nextSignalIn = 0.8f;
                StartEngine();
                _guidedBotT = 0f;
                var keepSky = new Func<Rect>[] { coach.Zone(_skyZone), coach.Zone(_missionFill.rectTransform) };
                yield return StartCoroutine(coach.Watch(() => coach.RectOf(_stripFill.rectTransform), CoachTexts.Piloto.Steer, () => _steeredSeconds >= 1.5f, 12f, keep: keepSky));
                ok = !t.Skipped;
                if (ok) script.Success();
            }

            // 3) tocar una señal de la misión (si no hay una a la vista, nace una)
            if (ok)
            {
                Signal target = FindAlive(true) ?? SpawnForced(new PilotSignalKind(_mission.Shape, _mission.Detail, true, false), PracticeExposure * 2f);
                yield return null;
                if (target.View >= 0)
                {
                    int view = target.View;
                    yield return StartCoroutine(coach.Touch(() => coach.RectOf(_views[view].Root), CoachTexts.Piloto.Catch, circle: true, keep: new Func<Rect>[] { SignalsZone(coach) }));
                    ok = !t.Skipped;
                    if (ok)
                    {
                        if (target.Alive) CatchSignal(target);                  // la práctica hace la jugada (si el toque no llegó —red de seguridad— igual sigue, y nunca se repite el paso)
                        script.Success();
                    }
                }
            }

            // 4) las parecidas: misma forma con otro detalle (una a la vista, que dura lo suficiente)
            if (ok)
            {
                _practiceSpawn = false;
                SpawnForced(new PilotSignalKind(_mission.Shape, SignalDetail.Ring, false, false), 5f);
                yield return null;
                yield return StartCoroutine(coach.Notice(CoachTexts.Piloto.Lookalike, 3.4f, keep: new Func<Rect>[] { SignalsZone(coach), coach.Zone(_missionFill.rectTransform) }));
                ok = !t.Skipped;
                if (ok) script.Success();
            }

            // 5) en cada sector cambia la misión
            if (ok)
            {
                yield return StartCoroutine(coach.Notice(CoachTexts.Piloto.Sector, 2.8f, keep: new Func<Rect>[] { SignalsZone(coach), coach.Zone(_missionFill.rectTransform) }));
                ok = !t.Skipped;
                if (ok) script.Success();
            }
            if (ok) yield return StartCoroutine(coach.Notice(CoachTexts.Ready, 1.5f));
            coach.Hide();

            // se deja todo como estaba (con «Cómo se juega» desde la pausa, el vuelo en curso se conserva)
            _practiceSpawn = false;
            _phase = Phase.Idle;
            StopEngine();
            ClearSignals();
            ClearNotices();
            foreach (var f in _floatViews) { f.Active = false; f.Root.gameObject.SetActive(false); }
            _steerFinger = -1;
            _mouseSteer = false;
            _guided = false;
            _run = savedRun;
            _mission = savedMission;
            _missionFreshAt = savedFresh;
            _t = savedT;
            _dist = savedDist;
            _hyperLeft = savedHyper;
            _sector = savedSector;
            _tintFrom = savedTintFrom;
            _tintTo = savedTintTo;
            _tintMix = savedMix;
            Layout();
            t.EndPractice();
        }
        // </guided>

        private float _guidedBotT;

        private Signal FindAlive(bool target)
        {
            foreach (var s in _signals) if (s.Alive && s.Kind.IsTarget == target && s.View >= 0) return s;
            return null;
        }

        /// <summary>Una señal de la práctica con la clase y la duración que se pidan, en un lugar libre del cielo (si ya hay muchas, retira la más vieja).</summary>
        private Signal SpawnForced(PilotSignalKind kind, float expo)
        {
            int view = FreeView();
            if (view < 0)
            {
                ReleaseView(_signals[0]);
                _signals.RemoveAt(0);
                view = FreeView();
            }
            BuildForbidden();
            _liveSpots.Clear();
            foreach (var s in _signals) if (s.Alive) _liveSpots.Add((s.X, s.Y));
            if (!PilotSpawn.TryPlace(_rng, false, _plan.SkyTop, _plan.SkyBottom, _forbidden, _liveSpots, RouteAtY, out float x, out float y))
            {
                x = PilotPlan.Width * 0.5f + 70f;
                y = (_plan.SkyTop + _plan.SkyBottom) * 0.5f;
            }
            return AddSignal(kind, x, y, expo, view, Now);
        }

        /// <summary>Una zona protegida para el globo de Nubi que cubre TODAS las señales a la vista (o, si no hay ninguna, el cielo de señales): el globo nunca tapa una señal.</summary>
        private Func<Rect> SignalsZone(NubiCoach coach) => () =>
        {
            Rect r = default;
            bool first = true;
            foreach (var s in _signals)
            {
                if (s.View < 0) continue;
                var x = coach.RectOf(_views[s.View].Root);
                r = first ? x : Rect.MinMaxRect(Mathf.Min(r.xMin, x.xMin), Mathf.Min(r.yMin, x.yMin), Mathf.Max(r.xMax, x.xMax), Mathf.Max(r.yMax, x.yMax));
                first = false;
            }
            return first ? coach.RectOf(_skyZone) : r;
        };
    }
}
