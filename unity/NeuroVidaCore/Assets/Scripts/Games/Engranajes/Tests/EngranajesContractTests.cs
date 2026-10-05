using System;
using System.Collections.Generic;
using NUnit.Framework;
using NeuroVida.Games.Engranajes;

namespace NeuroVida.Games.Engranajes.Tests
{
    /// <summary>«Engranajes: Taller de reparación» (docs/diseno-engranajes.md): miles de máquinas por etapa sin contactos no deseados, sin callejones, con la regla de oro, las correas y los
    /// interruptores que dice la etapa, la solución guardada que funciona, el mínimo de cambios que coincide con la etapa, la física, los textos y el cohete que se guarda.</summary>
    public class EngranajesContractTests
    {
        private const int PerLevel = 500;

        private static Random Rng(int seed) => new Random(seed);

        private static IEnumerable<Machine> Machines(int level, int count, int seed)
        {
            var rng = Rng(seed * 31 + level);
            for (int k = 0; k < count; k++) yield return EngranajesContract.Generate(level, rng);
        }

        private static int PopCount(int v) { int n = 0; while (v != 0) { v &= v - 1; n++; } return n; }

        private static List<int> Ids(Machine m, params int[] ids) => new List<int>(ids);

        // ------------------------------------------------------------------ etapas

        [Test]
        public void ThereAreTwelveStages_WithTheIntroductionsInOrder_AndFiveGroups()
        {
            Assert.AreEqual(12, EngranajesContract.Stages.Length);
            Assert.AreEqual(12, EngranajesContract.MaxLevel);
            var intros = new List<Intro>();
            foreach (var st in EngranajesContract.Stages) if (st.Intro != Intro.None) intros.Add(st.Intro);
            CollectionAssert.AreEqual(new[] { Intro.Taller, Intro.Correas, Intro.Ramas, Intro.Barras, Intro.Bien, Intro.Dos }, intros);
            foreach (var i in intros) Assert.That(EngranajesContract.IntroText(i).Length, Is.InRange(2, 3), i + ": dos o tres líneas");
            int[] etapas = { 1, 1, 2, 2, 3, 3, 4, 4, 5, 5, 5, 5 };
            for (int l = 1; l <= 12; l++) Assert.AreEqual(etapas[l - 1], EngranajesContract.Etapa(l), "grupo de etapa del nivel " + l);
            int[] pieces = { 1, 1, 2, 2, 2, 2, 3, 3, 2, 3, 3, 3 };
            int[] belts = { 0, 1, 2, 2, 2, 2, 3, 3, 2, 3, 3, 4 };
            int[] keys = { 1, 1, 1, 1, 1, 1, 1, 1, 2, 2, 2, 2 };
            for (int l = 1; l <= 12; l++)
            {
                var st = EngranajesContract.StageFor(l);
                Assert.AreEqual(pieces[l - 1], st.Pieces, "piezas del nivel " + l);
                Assert.AreEqual(belts[l - 1], st.Belts, "correas del nivel " + l);
                Assert.AreEqual(keys[l - 1], st.Keys, "llaves del nivel " + l);
            }
            Assert.AreEqual(Fail.All, EngranajesContract.StageFor(1).Fail);
            Assert.AreEqual(Fail.One, EngranajesContract.StageFor(3).Fail);
            Assert.AreEqual(Fail.Two, EngranajesContract.StageFor(9).Fail);
            Assert.AreEqual(Fail.Two, EngranajesContract.StageFor(12).Fail);
            Assert.AreEqual(0.25f, EngranajesContract.StageFor(6).Ok);
            Assert.AreEqual(0.20f, EngranajesContract.StageFor(8).Ok);
            Assert.AreEqual(0.15f, EngranajesContract.StageFor(11).Ok);
            Assert.IsNotNull(EngranajesContract.StageFor(0), "los niveles se acotan");
            Assert.AreEqual(10, EngranajesContract.PrecisionMachines);
            Assert.AreEqual(120, EngranajesContract.RetoSeconds);
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

        // ------------------------------------------------------------------ la máquina: árbol limpio

        [Test]
        public void EveryMachine_AtEveryLevel_HasNoUnwantedContacts_AndEveryGearIsReachable()
        {
            for (int level = 1; level <= 12; level++)
                foreach (var m in Machines(level, PerLevel, 1))
                {
                    var bad = EngranajesContract.UnwantedContacts(m);
                    Assert.IsEmpty(bad, "nivel " + level + ": " + string.Join(", ", bad));
                    var sol = EngranajesContract.Solve(m, null);
                    for (int i = 0; i < m.Gears.Count; i++) Assert.GreaterOrEqual(sol.Depth[i], 0, "nivel " + level + ": el engranaje " + i + " no recibe la fuerza del motor");
                }
        }

        [Test]
        public void NoDeadEnds_EveryLeafIsAPiece_AndThePiecesAreExactlyTheStageOnes()
        {
            for (int level = 1; level <= 12; level++)
                foreach (var m in Machines(level, PerLevel, 2))
                {
                    var degree = new int[m.Gears.Count];
                    foreach (var l in m.Links) { degree[l.A]++; degree[l.B]++; }
                    for (int i = 1; i < m.Gears.Count; i++)
                        if (degree[i] == 1) Assert.IsTrue(m.Gears[i].HasStation, "nivel " + level + ": el engranaje " + i + " no lleva a ninguna pieza (callejón)");
                    Assert.AreEqual(GearRole.Motor, m.Gears[0].Role);
                    Assert.AreEqual(m.Stage.Pieces, m.Targets.Length, "nivel " + level);
                    Assert.AreEqual(m.Targets.Length, m.Stations.Count);
                    foreach (var t in m.Targets)
                    {
                        Assert.IsTrue(m.Stations.ContainsKey(t), "nivel " + level + ": " + t);
                        var g = m.Gears[m.Stations[t]];
                        Assert.AreEqual(GearRole.Station, g.Role);
                        Assert.AreEqual(4, g.Col, "la pieza va en la columna del cohete");
                        Assert.AreEqual(EngranajesContract.StationRow(t), g.Row);
                    }
                    Assert.IsFalse(m.Stations.ContainsKey(Station.Compuerta) && m.Stations.ContainsKey(Station.Carga), "compuerta y carga nunca van juntas (sus filas son vecinas)");
                    if (m.Stage.Pool == Pool.Turn)
                    {
                        if (m.Stage.Pieces == 1) CollectionAssert.IsSubsetOf(m.Targets, new[] { Station.Antena, Station.Turbina });
                        else CollectionAssert.AreEquivalent(new[] { Station.Antena, Station.Turbina }, m.Targets);
                    }
                    if (m.Stage.Pool == Pool.Rack) Assert.IsTrue(m.Stations.ContainsKey(Station.Compuerta) || m.Stations.ContainsKey(Station.Carga), "nivel " + level + ": una pieza con barra");
                }
        }

        [Test]
        public void GoldenRule_RemovingAnyGear_ChangesTheResultOfSomePieceOrDisconnectsIt()
        {
            for (int level = 1; level <= 12; level++)
                foreach (var m in Machines(level, 120, 3))
                {
                    var baseline = EngranajesContract.Results(m, null);
                    for (int gone = 0; gone < m.Gears.Count; gone++)
                    {
                        // la máquina sin ese engranaje: se vuelve a seguir la fuerza desde el motor (si falta el motor, nada se mueve)
                        bool changed = false;
                        if (gone == 0) changed = true;
                        else
                        {
                            var reach = new HashSet<int> { 0 };
                            var queue = new Queue<int>(new[] { 0 });
                            while (queue.Count > 0)
                            {
                                int i = queue.Dequeue();
                                foreach (var l in m.Links)
                                {
                                    int j = l.A == i ? l.B : l.B == i ? l.A : -1;
                                    if (j < 0 || j == gone || reach.Contains(j)) continue;
                                    reach.Add(j);
                                    queue.Enqueue(j);
                                }
                            }
                            foreach (var t in m.Targets) if (!reach.Contains(m.Stations[t])) changed = true;
                        }
                        Assert.IsTrue(changed, "nivel " + level + ": quitar el engranaje " + gone + " no cambia nada (decoración)");
                    }
                    Assert.AreEqual(m.Targets.Length, baseline.Length);
                }
        }

        // ------------------------------------------------------------------ correas e interruptores

        [Test]
        public void EveryStage_HasExactlyTheBeltsAndSwitchesItAsksFor_EachBeltMovingADifferentGroup()
        {
            for (int level = 1; level <= 12; level++)
                foreach (var m in Machines(level, PerLevel, 4))
                {
                    int belts = 0;
                    var masks = new HashSet<int>();
                    foreach (var l in m.Links)
                    {
                        if (l.Type != LinkType.Belt) continue;
                        belts++;
                        var a = m.Gears[l.A];
                        var b = m.Gears[l.B];
                        double d = Math.Sqrt((a.X - b.X) * (a.X - b.X) + (a.Y - b.Y) * (a.Y - b.Y));
                        Assert.AreEqual(2 * EngranajesContract.GridStep, d, 1e-3, "una correa une dos engranajes a dos casillas, en línea recta");
                        Assert.IsTrue(a.Col == b.Col || a.Row == b.Row);
                    }
                    Assert.AreEqual(m.Stage.Belts, belts, "nivel " + level + ": correas");
                    Assert.AreEqual(1 + belts, m.Switches.Count, "el motor y cada correa");
                    Assert.AreEqual(EngranajesContract.MotorId, m.Switches[0].Id);
                    Assert.AreEqual(m.FullMask, m.Switches[0].Mask, "el motor mueve todas las piezas");
                    for (int i = 1; i < m.Switches.Count; i++)
                    {
                        Assert.AreEqual(LinkType.Belt, m.Links[m.Switches[i].Id].Type);
                        Assert.AreNotEqual(0, m.Switches[i].Mask, "una correa siempre mueve alguna pieza");
                        masks.Add(m.Switches[i].Mask);
                    }
                    if (m.Stage.Pieces > 1) Assert.AreEqual(belts, masks.Count, "nivel " + level + ": cada correa mueve un grupo distinto de piezas");
                }
        }

        [Test]
        public void TheSwitchMasks_AreWhatThePhysicsReallyChanges()
        {
            for (int level = 1; level <= 12; level++)
                foreach (var m in Machines(level, 150, 5))
                {
                    var none = EngranajesContract.Results(m, null);
                    foreach (var sw in m.Switches)
                    {
                        var res = EngranajesContract.Results(m, new List<int> { sw.Id });
                        int flipped = 0;
                        for (int k = 0; k < m.Targets.Length; k++) if (res[k].Got != none[k].Got) flipped |= 1 << k;
                        Assert.AreEqual(sw.Mask, flipped, "nivel " + level + ": el interruptor " + sw.Id + " mueve otras piezas de las que dice");
                    }
                }
        }

        // ------------------------------------------------------------------ el problema: qué falla, solución y mínimo

        [Test]
        public void TheStoredSolution_Works_AndUsesTheMinimumNumberOfChanges()
        {
            for (int level = 1; level <= 12; level++)
                foreach (var m in Machines(level, PerLevel, 6))
                {
                    Assert.AreEqual(m.MinK, m.Solution.Length, "nivel " + level + ": la solución guardada usa el mínimo");
                    Assert.LessOrEqual(m.Solution.Length, m.Stage.Keys, "nivel " + level + ": cabe en las llaves");
                    Assert.IsTrue(EngranajesContract.AllOk(EngranajesContract.Results(m, m.Solution)), "nivel " + level + ": la solución guardada no arregla la máquina");
                }
        }

        [Test]
        public void DoingNothing_Fails_ExceptWhenTheMachineWasAlreadyRight()
        {
            for (int level = 1; level <= 12; level++)
                foreach (var m in Machines(level, PerLevel, 7))
                {
                    bool ok = EngranajesContract.AllOk(EngranajesContract.Results(m, null));
                    Assert.AreEqual(m.W == 0, ok, "nivel " + level + ": «sin cambios» solo sirve si no falla nada");
                    if (m.W == 0) Assert.AreEqual(0, m.Solution.Length);
                }
        }

        [Test]
        public void TheResultIsJudged_NotThePath_AnyCombinationThatLeavesEveryPieceRightCounts()
        {
            for (int level = 1; level <= 12; level++)
                foreach (var m in Machines(level, 80, 8))
                {
                    var all = new List<List<int>> { new List<int>() };
                    foreach (var s in m.Switches) all.Add(new List<int> { s.Id });
                    if (m.Stage.Keys == 2)
                        for (int a = 0; a < m.Switches.Count; a++)
                            for (int b = a + 1; b < m.Switches.Count; b++) all.Add(new List<int> { m.Switches[a].Id, m.Switches[b].Id });
                    foreach (var changes in all)
                    {
                        int xor = 0;
                        foreach (int id in changes) foreach (var s in m.Switches) if (s.Id == id) xor ^= s.Mask;
                        bool right = EngranajesContract.AllOk(EngranajesContract.Results(m, changes));
                        Assert.AreEqual(xor == m.W, right, "nivel " + level + ": el resultado debe depender solo de qué piezas cambian de sentido");
                    }
                }
        }

        [Test]
        public void TheMinimumChangesMatchTheStage_AndTheFailingGroupIsTheOneItAsksFor()
        {
            for (int level = 1; level <= 12; level++)
            {
                var st = EngranajesContract.StageFor(level);
                foreach (var m in Machines(level, PerLevel, 9))
                {
                    Assert.LessOrEqual(m.MinK, st.Keys, "nivel " + level);
                    if (m.W == 0) continue;
                    switch (st.Fail)
                    {
                        case Fail.All:
                            Assert.AreEqual(m.FullMask, m.W, "nivel " + level + ": fallan todas");
                            Assert.AreEqual(1, m.MinK);
                            break;
                        case Fail.One:
                            Assert.AreEqual(1, PopCount(m.W), "nivel " + level + ": falla una sola pieza");
                            Assert.AreEqual(1, m.MinK);
                            break;
                        case Fail.Two:
                            Assert.AreEqual(2, m.MinK, "nivel " + level + ": hacen falta justo dos cambios");
                            break;
                    }
                }
            }
        }

        [Test]
        public void WithOneKey_TheSinglePieceStages_AreNeverFixedByTheMotor()
        {
            foreach (int level in new[] { 3 })
                foreach (var m in Machines(level, 1000, 10))
                {
                    Assert.AreEqual(1, PopCount(m.W));
                    Assert.IsFalse(EngranajesContract.AllOk(EngranajesContract.Results(m, Ids(m, EngranajesContract.MotorId))), "el motor mueve todas las piezas: no arregla una sola");
                    CollectionAssert.DoesNotContain(m.Solution, EngranajesContract.MotorId);
                }
        }

        [Test]
        public void EveryStage_KeepsItsAlreadyRightPercentage_AndTheAllFailGroupIsRare()
        {
            for (int level = 1; level <= 12; level++)
            {
                var st = EngranajesContract.StageFor(level);
                int n = 1500, already = 0, all = 0;
                foreach (var m in Machines(level, n, 11))
                {
                    if (m.W == 0) already++;
                    else if (m.W == m.FullMask) all++;
                }
                double frac = already / (double)n;
                if (st.Ok <= 0f) Assert.AreEqual(0, already, "nivel " + level + ": no hay máquinas «ya bien»");
                else Assert.AreEqual(st.Ok, frac, 0.04, "nivel " + level + ": «ya está bien» " + frac);
                if (st.Fail == Fail.Any) Assert.LessOrEqual(all / (double)(n - already), 0.30, "nivel " + level + ": «todas fallan» sale a lo más ~20 %");
            }
        }

        [Test]
        public void ForcingThePiece_GivesTheAntennaInStageOne_ForTheTutorial()
        {
            var rng = Rng(12);
            for (int i = 0; i < 60; i++)
            {
                var m = EngranajesContract.Generate(1, rng, Station.Antena);
                CollectionAssert.AreEqual(new[] { Station.Antena }, m.Targets);
                Assert.AreEqual(m.FullMask, m.W, "la antena viene fallando y se arregla con el motor");
                Assert.AreEqual(EngranajesContract.MotorId, m.Solution[0]);
            }
        }

        [Test]
        public void TheMachines_AreVaried_NotAlwaysTheSame()
        {
            for (int level = 1; level <= 12; level++)
            {
                var seen = new HashSet<string>();
                foreach (var m in Machines(level, 200, 13))
                {
                    var sb = new System.Text.StringBuilder();
                    foreach (var g in m.Gears) sb.Append(g.Col).Append(',').Append(g.Row).Append(';');
                    foreach (var l in m.Links) if (l.Type == LinkType.Belt) sb.Append('b').Append(l.A).Append('-').Append(l.B).Append(l.Crossed ? "x" : "=");
                    sb.Append(m.W);
                    seen.Add(sb.ToString());
                }
                Assert.GreaterOrEqual(seen.Count, level == 1 ? 8 : 15, "nivel " + level + ": pocas máquinas distintas");
            }
        }

        // ------------------------------------------------------------------ física

        [Test]
        public void Solve_TouchingGearsTurnOppositeWays_AndTheSpeedFollowsTheSize()
        {
            foreach (var m in Machines(2, 100, 14))
            {
                var s = EngranajesContract.Solve(m, null);
                foreach (var l in m.Links)
                {
                    if (l.Type == LinkType.Mesh) Assert.AreEqual(-s.Dir[l.A], s.Dir[l.B], "dos engranajes que se tocan giran al revés");
                    float ra = Math.Abs(s.Speed[l.A]) * m.Gears[l.A].Pitch, rb = Math.Abs(s.Speed[l.B]) * m.Gears[l.B].Pitch;
                    Assert.AreEqual(ra, rb, 1e-3f, "la velocidad en el punto de contacto es la misma");
                }
                Assert.AreEqual(m.MotorDir, s.Dir[0]);
            }
        }

        [Test]
        public void Solve_AStraightBeltKeepsTheDirection_ACrossedBeltReversesIt_AndTouchingItFlipsIt()
        {
            int checkedBelts = 0;
            foreach (var m in Machines(4, 200, 15))
                for (int li = 0; li < m.Links.Count; li++)
                {
                    var l = m.Links[li];
                    if (l.Type != LinkType.Belt) continue;
                    var s = EngranajesContract.Solve(m, null);
                    Assert.AreEqual(l.Crossed ? -s.Dir[l.A] : s.Dir[l.A], s.Dir[l.B], "recta: mismo giro; cruzada: contrario");
                    var s2 = EngranajesContract.Solve(m, new List<int> { li });
                    Assert.AreEqual(l.Crossed ? s2.Dir[l.A] : -s2.Dir[l.A], s2.Dir[l.B], "tocarla la invierte");
                    checkedBelts++;
                }
            Assert.Greater(checkedBelts, 100);
        }

        [Test]
        public void Solve_TouchingTheMotorReversesEverything()
        {
            foreach (var m in Machines(7, 100, 16))
            {
                var a = EngranajesContract.Solve(m, null);
                var b = EngranajesContract.Solve(m, Ids(m, EngranajesContract.MotorId));
                for (int i = 0; i < m.Gears.Count; i++) Assert.AreEqual(-a.Dir[i], b.Dir[i]);
            }
        }

        [Test]
        public void Behave_BarToTheRight_ClockwiseLowersTheLoadAndClosesTheGate()
        {
            Assert.AreEqual(Act.Down, EngranajesContract.Behave(Station.Carga, 1));
            Assert.AreEqual(Act.Up, EngranajesContract.Behave(Station.Carga, -1));
            Assert.AreEqual(Act.Close, EngranajesContract.Behave(Station.Compuerta, 1));
            Assert.AreEqual(Act.Open, EngranajesContract.Behave(Station.Compuerta, -1));
            Assert.AreEqual(Act.Cw, EngranajesContract.Behave(Station.Antena, 1));
            Assert.AreEqual(Act.Ccw, EngranajesContract.Behave(Station.Turbina, -1));
            CollectionAssert.AreEqual(new[] { "reloj", "al revés", "sube", "baja", "se abre", "se cierra" },
                new[] { EngranajesContract.MissionWord(Act.Cw), EngranajesContract.MissionWord(Act.Ccw), EngranajesContract.MissionWord(Act.Up), EngranajesContract.MissionWord(Act.Down), EngranajesContract.MissionWord(Act.Open), EngranajesContract.MissionWord(Act.Close) });
        }

        [Test]
        public void TheCartels_AreBuiltFromWhatEachPieceDoesToday_WithTheFailingOnesInverted()
        {
            foreach (var m in Machines(8, 300, 17))
            {
                var none = EngranajesContract.Results(m, null);
                for (int k = 0; k < m.Targets.Length; k++)
                {
                    bool inW = ((m.W >> k) & 1) == 1;
                    Assert.AreEqual(!inW, none[k].Ok, "una pieza de W incumple su cartel y las demás cumplen");
                }
            }
        }

        // ------------------------------------------------------------------ dientes que encajan

        [Test]
        public void TheTeethMesh_EveryToothFacesTheGapOfItsNeighbour()
        {
            for (int level = 1; level <= 12; level++)
                foreach (var m in Machines(level, 100, 18))
                    foreach (var l in m.Links)
                        if (l.Type == LinkType.Mesh) AssertMeshed(m.Gears[l.A], m.Gears[l.B], "nivel " + level);
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

        // ------------------------------------------------------------------ qué se marca al equivocarse

        [Test]
        public void Downstream_OfTheMotorIsEverything_AndOfABeltOnlyWhatFollowsIt()
        {
            foreach (var m in Machines(7, 100, 19))
            {
                Assert.AreEqual(m.Gears.Count, EngranajesContract.Downstream(m, EngranajesContract.MotorId).Count, "el motor mueve todo");
                foreach (var sw in m.Switches)
                {
                    if (sw.Id == EngranajesContract.MotorId) continue;
                    var set = EngranajesContract.Downstream(m, sw.Id);
                    Assert.Less(set.Count, m.Gears.Count, "una correa no mueve todo");
                    int mask = 0;
                    foreach (int i in set) if (m.Gears[i].HasStation) mask |= 1 << Array.IndexOf(m.Targets, m.Gears[i].Station);
                    Assert.AreEqual(sw.Mask, mask, "las piezas que están aguas abajo son las de su máscara");
                }
            }
        }

        // ------------------------------------------------------------------ textos

        [Test]
        public void TheFailureAndHintTexts_AreTheOnesOfTheDoc()
        {
            var rng = Rng(20);
            bool sawAlso = false, sawNone = false, sawTrick = false;
            for (int level = 1; level <= 12; level++)
                for (int k = 0; k < 200; k++)
                {
                    var m = EngranajesContract.Generate(level, rng);
                    // cambios al azar: lo suficiente para ver las tres pistas
                    var changes = new List<int>();
                    int n = rng.Next(0, m.Stage.Keys + 1);
                    for (int j = 0; j < n; j++) { int id = m.Switches[rng.Next(m.Switches.Count)].Id; if (!changes.Contains(id)) changes.Add(id); }
                    var res = EngranajesContract.Results(m, changes);
                    if (EngranajesContract.AllOk(res))
                    {
                        Assert.AreEqual("", EngranajesContract.FailText(res));
                        continue;
                    }
                    string fail = EngranajesContract.FailText(res), hint = EngranajesContract.HintText(m, res, changes);
                    Assert.That(fail, Does.StartWith("La ").Or.StartWith("Fallaron "), fail);
                    Assert.LessOrEqual(fail.Length, 48, fail);
                    Assert.LessOrEqual(hint.Length, 46, hint);
                    if (hint.StartsWith("Tu cambio también movió ")) sawAlso = true;
                    else if (hint == "Brilla en dorado lo que había que cambiar") { sawNone = true; Assert.AreEqual(0, changes.Count); }
                    else { sawTrick = true; Assert.AreEqual("Truco: cada engranaje que toca gira al revés", hint); }
                }
            Assert.IsTrue(sawAlso && sawNone && sawTrick, "salieron las tres pistas");
            var one = new[] { new PartResult(Station.Turbina, Act.Ccw, false) };
            Assert.AreEqual("La turbina giró al revés de su cartel", EngranajesContract.FailText(one));
            Assert.AreEqual("La carga subió en vez de bajar", EngranajesContract.FailText(new[] { new PartResult(Station.Carga, Act.Up, false) }));
            Assert.AreEqual("La carga bajó en vez de subir", EngranajesContract.FailText(new[] { new PartResult(Station.Carga, Act.Down, false) }));
            Assert.AreEqual("La compuerta se cerró en vez de abrirse", EngranajesContract.FailText(new[] { new PartResult(Station.Compuerta, Act.Close, false) }));
            Assert.AreEqual("La compuerta se abrió en vez de cerrarse", EngranajesContract.FailText(new[] { new PartResult(Station.Compuerta, Act.Open, false) }));
            Assert.AreEqual("Fallaron la antena y la turbina", EngranajesContract.FailText(new[] { new PartResult(Station.Antena, Act.Cw, false), new PartResult(Station.Turbina, Act.Ccw, false) }));
            Assert.AreEqual("Fallaron la antena, la carga y la turbina", EngranajesContract.FailText(new[] { new PartResult(Station.Antena, Act.Cw, false), new PartResult(Station.Carga, Act.Up, false), new PartResult(Station.Turbina, Act.Ccw, false) }));
        }

        [Test]
        public void TheSuccessText_RotatesAmongThree_AndHasItsOwnForAMachineThatWasAlreadyRight()
        {
            var rng = Rng(21);
            Machine ready = null;
            while (ready == null) { var c = EngranajesContract.Generate(6, rng); if (c.W == 0) ready = c; }
            Assert.AreEqual("¡Bien visto! Ya estaba lista", EngranajesContract.SuccessText(ready, new List<int>(), 0));
            var broken = EngranajesContract.Generate(1, rng);
            var seen = new HashSet<string>();
            for (int i = 0; i < 6; i++) seen.Add(EngranajesContract.SuccessText(broken, broken.Solution, i));
            CollectionAssert.AreEquivalent(new[] { "¡Cohete listo!", "¡Arreglado!", "¡Todo en orden!" }, seen);
        }

        [Test]
        public void TheConsigna_HasTwoLines_AndTheSecondDependsOnTheStage()
        {
            CollectionAssert.AreEqual(new[] { "Que cada pieza cumpla su cartel", "Toca el motor para cambiar su giro" }, EngranajesContract.Consigna(EngranajesContract.StageFor(1)));
            Assert.AreEqual("Un cambio: el motor o una correa", EngranajesContract.Consigna(EngranajesContract.StageFor(2))[1]);
            Assert.AreEqual("Un cambio, o ninguno si ya está bien", EngranajesContract.Consigna(EngranajesContract.StageFor(6))[1]);
            Assert.AreEqual("Hasta dos cambios", EngranajesContract.Consigna(EngranajesContract.StageFor(9))[1]);
            for (int l = 1; l <= 12; l++) foreach (var line in EngranajesContract.Consigna(EngranajesContract.StageFor(l))) Assert.LessOrEqual(line.Length, 36, line);
        }

        // ------------------------------------------------------------------ puntaje y cohete

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
    }
}
