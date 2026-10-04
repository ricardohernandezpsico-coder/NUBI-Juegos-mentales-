package com.example.bridge

import com.example.data.PuntaTier
import org.junit.Assert.assertEquals
import org.junit.Assert.assertNotNull
import org.junit.Assert.assertNull
import org.junit.Assert.assertTrue
import org.junit.Test
import org.junit.runner.RunWith
import org.robolectric.RobolectricTestRunner
import org.robolectric.annotation.Config

/** El resultado de «En la punta de la lengua» tal como lo manda Unity (JsonUtility: -1 o "" = sin dato, palabras separadas por «;»). */
@RunWith(RobolectricTestRunner::class)
@Config(sdk = [36])
class PuntaTelemetryTest {
  private fun json(extra: String, game: String = "anagramas") =
    """{"user_id":"u1","game_id":"$game","session_metrics":{"correct_trials":7,"total_trials":8,"calculated_score":71,"average_response_time_ms":3100,"level":3,"timed":false,"end_rating":0.5,"peak_level":4,"mode_trials":6,"mode_hits":5,$extra}}"""

  @Test
  fun `lee los cuatro luceros, el tiempo, la lista y las azules`() {
    val r = NativeReceiver.parse(
      json(
        """"punta_solo":4,"punta_pista":2,"punta_letras":1,"punta_vista":1,"punta_ms":3100,""" +
          """"punta_words":"reloj:o;faro:p;búho:a;nido:c","punta_blue":"búho","punta_cleared":"faro;nido""""
      )
    )
    assertNotNull(r)
    r!!
    assertEquals("anagramas", r.gameId)
    assertEquals(71, r.score)
    assertEquals(4, r.puntaSolo)
    assertEquals(2, r.puntaPista)
    assertEquals(1, r.puntaLetras)
    assertEquals(1, r.puntaVista)
    assertEquals(3100, r.puntaMs)
    assertEquals(listOf("reloj", "faro", "búho", "nido"), r.puntaWords!!.map { it.word })
    assertEquals(listOf(PuntaTier.SOLO, PuntaTier.PISTA, PuntaTier.VISTA, PuntaTier.LETRAS), r.puntaWords!!.map { it.tier })
    assertEquals(listOf("búho"), r.puntaBlue)
    assertEquals(listOf("faro", "nido"), r.puntaCleared)
  }

  @Test
  fun `lo que Unity manda como sin dato queda en null`() {
    val r = NativeReceiver.parse(json(""""punta_words":"","punta_blue":"","punta_cleared":"","punta_solo":0,"punta_pista":0,"punta_letras":0,"punta_vista":0,"punta_ms":-1"""))!!
    assertEquals(0, r.puntaSolo)
    assertNull(r.puntaMs)
    assertNull(r.puntaWords)
    assertTrue(r.puntaBlue!!.isEmpty())
    assertTrue(r.puntaCleared!!.isEmpty())
  }

  @Test
  fun `una version vieja sin los campos nuevos no rompe nada`() {
    val r = NativeReceiver.parse(json(""""campo_nuevo_desconocido":3"""))!!
    assertNull(r.puntaSolo)
    assertNull(r.puntaWords)
  }

  @Test
  fun `otros juegos no traen los campos de este`() {
    val r = NativeReceiver.parse(json(""""punta_solo":5,"punta_words":"reloj:o"""", game = "calculo"))!!
    assertNull(r.puntaSolo)
    assertNull(r.puntaWords)
    assertNull(r.puntaBlue)
  }
}
