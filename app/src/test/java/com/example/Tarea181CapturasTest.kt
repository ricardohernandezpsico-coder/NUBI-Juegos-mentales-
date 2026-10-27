package com.example

import androidx.compose.foundation.background
import androidx.compose.foundation.layout.Box
import androidx.compose.foundation.layout.fillMaxWidth
import androidx.compose.foundation.layout.height
import androidx.compose.foundation.layout.padding
import androidx.compose.foundation.layout.width
import androidx.compose.runtime.CompositionLocalProvider
import androidx.compose.runtime.getValue
import androidx.compose.runtime.mutableStateOf
import androidx.compose.runtime.remember
import androidx.compose.runtime.setValue
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.graphics.Color
import androidx.compose.ui.platform.LocalDensity
import androidx.compose.ui.platform.testTag
import androidx.compose.ui.test.junit4.createComposeRule
import androidx.compose.ui.test.onNodeWithTag
import androidx.compose.ui.test.onRoot
import androidx.compose.ui.test.performClick
import androidx.compose.ui.unit.Density
import androidx.compose.ui.unit.dp
import com.example.data.PlayMode
import com.example.data.TimeTrialResult
import com.example.data.TimeTrialStats
import com.example.games.TimeTrialResultScreen
import com.example.model.AgeBand
import com.example.model.GameRegistry
import com.example.ui.screens.GameCardData
import com.example.ui.screens.GameSheetContent
import com.example.ui.screens.cardData
import com.example.ui.screens.sheetTimeTrialOf
import com.example.ui.components.SlabColor
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
 * Las capturas de la lámina de la Tarea 181 («hoy | propuesta»): la ficha de Tinta o Palabra y la de Freno de Emergencia, plegadas y con «Más opciones», con letra 1,0, 1,3 y 2,0; y el final de un Contrarreloj. «hoy» se grabó con la ficha de antes (sin el botón ni
 * `timeTrial`; `git stash`) y «propuesta» con la de ahora, con el botón «Contrarreloj» en lila y en crema, sin y con récord; la carpeta sale de la variable de entorno `T181_OUT` («hoy» o «propuesta»; por omisión `propuesta`). No corre en las pruebas de siempre: solo al grabar (`./gradlew.bat :app:recordRoborazziDebug --tests "*Tarea181CapturasTest*"`).
 */
@RunWith(RobolectricTestRunner::class)
@GraphicsMode(GraphicsMode.Mode.NATIVE)
@Config(qualifiers = "w393dp-h873dp-440dpi", sdk = [36])
class Tarea181CapturasTest {
  @get:Rule val rule = createComposeRule()

  @Before fun solo_al_grabar() { Assume.assumeTrue("solo al grabar capturas", System.getProperty("roborazzi.test.record") == "true") }

  private val day = 86_400_000L
  private val now = 400 * day + 3_600_000L
  private val out = "src/test/screenshots/tarea-181/" + (System.getenv("T181_OUT") ?: "propuesta")

  private fun card(id: String, points: Float): GameCardData {
    val g = GameRegistry.getById(id)!!
    return cardData(g, g.title, points)
  }

  /** La ficha de [id] en el marco de la ventana real (393 × 801 dp: lo que dejan las barras del sistema) con la letra [fontScale]; [unfold] la despliega («Más opciones»). */
  private fun sheet(id: String, name: String, fontScale: Float = 1f, timed: Boolean = false, unfold: Boolean = false, record: Int? = null, color: SlabColor = SlabColor.LILAC) {
    rule.mainClock.autoAdvance = false
    val data = card(id, 0.62f)
    rule.setContent {
      CompositionLocalProvider(LocalDensity provides Density(LocalDensity.current.density, fontScale)) {
        NeuroVidaTheme {
          var choice by remember { mutableStateOf(PlayMode.A_TU_MEDIDA) }
          var withClock by remember { mutableStateOf(timed) }
          Box(Modifier.width(393.dp).height(873.dp - 72.dp).background(Color(0xFF050823)).testTag("frame").padding(horizontal = 14.dp, vertical = 12.dp), contentAlignment = Alignment.Center) {
            Box(Modifier.fillMaxWidth()) {
              GameSheetContent(
                data, AgeBand.ADULT, expertOpen = false, choice = choice, onChoose = { choice = it }, onPlay = {}, timed = withClock, onChooseTimed = { withClock = it },
                timeTrial = sheetTimeTrialOf(id, data.title, record, "2026-10-26", color), onTimeTrial = {}
              )
            }
          }
        }
      }
    }
    repeat(3) { rule.mainClock.advanceTimeBy(600); rule.waitForIdle() }
    if (unfold) {
      rule.onNodeWithTag("sheet_change").performClick()
      repeat(4) { rule.mainClock.advanceTimeBy(600); rule.waitForIdle() }
    }
    rule.onNodeWithTag("frame").captureRoboImage(filePath = "$out/$name.png")
  }

  @Test fun tinta_100() = sheet("stroop", "tinta-1_0")
  @Test fun tinta_130() = sheet("stroop", "tinta-1_3", fontScale = 1.3f)
  @Test fun tinta_200() = sheet("stroop", "tinta-2_0", fontScale = 2f)
  @Test fun tinta_mas_100() = sheet("stroop", "tinta-mas-1_0", unfold = true)
  @Test fun tinta_mas_130() = sheet("stroop", "tinta-mas-1_3", fontScale = 1.3f, unfold = true)
  @Test fun tinta_record_100() = sheet("stroop", "tinta-record-1_0", record = 41)
  @Test fun tinta_record_130() = sheet("stroop", "tinta-record-1_3", fontScale = 1.3f, record = 41)
  @Test fun tinta_record_200() = sheet("stroop", "tinta-record-2_0", fontScale = 2f, record = 41)
  @Test fun tinta_crema_100() = sheet("stroop", "tinta-crema-1_0", color = SlabColor.CREAM)
  @Test fun tinta_crema_record_100() = sheet("stroop", "tinta-crema-record-1_0", record = 41, color = SlabColor.CREAM)
  @Test fun disparate_record_130() = sheet("disparate", "disparate-record-1_3", fontScale = 1.3f, record = 33)
  @Test fun rescate_record_130() = sheet("radar", "rescate-record-1_3", fontScale = 1.3f, record = 12)
  @Test fun acoplamiento_100() = sheet("acoplamiento", "acoplamiento-1_0")
  @Test fun freno_100() = sheet("freno", "freno-1_0", timed = true)
  @Test fun freno_130() = sheet("freno", "freno-1_3", fontScale = 1.3f, timed = true)
  @Test fun freno_mas_100() = sheet("freno", "freno-mas-1_0", timed = true, unfold = true)

  private fun at(d: Int) = Calendar.getInstance().apply { set(2026, Calendar.OCTOBER, d, 12, 0, 0); set(Calendar.MILLISECOND, 0) }.timeInMillis
  private fun r(hits: Int, d: Int, game: String = "stroop", errors: Int = 0) =
    TimeTrialResult(id = "r-$game-$d-$hits-$errors", gameId = game, day = "2026-10-%02d".format(d), trackVersion = 2, hits = hits, errors = errors, level = 4, seconds = 120, timestamp = at(d))

  /** El final de un Contrarreloj de Tinta o Palabra: [d] = el día de su pista (el 26 es el día de Tinta; el 27, no). */
  private fun final(d: Int, name: String, fontScale: Float = 1f) {
    val history = listOf(r(31, 20), r(35, 21), r(41, 23), r(36, 24))
    val played = r(38, d, errors = 3)
    val outcome = TimeTrialStats.outcome(history + played, played)
    rule.setContent {
      CompositionLocalProvider(LocalDensity provides Density(LocalDensity.current.density, fontScale)) {
        NeuroVidaTheme { Box(Modifier.width(393.dp).height(873.dp).background(Color(0xFF050823))) { TimeTrialResultScreen(outcome = outcome, onAgain = {}, onDone = {}, soundEnabled = false) } }
      }
    }
    rule.waitForIdle()
    rule.onRoot().captureRoboImage(filePath = "$out/$name.png")
  }

  @Test fun final_dia_de_tinta() = final(26, "final-26")
  @Test fun final_otro_dia() = final(27, "final-27")
}
