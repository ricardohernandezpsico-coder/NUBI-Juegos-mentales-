package com.example

import androidx.compose.foundation.layout.Box
import androidx.compose.foundation.layout.Column
import androidx.compose.foundation.layout.fillMaxWidth
import androidx.compose.foundation.layout.width
import androidx.compose.runtime.CompositionLocalProvider
import androidx.compose.ui.Modifier
import androidx.compose.ui.platform.LocalDensity
import androidx.compose.ui.test.hasText
import androidx.compose.ui.test.junit4.createComposeRule
import androidx.compose.ui.test.onNodeWithTag
import androidx.compose.ui.text.TextLayoutResult
import androidx.compose.ui.unit.Density
import androidx.compose.ui.unit.dp
import com.example.data.PlayMode
import com.example.model.AgeBand
import com.example.model.GameRegistry
import com.example.ui.components.SpaceSectionTitle
import com.example.ui.screens.GameAboutContent
import com.example.ui.screens.GameSheetContent
import com.example.ui.screens.PanelHeader
import com.example.ui.screens.cardData
import com.example.ui.theme.AppFamily
import com.example.ui.theme.NeuroVidaTheme
import com.example.ui.theme.TitleFamily
import com.example.ui.theme.Typography
import org.junit.Assert.assertEquals
import org.junit.Assert.assertFalse
import org.junit.Assert.assertNotEquals
import org.junit.Assert.assertTrue
import org.junit.Rule
import org.junit.Test
import org.junit.runner.RunWith
import org.robolectric.RobolectricTestRunner
import org.robolectric.annotation.Config
import org.robolectric.annotation.GraphicsMode
import java.io.File

/**
 * Rubik Bold en los TÍTULOS de toda la app Kotlin (Tarea 175, 25-oct; Ricardo la probó en Avance —Tarea 174, «me convence»— y la aprobó para el resto): un título es el texto que NOMBRA una pantalla, una sección, una tarjeta o una lámina; los botones de arcilla (Fredoka), las cifras grandes, los chips,
 * las etiquetas, las pestañas y el cuerpo (Nunito) NO la usan. Se prueba con las piezas reales (la tipografía de Material, la ficha y «Sobre este juego», la cabecera de los paneles, los títulos de sección) y mirando el código donde la composición no llega (Hoy, Todos los juegos, el final, el Primer vuelo…).
 * Los juegos de Unity no se tocan. Que Rubik quepa en cada pantalla con letra 1,0, 1,3 y 2,0 lo miden `HoyTarjetaTest`, `GameSheetFitScreenshotTest` y `AvancePuntosTest`, y las capturas de `docs/previews/tarea-175.png`.
 */
@RunWith(RobolectricTestRunner::class)
@GraphicsMode(GraphicsMode.Mode.NATIVE)
@Config(sdk = [36])
class TitulosRubikTest {
  @get:Rule val rule = createComposeRule()

  private fun src(path: String) = File("src/main/java/com/example/$path").readText().replace("\r\n", "\n")

  private fun layoutOfTag(tag: String): TextLayoutResult {
    val node = rule.onNodeWithTag(tag, useUnmergedTree = true).fetchSemanticsNode()
    val out = mutableListOf<TextLayoutResult>()
    node.config[androidx.compose.ui.semantics.SemanticsActions.GetTextLayoutResult].action?.invoke(out)
    return out.firstOrNull() ?: error("«$tag» no entregó su diseño de texto")
  }

  private fun layoutOfText(text: String): TextLayoutResult {
    val node = rule.onAllNodes(hasText(text), useUnmergedTree = true)[0].fetchSemanticsNode()        // el primero: «Desafío» está en el botón y en una casilla
    val out = mutableListOf<TextLayoutResult>()
    node.config[androidx.compose.ui.semantics.SemanticsActions.GetTextLayoutResult].action?.invoke(out)
    return out.firstOrNull() ?: error("«$text» no entregó su diseño de texto")
  }

  @Test
  fun rubik_es_la_fuente_open_font_license_de_140_kb() {
    val ttf = File("src/main/res/font/rubik_bold.ttf")
    assertTrue("la fuente está en res/font", ttf.exists())
    val bytes = ttf.readBytes()
    assertTrue("es un TrueType", bytes[0].toInt() == 0 && bytes[1].toInt() == 1)
    val text = String(bytes, Charsets.ISO_8859_1) + String(bytes, Charsets.UTF_16BE)
    assertTrue("trae su nombre «Rubik» y la licencia SIL Open Font License en la tabla de nombres", text.contains("Rubik") && text.contains("SIL Open Font License"))
    assertTrue("pesa menos de 200 KB (${bytes.size / 1024} KB)", bytes.size < 200 * 1024)
  }

  @Test
  fun los_estilos_de_titulo_de_material_son_rubik_y_los_de_cuerpo_y_etiqueta_siguen_igual() {
    for ((name, style) in listOf("displayLarge" to Typography.displayLarge, "headlineMedium" to Typography.headlineMedium, "headlineSmall" to Typography.headlineSmall, "titleLarge" to Typography.titleLarge, "titleMedium" to Typography.titleMedium, "titleSmall" to Typography.titleSmall)) {
      assertEquals("$name es Rubik", TitleFamily, style.fontFamily)
    }
    for ((name, style) in listOf("bodyLarge" to Typography.bodyLarge, "bodyMedium" to Typography.bodyMedium, "bodySmall" to Typography.bodySmall, "labelLarge" to Typography.labelLarge, "labelMedium" to Typography.labelMedium, "labelSmall" to Typography.labelSmall)) {
      assertEquals("$name sigue en AppFamily (Nunito y Fredoka por peso)", AppFamily, style.fontFamily)
    }
    val type = src("ui/theme/Type.kt")
    val start = type.indexOf("val AppFamily = FontFamily(")
    val appFamily = type.substring(start, type.indexOf("\n)\n", start))
    assertFalse("AppFamily (la de toda la app, que elige por peso y arrastraría a los botones) no cambia: sigue Nunito y Fredoka", appFamily.contains("rubik"))
    assertTrue(appFamily.contains("R.font.nunito_regular") && appFamily.contains("fredoka("))
  }

  @Test
  fun la_ficha_lleva_su_nombre_y_sus_secciones_en_rubik_y_sus_botones_no() {
    val g = GameRegistry.getById("piloto")!!
    val data = cardData(g, g.title, null, 400 * 86_400_000L, 0.4f, emptyList(), playsLast14 = 0)
    rule.setContent {
      NeuroVidaTheme {
        Box(Modifier.width(393.dp)) { GameSheetContent(data, AgeBand.ADULT, expertOpen = false, choice = PlayMode.A_TU_MEDIDA, onChoose = {}, onPlay = {}, timed = true, onChooseTimed = {}, options = androidx.compose.runtime.mutableStateOf(true)) }
      }
    }
    rule.waitForIdle()
    assertEquals("el nombre del juego", TitleFamily, layoutOfTag("sheet_title").layoutInput.style.fontFamily)
    for (t in listOf("¿Cómo quieres jugar?", "¿Con reloj?")) assertEquals("«$t» (título de sección)", TitleFamily, layoutOfText(t).layoutInput.style.fontFamily)
    // los botones de arcilla y las casillas siguen en Fredoka (por peso, AppFamily)
    for (t in listOf("Jugar", "Desafío", "Menos opciones", "Sobre este juego", "Suave", "Sin reloj")) {
      val fam = layoutOfText(t).layoutInput.style.fontFamily
      assertNotEquals("el botón «$t» NO va en Rubik", TitleFamily, fam)
    }
  }

  @Test
  fun sobre_este_juego_lleva_sus_titulos_en_rubik_y_su_boton_no() {
    val g = GameRegistry.getById("radar")!!
    val data = cardData(g, g.title, null, 400 * 86_400_000L, 0.4f, emptyList(), playsLast14 = 0)
    rule.setContent { NeuroVidaTheme { Box(Modifier.width(393.dp)) { GameAboutContent(data) } } }
    rule.waitForIdle()
    assertEquals("«Sobre este juego»", TitleFamily, layoutOfTag("about_caption").layoutInput.style.fontFamily)
    assertEquals("el nombre del juego", TitleFamily, layoutOfTag("about_title").layoutInput.style.fontFamily)
    assertEquals("«Qué pones en juego»", TitleFamily, layoutOfText("Qué pones en juego").layoutInput.style.fontFamily)
    assertNotEquals("el botón «Cómo se juega» NO va en Rubik", TitleFamily, layoutOfText("Cómo se juega").layoutInput.style.fontFamily)
  }

  @Test
  fun la_cabecera_de_los_paneles_y_los_titulos_de_seccion_son_rubik_y_su_ayuda_no() {
    rule.setContent {
      NeuroVidaTheme {
        Column(Modifier.fillMaxWidth()) {
          PanelHeader("Ajustes") {}
          SpaceSectionTitle("Tus medallas", hint = "Por lo que haces, no por el puntaje")
        }
      }
    }
    rule.waitForIdle()
    assertEquals("la cabecera de Ajustes y Licencias", TitleFamily, layoutOfText("Ajustes").layoutInput.style.fontFamily)
    assertEquals("el título de sección de Tú", TitleFamily, layoutOfText("Tus medallas").layoutInput.style.fontFamily)
    assertNotEquals("la ayuda de debajo, no", TitleFamily, layoutOfText("Por lo que haces, no por el puntaje").layoutInput.style.fontFamily)
  }

  /** Lo que se mira en el código porque armar Hoy, el final o el Primer vuelo aquí pediría un ViewModel: cada título nombrado lleva `TitleFamily` en su misma línea, y las cifras y los botones no. */
  private fun assertLine(file: String, needle: String, expectTitle: Boolean) {
    val lines = src(file).lines().filter { needle in it }
    assertTrue("$file: no encontré «$needle»", lines.isNotEmpty())
    for (l in lines) assertEquals("$file «$needle»: ${if (expectTitle) "debe llevar" else "NO debe llevar"} TitleFamily en su línea: ${l.trim().take(120)}", expectTitle, l.contains("TitleFamily"))
  }

  @Test
  fun los_titulos_de_las_pantallas_principales_piden_rubik_y_las_cifras_y_los_botones_no() {
    // Hoy
    assertLine("ui/screens/HomeScreen.kt", "testTag(\"home_greeting\")", true)
    assertLine("ui/screens/HomeScreen.kt", "testTag(\"home_route_title\")", true)
    // Todos los juegos: el título de la pantalla y el nombre de cada área
    assertLine("ui/screens/AllGamesScreen.kt", "testTag(\"all_games_title\")", true)
    assertLine("ui/screens/AllGamesScreen.kt", "place.areaName", true)
    // Tú, Ajustes y Licencias (la cabecera de los paneles y las secciones)
    assertLine("ui/screens/ProfileScreen.kt", "testTag(\"tu_title\")", true)
    assertLine("ui/screens/LicensesScreen.kt", "text, color = Title, fontSize = 19.sp", true)
    // el final de la partida, el resumen de la ruta, el Primer vuelo, los diálogos y la medalla
    assertLine("ui/components/MeaningfulResult.kt", "model.whyTitle", true)
    assertLine("ui/screens/SessionSummaryScreen.kt", "¡Ruta de hoy completa!", true)
    assertLine("ui/screens/FirstFlightScreen.kt", "fontSize = size.sp, lineHeight = (size + 5).sp", true)
    assertLine("ui/components/ClayPanels.kt", "Text(title, color = Clay.Ink, fontSize = 22.sp", true)
    assertLine("ui/components/AchievementOverlay.kt", "OnBoardText(def.title", true)
    // lo que NO: las cifras grandes del final y de Avance
    assertLine("ui/components/MeaningfulResult.kt", "text = model.headline", false)
    assertLine("ui/screens/AvanceAreas.kt", "testTag(\"area_points_", false)
    // los botones de arcilla y su segunda línea, la barra de pestañas y las losas
    for (file in listOf("ui/components/ClayKit.kt", "ui/theme/Clay.kt", "ui/components/NeuroNavBar.kt")) assertFalse("$file no usa Rubik", src(file).contains("TitleFamily") || src(file).contains("R.font.rubik"))
  }
}
