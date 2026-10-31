package com.example.ui.screens

import androidx.compose.foundation.background
import androidx.compose.foundation.border
import androidx.compose.foundation.layout.Arrangement
import androidx.compose.foundation.layout.Box
import androidx.compose.foundation.layout.BoxWithConstraints
import androidx.compose.foundation.layout.Column
import androidx.compose.foundation.layout.ColumnScope
import androidx.compose.foundation.layout.IntrinsicSize
import androidx.compose.foundation.layout.Row
import androidx.compose.foundation.layout.Spacer
import androidx.compose.foundation.layout.fillMaxHeight
import androidx.compose.foundation.layout.fillMaxSize
import androidx.compose.foundation.layout.fillMaxWidth
import androidx.compose.foundation.layout.height
import androidx.compose.foundation.layout.heightIn
import androidx.compose.foundation.layout.padding
import androidx.compose.foundation.layout.size
import androidx.compose.foundation.layout.width
import androidx.compose.foundation.layout.widthIn
import androidx.compose.foundation.rememberScrollState
import androidx.compose.foundation.shape.CircleShape
import androidx.compose.foundation.shape.RoundedCornerShape
import androidx.compose.foundation.verticalScroll
import androidx.compose.material3.Text
import androidx.compose.runtime.Composable
import androidx.compose.runtime.CompositionLocalProvider
import androidx.compose.runtime.getValue
import androidx.compose.runtime.mutableIntStateOf
import androidx.compose.runtime.remember
import androidx.compose.runtime.setValue
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.draw.clip
import androidx.compose.ui.draw.drawBehind
import androidx.compose.ui.geometry.Offset
import androidx.compose.ui.geometry.Size
import androidx.compose.ui.graphics.Brush
import androidx.compose.ui.graphics.Color
import androidx.compose.ui.layout.onSizeChanged
import androidx.compose.ui.platform.LocalDensity
import androidx.compose.ui.platform.testTag
import androidx.compose.ui.semantics.Role
import androidx.compose.ui.semantics.clearAndSetSemantics
import androidx.compose.ui.semantics.contentDescription
import androidx.compose.ui.semantics.heading
import androidx.compose.ui.semantics.selected
import androidx.compose.ui.semantics.SemanticsProperties
import androidx.compose.ui.semantics.semantics
import androidx.compose.ui.text.TextStyle
import androidx.compose.ui.text.font.FontWeight
import androidx.compose.ui.text.rememberTextMeasurer
import androidx.compose.ui.text.style.TextAlign
import androidx.compose.ui.unit.Density
import androidx.compose.ui.unit.Dp
import androidx.compose.ui.unit.dp
import androidx.compose.ui.unit.sp
import com.example.data.TimeTrialDay
import com.example.data.TimeTrialMarks
import com.example.data.TimeTrialResult
import com.example.data.TimeTrialStats
import com.example.data.glueNumbers
import com.example.model.AppLanguage
import com.example.model.GameRegistry
import com.example.ui.components.ClaySlab
import com.example.ui.components.GameEmblem
import com.example.ui.components.Glyph
import com.example.ui.components.KitBackButton
import com.example.ui.components.KitGlyph
import com.example.ui.components.OnBoardText
import com.example.ui.components.SlabColor
import com.example.ui.components.SlabSize
import com.example.ui.i18n.getGameTitle
import com.example.ui.theme.AppFamily
import com.example.ui.theme.Clay
import com.example.ui.theme.ClayCard
import com.example.ui.theme.NubiTokens
import com.example.ui.theme.TitleFamily

/** Lo que baja el cielo de la cabecera hasta el noche plano de debajo (Fable §11.2): 24 dp entre el subtítulo y el selector. */
private val FadeHeight = 24.dp

/** Margen de la página (las losas miden el ancho menos esto a cada lado) y aire entre losas (cuadrícula de 8 dp). */
private val PageMargin = 16.dp
private val CardGap = 16.dp

/** El alto de la barra más larga de «Tus últimas 10 marcas» y el de la más corta (una partida con 0 aciertos sigue viéndose). */
private val BarMax = 96.dp
private val BarMin = 8.dp
private val BarWidthMax = 36.dp

/** La letra de los números y títulos grandes de la página no pasa de 1,3 (con 2,0, un título de 52 sp se salía del margen: Fable §11.6). */
private const val BIG_TEXT_SCALE_CAP = 1.3f

/**
 * La PÁGINA «Contrarreloj» de Avance (Tarea 199; `auditoria-fable/estadisticas-contrarreloj.md` §2.1, §3 y §11; solo con Nubi Plus): TODAS tus marcas del Contrarreloj, comparadas solo contigo y sin servidor (`TimeTrialMarks`: lo guardado en `time_trial_results`). Se abre como la página de un
 * área (reemplaza la lista de Avance; Atrás vuelve; la barra de pestañas sigue en «Avance») y se desliza entera.
 *
 * De arriba abajo: la cabecera sobre el cielo («Contrarreloj» y «Tus marcas, comparadas solo contigo.», con [OnBoardText]); el cielo se DESVANECE en 24 dp hasta el noche plano ([NubiTokens.NightFlat]) y de ahí para abajo todo va en losas crema sobre ese fondo plano, sin luceros ni luna entre ellas;
 * el selector de los cuatro juegos (dos filas parejas; una columna si «Acoplamiento» no cabe entero en media fila; el elegido en lima con el visto; abre con el juego de hoy), «Tu récord» (la cifra grande con su fecha y su paso, y adentro «Tus últimas 10 marcas»: barras de plasticina con el récord en sol y la moneda del visto,
 * a 5 barras si las cifras no caben a 16 sp), «Tu ritmo en la última partida» (Tarea 201: los aciertos y los errores de cada medio minuto), «Esta semana y la pasada», «Aciertos y errores» y, al final, «Pistas anteriores» (solo si hay) con la nota de siempre.
 * Desde la Tarea 201 (opción B de Fable, `auditoria-fable/nubi-plus-comparacion.md` §11): una línea fina separa lo que una losa junta (la cifra del récord y sus marcas, las dos semanas, los dos pares de barras y la nota) y los gráficos de barras van en una ventanilla noche ([NightWindow]); las barras horizontales de «Aciertos y errores» siguen en crema.
 *
 * Todo a 16 sp o más (14 sp solo los rótulos del eje de las barras); cada número con su unidad y en frase completa; nada solo por color (el récord lleva moneda con visto y la palabra; los errores, su palabra). [results] = TODAS las partidas del Contrarreloj; [selected] = el juego elegido; [now] = el momento de mirar.
 */
@Composable
internal fun ContrarrelojPage(
  results: List<TimeTrialResult>,
  selected: String,
  onSelect: (String) -> Unit,
  onBack: () -> Unit,
  now: Long,
  modifier: Modifier = Modifier,
  lang: AppLanguage = com.example.ui.i18n.LocalAppLanguage.current
) {
  val page = remember(results, selected, now) { TimeTrialMarks.page(results, selected, now) }
  val records = remember(results) { TimeTrialDay.GAMES.map { it to TimeTrialStats.record(results, it) } }
  val pace = remember(results, selected) { TimeTrialMarks.pace(results, selected) }
  val scroll = rememberScrollState()
  var headerPx by remember { mutableIntStateOf(0) }
  val fadePx = with(LocalDensity.current) { FadeHeight.toPx() }
  val barInsetPx = with(LocalDensity.current) { com.example.ui.components.LocalBarInset.current.toPx() }
  Box(
    modifier.fillMaxSize().testTag("contrarreloj_page").drawBehind {
      // el noche plano nace donde termina el subtítulo (con el scroll) y llega hasta abajo, también bajo la barra de pestañas: el cielo no vuelve a asomar entre las losas
      val top = (headerPx - scroll.value).toFloat()
      if (top < size.height + barInsetPx) {
        val start = top.coerceAtLeast(-fadePx)
        drawRect(
          Brush.verticalGradient(listOf(Color.Transparent, NubiTokens.NightFlat), startY = top, endY = top + fadePx),
          topLeft = Offset(0f, start), size = Size(size.width, size.height + barInsetPx - start)
        )
      }
    }
  ) {
    Column(Modifier.fillMaxSize().verticalScroll(scroll).testTag("contrarreloj_scroll")) {
      PageHeader(onBack, Modifier.onSizeChanged { headerPx = it.height })
      Spacer(Modifier.height(FadeHeight))
      Column(Modifier.padding(horizontal = PageMargin).padding(bottom = 32.dp), verticalArrangement = Arrangement.spacedBy(CardGap)) {
        GameSelector(records, selected, lang, onSelect)
        RecordCard(page, now)
        if (page.hasMarks) {
          RhythmCard(pace)
          WeeksCard(page)
          SplitCard(page)
        }
        NoteCard(page)
      }
    }
  }
}

// ------------------------------------------------------------------ la cabecera, sobre el cielo

/** El disco de volver, «Contrarreloj» (Rubik, con tope de letra 1,3) y su subtítulo, que parte la línea por palabras bajo el título (del disco al margen). Todo con [OnBoardText]: el cielo tiene luceros. */
@Composable
private fun PageHeader(onBack: () -> Unit, modifier: Modifier = Modifier) {
  Column(modifier.fillMaxWidth().padding(start = PageMargin, end = PageMargin, top = 8.dp).testTag("contrarreloj_header")) {
    Row(Modifier.fillMaxWidth(), verticalAlignment = Alignment.CenterVertically) {
      KitBackButton(onBack, Modifier.testTag("contrarreloj_back"), label = "Volver a Avance")
      Spacer(Modifier.width(16.dp))
      CapScale(BIG_TEXT_SCALE_CAP) {
        OnBoardText(TimeTrialMarks.TITLE, color = NubiTokens.OnNight, fontSize = 26.sp, fontWeight = FontWeight.Bold, fontFamily = TitleFamily, lineHeight = 32.sp, modifier = Modifier.weight(1f).semantics { heading() }.testTag("contrarreloj_title"))
      }
    }
    Row(Modifier.fillMaxWidth()) {
      Spacer(Modifier.width(64.dp))
      OnBoardText(TimeTrialMarks.SUBTITLE, color = NubiTokens.OnNightDim, fontSize = 16.sp, lineHeight = 22.sp, modifier = Modifier.weight(1f).testTag("contrarreloj_subtitle"))
    }
  }
}

// ------------------------------------------------------------------ el selector de los cuatro juegos

/**
 * Los cuatro juegos en losas: el emblema, el nombre y su récord («44 aciertos» o «sin marca aún»); el ELEGIDO va en lima con el visto sobre su emblema (forma y color, nunca solo color) y el lector de pantalla dice «Tinta o Palabra, récord 44 aciertos, elegido». Dos filas de dos, parejas (cada fila tiene el alto de su losa
 * más alta); si la palabra más larga («Acoplamiento») no cabe entera en media fila a la letra de la persona, UNA columna: una palabra nunca se parte. Cada losa se toca en 72 dp o más (la losa pide 78: 6 dp son su sombra).
 */
@Composable
private fun GameSelector(records: List<Pair<String, Int?>>, selected: String, lang: AppLanguage, onSelect: (String) -> Unit) {
  val measurer = rememberTextMeasurer()
  val base = androidx.compose.material3.LocalTextStyle.current
  val density = LocalDensity.current
  val titles = records.map { (id, _) -> GameRegistry.getById(id)?.let { getGameTitle(it.id, lang, it.title) } ?: id }
  BoxWithConstraints(Modifier.fillMaxWidth().testTag("contrarreloj_selector")) {
    val gapPx = with(density) { 8.dp.roundToPx() }
    val textRoomPx = (constraints.maxWidth - gapPx) / 2 - with(density) { (TilePad * 2 + TileEmblem + TileGap).roundToPx() }
    val nameStyle = base.merge(TextStyle(fontSize = 16.sp, fontWeight = FontWeight.Bold, fontFamily = AppFamily))
    val wordsFit = titles.all { t -> t.split(' ').all { w -> measurer.measure(w, nameStyle, softWrap = false, maxLines = 1).size.width <= textRoomPx } }
    val valuesFit = records.all { (_, r) -> measurer.measure(TimeTrialMarks.tileValue(r), base.merge(TextStyle(fontSize = 16.sp)), softWrap = false, maxLines = 1).size.width <= textRoomPx }
    val cols = if (wordsFit && valuesFit) 2 else 1
    Column(verticalArrangement = Arrangement.spacedBy(8.dp)) {
      records.indices.chunked(cols).forEach { row ->
        Row(Modifier.fillMaxWidth().height(IntrinsicSize.Min), horizontalArrangement = Arrangement.spacedBy(8.dp)) {
          row.forEach { i ->
            val (id, record) = records[i]
            GameTile(id, titles[i], record, id == selected, { onSelect(id) }, Modifier.weight(1f).fillMaxHeight())
          }
        }
      }
    }
  }
}

private val TilePad = 10.dp
private val TileEmblem = 36.dp
private val TileGap = 10.dp

@Composable
private fun GameTile(gameId: String, title: String, record: Int?, selected: Boolean, onClick: () -> Unit, modifier: Modifier) {
  ClaySlab(
    color = if (selected) SlabColor.LIME else SlabColor.CREAM, size = SlabSize.SMALL, onClick = onClick, role = Role.RadioButton, contentAlignment = Alignment.CenterStart,
    modifier = modifier.heightIn(min = 78.dp).semantics(mergeDescendants = true) { this.selected = selected; contentDescription = TimeTrialMarks.tileSpoken(title, record, selected) }.testTag("contrarreloj_game_$gameId")
  ) {
    Row(Modifier.fillMaxWidth().padding(horizontal = TilePad, vertical = TilePad), verticalAlignment = Alignment.CenterVertically) {
      GameEmblem(gameId, TileEmblem, Modifier.testTag("contrarreloj_game_emblem_$gameId"), done = selected)
      Spacer(Modifier.width(TileGap))
      Column(Modifier.weight(1f)) {
        Text(title, color = Clay.Ink, fontSize = 16.sp, fontWeight = FontWeight.Bold, fontFamily = AppFamily, lineHeight = 20.sp, modifier = Modifier.testTag("contrarreloj_game_name_$gameId"))
        Spacer(Modifier.height(4.dp))
        Text(TimeTrialMarks.tileValue(record).glueNumbers(), color = if (selected) Clay.Ink else Clay.InkMuted, fontSize = 16.sp, lineHeight = 20.sp, modifier = Modifier.testTag("contrarreloj_game_value_$gameId"))
      }
    }
  }
}

// ------------------------------------------------------------------ las losas de información

/** Una losa crema de la página: título en Rubik (24 dp de alto) con 16 dp de aire antes del contenido, igual en todas; 16 dp de relleno. */
@Composable
private fun InfoCard(tag: String, title: String?, modifier: Modifier = Modifier, content: @Composable ColumnScope.() -> Unit) {
  ClayCard(modifier.fillMaxWidth().testTag(tag), size = SlabSize.LARGE, contentPadding = 16.dp) {
    if (title != null) {
      Text(title, color = Clay.Ink, fontSize = 20.sp, fontWeight = FontWeight.Bold, fontFamily = TitleFamily, lineHeight = 24.sp, modifier = Modifier.semantics { heading() }.testTag("${tag}_title"))
      Spacer(Modifier.height(16.dp))
    }
    content()
  }
}

/** «Tu récord»: la cifra grande («44 aciertos», letra con tope 1,3), su fecha y unidad, el paso de la pista y, adentro, «Tus últimas 10 marcas» con las barras y los dos promedios. Sin marcas, una frase que dice qué falta. */
@Composable
private fun RecordCard(page: TimeTrialMarks.Page, now: Long) {
  InfoCard("contrarreloj_record", TimeTrialMarks.RECORD_TITLE) {
    val record = page.record
    if (record == null) {
      Text("Todavía no tienes marcas en este juego. Cuando juegues su Contrarreloj, aquí aparece tu récord.", color = Clay.Ink, fontSize = 16.sp, lineHeight = 22.sp, modifier = Modifier.testTag("contrarreloj_record_empty"))
      return@InfoCard
    }
    CapScale(BIG_TEXT_SCALE_CAP) {
      Text(TimeTrialDay.hitsText(record.mark).glueNumbers(), color = Clay.Ink, fontSize = 34.sp, fontWeight = FontWeight.Bold, fontFamily = TitleFamily, lineHeight = 40.sp, modifier = Modifier.testTag("contrarreloj_record_figure"))
    }
    Text(TimeTrialMarks.recordWhen(record, now).glueNumbers(), color = Clay.InkMuted, fontSize = 16.sp, lineHeight = 22.sp, modifier = Modifier.padding(top = 4.dp).testTag("contrarreloj_record_when"))
    Spacer(Modifier.height(4.dp))
    Text(TimeTrialStats.stepLine(record.level, page.maxLevel).glueNumbers(), color = Clay.Ink, fontSize = 16.sp, fontWeight = FontWeight.Bold, lineHeight = 22.sp, modifier = Modifier.testTag("contrarreloj_record_step"))
    ClayDivider("contrarreloj_divider_record")
    MarksChart(page, now)
  }
}

/**
 * «Tus últimas 10 marcas» (en la ventanilla noche, Tarea 201): una barra por partida, de la más vieja a la más nueva, con la cifra encima (todas en UNA fila), una base común, el récord en sol con la moneda del visto en su propia fila, y bajo la base solo dos rótulos de 14 sp: la fecha de la PRIMERA barra que se ve y «hoy». Si las diez cifras de 16 sp
 * no caben con 4 dp de aire (letra grande), se dibujan las últimas 5 (y la moneda solo si el récord sigue entre ellas); con menos partidas, las que haya, sin estirarse. Con una sola, la frase «Tu primera marca. Desde la segunda, se dibujan aquí.». Debajo, los dos promedios en palabras. El lector de pantalla dice las cifras de corrido (`TimeTrialMarks.barsSpoken`).
 */
@Composable
private fun MarksChart(page: TimeTrialMarks.Page, now: Long) {
  val measurer = rememberTextMeasurer()
  val base = androidx.compose.material3.LocalTextStyle.current
  val density = LocalDensity.current
  Column(Modifier.testTag("contrarreloj_chart")) {
    if (page.marks.size < 2) {
      Text(TimeTrialMarks.barsTitle(1), color = Clay.Ink, fontSize = 18.sp, fontWeight = FontWeight.Bold, fontFamily = AppFamily, lineHeight = 24.sp, modifier = Modifier.semantics { heading() }.testTag("contrarreloj_chart_title"))
      Spacer(Modifier.height(8.dp))
      Text(TimeTrialMarks.FIRST_MARK, color = Clay.Ink, fontSize = 16.sp, lineHeight = 22.sp, modifier = Modifier.testTag("contrarreloj_chart_first"))
      return@Column
    }
    BoxWithConstraints(Modifier.fillMaxWidth()) {
      // las barras van en la ventanilla noche: el ancho de las barras es el de la losa menos el relleno de la ventanilla a cada lado
      val inner = maxWidth - NightPad * 2
      val innerPx = constraints.maxWidth - with(density) { (NightPad * 2).roundToPx() }
      val figureStyle = base.merge(TextStyle(fontSize = 16.sp, fontWeight = FontWeight.Bold, fontFamily = AppFamily))
      val widest = page.marks.maxOf { measurer.measure(it.mark.toString(), figureStyle, softWrap = false, maxLines = 1).size.width }
      val airPx = with(density) { 4.dp.toPx() }
      val capacity = if (page.marks.size > TimeTrialMarks.HALF && innerPx / TimeTrialMarks.LAST_N - widest >= airPx) TimeTrialMarks.LAST_N else TimeTrialMarks.HALF
      val shown = page.marks.takeLast(capacity)
      val recordIndex = page.recordIndex?.let { it - (page.marks.size - shown.size) }?.takeIf { it >= 0 }
      val slot = inner / capacity
      val groupWidth = slot * shown.size
      val most = shown.maxOf { it.mark }.coerceAtLeast(1)
      Column {
        Text(TimeTrialMarks.barsTitle(shown.size), color = Clay.Ink, fontSize = 18.sp, fontWeight = FontWeight.Bold, fontFamily = AppFamily, lineHeight = 24.sp, modifier = Modifier.semantics { heading() }.testTag("contrarreloj_chart_title"))
        Spacer(Modifier.height(12.dp))
        NightWindow("contrarreloj_chart_window") {
          Column(Modifier.width(groupWidth).semantics { contentDescription = TimeTrialMarks.barsSpoken(shown, recordIndex, now) }.testTag("contrarreloj_bars")) {
            Row {
              shown.forEachIndexed { i, r ->
                val isRecord = i == recordIndex
                Column(Modifier.width(slot), horizontalAlignment = Alignment.CenterHorizontally) {
                  // la moneda del visto en su propia fila (solo con récord a la vista; las demás columnas dejan el hueco), 4 dp hasta la cifra
                  if (recordIndex != null) Box(Modifier.height(20.dp), contentAlignment = Alignment.Center) { if (isRecord) Coin() }
                  Spacer(Modifier.height(4.dp))
                  Text(r.mark.toString(), color = if (isRecord) NubiTokens.SunOnNight else NubiTokens.OnNight, fontSize = 16.sp, fontWeight = if (isRecord) FontWeight.Bold else FontWeight.Medium, fontFamily = AppFamily, lineHeight = 20.sp, textAlign = TextAlign.Center, softWrap = false, modifier = Modifier.height(with(density) { 20.sp.toDp() }).hiddenTag("contrarreloj_figure_$i"))
                  Spacer(Modifier.height(4.dp))
                  // la base común: las barras se apoyan abajo, todas en la misma línea
                  Box(Modifier.height(BarMax), contentAlignment = Alignment.BottomCenter) {
                    val shape = RoundedCornerShape(topStart = 8.dp, topEnd = 8.dp)
                    Box(
                      Modifier.width(minOf(BarWidthMax, slot - 8.dp)).height(BarMin + (BarMax - BarMin) * (r.mark.toFloat() / most)).clip(shape)
                        .background(if (isRecord) Clay.Sun else Clay.Grape).hiddenTag("contrarreloj_barshape_$i")
                    )
                  }
                }
              }
            }
            Box(Modifier.fillMaxWidth().height(2.dp).background(NubiTokens.OnNightDim.copy(alpha = 0.4f)).hiddenTag("contrarreloj_baseline"))
            Spacer(Modifier.height(4.dp))
            AxisLabels(TimeTrialMarks.axisLabels(shown, now), groupWidth)
          }
        }
        TimeTrialMarks.averagesSentence(page)?.let {
          Spacer(Modifier.height(16.dp))
          Text(it.glueNumbers(), color = Clay.Ink, fontSize = 16.sp, fontWeight = FontWeight.Bold, lineHeight = 22.sp, modifier = Modifier.testTag("contrarreloj_averages"))
        }
      }
    }
  }
}

/** La moneda lima con el visto del récord (forma y color: nunca solo color): 20 dp con el borde de tinta del kit. */
@Composable
private fun Coin() {
  Box(Modifier.size(20.dp).clip(CircleShape).background(Clay.Lime).border(2.dp, Clay.Ink, CircleShape).hiddenTag("contrarreloj_coin"), contentAlignment = Alignment.Center) { KitGlyph(Glyph.CHECK, size = 12.dp) }
}

/** Los dos rótulos del eje (14 sp, los únicos de la página bajo 16): la fecha de la primera barra que se ve a la izquierda y «hoy» a la derecha; si no queda 8 dp entre los dos con la letra de la persona, se omite el de la derecha. */
@Composable
private fun AxisLabels(labels: Pair<String, String?>?, width: androidx.compose.ui.unit.Dp) {
  if (labels == null) return
  val measurer = rememberTextMeasurer()
  val style = androidx.compose.material3.LocalTextStyle.current.merge(TextStyle(fontSize = 14.sp))
  val density = LocalDensity.current
  val (left, right) = labels
  val fits = right != null && with(density) {
    measurer.measure(left, style, softWrap = false, maxLines = 1).size.width + 8.dp.roundToPx() + measurer.measure(right, style, softWrap = false, maxLines = 1).size.width <= width.roundToPx()
  }
  Row(Modifier.width(width), verticalAlignment = Alignment.Top) {
    Text(left, color = NubiTokens.OnNightDim, fontSize = 14.sp, lineHeight = 18.sp, softWrap = false, modifier = Modifier.hiddenTag("contrarreloj_axis_left"))
    if (fits && right != null) {
      Spacer(Modifier.weight(1f))
      Text(right, color = NubiTokens.OnNightDim, fontSize = 14.sp, lineHeight = 18.sp, softWrap = false, modifier = Modifier.hiddenTag("contrarreloj_axis_right"))
    }
  }
}

// ------------------------------------------------------------------ la ventanilla noche y la línea de separación (Tarea 201)

/** El relleno de la ventanilla noche (Fable §11.4 B: relleno 12 dp, esquina 12 dp). */
private val NightPad = 12.dp

/**
 * La ventanilla noche de los gráficos (Tarea 201, opción B de Fable, `auditoria-fable/nubi-plus-comparacion.md` §11.4): un recuadro plano `#100F3A` ([NubiTokens.NightWindow], el tono del medio del cielo) de esquina 12 dp y relleno 12 dp, sin borde ni sombra, dentro de la losa crema (lo calibrado sobre oscuro va en su ventanilla).
 * Sobre ella, la paleta de noche: barras lila `Clay.Grape` sin borde, el récord en `Clay.Sun`, cifras en `OnNight` (la resaltada, en `SunOnNight`) y rótulos del eje en `OnNightDim`. No se toca: informa. La etiqueta de prueba cubre la ventanilla entera, con su relleno.
 */
@Composable
private fun NightWindow(tag: String, modifier: Modifier = Modifier, content: @Composable ColumnScope.() -> Unit) {
  Column(modifier.fillMaxWidth().testTag(tag).clip(RoundedCornerShape(12.dp)).background(NubiTokens.NightWindow).padding(NightPad), content = content)
}

/**
 * La línea de separación DENTRO de una losa (Fable §11.3): 1 dp, tinta al 14 % (el tono del canal de `KitBar`), de margen a margen de la losa, con 16 dp de aire arriba y 16 abajo (un bloque de 33 dp que cae en la cuadrícula de 8). Nunca sombra ni relieve: es orden, no brillo.
 * Va donde una losa junta dos ideas; no en «Pistas anteriores» ni en «Tu ritmo» (una sola idea por losa). [top] es el aire de arriba: 12 dp tras una fila «rótulo … cifra», que ya trae 4 dp propios (a la vista, 16 y 16).
 */
@Composable
private fun ClayDivider(tag: String, top: Dp = 16.dp) {
  Box(Modifier.fillMaxWidth().padding(top = top, bottom = 16.dp).height(1.dp).background(Clay.Ink.copy(alpha = 0.14f)).hiddenTag(tag))
}

// ------------------------------------------------------------------ «Tu ritmo en la última partida» (Tarea 201; los datos son los de la Tarea 200)

/** Las cifras y los rótulos del eje del gráfico del ritmo no pasan de la escala 1,3 (con 2,0, dos dígitos de 16 sp ya no caben en su columna sin tocar a la vecina, ni «120 s» de 14 sp en un cuarto de la ventanilla sin dejar la «s» sola). */
private const val RHYTHM_TEXT_CAP = 1.3f

/**
 * «Tu ritmo en la última partida» (Fable §2.3): cuatro medios minutos (0 a 30, 30 a 60, 60 a 90 y 90 a 120 s) de la última partida con el ritmo guardado, en la ventanilla noche: los aciertos en barras lila y los errores en barras coral (la misma escala), las cifras en UNA fila encima de las barras
 * (el medio minuto mejor, en `SunOnNight` y negrita; los errores, solo cuando hubo), el eje debajo, la leyenda en palabras («Barras lila: aciertos. Barras coral: errores.») y la frase que lee el gráfico ([TimeTrialMarks.Pace.sentence]). Se cuenta el ritmo: nunca un tiempo por ensayo ni una comparación.
 * Con marcas pero sin ninguna partida con ritmo (las de antes de la versión 17), solo la frase que dice cuándo aparece. Sin marcas en el juego, la losa no está (la página no la pone).
 */
@Composable
private fun RhythmCard(pace: TimeTrialMarks.Pace?) {
  InfoCard("contrarreloj_rhythm", TimeTrialMarks.PACE_TITLE) {
    if (pace == null) {
      Text(TimeTrialMarks.PACE_WAIT, color = Clay.Ink, fontSize = 16.sp, lineHeight = 22.sp, modifier = Modifier.testTag("contrarreloj_rhythm_wait"))
      return@InfoCard
    }
    RhythmChart(pace)
    Spacer(Modifier.height(12.dp))
    Text(TimeTrialMarks.PACE_LEGEND, color = Clay.InkMuted, fontSize = 16.sp, lineHeight = 22.sp, modifier = Modifier.testTag("contrarreloj_rhythm_legend"))
    Spacer(Modifier.height(12.dp))
    Text(pace.sentence.glueNumbers(), color = Clay.Ink, fontSize = 16.sp, fontWeight = FontWeight.Bold, lineHeight = 22.sp, modifier = Modifier.testTag("contrarreloj_rhythm_sentence"))
  }
}

/**
 * El gráfico: cada medio minuto es una columna con dos barras (aciertos a la izquierda, errores a la derecha, más angosta) que parten de una base común y se miden con la MISMA escala; un medio minuto sin aciertos o sin errores no dibuja esa barra (el 0 de los aciertos sí se escribe).
 * El ancho de cada columna sale del de la ventanilla (un cuarto, menos 8 dp de aire entre columnas), así que cabe también a 360 dp; las cifras y los rótulos del eje pasan a lo sumo a 1,3 de escala, y estos parten la línea por palabras si no caben (con «a» pegada al número).
 * El lector de pantalla dice el gráfico entero, medio minuto por medio minuto ([TimeTrialMarks.paceSpoken]); las barras y las cifras sueltas no se leen aparte.
 */
@Composable
private fun RhythmChart(pace: TimeTrialMarks.Pace) {
  val measurer = rememberTextMeasurer()
  val density = LocalDensity.current
  val highlight = TimeTrialMarks.paceHighlight(pace)
  val most = pace.halves.maxOf { maxOf(it.hits, it.errors) }.coerceAtLeast(1)
  NightWindow("contrarreloj_rhythm_window", Modifier.semantics { contentDescription = TimeTrialMarks.paceSpoken(pace) }) {
    BoxWithConstraints(Modifier.fillMaxWidth()) {
      val slot = maxWidth / TimeTrialMarks.HALF_MINUTES
      val pair = slot - 8.dp
      val hitCell = (pair - 4.dp) * 0.54f
      val errCell = pair - 4.dp - hitCell
      // si los cuatro rótulos del eje caben en una línea (con 1 dp de aire) se escriben así; si no, parten la línea por palabras y el «a» va pegado al número que lo precede
      val axisStyle = androidx.compose.material3.LocalTextStyle.current.merge(TextStyle(fontSize = 14.sp))
      val axis = TimeTrialMarks.PACE_AXIS.map { it.glueNumbers() }
      val axisOneLine = axis.all { measurer.measure(it, axisStyle, softWrap = false, maxLines = 1).size.width <= with(density) { (slot - 1.dp).roundToPx() } }
      Column {
        Row(Modifier.fillMaxWidth()) {
          pace.halves.forEachIndexed { i, half ->
            Row(Modifier.width(slot), horizontalArrangement = Arrangement.Center) {
              RhythmCell(hitCell, half.hits, most, hitCell - 6.dp, Clay.Grape, if (i in highlight) NubiTokens.SunOnNight else NubiTokens.OnNight, i in highlight, true, 8.dp, "hit_$i")
              Spacer(Modifier.width(4.dp))
              RhythmCell(errCell, half.errors, most, errCell - 12.dp, Clay.Coral, Clay.Coral, true, false, 4.dp, "err_$i")
            }
          }
        }
        Box(Modifier.fillMaxWidth().height(2.dp).background(NubiTokens.OnNightDim.copy(alpha = 0.4f)).hiddenTag("contrarreloj_rhythm_baseline"))
        Spacer(Modifier.height(4.dp))
        Row(Modifier.fillMaxWidth()) {
          axis.forEachIndexed { i, label ->
            Box(Modifier.width(slot), contentAlignment = Alignment.TopCenter) {
              CapScale(RHYTHM_TEXT_CAP) {
                Text(label, color = NubiTokens.OnNightDim, fontSize = 14.sp, lineHeight = 18.sp, textAlign = TextAlign.Center, softWrap = !axisOneLine, modifier = Modifier.widthIn(max = if (axisOneLine) slot else pair).hiddenTag("contrarreloj_rhythm_axis_$i"))
              }
            }
          }
        }
        // la unidad, escrita completa, una sola vez bajo los cuatro rótulos
        CapScale(RHYTHM_TEXT_CAP) {
          Text(TimeTrialMarks.PACE_AXIS_UNIT, color = NubiTokens.OnNightDim, fontSize = 14.sp, lineHeight = 18.sp, textAlign = TextAlign.Center, modifier = Modifier.fillMaxWidth().padding(top = 4.dp).hiddenTag("contrarreloj_rhythm_axis_unit"))
        }
      }
    }
  }
}

/** Una columna del gráfico del ritmo: la cifra en la fila de arriba (igual de alta en todas) y, bajo ella, la barra apoyada en la base común; con [value] en 0 no hay barra y, si no [figureAtZero], tampoco cifra. */
@Composable
private fun RhythmCell(width: Dp, value: Int, most: Int, barWidth: Dp, color: Color, figureColor: Color, bold: Boolean, figureAtZero: Boolean, corner: Dp, tag: String) {
  Column(Modifier.width(width), horizontalAlignment = Alignment.CenterHorizontally) {
    CapScale(RHYTHM_TEXT_CAP) {
      Box(Modifier.height(with(LocalDensity.current) { 20.sp.toDp() }), contentAlignment = Alignment.Center) {
        if (value > 0 || figureAtZero) {
          Text(value.toString(), color = figureColor, fontSize = 16.sp, fontWeight = if (bold) FontWeight.Bold else FontWeight.Medium, fontFamily = AppFamily, lineHeight = 20.sp, textAlign = TextAlign.Center, softWrap = false, modifier = Modifier.hiddenTag("contrarreloj_rhythm_fig_$tag"))
        }
      }
    }
    Spacer(Modifier.height(4.dp))
    Box(Modifier.height(BarMax), contentAlignment = Alignment.BottomCenter) {
      if (value > 0) {
        val shape = RoundedCornerShape(topStart = corner, topEnd = corner)
        Box(Modifier.width(barWidth).height((BarMax * (value.toFloat() / most)).coerceAtLeast(BarMin)).clip(shape).background(color).hiddenTag("contrarreloj_rhythm_bar_$tag"))
      }
    }
  }
}

/** «Esta semana y la pasada»: dos bloques («Esta semana», «La semana pasada») de filas «rótulo … cifra» con la cifra a la derecha y en cifras de igual ancho; si no caben en una línea con la letra de la persona, la cifra pasa debajo. */
@Composable
private fun WeeksCard(page: TimeTrialMarks.Page) {
  InfoCard("contrarreloj_weeks", TimeTrialMarks.WEEKS_TITLE) {
    WeekBlock("Esta semana", page.thisWeek, "this")
    ClayDivider("contrarreloj_divider_weeks", top = 12.dp)
    WeekBlock("La semana pasada", page.lastWeek, "last")
  }
}

@Composable
private fun WeekBlock(title: String, week: TimeTrialMarks.WeekSummary, key: String) {
  Column(Modifier.testTag("contrarreloj_week_$key")) {
    Text(title, color = Clay.Ink, fontSize = 16.sp, fontWeight = FontWeight.Bold, lineHeight = 24.sp, modifier = Modifier.semantics { heading() }.testTag("contrarreloj_week_${key}_title"))
    Spacer(Modifier.height(4.dp))
    TimeTrialMarks.weekRows(week).forEachIndexed { i, (label, value) -> FactRow(label, value, "contrarreloj_week_${key}_$i") }
  }
}

/**
 * Una fila «rótulo … cifra»: el rótulo a la izquierda y la cifra (en negrita y cifras de igual ancho) a la derecha; si no caben en una línea con la letra de la persona, la cifra pasa debajo. El lector de pantalla las dice juntas («Jugaste, 3 partidas»).
 * Cada texto tiene 20 sp de interlínea y la fila suma 4 dp: entre dos filas siempre quedan 4 dp (24 dp de paso con letra 1,0).
 */
@Composable
private fun FactRow(label: String, value: String, tag: String) {
  val measurer = rememberTextMeasurer()
  val base = androidx.compose.material3.LocalTextStyle.current
  val valueStyle = TextStyle(fontSize = 16.sp, fontWeight = FontWeight.Bold, fontFamily = AppFamily, fontFeatureSettings = "tnum", lineHeight = 20.sp)
  BoxWithConstraints(Modifier.fillMaxWidth().semantics(mergeDescendants = true) { contentDescription = TimeTrialMarks.rowSpoken(label, value) }.testTag(tag)) {
    val shown = value.glueNumbers()
    val need = measurer.measure(label, base.merge(TextStyle(fontSize = 16.sp)), softWrap = false, maxLines = 1).size.width + with(LocalDensity.current) { 12.dp.roundToPx() } +
      measurer.measure(shown, base.merge(valueStyle), softWrap = false, maxLines = 1).size.width
    if (need <= constraints.maxWidth) {
      Row(Modifier.fillMaxWidth().padding(bottom = 4.dp), verticalAlignment = Alignment.CenterVertically) {
        Text(label, color = Clay.Ink, fontSize = 16.sp, lineHeight = 20.sp, modifier = Modifier.weight(1f).testTag("${tag}_label"))
        Spacer(Modifier.width(12.dp))
        Text(shown, color = Clay.Ink, style = valueStyle, softWrap = false, textAlign = TextAlign.End, modifier = Modifier.testTag("${tag}_value"))
      }
    } else {
      Column(Modifier.fillMaxWidth().padding(bottom = 4.dp)) {
        Text(label, color = Clay.Ink, fontSize = 16.sp, lineHeight = 20.sp, modifier = Modifier.testTag("${tag}_label"))
        Spacer(Modifier.height(4.dp))
        Text(shown, color = Clay.Ink, style = valueStyle, textAlign = TextAlign.End, modifier = Modifier.fillMaxWidth().testTag("${tag}_value"))
      }
    }
  }
}

/** «Aciertos y errores»: en tu récord y en tus últimas partidas (por partida), una barra lila con su cifra de aciertos y una coral con la de errores, a la MISMA escala, y la regla de la marca. Cada barra lleva su palabra: el error nunca se ve solo por el color. */
@Composable
private fun SplitCard(page: TimeTrialMarks.Page) {
  val record = page.record ?: return
  val scale = maxOf(record.hits, page.recentHits ?: 0, 1).toFloat()
  InfoCard("contrarreloj_split", TimeTrialMarks.SPLIT_TITLE) {
    SplitGroup("En tu récord", record.hits, record.errors, scale, "record")
    ClayDivider("contrarreloj_divider_split_record")
    TimeTrialMarks.recentTitle(page.recentCount)?.let { title ->
      SplitGroup(title, page.recentHits ?: 0, page.recentErrors ?: 0, scale, "recent")
      ClayDivider("contrarreloj_divider_split_recent")
    }
    Text(TimeTrialStats.RULE, color = Clay.InkMuted, fontSize = 16.sp, lineHeight = 22.sp, modifier = Modifier.testTag("contrarreloj_rule"))
  }
}

@Composable
private fun SplitGroup(title: String, hits: Int, errors: Int, scale: Float, key: String) {
  Column(Modifier.testTag("contrarreloj_split_$key")) {
    Text(title, color = Clay.Ink, fontSize = 16.sp, fontWeight = FontWeight.Bold, lineHeight = 24.sp, modifier = Modifier.semantics { heading() }.testTag("contrarreloj_split_${key}_title"))
    Spacer(Modifier.height(8.dp))
    BarRow(hits / scale, Clay.Grape, TimeTrialDay.hitsText(hits), "contrarreloj_split_${key}_hits")
    Spacer(Modifier.height(4.dp))
    BarRow(errors / scale, Clay.Coral, TimeTrialMarks.errorsText(errors), "contrarreloj_split_${key}_errors")
  }
}

/** Una barra con su cifra a la derecha («46 aciertos»); sin sitio para la cifra a su lado (letra grande), la cifra baja debajo. La barra es de tinta y relleno, con el canto de tinta del kit; con 0 no se dibuja (queda la cifra, «0 errores»). */
@Composable
private fun BarRow(fraction: Float, color: Color, value: String, tag: String) {
  val measurer = rememberTextMeasurer()
  val base = androidx.compose.material3.LocalTextStyle.current
  val valueStyle = TextStyle(fontSize = 16.sp, fontWeight = FontWeight.Bold, fontFamily = AppFamily, fontFeatureSettings = "tnum", lineHeight = 24.sp)
  BoxWithConstraints(Modifier.fillMaxWidth().semantics(mergeDescendants = true) { contentDescription = value }.testTag(tag)) {
    val shown = value.glueNumbers()
    val valuePx = measurer.measure(shown, base.merge(valueStyle), softWrap = false, maxLines = 1).size.width
    val needPx = valuePx + with(LocalDensity.current) { (12.dp + 96.dp).roundToPx() }
    val inline = needPx <= constraints.maxWidth
    val boxWidth = maxWidth
    @Composable fun Bar(room: androidx.compose.ui.unit.Dp) {
      if (fraction > 0f) Box(Modifier.width((room * fraction.coerceIn(0f, 1f)).coerceAtLeast(14.dp)).height(14.dp).clip(RoundedCornerShape(7.dp)).background(color).border(2.dp, Clay.Ink, RoundedCornerShape(7.dp)).hiddenTag("${tag}_bar"))
    }
    if (inline) {
      val room = boxWidth - with(LocalDensity.current) { valuePx.toDp() } - 12.dp
      Row(Modifier.fillMaxWidth().heightIn(min = 24.dp), verticalAlignment = Alignment.CenterVertically) {
        Box(Modifier.width(room), contentAlignment = Alignment.CenterStart) { Bar(room) }
        Spacer(Modifier.width(12.dp))
        Text(shown, color = Clay.Ink, style = valueStyle, softWrap = false, textAlign = TextAlign.End, modifier = Modifier.weight(1f).testTag("${tag}_value"))
      }
    } else {
      Column(Modifier.fillMaxWidth()) {
        Bar(boxWidth)
        Text(shown, color = Clay.Ink, style = valueStyle, modifier = Modifier.padding(top = 2.dp).testTag("${tag}_value"))
      }
    }
  }
}

/** «Pistas anteriores» (solo si hay un récord de una pista anterior: «Tu récord de la pista 1: 38 aciertos. Las marcas de otra pista no se comparan.») y, en la misma losa, la nota de siempre: «Es el resultado de un juego, comparado solo contigo…». Sin pista anterior, la losa es solo la nota. */
@Composable
private fun NoteCard(page: TimeTrialMarks.Page) {
  val older = page.olderTrack
  InfoCard("contrarreloj_older", if (older != null) TimeTrialMarks.OLDER_TITLE else null) {
    if (older != null) {
      Text(TimeTrialMarks.olderSentence(older).glueNumbers(), color = Clay.Ink, fontSize = 16.sp, lineHeight = 22.sp, modifier = Modifier.testTag("contrarreloj_older_text"))
      Spacer(Modifier.height(16.dp))
    }
    Text(TimeTrialStats.NOTE, color = Clay.InkMuted, fontSize = 16.sp, lineHeight = 22.sp, modifier = Modifier.testTag("contrarreloj_note"))
  }
}

/** La letra de [content] no pasa de [max] veces el tamaño base (los números y títulos grandes: con 2,0 se saldrían del margen). */
@Composable
internal fun CapScale(max: Float, content: @Composable () -> Unit) {
  val d = LocalDensity.current
  if (d.fontScale <= max) { content(); return }
  CompositionLocalProvider(LocalDensity provides Density(d.density, max)) { content() }
}

/**
 * Un adorno o una cifra que el lector de pantalla NO lee por separado (el gráfico lo dice entero en su descripción, `TimeTrialMarks.barsSpoken`): borra lo que diría el nodo y le deja solo la marca de prueba, así las pruebas siguen midiendo cada pieza sin que TalkBack diga «31, 35, 39…» dos veces.
 */
private fun Modifier.hiddenTag(tag: String): Modifier = clearAndSetSemantics { this[SemanticsProperties.TestTag] = tag }
