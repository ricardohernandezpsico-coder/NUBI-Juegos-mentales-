#if UNITY_EDITOR
using System;
using System.Collections;
using System.Linq;
using UnityEngine;
using NeuroVida.Games.Shared;

namespace NeuroVida.Games.Correo
{
    public sealed partial class MailGameController
    {
        /// <summary>true cuando el guion de capturas llegó al final.</summary>
        public bool EditorShotFinished { get; private set; }

        /// <summary>
        /// SOLO EN EL EDITOR. El guion de las capturas de pantalla reales de «La estación de correo» (<c>verificar-todo.sh --capturas Correo</c>, docs/previews/correo-estacion.png): lleva la partida por los momentos que se fotografían usando
        /// el flujo REAL del juego (las tarjetas «NUEVO», la hoja, la cinta, el faro con la nave, el resumen del día, la pantalla final y la pausa) y llama a <paramref name="shot"/>(nombre) cuando cada uno está listo (<paramref name="pauseShot"/> abre y saca la pausa). Arranca en la etapa 5
        /// (la del lazo: trae hora, lo de todos los días y lazo; sin radio) y necesita <see cref="EditorShotMode"/> encendido. Para que las cartas de la cinta sean las que se quieren ver (una con sello dorado y otra con lazo), se arma la fila a mano.
        /// </summary>
        public IEnumerator EditorShotScript(Action<string> shot, Func<IEnumerator> pauseShot)
        {
            EditorShotFinished = false;
            // --- las tarjetas «NUEVO»: la primera (la estación) y la del lazo; las demás se pasan
            bool lazoShot = false, firstShot = false;
            while (_phase != Phase.Brief)
            {
                if (_phase == Phase.Intro && !_tapped)
                {
                    yield return WaitSeconds(0.7f);
                    if (_phase == Phase.Intro && !firstShot) { shot("nuevo-estacion"); firstShot = true; }
                    else if (_phase == Phase.Intro && _introKind == MailIntro.Lazo && !lazoShot) { shot("nuevo-lazo"); lazoShot = true; }
                    _tapped = true;
                    yield return WaitSeconds(0.2f);
                }
                else yield return null;
            }
            // --- la hoja de encargos
            yield return WaitSeconds(0.8f);
            shot("hoja");
            _tapped = true;
            while (_phase != Phase.Play) yield return null;
            // --- la cinta: una carta con sello dorado delante y otra con lazo detrás; sin cartas nuevas ni sacos mientras se mira
            _day.RushPlan.Clear();
            _day.NextIndex = _day.Plan.Count;
            _day.Belt.Clear();
            ClearBelt();
            _day.Belt.Add(new MailLetter { Planet = 1, Cue = MailCue.Gold });
            _day.Belt.Add(new MailLetter { Planet = 2, Cue = MailCue.Lazo });
            _day.Belt.Add(new MailLetter { Planet = 0 });
            _day.Belt.Add(new MailLetter { Planet = 3 });
            _day.Combo = 12;
            yield return WaitSeconds(1.3f);
            shot("cinta-dorado");
            // --- la misma cinta con «quitar animaciones»: el sello dorado se nota por su forma y su tamaño, sin brillo que late
            GameFeel.ReduceMotion = true;
            yield return WaitSeconds(1.2f);
            shot("cinta-sin-animaciones");
            GameFeel.ReduceMotion = false;
            yield return WaitSeconds(0.6f);
            // --- la pausa (con la cinta de fondo)
            yield return pauseShot();                       // el corredor abre la pausa (GameEntryPoint vive en Bootstrap, que este ensamblado no ve), saca la foto «pausa» y la cierra
            // --- el lazo al frente
            _day.Belt.RemoveAt(0);
            yield return WaitSeconds(1.2f);
            shot("cinta-lazo");
            // --- el faro con la nave a mitad del espectáculo
            var hour = _day.Todos.First(t => t.IsTime && !t.Cancelled);
            _day.JumpTo((hour.W0 + hour.W1) / 2f);
            DoBeacon();
            yield return WaitSeconds(1.5f);
            shot("faro");
            // --- el resumen del día 1: casi todo cumplido (se ve un ✓ y una raya)
            MarkDayDone(missOneLazo: true);
            yield return WaitSeconds(2.2f);
            _day.JumpTo(1f);
            while (_phase != Phase.Recap) yield return null;
            yield return WaitSeconds(0.9f);
            shot("resumen");
            // --- el día 2 (etapa 6: trae la radio que cancela): «¡Llega un saco!» y la radio cancelando un encargo
            _run.Level = 6;
            _tapped = true;
            while (_phase != Phase.Brief) yield return null;
            yield return WaitSeconds(0.4f);
            _tapped = true;
            while (_phase != Phase.Play) yield return null;
            _day.Belt.Clear();
            ClearBelt();
            _day.NextIndex = _day.Plan.Count;
            _day.RushPlan.Clear();
            _day.RushPlan.Add(new MailRush { At = _day.Fraction });
            yield return WaitSeconds(1.0f);                   // el cartel (entero, antes de que se desvanezca) y las primeras cartas del saco
            shot("saco");
            while (_day.Rush != null) yield return null;      // el saco termina de caer
            _day.Belt.Clear();
            ClearBelt();
            if (_day.CancelTodo != null)
            {
                _day.JumpTo(_day.CancelAt + 0.01f);
                yield return WaitSeconds(1.2f);               // el aviso de la radio
                shot("radio");
            }
            // --- la pantalla final: se salta al último día para verla sin jugar los cuatro
            MarkDayDone(missOneLazo: false);
            yield return WaitSeconds(0.5f);
            _day.JumpTo(1f);
            while (_phase != Phase.Recap) yield return null;
            yield return WaitSeconds(0.9f);
            _run.DayNo = MailContract.Days - 1;
            _tapped = true;
            while (_phase != Phase.Brief) yield return null;
            yield return WaitSeconds(0.4f);
            _tapped = true;
            while (_phase != Phase.Play) yield return null;
            MarkDayDone(missOneLazo: false);
            yield return WaitSeconds(0.5f);
            _day.JumpTo(1f);
            while (_phase != Phase.Recap) yield return null;
            yield return WaitSeconds(0.9f);
            _tapped = true;
            while (_phase != Phase.Done) yield return null;
            // al terminar, el juego tapa todo con la cortina «¡Listo!» y, fuera del teléfono, la quita y deja ver el resultado con su botón «Continuar» (ver ExitButton): se espera a eso
            yield return WaitSeconds(7.5f);
            shot("final");
            EditorShotFinished = true;
        }

        private static IEnumerator WaitSeconds(float seconds)
        {
            // tiempo REAL de cuadros (la pausa del juego no debe frenar el guion)
            float t = 0f;
            while (t < seconds) { t += Time.unscaledDeltaTime; yield return null; }
        }

        /// <summary>Marca el día como bien jugado para que el resumen y la pantalla final tengan algo que mostrar: los encargos por hora y por evento cumplidos (con un lazo escapado si se pide), las cartas, la racha y las miradas al reloj.</summary>
        private void MarkDayDone(bool missOneLazo)
        {
            bool missed = false;
            foreach (var t in _day.Todos)
            {
                if (t.IsTime) { t.State = MailState.Hit; continue; }
                t.Hits = 2; t.Misses = 0;
                if (missOneLazo && !missed && t.Cue == MailCue.Lazo) { t.Hits = 1; t.Misses = 1; missed = true; }
            }
            _day.Right = 13; _day.Sorted = 14; _day.BestCombo = 9; _day.Late = 0;
            _day.Peeks.Clear();
            var hour = _day.Todos.FirstOrDefault(t => t.IsTime);
            _day.Peeks.Add(0.25f);
            if (hour != null) _day.Peeks.Add(hour.W0 - 0.1f);
        }
    }
}
#endif
