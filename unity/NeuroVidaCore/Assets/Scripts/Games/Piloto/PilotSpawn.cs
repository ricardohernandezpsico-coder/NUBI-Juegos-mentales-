using System;
using System.Collections.Generic;

namespace NeuroVida.Games.Piloto
{
    /// <summary>Una caja en dp (x a la derecha, y hacia abajo): un aviso a la vista, una zona protegida.</summary>
    public readonly struct Box
    {
        public readonly float X0, Y0, X1, Y1;

        public Box(float x0, float y0, float x1, float y1)
        {
            X0 = Math.Min(x0, x1);
            Y0 = Math.Min(y0, y1);
            X1 = Math.Max(x0, x1);
            Y1 = Math.Max(y0, y1);
        }

        public static Box Around(float cx, float cy, float half) => new Box(cx - half, cy - half, cx + half, cy + half);

        /// <summary>¿Se pisan las dos cajas (con <paramref name="pad"/> dp de aire)?</summary>
        public bool Intersects(Box o, float pad = 0f) => X0 < o.X1 + pad && X1 > o.X0 - pad && Y0 < o.Y1 + pad && Y1 > o.Y0 - pad;
    }

    /// <summary>
    /// Dónde nace una señal (lógica pura, pedido explícito de Ricardo del 9-oct: ningún aviso, globo o cartel puede tapar una señal visible ni la zona donde nacen). Una señal nace en el cielo de adelante, lejos de la nave, desde el nivel 5 también en los bordes; NUNCA dentro del
    /// rectángulo de un aviso a la vista (<c>forbidden</c>), ni encima de otra señal viva, ni sobre la línea de balizas de la ruta (donde se confundiría con ellas).
    /// </summary>
    public static class PilotSpawn
    {
        public const float SideMargin = 34f, InnerMargin = 70f, EdgeBand = 30f, RouteClear = 28f, SignalClear = 66f, NoticePad = 6f;

        /// <summary>
        /// Elige un punto (dp) o devuelve false si en 40 intentos no hubo uno válido (la señal se salta y se prueba de nuevo enseguida). <paramref name="routeAt"/>: centro y medio ancho de la ruta a la altura y (para no caer sobre sus balizas);
        /// <paramref name="forbidden"/>: los rectángulos de los avisos a la vista; <paramref name="live"/>: las señales vivas.
        /// </summary>
        public static bool TryPlace(Random rng, bool peripheral, float skyTop, float skyBottom, IReadOnlyList<Box> forbidden, IReadOnlyList<(float x, float y)> live,
                                    Func<float, (float center, float half)> routeAt, out float x, out float y)
        {
            float w = PilotContract.FieldWidth;
            for (int tries = 0; tries < 40; tries++)
            {
                x = peripheral
                    ? (rng.NextDouble() < 0.5 ? SideMargin + (float)rng.NextDouble() * EdgeBand : w - SideMargin - (float)rng.NextDouble() * EdgeBand)
                    : InnerMargin + (float)rng.NextDouble() * (w - 2f * InnerMargin);
                y = skyTop + (float)rng.NextDouble() * (skyBottom - skyTop);
                if (Valid(x, y, forbidden, live, routeAt)) return true;
            }
            x = y = 0f;
            return false;
        }

        /// <summary>¿Puede nacer una señal en (x, y)? El anillo de la señal (54 dp) no pisa ningún aviso, ninguna otra señal ni la línea de balizas.</summary>
        public static bool Valid(float x, float y, IReadOnlyList<Box> forbidden, IReadOnlyList<(float x, float y)> live, Func<float, (float center, float half)> routeAt)
        {
            var box = Box.Around(x, y, PilotContract.SignalRingSize * 0.5f);
            if (forbidden != null)
                for (int i = 0; i < forbidden.Count; i++)
                    if (box.Intersects(forbidden[i], NoticePad)) return false;
            if (live != null)
                for (int i = 0; i < live.Count; i++)
                {
                    float dx = live[i].x - x, dy = live[i].y - y;
                    if (dx * dx + dy * dy < SignalClear * SignalClear) return false;
                }
            if (routeAt != null)
            {
                var r = routeAt(y);
                if (Math.Abs(Math.Abs(x - r.center) - r.half) <= RouteClear) return false;
            }
            return true;
        }
    }
}
