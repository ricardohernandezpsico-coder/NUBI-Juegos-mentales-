package com.example.data

import org.junit.Assert.assertEquals
import org.junit.Assert.assertNull
import org.junit.Test

class AreaProgressTest {
  private val day = 86_400_000L
  private val now = 100 * day
  private val eps = 0.0001f

  @Test
  fun `el cambio compara con hace 7 dias y un juego nuevo no cuenta`() {
    val pts = listOf(
      ProgressPoint("parejas", now - 20 * day, 0.30f), ProgressPoint("parejas", now - 2 * day, 0.36f),
      ProgressPoint("rumbo", now - 1 * day, 0.50f) // medido por primera vez esta semana
    )
    val s = AreaProgress.status("MEMORIA", listOf("parejas", "rumbo", "secuencia"), mapOf("parejas" to 0.36f, "rumbo" to 0.50f), pts, now)
    assertEquals(0.43f, s.value!!, eps)          // promedio de los dos medidos
    assertEquals(0.06f, s.change, eps)           // solo parejas: 0,30 → 0,36
    assertEquals(43, s.points); assertEquals(37, s.pointsBefore)
    assertEquals("Esta semana avanzó de 37 a 43", AreaProgress.changeLine(s))
    assertEquals("Te faltan 17 para Experto", AreaProgress.toNextLine(s))
  }

  @Test
  fun `bajar se dice sin culpa y lo que no se movio queda igual`() {
    val down = AreaProgress.status("LENGUAJE", listOf("anagramas"), mapOf("anagramas" to 0.38f),
      listOf(ProgressPoint("anagramas", now - 10 * day, 0.40f), ProgressPoint("anagramas", now - day, 0.38f)), now)
    assertEquals(-0.02f, down.change, eps)
    assertEquals("Esta semana bajó un poco, de 40 a 38", AreaProgress.changeLine(down))
    // Sin historia anotada (medido antes del registro): no inventa cambios.
    val old = AreaProgress.status("CALCULO", listOf("calculo"), mapOf("calculo" to 0.3f), emptyList(), now)
    assertEquals(0f, old.change, eps)
    assertEquals("Esta semana se mantuvo en 30", AreaProgress.changeLine(old))
    assertEquals(listOf(0.3f, 0.3f, 0.3f, 0.3f), old.weeks)
    // Sin medir.
    val none = AreaProgress.status("VELOCIDAD", listOf("radar"), emptyMap(), emptyList(), now)
    assertNull(none.value); assertNull(AreaProgress.toNextLine(none))
    assertEquals("Aún sin medir: tu primera partida lo muestra", AreaProgress.changeLine(none))
  }

  @Test
  fun `las cuatro semanas y la frase de Nubi`() {
    val pts = listOf(ProgressPoint("radar", now - 25 * day, 0.40f), ProgressPoint("radar", now - 12 * day, 0.46f),
      ProgressPoint("radar", now - 3 * day, 0.52f))
    val s = AreaProgress.status("VELOCIDAD", listOf("radar"), mapOf("radar" to 0.52f), pts, now)
    assertEquals(listOf(0.40f, 0.40f, 0.46f, 0.52f), s.weeks) // al final de cada semana: el último punto hasta ahí
    val names = mapOf("VELOCIDAD" to "Velocidad", "MEMORIA" to "Memoria", "LENGUAJE" to "Lenguaje")
    val mem = AreaStatus("MEMORIA", 0.4f, 0.02f, emptyList())
    val len = AreaStatus("LENGUAJE", 0.2f, -0.02f, emptyList())
    assertEquals("Velocidad y Memoria avanzaron", AreaProgress.nubiLine(listOf(mem, s, len), names))
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
