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

        // ------------------------------------------------------------------ la misma pista para todos

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
            run.Register(true); run.Register(true); run.Register(true);
            Assert.AreEqual(3, run.Score);
            run.Register(false);
            Assert.AreEqual((3, 1, 2), (run.Hits, run.Errors, run.Score), "el error resta uno a la marca");
            run.Register(false); run.Register(false); run.Register(false);
            Assert.AreEqual(0, run.Score, "piso en 0: nunca negativa");
        }

        [Test]
        public void AnErrorDoesNotMakeThePersonWait_AndDoesNotChangeTheStep()
        {
            Assert.LessOrEqual(TimeTrial.ErrorFeedbackSeconds, 1f, "el error solo da un aviso breve (Tarea 178): ya no hay pausa de 3, 6 o 9 s");
            var run = new TimeTrial.Run("disparate", Day);
            int before = run.Level;
            for (int i = 0; i < 6; i++) run.Register(false);
            Assert.AreEqual(before, run.Level, "los errores no bajan ni suben el paso: la pista sube solo por aciertos");
            Assert.AreEqual(0, run.Hits);
        }

        [Test]
        public void TheFourTracksAreVersion2_BecauseTheMarkChanged()
        {
            foreach (var g in Games) Assert.AreEqual(2, TimeTrial.TrackVersion(g), g + ": la regla de la marca cambió (Tarea 178): los récords empiezan de nuevo");
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
            Assert.IsFalse(run.Register(true));
            Assert.IsTrue(run.Register(true), "el 2.º acierto sube al paso 2");
            Assert.AreEqual(2, run.Level);
            Assert.IsFalse(run.Register(false), "un error no sube");
            Assert.AreEqual(2, run.Level, "y no baja de paso");
            Assert.IsFalse(run.Register(true));
            Assert.IsTrue(run.Register(true));
            Assert.AreEqual((4, 1, 5, 3), (run.Hits, run.Errors, run.Trials, run.Level));
            Assert.AreEqual(3, run.PeakLevel);
            for (int i = 0; i < 60; i++) run.Register(true);
            Assert.AreEqual(12, run.Level);
            Assert.AreEqual(12, run.PeakLevel);
        }

        [Test]
        public void TheRunsRandomIsTheNextTrialsRandom()
        {
            var run = new TimeTrial.Run("radar", Day);
            int first = run.Rng().Next(1000000);
            Assert.AreEqual(first, TimeTrial.TrialRng(run.Seed, 0).Next(1000000));
            run.Register(false);
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
            Assert.AreEqual(TimeTrial.Seed(Day, "disparate"), run.Seed);
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
            for (int i = 0; i < 20; i++) run.Register(true);
            run.Register(false); run.Register(false);
            var m = new StroopSessionMetrics { mode_trials = 14, mode_hits = 9, end_rating = 0.37f };
            run.Fill(m);
            Assert.AreEqual((20, 2, 3, TimeTrial.Seconds, TimeTrial.TrackVersion("stroop")), (m.tt_hits, m.tt_errors, m.tt_peak_level, m.tt_seconds, m.tt_track_version));
            Assert.AreEqual((0, 0), (m.mode_trials, m.mode_hits), "la dificultad adaptativa no corrió: no hay ensayos que cuenten para Desafío");
            Assert.AreEqual(0.37f, m.end_rating, "el nivel de la persona no se toca");
            Assert.AreEqual(-1, new StroopSessionMetrics().tt_hits, "una partida de siempre dice -1");
            Assert.AreEqual(-1, new StroopSessionMetrics().tt_track_version);
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
