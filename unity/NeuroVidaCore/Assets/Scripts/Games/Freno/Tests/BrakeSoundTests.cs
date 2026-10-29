using System.IO;
using System.Text.RegularExpressions;
using NeuroVida.Games.Shared;
using NUnit.Framework;
using UnityEngine;

namespace NeuroVida.Games.Freno.Tests
{
    /// <summary>
    /// El sonido de Freno de emergencia en «Madera cálida» (Fase E, Tarea 139). La señal de salida es el ESTÍMULO del tiempo de reacción: conserva su altura (784 Hz, ±1 %), su duración (60 ms) y su nivel, empieza de inmediato (≤ 2 ms) y NUNCA espera a calcularse. «¡Más rápido!» es el Knock de la paleta, el rugido y el resoplido
    /// se quitaron (ya suena el acierto) y la sirena de «¡ALTO!» se queda con PlayTone: es el único uso vivo.
    /// </summary>
    public class BrakeSoundTests
    {
        private bool _soundWas;

        [SetUp]
        public void SetUp() { _soundWas = GameFeel.SoundOn; GameFeel.SoundOn = true; WoodCore.Reset(); }

        [TearDown]
        public void TearDown() { WoodCore.Reset(); GameFeel.SoundOn = _soundWas; }

        private static string Controller() => File.ReadAllText(Path.Combine(Application.dataPath, "Scripts", "Games", "Freno", "BrakeGameController.cs"));

        [Test]
        public void TheExitSignal_KeepsItsPitchAndDuration_StartsRightAway_AndDecaysQuickly()
        {
            Assert.AreEqual(784.0, BrakeSounds.ExitHz, "784 Hz como hoy");
            Assert.AreEqual(0.06, BrakeSounds.ExitSeconds, 1e-9, "60 ms como hoy");
            WoodSounds.Render(WoodSounds.MakeCue(BrakeSounds.ExitHz, BrakeSounds.ExitSeconds), out var l, out var r);
            Assert.Greater(SoundMeter.PitchClarity(l, r, 784.0, 2, 80), 0.5, "se oye en 784 Hz (claridad en los primeros 80 ms)");
            Assert.Less(SoundMeter.PitchClarity(l, r, 784.0 * 1.06, 2, 80), 0.5, "y no un semitono arriba (la altura se conserva al 1 %)");
            Assert.LessOrEqual(SoundMeter.OnsetMs(l, r), 2.0, "empieza de inmediato (el tono de hoy tardaba 15 ms: la señal llega antes, no después)");
            Assert.LessOrEqual(SoundMeter.DurationMs(l, r, 30.0), 150.0, "cae a −30 dB antes de 0,15 s (es una señal corta)");
            Assert.AreEqual(-6f, BrakeSounds.ExitGainDb, "el mismo nivel de hoy");
        }

        [Test]
        public void TheExitSignalIsAskedForFirst_AndNeverWaitsToBeBaked()
        {
            GameFeel.Warm("freno");
            BrakeSounds.Request();
            WoodCore.Drain();
            Assert.IsNotNull(WoodCore.GetExtra(WoodCore.CueKey(BrakeSounds.ExitHz, BrakeSounds.ExitSeconds)), "la señal de salida está pedida");
            Assert.IsNotNull(WoodCore.Get(WoodSfx.Knock), "«¡Más rápido!»");
            WoodCore.ResetCounters();
            BrakeSounds.PlayExit();
            Assert.AreEqual(0, WoodCore.LateBakes, "el estímulo no puede esperar a calcularse (un tirón de ms en el tiempo de reacción)");
            Assert.AreEqual(0, WoodCore.Skipped);
            // la señal es lo PRIMERO que se pide: va delante de todo lo demás de Freno
            WoodCore.Reset();
            GameFeel.Warm("freno");
            BrakeSounds.Request();
            string src = File.ReadAllText(Path.Combine(Application.dataPath, "Scripts", "Games", "Freno", "BrakeSounds.cs"));
            Assert.Less(src.IndexOf("WoodCore.RequestCue(ExitHz", System.StringComparison.Ordinal), src.IndexOf("WoodCore.Request(WoodSfx.Knock)", System.StringComparison.Ordinal), "la señal se pide antes que el Knock");
            CollectionAssert.IsEmpty(System.Linq.Enumerable.ToArray(BrakeSounds.All()), "Freno no tiene sonidos propios");
        }

        [Test]
        public void TheControllerSoundsThroughWoodCore_TheRoarAndTheHissAreGone_AndOnlyTheSirenKeepsPlayTone()
        {
            string text = Controller();
            Assert.AreEqual(2, Regex.Matches(text, @"BrakeSounds\.PlayExit\(\)").Count, "la salida, en el juego y en la práctica");
            StringAssert.Contains("WoodCore.Play(WoodSfx.Knock)", text, "«¡Más rápido!»");
            StringAssert.Contains("BrakeSounds.Request()", text);
            foreach (var gone in new[] { "PlayTone(196f", "PlayTone(147f", "PlayTone(262f", "PlayTone(784f" }) StringAssert.DoesNotContain(gone, text, "ya no suena " + gone);
            // la sirena: los tres tonos de siempre
            foreach (var siren in new[] { "PlayTone(988f, 0.09f, 0.08f)", "PlayTone(740f, 0.09f, 0.08f)", "PlayTone(988f, 0.09f, 0.07f)" }) StringAssert.Contains(siren, text, "la sirena se queda tal cual");
            StringAssert.Contains("SirenTail", text);
            // los avisos de «¡Listo!» y los de nivel siguen siendo de la paleta
            StringAssert.Contains("GameFeel.Notice()", text);
            StringAssert.Contains("GameFeel.LevelUp()", text);
        }
    }
}
