package com.example

import androidx.compose.foundation.layout.Box
import androidx.compose.foundation.layout.height
import androidx.compose.foundation.layout.padding
import androidx.compose.foundation.layout.width
import androidx.compose.runtime.getValue
import androidx.compose.runtime.mutableStateOf
import androidx.compose.runtime.setValue
import androidx.compose.ui.Modifier
import androidx.compose.ui.semantics.SemanticsActions
import androidx.compose.ui.platform.testTag
import androidx.compose.ui.test.hasText
import androidx.compose.ui.test.onNodeWithTag
import androidx.compose.ui.test.junit4.createComposeRule
import androidx.compose.ui.text.TextLayoutResult
import androidx.compose.ui.unit.dp
import androidx.compose.ui.unit.sp
import com.example.data.Week
import com.example.model.AppLanguage
import com.example.model.GameRegistry
import com.example.model.RankTier
import com.example.ui.components.BoardBackground
import com.example.ui.i18n.getGameTitle
import com.example.ui.screens.HomeContent
import com.example.ui.screens.HomeUi
import com.example.ui.screens.StoneFit
import com.example.ui.screens.homeStops
import com.example.ui.theme.NeuroVidaTheme
import com.github.takahirom.roborazzi.RobolectricDeviceQualifiers
import org.junit.Assert.assertEquals
import org.junit.Assert.assertTrue
import org.junit.Rule
import org.junit.Test
import org.junit.runner.RunWith
import org.robolectric.RobolectricTestRunner
import org.robolectric.annotation.Config
import org.robolectric.annotation.GraphicsMode
import java.util.Calendar

/**
 * Tarea 87: en las piedritas de «Tu ruta de hoy» NUNCA se parte una palabra a la mitad («Constelacione / s» a 411 dp). Se prueba con los nombres de TODOS los juegos de la app, de a tres (como en la ruta), a 360, 393 y 411 dp:
 * cada nombre se lee con sus saltos de línea solo entre palabras, a 14 sp o más; y `StoneFit` elige la primera medida en la que cabe la palabra más larga (o pasa a filas).
 */
@RunWith(RobolectricTestRunner::class)
@GraphicsMode(GraphicsMode.Mode.NATIVE)
@Config(qualifiers = RobolectricDeviceQualifiers.Pixel8, sdk = [36])
class HoyStonesWordsTest {
  @get:Rule val rule = createComposeRule()

  private val now = Calendar.getInstance().apply { set(2026, Calendar.OCTOBER, 11, 16, 30, 0) }.timeInMillis
  private val lamps = Week.Lamps(listOf(true, true, false, false, false, false, true), todayIndex = 6)
  private val ids = GameRegistry.allGames.map { it.id }
  private fun title(id: String) = getGameTitle(id, AppLanguage.SPANISH, GameRegistry.getById(id)!!.title)

  private fun uiFor(three: List<String>) = HomeUi(
    name = "Ricardo", now = now, tier = RankTier.fromRating(0), streak = 3, weeklyGoal = 4, lamps = lamps,
    stops = homeStops(three, 0, AppLanguage.SPANISH), pausedTitle = null, flightDay = false, hasBaseline = true
  )

  /** Los saltos de línea que caen en medio de una palabra (letra con letra a cada lado del corte). */
  private fun midWordBreaks(l: TextLayoutResult): List<String> {
    val text = l.layoutInput.text.text
    val out = mutableListOf<String>()
    for (i in 0 until l.lineCount - 1) {
      val end = l.getLineEnd(i, visibleEnd = false)
      if (end in 1 until text.length && text[end - 1].isLetterOrDigit() && text[end].isLetterOrDigit()) out += "«${text.substring(maxOf(0, end - 4), end)}|${text.substring(end, minOf(text.length, end + 4))}»"
    }
    return out
  }

  private fun checkWidth(widthDp: Int, heightDp: Int) {
    var current by mutableStateOf(uiFor(ids.take(3)))
    rule.setContent {
      NeuroVidaTheme {
        Box(Modifier.width(widthDp.dp).height(heightDp.dp)) {
          BoardBackground()
          HomeContent(ui = current, onAllGames = {}, onPlaceGame = {}, onPlaceGameCard = {}, onStop = {}, onPlay = {}, onResume = {}, onBaseline = {}, modifier = Modifier.padding(top = 24.dp, bottom = 163.dp))
        }
      }
    }
    var checked = 0
    val problems = mutableListOf<String>()
    for (three in (ids + ids.take(2)).chunked(3).filter { it.size == 3 }) {
      current = uiFor(three); rule.waitForIdle()
      for (id in three) {
        val nodes = rule.onAllNodes(hasText(title(id)), useUnmergedTree = true).fetchSemanticsNodes()
        if (nodes.isEmpty()) { problems += "$widthDp dp: no encontré «${title(id)}»"; continue }
        for (node in nodes) {
          val results = mutableListOf<TextLayoutResult>()
          node.config[SemanticsActions.GetTextLayoutResult].action?.invoke(results)
          val layout = results.firstOrNull() ?: continue
          checked++
          val breaks = midWordBreaks(layout)
          if (breaks.isNotEmpty()) problems += "$widthDp dp: «${title(id)}» se parte a media palabra $breaks"
          val sp = layout.layoutInput.style.fontSize.value
          if (sp < 14f) problems += "$widthDp dp: «${title(id)}» baja de 14 sp ($sp)"
        }
      }
    }
    assertTrue("hay ${ids.size} juegos en la app (≥ 17)", ids.size >= 17)
    assertTrue("se revisaron los nombres de todos los juegos ($checked)", checked >= ids.size)
    assertEquals("nombres partidos a media palabra: $problems", emptyList<String>(), problems)
  }

  @Test
  fun el_detector_si_ve_una_palabra_partida_a_la_mitad() {
    rule.setContent { NeuroVidaTheme { androidx.compose.material3.Text("Constelaciones", modifier = Modifier.width(70.dp).testTag("estrecho"), fontSize = 15.sp) } }
    rule.waitForIdle()
    val results = mutableListOf<TextLayoutResult>()
    rule.onNodeWithTag("estrecho").fetchSemanticsNode().config[SemanticsActions.GetTextLayoutResult].action?.invoke(results)
    assertTrue("a 70 dp «Constelaciones» se parte y el detector lo ve: ${midWordBreaks(results.first())}", midWordBreaks(results.first()).isNotEmpty())
  }

  @Test fun ningun_nombre_se_parte_a_360_dp() = checkWidth(360, 780)
  @Test fun ningun_nombre_se_parte_a_393_dp() = checkWidth(393, 873)
  @Test fun ningun_nombre_se_parte_a_411_dp() = checkWidth(411, 914)

  @Test
  fun StoneFit_elige_la_primera_medida_en_la_que_cabe_la_palabra_mas_larga_o_pasa_a_filas() {
    val d = 1f
    // palabra más larga de 100 px a 15 sp y de 94 a 14 sp
    val widest = { sp: Int -> if (sp == 15) 100f else 94f }
    assertEquals("sobra: lo normal (15 sp, relleno 9)", StoneFit.NORMAL, StoneFit.pick(140f, d, widest))
    assertEquals("justo: 15 sp con relleno chico (6)", StoneFit.TIGHT_PADDING, StoneFit.pick(115f, d, widest))
    assertEquals("más justo: 14 sp con relleno chico", StoneFit.SMALL_TEXT, StoneFit.pick(107f, d, widest))
    assertEquals("ni a 14 sp cabe: filas", null, StoneFit.pick(100f, d, widest))
    assertEquals("el relleno de las tres medidas: 9, 6 y 6 dp; nombre de 15, 15 y 14 sp", listOf(9, 6, 6) to listOf(15, 15, 14), StoneFit.entries.map { it.padding.value.toInt() } to StoneFit.entries.map { it.titleSp })
  }
}
