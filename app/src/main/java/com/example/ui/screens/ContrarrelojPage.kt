package com.example.ui.screens

import androidx.compose.foundation.Canvas
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
import androidx.compose.foundation.layout.offset
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
import androidx.compose.ui.graphics.Path
import androidx.compose.ui.graphics.PathEffect
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
import com.example.data.Comparison
import com.example.data.PlusAccess
import com.example.data.TimeTrialDay
import com.example.data.TimeTrialMarks
import com.example.data.TimeTrialResult
import com.example.data.TimeTrialStats
import com.example.data.glueNumbers
import com.example.model.AgeRange
import com.example.model.AppLanguage
import com.example.model.GameRegistry
import com.example.ui.components.CapScale
import com.example.ui.components.ClaySlab
import com.example.ui.components.DivisionEmblem
import com.example.ui.components.DivisionEmblemHero
import com.example.ui.components.DivisionEmblemLadder
import com.example.ui.components.GameEmblem
import com.example.ui.components.Glyph
import com.example.ui.components.KitButton
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
/** El aire entre las barras de «Semana a semana» y el rótulo «la mitad» de su raya. */
private val HalfLabelGap = 8.dp

/** La letra de los números y títulos grandes de la página no pasa de 1,3 (con 2,0, un título de 52 sp se salía del margen: Fable §11.6). */
private const val BIG_TEXT_SCALE_CAP = 1.3f

/**
 * La PESTAÑA «Contrarreloj» (Tarea 215, 2-nov; Ricardo decidió las 9 decisiones de `auditoria-fable/idea-pestana-contrarreloj.md` §10 en su opción recomendada). Antes (Tarea 199, rediseñada en la 203 con la dirección D2 de Fable) era una página dentro de Avance, detrás de una puerta y entera para Nubi Plus; ahora es la pestaña de al lado de Hoy
 * y se parte en DOS mitades: **arriba lo gratis, a color; debajo lo de Plus** (sin Plus, DOS losas en gris con candado; con Plus, las secciones de siempre, a color y en el mismo orden). Lo que la persona no tendría ni con Plus no se muestra en gris (sin franja de edad o sin tabla del juego no hay losa de «tu lugar»: una promesa que no se cumple es contenido engañoso para Play).
 * Se desliza entera; ya no tiene flecha de volver (es una pestaña). Escala de letra fija: 24 (títulos y la cifra), 18 (Fredoka), 16 y 14 (solo los rótulos de los ejes); el título de la página, 26, es el de toda pestaña con título.
 *
 * De arriba abajo: la cabecera sobre el cielo («Contrarreloj» y, solo cuando hay comparación, «El Contrarreloj define tu lugar de la semana.»; sin ella, «Tus marcas, comparadas solo contigo.», con [OnBoardText]); el cielo se DESVANECE en 24 dp hasta el noche plano ([NubiTokens.NightFlat]) y de ahí para abajo todo va en losas sobre ese fondo plano;
 * el selector de los cuatro juegos ([GameSelector]: el de hoy marcado «Hoy»; sin Plus, los otros tres en gris con candado —tocarlos abre la hoja— y con su récord siempre a la vista); **LO GRATIS en UNA losa crema** ([FreeCard]: «Jugar · 2 minutos», «Tu récord: 44 aciertos.», «Esta semana jugaste 3 partidas; la mejor, 41 aciertos.», y con comparación la división con su emblema y su lugar entre las cinco,
 * «Te faltan 7 aciertos para En la Luna.» con su barra y «La semana cierra el domingo.»; antes era lo que daba la puerta de Avance) y la fila «Con qué te comparas ›» ([CompareLink], solo con la referencia estimada: es la honestidad de la referencia, no un dato de pago). Sin franja de edad, de 13 a 17 años o antes de la tercera partida (Tarea 207) lo gratis no dice la división y
 * el aviso de siempre sale bajo el selector ([Comparison.Gate]).
 *
 * **Con Plus** vienen debajo, bajo el rótulo «Esta semana, comparado con tu edad», «tu lugar en detalle» ([HeroCard]: la frase «Con 46 aciertos, superas al 5 % de la referencia estimada para tu edad.», [LadderCard]: la escalera de divisiones con sus semanas seguidas, [BellCard], [OthersCard] y «Semana a semana»), y bajo «Tus marcas», «todas tus marcas» (las últimas 10 en [MarksCard],
 * el ritmo en [RhythmCard] y las notas en [NotesCard]); el récord, la mejor de la semana y las partidas ya las dice lo gratis, así que las tres cifras de la Tarea 203 salieron. **Sin Plus** esas dos mitades son [LockedCard]: la imagen crema del kit sin saturación y un 12 % más oscura, el título legible en Rubik 18, el candado, «Con Nubi Plus: tu lugar en detalle.» / «Con Nubi Plus: todas tus marcas.»
 * en tinta negrita y una silueta decorativa que el lector no lee; toda la losa es un botón que abre la hoja del candado. **Nunca va en gris**: la pestaña, «Jugar», el récord, la división ni «Con qué te comparas ›». El gris tampoco aparece a mitad de una partida ni en el final.
 *
 * Todo a 16 sp o más (14 sp solo los rótulos de los ejes); cada número con su unidad y en frase completa; nada solo por color (el récord lleva moneda con visto y la palabra; tu fila de la escalera, la división en palabras; el gris, el candado y su frase). [results] = TODAS las partidas del Contrarreloj; [selected] = el juego elegido (sin Plus,
 * el de [today]); [today] = el juego del día; [plus] = Nubi Plus ([onLocked] abre la hoja del candado); [onPlay] juega el juego elegido; [now] = el momento de mirar.
 */
@Composable
internal fun ContrarrelojPage(
  results: List<TimeTrialResult>,
  selected: String,
  onSelect: (String) -> Unit,
  now: Long,
  modifier: Modifier = Modifier,
  lang: AppLanguage = com.example.ui.i18n.LocalAppLanguage.current,
  compare: Comparison.Source? = null,
  ageRange: AgeRange? = null,
  /** Abre «De dónde sale la referencia» (Tarea 209): la fila «Con qué te comparas ›». */
  onOpenSource: () -> Unit = {},
  /** Nubi Plus (Tarea 215): con él, el selector se toca entero y las dos mitades de abajo van a color; sin él, el gris con candado. */
  plus: Boolean = true,
  /** El juego del día (`TimeTrialDay.gameFor`): el selector lo marca «Hoy» y, sin Plus, es el único que se toca. null = ninguno marcado. */
  today: String? = null,
  /** «Jugar · 2 minutos»: juega el juego elegido (sin Plus, siempre el de hoy). */
  onPlay: () -> Unit = {},
  /** Tocar algo en gris abre la hoja del candado. */
  onLocked: () -> Unit = {}
) {
  val page = remember(results, selected, now) { TimeTrialMarks.page(results, selected, now) }
  val records = remember(results) { TimeTrialDay.GAMES.map { it to TimeTrialStats.record(results, it) } }
  val pace = remember(results, selected) { TimeTrialMarks.pace(results, selected) }
  // La comparación (Tareas 202 y 205): solo si hay de dónde sacarla (`compare`: la referencia estimada del APK, también en la tienda, que cubra este juego) y una franja con la que comparar (las siete se comparan, también la de 14 a 17 años con la tabla de 18 a 29: Tarea 216). Sin ella, la página queda como en la Tarea 201.
  // Tarea 207 (decisión de Ricardo, 1-nov): el lugar aparece desde la tercera partida GUARDADA de cada juego; antes, la página queda como «sin comparación» y dice por qué UNA vez. La precedencia vive en `Comparison.gate`:
  // sin franja de edad, el aviso de dónde elegirla; sin tabla para el juego, nada; con franja y tabla, [Comparison.Gate.EARLY] (menos de 3 partidas de ESTE juego, de cualquier semana y pista) o [Comparison.Gate.SHOW].
  val gate = remember(compare, ageRange, results, selected) { Comparison.gate(compare, ageRange, selected, results) }
  val section = remember(compare, gate, ageRange, results, selected, now, lang) {
    val range = compare?.let { Comparison.rangeFor(ageRange, it) }
    if (compare == null || range == null || gate != Comparison.Gate.SHOW) null
    else Comparison.section(compare, selected, GameRegistry.getById(selected)?.let { getGameTitle(it.id, lang, it.title) } ?: selected, range, results, now)
  }
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
      PageHeader(compared = section != null, Modifier.onSizeChanged { headerPx = it.height })
      Spacer(Modifier.height(FadeHeight))
      Column(Modifier.padding(horizontal = PageMargin).padding(bottom = 32.dp), verticalArrangement = Arrangement.spacedBy(CardGap)) {
        GameSelector(records, selected, today, plus, lang, onSelect, onLocked)
        // Tarea 205b: hay referencia para este juego pero la persona no tiene franja de edad guardada (instaló antes de la Tarea 195): la página no se queda callada, dice dónde elegirla
        if (gate == Comparison.Gate.NEEDS_AGE) Text(Comparison.AGE_HINT, color = NubiTokens.OnNight, fontSize = 16.sp, lineHeight = 22.sp, modifier = Modifier.fillMaxWidth().testTag("contrarreloj_age_hint"))
        // Tarea 207: con franja y tabla pero menos de tres partidas de este juego, UNA línea corrida y sin negrita donde iría el rótulo de la comparación: las primeras partidas sirven para conocer el juego y no se comparan
        if (gate == Comparison.Gate.EARLY) Text(Comparison.EARLY_NOTICE, color = NubiTokens.OnNight, fontSize = 16.sp, lineHeight = 22.sp, modifier = Modifier.fillMaxWidth().testTag("contrarreloj_early_notice"))

        // ---- LO GRATIS: nunca en gris
        FreeCard(page, section, onPlay)
        if (section != null) {
          // sin lugar que decir (sin marca esta semana, «Todavía somos pocos») va su losa; la fila «Con qué te comparas ›» queda DEBAJO de lo gratis, antes de lo de Plus (Tarea 213: debajo de la losa de la cifra)
          if (section.place !is Comparison.Place.Found) PlaceCard(section)
          CompareLink(section, onOpenSource)
        }

        // ---- LO DE PLUS: a color con Plus; sin Plus, dos losas en gris con candado
        if (plus) {
          // «tu lugar en detalle»: bajo su rótulo solo va lo que hay (el rótulo no se queda solo); el de «Tus marcas» solo cuando hay dos secciones
          val detail = section != null && (section.place is Comparison.Place.Found || section.others != null || section.weeks.isNotEmpty())
          if (section != null && detail) {
            CompareHeader()
            (section.place as? Comparison.Place.Found)?.let { HeroCard(it.ranked, section); LadderCard(it.ranked, section); BellCard(it.ranked, section.range) }
            section.others?.let { OthersCard(section, it) }
            if (section.weeks.isNotEmpty()) PercentileWeeksCard(section.weeks, section.estimated)
            SectionLabel(Comparison.SECTION_MARKS, "contrarreloj_section_marks")
          }
          // «todas tus marcas»: las últimas 10 y el ritmo; al final, las notas
          if (page.hasMarks) {
            MarksCard(page, now)
            RhythmCard(pace)
          } else EmptyCard()
          NotesCard(page, compared = section != null)
          // Tarea 213: el pie de la Tarea 211 (la frase larga y la fila al final) desapareció: la explicación vive en la hoja que abre «Con qué te comparas ›». Solo queda, en depuración, el aviso de los datos de muestra
          if (section != null) SampleFooter(section)
        } else {
          // sin franja de edad o sin tabla del juego, Plus no daría «tu lugar»: no se vende en gris (antes de la tercera partida sí: con Plus se vería desde la tercera)
          if (gate == Comparison.Gate.SHOW || gate == Comparison.Gate.EARLY) LockedCard("contrarreloj_locked_place", PlusAccess.LOCKED_PLACE_TITLE, PlusAccess.LOCKED_PLACE, onLocked) { PlaceSilhouette() }
          LockedCard("contrarreloj_locked_marks", PlusAccess.LOCKED_MARKS_TITLE, PlusAccess.LOCKED_MARKS, onLocked) { MarksSilhouette() }
          if (section != null) SampleFooter(section)
        }
      }
    }
  }
}

// ------------------------------------------------------------------ la cabecera, sobre el cielo

/** «Contrarreloj» (Rubik, con tope de letra 1,3) y su subtítulo, que parte la línea por palabras. Sin flecha de volver: es una pestaña (Tarea 215). Todo con [OnBoardText]: el cielo tiene luceros. [compared] = hay comparación: «El Contrarreloj define tu lugar de la semana.». */
@Composable
private fun PageHeader(compared: Boolean, modifier: Modifier = Modifier) {
  Column(modifier.fillMaxWidth().padding(start = 20.dp, end = PageMargin, top = 8.dp).testTag("contrarreloj_header")) {
    CapScale(BIG_TEXT_SCALE_CAP) {
      OnBoardText(TimeTrialMarks.TITLE, color = NubiTokens.OnNight, fontSize = 26.sp, fontWeight = FontWeight.Bold, fontFamily = TitleFamily, lineHeight = 32.sp, modifier = Modifier.fillMaxWidth().semantics { heading() }.testTag("contrarreloj_title"))
    }
    OnBoardText(TimeTrialMarks.subtitle(compared), color = NubiTokens.OnNightDim, fontSize = 16.sp, lineHeight = 22.sp, modifier = Modifier.fillMaxWidth().padding(top = 4.dp).testTag("contrarreloj_subtitle"))
  }
}

// ------------------------------------------------------------------ el selector de los cuatro juegos

/**
 * Los cuatro juegos en losas: el emblema, el nombre y su récord («44 aciertos» o «sin marca aún»); el ELEGIDO va en lima con el visto sobre su emblema (forma y color, nunca solo color) y el lector de pantalla dice «Tinta o Palabra, récord 44 aciertos, elegido». El de HOY lleva además la palabra «Hoy» (Tarea 215).
 * **Sin Nubi Plus ([plus] falso) los otros tres van en gris con candado** (la misma imagen crema sin saturación, el candado sobre el emblema y «con Nubi Plus» para el lector) y tocarlos abre la hoja ([onLocked]); su récord sigue a la vista, porque es tuyo. Dos filas de dos, parejas (cada fila tiene el alto de su losa más alta);
 * si la palabra más larga («Acoplamiento») no cabe entera en media fila a la letra de la persona, UNA columna: una palabra nunca se parte. Cada losa se toca en 72 dp o más (la losa pide 78: 6 dp son su sombra).
 */
@Composable
private fun GameSelector(records: List<Pair<String, Int?>>, selected: String, today: String?, plus: Boolean, lang: AppLanguage, onSelect: (String) -> Unit, onLocked: () -> Unit) {
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
            val locked = !plus && id != today
            GameTile(id, titles[i], record, selected = id == selected, isToday = id == today, locked = locked, onClick = { if (locked) onLocked() else onSelect(id) }, modifier = Modifier.weight(1f).fillMaxHeight())
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
private fun GameTile(gameId: String, title: String, record: Int?, selected: Boolean, isToday: Boolean, locked: Boolean, onClick: () -> Unit, modifier: Modifier) {
  ClaySlab(
    color = if (selected) SlabColor.LIME else SlabColor.CREAM, size = SlabSize.SMALL, onClick = onClick, role = if (locked) Role.Button else Role.RadioButton, contentAlignment = Alignment.CenterStart, locked = locked,
    modifier = modifier.heightIn(min = 78.dp).semantics(mergeDescendants = true) { this.selected = selected; contentDescription = TimeTrialMarks.tileSpoken(title, record, selected, isToday, locked) }.testTag("contrarreloj_game_$gameId")
  ) {
    Row(Modifier.fillMaxWidth().padding(horizontal = TilePad, vertical = TilePad), verticalAlignment = Alignment.CenterVertically) {
      GameEmblem(gameId, TileEmblem, Modifier.testTag("contrarreloj_game_emblem_$gameId"), done = selected, locked = locked)
      Spacer(Modifier.width(TileGap))
      Column(Modifier.weight(1f)) {
        Text(title, color = Clay.Ink, fontSize = 16.sp, fontWeight = FontWeight.Bold, fontFamily = AppFamily, lineHeight = 20.sp, modifier = Modifier.testTag("contrarreloj_game_name_$gameId"))
        Spacer(Modifier.height(4.dp))
        Text(TimeTrialMarks.tileValue(record).glueNumbers(), color = if (selected) Clay.Ink else Clay.InkMuted, fontSize = 16.sp, lineHeight = 20.sp, modifier = Modifier.testTag("contrarreloj_game_value_$gameId"))
        // «Hoy»: el juego del día, en una pastilla plana de tinta al 12 % (forma y palabra, nunca solo color)
        if (isToday) Text(
          TimeTrialMarks.TODAY_TAG, color = Clay.Ink, fontSize = 16.sp, fontWeight = FontWeight.Bold, fontFamily = AppFamily, lineHeight = 20.sp,
          modifier = Modifier.padding(top = 6.dp).clip(RoundedCornerShape(8.dp)).background(Clay.Ink.copy(alpha = 0.12f)).padding(horizontal = 8.dp, vertical = 1.dp).testTag("contrarreloj_game_today_$gameId")
        )
      }
    }
  }
}

// ------------------------------------------------------------------ lo gratis (Tarea 215)

/**
 * LO GRATIS, en UNA losa crema: «Jugar · 2 minutos» (sol; juega el juego elegido), «Tu récord: 44 aciertos.» (Fredoka 18), «Esta semana jugaste 3 partidas; la mejor, 41 aciertos.» y, con una comparación que decir ([Comparison.Place.Found]), una línea fina y la división con su emblema y su lugar entre las cinco ([DivisionLine]),
 * «Te faltan 7 aciertos para En la Luna.» con su barra y «La semana cierra el domingo.» (antes lo daba la puerta de Avance, y la barra y el domingo eran de la escalera). Sin comparación queda solo lo de tus marcas. El lector de pantalla lee cada grupo por separado: el botón, el récord y la semana, y la división con su lugar.
 */
@Composable
private fun FreeCard(page: TimeTrialMarks.Page, section: Comparison.Section?, onPlay: () -> Unit) {
  val found = (section?.place as? Comparison.Place.Found)?.ranked
  InfoCard("contrarreloj_free", null) {
    KitButton(TimeTrialMarks.PLAY, onClick = onPlay, color = SlabColor.SUN, modifier = Modifier.testTag("btn_contrarreloj_play"))
    Spacer(Modifier.height(12.dp))
    Text(TimeTrialMarks.freeRecordLine(page.recordMark).glueNumbers(), color = Clay.Ink, fontSize = 18.sp, fontWeight = FontWeight.Bold, fontFamily = AppFamily, lineHeight = 24.sp, modifier = Modifier.testTag("contrarreloj_free_record"))
    Text(TimeTrialMarks.weekLine(page.thisWeek).glueNumbers(), color = Clay.InkMuted, fontSize = 16.sp, lineHeight = 22.sp, modifier = Modifier.padding(top = 4.dp).testTag("contrarreloj_free_week"))
    if (found != null) {
      val ladder = remember(found) { Comparison.ladder(found) }
      ClayDivider("contrarreloj_divider_free")
      DivisionLine(found)
      Text(Comparison.needSentence(ladder, found).glueNumbers().glueNamesForLetter(), color = Clay.Ink, fontSize = 16.sp, fontWeight = FontWeight.Bold, lineHeight = 22.sp, modifier = Modifier.padding(top = 12.dp).testTag("contrarreloj_ladder_need"))
      ladder.progress?.let {
        Spacer(Modifier.height(8.dp))
        ProgressBar(it, "contrarreloj_ladder_bar")
      }
      Text(Comparison.CLOSES, color = Clay.InkMuted, fontSize = 16.sp, lineHeight = 22.sp, modifier = Modifier.padding(top = 8.dp).testTag("contrarreloj_ladder_closes"))
    }
  }
}

// ------------------------------------------------------------------ lo de Plus que no se tiene: la losa gris con candado (Tarea 215)

/**
 * Una mitad de lo de Plus VISTA pero no tenida (Fable §3.2): la losa crema del kit sin saturación y un 12 % más oscura ([com.example.ui.components.LockedSlabFilter], sin render nuevo), el [title] legible en tinta (Rubik 18), el candado del kit a 20 dp con [phrase] —«Con Nubi Plus: tu lugar en detalle.»— en tinta negrita de 16 sp
 * y una silueta decorativa ([silhouette]: sin números ni letras; el lector de pantalla no la lee). Toda la losa es un botón que abre la hoja del candado ([onClick]); el lector dice «Con Nubi Plus: tu lugar en detalle. Botón.». Nada se dice solo con el color: lleva el candado y la frase.
 */
@Composable
private fun LockedCard(tag: String, title: String, phrase: String, onClick: () -> Unit, silhouette: @Composable () -> Unit) {
  ClaySlab(
    SlabColor.CREAM, Modifier.fillMaxWidth().semantics(mergeDescendants = true) { contentDescription = phrase }.testTag(tag), size = SlabSize.LARGE, onClick = onClick, locked = true
  ) {
    Column(Modifier.fillMaxWidth().padding(16.dp)) {
      Text(title, color = Clay.Ink, fontSize = 18.sp, fontWeight = FontWeight.Bold, fontFamily = TitleFamily, lineHeight = 24.sp, modifier = Modifier.testTag("${tag}_title"))
      Row(Modifier.padding(top = 8.dp), verticalAlignment = Alignment.CenterVertically) {
        KitGlyph(Glyph.LOCK, size = 20.dp, modifier = Modifier.hiddenTag("${tag}_lock"))
        Spacer(Modifier.width(8.dp))
        Text(phrase, color = Clay.Ink, fontSize = 16.sp, fontWeight = FontWeight.Bold, lineHeight = 22.sp, modifier = Modifier.weight(1f).testTag("${tag}_phrase"))
      }
      Spacer(Modifier.height(12.dp))
      Box(Modifier.fillMaxWidth().height(SilhouetteHeight).hiddenTag("${tag}_silhouette")) { silhouette() }
    }
  }
}

/** El alto de la silueta decorativa de una losa gris, y su color: un gris más claro que la losa (#D0D0D0). */
private val SilhouetteHeight = 56.dp
private val SilhouetteColor = Color.White.copy(alpha = 0.55f)

/** «Tu lugar en detalle»: una escalera de barras a la izquierda y una campana a la derecha. Solo formas, sin números ni letras. */
@Composable
private fun PlaceSilhouette() {
  Canvas(Modifier.fillMaxSize()) {
    val h = size.height; val w = size.width
    val rungs = 5; val rungH = h / (rungs * 2 - 1).toFloat(); val half = w * 0.42f
    for (i in 0 until rungs) {                                       // la escalera: cinco peldaños que crecen
      val bw = half * (0.35f + 0.65f * (i + 1) / rungs)
      drawRoundRect(SilhouetteColor, topLeft = Offset(0f, i * 2 * rungH), size = Size(bw, rungH), cornerRadius = androidx.compose.ui.geometry.CornerRadius(rungH / 2))
    }
    val path = Path().apply {                                        // la campana
      val x0 = w * 0.54f; val bell = w - x0
      moveTo(x0, h)
      for (i in 0..40) { val t = i / 40f; val x = x0 + bell * t; val y = h - h * 0.95f * Math.exp(-Math.pow((t - 0.5) / 0.18, 2.0)).toFloat(); lineTo(x, y) }
      lineTo(w, h); close()
    }
    drawPath(path, SilhouetteColor)
  }
}

/** «Todas tus marcas»: siete barras de alturas distintas sobre una base. Solo formas, sin números ni letras. */
@Composable
private fun MarksSilhouette() {
  Canvas(Modifier.fillMaxSize()) {
    val h = size.height; val w = size.width
    val heights = listOf(0.45f, 0.7f, 0.55f, 0.9f, 0.62f, 0.8f, 1f)
    val gap = 8.dp.toPx(); val bw = (w - gap * (heights.size - 1)) / heights.size
    heights.forEachIndexed { i, f -> drawRoundRect(SilhouetteColor, topLeft = Offset(i * (bw + gap), h - h * f), size = Size(bw, h * f), cornerRadius = androidx.compose.ui.geometry.CornerRadius(6.dp.toPx())) }
  }
}

// ------------------------------------------------------------------ las losas de información

/** Una losa crema de la página: título en Rubik 24 (Tarea 203: antes 20) con 16 dp de aire antes del contenido, igual en todas; 16 dp de relleno. Sin título, la losa abre directo con su contenido. */
@Composable
private fun InfoCard(tag: String, title: String?, modifier: Modifier = Modifier, content: @Composable ColumnScope.() -> Unit) {
  ClayCard(modifier.fillMaxWidth().testTag(tag), size = SlabSize.LARGE, contentPadding = 16.dp) {
    if (title != null) {
      Text(title.glueNumbers(), color = Clay.Ink, fontSize = 24.sp, fontWeight = FontWeight.Bold, fontFamily = TitleFamily, lineHeight = 28.sp, modifier = Modifier.semantics { heading() }.testTag("${tag}_title"))
      Spacer(Modifier.height(16.dp))
    }
    content()
  }
}

// ------------------------------------------------------------------ la cifra protagonista, la escalera y la campana (Tarea 203; `auditoria-fable/contrarreloj-rediseno.md` §3, dirección D2)

/** Una línea finita de 1 dp (tinta al 14 %) entre las filas de una lista dentro de una losa: sin aire propio (cada fila lleva el suyo). */
@Composable
private fun ThinLine() {
  Box(Modifier.fillMaxWidth().height(1.dp).background(Clay.Ink.copy(alpha = 0.14f)))
}

/**
 * «Tu lugar en detalle», primera losa (D2; Tarea 213; Tarea 215): UNA sola frase, «Con 46 aciertos, superas al 5 % de la referencia estimada para tu edad.», en Rubik Bold de 24 sp con tope de escala 1,3 como las cifras grandes (un tamaño de la escala de la página, un solo estilo: sin negritas sueltas dentro; «5 %» y «46 aciertos» no se
 * parten: [glueNumbers]) —decisión de Ricardo del 2-nov, «breve y claro; si explicas mucho se complica», en lugar de «63 de cada 100» con su frase de abajo, que muchos leían al revés—. Si la comparación usó otra clase (las 4 semanas o las franjas juntas) la frase lo dice y, con las franjas juntas, una línea más.
 * **Desde la Tarea 215 la división, con su emblema de plasticina (Tarea 212) y su lugar entre las cinco, ya no va aquí sino arriba, en lo gratis ([FreeCard])**: esta losa es de Plus y la división no. El lector de pantalla lee la frase (y, con las franjas juntas, su línea).
 */
@Composable
private fun HeroCard(r: Comparison.Ranked, s: Comparison.Section) {
  InfoCard("contrarreloj_hero", null, Modifier.semantics(mergeDescendants = true) { contentDescription = listOfNotNull(Comparison.heroSentence(r, s.range), Comparison.scopeLine(r)).joinToString(" ") }) {
    Column(Modifier.fillMaxWidth()) {
      CapScale(BIG_TEXT_SCALE_CAP) {
        Text(Comparison.heroSentence(r, s.range).glueNumbers(), color = Clay.Ink, fontSize = 24.sp, fontWeight = FontWeight.Bold, fontFamily = TitleFamily, lineHeight = 30.sp, modifier = Modifier.fillMaxWidth().testTag("contrarreloj_hero_sentence"))
      }
      Comparison.scopeLine(r)?.let { Text(it, color = Clay.InkMuted, fontSize = 16.sp, lineHeight = 22.sp, modifier = Modifier.padding(top = 8.dp).testTag("contrarreloj_hero_scope")) }
    }
  }
}

/**
 * La división («En órbita», Fredoka negrita 18) y, debajo, su lugar entre las cinco en 16 sp `InkMuted` («La tercera de cinco divisiones.»; Tarea 209: reemplaza a «Percentil 63», que Ricardo quitó de todo texto el 1-nov), con el emblema de la división a la izquierda (Tarea 212: 52 dp, 12 dp de aire, arriba, a la altura de la primera línea).
 * El emblema no crece con la letra y los textos van en una columna a su lado, así que con letra grande los textos parten por palabras («En la / plataforma») y nunca se encima nada; el bloque mide 52 dp con letra 1,0 (antes 50: la losa crece 2 dp).
 */
@Composable
private fun DivisionLine(r: Comparison.Ranked) {
  Row(Modifier.fillMaxWidth().semantics(mergeDescendants = true) { contentDescription = "${r.division.label}. ${Comparison.divisionPlaceLine(r.division)}" }.testTag("contrarreloj_hero_division_line"), verticalAlignment = Alignment.Top) {
    DivisionEmblem(r.division, DivisionEmblemHero, Modifier.hiddenTag("contrarreloj_hero_emblem_${r.division.name}"))
    Spacer(Modifier.width(12.dp))
    Column(Modifier.weight(1f)) {
      Text(r.division.label, color = Clay.Ink, fontSize = 18.sp, fontWeight = FontWeight.Bold, fontFamily = AppFamily, lineHeight = 24.sp, modifier = Modifier.testTag("contrarreloj_hero_division"))
      Text(Comparison.divisionPlaceLine(r.division), color = Clay.InkMuted, fontSize = 16.sp, lineHeight = 22.sp, modifier = Modifier.padding(top = 4.dp).testTag("contrarreloj_hero_place"))
    }
  }
}

/**
 * La escalera de divisiones (D2): las cinco de arriba abajo, cada una con el primer acierto con que se llega a ella («desde 39 aciertos»), la fila de la persona en lima («En órbita · Tú · 43 aciertos»: la división dicha en palabras, no solo por el color) y, al pie, lo que sigue: «Te faltan 7 aciertos para En la Luna.» con su barra,
 * «La semana cierra el domingo.», la racha de semanas («Llevas 3 semanas seguidas En órbita o más arriba.»; sin racha no dice nada) y la clase de la escalera dicha una sola vez («Con las marcas de las 128 personas de 45 a 54 años que jugaron Tinta o Palabra esta semana.»). En la división más alta: «Estás en la división más alta de tu franja esta semana.», sin barra.
 * Los umbrales salen de la distribución de la franja esa semana ([Comparison.thresholds]) y cambian con la gente. No es una liga: no hay grupo, ni nombres, ni ascenso entre personas.
 * Desde la Tarea 212 (D3) cada fila lleva el emblema de su división (28 dp, a todo color también las apagadas: la escalera es la leyenda de las cinco, y el estado nunca depende del color) antes del nombre; las filas apagadas siguen midiendo 40 dp (relleno de 6 arriba y 2 abajo en vez de 10 y 6) y la lima 52 dp.
 */
@Composable
private fun LadderCard(r: Comparison.Ranked, s: Comparison.Section) {
  val ladder = remember(r) { Comparison.ladder(r) }
  InfoCard("contrarreloj_ladder", Comparison.LADDER_TITLE) {
    ladder.rungs.forEachIndexed { i, rung ->
      if (i > 0) ThinLine()
      if (rung.division == r.division) LadderYouRow(r)
      else Column(Modifier.padding(top = 6.dp, bottom = 2.dp)) {          // con el emblema de 28 dp la fila sigue en 40 dp: 6 + 28 + 4 + 2
        FactRow(rung.division.label, Comparison.rungText(rung, ladder.rungs.getOrNull(i - 1)), "contrarreloj_ladder_row_$i", muted = true, leadingWidth = DivisionEmblemLadder,
          leading = { DivisionEmblem(rung.division, DivisionEmblemLadder, Modifier.hiddenTag("contrarreloj_ladder_emblem_$i")) })
      }
    }
    ClayDivider("contrarreloj_divider_ladder")
    // (Tarea 215: «Te faltan N aciertos», su barra y «La semana cierra el domingo.» subieron a lo gratis, [FreeCard]: eran lo que daba la puerta de Avance)
    Comparison.streakSentence(Comparison.streak(s.weeks))?.let { Text(it, color = Clay.Ink, fontSize = 16.sp, lineHeight = 22.sp, modifier = Modifier.padding(bottom = 8.dp).testTag("contrarreloj_ladder_streak")) }
    Text(Comparison.ladderNote(r, s.range).glueNumbers().glueNamesForLetter(), color = Clay.InkMuted, fontSize = 16.sp, lineHeight = 22.sp, modifier = Modifier.testTag("contrarreloj_ladder_note"))
  }
}

/**
 * La fila de la persona en la escalera: un recuadro PLANO lima (esquina de 12 dp, sin borde ni sombra, no tocable) con el emblema de la división (28 dp; Tarea 212), la división a su derecha y «Tú · 43 aciertos» a la derecha (Fredoka negrita 18); con letra grande, uno bajo el otro.
 * El relleno vertical interior es de 8 dp (antes 10) para que la fila siga en 52 dp con el emblema; con el emblema, «En la plataforma» + «Tú · 46 aciertos» ya no caben en una línea a 393 dp y la fila se apila (el emblema y el nombre arriba, «Tú · 46 aciertos» abajo a la derecha), como con letra grande.
 */
@Composable
private fun LadderYouRow(r: Comparison.Ranked) {
  val measurer = rememberTextMeasurer()
  val density = LocalDensity.current
  val base = androidx.compose.material3.LocalTextStyle.current
  val spoken = r.division.label
  val name = spoken.glueNamesForLetter()
  val you = Comparison.youRow(r).glueNumbers()
  val style = base.merge(TextStyle(fontSize = 18.sp, fontWeight = FontWeight.Bold, fontFamily = AppFamily))
  BoxWithConstraints(
    Modifier.fillMaxWidth().padding(vertical = 4.dp).semantics(mergeDescendants = true) { contentDescription = "$spoken, ${Comparison.youRow(r)}" }.testTag("contrarreloj_ladder_you")
      .clip(RoundedCornerShape(12.dp)).background(Clay.Lime).padding(horizontal = 12.dp, vertical = 8.dp)
  ) {
    val emblem = @Composable { DivisionEmblem(r.division, DivisionEmblemLadder, Modifier.hiddenTag("contrarreloj_ladder_you_emblem")) }
    val need = with(density) { (DivisionEmblemLadder + 10.dp).roundToPx() } + measurer.measure(name, style, softWrap = false, maxLines = 1).size.width + with(density) { 12.dp.roundToPx() } + measurer.measure(you, style, softWrap = false, maxLines = 1).size.width
    if (need <= constraints.maxWidth) {
      Row(Modifier.fillMaxWidth(), verticalAlignment = Alignment.CenterVertically) {
        emblem()
        Spacer(Modifier.width(10.dp))
        Text(name, color = Clay.Ink, fontSize = 18.sp, fontWeight = FontWeight.Bold, fontFamily = AppFamily, lineHeight = 24.sp, modifier = Modifier.weight(1f).testTag("contrarreloj_ladder_you_name"))
        Spacer(Modifier.width(12.dp))
        Text(you, color = Clay.Ink, fontSize = 18.sp, fontWeight = FontWeight.Bold, fontFamily = AppFamily, lineHeight = 24.sp, softWrap = false, modifier = Modifier.testTag("contrarreloj_ladder_you_value"))
      }
    } else {
      Column(Modifier.fillMaxWidth()) {
        Row(Modifier.fillMaxWidth(), verticalAlignment = Alignment.CenterVertically) {
          emblem()
          Spacer(Modifier.width(10.dp))
          Text(name, color = Clay.Ink, fontSize = 18.sp, fontWeight = FontWeight.Bold, fontFamily = AppFamily, lineHeight = 24.sp, modifier = Modifier.weight(1f).testTag("contrarreloj_ladder_you_name"))
        }
        Text(you, color = Clay.Ink, fontSize = 18.sp, fontWeight = FontWeight.Bold, fontFamily = AppFamily, lineHeight = 24.sp, textAlign = TextAlign.End, modifier = Modifier.fillMaxWidth().padding(top = 4.dp).testTag("contrarreloj_ladder_you_value"))
      }
    }
  }
}

/** La barra de lo que falta para la siguiente división: un carril gris de 16 dp y, encima, el relleno lima con el borde de tinta del kit (nunca vacío: mide al menos el 8 %). No se lee por separado: la frase de arriba dice cuántos aciertos faltan. */
@Composable
private fun ProgressBar(fraction: Float, tag: String) {
  Box(Modifier.fillMaxWidth().height(16.dp).testTag(tag).clip(RoundedCornerShape(8.dp)).background(Clay.Ink.copy(alpha = 0.12f))) {
    Box(Modifier.fillMaxWidth(fraction.coerceIn(0.08f, 1f)).fillMaxHeight().clip(RoundedCornerShape(8.dp)).background(Clay.Lime).border(2.dp, Clay.Ink, RoundedCornerShape(8.dp)).hiddenTag("${tag}_fill"))
  }
}

/** «Cómo se reparten las marcas»: la campana real en su propia losa (Ricardo no aceptó mandarla a una hoja), con UNA frase corta que no repite ningún número de la escalera. */
@Composable
private fun BellCard(r: Comparison.Ranked, range: AgeRange) {
  InfoCard("contrarreloj_bell", Comparison.BELL_TITLE) {
    // Tarea 205b: si la barra «Tú» tapa la raya de la mitad (la marca cae en la mitad o a uno o dos aciertos, según el ancho), la raya no se dibuja y la frase lo dice: nunca habla de una raya que no se ve
    BoxWithConstraints(Modifier.fillMaxWidth()) {
      val onHalf = Comparison.barCoversHalf(r.mark, r.distribution, (maxWidth - NightPad * 2).value.toDouble())
      Column {
        BellChart(r, range, onHalf)
        Text(Comparison.bellCaption(r.estimated, onHalf), color = Clay.InkMuted, fontSize = 16.sp, lineHeight = 22.sp, modifier = Modifier.padding(top = 12.dp).testTag("contrarreloj_bell_caption"))
      }
    }
  }
}

// ------------------------------------------------------------------ «Tus marcas» (Tarea 203)

/** «Tus últimas 10 marcas» (y, bajo la ventanilla, el récord con su fecha y su paso): su propia losa. */
@Composable
private fun MarksCard(page: TimeTrialMarks.Page, now: Long) {
  InfoCard("contrarreloj_marks", null) { MarksChart(page, now) }
}

/** Sin marcas en el juego: una frase que dice qué falta (y nada más: ni cifras ni gráficos). */
@Composable
private fun EmptyCard() {
  InfoCard("contrarreloj_empty", null) {
    Text("Todavía no tienes marcas en este juego. Cuando juegues su Contrarreloj, aquí aparece tu récord.", color = Clay.Ink, fontSize = 16.sp, lineHeight = 22.sp, modifier = Modifier.testTag("contrarreloj_record_empty"))
  }
}

/**
 * Las notas, juntas y al final, en una losa sin título: cómo se cuenta la marca («La marca cuenta los aciertos menos los errores, sin bajar de cero.»), el récord de la pista anterior si lo hay y la nota de siempre, en sus dos versiones: «…, comparado solo contigo. No es una medida de capacidad.» sin comparación y «Es el resultado de un juego. No es una medida de capacidad.» con ella.
 */
@Composable
private fun NotesCard(page: TimeTrialMarks.Page, compared: Boolean) {
  InfoCard("contrarreloj_notes", null) {
    if (page.hasMarks) Text(TimeTrialStats.RULE, color = Clay.InkMuted, fontSize = 16.sp, lineHeight = 22.sp, modifier = Modifier.padding(bottom = 12.dp).testTag("contrarreloj_rule"))
    page.olderTrack?.let { Text(TimeTrialMarks.olderSentence(it).glueNumbers(), color = Clay.InkMuted, fontSize = 16.sp, lineHeight = 22.sp, modifier = Modifier.padding(bottom = 12.dp).testTag("contrarreloj_older_text")) }
    Text(TimeTrialStats.note(compared), color = Clay.InkMuted, fontSize = 16.sp, lineHeight = 22.sp, modifier = Modifier.testTag("contrarreloj_note"))
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
      Text(TimeTrialMarks.barsTitle(1), color = Clay.Ink, fontSize = 24.sp, fontWeight = FontWeight.Bold, fontFamily = TitleFamily, lineHeight = 28.sp, modifier = Modifier.semantics { heading() }.testTag("contrarreloj_chart_title"))
      Spacer(Modifier.height(16.dp))
      Text(TimeTrialMarks.FIRST_MARK, color = Clay.Ink, fontSize = 16.sp, lineHeight = 22.sp, modifier = Modifier.testTag("contrarreloj_chart_first"))
      page.record?.let { Text(TimeTrialMarks.recordLine(it, now, page.maxLevel).glueNumbers(), color = Clay.InkMuted, fontSize = 16.sp, lineHeight = 22.sp, modifier = Modifier.padding(top = 8.dp).testTag("contrarreloj_record_line")) }
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
        Text(TimeTrialMarks.barsTitle(shown.size), color = Clay.Ink, fontSize = 24.sp, fontWeight = FontWeight.Bold, fontFamily = TitleFamily, lineHeight = 28.sp, modifier = Modifier.semantics { heading() }.testTag("contrarreloj_chart_title"))
        Spacer(Modifier.height(16.dp))
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
        page.record?.let { Text(TimeTrialMarks.recordLine(it, now, page.maxLevel).glueNumbers(), color = Clay.InkMuted, fontSize = 16.sp, lineHeight = 22.sp, modifier = Modifier.padding(top = 12.dp).testTag("contrarreloj_record_line")) }
        TimeTrialMarks.averagesSentence(page)?.let {
          Text(it.glueNumbers(), color = Clay.InkMuted, fontSize = 16.sp, lineHeight = 22.sp, modifier = Modifier.padding(top = 8.dp).testTag("contrarreloj_averages"))
        }
      }
    }
  }
}

/** La moneda lima con el visto del récord (forma y color: nunca solo color): 20 dp con el borde de tinta del kit. */
@Composable
private fun Coin(tag: String = "contrarreloj_coin", size: Dp = 20.dp) {
  Box(Modifier.size(size).clip(CircleShape).background(Clay.Lime).border(2.dp, Clay.Ink, CircleShape).hiddenTag(tag), contentAlignment = Alignment.Center) { KitGlyph(Glyph.CHECK, size = size * 0.6f) }
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
    Text(pace.sentence.glueNumbers(), color = Clay.Ink, fontSize = 16.sp, lineHeight = 22.sp, modifier = Modifier.testTag("contrarreloj_rhythm_sentence"))
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

/**
 * Una fila «rótulo … cifra»: el rótulo a la izquierda y la cifra (en negrita y cifras de igual ancho) a la derecha; si no caben en una línea con la letra de la persona, la cifra pasa debajo. El lector de pantalla las dice juntas («Jugaste, 3 partidas»).
 * [leading] (Tarea 212: el emblema de la división de la escalera, de [leadingWidth] de ancho) va antes del rótulo con 10 dp de aire y centrado con él; cuando la fila no cabe en una línea, el emblema y el rótulo quedan juntos en la primera y la cifra debajo, a la derecha. No cambia lo que dice el lector.
 * Cada texto tiene 20 sp de interlínea y la fila suma 4 dp: entre dos filas siempre quedan 4 dp (24 dp de paso con letra 1,0).
 */
@Composable
private fun FactRow(label: String, value: String, tag: String, bold: Boolean = false, muted: Boolean = false, leading: (@Composable () -> Unit)? = null, leadingWidth: Dp = 0.dp) {
  val measurer = rememberTextMeasurer()
  val base = androidx.compose.material3.LocalTextStyle.current
  val valueStyle = TextStyle(fontSize = 16.sp, fontWeight = if (muted) FontWeight.Normal else FontWeight.Bold, fontFamily = AppFamily, fontFeatureSettings = "tnum", lineHeight = 20.sp)
  val valueColor = if (muted) Clay.InkMuted else Clay.Ink
  BoxWithConstraints(Modifier.fillMaxWidth().semantics(mergeDescendants = true) { contentDescription = TimeTrialMarks.rowSpoken(label, value) }.testTag(tag)) {
    val shown = value.glueNumbers()
    val label = label.glueNamesForLetter()
    val lead = if (leading != null) with(LocalDensity.current) { (leadingWidth + 10.dp).roundToPx() } else 0
    val need = lead + measurer.measure(label, base.merge(TextStyle(fontSize = 16.sp, fontWeight = if (bold) FontWeight.Bold else null)), softWrap = false, maxLines = 1).size.width + with(LocalDensity.current) { 12.dp.roundToPx() } +
      measurer.measure(shown, base.merge(valueStyle), softWrap = false, maxLines = 1).size.width
    if (need <= constraints.maxWidth) {
      Row(Modifier.fillMaxWidth().padding(bottom = 4.dp), verticalAlignment = Alignment.CenterVertically) {
        if (leading != null) { leading(); Spacer(Modifier.width(10.dp)) }
        Text(label, color = Clay.Ink, fontSize = 16.sp, fontWeight = if (bold) FontWeight.Bold else null, lineHeight = 20.sp, modifier = Modifier.weight(1f).testTag("${tag}_label"))
        Spacer(Modifier.width(12.dp))
        Text(shown, color = valueColor, style = valueStyle, softWrap = false, textAlign = TextAlign.End, modifier = Modifier.testTag("${tag}_value"))
      }
    } else {
      Column(Modifier.fillMaxWidth().padding(bottom = 4.dp)) {
        if (leading != null) {
          Row(Modifier.fillMaxWidth(), verticalAlignment = Alignment.CenterVertically) {
            leading()
            Spacer(Modifier.width(10.dp))
            Text(label, color = Clay.Ink, fontSize = 16.sp, fontWeight = if (bold) FontWeight.Bold else null, lineHeight = 20.sp, modifier = Modifier.weight(1f).testTag("${tag}_label"))
          }
        } else Text(label, color = Clay.Ink, fontSize = 16.sp, fontWeight = if (bold) FontWeight.Bold else null, lineHeight = 20.sp, modifier = Modifier.testTag("${tag}_label"))
        Spacer(Modifier.height(4.dp))
        Text(shown, color = valueColor, style = valueStyle, textAlign = TextAlign.End, modifier = Modifier.fillMaxWidth().testTag("${tag}_value"))
      }
    }
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
        Text(shown, color = Clay.Ink, style = valueStyle, modifier = Modifier.padding(top = 4.dp).testTag("${tag}_value"))
      }
    }
  }
}

/**
 * El rótulo de una sección (Fable §11.2; Tarea 203: Fredoka negrita 18, de la escala de cinco tamaños) en crema (`OnNight`, sobre el noche plano: sin luceros no necesita orilla) y, desde el final del texto hasta el margen derecho, una línea de 1 dp (crema al 35 %). 8 dp de aire propios arriba (con los 16 de entre losas, 24) y los 16 de siempre abajo.
 * **Si el rótulo no cabe en UNA línea con al menos 24 dp de raya (letra grande), la raya NO va** (Tarea 203b): con letra 2,0 «Esta semana, comparado con tu edad» parte en tres renglones y la raya quedaba como un guion suelto junto a la última palabra; sin ella el rótulo queda limpio y parte por palabras.
 */
@Composable
private fun SectionLabel(text: String, tag: String) {
  val measurer = rememberTextMeasurer()
  val density = LocalDensity.current
  val style = androidx.compose.material3.LocalTextStyle.current.merge(TextStyle(fontSize = 18.sp, fontWeight = FontWeight.Bold, fontFamily = AppFamily))
  BoxWithConstraints(Modifier.fillMaxWidth().padding(top = 8.dp)) {
    val oneLine = measurer.measure(text, style, softWrap = false, maxLines = 1).size.width
    val withLine = oneLine <= constraints.maxWidth - with(density) { (12.dp + 24.dp).roundToPx() }
    if (withLine) {
      Row(Modifier.fillMaxWidth(), verticalAlignment = Alignment.Bottom) {
        Text(text, color = NubiTokens.OnNight, fontSize = 18.sp, fontWeight = FontWeight.Bold, fontFamily = AppFamily, lineHeight = 24.sp, modifier = Modifier.semantics { heading() }.testTag(tag))
        Spacer(Modifier.width(12.dp))
        Box(Modifier.weight(1f).padding(bottom = 11.dp).height(1.dp).background(NubiTokens.OnNight.copy(alpha = 0.35f)).hiddenTag("${tag}_line"))
      }
    } else {
      Text(text, color = NubiTokens.OnNight, fontSize = 18.sp, fontWeight = FontWeight.Bold, fontFamily = AppFamily, lineHeight = 24.sp, modifier = Modifier.fillMaxWidth().semantics { heading() }.testTag(tag))
    }
  }
}

/**
 * El rótulo de la sección de comparación, «Esta semana, comparado con tu edad», y nada más (la frase de con qué se compara salió de la página en la Tarea 213; la fila «Con qué te comparas ›» va debajo de la cifra, [CompareLink]; la página sigue diciendo desde arriba que es una referencia estimada: el subtítulo de la cabecera y las losas dicen «referencia estimada»).
 */
@Composable
private fun CompareHeader() {
  SectionLabel(Comparison.SECTION_COMPARE, "contrarreloj_section_compare")
}

/**
 * La fila «Con qué te comparas ›» (Tarea 213, decisión de Ricardo del 2-nov): DEBAJO de la losa de la cifra y antes de la escalera, abre «De dónde sale la referencia» ([onOpenSource] → `TopPanel.REFERENCIA`). Reemplaza a la frase larga y a la fila «De dónde sale la referencia ›» que desde la Tarea 211 iban al final:
 * 29 y 25 de 35 personajes de MiroFish la querían arriba, «donde está el número», y Ricardo quería ver primero sus datos; una fila de 48 dp cumple las dos cosas. Mismo molde de [SourceLinkRow] (rol de botón, tocable entera, `SunOnNight` de 16 sp sobre el noche plano; el lector dice «Con qué te comparas, botón»)
 * y la misma marca de prueba, `contrarreloj_source_link`. Solo con la referencia estimada: con los datos de muestra de depuración no hay estudios que mostrar, y con personas reales (futuro) no hace falta.
 */
@Composable
private fun CompareLink(s: Comparison.Section, onOpenSource: () -> Unit) {
  if (s.estimated) SourceLinkRow(onOpenSource, NubiTokens.SunOnNight, "contrarreloj_source_link", label = Comparison.COMPARE_LINK)
}

/** Lo último de la página, SOLO en depuración con datos de muestra (Tarea 213: antes, con la referencia estimada, también iban aquí la frase larga y su fila): «Estos son datos de muestra: solo se ven en la versión de prueba.» */
@Composable
private fun SampleFooter(s: Comparison.Section) {
  if (s.estimated || !s.sample) return
  Text(Comparison.SAMPLE_NOTICE.glueNumbers().glueNamesForLetter(), color = NubiTokens.OnNight, fontSize = 16.sp, lineHeight = 22.sp, modifier = Modifier.fillMaxWidth().testTag("contrarreloj_sample_notice"))
}

/**
 * El recuadro plano de la frase principal: el MISMO lila claro de «Marca nueva» en Hoy (`TimeTrialDoneFill`, `#D9CEFF`, Tarea 169), esquina y relleno de 12 dp, sin borde ni sombra, no tocable; la letra, seminegrita de 16 sp en tinta (11,9:1 sobre el lila). La etiqueta de prueba cubre el recuadro entero.
 */
@Composable
private fun FlatNote(text: String, tag: String) {
  Text(text.glueNumbers().glueNamesForLetter(), color = Clay.Ink, fontSize = 16.sp, fontWeight = FontWeight.SemiBold, lineHeight = 22.sp, modifier = Modifier.fillMaxWidth().testTag(tag).clip(RoundedCornerShape(12.dp)).background(TimeTrialDoneFill).padding(12.dp))
}

/**
 * «Tu lugar esta semana» cuando NO hay un lugar que decir (Tarea 203; con lugar, la página usa la cifra, la escalera y la campana): «Todavía somos pocos» con su cuenta, su barra y «Mientras tanto…» (sin umbrales ni «te faltan»: con tan pocas personas no se inventan), o, sin marca esta semana, qué hacer para verlo.
 * Todo lo que se dice sale de [Comparison] (probado ahí); aquí solo se dibuja.
 */
@Composable
private fun PlaceCard(s: Comparison.Section) {
  InfoCard("contrarreloj_place", Comparison.PLACE_TITLE) {
    when (val p = s.place) {
      is Comparison.Place.NoMark -> Text(Comparison.noMarkSentence(s.gameTitle, s.estimated), color = Clay.Ink, fontSize = 16.sp, lineHeight = 22.sp, modifier = Modifier.testTag("contrarreloj_place_nomark"))
      is Comparison.Place.Few -> FewBlock(p, s)
      is Comparison.Place.Found -> Unit
    }
  }
}

/** «Todavía somos pocos»: la frase enmarcada con la cuenta real, la barra «23 de 50 personas», una línea y, si con todas las edades de 4 semanas alcanzan 50, «Mientras tanto, …». */
@Composable
private fun FewBlock(few: Comparison.Place.Few, s: Comparison.Section) {
  FlatNote(Comparison.fewSentence(few, s.range, s.gameTitle), "contrarreloj_few_note")
  Spacer(Modifier.height(12.dp))
  BarRow(few.own.toFloat() / Comparison.FEW_BAR_GOAL, Clay.Lime, Comparison.fewBarText(few.own), "contrarreloj_few_bar")
  few.meanwhile?.let {
    ClayDivider("contrarreloj_divider_few")
    Text(Comparison.meanwhileSentence(it).glueNumbers().glueNamesForLetter(), color = Clay.InkMuted, fontSize = 16.sp, lineHeight = 22.sp, modifier = Modifier.testTag("contrarreloj_few_meanwhile"))
  }
}

/** Lo que la campana mide: el tope de su curva y lo que se reserva arriba para «Tú» y su moneda (palabra de hasta 26 dp, moneda de 18 y 4 dp de aire entre cada pieza). */
private val BellCurveMax = 84.dp
private val BellTopReserve = 60.dp
private const val BELL_SAMPLES = 160

/**
 * La campana REAL (Fable §1.2 y §11.4 B): el histograma suavizado 1-2-1 (solo para dibujar; el percentil sale de los tramos sin suavizar) relleno de lila `Clay.Grape` sobre la ventanilla noche, sin curva ajustada ni colas que no existen; la línea fina de la mitad (la mediana), «Tú» como barra sol de 6 dp
 * hasta la curva con la moneda del visto y la palabra encima, y el eje «0 aciertos · 25 · 50 · 75 o más». La barra, la línea y la curva salen de la MISMA función ([Comparison.bellHeight]), así que la barra llega justo a la curva. El lector de pantalla dice «Campana con las marcas de las 128 personas…» (el dibujo no habla; las dos frases de abajo son el contenido).
 */
@Composable
private fun BellChart(r: Comparison.Ranked, range: AgeRange, onHalf: Boolean) {
  val d = r.distribution
  val smooth = remember(d) { Comparison.smoothed(d.bins) }
  val peak = remember(smooth) { smooth.max().coerceAtLeast(1.0) }
  NightWindow("contrarreloj_bell_window", Modifier.semantics { contentDescription = Comparison.bellSpoken(r, range, onHalf) }) {
    BoxWithConstraints(Modifier.fillMaxWidth()) {
      val width = maxWidth
      val canvasHeight = BellTopReserve + BellCurveMax
      fun xDp(x: Double): Dp = width * (x / d.topMark).toFloat()
      fun heightAt(x: Double): Dp = BellCurveMax * (Comparison.bellHeight(smooth, x, d.binWidth) / peak).toFloat()
      val youX = Comparison.markX(r.mark, d.topMark)
      val barH = heightAt(youX).coerceAtLeast(6.dp)
      val medianH = heightAt(Comparison.markX(d.p50, d.topMark))
      Column {
        Box(Modifier.fillMaxWidth().height(canvasHeight)) {
          Canvas(Modifier.fillMaxSize().hiddenTag("contrarreloj_bell_area")) {
            val base = size.height
            val path = Path().apply {
              moveTo(0f, base)
              for (i in 0..BELL_SAMPLES) {
                val x = d.topMark * i / BELL_SAMPLES.toDouble()
                lineTo(size.width * i / BELL_SAMPLES, base - (Comparison.bellHeight(smooth, x, d.binWidth) / peak).toFloat() * BellCurveMax.toPx())
              }
              lineTo(size.width, base)
              close()
            }
            drawPath(path, Clay.Grape)
            drawRect(NubiTokens.OnNightDim.copy(alpha = 0.4f), topLeft = Offset(0f, base - 2.dp.toPx()), size = Size(size.width, 2.dp.toPx()))
          }
          // la mitad: una línea fina, del color de la ventanilla, que corta el lila hasta la curva
          // (si la barra «Tú» la taparía, no se dibuja: nada se superpone y la frase de abajo dice que la marca está en la mitad)
          if (!onHalf) Box(Modifier.offset(x = xDp(Comparison.markX(d.p50, d.topMark)) - 0.75.dp, y = canvasHeight - medianH).width(Comparison.BELL_LINE_DP.dp).height(medianH).background(NubiTokens.NightWindow).hiddenTag("contrarreloj_bell_median"))
          // «Tú»: la barra sol hasta la curva, y encima la moneda del visto y la palabra (sobre la barra, sin salirse de la ventanilla)
          Box(
            Modifier.offset(x = xDp(youX) - 3.dp, y = canvasHeight - barH).width(6.dp).height(barH).clip(RoundedCornerShape(topStart = 3.dp, topEnd = 3.dp)).background(Clay.Sun).hiddenTag("contrarreloj_bell_you_bar")
          )
          val labelX = xDp(youX).coerceIn(20.dp, width - 20.dp)
          Box(Modifier.offset(x = labelX - 20.dp, y = 0.dp).width(40.dp).height(canvasHeight - barH - 4.dp), contentAlignment = Alignment.BottomCenter) {
            Column(horizontalAlignment = Alignment.CenterHorizontally) {
              CapScale(RHYTHM_TEXT_CAP) {
                Text("Tú", color = NubiTokens.OnNight, fontSize = 16.sp, fontWeight = FontWeight.Bold, fontFamily = AppFamily, lineHeight = 20.sp, textAlign = TextAlign.Center, softWrap = false, modifier = Modifier.hiddenTag("contrarreloj_bell_you"))
              }
              Spacer(Modifier.height(4.dp))
              Coin("contrarreloj_bell_coin", size = 18.dp)
            }
          }
        }
        Spacer(Modifier.height(4.dp))
        BellAxis(width, d, r.mark, onHalf)
      }
    }
  }
}

/**
 * El eje de la campana (14 sp, rótulo de gráfico): «0 aciertos» al borde izquierdo, «75 o más» al derecho y «25» y «50» sobre su lugar; un rótulo que no deja 8 dp al anterior se omite (primero se guardan los dos extremos, luego el «50» y por último el «25»). Con tope de escala 1,3 como el resto de los gráficos.
 */
@Composable
private fun BellAxis(width: Dp, d: Comparison.Distribution, mark: Int, onHalf: Boolean) {
  CapScale(RHYTHM_TEXT_CAP) {
    val measurer = rememberTextMeasurer()
    val density = LocalDensity.current
    val style = androidx.compose.material3.LocalTextStyle.current.merge(TextStyle(fontSize = 14.sp))
    val widthPx = with(density) { width.toPx() }
    val ticks = Comparison.axisTicks(d)
    val labels = listOf("0 aciertos", ticks[1].toString(), ticks[2].toString(), "${ticks[3]} o más")
    val w = labels.map { measurer.measure(it, style, softWrap = false, maxLines = 1).size.width.toFloat() }
    val start = listOf(0f, widthPx * ticks[1] / d.topMark - w[1] / 2, widthPx * ticks[2] / d.topMark - w[2] / 2, widthPx - w[3])
    // Tarea 207: los extremos siempre; un intermedio no se dibuja si no cabe (8 dp de hueco) ni si la raya de la mitad o la barra «Tú» caen dentro de su caja más 2 dp a cada lado (la lógica pura, con el ancho real del dibujo)
    val shown = Comparison.axisLabelsFor(d, mark, onHalf, width.value.toDouble(), w.map { with(density) { it.toDp().value.toDouble() } })
    Box(Modifier.fillMaxWidth().height(with(density) { 18.sp.toDp() })) {
      for (i in shown) {
        Text(labels[i], color = NubiTokens.OnNightDim, fontSize = 14.sp, lineHeight = 18.sp, softWrap = false, modifier = Modifier.offset(x = with(density) { start[i].toDp() }).hiddenTag("contrarreloj_bell_axis_$i"))
      }
    }
  }
}

/** «Semana a semana»: las semanas con marca propia y con 50 personas o más en la franja, en barras de 0 a 100 en la ventanilla noche, y la frase («En 3 de las 4 semanas estuviste por encima de la mitad de tu franja.»). */
@Composable
private fun PercentileWeeksCard(points: List<Comparison.WeekPoint>, estimated: Boolean) {
  InfoCard("contrarreloj_percweeks", Comparison.WEEKS_TITLE) {
    PercentileWeeksChart(points, estimated)
    Comparison.weeksSentence(points)?.let { Text(it, color = Clay.Ink, fontSize = 16.sp, lineHeight = 22.sp, modifier = Modifier.padding(top = 12.dp).testTag("contrarreloj_percweeks_sentence")) }
    if (estimated) Text(Comparison.WEEKS_ESTIMATED_NOTE, color = Clay.InkMuted, fontSize = 16.sp, lineHeight = 22.sp, modifier = Modifier.padding(top = 8.dp).testTag("contrarreloj_percweeks_note"))
  }
}

/**
 * Los lugares de las semanas (desde la más vieja con dato, hasta esta; Tarea 203: antes eran siempre 7 con los huecos en blanco): cada semana con punto lleva su cifra encima (todas en UNA fila; la de esta semana en `SunOnNight` y negrita) y una barra de 0 a 100 (la de esta semana en sol, el resto lila); una semana sin punto queda sin barra ni cifra. Cifras con tope de escala 1,3; el eje dice «hace 7 semanas»
 * a la izquierda y «esta semana» a la derecha, y con letra grande el de la derecha baja de fila. **Una raya punteada en 50** (Tarea 203b), detrás de las barras, con su rótulo «la mitad» (14 sp, como los ejes, con tope de escala 1,3) en un carril propio a la derecha del gráfico: ninguna barra ni cifra se le cruza;
 * con muchas semanas y letra grande el rótulo pasa a dos líneas («la / mitad») y el carril se angosta, para que las cifras de dos dígitos conserven 8 dp entre columnas. El lector de pantalla dice cada semana ([Comparison.weeksSpoken]).
 */
@Composable
private fun PercentileWeeksChart(points: List<Comparison.WeekPoint>, estimated: Boolean) {
  val byAgo = points.associateBy { it.weeksAgo }
  val oldest = points.first().weeksAgo
  val measurer = rememberTextMeasurer()
  val density = LocalDensity.current
  val capped = Density(density.density, minOf(density.fontScale, RHYTHM_TEXT_CAP))
  val base = androidx.compose.material3.LocalTextStyle.current
  // la raya de 50 («la mitad», 14 sp como los ejes, con el mismo tope de escala) tiene su rótulo en un carril a la derecha del gráfico: ninguna barra ni cifra se le cruza.
  // Si con el rótulo en UNA línea las cifras de dos dígitos no dejan 8 dp entre columnas (muchas semanas con letra grande), el rótulo pasa a dos líneas, «la / mitad», y el carril se angosta.
  val labelStyle = base.merge(TextStyle(fontSize = 14.sp))
  fun labelWidthOf(text: String) = with(density) { (measurer.measure(text, labelStyle, softWrap = false, maxLines = 1, density = capped).size.width + 1).toDp() }
  val figureWidth = with(density) { measurer.measure("99", base.merge(TextStyle(fontSize = 16.sp, fontWeight = FontWeight.Bold, fontFamily = AppFamily)), softWrap = false, maxLines = 1, density = capped).size.width.toDp() }
  NightWindow("contrarreloj_percweeks_window", Modifier.semantics { contentDescription = Comparison.weeksSpoken(points, estimated) }) {
    BoxWithConstraints(Modifier.fillMaxWidth()) {
      val oneLineWidth = labelWidthOf(Comparison.WEEKS_HALF_LABEL)
      val stacked = (maxWidth - oneLineWidth - HalfLabelGap) / (oldest + 1) < figureWidth + 8.dp
      val halfLabelWidth = if (stacked) Comparison.WEEKS_HALF_LABEL.split(" ").maxOf { labelWidthOf(it) } else oneLineWidth
      val chartWidth = maxWidth - halfLabelWidth - HalfLabelGap
      val slot = chartWidth / (oldest + 1)
      val barWidth = minOf(44.dp, slot - 12.dp)
      val figureHeight = with(capped) { 20.sp.toDp() }
      val halfY = figureHeight + 4.dp + BarMax / 2                  // la fila de cifras, 4 dp de aire y la mitad del alto de las barras
      Column {
        Box(Modifier.fillMaxWidth()) {
          // la raya va DETRÁS de las barras (se ve en los huecos y por encima de las bajas): punteada y fina, para no parecerse a la base
          Box(
            Modifier.offset(y = halfY - 0.5.dp).width(chartWidth).height(1.dp).drawBehind {
              drawLine(NubiTokens.OnNightDim.copy(alpha = 0.6f), Offset(0f, size.height / 2), Offset(size.width, size.height / 2), strokeWidth = 1.dp.toPx(), pathEffect = PathEffect.dashPathEffect(floatArrayOf(6.dp.toPx(), 4.dp.toPx())))
            }.hiddenTag("contrarreloj_percweeks_half_line")
          )
          Row(Modifier.width(chartWidth)) {
            for (ago in oldest downTo 0) {
              val point = byAgo[ago]
              Column(Modifier.width(slot), horizontalAlignment = Alignment.CenterHorizontally) {
                CapScale(RHYTHM_TEXT_CAP) {
                  Box(Modifier.height(with(LocalDensity.current) { 20.sp.toDp() }), contentAlignment = Alignment.Center) {
                    if (point != null) Text(point.percentile.toString(), color = if (ago == 0) NubiTokens.SunOnNight else NubiTokens.OnNight, fontSize = 16.sp, fontWeight = if (ago == 0) FontWeight.Bold else FontWeight.Medium, fontFamily = AppFamily, lineHeight = 20.sp, textAlign = TextAlign.Center, softWrap = false, modifier = Modifier.hiddenTag("contrarreloj_percweeks_fig_$ago"))
                  }
                }
                Spacer(Modifier.height(4.dp))
                Box(Modifier.height(BarMax), contentAlignment = Alignment.BottomCenter) {
                  if (point != null) Box(Modifier.width(barWidth).height((BarMax * (point.percentile / 100f)).coerceAtLeast(BarMin)).clip(RoundedCornerShape(topStart = 8.dp, topEnd = 8.dp)).background(if (ago == 0) Clay.Sun else Clay.Grape).hiddenTag("contrarreloj_percweeks_bar_$ago"))
                }
              }
            }
          }
          CapScale(RHYTHM_TEXT_CAP) {
            Text(
              Comparison.WEEKS_HALF_LABEL, color = NubiTokens.OnNightDim, fontSize = 14.sp, lineHeight = 18.sp, softWrap = stacked,
              modifier = Modifier.offset(x = chartWidth + HalfLabelGap, y = halfY - with(capped) { (if (stacked) 18.sp else 9.sp).toDp() }).width(halfLabelWidth).hiddenTag("contrarreloj_percweeks_half")
            )
          }
        }
        Box(Modifier.width(chartWidth).height(2.dp).background(NubiTokens.OnNightDim.copy(alpha = 0.4f)).hiddenTag("contrarreloj_percweeks_baseline"))
        Spacer(Modifier.height(4.dp))
        WeeksAxis(chartWidth, Comparison.weeksAxisLeft(points))
      }
    }
  }
}

/** Los dos rótulos del eje de las semanas (14 sp con tope de escala 1,3): a la izquierda «hace 3 semanas» (las de la más vieja con dato; sin él, solo el de la derecha) y a la derecha «esta semana»; si no caben en una fila con 8 dp entre los dos, el de la derecha baja a su propia fila, a la derecha. */
@Composable
private fun WeeksAxis(width: Dp, left: String?) {
  CapScale(RHYTHM_TEXT_CAP) {
    val measurer = rememberTextMeasurer()
    val density = LocalDensity.current
    val style = androidx.compose.material3.LocalTextStyle.current.merge(TextStyle(fontSize = 14.sp))
    val fits = left == null || with(density) {
      measurer.measure(left, style, softWrap = false, maxLines = 1).size.width + Comparison.AXIS_GAP_DP.dp.roundToPx() +
        measurer.measure(Comparison.WEEKS_AXIS_RIGHT, style, softWrap = false, maxLines = 1).size.width <= width.roundToPx()
    }
    Column(Modifier.width(width)) {
      Row(Modifier.fillMaxWidth()) {
        if (left != null) Text(left, color = NubiTokens.OnNightDim, fontSize = 14.sp, lineHeight = 18.sp, softWrap = false, modifier = Modifier.hiddenTag("contrarreloj_percweeks_axis_left"))
        if (fits) {
          Spacer(Modifier.weight(1f))
          Text(Comparison.WEEKS_AXIS_RIGHT, color = NubiTokens.OnNightDim, fontSize = 14.sp, lineHeight = 18.sp, softWrap = false, modifier = Modifier.hiddenTag("contrarreloj_percweeks_axis_right"))
        }
      }
      if (!fits) Row(Modifier.fillMaxWidth().padding(top = 4.dp)) {
        Spacer(Modifier.weight(1f))
        Text(Comparison.WEEKS_AXIS_RIGHT, color = NubiTokens.OnNightDim, fontSize = 14.sp, lineHeight = 18.sp, softWrap = false, modifier = Modifier.hiddenTag("contrarreloj_percweeks_axis_right"))
      }
    }
  }
}

/**
 * «Otras edades» (Tarea 203b): la frase de entrada y una fila por franja con «Supera al N %» a la derecha (la propia, en negrita y con «(tu franja)»), SOLO las que llegan a 50 personas; una línea y «Todas las edades» (si llega a 50) y, si alguna falta, UNA línea que cierra:
 * «Las demás edades aparecen cuando lleguen a 50 personas.». Sin ninguna fila la losa no va (la página no la pone). Sin fila de «14 a 17 años» (comparte la tabla de 18 a 29: sería la de 18 a 29 repetida) y sin filas de relleno que dijeran «todavía somos pocos». Sin botón ni hoja aparte: una sola puerta, nada que tocar.
 */
@Composable
private fun OthersCard(s: Comparison.Section, o: Comparison.Others) {
  InfoCard("contrarreloj_others", Comparison.OTHERS_TITLE) {
    Text(Comparison.othersLead(s.gameTitle, s.estimated).glueNumbers(), color = Clay.Ink, fontSize = 16.sp, lineHeight = 22.sp, modifier = Modifier.testTag("contrarreloj_others_lead"))
    Spacer(Modifier.height(12.dp))
    o.rows.forEachIndexed { i, row -> FactRow(row.label, row.value, "contrarreloj_others_$i", bold = row.own) }
    o.all?.let { all ->
      if (o.rows.isNotEmpty()) ClayDivider("contrarreloj_divider_others", top = 12.dp)
      FactRow(all.label, all.value, "contrarreloj_others_all")
    }
    if (o.hidden) Text(Comparison.OTHERS_HIDDEN, color = Clay.InkMuted, fontSize = 16.sp, lineHeight = 22.sp, modifier = Modifier.padding(top = 8.dp).testTag("contrarreloj_others_hidden"))
  }
}

/**
 * Un adorno o una cifra que el lector de pantalla NO lee por separado (el gráfico lo dice entero en su descripción, `TimeTrialMarks.barsSpoken`): borra lo que diría el nodo y le deja solo la marca de prueba, así las pruebas siguen midiendo cada pieza sin que TalkBack diga «31, 35, 39…» dos veces.
 */
private fun Modifier.hiddenTag(tag: String): Modifier = clearAndSetSemantics { this[SemanticsProperties.TestTag] = tag }
