package com.example

import androidx.compose.foundation.layout.Box
import androidx.compose.foundation.layout.height
import androidx.compose.foundation.layout.padding
import androidx.compose.foundation.layout.width
import androidx.compose.ui.Modifier
import androidx.compose.ui.test.junit4.createComposeRule
import androidx.compose.ui.test.onRoot
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
import org.junit.Assume
import org.junit.Before
import org.junit.Rule
import org.junit.Test
import org.junit.runner.RunWith
import org.robolectric.RobolectricTestRunner
import org.robolectric.annotation.Config
import org.robolectric.annotation.GraphicsMode
import java.util.Calendar

/**
 * La captura de la lámina de la Tarea 184: Hoy a las 23:00, antes (Nubi dormida de 20 a 6, regla de la Tarea 165) y después (Nubi siempre despierta). Solo corre al grabar capturas; la carpeta sale de la variable de entorno `T184_OUT`
 * («antes», que se grabó con el código de antes de la tarea, o «despues»).
 */
@RunWith(RobolectricTestRunner::class)
@GraphicsMode(GraphicsMode.Mode.NATIVE)
@Config(qualifiers = RobolectricDeviceQualifiers.Pixel8, sdk = [36])
class Tarea184CapturasTest {
  @get:Rule val rule = createComposeRule()

  @Before fun solo_al_grabar() { Assume.assumeTrue("solo al grabar capturas", System.getProperty("roborazzi.test.record") == "true") }

  private val out = "src/test/screenshots/tarea-184/" + (System.getenv("T184_OUT") ?: "despues")
  private val night = Calendar.getInstance().apply { set(2026, Calendar.OCTOBER, 11, 23, 0, 0) }.timeInMillis

  @Test
  fun hoy_a_las_23_horas() {
    val ui = HomeUi(
      name = "Ricardo", now = night, tier = RankTier.fromRating(0), streak = 3, weeklyGoal = 4,
      lamps = Week.Lamps(listOf(true, true, false, false, false, false, true), todayIndex = 6),
      stops = homeStops(listOf("bodega", "calculo", "intrusa"), 1, AppLanguage.SPANISH), pausedTitle = null, flightDay = false, hasBaseline = true,
      timeTrial = HomeTimeTrial("stroop", "Tinta o Palabra", TimeTrialState.PENDIENTE, todayHits = null, record = 41)
    )
    rule.setContent {
      NeuroVidaTheme {
        Box(Modifier.width(393.dp).height(873.dp)) {
          BoardBackground()
          HomeContent(ui = ui, onAllGames = {}, onPlay = {}, onResume = {}, onBaseline = {}, onTimeTrial = {}, modifier = Modifier.padding(top = 24.dp, bottom = 163.dp))
        }
      }
    }
    rule.waitForIdle()
    rule.onRoot().captureRoboImage(filePath = "$out/hoy-23h.png")
  }
}
