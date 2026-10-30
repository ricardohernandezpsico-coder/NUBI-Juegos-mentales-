using System;
using NUnit.Framework;
using NeuroVida.Contracts;
using NeuroVida.Games.Shared;

namespace NeuroVida.Games.Tests
{
    /// <summary>Las marcas de «ya visto» (Tarea 191): «Borrar datos» las borra dentro del proceso de Unity y «Jugar como la primera vez» no lee ni guarda nada.</summary>
    public class SeenMarksTests
    {
        private Action _clearAll;
        private int _cleared;
        private const string Key = "prueba_marcas_t191";

        [SetUp]
        public void SetUp() { _clearAll = SeenMarks.ClearAll; _cleared = 0; SeenMarks.ClearAll = () => _cleared++; SeenMarks.Configure(null); UnityEngine.PlayerPrefs.DeleteKey(Key); }

        [TearDown]
        public void TearDown() { SeenMarks.ClearAll = _clearAll; SeenMarks.Configure(null); UnityEngine.PlayerPrefs.DeleteKey(Key); }

        [Test]
        public void TheAppAskingToResetClearsUnitysPreferences_InsideUnitysOwnProcess()
        {
            SeenMarks.Configure(new SequenceConfigDetails { reset_unity_prefs = true });
            Assert.AreEqual(1, _cleared, "la config con reset_unity_prefs borra las preferencias de Unity antes de que el juego lea una sola marca");
            SeenMarks.Configure(new SequenceConfigDetails());
            Assert.AreEqual(1, _cleared, "sin el pedido no se borra nada");
        }

        [Test]
        public void ASeenMarkIsKeptByBit_AndSurvivesUntilReset()
        {
            Assert.IsFalse(SeenMarks.IsSeen(Key, 1 << 2));
            SeenMarks.Mark(Key, 1 << 2);
            Assert.IsTrue(SeenMarks.IsSeen(Key, 1 << 2));
            Assert.IsFalse(SeenMarks.IsSeen(Key, 1 << 3), "cada tarjeta tiene su bit");
        }

        [Test]
        public void PlayingLikeTheFirstTime_NeverReadsOrWritesThePreferences()
        {
            SeenMarks.Mark(Key, 1);                                                    // una marca vieja, de verdad
            SeenMarks.Configure(new SequenceConfigDetails { fresh_start = true });
            Assert.IsFalse(SeenMarks.IsSeen(Key, 1), "como la primera vez: la marca vieja no se ve");
            SeenMarks.Mark(Key, 2);
            Assert.IsTrue(SeenMarks.IsSeen(Key, 2), "lo que se ve durante esa partida sí queda marcado en la memoria de la partida");
            SeenMarks.Configure(null);
            Assert.IsFalse(SeenMarks.IsSeen(Key, 2), "pero no se guardó en las preferencias");
            Assert.IsTrue(SeenMarks.IsSeen(Key, 1), "y la marca vieja sigue intacta");
        }

        [Test]
        public void ResetIsIgnoredWhenPlayingLikeTheFirstTime_SoTheRealMarksAreNeverErased()
        {
            SeenMarks.Configure(new SequenceConfigDetails { fresh_start = true, reset_unity_prefs = true });
            Assert.AreEqual(0, _cleared);
        }
    }
}
