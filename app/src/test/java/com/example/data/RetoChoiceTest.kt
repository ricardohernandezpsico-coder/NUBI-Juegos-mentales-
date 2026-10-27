package com.example.data

import com.example.model.GameRegistry
import org.junit.Assert.assertEquals
import org.junit.Assert.assertFalse
import org.junit.Assert.assertNotNull
import org.junit.Assert.assertNull
import org.junit.Assert.assertTrue
import org.junit.Test

/** «Sin reloj / Con reloj» por juego (ficha de Juegos, 6-oct): la elección que vale, lo que se guarda y cómo se dice. Lógica pura. */
class RetoChoiceTest {
  @Test
  fun `sin eleccion propia vale el ajuste viejo de Ajustes y la eleccion propia gana`() {
    assertFalse(RetoChoice.resolve("freno", null, initial = false))
    assertTrue("migración: quien tenía el Reto activado en Ajustes lo conserva en cada juego", RetoChoice.resolve("freno", null, initial = true))
    assertFalse("lo que se eligió en la ficha pesa más que el ajuste viejo", RetoChoice.resolve("freno", false, initial = true))
    assertTrue(RetoChoice.resolve("freno", true, initial = false))
  }

  @Test
  fun `se guarda por juego y solo las claves reto_ con un si o no se leen`() {
    assertEquals("reto_bodega", RetoChoice.key("bodega"))
    val all: Map<String, Any?> = mapOf("reto_bodega" to true, "reto_radar" to false, "education" to "UNIVERSITARIA", "reto_" to true, "reto_calculo" to "si", "goals" to "A;B")
    assertEquals(mapOf("bodega" to true, "radar" to false), RetoChoice.decode(all))
    assertEquals(emptyMap<String, Boolean>(), RetoChoice.decode(emptyMap<String, Any?>()))
  }

  @Test
  fun `todos los juegos tienen Reto menos La estacion de correo, y cada uno dice su duracion`() {
    assertEquals(setOf("correo"), RetoChoice.WITHOUT_RETO)
    for (g in GameRegistry.allGames) {
      if (g.id in RetoChoice.WITHOUT_RETO) {
        assertFalse("${g.id} no debería ofrecer Reto", RetoChoice.supports(g.id))
        assertNull("${g.id} no tiene duración de Reto", RetoChoice.seconds(g.id))
        continue
      }
      // los cuatro con Contrarreloj siguen teniendo Reto en el código, pero su ficha ya no lo ofrece (Tarea 181)
      assertEquals("${g.id} ofrece «¿Con reloj?» salvo si tiene Contrarreloj", g.id !in TimeTrialDay.GAMES, RetoChoice.supports(g.id))
      assertNotNull("${g.id} no tiene duración de Reto", RetoChoice.seconds(g.id))
    }
    assertEquals("Constelaciones dura 3 min", 180, RetoChoice.seconds("parejas"))
    assertEquals("Con reloj (3 minutos)", RetoChoice.timedLabel("parejas"))
  }

  /** Tarea 181: en los cuatro juegos con Contrarreloj el tiempo es un botón propio de la ficha, no una elección: sin «¿Con reloj?», lo guardado se ignora (y no se borra) y se juega sin reloj. */
  @Test
  fun `los cuatro juegos con Contrarreloj ya no ofrecen con reloj y los otros siguen igual`() {
    val four = setOf("stroop", "disparate", "radar", "acoplamiento")
    assertEquals("son los cuatro de la primera tanda del Contrarreloj", four, TimeTrialDay.GAMES.toSet())
    for (g in four) {
      assertFalse("$g no ofrece «¿Con reloj?»", RetoChoice.supports(g))
      assertNotNull("$g conserva la duración de su Reto en el código (Unity y su smoke lo siguen teniendo)", RetoChoice.seconds(g))
      for (saved in listOf(true, false, null)) for (initial in listOf(true, false)) assertFalse("$g ignora lo guardado ($saved) y el ajuste viejo ($initial): se juega sin reloj", RetoChoice.resolve(g, saved, initial))
    }
    val others = GameRegistry.allGames.map { it.id }.filter { it !in four }
    assertEquals("17 juegos, 4 con Contrarreloj", 13, others.size)
    assertEquals("de los otros 13, Correo ya no tenía «¿Con reloj?»: quedan 12 con él", 12, others.count { RetoChoice.supports(it) })
    for (g in others) {
      if (g == "correo") { assertFalse(RetoChoice.supports(g)); continue }
      assertTrue("$g sigue con «¿Con reloj?»", RetoChoice.supports(g))
      assertTrue("$g con reloj elegido sigue con reloj", RetoChoice.resolve(g, true, initial = false))
      assertFalse("$g sin reloj elegido sigue sin reloj", RetoChoice.resolve(g, false, initial = true))
    }
  }

  @Test
  fun `la clave guardada de un juego con Contrarreloj se sigue leyendo para el respaldo pero no se aplica`() {
    val all: Map<String, Any?> = mapOf("reto_stroop" to true, "reto_radar" to true, "reto_freno" to true)
    assertEquals("la clave se decodifica igual: no se borra, va en el respaldo", mapOf("stroop" to true, "radar" to true, "freno" to true), RetoChoice.decode(all))
    val decoded = RetoChoice.decode(all)
    assertFalse("Tinta o Palabra: se ignora", RetoChoice.resolve("stroop", decoded["stroop"], initial = true))
    assertFalse("Rescate: se ignora", RetoChoice.resolve("radar", decoded["radar"], initial = true))
    assertTrue("Freno: se aplica", RetoChoice.resolve("freno", decoded["freno"], initial = false))
  }

  @Test
  fun `la segunda linea de Jugar no habla del reloj en los cuatro y si en los demas`() {
    for (g in TimeTrialDay.GAMES) for (timed in listOf(true, false, null)) {
      assertEquals("$g: «a tu medida» a secas", "a tu medida", RetoChoice.playDetail(PlayMode.A_TU_MEDIDA, timed, RetoChoice.supports(g)))
      assertFalse(RetoChoice.playLabel(PlayMode.DESAFIO, timed, RetoChoice.supports(g)).contains("reloj"))
    }
    assertEquals("a tu medida · sin reloj", RetoChoice.playDetail(PlayMode.A_TU_MEDIDA, false, RetoChoice.supports("freno")))
    assertEquals("en Desafío · con reloj", RetoChoice.playDetail(PlayMode.DESAFIO, true, RetoChoice.supports("piloto")))
  }

  @Test
  fun `el boton Jugar dice lo elegido en palabras`() {
    assertEquals("Jugar · a tu medida · con reloj", RetoChoice.playLabel(PlayMode.A_TU_MEDIDA, true, supportsReto = true))
    assertEquals("Jugar · a tu medida · sin reloj", RetoChoice.playLabel(PlayMode.A_TU_MEDIDA, false, supportsReto = true))
    assertEquals("Jugar · suave · sin reloj", RetoChoice.playLabel(PlayMode.SUAVE, false, supportsReto = true))
    assertEquals("Jugar · suave · con reloj", RetoChoice.playLabel(PlayMode.SUAVE, true, supportsReto = true))
    assertEquals("Jugar · en Desafío · con reloj", RetoChoice.playLabel(PlayMode.DESAFIO, true, supportsReto = true))
    assertEquals("Jugar · al límite · sin reloj", RetoChoice.playLabel(PlayMode.EXPERTO, false, supportsReto = true))
  }

  @Test
  fun `en un juego sin Reto el boton no habla del reloj aunque llegue un valor`() {
    for (timed in listOf(true, false, null)) {
      assertEquals("Jugar · a tu medida", RetoChoice.playLabel(PlayMode.A_TU_MEDIDA, timed, supportsReto = false))
      assertEquals("Jugar · en Desafío", RetoChoice.playLabel(PlayMode.DESAFIO, timed, supportsReto = false))
      assertEquals("A tu medida", RetoChoice.summaryLabel(PlayMode.A_TU_MEDIDA, timed, supportsReto = false, seconds = 120))
    }
    assertEquals("a tu medida", RetoChoice.playDetail(PlayMode.A_TU_MEDIDA, true, supportsReto = false))
    assertEquals("a tu medida", RetoChoice.playSpoken(PlayMode.A_TU_MEDIDA, true, supportsReto = false))
  }

  @Test
  fun `sin valor de reloj cuenta como sin reloj`() {
    assertEquals("Jugar · a tu medida · sin reloj", RetoChoice.playLabel(PlayMode.A_TU_MEDIDA, null, supportsReto = true))
    assertEquals("A tu medida · sin reloj", RetoChoice.summaryLabel(PlayMode.A_TU_MEDIDA, null, supportsReto = true))
  }

  @Test
  fun `la linea de resumen dice el modo y el reloj con su duracion`() {
    assertEquals("A tu medida · con reloj (2 minutos)", RetoChoice.summaryLabel(PlayMode.A_TU_MEDIDA, true, supportsReto = true, seconds = 120))
    assertEquals("A tu medida · sin reloj", RetoChoice.summaryLabel(PlayMode.A_TU_MEDIDA, false, supportsReto = true, seconds = 120))
    assertEquals("Suave · sin reloj", RetoChoice.summaryLabel(PlayMode.SUAVE, false, supportsReto = true, seconds = 120))
    assertEquals("Desafío · con reloj (3 minutos)", RetoChoice.summaryLabel(PlayMode.DESAFIO, true, supportsReto = true, seconds = 180))
    assertEquals("Al límite · con reloj (1 minuto y 30 segundos)", RetoChoice.summaryLabel(PlayMode.EXPERTO, true, supportsReto = true, seconds = 90))
    assertEquals("sin duración conocida, solo «con reloj»", "A tu medida · con reloj", RetoChoice.summaryLabel(PlayMode.A_TU_MEDIDA, true, supportsReto = true))
  }

  @Test
  fun `para el lector de pantalla va con comas y para cada juego real coincide con lo que se ve`() {
    assertEquals("a tu medida, con reloj", RetoChoice.playSpoken(PlayMode.A_TU_MEDIDA, true, supportsReto = true))
    assertEquals("suave, sin reloj", RetoChoice.playSpoken(PlayMode.SUAVE, false, supportsReto = true))
    for (g in GameRegistry.allGames) {
      val supports = RetoChoice.supports(g.id)
      for (m in PlayMode.entries) for (timed in listOf(true, false)) {
        val label = RetoChoice.playLabel(m, timed, supports)
        assertTrue("«$label» empieza por «Jugar · »", label.startsWith("Jugar · "))
        assertEquals("lo que dice el lector es lo que se ve, con comas", RetoChoice.playDetail(m, timed, supports).replace(" · ", ", "), RetoChoice.playSpoken(m, timed, supports))
        assertEquals("el modo y, si el juego tiene Reto, el reloj", if (supports) 2 else 1, RetoChoice.playDetail(m, timed, supports).split(" · ").size)
      }
    }
  }

  @Test
  fun `la duracion se dice en minutos`() {
    assertEquals("1 minuto", RetoChoice.durationText(60))
    assertEquals("1 minuto y 30 segundos", RetoChoice.durationText(90))
    assertEquals("2 minutos", RetoChoice.durationText(120))
    assertEquals("3 minutos", RetoChoice.durationText(180))
    assertEquals("45 segundos", RetoChoice.durationText(45))
    assertEquals("Con reloj (2 minutos)", RetoChoice.timedLabel("bodega"))
    assertEquals("Sin reloj", RetoChoice.UNTIMED_LABEL)
  }
}
