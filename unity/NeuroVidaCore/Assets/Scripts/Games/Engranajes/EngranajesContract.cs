using System;
using System.Collections.Generic;

namespace NeuroVida.Games.Engranajes
{
    /// <summary>Las cuatro piezas del cohete que se mueven con engranajes (de arriba a abajo en la columna del cohete).</summary>
    public enum Station { Antena, Compuerta, Carga, Turbina }

    public enum GearRole { Motor, Main, Station }

    /// <summary>Mesh = dientes que se tocan (giran al revés). Belt = correa entre dos engranajes lejanos (recta: mismo giro; cruzada: giro contrario).</summary>
    public enum LinkType { Mesh, Belt }

    /// <summary>Lo que debe hacer una pieza (su cartel) o lo que hace: girar como el reloj o al revés (antena, turbina), subir o bajar (carga), abrirse o cerrarse (compuerta).</summary>
    public enum Act { Cw, Ccw, Up, Down, Open, Close }

    public enum Intro { None, Taller, Correas, Ramas, Barras, Bien, Dos }

    /// <summary>Qué piezas lleva la etapa: solo antena y turbina (giro), una con barra dentada, o las cuatro posibles.</summary>
    public enum Pool { Turn, Rack, All }

    /// <summary>Qué falla en la máquina: todas las piezas (se arregla con el motor), una sola, cualquier grupo que se arregle con las llaves, o un grupo que pide justo dos cambios.</summary>
    public enum Fail { All, One, Any, Two }

    /// <summary>Qué late en las primeras máquinas de la etapa para decir qué se puede tocar.</summary>
    public enum Hint { Motor, Belt, Ok, Two }

    public sealed class GearDef
    {
        public float X, Y;
        public int Col, Row;
        public float Tip, Pitch;
        public int N;
        public GearRole Role;
        public bool HasStation;
        public Station Station;
        /// <summary>Ángulo (radianes) de un diente: las fases de los vecinos se calculan para que cada diente quede frente al hueco del otro.</summary>
        public float A;
        public bool PhaseSet;
    }

    public readonly struct Link
    {
        public readonly int A, B;
        public readonly LinkType Type;
        /// <summary>Solo para correas: true si viene cruzada (invierte el giro).</summary>
        public readonly bool Crossed;
        public Link(int a, int b, LinkType type, bool crossed = false) { A = a; B = b; Type = type; Crossed = crossed; }
    }

    /// <summary>Un interruptor: el motor (<see cref="EngranajesContract.MotorId"/>) o una correa (el índice de su unión). <see cref="Mask"/> = qué piezas (bit k = <c>Targets[k]</c>) cambian de sentido si se toca.</summary>
    public readonly struct Switch
    {
        public readonly int Id;
        public readonly int Mask;
        public Switch(int id, int mask) { Id = id; Mask = mask; }
    }

    public sealed class StageDef
    {
        public int Pieces, Belts, Keys;
        public Pool Pool;
        public Fail Fail;
        /// <summary>Probabilidad de que la máquina ya esté bien (no hay que cambiar nada).</summary>
        public float Ok;
        public Hint Hint;
        public Intro Intro;
    }

    /// <summary>Una máquina de la partida: los engranajes (en una cuadrícula de 4 × 6 y la columna del cohete), cómo se unen, qué falla y la solución mínima.</summary>
    public sealed class Machine
    {
        public List<GearDef> Gears = new List<GearDef>();
        public List<Link> Links = new List<Link>();
        public Station[] Targets;
        public StageDef Stage;
        public int Level;
        /// <summary>1 = el motor gira como el reloj, -1 = al revés.</summary>
        public int MotorDir;
        public Dictionary<Station, int> Stations = new Dictionary<Station, int>();
        public List<Switch> Switches = new List<Switch>();
        /// <summary>Las piezas que vienen fallando (bits sobre <see cref="Targets"/>); 0 = la máquina ya está bien.</summary>
        public int W;
        /// <summary>El mínimo de cambios (interruptores) que arregla la máquina.</summary>
        public int MinK;
        /// <summary>El cartel de cada pieza: lo que debe hacer.</summary>
        public Dictionary<Station, Act> Mission = new Dictionary<Station, Act>();
        /// <summary>Una solución mínima (ids de interruptor), para mostrarla si se equivoca.</summary>
        public int[] Solution = new int[0];
        public int Count => Gears.Count;
        public int FullMask => (1 << Targets.Length) - 1;
    }

    /// <summary>El resultado de hacer arrancar la máquina: sentido, velocidad y profundidad (pasos desde el motor) de cada engranaje.</summary>
    public sealed class Solution
    {
        public int[] Dir;
        public float[] Speed;
        public int[] Depth;
    }

    /// <summary>Lo que hizo una pieza al arrancar y si cumplió su cartel.</summary>
    public readonly struct PartResult
    {
        public readonly Station Station;
        public readonly Act Got;
        public readonly bool Ok;
        public PartResult(Station station, Act got, bool ok) { Station = station; Got = got; Ok = ok; }
    }

    /// <summary>
    /// Reglas puras de «Engranajes: Taller de reparación» (docs/diseno-engranajes.md; puerto fiel de la lógica del boceto docs/previews/engranajes-taller-boceto.html): la cuadrícula de la sala de
    /// máquinas, el árbol limpio del motor a las piezas, las correas, los interruptores (el motor y cada correa, cada uno con las piezas que mueve), el problema (qué piezas fallan y el mínimo de
    /// cambios que lo arregla), la física, el resultado de cada pieza y las 12 etapas. Se juzga el RESULTADO, no el camino.
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
        /// <summary>El id del interruptor del motor (los de las correas son el índice de su unión).</summary>
        public const int MotorId = -1;

        // ---- la cuadrícula (unidades del boceto; la pantalla las escala)
        public const float GridX0 = 40f, GridY0 = 150f, GridStep = 50f;
        public const int Cols = 4, Rows = 6;
        /// <summary>Casillas alternadas grandes (15 dientes) y chicas (10 dientes), como un tablero de ajedrez.</summary>
        public const float BigTip = 33f, BigPitch = 30f, SmallTip = 23f, SmallPitch = 20f;
        public const int BigTeeth = 15, SmallTeeth = 10;
        public const float ToothDepth = 6f;
        /// <summary>El radio de la polea que va en el eje de cada extremo de una correa.</summary>
        public const float PulleyRadius = 14f;
        /// <summary>Cuánto se aleja del trazo de la correa un toque y aún cuenta (unidades del boceto).</summary>
        public const float BeltTouch = 26f;

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
            x = GridX0 + col * GridStep;
            y = GridY0 + row * GridStep;
        }

        // ------------------------------------------------------------------ las 12 etapas

        public static readonly StageDef[] Stages =
        {
            Stage(1, Pool.Turn, 0, 1, Fail.All, 0f, Hint.Motor, Intro.Taller),
            Stage(1, Pool.Turn, 1, 1, Fail.All, 0f, Hint.Belt, Intro.Correas),
            Stage(2, Pool.Turn, 2, 1, Fail.One, 0f, Hint.Belt, Intro.Ramas),
            Stage(2, Pool.Turn, 2, 1, Fail.Any, 0f, Hint.Belt, Intro.None),
            Stage(2, Pool.Rack, 2, 1, Fail.Any, 0f, Hint.Belt, Intro.Barras),
            Stage(2, Pool.Rack, 2, 1, Fail.Any, 0.25f, Hint.Ok, Intro.Bien),
            Stage(3, Pool.All, 3, 1, Fail.Any, 0f, Hint.Belt, Intro.None),
            Stage(3, Pool.All, 3, 1, Fail.Any, 0.20f, Hint.Ok, Intro.None),
            Stage(2, Pool.All, 2, 2, Fail.Two, 0f, Hint.Two, Intro.Dos),
            Stage(3, Pool.All, 3, 2, Fail.Two, 0f, Hint.Two, Intro.None),
            Stage(3, Pool.All, 3, 2, Fail.Any, 0.15f, Hint.Two, Intro.None),
            Stage(3, Pool.All, 4, 2, Fail.Two, 0f, Hint.Two, Intro.None),
        };

        private static StageDef Stage(int pieces, Pool pool, int belts, int keys, Fail fail, float ok, Hint hint, Intro intro) =>
            new StageDef { Pieces = pieces, Pool = pool, Belts = belts, Keys = keys, Fail = fail, Ok = ok, Hint = hint, Intro = intro };

        private static readonly int[] EtapaOf = { 1, 1, 2, 2, 3, 3, 4, 4, 5, 5, 5, 5 };

        public static StageDef StageFor(int level) => Stages[Math.Max(1, Math.Min(MaxLevel, level)) - 1];

        /// <summary>Los 5 grupos de etapa de la pantalla final: motor y correas, ramas, carga y compuerta, tres piezas y dos llaves.</summary>
        public static int Etapa(int level) => EtapaOf[Math.Max(1, Math.Min(MaxLevel, level)) - 1];

        /// <summary>El texto de cada tarjeta «NUEVO» (una vez por instalación).</summary>
        public static string[] IntroText(Intro intro)
        {
            switch (intro)
            {
                case Intro.Taller: return new[] { "La máquina del cohete viene mal armada.", "Cada pieza tiene un cartel con lo que debe hacer.", "Toca el motor para cambiar su giro y arranca." };
                case Intro.Correas: return new[] { "Toca una correa para cruzarla o descruzarla.", "Recta: mismo giro.", "Cruzada (en X): giro contrario." };
                case Intro.Ramas: return new[] { "Ahora la fuerza se reparte en ramas.", "Un cambio antes de la rama cambia todo;", "uno dentro de la rama, solo esa pieza." };
                case Intro.Barras: return new[] { "Carga y compuerta van con una barra dentada.", "Si su engranaje gira como el reloj, la barra baja:", "la carga baja y la compuerta se cierra." };
                case Intro.Bien: return new[] { "A veces la máquina ya está bien.", "Si todo cumple su cartel, no cambies nada:", "solo arranca." };
                case Intro.Dos: return new[] { "Ahora tienes dos llaves.", "Puedes hacer hasta dos cambios." };
                default: return new string[0];
            }
        }

        /// <summary>La consigna de arriba: primera línea fija y segunda según lo que se puede hacer en la etapa.</summary>
        public static string[] Consigna(StageDef st)
        {
            string second;
            switch (st.Hint)
            {
                case Hint.Motor: second = "Toca el motor para cambiar su giro"; break;
                case Hint.Belt: second = "Un cambio: el motor o una correa"; break;
                case Hint.Ok: second = "Un cambio, o ninguno si ya está bien"; break;
                default: second = "Hasta dos cambios"; break;
            }
            return new[] { "Que cada pieza cumpla su cartel", second };
        }

        // ------------------------------------------------------------------ lo que hace cada pieza

        /// <summary>Lo que hace una pieza según el giro de su engranaje (barra dentada a la derecha del engranaje: si gira como el reloj, la barra baja).</summary>
        public static Act Behave(Station s, int dir)
        {
            if (s == Station.Carga) return dir > 0 ? Act.Down : Act.Up;
            if (s == Station.Compuerta) return dir > 0 ? Act.Close : Act.Open;
            return dir > 0 ? Act.Cw : Act.Ccw;
        }

        /// <summary>La palabra del cartel: reloj / al revés / sube / baja / se abre / se cierra.</summary>
        public static string MissionWord(Act a)
        {
            switch (a)
            {
                case Act.Cw: return "reloj";
                case Act.Ccw: return "al revés";
                case Act.Up: return "sube";
                case Act.Down: return "baja";
                case Act.Open: return "se abre";
                default: return "se cierra";
            }
        }

        // ------------------------------------------------------------------ generación

        private static T Pick<T>(IList<T> list, Random rng) => list[rng.Next(list.Count)];

        private static void Shuffle<T>(IList<T> list, Random rng)
        {
            for (int i = list.Count - 1; i > 0; i--)
            {
                int j = rng.Next(i + 1);
                T t = list[i]; list[i] = list[j]; list[j] = t;
            }
        }

        /// <summary>Las piezas de la etapa (el orden fija el bit de cada una en las máscaras). Con <paramref name="only"/> y una sola pieza, esa (para el tutorial).</summary>
        private static Station[] PickTargets(StageDef st, Random rng, Station? only)
        {
            var rack = Pick(new[] { Station.Compuerta, Station.Carga }, rng);
            if (st.Pieces == 1) return new[] { only ?? Pick(new[] { Station.Antena, Station.Turbina }, rng) };
            if (st.Pool == Pool.Turn) return new[] { Station.Antena, Station.Turbina };
            if (st.Pool == Pool.Rack) return new[] { Pick(new[] { Station.Antena, Station.Turbina }, rng), rack };
            if (st.Pieces == 3) return new[] { Station.Antena, rack, Station.Turbina };
            switch (rng.Next(3))
            {
                case 0: return new[] { Station.Antena, Station.Turbina };
                case 1: return new[] { Station.Antena, rack };
                default: return new[] { rack, Station.Turbina };
            }
        }

        /// <summary>Una máquina del nivel <paramref name="level"/> (1..12); siempre devuelve una (reintenta hasta lograrla). <paramref name="only"/> fija la pieza de las etapas de una sola.</summary>
        public static Machine Generate(int level, Random rng, Station? only = null)
        {
            var st = StageFor(level);
            for (int k = 0; k < 24; k++)
            {
                var m = TryGenerate(st, rng, only);
                if (m != null) { m.Level = Math.Max(1, Math.Min(MaxLevel, level)); return m; }
            }
            throw new InvalidOperationException("No se pudo armar una máquina del nivel " + level);
        }

        /// <summary>
        /// El árbol limpio: el motor en la columna 0 de la fila <paramref name="r0"/>, un tronco hasta la columna <paramref name="j"/>, desde ahí una vertical por la columna j hacia arriba y hacia abajo
        /// y, en la fila de cada pieza, un tramo a la derecha hasta la columna 4 (donde está su engranaje). Todo engranaje está en el camino del motor a alguna pieza.
        /// </summary>
        private static void Build(Machine m, Station[] targets, int j, int r0)
        {
            int Put(int c, int r, GearRole role)
            {
                CellXY(c, r, out float x, out float y);
                bool big = IsBig(c, r);
                m.Gears.Add(new GearDef
                {
                    X = x, Y = y, Col = c, Row = r, Role = role,
                    Tip = big ? BigTip : SmallTip, Pitch = big ? BigPitch : SmallPitch, N = big ? BigTeeth : SmallTeeth
                });
                return m.Gears.Count - 1;
            }

            void Join(int a, int b) => m.Links.Add(new Link(a, b, LinkType.Mesh));

            int prev = Put(0, r0, GearRole.Motor);
            for (int c = 1; c <= j; c++)
            {
                int g = Put(c, r0, GearRole.Main);
                Join(prev, g);
                prev = g;
            }
            int junction = prev;

            void Run(int from, int row, Station name)
            {
                int p = from;
                for (int c = j + 1; c <= 4; c++)
                {
                    int g = Put(c, row, c == 4 ? GearRole.Station : GearRole.Main);
                    Join(p, g);
                    p = g;
                }
                m.Gears[p].HasStation = true;
                m.Gears[p].Station = name;
            }

            foreach (int side in new[] { -1, 1 })
            {
                var ts = new List<Station>();
                foreach (var t in targets) if (Math.Sign(StationRow(t) - r0) == side) ts.Add(t);
                ts.Sort((a, b) => Math.Abs(StationRow(a) - r0).CompareTo(Math.Abs(StationRow(b) - r0)));
                int p = junction, r = r0;
                foreach (var t in ts)
                {
                    while (r != StationRow(t))
                    {
                        r += side;
                        int g = Put(j, r, GearRole.Main);
                        Join(p, g);
                        p = g;
                    }
                    Run(p, r, t);
                }
            }
            foreach (var t in targets) if (StationRow(t) == r0) Run(junction, r0, t);
        }

        /// <summary>Qué piezas mueve cada engranaje «aguas abajo» (máscara de bits sobre <c>m.Targets</c>), y de qué engranaje cuelga cada uno (-1 = el motor).</summary>
        public static void SubtreeMasks(Machine m, out int[] mask, out int[] par)
        {
            int n = m.Gears.Count;
            par = new int[n];
            for (int i = 0; i < n; i++) par[i] = -2;
            par[0] = -1;
            var order = new List<int> { 0 };
            for (int k = 0; k < order.Count; k++)
            {
                int i = order[k];
                foreach (var l in m.Links)
                {
                    int o = l.A == i ? l.B : l.B == i ? l.A : -1;
                    if (o < 0 || par[o] != -2) continue;
                    par[o] = i;
                    order.Add(o);
                }
            }
            mask = new int[n];
            for (int k = order.Count - 1; k >= 0; k--)
            {
                int i = order[k];
                var g = m.Gears[i];
                if (g.HasStation) mask[i] |= 1 << Array.IndexOf(m.Targets, g.Station);
                if (par[i] >= 0) mask[par[i]] |= mask[i];
            }
        }

        private static Machine TryGenerate(StageDef st, Random rng, Station? only)
        {
            for (int tries = 0; tries < 600; tries++)
            {
                var targets = PickTargets(st, rng, only);
                int j = st.Pieces == 1 && st.Belts == 0 ? 1 + rng.Next(3) : 1 + rng.Next(2);
                int r0 = rng.Next(Rows);
                var m = new Machine { Stage = st, Targets = targets, MotorDir = rng.NextDouble() < 0.5 ? 1 : -1 };
                Build(m, targets, j, r0);

                // correas: en un tramo recto de tres, el del medio se quita y los extremos quedan unidos por una correa
                var cand = new List<int[]>();
                for (int i = 0; i < m.Gears.Count; i++)
                {
                    var g = m.Gears[i];
                    if (g.Role != GearRole.Main) continue;
                    var ns = new List<int>();
                    foreach (var l in m.Links) if (l.A == i || l.B == i) ns.Add(l.A == i ? l.B : l.A);
                    if (ns.Count != 2) continue;
                    var a = m.Gears[ns[0]];
                    var c = m.Gears[ns[1]];
                    if ((a.Col == c.Col && a.Col == g.Col) || (a.Row == c.Row && a.Row == g.Row)) cand.Add(new[] { i, ns[0], ns[1] });
                }
                SubtreeMasks(m, out var mask0, out _);
                var used = new HashSet<int>();
                var seen = new HashSet<int>();
                var chosen = new List<int[]>();
                Shuffle(cand, rng);
                foreach (var t in cand)
                {
                    if (chosen.Count >= st.Belts) break;
                    if (used.Contains(t[0]) || used.Contains(t[1]) || used.Contains(t[2])) continue;
                    if (seen.Contains(mask0[t[2]]) && st.Pieces > 1) continue;      // cada correa mueve un grupo distinto de piezas: así es una decisión distinta
                    chosen.Add(t);
                    used.Add(t[0]); used.Add(t[1]); used.Add(t[2]);
                    seen.Add(mask0[t[2]]);
                }
                if (chosen.Count < st.Belts) continue;
                var removed = new HashSet<int>();
                var beltLinks = new List<Link>();
                foreach (var t in chosen)
                {
                    removed.Add(t[0]);
                    beltLinks.Add(new Link(t[1], t[2], LinkType.Belt, rng.NextDouble() < 0.5));
                }
                // compactar: sacar los engranajes quitados y renumerar
                var remap = new int[m.Gears.Count];
                var kept = new List<GearDef>();
                for (int i = 0; i < m.Gears.Count; i++)
                {
                    if (removed.Contains(i)) { remap[i] = -1; continue; }
                    remap[i] = kept.Count;
                    kept.Add(m.Gears[i]);
                }
                var links = new List<Link>();
                foreach (var l in m.Links)
                    if (remap[l.A] >= 0 && remap[l.B] >= 0) links.Add(new Link(remap[l.A], remap[l.B], l.Type, l.Crossed));
                foreach (var l in beltLinks) links.Add(new Link(remap[l.A], remap[l.B], LinkType.Belt, l.Crossed));
                m.Gears = kept;
                m.Links = links;
                for (int i = 0; i < m.Gears.Count; i++) if (m.Gears[i].HasStation) m.Stations[m.Gears[i].Station] = i;

                // interruptores: el motor (todas las piezas) y cada correa (las piezas que siguen después de ella)
                SubtreeMasks(m, out var mask, out var par);
                m.Switches.Add(new Switch(MotorId, m.FullMask));
                for (int i = 0; i < m.Links.Count; i++)
                {
                    var l = m.Links[i];
                    if (l.Type != LinkType.Belt) continue;
                    int down = par[l.B] == l.A ? l.B : l.A;
                    m.Switches.Add(new Switch(i, mask[down]));
                }

                // qué grupos de piezas se arreglan, y con cuántos cambios como mínimo
                var minK = new Dictionary<int, int> { { 0, 0 } };
                var order = new List<int> { 0 };
                foreach (var s in m.Switches) if (!minK.ContainsKey(s.Mask)) { minK[s.Mask] = 1; order.Add(s.Mask); }
                for (int a = 0; a < m.Switches.Count; a++)
                    for (int b = a + 1; b < m.Switches.Count; b++)
                    {
                        int x = m.Switches[a].Mask ^ m.Switches[b].Mask;
                        if (!minK.ContainsKey(x)) { minK[x] = 2; order.Add(x); }
                    }

                int w;
                if (st.Ok > 0f && rng.NextDouble() < st.Ok) w = 0;
                else
                {
                    var opts = new List<int>();
                    foreach (int mk in order) if (mk != 0 && minK[mk] <= st.Keys) opts.Add(mk);
                    var pool = new List<int>();
                    int full = m.FullMask;
                    switch (st.Fail)
                    {
                        case Fail.All: foreach (int o in opts) if (o == full) pool.Add(o); break;
                        case Fail.One: foreach (int o in opts) if ((o & (o - 1)) == 0) pool.Add(o); break;
                        case Fail.Two: foreach (int o in opts) if (minK[o] == 2) pool.Add(o); break;
                        default:
                            foreach (int o in opts) if (o != full) pool.Add(o);
                            if (pool.Count == 0 || rng.NextDouble() < 0.2) pool = opts;       // «todas fallan» (se arregla con el motor) sale a lo más ~20 %
                            break;
                    }
                    if (pool.Count == 0) continue;
                    w = Pick(pool, rng);
                }
                m.W = w;
                m.MinK = minK[w];

                var s0 = Solve(m, null);
                for (int k = 0; k < targets.Length; k++)
                {
                    int d = s0.Dir[m.Stations[targets[k]]];
                    m.Mission[targets[k]] = Behave(targets[k], ((w >> k) & 1) == 1 ? -d : d);
                }
                // una solución mínima, para mostrarla si se equivoca
                if (w != 0)
                {
                    foreach (var s in m.Switches) if (s.Mask == w) { m.Solution = new[] { s.Id }; break; }
                    if (m.Solution.Length == 0)
                        for (int a = 0; a < m.Switches.Count && m.Solution.Length == 0; a++)
                            for (int b = a + 1; b < m.Switches.Count; b++)
                                if ((m.Switches[a].Mask ^ m.Switches[b].Mask) == w) { m.Solution = new[] { m.Switches[a].Id, m.Switches[b].Id }; break; }
                }
                Phase(m, rng);
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

        /// <summary>Fases iniciales: desde el motor, cada engranaje unido por dientes queda encajado con el anterior (los de una correa no engranan: ángulo al azar).</summary>
        public static void Phase(Machine m, Random rng)
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
                    var gj = m.Gears[j];
                    seen.Add(j);
                    queue.Enqueue(j);
                    gj.A = l.Type == LinkType.Mesh ? MeshPhase(gi, gj) : (float)(rng.NextDouble() * 6.28);
                    gj.PhaseSet = true;
                }
            }
        }

        // ------------------------------------------------------------------ física

        /// <summary>
        /// Sentido y velocidad de cada engranaje con los <paramref name="changes"/> aplicados (ids de interruptor; null = ninguno): dos engranajes que se tocan giran en sentido contrario; una correa
        /// recta conserva el sentido y una cruzada lo invierte (y tocarla la invierte); tocar el motor invierte su giro. La velocidad va según el tamaño (chico = más rápido).
        /// </summary>
        public static Solution Solve(Machine m, ICollection<int> changes)
        {
            int n = m.Gears.Count;
            var sol = new Solution { Dir = new int[n], Speed = new float[n], Depth = new int[n] };
            for (int i = 0; i < n; i++) sol.Depth[i] = -1;
            int md = m.MotorDir * (changes != null && changes.Contains(MotorId) ? -1 : 1);
            sol.Dir[0] = md;
            sol.Speed[0] = md;
            sol.Depth[0] = 0;
            var queue = new Queue<int>();
            queue.Enqueue(0);
            while (queue.Count > 0)
            {
                int i = queue.Dequeue();
                for (int li = 0; li < m.Links.Count; li++)
                {
                    var l = m.Links[li];
                    int j = l.A == i ? l.B : l.B == i ? l.A : -1;
                    if (j < 0 || sol.Depth[j] >= 0) continue;
                    bool flip = l.Type == LinkType.Mesh || (l.Crossed != (changes != null && changes.Contains(li)));
                    sol.Dir[j] = flip ? -sol.Dir[i] : sol.Dir[i];
                    sol.Speed[j] = Math.Abs(sol.Speed[i]) * m.Gears[i].Pitch / m.Gears[j].Pitch * sol.Dir[j];
                    sol.Depth[j] = sol.Depth[i] + 1;
                    queue.Enqueue(j);
                }
            }
            return sol;
        }

        /// <summary>Lo que hizo cada pieza con esos cambios, y si cumplió su cartel.</summary>
        public static PartResult[] Results(Machine m, ICollection<int> changes)
        {
            var s = Solve(m, changes);
            var res = new PartResult[m.Targets.Length];
            for (int k = 0; k < res.Length; k++)
            {
                var t = m.Targets[k];
                var got = Behave(t, s.Dir[m.Stations[t]]);
                res[k] = new PartResult(t, got, got == m.Mission[t]);
            }
            return res;
        }

        public static bool AllOk(PartResult[] results)
        {
            foreach (var r in results) if (!r.Ok) return false;
            return true;
        }

        /// <summary>Lo que mueve un cambio: el engranaje que cuelga de él y todo lo que sigue después (para marcarlo en celeste al equivocarse).</summary>
        public static HashSet<int> Downstream(Machine m, int switchId)
        {
            SubtreeMasks(m, out _, out var par);
            int root = 0;
            if (switchId != MotorId)
            {
                var l = m.Links[switchId];
                root = par[l.B] == l.A ? l.B : l.A;
            }
            var res = new HashSet<int>();
            var stack = new Stack<int>();
            stack.Push(root);
            while (stack.Count > 0)
            {
                int i = stack.Pop();
                res.Add(i);
                for (int j = 0; j < m.Gears.Count; j++) if (par[j] == i) stack.Push(j);
            }
            return res;
        }

        // ------------------------------------------------------------------ al arrancar: qué dice el aviso

        /// <summary>Lo que falló, en una frase («La turbina giró al revés de su cartel»).</summary>
        public static string FailText(PartResult[] results)
        {
            var fails = new List<PartResult>();
            foreach (var r in results) if (!r.Ok) fails.Add(r);
            if (fails.Count == 0) return "";
            if (fails.Count == 1)
            {
                var f = fails[0];
                switch (f.Station)
                {
                    case Station.Carga: return f.Got == Act.Up ? "La carga subió en vez de bajar" : "La carga bajó en vez de subir";
                    case Station.Compuerta: return f.Got == Act.Open ? "La compuerta se abrió en vez de cerrarse" : "La compuerta se cerró en vez de abrirse";
                    case Station.Antena: return "La antena giró al revés de su cartel";
                    default: return "La turbina giró al revés de su cartel";
                }
            }
            var names = new List<string>();
            foreach (var f in fails) names.Add(StationName(f.Station));
            string last = names[names.Count - 1];
            names.RemoveAt(names.Count - 1);
            return "Fallaron " + string.Join(", ", names) + " y " + last;
        }

        /// <summary>La pista de la segunda línea del aviso de error.</summary>
        public static string HintText(Machine m, PartResult[] results, ICollection<int> changes)
        {
            if (changes == null || changes.Count == 0) return "Brilla en dorado lo que había que cambiar";
            var before = Results(m, null);
            for (int k = 0; k < results.Length; k++)
                if (!results[k].Ok && before[k].Ok) return "Tu cambio también movió " + StationName(results[k].Station);
            return "Truco: cada engranaje que toca gira al revés";
        }

        /// <summary>El aviso al acertar (rota entre tres; si ya estaba bien y no se tocó nada, uno propio).</summary>
        public static string SuccessText(Machine m, ICollection<int> changes, int count)
        {
            if (m.W == 0 && (changes == null || changes.Count == 0)) return "¡Bien visto! Ya estaba lista";
            return new[] { "¡Cohete listo!", "¡Arreglado!", "¡Todo en orden!" }[Math.Abs(count) % 3];
        }

        // ------------------------------------------------------------------ puntaje

        /// <summary>Puntaje 0-100: máquinas arregladas sobre las 10 de Precisión; en el Reto, sobre el ritmo de referencia.</summary>
        public static int Score(int correct, bool endless)
        {
            float denom = endless ? RetoReferenceMachines : PrecisionMachines;
            return Math.Max(0, Math.Min(100, (int)Math.Round(100f * correct / denom)));
        }

        /// <summary>Segundos de cada paso de la cascada (más lento para mayores: «más tiempo para mirar»).</summary>
        public static float StepSeconds(bool senior) => senior ? 0.21f : 0.15f;

        // ------------------------------------------------------------------ chequeos de coherencia (los usan las pruebas)

        /// <summary>Pares de engranajes que se tocan sin estar unidos por dientes (sus puntas se pisan), o unidos por dientes pero no a distancia de paso. Una correa une engranajes lejanos: no cuenta como contacto.</summary>
        public static List<string> UnwantedContacts(Machine m)
        {
            var bad = new List<string>();
            for (int a = 0; a < m.Gears.Count; a++)
                for (int b = a + 1; b < m.Gears.Count; b++)
                {
                    var ga = m.Gears[a];
                    var gb = m.Gears[b];
                    double d = Math.Sqrt((ga.X - gb.X) * (ga.X - gb.X) + (ga.Y - gb.Y) * (ga.Y - gb.Y));
                    bool meshed = false;
                    foreach (var l in m.Links)
                        if (l.Type == LinkType.Mesh && ((l.A == a && l.B == b) || (l.A == b && l.B == a))) meshed = true;
                    if (meshed)
                    {
                        if (Math.Abs(d - (ga.Pitch + gb.Pitch)) > 0.01) bad.Add("unidos pero no a distancia de paso: " + a + "-" + b);
                    }
                    else if (d < ga.Tip + gb.Tip) bad.Add("se tocan sin estar unidos: " + a + "-" + b);
                }
            return bad;
        }
    }

    /// <summary>
    /// El cohete que se guarda (docs/diseno-engranajes.md §8): 10 luces por cohete que sobreviven entre partidas; al juntarse las 10 el cohete despega en ese momento y empieza uno
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

    /// <summary>La medida de la partida (docs/diseno-engranajes.md §9): máquinas arregladas, etapa más alta, ritmo y los cohetes que despegaron.</summary>
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
