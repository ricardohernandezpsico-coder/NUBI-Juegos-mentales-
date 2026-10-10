package com.example.data

import org.junit.Assert.assertEquals
import org.junit.Assert.assertNotNull
import org.junit.Assert.assertNull
import org.junit.Assert.assertTrue
import org.junit.Test

/** «Finales con sentido» (Etapa 3, `docs/finales-con-sentido.md`): los textos de la tabla de los 17 juegos y «Tu avance» (hoy, tu promedio, tu mejor y su frase) con su tolerancia de «parejo». */
class FinalesConSentidoTest {

  @Test
  fun `la tabla trae los 17 juegos de la etapa 3 y ninguno trae a Aterrizaje`() {
    val ids = FinalesConSentido.gameIds
    assertEquals(17, ids.size)
    assertEquals("sin repetidos", 17, ids.toSet().size)
    assertNull("Aterrizaje Lunar ya está hecho (Tareas 70 a 73) y tiene sus textos en Aterrizaje.kt", FinalesConSentido.copy("aterrizaje"))
    for (id in listOf("parejas", "secuencia", "bodega", "correo", "stroop", "piloto", "freno", "satelites", "radar", "calculo", "engranajes", "acoplamiento", "anagramas", "meteoros", "disparate", "cosecha", "intrusa")) {
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
    val infinitives = listOf("bodega", "stroop", "piloto", "freno", "satelites", "radar", "calculo", "engranajes", "acoplamiento", "anagramas", "meteoros", "disparate", "cosecha", "intrusa")
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
    assertEquals(17, infinitives.size + 3)
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
    assertEquals("Juega otra vez para ver tu avance.", p.phrase)
    val q = FinalesConSentido.progress(null, listOf(60f, 70f), tol)
    assertNull(q.today)
    assertEquals("Juega otra vez para ver tu avance.", q.phrase)
  }

  @Test
  fun `mejor que tu mejor anterior es tu mejor partida, aunque sea por poco`() {
    val p = FinalesConSentido.progress(81f, listOf(60f, 80f, 70f), tol)
    assertEquals("¡Tu mejor partida hasta ahora!", p.phrase)
    assertEquals("el promedio sale de las ANTERIORES", 70f, p.average!!, 1e-4f)
    assertEquals("tu mejor incluye hoy", 81f, p.best!!, 1e-4f)
    // empatar la mejor NO es superarla
    assertEquals("Hoy te fue mejor que tu promedio.", FinalesConSentido.progress(80f, listOf(60f, 80f, 70f), tol).phrase)
  }

  @Test
  fun `mejor, parejo o por debajo del promedio segun la tolerancia`() {
    val prev = listOf(60f, 80f, 70f, 90f)                 // promedio 75, mejor 90
    assertEquals("Hoy te fue mejor que tu promedio.", FinalesConSentido.progress(85f, prev, tol).phrase)
    assertEquals("Igual que tu promedio: vas parejo.", FinalesConSentido.progress(79f, prev, tol).phrase)
    assertEquals("la tolerancia es de 5 puntos alrededor del promedio, en los dos lados", "Igual que tu promedio: vas parejo.", FinalesConSentido.progress(80f, prev, tol).phrase)
    assertEquals("Igual que tu promedio: vas parejo.", FinalesConSentido.progress(70f, prev, tol).phrase)
    assertEquals("Un poco por debajo de tu promedio: es normal que varíe.", FinalesConSentido.progress(69f, prev, tol).phrase)
    assertEquals(90f, FinalesConSentido.progress(69f, prev, tol).best!!, 1e-4f)
  }

  @Test
  fun `el rastro tiene media luz de tolerancia`() {
    val prev = listOf(4f, 5f, 4f, 5f, 4f)                // promedio 4,4 y mejor 5
    assertEquals("Igual que tu promedio: vas parejo.", FinalesConSentido.progress(4f, prev, 0.5f).phrase)         // 0,4 por debajo: parejo
    assertEquals("Hoy te fue mejor que tu promedio.", FinalesConSentido.progress(5f, prev, 0.5f).phrase)          // 0,6 por encima, sin superar tu mejor (5)
    assertEquals("¡Tu mejor partida hasta ahora!", FinalesConSentido.progress(6f, prev, 0.5f).phrase)
    assertEquals("Un poco por debajo de tu promedio: es normal que varíe.", FinalesConSentido.progress(3f, prev, 0.5f).phrase)
    assertEquals("Igual que tu promedio: vas parejo.", FinalesConSentido.progress(5f, listOf(5f, 5f, 5f, 4f, 5f), 0.5f).phrase)   // promedio 4,8
  }

  @Test
  fun `si menos es mejor el sentido se invierte`() {
    val prev = listOf(300f, 280f, 320f)                  // promedio 300, mejor (más bajo) 280
    assertEquals("¡Tu mejor partida hasta ahora!", FinalesConSentido.progress(250f, prev, 10f, lowerIsBetter = true).phrase)
    assertEquals(250f, FinalesConSentido.progress(250f, prev, 10f, lowerIsBetter = true).best!!, 1e-4f)
    assertEquals("Hoy te fue mejor que tu promedio.", FinalesConSentido.progress(285f, prev, 10f, lowerIsBetter = true).phrase)
    assertEquals("Igual que tu promedio: vas parejo.", FinalesConSentido.progress(305f, prev, 10f, lowerIsBetter = true).phrase)
    assertEquals("Un poco por debajo de tu promedio: es normal que varíe.", FinalesConSentido.progress(330f, prev, 10f, lowerIsBetter = true).phrase)
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
  }

  @Test
  fun `la comparacion es solo contigo y ninguna frase habla de otras personas ni de percentiles`() {
    for (t in listOf(FinalesConSentido.PHRASE_FIRST, FinalesConSentido.PHRASE_BEST, FinalesConSentido.PHRASE_BETTER, FinalesConSentido.PHRASE_BELOW, FinalesConSentido.PHRASE_EVEN, FinalesConSentido.NOTE)) {
      assertTrue(t, !t.contains("percentil", true) && !t.contains("otras personas", true) && !t.contains("la mayoría", true))
    }
  }
}
