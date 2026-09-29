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
import com.example.ui.theme.AppFamily
import kotlinx.coroutines.delay
import kotlinx.coroutines.launch
import kotlin.math.PI
import kotlin.math.cos
import kotlin.math.roundToInt
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
  rank: GameRankInfo? = null,
  /** Qué pasó con el modo elegido (data/Skill.kt): "¡Desafío superado!…", o null a tu medida. */
  modeNote: String? = null
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
      fontFamily = AppFamily,
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
      fontFamily = AppFamily,
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
      Stat(LevelTier.fromLevel(result.level).tierName, "etapa")
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
        fontFamily = AppFamily
      )
      Text(
        text = "Cuánto bajó tu puntería con las señales al pasar de solo mirarlas (piloto automático) a pilotar y mirarlas a la vez. Mientras más bajo, mejor repartes la atención. Con práctica suele bajar.",
        color = TextSoft,
        fontSize = 15.sp,
        textAlign = TextAlign.Center,
        modifier = Modifier.padding(horizontal = 28.dp, vertical = 2.dp)
      )
    }

    // Radar (Rescate relámpago): tu vistazo, tu captura, tu filtro y tu radar. Ver docs/medidas-juegos-estrella.md.
    result.glanceMs?.let { ms ->
      Spacer(Modifier.height(14.dp))
      Text(
        text = "Tu vistazo: $ms ms",
        color = Clay.Sky,
        fontWeight = FontWeight.Bold,
        fontSize = 18.sp,
        fontFamily = AppFamily
      )
      val load = result.glanceLoad?.let { l ->
        val n = String.format(java.util.Locale("es"), if (l % 1f == 0f) "%.0f" else "%.1f", l)
        "con $n astronautas a la vez, "
      } ?: ""
      Text(
        text = "El destello más breve con el que, ${load}los rescatas casi todos unas 4 de cada 5 veces. Mientras menos milisegundos, más rápido captas.",
        color = TextSoft,
        fontSize = 15.sp,
        textAlign = TextAlign.Center,
        modifier = Modifier.padding(horizontal = 28.dp, vertical = 2.dp)
      )
    }
    result.captureK?.let { k ->
      Spacer(Modifier.height(12.dp))
      val kText = String.format(java.util.Locale("es"), "%.1f", k)
      Text(
        text = "Tu captura: $kText de un vistazo",
        color = Clay.Sun,
        fontWeight = FontWeight.Bold,
        fontSize = 18.sp,
        fontFamily = AppFamily
      )
      Spacer(Modifier.height(6.dp))
      TrackingSlots(k, slots = 6, modifier = Modifier.semantics { contentDescription = "Captas $kText astronautas de un vistazo" })
      Text(
        text = "Cuántos astronautas captas cuando el destello no apura (en las lluvias de astronautas), sin contar las balizas puestas al azar. Los adultos suelen captar entre 3 y 4 cosas de un vistazo.",
        color = TextSoft,
        fontSize = 15.sp,
        textAlign = TextAlign.Center,
        modifier = Modifier.padding(horizontal = 28.dp, vertical = 4.dp)
      )
    }
    val robotsShown = result.robotsShown
    if (robotsShown != null && robotsShown >= 6) {
      val touched = result.robotsTouched ?: 0
      Spacer(Modifier.height(12.dp))
      Text(
        text = "Tu filtro: tocaste $touched de $robotsShown robots",
        color = Clay.Cream,
        fontWeight = FontWeight.Bold,
        fontSize = 16.sp,
        fontFamily = AppFamily
      )
      Text(
        text = if (touched * 5 <= robotsShown) "Ignoras bien lo que no hay que rescatar, aunque se parezca."
        else "A veces un robot se cuela como astronauta: fíjate en la forma (el robot es cuadrado, el casco es redondo).",
        color = TextSoft,
        fontSize = 15.sp,
        textAlign = TextAlign.Center,
        modifier = Modifier.padding(horizontal = 28.dp, vertical = 2.dp)
      )
    }
    val hits = result.sectorHits
    val trials = result.sectorTrials
    if (hits != null && trials != null && trials.sum() > 0) {
      Spacer(Modifier.height(12.dp))
      val summary = radarSummary(hits, trials) + ringSummary(result.ringHits, result.ringTrials)
      Text("Tu radar", color = Clay.Cream, fontWeight = FontWeight.Bold, fontSize = 16.sp, fontFamily = AppFamily)
      Spacer(Modifier.height(6.dp))
      RadarField(hits, trials, Modifier.size(150.dp).semantics { contentDescription = "Tu radar. $summary" })
      Text(
        text = summary,
        color = TextSoft,
        fontSize = 15.sp,
        textAlign = TextAlign.Center,
        modifier = Modifier.padding(horizontal = 28.dp, vertical = 4.dp)
      )
    }

    // Satélites: "tu seguimiento" (cuántos se siguen de verdad a la vez, sin contar la suerte) y la velocidad superada.
    result.trackingCapacity?.let { cap ->
      Spacer(Modifier.height(14.dp))
      val capText = String.format(java.util.Locale("es"), "%.1f", cap)
      val targets = result.trackingTargets
      val ofText = targets?.let { " de " + String.format(java.util.Locale("es"), if (it % 1f == 0f) "%.0f" else "%.1f", it) } ?: ""
      Text(
        text = "Tu seguimiento: $capText$ofText a la vez",
        color = Clay.Sun,
        fontWeight = FontWeight.Bold,
        fontSize = 18.sp,
        fontFamily = AppFamily
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
        text = if (targets != null && targets < 3.5f)
          "Cuántos seguiste de verdad de los que había que seguir, sin contar los que aciertas por suerte. El juego suma satélites y velocidad a medida que aciertas: así se ve hasta dónde llegas."
        else
          "Cuántos seguiste de verdad al mismo tiempo, sin contar los que aciertas por suerte. A velocidad moderada, los adultos suelen seguir entre 3 y 4; más rápido, menos (Alvarez y Franconeri, 2007).",
        color = TextSoft,
        fontSize = 15.sp,
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
          fontFamily = AppFamily
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
        text = if (brake != null) "Estimación de cuánto tardas en frenar una acción que ya ibas a hacer. Mientras más bajo, más rápido frenas. Con pocos altos por partida varía bastante: mira cómo va en varias."
        else "Esta vez no se pudo estimar tu freno: hacen falta al menos 6 altos y haber frenado entre 1 de cada 4 y 3 de cada 4. Lanza apenas se encienda la luz, sin esperar al ALTO: así la medida funciona.",
        color = TextSoft,
        fontSize = 15.sp,
        textAlign = TextAlign.Center,
        modifier = Modifier.padding(horizontal = 28.dp, vertical = 2.dp)
      )
    }

    // Aterrizaje Lunar: "tu estimación" y "tu línea" (cada blanco y dónde te posaste), con el tramo donde más se aleja.
    result.numlineErrorPct?.let { err ->
      Spacer(Modifier.height(14.dp))
      val errText = String.format(java.util.Locale("es"), "%.1f", err)
      Text(
        text = "Tu estimación: a $errText% del blanco",
        color = Clay.Sky,
        fontWeight = FontWeight.Bold,
        fontSize = 18.sp,
        fontFamily = AppFamily,
        textAlign = TextAlign.Center,
        modifier = Modifier.padding(horizontal = 24.dp)
      )
      Text(
        text = "En promedio, qué tan lejos del blanco te posaste (en % del largo de la regla). Ubicar un número en una regla junta dos cosas: saber cuánto vale y calcular a ojo qué parte de la regla le toca.",
        color = TextSoft,
        fontSize = 15.sp,
        textAlign = TextAlign.Center,
        modifier = Modifier.padding(horizontal = 28.dp, vertical = 2.dp)
      )
      val trues = result.numlineTrue
      val givens = result.numlineGiven
      if (trues != null && givens != null) {
        val bias = com.example.data.NumberLine.reading(trues, givens)
        Spacer(Modifier.height(8.dp))
        Text("Tu línea", color = Clay.Cream, fontWeight = FontWeight.Bold, fontSize = 16.sp, fontFamily = AppFamily)
        NumberLineStrip(
          trues, givens,
          Modifier.fillMaxWidth().padding(horizontal = 28.dp).height(64.dp)
            .semantics { contentDescription = "Tu línea: ${com.example.data.NumberLine.message(bias)}" }
        )
        Text(
          text = com.example.data.NumberLine.message(bias),
          color = TextSoft,
          fontSize = 15.sp,
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
          fontFamily = AppFamily
        )
      }
      result.rotationCurveMs?.let { curve ->
        Spacer(Modifier.height(8.dp))
        Text("Tu curva de giro", color = Clay.Cream, fontWeight = FontWeight.Bold, fontSize = 16.sp, fontFamily = AppFamily)
        RotationCurve(
          curve,
          Modifier.padding(horizontal = 40.dp).fillMaxWidth().height(110.dp)
            .semantics { contentDescription = "Tu curva de giro: tiempo de respuesta según cuán girada venía la pieza" }
        )
      }
      Text(
        text = if (result.rotationSpeedDps != null) "Cuanto más girada viene la pieza, más tardamos: es la huella de girarla en la mente (Cooper y Shepard, 1973). Tu giro sale de cuánto sube tu tiempo por cada grado, solo con tus aciertos. En Precisión, sin apuro de combustible, la medida es más fiel."
        else "Tu giro mental se calcula con al menos 8 aciertos en 3 ángulos distintos y 7 de cada 10 respuestas bien: con más partidas lo verás.",
        color = TextSoft,
        fontSize = 15.sp,
        textAlign = TextAlign.Center,
        modifier = Modifier.padding(horizontal = 28.dp, vertical = 4.dp)
      )
    }

    // Tráfico Estelar: "tu carga" (cuántas cápsulas coordinaste a la vez sin errores) y "tu anticipación" (control
    // proactivo vs reactivo, Braver 2012).
    if (result.trafficLeadMs != null || result.trafficPeakPods != null) {
      Spacer(Modifier.height(14.dp))
      result.trafficPeakPods?.let { n ->
        Text(
          text = if (n == 1) "Tu carga: 1 cápsula a la vez" else "Tu carga: $n cápsulas a la vez",
          color = Clay.Sun,
          fontWeight = FontWeight.Bold,
          fontSize = 20.sp,
          fontFamily = AppFamily
        )
        Spacer(Modifier.height(6.dp))
        LoadSlots(n, Modifier.semantics { contentDescription = "Coordinaste $n cápsulas a la vez sin errores" })
        Text(
          text = "Las que tuviste en viaje al mismo tiempo sin ningún error entre ellas. Sube a medida que el juego te da más tráfico.",
          color = TextSoft,
          fontSize = 15.sp,
          textAlign = TextAlign.Center,
          modifier = Modifier.padding(horizontal = 28.dp, vertical = 4.dp)
        )
        Spacer(Modifier.height(8.dp))
      }
      result.trafficLeadMs?.let { ms ->
        Text(
          text = "Tu anticipación: " + String.format(java.util.Locale("es"), "%.1f s", ms / 1000f),
          color = Clay.Sky,
          fontWeight = FontWeight.Bold,
          fontSize = 18.sp,
          fontFamily = AppFamily
        )
        Text(
          text = "Cuánto antes de que pase la cápsula dejas listo su desvío (valor típico de la partida).",
          color = TextSoft,
          fontSize = 15.sp,
          textAlign = TextAlign.Center,
          modifier = Modifier.padding(horizontal = 28.dp, vertical = 2.dp)
        )
      }
      result.trafficProactivePct?.let { pct ->
        Spacer(Modifier.height(8.dp))
        PlanReactBar(
          pct,
          Modifier.padding(horizontal = 36.dp).fillMaxWidth().height(26.dp)
            .semantics { contentDescription = "Planificas $pct por ciento, a último momento ${100 - pct} por ciento" }
        )
        Row(Modifier.padding(horizontal = 36.dp).fillMaxWidth(), horizontalArrangement = Arrangement.SpaceBetween) {
          Text("Planificas $pct%", color = Clay.Lime, fontSize = 15.sp, fontWeight = FontWeight.SemiBold)
          Text("A último momento ${100 - pct}%", color = Clay.Sun, fontSize = 15.sp, fontWeight = FontWeight.SemiBold)
        }
        Text(
          text = if (pct >= 50) "Te anticipas: eso deja holgura cuando el tráfico aumenta."
          else "Reaccionas a tiempo, pero justo. Prueba mirar las próximas y preparar la ruta antes de que salgan.",
          color = TextSoft,
          fontSize = 15.sp,
          textAlign = TextAlign.Center,
          modifier = Modifier.padding(horizontal = 28.dp, vertical = 4.dp)
        )
      }
    }

    // Bitácora de Misión: memoria con demora (qué-dónde y orden), retención de lo aprendido y la colección.
    if (result.memPhase != null) {
      Spacer(Modifier.height(14.dp))
      val items = result.memItems ?: 0
      if (result.memPhase == "encode") {
        val learned = result.memLearned ?: 0
        Text("Transmisión guardada", color = Clay.Sky, fontWeight = FontWeight.Bold, fontSize = 20.sp, fontFamily = AppFamily)
        Spacer(Modifier.height(6.dp))
        FilledSlots(learned, items, Clay.Sky, Modifier.semantics { contentDescription = "Aprendiste $learned de $items" })
        Text(
          text = "Aprendiste $learned de $items en el primer repaso. El informe se abre al terminar tu sesión de hoy (o en 10 minutos). No hace falta repasar: la idea es ver cuánto guarda tu memoria por sí sola.",
          color = TextSoft,
          fontSize = 15.sp,
          textAlign = TextAlign.Center,
          modifier = Modifier.padding(horizontal = 28.dp, vertical = 4.dp)
        )
      } else {
        val recalled = result.memRecalled ?: 0
        val delay = result.memDelayS?.let { com.example.data.MissionLog.delayLabel(it) } ?: ""
        Text(
          text = "Tu memoria $delay: $recalled de $items",
          color = Clay.Sun,
          fontWeight = FontWeight.Bold,
          fontSize = 20.sp,
          fontFamily = AppFamily,
          textAlign = TextAlign.Center,
          modifier = Modifier.padding(horizontal = 24.dp)
        )
        Spacer(Modifier.height(6.dp))
        FilledSlots(recalled, items, Clay.Sun, Modifier.semantics { contentDescription = "Recordaste $recalled de $items en su lugar" })
        Text(
          text = "Hallazgos que recordaste en su planeta, sin ayuda.",
          color = TextSoft,
          fontSize = 15.sp,
          textAlign = TextAlign.Center,
          modifier = Modifier.padding(horizontal = 28.dp, vertical = 2.dp)
        )
        result.memRetentionPct?.let { pct ->
          Text(
            text = "Guardaste el $pct% de lo que aprendiste",
            color = Clay.Lime,
            fontSize = 15.sp,
            fontWeight = FontWeight.SemiBold,
            modifier = Modifier.padding(top = 8.dp)
          )
          Text(
            text = "De lo que acertaste en el primer repaso, cuánto seguía ahí en el informe. Separa aprender de retener.",
            color = TextSoft,
            fontSize = 15.sp,
            textAlign = TextAlign.Center,
            modifier = Modifier.padding(horizontal = 28.dp, vertical = 2.dp)
          )
        }
        result.memOrderOk?.let { ok ->
          Text(
            text = "La ruta: $ok de $items en orden",
            color = Clay.Cream,
            fontSize = 14.sp,
            fontWeight = FontWeight.SemiBold,
            modifier = Modifier.padding(top = 8.dp)
          )
        }
        result.memIntrusions?.takeIf { it > 0 }?.let { n ->
          Text(
            text = (if (n == 1) "Elegiste 1 hallazgo que no estaba en la misión" else "Elegiste $n hallazgos que no estaban en la misión") +
              ": la memoria a veces completa huecos con lo que parece probable. Es normal.",
            color = TextSoft,
            fontSize = 15.sp,
            textAlign = TextAlign.Center,
            modifier = Modifier.padding(horizontal = 28.dp, vertical = 4.dp)
          )
        }
        Text(
          text = "Truco para la próxima: imagina cada hallazgo haciendo algo en su planeta. Una escena se recuerda mejor que un dato suelto.",
          color = TextSoft,
          fontSize = 15.sp,
          textAlign = TextAlign.Center,
          modifier = Modifier.padding(horizontal = 28.dp, vertical = 4.dp)
        )
      }
      result.memArchivedTotal?.takeIf { it > 0 }?.let { total ->
        Text(
          text = if (total == 1) "Tu bitácora: 1 hallazgo archivado" else "Tu bitácora: $total hallazgos archivados",
          color = Clay.Sun,
          fontSize = 14.sp,
          fontWeight = FontWeight.SemiBold,
          modifier = Modifier.padding(top = 6.dp)
        )
      }
    }

    // Rumbo a Casa: "tu brújula interna" (a qué distancia de casa quedaste) y "tus llegadas" (cada vuelta alrededor de
    // la base), separando rumbo y distancia; con faro / sin faro si hubo viajes suficientes de cada tipo.
    result.homingErrorPct?.let { err ->
      Spacer(Modifier.height(14.dp))
      Text(
        text = "Tu brújula interna: a ${err.roundToInt()}% de casa",
        color = Clay.Lime,
        fontWeight = FontWeight.Bold,
        fontSize = 18.sp,
        fontFamily = AppFamily,
        textAlign = TextAlign.Center,
        modifier = Modifier.padding(horizontal = 24.dp)
      )
      Text(
        text = "En promedio, a qué distancia de tu base quedaste, en % de lo que había que volver. Volver sin mapa usa lo que registras al moverte: cuánto giras y cuánto avanzas.",
        color = TextSoft,
        fontSize = 15.sp,
        textAlign = TextAlign.Center,
        modifier = Modifier.padding(horizontal = 28.dp, vertical = 2.dp)
      )
      val along = result.homingAlong
      val lateral = result.homingLateral
      val beacon = result.homingBeacon
      if (along != null && lateral != null && beacon != null) {
        val trips = com.example.data.Homing.trips(along, lateral, beacon)
        val angle = com.example.data.Homing.meanAbsAngle(trips)
        val distance = com.example.data.Homing.distanceMessage(com.example.data.Homing.distance(trips))
        Spacer(Modifier.height(8.dp))
        Text("Tus llegadas", color = Clay.Cream, fontWeight = FontWeight.Bold, fontSize = 16.sp, fontFamily = AppFamily)
        HomingTarget(
          trips,
          Modifier.fillMaxWidth().padding(horizontal = 56.dp).height(230.dp)
            .semantics {
              contentDescription = "Tus llegadas: ${trips.size} vueltas alrededor de tu base." +
                (angle?.let { " Te desviaste ${it.roundToInt()} grados en promedio." } ?: "") + (distance?.let { " $it" } ?: "")
            }
        )
        Text(
          text = "Cada punto es dónde quedaste. Encima de tu base es pasarte, debajo es quedarte corto y a los lados es desviar el rumbo.",
          color = TextSoft,
          fontSize = 15.sp,
          textAlign = TextAlign.Center,
          modifier = Modifier.padding(horizontal = 28.dp, vertical = 2.dp)
        )
        angle?.let { a ->
          Text(
            text = "Rumbo: te desviaste ${a.roundToInt()}° en promedio",
            color = Clay.Cream,
            fontSize = 14.sp,
            fontWeight = FontWeight.SemiBold,
            textAlign = TextAlign.Center,
            modifier = Modifier.padding(horizontal = 28.dp, vertical = 2.dp)
          )
        }
        distance?.let {
          Text(
            text = it,
            color = Clay.Cream,
            fontSize = 14.sp,
            fontWeight = FontWeight.SemiBold,
            textAlign = TextAlign.Center,
            modifier = Modifier.padding(horizontal = 28.dp, vertical = 2.dp)
          )
        }
        com.example.data.Homing.sourceMessage(com.example.data.Homing.source(trips))?.let {
          Text(it, color = TextSoft, fontSize = 15.sp, textAlign = TextAlign.Center, modifier = Modifier.padding(horizontal = 28.dp, vertical = 2.dp))
        }
        com.example.data.Homing.beaconAngles(trips)?.let { (withDeg, withoutDeg) ->
          Text(
            text = com.example.data.Homing.beaconMessage(withDeg, withoutDeg),
            color = TextSoft,
            fontSize = 15.sp,
            textAlign = TextAlign.Center,
            modifier = Modifier.padding(horizontal = 28.dp, vertical = 2.dp)
          )
        }
      }
      result.homingPerfect?.takeIf { it > 0 }?.let { n ->
        Text(
          text = if (n == 1) "1 llegada perfecta" else "$n llegadas perfectas",
          color = Clay.Sun,
          fontSize = 14.sp,
          fontWeight = FontWeight.SemiBold,
          modifier = Modifier.padding(top = 4.dp)
        )
      }
    }

    // Correo Estelar: "tu memoria para lo pendiente", por lugar (planetas) y por hora (radio), y cómo se usó el reloj.
    result.mailEventTotal?.let { evTotal ->
      Spacer(Modifier.height(14.dp))
      val evHits = result.mailEventHits ?: 0
      val raTotal = result.mailRadioTotal ?: 0
      val raHits = result.mailRadioHits ?: 0
      Text(
        text = "Tu memoria para lo pendiente: ${evHits + raHits} de ${evTotal + raTotal} encargos",
        color = Clay.Sun,
        fontWeight = FontWeight.Bold,
        fontSize = 18.sp,
        fontFamily = AppFamily,
        textAlign = TextAlign.Center,
        modifier = Modifier.padding(horizontal = 24.dp)
      )
      Text(
        text = "Acordarte de hacer algo en el momento justo, sin que nada te avise del todo: como tomar un remedio o hacer una llamada.",
        color = TextSoft,
        fontSize = 15.sp,
        textAlign = TextAlign.Center,
        modifier = Modifier.padding(horizontal = 28.dp, vertical = 2.dp)
      )
      Spacer(Modifier.height(6.dp))
      Text("Por lugar (planetas): $evHits de $evTotal", color = Clay.Cream, fontSize = 15.sp, fontWeight = FontWeight.SemiBold)
      FilledSlots(evHits, evTotal, Clay.Coral, Modifier.padding(top = 4.dp).semantics { contentDescription = "Por lugar: $evHits de $evTotal" })
      if (raTotal > 0) {
        Text(
          "Por hora (radio): $raHits de $raTotal",
          color = Clay.Cream,
          fontSize = 15.sp,
          fontWeight = FontWeight.SemiBold,
          modifier = Modifier.padding(top = 8.dp)
        )
        FilledSlots(raHits, raTotal, Clay.Grape, Modifier.padding(top = 4.dp).semantics { contentDescription = "Por hora: $raHits de $raTotal" })
      }
      val notes = listOfNotNull(
        com.example.data.Mail.commissionMessage(result.mailCommissions ?: 0, result.mailLureCommissions ?: 0),
        com.example.data.Mail.clockMessage(result.mailClockChecks ?: -1, result.mailClockLate ?: 0, raTotal),
        com.example.data.Mail.compareMessage(evHits, evTotal, raHits, raTotal)
      )
      notes.forEach {
        Text(it, color = TextSoft, fontSize = 15.sp, textAlign = TextAlign.Center, modifier = Modifier.padding(horizontal = 28.dp, vertical = 3.dp))
      }
      val lane = result.mailLanePct
      val asteroids = result.mailAsteroids
      val dodged = asteroids?.let { it - (result.mailAsteroidHits ?: 0) }
      if (lane != null && lane >= 0) {
        Text(
          text = "Ruta: $lane% del vuelo" + (if (asteroids != null && asteroids > 0) " · esquivaste $dodged de $asteroids asteroides" else ""),
          color = TextSoft,
          fontSize = 15.sp,
          modifier = Modifier.padding(top = 4.dp)
        )
      }
      com.example.data.Mail.shipMessage(result.mailHullIntactPct ?: -1, result.mailEmergencies ?: 0)?.let {
        Text(it, color = TextSoft, fontSize = 15.sp, textAlign = TextAlign.Center, modifier = Modifier.padding(horizontal = 28.dp, vertical = 3.dp))
      }
    }

    // Nota común a las medidas propias de los juegos estrella: son de esta partida, no un diagnóstico.
    val hasStarMeasure = listOf(
      result.multitaskCost, result.glanceMs, result.captureK, result.trackingCapacity, result.stopsTotal, result.numlineErrorPct,
      result.rotationSpeedDps, result.rotationCurveMs, result.trafficLeadMs, result.trafficPeakPods, result.memRecalled,
      result.homingErrorPct, result.mailEventTotal
    ).any { it != null }
    if (hasStarMeasure) {
      Text(
        text = "Medida de esta partida: cambia de un día a otro. Lo que vale es cómo evoluciona, no un resultado suelto. No es un diagnóstico.",
        color = TextSoft.copy(alpha = 0.8f),
        fontSize = 14.sp,
        textAlign = TextAlign.Center,
        modifier = Modifier.padding(horizontal = 32.dp, vertical = 10.dp)
      )
    }

    if (modeNote != null) {
      Spacer(Modifier.height(14.dp))
      ClayPill(text = modeNote, color = if (modeNote.startsWith("¡")) Clay.Lime else Clay.Cream, modifier = Modifier.testTag("mode_note"))
    }

    if (didLevelUp) {
      Spacer(Modifier.height(18.dp))
      ClayPill(
        text = if (result.level >= 5) "¡Tu maestría sube!" else "¡Subiste de etapa!",
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
    // La transmisión y el informe de la misión del día no se repiten (serían otra misión): sin "Jugar de nuevo".
    if (result.memPhase.isNullOrEmpty()) {
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
}

/** Número "de arcilla": relleno de color, contorno tinta grueso y sombra dura tinta (como en los juegos). */
@Composable
private fun ClayNumber(text: String, color: Color, fontSize: TextUnit) {
  val base = TextStyle(fontFamily = AppFamily, fontWeight = FontWeight.Bold, fontSize = fontSize)
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
    Text(value, color = Clay.Cream, fontWeight = FontWeight.Bold, fontSize = 20.sp, fontFamily = AppFamily)
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
  // Con pocos astronautas por dirección las diferencias suelen ser azar: solo se nombra una dirección con 4 o más
  // en cada una y una diferencia grande (40 puntos).
  val rated = (0 until 8).filter { trials[it] >= 4 }.map { it to hits[it].toFloat() / trials[it] }
  if (rated.size < 4) return "Cada cuña es una dirección: mientras más larga, más astronautas rescataste ahí. Con más partidas se ve si alguna dirección te cuesta más."
  val best = rated.maxBy { it.second }
  val worst = rated.minBy { it.second }
  if (best.second - worst.second < 0.4f) return "Parejo en todas las direcciones. Cada cuña larga = muchos rescates."
  return "En esta partida rescataste más ${RadarDirections[best.first]} y menos ${RadarDirections[worst.first]}. Si se repite en otras partidas, vale la pena mirar más hacia ese lado."
}

/**
 * Cerca / lejos del centro: se nombra solo con 6 o más astronautas en cada anillo y 25 puntos de diferencia (el campo
 * visual útil se achica hacia la periferia cuando la tarea apura).
 */
private fun ringSummary(hits: List<Int>?, trials: List<Int>?): String {
  if (hits == null || trials == null || trials.size != 2 || trials.any { it < 6 }) return ""
  val near = hits[0].toFloat() / trials[0]
  val far = hits[1].toFloat() / trials[1]
  return when {
    near - far >= 0.25f -> " Te cuestan más los de lejos del centro: al esperar el destello, mira el centro pero abarca todo el radar."
    far - near >= 0.25f -> " Te cuestan más los de cerca del centro: no te vayas solo al borde."
    else -> " Cerca y lejos del centro, parecido."
  }
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
 * Discos de arcilla (5; 6 en la captura de Radar): se llenan en sol hasta la capacidad (3,4 = tres llenos y el cuarto al 40%).
 * La cantidad se lee por cuántos están llenos, no por el color.
 */
@Composable
private fun TrackingSlots(capacity: Float, modifier: Modifier = Modifier, slots: Int = 5) {
  Canvas(modifier.size(width = 34.dp * slots + 10.dp * (slots - 1), height = 40.dp)) {
    val r = 17.dp.toPx()
    val gap = 10.dp.toPx()
    val border = 2.5.dp.toPx()
    for (i in 0 until slots) {
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

// ---------- Bitácora de Misión ----------

/** Fila de discos de arcilla: [filled] encendidos (del color dado) de [total]. El número va en el texto de arriba. */
@Composable
private fun FilledSlots(filled: Int, total: Int, color: Color, modifier: Modifier = Modifier) {
  val n = total.coerceIn(1, 8)
  Canvas(modifier.size(width = 26.dp * n + 8.dp * (n - 1), height = 32.dp)) {
    val r = 13.dp.toPx()
    val gap = 8.dp.toPx()
    val border = 2.5.dp.toPx()
    for (i in 0 until n) {
      val c = Offset(r + i * (2 * r + gap), size.height / 2f - 2.dp.toPx())
      drawCircle(Clay.Ink, r, c + Offset(0f, 3.dp.toPx()))
      drawCircle(if (i < filled) color else Color(0xFF1B2466), r, c)
      drawCircle(Clay.Ink, r, c, style = Stroke(border))
    }
  }
}

// ---------- Tráfico Estelar: "tu carga" ----------

/** Colores de las cápsulas del juego (mismo orden que TrafficSprites.Colors en Unity). */
private val PodColors = listOf(
  Color(0xFFFF6B4A), Color(0xFFFFC93C), Color(0xFF4CC9F0), Color(0xFF9BE564),
  Color(0xFFB8A4FF), Color(0xFFFF7BC0), Color(0xFF5FD68A), Color(0xFFFF8A3D)
)

/**
 * Ocho cápsulas de arcilla en fila: se encienden (cada una con su color) tantas como las que coordinaste a la vez; las
 * demás quedan apagadas. El número va en el texto de arriba: no depende del color.
 */
@Composable
private fun LoadSlots(load: Int, modifier: Modifier = Modifier) {
  Canvas(modifier.size(width = 26.dp * 8 + 8.dp * 7, height = 32.dp)) {
    val r = 13.dp.toPx()
    val gap = 8.dp.toPx()
    val border = 2.5.dp.toPx()
    for (i in 0 until 8) {
      val c = Offset(r + i * (2 * r + gap), size.height / 2f - 2.dp.toPx())
      drawCircle(Clay.Ink, r, c + Offset(0f, 3.dp.toPx()))
      drawCircle(if (i < load) PodColors[i] else Color(0xFF1B2466), r, c)
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
    Text("lento", color = TextSoft, fontSize = 14.sp, modifier = Modifier.padding(end = 6.dp))
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
    Text("rápido", color = TextSoft, fontSize = 14.sp, modifier = Modifier.padding(start = 6.dp))
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

// ---------- Rumbo a Casa: "tus llegadas" ----------

/**
 * Diana alrededor de tu base: la base arriba al centro y, abajo, el punto donde empezó la vuelta (la vuelta justa es la
 * línea crema entre los dos). Cada vuelta es un punto donde quedó la nave, en fracciones de la distancia que había:
 * encima de la base = te pasaste, debajo = te quedaste corto, a los costados = desviaste el rumbo. Dos anillos finos:
 * "llegada perfecta" (12%) y "llegaste a casa" (35%). Lo que se lee es la posición (con texto aparte), no el color.
 */
@Composable
private fun HomingTarget(trips: List<com.example.data.HomingTrip>, modifier: Modifier = Modifier) {
  val measurer = androidx.compose.ui.text.rememberTextMeasurer()
  Canvas(modifier) {
    val homeY = size.height * 0.34f
    val startY = size.height * 0.9f
    val unit = startY - homeY
    val cx = size.width / 2f
    val home = Offset(cx, homeY)
    // Vuelta justa, anillos y rótulos.
    drawLine(
      Clay.Cream.copy(alpha = 0.45f), Offset(cx, startY), home, 2.dp.toPx(),
      pathEffect = androidx.compose.ui.graphics.PathEffect.dashPathEffect(floatArrayOf(8.dp.toPx(), 6.dp.toPx()))
    )
    drawCircle(Clay.Cream.copy(alpha = 0.35f), unit * 0.35f, home, style = Stroke(1.5.dp.toPx()))
    drawCircle(Clay.Lime.copy(alpha = 0.6f), unit * 0.12f, home, style = Stroke(1.5.dp.toPx()))
    drawCircle(Clay.Ink, 5.dp.toPx(), Offset(cx, startY))
    drawCircle(Clay.Cream, 3.dp.toPx(), Offset(cx, startY))
    val small = TextStyle(color = TextSoft, fontSize = 13.sp)
    val startLab = measurer.measure("inicio de la vuelta", small)
    drawText(startLab, topLeft = Offset(cx + 10.dp.toPx(), startY - startLab.size.height / 2f))
    // Tu base: anillo crema con centro coral (como en el juego).
    drawCircle(Clay.Ink, 13.dp.toPx(), home + Offset(0f, 3.dp.toPx()))
    drawCircle(Clay.Ink, 13.dp.toPx(), home)
    drawCircle(Clay.Cream, 10.5.dp.toPx(), home)
    drawCircle(Clay.Coral, 4.5.dp.toPx(), home)
    val homeLab = measurer.measure("tu base", small)
    drawText(homeLab, topLeft = Offset(cx - homeLab.size.width / 2f, homeY - 17.dp.toPx() - homeLab.size.height))
    // Llegadas.
    val pad = 6.dp.toPx()
    for (t in trips) {
      val x = (cx + t.lateral * unit).coerceIn(pad, size.width - pad)
      val y = (homeY + (1f - t.along) * unit).coerceIn(pad, size.height - pad)
      drawCircle(Clay.Ink, 6.dp.toPx(), Offset(x, y))
      drawCircle(Clay.Sun, 4.5.dp.toPx(), Offset(x, y))
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
    val small = TextStyle(color = TextSoft, fontSize = 13.sp)
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

// ---------- Tráfico Estelar: planificas / a último momento ----------

/** Barra de arcilla partida en dos: lima (planificas) y sol (a último momento). Los % van en texto debajo. */
@Composable
private fun PlanReactBar(proactivePct: Int, modifier: Modifier = Modifier) {
  Canvas(modifier) {
    val r = androidx.compose.ui.geometry.CornerRadius(size.height / 2f)
    val drop = 3.dp.toPx()
    val h = size.height - drop
    drawRoundRect(Clay.Ink, Offset(0f, drop), androidx.compose.ui.geometry.Size(size.width, h), r)
    drawRoundRect(Clay.Sun, Offset.Zero, androidx.compose.ui.geometry.Size(size.width, h), r)
    val w = size.width * proactivePct.coerceIn(0, 100) / 100f
    if (w > 0f) {
      clipRect(right = w) { drawRoundRect(Clay.Lime, Offset.Zero, androidx.compose.ui.geometry.Size(size.width, h), r) }
    }
    drawRoundRect(Clay.Ink, Offset.Zero, androidx.compose.ui.geometry.Size(size.width, h), r, style = Stroke(2.5.dp.toPx()))
  }
}
