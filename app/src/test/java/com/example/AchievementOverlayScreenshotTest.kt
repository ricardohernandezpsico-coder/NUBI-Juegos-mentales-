package com.example

import androidx.compose.foundation.layout.Box
import androidx.compose.foundation.layout.fillMaxSize
import androidx.compose.runtime.CompositionLocalProvider
import androidx.compose.ui.Modifier
import androidx.compose.ui.test.assertIsDisplayed
import androidx.compose.ui.test.hasContentDescription
import androidx.compose.ui.test.hasTestTag
import androidx.compose.ui.test.hasText
import androidx.compose.ui.test.junit4.createComposeRule
import androidx.compose.ui.test.onNodeWithTag
import androidx.compose.ui.test.onNodeWithText
import androidx.compose.ui.test.onRoot
import com.example.data.Achievements
import com.example.ui.components.AchievementOverlay
import com.example.ui.components.BoardBackground
import com.example.ui.components.LocalReduceMotion
import com.example.ui.theme.NeuroVidaTheme
import com.github.takahirom.roborazzi.captureRoboImage
import org.junit.Assert.assertEquals
import org.junit.Rule
import org.junit.Test
import org.junit.runner.RunWith
import org.robolectric.RobolectricTestRunner
import org.robolectric.annotation.Config
import org.robolectric.annotation.GraphicsMode

/**
 * La celebración de una medalla SIN la línea de la isla (Tarea 185, 28-oct): hasta la Tarea 184 una medalla que daba una pieza de la isla sumaba «Y en tu isla aparece: …» con la pieza chica y un sonido; la isla salió de la app (Ricardo: arriba de Tú va SOLO Nubi) y la
 * celebración queda igual para todas las medallas: la medalla, su nombre, qué se hizo, «N de 15 medallas» y los dos botones. Capturas a 393 × 873 con «quitar animaciones» (todo en su estado final).
 */
@RunWith(RobolectricTestRunner::class)
@GraphicsMode(GraphicsMode.Mode.NATIVE)
@Config(qualifiers = "w393dp-h873dp-xxhdpi", sdk = [36])
class AchievementOverlayScreenshotTest {
  @get:Rule val rule = createComposeRule()

  private fun show(id: String, soundEnabled: Boolean = true) {
    val def = Achievements.byId(id) ?: throw AssertionError("no hay un logro «$id»")
    rule.setContent {
      CompositionLocalProvider(LocalReduceMotion provides true) {
        NeuroVidaTheme {
          Box(Modifier.fillMaxSize()) {
            BoardBackground()
            AchievementOverlay(def = def, unlockedCount = 6, totalCount = Achievements.all.size, streak = 7, onDismiss = {}, delayMs = 0L, soundEnabled = soundEnabled)
          }
        }
      }
    }
    rule.waitForIdle()
  }

  private fun nothingAboutTheIsland() {
    assertEquals("ya no hay línea de pieza", 0, rule.onAllNodes(hasTestTag("achievement_piece")).fetchSemanticsNodes().size)
    assertEquals("ningún texto dice «isla»", 0, rule.onAllNodes(hasText("isla", substring = true, ignoreCase = true)).fetchSemanticsNodes().size)
    assertEquals("ni ninguna descripción", 0, rule.onAllNodes(hasContentDescription("isla", substring = true, ignoreCase = true), useUnmergedTree = true).fetchSemanticsNodes().size)
  }

  @Test
  fun una_medalla_que_antes_traia_pieza_de_la_isla_ya_no_habla_de_ella() {
    show("racha_7")                                                  // antes: «Y en tu isla aparece: dos arbustos»
    rule.onNodeWithText("Medalla nueva").assertIsDisplayed()
    nothingAboutTheIsland()
    rule.onNodeWithTag("btn_achievement_ok").assertIsDisplayed()
    rule.onRoot().captureRoboImage(filePath = "src/test/screenshots/logro-sin-pieza-racha.png")
  }

  @Test
  fun una_medalla_sin_pieza_queda_como_era() {
    show("primer_paso")
    rule.onNodeWithText("Medalla nueva").assertIsDisplayed()
    nothingAboutTheIsland()
    rule.onRoot().captureRoboImage(filePath = "src/test/screenshots/logro-sin-pieza.png")
  }

  @Test
  fun el_nombre_mas_largo_cabe_y_los_botones_siguen_a_la_vista() {
    show("dominios")                                                 // antes: la línea más larga de la isla, «banderines entre la Antena y la Biblioteca»
    nothingAboutTheIsland()
    rule.onNodeWithTag("btn_achievement_ok").assertIsDisplayed()
    rule.onRoot().captureRoboImage(filePath = "src/test/screenshots/logro-con-pieza-larga.png")
  }
}
