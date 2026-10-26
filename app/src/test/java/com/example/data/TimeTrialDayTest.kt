package com.example.data

import com.example.model.GamePlayResult
import com.example.model.GameRegistry
import org.junit.Assert.assertEquals
import org.junit.Assert.assertFalse
import org.junit.Assert.assertNotEquals
import org.junit.Assert.assertNull
import org.junit.Assert.assertTrue
import org.junit.Test
import java.io.File

/**
 * El Contrarreloj de hoy (`TimeTrialDay`, Tarea 177): el juego sale de la FECHA (el mismo para todas las personas y rota cada cuatro días), la pista (versión, paso y largo) es la de Unity, el estado del día se DERIVA de lo guardado (pendiente · jugado · marca nueva) y los textos son los del diseño de Fable
 * (con «Acoplamiento» en lugar de «Carga exacta»). Lógica pura.
 */
class TimeTrialDayTest {
  private val day = "2026-10-26"
  // por defecto SIN errores: la marca (aciertos menos errores) es igual a los aciertos; las pruebas de la marca neta ponen sus errores
  private fun r(game: String, hits: Int, at: Long, d: String = day, version: Int = 2, errors: Int = 0, level: Int = 2, id: String = "r$at") =
    TimeTrialResult(id = id, gameId = game, day = d, trackVersion = version, hits = hits, errors = errors, level = level, seconds = 120, timestamp = at)

  // ------------------------------------------------------------------ el juego del día

  @Test
  fun `la primera tanda son los cuatro juegos de Ricardo y Carga exacta no esta`() {
    assertEquals(listOf("stroop", "disparate", "radar", "acoplamiento"), TimeTrialDay.GAMES)
    assertFalse("a Ricardo no le gusta Carga exacta", "calculo" in TimeTrialDay.GAMES)
    assertFalse("Piloto queda para la fase 3", "piloto" in TimeTrialDay.GAMES)
    for (g in TimeTrialDay.GAMES) assertTrue("$g está en el catálogo", GameRegistry.getById(g) != null)
    assertEquals(TimeTrialDay.GAMES.toSet(), TimeTrialDay.TRACKS.keys)
  }

  @Test
  fun `el juego sale de la fecha y rota cada cuatro dias`() {
    // 26-oct-2026 = día 20752 desde 1970 (módulo 4 = 0): Tinta o Palabra; después, Disparate, Rescate y Acoplamiento, y vuelta
    assertEquals(listOf("stroop", "disparate", "radar", "acoplamiento", "stroop", "disparate"), (26..31).map { TimeTrialDay.gameFor("2026-10-$it") })
    val twoWeeks = (0 until 28).map { TimeTrialDay.gameFor(java.time.LocalDate.of(2026, 10, 26).plusDays(it.toLong()).toString()) }
    assertEquals("cada juego sale lo mismo", TimeTrialDay.GAMES.associateWith { 7 }, twoWeeks.groupingBy { it }.eachCount())
    for (i in 1 until twoWeeks.size) assertNotEquals("nunca el mismo dos días seguidos", twoWeeks[i - 1], twoWeeks[i])
  }

  @Test
  fun `es el mismo para todas las personas porque depende solo de la fecha`() {
    for (d in listOf("2026-01-01", "2026-10-26", "2027-02-28", "2030-12-31")) assertEquals(TimeTrialDay.gameFor(d), TimeTrialDay.gameFor(d))
    assertEquals("cruza el cambio de mes y de año sin saltos", listOf("2026-12-30", "2026-12-31", "2027-01-01", "2027-01-02", "2027-01-03").map { TimeTrialDay.gameFor(it) }.let { it.zipWithNext().all { (a, b) -> a != b } }, true)
  }

  @Test
  fun `la semilla que se manda a Unity es la fecha como numero`() {
    assertEquals(20261026, TimeTrialDay.trackSeed("2026-10-26"))
    assertEquals(20270102, TimeTrialDay.trackSeed("2027-01-02"))
    assertEquals(0, TimeTrialDay.trackSeed("sin fecha"))
  }

  // ------------------------------------------------------------------ la pista

  @Test
  fun `la tabla de pistas es la de Unity en TimeTrial_cs con versiones, pasos y largo`() {
    val rel = "unity/NeuroVidaCore/Assets/Scripts/Games/Shared/TimeTrial.cs"
    val cs = listOf(File(rel), File("../$rel")).firstOrNull { it.exists() }?.readText()?.replace("\r\n", "\n") ?: error("no encuentro $rel desde ${File(".").absolutePath}")
    fun contract(name: String, folder: String): Pair<String, Int> {
      val src = listOf("unity/NeuroVidaCore/Assets/Scripts/Games/$folder/$name.cs").map { p -> listOf(File(p), File("../$p")).first { it.exists() } }.first().readText()
      val id = Regex("""public const string GameId = "(\w+)"""").find(src)!!.groupValues[1]
      val max = Regex("""public const int MaxLevel = (\d+)""").find(src)!!.groupValues[1].toInt()
      return id to max
    }
    val contracts = mapOf(
      "StroopContract" to contract("StroopContract", "Stroop"), "DisparateContract" to contract("DisparateContract", "Disparate"),
      "RadarContract" to contract("RadarContract", "Radar"), "DockingContract" to contract("DockingContract", "Acoplamiento")
    )
    val tracks = Regex("""new Track\((\w+)\.GameId,\s*(\d+),\s*(\d+),\s*(\w+)\.MaxLevel\)""").findAll(cs).toList()
    assertEquals("las cuatro pistas de Unity", 4, tracks.size)
    assertEquals("el orden de la rotación es el mismo", TimeTrialDay.GAMES, tracks.map { contracts[it.groupValues[1]]!!.first })
    for (m in tracks) {
      val (id, max) = contracts[m.groupValues[1]]!!
      val t = TimeTrialDay.TRACKS[id]!!
      assertEquals("versión de la pista de $id", m.groupValues[2].toInt(), t.version)
      assertEquals("cada cuántos aciertos sube un paso en $id", m.groupValues[3].toInt(), t.stepEvery)
      assertEquals("pasos de la pista de $id (los de la escalera del juego)", max, t.maxLevel)
      assertEquals(max, contracts[m.groupValues[4]]!!.second)
    }
    assertEquals("la partida dura lo mismo", TimeTrialDay.SECONDS, Regex("""public const int Seconds = (\d+);""").find(cs)!!.groupValues[1].toInt())
  }

  @Test
  fun `la versión y el largo de la pista salen de la tabla`() {
    assertEquals(5, TimeTrialDay.maxLevel("stroop"))
    assertEquals(12, TimeTrialDay.maxLevel("acoplamiento"))
    assertEquals("la versión 2 es la de la marca neta (Tarea 178)", 2, TimeTrialDay.trackVersion("radar"))
    assertTrue(TimeTrialDay.supports("disparate"))
    assertFalse(TimeTrialDay.supports("calculo"))
  }

  // ------------------------------------------------------------------ el estado del día se deriva de lo guardado

  @Test
  fun `sin partidas hoy esta pendiente aunque haya de otros dias o de otros juegos`() {
    assertEquals(TimeTrialState.PENDIENTE, TimeTrialDay.stateOf(emptyList(), day, "stroop"))
    val others = listOf(r("stroop", 30, 100, d = "2026-10-25"), r("disparate", 20, 200))
    assertEquals(TimeTrialState.PENDIENTE, TimeTrialDay.stateOf(others, day, "stroop"))
  }

  @Test
  fun `la primera partida de un juego se juega pero nunca es marca nueva`() {
    assertEquals(TimeTrialState.JUGADO, TimeTrialDay.stateOf(listOf(r("stroop", 38, 100)), day, "stroop"))
  }

  @Test
  fun `superar el record de antes es marca nueva, igualarlo o quedarse abajo no`() {
    val before = r("stroop", 41, 100, d = "2026-10-23")
    assertEquals(TimeTrialState.MARCA_NUEVA, TimeTrialDay.stateOf(listOf(before, r("stroop", 43, 200)), day, "stroop"))
    assertEquals("igualar no es superar", TimeTrialState.JUGADO, TimeTrialDay.stateOf(listOf(before, r("stroop", 41, 200)), day, "stroop"))
    assertEquals(TimeTrialState.JUGADO, TimeTrialDay.stateOf(listOf(before, r("stroop", 38, 200)), day, "stroop"))
  }

  @Test
  fun `la marca nueva no se pierde al repetir sin superarla`() {
    val all = listOf(r("stroop", 41, 100, d = "2026-10-23"), r("stroop", 43, 200), r("stroop", 30, 300))
    assertEquals(TimeTrialState.MARCA_NUEVA, TimeTrialDay.stateOf(all, day, "stroop"))
  }

  @Test
  fun `el record que se compara es el de justo antes de cada partida`() {
    // 30, luego 35 (marca nueva), luego 33 (no): el estado de hoy es marca nueva por la segunda
    val all = listOf(r("radar", 30, 100, d = "2026-10-20"), r("radar", 35, 200), r("radar", 33, 300))
    assertEquals(TimeTrialState.MARCA_NUEVA, TimeTrialDay.stateOf(all, day, "radar"))
    val mine = TimeTrialDay.ofGame(all, "radar")
    assertFalse(TimeTrialDay.isNewRecord(mine, mine[0]))
    assertTrue(TimeTrialDay.isNewRecord(mine, mine[1]))
    assertFalse(TimeTrialDay.isNewRecord(mine, mine[2]))
  }

  @Test
  fun `si cambia la version de la pista el record empieza de nuevo y el viejo no cuenta`() {
    val old = r("stroop", 50, 100, d = "2026-10-20", version = 1)       // una pista anterior (la de hoy es la 2: la marca pasó a ser aciertos menos errores)
    val now = r("stroop", 20, 200)
    assertEquals("20 es la primera partida con la pista de hoy: no es marca nueva", TimeTrialState.JUGADO, TimeTrialDay.stateOf(listOf(old, now), day, "stroop"))
    assertEquals(listOf(now), TimeTrialDay.ofGame(listOf(old, now), "stroop"))
  }

  // ------------------------------------------------------------------ la marca: aciertos menos errores (Tarea 178)

  @Test
  fun `la marca es aciertos menos errores con piso en cero, los mismos casos que Unity`() {
    // los MISMOS casos que `TimeTrialTests.TheMarkIsHitsMinusErrors_FlooredAtZero` en Unity (`TimeTrial.Mark`): la fórmula está en los dos lados
    assertEquals(34, TimeTrialDay.markOf(2, 38, 4))
    assertEquals(37, TimeTrialDay.markOf(2, 43, 6))
    assertEquals("nunca negativa", 0, TimeTrialDay.markOf(2, 3, 5))
    assertEquals(0, TimeTrialDay.markOf(2, 0, 0))
    assertEquals(1, TimeTrialDay.markOf(2, 1, 0))
    assertEquals("la pista 1 contaba solo los aciertos", 38, TimeTrialDay.markOf(1, 38, 4))
    assertEquals(34, r("stroop", 38, 100, errors = 4).mark)
  }

  @Test
  fun `la marca nueva se decide con la marca neta y no con los aciertos`() {
    val before = r("stroop", 41, 100, d = "2026-10-23")                                   // récord: 41
    val clean = r("stroop", 40, 200, errors = 0)                                          // 40: no supera
    val messy = r("stroop", 50, 300, errors = 12)                                         // 50 aciertos pero 38 de marca: no supera
    val good = r("stroop", 45, 400, errors = 2)                                           // 43: sí
    assertEquals(TimeTrialState.JUGADO, TimeTrialDay.stateOf(listOf(before, clean, messy), day, "stroop"))
    assertEquals(TimeTrialState.MARCA_NUEVA, TimeTrialDay.stateOf(listOf(before, clean, messy, good), day, "stroop"))
    assertEquals("la marca de hoy en Hoy es la neta", 43, TimeTrialDay.ofGame(listOf(before, good), "stroop").filter { it.day == day }.maxOf { it.mark })
  }

  // ------------------------------------------------------------------ lo que Unity devuelve

  @Test
  fun `la partida que devuelve Unity se guarda con su marca, sus errores, su paso y la version con que se jugo`() {
    val raw = GamePlayResult(gameId = "acoplamiento", score = 50, correctAnswers = 19, totalTrials = 23, timed = true, level = 3, timestamp = 777L, ttHits = 19, ttErrors = 4, ttPeakLevel = 10, ttSeconds = 120, ttTrackVersion = 2)
    val saved = TimeTrialDay.resultOf(raw, "2026-10-26")
    assertEquals(Triple("acoplamiento", "2026-10-26", 2), Triple(saved.gameId, saved.day, saved.trackVersion))
    assertEquals("Unity manda los aciertos y los errores SIN restar; la marca la calcula la app", 15, saved.mark)
    assertEquals(listOf<Any>(19, 4, 10, 120, 777L), listOf<Any>(saved.hits, saved.errors, saved.level, saved.seconds, saved.timestamp))
    assertTrue(raw.isTimeTrial)
    assertFalse(raw.copy(ttHits = null).isTimeTrial)
  }

  // ------------------------------------------------------------------ los textos (Fable §9.8)

  @Test
  fun `los textos son los del diseno, con aciertos en singular y plural`() {
    assertEquals("Contrarreloj de hoy", TimeTrialDay.LABEL)
    assertEquals("2 minutos", TimeTrialDay.MINUTES_TEXT)
    assertEquals("1 acierto", TimeTrialDay.hitsText(1))
    assertEquals("0 aciertos", TimeTrialDay.hitsText(0))
    assertEquals("38 aciertos", TimeTrialDay.hitsText(38))
    assertEquals("Tu récord: 41 aciertos.", TimeTrialDay.recordLine(41))
    assertEquals("Tu récord: 1 acierto.", TimeTrialDay.recordLine(1))
    assertEquals("Marca nueva: 43 aciertos en Tinta o Palabra.", TimeTrialDay.newMarkLine(43, "Tinta o Palabra"))
    assertEquals("hoy 38 aciertos", TimeTrialDay.todayText(38))
  }

  @Test
  fun `la segunda linea del boton cambia con el estado, el ancho y la letra grande`() {
    val t = "Tinta o Palabra"
    assertNull("en media fila, pendiente, no hay segunda línea", TimeTrialDay.secondLine(t, TimeTrialState.PENDIENTE, null, wide = false, large = false))
    assertEquals("Tinta o Palabra · 2 minutos", TimeTrialDay.secondLine(t, TimeTrialState.PENDIENTE, null, wide = true, large = false))
    assertEquals("con letra 1,3 solo el juego", "Tinta o Palabra", TimeTrialDay.secondLine(t, TimeTrialState.PENDIENTE, null, wide = true, large = true))
    assertEquals("Tinta o Palabra · hoy 38 aciertos", TimeTrialDay.secondLine(t, TimeTrialState.JUGADO, 38, wide = true, large = false))
    assertEquals("hoy 38 aciertos", TimeTrialDay.secondLine(t, TimeTrialState.JUGADO, 38, wide = true, large = true))
    assertEquals("otra vez", TimeTrialDay.secondLine(t, TimeTrialState.JUGADO, 38, wide = false, large = false))
  }

  @Test
  fun `el lector de pantalla dice todo en el boton`() {
    assertEquals("Contrarreloj de hoy, Tinta o Palabra, 2 minutos, tu récord 41 aciertos", TimeTrialDay.description("Tinta o Palabra", TimeTrialState.PENDIENTE, null, 41))
    assertEquals("Contrarreloj de hoy, Tinta o Palabra, 2 minutos", TimeTrialDay.description("Tinta o Palabra", TimeTrialState.PENDIENTE, null, null))
    assertEquals("Contrarreloj de hoy, Tinta o Palabra, 2 minutos, hoy 38 aciertos, otra vez, tu récord 41 aciertos", TimeTrialDay.description("Tinta o Palabra", TimeTrialState.JUGADO, 38, 41))
  }

  @Test
  fun `una palabra por concepto, nunca Reto ni Desafio ni Con reloj para el Contrarreloj`() {
    val texts = listOf(TimeTrialDay.LABEL, TimeTrialDay.recordLine(3), TimeTrialDay.newMarkLine(3, "X"), TimeTrialDay.todayText(3), TimeTrialDay.description("X", TimeTrialState.JUGADO, 3, 4)) +
      TimeTrialState.values().mapNotNull { TimeTrialDay.secondLine("X", it, 3, wide = true, large = false) }
    for (t in texts) for (banned in listOf("Reto", "reto ", "Desafío", "Con reloj", "nivel", "Nivel", "racha")) assertFalse("«$t» no debe decir «$banned»", t.contains(banned))
  }

  @Test
  fun `el Contrarreloj no cuenta para el futuro tope de partidas libres`() {
    assertFalse(TimeTrialDay.COUNTS_FOR_FREE_CAP)
  }
}
