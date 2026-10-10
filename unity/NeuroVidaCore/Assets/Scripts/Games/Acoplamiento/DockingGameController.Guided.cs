using System;
using System.Collections;
using UnityEngine;
using NeuroVida.Games.Shared;

namespace NeuroVida.Games.Acoplamiento
{
    public sealed partial class DockingGameController
    {
        /// <summary>La pieza de la práctica: una L de 4 bloques (siempre quiral).</summary>
        private static Cell[] PracticeShape() => new[] { new Cell(0, 0), new Cell(0, 1), new Cell(0, 2), new Cell(1, 0) };

        // ------------------------------------------------------------------ ronda guiada del tutorial (pieza común)

        // <guided>
        protected override IEnumerator GuidedRound(GuidedTutorial t)
        {
            t.BeginPractice();
            _guided = true;
            while (!_baked) yield return null;
            // «Nubi entrenadora» (docs/diseno-acoplamiento.md §9), sobre tres módulos de práctica que no cuentan (no alimentan el motor, las medidas ni el récord): 1) el puerto («el hueco tiene la forma de la pieza»); 2) llega un módulo girado 90° que SÍ encaja: «¿calza en el hueco?»
            // y un paso de Tocar «Encaja» (toque real); 3) el módulo gira, baja y encaja: se suma a la estación; 4) llega un espejo (L girada 45°) y se muestra sin tocar el giro y la vuelta que lo hace calzar: «Si está dado vuelta, toca Espejo»; 5) otro espejo (90°): «¿Y este?» con un paso de
            // Tocar «Espejo» (toque real) y su revelación; 6) «¡Listo!». «La práctica asegura, nunca alterna»: el toque de un paso de Tocar solo lo recibe el paso (la entrada del juego está apagada) y la práctica registra la respuesta si no estaba, con guardias de estado para el smoke.
            int savedShown = _stationShown;
            int savedStreak = _streak;
            _stationShown = 0;
            _streak = 0;
            Layout();                                                                  // en la práctica el campo es más bajo: los controles del tutorial van abajo
            _inputOn = false;
            _phase = Phase.Idle;
            HideModule();
            SetNotice("", Lime, false);
            SetFuelVisible(false);
            _hint.gameObject.SetActive(false);
            RefreshStation();
            RefreshButtons(Now);
            var coach = t.Coach;
            var script = new GuidedScript(6);
            bool ok = !t.Skipped;
            var shape = PracticeShape();
            Func<Rect> portZone = coach.Zone(() => PortRect(coach));
            Func<Rect> moduleZone = coach.Zone(() => coach.AroundOf(_moduleRoot, Vector2.one * (ModuleRadius() * 2f * _s)));
            Func<Rect> stationZone = coach.Zone(_stationZone);
            Func<Rect> buttonsZone = coach.ZoneOf(new[] { _buttons[0].Root, _buttons[1].Root });

            // 1) el puerto: el hueco tiene la forma de la pieza
            if (ok)
            {
                BeginTrial(new DockingTrial { Shape = shape, Mirrored = false, AngleDeg = 90, Level = 1 });
                yield return null;
                yield return StartCoroutine(coach.Watch(() => PortRect(coach), CoachTexts.Acoplamiento.Port, () => false, 3.4f));
                ok = !t.Skipped;
                if (ok) script.Success();
            }

            // 2) llega un módulo girado que SÍ encaja: «Si calza, toca Encaja» (toque real)
            if (ok)
            {
                yield return StartCoroutine(DoArrive());
                _phase = Phase.Decide;
                _answer = NoAnswer;
                _decideAt = Now;
                RefreshButtons(Now);
                yield return StartCoroutine(coach.Watch(() => coach.AroundOf(_moduleRoot, Vector2.one * (ModuleRadius() * 2f * _s)), CoachTexts.Acoplamiento.Arrives, () => false, 3.8f, circle: true, keep: new[] { portZone }));
                ok = !t.Skipped;
                if (ok)
                {
                    yield return StartCoroutine(coach.Touch(() => coach.RectOf(_buttons[0].Root), CoachTexts.Acoplamiento.Fits, keep: new[] { moduleZone, portZone }));
                    ok = !t.Skipped;
                    if (ok) { EnsureAnswer(DockingContract.AnswerFits); script.Success(); }          // la práctica ASEGURA la respuesta: si ya está, no se toca (el toque del paso no llega al juego)
                    TutorialGuards.Expect(!ok || _answer == DockingContract.AnswerFits, "Acoplamiento", "tras el toque guiado en «Encaja» la respuesta no quedó registrada");
                }
            }

            // 3) el módulo gira, baja y encaja: se suma a la estación
            if (ok)
            {
                bool correct = DockingContract.IsCorrect(_trial.Mirrored, _answer);
                TutorialGuards.Expect(correct, "Acoplamiento", "la primera práctica (un módulo que encaja) no terminó acertada");
                var reveal = StartCoroutine(RevealRoutine(correct, _answer));
                yield return null;
                yield return StartCoroutine(coach.Watch(() => coach.RectOf(_stationZone), CoachTexts.Acoplamiento.Docks, () => !_revealing, 6f, keep: new[] { portZone }));
                ok = !t.Skipped;
                while (ok && _revealing) { yield return null; ok = !t.Skipped; }
                if (ok) script.Success();
                if (reveal != null) StopCoroutine(reveal);
                TutorialGuards.Expect(!ok || _stationShown == 1, "Acoplamiento", "tras la primera práctica la estación debía tener 1 módulo y tiene " + _stationShown);
                _phase = Phase.Idle;
                HideModule();
            }

            // 4) llega un espejo: se muestra sin tocar el giro y la vuelta que lo hace calzar
            if (ok)
            {
                BeginTrial(new DockingTrial { Shape = shape, Mirrored = true, AngleDeg = 45, Level = 1 });
                yield return null;
                yield return StartCoroutine(DoArrive());
                _phase = Phase.Decide;
                RefreshButtons(Now);
                yield return StartCoroutine(coach.Watch(() => coach.AroundOf(_moduleRoot, Vector2.one * (ModuleRadius() * 2f * _s)), CoachTexts.Acoplamiento.MirrorSeen, () => false, 3.2f, circle: true, keep: new[] { portZone }));
                ok = !t.Skipped;
                if (ok)
                {
                    var demo = StartCoroutine(RevealRoutine(true, DockingContract.AnswerMirror, demo: true));
                    yield return null;
                    while (ok && _revealing) { yield return null; ok = !t.Skipped; }
                    if (demo != null) StopCoroutine(demo);
                    _phase = Phase.Idle;
                    HideModule();
                    if (ok) yield return StartCoroutine(coach.Notice(CoachTexts.Acoplamiento.MirrorTouch, 2.6f, keep: new[] { buttonsZone, portZone }));
                    ok = !t.Skipped;
                    if (ok) script.Success();
                }
            }

            // 5) otro espejo (90°): «¿Y este?» con un paso de Tocar «Espejo» (toque real) y su revelación
            if (ok)
            {
                BeginTrial(new DockingTrial { Shape = shape, Mirrored = true, AngleDeg = 90, Level = 1 });
                yield return null;
                yield return StartCoroutine(DoArrive());
                _phase = Phase.Decide;
                _answer = NoAnswer;
                _decideAt = Now;
                RefreshButtons(Now);
                yield return StartCoroutine(coach.Touch(() => coach.RectOf(_buttons[1].Root), CoachTexts.Acoplamiento.AndThis, keep: new[] { moduleZone, portZone }));
                ok = !t.Skipped;
                if (ok) { EnsureAnswer(DockingContract.AnswerMirror); script.Success(); }
                TutorialGuards.Expect(!ok || _answer == DockingContract.AnswerMirror, "Acoplamiento", "tras el toque guiado en «Espejo» la respuesta no quedó registrada");
                if (ok)
                {
                    bool correct = DockingContract.IsCorrect(_trial.Mirrored, _answer);
                    TutorialGuards.Expect(correct, "Acoplamiento", "la segunda práctica (un espejo) no terminó acertada");
                    var reveal = StartCoroutine(RevealRoutine(correct, _answer));
                    yield return null;
                    while (ok && _revealing) { yield return null; ok = !t.Skipped; }
                    if (reveal != null) StopCoroutine(reveal);
                    TutorialGuards.Expect(!ok || _stationShown == 2, "Acoplamiento", "tras las dos prácticas la estación debía tener 2 módulos y tiene " + _stationShown);
                    if (ok) script.Success();
                }
            }
            if (ok) yield return StartCoroutine(coach.Notice(CoachTexts.Ready, 1.5f));
            coach.Hide();

            // se deja todo como estaba (con «Cómo se juega» desde la pausa, lo ganado en la partida se conserva)
            _phase = Phase.Idle;
            _inputOn = false;
            _landingPending = false;
            _revealDemo = false;
            _revealing = false;
            _shownAnswer = NoAnswer;
            HideModule();
            SetNotice("", Lime, false);
            _guided = false;
            _stationShown = savedShown;
            _streak = savedStreak;
            Layout();
            t.EndPractice();
        }
        // </guided>

        /// <summary>El panel del puerto tal como se ve (168 × 146 dp, escalado): su sprite es un cuadrado más grande (con el borde y la sombra) y iluminar ese cuadrado dejaba ver franjas de los botones.</summary>
        private Rect PortRect(NubiCoach coach) => coach.AroundOf(_portPanel.rectTransform, new Vector2(_plan.PortW, _plan.PortH) * _s);

        /// <summary>«La práctica asegura, nunca alterna»: registra la respuesta de la práctica solo si todavía no hay una (el toque de un paso de Tocar no llega al juego: su entrada está apagada).</summary>
        private void EnsureAnswer(int answer)
        {
            if (_answer == NoAnswer) PressButton(answer);
        }
    }
}
