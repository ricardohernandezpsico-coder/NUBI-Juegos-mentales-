using NUnit.Framework;
using NeuroVida.Games.Shared;

namespace NeuroVida.Games.Tests
{
    /// <summary>El orden de la entrada (Tarea 191): las tarjetas «NUEVO» y las reglas de Freno van ANTES del «¡Ya!»; el «¡Ya!» es uno solo y va justo antes del primer ensayo; la hoja de la mañana de Correo es juego y va después.</summary>
    public class EntrySequenceTests
    {
        private static System.Collections.Generic.List<string> Check(params string[] events) => EntrySequence.Problems(events);

        [Test]
        public void TheCardsAndTheRulesComeBeforeTheGo_AndTheMorningSheetAfterIt()
        {
            CollectionAssert.IsEmpty(Check("tarjeta", "tarjeta", "ya", "primer ensayo", "hoja", "tarjeta"));
            CollectionAssert.IsEmpty(Check("reglas", "ya", "primer ensayo"), "el repaso de Freno va antes del «¡Ya!»");
            CollectionAssert.IsEmpty(Check("ya", "primer ensayo", "tarjeta"), "una tarjeta a mitad de partida (la etapa sube) va antes de su ensayo y sin otro «¡Ya!»");
            CollectionAssert.IsEmpty(Check(), "una corrida que no llegó al «¡Ya!» no tiene nada que ordenar");
            CollectionAssert.IsEmpty(Check("tarjeta", "ya"), "una corrida que terminó mientras sonaba el «¡Ya!» tampoco");
        }

        [Test]
        public void ACardRightAfterTheGo_ASecondGo_OrTheSheetBeforeTheGo_AreWrong()
        {
            Assert.IsNotEmpty(Check("ya", "tarjeta", "primer ensayo"), "una tarjeta entre el «¡Ya!» y el primer ensayo");
            Assert.IsNotEmpty(Check("ya", "primer ensayo", "ya", "primer ensayo"), "dos «¡Ya!» en una partida");
            Assert.IsNotEmpty(Check("hoja", "ya", "primer ensayo"), "la hoja de la mañana es juego: antes del «¡Ya!» no");
            Assert.IsNotEmpty(Check("ya", "primer ensayo", "reglas"), "el repaso de reglas después del «¡Ya!»");
        }
    }
}
