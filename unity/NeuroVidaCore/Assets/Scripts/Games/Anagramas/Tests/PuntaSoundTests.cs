using System.IO;
using NeuroVida.Games.Shared;
using NUnit.Framework;
using UnityEngine;

namespace NeuroVida.Games.Anagramas.Tests
{
    /// <summary>El sonido de En la punta de la lengua en «Madera cálida» (Fase C, Tarea 135): las 10 notas de las casillas son kalimba afinada que empieza enseguida, el juego las pide por adelantado y los retardos de la cascada final no cambian.</summary>
    public class PuntaSoundTests
    {
        private bool _soundWas;

        [SetUp]
        public void SetUp() { _soundWas = GameFeel.SoundOn; GameFeel.SoundOn = true; WoodCore.Reset(); }

        [TearDown]
        public void TearDown() { WoodCore.Reset(); GameFeel.SoundOn = _soundWas; }

        [Test]
        public void TheTenNotesOfTheSlots_AreKalimba_InTune_AndStartAtOnce()
        {
            Assert.AreEqual(10, PuntaSounds.Penta.Length);
            for (int k = 0; k < PuntaSounds.Penta.Length; k++)
            {
                double hz = PuntaSounds.Penta[k];
                WoodSounds.Render(WoodSounds.MakeNote(hz, WoodVoice.Kalimba), out var l, out var r);
                Assert.Greater(SoundMeter.PitchClarity(l, r, hz), 0.6, "la nota de la casilla " + k + " (" + hz + " Hz) se oye afinada");
                Assert.Less(SoundMeter.OnsetMs(l, r), 3.0, "la nota de la casilla " + k + " empieza en menos de 3 ms");
                if (k > 0) Assert.Greater(PuntaSounds.Penta[k], PuntaSounds.Penta[k - 1], "la melodía sube con las casillas: el orden es información");
            }
        }

        [Test]
        public void TheGameAsksForItsTenNotes_BeforeTheCountdown()
        {
            GameFeel.Warm("anagramas");
            PuntaSounds.Request();
            int guard = 0; while (!WoodCore.Idle) { WoodCore.Step(1f); Assert.Less(++guard, 200000); }
            for (int k = 0; k < PuntaSounds.Penta.Length; k++)
                Assert.IsNotNull(WoodCore.GetExtra(WoodCore.NoteKey(PuntaSounds.Penta[k], WoodVoice.Kalimba)), "la nota " + k + " ya está calculada");
            Assert.AreEqual(0, WoodCore.LateBakes, "nada se calculó tarde");
        }

        [Test]
        public void TheDelaysOfTheFinalCascade_AreStillTheOnesOfBefore()
        {
            string text = File.ReadAllText(Path.Combine(Application.dataPath, "Scripts", "Games", "Anagramas", "PuntaGameController.cs"));
            StringAssert.Contains("calm ? 0f : k * 0.09f", text, "cada letra se enciende k · 0,09 s después de la anterior");
            StringAssert.Contains("n * 0.09f + 0.5f", text, "la cola de la cascada");
            StringAssert.Contains("Mathf.Max(0f, tail - 0.2f)", text, "el acorde final entra 0,2 s antes del cierre");
        }
    }
}
