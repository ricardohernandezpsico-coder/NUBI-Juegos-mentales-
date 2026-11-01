using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using NeuroVida.Games.Disparate;
using NeuroVida.Games.Shared;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace NeuroVida.Games.Tests
{
    /// <summary>
    /// Los robots por edad (<see cref="AgeReference"/>, Tarea 204): lo que se vigila es lo que no puede romperse sin que nadie lo note. (1) El ORDEN entre franjas: con la edad, la marca de la referencia no sube (la distancia entre franjas es lo único que los estudios fijan, y la página la promete). (2) Una semilla fija da SIEMPRE la misma tabla.
    /// (3) El JSON que lee la app está bien armado: tramos del ancho de cada juego, la cuenta suma los robots, los cuartiles en orden, la escala y las semillas escritas. (4) Una persona más rápida y más exacta hace más marca que una lenta, en los cuatro juegos (el robot juega la pista real, no un número).
    /// Corren con pocos robots (segundos); la tabla grande sale de <see cref="ReferenceTableRunner"/>.
    /// </summary>
    public class AgeReferenceTests
    {
        private static SentenceBank _bank;
        private static SentenceBank Bank => _bank ?? (_bank = SentenceBank.Load());

        private static readonly AgeReference.Options Small = new AgeReference.Options(250, 3, 20261101, 20261101UL);

        private static readonly Dictionary<string, AgeReference.Table> Tables = new Dictionary<string, AgeReference.Table>();

        private static AgeReference.Table TableOf(string game)
        {
            if (!Tables.TryGetValue(game, out var t)) Tables[game] = t = AgeReference.Build(game, Bank, Small);
            return t;
        }

        // ------------------------------------------------------------------ el orden entre franjas

        private static void AssertOrderedByAge(string game)
        {
            var t = TableOf(game);
            double tol = Math.Max(0.75, 0.3 * t.BinWidth);          // el azar del muestreo: con 250 robots la media se mueve unas décimas
            for (int f = 0; f + 1 < t.Ranges.Length; f++)
                Assert.LessOrEqual(t.Ranges[f + 1].Mean, t.Ranges[f].Mean + tol, game + ": la media de " + t.Ranges[f + 1].Label + " (" + t.Ranges[f + 1].Mean.ToString("0.0", CultureInfo.InvariantCulture) + ") no debe pasar a la de " + t.Ranges[f].Label + " (" + t.Ranges[f].Mean.ToString("0.0", CultureInfo.InvariantCulture) + ")");
            double drop = t.Ranges[0].Mean - t.Ranges[t.Ranges.Length - 1].Mean;
            Assert.GreaterOrEqual(drop, t.BinWidth, game + ": de 18 a 29 a 75 o más la marca debe bajar al menos un tramo (bajó " + drop.ToString("0.0", CultureInfo.InvariantCulture) + ")");
            Assert.LessOrEqual(t.Ranges[t.Ranges.Length - 1].P50, t.Ranges[0].P50, game + ": la mitad de 75 o más no puede estar sobre la de 18 a 29");
            Assert.LessOrEqual(t.Ranges[t.Ranges.Length - 1].P75, t.Ranges[0].P75, game + ": el cuartil alto de 75 o más no puede estar sobre el de 18 a 29");
        }

        [Test] public void Stroop_OlderPeopleScoreLower() => AssertOrderedByAge("stroop");
        [Test] public void Disparate_OlderPeopleScoreLower() => AssertOrderedByAge("disparate");
        [Test] public void Radar_OlderPeopleScoreLower() => AssertOrderedByAge("radar");
        [Test] public void Docking_OlderPeopleScoreLower() => AssertOrderedByAge("acoplamiento");

        // ------------------------------------------------------------------ una semilla fija, la misma tabla

        [Test]
        public void TheSameSeedGivesTheSameTable()
        {
            var o = new AgeReference.Options(40, 3, 20261101, 7UL);
            foreach (var g in TimeTrial.GameIds)
            {
                string a = AgeReference.ToJson(AgeReference.Build(g, Bank, o), "2026-11-01");
                string b = AgeReference.ToJson(AgeReference.Build(g, Bank, o), "2026-11-01");
                Assert.AreEqual(a, b, g + ": la misma semilla debe dar la misma tabla");
                string c = AgeReference.ToJson(AgeReference.Build(g, Bank, new AgeReference.Options(40, 3, 20261101, 8UL)), "2026-11-01");
                Assert.AreNotEqual(a, c, g + ": otra semilla debe dar otra tabla (si no, la semilla no hace nada)");
            }
        }

        [Test]
        public void TheScaleMovesTheTableAndIsWrittenInTheJson()
        {
            var o = new AgeReference.Options(120, 3, 20261101, 3UL);
            var normal = AgeReference.Build("stroop", Bank, o);
            var slow = AgeReference.Build("stroop", Bank, new AgeReference.Options(120, 3, 20261101, 3UL, 1.3));
            Assert.Less(slow.Ranges[2].Mean, normal.Ranges[2].Mean - 3, "con la respuesta 30 % más lenta la media de 45 a 54 cae varios aciertos (Fable: de 55 a 45 la mitad)");
            Assert.AreEqual(ReferenceScales.For("stroop"), normal.Scale, "la escala por defecto es la de ReferenceScales");
            StringAssert.Contains("\"escala\": 1.3", AgeReference.ToJson(slow, "2026-11-01"));
            foreach (var g in TimeTrial.GameIds) Assert.Greater(ReferenceScales.For(g), 0.0, g);
        }

        // ------------------------------------------------------------------ el JSON

        [Test]
        public void TheJsonIsWellFormedForEachGame()
        {
            foreach (var g in TimeTrial.GameIds)
            {
                var t = TableOf(g);
                string json = AgeReference.ToJson(t, "2026-11-01");
                StringAssert.Contains("\"estimada\": true", json, g);
                StringAssert.Contains("\"juego\": \"" + g + "\"", json);
                StringAssert.Contains("\"version_pista\": " + TimeTrial.TrackVersion(g), json);
                StringAssert.Contains("\"ancho_tramo\": " + AgeReference.BinWidth(g), json);
                StringAssert.Contains("\"tramos\": " + AgeReference.BinCount(g) + ",", json);
                StringAssert.Contains("\"semilla\": 20261101", json);
                StringAssert.Contains("\"robots_por_franja\": " + Small.People, json);
                StringAssert.Contains("\"partidas_por_semana\": 3", json);
                StringAssert.Contains("\"dias_de_pista\": 7", json);
                StringAssert.Contains("\"modelo\": " + AgeReference.ModelVersion, json);
                StringAssert.Contains("\"fuente_modelo\": \"" + AgeReference.Source + "\"", json);
                Assert.AreEqual(g + "_v" + TimeTrial.TrackVersion(g) + ".json", AgeReference.FileName(t));
                Assert.AreEqual(6, t.Ranges.Length, "seis franjas, de 18 a 29 a 75 o más (sin 13 a 17)");
                foreach (var r in t.Ranges)
                {
                    Assert.AreEqual(AgeReference.BinCount(g), r.Bins.Length, g + " " + r.Key);
                    int sum = 0;
                    foreach (int b in r.Bins) sum += b;
                    Assert.AreEqual(Small.People, sum, g + " " + r.Key + ": los tramos suman los robots");
                    Assert.AreEqual(Small.People, r.People);
                    Assert.LessOrEqual(r.P25, r.P50); Assert.LessOrEqual(r.P50, r.P75); Assert.LessOrEqual(r.P75, r.P90);
                    StringAssert.Contains("\"" + r.Key + "\": { \"robots\": " + Small.People, json);
                }
            }
        }

        [Test]
        public void BinWidthsAreTheOnesRicardoChose()
        {
            Assert.AreEqual(3, AgeReference.BinWidth("radar"), "Rescate relámpago: de a 3 aciertos desde la marca por cápsula (Tarea 208)");
            Assert.AreEqual(2, AgeReference.BinWidth("acoplamiento"));
            Assert.AreEqual(5, AgeReference.BinWidth("stroop"));
            Assert.AreEqual(5, AgeReference.BinWidth("disparate"));
            Assert.AreEqual(16, AgeReference.BinCount("radar"), "Rescate relámpago: 16 tramos de a 3 (0 a 47; Tarea 208)");
            Assert.AreEqual(20, AgeReference.BinCount("stroop"), "Tinta o Palabra: 20 tramos de a 5 (0 a 99; Tarea 206)");
            Assert.AreEqual(20, AgeReference.BinCount("acoplamiento"), "Acoplamiento: 20 tramos de a 2 (0 a 39; Tarea 206)");
            Assert.AreEqual(16, AgeReference.BinCount("disparate"), "¿Verdad o disparate?: 16 tramos de a 5 (0 a 79)");
        }

        [Test]
        public void TheMarksOfEveryGameFitTheirBins()
        {
            // Lo que importa (Opus, Tarea 206): que el percentil 90 se alcance CON MARGEN en todas las franjas, es decir, que el último tramo («N o más») junte 5 % o menos de los robots; si junta más, los de arriba se amontonan ahí, el percentil 90 no se alcanza y «Entre estrellas» se queda sin peldaño.
            // Rescate relámpago cumple la regla general desde la Tarea 208 (16 tramos de a 3) y conserva la suya: MENOS del 1 % de 18 a 29 años en el último tramo y NINGÚN robot sobre 45 aciertos (el techo del juego: unas 15 rondas de 7 s, una partida perfecta vale unos 39).
            foreach (var g in TimeTrial.GameIds)
            {
                var t = TableOf(g);
                int top = AgeReference.BinWidth(g) * AgeReference.BinCount(g);
                Assert.Less(t.Ranges[0].P90, top, g + ": el p90 de los más jóvenes cabe dentro de la pista de la tabla");
                if (g == "radar")
                {
                    double young = t.Ranges[0].Bins[AgeReference.BinCount(g) - 1] / (double)t.Ranges[0].People;
                    Assert.Less(young, 0.01, g + ": el último tramo no es donde se amontona la gente (los tramos cubren la marca máxima razonable)");
                    foreach (var r in t.Ranges) Assert.AreEqual(0, r.Bins[AgeReference.BinCount(g) - 1], g + " " + r.Key + ": ningún robot pasa de 45 aciertos (el techo del juego)");
                }
                foreach (var r in t.Ranges)
                {
                    double share = r.Bins[AgeReference.BinCount(g) - 1] / (double)r.People;
                    Assert.LessOrEqual(share, 0.05, g + " " + r.Key + ": el último tramo junta " + (100 * share).ToString("0.0") + " % de los robots; con más del 5 % el percentil 90 no se alcanza con margen");
                }
            }
        }

        // ------------------------------------------------------------------ el robot juega la pista real

        [Test]
        public void AFasterAndMoreAccuratePersonScoresMoreInEveryGame()
        {
            var fast = new AgeReference.Person(0.8, 0.97, 3.8, false);
            var slow = new AgeReference.Person(1.9, 0.86, 2.2, true);
            foreach (var g in TimeTrial.GameIds)
            {
                double f = 0, s = 0;
                const int days = 20;
                for (int d = 0; d < days; d++)
                {
                    int day = 20261101 + d;
                    f += AgeReference.Play(g, fast, day, new TimeTrial.TrackRandom((ulong)(100 + d)), Bank);
                    s += AgeReference.Play(g, slow, day, new TimeTrial.TrackRandom((ulong)(100 + d)), Bank);
                }
                Assert.Greater(f / days, s / days + 2, g + ": la persona rápida y exacta (" + (f / days).ToString("0.0", CultureInfo.InvariantCulture) + ") debe superar a la lenta (" + (s / days).ToString("0.0", CultureInfo.InvariantCulture) + ")");
            }
        }

        [Test]
        public void ThePlayIsDeterministicForTheSamePersonDayAndSeed()
        {
            var p = AgeReference.Draw(2, 1.0, new TimeTrial.TrackRandom(5UL));
            foreach (var g in TimeTrial.GameIds)
            {
                double a = AgeReference.Play(g, p, 20261103, new TimeTrial.TrackRandom(9UL), Bank);
                double b = AgeReference.Play(g, p, 20261103, new TimeTrial.TrackRandom(9UL), Bank);
                Assert.AreEqual(a, b, g);
                Assert.GreaterOrEqual(a, 0, g + ": la marca nunca es negativa (piso en 0)");
            }
        }

        [Test]
        public void ThreePlayedDaysAreDistinctSortedAndFixedPerPerson()
        {
            var seen = new HashSet<string>();
            for (ulong s = 1; s <= 200; s++)
            {
                var days = AgeReference.PickDays(s, 3);
                Assert.AreEqual(3, days.Length);
                for (int i = 0; i < days.Length; i++)
                {
                    Assert.That(days[i], Is.InRange(0, AgeReference.WeekDays - 1));
                    if (i > 0) Assert.Greater(days[i], days[i - 1], "tres días DISTINTOS y de menor a mayor");
                }
                CollectionAssert.AreEqual(days, AgeReference.PickDays(s, 3), "la misma persona juega siempre los mismos días");
                seen.Add(string.Join(",", days));
            }
            Assert.Greater(seen.Count, 20, "personas distintas juegan días distintos (hay 35 combinaciones posibles)");
            CollectionAssert.AreEqual(new[] { 0, 1, 2, 3, 4, 5, 6 }, AgeReference.PickDays(5UL, 7), "con 7 partidas se juegan todos los días");
            Assert.AreEqual(1, AgeReference.PickDays(5UL, 0).Length, "como mínimo una partida");
        }

        [Test]
        public void MorePlayedGamesRaiseTheBestMark()
        {
            var three = AgeReference.Build("stroop", Bank, new AgeReference.Options(150, 3, 20261101, 11UL));
            var seven = AgeReference.Build("stroop", Bank, new AgeReference.Options(150, 7, 20261101, 11UL));
            for (int f = 0; f < three.Ranges.Length; f++)
                Assert.Greater(seven.Ranges[f].Mean, three.Ranges[f].Mean, "la mejor de 7 partidas supera a la mejor de 3 (" + three.Ranges[f].Label + ")");
        }

        [Test]
        public void TheDayKeysCrossTheMonth()
        {
            CollectionAssert.AreEqual(new[] { 20261101, 20261102, 20261103 }, AgeReference.DayKeys(20261101, 3));
            CollectionAssert.AreEqual(new[] { 20261129, 20261130, 20261201 }, AgeReference.DayKeys(20261129, 3));
        }

        // ------------------------------------------------------------------ Rescate relámpago por cápsula (Tarea 208)

        [Test]
        public void TheRadarRobotNeverMarksMoreTypesThanThereWereCapsules()
        {
            var rng = new TimeTrial.TrackRandom(12345UL);
            int checkedRounds = 0;
            for (int f = 0; f < AgeReference.RangeKeys.Length; f++)
                for (int k = 0; k < 40; k++)
                {
                    var person = AgeReference.Draw(f, 1.0, rng);
                    foreach (int n in new[] { 2, 3, 4 })
                        foreach (int rocks in new[] { 0, 1, 2 })
                            foreach (double ms in new[] { 100.0, 280.0, 800.0 })
                            {
                                AgeReference.RadarRoundMarks(person, n, rocks, ms, rng, out int hits, out int extras);
                                Assert.GreaterOrEqual(hits, 0); Assert.GreaterOrEqual(extras, 0);
                                Assert.LessOrEqual(hits + extras, n, "marcó " + (hits + extras) + " tipos con " + n + " cápsulas (el tablero no deja pasar de n)");
                                Assert.LessOrEqual(hits, n);
                                checkedRounds++;
                            }
                }
            Assert.Greater(checkedRounds, 4000);
        }

        [Test]
        public void TheRadarRobotMisreadsAndLapsesAsOftenAsTheModelSays()
        {
            // una persona que lo ve todo (capacidad 4,5, lenta 1,0) con 3 cápsulas y 1000 ms: sin lapso ni error nombra bien 3 de 3; con el modelo, ~17,5 % de las rondas salen en 0 vistas (lapso) y ~5 % de lo visto sale mal
            var rng = new TimeTrial.TrackRandom(777UL);
            var person = new AgeReference.Person(1.0, 0.95, 4.5, false);
            int rounds = 20000, perfect = 0; double hitsSum = 0;
            for (int i = 0; i < rounds; i++)
            {
                AgeReference.RadarRoundMarks(person, 3, 0, 1000.0, rng, out int hits, out int extras);
                hitsSum += hits;
                if (hits == 3 && extras == 0) perfect++;
            }
            double perfectShare = perfect / (double)rounds;
            // perfecta = sin lapso (0,825) y sin error de nombre en las 3 (0,95³ = 0,857) ≈ 0,707, más lo poco que acierta adivinando de las rondas perdidas: ~0,71
            Assert.AreEqual(0.710, perfectShare, 0.025, "la proporción de rondas perfectas debe salir de LAPSE e IDERR");
        }

        [Test]
        public void TheRadarTableHasThe21To29MediansOfTheSimulation_InOrderByAge()
        {
            var t = TableOf("radar");
            int[] expected = { 29, 28, 27, 26, 24, 21 };
            for (int f = 0; f < expected.Length; f++)
                Assert.AreEqual(expected[f], t.Ranges[f].P50, 2, "la mitad de " + t.Ranges[f].Label + " (la simulación de Fable da " + expected[f] + " ± 2)");
            for (int f = 0; f + 1 < expected.Length; f++) Assert.LessOrEqual(t.Ranges[f + 1].P50, t.Ranges[f].P50, "el orden por edad");
            Assert.Greater(t.Ranges[0].P50 - t.Ranges[5].P50, 4, "de 18 a 29 a 75 o más la mitad baja varios aciertos (Fable: 8)");
        }

        [Test]
        public void TheOldRuleWithTheFivePeopleGivesAMedianNear50()
        {
            // el ajuste de LAPSE e IDERR: con la regla VIEJA (rondas logradas menos rondas fallidas) y los 5 de la primera semana (P1 9, P2 7, P3 4, P4 14, P5 14), la mediana de sus percentiles queda cerca de 50 (Fable: 51 con 2.000 robots)
            var options = new AgeReference.Options(1000, 3, 20261101, 20261101UL, 0.0, legacyRadarMark: true);
            var legacy = AgeReference.Build("radar", Bank, options);
            (int range, int mark)[] people = { (1, 9), (0, 7), (3, 4), (1, 14), (2, 14) };      // 30-44, 18-29, 55-64, 30-44, 45-54
            var pcts = new List<int>();
            foreach (var (range, mark) in people)
            {
                var marks = legacy.Ranges[range].Marks;
                int below = 0;
                foreach (int m in marks) if (m < mark) below++;
                pcts.Add(System.Math.Min(99, 100 * below / marks.Length));
            }
            pcts.Sort();
            Assert.That(pcts[2], Is.InRange(44, 56), "la mediana de los percentiles con la regla vieja: " + string.Join(" · ", pcts));
        }

        [Test]
        public void TheParametersAreTheOnesOfTheDesignTable()
        {
            // la tabla de la §2.2 de Fable, tal cual: si alguien la cambia, que sea a propósito (y suba AgeReference.ModelVersion)
            CollectionAssert.AreEqual(new[] { 1.00, 1.06, 1.14, 1.24, 1.40, 1.62 }, AgeReference.Slowness);
            CollectionAssert.AreEqual(new[] { 0.18, 0.19, 0.20, 0.22, 0.25, 0.28 }, AgeReference.Dispersion);
            CollectionAssert.AreEqual(new[] { 0.95, 0.95, 0.94, 0.93, 0.91, 0.89 }, AgeReference.Accuracy);
            CollectionAssert.AreEqual(new[] { 3.6, 3.4, 3.2, 3.0, 2.7, 2.4 }, AgeReference.Objects);
            CollectionAssert.AreEqual(new[] { false, false, false, false, true, true }, AgeReference.Senior);
            CollectionAssert.AreEqual(new[] { "R18_29", "R30_44", "R45_54", "R55_64", "R65_74", "R75_PLUS" }, AgeReference.RangeKeys);
            Assert.AreEqual(1.25, AgeReference.StroopResponse);
            Assert.AreEqual(0.30, AgeReference.DisparatePerWord);
            Assert.AreEqual(2.2, AgeReference.RadarResponse);
            // Rescate por cápsula (Tarea 208, auditoria-fable/rescate-por-capsula.md §3): los dos números nuevos ajustados con la regla vieja a las 5 personas, y la adivinanza de a una
            Assert.AreEqual(0.175, AgeReference.RadarLapse);
            Assert.AreEqual(0.05, AgeReference.RadarNameError);
            Assert.AreEqual(0.70, AgeReference.RadarGuessRate);
            Assert.AreEqual(120, AgeReference.RadarTauMs); Assert.AreEqual(0.12, AgeReference.RadarRockMistake); Assert.AreEqual(0.25, AgeReference.RadarPerCapsule);
            Assert.AreEqual(2, AgeReference.ModelVersion, "el modelo es la versión 2 desde que Rescate cuenta por cápsula");
            StringAssert.Contains("rescate-por-capsula.md", AgeReference.Source);
            StringAssert.Contains("referencia-estimada", AgeReference.Source);
            Assert.AreEqual(1.3, AgeReference.DockingBase);
        }
    }

    /// <summary>
    /// GENERA las tablas de la referencia estimada (Tarea 204): <c>bash tools/verificar-todo.sh --referencia</c> abre Unity en batch y llama a <see cref="Run"/>, que arma los cuatro JSON en <c>app/src/main/assets/referencia/</c> y un informe en <c>unity/test-results/referencia-estimada.txt</c>.
    /// Se vuelve a correr cuando cambia una pista (versión), la escala (<see cref="ReferenceScales"/>) o el modelo (<see cref="AgeReference.ModelVersion"/>). Variables: <c>NUBI_REF_PEOPLE</c> (robots por franja, 2000), <c>NUBI_REF_PARTIDAS</c> (partidas por persona en la semana, 3: días DISTINTOS de los 7),
    /// <c>NUBI_REF_SEED</c> (semilla, 20261101), <c>NUBI_REF_OUT</c> (carpeta de salida) y <c>NUBI_REF_SCALE</c> (una escala para los cuatro juegos, para probar; sin ella manda <see cref="ReferenceScales"/>).
    /// </summary>
    public static class ReferenceTableRunner
    {
        public static void Run()
        {
            try
            {
                var inv = CultureInfo.InvariantCulture;
                int people = EnvInt("NUBI_REF_PEOPLE", 2000);
                int played = EnvInt("NUBI_REF_PARTIDAS", 3);
                ulong seed = (ulong)EnvInt("NUBI_REF_SEED", 20261101);
                double scale = double.TryParse(Environment.GetEnvironmentVariable("NUBI_REF_SCALE"), NumberStyles.Float, inv, out var s) ? s : 0.0;
                string repo = Path.GetFullPath(Path.Combine(Application.dataPath, "..", "..", ".."));
                string outDir = Environment.GetEnvironmentVariable("NUBI_REF_OUT");
                if (string.IsNullOrEmpty(outDir)) outDir = Path.Combine(repo, "app", "src", "main", "assets", "referencia");
                Directory.CreateDirectory(outDir);
                var bank = SentenceBank.Load();
                var options = new AgeReference.Options(people, played, 20261101, seed, scale);
                string date = DateTime.Now.ToString("yyyy-MM-dd", inv);
                var report = new StringBuilder();
                report.AppendLine("Referencia estimada del Contrarreloj (Tarea 204): robots por edad sobre la pista real. Modelo " + AgeReference.ModelVersion + ", generada el " + date + ".");
                report.AppendLine(people + " robots por franja × " + options.Played + " partidas por semana en días distintos de los " + AgeReference.WeekDays + " de la semana (de cada uno, su mejor marca), semilla " + seed + ". NO son personas.");
                report.AppendLine();
                var clock = System.Diagnostics.Stopwatch.StartNew();
                foreach (var g in TimeTrial.GameIds)
                {
                    var t = AgeReference.Build(g, bank, options);
                    File.WriteAllText(Path.Combine(outDir, AgeReference.FileName(t)), AgeReference.ToJson(t, date), new UTF8Encoding(false));
                    report.AppendLine(AgeReference.Describe(t));
                    Debug.Log("[Referencia] " + g + " listo (" + (clock.ElapsedMilliseconds / 1000) + " s)");
                }
                report.AppendLine("Tiempo: " + (clock.ElapsedMilliseconds / 1000) + " s.");
                string resultsDir = Path.Combine(repo, "unity", "test-results");
                Directory.CreateDirectory(resultsDir);
                File.WriteAllText(Path.Combine(resultsDir, "referencia-estimada.txt"), report.ToString(), new UTF8Encoding(false));
                Debug.Log("[Referencia]\n" + report);
                EditorApplication.Exit(0);
            }
            catch (Exception e)
            {
                Debug.LogError("[Referencia] falló: " + e);
                EditorApplication.Exit(1);
            }
        }

        /// <summary>
        /// BARRIDO de escalas para calibrar <see cref="ReferenceScales"/> (Tarea 206): arma las tablas de los juegos pedidos con VARIAS escalas en una sola corrida de Unity y deja cada JSON en <c>&lt;NUBI_REF_OUT&gt;/e&lt;escala&gt;/</c> (NUNCA en los assets de la app). El resto es idéntico a <see cref="Run"/>: mismo modelo, misma semilla, mismos robots y partidas;
        /// solo cambia la escala. Variables: <c>NUBI_REF_SWEEP</c> (<c>desde:hasta:paso</c>, por ejemplo <c>0.60:1.00:0.01</c>), <c>NUBI_REF_GAMES</c> (ids separados por coma; sin ella, los cuatro), <c>NUBI_REF_OUT</c> (obligatoria) y las de <see cref="Run"/>.
        /// Llamar:  <c>Unity -batchmode -nographics -quit -projectPath … -executeMethod NeuroVida.Games.Tests.ReferenceTableRunner.Sweep</c>.
        /// </summary>
        public static void Sweep()
        {
            try
            {
                var inv = CultureInfo.InvariantCulture;
                int people = EnvInt("NUBI_REF_PEOPLE", 2000);
                int played = EnvInt("NUBI_REF_PARTIDAS", 3);
                ulong seed = (ulong)EnvInt("NUBI_REF_SEED", 20261101);
                string outRoot = Environment.GetEnvironmentVariable("NUBI_REF_OUT");
                if (string.IsNullOrEmpty(outRoot)) throw new ArgumentException("NUBI_REF_OUT es obligatoria en el barrido: nunca se escribe en los assets de la app");
                var parts = (Environment.GetEnvironmentVariable("NUBI_REF_SWEEP") ?? "0.60:1.00:0.01").Split(':');
                if (parts.Length != 3) throw new ArgumentException("NUBI_REF_SWEEP = desde:hasta:paso");
                // en centésimas enteras, para que 0,70 + 0,01 × 3 no dé 0,7300000001
                long from = (long)Math.Round(double.Parse(parts[0], inv) * 100), to = (long)Math.Round(double.Parse(parts[1], inv) * 100), step = Math.Max(1, (long)Math.Round(double.Parse(parts[2], inv) * 100));
                var wanted = (Environment.GetEnvironmentVariable("NUBI_REF_GAMES") ?? "").Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
                var games = new List<string>();
                foreach (var g in TimeTrial.GameIds) if (wanted.Length == 0 || Array.IndexOf(wanted, g) >= 0) games.Add(g);
                if (games.Count == 0) throw new ArgumentException("NUBI_REF_GAMES no nombra ningún juego con Contrarreloj");
                var bank = SentenceBank.Load();
                var clock = System.Diagnostics.Stopwatch.StartNew();
                for (long c = from; c <= to; c += step)
                {
                    double scale = c / 100.0;
                    string dir = Path.Combine(outRoot, "e" + scale.ToString("0.00", inv));
                    Directory.CreateDirectory(dir);
                    var options = new AgeReference.Options(people, played, 20261101, seed, scale);
                    foreach (var g in games)
                    {
                        var t = AgeReference.Build(g, bank, options);
                        File.WriteAllText(Path.Combine(dir, AgeReference.FileName(t)), AgeReference.ToJson(t, DateTime.Now.ToString("yyyy-MM-dd", inv)), new UTF8Encoding(false));
                    }
                    Debug.Log("[Referencia] escala " + scale.ToString("0.00", inv) + " lista (" + (clock.ElapsedMilliseconds / 1000) + " s)");
                }
                Debug.Log("[Referencia] barrido listo: " + games.Count + " juegos × " + ((to - from) / step + 1) + " escalas en " + (clock.ElapsedMilliseconds / 1000) + " s.");
                EditorApplication.Exit(0);
            }
            catch (Exception e)
            {
                Debug.LogError("[Referencia] el barrido falló: " + e);
                EditorApplication.Exit(1);
            }
        }

        private static int EnvInt(string name, int fallback) => int.TryParse(Environment.GetEnvironmentVariable(name), NumberStyles.Integer, CultureInfo.InvariantCulture, out var v) ? v : fallback;
    }
}
