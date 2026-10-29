using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using NeuroVida.Games.Shared;

namespace NeuroVida.Games.Tests
{
    /// <summary>
    /// La sonda de entrada (Tarea 190, 190b) solo mide con la bandera de depuración: en release no crea objetos, no anota el reinicio ni escribe nada.
    /// </summary>
    public class EntryProbeTests
    {
        private static float RestartAt() => (float)typeof(EntryProbe).GetField("_restartAt", BindingFlags.NonPublic | BindingFlags.Static).GetValue(null);

        [Test]
        public void SinLaBanderaDeDepuracionNoMideNiCreaObjetosNiAnotaElReinicio()
        {
            EntryProbe.Begin(false, "stroop");
            Assert.IsFalse(EntryProbe.Enabled);
            Assert.IsNull(GameObject.Find("EntryProbe"), "en release no hay ni controlador");
            EntryProbe.MarkRestart();
            Assert.AreEqual(-1f, RestartAt(), "sin la bandera, el toque de «Reiniciar» no se anota");
            EntryProbe.Cost("sonido", 5.0);
            Shared_ShownIsHarmless();
        }

        private static void Shared_ShownIsHarmless()
        {
            EntryProbe.Shown();
            Assert.IsFalse(EntryProbe.Enabled);
        }
    }
}
