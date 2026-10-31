package com.example.data

import com.example.Features
import com.example.model.AgeRange
import org.junit.Assert.assertEquals
import org.junit.Assert.assertFalse
import org.junit.Assert.assertNotNull
import org.junit.Assert.assertNull
import org.junit.Assert.assertTrue
import org.junit.Test

/**
 * Tu lugar entre personas de tu edad (`Comparison`, Tarea 202; `auditoria-fable/nubi-plus-comparacion.md` §1 y §4): el percentil desde los tramos sin suavizar, la división, la frase en «de cada 100», las reglas de «pocos» en orden, el percentil semana a semana, «otras edades» sin la franja de 13 a 17 años, la franja
 * de la persona y la forma de la campana. Lógica pura.
 */
class ComparisonTest {
  private val base = SampleComparison.BASE

  private fun dist(bins: List<Int>, p25: Int = 0, p50: Int = 0, p75: Int = 0) = Comparison.Distribution(bins.sum(), bins, p25, p50, p75)

  /** La silueta de la lámina escalada a [people] personas. */
  private fun shaped(people: Int) = Comparison.Distribution(people, SampleComparison.fit(base, people), 31, 39, 47)

  /** Una fuente con las personas de cada clase dichas por [people]: (franjas, semanas atrás, cuántas semanas) → cuántas. Anota lo que se pidió. */
  private class Fake(val people: (List<AgeRange>?, Int, Int) -> Int) : Comparison.Source {
    val asked = mutableListOf<Triple<List<AgeRange>?, Int, Int>>()
    override fun distribution(game: String, ranges: List<AgeRange>?, weeksAgo: Int, weeks: Int): Comparison.Distribution {
      asked += Triple(ranges, weeksAgo, weeks)
      val n = people(ranges, weeksAgo, weeks)
      return Comparison.Distribution(n, SampleComparison.fit(SampleComparison.BASE, n), 31, 39, 47)
    }
  }

  // ------------------------------------------------------------------ el percentil

  @Test
  fun `la marca de la lamina de Fable, 43 aciertos entre 128 personas, supera a 63 de cada 100`() {
    val d = shaped(128)
    assertEquals(128, d.people)
    assertEquals("43 cae en el tramo 40 a 44: 72 personas debajo y 3 quintos de las 15 de ese tramo = 81 de 128", 63, Comparison.percentile(43, d))
  }

  @Test
  fun `el percentil cuenta los tramos de abajo enteros y reparte parejo el propio tramo`() {
    val d = dist(listOf(100) + List(15) { 0 })            // las 100 personas hicieron de 0 a 4
    assertEquals("con 0 no se supera a nadie", 0, Comparison.percentile(0, d))
    assertEquals(20, Comparison.percentile(1, d))
    assertEquals(80, Comparison.percentile(4, d))
    assertEquals("5 aciertos: todo el tramo de 0 a 4 queda debajo; nunca 100", 99, Comparison.percentile(5, d))
  }

  @Test
  fun `el percentil se redondea hacia abajo, no pasa de 99 y no baja de 0`() {
    // 1 de 3 personas debajo: 33,33… de cada 100 → 33
    assertEquals(33, Comparison.percentile(5, dist(listOf(1, 2) + List(14) { 0 })))
    val top = dist(List(15) { 0 } + listOf(10))
    assertEquals("todas en el último tramo: nadie queda debajo", 0, Comparison.percentile(80, top))
    assertEquals("una sola persona, muy por debajo: nunca 100", 99, Comparison.percentile(100, dist(listOf(1) + List(15) { 0 })))
  }

  @Test
  fun `el ultimo tramo, 75 o mas, no se parte y cuenta solo a los de abajo`() {
    val d = shaped(128)
    val below = d.bins.take(15).sum()
    val expected = (100 * below / d.people)
    assertEquals(expected, Comparison.percentile(75, d))
    assertEquals(Comparison.percentile(75, d), Comparison.percentile(80, d))
    assertEquals(Comparison.percentile(75, d), Comparison.percentile(150, d))
  }

  @Test
  fun `el percentil nunca baja cuando sube la marca`() {
    for (people in listOf(50, 128, 211)) {
      val d = shaped(people)
      var last = -1
      for (mark in 0..100) { val p = Comparison.percentile(mark, d); assertTrue("con $mark: $p < $last", p >= last); last = p }
    }
  }

  @Test
  fun `sin personas el percentil es 0 y una distribucion que no suma no se arma`() {
    assertEquals(0, Comparison.percentile(10, Comparison.Distribution(0, List(Comparison.BINS) { 0 }, 0, 0, 0)))
    val bad = runCatching { Comparison.Distribution(10, List(Comparison.BINS) { 0 }, 0, 0, 0) }
    assertTrue("los tramos deben sumar las personas", bad.isFailure)
    assertTrue("y deben ser 16", runCatching { Comparison.Distribution(1, listOf(1), 0, 0, 0) }.isFailure)
  }

  // ------------------------------------------------------------------ la división

  @Test
  fun `las cinco divisiones con nombre cambian en 25, 50, 75 y 90`() {
    val expect = mapOf(0 to "En la plataforma", 24 to "En la plataforma", 25 to "Despegando", 49 to "Despegando", 50 to "En órbita", 63 to "En órbita", 74 to "En órbita", 75 to "En la Luna", 89 to "En la Luna", 90 to "Entre estrellas", 99 to "Entre estrellas")
    for ((p, name) in expect) assertEquals("percentil $p", name, Comparison.division(p).label)
  }

  // ------------------------------------------------------------------ las franjas vecinas

  @Test
  fun `las franjas vecinas son 30 a 54, 55 a 74 y 65 o mas, y la de 18 a 29 queda sola sin los menores`() {
    assertFalse("la comparación de menores sigue apagada hasta el abogado", Features.COMPARE_MINORS)
    assertEquals(listOf(AgeRange.R30_44, AgeRange.R45_54), Comparison.neighborGroup(AgeRange.R30_44))
    assertEquals(listOf(AgeRange.R30_44, AgeRange.R45_54), Comparison.neighborGroup(AgeRange.R45_54))
    assertEquals(listOf(AgeRange.R55_64, AgeRange.R65_74), Comparison.neighborGroup(AgeRange.R55_64))
    assertEquals("la de 65 a 74 se junta con la de 55 a 64 (la primera pareja que la trae)", listOf(AgeRange.R55_64, AgeRange.R65_74), Comparison.neighborGroup(AgeRange.R65_74))
    assertEquals(listOf(AgeRange.R65_74, AgeRange.R75_PLUS), Comparison.neighborGroup(AgeRange.R75_PLUS))
    assertNull("sin la franja de 13 a 17 años, la de 18 a 29 no tiene con quién juntarse", Comparison.neighborGroup(AgeRange.R18_29))
    assertNull(Comparison.neighborGroup(AgeRange.R13_17))
    assertEquals("30 a 54 años", Comparison.groupLabel(listOf(AgeRange.R30_44, AgeRange.R45_54)))
    assertEquals("55 a 74 años", Comparison.groupLabel(listOf(AgeRange.R55_64, AgeRange.R65_74)))
    assertEquals("65 años o más", Comparison.groupLabel(listOf(AgeRange.R65_74, AgeRange.R75_PLUS)))
  }

  // ------------------------------------------------------------------ las reglas de «pocos» (§4), en orden

  @Test
  fun `con 50 personas o mas en la franja esta semana se usa lo normal y no se pide nada mas`() {
    val src = Fake { _, _, _ -> 128 }
    val place = Comparison.place(src, "stroop", AgeRange.R45_54, 43)
    val r = (place as Comparison.Place.Found).ranked
    assertEquals(Comparison.Scope.OWN_WEEK, r.scope)
    assertEquals(63, r.percentile)
    assertEquals(Comparison.Division.ORBIT, r.division)
    assertEquals("solo se pidió la franja esta semana", 1, src.asked.size)
  }

  @Test
  fun `con menos de 50 esta semana se prueban las ultimas 4 semanas de la franja, luego las franjas vecinas, luego todas las edades`() {
    // paso 2
    var r = (Comparison.place(Fake { rs, _, w -> if (rs?.size == 1 && w == 4) 80 else 23 }, "stroop", AgeRange.R45_54, 43) as Comparison.Place.Found).ranked
    assertEquals(Comparison.Scope.OWN_4_WEEKS, r.scope)
    // paso 3
    r = (Comparison.place(Fake { rs, _, w -> if (rs?.size == 2 && w == 4) 70 else 23 }, "stroop", AgeRange.R45_54, 43) as Comparison.Place.Found).ranked
    assertEquals(Comparison.Scope.NEIGHBORS_4_WEEKS, r.scope)
    assertEquals(listOf(AgeRange.R30_44, AgeRange.R45_54), r.ranges)
  }

  @Test
  fun `si ni las vecinas alcanzan sale Todavia somos pocos con la cuenta real y, mientras tanto, todas las edades si llegan a 50`() {
    val world = Fake { rs, _, w -> if (rs == null && w == 4) 212 else 23 }
    val few = Comparison.place(world, "stroop", AgeRange.R45_54, 43) as Comparison.Place.Few
    assertEquals(23, few.own)
    assertEquals(Comparison.Scope.ALL_4_WEEKS, few.meanwhile?.scope)
    assertNull("todas las edades no tiene franjas", few.meanwhile?.ranges)
    val nothing = Comparison.place(Fake { _, _, _ -> 10 }, "stroop", AgeRange.R45_54, 43) as Comparison.Place.Few
    assertEquals(10, nothing.own)
    assertNull("con todas las edades también faltan: no hay «mientras tanto»", nothing.meanwhile)
  }

  @Test
  fun `Mientras tanto usa todas las edades de ESTA semana si llegan a 50 y solo si no, las ultimas 4 semanas`() {
    val thisWeek = Comparison.place(Fake { rs, _, _ -> if (rs == null) 212 else 23 }, "stroop", AgeRange.R45_54, 43) as Comparison.Place.Few
    assertEquals(Comparison.Scope.ALL_WEEK, thisWeek.meanwhile?.scope)
    val fourWeeks = Comparison.place(Fake { rs, _, w -> if (rs == null && w == 4) 212 else 23 }, "stroop", AgeRange.R45_54, 43) as Comparison.Place.Few
    assertEquals("esta semana no llegan a 50: las últimas 4 semanas", Comparison.Scope.ALL_4_WEEKS, fourWeeks.meanwhile?.scope)
    val none = Comparison.place(Fake { _, _, _ -> 23 }, "stroop", AgeRange.R45_54, 43) as Comparison.Place.Few
    assertNull(none.meanwhile)
  }

  @Test
  fun `el numero de Mientras tanto y el de la fila Todas las edades coinciden cuando hablan de la misma semana y nunca se muestran dos numeros del mismo grupo`() {
    // «pocos»: todas las edades de esta semana llegan a 50 → el mismo número en «Mientras tanto» y en la fila
    val few = SampleComparison(SampleComparison.World.FEW)
    val place = Comparison.place(few, "stroop", AgeRange.R45_54, 43) as Comparison.Place.Few
    val others = Comparison.others(few, "stroop", AgeRange.R45_54, 43)
    assertEquals(Comparison.Scope.ALL_WEEK, place.meanwhile?.scope)
    assertNotNull(others.all.percentile)
    assertEquals("el mismo número en las dos frases", place.meanwhile?.percentile, others.all.percentile)
    assertTrue("y en las palabras: «supera a N de cada 100» y «N de cada 100»", Comparison.meanwhileSentence(place.meanwhile!!).contains("supera a ${others.all.percentile} de cada 100 personas.") && others.all.value == "${others.all.percentile} de cada 100")
    // «muy pocos»: esta semana no llegan a 50 → «Mientras tanto» usa 4 semanas, lo dice, y la fila dice «todavía somos pocos»: ningún número repetido ni distinto
    val veryFew = SampleComparison(SampleComparison.World.VERY_FEW)
    val place2 = Comparison.place(veryFew, "stroop", AgeRange.R45_54, 43) as Comparison.Place.Few
    val others2 = Comparison.others(veryFew, "stroop", AgeRange.R45_54, 43)
    assertEquals(Comparison.Scope.ALL_4_WEEKS, place2.meanwhile?.scope)
    assertTrue(Comparison.meanwhileSentence(place2.meanwhile!!).contains("de las últimas 4 semanas"))
    assertNull("y la fila de todas las edades de esta semana no da número", others2.all.percentile)
    assertEquals("todavía somos pocos", others2.all.value)
  }

  @Test
  fun `las dos versiones de Mientras tanto dicen de que semanas son`() {
    val d = shaped(212)
    val week = Comparison.Ranked(43, 53, Comparison.division(53), Comparison.Scope.ALL_WEEK, null, d)
    val four = Comparison.Ranked(43, 53, Comparison.division(53), Comparison.Scope.ALL_4_WEEKS, null, d)
    assertEquals("Mientras tanto, comparada con todas las edades de esta semana, tu mejor marca, 43 aciertos, supera a 53 de cada 100 personas.", Comparison.meanwhileSentence(week))
    assertEquals("Mientras tanto, comparada con todas las edades de las últimas 4 semanas, tu mejor marca de esta semana, 43 aciertos, supera a 53 de cada 100 personas.", Comparison.meanwhileSentence(four))
    assertEquals("Tu mejor marca de esta semana, 43 aciertos, supera a 53 de cada 100 personas de todas las edades que jugaron Tinta o Palabra esta semana.", Comparison.mainSentence(week, AgeRange.R45_54, "Tinta o Palabra"))
  }

  @Test
  fun `la franja de 18 a 29 no tiene vecina y pasa directo a todas las edades`() {
    val src = Fake { rs, _, w -> if (rs == null && w == 4) 300 else 20 }
    val few = Comparison.place(src, "stroop", AgeRange.R18_29, 43) as Comparison.Place.Few
    assertNotNull(few.meanwhile)
    assertTrue("no se pidió ninguna pareja de franjas", src.asked.none { (rs, _, _) -> rs != null && rs.size > 1 })
  }

  @Test
  fun `sin marca esta semana no hay con que compararse y no se pide nada`() {
    val src = Fake { _, _, _ -> 128 }
    assertEquals(Comparison.Place.NoMark, Comparison.place(src, "stroop", AgeRange.R45_54, null))
    assertTrue(src.asked.isEmpty())
  }

  // ------------------------------------------------------------------ las frases, tal cual la lámina

  private fun ranked(scope: Comparison.Scope, people: Int = 128, ranges: List<AgeRange>? = listOf(AgeRange.R45_54), mark: Int = 43): Comparison.Ranked {
    val d = shaped(people)
    return Comparison.Ranked(mark, Comparison.percentile(mark, d), Comparison.division(Comparison.percentile(mark, d)), scope, ranges, d)
  }

  @Test
  fun `la frase principal es la de la lamina y dice franja, juego y semana`() {
    val r = ranked(Comparison.Scope.OWN_WEEK)
    assertEquals("Tu mejor marca de esta semana, 43 aciertos, supera a 63 de cada 100 personas de 45 a 54 años que jugaron Tinta o Palabra esta semana.", Comparison.mainSentence(r, AgeRange.R45_54, "Tinta o Palabra"))
    assertEquals("Es el percentil 63: la división En órbita.", Comparison.percentileLine(r))
    assertEquals("La mitad de las personas hizo 39 aciertos o menos; la cuarta parte más alta, 47 o más. La línea marca la mitad.", Comparison.quartilesSentence(r.distribution))
    assertEquals("Hecha con las marcas de las 128 personas tal como son, no con una curva teórica.", Comparison.madeOfSentence(r.distribution))
    assertNull("la clase normal no necesita aclarar nada más", Comparison.scopeLine(r))
  }

  @Test
  fun `cada clase de referencia se dice con sus palabras, las 4 semanas, las dos franjas juntas y todas las edades`() {
    val four = ranked(Comparison.Scope.OWN_4_WEEKS)
    assertEquals("Tu mejor marca de esta semana, 43 aciertos, supera a ${four.percentile} de cada 100 personas de 45 a 54 años que jugaron Tinta o Palabra en las últimas 4 semanas.", Comparison.mainSentence(four, AgeRange.R45_54, "Tinta o Palabra"))
    val merged = ranked(Comparison.Scope.NEIGHBORS_4_WEEKS, ranges = listOf(AgeRange.R30_44, AgeRange.R45_54))
    assertEquals("Tu mejor marca de esta semana, 43 aciertos, supera a ${merged.percentile} de cada 100 personas de 30 a 54 años que jugaron Tinta o Palabra en las últimas 4 semanas.", Comparison.mainSentence(merged, AgeRange.R45_54, "Tinta o Palabra"))
    assertEquals("Juntamos dos franjas porque todavía somos pocos.", Comparison.scopeLine(merged))
    val all = ranked(Comparison.Scope.ALL_4_WEEKS, ranges = null)
    assertEquals("Tu mejor marca de esta semana, 43 aciertos, supera a ${all.percentile} de cada 100 personas de todas las edades que jugaron Tinta o Palabra en las últimas 4 semanas.", Comparison.mainSentence(all, AgeRange.R45_54, "Tinta o Palabra"))
    assertEquals("Mientras tanto, comparada con todas las edades de las últimas 4 semanas, tu mejor marca de esta semana, 43 aciertos, supera a ${all.percentile} de cada 100 personas.", Comparison.meanwhileSentence(all))
  }

  @Test
  fun `Todavia somos pocos dice la cuenta real en singular o plural y la barra va hasta 50`() {
    assertEquals("Todavía somos pocos: 23 personas de 45 a 54 años jugaron Tinta o Palabra esta semana. Cuando sean 50, verás aquí tu lugar entre ellas.", Comparison.fewSentence(Comparison.Place.Few(23, null), AgeRange.R45_54, "Tinta o Palabra"))
    assertEquals("Todavía somos pocos: 1 persona de 75 años o más jugó Rescate relámpago esta semana. Cuando sean 50, verás aquí tu lugar entre ellas.", Comparison.fewSentence(Comparison.Place.Few(1, null), AgeRange.R75_PLUS, "Rescate relámpago"))
    assertEquals("23 de 50 personas", Comparison.fewBarText(23))
    assertEquals("Todavía no tienes una marca esta semana en Acoplamiento. Juega su Contrarreloj para ver tu lugar entre personas de tu edad.", Comparison.noMarkSentence("Acoplamiento"))
  }

  @Test
  fun `una franja de 75 o mas se dice con sus palabras, y el 0 y el 99 se dicen igual sin culpa`() {
    val r = ranked(Comparison.Scope.OWN_WEEK, mark = 0)
    assertEquals("Tu mejor marca de esta semana, 0 aciertos, supera a 0 de cada 100 personas de 75 años o más que jugaron Tinta o Palabra esta semana.", Comparison.mainSentence(r, AgeRange.R75_PLUS, "Tinta o Palabra"))
    assertTrue(Comparison.mainSentence(ranked(Comparison.Scope.OWN_WEEK, mark = 90), AgeRange.R45_54, "Tinta o Palabra").contains("supera a 99 de cada 100"))
    assertEquals("En la plataforma", r.division.label)
  }

  // ------------------------------------------------------------------ el percentil de las últimas 7 semanas

  @Test
  fun `solo entran las semanas con marca propia y con 50 personas o mas, de la mas vieja a esta`() {
    val src = Fake { _, ago, _ -> if (ago == 3) 20 else 128 }
    val bests = listOf(43, 41, null, 35, 44, null, 30)          // semanas atrás 0..6
    val points = Comparison.weekPoints(src, "stroop", AgeRange.R45_54, bests)
    assertEquals("sin la 2 y la 5 (sin marca) ni la 3 (menos de 50 personas)", listOf(6, 4, 1, 0), points.map { it.weeksAgo })
    assertEquals(listOf(30, 44, 41, 43), points.map { it.mark })
    assertEquals("la de esta semana es la de la lámina", 63, points.last().percentile)
  }

  @Test
  fun `la frase de las semanas cuenta las que hay y dice todas, ninguna o una sola`() {
    fun pts(vararg p: Int) = p.mapIndexed { i, v -> Comparison.WeekPoint(i, 30, v) }
    assertEquals("En 6 de las 7 semanas estuviste por encima de la mitad de tu franja.", Comparison.weeksSentence(pts(52, 58, 49, 61, 66, 55, 63)))
    assertEquals("En las 3 semanas estuviste por encima de la mitad de tu franja.", Comparison.weeksSentence(pts(50, 60, 70)))
    assertEquals("En ninguna de las 2 semanas estuviste por encima de la mitad de tu franja.", Comparison.weeksSentence(pts(10, 49)))
    assertEquals("Tu única semana con marca estuvo por encima de la mitad de tu franja.", Comparison.weeksSentence(pts(80)))
    assertEquals("Tu única semana con marca estuvo en la mitad de abajo de tu franja.", Comparison.weeksSentence(pts(12)))
    assertNull("sin semanas con dato no hay frase ni losa", Comparison.weeksSentence(emptyList()))
    assertEquals(
      "Tu percentil en las últimas 7 semanas: hace 6 semanas, supera a 52 de cada 100; hace 1 semana, supera a 58 de cada 100; esta semana, supera a 63 de cada 100.",
      Comparison.weeksSpoken(listOf(Comparison.WeekPoint(6, 30, 52), Comparison.WeekPoint(1, 30, 58), Comparison.WeekPoint(0, 43, 63)))
    )
  }

  // ------------------------------------------------------------------ comparado con otras edades

  @Test
  fun `otras edades tiene seis franjas sin la de 13 a 17 anos, la propia en negrita con su marca y todas las edades aparte`() {
    val src = Fake { rs, _, _ -> when { rs == null -> 400; rs.singleOrNull() == AgeRange.R75_PLUS -> 38; rs.singleOrNull() == AgeRange.R45_54 -> 128; else -> 120 } }
    val o = Comparison.others(src, "stroop", AgeRange.R45_54, 43)
    assertEquals(listOf("18 a 29 años", "30 a 44 años", "45 a 54 años (tu franja)", "55 a 64 años", "65 a 74 años", "75 años o más"), o.rows.map { it.label })
    assertEquals(listOf(false, false, true, false, false, false), o.rows.map { it.own })
    assertNull("la de 75 o más tiene 38 personas: todavía somos pocos", o.rows.last().percentile)
    assertEquals("todavía somos pocos", o.rows.last().value)
    assertTrue(o.rows.dropLast(1).all { it.percentile != null })
    assertEquals("la de la lámina: 63 de cada 100", "63 de cada 100", o.rows[2].value)
    assertEquals("Todas las edades", o.all.label)
    assertNotNull(o.all.percentile)
    assertEquals("Tu mejor marca de esta semana, 43 aciertos, supera a tantas de cada 100 personas que jugaron Tinta o Palabra esta semana:", Comparison.othersLead(43, "Tinta o Palabra"))
    assertEquals("45 a 54 años (tu franja), 63 de cada 100", Comparison.rowSpoken(Comparison.OtherRow("45 a 54 años (tu franja)", 63, true, 128)))
    assertEquals("De 13 a 17 años no hay comparación por ahora.", Comparison.NO_MINORS)
  }

  // ------------------------------------------------------------------ la franja de la persona y la build

  @Test
  fun `quien tiene de 13 a 17 anos no ve la comparacion y sin franja solo se compara con datos de muestra`() {
    assertNull("de 13 a 17 años: ninguna franja, ni con datos de muestra", Comparison.rangeFor(AgeRange.R13_17, sample = true))
    assertNull(Comparison.rangeFor(AgeRange.R13_17, sample = false))
    assertEquals(AgeRange.R45_54, Comparison.rangeFor(null, sample = true))
    assertNull("sin franja y con datos de verdad no hay con quién compararla", Comparison.rangeFor(null, sample = false))
    for (r in AgeRange.entries.filter { it != AgeRange.R13_17 }) assertEquals(r, Comparison.rangeFor(r, sample = false))
  }

  @Test
  fun `la build de la tienda no tiene comparacion y la de depuracion usa solo datos de muestra`() {
    assertFalse("Features.COMPARE sigue apagada: no hay servidor", Features.COMPARE)
    for (world in SampleComparison.World.entries) {
      assertNull("release: ninguna fuente, la sección no existe ($world)", Comparison.sourceFor(debugBuild = false, world = world))
      assertTrue("depuración: los datos de muestra ($world)", Comparison.sourceFor(debugBuild = true, world = world) is SampleComparison)
    }
  }

  // ------------------------------------------------------------------ la sección entera, con las partidas de la persona

  @Test
  fun `la seccion usa la mejor marca de cada semana del juego con la pista de hoy`() {
    val now = java.util.Calendar.getInstance().apply { set(2026, java.util.Calendar.OCTOBER, 28, 18, 0, 0); set(java.util.Calendar.MILLISECOND, 0) }.timeInMillis
    fun at(day: Int) = java.util.Calendar.getInstance().apply { set(2026, java.util.Calendar.OCTOBER, day, 12, 0, 0); set(java.util.Calendar.MILLISECOND, 0) }.timeInMillis
    fun r(mark: Int, day: Int, game: String = "stroop", version: Int = 2) = TimeTrialResult(id = "r$game$day$mark", gameId = game, day = "2026-10-%02d".format(day), trackVersion = version, hits = mark + 2, errors = 2, level = 4, timestamp = at(day))
    val results = listOf(r(31, 8), r(35, 10), r(39, 13), r(36, 15), r(44, 17), r(38, 19), r(41, 22), r(37, 26), r(42, 27), r(43, 28), r(60, 21, game = "disparate"), r(70, 27, version = 1))
    val s = Comparison.section(SampleComparison(few = false), "stroop", "Tinta o Palabra", AgeRange.R45_54, results, now)
    assertEquals("43, la mejor de esta semana (lunes 26 a hoy); la pista 1 y otro juego no cuentan", 43, s.mark)
    assertEquals(63, (s.place as Comparison.Place.Found).ranked.percentile)
    assertEquals("las semanas con marca: hace 3 (35), 2 (44), 1 (41) y esta (43)", listOf(3, 2, 1, 0), s.weeks.map { it.weeksAgo })
    assertEquals(listOf(35, 44, 41, 43), s.weeks.map { it.mark })
    assertNotNull(s.others)
    val none = Comparison.section(SampleComparison(few = false), "radar", "Rescate relámpago", AgeRange.R45_54, results, now)
    assertEquals(Comparison.Place.NoMark, none.place)
    assertNull("sin marca esta semana no hay otras edades", none.others)
    assertTrue(none.weeks.isEmpty())
  }

  // ------------------------------------------------------------------ la forma de la campana

  @Test
  fun `suavizar 1-2-1 conserva el total de personas y no mueve el percentil, que sale de los tramos sin suavizar`() {
    val s = Comparison.smoothed(base)
    assertEquals(16, s.size)
    assertEquals(base.sum().toDouble(), s.sum(), 1e-9)
    assertEquals("el tramo del medio pesa la mitad y sus vecinos un cuarto", (base[6] + 2.0 * base[7] + base[8]) / 4, s[7], 1e-9)
    assertEquals("en el borde, el tramo se repite", (base[0] + 2.0 * base[0] + base[1]) / 4, s[0], 1e-9)
    assertEquals("el percentil de la lámina sale de los tramos sin suavizar", 63, Comparison.percentile(43, shaped(128)))
  }

  @Test
  fun `la curva pasa por el centro de cada tramo suavizado y nunca baja de cero`() {
    val s = Comparison.smoothed(base)
    for (i in 0 until 16) assertEquals("el centro del tramo $i", s[i], Comparison.bellHeight(s, i * 5 + 2.5), 1e-9)
    val spiky = Comparison.smoothed(listOf(0, 0, 0, 50, 0, 0, 0, 0, 0, 0, 0, 0, 0, 40, 0, 0))
    var x = 0.0
    while (x <= 80.0) { assertTrue("altura negativa en $x", Comparison.bellHeight(spiky, x) >= 0.0); x += 0.25 }
  }

  @Test
  fun `la barra Tu cae en el centro de su marca y se topa en el borde derecho`() {
    assertEquals(43.5, Comparison.markX(43), 1e-9)
    assertEquals(0.5, Comparison.markX(0), 1e-9)
    assertEquals("75 o más se dibuja dentro del último tramo", 79.5, Comparison.markX(75 + 40), 1e-9)
    assertTrue(Comparison.markX(1000) <= Comparison.TOP_MARK)
    assertEquals("Campana con las marcas de las 128 personas. Tu mejor marca de esta semana, 43 aciertos, está marcada.", Comparison.bellSpoken(ranked(Comparison.Scope.OWN_WEEK)))
  }

  // ------------------------------------------------------------------ lo que se dice

  @Test
  fun `ninguna frase usa las palabras que no se dicen y cada una termina en punto`() {
    val lines = mutableListOf<String>()
    for (people in listOf(50, 128, 300)) for (mark in listOf(0, 7, 43, 90)) for (scope in Comparison.Scope.entries) for (range in AgeRange.entries) {
      val r = ranked(scope, people = people, mark = mark, ranges = if (scope == Comparison.Scope.ALL_4_WEEKS) null else listOf(range))
      lines += listOf(Comparison.mainSentence(r, range, "Tinta o Palabra"), Comparison.percentileLine(r), Comparison.quartilesSentence(r.distribution), Comparison.madeOfSentence(r.distribution), Comparison.meanwhileSentence(r), Comparison.bellSpoken(r))
    }
    for (n in listOf(0, 1, 23, 49)) lines += Comparison.fewSentence(Comparison.Place.Few(n, null), AgeRange.R45_54, "Tinta o Palabra")
    for (k in 1..7) for (p in 0..100 step 25) lines += Comparison.weeksSentence(List(k) { Comparison.WeekPoint(it, 20, p) })!!
    for (line in lines) {
      assertTrue("«$line» termina en punto", line.endsWith("."))
      val low = line.lowercase()
      for (bad in listOf("capacidad", "edad mental", "ranking", "velocidad", "rendimiento", "cerebro", "cognitiv", "entren", "puntaje", "nivel"))
        assertFalse("«$line» dice «$bad»", bad in low)
    }
  }
}
