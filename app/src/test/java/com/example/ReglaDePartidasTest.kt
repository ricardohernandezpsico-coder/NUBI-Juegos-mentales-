package com.example

import androidx.compose.foundation.background
import androidx.compose.foundation.layout.Box
import androidx.compose.foundation.layout.fillMaxWidth
import androidx.compose.foundation.layout.height
import androidx.compose.foundation.layout.padding
import androidx.compose.foundation.layout.width
import androidx.compose.runtime.CompositionLocalProvider
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.graphics.Color
import androidx.compose.ui.layout.positionInRoot
import androidx.compose.ui.platform.LocalDensity
import androidx.compose.ui.platform.testTag
import androidx.compose.ui.semantics.SemanticsActions
import androidx.compose.ui.semantics.SemanticsNode
import androidx.compose.ui.semantics.SemanticsProperties
import androidx.compose.ui.semantics.getOrNull
import androidx.compose.ui.test.hasTestTag
import androidx.compose.ui.test.junit4.createComposeRule
import androidx.compose.ui.test.onNodeWithTag
import androidx.compose.ui.test.onRoot
import androidx.compose.ui.test.performClick
import androidx.compose.ui.test.performScrollTo
import androidx.compose.ui.text.TextLayoutResult
import androidx.compose.ui.unit.Density
import androidx.compose.ui.unit.dp
import com.example.data.NBSP
import com.example.data.PlusAccess
import com.example.data.TimeTrialDay
import com.example.data.TimeTrialResult
import com.example.data.TimeTrialState
import com.example.data.Week
import com.example.games.TimeTrialResultScreen
import com.example.data.TimeTrialStats
import com.example.model.AgeBand
import com.example.model.AppLanguage
import com.example.model.GameRegistry
import com.example.model.RankTier
import com.example.ui.components.BoardBackground
import com.example.ui.components.LocalReduceMotion
import com.example.ui.components.SlabColor
import com.example.ui.screens.ContrarrelojPage
import com.example.ui.screens.GameSheetContent
import com.example.ui.screens.HomeContent
import com.example.ui.screens.HomeTimeTrial
import com.example.ui.screens.HomeUi
import com.example.ui.screens.cardData
import com.example.ui.screens.homeStops
import com.example.ui.screens.sheetTimeTrialOf
import com.example.ui.screens.timeTrialOf
import com.example.ui.theme.NeuroVidaTheme
import com.github.takahirom.roborazzi.RobolectricDeviceQualifiers
import org.junit.Assert.assertEquals
import org.junit.Assert.assertFalse
import org.junit.Assert.assertTrue
import org.junit.Before
import org.junit.Rule
import org.junit.Test
import org.junit.runner.RunWith
import org.robolectric.RobolectricTestRunner
import org.robolectric.annotation.Config
import org.robolectric.annotation.GraphicsMode
import java.util.Calendar

/**
 * La regla de partidas del Contrarreloj (Tarea 218; `auditoria-fable/idea-pestana-contrarreloj.md` §4, §5 y §7): sin Nubilus Plus, solo el Contrarreloj de HOY y 3 partidas al día. Aquí lo que se VE en cada puerta: la ficha del juego (con partidas, gris con candado en los otros tres y sin partidas), la pestaña, Hoy y el final de la partida.
 * La lógica pura vive en `TimeTrialDayTest` y el lanzamiento en `GameFlowTest`. Una composición por prueba.
 */
@RunWith(RobolectricTestRunner::class)
@GraphicsMode(GraphicsMode.Mode.NATIVE)
@Config(qualifiers = RobolectricDeviceQualifiers.Pixel8, sdk = [36])
class ReglaDePartidasTest {
  @get:Rule val rule = createComposeRule()

  @Before fun setUp() { TestSupport.fastFrames() }

  private val today = "2026-10-26"                                       // ese día el Contrarreloj de hoy es Tinta o Palabra
  private fun played(n: Int, game: String = "stroop") = (1..n).map { TimeTrialResult(id = "p$it", gameId = game, day = today, trackVersion = TimeTrialDay.trackVersion(game), hits = 30 + it, errors = 2, level = 3, seconds = 120, timestamp = 1_000L * it) }

  private fun textOf(tag: String): String = rule.onNode(hasTestTag(tag), useUnmergedTree = true).fetchSemanticsNode().config.getOrNull(SemanticsProperties.Text)?.joinToString("") { it.text }.orEmpty().replace(NBSP, " ")
  private fun spokenOf(tag: String): String = rule.onNodeWithTag(tag).fetchSemanticsNode().config.getOrNull(SemanticsProperties.ContentDescription)?.joinToString(". ").orEmpty().replace(NBSP, " ")
  private fun count(tag: String) = rule.onAllNodes(hasTestTag(tag), useUnmergedTree = true).fetchSemanticsNodes().size
  private fun top(tag: String) = rule.onNodeWithTag(tag).fetchSemanticsNode().positionInRoot.y
  private fun bottom(tag: String) = rule.onNodeWithTag(tag).fetchSemanticsNode().let { it.positionInRoot.y + it.size.height }

  // ---------------------------------------------------------------- la ficha del juego

  private var trials = 0

  private fun mountSheet(gameId: String, plus: Boolean, results: List<TimeTrialResult>) {
    rule.setContent {
      NeuroVidaTheme {
        val g = GameRegistry.getById(gameId)!!
        Box(Modifier.width(393.dp).height(900.dp).background(Color(0xFF050823)).padding(horizontal = 14.dp, vertical = 12.dp), contentAlignment = Alignment.Center) {
          Box(Modifier.fillMaxWidth()) {
            GameSheetContent(
              cardData(g, g.title, 0.55f), AgeBand.ADULT, expertOpen = false, choice = com.example.data.PlayMode.A_TU_MEDIDA, onChoose = {}, onPlay = {}, onClose = {},
              timeTrial = sheetTimeTrialOf(gameId, g.title, 41, today, SlabColor.LILAC, plus = plus, results = results), onTimeTrial = { trials++ }
            )
          }
        }
      }
    }
    rule.waitForIdle()
  }

  @Test fun la_ficha_del_juego_de_hoy_dice_cuantas_partidas_quedan_sin_Plus() {
    mountSheet("stroop", plus = false, results = played(1))
    assertEquals(listOf("Contrarreloj", "Te quedan 2 partidas de hoy."), rule.onNodeWithTag("sheet_timetrial").fetchSemanticsNode().config[SemanticsProperties.Text].map { it.text.replace(NBSP, " ") })
    assertTrue("el lector dice el modo, el juego, los 2 minutos, el récord y las partidas: ${spokenOf("sheet_timetrial")}", spokenOf("sheet_timetrial").let { "Tinta" in it && "2 minutos" in it && "récord 41 aciertos" in it && it.endsWith("Te quedan 2 partidas de hoy") })
    assertEquals("no es el gris: es el botón de siempre", 0, count("sheet_timetrial_locked"))
    rule.onNodeWithTag("sheet_timetrial").performScrollTo().performClick(); rule.waitForIdle()
    assertEquals("con partidas, el botón lanza", 1, trials)
  }

  @Test fun con_una_sola_partida_la_ficha_dice_Te_queda_1_partida_de_hoy() {
    mountSheet("stroop", plus = false, results = played(2))
    assertEquals("Te queda 1 partida de hoy.", rule.onNodeWithTag("sheet_timetrial").fetchSemanticsNode().config[SemanticsProperties.Text].last().text.replace(NBSP, " "))
  }

  @Test fun sin_partidas_la_ficha_no_tiene_el_boton_y_dice_Hoy_ya_jugaste_tus_3_partidas() {
    mountSheet("stroop", plus = false, results = played(3))
    assertEquals("el botón no sale", 0, count("sheet_timetrial") + count("sheet_timetrial_locked"))
    assertEquals("Hoy ya jugaste tus 3 partidas.", textOf("sheet_timetrial_none"))
    assertEquals("y el lector dice lo mismo", "Hoy ya jugaste tus 3 partidas.", spokenOf("sheet_timetrial_none"))
  }

  @Test fun en_un_juego_que_no_es_el_de_hoy_el_boton_va_en_gris_con_candado_y_no_lanza() {
    mountSheet("disparate", plus = false, results = emptyList())
    assertEquals("el botón existe, en su forma gris", 0, count("sheet_timetrial"))
    assertEquals(listOf("Contrarreloj", "Con Nubilus Plus"), rule.onNodeWithTag("sheet_timetrial_locked").fetchSemanticsNode().config[SemanticsProperties.Text].map { it.text })
    assertEquals("el lector dice el candado: «Contrarreloj, ¿Verdad o disparate?, con Nubilus Plus»", "Contrarreloj, ${GameRegistry.getById("disparate")!!.title}, con Nubilus Plus", spokenOf("sheet_timetrial_locked"))
    rule.onNodeWithTag("sheet_timetrial_locked").performScrollTo().performClick(); rule.waitForIdle()
    assertEquals("quien lo recibe (la ventana de la ficha) abre la hoja del candado en vez de lanzar: el botón llama a onTimeTrial", 1, trials)
    assertFalse("nunca dice «bloqueado»", spokenOf("sheet_timetrial_locked").contains("bloquead"))
  }

  @Test fun con_Plus_las_fichas_quedan_como_siempre_con_el_record_o_los_2_minutos() {
    mountSheet("disparate", plus = true, results = played(3, "disparate").map { it.copy(day = today) })
    assertEquals(listOf("Contrarreloj", "Tu récord: 41 aciertos."), rule.onNodeWithTag("sheet_timetrial").fetchSemanticsNode().config[SemanticsProperties.Text].map { it.text })
    assertEquals(0, count("sheet_timetrial_locked") + count("sheet_timetrial_none"))
  }

  @Test fun la_ficha_pura_de_cada_caso() {
    fun t(game: String, plus: Boolean, n: Int) = sheetTimeTrialOf(game, GameRegistry.getById(game)!!.title, 12, today, plus = plus, results = played(n, game))
    assertEquals("un juego sin Contrarreloj no lleva botón", null, sheetTimeTrialOf("calculo", "Carga exacta", null, today, plus = false))
    assertEquals(listOf(false, false), t("stroop", false, 0)!!.let { listOf(it.locked, it.noGamesLine != null) })
    assertEquals(listOf(false, true), t("stroop", false, 3)!!.let { listOf(it.locked, it.noGamesLine != null) })
    assertEquals(listOf(true, false), t("radar", false, 0)!!.let { listOf(it.locked, it.noGamesLine != null) })
    assertEquals("con Plus nunca hay candado ni tope", listOf(false, false), t("radar", true, 9)!!.let { listOf(it.locked, it.noGamesLine != null) })
    assertEquals("la segunda línea del gris", PlusAccess.LOCK_LABEL, t("acoplamiento", false, 0)!!.secondLine)
  }

  // ---------------------------------------------------------------- la pestaña

  private var plays = 0
  private var lockedTaps = 0

  private fun mountPage(gamesLeft: Int?, plus: Boolean = false, scale: Float = 1f, width: Int = 393) {
    val results = played(1)
    rule.setContent {
      val base = LocalDensity.current
      CompositionLocalProvider(LocalDensity provides Density(base.density, scale), LocalReduceMotion provides true) {
        NeuroVidaTheme {
          Box(Modifier.width(width.dp).height(873.dp)) {
            BoardBackground()
            ContrarrelojPage(
              results = results, selected = "stroop", onSelect = {}, now = 1_000L, lang = AppLanguage.SPANISH, plus = plus, today = "stroop",
              onPlay = { plays++ }, gamesLeft = gamesLeft, onLocked = { lockedTaps++ }
            )
          }
        }
      }
    }
    rule.waitForIdle()
  }

  @Test fun la_pestana_con_partidas_dice_cuantas_quedan_dentro_de_la_losa_gratis_bajo_Jugar() {
    mountPage(gamesLeft = 2)
    assertEquals("Te quedan 2 partidas de hoy.", textOf("contrarreloj_games_left"))
    assertEquals("Jugar sigue", 1, count("btn_contrarreloj_play"))
    assertEquals("ni la frase de sin partidas ni la fila gris", 0, count("contrarreloj_no_games") + count("contrarreloj_locked_games"))
    assertTrue("dentro de la losa gratis y bajo el botón", top("contrarreloj_games_left") >= bottom("btn_contrarreloj_play") && bottom("contrarreloj_games_left") <= bottom("contrarreloj_free"))
    rule.onNodeWithTag("btn_contrarreloj_play").performClick(); rule.waitForIdle()
    assertEquals(1, plays)
  }

  @Test fun la_pestana_sin_partidas_cambia_Jugar_por_la_frase_y_pone_UNA_fila_gris_bajo_la_losa() {
    mountPage(gamesLeft = 0)
    assertEquals("Hoy ya jugaste tus 3 partidas.", textOf("contrarreloj_no_games"))
    assertEquals("Jugar no sale", 0, count("btn_contrarreloj_play") + count("contrarreloj_games_left"))
    assertEquals("la fila gris con candado, con su frase", PlusAccess.LOCKED_GAMES, textOf("contrarreloj_locked_games_phrase"))
    assertEquals("el lector dice la frase", PlusAccess.LOCKED_GAMES, spokenOf("contrarreloj_locked_games"))
    assertEquals("UNA sola fila gris de partidas", 1, count("contrarreloj_locked_games"))
    assertTrue("y va bajo la losa gratis", top("contrarreloj_locked_games") >= bottom("contrarreloj_free"))
    assertTrue("dentro de la losa, la frase va antes que el récord", top("contrarreloj_no_games") < top("contrarreloj_free_record"))
    val h = rule.onNodeWithTag("contrarreloj_locked_games").fetchSemanticsNode().size.height / rule.density.density
    assertTrue("la fila se toca en 48 dp o más ($h dp)", h >= 48f)
    rule.onNodeWithTag("contrarreloj_locked_games").performScrollTo().performClick(); rule.waitForIdle()
    assertEquals("toda la fila abre la hoja", 1, lockedTaps)
  }

  @Test fun con_Plus_la_pestana_no_cuenta_partidas_ni_pone_fila_gris() {
    mountPage(gamesLeft = null, plus = true)
    assertEquals(0, count("contrarreloj_games_left") + count("contrarreloj_no_games") + count("contrarreloj_locked_games"))
    assertEquals("Jugar sigue", 1, count("btn_contrarreloj_play"))
  }

  @Test fun la_fila_gris_de_la_pestana_no_parte_palabras_con_letra_2_0_a_360_dp() {
    mountPage(gamesLeft = 0, scale = 2f, width = 360)
    val results = mutableListOf<TextLayoutResult>()
    rule.onNode(hasTestTag("contrarreloj_locked_games_phrase"), useUnmergedTree = true).fetchSemanticsNode().config.getOrNull(SemanticsActions.GetTextLayoutResult)?.action?.invoke(results)
    val layout = results.first(); val text = layout.layoutInput.text.text
    for (line in 0 until layout.lineCount - 1) {
      val end = layout.getLineEnd(line)
      assertTrue("el renglón ${line + 1} termina entre palabras", end >= text.length || text[end - 1] == ' ' || text[end] == ' ')
    }
    assertEquals("sin partidas la losa dice la frase entera", "Hoy ya jugaste tus 3 partidas.", textOf("contrarreloj_no_games"))
  }

  /** El último renglón de [tag] (sin recortar) no es una palabra sola: con letra grande «hoy.» o «partidas.» quedaban solas bajo la cifra (Tarea 218, visto en el emulador). */
  private fun lastLineIsNotAWord(tag: String, label: String) {
    val results = mutableListOf<TextLayoutResult>()
    rule.onNode(hasTestTag(tag), useUnmergedTree = true).fetchSemanticsNode().config.getOrNull(SemanticsActions.GetTextLayoutResult)?.action?.invoke(results)
    val layout = results.first(); val text = layout.layoutInput.text.text
    if (layout.lineCount > 1) {
      val last = text.substring(layout.getLineStart(layout.lineCount - 1), layout.getLineEnd(layout.lineCount - 1)).trim().replace(NBSP, " ")
      assertTrue("$label: el último renglón («$last») no es una palabra sola", ' ' in last)
    }
  }

  @Test fun con_letra_2_0_a_393_dp_la_pestana_no_deja_hoy_solo_en_el_ultimo_renglon() { mountPage(gamesLeft = 2, scale = 2f); lastLineIsNotAWord("contrarreloj_games_left", "393 dp, letra 2,0") }
  @Test fun con_letra_2_0_a_360_dp_la_pestana_no_deja_hoy_solo_en_el_ultimo_renglon() { mountPage(gamesLeft = 2, scale = 2f, width = 360); lastLineIsNotAWord("contrarreloj_games_left", "360 dp, letra 2,0") }
  @Test fun con_letra_2_0_a_393_dp_la_frase_de_sin_partidas_no_deja_partidas_sola() { mountPage(gamesLeft = 0, scale = 2f); lastLineIsNotAWord("contrarreloj_no_games", "393 dp, letra 2,0") }
  @Test fun con_letra_2_0_a_360_dp_la_frase_de_sin_partidas_no_deja_partidas_sola() { mountPage(gamesLeft = 0, scale = 2f, width = 360); lastLineIsNotAWord("contrarreloj_no_games", "360 dp, letra 2,0") }

  // ---------------------------------------------------------------- Hoy

  private val lamps = Week.Lamps(listOf(true, true, false, false, false, false, true), todayIndex = 6)
  private val nowHoy = Calendar.getInstance().apply { set(2026, Calendar.OCTOBER, 26, 16, 30, 0) }.timeInMillis
  private val stroopSinPartidas = HomeTimeTrial("stroop", "Tinta o Palabra", TimeTrialState.JUGADO, todayHits = 38, record = 41, gamesLeft = 0, tomorrowTitle = "¿Verdad o disparate?")
  private var homeTrials = 0

  private fun homeUi(t: HomeTimeTrial?, completed: Int = 3) = HomeUi(
    name = "Ricardo", now = nowHoy, tier = RankTier.fromRating(0), streak = 3, weeklyGoal = 4, lamps = lamps,
    stops = homeStops(listOf("bodega", "calculo", "intrusa"), completed, AppLanguage.SPANISH), pausedTitle = null, flightDay = false, hasBaseline = true, timeTrial = t
  )

  private fun mountHome(ui: HomeUi, width: Int = 393, height: Int = 873, scale: Float = 1f) {
    rule.setContent {
      val base = LocalDensity.current
      CompositionLocalProvider(LocalDensity provides Density(base.density, scale), LocalReduceMotion provides true) {
        NeuroVidaTheme {
          Box(Modifier.width(width.dp).height(height.dp)) {
            BoardBackground()
            HomeContent(ui = ui, onPlay = {}, onResume = {}, onBaseline = {}, onTimeTrial = { homeTrials++ }, onLocked = { lockedTaps++ }, modifier = Modifier.padding(top = 24.dp, bottom = 163.dp))
          }
        }
      }
    }
    rule.waitForIdle()
  }

  @Test fun Hoy_sin_partidas_no_tiene_el_boton_Contrarreloj_y_dice_el_juego_de_manana_con_la_fila_gris() {
    mountHome(homeUi(stroopSinPartidas))
    assertEquals("ni el botón «Contrarreloj» ni «Otra vez»: nunca lanzan una partida sin partidas", 0, count("btn_timetrial"))
    assertEquals("Hoy ya jugaste tus 3 partidas. Mañana: ¿Verdad o disparate?", textOf("home_timetrial_tomorrow"))
    assertEquals(PlusAccess.LOCKED_GAMES, textOf("home_locked_games_phrase"))
    assertEquals("UNA sola fila gris", 1, count("home_locked_games"))
    assertTrue("en orden: la tarjeta, el recuadro de la frase y la fila gris", top("home_card") < top("home_timetrial_none") && top("home_timetrial_none") < top("home_locked_games"))
    assertEquals("«Todos los juegos» ya no está en Hoy (Tarea 220: es la pestaña Juegos)", 0, count("btn_all_games"))
    rule.onNodeWithTag("home_locked_games").performScrollTo().performClick(); rule.waitForIdle()
    assertEquals("la fila abre la hoja", 1, lockedTaps)
    assertEquals(0, homeTrials)
  }

  @Test fun Hoy_con_marca_nueva_y_sin_partidas_deja_el_recuadro_arriba_y_no_ofrece_Otra_vez() {
    mountHome(homeUi(stroopSinPartidas.copy(state = TimeTrialState.MARCA_NUEVA, todayHits = 43, record = 43)))
    assertEquals("UN solo recuadro (Tarea 219): la marca nueva va dentro del de «sin partidas»", 1, count("home_timetrial_none"))
    assertEquals("y no hay otro aparte", 0, count("home_timetrial_done"))
    assertEquals("sin «Otra vez»", 0, count("btn_timetrial"))
    assertEquals("Marca nueva: 43 aciertos en Tinta o Palabra.", textOf("home_timetrial_mark"))
    fun topUnmerged(tag: String) = rule.onNode(hasTestTag(tag), useUnmergedTree = true).fetchSemanticsNode().positionInRoot.y
    assertTrue("la marca va arriba de la frase", topUnmerged("home_timetrial_mark") < topUnmerged("home_timetrial_tomorrow"))
  }

  @Test fun Hoy_con_partidas_queda_como_siempre() {
    mountHome(homeUi(HomeTimeTrial("stroop", "Tinta o Palabra", TimeTrialState.PENDIENTE, record = 41, gamesLeft = 2)))
    assertEquals("el botón de siempre", 1, count("btn_timetrial"))
    assertEquals(0, count("home_timetrial_none") + count("home_locked_games"))
    rule.onNodeWithTag("btn_timetrial").performScrollTo().performClick(); rule.waitForIdle()
    assertEquals(1, homeTrials)
  }

  @Test fun timeTrialOf_cuenta_las_partidas_de_hoy_y_con_Plus_no_hay_tope() {
    val sin3 = timeTrialOf(played(3), today, AppLanguage.SPANISH, plus = false)!!
    assertEquals(0, sin3.gamesLeft); assertEquals("¿Verdad o disparate?", sin3.tomorrowTitle)
    val sin1 = timeTrialOf(played(1), today, AppLanguage.SPANISH, plus = false)!!
    assertEquals(2, sin1.gamesLeft); assertEquals("mañana solo se dice cuando no quedan partidas", null, sin1.tomorrowTitle)
    val con = timeTrialOf(played(9), today, AppLanguage.SPANISH, plus = true)!!
    assertEquals("con Plus no hay cuenta", null, con.gamesLeft); assertEquals(null, con.tomorrowTitle)
  }

  /** La cabida de Hoy sin partidas (Opus: «a 393 × 873 y 360 × 780, con 1,0 y 1,3»): el contenido más bajo, sin recortar, contra el borde de la barra (163 dp del fondo); ninguna palabra se parte. */
  private fun hoyCabe(width: Int, height: Int, scale: Float, ui: HomeUi = homeUi(stroopSinPartidas)) {
    mountHome(ui, width, height, scale)
    val end = rule.onNodeWithTag("home_end").fetchSemanticsNode()
    val bottomDp = end.positionInRoot.y / rule.density.density - 6f             // lo último es la fila gris, una losa: sus 6 dp de sombra no cuentan
    val limit = height - 163f
    println("MEDIDA Hoy sin partidas $width x $height letra $scale: la fila gris termina en ${"%.1f".format(bottomDp)} dp; la barra empieza en ${"%.0f".format(limit)} dp (${if (bottomDp <= limit) "cabe" else "se pasa ${"%.1f".format(bottomDp - limit)} dp: Hoy se desliza"})")
    val cuts = mutableListOf<String>()
    fun walk(n: SemanticsNode) {
      val rs = mutableListOf<TextLayoutResult>(); n.config.getOrNull(SemanticsActions.GetTextLayoutResult)?.action?.invoke(rs)
      rs.firstOrNull()?.let { l -> val t = l.layoutInput.text.text; if ("jugaste" in t || "partidas libres" in t) for (i in 0 until l.lineCount - 1) { val e = l.getLineEnd(i); if (e in 1 until t.length && t[e - 1] != ' ' && t[e] != ' ') cuts += "«${t.substring(maxOf(0, e - 10), e)}|${t.substring(e, minOf(t.length, e + 10))}»" } }
      n.children.forEach { walk(it) }
    }
    walk(rule.onRoot(useUnmergedTree = true).fetchSemanticsNode())
    assertEquals("ninguna palabra partida en Hoy sin partidas a $width dp con letra $scale", emptyList<String>(), cuts)
    assertTrue("la tarjeta queda más arriba que el recuadro y la fila gris (Tarea 220: sin «Todos los juegos» entre medio)", bottom("home_card") <= top("home_timetrial_none") + 1f && bottom("home_timetrial_none") <= top("home_locked_games") + 1f)
  }

  @Test fun Hoy_sin_partidas_a_393_x_873_con_letra_1_0() = hoyCabe(393, 873, 1f)
  @Test fun Hoy_sin_partidas_a_393_x_873_con_letra_1_3() = hoyCabe(393, 873, 1.3f)
  @Test fun Hoy_sin_partidas_a_360_x_780_con_letra_1_0() = hoyCabe(360, 780, 1f)
  @Test fun Hoy_sin_partidas_a_360_x_780_con_letra_1_3() = hoyCabe(360, 780, 1.3f)
  @Test fun Hoy_sin_partidas_con_la_ruta_a_medias_a_393_x_873_con_letra_1_0() = hoyCabe(393, 873, 1f, homeUi(stroopSinPartidas, completed = 1))

  // ---------------------------------------------------------------- el final

  private fun mountFinal(canAgain: Boolean, plus: Boolean = false) {
    val all = listOf(TimeTrialResult(id = "f1", gameId = "stroop", day = today, trackVersion = 2, hits = 31, errors = 0, level = 4, seconds = 120, timestamp = 1_000), TimeTrialResult(id = "f2", gameId = "stroop", day = today, trackVersion = 2, hits = 35, errors = 0, level = 4, seconds = 120, timestamp = 2_000))
    val outcome = TimeTrialStats.outcome(all, all.last(), 3_000)
    rule.setContent {
      CompositionLocalProvider(LocalReduceMotion provides true) {
        NeuroVidaTheme { Box(Modifier.width(393.dp).height(1200.dp)) { TimeTrialResultScreen(outcome = outcome, onAgain = { plays++ }, onDone = {}, soundEnabled = false, plus = plus, onAllMarks = if (plus) ({}) else null, canAgain = canAgain) } }
      }
    }
    rule.waitForIdle()
  }

  @Test fun el_final_sin_partidas_no_ofrece_Otra_vez_y_dice_Manana_hay_otro_Contrarreloj() {
    mountFinal(canAgain = false)
    assertEquals(0, count("btn_timetrial_again"))
    assertEquals("Mañana hay otro Contrarreloj.", textOf("timetrial_tomorrow"))
    assertEquals("«Listo» sigue", 1, count("btn_timetrial_done"))
    assertEquals("nada gris en el final (regla de §7): ni fila de candado ni «Con Nubilus Plus»", 0, count("home_locked_games") + count("contrarreloj_locked_games") + count("locked_row_phrase"))
    assertTrue("va donde iba «Otra vez»: antes de «Listo»", top("timetrial_tomorrow") < top("btn_timetrial_done"))
  }

  @Test fun el_final_con_partidas_conserva_Otra_vez() {
    mountFinal(canAgain = true)
    assertEquals(1, count("btn_timetrial_again")); assertEquals(0, count("timetrial_tomorrow"))
    rule.onNodeWithTag("btn_timetrial_again").performScrollTo().performClick(); rule.waitForIdle()
    assertEquals(1, plays)
  }

  @Test fun el_atajo_Todas_tus_marcas_del_final_sigue_solo_con_Plus_aunque_no_queden_partidas() {
    mountFinal(canAgain = false, plus = true)
    assertEquals("con Plus el atajo está; sin partidas, «Otra vez» no", listOf(1, 0), listOf(count("btn_timetrial_all_marks"), count("btn_timetrial_again")))
  }

  // ---------------------------------------------------------------- el final, como lo compone MainActivity (Tarea 219, auditoría de Fable §3.2 y §3.3)

  /** Compone el final como `MainActivity`: `canAgain` y la línea de «sin partidas» salen de [TimeTrialDay.canAgainAfter] y [TimeTrialDay.noMoreLine] con la lista [saved] que ya tiene el flujo de Room (puede no traer todavía la partida [just]). */
  private fun mountFinalLikeApp(saved: List<TimeTrialResult>, just: TimeTrialResult, plus: Boolean = false, todayKey: String = today) {
    val outcome = TimeTrialStats.outcome(saved.filter { it.id != just.id } + just, just, 3_000)
    val canAgain = TimeTrialDay.canAgainAfter(plus, just, todayKey, saved)
    val noMore = TimeTrialDay.noMoreLine(just.day, todayKey)
    rule.setContent {
      CompositionLocalProvider(LocalReduceMotion provides true) {
        NeuroVidaTheme { Box(Modifier.width(393.dp).height(1200.dp)) { TimeTrialResultScreen(outcome = outcome, onAgain = { plays++ }, onDone = {}, soundEnabled = false, plus = plus, onAllMarks = if (plus) ({}) else null, canAgain = canAgain, noMoreLine = noMore) } }
      }
    }
    rule.waitForIdle()
  }

  private fun justPlayed(game: String = "stroop", day: String = today) = TimeTrialResult(id = "just", gameId = game, day = day, trackVersion = TimeTrialDay.trackVersion(game), hits = 33, errors = 1, level = 3, seconds = 120, timestamp = 9_000)

  /** El final de la TERCERA partida con la lista vieja (dos guardadas: Room todavía no emitió): se compone UNA vez y no ofrece «Otra vez» ni un instante. */
  @Test fun el_final_de_la_tercera_partida_no_ofrece_Otra_vez_aunque_la_lista_guardada_todavia_no_la_traiga() {
    val just = justPlayed()
    assertFalse("con la lista vieja (2 guardadas) ya no quedan partidas: la recién terminada cuenta", TimeTrialDay.canAgainAfter(false, just, today, played(2)))
    assertEquals("y con la lista nueva (la trae) da lo mismo: sin parpadeo", TimeTrialDay.canAgainAfter(false, just, today, played(2)), TimeTrialDay.canAgainAfter(false, just, today, played(2) + just))
    mountFinalLikeApp(played(2), just)
    assertEquals("ni un cuadro con «Otra vez»", 0, count("btn_timetrial_again"))
    assertEquals("Mañana hay otro Contrarreloj.", textOf("timetrial_tomorrow"))
  }

  @Test fun el_final_de_la_segunda_partida_con_la_lista_vieja_si_ofrece_Otra_vez_y_la_recien_terminada_no_se_cuenta_dos_veces() {
    val just = justPlayed()
    assertTrue("1 guardada + la recién terminada = 2: queda una", TimeTrialDay.canAgainAfter(false, just, today, played(1)))
    assertTrue("si la lista YA trae la recién terminada no se cuenta dos veces (2 guardadas, una es ella)", TimeTrialDay.canAgainAfter(false, just, today, played(1) + just))
    mountFinalLikeApp(played(1), just)
    assertEquals(1, count("btn_timetrial_again")); assertEquals(0, count("timetrial_tomorrow"))
  }

  @Test fun con_Plus_el_final_siempre_ofrece_Otra_vez() {
    assertTrue(TimeTrialDay.canAgainAfter(true, justPlayed(), today, played(5)))
  }

  /** Medianoche: una partida lanzada a las 23:58 (con la pista de AYER) y vista a las 00:01 no dice «Mañana hay otro Contrarreloj.»: lo de hoy ya está disponible. */
  @Test fun el_final_de_una_partida_de_ayer_visto_despues_de_medianoche_dice_Hoy_hay_otro_Contrarreloj() {
    val yesterday = "2026-10-25"
    val game = TimeTrialDay.gameFor(yesterday)
    assertTrue("ayer el Contrarreloj no era el de hoy", game != TimeTrialDay.gameFor(today))
    val just = justPlayed(game = game, day = yesterday)
    assertEquals("Hoy hay otro Contrarreloj.", TimeTrialDay.noMoreLine(just.day, today))
    assertEquals("Mañana hay otro Contrarreloj.", TimeTrialDay.noMoreLine(today, today))
    mountFinalLikeApp(emptyList(), just, todayKey = today)
    assertEquals(0, count("btn_timetrial_again"))
    assertEquals("Hoy hay otro Contrarreloj.", textOf("timetrial_tomorrow"))
    assertTrue("y nunca dice «Mañana»", !textOf("timetrial_tomorrow").contains("Mañana"))
  }

  @Test fun con_Plus_el_final_de_una_partida_de_ayer_conserva_Otra_vez() {
    val yesterday = "2026-10-25"
    mountFinalLikeApp(emptyList(), justPlayed(game = TimeTrialDay.gameFor(yesterday), day = yesterday), plus = true)
    assertEquals(1, count("btn_timetrial_again")); assertEquals(0, count("timetrial_tomorrow"))
  }
}
