using System.IO;
using System.Text.RegularExpressions;
using NeuroVida.Games.Shared;
using NUnit.Framework;
using UnityEngine;

namespace NeuroVida.Games.Satelites.Tests
{
    /// <summary>
    /// El sonido de Satélites en «Madera cálida» (Fase D, Tarea 138): las 5 campanas de la presentación son marimba con la altura y la caída de hoy y suenan cada 0,12 s (el controlador y el tutorial usan la misma constante); marcar es el <c>Select</c> de Rescate con la altura de hoy;
    /// la escalera de las luces conserva su ciclo de cinco notas; el juego pide por adelantado todo lo que suena; y el zumbido en bucle se queda (un segundo exacto, sin pasar de 0,5).
    /// </summary>
    public class SatelliteSoundTests
    {
        private bool _soundWas;

        [SetUp]
        public void SetUp() { _soundWas = GameFeel.SoundOn; GameFeel.SoundOn = true; WoodCore.Reset(); }

        [TearDown]
        public void TearDown() { WoodCore.Reset(); GameFeel.SoundOn = _soundWas; }

        private static string Folder() => Path.Combine(Application.dataPath, "Scripts", "Games", "Satelites");

        private static string Controllers() => File.ReadAllText(Path.Combine(Folder(), "SatelliteGameController.cs")) + File.ReadAllText(Path.Combine(Folder(), "SatelliteGameController.Guided.cs")) + File.ReadAllText(Path.Combine(Folder(), "SatelliteGameController.Scene.cs"));

        [Test]
        public void TheFiveCueBells_KeepTheirPitch_AreMarimba_InTune_AndStartAtOnce()
        {
            CollectionAssert.AreEqual(new[] { 659.25f, 783.99f, 880f, 1046.5f, 1174.66f }, new[] { SatelliteSounds.CueHz(0), SatelliteSounds.CueHz(1), SatelliteSounds.CueHz(2), SatelliteSounds.CueHz(3), SatelliteSounds.CueHz(4) }, "las alturas de hoy (Penta[2..6])");
            for (int i = 0; i < 5; i++)
            {
                double hz = SatelliteSounds.CueHz(i);
                WoodSounds.Render(WoodSounds.MakeNote(hz, WoodVoice.Marimba), out var l, out var r);
                Assert.Greater(SoundMeter.PitchClarity(l, r, hz), 0.6, "la campana " + i + " (" + hz + " Hz) se oye afinada");
                Assert.Less(SoundMeter.PitchClarity(l, r, hz * System.Math.Pow(2.0, 1.0 / 12.0)), 0.3, "y no confunde con el semitono de arriba");
                Assert.Less(SoundMeter.OnsetMs(l, r), 3.0, "la campana " + i + " empieza en menos de 3 ms");
            }
        }

        [Test]
        public void TheBellsRingEvery120Ms_AndTheTutorialUsesTheSameInterval()
        {
            Assert.AreEqual(0.12f, SatelliteContract.CueBellGap, 1e-6f, "una campana cada 0,12 s: la cantidad se oye");
            string folder = Folder();
            StringAssert.Contains("rung * SatelliteContract.CueBellGap", File.ReadAllText(Path.Combine(folder, "SatelliteGameController.cs")), "el juego usa la constante");
            StringAssert.Contains("rung * SatelliteContract.CueBellGap", File.ReadAllText(Path.Combine(folder, "SatelliteGameController.Guided.cs")), "el tutorial usa la misma (antes sonaban todas de golpe)");
            StringAssert.DoesNotContain("rung * 0.12f", File.ReadAllText(Path.Combine(folder, "SatelliteGameController.cs")), "ya no hay un 0,12 suelto en el ritmo de las campanas");
            Assert.AreEqual(1.8f, SatelliteContract.CueSeconds, "la presentación sigue durando lo mismo");
        }

        [Test]
        public void EachMark_SoundsTheSelectOfRescue_AtTheSamePitchAsToday_AndItRisesWithEveryMark()
        {
            for (int n = 1; n <= 7; n++)
            {
                Assert.AreEqual(SatelliteSounds.Penta[n + 1], (float)WoodKit.P[SatelliteSounds.MarkStep(n)], 0.01f, "la marca " + n + " suena la altura de hoy (Penta[n + 1]; la primera es mi5)");
                if (n > 1) Assert.Greater(WoodKit.P[SatelliteSounds.MarkStep(n)], WoodKit.P[SatelliteSounds.MarkStep(n - 1)], "cada marca sube");
            }
            Assert.AreEqual(659.25f, SatelliteSounds.Penta[SatelliteSounds.MarkStep(1)], 1e-3f);
            Assert.AreEqual(SatelliteSounds.MarkStep(7), SatelliteSounds.MarkStep(12), "más de siete marcas suenan como la séptima");
            StringAssert.Contains("WoodCore.Play(WoodSfx.Select, MarkStep(n))", File.ReadAllText(Path.Combine(Folder(), "SatelliteSounds.cs")));
        }

        [Test]
        public void TheLightsKeepTheirFiveNoteLadder_ThatRisesAndStartsAgain()
        {
            var steps = new System.Collections.Generic.List<int>();
            for (int i = 0; i < 11; i++) steps.Add(SatelliteSounds.LightStep(i));
            CollectionAssert.AreEqual(new[] { 1, 2, 3, 4, 5, 1, 2, 3, 4, 5, 1 }, steps, "cinco notas que suben y vuelven a empezar (como hoy: Penta[4 + luz % 5])");
            for (int k = 2; k <= 5; k++) Assert.Greater(WoodKit.P[4 + k], WoodKit.P[4 + k - 1], "dentro de la escalera sube");
        }

        [Test]
        public void TheGameAsksForEverythingItSounds_BeforeTheCountdown()
        {
            GameFeel.Warm("satelites");
            SatelliteSounds.Request();
            int guard = 0; while (!WoodCore.Idle) { WoodCore.Step(1f); Assert.Less(++guard, 200000); }
            for (int i = 0; i < 5; i++) Assert.IsNotNull(WoodCore.GetExtra(WoodCore.NoteKey(SatelliteSounds.CueHz(i), WoodVoice.Marimba)), "la campana " + i);
            for (int n = 1; n <= 7; n++) Assert.IsNotNull(WoodCore.Get(WoodSfx.Select, SatelliteSounds.MarkStep(n)), "marca " + n);
            for (int k = 1; k <= 5; k++) Assert.IsNotNull(WoodCore.Get(WoodSfx.Spark, k), "luz " + k);
            foreach (var sfx in new[] { WoodSfx.Clack, WoodSfx.Unselect, WoodSfx.Knock, WoodSfx.Perfect, WoodSfx.Wrong, WoodSfx.New }) Assert.IsNotNull(WoodCore.Get(sfx), sfx.ToString());
            Assert.AreEqual(0, WoodCore.LateBakes);
            CollectionAssert.IsEmpty(System.Linq.Enumerable.ToArray(SatelliteSounds.All()), "Satélites no tiene sonidos propios: todo es de la paleta");
        }

        [Test]
        public void TheControllerSoundsEverythingThroughWoodCore_ExceptTheApprovedHumLoop()
        {
            string text = Controllers();
            StringAssert.DoesNotContain("PlayClip", text, "el juego suena todo por WoodCore");
            foreach (var sfx in new[] { "Clack", "Perfect", "Wrong", "New", "Unselect", "Knock" }) StringAssert.Contains("WoodCore.Play(WoodSfx." + sfx + ")", text, sfx);
            StringAssert.Contains("SatelliteSounds.PlayMark(n)", text);
            StringAssert.Contains("SatelliteSounds.PlayLight(_lightsShown)", text);
            StringAssert.Contains("SatelliteSounds.Request()", text);
            StringAssert.Contains("_humSource.clip = SatelliteSounds.Hum()", text, "el zumbido en bucle se queda con su altavoz");
            // sigue plano: durante el seguimiento no hay respuesta ni sonido de la paleta (solo el zumbido)
            string c = File.ReadAllText(Path.Combine(Folder(), "SatelliteGameController.cs"));
            int a = c.IndexOf("private IEnumerator DoTrack()", System.StringComparison.Ordinal);
            Assert.GreaterOrEqual(a, 0, "DoTrack existe");
            int b = c.IndexOf("        private ", a + 40, System.StringComparison.Ordinal);
            string track = c.Substring(a, b > a ? b - a : c.Length - a);
            StringAssert.DoesNotContain("SatelliteSounds.Play", track, "el seguimiento no suena notas");
            StringAssert.DoesNotContain("WoodCore.PlayNote", track);
        }

        [Test]
        public void TheHumLoop_StaysAsApproved_OneExactSecond_NeverLouderThanHalf()
        {
            var clip = SatelliteSounds.Hum();
            Assert.AreEqual(44100, clip.samples, "el zumbido dura un segundo exacto: se repite sin chasquido");
            var data = new float[clip.samples];
            clip.GetData(data, 0);
            float peak = 0f;
            foreach (float s in data) peak = Mathf.Max(peak, Mathf.Abs(s));
            Assert.Greater(peak, 0.05f, "suena");
            Assert.LessOrEqual(peak, 0.5001f, "no pasa de 0,5");
            Assert.AreSame(clip, SatelliteSounds.Hum(), "un solo clip");
        }
    }
}
