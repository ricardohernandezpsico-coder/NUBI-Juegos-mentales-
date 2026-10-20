using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using NeuroVida.Games.Shared;

namespace NeuroVida.Games.Anagramas.Tests
{
    public class PuntaContractTests
    {
        private static PuntaBank Bank => PuntaBank.Load();

        // ------------------------------------------------------------------ banco

        [Test]
        public void Bank_HasAtLeast400WordsAndEveryLevelHasEnoughForAGame()
        {
            var bank = Bank;
            Assert.GreaterOrEqual(bank.Count, 400);
            for (int level = 1; level <= PuntaContract.MaxLevel; level++)
                Assert.GreaterOrEqual(bank.OfLevel(level).Count, 50, "nivel " + level);
        }

        [Test]
        public void Bank_EveryWordIsWellFormed()
        {
            var bank = Bank;
            foreach (var w in bank.All)
            {
                Assert.IsTrue(w.Tiles.All(c => (c >= 'A' && c <= 'Z') || c == 'Ñ'), "fichas inválidas: " + w.Word + " → " + w.Tiles);
                Assert.AreEqual(w.Word.Length, w.Tiles.Length, w.Word);
                Assert.That(w.Tiles.Length, Is.InRange(4, 10), w.Word);
                Assert.LessOrEqual(w.Clue.Length, 90, w.Word);
                Assert.That(w.Level, Is.InRange(1, PuntaContract.MaxLevel), w.Word);
                Assert.AreEqual(w.Word, w.Word.ToLowerInvariant(), "la palabra va en minúsculas: " + w.Word);
                Assert.AreSame(w, bank.Find(w.Word));
            }
        }

        [Test]
        public void Bank_ClueNeverContainsTheWordItself()
        {
            foreach (var w in Bank.All)
            {
                var root = w.Tiles.Substring(0, Math.Min(4, w.Tiles.Length));
                foreach (var token in w.Clue.ToUpperInvariant().Split(' ', ',', '.', ':', ';', '«', '»', '¡', '!', '¿', '?', '-', '(', ')'))
                {
                    var t = token.Replace('Á', 'A').Replace('É', 'E').Replace('Í', 'I').Replace('Ó', 'O').Replace('Ú', 'U').Replace('Ü', 'U');
                    Assert.IsFalse(t.StartsWith(root), w.Word + ": «" + w.Clue + "»");
                }
            }
        }

        [Test]
        public void Bank_FromJson_RejectsAnEmptyBank_AndFallbackAlwaysWorks()
        {
            Assert.Throws<FormatException>(() => PuntaBank.FromJson("{\"version\":1,\"palabras\":[]}"));
            var fb = PuntaBank.Fallback();
            Assert.GreaterOrEqual(fb.Count, 5);
            for (int level = 1; level <= PuntaContract.MaxLevel; level++) Assert.GreaterOrEqual(fb.OfLevel(level).Count, 1, "nivel " + level);
        }

        [Test]
        public void Bank_FromJson_ReadsTheFieldsAndSkipsBrokenRows()
        {
            var bank = PuntaBank.FromJson("{\"version\":1,\"palabras\":[{\"p\":\"búho\",\"l\":\"BUHO\",\"d\":\"Ave nocturna.\",\"n\":2,\"b\":1,\"z\":3.1,\"c\":\"animal\"}," +
                "{\"p\":\"mal\",\"l\":\"MALO\",\"d\":\"x\",\"n\":2,\"b\":1,\"z\":3,\"c\":\"x\"},{\"p\":\"fuera\",\"l\":\"FUERA\",\"d\":\"x\",\"n\":9,\"b\":1,\"z\":3,\"c\":\"x\"}]}");
            Assert.AreEqual(1, bank.Count);
            var w = bank.Find("búho");
            Assert.AreEqual("BUHO", w.Tiles);
            Assert.AreEqual(2, w.Level);
            Assert.AreEqual("animal", w.Category);
        }

        // ------------------------------------------------------------------ dificultad y fichas

        [Test]
        public void ExtraLetters_AreTwoAtLevelOneAndFourAtLevelFive_SeniorsOneFewer()
        {
            Assert.AreEqual(2, PuntaContract.ExtraLetters(1, false));
            Assert.AreEqual(4, PuntaContract.ExtraLetters(5, false));
            Assert.AreEqual(1, PuntaContract.ExtraLetters(1, true));
            Assert.AreEqual(3, PuntaContract.ExtraLetters(5, true));
            int prev = 0;
            for (int level = 1; level <= PuntaContract.MaxLevel; level++)
            {
                int n = PuntaContract.ExtraLetters(level, false);
                Assert.GreaterOrEqual(n, prev);
                Assert.AreEqual(Math.Max(1, n - 1), PuntaContract.ExtraLetters(level, true));
                prev = n;
            }
        }

        [Test]
        public void BuildTiles_HasEveryLetterOfTheWordPlusTheExtras_NeverAlreadyInOrder()
        {
            var rng = new Random(11);
            foreach (var w in Bank.All.Take(200))
            {
                for (int extra = 1; extra <= 4; extra++)
                {
                    var tiles = PuntaContract.BuildTiles(w.Tiles, extra, rng);
                    Assert.AreEqual(w.Tiles.Length + extra, tiles.Length, w.Word);
                    var pool = new List<char>(tiles);
                    foreach (char c in w.Tiles) Assert.IsTrue(pool.Remove(c), w.Word + ": falta " + c);
                    Assert.AreEqual(extra, pool.Count);
                    foreach (char c in pool) Assert.IsTrue(c >= 'A' && c <= 'Z');
                    Assert.IsFalse(new string(tiles).StartsWith(w.Tiles), w.Word + " ya armada");
                }
            }
        }

        [Test]
        public void BuildTiles_ExtrasAvoidTheLettersOfTheWord_WhenThereAreEnoughOthers()
        {
            var rng = new Random(3);
            for (int i = 0; i < 100; i++)
            {
                var tiles = PuntaContract.BuildTiles("RELOJ", 2, rng);
                var extras = new List<char>(tiles);
                foreach (char c in "RELOJ") extras.Remove(c);
                Assert.AreEqual(2, extras.Count);
                foreach (char c in extras) Assert.IsTrue("RELOJ".IndexOf(c) < 0, "relleno que repite una letra: " + c);
                Assert.AreNotEqual(extras[0], extras[1], "dos rellenos iguales");
            }
        }

        [Test]
        public void OnlyLetters_IsAPermutationOfTheWord_NeverInOrder()
        {
            var rng = new Random(2);
            foreach (var w in Bank.All.Take(200))
            {
                var t = PuntaContract.OnlyLetters(w.Tiles, rng);
                Assert.AreEqual(string.Concat(w.Tiles.OrderBy(c => c)), string.Concat(t.OrderBy(c => c)), w.Word);
                Assert.AreNotEqual(w.Tiles, new string(t), w.Word);
            }
        }

        // ------------------------------------------------------------------ colores, puntos, DDA

        [Test]
        public void TierOf_ShownIsBlue_AloneIsGold_OneOrTwoHelpsSilver_ThreeIsCopper()
        {
            Assert.AreEqual(PuntaTier.Solo, PuntaContract.TierOf(0, false));
            Assert.AreEqual(PuntaTier.Pista, PuntaContract.TierOf(1, false));
            Assert.AreEqual(PuntaTier.Pista, PuntaContract.TierOf(2, false));
            Assert.AreEqual(PuntaTier.Letras, PuntaContract.TierOf(3, false));
            Assert.AreEqual(PuntaTier.Vista, PuntaContract.TierOf(0, true));
            Assert.AreEqual(PuntaTier.Vista, PuntaContract.TierOf(3, true));
        }

        [Test]
        public void Credit_AloneAndOneOrTwoHelpsCount_LettersCountHalf_ShownIsAnError()
        {
            var c = new PuntaCredit();
            Assert.IsTrue(c.CountsAsHit(PuntaTier.Solo));
            Assert.IsTrue(c.CountsAsHit(PuntaTier.Pista));
            Assert.IsFalse(c.CountsAsHit(PuntaTier.Vista));
            var letters = new[] { c.CountsAsHit(PuntaTier.Letras), c.CountsAsHit(PuntaTier.Letras), c.CountsAsHit(PuntaTier.Letras), c.CountsAsHit(PuntaTier.Letras) };
            Assert.AreEqual(new[] { true, false, true, false }, letters);
            // otras palabras entre medio no cambian la alternancia
            var d = new PuntaCredit();
            Assert.IsTrue(d.CountsAsHit(PuntaTier.Letras));
            d.CountsAsHit(PuntaTier.Solo);
            Assert.IsFalse(d.CountsAsHit(PuntaTier.Letras));
        }

        [Test]
        public void Points_FallWithEachHelp_AndStreakAddsALittleOnlyWithoutTheLetters()
        {
            Assert.AreEqual(100, PuntaContract.PointsFor(PuntaTier.Solo, 1));
            Assert.AreEqual(150, PuntaContract.PointsFor(PuntaTier.Solo, 6));
            Assert.AreEqual(150, PuntaContract.PointsFor(PuntaTier.Solo, 20));
            Assert.AreEqual(70, PuntaContract.PointsFor(PuntaTier.Pista, 1));
            Assert.AreEqual(40, PuntaContract.PointsFor(PuntaTier.Letras, 5));
            Assert.AreEqual(0, PuntaContract.PointsFor(PuntaTier.Vista, 5));
        }

        private static PuntaTally TallyOf(params PuntaTier[] tiers)
        {
            var t = new PuntaTally();
            for (int i = 0; i < tiers.Length; i++) t.Add("palabra" + i, tiers[i], tiers[i] == PuntaTier.Solo ? 1000 : -1);
            return t;
        }

        [Test]
        public void Score_IsTheWeightedShareOfWords_AndTheRetoAlsoRewardsPace()
        {
            var allSolo = TallyOf(Enumerable.Repeat(PuntaTier.Solo, 8).ToArray());
            Assert.AreEqual(100, PuntaContract.Score(allSolo, false));
            Assert.AreEqual(100, PuntaContract.Score(allSolo, true));
            Assert.AreEqual(0, PuntaContract.Score(TallyOf(Enumerable.Repeat(PuntaTier.Vista, 8).ToArray()), false));
            Assert.AreEqual(0, PuntaContract.Score(new PuntaTally(), false));
            Assert.AreEqual(50, PuntaContract.Score(TallyOf(Enumerable.Repeat(PuntaTier.Solo, 4).ToArray()), true), "4 de 8 en el Reto: la mitad del ritmo");
            Assert.AreEqual(100, PuntaContract.Score(TallyOf(Enumerable.Repeat(PuntaTier.Solo, 4).ToArray()), false));
            // más ayuda, menos puntaje; nunca cae a cero si la persona la encontró
            int solo = PuntaContract.Score(TallyOf(PuntaTier.Solo, PuntaTier.Solo), false);
            int pista = PuntaContract.Score(TallyOf(PuntaTier.Solo, PuntaTier.Pista), false);
            int letras = PuntaContract.Score(TallyOf(PuntaTier.Solo, PuntaTier.Letras), false);
            int vista = PuntaContract.Score(TallyOf(PuntaTier.Solo, PuntaTier.Vista), false);
            Assert.Greater(solo, pista);
            Assert.Greater(pista, letras);
            Assert.Greater(letras, vista);
            Assert.Greater(vista, 0);
        }

        // ------------------------------------------------------------------ la definición que se escribe

        [Test]
        public void TypedCount_WaitsAQuarterSecond_ThenAbout22MsPerLetter_AndNeverPastTheEnd()
        {
            Assert.AreEqual(0, PuntaContract.TypedCount(0f, 40, false));
            Assert.AreEqual(0, PuntaContract.TypedCount(0.25f, 40, false));
            Assert.AreEqual(10, PuntaContract.TypedCount(0.25f + 0.0221f * 10, 40, false));
            Assert.AreEqual(40, PuntaContract.TypedCount(60f, 40, false));
            Assert.AreEqual(40, PuntaContract.TypedCount(0f, 40, true), "con quitar animaciones el texto aparece entero");
            Assert.AreEqual(0f, PuntaContract.TypeSeconds(40, true));
            Assert.AreEqual(0.25f + 40 * 0.022f, PuntaContract.TypeSeconds(40, false), 1e-4f);
        }

        [Test]
        public void IsAccepted_NeedsExactlyTheWord()
        {
            Assert.IsTrue(PuntaContract.IsAccepted("BUHO", "BUHO"));
            Assert.IsFalse(PuntaContract.IsAccepted("BOHU", "BUHO"));
            Assert.IsFalse(PuntaContract.IsAccepted("BUH", "BUHO"));
            Assert.IsFalse(PuntaContract.IsAccepted("buho", "BUHO"));
        }

        // ------------------------------------------------------------------ lo que se guarda

        [Test]
        public void Words_ParseAndFormat_AreForgivingAndDeduplicate()
        {
            CollectionAssert.AreEqual(new[] { "búho", "faro" }, PuntaContract.ParseWords(" búho; faro ;;búho "));
            Assert.AreEqual(0, PuntaContract.ParseWords(null).Count);
            Assert.AreEqual("a;b", PuntaContract.FormatWords(new[] { "a", "b" }));
        }

        [Test]
        public void PushRecent_AppendsWithoutRepeating_AndKeepsTheLastEighty()
        {
            Assert.AreEqual("a;b;c", PuntaContract.PushRecent("a;b", new[] { "c" }));
            Assert.AreEqual("b;a", PuntaContract.PushRecent("a;b", new[] { "a" }), "una repetida pasa al final");
            var many = Enumerable.Range(0, 120).Select(i => "w" + i).ToArray();
            var list = PuntaContract.ParseWords(PuntaContract.PushRecent("", many));
            Assert.AreEqual(PuntaContract.RecentMax, list.Count);
            Assert.AreEqual("w119", list.Last());
        }

        // ------------------------------------------------------------------ las cuentas del final

        [Test]
        public void Tally_CountsEachColor_MeanTimeOnlyOfTheOnesFoundAlone_AndListsTheBlueOnes()
        {
            var t = new PuntaTally(new[] { "faro", "búho", "nido" });
            t.Add("reloj", PuntaTier.Solo, 2000);
            t.Add("faro", PuntaTier.Solo, 4000);      // estaba pendiente: sale
            t.Add("búho", PuntaTier.Pista, -1);        // pendiente: con pista también sale
            t.Add("nido", PuntaTier.Letras, -1);       // pendiente: con las letras NO sale
            t.Add("mesa", PuntaTier.Vista, -1);
            Assert.AreEqual(5, t.Total);
            Assert.AreEqual(2, t.Solo);
            Assert.AreEqual(1, t.Pista);
            Assert.AreEqual(1, t.Letras);
            Assert.AreEqual(1, t.Vista);
            Assert.AreEqual(4, t.Found);
            Assert.AreEqual(3000, t.MeanSoloMs);
            Assert.AreEqual("reloj:o;faro:o;búho:p;nido:c;mesa:a", t.WordsCsv());
            Assert.AreEqual("mesa", t.BlueCsv());
            Assert.AreEqual("faro;búho", t.ClearedCsv());
        }

        [Test]
        public void Tally_WithoutAnyAloneWord_HasNoMeanTime()
        {
            var t = new PuntaTally();
            t.Add("a", PuntaTier.Pista, -1);
            Assert.AreEqual(-1, t.MeanSoloMs);
            Assert.AreEqual("", t.BlueCsv());
            Assert.AreEqual("", t.ClearedCsv());
        }

        // ------------------------------------------------------------------ las palabras de la partida

        private static List<string> Play(PuntaDirector d, int words, Func<int, int> level, out int blue, out List<int> blueAt)
        {
            blue = 0;
            blueAt = new List<int>();
            var played = new List<string>();
            for (int i = 0; i < words; i++)
            {
                var w = d.Next(level(i), i, out bool isBlue);
                played.Add(w.Word);
                if (isBlue) { blue++; blueAt.Add(i); }
            }
            return played;
        }

        [Test]
        public void Director_NeverRepeatsAWordInAGame_AndFollowsTheLevel()
        {
            var bank = Bank;
            var d = new PuntaDirector(bank, new Random(1), null, null, true);
            var played = new List<PuntaWord>();
            for (int i = 0; i < 8; i++) played.Add(d.Next(3, i, out _));
            Assert.AreEqual(8, played.Select(w => w.Word).Distinct().Count());
            Assert.IsTrue(played.All(w => w.Level == 3), "nivel 3 pedido");
        }

        [Test]
        public void Director_AvoidsRecentWords_AndAlternatesCategories()
        {
            var bank = Bank;
            var recent = bank.OfLevel(2).Take(bank.OfLevel(2).Count - 20).Select(w => w.Word).ToList();
            var d = new PuntaDirector(bank, new Random(4), recent, null, true);
            string lastCat = null;
            for (int i = 0; i < 8; i++)
            {
                var w = d.Next(2, i, out _);
                Assert.IsFalse(recent.Contains(w.Word), "recién jugada: " + w.Word);
                if (lastCat != null) Assert.AreNotEqual(lastCat, w.Category, "misma categoría seguida en " + i);
                lastCat = w.Category;
            }
        }

        [Test]
        public void Director_WhenALevelRunsOut_UsesTheNeighbourAndNeverCrashes()
        {
            var tiny = new PuntaBank(new[]
            {
                new PuntaWord("reloj", "RELOJ", "Lo miras para saber qué hora es.", 1, "objeto"),
                new PuntaWord("faro", "FARO", "Torre junto al mar con una luz.", 2, "lugar"),
            });
            var d = new PuntaDirector(tiny, new Random(1), null, null, false);
            for (int i = 0; i < 6; i++) Assert.IsNotNull(d.Next(5, i, out _));
        }

        [Test]
        public void Director_Precision_BringsBackAtMostTwoBlueWords_NeverTheFirst()
        {
            var bank = Bank;
            var pending = bank.All.Take(5).Select(w => w.Word).ToList();
            for (int seed = 0; seed < 30; seed++)
            {
                var d = new PuntaDirector(bank, new Random(seed), null, pending, true);
                var played = Play(d, PuntaContract.PrecisionWords, i => 2, out int blue, out var at);
                Assert.AreEqual(PuntaContract.MaxBlueInPrecision, blue, "semilla " + seed);
                Assert.IsFalse(at.Contains(0), "la primera nunca es azul");
                foreach (int i in at) Assert.IsTrue(pending.Contains(played[i]));
                Assert.AreEqual(played.Count, played.Distinct().Count());
            }
        }

        [Test]
        public void Director_Precision_WithOnePendingBringsOne_WithNoneBringsNone_UnknownWordsAreIgnored()
        {
            var bank = Bank;
            var one = new PuntaDirector(bank, new Random(2), null, new[] { bank.All[3].Word, "palabra-que-no-existe" }, true);
            Play(one, 8, i => 2, out int blue1, out _);
            Assert.AreEqual(1, blue1);
            var none = new PuntaDirector(bank, new Random(2), null, null, true);
            Play(none, 8, i => 2, out int blue0, out _);
            Assert.AreEqual(0, blue0);
        }

        [Test]
        public void Director_Reto_BringsBackOneBlueEveryFourWords_StartingAtTheThird()
        {
            var bank = Bank;
            var pending = bank.All.Take(6).Select(w => w.Word).ToList();
            var d = new PuntaDirector(bank, new Random(9), null, pending, false);
            Play(d, 12, i => 2, out int blue, out var at);
            CollectionAssert.AreEqual(new[] { 2, 6, 10 }, at);
            Assert.AreEqual(3, blue);
        }

        [Test]
        public void Director_RecordsTheWordsInTheOrderTheyWerePlayed()
        {
            var d = new PuntaDirector(Bank, new Random(5), null, null, true);
            var played = Play(d, 5, i => 1, out _, out _);
            CollectionAssert.AreEqual(played, d.UsedInOrder);
        }
    }

    public class PuntaVisibilityTests
    {
        [Test]
        public void EveryPieceTheGameDraws_IsOnAndOpaque_TheTilesAboveAll()
        {
            // 3-oct: la cara, el borde, la sombra y el brillo de la ficha se creaban APAGADOS y de la ficha solo se veía la letra tinta sobre el cielo oscuro.
            var go = new UnityEngine.GameObject("PuntaAudit");
            try
            {
                var controller = go.AddComponent<PuntaGameController>();
                // solo se arma la interfaz (Awake también estiliza el panel de resultado, y eso usa Destroy, que en modo edición no se puede)
                var build = typeof(PuntaGameController).GetMethod("BuildUi", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                build.Invoke(controller, null);
                go.SetActive(true);
                var problems = controller.AuditVisibility();
                Assert.IsEmpty(problems, "Piezas que no se verían: " + string.Join(" | ", problems));
            }
            finally { UnityEngine.Object.DestroyImmediate(go); }
        }
    }

    public class PuntaLayoutTests
    {
        [Test]
        public void TheTutorialCaptionGap_LeavesRoomBetweenTheCardAndTheSlots_OnNormalPhones()
        {
            foreach (float h in new[] { 760f, 800f, 860f })
            {
                var plain = PuntaLayout.Compute(h - 40f, 9);
                var gap = PuntaLayout.Compute(h - 40f, 9, 40f);
                Assert.GreaterOrEqual(gap.SlotY - 16f - gap.CardBottom, 44f + 40f - 30f, "alto " + h + ": cabe el mensaje de Nubi entre la tarjeta y las casillas");
                Assert.GreaterOrEqual(gap.SlotY, plain.SlotY, "alto " + h);
                float bankBottom = gap.BankFirstY + (gap.Rows - 1) * gap.RowGap + gap.TileD / 2f;
                Assert.LessOrEqual(bankBottom, gap.LadderY - 8f, "alto " + h);
            }
        }

        private static readonly float[] Heights = { 640f, 700f, 780f, 860f, 960f };

        [Test]
        public void EveryScreenHeight_FitsEverythingInOrder_ForAnyNumberOfTiles()
        {
            foreach (float h in Heights)
            {
                for (int count = 5; count <= 14; count++)
                {
                    var m = PuntaLayout.Compute(h, count);
                    string at = $"alto {h}, {count} fichas";
                    Assert.Less(m.SkyY, m.CardTop, at);
                    Assert.Less(m.CardBottom, m.SayY, at);
                    Assert.Less(m.SayY, m.SlotY - 16f, at);
                    Assert.Less(m.SlotY + 16f, m.BankFirstY - m.TileD / 2f, at + ": las fichas no pisan las casillas");
                    float bankBottom = m.BankFirstY + (m.Rows - 1) * m.RowGap + m.TileD / 2f;
                    Assert.LessOrEqual(bankBottom, m.LadderY - 8f, at + ": el banco no pisa la escalera de ayudas");
                    Assert.Less(m.LadderY, m.ButtonsTop, at);
                    Assert.AreEqual(h - PuntaLayout.ButtonsBottomMargin, m.ButtonsTop + PuntaLayout.ButtonH, 0.01f, at);
                    Assert.GreaterOrEqual(m.TileD, 40f, at);
                    Assert.GreaterOrEqual(PuntaLayout.ButtonH, 48f);
                }
            }
        }

        [Test]
        public void BankTiles_NeverOverlapAndStayInsideTheScreen()
        {
            foreach (float h in Heights)
            {
                for (int count = 5; count <= 14; count++)
                {
                    var m = PuntaLayout.Compute(h, count);
                    var pos = Enumerable.Range(0, count).Select(k => PuntaLayout.BankHome(m, k, count)).ToList();
                    for (int a = 0; a < count; a++)
                    {
                        Assert.GreaterOrEqual(pos[a].x - m.TileD / 2f, 4f, $"alto {h}, {count} fichas, ficha {a}");
                        Assert.LessOrEqual(pos[a].x + m.TileD / 2f, PuntaLayout.W - 4f);
                        for (int b = a + 1; b < count; b++)
                            Assert.GreaterOrEqual((pos[a] - pos[b]).magnitude, m.TileD - 0.01f, $"alto {h}, {count} fichas: {a} y {b} se pisan");
                    }
                }
            }
        }

        [Test]
        public void Slots_AreOneRowInsideTheScreen_AndPlacedTilesNeverOverlap()
        {
            var m = PuntaLayout.Compute(780f, 12);
            for (int n = 4; n <= 10; n++)
            {
                float prevX = -1f;
                for (int k = 0; k < n; k++)
                {
                    var p = PuntaLayout.SlotPos(m, k, n);
                    Assert.Greater(p.x, prevX);
                    Assert.GreaterOrEqual(p.x - 16f, 0f);
                    Assert.LessOrEqual(p.x + 16f, PuntaLayout.W);
                    Assert.AreEqual(m.SlotY, p.y);
                    prevX = p.x;
                }
                float scale = PuntaLayout.PlacedScale(m, n);
                Assert.LessOrEqual(scale, 1f);
                Assert.GreaterOrEqual(scale * m.TileD, 28f, "la ficha colocada con " + n + " letras sigue siendo legible");
                Assert.LessOrEqual(scale * m.TileD, PuntaLayout.SlotSpacing(n) + 2.01f);
            }
        }

        [Test]
        public void SkyRow_FitsEightInPrecisionAndUpToSixteenInTheReto()
        {
            var m = PuntaLayout.Compute(780f, 8);
            foreach (int slots in new[] { 1, 8, 12, 16 })
            {
                float prev = -1f;
                for (int k = 0; k < slots; k++)
                {
                    var p = PuntaLayout.SkyPos(m, k, slots);
                    Assert.Greater(p.x - PuntaLayout.SkyRadius, 0f);
                    Assert.Less(p.x + PuntaLayout.SkyRadius, PuntaLayout.W);
                    if (prev >= 0f) Assert.GreaterOrEqual(p.x - prev, PuntaLayout.SkyRadius * 2f, "luceros pisados con " + slots);
                    prev = p.x;
                }
            }
        }

        // ---- ronda guiada del tutorial (Tarea 60, 9-oct): «Saltar tutorial» nunca cae sobre las fichas.
        // El smoke falló una vez en 1080x1920: la palabra de la ronda era de 5 letras (+2 de relleno = 7 fichas = DOS filas) y el hueco de la ficha pedida, en la fila de abajo, pisaba «Saltar tutorial».

        /// <summary>Alturas del área de juego en dp: 640 = lienzo 1080x1920 (la más corta que se revisa), 720 = 1080x2160, 800 = 1080x2400 y teléfonos más altos.</summary>
        private static readonly float[] GuidedHeights = { 640f, 660f, 700f, 720f, 780f, 800f, 860f, 960f };

        /// <summary>Lo más bajo que ilumina el foco sobre la ficha <paramref name="tileY"/> (dp desde arriba).</summary>
        private static float HoleBottom(PuntaLayout.Metrics m, float tileY) => tileY + PuntaLayout.GuidedHoleScale * m.TileD / 2f + PuntaLayout.GuidedHoleSlack;

        [Test]
        public void GuidedRound_PicksOnlyWordsWhoseBankIsOneRow_ForYoungAndSenior()
        {
            var bank = PuntaBank.Load();
            foreach (bool senior in new[] { false, true })
            {
                int extra = PuntaContract.ExtraLetters(1, senior);
                var pool = bank.OfLevel(1).Where(w => PuntaLayout.FitsGuidedBank(w.Tiles, extra)).ToList();
                Assert.GreaterOrEqual(pool.Count, 20, "hay palabras de sobra para dos rondas distintas (mayores: " + senior + ")");
                foreach (var w in pool)
                    foreach (float h in GuidedHeights)
                        Assert.AreEqual(1, PuntaLayout.Compute(h, w.Tiles.Length + extra, 0f, PuntaLayout.GuidedReserve, GuidedTutorial.RowBottomDp + 2f).Rows, w.Word + " con " + (w.Tiles.Length + extra) + " fichas a " + h + " dp");
            }
            Assert.IsFalse(PuntaLayout.FitsGuidedBank("RELOJ", 2), "5 letras + 2 de relleno son 7 fichas: dos filas");
            Assert.IsTrue(PuntaLayout.FitsGuidedBank("CASA", 2));
            Assert.IsTrue(PuntaLayout.FitsGuidedBank("RELOJ", 1), "los mayores llevan una de relleno menos");
            Assert.IsFalse(PuntaLayout.FitsGuidedBank("SOL", 2), "menos de 4 letras no es una palabra de la ronda");
        }

        [Test]
        public void GuidedLayout_KeepsEverythingInOrder_BelowTheFixedTutorialRow_AtEveryHeight()
        {
            // «Saltar tutorial» ya no va abajo, entre el banco y «Ayudas»: desde la Tarea 127 está en la fila fija de arriba, así que solo queda comprobar que la tarjeta baja de esa fila y que el resto sigue en orden
            foreach (float h in GuidedHeights)
            {
                for (int count = 5; count <= PuntaLayout.GuidedMaxTiles; count++)
                {
                    var m = PuntaLayout.Compute(h, count, 0f, PuntaLayout.GuidedReserve, GuidedTutorial.RowBottomDp + 2f);
                    string at = $"alto {h}, {count} fichas";
                    Assert.AreEqual(1, m.Rows, at);
                    Assert.GreaterOrEqual(m.CardTop, GuidedTutorial.RowBottomDp, at + ": la tarjeta queda bajo la fila fija del tutorial");
                    // el resto de la disposición sigue en orden con el aire reservado
                    Assert.Less(m.CardBottom, m.SayY, at);
                    Assert.Less(m.SayY, m.SlotY - 16f, at);
                    Assert.Less(m.SlotY + 16f, m.BankFirstY - m.TileD / 2f, at + ": las fichas no pisan las casillas");
                    Assert.Less(m.LadderY, m.ButtonsTop, at);
                }
            }
        }

        [Test]
        public void TheQuestionSticksUnderTheCard_AndTheTrayDoesNotDependOnTheWord()
        {
            foreach (float h in new[] { 700f, 780f, 860f, 960f })
            {
                string at = "alto " + h;
                var m = PuntaLayout.Compute(h, 8);
                Assert.IsTrue(m.FixedGrid, at);
                Assert.AreEqual(PuntaLayout.AskGap, m.AskY - 14f - m.CardBottom, 0.01f, at + ": la pregunta va a 16 dp bajo la tarjeta");
                Assert.Less(m.AskY + 14f, m.SlotY - 23f, at + ": y sobre las casillas de la palabra (que aparecen con la ayuda 1)");
                // la bandeja es igual para toda palabra, de las fichas que traiga (5 a 14): no dice cuántas letras tiene
                for (int count = 5; count <= PuntaLayout.MaxBankTiles; count++)
                {
                    var c = PuntaLayout.Compute(h, count);
                    Assert.AreEqual(m.BankFirstY, c.BankFirstY, 0.01f, at + ", " + count + " fichas: la bandeja no se mueve con la palabra");
                    Assert.AreEqual(m.SlotY, c.SlotY, 0.01f, at + ", " + count + " fichas");
                    Assert.AreEqual(m.RowGap, c.RowGap, 0.01f, at + ", " + count + " fichas: ni cambia la separación de las filas");
                }
                // las tres filas de pozos siempre caben entre la pregunta y las ayudas, sin pisar nada
                float trayTop = m.BankFirstY - m.TileD / 2f - PuntaLayout.TrayPad;
                float trayBottom = trayTop + PuntaLayout.TrayHeight(m, PuntaLayout.TrayMaxRows);
                Assert.Less(m.SlotY + 23f, trayTop, at + ": las casillas de la palabra quedan sobre la bandeja");
                Assert.LessOrEqual(trayBottom, m.LadderY - 16f + PuntaLayout.TrayPad + 0.01f, at + ": la bandeja queda sobre las ayudas");
            }
        }

        [Test]
        public void TheTrayPockets_AreTheSameForEveryWord_AndEnoughForTheBiggestBank()
        {
            // Opus (20-oct): «los pozos fijos no regalan la ayuda 1»: la cantidad no depende del largo de la palabra y alcanza para el banco más grande que el juego puede armar
            var bank = PuntaBank.Load();
            Assert.Greater(bank.Count, 100, "el banco de palabras de verdad, no el mínimo de respaldo");
            int longest = 0;
            foreach (var w in bank.All) longest = Math.Max(longest, w.Tiles.Length);
            int biggestExtra = 0;
            for (int level = 1; level <= PuntaContract.MaxLevel; level++) biggestExtra = Math.Max(biggestExtra, Math.Max(PuntaContract.ExtraLetters(level, false), PuntaContract.ExtraLetters(level, true)));
            int biggest = longest + biggestExtra;
            Assert.LessOrEqual(biggest, PuntaLayout.TrayPockets, "nunca falta un pozo: la palabra más larga (" + longest + ") más el relleno más grande (" + biggestExtra + ") = " + biggest);
            Assert.AreEqual(PuntaLayout.MaxBankTiles, biggest, "MaxBankTiles dice lo mismo que los datos reales: si el banco de palabras o el relleno cambian, hay que revisar los pozos");
            Assert.AreEqual(PuntaLayout.BankCols * PuntaLayout.TrayMaxRows, PuntaLayout.TrayPockets);
            Assert.GreaterOrEqual(PuntaLayout.TrayMaxRows * PuntaLayout.BankCols, PuntaLayout.MaxBankTiles);
            // y todo lo que se ve antes de «¡La tengo!» (los pozos, su bandeja y el lugar de las casillas de la palabra) sale igual con CUALQUIER largo de palabra y de relleno
            foreach (float h in new[] { 640f, 700f, 780f, 860f, 960f })
            {
                var reference = PuntaLayout.Compute(h, 5);
                for (int tiles = 5; tiles <= biggest; tiles++)
                {
                    var c = PuntaLayout.Compute(h, tiles);
                    Assert.AreEqual(reference.BankFirstY, c.BankFirstY, 0.01f, "alto " + h + ", " + tiles + " fichas: el primer pozo");
                    Assert.AreEqual(reference.RowGap, c.RowGap, 0.01f, "alto " + h + ", " + tiles + " fichas: la separación de las filas");
                    Assert.AreEqual(reference.TileD, c.TileD, 0.01f, "alto " + h + ", " + tiles + " fichas: el tamaño del pozo");
                    Assert.AreEqual(PuntaLayout.TrayHeight(reference, PuntaLayout.TrayMaxRows), PuntaLayout.TrayHeight(c, PuntaLayout.TrayMaxRows), 0.01f, "alto " + h + ", " + tiles + " fichas: la bandeja");
                    // y cada ficha del banco cae en un pozo que existe
                    for (int k = 0; k < tiles; k++)
                        Assert.Less(k, PuntaLayout.TrayPockets, "alto " + h + ", " + tiles + " fichas: la ficha " + k + " tiene su pozo");
                }
            }
        }

        [Test]
        public void TheTray_IsDimWhileThinking_AndFirmOnceTheTilesCome()
        {
            // Ricardo (20-oct): «tenues hasta ¡La tengo!»: mientras se piensa (lo que mide punta_ms) la bandeja es un lugar que espera; con «¡La tengo!» pasa a firme a la vez que caen las fichas
            Assert.AreEqual(0f, PuntaLayout.TrayFirmTarget(false));
            Assert.AreEqual(1f, PuntaLayout.TrayFirmTarget(true));
            float thinking = PuntaLayout.TrayAlpha(PuntaLayout.TrayFirmTarget(false)), firm = PuntaLayout.TrayAlpha(PuntaLayout.TrayFirmTarget(true));
            Assert.GreaterOrEqual(thinking, 0.40f, "tenue: entre 40 y 50 %");
            Assert.LessOrEqual(thinking, 0.50f, "tenue: entre 40 y 50 %");
            Assert.AreEqual(1f, firm, 1e-6f, "firme con las fichas");
            for (int i = 0; i < 10; i++) Assert.LessOrEqual(PuntaLayout.TrayAlpha(i / 10f), PuntaLayout.TrayAlpha((i + 1) / 10f) + 1e-6f, "el paso de tenue a firme sube sin saltos hacia atrás");
            Assert.AreEqual(PuntaLayout.TrayDimAlpha, PuntaLayout.TrayAlpha(-3f), 1e-6f);
            Assert.AreEqual(1f, PuntaLayout.TrayAlpha(7f), 1e-6f);
            Assert.AreEqual(0.2f, PuntaLayout.TrayFirmSeconds, 1e-6f, "firme en 200 ms");
            // y el controlador usa estas funciones (no un número suelto): tenue/firme según «¡La tengo!» (_built), con «quitar animaciones» de golpe
            string path = System.IO.Path.Combine(UnityEngine.Application.dataPath, "Scripts/Games/Anagramas/PuntaGameController.cs");
            string source = System.IO.File.ReadAllText(path);
            StringAssert.Contains("PuntaLayout.TrayFirmTarget(_built)", source);
            StringAssert.Contains("PuntaLayout.TrayAlpha(_trayFirm)", source);
            StringAssert.Contains("smooth ? Mathf.MoveTowards(_trayFirm, firmTarget", source);
            StringAssert.Contains(": firmTarget;", source, "con «quitar animaciones» el cambio es de golpe");
        }

        [Test]
        public void TheTrayShrinksToTheRowsTheBankUses()
        {
            Assert.AreEqual(1, PuntaLayout.TrayRowsFor(4));
            Assert.AreEqual(1, PuntaLayout.TrayRowsFor(5));
            Assert.AreEqual(2, PuntaLayout.TrayRowsFor(6));
            Assert.AreEqual(2, PuntaLayout.TrayRowsFor(10));
            Assert.AreEqual(3, PuntaLayout.TrayRowsFor(11));
            Assert.AreEqual(3, PuntaLayout.TrayRowsFor(14));
            var m = PuntaLayout.Compute(860f, 8);
            Assert.AreEqual(m.BankFirstY - m.TileD / 2f - PuntaLayout.TrayPad, PuntaLayout.TrayCenterY(m, 3) - PuntaLayout.TrayHeight(m, 3) / 2f, 0.01f, "la bandeja se achica por abajo: el borde de arriba no se mueve");
            Assert.AreEqual(PuntaLayout.TrayCenterY(m, 1) - PuntaLayout.TrayHeight(m, 1) / 2f, PuntaLayout.TrayCenterY(m, 3) - PuntaLayout.TrayHeight(m, 3) / 2f, 0.01f);
        }

        [Test]
        public void TheBankFillsTheTrayLeftToRight_FivePerRow()
        {
            var m = PuntaLayout.Compute(860f, 9);
            for (int k = 0; k < 9; k++)
            {
                var p = PuntaLayout.BankHome(m, k, 9);
                Assert.AreEqual(m.BankFirstY + (k / 5) * m.RowGap, p.y, 0.01f, "ficha " + k + ": fila");
                Assert.AreEqual(PuntaLayout.W / 2f + ((k % 5) - 2f) * m.TileSpacing, p.x, 0.01f, "ficha " + k + ": columna");
            }
            // la ronda guiada conserva su fila centrada
            var g = PuntaLayout.Compute(860f, 5, 0f, PuntaLayout.GuidedReserve, GuidedTutorial.RowBottomDp + 2f);
            Assert.IsFalse(g.FixedGrid);
            Assert.AreEqual(g.SlotY, g.AskY, 0.01f, "en la ronda guiada la pregunta queda donde estaba");
            Assert.AreEqual(PuntaLayout.W / 2f, PuntaLayout.BankHome(g, 2, 5).x, 0.01f, "5 fichas centradas: la del medio");
        }

        [Test]
        public void GuidedSkip_TheOldRuleOf19PercentOfTheHeight_DidHitTheBottomRowOfASevenTileBank()
        {
            // la regla vieja: «Saltar tutorial» a 0,19 de la altura desde abajo y una palabra de 5 letras + 2 de relleno = 7 fichas = dos filas
            float h = 640f;
            // el banco de ANTES de la bandeja (Tarea 144: ahora el banco normal es una cuadrícula fija de 5 columnas dentro de una bandeja de 3 filas): lo da la disposición de la ronda guiada (minCardTop > 0), que lo conserva
            var m = PuntaLayout.Compute(h, 7, 0f, 0f, 1f);
            Assert.IsFalse(m.FixedGrid);
            Assert.AreEqual(2, m.Rows);
            float skipTop = (h - h * 0.19f) - PuntaLayout.SkipH / 2f;
            bool hit = false;
            for (int k = 0; k < 7; k++) hit |= HoleBottom(m, PuntaLayout.BankHome(m, k, 7).y) > skipTop;
            Assert.IsTrue(hit, "con la regla vieja y dos filas, el hueco de una ficha de abajo pisaba «Saltar tutorial» (5105 u² en el smoke del 9-oct)");
            // con seis fichas (una fila) la regla vieja pasaba por apenas 4 dp: por eso falló "a veces" y solo en la pantalla más corta
            var one = PuntaLayout.Compute(h, 6, 0f, 0f, 1f);
            Assert.Less(HoleBottom(one, one.BankFirstY), skipTop);
            Assert.Less(skipTop - HoleBottom(one, one.BankFirstY), 5f);
        }
    }
}
