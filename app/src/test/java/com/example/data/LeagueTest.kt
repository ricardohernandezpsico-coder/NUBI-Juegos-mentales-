package com.example.data

import com.example.model.AgeRange
import org.junit.Assert.assertEquals
import org.junit.Assert.assertFalse
import org.junit.Assert.assertNotNull
import org.junit.Assert.assertNull
import org.junit.Assert.assertTrue
import org.junit.Test

/**
 * La liga de la semana (`League`, Tarea 229; `docs/liga-de-la-semana.md` §2 a §4 y §7): el % de la semana (el promedio redondeado hacia abajo de lo que supera la mejor marca de cada juego que cuenta), subir / quedarse / bajar de a una, cerrar las semanas pendientes en orden, lo cerrado que nunca se recalcula,
 * guardar y leer, y los textos de las diez divisiones. Lógica pura.
 */
class LeagueTest {
  /** Una referencia en que el percentil de una marca es la marca misma (100 personas, una por acierto, tramos de 1): así la prueba dice qué % saca cada juego sin armar tablas. */
  private object Linear : Comparison.Source {
    override fun distribution(game: String, ranges: List<AgeRange>?, weeksAgo: Int, weeks: Int) =
      Comparison.Distribution(100, List(100) { 1 }, 25, 50, 75, binWidth = 1, estimated = true)
  }

  private val range = AgeRange.R45_54
  private val now: Long = java.util.Calendar.getInstance().apply { clear(); set(2026, java.util.Calendar.NOVEMBER, 4, 12, 0) }.timeInMillis        // un miércoles
  private fun weekAt(ago: Int, hours: Long = 5) = Week.weekStart(now, ago) + hours * 3_600_000L
  private fun r(game: String, mark: Int, at: Long, track: Int = TimeTrialDay.trackVersion(game)) =
    TimeTrialResult(gameId = game, day = "2026-11-04", trackVersion = track, hits = mark, errors = 0, level = 1, timestamp = at)

  /** Tres partidas viejas del juego (para que cuente: desde la tercera) y una de esta semana con [mark]. */
  private fun played(game: String, mark: Int, at: Long = weekAt(0)): List<TimeTrialResult> =
    listOf(r(game, 1, weekAt(5)), r(game, 2, weekAt(5, 7)), r(game, 3, weekAt(5, 9)), r(game, mark, at))

  // ------------------------------------------------------------------ tu semana

  @Test
  fun `el promedio se redondea hacia abajo, como el percentil, y el ejemplo del video da 51`() {
    val games = listOf(94, 19, 42).mapIndexed { i, p -> League.GameWeek("g$i", p, p, League.Status.COUNTS) }
    assertEquals("94 + 19 + 42 = 155 ÷ 3 = 51,67: nunca 52", 51, League.weekPercent(games))
    assertEquals(0, League.weekPercent(listOf(League.GameWeek("a", 3, 0, League.Status.COUNTS), League.GameWeek("b", 3, 1, League.Status.COUNTS))))
    assertEquals("promedio exacto", 50, League.weekPercent(listOf(League.GameWeek("a", 1, 40, League.Status.COUNTS), League.GameWeek("b", 1, 60, League.Status.COUNTS))))
  }

  @Test
  fun `con las partidas reales cada juego saca el percentil de su mejor marca y el promedio los junta`() {
    val results = played("stroop", 94) + played("disparate", 19) + played("acoplamiento", 42)
    val games = League.weekGames(Linear, range, results, now)
    assertEquals(listOf(94, 19, null, 42), games.map { it.percent })
    assertEquals(League.Status.NO_PLAYS, games.first { it.game == "radar" }.status)
    assertEquals("el juego no jugado no cuenta ni baja el promedio", 51, League.weekPercent(games))
  }

  @Test
  fun `un juego con menos de tres partidas guardadas no cuenta (EARLY) y la semana queda sin porcentaje`() {
    val results = listOf(r("stroop", 80, weekAt(0)), r("stroop", 70, weekAt(0, 7)))
    val g = League.gameWeek(Linear, range, results, "stroop", now, 0)
    assertEquals(League.Status.EARLY, g.status)
    assertEquals("la marca se muestra, el porcentaje no", 80, g.best)
    assertNull(g.percent)
    assertNull(League.weekPercent(League.weekGames(Linear, range, results, now)))
  }

  @Test
  fun `la pista vieja no cuenta`() {
    val old = listOf(r("stroop", 90, weekAt(0), track = 1), r("stroop", 90, weekAt(0, 7), track = 1), r("stroop", 90, weekAt(0, 9), track = 1))
    val g = League.gameWeek(Linear, range, old, "stroop", now, 0)
    assertEquals("ninguna de la pista de hoy: sin partidas esta semana", League.Status.NO_PLAYS, g.status)
  }

  @Test
  fun `cuenta la mejor marca de la semana, no la ultima`() {
    val results = played("stroop", 70, at = weekAt(0, 30)) + r("stroop", 40, weekAt(0, 80))
    assertEquals("la última partida fue de 40 pero la mejor fue de 70", 70, League.gameWeek(Linear, range, results, "stroop", now, 0).percent)
  }

  @Test
  fun `una semana vieja se juzga con las partidas que habia hasta su fin`() {
    // dos partidas hasta la semana pasada y la tercera ESTA semana: la semana pasada todavía no podía comparar
    val results = listOf(r("stroop", 60, weekAt(1)), r("stroop", 61, weekAt(1, 40)), r("stroop", 62, weekAt(0)))
    assertEquals(League.Status.EARLY, League.gameWeek(Linear, range, results, "stroop", now, 1).status)
    val thisWeek = League.gameWeek(Linear, range, results, "stroop", now, 0)
    assertEquals("esta semana ya son tres partidas", League.Status.COUNTS, thisWeek.status)
    assertEquals(62, thisWeek.percent)
  }

  @Test
  fun `sin franja de edad o sin referencia no hay liga y el juego no cuenta`() {
    val results = played("stroop", 50)
    assertEquals(League.Status.NO_COMPARE, League.gameWeek(Linear, null, results, "stroop", now, 0).status)
    assertEquals(League.Status.NO_COMPARE, League.gameWeek(null, range, results, "stroop", now, 0).status)
    assertNull(League.weekPercent(League.weekGames(null, range, results, now)))
  }

  // ------------------------------------------------------------------ subir, quedarse, bajar

  @Test
  fun `exactamente en la linea de arriba sube, exactamente en la propia se queda`() {
    assertEquals(Comparison.Division.CLOUDS, League.next(Comparison.Division.TAKEOFF, 20))
    assertEquals(Comparison.Division.TAKEOFF, League.next(Comparison.Division.TAKEOFF, 19))
    assertEquals("en la línea de la suya se queda", Comparison.Division.TAKEOFF, League.next(Comparison.Division.TAKEOFF, 10))
    assertEquals("un punto menos baja", Comparison.Division.PLATFORM, League.next(Comparison.Division.TAKEOFF, 9))
  }

  @Test
  fun `En la plataforma no baja y la mas alta, con 90 o mas, se queda`() {
    assertEquals(Comparison.Division.PLATFORM, League.next(Comparison.Division.PLATFORM, 0))
    assertEquals(Comparison.Division.PLATFORM, League.next(Comparison.Division.PLATFORM, 9))
    assertEquals(Comparison.Division.PLATFORM, League.next(Comparison.Division.PLATFORM, null))
    assertEquals(Comparison.Division.GALAXY, League.next(Comparison.Division.GALAXY, 99))
    assertEquals(Comparison.Division.GALAXY, League.next(Comparison.Division.GALAXY, 90))
    assertEquals("con menos de 90 baja una", Comparison.Division.STARS, League.next(Comparison.Division.GALAXY, 89))
  }

  @Test
  fun `se sube y se baja de a una aunque el porcentaje alcance para tres`() {
    assertEquals(Comparison.Division.TAKEOFF, League.next(Comparison.Division.PLATFORM, 95))
    assertEquals("un 95 % desde En órbita sube solo a En la Luna", Comparison.Division.MOON, League.next(Comparison.Division.ORBIT, 95))
    assertEquals("un 0 % desde En Marte baja solo a En la Luna", Comparison.Division.MOON, League.next(Comparison.Division.MARS, 0))
  }

  @Test
  fun `una semana sin porcentaje baja una`() {
    assertEquals(Comparison.Division.TAKEOFF, League.next(Comparison.Division.CLOUDS, null))
    assertEquals(Comparison.Division.STARS, League.next(Comparison.Division.GALAXY, null))
  }

  @Test
  fun `las diez divisiones tienen sus lineas de 10 en 10 y la division de un porcentaje es la mas alta que alcanza`() {
    assertEquals((0..9).map { it * 10 }, Comparison.Division.entries.map { it.from })
    assertEquals(10, Comparison.Division.entries.size)
    assertEquals(Comparison.Division.PLATFORM, Comparison.division(9))
    assertEquals(Comparison.Division.TAKEOFF, Comparison.division(10))
    assertEquals(Comparison.Division.GALAXY, Comparison.division(99))
  }

  // ------------------------------------------------------------------ empezar y cerrar

  @Test
  fun `la liga empieza la semana en que se abre y esa semana ya cuenta`() {
    val s = League.begin(League.State(), now)
    assertEquals(Week.weekStart(now), s.startWeek)
    assertEquals("todos parten En la plataforma, también quienes ya tenían partidas", Comparison.Division.PLATFORM, s.division)
    assertTrue(s.firstWeek)
    assertTrue("la semana de inicio es la actual: nada pendiente", League.pendingWeeks(s, now).isEmpty())
    assertEquals("begin no pisa el inicio", s, League.begin(s, now + 20 * 86_400_000L))
  }

  @Test
  fun `se cierran en orden todas las semanas pendientes, cada una con sus partidas o sin porcentaje`() {
    val start = League.begin(League.State(), weekAt(3))                    // empezó hace 3 semanas
    val pending = League.pendingWeeks(start, now)
    assertEquals(listOf(3, 2, 1), pending.map { it.second })
    assertEquals("de la más vieja a la más nueva", pending.map { it.first }.sorted(), pending.map { it.first })
    val percents = mapOf(3 to 25, 2 to null, 1 to 40)                        // sube, baja (sin %), sube
    val closed = League.close(start, now) { _, ago -> percents[ago] }
    assertEquals(3, closed.weeks.size)
    assertEquals("la primera sale de En la plataforma", Comparison.Division.PLATFORM, closed.weeks.first().from)
    assertEquals(listOf(Comparison.Division.TAKEOFF, Comparison.Division.PLATFORM, Comparison.Division.TAKEOFF), closed.weeks.map { it.to })
    assertEquals(listOf(25, null, 40), closed.weeks.map { it.percent })
    assertEquals("la división de ahora es la «terminó» de la última", Comparison.Division.TAKEOFF, closed.division)
    assertTrue("ya no queda nada pendiente", League.pendingWeeks(closed, now).isEmpty())
    assertFalse(closed.firstWeek)
  }

  @Test
  fun `lo cerrado nunca se recalcula`() {
    val start = League.begin(League.State(), weekAt(1))
    val closed = League.close(start, now) { _, _ -> 35 }
    var asked = 0
    val again = League.close(closed, now) { _, _ -> asked++; 0 }
    assertEquals("no se pidió ningún porcentaje: nada pendiente", 0, asked)
    assertEquals(closed, again)
    // aunque un día después cambie todo, la semana cerrada queda con su 35
    assertEquals(35, again.weeks.single().percent)
  }

  @Test
  fun `sin partidas durante tres semanas se baja de a una por semana y En la plataforma no baja mas`() {
    val high = League.State(startWeek = Week.weekStart(now, 6), weeks = listOf(League.Closed(Week.weekStart(now, 4), 70, Comparison.Division.JUPITER, Comparison.Division.JUPITER)))
    // la última cerrada es la de hace 4 semanas; faltan las de hace 3, 2 y 1
    val closed = League.close(high, now) { _, _ -> null }
    assertEquals("de En Júpiter baja de a una por semana", listOf(Comparison.Division.ASTEROIDS, Comparison.Division.MARS, Comparison.Division.MOON), closed.weeks.drop(1).map { it.to })
    val low = League.close(League.begin(League.State(), weekAt(2)), now) { _, _ -> null }
    assertEquals(listOf(Comparison.Division.PLATFORM, Comparison.Division.PLATFORM), low.weeks.map { it.to })
  }

  @Test
  fun `sin inicio de liga no hay nada que cerrar`() {
    assertTrue(League.pendingWeeks(League.State(), now).isEmpty())
    assertEquals(League.State(), League.close(League.State(), now) { _, _ -> 99 })
  }

  @Test
  fun `el cierre de punta a punta con partidas reales`() {
    val results = played("stroop", 60, at = weekAt(1)) + played("disparate", 40, at = weekAt(1))
    val start = League.begin(League.State(), weekAt(1))
    val closed = League.close(start, now) { _, ago -> League.closedPercent(Linear, range, results, now, ago) }
    assertEquals("(60 + 40) ÷ 2 = 50 → sube una, a Despegando", 50, closed.weeks.single().percent)
    assertEquals(Comparison.Division.TAKEOFF, closed.division)
  }

  // ------------------------------------------------------------------ cómo va ahora

  @Test
  fun `la semana en marcha dice si subiria, se quedaria o bajaria`() {
    fun live(d: Comparison.Division, percent: Int?): League.Live {
      val weeks = if (d == Comparison.Division.PLATFORM) emptyList() else listOf(League.Closed(weekAt(1), 50, Comparison.Division.PLATFORM, d))
      val games = if (percent == null) emptyList() else listOf(League.GameWeek("stroop", percent, percent, League.Status.COUNTS))
      return League.live(League.State(weekAt(2), weeks), games)
    }
    assertEquals(League.Course.UP, live(Comparison.Division.TAKEOFF, 22).course)
    assertEquals(Comparison.Division.CLOUDS, live(Comparison.Division.TAKEOFF, 22).target)
    assertEquals(League.Course.STAY, live(Comparison.Division.TAKEOFF, 15).course)
    assertEquals(League.Course.DOWN, live(Comparison.Division.TAKEOFF, 4).course)
    assertEquals(League.Course.NONE, live(Comparison.Division.TAKEOFF, null).course)
    assertEquals(League.Course.TOP, live(Comparison.Division.GALAXY, 95).course)
    assertEquals("la más alta con menos de 90 bajaría", League.Course.DOWN, live(Comparison.Division.GALAXY, 85).course)
  }

  // ------------------------------------------------------------------ guardar y leer

  @Test
  fun `lo guardado se lee igual y un texto roto da una liga sin empezar`() {
    val s = League.State(
      startWeek = weekAt(4), seenWeek = weekAt(1),
      weeks = listOf(League.Closed(weekAt(3), 51, Comparison.Division.PLATFORM, Comparison.Division.TAKEOFF), League.Closed(weekAt(2), null, Comparison.Division.TAKEOFF, Comparison.Division.PLATFORM))
    )
    assertEquals(s, League.decode(League.encode(s)))
    assertEquals(League.State(), League.decode(League.encode(League.State())))
    for (bad in listOf(null, "", "basura", "v1|1|2", "v1|x|-|1,2,3,4", "v2|-|-|", "v1|-|-|5,7,NOPE,PLATFORM")) assertEquals("«$bad» no revienta", League.State(), League.decode(bad))
  }

  @Test
  fun `la hoja del lunes es la de la ultima semana cerrada y se ve una sola vez`() {
    val a = League.Closed(weekAt(3), 10, Comparison.Division.PLATFORM, Comparison.Division.TAKEOFF)
    val b = League.Closed(weekAt(2), 5, Comparison.Division.TAKEOFF, Comparison.Division.PLATFORM)
    val s = League.State(startWeek = weekAt(3), weeks = listOf(a, b))
    assertEquals("habla solo de la última", b, s.pendingSheet)
    val seen = League.seen(s, b.weekStart)
    assertNull(seen.pendingSheet)
    assertNull("sin semanas cerradas no hay hoja", League.State(startWeek = weekAt(0)).pendingSheet)
  }

  // ------------------------------------------------------------------ los textos

  private fun allPhrases(): List<String> = buildList {
    for (d in Comparison.Division.entries) {
      val up = d.up
      for (course in League.Course.entries) {
        val target = when (course) { League.Course.UP -> up ?: d; League.Course.DOWN -> d.down ?: d; else -> d }
        for (first in listOf(false, true)) {
          val live = League.Live(d, if (course == League.Course.NONE) null else d.from + 3, course, target, first)
          add(League.mondaySentence(live)); add(League.hoyLine(live))
          add(League.mondaySentence(live.copy(playedButEarly = true, percent = null, course = League.Course.NONE)))
        }
      }
      add(League.tuLine(d)); add(Comparison.divisionPlaceLine(d)); add(Comparison.divisionPlace(d)); add("Tu división: ${Comparison.divisionRank(d)}")
      for (move in listOf(League.Move.UP, League.Move.STAY, League.Move.DOWN)) for (p in listOf(null, d.from, 99)) {
        val to = when (move) { League.Move.UP -> up ?: d; League.Move.DOWN -> d.down ?: d; else -> d }
        val s = League.sheet(League.Closed(0L, p, d, to))
        add(s.title); add(s.last); s.extra?.let { add(it) }; add(s.now)
      }
    }
    for (n in 0..6) add(League.closesLine(n))
    add(League.averageSentence(51)); add(League.averageSentence(51, estimated = false))
  }

  @Test
  fun `ninguna frase dice en En, percentil ni ranking, y todas las cifras llevan el porcentaje pegado`() {
    for (p in allPhrases()) {
      assertFalse("«$p» dice «en En»", Regex("\\ben (En|Entre|Sobre)\\b").containsMatchIn(p))
      assertFalse("«$p» dice «percentil»", p.contains("percentil", ignoreCase = true))
      assertFalse("«$p» dice «ranking»", p.contains("ranking", ignoreCase = true))
      assertFalse("«$p» dice «bajaste» o «perdiste»", p.contains("bajaste", ignoreCase = true) || p.contains("perdiste", ignoreCase = true))
      assertFalse("«$p» deja un número separado de su %", Regex("\\d %").containsMatchIn(p.glueNumbers()))
    }
  }

  @Test
  fun `las preposiciones van pegadas al nombre en las diez divisiones`() {
    val expected = mapOf(
      Comparison.Division.PLATFORM to "En la plataforma", Comparison.Division.TAKEOFF to "en Despegando", Comparison.Division.CLOUDS to "Sobre las nubes", Comparison.Division.ORBIT to "En órbita",
      Comparison.Division.MOON to "En la Luna", Comparison.Division.MARS to "En Marte", Comparison.Division.ASTEROIDS to "Entre asteroides", Comparison.Division.JUPITER to "En Júpiter",
      Comparison.Division.STARS to "Entre estrellas", Comparison.Division.GALAXY to "En otra galaxia"
    )
    for ((d, text) in expected) assertEquals(text, Comparison.placeIn(d))
  }

  @Test
  fun `los lugares se dicen de diez`() {
    assertEquals("la primera de diez divisiones", Comparison.divisionPlace(Comparison.Division.PLATFORM))
    assertEquals("la segunda de diez divisiones", Comparison.divisionPlace(Comparison.Division.TAKEOFF))
    assertEquals("la novena de diez divisiones", Comparison.divisionPlace(Comparison.Division.STARS))
    assertEquals("la décima y más alta de las diez divisiones", Comparison.divisionPlace(Comparison.Division.GALAXY))
    assertEquals("La tercera de diez divisiones.", Comparison.divisionPlaceLine(Comparison.Division.CLOUDS))
    assertEquals("la segunda de diez.", Comparison.divisionRank(Comparison.Division.TAKEOFF))
    assertEquals("la décima y más alta.", Comparison.divisionRank(Comparison.Division.GALAXY))
  }

  @Test
  fun `la frase del lunes de la losa dice lo de la especificacion`() {
    fun live(d: Comparison.Division, course: League.Course, percent: Int?, target: Comparison.Division = d, first: Boolean = false) = League.Live(d, percent, course, target, first)
    val takeoff = Comparison.Division.TAKEOFF
    assertEquals("Si la semana terminara hoy, el lunes subirías a Sobre las nubes.", League.mondaySentence(live(takeoff, League.Course.UP, 25, Comparison.Division.CLOUDS)))
    assertEquals("Si la semana terminara hoy, te quedarías en Despegando. Para subir a Sobre las nubes, te falta llegar al 20 %.", League.mondaySentence(live(takeoff, League.Course.STAY, 14)))
    assertEquals("Si la semana terminara hoy, bajarías a En la plataforma. Para quedarte, te falta llegar al 10 %.", League.mondaySentence(live(takeoff, League.Course.DOWN, 4, Comparison.Division.PLATFORM)))
    assertEquals("Todavía no juegas esta semana. Con cualquiera de los cuatro juegos puedes quedarte en Despegando o subir.", League.mondaySentence(live(takeoff, League.Course.NONE, null)))
    assertEquals("Todavía no juegas esta semana. Con cualquiera de los cuatro juegos puedes subir a Despegando.", League.mondaySentence(live(Comparison.Division.PLATFORM, League.Course.NONE, null)))
    assertEquals("Si la semana terminara hoy, te quedarías En otra galaxia, la división más alta.", League.mondaySentence(live(Comparison.Division.GALAXY, League.Course.TOP, 95)))
    assertEquals("Tu primera semana: todos parten En la plataforma.", League.mondaySentence(live(Comparison.Division.PLATFORM, League.Course.NONE, null, first = true)))
    assertEquals("Tu primera semana con marcas que todavía no cuentan no dice que no juega", true, !League.mondaySentence(live(Comparison.Division.PLATFORM, League.Course.NONE, null, first = true).copy(playedButEarly = true)).startsWith("Tu primera semana"))
  }

  @Test
  fun `la linea de Hoy dice lo de la especificacion`() {
    fun line(d: Comparison.Division, course: League.Course, target: Comparison.Division = d) = League.hoyLine(League.Live(d, if (course == League.Course.NONE) null else 50, course, target, false))
    val takeoff = Comparison.Division.TAKEOFF
    assertEquals("Despegando · vas para subir a Sobre las nubes", line(takeoff, League.Course.UP, Comparison.Division.CLOUDS))
    assertEquals("Despegando · vas para quedarte", line(takeoff, League.Course.STAY))
    assertEquals("Despegando · juega para quedarte", line(takeoff, League.Course.DOWN, Comparison.Division.PLATFORM))
    assertEquals("Despegando · juega para quedarte", line(takeoff, League.Course.NONE))
    assertEquals("En la plataforma · juega para subir", line(Comparison.Division.PLATFORM, League.Course.NONE))
    assertEquals("En otra galaxia · vas para quedarte", line(Comparison.Division.GALAXY, League.Course.TOP))
  }

  @Test
  fun `la hoja del lunes dice lo de la especificacion en sus casos`() {
    val up = League.sheet(League.Closed(0L, 51, Comparison.Division.TAKEOFF, Comparison.Division.CLOUDS))
    assertEquals("¡Subiste a Sobre las nubes!", up.title)
    assertEquals("Tu semana pasada: superaste en promedio al 51 % de la referencia estimada para tu edad.", up.last)
    assertEquals("Alcanzaba para En Marte, pero se sube de a una división por semana.", up.extra)
    assertEquals("Esta semana: con el 30 % subes a En órbita; con el 20 %, te quedas Sobre las nubes.", up.now)
    val stay = League.sheet(League.Closed(0L, 12, Comparison.Division.TAKEOFF, Comparison.Division.TAKEOFF))
    assertEquals("Sigues en Despegando", stay.title)
    assertNull(stay.extra)
    assertEquals("Esta semana: con el 20 % subes a Sobre las nubes; con el 10 %, te quedas en Despegando.", stay.now)
    val down = League.sheet(League.Closed(0L, 4, Comparison.Division.TAKEOFF, Comparison.Division.PLATFORM))
    assertEquals("Esta semana estás En la plataforma", down.title)
    assertEquals("Esta semana: con el 10 % subes a Despegando.", down.now)
    val none = League.sheet(League.Closed(0L, null, Comparison.Division.CLOUDS, Comparison.Division.TAKEOFF))
    assertEquals("Tu semana pasada: no jugaste el Contrarreloj.", none.last)
    assertEquals("Esta semana estás en Despegando", none.title)
    assertNotNull(League.sheetSpoken(none))
    assertEquals("Esta semana: con el 90 %, te quedas En otra galaxia.", League.sheet(League.Closed(0L, 95, Comparison.Division.GALAXY, Comparison.Division.GALAXY)).now)
  }

  @Test
  fun `cuantos dias quedan y como se dice`() {
    // el miércoles quedan 4 días (jueves, viernes, sábado y domingo)
    assertEquals(4, League.daysLeft(now))
    assertEquals(6, League.daysLeft(Week.weekStart(now) + 3_600_000L))
    assertEquals("La semana cierra el domingo. Quedan 4 días.", League.closesLine(4))
    assertEquals("La semana cierra el domingo. Queda 1 día.", League.closesLine(1))
    assertEquals("La semana cierra hoy a medianoche.", League.closesLine(0))
  }

  @Test
  fun `los cuadros de los juegos dicen su marca, su porcentaje o por que no`() {
    assertEquals("81 aciertos", League.tileMark(League.GameWeek("stroop", 81, 94, League.Status.COUNTS)))
    assertEquals("Superas al 94 %", League.tilePercent(League.GameWeek("stroop", 81, 94, League.Status.COUNTS)))
    assertEquals("Sin partidas esta semana", League.tileMark(League.GameWeek("stroop", null, null, League.Status.NO_PLAYS)))
    assertNull(League.tilePercent(League.GameWeek("stroop", null, null, League.Status.NO_PLAYS)))
    assertEquals("Cuenta desde tu tercera partida", League.tilePercent(League.GameWeek("stroop", 30, null, League.Status.EARLY)))
    assertNull("sin comparación solo la marca", League.tilePercent(League.GameWeek("stroop", 30, null, League.Status.NO_COMPARE)))
    assertEquals("Tinta o Palabra, hoy, 81 aciertos, superas al 94 %, elegido", League.tileSpoken("Tinta o Palabra", League.GameWeek("stroop", 81, 94, League.Status.COUNTS), isToday = true, selected = true, locked = false))
  }

  @Test
  fun `las semanas seguidas usan la mitad de arriba y no el nombre de ninguna division`() {
    val pts = listOf(Comparison.WeekPoint(2, 30, 60), Comparison.WeekPoint(1, 30, 50), Comparison.WeekPoint(0, 30, 70))
    assertEquals(3, Comparison.streak(pts))
    assertEquals("Llevas 3 semanas seguidas en la mitad de arriba de la referencia estimada.", Comparison.streakSentence(3))
    assertEquals("Esta semana empiezas una racha en la mitad de arriba de la referencia estimada.", Comparison.streakSentence(1))
    assertNull(Comparison.streakSentence(0))
    assertEquals("una semana en 49 corta la cuenta", 1, Comparison.streak(listOf(Comparison.WeekPoint(1, 30, 49), Comparison.WeekPoint(0, 30, 70))))
    for (d in Comparison.Division.entries) assertFalse(Comparison.streakSentence(3)!!.contains(d.label))
  }
}
