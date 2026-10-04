using System.IO;
using NUnit.Framework;
using UnityEngine;
using NeuroVida.Games.Shared;

namespace NeuroVida.Games.Tests
{
    /// <summary>
    /// El tutorial guiado COMÚN (Games/Shared/GuidedTutorial.cs): el guion de la ronda guiada («Saltar» lleva a la partida; un error repite el paso), el reloj de
    /// «Cómo se juega» desde la pausa (el tiempo que quedaba es el mismo que queda después), el arranque suave de la versión corta, y una guardia de que el código de
    /// la ronda guiada de cada juego NO toca puntaje, DDA, rachas ni conteos (lo que ya ve la ronda guiada no se guarda).
    /// </summary>
    public class GuidedTutorialTests
    {
        // ------------------------------------------------------------------ el guion

        [Test]
        public void TheScript_AdvancesOnSuccess_AndRepeatsTheSameStepOnFailure()
        {
            var s = new GuidedScript(4);
            Assert.IsFalse(s.Finished);
            Assert.AreEqual(0, s.Index);
            s.Failure();
            s.Failure();
            Assert.AreEqual(0, s.Index, "un error repite el mismo paso");
            Assert.AreEqual(2, s.Failures);
            s.Success();
            Assert.AreEqual(1, s.Index);
            s.Success(); s.Success();
            Assert.IsFalse(s.Finished);
            s.Success();
            Assert.IsTrue(s.Finished, "se acabaron los pasos");
            s.Success();
            Assert.AreEqual(4, s.Index, "una vez terminado, ya no avanza");
        }

        [Test]
        public void TheScript_Skip_EndsItAtOnce_AndTakesYouToTheGame()
        {
            var s = new GuidedScript(4);
            s.Success();
            s.Skip();
            Assert.IsTrue(s.Skipped);
            Assert.IsTrue(s.Finished, "«Saltar tutorial» termina de una vez");
            s.Failure();
            Assert.AreEqual(0, s.Failures, "tras saltar no se cuenta nada más");
            Assert.AreEqual(1, s.Index);
        }

        // ------------------------------------------------------------------ «Cómo se juega»: el reloj y el estado

        [Test]
        public void HowToClock_ShiftsEveryTimeAnchorByTheDurationOfTheTutorial()
        {
            // el Reto terminaba en el segundo 130 y se abrió «Cómo se juega» en el 40; duró 25 s
            var clock = new HowToClock(40f);
            float spent = clock.Finish(65f);
            Assert.AreEqual(25f, spent, 1e-5f);
            float endsAt = 130f;
            float remainingBefore = endsAt - 40f;
            float shifted = HowToClock.Shift(endsAt, spent);
            Assert.AreEqual(155f, shifted, 1e-5f);
            Assert.AreEqual(remainingBefore, shifted - 65f, 1e-5f, "el tiempo que quedaba antes es el mismo que queda después");
        }

        [Test]
        public void HowToClock_NeverGoesBackwards_AndAnUnshiftedAnchorWouldHaveLostTime()
        {
            Assert.AreEqual(0f, new HowToClock(50f).Finish(49f), "nunca negativo");
            Assert.AreEqual(100f, HowToClock.Shift(100f, 0f));
            // sin correr el ancla, el tutorial se le habría descontado al Reto
            float remainingWithoutShift = 130f - 65f;
            Assert.Less(remainingWithoutShift, 130f - 40f);
        }

        [Test]
        public void HowToClock_TwoTutorialsInARow_AddUp()
        {
            float endsAt = 120f;
            endsAt = HowToClock.Shift(endsAt, new HowToClock(10f).Finish(30f));
            endsAt = HowToClock.Shift(endsAt, new HowToClock(50f).Finish(58f));
            Assert.AreEqual(148f, endsAt, 1e-5f);
        }

        // ------------------------------------------------------------------ la versión corta del inicio

        [Test]
        public void TheShortVersion_StartsSoft_InSeniorsAndAdultsAlike()
        {
            Assert.AreEqual(1f, Assessment.SoftStartLevel(AgeBand.Senior));
            Assert.AreEqual(3f, Assessment.SoftStartLevel(AgeBand.Adult));
            Assert.AreEqual(3f, Assessment.SoftStartLevel(AgeBand.Under18));
            Assert.AreEqual(2f, Assessment.SoftStartLevel(AgeBand.Adult, 2f));
            Assert.AreEqual(1f, Assessment.SoftStartLevel(AgeBand.Senior, 2f), "en mayores siempre el nivel 1");
        }

        // ------------------------------------------------------------------ guardia: la ronda guiada no cuenta

        private static string GuidedRegion(string relativePath)
        {
            string path = Path.Combine(Application.dataPath, "Scripts", relativePath);
            Assert.IsTrue(File.Exists(path), "no está " + path);
            string text = File.ReadAllText(path);
            int a = text.IndexOf("// <guided>", System.StringComparison.Ordinal);
            int b = text.IndexOf("// </guided>", System.StringComparison.Ordinal);
            Assert.Greater(a, 0, relativePath + ": falta la marca // <guided>");
            Assert.Greater(b, a, relativePath + ": falta la marca // </guided>");
            return text.Substring(a, b - a);
        }

        private static void AssertDoesNotTouch(string relativePath, params string[] forbidden)
        {
            string region = GuidedRegion(relativePath);
            foreach (var token in forbidden)
                Assert.IsFalse(region.Contains(token), $"{relativePath}: la ronda guiada no puede tocar «{token}» (no suma puntos, no toca el DDA ni el avance y no se guarda)");
            StringAssert.Contains("GuidedScript", region.Replace("GuidedTutorial", ""), relativePath + ": la ronda guiada se arma con el guion común");
        }

        [Test]
        public void TheGuidedRound_OfEveryGame_NeverTouchesTheScoreTheDdaOrTheCounts()
        {
            // Rastro de luz: la ronda guiada no pasa por RastroSession.Complete (sus pruebas lo cubren por dentro: TheGuidedRound_LeavesNoTrace…)
            AssertDoesNotTouchNoScript("Games/Secuencia/RastroGameController.cs", "_session.Complete", "_counter", ".Tally", "_dda", "Dda.");
            AssertDoesNotTouch("Games/Freno/BrakeGameController.cs", "_dda", "_points", "_streak", "_trials", "_goTrials", "_goCorrect", "_stopsOk", "_launched",
                "_bestSsd", "_ssd", "_rtSum", "_goRts", "_stopSsds", "_stopResponded", "Register(");
            AssertDoesNotTouch("Games/Aterrizaje/LandingGameController.cs", "_dda", "_points", "_streak", "_bestStreak", "_errors", "_hits", "_bullseyes",
                "_trueFractions", "_givenFractions", "Register(", "(Reveal(", " Reveal(");
            AssertDoesNotTouch("Games/Meteoros/MeteorGameController.cs", "_dda", "_points", "_streak", "_bestStreak", "_rescued", "_resolved", "_tally", "_allMs",
                "Register(", "ResolveTap(", "ResolvePass(", "RescueEffect(", "LightConstellation(");
            AssertDoesNotTouch("Games/Anagramas/PuntaGameController.cs", "_dda", "_points", "_streak", "_bestStreak", "_resolved", "_tally", "_credit", "_director",
                "Register(", "LaunchFlyer(", "FillLucero(", "PlayerPrefs");
            AssertDoesNotTouch("Games/Calculo/CalculoGameController.cs", "_dda", "_points", "_streak", "_bestStreak", "_resolved", "_tally", "_credit", "_director",
                "Register(", "RecordLoad(", "PlayerPrefs");
        }

        private static void AssertDoesNotTouchNoScript(string relativePath, params string[] forbidden)
        {
            string region = GuidedRegion(relativePath);
            foreach (var token in forbidden)
                Assert.IsFalse(region.Contains(token), $"{relativePath}: la ronda guiada no puede tocar «{token}»");
        }

        [Test]
        public void TheGuidedRound_OfEveryGame_LearnsByDoing_EachStepWaitsForATouchAndNothingAdvancesByTimeAlone()
        {
            // «Aprender haciendo» (Ricardo, 3-oct): las explicaciones quedan puestas hasta un toque; ninguna pausa fija ≥ 1 s entre un paso y el siguiente.
            var files = new[]
            {
                ("Games/Secuencia/RastroGameController.cs", true), ("Games/Freno/BrakeGameController.cs", true), ("Games/Aterrizaje/LandingGameController.cs", true),
                ("Games/Meteoros/MeteorGameController.cs", true), ("Games/Stroop/StroopGameController.cs", true), ("Games/Anagramas/PuntaGameController.cs", false), ("Games/Calculo/CalculoGameController.cs", true)
            };
            foreach (var (file, usesCommonWait) in files)
            {
                string region = GuidedRegion(file);
                if (usesCommonWait) Assert.IsTrue(region.Contains("WaitForContinue") || region.Contains("coach."), file + ": cada paso es un foco de Nubi entrenadora o espera un toque (GuidedTutorial.Coach / WaitForContinue)");
                foreach (System.Text.RegularExpressions.Match m in System.Text.RegularExpressions.Regex.Matches(region, @"(?:Hold|Wait)\(\s*(\d+(?:\.\d+)?)f?\s*\)"))
                    Assert.Less(float.Parse(m.Groups[1].Value, System.Globalization.CultureInfo.InvariantCulture), 1f, file + ": una pausa fija de " + m.Value + " avanza el tutorial sin que la persona toque");
            }
        }

        [Test]
        public void TheGuidedRound_IsReachableFromThePause_ForEveryGameWithATutorial()
        {
            // cada juego con tutorial dice cuándo «Cómo se juega» está disponible y cómo retoma su partida
            foreach (var file in new[] { "Games/Secuencia/RastroGameController.cs", "Games/Freno/BrakeGameController.cs", "Games/Aterrizaje/LandingGameController.cs", "Games/Meteoros/MeteorGameController.cs", "Games/Anagramas/PuntaGameController.cs", "Games/Calculo/CalculoGameController.cs" })
            {
                string text = File.ReadAllText(Path.Combine(Application.dataPath, "Scripts", file));
                StringAssert.Contains("override bool HowToReady", text, file);
                StringAssert.Contains("override void HowToSuspend", text, file);
                StringAssert.Contains("override void HowToResume", text, file);
                StringAssert.Contains("PollTutorialSkip", text.Replace("RastroGameController", "") + (file.Contains("Secuencia") ? "PollTutorialSkip" : ""), file); // Rastro lee los toques en ReadPointer
                StringAssert.Contains("RunTutorialIfNeeded", text, file);
                StringAssert.Contains("BuildTutorial", text, file);
            }
        }
    }
}
