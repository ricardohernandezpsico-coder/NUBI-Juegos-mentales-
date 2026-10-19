package com.example

import androidx.compose.foundation.layout.Box
import androidx.compose.foundation.layout.fillMaxSize
import androidx.compose.runtime.CompositionLocalProvider
import androidx.compose.ui.Modifier
import androidx.compose.ui.test.assertIsDisplayed
import androidx.compose.ui.test.hasTestTag
import androidx.compose.ui.test.junit4.createComposeRule
import androidx.compose.ui.test.onNodeWithContentDescription
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
import org.junit.Assert.assertTrue
import org.junit.Rule
import org.junit.Test
import org.junit.runner.RunWith
import org.robolectric.RobolectricTestRunner
import org.robolectric.annotation.Config
import org.robolectric.annotation.GraphicsMode

/**
 * La celebración de una medalla con la pieza de la isla (Tarea 136, «Tu isla crece»): UNA línea, «Y en tu isla aparece: {nombre}», con la pieza chica al lado y el `New` de la paleta Madera, una sola vez y solo con «Efectos de sonido» prendido;
 * una medalla sin pieza queda como era (sin la línea y sin sonido). Capturas a 393 × 873 con «quitar animaciones» (todo en su estado final).
 */
@RunWith(RobolectricTestRunner::class)
@GraphicsMode(GraphicsMode.Mode.NATIVE)
@Config(qualifiers = "w393dp-h873dp-xxhdpi", sdk = [36])
class AchievementOverlayScreenshotTest {
  @get:Rule val rule = createComposeRule()

  private var sounds = 0

  private fun show(id: String, soundEnabled: Boolean = true) {
    val def = Achievements.byId(id) ?: throw AssertionError("no hay un logro «$id»")
    rule.setContent {
      CompositionLocalProvider(LocalReduceMotion provides true) {
        NeuroVidaTheme {
          Box(Modifier.fillMaxSize()) {
            BoardBackground()
            AchievementOverlay(def = def, unlockedCount = 6, totalCount = Achievements.all.size, streak = 7, onDismiss = {}, delayMs = 0L, soundEnabled = soundEnabled, pieceSound = { sounds++ })
          }
        }
      }
    }
    rule.waitForIdle()
  }

  private fun lineExists() = rule.onAllNodes(hasTestTag("achievement_piece")).fetchSemanticsNodes().isNotEmpty()

  @Test
  fun una_medalla_con_pieza_dice_que_aparece_en_la_isla_y_suena_una_vez() {
    show("racha_7")
    rule.onNodeWithText("Y en tu isla aparece: dos arbustos").assertIsDisplayed()
    rule.onNodeWithContentDescription("Y en tu isla aparece: dos arbustos").assertExists()
    assertEquals("el New de la paleta suena UNA vez, con la línea", 1, sounds)
    rule.onRoot().captureRoboImage(filePath = "src/test/screenshots/logro-con-pieza.png")
  }

  @Test
  fun una_medalla_sin_pieza_queda_como_era_sin_la_linea_y_sin_sonido() {
    show("primer_paso")
    assertTrue(!lineExists())
    assertEquals(0, sounds)
    rule.onRoot().captureRoboImage(filePath = "src/test/screenshots/logro-sin-pieza.png")
  }

  @Test
  fun con_los_efectos_de_sonido_apagados_la_linea_sale_pero_no_suena() {
    show("racha_7", soundEnabled = false)
    rule.onNodeWithText("Y en tu isla aparece: dos arbustos").assertIsDisplayed()
    assertEquals(0, sounds)
  }

  @Test
  fun el_nombre_mas_largo_cabe_en_el_ancho_y_en_la_pantalla() {
    show("dominios")
    rule.onNodeWithText("Y en tu isla aparece: banderines entre la Antena y la Biblioteca").assertExists()
    val line = rule.onNodeWithTag("achievement_piece").fetchSemanticsNode().boundsInRoot
    val root = rule.onRoot().fetchSemanticsNode().boundsInRoot
    assertTrue("la línea se sale del ancho: $line (pantalla $root)", line.left >= 0f && line.right <= root.right)
    rule.onNodeWithTag("btn_achievement_ok").assertIsDisplayed()
    rule.onRoot().captureRoboImage(filePath = "src/test/screenshots/logro-con-pieza-larga.png")
  }
}
