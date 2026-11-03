package com.example

import androidx.activity.ComponentActivity
import androidx.compose.foundation.background
import androidx.compose.foundation.layout.Box
import androidx.compose.foundation.layout.Column
import androidx.compose.foundation.layout.fillMaxSize
import androidx.compose.foundation.layout.height
import androidx.compose.foundation.layout.padding
import androidx.compose.foundation.layout.width
import androidx.compose.runtime.CompositionLocalProvider
import androidx.compose.runtime.getValue
import androidx.compose.runtime.mutableStateOf
import androidx.compose.runtime.setValue
import androidx.compose.ui.Modifier
import androidx.compose.ui.graphics.Color
import androidx.compose.ui.platform.LocalDensity
import androidx.compose.ui.semantics.SemanticsActions
import androidx.compose.ui.test.junit4.createAndroidComposeRule
import androidx.compose.ui.test.onNodeWithTag
import androidx.compose.ui.test.onRoot
import androidx.compose.ui.test.performSemanticsAction
import androidx.compose.ui.test.performScrollTo
import androidx.compose.ui.unit.Density
import androidx.compose.ui.unit.dp
import com.example.data.Comparison
import com.example.data.EstimatedReference
import com.example.data.PlusAccess
import com.example.data.Preguntas
import com.example.data.TimeTrialDay
import com.example.data.TimeTrialResult
import com.example.model.AgeRange
import com.example.ui.components.BoardBackground
import com.example.ui.components.LocalReduceMotion
import com.example.ui.screens.ContrarrelojPage
import com.example.ui.screens.PanelHeader
import com.example.ui.screens.PlusLockSheetContent
import com.example.ui.screens.PreguntasScreen
import com.example.ui.screens.ReferenciaPanel
import com.example.ui.theme.NeuroVidaTheme
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
 * La lámina de la Tarea 209 (`docs/previews/tarea-209.png`, `tools/previews/tarea_209.py`): la comparación con su frase nueva y el enlace «De dónde sale la referencia ›», la puerta de Avance con la frase gratis y el candado largo (1,0 y 2,0), la hoja del candado con lo gratis y lo de Plus (1,0 y 2,0; el CONTENIDO, sin `Dialog`,
 * que ignora la densidad de la prueba), la página de 13 a 17 años con su línea, «Preguntas frecuentes» y «De dónde sale la referencia». Tú y todo lo que va a 2,0 en la ventana real salen del EMULADOR. No corre en las pruebas de siempre: solo al grabar
 * (`./gradlew.bat :app:recordRoborazziDebug --tests "*Tarea209CapturasTest*"`); los PNG van a `src/test/screenshots/t209/`.
 */
@RunWith(RobolectricTestRunner::class)
@GraphicsMode(GraphicsMode.Mode.NATIVE)
@Config(qualifiers = "w393dp-h873dp-xxhdpi", sdk = [36])
class Tarea209CapturasTest {
  @get:Rule val rule = createAndroidComposeRule<ComponentActivity>()

  @Before fun setUp() {
    Assume.assumeTrue("solo al grabar capturas", System.getProperty("roborazzi.test.record") == "true")
    TestSupport.fastFrames()
  }

  private fun at(d: Int, h: Int = 12) = Calendar.getInstance().apply { set(2026, Calendar.OCTOBER, d, h, 0, 0); set(Calendar.MILLISECOND, 0) }.timeInMillis
  private fun r(hits: Int, d: Int, game: String = "stroop") =
    TimeTrialResult(id = "r-$game-$d-$hits", gameId = game, day = "2026-10-%02d".format(d), trackVersion = TimeTrialDay.trackVersion(game), hits = hits, errors = 2, level = 4, seconds = 120, timestamp = at(d))

  private val now = at(28, 18)
  private val results = listOf(r(58, 12), r(62, 20), r(66, 27))          // la mejor de esta semana: 64 aciertos (con 2 errores), En órbita en la referencia de 45 a 54 años

  /** La referencia estimada REAL del APK, como la ve la tienda. */
  private val estWorld: Comparison.Source by lazy {
    val dir = java.io.File("src/main/assets/${EstimatedReference.ASSET_DIR}")
    Comparison.ComposedSource(null, EstimatedReference.of(dir.listFiles { f -> f.extension == "json" }.orEmpty().sortedBy { it.name }.map { it.readText(Charsets.UTF_8) }))
  }

  private var fontState by mutableStateOf(1f)

  private fun shot(name: String, scale: Float = 1f, content: @androidx.compose.runtime.Composable () -> Unit) {
    rule.mainClock.autoAdvance = true
    fontState = scale
    rule.setContent {
      val base = LocalDensity.current
      CompositionLocalProvider(LocalDensity provides Density(base.density, fontState), LocalReduceMotion provides true) {
        NeuroVidaTheme { Box(Modifier.width(393.dp).height(873.dp).background(Color(0xFF050823))) { BoardBackground(); content() } }
      }
    }
    rule.waitForIdle()
  }

  private fun scrollTo(tag: String, marginDp: Int = 12) {
    val y = rule.onNodeWithTag(tag).fetchSemanticsNode().positionInRoot.y
    rule.onNodeWithTag("contrarreloj_scroll").performSemanticsAction(SemanticsActions.ScrollBy) { it(0f, y - marginDp * 3f) }     // xxhdpi: 3 px por dp
    rule.waitForIdle()
  }

  private fun save(name: String) { rule.onRoot().captureRoboImage(filePath = "src/test/screenshots/t209/ahora_$name.png") }

  // ---------------------------------------------------------------- la página con su frase nueva, su enlace y el lugar de la división

  @Test fun a_comparacion_393_10() {
    shot("a") { ContrarrelojPage(results = results, selected = "stroop", onSelect = {}, now = now, compare = estWorld, ageRange = AgeRange.R45_54) }
    // baja hasta el rótulo de la comparación para que se vea la frase, el enlace y la cifra con su lugar
    scrollTo("contrarreloj_section_compare")
    save("comparacion_393_10")
  }

  @Test fun b_menores_393_10() {
    shot("b") { ContrarrelojPage(results = results, selected = "stroop", onSelect = {}, now = now, compare = estWorld, ageRange = AgeRange.R14_17) }
    save("menores_393_10")
  }

  // ---------------------------------------------------------------- la hoja del candado (el contenido, sin Dialog)

  private fun sheet(scale: Float, name: String, toEnd: Boolean) {
    shot(name, scale) { PlusLockSheetContent(onClose = {}, kind = Comparison.Kind.ESTIMATED, onSource = {}) }
    if (toEnd) { rule.onNodeWithTag("btn_plus_sheet_ok", useUnmergedTree = true).performScrollTo(); rule.waitForIdle() }
    save(name)
  }

  @Test fun d_hoja_393_10() = sheet(1f, "hoja_393_10", false)
  @Test fun d2_hoja_393_20() = sheet(2f, "hoja_393_20", false)
  @Test fun d3_hoja_393_20_fin() = sheet(2f, "hoja_393_20_fin", true)

  // ---------------------------------------------------------------- Preguntas frecuentes y De dónde sale la referencia

  @Test fun e_preguntas_393_10() {
    shot("e") {
      Column(Modifier.fillMaxSize()) {
        PanelHeader(Preguntas.TITLE, onBack = {})
        PreguntasScreen(expanded = setOf("telefono", "referencia"), onToggle = {}, onOpenReferencia = {}, modifier = Modifier.weight(1f))
      }
    }
    save("preguntas_393_10")
  }

  @Test fun f_referencia_393_10() {
    shot("f") { ReferenciaPanel(onClose = {}) }
    save("referencia_393_10")
  }

  @Test fun g_referencia_fin_393_10() {
    shot("g") { ReferenciaPanel(onClose = {}) }
    rule.onNodeWithTag("referencia_linea_otros").performScrollTo(); rule.waitForIdle()
    save("referencia_fin_393_10")
  }
}
