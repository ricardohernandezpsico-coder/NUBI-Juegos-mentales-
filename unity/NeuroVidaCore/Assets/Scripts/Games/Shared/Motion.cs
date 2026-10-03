using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace NeuroVida.Games.Shared
{
    /// <summary>
    /// Punto único de "quitar animaciones" para los componentes comunes y los juegos. La fuente de verdad sigue siendo
    /// <see cref="GameFeel.ReduceMotion"/> (la app lo manda desde el ajuste del teléfono); acá solo hay atajos para no
    /// repetir la regla (docs/movimiento-reducido.md):
    /// se QUEDA el movimiento esencial (el que ES la tarea), se QUITA lo decorativo, y lo decorativo que comunica algo
    /// (acierto, error, nivel, final) se reemplaza por su estado final con un fundido corto o un cambio inmediato.
    /// </summary>
    public static class Motion
    {
        /// <summary>Duración del fundido que reemplaza a un movimiento decorativo (la regla pide 120-200 ms).</summary>
        public const float FadeSeconds = 0.16f;

        /// <summary>true = se pueden dibujar adornos que se mueven solos; false = "quitar animaciones" activo.</summary>
        public static bool Decorative => !GameFeel.ReduceMotion;

        /// <summary>Lleva el alfa de <paramref name="graphic"/> hasta <paramref name="toAlpha"/> (reloj de juego).</summary>
        public static IEnumerator Fade(Graphic graphic, float toAlpha, float seconds = FadeSeconds)
        {
            if (graphic == null) yield break;
            var c = graphic.color;
            yield return ColorTo(graphic, new Color(c.r, c.g, c.b, toAlpha), seconds);
        }

        /// <summary>Lleva el color de <paramref name="graphic"/> hasta <paramref name="to"/> con un fundido.</summary>
        public static IEnumerator ColorTo(Graphic graphic, Color to, float seconds = FadeSeconds)
        {
            if (graphic == null) yield break;
            var from = graphic.color;
            float t = 0f;
            while (t < seconds)
            {
                if (graphic == null) yield break;
                t += GameClock.DeltaTime;
                graphic.color = Color.Lerp(from, to, Mathf.Clamp01(t / seconds));
                yield return null;
            }
            if (graphic != null) graphic.color = to;
        }

        /// <summary>Lleva el alfa de un <see cref="CanvasGroup"/> de <paramref name="from"/> a <paramref name="to"/>.</summary>
        public static IEnumerator Fade(CanvasGroup group, float from, float to, float seconds = FadeSeconds)
        {
            if (group == null) yield break;
            group.alpha = from;
            float t = 0f;
            while (t < seconds)
            {
                if (group == null) yield break;
                t += GameClock.DeltaTime;
                group.alpha = Mathf.Lerp(from, to, Mathf.Clamp01(t / seconds));
                yield return null;
            }
            if (group != null) group.alpha = to;
        }

        /// <summary>Escala de <paramref name="from"/> a <paramref name="to"/> con la curva <paramref name="ease"/> (p. ej.
        /// <see cref="UiFx.EaseOutBack"/>). Con "quitar animaciones" deja la escala final de una vez, sin rebote.</summary>
        public static IEnumerator ScaleTo(RectTransform rect, float from, float to, float seconds, Func<float, float> ease = null)
        {
            if (rect == null) yield break;
            if (!Decorative)
            {
                rect.localScale = Vector3.one * to;
                yield break;
            }
            float t = 0f;
            while (t < seconds)
            {
                if (rect == null) yield break;
                t += GameClock.DeltaTime;
                float k = Mathf.Clamp01(t / seconds);
                rect.localScale = Vector3.one * Mathf.LerpUnclamped(from, to, ease != null ? ease(k) : k);
                yield return null;
            }
            if (rect != null) rect.localScale = Vector3.one * to;
        }
    }
}
