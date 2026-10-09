package com.example.data

import org.junit.Assert.assertEquals
import org.junit.Assert.assertFalse
import org.junit.Assert.assertNotNull
import org.junit.Assert.assertNull
import org.junit.Assert.assertTrue
import org.junit.Test

/** «Tu freno» por zonas y como promedio de las últimas partidas (Tarea 57): lógica pura de `data/Brake.kt`. */
class BrakeZonesTest {
  private fun point(ts: Long, ms: Float, key: String = Brake.KEY) = MeasurePoint(ts, key, ms)

  // ------------------------------------------------------------------ zonas

  @Test
  fun `los cortes son 230 y 300 ms con los dos incluidos en firme`() {
    assertEquals(BrakeZone.AGIL, Brake.zone(150f))
    assertEquals(BrakeZone.AGIL, Brake.zone(229.9f))
    assertEquals(BrakeZone.FIRME, Brake.zone(230f))
    assertEquals(BrakeZone.FIRME, Brake.zone(265f))
    assertEquals(BrakeZone.FIRME, Brake.zone(300f))
    assertEquals(BrakeZone.PAUSADO, Brake.zone(300.1f))
    assertEquals(BrakeZone.PAUSADO, Brake.zone(450f))
  }

  @Test
  fun `cada zona tiene su nombre en palabras`() {
    assertEquals(listOf("ágil", "firme", "pausado"), BrakeZone.entries.map { it.label })
  }

  // ------------------------------------------------------------------ promedio

  @Test
  fun `sin estimaciones no hay lectura`() {
    assertNull(Brake.average(emptyList()))
    assertNull(Brake.reading(emptyList(), null, 1L))
    assertNull(Brake.reading(listOf(point(1, 250f, key = "glance")), null, 9L))
  }

  @Test
  fun `el promedio usa solo las ultimas 5 estimaciones`() {
    val r = Brake.average(listOf(500f, 500f, 200f, 210f, 220f, 230f, 240f))!!       // las dos de 500 quedan fuera
    assertEquals(5, r.count)
    assertEquals(220f, r.averageMs, 0.01f)
  }

  @Test
  fun `solo cuentan las estimaciones validas`() {
    // -1, 0, NaN y valores fuera del rango de Unity (50-900 ms) no son estimaciones
    val r = Brake.average(listOf(-1f, 0f, Float.NaN, 20f, 1200f, 240f, 260f))!!
    assertEquals(2, r.count)
    assertEquals(250f, r.averageMs, 0.01f)
    assertNull(Brake.average(listOf(-1f, 0f, Float.NaN)))
  }

  @Test
  fun `las invalidas no ocupan lugar de la ventana de 5`() {
    val r = Brake.average(listOf(240f, 250f, 260f, 270f, 280f, -1f, -1f))!!
    assertEquals(5, r.count)
    assertEquals(260f, r.averageMs, 0.01f)
  }

  // ------------------------------------------------------------------ 1 o 2 frente a 3 o más

  @Test
  fun `con una o dos estimaciones es una primera lectura`() {
    val one = Brake.average(listOf(250f))!!
    assertTrue(one.isFirstReading)
    assertEquals("Primera lectura: zona firme. Se afina con más partidas", one.headline)
    val two = Brake.average(listOf(210f, 220f))!!
    assertTrue(two.isFirstReading)
    assertEquals("Primera lectura: zona ágil. Se afina con más partidas", two.headline)
  }

  @Test
  fun `con tres o mas es el promedio de las ultimas N partidas`() {
    val three = Brake.average(listOf(310f, 320f, 330f))!!
    assertFalse(three.isFirstReading)
    assertEquals("Tu freno, promedio de tus últimas 3 partidas: zona pausado", three.headline)
    val five = Brake.average(listOf(250f, 255f, 260f, 265f, 270f))!!
    assertEquals("Tu freno, promedio de tus últimas 5 partidas: zona firme", five.headline)
  }

  @Test
  fun `el texto no trae milisegundos y la lectura hablada dice la zona`() {
    val r = Brake.average(listOf(250f, 255f, 260f, 265f))!!
    assertFalse(r.headline.contains("ms"))
    assertFalse(r.spoken.contains("ms"))
    assertEquals("Tu freno: zona firme, promedio de tus últimas 4 partidas", r.spoken)
    assertEquals("Tu freno: primera lectura, zona ágil", Brake.average(listOf(200f))!!.spoken)
  }

  // ------------------------------------------------------------------ con la de hoy

  @Test
  fun `la lectura de la pantalla final suma la de hoy al historial`() {
    val history = listOf(point(1, 300f), point(2, 310f), point(3, 320f), point(4, 330f))
    val r = Brake.reading(history, thisMs = 200, thisTimestamp = 5L)!!
    assertEquals(5, r.count)
    assertEquals((300f + 310f + 320f + 330f + 200f) / 5f, r.averageMs, 0.01f)
  }

  @Test
  fun `si la de hoy ya se guardo no cuenta dos veces`() {
    val history = listOf(point(1, 300f), point(2, 310f), point(5, 200f))          // la última ES la de hoy (timestamp 5)
    val r = Brake.reading(history, thisMs = 200, thisTimestamp = 5L)!!
    assertEquals(3, r.count)
    assertEquals((300f + 310f + 200f) / 3f, r.averageMs, 0.01f)
  }

  @Test
  fun `si hoy no se pudo estimar se muestra el promedio de las partidas anteriores`() {
    val history = listOf(point(1, 250f), point(2, 260f), point(3, 270f))
    val r = Brake.reading(history, thisMs = null, thisTimestamp = 9L)!!
    assertEquals(3, r.count)
    assertEquals(260f, r.averageMs, 0.01f)
    assertNull(Brake.reading(emptyList(), thisMs = null, thisTimestamp = 9L))
  }

  @Test
  fun `el historial se ordena por fecha aunque venga desordenado`() {
    val history = listOf(point(3, 400f), point(1, 200f), point(2, 210f))
    val r = Brake.reading(history, thisMs = 220, thisTimestamp = 9L)!!
    assertEquals(4, r.count)
    assertEquals((200f + 210f + 400f + 220f) / 4f, r.averageMs, 0.01f)
    // con 7 partidas, las 5 últimas por fecha
    val many = (1..7).map { point(it.toLong(), 100f * it) }
    val last5 = Brake.reading(many)!!
    assertEquals(5, last5.count)
    assertEquals((300f + 400f + 500f + 600f + 700f) / 5f, last5.averageMs, 0.01f)
  }

  // ------------------------------------------------------------------ la aguja

  @Test
  fun `la aguja va de izquierda (lento) a derecha (rapido) y cada zona ocupa un tercio`() {
    assertEquals(0f, Brake.gaugePosition(450f), 1e-4f)
    assertEquals(0f, Brake.gaugePosition(900f), 1e-4f)            // más lento que el extremo: queda en el extremo
    assertEquals(1f / 3f, Brake.gaugePosition(300f), 1e-4f)
    assertEquals(2f / 3f, Brake.gaugePosition(230f), 1e-4f)
    assertEquals(1f, Brake.gaugePosition(150f), 1e-4f)
    assertEquals(1f, Brake.gaugePosition(80f), 1e-4f)
    // dentro de cada zona avanza hacia la derecha mientras más rápido
    assertTrue(Brake.gaugePosition(400f) < Brake.gaugePosition(350f))
    assertTrue(Brake.gaugePosition(290f) < Brake.gaugePosition(240f))
    assertTrue(Brake.gaugePosition(220f) < Brake.gaugePosition(170f))
    // y la zona de cada valor cae en el tercio que le toca
    for (ms in listOf(160f, 200f, 229f)) assertTrue(ms.toString(), Brake.gaugePosition(ms) >= 2f / 3f)
    for (ms in listOf(231f, 265f, 299f)) assertTrue(ms.toString(), Brake.gaugePosition(ms) in 1f / 3f..2f / 3f)
    for (ms in listOf(301f, 380f, 440f)) assertTrue(ms.toString(), Brake.gaugePosition(ms) <= 1f / 3f)
  }

  // ------------------------------------------------------------------ lo que muestran Juegos y Hoy

  @Test
  fun `Juegos y Hoy muestran la zona del promedio y no milisegundos`() {
    val points = listOf(point(1, 250f), point(2, 255f), point(3, 245f), point(4, 20f, key = "glance"))
    val def = StarMeasures.def("brake")!!
    assertEquals("zona firme", StarMeasures.zoneText(def, points))
    assertNull("las demás medidas siguen con su unidad", StarMeasures.zoneText(StarMeasures.def("glance")!!, points))
    assertNull("sin estimaciones no hay zona", StarMeasures.zoneText(def, emptyList()))
    assertNotNull(StarMeasures.def("brake"))
  }
}
