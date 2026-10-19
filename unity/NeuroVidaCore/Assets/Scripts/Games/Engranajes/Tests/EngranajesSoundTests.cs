using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using NeuroVida.Games.Shared;
using NUnit.Framework;
using UnityEngine;

namespace NeuroVida.Games.Engranajes.Tests
{
    /// <summary>
    /// El sonido de Engranajes en «Madera cálida» (Fase E, Tarea 139). La cascada es la secuencia más densa de la app: los diez pasos conservan su nota (<c>Penta[profundidad + 1]</c>) en marimba de caída 0,15 s y el reloj de la cascada no cambia; la compuerta suena tres golpes graves a 0,27 s y un golpe seco al parar;
    /// el despegue dura lo que hoy y es un hito; «Arrancar» es el toque del kit y la memoria de lo que pide el juego cabe en los 10 MB.
    /// </summary>
    public class EngranajesSoundTests
    {
        private bool _soundWas;

        [SetUp]
        public void SetUp() { _soundWas = GameFeel.SoundOn; GameFeel.SoundOn = true; WoodCore.Reset(); }

        [TearDown]
        public void TearDown() { WoodCore.Reset(); GameFeel.SoundOn = _soundWas; }

        private static string Folder() => Path.Combine(Application.dataPath, "Scripts", "Games", "Engranajes");

        private static string Controllers() => File.ReadAllText(Path.Combine(Folder(), "EngranajesGameController.cs")) + File.ReadAllText(Path.Combine(Folder(), "EngranajesGameController.Scene.cs")) + File.ReadAllText(Path.Combine(Folder(), "EngranajesGameController.Build.cs"));

        [Test]
        public void TheTenStepNotes_KeepTheirPitch_AreMarimba_InTune_AndFallLikeTheCascadeNeeds()
        {
            CollectionAssert.AreEqual(new[] { 523.25f, 587.33f, 659.25f, 783.99f, 880f, 1046.5f, 1174.66f, 1318.5f, 1567.98f, 1760f }, EngranajesSounds.Penta, "la escala de hoy");
            Assert.AreEqual(1, EngranajesSounds.StepIndex(0), "el motor (profundidad 0) suena la nota 1: profundidad + 1, como hoy");
            Assert.AreEqual(9, EngranajesSounds.StepIndex(40), "hasta la última nota");
            Assert.AreEqual(0, EngranajesSounds.StepIndex(-1));
            var decays = new List<double>();
            for (int k = 0; k < EngranajesSounds.Penta.Length; k++)
            {
                double hz = EngranajesSounds.Penta[k];
                WoodSounds.Render(WoodSounds.MakeNote(hz, WoodVoice.Marimba, EngranajesSounds.StepTau), out var l, out var r);
                Assert.Greater(SoundMeter.PitchClarity(l, r, hz), 0.6, "la nota " + k + " (" + hz + " Hz) se oye afinada");
                Assert.Less(SoundMeter.OnsetMs(l, r), 3.0, "la nota " + k + " empieza en menos de 3 ms");
                decays.Add(SoundMeter.DecayMs(l, r, 20.0));
            }
            double mean = 0; foreach (var d in decays) mean += d / decays.Count;
            // 20 dB en ≈ 0,35 s: a 0,15 s entre pasos la pila de notas es la de hoy (con τ 0,3 serían ≈ 700 ms y la cascada se enlodaría)
            Assert.That(mean, Is.InRange(250.0, 450.0), "la caída media de la nota de un paso (ms): " + mean.ToString("0") + " · " + string.Join(" | ", decays.ConvertAll(d => d.ToString("0"))));
            Assert.AreEqual(0.15, EngranajesSounds.StepTau, 1e-9);
        }

        [Test]
        public void EachCascadeStep_IsAClickThenItsNote_AndTheCascadeClockIsUntouched()
        {
            for (int depth = 0; depth < 9; depth++)
            {
                var recipe = EngranajesSounds.MakeRecipe(EngranajesSounds.StepSfx(depth));
                WoodSounds.Render(recipe, out var l, out var r);
                Assert.Less(SoundMeter.OnsetMs(l, r), 3.0, "el paso " + depth + " arranca con su clic de dientes");
                Assert.AreEqual(WoodTier.Respuesta, recipe.Tier, "los pasos son de la familia «respuesta» (baja: van de a diez en 1,5 s)");
            }
            // el reloj de la cascada y sus pasos NO se tocan (la prueba lee el controlador)
            string text = Controllers();
            StringAssert.Contains("0.12f + nextStep * step", text, "el reloj de la cascada");
            StringAssert.Contains("arrive + k * 0.12f", text, "las piezas entran a arrive + k·0,12");
            StringAssert.Contains("arrive + 0.6f", text, "y el acierto a arrive + 0,6");
            StringAssert.Contains("EngranajesSounds.PlayStep(nextStep)", text);
            Assert.AreEqual(0.15f, EngranajesContract.StepSeconds(false), 1e-6f, "un paso cada 0,15 s");
            Assert.AreEqual(0.21f, EngranajesContract.StepSeconds(true), 1e-6f, "y cada 0,21 s en mayores");
        }

        [Test]
        public void TheRack_SoundsThreeLowKnocksEvery270Ms_AndADryKnockWhenItStops()
        {
            Assert.AreEqual(0.27, EngranajesSounds.RackBeat, 1e-9);
            Assert.AreEqual(0.80, EngranajesSounds.RackStop, 1e-9);
            WoodSounds.Render(EngranajesSounds.MakeRecipe(EngranajesSfx.Rack), out var l, out var r);
            var onsets = SoundMeter.OnsetsMs(l, r);
            foreach (var at in new[] { 0.0, 270.0, 540.0, 800.0 })
                Assert.IsTrue(onsets.Exists(o => System.Math.Abs(o - at) <= 3.0), "un golpe a los " + at + " ms (inicios detectados: " + string.Join(", ", onsets.ConvertAll(o => o.ToString("0"))) + ")");
        }

        [Test]
        public void TheLaunch_LastsAsLongAsToday_WithFiveKalimbasFrom1Second_AndItIsAMilestoneNotTheRecord()
        {
            var recipe = EngranajesSounds.MakeRecipe(EngranajesSfx.Launch);
            WoodSounds.Render(recipe, out var l, out var r);
            Assert.GreaterOrEqual(SoundMeter.DurationMs(l, r), 3800.0, "el despegue dura al menos lo de hoy (3,8 s)");
            Assert.AreEqual(WoodTier.Hito, recipe.Tier, "es un HITO, no el Record de la app (ese suena en un récord, no en un despegue)");
            Assert.AreEqual(1.0, EngranajesSounds.LaunchBellsFrom, 1e-9);
            Assert.AreEqual(0.14, EngranajesSounds.LaunchBellGap, 1e-9);
            string text = File.ReadAllText(Path.Combine(Folder(), "EngranajesSounds.cs"));
            StringAssert.DoesNotContain("WoodSfx.Record", text);
            StringAssert.DoesNotContain("WoodSfx.Record", Controllers());
        }

        [Test]
        public void TheGameAsksForEverythingItSounds_AndTheMemoryFitsTheTenMegabytes()
        {
            GameFeel.Warm("engranajes");
            EngranajesSounds.Request();
            int guard = 0; while (!WoodCore.Idle) { WoodCore.Step(1f); Assert.Less(++guard, 200000); }
            foreach (EngranajesSfx sfx in System.Enum.GetValues(typeof(EngranajesSfx))) Assert.IsNotNull(WoodCore.GetExtra(EngranajesSounds.Key(sfx)), "propio: " + sfx);
            foreach (var sfx in new[] { WoodSfx.Hum, WoodSfx.Clack, WoodSfx.Knock, WoodSfx.Perfect, WoodSfx.New }) Assert.IsNotNull(WoodCore.Get(sfx), sfx.ToString());
            for (int k = 1; k <= 8; k++) Assert.IsNotNull(WoodCore.Get(WoodSfx.Spark, k), "luz " + k);
            Assert.AreEqual(0, WoodCore.LateBakes);
            Assert.LessOrEqual(WoodCore.ExtraBytes(), 10L * 1024 * 1024, "lo que pide Engranajes pesa " + (WoodCore.ExtraBytes() / 1024) + " KB (techo 10 MB)");
            Assert.AreEqual(14, new List<(string, WoodSounds.Recipe, bool)>(EngranajesSounds.All()).Count, "14 propios: 10 pasos, antena, turbina, compuerta y despegue");
        }

        [Test]
        public void TheControllerSoundsThroughWoodCore_StartIsTheKitTap_AndNothingElseIsLoose()
        {
            string text = Controllers();
            StringAssert.DoesNotContain("PlayClip", text, "el juego suena todo por WoodCore");
            StringAssert.DoesNotContain("EngranajesSounds.Press", text, "«Arrancar» ya no tiene nota propia");
            StringAssert.Contains("b.Touch.PlaysTap = true", text, "«Arrancar» es el toque del kit");
            foreach (var call in new[] { "WoodCore.Play(WoodSfx.Hum)", "WoodCore.Play(WoodSfx.New)", "WoodCore.Play(WoodSfx.Knock)", "WoodCore.Play(WoodSfx.Perfect)", "WoodCore.Play(WoodSfx.Clack)", "WoodCore.Play(WoodSfx.Knock, gainDb: -6f)", "WoodCore.Play(WoodSfx.Spark, Mathf.Clamp(idx + 1, 1, 8))", "EngranajesSounds.Play(EngranajesSfx.Launch)", "EngranajesSounds.Request()" })
                StringAssert.Contains(call, text, call);
            foreach (var piece in new[] { "Antenna", "Turbine", "Rack" }) StringAssert.Contains("EngranajesSounds.Play(EngranajesSfx." + piece + ")", text);
            Assert.AreEqual(2, Regex.Matches(text, @"WoodCore\.Play\(WoodSfx\.Knock").Count, "el cartel que no cumplió y la jugada que no era");
        }
    }
}
