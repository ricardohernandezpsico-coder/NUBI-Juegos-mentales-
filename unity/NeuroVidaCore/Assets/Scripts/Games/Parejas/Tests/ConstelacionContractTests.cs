using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using NeuroVida.Contracts;

namespace NeuroVida.Games.Parejas.Tests
{
    /// <summary>«Constelaciones» (docs/diseno-constelaciones.md): las 18 etapas, el reparto de las luces, las reglas de un toque (oportunidad, acierto, «se te escapó», racha), las líneas, el motor común, la medida y los textos.</summary>
    public class ConstelacionContractTests
    {
        // ------------------------------------------------------------------ etapas (§4)

        [Test]
        public void TheLadderHasEighteenStagesInSixGroupsOfThree_AsInTheTable()
        {
            Assert.AreEqual(18, ConstelacionContract.MaxLevel);
            // grupos, tamaño, gemelos, tríos, luces
            var expected = new[]
            {
                (3, 2, 0, 0, 6), (4, 2, 0, 0, 8), (5, 2, 0, 0, 10),
                (6, 2, 0, 0, 12), (7, 2, 0, 0, 14), (8, 2, 0, 0, 16),
                (6, 2, 1, 0, 12), (7, 2, 1, 0, 14), (8, 2, 2, 0, 16),
                (4, 3, 0, 0, 12), (5, 3, 0, 0, 15), (5, 3, 1, 0, 15),
                (6, 2, 0, 2, 14), (7, 2, 1, 2, 16), (8, 2, 1, 3, 19),
                (10, 2, 2, 0, 20), (11, 2, 3, 0, 22), (11, 2, 3, 2, 24),
            };
            for (int l = 1; l <= 18; l++)
            {
                var st = ConstelacionContract.Stage(l);
                var e = expected[l - 1];
                Assert.AreEqual((e.Item1, e.Item2, e.Item3, e.Item4), (st.Groups, st.Size, st.Twins, st.Trios), "etapa " + l);
                Assert.AreEqual(e.Item5, st.Lights, "luces de la etapa " + l);
                Assert.AreEqual((l - 1) / 3 + 1, ConstelacionContract.GroupOf(l));
            }
            Assert.AreEqual(6, ConstelacionContract.GroupOf(18));
            Assert.AreEqual(24, Enumerable.Range(1, 18).Max(l => ConstelacionContract.Stage(l).Lights), "el cielo más grande tiene 24 luces");
        }

        [Test]
        public void TheLadderNeverGetsEasier_EachGroupOfStagesAddsOrKeepsLights_AndStageOutOfRangeIsClamped()
        {
            Assert.AreEqual(6, ConstelacionContract.Stage(1).Lights);
            Assert.AreEqual(ConstelacionContract.Stage(1).Lights, ConstelacionContract.Stage(-4).Lights);
            Assert.AreEqual(ConstelacionContract.Stage(18).Lights, ConstelacionContract.Stage(99).Lights);
        }

        [Test]
        public void TheNewCardsComeAtTheRightStage_AndAnyoneStartingHigherSeesThemAll()
        {
            Assert.AreEqual(new[] { ConIntro.Cielo }, ConstelacionContract.IntrosFor(1).ToArray());
            Assert.AreEqual(new[] { ConIntro.Cielo }, ConstelacionContract.IntrosFor(6).ToArray());
            Assert.AreEqual(new[] { ConIntro.Cielo, ConIntro.Gemelos }, ConstelacionContract.IntrosFor(7).ToArray());
            Assert.AreEqual(new[] { ConIntro.Cielo, ConIntro.Gemelos, ConIntro.Trios }, ConstelacionContract.IntrosFor(10).ToArray());
            Assert.AreEqual(new[] { ConIntro.Cielo, ConIntro.Gemelos, ConIntro.Trios, ConIntro.Mezcla }, ConstelacionContract.IntrosFor(13).ToArray());
            Assert.AreEqual(new[] { ConIntro.Cielo, ConIntro.Gemelos, ConIntro.Trios, ConIntro.Mezcla, ConIntro.Grande }, ConstelacionContract.IntrosFor(16).ToArray());
            Assert.AreEqual(ConIntro.Gemelos, ConstelacionContract.Stage(7).Intro);
            Assert.AreEqual(ConIntro.Trios, ConstelacionContract.Stage(10).Intro);
            Assert.AreEqual(ConIntro.Mezcla, ConstelacionContract.Stage(13).Intro);
            Assert.AreEqual(ConIntro.Grande, ConstelacionContract.Stage(16).Intro);
        }

        // ------------------------------------------------------------------ el cielo armado

        [Test]
        public void EverySkyHasTheRightGroupsLightsTwinsAndTrios_InAllEighteenStages()
        {
            var rng = new Random(3);
            for (int l = 1; l <= 18; l++)
            {
                var st = ConstelacionContract.Stage(l);
                for (int rep = 0; rep < 25; rep++)
                {
                    var sky = ConstelacionSky.Create(st, rng, ConstelacionLayout.SkyW, ConstelacionLayout.RefSkyH);
                    Assert.AreEqual(st.Lights, sky.Lights.Length, "luces, etapa " + l);
                    var byGroup = sky.Lights.GroupBy(x => x.Group).ToList();
                    Assert.AreEqual(st.Groups, byGroup.Count, "grupos, etapa " + l);
                    foreach (var g in byGroup)
                    {
                        Assert.AreEqual(g.First().Size, g.Count(), "cada grupo trae sus luces completas");
                        Assert.AreEqual(1, g.Select(x => x.Key).Distinct().Count(), "un grupo, un solo objeto");
                    }
                    Assert.AreEqual(st.Trios + (st.Size == 3 ? st.Groups : 0), byGroup.Count(g => g.Count() == 3), "tríos, etapa " + l);
                    // gemelos: cada gemelo son DOS grupos del mismo objeto con variantes 0 y 1 (otra clave)
                    var twinKinds = sky.Lights.GroupBy(x => x.Kind).Where(k => k.Select(x => x.Variant).Distinct().Count() == 2).ToList();
                    Assert.AreEqual(st.Twins, twinKinds.Count, "gemelos, etapa " + l);
                    foreach (var k in twinKinds) Assert.IsTrue(ConstelacionContract.HasTwin(k.Key), "solo los objetos con gemelo lo tienen");
                    // los tríos de la mezcla nunca son de un gemelo
                    foreach (var g in byGroup.Where(g => st.Size == 2 && g.Count() == 3)) Assert.AreEqual(0, g.First().Variant + (twinKinds.Any(t => t.Key == g.First().Kind) ? 1 : 0), "un trío de la mezcla no es un gemelo");
                }
            }
        }

        // ------------------------------------------------------------------ reparto de las luces (§5)

        [Test]
        public void TheSkyIsDealtLikeStars_300DealsPerSize_FromSixToTwentyFourLights_NeverOverlappingNorOutside()
        {
            var rng = new Random(11);
            foreach (float h in new[] { ConstelacionLayout.RefSkyH, 450f, ConstelacionLayout.MaxSkyH })
                for (int n = 6; n <= 24; n++)
                {
                    float r = ConstelacionLayout.RadiusFor(n, ConstelacionLayout.SkyW, h);
                    Assert.GreaterOrEqual(r, 28f);
                    Assert.LessOrEqual(r, 38f);
                    for (int rep = 0; rep < 300; rep++)
                    {
                        var p = ConstelacionLayout.Place(n, ConstelacionLayout.SkyW, h, rng);
                        Assert.AreEqual(n, p.Points.Length);
                        Assert.GreaterOrEqual(ConstelacionLayout.MinSeparation(p.Points), 2f * p.R + 12f - 0.01f, $"{n} luces en {h} dp, reparto {rep}: dos luces muy juntas");
                        foreach (var pt in p.Points)
                        {
                            Assert.GreaterOrEqual(pt.X, p.R - 0.01f);
                            Assert.LessOrEqual(pt.X, ConstelacionLayout.SkyW - p.R + 0.01f);
                            Assert.GreaterOrEqual(pt.Y, p.R - 0.01f);
                            Assert.LessOrEqual(pt.Y, h - p.R + 0.01f);
                        }
                    }
                }
        }

        [Test]
        public void TheTouchTargetIsAtLeast56dp_AndTheNearestLightWinsWithinItsRadiusPlusTen()
        {
            for (int n = 6; n <= 24; n++)
            {
                float r = ConstelacionLayout.RadiusFor(n, ConstelacionLayout.SkyW, ConstelacionLayout.RefSkyH);
                Assert.GreaterOrEqual(2f * r, 56f - 0.01f, "una luz se toca en 56 dp o más");
            }
            var pts = new[] { new ConPt(50f, 50f), new ConPt(200f, 50f) };
            Assert.AreEqual(0, ConstelacionLayout.Nearest(pts, 30f, 50f + 39f, 50f), "dentro de R + 10");
            Assert.AreEqual(-1, ConstelacionLayout.Nearest(pts, 30f, 50f + 41f, 50f), "fuera de R + 10 no es un toque");
            Assert.AreEqual(1, ConstelacionLayout.Nearest(pts, 30f, 170f, 50f), "la más cercana");
        }

        [Test]
        public void TheLayoutOnScreen_FitsEveryPhone_AndNeverShrinksTheSkyBelow88Percent()
        {
            for (float height = 570f; height <= 900f; height += 5f)
            {
                var m = ConstelacionLayout.Compute(height);
                Assert.GreaterOrEqual(m.Scale, ConstelacionLayout.MinScale - 1e-4f);
                Assert.LessOrEqual(m.Scale, 1f);
                Assert.GreaterOrEqual(m.SkyH, ConstelacionLayout.RefSkyH - 1e-3f, "el cielo nunca es más bajo que el del boceto");
                Assert.LessOrEqual(m.SkyH, ConstelacionLayout.MaxSkyH + 1e-3f);
                Assert.GreaterOrEqual(m.SkyTop, m.CardTop + ConstelacionLayout.CardH, "el cielo va debajo de la tarjeta");
                Assert.GreaterOrEqual(m.RowTop, m.SkyTop + m.SkyH * m.Scale, "la fila «De memoria» va debajo del cielo");
                if (height >= 618f) Assert.LessOrEqual(m.Bottom + ConstelacionLayout.BottomMargin, height + 0.01f, "todo cabe en una pantalla de " + height);
            }
            Assert.AreEqual(1f, ConstelacionLayout.Compute(640f).Scale, 1e-4f, "en 16:9 (360 × 640) el cielo va a tamaño completo");
        }

        // ------------------------------------------------------------------ las reglas de un toque (§3)

        private static ConLight L(int kind, int group, int size, float x, float y, int variant = 0) =>
            new ConLight { Kind = kind, Variant = variant, Group = group, Size = size, Pos = new ConPt(x, y) };

        /// <summary>Un cielo a mano: 2 planetas (A), 2 cohetes (B) y 2 cometas (C); el reloj en 0 y todas pueden tocarse desde el 200.</summary>
        private static ConstelacionSky ThreePairs() => ConstelacionSky.FromLights(ConstelacionContract.Stage(1), new[]
        {
            L(0, 0, 2, 60f, 60f), L(0, 0, 2, 270f, 60f), L(1, 1, 2, 60f, 200f), L(1, 1, 2, 270f, 200f), L(2, 2, 2, 60f, 330f), L(2, 2, 2, 270f, 330f)
        }, 332f, 396f);

        private const float Now = 1000f;

        [Test]
        public void TheFirstLightOfATurnIsNeverJudged_AndAMatchOfTwoNewLightsIsLuck_NotMemory()
        {
            var sky = ThreePairs();
            var r1 = sky.Tap(0, Now);
            Assert.IsTrue(r1.Accepted && r1.FirstOfTurn);
            Assert.IsFalse(r1.Opportunity);
            Assert.AreEqual(0, sky.OpportunityCount);
            var r2 = sky.Tap(1, Now);
            Assert.IsTrue(r2.Matched, "eran iguales");
            Assert.IsFalse(r2.Opportunity, "la compañera no se había visto: no había nada que recordar");
            Assert.IsTrue(r2.GroupComplete());
            Assert.AreEqual(ConLine.New, sky.Links[0].Type, "a la primera vista: línea celeste");
            Assert.AreEqual(ConLine.New, sky.Lights[0].Ring);
            Assert.AreEqual(0, sky.OpportunityCount);
            Assert.AreEqual(0, sky.Streak, "la suerte no suma racha");
            Assert.AreEqual(1, sky.GroupsFound);
            Assert.AreEqual(0, sky.MemoryGroups);
        }

        [Test]
        public void GoingStraightToASeenPartnerIsAHit_GoldLine_AndTheStreakGrows()
        {
            var sky = ThreePairs();
            sky.Tap(0, Now);          // planeta nuevo
            sky.Tap(2, Now);          // cohete nuevo: no coincide (las dos quedan abiertas)
            var r = sky.Tap(3, Now);  // (cierra las anteriores) primera luz del turno: el segundo cohete, ya visto... pero es la PRIMERA del turno: no se juzga
            Assert.IsTrue(r.FirstOfTurn);
            Assert.AreEqual(0, sky.OpportunityCount);
            var r2 = sky.Tap(2, Now + 10f);   // la compañera del cohete ya vista y escondida → oportunidad y acierto
            Assert.IsTrue(r2.Accepted && r2.Opportunity && r2.Hit && r2.Matched);
            Assert.AreEqual(ConLine.Memory, sky.Links[0].Type, "de memoria: línea dorada");
            Assert.AreEqual(ConLine.Memory, sky.Lights[3].Ring);
            Assert.AreEqual(1, sky.Hits);
            Assert.AreEqual(1, sky.Streak);
            Assert.AreEqual(new[] { true }, sky.Opps.ToArray());
            Assert.AreEqual(1, sky.MemoryGroups);
        }

        [Test]
        public void TouchingAnotherLightWhenAPartnerWasSeen_IsAMiss_CutsTheStreak_AndTheSeenPartnerIsTheHint()
        {
            var s2 = ThreePairs();
            s2.Tap(2, Now);               // ve un cohete (nuevo)
            s2.Tap(4, Now);               // ve un cometa (no coincide, quedan abiertas)
            var first = s2.Tap(3, Now);   // primera luz del turno siguiente: el otro cohete
            Assert.IsTrue(first.FirstOfTurn);
            s2.Streak = 3;
            var miss = s2.Tap(4, Now);    // otra luz, con la compañera (la luz 2) ya vista y escondida: se te escapó
            Assert.IsTrue(miss.Opportunity && miss.Miss && !miss.Hit && !miss.Matched);
            Assert.AreEqual(0, s2.Streak, "un «se te escapó» corta la racha");
            Assert.AreEqual(1, s2.Misses);
            Assert.AreEqual(new[] { false }, s2.Opps.ToArray());
            Assert.AreEqual(new[] { s2.Lights[2] }, miss.Kin.ToArray(), "donde brilla la pista: la compañera ya vista, sin darse vuelta");
            Assert.AreEqual(ConState.Down, s2.Lights[2].State, "la pista no la da vuelta");
            Assert.AreEqual(2, miss.Pending.Count, "las dos abiertas esperan");
            Assert.AreEqual(Now + 850f, miss.PendingCloseAtMs, 0.01f, "se cierran a los 850 ms");
        }

        [Test]
        public void ExploringNewLightsOrFailingWithoutAnOpportunityNeverCutsTheStreak()
        {
            var sky = ThreePairs();
            sky.Streak = 4;
            sky.BestStreak = 4;
            sky.Tap(0, Now);                      // planeta
            var r = sky.Tap(2, Now);              // cohete: no coincide, y el planeta no tenía compañera vista → sin oportunidad
            Assert.IsFalse(r.Opportunity);
            Assert.IsFalse(r.Miss);
            Assert.AreEqual(4, sky.Streak, "fallar sin oportunidad no corta la racha");
            Assert.AreEqual(0, sky.Misses);
            Assert.AreEqual(0, sky.OpportunityCount);
        }

        [Test]
        public void NothingBlocksTheTap_ALightCanBeTouchedWhileTwoOthersAreStillOpen_AndThoseCloseAtOnce()
        {
            var sky = ThreePairs();
            sky.Tap(0, Now);
            var wrong = sky.Tap(2, Now);                       // no coinciden: quedan abiertas
            Assert.AreEqual(2, sky.Pending.Count);
            Assert.AreEqual(ConState.Up, sky.Lights[0].State);
            Assert.AreEqual(ConState.Up, sky.Lights[2].State);
            var next = sky.Tap(4, Now + 100f);                 // se toca otra luz AL INSTANTE (no espera a los 850 ms)
            Assert.IsTrue(next.Accepted, "nada bloquea el toque");
            Assert.AreEqual(2, next.ClosedNow.Count, "las dos abiertas se cierran al instante");
            Assert.AreEqual(ConState.Down, sky.Lights[0].State);
            Assert.AreEqual(ConState.Down, sky.Lights[2].State);
            Assert.AreEqual(ConState.Up, sky.Lights[4].State, "y el toque nuevo cuenta");
            Assert.IsTrue(next.FirstOfTurn);
        }

        [Test]
        public void TheOpenLightsCloseAloneAfter850ms_AndNotBefore()
        {
            var sky = ThreePairs();
            sky.Tap(0, Now);
            sky.Tap(2, Now);
            Assert.AreEqual(0, sky.Tick(Now + 849f).Count);
            Assert.AreEqual(2, sky.Pending.Count);
            Assert.AreEqual(2, sky.Tick(Now + 850f).Count);
            Assert.AreEqual(0, sky.Pending.Count);
            Assert.AreEqual(ConState.Down, sky.Lights[0].State);
            Assert.IsTrue(sky.Lights[0].Seen, "aunque se cierren, ya se vieron");
        }

        [Test]
        public void ALightCannotBeTouchedBeforeItAppears_ButNothingElseBlocksIt()
        {
            var sky = ThreePairs();
            sky.Lights[0].AppearAtMs = 500f;
            Assert.IsFalse(sky.CanTap(0, 600f), "recién aparece (150 ms)");
            Assert.IsTrue(sky.CanTap(0, 650f), "apenas aparece se puede tocar");
            Assert.IsFalse(sky.Tap(0, 600f).Accepted);
            Assert.IsTrue(sky.Tap(0, 650f).Accepted);
            Assert.IsFalse(sky.Tap(0, 700f).Accepted, "una luz abierta no se vuelve a tocar");
        }

        [Test]
        public void AUselessTurnIsASeenLightThatWasNotAPartner_WithNothingToRemember()
        {
            var sky = ThreePairs();
            sky.Tap(0, Now);                       // planeta (nuevo)
            sky.Tap(2, Now);                       // cohete: no coincide
            sky.Tap(4, Now);                       // (cierra) primera luz del turno: cometa nuevo
            var useless = sky.Tap(2, Now);         // el cohete ya visto, no es compañero del cometa y no hay cometa visto → vuelta inútil
            Assert.IsTrue(useless.Useless);
            Assert.IsFalse(useless.Opportunity);
            Assert.AreEqual(1, sky.Useless);
        }

        [Test]
        public void ATrioNeedsThreeLights_AndWhenTwoAreOpenTheThirdIsMissing()
        {
            var st = ConstelacionContract.Stage(10);
            var sky = ConstelacionSky.FromLights(st, new[]
            {
                L(0, 0, 3, 50f, 60f), L(0, 0, 3, 150f, 60f), L(0, 0, 3, 250f, 60f),
                L(1, 1, 3, 50f, 200f), L(1, 1, 3, 150f, 200f), L(1, 1, 3, 250f, 200f),
                L(2, 2, 3, 50f, 330f), L(2, 2, 3, 150f, 330f), L(2, 2, 3, 250f, 330f),
                L(3, 3, 3, 100f, 380f), L(3, 3, 3, 200f, 380f), L(3, 3, 3, 300f, 380f),
            }, 332f, 396f);
            sky.Tap(0, Now);
            var second = sky.Tap(1, Now);
            Assert.IsTrue(second.Matched);
            Assert.IsTrue(second.ThirdMissing, "«¡Falta la tercera!»");
            Assert.IsFalse(second.Completed.Any(), "con dos no se completa un trío");
            Assert.AreEqual(2, sky.Face.Count, "el turno sigue");
            var third = sky.Tap(2, Now);
            Assert.IsTrue(third.Completed.Count == 3);
            Assert.AreEqual(2, third.NewLinks.Count, "un trío se une con dos líneas");
            Assert.AreEqual(Now + 200f, sky.Links[1].AtMs, "la segunda sale 200 ms después");
            Assert.AreEqual(sky.Lights[1], sky.Links[1].A, "A-B y B-C");
            Assert.AreEqual(sky.Lights[1], sky.Links[0].B);
            Assert.AreEqual(3, sky.TriosLeft);
            Assert.AreEqual(0, sky.PairsLeft);
        }

        [Test]
        public void ATrioFoundWithOneSeenLightIsMemory_AndTheTypeOfEachLineIsThatOfItsArrivalLight()
        {
            var sky = ConstelacionSky.FromLights(ConstelacionContract.Stage(10), new[]
            {
                L(0, 0, 3, 50f, 60f), L(0, 0, 3, 150f, 60f), L(0, 0, 3, 250f, 60f),
                L(1, 1, 3, 50f, 200f), L(1, 1, 3, 150f, 200f), L(1, 1, 3, 250f, 200f),
            }, 332f, 396f);
            sky.Tap(0, Now);        // planeta
            sky.Tap(3, Now);        // cohete: no coincide
            sky.Tap(1, Now);        // (cierra) primera luz del turno: planeta 2
            sky.Tap(0, Now);        // planeta 1 (visto): compañera ya vista → acierto de memoria... (había oportunidad)
            Assert.AreEqual(ConLine.Memory, sky.Types[0], "la segunda llegó de memoria: línea dorada");
            var third = sky.Tap(2, Now);   // la tercera es nueva: celeste
            Assert.AreEqual(ConLine.Memory, sky.Links[0].Type);
            Assert.AreEqual(ConLine.New, sky.Links[1].Type, "el tipo es el de la luz de llegada: la tercera era nueva");
            Assert.AreEqual(ConLine.Memory, sky.Lights[2].Ring, "el aro de las tres es el del grupo: dorado si alguna llegada fue de memoria");
            Assert.AreEqual(1, sky.MemoryGroups);
        }

        [Test]
        public void MixedSky_APairIsDoneWithTwo_AndATrioWithThree_AndTheCardCountsWhatIsLeft()
        {
            var st = ConstelacionContract.Stage(13);
            var rng = new Random(5);
            var sky = ConstelacionSky.Create(st, rng, 332f, 396f);
            Assert.AreEqual(st.Groups - st.Trios, sky.PairsLeft);
            Assert.AreEqual(st.Trios, sky.TriosLeft);
            // se resuelve un par de verdad
            var pair = sky.Lights.Where(l => l.Size == 2).GroupBy(l => l.Group).First().ToList();
            sky.Tap(pair[0].Id, 5000f);
            Assert.IsTrue(sky.Tap(pair[1].Id, 5000f).Completed.Count == 2);
            Assert.AreEqual(st.Groups - st.Trios - 1, sky.PairsLeft);
            Assert.AreEqual(st.Trios, sky.TriosLeft);
        }

        // ------------------------------------------------------------------ partidas jugadas por un robot: nunca se traba y la memoria se mide bien

        private sealed class Bot
        {
            public readonly ConstelacionSky Sky;
            private readonly Random _rng;
            private readonly double _forget;
            private readonly HashSet<int> _known = new HashSet<int>();   // lo que «recuerda»
            public int Taps;
            public Bot(ConstelacionSky sky, Random rng, double forget) { Sky = sky; _rng = rng; _forget = forget; }

            /// <summary>Juega el cielo entero. Con memoria perfecta (olvido 0) va directo a toda compañera que ya vio; si olvida, prueba otra luz al azar.</summary>
            public bool Play(float startMs)
            {
                float now = startMs + 2000f;      // ya aparecieron todas
                for (int guard = 0; guard < 2000 && !Sky.Complete; guard++)
                {
                    now += 200f;
                    var down = Sky.Lights.Where(l => l.State == ConState.Down).ToList();
                    if (down.Count == 0) { Sky.Tick(now + 900f); continue; }
                    ConLight pick;
                    if (Sky.Face.Count == 0)
                    {
                        // primera luz del turno: una nueva si hay (explorar), si no cualquiera
                        var fresh = down.Where(l => !l.Seen).ToList();
                        pick = fresh.Count > 0 ? fresh[_rng.Next(fresh.Count)] : down[_rng.Next(down.Count)];
                    }
                    else
                    {
                        var first = Sky.Face[0];
                        var seenKin = down.Where(l => l.Key == first.Key && l.Seen && _known.Contains(l.Id) && _rng.NextDouble() >= _forget).ToList();
                        if (seenKin.Count > 0) pick = seenKin[0];
                        else
                        {
                            var fresh = down.Where(l => !l.Seen).ToList();
                            pick = fresh.Count > 0 ? fresh[_rng.Next(fresh.Count)] : down[_rng.Next(down.Count)];
                        }
                    }
                    var res = Sky.Tap(pick.Id, now);
                    Assert.IsTrue(res.Accepted, "el robot toca una luz dormida y visible: siempre se acepta");
                    Taps++;
                    if (res.Opened != null) _known.Add(res.Opened.Id);
                }
                return Sky.Complete;
            }
        }

        [Test]
        public void ABotWithPerfectMemory_FinishesEverySky_AndEveryOpportunityIsAHit()
        {
            var rng = new Random(21);
            foreach (int level in new[] { 1, 6, 7, 10, 13, 16, 18 })
                for (int rep = 0; rep < 20; rep++)
                {
                    var sky = ConstelacionSky.Create(ConstelacionContract.Stage(level), rng, 332f, 396f);
                    var bot = new Bot(sky, rng, 0.0);
                    Assert.IsTrue(bot.Play(0f), "etapa " + level + ": el cielo se completa");
                    Assert.AreEqual(sky.OpportunityCount, sky.Hits, "con memoria perfecta no se escapa ninguna (etapa " + level + ")");
                    Assert.AreEqual(0, sky.Misses);
                    Assert.AreEqual(ConstelacionContract.Stage(level).Groups, sky.GroupsFound);
                    Assert.AreEqual(sky.Links.Count, sky.Lights.Length - sky.GroupsFound, "una línea menos que luces por grupo (una por pareja, dos por trío)");
                }
        }

        [Test]
        public void ABotThatForgets_StillFinishesEverySky_AndItsMemoryScoreFollowsHowMuchItForgets()
        {
            var rng = new Random(77);
            double[] pct = new double[3];
            double[] forget = { 0.0, 0.3, 0.8 };
            for (int f = 0; f < 3; f++)
            {
                int hits = 0, opps = 0;
                for (int rep = 0; rep < 60; rep++)
                {
                    var sky = ConstelacionSky.Create(ConstelacionContract.Stage(1 + rng.Next(18)), rng, 332f, 396f);
                    Assert.IsTrue(new Bot(sky, rng, forget[f]).Play(0f), "nunca se traba");
                    hits += sky.Hits;
                    opps += sky.OpportunityCount;
                }
                Assert.Greater(opps, 100);
                pct[f] = 100.0 * hits / opps;
            }
            Assert.AreEqual(100.0, pct[0], 0.001, "memoria perfecta: 100 %");
            Assert.Less(pct[1], pct[0], "olvidar baja la medida");
            Assert.Less(pct[2], pct[1], "olvidar más la baja más");
        }

        // ------------------------------------------------------------------ líneas (§5)

        [Test]
        public void LinesBetweenCloseLightsAreStraight_AndCurvesNeverDriftMoreThan50dp()
        {
            float r = 30f;
            var lights = new List<ConPt> { new ConPt(100f, 100f), new ConPt(180f, 100f) };
            var (cx, cy) = ConstelacionLayout.Route(lights[0], lights[1], r, 332f, 396f, lights, new List<ConLinkGeo>());
            Assert.AreEqual(140f, cx, 0.01f, "cerca (menos de 4R) va recta");
            Assert.AreEqual(100f, cy, 0.01f);

            var rng = new Random(2);
            for (int rep = 0; rep < 200; rep++)
            {
                var p = ConstelacionLayout.Place(14, 332f, 396f, rng);
                var a = p.Points[0];
                var b = p.Points[1];
                var (qx, qy) = ConstelacionLayout.Route(a, b, p.R, 332f, 396f, p.Points, new List<ConLinkGeo>());
                var geo = new ConLinkGeo { A = a, B = b, Cx = qx, Cy = qy };
                var mid = geo.At(0.5f);
                float straightMidX = (a.X + b.X) / 2f, straightMidY = (a.Y + b.Y) / 2f;
                Assert.LessOrEqual(Math.Sqrt((mid.X - straightMidX) * (mid.X - straightMidX) + (mid.Y - straightMidY) * (mid.Y - straightMidY)), 50.01, "el desvío máximo es de ±50");
            }
        }

        [Test]
        public void ALineIsBornAtTheEdgeOfItsFirstLight_AndDiesAtTheEdgeOfTheSecond()
        {
            float r = 30f;
            var geo = new ConLinkGeo { A = new ConPt(40f, 100f), B = new ConPt(240f, 140f), Cx = 140f, Cy = 80f };
            ConstelacionLayout.Trim(geo, r);
            Assert.Greater(geo.T0, 0f);
            Assert.Less(geo.T1, 1f);
            Assert.Greater(ConPt.Dist(geo.At(geo.T0), geo.A), r + 4f, "nace afuera de su luz");
            Assert.Greater(ConPt.Dist(geo.At(geo.T1), geo.B), r + 4f, "muere afuera de su luz");
            Assert.Less(geo.T0, geo.T1);
        }

        // ------------------------------------------------------------------ motor común, medida y textos

        [Test]
        public void TheCommonEngineRunsOnEighteenStages_AndEveryOpportunityIsATrial()
        {
            var cfg = new SequenceConfigDetails { age_band = "ADULT", level = 3, base_intensity = 0 };
            var dda = ConstelacionContract.CreateEngine(cfg);
            Assert.AreEqual(18, dda.MaxLevel);
            float start = dda.Rating;
            dda.Register(true);
            dda.Register(true);
            Assert.Greater(dda.Rating, start, "los aciertos de memoria suben");
            Assert.AreEqual(2, dda.Trials);
            float before = dda.Rating;
            dda.Register(false);
            Assert.Less(dda.Rating, before, "un «se te escapó» baja");
        }

        [Test]
        public void ASkyWithNoOpportunitiesNeverMovesTheRating_AndAPerfectOneRisesWhileManyMissesLower()
        {
            var cfg = new SequenceConfigDetails { age_band = "ADULT", level = 3 };
            var dda = ConstelacionContract.CreateEngine(cfg);
            float r0 = dda.Rating;
            Assert.AreEqual(r0, dda.Rating, "sin ensayos no se mueve");
            for (int i = 0; i < 8; i++) dda.Register(true);
            Assert.GreaterOrEqual(dda.Level, (int)Math.Floor(r0) + 1, "un cielo perfecto con varias oportunidades sube una etapa");
            var dda2 = ConstelacionContract.CreateEngine(cfg);
            float s = dda2.Rating;
            dda2.Register(true); dda2.Register(true); dda2.Register(false); dda2.Register(false); dda2.Register(false);
            Assert.Less(dda2.Rating, s, "tres o más «se te escapó» bajan");
        }

        [Test]
        public void TheDebugStageStartsTheEngineExactlyThere_AndTheSavedRatingIsTheNormalStart()
        {
            var cfg = new SequenceConfigDetails { age_band = "ADULT", con_stage = 13 };
            Assert.AreEqual(13, ConstelacionContract.CreateEngine(cfg).Level);
            var saved = new SequenceConfigDetails { age_band = "ADULT", has_dda_rating = true, dda_rating = 0.5f };
            Assert.AreEqual(1f + 0.5f * 18f, ConstelacionContract.CreateEngine(saved).Rating, 0.001f);
        }

        [Test]
        public void TheScoreIsTheShareOfOpportunitiesWonFromMemory_AndNoOpportunitiesIsNotABadGame()
        {
            Assert.AreEqual(100, ConstelacionContract.Score(0, 0));
            Assert.AreEqual(70, ConstelacionContract.Score(7, 10));
            Assert.AreEqual(100, ConstelacionContract.Score(12, 10));
            Assert.AreEqual(0, ConstelacionContract.Score(-3, 10));
            Assert.AreEqual(6, ConstelacionContract.NewRecord(2, 6));
            Assert.AreEqual(6, ConstelacionContract.NewRecord(6, 2), "el récord nunca baja");
            Assert.AreEqual(0, ConstelacionContract.NewRecord(-1, -4));
        }

        [Test]
        public void TheTallyAddsEveryBoard_AndTheTelemetryCarriesTheMeasure()
        {
            var rng = new Random(9);
            var tally = new ConTally();
            for (int b = 0; b < 3; b++)
            {
                var sky = ConstelacionSky.Create(ConstelacionContract.Stage(4 + b), rng, 332f, 396f);
                new Bot(sky, rng, 0.2).Play(0f);
                tally.AddBoard(sky, 4 + b);
            }
            Assert.AreEqual(3, tally.Boards);
            Assert.AreEqual(6 + 7 + 8, tally.Groups);
            Assert.AreEqual(6, tally.PeakLevel);
            var cfg = new SequenceConfigDetails { age_band = "ADULT", level = 2, timed = true };
            var dda = ConstelacionContract.CreateEngine(cfg);
            var m = ConstelacionContract.BuildMetrics(dda, tally, 5, true, ConstelacionContract.Score(tally.Hits, tally.Opportunities), cfg);
            Assert.AreEqual(tally.Hits, m.con_hits);
            Assert.AreEqual(tally.Opportunities, m.con_opps);
            Assert.AreEqual(tally.Groups, m.con_groups);
            Assert.AreEqual(tally.MemGroups, m.con_mem_groups);
            Assert.AreEqual(tally.BestStreak, m.con_best_streak);
            Assert.AreEqual(tally.Useless, m.con_useless);
            Assert.AreEqual(tally.Turns, m.con_turns);
            Assert.AreEqual(2, m.con_group, "la etapa más alta fue la 6: grupo 2 de 6");
            Assert.AreEqual(5, m.con_best);
            Assert.AreEqual(1, m.con_new);
            Assert.IsTrue(m.timed);
            Assert.AreEqual(dda.RatingNormalized, m.end_rating, 1e-6f);
            // sin dato: un cielo sin jugar no inventa valores
            var empty = new CardsSessionMetrics();
            Assert.AreEqual(-1, empty.con_hits);
            Assert.AreEqual(-1, empty.con_group);
        }

        [Test]
        public void TheTextsAreAsInTheDesign_AtLeast14dpIsCheckedInTheView_AndNothingPromisesHealth()
        {
            var st1 = ConstelacionContract.Stage(1);
            Assert.AreEqual("Busca las parejas", ConstelacionContract.CardTitle(st1));
            Assert.AreEqual("Faltan 3 parejas", ConstelacionContract.CardSub(st1, 3, 0));
            Assert.AreEqual("Faltan 1 pareja", ConstelacionContract.CardSub(st1, 1, 0));
            Assert.AreEqual("Busca tres iguales", ConstelacionContract.CardTitle(ConstelacionContract.Stage(10)));
            Assert.AreEqual("Faltan 2 tríos", ConstelacionContract.CardSub(ConstelacionContract.Stage(10), 0, 2));
            Assert.AreEqual("Ojo con los gemelos", ConstelacionContract.CardTitle(ConstelacionContract.Stage(7)));
            Assert.AreEqual("Solo se unen los idénticos · faltan 4 parejas", ConstelacionContract.CardSub(ConstelacionContract.Stage(7), 4, 0));
            Assert.AreEqual("Parejas y tríos", ConstelacionContract.CardTitle(ConstelacionContract.Stage(13)));
            Assert.AreEqual("Faltan 2 parejas y 1 trío", ConstelacionContract.CardSub(ConstelacionContract.Stage(13), 2, 1));
            Assert.AreEqual("Gemelos, parejas y tríos", ConstelacionContract.CardTitle(ConstelacionContract.Stage(14)));
            Assert.AreEqual("Cielo 2 de 6", ConstelacionContract.SkyOf(2, 6));
            Assert.AreEqual("3 de 4 de memoria", ConstelacionContract.DoneLine(3, 4, false));
            Assert.AreEqual("3 de 4 de memoria · ¡racha récord!", ConstelacionContract.DoneLine(3, 4, true));
            Assert.AreEqual("Todas a la primera vista: ¡qué suerte!", ConstelacionContract.DoneLine(0, 0, false));
            Assert.AreEqual("aún nada que recordar", ConstelacionContract.MemoryCount(0, 0));
            Assert.AreEqual("2 de 5", ConstelacionContract.MemoryCount(2, 5));
            StringAssert.StartsWith("Truco: da vuelta primero una luz nueva", ConstelacionContract.Tip(0));
            foreach (var intro in new[] { ConIntro.Cielo, ConIntro.Gemelos, ConIntro.Trios, ConIntro.Mezcla, ConIntro.Grande })
                Assert.AreEqual(3, ConstelacionContract.IntroLines(intro).Length, "cada tarjeta explica con 3 líneas");
            Assert.AreEqual("CONSTELACIONES", ConstelacionContract.IntroTag(ConIntro.Cielo));
            Assert.AreEqual("NUEVO", ConstelacionContract.IntroTag(ConIntro.Grande));
            var all = new List<string> { ConstelacionContract.MissTitle, ConstelacionContract.MissSub, ConstelacionContract.CardDoneTitle, ConstelacionContract.LuckyLine, ConstelacionContract.GoldLegend };
            all.AddRange(ConstelacionContract.AllTips());
            foreach (var i in new[] { ConIntro.Cielo, ConIntro.Gemelos, ConIntro.Trios, ConIntro.Mezcla, ConIntro.Grande }) all.AddRange(ConstelacionContract.IntroLines(i));
            foreach (var text in all)
                foreach (var banned in new[] { "alzheimer", "demencia", "previene", "diagn", "neurona", "error", "fallaste", "mal" })
                    StringAssert.DoesNotContain(banned, text.ToLowerInvariant(), "texto sin culpa ni promesas de salud: " + text);
        }

        [Test]
        public void TheObjectsAreTwelveWithFourTwins_AndTheNotesClimbTheScaleAndRepeatAnOctaveUp()
        {
            Assert.AreEqual(12, ConstelacionContract.ObjectKinds);
            Assert.AreEqual(12, ConstelacionContract.ObjectNames.Length);
            CollectionAssert.AreEqual(new[] { 0, 1, 3, 5 }, ConstelacionContract.TwinKinds);
            Assert.AreEqual("el planeta sin anillo", ConstelacionContract.ArticleName(0, 1));
            Assert.AreEqual("la brújula", ConstelacionContract.ArticleName(11, 0));
            Assert.AreEqual(ConstelacionContract.Penta[0], ConstelacionContract.NoteHz(0), 0.001f);
            Assert.AreEqual(ConstelacionContract.Penta[3], ConstelacionContract.NoteHz(3), 0.001f);
            Assert.AreEqual(ConstelacionContract.Penta[0] * 2f, ConstelacionContract.NoteHz(10), 0.001f, "del 10 en adelante repite una octava arriba");
            for (int g = 1; g < 10; g++) Assert.Greater(ConstelacionContract.NoteHz(g), ConstelacionContract.NoteHz(g - 1));
        }
    }

    internal static class ConTestExtensions
    {
        /// <summary>true si el toque completó un grupo.</summary>
        public static bool GroupComplete(this ConTapResult r) => r.Completed.Count > 0;
    }
}
