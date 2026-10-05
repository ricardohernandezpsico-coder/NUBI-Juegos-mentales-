using System;
using System.Collections.Generic;
using NUnit.Framework;
using NeuroVida.Games.Engranajes;

namespace NeuroVida.Games.Engranajes.Tests
{
    /// <summary>«Engranajes» (docs/diseno-engranajes.md): cientos de máquinas por etapa sin contactos no deseados, con todas las ramas terminando en una pieza, con la física y las respuestas
    /// correctas y balanceadas, y el cohete que se guarda.</summary>
    public class EngranajesContractTests
    {
        private const int PerLevel = 300;

        private static Random Rng(int seed) => new Random(seed);

        private static IEnumerable<Machine> Machines(int level, int count, int seed)
        {
            var rng = Rng(seed * 31 + level);
            for (int k = 0; k < count; k++) yield return EngranajesContract.Generate(level, rng);
        }

        private static Piece PieceOf(Machine m) => m.Truth == Answer.Gear ? Piece.Gear : m.Truth == Answer.Crossed ? Piece.Crossed : Piece.None;

        // ------------------------------------------------------------------ etapas

        [Test]
        public void ThereAreTwelveStages_WithTheIntroductionsInOrder_AndFiveEtapas()
        {
            Assert.AreEqual(12, EngranajesContract.Stages.Length);
            Assert.AreEqual(12, EngranajesContract.MaxLevel);
            var intros = new List<Intro>();
            foreach (var st in EngranajesContract.Stages) if (st.Intro != Intro.None) intros.Add(st.Intro);
            CollectionAssert.AreEqual(new[] { Intro.Ramas, Intro.Correas, Intro.Carga, Intro.Compuerta, Intro.Velocidad, Intro.Traba, Intro.Arma }, intros);
            foreach (var i in intros) Assert.AreEqual(2, EngranajesContract.IntroText(i).Length, i + ": dos líneas");
            int[] etapas = { 1, 1, 2, 2, 3, 3, 4, 4, 5, 5, 5, 5 };
            for (int l = 1; l <= 12; l++) Assert.AreEqual(etapas[l - 1], EngranajesContract.Etapa(l), "etapa del nivel " + l);
            Assert.AreEqual(Question.Dir, EngranajesContract.StageFor(1).Q);
            Assert.AreEqual(Question.Jam, EngranajesContract.StageFor(9).Q);
            Assert.AreEqual(Question.Build, EngranajesContract.StageFor(10).Q);
            Assert.AreEqual(Question.Build, EngranajesContract.StageFor(12).Q);
            Assert.AreEqual(1, EngranajesContract.StageFor(0).Targets.Length > 0 ? 1 : 0, "los niveles se acotan");
        }

        // ------------------------------------------------------------------ cuadrícula

        [Test]
        public void TheGrid_AlternatesBigAndSmallGears_SoNeighboursMeshExactlyAndDiagonalsNeverTouch()
        {
            for (int c = 0; c <= 4; c++)
                for (int r = 0; r < EngranajesContract.Rows; r++)
                {
                    EngranajesContract.CellXY(c, r, out float x, out float y);
                    float pitch = EngranajesContract.IsBig(c, r) ? EngranajesContract.BigPitch : EngranajesContract.SmallPitch;
                    float tip = EngranajesContract.IsBig(c, r) ? EngranajesContract.BigTip : EngranajesContract.SmallTip;
                    foreach (var d in new[] { new[] { 1, 0 }, new[] { 0, 1 } })
                    {
                        int nc = c + d[0], nr = r + d[1];
                        if (nc > 4 || nr >= EngranajesContract.Rows) continue;
                        EngranajesContract.CellXY(nc, nr, out float x2, out float y2);
                        float pitch2 = EngranajesContract.IsBig(nc, nr) ? EngranajesContract.BigPitch : EngranajesContract.SmallPitch;
                        Assert.AreEqual(EngranajesContract.GridStep, pitch + pitch2, 1e-4f, "vecinos " + c + "," + r + " y " + nc + "," + nr + ": engranan justo");
                        Assert.AreEqual(EngranajesContract.GridStep, Math.Sqrt((x2 - x) * (x2 - x) + (y2 - y) * (y2 - y)), 1e-3);
                    }
                    if (c < 4 && r + 1 < EngranajesContract.Rows)
                    {
                        EngranajesContract.CellXY(c + 1, r + 1, out float dx, out float dy);
                        float tip2 = EngranajesContract.IsBig(c + 1, r + 1) ? EngranajesContract.BigTip : EngranajesContract.SmallTip;
                        Assert.Greater(Math.Sqrt((dx - x) * (dx - x) + (dy - y) * (dy - y)), tip + tip2, "diagonal " + c + "," + r + ": nunca se tocan");
                    }
                }
            Assert.AreEqual(0, EngranajesContract.StationRow(Station.Antena));
            Assert.AreEqual(2, EngranajesContract.StationRow(Station.Compuerta));
            Assert.AreEqual(3, EngranajesContract.StationRow(Station.Carga));
            Assert.AreEqual(5, EngranajesContract.StationRow(Station.Turbina));
            Assert.IsTrue(EngranajesContract.IsBig(4, EngranajesContract.StationRow(Station.Antena)), "la antena es grande");
            Assert.IsFalse(EngranajesContract.IsBig(4, EngranajesContract.StationRow(Station.Turbina)), "la turbina es chica");
        }

        // ------------------------------------------------------------------ caminos

        [Test]
        public void Route_EachNewCellOnlyTouchesThePreviousOne_AndEntersTheRocketColumnFromTheLeft()
        {
            var rng = Rng(5);
            for (int k = 0; k < 400; k++)
            {
                int fromRow = rng.Next(EngranajesContract.Rows);
                int toRow = EngranajesContract.StationRow(EngranajesContract.AllStations[rng.Next(4)]);
                var occ = new Dictionary<int, int>();
                var p = EngranajesContract.BestRoute(occ, 0, fromRow, 4, toRow, 14, rng);
                if (p == null) continue;
                var seen = new List<int[]> { new[] { 0, fromRow } };
                foreach (var c in p)
                {
                    var prev = seen[seen.Count - 1];
                    Assert.AreEqual(1, Math.Abs(c[0] - prev[0]) + Math.Abs(c[1] - prev[1]), "paso de a una casilla");
                    foreach (var o in seen.GetRange(0, seen.Count - 1))
                        Assert.Greater(Math.Abs(c[0] - o[0]) + Math.Abs(c[1] - o[1]), 1, "la casilla " + c[0] + "," + c[1] + " toca a una que no es la anterior");
                    Assert.IsTrue(c[0] <= 3 || (c[0] == 4 && c[1] == toRow), "solo el último paso entra a la columna del cohete");
                    seen.Add(c);
                }
                var last = p[p.Count - 1];
                Assert.AreEqual(4, last[0]);
                Assert.AreEqual(toRow, last[1]);
                Assert.AreEqual(3, p[p.Count - 2][0], "a la columna del cohete se entra desde la izquierda");
            }
        }

        [Test]
        public void BestRoute_NeverHasMoreTurnsThanTheStraightLineNeeds_PlusTwo()
        {
            var rng = Rng(9);
            for (int row = 0; row < EngranajesContract.Rows; row++)
                foreach (var s in EngranajesContract.AllStations)
                {
                    var p = EngranajesContract.BestRoute(new Dictionary<int, int>(), 0, row, 4, EngranajesContract.StationRow(s), 14, rng);
                    Assert.IsNotNull(p, "fila " + row + " → " + s);
                    // sin obstáculos basta una vuelta (derecho y una subida o bajada)
                    Assert.LessOrEqual(EngranajesContract.Turns(0, row, p), 3, "fila " + row + " → " + s + ": demasiadas vueltas");
                }
        }

        // ------------------------------------------------------------------ las máquinas

        [Test]
        public void EveryMachine_AtEveryLevel_HasNoUnwantedContacts_AndEveryGearIsReachable()
        {
            for (int level = 1; level <= 12; level++)
                foreach (var m in Machines(level, PerLevel, 1))
                {
                    string where = "nivel " + level;
                    Assert.IsEmpty(EngranajesContract.UnwantedContacts(m, Piece.None), where + ": contactos no deseados");
                    var sol = EngranajesContract.Solve(m, m.Q == Question.Build ? PieceOf(m) : Piece.None);
                    for (int i = 0; i < m.Gears.Count; i++)
                        if (!m.Gears[i].Removed) Assert.GreaterOrEqual(sol.Depth[i], 0, where + ": el engranaje " + i + " no recibe fuerza del motor");
                    if (m.Q == Question.Build) Assert.IsEmpty(EngranajesContract.UnwantedContacts(m, Piece.Gear), where + ": contactos con el engranaje del hueco");
                }
        }

        [Test]
        public void EveryBranch_EndsInAnotherPieceOfTheRocket_AndNothingEndsInNothing()
        {
            for (int level = 1; level <= 12; level++)
            {
                var st = EngranajesContract.StageFor(level);
                foreach (var m in Machines(level, PerLevel, 2))
                {
                    string where = "nivel " + level;
                    var degree = new int[m.Gears.Count];
                    foreach (var l in m.Links) { degree[l.A]++; degree[l.B]++; }
                    var ends = new List<Station>();
                    for (int i = 1; i < m.Gears.Count; i++)
                    {
                        if (m.Gears[i].Removed) continue;
                        if (degree[i] == 1)
                        {
                            Assert.IsTrue(m.Gears[i].HasStation, where + ": el engranaje " + i + " es una rama que termina en la nada");
                            Assert.AreEqual(GearRole.Station, m.Gears[i].Role);
                            ends.Add(m.Gears[i].Station);
                        }
                        else Assert.GreaterOrEqual(degree[i], 2, where + ": engranaje suelto " + i);
                    }
                    Assert.AreEqual(1 + st.Branches, ends.Count, where + ": una pieza por cada camino");
                    CollectionAssert.AllItemsAreUnique(ends, where + ": cada pieza la mueve un solo camino");
                    Assert.Contains(m.TargetStation, ends, where + ": la pieza preguntada recibe fuerza");
                    Assert.IsFalse(ends.Contains(Station.Compuerta) && ends.Contains(Station.Carga), where + ": compuerta y carga nunca van activas a la vez");
                    Assert.Contains(m.TargetStation, st.Targets, where + ": la pieza preguntada es de esta etapa");
                    foreach (var kv in m.Stations) Assert.AreEqual(kv.Key, m.Gears[kv.Value].Station);
                    Assert.AreEqual(ends.Count, m.Stations.Count);
                }
            }
        }

        [Test]
        public void EveryStage_HasExactlyTheBranchesBeltsGapAndTrapItAsksFor()
        {
            for (int level = 1; level <= 12; level++)
            {
                var st = EngranajesContract.StageFor(level);
                int jams = 0;
                foreach (var m in Machines(level, PerLevel, 3))
                {
                    int belts = 0, gaps = 0;
                    foreach (var l in m.Links)
                    {
                        if (l.Type == LinkType.Straight || l.Type == LinkType.Crossed) belts++;
                        if (l.Type == LinkType.Gap) gaps++;
                    }
                    Assert.AreEqual(st.Belts, belts, "nivel " + level + ": correas");
                    Assert.AreEqual(st.Q == Question.Build ? 1 : 0, gaps, "nivel " + level + ": huecos");
                    Assert.AreEqual(st.Q == Question.Build, m.Slot != null);
                    if (m.Jam) { jams++; Assert.AreEqual(Question.Jam, st.Q, "solo el nivel 9 lleva trampa"); }
                    int removed = 0;
                    foreach (var g in m.Gears) if (g.Removed) removed++;
                    Assert.AreEqual(st.Belts + (st.Q == Question.Build ? 1 : 0), removed, "nivel " + level + ": engranajes quitados");
                }
                if (st.Q == Question.Jam) Assert.Greater(jams, PerLevel / 5, "la trampa aparece (~45 %)");
            }
        }

        [Test]
        public void TheFarStages_PutTheMotorFarFromThePiece_AndTheOthersNear()
        {
            for (int level = 1; level <= 12; level++)
            {
                var st = EngranajesContract.StageFor(level);
                foreach (var m in Machines(level, 100, 4))
                {
                    int dist = Math.Abs(m.Gears[0].Row - EngranajesContract.StationRow(m.TargetStation));
                    if (st.Far) Assert.GreaterOrEqual(dist, 2, "nivel " + level);
                    else Assert.LessOrEqual(dist, 2, "nivel " + level);
                }
            }
        }

        [Test]
        public void TheTrap_IsASmallGearTouchingTwoNeighboursOfThePath_FormingATriangle()
        {
            int found = 0;
            foreach (var m in Machines(9, 400, 5))
            {
                if (!m.Jam) continue;
                found++;
                Assert.AreEqual(3, m.JamTri.Length);
                var j = m.Gears[m.JamTri[2]];
                Assert.AreEqual(GearRole.Jam, j.Role);
                Assert.Less(j.Tip, EngranajesContract.SmallTip);
                var a = m.Gears[m.JamTri[0]];
                var b = m.Gears[m.JamTri[1]];
                Assert.AreEqual(a.Pitch + b.Pitch, Math.Sqrt((a.X - b.X) * (a.X - b.X) + (a.Y - b.Y) * (a.Y - b.Y)), 1e-3, "los dos vecinos ya engranan entre sí");
                Assert.AreEqual(a.Pitch + j.Pitch, Math.Sqrt((a.X - j.X) * (a.X - j.X) + (a.Y - j.Y) * (a.Y - j.Y)), 1e-2);
                Assert.AreEqual(b.Pitch + j.Pitch, Math.Sqrt((b.X - j.X) * (b.X - j.X) + (b.Y - j.Y) * (b.Y - j.Y)), 1e-2);
                Assert.AreEqual(Answer.Stuck, m.Truth);
            }
            Assert.Greater(found, 100, "hay trampas");
        }

        // ------------------------------------------------------------------ física

        private static Machine Chain(params GearDef[] gears)
        {
            var m = new Machine { Q = Question.Dir };
            foreach (var g in gears) m.Gears.Add(g);
            m.MotorDir = 1;
            return m;
        }

        private static GearDef G(float pitch, float x = 0f) => new GearDef { Pitch = pitch, Tip = pitch + 3f, N = (int)(pitch / 2f), X = x };

        [Test]
        public void Solve_TouchingGearsTurnOppositeWays_AndTheSpeedFollowsTheSize()
        {
            var m = Chain(G(30), G(20), G(30), G(20));
            m.Links.Add(new Link(0, 1, LinkType.Mesh));
            m.Links.Add(new Link(1, 2, LinkType.Mesh));
            m.Links.Add(new Link(2, 3, LinkType.Mesh));
            var s = EngranajesContract.Solve(m, Piece.None);
            CollectionAssert.AreEqual(new[] { 1, -1, 1, -1 }, s.Dir);
            CollectionAssert.AreEqual(new[] { 0, 1, 2, 3 }, s.Depth);
            Assert.AreEqual(1.5f, Math.Abs(s.Speed[1]), 1e-4f, "uno chico gira más rápido (30/20)");
            Assert.AreEqual(1f, Math.Abs(s.Speed[2]), 1e-4f, "el intermedio no cambia la razón entre el motor y la pieza");
            Assert.AreEqual(1.5f, Math.Abs(s.Speed[3]), 1e-4f);
        }

        [Test]
        public void Solve_AStraightBeltKeepsTheDirection_AndACrossedBeltReversesIt()
        {
            var straight = Chain(G(20), G(20));
            straight.Links.Add(new Link(0, 1, LinkType.Straight));
            Assert.AreEqual(1, EngranajesContract.Solve(straight, Piece.None).Dir[1]);
            var crossed = Chain(G(20), G(20));
            crossed.Links.Add(new Link(0, 1, LinkType.Crossed));
            Assert.AreEqual(-1, EngranajesContract.Solve(crossed, Piece.None).Dir[1]);
        }

        [Test]
        public void Solve_TheGap_DoesNotCarryForceUntilAPieceIsPlaced()
        {
            var m = Chain(G(20), G(20));
            m.Links.Add(new Link(0, 1, LinkType.Gap));
            Assert.AreEqual(-1, EngranajesContract.Solve(m, Piece.None).Depth[1], "sin pieza no pasa nada");
            var withGear = EngranajesContract.Solve(m, Piece.Gear);
            Assert.AreEqual(1, withGear.Dir[1], "con un engranaje en medio: dos contactos, mismo sentido");
            Assert.AreEqual(2, withGear.Depth[1], "y dos pasos de cascada");
            var withBelt = EngranajesContract.Solve(m, Piece.Crossed);
            Assert.AreEqual(-1, withBelt.Dir[1]);
            Assert.AreEqual(1, withBelt.Depth[1]);
        }

        [Test]
        public void Effect_BarToTheRight_ClockwiseLowersTheLoadAndClosesTheGate()
        {
            var m = Chain(G(30), G(20));
            m.Links.Add(new Link(0, 1, LinkType.Mesh));
            m.Target = 1;
            m.TargetStation = Station.Carga;
            m.MotorDir = -1;                       // el motor al revés → el engranaje de la pieza gira como el reloj
            Assert.AreEqual(Answer.Down, EngranajesContract.Effect(m, EngranajesContract.Solve(m, Piece.None)));
            m.MotorDir = 1;
            Assert.AreEqual(Answer.Up, EngranajesContract.Effect(m, EngranajesContract.Solve(m, Piece.None)));
            m.TargetStation = Station.Compuerta;
            Assert.AreEqual(Answer.Open, EngranajesContract.Effect(m, EngranajesContract.Solve(m, Piece.None)));
            m.MotorDir = -1;
            Assert.AreEqual(Answer.Close, EngranajesContract.Effect(m, EngranajesContract.Solve(m, Piece.None)));
            m.Jam = true;
            Assert.AreEqual(Answer.Stuck, EngranajesContract.Effect(m, EngranajesContract.Solve(m, Piece.None)), "con traba nada se mueve");
        }

        // ------------------------------------------------------------------ respuestas

        [Test]
        public void TheCorrectAnswer_MatchesThePhysics_AndIsOneOfTheButtons()
        {
            for (int level = 1; level <= 12; level++)
                foreach (var m in Machines(level, PerLevel, 6))
                {
                    var buttons = EngranajesContract.Buttons(m);
                    bool has = false;
                    foreach (var b in buttons) if (b.Value == m.Truth) has = true;
                    Assert.IsTrue(has, "nivel " + level + ": la respuesta correcta (" + m.Truth + ") no es un botón");
                    Assert.AreEqual(m.Q == Question.Jam ? 3 : 2, buttons.Length);
                    Assert.IsNotEmpty(EngranajesContract.QuestionText(m));
                    Assert.IsNotEmpty(EngranajesContract.Explain(m));
                    var s = EngranajesContract.Solve(m, Piece.None);
                    switch (m.Q)
                    {
                        case Question.Dir:
                            Assert.AreEqual(s.Dir[m.Target] > 0 ? Answer.Cw : Answer.Ccw, m.Truth);
                            break;
                        case Question.Jam:
                            Assert.AreEqual(m.Jam ? Answer.Stuck : (s.Dir[m.Target] > 0 ? Answer.Cw : Answer.Ccw), m.Truth);
                            break;
                        case Question.Rack:
                            Assert.AreEqual(EngranajesContract.Effect(m, s), m.Truth);
                            Assert.IsTrue(m.TargetStation == Station.Carga || m.TargetStation == Station.Compuerta);
                            break;
                        case Question.Speed:
                            float ratio = Math.Abs(s.Speed[m.Target]);
                            Assert.IsFalse(ratio > 0.8f && ratio < 1.25f, "la velocidad se parece demasiado a la del motor");
                            Assert.AreEqual(ratio > 1f ? Answer.Fast : Answer.Slow, m.Truth);
                            break;
                    }
                }
        }

        [Test]
        public void InBuildStages_TheTwoPiecesAlwaysGiveDifferentResults_AndTheTruthIsTheOneThatGetsTheGoal()
        {
            for (int level = 10; level <= 12; level++)
            {
                if (EngranajesContract.StageFor(level).Q != Question.Build) continue;
                foreach (var m in Machines(level, PerLevel, 7))
                {
                    var eG = EngranajesContract.Effect(m, EngranajesContract.Solve(m, Piece.Gear));
                    var eC = EngranajesContract.Effect(m, EngranajesContract.Solve(m, Piece.Crossed));
                    Assert.AreNotEqual(eG, eC, "nivel " + level + ": las dos piezas dan lo mismo");
                    Assert.AreEqual(m.TargetStation == Station.Carga ? Answer.Up : Answer.Open, m.Goal);
                    Assert.AreEqual(m.Truth == Answer.Gear ? eG : eC, m.Goal, "la respuesta correcta logra lo que pide Nubi");
                    Assert.AreNotEqual(m.Truth == Answer.Gear ? eC : eG, m.Goal, "la otra no lo logra");
                }
            }
        }

        [Test]
        public void TheAnswers_AreBalanced()
        {
            // sentido 50/50 (el motor gira al azar), sube/baja, abre/cierra y rápido/lento según la pieza
            foreach (int level in new[] { 1, 3, 5, 6, 7, 8, 9, 10, 11, 12 })
            {
                var counts = new Dictionary<Answer, int>();
                const int n = 600;
                foreach (var m in Machines(level, n, 8))
                {
                    counts.TryGetValue(m.Truth, out int c);
                    counts[m.Truth] = c + 1;
                }
                foreach (var kv in counts)
                {
                    if (kv.Key == Answer.Stuck) continue;   // la traba va aparte (~45 % del nivel 9)
                    var st = EngranajesContract.StageFor(level);
                    double share = kv.Value / (double)n;
                    double expected = st.Q == Question.Rack && st.Targets.Length == 2 ? 0.25 : 0.5;   // carga Y compuerta: cuatro respuestas, a un cuarto cada una
                    if (st.Q == Question.Jam) expected = (1 - counts.GetValueOrDefault(Answer.Stuck) / (double)n) / 2;
                    Assert.AreEqual(expected, share, 0.08, "nivel " + level + ": " + kv.Key + " = " + kv.Value + " de " + n);
                }
            }
        }

        [Test]
        public void TheTrapStage_AsksForStuck_AboutAThirdToHalfOfTheTime()
        {
            int stuck = 0, n = 600;
            foreach (var m in Machines(9, n, 9)) if (m.Truth == Answer.Stuck) stuck++;
            Assert.AreEqual(0.45, stuck / (double)n, 0.08);
        }

        [Test]
        public void TheSpeedStages_AskAboutBothFastAndSlow_AndAntennaIsSlowWhenTheMotorIsSmallAndViceVersa()
        {
            int fast = 0, slow = 0;
            foreach (var m in Machines(7, 400, 10))
            {
                float motor = m.Gears[0].Pitch, target = m.Gears[m.Target].Pitch;
                Assert.AreNotEqual(motor, target, "un motor del mismo tamaño no da velocidad distinta");
                if (m.Truth == Answer.Fast) { fast++; Assert.Less(target, motor); }
                else { slow++; Assert.Greater(target, motor); }
            }
            Assert.Greater(fast, 120);
            Assert.Greater(slow, 120);
        }

        // ------------------------------------------------------------------ dientes que encajan

        [Test]
        public void TheTeethMesh_EveryToothFacesTheGapOfItsNeighbour()
        {
            for (int level = 1; level <= 12; level++)
                foreach (var m in Machines(level, 100, 11))
                {
                    if (m.Q == Question.Build) EngranajesContract.Phase(m, PieceOf(m), Rng(1));
                    foreach (var l in m.Links)
                    {
                        if (l.Type != LinkType.Mesh) continue;
                        if (m.Gears[l.A].Role == GearRole.Jam || m.Gears[l.B].Role == GearRole.Jam) continue;   // el triángulo de la trampa no puede encajar con los dos (por eso se traba)
                        AssertMeshed(m.Gears[l.A], m.Gears[l.B], "nivel " + level);
                    }
                    if (m.Jam)
                    {
                        // …pero con uno de sus dos vecinos sí encaja (se ve el diente en el hueco)
                        int ji = m.JamTri[2];
                        bool one = false;
                        foreach (var l in m.Links)
                        {
                            if (l.B != ji && l.A != ji) continue;
                            var o = m.Gears[l.A == ji ? l.B : l.A];
                            try { AssertMeshed(o, m.Gears[ji], "x"); one = true; } catch (AssertionException) { }
                        }
                        Assert.IsTrue(one, "nivel " + level + ": el engranaje de la trampa no encaja con ninguno");
                    }
                    if (m.Q == Question.Build && PieceOf(m) == Piece.Gear)
                    {
                        AssertMeshed(m.Gears[m.Slot.From], m.Slot.Gear, "nivel " + level + " (hueco, entrada)");
                        AssertMeshed(m.Slot.Gear, m.Gears[m.Slot.To], "nivel " + level + " (hueco, salida)");
                    }
                }
        }

        private static void AssertMeshed(GearDef a, GearDef b, string where)
        {
            // en la dirección de a a b, el diente de a (fracción fa de su paso) debe quedar frente a un hueco de b (fracción 0,5 - fa)
            double th = Math.Atan2(b.Y - a.Y, b.X - a.X);
            double pa = 2 * Math.PI / a.N, pb = 2 * Math.PI / b.N;
            double fa = (((th - a.A) / pa) % 1.0 + 1.0) % 1.0;
            double fb = (((th + Math.PI - b.A) / pb) % 1.0 + 1.0) % 1.0;
            double want = ((0.5 - fa) % 1.0 + 1.0) % 1.0;
            double diff = Math.Abs(fb - want);
            diff = Math.Min(diff, 1.0 - diff);
            Assert.Less(diff, 1e-3, where + ": los dientes no encajan");
        }

        // ------------------------------------------------------------------ truco, textos, puntaje

        [Test]
        public void TheTrick_FollowsThePathFromTheMotorToThePiece()
        {
            foreach (var m in Machines(4, 100, 12))
            {
                var path = EngranajesContract.PathTo(m, m.Target, false);
                Assert.AreEqual(0, path[0], "empieza en el motor");
                Assert.AreEqual(m.Target, path[path.Count - 1], "termina en la pieza");
                for (int i = 1; i < path.Count; i++)
                    Assert.IsTrue(m.Links.Exists(l => (l.A == path[i - 1] && l.B == path[i]) || (l.B == path[i - 1] && l.A == path[i])), "cada paso es una unión");
                Assert.AreEqual("Truco: cada engranaje que toca gira al revés", EngranajesContract.Trick(m));
            }
            var speed = EngranajesContract.Generate(7, Rng(1));
            StringAssert.Contains("tamaño", EngranajesContract.Trick(speed));
        }

        [Test]
        public void TheQuestionsAndExplanations_AreShortAndReadable()
        {
            for (int level = 1; level <= 12; level++)
                foreach (var m in Machines(level, 40, 13))
                {
                    Assert.LessOrEqual(EngranajesContract.QuestionText(m).Length, 60, "pregunta del nivel " + level);
                    Assert.LessOrEqual(EngranajesContract.Explain(m).Length, 60, "explicación del nivel " + level);
                    foreach (var b in EngranajesContract.Buttons(m)) Assert.LessOrEqual(b.Label.Length, 16, b.Label);
                }
        }

        [Test]
        public void Score_PrecisionIsOutOfTenMachines_RetoOutOfTheReferencePace()
        {
            Assert.AreEqual(100, EngranajesContract.Score(10, false));
            Assert.AreEqual(70, EngranajesContract.Score(7, false));
            Assert.AreEqual(0, EngranajesContract.Score(0, true));
            Assert.AreEqual(100, EngranajesContract.Score(20, true), "nunca pasa de 100");
            Assert.AreEqual(56, EngranajesContract.Score(5, true));
            Assert.Greater(EngranajesContract.StepSeconds(true), EngranajesContract.StepSeconds(false), "mayores: cascada algo más lenta");
        }

        // ------------------------------------------------------------------ el cohete

        [Test]
        public void TheRocket_GainsALightPerHit_AndLaunchesOnlyWhenItIsComplete()
        {
            var r = new RocketState(0, 0);
            for (int k = 1; k <= 9; k++)
            {
                r = r.AddLight(out bool launched);
                Assert.IsFalse(launched, "con " + k + " luces no despega");
                Assert.AreEqual(k, r.Lights);
                Assert.AreEqual(0, r.Orbit);
            }
            r = r.AddLight(out bool tookOff);
            Assert.IsTrue(tookOff, "con la luz 10 despega");
            Assert.AreEqual(0, r.Lights, "después empieza uno nuevo");
            Assert.AreEqual(1, r.Orbit);
            Assert.AreEqual(2, r.RocketNumber);
        }

        [Test]
        public void TheRocket_NeverKeepsAnIncompleteLaunch_AndClampsWhatTheAppSends()
        {
            var weird = new RocketState(25, -3);
            Assert.AreEqual(9, weird.Lights, "nunca guarda las 10 luces: ya habría despegado");
            Assert.AreEqual(0, weird.Orbit);
            Assert.AreEqual(0, new RocketState(-4, 2).Lights);
            var r = new RocketState(8, 3);
            r = r.AddLight(out bool l1);
            Assert.IsFalse(l1);
            r = r.AddLight(out bool l2);
            Assert.IsTrue(l2);
            Assert.AreEqual(4, r.Orbit);
        }

        [Test]
        public void Tally_CountsMachinesPeakEtapaPaceAndLaunches()
        {
            var t = new EngranajesTally();
            Assert.AreEqual(-1, t.MeanMs);
            t.Record(true, 8000, 1);
            t.Record(false, 12000, 3);
            t.Record(true, 10000, 9);
            t.Launched();
            Assert.AreEqual(3, t.Total);
            Assert.AreEqual(2, t.Correct);
            Assert.AreEqual(9, t.PeakLevel);
            Assert.AreEqual(5, t.PeakEtapa);
            Assert.AreEqual(10000, t.MeanMs);
            Assert.AreEqual(1, t.Launches);
        }

        /// <summary>Sin ramas el camino es simple y pocas formas caben (y en velocidad el motor debe ser de otro tamaño que la pieza); con ramas hay muchas.</summary>
        private static int MinVariety(int level)
        {
            var st = EngranajesContract.StageFor(level);
            if (st.Branches > 0) return 9;
            return st.Q == Question.Speed ? 2 : 5;
        }

        [Test]
        public void TheMachines_AreVaried_NotAlwaysTheSame()
        {
            for (int level = 1; level <= 12; level++)
            {
                var seen = new HashSet<string>();
                foreach (var m in Machines(level, 100, 14))
                {
                    var sb = new System.Text.StringBuilder();
                    foreach (var g in m.Gears) sb.Append(g.Col).Append(',').Append(g.Row).Append(g.Removed ? "x" : "").Append(';');
                    seen.Add(sb.ToString());
                }
                Assert.GreaterOrEqual(seen.Count, MinVariety(level), "nivel " + level + ": pocas máquinas distintas");
            }
        }
    }
}
