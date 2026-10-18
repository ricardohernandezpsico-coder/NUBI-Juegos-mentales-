using System;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace NeuroVida.Games.Acoplamiento.Tests
{
    /// <summary>
    /// Acoplamiento «muelle de acoplamiento» (Tarea 68, 10-oct; v3 en la Tarea 118, 17-oct; docs/diseno-acoplamiento.md): las piezas caben en 4 × 4 bloques, la pantalla de 360 × 780 dp (y la de 16:9 que escala; sin la estación, con la fila de 8 luces), los textos del aviso, la revelación y la escultura
    /// de la pantalla final. El arte de la pieza (la lámpara fija en la pantalla) se prueba en <see cref="DockingV3Tests"/>.
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
            Assert.AreEqual("¡Encaja! 3 seguidos", DockingContract.Notice(false, DockingContract.AnswerFits, 3));
            Assert.AreEqual("¡Encaja! 7 seguidos", DockingContract.Notice(false, DockingContract.AnswerFits, 7));
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

        // ------------------------------------------------------------------ la pantalla (v3: sin estación, el bloque de 38 dp y la fila de luces)

        [Test]
        public void OnTheReferenceScreenTheNumbersAreTheOnesOfTheV3Sheet()
        {
            var p = new DockingPlan(780f, false);
            float r = (float)Math.Sqrt(32.0) * 0.5f * 38f;
            Assert.AreEqual(1f, p.K, 1e-5f);
            Assert.AreEqual(0f, p.Extra, 1e-5f);
            Assert.AreEqual(38f, p.Block, 1e-4f, "bloques de 38 dp (hoy 26)");
            Assert.AreEqual(180f, p.ModuleCx, 1e-4f);
            Assert.AreEqual(150f + 8f + r, p.ModuleCy, 1e-3f, "el módulo: 8 dp bajo la franja y a su círculo de giro de 215 dp");
            Assert.AreEqual(2f * r, p.ModuleBox.Width, 0.01f);
            Assert.AreEqual(215f, p.ModuleBox.Width, 0.5f);
            Assert.AreEqual(p.ModuleCy + r + 12f, p.FuelCy, 1e-3f, "la barra de tiempo a 12 dp del círculo");
            Assert.AreEqual(200f, p.FuelW, 1e-4f);
            Assert.AreEqual(12f, p.FuelH, 1e-4f);
            Assert.AreEqual(245.5f, p.PortW, 0.1f, "el panel del puerto de 245 × 214 dp (hoy 168 × 146)");
            Assert.AreEqual(213.6f, p.PortH, 0.1f);
            Assert.AreEqual(p.FuelCy + 14f + p.PortH * 0.5f, p.PortCy, 1e-3f);
            Assert.AreEqual(p.PortCy - 3f, p.HoleCy, 1e-4f, "el hueco, 3 dp más arriba que el centro del panel");
            Assert.AreEqual(p.PortCy + p.PortH * 0.5f + 14f + 38f, p.ButtonCy, 1e-3f);
            Assert.AreEqual(158f, p.ButtonW, 1e-4f);
            Assert.AreEqual(76f, p.ButtonH, 1e-4f, "los botones no cambian: 158 × 76");
            Assert.AreEqual(12f, p.ButtonGap, 1e-4f);
            Assert.AreEqual(p.ButtonCy + 38f + 20f, p.HintCy, 1e-3f);
            Assert.Less(p.HintCy + 8f, 780f, "la pista entra con aire");
            var (lx, ly) = p.ButtonCenter(0);
            var (rx, ry) = p.ButtonCenter(1);
            Assert.AreEqual(180f - 85f, lx, 1e-3f, "«Encaja» a la izquierda: x 16 a 174");
            Assert.AreEqual(180f + 85f, rx, 1e-3f, "«Espejo» a la derecha: x 186 a 344");
            Assert.AreEqual(p.ButtonCy, ly, 1e-3f);
            Assert.AreEqual(p.ButtonCy, ry, 1e-3f);
            Assert.AreEqual(118f, DockingPlan.StripTop);
            Assert.AreEqual(150f, DockingPlan.StripBottom);
            Assert.AreEqual(100f, DockingPlan.LightsCy);
            Assert.AreEqual(8, DockingPlan.Lights);
            Assert.AreEqual(38f, DockingPlan.ReferenceBlock);
        }

        [Test]
        public void OnA16By9ScreenEverythingBelowTheStripShrinksTogether_AndItNeverTouchesTheStrip()
        {
            var p = new DockingPlan(640f, false);
            Assert.Less(p.K, 1f);
            Assert.GreaterOrEqual(p.K, 0.6f);
            Assert.AreEqual(38f * p.K, p.Block, 1e-4f);
            Assert.AreEqual(31.3f, p.Block, 0.1f, "el bloque de 16:9 de la hoja: 31 dp (hoy 20)");
            Assert.AreEqual(6.46f * p.Block, p.PortW, 1e-3f);
            Assert.AreEqual(158f * p.K, p.ButtonW, 1e-3f);
            Assert.LessOrEqual(p.ButtonBox(0).Y1, 640f, "los botones entran");
            Assert.LessOrEqual(p.HintCy + 7f, 640f, "y la pista también");
            Assert.GreaterOrEqual(p.ButtonH, 56f, "y miden 56 dp o más (63 en 16:9)");
        }

        [Test]
        public void TheScreenFitsInManyPhoneShapes_AndNothingOverlapsInTheEightShapesOfTheSheet()
        {
            foreach (float h in new[] { 640f, 700f, 720f, 740f, 780f, 800f, 900f, 1000f })
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
                    Assert.IsFalse(strip.Intersects(p.LightsBox), "franja y fila de luces" + at);
                    Assert.IsFalse(p.LightsBox.Intersects(p.ModuleBox), "fila de luces y módulo" + at);
                    Assert.LessOrEqual(p.LightsBox.Y1, DockingPlan.StripTop, "la fila de luces va sobre la franja" + at);
                    Assert.GreaterOrEqual(p.LightsBox.Y0, DockingPlan.HudBottom, "y bajo el marcador" + at);
                    Assert.IsFalse(p.ModuleBox.Intersects(p.PortBox), "módulo y puerto" + at);
                    Assert.IsFalse(p.ModuleBox.Intersects(p.FuelBox), "módulo y barra de tiempo" + at);
                    Assert.IsFalse(p.FuelBox.Intersects(p.PortBox), "barra de tiempo y puerto" + at);
                    Assert.IsFalse(p.PortBox.Intersects(p.ButtonsBox), "puerto y botones" + at);
                    Assert.IsFalse(p.ButtonBox(0).Intersects(p.ButtonBox(1)), "los dos botones" + at);
                    Assert.IsFalse(p.ButtonsBox.Intersects(p.HintBox), "botones y pista" + at);
                    float bottom = h - (guided ? DockingPlan.TutorialControls : 0f);
                    Assert.LessOrEqual(p.ButtonsBox.Y1, bottom + 0.01f, "los botones entran" + at);
                    Assert.LessOrEqual(p.HintBox.Y1, bottom + 0.01f, "la pista entra" + at);
                    Assert.GreaterOrEqual(p.ModuleBox.X0, 0f, "el módulo entra a lo ancho" + at);
                    Assert.LessOrEqual(p.ModuleBox.X1, DockingPlan.Width, "el módulo entra a lo ancho" + at);
                    Assert.LessOrEqual(p.PortBox.X1, DockingPlan.Width, "el puerto entra a lo ancho" + at);
                    Assert.LessOrEqual(p.ButtonsBox.X1, DockingPlan.Width, "los botones entran a lo ancho" + at);
                    Assert.GreaterOrEqual(p.ButtonsBox.X0, 0f, "los botones entran a lo ancho" + at);
                    Assert.GreaterOrEqual(p.K, 0.6f, "la escala no baja de 0,6" + at);
                    if (!(guided && h < 700f)) Assert.GreaterOrEqual(p.ButtonH, 56f - 1e-3f, "los botones miden 56 dp o más (con el tutorial en 640 la escala llega al piso de 0,6, como hoy)" + at);
                    else Assert.GreaterOrEqual(p.ButtonH, 76f * 0.6f - 1e-3f, "y nunca menos que el piso de la escala" + at);
                    Assert.GreaterOrEqual(p.Block, 22.8f - 1e-3f, "la pieza nunca es más chica que 22,8 dp (0,6 de 38)" + at);
                }
        }

        [Test]
        public void TheLightsRowIsEightLightsCenteredAndTheMedalSitsRightOfIt()
        {
            Assert.AreEqual(180f - 3.5f * 30f, DockingPlan.LightX(0), 1e-3f);
            Assert.AreEqual(180f + 3.5f * 30f, DockingPlan.LightX(7), 1e-3f);
            for (int i = 1; i < DockingPlan.Lights; i++) Assert.AreEqual(30f, DockingPlan.LightX(i) - DockingPlan.LightX(i - 1), 1e-3f, "cada 30 dp");
            Assert.Greater(DockingPlan.Width * 0.5f + DockingPlan.MedalDx - DockingPlan.MedalSprite * 0.5f, DockingPlan.LightX(7) + DockingPlan.LightSprite * 0.5f - 6f, "la medalla no pisa la última luz");
            Assert.LessOrEqual(DockingPlan.Width * 0.5f + DockingPlan.MedalDx + DockingPlan.MedalSprite * 0.5f, DockingPlan.Width, "y entra a lo ancho");
        }

        [Test]
        public void TallerThan780TheExtraSpaceGoesDown_AndTheButtonsFollowTheThumb()
        {
            var a = new DockingPlan(780f, false);
            var b = new DockingPlan(900f, false);
            Assert.AreEqual(a.Block, b.Block, 1e-4f, "las piezas no crecen más");
            Assert.Greater(b.ButtonCy, a.ButtonCy + 90f, "los botones bajan");
            Assert.Greater(b.PortCy, a.PortCy + 50f);
            Assert.Greater(b.ModuleCy, a.ModuleCy + 20f);
            Assert.LessOrEqual(b.ButtonsBox.Y1, 900f);
        }

        // ------------------------------------------------------------------ la escultura de la pantalla final

        [Test]
        public void TheSculptureDrawsAtMostThreeRings_TheNewestOnes_AndTheOldOnesSlideAway()
        {
            Assert.AreEqual((0, 1), DockingPlan.VisibleRange(0));
            Assert.AreEqual((0, 1), DockingPlan.VisibleRange(1));
            Assert.AreEqual((0, 1), DockingPlan.VisibleRange(8), "ocho módulos son un anillo completo");
            Assert.AreEqual((0, 2), DockingPlan.VisibleRange(9));
            Assert.AreEqual((0, 3), DockingPlan.VisibleRange(23), "23 módulos: dos anillos completos y siete del tercero");
            Assert.AreEqual((0, 3), DockingPlan.VisibleRange(24));
            Assert.AreEqual((1, 3), DockingPlan.VisibleRange(25), "con 25 módulos ya son 4 anillos: se dibujan los 3 más nuevos");
            Assert.AreEqual((7, 3), DockingPlan.VisibleRange(80), "con 80 módulos hay 10 anillos: se dibujan del 8.º al 10.º");
            Assert.AreEqual(DockingPlan.SculptureRings, DockingPlan.SculptureRingY.Length);
            // la escultura cabe entre el título de la pantalla final y la tarjeta de resultados (y 250)
            float top = DockingPlan.SculptureRingY[DockingPlan.SculptureRings - 1] - DockingPlan.SculptureRy - 2f * DockingPlan.SculptureBlock;
            float bottom = DockingPlan.SculptureRingY[0] + DockingPlan.SculptureRy + 2f * DockingPlan.SculptureBlock;
            Assert.Greater(top, 80f, "no toca el título");
            Assert.Less(bottom, 270f, "no pasa de la tarjeta (con su sombra)");
        }

        [Test]
        public void TheEightSlotsOfARingAreFrontAndBackOfTheCore_AndInsideTheEllipse()
        {
            int front = 0, back = 0;
            for (int k = 0; k < DockingContract.Slots; k++)
            {
                var p = DockingPlan.SlotPoint(200f, k);
                if (DockingPlan.IsFront(k)) front++; else back++;
                Assert.LessOrEqual(Math.Abs(p.x - DockingPlan.SculptureCx), DockingPlan.SculptureRx + 0.01f);
                Assert.LessOrEqual(Math.Abs(p.y - 200f), DockingPlan.SculptureRy + 0.01f);
            }
            Assert.AreEqual(5, front, "tres adelante y uno en cada costado");
            Assert.AreEqual(3, back, "tres atrás");
            var first = DockingPlan.SlotPoint(200f, 0);
            Assert.AreEqual(180f, first.x, 0.01f, "el primer casillero va al frente, abajo");
            Assert.AreEqual(220f, first.y, 0.01f);
            Assert.AreEqual(1f, first.depth, 1e-5f);
        }
    }
}
