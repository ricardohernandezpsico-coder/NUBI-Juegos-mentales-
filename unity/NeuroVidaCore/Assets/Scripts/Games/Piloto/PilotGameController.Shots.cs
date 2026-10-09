#if UNITY_EDITOR
using System;
using System.Collections;
using UnityEngine;
using NeuroVida.Games.Shared;

namespace NeuroVida.Games.Piloto
{
    public sealed partial class PilotGameController
    {
        /// <summary>true cuando el guion de capturas llegó al final.</summary>
        public bool EditorShotFinished { get; private set; }

        private bool _shotOffRoute;

        /// <summary>
        /// SOLO EN EL EDITOR. El guion de las capturas de pantalla reales de «Piloto Estelar: la ruta de las balizas» (<c>verificar-todo.sh --capturas Piloto</c>, docs/previews/capturas/piloto.png): lleva el vuelo por los momentos que se fotografían
        /// usando el flujo REAL del juego (inicio suave, ruta con las balizas encendidas, señal atrapada con su rayo, señal parecida tocada con su aspa, nave fuera de la ruta con las balizas en coral, arco de sector con «NUEVA MISIÓN», hiperimpulso, la pausa,
        /// «quitar animaciones» y la pantalla final) y llama a <paramref name="shot"/>(nombre) cuando cada uno está listo (<paramref name="pauseShot"/> abre y saca la pausa). Arranca en el nivel 6 y necesita <see cref="EditorShotMode"/>.
        /// </summary>
        public IEnumerator EditorShotScript(Action<string> shot, Func<IEnumerator> pauseShot)
        {
            EditorShotFinished = false;
            StartCoroutine(ShotPilot());
            // --- el inicio suave: ruta ancha, el aviso del sector 1 y el recordatorio de la franja
            yield return UntilPhase(Phase.Fly);
            yield return WaitSeconds(1.1f);
            shot("inicio-suave");
            // --- la ruta con las balizas encendidas
            yield return WaitSeconds(3.4f);
            shot("ruta-balizas");
            // --- una señal de la misión atrapada: el rayo de la nave
            yield return UntilAlive(true, 0.5f);
            var target = FindAliveOld(true);
            if (target != null) TapAt(target.X, target.Y);
            yield return WaitSeconds(0.1f);
            shot("senal-atrapada");
            // --- una parecida tocada: el aspa coral y «No era de tu misión»
            yield return WaitSeconds(1.0f);
            yield return UntilAlive(false, 0.5f, lookalike: true);
            var look = FindAliveOld(false, lookalike: true) ?? FindAliveOld(false);
            if (look != null) TapAt(look.X, look.Y);
            yield return WaitSeconds(0.15f);
            shot("parecida-tocada");
            // --- la nave fuera de la ruta: balizas en coral, la nave se ve rojiza
            yield return WaitSeconds(0.9f);
            _shotOffRoute = true;
            yield return WaitSeconds(2.1f);
            shot("nave-fuera");
            _shotOffRoute = false;
            yield return WaitSeconds(1.6f);
            // --- el arco de sector: la nave lo cruza y la tarjeta dice «NUEVA MISIÓN»
            _t = _sectorSeconds - 3.4f;
            float guard = 0f;
            bool arcShot = false;
            while (guard < 8f && _sector < 1)
            {
                // el arco dorado a la vista, a media altura, antes de cruzarlo
                if (!arcShot && _gates.Count > 0 && YOf(_gates[0].P) > _plan.ShipY - 260f) { arcShot = true; shot("arco-de-sector"); }
                guard += Time.unscaledDeltaTime;
                yield return null;
            }
            if (!arcShot) Debug.Log("[Capturas] Piloto: el arco de sector no se vio a media altura antes de cruzarlo");
            // el aviso de misión nueva a la vista y entero (entra con un fundido de 0,2 s; con pocos cuadros por segundo en las capturas, un tiempo fijo no basta): se espera a que se vea y se saca justo en el latido de la tarjeta
            float noticeGuard = 0f;
            while (noticeGuard < 4f && !(_notice.HasValue && _notice.Value.Tall && _bannerGroup.alpha > 0.95f)) { noticeGuard += Time.unscaledDeltaTime; yield return null; }
            if (noticeGuard >= 4f) Debug.Log("[Capturas] Piloto: el aviso de misión nueva no llegó a verse en 4 s (señales a la vista: " + _signals.Count + ", avisos en espera: " + _noticeQueue.Count + ")");
            yield return WaitSeconds(0.1f);
            shot("sector-nueva-mision");
            // --- el hiperimpulso: estrellas en líneas, la nave azul
            yield return WaitSeconds(2.0f);
            if (!Hyper) StartHyper();
            yield return WaitSeconds(0.9f);
            shot("hiperimpulso");
            // --- la pausa
            yield return pauseShot();
            // --- con «quitar animaciones»: sin temblor ni estrellas en línea, las balizas con brillo fijo
            GameFeel.ReduceMotion = true;
            _wobbleAt = Now;
            yield return WaitSeconds(0.5f);
            shot("sin-animaciones");
            GameFeel.ReduceMotion = false;
            // --- la partida avanza de golpe: aciertos y toques suficientes para la medida, y el vuelo llega al final
            for (int i = 0; i < 14; i++) _run.Catch(false);
            for (int i = 0; i < 3; i++) _run.Miss();
            for (int i = 0; i < 2; i++) _run.FalseAlarm();
            _t = _flightSeconds - 0.3f;
            yield return UntilPhase(Phase.Done);
            // al terminar, el juego tapa todo con la cortina «¡Listo!» y, fuera del teléfono, la quita y deja ver el resultado con su botón «Continuar» (ver ExitButton): se espera a eso
            yield return WaitSeconds(7.5f);
            shot("final");
            EditorShotFinished = true;
        }

        /// <summary>El «piloto» del guion de capturas: lleva la nave por el centro de la ruta (o por fuera, si el guion lo pide).</summary>
        private IEnumerator ShotPilot()
        {
            while (!EditorShotFinished)
            {
                if (_phase == Phase.Fly)
                {
                    _route.At(_dist, out float center, out float half);
                    _steerX = Mathf.Clamp(center + (_shotOffRoute ? (center < PilotPlan.Width * 0.5f ? 1f : -1f) * (half + 26f) : Mathf.Sin(Time.unscaledTime * 0.8f) * half * 0.2f), 20f, PilotPlan.Width - 20f);
                }
                yield return null;
            }
        }

        private Signal FindAliveOld(bool target, bool lookalike = false)
        {
            foreach (var s in _signals)
            {
                if (!s.Alive || s.View < 0 || Now - s.At < 0.45f || s.Kind.IsTarget != target) continue;
                if (lookalike && !(s.Kind.Shape == _mission.Shape && s.Kind.Detail != _mission.Detail)) continue;
                return s;
            }
            return null;
        }

        private IEnumerator UntilAlive(bool target, float minAge, bool lookalike = false)
        {
            float guard = 0f;
            while (guard < 25f && FindAliveOld(target, lookalike) == null) { guard += Time.unscaledDeltaTime; yield return null; }
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
