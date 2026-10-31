package com.example.ui.screens

import androidx.activity.compose.BackHandler
import androidx.compose.foundation.background
import androidx.compose.foundation.border
import androidx.compose.foundation.layout.Arrangement
import androidx.compose.foundation.layout.Box
import androidx.compose.foundation.layout.Column
import androidx.compose.foundation.layout.Row
import androidx.compose.foundation.layout.Spacer
import androidx.compose.foundation.layout.fillMaxSize
import androidx.compose.foundation.layout.fillMaxWidth
import androidx.compose.foundation.layout.heightIn
import androidx.compose.foundation.layout.padding
import androidx.compose.foundation.layout.size
import androidx.compose.foundation.layout.width
import androidx.compose.foundation.lazy.LazyColumn
import androidx.compose.foundation.lazy.items
import androidx.compose.foundation.lazy.rememberLazyListState
import androidx.compose.foundation.shape.CircleShape
import androidx.compose.foundation.text.KeyboardActions
import androidx.compose.foundation.text.KeyboardOptions
import androidx.compose.material3.Text
import androidx.compose.runtime.Composable
import androidx.compose.runtime.LaunchedEffect
import androidx.compose.runtime.collectAsState
import androidx.compose.runtime.getValue
import androidx.compose.runtime.mutableStateOf
import androidx.compose.runtime.remember
import androidx.compose.runtime.saveable.rememberSaveable
import androidx.compose.runtime.setValue
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.draw.alpha
import androidx.compose.ui.draw.clip
import androidx.compose.ui.layout.Layout
import androidx.compose.ui.layout.Placeable
import androidx.compose.ui.platform.LocalDensity
import androidx.compose.ui.platform.LocalFocusManager
import androidx.compose.ui.platform.testTag
import androidx.compose.ui.semantics.Role
import androidx.compose.ui.semantics.clearAndSetSemantics
import androidx.compose.ui.semantics.contentDescription
import androidx.compose.ui.semantics.heading
import androidx.compose.ui.semantics.selected
import androidx.compose.ui.semantics.semantics
import androidx.compose.ui.text.font.FontWeight
import androidx.compose.ui.text.input.ImeAction
import androidx.compose.ui.unit.Constraints
import androidx.compose.ui.unit.dp
import androidx.compose.ui.unit.sp
import com.example.data.GameSearch
import com.example.data.PlaceGameRow
import com.example.data.PlaceSheetUi
import com.example.data.PlayMode
import com.example.data.Suggestion
import com.example.model.GameRegistry
import com.example.ui.components.AREA_ORDER
import com.example.ui.components.AreaSquares
import com.example.ui.components.ClaySlab
import com.example.ui.components.Glyph
import com.example.ui.components.KitGlyph
import com.example.ui.components.ClayTextField
import com.example.ui.components.KitCloseButton
import com.example.ui.components.OnBoardText
import com.example.ui.components.SlabColor
import com.example.ui.components.SlabSize
import com.example.ui.i18n.LocalAppLanguage
import com.example.ui.theme.Clay
import com.example.ui.theme.NubiTokens
import com.example.viewmodel.NeuroVidaViewModel

/**
 * «Todos los juegos» (Tarea 158, «La isla es el mapa»): la ventana que abre la losa de Hoy para quien busca un juego POR SU NOMBRE (la isla es el mapa de los juegos, pero no hace falta saber en qué edificio vive cada uno). Ocupa toda la pantalla, sin la barra de abajo, y se cierra con su X o con Atrás.
 * De arriba abajo: el título con la X, un buscador grande («Busca un juego por su nombre»: ignora tildes y mayúsculas, `GameSearch`), los chips de área (Todas · Memoria · Atención · Razonamiento · Lenguaje) y los 17 juegos agrupados por área, cada uno con la MISMA fila de la lámina de un lugar ([PlaceGameRowItem]: emblema, nombre y
 * «cuándo»); tocar una fila abre la ficha del juego y de ahí se juega. Lo escrito, el chip y la fila marcada (el último juego que se jugó desde aquí, en sol) viven en el ViewModel ([com.example.viewmodel.AllGamesState]): al volver de una partida la ventana queda como se dejó.
 */
@Composable
fun AllGamesScreen(viewModel: NeuroVidaViewModel, modifier: Modifier = Modifier) {
  val state by viewModel.allGames.collectAsState()
  val s = state ?: return
  BackHandler { viewModel.closeAllGames() }
  val history by viewModel.gameHistory.collectAsState()
  val measures by viewModel.starMeasures.collectAsState()
  val daily by viewModel.dailySession.collectAsState()
  val lang = LocalAppLanguage.current
  val now = remember(history) { System.currentTimeMillis() }
  val lastPlayed = remember(history) { history.groupBy { it.gameId }.mapValues { (_, l) -> l.maxOf { it.timestamp } } }
  val pick = remember(daily, lastPlayed, now) { Suggestion.next(daily.gameIds, daily.completedCount, lastPlayed, now, com.example.data.Streak::dayOf) }
  val places = rememberAreaPlaces(history, measures, now, lang, pick, daily.gameIds, daily.completedCount)

  // Lo escrito vive en el campo (para que el teclado no salte) y se avisa al ViewModel en cada cambio, para volver a ello tras una partida.
  var query by rememberSaveable { mutableStateOf(s.query) }
  var sheetFor by remember { mutableStateOf<com.example.model.GameDefinition?>(null) }

  AllGamesContent(
    places = places, area = s.area, query = query, mark = s.mark,
    onQueryChange = { query = it.take(40); viewModel.setAllGamesQuery(query) },
    onArea = { viewModel.setAllGamesArea(it) },
    onClose = { viewModel.closeAllGames() },
    onGame = { id -> GameRegistry.getById(id)?.let { sheetFor = it } },
    modifier = modifier
  )

  // La ficha del juego tocado; al jugar desde ella la ventana se recuerda (con su búsqueda, su chip y la fila marcada) y se vuelve a ella al terminar.
  sheetFor?.let { g ->
    GameSheetHost(
      game = g, viewModel = viewModel, onDismiss = { sheetFor = null }, onPlay = { m: PlayMode, timed: Boolean -> sheetFor = null; viewModel.playFromHome(g.id, m, timed) },
      onTutorial = { sheetFor = null; viewModel.playTutorialFromHome(g.id) },           // «Cómo se juega» (Tarea 171): el tutorial otra vez
      onTimeTrial = { sheetFor = null; viewModel.startTimeTrial(g.id) }                  // «Contrarreloj» (Tarea 181): el de ESTE juego con la pista del día; al terminar vuelve aquí, como una partida de siempre
    )
  }
}

/**
 * Lo que dibuja «Todos los juegos» sin ViewModel (así se prueba y se captura con datos fijos): [places] = la lámina de cada área (de donde salen las filas), [area] = el chip elegido (la clave del área; null = Todas), [query] = lo escrito en el buscador y [mark] = el juego marcado.
 */
@Composable
internal fun AllGamesContent(
  places: Map<String, PlaceSheetUi>,
  area: String?,
  query: String,
  mark: String?,
  onQueryChange: (String) -> Unit,
  onArea: (String?) -> Unit,
  onClose: () -> Unit,
  onGame: (String) -> Unit,
  modifier: Modifier = Modifier
) {
  val focusManager = LocalFocusManager.current
  val sections = remember(places, area, query) { allGamesSections(places, area, query) }
  val flat = remember(sections) { sections.flatMap { (place, rows) -> listOf<Any>(AllGamesHeader(place, rows.size)) + rows.map { AllGamesRow(place, it) } } }

  Column(modifier.fillMaxSize()) {
    Row(Modifier.fillMaxWidth().padding(start = 20.dp, end = 12.dp, top = 8.dp, bottom = 4.dp), verticalAlignment = Alignment.CenterVertically) {
      OnBoardText("Todos los juegos", color = NubiTokens.OnNight, fontSize = 26.sp, fontWeight = FontWeight.Bold, fontFamily = com.example.ui.theme.TitleFamily, modifier = Modifier.weight(1f).semantics { heading() }.testTag("all_games_title"))
      KitCloseButton(onClose, Modifier.testTag("all_games_close"))
    }
    // El buscador y los chips quedan fijos arriba; con letra muy grande (> 1,3) ocupan casi la mitad de la pantalla y se deslizan con la lista
    val scrollControls = LocalDensity.current.fontScale > 1.3f
    val controls: @Composable () -> Unit = {
      Column(Modifier.padding(horizontal = if (scrollControls) 0.dp else 16.dp)) {
      ClayTextField(
        value = query, onValueChange = onQueryChange,
        placeholder = "Busca un juego por su nombre", fontSize = 20.sp,
        modifier = Modifier.testTag("all_games_search"),
        keyboardOptions = KeyboardOptions(imeAction = ImeAction.Search),
        keyboardActions = KeyboardActions(onSearch = { focusManager.clearFocus() })
      )
      AreaChips(places, area, onArea)
    }
    }
    if (!scrollControls) controls()
    val listState = rememberLazyListState()
    // Al volver de una partida lanzada desde aquí, la fila marcada se deja a la vista
    LaunchedEffect(Unit) {
      val i = flat.indexOfFirst { it is AllGamesRow && it.row.gameId == mark }
      if (i > 0) listState.scrollToItem(i + if (scrollControls) 1 else 0)          // (con letra muy grande el buscador y los chips son el primer elemento de la lista)
    }
    LazyColumn(Modifier.weight(1f).padding(horizontal = 16.dp).testTag("all_games_list"), state = listState, contentPadding = androidx.compose.foundation.layout.PaddingValues(top = 6.dp, bottom = 28.dp), verticalArrangement = Arrangement.spacedBy(8.dp)) {
      if (scrollControls) item { controls() }
      if (flat.isEmpty()) {
        item {
          OnBoardText(
            "No encuentro un juego con ese nombre. Prueba con otra palabra o elige «Todas».", color = NubiTokens.OnNightDim, fontSize = 18.sp, lineHeight = 24.sp,
            modifier = Modifier.padding(top = 16.dp).testTag("all_games_empty")
          )
        }
      }
      items(flat, key = { if (it is AllGamesHeader) "area_" + it.place.area else "game_" + (it as AllGamesRow).row.gameId }) { entry ->
        if (entry is AllGamesHeader) AreaHeader(entry.place, entry.count)
        else {
          entry as AllGamesRow
          PlaceGameRowItem(entry.row, suggested = entry.row.gameId == entry.place.suggestedGameId, marked = entry.row.gameId == mark, onClick = { onGame(entry.row.gameId) })
        }
      }
    }
  }
}

/** Una fila de la lista (el juego con la lámina de su área, de donde sale si es el que sugiere Nubi). */
private data class AllGamesRow(val place: PlaceSheetUi, val row: PlaceGameRow)

/** El encabezado de un área: su lámina y cuántos de sus juegos se ven ahora (con una búsqueda, no son todos). */
private data class AllGamesHeader(val place: PlaceSheetUi, val count: Int)

/**
 * Los juegos que se ven, agrupados por área: solo las áreas de [area] (null = todas) y solo los juegos cuyo nombre tiene lo escrito en [query] ([GameSearch]); un área sin juegos que coincidan no aparece.
 * Es la lista de la pantalla; vive aparte para probarla sin pantalla.
 */
internal fun allGamesSections(places: Map<String, PlaceSheetUi>, area: String?, query: String): List<Pair<PlaceSheetUi, List<PlaceGameRow>>> =
  AREA_ORDER.mapNotNull { key ->
    if (area != null && area != key) return@mapNotNull null
    val place = places[key] ?: return@mapNotNull null
    val rows = place.games.filter { GameSearch.matches(it.title, query) }
    if (rows.isEmpty()) null else place to rows
  }

/** El nombre del área con su punto de color (como en la lámina) y cuántos de sus juegos se ven. */
@Composable
private fun AreaHeader(place: PlaceSheetUi, count: Int) {
  Row(Modifier.fillMaxWidth().padding(top = 10.dp).semantics(mergeDescendants = true) { heading() }.testTag("all_area_${place.area.lowercase()}"), verticalAlignment = Alignment.CenterVertically) {
    AreaDot(place.area)
    Spacer(Modifier.width(8.dp))
    OnBoardText(place.areaName, color = NubiTokens.OnNight, fontSize = 20.sp, fontWeight = FontWeight.Bold, fontFamily = com.example.ui.theme.TitleFamily)
    Spacer(Modifier.width(8.dp))
    OnBoardText(if (count == 1) "1 juego" else "$count juegos", color = NubiTokens.OnNightDim, fontSize = 16.sp)
  }
}

/** El punto de color de un área (12 dp, con el borde de tinta): el del encabezado de la lista y el de su chip. Decorativo: el nombre ya dice el área. */
@Composable
private fun AreaDot(area: String, modifier: Modifier = Modifier) {
  Box(modifier.size(12.dp).clip(CircleShape).background(placeColor(area)).border(2.dp, Clay.Ink, CircleShape))
}

/**
 * Los cinco chips de área: «Todas» y las cuatro (Tarea 196, 30-oct: Ricardo los veía «desordenados», con anchos distintos, un borde dentado, sin el color del área y dos soles en pantalla; eligió la variante «B ajustada, con color»).
 * Van en filas parejas ([AreaChipGrid]); cada uno lleva el color de su área en un hueco antes del nombre (el punto de 12 dp; «Todas», las cuatro casillas) y el ELEGIDO cambia ese hueco por el visto: lima, visto y negrita.
 */
@Composable
private fun AreaChips(places: Map<String, PlaceSheetUi>, area: String?, onArea: (String?) -> Unit) {
  val items = listOf<Pair<String?, String>>(null to "Todas") + AREA_ORDER.map { it to (places[it]?.areaName ?: it) }
  AreaChipGrid(Modifier.fillMaxWidth().padding(top = 6.dp)) {
    items.forEach { (key, name) -> AreaChip(name, area == key, "all_chip_${key?.lowercase() ?: "todas"}", key) { onArea(key) } }
  }
}

/** Cuántos chips caben, como máximo, en una fila de los de área (arriba tres, abajo dos). */
private const val AREA_CHIPS_PER_ROW = 3

/**
 * Los chips en filas parejas: cada fila reparte el ancho entero en partes iguales (borde derecho recto) y una fila con un solo chip lo toma a todo el ancho. Cuántos entran en una fila lo decide el chip MÁS ANCHO de esa fila: se ponen tantos
 * como quepan, hasta [AREA_CHIPS_PER_ROW], con la parte igual ≥ lo que pide el más ancho. Las filas se pegan (la losa ya deja 6 dp de sombra abajo); entre chips hay 8 dp. Pide un ancho acotado.
 *
 * ¿Por qué no un `FlowRow` con `Modifier.weight(1f)`? Sus pesos reparten el ancho de la fila SIN mirar lo que mide cada chip, y el `FlowRow` decide cuántos entran SUMANDO anchos, no por el más ancho: con una fila de tres, «Atención» elegida (con su visto) pedía unos 117 dp a letra 1,0
 * y unos 137 a 1,3 y recibía una parte de 115 dp, y se veía «Atenció✓»: una palabra cortada. Aquí la parte igual siempre alcanza para el más ancho; si no cabe, la fila tiene menos chips (a 360 dp o con letra grande salen 2 + 2 + 1, y con 1,3, filas sueltas). Se probó con cajas de ancho fijo y con los chips reales (`AreaChipsTest`).
 */
@Composable
private fun AreaChipGrid(modifier: Modifier = Modifier, content: @Composable () -> Unit) {
  Layout(content, modifier) { measurables, constraints ->
    val gap = 8.dp.roundToPx()
    val width = constraints.maxWidth
    val natural = measurables.map { it.maxIntrinsicWidth(Int.MAX_VALUE) }
    val rows = ArrayList<List<Int>>()
    var i = 0
    while (i < measurables.size) {
      var k = minOf(AREA_CHIPS_PER_ROW, measurables.size - i)
      while (k > 1) {
        val share = (width - (k - 1) * gap) / k
        if ((i until i + k).all { natural[it] <= share }) break
        k--
      }
      rows += (i until i + k).toList()
      i += k
    }
    var y = 0
    val placed = ArrayList<Triple<Placeable, Int, Int>>()
    for (row in rows) {
      val k = row.size
      val room = width - (k - 1) * gap
      var x = 0
      val rowPlaceables = row.mapIndexed { n, idx ->
        val w = room / k + if (n < room % k) 1 else 0                       // los píxeles que sobran de la división se reparten de a uno: la fila termina justo en el borde
        measurables[idx].measure(Constraints(minWidth = w, maxWidth = w)).also { p -> placed += Triple(p, x, y); x += w + gap }
      }
      y += rowPlaceables.maxOf { it.height }
    }
    layout(width, y) { placed.forEach { (p, px, py) -> p.placeRelative(px, py) } }
  }
}

/**
 * Un chip de área: una losa chica del kit, crema; el ELEGIDO va en LIMA, con el visto del kit y en negrita (la misma forma de «elegida» que el Primer vuelo: color, forma y texto, nunca solo color) y no en sol, para que el único sol de la pantalla sea «Nubi sugiere».
 * [areaKey] = el área (null = «Todas»). Antes del nombre va un hueco con el color del área: el punto de 12 dp centrado en 16 dp (en «Todas», las cuatro casillas, en 20 dp), con 4 dp hasta el nombre; el visto de la elegida ocupa ese MISMO hueco, así que elegir no cambia el ancho
 * del chip. El relleno lateral es de 12 dp. El punto y las casillas son decorativos: el lector de pantalla dice solo el nombre y, si es la elegida, «…, elegida». Toca 52 dp de alto como mínimo.
 */
@Composable
private fun AreaChip(label: String, selected: Boolean, tag: String, areaKey: String?, onClick: () -> Unit) {
  ClaySlab(
    color = if (selected) SlabColor.LIME else SlabColor.CREAM, size = SlabSize.SMALL, onClick = onClick, role = Role.RadioButton, contentAlignment = Alignment.Center,
    modifier = Modifier.heightIn(min = 52.dp).semantics(mergeDescendants = true) { this.selected = selected; contentDescription = if (selected) "$label, elegida" else label }.testTag(tag)
  ) {
    Row(Modifier.padding(horizontal = 12.dp, vertical = 8.dp), verticalAlignment = Alignment.CenterVertically) {
      val hole = if (areaKey == null) 20.dp else 16.dp
      Box(Modifier.size(hole), contentAlignment = Alignment.Center) {
        if (selected) Box(Modifier.size(hole).testTag("all_chip_check")) { KitGlyph(Glyph.CHECK, size = hole) }
        else if (areaKey == null) AreaSquares(size = 20.dp)
        else AreaDot(areaKey, Modifier.testTag("all_chip_dot_${areaKey.lowercase()}"))
      }
      Spacer(Modifier.width(4.dp))
      // El nombre se MIDE siempre con sus dos pesos: dos copias invisibles (negrita y normal) reservan el ancho y el alto del peor caso de cada uno, y el nombre que se ve va encima, pegado al hueco. Así elegir un chip nunca lo ensancha ni lo agranda:
      // ni cambia cuántos chips entran en cada fila ni mueve la lista de abajo. La negrita (Fredoka) es más ancha que la normal (Nunito) y la normal, con letra grande, un poco más alta que la negrita (3 dp con la letra al 200 %).
      Box(contentAlignment = Alignment.CenterStart) {
        Text(label, color = Clay.Ink, fontSize = 16.sp, fontWeight = FontWeight.Bold, lineHeight = 20.sp, modifier = Modifier.alpha(0f).clearAndSetSemantics { })
        Text(label, color = Clay.Ink, fontSize = 16.sp, fontWeight = FontWeight.Medium, lineHeight = 20.sp, modifier = Modifier.alpha(0f).clearAndSetSemantics { })
        Text(label, color = Clay.Ink, fontSize = 16.sp, fontWeight = if (selected) FontWeight.Bold else FontWeight.Medium, lineHeight = 20.sp, modifier = Modifier.testTag("${tag}_text"))
      }
    }
  }
}
