package com.example

import com.example.data.AreaStatus
import com.example.data.PlaceSheets
import com.example.data.Streak
import com.example.data.Suggestion
import com.example.data.Week
import com.example.data.nubiWeekLine
import com.example.model.AppLanguage
import com.example.model.DailySessionState
import com.example.ui.components.EMPTY_AVANCE_LINE
import com.example.ui.screens.suggestArea
import org.junit.Assert.assertEquals
import org.junit.Assert.assertTrue
import org.junit.Test

/**
 * La regla para TODOS los globos de Nubi (Opus, 21-oct): como mucho DOS frases y sin repetir lo que la pantalla ya dice (antes, el globo del detalle de un área tenía 6 líneas y repetía la línea de avance que está justo debajo).
 * Aquí se cuentan las frases de cada texto que Nubi dice en un globo: la semana de Avance, el avance de la lámina del lugar, el detalle de un área, el encabezado de Juegos, la ventana de un área y el Avance vacío.
 */
class GlobosDeNubiTest {
  /** Las frases de un texto: se separan donde un punto, un «?» o un «!» van seguidos de un espacio (los dos puntos y las comas no cortan: «Te sugiero X: hace 4 días…» es UNA frase). */
  private fun sentences(text: String): Int = text.trim().split(Regex("(?<=[.!?])\\s+")).count { it.isNotBlank() }

  private fun assertGlobe(text: String, origin: String) {
    assertTrue("$origin: «$text» tiene ${sentences(text)} frases (máximo 2)", sentences(text) in 1..2)
    assertTrue("$origin: «$text» es demasiado larga para un globo (máximo ~100 letras)", text.length <= 100)
  }

  @Test
  fun el_contador_de_frases_cuenta_bien() {
    assertEquals(1, sentences("Te sugiero Atención: hace 4 días que no juegas Constelaciones"))
    assertEquals(2, sentences("¿Le damos un empujón? Te sugiero Constelaciones: sigue en tu ruta de hoy."))
    assertEquals(2, sentences("Esta semana avanzó de 18 a 22. Te faltan 18 para Hábil."))
    assertEquals(3, sentences("Una. Dos. Tres."))
  }

  @Test
  fun la_semana_de_avance_dice_una_o_dos_frases_y_ninguna_cifra_que_la_pantalla_ya_dice() {
    fun lamps(days: Int) = Week.Lamps(List(7) { it < days }, todayIndex = 3)
    val streaks = listOf(Streak.StreakInfo(3, null, true), Streak.StreakInfo(5, 5L, false), Streak.StreakInfo(0, null, true))
    for (days in 0..7) for (goal in 0..7) for (areas in 0..4) for (st in streaks) for (none in listOf(false, true)) {
      val line = nubiWeekLine(lamps(days), goal, st, areas, none)
      assertGlobe(line, "nubiWeekLine($days días, meta $goal, $areas áreas)")
      assertTrue("«$line» repite una cifra que la pantalla ya dice («2 de 4 días», «Áreas esta semana: 4 de 4»)", line.none { it.isDigit() })
    }
  }

  @Test
  fun la_lamina_y_el_detalle_dicen_dos_frases_como_mucho() {
    for ((value, change) in listOf(0.47f to 0.10f, 0.38f to -0.02f, 0.47f to 0f, 0.91f to 0f, null to 0f, 0.05f to 0.05f)) {
      assertGlobe(PlaceSheets.bubble(AreaStatus("MEMORIA", value, change, listOf(null, null, null, value))), "lámina ($value, $change)")
    }
    val pick = Suggestion.Pick("parejas", "MEMORIA", Suggestion.Why.ROUTE)
    assertEquals("¿Le damos un empujón? Te sugiero Constelaciones: sigue en tu ruta de hoy.", Suggestion.detailGlobe("Constelaciones", pick))
    assertEquals("en las otras áreas, solo la pregunta", "¿Le damos un empujón?", Suggestion.detailGlobe("Constelaciones", null))
    for (why in Suggestion.Why.entries) for (days in listOf(0, 1, 4, 30)) {
      assertGlobe(Suggestion.detailGlobe("Rescate relámpago", Suggestion.Pick("radar", "ATENCION", why, days)), "detalle ($why, $days)")
    }
    assertTrue("el detalle ya no repite «Esta semana … avanzó» ni «tu memoria» (la línea de avance está debajo)", Suggestion.detailGlobe("X", pick).let { !it.contains("Esta semana") && !it.contains("tu memoria") && !it.contains("Elegí") })
  }

  @Test
  fun juegos_y_el_avance_vacio_tambien_cumplen() {
    val day = 86_400_000L
    val now = 400 * day
    val route = DailySessionState("2026-10-21", listOf("calculo", "secuencia", "stroop"), 0)
    val last = com.example.model.GameRegistry.allGames.associate { it.id to now - 3 * day }
    for (daily in listOf(route, route.copy(completedCount = 3), DailySessionState("2026-10-21", emptyList(), 0))) for (lp in listOf(last, emptyMap())) {
      val s = suggestArea(daily, lp, now, AppLanguage.SPANISH)
      assertGlobe("Te sugiero ${s.name}: ${s.reason}", "encabezado de Juegos")
    }
    assertGlobe("¿Qué juego de Memoria jugamos?", "ventana de un área")
    assertGlobe(EMPTY_AVANCE_LINE, "Avance vacío")
  }
}
