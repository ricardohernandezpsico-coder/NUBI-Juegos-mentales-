package com.example

import androidx.compose.foundation.layout.Box
import androidx.compose.foundation.layout.fillMaxSize
import androidx.compose.foundation.layout.height
import androidx.compose.foundation.layout.padding
import androidx.compose.foundation.layout.width
import androidx.compose.runtime.CompositionLocalProvider
import androidx.compose.runtime.getValue
import androidx.compose.runtime.mutableIntStateOf
import androidx.compose.runtime.mutableStateOf
import androidx.compose.runtime.setValue
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.layout.onSizeChanged
import androidx.compose.ui.platform.LocalDensity
import androidx.compose.ui.platform.testTag
import androidx.compose.ui.test.junit4.createComposeRule
import androidx.compose.ui.test.onNodeWithTag
import androidx.compose.ui.unit.Density
import androidx.compose.ui.unit.dp
import com.example.data.IslaPieza
import com.example.data.Week
import com.example.model.AppLanguage
import com.example.model.RankTier
import com.example.ui.components.BoardBackground
import com.example.ui.components.LocalBarInset
import com.example.ui.components.NeuroNavBar
import com.example.ui.screens.HomeContent
import com.example.ui.screens.HomeUi
import com.example.ui.screens.homeStops
import com.example.ui.theme.NeuroVidaTheme
import com.example.viewmodel.AppTab
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
 * Las capturas de Hoy con UNA sola tarjeta (Tarea 165, `docs/previews/tarea-165.png`): los cinco estados —la primera hora, a mitad de ruta, la ruta completa (la isla chica con Nubi celebrando), una partida en pausa y de noche (Nubi dormida)— a 393 × 873 dp con letra 1,0 y 1,3, con la barra de pestañas y la del sistema
 * (24 dp arriba, 48 abajo) como en el teléfono. Datos fijos (sin base de datos): lo que Hoy muestra se arma con `HomeUi`. No corre en las pruebas de siempre: solo al grabar capturas (`./gradlew.bat :app:recordRoborazziDebug --tests "*Tarea165CapturasTest*"`),
 * y deja los PNG en `src/test/screenshots/tarea-165/`; `tools/previews/tarea_165.py` arma la lámina.
 */
@RunWith(RobolectricTestRunner::class)
@GraphicsMode(GraphicsMode.Mode.NATIVE)
@Config(qualifiers = "w393dp-h873dp-440dpi", sdk = [36])
class Tarea165CapturasTest {
  @get:Rule val rule = createComposeRule()

  @Before fun solo_al_grabar() { Assume.assumeTrue("solo al grabar capturas", System.getProperty("roborazzi.test.record") == "true") }

  private fun at(h: Int, m: Int = 30) = Calendar.getInstance().apply { set(2026, Calendar.OCTOBER, 24, h, m, 0) }.timeInMillis
  private val lamps = Week.Lamps(listOf(true, true, false, true, false, false, true), todayIndex = 6)
  private val ids = listOf("bodega", "calculo", "intrusa")

  private fun ui(completed: Int, paused: String? = null, now: Long = at(16), pieces: List<IslaPieza> = emptyList()) = HomeUi(
    name = "Ricardo", now = now, tier = RankTier.fromRating(0), streak = 7, streakNote = "Descansaste el domingo: tu racha sigue.", weeklyGoal = 4, lamps = lamps,
    stops = homeStops(ids, completed, AppLanguage.SPANISH), pausedTitle = paused, flightDay = false, hasBaseline = true, islandPieces = pieces
  )

  /** El marco de 393 dp con Hoy y la barra de pestañas: [shot] captura con la letra pedida (1,0 o 1,3). */
  private inner class Frame(ui: HomeUi) {
    var fontScale by mutableStateOf(1f)
    var barPx by mutableIntStateOf(0)

    init {
      rule.mainClock.autoAdvance = false
      rule.setContent {
        val base = LocalDensity.current
        CompositionLocalProvider(LocalDensity provides Density(base.density, fontScale)) {
          NeuroVidaTheme {
            Box(Modifier.width(393.dp).height(873.dp).testTag("frame")) {
              BoardBackground()
              Box(Modifier.fillMaxSize().padding(bottom = 48.dp)) {                                  // 48 dp de la barra de navegación del sistema
                val barInset = with(LocalDensity.current) { if (barPx > 0) barPx.toDp() else 88.dp }
                CompositionLocalProvider(LocalBarInset provides barInset) {
                  Box(Modifier.padding(top = 24.dp)) { HomeContent(ui = ui, onAllGames = {}, onPlay = {}, onResume = {}, onBaseline = {}) }
                }
                NeuroNavBar(current = AppTab.HOY, onSelect = {}, modifier = Modifier.align(Alignment.BottomCenter).onSizeChanged { if (it.height > 0) barPx = it.height })
              }
            }
          }
        }
      }
    }

    fun settle() = repeat(3) { rule.mainClock.advanceTimeBy(600); rule.waitForIdle() }

    fun shot(name: String, scale: Float = 1f) {
      rule.runOnIdle { fontScale = scale }
      settle()
      rule.onNodeWithTag("frame").captureRoboImage(filePath = "src/test/screenshots/tarea-165/$name${if (scale == 1f) "" else "-letra" + (scale * 10).toInt()}.png")
    }
  }

  private fun estado(name: String, ui: HomeUi, scales: List<Float> = listOf(1f, 1.3f)) { val f = Frame(ui); for (s in scales) f.shot(name, s) }

  @Test fun primera_hora() = estado("primera-hora", ui(completed = 0))
  @Test fun a_mitad_de_ruta() = estado("mitad-de-ruta", ui(completed = 1))
  @Test fun ruta_completa() = estado("ruta-completa", ui(completed = 3, pieces = listOf(IslaPieza.C1, IslaPieza.V1, IslaPieza.VO1)))
  @Test fun partida_en_pausa() = estado("partida-en-pausa", ui(completed = 1, paused = "Carga exacta"))
  @Test fun de_noche() = estado("de-noche", ui(completed = 1, now = at(22, 10)))
  @Test fun letra_2_0() = estado("mitad-de-ruta", ui(completed = 1), scales = listOf(2f))
}
