using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using NeuroVida.Games.Shared;
using Motion = NeuroVida.Games.Shared.Motion; // UnityEngine.Motion también existe

namespace NeuroVida.Games.Satelites
{
    public sealed partial class SatelliteGameController
    {
        private bool _trackDone;
        private Coroutine _practiceTrack, _practiceReveal;

        // ------------------------------------------------------------------ ronda guiada del tutorial (pieza común)

        // <guided>
        protected override IEnumerator GuidedRound(GuidedTutorial t)
        {
            t.BeginPractice();
            _guided = true;
            while (!_baked) yield return null;
            // «Nubi entrenadora» (docs/diseno-satelites.md §10), sobre una ronda de práctica que no cuenta (2 de 5, lenta, sin sorpresas): 1) «Algunos traen un mensaje», con los dos encendidos; 2) «Síguelos con la vista» (el aviso va con los satélites quietos y la persona mira girar sola);
            // 3) tocar cada uno de los que traían mensaje (hueco = ese satélite; el toque en el hueco es el de verdad y la ronda hace la jugada, sin bucles); 4) «¡Cada mensaje enciende una luz!»; 5) «¡Listo!».
            var savedSpots = new List<(float x, float y)>(_lightSpots);              // «Cómo se juega» desde la pausa: las luces ganadas se guardan y se devuelven
            int savedLights = _lightsShown;
            UpdateHud();                                                               // el marcador no queda vacío mientras se practica
            Layout();                                                                  // en la práctica el campo es más bajo (los controles del tutorial van abajo) y el pie no se ve
            ResetPlanet();
            var coach = t.Coach;
            var script = new GuidedScript(5);
            _level = 2;
            _surprise = Surprise.None;
            _spec = SatelliteContract.Practice;
            BeginRoundObjects(_spec, _surprise);
            _inputOn = false;
            _phase = Phase.Ready;
            SetPrompt(SatelliteContract.CueTitle(_spec.K), SatelliteContract.CueSub, new Color(237f / 255f, 234f / 255f, 251f / 255f));
            var targets = new List<int>();
            for (int i = 0; i < _swarm.Count; i++) if (_swarm.Target[i]) targets.Add(i);
            bool ok = !t.Skipped;
            Func<Rect>[] keepRects = null;

            // 1) algunos traen un mensaje: aro dorado y sobre
            if (ok)
            {
                _phase = Phase.Cue;
                foreach (int i in targets) SetCue(i, 1f, 1f);
                for (int j = 0; j < targets.Count; j++) PlayClip(SatelliteSounds.CueBell(j), 0.7f);
                keepRects = new Func<Rect>[targets.Count];
                for (int j = 0; j < targets.Count; j++)
                {
                    int i = targets[j];
                    keepRects[j] = coach.Zone(() => coach.RectOf(_sats[i].Root));
                }
                yield return StartCoroutine(coach.Notice(CoachTexts.Satelites.Message, 3.2f, keep: keepRects));
                ok = !t.Skipped;
                if (ok) script.Success();
            }

            // 2) «Síguelos con la vista»: el aviso se da con los satélites todavía quietos (y los dos con su aro) y se va ANTES de que giren. Un foco de «Mirar» no cabe acá: las órbitas ocupan todo el campo y Nubi y su globo no tendrían dónde ponerse
            //    sin tapar el juego; mientras giran, la persona mira sola, como en la partida (la ronda de práctica no se acelera ni se frena por el tutorial).
            if (ok)
            {
                yield return StartCoroutine(coach.Notice(CoachTexts.Satelites.Follow, 2.4f, keep: keepRects));
                ok = !t.Skipped;
                if (ok) script.Success();
            }
            if (ok)
            {
                float f = 0f;
                while (Motion.Decorative && f < SatelliteContract.CueFadeSeconds)
                {
                    f += GameClock.DeltaTime;
                    foreach (int i in targets) SetCue(i, Mathf.Clamp01(1f - f / SatelliteContract.CueFadeSeconds), 1f);
                    yield return null;
                }
                foreach (int i in targets) HideCue(i);
                _trackDone = false;
                _practiceTrack = StartCoroutine(PracticeTrack());
                while (ok && !_trackDone) { yield return null; ok = !t.Skipped; }
            }

            // 3) tocar cada uno de los que traían mensaje
            for (int n = 0; ok && n < targets.Count; n++)
            {
                int i = targets[n];
                _phase = Phase.Answer;
                UpdateAnswerPrompt();
                yield return StartCoroutine(coach.Touch(() => coach.RectOf(_sats[i].Root), n == 0 ? CoachTexts.Satelites.First : CoachTexts.Satelites.Second, circle: true));
                ok = !t.Skipped;
                if (ok) { ToggleMark(i); script.Success(); }              // la ronda marca (el toque era del paso, no del juego: la entrada está apagada durante la práctica, así que el toque no marca dos veces); si el toque no llegó (red de seguridad) igual sigue, y nunca se repite el paso
                TutorialGuards.Expect(!ok || _marked[i], "Satélites", "tras el toque guiado el satélite " + (n + 1) + " no quedó marcado");
            }
            TutorialGuards.Expect(!ok || MarkedCount() == targets.Count, "Satélites", "al terminar los toques guiados hay " + MarkedCount() + " marcados y eran " + targets.Count);

            // 4) cada mensaje enciende una luz
            if (ok)
            {
                yield return Motion.Hold(SatelliteContract.EvalDelaySeconds);
                _practiceReveal = StartCoroutine(DoReveal());
                yield return null;
                yield return StartCoroutine(coach.Notice(CoachTexts.Satelites.Lights, 3.2f));
                ok = !t.Skipped;
                while (ok && _phase == Phase.Reveal) { yield return null; ok = !t.Skipped; }
                if (ok) script.Success();
            }
            if (ok) yield return StartCoroutine(coach.Notice(CoachTexts.Ready, 1.5f));
            coach.Hide();

            // se deja todo como estaba
            if (_practiceTrack != null) StopCoroutine(_practiceTrack);
            if (_practiceReveal != null) StopCoroutine(_practiceReveal);
            _practiceTrack = _practiceReveal = null;
            StopHum();
            _inputOn = false;
            _phase = Phase.Idle;
            ClearRoundViews();
            foreach (var f in _flights) { f.Active = false; f.Root.gameObject.SetActive(false); }
            _guided = false;
            ResetPlanet();
            RestoreLights(savedSpots, savedLights);
            Layout();
            t.EndPractice();
        }
        // </guided>

        private IEnumerator PracticeTrack()
        {
            yield return StartCoroutine(DoTrack());
            _trackDone = true;
        }

        /// <summary>Devuelve al planeta las luces ganadas antes de «Cómo se juega» (la práctica enciende y apaga las suyas).</summary>
        private void RestoreLights(List<(float x, float y)> spots, int count)
        {
            _lightsShown = count;
            for (int i = 0; i < spots.Count && i < _lightDots.Length; i++)
            {
                var d = _lightDots[i];
                d.Spot = new Vector2(spots[i].x, spots[i].y);
                SetChild(d.Root, d.Spot.x, d.Spot.y, 18f, 18f);
                d.On = true;
                d.At = Now - 1f;
                d.Root.gameObject.SetActive(true);
                _lightSpots.Add(spots[i]);
            }
            UpdateFooter();
        }
    }
}
