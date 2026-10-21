package com.example

import androidx.compose.foundation.background
import androidx.compose.foundation.layout.Box
import androidx.compose.foundation.layout.fillMaxSize
import androidx.compose.foundation.layout.padding
import androidx.compose.runtime.CompositionLocalProvider
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.graphics.Color
import androidx.compose.ui.test.junit4.createComposeRule
import androidx.compose.ui.test.onRoot
import androidx.compose.ui.unit.dp
import com.example.ui.components.LocalReduceMotion
import com.example.ui.components.NubiClay
import com.example.ui.components.NubiIdle
import com.example.ui.components.NubiSize
import com.example.ui.theme.NeuroVidaTheme
import com.github.takahirom.roborazzi.captureRoboImage
import org.junit.After
import org.junit.Assert.assertEquals
import org.junit.Assert.assertTrue
import org.junit.Rule
import org.junit.Test
import org.junit.runner.RunWith
import org.robolectric.RobolectricTestRunner
import org.robolectric.annotation.Config
import org.robolectric.annotation.GraphicsMode
import kotlin.random.Random

/**
 * Nubi viva (Tarea 148, P1): con el reloj de la prueba a mano, Nubi flota (en el punto alto, 6 dp arriba) y parpadea (a la hora que marca la semilla fija, los ojos se cierran del todo y vuelven a abrirse), y con «quitar animaciones» queda quieta.
 * Lo que se mide es el estado de `rememberNubiIdle` (`NubiIdle.lastForTests`); las capturas de los cuatro cuadros (reposo, arriba, ojos cerrados, de vuelta) alimentan la tira de `docs/previews/tarea-148.png`.
 */
@RunWith(RobolectricTestRunner::class)
@GraphicsMode(GraphicsMode.Mode.NATIVE)
@Config(qualifiers = "w393dp-h400dp-xxhdpi", sdk = [36])
class NubiIdleScreenshotTest {
  @get:Rule val rule = createComposeRule()

  @After fun reset() { NubiIdle.seedForTests = null; NubiIdle.lastForTests = null }

  private fun show(reduceMotion: Boolean = false) {
    NubiIdle.seedForTests = 148L
    rule.mainClock.autoAdvance = false
    rule.setContent {
      CompositionLocalProvider(LocalReduceMotion provides reduceMotion) {
        NeuroVidaTheme {
          Box(Modifier.fillMaxSize().background(Color(0xFF0E0D33)).padding(24.dp), contentAlignment = Alignment.Center) { NubiClay(NubiSize.L) }
        }
      }
    }
  }

  private val idle get() = NubiIdle.lastForTests ?: throw AssertionError("rememberNubiIdle no armó su estado")

  @Test
  fun nubi_flota_y_parpadea_y_las_cuatro_tomas_son_distintas() {
    show()
    rule.mainClock.advanceTimeBy(64)                                // el primer cuadro: en reposo
    assertEquals("en reposo", 0f, idle.floatDp, 0.5f)
    assertEquals(0f, idle.eyesClosed, 0f)
    rule.onRoot().captureRoboImage(filePath = "src/test/screenshots/nubi-viva-1-reposo.png")
    var now = 64L
    rule.mainClock.advanceTimeBy(1125 - now); now = 1125L           // un cuarto del período: el punto más alto
    assertEquals("flota 6 dp hacia arriba", 6f, idle.floatDp, 0.3f)
    assertEquals(0f, idle.eyesClosed, 0f)
    rule.onRoot().captureRoboImage(filePath = "src/test/screenshots/nubi-viva-2-arriba.png")
    val firstBlink = NubiIdle.waitMs(Random(148L))                  // la hora del primer parpadeo con la semilla fija (entre 4 y 7 s)
    assertTrue("el primer parpadeo cae entre 4 y 7 s", firstBlink in NubiIdle.MIN_WAIT_MS..NubiIdle.MAX_WAIT_MS)
    var closed = 0f
    while (now < firstBlink + 240) {                                // por cuadros, para no perderse el cierre completo (dura 120 ms)
      rule.mainClock.advanceTimeBy(16); now += 16
      closed = maxOf(closed, idle.eyesClosed)
      if (idle.eyesClosed >= 0.95f && now >= firstBlink) break
    }
    assertTrue("parpadea: los ojos se cierran (${closed})", closed >= 0.9f)
    rule.onRoot().captureRoboImage(filePath = "src/test/screenshots/nubi-viva-3-ojos-cerrados.png")
    rule.mainClock.advanceTimeBy(400); now += 400                   // los ojos abiertos otra vez
    assertEquals("y vuelven a abrirse", 0f, idle.eyesClosed, 0f)
    rule.onRoot().captureRoboImage(filePath = "src/test/screenshots/nubi-viva-4-de-vuelta.png")
  }

  @Test
  fun con_quitar_animaciones_nubi_queda_quieta_y_con_los_ojos_abiertos() {
    show(reduceMotion = true)
    var now = 0L
    for (t in listOf(100L, 1125L, 4500L, NubiIdle.waitMs(Random(148L)) + 60, 15_000L).sorted()) {
      rule.mainClock.advanceTimeBy(t - now); now = t
      assertEquals("quieta a los $t ms", 0f, idle.floatDp, 0f)
      assertEquals("con los ojos abiertos a los $t ms", 0f, idle.eyesClosed, 0f)
    }
  }
}
