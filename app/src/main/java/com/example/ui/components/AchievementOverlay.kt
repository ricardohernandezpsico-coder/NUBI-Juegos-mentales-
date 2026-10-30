package com.example.ui.components

import androidx.activity.compose.BackHandler
import androidx.compose.animation.core.Animatable
import androidx.compose.animation.core.LinearOutSlowInEasing
import androidx.compose.animation.core.spring
import androidx.compose.animation.core.tween
import androidx.compose.foundation.Canvas
import androidx.compose.foundation.background
import androidx.compose.foundation.clickable
import androidx.compose.foundation.interaction.MutableInteractionSource
import androidx.compose.foundation.layout.Box
import androidx.compose.foundation.layout.Column
import androidx.compose.foundation.rememberScrollState
import androidx.compose.foundation.verticalScroll
import androidx.compose.foundation.layout.Spacer
import androidx.compose.foundation.layout.fillMaxSize
import androidx.compose.foundation.layout.fillMaxWidth
import androidx.compose.foundation.layout.height
import androidx.compose.foundation.layout.padding
import androidx.compose.foundation.layout.size
import androidx.compose.material3.Text
import androidx.compose.runtime.Composable
import androidx.compose.runtime.LaunchedEffect
import androidx.compose.runtime.getValue
import androidx.compose.runtime.mutableStateOf
import androidx.compose.runtime.remember
import androidx.compose.runtime.rememberCoroutineScope
import androidx.compose.runtime.setValue
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.draw.alpha
import androidx.compose.ui.geometry.Offset
import androidx.compose.ui.graphics.Brush
import androidx.compose.ui.graphics.Color
import androidx.compose.ui.graphics.graphicsLayer
import androidx.compose.ui.graphics.lerp
import androidx.compose.ui.hapticfeedback.HapticFeedbackType
import androidx.compose.ui.platform.LocalContext
import androidx.compose.ui.platform.LocalHapticFeedback
import androidx.compose.ui.platform.testTag
import androidx.compose.ui.text.font.FontWeight
import androidx.compose.ui.text.style.TextAlign
import androidx.compose.ui.unit.dp
import androidx.compose.ui.unit.sp
import com.example.data.AchievementDef
import com.example.data.FamilyMedalState
import com.example.ui.theme.Clay
import com.example.ui.theme.ClayButton
import com.example.ui.theme.AppFamily
import kotlinx.coroutines.delay
import kotlinx.coroutines.launch
import kotlin.math.PI
import kotlin.math.cos
import kotlin.math.sin
import kotlin.random.Random

/** El alto de la medalla de plasticina en la celebración (Tarea 145): grande, para que se vea a distancia de teléfono; el número de su nivel va en su placa (`FamilyMedal` lo escribe desde 80 dp). */
private val MEDAL_HEIGHT = 160.dp

/**
 * Celebración de un logro conseguido ("¡Logro! Una semana"): más liviana que el ascenso de liga, pero con la
 * misma familia visual. La medalla cae con rebote sobre rayos de su color, destellos, vibración; debajo el nombre,
 * qué se hizo y cuántos logros llevas; "¡Genial!" y "Compartir" (tarjeta con la medalla, [ShareCard]).
 * Si hay varios logros seguidos se muestran de a uno. Atrás = "¡Genial!". Respeta "quitar animaciones".
 *
 * Desde la Tarea 185 (28-oct) la celebración ya no lleva la línea de la pieza de la isla ni su sonido: la isla salió de la app (Ricardo: arriba de Tú va SOLO Nubi).
 */
@Composable
fun AchievementOverlay(
  def: AchievementDef,
  unlockedCount: Int,
  totalCount: Int,
  streak: Int,
  onDismiss: () -> Unit,
  delayMs: Long = 1700L,
  soundEnabled: Boolean = true
) {
  val context = LocalContext.current
  val haptics = LocalHapticFeedback.current
  val scope = rememberCoroutineScope()
  val reduceMotion = rememberReduceMotion()
  var visible by remember(def.id) { mutableStateOf(reduceMotion || delayMs <= 0L) }
  LaunchedEffect(def.id) {
    if (!visible) {
      delay(delayMs)
      visible = true
    }
  }
  if (!visible) return

  BackHandler(onBack = onDismiss)

  val scrim = remember(def.id) { Animatable(if (reduceMotion) 1f else 0f) }
  val medal = remember(def.id) { Animatable(if (reduceMotion) 1f else 0f) }
  val burst = remember(def.id) { Animatable(if (reduceMotion) 1f else 0f) }
  val texts = remember(def.id) { Animatable(if (reduceMotion) 1f else 0f) }
  LaunchedEffect(def.id) {
    if (reduceMotion) return@LaunchedEffect
    scrim.animateTo(1f, tween(240))
    haptics.performHapticFeedback(HapticFeedbackType.LongPress)
    launch { burst.animateTo(1f, tween(1400, easing = LinearOutSlowInEasing)) }
    launch { medal.animateTo(1f, spring(dampingRatio = 0.45f, stiffness = 280f)) }
    delay(300)
    texts.animateTo(1f, tween(320))
  }
  val color = def.medalColor()
  val light = lerp(color, Color.White, 0.45f)
  val sparks = remember(def.id) {
    val rnd = Random(def.id.hashCode())
    List(18) { Triple(rnd.nextFloat() * 2f * PI.toFloat(), 0.45f + rnd.nextFloat() * 0.55f, 7f + rnd.nextFloat() * 10f) }
  }
  val sparkColors = listOf(light, Clay.Sun, Clay.Sky, Clay.Coral, Clay.Grape, Color.White)

  Box(
    modifier = Modifier
      .fillMaxSize()
      .alpha(scrim.value)
      .background(Color(0xF202030F))
      .clickable(interactionSource = remember { MutableInteractionSource() }, indication = null) {}
      .testTag("achievement_overlay"),
    contentAlignment = Alignment.Center
  ) {
    Column(
      modifier = Modifier.fillMaxWidth().verticalScroll(rememberScrollState()).padding(horizontal = 28.dp),          // si no cabe (pantalla chica o letra grande), se desliza
      horizontalAlignment = Alignment.CenterHorizontally
    ) {
      Text(
        text = "Medalla nueva",              // (Tarea 143, B3) antes «¡LOGRO!» en mayúsculas sostenidas con espaciado: DESIGN §1 no usa mayúsculas sostenidas, y la app las llama medallas
        fontFamily = com.example.ui.theme.TitleFamily,
        fontWeight = FontWeight.Bold,
        fontSize = 16.sp,
        color = com.example.ui.theme.NubiTokens.SunOnNight
      )
      Box(modifier = Modifier.size(270.dp), contentAlignment = Alignment.Center) {
        Canvas(Modifier.fillMaxSize()) {
          val c = center
          val reach = size.minDimension / 2f
          val a = medal.value.coerceIn(0f, 1f)
          // Un halo sol suave detrás de la medalla (como el de la farola de la Antena) y seis luceros redondos que suben (Tarea 148, P4: antes un abanico de rayos vectoriales que giraba y 18 destellos con puntas; la regla de símbolos pide luceros redondos)
          drawCircle(Brush.radialGradient(listOf(Clay.Sun.copy(alpha = 0.5f * a), color.copy(alpha = 0.22f * a), Color.Transparent), c, reach), reach, c)
          val b = burst.value
          if (b > 0f && b < 1f) {
            val rise = 1f - (1f - b) * (1f - b)
            sparks.take(6).forEachIndexed { i, (ang, speed, s) ->
              val x = c.x + cos(ang) * reach * 0.75f * speed
              val y = c.y + sin(ang) * reach * 0.35f - reach * 0.9f * rise * speed
              drawCircle(sparkColors[i % sparkColors.size].copy(alpha = 1f - b), s.dp.toPx() * 0.5f * (1f - b * 0.4f), Offset(x, y))
            }
          }
        }
        // La medalla de PLASTICINA de su familia (Tarea 145, ítem 14; antes una moneda vectorial con una llama, el último dibujo «de antes» de la celebración): la misma de «Tus medallas» en Avance y en el Perfil, grande, con el número de su nivel en la placa.
        // Los rayos, la luz y los destellos de arriba no cambian. Un logro viejo, de desempeño (ya no se otorgan: llegar aquí no pasa), conserva el dibujo de siempre para no romper la pantalla.
        val medalState = remember(def.id) { def.family?.let { FamilyMedalState(it, reached = def, next = null, progress = null) } }
        Box(
          modifier = Modifier
            .graphicsLayer {
              val k = medal.value
              scaleX = k
              scaleY = k
              alpha = k.coerceIn(0f, 1f)
              translationY = (1f - k.coerceIn(0f, 1f)) * -120f
            }
            .testTag("achievement_medal"),
          contentAlignment = Alignment.Center
        ) {
          if (medalState != null) FamilyMedal(medalState, MEDAL_HEIGHT)
          else AchievementMedal(def = def, unlocked = true, size = 170.dp)
        }
      }
      Column(
        modifier = Modifier.alpha(texts.value),
        horizontalAlignment = Alignment.CenterHorizontally
      ) {
        OnBoardText(def.title, color = com.example.ui.theme.NubiTokens.SunOnNight, fontSize = 36.sp, lineHeight = 40.sp, fontWeight = FontWeight.Bold, fontFamily = com.example.ui.theme.TitleFamily, textAlign = TextAlign.Center)          // sol con el contorno de los títulos de las pestañas (Tarea 148, P4)
        Text(
          text = def.description,
          fontFamily = AppFamily,
          fontWeight = FontWeight.SemiBold,
          fontSize = 18.sp,
          color = Color.White,
          textAlign = TextAlign.Center,
          modifier = Modifier.padding(top = 4.dp)
        )
        Text(
          text = "$unlockedCount de $totalCount medallas",
          fontFamily = AppFamily,
          fontSize = 16.sp,
          color = com.example.ui.theme.NubiTokens.OnNightDim,
          modifier = Modifier.padding(top = 10.dp)
        )
        Spacer(Modifier.height(28.dp))
        ClayButton(text = "¡Genial!", onClick = onDismiss, modifier = Modifier.testTag("btn_achievement_ok"))
        Spacer(Modifier.height(14.dp))
        ClayButton(
          text = "Compartir",
          color = Clay.Cream,
          glyph = Glyph.SHARE,
          onClick = {
            scope.launch {
              try {
                ShareCard.share(
                  context,
                  ShareCard.Content(
                    headline = def.title,
                    subtitle = def.description,
                    achievement = def,
                    stats = buildList {
                      add("$unlockedCount" to "medallas")
                      if (streak > 0) add("$streak" to if (streak == 1) "día de racha" else "días de racha")
                    }
                  ),
                  "¡Conseguí la medalla \"${def.title}\" en Nubi!"
                )
              } catch (e: Exception) {
                android.util.Log.e("AchievementOverlay", "No se pudo compartir el logro", e)
                com.example.diag.ErrorLog.record("COMPARTIR", "No se pudo compartir el logro.", e)
              }
            }
          }
        )
      }
    }
  }
}
