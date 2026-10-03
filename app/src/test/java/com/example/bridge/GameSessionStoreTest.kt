package com.example.bridge

import com.example.data.PlayMode
import org.junit.Assert.assertEquals
import org.junit.Assert.assertFalse
import org.junit.Assert.assertNotNull
import org.junit.Assert.assertNull
import org.junit.Assert.assertTrue
import org.junit.Test
import org.junit.runner.RunWith
import org.robolectric.RobolectricTestRunner
import org.robolectric.annotation.Config

/**
 * Lo que la app anota en disco mientras Unity está al frente, por si Android la cierra (ver GameSessionStore). Si esto
 * falla, después de un juego no aparece el resultado, se pierde el camino diario o la evaluación empieza de cero.
 */
@RunWith(RobolectricTestRunner::class)
@Config(sdk = [36])
class GameSessionStoreTest {
  private fun flight(id: String = "L1", game: String = "radar") =
    GameSessionStore.InFlight(id, game, level = 3, timed = true, daily = true, intensity = 2, assessmentStep = 0, mode = PlayMode.DESAFIO)

  @Test
  fun `la partida en curso se recupera con su id y con todo lo que llevaba`() {
    GameSessionStore.saveInFlight(flight())
    assertEquals(flight(), GameSessionStore.inFlight("L1"))
  }

  @Test
  fun `una partida distinta o sin id no se confunde con la anotada`() {
    GameSessionStore.saveInFlight(flight("L1"))
    assertNull(GameSessionStore.inFlight("OTRA"))
    assertNull(GameSessionStore.inFlight(null))
    assertFalse(GameSessionStore.hasInFlight("OTRA"))
    assertTrue(GameSessionStore.hasInFlight("L1"))
  }

  @Test
  fun `al terminar la partida queda borrada, tambien el modo`() {
    GameSessionStore.saveInFlight(flight())
    GameSessionStore.clearInFlight()
    assertNull(GameSessionStore.inFlight("L1"))
    // Una partida nueva sin modo no debe heredar el de la anterior.
    GameSessionStore.saveInFlight(flight("L2").copy(mode = PlayMode.A_TU_MEDIDA))
    assertEquals(PlayMode.A_TU_MEDIDA, GameSessionStore.inFlight("L2")?.mode)
  }

  @Test
  fun `un resultado pendiente se entrega una sola vez`() {
    GameSessionStore.savePendingResult("L1", """{"game_id":"radar"}""")
    assertEquals("L1" to """{"game_id":"radar"}""", GameSessionStore.takePendingResult())
    assertNull(GameSessionStore.takePendingResult())
  }

  @Test
  fun `borrar datos no deja nada`() {
    GameSessionStore.saveInFlight(flight())
    GameSessionStore.savePendingResult("L1", "{}")
    GameSessionStore.clearAll()
    assertNull(GameSessionStore.inFlight("L1"))
    assertNull(GameSessionStore.takePendingResult())
  }
}
