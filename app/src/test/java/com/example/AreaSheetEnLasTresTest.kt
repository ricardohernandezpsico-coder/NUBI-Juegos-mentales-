package com.example

import androidx.activity.ComponentActivity
import androidx.compose.foundation.layout.Box
import androidx.compose.foundation.layout.Column
import androidx.compose.foundation.layout.fillMaxSize
import androidx.compose.foundation.layout.height
import androidx.compose.foundation.layout.padding
import androidx.compose.foundation.layout.width
import androidx.compose.foundation.rememberScrollState
import androidx.compose.foundation.verticalScroll
import androidx.compose.runtime.CompositionLocalProvider
import androidx.compose.runtime.mutableIntStateOf
import androidx.compose.runtime.rememberCoroutineScope
import androidx.compose.ui.Modifier
import androidx.compose.ui.geometry.Rect
import androidx.compose.ui.platform.LocalDensity
import androidx.compose.ui.test.assertIsDisplayed
import androidx.compose.ui.test.click
import androidx.compose.ui.test.hasTestTag
import androidx.compose.ui.test.hasText
import androidx.compose.ui.test.junit4.createAndroidComposeRule
import androidx.compose.ui.test.onNodeWithTag
import androidx.compose.ui.test.onNodeWithText
import androidx.compose.ui.test.onRoot
import androidx.compose.ui.test.performClick
import androidx.compose.ui.test.performTouchInput
import androidx.compose.ui.unit.Density
import androidx.compose.ui.unit.dp
import com.example.data.IslaLugar
import com.example.data.PlaceSheetUi
import com.example.data.PlaceSheets
import com.example.data.RouteState
import com.example.data.Week
import com.example.data.nubiWeekLine
import com.example.data.routeStatesOf
import com.example.data.weekSummary
import com.example.model.DomainType
import com.example.model.GameRegistry
import com.example.ui.components.AREA_ORDER
import com.example.ui.components.BoardBackground
import com.example.ui.components.LocalReduceMotion
import com.example.ui.components.LocalTabSwipeLock
import com.example.ui.screens.AreaGrid
import com.example.ui.screens.AreaSheetScaffold
import com.example.ui.screens.AreaSuggestionUi
import com.example.ui.screens.AvanceAreas
import com.example.ui.screens.AvanceWeek
import com.example.ui.screens.NubiAnchor
import com.example.ui.screens.PlaceFx
import com.example.ui.screens.SuggestionHeader
import com.example.ui.theme.NeuroVidaTheme
import com.github.takahirom.roborazzi.RobolectricDeviceQualifiers
import com.github.takahirom.roborazzi.captureRoboImage
import org.junit.Assert.assertEquals
import org.junit.Assert.assertNotNull
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
 * Un área, una sola vista (Tarea 155; `auditoria-fable/spec-area-unica.md`, opción 1): tocar un área en JUEGOS o en AVANCE abre LA MISMA lámina que Hoy (mismos textos, mismas filas, mismo botón), con su velo, y Nubi vuela desde su sitio en la pestaña; en Avance la lámina lleva además el
 * gráfico de las 4 semanas. Las filas de las paradas de la ruta de hoy dicen «hoy, en tu ruta» o «hecho hoy» y van primero. X, Atrás y el velo la cierran; tocar una fila abre la ficha (aquí, `onGame`) encima y la lámina queda abierta.
 * Cada pestaña se arma con sus piezas reales (`SuggestionHeader` + `AreaGrid`; `AvanceWeek` + `AvanceAreas`) dentro de `AreaSheetScaffold`, sin ViewModel.
 * Capturas: `juegos-lamina` y `avance-lamina` (1,0 y 1,3), a 393 × 873.
 */
@RunWith(RobolectricTestRunner::class)
@GraphicsMode(GraphicsMode.Mode.NATIVE)
@Config(qualifiers = RobolectricDeviceQualifiers.Pixel8, sdk = [36])
class AreaSheetEnLasTresTest {
  @get:Rule val rule = createAndroidComposeRule<ComponentActivity>()

  private val now = Calendar.getInstance().apply { set(2026, Calendar.OCTOBER, 12, 10, 0, 0) }.timeInMillis
  private val lock = mutableIntStateOf(0)
  private var played: String? = null
  private var card: String? = null
  private val fx = PlaceFx(looks = false)
  private val anchor = NubiAnchor()
  private val names = DomainType.values().associate { it.name to it.displayName }
  private val statuses = AREA_ORDER.map { PlaceTestData.statuses.getValue(it) }

  private fun placesWithRoute(gameIds: List<String> = listOf("bodega", "correo", "calculo"), completed: Int = 1): Map<String, PlaceSheetUi> {
    val routes = routeStatesOf(gameIds, completed)
    return PlaceTestData.statuses.mapValues { (area, status) ->
      PlaceSheets.build(
        area = area, status = status, games = GameRegistry.allGames.filter { it.domain.name == area }, titleOf = { it.title },
        history = emptyList(), measures = emptyList(), now = now, suggestion = PlaceTestData.suggestion, routes = routes
      )
    }
  }

  private fun show(tab: String, widthDp: Int = 393, heightDp: Int = 873, fontScale: Float = 1f, reduce: Boolean = false, places: Map<String, PlaceSheetUi> = placesWithRoute()) {
    rule.setContent {
      val base = LocalDensity.current
      val scope = rememberCoroutineScope()
      CompositionLocalProvider(LocalDensity provides Density(base.density, fontScale), LocalTabSwipeLock provides lock, LocalReduceMotion provides reduce) {
        NeuroVidaTheme {
          Box(Modifier.width(widthDp.dp).height(heightDp.dp)) {
            BoardBackground()
            AreaSheetScaffold(
              fx = fx, places = places, anchor = anchor, showChart = tab == "avance",
              onClose = { fx.close(scope, reduce) }, onPlay = { played = it }, onGame = { card = it },
              modifier = Modifier.padding(top = 24.dp)          // 24 dp de barra de estado; la barra de navegación cae DENTRO de la lámina (en la app la cubre y el pie la esquiva con su inset, que en la prueba es 0)
            ) {
              val open: (String) -> Unit = { area -> IslaLugar.nameOfArea(area)?.lowercase()?.let { fx.open(scope, it, reduce) } }
              if (tab == "juegos") {
                Column(Modifier.fillMaxSize()) {
                  SuggestionHeader(AreaSuggestionUi("MEMORIA", "Memoria", "sigue Bodega de carga en tu ruta de hoy", "bodega"), anchor)
                  AreaGrid(statuses, playedToday = setOf("MEMORIA"), suggested = "MEMORIA", lang = com.example.model.AppLanguage.SPANISH, onArea = open, modifier = Modifier.weight(1f))
                }
              } else {
                val lamps = Week.Lamps(listOf(true, true, false, true, false, false, false), todayIndex = 3)
                val summary = weekSummary(lamps, goal = 4, streak = 3, areas = 2, noGamesAtAll = false)
                Column(Modifier.fillMaxSize().verticalScroll(rememberScrollState()).padding(horizontal = 22.dp)) {
                  AvanceWeek(summary, lamps, "Descansaste el martes: tu racha sigue.", nubiWeekLine(lamps, 4, com.example.data.Streak.StreakInfo(3, 5L, false), 2, noGamesAtAll = false), anchor)
                  AvanceAreas(statuses, names, open)
                }
              }
            }
          }
        }
      }
    }
    rule.waitForIdle()
  }

  private fun openArea(tab: String, area: String = "memoria") {
    val tag = if (tab == "juegos") "area_$area" else "progress_area_$area"
    rule.onNodeWithTag(tag).performClick()
    rule.waitForIdle()
  }

  private fun count(tag: String) = rule.onAllNodes(hasTestTag(tag)).fetchSemanticsNodes().size
  private fun textOf(tag: String, unmerged: Boolean = false): String = rule.onNodeWithTag(tag, useUnmergedTree = unmerged).fetchSemanticsNode().config[androidx.compose.ui.semantics.SemanticsProperties.Text].joinToString("") { it.text }
  private fun rawBounds(tag: String): Rect = rule.onNodeWithTag(tag).fetchSemanticsNode().let { Rect(it.positionInRoot.x, it.positionInRoot.y, it.positionInRoot.x + it.size.width, it.positionInRoot.y + it.size.height) }
  private fun pressBack() { rule.runOnUiThread { rule.activity.onBackPressedDispatcher.onBackPressed() }; rule.waitForIdle() }

  // ---- La misma lámina en las dos pestañas

  @Test
  fun juegos_abre_la_misma_lamina_que_hoy_con_sus_textos_y_sus_filas() {
    show("juegos"); openArea("juegos")
    rule.onNodeWithTag("place_sheet").assertIsDisplayed()
    assertEquals("Observatorio", textOf("place_title"))
    assertEquals("el avance en UNA línea, como en Hoy", "Aprendiz · 22 de 100", textOf("place_stage"))
    rule.onNodeWithTag("place_bar").assertIsDisplayed()
    rule.onNodeWithText("Esta semana avanzó de 18 a 22. Te faltan 18 para Hábil.").assertIsDisplayed()
    rule.onNodeWithTag("place_play").assertIsDisplayed()
    assertEquals("en Juegos la lámina NO lleva el gráfico de las 4 semanas", 0, count("place_chart"))
    for (g in placesWithRoute().getValue("MEMORIA").games) rule.onNodeWithTag("place_game_${g.gameId}").assertExists()
  }

  @Test
  fun avance_abre_la_misma_lamina_con_el_grafico_de_las_cuatro_semanas() {
    show("avance"); openArea("avance")
    rule.onNodeWithTag("place_sheet").assertIsDisplayed()
    assertEquals("Observatorio", textOf("place_title"))
    assertEquals("Aprendiz · 22 de 100", textOf("place_stage"))
    rule.onNodeWithText("Esta semana avanzó de 18 a 22. Te faltan 18 para Hábil.").assertIsDisplayed()
    rule.onNodeWithTag("place_chart").assertIsDisplayed()
    for (label in listOf("hace 3 sem.", "hace 2 sem.", "sem. pasada", "esta sem.")) rule.onNodeWithText(label).assertExists()
    rule.onNodeWithTag("place_play").assertIsDisplayed()
  }

  @Test
  fun un_area_sin_medir_no_dibuja_el_grafico_ni_inventa_numeros() {
    show("avance"); openArea("avance", "lenguaje")
    assertEquals("Aún sin medir", textOf("place_stage"))
    assertEquals(0, count("place_chart"))
  }

  @Test
  fun el_boton_de_juegos_juega_el_juego_que_sugiere_nubi() {
    val chosen = placesWithRoute().getValue("MEMORIA").chosenGameId!!
    show("juegos"); openArea("juegos")
    rule.onNodeWithTag("place_play").performClick()
    assertEquals(chosen, played)
    assertNull("el botón no abre una ficha", card)
  }

  @Test
  fun el_boton_de_avance_juega_el_mismo_juego_que_el_de_juegos_y_el_de_hoy() {
    val chosen = placesWithRoute().getValue("MEMORIA").chosenGameId!!
    show("avance"); openArea("avance")
    rule.onNodeWithTag("place_play").performClick()
    assertEquals("el botón juega el MISMO juego en las tres pestañas (LA sugerencia de Nubi)", chosen, played)
    assertNull(card)
  }

  // ---- Las paradas de la ruta de hoy

  @Test
  fun las_filas_de_la_ruta_dicen_hoy_en_tu_ruta_y_hecho_hoy_y_van_primero() {
    val memoria = placesWithRoute(listOf("bodega", "correo", "calculo"), completed = 1).getValue("MEMORIA")
    assertEquals("la parada pendiente va primero, luego la hecha y después el resto", listOf("correo", "bodega"), memoria.games.take(2).map { it.gameId })
    assertEquals(RouteState.PENDING, memoria.games[0].routeState)
    assertEquals(RouteState.DONE, memoria.games[1].routeState)
    assertEquals("hoy, en tu ruta", memoria.games[0].whenShort)
    assertEquals("hecho hoy", memoria.games[1].whenShort)
    assertTrue("el lector de pantalla lo dice completo", memoria.games[0].spoken.contains("ruta") && memoria.games[1].spoken.contains("hecho hoy"))
    show("juegos", places = mapOf("MEMORIA" to memoria) + PlaceTestData.places(now).filterKeys { it != "MEMORIA" }); openArea("juegos")
    assertEquals("hoy, en tu ruta", textOf("place_game_when_correo", unmerged = true))
    assertEquals("hecho hoy", textOf("place_game_when_bodega", unmerged = true))
    rule.onNodeWithTag("place_game_correo").assertIsDisplayed()
  }

  @Test
  fun un_juego_que_no_es_parada_de_hoy_dice_cuando_se_jugo() {
    val rows = placesWithRoute().getValue("MEMORIA").games
    val other = rows.first { it.routeState == RouteState.NONE }
    assertEquals(PlaceSheets.whenShort(other.daysAgo), other.whenShort)
    assertTrue("las paradas van antes que los demás", rows.indexOfFirst { it.routeState == RouteState.NONE } > rows.indexOfLast { it.routeState != RouteState.NONE })
  }

  // ---- Nubi vuela desde la pestaña y se posa en la lámina

  private fun nubiSePosa(tab: String) {
    show(tab)
    assertNotNull("la Nubi de $tab marcó su caja", anchor.bounds)
    openArea(tab)
    assertEquals("Nubi está en la lámina ($tab)", 1, count("place_nubi"))
    assertEquals("y su hueco, dentro de ella", 1, count("place_nubi_hole"))
  }

  @Test fun nubi_se_posa_en_la_lamina_desde_la_cabecera_de_juegos() = nubiSePosa("juegos")
  @Test fun nubi_se_posa_en_la_lamina_desde_tu_semana_en_avance() = nubiSePosa("avance")

  @Test
  fun con_quitar_animaciones_nubi_aparece_posada_sin_volar() {
    show("juegos", reduce = true); openArea("juegos")
    assertEquals(1, count("place_nubi"))
    rule.onNodeWithTag("place_sheet").assertIsDisplayed()
  }

  // ---- Cerrar, abrir la ficha y las pestañas quietas

  private fun cierres(tab: String) {
    show(tab); openArea(tab)
    assertTrue("abierta ($tab)", fx.active)
    rule.onNodeWithTag("place_close").performClick(); rule.waitForIdle()
    assertNull("la X la cierra ($tab)", fx.key)
    openArea(tab)
    pressBack()
    assertNull("Atrás la cierra ($tab)", fx.key)
    openArea(tab)
    rule.onNodeWithTag("place_veil").performTouchInput { click(androidx.compose.ui.geometry.Offset(width / 2f, 40f)) }
    rule.waitForIdle()
    assertNull("el velo la cierra ($tab)", fx.key)
  }

  @Test fun x_atras_y_velo_cierran_la_lamina_en_juegos() = cierres("juegos")
  @Test fun x_atras_y_velo_cierran_la_lamina_en_avance() = cierres("avance")

  @Test
  fun tocar_una_fila_abre_la_ficha_y_la_lamina_se_queda_abierta() {
    show("juegos"); openArea("juegos")
    val first = placesWithRoute().getValue("MEMORIA").games.first().gameId
    rule.onNodeWithTag("place_game_$first").performClick(); rule.waitForIdle()
    assertEquals(first, card)
    assertNull("tocar el nombre no juega", played)
    assertTrue("la lámina sigue abierta debajo de la ficha: Atrás vuelve a ella", fx.active)
  }

  @Test
  fun con_la_lamina_abierta_las_pestanas_no_se_deslizan() {
    show("avance")
    assertEquals(0, lock.intValue)
    openArea("avance")
    assertTrue("las pestañas quedan bloqueadas (y la barra se retira)", lock.intValue > 0)
  }

  // ---- Todo cabe en una vista (Avance, con el gráfico, es la más alta)

  private fun assertEverythingInside(widthDp: Int, heightDp: Int, area: String, label: String, allowScroll: Boolean) {
    val density = rule.density.density
    val pageTop = 24 * density; val pageBottom = heightDp * density
    val play = rawBounds("place_play")
    assertTrue("$label: el botón $play cabe en la pantalla", play.top >= pageTop && play.bottom <= pageBottom + 1f)
    val sheet = rawBounds("place_sheet")
    assertTrue("$label: queda al menos 56 dp de cielo sobre la lámina: ${sheet.top} contra ${pageTop + 56 * density}", sheet.top >= pageTop + 56 * density - 1f)
    if (!allowScroll) {
      assertEquals("$label: la lámina no se desliza", 0, count("place_scroll"))
      for (g in placesWithRoute().getValue(area).games) {
        val r = rawBounds("place_game_${g.gameId}")
        assertTrue("$label: la fila de ${g.title} ($r) cabe", r.top >= pageTop && r.bottom <= pageBottom + 1f)
      }
    }
  }

  @Test fun avance_con_grafico_cabe_en_una_vista_a_393_por_873() { show("avance", 393, 873); openArea("avance"); assertEverythingInside(393, 873, "MEMORIA", "Avance 393x873", allowScroll = false); assertEquals(1, count("place_chart")) }
  @Test fun juegos_cabe_en_una_vista_a_393_por_873() { show("juegos", 393, 873); openArea("juegos"); assertEverythingInside(393, 873, "MEMORIA", "Juegos 393x873", allowScroll = false) }
  @Test fun juegos_con_cinco_juegos_cabe_a_360_por_780() { show("juegos", 360, 780); openArea("juegos", "atencion"); assertEverythingInside(360, 780, "ATENCION", "Juegos Antena 360x780", allowScroll = false) }
  @Test fun avance_con_letra_1_3_deja_el_boton_y_los_juegos_a_la_vista() {
    show("avance", 393, 873, 1.3f); openArea("avance")
    assertEverythingInside(393, 873, "MEMORIA", "Avance 393x873 letra 1,3", allowScroll = true)
    for (g in placesWithRoute().getValue("MEMORIA").games) rule.onNodeWithTag("place_game_${g.gameId}").assertExists()
  }
  @Test fun avance_con_letra_2_0_deja_el_boton_fijo_a_la_vista() { show("avance", 393, 873, 2f); openArea("avance"); assertEverythingInside(393, 873, "MEMORIA", "Avance letra 2,0", allowScroll = true) }

  // ---- Capturas

  private fun foto(tab: String, fontScale: Float, name: String) {
    show(tab, fontScale = fontScale); openArea(tab)
    rule.onRoot().captureRoboImage(filePath = "src/test/screenshots/$name.png")
  }

  @Test fun captura_juegos_lamina() = foto("juegos", 1f, "juegos-lamina")
  @Test fun captura_avance_lamina() = foto("avance", 1f, "avance-lamina")
  @Test fun captura_juegos_lamina_letra13() = foto("juegos", 1.3f, "juegos-lamina-letra13")
  @Test fun captura_avance_lamina_letra13() = foto("avance", 1.3f, "avance-lamina-letra13")
}
