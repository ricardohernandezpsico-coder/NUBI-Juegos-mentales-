package com.example.data

import org.junit.Assert.assertEquals
import org.junit.Assert.assertFalse
import org.junit.Assert.assertNull
import org.junit.Assert.assertTrue
import org.junit.Test

/** «La estación de correo»: la lectura de lo que manda Unity (memoria para lo pendiente), el consejo, el récord y la traducción del rating del vuelo viejo. Lógica pura. */
class MailTest {
  @Test
  fun `los encargos cumplidos suman evento, hora y cancelados que no se hicieron`() {
    // 3 de 4 por evento, 1 de 2 por hora, 2 cancelados de los que se hizo 1 igual: 3 + 1 + 1 = 5 de 4 + 2 + 2 = 8.
    assertEquals(5, Mail.done(3, 1, 2, 1))
    assertEquals(8, Mail.total(4, 2, 2))
    // hacer más cancelados de los que hubo no resta de más
    assertEquals(3, Mail.done(3, 0, 1, 5))
    assertEquals(0, Mail.done(-2, -1, 0, 0))
  }

  @Test
  fun `el porcentaje y la marca usan solo los encargos de la partida`() {
    assertEquals(63, Mail.percent(5, 8))
    assertEquals(62.5f, Mail.mark(5, 8)!!, 1e-3f)
    assertEquals(100, Mail.percent(9, 8))       // nunca pasa de 100
    assertNull(Mail.percent(0, 0))
    assertNull(Mail.mark(0, 0))
    assertNull(Mail.percent(null, 5))
    assertEquals("Tu memoria para lo pendiente: 63 %", Mail.headline(5, 8))
    assertEquals("Tu memoria para lo pendiente: —", Mail.headline(0, 0))
  }

  @Test
  fun `las lineas de la pantalla final dicen cada dato una vez y callan lo que no hubo`() {
    assertEquals("8 de 11 encargos cumplidos", Mail.detailLine(8, 11))
    assertEquals("1 de 1 encargo cumplido", Mail.detailLine(1, 1))
    assertEquals("No hubo encargos que recordar", Mail.detailLine(0, 0))
    assertEquals("Por evento (cartas señal): 3 de 4", Mail.eventLine(3, 4))
    assertNull(Mail.eventLine(0, 0))
    assertEquals("Por hora (faro): 1 de 2", Mail.timeLine(1, 2))
    assertNull(Mail.timeLine(null, null))
    assertEquals("Cancelados que no hiciste: 1 de 2", Mail.cancelLine(2, 1))
    assertNull(Mail.cancelLine(0, 0))
    assertEquals("Miradas al reloj cerca de la hora: 2 de 3", Mail.clockLine(2, 3))
    assertNull(Mail.clockLine(0, 0))
    assertEquals("Cartas bien puestas: 20", Mail.cardsLine(20))
    assertNull(Mail.cardsLine(-1))
    assertEquals("Etapa más alta: 3 de 5 (el lazo)", Mail.groupLine(3))
    assertEquals("Etapa más alta: 1 de 5 (sellos y la hora)", Mail.groupLine(1))
    assertEquals("Etapa más alta: 5 de 5 (el día completo)", Mail.groupLine(5))
    assertNull(Mail.groupLine(0))
    assertNull(Mail.groupLine(6))
  }

  @Test
  fun `cada grupo de etapas tiene su nombre y son tantos como grupos`() {
    assertEquals(Mail.GROUPS, Mail.GROUP_NAMES.size)
    assertEquals(listOf("sellos y la hora", "lo diario y la radio", "el lazo", "dos horas al día", "el día completo"), Mail.GROUP_NAMES)
    for (g in 1..Mail.GROUPS) assertTrue(Mail.groupLine(g)!!.endsWith("(" + Mail.GROUP_NAMES[g - 1] + ")"))
    assertTrue("los nombres no se repiten", Mail.GROUP_NAMES.toSet().size == Mail.GROUPS)
  }

  @Test
  fun `el record solo se celebra si Unity dice que se supero`() {
    assertNull(Mail.recordLine(0, true))
    assertNull(Mail.recordLine(null, null))
    assertEquals("¡Nuevo récord! 24 cartas en un día perfecto", Mail.recordLine(24, true))
    assertEquals("Tu mejor día perfecto: 24 cartas", Mail.recordLine(24, false))
    assertEquals("Tu mejor día perfecto: 24 cartas", Mail.recordLine(24, null))
  }

  @Test
  fun `el consejo es un truco segun lo que mas se escapo y calla si todo salio bien`() {
    // lo cancelado que se hizo igual va primero
    assertTrue(Mail.tip(4, 4, 2, 2, 1, 2, 2)!!.startsWith("Truco: "))
    assertTrue(Mail.tip(4, 4, 2, 2, 1, 2, 2)!!.contains("hoy no lo hago"))
    // una hora que se escapó sin mirar el reloj: mirar el reloj; mirándolo bien: imaginarse haciéndolo
    assertTrue(Mail.tip(4, 4, 1, 2, 0, 0, 0)!!.contains("mira el reloj"))
    assertTrue(Mail.tip(4, 4, 1, 2, 0, 3, 1)!!.contains("mira el reloj"))
    assertEquals(Mail.TIP_IMAGINE_BEACON, Mail.tip(4, 4, 1, 2, 0, 3, 3))
    // las cartas señal que se escaparon: sin el desglose (una versión vieja de Unity), el truco habla de «una carta con señal»
    assertEquals(Mail.TIP_CUE_GENERIC, Mail.tip(2, 4, 2, 2, 0, 1, 1))
    // todo bien o sin encargos: ningún consejo
    assertNull(Mail.tip(4, 4, 2, 2, 0, 1, 1))
    assertNull(Mail.tip(0, 0, 0, 0, 0, 0, 0))
    assertNull(Mail.tip(null, null, null, null, null, null, null))
  }

  @Test
  fun `el truco de las cartas senal nombra la senal que de verdad se escapo`() {
    // etapa 2: solo trae sello dorado → el truco habla del sello dorado y nunca de un lazo que el jugador no vio
    val etapa2 = Mail.tip(1, 2, 0, 0, 0, 0, 0, goldMissed = 1, lazoMissed = 0)!!
    assertEquals(Mail.TIP_GOLD, etapa2)
    assertTrue(etapa2.contains("sello dorado"))
    assertFalse(etapa2.contains("lazo"))
    // etapa 5: solo trae lazo → el truco habla del lazo
    val etapa5 = Mail.tip(0, 2, 0, 0, 0, 0, 0, goldMissed = 0, lazoMissed = 2)!!
    assertEquals(Mail.TIP_LAZO, etapa5)
    assertTrue(etapa5.contains("lazo"))
    assertFalse(etapa5.contains("sello dorado"))
    // si se escaparon las dos, la que más veces
    assertEquals(Mail.TIP_LAZO, Mail.tip(1, 5, 0, 0, 0, 0, 0, goldMissed = 1, lazoMissed = 3))
    assertEquals(Mail.TIP_GOLD, Mail.tip(1, 5, 0, 0, 0, 0, 0, goldMissed = 3, lazoMissed = 1))
    assertEquals("empate: el sello dorado, la primera señal que se aprende", Mail.TIP_GOLD, Mail.tip(1, 5, 0, 0, 0, 0, 0, goldMissed = 2, lazoMissed = 2))
  }

  @Test
  fun `el truco de la hora nombra la accion concreta`() {
    assertEquals("Truco: imagínate tocando el faro cuando llegue la hora.", Mail.TIP_IMAGINE_BEACON)
    // miró bien el reloj pero no encendió el faro a tiempo
    assertEquals(Mail.TIP_IMAGINE_BEACON, Mail.tip(4, 4, 1, 2, 0, 3, 3))
    assertFalse(Mail.TIP_IMAGINE_BEACON.contains("encargo"))
  }

  @Test
  fun `el consejo se separa de la medida con la marca Truco`() {
    val t = Mail.tip(4, 4, 1, 2, 0, 0, 0)!!
    assertEquals(t.removePrefix("Truco: ").replaceFirstChar { it.uppercase() }, ResultAdvice.tipOf(t))
  }

  @Test
  fun `el record guardado nunca baja ni es negativo`() {
    assertEquals(0, Mail.mergeRecord(0, null))
    assertEquals(12, Mail.mergeRecord(12, null))
    assertEquals(12, Mail.mergeRecord(12, 7))
    assertEquals(20, Mail.mergeRecord(12, 20))
    assertEquals(0, Mail.mergeRecord(-3, -5))
  }

  @Test
  fun `el rating del vuelo viejo se lleva a la mitad y sin dato no se toca`() {
    assertEquals(0.45f, Mail.translateOldRating(0.9f), 1e-6f)
    assertEquals(0.25f, Mail.translateOldRating(0.5f), 1e-6f)
    assertEquals(0f, Mail.translateOldRating(0f), 0f)
    assertEquals(-1f, Mail.translateOldRating(-1f), 0f)
    assertFalse(Mail.translateOldRating(1f) > 0.5f)
  }

  @Test
  fun `lo que se oye para lectores de pantalla es el detalle sin simbolos`() {
    assertEquals("5 de 8 encargos cumplidos", Mail.spoken(5, 8))
    assertEquals("No hubo encargos que recordar", Mail.spoken(null, null))
  }
}
