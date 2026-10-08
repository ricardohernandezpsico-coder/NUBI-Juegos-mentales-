using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using NeuroVida.Games.Shared;
using Motion = NeuroVida.Games.Shared.Motion; // UnityEngine.Motion también existe

namespace NeuroVida.Games.Parejas
{
    public sealed partial class ConstelacionGameController
    {
        // ------------------------------------------------------------------ ronda guiada del tutorial (pieza común)

        // <guided>
        protected override IEnumerator GuidedRound(GuidedTutorial t)
        {
            t.BeginPractice();
            _guided = true;
            while (!_baked) yield return null;                         // el arte se hornea mientras Nubi se presenta
            var coach = t.Coach;
            // «Nubi entrenadora» (docs/diseno-constelaciones.md §8), sobre un cielo de 3 parejas que no cuenta: 1) tocar una luz; 2) tocar otra distinta (no son iguales: se cierran); 3) Nubi abre una luz NUEVA cuya
            // compañera es la del paso 1 y pregunta dónde estaba su pareja: el hueco cubre todas las dormidas y solo vale la compañera (a las 2 equivocadas la compañera brilla); 4) la línea dorada: de memoria;
            // 5) la fila «De memoria» de abajo. Después el cielo se termina solo, a tu ritmo. Cada foco congela el juego y el toque en el hueco es el de verdad.
            var script = new GuidedScript(1);                          // un cielo; «Saltar tutorial» lo termina
            _sky = PracticeSky();
            BindBoard(_sky);
            UpdateHud();
            _phase = Phase.Play;
            _inputOn = false;
            _guidedOnly = -1;
            _sky.RestartAppearance(NowMs);
            yield return Motion.Hold(0.9f);                            // las luces terminan de aparecer
            bool ok = !t.Skipped;
            var card = new[] { coach.Zone(() => coach.RectOf(_cardBg.rectTransform)) };
            var row = new[] { coach.Zone(() => coach.RectOf(_rowRect)), coach.Zone(() => coach.RectOf(_cardBg.rectTransform)) };
            var a1 = _sky.Lights[0];
            var a2 = _sky.Lights.First(l => l.Group == a1.Group && l.Id != a1.Id);
            var b1 = _sky.Lights.First(l => l.Group != a1.Group);

            if (ok)
            {
#if UNITY_EDITOR
                StartCoroutine(ProbeLightTap(coach, a1.Id, () => a1.State != ConState.Down));
#endif
                yield return StartCoroutine(coach.Touch(() => HoleOf(a1), CoachTexts.Constelaciones.First, circle: true, keep: card));
                ok = !t.Skipped;
                if (ok) TapLight(a1.Id);                               // el toque en el hueco ES el toque en la luz
            }
            if (ok)
            {
                yield return StartCoroutine(coach.Touch(() => HoleOf(b1), CoachTexts.Constelaciones.Second, circle: true, keep: card));
                ok = !t.Skipped;
                if (ok) TapLight(b1.Id);
            }
            if (ok)
            {
                yield return Motion.Hold(0.9f);                        // se ve cómo las dos se cierran solas
                ok = !t.Skipped;
            }
            if (ok)
            {
                TapLight(a2.Id);                                       // Nubi abre una luz NUEVA cuya compañera es la del paso 1
                yield return Motion.Hold(0.6f);
                _inputOn = true;
                _press = null;
                _guidedOnly = a1.Id;
                _guidedHit = false;
                _guidedMisses = 0;
                var coaching = false;
                while (ok && !_guidedHit)
                {
#if UNITY_EDITOR
                    StartCoroutine(ProbeLightTap(coach, a1.Id, () => _guidedHit));
#endif
                    yield return StartCoroutine(coach.Touch(DormantHole, CoachTexts.Constelaciones.Partner, keep: card));
                    ok = !t.Skipped;
                    if (!ok) break;
                    yield return null;                                 // el toque que cerró el foco llega al juego en este mismo cuadro
                    if (!_guidedHit && _guidedMisses >= 2 && !coaching)
                    {
                        coaching = true;
                        _lv[a1.Id].HintAt = GameClock.Time;            // tras dos toques en otra luz, la compañera brilla
                    }
                }
                _inputOn = false;
                _guidedOnly = -1;
            }
            if (ok)
            {
                yield return Motion.Hold(0.8f);                        // la línea se traza
                var pair = new[] { coach.ZoneOf(_lv[a1.Id].Body.rectTransform, _lv[a2.Id].Body.rectTransform) };
                yield return StartCoroutine(coach.Notice(CoachTexts.Constelaciones.GoldLine, 2.4f, keep: pair));
                ok = !t.Skipped;
            }
            if (ok)
            {
                yield return StartCoroutine(coach.Notice(CoachTexts.Constelaciones.Row, 2.4f, keep: row));
                ok = !t.Skipped;
            }
            if (ok)
            {
                // el resto del cielo, a tu ritmo (no cuenta)
                _inputOn = true;
                _press = null;
                AutoPlaySky();
                while (!_sky.Complete && !t.Skipped) yield return null;
                ok = !t.Skipped;
                if (ok)
                {
                    yield return Motion.Hold(0.65f);
                    yield return StartCoroutine(coach.Notice(CoachTexts.Ready, 1.5f));
                }
            }
            coach.Hide();
            _inputOn = false;
            _guidedOnly = -1;
            _phase = Phase.Idle;
            ResetBoardViews();
            _sky = null;
            _guided = false;
            t.EndPractice();
        }
        // </guided>

        private static readonly int[] PracticeKinds = { 1, 4, 6, 7, 9, 11 };

        /// <summary>El cielo de práctica: 3 parejas de objetos distintos, juntas en la parte de arriba del cielo (abajo queda lugar para Nubi y su globo cuando el hueco cubre todas las dormidas).</summary>
        private ConstelacionSky PracticeSky()
        {
            var kinds = new List<int>(PracticeKinds);
            for (int i = kinds.Count - 1; i > 0; i--) { int j = _rng.Next(i + 1); int tmp = kinds[i]; kinds[i] = kinds[j]; kinds[j] = tmp; }
            var slots = new List<ConPt>();
            foreach (float y in new[] { 58f, 150f }) foreach (float x in new[] { 60f, 166f, 272f }) slots.Add(new ConPt(x, y));
            for (int i = slots.Count - 1; i > 0; i--) { int j = _rng.Next(i + 1); var tmp = slots[i]; slots[i] = slots[j]; slots[j] = tmp; }
            var lights = new List<ConLight>();
            for (int g = 0; g < 3; g++)
                for (int r = 0; r < 2; r++)
                    lights.Add(new ConLight { Kind = kinds[g], Variant = 0, Group = g, Size = 2, Pos = slots[g * 2 + r] });
            return ConstelacionSky.FromLights(ConstelacionContract.Stage(1), lights, ConstelacionLayout.SkyW, ConstelacionLayout.RefSkyH);
        }

        /// <summary>El hueco de una luz: un círculo un poco más grande que ella (unidades de la pantalla).</summary>
        private Rect HoleOf(ConLight l) => _tutorial.Coach.AroundOf(_lv[l.Id].Root, Vector2.one * (2f * _sky.Placement.R * _lay.Scale * _s * 1.15f));

        /// <summary>El hueco que cubre todas las luces dormidas (y solo ellas).</summary>
        private Rect DormantHole()
        {
            Rect r = default;
            bool first = true;
            foreach (var l in _sky.Lights)
            {
                if (l.State != ConState.Down) continue;
                var x = _tutorial.Coach.RectOf(_lv[l.Id].Body.rectTransform);
                r = first ? x : Rect.MinMaxRect(Mathf.Min(r.xMin, x.xMin), Mathf.Min(r.yMin, x.yMin), Mathf.Max(r.xMax, x.xMax), Mathf.Max(r.yMax, x.yMax));
                first = false;
            }
            return r;
        }

        /// <summary>SOLO EN EL EDITOR (smoke): termina el cielo de práctica solo, una pareja cada 0,7 s. En el teléfono no hace nada.</summary>
        private void AutoPlaySky()
        {
#if UNITY_EDITOR
            if (GuidedTutorial.EditorAutoContinue) StartCoroutine(EditorAutoSolve());
#endif
        }

#if UNITY_EDITOR
        private static IEnumerator RealWait(float seconds)
        {
            for (float t = 0f; t < seconds; t += GameClock.RealDeltaTime) yield return null;
        }

        private IEnumerator EditorAutoSolve()
        {
            for (int guard = 0; guard < 40 && _sky != null && !_sky.Complete; guard++)
            {
                yield return RealWait(0.35f);
                if (_sky == null) yield break;
                foreach (var l in _sky.Lights)
                {
                    if (l.State != ConState.Down) continue;
                    foreach (var m in _sky.Lights)
                    {
                        if (m.Id == l.Id || m.State != ConState.Down || m.Key != l.Key) continue;
                        TapLight(l.Id);
                        yield return RealWait(0.2f);
                        if (_sky != null) TapLight(m.Id);
                        break;
                    }
                    break;
                }
            }
        }

        /// <summary>SOLO EN EL EDITOR (smoke del tutorial): da un toque «de verdad» en el centro de la luz pedida, donde la persona tocaría, y comprueba que el paso lo acepta: que se haga lo que ese toque debía hacer
        /// (<paramref name="worked"/>: en el primer paso la luz se abre; en el de la compañera, el juego recibe el toque). Si lo iluminado no coincidiera con la luz dibujada, el tutorial se «pegaría» sin dejar tocar lo que pide;
        /// así se vería antes de llegar al teléfono.</summary>
        private IEnumerator ProbeLightTap(NubiCoach coach, int lightId, System.Func<bool> worked)
        {
            if (!GuidedTutorial.EditorAutoContinue) yield break;
            float w = 0f;
            while (!coach.Active && w < 3f) { w += GameClock.RealDeltaTime; yield return null; }
            w = 0f;
            while (coach.Active && w < 0.7f) { w += GameClock.RealDeltaTime; yield return null; }
            if (!coach.Active) yield break;
            GuidedTutorial.EditorPressPos = RectTransformUtility.WorldToScreenPoint(null, _lv[lightId].Root.position);
            GuidedTutorial.EditorPressFrame = Time.frameCount + 1;
            for (int i = 0; i < 8 && !worked(); i++) yield return null;
            if (!worked())
            {
                var root = (RectTransform)coach.transform;
                RectTransformUtility.ScreenPointToLocalPointInRectangle(root, GuidedTutorial.EditorPressPos, null, out var local);
                var hole = coach.AroundOf(_lv[lightId].Root, Vector2.one * (2f * _sky.Placement.R * _lay.Scale * _s * 1.15f));
                Debug.LogError("[SmokeTest] Constelaciones: un toque en el centro de la luz pedida NO fue aceptado por el paso del tutorial (el hueco no coincide con la luz): toque " + local + ", hueco de esa luz " + hole + ", luz " + lightId + " en estado " + _sky.Lights[lightId].State + ", raíz " + root.rect);
            }
            else Debug.Log("[SmokeTest] Constelaciones: un toque en el centro de la luz pedida fue aceptado por el paso del tutorial");
        }
#endif
    }
}
