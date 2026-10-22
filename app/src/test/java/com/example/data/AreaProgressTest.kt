package com.example.data

import org.junit.Assert.assertEquals
import org.junit.Test

/** El registro del avance de cada juego (`progress_log`): solo se agrega cuando cambia, se guarda un año y se lee igual. Los puntos por área que salen de él: `AreaPointsTest` (Tarea 159). */
class AreaProgressTest {
  private val day = 86_400_000L
  private val now = 1_800_000_000_000L

  @Test
  fun `el registro solo agrega cambios y se lee igual`() {
    var log = emptyList<ProgressPoint>()
    log = AreaProgress.append(log, ProgressPoint("radar", now - 400 * day, 0.2f))
    log = AreaProgress.append(log, ProgressPoint("radar", now - 380 * day, 0.25f))
    log = AreaProgress.append(log, ProgressPoint("radar", now - 1 * day, 0.25f)) // igual: no se anota
    log = AreaProgress.append(log, ProgressPoint("radar", now, 0.3f))
    // Se quedan los del último año + el último anterior (0,25) para saber dónde estaba.
    assertEquals(listOf(0.25f, 0.3f), log.map { it.progress })
    assertEquals(log, AreaProgress.decode(AreaProgress.encode(log)))
    assertEquals("Tarea 159: un año (antes 60 días), para que la mejor marca de un área y las 8 semanas del gráfico tengan de dónde leer", 365, AreaProgress.KEEP_DAYS)
  }

  @Test
  fun `lo de hace meses sigue en el registro y la mejor marca del area lo ve`() {
    var log = emptyList<ProgressPoint>()
    log = AreaProgress.append(log, ProgressPoint("parejas", now - 200 * day, 0.8f))      // hace más de 60 días: antes ya se habría ido
    log = AreaProgress.append(log, ProgressPoint("parejas", now - 3 * day, 0.6f))
    assertEquals(2, log.size)
    val area = AreaPoints.compute("MEMORIA", AreaPoints.gamesOf("MEMORIA"), log, now)
    assertEquals(150, area.total); assertEquals(200, area.best)
  }

  @Test
  fun `las lineas mal escritas no rompen la lectura`() {
    assertEquals(listOf(ProgressPoint("radar", 5L, 0.3f)), AreaProgress.decode("radar|5|300\nbasura\nradar|x|1\nradar|6|y"))
    assertEquals(emptyList<ProgressPoint>(), AreaProgress.decode(null))
  }
}
