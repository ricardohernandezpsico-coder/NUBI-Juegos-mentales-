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

        [Test]
        public void SinLaBanderaLasMarcasNoHacenNada()
        {
            EntryProbe.Begin(false, "stroop", 123456789L);
            EntryProbe.Note("¡Ya!");
            Assert.IsFalse(EntryProbe.Enabled);
        }

        [Test]
        public void ElCuadroLargoDiceSuMarcaYLoQueMasPasoEnEseCuadro()
        {
            string line = EntryProbe.FormatHitch("stroop", 2.69f, 50f, "primer ensayo", 1.5f, 1, 0, true);
            StringAssert.Contains("[sonda-entrada-cuadro] juego=stroop a los 2.69 s de «listo»: cuadro de 50 ms", line);
            StringAssert.Contains("última marca «primer ensayo» hace 1.50 s", line);
            StringAssert.Contains("letras reconstruidas en ese cuadro: 1", line);
            StringAssert.Contains("recolecciones de memoria: 0", line);
            StringAssert.Contains("toque: sí", line);
            StringAssert.Contains("sin marca", EntryProbe.FormatHitch("stroop", 0.09f, 68f, "", 0f, 0, 0, false));
            StringAssert.Contains("toque: no", EntryProbe.FormatHitch("stroop", 0.09f, 68f, "", 0f, 0, 0, false));
        }

        [Test]
        public void ElTraspasoRepartePorSegundosElToqueLaConfiguracionYElListo_YSinHoraDelToqueNoDiceNada()
        {
            string text = EntryProbe.HandoffText(1_000_000L, 1_000_900L, 1_001_400L);
            StringAssert.Contains("a la configuración en Unity 0.90 s y al «listo» 1.40 s", text);
            Assert.AreEqual("", EntryProbe.HandoffText(0L, 1_000_900L, 1_001_400L));
        }

        private static void Shared_ShownIsHarmless()
        {
            EntryProbe.Shown();
            Assert.IsFalse(EntryProbe.Enabled);
        }
    }
}
