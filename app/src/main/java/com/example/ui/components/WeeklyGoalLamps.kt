package com.example.ui.components

import androidx.compose.animation.core.Animatable
import androidx.compose.animation.core.animateFloatAsState
import androidx.compose.animation.core.tween
import androidx.compose.foundation.background
import androidx.compose.foundation.layout.Arrangement
import androidx.compose.foundation.layout.Box
import androidx.compose.foundation.layout.BoxWithConstraints
import androidx.compose.foundation.layout.Column
import androidx.compose.foundation.layout.Row
import androidx.compose.foundation.layout.Spacer
import androidx.compose.foundation.layout.fillMaxWidth
import androidx.compose.foundation.layout.height
import androidx.compose.foundation.layout.padding
import androidx.compose.foundation.layout.width
import androidx.compose.foundation.shape.RoundedCornerShape
import androidx.compose.foundation.selection.selectable
import androidx.compose.foundation.selection.selectableGroup
import androidx.compose.material3.Text
import androidx.compose.material3.minimumInteractiveComponentSize
import androidx.compose.runtime.Composable
import androidx.compose.runtime.LaunchedEffect
import androidx.compose.runtime.getValue
import androidx.compose.runtime.mutableIntStateOf
import androidx.compose.runtime.remember
import androidx.compose.runtime.setValue
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.draw.alpha
import androidx.compose.ui.draw.drawBehind
import androidx.compose.ui.draw.scale
import androidx.compose.ui.graphics.Brush
import androidx.compose.ui.graphics.Color
import androidx.compose.ui.platform.testTag
import androidx.compose.ui.semantics.Role
import androidx.compose.ui.semantics.contentDescription
import androidx.compose.ui.semantics.semantics
import androidx.compose.ui.text.font.FontWeight
import androidx.compose.ui.unit.Dp
import androidx.compose.ui.unit.dp
import androidx.compose.ui.unit.sp
import com.example.ui.theme.Clay

/** Los días de la meta semanal: de 1 a 7. */
const val WEEKLY_GOAL_MIN = 1
const val WEEKLY_GOAL_MAX = 7

/**
 * La meta semanal de Ajustes (Tarea 130; Fable, `auditoria-fable/spec-kit-juice-ajustes.md` §3.3): 7 farolas del kit (la misma de Hoy y Avance), encendidas de la 1 a la meta, con su número 1 a 7 debajo (16 sp en #514B78):
 * los números hacen que se lea como CANTIDAD (en Hoy y Avance la misma farola significa lunes … domingo). Reemplaza al `Slider` de Material, el único control de la app que no era del kit.
 *
 * Cada farola es un botón de radio (`Role.RadioButton`, `selected` la de la meta) en una caja de 46 dp con el toque mínimo de Material; todas juntas son un grupo (`selectableGroup`). Se enciende con un fundido de
 * [NubiMotion.STATE_MS] y la que se toca da un pulso de [NubiMotion.CELEBRATE_MS]; con «quitar animaciones» todo salta a su estado final. Al apoyar el dedo, el tic del kit ([rememberKitTap]).
 *
 * La elegida se marca también por FORMA, no solo por brillo: su número va dentro de un chip sol con la letra en tinta (Opus, 18-oct: «cuesta ver cuál está elegida»); las encendidas llevan un halo sol, más fuerte en la elegida.
 *
 * [goal] es la meta de hoy; [onGoal] recibe la que se toca (no hay que validar el rango: solo existen 1 a 7).
 */
@Composable
fun WeeklyGoalLamps(goal: Int, onGoal: (Int) -> Unit, modifier: Modifier = Modifier) {
  val tap = rememberKitTap()
  val reduce = rememberReduceMotion()
  BoxWithConstraints(modifier.fillMaxWidth().testTag("goal_lamps")) {
    val box = minOf(46.dp, maxWidth / WEEKLY_GOAL_MAX)                       // 7 × 46 = 322 dp: a 393 dp caben; en pantallas angostas las cajas se achican parejo
    Row(Modifier.fillMaxWidth().selectableGroup(), horizontalArrangement = Arrangement.SpaceBetween, verticalAlignment = Alignment.Top) {
      for (k in WEEKLY_GOAL_MIN..WEEKLY_GOAL_MAX) GoalLamp(k = k, on = k <= goal, selected = k == goal, box = box, reduce = reduce, onTap = { tap(); onGoal(k) })
    }
  }
}

private const val LAMP_DP = 32                                                   // la farola: 32 dp de alto (28 antes), dentro de la caja de 46

@Composable
private fun GoalLamp(k: Int, on: Boolean, selected: Boolean, box: Dp, reduce: Boolean, onTap: () -> Unit) {
  val glow by animateFloatAsState(if (on) 1f else 0f, NubiMotion.spec(reduce, NubiMotion.STATE_MS), label = "farola_meta")
  val halo by animateFloatAsState(if (selected) 0.9f else if (on) 0.55f else 0f, NubiMotion.spec(reduce, NubiMotion.STATE_MS), label = "farola_meta_halo")
  val pulse = remember { Animatable(1f) }
  var pulseKey by remember { mutableIntStateOf(0) }
  LaunchedEffect(pulseKey) {
    if (pulseKey > 0 && !reduce) {                                          // el pulso de la tocada: 1 → 1,12 → 1 en CELEBRATE_MS
      pulse.animateTo(1.12f, tween(NubiMotion.CELEBRATE_MS / 2, easing = NubiMotion.Standard))
      pulse.animateTo(1f, tween(NubiMotion.CELEBRATE_MS / 2, easing = NubiMotion.Standard))
    }
  }
  Column(
    Modifier
      .width(box)
      .minimumInteractiveComponentSize()
      .selectable(selected = selected, role = Role.RadioButton, onClick = { pulseKey++; onTap() })
      .semantics { contentDescription = "$k días por semana" }
      .testTag("goal_lamp_$k"),
    horizontalAlignment = Alignment.CenterHorizontally
  ) {
    Box(
      Modifier
        .scale(pulse.value)
        .drawBehind {                                                           // el halo sol de las encendidas (más fuerte en la elegida)
          if (halo > 0.01f) drawCircle(Brush.radialGradient(listOf(Clay.Sun.copy(alpha = halo), Color.Transparent), center = center, radius = 24.dp.toPx()), radius = 24.dp.toPx(), center = center)
        },
      contentAlignment = Alignment.Center
    ) {
      FarolaImage(on = false, height = LAMP_DP.dp)                           // la apagada, siempre debajo; la encendida se funde encima
      FarolaImage(on = true, height = LAMP_DP.dp, modifier = Modifier.alpha(glow))
    }
    Spacer(Modifier.height(2.dp))
    Box(Modifier.background(if (selected) Clay.Sun else Color.Transparent, RoundedCornerShape(10.dp)).padding(horizontal = 8.dp, vertical = 1.dp)) {    // la elegida, por forma: su número en un chip sol con tinta
      Text(text = "$k", fontSize = 16.sp, fontWeight = FontWeight.Bold, color = if (selected) Clay.Ink else Clay.InkMuted)
    }
  }
}
