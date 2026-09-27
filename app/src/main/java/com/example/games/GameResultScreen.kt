package com.example.games

import android.provider.Settings
import androidx.activity.compose.BackHandler
import androidx.compose.animation.core.Animatable
import androidx.compose.animation.core.FastOutSlowInEasing
import androidx.compose.animation.core.LinearOutSlowInEasing
import androidx.compose.animation.core.RepeatMode
import androidx.compose.animation.core.animateFloat
import androidx.compose.animation.core.infiniteRepeatable
import androidx.compose.animation.core.rememberInfiniteTransition
import androidx.compose.animation.core.spring
import androidx.compose.animation.core.tween
import androidx.compose.foundation.Canvas
import androidx.compose.foundation.layout.Arrangement
import androidx.compose.foundation.layout.Box
import androidx.compose.foundation.layout.Column
import androidx.compose.foundation.layout.Row
import androidx.compose.foundation.layout.Spacer
import androidx.compose.foundation.layout.fillMaxSize
import androidx.compose.foundation.layout.fillMaxWidth
import androidx.compose.foundation.layout.height
import androidx.compose.foundation.layout.offset
import androidx.compose.foundation.layout.padding
import androidx.compose.foundation.layout.size
import androidx.compose.foundation.layout.width
import androidx.compose.foundation.rememberScrollState
import androidx.compose.foundation.verticalScroll
import androidx.compose.material.icons.Icons
import androidx.compose.material.icons.automirrored.filled.ArrowForward
import androidx.compose.material.icons.filled.Refresh
import androidx.compose.material3.Text
import androidx.compose.runtime.Composable
import androidx.compose.runtime.LaunchedEffect
import androidx.compose.runtime.getValue
import androidx.compose.runtime.remember
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.draw.scale
import androidx.compose.ui.geometry.Offset
import androidx.compose.ui.geometry.Size
import androidx.compose.ui.graphics.Brush
import androidx.compose.ui.graphics.Color
import androidx.compose.ui.graphics.Path
import androidx.compose.ui.graphics.StrokeJoin
import androidx.compose.ui.graphics.drawscope.DrawScope
import androidx.compose.ui.graphics.drawscope.Stroke
import androidx.compose.ui.graphics.drawscope.clipRect
import androidx.compose.ui.platform.LocalContext
import androidx.compose.ui.platform.testTag
import androidx.compose.ui.semantics.contentDescription
import androidx.compose.ui.semantics.semantics
import androidx.compose.ui.text.TextStyle
import androidx.compose.ui.text.drawText
import androidx.compose.ui.text.font.FontWeight
import androidx.compose.ui.text.style.TextAlign
import androidx.compose.ui.unit.TextUnit
import androidx.compose.ui.unit.dp
import androidx.compose.ui.unit.sp
import com.example.model.GamePlayResult
import com.example.model.GameRankInfo
import com.example.model.GameRegistry
import com.example.model.LevelTier
import com.example.ui.components.LeagueShield
import com.example.ui.components.pipCount
import com.example.ui.theme.Clay
import com.example.ui.theme.ClayButton
import com.example.ui.theme.ClayPill
import com.example.ui.theme.FredokaFamily
import kotlinx.coroutines.delay
import kotlinx.coroutines.launch
import kotlin.math.PI
import kotlin.math.cos
import kotlin.math.sin
import kotlin.random.Random

private val TextSoft = Color(0xFFB4BFEA) // secundario sobre el cielo nocturno (contraste > 7:1)

/**
 * Pantalla de resultado de la app, con el sello "noche + arcilla" (rediseño 25-sep): va sobre el cielo
 * nocturno de la app (`CosmosBackground`, visible porque el fondo del tema es transparente), sin tarjetas: la
 * información es texto suelto y objetos, lo tocable es arcilla.
 *
 * Un solo protagonista animado: el puntaje cuenta hasta su valor, luego aparecen las estrellas con rebote y, si
 * te fue bien (2-3 estrellas), una lluvia de destellos. Con "quitar animaciones" del sistema todo aparece quieto.
 * Atrás = Continuar (comportamiento predecible). Sin promesas de salud en los textos.
 */
@Composable
fun GameResultScreen(
  result: GamePlayResult,
  didLevelUp: Boolean,
  isDailyFlow: Boolean,
  dailyCompletedCount: Int,
  dailyTotalCount: Int = 3,
  onPlayAgain: () -> Unit,
  onContinue: () -> Unit,
  modifier: Modifier = Modifier,
  rank: GameRankInfo? = null
) {
  val gameDef = GameRegistry.getById(result.gameId)
  val domainColor = gameDef?.domain?.color ?: Clay.Sky
  val stars = when {
    result.score >= 90 -> 3
    result.score >= 65 -> 2
    result.score >= 40 -> 1
    else -> 0
  }
  val context = LocalContext.current
  val reduceMotion = remember {
    Settings.Global.getFloat(context.contentResolver, Settings.Global.ANIMATOR_DURATION_SCALE, 1f) == 0f
  }

  BackHandler(onBack = onContinue)

  val shownScore = remember(result) { Animatable(if (reduceMotion) result.score.toFloat() else 0f) }
  val starScale = remember(result) { List(3) { Animatable(if (reduceMotion) 1f else 0f) } }
  val burst = remember(result) { Animatable(0f) }
  val levelPop = remember(result) { Animatable(if (reduceMotion) 1f else 0f) }
  LaunchedEffect(result) {
    if (reduceMotion) return@LaunchedEffect
    shownScore.animateTo(result.score.toFloat(), tween(900, easing = FastOutSlowInEasing))
    starScale.forEachIndexed { i, anim ->
      launch {
        delay(i * 170L)
        anim.animateTo(1f, spring(dampingRatio = 0.42f, stiffness = 320f))
      }
    }
    if (stars >= 2) launch { burst.animateTo(1f, tween(1500, easing = LinearOutSlowInEasing)) }
    delay(3 * 170L)
    levelPop.animateTo(1f, spring(dampingRatio = 0.5f, stiffness = 300f))
  }

  val halo = if (reduceMotion) 1f else {
    val t = rememberInfiniteTransition(label = "halo")
    val v by t.animateFloat(0.92f, 1.08f, infiniteRepeatable(tween(2200), RepeatMode.Reverse), label = "haloScale")
    v
  }

  Column(
    modifier = modifier
      .fillMaxSize()
      .verticalScroll(rememberScrollState())
      .padding(horizontal = 24.dp)
      .padding(top = 28.dp, bottom = 32.dp),
    horizontalAlignment = Alignment.CenterHorizontally
  ) {
    // Juego y dominio, como texto suelto.
    Text(
      text = gameDef?.title ?: "Partida terminada",
      color = Clay.Cream,
      fontFamily = FredokaFamily,
      fontWeight = FontWeight.Bold,
      fontSize = 26.sp,
      textAlign = TextAlign.Center
    )
    if (gameDef != null) {
      Row(verticalAlignment = Alignment.CenterVertically, modifier = Modifier.padding(top = 4.dp)) {
        Canvas(Modifier.size(8.dp)) { drawCircle(domainColor) }
        Spacer(Modifier.width(6.dp))
        Text(gameDef.domain.displayName, color = TextSoft, fontSize = 15.sp, fontWeight = FontWeight.SemiBold)
      }
    }

    // Protagonista: halo del dominio, destellos, estrellas y el puntaje gigante de arcilla.
    Box(
      modifier = Modifier
        .fillMaxWidth()
        .height(250.dp),
      contentAlignment = Alignment.Center
    ) {
      Canvas(Modifier.fillMaxSize()) {
        val c = Offset(size.width / 2f, size.height * 0.58f)
        drawCircle(
          Brush.radialGradient(listOf(domainColor.copy(alpha = 0.40f), Color.Transparent), c, size.minDimension * 0.55f * halo),
          radius = size.minDimension * 0.55f * halo,
          center = c
        )
        if (burst.value > 0f && burst.value < 1f) drawBurst(c, burst.value, size.minDimension * 0.62f)
      }
      Column(horizontalAlignment = Alignment.CenterHorizontally) {
        Row(horizontalArrangement = Arrangement.spacedBy(10.dp), verticalAlignment = Alignment.Bottom) {
          for (i in 0 until 3) {
            val big = i == 1
            ResultStar(
              earned = i < stars,
              modifier = Modifier
                .size(if (big) 58.dp else 46.dp)
                .offset(y = if (big) (-8).dp else 0.dp)
                .scale(starScale[i].value)
            )
          }
        }
        Spacer(Modifier.height(4.dp))
        ClayNumber(text = shownScore.value.toInt().toString(), color = Clay.Sun, fontSize = 104.sp)
        Text("puntos", color = TextSoft, fontSize = 15.sp, fontWeight = FontWeight.SemiBold)
      }
    }

    Text(
      text = when {
        result.score >= 90 -> "¡Extraordinario!"
        result.score >= 70 -> "¡Muy bien!"
        result.score >= 50 -> "Buen trabajo"
        else -> "Sigue practicando"
      },
      color = Clay.Cream,
      fontFamily = FredokaFamily,
      fontWeight = FontWeight.Bold,
      fontSize = 28.sp,
      textAlign = TextAlign.Center
    )
    Text(
      text = when {
        result.score >= 85 -> "Precisión y ritmo excelentes."
        result.score >= 60 -> "Buen equilibrio entre precisión y velocidad."
        else -> "La dificultad se ajusta a tu ritmo en cada partida."
      },
      color = TextSoft,
      fontSize = 16.sp,
      textAlign = TextAlign.Center,
      modifier = Modifier.padding(top = 4.dp)
    )

    // Datos de la partida, sueltos en una línea.
    Spacer(Modifier.height(18.dp))
    Row(verticalAlignment = Alignment.CenterVertically) {
      Stat("${result.correctAnswers}/${result.totalTrials}", "aciertos")
      Dot()
      Stat("Nivel ${result.level}", LevelTier.fromLevel(result.level).tierName)
      Dot()
      Stat(if (result.timed) "Reto" else "Precisión", "modo")
    }

    // Piloto Estelar: la medida propia del juego (NeuroRacer): cuánto baja la precisión al hacer dos cosas a la vez.
    result.multitaskCost?.let { cost ->
      Spacer(Modifier.height(14.dp))
      Text(
        text = "Costo de multitarea: $cost%",
        color = if (cost <= 15) Clay.Lime else Clay.Sun,
        fontWeight = FontWeight.Bold,
        fontSize = 18.sp,
        fontFamily = FredokaFamily
      )
      Text(
        text = "Cuánto baja tu precisión al pilotar y atrapar señales a la vez. Mientras más bajo, mejor.",
        color = TextSoft,
        fontSize = 13.sp,
        textAlign = TextAlign.Center,
        modifier = Modifier.padding(horizontal = 28.dp, vertical = 2.dp)
      )
    }

    // Radar: sus dos medidas propias. "Tu vistazo" (tarea UFOV: el destello más breve que se maneja) y el mapa de
    // aciertos por dirección, que ninguna otra app muestra.
    result.glanceMs?.let { ms ->
      Spacer(Modifier.height(14.dp))
      Text(
        text = "Tu vistazo: $ms ms",
        color = Clay.Sky,
        fontWeight = FontWeight.Bold,
        fontSize = 18.sp,
        fontFamily = FredokaFamily
      )
      Text(
        text = "El destello más breve con el que sigues acertando casi siempre. Mientras más bajo, más rápido captas.",
        color = TextSoft,
        fontSize = 13.sp,
        textAlign = TextAlign.Center,
        modifier = Modifier.padding(horizontal = 28.dp, vertical = 2.dp)
      )
    }
    val hits = result.sectorHits
    val trials = result.sectorTrials
    if (hits != null && trials != null && trials.sum() > 0) {
      Spacer(Modifier.height(12.dp))
      val summary = radarSummary(hits, trials)
      Text("Tu radar", color = Clay.Cream, fontWeight = FontWeight.Bold, fontSize = 16.sp, fontFamily = FredokaFamily)
      Spacer(Modifier.height(6.dp))
      RadarField(hits, trials, Modifier.size(150.dp).semantics { contentDescription = "Tu radar. $summary" })
      Text(
        text = summary,
        color = TextSoft,
        fontSize = 13.sp,
        textAlign = TextAlign.Center,
        modifier = Modifier.padding(horizontal = 28.dp, vertical = 4.dp)
      )
    }

    // Satélites: "tu seguimiento" (cuántos se siguen de verdad a la vez, sin contar la suerte) y la velocidad superada.
    result.trackingCapacity?.let { cap ->
      Spacer(Modifier.height(14.dp))
      val capText = String.format(java.util.Locale("es"), "%.1f", cap)
      Text(
        text = "Tu seguimiento: $capText a la vez",
        color = Clay.Sun,
        fontWeight = FontWeight.Bold,
        fontSize = 18.sp,
        fontFamily = FredokaFamily
      )
      Spacer(Modifier.height(6.dp))
      TrackingSlots(cap, Modifier.semantics { contentDescription = "Sigues $capText satélites a la vez" })
      result.trackingSpeed?.let { speed ->
        Text(
          text = "Velocidad más alta superada: ${String.format(java.util.Locale("es"), "%.1f", speed)}×",
          color = Clay.Cream,
          fontSize = 14.sp,
          fontWeight = FontWeight.SemiBold,
          modifier = Modifier.padding(top = 6.dp)
        )
      }
      Text(
        text = "Cuántos satélites sigues de verdad al mismo tiempo, sin contar los que aciertas por suerte. En los estudios, la mayoría de los adultos sigue alrededor de 4.",
        color = TextSoft,
        fontSize = 13.sp,
        textAlign = TextAlign.Center,
        modifier = Modifier.padding(horizontal = 28.dp, vertical = 4.dp)
      )
    }

    // Freno de Emergencia: "tu freno" (tiempo de frenado, SSRT) en un velocímetro de arcilla + cuántos altos frenó.
    if (result.stopsTotal != null) {
      Spacer(Modifier.height(14.dp))
      val brake = result.brakeMs
      if (brake != null) {
        Text(
          text = "Tu freno: $brake ms",
          color = Clay.Coral,
          fontWeight = FontWeight.Bold,
          fontSize = 18.sp,
          fontFamily = FredokaFamily
        )
        Spacer(Modifier.height(6.dp))
        BrakeGauge(brake, Modifier.semantics { contentDescription = "Tu freno: $brake milisegundos" })
      }
      val record = result.brakeBestSsdMs?.let { " · récord: frenaste con el alto a $it ms" } ?: ""
      Text(
        text = "Frenaste ${result.stopsOk ?: 0} de ${result.stopsTotal} altos$record",
        color = Clay.Cream,
        fontSize = 14.sp,
        fontWeight = FontWeight.SemiBold,
        textAlign = TextAlign.Center,
        modifier = Modifier.padding(horizontal = 28.dp, vertical = 4.dp)
      )
      Text(
        text = if (brake != null) "El tiempo que necesita tu mente para detener una acción que ya empezó. Mientras más bajo, mejor frenas."
        else "Tu freno se mide con al menos 6 altos: en una partida más larga lo verás.",
        color = TextSoft,
        fontSize = 13.sp,
        textAlign = TextAlign.Center,
        modifier = Modifier.padding(horizontal = 28.dp, vertical = 2.dp)
      )
    }

    // Aterrizaje Lunar: "tu precisión numérica" y "tu línea" (cada blanco y dónde te posaste), con la lectura de sesgo.
    result.numlineErrorPct?.let { err ->
      Spacer(Modifier.height(14.dp))
      val errText = String.format(java.util.Locale("es"), "%.1f", err)
      Text(
        text = "Tu precisión numérica: te desvías $errText%",
        color = Clay.Sky,
        fontWeight = FontWeight.Bold,
        fontSize = 18.sp,
        fontFamily = FredokaFamily,
        textAlign = TextAlign.Center,
        modifier = Modifier.padding(horizontal = 24.dp)
      )
      val trues = result.numlineTrue
      val givens = result.numlineGiven
      if (trues != null && givens != null) {
        val bias = com.example.data.NumberLine.bias(trues, givens)
        Spacer(Modifier.height(8.dp))
        Text("Tu línea", color = Clay.Cream, fontWeight = FontWeight.Bold, fontSize = 16.sp, fontFamily = FredokaFamily)
        NumberLineStrip(
          trues, givens,
          Modifier.fillMaxWidth().padding(horizontal = 28.dp).height(64.dp)
            .semantics { contentDescription = "Tu línea: ${com.example.data.NumberLine.message(bias)}" }
        )
        Text(
          text = com.example.data.NumberLine.message(bias),
          color = TextSoft,
          fontSize = 13.sp,
          textAlign = TextAlign.Center,
          modifier = Modifier.padding(horizontal = 28.dp, vertical = 2.dp)
        )
      }
      result.numlineBullseyes?.takeIf { it > 0 }?.let { n ->
        Text(
          text = if (n == 1) "1 diana lunar" else "$n dianas lunares",
          color = Clay.Sun,
          fontSize = 14.sp,
          fontWeight = FontWeight.SemiBold,
          modifier = Modifier.padding(top = 4.dp)
        )
      }
    }

    // Acoplamiento: "tu giro mental" (grados por segundo) y "tu curva de giro" (cuánto más tarda cuanto más girado).
    if (result.rotationSpeedDps != null || result.rotationCurveMs != null) {
      Spacer(Modifier.height(14.dp))
      result.rotationSpeedDps?.let { dps ->
        Text(
          text = "Tu giro mental: $dps° por segundo",
          color = Clay.Grape,
          fontWeight = FontWeight.Bold,
          fontSize = 18.sp,
          fontFamily = FredokaFamily
        )
      }
      result.rotationCurveMs?.let { curve ->
        Spacer(Modifier.height(8.dp))
        Text("Tu curva de giro", color = Clay.Cream, fontWeight = FontWeight.Bold, fontSize = 16.sp, fontFamily = FredokaFamily)
        RotationCurve(
          curve,
          Modifier.padding(horizontal = 40.dp).fillMaxWidth().height(110.dp)
            .semantics { contentDescription = "Tu curva de giro: tiempo de respuesta según cuán girada venía la pieza" }
        )
      }
      Text(
        text = if (result.rotationSpeedDps != null) "Cuanto más girada viene la pieza, más tardamos: es la huella de girarla en la mente (Shepard y Metzler, 1971). Mientras más plana tu curva, más rápido giras."
        else "Con más aciertos en distintos ángulos se mide tu giro mental.",
        color = TextSoft,
        fontSize = 13.sp,
        textAlign = TextAlign.Center,
        modifier = Modifier.padding(horizontal = 28.dp, vertical = 4.dp)
      )
    }

    if (didLevelUp) {
      Spacer(Modifier.height(18.dp))
      ClayPill(
        text = if (result.level >= 5) "¡Tu maestría sube!" else "¡Subes al nivel ${result.level + 1}!",
        color = Clay.Lime,
        modifier = Modifier.scale(levelPop.value)
      )
    }

    if (rank != null) {
      Spacer(Modifier.height(18.dp))
      Row(verticalAlignment = Alignment.CenterVertically) {
        LeagueShield(tier = rank.tier, size = 40.dp, pips = rank.pipCount())
        Spacer(Modifier.width(10.dp))
        Column {
          Text(rank.label, color = Clay.Cream, fontWeight = FontWeight.Bold, fontSize = 17.sp)
          Text("${rank.rating} trofeos en este juego", color = TextSoft, fontSize = 14.sp)
        }
      }
    }

    if (isDailyFlow) {
      Spacer(Modifier.height(20.dp))
      DailyProgress(completed = dailyCompletedCount, total = dailyTotalCount)
    }

    Spacer(Modifier.height(28.dp))
    val moreToday = isDailyFlow && dailyCompletedCount < dailyTotalCount
    ClayButton(
      text = if (moreToday) "Siguiente juego (${dailyCompletedCount + 1} de $dailyTotalCount)" else "Continuar",
      onClick = onContinue,
      icon = Icons.AutoMirrored.Filled.ArrowForward,
      modifier = Modifier.testTag("btn_result_continue")
    )
    Spacer(Modifier.height(14.dp))
    ClayButton(
      text = "Jugar de nuevo",
      onClick = onPlayAgain,
      color = Clay.Cream,
      icon = Icons.Filled.Refresh,
      modifier = Modifier.testTag("btn_result_replay")
    )
  }
}

/** Número "de arcilla": relleno de color, contorno tinta grueso y sombra dura tinta (como en los juegos). */
@Composable
private fun ClayNumber(text: String, color: Color, fontSize: TextUnit) {
  val base = TextStyle(fontFamily = FredokaFamily, fontWeight = FontWeight.Bold, fontSize = fontSize)
  val stroke = Stroke(width = 16f, join = StrokeJoin.Round)
  Box {
    Text(text, style = base.copy(color = Clay.Ink, drawStyle = stroke), modifier = Modifier.offset(y = 6.dp))
    Text(text, style = base.copy(color = Clay.Ink, drawStyle = stroke))
    Text(text, style = base.copy(color = color))
  }
}

/** Estrella de 5 puntas de arcilla: sol con borde tinta si se ganó, contorno tenue si no. */
@Composable
private fun ResultStar(earned: Boolean, modifier: Modifier = Modifier) {
  Canvas(modifier) {
    val path = starPath(size.width / 2f, size.height / 2f, size.minDimension * 0.48f, size.minDimension * 0.21f, 5)
    if (earned) {
      drawPath(path, Clay.Ink, style = Stroke(width = size.minDimension * 0.12f, join = StrokeJoin.Round))
      drawPath(path, Clay.Sun)
      // Brillo arriba a la izquierda.
      drawCircle(Color.White.copy(alpha = 0.55f), radius = size.minDimension * 0.07f, center = Offset(size.width * 0.40f, size.height * 0.36f))
    } else {
      drawPath(path, Color.White.copy(alpha = 0.10f))
      drawPath(path, Color.White.copy(alpha = 0.30f), style = Stroke(width = size.minDimension * 0.05f, join = StrokeJoin.Round))
    }
  }
}

private fun starPath(cx: Float, cy: Float, outer: Float, inner: Float, points: Int): Path = Path().apply {
  for (i in 0 until points * 2) {
    val r = if (i % 2 == 0) outer else inner
    val a = -PI / 2 + i * PI / points
    val x = cx + (r * cos(a)).toFloat()
    val y = cy + (r * sin(a)).toFloat()
    if (i == 0) moveTo(x, y) else lineTo(x, y)
  }
  close()
}

private val BurstColors = listOf(Clay.Sun, Clay.Coral, Clay.Sky, Clay.Grape, Clay.Lime, Color.White)

/** Lluvia de destellos de 4 puntas que salen del centro y se apagan (celebración de 2-3 estrellas). */
private fun DrawScope.drawBurst(center: Offset, progress: Float, reach: Float) {
  val rnd = Random(7)
  val fade = 1f - progress
  repeat(26) { i ->
    val angle = rnd.nextFloat() * 2f * PI.toFloat()
    val speed = 0.45f + rnd.nextFloat() * 0.55f
    val d = reach * speed * (1f - (1f - progress) * (1f - progress)) // sale rápido y frena
    val p = Offset(center.x + cos(angle) * d, center.y + sin(angle) * d)
    val s = (10f + rnd.nextFloat() * 16f) * (1f - progress * 0.5f)
    val color = BurstColors[i % BurstColors.size].copy(alpha = fade)
    val sparkle = Path().apply {
      moveTo(p.x, p.y - s); lineTo(p.x + s * 0.22f, p.y - s * 0.22f)
      lineTo(p.x + s, p.y); lineTo(p.x + s * 0.22f, p.y + s * 0.22f)
      lineTo(p.x, p.y + s); lineTo(p.x - s * 0.22f, p.y + s * 0.22f)
      lineTo(p.x - s, p.y); lineTo(p.x - s * 0.22f, p.y - s * 0.22f)
      close()
    }
    drawPath(sparkle, color)
  }
}

@Composable
private fun Stat(value: String, label: String) {
  Column(horizontalAlignment = Alignment.CenterHorizontally, modifier = Modifier.padding(horizontal = 6.dp)) {
    Text(value, color = Clay.Cream, fontWeight = FontWeight.Bold, fontSize = 20.sp, fontFamily = FredokaFamily)
    Text(label, color = TextSoft, fontSize = 14.sp)
  }
}

@Composable
private fun Dot() {
  Canvas(Modifier.padding(horizontal = 8.dp).size(5.dp)) { drawCircle(TextSoft.copy(alpha = 0.6f)) }
}

/** Avance de la sesión diaria como nodos de arcilla (como el camino de Hoy): hechos en coral con tilde. */
@Composable
private fun DailyProgress(completed: Int, total: Int) {
  Column(horizontalAlignment = Alignment.CenterHorizontally) {
    Text("Sesión de hoy", color = TextSoft, fontSize = 15.sp, fontWeight = FontWeight.SemiBold)
    Spacer(Modifier.height(8.dp))
    Row(verticalAlignment = Alignment.CenterVertically) {
      for (i in 0 until total) {
        if (i > 0) {
          Canvas(Modifier.width(26.dp).height(4.dp)) {
            drawLine(
              if (i <= completed) Clay.Coral else Color.White.copy(alpha = 0.18f),
              Offset(0f, size.height / 2), Offset(size.width, size.height / 2), strokeWidth = size.height
            )
          }
        }
        Canvas(Modifier.size(30.dp)) {
          val r = size.minDimension / 2f
          if (i < completed) {
            drawCircle(Clay.Ink, radius = r, center = Offset(r, r + 2.dp.toPx()))
            drawCircle(Clay.Ink, radius = r)
            drawCircle(Clay.Coral, radius = r - 3.dp.toPx())
            val check = Path().apply {
              moveTo(r * 0.55f, r * 1.02f); lineTo(r * 0.88f, r * 1.34f); lineTo(r * 1.45f, r * 0.70f)
            }
            drawPath(check, Clay.Ink, style = Stroke(width = 3.dp.toPx(), join = StrokeJoin.Round))
          } else {
            drawCircle(Color.White.copy(alpha = 0.30f), radius = r - 1.dp.toPx(), style = Stroke(width = 2.dp.toPx()))
          }
        }
      }
    }
  }
}

// ---------- Radar: "tu radar" ----------

private val RadarDirections = listOf(
  "arriba", "arriba a la derecha", "a la derecha", "abajo a la derecha",
  "abajo", "abajo a la izquierda", "a la izquierda", "arriba a la izquierda"
)

/** Dónde se rescató más y dónde menos (solo direcciones con al menos 2 destellos, para no sacar conclusiones de uno). */
private fun radarSummary(hits: List<Int>, trials: List<Int>): String {
  val rated = (0 until 8).filter { trials[it] >= 2 }.map { it to hits[it].toFloat() / trials[it] }
  if (rated.size < 2) return "Cada cuña es una dirección: mientras más larga, más astronautas rescataste ahí."
  val best = rated.maxBy { it.second }
  val worst = rated.minBy { it.second }
  if (best.second - worst.second < 0.2f) return "Parejo en todas las direcciones. Cada cuña larga = muchos rescates."
  return "Donde más rescataste: ${RadarDirections[best.first]}. Donde menos: ${RadarDirections[worst.first]}."
}

/**
 * Mapa de aciertos por dirección: un radar de arcilla con una cuña por dirección, tan larga como la proporción de
 * astronautas ubicados ahí (la longitud dice el valor; no depende del color). Direcciones sin destellos: un punto.
 */
@Composable
private fun RadarField(hits: List<Int>, trials: List<Int>, modifier: Modifier = Modifier) {
  Canvas(modifier) {
    val c = center
    val r = size.minDimension / 2f - 6.dp.toPx()
    val border = 3.dp.toPx()
    drawCircle(Clay.Ink, r + border, c + Offset(0f, 4.dp.toPx()))
    drawCircle(Color(0xFF0C1648), r, c)
    drawCircle(Clay.Ink, r, c, style = Stroke(border))
    listOf(0.33f, 0.66f).forEach { k ->
      drawCircle(Clay.Sky.copy(alpha = 0.22f), r * k, c, style = Stroke(1.dp.toPx()))
    }
    for (d in 0 until 8) {
      val angle = -90f + 45f * d
      if (trials[d] <= 0) {
        val a = Math.toRadians(angle.toDouble())
        val p = c + Offset((kotlin.math.cos(a) * r * 0.8f).toFloat(), (kotlin.math.sin(a) * r * 0.8f).toFloat())
        drawCircle(Clay.Cream.copy(alpha = 0.35f), 3.dp.toPx(), p)
        continue
      }
      val acc = hits[d].toFloat() / trials[d]
      val wr = r * (0.18f + 0.78f * acc)
      val topLeft = c - Offset(wr, wr)
      drawArc(Clay.Lime, angle - 19f, 38f, useCenter = true, topLeft = topLeft, size = Size(wr * 2f, wr * 2f))
      drawArc(Clay.Ink, angle - 19f, 38f, useCenter = true, topLeft = topLeft, size = Size(wr * 2f, wr * 2f), style = Stroke(2.dp.toPx()))
    }
    drawCircle(Clay.Cream, 4.dp.toPx(), c)
  }
}

// ---------- Satélites: "tu seguimiento" ----------

/**
 * Cinco discos de arcilla: se llenan en sol hasta la capacidad de seguimiento (3,4 = tres llenos y el cuarto al 40%).
 * La cantidad se lee por cuántos están llenos, no por el color.
 */
@Composable
private fun TrackingSlots(capacity: Float, modifier: Modifier = Modifier) {
  Canvas(modifier.size(width = 34.dp * 5 + 10.dp * 4, height = 40.dp)) {
    val r = 17.dp.toPx()
    val gap = 10.dp.toPx()
    val border = 2.5.dp.toPx()
    for (i in 0 until 5) {
      val c = Offset(r + i * (2 * r + gap), size.height / 2f - 2.dp.toPx())
      val fill = (capacity - i).coerceIn(0f, 1f)
      drawCircle(Clay.Ink, r, c + Offset(0f, 3.dp.toPx()))
      drawCircle(Color(0xFF1B2466), r, c)
      if (fill > 0f) {
        // Se llena de abajo hacia arriba.
        val top = c.y + r - 2f * r * fill
        clipRect(left = c.x - r, top = top, right = c.x + r, bottom = c.y + r) { drawCircle(Clay.Sun, r, c) }
      }
      drawCircle(Clay.Ink, r, c, style = Stroke(border))
    }
  }
}

// ---------- Freno de Emergencia: "tu freno" ----------

/**
 * Velocímetro de arcilla: medio aro de 450 ms (izquierda, freno lento) a 150 ms (derecha, freno rápido), con la aguja
 * en el tiempo de frenado. Rótulos en texto a los lados: el valor no depende del color.
 */
@Composable
private fun BrakeGauge(brakeMs: Int, modifier: Modifier = Modifier) {
  Row(modifier, verticalAlignment = Alignment.Bottom) {
    Text("lento", color = TextSoft, fontSize = 12.sp, modifier = Modifier.padding(end = 6.dp))
    Canvas(Modifier.size(width = 150.dp, height = 84.dp)) {
      val stroke = 14.dp.toPx()
      val r = size.width / 2f - stroke
      val c = Offset(size.width / 2f, size.height - 6.dp.toPx())
      val topLeft = c - Offset(r, r)
      val arc = Size(r * 2f, r * 2f)
      drawArc(Clay.Ink, 180f, 180f, useCenter = false, topLeft = topLeft + Offset(0f, 3.dp.toPx()), size = arc, style = Stroke(stroke + 6.dp.toPx()))
      drawArc(Color(0xFF1B2466), 180f, 180f, useCenter = false, topLeft = topLeft, size = arc, style = Stroke(stroke))
      val k = ((450f - brakeMs) / 300f).coerceIn(0f, 1f)
      drawArc(Clay.Coral, 180f, 180f * k, useCenter = false, topLeft = topLeft, size = arc, style = Stroke(stroke))
      val ang = Math.toRadians((180.0 + 180.0 * k))
      val tip = c + Offset((kotlin.math.cos(ang) * r * 0.95f).toFloat(), (kotlin.math.sin(ang) * r * 0.95f).toFloat())
      drawLine(Clay.Ink, c, tip, 7.dp.toPx(), androidx.compose.ui.graphics.StrokeCap.Round)
      drawLine(Clay.Cream, c, tip, 3.dp.toPx(), androidx.compose.ui.graphics.StrokeCap.Round)
      drawCircle(Clay.Ink, 8.dp.toPx(), c)
      drawCircle(Clay.Sun, 5.dp.toPx(), c)
    }
    Text("rápido", color = TextSoft, fontSize = 12.sp, modifier = Modifier.padding(start = 6.dp))
  }
}

// ---------- Aterrizaje Lunar: "tu línea" ----------

/**
 * La regla de 0 a 1 con cada aterrizaje: una marca tinta donde estaba el blanco y un punto donde se posó la nave,
 * unidos por una línea fina (lima si quedó cerca, sol si no). La distancia se ve por la posición, no por el color.
 */
@Composable
private fun NumberLineStrip(trues: List<Float>, givens: List<Float>, modifier: Modifier = Modifier) {
  Canvas(modifier) {
    val y = size.height * 0.62f
    val l = 8.dp.toPx()
    val r = size.width - 8.dp.toPx()
    val w = r - l
    drawLine(Clay.Ink, Offset(l, y + 3.dp.toPx()), Offset(r, y + 3.dp.toPx()), 8.dp.toPx(), androidx.compose.ui.graphics.StrokeCap.Round)
    drawLine(Clay.Cream, Offset(l, y), Offset(r, y), 6.dp.toPx(), androidx.compose.ui.graphics.StrokeCap.Round)
    for (x in listOf(l, r)) drawLine(Clay.Ink, Offset(x, y - 10.dp.toPx()), Offset(x, y + 10.dp.toPx()), 3.dp.toPx())
    val n = minOf(trues.size, givens.size)
    for (i in 0 until n) {
      val tx = l + w * trues[i].coerceIn(0f, 1f)
      val gx = l + w * givens[i].coerceIn(0f, 1f)
      // Cada intento a una altura un poco distinta, para que no se tapen.
      val gy = y - (14f + (i % 4) * 7f) * density
      val near = kotlin.math.abs(trues[i] - givens[i]) <= 0.05f
      val col = if (near) Clay.Lime else Clay.Sun
      drawLine(col.copy(alpha = 0.7f), Offset(tx, y - 4.dp.toPx()), Offset(gx, gy), 1.5.dp.toPx())
      drawLine(Clay.Ink, Offset(tx, y - 5.dp.toPx()), Offset(tx, y + 5.dp.toPx()), 2.dp.toPx())
      drawCircle(Clay.Ink, 4.5.dp.toPx(), Offset(gx, gy))
      drawCircle(col, 3.dp.toPx(), Offset(gx, gy))
    }
  }
}

// ---------- Acoplamiento: "tu curva de giro" ----------

/**
 * Cinco columnas de arcilla (0°, 45°, 90°, 135°, 180°): la altura es el tiempo medio de respuesta a ese ángulo. Rótulo
 * del ángulo debajo y del tiempo (en segundos) encima: los valores se leen en texto, no solo por la altura.
 */
@Composable
private fun RotationCurve(curve: List<Int?>, modifier: Modifier = Modifier) {
  val measurer = androidx.compose.ui.text.rememberTextMeasurer()
  val labels = listOf("0°", "45°", "90°", "135°", "180°")
  Canvas(modifier) {
    val maxMs = (curve.filterNotNull().maxOrNull() ?: 1).coerceAtLeast(1)
    val slot = size.width / 5f
    val barW = slot * 0.46f
    val labelH = 18.dp.toPx()
    val valueH = 16.dp.toPx()
    val chartH = size.height - labelH - valueH
    val small = TextStyle(color = TextSoft, fontSize = 11.sp)
    for (i in 0 until 5) {
      val cx = slot * (i + 0.5f)
      val lab = measurer.measure(labels[i], small)
      drawText(lab, topLeft = Offset(cx - lab.size.width / 2f, size.height - lab.size.height.toFloat()))
      val ms = curve.getOrNull(i) ?: continue
      val h = (chartH * ms / maxMs).coerceAtLeast(4.dp.toPx())
      val top = valueH + chartH - h
      drawRoundRect(Clay.Ink, Offset(cx - barW / 2f, top + 3.dp.toPx()), androidx.compose.ui.geometry.Size(barW, h),
        androidx.compose.ui.geometry.CornerRadius(6.dp.toPx()))
      drawRoundRect(Clay.Grape, Offset(cx - barW / 2f, top), androidx.compose.ui.geometry.Size(barW, h),
        androidx.compose.ui.geometry.CornerRadius(6.dp.toPx()))
      val v = measurer.measure(String.format(java.util.Locale("es"), "%.1f s", ms / 1000f), small.copy(color = Clay.Cream))
      drawText(v, topLeft = Offset(cx - v.size.width / 2f, top - v.size.height - 2.dp.toPx()))
    }
  }
}
