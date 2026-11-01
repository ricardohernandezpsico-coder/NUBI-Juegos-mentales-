package com.example.data

import org.junit.Assert.assertEquals
import org.junit.Test

/** `glueNumbers`: el espacio entre un número y su unidad, y entre «de» y una cifra, es duro (U+00A0); nada más cambia. */
class NoBreakTest {
  @Test
  fun pega_el_numero_a_su_unidad() {
    assertEquals("21${NBSP}días", "21 días".glueNumbers())
    assertEquals("41${NBSP}partidas", "41 partidas".glueNumbers())
    assertEquals("Tu mejor racha: 21${NBSP}días", "Tu mejor racha: 21 días".glueNumbers())
    assertEquals("siguiente: 30${NBSP}días", "siguiente: 30 días".glueNumbers())
    assertEquals("desde el 8${NBSP}de octubre", "desde el 8 de octubre".glueNumbers())
  }

  @Test
  fun pega_el_numero_a_su_unidad_en_las_cantidades_con_de() {
    // «de 15 medallas»: el 15 con «medallas»; «de» puede quedar solo al final de un renglón, pero «15» nunca se separa de «medallas»
    assertEquals("de 15${NBSP}medallas", "de 15 medallas".glueNumbers())
    assertEquals("5${NBSP}de 8", "5 de 8".glueNumbers())
    assertEquals("17${NBSP}de 17", "17 de 17".glueNumbers())
  }

  @Test
  fun pega_de_al_ano_que_le_sigue() {
    assertEquals("desde el 3${NBSP}de octubre de${NBSP}2025", "desde el 3 de octubre de 2025".glueNumbers())
    assertEquals("de${NBSP}1999", "de 1999".glueNumbers())
  }

  @Test
  fun lo_demas_queda_igual() {
    for (s in listOf("probados al menos una vez", "días seguidos", "Tu racha empieza con tu próxima partida", "desde el origen", "siguiente: volver una vez", "Racha: ", "a las 19:00."))
      assertEquals(s, s.glueNumbers())
  }

  @Test
  fun es_idempotente() {
    val once = "Tu mejor racha: 21 días, desde el 8 de octubre".glueNumbers()
    assertEquals(once, once.glueNumbers())
  }

  // ------------------------------------------------------------------ los nombres de la página «Contrarreloj» (Tarea 203b)

  @Test
  fun los_nombres_de_las_divisiones_no_se_parten() {
    assertEquals("para En${NBSP}la${NBSP}Luna.", "para En la Luna.".glueNames())
    assertEquals("Entre${NBSP}estrellas", "Entre estrellas".glueNames())
    assertEquals("En${NBSP}la${NBSP}plataforma", "En la plataforma".glueNames())
    assertEquals("Llevas 3 semanas seguidas En${NBSP}órbita o más arriba.", "Llevas 3 semanas seguidas En órbita o más arriba.".glueNames())
    assertEquals("una sola palabra queda igual", "Despegando", "Despegando".glueNames())
  }

  @Test
  fun la_franja_de_edad_va_entera_con_su_unidad() {
    assertEquals("personas de 45${NBSP}a${NBSP}54${NBSP}años quedan detrás", "personas de 45 a 54 años quedan detrás".glueNames())
    assertEquals("de 75${NBSP}años${NBSP}o${NBSP}más", "de 75 años o más".glueNames())
    assertEquals("30${NBSP}a${NBSP}54${NBSP}años", "30 a 54 años".glueNames())
    assertEquals("65${NBSP}años${NBSP}o${NBSP}más", "65 años o más".glueNames())
  }

  @Test
  fun te_faltan_va_con_su_cifra_y_su_unidad() {
    assertEquals("Te${NBSP}faltan${NBSP}7${NBSP}aciertos para En${NBSP}la${NBSP}Luna.", "Te faltan 7 aciertos para En la Luna.".glueNames())
    assertEquals("Te${NBSP}falta${NBSP}1${NBSP}acierto para En${NBSP}órbita.", "Te falta 1 acierto para En órbita.".glueNames())
  }

  @Test
  fun glueNames_es_idempotente_y_no_toca_lo_demas() {
    val s = "Esta semana estás En órbita. Te faltan 7 aciertos para En la Luna. Con las 128 personas de 45 a 54 años.".glueNames()
    assertEquals(s, s.glueNames())
    for (x in listOf("Tu récord es del 17 de octubre", "una frase sin nombres", "de 2 a 3 veces")) assertEquals(x, x.glueNames())
  }
}
