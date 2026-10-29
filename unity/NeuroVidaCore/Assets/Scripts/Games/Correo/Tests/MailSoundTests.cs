using System.IO;
using System.Text.RegularExpressions;
using NeuroVida.Games.Shared;
using NUnit.Framework;
using UnityEngine;

namespace NeuroVida.Games.Correo.Tests
{
    /// <summary>El sonido de La estación de correo en «Madera cálida» (Fase C, Tarea 135): las 4 notas de los buzones son kalimba afinada que empieza enseguida, el juego pide por adelantado todo lo que suena (notas, los tres sonidos propios y la paleta) y la bocina del faro dura más que un golpe.</summary>
    public class MailSoundTests
    {
        private bool _soundWas;

        [SetUp]
        public void SetUp() { _soundWas = GameFeel.SoundOn; GameFeel.SoundOn = true; WoodCore.Reset(); }

        [TearDown]
        public void TearDown() { WoodCore.Reset(); GameFeel.SoundOn = _soundWas; }

        private static string Folder() => Path.Combine(Application.dataPath, "Scripts", "Games", "Correo");

        [Test]
        public void TheFourNotesOfTheMailboxes_AreKalimba_InTune_AndStartAtOnce()
        {
            Assert.AreEqual(4, MailContract.PlanetNotes.Length);
            for (int b = 0; b < 4; b++)
            {
                double hz = MailContract.PlanetNotes[b];
                WoodSounds.Render(WoodSounds.MakeNote(hz, WoodVoice.Kalimba), out var l, out var r);
                Assert.Greater(SoundMeter.PitchClarity(l, r, hz), 0.6, "la nota del buzón " + b + " (" + hz + " Hz) se oye afinada");
                Assert.Less(SoundMeter.OnsetMs(l, r), 3.0, "la nota del buzón " + b + " empieza en menos de 3 ms");
                if (b > 0) Assert.Greater(MailContract.PlanetNotes[b], MailContract.PlanetNotes[b - 1], "cada buzón suena distinto");
            }
        }

        [Test]
        public void TheGameAsksForEverythingItSounds_BeforeTheCountdown()
        {
            GameFeel.Warm("correo");
            MailSounds.Request();
            WoodCore.Drain();
            foreach (float hz in MailContract.PlanetNotes) Assert.IsNotNull(WoodCore.GetExtra(WoodCore.NoteKey(hz, WoodVoice.Kalimba)), "la nota de " + hz + " Hz");
            foreach (MailSfx sfx in System.Enum.GetValues(typeof(MailSfx))) Assert.IsNotNull(WoodCore.GetExtra(MailSounds.Key(sfx)), "propio: " + sfx);
            for (int n = 1; n <= 8; n++) Assert.IsNotNull(WoodCore.Get(WoodSfx.Spark, n), "brillo " + n);
            foreach (var sfx in new[] { WoodSfx.Hum, WoodSfx.Knock, WoodSfx.Streak, WoodSfx.Perfect, WoodSfx.New }) Assert.IsNotNull(WoodCore.Get(sfx), sfx.ToString());
            foreach (var (sfx, arg) in new[] { (WoodSfx.Wrong, 0), (WoodSfx.Tick, 0), (WoodSfx.Notice, 0) }) Assert.IsNotNull(WoodCore.Get(sfx, arg), "va con el núcleo: " + sfx);
            Assert.IsNotNull(WoodCore.Get(WoodSfx.LevelUp), "el cierre del día");
            Assert.AreEqual(0, WoodCore.LateBakes);
        }

        [Test]
        public void EveryOwnSoundTheControllerPlays_IsOneTheGameAsksFor()
        {
            string text = File.ReadAllText(Path.Combine(Folder(), "MailGameController.cs")) + File.ReadAllText(Path.Combine(Folder(), "MailGameController.Scene.cs"));
            var used = new System.Collections.Generic.HashSet<string>();
            foreach (Match m in Regex.Matches(text, @"MailSounds\.Play\(MailSfx\.(\w+)")) used.Add(m.Groups[1].Value);
            CollectionAssert.AreEquivalent(new[] { "Horn", "Sack", "Radio" }, used, "el juego suena sus tres propios (la bocina del faro, el saco y la radio)");
            StringAssert.DoesNotContain("PlayClip", text, "el juego suena todo por WoodCore");
        }

        [Test]
        public void TheBeaconHorn_LastsMoreThanABlow_AndSoundsBehindTheFanfare()
        {
            var horn = MailSounds.MakeRecipe(MailSfx.Horn);
            Assert.GreaterOrEqual(horn.X.Length / (double)SoundKit.Rate, 1.9, "la bocina de tres notas dura más que un golpe");
            Assert.AreEqual(WoodTier.Textura, horn.Tier, "de fondo: la fanfarria (Perfect) manda y la bocina queda 8 LU más abajo");
        }
    }
}
