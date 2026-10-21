package com.example.ui.components

import androidx.compose.foundation.Canvas
import androidx.compose.foundation.layout.Arrangement
import androidx.compose.foundation.layout.Box
import androidx.compose.foundation.layout.Column
import androidx.compose.foundation.layout.Row
import androidx.compose.foundation.layout.Spacer
import androidx.compose.foundation.layout.fillMaxWidth
import androidx.compose.foundation.layout.heightIn
import androidx.compose.foundation.layout.padding
import androidx.compose.foundation.layout.size
import androidx.compose.foundation.layout.width
import androidx.compose.material3.Icon
import androidx.compose.runtime.Composable
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.draw.drawBehind
import androidx.compose.ui.geometry.Offset
import androidx.compose.ui.graphics.Brush
import androidx.compose.ui.graphics.Color
import androidx.compose.ui.graphics.drawscope.Stroke
import androidx.compose.ui.platform.testTag
import androidx.compose.ui.semantics.contentDescription
import androidx.compose.ui.semantics.semantics
import androidx.compose.ui.text.font.FontWeight
import androidx.compose.ui.unit.Dp
import androidx.compose.ui.unit.dp
import androidx.compose.ui.unit.sp
import com.example.data.Week
import com.example.ui.theme.Clay
import com.example.ui.theme.NubiTokens

private val WeekLampCream = Color(0xFFF6EBD3)

/** La caja de cada farola de la semana en Hoy: 36 dp (la farola del kit dibuja 28 dentro, como en Avance, y deja sitio al aro de «hoy»). */
val HOY_LAMP_BOX = 36.dp

/**
 * Los siete farolitos de la semana (los días jugados, encendidos; hoy con un aro lima) y tu racha a su lado, a la izquierda, donde el fondo no tiene luceros. Lo usa Hoy (22 dp) y lo usará Avance («Tu semana», 28 dp con la farola
 * renderizada del kit): antes vivía dentro de `HomeScreen` (Tarea 124). [streakNote] = la línea de debajo: «Descansaste el martes: tu racha sigue.» (el día perdonado, si lo hubo) o, por defecto, «Un día de descanso por semana no la corta.».
 */
@OptIn(androidx.compose.foundation.layout.ExperimentalLayoutApi::class)
@Composable
fun HoyWeek(lamps: Week.Lamps, streak: Int, streakNote: String? = null, lampSize: Dp = HOY_LAMP_BOX, modifier: Modifier = Modifier) {
  Column(modifier.fillMaxWidth().padding(start = 20.dp, end = 16.dp, top = 4.dp, bottom = 2.dp)) {
    // Las farolas a 28 dp, como en Avance, Ajustes y el Primer vuelo (Tarea 147, P7: antes 22-24 y la fila se veía más chica que todo lo demás); si «7 días» no cabe al lado, baja en vez de apretarlas
    androidx.compose.foundation.layout.FlowRow(horizontalArrangement = Arrangement.spacedBy(12.dp), verticalArrangement = Arrangement.spacedBy(2.dp)) {
      val spoken = Week.lampsSpoken(lamps.daysPlayed)
      Row(Modifier.testTag("home_week").semantics { contentDescription = spoken }) {
        // las farolas del kit (Tarea 143, ítem 11: antes Hoy dibujaba discos y Avance, Ajustes y el Primer vuelo farolas); el aro lima de «hoy» se queda
        lamps.played.forEachIndexed { i, on -> WeekLampRendered(on = on, today = i == lamps.todayIndex, size = lampSize) }
      }
      // un solo sol en Hoy (P7): la racha va en crema (OnNight), no en sol; el sol queda para el botón grande y la piedrita «Sigue»
      Row(Modifier.heightIn(min = HOY_LAMP_BOX), verticalAlignment = Alignment.CenterVertically) {
        KitLlama(lit = streak > 0, size = 16.dp)
        Spacer(Modifier.width(6.dp))
        OnBoardText(
          if (streak == 1) "1 día" else "$streak días", color = NubiTokens.OnNight, fontSize = 16.sp, fontWeight = FontWeight.Bold,
          modifier = Modifier.testTag("streak_pill")
        )
      }
    }
    if (streakNote != null) {
      OnBoardText(streakNote, color = NubiTokens.OnNightDim, fontSize = 16.sp, lineHeight = 20.sp, modifier = Modifier.padding(top = 2.dp).testTag("streak_note"))
    }
  }
}

/** Un farolito: encendido = disco sol con resplandor; apagado = anillo tenue (la forma cambia, no solo el color); hoy lleva además un aro lima. */
@Composable
fun WeekLamp(on: Boolean, today: Boolean, size: Dp = 22.dp) {
  Canvas(Modifier.size(size)) {
    val c = center
    val r = this.size.minDimension * 0.34f
    if (on) {
      drawCircle(Brush.radialGradient(listOf(Clay.Sun.copy(alpha = 0.55f), Color.Transparent), c, this.size.minDimension / 2f), this.size.minDimension / 2f, c)
      drawCircle(Clay.Sun, r, c)
      drawCircle(Color.White.copy(alpha = 0.7f), r * 0.32f, Offset(c.x - r * 0.3f, c.y - r * 0.3f))
    } else {
      drawCircle(WeekLampCream.copy(alpha = 0.16f), r, c)
      drawCircle(WeekLampCream.copy(alpha = 0.5f), r, c, style = Stroke(1.5.dp.toPx()))
    }
    if (today) drawCircle(Clay.Lime, this.size.minDimension / 2f - 1.5.dp.toPx(), c, style = Stroke(2.5.dp.toPx()))
  }
}

/**
 * Una farola de «Tu semana» de Avance (Tarea 125): la del kit (la de la isla y del ícono de Avance), apagada o encendida, de 28 dp de alto; «hoy» es un aro lima de 2,5 dp dibujado acá (forma, no solo color). La caja mide 36 dp para el aro.
 */
@Composable
fun WeekLampRendered(on: Boolean, today: Boolean, modifier: Modifier = Modifier, size: Dp = 36.dp) {
  Box(
    modifier.size(size).drawBehind { if (today) drawCircle(Clay.Lime, this.size.minDimension / 2f - 1.5.dp.toPx(), center, style = Stroke(2.5.dp.toPx())) },
    contentAlignment = Alignment.Center
  ) { FarolaImage(on, size * 28f / 36f) }
}
