#if UNITY_EDITOR
using System;
using System.Collections;
using UnityEngine;
using NeuroVida.Games.Shared;

namespace NeuroVida.Games.Aterrizaje
{
    public sealed partial class LandingGameController
    {
        /// <summary>true cuando el guion de capturas llegó al final.</summary>
        public bool EditorShotFinished { get; private set; }

        /// <summary>El próximo aterrizaje (la regla y el blanco) que fuerzan las capturas; null = el que toca.</summary>
        private LandingTrial _editorTrial;

        /// <summary>-1 = el resultado corre normal; ≥ 0 = se sostiene en ese segundo del resultado mientras el guion saca las fotos.</summary>
        private float _editorResultT = -1f;

        private static LandingTrial ShotTrial(int level, float target, float min = 0f, float max = 100f, bool mid = true, string label = null, bool unit = true) => new LandingTrial
        {
            Level = level, Min = min, Max = max, Target = target, Label = label ?? Mathf.RoundToInt(target).ToString(), MinLabel = Mathf.RoundToInt(min).ToString(), MaxLabel = Mathf.RoundToInt(max).ToString(), MidTick = mid, Unit = unit,
        };

        /// <summary>
        /// SOLO EN EL EDITOR. El guion de las capturas de pantalla reales de Aterrizaje (<c>verificar-todo.sh --capturas Aterrizaje</c>, docs/previews/capturas/aterrizaje.png): lleva la partida por los momentos que se fotografían usando el flujo REAL del juego: la bajada con guía y la instrucción, un acierto con su bandera y ✓, un aterrizaje
        /// lejos con su aspa, una diana, una suma (la bandera lleva el RESULTADO), la cúpula nueva, la pausa, «quitar animaciones» (sin cordilleras) y la pantalla final. El resultado se detiene en un instante con <c>_editorResultT</c>. Necesita <see cref="EditorShotMode"/>.
        /// </summary>
        public IEnumerator EditorShotScript(Action<string> shot, Func<IEnumerator> pauseShot)
        {
            EditorShotFinished = false;
            // --- 1) la bajada con su guía y la instrucción (el primer aterrizaje)
            _editorTrial = ShotTrial(3, 37f);
            yield return UntilPhase(Phase.Fall);
            _tx = LandingPlan.FracX(0.36f);
            yield return WaitSeconds(2.4f);
            shot("bajada");
            // --- 2) un acierto (el MISMO aterrizaje): se lleva a 3 % del blanco y se suelta → bandera, tramo lima con ✓ y «¡Justo!»
            yield return LandAt(0.37f + 0.03f);
            yield return Freeze(1.0f);
            shot("acierto");
            _editorResultT = -1f;
            // --- 3) lejos: tramo coral con aspa y «Cerca: a N del blanco»
            _editorTrial = ShotTrial(4, 71f, 0f, 100f, false);
            yield return LandAt(0.35f);
            yield return Freeze(1.0f);
            shot("lejos");
            _editorResultT = -1f;
            // --- 4) una diana
            _editorTrial = ShotTrial(5, 640f, 0f, 1000f, true);
            yield return LandAt(0.64f);
            yield return Freeze(0.95f);
            shot("diana");
            _editorResultT = -1f;
            // --- 5) una suma (nivel 10): la bandera dice el RESULTADO
            _editorTrial = SumTrial();
            yield return LandAt(_editorTrial.TargetFraction + 0.02f);
            yield return Freeze(1.0f);
            shot("suma");
            _editorResultT = -1f;
            // --- 6) la cúpula nueva: el quinto aterrizaje justo
            _hits = 4;
            _shownHits = 4;
            _editorTrial = ShotTrial(3, 62f);
            yield return LandAt(0.62f + 0.025f);
            yield return Freeze(1.9f);
            shot("cupula");
            _editorResultT = -1f;
            // --- 7) la pausa y «quitar animaciones» (con la nave bajando)
            _editorTrial = ShotTrial(3, 25f);
            yield return UntilPhase(Phase.Fall);
            _tx = LandingPlan.FracX(0.3f);
            yield return WaitSeconds(1.6f);
            yield return pauseShot();
            GameFeel.ReduceMotion = true;
            yield return WaitSeconds(0.9f);
            shot("sin-animaciones");
            GameFeel.ReduceMotion = false;
            // --- 8) la partida avanza de golpe: el último aterrizaje (el que está bajando) se juega bien y llega el final (con los aciertos, las dianas y las cúpulas)
            AddFakeProgress();
            _trialsTotal = _trials + 1;
            yield return LandAt(0.25f + 0.02f);
            yield return UntilPhase(Phase.Done);
            // al terminar, el juego tapa todo con la cortina «¡Listo!» y, fuera del teléfono, la quita y deja ver el resultado con su botón «Continuar» (ver ExitButton): se espera a eso
            yield return WaitSeconds(7.5f);
            shot("final");
            EditorShotFinished = true;
        }

        /// <summary>SOLO EN EL EDITOR. El guion corto de las capturas en 16:9 (1080 × 1920): la bajada, un acierto y el final.</summary>
        public IEnumerator EditorShortShotScript(Action<string> shot)
        {
            EditorShotFinished = false;
            _editorTrial = ShotTrial(3, 37f);
            yield return UntilPhase(Phase.Fall);
            _tx = LandingPlan.FracX(0.36f);
            yield return WaitSeconds(2.0f);
            shot("bajada-16x9");
            yield return LandAt(0.37f + 0.03f);
            yield return Freeze(1.0f);
            shot("acierto-16x9");
            _editorResultT = -1f;
            _hits = 4;
            _shownHits = 4;
            _editorTrial = ShotTrial(3, 62f);
            yield return LandAt(0.62f + 0.025f);
            yield return Freeze(1.9f);
            shot("cupula-16x9");
            _editorResultT = -1f;
            AddFakeProgress();
            _trialsTotal = _trials + 1;
            _editorTrial = ShotTrial(4, 48f, 0f, 100f, false);
            yield return LandAt(0.48f + 0.02f);
            yield return UntilPhase(Phase.Done);
            yield return WaitSeconds(7.5f);
            shot("final-16x9");
            EditorShotFinished = true;
        }

        private static LandingTrial SumTrial()
        {
            var t = LandingContract.NextTrial(10, new System.Random(3));
            return t;
        }

        /// <summary>Suma aterrizajes de golpe (la partida, el marcador y las medidas del final): 10 aterrizajes con 8 justos, 2 dianas, racha de 4, una cúpula y un error medio de unos 9 de cada 100.</summary>
        private void AddFakeProgress()
        {
            float[] errors = { 0.009f, 0.031f, 0.14f, 0.022f, 0.008f, 0.041f, 0.012f, 0.19f, 0.027f, 0.045f };
            foreach (float e in errors)
            {
                _errors.Add(e);
                _trueFractions.Add(0.5f);
                _givenFractions.Add(0.5f + e);
                _trials++;
                bool hit = e <= LandingContract.HitError;
                if (hit) _hits++;
                if (e <= LandingContract.BullseyeError) _bulls++;
                _streak = hit ? _streak + 1 : 0;
                _bestStreak = Mathf.Max(_bestStreak, _streak);
            }
            _domes = LandingContract.Domes(_hits);
            _shownHits = _hits;
            _shownDomes = _domes;
            RefreshHudExtras();
            UpdateHud();
        }

        /// <summary>La nave se lleva a la fracción <paramref name="frac"/> de la regla y se suelta; termina cuando toca la regla.</summary>
        private IEnumerator LandAt(float frac)
        {
            yield return UntilPhase(Phase.Fall);
            _tx = LandingPlan.FracX(Mathf.Clamp01(frac));
            yield return WaitSeconds(0.9f);                                                         // el suavizado de 14 por segundo la alcanza
            Release();
            yield return UntilPhase(Phase.Land);
        }

        /// <summary>Detiene el resultado en el instante <paramref name="t"/> (s) y espera un momento para que se dibuje.</summary>
        private IEnumerator Freeze(float t)
        {
            _editorResultT = t;
            yield return WaitSeconds(0.35f);
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
