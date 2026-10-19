using System.Collections.Generic;
using System.IO;
using NeuroVida.Games.Shared;
using NUnit.Framework;
using UnityEngine;

namespace NeuroVida.Games.Secuencia.Tests
{
    /// <summary>
    /// El sonido de Rastro de luz en «Madera cálida» (Fase D, Tarea 138). El camino se aprende con el oído: las 9 notas de los luceros son MARIMBA con la MISMA altura de siempre, empiezan enseguida y caen 20 dB en lo mismo que la campana de hoy (τ 0,2 s); los 4 avisos de modo
    /// conservan sus notas y sus instantes; el juego pide por adelantado todo lo que suena y la ronda lograda es el <c>Correct</c> que sube con el largo del camino.
    /// </summary>
    public class RastroSoundTests
    {
        private bool _soundWas;

        [SetUp]
        public void SetUp() { _soundWas = GameFeel.SoundOn; GameFeel.SoundOn = true; WoodCore.Reset(); }

        [TearDown]
        public void TearDown() { WoodCore.Reset(); GameFeel.SoundOn = _soundWas; }

        private static string Controller() => File.ReadAllText(Path.Combine(Application.dataPath, "Scripts", "Games", "Secuencia", "RastroGameController.cs"));

        [Test]
        public void TheNineOrbNotes_KeepTheirPitch_TheyAreTheOnesOfToday()
        {
            // La altura NO cambia (es información): las del tablero de hoy, 392 a 1175 Hz.
            CollectionAssert.AreEqual(new[] { 392f, 440f, 523.25f, 587.33f, 659.25f, 783.99f, 880f, 1046.5f, 1174.66f }, RastroBoard.Notes);
            for (int i = 0; i < RastroBoard.Notes.Length; i++) Assert.AreEqual(RastroBoard.Notes[i], RastroSounds.OrbHz(i), 1e-4, "la nota del lucero " + i);
        }

        [Test]
        public void TheNineOrbNotes_AreMarimba_InTune_StartAtOnce_AndDecayLikeTheBellOfToday()
        {
            var decays = new List<string>();
            for (int i = 0; i < RastroBoard.Notes.Length; i++)
            {
                double hz = RastroSounds.OrbHz(i);
                WoodSounds.Render(WoodSounds.MakeNote(hz, WoodVoice.Marimba, RastroSounds.OrbTau), out var l, out var r);
                decays.Add(hz.ToString("0") + " Hz: " + SoundMeter.DecayMs(l, r, 20.0).ToString("0") + " ms");
                Assert.Greater(SoundMeter.PitchClarity(l, r, hz), 0.6, "la nota del lucero " + i + " (" + hz + " Hz) se oye afinada");
                Assert.Less(SoundMeter.PitchClarity(l, r, hz * System.Math.Pow(2.0, 1.0 / 12.0)), 0.3, "y no confunde con el semitono de arriba");
                Assert.Less(SoundMeter.OnsetMs(l, r), 3.0, "la nota del lucero " + i + " empieza en menos de 3 ms");
            }
            // Cae 20 dB en lo que la campana de hoy (450 ms; entre 370 y 560 ms según la nota, por cómo se mezcla con la sala): con τ 0,3 serían ≈ 700 y las notas de un camino rápido se encimarían más.
            double sum = 0;
            foreach (var d in decays)
            {
                double ms = double.Parse(d.Substring(d.IndexOf(": ") + 2).Replace(" ms", ""));
                sum += ms;
                Assert.That(ms, Is.InRange(330.0, 600.0), d + " · " + string.Join(" | ", decays));
            }
            Assert.That(sum / decays.Count, Is.InRange(400.0, 520.0), "la caída media (hoy 450 ms) · " + string.Join(" | ", decays));
        }

        [Test]
        public void TheOrbNote_IsTheSameWhenTheSparkArrivesAndWhenThePlayerTouches()
        {
            string text = Controller();
            int plays = System.Text.RegularExpressions.Regex.Matches(text, @"RastroSounds\.PlayOrb\(orb\)").Count;
            Assert.AreEqual(2, plays, "la nota del lucero suena en los dos sitios (la chispa llega y el jugador toca) con un solo nivel");
            StringAssert.DoesNotContain("PlayClip", text, "el juego suena todo por WoodCore");
            StringAssert.DoesNotContain("RastroSounds.Orb(", text);
        }

        [Test]
        public void TheFourModeCues_KeepTheirNotesAndTheirInstants()
        {
            // Las alturas y los instantes de hoy (la instrucción del modo: cuántas notas y hacia dónde van).
            CollectionAssert.AreEqual(new[] { 659.25 }, RastroSounds.CueHz[0]);
            CollectionAssert.AreEqual(new[] { 880.0, 659.25 }, RastroSounds.CueHz[1]);
            CollectionAssert.AreEqual(new[] { 659.25, 880.0, 659.25 }, RastroSounds.CueHz[2]);
            CollectionAssert.AreEqual(new[] { 523.25, 659.25, 783.99 }, RastroSounds.CueHz[3]);
            foreach (RastroSfx sfx in System.Enum.GetValues(typeof(RastroSfx)))
            {
                int k = (int)sfx;
                WoodSounds.Render(RastroSounds.MakeRecipe(sfx), out var l, out var r);
                var onsets = SoundMeter.OnsetsMs(l, r);
                // Cada nota entra en el instante de hoy (±3 ms). (El detector puede marcar de más el batido entre dos notas o el corte final de la marimba, a −28 dB: lo que importa es que cada nota esté donde tiene que estar.)
                for (int i = 0; i < RastroSounds.CueAt[k].Length; i++)
                    Assert.IsTrue(onsets.Exists(o => System.Math.Abs(o - RastroSounds.CueAt[k][i] * 1000.0) <= 3.0), sfx + ": la nota " + i + " tiene que entrar a los " + (RastroSounds.CueAt[k][i] * 1000.0).ToString("0") + " ms (inicios detectados: " + string.Join(", ", onsets.ConvertAll(o => o.ToString("0"))) + ")");
                for (int i = 0; i < RastroSounds.CueHz[k].Length; i++) Assert.Greater(SoundMeter.PitchClarity(l, r, RastroSounds.CueHz[k][i], (int)(RastroSounds.CueAt[k][i] * 1000.0) + 5, 150), 0.5, sfx + ": la nota " + i + " se oye afinada");
            }
            Assert.AreEqual(RastroSfx.Cue1, RastroSounds.CueOf(RastroMode.Reves));
            Assert.AreEqual(RastroSfx.Cue2, RastroSounds.CueOf(RastroMode.Gira));
            Assert.AreEqual(RastroSfx.Cue3, RastroSounds.CueOf(RastroMode.Marcha));
            Assert.AreEqual(RastroSfx.Cue0, RastroSounds.CueOf(RastroMode.Rastro));
        }

        [Test]
        public void TheGameAsksForEverythingItSounds_BeforeTheCountdown()
        {
            GameFeel.Warm("secuencia");
            RastroSounds.Request();
            int guard = 0; while (!WoodCore.Idle) { WoodCore.Step(1f); Assert.Less(++guard, 200000); }
            for (int i = 0; i < RastroBoard.Notes.Length; i++) Assert.IsNotNull(WoodCore.GetExtra(WoodCore.NoteKey(RastroSounds.OrbHz(i), WoodVoice.Marimba, RastroSounds.OrbTau)), "la nota del lucero " + i);
            foreach (RastroSfx sfx in System.Enum.GetValues(typeof(RastroSfx))) Assert.IsNotNull(WoodCore.GetExtra(RastroSounds.Key(sfx)), "aviso: " + sfx);
            foreach (var sfx in new[] { WoodSfx.SweepUp, WoodSfx.New, WoodSfx.Wrong }) Assert.IsNotNull(WoodCore.Get(sfx), sfx.ToString());
            for (int n = 1; n <= 7; n++) Assert.IsNotNull(WoodCore.Get(WoodSfx.Correct, n), "Correct " + n + " va con el núcleo");
            Assert.AreEqual(0, WoodCore.LateBakes);
            foreach (var key in new[] { "rastro.cue0", "rastro.cue1", "rastro.cue2", "rastro.cue3" }) CollectionAssert.Contains(new List<string>(System.Linq.Enumerable.Select(RastroSounds.All(), a => a.Key)), key);
        }

        [Test]
        public void TheRoundThatSucceeds_IsTheCorrectThatRisesWithThePathLength()
        {
            // Decisión de Ricardo (19-oct): la ronda lograda es una respuesta (el par sube con el largo del camino), no un hito en cada ronda.
            Assert.AreEqual(1, RastroSounds.RoundStep(2), "la ronda guiada del tutorial (2 luceros)");
            Assert.AreEqual(1, RastroSounds.RoundStep(3));
            Assert.AreEqual(2, RastroSounds.RoundStep(4));
            Assert.AreEqual(7, RastroSounds.RoundStep(9));
            Assert.AreEqual(7, RastroSounds.RoundStep(12), "el tope de la paleta");
            string text = Controller();
            Assert.AreEqual(2, System.Text.RegularExpressions.Regex.Matches(text, @"WoodCore\.Play\(WoodSfx\.Correct, RastroSounds\.RoundStep\(r\.Asked\)\)").Count, "la ronda lograda y la del tutorial");
            StringAssert.DoesNotContain("WoodSfx.Perfect", text, "ninguna ronda es un hito");
            StringAssert.Contains("RastroSounds.PlayCue(", text);
        }

        [Test]
        public void TheSoundsOfTheGame_AreOnlyTheFourCues()
        {
            CollectionAssert.AreEqual(new[] { "rastro.cue0", "rastro.cue1", "rastro.cue2", "rastro.cue3" }, System.Linq.Enumerable.ToArray(System.Linq.Enumerable.Select(RastroSounds.All(), a => a.Key)));
        }
    }
}
