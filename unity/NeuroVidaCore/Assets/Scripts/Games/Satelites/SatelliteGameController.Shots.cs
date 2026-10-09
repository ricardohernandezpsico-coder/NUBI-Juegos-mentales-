#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using NeuroVida.Games.Shared;

namespace NeuroVida.Games.Satelites
{
    public sealed partial class SatelliteGameController
    {
        /// <summary>true cuando el guion de capturas llegó al final.</summary>
        public bool EditorShotFinished { get; private set; }

        /// <summary>
        /// SOLO EN EL EDITOR. El guion de las capturas de pantalla reales de «Satélites: enciende tu planeta» (<c>verificar-todo.sh --capturas Satelites</c>, docs/previews/capturas/satelites.png): lleva la partida por los momentos que se
        /// fotografían usando el flujo REAL del juego (presentación, seguimiento, respuesta con marcas, revelación con «¿Aquí se cruzaron?», los sobres volando, el aviso «NUEVO» de la nube, la nube a mitad del seguimiento, las luces
        /// con «quitar animaciones» y la pantalla final) y llama a <paramref name="shot"/>(nombre) cuando cada uno está listo (<paramref name="pauseShot"/> abre y saca la pausa). Arranca en el nivel 6 y necesita <see cref="EditorShotMode"/>.
        /// </summary>
        public IEnumerator EditorShotScript(Action<string> shot, Func<IEnumerator> pauseShot)
        {
            EditorShotFinished = false;
            // --- ronda 1 (sin sorpresa): presentación y seguimiento
            yield return UntilPhase(Phase.Cue);
            yield return WaitSeconds(1.0f);
            shot("presentacion");
            yield return UntilPhase(Phase.Track);
            yield return WaitSeconds(2.0f);
            shot("seguimiento");
            // --- la respuesta: K−1 bien marcados y, antes de completar, la toma; después el último, equivocado (el vecino del que falta)
            yield return UntilPhase(Phase.Answer);
            yield return WaitSeconds(0.4f);
            var targets = new List<int>();
            for (int i = 0; i < _swarm.Count; i++) if (_swarm.Target[i]) targets.Add(i);
            for (int j = 0; j < targets.Count - 1; j++) { ToggleMark(targets[j]); yield return WaitSeconds(0.15f); }
            yield return WaitSeconds(0.5f);
            shot("respuesta");
            int missed = targets[targets.Count - 1], wrong = -1;
            float best = float.MaxValue;
            for (int i = 0; i < _swarm.Count; i++)
            {
                if (_swarm.Target[i]) continue;
                float d = Mathf.Abs(_swarm.X[i] - _swarm.X[missed]) + Mathf.Abs(_swarm.Y[i] - _swarm.Y[missed]);
                if (d < best) { best = d; wrong = i; }
            }
            ToggleMark(wrong);
            // --- la revelación: los sobres vuelan al planeta y después se ven las marcas
            yield return UntilPhase(Phase.Reveal);
            yield return WaitSeconds(0.75f);
            shot("luces-volando");
            yield return WaitSeconds(0.9f);
            shot("revelacion");
            // --- la pausa (con el planeta ya con luces)
            yield return pauseShot();
            // --- ronda 2: la nube de polvo, con su aviso «NUEVO»
            _debugSurprise = (int)Surprise.Cloud;
            yield return UntilPhase(Phase.Surprise);
            yield return WaitSeconds(0.9f);
            shot("aviso-nube");
            yield return UntilPhase(Phase.Track);
            float guard = 0f;
            while (guard < 8f && !(_swarm != null && _swarm.Cloud != null && Mathf.Abs(_swarm.Cloud.X - ScreenPlan.Width * 0.5f) < 25f)) { guard += Time.unscaledDeltaTime; yield return null; }
            shot("nube");
            // --- la ronda 2 termina bien y, con «quitar animaciones», las luces quedan fijas
            yield return UntilPhase(Phase.Answer);
            targets.Clear();
            for (int i = 0; i < _swarm.Count; i++) if (_swarm.Target[i]) targets.Add(i);
            foreach (int t in targets) { ToggleMark(t); yield return WaitSeconds(0.1f); }
            _debugSurprise = 0;
            yield return UntilPhase(Phase.Reveal);
            GameFeel.ReduceMotion = true;
            yield return WaitSeconds(1.7f);                 // ya aterrizaron los sobres (con «quitar animaciones» no vuelan: las luces aparecen quietas) y la revelación sigue a la vista
            shot("sin-animaciones");
            GameFeel.ReduceMotion = false;
            // --- la partida avanza de golpe: seis rondas más ya jugadas y un planeta con muchas luces; la última se juega bien y llega el final
            while (_phase == Phase.Reveal) yield return null;
            for (int i = 0; _run.RoundsPlayed < SatelliteContract.PrecisionRounds - 1; i++) _run.Add(4, 4, 9, 6);
            AddShotLights(24);
            yield return UntilPhase(Phase.Answer);
            targets.Clear();
            for (int i = 0; i < _swarm.Count; i++) if (_swarm.Target[i]) targets.Add(i);
            foreach (int t in targets) { ToggleMark(t); yield return WaitSeconds(0.1f); }
            yield return UntilPhase(Phase.Done);
            // al terminar, el juego tapa todo con la cortina «¡Listo!» y, fuera del teléfono, la quita y deja ver el resultado con su botón «Continuar» (ver ExitButton): se espera a eso
            yield return WaitSeconds(7.5f);
            shot("final");
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

        /// <summary>Enciende de una vez <paramref name="count"/> luces más en el planeta (para que la pantalla final tenga algo que mostrar sin jugar toda la partida).</summary>
        private void AddShotLights(int count)
        {
            for (int i = 0; i < count && _lightSpots.Count < _lightDots.Length; i++)
            {
                var spot = SatelliteContract.NextLightSpot(_lightSpots, _rng);
                var d = _lightDots[_lightSpots.Count];
                d.Spot = new Vector2(spot.x, spot.y);
                SetChild(d.Root, d.Spot.x, d.Spot.y, 18f, 18f);
                d.On = true;
                d.At = Now - 1f;
                d.Root.gameObject.SetActive(true);
                _lightSpots.Add(spot);
                _lightsShown++;
            }
            UpdateFooter();
        }
    }
}
#endif
