package com.example

import androidx.compose.foundation.layout.Box
import androidx.compose.foundation.layout.height
import androidx.compose.foundation.layout.padding
import androidx.compose.foundation.layout.width
import androidx.compose.runtime.CompositionLocalProvider
import androidx.compose.ui.Modifier
import androidx.compose.ui.platform.LocalDensity
import androidx.compose.ui.test.junit4.createComposeRule
import androidx.compose.ui.test.onRoot
import androidx.compose.ui.unit.Density
import androidx.compose.ui.unit.dp
import com.example.data.TimeTrialState
import com.example.data.Week
import com.example.model.AppLanguage
import com.example.model.RankTier
import com.example.ui.components.BoardBackground
import com.example.ui.screens.HomeContent
import com.example.ui.screens.HomeTimeTrial
import com.example.ui.screens.HomeUi
import com.example.ui.screens.homeStops
import com.example.ui.theme.NeuroVidaTheme
import com.github.takahirom.roborazzi.RobolectricDeviceQualifiers
import com.github.takahirom.roborazzi.captureRoboImage
import org.junit.Rule
import org.junit.Test
import org.junit.runner.RunWith
import org.robolectric.RobolectricTestRunner
import org.robolectric.annotation.Config
import org.robolectric.annotation.GraphicsMode
import java.util.Calendar

/**
 * Las capturas de la lámina de la Tarea 180 («hoy | propuesta»): Hoy en sus estados del día, a 393 × 873 con letra 1,0, 1,3 y 2,0 (solo al grabar). Este archivo se escribió para correr IGUAL con la disposición de antes y con la nueva (solo usa lo que las dos tienen): la carpeta sale de la variable de entorno `T180_OUT`
 * («hoy» o «propuesta»; por omisión `src/test/screenshots/tarea-180/propuesta`).
 */
@RunWith(RobolectricTestRunner::class)
@GraphicsMode(GraphicsMode.Mode.NATIVE)
@Config(qualifiers = RobolectricDeviceQualifiers.Pixel8, sdk = [36])
class Tarea180CapturasTest {
  @get:Rule val composeTestRule = createComposeRule()

  private val now = Calendar.getInstance().apply { set(2026, Calendar.OCTOBER, 11, 16, 30, 0) }.timeInMillis
  private val lamps = Week.Lamps(listOf(true, true, false, false, false, false, true), todayIndex = 6)
  private val stroop = HomeTimeTrial("stroop", "Tinta o Palabra", TimeTrialState.PENDIENTE, todayHits = null, record = 41)

  private fun ui(completed: Int = 1, paused: String? = null, tt: HomeTimeTrial = stroop) = HomeUi(
    name = "Ricardo", now = now, tier = RankTier.fromRating(0), streak = 3, weeklyGoal = 4, lamps = lamps,
    stops = homeStops(listOf("bodega", "calculo", "intrusa"), completed, AppLanguage.SPANISH), pausedTitle = paused, flightDay = false, hasBaseline = true, timeTrial = tt
  )

  private var holder: androidx.compose.runtime.MutableState<Pair<HomeUi, Float>>? = null

  private fun show(u: HomeUi, fs: Float) {
    val st = holder
    if (st != null) { st.value = u to fs; composeTestRule.waitForIdle(); return }
    val h = androidx.compose.runtime.mutableStateOf(u to fs).also { holder = it }
    composeTestRule.setContent {
      val base = LocalDensity.current
      CompositionLocalProvider(LocalDensity provides Density(base.density, h.value.second)) {
        NeuroVidaTheme {
          Box(Modifier.width(393.dp).height(873.dp)) {
            BoardBackground()
            HomeContent(ui = h.value.first, onAllGames = {}, onPlay = {}, onResume = {}, onBaseline = {}, onTimeTrial = {}, modifier = Modifier.padding(top = 24.dp, bottom = 163.dp))
          }
        }
      }
    }
    composeTestRule.waitForIdle()
  }

  @Test
  fun tarea_180_capturas() {
    org.junit.Assume.assumeTrue("solo al grabar capturas", System.getProperty("roborazzi.test.record") == "true")
    val out = "src/test/screenshots/tarea-180/" + (System.getenv("T180_OUT") ?: "propuesta")
    val jugado = stroop.copy(state = TimeTrialState.JUGADO, todayHits = 38)
    val marca = stroop.copy(state = TimeTrialState.MARCA_NUEVA, todayHits = 43, record = 43)
    val states = listOf(
      "media-ruta" to ui(completed = 1),
      "ruta-completa" to ui(completed = 3),
      "jugado" to ui(completed = 3, tt = jugado),
      "marca-nueva" to ui(completed = 3, tt = marca),
      "pausa" to ui(completed = 1, paused = "Carga exacta")
    )
    for (fs in listOf(1f, 1.3f, 2f)) for ((name, u) in states) {
      show(u, fs)
      composeTestRule.onRoot().captureRoboImage(filePath = "$out/${name}-${fs.toString().replace('.', '_')}.png")
    }
  }
}
