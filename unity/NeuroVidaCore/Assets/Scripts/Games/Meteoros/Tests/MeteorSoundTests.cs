using System.IO;
using NeuroVida.Games.Shared;
using NUnit.Framework;
using UnityEngine;

namespace NeuroVida.Games.Meteoros.Tests
{
    /// <summary>
    /// El sonido de Lluvia de meteoros en «Madera cálida» (Fase E, Tarea 139): el 330 Hz de «dejar pasar una inventada» se queda como una señal suave (misma altura y duración, −9 dB) y no está doblado con ningún acierto común: es el único sonido de ese acierto.
    /// </summary>
    public class MeteorSoundTests
    {
        private bool _soundWas;

        [SetUp]
        public void SetUp() { _soundWas = GameFeel.SoundOn; GameFeel.SoundOn = true; WoodCore.Reset(); }

        [TearDown]
        public void TearDown() { WoodCore.Reset(); GameFeel.SoundOn = _soundWas; }

        private static string Controller() => File.ReadAllText(Path.Combine(Application.dataPath, "Scripts", "Games", "Meteoros", "MeteorGameController.cs"));

        [Test]
        public void TheLetPassSignal_KeepsItsPitchAndDuration_AndStartsRightAway()
        {
            Assert.AreEqual(330.0, MeteorSounds.LetPassHz, "330 Hz como hoy");
            Assert.AreEqual(0.12, MeteorSounds.LetPassSeconds, 1e-9, "0,12 s como hoy");
            WoodSounds.Render(WoodSounds.MakeCue(MeteorSounds.LetPassHz, MeteorSounds.LetPassSeconds), out var l, out var r);
            Assert.Greater(SoundMeter.PitchClarity(l, r, 330.0, 2, 120), 0.5, "en su altura");
            Assert.LessOrEqual(SoundMeter.OnsetMs(l, r), 2.0, "y empieza de inmediato");
            Assert.LessOrEqual(SoundMeter.DurationMs(l, r, 30.0), 250.0, "y cae a −30 dB en menos de 0,25 s");
            Assert.AreEqual(-9f, MeteorSounds.LetPassGainDb);
            CollectionAssert.Contains(WoodCore.AllowedGainsDb, MeteorSounds.LetPassGainDb, "−9 dB es un paso permitido de la paleta");
        }

        [Test]
        public void TheGameAsksForTheSignal_BeforeTheCountdown_AndItIsTheOnlySoundOfThatSuccess()
        {
            GameFeel.Warm("meteoros");
            MeteorSounds.Request();
            int guard = 0; while (!WoodCore.Idle) { WoodCore.Step(1f); Assert.Less(++guard, 200000); }
            Assert.IsNotNull(WoodCore.GetExtra(WoodCore.CueKey(MeteorSounds.LetPassHz, MeteorSounds.LetPassSeconds)), "la señal está pedida");
            WoodCore.ResetCounters();
            MeteorSounds.PlayLetPass();
            Assert.AreEqual(0, WoodCore.LateBakes);
            string text = Controller();
            StringAssert.DoesNotContain("PlayTone", text, "ya no hay tonos sueltos");
            Assert.AreEqual(2, System.Text.RegularExpressions.Regex.Matches(text, @"MeteorSounds\.PlayLetPass\(\)").Count, "en el juego y en la práctica");
            StringAssert.Contains("MeteorSounds.Request()", text);
            CollectionAssert.IsEmpty(System.Linq.Enumerable.ToArray(MeteorSounds.All()), "Meteoros no tiene sonidos propios");
        }
    }
}
