using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using NeuroVida.Games.Shared;

namespace NeuroVida.Games.Aterrizaje
{
    public sealed partial class LandingGameController
    {
        private Image _zone;

        // <guided>
        protected override IEnumerator GuidedRound(GuidedTutorial t)
        {
            t.BeginPractice();
            _guided = true;
            while (!_baked) yield return null;
            // «Nubi entrenadora», con las reglas de la Tarea 67 (la práctica ASEGURA, nunca alterna; el smoke mira el estado): UN aterrizaje de práctica que no cuenta (no alimenta el motor, las medidas, las cúpulas ni el marcador). Foco sobre la línea (el juego se congela; el toque ahí es el
            // real: se arrastra la nave y se suelta) con una franja sol sobre el lugar justo; al posarse sube la bandera y un aviso dice cómo salió. Si la nave queda fuera de la franja ancha, se repite; el número grande de la misión queda iluminado junto a la línea.
            Layout();                                                                  // en la práctica el campo es más bajo: los controles de Nubi van abajo
            _phase = Phase.Idle;
            _land = null;
            _missionSmall.gameObject.SetActive(false);
            ClearResultViews();
            _hint.gameObject.SetActive(false);
            var coach = t.Coach;
            var script = new GuidedScript(1);
            var trial = LandingContract.GuidedTrial(_rng);
            var mission = new[] { coach.ZoneOfTexts(_missionNumber) };
            var drag = new[] { mission[0], coach.Zone(_landerRoot) };                  // al empezar, la tarjeta de Nubi no tapa la nave que hay que arrastrar
            while (!script.Finished)
            {
                if (t.Skipped) { script.Skip(); break; }
                float zoneHalf = LandingContract.GuidedZones[0];
                BeginTrial(trial, 14f);                                                // baja despacio (14 s) para que dé tiempo a moverla
                ShowZone(trial, zoneHalf);
                AutoLand(trial, script.Failures);
                yield return null;                                                     // la nave aparece arriba
                yield return StartCoroutine(coach.Touch(() => coach.RectOf(_lineZone),
                    script.Failures == 0 ? CoachTexts.Aterrizaje.Drag(trial.Label) : CoachTexts.Aterrizaje.Again(trial.Label), keep: drag));
                while (_phase == Phase.Fall || _phase == Phase.Drop)
                {
                    if (t.Skipped) break;
                    yield return null;
                }
                if (t.Skipped) { script.Skip(); break; }
                TutorialGuards.Expect(_land != null && _phase == Phase.Land, "Aterrizaje", "tras la bajada guiada la nave no quedó posada y medida");

                if (_land.Err <= zoneHalf)
                {
                    GameFeel.Correct(1);
                    script.Success();
                    yield return StartCoroutine(GuidedNotice(CoachTexts.Aterrizaje.Good, 2.2f, mission, coach));
                }
                else
                {
                    GameFeel.Wrong();
                    script.Failure();
                    yield return StartCoroutine(GuidedNotice(CoachTexts.Aterrizaje.Close(trial.Label), 2.4f, mission, coach));
                }
                HideZone();
                EndTrial();
            }
            HideZone();
            EndTrial();
            _missionSmall.gameObject.SetActive(true);
            if (!script.Skipped)
            {
                Play(LandSfx.Flag);
                yield return StartCoroutine(coach.Notice(CoachTexts.Ready, 1.5f));
            }
            // se deja todo como estaba (con «Cómo se juega» desde la pausa, lo ganado en la partida se conserva)
            _phase = Phase.Idle;
            _land = null;
            _guided = false;
            Layout();
            t.EndPractice();
        }

        /// <summary>El aviso de Nubi con la nave ya posada: la bandera y el tramo siguen a la vista (los dibuja <see cref="AnimateResult"/> con el tiempo del resultado) y el aviso del juego no sale (lo cuenta Nubi).</summary>
        private IEnumerator GuidedNotice(string text, float seconds, System.Func<Rect>[] keep, NubiCoach coach)
        {
            yield return StartCoroutine(coach.Notice(text, seconds, keep));
        }
        // </guided>

        /// <summary>La franja guía sobre el lugar justo: de ± <paramref name="halfFraction"/> del largo de la regla a cada lado del número.</summary>
        private void ShowZone(LandingTrial trial, float halfFraction)
        {
            float tx = LandingPlan.FracX(trial.TargetFraction), w = (LandingPlan.RulerX1 - LandingPlan.RulerX0) * halfFraction * 2f;
            SetRect(_zone.rectTransform, tx, _plan.RulerY + 2f, w, 56f);
            _zone.gameObject.SetActive(true);
        }

        private void HideZone()
        {
            if (_zone != null) _zone.gameObject.SetActive(false);
        }

        /// <summary>SOLO EN EL EDITOR (smoke): la nave se lleva sola. Con «Saltar tutorial» no hace nada. En el primer intento se suelta sin moverla (casi siempre queda fuera de la franja: pasa por el aviso «Casi») y en el segundo se lleva al número y se suelta (pasa por «¡Bien!»). En el teléfono no hace nada.</summary>
        private void AutoLand(LandingTrial trial, int failures)
        {
#if UNITY_EDITOR
            if (GuidedTutorial.EditorAutoContinue) StartCoroutine(EditorAutoLand(trial, failures));
#endif
        }

#if UNITY_EDITOR
        private IEnumerator EditorAutoLand(LandingTrial trial, int failures)
        {
            float waited = 0f;
            while (waited < 1.6f) { waited += GameClock.RealDeltaTime; yield return null; }
            if (_phase != Phase.Fall) yield break;
            if (failures > 0) _tx = LandingPlan.FracX(trial.TargetFraction);
            waited = 0f;
            while (waited < 0.9f) { waited += GameClock.RealDeltaTime; yield return null; }
            if (_phase == Phase.Fall) Release();
        }
#endif
    }
}
