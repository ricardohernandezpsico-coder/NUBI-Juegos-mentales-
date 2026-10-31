package com.example.data

import org.junit.Assert.assertEquals
import org.junit.Assert.assertFalse
import org.junit.Assert.assertNotNull
import org.junit.Assert.assertNull
import org.junit.Assert.assertTrue
import org.junit.Test

/**
 * El ritmo de una partida de Contrarreloj (`TimeTrialTrials`, Tarea 200): lo que Unity manda como tres listas paralelas (`tt_t_ms`, `tt_ok`, `tt_level`) hecho registro, y el texto JSON de la columna `trials`. Lógica pura.
 * Todo o nada: un registro que no cuadra no se guarda (la partida sí); un texto dañado en el disco deja esa partida sin ritmo y nunca rompe la lista.
 */
class TimeTrialTrialsTest {
  private val t = listOf(1200, 3400, 5000)
  private val ok = listOf(1, 0, 1)
  private val level = listOf(1, 1, 2)

  @Test
  fun `las tres listas de Unity se vuelven un registro con un ensayo por posicion`() {
    val r = TimeTrialTrials.of(t, ok, level)!!
    assertEquals(t, r.tMs)
    assertEquals(listOf(true, false, true), r.ok)
    assertEquals(level, r.level)
    assertEquals(3, r.size)
    assertEquals("2 aciertos y 1 error", 2 to 1, r.hits to r.errors)
  }

  @Test
  fun `sin listas, vacias o de distinto largo no hay ritmo`() {
    assertNull(TimeTrialTrials.of(null, ok, level))
    assertNull(TimeTrialTrials.of(t, null, level))
    assertNull(TimeTrialTrials.of(t, ok, null))
    assertNull("una partida de siempre las manda vacías", TimeTrialTrials.of(emptyList(), emptyList(), emptyList()))
    assertNull("miden distinto: no se sabe qué ensayo es cuál", TimeTrialTrials.of(t, listOf(1, 0), level))
    assertNull(TimeTrialTrials.of(t, ok, listOf(1, 1)))
  }

  @Test
  fun `el tope es de 200 ensayos`() {
    fun n(k: Int) = TimeTrialTrials.of(List(k) { it * 100 }, List(k) { 1 }, List(k) { 1 })
    assertEquals(200, TimeTrialTrials.MAX)
    assertNotNull("200 entran", n(200))
    assertNull("201 no vienen de Unity: no se guardan", n(201))
  }

  @Test
  fun `un dato imposible deja toda la lista fuera`() {
    assertNull("ok que no es 0 ni 1", TimeTrialTrials.of(t, listOf(1, 2, 1), level))
    assertNull("un tiempo negativo", TimeTrialTrials.of(listOf(-1, 3400, 5000), ok, level))
    assertNull("los tiempos no decrecen", TimeTrialTrials.of(listOf(1200, 900, 5000), ok, level))
    assertNull("un tiempo más allá de los 120 s", TimeTrialTrials.of(listOf(1200, 3400, 120_001), ok, level))
    assertNotNull("justo 120 s sí", TimeTrialTrials.of(listOf(1200, 3400, 120_000), ok, level))
    assertNotNull("dos ensayos a la misma hora sí", TimeTrialTrials.of(listOf(1200, 1200, 5000), ok, level))
    assertNull("un paso menor que 1", TimeTrialTrials.of(t, ok, listOf(0, 1, 2)))
    assertNull("la pista nunca baja de paso", TimeTrialTrials.of(t, ok, listOf(1, 2, 1)))
    assertNotNull("el mismo paso varios ensayos sí", TimeTrialTrials.of(t, ok, listOf(2, 2, 2)))
  }

  @Test
  fun `el JSON de la columna usa los nombres de Unity y 1 o 0 por acierto o error`() {
    val r = TimeTrialTrials.of(t, ok, level)!!
    assertEquals("""{"t_ms":[1200,3400,5000],"ok":[1,0,1],"level":[1,1,2]}""", r.toJson())
  }

  @Test
  fun `lo que se guarda se lee igual`() {
    val r = TimeTrialTrials.of(t, ok, level)!!
    assertEquals(r, TimeTrialTrials.fromJson(r.toJson()))
    val big = TimeTrialTrials.of(List(200) { it * 600 }, List(200) { if (it % 7 == 3) 0 else 1 }, List(200) { 1 + it / 20 })!!
    assertEquals("también un registro entero", big, TimeTrialTrials.fromJson(big.toJson()))
    assertTrue("pesa menos de 3 KB", big.toJson().toByteArray().size < 3000)
  }

  @Test
  fun `un texto que falta o esta danado deja la partida sin ritmo, sin romper nada`() {
    for (bad in listOf(null, "", "   ", "{", "[]", "null", "no es json", """{"t_ms":[1],"ok":[1]}""", """{"t_ms":"x","ok":[1],"level":[1]}""", """{"t_ms":[5,1],"ok":[1,1],"level":[1,1]}""", """{"t_ms":[1],"ok":[3],"level":[1]}""", """{"t_ms":[],"ok":[],"level":[]}"""))
      assertNull("«$bad» no es un ritmo", TimeTrialTrials.fromJson(bad))
    assertNotNull(TimeTrialTrials.fromJson("""{"t_ms":[1],"ok":[1],"level":[1]}"""))
    assertNotNull("los campos de más no estorban", TimeTrialTrials.fromJson("""{"t_ms":[1],"ok":[1],"level":[1],"otra":5}"""))
  }

  private fun result(trials: TimeTrialTrials?, hits: Int, errors: Int) =
    TimeTrialResult(gameId = "stroop", day = "2026-10-31", trackVersion = 2, hits = hits, errors = errors, level = 2, trials = trials)

  @Test
  fun `solo una partida con el registro COMPLETO tiene ritmo`() {
    val r = TimeTrialTrials.of(t, ok, level)!!
    assertTrue(result(r, hits = 2, errors = 1).hasPace)
    assertFalse("sin registro (una partida vieja)", result(null, hits = 2, errors = 1).hasPace)
    assertFalse("registro más corto que aciertos + errores (más de 200 ensayos): los números no serían los del final", result(r, hits = 40, errors = 3).hasPace)
    assertFalse("y uno que no cuadra por menos", result(r, hits = 1, errors = 1).hasPace)
  }
}
