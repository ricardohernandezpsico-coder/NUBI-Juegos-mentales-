package com.example

import androidx.compose.foundation.background
import androidx.compose.foundation.layout.Box
import androidx.compose.foundation.layout.height
import androidx.compose.foundation.layout.width
import androidx.compose.runtime.CompositionLocalProvider
import androidx.compose.runtime.getValue
import androidx.compose.runtime.mutableStateOf
import androidx.compose.runtime.setValue
import androidx.compose.ui.Modifier
import androidx.compose.ui.graphics.Color
import androidx.compose.ui.layout.positionInRoot
import androidx.compose.ui.platform.LocalDensity
import androidx.compose.ui.semantics.SemanticsActions
import androidx.compose.ui.test.SemanticsMatcher
import androidx.compose.ui.test.assertIsDisplayed
import androidx.compose.ui.test.hasTestTag
import androidx.compose.ui.test.hasText
import androidx.compose.ui.test.junit4.createComposeRule
import androidx.compose.ui.test.onAllNodesWithTag
import androidx.compose.ui.test.onNodeWithTag
import androidx.compose.ui.test.onRoot
import androidx.compose.ui.test.performClick
import androidx.compose.ui.test.performScrollTo
import androidx.compose.ui.unit.Density
import androidx.compose.ui.unit.dp
import com.github.takahirom.roborazzi.captureRoboImage
import com.example.data.TimeTrialResult
import com.example.data.TimeTrialStats
import com.example.games.TimeTrialBarMax
import com.example.games.TimeTrialBarMin
import com.example.games.TimeTrialResultScreen
import com.example.games.timeTrialFinalUi
import com.example.ui.theme.NeuroVidaTheme
import org.junit.Assert.assertEquals
import org.junit.Assert.assertFalse
import org.junit.Assert.assertNull
import org.junit.Assert.assertTrue
import org.junit.Rule
import org.junit.Test
import org.junit.runner.RunWith
import org.robolectric.RobolectricTestRunner
import org.robolectric.annotation.Config
import org.robolectric.annotation.GraphicsMode
import java.util.Calendar

/**
 * El final del Contrarreloj (Tarea 177; `auditoria-fable/contrarreloj.md` §4) en sus tres estados —sin récord, con marca nueva y la primera vez—: lo que dice, lo que NO dice (percentiles, otras personas), que cada botón se toque y que
 * entre en un teléfono de 393 × 873 y de 360 × 780 con letra 1,0 y 1,3, sin partir ninguna palabra. Una sola pantalla por prueba, a la que se le cambia la partida.
 */
@RunWith(RobolectricTestRunner::class)
@GraphicsMode(GraphicsMode.Mode.NATIVE)
@Config(qualifiers = "w393dp-h873dp-xxhdpi", sdk = [36])
class TimeTrialResultScreenTest {
  @get:Rule val rule = createComposeRule()

  private var current by mutableStateOf<TimeTrialStats.Outcome?>(null)
  private var again = 0
  private var done = 0

  private fun at(day: Int, h: Int = 12) = Calendar.getInstance().apply { set(2026, Calendar.OCTOBER, day, h, 0, 0); set(Calendar.MILLISECOND, 0) }.timeInMillis
  private fun r(hits: Int, day: Int, game: String = "stroop", errors: Int = 5, level: Int = 4) =
    TimeTrialResult(id = "r-$game-$day-$hits", gameId = game, day = "2026-10-%02d".format(day), trackVersion = 1, hits = hits, errors = errors, level = level, seconds = 120, timestamp = at(day))

  private val history = listOf(r(31, 20), r(35, 21), r(41, 23), r(36, 24))

  /** Sin récord: hoy 38 aciertos y el récord es 41. */
  private val sinRecord: TimeTrialStats.Outcome get() = r(38, 26).let { TimeTrialStats.outcome(history + it, it) }

  /** Marca nueva: hoy 43 aciertos y el récord de antes era 41. */
  private val marcaNueva: TimeTrialStats.Outcome get() = r(43, 26).let { TimeTrialStats.outcome(history + it, it) }

  /** La primera vez que se juega este juego. */
  private val primeraVez: TimeTrialStats.Outcome get() = r(24, 26, game = "radar", level = 3).let { TimeTrialStats.outcome(listOf(it), it) }

  private var widthState by mutableStateOf(393)
  private var heightState by mutableStateOf(873)
  private var fontState by mutableStateOf(1f)

  /** Arma UNA pantalla (un solo `setContent` por prueba); el tamaño y la letra se cambian después con [resize]. */
  private fun mount(widthDp: Int = 393, heightDp: Int = 873, fontScale: Float = 1f) {
    widthState = widthDp; heightState = heightDp; fontState = fontScale
    rule.setContent {
      val d = LocalDensity.current
      CompositionLocalProvider(LocalDensity provides Density(d.density, fontState)) {
        NeuroVidaTheme {
          Box(Modifier.width(widthState.dp).height(heightState.dp).background(Color(0xFF050823))) {
            current?.let { o -> TimeTrialResultScreen(outcome = o, onAgain = { again++ }, onDone = { done++ }, soundEnabled = false) }
          }
        }
      }
    }
  }

  private fun resize(widthDp: Int, heightDp: Int, fontScale: Float) { widthState = widthDp; heightState = heightDp; fontState = fontScale; rule.waitForIdle() }

  private fun show(o: TimeTrialStats.Outcome) { current = o; rule.waitForIdle() }
  private fun has(tag: String) = rule.onAllNodesWithTag(tag, useUnmergedTree = true).fetchSemanticsNodes().isNotEmpty()
  private fun topDp(tag: String): Float = rule.onNodeWithTag(tag, useUnmergedTree = true).fetchSemanticsNode().layoutInfo.coordinates.positionInRoot().y / rule.density.density
  private fun heightOf(tag: String): Float = rule.onNodeWithTag(tag, useUnmergedTree = true).fetchSemanticsNode().layoutInfo.height / rule.density.density

  /** Todo el texto que se ve en la pantalla (unido), para buscar frases. */
  private fun allText(): String =
    rule.onAllNodes(SemanticsMatcher.keyIsDefined(androidx.compose.ui.semantics.SemanticsProperties.Text), useUnmergedTree = true).fetchSemanticsNodes()
      .joinToString(" | ") { n -> n.config[androidx.compose.ui.semantics.SemanticsProperties.Text].joinToString("") { it.text } }

  /** NUNCA una palabra partida a la mitad: el renglón de cada texto termina en un espacio o al final. */
  private fun assertNoWordSplit() {
    for (node in rule.onAllNodes(SemanticsMatcher.keyIsDefined(SemanticsActions.GetTextLayoutResult), useUnmergedTree = true).fetchSemanticsNodes()) {
      val results = mutableListOf<androidx.compose.ui.text.TextLayoutResult>()
      node.config[SemanticsActions.GetTextLayoutResult].action?.invoke(results)
      val l = results.firstOrNull() ?: continue
      val text = l.layoutInput.text.text
      for (line in 0 until l.lineCount - 1) {
        val end = l.getLineEnd(line, visibleEnd = false)
        assertTrue("«$text» parte una palabra a la mitad en el renglón ${line + 1}", end >= text.length || text[end - 1] == ' ' || text[end] == ' ' || text[end - 1] == '\n')
      }
    }
  }

  // ------------------------------------------------------------------ lo que dice en cada estado

  @Test
  fun `con marca nueva dice el sello, la cifra, lo de antes, el paso, las ultimas partidas y la semana`() {
    mount(); show(marcaNueva)
    rule.onNodeWithTag("timetrial_result").assertIsDisplayed()
    val t = allText()
    for (part in listOf("Atención · Contrarreloj de hoy", TimeTrialStats.SEAL, "43 aciertos", TimeTrialStats.UNIT, "Antes, tu mejor marca era 41 aciertos.", "Llegaste al paso 4 de 5 de la pista.", TimeTrialStats.LAST_TITLE, "La mejor de esta semana: 43 aciertos, la de hoy.", TimeTrialStats.NOTE))
      assertTrue("falta «$part» en: $t", t.contains(part))
    assertEquals("cinco barras: las cuatro de antes y la de hoy", 5, (0..4).count { has("timetrial_bar_$it") })
    assertTrue(has("timetrial_last") && has("timetrial_week") && has("timetrial_step"))
  }

  @Test
  fun `sin record no hay sello y dice cuanto es el record y de hace cuantos dias`() {
    mount(); show(sinRecord)
    val t = allText()
    assertFalse("no hay marca nueva: sin sello", t.contains(TimeTrialStats.SEAL))
    assertTrue(t.contains("38 aciertos"))
    assertTrue("falta lo del récord en: $t", t.contains("Tu mejor marca sigue en 41 aciertos"))
    assertTrue(t.contains(TimeTrialStats.NOTE))
    assertTrue(has("timetrial_last"))
  }

  @Test
  fun `la primera vez no dibuja las ultimas partidas ni la semana y dice como seguir`() {
    mount(); show(primeraVez)
    val t = allText()
    assertFalse(t.contains(TimeTrialStats.SEAL))
    assertTrue(t.contains("24 aciertos"))
    assertTrue("falta la frase de la primera vez en: $t", t.contains("Es tu primera marca en el Contrarreloj. La próxima te comparas contigo."))
    assertFalse("no hay con qué comparar: sin barras", has("timetrial_last"))
    assertFalse(has("timetrial_week"))
    assertTrue("el paso sí se dice", has("timetrial_step"))
    assertTrue(t.contains("Llegaste al paso 3 de 12 de la pista."))
  }

  @Test
  fun `nunca habla de percentiles, de otras personas ni de capacidad, y la nota lo aclara`() {
    mount()
    for (o in listOf(sinRecord, marcaNueva, primeraVez)) {
      show(o)
      val t = allText()
      for (banned in listOf("percentil", "Percentil", "personas como tú", "mejor que", "cerebro", "cognitiv", "entrenamiento", "Reto", "Desafío", "nivel", "Nivel"))
        assertFalse("«$banned» no debe verse: $t", t.contains(banned))
      assertTrue(t.contains("Es el resultado de un juego, comparado solo contigo. No es una medida de capacidad."))
    }
  }

  // ------------------------------------------------------------------ los botones y las barras

  @Test
  fun `Otra vez y Listo se tocan cada uno con su accion`() {
    mount(); show(marcaNueva)
    rule.onNodeWithTag("btn_timetrial_again").performScrollTo().performClick()
    assertEquals(1, again); assertEquals(0, done)
    rule.onNodeWithTag("btn_timetrial_done").performScrollTo().performClick()
    assertEquals(1, again); assertEquals(1, done)
    assertTrue(allText().contains(TimeTrialStats.AGAIN) && allText().contains(TimeTrialStats.DONE))
  }

  @Test
  fun `las barras crecen con la marca, la mas larga mide el maximo y la de 0 aciertos sigue viendose`() {
    mount(); show(marcaNueva)
    val ui = timeTrialFinalUi(marcaNueva, "Tinta o Palabra")
    assertEquals(listOf(35, 41, 36, 43).takeLast(4), ui.last.takeLast(4))
    assertEquals("la última es la de hoy", 43, ui.last.last())
    val tallest = (0 until ui.last.size).maxOf { heightOf("timetrial_bar_$it") }
    assertTrue("la barra más larga no pasa de ${TimeTrialBarMax.value} dp + su cifra", tallest > TimeTrialBarMax.value && tallest < TimeTrialBarMax.value + 70f)
    val zero = r(0, 26).let { TimeTrialStats.outcome(history + it, it) }
    show(zero)
    assertEquals("0 aciertos en la barra de hoy", 0, timeTrialFinalUi(zero, "Tinta o Palabra").last.last())
    assertTrue("y la barra de hoy sigue viéndose (mínimo ${TimeTrialBarMin.value} dp)", heightOf("timetrial_bar_${timeTrialFinalUi(zero, "Tinta o Palabra").last.lastIndex}") >= TimeTrialBarMin.value)
  }

  @Test
  fun `el armado de la pantalla trae solo lo del diseno`() {
    val ui = timeTrialFinalUi(marcaNueva, "Tinta o Palabra")
    assertEquals("43 aciertos", ui.headline)
    assertEquals(TimeTrialStats.UNIT, ui.unit)
    assertEquals(TimeTrialStats.SEAL, ui.seal)
    assertEquals(TimeTrialStats.NOTE, ui.note)
    assertNull("sin marca nueva no hay sello", timeTrialFinalUi(sinRecord, "Tinta o Palabra").seal)
    assertTrue("la primera vez no trae barras", timeTrialFinalUi(primeraVez, "Rescate relámpago").last.isEmpty())
    assertTrue(ui.spoken.contains("Tinta o Palabra, Contrarreloj de hoy"))
  }

  // ------------------------------------------------------------------ que entre en el teléfono

  private fun assertFits(widthDp: Int, heightDp: Int, fontScale: Float, o: TimeTrialStats.Outcome, label: String) {
    show(o)
    assertNoWordSplit()
    // el botón principal EMPIEZA por encima del borde de abajo (se ve sin deslizar) con letra 1,0 y 1,3
    val top = topDp("btn_timetrial_again")
    assertTrue("$label a $widthDp × $heightDp con letra $fontScale: «Otra vez» empieza en $top dp, más abajo del borde ($heightDp dp)", top < heightDp - 56f)
    rule.onNodeWithTag("btn_timetrial_done").performScrollTo().assertIsDisplayed()
  }

  /** Las capturas de la lámina de la Tarea 177 (solo al grabar): el final en sus tres estados y con letra 1,3. En `src/test/screenshots/tarea-177/`. */
  @Test
  fun tarea_177_capturas() {
    org.junit.Assume.assumeTrue("solo al grabar capturas", System.getProperty("roborazzi.test.record") == "true")
    val out = "src/test/screenshots/tarea-177"
    mount()
    fun shot(name: String, o: TimeTrialStats.Outcome, w: Int = 393, h: Int = 873, fs: Float = 1f, bottom: Boolean = false) {
      resize(w, h, fs); show(o); repeat(3) { rule.mainClock.advanceTimeBy(300); rule.waitForIdle() }
      rule.onRoot().captureRoboImage(filePath = "$out/$name.png")
      if (bottom) { rule.onNodeWithTag("btn_timetrial_done").performScrollTo(); rule.waitForIdle(); rule.onRoot().captureRoboImage(filePath = "$out/$name-abajo.png") }
    }
    shot("final-sin-record", sinRecord, bottom = true)
    shot("final-marca-nueva", marcaNueva, bottom = true)
    shot("final-primera-vez", primeraVez, bottom = true)
    shot("final-marca-nueva-1_3", marcaNueva, fs = 1.3f, bottom = true)
  }

  @Test
  fun `entra en 393 x 873 y en 360 x 780 con letra 1_0 y 1_3 en los tres estados`() {
    mount()
    for ((w, h) in listOf(393 to 873, 360 to 780)) for (fs in listOf(1f, 1.3f)) for ((name, o) in listOf("sin récord" to sinRecord, "marca nueva" to marcaNueva, "primera vez" to primeraVez)) {
      resize(w, h, fs); assertFits(w, h, fs, o, name)
    }
  }
}
