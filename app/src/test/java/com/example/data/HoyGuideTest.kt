package com.example.data

import com.example.model.GameRegistry
import org.junit.Assert.assertEquals
import org.junit.Assert.assertFalse
import org.junit.Assert.assertNull
import org.junit.Assert.assertTrue
import org.junit.Test

/**
 * La guía de Nubi en la primera entrada a Hoy (Tarea 193; `auditoria-fable/guia-primer-ingreso.md` §3 y §6.3): lógica pura. Qué se dice, en qué orden, cuándo sale y qué sigue.
 */
class HoyGuideTest {
  /** Las reglas de los globos de Nubi (`GlobosDeNubiTest`): 2 frases y 100 letras como máximo. */
  private fun sentences(text: String): Int = text.trim().split(Regex("(?<=[.!?])\\s+")).count { it.isNotBlank() }

  private fun assertGlobe(text: String, origin: String) {
    assertTrue("$origin: «$text» tiene ${sentences(text)} frases (máximo 2)", sentences(text) in 1..2)
    assertTrue("$origin: «$text» es demasiado larga para un globo (máximo ~100 letras; el paso 3 mide 101 desde la Tarea 225: lo que pidió Opus, y `HoyGuideOverlayTest` mide que el globo lo muestre entero)", text.length <= 102)
  }

  @Test
  fun son_tres_pasos_en_este_orden() {
    assertEquals("Tarea 220: siguen la pantalla de arriba abajo (el Contrarreloj de Hoy → la pestaña Juegos de la barra → las dos de la derecha, Tarea 225)", listOf(GuideStep.CONTRARRELOJ, GuideStep.JUEGOS, GuideStep.BARRA), HoyGuide.steps)
  }

  @Test
  fun los_textos_son_los_de_la_spec_y_cumplen_las_reglas_de_los_globos() {
    assertEquals("El Contrarreloj dura 2 minutos y su juego cambia cada día.", HoyGuide.text(GuideStep.CONTRARRELOJ))
    assertEquals("Aquí están los 17 juegos: eliges el que quieras.", HoyGuide.text(GuideStep.JUEGOS))
    assertEquals("En Contrarreloj ves tus marcas; en Avance, tu punto de partida. Arriba a la derecha, Tú: tus ajustes.", HoyGuide.text(GuideStep.BARRA))
    assertEquals("el «17» del texto es el catálogo de verdad: si cambia, el texto se revisa a propósito", 17, GameRegistry.allGames.size)
    for (step in HoyGuide.steps) {
      assertGlobe(HoyGuide.text(step), step.name)
      assertFalse("sin sol ni «ahora»: valen en cualquier momento del día del vuelo", HoyGuide.text(step).lowercase().let { "sol" in it.split(' ', '.', ',', ':') || "ahora" in it })
    }
  }

  @Test
  fun el_contador_dice_Paso_N_de_3_sin_palabras_sueltas() {
    assertEquals("Paso 1 de 3", HoyGuide.counter(GuideStep.CONTRARRELOJ))
    assertEquals("Paso 2 de 3", HoyGuide.counter(GuideStep.JUEGOS))
    assertEquals("Paso 3 de 3", HoyGuide.counter(GuideStep.BARRA))
  }

  @Test
  fun el_chip_dice_Seguir_con_flecha_y_en_el_ultimo_paso_Listo_sin_flecha() {
    assertEquals("Seguir ›", HoyGuide.chipLabel(GuideStep.CONTRARRELOJ))
    assertEquals("Seguir ›", HoyGuide.chipLabel(GuideStep.JUEGOS))
    assertEquals("Listo", HoyGuide.chipLabel(GuideStep.BARRA))
    assertEquals("para el lector de pantalla, sin la flecha", "Seguir", HoyGuide.chipSpoken(GuideStep.CONTRARRELOJ))
    assertEquals("Listo", HoyGuide.chipSpoken(GuideStep.BARRA))
    assertEquals("Saltar la guía", HoyGuide.SKIP_LABEL)
  }

  @Test
  fun TalkBack_dice_el_paso_el_lugar_de_lo_iluminado_y_el_texto() {
    assertEquals("«Nubi te muestra Hoy» va solo en el primer paso; el Contrarreloj no dice «botón»: sin partidas lo iluminado es un recuadro con la fila gris, no un botón", "Nubi te muestra Hoy. Paso 1 de 3. El Contrarreloj, debajo de tu ruta. El Contrarreloj dura 2 minutos y su juego cambia cada día.", HoyGuide.spoken(GuideStep.CONTRARRELOJ))
    assertEquals("lo iluminado es la columna de la pestaña Juegos de la barra, no un botón de Hoy", "Paso 2 de 3. La pestaña Juegos, en la barra de abajo. Aquí están los 17 juegos: eliges el que quieras.", HoyGuide.spoken(GuideStep.JUEGOS))
    for (step in listOf(GuideStep.JUEGOS, GuideStep.BARRA)) assertFalse("solo el primer paso presenta a Nubi", HoyGuide.spoken(step).contains("Nubi te muestra"))
    for (step in HoyGuide.steps) assertFalse("«Todos los juegos» ya no es un lugar de la app: una palabra por concepto", HoyGuide.spoken(step).contains("Todos los juegos"))
    assertEquals("Paso 3 de 3. Las pestañas Contrarreloj y Avance, en la barra de abajo. En Contrarreloj ves tus marcas; en Avance, tu punto de partida. Arriba a la derecha, Tú: tus ajustes.", HoyGuide.spoken(GuideStep.BARRA))
  }

  @Test
  fun seguir_avanza_y_despues_del_tercer_paso_termina() {
    assertEquals(GuideStep.JUEGOS, HoyGuide.next(GuideStep.CONTRARRELOJ))
    assertEquals(GuideStep.BARRA, HoyGuide.next(GuideStep.JUEGOS))
    assertNull("tras el paso 3 la guía termina", HoyGuide.next(GuideStep.BARRA))
  }

  @Test
  fun solo_sale_si_termino_el_vuelo_completo_sin_marca_sin_nada_encima_y_con_el_arranque_listo() {
    assertTrue(HoyGuide.shouldShow(firstFlightDone = true, seen = false, somethingAbove = false, startupReady = true))
    // todas las demás combinaciones de las cuatro condiciones: ninguna la muestra
    for (mask in 0 until 16) {
      val done = mask and 1 != 0
      val seen = mask and 2 != 0
      val above = mask and 4 != 0
      val ready = mask and 8 != 0
      val expected = done && !seen && !above && ready
      assertEquals("firstFlightDone=$done seen=$seen somethingAbove=$above startupReady=$ready", expected, HoyGuide.shouldShow(done, seen, above, ready))
    }
  }

  /** Tarea 193b: si el vuelo terminó sin jugar ningún juego, Avance solo dice «Todavía no hay nada aquí» y la invitación: el paso 3 habla de lo que se verá, no de un punto de partida que todavía no existe. */
  @Test
  fun sin_ninguna_partida_el_paso_3_dice_como_vas_en_cada_area_y_los_pasos_1_y_2_no_cambian() {
    assertEquals("En Contrarreloj ves tus marcas; en Avance, cada área. Arriba a la derecha, Tú: tus ajustes.", HoyGuide.text(GuideStep.BARRA, startingPoint = false))
    assertEquals("con partidas, el texto de siempre", "En Contrarreloj ves tus marcas; en Avance, tu punto de partida. Arriba a la derecha, Tú: tus ajustes.", HoyGuide.text(GuideStep.BARRA, startingPoint = true))
    assertEquals("por omisión, el de siempre", HoyGuide.text(GuideStep.BARRA, startingPoint = true), HoyGuide.text(GuideStep.BARRA))
    for (step in listOf(GuideStep.CONTRARRELOJ, GuideStep.JUEGOS)) assertEquals(HoyGuide.text(step, startingPoint = true), HoyGuide.text(step, startingPoint = false))
    assertGlobe(HoyGuide.text(GuideStep.BARRA, startingPoint = false), "paso 3 sin partidas")
    assertEquals(
      "Paso 3 de 3. Las pestañas Contrarreloj y Avance, en la barra de abajo. En Contrarreloj ves tus marcas; en Avance, cada área. Arriba a la derecha, Tú: tus ajustes.",
      HoyGuide.spoken(GuideStep.BARRA, startingPoint = false)
    )
    assertEquals(HoyGuide.spoken(GuideStep.JUEGOS), HoyGuide.spoken(GuideStep.JUEGOS, startingPoint = false))
    assertEquals(HoyGuide.spoken(GuideStep.CONTRARRELOJ), HoyGuide.spoken(GuideStep.CONTRARRELOJ, startingPoint = false))
  }

  @Test
  fun la_espera_es_de_600_ms() {
    assertEquals(600L, HoyGuide.STEP_DELAY_MS)
  }
}
