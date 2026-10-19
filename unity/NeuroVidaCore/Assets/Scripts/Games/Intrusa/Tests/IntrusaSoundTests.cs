using System.IO;
using System.Text.RegularExpressions;
using NeuroVida.Games.Shared;
using NUnit.Framework;
using UnityEngine;

namespace NeuroVida.Games.Intrusa.Tests
{
    /// <summary>
    /// El sonido de La estrella intrusa en «Madera cálida» (Fase D, Tarea 138): las 9 notas de la chispa son marimba con la altura de hoy; la estrella CON palabra suena la MISMA nota 6 dB más fuerte que la que no la tiene (la información, medida: dos pasos de ganancia);
    /// el juego pide por adelantado todo lo que suena y suena todo por WoodCore.
    /// </summary>
    public class IntrusaSoundTests
    {
        private bool _soundWas;

        [SetUp]
        public void SetUp() { _soundWas = GameFeel.SoundOn; GameFeel.SoundOn = true; WoodCore.Reset(); }

        [TearDown]
        public void TearDown() { WoodCore.Reset(); GameFeel.SoundOn = _soundWas; }

        private static string Controller() => File.ReadAllText(Path.Combine(Application.dataPath, "Scripts", "Games", "Intrusa", "IntrusaGameController.cs"));

        [Test]
        public void TheNineNotes_KeepTheirPitch_AreMarimba_InTune_AndStartAtOnce()
        {
            CollectionAssert.AreEqual(new[] { 523.25f, 587.33f, 659.25f, 783.99f, 880f, 1046.5f, 1174.66f, 1318.51f, 1567.98f }, new[] { IntrusaSounds.NoteHz(0), IntrusaSounds.NoteHz(1), IntrusaSounds.NoteHz(2), IntrusaSounds.NoteHz(3), IntrusaSounds.NoteHz(4), IntrusaSounds.NoteHz(5), IntrusaSounds.NoteHz(6), IntrusaSounds.NoteHz(7), IntrusaSounds.NoteHz(8) }, "las alturas de hoy: do5 a sol6");
            Assert.AreEqual(9, IntrusaSounds.NoteCount);
            for (int i = 0; i < IntrusaSounds.NoteCount; i++)
            {
                double hz = IntrusaSounds.NoteHz(i);
                WoodSounds.Render(WoodSounds.MakeNote(hz, WoodVoice.Marimba), out var l, out var r);
                Assert.Greater(SoundMeter.PitchClarity(l, r, hz), 0.6, "la nota " + i + " (" + hz + " Hz) se oye afinada");
                Assert.Less(SoundMeter.PitchClarity(l, r, hz * System.Math.Pow(2.0, 1.0 / 12.0)), 0.3, "y no confunde con el semitono de arriba");
                Assert.Less(SoundMeter.OnsetMs(l, r), 3.0, "la nota " + i + " empieza en menos de 3 ms");
            }
        }

        [Test]
        public void TheStarWithAWord_SoundsTheSameNote_SixDecibelsLouder()
        {
            Assert.AreEqual(-6f, IntrusaSounds.WithoutWordGainDb);
            CollectionAssert.Contains(WoodCore.AllowedGainsDb, IntrusaSounds.WithoutWordGainDb, "−6 dB es un paso permitido de la paleta (−3 y −3)");
            // dos pasos de ganancia sobre la misma receta: la diferencia es exactamente la de dos pasos de 3 dB (6,02 dB)
            double diff = 20.0 * System.Math.Log10(1.0 / System.Math.Pow(10.0, IntrusaSounds.WithoutWordGainDb / 20.0));
            Assert.AreEqual(6.0, diff, 0.05, "la estrella con palabra suena 6 dB más fuerte");
            string sounds = File.ReadAllText(Path.Combine(Application.dataPath, "Scripts", "Games", "Intrusa", "IntrusaSounds.cs"));
            StringAssert.Contains("WoodCore.PlayNote(NoteHz(step), WoodVoice.Marimba, gainDb: strong ? 0f : WithoutWordGainDb)", sounds, "una sola receta por nota; la información es la ganancia");
            StringAssert.Contains("IntrusaSounds.PlayNote(step, word && !quiet)", Controller(), "la estrella con palabra (y no en la variante tranquila) suena fuerte");
        }

        [Test]
        public void TheGameAsksForEverythingItSounds_BeforeTheCountdown()
        {
            GameFeel.Warm("intrusa");
            IntrusaSounds.Request();
            int guard = 0; while (!WoodCore.Idle) { WoodCore.Step(1f); Assert.Less(++guard, 200000); }
            for (int i = 0; i < IntrusaSounds.NoteCount; i++) Assert.IsNotNull(WoodCore.GetExtra(WoodCore.NoteKey(IntrusaSounds.NoteHz(i), WoodVoice.Marimba)), "la nota " + i);
            foreach (IntrusaSfx sfx in System.Enum.GetValues(typeof(IntrusaSfx))) Assert.IsNotNull(WoodCore.GetExtra(IntrusaSounds.Key(sfx)), "propio: " + sfx);
            foreach (var sfx in new[] { WoodSfx.Perfect, WoodSfx.Hum, WoodSfx.Knock, WoodSfx.Wrong }) Assert.IsNotNull(WoodCore.Get(sfx), sfx.ToString());
            Assert.IsNotNull(WoodCore.Get(WoodSfx.Correct, 3), "«¿Qué las une?» acertada va con el núcleo");
            Assert.AreEqual(0, WoodCore.LateBakes);
        }

        [Test]
        public void TheControllerSoundsEverythingThroughWoodCore()
        {
            string text = Controller();
            StringAssert.DoesNotContain("PlayClip", text, "el juego suena todo por WoodCore");
            foreach (var call in new[] { "WoodCore.Play(WoodSfx.Hum, gainDb: -3f)", "WoodCore.Play(WoodSfx.Knock)", "WoodCore.Play(WoodSfx.Correct, 3)", "WoodCore.Play(WoodSfx.Wrong)", "WoodCore.Play(WoodSfx.Perfect, gainDb: quiet ? -6f : 0f)", "IntrusaSounds.Play(IntrusaSfx.Fall)", "IntrusaSounds.Play(IntrusaSfx.Engrave)", "IntrusaSounds.Request()" })
                StringAssert.Contains(call, text, call);
            Assert.AreEqual(2, Regex.Matches(text, @"WoodCore\.Play\(WoodSfx\.Perfect, gainDb: quiet \? -6f : 0f\)").Count, "el cierre de la figura, en sus dos caminos (con y sin movimiento)");
            StringAssert.Contains("_seguirTouch.PlaysTap = true", text, "«Seguir» sigue sonando el toque del kit");
            StringAssert.Contains("GameFeel.LevelUp()", text, "y el aviso de nivel");
        }

        [Test]
        public void TheEngraving_LastsAsLongAsTheDrawing_AndTheFallingStarIsHeard()
        {
            WoodSounds.Render(IntrusaSounds.MakeRecipe(IntrusaSfx.Engrave), out var l, out var r);
            double ms = SoundMeter.DurationMs(l, r);
            Assert.That(ms, Is.InRange(900.0, 2600.0), "el grabado acompaña el dibujo (el barrido dura 1,1 s, como hoy 1,2 s; el resto es la cola de la marimba grave y de la sala): " + ms.ToString("0") + " ms");
            WoodSounds.Render(IntrusaSounds.MakeRecipe(IntrusaSfx.Fall), out var fl, out var fr);
            double fall = SoundMeter.DurationMs(fl, fr);
            Assert.That(fall, Is.InRange(600.0, 1500.0), "la estrella fugaz dura lo que hoy (0,95 s): " + fall.ToString("0") + " ms");
            Assert.Greater(SoundMeter.Peak(fl, fr), 0.1, "la estrella fugaz se oye (el SweepDown de la paleta casi no se oía)");
        }
    }
}
