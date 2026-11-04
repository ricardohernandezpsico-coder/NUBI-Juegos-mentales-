package com.example

import androidx.compose.foundation.background
import androidx.compose.foundation.layout.Arrangement
import androidx.compose.foundation.layout.Box
import androidx.compose.foundation.layout.Column
import androidx.compose.foundation.layout.Row
import androidx.compose.foundation.layout.Spacer
import androidx.compose.foundation.layout.fillMaxSize
import androidx.compose.foundation.layout.fillMaxWidth
import androidx.compose.foundation.layout.height
import androidx.compose.foundation.layout.padding
import androidx.compose.foundation.layout.width
import androidx.compose.material3.Text
import androidx.compose.runtime.Composable
import androidx.compose.runtime.CompositionLocalProvider
import androidx.compose.runtime.getValue
import androidx.compose.runtime.mutableStateOf
import androidx.compose.runtime.setValue
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.graphics.Color
import androidx.compose.ui.platform.LocalDensity
import androidx.compose.ui.test.junit4.createComposeRule
import androidx.compose.ui.test.onNodeWithTag
import androidx.compose.ui.test.onRoot
import androidx.compose.ui.text.font.FontWeight
import androidx.compose.ui.unit.Density
import androidx.compose.ui.unit.dp
import androidx.compose.ui.unit.sp
import com.example.data.Achievements
import com.example.data.AchievementFamily
import com.example.data.HistoryRow
import com.example.data.SessionGame
import com.example.data.SessionSummary
import com.example.data.TimeTrialState
import com.example.data.TuStats
import com.example.data.Week
import com.example.data.computeAchievementStats
import com.example.data.familyState
import com.example.model.AppLanguage
import com.example.model.RankTier
import com.example.ui.components.BoardBackground
import com.example.ui.components.ClayPanel
import com.example.ui.components.HoyWeek
import com.example.ui.components.LocalBarInset
import com.example.ui.components.LocalReduceMotion
import com.example.ui.components.NeuroNavBar
import com.example.ui.components.WeeklyGoalLamps
import com.example.ui.screens.AvanceWeekLine
import com.example.ui.screens.HomeContent
import com.example.ui.screens.HomeTimeTrial
import com.example.ui.screens.HomeUi
import com.example.ui.screens.SessionSummaryScreen
import com.example.ui.screens.TuCuadros
import com.example.ui.screens.homeStops
import com.example.ui.screens.medalForTile
import com.example.ui.theme.Clay
import com.example.ui.theme.NeuroVidaTheme
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

/**
 * LAS CAPTURAS «antes | después» DE LA TAREA 223 (`docs/previews/tarea-223.png`, `tools/previews/tarea_223.py`): las llamitas de la semana y de la racha y el logotipo «Nubilus» arriba de Hoy, con los componentes REALES de la app (nada de prototipos).
 * Se graba DOS veces con el mismo código de prueba: con `T223_FASE=antes` antes de tocar la app y con `T223_FASE=despues` (el valor por omisión) cuando ya está; los PNG van a `app/src/test/screenshots/t223/<fase>_<nombre>.png` (fuera de git).
 *
 * No corre en las pruebas de siempre: solo al grabar (`./gradlew.bat :app:recordRoborazziDebug --tests "*Tarea223CapturasTest*"`).
 */
@RunWith(RobolectricTestRunner::class)
@GraphicsMode(GraphicsMode.Mode.NATIVE)
@Config(qualifiers = RobolectricDeviceQualifiers.Pixel8, sdk = [36])
class Tarea223CapturasTest {
  @get:Rule val rule = createComposeRule()

  private val fase = System.getenv("T223_FASE") ?: "despues"

  @Before fun setUp() {
    Assume.assumeTrue("solo al grabar capturas", System.getProperty("roborazzi.test.record") == "true")
    TestSupport.fastFrames()
  }

  private fun at(d: Int, h: Int = 16) = Calendar.getInstance().apply { set(2026, Calendar.OCTOBER, d, h, 0, 0); set(Calendar.MILLISECOND, 0) }.timeInMillis
  private val now = at(26)

  /** Una semana con 3 días jugados (lunes, martes y hoy) y hoy encendido. */
  private val lamps = Week.Lamps(listOf(true, true, false, false, false, false, true), todayIndex = 6)
  /** Una semana donde hoy todavía no se juega (lo que ve la persona antes de su primera partida del día). */
  private val lampsSinHoy = Week.Lamps(listOf(true, true, false, false, false, false, false), todayIndex = 6)
  private val stroop = HomeTimeTrial("stroop", "Tinta o Palabra", TimeTrialState.PENDIENTE, record = 41)

  private val sample = listOf(
    HistoryRow("calculo", "Carga exacta", "hoy", "en tu ruta", "70 % de las cargas, sin pista", now - 1),
    HistoryRow("bodega", "Bodega de carga", "hoy", "en tu ruta", "75 % de los objetos, al primer intento", now - 2),
    HistoryRow("stroop", "Tinta o Palabra", "hoy", "Contrarreloj", "38 aciertos", now - 3, timeTrial = true),
    HistoryRow("secuencia", "Rastro de luz", "ayer", "a tu medida", "7 luces seguidas", now - 4),
    HistoryRow("satelites", "Satélites", "ayer", "en Desafío", "3,2 satélites a la vez", now - 5),
    HistoryRow("acoplamiento", "Acoplamiento", "ayer", "Contrarreloj", "21 aciertos", now - 6, timeTrial = true)
  )

  private fun ui(completed: Int, l: Week.Lamps = lamps) = HomeUi(
    name = "Ricardo", now = now, tier = RankTier.fromRating(0), streak = 3, weeklyGoal = 4, lamps = l,
    stops = homeStops(listOf("bodega", "calculo", "intrusa"), completed, AppLanguage.SPANISH), pausedTitle = null, flightDay = false, hasBaseline = true, timeTrial = stroop, history = sample
  )

  private var fontState by mutableStateOf(1f)

  /** Un marco del tamaño de la pantalla (o de un recorte ampliado, con [zoom] mayor que 1 la densidad se multiplica por él: la pantalla simulada mide 1080 px de ancho, así que el marco ampliado no pasa de 1080 / (2,625 × zoom) dp) sobre el cielo de la app. */
  private fun frame(widthDp: Int, heightDp: Int, scale: Float, zoom: Float = 1f, content: @Composable () -> Unit) {
    fontState = scale
    rule.setContent {
      val base = LocalDensity.current
      CompositionLocalProvider(
        LocalDensity provides Density(base.density * zoom, fontState), LocalReduceMotion provides true,
        LocalBarInset provides (if (scale >= 1.3f) 95 else 88).dp
      ) {
        NeuroVidaTheme { Box(Modifier.width(widthDp.dp).height(heightDp.dp).background(Color(0xFF050823))) { BoardBackground(); content() } }
      }
    }
    rule.waitForIdle()
  }

  private fun save(name: String) { rule.onRoot().captureRoboImage(filePath = "src/test/screenshots/t223/${fase}_$name.png") }
  private fun meta(name: String, text: String) { java.io.File("src/test/screenshots/t223").mkdirs(); java.io.File("src/test/screenshots/t223/${fase}_$name.txt").writeText(text) }

  // ---------------------------------------------------------------- Hoy entera (con las barras del sistema simuladas por margen y la barra de pestañas)

  private fun hoy(name: String, w: Int = 393, h: Int = 873, scale: Float = 1f) {
    frame(w, h, scale) {
      Box(Modifier.fillMaxSize().padding(top = 24.dp, bottom = 24.dp)) {
        HomeContent(ui = ui(1), onPlay = {}, onResume = {}, onBaseline = {}, onTimeTrial = {}, onLocked = {})
        Box(Modifier.align(Alignment.BottomCenter)) { NeuroNavBar(current = AppTab.HOY, onSelect = {}) }
      }
    }
    val d = rule.density.density
    val card = rule.onNodeWithTag("home_card").fetchSemanticsNode().positionInRoot.y
    val bar = rule.onNodeWithTag("bottom_nav_bar").fetchSemanticsNode().positionInRoot.y
    val week = rule.onNodeWithTag("home_week").fetchSemanticsNode()
    meta("hoy_$name", "card_top_px=${card.toInt()}\nbar_top_px=${bar.toInt()}\nweek_top_px=${week.positionInRoot.y.toInt()}\nweek_bottom_px=${(week.positionInRoot.y + week.size.height).toInt()}\ndensity=$d\n")
    save("hoy_$name")
  }

  @Test fun hoy_393_10() = hoy("393_10")
  @Test fun hoy_360_10() = hoy("360_10", 360, 780)
  @Test fun hoy_393_20() = hoy("393_20", scale = 2f)

  // ---------------------------------------------------------------- recortes ampliados de la semana de Hoy (el componente real, con la racha al lado)

  private fun semanaZoom(name: String, l: Week.Lamps, streak: Int) {
    frame(300, 96, 1f, zoom = 1.35f) { Box(Modifier.fillMaxSize().padding(top = 4.dp)) { HoyWeek(l, streak) } }
    save("zoom_semana_$name")
  }

  @Test fun zoom_semana_con_hoy() = semanaZoom("con_hoy", lamps, 3)
  @Test fun zoom_semana_sin_hoy() = semanaZoom("sin_hoy", lampsSinHoy, 0)

  // ---------------------------------------------------------------- el resumen del día, Avance, la meta de Ajustes y los cuadros de Tú

  @Test fun resumen_393_10() {
    val summary = SessionSummary(
      games = listOf(SessionGame("bodega", "MEMORIA", played = true, gained = 9), SessionGame("freno", "ATENCION", played = true, gained = 12), SessionGame("aterrizaje", "RAZONAMIENTO", played = true, gained = 0)),
      areas = listOf(PlaceTestData.points("MEMORIA", 330, 9), PlaceTestData.points("ATENCION", 410, 12), PlaceTestData.points("RAZONAMIENTO", 90, 0))
    )
    frame(393, 873, 1f) { SessionSummaryScreen(summary, 3, lamps, AppLanguage.SPANISH, {}) }
    save("resumen_393_10")
  }

  @Test fun avance_semana() {
    frame(393, 64, 1f) { Column(Modifier.fillMaxSize().padding(start = 20.dp, top = 12.dp, end = 16.dp)) { AvanceWeekLine(lamps, 4, empty = false) } }
    save("avance_semana")
  }

  @Test fun ajustes_meta() {
    var goal by mutableStateOf(4)
    frame(393, 270, 1f) {
      Box(Modifier.fillMaxSize().padding(16.dp)) {
        ClayPanel(modifier = Modifier.fillMaxWidth(), verticalSpacing = 14.dp) {
          Column(verticalArrangement = Arrangement.spacedBy(6.dp)) {
            Text("Meta semanal de juego", fontSize = 16.sp, fontWeight = FontWeight.SemiBold)
            Text("$goal días por semana", fontSize = 18.sp, fontWeight = FontWeight.Black, color = Color(0xFF2B57B8))
            WeeklyGoalLamps(goal = goal, onGoal = { goal = it })
          }
        }
      }
    }
    save("ajustes_meta")
  }

  @Test fun tu_racha() {
    val medals = AchievementFamily.entries.map { Achievements.familyState(it, emptySet(), computeAchievementStats(emptyList(), emptyMap())) }
    val tiles = TuStats.tiles(21, 21, 41, Calendar.getInstance().apply { set(2026, Calendar.OCTOBER, 8) }.timeInMillis, now, 9, 15, 15, 17)
    frame(393, 520, 1f) { Box(Modifier.fillMaxSize().padding(16.dp)) { TuCuadros(tiles, streakLit = true, medal = medalForTile(medals)) } }
    save("tu_cuadros")
  }
}
