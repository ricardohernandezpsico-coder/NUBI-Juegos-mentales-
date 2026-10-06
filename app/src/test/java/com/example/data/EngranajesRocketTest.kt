package com.example.data

import android.app.Application
import androidx.test.core.app.ApplicationProvider
import com.example.TestSupport
import com.example.bridge.NativeReceiver
import com.example.data.local.NeuroVidaDatabase
import kotlinx.coroutines.runBlocking
import org.junit.After
import org.junit.Assert.assertEquals
import org.junit.Assert.assertNotNull
import org.junit.Before
import org.junit.Test
import org.junit.runner.RunWith
import org.robolectric.RobolectricTestRunner
import org.robolectric.annotation.Config

/** El cohete de «Engranajes»: las luces y los cohetes en órbita se guardan al terminar la partida (en cualquier modo), sobreviven a reiniciar la app y se mandan a Unity en la siguiente. */
@RunWith(RobolectricTestRunner::class)
@Config(sdk = [36])
class EngranajesRocketTest {
  @get:org.junit.Rule val retryOnDbFlake = TestSupport.retryOnDbFlake()

  private lateinit var app: Application
  private lateinit var repo: NeuroVidaRepository

  @Before
  fun setUp() {
    app = ApplicationProvider.getApplicationContext()
    TestSupport.resetDatabase()
    app.getSharedPreferences("engranajes_rocket", android.content.Context.MODE_PRIVATE).edit().clear().commit()
    repo = NeuroVidaRepository(app)
    TestSupport.awaitUntil(message = "La base de datos no se inicializó") {
      runBlocking { NeuroVidaDatabase.getDatabase(app).gameProgressDao().getAllProgressSync().isNotEmpty() }
    }
  }

  @After
  fun tearDown() {
    TestSupport.resetDatabase()
  }

  private fun game(lights: Int, orbit: Int, launches: Int = 0, game: String = "engranajes") = NativeReceiver.parse(
    """{"user_id":"u1","game_id":"$game","session_metrics":{"correct_trials":7,"total_trials":10,"calculated_score":70,"average_response_time_ms":7500,"level":3,"timed":false,"end_rating":0.5,"peak_level":4,"mode_trials":8,"mode_hits":6,"engr_etapa":3,"engr_lights":$lights,"engr_orbit":$orbit,"engr_launches":$launches,"engr_ms":7500}}"""
  )!!

  private fun saved() = app.getSharedPreferences("engranajes_rocket", android.content.Context.MODE_PRIVATE).let { it.getInt("lights", -1) to it.getInt("orbit", -1) }

  @Test
  fun `empieza sin luces ni cohetes`() {
    assertEquals(Engranajes.Rocket(0, 0), repo.engranajesRocket.value)
  }

  @Test
  fun `las luces y los cohetes de una partida quedan guardados en las preferencias del respaldo`() = runBlocking {
    repo.recordGameResult(game(lights = 6, orbit = 1), countsForDailySession = false)
    assertEquals(Engranajes.Rocket(6, 1), repo.engranajesRocket.value)
    assertEquals(6 to 1, saved())
  }

  @Test
  fun `si el cohete despego en medio de la partida, queda guardado el nuevo con uno mas en orbita`() = runBlocking {
    // Unity manda el estado con que termina: el cohete despegó (más órbita) y siguió con 3 luces del siguiente. La app guarda ESE estado, sin sumar ni restar nada por su cuenta.
    repo.recordGameResult(game(lights = 3, orbit = 2, launches = 1), countsForDailySession = false)
    assertEquals(Engranajes.Rocket(3, 2), repo.engranajesRocket.value)
    assertEquals(3 to 2, saved())
  }

  @Test
  fun `sobrevive a reiniciar la app (queda en las preferencias y se lee igual al abrir)`() = runBlocking {
    repo.recordGameResult(game(lights = 5, orbit = 4), countsForDailySession = true)
    // lo que lee el repositorio al abrir la app: las mismas preferencias (no se crea otro repositorio: dejaría trabajo de fondo sobre la base de la prueba siguiente)
    val prefs = app.getSharedPreferences("engranajes_rocket", android.content.Context.MODE_PRIVATE)
    assertEquals(Engranajes.Rocket(5, 4), Engranajes.rocket(prefs.getInt("lights", 0), prefs.getInt("orbit", 0)))
  }

  @Test
  fun `una partida de otro juego o de una version vieja no toca el cohete`() = runBlocking {
    repo.recordGameResult(game(lights = 5, orbit = 1), countsForDailySession = false)
    repo.recordGameResult(game(lights = 9, orbit = 9, game = "calculo"), countsForDailySession = false)
    val old = NativeReceiver.parse(
      """{"user_id":"u1","game_id":"engranajes","session_metrics":{"correct_trials":7,"total_trials":10,"calculated_score":70,"average_response_time_ms":7500,"level":3,"timed":false}}"""
    )!!
    repo.recordGameResult(old, countsForDailySession = false)
    assertEquals(Engranajes.Rocket(5, 1), repo.engranajesRocket.value)
  }

  @Test
  fun `la medida de la partida se guarda en las medidas del juego`() = runBlocking {
    repo.recordGameResult(game(lights = 2, orbit = 0), countsForDailySession = false)
    val p = repo.starMeasures.value.lastOrNull { it.key == "taller" }
    assertNotNull(p)
    assertEquals(70f, p!!.value, 1e-3f)      // 7 de 10 máquinas
  }
}
