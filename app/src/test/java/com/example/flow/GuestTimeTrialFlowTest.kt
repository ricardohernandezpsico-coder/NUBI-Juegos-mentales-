package com.example.flow

import android.app.Application
import androidx.test.core.app.ApplicationProvider
import com.example.TestSupport
import com.example.bridge.UnityGameLauncher
import com.example.data.local.NeuroVidaDatabase
import com.example.data.local.toDomain
import com.example.model.AgeBand
import com.example.model.AgeRange
import com.example.model.GamePlayResult
import com.example.viewmodel.NeuroVidaViewModel
import kotlinx.coroutines.runBlocking
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
 * El «[Debug] Contrarreloj de invitado» (Tareas 205 y 205c; solo depuración): una persona juega la pista de HOY con SU edad en el teléfono de Ricardo y el final solo muestra la marca; NO se guarda nada (ni la partida, ni el récord, ni la racha, ni lo «ya visto» de Unity).
 * Desde la 205c el invitado ELIGE su edad antes de jugar (una de las seis franjas de 18 en adelante): la partida va con la banda de esa edad —de 65 años en adelante, Tinta llega ×1,3 más tarde, como en los robots de la referencia— y el final dice la edad para anotarla; ve el tutorial como alguien nuevo; y lo que queda de un
 * final de invitado no sobrevive a una partida nueva.
 */
@RunWith(RobolectricTestRunner::class)
@Config(sdk = [36])
class GuestTimeTrialFlowTest {
  private lateinit var app: Application
  private val viewModels = mutableListOf<NeuroVidaViewModel>()

  @Before fun setUp() { app = ApplicationProvider.getApplicationContext(); TestSupport.resetDatabase() }

  @After fun tearDown() {
    TestSupport.release(*viewModels.toTypedArray())
    viewModels.clear()
    TestSupport.resetDatabase()
  }

  private fun newViewModel(): NeuroVidaViewModel = NeuroVidaViewModel(app).also { viewModels.add(it) }

  private fun timeTrialResult(gameId: String, hits: Int, errors: Int = 4) =
    GamePlayResult(
      gameId = gameId, score = 50, correctAnswers = hits, totalTrials = hits + errors, timed = true, level = 2, endRating = 0.5f, modeTrials = 0, modeHits = 0,
      ttHits = hits, ttErrors = errors, ttPeakLevel = 2, ttSeconds = 120, ttTrackVersion = 2
    )

  private fun normalResult(game: String = "calculo") = GamePlayResult(gameId = game, score = 80, correctAnswers = 8, totalTrials = 10, timed = false, level = 2)

  private fun savedTimeTrials() = runBlocking { NeuroVidaDatabase.getDatabase(app).timeTrialResultDao().getAllSync().map { it.toDomain() } }
  private fun savedResults() = runBlocking { NeuroVidaDatabase.getDatabase(app).gameResultDao().getAllResultsSync().size }

  /** Lo que se manda a Unity: la configuración en texto y el id del lanzamiento. */
  private fun launch(range: AgeRange?, guest: Boolean = true): Pair<String, String> {
    val intent = UnityGameLauncher.buildGameIntent(
      app, "stroop", "u1", level = 3, baseIntensity = 0, timed = true, ageBand = range?.toBand() ?: AgeBand.ADULT, timeTrial = true, trackSeed = 20261101, guest = guest
    )
    return intent.getStringExtra(UnityGameLauncher.EXTRA_CONFIG_JSON)!! to intent.getStringExtra(UnityGameLauncher.EXTRA_LAUNCH_ID)!!
  }

  @Test
  fun el_invitado_elige_su_edad_y_la_partida_se_juega_con_la_banda_de_esa_edad() {
    val vm = newViewModel()
    vm.startGuestTimeTrial("stroop", AgeRange.R65_74)
    val session = vm.activeGame.value!!
    assertEquals(AgeRange.R65_74, session.guestRange)
    assertEquals("la pista de hoy", vm.todayKey(), session.timeTrialDay)
    assertTrue("siempre contra el reloj", session.timed)
    val (json, launchId) = launch(session.guestRange)
    assertTrue("a Unity va la banda de la edad elegida (65 a 74 = SENIOR), no la del dueño: $json", json.contains("\"age_band\":\"SENIOR\""))
    assertTrue(UnityGameLauncher.isGuestLaunch(launchId))
    // y a una persona de 30 a 44 años, la de los adultos
    assertTrue(launch(AgeRange.R30_44).first.contains("\"age_band\":\"ADULT\""))
    assertTrue(launch(AgeRange.R75_PLUS).first.contains("\"age_band\":\"SENIOR\""))
  }

  @Test
  fun el_invitado_ve_el_tutorial_como_una_persona_nueva_y_Unity_no_lee_ni_escribe_lo_ya_visto() {
    val (json, _) = launch(AgeRange.R45_54)
    assertTrue("el tutorial guiado va siempre (aunque el dueño ya jugó): $json", json.contains("\"show_tutorial\":true"))
    assertTrue("Unity no lee ni escribe lo «ya visto»", json.contains("\"fresh_start\":true"))
    // una partida que no es de invitado NO lleva ese prefijo ni fuerza nada
    val (normalJson, normalId) = launch(null, guest = false)
    assertFalse(UnityGameLauncher.isGuestLaunch(normalId))
    assertFalse(normalJson.contains("\"fresh_start\":true"))
  }

  @Test
  fun el_final_de_invitado_no_guarda_nada_y_dice_la_marca_y_la_edad_elegida() {
    val vm = newViewModel()
    vm.startGuestTimeTrial("stroop", AgeRange.R55_64)
    val id = UnityGameLauncher.GUEST_LAUNCH_PREFIX + "prueba-1"
    vm.onUnityLaunched(id)
    vm.onUnityResult(timeTrialResult("stroop", hits = 38, errors = 4), id)
    TestSupport.awaitUntil(message = "No se abrió el final de invitado") { vm.guestTrial.value != null }
    val trial = vm.guestTrial.value!!
    assertEquals("stroop", trial.gameId)
    assertEquals(38, trial.hits); assertEquals(4, trial.errors)
    assertEquals("la marca es aciertos menos errores", 34, trial.mark)
    assertEquals("y la edad elegida, para anotarla", AgeRange.R55_64, trial.range)
    assertNotNull("hay un final abierto (la barra y las pestañas se esconden)", vm.lastResult.value)
    assertNull("la sesión terminó", vm.activeGame.value)
    Thread.sleep(300)           // por si algo se guardara de fondo
    assertTrue("ni una fila en time_trial_results", savedTimeTrials().isEmpty())
    assertEquals("ni una partida en game_results", 0, savedResults())
    assertTrue("ni en lo que lee la app", vm.timeTrialResults.value.isEmpty() && vm.gameHistory.value.isEmpty())
    assertNull("no es el final del Contrarreloj de siempre (sin récord ni marca nueva)", vm.lastTimeTrial.value)
  }

  @Test
  fun lo_que_queda_de_un_final_de_invitado_no_sobrevive_a_una_partida_nueva() {
    val vm = newViewModel()
    vm.startGuestTimeTrial("stroop", AgeRange.R45_54)
    val first = UnityGameLauncher.GUEST_LAUNCH_PREFIX + "a"
    vm.onUnityLaunched(first)
    vm.onUnityResult(timeTrialResult("stroop", hits = 30), first)
    TestSupport.awaitUntil(message = "No se abrió el final de invitado") { vm.guestTrial.value != null }
    // «Otra vez»: lanzar de nuevo limpia el final viejo
    vm.startGuestTimeTrial("stroop", AgeRange.R45_54)
    assertNull("al lanzar otra partida de invitado el final de antes se va", vm.guestTrial.value)
    // …«Salir» de la pausa y una partida de siempre: nunca aparece el final de invitado viejo
    vm.launchGame("calculo")
    assertNull("tampoco con una partida de siempre", vm.guestTrial.value)
    vm.onUnityLaunched("L9")
    vm.onUnityResult(normalResult("calculo"), "L9")
    TestSupport.awaitUntil(message = "No se abrió el final de la partida de siempre") { vm.lastResult.value?.first?.gameId == "calculo" }
    assertNull("el final de la partida de siempre no es el de invitado", vm.guestTrial.value)
    TestSupport.awaitUntil(message = "La partida de siempre no se guardó") { savedResults() == 1 }
  }

  @Test
  fun cerrar_el_final_de_invitado_lo_quita_y_una_partida_de_invitado_en_pausa_no_se_retoma_como_otra_edad() {
    val vm = newViewModel()
    vm.startGuestTimeTrial("stroop", AgeRange.R65_74)
    val sessionA = vm.activeGame.value!!
    vm.closeGameOrResult()
    assertNull(vm.guestTrial.value)
    // la misma partida pedida con OTRA edad es una partida nueva (no retoma la de 65 a 74)
    vm.startGuestTimeTrial("stroop", AgeRange.R30_44)
    assertEquals(AgeRange.R30_44, vm.activeGame.value!!.guestRange)
    assertTrue(vm.activeGame.value!!.sessionToken != sessionA.sessionToken)
  }
}
