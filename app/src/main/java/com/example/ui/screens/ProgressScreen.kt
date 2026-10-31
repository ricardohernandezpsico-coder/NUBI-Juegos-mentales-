package com.example.ui.screens

import androidx.activity.compose.BackHandler
import androidx.compose.foundation.layout.Arrangement
import androidx.compose.foundation.layout.Column
import androidx.compose.foundation.layout.PaddingValues
import androidx.compose.foundation.layout.Row
import androidx.compose.foundation.layout.Spacer
import androidx.compose.foundation.layout.fillMaxSize
import androidx.compose.foundation.layout.fillMaxWidth
import androidx.compose.foundation.layout.height
import androidx.compose.foundation.layout.padding
import androidx.compose.foundation.lazy.LazyColumn
import androidx.compose.foundation.lazy.rememberLazyListState
import androidx.compose.runtime.Composable
import androidx.compose.runtime.collectAsState
import androidx.compose.runtime.getValue
import androidx.compose.runtime.mutableStateOf
import androidx.compose.runtime.remember
import androidx.compose.runtime.setValue
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.graphics.Color
import androidx.compose.ui.platform.testTag
import androidx.compose.ui.text.font.FontWeight
import androidx.compose.ui.unit.dp
import androidx.compose.ui.unit.sp
import com.example.data.AreaPoints
import com.example.data.StarMeasures
import com.example.data.TimeTrialDay
import com.example.data.TimeTrialStats
import com.example.data.Week
import com.example.data.leagueRow
import com.example.model.DomainType
import com.example.model.GameRegistry
import com.example.model.RankTier
import com.example.ui.components.AREA_ORDER
import com.example.ui.components.OnBoardText
import com.example.ui.components.SpaceSectionTitle
import com.example.ui.i18n.LocalAppLanguage
import com.example.ui.theme.TitleFamily
import com.example.ui.theme.Clay
import com.example.ui.theme.ClayButton
import com.example.viewmodel.NeuroVidaViewModel
import kotlinx.coroutines.launch

private val OnNight = Color(0xFFEAF0FF)

/**
 * Avance con PUNTOS por área (Tarea 159; antes la pestaña con tu semana, tus áreas, tus medidas, tus medallas y tus últimas partidas; Ricardo, 22-oct: «con puntos por área», «sin palabras sueltas»; `auditoria-fable/avance-b-puntos.md`). De arriba abajo: el título, UNA línea de la semana,
 * **tus áreas** (desde la Tarea 174, cuatro cuadros en 2 × 2 con el nombre, los puntos de 0 a 1000 y una barra del color del área; tocar uno abre su PÁGINA, [AvanceAreaPage], donde viven la etapa, lo que falta y la semana; el título y los títulos de sección van en Rubik: [TitleFamily]), **tus mejores marcas** y **tu punto de partida**. Salieron «Tus medidas» (pasaron a la página del área), «Descubrimientos» con su «Jugar» (Avance mira, Hoy juega),
 * el enlace a las medallas (viven en «Tú») y «Tus últimas partidas». La página del área reemplaza a la lista mientras está abierta y Atrás vuelve a la lista en el mismo lugar (el estado de la lista vive aquí). La barra de pestañas sigue visible y «Avance» sigue elegida.
 * **La puerta del Contrarreloj** (Tarea 199): entre «Tus áreas» y «Tus mejores marcas», UNA losa ([ContrarrelojDoor]) que con Nubi Plus abre la página «Contrarreloj» ([ContrarrelojPage]; se aloja igual que la de un área: reemplaza a la lista, Atrás vuelve) y sin Plus abre la hoja del candado ([PlusLockSheet]).
 */
@Composable
fun ProgressScreen(
  viewModel: NeuroVidaViewModel,
  modifier: Modifier = Modifier
) {
  val gameRanks by viewModel.gameRanks.collectAsState()
  val history by viewModel.gameHistory.collectAsState()
  // Las partidas de juegos retirados (data/Models.kt, GameRegistry.retiredDomains) siguen guardadas pero no se muestran.
  val shownHistory = remember(history) { history.filter { !GameRegistry.isRetired(it.gameId) } }
  val progressLog by viewModel.progressLog.collectAsState()
  val measures by viewModel.starMeasures.collectAsState()
  val baseline by viewModel.baseline.collectAsState()
  val settings by viewModel.userSettings.collectAsState()
  val streak by viewModel.currentStreak.collectAsState()
  val currentLang = LocalAppLanguage.current
  val contrarreloj by viewModel.contrarreloj.collectAsState()      // la página «Contrarreloj» abierta (el juego elegido; Tarea 199): vive en el ViewModel como la de un área
  val plus by viewModel.plus.collectAsState()
  val compareWorld by viewModel.compareWorld.collectAsState()
  val ageRange by viewModel.ageRange.collectAsState()
  val ttResults by viewModel.timeTrialResults.collectAsState()
  var plusSheet by remember { mutableStateOf(false) }
  val avanceArea by viewModel.avanceArea.collectAsState()        // la página abierta: la recuerda el ViewModel (rotar la pantalla no la cierra; cambiar de pestaña, sí)
  val listState = rememberLazyListState()                       // la posición de la lista queda aquí: al volver de la página de un área, Avance está donde estaba
  val context = androidx.compose.ui.platform.LocalContext.current
  val shareScope = androidx.compose.runtime.rememberCoroutineScope()

  val avg = if (gameRanks.isEmpty()) 0 else gameRanks.sumOf { it.rating } / gameRanks.size
  val tier = RankTier.fromRating(avg)
  val now = remember(history, progressLog) { System.currentTimeMillis() }
  val names = remember { DomainType.values().associate { it.name to it.displayName } }
  val areaPoints = remember(progressLog, now) { AreaPoints.forAreas(AREA_ORDER, progressLog, now) }
  val lamps = remember(history, now) { Week.lampsFor(history.map { it.timestamp }, now) }
  val bestMarks = remember(measures) { StarMeasures.bestMarks(measures) }
  val lastPlayed = remember(history) { history.groupBy { it.gameId }.mapValues { (_, l) -> l.maxOf { it.timestamp } } }

  if (avanceArea != null) BackHandler(onBack = viewModel::closeAvanceArea)        // Atrás cierra la página y vuelve a la lista
  val contrarrelojOpen = contrarreloj != null && plus                              // sin Plus no hay página: si quedó abierta (el interruptor cambió), se ve la lista
  if (contrarrelojOpen) BackHandler(onBack = viewModel::closeContrarreloj)
  val ttNow = remember(ttResults) { System.currentTimeMillis() }

  Column(modifier.fillMaxSize().padding(bottom = com.example.ui.components.LocalBarInset.current)) {      // la franja de la barra de pestañas queda libre
    val ttOpen = contrarreloj
    if (ttOpen != null && contrarrelojOpen) {
      // La comparación con personas de tu edad (Tarea 202): `Comparison.sourceFor` es null en la build de la tienda y mientras no haya servidor; en depuración, datos de muestra (la página lo dice con una frase visible).
      val compare = remember(compareWorld) { com.example.data.Comparison.sourceFor(com.example.BuildConfig.DEBUG, compareWorld) }
      ContrarrelojPage(results = ttResults, selected = ttOpen, onSelect = viewModel::selectContrarrelojGame, onBack = viewModel::closeContrarreloj, now = ttNow, lang = currentLang, compare = compare, ageRange = ageRange)
      return@Column
    }
    val openKey = avanceArea
    val open = openKey?.let { k -> areaPoints.firstOrNull { it.area == k } }
    if (open != null) {
      val weekly = remember(open.area, progressLog, now) { AreaPoints.weekly(AreaPoints.gamesOf(open.area), progressLog, now) }
      AvanceAreaPage(
        points = open, weekly = weekly, areaName = names[open.area] ?: open.area,
        lastPlayed = lastPlayed, measures = measures, lang = currentLang, now = now, onBack = viewModel::closeAvanceArea, timedFor = viewModel::retoFor
      )
      return@Column
    }
    Row(Modifier.fillMaxWidth().padding(start = 20.dp, end = 16.dp, top = 8.dp, bottom = 4.dp), verticalAlignment = Alignment.CenterVertically) { OnBoardText("Avance", color = OnNight, fontSize = 26.sp, fontWeight = FontWeight.Bold, fontFamily = TitleFamily, modifier = Modifier.testTag("avance_title")) }
    LazyColumn(
      state = listState,
      modifier = Modifier
        .weight(1f)
        .padding(horizontal = 20.dp),                                       // 20 como el título; los cuadros de las áreas necesitan el ancho (Tarea 174)
      contentPadding = PaddingValues(top = 8.dp, bottom = 28.dp),
      verticalArrangement = Arrangement.spacedBy(22.dp)
    ) {
      item { AvanceWeekLine(lamps, settings.weeklyGoal, empty = shownHistory.isEmpty()) }

      if (shownHistory.isEmpty()) {
        // Sin ninguna partida (Tarea 147, P8): en vez de cuatro filas vacías, Nubi con UNA línea
        item { com.example.ui.components.NubiEmptyLine(com.example.ui.components.EMPTY_AVANCE_LINE) }
        // el Contrarreloj no entra en `game_results`: quien solo jugó eso sigue teniendo su puerta
        if (ttResults.isNotEmpty()) item { DoorRow(ttResults, plus, viewModel) { plusSheet = true } }
      } else {
        item { AvanceAreaGrid(areaPoints, names) { viewModel.openAvanceArea(it) } }
        item { DoorRow(ttResults, plus, viewModel) { plusSheet = true } }
        item { AvanceBestMarks(bestMarks, now) }
      }

      // «Tu liga» y su Compartir: fuera de la v1 (Tarea 149); se ven solo con `Features.LEAGUES`
      if (com.example.Features.LEAGUES) item {
        AvanceLeague(leagueRow(avg)) {
          // Compartir la liga general como imagen (misma tarjeta que al ascender).
          shareScope.launch {
            try {
              com.example.ui.components.ShareCard.share(
                context,
                com.example.ui.components.ShareCard.Content(
                  headline = "Estoy en liga ${tier.tierName}",
                  subtitle = "Mi liga general",
                  tier = tier,
                  stats = com.example.ui.components.ShareCard.Content.trophiesAndStreak(avg, streak)
                ),
                "¡Estoy en la liga ${tier.tierName} de Nubi!"
              )
            } catch (e: Exception) {
              android.util.Log.e("ProgressScreen", "No se pudo compartir la liga", e)
              com.example.diag.ErrorLog.record("COMPARTIR", "No se pudo compartir la liga.", e)
            }
          }
        }
      }

      // Punto de partida (antes en el perfil): si se saltó, invitación a hacerlo; si ya se hizo, repetirlo.
      item {
        Column {
          SpaceSectionTitle(
            "Tu punto de partida",
            hint = if (baseline == null) "4 juegos cortos para que cada juego empiece a tu medida" else "Puedes repetirlo cuando quieras",
            fontFamily = TitleFamily
          )
          Spacer(Modifier.height(12.dp))
          ClayButton(
            text = if (baseline == null) "Encontrar mi punto de partida" else "Repetir la evaluación",
            onClick = { viewModel.startBaseline() },
            color = if (baseline == null) Clay.Sun else Clay.Cream,
            modifier = Modifier.testTag("btn_profile_baseline")
          )
        }
      }
    }
  }

  if (plusSheet) PlusLockSheet(onDismiss = { plusSheet = false })       // sin Plus: qué trae la página, con «Entendido» (la compra llega con Google Play Billing, Etapa 4)
}

/** La puerta del Contrarreloj con el récord del juego de HOY (el mismo de Hoy); 2 dp de aire propio arriba y abajo sobre los 22 dp entre secciones (24 dp hasta cada vecina). Con Plus abre la página con el juego de hoy elegido; sin Plus, [onLocked]. */
@Composable
private fun DoorRow(results: List<com.example.data.TimeTrialResult>, plus: Boolean, viewModel: NeuroVidaViewModel, onLocked: () -> Unit) {
  val todayKey = viewModel.todayKey()
  val record = remember(results, todayKey) { TimeTrialStats.record(results, TimeTrialDay.gameFor(todayKey)) }
  ContrarrelojDoor(record, plus, onClick = { if (plus) viewModel.openContrarreloj() else onLocked() }, modifier = Modifier.padding(vertical = 2.dp))
}
