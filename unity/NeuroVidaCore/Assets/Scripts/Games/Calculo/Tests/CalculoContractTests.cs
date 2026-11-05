using System.Collections.Generic;
using NUnit.Framework;
using NeuroVida.Contracts;
using NeuroVida.Games.Calculo;

namespace NeuroVida.Games.Calculo.Tests
{
    /// <summary>«Carga exacta» (docs/diseno-carga-exacta.md): el generador siempre da cargas con solución, el solucionador la alcanza, las reglas de cada jugada y las medidas.</summary>
    public class CalculoContractTests
    {
        private const int PerLevel = 300;

        private static System.Random Rng(int seed) => new System.Random(seed);

        // ------------------------------------------------------------------ generador

        [Test]
        public void EveryLoad_AtEveryLevel_HasASolution_AndTheSolversPathReachesIt()
        {
            for (int level = 1; level <= CalculoContract.MaxLevel; level++)
            {
                var rng = Rng(level * 101);
                for (int k = 0; k < PerLevel; k++)
                {
                    var p = CargaGenerator.Generate(level, rng);
                    Assert.IsFalse(p.Fallback, "nivel " + level + ": el sorteo no debe caer en la carga de respaldo");
                    Assert.IsNotNull(p.Solution, "nivel " + level);
                    // el camino lleva de verdad a la carga, jugándolo en un tablero normal
                    var board = new CargaBoard(p.Cells);
                    foreach (var step in p.Solution)
                    {
                        int ia = board.IndexOfValue(step.A);
                        int ib = board.IndexOfValue(step.B, ia);
                        Assert.GreaterOrEqual(ia, 0, p.Key + ": falta la celda " + step.A);
                        Assert.GreaterOrEqual(ib, 0, p.Key + ": falta la celda " + step.B);
                        Assert.IsTrue(board.TryMerge(ia, step.Op, ib, out var move, out _), p.Key + ": " + step + " no vale");
                        Assert.AreEqual(step.R, move.Step.R);
                    }
                    Assert.GreaterOrEqual(board.Winning(p.Target, p.RequireAll), 0, p.Key + ": el camino no llegó a la carga");
                }
            }
        }

        [Test]
        public void EveryLoad_FollowsTheGenerationRules()
        {
            for (int level = 1; level <= CalculoContract.MaxLevel; level++)
            {
                var spec = CalculoContract.LevelSpec(level);
                var rng = Rng(level * 17);
                for (int k = 0; k < PerLevel; k++)
                {
                    var p = CargaGenerator.Generate(level, rng);
                    string where = "nivel " + level + " " + p.Key;
                    Assert.GreaterOrEqual(p.Target, CalculoContract.MinTarget, where);
                    Assert.LessOrEqual(p.Target, CalculoContract.MaxTarget, where);
                    Assert.IsFalse(System.Array.IndexOf(p.Cells, p.Target) >= 0, where + ": la carga ya está entre las celdas");
                    Assert.GreaterOrEqual(p.Cells.Length, spec.CellsMin, where);
                    Assert.LessOrEqual(p.Cells.Length, spec.CellsMax, where);
                    foreach (int c in p.Cells)
                    {
                        Assert.GreaterOrEqual(c, 1, where);
                        Assert.LessOrEqual(c, spec.MaxNumber, where);
                    }
                    var distinct = new HashSet<int>(p.Cells);
                    Assert.GreaterOrEqual(distinct.Count, p.Cells.Length - (p.Cells.Length >= 5 ? 1 : 0), where + ": celdas repetidas");
                    Assert.GreaterOrEqual(p.ShortestSteps, spec.MinShortest, where);
                    foreach (var s in p.Solution)
                    {
                        Assert.Contains(s.Op, spec.Ops, where + ": operación fuera del nivel");
                        Assert.GreaterOrEqual(s.R, 0, where + ": resultado negativo");
                        if (s.Op == CargaOp.Div) Assert.AreEqual(0, s.A % s.B, where + ": división inexacta");
                    }
                    if (p.RequireAll) Assert.AreEqual(p.Cells.Length - 1, p.ShortestSteps, where + ": «usa todas» junta todas las celdas");
                }
            }
        }

        [Test]
        public void Level1_IsOnlySums_Level2_AddsSubtraction_Level3_AddsProducts_Level4_AddsDivision()
        {
            var ops = new Dictionary<int, HashSet<CargaOp>>();
            for (int level = 1; level <= 5; level++)
            {
                ops[level] = new HashSet<CargaOp>();
                var rng = Rng(level * 7);
                for (int k = 0; k < PerLevel; k++)
                    foreach (var s in CargaGenerator.Generate(level, rng).Solution) ops[level].Add(s.Op);
            }
            CollectionAssert.AreEquivalent(new[] { CargaOp.Add }, ops[1]);
            CollectionAssert.IsSubsetOf(ops[2], new[] { CargaOp.Add, CargaOp.Sub });
            CollectionAssert.IsSubsetOf(ops[3], new[] { CargaOp.Add, CargaOp.Sub, CargaOp.Mul });
            Assert.IsTrue(ops[3].Contains(CargaOp.Mul), "el nivel 3 usa multiplicaciones");
            Assert.IsTrue(ops[4].Contains(CargaOp.Div), "el nivel 4 usa divisiones");
        }

        [Test]
        public void Level5_SometimesAsksToUseAllTheCells_AndHasFourOrFiveCells()
        {
            var rng = Rng(55);
            int all = 0, sizes4 = 0, sizes5 = 0;
            for (int k = 0; k < PerLevel; k++)
            {
                var p = CargaGenerator.Generate(5, rng);
                if (p.RequireAll) all++;
                if (p.Cells.Length == 4) sizes4++; else sizes5++;
            }
            Assert.Greater(all, 0, "algunas piden usar todas");
            Assert.Less(all, PerLevel, "pero no todas");
            Assert.Greater(sizes5, 0);
            Assert.Greater(sizes4, 0);
        }

        [Test]
        public void NoRepeatedLoads_InAGame_AndNoTwoInARowWithTheSameNumber()
        {
            for (int level = 1; level <= CalculoContract.MaxLevel; level++)
            {
                var director = new CargaDirector(Rng(level));
                var seen = new HashSet<string>();
                int last = -1;
                for (int k = 0; k < 60; k++)
                {
                    var p = director.Next(level);
                    Assert.IsTrue(seen.Add(p.Key), "nivel " + level + ": carga repetida " + p.Key);
                    Assert.AreNotEqual(last, p.Target, "nivel " + level + ": dos cargas seguidas iguales");
                    last = p.Target;
                }
            }
        }

        [Test]
        public void TheFallbackLoads_AreValid()
        {
            foreach (var spec in CalculoContract.Levels)
            {
                var p = CargaGenerator.FallbackFor(spec);
                Assert.IsNotNull(p.Solution, "nivel " + spec.Level);
                Assert.GreaterOrEqual(p.ShortestSteps, spec.MinShortest, "nivel " + spec.Level);
                Assert.GreaterOrEqual(p.Target, CalculoContract.MinTarget);
                Assert.IsTrue(System.Array.IndexOf(p.Cells, p.Target) < 0);
            }
        }

        // ------------------------------------------------------------------ la escalera de 8 niveles (Tarea 233, docs/alargar-dificultad.md §B.2)

        [Test]
        public void TheLadderGrewFromFiveToEight_AndTheFirstFiveLevelsDidNotChange()
        {
            Assert.AreEqual(8, CalculoContract.MaxLevel);
            Assert.AreEqual(5, CalculoContract.V1MaxLevel, "el largo de antes: ancla los puntos de la app (Skill.Ladder.pointsLevels) y reparte el nivel elegido de quien no tiene rating");
            Assert.AreEqual(8, CalculoContract.Levels.Length);
            // los cinco de la v1, tal cual: { celdas mín., máx., se juntan mín., máx., número máx., «usa todas» en %, camino más corto mín. }
            int[][] v1 =
            {
                new[] { 3, 3, 2, 2, 9, 0, 1 }, new[] { 3, 4, 2, 3, 12, 0, 1 }, new[] { 4, 4, 2, 3, 9, 0, 1 }, new[] { 4, 4, 3, 3, 10, 0, 2 }, new[] { 4, 5, 4, 4, 10, 35, 2 }
            };
            for (int i = 0; i < 5; i++)
            {
                var lv = CalculoContract.Levels[i];
                Assert.AreEqual(i + 1, lv.Level);
                CollectionAssert.AreEqual(v1[i], new[] { lv.CellsMin, lv.CellsMax, lv.UseMin, lv.UseMax, lv.MaxNumber, (int)System.Math.Round(lv.AllChance * 100f), lv.MinShortest }, "el nivel " + (i + 1) + " no debe cambiar");
            }
            Assert.AreEqual(1, CalculoContract.Levels[0].Ops.Length);
            Assert.AreEqual(2, CalculoContract.Levels[1].Ops.Length);
            Assert.AreEqual(3, CalculoContract.Levels[2].Ops.Length);
            Assert.AreEqual(4, CalculoContract.Levels[3].Ops.Length);
            Assert.AreEqual(4, CalculoContract.Levels[4].Ops.Length);
        }

        [Test]
        public void Levels6To8_AskForMoreWithTheSameFiveCells_AndEachOneAsksForAtLeastWhatThePreviousOneDid()
        {
            var l6 = CalculoContract.LevelSpec(6); var l7 = CalculoContract.LevelSpec(7); var l8 = CalculoContract.LevelSpec(8);
            foreach (var lv in new[] { l6, l7, l8 })
            {
                Assert.AreEqual(5, lv.CellsMin, "nivel " + lv.Level + ": siempre cinco celdas");
                Assert.AreEqual(5, lv.CellsMax, "nivel " + lv.Level);
                Assert.AreEqual(4, lv.Ops.Length, "nivel " + lv.Level + ": las cuatro operaciones");
            }
            CollectionAssert.AreEqual(new[] { 4, 5, 5 }, new[] { l6.UseMin, l7.UseMin, l8.UseMin }, "se juntan 4, 5 y 5");
            CollectionAssert.AreEqual(new[] { 4, 5, 5 }, new[] { l6.UseMax, l7.UseMax, l8.UseMax });
            CollectionAssert.AreEqual(new[] { 12, 12, 14 }, new[] { l6.MaxNumber, l7.MaxNumber, l8.MaxNumber });
            CollectionAssert.AreEqual(new[] { 60, 85, 100 }, new[] { (int)System.Math.Round(l6.AllChance * 100f), (int)System.Math.Round(l7.AllChance * 100f), (int)System.Math.Round(l8.AllChance * 100f) });
            CollectionAssert.AreEqual(new[] { 3, 3, 4 }, new[] { l6.MinShortest, l7.MinShortest, l8.MinShortest });
            // de la 5 en adelante cada nivel pide al menos lo del anterior (los cinco de antes no son monótonos: el 3 tiene números más chicos que el 2 y no se tocan)
            for (int level = 6; level <= CalculoContract.MaxLevel; level++)
            {
                var prev = CalculoContract.LevelSpec(level - 1); var cur = CalculoContract.LevelSpec(level);
                Assert.GreaterOrEqual(cur.MaxNumber, prev.MaxNumber, "nivel " + level);
                Assert.GreaterOrEqual(cur.AllChance, prev.AllChance, "nivel " + level);
                Assert.GreaterOrEqual(cur.MinShortest, prev.MinShortest, "nivel " + level);
                Assert.GreaterOrEqual(cur.CellsMax, prev.CellsMax, "nivel " + level);
            }
        }

        [Test]
        public void Levels6To8_NeverFallBackToTheHandMadeLoad_AcrossManySeeds()
        {
            // La carga de respaldo es solo por si el sorteo no diera; en los niveles nuevos (camino más corto de 3 y de 4 pasos) hay que comprobar que de verdad no cae nunca: 4 semillas de 500 cargas por nivel
            for (int level = 6; level <= 8; level++)
                for (int seed = 1; seed <= 4; seed++)
                {
                    var rng = Rng(seed * 1000 + level);
                    for (int k = 0; k < 500; k++)
                    {
                        var p = CargaGenerator.Generate(level, rng);
                        Assert.IsFalse(p.Fallback, "nivel " + level + ", semilla " + seed + ": el sorteo cayó en la carga de respaldo " + p.Key);
                        Assert.GreaterOrEqual(p.ShortestSteps, CalculoContract.LevelSpec(level).MinShortest, "nivel " + level + " " + p.Key);
                    }
                }
        }

        [Test]
        public void Level6_AlwaysHasFiveCells_AndLevel8_AlwaysAsksToUseAllOfThem_WithFourSteps()
        {
            var rng6 = Rng(66); int all6 = 0;
            for (int k = 0; k < PerLevel; k++)
            {
                var p = CargaGenerator.Generate(6, rng6);
                Assert.AreEqual(5, p.Cells.Length, "nivel 6: siempre cinco celdas, también cuando pide usar todas");
                if (p.RequireAll) { all6++; Assert.AreEqual(4, p.ShortestSteps, "usar todas = juntar las 5"); }
            }
            Assert.Greater(all6, 0, "el nivel 6 a veces pide usar todas");
            Assert.Less(all6, PerLevel, "y a veces no");
            var rng7 = Rng(77); int all7 = 0;
            for (int k = 0; k < PerLevel; k++)
            {
                var p = CargaGenerator.Generate(7, rng7);
                Assert.AreEqual(5, p.Cells.Length);
                if (p.RequireAll) all7++;
            }
            Assert.Greater(all7, all6 / 2, "el nivel 7 pide usar todas más seguido que el 6");
            var rng8 = Rng(88);
            for (int k = 0; k < PerLevel; k++)
            {
                var p = CargaGenerator.Generate(8, rng8);
                Assert.AreEqual(5, p.Cells.Length);
                Assert.IsTrue(p.RequireAll, "nivel 8: siempre usa todas");
                Assert.AreEqual(4, p.ShortestSteps, "juntar las cinco celdas son 4 pasos");
            }
        }

        [Test]
        public void TheFallbackLoadsOfLevels6To8_HaveFiveDistinctCellsWithinTheLevelsNumbers()
        {
            for (int level = 6; level <= 8; level++)
            {
                var spec = CalculoContract.LevelSpec(level);
                var p = CargaGenerator.FallbackFor(spec);
                Assert.AreEqual(5, p.Cells.Length, "nivel " + level);
                Assert.AreEqual(5, new HashSet<int>(p.Cells).Count, "nivel " + level + ": celdas distintas");
                foreach (int c in p.Cells) Assert.LessOrEqual(c, spec.MaxNumber, "nivel " + level);
                Assert.AreEqual(spec.AllChance >= 1f, p.RequireAll, "nivel " + level + ": el 8 usa todas");
                Assert.IsTrue(p.Fallback);
            }
        }

        [Test]
        public void StartRating_WithoutASavedRating_SpreadsTheChosenLevelOverTheFiveLevelsOfBefore_AndWithOneItContinuesFromIt()
        {
            // Sin rating guardado (raro: la app siembra uno) el nivel elegido 1..5 se reparte sobre los 5 niveles de antes, no sobre los 8: quien empieza de cero parte donde partía
            var none = new SequenceConfigDetails { level = 3, base_intensity = 0, has_dda_rating = false };
            Assert.AreEqual(3f, AdaptiveDifficulty.StartRating(none, CalculoContract.MaxLevel, CalculoContract.V1MaxLevel), 1e-4f, "el nivel 3 de 5 es el nivel 3");
            Assert.AreEqual(4.5f, AdaptiveDifficulty.StartRating(none, CalculoContract.MaxLevel), 1e-4f, "sin el reparto, el mismo nivel 3 de 5 caería en el 4,5 de 8 (varios niveles más arriba)");
            var five = new SequenceConfigDetails { level = 5, base_intensity = 0, has_dda_rating = false };
            Assert.AreEqual(5f, AdaptiveDifficulty.StartRating(five, CalculoContract.MaxLevel, CalculoContract.V1MaxLevel), 1e-4f);
            // con rating guardado (ya en la escalera de 8: la app lo tradujo) se continúa desde ahí, con o sin reparto
            var saved = new SequenceConfigDetails { level = 1, has_dda_rating = true, dda_rating = 4.6f / 8f };
            Assert.AreEqual(5.6f, AdaptiveDifficulty.StartRating(saved, CalculoContract.MaxLevel, CalculoContract.V1MaxLevel), 1e-3f, "el 4,6 de 5 de antes es el nivel 4,6 de 8 (rating 4,6/8) = rating de partida 5,6");
            Assert.AreEqual(AdaptiveDifficulty.StartRating(saved, CalculoContract.MaxLevel), AdaptiveDifficulty.StartRating(saved, CalculoContract.MaxLevel, CalculoContract.V1MaxLevel));
            // y el motor de un juego sin alargar no cambia: el parámetro nuevo vale 0
            Assert.AreEqual(AdaptiveDifficulty.StartRating(none, 12), AdaptiveDifficulty.StartRating(none, 12, 0));
        }

        // ------------------------------------------------------------------ solucionador

        [Test]
        public void Solver_FindsTheShortestPath_AndSaysWhenThereIsNone()
        {
            var ops = CalculoContract.LevelSpec(4).Ops;
            Assert.AreEqual(1, CargaSolver.Shortest(new[] { 6, 4, 9 }, ops, 24, false).Count, "6 × 4");
            Assert.AreEqual(2, CargaSolver.Shortest(new[] { 2, 3, 5, 8 }, ops, 22, false).Count);
            Assert.IsNull(CargaSolver.Shortest(new[] { 2, 4 }, CalculoContract.LevelSpec(1).Ops, 99, false), "con solo sumas no se llega a 99");
            Assert.IsNull(CargaSolver.Shortest(new[] { 3, 5 }, CalculoContract.LevelSpec(1).Ops, 12, false));
        }

        [Test]
        public void Solver_WithUseAll_NeedsASingleCellLeft()
        {
            var ops = CalculoContract.LevelSpec(5).Ops;
            // 2 + 3 + 5 = 10; la carga 5 ya está en las celdas, pero «usa todas» pide juntarlas
            var path = CargaSolver.Shortest(new[] { 2, 3, 5 }, ops, 10, true);
            Assert.IsNotNull(path);
            Assert.AreEqual(2, path.Count);
            Assert.AreEqual(0, CargaSolver.Shortest(new[] { 2, 3, 5 }, ops, 5, false).Count, "sin «usa todas» la celda 5 ya es la carga");
        }

        // ------------------------------------------------------------------ reglas de una jugada

        [Test]
        public void Rules_RejectNegativesAndInexactDivisions_WithKindText()
        {
            Assert.IsFalse(CargaRules.Apply(3, CargaOp.Sub, 8, out _, out var r1));
            Assert.AreEqual(CargaReject.Negative, r1);
            StringAssert.Contains("negativa", CargaRules.Explain(3, CargaOp.Sub, 8, r1));
            Assert.IsFalse(CargaRules.Apply(7, CargaOp.Div, 2, out _, out var r2));
            Assert.AreEqual(CargaReject.NotExact, r2);
            Assert.AreEqual("Esa división no da exacta", CargaRules.Explain(7, CargaOp.Div, 2, r2));
            Assert.AreEqual("Esa división no da exacta: prueba al revés", CargaRules.Explain(2, CargaOp.Div, 6, CargaReject.NotExact), "6 ÷ 2 sí da: lo sugiere");
            Assert.IsFalse(CargaRules.Apply(5, CargaOp.Div, 0, out _, out var r3));
            Assert.AreEqual(CargaReject.NotExact, r3);
            Assert.IsTrue(CargaRules.Apply(24, CargaOp.Div, 6, out int q, out _));
            Assert.AreEqual(4, q);
            Assert.IsTrue(CargaRules.Apply(6, CargaOp.Sub, 6, out int z, out _));
            Assert.AreEqual(0, z);
        }

        // ------------------------------------------------------------------ tablero

        [Test]
        public void Board_Merge_RemovesTheFirstCell_AndTheSecondKeepsItsIdWithTheResult()
        {
            var b = new CargaBoard(new[] { 6, 4, 9 });
            Assert.IsTrue(b.TryMerge(0, CargaOp.Mul, 1, out var move, out _));
            Assert.AreEqual(2, b.Count);
            Assert.AreEqual(24, b.Cells[0].Value, "la resultante ocupa el lugar de la segunda");
            Assert.AreEqual(1, b.Cells[0].Id);
            Assert.AreEqual(1, b.Steps);
            Assert.AreEqual(0, move.IdA);
            Assert.AreEqual(1, move.IdB);
        }

        [Test]
        public void Board_AcceptsAnyPathToTheLoad()
        {
            var b = new CargaBoard(new[] { 3, 5, 7 });
            Assert.IsTrue(b.TryMerge(2, CargaOp.Add, 1, out _, out _), "7 + 5");
            Assert.GreaterOrEqual(b.Winning(12, false), 0, "12 = 7 + 5");
            // otro camino, hacia 15: 3 + 5 y luego + 7
            var c = new CargaBoard(new[] { 3, 5, 7 });
            Assert.IsTrue(c.TryMerge(0, CargaOp.Add, 1, out _, out _));
            Assert.IsTrue(c.TryMerge(0, CargaOp.Add, 1, out _, out _));
            Assert.AreEqual(15, c.Cells[0].Value);
            Assert.AreEqual(0, c.Winning(15, false));
        }

        [Test]
        public void Board_UseAll_OnlyCountsWithOneCellLeft_AndStuckIsDetected()
        {
            var b = new CargaBoard(new[] { 2, 3, 5 });
            Assert.IsTrue(b.TryMerge(0, CargaOp.Add, 1, out _, out _));      // quedan 5 y 5
            Assert.IsTrue(b.HasTarget(5));
            Assert.AreEqual(-1, b.Winning(5, true), "quedan dos celdas: todavía no");
            Assert.GreaterOrEqual(b.Winning(5, false), 0);
            Assert.IsTrue(b.TryMerge(0, CargaOp.Add, 1, out _, out _));      // 10
            Assert.AreEqual(0, b.Winning(10, true));
            Assert.IsTrue(b.Stuck(15));
            Assert.IsFalse(b.Stuck(10));
        }

        [Test]
        public void Board_RefusedMoves_DoNotChangeAnything_AndUndoAndResetWork()
        {
            var b = new CargaBoard(new[] { 3, 8, 5 });
            Assert.IsFalse(b.TryMerge(0, CargaOp.Sub, 1, out _, out var why));
            Assert.AreEqual(CargaReject.Negative, why);
            Assert.IsFalse(b.TryMerge(0, CargaOp.Div, 2, out _, out why));
            Assert.AreEqual(CargaReject.NotExact, why);
            Assert.IsFalse(b.TryMerge(1, CargaOp.Add, 1, out _, out _), "una celda no se junta con sí misma");
            Assert.AreEqual(3, b.Count);
            Assert.AreEqual(0, b.Steps);
            Assert.IsTrue(b.TryMerge(1, CargaOp.Sub, 0, out _, out _));      // 8 − 3 = 5
            Assert.IsTrue(b.TryMerge(0, CargaOp.Add, 1, out _, out _));      // 5 + 5? (queda una)
            Assert.AreEqual(2, b.Steps);
            Assert.IsTrue(b.Undo());
            Assert.AreEqual(1, b.Steps);
            Assert.AreEqual(2, b.Count);
            b.Reset();
            Assert.AreEqual(0, b.Steps);
            Assert.AreEqual(3, b.Count);
            Assert.AreEqual(3, b.Cells[0].Value);
            Assert.AreEqual(8, b.Cells[1].Value);
            Assert.IsFalse(b.Undo(), "no hay nada que deshacer");
        }

        // ------------------------------------------------------------------ medidas, crédito y puntaje

        [Test]
        public void Tally_CountsLoadsHintsShortPathsAndTheMeanTimeOfTheOnesSolvedAlone()
        {
            var t = new CargaTally();
            t.Record(CargaOutcome.Alone, 2, 2, 10000);
            t.Record(CargaOutcome.Alone, 4, 2, 20000);
            t.Record(CargaOutcome.Hinted, 2, 2, 60000);
            t.Record(CargaOutcome.Missed, 0, 2, 0);
            Assert.AreEqual(4, t.Total);
            Assert.AreEqual(2, t.SolvedAlone);
            Assert.AreEqual(1, t.Hinted);
            Assert.AreEqual(1, t.Missed);
            Assert.AreEqual(3, t.Solved);
            Assert.AreEqual(1, t.ShortPaths, "solo la primera usó los pasos del camino más corto sin pista");
            Assert.AreEqual(15000, t.MeanAloneMs);
            Assert.AreEqual(-1, new CargaTally().MeanAloneMs);
        }

        [Test]
        public void Credit_HintedCountsHalf_AsAlternatingHits()
        {
            var c = new CargaCredit();
            Assert.IsTrue(c.CountsAsHit(CargaOutcome.Alone));
            Assert.IsFalse(c.CountsAsHit(CargaOutcome.Missed));
            Assert.IsTrue(c.CountsAsHit(CargaOutcome.Hinted));
            Assert.IsFalse(c.CountsAsHit(CargaOutcome.Hinted));
            Assert.IsTrue(c.CountsAsHit(CargaOutcome.Hinted));
        }

        [Test]
        public void Score_PrecisionIsOutOfEightLoads_RetoOutOfTheReferencePace()
        {
            var t = new CargaTally();
            for (int i = 0; i < 8; i++) t.Record(CargaOutcome.Alone, 1, 1, 5000);
            Assert.AreEqual(100, CalculoContract.Score(t, false));
            var h = new CargaTally();
            for (int i = 0; i < 8; i++) h.Record(CargaOutcome.Hinted, 1, 1, 30000);
            Assert.AreEqual(50, CalculoContract.Score(h, false), "con pista vale la mitad");
            var r = new CargaTally();
            for (int i = 0; i < 3; i++) r.Record(CargaOutcome.Alone, 1, 1, 8000);
            Assert.AreEqual(50, CalculoContract.Score(r, true), "3 de las 6 de referencia");
            Assert.AreEqual(0, CalculoContract.Score(new CargaTally(), true));
            for (int i = 0; i < 10; i++) r.Record(CargaOutcome.Alone, 1, 1, 8000);
            Assert.AreEqual(100, CalculoContract.Score(r, true), "nunca pasa de 100");
        }

        [Test]
        public void Hint_IsEarlierForSeniors()
        {
            Assert.AreEqual(25f, CalculoContract.HintAfter(false));
            Assert.AreEqual(20f, CalculoContract.HintAfter(true));
        }
    }

    public class CalculoVisibilityTests
    {
        [Test]
        public void EveryPieceTheGameDraws_IsOnAndOpaque()
        {
            // lección de Punta (3-oct): una pieza creada APAGADA deja solo el número tinta sobre el cielo oscuro.
            var go = new UnityEngine.GameObject("CargaAudit");
            try
            {
                var controller = go.AddComponent<CalculoGameController>();
                // solo se arma la interfaz (Awake también estiliza el panel de resultado, y eso usa Destroy, que en modo edición no se puede)
                var build = typeof(CalculoGameController).GetMethod("BuildUi", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                build.Invoke(controller, null);
                go.SetActive(true);
                var problems = controller.AuditVisibility();
                Assert.IsEmpty(problems, "Piezas que no se verían: " + string.Join(" | ", problems));
            }
            finally { UnityEngine.Object.DestroyImmediate(go); }
        }
    }

    /// <summary>El aro de 12 luces del reactor (Tarea 130, juice): se llena con la mejor celda que no se pasa de la carga, <c>round(12 · mejor / carga)</c>. Es una lectura del tablero, no una pista.</summary>
    public class CargaLitLightsTests
    {
        [Test]
        public void TheBestCellThatDoesNotPass_FillsTheRing_InTwelfths()
        {
            Assert.AreEqual(9, CargaRules.LitLights(new[] { 3, 7, 9 }, 12), "9 de 12: nueve luces");
            Assert.AreEqual(5, CargaRules.LitLights(new[] { 5, 20, 31 }, 12), "las celdas que se pasan no cuentan: la mejor es 5");
            Assert.AreEqual(12, CargaRules.LitLights(new[] { 2, 12 }, 12), "justo: las doce");
            Assert.AreEqual(2, CargaRules.LitLights(new[] { 4 }, 30), "4 de 30 = 1,6: redondea a 2");
            Assert.AreEqual(0, CargaRules.LitLights(new[] { 1 }, 30), "1 de 30 = 0,4: ninguna");
            Assert.AreEqual(1, CargaRules.LitLights(new[] { 6 }, 99), "6 de 99 = 0,7: una");
        }

        [Test]
        public void WhenEveryCellPassesTheTarget_OrThereIsNoTarget_TheRingIsDark()
        {
            Assert.AreEqual(0, CargaRules.LitLights(new[] { 13, 20 }, 12));
            Assert.AreEqual(0, CargaRules.LitLights(new int[0], 12));
            Assert.AreEqual(0, CargaRules.LitLights(new[] { 3 }, 0));
            Assert.AreEqual(0, CargaRules.LitLights(null, 12));
        }

        [Test]
        public void TheRing_NeverHasMoreThanTwelveLights_NorGoesBackwardsWhenTheBestCellGrows()
        {
            int last = 0;
            for (int best = 0; best <= 24; best++)
            {
                int lit = CargaRules.LitLights(new[] { best }, 24);
                Assert.GreaterOrEqual(lit, last, "con una celda mejor nunca hay menos luces");
                Assert.LessOrEqual(lit, 12);
                last = lit;
            }
            Assert.AreEqual(12, last);
        }
    }

    public class CalculoLayoutTests
    {
        private static readonly float[] Heights = { 600f, 640f, 700f, 780f, 860f, 960f };

        [Test]
        public void EveryScreenHeight_FitsEverythingInOrder_WithoutOverlap()
        {
            foreach (float h in Heights)
            {
                var m = CargaLayout.Compute(h);
                string where = "alto " + h;
                Assert.Greater(m.ReactorY - CargaLayout.RingR, CargaLayout.HudDp, where + ": el reactor queda bajo el marcador");
                Assert.Less(m.ReactorY + CargaLayout.RingR, m.SayY - 10f, where + ": el mensaje va bajo el reactor");
                Assert.Less(m.SayY + 15f, m.CellsY - CargaLayout.CellH / 2f - 6f, where + ": las celdas van bajo el mensaje");
                Assert.Less(m.CellsY + CargaLayout.CellH / 2f, m.GuideY - 10f, where + ": la guía va bajo las celdas");
                Assert.Less(m.GuideY + 12f, m.OpsY - CargaLayout.OpH / 2f, where + ": las operaciones van bajo la guía");
                Assert.Less(m.OpsY + CargaLayout.OpH / 2f, m.ToolsY - CargaLayout.ToolH / 2f - 8f, where + ": los botones van bajo las operaciones");
                Assert.LessOrEqual(m.ToolsY + CargaLayout.ToolH / 2f, h - 20f, where + ": los botones caben");
            }
        }

        [Test]
        public void TheCells_AreBigEnoughToTouch_AndFitTheWidth_ForThreeFourAndFive()
        {
            var m = CargaLayout.Compute(700f);
            for (int n = 3; n <= 5; n++)
            {
                Assert.GreaterOrEqual(CargaLayout.CellW(n), 56f, n + " celdas (Tarea 129: lo mínimo para tocar con el pulgar; una carga nunca pasa de 5 celdas)");
                Assert.GreaterOrEqual(CargaLayout.CellH, 44f);
                float left = CargaLayout.CellPos(m, 0, n).x - CargaLayout.CellW(n) / 2f;
                float right = CargaLayout.CellPos(m, n - 1, n).x + CargaLayout.CellW(n) / 2f;
                Assert.GreaterOrEqual(left, 10f, n + " celdas: cabe a la izquierda");
                Assert.LessOrEqual(right, CargaLayout.W - 10f, n + " celdas: cabe a la derecha");
                for (int k = 1; k < n; k++)
                    Assert.Greater(CargaLayout.CellPos(m, k, n).x - CargaLayout.CellPos(m, k - 1, n).x, CargaLayout.CellW(n), "no se pisan");
            }
        }

        [Test]
        public void NoLevel_HasMoreCellsThanFitInOneRow_At56Dp()
        {
            foreach (var lv in CalculoContract.Levels)
                Assert.GreaterOrEqual(CargaLayout.CellW(lv.CellsMax), 56f, "nivel " + lv.Level + ": hasta " + lv.CellsMax + " celdas en una fila miden al menos 56 dp (si algún nivel pidiera más, irían en 2 filas)");
        }

        [Test]
        public void TheOperationsAndTheToolButtons_AreBigEnoughToTouch_AndDoNotOverlap()
        {
            var m = CargaLayout.Compute(700f);
            Assert.GreaterOrEqual(CargaLayout.OpW, 44f);
            Assert.GreaterOrEqual(CargaLayout.OpH, 44f);
            Assert.GreaterOrEqual(CargaLayout.ToolH, 44f);
            for (int n = 1; n <= 4; n++)
                for (int k = 1; k < n; k++)
                    Assert.Greater(CargaLayout.OpPos(m, k, n).x - CargaLayout.OpPos(m, k - 1, n).x, CargaLayout.OpW);
            Assert.Greater(CargaLayout.ToolPos(m, 1).x - CargaLayout.ToolPos(m, 0).x, CargaLayout.ToolW);
            Assert.LessOrEqual(CargaLayout.ToolPos(m, 1).x + CargaLayout.ToolW / 2f, CargaLayout.W - 10f);
        }
    }
}
