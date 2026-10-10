#if UNITY_EDITOR
using System;
using System.Collections;
using UnityEngine;
using NeuroVida.Games.Shared;

namespace NeuroVida.Games.Acoplamiento
{
    public sealed partial class DockingGameController
    {
        /// <summary>true cuando el guion de capturas llegó al final.</summary>
        public bool EditorShotFinished { get; private set; }

        /// <summary>-1 = la revelación corre normal; ≥ 0 = se sostiene en ese segundo de la revelación mientras el guion saca las fotos (a mitad del giro, de la vuelta, del vuelo…).</summary>
        private float _editorRevealT = -1f;

        /// <summary>> 0 = el tiempo para decidir (s) de los módulos que vienen (así «Sin tiempo» no espera 6 s).</summary>
        private float _editorDeadline;

        /// <summary>Fuerzan el próximo módulo: <c>_editorMirror</c> (−1 al azar, 0 igual, 1 espejo) y <c>_editorAngle</c> (grados; <see cref="NoAngle"/> al azar).</summary>
        private int _editorMirror = -1, _editorAngle = NoAngle;
        private const int NoAngle = -1000;

        /// <summary>
        /// SOLO EN EL EDITOR. El guion de las capturas de pantalla reales de Acoplamiento (<c>verificar-todo.sh --capturas Acoplamiento</c>, docs/previews/capturas/acoplamiento.png): lleva la partida (en Reto, para que se vea la barra de «Tiempo») por los momentos que se fotografían usando el flujo REAL del juego: la llegada de un módulo,
        /// la respuesta, la vuelta del espejo, el acople, el vuelo a la estación, «¡Anillo completo!», un error, «Sin tiempo», la pausa, «quitar animaciones» y la pantalla final. La revelación se detiene a mitad de cada paso con <c>_editorRevealT</c>. Necesita <see cref="EditorShotMode"/>.
        /// </summary>
        public IEnumerator EditorShotScript(Action<string> shot, Func<IEnumerator> pauseShot)
        {
            EditorShotFinished = false;
            _editorMirror = 1;                                                                    // el primer módulo es un espejo girado 135°: se ve el giro y la vuelta
            _editorAngle = 135;
            // --- módulo 1: llegada, respuesta, y la revelación de un espejo bien visto (giro, vuelta, acople, vuelo)
            yield return UntilPhase(Phase.Arrive);
            yield return WaitSeconds(0.16f);
            shot("llegada");
            yield return UntilPhase(Phase.Decide);
            yield return WaitSeconds(0.55f);
            shot("respuesta");
            PressButton(DockingContract.AnswerMirror);
            _editorRevealT = 0f;
            yield return UntilPhase(Phase.Reveal);
            float up = RevealUp, flip = RevealFlip, down = RevealDown, hold = RevealHold, fly = RevealFly;
            _editorRevealT = up + flip * 0.28f;                                                   // en plena vuelta (a mitad exacta el módulo está de canto y no se ve)
            yield return WaitSeconds(0.3f);
            shot("vuelta-del-espejo");
            _editorRevealT = up + flip + down + 0.12f;                                            // en el puerto: encaja (borde y chevrones lima, ✓ en el botón)
            yield return WaitSeconds(0.3f);
            shot("acople");
            _editorRevealT = up + flip + down + hold + fly * 0.5f;                                // volando a la estación
            yield return WaitSeconds(0.3f);
            shot("vuelo-a-la-estacion");
            _editorRevealT = -1f;
            _editorMirror = 0;
            _editorAngle = 45;
            // --- el anillo completo: la estación ya tiene 7 módulos; este es el 8.º
            yield return UntilPhase(Phase.Decide);
            AddFakeProgress(6, false);
            yield return WaitSeconds(0.3f);
            PressButton(DockingContract.AnswerFits);
            _editorRevealT = 0f;
            yield return UntilPhase(Phase.Reveal);
            _editorRevealT = down + up + hold + fly + 0.05f;                                      // el módulo cae en su casillero: se completa el anillo
            yield return WaitSeconds(0.5f);
            shot("anillo-completo");
            _editorRevealT = -1f;
            // --- un error: dijo «Espejo» y sí encajaba
            _editorMirror = 0;
            _editorAngle = 90;
            yield return UntilPhase(Phase.Decide);
            yield return WaitSeconds(0.3f);
            PressButton(DockingContract.AnswerMirror);
            _editorRevealT = 0f;
            yield return UntilPhase(Phase.Reveal);
            _editorRevealT = up + down + 0.32f;                                                   // el módulo se aleja sin castigo; el botón tocado lleva ✗
            yield return WaitSeconds(0.3f);
            shot("error");
            _editorRevealT = -1f;
            // --- sin tiempo
            _editorMirror = 1;
            _editorAngle = 45;
            _editorDeadline = 0.9f;
            yield return UntilPhase(Phase.Decide);
            _editorRevealT = 0f;                                                                  // la revelación nace detenida (así no se escapa mientras se espera)
            yield return UntilPhase(Phase.Reveal);                                                // nadie contesta: se acaba el tiempo y se muestra la verdad
            _editorRevealT = up + flip + down + 0.3f;
            yield return WaitSeconds(0.3f);
            shot("sin-tiempo");
            _editorRevealT = -1f;
            _editorDeadline = 0f;
            _editorMirror = -1;
            _editorAngle = NoAngle;
            // --- la pausa y «quitar animaciones» (con un módulo esperando)
            yield return UntilPhase(Phase.Decide);
            yield return WaitSeconds(0.4f);
            yield return pauseShot();
            GameFeel.ReduceMotion = true;
            yield return WaitSeconds(0.9f);
            shot("sin-animaciones");
            GameFeel.ReduceMotion = false;
            // --- la partida avanza de golpe: el último módulo se juega bien y llega el final con la curva y el giro mental
            AddFakeProgress(14, true);
            _trialsTotal = _trials + 1;
            _retoSeconds = 0f;
            _endsAt = 0f;
            yield return UntilPhase(Phase.Decide);
            yield return WaitSeconds(0.3f);
            PressButton(_trial.Mirrored ? DockingContract.AnswerMirror : DockingContract.AnswerFits);
            yield return UntilPhase(Phase.Done);
            // al terminar, el juego tapa todo con la cortina «¡Listo!» y, fuera del teléfono, la quita y deja ver el resultado con su botón «Continuar» (ver ExitButton): se espera a eso
            yield return WaitSeconds(7.5f);
            shot("final");
            EditorShotFinished = true;
        }

        /// <summary>
        /// SOLO EN EL EDITOR. El guion corto de las capturas en 16:9 (1080 × 1920): un módulo esperando y su acople. Necesita <see cref="EditorShotMode"/>.
        /// </summary>
        public IEnumerator EditorShortShotScript(Action<string> shot)
        {
            EditorShotFinished = false;
            _editorMirror = 1;
            _editorAngle = 90;
            yield return UntilPhase(Phase.Decide);
            yield return WaitSeconds(0.55f);
            shot("respuesta-16x9");
            PressButton(DockingContract.AnswerMirror);
            _editorRevealT = 0f;
            yield return UntilPhase(Phase.Reveal);
            _editorRevealT = RevealUp + RevealFlip + RevealDown + 0.12f;
            yield return WaitSeconds(0.3f);
            shot("acople-16x9");
            _editorRevealT = RevealUp + RevealFlip + RevealDown + RevealHold + RevealFly * 0.5f;
            yield return WaitSeconds(0.3f);
            shot("vuelo-16x9");
            _editorRevealT = -1f;
            _editorMirror = -1;
            _editorAngle = NoAngle;
            // --- la pantalla final en 16:9 (la más apretada): el último módulo se juega bien y llega el final
            AddFakeProgress(14, true);
            _trialsTotal = _trials + 1;
            _retoSeconds = 0f;
            _endsAt = 0f;
            yield return UntilPhase(Phase.Decide);
            yield return WaitSeconds(0.3f);
            PressButton(_trial.Mirrored ? DockingContract.AnswerMirror : DockingContract.AnswerFits);
            yield return UntilPhase(Phase.Done);
            yield return WaitSeconds(7.5f);
            shot("final-16x9");
            EditorShotFinished = true;
        }

        /// <summary>Suma aciertos de golpe (la estación, el contador y las medidas del final): en una partida real cada acierto se acopla una sola vez, así que todo sube junto.</summary>
        private void AddFakeProgress(int hits, bool curve)
        {
            int[] disparities = { 0, 45, 90, 135, 180 };
            for (int i = 0; i < hits; i++)
            {
                int d = disparities[i % disparities.Length];
                _disparities.Add(d);
                _rts.Add(650f + 5.5f * d + (i % 3) * 40f);
                _corrects.Add(true);
                _trials++;
                _correct++;
                _docked++;
                _stationShown++;
                _streak++;
                _bestStreak = Mathf.Max(_bestStreak, _streak);
            }
            _rings = _docked / DockingContract.Slots;
            RefreshStation();
            UpdateHud();
        }

        private IEnumerator UntilPhase(Phase phase)
        {
            float guard = 0f;
            while (_phase != phase && guard < 90f) { guard += Time.unscaledDeltaTime; yield return null; }
        }

        private static IEnumerator WaitSeconds(float seconds)
        {
            // tiempo REAL de cuadros (la pausa del juego no debe frenar el guion)
            float t = 0f;
            while (t < seconds) { t += Time.unscaledDeltaTime; yield return null; }
        }
    }
}
#endif
