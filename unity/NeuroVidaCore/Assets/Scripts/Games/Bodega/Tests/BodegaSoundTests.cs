using System.IO;
using System.Text.RegularExpressions;
using NeuroVida.Games.Shared;
using NUnit.Framework;
using UnityEngine;

namespace NeuroVida.Games.Bodega.Tests
{
    /// <summary>El sonido de Bodega de carga en «Madera cálida» (Fase C, Tarea 135): las 12 notas de las escotillas son kalimba afinada que empieza enseguida, el juego pide por adelantado todo lo que suena (notas, los cinco sonidos propios y la paleta) y la matraca dura lo que el giro.</summary>
    public class BodegaSoundTests
    {
        private bool _soundWas;

        [SetUp]
        public void SetUp() { _soundWas = GameFeel.SoundOn; GameFeel.SoundOn = true; WoodCore.Reset(); }

        [TearDown]
        public void TearDown() { WoodCore.Reset(); GameFeel.SoundOn = _soundWas; }

        private static string Folder() => Path.Combine(Application.dataPath, "Scripts", "Games", "Bodega");

        [Test]
        public void TheTwelveNotesOfTheHatches_AreKalimba_InTune_AndStartAtOnce()
        {
            for (int h = 0; h < 12; h++)
            {
                double hz = BodegaSounds.NoteHz(h);
                WoodSounds.Render(WoodSounds.MakeNote(hz, WoodVoice.Kalimba), out var l, out var r);
                Assert.Greater(SoundMeter.PitchClarity(l, r, hz), 0.6, "la nota de la escotilla " + h + " (" + hz + " Hz) se oye afinada");
                Assert.Less(SoundMeter.OnsetMs(l, r), 3.0, "la nota de la escotilla " + h + " empieza en menos de 3 ms");
            }
        }

        [Test]
        public void TheGameAsksForEverythingItSounds_BeforeTheCountdown()
        {
            GameFeel.Warm("bodega");
            BodegaSounds.Request();
            int guard = 0; while (!WoodCore.Idle) { WoodCore.Step(1f); Assert.Less(++guard, 200000); }
            for (int h = 0; h < 12; h++) Assert.IsNotNull(WoodCore.GetExtra(WoodCore.NoteKey(BodegaSounds.NoteHz(h), WoodVoice.Kalimba)), "la nota de la escotilla " + h);
            foreach (BodegaSfx sfx in System.Enum.GetValues(typeof(BodegaSfx))) Assert.IsNotNull(WoodCore.GetExtra(BodegaSounds.Key(sfx)), "propio: " + sfx);
            for (int n = 1; n <= 8; n++) Assert.IsNotNull(WoodCore.Get(WoodSfx.Spark, n), "destello " + n);
            foreach (var sfx in new[] { WoodSfx.Hum, WoodSfx.Clack, WoodSfx.Perfect, WoodSfx.New }) Assert.IsNotNull(WoodCore.Get(sfx), sfx.ToString());
            Assert.AreEqual(0, WoodCore.LateBakes);
        }

        [Test]
        public void EveryOwnSoundTheControllerPlays_IsOneTheGameAsksFor()
        {
            string text = File.ReadAllText(Path.Combine(Folder(), "BodegaGameController.cs")) + File.ReadAllText(Path.Combine(Folder(), "BodegaGameController.Scene.cs"));
            var used = new System.Collections.Generic.HashSet<string>();
            foreach (Match m in Regex.Matches(text, @"BodegaSounds\.Play\(BodegaSfx\.(\w+)")) used.Add(m.Groups[1].Value);
            CollectionAssert.AreEquivalent(new[] { "Arrive", "Beam", "Open", "Fly", "Ratchet" }, used, "el juego suena sus cinco propios (llegar, haz, abrir, volar y la matraca)");
            StringAssert.DoesNotContain("PlayClip", text, "el juego suena todo por WoodCore");
        }

        [Test]
        public void TheRatchet_LastsAsLongAsTheSpin_PlusItsTail()
        {
            var recipe = BodegaSounds.MakeRecipe(BodegaSfx.Ratchet);
            Assert.AreEqual(BodegaMotion.SpinSeconds + 0.3, recipe.X.Length / (double)SoundKit.Rate, 0.02, "dura lo que el giro (la matraca sigue 1,5 s aunque el giro sea más lento en mayores: como hoy)");
        }
    }
}
