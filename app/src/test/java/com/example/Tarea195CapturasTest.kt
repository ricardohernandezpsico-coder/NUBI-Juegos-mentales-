package com.example

import android.app.Application
import androidx.compose.foundation.layout.Box
import androidx.compose.foundation.layout.fillMaxSize
import androidx.compose.foundation.layout.height
import androidx.compose.foundation.layout.padding
import androidx.compose.foundation.layout.width
import androidx.compose.runtime.CompositionLocalProvider
import androidx.compose.ui.Modifier
import androidx.compose.ui.platform.LocalDensity
import androidx.compose.ui.platform.testTag
import androidx.compose.ui.test.junit4.createComposeRule
import androidx.compose.ui.test.onNodeWithTag
import androidx.compose.ui.test.onNodeWithText
import androidx.compose.ui.test.onRoot
import androidx.compose.ui.test.performScrollTo
import androidx.compose.ui.unit.Density
import androidx.compose.ui.unit.dp
import androidx.test.core.app.ApplicationProvider
import com.example.data.FlightMode
import com.example.data.FlightState
import com.example.data.FlightStep
import com.example.data.local.NeuroVidaDatabase
import com.example.data.local.toEntity
import com.example.model.AgeBand
import com.example.model.UserSettings
import com.example.ui.components.BoardBackground
import com.example.ui.components.LocalReduceMotion
import com.example.ui.screens.FirstFlightScreen
import com.example.ui.screens.FlightActions
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
 * La lámina «hoy | propuesta» de la Tarea 195 (`docs/previews/tarea-195.png`): la pregunta de la edad del Primer vuelo con letra 1,0 y 1,3, y la sección «Rango de edad» de Ajustes con letra 1,0, a 393 × 873 dp. No corre en las pruebas de siempre: solo al grabar
 * (`./gradlew.bat :app:recordRoborazziDebug --tests "*Tarea195CapturasTest*"`). El prefijo (`hoy` = el código de antes, con las tres bandas; `prop` = el nuevo, con las siete franjas) sale de la variable de entorno T195_PREFIX; los PNG van a `src/test/screenshots/t195/` y `tools/previews/tarea_195.py` arma la hoja.
 * Escrita para compilar con el código de antes y con el de ahora (por eso no nombra `AgeRange`): la franja de Ajustes se deja guardada escribiendo `age_range` en `profile_extra`, que el código de antes ignora.
 */
@RunWith(RobolectricTestRunner::class)
@GraphicsMode(GraphicsMode.Mode.NATIVE)
@Config(qualifiers = "w393dp-h873dp-xxhdpi", sdk = [36])
class Tarea195CapturasTest {
  @get:Rule val composeTestRule = createComposeRule()

  private lateinit var app: Application
  private val viewModels = mutableListOf<NeuroVidaViewModel>()
  private val prefix = System.getenv("T195_PREFIX") ?: "prop"

  @Before fun setUp() {
    Assume.assumeTrue("solo al grabar capturas", System.getProperty("roborazzi.test.record") == "true")
    app = ApplicationProvider.getApplicationContext()
    TestSupport.resetDatabase()
    TestSupport.fastFrames()
  }

  @After fun tearDown() { TestSupport.release(*viewModels.toTypedArray()); viewModels.clear(); TestSupport.resetDatabase() }

  private fun edad(name: String, fontScale: Float) {
    composeTestRule.setContent {
      val base = LocalDensity.current
      CompositionLocalProvider(LocalDensity provides Density(base.density, fontScale), LocalReduceMotion provides true) {
        NeuroVidaTheme {
          Box(Modifier.fillMaxSize()) {
            BoardBackground()
            FirstFlightScreen(FlightState(FlightMode.FULL, FlightStep.EDAD, name = "Ana"), emptyList(), FlightActions.None)
          }
        }
      }
    }
    composeTestRule.waitForIdle()
    composeTestRule.onRoot().captureRoboImage(filePath = "src/test/screenshots/t195/${prefix}_$name.png")
  }

  @Test fun edad_10() = edad("edad_10", 1f)
  @Test fun edad_13() = edad("edad_13", 1.3f)

  @Test fun ajustes_10() {
    // una persona adulta que ya eligió su rango: antes, «18 a 64» (la banda); ahora, «30 a 44 años» (la franja fina, escrita en profile_extra)
    runBlocking { NeuroVidaDatabase.getDatabase(app).userProfileDao().insertOrUpdate(UserSettings(ageBand = AgeBand.ADULT, name = "Ana", weeklyGoal = 4).toEntity()) }
    app.getSharedPreferences("profile_extra", android.content.Context.MODE_PRIVATE).edit().putString("age_range", "R30_44").commit()
    val vm = NeuroVidaViewModel(app).also { viewModels.add(it) }
    composeTestRule.mainClock.autoAdvance = false
    composeTestRule.setContent {
      val base = LocalDensity.current
      CompositionLocalProvider(LocalDensity provides Density(base.density, 1f), LocalReduceMotion provides true) {
        NeuroVidaTheme {
          Box(Modifier.width(393.dp).height(873.dp).testTag("frame")) {
            BoardBackground()
            Box(Modifier.padding(top = 24.dp)) { SettingsPanel(viewModel = vm, onClose = {}) }
          }
        }
      }
    }
    repeat(3) { composeTestRule.mainClock.advanceTimeBy(600); composeTestRule.waitForIdle() }
    composeTestRule.onNodeWithText("Rango de edad").performScrollTo()
    repeat(2) { composeTestRule.mainClock.advanceTimeBy(600); composeTestRule.waitForIdle() }
    composeTestRule.onNodeWithTag("frame").captureRoboImage(filePath = "src/test/screenshots/t195/${prefix}_ajustes_10.png")
  }
}
