using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;

namespace NeuroVida.Games.Radar.Tests
{
    /// <summary>
    /// «Rescate relámpago: qué cápsulas viste» (docs/diseno-rescate.md): los 12 niveles, la ubicación continua al azar (10.000 semillas, 6 objetos), la zona del destello fija, el tablero de orden fijo, el éxito del motor, «Tu captura», la lluvia fija de 300 ms,
    /// el tope de 25 s y las reglas de patentes (responder QUÉ y no DÓNDE).
    /// </summary>
    public class RadarContractTests
    {
        // ------------------------------------------------------------------ los 12 niveles (§5)

        [Test]
        public void TheLadderHasTwelveLevels_AsInTheTable()
        {
            var table = new (int capsules, int ms, int rocks)[]
            {
                (2, 800, 0), (2, 600, 0), (3, 600, 0), (3, 450, 0), (3, 350, 0), (3, 280, 1),
                (4, 280, 1), (4, 220, 1), (4, 180, 2), (4, 150, 2), (4, 120, 2), (4, 100, 2),
            };
            Assert.AreEqual(12, RadarContract.MaxLevel);
            for (int l = 1; l <= 12; l++)
            {
                Assert.AreEqual(table[l - 1].capsules, RadarContract.Capsules(l), "cápsulas, nivel " + l);
                Assert.AreEqual(table[l - 1].ms, RadarContract.ExposureMs(l), "destello, nivel " + l);
                Assert.AreEqual(table[l - 1].rocks, RadarContract.Rocks(l), "rocas, nivel " + l);
            }
            Assert.AreEqual(2, RadarContract.Capsules(0), "un nivel de menos se ajusta al 1");
            Assert.AreEqual(100, RadarContract.ExposureMs(99), "un nivel de más se ajusta al 12");
        }

        [Test]
        public void TheFlashNeverGetsLongerAndTheCountNeverDrops()
        {
            for (int l = 2; l <= 12; l++)
            {
                Assert.LessOrEqual(RadarContract.ExposureMs(l), RadarContract.ExposureMs(l - 1));
                Assert.GreaterOrEqual(RadarContract.Capsules(l), RadarContract.Capsules(l - 1));
                Assert.GreaterOrEqual(RadarContract.Rocks(l), RadarContract.Rocks(l - 1));
            }
        }

        // ------------------------------------------------------------------ ubicación continua al azar (§4)

        [Test]
        public void SixObjectsAlwaysFit_With10000Seeds()
        {
            for (int seed = 0; seed < 10000; seed++)
            {
                var pts = RadarContract.Place(6, new Random(seed));
                Assert.AreEqual(6, pts.Count, "seed " + seed);
                AssertValid(pts, "seed " + seed);
                // el plan B (anillo girado) también es válido
                if (seed % 100 == 0) AssertValid(RadarContract.Ring(6, new Random(seed)), "anillo, seed " + seed);
            }
        }

        private static void AssertValid(List<(float x, float y)> pts, string where)
        {
            for (int i = 0; i < pts.Count; i++)
            {
                float r = (float)Math.Sqrt(pts[i].x * pts[i].x + pts[i].y * pts[i].y);
                Assert.LessOrEqual(r, RadarContract.SpawnRadius + 2f, "dentro del disco, " + where);
                for (int j = i + 1; j < pts.Count; j++)
                {
                    float dx = pts[i].x - pts[j].x, dy = pts[i].y - pts[j].y;
                    Assert.GreaterOrEqual(Math.Sqrt(dx * dx + dy * dy), RadarContract.MinSeparation - 0.01, "distancia mínima, " + where);
                }
            }
        }

        [Test]
        public void ThePositionsAreContinuous_NotOnAFixedGridOrOnRings()
        {
            // 2.000 posiciones sueltas: sus radios cubren todo el disco (no se amontonan en uno o dos anillos) y los ángulos, toda la vuelta
            var rng = new Random(3);
            var radii = new List<double>();
            var octants = new int[8];
            for (int i = 0; i < 1000; i++)
                foreach (var p in RadarContract.Place(2, rng))
                {
                    radii.Add(Math.Sqrt(p.x * p.x + p.y * p.y));
                    double a = Math.Atan2(p.y, p.x) + Math.PI;
                    octants[Math.Min(7, (int)(a / (Math.PI / 4)))]++;
                }
            Assert.Greater(radii.Max(), RadarContract.SpawnRadius * 0.9);
            Assert.Less(radii.Min(), RadarContract.SpawnRadius * 0.1);
            for (int b = 0; b < 4; b++)
                Assert.Greater(radii.Count(r => r >= b * RadarContract.SpawnRadius / 4 && r < (b + 1) * RadarContract.SpawnRadius / 4), 60, "cuartos de radio llenos");
            foreach (int c in octants) Assert.Greater(c, 150, "todas las direcciones");
            // y casi no hay dos posiciones repetidas (no hay lugares fijos)
            var rng2 = new Random(9);
            var seen = new HashSet<(int, int)>();
            for (int i = 0; i < 500; i++) foreach (var p in RadarContract.Place(4, rng2)) seen.Add(((int)(p.x * 10), (int)(p.y * 10)));
            Assert.Greater(seen.Count, 1900);
        }

        [Test]
        public void TheZoneOfTheFlashNeverGrowsWithTheLevel()
        {
            // regla de patentes 2: el radio es fijo; solo cambian la cantidad, la duración y las rocas
            for (int level = 1; level <= RadarContract.MaxLevel; level++)
                for (int seed = 0; seed < 300; seed++)
                {
                    var round = RadarContract.NextRound(level, new Random(seed * 13 + level));
                    foreach (var o in round.Capsules.Concat(round.Rocks))
                        Assert.LessOrEqual(Math.Sqrt(o.X * o.X + o.Y * o.Y), RadarContract.SpawnRadius + 2f, "nivel " + level);
                    Assert.AreEqual(RadarContract.Capsules(level), round.Capsules.Length);
                    Assert.AreEqual(RadarContract.Rocks(level), round.Rocks.Length);
                }
            Assert.AreEqual(128f, RadarContract.RadarRadius);
            Assert.AreEqual(94f, RadarContract.SpawnRadius);
            Assert.AreEqual(66f, RadarContract.MinSeparation);
        }

        [Test]
        public void EveryCapsuleTypeIsDifferentInARound_AndRocksHaveNoType()
        {
            for (int level = 1; level <= RadarContract.MaxLevel; level++)
                for (int seed = 0; seed < 200; seed++)
                {
                    var round = RadarContract.NextRound(level, new Random(seed));
                    Assert.AreEqual(round.Capsules.Length, round.Types().Count, "no se repite ningún tipo");
                    Assert.IsTrue(round.Rocks.All(r => r.IsRock));
                    Assert.IsTrue(round.Capsules.All(c => !c.IsRock));
                }
        }

        // ------------------------------------------------------------------ el tablero: orden fijo (patentes 1 y 3)

        [Test]
        public void TheBoardHasAFixedOrder_ThatNeverDependsOnTheRound()
        {
            Assert.AreEqual(6, RadarContract.BoardOrder.Length);
            CollectionAssert.AreEqual(new[] { CapsuleType.Hexagon, CapsuleType.Drop, CapsuleType.Circle, CapsuleType.Square, CapsuleType.Triangle, CapsuleType.Diamond }, RadarContract.BoardOrder);
            CollectionAssert.AllItemsAreUnique(RadarContract.BoardOrder);
            // el contrato no tiene ninguna función de «lugares»: ni casillas, ni el lugar más cercano, ni cuadrícula, ni sectores
            foreach (var m in typeof(RadarContract).GetMethods(BindingFlags.Public | BindingFlags.Static))
            {
                string name = m.Name.ToLowerInvariant();
                Assert.IsFalse(name.Contains("slot") || name.Contains("nearest") || name.Contains("grid") || name.Contains("sector"), "función de lugares: " + m.Name);
            }
        }

        [Test]
        public void ThereAreSixCapsuleTypesWithFixedNames()
        {
            Assert.AreEqual(RadarContract.TypeCount, Enum.GetValues(typeof(CapsuleType)).Length);
            Assert.AreEqual(new[] { "Hexágono", "Gota", "Círculo", "Cuadrado", "Triángulo", "Rombo" }, RadarContract.TypeNames);
            Assert.AreEqual("Rombo", RadarContract.TypeName(CapsuleType.Diamond));
        }

        // ------------------------------------------------------------------ éxito del motor y ronda perfecta

        [Test]
        public void Needed_IsAllUpToThree_AndAllButOneWithFour()
        {
            Assert.AreEqual(2, RadarContract.Needed(2));
            Assert.AreEqual(3, RadarContract.Needed(3));
            Assert.AreEqual(3, RadarContract.Needed(4));
        }

        [Test]
        public void Success_IsHitsMinusExtrasAtLeastNeeded()
        {
            Assert.IsTrue(RadarContract.Success(2, 2, 0));
            Assert.IsFalse(RadarContract.Success(2, 2, 1), "una de más resta una");
            Assert.IsFalse(RadarContract.Success(2, 1, 0));
            Assert.IsTrue(RadarContract.Success(4, 3, 0));
            Assert.IsTrue(RadarContract.Success(4, 4, 1));
            Assert.IsFalse(RadarContract.Success(4, 3, 1));
            Assert.IsFalse(RadarContract.Success(3, 3, 1));
            Assert.IsTrue(RadarContract.Perfect(4, 4, 0));
            Assert.IsFalse(RadarContract.Perfect(4, 4, 1));
            Assert.IsFalse(RadarContract.Perfect(4, 3, 0));
        }

        [Test]
        public void EvaluateCountsHitsAndExtras_AndRepeatedPicksCountOnce()
        {
            var round = new RadarRound
            {
                Capsules = new[] { new RadarObject { Type = CapsuleType.Hexagon }, new RadarObject { Type = CapsuleType.Square } },
                Rocks = new RadarObject[0]
            };
            var a = RadarContract.Evaluate(round, new[] { CapsuleType.Hexagon, CapsuleType.Hexagon, CapsuleType.Drop, CapsuleType.Square });
            Assert.AreEqual(2, a.Hits);
            Assert.AreEqual(1, a.Extras);
            Assert.IsFalse(RadarContract.Success(round, a));
            Assert.IsTrue(RadarContract.Success(round, RadarContract.Evaluate(round, new[] { CapsuleType.Hexagon, CapsuleType.Square })));
        }

        // ------------------------------------------------------------------ la lluvia (§4) y «Tu captura» (§6)

        [Test]
        public void TheRainIsFourCapsulesWithoutRocksAndAFixedFlashOf300Ms_AtEveryLevel()
        {
            for (int level = 1; level <= 12; level++)
            {
                var rain = RadarContract.RainRound(level, new Random(level));
                Assert.IsTrue(rain.Rain);
                Assert.AreEqual(4, rain.Capsules.Length);
                Assert.AreEqual(0, rain.Rocks.Length);
                Assert.AreEqual(300, rain.ExposureMs, "destello fijo (el boceto usaba max(250, ms del nivel))");
            }
            Assert.IsFalse(RadarContract.NextRound(5, new Random(1)).Rain);
        }

        [Test]
        public void ARainComesEveryFourthRound()
        {
            var rains = Enumerable.Range(0, 12).Where(RadarContract.IsRainRound).ToArray();
            CollectionAssert.AreEqual(new[] { 3, 7, 11 }, rains, "la 4, la 8 y la 12");
            Assert.IsFalse(RadarContract.IsRainRound(-1));
            Assert.AreEqual(12, RadarContract.PrecisionRounds);
            Assert.AreEqual(120, RadarContract.RetoSeconds);
        }

        [Test]
        public void TheCaptureIsTheAverageOfHitsMinusTwiceTheExtras_WithAFloorOfZero()
        {
            Assert.AreEqual(-1f, RadarContract.Capture(new[] { 4 }), "con menos de 2 lluvias no hay medida");
            Assert.AreEqual(3.5f, RadarContract.Capture(new[] { 4, 3 }), 1e-5f);
            Assert.AreEqual(4f, RadarContract.Capture(new[] { 4, 4, 4 }), 1e-5f);
            // cada lluvia NO se corta en 0 (así no se infla el azar): 4 y −2 promedian 1, no 2
            Assert.AreEqual(1f, RadarContract.Capture(new[] { 4, -2 }), 1e-5f);
            Assert.AreEqual(0f, RadarContract.Capture(new[] { -2, -4 }), "el total tiene mínimo 0");
            Assert.AreEqual(2, RadarContract.RainRaw(4, 1), "cuatro bien y una de más: 4 − 2");
            Assert.AreEqual(2, RadarContract.RainRaw(2, 0));
            Assert.AreEqual(-2, RadarContract.RainRaw(2, 2));
        }

        [Test]
        public void GuessingGivesZeroOnAverage_SoTheCaptureIsNotInflatedByChance()
        {
            // 4 cápsulas entre 6 tipos: quien marca 4 al azar acierta 4·(4/6) = 2,67 y se pasa en 1,33: aciertos − 2 × de más = 0
            var rng = new Random(7);
            double sum = 0;
            const int n = 30000;
            for (int i = 0; i < n; i++)
            {
                var round = RadarContract.RainRound(5, rng);
                var types = Enumerable.Range(0, 6).OrderBy(_ => rng.Next()).Take(4).Select(t => (CapsuleType)t).ToArray();
                var a = RadarContract.Evaluate(round, types);
                sum += RadarContract.RainRaw(a.Hits, a.Extras);
            }
            Assert.AreEqual(0.0, sum / n, 0.05);
        }

        // ------------------------------------------------------------------ «Tu vistazo» y puntaje

        [Test]
        public void TheGlanceUsesRealDurationsFromTheFourthNormalRound_WithFiveOrMoreRoundsCounted()
        {
            Assert.AreEqual(-1, RadarContract.GlanceMs(new float[] { 800, 600, 600, 450, 350, 280, 280 }), "con 7 rondas solo cuentan 4: no se muestra");
            Assert.AreEqual(280, RadarContract.GlanceMs(new float[] { 9999, 9999, 9999, 280, 280, 280, 280, 280 }), "las 3 primeras no cuentan");
            // media geométrica de 100 y 400 (tres de cada una)
            int g = RadarContract.GlanceMs(new float[] { 5, 5, 5, 100, 400, 100, 400, 100, 400 });
            Assert.AreEqual((int)Math.Round(Math.Pow(100.0 * 100 * 100 * 400 * 400 * 400, 1.0 / 6.0)), g);
            Assert.AreEqual(3.6f, RadarContract.GlanceLoad(new[] { 2, 2, 2, 3, 3, 4, 4, 4 }), 0.01f);
            Assert.AreEqual(-1f, RadarContract.GlanceLoad(new[] { 2, 2, 2, 3 }));
        }

        [Test]
        public void TheScoreMixesSuccessAndSpeed()
        {
            Assert.AreEqual(100, RadarContract.Score(1f, 100));
            Assert.AreEqual(50, RadarContract.Score(1f, 800), "lo más lento: solo cuenta la mitad de las rondas logradas");
            Assert.AreEqual(80, RadarContract.Score(0.8f, 0), "sin vistazo, solo las rondas logradas");
            Assert.AreEqual(0, RadarContract.Score(0f, 800));
        }

        // ------------------------------------------------------------------ tiempos y textos

        [Test]
        public void TheTimesAreAsInTheDesign()
        {
            Assert.AreEqual(25f, RadarContract.AnswerTimeoutSeconds, "tope de espera de la respuesta");
            Assert.AreEqual(350, RadarContract.MaskMs);
            Assert.AreEqual(1.5f, RadarContract.WatchMinSeconds);
            Assert.AreEqual(3.5f, RadarContract.WatchMaxSeconds);
            Assert.AreEqual(2.3f, RadarContract.RevealSeconds);
        }

        [Test]
        public void TheTextsAreTheDesignOnes_AndAvoidTheForbiddenWords()
        {
            Assert.AreEqual("¿Qué cápsulas viste? (eran 3)", RadarContract.AskMessage(3));
            Assert.AreEqual("¡Todos a salvo!", RadarContract.AllSafe(2));
            Assert.AreEqual("¡Todos a salvo! Racha ×3", RadarContract.AllSafe(3));
            Assert.AreEqual("Rescataste 2 de 3", RadarContract.Partial(2, 3, 0));
            Assert.AreEqual("Rescataste 2 de 3 · 1 no estaba", RadarContract.Partial(2, 3, 1));
            Assert.AreEqual("Rescataste 1 de 4 · 2 no estaban", RadarContract.Partial(1, 4, 2));
            Assert.AreEqual("Ronda 5 de 12", RadarContract.RoundChip(5, 12));
            Assert.AreEqual("Ronda 7", RadarContract.RoundChip(7, 0));
            Assert.AreEqual("3,5 de 4", RadarContract.CaptureValue(3.5f));
            Assert.AreEqual("—", RadarContract.CaptureValue(-1f));
            Assert.AreEqual("1 cápsula a salvo", RadarContract.EndTitle(1));
            Assert.AreEqual("23 cápsulas a salvo", RadarContract.EndTitle(23));
            var texts = new List<string>();
            foreach (var f in typeof(RadarContract).GetFields(BindingFlags.Public | BindingFlags.Static))
            {
                if (f.FieldType == typeof(string)) texts.Add((string)f.GetValue(null));
                else if (f.FieldType == typeof(string[])) texts.AddRange((string[])f.GetValue(null));
            }
            foreach (var t in texts)
                foreach (var banned in new[] { "cognitiv", "entrenamiento", "cerebro", "ufov", "double decision", "eagle eye", "speed match", "astronauta" })
                    StringAssert.DoesNotContain(banned, t.ToLowerInvariant());
            StringAssert.Contains("No es un diagnóstico", RadarContract.EndNote3);
        }
    }
}
