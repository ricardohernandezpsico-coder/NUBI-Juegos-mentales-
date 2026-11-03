package com.example

import androidx.compose.foundation.layout.Box
import androidx.compose.foundation.layout.height
import androidx.compose.foundation.layout.padding
import androidx.compose.foundation.layout.width
import androidx.compose.foundation.background
import androidx.compose.runtime.CompositionLocalProvider
import androidx.compose.runtime.getValue
import androidx.compose.runtime.mutableStateOf
import androidx.compose.runtime.setValue
import androidx.compose.ui.Modifier
import androidx.compose.ui.graphics.Color
import androidx.compose.ui.platform.LocalDensity
import androidx.compose.ui.semantics.SemanticsActions
import androidx.compose.ui.semantics.SemanticsProperties
import androidx.compose.ui.test.SemanticsMatcher
import androidx.compose.ui.test.assert
import androidx.compose.ui.test.onAllNodesWithText
import androidx.compose.ui.text.TextLayoutResult
import androidx.compose.ui.test.assertIsDisplayed
import androidx.compose.ui.test.hasTestTag
import androidx.compose.ui.test.junit4.createComposeRule
import androidx.compose.ui.test.onNodeWithTag
import androidx.compose.ui.test.onRoot
import androidx.compose.ui.test.performClick
import androidx.compose.ui.test.performTextInput
import androidx.compose.ui.unit.Density
import androidx.compose.ui.unit.dp
import com.example.data.GameSearch
import com.example.data.MeasurePoint
import com.example.data.PlayMode
import com.example.model.AgeBand
import com.example.model.GameRegistry
import com.example.ui.components.AREA_ORDER
import com.example.ui.components.BoardBackground
import com.example.ui.screens.AllGamesContent
import com.example.ui.screens.GameSheetContent
import com.example.ui.screens.allGamesSections
import com.example.ui.screens.cardData
import com.example.ui.theme.NeuroVidaTheme
import com.github.takahirom.roborazzi.RobolectricDeviceQualifiers
import com.github.takahirom.roborazzi.captureRoboImage
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
 * La pestaña «Juegos» (Tarea 220; antes «Todos los juegos», la ventana de Hoy, Tarea 158; antes esta clase era `LibraryScreenshotTest`, de la pestaña Juegos de las cuatro áreas): los 17 juegos, su buscador (ignora tildes y mayúsculas), los chips de área y las filas de la lámina de un lugar. Se arma con `AllGamesContent` y datos fijos,
 * sin ViewModel. Se prueba que estén los 17 agrupados por área, que cada chip filtre, que «rescate relampago» encuentre «Rescate relámpago», que sin resultados haya una frase de Nubi, que la fila marcada quede a la vista y que todo quepa a letra 1,3 y 2,0; y se capturan
 * (`juegos`, `juegos-memoria`, `juegos-letra13`) junto con la ficha de un juego (`library-ficha`, plegada y desplegada, que la abren estas filas).
 */
@RunWith(RobolectricTestRunner::class)
@GraphicsMode(GraphicsMode.Mode.NATIVE)
@Config(qualifiers = RobolectricDeviceQualifiers.Pixel8, sdk = [36])
class AllGamesScreenshotTest {

  @get:Rule val composeTestRule = createComposeRule()

  private val day = 86_400_000L
  private val now = 400 * day + 3_600_000L
  private val places = PlaceTestData.places(now)

  private var area by mutableStateOf<String?>(null)
  private var query by mutableStateOf("")
  private var mark by mutableStateOf<String?>(null)
  private var opened: String? = null
  private var revealed = 0

  private fun show(fontScale: Float = 1f, widthDp: Int = 393, heightDp: Int = 873, reveal: String? = null, barInset: Int = 0) {
    composeTestRule.setContent {
      val base = LocalDensity.current
      CompositionLocalProvider(LocalDensity provides Density(base.density, fontScale), com.example.ui.components.LocalBarInset provides barInset.dp) {
        NeuroVidaTheme {
          Box(Modifier.width(widthDp.dp).height(heightDp.dp)) {
            BoardBackground()
            AllGamesContent(
              places = places, area = area, query = query, mark = mark,
              onQueryChange = { query = it }, onArea = { area = it }, onGame = { opened = it },
              reveal = reveal, onRevealed = { revealed++ },
              modifier = Modifier.padding(top = 24.dp)
            )
          }
        }
      }
    }
    composeTestRule.waitForIdle()
  }

  private fun count(tag: String) = composeTestRule.onAllNodes(hasTestTag(tag)).fetchSemanticsNodes().size
  private fun rows() = GameRegistry.allGames.count { count("place_game_${it.id}") > 0 }

  @Test
  fun estan_los_17_juegos_agrupados_por_area_y_cada_fila_abre_su_ficha() {
    show()
    assertEquals(17, GameRegistry.allGames.size)
    // los que caben en pantalla están; los demás se alcanzan deslizando la lista: se comprueba con la lista de la lógica
    val all = allGamesSections(places, null, "")
    assertEquals("las cuatro áreas, en su orden", AREA_ORDER, all.map { it.first.area })
    assertEquals("los 17 juegos", 17, all.sumOf { it.second.size })
    composeTestRule.onNodeWithTag("all_games_title").assertIsDisplayed()
    val first = all.first().second.first().gameId
    composeTestRule.onNodeWithTag("place_game_$first").performClick()
    assertEquals("tocar una fila abre la ficha de ese juego", first, opened)
  }

  @Test
  fun cada_chip_deja_solo_los_juegos_de_su_area() {
    show()
    for (key in AREA_ORDER) {
      composeTestRule.onNodeWithTag("all_chip_${key.lowercase()}").performClick()
      composeTestRule.waitForIdle()
      assertEquals(key, area)
      val expected = GameRegistry.allGames.filter { it.domain.name == key }.map { it.id }.toSet()
      assertEquals("solo ${key}", expected, allGamesSections(places, area, query).flatMap { it.second }.map { it.gameId }.toSet())
      assertTrue("el encabezado del área está", count("all_area_${key.lowercase()}") == 1)
    }
    composeTestRule.onNodeWithTag("all_chip_todas").performClick()
    assertEquals(null, area)
  }

  @Test
  fun el_chip_elegido_dice_elegida_para_el_lector_de_pantalla() {
    area = "ATENCION"
    show()
    assertEquals(true, composeTestRule.onNodeWithTag("all_chip_atencion").fetchSemanticsNode().config[SemanticsProperties.Selected])
    assertEquals(false, composeTestRule.onNodeWithTag("all_chip_memoria").fetchSemanticsNode().config[SemanticsProperties.Selected])
  }

  @Test
  fun buscar_ignora_tildes_y_mayusculas() {
    show()
    composeTestRule.onNodeWithTag("all_games_search").performTextInput("rescate relampago")
    composeTestRule.waitForIdle()
    assertEquals("rescate relampago", query)
    val found = allGamesSections(places, area, query).flatMap { it.second }.map { it.gameId }
    assertEquals("«rescate relampago» encuentra «Rescate relámpago» (radar)", listOf("radar"), found)
    assertTrue(GameSearch.matches("Rescate relámpago", "RESCATE RELAMPAGO"))
    assertTrue(GameSearch.matches("Tinta o Palabra", "tinta"))
    assertTrue("varias palabras, en cualquier orden", GameSearch.matches("Tinta o Palabra", "palabra tinta"))
    assertFalse(GameSearch.matches("Tinta o Palabra", "tintero"))
    assertTrue("una búsqueda vacía encuentra todo", GameSearch.matches("Constelaciones", "   "))
    composeTestRule.onNodeWithTag("place_game_radar").assertIsDisplayed()
    assertEquals("las demás filas se van", 1, rows())
  }

  @Test
  fun sin_resultados_nubi_lo_dice_con_una_frase_y_no_hay_filas() {
    show()
    composeTestRule.onNodeWithTag("all_games_search").performTextInput("zzzz")
    composeTestRule.waitForIdle()
    composeTestRule.onNodeWithTag("all_games_empty").assertIsDisplayed()
    assertEquals(0, rows())
    assertEquals(0, count("all_area_memoria"))
  }

  /** Tarea 220: es una pestaña, no una ventana: el título es «Juegos» (como el rótulo de la barra), es un encabezado para el lector de pantalla, y no hay X (Atrás hace lo de las otras pestañas). */
  @Test
  fun la_pestana_se_llama_Juegos_es_un_encabezado_y_no_tiene_X() {
    show()
    composeTestRule.onNodeWithTag("all_games_title").assertIsDisplayed()
    composeTestRule.onNodeWithTag("all_games_title").assert(SemanticsMatcher.keyIsDefined(SemanticsProperties.Heading))
    assertEquals("el título dice «Juegos»", listOf("Juegos"), composeTestRule.onNodeWithTag("all_games_title").fetchSemanticsNode().config[SemanticsProperties.Text].map { it.text })
    assertEquals("sin la X", 0, count("all_games_close"))
    assertEquals("y ya no dice «Todos los juegos»", 0, composeTestRule.onAllNodesWithText("Todos los juegos").fetchSemanticsNodes().size)
    val r = ArrayList<TextLayoutResult>()
    composeTestRule.onNodeWithTag("all_games_title").fetchSemanticsNode().config[SemanticsActions.GetTextLayoutResult].action?.invoke(r)
    assertEquals("26 sp, como «Avance» y «Tú»", 26f, r.first().layoutInput.style.fontSize.value, 0.01f)
  }

  /** La franja de la barra de abajo queda libre (`LocalBarInset`): la lista termina sobre ella, no detrás. */
  @Test
  fun la_lista_termina_sobre_la_franja_de_la_barra() {
    show(heightDp = 873, barInset = 88)
    val list = composeTestRule.onNodeWithTag("all_games_list").fetchSemanticsNode().boundsInRoot
    val d = composeTestRule.density.density
    assertTrue("la lista llega a ${list.bottom / d} dp y la barra empieza en ${873 - 88} dp", list.bottom <= (873f - 88f) * d + 1f)
  }

  @Test
  fun la_fila_marcada_queda_a_la_vista_aunque_este_abajo() {
    mark = "intrusa"                                    // un juego de Lenguaje: lo último de la lista
    show(heightDp = 600, reveal = "intrusa")
    composeTestRule.onNodeWithTag("place_game_intrusa").assertIsDisplayed()
    assertEquals("y se avisa UNA vez, para que al volver de otra pestaña la lista quede donde se dejó", 1, revealed)
  }

  @Test
  fun sin_reveal_la_lista_no_se_mueve_aunque_haya_una_fila_marcada() {
    mark = "intrusa"
    show(heightDp = 600)
    assertEquals("la fila marcada sigue marcada pero no se trae a la vista", 0, count("place_game_intrusa"))
    assertEquals(0, revealed)
  }

  private fun tocaBienConLetra(scale: Float, label: String) {
    show(fontScale = scale)
    val d = composeTestRule.density.density
    for (id in listOf("all_chip_todas") + AREA_ORDER.map { "all_chip_${it.lowercase()}" }) {
      val b = composeTestRule.onNodeWithTag(id).fetchSemanticsNode().boundsInRoot
      assertTrue("$label: el chip $id mide ${b.height / d} dp de alto (≥ 48)", b.height >= 48f * d)
      assertTrue("$label: el chip $id cabe en el ancho: ${b.right / d} dp", b.left >= 0f && b.right <= 393f * d + 1f)
    }
    val search = composeTestRule.onNodeWithTag("all_games_search").fetchSemanticsNode().boundsInRoot
    assertTrue("$label: el buscador mide ≥ 48 dp de alto: ${search.height / d}", search.height >= 48f * d)
  }

  @Test fun los_chips_y_el_buscador_tocan_48_dp_con_letra_1_0() = tocaBienConLetra(1f, "letra 1,0")
  @Test fun los_chips_y_el_buscador_tocan_48_dp_con_letra_1_3() = tocaBienConLetra(1.3f, "letra 1,3")
  @Test fun los_chips_y_el_buscador_tocan_48_dp_con_letra_2_0() = tocaBienConLetra(2f, "letra 2,0")

  // ---- Capturas

  @Test fun captura_juegos() { show(); composeTestRule.onRoot().captureRoboImage(filePath = "src/test/screenshots/juegos.png") }
  @Test fun captura_juegos_con_memoria() { area = "MEMORIA"; mark = null; show(); composeTestRule.onRoot().captureRoboImage(filePath = "src/test/screenshots/juegos-memoria.png") }
  @Test fun captura_juegos_buscando() { query = "tinta"; show(); composeTestRule.onRoot().captureRoboImage(filePath = "src/test/screenshots/juegos-buscando.png") }
  @Test fun captura_juegos_letra_1_3() { show(fontScale = 1.3f); composeTestRule.onRoot().captureRoboImage(filePath = "src/test/screenshots/juegos-letra13.png") }

  @Test
  fun sheet() {
    val aterrizaje = GameRegistry.getById("aterrizaje")!!
    val points = listOf(31f, 27f, 29f, 24f, 22f, 18f).mapIndexed { i, v -> MeasurePoint(now - (6 - i) * day, "numline", v) }
    val data = cardData(aterrizaje, aterrizaje.title, 0.45f)
    composeTestRule.setContent {
      NeuroVidaTheme {
        Box(Modifier.width(412.dp).background(Color(0xFF050823)).padding(14.dp)) {
          GameSheetContent(data, AgeBand.SENIOR, expertOpen = false, choice = PlayMode.DESAFIO, onChoose = {}, onPlay = {})
        }
      }
    }
    composeTestRule.onRoot().captureRoboImage(filePath = "src/test/screenshots/library-ficha.png")                // plegada (por defecto)
    composeTestRule.onNodeWithTag("sheet_change").performClick()
    composeTestRule.waitForIdle()
    composeTestRule.onRoot().captureRoboImage(filePath = "src/test/screenshots/library-ficha-desplegada.png")     // «Listo»: las dos secciones como siempre
  }
}
