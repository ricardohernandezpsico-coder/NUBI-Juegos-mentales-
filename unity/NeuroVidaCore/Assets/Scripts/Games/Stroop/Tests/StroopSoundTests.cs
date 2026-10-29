using System.IO;
using NeuroVida.Games.Shared;
using NUnit.Framework;
using UnityEngine;

namespace NeuroVida.Games.Stroop.Tests
{
    /// <summary>
    /// El sonido de Tinta o Palabra en «Madera cálida» (Fase E, Tarea 139): las tres señales (la tarjeta de la regla y los dos cambios de regla) conservan su altura (±1 %) y su duración, y empiezan de inmediato (≤ 2 ms: la instrucción llega antes, no después). La altura ES la regla y el juego mide tiempos
    /// de reacción: solo cambia el timbre.
    /// </summary>
    public class StroopSoundTests
    {
        private bool _soundWas;

        [SetUp]
        public void SetUp() { _soundWas = GameFeel.SoundOn; GameFeel.SoundOn = true; WoodCore.Reset(); }

        [TearDown]
        public void TearDown() { WoodCore.Reset(); GameFeel.SoundOn = _soundWas; }

        private static string Controller() => File.ReadAllText(Path.Combine(Application.dataPath, "Scripts", "Games", "Stroop", "StroopGameController.cs"));

        [Test]
        public void TheThreeSignals_KeepTheirPitch_AreShort_AndStartRightAway()
        {
            Assert.AreEqual(330.0, StroopSounds.RuleHz(true), "«el color de la tinta»: 330 Hz como hoy");
            Assert.AreEqual(440.0, StroopSounds.RuleHz(false), "«lo que dice la palabra»: 440 Hz como hoy");
            Assert.AreEqual(392.0, StroopSounds.CardHz, "la tarjeta de la regla: 392 Hz como hoy");
            foreach (var (hz, seconds) in new[] { (StroopSounds.CardHz, StroopSounds.CardSeconds), (StroopSounds.InkHz, StroopSounds.RuleSeconds), (StroopSounds.WordHz, StroopSounds.RuleSeconds) })
            {
                WoodSounds.Render(WoodSounds.MakeCue(hz, seconds), out var l, out var r);
                Assert.Greater(SoundMeter.PitchClarity(l, r, hz, 2, 120), 0.5, "la señal de " + hz + " Hz se oye en su altura");
                Assert.Less(SoundMeter.PitchClarity(l, r, hz * 1.06, 2, 120), 0.5, "y no en un semitono de arriba (la altura se conserva al 1 %)");
                Assert.LessOrEqual(SoundMeter.DurationMs(l, r, 30.0), 250.0, "la señal de " + hz + " Hz cae a −30 dB en menos de 0,25 s (hoy dura " + seconds + " s)");
                Assert.LessOrEqual(SoundMeter.OnsetMs(l, r), 2.0, "la señal de " + hz + " Hz empieza de inmediato (el tono de hoy tardaba 15 ms)");
            }
            Assert.IsTrue(StroopSounds.RuleHz(true) != StroopSounds.RuleHz(false), "las dos reglas se distinguen por la altura");
        }

        [Test]
        public void TheGameAsksForTheSignals_BeforeTheCountdown_AndNothingIsBakedLate()
        {
            GameFeel.Warm("stroop");
            StroopSounds.Request();
            WoodCore.Drain();
            foreach (var (hz, seconds) in new[] { (StroopSounds.CardHz, StroopSounds.CardSeconds), (StroopSounds.InkHz, StroopSounds.RuleSeconds), (StroopSounds.WordHz, StroopSounds.RuleSeconds) })
                Assert.IsNotNull(WoodCore.GetExtra(WoodCore.CueKey(hz, seconds)), "la señal de " + hz + " Hz");
            WoodCore.ResetCounters();
            StroopSounds.PlayCard(); StroopSounds.PlayRule(true); StroopSounds.PlayRule(false);
            Assert.AreEqual(0, WoodCore.LateBakes, "la señal del estímulo no puede esperar a calcularse");
            CollectionAssert.IsEmpty(System.Linq.Enumerable.ToArray(StroopSounds.All()), "Tinta no tiene sonidos propios");
        }

        [Test]
        public void TheControllerSoundsTheSignalsThroughWoodCore_AndKeepsItsPlasticTap()
        {
            string text = Controller();
            StringAssert.DoesNotContain("PlayTone", text, "ya no hay tonos sueltos");
            StringAssert.Contains("StroopSounds.PlayCard()", text);
            StringAssert.Contains("StroopSounds.PlayRule(ink)", text);
            StringAssert.Contains("StroopSounds.Request()", text);
            StringAssert.Contains("PlaysTap = false", text, "los botones de Tinta siguen sin el toque del kit (suenan su acierto o su error)");
        }
    }
}
