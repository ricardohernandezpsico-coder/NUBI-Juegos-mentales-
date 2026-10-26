package com.example.bridge

import android.app.Application
import androidx.test.core.app.ApplicationProvider
import com.example.TestSupport
import com.example.model.AgeBand
import org.junit.After
import org.junit.Assert.assertEquals
import org.junit.Assert.assertFalse
import org.junit.Assert.assertNotNull
import org.junit.Assert.assertNull
import org.junit.Assert.assertTrue
import org.junit.Before
import org.junit.Test
import org.junit.runner.RunWith
import org.robolectric.RobolectricTestRunner
import org.robolectric.annotation.Config

/**
 * El Contrarreloj entre la app y Unity (Tarea 177; `docs/contrarreloj.md`): lo que la app manda para lanzarlo (`time_trial` y `track_seed`, la fecha del día) y lo que Unity devuelve (`tt_hits`, `tt_errors`, `tt_peak_level`, `tt_seconds`, `tt_track_version`; -1 = sin dato).
 */
@RunWith(RobolectricTestRunner::class)
@Config(sdk = [36])
class TimeTrialBridgeTest {
  private lateinit var app: Application

  @Before fun setUp() { app = ApplicationProvider.getApplicationContext(); TestSupport.resetDatabase() }
  @After fun tearDown() { TestSupport.resetDatabase() }

  private fun json(game: String, extra: String) =
    """{"user_id":"u1","game_id":"$game","session_metrics":{"correct_trials":38,"total_trials":43,"calculated_score":88,"average_response_time_ms":1500,"level":3,"timed":true,"end_rating":0.5,"peak_level":4,"mode_trials":0,"mode_hits":0$extra}}"""

  // ------------------------------------------------------------------ lo que Unity devuelve

  @Test
  fun `lee la marca, los errores, el paso, los segundos y la version de la pista`() {
    val r = NativeReceiver.parse(json("stroop", ""","tt_hits":38,"tt_errors":5,"tt_peak_level":4,"tt_seconds":120,"tt_track_version":2"""))
    assertNotNull(r)
    r!!
    assertEquals(38, r.ttHits)
    assertEquals(5, r.ttErrors)
    assertEquals(4, r.ttPeakLevel)
    assertEquals(120, r.ttSeconds)
    assertEquals(2, r.ttTrackVersion)
    assertTrue("con marca, es un Contrarreloj", r.isTimeTrial)
  }

  @Test
  fun `cero aciertos es una marca valida y menos uno es sin dato`() {
    val zero = NativeReceiver.parse(json("disparate", ""","tt_hits":0,"tt_errors":9,"tt_peak_level":1,"tt_seconds":120,"tt_track_version":1"""))!!
    assertEquals("cero aciertos sigue siendo una partida de Contrarreloj", 0, zero.ttHits)
    assertTrue(zero.isTimeTrial)
    val none = NativeReceiver.parse(json("disparate", ""","tt_hits":-1,"tt_errors":-1,"tt_peak_level":-1,"tt_seconds":-1,"tt_track_version":-1"""))!!
    assertNull(none.ttHits)
    assertNull(none.ttErrors)
    assertNull(none.ttPeakLevel)
    assertNull(none.ttSeconds)
    assertNull(none.ttTrackVersion)
    assertFalse(none.isTimeTrial)
  }

  @Test
  fun `una partida de siempre, o de una version vieja de Unity, no trae nada del Contrarreloj`() {
    for (game in listOf("stroop", "disparate", "radar", "acoplamiento")) {
      val r = NativeReceiver.parse(json(game, ""))!!
      assertNull("$game: sin tt_hits no hay marca", r.ttHits)
      assertFalse(r.isTimeTrial)
    }
  }

  @Test
  fun `sin marca, los otros campos del Contrarreloj no se toman`() {
    val r = NativeReceiver.parse(json("radar", ""","tt_hits":-1,"tt_errors":3,"tt_peak_level":5,"tt_seconds":120,"tt_track_version":1"""))!!
    assertNull(r.ttHits)
    assertNull("sin marca no hay errores sueltos", r.ttErrors)
    assertNull(r.ttPeakLevel)
    assertNull(r.ttSeconds)
    assertNull(r.ttTrackVersion)
  }

  // ------------------------------------------------------------------ lo que la app manda

  private fun config(gameId: String, timeTrial: Boolean, trackSeed: Int): String =
    UnityGameLauncher.buildGameIntent(
      app, gameId, "u1", level = 3, baseIntensity = 50, timed = true, ageBand = AgeBand.ADULT, soundEnabled = true,
      timeTrial = timeTrial, trackSeed = trackSeed
    ).getStringExtra(UnityGameLauncher.EXTRA_CONFIG_JSON)!!

  @Test
  fun `el Contrarreloj lanza los cuatro juegos con la fecha como semilla de la pista`() {
    for (game in listOf("stroop", "disparate", "radar", "acoplamiento")) {
      val cfg = config(game, timeTrial = true, trackSeed = 20261026)
      assertTrue("$game: time_trial en true", cfg.contains("\"time_trial\":true"))
      assertTrue("$game: la fecha es la semilla", cfg.contains("\"track_seed\":20261026"))
    }
  }

  @Test
  fun `una partida de siempre no lleva ni el modo ni la semilla`() {
    val cfg = config("stroop", timeTrial = false, trackSeed = 20261026)
    assertTrue(cfg.contains("\"time_trial\":false"))
    assertTrue("sin Contrarreloj la semilla se descarta", cfg.contains("\"track_seed\":0"))
  }

  @Test
  fun `un juego sin pista fija no se lanza como Contrarreloj aunque se pida`() {
    val cfg = config("calculo", timeTrial = true, trackSeed = 20261026)
    assertTrue("Carga exacta no está en la primera tanda", cfg.contains("\"time_trial\":false"))
    assertTrue(cfg.contains("\"track_seed\":0"))
  }
}
