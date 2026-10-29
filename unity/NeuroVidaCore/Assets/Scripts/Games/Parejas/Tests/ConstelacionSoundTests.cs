using System.Collections.Generic;
using System.IO;
using NeuroVida.Games.Shared;
using NUnit.Framework;
using UnityEngine;

namespace NeuroVida.Games.Parejas.Tests
{
    /// <summary>El sonido de Constelaciones en «Madera cálida» (Fase C, Tareas 135 y 137): las 11 notas de los grupos son kalimba afinada que empieza enseguida, el juego pide por adelantado todo lo que suena, el destello (Spark) ya no suena y la pista (opción (a) que eligió Ricardo) es la nota del grupo de cada luz hermana, en kalimba a −6 dB, una por luz.</summary>
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
            WoodCore.Drain();
            for (int g = 0; g < 11; g++) Assert.IsNotNull(WoodCore.GetExtra(WoodCore.NoteKey(ConstelacionContract.NoteHz(g), WoodVoice.Kalimba)), "la nota del grupo " + g);
            foreach (var sfx in new[] { WoodSfx.Hum, WoodSfx.Knock, WoodSfx.Perfect, WoodSfx.New, WoodSfx.Unselect }) Assert.IsNotNull(WoodCore.Get(sfx), sfx.ToString());
            foreach (var (sfx, arg) in new[] { (WoodSfx.Tap, 0), (WoodSfx.Correct, 1), (WoodSfx.Correct, 7) }) Assert.IsNotNull(WoodCore.Get(sfx, arg), "va con el núcleo: " + sfx + arg);
            Assert.IsNull(WoodCore.GetExtra("parejas.hint"), "la receta temporal de las campanitas se fue: la pista es la nota del grupo, ya pedida");
            Assert.AreEqual(0, WoodCore.LateBakes);
        }

        [Test]
        public void TheStreakSpark_NoLongerSounds_RicardoRemovedItByEar()
        {
            string text = Controller();
            Assert.IsFalse(System.Text.RegularExpressions.Regex.IsMatch(text, @"WoodSfx\.Spark|ConstelacionSounds\.Spark"), "el destello se quitó: el par de Correct ya sube con la racha");
            StringAssert.DoesNotContain("PlayClip", text, "el juego suena todo por WoodCore");
            GameFeel.Warm("parejas"); ConstelacionSounds.Request();
            WoodCore.Drain();
            Assert.IsNull(WoodCore.Get(WoodSfx.Spark, 1), "ni siquiera se calcula");
        }

        [Test]
        public void TheHint_IsTheNoteOfTheGroup_InKalimba_SoftBy6dB_AndBakesNothingNew()
        {
            Assert.AreEqual(-6f, ConstelacionSounds.HintGainDb, "−6 dB respecto de la nota normal: Ricardo no pidió bajarla más");
            CollectionAssert.Contains(WoodCore.AllowedGainsDb, ConstelacionSounds.HintGainDb);
            GameFeel.Warm("parejas");
            ConstelacionSounds.Request();
            WoodCore.Drain();
            WoodCore.ResetCounters();
            int baked = WoodCore.BakeCount;
            for (int g = 0; g < 11; g++) ConstelacionSounds.PlayHint(g);
            Assert.AreEqual(0, WoodCore.LateBakes, "la nota de cada grupo ya estaba pedida (RequestNote): no se calcula nada en pleno juego");
            Assert.AreEqual(0, WoodCore.Skipped, "ninguna pista se pierde");
            Assert.AreEqual(baked, WoodCore.BakeCount, "no se hornea nada nuevo");
            CollectionAssert.IsEmpty(System.Linq.Enumerable.ToArray(ConstelacionSounds.All()), "Constelaciones ya no tiene sonidos propios: se fue la receta temporal de las campanitas");
            string sounds = File.ReadAllText(Path.Combine(Application.dataPath, "Scripts", "Games", "Parejas", "ConstelacionSounds.cs"));
            StringAssert.Contains("WoodCore.PlayNote(ConstelacionContract.NoteHz(group), WoodVoice.Kalimba, gainDb: HintGainDb)", sounds, "la pista es la nota de SU grupo, en kalimba, a −6 dB");
            StringAssert.DoesNotContain("HintRecipe", sounds);
            StringAssert.DoesNotContain("HintBell", sounds);
            string controller = Controller();
            StringAssert.Contains("ConstelacionSounds.PlayHint(_hintNotes[0].Group)", controller, "el juego suena la nota del grupo de cada luz hermana");
            StringAssert.DoesNotContain("WoodSfx.Hint", controller, "el Hint de la paleta no le sonó a ayuda a Ricardo");
            StringAssert.DoesNotContain("PlayHint()", controller, "ya no es un sonido único: una nota por luz");
        }

        [Test]
        public void TheHint_HasOneNotePerSiblingLight_EachWithTheNoteOfItsGroup_160MsApart()
        {
            var pair = new List<ConLight> { new ConLight { Id = 4, Group = 2 } };
            var one = ConstelacionContract.HintCues(pair, 10f);
            Assert.AreEqual(1, one.Count, "con una pareja, una sola nota");
            Assert.AreEqual(4, one[0].LightId);
            Assert.AreEqual(2, one[0].Group, "la nota del grupo de la hermana (la misma que sonó cuando la vio)");
            Assert.AreEqual(10f, one[0].At, 1e-5f, "la primera suena en el momento en que brilla");

            var trio = new List<ConLight> { new ConLight { Id = 4, Group = 7 }, new ConLight { Id = 9, Group = 7 } };
            var two = ConstelacionContract.HintCues(trio, 10f);
            Assert.AreEqual(2, two.Count, "con un trío, dos hermanas y dos notas: una por luz");
            Assert.AreEqual(new[] { 4, 9 }, new[] { two[0].LightId, two[1].LightId }, "cada nota va con su luz");
            Assert.AreEqual(ConstelacionContract.NoteHz(7), ConstelacionContract.NoteHz(two[0].Group), 1e-9);
            Assert.AreEqual(ConstelacionContract.NoteHz(two[0].Group), ConstelacionContract.NoteHz(two[1].Group), 1e-9, "las dos hermanas son del mismo grupo: la misma nota");
            Assert.AreEqual(10f, two[0].At, 1e-5f);
            Assert.AreEqual(10f + 0.16f, two[1].At, 1e-5f, "la segunda brilla (y suena) 160 ms después, como en la página de la pista");
            Assert.AreEqual(160f, ConstelacionContract.HintStaggerMs);
            Assert.AreEqual(0, ConstelacionContract.HintCues(new List<ConLight>(), 3f).Count, "sin hermanas dormidas no suena nada");
            StringAssert.Contains("ConstelacionContract.HintCues(sleeping, now)", Controller(), "el controlador usa el mismo cálculo para el brillo y la nota");
            StringAssert.Contains("_lv[cue.LightId].HintAt = cue.At", Controller(), "la luz brilla en el momento en que suena su nota");
        }
    }
}
