package com.example.data

import org.junit.Assert.assertEquals
import org.junit.Assert.assertFalse
import org.junit.Assert.assertNull
import org.junit.Assert.assertTrue
import org.junit.Test

/**
 * Las medidas de los juegos estrella en frases completas (Tarea 159; Ricardo: «sin palabras sueltas»; Opus: segundos y no milisegundos, «grados por segundo» escrito completo, «En tu última partida» cuando no fue hoy). Las medidas internas no cambian: solo lo que se ve.
 */
class MeasurePhrasesTest {
  private val day = 86_400_000L
  private val now = 300 * day + 12 * 3_600_000L
  private fun def(key: String) = StarMeasures.def(key)!!
  private fun p(key: String, daysAgo: Int, v: Float, rating: Float = 0.5f) = MeasurePoint(now - daysAgo * day, key, v, rating, true, PlayMode.A_TU_MEDIDA)

  @Test
  fun `los segundos se escriben con coma, sin ceros de sobra y sin errores de redondeo`() {
    assertEquals("0,16", MeasurePhrases.seconds(160f)); assertEquals("0,2", MeasurePhrases.seconds(200f)); assertEquals("1", MeasurePhrases.seconds(1000f))
    assertEquals("0,86", MeasurePhrases.seconds(855f)); assertEquals("0,45", MeasurePhrases.seconds(450f)); assertEquals("0,1", MeasurePhrases.seconds(100f)); assertEquals("1,29", MeasurePhrases.seconds(1291f))
    assertEquals("1 segundo", MeasurePhrases.secondsWord(1000f)); assertEquals("0,16 segundos", MeasurePhrases.secondsWord(160f)); assertEquals("1,4 segundos", MeasurePhrases.secondsWord(1400f))
  }

  @Test
  fun `cada medida se dice con su unidad en una frase y nunca en milisegundos ni con el simbolo de grado`() {
    val samples = mapOf(
      "glance" to 160f, "brake" to 255f, "tracking" to 2.5f, "numline" to 3.8f, "rotation" to 96f, "mandos" to 80f, "vocab" to 62f, "wpm" to 180f, "atlas" to 75f,
      "punta" to 60f, "carga" to 80f, "taller" to 70f, "bodega" to 90f, "place" to 70f, "trail" to 7f, "stroop" to 400f, "estacion" to 85f
    )
    assertEquals("las 17 medidas", 17, StarMeasures.defs.size); assertEquals(StarMeasures.defs.map { it.key }.toSet(), samples.keys)
    for ((k, v) in samples) {
      val d = def(k)
      for (text in listOf(MeasurePhrases.value(d, v), MeasurePhrases.short(d, v), MeasurePhrases.recordLabel(d))) {
        assertTrue("$k: «$text» vacía", text.length > 4)
        for (bad in listOf(" ms", "°", "/s", "pts", "→", "  ")) assertFalse("$k: «$text» trae «$bad»", bad in text)
      }
    }
    assertEquals("0,16 segundos de destello", MeasurePhrases.value(def("glance"), 160f))
    assertEquals("96 grados por segundo", MeasurePhrases.value(def("rotation"), 96f)); assertEquals("1 grado por segundo", MeasurePhrases.value(def("rotation"), 1f))
    assertEquals("5 luces seguidas", MeasurePhrases.value(def("trail"), 5f)); assertEquals("1 luz seguida", MeasurePhrases.value(def("trail"), 1f))
    assertEquals("7 luces", MeasurePhrases.short(def("trail"), 7f))
    assertEquals("2,5 satélites a la vez", MeasurePhrases.value(def("tracking"), 2.5f))
    assertEquals("a 3,8 % del blanco", MeasurePhrases.value(def("numline"), 3.8f))
    assertEquals("180 palabras por minuto", MeasurePhrases.value(def("wpm"), 180f))
    assertEquals("70 % de las veces, directo a una pareja ya vista", MeasurePhrases.value(def("place"), 70f)); assertEquals("80 % de las veces", MeasurePhrases.short(def("place"), 80f))
    assertEquals("Tinta o Palabra, en palabras como en su final", "la palabra te frena 0,4 segundos", MeasurePhrases.value(def("stroop"), 400f))
    assertEquals("la palabra casi no te frena", MeasurePhrases.value(def("stroop"), 30f))
  }

  @Test
  fun `la frase de la medida dice hoy o en tu ultima partida y la mejor marca solo con tres partidas`() {
    val d = def("trail")
    assertEquals("Hoy: 5 luces seguidas. Tu mejor marca: 7 luces.", MeasurePhrases.sentence(d, 5f, 7f, today = true))
    assertEquals("En tu última partida: 5 luces seguidas. Tu mejor marca: 7 luces.", MeasurePhrases.sentence(d, 5f, 7f, today = false))
    assertEquals("Hoy: 7 luces seguidas. Es tu mejor marca.", MeasurePhrases.sentence(d, 7f, 7f, today = true))
    assertEquals("con menos de tres partidas no se dice nada de «mejor»", "Hoy: 5 luces seguidas.", MeasurePhrases.sentence(d, 5f, null, today = true))
    // menos es mejor: el vistazo
    assertEquals("En tu última partida: 0,19 segundos de destello. Tu mejor marca: 0,16 segundos.", MeasurePhrases.sentence(def("glance"), 190f, 160f, today = false))
    assertEquals("Hoy: 0,16 segundos de destello. Es tu mejor marca.", MeasurePhrases.sentence(def("glance"), 160f, 160f, today = true))
  }

  @Test
  fun `la frase sin su comienzo es la de la fila Tu ultima partida y puede ir sin la mejor marca`() {
    val d = def("trail")
    assertEquals("5 luces seguidas. Tu mejor marca: 7 luces.", MeasurePhrases.body(d, 5f, 7f))
    assertEquals("Rastro y Satélites: sin «Tu mejor marca» (el nivel ya dice lo que repites)", "5 luces seguidas.", MeasurePhrases.body(d, 5f, 7f, withBest = false))
    assertEquals("«Es tu mejor marca.» sí se dice siempre", "7 luces seguidas. Es tu mejor marca.", MeasurePhrases.body(d, 7f, 7f, withBest = false))
    assertEquals("con menos de tres partidas no se dice nada de «mejor»", "5 luces seguidas.", MeasurePhrases.body(d, 5f, null))
    assertEquals("menos es mejor", "0,16 segundos de destello. Tu mejor marca: 0,15 segundos.", MeasurePhrases.body(def("glance"), 160f, 150f))
    assertEquals("234 grados por segundo. Es tu mejor marca.", MeasurePhrases.body(def("rotation"), 234f, 234f))
    assertEquals("la frase entera es el comienzo más la frase sin comienzo", "Hoy: " + MeasurePhrases.body(d, 5f, 7f), MeasurePhrases.sentence(d, 5f, 7f, today = true))
  }

  @Test
  fun `la ultima medida de un juego dice cuando fue y la frase sin comienzo`() {
    val pts = listOf(p("trail", 9, 4f), p("trail", 5, 7f), p("trail", 3, 6f), p("trail", 0, 5f))
    val m = StarMeasures.lastMeasure("secuencia", pts)!!
    assertEquals(now, m.timestamp); assertEquals("5 luces seguidas. Tu mejor marca: 7 luces.", m.body); assertFalse(m.standalone)
    assertEquals("5 luces seguidas.", StarMeasures.lastMeasure("secuencia", pts, withBest = false)!!.body)
    val old = StarMeasures.lastMeasure("secuencia", listOf(p("trail", 9, 4f), p("trail", 6, 5f), p("trail", 3, 5f)))!!
    assertEquals("la fecha es la de la medida, no la de hoy", now - 3 * day, old.timestamp)
    assertNull("sin medida propia no hay nada", StarMeasures.lastMeasure("no_existe", pts))
    assertNull("sin partidas con la medida, nada", StarMeasures.lastMeasure("secuencia", emptyList()))
    // Freno: su medida es un promedio y va en una oración aparte («Tu última partida, ayer. Tu freno, …»)
    val brake = StarMeasures.lastMeasure("freno", listOf(p("brake", 4, 250f), p("brake", 3, 255f), p("brake", 1, 260f)))!!
    assertTrue(brake.standalone); assertEquals(now - day, brake.timestamp)
    assertTrue(brake.body, brake.body.startsWith("Tu freno, promedio de tus últimas 3 partidas: zona firme") && brake.body.endsWith("."))
  }

  @Test
  fun `la frase de la tarjeta de un juego sale de las partidas comparables con la ultima`() {
    val pts = listOf(p("trail", 9, 4f), p("trail", 5, 7f), p("trail", 3, 6f), p("trail", 0, 5f))
    assertEquals("Hoy: 5 luces seguidas. Tu mejor marca: 7 luces.", StarMeasures.sentence("secuencia", pts, now))
    val older = listOf(p("trail", 9, 4f), p("trail", 6, 5f), p("trail", 3, 5f))
    assertEquals("la última fue hace 3 días y es la mejor: «En tu última partida»", "En tu última partida: 5 luces seguidas. Es tu mejor marca.", StarMeasures.sentence("secuencia", older, now))
    assertNull("sin medida propia (el juego no es estrella) no hay frase", StarMeasures.sentence("no_existe", pts, now))
    assertNull("sin partidas con la medida, nada", StarMeasures.sentence("secuencia", emptyList(), now))
    assertEquals("dos partidas: la de hoy y nada de «mejor»", "Hoy: 5 luces seguidas.", StarMeasures.sentence("secuencia", listOf(p("trail", 2, 7f), p("trail", 0, 5f)), now))
  }

  @Test
  fun `Freno de Emergencia dice su zona y nunca milisegundos`() {
    val pts = listOf(p("brake", 4, 250f), p("brake", 3, 255f), p("brake", 0, 260f))
    val s = StarMeasures.sentence("freno", pts, now)!!
    assertTrue(s, s.startsWith("Tu freno, promedio de tus últimas 3 partidas: zona firme"))
    assertFalse(s, " ms" in s || "milisegundos" in s)
    assertTrue("«Tus mejores marcas» no lo incluye (siempre en zonas)", StarMeasures.bestMarks(pts).none { it.def.key == "brake" })
  }

  @Test
  fun `tus mejores marcas toma las mas recientes con tres partidas o mas y nunca las comparables de otro nivel`() {
    val pts = listOf(p("glance", 10, 200f), p("glance", 9, 180f), p("glance", 8, 160f), p("glance", 7, 190f)) +                // récord hace 8 días
      listOf(p("trail", 6, 4f), p("trail", 5, 7f), p("trail", 1, 6f)) +                                                         // récord hace 5 días
      listOf(p("tracking", 2, 2f), p("tracking", 1, 3f)) +                                                                    // solo 2 partidas
      listOf(p("bodega", 4, 100f, rating = 0.2f), p("bodega", 3, 70f, rating = 0.8f), p("bodega", 2, 75f, rating = 0.8f), p("bodega", 0, 72f, rating = 0.8f))   // 100% era de un nivel mucho más fácil
    val marks = StarMeasures.bestMarks(pts, max = 5)
    assertEquals("la más reciente primero: trail (hace 5 días), glance (hace 8)", listOf("trail", "glance"), marks.filter { it.def.key != "bodega" }.map { it.def.key })
    assertEquals(160f, marks.first { it.def.key == "glance" }.value, 0f); assertEquals(7f, marks.first { it.def.key == "trail" }.value, 0f)
    assertEquals("de Bodega solo cuentan las partidas de un nivel parecido al de la última: 75, no 100", 75f, marks.first { it.def.key == "bodega" }.value, 0f)
    assertTrue("tracking tiene solo 2 partidas: no entra", marks.none { it.def.key == "tracking" })
    assertEquals("el máximo manda", 1, StarMeasures.bestMarks(pts, max = 1).size)
    assertEquals(emptyList<BestMark>(), StarMeasures.bestMarks(emptyList()))
  }

  @Test
  fun `cuando fue en palabras`() {
    assertEquals("hoy", MeasurePhrases.ago(0)); assertEquals("ayer", MeasurePhrases.ago(1)); assertEquals("hace 6 días", MeasurePhrases.ago(6))
    assertEquals("hace 2 semanas", MeasurePhrases.ago(14)); assertEquals("hace 3 semanas", MeasurePhrases.ago(21)); assertEquals("hace 3 meses", MeasurePhrases.ago(95))
  }
}
