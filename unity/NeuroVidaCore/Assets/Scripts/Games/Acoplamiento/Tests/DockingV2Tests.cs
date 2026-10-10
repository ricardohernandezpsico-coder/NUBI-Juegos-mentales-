using System;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace NeuroVida.Games.Acoplamiento.Tests
{
    /// <summary>
    /// Acoplamiento «muelle de acoplamiento» (Tarea 68, 10-oct; docs/diseno-acoplamiento.md): las piezas caben en 4 × 4 bloques, la pantalla de 360 × 780 dp (y la de 16:9 que escala), la estación de anillos de 8, los textos del aviso, la revelación y el arte (el módulo sin luz horneada: la luz
    /// y la sombra quedan fijas en la pantalla).
    /// </summary>
    public class DockingV2Tests
    {
        // ------------------------------------------------------------------ el contrato: la pieza cabe en 4 × 4

        [Test]
        public void EveryPieceFitsIn4By4Blocks_AndIsConnectedChiralAndTheRightSize_At12LevelsAndManySeeds()
        {
            for (int level = 1; level <= DockingContract.MaxLevel; level++)
                for (int seed = 0; seed < 600; seed++)
                {
                    var shape = DockingContract.RandomChiralShape(DockingContract.Cells(level), new System.Random(seed * 7919 + level));
                    string at = " (nivel " + level + ", semilla " + seed + ")";
                    Assert.AreEqual(DockingContract.Cells(level), shape.Length, "la cantidad de bloques" + at);
                    Assert.IsTrue(DockingContract.FitsInBox(shape), "cabe en 4 × 4" + at);
                    Assert.IsTrue(DockingContract.IsConnected(shape), "conexa" + at);
                    Assert.IsTrue(DockingContract.IsChiral(shape), "quiral (distinta de su espejo)" + at);
                }
        }

        [Test]
        public void ATrialAtAnyLevelKeepsTheLadderAndFitsIn4By4()
        {
            for (int level = 1; level <= DockingContract.MaxLevel; level++)
                for (int seed = 0; seed < 100; seed++)
                {
                    var t = DockingContract.NextTrial(level, new System.Random(seed));
                    Assert.IsTrue(DockingContract.FitsInBox(t.Shape));
                    Assert.LessOrEqual(Math.Abs(t.AngleDeg), DockingContract.MaxAngle(level));
                    Assert.AreEqual(0, Math.Abs(t.AngleDeg) % DockingContract.AngleStep(level));
                }
            Assert.IsFalse(DockingContract.FitsInBox(new[] { new Cell(0, 0), new Cell(1, 0), new Cell(2, 0), new Cell(3, 0), new Cell(4, 0) }), "una barra de 5 no cabe");
            Assert.IsTrue(DockingContract.FitsInBox(new[] { new Cell(0, 0), new Cell(1, 0), new Cell(2, 0), new Cell(3, 0) }), "una barra de 4 sí");
        }

        [Test]
        public void TheRulesOfTodayDidNotChange()
        {
            Assert.AreEqual(12, DockingContract.MaxLevel);
            Assert.AreEqual(120, DockingContract.RetoSeconds);
            Assert.AreEqual(24, DockingContract.PrecisionTrials);
            Assert.AreEqual(4, DockingContract.Cells(1));
            Assert.AreEqual(5, DockingContract.Cells(4));
            Assert.AreEqual(6, DockingContract.Cells(7));
            Assert.AreEqual(7, DockingContract.Cells(12));
            Assert.AreEqual(6000, DockingContract.DeadlineMs(1, false));
            Assert.AreEqual(2500, DockingContract.DeadlineMs(12, false));
            Assert.AreEqual(12000, DockingContract.DeadlineMs(5, true));
            Assert.AreEqual(90, DockingContract.MaxAngle(1));
            Assert.AreEqual(135, DockingContract.MaxAngle(2));
            Assert.AreEqual(180, DockingContract.MaxAngle(3));
            Assert.AreEqual(45, DockingContract.AngleStep(4));
            Assert.AreEqual(15, DockingContract.AngleStep(5));
        }

        // ------------------------------------------------------------------ los textos y la revelación

        [Test]
        public void TheNoticesAreTheOnesOfTheTable()
        {
            Assert.AreEqual("¡Encaja!", DockingContract.Notice(false, DockingContract.AnswerFits, 1));
            Assert.AreEqual("¡Encaja!", DockingContract.Notice(false, DockingContract.AnswerFits, 2));
            Assert.AreEqual("¡Encaja! Racha ×3", DockingContract.Notice(false, DockingContract.AnswerFits, 3));
            Assert.AreEqual("¡Encaja! Racha ×7", DockingContract.Notice(false, DockingContract.AnswerFits, 7));
            Assert.AreEqual("¡Bien visto! Era su espejo", DockingContract.Notice(true, DockingContract.AnswerMirror, 5));
            Assert.AreEqual("Era su espejo", DockingContract.Notice(true, DockingContract.AnswerFits, 0));
            Assert.AreEqual("Sí encajaba", DockingContract.Notice(false, DockingContract.AnswerMirror, 0));
            Assert.AreEqual("Sin tiempo: era su espejo", DockingContract.Notice(true, DockingContract.AnswerTimeout, 0));
            Assert.AreEqual("Sin tiempo: sí encajaba", DockingContract.Notice(false, DockingContract.AnswerTimeout, 0));
            foreach (var bad in new[] { "cognitiv", "entrenamiento", "cerebro", "combustible" })
                foreach (var t in new[] { DockingContract.Hint, DockingContract.RingComplete, DockingContract.EndTag, DockingContract.EndNote1, DockingContract.EndNote2, DockingContract.Notice(true, DockingContract.AnswerTimeout, 0) })
                    Assert.IsFalse(t.ToLowerInvariant().Contains(bad), "«" + t + "» usa «" + bad + "»");
        }

        [Test]
        public void ARightAnswerIsTheOneThatMatchesTheTruth_AndNoAnswerIsAlwaysAMistake()
        {
            Assert.IsTrue(DockingContract.IsCorrect(false, DockingContract.AnswerFits));
            Assert.IsTrue(DockingContract.IsCorrect(true, DockingContract.AnswerMirror));
            Assert.IsFalse(DockingContract.IsCorrect(false, DockingContract.AnswerMirror));
            Assert.IsFalse(DockingContract.IsCorrect(true, DockingContract.AnswerFits));
            Assert.IsFalse(DockingContract.IsCorrect(false, DockingContract.AnswerTimeout));
            Assert.IsFalse(DockingContract.IsCorrect(true, DockingContract.AnswerTimeout));
        }

        [Test]
        public void TheRevealTimesAreTheOnesOfTheDesign_AndEveryEightModulesTheRingIsComplete()
        {
            Assert.AreEqual(420, DockingContract.ArriveMs);
            Assert.AreEqual(320, DockingContract.UpMs);
            Assert.AreEqual(300, DockingContract.FlipMs);
            Assert.AreEqual(380, DockingContract.DownMs);
            Assert.AreEqual(220, DockingContract.HoldMs);
            Assert.AreEqual(520, DockingContract.FlyMs);
            Assert.AreEqual(600, DockingContract.GoneMs);
            for (int docked = 1; docked <= 40; docked++) Assert.AreEqual(docked % 8 == 0, DockingContract.CompletesRing(docked), "módulo " + docked);
            Assert.IsFalse(DockingContract.CompletesRing(0));
            Assert.AreEqual((0, 0), DockingContract.SlotOf(0));
            Assert.AreEqual((0, 7), DockingContract.SlotOf(7));
            Assert.AreEqual((1, 0), DockingContract.SlotOf(8));
            Assert.AreEqual((2, 3), DockingContract.SlotOf(19));
        }

        [Test]
        public void TheEndTextsAndTheCurve()
        {
            Assert.AreEqual("1 módulo acoplado", DockingContract.EndTitle(1));
            Assert.AreEqual("24 módulos acoplados", DockingContract.EndTitle(24));
            Assert.AreEqual("0 módulos acoplados", DockingContract.EndTitle(0));
            Assert.AreEqual("9 de 12", DockingContract.HitsValue(9, 12));
            Assert.AreEqual("212° por segundo", DockingContract.SpeedValue(212));
            Assert.AreEqual("—", DockingContract.SpeedValue(-1));
            Assert.AreEqual("×4", DockingContract.StreakValue(4));
            Assert.AreEqual("—", DockingContract.StreakValue(1));
            Assert.AreEqual("1,5 s", DockingContract.CurveValue(1500));
            Assert.AreEqual("0,9 s", DockingContract.CurveValue(900));
            Assert.AreEqual("—", DockingContract.CurveValue(-1));
            Assert.AreEqual("Módulo 3 de 24", DockingContract.RoundChip(3, 24));
            Assert.AreEqual("Módulo 30", DockingContract.RoundChip(30, 0));
            Assert.AreEqual("Tu récord: 1 módulo", DockingContract.RecordLine(1));
            Assert.AreEqual("Tu récord: 31 módulos", DockingContract.RecordLine(31));
        }

        // ------------------------------------------------------------------ la pantalla

        [Test]
        public void OnTheReferenceScreenTheNumbersAreTheOnesOfTheDesign()
        {
            var p = new DockingPlan(780f, false);
            Assert.AreEqual(1f, p.K, 1e-5f);
            Assert.AreEqual(0f, p.Extra, 1e-5f);
            Assert.AreEqual(26f, p.Block, 1e-4f, "bloques de 26 dp");
            Assert.AreEqual(180f, p.ModuleCx, 1e-4f);
            Assert.AreEqual(360f, p.ModuleCy, 1e-3f, "módulo en (180, 360)");
            Assert.AreEqual(444f, p.FuelCy, 1e-3f, "barra de tiempo (80, 438) de 200 × 12 → centro y 444");
            Assert.AreEqual(200f, p.FuelW, 1e-4f);
            Assert.AreEqual(12f, p.FuelH, 1e-4f);
            Assert.AreEqual(534f, p.PortCy, 1e-3f, "puerto en (180, 534)");
            Assert.AreEqual(168f, p.PortW, 1e-4f);
            Assert.AreEqual(146f, p.PortH, 1e-4f);
            Assert.AreEqual(660f, p.ButtonCy, 1e-3f, "botones desde y 622 de 76 de alto → centro y 660");
            Assert.AreEqual(158f, p.ButtonW, 1e-4f);
            Assert.AreEqual(76f, p.ButtonH, 1e-4f);
            Assert.AreEqual(12f, p.ButtonGap, 1e-4f);
            Assert.AreEqual(734f, p.HintCy, 1e-3f, "la pista en y 734");
            var (lx, ly) = p.ButtonCenter(0);
            var (rx, ry) = p.ButtonCenter(1);
            Assert.AreEqual(180f - 85f, lx, 1e-3f, "«Encaja» a la izquierda: x 16 a 174");
            Assert.AreEqual(180f + 85f, rx, 1e-3f, "«Espejo» a la derecha: x 186 a 344");
            Assert.AreEqual(660f, ly, 1e-3f);
            Assert.AreEqual(660f, ry, 1e-3f);
            Assert.AreEqual(246f, DockingPlan.StripTop);
            Assert.AreEqual(278f, DockingPlan.StripBottom);
            Assert.AreEqual(200f, DockingPlan.StationBase);
            Assert.AreEqual(128f, DockingPlan.StationRx);
            Assert.AreEqual(30f, DockingPlan.StationRy);
            Assert.AreEqual(24f, DockingPlan.StationStep);
        }

        [Test]
        public void OnA16By9ScreenEverythingBelowTheStripShrinksTogether_AndItNeverTouchesTheStrip()
        {
            var p = new DockingPlan(640f, false);
            Assert.Less(p.K, 1f);
            Assert.GreaterOrEqual(p.K, 0.6f);
            Assert.AreEqual(26f * p.K, p.Block, 1e-4f);
            Assert.AreEqual(168f * p.K, p.PortW, 1e-3f);
            Assert.AreEqual(158f * p.K, p.ButtonW, 1e-3f);
            Assert.LessOrEqual(p.ButtonBox(0).Y1, 640f, "los botones entran");
            Assert.LessOrEqual(p.HintCy + 7f, 640f, "y la pista también");
        }

        [Test]
        public void TheScreenFitsInManyPhoneShapes_AndTheStripNeverTouchesTheModuleThePortOrTheButtons()
        {
            foreach (float h in new[] { 640f, 700f, 720f, 780f, 800f, 900f, 1000f })
                foreach (bool guided in new[] { false, true })
                {
                    var p = new DockingPlan(h, guided);
                    string at = " (alto " + h + ", guiada " + guided + ", escala " + p.K.ToString("0.00") + ")";
                    var strip = p.StripBox;
                    Assert.IsFalse(strip.Intersects(p.ModuleBox), "franja y módulo" + at);
                    Assert.IsFalse(strip.Intersects(p.PortBox), "franja y puerto" + at);
                    Assert.IsFalse(strip.Intersects(p.ButtonBox(0)), "franja y «Encaja»" + at);
                    Assert.IsFalse(strip.Intersects(p.ButtonBox(1)), "franja y «Espejo»" + at);
                    Assert.IsFalse(strip.Intersects(p.FuelBox), "franja y barra de tiempo" + at);
                    Assert.IsFalse(p.ModuleBox.Intersects(p.PortBox), "módulo y puerto" + at);
                    Assert.IsFalse(p.ModuleBox.Intersects(p.FuelBox), "módulo y barra de tiempo" + at);
                    Assert.IsFalse(p.FuelBox.Intersects(p.PortBox), "barra de tiempo y puerto" + at);
                    Assert.IsFalse(p.PortBox.Intersects(p.ButtonsBox), "puerto y botones" + at);
                    Assert.IsFalse(p.ButtonBox(0).Intersects(p.ButtonBox(1)), "los dos botones" + at);
                    float bottom = h - (guided ? DockingPlan.TutorialControls : 0f);
                    Assert.LessOrEqual(p.ButtonsBox.Y1, bottom + 0.01f, "los botones entran" + at);
                    Assert.GreaterOrEqual(p.ModuleBox.X0, 0f, "el módulo entra a lo ancho" + at);
                    Assert.LessOrEqual(p.ModuleBox.X1, DockingPlan.Width, "el módulo entra a lo ancho" + at);
                    Assert.LessOrEqual(p.ButtonsBox.X1, DockingPlan.Width, "los botones entran a lo ancho" + at);
                    Assert.GreaterOrEqual(p.ButtonsBox.X0, 0f, "los botones entran a lo ancho" + at);
                    Assert.GreaterOrEqual(p.K, 0.6f, "la escala no baja de 0,6" + at);
                }
        }

        [Test]
        public void TallerThan780TheExtraSpaceGoesDown_AndTheButtonsFollowTheThumb()
        {
            var a = new DockingPlan(780f, false);
            var b = new DockingPlan(900f, false);
            Assert.AreEqual(a.Block, b.Block, 1e-4f, "las piezas no crecen más");
            Assert.Greater(b.ButtonCy, a.ButtonCy + 90f, "los botones bajan");
            Assert.Greater(b.PortCy, a.PortCy + 50f);
            Assert.Greater(b.ModuleCy, a.ModuleCy + 30f);
            Assert.LessOrEqual(b.ButtonsBox.Y1, 900f);
        }

        // ------------------------------------------------------------------ la estación

        [Test]
        public void TheStationDrawsAtMost5RingsAndSlidesTheOldOnesAway()
        {
            Assert.AreEqual((0, 1), DockingPlan.VisibleRange(0));
            Assert.AreEqual((0, 1), DockingPlan.VisibleRange(7));
            Assert.AreEqual((0, 2), DockingPlan.VisibleRange(8), "al completar un anillo aparece uno nuevo encima");
            Assert.AreEqual((0, 3), DockingPlan.VisibleRange(23));
            Assert.AreEqual((0, 4), DockingPlan.VisibleRange(24));
            Assert.AreEqual((0, 5), DockingPlan.VisibleRange(39));
            Assert.AreEqual((1, 5), DockingPlan.VisibleRange(40), "con 40 módulos ya son 6 anillos: se dibujan los 5 más nuevos");
            Assert.AreEqual((6, 5), DockingPlan.VisibleRange(80), "con 80 módulos hay 11 anillos: se dibujan del 6.º al 10.º");
            // la torre y el anillo más alto quedan bajo el marcador (63 dp)
            float topRing = DockingPlan.RingY(DockingPlan.VisibleRings - 1) - DockingPlan.StationRy - 4.5f;
            Assert.Greater(topRing, DockingPlan.HudBottom, "el anillo más alto no toca el marcador");
            Assert.Less(DockingPlan.StationBase + DockingPlan.StationRy + 12f, DockingPlan.StripTop, "el anillo de abajo no llega a la franja de avisos");
        }

        [Test]
        public void TheEightSlotsOfARingAreHalfInFrontAndHalfBehind_AndInsideTheEllipse()
        {
            int front = 0, back = 0, side = 0;
            for (int k = 0; k < DockingContract.Slots; k++)
            {
                var p = DockingPlan.SlotPoint(200f, k);
                if (p.depth > 0.01f) front++; else if (p.depth < -0.01f) back++; else side++;
                Assert.LessOrEqual(Math.Abs(p.x - DockingPlan.StationCx), DockingPlan.StationRx + 0.01f);
                Assert.LessOrEqual(Math.Abs(p.y - 200f), DockingPlan.StationRy + 0.01f);
            }
            Assert.AreEqual(3, front, "tres adelante");
            Assert.AreEqual(3, back, "tres atrás");
            Assert.AreEqual(2, side, "y uno en cada punta");
            var first = DockingPlan.SlotPoint(200f, 0);
            Assert.AreEqual(180f, first.x, 0.01f, "el primer casillero va al frente, abajo");
            Assert.AreEqual(230f, first.y, 0.01f);
            Assert.AreEqual(1f, first.depth, 1e-5f);
        }

        // ------------------------------------------------------------------ el arte

        private static Cell[] LShape() => new[] { new Cell(0, 0), new Cell(0, 1), new Cell(0, 2), new Cell(1, 0) };

        private static Color32 PixelAt(Color32[] px, int size, Cell[] cells, float gx, float gy)
        {
            DockingSprites.PieceCenter(cells, out float cx, out float cy, out _, out _);
            int x = Mathf.RoundToInt(((gx - cx) / DockingSprites.PieceSideCells + 0.5f) * size - 0.5f);
            int y = Mathf.RoundToInt(((gy - cy) / DockingSprites.PieceSideCells + 0.5f) * size - 0.5f);
            return px[y * size + x];
        }

        [Test]
        public void TheModuleIsFlatCelesteWithInkBorder_AndHasNoBakedLight()
        {
            const int size = 192;
            var cells = LShape();
            var px = DockingSprites.RenderPiece(cells, DockingSprites.PieceLayer.Body, size);
            // el centro de un bloque es celeste, opaco
            var center = PixelAt(px, size, cells, 0f, 2f);
            Assert.AreEqual(255, center.a);
            // fuera de la pieza, transparente
            Assert.AreEqual(0, PixelAt(px, size, cells, 1.5f, 2f).a, "fuera de la pieza no hay nada");
            Assert.AreEqual(0, PixelAt(px, size, cells, 1f, 1.5f).a);
            // el borde de tinta rodea la silueta: justo fuera del bloque (a 0,56 de su centro) hay tinta
            var edge = PixelAt(px, size, cells, -0.56f, 2f);
            Assert.Greater(edge.a, 200);
            Assert.Less(edge.r, 80, "tinta oscura");
            // SIN luz horneada: el celeste del arriba y del abajo de un mismo bloque (lejos de juntas y remaches) es el mismo; la luz la pone el controlador, fija en la pantalla
            var top = PixelAt(px, size, cells, 0f, 2.18f);
            var bottom = PixelAt(px, size, cells, 0f, 1.82f);
            Assert.AreEqual(top.r, bottom.r, 2, "mismo rojo arriba y abajo");
            Assert.AreEqual(top.g, bottom.g, 2);
            Assert.AreEqual(top.b, bottom.b, 2);
            Assert.AreEqual(top.r, center.r, 2, "y mismo celeste en el centro");
            var left = PixelAt(px, size, cells, -0.18f, 2f);
            var right = PixelAt(px, size, cells, 0.18f, 2f);
            Assert.AreEqual(left.g, right.g, 2, "ni de un lado ni del otro");
        }

        [Test]
        public void ThereIsAThinInkJointBetweenNeighborBlocks_AndFourRivetsPerBlock()
        {
            const int size = 384;
            var cells = LShape();
            var px = DockingSprites.RenderPiece(cells, DockingSprites.PieceLayer.Body, size);
            var joint = PixelAt(px, size, cells, 0.1f, 0.5f);                          // entre el bloque (0,0) y el (0,1)
            var flat = PixelAt(px, size, cells, 0.1f, 0.3f);
            Assert.Less(joint.g, flat.g - 15, "la junta es más oscura que el bloque");
            var sideJoint = PixelAt(px, size, cells, 0.5f, 0.2f);                       // el borde entre el bloque (0,0) y el (1,0) también es junta
            Assert.Less(sideJoint.g, flat.g - 15);
            // los remaches (4 simétricos por bloque): oscuros, en (±0,27; ±0,27) del centro de cada bloque
            foreach (var c in new[] { new Cell(0, 2), new Cell(1, 0) })
                foreach (var (dx, dy) in new[] { (-0.27f, -0.27f), (0.27f, -0.27f), (-0.27f, 0.27f), (0.27f, 0.27f) })
                {
                    var rivet = PixelAt(px, size, cells, c.X + dx, c.Y + dy);
                    var plain = PixelAt(px, size, cells, c.X + dx * 0.4f, c.Y + dy * 0.4f);
                    Assert.Less(rivet.g, plain.g - 20, "remache en el bloque " + c.X + "," + c.Y + " (" + dx + "," + dy + ")");
                }
        }

        [Test]
        public void TheHolePortIsTheSamePieceAtTheSameScale_AndItsEdgeIsLargerThanTheHole()
        {
            const int size = 192;
            var cells = LShape();
            var edge = DockingSprites.RenderPiece(cells, DockingSprites.PieceLayer.Edge, size);
            var hole = DockingSprites.RenderPiece(cells, DockingSprites.PieceLayer.Hole, size);
            var body = DockingSprites.RenderPiece(cells, DockingSprites.PieceLayer.Body, size);
            int nEdge = 0, nHole = 0, nBody = 0, holeOutsideEdge = 0, bodyOutsideEdge = 0;
            for (int i = 0; i < edge.Length; i++)
            {
                bool e = edge[i].a > 128, h = hole[i].a > 128, b = body[i].a > 128;
                if (e) nEdge++;
                if (h) nHole++;
                if (b) nBody++;
                if (h && !e) holeOutsideEdge++;
                if (b && !e) bodyOutsideEdge++;
            }
            Assert.Greater(nEdge, nHole, "el borde es más grande que el hueco");
            Assert.AreEqual(0, holeOutsideEdge, "el hueco queda dentro del borde");
            Assert.Greater(nHole, 0);
            // el módulo (con su borde de tinta) y el borde del hueco son la misma silueta: el mismo tamaño
            Assert.AreEqual(nBody, nEdge, nBody * 0.01, "módulo y hueco a la misma escala");
            Assert.AreEqual(0, bodyOutsideEdge);
        }

        [Test]
        public void ThePieceTextureIsSquareAndBakedPerTrial_AndReleased()
        {
            DockingSprites.BakePiece(LShape(), out var body, out var edge, out var hole);
            Assert.IsNotNull(body);
            Assert.AreEqual(DockingSprites.PiecePx, body.texture.width);
            Assert.AreEqual(DockingSprites.PiecePx, body.texture.height);
            DockingSprites.Release(body);
            DockingSprites.Release(edge);
            DockingSprites.Release(hole);
            DockingSprites.Release(null);                                              // soltar nada no rompe
        }

        [Test]
        public void TheEllipseDistanceIsRightOnAndAroundTheRing()
        {
            const float a = 128f, b = 30f;
            Assert.AreEqual(0f, DockingSprites.EllipseDistance(a, 0f, a, b), 0.05f, "en el extremo derecho");
            Assert.AreEqual(0f, DockingSprites.EllipseDistance(0f, -b, a, b), 0.05f, "abajo");
            Assert.AreEqual(0f, DockingSprites.EllipseDistance(0f, b, a, b), 0.05f, "arriba");
            Assert.AreEqual(5f, DockingSprites.EllipseDistance(0f, b + 5f, a, b), 0.2f, "5 dp arriba del anillo");
            Assert.AreEqual(5f, DockingSprites.EllipseDistance(a + 5f, 0f, a, b), 0.3f, "5 dp afuera del extremo");
            Assert.Less(DockingSprites.EllipseDistance(0f, 0f, a, b), 0f, "el centro está adentro");
            Assert.AreEqual(-10f, DockingSprites.EllipseDistance(0f, b - 10f, a, b), 0.2f);
            // un punto cualquiera del anillo
            float t = 0.7f;
            Assert.AreEqual(0f, DockingSprites.EllipseDistance(a * Mathf.Cos(t), b * Mathf.Sin(t), a, b), 0.05f);
        }

        [Test]
        public void TheRingSpritesDrawAStrokeAlongTheEllipse()
        {
            var ring = DockingSprites.RenderEllipseRing(5f);
            int w = (int)(DockingSprites.RingSpriteW * DockingSprites.RingPxPerDp), h = (int)(DockingSprites.RingSpriteH * DockingSprites.RingPxPerDp);
            Assert.AreEqual(w * h, ring.Length);
            // el trazo pasa por las dos puntas de la elipse (a la altura del centro), no por el centro
            int cy = h / 2, cx = w / 2, reach = (int)(DockingSprites.RingRx * DockingSprites.RingPxPerDp);
            Assert.Greater(ring[cy * w + cx + reach].a, 128, "la punta derecha");
            Assert.Greater(ring[cy * w + cx - reach].a, 128, "la punta izquierda");
            Assert.AreEqual(0, ring[cy * w + cx].a, "el centro del anillo está vacío");
            int on = 0;
            foreach (var p in ring) if (p.a > 128) on++;
            Assert.Greater(on, 800, "se dibuja el trazo");
        }

        [Test]
        public void TheButtonsThePanelAndTheIconsAreDrawn()
        {
            var panel = DockingSprites.RenderPortPanel(256);
            Assert.AreEqual(256 * 256, panel.Length);
            Assert.AreEqual(255, panel[128 * 256 + 128].a, "el panel es opaco al centro");
            Assert.AreEqual(0, panel[2 * 256 + 2].a, "y transparente en la esquina");
            var fit = DockingSprites.RenderButton(256, DockingSprites.LimeColor);
            var mirror = DockingSprites.RenderButton(256, DockingSprites.GrapeColor);
            Assert.AreNotEqual(fit[128 * 256 + 128], mirror[128 * 256 + 128], "uno es lima y el otro uva");
            Assert.Greater(fit[128 * 256 + 128].g, fit[128 * 256 + 128].b, "«Encaja» es lima (más verde que azul)");
            Assert.Greater(mirror[128 * 256 + 128].b, mirror[128 * 256 + 128].g, "«Espejo» es uva (más azul que verde)");
            foreach (var icon in new[] { DockingSprites.RenderFitIcon(128), DockingSprites.RenderMirrorIcon(128) })
            {
                int on = 0;
                foreach (var p in icon) if (p.a > 128) on++;
                Assert.Greater(on, 300, "el ícono se dibuja");
            }
            Assert.AreEqual(36f, DockingSprites.SlotSide);
            foreach (var slot in new[] { DockingSprites.RenderSlot(64, DockingSprites.ModuleColor), DockingSprites.RenderSlot(64, DockingSprites.LimeColor), DockingSprites.RenderSlotEmpty(64), DockingSprites.RenderChevron(64), DockingSprites.RenderSpot(64) })
            {
                int on = 0;
                foreach (var p in slot) if (p.a > 64) on++;
                Assert.Greater(on, 40);
            }
        }

        [Test]
        public void NothingInThePortOrTheStationDependsOnTheLevel()
        {
            // la pantalla y el arte no reciben el nivel: dificultad = tiempo, ángulo y tamaño de la pieza, nunca contraste
            foreach (var m in typeof(DockingPlan).GetConstructors())
                foreach (var prm in m.GetParameters())
                    Assert.IsFalse(prm.Name.ToLowerInvariant().Contains("level") || prm.Name.ToLowerInvariant().Contains("nivel"), "DockingPlan recibe el nivel");
            foreach (var m in typeof(DockingSprites).GetMethods(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static))
                foreach (var prm in m.GetParameters())
                    Assert.IsFalse(prm.Name.ToLowerInvariant().Contains("level") || prm.Name.ToLowerInvariant().Contains("nivel"), m.Name + " recibe el nivel");
        }
    }
}
