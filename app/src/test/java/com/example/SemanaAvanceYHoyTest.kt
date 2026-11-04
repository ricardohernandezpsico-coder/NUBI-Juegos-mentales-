package com.example

import android.app.Application
import androidx.activity.ComponentActivity
import androidx.compose.foundation.layout.Box
import androidx.compose.foundation.layout.height
import androidx.compose.foundation.layout.width
import androidx.compose.runtime.CompositionLocalProvider
import androidx.compose.ui.Modifier
import androidx.compose.ui.platform.LocalDensity
import androidx.compose.ui.semantics.SemanticsProperties
import androidx.compose.ui.semantics.getOrNull
import androidx.compose.ui.test.junit4.createAndroidComposeRule
import androidx.compose.ui.test.onNodeWithTag
import androidx.compose.ui.unit.Density
import androidx.compose.ui.unit.dp
import androidx.test.core.app.ApplicationProvider
import com.example.data.TimeTrialResult
import com.example.data.Week
import com.example.data.local.NeuroVidaDatabase
import com.example.data.local.toEntity
import com.example.model.AgeBand
import com.example.model.UserSettings
import com.example.ui.components.LocalReduceMotion
import com.example.ui.theme.NeuroVidaTheme
import com.example.viewmodel.AppTab
import com.example.viewmodel.NeuroVidaViewModel
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
import org.robolectric.annotation.GraphicsMode

/**
 * La semana de Avance dice lo mismo que la de Hoy (Tarea 227, 4-nov; error que Ricardo vio en un video): Avance contaba los días con `gameHistory`, que no trae el Contrarreloj, y decía «0 de 4 días de tu meta» con las siete llamitas apagadas
 * mientras Hoy, que usa `activityTimestamps` (las partidas de siempre Y el Contrarreloj), decía «2 de 4» con dos llamitas encendidas, porque esos dos días solo se había jugado Contrarreloj. Estas pruebas componen la app REAL con su ViewModel.
 */
@RunWith(RobolectricTestRunner::class)
@GraphicsMode(GraphicsMode.Mode.NATIVE)
@Config(qualifiers = "w393dp-h873dp-440dpi", sdk = [36])
class SemanaAvanceYHoyTest {
  @get:Rule val rule = createAndroidComposeRule<ComponentActivity>()

  private lateinit var app: Application
  private val viewModels = mutableListOf<NeuroVidaViewModel>()

  @Before fun setUp() {
    app = ApplicationProvider.getApplicationContext()
    TestSupport.resetDatabase()
    TestSupport.fastFrames()
  }

  @After fun tearDown() {
    TestSupport.release(*viewModels.toTypedArray())
    viewModels.clear()
    TestSupport.resetDatabase()
  }

  private fun viewModel(): NeuroVidaViewModel {
    runBlocking { NeuroVidaDatabase.getDatabase(app).userProfileDao().insertOrUpdate(UserSettings(ageBand = AgeBand.ADULT, name = "Ricardo", weeklyGoal = 4).toEntity()) }
    val vm = NeuroVidaViewModel(app).also { viewModels.add(it) }
    TestSupport.awaitUntil(30_000, "El perfil no se cargó") { vm.userSettings.value.ageBand != null }
    return vm
  }

  private fun show(vm: NeuroVidaViewModel) {
    rule.setContent {
      val base = LocalDensity.current
      NeuroVidaTheme {
        CompositionLocalProvider(LocalReduceMotion provides true, LocalDensity provides Density(base.density, 1f)) {
          Box(Modifier.width(393.dp).height(873.dp)) { NeuroVidaApp(viewModel = vm) }
        }
      }
    }
    rule.waitForIdle()
  }

  /** Dos partidas de Contrarreloj de ESTA semana, en días distintos si la semana ya los tiene (lunes y martes a las 10:00; si hoy es lunes o martes por la mañana, lo que ya pasó). */
  private fun playOnlyTimeTrials(): List<Long> {
    val now = System.currentTimeMillis()
    val weekStart = Week.weekStart(now)
    val stamps = listOf(weekStart + 10L * 3_600_000L, weekStart + 34L * 3_600_000L).map { minOf(it, now - 60_000L) }
    runBlocking {
      val d = NeuroVidaDatabase.getDatabase(app)
      stamps.forEachIndexed { i, ts -> d.timeTrialResultDao().insert(TimeTrialResult(id = "t227-$i", gameId = "stroop", day = "2026-11-0${i + 1}", trackVersion = 2, hits = 30, errors = 2, level = 3, timestamp = ts).toEntity()) }
    }
    return stamps
  }

  private fun textOf(tag: String): String = rule.onNodeWithTag(tag, useUnmergedTree = true).fetchSemanticsNode().config[SemanticsProperties.Text].joinToString("") { it.text }
  private fun describe(tag: String): String? = rule.onNodeWithTag(tag, useUnmergedTree = true).fetchSemanticsNode().config.getOrNull(SemanticsProperties.ContentDescription)?.firstOrNull()
  private fun goTo(vm: NeuroVidaViewModel, tab: AppTab) { rule.runOnUiThread { vm.setTab(tab) }; rule.waitForIdle() }

  @Test
  fun quien_esta_semana_solo_jugo_Contrarreloj_ve_lo_mismo_en_Hoy_y_en_Avance() {
    val vm = viewModel()
    show(vm)
    val stamps = playOnlyTimeTrials()
    TestSupport.awaitUntil(15_000, "El ViewModel no vio el Contrarreloj") { vm.activityTimestamps.value.size == stamps.size }
    rule.waitForIdle()
    val expected = Week.lampsFor(stamps, System.currentTimeMillis()).daysPlayed
    assertTrue("la semana de la prueba tiene al menos un día jugado ($expected)", expected >= 1)
    assertTrue("sin ninguna partida de siempre", vm.gameHistory.value.isEmpty())

    val caption = Week.caption(expected, 4)
    goTo(vm, AppTab.HOY)
    assertTrue("Hoy dice «$caption» (dice «${textOf("home_date")}»)", textOf("home_date").endsWith(caption))
    goTo(vm, AppTab.PROGRESO)
    assertEquals("Avance dice lo mismo que Hoy", caption, textOf("avance_week_line"))
    assertEquals("y las llamitas de Avance cuentan los mismos días", Week.lampsSpoken(expected), describe("avance_lamps"))
    assertFalse("no es la semana vacía de quien no jugó nada", textOf("avance_week_line").contains("empieza hoy"))
  }

  @Test
  fun quien_no_jugo_nada_ve_la_semana_vacia_en_Avance() {
    val vm = viewModel()
    show(vm)
    goTo(vm, AppTab.PROGRESO)
    assertEquals("Tu semana empieza hoy", textOf("avance_week_line"))
  }
}
