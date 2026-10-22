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
import androidx.compose.ui.semantics.SemanticsProperties
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
 * «Todos los juegos» (Tarea 158, «La isla es el mapa»; antes esta clase era `LibraryScreenshotTest`, de la pestaña Juegos): la ventana de Hoy con los 17 juegos, su buscador (ignora tildes y mayúsculas), los chips de área y las filas de la lámina de un lugar. Se arma con `AllGamesContent` y datos fijos,
 * sin ViewModel. Se prueba que estén los 17 agrupados por área, que cada chip filtre, que «rescate relampago» encuentre «Rescate relámpago», que sin resultados haya una frase de Nubi, que la fila marcada quede a la vista y que todo quepa a letra 1,3 y 2,0; y se capturan
 * (`todos-los-juegos`, `todos-los-juegos-memoria`, `todos-los-juegos-letra13`) junto con la ficha de un juego (`library-ficha`, plegada y desplegada, que la abren estas filas).
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
  private var closed = 0

  private fun show(fontScale: Float = 1f, widthDp: Int = 393, heightDp: Int = 873) {
    composeTestRule.setContent {
      val base = LocalDensity.current
      CompositionLocalProvider(LocalDensity provides Density(base.density, fontScale)) {
        NeuroVidaTheme {
          Box(Modifier.width(widthDp.dp).height(heightDp.dp)) {
            BoardBackground()
            AllGamesContent(
              places = places, area = area, query = query, mark = mark,
              onQueryChange = { query = it }, onArea = { area = it }, onClose = { closed++ }, onGame = { opened = it },
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

  @Test
  fun la_x_cierra_la_ventana() {
    show()
    composeTestRule.onNodeWithTag("all_games_close").performClick()
    assertEquals(1, closed)
  }

  @Test
  fun la_fila_marcada_queda_a_la_vista_aunque_este_abajo() {
    mark = "intrusa"                                    // un juego de Lenguaje: lo último de la lista
    show(heightDp = 600)
    composeTestRule.onNodeWithTag("place_game_intrusa").assertIsDisplayed()
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
    val close = composeTestRule.onNodeWithTag("all_games_close").fetchSemanticsNode().boundsInRoot
    assertTrue("$label: la X toca ≥ 48 dp", close.width >= 48f * d && close.height >= 48f * d)
  }

  @Test fun los_chips_el_buscador_y_la_x_tocan_48_dp_con_letra_1_0() = tocaBienConLetra(1f, "letra 1,0")
  @Test fun los_chips_el_buscador_y_la_x_tocan_48_dp_con_letra_1_3() = tocaBienConLetra(1.3f, "letra 1,3")
  @Test fun los_chips_el_buscador_y_la_x_tocan_48_dp_con_letra_2_0() = tocaBienConLetra(2f, "letra 2,0")

  // ---- Capturas

  @Test fun captura_todos_los_juegos() { show(); composeTestRule.onRoot().captureRoboImage(filePath = "src/test/screenshots/todos-los-juegos.png") }
  @Test fun captura_todos_los_juegos_con_memoria() { area = "MEMORIA"; mark = null; show(); composeTestRule.onRoot().captureRoboImage(filePath = "src/test/screenshots/todos-los-juegos-memoria.png") }
  @Test fun captura_todos_los_juegos_buscando() { query = "tinta"; show(); composeTestRule.onRoot().captureRoboImage(filePath = "src/test/screenshots/todos-los-juegos-buscando.png") }
  @Test fun captura_todos_los_juegos_letra_1_3() { show(fontScale = 1.3f); composeTestRule.onRoot().captureRoboImage(filePath = "src/test/screenshots/todos-los-juegos-letra13.png") }

  @Test
  fun sheet() {
    val aterrizaje = GameRegistry.getById("aterrizaje")!!
    val points = listOf(31f, 27f, 29f, 24f, 22f, 18f).mapIndexed { i, v -> MeasurePoint(now - (6 - i) * day, "numline", v) }
    val data = cardData(aterrizaje, aterrizaje.title, now - day, now, 0.45f, points, playsLast14 = 6)
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
