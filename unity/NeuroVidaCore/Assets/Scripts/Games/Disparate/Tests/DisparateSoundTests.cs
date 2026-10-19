using System.IO;
using System.Text.RegularExpressions;
using NeuroVida.Games.Shared;
using NUnit.Framework;
using UnityEngine;

namespace NeuroVida.Games.Disparate.Tests
{
    /// <summary>
    /// El sonido de ¿Verdad o disparate? en «Madera cálida» (Fase E, Tarea 139): el juego ya NO tiene sonidos propios (DisparateSounds.cs desapareció): la sintonía es SweepUp a −3 dB, «se perdió la señal» Wrong, la transmisión perfecta SOLO Streak (sin el LevelUp que sonaba encima),
    /// «gracias por avisar» el Notice del propio aviso, y el acierto y el error quedan solo con los comunes (la onda y la estática, que sonaban encima, se quitaron).
    /// </summary>
    public class DisparateSoundTests
    {
        private bool _soundWas;

        [SetUp]
        public void SetUp() { _soundWas = GameFeel.SoundOn; GameFeel.SoundOn = true; WoodCore.Reset(); }

        [TearDown]
        public void TearDown() { WoodCore.Reset(); GameFeel.SoundOn = _soundWas; }

        private static string Folder() => Path.Combine(Application.dataPath, "Scripts", "Games", "Disparate");

        private static string Controller() => File.ReadAllText(Path.Combine(Folder(), "DisparateGameController.cs"));

        /// <summary>El cuerpo del método [signature] (hasta el siguiente método privado), para mirar solo lo que suena ahí.</summary>
        private static string Body(string text, string signature)
        {
            int a = text.IndexOf(signature, System.StringComparison.Ordinal);
            Assert.GreaterOrEqual(a, 0, signature + " existe");
            int b = text.IndexOf("        private ", a + signature.Length, System.StringComparison.Ordinal);
            return text.Substring(a, b > a ? b - a : text.Length - a);
        }

        [Test]
        public void TheGameHasNoSoundsOfItsOwn_DisparateSoundsIsGone()
        {
            Assert.IsFalse(File.Exists(Path.Combine(Folder(), "DisparateSounds.cs")), "DisparateSounds.cs desapareció entera: no le quedaba ningún propio");
            string text = Controller();
            StringAssert.DoesNotContain("DisparateSounds", text);
            StringAssert.DoesNotContain("PlayClip", text, "el juego suena todo por WoodCore");
            foreach (var gone in new[] { "WaveRing(", "StaticClick(" }) StringAssert.DoesNotContain(gone, text, gone + " se quitó (la onda y la estática ya no suenan)");
        }

        [Test]
        public void EverythingTheGameSoundsIsPalette_AndTheOnlyExtrasAreAskedForBeforeTheCountdown()
        {
            string text = Controller();
            StringAssert.Contains("WoodCore.Play(WoodSfx.SweepUp, gainDb: -3f)", text, "la sintonía");
            StringAssert.Contains("WoodCore.Play(WoodSfx.Wrong)", text, "se perdió la señal");
            StringAssert.Contains("WoodCore.Play(WoodSfx.Streak)", text, "la transmisión perfecta");
            StringAssert.Contains("RequestSounds();", text);
            GameFeel.Warm("disparate");
            DisparateGameController.RequestSounds();
            int guard = 0; while (!WoodCore.Idle) { WoodCore.Step(1f); Assert.Less(++guard, 200000); }
            foreach (var sfx in new[] { WoodSfx.SweepUp, WoodSfx.Streak, WoodSfx.Wrong, WoodSfx.Notice }) Assert.IsNotNull(WoodCore.Get(sfx), sfx.ToString());
            for (int n = 1; n <= 7; n++) Assert.IsNotNull(WoodCore.Get(WoodSfx.Correct, n), "Correct " + n + " va con el núcleo");
            Assert.AreEqual(0, WoodCore.LateBakes);
        }

        [Test]
        public void ThePerfectTransmission_IsAStreakMilestoneAlone_NoLevelUpOnTop()
        {
            string perfect = Body(Controller(), "private IEnumerator PerfectTransmission()");
            StringAssert.Contains("WoodCore.Play(WoodSfx.Streak)", perfect);
            StringAssert.DoesNotContain("GameFeel.LevelUp()", perfect, "es un hito de racha (× 1,5), no un nivel: se quitó el LevelUp que sonaba encima");
            StringAssert.Contains("sound: false", perfect, "el aviso de la transmisión perfecta sigue mudo (ya suena el Streak)");
            // «¡Ráfaga!» SÍ es un nivel y conserva su LevelUp
            StringAssert.Contains("GameFeel.LevelUp()", Controller());
        }

        [Test]
        public void TheRightAndWrongAnswers_SoundOnlyTheCommonOnes_AndTheUnclearThanksUsesTheNoticeOfItsToast()
        {
            string text = Controller();
            string right = Body(text, "private IEnumerator CorrectFeedback(");
            string wrong = Body(text, "private IEnumerator WrongFeedback(");
            StringAssert.Contains("GameFeel.Correct(_streak)", right);
            StringAssert.Contains("GameFeel.Wrong()", wrong);
            foreach (var body in new[] { right, wrong }) Assert.AreEqual(0, Regex.Matches(body, @"WoodCore\.Play|PlayNote|PlayCue|PlayTone").Count, "una sola respuesta, un solo sonido");
            // «Gracias, la revisaremos»: el aviso suena su Notice (ya no va mudo)
            var thanks = Regex.Match(text, @"_toast\.Show\(""Gracias, la revisaremos""[^;]*;");
            Assert.IsTrue(thanks.Success, "el aviso existe");
            StringAssert.DoesNotContain("sound: false", thanks.Value, "el aviso ya no va mudo: suena el Notice");
            // y «se perdió la señal» sigue con su aviso mudo (suena el Wrong)
            var lost = Regex.Match(text, @"_toast\.Show\(""Se perdió la señal""[^;]*;");
            StringAssert.Contains("sound: false", lost.Value);
        }
    }
}
