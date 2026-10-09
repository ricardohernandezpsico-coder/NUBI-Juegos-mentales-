using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using NeuroVida.Games.Shared;

namespace NeuroVida.Games.Radar
{
    public sealed partial class RadarGameController
    {
        private bool _practiceReady;
        private Coroutine _practiceFlash, _practiceReveal;

        // ------------------------------------------------------------------ ronda guiada del tutorial (pieza común)

        // <guided>
        protected override IEnumerator GuidedRound(GuidedTutorial t)
        {
            t.BeginPractice();
            _guided = true;
            while (!_baked) yield return null;
            // «Nubi entrenadora» (docs/diseno-rescate.md §13), sobre dos rondas de práctica que no cuentan (no alimentan el motor ni las medidas): 1) «Mira el radar: un relámpago mostrará unas cápsulas» (foco en el radar; práctica de 2 cápsulas, sin rocas y con un
            // destello largo de 1200 ms); 2) tocar en el tablero las que viste (un paso de Tocar por cada una: el hueco es su botón y el toque en el hueco es el de verdad: la práctica hace la jugada, sin bucles); 3) «Ahora toca ¡Rescatar!» (hueco: el botón);
            // 4) «¡Las rescatadas suben a tu nave!» (foco en la nave, con el rayo tractor); 5) segunda práctica (2 cápsulas, 1 roca, 800 ms): «Las rocas grises no se rescatan: no están en el tablero»; 6) «¡Listo!».
            var savedSeats = new List<CapsuleType>(_seats);
            int savedShown = _shownRescued;
            Layout();                                                                  // en la práctica el campo es más bajo: los controles del tutorial van abajo
            _seats.Clear();
            _shownRescued = 0;
            RefreshSeats();
            UpdateHudExtras();
            HideRoundObjects();
            SetBoard(BoardMode.Dim);
            SetMessage("", "");
            var coach = t.Coach;
            var script = new GuidedScript(6);
            bool ok = !t.Skipped;
            Func<Rect> boardZone = coach.ZoneOf(BoardRoots());

            // 1) mirar el radar: el destello llega solo, con la práctica de 2 cápsulas
            var r1 = RadarContract.Custom(2, 0, 1200, new System.Random(11));
            var types1 = new List<CapsuleType>(r1.Types());
            if (ok)
            {
                BeginRoundObjects(r1);
                _practiceReady = false;
                _boardLayer.gameObject.SetActive(false);                   // el tablero aparece después del destello (paso 2): así Nubi tiene dónde ponerse y no hay nada que mirar fuera del radar
                _practiceFlash = StartCoroutine(PracticeFlash(1.8f));
                yield return StartCoroutine(coach.Watch(() => coach.RectOf(_radarZone), CoachTexts.Radar.Watch, () => _practiceReady, 14f, circle: true));
                ok = !t.Skipped;
                if (ok) script.Success();
                while (ok && !_practiceReady) { yield return null; ok = !t.Skipped; }
            }

            // 2) tocar en el tablero las que viste (una por una)
            for (int n = 0; ok && n < types1.Count; n++)
            {
                var type = types1[n];
                int cell = Array.IndexOf(RadarContract.BoardOrder, type);
                yield return StartCoroutine(coach.Touch(() => coach.RectOf(_buttons[cell].Root), n == 0 ? CoachTexts.Radar.First : CoachTexts.Radar.Second));
                ok = !t.Skipped;
                if (ok) { ToggleType(type, cell); script.Success(); }              // la práctica marca (el toque era del paso, no del juego): si el toque no llegó (red de seguridad) igual sigue, y nunca se repite el paso
            }

            // 3) rescatar
            if (ok)
            {
                yield return StartCoroutine(coach.Touch(() => coach.RectOf(_go.Root), CoachTexts.Radar.Rescue));
                ok = !t.Skipped;
                if (ok) { PressGo(); script.Success(); }
            }

            // 4) las rescatadas suben a la nave
            if (ok)
            {
                _practiceReveal = StartCoroutine(DoReveal());
                yield return null;
                yield return StartCoroutine(coach.Watch(() => coach.RectOf(_shipZone), CoachTexts.Radar.Ship, () => _phase != Phase.Reveal, 5f, keep: new[] { boardZone }));
                ok = !t.Skipped;
                while (ok && _phase == Phase.Reveal) { yield return null; ok = !t.Skipped; }
                if (ok) script.Success();
            }

            // 5) las rocas: segunda práctica (2 cápsulas, 1 roca, 800 ms); después de la estática se explica y la práctica contesta sola
            if (ok)
            {
                var r2 = RadarContract.Custom(2, 1, 800, new System.Random(5));
                BeginRoundObjects(r2);
                _practiceReady = false;
                _practiceFlash = StartCoroutine(PracticeFlash(1.0f));
                while (ok && !_practiceReady) { yield return null; ok = !t.Skipped; }
                if (ok)
                {
                    yield return StartCoroutine(coach.Notice(CoachTexts.Radar.Rocks, 3.4f, keep: new[] { boardZone, coach.Zone(_go.Root) }));
                    ok = !t.Skipped;
                }
                if (ok)
                {
                    foreach (var type in new List<CapsuleType>(r2.Types())) ToggleType(type, Array.IndexOf(RadarContract.BoardOrder, type));
                    PressGo();
                    _practiceReveal = StartCoroutine(DoReveal());
                    yield return null;
                    while (ok && _phase == Phase.Reveal) { yield return null; ok = !t.Skipped; }
                    if (ok) script.Success();
                }
            }
            if (ok) yield return StartCoroutine(coach.Notice(CoachTexts.Ready, 1.5f));
            coach.Hide();

            // se deja todo como estaba (con «Cómo se juega» desde la pausa, lo ganado en la partida se conserva)
            if (_practiceFlash != null) StopCoroutine(_practiceFlash);
            if (_practiceReveal != null) StopCoroutine(_practiceReveal);
            _practiceFlash = _practiceReveal = null;
            _phase = Phase.Idle;
            _picks.Clear();
            _mask.gameObject.SetActive(false);
            HideRoundObjects();
            _lifts.Clear();
            _drifting.Clear();
            foreach (var b in _beams) b.gameObject.SetActive(false);
            foreach (var f in _flying) f.gameObject.SetActive(false);
            foreach (var sp in _sparks) sp.gameObject.SetActive(false);
            _boardLayer.gameObject.SetActive(true);
            SetBoard(BoardMode.Dim);
            SetMessage("", "");
            _beamOn = true;
            _guided = false;
            _seats.Clear();
            _seats.AddRange(savedSeats);
            _shownRescued = savedShown;
            RefreshSeats();
            UpdateHudExtras();
            Layout();
            t.EndPractice();
        }
        // </guided>

        /// <summary>El radar gira un rato, llega el destello, la estática, y la práctica queda lista para responder.</summary>
        private IEnumerator PracticeFlash(float watchSeconds)
        {
            _phase = Phase.Watch;
            _beamOn = true;
            SetBoard(BoardMode.Dim);
            SetMessage(RadarContract.WatchMessage, "", Soft);
            yield return Wait(watchSeconds);
            yield return StartCoroutine(DoFlash());
            yield return StartCoroutine(DoMask());
            _boardLayer.gameObject.SetActive(true);
            _phase = Phase.Answer;
            _picks.Clear();
            _rescueTapped = false;
            SetBoard(BoardMode.Live);
            SetMessage(RadarContract.AskMessage(_round.Count), RadarContract.AskHint, Cyan);
            _practiceReady = true;
        }

        private RectTransform[] BoardRoots()
        {
            var r = new RectTransform[_buttons.Length];
            for (int i = 0; i < r.Length; i++) r[i] = _buttons[i].Root;
            return r;
        }
    }
}
