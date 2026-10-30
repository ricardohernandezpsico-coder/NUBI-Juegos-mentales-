using NUnit.Framework;
using NeuroVida.Contracts;

namespace NeuroVida.Games.Tests
{
    /// <summary>
    /// «Abajo y sube rápido» (Tarea 191, decisión de Ricardo): la PRIMERA partida normal de un juego sin historial parte en la etapa 1 (la del tutorial) y sube UN paso por cada ensayo bien hecho hasta el nivel sembrado; al primer error vuelve el ajuste de siempre desde donde esté.
    /// </summary>
    public class AdaptiveRampTests
    {
        private SequenceConfigDetails _cfg;

        [SetUp]
        public void SetUp() { AdaptiveDifficulty.RampPending = false; AdaptiveDifficulty.FastCalibration = false; AdaptiveDifficulty.ModeFloor = -1f; AdaptiveDifficulty.ModeCeiling = -1f; _cfg = new SequenceConfigDetails { age_band = "ADULT", has_dda_rating = true, dda_rating = 0.45f }; }

        [TearDown]
        public void TearDown() { AdaptiveDifficulty.RampPending = false; }

        private AdaptiveDifficulty New(int maxLevel, float step = 0.2f)
        {
            AdaptiveDifficulty.ConfigureMode(_cfg);
            return new AdaptiveDifficulty(maxLevel, AgeBand.Adult, AdaptiveDifficulty.StartRating(_cfg, maxLevel), step, useReaction: false);
        }

        [Test]
        public void TheFirstGame_StartsAtStage1_AndClimbsOneStagePerCorrectTrial_UpToTheSeededLevelAndNoFurther()
        {
            _cfg.first_game = true;
            var dda = New(12);                                           // sembrado: 1 + 0,45 × 12 = 6,4
            Assert.IsTrue(dda.Ramping);
            Assert.AreEqual(1, dda.Level);
            Assert.AreEqual(1, dda.PresentedLevel, "parte en la etapa 1, sin el calentamiento que baja uno");
            var seen = new System.Collections.Generic.List<int> { dda.PresentedLevel };
            for (int i = 0; i < 5; i++) { dda.Register(true); seen.Add(dda.PresentedLevel); }
            CollectionAssert.AreEqual(new[] { 1, 2, 3, 4, 5, 6 }, seen, "una etapa por ensayo bien hecho: el ensayo n va en la etapa n hasta la del sembrado");
            Assert.IsTrue(dda.Ramping, "en la etapa 6 todavía falta el 0,4 que queda hasta el sembrado");
            dda.Register(true);
            Assert.IsFalse(dda.Ramping, "al llegar al sembrado termina");
            Assert.AreEqual(6.4f, dda.Rating, 0.001f, "y queda EXACTAMENTE en el sembrado: la subida nunca lo pasa");
        }

        [Test]
        public void TheClimbNeverRaisesTheRatingBeyondTheSeededOne()
        {
            _cfg.first_game = true;
            var dda = New(18);                                           // sembrado: 9,1
            float seeded = AdaptiveDifficulty.StartRating(_cfg, 18);
            while (dda.Ramping) { Assert.LessOrEqual(dda.Rating, seeded + 1e-4f); dda.Register(true); }
            Assert.AreEqual(seeded, dda.Rating, 1e-4f);
        }

        [Test]
        public void TheFirstError_EndsTheClimb_AndTheUsualAdjustmentContinuesFromWhereItIs()
        {
            _cfg.first_game = true;
            var dda = New(12);
            dda.Register(true); dda.Register(true);                      // etapa 3
            Assert.AreEqual(3f, dda.Rating, 0.001f);
            dda.Register(false);
            Assert.IsFalse(dda.Ramping);
            Assert.Less(dda.Rating, 3f, "el error baja con la regla de siempre, desde donde esté");
            Assert.Greater(dda.Rating, 1f - 1e-4f);
            float after = dda.Rating;
            dda.Register(true);
            Assert.Less(dda.Rating - after, 1f, "ya no sube de a una etapa por acierto: vuelve el paso chico del ajuste normal");
        }

        [Test]
        public void WithoutTheFlag_NothingChanges()
        {
            _cfg.first_game = false;
            var dda = New(12);
            Assert.IsFalse(dda.Ramping);
            Assert.AreEqual(6, dda.Level);
            Assert.AreEqual(5, dda.PresentedLevel, "el calentamiento de siempre: uno por debajo en los 2 primeros ensayos");
        }

        [Test]
        public void TheClimbIsOnlyForTheFirstNormalGame_NotForTheTimeTrialOrTheAssessment()
        {
            _cfg.first_game = true; _cfg.time_trial = true;
            Assert.IsFalse(New(12).Ramping, "el Contrarreloj no sube de a una etapa: su pista es fija");
            _cfg.time_trial = false; _cfg.assessment = true;
            Assert.IsFalse(New(12).Ramping, "la evaluación del Primer vuelo no cambia");
        }

        [Test]
        public void AGameSeededAtStage1_HasNothingToClimb()
        {
            _cfg.first_game = true; _cfg.dda_rating = 0.05f;             // 1 + 0,05 × 12 = 1,6: ya parte en la etapa 1
            var dda = New(12);
            Assert.IsFalse(dda.Ramping);
            Assert.AreEqual(1, dda.Level);
        }
    }
}
