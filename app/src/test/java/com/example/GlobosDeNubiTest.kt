package com.example

import com.example.data.PlaceSheets
import com.example.data.Streak
import com.example.data.Suggestion
import com.example.data.Week
import com.example.model.AppLanguage
import com.example.model.DailySessionState
import com.example.ui.components.EMPTY_AVANCE_LINE
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
    assertEquals(2, sentences("Te faltan 130 puntos para Experto. Esta semana sumaste 12 puntos."))
    assertEquals(3, sentences("Una. Dos. Tres."))
  }

  @Test
  fun la_lamina_del_area_dice_dos_frases_como_mucho() {
    for ((total, change, measured) in listOf(Triple(470, 100, true), Triple(380, -20, true), Triple(470, 0, true), Triple(910, 0, true), Triple(0, 0, false), Triple(50, 50, true))) {
      assertGlobe(PlaceSheets.bubble(PlaceTestData.points("MEMORIA", total, change, measured), "Memoria"), "lámina ($total, $change, $measured)")
    }
  }

  @Test
  fun la_isla_como_mapa_y_el_avance_vacio_tambien_cumplen() {
    assertGlobe(com.example.ui.screens.ISLAND_NOTE, "el globo de «los juegos ahora están en la isla» (Tarea 158)")
    assertGlobe(EMPTY_AVANCE_LINE, "Avance vacío")
  }
}
