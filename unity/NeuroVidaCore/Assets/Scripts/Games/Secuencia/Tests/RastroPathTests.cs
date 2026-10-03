using System;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace NeuroVida.Games.Secuencia.Tests
{
    /// <summary>
    /// El tablero y los caminos de «Rastro de luz»: separación y tamaño de los luceros, qué tramos quedan despejados, generación de caminos
    /// (sin repetir el mismo lucero seguido; cruces solo donde el nivel los permite), «en marcha» (pide exactamente las últimas N), el giro del cielo
    /// (los luceros conservan su identidad) y qué lucero toca el dedo.
    /// </summary>
    public class RastroPathTests
    {
        // ------------------------------------------------------------------ el tablero

        [Test]
        public void TheBoard_OrbsAreFarEnoughApart_AndFitOnTheScreenAtAnyTurn()
        {
            float minCenters = float.MaxValue;
            for (int i = 0; i < RastroBoard.Orbs; i++)
                for (int j = i + 1; j < RastroBoard.Orbs; j++)
                    minCenters = Mathf.Min(minCenters, Vector2.Distance(RastroBoard.Base(i), RastroBoard.Base(j)));
            // luceros de 64 dp (mayores) + 12 dp de aire = 76 dp entre centros (con 56 dp sobra)
            Assert.GreaterOrEqual(minCenters, RastroBoard.OrbDiameterSenior + 12f, $"separación mínima {minCenters:0.0} dp");
            // las zonas de enganche no se pisan
            Assert.GreaterOrEqual(minCenters, 2f * RastroBoard.HitRadiusSenior);
            Assert.GreaterOrEqual(2f * RastroBoard.HitRadius, 72f, "zona de toque ≥ 72 dp");
            for (float angle = 0f; angle < 360f; angle += 15f)
                for (int i = 0; i < RastroBoard.Orbs; i++)
                {
                    var p = RastroBoard.Position(i, angle);
                    float r = RastroBoard.OrbDiameterSenior * 0.5f;
                    Assert.That(p.x - r, Is.GreaterThanOrEqualTo(4f), $"giro {angle}° lucero {i}");
                    Assert.That(p.x + r, Is.LessThanOrEqualTo(356f), $"giro {angle}° lucero {i}");
                    Assert.That(p.y - r, Is.GreaterThanOrEqualTo(150f), $"giro {angle}° lucero {i}: choca con el marcador");
                    Assert.That(p.y + r, Is.LessThanOrEqualTo(530f), $"giro {angle}° lucero {i}: choca con las instrucciones");
                }
            Assert.GreaterOrEqual(RastroBoard.OrbDiameter, 56f);
            Assert.GreaterOrEqual(RastroBoard.OrbDiameterSenior, 64f);
        }

        [Test]
        public void TheOrbsHaveNineDistinctColorsAndNotes()
        {
            Assert.AreEqual(9, RastroBoard.Colors.Length);
            Assert.AreEqual(9, RastroBoard.Notes.Length);
            CollectionAssert.AllItemsAreUnique(RastroBoard.Colors);
            CollectionAssert.AllItemsAreUnique(RastroBoard.Notes);
            for (int i = 1; i < 9; i++) Assert.Greater(RastroBoard.Notes[i], RastroBoard.Notes[i - 1], "las notas suben por la pentatónica");
        }

        [Test]
        public void TurningTheSky_KeepsEveryOrbsIdentityAndDistances()
        {
            // «El cielo gira»: lo que gira es el cielo, no el orden. El lucero i sigue siendo i (mismo color y nota) y las distancias no cambian.
            foreach (float angle in new[] { 45f, 90f, 137f, 180f, -120f, 359f })
            {
                for (int i = 0; i < RastroBoard.Orbs; i++)
                {
                    Assert.AreEqual(Vector2.Distance(RastroBoard.Base(i), RastroBoard.Center), Vector2.Distance(RastroBoard.Position(i, angle), RastroBoard.Center), 1e-3f);
                    // y el lucero que toca el dedo en la posición girada de i es i (no otro)
                    Assert.AreEqual(i, RastroBoard.OrbAt(RastroBoard.Position(i, angle), angle, RastroBoard.HitRadius), $"giro {angle}°");
                    for (int j = i + 1; j < RastroBoard.Orbs; j++)
                        Assert.AreEqual(Vector2.Distance(RastroBoard.Base(i), RastroBoard.Base(j)), Vector2.Distance(RastroBoard.Position(i, angle), RastroBoard.Position(j, angle)), 1e-2f);
                }
            }
            for (int i = 0; i < RastroBoard.Orbs; i++)
                Assert.AreEqual(0f, Vector2.Distance(RastroBoard.Base(i), RastroBoard.Position(i, 360f)), 1e-2f);
        }

        [Test]
        public void OrbAt_FindsTheNearestOrbWithinTheRadius()
        {
            Assert.AreEqual(-1, RastroBoard.OrbAt(new Vector2(5f, 5f), 0f, RastroBoard.HitRadius));
            var c0 = RastroBoard.Base(0);
            Assert.AreEqual(0, RastroBoard.OrbAt(c0 + new Vector2(30f, 0f), 0f, RastroBoard.HitRadius));
            Assert.AreEqual(-1, RastroBoard.OrbAt(c0 + new Vector2(0f, 45f), 0f, 36f), "a 45 dp de cualquier lucero");
            // entre dos luceros gana el más cercano
            var mid = Vector2.Lerp(RastroBoard.Base(1), RastroBoard.Base(2), 0.3f);
            Assert.AreEqual(1, RastroBoard.OrbAt(mid, 0f, 60f));
        }

        // ------------------------------------------------------------------ tramos despejados y cruces

        [Test]
        public void ClearSegments_LeaveEveryOrbWithAtLeastThreeNeighbours_AndTheRingIsAlwaysClear()
        {
            for (int i = 0; i < RastroBoard.Orbs; i++)
            {
                int degree = 0;
                for (int j = 0; j < RastroBoard.Orbs; j++)
                    if (i != j && RastroBoard.SegmentClear(i, j)) degree++;
                Assert.GreaterOrEqual(degree, 3, $"el lucero {i} solo tiene {degree} tramos despejados");
            }
            for (int i = 0; i < 8; i++) Assert.IsTrue(RastroBoard.SegmentClear(i, (i + 1) % 8), $"el anillo {i}-{(i + 1) % 8}");
            for (int i = 0; i < 8; i++) Assert.IsTrue(RastroBoard.SegmentClear(i, 8), $"el centro conecta con {i}");
            // simétrico
            for (int i = 0; i < 9; i++)
                for (int j = 0; j < 9; j++)
                    if (i != j) Assert.AreEqual(RastroBoard.SegmentClear(i, j), RastroBoard.SegmentClear(j, i));
        }

        [Test]
        public void SegmentsCross_DetectsProperCrossingsOnly()
        {
            Assert.IsTrue(RastroBoard.SegmentsCross(new Vector2(0, 0), new Vector2(10, 10), new Vector2(0, 10), new Vector2(10, 0)));
            Assert.IsFalse(RastroBoard.SegmentsCross(new Vector2(0, 0), new Vector2(10, 0), new Vector2(0, 5), new Vector2(10, 5)));
            Assert.IsFalse(RastroBoard.SegmentsCross(new Vector2(0, 0), new Vector2(5, 5), new Vector2(5, 5), new Vector2(10, 0)), "comparten un extremo: no es cruce");
        }

        [Test]
        public void TheBoardHasPathsThatCross_AndCrossingsAreCountedOnNonNeighbouringSegmentsOnly()
        {
            // algún par de tramos despejados que no comparten lucero se cruza
            bool found = false;
            for (int a = 0; a < 9 && !found; a++)
                for (int b = a + 1; b < 9 && !found; b++)
                    for (int c = 0; c < 9 && !found; c++)
                        for (int d = c + 1; d < 9 && !found; d++)
                        {
                            if (a == c || a == d || b == c || b == d) continue;
                            if (!RastroBoard.SegmentClear(a, b) || !RastroBoard.SegmentClear(c, d)) continue;
                            if (!RastroBoard.SegmentsCross(RastroBoard.Base(a), RastroBoard.Base(b), RastroBoard.Base(c), RastroBoard.Base(d))) continue;
                            // camino a-b-c-d: los tramos a-b y c-d no son vecinos y se cruzan
                            if (RastroBoard.SegmentClear(b, c)) { Assert.AreEqual(1, RastroPaths.Crossings(new[] { a, b, c, d })); found = true; }
                        }
            Assert.IsTrue(found, "el tablero debe permitir cruces");
            Assert.AreEqual(0, RastroPaths.Crossings(new[] { 0, 1, 2 }), "tres luces no tienen tramos no vecinos");
        }

        // ------------------------------------------------------------------ generación

        [Test]
        public void GeneratedPaths_HaveTheLengthTheyAsk_NeverRepeatAnOrb_AndEverySegmentIsClear()
        {
            var rng = new System.Random(11);
            for (int length = 2; length <= 9; length++)
                for (int k = 0; k < 120; k++)
                {
                    var path = RastroPaths.Generate(rng, length, RastroLadder.CrossAny, 3);
                    Assert.AreEqual(length, path.Length);
                    CollectionAssert.AllItemsAreUnique(path, $"largo {length}");
                    for (int i = 0; i < path.Length; i++) Assert.That(path[i], Is.InRange(0, 8));
                    for (int i = 1; i < path.Length; i++)
                    {
                        Assert.AreNotEqual(path[i - 1], path[i], "nunca el mismo lucero seguido");
                        Assert.IsTrue(RastroBoard.SegmentClear(path[i - 1], path[i]), $"tramo {path[i - 1]}→{path[i]} pasa pegado a otro lucero");
                    }
                }
        }

        [Test]
        public void Crossings_NeverExceedWhatTheLevelAllows_AndLevelsWithoutCrossingsHaveNone()
        {
            var rng = new System.Random(21);
            for (int level = 1; level <= 16; level++)
            {
                var p = RastroLadder.Get(level);
                foreach (var mode in RastroLadder.FamiliesAt(level))
                {
                    int length = mode == RastroMode.Marcha ? p.ShownMax : p.Length(mode);
                    for (int k = 0; k < 150; k++)
                    {
                        var path = RastroPaths.Generate(rng, length, p.CrossMax, p.CrossTarget);
                        int c = RastroPaths.Crossings(path);
                        Assert.LessOrEqual(c, p.CrossMax, $"nivel {level} {mode}: {c} cruces");
                    }
                }
            }
            for (int level = 1; level <= 9; level++)
                for (int k = 0; k < 100; k++)
                    Assert.AreEqual(0, RastroPaths.Crossings(RastroPaths.Generate(rng, RastroLadder.Get(level).LenRastro, 0, 0)), $"nivel {level}");
        }

        [Test]
        public void LevelsThatAllowCrossings_ReallyProduceThem()
        {
            var rng = new System.Random(31);
            foreach (int level in new[] { 10, 12, 16 })
            {
                var p = RastroLadder.Get(level);
                int withCross = 0;
                for (int k = 0; k < 100; k++) if (RastroPaths.Crossings(RastroPaths.Generate(rng, p.LenRastro, p.CrossMax, p.CrossTarget)) > 0) withCross++;
                Assert.GreaterOrEqual(withCross, 40, $"nivel {level}: solo {withCross} de 100 caminos con cruces");
            }
        }

        [Test]
        public void TheRingFallback_IsAlwaysClearAndCrossesNothing()
        {
            var rng = new System.Random(2);
            for (int length = 2; length <= 9; length++)
                for (int k = 0; k < 20; k++)
                {
                    var ring = RastroPaths.Ring(rng, length);
                    Assert.AreEqual(length, ring.Length);
                    CollectionAssert.AllItemsAreUnique(ring);
                    for (int i = 1; i < ring.Length; i++) Assert.IsTrue(RastroBoard.SegmentClear(ring[i - 1], ring[i]));
                    Assert.AreEqual(0, RastroPaths.Crossings(ring));
                }
        }

        // ------------------------------------------------------------------ las rondas

        [Test]
        public void TheMarchRound_AsksExactlyTheLastN_OfALongerPath()
        {
            var s = new RastroSession(new NeuroVida.Contracts.SequenceConfigDetails { age_band = "ADULT", has_dda_rating = true, dda_rating = 0.95f }, new System.Random(4), 15);
            int marchRounds = 0;
            for (int k = 0; k < 400 && marchRounds < 60; k++)
            {
                var r = s.NextRound();
                s.Complete(r, true);
                if (r.Mode != RastroMode.Marcha) continue;
                marchRounds++;
                var p = r.Params;
                Assert.AreEqual(p.LenMarcha, r.Asked, "pide exactamente las N del nivel");
                Assert.That(r.Shown.Length, Is.InRange(p.ShownMin, p.ShownMax));
                Assert.Greater(r.Shown.Length, r.Asked, "la chispa recorre más luces de las que se piden");
                int[] last = new int[r.Asked];
                Array.Copy(r.Shown, r.Shown.Length - r.Asked, last, 0, r.Asked);
                CollectionAssert.AreEqual(last, r.Target, "las últimas N, en orden");
            }
            Assert.GreaterOrEqual(marchRounds, 20);
        }

        [Test]
        public void TheReverseRound_AsksTheShownPathBackwards_AndTheTurnRoundKeepsTheOrder()
        {
            var s = new RastroSession(new NeuroVida.Contracts.SequenceConfigDetails { age_band = "ADULT", has_dda_rating = true, dda_rating = 0.7f }, new System.Random(8), 15);
            int reverse = 0, turn = 0;
            bool left = false, right = false;
            for (int k = 0; k < 600; k++)
            {
                var r = s.NextRound();
                s.Complete(r, true);
                if (r.Mode == RastroMode.Reves)
                {
                    reverse++;
                    var back = (int[])r.Shown.Clone();
                    Array.Reverse(back);
                    CollectionAssert.AreEqual(back, r.Target);
                    Assert.AreEqual(0f, r.TurnDegrees);
                }
                else if (r.Mode == RastroMode.Gira)
                {
                    turn++;
                    CollectionAssert.AreEqual(r.Shown, r.Target, "gira: se repite el mismo orden, sobre los mismos luceros");
                    float mag = Mathf.Abs(r.TurnDegrees);
                    Assert.That(mag, Is.InRange(r.Params.TurnMin - 0.01f, r.Params.TurnMax + 0.01f));
                    if (r.TurnDegrees < 0f) left = true; else right = true;
                }
                else if (r.Mode == RastroMode.Rastro)
                {
                    CollectionAssert.AreEqual(r.Shown, r.Target);
                    Assert.AreEqual(0f, r.TurnDegrees);
                }
                Assert.AreEqual(r.Params.Length(r.Mode), r.Asked);
            }
            Assert.Greater(reverse, 10);
            Assert.Greater(turn, 10);
            Assert.IsTrue(left && right, "el giro va para los dos lados");
        }

        [Test]
        public void TheSpark_IsSlowerForSeniors_By15Percent()
        {
            var adult = new RastroSession(new NeuroVida.Contracts.SequenceConfigDetails { age_band = "ADULT", has_dda_rating = true, dda_rating = 0.5f }, new System.Random(1), 15);
            var senior = new RastroSession(new NeuroVida.Contracts.SequenceConfigDetails { age_band = "SENIOR", has_dda_rating = true, dda_rating = 0.5f }, new System.Random(1), 15);
            var ra = adult.NextRound();
            var rs = senior.NextRound();
            Assert.AreEqual(ra.Level, rs.Level);
            Assert.AreEqual(ra.Params.SparkSpeed * 0.85f, rs.SparkSpeed, 1e-3f);
            Assert.AreEqual(ra.Params.SparkSpeed, ra.SparkSpeed, 1e-3f);
        }
    }
}
