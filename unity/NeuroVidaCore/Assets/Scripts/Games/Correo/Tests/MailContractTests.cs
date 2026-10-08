using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using NeuroVida.Contracts;

namespace NeuroVida.Games.Correo.Tests
{
    /// <summary>«La estación de correo» (docs/diseno-correo-estacion.md): las 10 etapas, el plan del día, las ventanas de hora, lo de todos los días, la cancelación, los sacos, la cinta, cada toque, el resumen, el avance, el motor común, la medida y los textos.</summary>
    public class MailContractTests
    {
        private static MailDay NewDay(int level, int dayNo = 1, int seed = 1, MailMoment? routine = null)
        {
            var r = routine;
            return MailDay.Create(level, dayNo, ref r, new Random(seed));
        }

        // ------------------------------------------------------------------ etapas (§4)

        [Test]
        public void TheLadderHasTenStages_AsInTheTable()
        {
            Assert.AreEqual(10, MailContract.MaxLevel);
            var expected = new (int boxes, float every, int gold, int lazo, MailMoment[] times, bool routine, bool cancel, float win, MailIntro intro)[]
            {
                (2, 2.6f, 2, 0, new MailMoment[0], false, false, 0.12f, MailIntro.Estacion),
                (3, 2.4f, 2, 0, new[] { MailMoment.Mediodia }, false, false, 0.12f, MailIntro.Hora),
                (3, 2.3f, 2, 0, new[] { MailMoment.Mediodia }, true, false, 0.12f, MailIntro.Rutina),
                (3, 2.1f, 2, 0, new[] { MailMoment.Tarde }, true, true, 0.12f, MailIntro.Cancela),
                (4, 2.1f, 0, 2, new[] { MailMoment.Mediodia }, true, false, 0.12f, MailIntro.Lazo),
                (4, 2.0f, 1, 2, new[] { MailMoment.Tarde }, true, true, 0.12f, MailIntro.None),
                (4, 1.9f, 0, 2, new[] { MailMoment.Manana, MailMoment.Tarde }, true, false, 0.10f, MailIntro.None),
                (4, 1.8f, 1, 2, new[] { MailMoment.Manana, MailMoment.Tarde }, true, true, 0.10f, MailIntro.None),
                (4, 1.7f, 0, 3, new[] { MailMoment.Mediodia, MailMoment.Noche }, true, true, 0.09f, MailIntro.None),
                (4, 1.6f, 1, 3, new[] { MailMoment.Manana, MailMoment.Tarde, MailMoment.Noche }, true, true, 0.08f, MailIntro.None),
            };
            for (int i = 0; i < 10; i++)
            {
                var st = MailContract.Stage(i + 1);
                var e = expected[i];
                string at = "etapa " + (i + 1);
                Assert.AreEqual(e.boxes, st.Boxes, at + " buzones");
                Assert.AreEqual(e.every, st.Every, 1e-5f, at + " carta cada");
                Assert.AreEqual(e.gold, st.Gold, at + " sellos dorados");
                Assert.AreEqual(e.lazo, st.Lazo, at + " lazos");
                CollectionAssert.AreEqual(e.times, st.Times, at + " encargos por hora");
                Assert.AreEqual(e.routine, st.Routine, at + " todos los días");
                Assert.AreEqual(e.cancel, st.Cancel, at + " cancelado");
                Assert.AreEqual(e.win, st.Win, 1e-5f, at + " ventana");
                Assert.AreEqual(e.intro, st.Intro, at + " NUEVO");
            }
            Assert.AreEqual(MailContract.Stage(1).Boxes, MailContract.Stage(0).Boxes, "fuera de rango se acota");
            Assert.AreEqual(MailContract.Stage(10).Boxes, MailContract.Stage(99).Boxes);
        }

        [Test]
        public void TheGroupsAreTwoStagesEach_FiveInAll()
        {
            var expected = new[] { 1, 1, 2, 2, 3, 3, 4, 4, 5, 5 };
            for (int i = 0; i < 10; i++) Assert.AreEqual(expected[i], MailContract.GroupOf(i + 1), "etapa " + (i + 1));
            Assert.AreEqual(5, MailContract.GroupCount);
        }

        [Test]
        public void TheNewCards_AppearOnceInTheStageThatBringsThemAndTheEarlierOnesAreOwedWhenStartingHigher()
        {
            CollectionAssert.AreEqual(new[] { MailIntro.Estacion }, MailContract.IntrosFor(1).ToArray());
            CollectionAssert.AreEqual(new[] { MailIntro.Estacion, MailIntro.Hora }, MailContract.IntrosFor(2).ToArray());
            CollectionAssert.AreEqual(new[] { MailIntro.Estacion, MailIntro.Hora, MailIntro.Rutina, MailIntro.Cancela, MailIntro.Lazo }, MailContract.IntrosFor(8).ToArray());
            foreach (MailIntro intro in new[] { MailIntro.Estacion, MailIntro.Hora, MailIntro.Rutina, MailIntro.Cancela, MailIntro.Lazo })
            {
                var lines = MailContract.IntroLines(intro);
                Assert.GreaterOrEqual(lines.Length, 3, intro + ": al menos 3 líneas");
                Assert.IsFalse(string.IsNullOrEmpty(MailContract.IntroTag(intro)));
            }
            Assert.AreEqual("CORREO ESTELAR", MailContract.IntroTag(MailIntro.Estacion));
            Assert.AreEqual("NUEVO", MailContract.IntroTag(MailIntro.Hora));
            // la tarjeta de la hora cuenta lo del faro y de la nave (docs §11)
            StringAssert.Contains("nave del correo", string.Join(" ", MailContract.IntroLines(MailIntro.Hora)));
        }

        // ------------------------------------------------------------------ el plan del día (§4)

        [Test]
        public void TheCueLetters_AreSpreadOut_NeverInTheFirstSecondsNorTwoInARow_AndAllOfThemCanArrive()
        {
            for (int level = 1; level <= 10; level++)
            {
                var st = MailContract.Stage(level);
                for (int seed = 0; seed < 80; seed++)
                {
                    var d = NewDay(level, 1, seed);
                    var idx = new List<int>();
                    for (int i = 0; i < d.Plan.Count; i++) if (d.Plan[i].IsCue) idx.Add(i);
                    Assert.AreEqual(st.Gold, d.Plan.Count(l => l.Cue == MailCue.Gold), "etapa " + level + ": sellos dorados");
                    Assert.AreEqual(st.Lazo, d.Plan.Count(l => l.Cue == MailCue.Lazo), "etapa " + level + ": lazos");
                    foreach (int i in idx)
                    {
                        Assert.GreaterOrEqual(i, 3, "nunca en los primeros segundos (etapa " + level + ")");
                        Assert.GreaterOrEqual(i * st.Every, 4.7f, "salen después de unos 5 s");
                        Assert.IsFalse(d.Plan[i - 1].IsCue || d.Plan[i + 1].IsCue, "dos señales seguidas (etapa " + level + ", semilla " + seed + ")");
                        float rushDelay = d.RushPlan.Count * MailContract.RushLetters * MailContract.RushEvery;
                        Assert.LessOrEqual(i * st.Every + rushDelay, d.Seconds - 5f, "la carta señal llega con tiempo de sobra aunque caiga un saco (etapa " + level + ")");
                    }
                    Assert.IsTrue(d.Plan.All(l => l.Planet >= 0 && l.Planet < st.Boxes), "cada carta es de un buzón que existe");
                }
            }
        }

        // ------------------------------------------------------------------ ventanas de hora (§4)

        [Test]
        public void TheMoments_AreAtTheirCenters_AndTheWindowWidthFollowsTheStage()
        {
            Assert.AreEqual(0.26f, MailContract.MomentCenter(MailMoment.Manana));
            Assert.AreEqual(0.50f, MailContract.MomentCenter(MailMoment.Mediodia));
            Assert.AreEqual(0.70f, MailContract.MomentCenter(MailMoment.Tarde));
            Assert.AreEqual(0.88f, MailContract.MomentCenter(MailMoment.Noche));
            for (int level = 2; level <= 10; level++)
            {
                var d = NewDay(level);
                foreach (var t in d.Todos.Where(x => x.IsTime))
                {
                    float c = MailContract.MomentCenter(t.Moment);
                    Assert.AreEqual(d.Stage.Win, t.W1 - t.W0, 1e-5f, "ancho de la ventana (etapa " + level + ")");
                    Assert.AreEqual(c, (t.W0 + t.W1) / 2f, 1e-5f);
                    Assert.Greater(t.W0, 0.12f, "la ventana no empieza antes de que pueda llegar el aviso");
                    Assert.Less(t.W1, 1f, "termina antes de que acabe el día");
                }
            }
            Assert.AreEqual(0.12f, MailContract.Stage(2).Win);
            Assert.AreEqual(0.08f, MailContract.Stage(10).Win);
        }

        [Test]
        public void TheTimeTodos_OfEachStageAreExactlyTheTable()
        {
            for (int level = 1; level <= 10; level++)
            {
                var d = NewDay(level);
                var moments = d.Todos.Where(t => t.IsTime).Select(t => t.Moment).ToArray();
                var expected = MailContract.Stage(level).Times.ToArray();
                CollectionAssert.AreEqual(expected, moments, "etapa " + level);
            }
        }

        // ------------------------------------------------------------------ lo de todos los días (§2)

        [Test]
        public void TheEveryDayTodo_IsFixedOnDayOne_AndStaysTheSameEvenWhenTheStageChanges()
        {
            MailMoment? routine = null;
            var d1 = MailDay.Create(3, 1, ref routine, new Random(1));              // mediodía
            Assert.AreEqual(MailMoment.Mediodia, routine);
            Assert.IsTrue(d1.Todos.First(t => t.IsTime).Routine);
            // la etapa sube a la 4, que pide «tarde»: el encargo de todos los días sigue siendo el mediodía
            var d2 = MailDay.Create(4, 2, ref routine, new Random(2));
            var time2 = d2.Todos.Where(t => t.IsTime).ToList();
            Assert.AreEqual(1, time2.Count);
            Assert.AreEqual(MailMoment.Mediodia, time2[0].Moment, "sigue igual aunque cambie la etapa");
            Assert.IsTrue(time2[0].Routine);
            // la etapa 8 pide mañana y tarde: el primero se cambia por el de todos los días, el segundo queda
            var d3 = MailDay.Create(8, 3, ref routine, new Random(3));
            CollectionAssert.AreEqual(new[] { MailMoment.Mediodia, MailMoment.Tarde }, d3.Todos.Where(t => t.IsTime).Select(t => t.Moment).ToArray());
            Assert.IsTrue(d3.Todos.First(t => t.IsTime).Routine);
            Assert.IsFalse(d3.Todos.Last(t => t.IsTime).Routine);
            // un día de la etapa 2 (sin rutina) no marca ningún encargo como de todos los días
            var d4 = MailDay.Create(2, 4, ref routine, new Random(4));
            Assert.IsFalse(d4.Todos.Any(t => t.Routine));
        }

        // ------------------------------------------------------------------ cancelación (§2)

        [Test]
        public void TheRadioCancels_OnlyFromDayTwo_WithAboutSixtyPercent_AndNeverTheEveryDayOneWhenThereIsAnother()
        {
            int cancelled = 0, trials = 400;
            for (int seed = 0; seed < trials; seed++)
            {
                MailMoment? r = null;
                var day1 = MailDay.Create(8, 1, ref r, new Random(seed));
                Assert.IsNull(day1.CancelTodo, "el día 1 no se cancela nada: primero hay que haberlo practicado");
                var day2 = MailDay.Create(8, 2, ref r, new Random(seed + 1000));
                if (day2.CancelTodo == null) continue;
                cancelled++;
                Assert.IsTrue(day2.CancelTodo.IsTime);
                Assert.IsFalse(day2.CancelTodo.Routine, "se cancela uno que no sea el de todos los días");
                Assert.AreEqual(MailMoment.Tarde, day2.CancelTodo.Moment);
                Assert.AreEqual(Math.Max(0.12f, 0.70f - 0.3f), day2.CancelAt, 1e-5f, "la radio avisa 0,3 del día antes de la hora");
            }
            Assert.That(cancelled / (float)trials, Is.InRange(0.5f, 0.7f), "probabilidad 0,6");
            // las etapas sin cancelación no la traen nunca
            for (int seed = 0; seed < 50; seed++)
            {
                MailMoment? r = null;
                MailDay.Create(3, 1, ref r, new Random(seed));
                Assert.IsNull(MailDay.Create(3, 2, ref r, new Random(seed)).CancelTodo);
            }
        }

        [Test]
        public void WhenTheRadioCancelsAnOnlyTodo_ItPicksThatOne_AndTheAnnouncementFiresOnceBeforeTheHour()
        {
            MailMoment? r = null;
            MailDay d = null;
            for (int seed = 0; seed < 100 && (d == null || d.CancelTodo == null); seed++) { r = null; MailDay.Create(4, 1, ref r, new Random(seed)); d = MailDay.Create(4, 2, ref r, new Random(seed)); }
            Assert.IsNotNull(d.CancelTodo, "con la etapa 4 y 100 semillas, alguna vez cancela");
            Assert.AreEqual(MailMoment.Tarde, d.CancelTodo.Moment, "no hay otro: se cancela el de hora");
            d.Begin();
            var fired = 0;
            for (int i = 0; i < 1000 && !d.Ended; i++)
            {
                d.Step(0.05f);
                foreach (var e in d.Events) if (e.Kind == MailEventKind.RadioCancelled) fired++;
                d.Events.Clear();
            }
            Assert.AreEqual(1, fired);
            Assert.IsTrue(d.CancelTodo.Cancelled);
            Assert.AreEqual(MailState.Open, d.CancelTodo.State, "una ventana cancelada no «se pasa»");
        }

        // ------------------------------------------------------------------ ráfagas (§11)

        [Test]
        public void TheSacks_OneADayUntilStageFour_TwoFromStageFive_SpreadOut_AndNotNearAWindowInTheFirstFiveStages()
        {
            for (int level = 1; level <= 10; level++)
            {
                Assert.AreEqual(level >= 5 ? 2 : 1, MailContract.RushCount(level));
                for (int seed = 0; seed < 150; seed++)
                {
                    var d = NewDay(level, 1, seed);
                    Assert.AreEqual(MailContract.RushCount(level), d.RushPlan.Count, "etapa " + level + ": cuántos sacos");
                    for (int i = 0; i < d.RushPlan.Count; i++)
                    {
                        float f = d.RushPlan[i].At;
                        Assert.That(f, Is.InRange(0.16f, 0.82f));
                        if (i > 0) Assert.GreaterOrEqual(f - d.RushPlan[i - 1].At, 0.2f, "separados por al menos el 20 % del día");
                        if (level <= 5)
                            foreach (var t in d.Todos.Where(x => x.IsTime))
                                Assert.IsFalse(f > t.W0 - 0.08f && f < t.W1 + 0.04f, "el saco cae cerca de la hora de un encargo (etapa " + level + ", semilla " + seed + ")");
                    }
                }
            }
        }

        [Test]
        public void ASack_BringsFiveLettersEveryEightyFiveHundredths_TheBeltHoldsSevenWhileItLasts_AndEmptiesWithoutPunishment()
        {
            var d = NewDay(8);
            d.RushPlan.Clear();
            d.RushPlan.Add(new MailRush { At = 0f });
            d.Begin();                                  // 1 carta
            int spawned = 0, fell = 0;
            bool started = false;
            for (int i = 0; i < 400; i++)
            {
                d.Step(0.05f);
                foreach (var e in d.Events)
                {
                    if (e.Kind == MailEventKind.RushStarted) started = true;
                    if (e.Kind == MailEventKind.Spawned && e.Letter.Rush) spawned++;
                    if (e.Kind == MailEventKind.Fell) fell++;
                }
                d.Events.Clear();
                if (d.Rush == null && started) break;
                Assert.LessOrEqual(d.Belt.Count, MailContract.RushBeltCapacity);
            }
            Assert.IsTrue(started);
            Assert.AreEqual(5, spawned, "5 cartas seguidas");
            Assert.AreEqual(0, fell, "la cinta aguanta 7: nada cae durante el saco");
            Assert.AreEqual(6, d.Belt.Count, "1 de antes y las 5 del saco");
            Assert.AreEqual(d.Belt.Count + 1, d.Grace, "después del saco la fila se va vaciando sin castigo");
            Assert.AreEqual(0f, d.SpawnT, "la calma vuelve de cero");
        }

        [Test]
        public void TheSacksLetters_ComeEveryEightyFiveHundredths_AfterASixTenthsDelay()
        {
            var d = NewDay(1);
            d.RushPlan.Clear();
            d.RushPlan.Add(new MailRush { At = 0f });
            d.Begin();
            var times = new List<float>();
            for (int i = 0; i < 600; i++)
            {
                d.Step(0.01f);
                foreach (var e in d.Events) if (e.Kind == MailEventKind.Spawned && e.Letter.Rush) times.Add(d.DayT);
                d.Events.Clear();
                if (times.Count == 5) break;
            }
            Assert.AreEqual(5, times.Count);
            Assert.AreEqual(0.25f, times[0], 0.03f, "la primera sale a los 0,25 s (0,6 de espera + 0,25)");
            for (int i = 1; i < 5; i++) Assert.AreEqual(0.85f, times[i] - times[i - 1], 0.03f);
        }

        // ------------------------------------------------------------------ la cinta y las atrasadas (§2)

        [Test]
        public void WhenTheBeltFills_TheOldestLetterFallsToLate_CutsTheCombo_AndACueLetterCountsAsAMiss()
        {
            var d = NewDay(1);
            d.RushPlan.Clear();
            d.Plan.ForEach(l => l.Cue = MailCue.None);
            var todo = d.Todos.First(t => !t.IsTime);
            d.Begin();
            d.Belt[0].Cue = MailCue.Gold;
            d.Combo = 7;
            var oldest = d.Belt[0];
            for (int i = 0; i < 2; i++) { d.Step(d.Stage.Every); }
            Assert.AreEqual(3, d.Belt.Count, "la cinta aguanta 3");
            Assert.AreEqual(0, d.Late);
            d.Step(d.Stage.Every);                              // la cuarta empuja a la más antigua
            Assert.AreEqual(3, d.Belt.Count);
            Assert.AreEqual(1, d.Late);
            Assert.AreEqual(0, d.Combo, "se corta la racha");
            Assert.IsFalse(d.Belt.Contains(oldest));
            Assert.AreEqual(1, d.CueMiss);
            Assert.AreEqual(1, todo.Misses, "una carta con sello dorado que cae es un encargo perdido");
            Assert.IsTrue(d.Events.Any(e => e.Kind == MailEventKind.Fell && e.WasCue && e.Letter == oldest));
        }

        [Test]
        public void TheLettersComeAtTheStageRhythm_AndTheFirstOneComesWithTheDay()
        {
            for (int level = 1; level <= 10; level++)
            {
                var d = NewDay(level);
                d.RushPlan.Clear();
                d.Begin();
                Assert.AreEqual(1, d.Belt.Count);
                int spawned = 0;
                for (int i = 0; i < 1000 && d.DayT < 10f; i++)
                {
                    d.Step(0.01f);
                    foreach (var e in d.Events) if (e.Kind == MailEventKind.Spawned) spawned++;
                    d.Events.Clear();
                }
                Assert.AreEqual((int)(10f / d.Stage.Every), spawned, 1, "etapa " + level + ": una carta cada " + d.Stage.Every + " s");
            }
        }

        // ------------------------------------------------------------------ los toques

        [Test]
        public void SortingALetter_RightBoxAddsToTheStreakWithATierEveryTen_AndAWrongBoxCutsIt()
        {
            var d = NewDay(2);
            d.RushPlan.Clear();
            d.Begin();
            for (int n = 1; n <= 30; n++)
            {
                d.Belt.Clear();
                d.Belt.Add(new MailLetter { Planet = n % 3 });
                var res = d.TapBox(n % 3, true);
                Assert.AreEqual(MailTap.BoxRight, res.Kind);
                Assert.AreEqual(n, res.Combo);
                Assert.AreEqual(n % 10 == 0 ? Math.Min(3, n / 10) : 0, res.ComboTier, "escalón de la racha en ×" + n);
            }
            Assert.AreEqual(30, d.Right);
            Assert.AreEqual(30, d.BestCombo);
            d.Belt.Add(new MailLetter { Planet = 0 });
            var wrong = d.TapBox(1, true);
            Assert.AreEqual(MailTap.BoxWrong, wrong.Kind);
            Assert.AreEqual(0, d.Combo);
            Assert.AreEqual(1, d.WrongBox);
            Assert.AreEqual(30, d.BestCombo, "la mejor racha no baja");
            Assert.AreEqual(31, d.Sorted);
        }

        [Test]
        public void ALetterThatHasNotArrivedCannotBeSorted_AndNothingHappens()
        {
            var d = NewDay(1);
            d.Begin();
            var res = d.TapBox(0, false);
            Assert.AreEqual(MailTap.Ignored, res.Kind);
            Assert.AreEqual(1, d.Belt.Count);
            Assert.AreEqual(0, d.Sorted);
            Assert.AreEqual(MailTap.SafeNothing, d.TapSafe(false).Kind);
            Assert.AreEqual(1, d.Belt.Count);
            var empty = NewDay(1);
            Assert.AreEqual(MailTap.Ignored, empty.TapBox(0, true).Kind, "sin cartas no hay nada que clasificar");
        }

        [Test]
        public void ACueLetter_InTheSafeIsADoneTodo_InABoxItIsAMiss_AndAPlainLetterInTheSafeComesBack()
        {
            var d = NewDay(6);                       // un sello dorado y dos lazos
            d.RushPlan.Clear();
            var gold = d.Todos.First(t => t.Cue == MailCue.Gold);
            var lazo = d.Todos.First(t => t.Cue == MailCue.Lazo);
            d.Belt.Add(new MailLetter { Planet = 0, Cue = MailCue.Gold });
            var r1 = d.TapSafe(true);
            Assert.AreEqual(MailTap.SafeCue, r1.Kind);
            Assert.AreEqual(1, gold.Hits);
            Assert.AreEqual(1, d.CueHits);
            Assert.AreEqual(0, d.Belt.Count);
            d.Belt.Add(new MailLetter { Planet = 2, Cue = MailCue.Lazo });
            var r2 = d.TapBox(2, true);
            Assert.IsTrue(r2.CueMissed, "una carta con lazo que se clasifica en un buzón es un encargo perdido");
            Assert.AreEqual(1, lazo.Misses);
            Assert.AreEqual(1, d.CueMiss);
            Assert.AreEqual(MailTap.BoxRight, r2.Kind, "la carta SÍ iba a su buzón: el error es no haberla guardado");
            // una carta normal a la caja fuerte
            d.Combo = 5;
            var plain = new MailLetter { Planet = 1 };
            d.Belt.Add(plain);
            var r3 = d.TapSafe(true);
            Assert.AreEqual(MailTap.SafeWrongLetter, r3.Kind);
            Assert.AreSame(plain, d.Belt[0], "la carta vuelve a la cinta");
            Assert.AreEqual(1, d.SafeFalse);
            Assert.AreEqual(0, d.Combo);
        }

        [Test]
        public void TheBeacon_InTheWindowIsOnTime_BeforeItIsEarly_AfterItClosesItIsAMiss_AndACancelledOneIsACommission()
        {
            var d = NewDay(2);
            d.RushPlan.Clear();
            var t = d.Todos.First(x => x.IsTime);
            d.Begin();
            // antes de la hora
            d.DayT = 0.2f * d.Seconds;
            var early = d.TapBeacon();
            Assert.AreEqual(MailTap.BeaconEarly, early.Kind);
            Assert.AreEqual(1, t.Early);
            Assert.AreEqual(MailState.Open, t.State, "tocar antes no gasta el encargo");
            // sin encargo a la vista: «ahora no toca el faro» después de cumplirlo
            d.DayT = d.Seconds * (t.W0 + t.W1) / 2f;
            var hit = d.TapBeacon();
            Assert.AreEqual(MailTap.BeaconHit, hit.Kind);
            Assert.AreSame(t, hit.Todo);
            Assert.AreEqual(MailState.Hit, t.State);
            Assert.AreEqual(MailTap.BeaconNotNow, d.TapBeacon().Kind);
            // una ventana que se cierra sin faro
            var d2 = NewDay(2, 1, 5);
            d2.RushPlan.Clear();
            var t2 = d2.Todos.First(x => x.IsTime);
            d2.Begin();
            d2.DayT = d2.Seconds * (t2.W1 - 0.001f);
            d2.Step(1f);
            Assert.AreEqual(MailState.Miss, t2.State);
            Assert.IsTrue(d2.Events.Any(e => e.Kind == MailEventKind.WindowClosed && e.Todo == t2));
            Assert.AreEqual(MailTap.BeaconNotNow, d2.TapBeacon().Kind, "después de la ventana, el faro ya no sirve");
            // cancelado: encenderlo igual en la ventana es un error de comisión
            var d3 = NewDay(2, 1, 6);
            var t3 = d3.Todos.First(x => x.IsTime);
            t3.Cancelled = true;
            d3.DayT = d3.Seconds * t3.W0;
            var c = d3.TapBeacon();
            Assert.AreEqual(MailTap.BeaconCommission, c.Kind);
            Assert.AreEqual(MailState.Commission, t3.State);
            // y un cancelado no se encendió: no cuenta como error
            var d4 = NewDay(2, 1, 7);
            var t4 = d4.Todos.First(x => x.IsTime);
            t4.Cancelled = true;
            d4.Begin();
            d4.DayT = d4.Seconds * (t4.W1 + 0.1f);
            d4.Step(0.05f);
            Assert.AreEqual(MailState.Open, t4.State);
        }

        [Test]
        public void TheClock_CanBePeekedEveryOnePointSixSeconds_AndEachPeekNotesTheTimeOfDay()
        {
            var d = NewDay(2);
            d.Begin();
            d.DayT = 10f;
            Assert.AreEqual(MailTap.Peek, d.TapClock().Kind);
            d.DayT = 11f;
            Assert.AreEqual(MailTap.PeekCooldown, d.TapClock().Kind, "se destapa 1,6 s: antes no se puede volver a tocar");
            d.DayT = 11.7f;
            Assert.AreEqual(MailTap.Peek, d.TapClock().Kind);
            Assert.AreEqual(2, d.Peeks.Count);
            Assert.AreEqual(10f / d.Seconds, d.Peeks[0], 1e-5f);
            Assert.AreEqual(11.7f / d.Seconds, d.Peeks[1], 1e-5f);
        }

        // ------------------------------------------------------------------ el resumen del día (§2 y §7)

        [Test]
        public void TheRecap_ListsEachTodoWithItsMark_AndTheNearTheHourPeeksAreThoseInTheLastThirtyPercentBeforeTheWindowOrInsideIt()
        {
            var d = NewDay(8);                       // 1 dorado, 2 lazos... un solo encargo por tipo; mañana y tarde
            d.RushPlan.Clear();
            var gold = d.Todos.First(t => t.Cue == MailCue.Gold);
            var lazo = d.Todos.First(t => t.Cue == MailCue.Lazo);
            var m1 = d.Todos.First(t => t.IsTime);
            var m2 = d.Todos.Last(t => t.IsTime);
            gold.Hits = 1;
            lazo.Hits = 1; lazo.Misses = 1;
            m1.State = MailState.Hit;
            m2.State = MailState.Miss;
            // miradas: una muy temprano (lejos), una a 0,1 antes de la ventana del primero (cerca), una dentro de la segunda (cerca)
            d.Peeks.AddRange(new[] { 0.33f, m1.W0 - 0.1f, (m2.W0 + m2.W1) / 2f });
            d.Right = 8; d.Sorted = 10;
            var r = d.Summarize();
            Assert.AreEqual(4, r.Items.Count);
            Assert.AreEqual(2, r.Stat.PmOk, "el dorado y el primer faro");
            Assert.AreEqual(4, r.Stat.PmAll);
            Assert.IsFalse(r.Stat.Perfect);
            Assert.AreEqual(3, r.Stat.Peeks);
            Assert.AreEqual(2, r.Stat.GoodPeeks, "dos de tres cerca de la hora");
            Assert.AreEqual(0.8f, r.Stat.Accuracy, 1e-5f);
            Assert.IsTrue(r.Items.Single(i => i.Todo == gold).Ok);
            Assert.IsFalse(r.Items.Single(i => i.Todo == lazo).Ok, "un lazo perdido: el encargo no se cumplió");
            Assert.AreEqual("1 de 2", r.Items.Single(i => i.Todo == lazo).Detail);
            Assert.AreEqual(MailContract.DetHit, r.Items.Single(i => i.Todo == m1).Detail);
            Assert.AreEqual(MailContract.DetMiss, r.Items.Single(i => i.Todo == m2).Detail);
            Assert.AreEqual(3, r.Stat.Events, "las cartas con señal resueltas: 1 dorada y 2 lazos");
            Assert.AreEqual(2, r.Stat.EventsOk);
            Assert.AreEqual(0, r.Stat.GoldMissed, "el sello dorado se guardó");
            Assert.AreEqual(1, r.Stat.LazoMissed, "se escapó un lazo");
        }

        [Test]
        public void TheMissedCues_AreCountedPerType_AndEachStageOnlyBringsItsOwnCue()
        {
            // etapa 2: solo sello dorado → lo que se escapa es un sello dorado, nunca un lazo
            var d2 = NewDay(2);
            d2.RushPlan.Clear();
            Assert.IsFalse(d2.Todos.Any(t => !t.IsTime && t.Cue == MailCue.Lazo), "las etapas 1 a 4 no traen lazo");
            d2.Belt.Add(new MailLetter { Planet = 0, Cue = MailCue.Gold });
            Assert.IsTrue(d2.TapBox(0, true).CueMissed);
            var r2 = d2.Summarize();
            Assert.AreEqual(1, r2.Stat.GoldMissed);
            Assert.AreEqual(0, r2.Stat.LazoMissed);
            // etapa 5: solo lazo → lo que se escapa es un lazo
            var d5 = NewDay(5);
            d5.RushPlan.Clear();
            Assert.IsFalse(d5.Todos.Any(t => !t.IsTime && t.Cue == MailCue.Gold), "la etapa 5 solo trae lazo");
            d5.Belt.Add(new MailLetter { Planet = 1, Cue = MailCue.Lazo });
            Assert.IsTrue(d5.TapBox(1, true).CueMissed);
            var r5 = d5.Summarize();
            Assert.AreEqual(0, r5.Stat.GoldMissed);
            Assert.AreEqual(1, r5.Stat.LazoMissed);
            // y se suman en la partida
            var tally = new MailTally();
            tally.Add(r2.Stat); tally.Add(r5.Stat);
            Assert.AreEqual(1, tally.GoldMissed);
            Assert.AreEqual(1, tally.LazoMissed);
        }

        [Test]
        public void TheRecap_CountsTheCancelledOnes_AndATodoWhoseLetterNeverGotToTheBeltHadNoChance()
        {
            var d = NewDay(4, 2, 3);
            var t = d.Todos.First(x => x.IsTime);
            t.Cancelled = true;
            var untouched = d.Summarize();
            Assert.AreEqual(1, untouched.Stat.Cancels);
            Assert.AreEqual(0, untouched.Stat.Commissions);
            Assert.IsTrue(untouched.Items.Single(i => i.Todo == t).Ok, "no encenderlo es lo correcto");
            Assert.AreEqual(MailContract.DetNoCommission, untouched.Items.Single(i => i.Todo == t).Detail);
            // los dorados que nunca llegaron a resolverse no tuvieron oportunidad y no cuentan
            Assert.IsFalse(untouched.Items.Any(i => !i.Todo.IsTime), "sin cartas con señal resueltas no hay encargo por evento que medir");
            t.State = MailState.Commission;
            var done = d.Summarize();
            Assert.AreEqual(1, done.Stat.Commissions);
            Assert.IsFalse(done.Items.Single(i => i.Todo == t).Ok);
            Assert.AreEqual(MailContract.DetCommission, done.Items.Single(i => i.Todo == t).Detail);
        }

        // ------------------------------------------------------------------ el avance (§4)

        [Test]
        public void TheDayAdvancesTheStage_WhenAllTodosAreDoneAndAtLeastEightyFivePercentOfTheLettersWereRight_AndFallsWithLessThanHalf()
        {
            Assert.AreEqual(5, MailContract.Advance(4, 3, 3, 0.85f), "todos y 85 %: sube");
            Assert.AreEqual(5, MailContract.Advance(4, 3, 3, 1f));
            Assert.AreEqual(4, MailContract.Advance(4, 3, 3, 0.84f), "con menos de 85 % se queda");
            Assert.AreEqual(4, MailContract.Advance(4, 2, 3, 1f), "faltó un encargo: se queda");
            Assert.AreEqual(4, MailContract.Advance(4, 2, 4, 1f), "la mitad justa: se queda");
            Assert.AreEqual(3, MailContract.Advance(4, 1, 4, 1f), "menos de la mitad: baja");
            Assert.AreEqual(3, MailContract.Advance(4, 0, 2, 1f));
            Assert.AreEqual(10, MailContract.Advance(10, 5, 5, 1f), "el techo es la etapa 10");
            Assert.AreEqual(1, MailContract.Advance(1, 0, 3, 0f), "el piso es la etapa 1");
            Assert.AreEqual(4, MailContract.Advance(4, 0, 0, 1f), "sin encargos no se mueve");
            // los límites del modo (Suave pone techo; Desafío y Experto, piso)
            Assert.AreEqual(5, MailContract.Advance(5, 3, 3, 1f, 1, 5));
            Assert.AreEqual(6, MailContract.Advance(6, 0, 3, 1f, 6, 10), "no baja del piso");
            Assert.IsTrue(MailContract.DayWasUp(3, 3, 0.9f));
            Assert.IsFalse(MailContract.DayWasUp(2, 3, 1f));
            Assert.IsFalse(MailContract.DayWasUp(0, 0, 1f));
        }

        // ------------------------------------------------------------------ la partida: 4 días

        [Test]
        public void ARun_IsFourDays_TheStageFollowsTheRule_AndTheEveryDayTodoSurvives()
        {
            var run = new MailRun(3, 0, new Random(5));
            for (int day = 1; day <= 4; day++)
            {
                Assert.IsFalse(run.Finished);
                var d = run.NextDay();
                Assert.AreEqual(day, d.DayNo);
                Play(d, new Random(day), memory: 1.0);
                run.Complete(d, d.Summarize());
            }
            Assert.IsTrue(run.Finished);
            Assert.IsNull(run.NextDay(), "ya fueron los 4 días");
            Assert.AreEqual(4, run.History.Count);
            Assert.AreEqual(4, run.Tally.Days);
            Assert.AreEqual(7, run.Level, "con memoria perfecta sube una etapa por día");
            Assert.AreEqual(7, run.MaxLevelReached);
            Assert.AreEqual(MailMoment.Mediodia, run.Routine, "el encargo de todos los días se fijó con el primero de la etapa 3");
        }

        [Test]
        public void ARun_FollowsTheModeBounds_AndAPerfectDayWritesTheRecord()
        {
            var run = new MailRun(4, 12, new Random(9)) { Ceiling = 4, Floor = 3 };
            var d = run.NextDay();
            Play(d, new Random(1), 1.0);
            var rec = d.Summarize();
            run.Complete(d, rec);
            Assert.AreEqual(4, run.Level, "Suave no deja pasar del techo");
            if (rec.Stat.Perfect && rec.Stat.Right > 12) { Assert.IsTrue(run.NewRecordToday); Assert.AreEqual(rec.Stat.Right, run.BestDayRecord); }
            else Assert.AreEqual(12, run.BestDayRecord);
            Assert.AreEqual(12, MailContract.NewRecord(12, 9), "el récord nunca baja");
            Assert.AreEqual(15, MailContract.NewRecord(12, 15));
            Assert.AreEqual(0, MailContract.NewRecord(-4, -1));
        }

        // ------------------------------------------------------------------ simulación (§4: «0 partidas trabadas»)

        /// <summary>Juega un día con un jugador que cumple cada encargo con la probabilidad <paramref name="memory"/> y clasifica bien cada carta.</summary>
        private static void Play(MailDay d, Random rng, double memory)
        {
            d.Begin();
            var peeked = new HashSet<MailTodo>();
            for (int i = 0; i < 5000 && !d.Ended; i++)
            {
                d.Step(0.05f);
                d.Events.Clear();
                foreach (var t in d.Todos)
                {
                    if (!t.IsTime || t.State != MailState.Open || t.Cancelled) continue;
                    if (d.Fraction >= t.W0 + 0.005f && !peeked.Contains(t)) { peeked.Add(t); if (rng.NextDouble() < memory) d.TapBeacon(); }
                }
                if (d.Belt.Count > 0)
                {
                    var l = d.Belt[0];
                    if (l.IsCue && rng.NextDouble() < memory) d.TapSafe(true);
                    else d.TapBox(l.Planet, true);
                }
            }
            Assert.IsTrue(d.Ended, "el día no se trabó: terminó en " + d.DayT + " s");
        }

        [Test]
        public void AFlawlessPlayer_NeverGetsStuck_AndDoesEveryTodoInEveryStage()
        {
            for (int level = 1; level <= 10; level++)
            {
                for (int seed = 0; seed < 25; seed++)
                {
                    MailMoment? routine = null;
                    var d = MailDay.Create(level, 2, ref routine, new Random(seed * 7 + level));
                    Play(d, new Random(seed), 1.0);
                    var r = d.Summarize();
                    int todos = d.Todos.Count;
                    Assert.AreEqual(todos, r.Stat.PmAll, "etapa " + level + ", semilla " + seed + ": cada encargo tuvo su oportunidad");
                    Assert.AreEqual(r.Stat.PmAll, r.Stat.PmOk, "etapa " + level + ", semilla " + seed + ": con memoria perfecta, todos cumplidos");
                    Assert.GreaterOrEqual(r.Stat.Accuracy, 0.99f);
                    Assert.AreEqual(0, d.Late);
                }
            }
        }

        [Test]
        public void TheMeasureFollowsTheMemoryOfThePlayer()
        {
            double Percent(double memory)
            {
                int ok = 0, all = 0;
                for (int seed = 0; seed < 40; seed++)
                {
                    var run = new MailRun(4, 0, new Random(seed));
                    while (!run.Finished)
                    {
                        var d = run.NextDay();
                        Play(d, new Random(seed * 31 + d.DayNo), memory);
                        run.Complete(d, d.Summarize());
                    }
                    ok += run.Tally.MeasureOk; all += run.Tally.MeasureAll;
                }
                return 100.0 * ok / all;
            }
            double p100 = Percent(1.0), p80 = Percent(0.8), p50 = Percent(0.5), p20 = Percent(0.2);
            Assert.GreaterOrEqual(p100, 99.0);
            Assert.Greater(p100, p80, "100 % > 80 %");
            Assert.Greater(p80, p50);
            Assert.Greater(p50, p20);
            Assert.Less(p20, 55.0, "con memoria floja la medida es baja (las cancelaciones no hechas cuentan como cumplidas)");
        }

        // ------------------------------------------------------------------ motor común, medida y telemetría (§7)

        [Test]
        public void TheCommonEngineRunsOnTenStages_ItStartsFromTheSavedRatingOrTheDebugStage()
        {
            var cfg = new SequenceConfigDetails { age_band = "ADULT", level = 1 };
            Assert.AreEqual(10, MailContract.MaxLevel);
            cfg.mail_stage = 7;
            Assert.AreEqual(7f, MailContract.StartRating(cfg));
            Assert.AreEqual(7, MailContract.StartLevel(MailContract.CreateEngine(cfg)));
            cfg.mail_stage = 0;
            cfg.has_dda_rating = true;
            cfg.dda_rating = 0.5f;
            Assert.AreEqual(6f, MailContract.StartRating(cfg), 1e-4f, "el rating guardado (0,5) = etapa 6");
            Assert.AreEqual(10, MailContract.CreateEngine(cfg).MaxLevel);
            // cada encargo es un ensayo: cumplir 3 sube, fallar 1 baja
            var dda = MailContract.CreateEngine(cfg);
            float before = dda.Rating;
            for (int i = 0; i < 3; i++) dda.Register(true);
            Assert.Greater(dda.Rating, before);
            float up = dda.Rating;
            dda.Register(false);
            Assert.Less(dda.Rating, up);
        }

        [Test]
        public void TheMeasureIsTheTodosDone_CancelledOnesNotDoneCountAsDone_AndNoTodosMeansNoMeasure()
        {
            Assert.AreEqual(70, MailContract.MemoryPercent(7, 10));
            Assert.AreEqual(100, MailContract.MemoryPercent(12, 10));
            Assert.AreEqual(0, MailContract.MemoryPercent(-3, 10));
            Assert.IsNull(MailContract.MemoryPercent(0, 0));
            Assert.AreEqual(100, MailContract.Score(0, 0), "un día sin nada que recordar no es una mala partida");
            Assert.AreEqual(70, MailContract.Score(7, 10));
            var t = new MailTally();
            t.Add(new MailDayStat { Events = 4, EventsOk = 3, TimesAll = 2, TimesOk = 1, Cancels = 1, Commissions = 0, Peeks = 3, GoodPeeks = 2, Right = 20, Sorted = 22, BestCombo = 9, Perfect = false });
            t.Add(new MailDayStat { Events = 2, EventsOk = 2, TimesAll = 1, TimesOk = 1, Cancels = 1, Commissions = 1, Peeks = 1, GoodPeeks = 1, Right = 15, Sorted = 15, BestCombo = 15, Perfect = true });
            Assert.AreEqual(11, t.MeasureAll);
            Assert.AreEqual(5 + 2 + (2 - 1), t.MeasureOk, "5 de eventos, 2 de horas y 1 cancelado sin hacer");
            Assert.AreEqual(73, t.Percent);
            Assert.AreEqual(35, t.Right);
            Assert.AreEqual(15, t.BestCombo);
            Assert.AreEqual(1, t.PerfectDays);
            Assert.AreEqual(2, t.Days);
        }

        [Test]
        public void TheTelemetry_ReplacesTheOldMailFields_AndCarriesTheNewMeasure()
        {
            var cfg = new SequenceConfigDetails { age_band = "ADULT", level = 2, timed = false };
            var dda = MailContract.CreateEngine(cfg);
            dda.Register(true); dda.Register(true); dda.Register(false);
            var t = new MailTally();
            t.Add(new MailDayStat { Events = 4, EventsOk = 3, GoldMissed = 0, LazoMissed = 1, TimesAll = 2, TimesOk = 1, Cancels = 1, Commissions = 1, Early = 2, Peeks = 3, GoodPeeks = 2, Right = 20, Sorted = 22, BestCombo = 9, Perfect = false });
            var m = MailContract.BuildMetrics(dda, t, 20, true, 6, cfg);
            Assert.AreEqual(3, m.mail_ev_hits); Assert.AreEqual(4, m.mail_ev_total);
            Assert.AreEqual(1, m.mail_time_hits); Assert.AreEqual(2, m.mail_time_total);
            Assert.AreEqual(1, m.mail_cancels); Assert.AreEqual(1, m.mail_commissions);
            Assert.AreEqual(2, m.mail_early);
            Assert.AreEqual(3, m.mail_peeks); Assert.AreEqual(2, m.mail_peeks_good);
            Assert.AreEqual(20, m.mail_right); Assert.AreEqual(22, m.mail_sorted); Assert.AreEqual(9, m.mail_best_combo);
            Assert.AreEqual(0, m.mail_days_perfect);
            Assert.AreEqual(0, m.mail_gold_missed); Assert.AreEqual(1, m.mail_lazo_missed);
            Assert.AreEqual(3, m.mail_group, "la etapa 6 es del grupo 3");
            Assert.AreEqual(20, m.mail_best); Assert.AreEqual(1, m.mail_new);
            Assert.AreEqual(m.correct_trials, 4 + 0, "aciertos de la medida: 3 de eventos + 1 hora + 0 cancelado hecho = 4");
            Assert.AreEqual(7, m.total_trials);
            Assert.AreEqual(MailContract.Score(4, 7), m.calculated_score);
            Assert.AreEqual(dda.RatingNormalized, m.end_rating, 1e-6f);
            Assert.AreEqual(dda.ScoredTrials, m.mode_trials);
            // lo que ya no existe: nada del vuelo, del escudo ni de los asteroides
            foreach (var f in typeof(StroopSessionMetrics).GetFields())
                foreach (var gone in new[] { "mail_radio", "mail_lane", "mail_envelopes", "mail_asteroid", "mail_hull", "mail_emergencies", "mail_lure", "mail_event_" })
                    StringAssert.DoesNotContain(gone, f.Name, "se fue con el vuelo: " + f.Name);
            // sin partida jugada, los campos nuevos quedan en -1 (una versión vieja de Unity no los manda)
            var blank = new StroopSessionMetrics();
            Assert.AreEqual(-1, blank.mail_ev_total);
            Assert.AreEqual(-1, blank.mail_group);
            Assert.AreEqual(-1, blank.mail_gold_missed); Assert.AreEqual(-1, blank.mail_lazo_missed);
        }

        // ------------------------------------------------------------------ textos (§5, §7 y §11)

        [Test]
        public void TheTexts_AreAsInTheDesign_AndNothingPromisesHealth()
        {
            Assert.AreEqual("al mediodía", MailContract.MomentPhrase(MailMoment.Mediodia));
            Assert.AreEqual("Al mediodía,", MailContract.TimeLineA(MailMoment.Mediodia));
            Assert.AreEqual("A media tarde,", MailContract.TimeLineA(MailMoment.Tarde));
            Assert.AreEqual("Si llega una carta con sello dorado,", MailContract.CueLineA(MailCue.Gold));
            Assert.AreEqual("Si llega una carta con lazo,", MailContract.CueLineA(MailCue.Lazo));
            Assert.AreEqual("Se pasó la hora: al anochecer", MailContract.FloatLateWindow(MailMoment.Noche));
            Assert.AreEqual("¡Racha ×10!", MailContract.FloatCombo(10));
            Assert.AreEqual("¡Imparable! ×20", MailContract.FloatCombo(20));
            Assert.AreEqual("¡Maestro del correo! ×30", MailContract.FloatCombo(30));
            Assert.AreEqual("¡Maestro del correo! ×40", MailContract.FloatCombo(40));
            Assert.AreEqual("hoy NO hace falta encender el faro a media tarde", MailContract.RadioText(MailMoment.Tarde));
            Assert.AreEqual("DÍA 2 DE 4", MailContract.DayTitle(2));
            Assert.AreEqual("Día 3 de 4", MailContract.DayHud(3));
            Assert.AreEqual("Cartas: 12", MailContract.CartasLine(12, 0));
            Assert.AreEqual("Cartas: 12 · atrasadas: 2", MailContract.CartasLine(12, 2));
            Assert.AreEqual("¡Todos los encargos!", MailContract.RecapTitle(3, 3));
            Assert.AreEqual("Encargos: 2 de 3", MailContract.RecapTitle(2, 3));
            Assert.AreEqual("Reloj: lo miraste 1 vez, 1 cerca de la hora", MailContract.RecapClock(1, 1));
            Assert.AreEqual("Reloj: lo miraste 3 veces, 2 cerca de la hora", MailContract.RecapClock(3, 2));
            Assert.AreEqual("Ver resultado", MailContract.RecapNext(4));
            Assert.AreEqual("Siguiente día", MailContract.RecapNext(2));
            Assert.AreEqual(3, MailContract.Tips.Length);
            Assert.AreNotEqual(MailContract.Tip(0), MailContract.Tip(1));
            Assert.AreEqual(MailContract.Tip(3), MailContract.Tip(0), "rotan");
            Assert.AreEqual(MailContract.Tip(2), MailContract.Tip(-1));
            foreach (var s in new[] { MailContract.BriefNote2, MailContract.RushSub, MailContract.RecapRecord, MailContract.FloatGoldMissed, MailContract.FloatShipArrived }.Concat(MailContract.Tips))
            {
                StringAssert.DoesNotContain("neurona", s.ToLowerInvariant());
                StringAssert.DoesNotContain("alzheimer", s.ToLowerInvariant());
                StringAssert.DoesNotContain("demencia", s.ToLowerInvariant());
                StringAssert.DoesNotContain("diagn", s.ToLowerInvariant().Replace("no es un diagnóstico", ""));
            }
        }

        [Test]
        public void ThePlanetsAreNeutralFigures_WithTheirOwnNote()
        {
            CollectionAssert.AreEqual(new[] { "Coralia", "Celesta", "Lima", "Uva" }, MailContract.PlanetNames);
            CollectionAssert.AreEqual(new[] { "círculo", "triángulo", "cuadrado", "gota" }, MailContract.PlanetShapes);
            Assert.AreEqual(4, MailContract.PlanetNotes.Distinct().Count(), "cada buzón suena distinto");
            foreach (var shape in MailContract.PlanetShapes)
                foreach (var banned in new[] { "estrella", "cruz", "luna", "sol" })
                    StringAssert.DoesNotContain(banned, shape);
        }
    }
}
