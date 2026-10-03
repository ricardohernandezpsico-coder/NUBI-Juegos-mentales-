using System;
using System.Collections.Generic;

namespace NeuroVida.Games.Intrusa
{
    /// <summary>Rectángulo lógico (x, y desde arriba a la izquierda, en dp de una pantalla de 360 de ancho).</summary>
    public struct LRect
    {
        public float X, Y, W, H;
        public LRect(float x, float y, float w, float h) { X = x; Y = y; W = w; H = h; }
        public float CenterX => X + W * 0.5f;
        public float CenterY => Y + H * 0.5f;
        public bool Contains(float px, float py) => px >= X && px <= X + W && py >= Y && py <= Y + H;
    }

    /// <summary>
    /// Geometría pura de la ronda, en unidades lógicas (dp de una pantalla de 360 de ancho; el controlador escala al lienzo). Es la MISMA
    /// regla que usa tools/intrusa/figuras.py para validar cada figura: la caja de juego es (40, 170, 280, 260), la figura se refleja y se
    /// gira ±12° en cada ronda (para que no se memorice), las placas de las palabras miden «texto + 22 × 30» y van debajo de la estrella
    /// (arriba si la estrella está en el 35% de arriba de la caja). Al armar la ronda se BUSCA una combinación (reflejo, giro, hueco de la
    /// intrusa y reparto de las palabras en las anclas) sin placas encimadas y dentro de la pantalla; las pruebas de Python exigen que para
    /// cada grupo del banco existan al menos 6 de ellas.
    /// </summary>
    public static class IntrusaLayout
    {
        public const float ScreenW = 360f;
        public const float BoxX = 40f, BoxY = 170f, BoxW = 280f, BoxH = 260f;
        public const float PlaquePad = 22f, PlaqueH = 30f, PlaqueGap = 14f;
        public const float HudBottom = 84f, TextTop = 508f, SideMargin = 8f, OverlapMargin = 3f;
        public const float MaxRotationDeg = 12f;
        public const float AboveFraction = 0.35f;
        public const float StarHitRadius = 14f;
        /// <summary>Toque mínimo (dp), también en la placa: el área de cada palabra se agranda hasta este lado.</summary>
        public const float MinTouch = 64f;

        /// <summary>De un punto de la figura (0..1) a la pantalla lógica, con reflejo y giro alrededor del centro de la caja.</summary>
        public static Vec2 ToScreen(Vec2 p, bool mirror, float rotationDeg)
        {
            float x = mirror ? 1f - p.X : p.X;
            float px = (x - 0.5f) * BoxW, py = (p.Y - 0.5f) * BoxH;
            double r = rotationDeg * Math.PI / 180.0;
            float c = (float)Math.Cos(r), s = (float)Math.Sin(r);
            return new Vec2(BoxX + BoxW * 0.5f + px * c - py * s, BoxY + BoxH * 0.5f + px * s + py * c);
        }

        public static bool PlaqueAbove(Vec2 screenPoint) => (screenPoint.Y - BoxY) / BoxH < AboveFraction;

        public static LRect Plaque(Vec2 star, float textWidth)
        {
            float w = textWidth + PlaquePad;
            bool above = PlaqueAbove(star);
            float py = above ? star.Y - PlaqueGap - PlaqueH : star.Y + PlaqueGap;
            float px = Math.Max(SideMargin, Math.Min(ScreenW - SideMargin - w, star.X - w * 0.5f));
            return new LRect(px, py, w, PlaqueH);
        }

        /// <summary>Cuánto «falla» una combinación: 0 = todo bien; si no, la suma de lo que se encima o se sale (en dp). Las placas deben
        /// quedar entre el HUD y el texto de abajo, dentro de la pantalla y sin tocarse (margen de 3 dp).</summary>
        public static float Violation(IReadOnlyList<LRect> plaques)
        {
            float v = 0f;
            foreach (var r in plaques)
            {
                if (r.Y < HudBottom) v += HudBottom - r.Y;
                if (r.Y + r.H > TextTop) v += r.Y + r.H - TextTop;
                if (r.X < SideMargin - 0.01f) v += SideMargin - r.X;
                if (r.X + r.W > ScreenW - SideMargin + 0.01f) v += r.X + r.W - (ScreenW - SideMargin);
            }
            for (int i = 0; i < plaques.Count; i++)
                for (int j = i + 1; j < plaques.Count; j++)
                {
                    var a = plaques[i]; var b = plaques[j];
                    float ox = Math.Min(a.X + a.W, b.X + b.W) - Math.Max(a.X, b.X) + OverlapMargin;
                    float oy = Math.Min(a.Y + a.H, b.Y + b.H) - Math.Max(a.Y, b.Y) + OverlapMargin;
                    if (ox > 0f && oy > 0f) v += Math.Min(ox, oy);
                }
            return v;
        }

        /// <summary>Las 5 placas (4 anclas + el hueco de la intrusa) de una combinación. [words] = las 4 palabras en el orden de las anclas.</summary>
        public static List<LRect> Plaques(FigureShape f, string[] words, string intruder, bool mirror, float rot, int hole, Func<string, float> textWidth)
        {
            var list = new List<LRect>(5);
            for (int i = 0; i < 4; i++) list.Add(Plaque(ToScreen(f.Point(f.anclas[i]), mirror, rot), textWidth(words[i])));
            list.Add(Plaque(ToScreen(f.Hole(hole), mirror, rot), textWidth(intruder)));
            return list;
        }

        /// <summary>
        /// Arma la ronda: primero al azar (reflejo, giro continuo en ±12°, hueco, reparto) hasta [randomTries] veces; si ninguna cabe, recorre
        /// todas las combinaciones (reflejo, giro -12/0/12, hueco, 24 repartos) desde un punto al azar; si tampoco, la de menor «falla».
        /// </summary>
        public static IntrusaSpec Arrange(IntrusaGroup g, FigureShape f, Random rng, Func<string, float> textWidth, bool review = false, int randomTries = 80)
        {
            var best = new IntrusaSpec { G = g, Figure = f, Review = review };
            float bestV = float.MaxValue;
            for (int k = 0; k < randomTries; k++)
            {
                bool mirror = rng.Next(2) == 1;
                float rot = (float)(rng.NextDouble() * 2.0 - 1.0) * MaxRotationDeg;
                int hole = rng.Next(f.HoleCount);
                var words = (string[])g.p.Clone();
                Shuffle(words, rng);
                float v = Violation(Plaques(f, words, g.x, mirror, rot, hole, textWidth));
                if (v < bestV) { bestV = v; Fill(best, mirror, rot, hole, words); }
                if (v <= 0f) return best;
            }
            var perms = Permutations(4);
            int start = rng.Next(perms.Count);
            float[] rots = { -MaxRotationDeg, 0f, MaxRotationDeg };
            for (int mi = 0; mi < 2; mi++)
                for (int ri = 0; ri < rots.Length; ri++)
                    for (int h = 0; h < f.HoleCount; h++)
                        for (int pi = 0; pi < perms.Count; pi++)
                        {
                            var perm = perms[(start + pi) % perms.Count];
                            var words = new string[4];
                            for (int i = 0; i < 4; i++) words[i] = g.p[perm[i]];
                            float v = Violation(Plaques(f, words, g.x, mi == 1, rots[ri], h, textWidth));
                            if (v < bestV) { bestV = v; Fill(best, mi == 1, rots[ri], h, words); }
                            if (v <= 0f) return best;
                        }
            return best;
        }

        private static void Fill(IntrusaSpec s, bool mirror, float rot, int hole, string[] words)
        {
            s.Mirror = mirror; s.RotationDeg = rot; s.Hole = hole; s.AnchorWords = (string[])words.Clone();
        }

        private static void Shuffle(string[] a, Random rng)
        {
            for (int i = a.Length - 1; i > 0; i--)
            {
                int j = rng.Next(i + 1);
                var t = a[i]; a[i] = a[j]; a[j] = t;
            }
        }

        private static List<int[]> Permutations(int n)
        {
            var res = new List<int[]>();
            var cur = new int[n];
            var used = new bool[n];
            void Rec(int d)
            {
                if (d == n) { res.Add((int[])cur.Clone()); return; }
                for (int i = 0; i < n; i++)
                {
                    if (used[i]) continue;
                    used[i] = true; cur[d] = i;
                    Rec(d + 1);
                    used[i] = false;
                }
            }
            Rec(0);
            return res;
        }

        /// <summary>Área de toque de una palabra: la caja que une la estrella y su placa, agrandada hasta <see cref="MinTouch"/> de lado.</summary>
        public static LRect TouchArea(Vec2 star, LRect plaque)
        {
            float x0 = Math.Min(star.X - StarHitRadius, plaque.X), x1 = Math.Max(star.X + StarHitRadius, plaque.X + plaque.W);
            float y0 = Math.Min(star.Y - StarHitRadius, plaque.Y), y1 = Math.Max(star.Y + StarHitRadius, plaque.Y + plaque.H);
            float w = x1 - x0, h = y1 - y0;
            float cx = (x0 + x1) * 0.5f, cy = (y0 + y1) * 0.5f;
            w = Math.Max(w, MinTouch); h = Math.Max(h, MinTouch);
            return new LRect(cx - w * 0.5f, cy - h * 0.5f, w, h);
        }

        /// <summary>Qué palabra se tocó: entre las áreas que contienen el punto, la de centro más cercano; -1 si ninguna.</summary>
        public static int Pick(float x, float y, IReadOnlyList<LRect> areas)
        {
            int best = -1;
            float bestD = float.MaxValue;
            for (int i = 0; i < areas.Count; i++)
            {
                if (!areas[i].Contains(x, y)) continue;
                float dx = x - areas[i].CenterX, dy = y - areas[i].CenterY;
                float d = dx * dx + dy * dy;
                if (d < bestD) { bestD = d; best = i; }
            }
            return best;
        }
    }

    /// <summary>Un tramo del recorrido de la chispa: de [From] a [To] (índices de punto de la figura), con su momento de salida y de llegada.</summary>
    public struct SparkSegment
    {
        public int From, To;
        public float Start, End;
        /// <summary>true si la chispa «salta»: reaparece en un nuevo origen porque el tramo anterior no termina donde empieza este.</summary>
        public bool Jump;
        public float Length;
    }

    /// <summary>
    /// El recorrido de la chispa por las aristas de la figura, en el orden del archivo (las aristas ya vienen en orden de trazado): sale
    /// del primer punto y avanza por cada arista; si la siguiente arista no toca donde quedó, reaparece en su origen (salto). La
    /// velocidad es ~430 px/s sobre una pantalla de 360 de ancho; el trazado completo se limita a 1,2-2,2 s (más corto con figuras chicas,
    /// más largo con figuras grandes, ajustando la velocidad).
    /// </summary>
    public static class IntrusaSpark
    {
        public const float BaseSpeed = 430f;
        public const float MinTotalSeconds = 1.2f;
        public const float MaxTotalSeconds = 2.2f;
        /// <summary>Pausa breve al saltar, para que se note que reaparece (s).</summary>
        public const float JumpPause = 0.05f;

        public static List<SparkSegment> Plan(FigureShape f, bool mirror, float rot, float speedFactor, out float totalSeconds)
        {
            var segs = new List<SparkSegment>();
            int n = f.EdgeCount;
            var pts = new Vec2[f.PointCount];
            for (int i = 0; i < pts.Length; i++) pts[i] = IntrusaLayout.ToScreen(f.Point(i), mirror, rot);

            int at = -1;
            float total = 0f;
            var order = new List<int[]>();
            for (int e = 0; e < n; e++)
            {
                int a = f.aristas[e * 2], b = f.aristas[e * 2 + 1];
                bool jump = false;
                if (at == a) { }
                else if (at == b) { int t = a; a = b; b = t; }
                else jump = e > 0;
                order.Add(new[] { a, b, jump ? 1 : 0 });
                at = b;
                total += Vec2.Distance(pts[a], pts[b]);
            }
            // velocidad: la base, pero con el trazado completo entre 1,2 y 2,2 s
            float seconds = total / BaseSpeed;
            seconds = Math.Max(MinTotalSeconds, Math.Min(MaxTotalSeconds, seconds));
            float speed = total / Math.Max(0.01f, seconds - JumpPause * CountJumps(order));
            speed = Math.Max(1f, speed) * Math.Max(0.1f, speedFactor);
            float t0 = 0f;
            foreach (var o in order)
            {
                float len = Vec2.Distance(pts[o[0]], pts[o[1]]);
                if (o[2] == 1) t0 += JumpPause / Math.Max(0.1f, speedFactor);
                float dur = len / speed;
                segs.Add(new SparkSegment { From = o[0], To = o[1], Start = t0, End = t0 + dur, Jump = o[2] == 1, Length = len });
                t0 += dur;
            }
            totalSeconds = t0;
            return segs;
        }

        private static int CountJumps(List<int[]> order)
        {
            int j = 0;
            foreach (var o in order) if (o[2] == 1) j++;
            return j;
        }
    }
}
