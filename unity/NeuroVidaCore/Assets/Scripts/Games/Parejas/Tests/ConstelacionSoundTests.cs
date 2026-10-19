using System.IO;
using NeuroVida.Games.Shared;
using NUnit.Framework;
using UnityEngine;

namespace NeuroVida.Games.Parejas.Tests
{
    /// <summary>El sonido de Constelaciones en «Madera cálida» (Fase C, Tarea 135): las 11 notas de los grupos son kalimba afinada que empieza enseguida, el juego pide por adelantado todo lo que suena, el destello (Spark) ya no suena y la pista sigue sonando como hoy.</summary>
    public class ConstelacionSoundTests
    {
        private bool _soundWas;

        [SetUp]
        public void SetUp() { _soundWas = GameFeel.SoundOn; GameFeel.SoundOn = true; WoodCore.Reset(); }

        [TearDown]
        public void TearDown() { WoodCore.Reset(); GameFeel.SoundOn = _soundWas; }

        private static string Controller() => File.ReadAllText(Path.Combine(Application.dataPath, "Scripts", "Games", "Parejas", "ConstelacionGameController.cs"));

        [Test]
        public void TheElevenNotesOfTheGroups_AreKalimba_InTune_AndStartAtOnce()
        {
            for (int g = 0; g < 11; g++)
            {
                double hz = ConstelacionContract.NoteHz(g);
                WoodSounds.Render(WoodSounds.MakeNote(hz, WoodVoice.Kalimba), out var l, out var r);
                Assert.Greater(SoundMeter.PitchClarity(l, r, hz), 0.6, "la nota del grupo " + g + " (" + hz + " Hz) se oye afinada");
                Assert.Less(SoundMeter.OnsetMs(l, r), 3.0, "la nota del grupo " + g + " empieza en menos de 3 ms");
            }
            Assert.AreNotEqual(ConstelacionContract.NoteHz(0), ConstelacionContract.NoteHz(1), "cada grupo tiene su nota");
        }

        [Test]
        public void TheGameAsksForEverythingItSounds_BeforeTheCountdown()
        {
            GameFeel.Warm("parejas");
            ConstelacionSounds.Request();
            int guard = 0; while (!WoodCore.Idle) { WoodCore.Step(1f); Assert.Less(++guard, 200000); }
            for (int g = 0; g < 11; g++) Assert.IsNotNull(WoodCore.GetExtra(WoodCore.NoteKey(ConstelacionContract.NoteHz(g), WoodVoice.Kalimba)), "la nota del grupo " + g);
            foreach (var sfx in new[] { WoodSfx.Hum, WoodSfx.Knock, WoodSfx.Perfect, WoodSfx.New, WoodSfx.Unselect }) Assert.IsNotNull(WoodCore.Get(sfx), sfx.ToString());
            foreach (var (sfx, arg) in new[] { (WoodSfx.Tap, 0), (WoodSfx.Correct, 1), (WoodSfx.Correct, 7) }) Assert.IsNotNull(WoodCore.Get(sfx, arg), "va con el núcleo: " + sfx + arg);
            Assert.IsNotNull(WoodCore.GetExtra(ConstelacionSounds.HintKey), "la pista");
            Assert.AreEqual(0, WoodCore.LateBakes);
        }

        [Test]
        public void TheStreakSpark_NoLongerSounds_RicardoRemovedItByEar()
        {
            string text = Controller();
            Assert.IsFalse(System.Text.RegularExpressions.Regex.IsMatch(text, @"WoodSfx\.Spark|ConstelacionSounds\.Spark"), "el destello se quitó: el par de Correct ya sube con la racha");
            StringAssert.DoesNotContain("PlayClip", text, "el juego suena todo por WoodCore");
            GameFeel.Warm("parejas"); ConstelacionSounds.Request();
            int guard = 0; while (!WoodCore.Idle) { WoodCore.Step(1f); Assert.Less(++guard, 200000); }
            Assert.IsNull(WoodCore.Get(WoodSfx.Spark, 1), "ni siquiera se calcula");
        }

        [Test]
        public void TheHint_StillSoundsLikeToday_VerySoftAndDry()
        {
            var recipe = ConstelacionSounds.HintRecipe();
            WoodSounds.Render(recipe, out var l, out var r);
            double peak = 0.0;
            foreach (var v in l) peak = System.Math.Max(peak, System.Math.Abs(v));
            double rawPeak = 0.0;
            foreach (var v in recipe.X) rawPeak = System.Math.Max(rawPeak, System.Math.Abs(v));
            double out_ = peak * WoodKit.MasterGain * WoodKit.PlayVolume;                          // lo que sale por el parlante
            Assert.AreEqual(rawPeak * 0.25, out_, 0.003, "hoy: las campanitas (pico " + rawPeak.ToString("0.00") + ") se reproducían a 0,25 de volumen");
            Assert.That(out_, Is.InRange(0.04, 0.09), "muy suave, como hoy (≈ −23 dBFS)");
            Assert.Greater(l.Length / (double)SoundKit.Rate, 0.7, "las dos campanitas se oyen completas");
            Assert.AreEqual(new[] { ConstelacionSounds.HintKey }, System.Linq.Enumerable.ToArray(System.Linq.Enumerable.Select(ConstelacionSounds.All(), a => a.Key)));
            StringAssert.Contains("ConstelacionSounds.PlayHint()", Controller(), "la pista llama a la receta «como hoy», no al Hint de la paleta");
            StringAssert.DoesNotContain("WoodSfx.Hint", Controller());
        }
    }
}
