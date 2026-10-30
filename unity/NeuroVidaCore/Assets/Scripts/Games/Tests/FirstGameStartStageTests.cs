using System.Collections.Generic;
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
    /// Tarea 191, paso 0 (hallazgo H3 de <c>docs/cuenta-y-tutoriales-diagnostico.md</c>): la app siembra el rating de CADA juego con 0,45 (<c>Baseline.DEFAULT_LEVEL</c>: la estimación de quien no midió nada o saltó el Primer vuelo)
    /// y manda <c>has_dda_rating</c> = true; Unity parte de <c>1 + rating × etapas</c>. Esta prueba mide en qué etapa parten DE VERDAD los cinco juegos con tarjetas «NUEVO» y escalera larga, mientras el tutorial enseña la etapa 1.
    /// </summary>
    public class FirstGameStartStageTests
    {
        private const float SeededRating = 0.45f;                  // Baseline.DEFAULT_LEVEL (Kotlin)

        private static SequenceConfigDetails Seeded() => new SequenceConfigDetails { level = 3, base_intensity = 0, age_band = "ADULT", has_dda_rating = true, dda_rating = SeededRating, show_tutorial = true };

        private static AdaptiveDifficulty Engine(string game)
        {
            var c = Seeded();
            switch (game)
            {
                case "correo": return MailContract.CreateEngine(c);
                case "parejas": return ConstelacionContract.CreateEngine(c);
                case "satelites": return SatelliteMetrics.CreateEngine(c);
                case "bodega": return new AdaptiveDifficulty(BodegaContract.MaxLevel, AgeBand.Adult, AdaptiveDifficulty.StartRating(c, BodegaContract.MaxLevel), BodegaContract.DdaStepUp, useReaction: false);
                default: return new AdaptiveDifficulty(EngranajesContract.MaxLevel, AgeBand.Adult, AdaptiveDifficulty.StartRating(c, EngranajesContract.MaxLevel), 0.5f, useReaction: false);
            }
        }

        // juego → (etapa real de partida, etapa presentada en los 2 primeros ensayos: el calentamiento baja uno)
        private static readonly Dictionary<string, (int level, int presented)> Expected = new Dictionary<string, (int, int)>
        {
            { "correo", (5, 4) }, { "parejas", (9, 8) }, { "bodega", (6, 5) }, { "engranajes", (6, 5) }, { "satelites", (6, 5) },
        };

        [Test]
        public void WithTheSeededRating_TheFiveLongLadderGames_StartWellAboveStage1_WhileTheTutorialTeachesStage1()
        {
            foreach (var kv in Expected)
            {
                var dda = Engine(kv.Key);
                TestContext.WriteLine($"{kv.Key}: rating {dda.Rating:F2}, etapa real {dda.Level}, presentada en los 2 primeros ensayos {dda.PresentedLevel}");
                Assert.AreEqual(kv.Value.level, dda.Level, kv.Key + ": etapa real de partida con el rating sembrado de 0,45");
                Assert.AreEqual(kv.Value.presented, dda.PresentedLevel, kv.Key + ": etapa que se presenta en los 2 primeros ensayos (calentamiento)");
                Assert.GreaterOrEqual(dda.PresentedLevel, 4, kv.Key + ": parte arriba, no en la etapa 1 que enseña el tutorial");
            }
        }

        [Test]
        public void Correo_WithTheSeededRating_NoLongerPilesUpTheNewCards()
        {
            var dda = MailContract.CreateEngine(Seeded());
            int cards = 0;
            foreach (var intro in MailContract.IntrosBetween(dda.Level - 1, dda.Level)) cards++;
            TestContext.WriteLine("correo: tarjetas «NUEVO» antes del primer día = " + cards);
            Assert.LessOrEqual(cards, 1, "sin relleno hacia atrás: antes eran 4 seguidas");
        }
    }
}
