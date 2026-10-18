package com.example

import android.app.Application
import androidx.compose.foundation.layout.Box
import androidx.compose.foundation.layout.height
import androidx.compose.foundation.layout.padding
import androidx.compose.foundation.layout.width
import androidx.compose.runtime.CompositionLocalProvider
import androidx.compose.ui.Modifier
import androidx.compose.ui.platform.LocalDensity
import androidx.compose.ui.platform.testTag
import androidx.compose.ui.test.junit4.createComposeRule
import androidx.compose.ui.test.onAllNodesWithTag
import androidx.compose.ui.test.onNodeWithTag
import androidx.compose.ui.test.performScrollTo
import androidx.compose.ui.unit.Density
import androidx.compose.ui.unit.dp
import androidx.test.core.app.ApplicationProvider
import com.example.data.local.NeuroVidaDatabase
import com.example.data.local.toEntity
import com.example.model.AgeBand
import com.example.model.UserSettings
import com.example.ui.components.BoardBackground
import com.example.ui.components.LocalReduceMotion
import com.example.ui.screens.SettingsPanel
import com.example.ui.theme.NeuroVidaTheme
import com.example.viewmodel.NeuroVidaViewModel
import com.github.takahirom.roborazzi.captureRoboImage
import kotlinx.coroutines.runBlocking
import org.junit.After
import org.junit.Assume
import org.junit.Before
import org.junit.Rule
import org.junit.Test
import org.junit.runner.RunWith
import org.robolectric.RobolectricTestRunner
import org.robolectric.annotation.Config
import org.robolectric.annotation.GraphicsMode

/**
 * La hoja «hoy | propuesta» de la meta semanal de Ajustes (Tarea 130, `docs/previews/ajustes-meta.png`): el panel «Meta semanal de juego» REAL a 393 × 873 dp, con la letra del sistema en 1,0 y en 1,3 (la que usan muchas personas mayores).
 * No corre en las pruebas de siempre: solo al grabar capturas (`./gradlew.bat :app:recordRoborazziDebug --tests "*AjustesMetaScreenshotTest*"`), y deja los PNG en `src/test/screenshots/ajustes-meta/`. El prefijo del nombre sale de la variable
 * de entorno AJUSTES_META_PREFIX (`hoy` con el slider de antes, `prop` con las farolas; por defecto `prop`); `tools/previews/ajustes_meta.py` arma la hoja.
 */
@RunWith(RobolectricTestRunner::class)
@GraphicsMode(GraphicsMode.Mode.NATIVE)
@Config(qualifiers = "w393dp-h873dp-440dpi", sdk = [36])
class AjustesMetaScreenshotTest {
  @get:Rule val composeTestRule = createComposeRule()

  private lateinit var app: Application
  private val viewModels = mutableListOf<NeuroVidaViewModel>()

  @Before
  fun setUp() {
    Assume.assumeTrue("solo al grabar capturas", System.getProperty("roborazzi.test.record") == "true")
    app = ApplicationProvider.getApplicationContext()
    TestSupport.resetDatabase()
  }

  @After
  fun tearDown() {
    TestSupport.release(*viewModels.toTypedArray())
    viewModels.clear()
    TestSupport.resetDatabase()
  }

  private fun shot(name: String, fontScale: Float) {
    runBlocking { NeuroVidaDatabase.getDatabase(app).userProfileDao().insertOrUpdate(UserSettings(ageBand = AgeBand.ADULT, name = "Ricardo", weeklyGoal = 4).toEntity()) }
    val vm = NeuroVidaViewModel(app).also { viewModels.add(it) }
    val prefix = System.getenv("AJUSTES_META_PREFIX") ?: "prop"
    composeTestRule.mainClock.autoAdvance = false
    composeTestRule.setContent {
      val base = LocalDensity.current
      CompositionLocalProvider(LocalDensity provides Density(base.density, fontScale), LocalReduceMotion provides true) {
        NeuroVidaTheme {
          Box(Modifier.width(393.dp).height(873.dp).testTag("frame")) {
            BoardBackground()
            Box(Modifier.padding(top = 24.dp)) { SettingsPanel(viewModel = vm, onClose = {}) }
          }
        }
      }
    }
    repeat(3) { composeTestRule.mainClock.advanceTimeBy(600); composeTestRule.waitForIdle() }
    // el panel de la meta queda a la vista (la pantalla de Ajustes se desliza)
    val tag = if (composeTestRule.onAllNodesWithTag("goal_lamps").fetchSemanticsNodes().isEmpty()) "slider_weekly_goal" else "goal_lamps"
    composeTestRule.onNodeWithTag(tag).performScrollTo()
    repeat(2) { composeTestRule.mainClock.advanceTimeBy(600); composeTestRule.waitForIdle() }
    composeTestRule.onNodeWithTag("frame").captureRoboImage(filePath = "src/test/screenshots/ajustes-meta/$prefix-$name.png")
  }

  @Test fun a_letra_1_0() = shot("393-1_0", 1f)

  @Test fun b_letra_1_3() = shot("393-1_3", 1.3f)
}
