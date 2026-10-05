using System;
using System.Collections.Generic;

namespace NeuroVida.Games.Engranajes
{
    /// <summary>Las cuatro piezas del cohete que se mueven con engranajes (de arriba a abajo en la columna del cohete).</summary>
    public enum Station { Antena, Compuerta, Carga, Turbina }

    public enum Question { Dir, Rack, Speed, Jam, Build }

    /// <summary>Todas las respuestas posibles de un botón.</summary>
    public enum Answer { None, Cw, Ccw, Stuck, Up, Down, Open, Close, Fast, Slow, Gear, Crossed }

    public enum GearRole { Motor, Main, Branch, Station, Jam }

    public enum LinkType { Mesh, Straight, Crossed, Gap }

    /// <summary>Lo que se pone en el hueco de «Arma tú».</summary>
    public enum Piece { None, Gear, Crossed }

    public enum Intro { None, Ramas, Correas, Carga, Compuerta, Velocidad, Traba, Arma }

    public enum ButtonIcon { Cw, Ccw, X, Up, Down, Fast, Slow, Gear, Belt }

    public readonly struct ButtonDef
    {
        public readonly Answer Value;
        public readonly string Label;
        public readonly ButtonIcon Icon;
        public ButtonDef(Answer value, string label, ButtonIcon icon) { Value = value; Label = label; Icon = icon; }
    }

    public sealed class GearDef
    {
        public float X, Y;
        public int Col, Row;
        public float Tip, Pitch;
        public int N;
        public GearRole Role;
        public bool HasStation;
        public Station Station;
        public bool Removed;
        /// <summary>Ángulo (radianes) de un diente: las fases de los vecinos se calculan para que cada diente quede frente al hueco del otro.</summary>
        public float A;
        public bool PhaseSet;
    }

    public readonly struct Link
    {
        public readonly int A, B;
        public readonly LinkType Type;
        public Link(int a, int b, LinkType type) { A = a; B = b; Type = type; }
    }

    /// <summary>El hueco de «Arma tú»: el engranaje que va ahí si se elige «Engranaje» (entre <see cref="From"/> y <see cref="To"/>).</summary>
    public sealed class Slot
    {
        public GearDef Gear;
        public int From, To;
    }

    public sealed class StageDef
    {
        public Question Q;
        public Station[] Targets;
        public int Branches, Belts;
        public bool Far;
        public Intro Intro;
    }

    /// <summary>Una máquina de la partida: los engranajes (en una cuadrícula de 4 × 6 y la columna del cohete), cómo se unen, y la respuesta correcta.</summary>
    public sealed class Machine
    {
        public List<GearDef> Gears = new List<GearDef>();
        public List<Link> Links = new List<Link>();
        /// <summary>Índices del camino principal: el motor y cada engranaje hasta la pieza preguntada.</summary>
        public List<int> Main = new List<int>();
        public int Target;
        public Station TargetStation;
        public Slot Slot;
        public bool Jam;
        public int[] JamTri;
        public Question Q;
        public StageDef Stage;
        public int Level;
        /// <summary>1 = el motor gira como el reloj, -1 = al revés.</summary>
        public int MotorDir;
        public Dictionary<Station, int> Stations = new Dictionary<Station, int>();
        /// <summary>«Arma tú»: lo que hay que lograr (<see cref="Answer.Up"/> o <see cref="Answer.Open"/>).</summary>
        public Answer Goal;
        public Answer Truth;
        public int Count => Gears.Count;
    }

    /// <summary>El resultado de hacer arrancar la máquina: sentido, velocidad y profundidad (pasos desde el motor) de cada engranaje.</summary>
    public sealed class Solution
    {
        public int[] Dir;
        public float[] Speed;
        public int[] Depth;
    }

    /// <summary>
    /// Reglas puras de «Engranajes» (docs/diseno-engranajes.md; puerto fiel del boceto docs/previews/engranajes-boceto.html): la cuadrícula de la sala de máquinas,
    /// los caminos limpios del motor a cada pieza del cohete, las ramas (siempre terminan en otra pieza), las correas y el hueco, la trampa en triángulo, la física
    /// (sentido y velocidad de cada engranaje), el efecto en cada pieza y las 12 etapas.
    /// </summary>
    public static class EngranajesContract
    {
        public const string GameId = "engranajes";
        public const int MaxLevel = 12;
        public const int PrecisionMachines = 10;
        public const int RetoSeconds = 120;
        /// <summary>Máquinas «de ritmo completo» en 120 s (una cada ~13 s): con ellas el Reto vale 100.</summary>
        public const float RetoReferenceMachines = 9f;
        public const int RocketLights = 10;

        // ---- la cuadrícula (unidades del boceto; la pantalla las escala)
        public const float GridX0 = 40f, GridY0 = 150f, GridStep = 50f;
        public const int Cols = 4, Rows = 6;
        /// <summary>Casillas alternadas grandes (15 dientes) y chicas (10 dientes), como un tablero de ajedrez.</summary>
        public const float BigTip = 33f, BigPitch = 30f, SmallTip = 23f, SmallPitch = 20f;
        public const int BigTeeth = 15, SmallTeeth = 10;
        public const float ToothDepth = 6f;
        public const float JamPitch = 12f, JamTip = 15f;
        public const int JamTeeth = 6;

        public static readonly Station[] AllStations = { Station.Antena, Station.Compuerta, Station.Carga, Station.Turbina };

        /// <summary>Fila de cada pieza en la columna del cohete.</summary>
        public static int StationRow(Station s)
        {
            switch (s)
            {
                case Station.Antena: return 0;
                case Station.Compuerta: return 2;
                case Station.Carga: return 3;
                default: return 5;
            }
        }

        public static string StationLabel(Station s)
        {
            switch (s)
            {
                case Station.Antena: return "Antena";
                case Station.Compuerta: return "Compuerta";
                case Station.Carga: return "Carga";
                default: return "Turbina";
            }
        }

        public static string StationName(Station s)
        {
            switch (s)
            {
                case Station.Antena: return "la antena";
                case Station.Compuerta: return "la compuerta";
                case Station.Carga: return "la carga";
                default: return "la turbina";
            }
        }

        public static bool IsBig(int col, int row) => (col + row) % 2 == 0;

        /// <summary>Centro de una casilla (la columna 4 es la del cohete, donde van los engranajes de las piezas).</summary>
        public static void CellXY(int col, int row, out float x, out float y)
        {
            x = col < 4 ? GridX0 + col * GridStep : GridX0 + 4 * GridStep;
            y = GridY0 + row * GridStep;
        }

        // ------------------------------------------------------------------ las 12 etapas

        public static readonly StageDef[] Stages =
        {
            Stage(Question.Dir, new[] { Station.Antena, Station.Turbina }, 0, 0, false, Intro.None),
            Stage(Question.Dir, new[] { Station.Antena, Station.Turbina }, 0, 0, true, Intro.None),
            Stage(Question.Dir, new[] { Station.Antena, Station.Turbina }, 1, 0, false, Intro.Ramas),
            Stage(Question.Dir, new[] { Station.Antena, Station.Turbina }, 1, 1, false, Intro.Correas),
            Stage(Question.Rack, new[] { Station.Carga }, 0, 0, false, Intro.Carga),
            Stage(Question.Rack, new[] { Station.Compuerta }, 1, 0, false, Intro.Compuerta),
            Stage(Question.Speed, new[] { Station.Antena, Station.Turbina }, 0, 0, false, Intro.Velocidad),
            Stage(Question.Speed, new[] { Station.Antena, Station.Turbina }, 1, 1, false, Intro.None),
            Stage(Question.Jam, new[] { Station.Antena, Station.Turbina }, 1, 0, false, Intro.Traba),
            Stage(Question.Build, new[] { Station.Carga }, 0, 0, false, Intro.Arma),
            Stage(Question.Rack, new[] { Station.Carga, Station.Compuerta }, 2, 1, true, Intro.None),
            Stage(Question.Build, new[] { Station.Compuerta }, 1, 1, true, Intro.None),
        };

        private static StageDef Stage(Question q, Station[] targets, int branches, int belts, bool far, Intro intro) =>
            new StageDef { Q = q, Targets = targets, Branches = branches, Belts = belts, Far = far, Intro = intro };

        private static readonly int[] EtapaOf = { 1, 1, 2, 2, 3, 3, 4, 4, 5, 5, 5, 5 };

        public static StageDef StageFor(int level) => Stages[Math.Max(1, Math.Min(MaxLevel, level)) - 1];

        /// <summary>Las 5 etapas de la medida final: giro, ramas y correas, movimiento (carga y compuerta), velocidad, trampas y armar.</summary>
        public static int Etapa(int level) => EtapaOf[Math.Max(1, Math.Min(MaxLevel, level)) - 1];

        public static string[] IntroText(Intro intro)
        {
            switch (intro)
            {
                case Intro.Ramas: return new[] { "Ahora la fuerza se reparte en ramas.", "Cada rama mueve otra pieza del cohete." };
                case Intro.Correas: return new[] { "Correa recta: mismo sentido.", "Correa cruzada: sentido contrario." };
                case Intro.Carga: return new[] { "El elevador sube o baja la carga", "según hacia dónde gire su engranaje." };
                case Intro.Compuerta: return new[] { "La compuerta se abre si sube", "y se cierra si baja." };
                case Intro.Velocidad: return new[] { "Un engranaje chico gira más rápido", "que uno grande." };
                case Intro.Traba: return new[] { "Ojo: tres engranajes que se tocan", "en triángulo se traban." };
                case Intro.Arma: return new[] { "¡Ahora armas tú! Falta una pieza:", "elige la que logra lo que pide Nubi." };
                default: return new string[0];
            }
        }

        // ------------------------------------------------------------------ generación

        private static readonly int[][] N4 = { new[] { 1, 0 }, new[] { 0, 1 }, new[] { 0, -1 }, new[] { -1, 0 } };

        private static int Key(int c, int r) => (c + 8) * 100 + (r + 8);

        /// <summary>Una máquina del nivel <paramref name="level"/> (1..12); siempre devuelve una (reintenta hasta lograrla).</summary>
        public static Machine Generate(int level, Random rng)
        {
            var st = StageFor(level);
            for (int k = 0; k < 24; k++)
            {
                var m = TryGenerate(st, rng);
                if (m != null) { m.Level = level; return m; }
            }
            throw new InvalidOperationException("No se pudo armar una máquina del nivel " + level);
        }

        private static void Shuffle<T>(IList<T> list, Random rng)
        {
            for (int i = list.Count - 1; i > 0; i--)
            {
                int j = rng.Next(i + 1);
                T t = list[i]; list[i] = list[j]; list[j] = t;
            }
        }

        private struct Move { public int A, B, D; public bool Same; public double Rnd; }

        /// <summary>
        /// Camino ordenado de <paramref name="from"/> a <paramref name="to"/>: prefiere seguir derecho mientras se acerca y dobla hacia el destino. Cada casilla nueva solo
        /// puede tocar a la anterior (nunca a otra ya ocupada ni a otra del mismo camino), y a la columna del cohete se entra solo desde la izquierda. Devuelve las casillas sin la
        /// de salida, o null.
        /// </summary>
        public static List<int[]> Route(Dictionary<int, int> occ, int fromC, int fromR, int toC, int toR, int maxLen, Random rng)
        {
            int tk = Key(toC, toR);
            var path = new HashSet<int> { Key(fromC, fromR) };
            var list = new List<int[]>();
            List<int[]> best = null;

            bool Ok(int c, int r, int prevKey)
            {
                int k = Key(c, r);
                if (k != tk && (c < 0 || c > 3 || r < 0 || r >= Rows)) return false;
                if (k == tk && c != 4) return false;
                if (occ.ContainsKey(k) || path.Contains(k)) return false;
                foreach (var d in N4)
                {
                    int n = Key(c + d[0], r + d[1]);
                    if (!(n == prevKey || (!occ.ContainsKey(n) && !path.Contains(n)))) return false;
                }
                return true;
            }

            void Dfs(int c, int r, int[] dir)
            {
                if (best != null) return;
                if (c == toC && r == toR) { best = new List<int[]>(list); return; }
                if (list.Count > maxLen) return;
                var moves = new List<Move>(4);
                foreach (var d in N4)
                    moves.Add(new Move
                    {
                        A = d[0], B = d[1],
                        D = Math.Abs(toC - (c + d[0])) + Math.Abs(toR - (r + d[1])),
                        Same = dir != null && d[0] == dir[0] && d[1] == dir[1],
                        Rnd = rng.NextDouble()
                    });
                moves.Sort((m1, m2) =>
                {
                    int c1 = m1.D.CompareTo(m2.D);
                    if (c1 != 0) return c1;
                    c1 = (m2.Same ? 1 : 0).CompareTo(m1.Same ? 1 : 0);
                    return c1 != 0 ? c1 : m1.Rnd.CompareTo(m2.Rnd);
                });
                foreach (var m in moves)
                {
                    int nc = c + m.A, nr = r + m.B;
                    if (!Ok(nc, nr, Key(c, r))) continue;
                    if (nc == 4 && m.A != 1) continue;           // al destino (la columna del cohete) solo se entra desde la izquierda
                    path.Add(Key(nc, nr));
                    list.Add(new[] { nc, nr });
                    Dfs(nc, nr, new[] { m.A, m.B });
                    if (best != null) return;
                    list.RemoveAt(list.Count - 1);
                    path.Remove(Key(nc, nr));
                }
            }

            Dfs(fromC, fromR, null);
            return best;
        }

        public static int Turns(int fromC, int fromR, List<int[]> p)
        {
            int t = 0, cc = fromC, cr = fromR;
            int[] prev = null;
            foreach (var c in p)
            {
                var d = new[] { c[0] - cc, c[1] - cr };
                if (prev != null && (d[0] != prev[0] || d[1] != prev[1])) t++;
                prev = d;
                cc = c[0];
                cr = c[1];
            }
            return t;
        }

        /// <summary>El mejor de 8 caminos: el de menos dobleces (puntaje: dobleces × 3 + largo).</summary>
        public static List<int[]> BestRoute(Dictionary<int, int> occ, int fromC, int fromR, int toC, int toR, int maxLen, Random rng)
        {
            List<int[]> best = null;
            int score = int.MaxValue;
            for (int k = 0; k < 8; k++)
            {
                var p = Route(occ, fromC, fromR, toC, toR, maxLen, rng);
                if (p == null) continue;
                int sc = Turns(fromC, fromR, p) * 3 + p.Count;
                if (sc < score) { score = sc; best = p; }
            }
            return best;
        }

        private static Machine TryGenerate(StageDef st, Random rng)
        {
            for (int tries = 0; tries < 400; tries++)
            {
                var occ = new Dictionary<int, int>();
                var m = new Machine { Stage = st, Q = st.Q };

                int Put(int c, int r, GearRole role)
                {
                    CellXY(c, r, out float x, out float y);
                    bool big = IsBig(c, r);
                    m.Gears.Add(new GearDef
                    {
                        X = x, Y = y, Col = c, Row = r, Role = role,
                        Tip = big ? BigTip : SmallTip, Pitch = big ? BigPitch : SmallPitch, N = big ? BigTeeth : SmallTeeth
                    });
                    occ[Key(c, r)] = m.Gears.Count - 1;
                    return m.Gears.Count - 1;
                }

                var tName = st.Targets[rng.Next(st.Targets.Length)];
                int tRow = StationRow(tName);
                int rm = rng.Next(Rows);
                if (!st.Far && Math.Abs(rm - tRow) > 2) continue;
                if (st.Far && Math.Abs(rm - tRow) < 2) continue;
                int motor = Put(0, rm, GearRole.Motor);
                var p = BestRoute(occ, 0, rm, 4, tRow, 14, rng);
                if (p == null) continue;
                m.Main.Add(motor);
                foreach (var cell in p)
                {
                    int gi = Put(cell[0], cell[1], cell[0] == 4 ? GearRole.Station : GearRole.Main);
                    m.Links.Add(new Link(m.Main[m.Main.Count - 1], gi, LinkType.Mesh));
                    m.Main.Add(gi);
                }
                m.Target = m.Main[m.Main.Count - 1];
                m.TargetStation = tName;
                m.Gears[m.Target].HasStation = true;
                m.Gears[m.Target].Station = tName;

                // ramas: desde un engranaje del camino hacia OTRA pieza del cohete (compuerta y carga son vecinas: nunca van activas a la vez)
                var others = new List<Station>();
                foreach (var s in AllStations)
                    if (s != tName && !((s == Station.Compuerta || s == Station.Carga) && (tName == Station.Compuerta || tName == Station.Carga))) others.Add(s);
                bool bOk = true;
                var branchEnds = new List<Station>();
                for (int b = 0; b < st.Branches && bOk; b++)
                {
                    var pool = new List<Station>();
                    foreach (var s in others)
                    {
                        if (branchEnds.Contains(s) || occ.ContainsKey(Key(4, StationRow(s)))) continue;
                        bool free = true;
                        foreach (var d in N4)
                            if (d[0] != -1 && occ.ContainsKey(Key(4 + d[0], StationRow(s) + d[1]))) { free = false; break; }
                        if (free) pool.Add(s);
                    }
                    if (pool.Count == 0) { bOk = false; break; }
                    var sName = pool[rng.Next(pool.Count)];
                    int sRow = StationRow(sName);
                    bool done = false;
                    var juncs = m.Main.GetRange(1, Math.Max(0, m.Main.Count - 2));
                    Shuffle(juncs, rng);
                    foreach (int j in juncs)
                    {
                        var g = m.Gears[j];
                        var dirs = new List<int[]>(N4);
                        Shuffle(dirs, rng);
                        foreach (var d in dirs)
                        {
                            int sc = g.Col + d[0], sr = g.Row + d[1];
                            if (sc < 0 || sc > 3 || sr < 0 || sr >= Rows) continue;
                            if (occ.ContainsKey(Key(sc, sr))) continue;
                            bool clear = true;
                            foreach (var x in N4)
                            {
                                int n = Key(sc + x[0], sr + x[1]);
                                if (n != Key(g.Col, g.Row) && occ.ContainsKey(n)) { clear = false; break; }
                            }
                            if (!clear) continue;
                            occ[Key(sc, sr)] = -1;
                            var q = BestRoute(occ, sc, sr, 4, sRow, 9, rng);
                            occ.Remove(Key(sc, sr));
                            if (q == null) continue;
                            int prev = j;
                            int first = Put(sc, sr, GearRole.Branch);
                            m.Links.Add(new Link(prev, first, LinkType.Mesh));
                            prev = first;
                            foreach (var cell in q)
                            {
                                int gi = Put(cell[0], cell[1], cell[0] == 4 ? GearRole.Station : GearRole.Branch);
                                m.Links.Add(new Link(prev, gi, LinkType.Mesh));
                                prev = gi;
                            }
                            m.Gears[prev].HasStation = true;
                            m.Gears[prev].Station = sName;
                            branchEnds.Add(sName);
                            done = true;
                            break;
                        }
                        if (done) break;
                    }
                    if (!done) bOk = false;
                }
                if (!bOk) continue;

                // correas y hueco: en un tramo recto de tres del camino principal se quita el del medio
                var triples = new List<int>();
                for (int k = 1; k < m.Main.Count - 2; k++)
                {
                    var A = m.Gears[m.Main[k - 1]];
                    var B = m.Gears[m.Main[k]];
                    var C = m.Gears[m.Main[k + 1]];
                    if ((A.Col == B.Col && B.Col == C.Col) || (A.Row == B.Row && B.Row == C.Row))
                    {
                        int deg = 0;
                        foreach (var l in m.Links) if (l.A == m.Main[k] || l.B == m.Main[k]) deg++;
                        if (deg == 2 && B.Role == GearRole.Main) triples.Add(k);
                    }
                }
                int wantBelt = st.Belts + (st.Q == Question.Build ? 1 : 0);
                if (triples.Count < wantBelt) continue;
                Shuffle(triples, rng);
                // los tramos elegidos no pueden ser vecinos (se perdería el engranaje que une dos correas)
                var chosen = new List<int>();
                foreach (int k0 in triples)
                {
                    bool near = false;
                    foreach (int c0 in chosen) if (Math.Abs(k0 - c0) < 2) { near = true; break; }
                    if (near) continue;
                    chosen.Add(k0);
                    if (chosen.Count == wantBelt) break;
                }
                if (chosen.Count < wantBelt) continue;
                for (int t = 0; t < wantBelt; t++)
                {
                    int k = chosen[t];
                    int mid = m.Main[k], a = m.Main[k - 1], c = m.Main[k + 1];
                    var g = m.Gears[mid];
                    g.Removed = true;
                    occ.Remove(Key(g.Col, g.Row));
                    for (int i = m.Links.Count - 1; i >= 0; i--) if (m.Links[i].A == mid || m.Links[i].B == mid) m.Links.RemoveAt(i);
                    bool isGap = st.Q == Question.Build && t == 0;
                    m.Links.Add(new Link(a, c, isGap ? LinkType.Gap : (rng.NextDouble() < 0.5 ? LinkType.Straight : LinkType.Crossed)));
                    if (isGap)
                        m.Slot = new Slot
                        {
                            Gear = new GearDef { X = g.X, Y = g.Y, Col = g.Col, Row = g.Row, Tip = g.Tip, Pitch = g.Pitch, N = g.N, Role = GearRole.Main },
                            From = a, To = c
                        };
                }

                // trampa: un engranaje chico fuera de la cuadrícula que toca a dos vecinos del camino, formando un triángulo
                if (st.Q == Question.Jam && rng.NextDouble() < 0.45)
                {
                    var cand = new List<Link>();
                    // el triángulo no incluye a la pieza preguntada: ella sigue siendo el final del camino
                    foreach (var l in m.Links)
                        if (l.Type == LinkType.Mesh && m.Main.Contains(l.A) && m.Main.Contains(l.B) && l.B != m.Target && !m.Gears[l.A].Removed && !m.Gears[l.B].Removed) cand.Add(l);
                    Shuffle(cand, rng);
                    bool placed = false;
                    foreach (var l in cand)
                    {
                        var A = m.Gears[l.A];
                        var B = m.Gears[l.B];
                        float pr = JamPitch, dA = A.Pitch + pr, dB = B.Pitch + pr;
                        float dAB = (float)Math.Sqrt((B.X - A.X) * (B.X - A.X) + (B.Y - A.Y) * (B.Y - A.Y));
                        float a = (dA * dA - dB * dB + dAB * dAB) / (2f * dAB), h2 = dA * dA - a * a;
                        if (h2 <= 0f) continue;
                        float h = (float)Math.Sqrt(h2), mx = A.X + a * (B.X - A.X) / dAB, my = A.Y + a * (B.Y - A.Y) / dAB;
                        foreach (int sg in new[] { 1, -1 })
                        {
                            float x = mx + sg * h * (B.Y - A.Y) / dAB, y = my - sg * h * (B.X - A.X) / dAB;
                            if (x < 16f || x > 240f || y < 120f || y > 440f) continue;
                            bool clear = true;
                            for (int i = 0; i < m.Gears.Count; i++)
                            {
                                var o = m.Gears[i];
                                if (o.Removed || i == l.A || i == l.B) continue;
                                if (Math.Sqrt((o.X - x) * (o.X - x) + (o.Y - y) * (o.Y - y)) <= o.Tip + pr + 3f + 4f) { clear = false; break; }
                            }
                            if (!clear) continue;
                            m.Gears.Add(new GearDef { X = x, Y = y, Tip = JamTip, Pitch = pr, N = JamTeeth, Role = GearRole.Jam, Col = -1, Row = -1 });
                            int ji = m.Gears.Count - 1;
                            m.Links.Add(new Link(l.A, ji, LinkType.Mesh));
                            m.Links.Add(new Link(l.B, ji, LinkType.Mesh));
                            m.Jam = true;
                            m.JamTri = new[] { l.A, l.B, ji };
                            placed = true;
                            break;
                        }
                        if (placed) break;
                    }
                    if (!placed) continue;
                }

                m.MotorDir = rng.NextDouble() < 0.5 ? 1 : -1;
                for (int i = 0; i < m.Gears.Count; i++) if (m.Gears[i].HasStation) m.Stations[m.Gears[i].Station] = i;
                if (st.Q == Question.Build)
                {
                    var want = tName == Station.Carga ? Answer.Up : Answer.Open;
                    var eG = Effect(m, Solve(m, Piece.Gear));
                    var eC = Effect(m, Solve(m, Piece.Crossed));
                    if (eG == eC) continue;
                    m.Goal = want;
                    m.Truth = eG == want ? Answer.Gear : Answer.Crossed;
                }
                else
                {
                    var s = Solve(m, Piece.None);
                    if (st.Q == Question.Speed)
                    {
                        float ratio = Math.Abs(s.Speed[m.Target]);
                        if (ratio > 0.8f && ratio < 1.25f) continue;
                        m.Truth = ratio > 1f ? Answer.Fast : Answer.Slow;
                    }
                    else if (st.Q == Question.Rack) m.Truth = Effect(m, s);
                    else m.Truth = m.Jam ? Answer.Stuck : (s.Dir[m.Target] > 0 ? Answer.Cw : Answer.Ccw);
                }
                Phase(m, Piece.None, rng);
                return m;
            }
            return null;
        }

        // ------------------------------------------------------------------ dientes que encajan

        /// <summary>El ángulo del diente de <paramref name="gj"/> para que, mirando a <paramref name="gi"/>, cada diente de uno quede frente al hueco del otro.</summary>
        public static float MeshPhase(GearDef gi, GearDef gj)
        {
            double th = Math.Atan2(gj.Y - gi.Y, gj.X - gi.X);
            double pi = 2.0 * Math.PI / gi.N, pj = 2.0 * Math.PI / gj.N;
            double fi = (((th - gi.A) / pi) % 1.0 + 1.0) % 1.0;
            double fj = ((0.5 - fi) % 1.0 + 1.0) % 1.0;
            return (float)(th + Math.PI - fj * pj);
        }

        /// <summary>Fases iniciales: desde el motor, cada engranaje unido por dientes queda encajado con el anterior. Con <paramref name="piece"/> también el hueco de «Arma tú».</summary>
        public static void Phase(Machine m, Piece piece, Random rng)
        {
            var g0 = m.Gears[0];
            if (!g0.PhaseSet) { g0.A = (float)(rng.NextDouble() * 6.28); g0.PhaseSet = true; }
            var seen = new HashSet<int> { 0 };
            var queue = new Queue<int>();
            queue.Enqueue(0);
            while (queue.Count > 0)
            {
                int i = queue.Dequeue();
                var gi = m.Gears[i];
                foreach (var l in m.Links)
                {
                    int j = l.A == i ? l.B : l.B == i ? l.A : -1;
                    if (j < 0 || seen.Contains(j)) continue;
                    if (l.Type == LinkType.Gap && piece == Piece.None) continue;
                    var gj = m.Gears[j];
                    seen.Add(j);
                    queue.Enqueue(j);
                    if (l.Type == LinkType.Mesh) { gj.A = MeshPhase(gi, gj); gj.PhaseSet = true; }
                    else if (l.Type == LinkType.Gap && piece == Piece.Gear)
                    {
                        var s = m.Slot.Gear;
                        s.A = MeshPhase(gi, s);
                        s.PhaseSet = true;
                        gj.A = MeshPhase(s, gj);
                        gj.PhaseSet = true;
                    }
                    else if (!gj.PhaseSet) { gj.A = (float)(rng.NextDouble() * 6.28); gj.PhaseSet = true; }
                }
            }
            foreach (var g in m.Gears) if (!g.PhaseSet) { g.A = (float)(rng.NextDouble() * 6.28); g.PhaseSet = true; }
        }

        // ------------------------------------------------------------------ física

        /// <summary>
        /// Sentido y velocidad de cada engranaje (el motor gira ±1): dos engranajes que se tocan giran en sentido contrario; una correa recta conserva el sentido y una
        /// cruzada lo invierte; un engranaje intermedio no cambia la razón entre el motor y la pieza (la velocidad va según el tamaño: chico = más rápido).
        /// </summary>
        public static Solution Solve(Machine m, Piece piece)
        {
            int n = m.Gears.Count;
            var sol = new Solution { Dir = new int[n], Speed = new float[n], Depth = new int[n] };
            for (int i = 0; i < n; i++) sol.Depth[i] = -1;
            sol.Dir[0] = m.MotorDir;
            sol.Speed[0] = m.MotorDir;
            sol.Depth[0] = 0;
            var queue = new Queue<int>();
            queue.Enqueue(0);
            while (queue.Count > 0)
            {
                int i = queue.Dequeue();
                foreach (var l in m.Links)
                {
                    int j = l.A == i ? l.B : l.B == i ? l.A : -1;
                    if (j < 0 || sol.Depth[j] >= 0) continue;
                    var t = l.Type;
                    bool two = false, flip;
                    if (t == LinkType.Gap)
                    {
                        if (piece == Piece.None) continue;
                        if (piece == Piece.Gear) { flip = false; two = true; }
                        else flip = true;
                    }
                    else flip = t == LinkType.Mesh || t == LinkType.Crossed;
                    float pi = m.Gears[i].Pitch, pj = m.Gears[j].Pitch;
                    sol.Dir[j] = flip ? -sol.Dir[i] : sol.Dir[i];
                    sol.Speed[j] = sol.Speed[i] * pi / pj * (flip ? -1f : 1f);
                    sol.Depth[j] = sol.Depth[i] + (two ? 2 : 1);
                    queue.Enqueue(j);
                }
            }
            return sol;
        }

        /// <summary>Lo que hace la pieza preguntada. La barra dentada está a la derecha del engranaje: si este gira como el reloj, la barra baja (carga baja, compuerta se cierra).</summary>
        public static Answer Effect(Machine m, Solution s)
        {
            int d = m.Jam ? 0 : s.Dir[m.Target];
            if (m.TargetStation == Station.Carga) return d == 0 ? Answer.Stuck : (d > 0 ? Answer.Down : Answer.Up);
            if (m.TargetStation == Station.Compuerta) return d == 0 ? Answer.Stuck : (d > 0 ? Answer.Close : Answer.Open);
            return d == 0 ? Answer.Stuck : (d > 0 ? Answer.Cw : Answer.Ccw);
        }

        /// <summary>El camino de engranajes del motor a <paramref name="target"/> (para el truco de Nubi). Con <paramref name="placed"/> también pasa por el hueco.</summary>
        public static List<int> PathTo(Machine m, int target, bool placed)
        {
            var par = new int[m.Gears.Count];
            for (int i = 0; i < par.Length; i++) par[i] = -2;
            par[0] = -1;
            var queue = new Queue<int>();
            queue.Enqueue(0);
            while (queue.Count > 0)
            {
                int i = queue.Dequeue();
                foreach (var l in m.Links)
                {
                    int j = l.A == i ? l.B : l.B == i ? l.A : -1;
                    if (j < 0 || par[j] != -2) continue;
                    if (l.Type == LinkType.Gap && !placed) continue;
                    par[j] = i;
                    queue.Enqueue(j);
                }
            }
            var path = new List<int>();
            for (int i = target; i >= 0; i = par[i]) path.Insert(0, i);
            return path;
        }

        // ------------------------------------------------------------------ pregunta, botones y explicación

        public static string QuestionText(Machine m)
        {
            switch (m.Q)
            {
                case Question.Dir:
                case Question.Jam: return "¿Cómo girará " + StationName(m.TargetStation) + "?";
                case Question.Rack: return m.TargetStation == Station.Carga ? "¿La carga sube o baja?" : "¿La compuerta se abre o se cierra?";
                case Question.Speed: return "¿" + (m.TargetStation == Station.Antena ? "La antena" : "La turbina") + " gira más rápido o más lento que el motor?";
                default: return m.Goal == Answer.Up ? "¿Qué pieza hace SUBIR la carga?" : "¿Qué pieza ABRE la compuerta?";
            }
        }

        public static ButtonDef[] Buttons(Machine m)
        {
            switch (m.Q)
            {
                case Question.Dir:
                    return new[] { new ButtonDef(Answer.Cw, "Como el reloj", ButtonIcon.Cw), new ButtonDef(Answer.Ccw, "Al revés", ButtonIcon.Ccw) };
                case Question.Jam:
                    return new[] { new ButtonDef(Answer.Cw, "Como el reloj", ButtonIcon.Cw), new ButtonDef(Answer.Ccw, "Al revés", ButtonIcon.Ccw), new ButtonDef(Answer.Stuck, "Se traba", ButtonIcon.X) };
                case Question.Rack:
                    return m.TargetStation == Station.Carga
                        ? new[] { new ButtonDef(Answer.Up, "Sube", ButtonIcon.Up), new ButtonDef(Answer.Down, "Baja", ButtonIcon.Down) }
                        : new[] { new ButtonDef(Answer.Open, "Se abre", ButtonIcon.Up), new ButtonDef(Answer.Close, "Se cierra", ButtonIcon.Down) };
                case Question.Speed:
                    return new[] { new ButtonDef(Answer.Fast, "Más rápido", ButtonIcon.Fast), new ButtonDef(Answer.Slow, "Más lento", ButtonIcon.Slow) };
                default:
                    return new[] { new ButtonDef(Answer.Gear, "Engranaje", ButtonIcon.Gear), new ButtonDef(Answer.Crossed, "Correa cruzada", ButtonIcon.Belt) };
            }
        }

        public static string Explain(Machine m)
        {
            if (m.Jam && m.Truth == Answer.Stuck) return "Se traba: tres engranajes en triángulo";
            switch (m.Q)
            {
                case Question.Speed:
                    return m.Truth == Answer.Fast ? "Más rápido: su engranaje es más chico que el del motor" : "Más lento: su engranaje es más grande que el del motor";
                case Question.Build:
                    return m.Truth == Answer.Gear ? "Con un engranaje en medio, gira igual que el de antes" : "La correa cruzada invierte el giro";
                case Question.Rack:
                    switch (m.Truth)
                    {
                        case Answer.Up: return "La carga sube";
                        case Answer.Down: return "La carga baja";
                        case Answer.Open: return "La compuerta se abre";
                        default: return "La compuerta se cierra";
                    }
                default:
                    return (m.Truth == Answer.Cw ? "Gira como el reloj: " : "Gira al revés del reloj: ") + StationName(m.TargetStation);
            }
        }

        /// <summary>El truco de Nubi al fallar (vacío si hay traba: ahí no hay giro que seguir).</summary>
        public static string Trick(Machine m)
        {
            if (m.Q == Question.Speed) return "Truco: compara el tamaño del motor con el de la pieza";
            if (m.Jam) return "";
            return "Truco: cada engranaje que toca gira al revés";
        }

        // ------------------------------------------------------------------ puntaje

        /// <summary>Puntaje 0-100: máquinas acertadas sobre las 10 de Precisión; en el Reto, sobre el ritmo de referencia.</summary>
        public static int Score(int correct, bool endless)
        {
            float denom = endless ? RetoReferenceMachines : PrecisionMachines;
            return Math.Max(0, Math.Min(100, (int)Math.Round(100f * correct / denom)));
        }

        /// <summary>Segundos de cada paso de la cascada (más lento para mayores: «más tiempo para mirar»).</summary>
        public static float StepSeconds(bool senior) => senior ? 0.21f : 0.15f;

        // ------------------------------------------------------------------ chequeos de coherencia (los usan las pruebas)

        /// <summary>Pares de engranajes que se tocan sin estar unidos: dos engranajes quedan «tocándose» si sus dientes se pisan (distancia menor que la suma de las puntas).
        /// Los unidos por dientes (mesh) y los de la trampa se esperan; todo otro par debe estar a más distancia que la suma de las puntas.</summary>
        public static List<string> UnwantedContacts(Machine m, Piece piece)
        {
            var bad = new List<string>();
            var all = new List<GearDef>();
            var index = new List<int>();
            for (int i = 0; i < m.Gears.Count; i++) if (!m.Gears[i].Removed) { all.Add(m.Gears[i]); index.Add(i); }
            bool slotGear = m.Slot != null && piece == Piece.Gear;
            if (slotGear) { all.Add(m.Slot.Gear); index.Add(-1); }
            for (int a = 0; a < all.Count; a++)
                for (int b = a + 1; b < all.Count; b++)
                {
                    double d = Math.Sqrt((all[a].X - all[b].X) * (all[a].X - all[b].X) + (all[a].Y - all[b].Y) * (all[a].Y - all[b].Y));
                    bool meshed = false;
                    int ia = index[a], ib = index[b];
                    foreach (var l in m.Links)
                        if (l.Type == LinkType.Mesh && ((l.A == ia && l.B == ib) || (l.A == ib && l.B == ia))) meshed = true;
                    if (slotGear)
                    {
                        if ((ia == -1 && (ib == m.Slot.From || ib == m.Slot.To)) || (ib == -1 && (ia == m.Slot.From || ia == m.Slot.To))) meshed = true;
                    }
                    if (meshed)
                    {
                        if (Math.Abs(d - (all[a].Pitch + all[b].Pitch)) > 0.01) bad.Add("unidos pero no a distancia de paso: " + ia + "-" + ib);
                    }
                    else if (d < all[a].Tip + all[b].Tip) bad.Add("se tocan sin estar unidos: " + ia + "-" + ib);
                }
            return bad;
        }
    }

    /// <summary>
    /// El cohete que se guarda (docs/diseno-engranajes.md §6): 10 luces por cohete que sobreviven entre partidas; al juntarse las 10 el cohete despega en ese momento y empieza uno
    /// nuevo. Nunca despega incompleto.
    /// </summary>
    public readonly struct RocketState
    {
        public readonly int Lights;
        public readonly int Orbit;
        public RocketState(int lights, int orbit)
        {
            Lights = Math.Max(0, Math.Min(EngranajesContract.RocketLights - 1, lights));
            Orbit = Math.Max(0, orbit);
        }

        /// <summary>Suma una luz. <paramref name="launched"/> = true si con esta luz se completaron las 10 (despega: queda sin luces y +1 en órbita).</summary>
        public RocketState AddLight(out bool launched)
        {
            if (Lights + 1 >= EngranajesContract.RocketLights)
            {
                launched = true;
                return new RocketState(0, Orbit + 1);
            }
            launched = false;
            return new RocketState(Lights + 1, Orbit);
        }

        /// <summary>El número del cohete en el que se está trabajando («Cohete n.º 3»).</summary>
        public int RocketNumber => Orbit + 1;
    }

    /// <summary>La medida de la partida (docs/diseno-engranajes.md §7): máquinas acertadas, etapa más alta, ritmo y los cohetes que despegaron.</summary>
    public sealed class EngranajesTally
    {
        public int Total { get; private set; }
        public int Correct { get; private set; }
        public int PeakLevel { get; private set; } = 1;
        public int Launches { get; private set; }
        private long _ms;

        public void Record(bool correct, int ms, int level)
        {
            Total++;
            if (correct) Correct++;
            _ms += Math.Max(0, ms);
            PeakLevel = Math.Max(PeakLevel, level);
        }

        public void Launched() => Launches++;

        public int PeakEtapa => EngranajesContract.Etapa(PeakLevel);
        /// <summary>Segundos medios por máquina, en ms; -1 sin máquinas.</summary>
        public int MeanMs => Total == 0 ? -1 : (int)(_ms / Total);
    }
}
