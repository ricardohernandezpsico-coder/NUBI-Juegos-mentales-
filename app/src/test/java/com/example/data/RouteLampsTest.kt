package com.example.data

import androidx.test.core.app.ApplicationProvider
import org.junit.Assert.assertEquals
import org.junit.Assert.assertFalse
import org.junit.Assert.assertNull
import org.junit.Assert.assertTrue
import org.junit.Test
import org.junit.runner.RunWith
import org.robolectric.RobolectricTestRunner
import org.robolectric.annotation.Config
import java.io.File
import java.util.Calendar

/**
 * Las farolas de la ruta de hoy (Tarea 88): qué farola es de cada lugar, cuáles se encienden con 0, 1, 2 y 3 paradas hechas, que cada farola pulse UNA vez por parada y por día, que la ruta completa se celebre UNA vez al día y la descripción para el lector de pantalla.
 */
@RunWith(RobolectricTestRunner::class)
@Config(sdk = [36])
class RouteLampsTest {
  private val layout = IslaLayout.parse(File("src/main/res/raw/isla.json").readText())
  private val day = "2026-10-13"

  // ---- La asignación farola → lugar

  @Test
  fun cada_lugar_tiene_su_farola_y_son_cuatro_distintas() {
    assertEquals(setOf("observatorio", "antena", "biblioteca", "hangar"), layout.lampOf.keys)
    assertEquals("cada farola es de UN lugar", setOf(0, 1, 2, 3), layout.lampOf.values.toSet())
  }

  @Test
  fun la_asignacion_es_la_aprobada_por_Opus() {
    // Antena → (0,4195, 0,4006); Biblioteca → (0,602, 0,4006); Hangar → (0,6057, 0,5022); Observatorio → (0,4158, 0,5022)
    val expected = mapOf("antena" to IslaPoint(0.4195f, 0.4006f), "biblioteca" to IslaPoint(0.602f, 0.4006f), "hangar" to IslaPoint(0.6057f, 0.5022f), "observatorio" to IslaPoint(0.4158f, 0.5022f))
    for ((place, point) in expected) {
      val lamp = layout.lamps[layout.lampIndex(place)!!]
      assertEquals("$place x", point.x, lamp.x, 0.0001f)
      assertEquals("$place y", point.y, lamp.y, 0.0001f)
    }
  }

  @Test
  fun la_asignacion_es_la_de_menor_distancia_total_entre_el_pie_del_edificio_y_el_tope_de_la_farola() {
    fun cost(assign: Map<String, Int>) = assign.entries.sumOf { (k, i) -> val f = layout.place(k).foot; val l = layout.lamps[i]; Math.hypot((f.x - l.x).toDouble(), (f.y - l.y).toDouble()) }
    val keys = layout.places.map { it.key }
    var best = Double.MAX_VALUE
    fun permute(rest: List<Int>, acc: List<Int>) {
      if (rest.isEmpty()) { best = minOf(best, cost(keys.zip(acc).toMap())); return }
      for (i in rest) permute(rest - i, acc + i)
    }
    permute(listOf(0, 1, 2, 3), emptyList())
    assertEquals("ninguna otra asignación suma menos distancia", best, cost(layout.lampOf), 1e-9)
  }

  // ---- Qué farolas se encienden

  private val observatorio = "MEMORIA"; private val hangar = "RAZONAMIENTO"; private val biblioteca = "LENGUAJE"

  @Test
  fun con_0_1_2_y_3_paradas_hechas_se_encienden_esas_farolas_y_ninguna_mas() {
    val route = listOf(observatorio, hangar, biblioteca)       // la ruta de ejemplo: Observatorio, Hangar y Biblioteca (la Antena no está en ella)
    assertEquals(emptySet<Int>(), RouteLamps.litLamps(layout, route.take(0)))
    assertEquals(setOf(layout.lampOf.getValue("observatorio")), RouteLamps.litLamps(layout, route.take(1)))
    assertEquals(setOf(layout.lampOf.getValue("observatorio"), layout.lampOf.getValue("hangar")), RouteLamps.litLamps(layout, route.take(2)))
    assertEquals(setOf(layout.lampOf.getValue("observatorio"), layout.lampOf.getValue("hangar"), layout.lampOf.getValue("biblioteca")), RouteLamps.litLamps(layout, route.take(3)))
    assertFalse("la farola de la Antena (no está en la ruta de hoy) nunca se enciende", layout.lampOf.getValue("antena") in RouteLamps.litLamps(layout, route))
  }

  @Test
  fun un_area_desconocida_no_enciende_nada() {
    assertEquals(emptySet<Int>(), RouteLamps.litLamps(layout, listOf("CALCULO")))
    assertNull(RouteLamps.placeKey("CALCULO"))
    assertEquals("observatorio", RouteLamps.placeKey("MEMORIA"))
  }

  // ---- Una vez por parada y por día

  @Test
  fun la_primera_vez_que_se_ve_hecha_una_parada_pulsa_su_farola() {
    val plan = RouteLamps.plan(listOf("observatorio"), allDone = false, saved = null, day = day)
    assertEquals(listOf("observatorio"), plan.pulse)
    assertFalse(plan.complete)
  }

  @Test
  fun una_farola_pulsa_una_sola_vez_por_parada_y_por_dia() {
    val first = RouteLamps.plan(listOf("observatorio"), false, null, day)
    val saved = RouteLamps.consume(first, null, day)
    assertEquals(setOf("observatorio"), saved.lamps)
    // al volver a Hoy otra vez ese día: ya está encendida y quieta
    assertTrue("sin pulso la segunda vez", RouteLamps.plan(listOf("observatorio"), false, saved, day).isEmpty)
    // se hace la segunda parada: pulsa SOLO la nueva
    val second = RouteLamps.plan(listOf("observatorio", "hangar"), false, saved, day)
    assertEquals(listOf("hangar"), second.pulse)
    val saved2 = RouteLamps.consume(second, saved, day)
    assertEquals(setOf("observatorio", "hangar"), saved2.lamps)
    assertTrue(RouteLamps.plan(listOf("observatorio", "hangar"), false, saved2, day).isEmpty)
  }

  @Test
  fun al_dia_siguiente_vuelven_a_pulsar() {
    val saved = RouteCelebrations("2026-10-12", setOf("observatorio", "hangar"), complete = true)
    val plan = RouteLamps.plan(listOf("observatorio"), false, saved, day)
    assertEquals("lo de ayer no cuenta", listOf("observatorio"), plan.pulse)
    assertEquals(setOf("observatorio"), RouteLamps.consume(plan, saved, day).lamps)
    assertFalse("y la ruta completa de ayer tampoco", RouteLamps.consume(plan, saved, day).complete)
  }

  @Test
  fun si_se_hacen_varias_paradas_de_una_vez_pulsan_todas_en_el_orden_de_la_ruta() {
    val plan = RouteLamps.plan(listOf("observatorio", "hangar", "biblioteca"), allDone = true, saved = null, day = day)
    assertEquals(listOf("observatorio", "hangar", "biblioteca"), plan.pulse)
    assertTrue(plan.complete)
  }

  // ---- La ruta completa, una vez al día

  @Test
  fun la_ruta_completa_se_celebra_una_sola_vez_al_dia() {
    val three = listOf("observatorio", "hangar", "biblioteca")
    val before = RouteCelebrations(day, setOf("observatorio", "hangar"))
    val first = RouteLamps.plan(three, allDone = true, saved = before, day = day)
    assertEquals(listOf("biblioteca"), first.pulse)
    assertTrue("al completar la tercera: saltito y chispas", first.complete)
    val saved = RouteLamps.consume(first, before, day)
    assertTrue(saved.complete)
    val again = RouteLamps.plan(three, allDone = true, saved = saved, day = day)
    assertTrue("al volver a Hoy: nada se mueve", again.isEmpty)
  }

  @Test
  fun sin_las_tres_hechas_no_hay_celebracion_de_ruta_completa() {
    assertFalse(RouteLamps.plan(listOf("observatorio", "hangar"), allDone = false, saved = null, day = day).complete)
  }

  // ---- El día

  @Test
  fun el_dia_se_separa_a_medianoche() {
    val a = Calendar.getInstance().apply { set(2026, Calendar.OCTOBER, 13, 23, 59, 0) }.timeInMillis
    val b = Calendar.getInstance().apply { set(2026, Calendar.OCTOBER, 14, 0, 1, 0) }.timeInMillis
    assertEquals("2026-10-13", RouteLamps.dayKey(a))
    assertEquals("2026-10-14", RouteLamps.dayKey(b))
  }

  // ---- Las preferencias pasajeras

  @Test
  fun lo_celebrado_se_guarda_y_se_lee_de_sus_propias_preferencias() {
    val store = PrefsRouteCelebrationStore(ApplicationProvider.getApplicationContext())
    assertEquals("route_celebrations", PrefsRouteCelebrationStore.FILE)
    store.save(RouteCelebrations(day, setOf("antena", "hangar"), complete = true))
    assertEquals(RouteCelebrations(day, setOf("antena", "hangar"), true), store.load())
  }

  // ---- La descripción para el lector de pantalla

  private val obs = "el Observatorio"; private val ant = "la Antena"; private val han = "el Hangar"

  @Test
  fun la_descripcion_dice_lo_hecho_y_lo_que_falta() {
    assertEquals("Tu isla: hoy la ruta pasa por el Observatorio, la Antena y el Hangar", RouteLamps.description(listOf(obs to false, ant to false, han to false)))
    assertEquals("Tu isla: hecho el Observatorio; faltan la Antena y el Hangar", RouteLamps.description(listOf(obs to true, ant to false, han to false)))
    assertEquals("Tu isla: hechos el Observatorio y la Antena; falta el Hangar", RouteLamps.description(listOf(obs to true, ant to true, han to false)))
    assertEquals("Tu isla: ruta completa, hechos el Observatorio, la Antena y el Hangar", RouteLamps.description(listOf(obs to true, ant to true, han to true)))
    assertEquals("Tu isla: el Observatorio, la Antena, la Biblioteca y el Hangar", RouteLamps.description(emptyList()))
  }
}
