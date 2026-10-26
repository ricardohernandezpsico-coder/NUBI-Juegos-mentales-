using System;
using System.Collections.Generic;
using NeuroVida.Games.Acoplamiento;
using NeuroVida.Games.Disparate;
using NeuroVida.Games.Radar;
using NeuroVida.Games.Shared;
using NeuroVida.Games.Stroop;

namespace NeuroVida.Games.Tests
{
    /// <summary>
    /// La partida SIMULADA del Contrarreloj para <see cref="TimeTrialFairnessTests"/> (Tarea 176; Tarea 178: la marca es aciertos menos errores, con piso en 0, y el error no hace esperar): la MISMA pista del día jugada por un bot de «azar» (responde apenas puede, sin mirar) y uno de «cuidado» (acierta el 85 % y responde con el tiempo de una persona atenta).
    /// El tiempo de cada ensayo sale de las MISMAS constantes que usan los controladores (llegada de la tarjeta, revelación de Acoplamiento, espera y revelación de Rescate, aviso breve del error, mínimo antes de responder) y de la pista real (los generadores sembrados por el día; el banco de frases real); lo único supuesto es cuánto tarda en
    /// responder una persona atenta (0,85 s en Tinta, 2,4 s en Disparate y Acoplamiento, 2,2 s en Rescate; con dispersión). Sin UnityEngine: se puede correr fuera de Unity (<c>Resources</c> no existe ahí: el banco de frases se le pasa armado).
    /// </summary>
    public static class TimeTrialSimulation
    {
        public const int Runs = 200;
        public const int Day = 20261026;
        /// <summary>El bot de cuidado acierta el 85 %.</summary>
        public const double Careful = 0.85;

        // lo que tarda una persona atenta en responder (mediana, s) desde que puede hacerlo, y su dispersión (lognormal): SUPUESTOS razonables, no medidas
        private static readonly Dictionary<string, double> CareSeconds = new Dictionary<string, double> { { "stroop", 0.85 }, { "disparate", 2.4 }, { "radar", 2.2 }, { "acoplamiento", 2.4 } };
        private const double CareSigma = 0.25;

        // lo que dura lo demás de cada ensayo que no está en un contrato, de los controladores: ShowSentence (0,2 s) y CorrectFeedback / despedida (0,12 s) de ¿Verdad o disparate?
        private const double DisparateArrive = 0.2, DisparateFarewell = 0.12;

        public struct Result
        {
            public string Game;
            public double CareMean, ChanceMean, ChanceP90, CareP90, ChanceMax;
            public bool Passes => ChanceMean < 0.7 * CareMean && ChanceP90 < CareMean;
        }

        public static string Describe(Result r) =>
            string.Format(System.Globalization.CultureInfo.InvariantCulture, "{0}: cuidado {1:0.0} · azar {2:0.0} (p90 {3:0.0}, máx. {4:0.0}) = {5:0}% del cuidado", r.Game, r.CareMean, r.ChanceMean, r.ChanceP90, r.ChanceMax, 100 * r.ChanceMean / r.CareMean);

        /// <summary>Juega la pista del día <see cref="Runs"/> veces con cada bot y resume.</summary>
        public static Result Measure(string game, SentenceBank bank, int runs = Runs)
        {
            var care = new double[runs];
            var chance = new double[runs];
            for (int i = 0; i < runs; i++)
            {
                care[i] = Play(game, true, new Random(1000 + i), bank);
                chance[i] = Play(game, false, new Random(9000 + i), bank);
            }
            Array.Sort(care);
            Array.Sort(chance);
            double Mean(double[] a) { double s = 0; foreach (var v in a) s += v; return s / a.Length; }
            return new Result { Game = game, CareMean = Mean(care), CareP90 = Percentile(care, 0.9), ChanceMean = Mean(chance), ChanceP90 = Percentile(chance, 0.9), ChanceMax = chance[chance.Length - 1] };
        }

        private static double Percentile(double[] sorted, double p) => sorted[Math.Min(sorted.Length - 1, (int)Math.Ceiling(p * sorted.Length) - 1)];

        /// <summary>Una partida simulada de 120 s sobre la pista del día: la MARCA del bot (aciertos menos errores, con piso en 0).</summary>
        public static double Play(string game, bool careful, Random bot, SentenceBank bank)
        {
            switch (game)
            {
                case "stroop": return PlayStroop(careful, bot);
                case "disparate": return PlayDisparate(careful, bot, bank);
                case "radar": return PlayRadar(careful, bot);
                default: return PlayDocking(careful, bot);
            }
        }

        /// <summary>El tiempo de respuesta de una persona atenta: lognormal alrededor de su mediana.</summary>
        private static double CareTime(string game, Random bot)
        {
            double u1 = 1.0 - bot.NextDouble(), u2 = bot.NextDouble();
            double z = Math.Sqrt(-2.0 * Math.Log(u1)) * Math.Cos(2.0 * Math.PI * u2);
            return CareSeconds[game] * Math.Exp(CareSigma * z);
        }

        /// <summary>El bot de azar toca apenas puede: de 0,15 a 0,30 s después.</summary>
        private static double ChanceTime(Random bot) => 0.15 + 0.15 * bot.NextDouble();

        // ------------------------------------------------------------------ Tinta o Palabra

        private static double PlayStroop(bool careful, Random bot)
        {
            var run = new TimeTrial.Run("stroop", Day);
            var seq = new StroopContract.Sequencer(new Random(0));
            double t = 0;
            while (true)
            {
                int level = run.Level;
                var trial = seq.Next(level, run.Rng());
                double answerAt = t + StroopContract.ArrivalSeconds(level, false) + (careful ? CareTime("stroop", bot) : ChanceTime(bot));
                if (answerAt > TimeTrial.Seconds) break;
                bool correct = careful ? bot.NextDouble() < Careful : bot.Next(StroopContract.Names.Length) == trial.CorrectIndex;
                run.Register(correct);
                t = answerAt + (correct ? StroopContract.RetoSlideOutSeconds : TimeTrial.ErrorFeedbackSeconds + StroopContract.FadeOutSeconds);
                if (t >= TimeTrial.Seconds) break;
            }
            return run.Score;
        }

        // ------------------------------------------------------------------ ¿Verdad o disparate?

        private static double PlayDisparate(bool careful, Random bot, SentenceBank bank)
        {
            var run = new TimeTrial.Run("disparate", Day);
            var director = new DisparateDirector(bank, new Random(0)) { NoBursts = true };
            double t = 0;
            while (true)
            {
                var spec = director.Next(run.Level, false, false, run.Rng());
                double minThink = TimeTrial.MinThinkSeconds("disparate", spec.S.n);
                double answerAt = t + DisparateArrive + Math.Max(minThink, careful ? CareTime("disparate", bot) : minThink + ChanceTime(bot));
                if (answerAt > TimeTrial.Seconds) break;
                bool correct = bot.NextDouble() < (careful ? Careful : 0.5);
                run.Register(correct);
                t = answerAt + (correct ? DisparateFarewell : TimeTrial.ErrorFeedbackSeconds + DisparateFarewell);
                if (t >= TimeTrial.Seconds) break;
            }
            return run.Score;
        }

        // ------------------------------------------------------------------ Rescate relámpago

        private static double PlayRadar(bool careful, Random bot)
        {
            var run = new TimeTrial.Run("radar", Day);
            double t = 0;
            while (true)
            {
                int level = run.Level;
                var round = RadarContract.NextRound(level, run.Rng());
                double watch = RadarContract.WatchMinSeconds + (RadarContract.WatchMaxSeconds - RadarContract.WatchMinSeconds) * run.Rng(1).NextDouble();
                int need = round.Needed;
                // el que adivina marca tantos tipos como hacen falta para lograr la ronda (la mejor jugada a ciegas) y toca «¡Rescatar!»: ~0,3 s más 0,25 s por tipo
                double answer = careful ? CareTime("radar", bot) : 0.3 + 0.25 * need;
                double answerAt = t + watch + round.ExposureMs / 1000.0 + RadarContract.MaskMs / 1000.0 + answer;
                if (answerAt > TimeTrial.Seconds) break;
                bool success;
                if (careful) success = bot.NextDouble() < Careful;
                else
                {
                    var types = round.Types();
                    var all = new List<int> { 0, 1, 2, 3, 4, 5 };
                    int hits = 0;
                    for (int k = 0; k < need; k++)
                    {
                        int j = bot.Next(all.Count);
                        if (types.Contains((CapsuleType)all[j])) hits++;
                        all.RemoveAt(j);
                    }
                    success = RadarContract.Success(round.Count, hits, need - hits);
                }
                run.Register(success);
                t = answerAt + RadarContract.RevealSeconds;
                if (t >= TimeTrial.Seconds) break;
            }
            return run.Score;
        }

        // ------------------------------------------------------------------ Acoplamiento

        private static double PlayDocking(bool careful, Random bot)
        {
            var run = new TimeTrial.Run("acoplamiento", Day);
            double t = 0;
            double minThink = TimeTrial.MinThinkSeconds("acoplamiento");
            while (true)
            {
                int level = run.Level;
                var trial = DockingContract.NextTrial(level, run.Rng());
                double decideAt = t + DockingContract.ArriveMs / 1000.0;
                double deadline = DockingContract.DeadlineMs(level, false) / 1000.0;
                double resp = Math.Max(minThink, careful ? CareTime("acoplamiento", bot) : minThink + ChanceTime(bot));
                bool timedOut = resp >= deadline;
                double answerAt = decideAt + (timedOut ? deadline : resp);
                if (answerAt > TimeTrial.Seconds) break;
                bool correct = !timedOut && bot.NextDouble() < (careful ? Careful : 0.5);
                run.Register(correct);
                double after = correct ? DockingContract.OkRevealSeconds(trial.Mirrored) : DockingContract.MissRevealSeconds(trial.Mirrored);
                t = answerAt + after;
                if (t >= TimeTrial.Seconds) break;
            }
            return run.Score;
        }
    }
}
