package com.example.ui.components

import androidx.compose.foundation.layout.Arrangement
import androidx.compose.foundation.layout.Column
import androidx.compose.foundation.layout.Row
import androidx.compose.foundation.layout.Spacer
import androidx.compose.foundation.layout.fillMaxWidth
import androidx.compose.foundation.layout.heightIn
import androidx.compose.foundation.layout.padding
import androidx.compose.foundation.layout.width
import androidx.compose.runtime.Composable
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.platform.testTag
import androidx.compose.ui.semantics.contentDescription
import androidx.compose.ui.semantics.semantics
import androidx.compose.ui.text.font.FontWeight
import androidx.compose.ui.unit.Dp
import androidx.compose.ui.unit.dp
import androidx.compose.ui.unit.sp
import com.example.data.Week
import com.example.ui.theme.NubiTokens

/** La caja de cada marca de la semana en Hoy: 36 dp (la llamita encendida mide 29 dp dentro y deja sitio al aro de «hoy»; hasta la Tarea 222, la farola del kit dibujaba 28). */
val HOY_LAMP_BOX = 36.dp

/**
 * Los siete farolitos de la semana (los días jugados, encendidos; hoy con un aro lima) y tu racha a su lado, a la izquierda, donde el fondo no tiene luceros. Lo usa Hoy (22 dp) y lo usará Avance («Tu semana», 28 dp con la farola
 * renderizada del kit): antes vivía dentro de `HomeScreen` (Tarea 124). [streakNote] = la línea de debajo: «Descansaste el martes: tu racha sigue.» (el día perdonado, si lo hubo) o, por defecto, «Un día de descanso por semana no la corta.». [streakText] = lo que va junto a la llama (por defecto «N días»; con la racha en 0, «Tu racha empieza hoy»: Tarea 186).
 */
@OptIn(androidx.compose.foundation.layout.ExperimentalLayoutApi::class)
@Composable
fun HoyWeek(lamps: Week.Lamps, streak: Int, streakNote: String? = null, lampSize: Dp = HOY_LAMP_BOX, modifier: Modifier = Modifier, streakText: String? = null, glintVisible: Boolean? = null) {
  // El destello de la llama de hoy (Tarea 223): solo en Hoy (el resumen del día no pasa [glintVisible]); `glintVisible` = si Hoy está a la vista y con sus datos al día
  val todayGlint = if (glintVisible != null) rememberTodayGlint(todayLit = lamps.played.getOrNull(lamps.todayIndex) == true, visible = glintVisible) else null
  Column(modifier.fillMaxWidth().padding(start = 20.dp, end = 16.dp, top = 4.dp, bottom = 2.dp)) {
    // Las farolas a 28 dp, como en Avance, Ajustes y el Primer vuelo (Tarea 147, P7: antes 22-24 y la fila se veía más chica que todo lo demás); si «7 días» no cabe al lado, baja en vez de apretarlas
    androidx.compose.foundation.layout.FlowRow(horizontalArrangement = Arrangement.spacedBy(12.dp), verticalArrangement = Arrangement.spacedBy(2.dp)) {
      val spoken = Week.lampsSpoken(lamps.daysPlayed)
      Row(Modifier.testTag("home_week").semantics { contentDescription = spoken }) {
        // las llamitas (Tarea 223; hasta la Tarea 222, las farolas del kit); el aro lima de «hoy» se queda, y solo la llama de hoy destella
        lamps.played.forEachIndexed { i, on -> WeekLampRendered(on = on, today = i == lamps.todayIndex, size = lampSize, glint = if (i == lamps.todayIndex && todayGlint != null) ({ todayGlint.value }) else null) }
      }
      // un solo sol en Hoy (P7): la racha va en crema (OnNight), no en sol; el sol queda para el botón grande y la piedrita «Sigue»
      Row(Modifier.heightIn(min = HOY_LAMP_BOX), verticalAlignment = Alignment.CenterVertically) {
        Llamita(lit = streak > 0, box = STREAK_LAMP_BOX)                                      // la MISMA llamita de la semana, más chica: un símbolo por concepto (Tarea 223; antes la gota del kit a 16 dp)
        Spacer(Modifier.width(2.dp))
        OnBoardText(
          streakText ?: if (streak == 1) "1 día" else "$streak días", color = NubiTokens.OnNight, fontSize = 16.sp, fontWeight = FontWeight.Bold,
          modifier = Modifier.testTag("streak_pill")
        )
      }
    }
    if (streakNote != null) {
      OnBoardText(streakNote, color = NubiTokens.OnNightDim, fontSize = 16.sp, lineHeight = 20.sp, modifier = Modifier.padding(top = 2.dp).testTag("streak_note"))
    }
  }
}

/** La caja de la llamita de la racha, junto a «N días» (24 dp: la llama encendida mide 19 dp). */
val STREAK_LAMP_BOX = 24.dp

/**
 * Una marca de la semana de Hoy, del resumen del día y de «Tu semana» de Avance (Tarea 125; desde la Tarea 223 es una [Llamita], antes la farola del kit): encendida (día jugado) o apagada; «hoy» es un aro lima de 2,5 dp (forma, no solo color).
 * La caja mide [size] (36 dp en Hoy). [glint] es el avance del destello de la llama de hoy (solo Hoy; ver [rememberTodayGlint]).
 */
@Composable
fun WeekLampRendered(on: Boolean, today: Boolean, modifier: Modifier = Modifier, size: Dp = 36.dp, glint: (() -> Float)? = null) {
  Llamita(lit = on, modifier = modifier, box = size, today = today, glint = glint ?: { 1f })
}
