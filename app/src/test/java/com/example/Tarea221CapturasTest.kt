package com.example

import androidx.compose.foundation.background
import androidx.compose.foundation.layout.Box
import androidx.compose.foundation.layout.fillMaxSize
import androidx.compose.foundation.layout.height
import androidx.compose.foundation.layout.padding
import androidx.compose.foundation.layout.width
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
import androidx.compose.ui.test.performScrollTo
import androidx.compose.ui.test.performScrollToIndex
import androidx.compose.ui.unit.Density
import androidx.compose.ui.unit.dp
import com.example.data.HistoryRow
import com.example.data.TimeTrialState
import com.example.data.Week
import com.example.model.AppLanguage
import com.example.model.RankTier
import com.example.ui.components.BoardBackground
import com.example.ui.components.LocalBarInset
import com.example.ui.components.LocalReduceMotion
import com.example.ui.components.NeuroNavBar
import com.example.ui.screens.HomeContent
import com.example.ui.screens.HomeTimeTrial
import com.example.ui.screens.HomeUi
import com.example.ui.screens.homeStops
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
 * La lámina de la Tarea 221 (`docs/previews/tarea-221.png`, `tools/previews/tarea_221.py`): Hoy con «Tus últimas partidas» en su primera pantalla (ruta a medias y ruta completa), deslizada hasta la tarjeta, con la lista deslizada hasta el final, con letra 1,3 y a 360 dp.
 * No corre en las pruebas de siempre: solo al grabar (`./gradlew.bat :app:recordRoborazziDebug --tests "*Tarea221CapturasTest*"`); las deja en `app/src/test/screenshots/t221/` (fuera de git).
 */
@RunWith(RobolectricTestRunner::class)
@GraphicsMode(GraphicsMode.Mode.NATIVE)
@Config(qualifiers = RobolectricDeviceQualifiers.Pixel8, sdk = [36])
class Tarea221CapturasTest {
  @get:Rule val rule = createComposeRule()

  @Before fun setUp() {
    Assume.assumeTrue("solo al grabar capturas", System.getProperty("roborazzi.test.record") == "true")
    TestSupport.fastFrames()
  }

  private val now = Calendar.getInstance().apply { set(2026, Calendar.OCTOBER, 26, 16, 0, 0) }.timeInMillis
  private val lamps = Week.Lamps(listOf(true, true, false, false, false, false, true), todayIndex = 6)
  private val stroop = HomeTimeTrial("stroop", "Tinta o Palabra", TimeTrialState.PENDIENTE, record = 41)

  private val sample = listOf(
    HistoryRow("calculo", "Carga exacta", "hoy", "en tu ruta", "70 % de las cargas, sin pista", now - 1),
    HistoryRow("bodega", "Bodega de carga", "hoy", "en tu ruta", "75 % de los objetos, al primer intento", now - 2),
    HistoryRow("stroop", "Tinta o Palabra", "hoy", "Contrarreloj", "38 aciertos", now - 3, timeTrial = true),
    HistoryRow("secuencia", "Rastro de luz", "ayer", "a tu medida", "7 luces seguidas", now - 4),
    HistoryRow("satelites", "Satélites", "ayer", "en Desafío", "3,2 satélites a la vez", now - 5),
    HistoryRow("acoplamiento", "Acoplamiento", "ayer", "Contrarreloj", "21 aciertos", now - 6, timeTrial = true),
    HistoryRow("intrusa", "La estrella intrusa", "ayer", "en tu ruta", "80 % de aciertos", now - 7),
    HistoryRow("freno", "Freno de Emergencia", "hace 2 días", "a tu medida", null, now - 8),
    HistoryRow("parejas", "Constelaciones", "hace 2 días", "suave", "70 % de las veces, directo a una pareja ya vista", now - 9),
    HistoryRow("meteoros", "Lluvia de meteoros", "hace 3 días", "al límite", "61 % de las palabras menos comunes, reconocidas", now - 10)
  )

  private fun ui(completed: Int, history: List<HistoryRow> = sample) = HomeUi(
    name = "Ricardo", now = now, tier = RankTier.fromRating(0), streak = 3, weeklyGoal = 4, lamps = lamps,
    stops = homeStops(listOf("bodega", "calculo", "intrusa"), completed, AppLanguage.SPANISH), pausedTitle = null, flightDay = false, hasBaseline = true, timeTrial = stroop, history = history
  )

  private var fontState by mutableStateOf(1f)

  private fun frame(widthDp: Int, heightDp: Int, scale: Float, completed: Int, history: List<HistoryRow> = sample) {
    fontState = scale
    rule.setContent {
      val base = LocalDensity.current
      CompositionLocalProvider(LocalDensity provides Density(base.density, fontState), LocalReduceMotion provides true, LocalBarInset provides (if (scale >= 1.3f) 95 else 88).dp) {
        NeuroVidaTheme {
          Box(Modifier.width(widthDp.dp).height(heightDp.dp).background(Color(0xFF050823))) {
            BoardBackground()
            // las barras del sistema simuladas por margen (24 dp arriba y abajo) y la barra de pestañas encima de la de abajo, como las aloja `NeuroVidaApp`
            Box(Modifier.fillMaxSize().padding(top = 24.dp, bottom = 24.dp)) {
              HomeContent(ui = ui(completed, history), onPlay = {}, onResume = {}, onBaseline = {}, onTimeTrial = {}, onLocked = {})
              Box(Modifier.align(Alignment.BottomCenter)) { NeuroNavBar(current = AppTab.HOY, onSelect = {}) }
            }
          }
        }
      }
    }
    rule.waitForIdle()
  }

  private fun save(name: String) { rule.onRoot().captureRoboImage(filePath = "src/test/screenshots/t221/$name.png") }

  private fun first(completed: Int, name: String, w: Int = 393, h: Int = 873, scale: Float = 1f) { frame(w, h, scale, completed); save(name) }
  private fun scrolled(completed: Int, name: String, w: Int = 393, h: Int = 873, scale: Float = 1f, listEnd: Boolean = false) {
    frame(w, h, scale, completed)
    rule.onNodeWithTag("home_history").performScrollTo(); rule.waitForIdle()
    if (listEnd) { rule.onNodeWithTag("home_history_list").performScrollToIndex(sample.size - 1); rule.waitForIdle() }
    save(name)
  }

  @Test fun primera_ruta_a_medias_393_10() = first(1, "primera_ruta_a_medias_393_10")
  @Test fun primera_ruta_completa_393_10() = first(3, "primera_ruta_completa_393_10")
  @Test fun deslizada_ruta_completa_393_10() = scrolled(3, "deslizada_ruta_completa_393_10")
  @Test fun deslizada_ruta_a_medias_393_10() = scrolled(1, "deslizada_ruta_a_medias_393_10")
  @Test fun final_ruta_completa_393_10() = scrolled(3, "final_ruta_completa_393_10", listEnd = true)
  @Test fun primera_ruta_a_medias_393_13() = first(1, "primera_ruta_a_medias_393_13", scale = 1.3f)
  @Test fun deslizada_ruta_a_medias_393_13() = scrolled(1, "deslizada_ruta_a_medias_393_13", scale = 1.3f)
  @Test fun primera_ruta_a_medias_360_10() = first(1, "primera_ruta_a_medias_360_10", 360, 780)
  @Test fun deslizada_ruta_a_medias_360_10() = scrolled(1, "deslizada_ruta_a_medias_360_10", 360, 780)
  @Test fun final_ruta_a_medias_360_10() = scrolled(1, "final_ruta_a_medias_360_10", 360, 780, listEnd = true)
  @Test fun una_sola_partida_393_10() = run { frame(393, 873, 1f, 3, sample.take(2)); rule.onNodeWithTag("home_history").performScrollTo(); rule.waitForIdle(); save("dos_partidas_393_10") }

  // ---- Tarea 221b: la segunda línea en minúscula salvo los nombres propios y Freno sin marca (una fila de cada modo, a 393 con 1,0)

  private val hour = 3_600_000L
  private val day = 86_400_000L

  /** ANTES (Tarea 221): el modo con mayúscula («En tu ruta», «Suave», «Desafío», «Al límite») y Freno con la zona de esa partida. */
  private val modesBefore = listOf(
    HistoryRow("calculo", "Carga exacta", "hoy", "En tu ruta", "70 % de las cargas, sin pista", now - hour),
    HistoryRow("stroop", "Tinta o Palabra", "hoy", "Contrarreloj", "38 aciertos", now - 2 * hour, timeTrial = true),
    HistoryRow("satelites", "Satélites", "ayer", "Desafío", "3,2 satélites a la vez", now - day - hour),
    HistoryRow("parejas", "Constelaciones", "ayer", "Suave", "70 % de las veces, directo a una pareja ya vista", now - day - 2 * hour),
    HistoryRow("freno", "Freno de Emergencia", "hace 2 días", "A tu medida", "zona firme", now - 2 * day - hour),
    HistoryRow("meteoros", "Lluvia de meteoros", "hace 3 días", "Al límite", "61 % de las palabras menos comunes, reconocidas", now - 3 * day - hour)
  )

  /** DESPUÉS: las mismas partidas pasadas por `historyRows` de verdad (modo en minúscula con `RetoChoice.modeWords`, Freno sin marca). */
  private val modesAfter: List<HistoryRow> by lazy {
    fun play(game: String, ago: Long, mode: com.example.data.PlayMode, route: Boolean = false) =
      com.example.model.GamePlayResult(id = "$game-$ago", gameId = game, score = 70, correctAnswers = 7, totalTrials = 10, timed = false, level = 3, timestamp = now - ago, playMode = mode, route = route)
    com.example.data.historyRows(
      history = listOf(
        play("calculo", hour, com.example.data.PlayMode.A_TU_MEDIDA, route = true), play("satelites", day + hour, com.example.data.PlayMode.DESAFIO), play("parejas", day + 2 * hour, com.example.data.PlayMode.SUAVE),
        play("freno", 2 * day + hour, com.example.data.PlayMode.A_TU_MEDIDA), play("meteoros", 3 * day + hour, com.example.data.PlayMode.EXPERTO)
      ),
      timeTrials = listOf(com.example.data.TimeTrialResult(id = "tt", gameId = "stroop", day = "2026-10-26", trackVersion = com.example.data.TimeTrialDay.trackVersion("stroop"), hits = 40, errors = 2, level = 4, seconds = 120, timestamp = now - 2 * hour)),
      measures = listOf(
        com.example.data.MeasurePoint(now - hour, "carga", 70f), com.example.data.MeasurePoint(now - day - hour, "tracking", 3.2f), com.example.data.MeasurePoint(now - day - 2 * hour, "place", 70f),
        com.example.data.MeasurePoint(now - 2 * day - hour, "brake", 260f), com.example.data.MeasurePoint(now - 3 * day - hour, "vocab", 61f)
      ),
      now = now
    )
  }

  private fun modes(rows: List<HistoryRow>, name: String, listEnd: Boolean) {
    frame(393, 873, 1f, 3, rows)
    rule.onNodeWithTag("home_history").performScrollTo(); rule.waitForIdle()
    if (listEnd) { rule.onNodeWithTag("home_history_list").performScrollToIndex(rows.size - 1); rule.waitForIdle() }
    save(name)
  }

  @Test fun modos_antes_inicio_393_10() = modes(modesBefore, "modos_antes_inicio_393_10", false)
  @Test fun modos_antes_final_393_10() = modes(modesBefore, "modos_antes_final_393_10", true)
  @Test fun modos_despues_inicio_393_10() = modes(modesAfter, "modos_despues_inicio_393_10", false)
  @Test fun modos_despues_final_393_10() = modes(modesAfter, "modos_despues_final_393_10", true)
}
