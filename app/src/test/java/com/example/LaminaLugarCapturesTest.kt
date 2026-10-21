package com.example

import androidx.activity.ComponentActivity
import androidx.compose.foundation.layout.Box
import androidx.compose.foundation.layout.height
import androidx.compose.foundation.layout.padding
import androidx.compose.foundation.layout.width
import androidx.compose.runtime.CompositionLocalProvider
import androidx.compose.runtime.mutableIntStateOf
import androidx.compose.ui.Modifier
import androidx.compose.ui.platform.LocalDensity
import androidx.compose.ui.semantics.SemanticsProperties
import androidx.compose.ui.semantics.getOrNull
import androidx.compose.ui.test.hasTestTag
import androidx.compose.ui.test.junit4.createAndroidComposeRule
import androidx.compose.ui.test.onNodeWithTag
import androidx.compose.ui.test.onRoot
import androidx.compose.ui.test.performClick
import androidx.compose.ui.unit.Density
import androidx.compose.ui.unit.dp
import com.example.data.Week
import com.example.model.AppLanguage
import com.example.model.RankTier
import com.example.ui.components.BoardBackground
import com.example.ui.components.LocalTabSwipeLock
import com.example.ui.screens.HomeContent
import com.example.ui.screens.HomeUi
import com.example.ui.screens.PlaceFx
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
import java.io.File
import java.util.Calendar

/**
 * Las capturas REALES de la lámina del lugar para la hoja «hoy | propuesta» de la Tarea 147 (`tools/previews/lamina_lugar.py`): el Observatorio (4 juegos) y la Antena (5), en 393 × 873 y 360 × 780, con letra 1,0 y 1,3 →
 * `src/test/screenshots/lamina-lugar/lamina-<lugar>-<ancho>x<alto>-<letra>.png`, y en `medidas.txt` lo que mide cada una (arriba y alto de la lámina en dp, y cuánto hay que deslizar para ver todo: 0 = cabe sin deslizar).
 * La misma prueba corre con el código de HOY (un `git worktree` del commit anterior) y con el de ahora: usa solo la API que no cambió (`HomeContent`). Solo corre al grabar capturas (`tools/previews/lamina_lugar_capturas.sh`); en las pruebas de siempre se salta.
 */
@RunWith(RobolectricTestRunner::class)
@GraphicsMode(GraphicsMode.Mode.NATIVE)
@Config(qualifiers = RobolectricDeviceQualifiers.Pixel8, sdk = [36])
class LaminaLugarCapturesTest {
  @get:Rule val rule = createAndroidComposeRule<ComponentActivity>()

  private val now = Calendar.getInstance().apply { set(2026, Calendar.OCTOBER, 12, 10, 0, 0) }.timeInMillis
  private val ui = HomeUi(
    name = "Ricardo", now = now, tier = RankTier.fromRating(0), streak = 0, weeklyGoal = 4, lamps = Week.Lamps(listOf(false, false, false, false, false, false, false), todayIndex = 0),
    stops = homeStops(listOf("bodega", "calculo", "intrusa"), 0, AppLanguage.SPANISH), pausedTitle = null, flightDay = false, hasBaseline = true, places = PlaceTestData.places(now)
  )
  private val fx = PlaceFx()
  private val lock = mutableIntStateOf(0)

  @Before fun soloAlGrabar() = Assume.assumeTrue("solo al grabar capturas", System.getProperty("roborazzi.test.record") == "true")

  private fun capture(place: String, widthDp: Int, heightDp: Int, fontScale: Float, letter: String) {
    rule.setContent {
      val base = LocalDensity.current
      CompositionLocalProvider(LocalDensity provides Density(base.density, fontScale), LocalTabSwipeLock provides lock) {
        NeuroVidaTheme {
          Box(Modifier.width(widthDp.dp).height(heightDp.dp)) {
            BoardBackground()
            HomeContent(
              ui = ui, onProfile = {}, onSettings = {}, onPlaceGame = {}, onPlaceGameCard = {}, onStop = {}, onPlay = {}, onResume = {}, onBaseline = {}, fx = fx,
              modifier = Modifier.padding(top = 24.dp, bottom = 48.dp)
            )
          }
        }
      }
    }
    rule.waitForIdle()
    rule.onNodeWithTag("isla_$place").performClick()
    rule.waitForIdle()
    val d = rule.density.density
    val sheet = rule.onNodeWithTag("place_sheet").fetchSemanticsNode()
    // cuánto hay que deslizar para ver todo (lo que hacía la lámina de hoy): el rango del desplazamiento de `place_scroll`
    val scrollNodes = rule.onAllNodes(hasTestTag("place_scroll")).fetchSemanticsNodes()
    val slackDp = scrollNodes.firstOrNull()?.config?.getOrNull(SemanticsProperties.VerticalScrollAxisRange)?.let { it.maxValue() / d } ?: 0f
    val name = "lamina-$place-${widthDp}x$heightDp-$letter"
    File("src/test/screenshots/lamina-lugar").mkdirs()
    rule.onRoot().captureRoboImage(filePath = "src/test/screenshots/lamina-lugar/$name.png")
    File("src/test/screenshots/lamina-lugar/medidas.txt").appendText(String.format(java.util.Locale.US, "%s|%.0f|%.0f|%.0f\n", name, sheet.positionInRoot.y / d, sheet.size.height / d, slackDp))
  }

  @Test fun observatorio_393_1_0() = capture("observatorio", 393, 873, 1f, "1.0")
  @Test fun observatorio_393_1_3() = capture("observatorio", 393, 873, 1.3f, "1.3")
  @Test fun observatorio_360_1_0() = capture("observatorio", 360, 780, 1f, "1.0")
  @Test fun observatorio_360_1_3() = capture("observatorio", 360, 780, 1.3f, "1.3")
  @Test fun antena_393_1_0() = capture("antena", 393, 873, 1f, "1.0")
  @Test fun antena_393_1_3() = capture("antena", 393, 873, 1.3f, "1.3")
  @Test fun antena_360_1_0() = capture("antena", 360, 780, 1f, "1.0")
  @Test fun antena_360_1_3() = capture("antena", 360, 780, 1.3f, "1.3")
}
