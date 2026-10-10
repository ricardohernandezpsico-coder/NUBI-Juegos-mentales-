using System.Collections;
using System.Linq;
using UnityEngine;
using NeuroVida.Games.Shared;
using Motion = NeuroVida.Games.Shared.Motion; // UnityEngine.Motion también existe

namespace NeuroVida.Games.Correo
{
    public sealed partial class MailGameController
    {
        // ------------------------------------------------------------------ ronda guiada del tutorial (pieza común)

        // <guided>
        protected override IEnumerator GuidedRound(GuidedTutorial t)
        {
            t.BeginPractice();
            _guided = true;
            while (!_baked) yield return null;
            var coach = t.Coach;
            // «Nubi entrenadora» (docs/diseno-correo-estacion.md §9), sobre un día de práctica de 30 s que no cuenta (3 buzones, un encargo con sello dorado y uno de hora): 1) el buzón del sello; 2) la hoja del día;
            // 3) llega una carta con sello dorado: a la caja fuerte; 4) el reloj tapado; 5) cuando llega la ventana de la hora, el faro (y la nave del correo). Cada foco congela el juego y el toque en el hueco es el de verdad.
            var script = new GuidedScript(5);
            MailMoment? routine = null;
            _day = MailDay.Create(2, 1, ref routine, _rng, MailContract.PracticeSeconds);
            _day.CancelTodo = null;
            _day.RushPlan.Clear();                                     // en la práctica no cae ningún saco
            BindDay(_day);
            UpdateHud();
            _phase = Phase.Play;
            _inputOn = false;
            _guidedFreeze = false;
            _guidedAllowFree = false;
            _day.Begin();
            var belt = new[] { coach.Zone(() => coach.RectOf(_beltBand.rectTransform)) };
            bool ok = !t.Skipped;

            // 1) la primera carta llega a su lugar: tocar el buzón de su sello
            while (ok && !FrontReady()) { yield return null; ok = !t.Skipped; }
            if (ok)
            {
                _guidedFreeze = true;
                var first = _day.Belt[0];
#if UNITY_EDITOR
                StartCoroutine(ProbeTap(coach, () => ScreenOf(_boxes[first.Planet].Body.rectTransform), () => _day.Sorted > 0));
#endif
                yield return StartCoroutine(coach.Touch(() => coach.RectOf(_boxes[first.Planet].Body.rectTransform), CoachTexts.Correo.First, keep: belt));
                ok = !t.Skipped;
                if (ok) { DoBox(first.Planet); script.Success(); }
                TutorialGuards.Expect(!ok || _day.Sorted > 0, "La estación de correo", "tras el toque guiado la primera carta no quedó en su buzón");
            }
            if (ok)
            {
                yield return Motion.Hold(0.6f);                        // la carta vuela al buzón
                ok = !t.Skipped;
            }

            // 2) la hoja del día: el encargo del sello dorado (se da en la mañana y durante el día no se ve)
            if (ok)
            {
                var gold = _day.Todos.First(x => !x.IsTime);
                _briefItems.Clear();
                _briefItems.Add(gold);
                _briefRoutineCard = false;
                _briefTag.text = MailContract.DayTitle(1);
                FillBrief();
                SetBriefButton(false);
                ShowOnly(_briefLayer);
                SetGroupAlpha(_briefLayer, 1f);
                var sheet = new[] { coach.Zone(() => coach.RectOf(_briefRows[0].Root)) };
                yield return StartCoroutine(coach.Notice(CoachTexts.Correo.Sheet, 3f, keep: sheet));
                ok = !t.Skipped;
                ShowOnly(null);
                SetBriefButton(true);
            }

            // 3) llega una carta con sello dorado: es la del encargo, a la caja fuerte
            if (ok)
            {
                ClearBelt();
                _day.Belt.Clear();
                _day.Belt.Add(new MailLetter { Planet = _rng.Next(_day.Stage.Boxes), Cue = MailCue.Gold });
                while (ok && !FrontReady()) { yield return null; ok = !t.Skipped; }
            }
            if (ok)
            {
#if UNITY_EDITOR
                StartCoroutine(ProbeTap(coach, () => ScreenOf(_safeBody.rectTransform), () => _day.CueHits > 0));
#endif
                yield return StartCoroutine(coach.Touch(() => coach.RectOf(_safeBody.rectTransform), CoachTexts.Correo.Gold, keep: belt));
                ok = !t.Skipped;
                if (ok) { DoSafe(); script.Success(); }
            }
            if (ok)
            {
                yield return Motion.Hold(0.6f);
                ok = !t.Skipped;
            }

            // 4) el encargo con hora: el reloj va tapado, se toca para mirar la hora
            if (ok)
            {
#if UNITY_EDITOR
                StartCoroutine(ProbeTap(coach, () => ScreenOf(_clockFill.rectTransform), () => _day.Peeks.Count > 0));
#endif
                yield return StartCoroutine(coach.Touch(() => coach.RectOf(_clockRim.rectTransform), CoachTexts.Correo.Clock, circle: true, keep: belt));
                ok = !t.Skipped;
                if (ok) { DoClock(); script.Success(); }
            }
            if (ok)
            {
                var panel = new[] { coach.Zone(() => coach.RectOf(_peekBg.rectTransform)) };
                yield return StartCoroutine(coach.Notice(CoachTexts.Correo.Hour, 1.8f, keep: panel));
                ok = !t.Skipped;
            }

            // 5) el reloj salta a justo antes de la hora; el día corre libre (se pueden clasificar cartas) hasta que se abre la ventana: el faro
            if (ok)
            {
                var hour = _day.Todos.First(x => x.IsTime);
                _day.JumpTo(hour.W0 - 0.03f);
                _guidedFreeze = false;
                _guidedAllowFree = true;
                _inputOn = true;
                _press = null;
                while (ok && _day.Fraction < hour.W0 + 0.005f) { yield return null; ok = !t.Skipped; }
                _guidedFreeze = true;
                _guidedAllowFree = false;
                _inputOn = false;
            }
            if (ok)
            {
#if UNITY_EDITOR
                StartCoroutine(ProbeTap(coach, () => ScreenOf(_bBody.rectTransform), () => _day.Todos.Any(x => x.IsTime && x.State == MailState.Hit)));
#endif
                yield return StartCoroutine(coach.Touch(() => coach.RectOf(_bBody.rectTransform), CoachTexts.Correo.Beacon, keep: belt));
                ok = !t.Skipped;
                if (ok) { DoBeacon(); script.Success(); }
            }
            if (ok)
            {
                yield return StartCoroutine(coach.Notice(CoachTexts.Correo.Show, 3.2f));
                ok = !t.Skipped;
            }
            if (ok) yield return StartCoroutine(coach.Notice(CoachTexts.Ready, 1.5f));
            coach.Hide();
            _inputOn = false;
            _guidedFreeze = false;
            _guidedAllowFree = false;
            _phase = Phase.Idle;
            ResetViews();
            _day = null;
            ShowOnly(null);
            _guided = false;
            t.EndPractice();
        }
        // </guided>

        /// <summary>En la ronda guiada, mientras el día corre libre solo se aceptan las cartas (los buzones y la caja fuerte); los demás toques los explica un paso.</summary>
        private bool GuidedAllows(Target target, int box) => _guidedAllowFree && (target == Target.Box || target == Target.Safe);

        private void SetBriefButton(bool on)
        {
            _briefBtn.gameObject.SetActive(on);
            _briefBtnRim.gameObject.SetActive(on);
        }

        private Vector2 ScreenOf(RectTransform rt) => RectTransformUtility.WorldToScreenPoint(null, rt.position);

#if UNITY_EDITOR
        /// <summary>SOLO EN EL EDITOR (smoke del tutorial): da un toque «de verdad» en el centro de lo que el paso ilumina, donde la persona tocaría, y comprueba que el paso lo acepta (<paramref name="worked"/>: que se haya
        /// hecho lo que ese toque debía hacer). Si lo iluminado no coincidiera con lo dibujado, el tutorial se «pegaría» sin dejar tocar lo que pide; así se vería antes de llegar al teléfono.</summary>
        private IEnumerator ProbeTap(NubiCoach coach, System.Func<Vector2> screenPos, System.Func<bool> worked)
        {
            if (!GuidedTutorial.EditorAutoContinue) yield break;
            float w = 0f;
            while (!coach.Active && w < 3f) { w += GameClock.RealDeltaTime; yield return null; }
            w = 0f;
            while (coach.Active && w < 0.7f) { w += GameClock.RealDeltaTime; yield return null; }
            if (!coach.Active) yield break;
            GuidedTutorial.EditorPressPos = screenPos();
            GuidedTutorial.EditorPressFrame = Time.frameCount + 1;
            for (int i = 0; i < 8 && !worked(); i++) yield return null;
            if (!worked()) Debug.LogError("[SmokeTest] Correo: un toque en el centro de lo que pide el paso del tutorial NO fue aceptado (lo iluminado no coincide con lo dibujado): toque " + GuidedTutorial.EditorPressPos);
            else Debug.Log("[SmokeTest] Correo: un toque en el centro de lo que pide el paso fue aceptado por el tutorial");
        }
#endif
    }
}
