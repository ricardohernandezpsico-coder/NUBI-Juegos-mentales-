package com.example

import android.app.Application
import androidx.compose.material3.Text
import androidx.compose.runtime.CompositionLocalProvider
import androidx.compose.runtime.SideEffect
import androidx.compose.runtime.getValue
import androidx.compose.runtime.mutableStateOf
import androidx.compose.runtime.setValue
import androidx.compose.ui.test.assertIsDisplayed
import androidx.compose.ui.test.junit4.createComposeRule
import androidx.compose.ui.test.onNodeWithText
import androidx.test.core.app.ApplicationProvider
import com.example.data.NeuroVidaRepository
import com.example.data.TimeTrialResult
import com.example.data.local.GameResultEntity
import com.example.data.local.NeuroVidaDatabase
import com.example.data.local.toEntity
import com.example.model.AgeBand
import com.example.model.UserSettings
import com.example.ui.components.LocalReduceMotion
import com.example.viewmodel.NeuroVidaViewModel
import com.example.viewmodel.StartupStage
import kotlinx.coroutines.runBlocking
import org.junit.After
import org.junit.Assert.assertEquals
import org.junit.Assert.assertFalse
import org.junit.Assert.assertTrue
import org.junit.Before
import org.junit.Rule
import org.junit.Test
import org.junit.runner.RunWith
import org.robolectric.RobolectricTestRunner
import org.robolectric.annotation.Config

/**
 * El arranque en frío de la app (Tarea 192, parte 2; `StartupGate` y `NeuroVidaViewModel.startupStage`): el cielo sale en el primer cuadro, el contenido se compone invisible con el perfil, entra cuando los datos están al día y nunca queda sin contenido.
 * Sin eso salía con la racha y la semana de una persona nueva y, un instante después, con las de verdad (medido en el emulador: «Tu racha empieza con tu próxima partida» con 12 días seguidos y 40 partidas ya cargadas).
 */
@RunWith(RobolectricTestRunner::class)
@Config(sdk = [36])
class ArranqueTest {
  @get:Rule val composeTestRule = createComposeRule()

  private lateinit var app: Application
  private val repos = mutableListOf<NeuroVidaRepository>()
  private val viewModels = mutableListOf<NeuroVidaViewModel>()

  @Before fun setUp() {
    app = ApplicationProvider.getApplicationContext()
    TestSupport.resetDatabase()
  }

  @After fun tearDown() {
    TestSupport.release(*viewModels.toTypedArray())
    viewModels.clear()
    repos.forEach { TestSupport.release(it) }
    repos.clear()
    TestSupport.resetDatabase()
  }

  private fun db() = NeuroVidaDatabase.getDatabase(app)

  private fun sembrar(partidas: Int, contrarreloj: Int) = runBlocking {
    val d = db()
    d.userProfileDao().insertOrUpdate(UserSettings(name = "Ricardo", ageBand = AgeBand.ADULT).toEntity())
    val now = System.currentTimeMillis()
    repeat(partidas) { i -> d.gameResultDao().insert(GameResultEntity("r$i", "radar", 80, 8, 10, false, 2, now - i * 3_600_000L)) }
    repeat(contrarreloj) { i -> d.timeTrialResultDao().insert(TimeTrialResult(id = "t$i", gameId = "stroop", day = "2026-10-${20 + i}", trackVersion = 2, hits = 30, errors = 2, level = 3, timestamp = now - (i + 1) * 86_400_000L).toEntity()) }
  }

  // ---------------------------------------------------------------- «ya llegó» es el valor de verdad, no el inicial

  @Test
  fun `cuando el repositorio dice que ya cargo los flujos traen los datos de la base`() {
    sembrar(partidas = 40, contrarreloj = 3)
    val r = NeuroVidaRepository(app).also { repos.add(it) }
    TestSupport.awaitUntil(message = "la primera carga no terminó") { r.isFirstLoadDone() }
    assertEquals("las partidas ya son las de la base, no la lista vacía inicial", 40, r.gameHistory.value.size)
    assertEquals("el Contrarreloj, igual", 3, r.timeTrialResults.value.size)
    assertEquals("el perfil de verdad (si no, Hoy abriría el recorrido de la primera vez)", AgeBand.ADULT, r.userSettings.value.ageBand)
    assertEquals("Ricardo", r.userSettings.value.name)
  }

  @Test
  fun `con la base vacia tambien termina la carga aunque los valores sean iguales a los iniciales`() {
    val r = NeuroVidaRepository(app).also { repos.add(it) }
    TestSupport.awaitUntil(message = "una instalación nueva no puede quedarse esperando: el valor de verdad es igual al inicial y el StateFlow no vuelve a emitir") { r.isFirstLoadDone() }
    assertTrue(r.gameHistory.value.isEmpty())
    assertEquals(null, r.userSettings.value.ageBand)
  }

  // ---------------------------------------------------------------- el ViewModel avisa en qué punto va la carga

  @Test
  fun `el ViewModel llega a READY con los flujos derivados ya contando todo`() {
    sembrar(partidas = 40, contrarreloj = 3)
    val vm = NeuroVidaViewModel(app).also { viewModels.add(it) }
    TestSupport.awaitUntil(message = "el arranque nunca quedó listo") { vm.startupStage.value == StartupStage.READY }
    assertEquals("las marcas de actividad = 40 partidas + 3 del Contrarreloj (el flujo derivado ya corrió cuando el arranque queda listo)", 43, vm.activityTimestamps.value.size)
    assertEquals(AgeBand.ADULT, vm.userSettings.value.ageBand)
  }

  @Test
  fun `con la base vacia el arranque tambien llega a READY`() {
    val vm = NeuroVidaViewModel(app).also { viewModels.add(it) }
    TestSupport.awaitUntil(message = "una instalación nueva no puede quedarse sin contenido") { vm.startupStage.value == StartupStage.READY }
    assertTrue(vm.gameHistory.value.isEmpty())
  }

  // ---------------------------------------------------------------- el portón de Compose

  private val contenido = "contenido de Hoy"

  /** Lo que el portón muestra, con «quitar animaciones» (el fundido es un salto). */
  private fun montar(stage: () -> StartupStage, capMs: Long = STARTUP_CAP_MS, compuesto: () -> Unit = {}) {
    composeTestRule.setContent {
      CompositionLocalProvider(LocalReduceMotion provides true) { StartupGate(stage = stage(), capMs = capMs) { SideEffect { compuesto() }; Text(contenido) } }
    }
  }

  @Test
  fun `sin el perfil solo hay cielo con el perfil el contenido se compone invisible y con todo al dia entra`() {
    var stage by mutableStateOf(StartupStage.LOADING)
    var compuesto = false
    montar({ stage }, capMs = 1_000_000L) { compuesto = true }          // sin tope: acá se prueba el orden de las etapas
    composeTestRule.waitForIdle()
    assertFalse("mientras el perfil no llega no se compone nada: no se sabe si toca Hoy o el Primer vuelo", compuesto)
    composeTestRule.runOnUiThread { stage = StartupStage.PROFILE }
    composeTestRule.waitForIdle()
    assertTrue("con el perfil el contenido ya se compone, para que la composición corra mientras llegan las partidas", compuesto)
    composeTestRule.onNodeWithText(contenido).assertDoesNotExist()      // fuera del lector de pantalla: todavía no se ve
    composeTestRule.runOnUiThread { stage = StartupStage.READY }
    composeTestRule.waitForIdle()
    composeTestRule.onNodeWithText(contenido).assertIsDisplayed()
  }

  @Test
  fun `a 1 segundo del primer cuadro entra lo que haya aunque la base no haya contestado`() {
    var compuesto = false
    composeTestRule.mainClock.autoAdvance = false
    montar({ StartupStage.LOADING }) { compuesto = true }
    composeTestRule.mainClock.advanceTimeBy(STARTUP_CAP_MS - 50)
    assertFalse(compuesto)
    composeTestRule.mainClock.advanceTimeBy(300)
    composeTestRule.onNodeWithText(contenido).assertIsDisplayed()
    assertEquals(1000L, STARTUP_CAP_MS)
  }

  @Test
  fun `con la app ya cargada el contenido sale en el primer cuadro`() {
    composeTestRule.mainClock.autoAdvance = false
    montar({ StartupStage.READY })
    composeTestRule.mainClock.advanceTimeBy(50)
    composeTestRule.onNodeWithText(contenido).assertIsDisplayed()
  }
}
