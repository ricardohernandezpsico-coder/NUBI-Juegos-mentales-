using System.Collections.Generic;
using System.Text;
using NUnit.Framework;
using NeuroVida.Contracts;
using NeuroVida.Games.Bodega;
using NeuroVida.Games.Correo;
using NeuroVida.Games.Engranajes;
using NeuroVida.Games.Parejas;
using NeuroVida.Games.Satelites;

namespace NeuroVida.Games.Tests
{
    /// <summary>
    /// La primera partida de cada juego con tarjetas «NUEVO» (Tarea 191, «abajo y sube rápido»), con el rating sembrado de 0,45: la etapa de cada ensayo para alguien nuevo que nunca se equivoca y para alguien que falla el 3.er ensayo, y las tarjetas que ve (las del tutorial ya están marcadas). Escribe la tabla en el registro de la prueba
    /// (la lámina <c>docs/previews/tarea-191.png</c> sale de ahí) y vigila lo que importa: llega al nivel sembrado y no lo pasa; tras un error no vuelve a subir de a una etapa.
    /// </summary>
    public class FirstGameTableTests
    {
        private SequenceConfigDetails _cfg;

        [SetUp]
        public void SetUp() { AdaptiveDifficulty.FastCalibration = false; AdaptiveDifficulty.ModeFloor = -1f; AdaptiveDifficulty.ModeCeiling = -1f; _cfg = new SequenceConfigDetails { age_band = "ADULT", has_dda_rating = true, dda_rating = 0.45f, first_game = true }; }

        [TearDown]
        public void TearDown() { AdaptiveDifficulty.RampPending = false; }

        private AdaptiveDifficulty Engine(string game)
        {
            AdaptiveDifficulty.ConfigureMode(_cfg);
            switch (game)
            {
                case "correo": return MailContract.CreateEngine(_cfg);
                case "parejas": return ConstelacionContract.CreateEngine(_cfg);
                case "satelites": return SatelliteMetrics.CreateEngine(_cfg);
                case "bodega": return new AdaptiveDifficulty(BodegaContract.MaxLevel, AgeBand.Adult, AdaptiveDifficulty.StartRating(_cfg, BodegaContract.MaxLevel), BodegaContract.DdaStepUp, useReaction: false);
                default: return new AdaptiveDifficulty(EngranajesContract.MaxLevel, AgeBand.Adult, AdaptiveDifficulty.StartRating(_cfg, EngranajesContract.MaxLevel), 0.5f, useReaction: false);
            }
        }

        /// <summary>Las etapas con tarjeta «NUEVO» de cada juego que el tutorial NO deja vistas (las de la etapa 1 y, en Correo, la 2 ya lo están).</summary>
        private static List<int> CardStages(string game)
        {
            var list = new List<int>();
            switch (game)
            {
                case "engranajes": for (int s = 2; s <= EngranajesContract.MaxLevel; s++) if (EngranajesContract.StageFor(s).Intro != NeuroVida.Games.Engranajes.Intro.None) list.Add(s); break;
                case "bodega": for (int s = 2; s <= BodegaContract.MaxLevel; s++) { var st = BodegaContract.StageOf(s); var prev = BodegaContract.StageOf(s - 1); if ((st.Moves > 0 && prev.Moves == 0) || (st.Spin && !prev.Spin)) list.Add(s); } break;
                case "parejas": for (int s = 2; s <= ConstelacionContract.MaxLevel; s++) if (ConstelacionContract.Stage(s).Intro != ConIntro.None) list.Add(s); break;
                case "correo": for (int s = 3; s <= MailContract.MaxLevel; s++) if (MailContract.Stage(s).Intro != MailIntro.None) list.Add(s); break;
            }
            return list;
        }

        [Test]
        public void TheTableOfTheFirstGame_PerGame_ForSomeoneWhoNeverErrs_AndForSomeoneWhoFailsTheThirdTrial()
        {
            var sb = new StringBuilder();
            foreach (var game in new[] { "parejas", "bodega", "engranajes", "satelites" })        // Correo cuenta por DÍAS (la etapa sube con un día perfecto: MailRun.Advance), no por ensayo: va aparte en la tabla
            {
                // A: nadie se equivoca
                var a = Engine(game);
                float seeded = a.RampTarget;
                int target = (int)System.Math.Floor(seeded);
                var stagesA = new List<int>();
                int arrival = -1;
                for (int trial = 1; trial <= 12; trial++)
                {
                    stagesA.Add(a.PresentedLevel);
                    if (arrival < 0 && a.PresentedLevel >= target) arrival = trial;
                    if (a.Ramping) Assert.LessOrEqual(a.Rating, seeded + 1e-4f, game + ": la subida nunca pasa del sembrado");
                    a.Register(true);
                }
                Assert.AreEqual(1, stagesA[0], game + ": parte en la etapa 1");
                Assert.AreEqual(target, arrival, game + ": el ensayo n va en la etapa n: llega al nivel sembrado en el ensayo " + target);
                Assert.AreEqual(seeded, Engine(game).RampTarget, 1e-4f);

                // B: falla el 3.er ensayo
                var b = Engine(game);
                var stagesB = new List<int>();
                for (int trial = 1; trial <= 8; trial++)
                {
                    stagesB.Add(b.PresentedLevel);
                    b.Register(trial != 3);
                }
                Assert.IsFalse(b.Ramping, game + ": el primer error termina la subida");
                Assert.Less(stagesB[3], stagesB[2], game + ": tras el error baja (la regla de siempre) en vez de seguir subiendo");

                var cardsA = new List<string>();
                foreach (int s in CardStages(game)) if (s <= System.Math.Max(a.PeakLevel, target)) { int t = stagesA.IndexOf(s) + 1; if (t > 0 && s <= stagesA[stagesA.Count - 1]) cardsA.Add("etapa " + s + " en el ensayo " + t); }
                var cardsB = new List<string>();
                foreach (int s in CardStages(game)) { int t = stagesB.IndexOf(s) + 1; if (t > 0) cardsB.Add("etapa " + s + " en el ensayo " + t); }
                sb.AppendLine(game + " | sembrado " + seeded.ToString("F1") + " (etapa " + target + ") | A: etapas " + string.Join(",", stagesA.GetRange(0, 8)) + " | llega en el ensayo " + arrival + " | tarjetas: " + (cardsA.Count == 0 ? "ninguna" : string.Join("; ", cardsA)) +
                              " | B: etapas " + string.Join(",", stagesB) + " | tarjetas: " + (cardsB.Count == 0 ? "ninguna" : string.Join("; ", cardsB)));
            }
            TestContext.WriteLine(sb.ToString());
        }
    }
}
