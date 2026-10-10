#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using NeuroVida.Games.Shared;

namespace NeuroVida.Games.Radar
{
    public sealed partial class RadarGameController
    {
        /// <summary>true cuando el guion de capturas llegó al final.</summary>
        public bool EditorShotFinished { get; private set; }

        /// <summary>-1 = el viaje a la estación corre normal; de 0 a 1 = se sostiene en ese punto del viaje (0,25 = a mitad de la salida, 0,8 = a mitad de la vuelta) mientras el guion saca las fotos.</summary>
        private float _editorTripK = -1f;

        /// <summary>
        /// SOLO EN EL EDITOR. El guion de las capturas de pantalla reales de «Rescate relámpago» (<c>verificar-todo.sh --capturas Radar</c>, docs/previews/capturas/radar.png): lleva la partida por los momentos que se fotografían usando el flujo REAL del juego (el haz que gira, el
        /// destello —que acá se sostiene 1,6 s para poder fotografiarlo—, la estática, el tablero con lo marcado, la revelación con el rayo tractor y las tres marcas, la lluvia, una ronda perfecta, la pausa, «quitar animaciones» y la pantalla final) y llama a
        /// <paramref name="shot"/>(nombre) cuando cada uno está listo (<paramref name="pauseShot"/> abre y saca la pausa). Arranca en el nivel 8 (cuatro cápsulas y una roca) y necesita <see cref="EditorShotMode"/>.
        /// </summary>
        public IEnumerator EditorShotScript(Action<string> shot, Func<IEnumerator> pauseShot)
        {
            EditorShotFinished = false;
            // --- ronda 1: el haz que gira, el destello (sostenido), la estática
            _editorFlashHold = 1.6f;
            yield return UntilPhase(Phase.Watch);
            yield return WaitSeconds(0.9f);
            shot("atento");
            yield return UntilPhase(Phase.Flash);
            yield return WaitSeconds(0.45f);
            shot("destello");
            _editorFlashHold = 0f;
            yield return UntilPhase(Phase.Mask);
            yield return WaitSeconds(0.1f);
            shot("estatica");
            // --- la respuesta: las que eran, menos una que se cambia por otra que no estaba (así la revelación muestra las tres marcas)
            yield return UntilPhase(Phase.Answer);
            yield return WaitSeconds(0.4f);
            var types = new List<CapsuleType>(_round.Types());
            var wrong = CapsuleType.Hexagon;
            foreach (var t in RadarContract.BoardOrder) if (!_round.Types().Contains(t)) { wrong = t; break; }
            for (int i = 0; i < types.Count - 1; i++) { ToggleType(types[i], Array.IndexOf(RadarContract.BoardOrder, types[i])); yield return WaitSeconds(0.15f); }
            yield return WaitSeconds(0.4f);
            shot("respuesta");
            ToggleType(wrong, Array.IndexOf(RadarContract.BoardOrder, wrong));
            yield return WaitSeconds(0.2f);
            PressGo();
            yield return UntilPhase(Phase.Reveal);
            yield return WaitSeconds(1.0f);
            shot("revelacion");
            // --- la lluvia: dos rondas ya jugadas (se suman mientras dura la revelación) para que la que sigue sea la 4.ª
            var rng = new System.Random(3);
            _run.Add(RadarContract.NextRound(5, rng), 3, 0, 350f);
            _run.Add(RadarContract.NextRound(6, rng), 3, 0, 280f);
            // la nave ya lleva 9 a bordo (las 3 de la primera ronda, que siguen subiendo, y las 6 de estas dos): con las 4 de la lluvia se llena y viaja a la estación. Se suman SOLO las 6 de golpe (las de la primera ronda se suman solas al aterrizar)
            for (int i = 0; i < 6; i++) _seats.Add((CapsuleType)(_seats.Count % RadarContract.TypeCount));
            _shownRescued += 6;
            yield return UntilPhase(Phase.Watch);
            yield return WaitSeconds(0.5f);
            shot("lluvia");
            // --- la lluvia se resuelve bien: ronda perfecta con racha
            yield return UntilPhase(Phase.Answer);
            yield return WaitSeconds(0.3f);
            foreach (var t in new List<CapsuleType>(_round.Types())) { ToggleType(t, Array.IndexOf(RadarContract.BoardOrder, t)); yield return WaitSeconds(0.12f); }
            PressGo();
            yield return UntilPhase(Phase.Reveal);
            yield return WaitSeconds(0.9f);
            shot("revelacion-perfecta");
            // --- el viaje a la estación: la nave llena sale por la derecha y vuelve vacía por la izquierda (se sostiene a mitad de la salida y a mitad de la vuelta)
            _editorTripK = 0.25f;
            yield return UntilPhase(Phase.Trip);
            yield return WaitSeconds(0.25f);
            shot("viaje-salida");
            _editorTripK = 0.8f;
            yield return WaitSeconds(0.25f);
            shot("viaje-vuelta");
            _editorTripK = -1f;
            // --- la pausa y «quitar animaciones» (con el haz girando)
            yield return UntilPhase(Phase.Watch);
            yield return WaitSeconds(0.4f);
            yield return pauseShot();
            GameFeel.ReduceMotion = true;
            yield return WaitSeconds(0.9f);
            shot("sin-animaciones");
            GameFeel.ReduceMotion = false;
            // --- la partida avanza de golpe: la última ronda se juega bien y llega el final con varias cápsulas a salvo y la captura medida
            for (int i = 0; i < 2; i++) _run.Add(RadarContract.RainRound(5, rng), 4, 0, 300f);
            for (int i = 0; i < 4; i++) _run.Add(RadarContract.NextRound(9, rng), 4, 0, 150f);
            // el contador de arriba y la nave tienen que decir lo mismo que el total de la partida (en una partida real coinciden solos: cada cápsula rescatada sube y suma una vez): se completan las que faltan por las rondas que se sumaron de golpe
            while (_seats.Count < _run.Rescued) _seats.Add((CapsuleType)(_seats.Count % RadarContract.TypeCount));
            _shownRescued = _seats.Count;
            _roundsTotal = _run.RoundsPlayed + 1;
            _retoSeconds = 0f;
            yield return UntilPhase(Phase.Answer);
            yield return WaitSeconds(0.3f);
            foreach (var t in new List<CapsuleType>(_round.Types())) { ToggleType(t, Array.IndexOf(RadarContract.BoardOrder, t)); yield return WaitSeconds(0.1f); }
            PressGo();
            yield return UntilPhase(Phase.Done);
            // al terminar, el juego tapa todo con la cortina «¡Listo!» y, fuera del teléfono, la quita y deja ver el resultado con su botón «Continuar» (ver ExitButton): se espera a eso
            yield return WaitSeconds(7.5f);
            shot("final");
            EditorShotFinished = true;
        }

        /// <summary>
        /// SOLO EN EL EDITOR. El guion corto de las capturas en 16:9 (1080 × 1920, el layout de la Tarea 62): el haz, el destello, la respuesta y la revelación con la ventana de la nave encendida. Necesita <see cref="EditorShotMode"/>.
        /// </summary>
        public IEnumerator EditorShortShotScript(Action<string> shot)
        {
            EditorShotFinished = false;
            _editorFlashHold = 1.6f;
            yield return UntilPhase(Phase.Watch);
            yield return WaitSeconds(0.9f);
            shot("atento-16x9");
            yield return UntilPhase(Phase.Flash);
            yield return WaitSeconds(0.45f);
            shot("destello-16x9");
            _editorFlashHold = 0f;
            yield return UntilPhase(Phase.Answer);
            yield return WaitSeconds(0.4f);
            var types = new List<CapsuleType>(_round.Types());
            for (int i = 0; i < types.Count; i++) { ToggleType(types[i], Array.IndexOf(RadarContract.BoardOrder, types[i])); yield return WaitSeconds(0.15f); }
            yield return WaitSeconds(0.4f);
            shot("respuesta-16x9");
            PressGo();
            yield return UntilPhase(Phase.Reveal);
            yield return WaitSeconds(1.0f);
            shot("revelacion-16x9");
            EditorShotFinished = true;
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
