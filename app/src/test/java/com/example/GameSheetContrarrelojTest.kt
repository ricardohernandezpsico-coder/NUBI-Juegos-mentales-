package com.example

import androidx.compose.foundation.background
import androidx.compose.foundation.layout.Box
import androidx.compose.foundation.layout.fillMaxWidth
import androidx.compose.foundation.layout.height
import androidx.compose.foundation.layout.padding
import androidx.compose.foundation.layout.width
import androidx.compose.runtime.CompositionLocalProvider
import androidx.compose.runtime.getValue
import androidx.compose.runtime.key
import androidx.compose.runtime.mutableStateOf
import androidx.compose.runtime.remember
import androidx.compose.runtime.setValue
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.graphics.Color
import androidx.compose.ui.platform.LocalDensity
import androidx.compose.ui.platform.testTag
import androidx.compose.ui.semantics.SemanticsProperties
import androidx.compose.ui.test.SemanticsMatcher
import androidx.compose.ui.test.assertContentDescriptionEquals
import androidx.compose.ui.test.assertIsDisplayed
import androidx.compose.ui.test.junit4.createComposeRule
import androidx.compose.ui.test.onNodeWithTag
import androidx.compose.ui.test.onNodeWithText
import androidx.compose.ui.test.performClick
import androidx.compose.ui.test.performScrollTo
import androidx.compose.ui.unit.Density
import androidx.compose.ui.unit.dp
import com.example.data.PlayMode
import com.example.data.RetoChoice
import com.example.data.TimeTrialDay
import com.example.model.AgeBand
import com.example.model.GameRegistry
import com.example.ui.components.SlabColor
import com.example.ui.screens.GameSheetContent
import com.example.ui.screens.SheetTimeTrial
import com.example.ui.screens.cardData
import com.example.ui.screens.sheetTimeTrialOf
import com.example.ui.theme.NeuroVidaTheme
import com.github.takahirom.roborazzi.RobolectricDeviceQualifiers
import org.junit.Assert.assertEquals
import org.junit.Assert.assertFalse
import org.junit.Assert.assertNotEquals
import org.junit.Assert.assertNotNull
import org.junit.Assert.assertNull
import org.junit.Assert.assertTrue
import org.junit.Rule
import org.junit.Test
import org.junit.runner.RunWith
import org.robolectric.RobolectricTestRunner
import org.robolectric.annotation.Config
import org.robolectric.annotation.GraphicsMode

/**
 * El Contrarreloj en la ficha (Tarea 181, 26-oct; `docs/contrarreloj.md` fase 1b): en Tinta o Palabra, ¿Verdad o disparate?, Rescate relámpago y Acoplamiento la ficha lleva un botón «Contrarreloj» a todo el ancho BAJO «Desafío | Más opciones» (el reloj de plasticina, el nombre y «Tu récord: N aciertos.» o «2 minutos») y ya no tiene
 * «¿Con reloj?» ni «· sin reloj» en «Jugar»; los otros 13 juegos quedan IGUAL. También MIDE cuánto se pasa la ficha del marco (`tarea_181_medidas`): la regla de la tarea fue medir e informar, no achicar.
 */
@RunWith(RobolectricTestRunner::class)
@GraphicsMode(GraphicsMode.Mode.NATIVE)
@Config(qualifiers = RobolectricDeviceQualifiers.Pixel8, sdk = [36])
class GameSheetContrarrelojTest {
  @get:Rule val rule = createComposeRule()

  private val day = 86_400_000L
  private val now = 400 * day
  private val today = "2026-10-26"                       // ese día el Contrarreloj de hoy es el de Tinta o Palabra

  private val four = listOf("stroop", "disparate", "radar", "acoplamiento")
  private val others = GameRegistry.allGames.map { it.id }.filter { it !in four }

  /** Lo que se pone en la ficha: el juego, su récord (null = ninguno), el tamaño del marco (el alto es el que dejan las barras del sistema en 393 × 873), la letra y el color del botón. [nonce] reinicia lo recordado (lo desplegado, lo elegido). */
  private data class Cfg(val gameId: String, val record: Int? = null, val width: Int = 393, val height: Int = 873 - 72, val fontScale: Float = 1f, val timed: Boolean = true, val color: SlabColor = SlabColor.LILAC, val nonce: Int = 0)

  private var cfg by mutableStateOf(Cfg("stroop"))
  private var plays = 0
  private var desafios = 0
  private var trials = 0

  private fun mount(c: Cfg) {
    cfg = c
    rule.setContent {
      val c1 = cfg
      key(c1.nonce) {
        CompositionLocalProvider(LocalDensity provides Density(LocalDensity.current.density, c1.fontScale)) {
          NeuroVidaTheme {
            var choice by remember { mutableStateOf(PlayMode.A_TU_MEDIDA) }
            var withClock by remember { mutableStateOf(c1.timed) }
            val g = GameRegistry.getById(c1.gameId)!!
            val data = cardData(g, g.title, now - day, now, 0.55f, emptyList(), playsLast14 = 3)
            Box(Modifier.width(c1.width.dp).height(c1.height.dp).background(Color(0xFF050823)).testTag("frame").padding(horizontal = 14.dp, vertical = 12.dp), contentAlignment = Alignment.Center) {
              Box(Modifier.fillMaxWidth()) {
                GameSheetContent(
                  data, AgeBand.ADULT, expertOpen = false, choice = choice, onChoose = { choice = it }, onPlay = { plays++ }, onClose = {}, timed = withClock, onChooseTimed = { withClock = it },
                  onPlayDesafio = { desafios++ }, onAbout = {}, timeTrial = sheetTimeTrialOf(c1.gameId, g.title, c1.record, today, c1.color), onTimeTrial = { trials++ }
                )
              }
            }
          }
        }
      }
    }
    rule.waitForIdle()
  }

  private fun show(c: Cfg) { cfg = c.copy(nonce = cfg.nonce + 1); rule.waitForIdle() }

  private fun textsOf(tag: String): List<String> = rule.onNodeWithTag(tag).fetchSemanticsNode().config[SemanticsProperties.Text].map { it.text }
  private fun boundsOf(tag: String) = rule.onNodeWithTag(tag).fetchSemanticsNode().boundsInRoot
  private fun has(tag: String) = rule.onAllNodes(androidx.compose.ui.test.hasTestTag(tag)).fetchSemanticsNodes().isNotEmpty()
  private fun unfold() { rule.onNodeWithTag("sheet_change").performScrollTo().performClick(); rule.waitForIdle() }

  /** Cuánto se pasa la ficha del marco: lo que la tarjeta se desliza por dentro (dp); 0 = cabe entera sin deslizar. */
  private fun overflowDp(): Float =
    rule.onAllNodes(SemanticsMatcher.keyIsDefined(SemanticsProperties.VerticalScrollAxisRange)).fetchSemanticsNodes().maxOf { it.config[SemanticsProperties.VerticalScrollAxisRange].maxValue() } / rule.density.density

  // ------------------------------------------------------------------ los cuatro: botón sí, «¿Con reloj?» no

  private fun assertHasTimeTrialButtonAndNoClockChoice(id: String, record: Int?) {
    mount(Cfg(id, record, timed = true))
    val g = GameRegistry.getById(id)!!
    assertTrue("$id: el botón «Contrarreloj» está", has("sheet_timetrial"))
    assertEquals("$id: el reloj de plasticina, el nombre y la segunda línea", listOf("Contrarreloj", TimeTrialDay.sheetSecondLine(record)), textsOf("sheet_timetrial"))
    assertEquals("$id: «Jugar» ya no dice «sin reloj» ni «con reloj»", listOf("Jugar", "a tu medida"), textsOf("sheet_play"))
    rule.onNodeWithTag("sheet_play").assertContentDescriptionEquals("Jugar, a tu medida")
    rule.onNodeWithTag("sheet_change").assertContentDescriptionEquals("Más opciones. Ahora: a tu medida")
    unfold()
    for (tag in listOf("mode_SUAVE", "mode_A_TU_MEDIDA", "mode_DESAFIO", "mode_EXPERTO")) assertTrue("$id: desplegada tiene «$tag»", has(tag))
    assertFalse("$id: ni la casilla «Sin reloj»", has("reto_sin"))
    assertFalse("$id: ni la casilla «Con reloj»", has("reto_con"))
    rule.onNodeWithText("¿Con reloj?").assertDoesNotExist()
    assertEquals("$id: desplegada, «Jugar» sigue igual", listOf("Jugar", "a tu medida"), textsOf("sheet_play"))
    assertEquals(g.title, textsOf("sheet_title").single())
  }

  @Test fun tinta_o_palabra_tiene_el_boton_y_no_tiene_con_reloj() = assertHasTimeTrialButtonAndNoClockChoice("stroop", null)
  @Test fun verdad_o_disparate_tiene_el_boton_y_no_tiene_con_reloj() = assertHasTimeTrialButtonAndNoClockChoice("disparate", 33)
  @Test fun rescate_relampago_tiene_el_boton_y_no_tiene_con_reloj() = assertHasTimeTrialButtonAndNoClockChoice("radar", 12)
  @Test fun acoplamiento_tiene_el_boton_y_no_tiene_con_reloj() = assertHasTimeTrialButtonAndNoClockChoice("acoplamiento", 1)

  @Test
  fun un_con_reloj_guardado_en_uno_de_los_cuatro_no_asoma_en_la_ficha() {
    // aunque llegue timed = true (lo guardado de antes), la ficha no lo dice: la segunda línea de «Jugar» y el lector de pantalla no hablan del reloj
    mount(Cfg("radar", timed = true))
    assertEquals(listOf("Jugar", "a tu medida"), textsOf("sheet_play"))
    rule.onNodeWithTag("sheet_desafio").assertContentDescriptionEquals("Desafío. Un paso más difícil")
    assertFalse(RetoChoice.supports("radar"))
  }

  // ------------------------------------------------------------------ los otros 13 quedan IGUAL

  @Test
  fun los_otros_13_juegos_no_tienen_el_boton_y_conservan_con_reloj_menos_correo_que_nunca_lo_tuvo() {
    assertEquals(13, others.size)
    mount(Cfg("freno"))
    for (id in others) {
      show(Cfg(id, timed = true))
      val supports = RetoChoice.supports(id)
      assertFalse("$id: sin botón «Contrarreloj»", has("sheet_timetrial"))
      assertEquals("$id: «Jugar» dice lo elegido como siempre", listOf("Jugar", if (supports) "a tu medida · con reloj" else "a tu medida"), textsOf("sheet_play"))
      unfold()
      assertEquals("$id: «¿Con reloj?» solo donde ya estaba (todos menos Correo)", id != "correo", has("reto_sin") && has("reto_con"))
      if (supports) rule.onNodeWithText("¿Con reloj?").assertExists()
    }
    assertEquals("12 con «¿Con reloj?»", 12, others.count { RetoChoice.supports(it) })
  }

  // ------------------------------------------------------------------ el botón: dónde está, cómo se toca, qué dice

  @Test
  fun el_boton_va_a_todo_el_ancho_bajo_la_fila_de_desafio_y_mas_opciones_y_se_toca_a_64_dp_o_mas() {
    mount(Cfg("stroop", record = 41))
    val d = rule.density.density
    val play = boundsOf("sheet_play"); val desafio = boundsOf("sheet_desafio"); val more = boundsOf("sheet_change"); val tt = boundsOf("sheet_timetrial")
    assertTrue("bajo «Desafío» (${desafio.bottom}) y «Más opciones» (${more.bottom}): ${tt.top}", tt.top >= maxOf(desafio.bottom, more.bottom))
    assertEquals("a todo el ancho, como «Jugar»", play.width, tt.width, 1f)
    assertEquals("y alineado con «Jugar»", play.left, tt.left, 1f)
    assertTrue("mide ${tt.height / d} dp de alto (mínimo 64)", tt.height / d >= 63.5f)
    rule.onNodeWithTag("sheet_timetrial").assertIsDisplayed()
    rule.onNodeWithTag("sheet_timetrial").performClick()
    rule.onNodeWithTag("sheet_play").performClick()
    rule.onNodeWithTag("sheet_desafio").performClick()
    assertEquals("«Contrarreloj» llama a lo suyo", 1, trials)
    assertEquals("y «Jugar» y «Desafío» no lo lanzan", 1, plays)
    assertEquals(1, desafios)
  }

  @Test
  fun la_segunda_linea_dice_el_record_de_ese_juego_o_dos_minutos() {
    mount(Cfg("stroop"))
    assertEquals(listOf("Contrarreloj", "2 minutos"), textsOf("sheet_timetrial"))
    show(Cfg("stroop", record = 41))
    assertEquals(listOf("Contrarreloj", "Tu récord: 41 aciertos."), textsOf("sheet_timetrial"))
    show(Cfg("radar", record = 1))
    assertEquals(listOf("Contrarreloj", "Tu récord: 1 acierto."), textsOf("sheet_timetrial"))
    show(Cfg("acoplamiento"))
    assertEquals("otro juego, otro récord: sin récord dice «2 minutos»", listOf("Contrarreloj", "2 minutos"), textsOf("sheet_timetrial"))
  }

  @Test
  fun el_lector_de_pantalla_dice_el_juego_los_dos_minutos_y_el_record_y_de_hoy_solo_si_es_el_del_dia() {
    mount(Cfg("stroop", record = 41))
    rule.onNodeWithTag("sheet_timetrial").assertContentDescriptionEquals("Contrarreloj de hoy, ${GameRegistry.getById("stroop")!!.title}, 2 minutos, tu récord 41 aciertos")
    show(Cfg("stroop"))
    rule.onNodeWithTag("sheet_timetrial").assertContentDescriptionEquals("Contrarreloj de hoy, ${GameRegistry.getById("stroop")!!.title}, 2 minutos")
    show(Cfg("radar", record = 12))
    rule.onNodeWithTag("sheet_timetrial").assertContentDescriptionEquals("Contrarreloj, ${GameRegistry.getById("radar")!!.title}, 2 minutos, tu récord 12 aciertos")
    show(Cfg("disparate"))
    rule.onNodeWithTag("sheet_timetrial").assertContentDescriptionEquals("Contrarreloj, ${GameRegistry.getById("disparate")!!.title}, 2 minutos")
  }

  @Test
  fun el_boton_nunca_es_sol_y_va_en_lila_o_en_crema() {
    val tt = sheetTimeTrialOf("stroop", "Tinta o Palabra", null, today)
    assertNotNull(tt)
    assertEquals("por defecto, el lila del Contrarreloj de Hoy", SlabColor.LILAC, tt!!.color)
    assertNotEquals("el único sol de la ficha es «Jugar»", SlabColor.SUN, tt.color)
    assertNull("un juego sin Contrarreloj no tiene botón", sheetTimeTrialOf("freno", "Freno de Emergencia", null, today))
    assertNull(sheetTimeTrialOf("correo", "Correo Estelar", 3, today))
    assertEquals(SheetTimeTrial("2 minutos", "Contrarreloj de hoy, Tinta o Palabra, 2 minutos", SlabColor.CREAM), sheetTimeTrialOf("stroop", "Tinta o Palabra", null, today, SlabColor.CREAM))
    val source = java.io.File("src/main/java/com/example/ui/screens/GameSheet.kt").readText()
    val button = source.substring(source.indexOf("text = TimeTrialDay.BUTTON_LABEL"), source.indexOf("// Las dos secciones solo se componen"))
    assertFalse("el botón no usa SlabColor.SUN", button.contains("SlabColor.SUN"))
    assertTrue("lleva el reloj de plasticina", button.contains("ClayClockIcon()"))
  }

  @Test
  fun desplegada_el_boton_sigue_entre_la_fila_y_las_secciones() {
    mount(Cfg("stroop", record = 41))
    unfold()
    val more = boundsOf("sheet_change"); val tt = boundsOf("sheet_timetrial"); val modes = boundsOf("mode_SUAVE")
    assertTrue("el botón queda bajo «Menos opciones» (${more.bottom}) y sobre los modos (${modes.top}): ${tt.top}-${tt.bottom}", tt.top >= more.bottom && tt.bottom <= modes.top)
    assertEquals("«Menos opciones» dice lo suyo", listOf("Menos opciones"), textsOf("sheet_change"))
  }

  @Test
  fun con_letra_130_y_200_ninguna_palabra_del_boton_se_parte_y_el_boton_se_toca_entero() {
    for (fs in listOf(1.3f, 2f)) {
      if (fs == 1.3f) mount(Cfg("disparate", record = 33, fontScale = fs)) else show(Cfg("disparate", record = 33, fontScale = fs))
      rule.onNodeWithTag("sheet_timetrial").performScrollTo()
      val card = boundsOf("game_sheet"); val tt = boundsOf("sheet_timetrial")
      assertTrue("letra $fs: el botón queda dentro de la tarjeta (${tt.left}..${tt.right} en ${card.left}..${card.right})", tt.left >= card.left && tt.right <= card.right)
      assertTrue("letra $fs: ≥ 48 dp de alto (${tt.height / rule.density.density})", tt.height / rule.density.density >= 47.5f)
      assertEquals("letra $fs: sus dos textos, cada uno entero", listOf("Contrarreloj", "Tu récord: 33 aciertos."), textsOf("sheet_timetrial"))
    }
  }

  // ------------------------------------------------------------------ cuánto se pasa la ficha del marco (solo MIDE; la regla de la tarea: informar, no achicar)

  /** Cuánto sobra de alto en el marco con la ficha plegada (dp; las barras del sistema y el margen de la ventana ya descontados): solo tiene sentido cuando cabe entera. */
  private fun slackDp(): Float {
    val frame = boundsOf("frame"); val card = boundsOf("game_sheet")
    return (frame.height - card.height) / rule.density.density - 24f
  }

  /** Lo medido de una ficha: lo que se pasa plegada (0 = cabe), lo que sobra si cabe y lo que se pasa desplegada (dp). */
  private data class Fit(val folded: Float, val slack: Float, val unfolded: Float)

  private fun measure(c: Cfg): Fit {
    show(c)
    val folded = overflowDp()
    val slack = slackDp()
    unfold()
    return Fit(folded, slack, overflowDp())
  }

  private val labels = listOf(Triple("antes (Freno)", "freno", null as Int?), Triple("Tinta sin récord", "stroop", null), Triple("Tinta con récord", "stroop", 41), Triple("Disparate con récord", "disparate", 33), Triple("Rescate con récord", "radar", 12), Triple("Acoplamiento sin récord", "acoplamiento", null))

  /**
   * Tarea 181: cuánto se pasa la ficha del marco de la ventana real (dp; 0 = cabe entera sin deslizar), a 393 × 873 y a 360 × 780 (menos las barras del sistema), con letra 1,0, 1,3 y 2,0, plegada y desplegada. «Antes» = un juego con la misma ficha de antes (Freno: con su «¿Con reloj?» y sin el botón), para compararla con los
   * cuatro de ahora. ¿Verdad o disparate? y Rescate relámpago tienen los nombres más largos; Tinta o Palabra, el más corto. Solo MIDE e informa (la regla de la tarea: nada se achica sin el OK de Opus) y fija lo medido para que una regresión se note: a 393 × 873 la ficha plegada cabe entera con letra 1,0 y 1,3
   * (los estados de todos los días), a 360 × 780 con 1,0, y con 1,3 se pasa 43 dp; con 2,0 se desliza (la tarjeta hace scroll por dentro, nada se corta).
   */
  @Test
  fun tarea_181_medidas() {
    mount(Cfg("freno"))
    val rows = mutableListOf<String>()
    for ((w, h) in listOf(393 to 873 - 72, 360 to 780 - 72)) for (fs in listOf(1f, 1.3f, 2f)) {
      val cells = mutableListOf<String>()
      for ((label, id, record) in labels) {
        val fit = measure(Cfg(id, record, w, h, fs))
        cells += "%s plegada %s desplegada %+.0f".format(label, if (fit.folded < 0.5f) "cabe (sobran %.0f)".format(fit.slack) else "+%.0f".format(fit.folded), fit.unfolded)
        val where = "$label a ${w}x${h + 72} letra $fs"
        if (id !in four) continue
        val tolerance = when { w == 393 && fs <= 1.3f -> 0.5f; w == 360 && fs == 1f -> 0.5f; w == 360 && fs == 1.3f -> 44f; else -> 1000f }            // lo medido; con 2,0 se desliza y no se fija
        assertTrue("$where: plegada se pasa ${fit.folded} dp (tolerancia $tolerance)", fit.folded <= tolerance)
      }
      rows += "MEDIDA %dx%d letra %.1f: %s".format(w, h + 72, fs, cells.joinToString(" | "))
    }
    rows.forEach { println(it) }
  }
}
