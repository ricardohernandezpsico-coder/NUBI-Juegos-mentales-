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
import androidx.compose.runtime.LaunchedEffect
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
import com.example.data.Streak
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
 * **Sin la puerta del Contrarreloj** (Tarea 215, 2-nov): la losa «Tus marcas del Contrarreloj» de la Tarea 199 y su hoja del candado se fueron de aquí. La página del Contrarreloj es la pestaña de al lado (`ContrarrelojScreen`) y Avance vuelve a ser solo las cuatro áreas, «Tus mejores marcas» y el punto de partida.
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
  val ladderProgress by viewModel.gameLadderProgress.collectAsState()          // el nivel de «Tu nivel: …» sale de la escalera entera, no de los puntos (Tarea 233)
  val measures by viewModel.starMeasures.collectAsState()
  val baseline by viewModel.baseline.collectAsState()
  val settings by viewModel.userSettings.collectAsState()
  val streak by viewModel.currentStreak.collectAsState()
  // Cuándo se jugó, como en Hoy (Tarea 227): las partidas de siempre Y el Contrarreloj. Con solo `history` la semana de Avance decía «0 de 4 días» mientras Hoy decía «2 de 4» si esos días solo se había jugado Contrarreloj.
  val activity by viewModel.activityTimestamps.collectAsState()
  val currentLang = LocalAppLanguage.current
  val avanceArea by viewModel.avanceArea.collectAsState()        // la página abierta: la recuerda el ViewModel (rotar la pantalla no la cierra; cambiar de pestaña, sí)
  val listState = rememberLazyListState()                       // la posición de la lista queda aquí: al volver de la página de un área, Avance está donde estaba
  val context = androidx.compose.ui.platform.LocalContext.current
  val shareScope = androidx.compose.runtime.rememberCoroutineScope()

  val avg = if (gameRanks.isEmpty()) 0 else gameRanks.sumOf { it.rating } / gameRanks.size
  val tier = RankTier.fromRating(avg)
  // El reloj de Hoy (cada 30 s, Tarea 227): `now` se renueva al cambiar el día aunque la app siga abierta; antes quedaba el de la última partida y, pasada la medianoche (o el lunes), la semana y los puntos «de esta semana» eran los de ayer.
  var clock by remember { mutableStateOf(System.currentTimeMillis()) }
  LaunchedEffect(Unit) {
    while (true) {
      kotlinx.coroutines.delay(30_000L)
      clock = System.currentTimeMillis()
    }
  }
  val today = Streak.dayOf(clock)
  val now = remember(history, progressLog, today) { System.currentTimeMillis() }
  val names = remember { DomainType.values().associate { it.name to it.displayName } }
  val areaPoints = remember(progressLog, now) { AreaPoints.forAreas(AREA_ORDER, progressLog, now) }
  val lamps = remember(activity, now) { Week.lampsFor(activity, now) }
  val bestMarks = remember(measures) { StarMeasures.bestMarks(measures) }
  val lastPlayed = remember(history) { history.groupBy { it.gameId }.mapValues { (_, l) -> l.maxOf { it.timestamp } } }

  if (avanceArea != null) BackHandler(enabled = com.example.ui.components.LocalTabActive.current, onBack = viewModel::closeAvanceArea)        // Atrás cierra la página y vuelve a la lista (solo con Avance a la vista: escondida en otra pestaña no atrapa el Atrás, Tarea 224)

  Column(modifier.fillMaxSize().padding(bottom = com.example.ui.components.LocalBarInset.current)) {      // la franja de la barra de pestañas queda libre
    val openKey = avanceArea
    val open = openKey?.let { k -> areaPoints.firstOrNull { it.area == k } }
    if (open != null) {
      val weekly = remember(open.area, progressLog, now) { AreaPoints.weekly(AreaPoints.gamesOf(open.area), progressLog, now) }
      AvanceAreaPage(
        points = open, weekly = weekly, areaName = names[open.area] ?: open.area,
        lastPlayed = lastPlayed, measures = measures, lang = currentLang, now = now, onBack = viewModel::closeAvanceArea, timedFor = viewModel::retoFor, ladderProgress = ladderProgress
      )
      return@Column
    }
    // La fila del título (Tarea 225b): 48 dp con el botón «Tú» adentro; el 8 dp de aire que sigue es el de la lista (la primera línea, las llamitas de la semana, empieza a 56 dp: la sombra del botón termina a los 48)
    com.example.ui.components.TuHeaderRow({ m -> com.example.ui.components.TuHeaderButton(viewModel, m) }, bottomGap = 0.dp) {
      OnBoardText("Avance", color = OnNight, fontSize = 26.sp, fontWeight = FontWeight.Bold, fontFamily = TitleFamily, modifier = Modifier.align(Alignment.CenterStart).padding(start = 20.dp, end = com.example.ui.components.TU_BUTTON_RESERVE).testTag("avance_title"))
    }
    LazyColumn(
      state = listState,
      modifier = Modifier
        .weight(1f)
        .padding(horizontal = 20.dp),                                       // 20 como el título; los cuadros de las áreas necesitan el ancho (Tarea 174)
      contentPadding = PaddingValues(top = 8.dp, bottom = 28.dp),
      verticalArrangement = Arrangement.spacedBy(22.dp)
    ) {
      item { AvanceWeekLine(lamps, settings.weeklyGoal, empty = activity.isEmpty()) }          // «Tu semana empieza hoy» solo si no se jugó NADA (ni Contrarreloj): quien solo jugó Contrarreloj ya tiene semana

      if (shownHistory.isEmpty()) {
        // Sin ninguna partida (Tarea 147, P8): en vez de cuatro filas vacías, Nubi con UNA línea
        item { com.example.ui.components.NubiEmptyLine(com.example.ui.components.EMPTY_AVANCE_LINE) }
      } else {
        item { AvanceAreaGrid(areaPoints, names) { viewModel.openAvanceArea(it) } }
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
                "¡Estoy en la liga ${tier.tierName} de Nubilus!"
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

}
