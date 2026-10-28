package com.example.data

import org.junit.Assert.assertEquals
import org.junit.Assert.assertNull
import org.junit.Assert.assertTrue
import org.junit.Test
import java.util.Calendar

/**
 * Los cuatro cuadros de Tú (Tarea 185; `TuStats`): lo que dicen, con su unidad, en singular y plural, sin culpa y como una frase para el lector de pantalla. Datos reales, ninguno inventado: salen de la racha, el historial, las medallas y los juegos probados.
 */
class TuStatsTest {
  private fun at(y: Int, m: Int, d: Int) = Calendar.getInstance().apply { set(y, m, d, 12, 0, 0) }.timeInMillis
  private val now = at(2026, Calendar.OCTOBER, 28)

  /** Lo que se ve lleva espacios duros entre el número y su unidad; para comparar con lo que dice una persona, se vuelven normales. */
  private fun plain(s: String?) = s?.replace(NBSP, " ")

  private fun tiles(streak: Int = 7, best: Int = 12, games: Int = 41, first: Long? = at(2026, Calendar.OCTOBER, 3), medals: Int = 9, distinct: Int = 15) =
    TuStats.tiles(streak, best, games, first, now, medals, 15, distinct, 17)

  @Test
  fun los_cuatro_cuadros_dicen_lo_que_pidio_Ricardo() {
    val t = tiles()
    assertEquals(listOf("racha", "partidas", "medallas", "juegos"), t.map { it.key })
    assertEquals(listOf("7", "41", "9", "15"), t.map { it.figure })
    assertEquals("7 días seguidos", plain(t[0].figure + " " + t[0].unit))
    assertEquals("Tu mejor racha: 12 días", plain(t[0].support))
    assertEquals("41 partidas", plain(t[1].figure + " " + t[1].unit))
    assertEquals("desde el 3 de octubre", plain(t[1].support))
    assertEquals("9 de 15 medallas", plain(t[2].figure + " " + t[2].unit))
    assertNull("las medallas no llevan línea de apoyo", t[2].support)
    assertEquals("15 de 17 juegos", plain(t[3].figure + " " + t[3].unit))
    assertEquals("probados al menos una vez", t[3].support)
  }

  @Test
  fun el_lector_de_pantalla_lee_cada_cuadro_como_una_frase() {
    val t = tiles()
    assertEquals("Racha: 7 días seguidos. Tu mejor racha: 12 días.", t[0].spoken)
    assertEquals("Partidas: 41 partidas, desde el 3 de octubre.", t[1].spoken)
    assertEquals("Medallas: 9 de 15 medallas.", t[2].spoken)
    assertEquals("Juegos: 15 de 17 juegos, probados al menos una vez.", t[3].spoken)
  }

  @Test
  fun el_singular_se_dice_bien() {
    val t = tiles(streak = 1, best = 1, games = 1, medals = 1, distinct = 1)
    assertEquals("día seguido", t[0].unit); assertEquals("Tu mejor racha: 1 día", plain(t[0].support))
    assertEquals("partida", t[1].unit)
    assertEquals("de 15 medallas", plain(t[2].unit))
    assertEquals("Racha: 1 día seguido. Tu mejor racha: 1 día.", t[0].spoken)
  }

  @Test
  fun la_mejor_racha_nunca_baja_de_la_de_hoy() {
    assertEquals("Tu mejor racha: 5 días", plain(tiles(streak = 5, best = 3)[0].support))
  }

  @Test
  fun sin_partidas_ni_racha_nada_culpa_y_dice_cuando_empieza() {
    val t = tiles(streak = 0, best = 0, games = 0, first = null, medals = 0, distinct = 0)
    assertEquals(listOf("0", "0", "0", "0"), t.map { it.figure })
    assertEquals("Tu racha empieza con tu próxima partida", t[0].support)
    assertEquals("Tu primera partida te espera", t[1].support)
    assertEquals("días seguidos", t[0].unit); assertEquals("partidas", t[1].unit)
    // una racha de 0 con una mejor de antes: no dice «perdiste», dice cuál fue la mejor
    assertEquals("Tu mejor racha: 12 días", plain(tiles(streak = 0, best = 12)[0].support))
  }

  @Test
  fun la_fecha_de_la_primera_partida_lleva_el_ano_solo_si_no_es_este() {
    assertEquals("3 de octubre", TuStats.since(at(2026, Calendar.OCTOBER, 3), now))
    assertEquals("3 de octubre de 2025", TuStats.since(at(2025, Calendar.OCTOBER, 3), now))
    assertTrue(plain(tiles(first = at(2025, Calendar.DECEMBER, 31))[1].support)!!.endsWith("31 de diciembre de 2025"))
  }

  @Test
  fun ninguna_frase_trae_palabras_sueltas_ni_signos() {
    for (t in tiles() + tiles(streak = 0, best = 0, games = 0, first = null, medals = 0, distinct = 0)) {
      val all = listOf(t.figure, t.unit, t.support.orEmpty(), t.spoken).joinToString(" ")
      assertTrue("«$all» no lleva flechas ni más o menos", listOf("→", "↑", "↓", "+", "−").none { it in all })
    }
  }

  @Test
  fun el_numero_nunca_se_separa_de_su_unidad_en_lo_que_se_ve() {
    // Ricardo, 28-oct: «Tu mejor racha: 21 días» se partió entre «21» y «días». Entre una cifra y la palabra que la sigue, y entre «de» y una cifra, el espacio es duro (U+00A0)
    val t = tiles()
    assertEquals("Tu mejor racha: 12${NBSP}días", t[0].support)
    assertEquals("desde el 3${NBSP}de octubre", t[1].support)
    assertEquals("de 15${NBSP}medallas", t[2].unit)
    assertEquals("de 17${NBSP}juegos", t[3].unit)
    val soft = Regex("""(\d) \p{L}|\bde (?:19|20)\d\d""")
    for (x in tiles() + tiles(streak = 1, best = 1, games = 1, medals = 1, distinct = 1) + tiles(first = at(2025, Calendar.OCTOBER, 3)))
      for (s in listOf(x.figure, x.unit, x.support.orEmpty())) assertTrue("«$s» deja un espacio normal entre un número y su unidad", !soft.containsMatchIn(s))
    // y lo que lee el lector de pantalla no lleva espacios duros
    for (x in tiles()) assertTrue(!x.spoken.contains(NBSP))
  }
}
