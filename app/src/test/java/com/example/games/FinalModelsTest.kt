package com.example.games

import com.example.data.MeasurePoint
import com.example.model.GamePlayResult
import org.junit.Assert.assertEquals
import org.junit.Assert.assertNotNull
import org.junit.Assert.assertNull
import org.junit.Assert.assertTrue
import org.junit.Test

/** Los finales con sentido del grupo 1 (Memoria): lo que hiciste con UN dato tuyo, tu avance con su unidad, el truco (condicional o general) y por qué importa con su fuente. */
class FinalModelsTest {
  private fun result(id: String, block: GamePlayResult.() -> GamePlayResult = { this }) =
    GamePlayResult(gameId = id, score = 70, correctAnswers = 7, totalTrials = 10, timed = false, level = 3, endRating = 0.5f, timestamp = 1_000L).block()

  private fun points(key: String, vararg values: Float) = values.mapIndexed { i, v -> MeasurePoint(100L * (i + 1), key, v, 0.5f, false) }

  private val constelaciones = result("parejas") { copy(correctAnswers = 7, totalTrials = 9, conGroup = 4, conGroups = 6, conMemGroups = 4, conBestStreak = 5, conBest = 6, conNewRecord = false) }
  private val rastro = result("secuencia") { copy(correctAnswers = 14, totalTrials = 19, rasBestLen = listOf(5, 4, 3, 2), rasRounds = listOf(7, 4, 5, 3), rasHits = listOf(7, 3, 2, 2), rasModesSeen = 15, rasNewModes = 0) }
  private val bodega = result("bodega") { copy(correctAnswers = 9, totalTrials = 12, bodGroup = 3, bodBestStreak = 4, bodBiggest = 5, bodBest = 6, bodNewRecord = false, bodMs = 4500) }
  private val correo = result("correo") { copy(correctAnswers = 8, totalTrials = 11, mailGroup = 2, mailEvHits = 4, mailEvTotal = 4, mailTimeHits = 1, mailTimeTotal = 2, mailCommissions = 0, mailPeeks = 3, mailPeeksGood = 1, mailRight = 6, mailBest = 9, mailNewRecord = false) }

  @Test
  fun `solo los juegos ya llevados al final con sentido lo usan`() {
    for (r in listOf(constelaciones, rastro, bodega, correo)) assertTrue(r.gameId, FinalModels.usesMeaningful(r))
    assertTrue(FinalModels.usesMeaningful(result("aterrizaje") { copy(numlineErrorPct = 8f) }))
    // sin los datos propios del juego (una partida de la versión vieja de Unity) se queda con el final de siempre
    for (id in listOf("parejas", "secuencia", "bodega", "correo", "aterrizaje")) assertTrue(id, !FinalModels.usesMeaningful(result(id)))
    for (id in listOf("stroop", "piloto", "freno", "satelites", "radar", "calculo", "engranajes", "acoplamiento", "anagramas", "meteoros", "disparate", "cosecha", "intrusa")) assertNull(id, FinalModels.forGame(result(id), emptyList()))
    assertNotNull(FinalModels.forGame(constelaciones, emptyList()))
  }

  @Test
  fun `Constelaciones dice el porcentaje, lo que paso y tu racha, y su truco condicional si algo se escapo`() {
    val m = FinalModels.forGame(constelaciones, emptyList())!!
    assertEquals("Tu memoria de lugar", m.boxTitle)
    assertEquals("78 %", m.headline)
    assertEquals("7 de 9 veces fuiste directo a una pareja que ya habías visto", m.headlineUnit)
    assertEquals("UN dato tuyo: la racha", "Racha de memoria más larga: 5", m.dataLine)
    assertEquals("se escapó alguna: el truco condicional", "Da vuelta primero una luz nueva.", m.trick)
    // sin nada que se escapara: el truco general de la tabla
    val perfect = FinalModels.forGame(constelaciones.copy(correctAnswers = 9), emptyList())!!
    assertEquals("Da vuelta primero una luz nueva: te muestra más del cielo.", perfect.trick)
    assertEquals("Pusiste en juego la memoria de lugar: recordar dónde quedó cada cosa. Recordar qué va con qué, como una cosa y su lugar, es la parte de la memoria que más atención pide con los años.", m.why)
    assertEquals("Fuente: Old y Naveh-Benjamin, 2008", m.source)
    assertEquals("Medida de esta partida. No es un diagnóstico.", m.note)
    // un récord nuevo es el dato a la vista
    assertEquals("¡Nueva mejor racha! 6", FinalModels.forGame(constelaciones.copy(conNewRecord = true), emptyList())!!.dataLine)
  }

  @Test
  fun `Constelaciones sin oportunidades no inventa un porcentaje`() {
    val m = FinalModels.forGame(constelaciones.copy(correctAnswers = 0, totalTrials = 0), listOf(MeasurePoint(100L, "place", 60f, 0.5f, false)))!!
    assertEquals("—", m.headline)
    assertEquals("No hubo parejas vistas que recordar", m.headlineUnit)
    assertEquals(listOf("—", "—", "—"), m.chips.map { it.value })
    assertEquals("Juega otra vez para ver tu avance.", m.progressPhrase)
  }

  @Test
  fun `Rastro dice las luces mas largas, lo que mas costo y el truco del modo que mas costo`() {
    val m = FinalModels.forGame(rastro, emptyList())!!
    assertEquals("Tu rastro", m.boxTitle)
    assertEquals("5 luces", m.headline)
    assertEquals("las más largas que repetiste bien en el rastro simple", m.headlineUnit)
    assertEquals("Con el giro hay que recordar el orden y no el lugar: tus luceros se mueven.", m.dataLine)
    assertEquals("Sigue a un lucero por su color mientras el cielo gira; el orden va con los luceros, no con el lugar.", m.trick)
    // sin modo marcado como el que más costó: el truco general
    val simple = FinalModels.forGame(rastro.copy(rasRounds = listOf(7, 0, 0, 0), rasHits = listOf(7, 0, 0, 0)), emptyList())!!
    assertEquals("Junta las luces de a dos o tres, como cuando dictas un número de teléfono.", simple.trick)
    assertNull(simple.dataLine)
    // un modo nuevo desbloqueado es el dato a la vista
    assertEquals("Desbloqueaste un modo nuevo: Al revés", FinalModels.forGame(rastro.copy(rasNewModes = 2), emptyList())!!.dataLine)
    assertEquals("Fuente: Baddeley, 2003", m.source)
  }

  @Test
  fun `Rastro sin rastro completo lo dice sin culpa`() {
    val m = FinalModels.forGame(rastro.copy(rasBestLen = listOf(0, 0, 0, 0), rasRounds = listOf(2, 0, 0, 0), rasHits = listOf(0, 0, 0, 0)), emptyList())!!
    assertEquals("—", m.headline)
    assertTrue(m.headlineUnit.isNotBlank())
    assertEquals(listOf("—", "—", "—"), m.chips.map { it.value })
  }

  @Test
  fun `Bodega dice los objetos al primer intento, tu bodega mas grande y el truco de la escotilla`() {
    val m = FinalModels.forGame(bodega, emptyList())!!
    assertEquals("Tu bodega", m.boxTitle)
    assertEquals("9 de 12 objetos", m.headline)
    assertEquals("al primer intento", m.headlineUnit)
    assertEquals("Tu bodega más grande hoy: 5 objetos sin errores", m.dataLine)
    assertEquals("Imagina cada objeto dentro de su escotilla y dile su nombre en voz baja.", m.trick)
    assertEquals("¡Nuevo récord! 6 objetos", FinalModels.forGame(bodega.copy(bodNewRecord = true), emptyList())!!.dataLine)
    assertEquals("Fuente: Postma, Kessels y van Asselen, 2008", m.source)
    assertEquals("los cuadros van en % porque cada partida trae un total de objetos distinto", "75 %", m.chips[0].value)
  }

  @Test
  fun `Correo dice los encargos cumplidos, la mirada al reloj y el truco segun lo que se escapo`() {
    val m = FinalModels.forGame(correo, emptyList())!!
    assertEquals("Tu memoria para lo pendiente", m.boxTitle)
    assertEquals("73 %", m.headline)
    assertEquals("8 de 11 encargos cumplidos", m.headlineUnit)
    assertEquals("Miradas al reloj cerca de la hora: 1 de 3", m.dataLine)
    assertEquals("se escapó una hora y miró poco el reloj", "Mira el reloj cuando se acerque la hora.", m.trick)
    // todo salió bien: el truco general
    val ok = FinalModels.forGame(correo.copy(mailEvHits = 4, mailTimeHits = 2, mailPeeksGood = 3), emptyList())!!
    assertEquals("Antes de empezar, dilo en voz baja: «cuando vea…, haré…».", ok.trick)
    assertEquals("Fuente: Hering y otros, 2014", m.source)
  }

  @Test
  fun `tu avance usa la misma unidad que el juego y se compara solo con partidas anteriores comparables`() {
    // Constelaciones: hoy 78 % (7 de 9); antes 60, 70 y 80 %
    val m = FinalModels.forGame(constelaciones, points("place", 60f, 70f, 80f))!!
    assertEquals(listOf("Hoy", "Tu promedio", "Tu mejor"), m.chips.map { it.label })
    assertEquals(listOf("78 %", "70 %", "80 %"), m.chips.map { it.value })
    assertEquals("Hoy te fue mejor que tu promedio.", m.progressPhrase)
    assertEquals("Tu avance", m.progressTitle)
    // Rastro: hoy 5 luces; antes 4, 5, 4, 5, 4 (promedio 4,4 → «5 luces» de promedio, redondeado)
    val r = FinalModels.forGame(rastro, points("trail", 4f, 5f, 4f, 5f, 4f))!!
    assertEquals(listOf("5 luces", "4 luces", "5 luces"), r.chips.map { it.value })
    assertEquals("Hoy te fue mejor que tu promedio.", r.progressPhrase)
    // la de hoy ya guardada no se cuenta dos veces
    val again = FinalModels.forGame(constelaciones, points("place", 60f, 70f, 80f) + MeasurePoint(1_000L, "place", 100f * 7 / 9, 0.5f, false))!!
    assertEquals(m.chips.map { it.value }, again.chips.map { it.value })
    // otro reloj: no se compara
    val otherClock = FinalModels.forGame(constelaciones, listOf(MeasurePoint(100L, "place", 60f, 0.5f, true)))!!
    assertEquals("Juega otra vez para ver tu avance.", otherClock.progressPhrase)
    assertEquals("—", otherClock.chips[1].value)
  }

  @Test
  fun `la primera partida no tiene con que compararse`() {
    val m = FinalModels.forGame(bodega, emptyList())!!
    assertEquals(listOf("75 %", "—", "—"), m.chips.map { it.value })
    assertEquals("Juega otra vez para ver tu avance.", m.progressPhrase)
  }

  @Test
  fun `todos los textos del final cumplen las reglas de vocabulario`() {
    for (r in listOf(constelaciones, rastro, bodega, correo)) {
      val m = FinalModels.forGame(r, points(if (r.gameId == "parejas") "place" else "x", 60f, 70f))!!
      val all = listOfNotNull(m.boxTitle, m.headline, m.headlineUnit, m.dataLine, m.progressPhrase, m.trick, m.why, m.source, m.note, m.spoken).joinToString(" ").lowercase()
      for (bad in listOf("entren", "cerebro", "cognitiv", "percentil", "otras personas")) assertTrue("${r.gameId}: «$bad»", !all.contains(bad))
    }
  }
}
