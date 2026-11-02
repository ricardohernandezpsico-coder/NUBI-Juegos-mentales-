using System.Collections.Generic;
using NeuroVida.Contracts;
using NeuroVida.Games.Acoplamiento;
using NeuroVida.Games.Disparate;
using NeuroVida.Games.Radar;
using NeuroVida.Games.Shared;
using NeuroVida.Games.Stroop;
using NUnit.Framework;
using UnityEngine;

namespace NeuroVida.Games.Tests
{
    /// <summary>
    /// El Contrarreloj, fase 1 (Tarea 176; <c>docs/contrarreloj.md</c>): la PISTA (la misma semilla da la misma pista, el paso sube por aciertos, cada ensayo tiene su propio azar), la pausa que crece al fallar, el mínimo antes de responder, los textos y la telemetría. Lógica pura: sin escena.
    /// Que adivinar no gane lo prueba <see cref="TimeTrialFairnessTests"/>.
    /// </summary>
    public class TimeTrialTests
    {
        private const int Day = 20261026;
        private static readonly string[] Games = { "stroop", "disparate", "radar", "acoplamiento" };

        // ------------------------------------------------------------------ la tabla de pistas

        [Test]
        public void TheFirstBatchIsTheFourGames_InTheirRotationOrder_AndCargaExactaIsNotIn()
        {
            CollectionAssert.AreEqual(new[] { "stroop", "disparate", "radar", "acoplamiento" }, TimeTrial.GameIds);
            foreach (var g in Games) { Assert.IsTrue(TimeTrial.Supports(g), g); Assert.GreaterOrEqual(TimeTrial.TrackVersion(g), 1, g); }
            Assert.IsFalse(TimeTrial.Supports("calculo"), "Carga exacta no entra: a Ricardo no le gusta");
            Assert.IsFalse(TimeTrial.Supports("piloto"), "Piloto queda para la fase 3");
            Assert.IsFalse(TimeTrial.Supports(null));
        }

        [Test]
        public void EveryGameHasItsOwnStep_AndItsOwnLadderLength()
        {
            // Tinta o Palabra: 5 pasos, uno cada 8 aciertos. Disparate y Rescate: 12 pasos, uno cada 3. Acoplamiento: 12 pasos, uno cada 2 (con «cada 3» nadie pasaría del 8 en 2 minutos).
            Assert.AreEqual((8, 5), (TimeTrial.StepEvery("stroop"), TimeTrial.MaxLevel("stroop")));
            Assert.AreEqual((3, 12), (TimeTrial.StepEvery("disparate"), TimeTrial.MaxLevel("disparate")));
            Assert.AreEqual((3, 12), (TimeTrial.StepEvery("radar"), TimeTrial.MaxLevel("radar")));
            Assert.AreEqual((2, 12), (TimeTrial.StepEvery("acoplamiento"), TimeTrial.MaxLevel("acoplamiento")));
            // y cada pista tiene tantos pasos como la escalera del juego (los generadores aceptan el nivel)
            Assert.AreEqual(StroopContract.MaxLevel, TimeTrial.MaxLevel("stroop"));
            Assert.AreEqual(DisparateContract.MaxLevel, TimeTrial.MaxLevel("disparate"));
            Assert.AreEqual(RadarContract.MaxLevel, TimeTrial.MaxLevel("radar"));
            Assert.AreEqual(DockingContract.MaxLevel, TimeTrial.MaxLevel("acoplamiento"));
        }

        [Test]
        public void TheLadderAlwaysStartsAtStepOne_AndClimbsOneStepEveryN_AndStaysAtTheTop()
        {
            foreach (var g in Games)
            {
                int n = TimeTrial.StepEvery(g), max = TimeTrial.MaxLevel(g);
                Assert.AreEqual(1, TimeTrial.LevelFor(g, 0), g + ": siempre desde el paso 1");
                Assert.AreEqual(1, TimeTrial.LevelFor(g, n - 1), g);
                Assert.AreEqual(2, TimeTrial.LevelFor(g, n), g + ": el paso 2 llega al acierto " + n);
                Assert.AreEqual(3, TimeTrial.LevelFor(g, 2 * n), g);
                Assert.AreEqual(max, TimeTrial.LevelFor(g, (max - 1) * n), g + ": el último paso llega al acierto " + (max - 1) * n);
                Assert.AreEqual(max, TimeTrial.LevelFor(g, 1000), g + ": en el último se queda");
                for (int hits = 0; hits < 60; hits++)
                {
                    int level = TimeTrial.LevelFor(g, hits);
                    Assert.LessOrEqual(level - (hits > 0 ? TimeTrial.LevelFor(g, hits - 1) : 1), 1, g + ": nunca salta más de un paso");
                    Assert.GreaterOrEqual(level, 1);
                    Assert.LessOrEqual(level, max);
                }
                for (int level = 1; level <= max; level++) Assert.AreEqual(level, TimeTrial.LevelFor(g, TimeTrial.HitsToReach(g, level)), g + " paso " + level);
            }
            // los números de Fable: el 5.º paso de Tinta llega al acierto 32; el 12.º de Disparate y Rescate, al 33; el de Acoplamiento, al 22
            Assert.AreEqual(32, TimeTrial.HitsToReach("stroop", 5));
            Assert.AreEqual(33, TimeTrial.HitsToReach("disparate", 12));
            Assert.AreEqual(33, TimeTrial.HitsToReach("radar", 12));
            Assert.AreEqual(22, TimeTrial.HitsToReach("acoplamiento", 12));
        }

        // ------------------------------------------------------------------ la semilla

        [Test]
        public void TheSeedIsTheDay_TheTrackVersion_AndTheGame()
        {
            foreach (var g in Games) Assert.AreEqual(TimeTrial.Seed(Day, g), TimeTrial.Seed(Day, g), g);
            Assert.AreNotEqual(TimeTrial.Seed(Day, "stroop"), TimeTrial.Seed(Day + 1, "stroop"), "mañana, otra pista");
            Assert.AreNotEqual(TimeTrial.Seed(Day, "stroop"), TimeTrial.Seed(Day, "disparate"), "otro juego, otra pista el mismo día");
            Assert.AreNotEqual(TimeTrial.Seed(Day, "radar"), TimeTrial.Seed(Day, "acoplamiento"));
            // valores fijos (de la VERSIÓN 2 de la pista: la versión entra en la semilla): la pista del día no cambia de un teléfono ni de una versión de Unity a otra (SplitMix64 propio, no System.Random)
            Assert.AreEqual(3509569136056027780UL, TimeTrial.Seed(20261026, "stroop"));
            Assert.AreEqual(7850226025669989732UL, TimeTrial.Seed(20261026, "disparate"));
            Assert.AreEqual(8221759096789279619UL, TimeTrial.Seed(20261027, "stroop"));
        }

        // ------------------------------------------------------------------ una pista nueva en cada partida (Tarea 210)

        private static SequenceInitConfig TrialConfig(string game) => new SequenceInitConfig { game_id = game, config = new SequenceConfigDetails { time_trial = true, track_seed = Day } };

        [Test]
        public void EveryRunOfTheSameDayAndGame_GetsItsOwnSeed_ButTheDaySeedStaysPureAndStable()
        {
            foreach (var g in Games)
            {
                var a = TimeTrial.Begin(TrialConfig(g));
                var b = TimeTrial.Begin(TrialConfig(g));
                Assert.AreNotEqual(a.Seed, b.Seed, g + ": dos partidas del mismo día y juego ya no comparten pista");
                Assert.AreNotEqual(TimeTrial.Seed(Day, g), a.Seed, g + ": ni la del día a secas");
                Assert.AreNotEqual(0UL, a.GameNumber, g);
                Assert.AreEqual(TimeTrial.SeedOfGame(TimeTrial.Seed(Day, g), a.GameNumber), a.Seed, g + ": la semilla de la partida sale del día y de su número");
                Assert.AreEqual(TimeTrial.Seed(Day, g), TimeTrial.Seed(Day, g), g + ": Seed sigue siendo una función pura");
            }
            // los valores fijos de la semilla del día NO cambian (las simulaciones y la referencia dependen de ellos)
            Assert.AreEqual(3509569136056027780UL, TimeTrial.Seed(20261026, "stroop"));
            Assert.AreEqual(7850226025669989732UL, TimeTrial.Seed(20261026, "disparate"));
            Assert.AreEqual(15825842088206235552UL, TimeTrial.Seed(20261026, "radar"));
        }

        [Test]
        public void SeedOfGame_IsPure_AndNumberZeroLeavesTheDaySeed()
        {
            ulong day = TimeTrial.Seed(Day, "radar");
            Assert.AreEqual(day, TimeTrial.SeedOfGame(day, 0UL), "con 0, la pista del día a secas: las simulaciones y las pruebas");
            Assert.AreEqual(TimeTrial.SeedOfGame(day, 12345UL), TimeTrial.SeedOfGame(day, 12345UL));
            Assert.AreNotEqual(day, TimeTrial.SeedOfGame(day, 12345UL));
            Assert.AreNotEqual(TimeTrial.SeedOfGame(day, 12345UL), TimeTrial.SeedOfGame(day, 12346UL), "otro número, otra pista");
            Assert.AreNotEqual(TimeTrial.SeedOfGame(day, 12345UL), TimeTrial.SeedOfGame(TimeTrial.Seed(Day, "stroop"), 12345UL), "el mismo número en otro juego, otra pista");
            Assert.AreEqual(day, new TimeTrial.Run("radar", Day).Seed, "new Run(juego, día) sin número = la semilla del día (así las simulaciones dan lo de siempre)");
            Assert.AreEqual(TimeTrial.SeedOfGame(day, 99UL), new TimeTrial.Run("radar", Day, 99UL).Seed);
        }

        [Test]
        public void TheGameNumber_IsNeverZero_NeverRepeats_AndATestCanFixIt()
        {
            var seen = new HashSet<ulong>();
            for (int i = 0; i < 2000; i++)
            {
                ulong n = TimeTrial.NewGameNumber();
                Assert.AreNotEqual(0UL, n, "0 quiere decir «sin número»");
                Assert.IsTrue(seen.Add(n), "el número " + n + " salió dos veces");
            }
            var source = TimeTrial.GameNumberSource;
            try
            {
                TimeTrial.GameNumberSource = () => 0UL;
                Assert.AreEqual(TimeTrial.Seed(Day, "disparate"), TimeTrial.Begin(TrialConfig("disparate")).Seed, "con el número fijo en 0, Begin vuelve a la pista del día");
                TimeTrial.GameNumberSource = () => 777UL;
                Assert.AreEqual(TimeTrial.Begin(TrialConfig("radar")).Seed, TimeTrial.Begin(TrialConfig("radar")).Seed, "con el mismo número, la misma pista");
            }
            finally { TimeTrial.GameNumberSource = source; }
        }

        [Test]
        public void RescateRounds_ComeOutDifferentInEveryRun_SoTheSequenceCannotBeLearned()
        {
            // lo que vio Ricardo: tres veces el Contrarreloj de Rescate y las cápsulas salían en el mismo orden
            var a = TimeTrial.Begin(TrialConfig("radar"));
            var b = TimeTrial.Begin(TrialConfig("radar"));
            int same = 0;
            for (int k = 0; k < 20; k++)
            {
                int level = TimeTrial.LevelFor("radar", k);
                var x = RadarContract.NextRound(level, TimeTrial.TrialRng(a.Seed, k));
                var y = RadarContract.NextRound(level, TimeTrial.TrialRng(b.Seed, k));
                bool equal = x.Count == y.Count && x.ExposureMs == y.ExposureMs;
                for (int i = 0; equal && i < x.Count; i++) equal = x.Capsules[i].Type == y.Capsules[i].Type && x.Capsules[i].X == y.Capsules[i].X && x.Capsules[i].Y == y.Capsules[i].Y;
                if (equal) same++;
            }
            Assert.Less(same, 20, "alguna de las 20 primeras rondas tiene que ser distinta");
            Assert.LessOrEqual(same, 2, "y casi todas");
        }

        [Test]
        public void TheLadderTheMarkAndTheTrackVersionDoNotDependOnTheRunsSeed()
        {
            // la forma de la pista es la misma: la versión no sube (los récords y las 3 partidas del lugar no empiezan de nuevo), la escalera y la marca tampoco cambian
            Assert.AreEqual(2, TimeTrial.TrackVersion("stroop")); Assert.AreEqual(2, TimeTrial.TrackVersion("disparate"));
            Assert.AreEqual(3, TimeTrial.TrackVersion("radar")); Assert.AreEqual(2, TimeTrial.TrackVersion("acoplamiento"));
            foreach (var g in Games)
            {
                var a = new TimeTrial.Run(g, Day, 1UL);
                var b = new TimeTrial.Run(g, Day, 987654321UL);
                var c = new TimeTrial.Run(g, Day);
                bool[] ok = { true, true, false, true, true, false, true, true, true, true, true, true, false, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true };
                for (int i = 0; i < ok.Length; i++)
                {
                    bool sa = a.Register(ok[i], 119f - 4 * i), sb = b.Register(ok[i], 119f - 4 * i), sc = c.Register(ok[i], 119f - 4 * i);
                    Assert.AreEqual(sa, sb, g + " ensayo " + i); Assert.AreEqual(sa, sc, g + " ensayo " + i);
                }
                Assert.AreEqual((c.Hits, c.Errors, c.Score, c.Level, c.PeakLevel, c.StepHits, c.Trials), (a.Hits, a.Errors, a.Score, a.Level, a.PeakLevel, a.StepHits, a.Trials), g);
                Assert.AreEqual((c.Hits, c.Errors, c.Score, c.Level, c.PeakLevel, c.StepHits, c.Trials), (b.Hits, b.Errors, b.Score, b.Level, b.PeakLevel, b.StepHits, b.Trials), g);
                var ma = new StroopSessionMetrics(); var mc = new StroopSessionMetrics();
                a.Fill(ma); c.Fill(mc);
                CollectionAssert.AreEqual(mc.tt_t_ms, ma.tt_t_ms, g); CollectionAssert.AreEqual(mc.tt_ok, ma.tt_ok, g); CollectionAssert.AreEqual(mc.tt_level, ma.tt_level, g);
                Assert.AreEqual((mc.tt_hits, mc.tt_errors, mc.tt_peak_level, mc.tt_seconds, mc.tt_track_version), (ma.tt_hits, ma.tt_errors, ma.tt_peak_level, ma.tt_seconds, ma.tt_track_version), g);
            }
        }

        [Test]
        public void InTheContrarreloj_DisparateAvoidsTheSentencesOfTheLastGames_LikeTheNormalMode()
        {
            // lo que hace el controlador (Tarea 210): el Director del Contrarreloj lleva las frases recientes, sin Ráfagas, con el azar de cada ensayo de la pista de ESA partida
            var bank = SentenceBank.Load();
            var run = TimeTrial.Begin(TrialConfig("disparate"));
            var first = new DisparateDirector(bank, new System.Random(1)) { NoBursts = true };
            for (int k = 0; k < 40; k++) first.Next(TimeTrial.LevelFor("disparate", k), false, false, TimeTrial.TrialRng(run.Seed, k));
            var recent = DisparateContract.RecentIds(DisparateContract.PushRecent("", first.UsedInOrder));
            Assert.AreEqual(40, recent.Count, "las 40 frases de la partida quedan anotadas como vistas");
            var next = TimeTrial.Begin(TrialConfig("disparate"));
            var second = new DisparateDirector(bank, new System.Random(2), recent) { NoBursts = true };
            for (int k = 0; k < 40; k++) second.Next(TimeTrial.LevelFor("disparate", k), false, false, TimeTrial.TrialRng(next.Seed, k));
            int repeated = 0;
            foreach (var id in second.UsedInOrder) if (recent.Contains(id)) repeated++;
            Assert.AreEqual(0, repeated, "hay frases de sobra en el banco: la partida siguiente no repite las de la anterior");
        }

        [Test]
        public void TrackRandom_IsDeterministic_AndDoesNotDependOnSystemRandom()
        {
            var a = new TimeTrial.TrackRandom(12345UL);
            Assert.AreEqual(285786412, a.Next());
            Assert.AreEqual(439840370, a.Next());
            Assert.AreEqual(256715742, a.Next());
            var b = new TimeTrial.TrackRandom(12345UL);
            Assert.AreEqual(0.1330796686614273, b.NextDouble(), 1e-15);
            Assert.AreEqual(0.20481663336165912, b.NextDouble(), 1e-15);
            var c = new TimeTrial.TrackRandom(12345UL);
            Assert.AreEqual(new[] { 1, 2, 1, 1 }, new[] { c.Next(10), c.Next(10), c.Next(10), c.Next(10) });
            // en rango, siempre
            var r = new TimeTrial.TrackRandom(7UL);
            for (int i = 0; i < 5000; i++)
            {
                int n = r.Next(6);
                Assert.IsTrue(n >= 0 && n < 6);
                double d = r.NextDouble();
                Assert.IsTrue(d >= 0.0 && d < 1.0);
                int m = r.Next(3, 9);
                Assert.IsTrue(m >= 3 && m < 9);
            }
            Assert.AreEqual(0, r.Next(0));
            Assert.AreEqual(5, r.Next(5, 5));
        }

        [Test]
        public void EveryTrialHasItsOwnRandom_SoAnErrorDoesNotShiftWhatComes()
        {
            ulong seed = TimeTrial.Seed(Day, "radar");
            // el ensayo 7 usa el mismo azar pase lo que pase antes
            var a = TimeTrial.TrialRng(seed, 7);
            for (int i = 0; i < 50; i++) TimeTrial.TrialRng(seed, i).Next();          // otros ensayos que se piden en otro orden
            var b = TimeTrial.TrialRng(seed, 7);
            for (int i = 0; i < 20; i++) Assert.AreEqual(a.Next(1000), b.Next(1000));
            // ensayos y flujos distintos, azar distinto
            Assert.AreNotEqual(TimeTrial.TrialRng(seed, 7).Next(), TimeTrial.TrialRng(seed, 8).Next());
            Assert.AreNotEqual(TimeTrial.TrialRng(seed, 7, 0).Next(), TimeTrial.TrialRng(seed, 7, 1).Next());
            Assert.AreNotEqual(TimeTrial.TrialRng(seed, 7).Next(), TimeTrial.TrialRng(TimeTrial.Seed(Day + 1, "radar"), 7).Next());
        }

        // ------------------------------------------------------------------ la misma semilla, la misma pista

        [Test]
        public void SameSeed_SameTrack_ForTheFourGenerators()
        {
            // Rescate relámpago: las rondas (qué cápsulas, dónde y cuánto dura el destello) y la espera del relámpago
            ulong rs = TimeTrial.Seed(Day, "radar");
            for (int k = 0; k < 40; k++)
            {
                int level = TimeTrial.LevelFor("radar", k);
                var x = RadarContract.NextRound(level, TimeTrial.TrialRng(rs, k));
                var y = RadarContract.NextRound(level, TimeTrial.TrialRng(rs, k));
                Assert.AreEqual(x.ExposureMs, y.ExposureMs);
                Assert.AreEqual(x.Count, y.Count);
                for (int i = 0; i < x.Count; i++)
                {
                    Assert.AreEqual(x.Capsules[i].Type, y.Capsules[i].Type);
                    Assert.AreEqual(x.Capsules[i].X, y.Capsules[i].X);
                    Assert.AreEqual(x.Capsules[i].Y, y.Capsules[i].Y);
                }
                Assert.AreEqual(TimeTrial.TrialRng(rs, k, 1).NextDouble(), TimeTrial.TrialRng(rs, k, 1).NextDouble());
            }
            // Acoplamiento: la pieza, si es espejo y el giro
            ulong ds = TimeTrial.Seed(Day, "acoplamiento");
            for (int k = 0; k < 40; k++)
            {
                int level = TimeTrial.LevelFor("acoplamiento", k);
                var x = DockingContract.NextTrial(level, TimeTrial.TrialRng(ds, k));
                var y = DockingContract.NextTrial(level, TimeTrial.TrialRng(ds, k));
                Assert.AreEqual(x.Mirrored, y.Mirrored);
                Assert.AreEqual(x.AngleDeg, y.AngleDeg);
                CollectionAssert.AreEqual(x.Shape, y.Shape);
            }
            // Tinta o Palabra: la palabra, la tinta y la orilla (el secuenciador lleva su estado: dos secuenciadores nuevos, la misma pista)
            ulong ss = TimeTrial.Seed(Day, "stroop");
            var s1 = new StroopContract.Sequencer(new System.Random(1));
            var s2 = new StroopContract.Sequencer(new System.Random(2));                       // su azar propio no cuenta: manda el que se le da en cada palabra
            for (int k = 0; k < 60; k++)
            {
                int level = TimeTrial.LevelFor("stroop", k);
                var x = s1.Next(level, TimeTrial.TrialRng(ss, k));
                var y = s2.Next(level, TimeTrial.TrialRng(ss, k));
                Assert.AreEqual((x.WordIndex, x.InkIndex, x.Rule, x.Switched), (y.WordIndex, y.InkIndex, y.Rule, y.Switched), "palabra " + k);
            }
            // ¿Verdad o disparate?: las frases (con el banco real y sin evitar las de las últimas partidas ni Ráfagas)
            ulong vs = TimeTrial.Seed(Day, "disparate");
            var bank = SentenceBank.Load();
            var d1 = new DisparateDirector(bank, new System.Random(1)) { NoBursts = true };
            var d2 = new DisparateDirector(bank, new System.Random(99)) { NoBursts = true };
            for (int k = 0; k < 80; k++)
            {
                int level = TimeTrial.LevelFor("disparate", k);
                var x = d1.Next(level, false, false, TimeTrial.TrialRng(vs, k));
                var y = d2.Next(level, false, false, TimeTrial.TrialRng(vs, k));
                Assert.AreEqual(x.S.i, y.S.i, "frase " + k);
                Assert.IsFalse(x.Burst, "el Contrarreloj no tiene Ráfagas");
            }
        }

        [Test]
        public void ATrialDependsOnlyOnTheSeedItsNumberAndTheStep_NotOnWhatTheRestDid()
        {
            // quien falló el ensayo 5 y quien no ven el MISMO ensayo 6 si están en el mismo paso: Rescate y Acoplamiento no guardan estado entre ensayos
            ulong rs = TimeTrial.Seed(Day, "radar"), ds = TimeTrial.Seed(Day, "acoplamiento");
            for (int level = 1; level <= 12; level++)
            {
                var a = RadarContract.NextRound(level, TimeTrial.TrialRng(rs, 6));
                for (int junk = 0; junk < 3; junk++) RadarContract.NextRound(level, TimeTrial.TrialRng(rs, junk));
                var b = RadarContract.NextRound(level, TimeTrial.TrialRng(rs, 6));
                CollectionAssert.AreEqual(new List<CapsuleType>(a.Types()), new List<CapsuleType>(b.Types()));
                var x = DockingContract.NextTrial(level, TimeTrial.TrialRng(ds, 6));
                var y = DockingContract.NextTrial(level, TimeTrial.TrialRng(ds, 6));
                Assert.AreEqual((x.Mirrored, x.AngleDeg), (y.Mirrored, y.AngleDeg));
            }
        }

        [Test]
        public void Disparate_TheTrackHasNoBurstsNoSignalAndEveryPhraseIsNew()
        {
            ulong vs = TimeTrial.Seed(Day, "disparate");
            var dir = new DisparateDirector(SentenceBank.Load(), new System.Random(5)) { NoBursts = true };
            var seen = new HashSet<string>();
            for (int k = 0; k < 120; k++)
            {
                var spec = dir.Next(TimeTrial.LevelFor("disparate", k), false, false, TimeTrial.TrialRng(vs, k));
                Assert.IsNotNull(spec);
                Assert.IsFalse(spec.Burst, "frase " + k);
                Assert.IsTrue(seen.Add(spec.S.i), "repetida: " + spec.S.f);
            }
            // las ráfagas siguen existiendo para el juego de siempre (cada 12 frases)
            var normal = new DisparateDirector(SentenceBank.Load(), new System.Random(5));
            bool anyBurst = false;
            for (int k = 0; k < 40; k++) anyBurst |= normal.Next(1, false, false).Burst;
            Assert.IsTrue(anyBurst);
        }

        // ------------------------------------------------------------------ la marca neta (Tarea 178) y el mínimo antes de responder

        [Test]
        public void TheMarkIsHitsMinusErrors_FlooredAtZero()
        {
            // los mismos casos que `TimeTrialDayTest` en la app (TimeTrialDay.markOf): la fórmula está en los dos lados y una prueba en cada uno la vigila
            Assert.AreEqual(34, TimeTrial.Mark(38, 4));
            Assert.AreEqual(37, TimeTrial.Mark(43, 6));
            Assert.AreEqual(0, TimeTrial.Mark(3, 5), "nunca negativa");
            Assert.AreEqual(0, TimeTrial.Mark(0, 0));
            Assert.AreEqual(1, TimeTrial.Mark(1, 0));
            var run = new TimeTrial.Run("stroop", Day);
            Assert.AreEqual(0, run.Score);
            run.Register(true, 60f); run.Register(true, 60f); run.Register(true, 60f);
            Assert.AreEqual(3, run.Score);
            run.Register(false, 60f);
            Assert.AreEqual((3, 1, 2), (run.Hits, run.Errors, run.Score), "el error resta uno a la marca");
            run.Register(false, 60f); run.Register(false, 60f); run.Register(false, 60f);
            Assert.AreEqual(0, run.Score, "piso en 0: nunca negativa");
        }

        [Test]
        public void AnErrorDoesNotMakeThePersonWait_AndDoesNotChangeTheStep()
        {
            Assert.LessOrEqual(TimeTrial.ErrorFeedbackSeconds, 1f, "el error solo da un aviso breve (Tarea 178): ya no hay pausa de 3, 6 o 9 s");
            var run = new TimeTrial.Run("disparate", Day);
            int before = run.Level;
            for (int i = 0; i < 6; i++) run.Register(false, 60f);
            Assert.AreEqual(before, run.Level, "los errores no bajan ni suben el paso: la pista sube solo por aciertos");
            Assert.AreEqual(0, run.Hits);
        }

        [Test]
        public void TheTracksAreVersion2_AndRescateIsVersion3_BecauseItsMarkNowCountsPerCapsule()
        {
            foreach (var g in Games)
                Assert.AreEqual(g == "radar" ? 3 : 2, TimeTrial.TrackVersion(g), g + (g == "radar" ? ": la marca cuenta por cápsula (Tarea 208): los récords de Rescate empiezan de nuevo" : ": la regla de la marca cambió (Tarea 178): los récords empiezan de nuevo"));
        }

        [Test]
        public void RescatesSeedChangesWithItsTrackVersion()
        {
            // la versión entra en la semilla: el mismo día, Rescate juega OTRA pista desde la versión 3 (valores fijos calculados con la misma fórmula: FNV-1a del id, fecha × la constante de oro, versión << 32, SplitMix64)
            Assert.AreEqual(15825842088206235552UL, TimeTrial.Seed(20261026, "radar"), "versión 3");
            Assert.AreNotEqual(15746051940230642383UL, TimeTrial.Seed(20261026, "radar"), "no es la semilla de la versión 2");
            Assert.AreEqual(3509569136056027780UL, TimeTrial.Seed(20261026, "stroop"), "las otras pistas siguen en la versión 2: su semilla no cambió");
        }

        [Test]
        public void TheTwoOptionGamesWaitBeforeAcceptingAnAnswer()
        {
            Assert.AreEqual(1.5f, TimeTrial.MinThinkSeconds("disparate", 3), "una frase corta: 1,5 s");
            Assert.AreEqual(1.5f, TimeTrial.MinThinkSeconds("disparate", 6), 1e-4f);
            Assert.AreEqual(2.5f, TimeTrial.MinThinkSeconds("disparate", 10), 1e-4f, "una frase larga: 0,25 s por palabra");
            Assert.AreEqual(1.2f, TimeTrial.MinThinkSeconds("acoplamiento"), "el puerto se abre a los 1,2 s");
            Assert.AreEqual(0f, TimeTrial.MinThinkSeconds("stroop"), "Tinta (4 opciones) y Rescate no lo necesitan");
            Assert.AreEqual(0f, TimeTrial.MinThinkSeconds("radar"));
        }

        // ------------------------------------------------------------------ la cuenta de la partida

        [Test]
        public void TheRunCountsHitsErrorsAndTheStepReached()
        {
            var run = new TimeTrial.Run("acoplamiento", Day);               // un paso cada 2 aciertos
            Assert.AreEqual(1, run.Level);
            Assert.IsFalse(run.Register(true, 60f));
            Assert.IsTrue(run.Register(true, 60f), "el 2.º acierto sube al paso 2");
            Assert.AreEqual(2, run.Level);
            Assert.IsFalse(run.Register(false, 60f), "un error no sube");
            Assert.AreEqual(2, run.Level, "y no baja de paso");
            Assert.IsFalse(run.Register(true, 60f));
            Assert.IsTrue(run.Register(true, 60f));
            Assert.AreEqual((4, 1, 5, 3), (run.Hits, run.Errors, run.Trials, run.Level));
            Assert.AreEqual(3, run.PeakLevel);
            for (int i = 0; i < 60; i++) run.Register(true, 60f);
            Assert.AreEqual(12, run.Level);
            Assert.AreEqual(12, run.PeakLevel);
        }

        // ------------------------------------------------------------------ Rescate relámpago por cápsula (Tarea 208): RegisterRound y StepHits

        [Test]
        public void ARoundCountsEachCapsule_ButTheLadderCountsRounds()
        {
            var run = new TimeTrial.Run("radar", Day);
            Assert.IsFalse(run.RegisterRound(2, 1, false, 60f), "una ronda con 2 bien y 1 de más no sube");
            Assert.AreEqual((2, 1, 1, 0, 1), (run.Hits, run.Errors, run.Score, run.StepHits, run.Trials), "2 aciertos, 1 error, marca 1, ninguna ronda lograda y UN número de ensayo");
            var m = new StroopSessionMetrics();
            run.Fill(m);
            CollectionAssert.AreEqual(new[] { 60000, 60000, 60000 }, m.tt_t_ms, "una cápsula, un elemento de ritmo, todos con el mismo instante");
            CollectionAssert.AreEqual(new[] { 1, 1, 0 }, m.tt_ok, "primero los aciertos y luego los errores");
            CollectionAssert.AreEqual(new[] { 1, 1, 1 }, m.tt_level, "todos en el paso en que se jugó la ronda");
            Assert.AreEqual((2, 1), (m.tt_hits, m.tt_errors), "la telemetría lleva las cápsulas, sin restar");
            Assert.AreEqual(m.tt_hits + m.tt_errors, run.LoggedTrials, "el ritmo queda completo: tantos elementos como aciertos más errores");
        }

        [Test]
        public void ARoundHasNoFloor_OnlyTheWholeGameDoes()
        {
            var run = new TimeTrial.Run("radar", Day);
            run.RegisterRound(0, 3, false, 50f);
            Assert.AreEqual(0, run.Score, "la partida nunca baja de 0");
            run.RegisterRound(3, 0, true, 40f);
            Assert.AreEqual((3, 3, 0), (run.Hits, run.Errors, run.Score), "la ronda mala NO se perdona por piso: las 3 de más se descuentan de las 3 de la siguiente");
            run.RegisterRound(2, 0, true, 30f);
            Assert.AreEqual(2, run.Score, "y desde ahí vuelve a subir");
        }

        [Test]
        public void ARoundWithNoMarksAddsATrialNumberAndNothingElse()
        {
            var run = new TimeTrial.Run("radar", Day);
            Assert.IsFalse(run.RegisterRound(0, 0, false, 50f));
            Assert.AreEqual((0, 0, 0, 0, 1, 0), (run.Hits, run.Errors, run.Score, run.StepHits, run.Trials, run.LoggedTrials));
        }

        [Test]
        public void TheStepClimbsWithSuccessfulRounds_NotWithCapsules()
        {
            var up = new TimeTrial.Run("radar", Day);                       // un paso cada 3 rondas logradas
            Assert.IsFalse(up.RegisterRound(2, 0, true, 100f));
            Assert.IsFalse(up.RegisterRound(2, 0, true, 90f));
            Assert.IsTrue(up.RegisterRound(2, 0, true, 80f), "la 3.ª ronda lograda sube al paso 2");
            Assert.AreEqual((2, 3, 6), (up.Level, up.StepHits, up.Hits), "con 6 cápsulas bien marcadas");
            var loose = new TimeTrial.Run("radar", Day);
            for (int i = 0; i < 3; i++) Assert.IsFalse(loose.RegisterRound(3, 0, false, 100f - 10 * i), "nueve cápsulas sueltas, sin ronda lograda, no suben");
            Assert.AreEqual((1, 0, 9), (loose.Level, loose.StepHits, loose.Hits));
            Assert.AreEqual(1, loose.PeakLevel);
            Assert.AreEqual(2, up.PeakLevel, "el paso más alto de la partida");
        }

        [Test]
        public void TheNextRoundsRandomUsesTheRoundNumber_NotTheCapsules()
        {
            var run = new TimeTrial.Run("radar", Day);
            run.RegisterRound(3, 1, false, 50f);
            Assert.AreEqual(TimeTrial.TrialRng(run.Seed, 1).Next(1000000), run.Rng().Next(1000000), "después de la ronda 0 viene el azar del ensayo 1, no el del 4");
            Assert.AreEqual(TimeTrial.TrialRng(run.Seed, 1, 1).NextDouble(), run.Rng(1).NextDouble());
        }

        [Test]
        public void ThePastTheCapRoundIsStillCounted_ButNoLongerLogged()
        {
            var run = new TimeTrial.Run("radar", Day);
            for (int i = 0; i < TimeTrial.Run.MaxLoggedTrials; i++) run.RegisterRound(1, 0, false, 60f);
            run.RegisterRound(3, 1, true, 60f);
            Assert.AreEqual(TimeTrial.Run.MaxLoggedTrials, run.LoggedTrials, "el registro llega al tope y no pasa");
            Assert.AreEqual((TimeTrial.Run.MaxLoggedTrials + 3, 1, TimeTrial.Run.MaxLoggedTrials + 1), (run.Hits, run.Errors, run.Trials), "pero todo se sigue contando");
        }

        [Test]
        public void TheOtherThreeGamesKeepCountingOneTrialAtATime_SameListsSameStepAsBefore()
        {
            foreach (var g in new[] { "stroop", "disparate", "acoplamiento" })
            {
                var a = new TimeTrial.Run(g, Day);
                var b = new TimeTrial.Run(g, Day);
                bool[] ok = { true, true, false, true, true, false, false, true, true, true, true, true, false, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true };
                for (int i = 0; i < ok.Length; i++)
                {
                    bool stepA = a.Register(ok[i], 119f - 4 * i);
                    bool stepB = b.RegisterRound(ok[i] ? 1 : 0, ok[i] ? 0 : 1, ok[i], 119f - 4 * i);
                    Assert.AreEqual(stepA, stepB, g + " ensayo " + i);
                }
                Assert.AreEqual((a.Hits, a.Errors, a.Trials, a.Level, a.PeakLevel, a.StepHits), (b.Hits, b.Errors, b.Trials, b.Level, b.PeakLevel, b.StepHits), g);
                Assert.AreEqual(a.Hits, a.StepHits, g + ": en estos juegos el paso se cuenta con los aciertos, como siempre");
                var ma = new StroopSessionMetrics(); var mb = new StroopSessionMetrics();
                a.Fill(ma); b.Fill(mb);
                CollectionAssert.AreEqual(ma.tt_t_ms, mb.tt_t_ms, g); CollectionAssert.AreEqual(ma.tt_ok, mb.tt_ok, g); CollectionAssert.AreEqual(ma.tt_level, mb.tt_level, g);
                Assert.AreEqual(ok.Length, ma.tt_ok.Length, g + ": un elemento de ritmo por ensayo");
            }
        }

        [Test]
        public void TheRunsRandomIsTheNextTrialsRandom()
        {
            var run = new TimeTrial.Run("radar", Day);
            int first = run.Rng().Next(1000000);
            Assert.AreEqual(first, TimeTrial.TrialRng(run.Seed, 0).Next(1000000));
            run.Register(false, 60f);
            Assert.AreEqual(TimeTrial.TrialRng(run.Seed, 1).Next(1000000), run.Rng().Next(1000000));
            Assert.AreEqual(TimeTrial.TrialRng(run.Seed, 1, 1).NextDouble(), run.Rng(1).NextDouble());
        }

        // ------------------------------------------------------------------ la config, la telemetría y los textos

        [Test]
        public void Begin_OnlyForTheGamesThatHaveIt_AndForcesTheClock()
        {
            SequenceInitConfig Cfg(string id, bool tt, bool timed) => new SequenceInitConfig { game_id = id, config = new SequenceConfigDetails { time_trial = tt, track_seed = Day, timed = timed } };
            var c = Cfg("disparate", true, false);
            var run = TimeTrial.Begin(c);
            Assert.IsNotNull(run);
            Assert.IsTrue(c.config.timed, "el Contrarreloj siempre es contra el reloj");
            Assert.AreEqual(TimeTrial.SeedOfGame(TimeTrial.Seed(Day, "disparate"), run.GameNumber), run.Seed, "la semilla de la partida: la del día con el número de ESA partida (Tarea 210)");
            Assert.AreEqual(Day, run.DayKey);
            Assert.IsNull(TimeTrial.Begin(Cfg("disparate", false, true)), "sin time_trial, juega como siempre");
            Assert.IsNull(TimeTrial.Begin(Cfg("calculo", true, false)), "un juego sin pista");
            Assert.IsNull(TimeTrial.Begin(null));
            Assert.IsNull(TimeTrial.Begin(new SequenceInitConfig { game_id = "stroop" }));
        }

        [Test]
        public void TheConfigTravelsAsJson_AndAnOldAppThatDoesNotSendItIsNotATimeTrial()
        {
            var json = JsonUtility.ToJson(new SequenceInitConfig { game_id = "stroop", config = new SequenceConfigDetails { time_trial = true, track_seed = 20261026 } });
            var back = JsonUtility.FromJson<SequenceInitConfig>(json);
            Assert.IsTrue(back.config.time_trial);
            Assert.AreEqual(20261026, back.config.track_seed);
            var old = JsonUtility.FromJson<SequenceInitConfig>("{\"game_id\":\"stroop\",\"config\":{\"timed\":true}}");
            Assert.IsFalse(old.config.time_trial);
            Assert.IsNull(TimeTrial.Begin(old));
        }

        [Test]
        public void TheTelemetryCarriesTheMarkTheStepAndTheTrackVersion_AndLeavesTheAdaptiveEngineAlone()
        {
            var run = new TimeTrial.Run("stroop", Day);
            for (int i = 0; i < 20; i++) run.Register(true, 60f);
            run.Register(false, 60f); run.Register(false, 60f);
            var m = new StroopSessionMetrics { mode_trials = 14, mode_hits = 9, end_rating = 0.37f };
            run.Fill(m);
            Assert.AreEqual((20, 2, 3, TimeTrial.Seconds, TimeTrial.TrackVersion("stroop")), (m.tt_hits, m.tt_errors, m.tt_peak_level, m.tt_seconds, m.tt_track_version));
            Assert.AreEqual((0, 0), (m.mode_trials, m.mode_hits), "la dificultad adaptativa no corrió: no hay ensayos que cuenten para Desafío");
            Assert.AreEqual(0.37f, m.end_rating, "el nivel de la persona no se toca");
            Assert.AreEqual(-1, new StroopSessionMetrics().tt_hits, "una partida de siempre dice -1");
            Assert.AreEqual(-1, new StroopSessionMetrics().tt_track_version);
        }

        // ------------------------------------------------------------------ el ritmo por ensayo (Tarea 200): tt_t_ms, tt_ok y tt_level

        [Test]
        public void TheRhythmLogsOneEntryPerTrial_InThreeParallelLists()
        {
            var run = new TimeTrial.Run("stroop", Day);                      // un paso cada 8 aciertos
            float[] left = { 118.4f, 112f, 105.5f, 100f, 96f, 90f, 84f, 80f, 74f, 61f, 59.9f, 30f, 29f, 2f };
            bool[] ok = { true, true, true, false, true, true, true, true, true, true, false, true, true, true };
            for (int i = 0; i < left.Length; i++) run.Register(ok[i], left[i]);
            var m = new StroopSessionMetrics();
            run.Fill(m);
            Assert.AreEqual(left.Length, m.tt_t_ms.Length);
            Assert.AreEqual(m.tt_t_ms.Length, m.tt_ok.Length, "tres listas paralelas: una posición, un ensayo");
            Assert.AreEqual(m.tt_t_ms.Length, m.tt_level.Length);
            Assert.AreEqual(m.tt_hits + m.tt_errors, m.tt_t_ms.Length, "un elemento por ensayo resuelto: aciertos + errores");
            CollectionAssert.AreEqual(new[] { 1600, 8000, 14500, 20000, 24000, 30000, 36000, 40000, 46000, 59000, 60100, 90000, 91000, 118000 }, m.tt_t_ms, "los ms desde el «¡Ya!»: 120 s menos lo que quedaba");
            CollectionAssert.AreEqual(new[] { 1, 1, 1, 0, 1, 1, 1, 1, 1, 1, 0, 1, 1, 1 }, m.tt_ok, "1 acierto, 0 error");
            Assert.AreEqual(run.Hits, System.Linq.Enumerable.Sum(m.tt_ok), "los aciertos de la lista son los de la partida");
        }

        [Test]
        public void ElapsedMs_IsTheGameClockSinceTheGoCue_ClampedToTheGame()
        {
            Assert.AreEqual(0, TimeTrial.Run.ElapsedMs(120f), "recién el «¡Ya!»");
            Assert.AreEqual(500, TimeTrial.Run.ElapsedMs(119.5f));
            Assert.AreEqual(30000, TimeTrial.Run.ElapsedMs(90f));
            Assert.AreEqual(47600, TimeTrial.Run.ElapsedMs(72.4f), "redondeado al milisegundo");
            Assert.AreEqual(120000, TimeTrial.Run.ElapsedMs(0f), "el último instante");
            Assert.AreEqual(120000, TimeTrial.Run.ElapsedMs(-3f), "al filo del final no pasa de la duración de la partida");
            Assert.AreEqual(0, TimeTrial.Run.ElapsedMs(130f), "antes del «¡Ya!» nunca es negativo");
            Assert.AreEqual(0, TimeTrial.Run.ElapsedMs(float.NaN), "lo que no es un número da 0");
            Assert.AreEqual(120000, TimeTrial.Run.ElapsedMs(float.NegativeInfinity));
        }

        [Test]
        public void TheLoggedTimesNeverGoBack()
        {
            var run = new TimeTrial.Run("radar", Day);
            run.Register(true, 100f);
            run.Register(true, 105f);                                        // el reloj de juego no retrocede: si un cuadro lo dijera, el ensayo queda donde estaba el anterior
            run.Register(false, 80f);
            var m = new StroopSessionMetrics();
            run.Fill(m);
            CollectionAssert.AreEqual(new[] { 20000, 20000, 40000 }, m.tt_t_ms);
        }

        [Test]
        public void EachTrialIsLoggedAtTheStepWhereItWasPlayed_InTheFourGames()
        {
            foreach (var g in Games)
            {
                var run = new TimeTrial.Run(g, Day);
                int n = 2 * TimeTrial.StepEvery(g) + 1;
                for (int i = 0; i < n; i++) run.Register(true, 100f - i * 0.5f);
                var m = new StroopSessionMetrics();
                run.Fill(m);
                for (int i = 0; i < n; i++)
                    Assert.AreEqual(TimeTrial.LevelFor(g, i), m.tt_level[i], g + ": el acierto " + (i + 1) + " se jugó en el paso en que estaba la pista ANTES de registrarlo (el que sube de paso se anota en el paso donde se jugó)");
                Assert.AreEqual(1, m.tt_level[0], g);
                Assert.AreEqual(2, m.tt_level[TimeTrial.StepEvery(g)], g + ": el primero del paso 2");
            }
        }

        [Test]
        public void AnErrorIsLoggedAtTheCurrentStep()
        {
            var run = new TimeTrial.Run("acoplamiento", Day);                // un paso cada 2 aciertos
            run.Register(true, 110f); run.Register(true, 100f);              // ya está en el paso 2
            run.Register(false, 90f);
            run.Register(true, 80f); run.Register(true, 70f);                // el 4.º acierto lleva al paso 3
            run.Register(false, 60f);
            var m = new StroopSessionMetrics();
            run.Fill(m);
            CollectionAssert.AreEqual(new[] { 1, 1, 2, 2, 2, 3 }, m.tt_level);
            CollectionAssert.AreEqual(new[] { 1, 1, 0, 1, 1, 0 }, m.tt_ok);
        }

        [Test]
        public void PastTheCap_EverythingIsStillCounted_ButTheTrialIsNoLongerLogged()
        {
            var run = new TimeTrial.Run("disparate", Day);
            Assert.AreEqual(200, TimeTrial.Run.MaxLoggedTrials);
            for (int i = 0; i < 250; i++) run.Register(i % 4 != 3, 120f - i * 0.4f);
            Assert.AreEqual(250, run.Trials);
            Assert.AreEqual(250, run.Hits + run.Errors, "la marca y el paso cuentan todo");
            Assert.AreEqual(200, run.LoggedTrials);
            var m = new StroopSessionMetrics();
            run.Fill(m);
            Assert.AreEqual((200, 200, 200), (m.tt_t_ms.Length, m.tt_ok.Length, m.tt_level.Length), "tope de 200 en las tres");
            Assert.AreEqual((run.Hits, run.Errors), (m.tt_hits, m.tt_errors), "tt_hits y tt_errors siguen siendo los de toda la partida");
            Assert.Less(m.tt_t_ms.Length, m.tt_hits + m.tt_errors, "una lista más corta que aciertos + errores avisa a la app: el registro quedó incompleto");
            Assert.AreEqual(0, m.tt_ok[3], "se anotan los PRIMEROS 200, en orden (el ensayo 4 fue un error)");
            Assert.AreEqual(0, m.tt_ok[199], "el ensayo 200 (posición 199) fue un error: 199 % 4 = 3");
            Assert.AreEqual(TimeTrial.Run.ElapsedMs(120f - 199 * 0.4f), m.tt_t_ms[199], "y su hora es la del ensayo 200, no la del último");
        }

        [Test]
        public void FillHandsOverACopy_ALaterTrialDoesNotChangeWhatWasSent()
        {
            var run = new TimeTrial.Run("stroop", Day);
            run.Register(true, 100f);
            var m = new StroopSessionMetrics();
            run.Fill(m);
            run.Register(false, 90f);
            Assert.AreEqual(1, m.tt_t_ms.Length);
            Assert.AreEqual(1, m.tt_ok.Length);
            Assert.AreEqual(1, m.tt_level.Length);
        }

        [Test]
        public void ARunWithNoTrialsFillsEmptyLists_NotNull()
        {
            var m = new StroopSessionMetrics();
            new TimeTrial.Run("radar", Day).Fill(m);
            Assert.AreEqual(0, m.tt_hits);
            Assert.IsNotNull(m.tt_t_ms);
            Assert.AreEqual((0, 0, 0), (m.tt_t_ms.Length, m.tt_ok.Length, m.tt_level.Length));
        }

        [Test]
        public void TheRhythmTravelsAsJson_AndAGameOutsideTheTimeTrialSendsNothing()
        {
            var run = new TimeTrial.Run("stroop", Day);
            run.Register(true, 112.5f); run.Register(false, 97.25f); run.Register(true, 40f);
            var m = new StroopSessionMetrics();
            run.Fill(m);
            string json = JsonUtility.ToJson(m);
            StringAssert.Contains("\"tt_t_ms\":[7500,22750,80000]", json);
            StringAssert.Contains("\"tt_ok\":[1,0,1]", json);
            StringAssert.Contains("\"tt_level\":[1,1,1]", json);
            var back = JsonUtility.FromJson<StroopSessionMetrics>(json);
            CollectionAssert.AreEqual(m.tt_t_ms, back.tt_t_ms);
            CollectionAssert.AreEqual(m.tt_ok, back.tt_ok);
            CollectionAssert.AreEqual(m.tt_level, back.tt_level);

            // una partida de siempre no llena nada: las listas llegan vacías (la app las lee como «sin ritmo»)
            var plain = JsonUtility.FromJson<StroopSessionMetrics>(JsonUtility.ToJson(new StroopSessionMetrics()));
            Assert.AreEqual(-1, plain.tt_hits);
            Assert.IsTrue(plain.tt_t_ms == null || plain.tt_t_ms.Length == 0);
            Assert.IsTrue(plain.tt_ok == null || plain.tt_ok.Length == 0);
            Assert.IsTrue(plain.tt_level == null || plain.tt_level.Length == 0);
        }

        [Test]
        public void TheRhythmListsAreOnlyFilledByTheTimeTrial()
        {
            // los juegos solo llaman a `Fill` desde su `_tt`: una partida de siempre no pasa por ahí y las listas quedan sin llenar
            var plain = new StroopSessionMetrics();
            Assert.IsNull(plain.tt_t_ms);
            Assert.IsNull(plain.tt_ok);
            Assert.IsNull(plain.tt_level);
        }

        [Test]
        public void TheTextsSayAciertosAndTheClockIsMinutesAndSeconds()
        {
            Assert.AreEqual("0 aciertos", TimeTrial.HitsText(0));
            Assert.AreEqual("1 acierto", TimeTrial.HitsText(1));
            Assert.AreEqual("23 aciertos", TimeTrial.HitsText(23));
            Assert.AreEqual("2:00", TimeTrial.ClockText(120f));
            Assert.AreEqual("1:12", TimeTrial.ClockText(72f));
            Assert.AreEqual("1:12", TimeTrial.ClockText(71.2f), "los segundos se redondean hacia arriba");
            Assert.AreEqual("0:05", TimeTrial.ClockText(4.1f));
            Assert.AreEqual("0:01", TimeTrial.ClockText(0.2f));
            Assert.AreEqual("0:00", TimeTrial.ClockText(0f));
            Assert.AreEqual("0:00", TimeTrial.ClockText(-3f));
            Assert.AreEqual("Contrarreloj de 2 minutos", TimeTrial.CountdownSub);
            Assert.AreEqual(120, TimeTrial.Seconds);
            foreach (var t in new[] { TimeTrial.HitsText(5), TimeTrial.CountdownSub, TimeTrial.Name })
                foreach (var banned in new[] { "Nivel", "nivel", "Reto", "racha", "sesión" }) Assert.IsFalse(t.Contains(banned), t);
        }

        [Test]
        public void TheDockingRevealDurationsAreTheOnesTheControllerPlays()
        {
            // lo que dura la revelación de Acoplamiento: gira, se da vuelta si era espejo, baja y se aleja (o, si acertó, espera, vuela a su casillero y queda a ras). Desde la Tarea 178 un error no suma pausa: queda solo esto
            Assert.AreEqual(1.5f, DockingContract.MissRevealSeconds(false), 1e-4f);
            Assert.AreEqual(1.8f, DockingContract.MissRevealSeconds(true), 1e-4f);
            Assert.AreEqual(1.82f, DockingContract.OkRevealSeconds(false), 1e-4f);
            Assert.AreEqual(2.12f, DockingContract.OkRevealSeconds(true), 1e-4f);
        }

        [Test]
        public void TheEditorCanShortenTheMatchForTheSmoke_AndTheDeviceNever()
        {
            Assert.AreEqual(120, TimeTrial.TotalSeconds);
#if UNITY_EDITOR
            TimeTrial.SecondsOverride = 20;
            try { Assert.AreEqual(20, TimeTrial.TotalSeconds); }
            finally { TimeTrial.SecondsOverride = 0; }
            Assert.AreEqual(120, TimeTrial.TotalSeconds);
#endif
        }
    }
}
