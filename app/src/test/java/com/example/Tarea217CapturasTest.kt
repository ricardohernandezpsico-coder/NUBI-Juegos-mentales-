package com.example

import android.app.Application
import androidx.activity.ComponentActivity
import androidx.compose.foundation.background
import androidx.compose.foundation.layout.Box
import androidx.compose.foundation.layout.fillMaxSize
import androidx.compose.foundation.layout.height
import androidx.compose.foundation.layout.width
import androidx.compose.runtime.CompositionLocalProvider
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.graphics.Color
import androidx.compose.ui.platform.LocalDensity
import androidx.compose.ui.test.hasTestTag
import androidx.compose.ui.test.junit4.createAndroidComposeRule
import androidx.compose.ui.test.onNodeWithTag
import androidx.compose.ui.test.onRoot
import androidx.compose.ui.test.performScrollTo
import androidx.compose.ui.test.performScrollToNode
import androidx.compose.ui.unit.Density
import androidx.compose.ui.unit.dp
import androidx.test.core.app.ApplicationProvider
import com.example.data.Comparison
import com.example.ui.components.BoardBackground
import com.example.ui.components.LocalBarInset
import com.example.ui.components.LocalReduceMotion
import com.example.ui.screens.PlusLockSheetContent
import com.example.ui.screens.SettingsScreen
import com.example.ui.screens.TuScreen
import com.example.ui.theme.NeuroVidaTheme
import com.example.viewmodel.NeuroVidaViewModel
import com.github.takahirom.roborazzi.captureRoboImage
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
 * La lámina de la Tarea 217 (`docs/previews/tarea-217.png`, `tools/previews/tarea_217.py`): la app se llama «Nubilus». Tú con las secciones renombradas («En qué se basa Nubilus» y «Quién hace Nubilus»), la hoja del candado con «Nubilus Plus» y «Acerca de Nubilus» a 393 dp con letra 1,0 y a 360 dp con 2,0.
 * Lo del emulador (letra 2,0 a 393 y 360 dp: la barra, la pestaña, Tú y Más ajustes) son capturas del `medium_phone`. No corre en las pruebas de siempre: solo al grabar (`./gradlew.bat :app:recordRoborazziDebug --tests "*Tarea217CapturasTest*"`); los PNG van a `src/test/screenshots/t217/` (fuera de git).
 */
@RunWith(RobolectricTestRunner::class)
@GraphicsMode(GraphicsMode.Mode.NATIVE)
@Config(qualifiers = "w393dp-h873dp-xxhdpi", sdk = [36])
class Tarea217CapturasTest {
  @get:Rule val rule = createAndroidComposeRule<ComponentActivity>()

  private val viewModels = mutableListOf<NeuroVidaViewModel>()

  @Before fun setUp() {
    Assume.assumeTrue("solo al grabar capturas", System.getProperty("roborazzi.test.record") == "true")
    TestSupport.resetDatabase()
    TestSupport.fastFrames()
  }

  @After fun tearDown() {
    TestSupport.release(*viewModels.toTypedArray())
    viewModels.clear()
    TestSupport.resetDatabase()
  }

  private fun frame(widthDp: Int, heightDp: Int, scale: Float = 1f, content: @androidx.compose.runtime.Composable () -> Unit) {
    rule.mainClock.autoAdvance = true
    rule.setContent {
      val base = LocalDensity.current
      CompositionLocalProvider(LocalDensity provides Density(base.density, scale), LocalReduceMotion provides true, LocalBarInset provides 88.dp) {
        NeuroVidaTheme { Box(Modifier.width(widthDp.dp).height(heightDp.dp).background(Color(0xFF050823))) { BoardBackground(); content() } }
      }
    }
    rule.waitForIdle()
  }

  private fun save(name: String) { rule.onRoot().captureRoboImage(filePath = "src/test/screenshots/t217/$name.png") }

  private fun vm() = NeuroVidaViewModel(ApplicationProvider.getApplicationContext<Application>()).also { viewModels.add(it) }

  @Test fun tu_393_10() {
    val v = vm()
    frame(393, 873) { TuScreen(viewModel = v) }
    rule.onNodeWithTag("tu_lista").performScrollToNode(hasTestTag("tu_quien_hace")); rule.waitForIdle()
    save("tu_393_10")
  }

  @Test fun hoja_393_10() {
    frame(393, 620) { Box(Modifier.fillMaxSize(), contentAlignment = Alignment.Center) { PlusLockSheetContent(onClose = {}, kind = Comparison.Kind.ESTIMATED, onSource = {}) } }
    save("hoja_393_10")
  }

  private fun acerca(widthDp: Int, heightDp: Int, scale: Float, name: String) {
    val v = vm()
    frame(widthDp, heightDp, scale) { SettingsScreen(viewModel = v) }
    rule.onNodeWithTag("card_acerca").performScrollTo()
    rule.waitForIdle()
    save(name)
  }

  @Test fun acerca_393_10() = acerca(393, 873, 1f, "acerca_393_10")
  @Test fun acerca_360_20() = acerca(360, 780, 2f, "acerca_360_20")
}
