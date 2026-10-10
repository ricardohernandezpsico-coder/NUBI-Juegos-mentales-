using System;
using NUnit.Framework;
using UnityEngine;

namespace NeuroVida.Games.Aterrizaje.Tests
{
    /// <summary>
    /// Aterrizaje Lunar renovado (Tarea 70, 10-oct; docs/diseno-aterrizaje.md): las REGLAS no cambian, el plano de la pantalla de 360 × 780 dp (y la de 16:9 que escala), los textos de los avisos, las cúpulas, los tiempos del resultado y el arte (la luna con cráteres SOLO lejos de la regla; la nave, la bandera y las insignias).
    /// </summary>
    public class LandingV2Tests
    {
        private static LandingTrial Trial(float min, float max, float target, bool unit) => new LandingTrial { Min = min, Max = max, Target = target, Label = target.ToString(), Unit = unit, Level = 3 };

        // ------------------------------------------------------------------ las reglas no cambian

        [Test]
        public void TheRulesOfTodayDidNotChange()
        {
            Assert.AreEqual(12, LandingContract.MaxLevel);
            Assert.AreEqual(120, LandingContract.RetoSeconds);
            Assert.AreEqual(15, LandingContract.PrecisionTrials);
            Assert.AreEqual(8, LandingContract.AssessmentTrials);
            Assert.AreEqual(8, LandingContract.TotalTrials(true));
            Assert.AreEqual(15, LandingContract.TotalTrials(false));
            Assert.AreEqual(0.3f, LandingContract.StepUp);
            Assert.AreEqual(0.05f, LandingContract.HitError, 1e-6f, "acierto si el error es ≤ 5 % del largo");
            Assert.AreEqual(0.012f, LandingContract.BullseyeError, 1e-6f, "diana si es ≤ 1,2 %");
            Assert.AreEqual(6f, LandingContract.DescentSeconds(1, false), 1e-5f);
            Assert.AreEqual(3.5f, LandingContract.DescentSeconds(12, false), 1e-5f);
            Assert.AreEqual(10f, LandingContract.DescentSeconds(7, true), 1e-5f, "Precisión: 10 s");
            Assert.IsTrue(LandingContract.IsHit(0.05f));
            Assert.IsFalse(LandingContract.IsHit(0.0501f));
            Assert.IsTrue(LandingContract.IsBullseye(0.012f));
            Assert.IsFalse(LandingContract.IsBullseye(0.0121f));
        }

        [Test]
        public void EveryLevelStillMakesTheSameKindOfRuler_AndKnowsIfItIsInUnitsOrInPercent()
        {
            var rng = new System.Random(11);
            for (int level = 1; level <= 12; level++)
                for (int i = 0; i < 40; i++)
                {
                    var t = LandingContract.NextTrial(level, rng);
                    Assert.AreEqual(level, t.Level);
                    Assert.GreaterOrEqual(t.Target, t.Min);
                    Assert.LessOrEqual(t.Target, t.Max);
                    bool fractional = level == 7 || level == 11 || (level == 8 && t.MaxLabel == "1");
                    Assert.AreEqual(!fractional, t.Unit, "nivel " + level + ": las reglas de enteros y porcentajes dicen la distancia en unidades; las de fracciones y decimales, en %");
                }
        }

        [Test]
        public void TheFlagSaysTheResultOfASum()
        {
            var rng = new System.Random(5);
            for (int i = 0; i < 30; i++)
            {
                var t = LandingContract.NextTrial(10, rng);
                Assert.IsTrue(t.Label.Contains(" + "), "la misión es la suma: " + t.Label);
                Assert.AreEqual(((int)t.Target).ToString(), t.FlagText, "la bandera dice el RESULTADO");
                Assert.AreNotEqual(t.Label, t.FlagText);
            }
            var plain = LandingContract.NextTrial(3, rng);
            Assert.AreEqual(plain.Label, plain.FlagText, "en los demás niveles la bandera dice el mismo número");
            var guided = LandingContract.GuidedTrial(rng);
            Assert.AreEqual(guided.Label, guided.FlagText);
        }

        // ------------------------------------------------------------------ los avisos

        [Test]
        public void TheDistanceIsSaidInUnitsOrInPercent_AndNeverAsZero()
        {
            Assert.AreEqual("a 3 del blanco", LandingContract.DistanceText(Trial(0, 100, 37, true), 40f));
            Assert.AreEqual("a 0,3 del blanco", LandingContract.DistanceText(Trial(0, 10, 4, true), 4.3f), "en una regla corta lleva decimales");
            Assert.AreEqual("a 0,5 del blanco", LandingContract.DistanceText(Trial(0, 20, 14, true), 14.5f));
            Assert.AreEqual("a 5 % del blanco", LandingContract.DistanceText(Trial(0, 1, 0.5f, false), 0.55f), "fracciones: en % de la regla");
            Assert.AreEqual("a 1 % del blanco", LandingContract.DistanceText(Trial(0, 100, 50, false), 50.3f), "nunca menos de 1 %");
            Assert.AreEqual("a 24 del blanco", LandingContract.DistanceText(Trial(0, 1000, 600, true), 624f));
        }

        [Test]
        public void TheNoticesAreTheOnesOfTheTable()
        {
            var t = Trial(0, 100, 37, true);
            Assert.AreEqual("¡Diana lunar!", LandingContract.Notice(t, 37.4f, 1));
            Assert.AreEqual("¡Diana lunar!", LandingContract.Notice(t, 37f, 9), "la diana no lleva racha");
            Assert.AreEqual("¡Justo! a 3 del blanco", LandingContract.Notice(t, 40f, 1));
            Assert.AreEqual("¡Justo! a 3 del blanco", LandingContract.Notice(t, 40f, 2));
            Assert.AreEqual("¡Justo! a 3 del blanco · Racha ×3", LandingContract.Notice(t, 40f, 3));
            Assert.AreEqual("¡Justo! a 4 del blanco · Racha ×7", LandingContract.Notice(t, 33f, 7));
            Assert.AreEqual("Cerca: a 23 del blanco", LandingContract.Notice(t, 60f, 0));
            Assert.AreEqual("Cerca: a 6 % del blanco", LandingContract.Notice(Trial(0, 1, 0.5f, false), 0.56f, 4), "lejos no lleva racha");
            Assert.AreEqual(LandingContract.Outcome.Bull, LandingContract.OutcomeOf(0.005f));
            Assert.AreEqual(LandingContract.Outcome.Hit, LandingContract.OutcomeOf(0.03f));
            Assert.AreEqual(LandingContract.Outcome.Far, LandingContract.OutcomeOf(0.2f));
            foreach (var bad in new[] { "cognitiv", "entrenamiento", "cerebro" })
                foreach (var text in new[] { LandingContract.Hint, LandingContract.BullNotice, LandingContract.DomeNotice, LandingContract.EndTag, LandingContract.EndBoxTitle, LandingContract.EndEstimate(88f), LandingContract.EndAnalogy(88f), LandingContract.Notice(t, 60f, 0) })
                    Assert.IsFalse(text.ToLowerInvariant().Contains(bad), "«" + text + "» usa «" + bad + "»");
        }

        [Test]
        public void EveryFifthJustLandingBuildsADome_AndTheMarkerCountsTowardTheNext()
        {
            for (int hits = 0; hits <= 24; hits++)
            {
                Assert.AreEqual(hits / 5, LandingContract.Domes(hits), "cúpulas con " + hits);
                Assert.AreEqual(hits % 5, LandingContract.ToNextDome(hits), "puntos con " + hits);
                Assert.AreEqual(hits > 0 && hits % 5 == 0, LandingContract.EarnsDome(hits), "¿se arma una con el justo número " + hits + "?");
            }
            Assert.AreEqual(5, LandingContract.DomeEvery);
        }

        [Test]
        public void TheResultTimesAreTheOnesOfTheDesign()
        {
            Assert.AreEqual(320, LandingContract.DropMs);
            Assert.AreEqual(300, LandingContract.LineDelayMs);
            Assert.AreEqual(300, LandingContract.LineMs);
            Assert.AreEqual(380, LandingContract.FlagDelayMs);
            Assert.AreEqual(420, LandingContract.FlagMs);
            Assert.AreEqual(620, LandingContract.NoticeDelayMs);
            Assert.AreEqual(900, LandingContract.DomeNoticeDelayMs);
            Assert.AreEqual(2300, LandingContract.ResultHoldMs);
            Assert.AreEqual(80f, LandingContract.FlagHeight);
            Assert.AreEqual(3, LandingContract.HintTrials);
        }

        [Test]
        public void TheEndTextsAreSingularWhenTheyShouldBe()
        {
            Assert.AreEqual("12 aterrizajes justos de 15", LandingContract.EndTitle(12, 15));
            Assert.AreEqual("1 aterrizaje justo de 8", LandingContract.EndTitle(1, 8));
            Assert.AreEqual("0 aterrizajes justos de 8", LandingContract.EndTitle(0, 8));
            Assert.AreEqual("11 % de la regla", LandingContract.EndEstimate(10.6f));
            Assert.AreEqual("1 % de la regla", LandingContract.EndEstimate(0.2f), "nunca menos de 1");
            Assert.AreEqual("—", LandingContract.EndEstimate(-1f));
            Assert.AreEqual("Tu distancia promedio al lugar justo", LandingContract.EndBoxTitle, "el rótulo dice de qué es la cifra (igual que la app)");
            Assert.AreEqual("Como quedar a 11 en una regla de 0 a 100.", LandingContract.EndAnalogy(10.6f), "la unidad puesta en algo que se imagina, con el mismo N de la cifra");
            Assert.AreEqual("Como quedar a 88 en una regla de 0 a 100.", LandingContract.EndAnalogy(88.2f), "un N de dos cifras");
            Assert.AreEqual("Como quedar a 1 en una regla de 0 a 100.", LandingContract.EndAnalogy(0.2f), "nunca menos de 1");
            Assert.AreEqual("", LandingContract.EndAnalogy(-1f), "sin aterrizajes no hay línea");
            Assert.AreEqual("1 diana lunar · racha mayor — · 1 cúpula", LandingContract.EndSummary(1, 0, 1));
            Assert.AreEqual("0 dianas lunares · racha mayor ×4 · 2 cúpulas", LandingContract.EndSummary(0, 4, 2));
            Assert.AreEqual("2 dianas lunares · racha mayor — · 0 cúpulas", LandingContract.EndSummary(2, 1, 0), "la racha de 1 no se nombra");
            Assert.AreEqual("Aterrizaje 3 de 15", LandingContract.RoundChip(3, 15));
            Assert.AreEqual("Aterrizaje 3", LandingContract.RoundChip(3, 0));
            Assert.AreEqual("Aterrizaje 15 de 15", LandingContract.RoundChip(16, 15));
            Assert.AreEqual("Racha ×5", LandingContract.StreakChip(5));
        }

        // ------------------------------------------------------------------ la pantalla

        [Test]
        public void OnTheReferenceScreenTheNumbersAreTheOnesOfTheSketch()
        {
            var p = new LandingPlan(780f, false);
            Assert.AreEqual(604f, p.RulerY, 1e-3f, "la regla en y 604");
            Assert.AreEqual(554f, p.RestY, 1e-3f, "la nave se posa 50 dp sobre la regla");
            Assert.AreEqual(748f, p.HintY, 1e-3f, "la instrucción en y 748");
            Assert.AreEqual(340f, p.NoticeSmallY, 1e-3f, "el aviso chico en y 340");
            Assert.AreEqual(300f, p.NoticeBigY, 1e-3f, "el aviso grande en y 300");
            Assert.AreEqual(318f, p.EarthX);
            Assert.AreEqual(300f, p.EarthY, 1e-3f);
            Assert.AreEqual(58f, p.EarthR);
            Assert.AreEqual(36f, LandingPlan.RulerX0);
            Assert.AreEqual(324f, LandingPlan.RulerX1);
            Assert.AreEqual(20f, LandingPlan.RulerH);
            Assert.AreEqual(196f, LandingPlan.StartY);
            Assert.AreEqual(160f, LandingPlan.TouchTopY);
            Assert.AreEqual(36f, LandingPlan.FracX(0f));
            Assert.AreEqual(324f, LandingPlan.FracX(1f));
            Assert.AreEqual(180f, LandingPlan.FracX(0.5f), 1e-4f);
            Assert.AreEqual(0.5f, LandingPlan.FracOf(180f), 1e-5f);
            Assert.AreEqual(0f, LandingPlan.FracOf(-50f), "no se pasa de los extremos");
            Assert.AreEqual(1f, LandingPlan.FracOf(400f));
        }

        [Test]
        public void TheRulerIsAnchoredToTheBottom_SoAShorterScreenShortensTheFallAndATallerOneLengthensIt()
        {
            var ref780 = new LandingPlan(780f, false);
            var wide = new LandingPlan(800f, false);
            var short16 = new LandingPlan(640f, false);
            var tall = new LandingPlan(900f, false);
            Assert.AreEqual(ref780.RulerY + 20f, wide.RulerY, 1e-3f);
            Assert.AreEqual(ref780.RulerY - 140f, short16.RulerY, 1e-3f);
            Assert.AreEqual(ref780.RulerY + 120f, tall.RulerY, 1e-3f);
            Assert.AreEqual(640f - 32f, short16.HintY, 1e-3f, "la instrucción queda a 32 dp del borde de abajo");
            foreach (float h in new[] { 640f, 700f, 780f, 800f, 900f, 1000f })
                foreach (bool guided in new[] { false, true })
                {
                    var p = new LandingPlan(h, guided);
                    Assert.Greater(p.RestY - LandingPlan.StartY, 100f, "la nave tiene de dónde caer (alto " + h + ", guiada " + guided + ")");
                    Assert.GreaterOrEqual(p.RulerY, LandingPlan.MinRulerY);
                    Assert.LessOrEqual(p.RulerY + LandingPlan.RulerH + 40f, (guided ? h - LandingPlan.TutorialControls : h) + 60f, "la regla y sus números entran");
                }
        }

        [Test]
        public void ANoticeNeverTouchesTheLanderTheRulerTheFlagOrTheMission_InManyPhoneShapes()
        {
            foreach (float h in new[] { 640f, 700f, 720f, 780f, 800f, 900f, 1000f })
                foreach (bool guided in new[] { false, true })
                    foreach (float f in new[] { 0f, 0.1f, 0.25f, 0.5f, 0.75f, 0.9f, 1f })
                    {
                        var p = new LandingPlan(h, guided);
                        float x = LandingPlan.FracX(f);
                        string at = " (alto " + h + ", guiada " + guided + ", x " + x + ")";
                        foreach (bool big in new[] { true, false })
                        {
                            var n = p.NoticeBox(big, LandingPlan.NoticeMaxWidth + 6f);
                            Assert.IsFalse(n.Intersects(p.LanderBox(x)), "aviso y nave" + at);
                            Assert.IsFalse(n.Intersects(p.RulerBox), "aviso y regla" + at);
                            Assert.IsFalse(n.Intersects(p.FlagBox(x)), "aviso y bandera" + at);
                            Assert.IsFalse(n.Intersects(p.MissionBox), "aviso y misión" + at);
                        }
                        Assert.IsFalse(p.NoticeBox(true, 304f).Intersects(p.NoticeBox(false, 304f)), "las dos filas de avisos no se pisan" + at);
                        Assert.IsFalse(p.FlagBox(x).Intersects(p.MissionBox), "la bandera no toca la misión" + at);
                        Assert.GreaterOrEqual(p.NoticeBox(true, 100f).Y0, 160f, "los avisos van bajo el marcador y la misión" + at);
                    }
        }

        [Test]
        public void TheFlagLabelNeverLeavesTheScreen()
        {
            var p = new LandingPlan(780f, false);
            var left = p.FlagBox(LandingPlan.FracX(0f));
            var right = p.FlagBox(LandingPlan.FracX(1f));
            Assert.GreaterOrEqual(left.X0, 0f);
            Assert.LessOrEqual(right.X1, LandingPlan.Width);
        }

        // ------------------------------------------------------------------ el arte

        private static Color32 At(Color32[] px, int w, int h, float x, float yFromTop) => px[(h - 1 - Mathf.Clamp(Mathf.RoundToInt(yFromTop * 1f), 0, h - 1)) * w + Mathf.Clamp(Mathf.RoundToInt(x), 0, w - 1)];

        [Test]
        public void TheLanderIsDrawnWithItsCabinWindowBaseAndLegs()
        {
            const int size = 256;
            var px = LandingSprites.RenderLander(size);
            float k = size / (2f * LandingSprites.LanderUnit);                                  // píxeles por dp del boceto
            // coordenadas del boceto (dp, y hacia abajo desde el centro) → píxel
            Color32 Sample(float dx, float dy) => px[Mathf.RoundToInt((-dy + LandingSprites.LanderUnit) * k - 0.5f) * size + Mathf.RoundToInt((dx + LandingSprites.LanderUnit) * k - 0.5f)];
            var window = Sample(0f, -9f);
            Assert.Greater(window.b, 200, "la ventanilla es celeste");
            Assert.Greater(window.g, 180);
            var cabin = Sample(-10f, -14f);
            Assert.Greater(cabin.r, 200, "la cabina es crema/lavanda clara");
            Assert.AreEqual(255, cabin.a);
            var baseSun = Sample(0f, 10f);
            Assert.Greater(baseSun.r, 220, "la base es sol");
            Assert.Greater(baseSun.g, 150);
            Assert.Less(baseSun.b, 130);
            var leg = Sample(18f, 24f);
            Assert.Less(leg.r, 90, "las patas son de tinta");
            Assert.AreEqual(255, leg.a);
            Assert.AreEqual(0, Sample(-34f, -30f).a, "las esquinas quedan transparentes");
            Assert.AreEqual(0, Sample(34f, -30f).a);
        }

        [Test]
        public void TheFlagPennantIsASunTriangleWithAnInkBorder()
        {
            var px = LandingSprites.RenderPennant();
            int w = (int)(LandingSprites.PennantW * LandingSprites.PennantPxPerDp), h = (int)(LandingSprites.PennantH * LandingSprites.PennantPxPerDp);
            Assert.AreEqual(w * h, px.Length);
            var inside = At(px, w, h, 10f * LandingSprites.PennantPxPerDp, 10f * LandingSprites.PennantPxPerDp);
            Assert.Greater(inside.r, 220, "relleno sol");
            Assert.Greater(inside.g, 150);
            Assert.AreEqual(0, At(px, w, h, 30f * LandingSprites.PennantPxPerDp, 20f * LandingSprites.PennantPxPerDp).a, "fuera del triángulo no hay nada");
        }

        [Test]
        public void TheEarthIsABlueGreenWorldWithAnInkBorder()
        {
            int size = (int)(LandingSprites.EarthSide * LandingSprites.EarthPxPerDp);
            var px = LandingSprites.RenderEarth(size);
            Assert.AreEqual(size * size, px.Length);
            Assert.AreEqual(0, px[0].a, "las esquinas son transparentes");
            var sea = At(px, size, size, size * 0.4219f, size * 0.773f);                 // (-10, +35) dp del centro: océano, sin tierra ni nubes
            Assert.AreEqual(255, sea.a);
            Assert.Greater(sea.b, sea.r, "el océano es azul");
            // el borde de tinta pasa por el radio 58 dp
            int edge = (int)(size * 0.5f - LandingSprites.EarthRadius * LandingSprites.EarthPxPerDp);
            var ink = At(px, size, size, edge + 1f, size * 0.5f);
            Assert.Less(ink.r, 80, "borde de tinta");
        }

        [Test]
        public void TheMoonHasCratersOnlyFarFromTheRuler_SoNothingFixedServesAsAMark()
        {
            var px = LandingSprites.RenderMoon();
            int w = (int)(LandingSprites.MoonW * LandingSprites.MoonPxPerDp), h = (int)(LandingSprites.MoonH * LandingSprites.MoonPxPerDp);
            float pxDp = LandingSprites.MoonPxPerDp;
            // más arriba de 102 dp bajo el borde de la luna (≈ 86 dp bajo la regla) la superficie es lisa: el mismo color de izquierda a derecha, solo cambia con la altura
            for (float y = 12f; y < 100f; y += 4f)
            {
                var a = At(px, w, h, (20f + 90f) * pxDp, y * pxDp);
                var b = At(px, w, h, (20f + 180f) * pxDp, y * pxDp);
                var c = At(px, w, h, (20f + 270f) * pxDp, y * pxDp);
                Assert.LessOrEqual(Mathf.Abs(a.r - b.r) + Mathf.Abs(a.g - b.g) + Mathf.Abs(a.b - b.b), 3, "sin cráteres a " + y + " dp del borde (izquierda y centro)");
                Assert.LessOrEqual(Mathf.Abs(b.r - c.r) + Mathf.Abs(b.g - c.g) + Mathf.Abs(b.b - c.b), 3, "sin cráteres a " + y + " dp del borde (centro y derecha)");
            }
            // más abajo sí hay cráteres, tenues: uno a (250, 154)
            var crater = At(px, w, h, (20f + 250f) * pxDp, 154f * pxDp);
            var plain = At(px, w, h, (20f + 190f) * pxDp, 154f * pxDp);
            Assert.Less(crater.r, plain.r - 5, "el cráter es más oscuro que el suelo");
            Assert.Greater(crater.r, plain.r - 60, "pero tenue");
            // el arco: arriba al centro está el borde de tinta y en las esquinas de arriba no hay luna
            var top = At(px, w, h, (20f + 180f) * pxDp, 1f * pxDp);
            Assert.Less(top.r, 90, "borde de tinta arriba");
            Assert.AreEqual(0, At(px, w, h, 3f, 3f).a, "la esquina de arriba queda fuera de la luna");
        }

        [Test]
        public void TheTwoRangesRepeatEveryFieldWidth_SoTheyCanScrollWithoutAJump()
        {
            Assert.AreEqual(2f * LandingPlan.Width, LandingSprites.RangeW, "el sprite tiene dos campos de ancho: se repite cada 360 dp");
            foreach (bool far in new[] { true, false })
            {
                var px = LandingSprites.RenderRange(far);
                int w = (int)(LandingSprites.RangeW * LandingSprites.RangePxPerDp), h = (int)(LandingSprites.RangeH * LandingSprites.RangePxPerDp);
                Assert.AreEqual(w * h, px.Length);
                // la primera mitad y la segunda son iguales (hasta un píxel de antialias)
                int half = w / 2, differ = 0;
                for (int y = 0; y < h; y += 7)
                    for (int x = 0; x < half; x += 5)
                        if (Mathf.Abs(px[y * w + x].a - px[y * w + x + half].a) > 6) differ++;
                Assert.LessOrEqual(differ, 2, (far ? "la de atrás" : "la de adelante") + " se repite");
                var solid = At(px, w, h, 100f, h - 4f);
                Assert.AreEqual(255, solid.a, "la base es sólida");
            }
        }

        [Test]
        public void TheDomeLightsUpWhenThereIsOne_AndTheBadgesAreNeverOnlyColor()
        {
            var on = LandingSprites.RenderDome(true);
            var off = LandingSprites.RenderDome(false);
            int w = (int)(LandingSprites.DomeW * LandingSprites.DomePxPerDp), h = (int)(LandingSprites.DomeH * LandingSprites.DomePxPerDp);
            var a = At(on, w, h, w * 0.5f, h * 0.45f);
            var b = At(off, w, h, w * 0.5f, h * 0.45f);
            Assert.Greater(a.b, b.b + 30, "la cúpula armada es celeste y la que falta, apagada");
            int size = (int)(LandingSprites.BadgeSide * LandingSprites.BadgePxPerDp);
            var ok = LandingSprites.RenderBadge(true, size);
            var cross = LandingSprites.RenderBadge(false, size);
            int differ = 0;
            for (int i = 0; i < ok.Length; i++) if (Mathf.Abs(ok[i].r - cross[i].r) + Mathf.Abs(ok[i].g - cross[i].g) > 40) differ++;
            Assert.Greater(differ, size * size / 8, "el ✓ y el aspa se distinguen por su forma y por su color");
            // la forma: contar píxeles de tinta del glifo (✓ y aspa dibujan cosas distintas)
            int inkOk = 0, inkCross = 0;
            for (int y = size / 4; y < size * 3 / 4; y++)
                for (int x = size / 4; x < size * 3 / 4; x++)
                {
                    if (ok[y * size + x].r < 60 && ok[y * size + x].a > 200) inkOk++;
                    if (cross[y * size + x].r < 60 && cross[y * size + x].a > 200) inkCross++;
                }
            Assert.Greater(inkOk, 40);
            Assert.Greater(inkCross, 40);
            Assert.AreNotEqual(inkOk, inkCross);
        }

        [Test]
        public void TheArtDoesNotDependOnTheLevel()
        {
            foreach (var m in typeof(LandingPlan).GetConstructors())
                foreach (var prm in m.GetParameters())
                    Assert.IsFalse(prm.Name.ToLowerInvariant().Contains("level") || prm.Name.ToLowerInvariant().Contains("nivel"), "LandingPlan recibe el nivel");
            foreach (var m in typeof(LandingSprites).GetMethods(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static))
                foreach (var prm in m.GetParameters())
                    Assert.IsFalse(prm.Name.ToLowerInvariant().Contains("level") || prm.Name.ToLowerInvariant().Contains("nivel"), m.Name + " recibe el nivel");
        }
    }
}
