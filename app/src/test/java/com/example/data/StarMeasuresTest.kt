package com.example.data

import org.junit.Assert.assertEquals
import org.junit.Assert.assertNull
import org.junit.Assert.assertTrue
import org.junit.Test

class StarMeasuresTest {
  private val day = 86_400_000L
  private val now = 200 * day

  private fun series(key: String, vararg v: Float, lastDaysAgo: Int = 0) =
    v.mapIndexed { i, x -> MeasurePoint(now - (lastDaysAgo + v.size - 1 - i) * day, key, x) }

  @Test
  fun `sin 3 partidas no hay descubrimiento, y la invitacion dice cuantas faltan`() {
    assertNull(StarMeasures.discover(series("glance", 120f, 110f), now))
    val n = StarMeasures.nudge(series("glance", 120f, 110f))
    assertEquals(1, n.remaining)
    assertEquals("Juega Radar 1 vez más y verás cómo evoluciona tu vistazo.", n.text)
    assertEquals("Juega Radar para descubrir tu vistazo.", StarMeasures.nudge(emptyList()).text)
  }

  @Test
  fun `un record reciente manda, con menos es mejor o mas es mejor segun la medida`() {
    val pts = series("glance", 132f, 121f, 118f, 104f, 96f, 84f) + series("load", 3f, 4f, 3f, lastDaysAgo = 1)
    val d = StarMeasures.discover(pts, now)!!
    assertEquals("glance", d.def.key)
    assertEquals(DiscoveryKind.RECORD, d.kind)
    assertEquals(6, d.values.size)
    // Un récord de hace más de una semana ya no es "del día": gana la mejora.
    val old = series("load", 5f, 3f, 4f, lastDaysAgo = 10) + series("brake", 260f, 250f, 240f, 230f, 235f)
    val d2 = StarMeasures.discover(old, now)!!
    assertEquals("brake", d2.def.key)
    assertEquals(DiscoveryKind.IMPROVING, d2.kind)
  }

  @Test
  fun `sin record ni mejora clara, se mantiene (sin culpa)`() {
    val d = StarMeasures.discover(series("homing", 20f, 21f, 20f, 21f), now)!!
    assertEquals(DiscoveryKind.STEADY, d.kind)
    assertTrue(d.caption.contains("constancia"))
  }

  @Test
  fun `formato con coma decimal y guardado de ida y vuelta`() {
    assertEquals("3,8%", StarMeasures.def("numline")!!.format(3.8f))
    assertEquals("84", StarMeasures.def("glance")!!.format(84.4f))
    assertEquals("84 ms", StarMeasures.def("glance")!!.compact(84.4f))
    assertEquals("a 18%", StarMeasures.def("homing")!!.compact(18f))
    assertEquals("96°/s", StarMeasures.def("rotation")!!.compact(96f))
    val pts = series("tracking", 2.5f, 2.75f, 3f)
    assertEquals(pts, StarMeasures.decode(StarMeasures.encode(pts)))
    assertEquals(0, StarMeasures.decode("x|glance|3\n1|nada|2\n").size)
    assertEquals(3f, StarMeasures.latest(pts, "satelites")!!.second)
  }

  @Test
  fun `la evolucion compara solo partidas parecidas`() {
    val now = 500 * 86_400_000L
    fun p(d: Long, key: String, v: Float, r: Float = -1f, t: Boolean? = null) = MeasurePoint(now - d * 86_400_000L, key, v, r, t)
    // Rumbo depende del nivel (10 niveles: banda 0,1): solo las de rating parecido a la última.
    val homing = listOf(p(5, "homing", 30f, 0.20f, false), p(4, "homing", 25f, 0.45f, false), p(3, "homing", 22f, 0.50f, true), p(1, "homing", 20f, 0.52f, false))
    assertEquals(listOf(25f, 20f), StarMeasures.comparable(homing).map { it.value })
    // Radar no depende del nivel: solo el reloj.
    val glance = listOf(p(3, "glance", 120f, 0.1f, true), p(2, "glance", 110f, 0.9f, false), p(1, "glance", 100f, 0.5f, true))
    assertEquals(listOf(120f, 100f), StarMeasures.comparable(glance).map { it.value })
    // Partidas viejas sin dato: se comparan entre ellas mientras la última tampoco lo tenga.
    val old = listOf(p(3, "homing", 30f), p(2, "homing", 28f))
    assertEquals(2, StarMeasures.comparable(old).size)
    assertEquals(1, StarMeasures.comparable(old + p(1, "homing", 20f, 0.4f, true)).size)
    // Guardar y leer con el dato nuevo, y leer el formato antiguo.
    val all = homing + old
    assertEquals(all, StarMeasures.decode(StarMeasures.encode(all)))
    assertEquals(listOf(MeasurePoint(7L, "glance", 84f)), StarMeasures.decode("7|glance|84.0"))
  }
}
