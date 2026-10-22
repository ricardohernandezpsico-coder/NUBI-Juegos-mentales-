package com.example

import androidx.compose.foundation.layout.Box
import androidx.compose.foundation.layout.Column
import androidx.compose.foundation.layout.fillMaxSize
import androidx.compose.foundation.layout.height
import androidx.compose.foundation.layout.padding
import androidx.compose.foundation.layout.width
import androidx.compose.runtime.CompositionLocalProvider
import androidx.compose.ui.Modifier
import androidx.compose.ui.platform.LocalDensity
import androidx.compose.ui.test.junit4.createComposeRule
import androidx.compose.ui.test.onRoot
import androidx.compose.ui.unit.Density
import androidx.compose.ui.unit.dp
import com.example.data.AreaPoints
import com.example.data.GamePoints
import com.example.data.MeasurePoint
import com.example.data.PlayMode
import com.example.model.AppLanguage
import com.example.ui.components.BoardBackground
import com.example.ui.screens.AvanceAreaPage
import com.example.ui.theme.NeuroVidaTheme
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
 * Capturas REALES de Roborazzi de la tarjeta de juego de la página del área (Tarea 162; `auditoria-fable/spec-tarjeta-juego-avance.md` §3 y §4) con los SEIS casos del spec en una misma página, a letra 1,0, 1,3 y 2,0:
 * normal (Acoplamiento, con su mejor marca), frase larga (Aterrizaje, última partida de hace 4 días con su mejor marca), nombre largo y sin medir (En la punta de la lengua), Freno de Emergencia (su medida es un promedio), sin partidas (Engranajes)
 * y Rastro de luz (el nivel y la medida hablan de lo mismo). Solo corren al grabar (`recordRoborazziDebug`); salen a `src/test/screenshots/tarea-162/casos-letra<N>.png`, que no va a git. La lámina `tools/previews/tarea_162.py` las junta con las de «hoy».
 */
@RunWith(RobolectricTestRunner::class)
@GraphicsMode(GraphicsMode.Mode.NATIVE)
@Config(qualifiers = "w393dp-h4000dp-440dpi", sdk = [36])
class Tarea162CapturasTest {
  @get:Rule val rule = createComposeRule()

  private val day = 86_400_000L
  private val now = Calendar.getInstance().apply { clear(); set(2026, Calendar.OCTOBER, 21, 12, 0, 0) }.timeInMillis

  @Before
  fun soloAlGrabar() {
    Assume.assumeTrue("solo al grabar capturas", System.getProperty("roborazzi.test.record") == "true")
  }

  private fun mp(key: String, daysAgo: Int, v: Float) = MeasurePoint(now - daysAgo * day, key, v, 0.5f, false, PlayMode.A_TU_MEDIDA)

  private val measures = listOf(
    mp("rotation", 9, 180f), mp("rotation", 6, 200f), mp("rotation", 3, 210f), mp("rotation", 0, 234f),            // Acoplamiento: hoy 234 grados por segundo, su mejor marca
    mp("numline", 12, 2f), mp("numline", 9, 4f), mp("numline", 6, 5f), mp("numline", 4, 3.5f),                      // Aterrizaje: hace 4 días a 3,5 % del blanco; su mejor marca, 2 %
    mp("brake", 8, 270f), mp("brake", 5, 262f), mp("brake", 3, 248f), mp("brake", 1, 255f),                         // Freno: promedio de las últimas partidas, zona firme
    mp("trail", 6, 4f), mp("trail", 4, 7f), mp("trail", 2, 6f), mp("trail", 0, 5f)                                  // Rastro: hoy 5 luces seguidas; su mejor marca, 7
  )

  /** Los seis casos del spec, de cualquier área (la prueba solo mira la tarjeta). */
  private val games = listOf(
    GamePoints("acoplamiento", 147, 250, 0.59f, true),
    GamePoints("aterrizaje", 90, 250, 0.36f, true),
    GamePoints("anagramas", 0, 250, 0f, false),                // En la punta de la lengua: jugado hace 2 días, aún sin medir
    GamePoints("freno", 120, 200, 0.6f, true),
    GamePoints("engranajes", 0, 250, 0f, false),               // sin partidas
    GamePoints("secuencia", 112, 250, 0.45f, true)
  )
  private val played = mapOf("acoplamiento" to now, "aterrizaje" to now - 4 * day, "anagramas" to now - 2 * day, "freno" to now - day, "secuencia" to now)

  private fun capture(fontScale: Float, shot: String, pageHeight: Int) {
    val points = AreaPoints("RAZONAMIENTO", 500, games, 0, 500)
    rule.setContent {
      val base = LocalDensity.current
      CompositionLocalProvider(LocalDensity provides Density(base.density, fontScale)) {
        NeuroVidaTheme {
          Box(Modifier.width(393.dp).height((pageHeight + 100).dp)) {
            BoardBackground()
            Column(Modifier.fillMaxSize().padding(horizontal = 0.dp)) {
              AvanceAreaPage(points, emptyList(), "Casos difíciles", played, measures, AppLanguage.SPANISH, now, {}, Modifier.height(pageHeight.dp))
            }
          }
        }
      }
    }
    rule.waitForIdle()
    rule.onRoot().captureRoboImage(filePath = "src/test/screenshots/tarea-162/$shot.png")
  }

  @Test fun casos_a_letra_1_0() = capture(1f, "casos-letra10", 3000)
  @Test fun casos_a_letra_1_3() = capture(1.3f, "casos-letra13", 3600)
  @Test fun casos_a_letra_2_0() = capture(2f, "casos-letra20", 5200)
}
