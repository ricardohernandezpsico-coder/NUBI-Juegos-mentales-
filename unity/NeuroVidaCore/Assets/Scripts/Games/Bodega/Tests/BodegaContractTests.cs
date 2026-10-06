using System;
using System.Collections.Generic;
using NUnit.Framework;
using NeuroVida.Games.Bodega;

namespace NeuroVida.Games.Bodega.Tests
{
    /// <summary>«Bodega de carga» (docs/diseno-bodega-de-carga.md): miles de pedidos por etapa con objetos distintos y nunca dos en una escotilla, la caja siempre a una escotilla vacía, la respuesta que
    /// sigue siendo la escotilla que de verdad tiene el objeto después de mover y girar, el giro de 2 o 3 posiciones, todos los objetos pedidos, la esclusa que nunca es una escotilla, las medidas, el DDA y el récord.</summary>
    public class BodegaContractTests
    {
        private const int PerLevel = 2000;

        private static IEnumerable<Order> Orders(int level, int count, int seed)
        {
            var rng = new Random(seed * 131 + level);
            for (int k = 0; k < count; k++) yield return BodegaContract.Generate(level, rng);
        }

        // ------------------------------------------------------------------ etapas

        [Test]
        public void ThereAreTwelveStages_WithTheSpecTable_AndFiveGroups()
        {
            Assert.AreEqual(12, BodegaContract.MaxLevel);
            int[] hatches = { 6, 6, 8, 8, 8, 8, 10, 8, 10, 10, 10, 10 };
            int[] objects = { 2, 3, 3, 4, 4, 5, 5, 4, 5, 5, 6, 7 };
            int[] moves = { 0, 0, 0, 0, 1, 0, 1, 0, 0, 1, 1, 2 };
            bool[] spin = { false, false, false, false, false, false, false, true, true, true, true, true };
            int[] groups = { 1, 1, 2, 2, 3, 3, 3, 4, 4, 5, 5, 5 };
            for (int l = 1; l <= 12; l++)
            {
                var st = BodegaContract.StageOf(l);
                Assert.AreEqual(hatches[l - 1], st.Hatches, "escotillas de la etapa " + l);
                Assert.AreEqual(objects[l - 1], st.Objects, "objetos de la etapa " + l);
                Assert.AreEqual(moves[l - 1], st.Moves, "cajas de la etapa " + l);
                Assert.AreEqual(spin[l - 1], st.Spin, "giro de la etapa " + l);
                Assert.AreEqual(groups[l - 1], BodegaContract.Group(l), "grupo de la etapa " + l);
                Assert.Less(st.Objects, st.Hatches, "siempre sobra al menos una escotilla vacía (para las cajas)");
            }
            Assert.AreEqual(1, BodegaContract.StageOf(0).Number, "los niveles se acotan");
            Assert.AreEqual(12, BodegaContract.StageOf(99).Number);
            Assert.AreEqual(6, BodegaContract.PrecisionOrders);
            Assert.AreEqual(120f, BodegaContract.RetoSeconds);
            Assert.AreEqual(1.35f, BodegaContract.Slow(true));
            Assert.AreEqual(1f, BodegaContract.Slow(false));
        }

        [Test]
        public void TheIntroCards_AppearForWhatEachStageBrings_InOrder()
        {
            CollectionAssert.AreEqual(new[] { Intro.Bodega }, BodegaContract.IntrosFor(1));
            CollectionAssert.AreEqual(new[] { Intro.Bodega, Intro.Mueve }, BodegaContract.IntrosFor(5));
            CollectionAssert.AreEqual(new[] { Intro.Bodega, Intro.Gira }, BodegaContract.IntrosFor(8));
            CollectionAssert.AreEqual(new[] { Intro.Bodega, Intro.Mueve, Intro.Gira }, BodegaContract.IntrosFor(10));
            foreach (var i in new[] { Intro.Bodega, Intro.Mueve, Intro.Gira })
                Assert.That(BodegaContract.IntroText(i).Length, Is.InRange(2, 3), i + ": dos o tres líneas");
            StringAssert.Contains("esclusa", BodegaContract.IntroText(Intro.Bodega)[0]);
            StringAssert.Contains("esclusa", BodegaContract.IntroText(Intro.Gira)[1]);
            Assert.AreEqual("Fíjate dónde queda la esclusa", BodegaContract.SpinSub);
            Assert.AreEqual("Llega a la bodega", BodegaContract.StoreLabel);
        }

        // ------------------------------------------------------------------ miles de pedidos

        [Test]
        public void EveryOrder_HasDistinctObjects_NeverTwoInAHatch_AndEveryObjectIsAsked()
        {
            for (int level = 1; level <= 12; level++)
                foreach (var o in Orders(level, PerLevel, 1))
                {
                    string where = "etapa " + level;
                    var st = BodegaContract.StageOf(level);
                    Assert.AreEqual(st.Hatches, o.Hatches, where);
                    Assert.AreEqual(st.Hatches, o.Loaded.Length, where + ": la esclusa no es una escotilla: hay exactamente " + st.Hatches);
                    Assert.AreEqual(st.Objects, o.Objects, where);
                    var seen = new HashSet<int>();
                    int filled = 0;
                    foreach (int obj in o.Loaded)
                    {
                        if (obj < 0) continue;
                        filled++;
                        Assert.That(obj, Is.InRange(0, BodegaContract.ObjectCount - 1), where);
                        Assert.IsTrue(seen.Add(obj), where + ": objeto repetido " + obj);
                    }
                    Assert.AreEqual(st.Objects, filled, where + ": una escotilla por objeto, nunca dos en una");
                    // se piden TODOS los objetos guardados, una vez cada uno
                    CollectionAssert.AreEquivalent(seen, new HashSet<int>(o.Asks), where + ": se piden todos los objetos");
                    Assert.AreEqual(st.Objects, new HashSet<int>(o.Asks).Count, where + ": ninguno repetido");
                    // el robot guarda cada escotilla ocupada, una vez
                    Assert.AreEqual(st.Objects, o.StoreOrder.Length, where);
                    var stored = new HashSet<int>(o.StoreOrder);
                    Assert.AreEqual(st.Objects, stored.Count, where);
                    foreach (int h in stored) Assert.GreaterOrEqual(o.Loaded[h], 0, where + ": se guarda en una escotilla con objeto");
                }
        }

        [Test]
        public void TheMovedCrate_AlwaysGoesToAnEmptyHatch_AndItsObjectIsStillFoundWhereItEnded()
        {
            for (int level = 1; level <= 12; level++)
                foreach (var o in Orders(level, PerLevel, 2))
                {
                    string where = "etapa " + level;
                    var st = BodegaContract.StageOf(level);
                    Assert.AreEqual(st.Moves, o.Moves.Length, where + ": las cajas que dice la etapa");
                    var sim = (int[])o.Loaded.Clone();
                    var moved = new HashSet<int>();
                    foreach (var m in o.Moves)
                    {
                        Assert.AreEqual(m.Obj, sim[m.From], where + ": la caja sale de una escotilla con ese objeto");
                        Assert.AreEqual(-1, sim[m.To], where + ": la caja va SIEMPRE a una escotilla vacía");
                        Assert.AreNotEqual(m.From, m.To, where);
                        Assert.That(m.From, Is.InRange(0, o.Hatches - 1), where + ": solo escotillas, nunca la esclusa");
                        Assert.That(m.To, Is.InRange(0, o.Hatches - 1), where + ": solo escotillas, nunca la esclusa");
                        Assert.IsTrue(moved.Add(m.Obj), where + ": dos cajas distintas");
                        sim[m.To] = sim[m.From];
                        sim[m.From] = -1;
                    }
                    CollectionAssert.AreEqual(sim, o.Final, where + ": lo que queda coincide con las cajas movidas");
                    foreach (int obj in o.Asks)
                    {
                        int h = o.HatchOf(obj);
                        Assert.GreaterOrEqual(h, 0, where);
                        Assert.AreEqual(obj, o.Final[h], where + ": la respuesta es la escotilla que de verdad tiene el objeto");
                    }
                }
        }

        [Test]
        public void TheSpin_IsTwoOrThreePositions_NeverFourOrMore_AndTheRightAnswerStaysTheRightHatch()
        {
            var signs = new HashSet<int>();
            for (int level = 1; level <= 12; level++)
                foreach (var o in Orders(level, PerLevel, 3))
                {
                    string where = "etapa " + level;
                    var st = BodegaContract.StageOf(level);
                    if (!st.Spin) { Assert.AreEqual(0, o.Spin, where + ": no gira"); continue; }
                    Assert.That(Math.Abs(o.Spin), Is.InRange(2, 3), where + ": 2 o 3 posiciones");
                    signs.Add(Math.Sign(o.Spin));
                    int places = o.Hatches + 1;
                    var seenPos = new HashSet<int>();
                    for (int h = 0; h < o.Hatches; h++)
                    {
                        int pos = o.PositionAfterSpin(h);
                        Assert.That(pos, Is.InRange(0, places - 1), where);
                        Assert.IsTrue(seenPos.Add(pos), where + ": dos escotillas en el mismo lugar del anillo");
                        Assert.AreEqual(h, o.HatchAtPosition(pos), where + ": del lugar del anillo a la escotilla y de vuelta");
                    }
                    // la esclusa sigue en la posición 0 del anillo girado: el lugar que queda libre es el suyo y no corresponde a ninguna escotilla
                    int lockPlace = BodegaContract.Wrap(0 + o.Spin, places);
                    Assert.IsFalse(seenPos.Contains(lockPlace), where + ": ninguna escotilla ocupa el lugar de la esclusa");
                    Assert.AreEqual(-1, o.HatchAtPosition(lockPlace), where + ": la esclusa nunca es una escotilla elegible");
                    // después de mover y girar, tocar el lugar donde ahora está el objeto responde con la escotilla que lo tiene
                    foreach (int obj in o.Asks)
                    {
                        int h = o.HatchOf(obj);
                        Assert.AreEqual(h, o.HatchAtPosition(o.PositionAfterSpin(h)), where);
                        Assert.AreEqual(obj, o.Final[o.HatchAtPosition(o.PositionAfterSpin(h))], where);
                    }
                }
            CollectionAssert.AreEquivalent(new[] { -1, 1 }, signs, "el giro es en cualquier sentido");
        }

        [Test]
        public void TheLock_IsNeverAnEligibleHatch_ForStoringMovingOrAnswering()
        {
            for (int level = 1; level <= 12; level++)
                foreach (var o in Orders(level, 500, 4))
                {
                    // todos los índices que el pedido usa son escotillas 0..N-1; el lugar 0 del anillo (la esclusa) nunca es un índice
                    foreach (int h in o.StoreOrder) Assert.That(h, Is.InRange(0, o.Hatches - 1));
                    foreach (var m in o.Moves) { Assert.That(m.From, Is.InRange(0, o.Hatches - 1)); Assert.That(m.To, Is.InRange(0, o.Hatches - 1)); }
                    foreach (int obj in o.Asks) Assert.That(o.HatchOf(obj), Is.InRange(0, o.Hatches - 1));
                    for (int h = 0; h < o.Hatches; h++) Assert.AreEqual(h + 1, BodegaContract.Wrap(o.PositionAfterSpin(h) - o.Spin, o.Hatches + 1), "las escotillas ocupan los lugares 1..N del anillo sin girar");
                }
        }

        [Test]
        public void TheObjectChoice_UsesAllTwelveObjects_AndTheNamesAreReadable()
        {
            var seen = new HashSet<int>();
            var rng = new Random(9);
            for (int k = 0; k < 400; k++) foreach (int obj in BodegaContract.Generate(12, rng).Asks) seen.Add(obj);
            Assert.AreEqual(12, seen.Count, "salen los 12 objetos");
            Assert.AreEqual(12, BodegaContract.ObjectNames.Length);
            Assert.AreEqual(12, BodegaContract.ObjectWithArticle.Length);
            for (int i = 0; i < 12; i++)
            {
                StringAssert.EndsWith(BodegaContract.ObjectNames[i], BodegaContract.ObjectWithArticle[i]);
                Assert.IsTrue(BodegaContract.ObjectWithArticle[i].StartsWith("el ") || BodegaContract.ObjectWithArticle[i].StartsWith("la "));
                Assert.AreEqual("¿Dónde está " + BodegaContract.ObjectWithArticle[i] + "?", BodegaContract.AskText(i));
                Assert.AreEqual(char.ToUpperInvariant(BodegaContract.ObjectWithArticle[i][0]), BodegaContract.Capitalized(i)[0]);
            }
            Assert.AreEqual("¿Dónde está el farol?", BodegaContract.AskText(2));
            // sin los objetos que la regla de símbolos y el diseño dejan fuera
            foreach (var banned in new[] { "estrella", "copa", "ancla", "cruz", "luna" })
                foreach (var n in BodegaContract.ObjectNames) StringAssert.DoesNotContain(banned, n);
        }

        // ------------------------------------------------------------------ medidas

        [Test]
        public void TheFirstTryMeasure_CountsRightWithErrors()
        {
            var t = new BodegaTally();
            t.Found(true, 1000);
            t.Found(true, 1200);
            t.Found(false, 3000);       // un error: se encuentra pero ya no es al primer intento
            t.Found(true, 800);
            Assert.AreEqual(4, t.Total);
            Assert.AreEqual(3, t.FirstTry);
            Assert.AreEqual(75, t.Percent);
            Assert.AreEqual(2, t.BestStreak, "la racha más larga: dos seguidos antes del error");
            Assert.AreEqual(1, t.Streak, "el error corta la racha y el siguiente la reanuda");
            Assert.AreEqual(1500, t.MeanMs);
            t.BreakStreak();
            Assert.AreEqual(0, t.Streak);
            Assert.AreEqual(4, t.Total, "romper la racha no suma ni resta objetos");
            Assert.AreEqual(100, BodegaContract.Score(5, 5));
            Assert.AreEqual(60, BodegaContract.Score(3, 5));
            Assert.AreEqual(0, BodegaContract.Score(0, 0));
            Assert.AreEqual(-1, new BodegaTally().MeanMs);
        }

        [Test]
        public void TheRecord_OnlyRisesWithPerfectOrders()
        {
            var t = new BodegaTally();
            t.EndOrder(5, 1, 6);          // un error: no cuenta para el récord
            Assert.AreEqual(0, t.BiggestPerfect);
            Assert.AreEqual(0, t.PerfectOrders);
            t.EndOrder(3, 0, 3);
            Assert.AreEqual(3, t.BiggestPerfect);
            t.EndOrder(7, 2, 12);
            Assert.AreEqual(3, t.BiggestPerfect, "un pedido grande con errores no sube el récord");
            t.EndOrder(4, 0, 4);
            Assert.AreEqual(4, t.BiggestPerfect);
            t.EndOrder(2, 0, 1);
            Assert.AreEqual(4, t.BiggestPerfect, "el más grande, no el último");
            Assert.AreEqual(5, t.Orders);
            Assert.AreEqual(3, t.PerfectOrders);
            Assert.AreEqual(12, t.PeakLevel);
            Assert.AreEqual(5, t.PeakGroup);
            Assert.AreEqual(5, BodegaContract.NewRecord(2, 5), "el récord guardado y el de hoy: el mayor");
            Assert.AreEqual(5, BodegaContract.NewRecord(5, 3), "nunca baja");
            Assert.AreEqual(0, BodegaContract.NewRecord(-4, 0));
        }

        // ------------------------------------------------------------------ DDA

        private static AdaptiveDifficulty PrimedDda(AgeBand age = AgeBand.Adult)
        {
            var dda = new AdaptiveDifficulty(BodegaContract.MaxLevel, age, 6.0f, BodegaContract.DdaStepUp, useReaction: false);
            // 10 objetos para salir de la calibración inicial y terminar con un error (así no pesa la racha de aciertos)
            foreach (bool ok in new[] { true, true, false, true, true, true, false, true, true, false }) dda.Register(ok);
            return dda;
        }

        private static float DeltaOf(params bool[] hits)
        {
            var dda = PrimedDda();
            float before = dda.Rating;
            foreach (bool h in hits) dda.Register(h);
            return dda.Rating - before;
        }

        [Test]
        public void TheDda_RisesWithAPerfectOrder_StaysWithOneError_AndFallsWithTwoOrMore()
        {
            float perfect = DeltaOf(true, true, true, true, true);
            Assert.That(perfect, Is.InRange(0.9f, 1.3f), "un pedido perfecto de 5 objetos sube una etapa: " + perfect);
            float one = DeltaOf(true, false, true, true, true);
            Assert.That(Math.Abs(one), Is.LessThanOrEqualTo(0.3f), "un solo error: se queda: " + one);
            float two = DeltaOf(true, false, true, false, true);
            Assert.That(two, Is.LessThanOrEqualTo(-0.8f), "dos errores bajan una etapa: " + two);
            float three = DeltaOf(false, false, false, true, true);
            Assert.That(three, Is.LessThan(two), "más errores, baja más");
            Assert.Greater(perfect, one);
            Assert.Greater(one, two);
        }

        [Test]
        public void TheDda_UsesTheCommonTarget_AndTheStagesAreTheLadder()
        {
            Assert.AreEqual(0.80f, AdaptiveDifficulty.TargetFor(AgeBand.Adult));
            Assert.AreEqual(0.85f, AdaptiveDifficulty.TargetFor(AgeBand.Senior));
            // a lo largo de una partida de 6 pedidos perfectos de 5 objetos el nivel sube y no pasa de la etapa 12
            var dda = PrimedDda();
            int start = dda.Level;
            for (int order = 0; order < 6; order++) for (int k = 0; k < 5; k++) dda.Register(true);
            Assert.Greater(dda.Level, start);
            Assert.LessOrEqual(dda.Level, BodegaContract.MaxLevel);
            // y con errores baja sin pasar de la etapa 1
            for (int order = 0; order < 30; order++) for (int k = 0; k < 5; k++) dda.Register(k >= 3);
            Assert.GreaterOrEqual(dda.Level, 1);
        }

        // ------------------------------------------------------------------ las notas, los textos

        [Test]
        public void EachHatchHasItsOwnNote_RisingAlongThePentatonicScale()
        {
            for (int h = 0; h < 10; h++)
            {
                Assert.AreEqual(h, BodegaContract.NoteIndex(h));
                Assert.AreEqual(1, BodegaContract.NoteOctave(h));
            }
            Assert.AreEqual(2, BodegaContract.NoteOctave(10), "de la 11 en adelante, una octava arriba");
            Assert.AreEqual(0, BodegaContract.NoteIndex(10));
            var hz = new HashSet<float>();
            for (int h = 0; h < 10; h++) Assert.IsTrue(hz.Add(BodegaSounds.NoteHz(h)), "cada escotilla suena distinto");
            for (int h = 1; h < 10; h++) Assert.Greater(BodegaSounds.NoteHz(h), BodegaSounds.NoteHz(h - 1), "la melodía sube con las escotillas");
        }

        [Test]
        public void TheCardAndToastTexts_AreAsInTheDesign()
        {
            Assert.AreEqual("Mira dónde guarda cada cosa", BodegaContract.WatchTitle);
            Assert.AreEqual("¡Pedido perfecto!", BodegaContract.PerfectTitle);
            Assert.AreEqual("¡Pedido completo!", BodegaContract.DoneTitle);
            Assert.AreEqual("3 de 5 al primer intento · ¡nuevo récord!", BodegaContract.DoneLine(3, 5, true));
            Assert.AreEqual("3 de 5 al primer intento", BodegaContract.DoneLine(3, 5, false));
            Assert.AreEqual("Récord: 4 objetos", BodegaContract.RecordLine(4));
            Assert.AreEqual("Racha ×3", BodegaContract.StreakLine(3));
            Assert.AreEqual("1 error en este pedido", BodegaContract.ToastTitle(1));
            Assert.AreEqual("3 errores en este pedido", BodegaContract.ToastTitle(3));
            StringAssert.DoesNotContain("perfecto", BodegaContract.ToastTitle(1).ToLowerInvariant(), "un pedido con errores nunca dice «perfecto»");
            var tips = new List<string>(BodegaContract.AllTips());
            Assert.AreEqual(3, tips.Count);
            foreach (var tip in tips) StringAssert.StartsWith("Truco: ", tip);
            Assert.AreNotEqual(BodegaContract.ToastTip(1, 1), BodegaContract.ToastTip(2, 1), "el truco rota");
            // nada de nombres de enfermedades ni promesas de salud
            var all = new List<string> { BodegaContract.WatchTitle, BodegaContract.MoveTitle, BodegaContract.MoveSub, BodegaContract.SpinTitle, BodegaContract.SpinSub };
            all.AddRange(tips);
            foreach (var i in new[] { Intro.Bodega, Intro.Mueve, Intro.Gira }) all.AddRange(BodegaContract.IntroText(i));
            foreach (var text in all)
                foreach (var banned in new[] { "alzheimer", "demencia", "previene", "diagn", "neurona" })
                    StringAssert.DoesNotContain(banned, text.ToLowerInvariant());
        }
    }
}
