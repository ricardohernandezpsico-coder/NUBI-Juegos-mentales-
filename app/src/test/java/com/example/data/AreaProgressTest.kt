package com.example.data

import org.junit.Assert.assertEquals
import org.junit.Assert.assertNull
import org.junit.Test

class AreaProgressTest {
  private val day = 86_400_000L
  private fun at(y: Int, m: Int, d: Int, h: Int = 12): Long = java.util.Calendar.getInstance().apply { clear(); set(y, m, d, h, 0, 0) }.timeInMillis
  /** Un miércoles al mediodía: «esta semana» empezó el lunes 19 a las 00:00 (Tarea 151, B4.1). */
  private val now = at(2026, java.util.Calendar.OCTOBER, 21)
  private val monday = at(2026, java.util.Calendar.OCTOBER, 19, 0)
  private val eps = 0.0001f

  @Test
  fun `el cambio compara con el lunes y un juego nuevo no cuenta`() {
    val pts = listOf(
      ProgressPoint("parejas", monday - 20 * day, 0.30f), ProgressPoint("parejas", monday + day, 0.36f),
      ProgressPoint("bodega", monday + day + 3_600_000L, 0.50f) // medido por primera vez esta semana
    )
    val s = AreaProgress.status("MEMORIA", listOf("parejas", "bodega", "secuencia"), mapOf("parejas" to 0.36f, "bodega" to 0.50f), pts, now)
    assertEquals(0.43f, s.value!!, eps)          // promedio de los dos medidos
    assertEquals(0.06f, s.change, eps)           // solo parejas: 0,30 → 0,36
    assertEquals(43, s.points); assertEquals(37, s.pointsBefore)
    assertEquals("Esta semana avanzó de 37 a 43", AreaProgress.changeLine(s))
    assertEquals("la tarjeta de Avance dice LO MISMO que el detalle y la lámina (Tarea 150, B1.8)", "Esta semana avanzó de 37 a 43", AreaProgress.cardLine(s.copy(weeks = listOf(0.3f, 0.3f, 0.37f, 0.43f))))
    assertEquals("Te faltan 17 para Experto", AreaProgress.toNextLine(s))
  }

  @Test
  fun `lo que pasó antes del lunes ya no cuenta como de esta semana`() {
    // una partida el domingo a las 23:00 (la semana pasada) y otra el martes: «esta semana» solo cuenta desde el lunes a las 00:00
    val pts = listOf(ProgressPoint("radar", monday - 7 * day, 0.30f), ProgressPoint("radar", monday - 3_600_000L, 0.40f), ProgressPoint("radar", monday + day, 0.43f))
    val s = AreaProgress.status("ATENCION", listOf("radar"), mapOf("radar" to 0.43f), pts, now)
    assertEquals(0.03f, s.change, eps)
    assertEquals(40, s.pointsBefore)
    assertEquals(listOf<Float?>(null, 0.30f, 0.40f, 0.43f), s.weeks)         // el último punto de «pasada» es el del domingo: el avance con que EMPEZÓ esta semana
    // el lunes a la mañana, sin partidas todavía: no hay cambio
    val mondayMorning = at(2026, java.util.Calendar.OCTOBER, 19, 8)
    val m = AreaProgress.status("ATENCION", listOf("radar"), mapOf("radar" to 0.40f), pts.take(2), mondayMorning)
    assertEquals(0f, m.change, eps)
    assertEquals("Esta semana se mantuvo en 40", AreaProgress.changeLine(m))
    // el domingo a la noche: la semana empezó seis días antes
    val sunday = at(2026, java.util.Calendar.OCTOBER, 25, 23)
    val d = AreaProgress.status("ATENCION", listOf("radar"), mapOf("radar" to 0.43f), pts, sunday)
    assertEquals(0.03f, d.change, eps)
  }

  @Test
  fun `las areas jugadas y el cambio de cada area cuentan la MISMA semana`() {
    // Tarea 151 (B4.1): «Áreas esta semana: N de 4» (desde el lunes) y «Esta semana avanzó…» (antes, los últimos 7 días) en la misma pantalla
    val sundayNight = monday - 3_600_000L                       // el domingo a las 23:00 de la semana pasada
    val tuesday = monday + day + 10 * 3_600_000L
    val pts = listOf(ProgressPoint("radar", monday - 10 * day, 0.30f), ProgressPoint("radar", sundayNight, 0.36f), ProgressPoint("radar", tuesday, 0.40f))
    val plays = listOf(sundayNight to "ATENCION", tuesday to "ATENCION")
    // el lunes a la mañana: nada de lo del domingo cuenta como de esta semana, ni las áreas ni el avance
    val mondayMorning = at(2026, java.util.Calendar.OCTOBER, 19, 8)
    assertEquals(0, Week.areasPlayedFor(plays, mondayMorning))
    assertEquals(0f, AreaProgress.status("ATENCION", listOf("radar"), mapOf("radar" to 0.36f), pts.take(2), mondayMorning).change, eps)
    // el miércoles, con la partida del martes: una área jugada y esa misma área avanzó
    assertEquals(1, Week.areasPlayedFor(plays, now))
    val s = AreaProgress.status("ATENCION", listOf("radar"), mapOf("radar" to 0.40f), pts, now)
    assertEquals(0.04f, s.change, eps)
    assertEquals("Esta semana avanzó de 36 a 40", AreaProgress.changeLine(s))
    // el domingo a las 23:00 todavía es la misma semana que el miércoles
    val sunday = at(2026, java.util.Calendar.OCTOBER, 25, 23)
    assertEquals(1, Week.areasPlayedFor(plays, sunday))
    assertEquals(0.04f, AreaProgress.status("ATENCION", listOf("radar"), mapOf("radar" to 0.40f), pts, sunday).change, eps)
  }

  @Test
  fun `bajar se dice sin culpa y lo que no se movio queda igual`() {
    val down = AreaProgress.status("LENGUAJE", listOf("anagramas"), mapOf("anagramas" to 0.38f),
      listOf(ProgressPoint("anagramas", monday - 10 * day, 0.40f), ProgressPoint("anagramas", monday + day, 0.38f)), now)
    assertEquals(-0.02f, down.change, eps)
    assertEquals("Esta semana bajó un poco, de 40 a 38: es normal que varíe", AreaProgress.changeLine(down))
    // Sin historia anotada (medido antes del registro): no inventa cambios.
    val old = AreaProgress.status("RAZONAMIENTO", listOf("calculo"), mapOf("calculo" to 0.3f), emptyList(), now)
    assertEquals(0f, old.change, eps)
    assertEquals("Esta semana se mantuvo en 30", AreaProgress.changeLine(old))
    assertEquals(listOf(0.3f, 0.3f, 0.3f, 0.3f), old.weeks)
    // Sin medir.
    val none = AreaProgress.status("ATENCION", listOf("radar"), emptyMap(), emptyList(), now)
    assertNull(none.value); assertNull(AreaProgress.toNextLine(none))
    assertEquals("Aún sin medir: tu primera partida lo muestra", AreaProgress.changeLine(none))
  }

  @Test
  fun `las cuatro semanas y la frase de Nubi`() {
    val pts = listOf(ProgressPoint("radar", at(2026, java.util.Calendar.OCTOBER, 3), 0.40f), ProgressPoint("radar", at(2026, java.util.Calendar.OCTOBER, 8), 0.46f),
      ProgressPoint("radar", at(2026, java.util.Calendar.OCTOBER, 20), 0.52f))
    val s = AreaProgress.status("ATENCION", listOf("radar"), mapOf("radar" to 0.52f), pts, now)
    assertEquals(listOf(0.40f, 0.46f, 0.46f, 0.52f), s.weeks) // al final de cada semana de calendario (el lunes a las 00:00): el último punto hasta ahí
    assertEquals("el cambio de esta semana ES lo que sube de «pasada» a «esta» en el gráfico", 0.06f, s.change, eps)
    val names = mapOf("ATENCION" to "Atención", "MEMORIA" to "Memoria", "LENGUAJE" to "Lenguaje")
    val mem = AreaStatus("MEMORIA", 0.4f, 0.02f, emptyList())
    val len = AreaStatus("LENGUAJE", 0.2f, -0.02f, emptyList())
    assertEquals("Atención y Memoria avanzaron", AreaProgress.nubiLine(listOf(mem, s, len), names))
    assertEquals("Memoria avanzó esta semana", AreaProgress.nubiLine(listOf(mem, len), names))
    assertEquals("Tu avance se mantiene esta semana", AreaProgress.nubiLine(listOf(len), names))
    assertEquals("Juega y aquí verás tu avance", AreaProgress.nubiLine(listOf(AreaStatus("MEMORIA", null, 0f, emptyList())), names))
    assertEquals("tu memoria", AreaProgress.your("Memoria"))
  }

  @Test
  fun `el registro solo agrega cambios y se lee igual`() {
    var log = emptyList<ProgressPoint>()
    log = AreaProgress.append(log, ProgressPoint("radar", now - 90 * day, 0.2f))
    log = AreaProgress.append(log, ProgressPoint("radar", now - 80 * day, 0.25f))
    log = AreaProgress.append(log, ProgressPoint("radar", now - 1 * day, 0.25f)) // igual: no se anota
    log = AreaProgress.append(log, ProgressPoint("radar", now, 0.3f))
    // Se quedan los de los últimos 60 días + el último anterior (0,25) para saber dónde estaba.
    assertEquals(listOf(0.25f, 0.3f), log.map { it.progress })
    assertEquals(log, AreaProgress.decode(AreaProgress.encode(log)))
    assertEquals(0.25f, AreaProgress.at(log, "radar", now - 7 * day)!!, eps)
  }
}
