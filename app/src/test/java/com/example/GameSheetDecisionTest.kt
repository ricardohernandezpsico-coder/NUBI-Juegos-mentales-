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
import androidx.compose.ui.semantics.LiveRegionMode
import androidx.compose.ui.semantics.Role
import androidx.compose.ui.semantics.SemanticsActions
import androidx.compose.ui.semantics.SemanticsProperties
import androidx.compose.ui.test.SemanticsMatcher
import androidx.compose.ui.test.assert
import androidx.compose.ui.test.assertContentDescriptionEquals
import androidx.compose.ui.test.assertIsDisplayed
import androidx.compose.ui.test.assertIsFocused
import androidx.compose.ui.test.assertIsSelected
import androidx.compose.ui.test.assertTextEquals
import androidx.compose.ui.test.junit4.createComposeRule
import androidx.compose.ui.test.onNodeWithTag
import androidx.compose.ui.test.onNodeWithText
import androidx.compose.ui.test.performClick
import androidx.compose.ui.test.performScrollTo
import androidx.compose.ui.unit.Density
import androidx.compose.ui.unit.dp
import com.example.data.PlayMode
import com.example.model.AgeBand
import com.example.model.GameRegistry
import com.example.ui.screens.GameSheetContent
import com.example.ui.screens.cardData
import com.example.ui.theme.NeuroVidaTheme
import com.github.takahirom.roborazzi.RobolectricDeviceQualifiers
import org.junit.Assert.assertEquals
import org.junit.Assert.assertFalse
import org.junit.Assert.assertTrue
import org.junit.Rule
import org.junit.Test
import org.junit.runner.RunWith
import org.robolectric.RobolectricTestRunner
import org.robolectric.annotation.Config
import org.robolectric.annotation.GraphicsMode

/**
 * La ficha como PORTADA (Tarea 171, 25-oct; antes, la ficha con UNA decisión visible de la Tarea 140): la escena del juego, el nombre y una línea, la etapa con su barra, «Jugar» en sol con lo elegido en su segunda línea, «Desafío» y «Más opciones».
 * «Más opciones» despliega las casillas de siempre, elegir no pliega sola, el foco va a la casilla elegida al desplegar y vuelve al botón al plegar, y el lector de pantalla recibe lo elegido, el estado y la acción del botón y la región viva del título.
 * Lo que se fue de la ficha (la marca con su gráfico y su chip, la constancia, las 5 etiquetas de las etapas, «Te faltan N puntos», «Qué pones en juego» y «Cambiar cómo jugar») no vuelve.
 */
@RunWith(RobolectricTestRunner::class)
@GraphicsMode(GraphicsMode.Mode.NATIVE)
@Config(qualifiers = RobolectricDeviceQualifiers.Pixel8, sdk = [36])
class GameSheetDecisionTest {
  @get:Rule val rule = createComposeRule()

  private val day = 86_400_000L
  private val now = 400 * day

  /** La ficha de [gameId] con el estado que la ventana real le da (la elección vive arriba, en quien la usa). */
  private fun setSheet(
    gameId: String = "radar", widthDp: Int = 393, heightDp: Int = 825, fontScale: Float = 1f, mode: PlayMode = PlayMode.A_TU_MEDIDA, timed: Boolean = true,
    onPlay: () -> Unit = {}, onPlayDesafio: () -> Unit = {}, onAbout: () -> Unit = {}, onClose: () -> Unit = {}
  ) {
    val g = GameRegistry.getById(gameId)!!
    val data = cardData(g, g.title, now - day, now, 0.55f, emptyList(), playsLast14 = 3)
    rule.setContent {
      NeuroVidaTheme {
        CompositionLocalProvider(LocalDensity provides Density(LocalDensity.current.density, fontScale)) {
          var choice by remember { mutableStateOf(mode) }
          var withClock by remember { mutableStateOf(timed) }
          Box(Modifier.width(widthDp.dp).height(heightDp.dp).background(Color(0xFF050823)).testTag("frame").padding(horizontal = 14.dp, vertical = 12.dp), contentAlignment = Alignment.Center) {
            Box(Modifier.fillMaxWidth()) {
              GameSheetContent(
                data, AgeBand.SENIOR, expertOpen = false, choice = choice, onChoose = { choice = it }, onPlay = onPlay, onClose = onClose, timed = withClock, onChooseTimed = { withClock = it },
                onPlayDesafio = onPlayDesafio, onAbout = onAbout
              )
            }
          }
        }
      }
    }
    rule.waitForIdle()
  }

  private fun clickLabelOf(tag: String): String? = rule.onNodeWithTag(tag).fetchSemanticsNode().config[SemanticsActions.OnClick].label

  private fun textsOf(tag: String): List<String> = rule.onNodeWithTag(tag).fetchSemanticsNode().config[SemanticsProperties.Text].map { it.text }

  private fun boundsOf(tag: String) = rule.onNodeWithTag(tag).fetchSemanticsNode().boundsInRoot

  private val sections = listOf("mode_SUAVE", "mode_A_TU_MEDIDA", "mode_DESAFIO", "mode_EXPERTO", "reto_sin", "reto_con")

  @Test
  fun la_ficha_parte_plegada_con_la_portada_el_nombre_la_etapa_y_los_tres_botones_y_las_secciones_ni_se_componen() {
    setSheet()
    val g = GameRegistry.getById("radar")!!
    rule.onNodeWithTag("sheet_cover").assertExists()
    rule.onNodeWithTag("sheet_emblem").assertExists()
    rule.onNodeWithTag("sheet_title").assertTextEquals(g.title)
    rule.onNodeWithTag("sheet_subtitle").assertTextEquals(g.subtitle)
    rule.onNodeWithTag("sheet_stage").assertExists()
    assertEquals(listOf("Jugar", "a tu medida · con reloj"), textsOf("sheet_play"))
    assertEquals(listOf("Desafío"), textsOf("sheet_desafio"))
    assertEquals(listOf("Más opciones"), textsOf("sheet_change"))
    for (tag in sections) rule.onNodeWithTag(tag).assertDoesNotExist()
    rule.onNodeWithText("¿Cómo quieres jugar?").assertDoesNotExist()
    rule.onNodeWithText("¿Con reloj?").assertDoesNotExist()
  }

  @Test
  fun lo_que_se_fue_de_la_ficha_no_esta() {
    setSheet()
    // ni el párrafo (pasó a «Sobre este juego»), ni la marca con su chip, ni la constancia, ni las 5 etiquetas de las etapas, ni «Te faltan», ni la línea de resumen y su botón
    for (tag in listOf("sheet_why", "sheet_summary", "sheet_to_next")) rule.onNodeWithTag(tag).assertDoesNotExist()
    for (text in listOf("Qué pones en juego", "Cambiar cómo jugar", "Tu récord", "Mejorando", "Se mantiene", "Jugaste ayer", "Jugaste hoy", "Listo")) rule.onNodeWithText(text, substring = true).assertDoesNotExist()
    rule.onNodeWithText("Te faltan", substring = true).assertDoesNotExist()
    rule.onNodeWithText("Etapas:", substring = true).assertDoesNotExist()
    rule.onNodeWithTag("stage_names").assertDoesNotExist()
    rule.onNodeWithText("Fuente:", substring = true).assertDoesNotExist()
  }

  @Test
  fun jugar_y_desafio_estan_en_la_primera_pantalla_y_cada_uno_llama_a_lo_suyo() {
    var plays = 0; var desafios = 0
    setSheet(onPlay = { plays++ }, onPlayDesafio = { desafios++ })
    for (tag in listOf("sheet_play", "sheet_desafio", "sheet_change", "sheet_about", "sheet_close")) rule.onNodeWithTag(tag).assertIsDisplayed()
    rule.onNodeWithTag("sheet_desafio").performClick()
    rule.onNodeWithTag("sheet_play").performClick()
    rule.onNodeWithTag("sheet_play").performClick()
    assertEquals(2, plays)
    assertEquals(1, desafios)
  }

  @Test
  fun sobre_este_juego_y_cerrar_llaman_a_lo_suyo_y_el_boton_lleva_el_icono_y_las_palabras() {
    var about = 0; var closes = 0
    setSheet(onAbout = { about++ }, onClose = { closes++ })
    assertEquals(listOf("Sobre este juego"), textsOf("sheet_about"))
    rule.onNodeWithTag("sheet_about").assertContentDescriptionEquals("Sobre este juego: qué pones en juego y su fuente")
    rule.onNodeWithTag("sheet_about").performClick()
    rule.onNodeWithTag("sheet_close").performClick()
    assertEquals(1, about)
    assertEquals(1, closes)
  }

  @Test
  fun los_botones_se_tocan_a_48_dp_o_mas() {
    setSheet()
    val d = rule.density.density
    for (tag in listOf("sheet_play", "sheet_desafio", "sheet_change", "sheet_about", "sheet_close")) {
      val b = boundsOf(tag)
      assertTrue("$tag mide ${b.height / d} dp de alto (mínimo 48)", b.height / d >= 47.5f)
      assertTrue("$tag mide ${b.width / d} dp de ancho (mínimo 48)", b.width / d >= 47.5f)
    }
  }

  @Test
  fun mas_opciones_dice_al_lector_de_pantalla_lo_elegido_su_estado_y_su_accion() {
    setSheet()
    rule.onNodeWithTag("sheet_change")
      .assert(SemanticsMatcher.expectValue(SemanticsProperties.Role, Role.Button))
      .assertContentDescriptionEquals("Más opciones. Ahora: a tu medida, con reloj")
      .assert(SemanticsMatcher.expectValue(SemanticsProperties.StateDescription, "opciones ocultas"))
    assertEquals("Mostrar opciones", clickLabelOf("sheet_change"))
    rule.onNodeWithTag("sheet_play").assertContentDescriptionEquals("Jugar, a tu medida, con reloj")
    rule.onNodeWithTag("sheet_desafio").assertContentDescriptionEquals("Desafío. Un paso más difícil")
    rule.onNodeWithTag("sheet_title").assert(SemanticsMatcher.keyIsDefined(SemanticsProperties.Heading))
  }

  @Test
  fun tocar_mas_opciones_despliega_las_dos_secciones_y_el_mismo_boton_dice_menos_opciones() {
    setSheet()
    rule.onNodeWithTag("sheet_change").performClick()
    rule.waitForIdle()
    rule.onNodeWithTag("sheet_change")
      .assertTextEquals("Menos opciones")
      .assertContentDescriptionEquals("Menos opciones. Ahora: a tu medida, con reloj")
      .assert(SemanticsMatcher.expectValue(SemanticsProperties.StateDescription, "opciones visibles"))
    assertEquals("Ocultar opciones", clickLabelOf("sheet_change"))
    for (tag in sections) rule.onNodeWithTag(tag).assertExists()
    rule.onNodeWithTag("mode_A_TU_MEDIDA").assertIsSelected()
    rule.onNodeWithTag("reto_con").assertIsSelected()
    // el título de la sección es una región viva: el lector anuncia que aparecieron las opciones
    rule.onNodeWithText("¿Cómo quieres jugar?").assert(SemanticsMatcher.expectValue(SemanticsProperties.LiveRegion, LiveRegionMode.Polite))
    // «Desafío» y «Jugar» siguen donde estaban, con las opciones abiertas
    rule.onNodeWithTag("sheet_desafio").assertExists()
    rule.onNodeWithTag("sheet_play").assertExists()
  }

  @Test
  fun elegir_un_modo_o_el_reloj_no_pliega_sola_y_jugar_lo_dice_hasta_que_la_persona_toca_menos_opciones() {
    setSheet()
    rule.onNodeWithTag("sheet_change").performClick()
    rule.onNodeWithTag("mode_DESAFIO").performClick()
    rule.waitForIdle()
    rule.onNodeWithTag("mode_DESAFIO").assertIsSelected()
    for (tag in sections) rule.onNodeWithTag(tag).assertExists()                       // no se plegó sola
    rule.onNodeWithTag("sheet_play").performScrollTo()
    assertEquals(listOf("Jugar", "en Desafío · con reloj"), textsOf("sheet_play"))
    rule.onNodeWithTag("sheet_play").assertContentDescriptionEquals("Jugar, en Desafío, con reloj")
    rule.onNodeWithTag("reto_sin").performScrollTo().performClick()
    rule.waitForIdle()
    rule.onNodeWithTag("reto_sin").assertIsSelected()
    for (tag in sections) rule.onNodeWithTag(tag).assertExists()
    assertEquals(listOf("Jugar", "en Desafío · sin reloj"), textsOf("sheet_play"))
    rule.onNodeWithTag("sheet_play").assertContentDescriptionEquals("Jugar, en Desafío, sin reloj")
    rule.onNodeWithTag("sheet_change").performScrollTo().performClick()               // «Menos opciones»: ahora sí pliega
    rule.waitForIdle()
    for (tag in sections) rule.onNodeWithTag(tag).assertDoesNotExist()
    rule.onNodeWithTag("sheet_change").assertTextEquals("Más opciones")
    assertEquals("y lo elegido se queda", listOf("Jugar", "en Desafío · sin reloj"), textsOf("sheet_play"))
  }

  /** El foco. Un `clickable` de Compose solo acepta el foco con teclado o mando, no en modo táctil: la casilla y el botón llevan `focusable()` para que el foco (y con él el lector de pantalla) llegue a donde la ficha lo manda. */
  @Test
  fun al_desplegar_el_foco_va_a_la_casilla_elegida_y_al_plegar_vuelve_al_boton() {
    setSheet(mode = PlayMode.DESAFIO)
    rule.onNodeWithTag("sheet_change").performClick()
    rule.waitForIdle()
    rule.onNodeWithTag("mode_DESAFIO").assertIsFocused()
    rule.onNodeWithTag("sheet_change").performScrollTo().performClick()
    rule.waitForIdle()
    rule.onNodeWithTag("sheet_change").assertIsFocused()
  }

  /** Ricardo (19-oct): la ficha plegada «un poco más arriba, no tan abajo»: va CENTRADA en el espacio libre entre las barras; desplegada, crece hacia arriba y hacia abajo. */
  @Test
  fun la_tarjeta_plegada_va_centrada_y_al_desplegar_crece_hacia_arriba_y_hacia_abajo() {
    setSheet(heightDp = 1400)                                         // un marco alto: sobra lugar arriba y abajo
    val tolerance = 10f * rule.density.density                        // la losa reserva 6 dp abajo para su sombra: «centrada» con unos dp de margen
    val frame = boundsOf("frame")
    val folded = boundsOf("game_sheet")
    assertTrue("plegada: sobra lugar arriba (${folded.top - frame.top}) y es parecido al de abajo (${frame.bottom - folded.bottom})", folded.top - frame.top > 100f * rule.density.density && Math.abs((folded.top - frame.top) - (frame.bottom - folded.bottom)) <= tolerance)
    rule.onNodeWithTag("sheet_change").performClick()
    rule.waitForIdle()
    val open = boundsOf("game_sheet")
    assertTrue("desplegada: crece hacia arriba (arriba ${folded.top} → ${open.top}) y hacia abajo (abajo ${folded.bottom} → ${open.bottom})", open.top < folded.top - 1f && open.bottom > folded.bottom + 1f)
    assertTrue("y sigue centrada", Math.abs((open.top - frame.top) - (frame.bottom - open.bottom)) <= tolerance)
  }

  /** El cambio de tamaño es suave: 200 ms (NubiMotion, cambio de estado) y de un salto con «quitar animaciones». */
  @Test
  fun las_secciones_cambian_de_altura_con_nubi_motion_y_de_un_salto_con_quitar_animaciones() {
    val source = java.io.File("src/main/java/com/example/ui/screens/GameSheet.kt").readText()
    val sheet = source.substring(source.indexOf("internal fun GameSheetContent("), source.indexOf("internal fun GameAboutContent("))
    assertTrue("entra y sale cambiando de altura", sheet.contains("expandVertically(") && sheet.contains("shrinkVertically("))
    assertTrue("con la duración de NubiMotion (cambio de estado, 200 ms)", sheet.contains("tween(NubiMotion.STATE_MS, easing = NubiMotion.Standard)"))
    assertTrue("y sin animación con «quitar animaciones»", sheet.contains("if (reduceMotion) EnterTransition.None") && sheet.contains("if (reduceMotion) ExitTransition.None"))
    assertEquals(200, com.example.ui.components.NubiMotion.STATE_MS)
  }

  /** «Un solo sol a la vez»: el único botón sol de la ficha es «Jugar»; «Desafío» es lila y «Más opciones» crema. Lo informativo es plano. */
  @Test
  fun un_solo_sol_en_la_ficha_el_de_jugar_y_el_desafio_es_lila_con_el_cometa() {
    val source = java.io.File("src/main/java/com/example/ui/screens/GameSheet.kt").readText()
    val sheet = source.substring(source.indexOf("internal fun GameSheetContent("), source.indexOf("internal fun GameAboutContent("))
    assertEquals("el único SlabColor.SUN de la ficha es el de «Jugar» (el sol por defecto de KitButton)", 0, Regex("SlabColor\\.SUN").findAll(sheet).count())
    assertTrue("«Jugar» no cambia el color sol por defecto", sheet.contains("text = \"Jugar\", secondLine"))
    assertTrue("«Desafío» va lila con el cometa", sheet.contains("text = \"Desafío\"") && sheet.contains("color = SlabColor.LILAC, glyph = Glyph.COMET"))
    assertTrue("«Más opciones» va crema", sheet.contains("color = SlabColor.CREAM, glyph = if (opciones) Glyph.UP else Glyph.DOWN"))
  }

  @Test
  fun jugar_dice_lo_elegido_en_su_segunda_linea_y_cada_texto_va_entero() {
    setSheet()
    assertEquals(listOf("Jugar", "a tu medida · con reloj"), textsOf("sheet_play"))
    rule.onNodeWithTag("sheet_change").performClick()
    rule.waitForIdle()
    rule.onNodeWithTag("sheet_play").performScrollTo()
    assertEquals("desplegada, lo mismo (el botón no cambia de forma)", listOf("Jugar", "a tu medida · con reloj"), textsOf("sheet_play"))
  }

  @Test
  fun con_letra_130_la_ficha_sigue_con_el_mismo_orden_y_los_dos_botones_de_abajo_a_lo_sumo_apilados() {
    setSheet(fontScale = 1.3f)
    assertEquals(listOf("Jugar", "a tu medida · con reloj"), textsOf("sheet_play"))
    val d = boundsOf("sheet_desafio"); val m = boundsOf("sheet_change")
    assertTrue("«Desafío» y «Más opciones» cada uno entero dentro de la tarjeta", d.left >= boundsOf("game_sheet").left && m.right <= boundsOf("game_sheet").right)
    assertTrue("o van lado a lado (mismo alto) o apilados (sin cruzarse)", d.right <= m.left + 1f || d.bottom <= m.top + 1f)
  }

  @Test
  fun con_letra_200_el_nombre_baja_debajo_del_emblema_y_los_dos_botones_se_apilan() {
    setSheet(fontScale = 2f, timed = false, mode = PlayMode.SUAVE)
    rule.onNodeWithTag("sheet_title").assertIsDisplayed()
    val emblem = boundsOf("sheet_emblem"); val title = boundsOf("sheet_title")
    assertTrue("el nombre no pisa el emblema (nombre desde ${title.top}, emblema hasta ${emblem.bottom})", title.top >= emblem.bottom - 1f)
    assertEquals(listOf("Jugar", "suave · sin reloj"), textsOf("sheet_play"))
    rule.onNodeWithTag("sheet_desafio").performScrollTo()
    val d = boundsOf("sheet_desafio"); val m = boundsOf("sheet_change")
    assertTrue("apilados: «Más opciones» debajo de «Desafío» (${d.bottom} y ${m.top})", m.top >= d.bottom - 1f && Math.abs(d.left - m.left) < 2f)
  }

  /** Lo que asoma bajo la ventanilla (el emblema a la izquierda y «Sobre este juego» a la derecha) no se toca entre sí, no se sale de la ventanilla por los lados y el nombre va debajo de los dos. */
  private fun assertPeekingPiecesAreClear(label: String) {
    val cover = boundsOf("sheet_cover"); val emblem = boundsOf("sheet_emblem"); val about = boundsOf("sheet_about"); val title = boundsOf("sheet_title")
    assertTrue("$label: el emblema asoma bajo el borde de la ventanilla (borde ${cover.bottom}, emblema ${emblem.top} a ${emblem.bottom})", emblem.top < cover.bottom && emblem.bottom > cover.bottom)
    assertTrue("$label: «Sobre este juego» asoma bajo el mismo borde (borde ${cover.bottom}, botón ${about.top} a ${about.bottom})", about.top < cover.bottom && about.bottom > cover.bottom)
    assertTrue("$label: emblema a la izquierda y «Sobre este juego» a la derecha, sin tocarse (${emblem.right} contra ${about.left})", emblem.right <= about.left)
    assertTrue("$label: «Sobre este juego» no pasa del borde derecho de la ventanilla (${about.right} contra ${cover.right})", about.right <= cover.right)
    assertTrue("$label: el nombre va debajo del emblema (${title.top} contra ${emblem.bottom}) y de «Sobre este juego» (${about.bottom})", title.top >= emblem.bottom - 1f && title.top >= about.bottom - 1f)
  }

  @Test
  fun el_emblema_y_sobre_este_juego_asoman_bajo_la_ventanilla_y_el_nombre_va_debajo_de_los_dos() {
    setSheet()
    assertPeekingPiecesAreClear("letra 1,0")
    val cover = boundsOf("sheet_cover"); val emblem = boundsOf("sheet_emblem"); val close = boundsOf("sheet_close")
    val d = rule.density.density
    assertTrue("el emblema tiene 56 dp", Math.abs(emblem.width / d - 56f) < 1f)
    assertTrue("la X queda dentro de la ventanilla, arriba a la derecha", close.top >= cover.top && close.right <= cover.right && close.bottom < cover.top + cover.height / 2)
    assertTrue("la ventanilla mide 150 dp (${cover.height / d})", Math.abs(cover.height / d - 150f) < 1f)
  }

  @Test
  fun con_letra_130_lo_que_asoma_no_se_pisa() {
    setSheet(fontScale = 1.3f)
    assertPeekingPiecesAreClear("letra 1,3")
  }

  @Test
  fun con_letra_200_lo_que_asoma_no_se_pisa_y_sobre_este_juego_puede_pasar_a_dos_lineas() {
    setSheet(fontScale = 2f)
    assertPeekingPiecesAreClear("letra 2,0")
    assertEquals(listOf("Sobre este juego"), textsOf("sheet_about"))                // un solo texto: si no cabe en una línea, pasa a dos entre palabras, nunca se parte una
  }

  /** «segundos» se partía en «segund / os» en la casilla de «Con reloj» de Piloto con letra 1,3 (y en teléfonos de 360 dp): las dos casillas del reloj van lado a lado solo si CADA palabra cabe en su media fila; si no, de a una. */
  @Test
  fun las_casillas_del_reloj_van_de_a_una_si_una_palabra_no_cabe_en_su_media_fila_y_lado_a_lado_si_caben() {
    setSheet(gameId = "piloto", fontScale = 1.3f)                                     // «1 minuto y 30 segundos»
    rule.onNodeWithTag("sheet_change").performClick()
    rule.waitForIdle()
    val sin = boundsOf("reto_sin"); val con = boundsOf("reto_con")
    assertTrue("«segundos» no cabe en media fila con letra 1,3: una casilla debajo de la otra (${sin.bottom} y ${con.top})", con.top >= sin.bottom - 1f)
    assertEquals("y cada una ocupa todo el ancho", sin.width, con.width, 1f)
  }

  @Test
  fun las_casillas_del_reloj_de_rescate_van_lado_a_lado_con_letra_100() {
    setSheet(gameId = "radar")                                                         // «2 minutos»
    rule.onNodeWithTag("sheet_change").performClick()
    rule.waitForIdle()
    val sin = boundsOf("reto_sin"); val con = boundsOf("reto_con")
    assertTrue("lado a lado: mismo alto de arriba (${sin.top} y ${con.top}) y una a la derecha de la otra (${sin.right} y ${con.left})", Math.abs(sin.top - con.top) < 2f && con.left >= sin.right - 1f)
  }

  @Test
  fun un_juego_sin_reto_dice_solo_a_tu_medida_y_no_tiene_la_seccion_del_reloj() {
    setSheet(gameId = "correo", timed = true)
    assertEquals("«Jugar» y debajo «a tu medida» a secas", listOf("Jugar", "a tu medida"), textsOf("sheet_play"))
    rule.onNodeWithTag("sheet_change").assertContentDescriptionEquals("Más opciones. Ahora: a tu medida")
    rule.onNodeWithTag("sheet_play").assertContentDescriptionEquals("Jugar, a tu medida")
    rule.onNodeWithTag("sheet_change").performClick()
    rule.waitForIdle()
    assertEquals(listOf("Jugar", "a tu medida"), textsOf("sheet_play"))
    rule.onNodeWithTag("mode_A_TU_MEDIDA").assertExists()
    rule.onNodeWithTag("mode_EXPERTO").assertExists()
    rule.onNodeWithTag("reto_sin").assertDoesNotExist()
    rule.onNodeWithTag("reto_con").assertDoesNotExist()
    rule.onNodeWithText("¿Con reloj?").assertDoesNotExist()
  }

  @Test
  fun al_limite_sigue_con_su_candado_hasta_superar_un_desafio() {
    setSheet()
    rule.onNodeWithTag("sheet_change").performClick()
    rule.waitForIdle()
    rule.onNodeWithTag("mode_EXPERTO").performScrollTo().assertIsDisplayed()
    rule.onNodeWithTag("mode_EXPERTO").assert(SemanticsMatcher.keyIsDefined(SemanticsProperties.Disabled))
    rule.onNodeWithTag("mode_EXPERTO").assertContentDescriptionEquals("Al límite, bloqueado: supera un Desafío")
  }

  @Test
  fun una_ficha_sin_medir_dice_aun_sin_medir_y_sigue_con_los_tres_botones() {
    val g = GameRegistry.getById("stroop")!!
    val data = cardData(g, g.title, null, now, null, emptyList(), playsLast14 = 0)
    rule.setContent {
      NeuroVidaTheme {
        Box(Modifier.width(393.dp).height(825.dp).background(Color(0xFF050823)).padding(horizontal = 14.dp, vertical = 12.dp), contentAlignment = Alignment.Center) {
          GameSheetContent(data, AgeBand.SENIOR, expertOpen = false, choice = PlayMode.A_TU_MEDIDA, onChoose = {}, onPlay = {}, timed = false, onChooseTimed = {})
        }
      }
    }
    rule.waitForIdle()
    rule.onNodeWithTag("sheet_stage").assertTextEquals("Aún sin medir")
    for (tag in listOf("sheet_play", "sheet_desafio", "sheet_change")) rule.onNodeWithTag(tag).assertIsDisplayed()
  }
}
