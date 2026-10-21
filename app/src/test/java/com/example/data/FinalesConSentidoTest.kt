package com.example.data

import org.junit.Assert.assertEquals
import org.junit.Assert.assertNotNull
import org.junit.Assert.assertNull
import org.junit.Assert.assertTrue
import org.junit.Test

/** «Finales con sentido» (Etapa 3, `docs/finales-con-sentido.md`): los textos de la tabla de los 16 juegos (los de la app menos Aterrizaje, que tiene los suyos) y «Tu avance» (hoy, tu promedio, tu mejor y su frase) con su tolerancia de «parejo». */
class FinalesConSentidoTest {

  @Test
  fun `la tabla trae los 16 juegos de la etapa 3 y ninguno trae a Aterrizaje`() {
    val ids = FinalesConSentido.gameIds
    assertEquals(16, ids.size)
    assertEquals("sin repetidos", 16, ids.toSet().size)
    assertNull("Aterrizaje Lunar ya está hecho (Tareas 70 a 73) y tiene sus textos en Aterrizaje.kt", FinalesConSentido.copy("aterrizaje"))
    for (id in listOf("parejas", "secuencia", "bodega", "correo", "stroop", "piloto", "freno", "satelites", "radar", "calculo", "engranajes", "acoplamiento", "anagramas", "meteoros", "disparate", "intrusa")) {
      assertNotNull(id, FinalesConSentido.copy(id))
    }
  }

  @Test
  fun `cada juego dice lo que pusiste en juego, su truco, por que importa y su fuente, sin el vocabulario que no va`() {
    for (id in FinalesConSentido.gameIds) {
      val c = FinalesConSentido.copy(id)!!
      for ((nombre, texto) in listOf("put" to c.put, "trick" to c.trick, "why" to c.why, "source" to c.source)) {
        assertTrue("$id: «$nombre» vacío", texto.isNotBlank())
        for (bad in listOf("entren", "cerebro", "cognitiv", "percentil", "diagnostic", "previene", "mejora tu", "fortalece")) {
          assertTrue("$id: «$nombre» dice «$bad»: $texto", !texto.lowercase().contains(bad))
        }
      }
      assertTrue("$id: lo que pusiste en juego empieza con mayúscula", c.put.first().isUpperCase())
      assertTrue("$id: la fuente es una cita corta con su año: ${c.source}", Regex(".*, \\d{4}").matches(c.source))
      val prefix = if (FinalesConSentido.startsWithInfinitive(c.put)) "Pusiste en juego lo que usas para " else "Pusiste en juego "
      assertTrue("$id: «$prefix» y lo que pusiste con su primera letra en minúscula", c.whyText.startsWith(prefix + c.put.first().lowercase() + c.put.drop(1)))
      assertTrue("$id: «por qué importa» sigue a lo que pusiste en juego", c.whyText.endsWith(" " + c.why))
      assertEquals("Fuente: " + c.source, c.sourceText)
    }
  }

  @Test
  fun `el texto de por que importa se arma como pide el documento`() {
    val c = FinalesConSentido.copy("secuencia")!!
    assertEquals(
      "Pusiste en juego la memoria de trabajo: sostener un orden en la cabeza mientras lo usas. La usamos para comprender, razonar y aprender: guarda por un rato lo que necesitamos mientras lo usamos.",
      c.whyText
    )
    assertEquals("Fuente: Baddeley, 2003", c.sourceText)
    assertEquals("Junta las luces de a dos o tres, como cuando dictas un número de teléfono.", c.trick)
    assertEquals("Medida de esta partida. No es un diagnóstico.", FinalesConSentido.NOTE)
  }

  @Test
  fun `si lo que pusiste en juego empieza con un verbo en infinitivo la frase se arma con lo que usas para`() {
    // los 14 que empiezan con infinitivo (Recordar, Frenar, Seguir, Captar, Atender, Hacer, Imaginar, Girar, Encontrar, Reconocer, Comprender, Sacar, Ordenar)
    val infinitives = listOf("bodega", "stroop", "piloto", "freno", "satelites", "radar", "calculo", "engranajes", "acoplamiento", "anagramas", "meteoros", "disparate", "intrusa")
    for (id in infinitives) {
      val c = FinalesConSentido.copy(id)!!
      assertTrue("$id: «${c.put}» empieza con un infinitivo", FinalesConSentido.startsWithInfinitive(c.put))
      assertTrue("$id: ${c.whyText}", c.whyText.startsWith("Pusiste en juego lo que usas para " + c.put.first().lowercase()))
      assertTrue("$id: nunca «Pusiste en juego recordar…»", !c.whyText.startsWith("Pusiste en juego " + c.put.first().lowercase()))
    }
    // los que empiezan con un sustantivo («La memoria…») quedan igual
    for (id in listOf("parejas", "secuencia", "correo")) {
      val c = FinalesConSentido.copy(id)!!
      assertTrue("$id: «${c.put}» NO empieza con un infinitivo", !FinalesConSentido.startsWithInfinitive(c.put))
      assertTrue(c.whyText.startsWith("Pusiste en juego la memoria"))
    }
    assertEquals(
      "Pusiste en juego lo que usas para recordar dónde guardaste cada cosa. Recordar dónde están las cosas es esencial en el día a día, y une tres piezas: la cosa, el lugar y el lazo entre las dos.",
      FinalesConSentido.copy("bodega")!!.whyText
    )
    assertEquals(
      "Pusiste en juego lo que usas para frenar a tiempo una acción que ya empezaste. Detener a tiempo lo que ya no conviene nos permite adaptarnos cuando las cosas cambian de golpe.",
      FinalesConSentido.copy("freno")!!.whyText
    )
    assertEquals(16, infinitives.size + 3)
    assertTrue(FinalesConSentido.startsWithInfinitive("Hacer cuentas con flexibilidad para llegar a un número."))
    assertTrue(!FinalesConSentido.startsWithInfinitive("La memoria de lugar: recordar dónde quedó cada cosa."))
    assertTrue(!FinalesConSentido.startsWithInfinitive("Memoria de trabajo"))
  }

  // ------------------------------------------------------------------ tu avance

  private val tol = 5f

  @Test
  fun `sin partidas anteriores o sin medida hoy no hay con que compararse`() {
    val p = FinalesConSentido.progress(78f, emptyList(), tol)
    assertEquals(78f, p.today!!, 1e-4f)
    assertNull(p.average)
    assertNull(p.best)
    assertEquals(FinalesConSentido.PHRASE_FIRST, p.phrase)
    assertEquals("Primera partida: la próxima te comparas contigo.", FinalesConSentido.phraseWith(p, FinalesConSentido::percentText))
    val q = FinalesConSentido.progress(null, listOf(60f, 70f), tol)
    assertNull(q.today)
    assertEquals(FinalesConSentido.PHRASE_FIRST, q.phrase)
    assertEquals("sin medida hoy no hay frase", "", FinalesConSentido.phraseWith(q, FinalesConSentido::percentText))
  }

  @Test
  fun `mejor que tu mejor anterior es tu mejor partida, aunque sea por poco`() {
    val p = FinalesConSentido.progress(81f, listOf(60f, 80f, 70f), tol)
    assertEquals(FinalesConSentido.PHRASE_BEST, p.phrase)
    assertEquals("¡Tu mejor hasta ahora! Tu promedio: 70 %.", FinalesConSentido.phraseWith(p, FinalesConSentido::percentText))
    assertEquals("el promedio sale de las ANTERIORES", 70f, p.average!!, 1e-4f)
    assertEquals("tu mejor incluye hoy", 81f, p.best!!, 1e-4f)
    // empatar la mejor NO es superarla
    assertEquals(FinalesConSentido.PHRASE_BETTER, FinalesConSentido.progress(80f, listOf(60f, 80f, 70f), tol).phrase)
  }

  @Test
  fun `mejor, parejo o por debajo del promedio segun la tolerancia`() {
    val prev = listOf(60f, 80f, 70f, 90f)                 // promedio 75, mejor 90
    assertEquals(FinalesConSentido.PHRASE_BETTER, FinalesConSentido.progress(85f, prev, tol).phrase)
    assertEquals(FinalesConSentido.PHRASE_EVEN, FinalesConSentido.progress(79f, prev, tol).phrase)
    assertEquals("la tolerancia es de 5 puntos alrededor del promedio, en los dos lados", FinalesConSentido.PHRASE_EVEN, FinalesConSentido.progress(80f, prev, tol).phrase)
    assertEquals(FinalesConSentido.PHRASE_EVEN, FinalesConSentido.progress(70f, prev, tol).phrase)
    assertEquals(FinalesConSentido.PHRASE_BELOW, FinalesConSentido.progress(69f, prev, tol).phrase)
    assertEquals(90f, FinalesConSentido.progress(69f, prev, tol).best!!, 1e-4f)
  }

  @Test
  fun `el rastro tiene media luz de tolerancia`() {
    val prev = listOf(4f, 5f, 4f, 5f, 4f)                // promedio 4,4 y mejor 5
    assertEquals(FinalesConSentido.PHRASE_EVEN, FinalesConSentido.progress(4f, prev, 0.5f).phrase)         // 0,4 por debajo: parejo
    assertEquals(FinalesConSentido.PHRASE_BETTER, FinalesConSentido.progress(5f, prev, 0.5f).phrase)          // 0,6 por encima, sin superar tu mejor (5)
    assertEquals(FinalesConSentido.PHRASE_BEST, FinalesConSentido.progress(6f, prev, 0.5f).phrase)
    assertEquals(FinalesConSentido.PHRASE_BELOW, FinalesConSentido.progress(3f, prev, 0.5f).phrase)
    assertEquals(FinalesConSentido.PHRASE_EVEN, FinalesConSentido.progress(5f, listOf(5f, 5f, 5f, 4f, 5f), 0.5f).phrase)   // promedio 4,8
    // con los números adentro, en luces
    assertEquals("Mejor que tu promedio (4 luces).", FinalesConSentido.phraseWith(FinalesConSentido.progress(5f, prev, 0.5f), FinalesConSentido::lightsText))
    assertEquals("Como tu promedio: vas parejo. Tu mejor: 5 luces.", FinalesConSentido.phraseWith(FinalesConSentido.progress(4f, prev, 0.5f), FinalesConSentido::lightsText))
    assertEquals("Un poco bajo tu promedio (4 luces): es normal que varíe.", FinalesConSentido.phraseWith(FinalesConSentido.progress(3f, prev, 0.5f), FinalesConSentido::lightsText))
  }

  @Test
  fun `si menos es mejor el sentido se invierte`() {
    val prev = listOf(300f, 280f, 320f)                  // promedio 300, mejor (más bajo) 280
    assertEquals(FinalesConSentido.PHRASE_BEST, FinalesConSentido.progress(250f, prev, 10f, lowerIsBetter = true).phrase)
    assertEquals(250f, FinalesConSentido.progress(250f, prev, 10f, lowerIsBetter = true).best!!, 1e-4f)
    assertEquals(FinalesConSentido.PHRASE_BETTER, FinalesConSentido.progress(285f, prev, 10f, lowerIsBetter = true).phrase)
    assertEquals(FinalesConSentido.PHRASE_EVEN, FinalesConSentido.progress(305f, prev, 10f, lowerIsBetter = true).phrase)
    assertEquals(FinalesConSentido.PHRASE_BELOW, FinalesConSentido.progress(330f, prev, 10f, lowerIsBetter = true).phrase)
    // con los números adentro, en milisegundos (menos es mejor)
    assertEquals("Mejor que tu promedio (300 ms). Tu mejor: 280 ms.", FinalesConSentido.phraseWith(FinalesConSentido.progress(285f, prev, 10f, lowerIsBetter = true), FinalesConSentido::msText))
  }

  @Test
  fun `las partidas anteriores salen de las medidas guardadas sin contar la de hoy y solo las comparables`() {
    val today = MeasurePoint(timestamp = 500L, key = "place", value = 70f, rating = 0.5f, timed = true)
    val saved = listOf(
      MeasurePoint(100L, "place", 60f, 0.5f, true),
      MeasurePoint(200L, "place", 80f, 0.5f, true),
      MeasurePoint(300L, "place", 10f, 0.5f, false),        // otro reloj: no se compara
      MeasurePoint(400L, "bodega", 99f, 0.5f, true),        // otra medida
      MeasurePoint(500L, "place", 70f, 0.5f, true)          // la de hoy, ya guardada: no se cuenta dos veces
    )
    assertEquals(listOf(60f, 80f), FinalesConSentido.previousValues(saved, today))
    assertEquals("si la de hoy todavía no se guardó, da lo mismo", listOf(60f, 80f), FinalesConSentido.previousValues(saved.dropLast(1), today))
    assertEquals(emptyList<Float>(), FinalesConSentido.previousValues(emptyList(), today))
    val many = (1..15).map { MeasurePoint(it * 100L, "place", it.toFloat(), -1f, null) }
    assertEquals("solo las últimas 10", (6..15).map { it.toFloat() }, FinalesConSentido.previousValues(many, MeasurePoint(10_000L, "place", 5f, -1f, null)))
  }

  @Test
  fun `los numeros de los cuadros se dicen con la unidad del juego y sin dato dicen raya`() {
    assertEquals("78 %", FinalesConSentido.percentText(77.9f))
    assertEquals("—", FinalesConSentido.percentText(null))
    assertEquals("6 luces", FinalesConSentido.lightsText(6f))
    assertEquals("1 luz", FinalesConSentido.lightsText(1f))
    assertEquals("5 luces", FinalesConSentido.lightsText(4.6f))
    assertEquals("—", FinalesConSentido.lightsText(null))
    assertEquals("2,6 satélites", FinalesConSentido.satellitesText(2.6f))
    assertEquals("—", FinalesConSentido.satellitesText(null))
    assertEquals("142 por minuto", FinalesConSentido.ppmText(141.6f))
    assertEquals("—", FinalesConSentido.ppmText(null))
  }

  @Test
  fun `las cinco frases con los numeros adentro, para una medida en porcentaje, una en luces, una de menos-es-mejor y la zona del freno`() {
    val pct = FinalesConSentido::percentText
    val prev = listOf(60f, 80f, 70f, 90f)                 // promedio 75, mejor 90
    assertEquals("Primera partida: la próxima te comparas contigo.", FinalesConSentido.phraseWith(FinalesConSentido.progress(78f, emptyList(), 5f), pct))
    assertEquals("Mejor que tu promedio (75 %). Tu mejor: 90 %.", FinalesConSentido.phraseWith(FinalesConSentido.progress(85f, prev, 5f), pct))
    assertEquals("Como tu promedio (75 %): vas parejo. Tu mejor: 90 %.", FinalesConSentido.phraseWith(FinalesConSentido.progress(79f, prev, 5f), pct))
    assertEquals("Un poco bajo tu promedio (75 %): es normal que varíe.", FinalesConSentido.phraseWith(FinalesConSentido.progress(69f, prev, 5f), pct))
    assertEquals("¡Tu mejor hasta ahora! Tu promedio: 75 %.", FinalesConSentido.phraseWith(FinalesConSentido.progress(95f, prev, 5f), pct))
    // la zona del freno (menos milisegundos es mejor)
    val zone: (Float?) -> String = { v -> if (v == null) "—" else "zona " + Brake.zone(v).label }
    assertEquals("Como tu promedio: vas parejo.", FinalesConSentido.phraseWith(FinalesConSentido.progress(245f, listOf(260f, 250f, 240f, 270f), 20f, lowerIsBetter = true), zone))
    // el récord con su sello propio se dice una sola vez (arriba): la frase usa la comparación con el promedio
    assertEquals("Mejor que tu promedio (75 %).", FinalesConSentido.phraseWith(FinalesConSentido.progress(95f, prev, 5f, avoidBest = true), pct))
  }

  @Test
  fun `ninguna frase, unidad ni texto de la ficha toca el vocabulario de la culpa`() {
    val rendered = listOf(
      FinalesConSentido.PHRASE_FIRST, FinalesConSentido.PHRASE_BEST, FinalesConSentido.PHRASE_BETTER, FinalesConSentido.PHRASE_BELOW, FinalesConSentido.PHRASE_EVEN
    ).map { it.replace("{promedio}", "4 luces").replace("{mejor}", "5 luces") }
    for (t in rendered) {
      assertEquals(t, null, com.example.NoLossGuard.hit(t, com.example.NoLossGuard.literals))
      assertEquals(t, null, com.example.NoLossGuard.hit(t, com.example.NoLossGuard.reminderRoots))
    }
    // los textos de la ficha («Qué pones en juego») de los 17. «rápido» queda fuera de las raíces: en la ficha de ¿Verdad o disparate? DESCRIBE lo que se pone en juego («Comprender frases rápido y decidir si son ciertas», aprobado el 10-oct), no apura a nadie
    val roots = com.example.NoLossGuard.reminderRoots - "rápido"
    for (id in FinalesConSentido.gameIds + "aterrizaje") {
      val (text, source) = FinalesConSentido.sheetFor(id)!!
      for (t in listOf(text, source)) {
        assertEquals("$id: «$t»", null, com.example.NoLossGuard.hit(t, com.example.NoLossGuard.literals))
        assertEquals("$id: «$t»", null, com.example.NoLossGuard.hit(t, roots))
      }
    }
  }

  @Test
  fun `el texto de la ficha va en presente con las dos reglas, infinitivo y sustantivo, y trae su fuente (los 17)`() {
    assertEquals(
      "Pones en juego la memoria de trabajo: sostener un orden en la cabeza mientras lo usas. La usamos para comprender, razonar y aprender: guarda por un rato lo que necesitamos mientras lo usamos.",
      FinalesConSentido.copy("secuencia")!!.sheetText
    )
    assertEquals(
      "Pones en juego lo que usas para recordar dónde guardaste cada cosa. Recordar dónde están las cosas es esencial en el día a día, y une tres piezas: la cosa, el lugar y el lazo entre las dos.",
      FinalesConSentido.copy("bodega")!!.sheetText
    )
    for (id in FinalesConSentido.gameIds) {
      val c = FinalesConSentido.copy(id)!!
      val prefix = if (FinalesConSentido.startsWithInfinitive(c.put)) "Pones en juego lo que usas para " else "Pones en juego "
      assertTrue("$id: ${c.sheetText}", c.sheetText.startsWith(prefix + c.put.first().lowercase() + c.put.drop(1)) && c.sheetText.endsWith(" " + c.why))
      assertEquals("$id: lo mismo que el final, pero en presente", c.whyText.removePrefix("Pusiste en juego"), c.sheetText.removePrefix("Pones en juego"))
      assertEquals(c.sheetText to c.sourceText, FinalesConSentido.sheetFor(id))
    }
    // Aterrizaje Lunar trae los suyos en Aterrizaje.kt, en presente
    val ater = FinalesConSentido.sheetFor("aterrizaje")!!
    assertEquals(Aterrizaje.SHEET_TEXT, ater.first)
    assertEquals(Aterrizaje.SOURCE, ater.second)
    assertTrue(ater.first.startsWith("Pones en juego la estimación en proporción."))
    assertEquals("lo mismo que el final, pero en presente", Aterrizaje.WHY.removePrefix("Pusiste"), Aterrizaje.SHEET_TEXT.removePrefix("Pones"))
    assertNull(FinalesConSentido.sheetFor("no_existe"))
    assertEquals("los 17 de la app", 17, (FinalesConSentido.gameIds + "aterrizaje").mapNotNull { FinalesConSentido.sheetFor(it) }.size)
  }

  @Test
  fun `cuantas partidas del juego hubo antes de esta sale de lo guardado y solo cuenta las anteriores de ese juego`() {
    fun r(id: String, t: Long) = com.example.model.GamePlayResult(gameId = id, score = 1, correctAnswers = 1, totalTrials = 1, timed = false, level = 1, timestamp = t)
    val history = listOf(r("secuencia", 100), r("secuencia", 200), r("bodega", 250), r("secuencia", 300), r("secuencia", 400))
    assertEquals(0, FinalesConSentido.playsBefore(emptyList(), "secuencia", 100))
    assertEquals(0, FinalesConSentido.playsBefore(history, "secuencia", 100))
    assertEquals(2, FinalesConSentido.playsBefore(history, "secuencia", 300))
    assertEquals("la de hoy, ya guardada, no se cuenta a sí misma", 3, FinalesConSentido.playsBefore(history, "secuencia", 400))
    assertEquals("si todavía no se guardó, da lo mismo", 4, FinalesConSentido.playsBefore(history, "secuencia", 500))
    assertEquals("otro juego: su propio conteo", 0, FinalesConSentido.playsBefore(history, "bodega", 250))
    assertEquals("«¿Por qué importa?» se ve en las 3 primeras", 3, FinalesConSentido.WHY_INLINE_PLAYS)
  }

  @Test
  fun `la comparacion es solo contigo y ninguna frase habla de otras personas ni de percentiles`() {
    for (t in listOf(FinalesConSentido.PHRASE_FIRST, FinalesConSentido.PHRASE_BEST, FinalesConSentido.PHRASE_BETTER, FinalesConSentido.PHRASE_BELOW, FinalesConSentido.PHRASE_EVEN, FinalesConSentido.NOTE)) {
      assertTrue(t, !t.contains("percentil", true) && !t.contains("otras personas", true) && !t.contains("la mayoría", true))
    }
  }

  @Test
  fun `la frase omite lo que repite la cifra de hoy`() {
    // Opus, 21-oct: con medidas por categorías (Freno: «zona firme») el promedio y el mejor dicen lo mismo que hoy y la frase quedaba «Como tu promedio (zona firme): vas parejo. Tu mejor: zona firme.»
    val zona: (Float?) -> String = { v -> if (v == null) "—" else "zona firme" }
    val parejo = FinalesConSentido.progress(260f, listOf(255f, 262f, 258f), 10f, lowerIsBetter = true)
    assertEquals("Como tu promedio: vas parejo.", FinalesConSentido.phraseWith(parejo, zona))
    // si solo el mejor es igual a hoy, sale solo «Tu mejor»; si solo el promedio, sale solo el paréntesis
    val pct = FinalesConSentido::percentText
    assertEquals("Mejor que tu promedio (75 %).", FinalesConSentido.phraseWith(FinalesConSentido.progress(90f, listOf(60f, 90f), 5f, avoidBest = true), pct))
    assertEquals("Como tu promedio: vas parejo. Tu mejor: 90 %.", FinalesConSentido.phraseWith(FinalesConSentido.progress(75f, listOf(60f, 90f), 5f), pct))
    // lo que NO repite se queda como estaba
    assertEquals("Como tu promedio (75 %): vas parejo. Tu mejor: 90 %.", FinalesConSentido.phraseWith(FinalesConSentido.progress(79f, listOf(60f, 90f), 5f), pct))
    assertEquals("¡Tu mejor hasta ahora! Tu promedio: 75 %.", FinalesConSentido.phraseWith(FinalesConSentido.progress(95f, listOf(60f, 90f), 5f), pct))
    assertEquals("Primera partida: la próxima te comparas contigo.", FinalesConSentido.phraseWith(FinalesConSentido.progress(78f, emptyList(), 5f), pct))
  }
}

