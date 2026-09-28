package com.example.ui.screens

import android.provider.Settings
import androidx.activity.compose.BackHandler
import androidx.compose.animation.core.Animatable
import androidx.compose.animation.core.RepeatMode
import androidx.compose.animation.core.animateFloat
import androidx.compose.animation.core.infiniteRepeatable
import androidx.compose.animation.core.rememberInfiniteTransition
import androidx.compose.animation.core.tween
import androidx.compose.foundation.Canvas
import androidx.compose.foundation.background
import androidx.compose.foundation.border
import androidx.compose.foundation.clickable
import androidx.compose.foundation.layout.Box
import androidx.compose.foundation.layout.BoxWithConstraints
import androidx.compose.foundation.layout.Column
import androidx.compose.foundation.layout.Row
import androidx.compose.foundation.layout.Spacer
import androidx.compose.foundation.layout.aspectRatio
import androidx.compose.foundation.layout.fillMaxSize
import androidx.compose.foundation.layout.fillMaxWidth
import androidx.compose.foundation.layout.height
import androidx.compose.foundation.layout.offset
import androidx.compose.foundation.layout.padding
import androidx.compose.foundation.layout.size
import androidx.compose.foundation.layout.width
import androidx.compose.foundation.rememberScrollState
import androidx.compose.foundation.shape.CircleShape
import androidx.compose.foundation.shape.RoundedCornerShape
import androidx.compose.foundation.verticalScroll
import androidx.compose.material3.Text
import androidx.compose.runtime.Composable
import androidx.compose.runtime.LaunchedEffect
import androidx.compose.runtime.getValue
import androidx.compose.runtime.remember
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.draw.clip
import androidx.compose.ui.draw.scale
import androidx.compose.ui.geometry.Offset
import androidx.compose.ui.graphics.Color
import androidx.compose.ui.graphics.Path
import androidx.compose.ui.graphics.PathEffect
import androidx.compose.ui.graphics.drawscope.Stroke
import androidx.compose.ui.platform.LocalContext
import androidx.compose.ui.platform.testTag
import androidx.compose.ui.text.font.FontWeight
import androidx.compose.ui.text.style.TextAlign
import androidx.compose.ui.unit.dp
import androidx.compose.ui.unit.sp
import com.example.data.Baseline
import com.example.data.BaselinePlan
import com.example.data.Education
import com.example.data.Percentile
import com.example.data.priorRating
import com.example.model.AgeBand
import com.example.model.DomainType
import com.example.model.GameRegistry
import com.example.ui.components.GameIcon
import com.example.ui.components.levelWord
import com.example.ui.theme.Clay
import com.example.ui.theme.ClayButton
import com.example.ui.theme.AppFamily
import com.example.viewmodel.BaselineRun
import kotlin.math.PI
import kotlin.math.cos
import kotlin.math.sin

private val OnNight = Color(0xFFEAF0FF)
private val OnNightDim = Color(0xFFB4BFEA)

/**
 * "Tu punto de partida" entre juego y juego de la evaluación y, al terminar, el mapa. Sin recuadros, sobre el cielo
 * de la app (el fondo lo pinta MainActivity):
 * - Mientras se juega: los 3 juegos como nodos del camino (hecho = check lima, siguiente = late), qué mide el
 *   siguiente y "Jugar". "Terminar después" estima el punto de partida y deja la evaluación en Perfil.
 * - Al terminar: radar de los 6 dominios (medidos = punto de arcilla; estimados = punto hueco y línea punteada),
 *   nivel en palabras y posición frente a personas de su edad y estudios ("estimación provisional"), y "Empezar
 *   mi camino". Nada de "edad cognitiva" ni diagnósticos: es un punto de partida para jugar.
 */
@Composable
fun BaselineScreen(
  run: BaselineRun,
  ageBand: AgeBand?,
  education: Education?,
  onContinue: () -> Unit,
  onLater: () -> Unit,
  onFinish: () -> Unit
) {
  val result = run.result
  BackHandler { if (result != null) onFinish() else onLater() }
  if (result == null) ProgressView(run.done, onContinue, onLater) else MapView(result, ageBand, education, onFinish)
}

@Composable
private fun reduceMotion(): Boolean {
  val context = LocalContext.current
  return remember { Settings.Global.getFloat(context.contentResolver, Settings.Global.ANIMATOR_DURATION_SCALE, 1f) == 0f }
}

// ------------------------------------------------------------------ entre juegos

@Composable
private fun ProgressView(done: Int, onContinue: () -> Unit, onLater: () -> Unit) {
  val steps = BaselinePlan.steps
  val next = steps.getOrNull(done) ?: return
  val nextGame = GameRegistry.getById(next.gameId)
  val still = reduceMotion()
  val pulse = if (still) 1f else {
    val t = rememberInfiniteTransition(label = "nextPulse")
    val v by t.animateFloat(1f, 1.08f, infiniteRepeatable(tween(900), RepeatMode.Reverse), label = "nextPulseV")
    v
  }

  Column(
    modifier = Modifier.fillMaxSize().padding(horizontal = 24.dp),
    horizontalAlignment = Alignment.CenterHorizontally
  ) {
    Spacer(Modifier.weight(0.3f))
    Text(
      if (done == 0) "Tu punto de partida" else "¡Bien! Vas $done de ${steps.size}",
      color = Color.White, fontFamily = AppFamily, fontWeight = FontWeight.Bold, fontSize = 30.sp, textAlign = TextAlign.Center
    )
    Text(
      "No es un examen: juega tranquilo, a tu ritmo.",
      color = OnNightDim, fontFamily = AppFamily, fontSize = 16.sp, textAlign = TextAlign.Center,
      modifier = Modifier.padding(top = 6.dp, bottom = 34.dp)
    )
    // Los 3 juegos como nodos del camino.
    Row(verticalAlignment = Alignment.CenterVertically) {
      steps.forEachIndexed { i, step ->
        if (i > 0) {
          Box(
            Modifier.width(34.dp).height(4.dp).clip(RoundedCornerShape(2.dp))
              .background(if (i <= done) Clay.Lime else Color.White.copy(alpha = 0.18f))
          )
        }
        val isDone = i < done
        val isNext = i == done
        Box(contentAlignment = Alignment.Center) {
          Box(
            modifier = Modifier
              .size(if (isNext) 84.dp else 66.dp)
              .scale(if (isNext) pulse else 1f)
              .clip(CircleShape)
              .background(if (isDone || isNext) step.domain.color else step.domain.color.copy(alpha = 0.3f))
              .border(3.dp, if (isNext) Clay.Sun else Clay.Ink, CircleShape),
            contentAlignment = Alignment.Center
          ) { GameIcon(step.gameId, size = if (isNext) 56.dp else 42.dp) }
          if (isDone) {
            Box(
              Modifier.align(Alignment.TopEnd).offset(x = 4.dp, y = (-4).dp).size(26.dp).clip(CircleShape)
                .background(Clay.Lime).border(2.5.dp, Clay.Ink, CircleShape),
              contentAlignment = Alignment.Center
            ) { CheckMark(Modifier.size(14.dp)) }
          }
        }
      }
    }
    Spacer(Modifier.height(30.dp))
    Text(
      "Siguiente: ${nextGame?.title ?: ""}",
      color = Clay.Sun, fontFamily = AppFamily, fontWeight = FontWeight.Bold, fontSize = 22.sp, textAlign = TextAlign.Center
    )
    Text(
      "${next.measures}. Dura alrededor de un minuto.",
      color = OnNight, fontFamily = AppFamily, fontSize = 16.sp, lineHeight = 22.sp, textAlign = TextAlign.Center,
      modifier = Modifier.padding(top = 6.dp)
    )
    Spacer(Modifier.weight(0.7f))
    ClayButton(
      text = if (done == 0) "Empezar" else "Jugar ${nextGame?.title ?: ""}",
      onClick = onContinue,
      modifier = Modifier.testTag("btn_baseline_next")
    )
    Text(
      "Terminar después",
      color = OnNight, fontFamily = AppFamily, fontWeight = FontWeight.SemiBold, fontSize = 16.sp,
      modifier = Modifier
        .padding(top = 8.dp)
        .clip(RoundedCornerShape(12.dp))
        .clickable(onClick = onLater)
        .padding(horizontal = 16.dp, vertical = 14.dp) // área de toque >= 48 dp
        .testTag("btn_baseline_later")
    )
    Spacer(Modifier.height(8.dp))
  }
}

/** Marca de hecho dibujada (no un carácter de la fuente). */
@Composable
private fun CheckMark(modifier: Modifier) {
  Canvas(modifier) {
    val w = size.width
    val p = Path().apply {
      moveTo(w * 0.12f, w * 0.52f)
      lineTo(w * 0.42f, w * 0.8f)
      lineTo(w * 0.9f, w * 0.22f)
    }
    drawPath(p, Clay.Ink, style = Stroke(w * 0.2f, cap = androidx.compose.ui.graphics.StrokeCap.Round, join = androidx.compose.ui.graphics.StrokeJoin.Round))
  }
}

// ------------------------------------------------------------------ el mapa

@Composable
private fun MapView(baseline: Baseline, ageBand: AgeBand?, education: Education?, onFinish: () -> Unit) {
  val still = reduceMotion()
  val grow = remember { Animatable(if (still) 1f else 0f) }
  LaunchedEffect(Unit) { if (!still) grow.animateTo(1f, tween(900)) }
  // Referencia para comparar: la provisional ajustada por edad y estudios ("personas parecidas a ti").
  val referenceMean = priorRating(ageBand, education)

  Column(
    modifier = Modifier.fillMaxSize().verticalScroll(rememberScrollState()).padding(horizontal = 24.dp),
    horizontalAlignment = Alignment.CenterHorizontally
  ) {
    Spacer(Modifier.height(18.dp))
    Text("Tu mapa", color = Color.White, fontFamily = AppFamily, fontWeight = FontWeight.Bold, fontSize = 34.sp)
    Text(
      "Tu punto de partida. Cada juego empieza ahora a tu medida y se ajusta mientras juegas.",
      color = OnNightDim, fontFamily = AppFamily, fontSize = 16.sp, lineHeight = 22.sp, textAlign = TextAlign.Center,
      modifier = Modifier.padding(top = 6.dp, bottom = 8.dp)
    )
    DomainRadar(baseline, grow.value, Modifier.fillMaxWidth().aspectRatio(1.1f))

    BaselinePlan.steps.forEach { step ->
      val r = baseline.measured[step.domain] ?: return@forEach
      val pct = Percentile.of(r, mean = referenceMean)
      Row(Modifier.fillMaxWidth().padding(vertical = 8.dp), verticalAlignment = Alignment.CenterVertically) {
        Box(Modifier.size(16.dp).clip(CircleShape).background(step.domain.color).border(2.dp, Clay.Ink, CircleShape))
        Spacer(Modifier.width(12.dp))
        Column(Modifier.weight(1f)) {
          Text(step.domain.displayName, color = Color.White, fontFamily = AppFamily, fontWeight = FontWeight.Bold, fontSize = 18.sp)
          Text(levelWord(r), color = OnNightDim, fontFamily = AppFamily, fontSize = 14.sp)
        }
        Column(horizontalAlignment = Alignment.End) {
          Text("P$pct", color = Clay.Sun, fontFamily = AppFamily, fontWeight = FontWeight.Bold, fontSize = 20.sp)
          Text("frente a personas como tú", color = OnNightDim, fontFamily = AppFamily, fontSize = 14.sp)
        }
      }
    }
    Text(
      "Razonamiento, lenguaje y cálculo se estiman hasta que los juegues. La comparación es una estimación " +
        "provisional: mejorará cuando haya más datos.",
      color = OnNightDim, fontFamily = AppFamily, fontSize = 15.sp, lineHeight = 18.sp, textAlign = TextAlign.Center,
      modifier = Modifier.padding(top = 10.dp, bottom = 22.dp)
    )
    ClayButton(text = "Empezar mi camino", onClick = onFinish, modifier = Modifier.testTag("btn_baseline_finish"))
    Spacer(Modifier.height(24.dp))
  }
}

/**
 * Radar de los 6 dominios en arcilla: anillos finos, área sol translúcida con borde sol; los vértices medidos son
 * puntos de arcilla del color del dominio y los estimados, puntos huecos con línea punteada. [grow] anima el área.
 */
@Composable
private fun DomainRadar(baseline: Baseline, grow: Float, modifier: Modifier) {
  val domains = DomainType.values().toList()
  val labelColor = OnNight
  BoxWithConstraints(modifier) {
    val pad = 34.dp
    // Las etiquetas van un poco por fuera del anillo exterior, según el tamaño real del radar.
    val labelRadius = (minOf(maxWidth, maxHeight) - pad * 2) / 2 + 20.dp
    Canvas(Modifier.fillMaxSize().padding(pad)) {
      val c = center
      val r = size.minDimension / 2f
      fun pt(i: Int, f: Float): Offset {
        val a = -PI / 2 + 2 * PI * i / domains.size
        return Offset(c.x + (r * f * cos(a)).toFloat(), c.y + (r * f * sin(a)).toFloat())
      }
      for (ring in 1..4) {
        val p = Path()
        for (i in domains.indices) {
          val q = pt(i, ring / 4f)
          if (i == 0) p.moveTo(q.x, q.y) else p.lineTo(q.x, q.y)
        }
        p.close()
        drawPath(p, Color.White.copy(alpha = 0.12f), style = Stroke(1.5.dp.toPx()))
      }
      for (i in domains.indices) drawLine(Color.White.copy(alpha = 0.10f), c, pt(i, 1f), 1.dp.toPx())

      val area = Path()
      domains.forEachIndexed { i, d ->
        val q = pt(i, ((baseline.domains[d] ?: 0f).coerceIn(0.06f, 1f)) * grow)
        if (i == 0) area.moveTo(q.x, q.y) else area.lineTo(q.x, q.y)
      }
      area.close()
      drawPath(area, Clay.Sun.copy(alpha = 0.28f))
      drawPath(area, Clay.Sun, style = Stroke(3.dp.toPx(), pathEffect = null))

      domains.forEachIndexed { i, d ->
        val q = pt(i, ((baseline.domains[d] ?: 0f).coerceIn(0.06f, 1f)) * grow)
        if (d in baseline.measured) {
          drawCircle(Clay.Ink, 9.dp.toPx(), q.copy(y = q.y + 2.dp.toPx()))
          drawCircle(Clay.Ink, 9.dp.toPx(), q)
          drawCircle(d.color, 6.5.dp.toPx(), q)
        } else {
          drawCircle(
            Color.White.copy(alpha = 0.7f), 6.dp.toPx(), q,
            style = Stroke(2.dp.toPx(), pathEffect = PathEffect.dashPathEffect(floatArrayOf(4f, 4f)))
          )
        }
      }
    }
    // Etiquetas de los dominios alrededor del radar.
    domains.forEachIndexed { i, d ->
      val a = -PI / 2 + 2 * PI * i / domains.size
      val x = cos(a).toFloat()
      val y = sin(a).toFloat()
      Text(
        d.displayName,
        color = if (d in baseline.measured) labelColor else OnNightDim,
        fontFamily = AppFamily, fontWeight = if (d in baseline.measured) FontWeight.Bold else FontWeight.Normal,
        fontSize = 15.sp,
        modifier = Modifier.align(Alignment.Center).offset(x = labelRadius * x * 1.08f, y = labelRadius * y)
      )
    }
  }
}
