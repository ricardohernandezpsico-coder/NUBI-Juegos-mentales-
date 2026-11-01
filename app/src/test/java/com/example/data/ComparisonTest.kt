package com.example.data

import com.example.Features
import com.example.model.AgeRange
import org.junit.Assert.assertEquals
import org.junit.Assert.assertFalse
import org.junit.Assert.assertNotEquals
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
    val others = Comparison.others(few, "stroop", AgeRange.R45_54, 43)!!
    assertEquals(Comparison.Scope.ALL_WEEK, place.meanwhile?.scope)
    assertNotNull(others.all)
    assertEquals("el mismo número en las dos frases", place.meanwhile?.percentile, others.all!!.percentile)
    assertTrue("y en las palabras: «deja detrás a N de cada 100» y «N de cada 100»", Comparison.meanwhileSentence(place.meanwhile!!).contains("deja detrás a ${others.all!!.percentile} de cada 100 personas.") && others.all!!.value == "${others.all!!.percentile} de cada 100")
    // «muy pocos»: esta semana no llegan a 50 → «Mientras tanto» usa 4 semanas y lo dice; la fila de todas las edades de esta semana NO existe (ni queda ninguna otra fila: la losa no va)
    val veryFew = SampleComparison(SampleComparison.World.VERY_FEW)
    val place2 = Comparison.place(veryFew, "stroop", AgeRange.R45_54, 43) as Comparison.Place.Few
    assertEquals(Comparison.Scope.ALL_4_WEEKS, place2.meanwhile?.scope)
    assertTrue(Comparison.meanwhileSentence(place2.meanwhile!!).contains("de las últimas 4 semanas"))
    assertNull("y de todas las edades de esta semana no hay número que mostrar: sin filas, sin losa", Comparison.others(veryFew, "stroop", AgeRange.R45_54, 43))
  }

  @Test
  fun `las dos versiones de Mientras tanto dicen de que semanas son`() {
    val d = shaped(212)
    val week = Comparison.Ranked(43, 53, Comparison.division(53), Comparison.Scope.ALL_WEEK, null, d)
    val four = Comparison.Ranked(43, 53, Comparison.division(53), Comparison.Scope.ALL_4_WEEKS, null, d)
    assertEquals("Mientras tanto, comparada con todas las edades de esta semana, tu mejor marca, 43 aciertos, deja detrás a 53 de cada 100 personas.", Comparison.meanwhileSentence(week))
    assertEquals("Mientras tanto, comparada con todas las edades de las últimas 4 semanas, tu mejor marca de esta semana, 43 aciertos, deja detrás a 53 de cada 100 personas.", Comparison.meanwhileSentence(four))
    assertEquals("personas de todas las edades quedan detrás de tu mejor marca de esta semana en Tinta o Palabra.", Comparison.heroCaption(week, AgeRange.R45_54, "Tinta o Palabra"))
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
  fun `la cifra protagonista lleva su unidad y su frase dice franja, juego y semana`() {
    val r = ranked(Comparison.Scope.OWN_WEEK)
    assertEquals("63 de cada 100", Comparison.heroFigure(r))
    assertEquals("personas de 45 a 54 años quedan detrás de tu mejor marca de esta semana en Tinta o Palabra.", Comparison.heroCaption(r, AgeRange.R45_54, "Tinta o Palabra"))
    assertEquals("con letra muy grande la unidad pasa a la frase", "de cada 100 personas de 45 a 54 años quedan detrás de tu mejor marca de esta semana en Tinta o Palabra.", Comparison.heroCaption(r, AgeRange.R45_54, "Tinta o Palabra", withUnit = true))
    assertEquals("La tercera de cinco divisiones.", Comparison.divisionPlaceLine(r.division))
    assertEquals(
      "63 de cada 100 personas de 45 a 54 años quedan detrás de tu mejor marca de esta semana en Tinta o Palabra. En órbita. La tercera de cinco divisiones.",
      Comparison.heroSpoken(r, AgeRange.R45_54, "Tinta o Palabra")
    )
    assertFalse("Tarea 209: «percentil» ya no se dice, tampoco al lector de pantalla", Comparison.heroSpoken(r, AgeRange.R45_54, "Tinta o Palabra").lowercase().contains("percentil"))
    assertNull("la clase normal no necesita aclarar nada más", Comparison.scopeLine(r))
  }

  @Test
  fun `cada clase de referencia se dice con sus palabras, las 4 semanas, las dos franjas juntas y todas las edades`() {
    val four = ranked(Comparison.Scope.OWN_4_WEEKS)
    assertEquals("personas de 45 a 54 años que jugaron Tinta o Palabra en las últimas 4 semanas quedan detrás de tu mejor marca de esta semana.", Comparison.heroCaption(four, AgeRange.R45_54, "Tinta o Palabra"))
    val merged = ranked(Comparison.Scope.NEIGHBORS_4_WEEKS, ranges = listOf(AgeRange.R30_44, AgeRange.R45_54))
    assertEquals("personas de 30 a 54 años que jugaron Tinta o Palabra en las últimas 4 semanas quedan detrás de tu mejor marca de esta semana.", Comparison.heroCaption(merged, AgeRange.R45_54, "Tinta o Palabra"))
    assertEquals("Juntamos dos franjas porque todavía somos pocos.", Comparison.scopeLine(merged))
    val all = ranked(Comparison.Scope.ALL_4_WEEKS, ranges = null)
    assertEquals("personas de todas las edades que jugaron Tinta o Palabra en las últimas 4 semanas quedan detrás de tu mejor marca de esta semana.", Comparison.heroCaption(all, AgeRange.R45_54, "Tinta o Palabra"))
    assertEquals("Mientras tanto, comparada con todas las edades de las últimas 4 semanas, tu mejor marca de esta semana, 43 aciertos, deja detrás a ${all.percentile} de cada 100 personas.", Comparison.meanwhileSentence(all))
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
    assertEquals("0 de cada 100", Comparison.heroFigure(r))
    assertEquals("personas de 75 años o más quedan detrás de tu mejor marca de esta semana en Tinta o Palabra.", Comparison.heroCaption(r, AgeRange.R75_PLUS, "Tinta o Palabra"))
    assertEquals("99 de cada 100", Comparison.heroFigure(ranked(Comparison.Scope.OWN_WEEK, mark = 90)))
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
      "Semana a semana: hace 6 semanas, deja detrás a 52 de cada 100; hace 1 semana, deja detrás a 58 de cada 100; esta semana, deja detrás a 63 de cada 100.",
      Comparison.weeksSpoken(listOf(Comparison.WeekPoint(6, 30, 52), Comparison.WeekPoint(1, 30, 58), Comparison.WeekPoint(0, 43, 63)))
    )
  }

  // ------------------------------------------------------------------ comparado con otras edades

  @Test
  fun `otras edades muestra solo las franjas con 50 personas o mas, la propia con su marca y todas las edades aparte`() {
    val src = Fake { rs, _, _ -> when { rs == null -> 400; rs.singleOrNull() == AgeRange.R75_PLUS -> 38; rs.singleOrNull() == AgeRange.R45_54 -> 128; else -> 120 } }
    val o = Comparison.others(src, "stroop", AgeRange.R45_54, 43)!!
    assertEquals("la de 75 o más tiene 38 personas: no hay fila (nada de «todavía somos pocos»)", listOf("18 a 29 años", "30 a 44 años", "45 a 54 años (tu franja)", "55 a 64 años", "65 a 74 años"), o.rows.map { it.label })
    assertEquals(listOf(false, false, true, false, false), o.rows.map { it.own })
    assertTrue("y avisa que falta alguna: la losa cierra con una línea", o.hidden)
    assertEquals("la de la lámina: 63 de cada 100", "63 de cada 100", o.rows[2].value)
    assertEquals("Todas las edades", o.all!!.label)
    assertEquals("Las demás edades aparecen cuando lleguen a 50 personas.", Comparison.OTHERS_HIDDEN)
    assertEquals("De cada 100 personas que jugaron Tinta o Palabra esta semana, tu mejor marca deja detrás a:", Comparison.othersLead("Tinta o Palabra"))
    assertEquals("45 a 54 años (tu franja), 63 de cada 100", Comparison.rowSpoken(Comparison.OtherRow("45 a 54 años (tu franja)", 63, true, 128)))
    assertEquals("Otras edades", Comparison.OTHERS_TITLE)
  }

  @Test
  fun `con todas las clases en 50 o mas no hay linea que cierre y sin ninguna la losa no va`() {
    val all = Comparison.others(Fake { _, _, _ -> 128 }, "stroop", AgeRange.R45_54, 43)!!
    assertEquals("las seis franjas (sin la de 13 a 17 años)", 6, all.rows.size)
    assertFalse("nada que ocultar", all.hidden)
    assertNotNull(all.all)
    val none = Comparison.others(Fake { _, _, _ -> 20 }, "stroop", AgeRange.R45_54, 43)
    assertNull("sin ninguna clase con 50 personas, no hay losa", none)
  }

  @Test
  fun `solo todas las edades basta para mostrar la losa, con su linea que cierra y sin raya entre filas`() {
    val src = Fake { rs, _, _ -> if (rs == null) 90 else 12 }
    val o = Comparison.others(src, "stroop", AgeRange.R45_54, 43)!!
    assertTrue(o.rows.isEmpty())
    assertNotNull(o.all)
    assertTrue(o.hidden)
  }

  @Test
  fun `la franja propia sin 50 personas tampoco tiene fila`() {
    val src = Fake { rs, _, _ -> if (rs?.singleOrNull() == AgeRange.R45_54) 23 else 120 }
    val o = Comparison.others(src, "stroop", AgeRange.R45_54, 43)!!
    assertTrue("ninguna fila propia: «(tu franja)» no se dice cuando no hay con qué", o.rows.none { it.own })
    assertTrue(o.hidden)
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
  fun `sin la referencia estimada la tienda no tiene comparacion y la depuracion usa solo datos de muestra`() {
    assertFalse("Features.COMPARE sigue apagada: no hay servidor", Features.COMPARE)
    for (world in SampleComparison.World.entries) {
      assertNull("release sin tablas: ninguna fuente, la sección no existe ($world)", Comparison.sourceFor(debugBuild = false, world = world))
      if (world == SampleComparison.World.REFERENCE) assertNull("depuración con «referencia» y sin tablas: tampoco", Comparison.sourceFor(debugBuild = true, world = world))
      else assertTrue("depuración: los datos de muestra ($world)", Comparison.sourceFor(debugBuild = true, world = world) is SampleComparison)
    }
  }

  @Test
  fun `con la referencia estimada la tienda SI compara y la depuracion de fabrica ve lo mismo que la tienda`() {
    val est = Comparison.Distribution(2000, List(16) { if (it == 8) 2000 else 0 }, 40, 40, 40, binWidth = 5, estimated = true)
    val fake = object : Comparison.Source {
      override fun distribution(game: String, ranges: List<AgeRange>?, weeksAgo: Int, weeks: Int) = est
    }
    for (world in SampleComparison.World.entries) {
      val store = Comparison.sourceFor(debugBuild = false, world = world, estimated = fake)
      assertNotNull("la tienda compara con la referencia, sea cual sea el mundo de depuración ($world)", store)
      assertEquals(Comparison.Kind.ESTIMATED, Comparison.kindOf(store))
    }
    assertEquals("de fábrica la depuración es la tienda", Comparison.Kind.ESTIMATED, Comparison.kindOf(Comparison.sourceFor(true, SampleComparison.World.REFERENCE, fake)))
    assertEquals("los otros mundos siguen siendo la muestra, con su línea", Comparison.Kind.PEOPLE, Comparison.kindOf(Comparison.sourceFor(true, SampleComparison.World.MANY, fake)))
    assertEquals(Comparison.Kind.NONE, Comparison.kindOf(null as Comparison.Source?))
    assertEquals("el primer mundo de la lista es la referencia: el de fábrica", SampleComparison.World.REFERENCE, SampleComparison.World.entries.first())
  }

  /**
   * Tarea 206: con la escala calibrada los robots son más rápidos y, si el último tramo («N o más») junta demasiado, el percentil 90 no se alcanza y «Entre estrellas» se queda sin peldaño (pasó con 16 tramos en Tinta y Acoplamiento).
   * Las tablas REALES del APK tienen que dar los cinco peldaños en las seis franjas de los cuatro juegos; Rescate relámpago entra desde la Tarea 208 (la marca por cápsula separa a las personas y vuelve a dar los cinco peldaños; antes tenía cuatro en 18 a 54 años). Si una escala futura vuelve a perderlo, falla AQUÍ y no en una lámina.
   */
  @Test
  fun `las tablas del APK dan los cinco peldaños en las seis franjas de los cuatro juegos`() {
    val reference = realReference
    val all = Comparison.Division.entries
    for (game in listOf("stroop", "disparate", "radar", "acoplamiento")) {
      val table = reference.loaded.single { it.game == game }
      assertEquals("$game: las seis franjas de 18 en adelante", 6, table.ranges.size)
      for ((range, d) in table.ranges) {
        val rungs = Comparison.thresholds(d)
        assertEquals("$game $range: los cinco peldaños, de abajo hacia arriba (hoy ${rungs.map { it.division.label }})", all, rungs.map { it.division })
        assertTrue("$game $range: cada peldaño cae dentro de la pista", rungs.all { it.from < d.topMark })
        assertTrue("$game $range: el último tramo junta el 5 % o menos (hoy ${100 * d.bins.last() / d.people} %), así que el percentil 90 se alcanza con margen", d.bins.last() * 20 <= d.people)
      }
    }
  }

  @Test
  fun `la fuente compuesta usa la real con 50 personas o mas y la estimada si no`() {
    fun d(n: Int, estimated: Boolean) = Comparison.Distribution(n, List(16) { if (it == 3) n else 0 }, 15, 15, 15, estimated = estimated)
    val est = object : Comparison.Source { override fun distribution(game: String, ranges: List<AgeRange>?, weeksAgo: Int, weeks: Int) = d(2000, true) }
    fun real(n: Int) = object : Comparison.Source { override fun distribution(game: String, ranges: List<AgeRange>?, weeksAgo: Int, weeks: Int) = d(n, false) }
    assertFalse("con 50 personas reales manda la real", Comparison.ComposedSource(real(50), est).distribution("stroop", listOf(AgeRange.R45_54), 0, 1).estimated)
    assertTrue("con 49 sigue la referencia", Comparison.ComposedSource(real(49), est).distribution("stroop", listOf(AgeRange.R45_54), 0, 1).estimated)
    assertTrue("sin servidor (hoy) manda la estimada", Comparison.ComposedSource(null, est).distribution("stroop", listOf(AgeRange.R45_54), 0, 1).estimated)
    assertEquals("sin ninguna fuente queda vacía", 0, Comparison.ComposedSource(null, null).distribution("stroop", null, 0, 1).people)
    assertTrue("cubre un juego si lo cubre alguna", Comparison.ComposedSource(null, est).covers("stroop"))
    assertFalse("y no lo cubre si ninguna lo cubre", Comparison.ComposedSource(null, null).covers("stroop"))
  }

  // ------------------------------------------------------------------ la sección entera, con las partidas de la persona

  @Test
  fun `la seccion usa la mejor marca de cada semana del juego con la pista de hoy`() {
    val now = java.util.Calendar.getInstance().apply { set(2026, java.util.Calendar.OCTOBER, 28, 18, 0, 0); set(java.util.Calendar.MILLISECOND, 0) }.timeInMillis
    fun at(day: Int) = java.util.Calendar.getInstance().apply { set(2026, java.util.Calendar.OCTOBER, day, 12, 0, 0); set(java.util.Calendar.MILLISECOND, 0) }.timeInMillis
    fun r(mark: Int, day: Int, game: String = "stroop", version: Int = TimeTrialDay.trackVersion(game)) = TimeTrialResult(id = "r$game$day$mark", gameId = game, day = "2026-10-%02d".format(day), trackVersion = version, hits = mark + 2, errors = 2, level = 4, timestamp = at(day))
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
      val l = Comparison.ladder(r)
      lines += listOf(
        Comparison.heroCaption(r, range, "Tinta o Palabra"), Comparison.heroCaption(r, range, "Tinta o Palabra", withUnit = true), Comparison.heroSpoken(r, range, "Tinta o Palabra"),
        Comparison.needSentence(l, r), Comparison.ladderNote(r, range), Comparison.meanwhileSentence(r), Comparison.bellSpoken(r)
      )
      Comparison.freeSentence(Comparison.Place.Found(r))?.let { lines += it }
    }
    for (n in 0..9) Comparison.streakSentence(n)?.let { lines += it }
    for (n in listOf(0, 1, 23, 49)) lines += Comparison.fewSentence(Comparison.Place.Few(n, null), AgeRange.R45_54, "Tinta o Palabra")
    for (k in 1..7) for (p in 0..100 step 25) lines += Comparison.weeksSentence(List(k) { Comparison.WeekPoint(it, 20, p) })!!
    for (line in lines) {
      assertTrue("«$line» termina en punto", line.endsWith("."))
      val low = line.lowercase()
      for (bad in listOf("capacidad", "edad mental", "ranking", "velocidad", "rendimiento", "cerebro", "cognitiv", "entren", "puntaje", "nivel"))
        assertFalse("«$line» dice «$bad»", bad in low)
    }
  }

  @Test
  fun `con la referencia estimada ninguna frase dice personas, termina en punto y no usa las palabras que no se dicen`() {
    val lines = mutableListOf<String>()
    val d = Comparison.Distribution(2000, SampleComparison.fit(base, 2000), 31, 39, 47, estimated = true)
    for (mark in listOf(0, 7, 43, 90)) for (range in AgeRange.entries) {
      val r = Comparison.Ranked(mark, Comparison.percentile(mark, d), Comparison.division(Comparison.percentile(mark, d)), Comparison.Scope.OWN_WEEK, listOf(range), d)
      val l = Comparison.ladder(r)
      lines += listOf(
        Comparison.heroCaption(r, range, "Tinta o Palabra"), Comparison.heroCaption(r, range, "Tinta o Palabra", withUnit = true), Comparison.heroSpoken(r, range, "Tinta o Palabra"),
        Comparison.needSentence(l, r), Comparison.ladderNote(r, range), Comparison.bellSpoken(r), Comparison.bellSpoken(r, range)
      )
      Comparison.freeSentence(Comparison.Place.Found(r))?.let { lines += it }
    }
    lines += listOf(Comparison.ESTIMATED_BELL_CAPTION, Comparison.WEEKS_ESTIMATED_NOTE, Comparison.noMarkSentence("Tinta o Palabra", estimated = true))
    for (k in 1..7) lines += Comparison.weeksSpoken(List(k) { Comparison.WeekPoint(it, 20, 50) }, estimated = true)
    val lead = Comparison.othersLead("Tinta o Palabra", estimated = true)       // el encabezado de «Otras edades» cierra con dos puntos
    assertTrue(lead.endsWith(":"))
    lines += lead
    for (line in lines) {
      assertTrue("«$line» termina en punto o dos puntos", line.endsWith(".") || line == lead)
      val low = line.lowercase()
      assertFalse("«$line» llama personas a la referencia", "personas" in low || "persona " in low)
      assertFalse("«$line» dice un número de robots", "robot" in low || "2000" in low || "2.000" in low)
      for (bad in listOf("capacidad", "edad mental", "ranking", "velocidad", "rendimiento", "cerebro", "cognitiv", "entren", "puntaje", "nivel", "todavía somos pocos", "mientras tanto", "todas las edades", "cuando lleguen"))
        assertFalse("«$line» dice «$bad»", bad in low)
    }
    assertTrue("la hoja y la frase larga sí la nombran", lines.any { "referencia estimada" in it })
  }

  @Test
  fun `la frase larga de la referencia dice de donde sale y que no son personas, una sola vez y sin prometer el cambio`() {
    for (range in AgeRange.entries) {
      val s = Comparison.estimatedNotice(range)
      assertEquals("No son otras personas: Nubi no junta datos de nadie. Por eso la referencia para ${range.label} es estimada: sale de estudios por edad y de un programa que jugó esta misma pista en un computador, no en tu teléfono.", s)
      val low = s.lowercase()
      for (promise in listOf("luego", "pronto", "cuando haya", "cuando lleguen", "reales", "después", "más adelante", "mientras", "todavía")) assertFalse("la frase larga promete «$promise»", promise in low)
      assertEquals("«No son otras personas:» es la única vez que se nombra a las personas", 1, Regex("personas").findAll(low).count())
      assertTrue("la razón va primero (Tarea 209)", s.startsWith("No son otras personas: Nubi no junta datos de nadie."))
    }
    assertEquals("el título de la campana es el de siempre (el largo partía en dos líneas)", "La campana de tu edad", Comparison.BELL_TITLE)
  }

  // ------------------------------------------------------------------ la escalera de divisiones (Tarea 203)

  @Test
  fun `los peldanos salen de los tramos sin suavizar con el primer acierto de cada division`() {
    val up = Comparison.thresholds(shaped(128))
    assertEquals(
      listOf(
        Comparison.Division.PLATFORM to 0, Comparison.Division.TAKEOFF to 31, Comparison.Division.ORBIT to 39,
        Comparison.Division.MOON to 50, Comparison.Division.STARS to 60
      ),
      up.map { it.division to it.from }
    )
    // cada peldaño es el PRIMER acierto que llega al percentil de su división, y el anterior no llega
    for (rung in up.drop(1)) {
      assertTrue("${rung.division} desde ${rung.from} llega", Comparison.percentile(rung.from, shaped(128)) >= rung.division.from)
      assertTrue("${rung.division} un acierto antes no llega", Comparison.percentile(rung.from - 1, shaped(128)) < rung.division.from)
    }
  }

  @Test
  fun `la division de cada marca coincide con el peldaño donde la escalera la pone`() {
    val d = shaped(128)
    for (mark in 0..79) {
      val r = Comparison.Ranked(mark, Comparison.percentile(mark, d), Comparison.division(Comparison.percentile(mark, d)), Comparison.Scope.OWN_WEEK, listOf(AgeRange.R45_54), d)
      val l = Comparison.ladder(r)
      val current = l.current
      assertNotNull("la marca $mark está en un peldaño", current)
      assertTrue("la marca $mark está en su peldaño o arriba de él", mark >= current!!.from)
      assertTrue("y por debajo del siguiente", l.next == null || mark < l.next.from)
    }
  }

  @Test
  fun `te faltan cuenta los aciertos hasta el primer acierto de la division siguiente`() {
    val r = ranked(Comparison.Scope.OWN_WEEK)
    val l = Comparison.ladder(r)
    assertEquals(Comparison.Division.ORBIT, l.current!!.division)
    assertEquals(Comparison.Division.MOON, l.next!!.division)
    assertEquals(7, l.missing)
    assertEquals("4 de los 11 aciertos del tramo", 4f / 11f, l.progress!!, 1e-6f)
    assertEquals("Te faltan 7 aciertos para En la Luna.", Comparison.needSentence(l, r))
    assertEquals(listOf("Entre estrellas", "En la Luna", "En órbita", "Despegando", "En la plataforma"), l.rungs.map { it.division.label })
    assertEquals("desde 39 aciertos", Comparison.rungText(l.current!!, l.rungs[l.rungs.indexOf(l.current) - 1]))
    assertEquals("Tú · 43 aciertos", Comparison.youRow(r))
  }

  @Test
  fun `te falta uno se dice en singular y el primer acierto de la division no deja decir cero`() {
    val d = shaped(128)
    val r = Comparison.Ranked(49, Comparison.percentile(49, d), Comparison.division(Comparison.percentile(49, d)), Comparison.Scope.OWN_WEEK, listOf(AgeRange.R45_54), d)
    val l = Comparison.ladder(r)
    assertEquals(1, l.missing)
    assertEquals("Te falta 1 acierto para En la Luna.", Comparison.needSentence(l, r))
    // justo en el primer acierto de una division, falta lo que hay hasta la siguiente (nunca 0)
    val atEdge = Comparison.Ranked(50, Comparison.percentile(50, d), Comparison.division(Comparison.percentile(50, d)), Comparison.Scope.OWN_WEEK, listOf(AgeRange.R45_54), d)
    assertEquals(Comparison.Division.MOON, atEdge.division)
    assertEquals(10, Comparison.ladder(atEdge).missing)
  }

  @Test
  fun `en la division mas alta no falta nada y la escalera lo dice sin cifra de te faltan`() {
    val d = shaped(128)
    val r = Comparison.Ranked(70, Comparison.percentile(70, d), Comparison.division(Comparison.percentile(70, d)), Comparison.Scope.OWN_WEEK, listOf(AgeRange.R45_54), d)
    assertEquals(Comparison.Division.STARS, r.division)
    val l = Comparison.ladder(r)
    assertNull(l.next)
    assertNull(l.missing)
    assertNull(l.progress)
    assertEquals("Estás en la división más alta de tu franja esta semana.", Comparison.needSentence(l, r))
    assertEquals("Estás en la división más alta de tu franja.", Comparison.needSentence(l, r.copy(scope = Comparison.Scope.OWN_4_WEEKS)))
    assertEquals("con personas, la más alta dice su lugar entre las cinco", "Esta semana estás Entre estrellas, la quinta y más alta de las cinco divisiones.", Comparison.freeSentence(Comparison.Place.Found(r)))
  }

  @Test
  fun `una franja donde casi todos hicieron lo mismo deja menos peldaños y nunca uno vacio`() {
    val top = dist(List(15) { 0 } + listOf(10))              // todas las personas hicieron 75 o más: nadie queda debajo de ninguna marca
    assertEquals("solo la plataforma: a ninguna otra se llega", listOf(Comparison.Division.PLATFORM), Comparison.thresholds(top).map { it.division })
    val low = dist(listOf(100) + List(15) { 0 })             // todas hicieron de 0 a 4
    val rungs = Comparison.thresholds(low)
    assertEquals(listOf(0, 2, 3, 4, 5), rungs.map { it.from })
    assertTrue("los primeros aciertos suben de a uno como mínimo", rungs.zipWithNext().all { (a, b) -> a.from < b.from })
    val lone = Comparison.ladder(Comparison.Ranked(90, 0, Comparison.Division.PLATFORM, Comparison.Scope.OWN_WEEK, listOf(AgeRange.R45_54), top))
    assertNull("con una sola división no hay siguiente", lone.next)
  }

  @Test
  fun `la nota de la escalera cuenta las personas una sola vez sin repetir la franja ni el juego de la cifra`() {
    assertEquals("Con las 128 personas de tu franja que jugaron esta semana.", Comparison.ladderNote(ranked(Comparison.Scope.OWN_WEEK), AgeRange.R45_54))
    assertEquals("con las 4 semanas, que lo diga", "Con las 128 personas de tu franja que jugaron en las últimas 4 semanas.", Comparison.ladderNote(ranked(Comparison.Scope.OWN_4_WEEKS), AgeRange.R45_54))
    assertEquals("Con las 128 personas de 30 a 54 años que jugaron en las últimas 4 semanas.", Comparison.ladderNote(ranked(Comparison.Scope.NEIGHBORS_4_WEEKS, ranges = listOf(AgeRange.R30_44, AgeRange.R45_54)), AgeRange.R45_54))
    assertEquals("Con las 128 personas de todas las edades que jugaron en las últimas 4 semanas.", Comparison.ladderNote(ranked(Comparison.Scope.ALL_4_WEEKS, ranges = null), AgeRange.R45_54))
    assertEquals("La escalera de tu franja", Comparison.LADDER_TITLE)
  }

  @Test
  fun `la fila de abajo dice hasta donde llega en vez de desde 0`() {
    val r = ranked(Comparison.Scope.OWN_WEEK)
    val l = Comparison.ladder(r)
    assertEquals(listOf(Comparison.Division.STARS, Comparison.Division.MOON, Comparison.Division.ORBIT, Comparison.Division.TAKEOFF, Comparison.Division.PLATFORM), l.rungs.map { it.division })
    assertEquals("hasta 30 aciertos", Comparison.rungText(l.rungs.last(), l.rungs[l.rungs.size - 2]))
    assertEquals("desde 31 aciertos", Comparison.rungText(l.rungs[3], l.rungs[2]))
    assertEquals("desde 60 aciertos", Comparison.rungText(l.rungs[0], null))
    assertEquals("sin división de arriba queda «desde 0»", "desde 0 aciertos", Comparison.rungText(l.rungs.last(), null))
    val low = Comparison.thresholds(dist(listOf(100) + List(15) { 0 }))
    assertEquals("con una escalera comprimida, hasta el primer acierto de la siguiente menos uno (2 - 1)", "hasta 1 acierto", Comparison.rungText(low.first(), low[1]))
  }

  // ------------------------------------------------------------------ las semanas seguidas arriba de la mitad (Tarea 203)

  private fun week(ago: Int, p: Int) = Comparison.WeekPoint(ago, 30, p)

  @Test
  fun `las semanas seguidas cuentan desde esta hacia atras con percentil 50 o mas`() {
    assertEquals(0, Comparison.streak(emptyList()))
    assertEquals(3, Comparison.streak(listOf(week(2, 50), week(1, 60), week(0, 63))))
    assertEquals("50 justo es En órbita: cuenta", 1, Comparison.streak(listOf(week(0, 50))))
    assertEquals("49 no cuenta", 0, Comparison.streak(listOf(week(1, 80), week(0, 49))))
  }

  @Test
  fun `una semana por debajo, o sin dato, corta la cuenta y lo anterior ya no suma`() {
    assertEquals("la de hace 1 semana quedó abajo: solo esta", 1, Comparison.streak(listOf(week(3, 70), week(2, 70), week(1, 30), week(0, 55))))
    assertEquals("sin marca hace 1 semana no se sabe cómo fue: corta", 1, Comparison.streak(listOf(week(2, 70), week(0, 55))))
    assertEquals("esta semana sin dato: no hay racha que contar", 0, Comparison.streak(listOf(week(2, 70), week(1, 70))))
    assertEquals("las 7 semanas", 7, Comparison.streak(List(7) { week(6 - it, 51) }))
  }

  @Test
  fun `la frase de las semanas seguidas calla cuando no hay y nunca habla de perder`() {
    assertEquals("Llevas 3 semanas seguidas En órbita o más arriba.", Comparison.streakSentence(3))
    assertEquals("Llevas 7 semanas seguidas En órbita o más arriba.", Comparison.streakSentence(7))
    assertEquals("Esta semana empiezas una racha En órbita o más arriba.", Comparison.streakSentence(1))
    assertNull("cuando se corta, la página calla", Comparison.streakSentence(0))
    for (n in 0..7) Comparison.streakSentence(n)?.let { s -> for (bad in listOf("perdiste", "perdió", "cortó", "rompiste", "volver a")) assertFalse("«$s» dice «$bad»", bad in s.lowercase()) }
  }

  @Test
  fun `el rotulo izquierdo del eje de las semanas dice la mas vieja y calla si es esta`() {
    assertEquals("hace 3 semanas", Comparison.weeksAxisLeft(listOf(week(3, 40), week(0, 63))))
    assertNull("si la más vieja es esta no hay de qué separar", Comparison.weeksAxisLeft(listOf(week(0, 63))))
    assertEquals("la raya de la mitad se rotula con una palabra corta", "la mitad", Comparison.WEEKS_HALF_LABEL)
    assertEquals("Semana a semana", Comparison.WEEKS_TITLE)
    assertFalse("«percentil» salió de todo texto visible (Tarea 209)", Comparison.WEEKS_TITLE.lowercase().contains("percentil"))
    // Tarea 209b: dos losas de la misma página («Tu lugar esta semana» cuando no hay marca esta semana y las semanas pasadas) no se parecen en el título
    assertNotEquals(Comparison.PLACE_TITLE, Comparison.WEEKS_TITLE)
    assertNotEquals("no empiezan igual", Comparison.PLACE_TITLE.split(' ').take(2), Comparison.WEEKS_TITLE.split(' ').take(2))
  }

  // ------------------------------------------------------------------ lo gratis (Tarea 203)

  @Test
  fun `la frase gratis dice la division y lo que falta, sin cifra de percentil ni escalera`() {
    val free = Comparison.freeSentence(Comparison.Place.Found(ranked(Comparison.Scope.OWN_WEEK)))
    assertEquals("Esta semana estás En órbita, la tercera de cinco divisiones. Te faltan 7 aciertos para En la Luna.", free)
    assertFalse("el percentil es de Plus", "percentil" in free!!.lowercase())
    assertFalse("y la cifra de cada 100 también", "100" in free)
  }

  @Test
  fun `con pocos o sin marca no hay frase gratis ni te faltan inventado`() {
    assertNull(Comparison.freeSentence(Comparison.Place.Few(23, null)))
    assertNull(Comparison.freeSentence(Comparison.Place.Few(23, ranked(Comparison.Scope.ALL_4_WEEKS, ranges = null))))
    assertNull(Comparison.freeSentence(Comparison.Place.NoMark))
  }

  @Test
  fun `la frase gratis de la puerta usa la mejor marca de esta semana del juego de hoy`() {
    val now = java.util.Calendar.getInstance().apply { set(2026, java.util.Calendar.OCTOBER, 28, 18, 0, 0); set(java.util.Calendar.MILLISECOND, 0) }.timeInMillis
    fun at(day: Int) = java.util.Calendar.getInstance().apply { set(2026, java.util.Calendar.OCTOBER, day, 12, 0, 0); set(java.util.Calendar.MILLISECOND, 0) }.timeInMillis
    fun r(mark: Int, day: Int, game: String = "stroop") = TimeTrialResult(id = "r$game$day$mark", gameId = game, day = "2026-10-%02d".format(day), trackVersion = TimeTrialDay.trackVersion(game), hits = mark + 2, errors = 2, level = 4, timestamp = at(day))
    val src = SampleComparison(few = false)
    assertEquals(
      "Esta semana estás En órbita, la tercera de cinco divisiones. Te faltan 7 aciertos para En la Luna.",
      Comparison.freeSentenceFor(src, "stroop", AgeRange.R45_54, listOf(r(30, 13), r(43, 27), r(40, 28)), now)
    )
    assertNull("sin partida esta semana no hay nada que decir", Comparison.freeSentenceFor(src, "stroop", AgeRange.R45_54, listOf(r(43, 20)), now))
    assertNull("ni con otro juego", Comparison.freeSentenceFor(src, "stroop", AgeRange.R45_54, listOf(r(43, 27, game = "disparate")), now))
    assertNull("con pocos tampoco: nada de te faltan sin datos", Comparison.freeSentenceFor(SampleComparison(SampleComparison.World.VERY_FEW), "stroop", AgeRange.R45_54, listOf(r(43, 27)), now))
  }

  // ------------------------------------------------------------------ el lugar aparece desde la tercera partida de cada juego (Tarea 207)

  /** Una fuente que SÍ tiene con qué comparar los cuatro juegos (la forma de una referencia estimada: 2.000 robots, todos en el tramo del medio). */
  private val referenceLike = object : Comparison.Source {
    override fun distribution(game: String, ranges: List<AgeRange>?, weeksAgo: Int, weeks: Int) = Comparison.Distribution(2000, List(16) { if (it == 8) 2000 else 0 }, 40, 40, 40, binWidth = 5, estimated = true)
  }

  /** [n] partidas guardadas de [game], de semanas y pistas cualesquiera. */
  private fun saved(game: String, n: Int, version: Int = 2) =
    List(n) { i -> TimeTrialResult(id = "g$game$version$i", gameId = game, day = "2026-09-%02d".format(1 + i), trackVersion = version, hits = 30 + i, errors = 2, level = 4, timestamp = 1_000L * (i + 1)) }

  @Test
  fun `la regla cuenta por juego, la tercera partida ya muestra y los cortes son 0, 1, 2 y 3`() {
    assertEquals(3, Comparison.PLACE_FROM_GAME)
    for ((n, unlocked) in listOf(0 to false, 1 to false, 2 to false, 3 to true, 4 to true, 20 to true)) {
      assertEquals("con $n partida(s) guardada(s)", n, Comparison.gamesPlayed(saved("stroop", n), "stroop"))
      assertEquals("con $n: ¿se dice el lugar?", unlocked, Comparison.placeUnlocked(saved("stroop", n), "stroop"))
      assertEquals("con $n: la regla de la página", if (unlocked) Comparison.Gate.SHOW else Comparison.Gate.EARLY, Comparison.gate(referenceLike, AgeRange.R45_54, "stroop", saved("stroop", n)))
    }
  }

  @Test
  fun `cuentan las partidas de cualquier semana y pista, y las de otro juego no suman`() {
    val mixed = saved("stroop", 1) + saved("stroop", 1, version = 1) + saved("disparate", 9) + saved("radar", 9)
    assertEquals("dos de Tinta (una de la pista vieja) y nada de los otros juegos", 2, Comparison.gamesPlayed(mixed, "stroop"))
    assertEquals(Comparison.Gate.EARLY, Comparison.gate(referenceLike, AgeRange.R45_54, "stroop", mixed))
    assertEquals("con la tercera, de cualquier pista, ya se dice", Comparison.Gate.SHOW, Comparison.gate(referenceLike, AgeRange.R45_54, "stroop", mixed + saved("stroop", 1, version = 3).map { it.copy(id = "otra") }))
    assertEquals("la cuenta de ¿Verdad o disparate? es suya", Comparison.Gate.SHOW, Comparison.gate(referenceLike, AgeRange.R45_54, "disparate", mixed))
    assertEquals("y la de Acoplamiento, que no jugó, empieza en cero", Comparison.Gate.EARLY, Comparison.gate(referenceLike, AgeRange.R45_54, "acoplamiento", mixed))
  }

  @Test
  fun `la precedencia, sin franja el aviso de edad, de 13 a 17 su aviso, sin fuente o sin tabla nada y la muestra como la referencia`() {
    for (n in 0..3) {
      assertEquals("sin franja guardada, con $n: el aviso de edad", Comparison.Gate.NEEDS_AGE, Comparison.gate(referenceLike, null, "stroop", saved("stroop", n)))
      assertEquals("de 13 a 17 años, con $n: lo dice (Tarea 209: antes callaba)", Comparison.Gate.MINOR, Comparison.gate(referenceLike, AgeRange.R13_17, "stroop", saved("stroop", n)))
      assertEquals("sin ninguna fuente, con $n: nada", Comparison.Gate.NONE, Comparison.gate(null, AgeRange.R45_54, "stroop", saved("stroop", n)))
      assertEquals("sin ninguna fuente y sin franja, con $n: nada", Comparison.Gate.NONE, Comparison.gate(null, null, "stroop", saved("stroop", n)))
    }
    val sinTabla = object : Comparison.Source {
      override fun distribution(game: String, ranges: List<AgeRange>?, weeksAgo: Int, weeks: Int) = referenceLike.distribution(game, ranges, weeksAgo, weeks)
      override fun covers(game: String) = false
    }
    for (n in 0..3) for (range in listOf(null, AgeRange.R45_54)) assertEquals("en un juego sin tabla ($range), con $n: nada", Comparison.Gate.NONE, Comparison.gate(sinTabla, range, "stroop", saved("stroop", n)))
    // con la muestra de depuración sin franja guardada se asume 45 a 54: no hay aviso de edad, vale lo mismo que con la referencia
    val sample = SampleComparison(few = false)
    for (n in 0..2) assertEquals("muestra, sin franja, con $n", Comparison.Gate.EARLY, Comparison.gate(sample, null, "stroop", saved("stroop", n)))
    assertEquals("muestra, sin franja, con 3", Comparison.Gate.SHOW, Comparison.gate(sample, null, "stroop", saved("stroop", 3)))
    assertEquals("muestra, de 13 a 17 años: lo dice", Comparison.Gate.MINOR, Comparison.gate(sample, AgeRange.R13_17, "stroop", saved("stroop", 3)))
  }

  @Test
  fun `el lugar de cada division entre las cinco se dice con palabras, en minuscula y en la cifra con mayuscula`() {
    assertEquals(
      listOf("la primera de cinco divisiones", "la segunda de cinco divisiones", "la tercera de cinco divisiones", "la cuarta de cinco divisiones", "la quinta y más alta de las cinco divisiones"),
      Comparison.Division.entries.map { Comparison.divisionPlace(it) }
    )
    assertEquals(
      listOf("La primera de cinco divisiones.", "La segunda de cinco divisiones.", "La tercera de cinco divisiones.", "La cuarta de cinco divisiones.", "La quinta y más alta de las cinco divisiones."),
      Comparison.Division.entries.map { Comparison.divisionPlaceLine(it) }
    )
    for (d in Comparison.Division.entries) assertFalse("«percentil» no se dice", Comparison.divisionPlace(d).contains("percentil"))
  }

  @Test
  fun `la frase gratis con la referencia dice el lugar de la division, la referencia estimada y lo que falta`() {
    val dist = Comparison.Distribution(2000, SampleComparison.fit(base, 2000), 31, 39, 47, estimated = true)
    fun free(mark: Int): String {
      val r = Comparison.Ranked(mark, Comparison.percentile(mark, dist), Comparison.division(Comparison.percentile(mark, dist)), Comparison.Scope.OWN_WEEK, listOf(AgeRange.R45_54), dist)
      return Comparison.freeSentence(Comparison.Place.Found(r))!!
    }
    // una marca en cada división: el nombre, su lugar entre las cinco y la referencia estimada, y el «te faltan» salvo en la más alta
    val seen = (0..100).map { free(it) }.distinct()
    for (division in Comparison.Division.entries) {
      val line = seen.firstOrNull { it.startsWith("Esta semana estás ${division.label}, ${Comparison.divisionPlace(division)} de la referencia estimada para tu edad.") }
      assertNotNull("hay una marca que cae en ${division.label}", line)
      if (division != Comparison.Division.STARS) assertTrue("«$line» dice cuánto falta", Regex("Te falt(a|an) \\d+ aciertos? para ").containsMatchIn(line!!))
    }
    assertTrue("la más alta, con la referencia, no pide nada más", seen.any { it == "Esta semana estás Entre estrellas, la quinta y más alta de las cinco divisiones de la referencia estimada para tu edad." })
    for (line in seen) assertFalse("«$line» llama personas a la referencia", "personas" in line.lowercase())
  }

  @Test
  fun `de 13 a 17 anos la pagina lo dice con una linea y la hoja y la puerta tambien saben que es esa franja`() {
    assertEquals("De 13 a 17 años, el Contrarreloj muestra solo tus propias marcas.", Comparison.MINORS_NOTICE)
    assertTrue("la guarda COMPARE_MINORS apagada: esa franja no se compara", !com.example.Features.COMPARE_MINORS)
    assertTrue(Comparison.isMinorLocked(AgeRange.R13_17))
    for (range in AgeRange.entries.filter { it != AgeRange.R13_17 }) assertFalse("$range sí se compara", Comparison.isMinorLocked(range))
    assertFalse("sin franja guardada no es de 13 a 17", Comparison.isMinorLocked(null))
    // con qué se compara ESTA persona, para el candado de la puerta y la hoja
    assertEquals(Comparison.Kind.ESTIMATED, Comparison.kindFor(referenceLike, AgeRange.R45_54))
    assertEquals("de 13 a 17 años, nada", Comparison.Kind.NONE, Comparison.kindFor(referenceLike, AgeRange.R13_17))
    assertEquals("sin franja guardada, nada (con la referencia no se asume una)", Comparison.Kind.NONE, Comparison.kindFor(referenceLike, null))
    assertEquals("sin fuente, nada", Comparison.Kind.NONE, Comparison.kindFor(null, AgeRange.R45_54))
    assertEquals("la fila que abre la hoja de los estudios", "De dónde sale la referencia ›", Comparison.SOURCE_LINK)
  }

  @Test
  fun `la puerta de Avance calla con menos de tres partidas del juego y habla desde la tercera`() {
    val now = java.util.Calendar.getInstance().apply { set(2026, java.util.Calendar.OCTOBER, 28, 18, 0, 0); set(java.util.Calendar.MILLISECOND, 0) }.timeInMillis
    fun at(day: Int) = java.util.Calendar.getInstance().apply { set(2026, java.util.Calendar.OCTOBER, day, 12, 0, 0); set(java.util.Calendar.MILLISECOND, 0) }.timeInMillis
    fun r(mark: Int, day: Int, game: String = "stroop") = TimeTrialResult(id = "r$game$day$mark", gameId = game, day = "2026-10-%02d".format(day), trackVersion = TimeTrialDay.trackVersion(game), hits = mark + 2, errors = 2, level = 4, timestamp = at(day))
    val src = SampleComparison(few = false)
    assertNull("con una partida esta semana, la puerta calla", Comparison.freeSentenceFor(src, "stroop", AgeRange.R45_54, listOf(r(43, 27)), now))
    assertNull("con dos, también", Comparison.freeSentenceFor(src, "stroop", AgeRange.R45_54, listOf(r(30, 13), r(43, 27)), now))
    assertNull("y las de otro juego no suman", Comparison.freeSentenceFor(src, "stroop", AgeRange.R45_54, listOf(r(43, 27), r(30, 13, "disparate"), r(31, 14, "disparate"), r(32, 15, "disparate")), now))
    assertEquals("con la tercera, la frase de siempre con su lugar", "Esta semana estás En órbita, la tercera de cinco divisiones. Te faltan 7 aciertos para En la Luna.", Comparison.freeSentenceFor(src, "stroop", AgeRange.R45_54, listOf(r(30, 13), r(43, 27), r(40, 28)), now))
  }

  @Test
  fun `la frase de la tercera partida es la exacta y no usa las palabras que no se dicen`() {
    assertEquals("Tu lugar aparece desde tu tercera partida: las primeras sirven para conocer el juego.", Comparison.EARLY_NOTICE)
    val low = Comparison.EARLY_NOTICE.lowercase()
    for (bad in listOf("personas", "robot", "capacidad", "cognitiv", "cerebro", "percentil", "ranking", "velocidad", "entrenamiento")) assertFalse("la frase dice «$bad»", bad in low)
    assertTrue(Comparison.EARLY_NOTICE.endsWith("."))
    assertNotEquals("y no es el aviso de la edad", Comparison.AGE_HINT, Comparison.EARLY_NOTICE)
  }

  // ------------------------------------------------------------------ los rótulos del eje de la campana (Tarea 207)

  /** Las tablas REALES del APK, como las lee la app. */
  private val realReference: EstimatedReference by lazy {
    val dir = java.io.File("src/main/assets/${EstimatedReference.ASSET_DIR}")
    EstimatedReference.of(dir.listFiles { f -> f.extension == "json" }.orEmpty().map { it.readText(Charsets.UTF_8) })
  }

  @Test
  fun `Tinta de 30 a 44 con la mitad en 65 no dibuja el 63, ni a 393 ni a 360 dp`() {
    val d = realReference.distribution("stroop", listOf(AgeRange.R30_44), 0, 1)
    assertEquals(65, d.p50); assertEquals(listOf(0, 32, 63, 95), Comparison.axisTicks(d))
    // anchos de «0 aciertos», «32», «63» y «95 o más» en 14 sp, en dp; el dibujo mide 305 dp a 393 y 272 a 360
    val widths = listOf(66.0, 16.5, 16.5, 52.0)
    for (width in listOf(305.0, 272.0)) {
      assertEquals("a $width dp: «0 aciertos · 32 · 95 o más»", listOf(0, 1, 3), Comparison.axisLabelsFor(d, 58, onHalf = false, widthDp = width, labelWidthsDp = widths))
    }
  }

  @Test
  fun `una marca justo sobre un rotulo lo oculta, y los extremos se dibujan siempre`() {
    val d = realReference.distribution("stroop", listOf(AgeRange.R30_44), 0, 1)
    val widths = listOf(66.0, 16.5, 16.5, 52.0)
    // la barra «Tú» de 63 cae sobre el «63» (la raya, en 65, también); la de 32 sobre el «32»
    assertEquals(listOf(0, 1, 3), Comparison.axisLabelsFor(d, 63, onHalf = false, widthDp = 305.0, labelWidthsDp = widths))
    assertEquals("con la barra en 32 no queda ningún intermedio: el 63 sigue bajo la raya", listOf(0, 3), Comparison.axisLabelsFor(d, 32, onHalf = false, widthDp = 305.0, labelWidthsDp = widths))
    // un intermedio lejos de la barra y de la raya se dibuja
    assertEquals("con la barra en 45 se ven los dos extremos y el 32", listOf(0, 1, 3), Comparison.axisLabelsFor(d, 45, onHalf = false, widthDp = 305.0, labelWidthsDp = widths))
    // los extremos nunca se quitan, aunque la barra caiga sobre ellos
    for (mark in listOf(0, 1, 94, 99)) { val shown = Comparison.axisLabelsFor(d, mark, onHalf = false, widthDp = 305.0, labelWidthsDp = widths); assertTrue("marca $mark: $shown", 0 in shown && 3 in shown) }
  }

  @Test
  fun `si la barra tapa la raya y la raya no se dibuja, cuenta solo la barra`() {
    // «N o más» queda lejos; los dos intermedios están en 40 y 100; la caja del segundo, con 2 dp de aire, va de 98 a 118
    val starts = listOf(0.0, 40.0, 100.0, 240.0); val widths = listOf(30.0, 16.0, 16.0, 40.0)
    val line = 98.5 to Comparison.BELL_LINE_DP          // el centro de la raya cae dentro de la caja del segundo rótulo más 2 dp
    val bar = 93.5 to Comparison.BELL_BAR_DP            // y la barra «Tú», a 5 dp a su izquierda, no la toca (termina en 96,5)
    assertEquals("con la raya dibujada, el segundo rótulo se oculta", listOf(0, 1, 3), Comparison.axisLabelsShown(starts, widths, 8.0, listOf(bar, line), 2.0))
    assertEquals("sin la raya (la tapa la barra), solo cuenta la barra y el rótulo se dibuja", listOf(0, 1, 2, 3), Comparison.axisLabelsShown(starts, widths, 8.0, listOf(bar), 2.0))
  }

  @Test
  fun `el aire de 2 dp se mide con la raya y la barra enteras, y un hueco menor de 8 dp sigue quitando el del medio`() {
    val starts = listOf(0.0, 40.0, 100.0, 240.0); val widths = listOf(30.0, 16.0, 16.0, 40.0)
    // una raya de 1,5 dp a 2 dp justos de la caja del segundo rótulo (que empieza en 100): la toca con el aire; a más de 2,75 dp ya no
    assertTrue("raya a 97,2 (termina en 97,95, a 2,05 dp de la caja): no toca y el rótulo se dibuja", 2 in Comparison.axisLabelsShown(starts, widths, 8.0, listOf(97.2 to 1.5), 2.0))
    assertFalse("raya a 98,0 (termina en 98,75, a 1,25 dp de la caja): toca y el rótulo se oculta", 2 in Comparison.axisLabelsShown(starts, widths, 8.0, listOf(98.0 to 1.5), 2.0))
    // la regla de siempre: los rótulos que se pisan (con 8 dp de hueco) se omiten, el de la derecha antes que el de la izquierda
    assertEquals(listOf(0, 3), Comparison.axisLabelsShown(listOf(0.0, 50.0, 100.0, 150.0), listOf(70.0, 70.0, 70.0, 70.0), 8.0, emptyList(), 2.0))
    assertEquals("con espacio caben los cuatro", listOf(0, 1, 2, 3), Comparison.axisLabelsShown(starts, widths, 8.0, emptyList(), 2.0))
  }

  @Test
  fun `los cinco peldaños y el eje salen de la misma tabla, el rotulo omitido nunca cambia la escalera`() {
    // omitir un rótulo del eje es solo dibujo: la escalera y la mitad no dependen de él
    val d = realReference.distribution("stroop", listOf(AgeRange.R30_44), 0, 1)
    assertEquals(listOf(0, 58, 65, 72, 78), Comparison.thresholds(d).map { it.from })
  }

  // ------------------------------------------------------------------ la barra «Tú» sobre la raya de la mitad (Tarea 205b)

  @Test
  fun `la barra Tu tapa la raya de la mitad cuando sus centros quedan a menos de 4,75 dp`() {
    // Tinta o Palabra: 16 tramos de 5 (80 aciertos de ancho); la mitad en 39 → 3,75 dp por acierto con 300 dp
    val stroop = Comparison.Distribution(2000, List(16) { if (it == 7) 2000 else 0 }, 35, 39, 44, binWidth = 5, estimated = true)
    assertTrue("en la mitad", Comparison.barCoversHalf(39, stroop, 300.0))
    assertTrue("a un acierto (3,75 dp)", Comparison.barCoversHalf(40, stroop, 300.0) && Comparison.barCoversHalf(38, stroop, 300.0))
    assertFalse("a dos aciertos (7,5 dp) la raya se ve", Comparison.barCoversHalf(41, stroop, 300.0) || Comparison.barCoversHalf(37, stroop, 300.0))
    assertTrue("con menos ancho (272 dp, ~3,4 dp por acierto) un acierto sigue tapando", Comparison.barCoversHalf(40, stroop, 272.0))
    assertFalse("con 272 dp dos aciertos (6,8 dp) ya no", Comparison.barCoversHalf(41, stroop, 272.0))
    // un juego de a 1 acierto y 24 tramos (así era Rescate relámpago hasta la Tarea 208; hoy va de a 3 con 16): 12,5 dp por acierto con 300 dp: solo la misma marca
    val radar = Comparison.Distribution(2000, List(24) { if (it == 14) 2000 else 0 }, 13, 14, 14, binWidth = 1, estimated = true)
    assertTrue(Comparison.barCoversHalf(14, radar, 300.0))
    assertFalse("a un acierto (12,5 dp) se ve", Comparison.barCoversHalf(15, radar, 300.0) || Comparison.barCoversHalf(13, radar, 300.0))
    // el umbral: 6/2 + 1,5/2 + 1 = 4,75 dp; justo 4,75 ya no tapa
    assertEquals(4.75, Comparison.BELL_BAR_DP / 2 + Comparison.BELL_LINE_DP / 2 + Comparison.BELL_CLEARANCE_DP, 1e-9)
    assertFalse("4,75 dp justos: separadas", Comparison.barCoversHalf(13, Comparison.Distribution(2000, List(16) { if (it == 7) 2000 else 0 }, 0, 12, 0, binWidth = 5, estimated = true), 80.0 * 4.75))
    // el último tramo («o más») se topa en su centro: la marca y la mitad ahí tapan igual
    assertTrue(Comparison.barCoversHalf(500, radar.copy(p50 = 23), 300.0))
  }

  @Test
  fun `la frase de la campana y lo que lee el lector de pantalla dicen cuando la marca esta en la mitad`() {
    assertEquals("La línea marca la mitad de la referencia estimada.", Comparison.bellCaption(estimated = true))
    assertEquals("La línea marca la mitad de las personas.", Comparison.bellCaption(estimated = false))
    assertEquals("Tu marca está en la mitad de la referencia estimada.", Comparison.bellCaption(estimated = true, onHalf = true))
    assertEquals("Tu marca está en la mitad de las personas.", Comparison.bellCaption(estimated = false, onHalf = true))
    val est = Comparison.Distribution(2000, SampleComparison.fit(base, 2000), 31, 39, 47, estimated = true)
    val r = Comparison.Ranked(39, Comparison.percentile(39, est), Comparison.division(Comparison.percentile(39, est)), Comparison.Scope.OWN_WEEK, listOf(AgeRange.R45_54), est)
    assertEquals("Campana de la referencia estimada para 45 a 54 años. Tu mejor marca de esta semana, 39 aciertos, está marcada.", Comparison.bellSpoken(r, AgeRange.R45_54))
    assertEquals("Campana de la referencia estimada para 45 a 54 años. Tu mejor marca de esta semana, 39 aciertos, está en la mitad.", Comparison.bellSpoken(r, AgeRange.R45_54, onHalf = true))
    val real = Comparison.Ranked(43, 63, Comparison.Division.ORBIT, Comparison.Scope.OWN_WEEK, listOf(AgeRange.R45_54), shaped(128))
    assertTrue(Comparison.bellSpoken(real, onHalf = true).endsWith("43 aciertos, está en la mitad."))
    for (text in listOf(Comparison.BELL_ON_HALF_ESTIMATED, Comparison.BELL_ON_HALF_PEOPLE)) { assertTrue(text.endsWith(".")); assertFalse("la frase de la mitad no habla de una raya", "línea" in text.lowercase() || "raya" in text.lowercase()) }
    assertFalse("con la referencia no se dice «personas»", "personas" in Comparison.BELL_ON_HALF_ESTIMATED)
  }

  @Test
  fun `la frase que dice donde elegir la edad es la ruta real, una sola frase sin palabras prohibidas`() {
    assertEquals("Para compararte con una referencia estimada para tu edad, elige tu edad en Tú › Más ajustes › Juego › Rango de edad.", Comparison.AGE_HINT)
    val low = Comparison.AGE_HINT.lowercase()
    for (bad in listOf("personas", "robot", "capacidad", "cognitiv", "cerebro", "percentil", "ranking")) assertFalse("la frase dice «$bad»", bad in low)
    assertTrue(Comparison.AGE_HINT.endsWith("."))
  }
}
