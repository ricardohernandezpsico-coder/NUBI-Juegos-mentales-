package com.example.data

import com.example.model.AgeRange
import org.junit.Assert.assertEquals
import org.junit.Assert.assertFalse
import org.junit.Assert.assertNotNull
import org.junit.Assert.assertNull
import org.junit.Assert.assertTrue
import org.junit.Test
import java.io.File

/**
 * La REFERENCIA ESTIMADA del Contrarreloj que lee la app (`EstimatedReference`, Tarea 205; `auditoria-fable/referencia-estimada.md`): las tablas de `assets/referencia/` que arma la Tarea 204, el ancho y el número de tramos de cada juego, y que el percentil, la escalera y el eje de la campana
 * salgan de ellos. Lee los archivos REALES del APK (no una copia), así que si se regeneran con otra forma, esta prueba lo dice.
 */
class EstimatedReferenceTest {
  private val dir = File("src/main/assets/${EstimatedReference.ASSET_DIR}")
  private val files: List<File> get() = dir.listFiles { f -> f.extension == "json" }.orEmpty().sortedBy { it.name }
  private val reference: EstimatedReference by lazy { EstimatedReference.of(files.map { it.readText(Charsets.UTF_8) }) }

  /**
   * (ancho del tramo, número de tramos) de cada juego. El ANCHO lo decidió Ricardo el 1-nov: 1 en Rescate relámpago (3 desde la Tarea 208), 2 en Acoplamiento y 5 en los otros dos. El NÚMERO es su consecuencia técnica: 24 de a 1 en Rescate (Tarea 204b) y, desde la Tarea 208 (la marca cuenta por cápsula), 16 de a 3 (0 a 47) y, desde la Tarea 206, 20 en Tinta o Palabra (0 a 99) y en Acoplamiento (0 a 39),
   * para que con las escalas calibradas el último tramo no junte a los jóvenes y el percentil 90 se alcance; ¿Verdad o disparate? se queda en 16.
   */
  private val shapes = mapOf("stroop" to (5 to 20), "disparate" to (5 to 16), "radar" to (3 to 16), "acoplamiento" to (2 to 20))

  /** La escala de cada juego tras la Tarea 206 (`ReferenceScales` en Unity): la que deja la mediana de los 5 percentiles de la primera semana lo más cerca de 50. Rescate relámpago no se calibró. */
  private val calibratedScales = mapOf("stroop" to 0.89, "disparate" to 0.92, "radar" to 1.0, "acoplamiento" to 0.90)

  private val sixRanges = listOf(AgeRange.R18_29, AgeRange.R30_44, AgeRange.R45_54, AgeRange.R55_64, AgeRange.R65_74, AgeRange.R75_PLUS)

  // ------------------------------------------------------------------ los archivos del APK

  @Test
  fun `el APK trae una tabla por juego del Contrarreloj con la version de su pista`() {
    assertEquals("cuatro archivos, uno por juego", TimeTrialDay.GAMES.size, files.size)
    for (game in TimeTrialDay.GAMES) {
      val name = "${game}_v${TimeTrialDay.trackVersion(game)}.json"
      assertTrue("falta $name (la versión de la pista de $game es la ${TimeTrialDay.trackVersion(game)})", files.any { it.name == name })
      assertTrue("la fuente cubre $game", reference.covers(game))
    }
    assertEquals(TimeTrialDay.GAMES.toSet(), reference.loaded.map { it.game }.toSet())
    assertTrue("la versión de cada tabla es la de su pista", reference.loaded.all { it.trackVersion == TimeTrialDay.trackVersion(it.game) })
  }

  @Test
  fun `cada juego trae su ancho de tramo y su numero de tramos, anchos 3 2 5 y 5 y tramos 16 20 16 y 20`() {
    for ((game, shape) in shapes) {
      val table = reference.loaded.single { it.game == game }
      assertEquals("ancho del tramo de $game", shape.first, table.binWidth)
      for ((range, d) in table.ranges) {
        assertEquals("$game $range: tramos", shape.second, d.bins.size)
        assertEquals("$game $range: ancho", shape.first, d.binWidth)
        assertEquals("$game $range: el borde derecho", shape.first * shape.second, d.topMark)
        assertTrue("$game $range: es la referencia estimada", d.estimated)
      }
    }
  }

  @Test
  fun `las seis franjas de 18 en adelante vienen con sus 2000 robots y la de 14 a 17 no tiene tabla propia`() {
    for (table in reference.loaded) {
      assertEquals("${table.game}: las seis franjas", sixRanges.toSet(), table.ranges.keys)
      assertFalse(AgeRange.R14_17 in table.ranges.keys)
      for ((range, d) in table.ranges) {
        assertEquals("${table.game} $range", 2000, d.people)
        assertTrue("alcanza de sobra para decir un lugar", d.enough)
        assertTrue("los cuartiles van en orden", d.p25 <= d.p50 && d.p50 <= d.p75)
      }
    }
  }

  // ------------------------------------------------------------------ 14 a 17 años: la tabla de 18 a 29 (Tarea 216)

  @Test
  fun `14 a 17 devuelve la distribucion de 18 a 29 en los cuatro juegos, sin tabla propia y sin tocar las seis`() {
    assertEquals("la franja compartida es 14 a 17 con la tabla de 18 a 29", AgeRange.R14_17 to AgeRange.R18_29, EstimatedReference.SHARED_RANGE)
    for (r in AgeRange.entries) assertEquals(if (r == AgeRange.R14_17) AgeRange.R18_29 else r, EstimatedReference.tableRangeOf(r))
    for (game in TimeTrialDay.GAMES) {
      val teen = reference.distribution(game, listOf(AgeRange.R14_17), weeksAgo = 0, weeks = 1)
      val young = reference.distribution(game, listOf(AgeRange.R18_29), weeksAgo = 0, weeks = 1)
      assertTrue("$game: 14 a 17 alcanza para decir un lugar", teen.enough && teen.estimated)
      assertEquals("$game: es la MISMA distribución de 18 a 29", young, teen)
      for (ago in 1..6) assertEquals("$game: también las otras semanas (hace $ago)", young, reference.distribution(game, listOf(AgeRange.R14_17), ago, 1))
      for (range in sixRanges) assertEquals("$game $range: las seis tablas de siempre no cambian", reference.loaded.single { it.game == game }.ranges.getValue(range), reference.distribution(game, listOf(range), 0, 1))
    }
  }

  @Test
  fun `la cifra y la campana de una persona de 14 a 17 son las de 18 a 29, juego por juego`() {
    for (game in TimeTrialDay.GAMES) {
      val shape = shapes.getValue(game)
      for (mark in listOf(0, 1, 5, 12, 20, 30, 43, 60, 75, 99)) {
        val teen = Comparison.place(reference, game, AgeRange.R14_17, mark)
        val young = Comparison.place(reference, game, AgeRange.R18_29, mark)
        val ranked = (teen as? Comparison.Place.Found)?.ranked ?: continue
        val other = (young as Comparison.Place.Found).ranked
        // (la franja que se dice, `ranges`, es la de cada persona; lo demás es lo mismo)
        assertEquals("$game con $mark: la marca, el percentil y la clase", listOf(other.mark, other.percentile, other.scope), listOf(ranked.mark, ranked.percentile, ranked.scope))
        assertEquals("$game con $mark: la misma campana", other.distribution, ranked.distribution)
        assertEquals("$game: ${shape.second} tramos", shape.second, ranked.distribution.bins.size)
      }
      assertEquals("$game: las nueve líneas de la liga de 14 a 17 son las de 18 a 29", (1..9).map { Comparison.firstMarkReaching(it * 10, reference.distribution(game, listOf(AgeRange.R18_29), 0, 1)) },
        (1..9).map { Comparison.firstMarkReaching(it * 10, reference.distribution(game, listOf(AgeRange.R14_17), 0, 1)) })
    }
  }

  @Test
  fun `la tabla dice cuantas partidas por semana jugaron los robots y de que modelo salen`() {
    for (table in reference.loaded) {
      assertEquals("${table.game}: 3 partidas por semana (decisión de Ricardo del 1-nov)", 3, table.playedPerWeek)
      assertEquals("${table.game}: la escala calibrada con la primera semana de las 5 personas (Tarea 206; Rescate relámpago queda en 1,0)", calibratedScales.getValue(table.game), table.scale, 0.0)
      assertTrue("${table.game}: dice de qué estudio sale el modelo", table.source.contains("referencia-estimada"))
    }
  }

  // ------------------------------------------------------------------ lo que contesta la fuente

  @Test
  fun `por franja contesta su tabla y es la misma todas las semanas`() {
    for (game in TimeTrialDay.GAMES) for (range in sixRanges) {
      val now = reference.distribution(game, listOf(range), weeksAgo = 0, weeks = 1)
      assertTrue("$game $range", now.enough && now.estimated)
      for (ago in 1..6) assertEquals("no cambia de semana en semana ($game $range, hace $ago)", now, reference.distribution(game, listOf(range), ago, 1))
      assertEquals("tampoco por las últimas 4 semanas", now, reference.distribution(game, listOf(range), 0, 4))
    }
  }

  @Test
  fun `las clases que no son una franja no existen y quedan vacias con la forma del juego`() {
    for ((game, shape) in shapes) {
      for (ranges in listOf(null, listOf(AgeRange.R30_44, AgeRange.R45_54), listOf(AgeRange.R14_17, AgeRange.R18_29))) {
        val d = reference.distribution(game, ranges, 0, 1)
        assertEquals("$game $ranges queda sin gente", 0, d.people)
        assertFalse(d.enough)
        assertEquals(shape.second, d.bins.size)
        assertEquals(shape.first, d.binWidth)
      }
    }
  }

  @Test
  fun `un juego sin tabla o con otra version de la pista queda sin comparacion`() {
    assertFalse("un juego que no es del Contrarreloj", reference.covers("calculo"))
    val old = files.first { it.name.startsWith("stroop_") }.readText(Charsets.UTF_8)
    val bumped = old.replace(""""version_pista": ${TimeTrialDay.trackVersion("stroop")}""", """"version_pista": ${TimeTrialDay.trackVersion("stroop") + 1}""")
    assertTrue("el texto cambió", bumped != old)
    val other = EstimatedReference.of(listOf(bumped))
    assertFalse("la tabla de otra versión no vale para la pista de hoy: ese juego queda sin comparación", other.covers("stroop"))
    assertEquals("y contesta vacío", 0, other.distribution("stroop", listOf(AgeRange.R45_54), 0, 1).people)
    val empty = EstimatedReference.of(emptyList())
    for (game in TimeTrialDay.GAMES) assertFalse("sin la carpeta de assets nadie compara ($game)", empty.covers(game))
  }

  @Test
  fun `una tabla rota no entra`() {
    val ok = files.first { it.name.startsWith("radar_") }.readText(Charsets.UTF_8)
    assertNotNull(EstimatedReference.parse(ok))
    assertNull("no es una referencia estimada", EstimatedReference.parse(ok.replace(""""estimada": true""", """"estimada": false""")))
    assertNull("no es un JSON", EstimatedReference.parse("{ esto no es"))
    assertNull("los robots de una franja no suman su histograma", EstimatedReference.parse(ok.replaceFirst(""""robots": 2000""", """"robots": 1999""")))
    assertNull("otro número de tramos que el que dice", EstimatedReference.parse(ok.replace(""""tramos": 16,""", """"tramos": 15,""")))
    assertNull("un ancho de tramo imposible", EstimatedReference.parse(ok.replace(""""ancho_tramo": 3,""", """"ancho_tramo": 0,""")))
  }

  // ------------------------------------------------------------------ de ellos salen el percentil, la escalera y el eje

  @Test
  fun `el percentil sube parejo con la marca y respeta el ancho de tramo de cada juego`() {
    for (table in reference.loaded) for ((range, d) in table.ranges) {
      var before = 0
      for (mark in 0..d.topMark + 2) {
        val p = Comparison.percentile(mark, d)
        assertTrue("${table.game} $range: el percentil nunca baja ($mark)", p >= before)
        assertTrue(p in 0..99)
        before = p
      }
      assertEquals("${table.game} $range: con 0 aciertos nadie queda detrás", 0, Comparison.percentile(0, d))
    }
    // Rescate cuenta de a 3 aciertos (Tarea 208): la marca 9 de los 18 a 29 años deja detrás justo a los tres primeros tramos (0 a 8), y no al «tramo de 5»
    val radar = reference.distribution("radar", listOf(AgeRange.R18_29), 0, 1)
    val behind = radar.bins.take(3).sum()
    assertEquals(100 * behind / radar.people, Comparison.percentile(9, radar))
    // y en Tinta o Palabra, de a 5: la marca 5 deja detrás justo al tramo 0 a 4
    val stroop = reference.distribution("stroop", listOf(AgeRange.R18_29), 0, 1)
    assertEquals(100 * stroop.bins[0] / stroop.people, Comparison.percentile(5, stroop))
  }

  @Test
  fun `las nueve lineas de la liga caen en marcas distintas en los cuatro juegos y las seis franjas`() {
    // Tarea 229 (`docs/liga-de-la-semana.md` §1; `docs/techo-dificultad.md` §8): las diez divisiones de la liga empiezan en el 0, 10, 20 … 90 % de la referencia; si dos líneas cayeran en la misma marca, subir de una a otra no costaría nada
    for (table in reference.loaded) for ((range, d) in table.ranges) {
      val marks = (1..9).map { Comparison.firstMarkReaching(it * 10, d) }
      assertTrue("${table.game} $range: las nueve líneas se alcanzan dentro de la pista ($marks)", marks.all { it != null })
      assertTrue("${table.game} $range: caen en marcas estrictamente crecientes ($marks)", marks.zipWithNext().all { (a, b) -> a!! < b!! })
      for ((i, m) in marks.withIndex()) {
        val line = (i + 1) * 10
        assertTrue("${table.game} $range: con $m aciertos se llega al $line %", Comparison.percentile(m!!, d) >= line)
        assertTrue("${table.game} $range: con uno menos todavía no", Comparison.percentile(m - 1, d) < line)
      }
    }
  }

  @Test
  fun `el eje de la campana va hasta donde empieza el ultimo tramo de cada juego`() {
    val ticks = shapes.mapValues { (game, _) -> Comparison.axisTicks(reference.distribution(game, listOf(AgeRange.R45_54), 0, 1)) }
    assertEquals("Tinta o Palabra: 20 tramos de 5, 0 · 32 · 63 · 95", listOf(0, 32, 63, 95), ticks["stroop"])
    assertEquals("¿Verdad o disparate?: 0 · 25 · 50 · 75", listOf(0, 25, 50, 75), ticks["disparate"])
    assertEquals("Rescate relámpago: 16 tramos de 3, 0 · 15 · 30 · 45", listOf(0, 15, 30, 45), ticks["radar"])
    assertEquals("Acoplamiento: 20 tramos de 2, 0 · 13 · 25 · 38", listOf(0, 13, 25, 38), ticks["acoplamiento"])
    val radar = reference.distribution("radar", listOf(AgeRange.R45_54), 0, 1)
    assertEquals("la marca dibujada se topa en el centro del último tramo de SU juego", 47.5, Comparison.markX(60, radar.topMark), 0.0)
    assertEquals(0.5, Comparison.markX(0, radar.topMark), 0.0)
  }

  @Test
  fun `el ultimo tramo no se parte aunque sea de a mas de un acierto`() {
    val radar = reference.distribution("radar", listOf(AgeRange.R18_29), 0, 1)
    assertEquals("Rescate empieza su último tramo en 45", 45, radar.lastStart)
    val below = 100 * radar.bins.take(radar.bins.lastIndex).sum() / radar.people
    assertEquals("de 45 en adelante cuenta solo a los de abajo", minOf(99, below), Comparison.percentile(45, radar))
    assertEquals(Comparison.percentile(45, radar), Comparison.percentile(60, radar))
  }

  @Test
  fun `la mitad y los cuartiles salen de los tramos con la regla del percentil y no de los campos del JSON`() {
    for (table in reference.loaded) for ((range, d) in table.ranges) {
      val label = "${table.game} $range"
      assertEquals("$label: la mitad es el primer acierto que llega al percentil 50", Comparison.markAtPercentile(50, d), d.p50)
      assertEquals("$label: el cuartil bajo", Comparison.markAtPercentile(25, d), d.p25)
      assertEquals("$label: el cuartil alto", Comparison.markAtPercentile(75, d), d.p75)
      assertTrue("$label: en orden", d.p25 <= d.p50 && d.p50 <= d.p75)
      assertTrue("$label: con la mitad ya se llega a 50", Comparison.percentile(d.p50, d) >= 50)
      if (d.p50 > 0) assertTrue("$label: con un acierto menos todavía no", Comparison.percentile(d.p50 - 1, d) < 50)
    }
    // y nada de eso depende de lo que diga el JSON: con p25, p50, p75 y p90 en cero sale la misma tabla
    for (file in files) {
      val text = file.readText(Charsets.UTF_8)
      val blanked = Regex(""""p(25|50|75|90)": \d+""").replace(text) { m -> """"p${m.groupValues[1]}": 0""" }
      assertTrue("la prueba cambió algo en ${file.name}", blanked != text)
      assertEquals("${file.name}: los campos p* del JSON no se leen", EstimatedReference.parse(text)!!.ranges, EstimatedReference.parse(blanked)!!.ranges)
    }
  }

  @Test
  fun `lo que depende de cuantos lee estimated y no la cuenta de robots`() {
    val few = Comparison.Distribution(10, List(16) { if (it == 3) 10 else 0 }, 15, 15, 15)
    val manyReal = Comparison.Distribution(50, List(16) { if (it == 3) 50 else 0 }, 15, 15, 15)
    val estimated = Comparison.Distribution(10, List(16) { if (it == 3) 10 else 0 }, 15, 15, 15, estimated = true)
    assertFalse("10 personas reales no alcanzan", few.enough)
    assertTrue("50 personas reales sí", manyReal.enough)
    assertTrue("la referencia estimada siempre alcanza, sin mirar su cuenta", estimated.enough)
    assertFalse("una clase vacía no alcanza", Comparison.Distribution.empty().enough)
    // la tabla entra solo con robots de verdad (50 o más): un archivo con 30 no es una referencia
    fun json(robots: Int, a: Int, b: Int) = """{"estimada": true, "juego": "stroop", "version_pista": 2, "ancho_tramo": 5, "tramos": 2, "franjas": {"R18_29": {"robots": $robots, "tramos": [$a, $b], "p25": 0, "p50": 0, "p75": 0}}}"""
    assertNull("30 robots no alcanzan para una tabla", EstimatedReference.parse(json(30, 10, 20)))
    assertNotNull("60 robots sí", EstimatedReference.parse(json(60, 20, 40)))
    // en la tabla de verdad la cuenta de robots no sale en ninguna parte de lo que ve la persona: OtherRow.people queda en 0
    val source = Comparison.sourceFor(debugBuild = false, world = SampleComparison.World.MANY, estimated = reference)!!
    for ((game, shape) in shapes) Comparison.others(source, game, AgeRange.R45_54, shape.first * shape.second / 4)!!.rows.forEach { assertEquals("$game: la fila no lleva la cuenta de robots", 0, it.people) }
  }

  // ------------------------------------------------------------------ la sección entera con la referencia

  @Test
  fun `con la referencia estimada la persona tiene su lugar y sus otras edades sin Todas las edades ni franjas escondidas`() {
    val source = Comparison.sourceFor(debugBuild = false, world = SampleComparison.World.MANY, estimated = reference)!!
    assertEquals(Comparison.Kind.ESTIMATED, Comparison.kindOf(source))
    for ((game, shape) in shapes) {
      val range = AgeRange.R45_54
      val mark = shape.first * shape.second / 4
      val place = Comparison.place(source, game, range, mark)
      assertTrue("$game: la franja propia siempre alcanza (nunca «Todavía somos pocos»)", place is Comparison.Place.Found)
      val ranked = (place as Comparison.Place.Found).ranked
      assertEquals(Comparison.Scope.OWN_WEEK, ranked.scope)
      assertTrue(ranked.estimated)
      assertEquals("sin marca esta semana, sin lugar", Comparison.Place.NoMark, Comparison.place(source, game, range, null))
      val others = Comparison.others(source, game, range, mark)!!
      assertNull("$game: sin fila «Todas las edades»", others.all)
      assertEquals("las seis franjas, cada una con su fila", 6, others.rows.size)
      assertFalse("y sin la línea que dice que faltan", others.hidden)
      assertTrue(others.rows.single { it.own }.label.endsWith("(tu franja)"))
      assertEquals("la semana pasada y las anteriores, la misma tabla", 7, Comparison.weekPoints(source, game, range, List(Comparison.WEEKS) { mark }).size)
    }
  }
}
