package com.example.bridge

import android.app.Application
import androidx.test.core.app.ApplicationProvider
import com.example.TestSupport
import com.example.data.PlayMode
import com.example.data.TimeTrialResult
import com.example.model.AgeBand
import com.example.model.GameRegistry
import com.example.model.GamePlayResult
import com.example.viewmodel.NeuroVidaViewModel
import java.io.File
import kotlinx.coroutines.runBlocking
import org.junit.After
import org.junit.Assert.assertEquals
import org.junit.Assert.assertFalse
import org.junit.Assert.assertTrue
import org.junit.Before
import org.junit.Test
import org.junit.runner.RunWith
import org.robolectric.RobolectricTestRunner
import org.robolectric.annotation.Config

/**
 * La primera partida de un juego (Tarea 191): «abajo y sube rápido» solo en la PRIMERA partida normal (A tu medida) de un juego sin historial, el tutorial sale una sola vez por juego aunque el primer modo sea el Contrarreloj,
 * «Jugar como la primera vez» ([Debug]) no guarda nada, y «Borrar datos» también borra las preferencias de Unity.
 */
@RunWith(RobolectricTestRunner::class)
@Config(sdk = [36])
class PrimeraPartidaTest {
  private lateinit var app: Application
  private val viewModels = mutableListOf<NeuroVidaViewModel>()

  @Before fun setUp() { app = ApplicationProvider.getApplicationContext(); TestSupport.resetDatabase() }

  @After fun tearDown() {
    TestSupport.release(*viewModels.toTypedArray())
    viewModels.clear()
    TestSupport.resetDatabase()
  }

  private fun played(game: String) = GamePlayResult(gameId = game, score = 80, correctAnswers = 8, totalTrials = 10, timed = false, level = 2)
  private fun trial(game: String) = TimeTrialResult(gameId = game, day = "2026-10-29", trackVersion = 2, hits = 30, errors = 2, level = 3)

  private fun config(game: String, mode: PlayMode = PlayMode.A_TU_MEDIDA, forceTutorial: Boolean = false, assessmentStep: Int = 0, timeTrial: Boolean = false, freshStart: Boolean = false): String =
    UnityGameLauncher.buildGameIntent(
      app, game, "u1", level = 3, baseIntensity = 0, timed = false, ageBand = AgeBand.ADULT, soundEnabled = true, mode = mode,
      forceTutorial = forceTutorial, assessmentStep = assessmentStep, assessmentTotal = if (assessmentStep > 0) 4 else 0, timeTrial = timeTrial, freshStart = freshStart
    ).getStringExtra(UnityGameLauncher.EXTRA_CONFIG_JSON)!!

  @Test
  fun `una partida de Contrarreloj cuenta como historial para el tutorial y sale una sola vez por juego`() {
    assertTrue(UnityGameLauncher.shouldShowTutorial("stroop", emptyList(), emptyList()))
    assertFalse("jugó su Contrarreloj: ya no hay tutorial", UnityGameLauncher.shouldShowTutorial("stroop", emptyList(), listOf(trial("stroop"))))
    assertTrue("el Contrarreloj de OTRO juego no cuenta", UnityGameLauncher.shouldShowTutorial("stroop", emptyList(), listOf(trial("disparate"))))
    assertFalse(UnityGameLauncher.shouldShowTutorial("stroop", listOf(played("stroop")), emptyList()))
    assertTrue("las llamadas de siempre, con dos argumentos, siguen valiendo", UnityGameLauncher.shouldShowTutorial("stroop", emptyList()))
  }

  @Test
  fun `la subida es solo de la primera partida normal de un juego sin historial`() {
    fun first(game: String = "engranajes", history: List<GamePlayResult> = emptyList(), tt: List<TimeTrialResult> = emptyList(), mode: PlayMode = PlayMode.A_TU_MEDIDA,
              assessment: Boolean = false, timeTrial: Boolean = false, force: Boolean = false) =
      UnityGameLauncher.isFirstGame(game, history, tt, mode, assessment, timeTrial, force)
    assertTrue(first())
    assertFalse("con historial", first(history = listOf(played("engranajes"))))
    assertFalse("con un Contrarreloj del juego (cuenta como historial)", first(tt = listOf(trial("engranajes"))))
    assertTrue("el historial de otro juego no cuenta", first(history = listOf(played("bodega"))))
    for (mode in listOf(PlayMode.SUAVE, PlayMode.DESAFIO, PlayMode.EXPERTO)) assertFalse("$mode no cambia", first(mode = mode))
    assertFalse("la evaluación del Primer vuelo no cambia", first(assessment = true))
    assertFalse("el Contrarreloj no cambia", first(timeTrial = true))
    assertFalse("«Cómo se juega» de la ficha (tutorial forzado) no cambia", first(force = true))
  }

  @Test
  fun `la configuracion que recibe Unity trae first_game solo en la primera partida normal`() {
    assertTrue(config("engranajes").contains("\"first_game\":true"))
    assertTrue("el tutorial sigue yendo con ella", config("engranajes").contains("\"show_tutorial\":true"))
    assertFalse(config("engranajes", mode = PlayMode.DESAFIO).contains("\"first_game\":true"))
    assertFalse(config("engranajes", forceTutorial = true).contains("\"first_game\":true"))
    assertFalse(config("freno", assessmentStep = 1).contains("\"first_game\":true"))
    assertFalse(config("engranajes").contains("\"fresh_start\":true"))
  }

  @Test
  fun `Jugar como la primera vez manda tutorial, subida y marcas nuevas, y su resultado no se guarda`() {
    val json = config("correo", freshStart = true)
    assertTrue(json.contains("\"fresh_start\":true") && json.contains("\"first_game\":true") && json.contains("\"show_tutorial\":true"))
    val intent = UnityGameLauncher.buildGameIntent(app, "correo", "u1", level = 3, baseIntensity = 0, timed = false, ageBand = AgeBand.ADULT, freshStart = true)
    val launchId = intent.getStringExtra(UnityGameLauncher.EXTRA_LAUNCH_ID)
    assertTrue(UnityGameLauncher.isFreshLaunch(launchId))
    assertFalse(UnityGameLauncher.isFreshLaunch("L5"))
    assertFalse(UnityGameLauncher.isFreshLaunch(null))

    val vm = NeuroVidaViewModel(app).also { viewModels.add(it) }
    vm.onUnityResult(played("correo"), launchId)
    vm.onUnityResult(played("correo"), "L-normal")                          // una partida de verdad sí se guarda (el contrapeso de la prueba)
    TestSupport.awaitUntil(message = "La partida normal no quedó guardada") { vm.gameHistory.value.size == 1 }
    Thread.sleep(300)
    assertEquals("la de «primera vez» no se guarda: solo queda la normal", 1, vm.gameHistory.value.size)
  }

  private fun unityFile(): File {
    val prefs = File(app.applicationInfo.dataDir, "shared_prefs").apply { mkdirs() }
    return File(prefs, UnityPrefs.fileName(app)).apply { writeText("<map><int name=\"con_intros\" value=\"3\" /></map>") }
  }

  @Test
  fun `Borrar datos con Unity apagado borra el archivo de preferencias de Unity y deja las demas`() {
    val unity = unityFile()
    val other = File(unity.parentFile, "otras_preferencias.xml").apply { writeText("<map/>") }
    assertEquals(listOf(unity.name), UnityPrefs.files(app).map { it.name })
    assertEquals("com.nubibraingames.app.v2.playerprefs.xml", unity.name)
    runBlocking { com.example.NeuroVidaApplication.instance.repository.resetData() }
    assertFalse("el archivo de Unity se borró", unity.exists())
    assertTrue("lo que no es de Unity no se toca aquí", other.exists())
    assertTrue("y Unity recibe el pedido igual (por si quedó algo en memoria)", UnityPrefs.resetPending(app))
    other.delete()
  }

  @Test
  fun `Borrar datos con el proceso de Unity VIVO detras no toca el archivo (Unity lo tiene en memoria) y le pide borrar desde dentro`() {
    val manager = app.getSystemService(android.content.Context.ACTIVITY_SERVICE) as android.app.ActivityManager
    org.robolectric.Shadows.shadowOf(manager).setProcesses(listOf(
      android.app.ActivityManager.RunningAppProcessInfo("com.nubibraingames.app", 100, emptyArray()),
      android.app.ActivityManager.RunningAppProcessInfo("com.nubibraingames.app:unity", 200, emptyArray())
    ))
    try {
      assertTrue(UnityPrefs.unityProcessAlive(app))
      val unity = unityFile()
      UnityPrefs.clear(app)
      assertTrue("con Unity vivo el archivo no se borra desde la app: Unity volvería a escribir lo que tiene en memoria", unity.exists())
      assertTrue(UnityPrefs.resetPending(app))
      val json = UnityGameLauncher.buildGameIntent(app, "engranajes", "u1", level = 3, baseIntensity = 0, timed = false, ageBand = AgeBand.ADULT).getStringExtra(UnityGameLauncher.EXTRA_CONFIG_JSON)!!
      assertTrue("la próxima configuración que recibe Unity le pide borrar sus preferencias", json.contains("\"reset_unity_prefs\":true"))
      UnityPrefs.clearPending(app)                                       // lo que hace el ViewModel al lanzar la partida
      assertFalse(UnityPrefs.resetPending(app))
      val after = UnityGameLauncher.buildGameIntent(app, "engranajes", "u1", level = 3, baseIntensity = 0, timed = false, ageBand = AgeBand.ADULT).getStringExtra(UnityGameLauncher.EXTRA_CONFIG_JSON)!!
      assertFalse("y no se lo vuelve a pedir en las siguientes", after.contains("\"reset_unity_prefs\":true"))
      unity.delete()
    } finally {
      org.robolectric.Shadows.shadowOf(manager).setProcesses(emptyList())
    }
  }
}
