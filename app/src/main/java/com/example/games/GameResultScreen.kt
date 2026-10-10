package com.example.games

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
import androidx.compose.foundation.layout.ColumnScope
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
import com.example.data.ResultFocus
import com.example.model.GamePlayResult
import com.example.model.GameRankInfo
import com.example.model.GameRegistry
import com.example.ui.components.MeaningfulResult
import com.example.ui.components.MeaningfulResultModel
import com.example.ui.components.ResultChip
import com.example.ui.components.rememberReduceMotion
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

/**
 * La frase de debajo del título. «Precisión y ritmo / equilibrio entre precisión y velocidad» solo vale donde el puntaje se apoya en la
 * rapidez; los juegos de memoria, estimación o razonamiento sin tiempo de reacción dicen solo «precisión» (Rastro de luz no mide velocidad).
 */
object ResultPhrases {
  /** Juegos cuyo puntaje y medida NO usan rapidez: ni tiempo de reacción en el motor ni una medida en ms o por segundo. */
  val NO_SPEED_GAMES = setOf("secuencia", "satelites", "aterrizaje", "anagramas", "intrusa", "engranajes", "bodega", "parejas", "correo")

  /**
   * La frase del veredicto: UN solo tono, cálido y sin culpa (el 3-oct se quitaron las variantes de tono: lo que cambia con la preferencia de la persona
   * es el ORDEN del resultado, ver [com.example.data.ResultFocus]). Las reglas de arriba valen: sin «velocidad» ni «ritmo» donde el puntaje no la usa.
   */
  fun feedback(gameId: String, score: Int): String {
    val noSpeed = gameId in NO_SPEED_GAMES
    return when {
      noSpeed -> when {
        score >= 85 -> "¡Qué precisión! Esta partida merece una celebración."
        score >= 60 -> "¡Muy buena precisión! Sigue así."
        else -> "¡Jugaste! La dificultad se ajusta para que sigas disfrutando."
      }
      score >= 85 -> "¡Excelente! Tu precisión y tu ritmo brillaron hoy."
      score >= 60 -> "¡Muy bien! Buen equilibrio entre precisión y velocidad."
      else -> "¡Jugaste! La dificultad se ajusta para que sigas disfrutando."
    }
  }
}


private val TextSoft = Color(0xFFB4BFEA) // secundario sobre el cielo nocturno (contraste > 7:1)

/**
 * Pantalla de resultado de la app, con el sello "noche + arcilla" (rediseño 25-sep): va sobre el cielo
 * nocturno de la app (`CosmosBackground`, visible porque el fondo del tema es transparente), sin tarjetas: la
 * información es texto suelto y objetos, lo tocable es arcilla.
 *
 * Un solo protagonista animado: el puntaje cuenta hasta su valor, luego aparecen los luceros con rebote y, si
 * te fue bien (2-3 luceros), una lluvia de destellos. Con "quitar animaciones" del sistema todo aparece quieto.
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
  modeNote: String? = null,
  /** El atlas de La estrella intrusa (láminas ganadas y por repasar); solo lo usa ese juego. */
  atlas: com.example.data.AtlasState? = null,
  /** Las medidas de los juegos estrella guardadas por partida (`star_measures`): «Tu freno» de Freno de Emergencia es el promedio de las últimas, no la de una sola partida. */
  starMeasures: List<com.example.data.MeasurePoint> = emptyList(),
  /** Rescate relámpago: las cápsulas rescatadas y los viajes a la estación de toda la vida (ya con esta partida sumada), para «En total: …». */
  rescateTotals: com.example.data.Rescate.Totals? = null,
  /** Acoplamiento: los módulos acoplados y los anillos completos de toda la vida (ya con esta partida sumada), para «Has acoplado …». */
  acoplamientoTotals: com.example.data.Acoplamiento.Totals? = null,
  /** Aterrizaje Lunar: las cúpulas de tu base de toda la vida (ya con esta partida sumada), para «Tu base lunar: …». */
  aterrizajeTotals: com.example.data.Aterrizaje.Totals? = null,
  /** Qué te sirve más ver primero (`result_focus`): solo cambia el orden de lo que se muestra. */
  resultFocus: ResultFocus = ResultFocus.DEFAULT
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
  val reduceMotion = rememberReduceMotion()

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

    // Protagonista: halo del dominio, destellos, luceros y el puntaje gigante de arcilla.
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
      text = ResultPhrases.feedback(result.gameId, result.score),
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

    // Las medidas de la partida (propias de cada juego): «Lo que avancé». Más abajo se decide si van antes o después del consejo.
    val measures: @Composable ColumnScope.() -> Unit = {
    // Piloto Estelar («la ruta de las balizas», 9-oct): «Tus señales a los mandos» (TODO medido con las dos tareas a la vez: no hay medida de una tarea sola) y, debajo, lo que pasó en el vuelo.
    result.pilLanePct?.let { lane ->
      Spacer(Modifier.height(14.dp))
      val measure = com.example.data.Piloto.measureLine(result.pilSignalPct)
      val spoken = com.example.data.Piloto.spoken(result.pilSignalPct, lane, result.pilHits, result.pilTargets)
      Column(horizontalAlignment = Alignment.CenterHorizontally, modifier = Modifier.semantics(mergeDescendants = true) { contentDescription = spoken }) {
        if (measure != null) {
          Text(measure, color = Clay.Sky, fontWeight = FontWeight.Bold, fontSize = 18.sp, fontFamily = AppFamily)
          Text(
            text = com.example.data.Piloto.MEASURE_EXPLANATION,
            color = TextSoft,
            fontSize = 15.sp,
            textAlign = TextAlign.Center,
            modifier = Modifier.padding(horizontal = 28.dp, vertical = 2.dp)
          )
        } else {
          Text(
            text = com.example.data.Piloto.TOO_FEW_LINE,
            color = TextSoft,
            fontSize = 15.sp,
            textAlign = TextAlign.Center,
            modifier = Modifier.padding(horizontal = 28.dp, vertical = 2.dp)
          )
        }
        Spacer(Modifier.height(6.dp))
        listOfNotNull(
          com.example.data.Piloto.laneLine(lane),
          com.example.data.Piloto.missionLine(result.pilHits, result.pilTargets),
          com.example.data.Piloto.wrongLine(result.pilFalse),
          com.example.data.Piloto.levelLine(result.pilSignalLevel),
          com.example.data.Piloto.streakLine(result.pilBestStreak),
          com.example.data.Piloto.pointsLine(result.pilPoints)
        ).forEach { Text(it, color = Clay.Cream, fontSize = 14.sp, fontWeight = FontWeight.SemiBold, modifier = Modifier.padding(top = 2.dp)) }
        com.example.data.Piloto.advice(result.pilHits, result.pilTargets, result.pilFalse, lane)?.let {
          Text(
            text = it,
            color = Clay.Sun,
            fontSize = 15.sp,
            fontWeight = FontWeight.SemiBold,
            textAlign = TextAlign.Center,
            modifier = Modifier.padding(horizontal = 28.dp, vertical = 8.dp)
          )
        }
      }
    }

    // Rescate relámpago («qué cápsulas viste», 9-oct): las cápsulas a salvo (el premio), «Tu captura» (de 4), «Tu vistazo» y lo que pasó en la partida. Ya no hay «Tu radar» ni «Tu filtro»: no se responde dónde. Ver docs/medidas-juegos-estrella.md.
    result.rescRescued?.let { rescued ->
      Spacer(Modifier.height(14.dp))
      Column(horizontalAlignment = Alignment.CenterHorizontally, modifier = Modifier.semantics(mergeDescendants = true) { contentDescription = com.example.data.Rescate.spoken(rescued, result.captureK, result.glanceMs) }) {
        RescuedCapsules(com.example.data.Rescate.dots(rescued), Modifier.padding(bottom = 8.dp))
        com.example.data.Rescate.rescuedLine(rescued)?.let { Text(it, color = Clay.Lime, fontWeight = FontWeight.Bold, fontSize = 20.sp, fontFamily = AppFamily) }
        com.example.data.Rescate.perfectLine(result.rescPerfect, result.rescRounds, result.rescBestStreak)?.let {
          Text(it, color = Clay.Cream, fontSize = 14.sp, fontWeight = FontWeight.SemiBold, modifier = Modifier.padding(top = 6.dp))
        }
        com.example.data.Rescate.shortestLine(result.rescShortestMs)?.let {
          Text(it, color = Clay.Cream, fontSize = 14.sp, fontWeight = FontWeight.SemiBold, modifier = Modifier.padding(top = 2.dp))
        }
        com.example.data.Rescate.tripsLine(result.rescTrips)?.let {
          Text(it, color = Clay.Cream, fontSize = 14.sp, fontWeight = FontWeight.SemiBold, modifier = Modifier.padding(top = 2.dp))
        }
        com.example.data.Rescate.recordLine(result.rescBest, result.rescNewRecord)?.let {
          Text(
            it,
            color = if (result.rescNewRecord == true) Clay.Sun else TextSoft,
            fontSize = if (result.rescNewRecord == true) 15.sp else 14.sp,
            fontWeight = if (result.rescNewRecord == true) FontWeight.Bold else FontWeight.SemiBold,
            modifier = Modifier.padding(top = 2.dp)
          )
        }
        com.example.data.Rescate.totalLine(rescateTotals)?.let {
          Text(it, color = TextSoft, fontSize = 14.sp, fontWeight = FontWeight.SemiBold, textAlign = TextAlign.Center, modifier = Modifier.padding(top = 2.dp, start = 24.dp, end = 24.dp))
        }
      }
    }
    com.example.data.Rescate.glanceLine(result.glanceMs)?.let { line ->
      Spacer(Modifier.height(14.dp))
      Text(text = line, color = Clay.Sky, fontWeight = FontWeight.Bold, fontSize = 18.sp, fontFamily = AppFamily)
      Text(
        text = com.example.data.Rescate.glanceExplanation(result.glanceLoad),
        color = TextSoft,
        fontSize = 15.sp,
        textAlign = TextAlign.Center,
        modifier = Modifier.padding(horizontal = 28.dp, vertical = 2.dp)
      )
    }
    result.captureK?.let { k ->
      Spacer(Modifier.height(12.dp))
      val line = com.example.data.Rescate.captureLine(k) ?: "Tu captura"
      Text(text = line, color = Clay.Sun, fontWeight = FontWeight.Bold, fontSize = 18.sp, fontFamily = AppFamily)
      Spacer(Modifier.height(6.dp))
      TrackingSlots(k, slots = com.example.data.Rescate.CAPTURE_MAX, modifier = Modifier.semantics { contentDescription = line })
      Text(
        text = com.example.data.Rescate.CAPTURE_EXPLANATION,
        color = TextSoft,
        fontSize = 15.sp,
        textAlign = TextAlign.Center,
        modifier = Modifier.padding(horizontal = 28.dp, vertical = 4.dp)
      )
    }

    // Satélites: «Encendiste N luces» (el planeta con las luces que se ganaron), «Tu seguimiento» (cuántos se siguieron de verdad a la vez, sin contar la suerte), las rondas perfectas, el récord de luces y la velocidad superada.
    // Las partidas del juego anterior (sin luces) siguen mostrando solo «Tu seguimiento».
    if (result.satLights != null) {
      Spacer(Modifier.height(14.dp))
      val spoken = com.example.data.Satelites.spoken(result.satLights, result.trackingCapacity, result.trackingTargets)
      LitPlanet(com.example.data.Satelites.dots(result.satLights), Modifier.size(150.dp).semantics { contentDescription = spoken })
      com.example.data.Satelites.lightsLine(result.satLights)?.let {
        Spacer(Modifier.height(6.dp))
        Text(it, color = Clay.Sun, fontWeight = FontWeight.Bold, fontSize = 20.sp, fontFamily = AppFamily)
      }
    }
    result.trackingCapacity?.let { cap ->
      Spacer(Modifier.height(14.dp))
      val capText = String.format(java.util.Locale("es"), "%.1f", cap)
      val targets = result.trackingTargets
      Text(
        text = com.example.data.Satelites.trackingLine(cap, targets) ?: "Tu seguimiento: $capText a la vez",
        color = Clay.Sun,
        fontWeight = FontWeight.Bold,
        fontSize = 18.sp,
        fontFamily = AppFamily
      )
      Spacer(Modifier.height(6.dp))
      TrackingSlots(cap, Modifier.semantics { contentDescription = "Sigues $capText satélites a la vez" })
      com.example.data.Satelites.perfectLine(result.satPerfect, result.totalTrials, result.satBestStreak)?.let {
        Text(it, color = Clay.Cream, fontSize = 14.sp, fontWeight = FontWeight.SemiBold, modifier = Modifier.padding(top = 6.dp))
      }
      com.example.data.Satelites.recordLine(result.satBest, result.satNewRecord)?.let {
        Text(
          it,
          color = if (result.satNewRecord == true) Clay.Sun else TextSoft,
          fontSize = if (result.satNewRecord == true) 15.sp else 14.sp,
          fontWeight = if (result.satNewRecord == true) FontWeight.Bold else FontWeight.SemiBold,
          modifier = Modifier.padding(top = 2.dp)
        )
      }
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
        text = if (result.satLights != null)
          "Cuántos seguiste de verdad al mismo tiempo, sin contar los que aciertas por suerte. Cambia con la velocidad y con cuántos satélites hay que seguir: el juego los va sumando a medida que aciertas."
        else if (targets != null && targets < 3.5f)
          "Cuántos seguiste de verdad de los que había que seguir, sin contar los que aciertas por suerte. El juego suma satélites y velocidad a medida que aciertas: así se ve hasta dónde llegas."
        else
          "Cuántos seguiste de verdad al mismo tiempo, sin contar los que aciertas por suerte. A velocidad moderada, los adultos suelen seguir entre 3 y 4; más rápido, menos (Alvarez y Franconeri, 2007).",
        color = TextSoft,
        fontSize = 15.sp,
        textAlign = TextAlign.Center,
        modifier = Modifier.padding(horizontal = 28.dp, vertical = 4.dp)
      )
    }

    // Freno de Emergencia: "tu freno" como PROMEDIO de las últimas partidas, en un velocímetro de tres zonas con nombre (sin milisegundos: una sola partida trae pocos altos) + cuántos altos frenó.
    if (result.stopsTotal != null) {
      Spacer(Modifier.height(14.dp))
      val brake = result.brakeMs
      val reading = com.example.data.Brake.reading(starMeasures, brake, result.timestamp)
      if (reading != null) {
        Text(
          text = reading.headline,
          color = Clay.Coral,
          fontWeight = FontWeight.Bold,
          fontSize = 18.sp,
          fontFamily = AppFamily,
          textAlign = TextAlign.Center,
          modifier = Modifier.padding(horizontal = 24.dp)
        )
        Spacer(Modifier.height(6.dp))
        BrakeGauge(reading, Modifier.semantics { contentDescription = reading.spoken })
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
        text = if (brake != null) "Mide cuánto tardas en frenar una acción que ya ibas a hacer. Una sola partida trae pocos altos: por eso mostramos el promedio de varias."
        else "Esta vez no se pudo estimar tu freno: hacen falta al menos 6 altos y haber frenado entre 1 de cada 4 y 3 de cada 4. Lanza apenas se encienda la luz, sin esperar al ALTO: así la medida funciona." +
          if (reading != null) " Una sola partida trae pocos altos: por eso mostramos el promedio de varias." else "",
        color = TextSoft,
        fontSize = 15.sp,
        textAlign = TextAlign.Center,
        modifier = Modifier.padding(horizontal = 28.dp, vertical = 2.dp)
      )
    }

    // Aterrizaje Lunar (renovado el 10-oct; docs/diseno-aterrizaje.md §6): un final CON SENTIDO, con el componente reutilizable MeaningfulResult: lo que hiciste («11 % de la regla» de distancia promedio al lugar justo, con un dato tuyo y lo que pasó), tu avance SOLO contigo (hoy, tu promedio y tu mejor), un truco para la próxima
    // y, abajo y en chico, por qué importa con su fuente. Ya no hay «Tu línea» (el gráfico de puntos no le decía nada a la persona). Nunca percentiles ni comparación con otras personas.
    if (result.gameId == "aterrizaje" && result.numlineErrorPct != null) {
      Spacer(Modifier.height(18.dp))
      val model = remember(result, starMeasures, aterrizajeTotals) { aterrizajeModel(result, starMeasures, aterrizajeTotals) }
      MeaningfulResult(model)
    }

    // Lluvia de meteoros (pantalla final, 1-oct: mezcla de las propuestas A y C de docs/previews/meteoros-final.png): "tu
    // vocabulario" = una cifra grande ("8 de cada 10") + tres barras (comunes, intermedias, raras) + una frase; "tu
    // reconocimiento" en una línea; "tu filtro" con el total y tres renglones; "tu colección" en una línea.
    val lexSeen = result.lexBandSeen
    val lexHits = result.lexBandHits
    if (lexSeen != null && lexHits != null) {
      val vocab = com.example.data.Vocabulary
      val bands = vocab.bandPercents(lexSeen, lexHits, result.lexFaSeen, result.lexFaHits)
      val groups = vocab.groupPercents(lexSeen, lexHits, result.lexFaSeen, result.lexFaHits)
      val phrase = vocab.phraseFor(groups)
      Spacer(Modifier.height(14.dp))
      Text("Tu vocabulario", color = Clay.Sun, fontWeight = FontWeight.Bold, fontSize = 19.sp, fontFamily = AppFamily)
      Column(
        Modifier.fillMaxWidth().padding(horizontal = 28.dp).semantics { contentDescription = "Tu vocabulario. $phrase" },
        horizontalAlignment = Alignment.CenterHorizontally
      ) {
        val lowGroup = vocab.lowestGroup(groups)
        vocab.GROUP_LABELS.forEachIndexed { i, label ->
          VocabBar(label, groups[i], i == lowGroup, Modifier.fillMaxWidth().padding(vertical = 4.dp))
        }
      }

      vocab.recognitionSentence(result.lexRtCommonMs, result.lexRtRareMs)?.let { line ->
        Spacer(Modifier.height(14.dp))
        Text("Tu reconocimiento", color = Clay.Sun, fontWeight = FontWeight.Bold, fontSize = 19.sp, fontFamily = AppFamily)
        Text(line, color = Clay.Cream, fontSize = 15.sp, fontWeight = FontWeight.SemiBold, textAlign = TextAlign.Center, modifier = Modifier.padding(horizontal = 16.dp))
        Text("Es normal: las raras tardan más.", color = TextSoft, fontSize = 14.sp)
      }

      val faSeen = result.lexFaSeen
      val faHits = result.lexFaHits
      vocab.filterHeadline(faSeen, faHits)?.let { headline ->
        Spacer(Modifier.height(14.dp))
        Text("Tu filtro", color = Clay.Sun, fontWeight = FontWeight.Bold, fontSize = 19.sp, fontFamily = AppFamily)
        Text(headline, color = Clay.Cream, fontSize = 15.sp, textAlign = TextAlign.Center, modifier = Modifier.padding(horizontal = 28.dp))
      }

      result.lexRareWords?.takeIf { it.isNotEmpty() }?.let { words ->
        Spacer(Modifier.height(14.dp))
        Row(Modifier.fillMaxWidth().padding(horizontal = 28.dp), verticalAlignment = Alignment.CenterVertically) {
          Text("Tu colección", color = Clay.Sun, fontWeight = FontWeight.Bold, fontSize = 19.sp, fontFamily = AppFamily, modifier = Modifier.weight(1f))
          Text(
            text = "+${words.size} palabra${if (words.size == 1) "" else "s"} rara${if (words.size == 1) "" else "s"}",
            color = Clay.Lime,
            fontWeight = FontWeight.Bold,
            fontSize = 17.sp,
            fontFamily = AppFamily
          )
        }
        Text(words.joinToString(" · "), color = Clay.Cream, fontSize = 15.sp, textAlign = TextAlign.Center, modifier = Modifier.padding(horizontal = 28.dp, vertical = 2.dp))
      }
    }

    // Cosecha de palabras (pantalla final, maqueta docs/previews/cosecha.png, pantalla 4): "tu cosecha" (cifra grande de palabras y
    // "de las comunes, N de M"), "tu manera de buscar" (barra partida racimos / saltos, con la frase-consejo y "cómo buscaste en esta
    // partida"), "tu ritmo" (primeros contra últimos 20 s), tu palabra estrella y "también podías". Sin recuadros, nunca solo color.
    val harvWords = com.example.data.Harvest.words(result.harvWords)
    if (harvWords != null) {
      val harv = com.example.data.Harvest
      Spacer(Modifier.height(14.dp))
      Text("Tu cosecha", color = Clay.Sun, fontWeight = FontWeight.Bold, fontSize = 19.sp, fontFamily = AppFamily)
      Column(
        Modifier.fillMaxWidth().padding(horizontal = 28.dp).semantics { contentDescription = harv.spoken(harvWords, result.harvCommonFound, result.harvCommonTotal) },
        horizontalAlignment = Alignment.CenterHorizontally
      ) {
        // la cifra grande es lo propio de la cosecha (las palabras comunes que habia); el total de palabras ya esta en la fila "aciertos"
        val common = harv.common(result.harvCommonFound, result.harvCommonTotal)
        Row(verticalAlignment = Alignment.Bottom) {
          Text(if (common != null) "${common.first} de ${common.second}" else "$harvWords", color = Clay.Grape, fontWeight = FontWeight.Bold, fontSize = 52.sp, fontFamily = AppFamily)
          Text(
            if (common != null) "palabras comunes" else harv.wordsLabel(harvWords), color = Clay.Cream, fontSize = 20.sp, fontWeight = FontWeight.SemiBold,
            modifier = Modifier.padding(start = 8.dp, bottom = 10.dp)
          )
        }
      }

      harv.clusterPct(result.harvClusterPct)?.let { pct ->
        Spacer(Modifier.height(14.dp))
        Text("Tu manera de buscar", color = Clay.Sun, fontWeight = FontWeight.Bold, fontSize = 19.sp, fontFamily = AppFamily)
        Column(Modifier.fillMaxWidth().padding(horizontal = 34.dp, vertical = 6.dp)) {
          HarvestSplitBar(pct, Modifier.fillMaxWidth().height(14.dp))
          Row(Modifier.fillMaxWidth().padding(top = 6.dp)) {
            Text(harv.clusterLabel(pct), color = Clay.Cream, fontSize = 16.sp, fontWeight = FontWeight.Bold, modifier = Modifier.weight(1f))
            Text(harv.jumpLabel(pct), color = Clay.Cream, fontSize = 16.sp, fontWeight = FontWeight.Bold)
          }
        }
        harv.searchLine(pct)?.let {
          Text(it, color = Clay.Cream, fontSize = 15.sp, textAlign = TextAlign.Center, modifier = Modifier.padding(horizontal = 28.dp, vertical = 4.dp))
        }
      }

      val first = result.harvFirst20
      val last = result.harvLast20
      harv.rhythmLine(first, last)?.let { line ->
        Spacer(Modifier.height(14.dp))
        Text("Tu ritmo", color = Clay.Sun, fontWeight = FontWeight.Bold, fontSize = 19.sp, fontFamily = AppFamily)
        Column(Modifier.fillMaxWidth().padding(horizontal = 28.dp, vertical = 6.dp)) {
          HarvestRhythmRow("Primeros 20 s", first ?: 0, harv.barFraction(first ?: 0, last ?: 0), Clay.Grape, Modifier.fillMaxWidth().padding(vertical = 3.dp))
          HarvestRhythmRow("Últimos 20 s", last ?: 0, harv.barFraction(last ?: 0, first ?: 0), Clay.Sky, Modifier.fillMaxWidth().padding(vertical = 3.dp))
        }
        Text(line, color = Clay.Cream, fontSize = 15.sp, textAlign = TextAlign.Center, modifier = Modifier.padding(horizontal = 28.dp))
      }

      harv.starTitle(result.harvStar, result.harvBest)?.let { (title, word) ->
        Spacer(Modifier.height(14.dp))
        Text(title, color = Clay.Sun, fontWeight = FontWeight.Bold, fontSize = 19.sp, fontFamily = AppFamily)
        Text(word, color = Clay.Sun, fontWeight = FontWeight.Bold, fontSize = 34.sp, fontFamily = AppFamily, modifier = Modifier.padding(vertical = 2.dp))
      }
      harv.missedLine(result.harvMissed)?.let {
        Text(it, color = Clay.Cream, fontSize = 15.sp, textAlign = TextAlign.Center, modifier = Modifier.padding(horizontal = 28.dp, vertical = 4.dp))
      }
      result.harvHints?.takeIf { it > 0 }?.let {
        Text("Pistas de Nubi: $it", color = TextSoft, fontSize = 14.sp)
      }
    }

    // La estrella intrusa (pantalla final, Atlas celeste): cada dato aparece UNA vez y en este orden: "tu red de significados" (una barra
    // por categoría con al menos 3 rondas, la más baja marcada con TEXTO; el total de aciertos ya está en la fila "aciertos"), "las
    // trampas" (solo cuántas te engañaron: lo resistido ya está en la barra), "¿qué las une?", "tu atlas" SIN recuadro (hasta 3
    // miniaturas de láminas ganadas hoy y una línea de cifras), la rapidez como dato secundario y la nota al pie.
    val intrSeen = result.intrSeenType
    if (intrSeen != null) {
      val atlasLogic = com.example.data.Atlas
      Spacer(Modifier.height(14.dp))
      Text("Tu red de significados", color = Clay.Sun, fontWeight = FontWeight.Bold, fontSize = 19.sp, fontFamily = AppFamily)
      val rows = atlasLogic.categoryRows(intrSeen, result.intrHitsType)
      if (rows.isNotEmpty()) {
        Column(
          Modifier.fillMaxWidth().padding(horizontal = 28.dp, vertical = 8.dp).semantics { contentDescription = atlasLogic.spoken(intrSeen, result.intrHitsType) }
        ) {
          rows.forEach { row -> AtlasBar(row, Modifier.fillMaxWidth().padding(vertical = 3.dp)) }
        }
      } else {
        Text(
          "Juega unas rondas más para ver tu red: cada tipo se mide con 3 rondas o más.",
          color = TextSoft, fontSize = 14.sp, textAlign = TextAlign.Center, modifier = Modifier.padding(horizontal = 28.dp, vertical = 4.dp)
        )
      }
      if (atlasLogic.trapReading(intrSeen, result.intrHitsType) != null) {
        Spacer(Modifier.height(12.dp))
        Text("Las trampas", color = Clay.Sun, fontWeight = FontWeight.Bold, fontSize = 19.sp, fontFamily = AppFamily)
        atlasLogic.trapFooledLine(intrSeen, result.intrHitsType)?.let {
          Text(it, color = Clay.Coral, fontSize = 17.sp, fontWeight = FontWeight.Bold, textAlign = TextAlign.Center, modifier = Modifier.padding(horizontal = 28.dp, vertical = 2.dp))
        }
        atlasLogic.trapReading(intrSeen, result.intrHitsType)?.let {
          Text(com.example.data.ResultAdvice.body(it), color = Clay.Cream, fontSize = 14.sp, textAlign = TextAlign.Center, modifier = Modifier.padding(horizontal = 28.dp, vertical = 2.dp))
        }
      }
      atlasLogic.bonusLine(result.intrBonusSeen, result.intrBonusHits)?.let {
        Spacer(Modifier.height(12.dp))
        Text("¿Qué las une?", color = Clay.Sun, fontWeight = FontWeight.Bold, fontSize = 19.sp, fontFamily = AppFamily)
        Text(it, color = Clay.Cream, fontSize = 17.sp, fontWeight = FontWeight.SemiBold, textAlign = TextAlign.Center)
      }
      // Tu atlas: las láminas de hoy (hasta 3) y las cifras, sin recuadro
      val today = remember { java.time.LocalDate.now().toEpochDay().toInt() }
      val figures = remember { com.example.data.FigureBank.load(context) }
      val summary = atlas?.let { atlasLogic.summary(it, today) }
      if (summary != null) {
        Spacer(Modifier.height(14.dp))
        Text("Tu atlas", color = Clay.Sun, fontWeight = FontWeight.Bold, fontSize = 19.sp, fontFamily = AppFamily)
        val thumbs = atlas?.let { atlasLogic.todayPlates(it, today) }.orEmpty().mapNotNull { figures[it] }
        if (thumbs.isNotEmpty()) {
          Spacer(Modifier.height(6.dp))
          com.example.ui.components.AtlasThumbRow(thumbs, Modifier.padding(horizontal = 16.dp))
        }
        Text(summary, color = Clay.Cream, fontSize = 17.sp, fontWeight = FontWeight.SemiBold, textAlign = TextAlign.Center, modifier = Modifier.padding(horizontal = 28.dp, vertical = 6.dp))
      }
      atlasLogic.speedLine(result.intrRtMs)?.let {
        Text(it, color = TextSoft, fontSize = 14.sp, textAlign = TextAlign.Center, modifier = Modifier.padding(top = 8.dp))
      }
      if (rows.any { it.lowest }) {
        Text(
          "Cada tipo se mide con 3 rondas o más. Sirve para ver dónde practicar, no para compararte.",
          color = TextSoft, fontSize = 14.sp, textAlign = TextAlign.Center, modifier = Modifier.padding(horizontal = 28.dp, vertical = 8.dp)
        )
      }
    }

    // En la punta de la lengua (pantalla final, docs/diseno-punta-de-la-lengua.md §6): «Tu cielo de palabras». «Encontraste X de N por tu cuenta» (las palabras
    // con lucero dorado), el desglose en una línea, la lista de palabras con su lucero (el nombre del lucero va en TEXTO: nunca solo color), las azules que vuelven y,
    // con 3 o más solas, el tiempo hasta «¡La tengo!». Cada dato aparece UNA vez; sin recuadros.
    val puntaSolo = result.puntaSolo
    if (puntaSolo != null) {
      val punta = com.example.data.Punta
      val puntaTotal = punta.total(puntaSolo, result.puntaPista, result.puntaLetras, result.puntaVista)
      Spacer(Modifier.height(14.dp))
      Text("Tu cielo de palabras", color = Clay.Sun, fontWeight = FontWeight.Bold, fontSize = 19.sp, fontFamily = AppFamily)
      Column(
        Modifier.fillMaxWidth().padding(horizontal = 28.dp).semantics { contentDescription = punta.spoken(puntaSolo, puntaTotal) }.testTag("punta_headline"),
        horizontalAlignment = Alignment.CenterHorizontally
      ) {
        Text("Encontraste", color = TextSoft, fontSize = 15.sp, fontWeight = FontWeight.SemiBold)
        Row(verticalAlignment = Alignment.Bottom) {
          Text("${puntaSolo.coerceIn(0, maxOf(puntaTotal, 0))} de $puntaTotal", color = Clay.Grape, fontWeight = FontWeight.Bold, fontSize = 52.sp, fontFamily = AppFamily)
          Text("por tu cuenta", color = Clay.Cream, fontSize = 20.sp, fontWeight = FontWeight.SemiBold, modifier = Modifier.padding(start = 8.dp, bottom = 10.dp))
        }
        punta.breakdown(puntaSolo, result.puntaPista, result.puntaLetras, result.puntaVista)?.let {
          Text(it, color = TextSoft, fontSize = 14.sp, textAlign = TextAlign.Center)
        }
      }
      val puntaEntries = result.puntaWords.orEmpty()
      if (puntaEntries.isNotEmpty()) {
        Spacer(Modifier.height(10.dp))
        Column(Modifier.fillMaxWidth().padding(horizontal = 36.dp).testTag("punta_words")) {
          puntaEntries.forEach { PuntaWordRow(it, Modifier.fillMaxWidth().padding(vertical = 4.dp)) }
        }
      }
      punta.blueNote(result.puntaVista)?.let {
        Text(it, color = Clay.Cream, fontSize = 15.sp, fontWeight = FontWeight.SemiBold, textAlign = TextAlign.Center, modifier = Modifier.padding(horizontal = 28.dp, vertical = 6.dp).testTag("punta_blue_note"))
      }
      punta.speedLine(result.puntaMs, puntaSolo)?.let {
        Text(it, color = TextSoft, fontSize = 14.sp, textAlign = TextAlign.Center, modifier = Modifier.padding(horizontal = 28.dp, vertical = 4.dp))
      }
    }

    // Carga exacta (pantalla final, docs/diseno-carga-exacta.md §6): «Tu reactor». «Lograste X de N sin pista» (las cargas sin la pista de Nubi), el desglose en una
    // línea y, con 3 o más cargas sin pista, el tiempo medio por carga y cuántas fueron por el camino más corto. Cada dato aparece UNA vez; sin recuadros.
    val cargaAlone = result.cargaAlone
    if (cargaAlone != null) {
      val carga = com.example.data.Carga
      val cargaTotal = result.totalTrials
      Spacer(Modifier.height(14.dp))
      Text("Tu reactor", color = Clay.Sun, fontWeight = FontWeight.Bold, fontSize = 19.sp, fontFamily = AppFamily)
      Column(
        Modifier.fillMaxWidth().padding(horizontal = 28.dp).semantics { contentDescription = carga.spoken(cargaAlone, cargaTotal) }.testTag("carga_headline"),
        horizontalAlignment = Alignment.CenterHorizontally
      ) {
        Text("Lograste", color = TextSoft, fontSize = 15.sp, fontWeight = FontWeight.SemiBold)
        Row(verticalAlignment = Alignment.Bottom) {
          Text("${cargaAlone.coerceIn(0, maxOf(cargaTotal, 0))} de $cargaTotal", color = Clay.Grape, fontWeight = FontWeight.Bold, fontSize = 52.sp, fontFamily = AppFamily)
          Text("sin pista", color = Clay.Cream, fontSize = 20.sp, fontWeight = FontWeight.SemiBold, modifier = Modifier.padding(start = 8.dp, bottom = 10.dp))
        }
        carga.breakdown(cargaAlone, result.cargaHinted, cargaTotal)?.let {
          Text(it, color = TextSoft, fontSize = 14.sp, textAlign = TextAlign.Center)
        }
      }
      carga.shortLine(result.cargaShort, cargaAlone)?.let {
        Text(it, color = TextSoft, fontSize = 14.sp, textAlign = TextAlign.Center, modifier = Modifier.padding(horizontal = 28.dp, vertical = 4.dp).testTag("carga_short"))
      }
      carga.speedLine(result.cargaMs, cargaAlone)?.let {
        Text(it, color = TextSoft, fontSize = 14.sp, textAlign = TextAlign.Center, modifier = Modifier.padding(horizontal = 28.dp, vertical = 4.dp).testTag("carga_speed"))
      }
    }

    // Engranajes (pantalla final, docs/diseno-engranajes.md §9): «Tu cohete». «Arreglaste X de N máquinas», la etapa más alta (de 5), qué pasó con el cohete (despegó, o
    // cuántas luces faltan y que espera en el hangar), los cohetes en órbita y, con 3 o más máquinas, el ritmo. Cada dato aparece UNA vez; sin recuadros.
    val engrEtapa = result.engrEtapa
    if (engrEtapa != null) {
      val eng = com.example.data.Engranajes
      val engTotal = result.totalTrials
      Spacer(Modifier.height(14.dp))
      Text("Tu cohete", color = Clay.Sun, fontWeight = FontWeight.Bold, fontSize = 19.sp, fontFamily = AppFamily)
      Column(
        Modifier.fillMaxWidth().padding(horizontal = 28.dp).semantics { contentDescription = eng.spoken(result.correctAnswers, engTotal) }.testTag("engranajes_headline"),
        horizontalAlignment = Alignment.CenterHorizontally
      ) {
        Text("Arreglaste", color = TextSoft, fontSize = 15.sp, fontWeight = FontWeight.SemiBold)
        Row(verticalAlignment = Alignment.Bottom) {
          Text("${result.correctAnswers.coerceIn(0, maxOf(engTotal, 0))} de $engTotal", color = Clay.Grape, fontWeight = FontWeight.Bold, fontSize = 52.sp, fontFamily = AppFamily)
          Text("máquinas", color = Clay.Cream, fontSize = 20.sp, fontWeight = FontWeight.SemiBold, modifier = Modifier.padding(start = 8.dp, bottom = 10.dp))
        }
      }
      eng.etapaLine(engrEtapa)?.let {
        Text(it, color = TextSoft, fontSize = 14.sp, textAlign = TextAlign.Center, modifier = Modifier.padding(horizontal = 28.dp, vertical = 4.dp).testTag("engranajes_etapa"))
      }
      eng.launchLine(result.engrLaunches, result.engrOrbit)?.let {
        Text(it, color = Clay.Sun, fontSize = 15.sp, fontWeight = FontWeight.SemiBold, textAlign = TextAlign.Center, modifier = Modifier.padding(horizontal = 28.dp, vertical = 4.dp).testTag("engranajes_launch"))
      }
      eng.hangarLine(result.engrLights)?.let {
        Text(it, color = TextSoft, fontSize = 14.sp, textAlign = TextAlign.Center, modifier = Modifier.padding(horizontal = 28.dp, vertical = 4.dp).testTag("engranajes_hangar"))
      }
      eng.orbitLine(result.engrOrbit)?.let {
        Text(it, color = TextSoft, fontSize = 14.sp, textAlign = TextAlign.Center, modifier = Modifier.padding(horizontal = 28.dp, vertical = 4.dp).testTag("engranajes_orbit"))
      }
      eng.paceLine(result.engrMs, engTotal)?.let {
        Text(it, color = TextSoft, fontSize = 14.sp, textAlign = TextAlign.Center, modifier = Modifier.padding(horizontal = 28.dp, vertical = 4.dp).testTag("engranajes_pace"))
      }
    }

    // Bodega de carga (pantalla final, docs/diseno-bodega-de-carga.md §7): «Tu bodega». «Encontraste X de N objetos al primer intento», la etapa más alta (de 5), la racha más larga, tu bodega
    // más grande de hoy (el pedido más grande sin errores), tu récord (con «¡Nuevo récord!» si lo superaste) y, con 3 o más objetos, el ritmo. Cada dato aparece UNA vez; sin recuadros.
    val bodGroup = result.bodGroup
    if (bodGroup != null) {
      val bod = com.example.data.Bodega
      val bodTotal = result.totalTrials
      Spacer(Modifier.height(14.dp))
      Text("Tu bodega", color = Clay.Sun, fontWeight = FontWeight.Bold, fontSize = 19.sp, fontFamily = AppFamily)
      Column(
        Modifier.fillMaxWidth().padding(horizontal = 28.dp).semantics { contentDescription = bod.spoken(result.correctAnswers, bodTotal) }.testTag("bodega_headline"),
        horizontalAlignment = Alignment.CenterHorizontally
      ) {
        Text("Al primer intento", color = TextSoft, fontSize = 15.sp, fontWeight = FontWeight.SemiBold)
        Row(verticalAlignment = Alignment.Bottom) {
          Text("${result.correctAnswers.coerceIn(0, maxOf(bodTotal, 0))} de $bodTotal", color = Clay.Grape, fontWeight = FontWeight.Bold, fontSize = 52.sp, fontFamily = AppFamily)
          Text("objetos", color = Clay.Cream, fontSize = 20.sp, fontWeight = FontWeight.SemiBold, modifier = Modifier.padding(start = 8.dp, bottom = 10.dp))
        }
      }
      bod.groupLine(bodGroup)?.let {
        Text(it, color = TextSoft, fontSize = 14.sp, textAlign = TextAlign.Center, modifier = Modifier.padding(horizontal = 28.dp, vertical = 4.dp).testTag("bodega_group"))
      }
      bod.streakLine(result.bodBestStreak, bodTotal)?.let {
        Text(it, color = TextSoft, fontSize = 14.sp, textAlign = TextAlign.Center, modifier = Modifier.padding(horizontal = 28.dp, vertical = 4.dp).testTag("bodega_streak"))
      }
      bod.biggestLine(result.bodBiggest)?.let {
        Text(it, color = TextSoft, fontSize = 14.sp, textAlign = TextAlign.Center, modifier = Modifier.padding(horizontal = 28.dp, vertical = 4.dp).testTag("bodega_biggest"))
      }
      bod.recordLine(result.bodBest, result.bodNewRecord)?.let {
        Text(it, color = if (result.bodNewRecord == true) Clay.Sun else TextSoft, fontSize = if (result.bodNewRecord == true) 15.sp else 14.sp, fontWeight = if (result.bodNewRecord == true) FontWeight.SemiBold else FontWeight.Normal, textAlign = TextAlign.Center, modifier = Modifier.padding(horizontal = 28.dp, vertical = 4.dp).testTag("bodega_record"))
      }
      bod.paceLine(result.bodMs, bodTotal)?.let {
        Text(it, color = TextSoft, fontSize = 14.sp, textAlign = TextAlign.Center, modifier = Modifier.padding(horizontal = 28.dp, vertical = 4.dp).testTag("bodega_pace"))
      }
    }

    // Constelaciones (pantalla final, docs/diseno-constelaciones.md §5): «Tu memoria de lugar». «X de Y veces fuiste directo a una pareja que ya habías visto» con el % en grande, las parejas de memoria,
    // la racha de memoria más larga, la etapa más alta (de 6), tu mejor racha (con «¡Nueva mejor racha!» si la superaste) y la nota de que lo encontrado por suerte no cuenta. Sin oportunidades no hay % («—»). Sin recuadros.
    val conGroup = result.conGroup
    if (conGroup != null) {
      val con = com.example.data.Constelaciones
      val conOpps = result.totalTrials
      Spacer(Modifier.height(14.dp))
      Text("Tu memoria de lugar", color = Clay.Sun, fontWeight = FontWeight.Bold, fontSize = 19.sp, fontFamily = AppFamily)
      Column(
        Modifier.fillMaxWidth().padding(horizontal = 28.dp).semantics { contentDescription = con.spoken(result.correctAnswers, conOpps) }.testTag("constelaciones_headline"),
        horizontalAlignment = Alignment.CenterHorizontally
      ) {
        Row(verticalAlignment = Alignment.Bottom) {
          Text(con.percent(result.correctAnswers, conOpps)?.let { "$it %" } ?: "—", color = Clay.Grape, fontWeight = FontWeight.Bold, fontSize = 52.sp, fontFamily = AppFamily)
        }
        Text(con.detailLine(result.correctAnswers, conOpps), color = TextSoft, fontSize = 15.sp, textAlign = TextAlign.Center, modifier = Modifier.padding(top = 2.dp))
      }
      con.memoryGroupsLine(result.conMemGroups, result.conGroups)?.let {
        Text(it, color = TextSoft, fontSize = 14.sp, textAlign = TextAlign.Center, modifier = Modifier.padding(horizontal = 28.dp, vertical = 4.dp).testTag("constelaciones_groups"))
      }
      con.streakLine(result.conBestStreak)?.let {
        Text(it, color = TextSoft, fontSize = 14.sp, textAlign = TextAlign.Center, modifier = Modifier.padding(horizontal = 28.dp, vertical = 4.dp).testTag("constelaciones_streak"))
      }
      con.groupLine(conGroup)?.let {
        Text(it, color = TextSoft, fontSize = 14.sp, textAlign = TextAlign.Center, modifier = Modifier.padding(horizontal = 28.dp, vertical = 4.dp).testTag("constelaciones_group"))
      }
      con.recordLine(result.conBest, result.conNewRecord)?.let {
        Text(it, color = if (result.conNewRecord == true) Clay.Sun else TextSoft, fontSize = if (result.conNewRecord == true) 15.sp else 14.sp, fontWeight = if (result.conNewRecord == true) FontWeight.SemiBold else FontWeight.Normal, textAlign = TextAlign.Center, modifier = Modifier.padding(horizontal = 28.dp, vertical = 4.dp).testTag("constelaciones_record"))
      }
      Text(con.LUCK_NOTE, color = TextSoft, fontSize = 14.sp, textAlign = TextAlign.Center, modifier = Modifier.padding(horizontal = 28.dp, vertical = 4.dp).testTag("constelaciones_luck"))
    }

    // Rastro de luz (pantalla final, docs/diseno-rastro-de-luz.md §6): "tu rastro" (cifra grande: las luces más largas que repetiste bien en el
    // rastro simple), "por modo" (al revés, el cielo gira y en marcha, cada uno con 3 rondas o más; el de menos aciertos marcado con TEXTO «el que
    // más te costó» y un truco), y la lectura en palabras. Sin recuadros ni percentiles; cada dato aparece UNA vez.
    val rasRounds = result.rasRounds
    if (rasRounds != null) {
      val trail = com.example.data.Trail
      val best = trail.bestTrail(result.rasBestLen)
      val rows = trail.modeRows(rasRounds, result.rasHits)
      Spacer(Modifier.height(14.dp))
      Text("Tu rastro", color = Clay.Sun, fontWeight = FontWeight.Bold, fontSize = 19.sp, fontFamily = AppFamily)
      Column(
        Modifier.fillMaxWidth().padding(horizontal = 28.dp).semantics { contentDescription = trail.spoken(result.rasBestLen) },
        horizontalAlignment = Alignment.CenterHorizontally
      ) {
        if (best != null) {
          Row(verticalAlignment = Alignment.Bottom) {
            Text("$best", color = Clay.Grape, fontWeight = FontWeight.Bold, fontSize = 52.sp, fontFamily = AppFamily)
            Text(
              if (best == 1) "luz" else "luces", color = Clay.Cream, fontSize = 20.sp, fontWeight = FontWeight.SemiBold,
              modifier = Modifier.padding(start = 8.dp, bottom = 10.dp)
            )
          }
          Text("las más largas que repetiste bien en el rastro simple", color = TextSoft, fontSize = 14.sp, textAlign = TextAlign.Center)
        } else {
          Text(trail.noTrailLine(rasRounds), color = Clay.Cream, fontSize = 15.sp, textAlign = TextAlign.Center)
        }
      }
      trail.unlockedLine(result.rasNewModes)?.let {
        Text(it, color = Clay.Lime, fontSize = 15.sp, fontWeight = FontWeight.Bold, textAlign = TextAlign.Center, modifier = Modifier.padding(horizontal = 28.dp, vertical = 6.dp))
      }
      if (rows.isNotEmpty()) {
        Spacer(Modifier.height(14.dp))
        Text("Por modo", color = Clay.Sun, fontWeight = FontWeight.Bold, fontSize = 19.sp, fontFamily = AppFamily)
        Column(Modifier.fillMaxWidth().padding(horizontal = 28.dp, vertical = 8.dp)) {
          rows.forEach { row -> TrailModeBar(row, Modifier.fillMaxWidth().padding(vertical = 3.dp)) }
        }
        trail.readingLines(rows).take(1).forEachIndexed { i, line ->
          Text(
            line, color = if (i == 0) Clay.Cream else TextSoft, fontSize = if (i == 0) 15.sp else 14.sp, textAlign = TextAlign.Center,
            modifier = Modifier.padding(horizontal = 28.dp, vertical = if (i == 0) 2.dp else 6.dp)
          )
        }
      }
      Text(trail.WORKING_MEMORY, color = TextSoft, fontSize = 14.sp, textAlign = TextAlign.Center, modifier = Modifier.padding(horizontal = 28.dp, vertical = 10.dp))
    }

    // ¿Verdad o disparate? (pantalla final, maqueta docs/previews/disparate.png): "tu lectura con comprensión" (cifra grande de
    // palabras por minuto), "qué te frena" (barras por tipo de frase, la más lenta marcada con TEXTO además del color), "tu
    // precisión" y "tu mejor racha". Sin recuadros; nada de perfil de sesgo.
    val svSeen = result.svSeenType
    if (svSeen != null) {
      val reading = com.example.data.Reading
      val wpm = reading.wpm(result.svWpm)
      val rows = reading.typeRows(result.svRtType)
      Spacer(Modifier.height(14.dp))
      Text("Tu lectura con comprensión", color = Clay.Sun, fontWeight = FontWeight.Bold, fontSize = 19.sp, fontFamily = AppFamily)
      if (wpm != null) {
        Column(
          Modifier.fillMaxWidth().padding(horizontal = 28.dp).semantics { contentDescription = reading.spoken(wpm) },
          horizontalAlignment = Alignment.CenterHorizontally
        ) {
          Text("$wpm", color = Clay.Grape, fontWeight = FontWeight.Bold, fontSize = 48.sp, fontFamily = AppFamily)
          Text("palabras por minuto", color = Clay.Cream, fontSize = 16.sp, fontWeight = FontWeight.SemiBold)
          Text("leyendo y decidiendo, en las frases que acertaste", color = TextSoft, fontSize = 14.sp, textAlign = TextAlign.Center)
        }
      } else {
        Text(
          "Juega un poco más para medir tu lectura: hacen falta unas 10 respuestas bien.",
          color = TextSoft, fontSize = 14.sp, textAlign = TextAlign.Center, modifier = Modifier.padding(horizontal = 28.dp, vertical = 4.dp)
        )
      }

      if (rows.size >= 2) {
        Spacer(Modifier.height(14.dp))
        Text("Qué te frena", color = Clay.Sun, fontWeight = FontWeight.Bold, fontSize = 19.sp, fontFamily = AppFamily)
        Column(Modifier.fillMaxWidth().padding(horizontal = 28.dp, vertical = 6.dp)) {
          rows.forEach { row -> ReadingBar(row, reading.barFraction(row.ms, rows), Modifier.fillMaxWidth().padding(vertical = 3.dp)) }
        }
        val slow = reading.slowText(rows)
        when {
          slow != null -> Text(
            text = slow,
            color = Clay.Cream, fontSize = 14.sp, textAlign = TextAlign.Center, modifier = Modifier.padding(horizontal = 28.dp)
          )
          reading.balanced(rows) -> Text(
            "Todas las frases te toman parecido. ¡Buen ritmo!",
            color = Clay.Cream, fontSize = 14.sp, textAlign = TextAlign.Center, modifier = Modifier.padding(horizontal = 28.dp)
          )
        }
      }

      reading.subtleLine(result.svSubtleHits, result.svSubtleSeen)?.let { line ->
        Spacer(Modifier.height(14.dp))
        Text("Disparates sutiles", color = Clay.Sun, fontWeight = FontWeight.Bold, fontSize = 19.sp, fontFamily = AppFamily)
        Text(line, color = Clay.Cream, fontSize = 17.sp, fontWeight = FontWeight.SemiBold, textAlign = TextAlign.Center, modifier = Modifier.padding(horizontal = 28.dp))
      }
      reading.streakLine(result.svBestStreak)?.let { line ->
        Spacer(Modifier.height(14.dp))
        Row(Modifier.fillMaxWidth().padding(horizontal = 28.dp), verticalAlignment = Alignment.CenterVertically) {
          Text("Tu mejor racha", color = Clay.Sun, fontWeight = FontWeight.Bold, fontSize = 19.sp, fontFamily = AppFamily, modifier = Modifier.weight(1f))
          Text(line, color = Clay.Lime, fontWeight = FontWeight.Bold, fontSize = 19.sp, fontFamily = AppFamily)
        }
      }
    }

    // Acoplamiento («muelle de acoplamiento», 10-oct): los módulos acoplados (el premio), los anillos completos, el récord y lo acoplado en toda la vida. Después van las medidas de siempre. Ver docs/diseno-acoplamiento.md §8.
    result.dockDocked?.let { docked ->
      Spacer(Modifier.height(14.dp))
      Column(
        horizontalAlignment = Alignment.CenterHorizontally,
        modifier = Modifier.semantics(mergeDescendants = true) { contentDescription = com.example.data.Acoplamiento.spoken(docked, result.dockRings, result.dockBest, result.dockNewRecord) }
      ) {
        com.example.data.Acoplamiento.dockedLine(docked)?.let { Text(it, color = Clay.Lime, fontWeight = FontWeight.Bold, fontSize = 20.sp, fontFamily = AppFamily) }
        com.example.data.Acoplamiento.ringsLine(result.dockRings)?.let {
          Text(it, color = Clay.Cream, fontSize = 14.sp, fontWeight = FontWeight.SemiBold, modifier = Modifier.padding(top = 6.dp))
        }
        com.example.data.Acoplamiento.recordLine(result.dockBest, result.dockNewRecord)?.let {
          Text(
            it,
            color = if (result.dockNewRecord == true) Clay.Sun else TextSoft,
            fontSize = if (result.dockNewRecord == true) 15.sp else 14.sp,
            fontWeight = if (result.dockNewRecord == true) FontWeight.Bold else FontWeight.SemiBold,
            modifier = Modifier.padding(top = 2.dp)
          )
        }
        com.example.data.Acoplamiento.totalLine(acoplamientoTotals)?.let {
          Text(it, color = TextSoft, fontSize = 14.sp, fontWeight = FontWeight.SemiBold, textAlign = TextAlign.Center, modifier = Modifier.padding(top = 2.dp, start = 24.dp, end = 24.dp))
        }
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
        text = if (result.rotationSpeedDps != null) "Cuanto más girada viene la pieza, más tardamos: es la huella de girarla en la mente (Cooper y Shepard, 1973). Tu giro sale de cuánto sube tu tiempo por cada grado, solo con tus aciertos. En Precisión, sin apuro de tiempo, la medida es más fiel."
        else "Tu giro mental se calcula con al menos 8 aciertos en 3 ángulos distintos y 7 de cada 10 respuestas bien: con más partidas lo verás.",
        color = TextSoft,
        fontSize = 15.sp,
        textAlign = TextAlign.Center,
        modifier = Modifier.padding(horizontal = 28.dp, vertical = 4.dp)
      )
    }

    // La estación de correo (pantalla final, docs/diseno-correo-estacion.md §7): «Tu memoria para lo pendiente» con el % en grande, los encargos cumplidos (por evento y por hora, con discos), los cancelados que no hiciste, las miradas al reloj
    // cerca de la hora, las cartas bien puestas, la etapa más alta y el récord. Cada dato aparece UNA vez; sin recuadros; sin encargos no hay % («—»).
    val mailGroup = result.mailGroup
    if (mailGroup != null) {
      val mail = com.example.data.Mail
      val ok = result.correctAnswers
      val all = result.totalTrials
      Spacer(Modifier.height(14.dp))
      Text("Tu memoria para lo pendiente", color = Clay.Sun, fontWeight = FontWeight.Bold, fontSize = 19.sp, fontFamily = AppFamily)
      Column(
        Modifier.fillMaxWidth().padding(horizontal = 28.dp).semantics { contentDescription = mail.spoken(ok, all) }.testTag("correo_headline"),
        horizontalAlignment = Alignment.CenterHorizontally
      ) {
        Text(mail.percent(ok, all)?.let { "$it %" } ?: "—", color = Clay.Grape, fontWeight = FontWeight.Bold, fontSize = 52.sp, fontFamily = AppFamily)
        Text(mail.detailLine(ok, all), color = TextSoft, fontSize = 15.sp, textAlign = TextAlign.Center)
      }
      Text(
        text = "Acordarte de hacer algo en el momento justo, sin que nada te avise del todo: como tomar un remedio o hacer una llamada.",
        color = TextSoft,
        fontSize = 15.sp,
        textAlign = TextAlign.Center,
        modifier = Modifier.padding(horizontal = 28.dp, vertical = 4.dp)
      )
      val evHits = result.mailEvHits ?: 0
      val evTotal = result.mailEvTotal ?: 0
      val timeHits = result.mailTimeHits ?: 0
      val timeTotal = result.mailTimeTotal ?: 0
      mail.eventLine(evHits, evTotal)?.let {
        Text(it, color = Clay.Cream, fontSize = 15.sp, fontWeight = FontWeight.SemiBold, modifier = Modifier.padding(top = 6.dp).testTag("correo_event"))
        FilledSlots(evHits, evTotal, Clay.Coral, Modifier.padding(top = 4.dp).semantics { contentDescription = "Por evento: $evHits de $evTotal" })
      }
      mail.timeLine(timeHits, timeTotal)?.let {
        Text(it, color = Clay.Cream, fontSize = 15.sp, fontWeight = FontWeight.SemiBold, modifier = Modifier.padding(top = 8.dp).testTag("correo_time"))
        FilledSlots(timeHits, timeTotal, Clay.Grape, Modifier.padding(top = 4.dp).semantics { contentDescription = "Por hora: $timeHits de $timeTotal" })
      }
      listOfNotNull(
        mail.cancelLine(result.mailCancels, result.mailCommissions),
        mail.clockLine(result.mailPeeksGood, result.mailPeeks),
        mail.cardsLine(result.mailRight),
        mail.groupLine(mailGroup)
      ).forEach {
        Text(it, color = TextSoft, fontSize = 14.sp, textAlign = TextAlign.Center, modifier = Modifier.padding(horizontal = 28.dp, vertical = 3.dp))
      }
      mail.recordLine(result.mailBest, result.mailNewRecord)?.let {
        Text(it, color = if (result.mailNewRecord == true) Clay.Sun else TextSoft, fontSize = if (result.mailNewRecord == true) 15.sp else 14.sp, fontWeight = if (result.mailNewRecord == true) FontWeight.SemiBold else FontWeight.Normal, textAlign = TextAlign.Center, modifier = Modifier.padding(horizontal = 28.dp, vertical = 4.dp).testTag("correo_record"))
      }
    }

    // Tinta o Palabra («Dos orillas»): «cuánto te frenó la palabra» (cifra grande) y, desde el nivel 3 (hay cambios de orilla), «cambiar de orilla te costó».
    // Ver docs/medidas-juegos-estrella.md. El consejo va aparte (ResultAdvice), antes o después según result_focus.
    result.interferenceMs?.let { ms ->
      Spacer(Modifier.height(14.dp))
      Text(com.example.data.DosOrillas.INTERFERENCE_TITLE, color = Clay.Sun, fontWeight = FontWeight.Bold, fontSize = 19.sp, fontFamily = AppFamily)
      Text(
        text = com.example.data.DosOrillas.cost(ms),
        color = Clay.Grape, fontWeight = FontWeight.Bold, fontSize = 40.sp, fontFamily = AppFamily,
        modifier = Modifier.semantics { contentDescription = "${com.example.data.DosOrillas.INTERFERENCE_TITLE}: ${com.example.data.DosOrillas.cost(ms)}" }
      )
      Text(
        text = com.example.data.DosOrillas.INTERFERENCE_LINE,
        color = TextSoft, fontSize = 15.sp, textAlign = TextAlign.Center, modifier = Modifier.padding(horizontal = 28.dp, vertical = 2.dp)
      )
    }
    result.switchCostMs?.let { ms ->
      Spacer(Modifier.height(14.dp))
      Text(com.example.data.DosOrillas.SWITCH_TITLE, color = Clay.Sun, fontWeight = FontWeight.Bold, fontSize = 19.sp, fontFamily = AppFamily)
      Text(
        text = com.example.data.DosOrillas.cost(ms),
        color = Clay.Sky, fontWeight = FontWeight.Bold, fontSize = 40.sp, fontFamily = AppFamily,
        modifier = Modifier.semantics { contentDescription = "${com.example.data.DosOrillas.SWITCH_TITLE}: ${com.example.data.DosOrillas.cost(ms)}" }
      )
      Text(
        text = com.example.data.DosOrillas.SWITCH_LINE,
        color = TextSoft, fontSize = 15.sp, textAlign = TextAlign.Center, modifier = Modifier.padding(horizontal = 28.dp, vertical = 2.dp)
      )
    }

    // Nota común a las medidas propias de los juegos estrella: son de esta partida, no un diagnóstico.
    val hasStarMeasure = listOf(
      result.pilLanePct, result.glanceMs, result.captureK, result.trackingCapacity, result.stopsTotal,
      result.rotationSpeedDps, result.rotationCurveMs,
      result.mailGroup, result.lexBandSeen, result.svSeenType, result.harvWords, result.intrSeenType,
      result.rasRounds, result.interferenceMs, result.switchCostMs, result.puntaSolo, result.cargaAlone, result.engrEtapa, result.conGroup
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
    }  // fin de measures

    // Lo que subió con la partida (la nota del modo y el cambio de etapa): va con «Lo que avancé».
    val progressNotes: @Composable ColumnScope.() -> Unit = {
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
    }  // fin de progressNotes

    // «Al terminar cada juego, ¿qué te sirve más ver primero?» (`result_focus`, Ajustes): SOLO cambia el orden. «Lo que avancé» pone primero las
    // medidas de la partida y lo que subió; «Un consejo para la próxima», el consejo concreto. Ningún dato se repite ni se oculta: el consejo sale
    // de las medidas (no se muestra dos veces) y los juegos sin consejo propio se ven igual con las dos opciones.
    val adviceTips = remember(result) { com.example.data.ResultAdvice.tips(result) }
    if (resultFocus == ResultFocus.CONSEJO) {
      AdviceBlock(adviceTips)
      measures()
      progressNotes()
    } else {
      measures()
      progressNotes()
      AdviceBlock(adviceTips)
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
    run {
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

/** Lucero REDONDO de arcilla (la «estrella» ganada, 4-oct: nada de estrellas con puntas): sol con borde tinta si se ganó, contorno tenue si no. */
@Composable
private fun ResultStar(earned: Boolean, modifier: Modifier = Modifier) {
  Canvas(modifier) {
    val center = Offset(size.width / 2f, size.height / 2f)
    val r = size.minDimension * 0.40f
    if (earned) {
      drawCircle(Clay.Sun.copy(alpha = 0.28f), radius = r * 1.22f, center = center)
      drawCircle(Clay.Ink, radius = r + size.minDimension * 0.06f, center = center)
      drawCircle(Clay.Sun, radius = r, center = center)
      // Brillo arriba a la izquierda.
      drawCircle(Color.White.copy(alpha = 0.55f), radius = size.minDimension * 0.08f, center = Offset(size.width * 0.38f, size.height * 0.36f))
    } else {
      drawCircle(Color.White.copy(alpha = 0.10f), radius = r, center = center)
      drawCircle(Color.White.copy(alpha = 0.30f), radius = r, center = center, style = Stroke(width = size.minDimension * 0.05f))
    }
  }
}

private val BurstColors = listOf(Clay.Sun, Clay.Coral, Clay.Sky, Clay.Grape, Clay.Lime, Color.White)

/** Lluvia de destellos REDONDOS que salen del centro y se apagan (celebración de 2-3 luceros). */
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
    drawCircle(color.copy(alpha = color.alpha * 0.35f), s * 0.9f, p)
    drawCircle(color, s * 0.45f, p)
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

// ---------- Satélites: "tu seguimiento" ----------

/**
 * Discos de arcilla (5; 4 en la captura de Rescate relámpago): se llenan en sol hasta la capacidad (3,4 = tres llenos y el cuarto al 40%).
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

// ---------- Satélites: el planeta con las luces que se ganaron ----------

/**
 * El planeta a oscuras (azul noche, con sus continentes tenues) y [lights] luces doradas repartidas sin amontonarse (la espiral del ángulo áureo: la primera al centro, las demás hacia afuera); el halo crece con la cantidad. Es el mismo dibujo
 * que la pantalla final del juego. Con 0 luces se ve apagado. La cantidad se lee por el número de arriba y por cuántas luces hay, no por el color.
 */
@Composable
private fun LitPlanet(lights: Int, modifier: Modifier = Modifier) {
  Canvas(modifier) {
    val c = Offset(size.width / 2f, size.height / 2f)
    val r = size.minDimension * 0.34f
    val lit = (lights / 30f).coerceIn(0f, 1f)
    if (lights > 0) {
      drawCircle(
        Brush.radialGradient(listOf(Color(0xFFFFD678).copy(alpha = 0.25f + 0.45f * lit), Color.Transparent), center = c, radius = r * 2.1f),
        r * 2.1f, c
      )
    }
    drawCircle(Clay.Ink, r + 3.dp.toPx(), c)
    drawCircle(Brush.radialGradient(listOf(Color(0xFF3B3F86), Color(0xFF22265E), Color(0xFF151843)), center = c + Offset(-r * 0.3f, -r * 0.34f), radius = r * 1.5f), r, c)
    val land = Color(0xFF7882D2).copy(alpha = 0.22f)
    for ((dx, dy, w, h) in listOf(listOf(-0.30f, -0.16f, 0.34f, 0.24f), listOf(0.26f, 0.20f, 0.28f, 0.19f), listOf(0.38f, -0.38f, 0.17f, 0.12f), listOf(-0.12f, 0.42f, 0.20f, 0.13f))) {
      drawOval(land, c + Offset((dx - w) * r, (dy - h) * r), Size(2f * w * r, 2f * h * r))
    }
    val n = com.example.data.Satelites.dots(lights)
    for (i in 0 until n) {
      val a = i * 2.39996f
      val d = Math.sqrt((i + 0.5) / n.coerceAtLeast(1).toDouble()).toFloat() * r * 0.78f
      val p = c + Offset((d * Math.cos(a.toDouble())).toFloat(), (d * Math.sin(a.toDouble())).toFloat())
      drawCircle(Color(0xFFFFDC8C).copy(alpha = 0.35f), r * 0.1f, p)
      drawCircle(Color(0xFFFFE7A8), r * 0.04f, p)
    }
  }
}

// ---------- Rescate relámpago: las cápsulas a salvo ----------

/**
 * Las cápsulas rescatadas, en filas de 8 (hasta 40), con las seis formas y colores del juego en orden (hexágono, gota, círculo, cuadrado, triángulo, rombo). Es el mismo dibujo que la nave de la pantalla final del juego. La cantidad se lee por el número de
 * arriba y por cuántas cápsulas hay; la forma lleva la identidad, no solo el color. Con 0 no dibuja nada.
 */
@Composable
private fun RescuedCapsules(count: Int, modifier: Modifier = Modifier) {
  if (count <= 0) return
  val cell = 30.dp
  val cols = count.coerceAtMost(com.example.data.Rescate.ROW)
  val rows = (count + com.example.data.Rescate.ROW - 1) / com.example.data.Rescate.ROW
  val colors = listOf(Color(0xFFA6E36B), Color(0xFF7FD8FF), Color(0xFFFF8A6B), Color(0xFFFFC94A), Color(0xFFB79BFF), Color(0xFFFFB3D1))
  Canvas(modifier.size(width = cell * cols, height = cell * rows + 3.dp)) {
    val step = cell.toPx()
    val r = 11.dp.toPx()
    val border = 2.dp.toPx()
    for (i in 0 until count) {
      val c = Offset((i % com.example.data.Rescate.ROW) * step + step / 2f, (i / com.example.data.Rescate.ROW) * step + step / 2f)
      val kind = i % 6
      drawPath(capsulePath(kind, c + Offset(0f, 2.dp.toPx()), r), Clay.Ink)
      drawPath(capsulePath(kind, c, r), colors[kind])
      drawPath(capsulePath(kind, c, r), Clay.Ink, style = Stroke(border, join = StrokeJoin.Round))
    }
  }
}

private fun capsulePath(kind: Int, c: Offset, r: Float): Path = Path().apply {
  when (kind) {
    0 -> {                                                                    // hexágono
      for (k in 0 until 6) {
        val a = Math.toRadians(30.0 + 60.0 * k)
        val p = Offset(c.x + (r * Math.cos(a)).toFloat(), c.y + (r * Math.sin(a)).toFloat())
        if (k == 0) moveTo(p.x, p.y) else lineTo(p.x, p.y)
      }
      close()
    }
    1 -> {                                                                    // gota
      moveTo(c.x, c.y - r)
      cubicTo(c.x + r * 0.95f, c.y - r * 0.05f, c.x + r * 0.9f, c.y + r, c.x, c.y + r)
      cubicTo(c.x - r * 0.9f, c.y + r, c.x - r * 0.95f, c.y - r * 0.05f, c.x, c.y - r)
      close()
    }
    2 -> addOval(androidx.compose.ui.geometry.Rect(c.x - r, c.y - r, c.x + r, c.y + r))   // círculo
    3 -> addRoundRect(androidx.compose.ui.geometry.RoundRect(c.x - r * 0.9f, c.y - r * 0.9f, c.x + r * 0.9f, c.y + r * 0.9f, r * 0.25f, r * 0.25f))   // cuadrado
    4 -> {                                                                    // triángulo
      moveTo(c.x, c.y - r)
      lineTo(c.x + r, c.y + r * 0.8f)
      lineTo(c.x - r, c.y + r * 0.8f)
      close()
    }
    else -> {                                                                 // rombo
      moveTo(c.x, c.y - r)
      lineTo(c.x + r * 0.85f, c.y)
      lineTo(c.x, c.y + r)
      lineTo(c.x - r * 0.85f, c.y)
      close()
    }
  }
}

// ---------- La estación de correo: filas de discos ----------

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

// ---------- Freno de Emergencia: "tu freno" ----------

/**
 * Velocímetro de arcilla de tres ZONAS con nombre (de izquierda, freno lento, a derecha, freno rápido): «pausado», «firme» y «ágil», un tercio del medio aro cada una, separadas por una marca, con la aguja en el PROMEDIO de las
 * últimas partidas. La zona se lee por su nombre, por su lugar y por la aguja (la de la persona va en negrita y subrayada), no por el color. Sin milisegundos.
 */
@Composable
private fun BrakeGauge(reading: com.example.data.BrakeReading, modifier: Modifier = Modifier) {
  val pos = com.example.data.Brake.gaugePosition(reading.averageMs)
  // de izquierda a derecha: pausado, firme, ágil (del tono más apagado al más vivo)
  val tints = listOf(Color(0xFF2B3680), Color(0xFF4A5AC0), Clay.Coral)
  val names = listOf(com.example.data.BrakeZone.PAUSADO, com.example.data.BrakeZone.FIRME, com.example.data.BrakeZone.AGIL)
  Column(modifier, horizontalAlignment = Alignment.CenterHorizontally) {
    Canvas(Modifier.size(width = 240.dp, height = 112.dp)) {
      val stroke = 18.dp.toPx()
      val r = size.width / 2f - stroke
      val c = Offset(size.width / 2f, size.height - 8.dp.toPx())
      val topLeft = c - Offset(r, r)
      val arc = Size(r * 2f, r * 2f)
      drawArc(Clay.Ink, 180f, 180f, useCenter = false, topLeft = topLeft + Offset(0f, 3.dp.toPx()), size = arc, style = Stroke(stroke + 6.dp.toPx()))
      for (i in 0 until 3) {
        val active = names[i] == reading.zone
        drawArc(if (active) tints[i] else tints[i].copy(alpha = 0.55f), 180f + 60f * i, 60f, useCenter = false, topLeft = topLeft, size = arc, style = Stroke(stroke))
      }
      // las marcas entre zonas (a 60° y 120° del arco)
      for (i in 1..2) {
        val a = Math.toRadians(180.0 + 60.0 * i)
        val from = c + Offset((kotlin.math.cos(a) * (r - stroke / 2f)).toFloat(), (kotlin.math.sin(a) * (r - stroke / 2f)).toFloat())
        val to = c + Offset((kotlin.math.cos(a) * (r + stroke / 2f)).toFloat(), (kotlin.math.sin(a) * (r + stroke / 2f)).toFloat())
        drawLine(Clay.Ink, from, to, 4.dp.toPx())
      }
      val ang = Math.toRadians(180.0 + 180.0 * pos)
      val tip = c + Offset((kotlin.math.cos(ang) * r * 0.95f).toFloat(), (kotlin.math.sin(ang) * r * 0.95f).toFloat())
      drawLine(Clay.Ink, c, tip, 7.dp.toPx(), androidx.compose.ui.graphics.StrokeCap.Round)
      drawLine(Clay.Cream, c, tip, 3.dp.toPx(), androidx.compose.ui.graphics.StrokeCap.Round)
      drawCircle(Clay.Ink, 8.dp.toPx(), c)
      drawCircle(Clay.Sun, 5.dp.toPx(), c)
    }
    Row(Modifier.width(240.dp), horizontalArrangement = Arrangement.SpaceBetween) {
      for (z in names) {
        val active = z == reading.zone
        Text(
          text = z.label,
          color = if (active) Clay.Cream else TextSoft,
          fontSize = 15.sp,
          fontWeight = if (active) FontWeight.Bold else FontWeight.Normal,
          textDecoration = if (active) androidx.compose.ui.text.style.TextDecoration.Underline else androidx.compose.ui.text.style.TextDecoration.None,
          textAlign = TextAlign.Center,
          modifier = Modifier.weight(1f)
        )
      }
    }
  }
}

// ---------- Aterrizaje Lunar: el final con sentido ----------

/** Lo que muestra el final de Aterrizaje Lunar (docs/diseno-aterrizaje.md §6), armado con la lectura de `data/Aterrizaje.kt` y `data/NumberLine.kt`. */
private fun aterrizajeModel(
  result: GamePlayResult,
  starMeasures: List<com.example.data.MeasurePoint>,
  totals: com.example.data.Aterrizaje.Totals?
): MeaningfulResultModel {
  val a = com.example.data.Aterrizaje
  val err = result.numlineErrorPct
  val trues = result.numlineTrue
  val givens = result.numlineGiven
  val reading = if (trues != null && givens != null) com.example.data.NumberLine.reading(trues, givens) else com.example.data.NumberLineReading.SIN_DATOS
  // la partida de hoy tal como se guarda como medida: así se compara con las ANTERIORES (misma marca de tiempo = no se cuenta dos veces)
  val today = com.example.data.MeasurePoint(result.timestamp, "numline", err ?: 0f, result.endRating?.coerceIn(0f, 1f) ?: -1f, result.timed)
  val previous = if (err != null) a.previousErrors(starMeasures, today) else emptyList()
  val progress = a.progress(err, previous)
  val headline = a.headline(progress.today)
  val dataLine = com.example.data.NumberLine.line(reading)
  val title = a.title(result.correctAnswers, result.totalTrials)
  return MeaningfulResultModel(
    title = title,
    boxTitle = a.BOX_TITLE,
    headline = headline,
    headlineUnit = a.unitLine(progress.today),
    dataLine = dataLine,
    summaryLine = a.summaryLine(result.numlineBullseyes, result.landStreak, result.landDomes),
    extraLine = a.baseLine(totals),
    progressTitle = a.PROGRESS_TITLE,
    chips = listOf(
      ResultChip("Hoy", a.chipValue(progress.today), highlight = true),
      ResultChip("Tu promedio", a.chipValue(progress.average)),
      ResultChip("Tu mejor", a.chipValue(progress.best))
    ),
    progressPhrase = progress.phrase,
    trickTitle = a.TRICK_TITLE,
    trick = com.example.data.NumberLine.trick(reading),
    whyTitle = a.WHY_TITLE,
    why = a.WHY,
    source = a.SOURCE,
    note = a.NOTE,
    spoken = a.spoken(title, headline, dataLine, progress.phrase)
  )
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

// ---------- Lluvia de meteoros: "tu vocabulario" ----------

/**
 * Una barra de "tu vocabulario": nombre del grupo a la izquierda, barra lila y el % a la derecha. El valor va siempre en
 * número (no depende del color). Un grupo con pocas palabras vistas queda con la barra vacía y "aún sin medir".
 */
@Composable
private fun VocabBar(label: String, percent: Int, lowest: Boolean, modifier: Modifier = Modifier) {
  Row(modifier, verticalAlignment = Alignment.CenterVertically) {
    Text(label, color = Clay.Cream, fontSize = 16.sp, fontWeight = FontWeight.SemiBold, modifier = Modifier.width(104.dp))
    Canvas(Modifier.weight(1f).height(12.dp)) {
      val r = androidx.compose.ui.geometry.CornerRadius(size.height / 2f)
      drawRoundRect(Color(0x33FFFFFF), cornerRadius = r)
      if (percent >= 0) drawRoundRect(Clay.Grape, size = androidx.compose.ui.geometry.Size(maxOf(size.height, size.width * percent / 100f), size.height), cornerRadius = r)
    }
    Column(Modifier.width(if (percent >= 0) 76.dp else 92.dp), horizontalAlignment = Alignment.End) {
      Text(
        text = if (percent >= 0) "$percent%" else "aún sin medir",
        color = if (percent >= 0) Clay.Cream else TextSoft,
        fontWeight = FontWeight.Bold,
        fontSize = if (percent >= 0) 17.sp else 14.sp,
        textAlign = TextAlign.End
      )
      if (lowest) Text("la más baja", color = Clay.Coral, fontWeight = FontWeight.Bold, fontSize = 12.sp, textAlign = TextAlign.End)
    }
  }
}


// ---------- ¿Verdad o disparate?: "qué te frena" ----------

/**
 * Una fila de "qué te frena": el tipo de frase a la izquierda, una barra (más larga = más tiempo) y el tiempo medio a la
 * derecha. La más lenta va en coral Y con el texto "la más lenta": se distingue sin depender del color.
 */
@Composable
private fun ReadingBar(row: com.example.data.Reading.TypeRow, fraction: Float, modifier: Modifier = Modifier) {
  val slowColor = if (row.slowest) Clay.Coral else Clay.Cream
  Row(modifier, verticalAlignment = Alignment.CenterVertically) {
    Text(row.label, color = Clay.Cream, fontSize = 15.sp, fontWeight = FontWeight.SemiBold, modifier = Modifier.width(124.dp))
    Canvas(Modifier.width(90.dp).height(11.dp)) {
      val r = androidx.compose.ui.geometry.CornerRadius(size.height / 2f)
      drawRoundRect(Color(0x33FFFFFF), cornerRadius = r)
      drawRoundRect(
        if (row.slowest) Clay.Coral else Clay.Grape,
        size = androidx.compose.ui.geometry.Size(maxOf(size.height, size.width * fraction), size.height),
        cornerRadius = r
      )
    }
    Spacer(Modifier.weight(1f))
    Column(horizontalAlignment = Alignment.End) {
      Text(
        text = com.example.data.Reading.seconds(row.ms),
        color = slowColor, fontSize = 15.sp, fontWeight = FontWeight.Bold, textAlign = TextAlign.End, softWrap = false
      )
      if (row.slowest) Text("la más lenta", color = Clay.Coral, fontSize = 14.sp, fontWeight = FontWeight.Bold, softWrap = false)
    }
  }
}


// ---------- Cosecha de palabras: "tu manera de buscar" y "tu ritmo" ----------

/**
 * Barra partida de "tu manera de buscar": a la izquierda los racimos (lila), a la derecha los saltos (celeste), con una marca de
 * tinta en el corte. Los porcentajes van siempre escritos debajo: el color solo acompaña.
 */
@Composable
private fun HarvestSplitBar(clusterPct: Int, modifier: Modifier = Modifier) {
  Canvas(modifier) {
    val r = androidx.compose.ui.geometry.CornerRadius(size.height / 2f)
    drawRoundRect(Clay.Sky, cornerRadius = r)
    val cut = size.width * clusterPct.coerceIn(0, 100) / 100f
    if (cut > 0f) drawRoundRect(Clay.Grape, size = androidx.compose.ui.geometry.Size(maxOf(cut, size.height), size.height), cornerRadius = r)
    if (clusterPct in 1..99) drawLine(Color(0xFF1A1240), Offset(cut, -2f), Offset(cut, size.height + 2f), 2.5f)
  }
}

/** Una fila de "tu ritmo": el tramo a la izquierda, una barra y las palabras a la derecha (el número siempre escrito). */
@Composable
private fun HarvestRhythmRow(label: String, count: Int, fraction: Float, color: Color, modifier: Modifier = Modifier) {
  Row(modifier, verticalAlignment = Alignment.CenterVertically) {
    Text(label, color = Clay.Cream, fontSize = 15.sp, fontWeight = FontWeight.SemiBold, modifier = Modifier.width(118.dp), softWrap = false)
    Canvas(Modifier.weight(1f).height(11.dp)) {
      val r = androidx.compose.ui.geometry.CornerRadius(size.height / 2f)
      drawRoundRect(Color(0x33FFFFFF), cornerRadius = r)
      drawRoundRect(color, size = androidx.compose.ui.geometry.Size(maxOf(size.height, size.width * fraction), size.height), cornerRadius = r)
    }
    Text(
      text = "$count ${com.example.data.Harvest.wordsLabel(count)}", color = Clay.Cream, fontSize = 15.sp, fontWeight = FontWeight.Bold,
      textAlign = TextAlign.End, softWrap = false, modifier = Modifier.width(104.dp)
    )
  }
}

// ---------- La estrella intrusa: "tu red de significados" ----------

/**
 * Una fila de "tu red de significados": la categoría a la izquierda, una barra de aciertos y "5 de 7" a la derecha. La más baja va en
 * coral Y con el texto "la más baja": se distingue sin depender del color.
 */
@Composable
private fun AtlasBar(row: com.example.data.Atlas.CategoryRow, modifier: Modifier = Modifier) {
  val color = if (row.lowest) Clay.Coral else Clay.Cream
  Row(modifier, verticalAlignment = Alignment.CenterVertically) {
    Text(row.label, color = Clay.Cream, fontSize = 15.sp, fontWeight = FontWeight.SemiBold, modifier = Modifier.width(124.dp))
    Canvas(Modifier.width(90.dp).height(11.dp)) {
      val r = androidx.compose.ui.geometry.CornerRadius(size.height / 2f)
      drawRoundRect(Color(0x33FFFFFF), cornerRadius = r)
      drawRoundRect(
        if (row.lowest) Clay.Coral else Clay.Grape,
        size = androidx.compose.ui.geometry.Size(maxOf(size.height, size.width * row.percent / 100f), size.height),
        cornerRadius = r
      )
    }
    Spacer(Modifier.weight(1f))
    Column(horizontalAlignment = Alignment.End) {
      Text("${row.hits} de ${row.seen}", color = color, fontSize = 15.sp, fontWeight = FontWeight.Bold, textAlign = TextAlign.End, softWrap = false)
      if (row.lowest) Text("la más baja", color = Clay.Coral, fontSize = 14.sp, fontWeight = FontWeight.Bold, softWrap = false)
    }
  }
}

// ---------- Rastro de luz: "por modo" ----------

/**
 * Una fila de "por modo": el modo a la izquierda, una barra de aciertos y "4 de 5" a la derecha. El de menos aciertos va en coral Y con el texto
 * «el que más te costó»: se distingue sin depender del color.
 */
@Composable
private fun TrailModeBar(row: com.example.data.Trail.ModeRow, modifier: Modifier = Modifier) {
  val color = if (row.lowest) Clay.Coral else Clay.Cream
  Row(modifier, verticalAlignment = Alignment.CenterVertically) {
    Column(Modifier.width(150.dp)) {
      Text(row.label, color = Clay.Cream, fontSize = 15.sp, fontWeight = FontWeight.SemiBold)
      if (row.lowest) Text("el que más te costó", color = Clay.Coral, fontSize = 14.sp, fontWeight = FontWeight.Bold, softWrap = false)
    }
    Canvas(Modifier.width(70.dp).height(11.dp)) {
      val r = androidx.compose.ui.geometry.CornerRadius(size.height / 2f)
      drawRoundRect(Color(0x33FFFFFF), cornerRadius = r)
      drawRoundRect(
        if (row.lowest) Clay.Coral else Clay.Grape,
        size = androidx.compose.ui.geometry.Size(maxOf(size.height, size.width * row.percent / 100f), size.height),
        cornerRadius = r
      )
    }
    Spacer(Modifier.weight(1f))
    Text("${row.hits} de ${row.rounds}", color = color, fontSize = 15.sp, fontWeight = FontWeight.Bold, textAlign = TextAlign.End, softWrap = false)
  }
}

// ---------- En la punta de la lengua: la lista de palabras con su lucero ----------

/** El color del lucero de cada palabra: dorado (sola), plateado (con pista), cobre (con las letras) y azul (te la mostró Nubi). */
private fun puntaColor(tier: com.example.data.PuntaTier): Color = when (tier) {
  com.example.data.PuntaTier.SOLO -> Color(0xFFFFC94A)
  com.example.data.PuntaTier.PISTA -> Color(0xFFD6DEF0)
  com.example.data.PuntaTier.LETRAS -> Color(0xFFE6965A)
  com.example.data.PuntaTier.VISTA -> Color(0xFF7FAAFF)
}

/** Un lucero: una estrella REDONDA (nunca con puntas) con su resplandor y un brillo arriba a la izquierda. */
@Composable
private fun PuntaLucero(tier: com.example.data.PuntaTier, modifier: Modifier = Modifier) {
  val color = puntaColor(tier)
  Canvas(modifier.size(26.dp)) {
    val c = Offset(size.width / 2f, size.height / 2f)
    val r = size.minDimension * 0.27f
    drawCircle(Brush.radialGradient(listOf(color.copy(alpha = 0.55f), Color.Transparent), center = c, radius = size.minDimension / 2f), radius = size.minDimension / 2f, center = c)
    drawCircle(color, radius = r, center = c)
    drawCircle(Color.White.copy(alpha = 0.85f), radius = r * 0.32f, center = Offset(c.x - r * 0.33f, c.y - r * 0.36f))
  }
}

/** Una fila de la lista: el lucero, la palabra y a la derecha cómo la encontraste, en texto. */
@Composable
private fun PuntaWordRow(entry: com.example.data.PuntaEntry, modifier: Modifier = Modifier) {
  Row(modifier.semantics { contentDescription = "${entry.word}, ${entry.tier.label}" }, verticalAlignment = Alignment.CenterVertically) {
    PuntaLucero(entry.tier)
    Text(entry.word, color = Clay.Cream, fontSize = 18.sp, fontWeight = FontWeight.Bold, fontFamily = AppFamily, modifier = Modifier.padding(start = 10.dp))
    Spacer(Modifier.weight(1f))
    Text(entry.tier.label, color = TextSoft, fontSize = 14.sp, textAlign = TextAlign.End, softWrap = false)
  }
}

/** «Un consejo para la próxima»: el consejo concreto de la partida (ver [com.example.data.ResultAdvice]); sin consejo, no hay bloque. */
@Composable
private fun ColumnScope.AdviceBlock(tips: List<String>) {
  if (tips.isEmpty()) return
  Spacer(Modifier.height(14.dp))
  Text("Un consejo para la próxima", color = Clay.Sun, fontWeight = FontWeight.Bold, fontSize = 19.sp, fontFamily = AppFamily, modifier = Modifier.testTag("result_advice"))
  tips.forEach { tip ->
    Text(tip, color = Clay.Cream, fontSize = 15.sp, textAlign = TextAlign.Center, modifier = Modifier.padding(horizontal = 28.dp, vertical = 3.dp))
  }
}
