package com.example

import androidx.compose.foundation.layout.Box
import androidx.compose.foundation.layout.height
import androidx.compose.foundation.layout.padding
import androidx.compose.foundation.layout.width
import androidx.compose.runtime.CompositionLocalProvider
import androidx.compose.runtime.mutableIntStateOf
import androidx.compose.runtime.remember
import androidx.compose.ui.Modifier
import androidx.compose.ui.platform.LocalDensity
import androidx.compose.ui.platform.testTag
import androidx.compose.ui.test.junit4.createAndroidComposeRule
import androidx.activity.ComponentActivity
import androidx.compose.ui.test.onNodeWithTag
import androidx.compose.ui.test.performClick
import androidx.compose.ui.unit.Density
import androidx.compose.ui.unit.dp
import com.example.data.Week
import com.example.model.AppLanguage
import com.example.model.RankTier
import com.example.ui.components.BoardBackground
import com.example.ui.components.ClayKitFlag
import com.example.ui.components.LocalTabSwipeLock
import com.example.ui.screens.HomeContent
import com.example.ui.screens.HomeUi
import com.example.ui.screens.PlaceFx
import com.example.ui.screens.homeStops
import com.example.ui.theme.NeuroVidaTheme
import com.github.takahirom.roborazzi.captureRoboImage
import org.junit.After
import org.junit.Rule
import org.junit.Test
import org.junit.runner.RunWith
import org.robolectric.RobolectricTestRunner
import org.robolectric.annotation.Config
import org.robolectric.annotation.GraphicsMode
import java.util.Calendar

/**
 * Hoy y la lámina con el kit de plasticina APAGADO y ENCENDIDO (Tarea 89), a 393 y 360 dp y con letra 2,0, para la hoja de comparación `t89-antes-despues.png`. Deja `kit-<hoy|lamina>-<on|off>-<393|360|letra20>.png` en `src/test/screenshots/` (git-ignorada).
 */
@RunWith(RobolectricTestRunner::class)
@GraphicsMode(GraphicsMode.Mode.NATIVE)
@Config(qualifiers = "w411dp-h914dp-xxhdpi", sdk = [36])
class KitComparisonScreenshotTest {
  @get:Rule val rule = createAndroidComposeRule<ComponentActivity>()

  private val now = Calendar.getInstance().apply { set(2026, Calendar.OCTOBER, 13, 16, 30, 0) }.timeInMillis
  private val lamps = Week.Lamps(listOf(true, true, false, false, false, false, true), todayIndex = 6)
  private val ids = listOf("bodega", "calculo", "intrusa")          // «Bodega de carga», «Carga exacta», «La estrella intrusa»: la ruta de ejemplo (caben en piedritas)
  private val fx = PlaceFx()

  @After fun tearDown() { ClayKitFlag.forTest(false) }

  private fun ui() = HomeUi(
    name = "Ana", now = now, tier = RankTier.fromRating(0), streak = 3, weeklyGoal = 4, lamps = lamps,
    stops = homeStops(ids, 1, AppLanguage.SPANISH), pausedTitle = null, flightDay = false, hasBaseline = false,
    places = PlaceTestData.places(now)
  )

  private fun show(on: Boolean, widthDp: Int, heightDp: Int, fontScale: Float) {
    ClayKitFlag.forTest(on)
    rule.setContent {
      val base = LocalDensity.current
      CompositionLocalProvider(LocalDensity provides Density(base.density, fontScale), LocalTabSwipeLock provides remember { mutableIntStateOf(0) }) {
        NeuroVidaTheme {
          Box(Modifier.width(widthDp.dp).height(heightDp.dp).testTag("kit_root")) {
            BoardBackground()
            HomeContent(
              ui = ui(), onProfile = {}, onSettings = {}, onPlaceGame = {}, onPlaceGameCard = {}, onStop = {}, onPlay = {}, onResume = {}, onBaseline = {}, fx = fx,
              modifier = Modifier.padding(top = 24.dp, bottom = 163.dp)
            )
          }
        }
      }
    }
    rule.waitForIdle()
  }

  private fun case(name: String, on: Boolean, w: Int, h: Int, font: Float, lamina: Boolean) {
    show(on, w, h, font)
    if (lamina) { rule.onNodeWithTag("isla_observatorio").performClick(); rule.waitForIdle() }
    rule.onNodeWithTag("kit_root").captureRoboImage(filePath = "src/test/screenshots/kit-${if (lamina) "lamina" else "hoy"}-${if (on) "on" else "off"}-$name.png")
  }

  @Test fun hoy_off_393() = case("393", false, 393, 873, 1f, false)
  @Test fun hoy_on_393() = case("393", true, 393, 873, 1f, false)
  @Test fun hoy_off_360() = case("360", false, 360, 780, 1f, false)
  @Test fun hoy_on_360() = case("360", true, 360, 780, 1f, false)
  @Test fun hoy_off_letra20() = case("letra20", false, 393, 873, 2f, false)
  @Test fun hoy_on_letra20() = case("letra20", true, 393, 873, 2f, false)
  @Test fun lamina_off_393() = case("393", false, 393, 873, 1f, true)
  @Test fun lamina_on_393() = case("393", true, 393, 873, 1f, true)
  @Test fun lamina_off_360() = case("360", false, 360, 780, 1f, true)
  @Test fun lamina_on_360() = case("360", true, 360, 780, 1f, true)
  @Test fun lamina_off_letra20() = case("letra20", false, 393, 873, 2f, true)
  @Test fun lamina_on_letra20() = case("letra20", true, 393, 873, 2f, true)
}
