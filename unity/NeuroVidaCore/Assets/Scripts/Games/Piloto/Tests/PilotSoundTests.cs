using System.IO;
using NeuroVida.Games.Shared;
using NUnit.Framework;
using UnityEngine;

namespace NeuroVida.Games.Piloto.Tests
{
    /// <summary>
    /// El sonido de Piloto Estelar en «Madera cálida» (Fase D, Tarea 138): las 4 notas de las balizas son marimba con la altura de hoy; el blip de las señales es el MISMO para todas; el aviso de misión nueva son dos marimbas separadas EXACTAMENTE 0,4 s (un latido de la tarjeta) en un solo clip;
    /// el juego pide por adelantado todo lo que suena y el motor «Cohete» (bucle aprobado) no se toca.
    /// </summary>
    public class PilotSoundTests
    {
        private bool _soundWas;

        [SetUp]
        public void SetUp() { _soundWas = GameFeel.SoundOn; GameFeel.SoundOn = true; WoodCore.Reset(); }

        [TearDown]
        public void TearDown() { WoodCore.Reset(); GameFeel.SoundOn = _soundWas; }

        private static string Folder() => Path.Combine(Application.dataPath, "Scripts", "Games", "Piloto");

        private static string Controllers() => File.ReadAllText(Path.Combine(Folder(), "PilotGameController.cs")) + File.ReadAllText(Path.Combine(Folder(), "PilotGameController.Scene.cs")) + File.ReadAllText(Path.Combine(Folder(), "PilotGameController.Guided.cs"));

        [Test]
        public void TheFourBeaconNotes_KeepTheirPitch_AreMarimba_InTune_AndStartAtOnce()
        {
            CollectionAssert.AreEqual(new[] { 587.33f, 659.25f, 783.99f, 880f }, new[] { PilotSounds.BeaconHz(0), PilotSounds.BeaconHz(1), PilotSounds.BeaconHz(2), PilotSounds.BeaconHz(3) }, "las alturas de hoy (Penta[1..4])");
            Assert.AreEqual(PilotSounds.BeaconHz(0), PilotSounds.BeaconHz(4), "la quinta baliza vuelve a la primera nota");
            for (int i = 0; i < 4; i++)
            {
                double hz = PilotSounds.BeaconHz(i);
                WoodSounds.Render(WoodSounds.MakeNote(hz, WoodVoice.Marimba), out var l, out var r);
                Assert.Greater(SoundMeter.PitchClarity(l, r, hz), 0.6, "la nota de la baliza " + i + " (" + hz + " Hz) se oye afinada");
                Assert.Less(SoundMeter.PitchClarity(l, r, hz * System.Math.Pow(2.0, 1.0 / 12.0)), 0.3, "y no confunde con el semitono de arriba");
                Assert.Less(SoundMeter.OnsetMs(l, r), 3.0, "la nota de la baliza " + i + " empieza en menos de 3 ms");
            }
        }

        [Test]
        public void TheNewMissionNotice_IsTwoMarimbasExactlyOneBeatApart_InOneClip()
        {
            Assert.AreEqual(0.4f, PilotContract.PulseBeatSeconds, 1e-6f, "un latido de la tarjeta dura 0,4 s");
            var recipe = PilotSounds.MakeRecipe(PilotSfx.Mission);
            WoodSounds.Render(recipe, out var l, out var r);
            var onsets = SoundMeter.OnsetsMs(l, r);
            Assert.IsTrue(onsets.Exists(o => o <= 3.0), "la primera nota entra al empezar (inicios detectados: " + string.Join(", ", onsets.ConvertAll(o => o.ToString("0"))) + " ms)");
            Assert.IsTrue(onsets.Exists(o => System.Math.Abs(o - 400.0) <= 3.0), "la segunda entra a los 400 ms ±3 (los dos latidos de la tarjeta): " + string.Join(", ", onsets.ConvertAll(o => o.ToString("0"))) + " ms");
            Assert.Greater(SoundMeter.PitchClarity(l, r, 659.25, 5, 300), 0.5, "mi6");
            Assert.Greater(SoundMeter.PitchClarity(l, r, 987.77, 405, 300), 0.5, "si6");
            Assert.AreEqual(new[] { "piloto.mission" }, System.Linq.Enumerable.ToArray(System.Linq.Enumerable.Select(PilotSounds.All(), a => a.Key)), "el único propio de Piloto");
            StringAssert.Contains("PilotContract.PulseBeatSeconds", File.ReadAllText(Path.Combine(Folder(), "PilotSounds.cs")), "el 0,4 s sale del contrato, no de un número suelto");
        }

        [Test]
        public void TheBlip_IsTheSameForEverySignal_ByConstruction()
        {
            GameFeel.Warm("piloto");
            PilotSounds.Request();
            int guard = 0; while (!WoodCore.Idle) { WoodCore.Step(1f); Assert.Less(++guard, 200000); }
            string key = WoodCore.CueKey(PilotSounds.BlipHz, PilotSounds.BlipSeconds);
            var clip = WoodCore.GetExtra(key);
            Assert.IsNotNull(clip, "el blip está pedido");
            PilotSounds.PlayBlip(); PilotSounds.PlayBlip();
            Assert.AreSame(clip, WoodCore.GetExtra(key), "un solo clip para todas las señales: no delata cuál es de la misión");
            Assert.AreEqual(0, WoodCore.LateBakes);
            Assert.AreEqual(880.0, PilotSounds.BlipHz, "880 Hz como hoy");
            string text = Controllers();
            Assert.AreEqual(1, System.Text.RegularExpressions.Regex.Matches(text, @"PilotSounds\.PlayBlip\(\)").Count, "el blip suena al aparecer cada señal, sin argumentos");
            StringAssert.Contains("gainDb: -6f", File.ReadAllText(Path.Combine(Folder(), "PilotSounds.cs")), "−6 dB: es una señal, no un acierto");
            CollectionAssert.Contains(WoodCore.AllowedGainsDb, -6f);
        }

        [Test]
        public void TheGameAsksForEverythingItSounds_BeforeTheCountdown()
        {
            GameFeel.Warm("piloto");
            PilotSounds.Request();
            int guard = 0; while (!WoodCore.Idle) { WoodCore.Step(1f); Assert.Less(++guard, 200000); }
            for (int i = 0; i < 4; i++) Assert.IsNotNull(WoodCore.GetExtra(WoodCore.NoteKey(PilotSounds.BeaconHz(i), WoodVoice.Marimba)), "la nota de la baliza " + i);
            Assert.IsNotNull(WoodCore.GetExtra(PilotSounds.Key(PilotSfx.Mission)), "el aviso de misión");
            foreach (var sfx in new[] { WoodSfx.Knock, WoodSfx.SweepUp, WoodSfx.Streak, WoodSfx.Wrong }) Assert.IsNotNull(WoodCore.Get(sfx), sfx.ToString());
            for (int n = 1; n <= 7; n++) Assert.IsNotNull(WoodCore.Get(WoodSfx.Correct, n), "Correct " + n + " va con el núcleo (la racha de atrapar)");
            Assert.AreEqual(0, WoodCore.LateBakes);
        }

        [Test]
        public void TheControllerSoundsThroughWoodCore_AndTheEngineStaysAsApproved()
        {
            string text = Controllers();
            StringAssert.DoesNotContain("PlayClip", text, "el juego suena todo por WoodCore");
            foreach (var call in new[] { "PilotSounds.PlayBeacon(", "WoodCore.Play(WoodSfx.Knock)", "WoodCore.Play(WoodSfx.Correct, _run.Streak)", "WoodCore.Play(WoodSfx.Wrong)", "WoodCore.Play(WoodSfx.Streak)", "WoodCore.Play(WoodSfx.SweepUp)", "PilotSounds.PlayMission()", "PilotSounds.Request()" })
                StringAssert.Contains(call, text, call);
            // el motor «Cohete» no se toca: sigue siendo de PilotSounds (EngineLoop / MakeEngineLoop) y PilotEngine lo usa
            StringAssert.Contains("PilotSounds.EngineLoop(", File.ReadAllText(Path.Combine(Folder(), "PilotEngine.cs")));
            Assert.IsNotNull(typeof(PilotSounds).GetMethod("MakeEngineLoop"));
            Assert.IsNull(typeof(PilotSounds).GetMethod("Beacon"), "las campanas de antes ya no existen");
        }
    }
}
