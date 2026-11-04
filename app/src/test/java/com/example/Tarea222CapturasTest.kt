package com.example

import androidx.compose.foundation.Canvas
import androidx.compose.foundation.background
import androidx.compose.foundation.layout.Arrangement
import androidx.compose.foundation.layout.Box
import androidx.compose.foundation.layout.Column
import androidx.compose.foundation.layout.Row
import androidx.compose.foundation.layout.Spacer
import androidx.compose.foundation.layout.fillMaxSize
import androidx.compose.foundation.layout.fillMaxWidth
import androidx.compose.foundation.layout.height
import androidx.compose.foundation.layout.heightIn
import androidx.compose.foundation.layout.offset
import androidx.compose.foundation.layout.padding
import androidx.compose.foundation.layout.size
import androidx.compose.foundation.layout.width
import androidx.compose.runtime.Composable
import androidx.compose.runtime.CompositionLocalProvider
import androidx.compose.runtime.getValue
import androidx.compose.runtime.mutableStateOf
import androidx.compose.runtime.remember
import androidx.compose.runtime.setValue
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.draw.drawBehind
import androidx.compose.ui.geometry.Offset
import androidx.compose.ui.graphics.Brush
import androidx.compose.ui.graphics.Color
import androidx.compose.ui.graphics.Path
import androidx.compose.ui.graphics.StrokeCap
import androidx.compose.ui.graphics.StrokeJoin
import androidx.compose.ui.graphics.drawscope.DrawScope
import androidx.compose.ui.graphics.drawscope.Fill
import androidx.compose.ui.graphics.drawscope.Stroke
import androidx.compose.ui.graphics.drawscope.translate
import androidx.compose.ui.platform.LocalDensity
import androidx.compose.ui.platform.testTag
import androidx.compose.ui.semantics.SemanticsProperties
import androidx.compose.ui.semantics.clearAndSetSemantics
import androidx.compose.ui.semantics.contentDescription
import androidx.compose.ui.semantics.heading
import androidx.compose.ui.semantics.semantics
import androidx.compose.ui.test.junit4.createComposeRule
import androidx.compose.ui.test.onNodeWithTag
import androidx.compose.ui.test.onNodeWithContentDescription
import androidx.compose.ui.test.onRoot
import androidx.compose.ui.text.TextLayoutResult
import androidx.compose.ui.text.TextStyle
import androidx.compose.ui.text.font.FontWeight
import androidx.compose.ui.text.style.TextAlign
import androidx.compose.ui.unit.Density
import androidx.compose.ui.unit.Dp
import androidx.compose.ui.unit.dp
import androidx.compose.ui.unit.sp
import com.example.data.HistoryRow
import com.example.data.TimeTrialState
import com.example.data.Week
import com.example.model.AppLanguage
import com.example.model.RankTier
import com.example.ui.components.BoardBackground
import com.example.ui.components.LocalBarInset
import com.example.ui.components.LocalReduceMotion
import com.example.ui.components.NeuroNavBar
import com.example.ui.components.OnBoardText
import com.example.ui.screens.HomeContent
import com.example.ui.screens.HomeTimeTrial
import com.example.ui.screens.HomeUi
import com.example.ui.screens.dateText
import com.example.ui.screens.homeStops
import com.example.ui.theme.AppFamily
import com.example.ui.theme.Clay
import com.example.ui.theme.NeuroVidaTheme
import com.example.ui.theme.NubiTokens
import com.example.ui.theme.TitleFamily
import com.example.viewmodel.AppTab
import com.github.takahirom.roborazzi.RobolectricDeviceQualifiers
import com.github.takahirom.roborazzi.captureRoboImage
import org.junit.Assume
import org.junit.Before
import org.junit.Rule
import org.junit.Test
import org.junit.runner.RunWith
import org.robolectric.RobolectricTestRunner
import org.robolectric.annotation.Config
import org.robolectric.annotation.GraphicsMode
import java.util.Calendar
import kotlin.math.PI
import kotlin.math.cos
import kotlin.math.sin

/**
 * LA LÁMINA DE LA TAREA 222 (`docs/previews/tarea-222.png`, `tools/previews/tarea_222.py`): opciones para los farolitos de la semana de Hoy y para un logotipo «Nubilus» arriba de Hoy. **Solo prototipos**: todo el dibujo vive en ESTA prueba (Compose, `Canvas` y `Path`); no hay nada nuevo en las pantallas reales.
 * Ricardo no quiere los farolitos («unas llamitas o solcitos», «de un color destellante») y pide el logotipo «como cuando entras al Instagram, arriba dice Instagram con sus letras».
 *
 * - **Marcas de la semana** (misma caja de 36 dp y mismo aro lima de «hoy»): A «llamitas» (una llama de dos a tres lenguas; encendida naranja con el centro amarillo claro y un halo cálido; apagada más chica, gris lila de ceniza, solo el contorno, sin halo) y B «solcitos» (sol con rayos frente a un disco opaco sin rayos).
 * - **El logotipo**: «Nubilus» en Fredoka Bold (~30 dp; no crece con la letra: es un logotipo), relleno crema, contorno grueso de tinta y sombra dura; L1 solo las letras, L2 con una estrellita amarilla en el punto de la «i».
 * - **Frames de Hoy**: la propuesta es el Hoy de verdad con lo de arriba reemplazado (el logotipo sobre el saludo, la semana con las marcas nuevas) y todo lo demás empujado hacia abajo lo que mide el logotipo; la lámina los arma empalmando esos dos trozos (`tools/previews/tarea_222.py`).
 *
 * **Desde la Tarea 223** la gota del kit (`KitLlama`) ya no existe: la variante B se dibuja con la llama también junto a «3 días» (la lámina `docs/previews/tarea-222.png` se conserva como estaba) y la llamita real vive en `ui/components/Llamita.kt`.
 * No corre en las pruebas de siempre: solo al grabar (`./gradlew.bat :app:recordRoborazziDebug --tests "*Tarea222CapturasTest*"`); deja las capturas y los datos de empalme en `app/src/test/screenshots/t222/` (fuera de git).
 */
@RunWith(RobolectricTestRunner::class)
@GraphicsMode(GraphicsMode.Mode.NATIVE)
@Config(qualifiers = RobolectricDeviceQualifiers.Pixel8, sdk = [36])
class Tarea222CapturasTest {
  @get:Rule val rule = createComposeRule()

  @Before fun setUp() {
    Assume.assumeTrue("solo al grabar capturas", System.getProperty("roborazzi.test.record") == "true")
    TestSupport.fastFrames()
  }

  // ---------------------------------------------------------------- colores de los prototipos

  private val HoyCream = Color(0xFFF6EBD3)
  private val HoySoft = Color(0xFFD6D1F0)
  private val FlameOrange = Color(0xFFFF8A1F)       // naranja fuego
  private val FlameCore = Color(0xFFFFE680)         // el centro, amarillo claro
  private val FlameHalo = Color(0xFFFF9D3F)         // el halo cálido
  private val SunYellow = Clay.Sun                  // el sol del kit
  private val AshFill = Color(0xFF4A4682)           // gris lila de ceniza
  private val AshLine = Color(0xFFA29FD0)           // su contorno
  private val Shadow = Clay.Ink.copy(alpha = 0.8f)
  private val LogoShadow = Color(0xFF080522)       // la sombra dura del logotipo: tinta más oscura que el cielo, para que se vea el relieve

  // ---------------------------------------------------------------- la llama (A)

  /** El contorno de la llama en una caja de [w] × [h] px con la punta arriba: tres lenguas (una grande al centro y dos chicas a los lados) y la panza redonda. */
  private fun flamePath(w: Float, h: Float): Path {
    fun x(v: Float) = v * w
    fun y(v: Float) = v * h
    return Path().apply {
      moveTo(x(0.50f), y(1.00f))
      cubicTo(x(0.20f), y(1.00f), x(0.02f), y(0.84f), x(0.07f), y(0.60f))                   // panza de la izquierda
      cubicTo(x(0.09f), y(0.48f), x(0.15f), y(0.42f), x(0.20f), y(0.28f))                   // sube a la lengua chica de la izquierda
      cubicTo(x(0.27f), y(0.36f), x(0.30f), y(0.44f), x(0.33f), y(0.50f))                   // y baja al valle
      cubicTo(x(0.33f), y(0.32f), x(0.44f), y(0.16f), x(0.50f), y(0.00f))                   // sube a la lengua grande
      cubicTo(x(0.58f), y(0.16f), x(0.66f), y(0.28f), x(0.67f), y(0.40f))                   // y baja al valle de la derecha
      cubicTo(x(0.72f), y(0.34f), x(0.76f), y(0.26f), x(0.78f), y(0.16f))                   // sube a la lengua de la derecha
      cubicTo(x(0.88f), y(0.30f), x(0.97f), y(0.44f), x(0.93f), y(0.62f))                   // y baja a la panza de la derecha
      cubicTo(x(0.90f), y(0.84f), x(0.80f), y(1.00f), x(0.50f), y(1.00f))                   // cierra por abajo
      close()
    }
  }

  /** El centro de la llama: una gota más chica, anclada abajo. */
  private fun corePath(w: Float, h: Float): Path {
    fun x(v: Float) = v * w
    fun y(v: Float) = v * h
    return Path().apply {
      moveTo(x(0.50f), y(0.46f))
      cubicTo(x(0.60f), y(0.58f), x(0.78f), y(0.70f), x(0.74f), y(0.84f))
      cubicTo(x(0.70f), y(0.98f), x(0.58f), y(1.00f), x(0.50f), y(1.00f))
      cubicTo(x(0.42f), y(1.00f), x(0.30f), y(0.98f), x(0.26f), y(0.84f))
      cubicTo(x(0.22f), y(0.70f), x(0.40f), y(0.58f), x(0.50f), y(0.46f))
      close()
    }
  }

  /** La caja de 36 dp de una marca de la semana: la llama (encendida o apagada) y, si es hoy, el aro lima (la misma forma de «hoy» de siempre). */
  @Composable
  private fun Flame(lit: Boolean, today: Boolean, box: Dp = 36.dp) {
    Canvas(Modifier.size(box)) {
      val s = size.minDimension
      if (today) drawCircle(Clay.Lime, s / 2f - 1.5.dp.toPx(), center, style = Stroke(2.5.dp.toPx()))
      val bottom = s * 0.90f
      if (lit) {
        val fh = s * 0.80f; val fw = fh * 0.82f
        val left = (s - fw) / 2f; val top = bottom - fh
        // el halo cálido, detrás
        drawCircle(Brush.radialGradient(listOf(FlameHalo.copy(alpha = 0.55f), FlameHalo.copy(alpha = 0f)), center = Offset(s / 2f, top + fh * 0.62f), radius = s * 0.62f), s * 0.62f, Offset(s / 2f, top + fh * 0.62f))
        translate(left, top) {
          val body = flamePath(fw, fh)
          translate(2.dp.toPx(), 2.dp.toPx()) { drawPath(body, Shadow, style = Fill) }                                  // la sombra dura de plasticina
          drawPath(body, FlameOrange, style = Fill)
          translate(fw * 0.0f, fh * 0.0f) { drawPath(corePath(fw * 0.62f, fh * 0.52f).also { it.translate(Offset(fw * 0.19f, fh * 0.46f)) }, FlameCore, style = Fill) }
          drawPath(body, Clay.Ink, style = Stroke(2.5.dp.toPx(), join = StrokeJoin.Round, cap = StrokeCap.Round))
          drawCircle(Color.White.copy(alpha = 0.55f), 1.6.dp.toPx(), Offset(fw * 0.30f, fh * 0.60f))                       // el brillo de la arcilla
        }
      } else {
        val fh = s * 0.52f; val fw = fh * 0.82f
        val left = (s - fw) / 2f; val top = bottom - fh
        translate(left, top) {
          val body = flamePath(fw, fh)
          drawPath(body, AshFill.copy(alpha = 0.35f), style = Fill)
          drawPath(body, AshLine, style = Stroke(2.dp.toPx(), join = StrokeJoin.Round, cap = StrokeCap.Round))
        }
      }
    }
  }

  // ---------------------------------------------------------------- el solcito (B)

  @Composable
  private fun Sun(lit: Boolean, today: Boolean, box: Dp = 36.dp) {
    Canvas(Modifier.size(box)) {
      val s = size.minDimension
      val c = Offset(s / 2f, s / 2f)
      if (today) drawCircle(Clay.Lime, s / 2f - 1.5.dp.toPx(), center, style = Stroke(2.5.dp.toPx()))
      if (lit) {
        val r = s * 0.20f
        fun rays(drawScope: DrawScope, color: Color, width: Float) = with(drawScope) {
          for (i in 0 until 8) {
            val a = (i * 45.0) * PI / 180.0
            drawLine(color, Offset(c.x + (r * 1.50f * cos(a)).toFloat(), c.y + (r * 1.50f * sin(a)).toFloat()), Offset(c.x + (r * 1.85f * cos(a)).toFloat(), c.y + (r * 1.85f * sin(a)).toFloat()), width, StrokeCap.Round)
          }
        }
        translate(2.dp.toPx(), 2.dp.toPx()) { rays(this, Shadow, 4.6.dp.toPx()); drawCircle(Shadow, r) }                    // la sombra dura
        rays(this, Clay.Ink, 4.4.dp.toPx()); rays(this, SunYellow, 2.2.dp.toPx())                                              // rayos con contorno de tinta
        drawCircle(SunYellow, r, c)
        drawCircle(Clay.Ink, r, c, style = Stroke(2.5.dp.toPx()))
        drawCircle(Color.White.copy(alpha = 0.55f), 1.6.dp.toPx(), Offset(c.x - r * 0.38f, c.y - r * 0.4f))
      } else {
        val r = s * 0.17f
        drawCircle(AshFill.copy(alpha = 0.6f), r, c)
        drawCircle(AshLine, r, c, style = Stroke(2.dp.toPx()))
      }
    }
  }

  // ---------------------------------------------------------------- la semana (las dos opciones) y su racha

  private enum class Marks { LLAMITAS, SOLCITOS }

  /** Lo mismo que `HoyWeek` (misma `Column`, `FlowRow` y medidas), con las marcas nuevas. En A la racha de al lado usa la MISMA llama (un símbolo por concepto); en B queda la gota del kit. */
  @OptIn(androidx.compose.foundation.layout.ExperimentalLayoutApi::class)
  @Composable
  private fun ProtoWeek(marks: Marks, lamps: Week.Lamps, streak: Int) {
    Column(Modifier.fillMaxWidth().padding(start = 20.dp, end = 16.dp, top = 4.dp, bottom = 2.dp)) {
      androidx.compose.foundation.layout.FlowRow(horizontalArrangement = Arrangement.spacedBy(12.dp), verticalArrangement = Arrangement.spacedBy(2.dp)) {
        Row(Modifier.testTag("proto_week")) {
          lamps.played.forEachIndexed { i, on -> if (marks == Marks.LLAMITAS) Flame(on, i == lamps.todayIndex) else Sun(on, i == lamps.todayIndex) }
        }
        Row(Modifier.heightIn(min = 36.dp), verticalAlignment = Alignment.CenterVertically) {
          Flame(lit = streak > 0, today = false, box = 24.dp)
          Spacer(Modifier.width(if (marks == Marks.LLAMITAS) 2.dp else 6.dp))
          OnBoardText(if (streak == 1) "1 día" else "$streak días", color = NubiTokens.OnNight, fontSize = 16.sp, fontWeight = FontWeight.Bold, modifier = Modifier.testTag("proto_streak"))
        }
      }
    }
  }

  // ---------------------------------------------------------------- el logotipo (L1 y L2)

  /** Una estrellita de cinco puntas (el punto de la «i» de L2): amarilla, con contorno de tinta y sombra dura, como el resto del kit. */
  @Composable
  private fun StarMark(size: Dp) {
    Canvas(Modifier.size(size)) {
      val r = this.size.minDimension / 2f
      val c = center
      val path = Path().apply {
        for (i in 0 until 10) {
          val rad = if (i % 2 == 0) r * 0.86f else r * 0.40f
          val a = (-90.0 + i * 36.0) * PI / 180.0
          val px = c.x + (rad * cos(a)).toFloat(); val py = c.y + (rad * sin(a)).toFloat()
          if (i == 0) moveTo(px, py) else lineTo(px, py)
        }
        close()
      }
      translate(1.5.dp.toPx(), 1.5.dp.toPx()) { drawPath(path, Shadow, style = Fill) }
      drawPath(path, SunYellow, style = Fill)
      drawPath(path, Clay.Ink, style = Stroke(2.dp.toPx(), join = StrokeJoin.Round))
    }
  }

  /**
   * «Nubilus» como logotipo: Fredoka Bold de ~30 dp (no crece con la letra del sistema: es una marca, no un texto), relleno crema ([HoyCream]), contorno grueso de tinta y una sombra dura corrida (tinta). [star] = L2: la «i» sin punto («ı») y, donde iba el punto, una estrellita.
   * El lector lo lee como «Nubilus» y como encabezado.
   */
  @Composable
  private fun Logo(star: Boolean, modifier: Modifier = Modifier) {
    val density = LocalDensity.current
    val fontScale = density.fontScale
    val style = TextStyle(fontFamily = AppFamily, fontWeight = FontWeight.Bold, fontSize = (30f / fontScale).sp, lineHeight = (36f / fontScale).sp, letterSpacing = (1.2f / fontScale).sp)
    val text = if (star) "Nubılus" else "Nubilus"
    var layout by remember { mutableStateOf<TextLayoutResult?>(null) }
    val strokePx = with(density) { 5.dp.toPx() }
    Box(modifier.testTag("proto_logo").clearAndSetSemantics { contentDescription = "Nubilus"; heading() }) {
      androidx.compose.material3.Text(text, style = style.copy(color = LogoShadow, drawStyle = Stroke(strokePx, join = StrokeJoin.Round)), modifier = Modifier.offset(4.dp, 4.dp))
      androidx.compose.material3.Text(text, style = style.copy(color = LogoShadow), modifier = Modifier.offset(4.dp, 4.dp))
      androidx.compose.material3.Text(text, style = style.copy(color = Clay.Ink, drawStyle = Stroke(strokePx, join = StrokeJoin.Round)))
      androidx.compose.material3.Text(text, style = style.copy(color = HoyCream), onTextLayout = { layout = it })
      if (star) layout?.let { l ->
        // la estrella va donde iba el punto de la «i» (el cuarto carácter): centrada en su caja y sobre la altura de las minúsculas
        val box = l.getBoundingBox(3)
        val starPx = with(density) { 14.dp.toPx() }
        val baseline = l.getLineBaseline(0)
        val cx = (box.left + box.right) / 2f
        val cy = baseline - with(density) { 30.dp.toPx() } * 0.69f
        Box(Modifier.offset { androidx.compose.ui.unit.IntOffset((cx - starPx / 2f).toInt(), (cy - starPx / 2f).toInt()) }) { StarMark(14.dp) }
      }
    }
  }

  // ---------------------------------------------------------------- Hoy: el de ahora y la propuesta

  private fun at(d: Int, h: Int = 16) = Calendar.getInstance().apply { set(2026, Calendar.OCTOBER, d, h, 0, 0); set(Calendar.MILLISECOND, 0) }.timeInMillis
  private val now = at(26)
  /** Una semana con 3 días jugados (lunes, martes y hoy) y hoy encendido. */
  private val lamps = Week.Lamps(listOf(true, true, false, false, false, false, true), todayIndex = 6)
  private val stroop = HomeTimeTrial("stroop", "Tinta o Palabra", TimeTrialState.PENDIENTE, record = 41)

  private val sample = listOf(
    HistoryRow("calculo", "Carga exacta", "hoy", "en tu ruta", "70 % de las cargas, sin pista", now - 1),
    HistoryRow("bodega", "Bodega de carga", "hoy", "en tu ruta", "75 % de los objetos, al primer intento", now - 2),
    HistoryRow("stroop", "Tinta o Palabra", "hoy", "Contrarreloj", "38 aciertos", now - 3, timeTrial = true),
    HistoryRow("secuencia", "Rastro de luz", "ayer", "a tu medida", "7 luces seguidas", now - 4),
    HistoryRow("satelites", "Satélites", "ayer", "en Desafío", "3,2 satélites a la vez", now - 5),
    HistoryRow("acoplamiento", "Acoplamiento", "ayer", "Contrarreloj", "21 aciertos", now - 6, timeTrial = true)
  )

  private fun ui(completed: Int) = HomeUi(
    name = "Ricardo", now = now, tier = RankTier.fromRating(0), streak = 3, weeklyGoal = 4, lamps = lamps,
    stops = homeStops(listOf("bodega", "calculo", "intrusa"), completed, AppLanguage.SPANISH), pausedTitle = null, flightDay = false, hasBaseline = true, timeTrial = stroop, history = sample
  )

  private var fontState by mutableStateOf(1f)

  private fun frame(widthDp: Int, heightDp: Int, scale: Float, density3x: Boolean = false, content: @Composable () -> Unit) {
    fontState = scale
    rule.setContent {
      val base = LocalDensity.current
      CompositionLocalProvider(
        LocalDensity provides Density(base.density * (if (density3x) 1.8f else 1f), fontState), LocalReduceMotion provides true,
        LocalBarInset provides (if (scale >= 1.3f) 95 else 88).dp
      ) {
        NeuroVidaTheme { Box(Modifier.width(widthDp.dp).height(heightDp.dp).background(Color(0xFF050823))) { BoardBackground(); content() } }
      }
    }
    rule.waitForIdle()
  }

  private fun save(name: String) { rule.onRoot().captureRoboImage(filePath = "src/test/screenshots/t222/$name.png") }
  private fun meta(name: String, text: String) { java.io.File("src/test/screenshots/t222").mkdirs(); java.io.File("src/test/screenshots/t222/$name.txt").writeText(text) }

  /** El Hoy de ahora (la ruta a medias, con «Tus últimas partidas»), con las barras del sistema simuladas por margen y la barra de pestañas. Guarda dónde está la tarjeta de la ruta y dónde empieza la barra, para el empalme. */
  private fun actual(name: String, w: Int = 393, h: Int = 873, scale: Float = 1f) {
    frame(w, h, scale) {
      Box(Modifier.fillMaxSize().padding(top = 24.dp, bottom = 24.dp)) {
        HomeContent(ui = ui(1), onPlay = {}, onResume = {}, onBaseline = {}, onTimeTrial = {}, onLocked = {})
        Box(Modifier.align(Alignment.BottomCenter)) { NeuroNavBar(current = AppTab.HOY, onSelect = {}) }
      }
    }
    val card = rule.onNodeWithTag("home_card").fetchSemanticsNode().positionInRoot.y
    val bar = rule.onNodeWithTag("bottom_nav_bar").fetchSemanticsNode().positionInRoot.y
    meta("actual_$name", "card_top_px=${card.toInt()}\nbar_top_px=${bar.toInt()}\n")
    save("actual_$name")
  }

  /** Lo de arriba de Hoy con la propuesta: el logotipo, el saludo y la fecha (iguales que en Hoy) y la semana con las marcas nuevas; y una marca donde empieza la tarjeta de la ruta. Guarda dónde queda esa marca. */
  private fun propuesta(name: String, marks: Marks, star: Boolean, w: Int = 393, h: Int = 873, scale: Float = 1f) {
    frame(w, h, scale) {
      Box(Modifier.fillMaxSize().padding(top = 24.dp, bottom = 24.dp)) {
        Column(Modifier.fillMaxWidth()) {
          Box(Modifier.padding(start = 20.dp, top = LOGO_TOP.dp).height(LOGO_BOX.dp), contentAlignment = Alignment.CenterStart) { Logo(star) }
          Row(Modifier.fillMaxWidth().padding(start = 20.dp, end = 16.dp, top = GREETING_TOP.dp), verticalAlignment = Alignment.CenterVertically) {
            OnBoardText("Buenas tardes, Ricardo", color = HoyCream, fontSize = 22.sp, fontWeight = FontWeight.Bold, fontFamily = TitleFamily, lineHeight = 27.sp, modifier = Modifier.weight(1f).testTag("proto_greeting"))
          }
          OnBoardText("${dateText(now)} · ${Week.caption(lamps.daysPlayed, 4)}", color = HoySoft, fontSize = 16.sp, lineHeight = 20.sp, modifier = Modifier.padding(start = 20.dp, end = 16.dp, top = 2.dp).testTag("proto_date"))
          ProtoWeek(marks, lamps, 3)
          Spacer(Modifier.height(10.dp))
          Box(Modifier.testTag("proto_card_top"))
        }
      }
    }
    meta("propuesta_$name", "card_top_px=${rule.onNodeWithTag("proto_card_top").fetchSemanticsNode().positionInRoot.y.toInt()}\n")
    // que nada se tape: las CAJAS de línea del logotipo (con su sombra de 4 dp y su contorno de 2,5 dp) y del saludo casi se tocan, pero las letras del logotipo no tienen rabillos que bajen (la caja trae aire abajo) y el saludo empieza ~6 dp bajo su caja:
    // el hueco de verdad entre las letras se mide en píxeles en la lámina (`tools/previews/tarea_222.py`); aquí se exige que las cajas no se metan más de 4 dp una en la otra
    val d = rule.density.density
    val logo = rule.onNodeWithTag("proto_logo").fetchSemanticsNode(); val greeting = rule.onNodeWithTag("proto_greeting").fetchSemanticsNode()
    val gap = (greeting.positionInRoot.y - (logo.positionInRoot.y + logo.size.height)) / d - 4f - 2.5f
    val week = rule.onNodeWithTag("proto_week").fetchSemanticsNode(); val streak = rule.onNodeWithTag("proto_streak").fetchSemanticsNode()
    val sameRow = streak.positionInRoot.y < week.positionInRoot.y + week.size.height
    println("MEDIDA logotipo $name: hueco hasta el saludo %.1f dp (con sombra y contorno); la semana mide %.0f dp de ancho; «3 días» %s".format(gap, week.size.width / d, if (sameRow) "va en la MISMA fila" else "baja a una SEGUNDA fila"))
    save("propuesta_$name")
    org.junit.Assert.assertTrue("las cajas del logotipo y del saludo se meten una en otra ($gap dp)", gap >= -4f)
  }

  /** Lo que ocupa el logotipo arriba de Hoy: 6 dp de aire, 40 dp de logotipo y 2 dp hasta el saludo (antes, 28 dp de aire sobre el saludo). */
  private val LOGO_TOP = 6f
  private val LOGO_BOX = 40f
  private val GREETING_TOP = 2f
  private val CURRENT_TOP = 28f
  /** Cuánto baja todo Hoy con el logotipo. */
  private val DELTA = LOGO_TOP + LOGO_BOX + GREETING_TOP - CURRENT_TOP

  @Test fun actual_393_10() = actual("393_10")
  @Test fun actual_360_10() = actual("360_10", 360, 780)
  @Test fun actual_393_20() = actual("393_20", scale = 2f)
  @Test fun propuesta_A_L1_393_10() = propuesta("A_L1_393_10", Marks.LLAMITAS, false)
  @Test fun propuesta_A_L2_393_10() = propuesta("A_L2_393_10", Marks.LLAMITAS, true)
  @Test fun propuesta_B_L1_393_10() = propuesta("B_L1_393_10", Marks.SOLCITOS, false)
  @Test fun propuesta_A_L1_360_10() = propuesta("A_L1_360_10", Marks.LLAMITAS, false, 360, 780)
  @Test fun propuesta_A_L1_393_20() = propuesta("A_L1_393_20", Marks.LLAMITAS, false, scale = 2f)

  // ---------------------------------------------------------------- recortes ampliados (a 1,8× la densidad: la pantalla simulada mide 1080 px de ancho y más no cabe)

  private fun zoom(name: String, w: Int, h: Int, content: @Composable () -> Unit) {
    frame(w, h, 1f, density3x = true) { Box(Modifier.fillMaxSize().padding(8.dp)) { content() } }
    save("zoom_$name")
  }

  @Test fun zoom_marcas_A() = zoom("marcas_A", 224, 58) {
    Row(horizontalArrangement = Arrangement.spacedBy(4.dp), verticalAlignment = Alignment.CenterVertically) {
      Flame(lit = true, today = false); Flame(lit = false, today = false); Flame(lit = true, today = true)
      Spacer(Modifier.width(4.dp)); Flame(lit = true, today = false, box = 24.dp)
      OnBoardText("3 días", color = NubiTokens.OnNight, fontSize = 16.sp, fontWeight = FontWeight.Bold)
    }
  }
  @Test fun zoom_marcas_B() = zoom("marcas_B", 224, 58) {
    Row(horizontalArrangement = Arrangement.spacedBy(4.dp), verticalAlignment = Alignment.CenterVertically) {
      Sun(lit = true, today = false); Sun(lit = false, today = false); Sun(lit = true, today = true)
      Spacer(Modifier.width(4.dp)); Flame(lit = true, today = false, box = 24.dp)
      OnBoardText("3 días", color = NubiTokens.OnNight, fontSize = 16.sp, fontWeight = FontWeight.Bold)
    }
  }
  @Test fun zoom_logo_L1() = zoom("logo_L1", 160, 62) { Box(Modifier.padding(start = 4.dp, top = 4.dp)) { Logo(false) } }
  @Test fun zoom_logo_L2() = zoom("logo_L2", 160, 62) { Box(Modifier.padding(start = 4.dp, top = 4.dp)) { Logo(true) } }

  // ---------------------------------------------------------------- medidas

  private class Shown(val ui: HomeUi, val delta: Float)
  private var shown: androidx.compose.runtime.MutableState<Shown>? = null

  /**
   * Cuánto baja Hoy con el logotipo ([DELTA]) y si el título de «Tus últimas partidas» sigue asomando sin deslizar a 393 × 873 con letra 1,0 en los 14 estados de la §31 de `docs/contrarreloj.md`: se compara la posición del título SIN el logotipo y CON él (todo Hoy desplazado [DELTA] dp),
   * contra donde empieza la barra (710 dp). También comprueba lo del lector: «Nubilus» como encabezado.
   */
  @Test
  fun medidas_cuanto_baja_Hoy_y_si_el_titulo_de_la_tarjeta_sigue_asomando() {
    val jugado = stroop.copy(state = TimeTrialState.JUGADO, todayHits = 38)
    val marca = stroop.copy(state = TimeTrialState.MARCA_NUEVA, todayHits = 43, record = 43)
    val sinPlus = stroop.copy(gamesLeft = 3); val sinPlusJugado = jugado.copy(gamesLeft = 2); val sinPlusMarca = marca.copy(gamesLeft = 2)
    val sinPartidas = jugado.copy(gamesLeft = 0, tomorrowTitle = "¿Verdad o disparate?"); val sinPartidasMarca = marca.copy(gamesLeft = 0, tomorrowTitle = "¿Verdad o disparate?")
    fun u(completed: Int, t: HomeTimeTrial, paused: String? = null) = ui(completed).copy(timeTrial = t, pausedTitle = paused, stops = homeStops(listOf("bodega", "calculo", "intrusa"), completed, AppLanguage.SPANISH))
    val states = listOf(
      "primera hora" to u(0, stroop), "a mitad de ruta" to u(1, stroop), "ruta completa" to u(3, stroop), "jugado" to u(3, jugado), "marca nueva" to u(3, marca), "marca nueva con la ruta a medias" to u(1, marca),
      "partida en pausa" to u(1, stroop, "Carga exacta"), "sin Plus, ruta a medias" to u(1, sinPlus), "sin Plus, ruta completa" to u(3, sinPlus), "sin Plus, jugado" to u(3, sinPlusJugado), "sin Plus, marca nueva" to u(3, sinPlusMarca),
      "sin partidas" to u(3, sinPartidas), "sin partidas con marca nueva" to u(3, sinPartidasMarca), "sin partidas, ruta a medias" to u(1, sinPartidas)
    )
    val holder = mutableStateOf(Shown(states.first().second, 0f)).also { shown = it }
    frame(393, 873, 1f) {
      val a = holder.value
      HomeContent(ui = a.ui, onPlay = {}, onResume = {}, onBaseline = {}, onTimeTrial = {}, onLocked = {}, modifier = Modifier.padding(top = (24f + a.delta).dp, bottom = 163.dp))
    }
    val density = rule.density.density
    fun titleBottom(): Float { val n = rule.onNodeWithTag("home_history_title").fetchSemanticsNode(); return (n.positionInRoot.y + n.size.height) / density }
    val bar = 873f - 163f
    val lost = mutableListOf<String>()
    val rows = mutableListOf<String>()
    for ((name, u) in states) {
      holder.value = Shown(u, 0f); rule.waitForIdle(); val before = titleBottom()
      holder.value = Shown(u, DELTA); rule.waitForIdle(); val after = titleBottom()
      val peeks = after <= bar + 1f
      if (!peeks) lost += name
      rows += "%s: %.0f → %.0f (barra %.0f) %s".format(name, before, after, bar, if (peeks) "asoma" else "YA NO ASOMA")
    }
    println("MEDIDA logotipo: Hoy baja %.0f dp (logotipo %.0f + aire %.0f + %.0f hasta el saludo − %.0f de antes)".format(DELTA, LOGO_BOX, LOGO_TOP, GREETING_TOP, CURRENT_TOP))
    rows.forEach { println("MEDIDA título $it") }
    println("MEDIDA título que dejan de asomar: ${if (lost.isEmpty()) "ninguno" else lost.joinToString(" | ")}")
  }

  /** «Nubilus» es un encabezado y se lee como «Nubilus» (las dos variantes). */
  @Test
  fun el_logotipo_se_lee_como_Nubilus_y_es_un_encabezado() {
    val star = mutableStateOf(false)
    frame(393, 120, 1f) { Box(Modifier.padding(24.dp)) { Logo(star.value) } }
    for (s in listOf(false, true)) {
      star.value = s; rule.waitForIdle()
      val n = rule.onNodeWithContentDescription("Nubilus").fetchSemanticsNode()
      org.junit.Assert.assertTrue("«Nubilus» debe ser un encabezado (estrella: $s)", n.config.contains(SemanticsProperties.Heading))
    }
  }
}
