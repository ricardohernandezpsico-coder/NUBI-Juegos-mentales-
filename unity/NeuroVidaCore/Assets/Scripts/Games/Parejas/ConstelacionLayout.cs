using System;
using System.Collections.Generic;

namespace NeuroVida.Games.Parejas
{
    public struct ConPt
    {
        public float X, Y;
        public ConPt(float x, float y) { X = x; Y = y; }
        public static float Dist(ConPt a, ConPt b) => (float)Math.Sqrt((a.X - b.X) * (a.X - b.X) + (a.Y - b.Y) * (a.Y - b.Y));
    }

    /// <summary>El reparto de las luces de un cielo: sus centros (dp, x hacia la derecha e y hacia abajo, con el origen en la esquina de arriba a la izquierda del cielo) y el radio de cada luz.</summary>
    public sealed class ConPlacement
    {
        public ConPt[] Points;
        public float R;
        public float Width, Height;
    }

    /// <summary>Una línea ya trazada entre dos luces: sus extremos y el punto de control de la curva (de segundo grado). Los tramos que quedan afuera de las luces (<see cref="T0"/> a <see cref="T1"/>) se calculan al dibujar.</summary>
    public sealed class ConLinkGeo
    {
        public ConPt A, B;
        public float Cx, Cy;
        public float T0, T1 = 1f;

        public ConPt At(float t)
        {
            float u = 1f - t;
            return new ConPt(u * u * A.X + 2f * u * t * Cx + t * t * B.X, u * u * A.Y + 2f * u * t * Cy + t * t * B.Y);
        }
    }

    /// <summary>
    /// Dónde va cada luz y cómo se curva cada línea (docs/diseno-constelaciones.md §5; es el <c>layout</c> y el <c>route</c> del boceto aprobado, copiados). Puro, con pruebas: 300 repartos por tamaño, de 6 a 24 luces,
    /// sin una sola luz encimada ni fuera del cielo. Las luces se reparten como estrellas, no en grilla: muestreo del mejor candidato (cada luz va lo más lejos posible de las demás) y después se empujan hasta
    /// respetar la distancia mínima (2R + 12, es decir, un toque de 56 dp o más con 12 de aire).
    /// </summary>
    public static class ConstelacionLayout
    {
        // el cielo, en dp: ancho fijo (x 14..346 en la pantalla de 360) y un alto que se acomoda al teléfono
        public const float SkyW = 332f, RefSkyH = 396f, MinScale = 0.88f, MaxSkyH = 560f;
        public const float MinR = 28f, MaxR = 38f, Air = 12f, TouchExtra = 10f;
        public const int Candidates = 40, PushIterations = 500;

        public static float RadiusFor(int n, float w, float h) => Math.Max(MinR, Math.Min(MaxR, (float)Math.Sqrt(w * h / Math.Max(1, n)) * 0.34f));

        public static float MinDistance(float r) => 2f * r + Air;

        public static ConPlacement Place(int n, float w, float h, Random rng)
        {
            float r = RadiusFor(n, w, h), dmin = MinDistance(r);
            float loX = r, loY = r, hiX = w - r, hiY = h - r;
            var p = new ConPt[n];
            // muestreo del mejor candidato: cada luz va lo más lejos posible de las demás
            for (int i = 0; i < n; i++)
            {
                ConPt best = default;
                float bd = -1f;
                for (int k = 0; k < Candidates; k++)
                {
                    var c = new ConPt(loX + (float)rng.NextDouble() * (hiX - loX), loY + (float)rng.NextDouble() * (hiY - loY));
                    float d = 1e9f;
                    for (int j = 0; j < i; j++) d = Math.Min(d, ConPt.Dist(c, p[j]));
                    if (d > bd) { bd = d; best = c; }
                }
                p[i] = best;
            }
            // y se empujan entre sí hasta respetar la distancia mínima
            for (int it = 0; it < PushIterations; it++)
            {
                bool moved = false;
                for (int i = 0; i < n; i++)
                    for (int j = i + 1; j < n; j++)
                    {
                        float dx = p[j].X - p[i].X, dy = p[j].Y - p[i].Y, d = (float)Math.Sqrt(dx * dx + dy * dy);
                        if (d >= dmin) continue;
                        if (d < 0.01f) { dx = (float)rng.NextDouble() - 0.5f; dy = (float)rng.NextDouble() - 0.5f; d = (float)Math.Sqrt(dx * dx + dy * dy); }
                        float m = (dmin - d) / 2f + 0.5f;
                        p[i].X -= dx / d * m; p[i].Y -= dy / d * m;
                        p[j].X += dx / d * m; p[j].Y += dy / d * m;
                        moved = true;
                    }
                for (int i = 0; i < n; i++)
                {
                    p[i].X = Math.Max(loX, Math.Min(hiX, p[i].X));
                    p[i].Y = Math.Max(loY, Math.Min(hiY, p[i].Y));
                }
                if (!moved) break;
            }
            return new ConPlacement { Points = p, R = r, Width = w, Height = h };
        }

        /// <summary>La distancia entre dos luces más cercana (para las pruebas).</summary>
        public static float MinSeparation(ConPt[] p)
        {
            float m = float.MaxValue;
            for (int i = 0; i < p.Length; i++)
                for (int j = i + 1; j < p.Length; j++) m = Math.Min(m, ConPt.Dist(p[i], p[j]));
            return m;
        }

        /// <summary>La luz más cercana a un toque, si cae dentro de su radio de toque (R + 10); si no, -1.</summary>
        public static int Nearest(ConPt[] p, float r, float x, float y)
        {
            int best = -1;
            float bd = float.MaxValue;
            for (int i = 0; i < p.Length; i++)
            {
                float d = ConPt.Dist(new ConPt(x, y), p[i]);
                if (d < bd) { bd = d; best = i; }
            }
            return best >= 0 && bd <= r + TouchExtra ? best : -1;
        }

        // ------------------------------------------------------------------ las líneas

        public const float MaxDetour = 50f;

        /// <summary>
        /// La curva de la línea entre <paramref name="a"/> y <paramref name="b"/>: se curva un poco (desvío máximo de ±50 dp) para esquivar otras luces y otras líneas cuando puede; las parejas cercanas (distancia
        /// menor que 4R) van rectas. Si no puede esquivar, pasa por encima (las líneas se dibujan sobre todas las luces). <paramref name="lights"/> son todas las luces del cielo, <paramref name="links"/> las líneas ya trazadas.
        /// </summary>
        public static (float cx, float cy) Route(ConPt a, ConPt b, float r, float w, float h, IList<ConPt> lights, IList<ConLinkGeo> links)
        {
            float dx = b.X - a.X, dy = b.Y - a.Y, len = (float)Math.Sqrt(dx * dx + dy * dy);
            if (len < 0.0001f) len = 1f;
            float nx = -dy / len, ny = dx / len, mx = (a.X + b.X) / 2f, my = (a.Y + b.Y) / 2f;
            var others = new List<ConPt[]>();
            foreach (var l in links)
            {
                var pts = new ConPt[13];
                for (int i = 0; i < 13; i++) pts[i] = l.At(i / 12f);
                others.Add(pts);
            }
            float[] offsets = len < 4f * r ? new[] { 0f } : new[] { 0f, 25f, -25f, 50f, -50f };
            float bp = float.MaxValue, bcx = mx, bcy = my;
            foreach (float off in offsets)
            {
                float cx = mx + nx * off * 2f, cy = my + ny * off * 2f, pen = 0f;
                for (int i = 1; i < 24; i++)
                {
                    float t = i / 24f, u = 1f - t;
                    float x = u * u * a.X + 2f * u * t * cx + t * t * b.X, y = u * u * a.Y + 2f * u * t * cy + t * t * b.Y;
                    if (x < 0f || x > w || y < 0f || y > h) pen += 2f;
                    foreach (var o in lights)
                    {
                        if ((o.X == a.X && o.Y == a.Y) || (o.X == b.X && o.Y == b.Y)) continue;
                        float d = (float)Math.Sqrt((x - o.X) * (x - o.X) + (y - o.Y) * (y - o.Y));
                        if (d < r + 8f) pen += 1f + (r + 8f - d) / 8f;
                    }
                    // tampoco corre pegada a otra línea (se confundirían)
                    foreach (var pts in others)
                        foreach (var q in pts)
                            if (Math.Sqrt((x - q.X) * (x - q.X) + (y - q.Y) * (y - q.Y)) < 16.0) pen += 0.35f;
                }
                pen += Math.Abs(off) / 160f;
                if (pen < bp) { bp = pen; bcx = cx; bcy = cy; }
            }
            return (bcx, bcy);
        }

        /// <summary>Dónde empieza y dónde termina el tramo visible de la línea: nace en el borde de su primera luz y muere en el borde de la segunda (así, si cruza otra luz, se ve que pasa de largo).</summary>
        public static void Trim(ConLinkGeo l, float r)
        {
            l.T0 = 0f;
            l.T1 = 1f;
            for (int i = 0; i <= 100; i++)
                if (ConPt.Dist(l.At(i / 100f), l.A) > r + 5f) { l.T0 = i / 100f; break; }
            for (int i = 100; i >= 0; i--)
                if (ConPt.Dist(l.At(i / 100f), l.B) > r + 5f) { l.T1 = i / 100f; break; }
        }

        // ------------------------------------------------------------------ la disposición en la pantalla

        public const float HudDp = 66f, CardH = 60f, CardGap = 8f, RowH = 64f, RowGap = 8f, BottomMargin = 12f, W = 360f;

        public struct Metrics
        {
            /// <summary>Arriba de la tarjeta, de la fila «De memoria» y del cielo (dp desde arriba del área de juego); alto del cielo (en dp del cielo, antes de la escala) y escala con que se dibuja.</summary>
            public float CardTop, SkyTop, SkyH, Scale, RowTop, Bottom;
        }

        /// <summary>La disposición para un área de juego de <paramref name="height"/> dp (incluye el marcador): el cielo ocupa lo que queda entre la tarjeta y la fila de abajo, sin pasar de 560; si la
        /// pantalla es baja el cielo entero se achica pareja (hasta 0,88: una luz de 28 dp de radio sigue siendo de 49 dp de toque) y si sobra aire se reparte.</summary>
        public static Metrics Compute(float height)
        {
            var m = new Metrics();
            m.CardTop = HudDp + 2f;
            float skyTop = m.CardTop + CardH + CardGap;
            float fixedBelow = RowGap + RowH + BottomMargin;
            float avail = height - skyTop - fixedBelow;
            float scale = Math.Max(MinScale, Math.Min(1f, avail / RefSkyH));
            float skyH = scale < 1f || avail < RefSkyH ? RefSkyH : Math.Min(MaxSkyH, avail);
            float used = skyH * scale;
            float free = Math.Max(0f, avail - used);
            m.Scale = scale;
            m.SkyH = skyH;
            m.SkyTop = skyTop + free * 0.35f;
            m.RowTop = m.SkyTop + used + RowGap + free * 0.25f;
            m.Bottom = m.RowTop + RowH;
            return m;
        }
    }
}
