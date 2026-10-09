using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using NeuroVida.Contracts;

namespace NeuroVida.Games.Piloto.Tests
{
    /// <summary>«Piloto Estelar: la ruta de las balizas» (docs/diseno-piloto.md): los 9 niveles, el inicio suave, las señales por forma y detalle, los puntos, la medida «Tus señales a los mandos», los sectores y las reglas permanentes de Ricardo.</summary>
    public class PilotContractTests
    {
        // ------------------------------------------------------------------ los 9 niveles (§5)

        [Test]
        public void TheLevelTableMatchesTheDesign()
        {
            // nivel: avance, medio ancho, curva, entre señales, exposición (el diseño redondea el medio ancho y el intervalo)
            var rows = new (int level, float speed, float half, float curve, float gap, int expo)[]
            {
                (1, 150f, 104f, 0.10f, 2.2f, 1600),
                (3, 186f, 91.5f, 0.15f, 1.925f, 1362),
                (5, 222f, 79f, 0.20f, 1.65f, 1125),
                (7, 258f, 66.5f, 0.25f, 1.375f, 887),
                (9, 294f, 54f, 0.30f, 1.1f, 650),
            };
            foreach (var r in rows)
            {
                string at = " (nivel " + r.level + ")";
                Assert.AreEqual(r.speed, PilotContract.Speed(r.level, false), 1e-3f, "avance" + at);
                Assert.AreEqual(r.half, PilotContract.HalfWidth(r.level), 1e-3f, "medio ancho" + at);
                Assert.AreEqual(r.curve, PilotContract.Curve(r.level), 1e-4f, "curva" + at);
                Assert.AreEqual(r.gap, PilotContract.Gap(r.level, false), 1e-3f, "entre señales" + at);
                Assert.AreEqual(r.expo, PilotContract.ExposureMs(r.level, false, false), 1, "exposición" + at);
            }
        }

        [Test]
        public void EveryLevelGetsHarderStepByStep()
        {
            for (int l = 2; l <= PilotContract.MaxLevel; l++)
            {
                Assert.Greater(PilotContract.Speed(l, false), PilotContract.Speed(l - 1, false), "avance " + l);
                Assert.Less(PilotContract.HalfWidth(l), PilotContract.HalfWidth(l - 1), "ancho " + l);
                Assert.Greater(PilotContract.Curve(l), PilotContract.Curve(l - 1), "curva " + l);
                Assert.Less(PilotContract.Gap(l, false), PilotContract.Gap(l - 1, false), "intervalo " + l);
                Assert.Less(PilotContract.ExposureMs(l, false, false), PilotContract.ExposureMs(l - 1, false, false), "exposición " + l);
            }
        }

        [Test]
        public void LookalikesAndTheSidesComeInAtTheirLevels()
        {
            Assert.AreEqual(0f, PilotContract.LookalikeChance(1));
            Assert.AreEqual(0f, PilotContract.LookalikeChance(2));
            Assert.AreEqual(0.3f, PilotContract.LookalikeChance(3), 1e-5f);
            Assert.AreEqual(0.3f, PilotContract.LookalikeChance(4), 1e-5f);
            Assert.AreEqual(0.55f, PilotContract.LookalikeChance(5), 1e-5f);
            Assert.AreEqual(0.55f, PilotContract.LookalikeChance(9), 1e-5f);
            Assert.IsFalse(PilotContract.PeripheralLevel(4));
            Assert.IsTrue(PilotContract.PeripheralLevel(5));
        }

        [Test]
        public void TheGentleStart_AndPrecision_AreGentlerButNeverSingleTask()
        {
            // inicio suave (10 s): la ruta usa el nivel de pilotaje − 2 (mínimo 1), las señales salen 1,4 veces más espaciadas y duran 1,25 veces más; las dos tareas siguen juntas
            Assert.AreEqual(1, PilotContract.DriveLevel(1, true));
            Assert.AreEqual(1, PilotContract.DriveLevel(2, true));
            Assert.AreEqual(4, PilotContract.DriveLevel(6, true));
            Assert.AreEqual(6, PilotContract.DriveLevel(6, false));
            Assert.AreEqual(PilotContract.Gap(5, false) * 1.4f, PilotContract.Gap(5, true), 1e-4f);
            Assert.AreEqual(PilotContract.ExposureMs(5, false, false) * 1.25f, PilotContract.ExposureMs(5, false, true), 1f);
            Assert.AreEqual(10f, PilotContract.GentleSeconds);
            // Precisión: velocidad × 0,8 y exposición × 1,3
            Assert.AreEqual(PilotContract.Speed(5, false) * 0.8f, PilotContract.Speed(5, true), 1e-3f);
            Assert.AreEqual(PilotContract.ExposureMs(5, false, false) * 1.3f, PilotContract.ExposureMs(5, true, false), 1f);
        }

        // ------------------------------------------------------------------ la ruta

        [Test]
        public void TheRouteNeverLeavesTheField()
        {
            for (int level = 1; level <= PilotContract.MaxLevel; level++)
            {
                float half = PilotContract.HalfWidth(level), curve = PilotContract.Curve(level);
                for (float p = 0f; p < 30000f; p += 23f)
                {
                    float c = PilotContract.CenterAt(p, curve, half);
                    Assert.GreaterOrEqual(c - half, PilotContract.RouteEdgeMargin - 1e-3f, "nivel " + level + " p " + p);
                    Assert.LessOrEqual(c + half, PilotContract.FieldWidth - PilotContract.RouteEdgeMargin + 1e-3f, "nivel " + level + " p " + p);
                }
            }
        }

        [Test]
        public void InsideUsesHalfWidthMinusTenDp()
        {
            Assert.IsTrue(PilotContract.Inside(180f, 180f, 60f));
            Assert.IsTrue(PilotContract.Inside(229f, 180f, 60f));
            Assert.IsFalse(PilotContract.Inside(231f, 180f, 60f));
            Assert.IsFalse(PilotContract.Inside(129f, 180f, 60f));
        }

        // ------------------------------------------------------------------ señales por forma + detalle (§6)

        [Test]
        public void SignalsAreTheMissionFortyPercent_AndLookalikesKeepTheShape()
        {
            var rng = new Random(11);
            var mission = new PilotMission(SignalShape.Triangle, SignalDetail.Ring);
            int targets = 0, lookalikes = 0, others = 0, peripheral = 0;
            const int n = 20000;
            for (int i = 0; i < n; i++)
            {
                var s = PilotContract.NextSignal(9, mission, rng);
                bool isMission = s.Shape == mission.Shape && s.Detail == mission.Detail;
                Assert.AreEqual(s.IsTarget, isMission, "solo la misión exacta es objetivo");
                if (s.IsTarget) targets++;
                else if (s.Shape == mission.Shape) lookalikes++;        // misma forma con OTRO detalle
                else others++;
                if (s.Peripheral) peripheral++;
            }
            Assert.That(targets / (float)n, Is.InRange(0.37f, 0.43f));
            Assert.That(lookalikes / (float)(n - targets), Is.InRange(0.5f, 0.6f), "55 % de los distractores son parecidos desde el nivel 5");
            Assert.Greater(others, 0);
            Assert.That(peripheral / (float)n, Is.InRange(0.41f, 0.49f), "45 % nacen en los costados desde el nivel 5");
        }

        [Test]
        public void ThereAreNoLookalikesNorSidesAtTheStart()
        {
            var rng = new Random(5);
            var mission = new PilotMission(SignalShape.Hexagon, SignalDetail.Dot);
            for (int i = 0; i < 3000; i++)
            {
                var s = PilotContract.NextSignal(2, mission, rng);
                if (!s.IsTarget) Assert.AreNotEqual(SignalShape.Hexagon, s.Shape, "en los niveles 1 y 2 los distractores tienen otra forma");
                Assert.IsFalse(s.Peripheral);
            }
        }

        [Test]
        public void ANewMissionIsNeverTheSameAsTheLastOne()
        {
            var rng = new Random(2);
            PilotMission? last = null;
            for (int i = 0; i < 2000; i++)
            {
                var m = PilotContract.PickMission(rng, last);
                if (last.HasValue) Assert.IsFalse(m.Equals(last.Value));
                last = m;
            }
        }

        [Test]
        public void EveryShapeKeepsOneColor_AndTheLookalikesShareIt()
        {
            // el color no decide: las parecidas (misma forma, otro detalle) tienen el mismo color, y cada forma tiene SIEMPRE el suyo, distinto de los demás
            var colors = PilotSignalSprites.ShapeColors;
            Assert.AreEqual(PilotContract.ShapeCount, colors.Length);
            for (int i = 0; i < colors.Length; i++)
                for (int j = i + 1; j < colors.Length; j++)
                {
                    float d = Math.Abs(colors[i].r - colors[j].r) + Math.Abs(colors[i].g - colors[j].g) + Math.Abs(colors[i].b - colors[j].b);
                    Assert.Greater(d, 0.3f, PilotContract.ShapeNames[i] + " y " + PilotContract.ShapeNames[j] + " se parecen en color");
                }
        }

        [Test]
        public void TheMissionHasAReadableName()
        {
            Assert.AreEqual("hexágono con punto", PilotContract.MissionName(new PilotMission(SignalShape.Hexagon, SignalDetail.Dot)));
            Assert.AreEqual("triángulo con franja", PilotContract.MissionName(new PilotMission(SignalShape.Triangle, SignalDetail.Stripe)));
            Assert.AreEqual("gota con anillo", PilotContract.MissionName(new PilotMission(SignalShape.Drop, SignalDetail.Ring)));
        }

        // ------------------------------------------------------------------ puntos y medida (§9)

        [Test]
        public void TheSignalMeasureIsHitsMinusFalseAlarmsOverTheMissionSignals()
        {
            Assert.IsNull(PilotContract.SignalScore(5, 0, 7), "con menos de 8 señales de la misión no se muestra");
            Assert.AreEqual(0.6f, PilotContract.SignalScore(7, 1, 10).Value, 1e-5f);
            Assert.AreEqual(1f, PilotContract.SignalScore(8, 0, 8).Value, 1e-5f);
            Assert.AreEqual(0f, PilotContract.SignalScore(2, 9, 10).Value, 1e-5f, "no baja de cero");
            Assert.AreEqual(8, PilotContract.MinTargetsForMeasure);
        }

        [Test]
        public void TheFlightScoreCombinesSignalsAndLane()
        {
            Assert.AreEqual(100, PilotContract.Score(1f, 1f));
            Assert.AreEqual(0, PilotContract.Score(0f, 0f));
            Assert.AreEqual(55, PilotContract.Score(1f, 0f));
            Assert.AreEqual(45, PilotContract.Score(0f, 1f));
            Assert.AreEqual(70, PilotContract.Score(null, 0.7f), "sin medida de señales cuenta solo la ruta");
        }

        [Test]
        public void PointsAreTenPerSignal_DoubleInHyperdrive()
        {
            Assert.AreEqual(10, PilotContract.Points(false));
            Assert.AreEqual(20, PilotContract.Points(true));
        }

        [Test]
        public void WeakerOfReportsTheTaskWithFewerHits()
        {
            Assert.AreEqual((6, 10), PilotContract.WeakerOf(6, 10, 18, 20));
            Assert.AreEqual((7, 10), PilotContract.WeakerOf(18, 20, 7, 10));
            Assert.AreEqual((5, 6), PilotContract.WeakerOf(0, 0, 5, 6));
        }

        // ------------------------------------------------------------------ sectores

        [Test]
        public void ThreeSectorsOfThirtySeconds_OrOneEveryEightSignals()
        {
            Assert.AreEqual(90, PilotContract.FlightSeconds);
            Assert.AreEqual(0, PilotContract.SectorAt(0f));
            Assert.AreEqual(0, PilotContract.SectorAt(29.9f));
            Assert.AreEqual(1, PilotContract.SectorAt(30f));
            Assert.AreEqual(2, PilotContract.SectorAt(60f));
            Assert.AreEqual(2, PilotContract.SectorAt(95f));
            Assert.AreEqual(24, PilotContract.PrecisionSignals);
            Assert.AreEqual(0, PilotContract.SectorAfter(7));
            Assert.AreEqual(1, PilotContract.SectorAfter(8));
            Assert.AreEqual(2, PilotContract.SectorAfter(16));
            Assert.AreEqual(2, PilotContract.SectorAfter(24));
            Assert.AreEqual("Nebulosa azul", PilotContract.SectorName(0));
            Assert.AreEqual("Sector 2 de 3", PilotContract.SectorChip(1));
        }

        // ------------------------------------------------------------------ las reglas permanentes de Ricardo (9-oct)

        [Test]
        public void ThereIsNoAutopilotNorMultitaskCostAnywhere()
        {
            // regla permanente 1: pilotar y señales siempre JUNTAS; nada se mide con una tarea sola. Esta prueba falla si alguien vuelve a poner un piloto automático o un «costo de multitarea».
            foreach (var t in new[] { typeof(PilotContract), typeof(PilotRun), typeof(PilotMetrics), typeof(PilotGameController) })
                foreach (var m in t.GetMembers(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance | BindingFlags.DeclaredOnly))
                {
                    string name = m.Name.ToLowerInvariant();
                    Assert.IsFalse(name.Contains("autopilot") || name.Contains("multitask") || name.Contains("single") && name.Contains("task"), t.Name + "." + m.Name);
                }
            var metrics = typeof(StroopSessionMetrics).GetFields();
            Assert.IsFalse(metrics.Any(f => f.Name == "multitask_cost"), "la telemetría ya no lleva el costo de multitarea");
        }

        [Test]
        public void TheGameTextsAvoidTheForbiddenWords()
        {
            var texts = new List<string>();
            foreach (var f in typeof(PilotContract).GetFields(BindingFlags.Public | BindingFlags.Static))
            {
                if (f.FieldType == typeof(string)) texts.Add((string)f.GetValue(null));
                else if (f.FieldType == typeof(string[])) texts.AddRange((string[])f.GetValue(null));
            }
            for (int s = 0; s < 4; s++) texts.Add(PilotContract.SectorHud(s));
            foreach (var shape in Enum.GetValues(typeof(SignalShape)).Cast<SignalShape>())
                foreach (var detail in Enum.GetValues(typeof(SignalDetail)).Cast<SignalDetail>())
                    texts.Add(PilotContract.MissionName(new PilotMission(shape, detail)));
            texts.Add(PilotContract.MeasureValue(0.71f));
            foreach (var t in texts)
                foreach (var banned in new[] { "cognitiv", "entrenamiento", "cerebro" })
                    StringAssert.DoesNotContain(banned, t.ToLowerInvariant());
            StringAssert.Contains("No es un diagnóstico", PilotContract.EndNote2);
            StringAssert.Contains("las dos cosas a la vez", PilotContract.EndNote1);
        }
    }
}
