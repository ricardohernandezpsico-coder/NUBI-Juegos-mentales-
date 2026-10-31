package com.example.ui.screens

import androidx.compose.foundation.background
import androidx.compose.foundation.layout.Arrangement
import androidx.compose.foundation.layout.Box
import androidx.compose.foundation.layout.Column
import androidx.compose.foundation.layout.ExperimentalLayoutApi
import androidx.compose.foundation.layout.FlowRow
import androidx.compose.foundation.layout.Row
import androidx.compose.foundation.layout.Spacer
import androidx.compose.foundation.layout.fillMaxSize
import androidx.compose.foundation.layout.fillMaxWidth
import androidx.compose.foundation.layout.height
import androidx.compose.foundation.layout.padding
import androidx.compose.foundation.layout.width
import androidx.compose.foundation.rememberScrollState
import androidx.compose.foundation.verticalScroll
import androidx.compose.material3.Text
import androidx.compose.runtime.Composable
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.geometry.Offset
import androidx.compose.ui.geometry.Size
import androidx.compose.ui.graphics.BlendMode
import androidx.compose.ui.graphics.Brush
import androidx.compose.ui.graphics.Color
import androidx.compose.ui.graphics.CompositingStrategy
import androidx.compose.ui.draw.drawWithContent
import androidx.compose.ui.graphics.graphicsLayer
import androidx.compose.ui.platform.testTag
import androidx.compose.ui.semantics.contentDescription
import androidx.compose.ui.semantics.semantics
import androidx.compose.ui.text.font.FontWeight
import androidx.compose.ui.text.style.TextAlign
import androidx.compose.ui.unit.Dp
import androidx.compose.ui.unit.dp
import androidx.compose.ui.unit.sp
import com.example.data.AreaPoints
import com.example.data.SessionSummary
import com.example.data.Week
import com.example.model.AppLanguage
import com.example.model.DomainType
import com.example.model.GameRegistry
import com.example.ui.components.HoyWeek
import com.example.ui.components.MiniPlanet
import com.example.ui.components.NubiClay
import com.example.ui.components.NubiSize
import com.example.ui.components.OnBoardText
import com.example.ui.components.SlabSize
import com.example.ui.components.areaDescription
import com.example.ui.components.domainOf
import com.example.ui.i18n.getGameTitle
import com.example.ui.theme.Clay
import com.example.ui.theme.ClayButton
import com.example.ui.theme.ClayCard

private val OnNightDim = com.example.ui.theme.NubiTokens.OnNightDim

/**
 * El aire sobre Nubi: 12 dp del saltito (`NubiHop.JUMP_DP`), 6 de la flotación y 2 de margen. La columna que se desliza recorta lo que sale de su caja: con 8 dp de aire y un salto de 12 dp, el borde de arriba de Nubi se iba 4 dp fuera (lo vio Ricardo en su teléfono).
 * `SessionSummaryScreenTest` compone con el reloj en lo alto del salto y mide que Nubi queda entera dentro del área visible.
 */
internal val NUBI_AIR = 20.dp

/** El alto del desvanecido del borde de abajo del área que se desliza (aparece solo mientras queda contenido por ver). */
internal val BOTTOM_FADE = 24.dp

/** El aire entre el final del área que se desliza y la frase del pie. */
internal val FOOTER_AIR = 16.dp

/**
 * La ruta de hoy completa (29-sep, pedido de Ricardo: «una lámina que señale qué se trabajó y cuánto avance»; Tarea 198, 31-oct: «se ve poco clara y desordenada», y eligió la opción B de Fable, `auditoria-fable/resumen-del-dia.md`, «Una losa, como el final de partida»).
 * De arriba abajo: Nubi (S, celebra con su saltito; 20 dp de aire arriba), el título (Rubik) y la frase, sobre el cielo con `OnBoardText`; UNA losa crema con dos secciones separadas por una línea fina: «Tus juegos» (los 3 de la ruta con su planetita y el visto, el nombre y lo que sumó:
 * «12 puntos en Atención»; si no sumó, «Hecho») y «Tu avance» (por área, el nombre a la izquierda y «330 de 1000 puntos» a la derecha en una línea —si no caben, uno bajo el otro— y debajo la barra de los cuadros de Avance, con lo de hoy a media tinta); debajo de la losa, la semana de Hoy
 * ([HoyWeek]: siete farolas, la de hoy con el aro lima, la gota y la racha); y el pie fijo con «Nubi te espera mañana para seguir» y «Volver a Hoy» (el único sol). Cada número sale UNA vez: ya no hay «Hoy sumaste N puntos.» ni «Racha: N días» suelta (el lector de pantalla sigue leyendo cada área entera: [areaDescription]).
 */
@OptIn(ExperimentalLayoutApi::class)
@Composable
fun SessionSummaryScreen(
  summary: SessionSummary,
  streak: Int,
  lamps: Week.Lamps,
  lang: AppLanguage,
  onClose: () -> Unit,
  modifier: Modifier = Modifier
) {
  val names = summary.areas.map { domainOf(it.area)?.displayName ?: it.area }
  Column(modifier.fillMaxSize().testTag("session_summary")) {
    val scroll = rememberScrollState()
    Box(Modifier.weight(1f).fillMaxWidth()) {
    Column(
      // el borde de abajo se DESVANECE mientras queda contenido por ver (24 dp, hacia lo que hay detrás: el cielo): lo que sigue se disuelve en vez de cortarse en seco sobre el pie. Va FUERA del `verticalScroll` para dibujarse sobre el área visible y no sobre lo que se desliza.
      Modifier.fillMaxSize().testTag("summary_scroll")
        .graphicsLayer { compositingStrategy = CompositingStrategy.Offscreen }
        .drawWithContent {
          drawContent()
          if (scroll.canScrollForward) {
            val fade = BOTTOM_FADE.toPx()
            drawRect(Brush.verticalGradient(listOf(Color.Black, Color.Transparent), startY = size.height - fade, endY = size.height), topLeft = Offset(0f, size.height - fade), size = Size(size.width, fade), blendMode = BlendMode.DstIn)
          }
        }
        .verticalScroll(scroll).padding(horizontal = 24.dp),
      horizontalAlignment = Alignment.CenterHorizontally
    ) {
      Spacer(Modifier.height(NUBI_AIR))
      NubiClay(NubiSize.S, Modifier.testTag("summary_nubi"), celebrate = true)
      OnBoardText("¡Ruta de hoy completa!", color = Color.White, fontSize = 28.sp, fontWeight = FontWeight.Bold, fontFamily = com.example.ui.theme.TitleFamily, textAlign = TextAlign.Center, modifier = Modifier.fillMaxWidth().testTag("summary_title"))
      OnBoardText(
        "Trabajaste " + joinNames(names.map { it.lowercase() }),
        color = OnNightDim, fontSize = 16.sp, textAlign = TextAlign.Center, modifier = Modifier.fillMaxWidth().padding(top = 4.dp).testTag("summary_sentence")
      )

      // la losa (Tarea 198): dentro de ella la letra va en tinta y los textos son `Text` normales; los luceros del cielo quedan detrás
      ClayCard(Modifier.fillMaxWidth().padding(top = 16.dp).testTag("summary_card"), color = Clay.Cream, size = SlabSize.LARGE, contentPadding = 0.dp) {
        Column(Modifier.fillMaxWidth().padding(start = 16.dp, end = 16.dp, top = 12.dp, bottom = 12.dp)) {
          SectionTitle("Tus juegos", "summary_games_title")
          summary.games.forEach { g ->
            val def = GameRegistry.getById(g.gameId)
            Row(verticalAlignment = Alignment.CenterVertically, modifier = Modifier.fillMaxWidth().padding(vertical = 6.dp).testTag("summary_row_${g.gameId}")) {
              MiniPlanet(g.gameId, 36.dp, done = g.played)
              Spacer(Modifier.width(12.dp))
              Column(Modifier.weight(1f)) {
                Text(def?.let { getGameTitle(it.id, lang, it.title) } ?: g.gameId, color = Clay.Ink, fontSize = 16.sp, fontWeight = FontWeight.SemiBold, modifier = Modifier.testTag("summary_name_${g.gameId}"))
                val areaName = domainOf(g.area)?.displayName ?: ""
                // los PUNTOS DE AVANCE que sumó hoy en su área (la misma escala del resto de la app) o «Te mantuviste en Razonamiento» (Tarea 198: antes «Hecho», una palabra suelta que no decía nada del área); sin partida, el área (Tarea 159: antes «puntaje de 100» de la partida, otra escala)
                Text(
                  if (!g.played) areaName else AreaPoints.gameLine(g.gained, areaName),
                  color = Clay.InkMuted, fontSize = 16.sp, modifier = Modifier.testTag("summary_game_${g.gameId}")
                )
              }
            }
          }

          Box(Modifier.fillMaxWidth().padding(vertical = 12.dp).height(1.dp).background(Clay.Ink.copy(alpha = 0.15f)).testTag("summary_divider"))

          SectionTitle("Tu avance", "summary_progress_title")
          summary.areas.forEachIndexed { i, a ->
            Column(Modifier.fillMaxWidth().padding(vertical = 8.dp).semantics(mergeDescendants = true) { contentDescription = areaDescription(names[i], a) }.testTag("summary_area_${a.area.lowercase()}")) {
              // el nombre a la izquierda y los puntos a la derecha en UNA línea; con letra grande, uno bajo el otro (la palabra nunca se parte)
              FlowRow(Modifier.fillMaxWidth(), horizontalArrangement = Arrangement.SpaceBetween) {
                Text(names[i], color = Clay.Ink, fontSize = 16.sp, fontWeight = FontWeight.SemiBold, modifier = Modifier.testTag("summary_area_name_${a.area.lowercase()}"))
                Text(
                  if (a.measured) "${a.total} de ${AreaPoints.SCALE} puntos" else "Aún sin medir",
                  color = Clay.InkMuted, fontSize = 16.sp, modifier = Modifier.testTag("summary_area_points_${a.area.lowercase()}")
                )
              }
              Spacer(Modifier.height(6.dp))
              AreaBar(if (a.measured) a.fill else 0f, DomainType.fromStored(a.area)?.color ?: Clay.Ink, "summary_${a.area.lowercase()}", change = if (a.measured) a.change else 0f)
              if (!a.measured) Text("Se mide al jugarlo a tu medida", color = Clay.InkMuted, fontSize = 16.sp, modifier = Modifier.padding(top = 4.dp))
            }
          }
        }
      }
      // fin de la losa

      // la semana de Hoy (siete farolas, la de hoy con el aro lima, la gota a 16 dp y la racha), en lugar de la línea suelta «Racha: N días». «N días» es el texto de siempre de `HoyWeek`; la frase de la racha («Tu racha empieza hoy», el descanso) solo se arma en Hoy (`LaFraseDeLaRachaTest`),
      // así que con la racha en 0 (el flujo aún no contó la partida de recién) no se dice nada junto a la gota
      HoyWeek(lamps, streak, streakText = if (streak > 0) null else "", lampSize = 32.dp, modifier = Modifier.padding(top = 10.dp).testTag("summary_week"))         // farolas de 32 dp (en Hoy, 36): con 36 la racha bajaba sola bajo las farolas a 393 dp
      Spacer(Modifier.height(8.dp))
    }
    // la marca del desvanecido (sin dibujo propio: el desvanecido lo dibuja el área que se desliza): solo está mientras queda contenido por ver
    if (scroll.canScrollForward) Box(Modifier.align(Alignment.BottomCenter).fillMaxWidth().height(BOTTOM_FADE).testTag("summary_fade"))
    }
    // 16 dp entre el final del área que se desliza y la frase del pie: con menos, el borde de la losa o un texto cortado quedaba pegado a «Nubi te espera…» (Tarea 198)
    Column(Modifier.fillMaxWidth().padding(start = 24.dp, end = 24.dp, bottom = 12.dp, top = FOOTER_AIR), verticalArrangement = Arrangement.spacedBy(8.dp)) {
      OnBoardText(
        "Nubi te espera mañana para seguir",
        color = OnNightDim, fontSize = 16.sp, textAlign = TextAlign.Center, modifier = Modifier.fillMaxWidth().testTag("summary_footer_line")
      )
      ClayButton(text = "Volver a Hoy", onClick = onClose, modifier = Modifier.testTag("btn_summary_close"))
    }
  }
}

/** El título de una sección de la losa: Rubik 20 sp, caja normal, en tinta (como los títulos de Avance, pero sobre crema). */
@Composable
private fun SectionTitle(text: String, tag: String) {
  Text(text, color = Clay.Ink, fontSize = 20.sp, fontWeight = FontWeight.Bold, fontFamily = com.example.ui.theme.TitleFamily, lineHeight = 25.sp, modifier = Modifier.fillMaxWidth().padding(bottom = 2.dp).testTag(tag))
}

/** "memoria", "memoria y atención", "memoria, atención y razonamiento". */
private fun joinNames(names: List<String>): String = when (names.size) {
  0 -> "hoy"
  1 -> names[0]
  else -> names.dropLast(1).joinToString(", ") + " y " + names.last()
}
