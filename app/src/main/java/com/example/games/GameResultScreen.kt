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
import androidx.compose.ui.draw.clip
import androidx.compose.foundation.background
import androidx.compose.foundation.border
import androidx.compose.runtime.CompositionLocalProvider
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
import com.example.model.GamePlayResult
import com.example.model.GameRankInfo
import com.example.model.GameRegistry
import com.example.ui.components.Glyph
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

private val TextSoft = com.example.ui.theme.NubiTokens.OnNightDim // secundario sobre el cielo nocturno (contraste > 7:1)

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
  /** «Efectos de sonido» de Ajustes: con false, el récord no suena (sí vibra, si la vibración está encendida). */
  soundEnabled: Boolean = true
) {
  val gameDef = GameRegistry.getById(result.gameId)
  val domainColor = gameDef?.domain?.color ?: Clay.Sky
  val context = LocalContext.current
  val reduceMotion = rememberReduceMotion()
  val haptics = com.example.ui.components.LocalHapticsEnabled.current
  val view = androidx.compose.ui.platform.LocalView.current

  BackHandler(onBack = onContinue)

  val usesMeaningfulResult = FinalModels.usesMeaningful(result)
  // El récord (Tarea 126; en Unity lo tapaba la cortina): una línea con su sello arriba, la vibración firme y el sonido «Record» de la paleta Madera. Es récord si el juego lo dice (su marca guardada) o si «Tu avance» dice «¡Tu mejor partida hasta ahora!».
  // El sello lo trae el modelo (Tarea 127): UNA vez por pantalla, con la cifra cuando la hay, y NUNCA en la primera partida de un juego (no hay nada que superar). Ver `FinalModels.build`.
  val recordSeal = remember(result, starMeasures) { if (usesMeaningfulResult) FinalModels.forGame(result, starMeasures)?.recordSeal else null }
  val isRecord = recordSeal != null

  val plaquePop = remember(result) { Animatable(if (reduceMotion) 1f else 0.82f) }
  val levelPop = remember(result) { Animatable(if (reduceMotion) 1f else 0f) }
  LaunchedEffect(result, isRecord) {
    // La placa entra con su rebote; el récord (la vibración firme y el sonido «Record») suena CUANDO TERMINA ese rebote, no a un tiempo fijo: en un arranque en frío la pantalla puede tardar en verse y el sonido no debe llegar antes que la placa (Tarea 127).
    if (!reduceMotion) plaquePop.animateTo(1f, spring(dampingRatio = 0.55f, stiffness = 380f)) else delay(150L)
    if (isRecord) {
      if (haptics && android.os.Build.VERSION.SDK_INT >= 30) view.performHapticFeedback(android.view.HapticFeedbackConstants.CONFIRM)
      if (soundEnabled) com.example.ui.components.RecordSound.play(context)
    }
    if (reduceMotion) return@LaunchedEffect
    delay(300L)
    levelPop.animateTo(1f, spring(dampingRatio = 0.5f, stiffness = 300f))
  }

  CompositionLocalProvider(com.example.ui.components.LocalResultScore provides com.example.ui.components.ResultScore.line(result)) {
  Column(
    modifier = modifier
      .fillMaxSize()
      .verticalScroll(rememberScrollState())
      .padding(horizontal = 24.dp)
      .padding(top = 28.dp, bottom = 32.dp),
    horizontalAlignment = Alignment.CenterHorizontally
  ) {
    // UN solo final (Tarea 126): abre con la placa del juego (su emblema, el nombre y el área) y directo con el final con sentido; sin el puntaje gigante, las tres estrellas ni el elogio. El puntaje se sigue guardando y va en «Ver el detalle de tu partida».
    ResultPlaque(result.gameId, gameDef?.title ?: "Partida terminada", gameDef?.domain?.displayName, domainColor, plaquePop.value)
    if (recordSeal != null) RecordLine(recordSeal)

    // Datos de la partida, sueltos en una línea. Los juegos con final con sentido (`MeaningfulResult`: Aterrizaje Lunar y, desde la Etapa 3, los que ya se llevaron a ese orden: ver `FinalModels.usesMeaningful`) ya dicen sus aciertos
    // en su propio recuadro («13 aterrizajes justos de 15», «7 de 9 veces…»): ahí la fila deja solo etapa y modo (Tarea 73). Los demás juegos quedan igual.
    Spacer(Modifier.height(18.dp))
    Row(verticalAlignment = Alignment.CenterVertically) {
      if (!usesMeaningfulResult) {
        Stat("${result.score}", "puntos")
        Dot()
        Stat("${result.correctAnswers}/${result.totalTrials}", "aciertos")
        Dot()
      }
      Stat(LevelTier.fromLevel(result.level).tierName, "etapa")
      Dot()
      Stat(if (result.timed) "Reto" else "Precisión", "modo")
    }

    // Las medidas de la partida (propias de cada juego): «Lo que avancé». Más abajo se decide si van antes o después del consejo.
    val measures: @Composable ColumnScope.() -> Unit = {
    // Final con sentido (Etapa 3, docs/finales-con-sentido.md): lo que hiciste con UN dato tuyo, tu avance, el truco y por qué importa. Lo que este juego mostraba antes NO se borra: va, SIN repetir lo que ya está arriba, detrás de «Ver el detalle de tu partida».
    if (result.gameId == "piloto" && usesMeaningfulResult) {
      Spacer(Modifier.height(18.dp))
      val pil = com.example.data.Piloto
      val model = remember(result, starMeasures) { FinalModels.piloto(result, starMeasures) }
      val pilRows = listOfNotNull(
        pil.laneLine(result.pilLanePct)?.let { "piloto_lane" to it },
        pil.wrongLine(result.pilFalse)?.let { "piloto_wrong" to it },
        pil.levelLine(result.pilSignalLevel)?.let { "piloto_level" to it },
        pil.streakLine(result.pilBestStreak)?.let { "piloto_streak" to it },
        pil.pointsLine(result.pilPoints)?.let { "piloto_points" to it }
      ).filter { it.second != model.dataLine && it.second != model.recordSeal }
      MeaningfulResult(model, detail = {
        DetailLine(pil.MEASURE_EXPLANATION, "piloto_explain", soft = false)
        pilRows.forEach { (tag, line) -> DetailLine(line, tag, strong = true) }
      })
    }

    // Final con sentido (Etapa 3, docs/finales-con-sentido.md): lo que hiciste con UN dato tuyo, tu avance, el truco y por qué importa. Lo que este juego mostraba antes NO se borra: va, SIN repetir lo que ya está arriba, detrás de «Ver el detalle de tu partida».
    if (result.gameId == "radar" && usesMeaningfulResult) {
      Spacer(Modifier.height(18.dp))
      val resc = com.example.data.Rescate
      val model = remember(result, starMeasures) { FinalModels.rescate(result, starMeasures) }
      val rescRows = listOfNotNull(
        resc.rescuedLine(result.rescRescued)?.let { "rescate_rescued" to it },
        resc.perfectLine(result.rescPerfect, result.rescRounds, result.rescBestStreak)?.let { "rescate_perfect" to it },
        resc.shortestLine(result.rescShortestMs)?.let { "rescate_shortest" to it },
        resc.tripsLine(result.rescTrips)?.let { "rescate_trips" to it },
        resc.recordLine(result.rescBest, result.rescNewRecord)?.let { "rescate_record" to it },
        resc.totalLine(rescateTotals)?.let { "rescate_total" to it }
      ).filter { it.second != model.dataLine && it.second != model.recordSeal }
      MeaningfulResult(model, detail = {
        RescuedCapsules(resc.dots(result.rescRescued), Modifier.padding(bottom = 8.dp).semantics { contentDescription = resc.spoken(result.rescRescued, result.captureK, result.glanceMs) })
        rescRows.forEach { (tag, line) -> DetailLine(line, tag, strong = true) }
        result.captureK?.let { k ->
          val line = resc.captureLine(k) ?: "Tu captura"
          Spacer(Modifier.height(12.dp))
          Text(text = line, color = Clay.Sun, fontWeight = FontWeight.Bold, fontSize = 18.sp, fontFamily = AppFamily)
          Spacer(Modifier.height(6.dp))
          TrackingSlots(k, slots = resc.CAPTURE_MAX, modifier = Modifier.semantics { contentDescription = line })
          DetailLine(resc.CAPTURE_EXPLANATION, "rescate_capture_explain", soft = false)
        }
        if (result.glanceMs != null) DetailLine(resc.glanceExplanation(result.glanceLoad), "rescate_glance_explain", soft = false)
      })
    }

    // Final con sentido (Etapa 3, docs/finales-con-sentido.md): lo que hiciste con UN dato tuyo, tu avance, el truco y por qué importa. Lo que este juego mostraba antes NO se borra: va, SIN repetir lo que ya está arriba, detrás de «Ver el detalle de tu partida».
    if (result.gameId == "satelites" && usesMeaningfulResult) {
      Spacer(Modifier.height(18.dp))
      val sat = com.example.data.Satelites
      val model = remember(result, starMeasures) { FinalModels.satelites(result, starMeasures) }
      val cap = result.trackingCapacity
      val targets = result.trackingTargets
      val satRows = listOfNotNull(
        sat.lightsLine(result.satLights)?.let { "satelites_lights" to it },
        sat.perfectLine(result.satPerfect, result.totalTrials, result.satBestStreak)?.let { "satelites_perfect" to it },
        sat.recordLine(result.satBest, result.satNewRecord)?.let { "satelites_record" to it },
        result.trackingSpeed?.let { "satelites_speed" to "Velocidad más alta superada: ${String.format(java.util.Locale("es"), "%.1f", it)}×" }
      ).filter { it.second != model.dataLine && it.second != model.recordSeal }
      MeaningfulResult(model, detail = {
        if (result.satLights != null) LitPlanet(sat.dots(result.satLights), Modifier.size(150.dp).semantics { contentDescription = sat.spoken(result.satLights, cap, targets) })
        satRows.forEach { (tag, line) -> DetailLine(line, tag, strong = true) }
        if (cap != null) {
          val capText = String.format(java.util.Locale("es"), "%.1f", cap)
          Spacer(Modifier.height(6.dp))
          TrackingSlots(cap, Modifier.semantics { contentDescription = "Sigues $capText satélites a la vez" })
          DetailLine(
            if (result.satLights != null)
              "Cuántos seguiste de verdad al mismo tiempo, sin contar los que aciertas por suerte. Cambia con la velocidad y con cuántos satélites hay que seguir: el juego los va sumando a medida que aciertas."
            else if (targets != null && targets < 3.5f)
              "Cuántos seguiste de verdad de los que había que seguir, sin contar los que aciertas por suerte. El juego suma satélites y velocidad a medida que aciertas: así se ve hasta dónde llegas."
            else
              "Cuántos seguiste de verdad al mismo tiempo, sin contar los que aciertas por suerte. A velocidad moderada, los adultos suelen seguir entre 3 y 4; más rápido, menos (Alvarez y Franconeri, 2007).",
            "satelites_explain", soft = false
          )
        }
      })
    }

    // Final con sentido (Etapa 3, docs/finales-con-sentido.md): lo que hiciste con UN dato tuyo, tu avance, el truco y por qué importa. Lo que este juego mostraba antes NO se borra: va, SIN repetir lo que ya está arriba, detrás de «Ver el detalle de tu partida».
    if (result.gameId == "freno" && usesMeaningfulResult) {
      Spacer(Modifier.height(18.dp))
      val brake = result.brakeMs
      val reading = com.example.data.Brake.reading(starMeasures, brake, result.timestamp)
      val model = remember(result, starMeasures) { FinalModels.freno(result, starMeasures) }
      MeaningfulResult(model, detail = {
        if (reading != null) {
          Text(
            text = reading.headline, color = Clay.Coral, fontWeight = FontWeight.Bold, fontSize = 18.sp, fontFamily = AppFamily, textAlign = TextAlign.Center,
            modifier = Modifier.padding(horizontal = 24.dp)
          )
          Spacer(Modifier.height(6.dp))
          BrakeGauge(reading, Modifier.semantics { contentDescription = reading.spoken })
        }
        result.brakeBestSsdMs?.let { DetailLine("Récord: frenaste con el alto a $it ms", "freno_record", strong = true) }
        DetailLine(
          if (brake != null) "Mide cuánto tardas en frenar una acción que ya ibas a hacer. Una sola partida trae pocos altos: por eso mostramos el promedio de varias."
          else "Esta vez no se pudo estimar tu freno: hacen falta al menos 6 altos y haber frenado entre 1 de cada 4 y 3 de cada 4. Lanza apenas se encienda la luz, sin esperar al ALTO: así la medida funciona." +
            if (reading != null) " Una sola partida trae pocos altos: por eso mostramos el promedio de varias." else "",
          "freno_explain", soft = false
        )
      })
    }

    // Aterrizaje Lunar (renovado el 10-oct; docs/diseno-aterrizaje.md §6): un final CON SENTIDO, con el componente reutilizable MeaningfulResult: lo que hiciste («11 % de la regla» de distancia promedio al lugar justo, con un dato tuyo y lo que pasó), tu avance SOLO contigo (hoy, tu promedio y tu mejor), un truco para la próxima
    // y, abajo y en chico, por qué importa con su fuente. Ya no hay «Tu línea» (el gráfico de puntos no le decía nada a la persona). Nunca percentiles ni comparación con otras personas.
    if (result.gameId == "aterrizaje" && usesMeaningfulResult) {
      Spacer(Modifier.height(18.dp))
      val model = remember(result, starMeasures, aterrizajeTotals) { aterrizajeModel(result, starMeasures, aterrizajeTotals) }
      MeaningfulResult(model)
    }

    // Final con sentido (Etapa 3, docs/finales-con-sentido.md): lo que hiciste con UN dato tuyo, tu avance, el truco y por qué importa. Lo que este juego mostraba antes NO se borra: va, SIN repetir lo que ya está arriba, detrás de «Ver el detalle de tu partida».
    if (result.gameId == "meteoros" && usesMeaningfulResult) {
      Spacer(Modifier.height(18.dp))
      val vocab = com.example.data.Vocabulary
      val groups = vocab.groupPercents(result.lexBandSeen, result.lexBandHits, result.lexFaSeen, result.lexFaHits)
      val phrase = vocab.phraseFor(groups)
      val model = remember(result, starMeasures) { FinalModels.meteoros(result, starMeasures) }
      MeaningfulResult(model, detail = {
        Text("Por grupo de palabras", color = Clay.Sun, fontWeight = FontWeight.Bold, fontSize = 19.sp, fontFamily = AppFamily)
        Column(Modifier.fillMaxWidth().padding(horizontal = 28.dp).semantics { contentDescription = "Tu vocabulario. $phrase" }, horizontalAlignment = Alignment.CenterHorizontally) {
          val lowGroup = vocab.lowestGroup(groups)
          vocab.GROUP_LABELS.forEachIndexed { i, label -> VocabBar(label, groups[i], i == lowGroup, Modifier.fillMaxWidth().padding(vertical = 4.dp)) }
        }
        vocab.recognitionSentence(result.lexRtCommonMs, result.lexRtRareMs)?.let { line ->
          Spacer(Modifier.height(14.dp))
          Text("Tu reconocimiento", color = Clay.Sun, fontWeight = FontWeight.Bold, fontSize = 19.sp, fontFamily = AppFamily)
          Text(line, color = Clay.Cream, fontSize = 15.sp, fontWeight = FontWeight.SemiBold, textAlign = TextAlign.Center, modifier = Modifier.padding(horizontal = 16.dp))
          Text("Es normal: las raras tardan más.", color = TextSoft, fontSize = 16.sp)
        }
        vocab.filterHeadline(result.lexFaSeen, result.lexFaHits)?.let { headline ->
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
              color = Clay.Lime, fontWeight = FontWeight.Bold, fontSize = 17.sp, fontFamily = AppFamily
            )
          }
          Text(words.joinToString(" · "), color = Clay.Cream, fontSize = 15.sp, textAlign = TextAlign.Center, modifier = Modifier.padding(horizontal = 28.dp, vertical = 2.dp))
        }
      })
    }

    // Final con sentido (Etapa 3, docs/finales-con-sentido.md): lo que hiciste con UN dato tuyo, tu avance, el truco y por qué importa. Lo que este juego mostraba antes NO se borra: va, SIN repetir lo que ya está arriba, detrás de «Ver el detalle de tu partida».
    if (result.gameId == "intrusa" && usesMeaningfulResult) {
      Spacer(Modifier.height(18.dp))
      val atlasLogic = com.example.data.Atlas
      val intrSeen = result.intrSeenType
      val model = remember(result, starMeasures) { FinalModels.intrusa(result, starMeasures) }
      val rows = atlasLogic.categoryRows(intrSeen, result.intrHitsType)
      MeaningfulResult(model, detail = {
        Text("Por tipo de cosa", color = Clay.Sun, fontWeight = FontWeight.Bold, fontSize = 19.sp, fontFamily = AppFamily)
        if (rows.isNotEmpty()) {
          Column(Modifier.fillMaxWidth().padding(horizontal = 28.dp, vertical = 8.dp).semantics { contentDescription = atlasLogic.spoken(intrSeen, result.intrHitsType) }) {
            rows.forEach { row -> AtlasBar(row, Modifier.fillMaxWidth().padding(vertical = 3.dp)) }
          }
        } else {
          DetailLine("Juega unas rondas más para ver tu red: cada tipo se mide con 3 rondas o más.", "intrusa_few")
        }
        atlasLogic.trapReading(intrSeen, result.intrHitsType)?.let {
          Spacer(Modifier.height(12.dp))
          Text("Las trampas", color = Clay.Sun, fontWeight = FontWeight.Bold, fontSize = 19.sp, fontFamily = AppFamily)
          atlasLogic.trapFooledLine(intrSeen, result.intrHitsType)?.takeIf { f -> f != model.dataLine }?.let { f ->
            Text(f, color = Clay.Coral, fontSize = 17.sp, fontWeight = FontWeight.Bold, textAlign = TextAlign.Center, modifier = Modifier.padding(horizontal = 28.dp, vertical = 2.dp))
          }
          Text(com.example.data.ResultAdvice.body(it), color = Clay.Cream, fontSize = 16.sp, textAlign = TextAlign.Center, modifier = Modifier.padding(horizontal = 28.dp, vertical = 2.dp))
        }
        atlasLogic.bonusLine(result.intrBonusSeen, result.intrBonusHits)?.takeIf { b -> model.dataLine?.endsWith(b) != true }?.let {
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
        atlasLogic.speedLine(result.intrRtMs)?.takeIf { sp -> sp != model.dataLine }?.let { DetailLine(it, "intrusa_speed") }
        if (rows.any { it.lowest }) DetailLine("Cada tipo se mide con 3 rondas o más. Sirve para ver dónde practicar, no para compararte.", "intrusa_note")
      })
    }

    // Final con sentido (Etapa 3, docs/finales-con-sentido.md): lo que hiciste con UN dato tuyo, tu avance, el truco y por qué importa. Lo que este juego mostraba antes NO se borra: va, SIN repetir lo que ya está arriba, detrás de «Ver el detalle de tu partida».
    if (result.gameId == "anagramas" && usesMeaningfulResult) {
      Spacer(Modifier.height(18.dp))
      val punta = com.example.data.Punta
      val puntaSolo = result.puntaSolo
      val puntaTotal = punta.total(puntaSolo, result.puntaPista, result.puntaLetras, result.puntaVista)
      val model = remember(result, starMeasures) { FinalModels.punta(result, starMeasures) }
      val puntaEntries = result.puntaWords.orEmpty()
      val puntaRows = listOfNotNull(
        punta.breakdown(puntaSolo, result.puntaPista, result.puntaLetras, result.puntaVista)?.let { "punta_breakdown" to it },
        punta.blueNote(result.puntaVista)?.let { "punta_blue_note" to it },
        punta.speedLine(result.puntaMs, puntaSolo)?.let { "punta_speed" to it }
      ).filter { it.second != model.dataLine && it.second != model.recordSeal }
      MeaningfulResult(model, detail = if (puntaEntries.isEmpty() && puntaRows.isEmpty()) null else ({
        if (puntaEntries.isNotEmpty()) {
          Column(Modifier.fillMaxWidth().padding(horizontal = 36.dp).testTag("punta_words")) {
            puntaEntries.forEach { PuntaWordRow(it, Modifier.fillMaxWidth().padding(vertical = 4.dp)) }
          }
        }
        puntaRows.forEach { (tag, line) -> DetailLine(line, tag) }
      }))
    }

    // Final con sentido (Etapa 3, docs/finales-con-sentido.md): lo que hiciste con UN dato tuyo, tu avance, el truco y por qué importa. Lo que este juego mostraba antes NO se borra: va, SIN repetir lo que ya está arriba, detrás de «Ver el detalle de tu partida».
    if (result.gameId == "calculo" && usesMeaningfulResult) {
      Spacer(Modifier.height(18.dp))
      val carga = com.example.data.Carga
      val cargaAlone = result.cargaAlone
      val cargaTotal = result.totalTrials
      val model = remember(result, starMeasures) { FinalModels.carga(result, starMeasures) }
      val cargaRows = listOfNotNull(
        carga.breakdown(cargaAlone, result.cargaHinted, cargaTotal)?.let { "carga_breakdown" to it },
        carga.shortLine(result.cargaShort, cargaAlone)?.let { "carga_short" to it },
        carga.speedLine(result.cargaMs, cargaAlone)?.let { "carga_speed" to it }
      ).filter { it.second != model.dataLine && it.second != model.recordSeal }
      MeaningfulResult(model, detail = if (cargaRows.isEmpty()) null else { { cargaRows.forEach { (tag, line) -> DetailLine(line, tag) } } })
    }

    // Final con sentido (Etapa 3, docs/finales-con-sentido.md): lo que hiciste con UN dato tuyo, tu avance, el truco y por qué importa. Lo que este juego mostraba antes NO se borra: va, SIN repetir lo que ya está arriba, detrás de «Ver el detalle de tu partida».
    if (result.gameId == "engranajes" && usesMeaningfulResult) {
      Spacer(Modifier.height(18.dp))
      val eng = com.example.data.Engranajes
      val engTotal = result.totalTrials
      val model = remember(result, starMeasures) { FinalModels.engranajes(result, starMeasures) }
      val engRows = listOfNotNull(
        eng.etapaLine(result.engrEtapa)?.let { "engranajes_etapa" to it },
        eng.launchLine(result.engrLaunches, result.engrOrbit)?.let { "engranajes_launch" to it },
        eng.hangarLine(result.engrLights)?.let { "engranajes_hangar" to it },
        eng.orbitLine(result.engrOrbit)?.let { "engranajes_orbit" to it },
        eng.paceLine(result.engrMs, engTotal)?.let { "engranajes_pace" to it }
      ).filter { it.second != model.dataLine && it.second != model.recordSeal }
      MeaningfulResult(model, detail = if (engRows.isEmpty()) null else { { engRows.forEach { (tag, line) -> DetailLine(line, tag) } } })
    }

    // Bodega de carga (pantalla final, docs/diseno-bodega-de-carga.md §7): «Tu bodega». «Encontraste X de N objetos al primer intento», la etapa más alta (de 5), la racha más larga, tu bodega
    // más grande de hoy (el pedido más grande sin errores), tu récord (con «¡Nuevo récord!» si lo superaste) y, con 3 o más objetos, el ritmo. Cada dato aparece UNA vez; sin recuadros.
    // Final con sentido (Etapa 3, docs/finales-con-sentido.md): lo que hiciste con UN dato tuyo, tu avance, el truco y por qué importa. Lo que este juego mostraba antes NO se borra: va, SIN repetir lo que ya está arriba, detrás de «Ver el detalle de tu partida».
    val bodGroup = result.bodGroup
    if (bodGroup != null) {
      Spacer(Modifier.height(18.dp))
      val bod = com.example.data.Bodega
      val bodTotal = result.totalTrials
      val model = remember(result, starMeasures) { FinalModels.bodega(result, starMeasures) }
      val bodRows = listOfNotNull(
        bod.groupLine(bodGroup)?.let { "bodega_group" to it },
        bod.streakLine(result.bodBestStreak, bodTotal)?.let { "bodega_streak" to it },
        bod.biggestLine(result.bodBiggest)?.let { "bodega_biggest" to it },
        bod.recordLine(result.bodBest, result.bodNewRecord)?.let { "bodega_record" to it },
        bod.paceLine(result.bodMs, bodTotal)?.let { "bodega_pace" to it }
      ).filter { it.second != model.dataLine && it.second != model.recordSeal }
      MeaningfulResult(model, detail = if (bodRows.isEmpty()) null else { { bodRows.forEach { (tag, line) -> DetailLine(line, tag) } } })
    }

    // Constelaciones (pantalla final, docs/diseno-constelaciones.md §5): «Tu memoria de lugar». «X de Y veces fuiste directo a una pareja que ya habías visto» con el % en grande, las parejas de memoria,
    // la racha de memoria más larga, la etapa más alta (de 6), tus más seguidos (con «¡Nuevo récord de seguidos!» si lo superaste) y la nota de que lo encontrado por suerte no cuenta. Sin oportunidades no hay % («—»). Sin recuadros.
    // Final con sentido (Etapa 3, docs/finales-con-sentido.md): lo que hiciste con UN dato tuyo, tu avance, el truco y por qué importa. Lo que este juego mostraba antes NO se borra: va, SIN repetir lo que ya está arriba, detrás de «Ver el detalle de tu partida».
    val conGroup = result.conGroup
    if (conGroup != null) {
      Spacer(Modifier.height(18.dp))
      val con = com.example.data.Constelaciones
      val model = remember(result, starMeasures) { FinalModels.constelaciones(result, starMeasures) }
      val conRows = listOfNotNull(
        con.memoryGroupsLine(result.conMemGroups, result.conGroups)?.let { "constelaciones_groups" to it },
        con.streakLine(result.conBestStreak)?.let { "constelaciones_streak" to it },
        con.groupLine(conGroup)?.let { "constelaciones_group" to it },
        con.recordLine(result.conBest, result.conNewRecord)?.let { "constelaciones_record" to it },
      ).filter { it.second != model.dataLine && it.second != model.recordSeal } + ("constelaciones_luck" to con.LUCK_NOTE)
      MeaningfulResult(model, detail = { conRows.forEach { (tag, line) -> DetailLine(line, tag) } })
    }

    // Rastro de luz (pantalla final, docs/diseno-rastro-de-luz.md §6): "tu rastro" (cifra grande: las luces más largas que repetiste bien en el
    // rastro simple), "por modo" (al revés, el cielo gira y en marcha, cada uno con 3 rondas o más; el de menos aciertos marcado con TEXTO «el que
    // más te costó» y un truco), y la lectura en palabras. Sin recuadros ni percentiles; cada dato aparece UNA vez.
    // Final con sentido (Etapa 3, docs/finales-con-sentido.md): lo que hiciste con UN dato tuyo, tu avance, el truco y por qué importa. Lo que este juego mostraba antes NO se borra: va, SIN repetir lo que ya está arriba, detrás de «Ver el detalle de tu partida».
    val rasRounds = result.rasRounds
    if (rasRounds != null) {
      Spacer(Modifier.height(18.dp))
      val trail = com.example.data.Trail
      val rows = trail.modeRows(rasRounds, result.rasHits)
      val model = remember(result, starMeasures) { FinalModels.rastro(result, starMeasures) }
      val reading = trail.readingLines(rows).take(1).filter { it != model.dataLine }
      MeaningfulResult(model, detail = {
        if (rows.isNotEmpty()) {
          Text("Por modo", color = Clay.Sun, fontWeight = FontWeight.Bold, fontSize = 19.sp, fontFamily = AppFamily)
          Column(Modifier.fillMaxWidth().padding(horizontal = 28.dp, vertical = 8.dp)) {
            rows.forEach { row -> TrailModeBar(row, Modifier.fillMaxWidth().padding(vertical = 3.dp)) }
          }
        }
        reading.forEach { DetailLine(it, "rastro_reading", strong = true) }
        DetailLine(trail.WORKING_MEMORY, "rastro_working_memory")
      })
    }

    // Final con sentido (Etapa 3, docs/finales-con-sentido.md): lo que hiciste con UN dato tuyo, tu avance, el truco y por qué importa. Lo que este juego mostraba antes NO se borra: va, SIN repetir lo que ya está arriba, detrás de «Ver el detalle de tu partida».
    if (result.gameId == "disparate" && usesMeaningfulResult) {
      Spacer(Modifier.height(18.dp))
      val reading = com.example.data.Reading
      val rows = reading.typeRows(result.svRtType)
      val model = remember(result, starMeasures) { FinalModels.disparate(result, starMeasures) }
      val slow = reading.slowText(rows)
      val subtle = reading.subtleLine(result.svSubtleHits, result.svSubtleSeen)
      val streak = reading.streakLine(result.svBestStreak)
      MeaningfulResult(model, detail = {
        if (rows.size >= 2) {
          Text("Qué te frena", color = Clay.Sun, fontWeight = FontWeight.Bold, fontSize = 19.sp, fontFamily = AppFamily)
          Column(Modifier.fillMaxWidth().padding(horizontal = 28.dp, vertical = 6.dp)) {
            rows.forEach { row -> ReadingBar(row, reading.barFraction(row.ms, rows), Modifier.fillMaxWidth().padding(vertical = 3.dp)) }
          }
          when {
            slow != null -> if (slow != model.dataLine) DetailLine(slow, "disparate_slow", soft = false)
            reading.balanced(rows) -> DetailLine("Todas las frases te toman parecido. ¡Buen ritmo!", "disparate_balanced", soft = false)
          }
        }
        subtle?.let {
          Spacer(Modifier.height(14.dp))
          Text("Disparates sutiles", color = Clay.Sun, fontWeight = FontWeight.Bold, fontSize = 19.sp, fontFamily = AppFamily)
          Text(it, color = Clay.Cream, fontSize = 17.sp, fontWeight = FontWeight.SemiBold, textAlign = TextAlign.Center, modifier = Modifier.padding(horizontal = 28.dp))
        }
        streak?.takeIf { "Más seguidos: ${result.svBestStreak}" != model.dataLine }?.let { line ->
          Spacer(Modifier.height(14.dp))
          Row(Modifier.fillMaxWidth().padding(horizontal = 28.dp), verticalAlignment = Alignment.CenterVertically) {
            Text("Más seguidos", color = Clay.Sun, fontWeight = FontWeight.Bold, fontSize = 19.sp, fontFamily = AppFamily, modifier = Modifier.weight(1f))
            Text(line, color = Clay.Lime, fontWeight = FontWeight.Bold, fontSize = 19.sp, fontFamily = AppFamily)
          }
        }
      })
    }

    // Final con sentido (Etapa 3, docs/finales-con-sentido.md): lo que hiciste con UN dato tuyo, tu avance, el truco y por qué importa. Lo que este juego mostraba antes NO se borra: va, SIN repetir lo que ya está arriba, detrás de «Ver el detalle de tu partida».
    if (result.gameId == "acoplamiento" && usesMeaningfulResult) {
      Spacer(Modifier.height(18.dp))
      val aco = com.example.data.Acoplamiento
      val model = remember(result, starMeasures) { FinalModels.acoplamiento(result, starMeasures) }
      val acoRows = listOfNotNull(
        aco.dockedLine(result.dockDocked)?.let { "acoplamiento_docked" to it },
        aco.ringsLine(result.dockRings)?.let { "acoplamiento_rings" to it },
        aco.recordLine(result.dockBest, result.dockNewRecord)?.let { "acoplamiento_record" to it },
        aco.totalLine(acoplamientoTotals)?.let { "acoplamiento_total" to it }
      ).filter { it.second != model.dataLine && it.second != model.recordSeal }
      MeaningfulResult(model, detail = {
        acoRows.forEach { (tag, line) -> DetailLine(line, tag) }
        result.rotationCurveMs?.let { curve ->
          Spacer(Modifier.height(8.dp))
          Text("Tu curva de giro", color = Clay.Cream, fontWeight = FontWeight.Bold, fontSize = 16.sp, fontFamily = AppFamily)
          RotationCurve(
            curve,
            Modifier.padding(horizontal = 40.dp).fillMaxWidth().height(110.dp)
              .semantics { contentDescription = "Tu curva de giro: tiempo de respuesta según cuán girada venía la pieza" }
          )
        }
        DetailLine(
          if (result.rotationSpeedDps != null) "Cuanto más girada viene la pieza, más tardamos: es la huella de girarla en la mente (Cooper y Shepard, 1973). Tu giro sale de cuánto sube tu tiempo por cada grado, solo con tus aciertos. En Precisión, sin apuro de tiempo, la medida es más fiel."
          else "Tu giro mental se calcula con al menos 8 aciertos en 3 ángulos distintos y 7 de cada 10 respuestas bien: con más partidas lo verás.",
          "acoplamiento_explain", soft = false
        )
      })
    }

    // La estación de correo (pantalla final, docs/diseno-correo-estacion.md §7): «Tu memoria para lo pendiente» con el % en grande, los encargos cumplidos (por evento y por hora, con discos), los cancelados que no hiciste, las miradas al reloj
    // cerca de la hora, las cartas bien puestas, la etapa más alta y el récord. Cada dato aparece UNA vez; sin recuadros; sin encargos no hay % («—»).
    // Final con sentido (Etapa 3, docs/finales-con-sentido.md): lo que hiciste con UN dato tuyo, tu avance, el truco y por qué importa. Lo que este juego mostraba antes NO se borra: va, SIN repetir lo que ya está arriba, detrás de «Ver el detalle de tu partida».
    val mailGroup = result.mailGroup
    if (mailGroup != null) {
      Spacer(Modifier.height(18.dp))
      val mail = com.example.data.Mail
      val model = remember(result, starMeasures) { FinalModels.correo(result, starMeasures) }
      val evHits = result.mailEvHits ?: 0
      val evTotal = result.mailEvTotal ?: 0
      val timeHits = result.mailTimeHits ?: 0
      val timeTotal = result.mailTimeTotal ?: 0
      val mailRows = listOfNotNull(
        mail.cancelLine(result.mailCancels, result.mailCommissions)?.let { "correo_cancel" to it },
        mail.clockLine(result.mailPeeksGood, result.mailPeeks)?.let { "correo_clock" to it },
        mail.cardsLine(result.mailRight)?.let { "correo_cards" to it },
        mail.groupLine(mailGroup)?.let { "correo_group" to it },
        mail.recordLine(result.mailBest, result.mailNewRecord)?.let { "correo_record" to it }
      ).filter { it.second != model.dataLine && it.second != model.recordSeal }
      MeaningfulResult(model, detail = {
        DetailLine("Acordarte de hacer algo en el momento justo, sin que nada te avise del todo: como tomar un remedio o hacer una llamada.", "correo_explain", soft = false)
        mail.eventLine(evHits, evTotal)?.let {
          Text(it, color = Clay.Cream, fontSize = 15.sp, fontWeight = FontWeight.SemiBold, modifier = Modifier.padding(top = 6.dp).testTag("correo_event"))
          FilledSlots(evHits, evTotal, Clay.Coral, Modifier.padding(top = 4.dp).semantics { contentDescription = "Por evento: $evHits de $evTotal" })
        }
        mail.timeLine(timeHits, timeTotal)?.let {
          Text(it, color = Clay.Cream, fontSize = 15.sp, fontWeight = FontWeight.SemiBold, modifier = Modifier.padding(top = 8.dp).testTag("correo_time"))
          FilledSlots(timeHits, timeTotal, Clay.Grape, Modifier.padding(top = 4.dp).semantics { contentDescription = "Por hora: $timeHits de $timeTotal" })
        }
        mailRows.forEach { (tag, line) -> DetailLine(line, tag) }
      })
    }

    // Final con sentido (Etapa 3, docs/finales-con-sentido.md): lo que hiciste con UN dato tuyo, tu avance, el truco y por qué importa. Lo que este juego mostraba antes NO se borra: va, SIN repetir lo que ya está arriba, detrás de «Ver el detalle de tu partida».
    if (result.gameId == "stroop" && usesMeaningfulResult) {
      Spacer(Modifier.height(18.dp))
      val model = remember(result, starMeasures) { FinalModels.tinta(result, starMeasures) }
      MeaningfulResult(model, detail = {
        DetailLine(com.example.data.DosOrillas.INTERFERENCE_LINE, "stroop_interference_line", soft = false)
        if (result.switchCostMs != null) DetailLine(com.example.data.DosOrillas.SWITCH_LINE, "stroop_switch_line", soft = false)
      })
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

    // Un solo orden: las medidas de la partida (cada juego con su final con sentido, que trae su propio «Truco para la próxima») y lo que subió.
    measures()
    progressNotes()

    if (rank != null) {
      Spacer(Modifier.height(18.dp))
      Row(verticalAlignment = Alignment.CenterVertically) {
        LeagueShield(tier = rank.tier, size = 40.dp, pips = rank.pipCount())
        Spacer(Modifier.width(10.dp))
        Column {
          Text(rank.label, color = Clay.Cream, fontWeight = FontWeight.Bold, fontSize = 17.sp)
          Text("${rank.rating} trofeos en este juego", color = TextSoft, fontSize = 16.sp)
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
      glyph = Glyph.RIGHT,
      modifier = Modifier.testTag("btn_result_continue")
    )
    run {
      Spacer(Modifier.height(14.dp))
      ClayButton(
        text = "Jugar de nuevo",
        onClick = onPlayAgain,
        color = Clay.Cream,
        modifier = Modifier.testTag("btn_result_replay")
      )
    }
  }
  }
}

/** La placa del juego, arriba del final (Tarea 126): su emblema (la ficha con el objeto del juego), el nombre y el área. Entra con un rebote corto (con «quitar animaciones», ya quieta). */
@Composable
private fun ResultPlaque(gameId: String, title: String, domainName: String?, domainColor: Color, pop: Float) {
  Column(Modifier.fillMaxWidth().testTag("result_plaque"), horizontalAlignment = Alignment.CenterHorizontally) {
    Box(Modifier.scale(pop)) { com.example.ui.components.GameEmblem(gameId, 112.dp) }
    Text(
      text = title, color = Clay.Cream, fontFamily = AppFamily, fontWeight = FontWeight.Bold, fontSize = 26.sp, textAlign = TextAlign.Center,
      modifier = Modifier.padding(top = 10.dp)
    )
    if (domainName != null) {
      Row(verticalAlignment = Alignment.CenterVertically, modifier = Modifier.padding(top = 4.dp)) {
        Canvas(Modifier.size(8.dp)) { drawCircle(domainColor) }
        Spacer(Modifier.width(6.dp))
        Text(domainName, color = TextSoft, fontSize = 16.sp, fontWeight = FontWeight.SemiBold)
      }
    }
  }
}

/** «¡Récord nuevo!» con su sello (un disco sol con el ✓ de plasticina: forma, no solo color). */
@Composable
private fun RecordLine(text: String) {
  Row(
    Modifier.padding(top = 14.dp).semantics(mergeDescendants = true) { contentDescription = text }.testTag("result_record"),
    verticalAlignment = Alignment.CenterVertically, horizontalArrangement = Arrangement.Center
  ) {
    Box(
      Modifier.size(34.dp).clip(androidx.compose.foundation.shape.CircleShape).background(Clay.Sun).border(2.dp, Clay.Ink, androidx.compose.foundation.shape.CircleShape),
      contentAlignment = Alignment.Center
    ) { com.example.ui.components.KitGlyph(Glyph.CHECK, size = 22.dp) }
    Spacer(Modifier.width(10.dp))
    Text(text, color = Clay.Sun, fontFamily = AppFamily, fontWeight = FontWeight.Bold, fontSize = 22.sp, textAlign = androidx.compose.ui.text.style.TextAlign.Center)
  }
}

@Composable
private fun Stat(value: String, label: String) {
  Column(horizontalAlignment = Alignment.CenterHorizontally, modifier = Modifier.padding(horizontal = 6.dp)) {
    Text(value, color = Clay.Cream, fontWeight = FontWeight.Bold, fontSize = 20.sp, fontFamily = AppFamily)
    Text(label, color = TextSoft, fontSize = 16.sp)
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
        fontSize = if (percent >= 0) 17.sp else 16.sp,
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
      if (row.slowest) Text("la más lenta", color = Clay.Coral, fontSize = 16.sp, fontWeight = FontWeight.Bold, softWrap = false)
    }
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
      if (row.lowest) Text("la más baja", color = Clay.Coral, fontSize = 16.sp, fontWeight = FontWeight.Bold, softWrap = false)
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
      if (row.lowest) Text("el que más te costó", color = Clay.Coral, fontSize = 16.sp, fontWeight = FontWeight.Bold, softWrap = false)
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
    Text(entry.tier.label, color = TextSoft, fontSize = 16.sp, textAlign = TextAlign.End, softWrap = false)
  }
}

/** Una línea del detalle de la partida (lo que cada juego mostraba antes de los finales con sentido): texto chico y suelto, sin recuadro; las que empiezan con «¡» (un récord nuevo) van en sol. */
@Composable
private fun DetailLine(text: String, tag: String, strong: Boolean = false, soft: Boolean = true) {
  val celebrate = text.startsWith("¡")
  Text(
    text = text,
    color = if (celebrate) Clay.Sun else if (strong) Clay.Cream else TextSoft,
    fontSize = if (celebrate || strong || !soft) 15.sp else 16.sp,
    fontWeight = if (celebrate) FontWeight.SemiBold else FontWeight.Normal,
    textAlign = TextAlign.Center,
    modifier = Modifier.padding(horizontal = 28.dp, vertical = 4.dp).testTag(tag)
  )
}
