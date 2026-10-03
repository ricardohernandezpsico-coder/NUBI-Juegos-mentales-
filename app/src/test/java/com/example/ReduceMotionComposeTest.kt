package com.example

import android.provider.Settings
import androidx.compose.foundation.layout.Box
import androidx.compose.runtime.CompositionLocalProvider
import androidx.compose.foundation.layout.height
import androidx.compose.foundation.layout.width
import androidx.compose.ui.Modifier
import androidx.compose.ui.test.junit4.createComposeRule
import androidx.compose.ui.test.onRoot
import androidx.compose.ui.unit.dp
import androidx.test.core.app.ApplicationProvider
import com.example.ui.components.CosmosBackground
import com.example.ui.components.LocalReduceMotion
import com.example.ui.components.systemReduceMotion
import com.github.takahirom.roborazzi.RobolectricDeviceQualifiers
import com.github.takahirom.roborazzi.captureRoboImage
import java.io.File
import org.junit.After
import org.junit.Assert.assertArrayEquals
import org.junit.Assert.assertFalse
import org.junit.Assert.assertTrue
import org.junit.Before
import org.junit.Rule
import org.junit.Test
import org.junit.runner.RunWith
import org.robolectric.RobolectricTestRunner
import org.robolectric.annotation.Config
import org.robolectric.annotation.GraphicsMode

/**
 * "Quitar animaciones" en la app (Compose): con [LocalReduceMotion] activo (en el telefono sale de la escala de animacion
 * del sistema en 0, `ANIMATOR_DURATION_SCALE`), las animaciones INFINITAS
 * (`rememberInfiniteTransition`) tienen que quedarse quietas. El fondo `CosmosBackground` es el caso: lo usan
 * todas las pantallas y solo se mueve con una transicion infinita.
 *
 * Se dibuja el fondo dos veces (a los 0,5 s y a los 6 s del reloj de animacion) y se comparan los bytes de las
 * dos imagenes. El control con escala 1 comprueba que la prueba SI detecta movimiento (si no, "queda quieto" no
 * probaria nada). Las imagenes van a `build/reduce-motion/`, no a `src/test/screenshots`.
 */
@RunWith(RobolectricTestRunner::class)
@GraphicsMode(GraphicsMode.Mode.NATIVE)
@Config(qualifiers = RobolectricDeviceQualifiers.Pixel8, sdk = [36])
class ReduceMotionComposeTest {

  @get:Rule val composeTestRule = createComposeRule()

  private val dir = File("build/reduce-motion")
  private var recordBefore: String? = null

  @Before
  fun setUp() {
    // Roborazzi solo escribe la imagen si esta en modo "record"; aca se usa como grabadora de pixeles.
    recordBefore = System.getProperty("roborazzi.test.record")
    System.setProperty("roborazzi.test.record", "true")
    dir.deleteRecursively()
    dir.mkdirs()
  }

  @After
  fun tearDown() {
    if (recordBefore == null) System.clearProperty("roborazzi.test.record")
    else System.setProperty("roborazzi.test.record", recordBefore!!)
  }

  /** Dibuja el fondo con "quitar animaciones" [reduce] y devuelve los bytes de la imagen a los 0,5 s y a los 6 s. */
  private fun backgroundAt(reduce: Boolean): Pair<ByteArray, ByteArray> {
    composeTestRule.mainClock.autoAdvance = false
    composeTestRule.setContent {
      CompositionLocalProvider(LocalReduceMotion provides reduce) { Box(Modifier.width(200.dp).height(400.dp)) { CosmosBackground() } }
    }
    composeTestRule.mainClock.advanceTimeBy(500)
    val a = File(dir, "reduce-$reduce-a.png")
    composeTestRule.onRoot().captureRoboImage(filePath = a.path)
    composeTestRule.mainClock.advanceTimeBy(5_500)
    val b = File(dir, "reduce-$reduce-b.png")
    composeTestRule.onRoot().captureRoboImage(filePath = b.path)
    return a.readBytes() to b.readBytes()
  }

  @Test
  fun control_sin_quitar_animaciones_el_fondo_SI_se_mueve() {
    val (a, b) = backgroundAt(false)
    assertFalse("sin 'quitar animaciones' el fondo debe cambiar entre dos instantes", a.contentEquals(b))
  }

  @Test
  fun con_quitar_animaciones_el_fondo_infinito_queda_quieto() {
    val (a, b) = backgroundAt(true)
    assertArrayEquals("con 'quitar animaciones' el fondo no debe cambiar", a, b)
  }

  @Test
  fun la_escala_del_sistema_en_0_se_lee_como_quitar_animaciones() {
    val context = ApplicationProvider.getApplicationContext<android.content.Context>()
    Settings.Global.putFloat(context.contentResolver, Settings.Global.ANIMATOR_DURATION_SCALE, 0f)
    assertTrue(systemReduceMotion(context))
    Settings.Global.putFloat(context.contentResolver, Settings.Global.ANIMATOR_DURATION_SCALE, 1f)
    assertFalse(systemReduceMotion(context))
  }
}
