package com.example

import androidx.compose.foundation.layout.Box
import androidx.compose.material3.Text
import androidx.compose.runtime.Composable
import androidx.compose.runtime.CompositionLocalProvider
import androidx.compose.runtime.getValue
import androidx.compose.runtime.mutableStateOf
import androidx.compose.runtime.setValue
import androidx.compose.ui.Modifier
import androidx.compose.ui.graphics.Color
import androidx.compose.ui.platform.testTag
import androidx.compose.ui.semantics.SemanticsProperties
import androidx.compose.ui.test.junit4.StateRestorationTester
import androidx.compose.ui.test.junit4.createComposeRule
import androidx.compose.ui.test.onNodeWithTag
import com.example.ui.components.FlameOrange
import com.example.ui.components.AshLine
import com.example.ui.components.LIT_HEIGHT
import com.example.ui.components.LocalReduceMotion
import com.example.ui.components.OFF_HEIGHT
import com.example.ui.components.glintPulse
import com.example.ui.components.glintScale
import com.example.ui.components.rememberTodayGlint
import com.example.ui.theme.Clay
import org.junit.Assert.assertEquals
import org.junit.Assert.assertTrue
import org.junit.Before
import org.junit.Rule
import org.junit.Test
import org.junit.runner.RunWith
import org.robolectric.RobolectricTestRunner
import org.robolectric.annotation.Config
import org.robolectric.annotation.GraphicsMode
import java.io.File
import java.util.Locale
import kotlin.math.max
import kotlin.math.min

/**
 * La llamita (Tarea 223): un solo símbolo para los días jugados y la racha, en lugar de las farolas y la gota. Lo que se vigila: la proporción de la apagada (0,65 de la encendida), la curva del destello, los contrastes que se midieron, que
 * el destello salga UNA vez por encendido y solo con Hoy a la vista (no al girar, no al cambiar de pestaña, no al abrir la app con el día ya encendido) y que con «quitar animaciones» la llama aparezca encendida de golpe; y que la gota
 * y la farola ya no estén donde ahora va la llamita. Las capturas de la tarea (`Tarea223CapturasTest`) dan lo que se ve; Robolectric no deja leer píxeles (`captureToImage` se cuelga), así que acá se mide el estado, no el dibujo.
 */
@RunWith(RobolectricTestRunner::class)
@GraphicsMode(GraphicsMode.Mode.NATIVE)
@Config(qualifiers = "w393dp-h873dp-xxhdpi", sdk = [36])
class LlamitaTest {
  @get:Rule val rule = createComposeRule()

  @Before fun setUp() { TestSupport.fastFrames() }

  // ---------------------------------------------------------------- las medidas del dibujo

  @Test
  fun la_llama_apagada_mide_0_65_de_la_encendida() {
    assertEquals(0.65f, OFF_HEIGHT / LIT_HEIGHT, 0.005f)
    assertEquals(0.80f, LIT_HEIGHT, 0f)
  }

  @Test
  fun el_destello_va_de_0_8_a_1_1_y_vuelve_a_1_y_el_halo_late_una_sola_vez() {
    assertEquals(0.8f, glintScale(0f), 1e-4f); assertEquals(1.1f, glintScale(0.5f), 1e-4f); assertEquals(1.0f, glintScale(1f), 1e-4f)
    var prev = glintScale(0f)
    for (i in 1..50) { val s = glintScale(i / 100f); assertTrue("sube hasta la mitad", s >= prev - 1e-6f); prev = s }
    for (i in 51..100) { val s = glintScale(i / 100f); assertTrue("baja desde la mitad", s <= prev + 1e-6f); prev = s }
    assertEquals(0f, glintPulse(0f), 1e-4f); assertEquals(1f, glintPulse(0.5f), 1e-4f); assertEquals(0f, glintPulse(1f), 1e-4f)
    // en reposo (avance 1) no hay destello: escala 1 y halo quieto
    assertEquals(1f, glintScale(1f), 1e-4f); assertEquals(0f, glintPulse(1f), 1e-4f)
  }

  private fun lum(c: Color): Double {
    fun f(v: Float): Double = if (v <= 0.03928f) v / 12.92 else Math.pow((v + 0.055) / 1.055, 2.4)
    return 0.2126 * f(c.red) + 0.7152 * f(c.green) + 0.0722 * f(c.blue)
  }
  private fun contrast(a: Color, b: Color): Double { val la = lum(a); val lb = lum(b); return (max(la, lb) + 0.05) / (min(la, lb) + 0.05) }

  @Test
  fun los_contrastes_medidos_sobre_el_cielo_y_sobre_el_crema_se_mantienen() {
    val sky = Color(21, 21, 57)                                 // la mediana del cielo (`BoardBackgroundContrastTest`)
    val cream = Color(0xFFFFEBD2)                               // el crema de la losa de Ajustes y de los cuadros de Tú
    assertTrue("la llama encendida sobre el cielo, al menos 4,5:1 (%.1f)".format(contrast(FlameOrange, sky)), contrast(FlameOrange, sky) >= 4.5)
    assertTrue("el contorno de la llama apagada sobre el cielo, al menos 4,5:1 (%.1f)".format(contrast(AshLine, sky)), contrast(AshLine, sky) >= 4.5)
    assertTrue("la llama apagada sobre el crema (tinta suave), al menos 4,5:1 (%.1f)".format(contrast(Clay.InkMuted, cream)), contrast(Clay.InkMuted, cream) >= 4.5)
    assertTrue("el aro lima de hoy sobre el cielo, al menos 3:1 (%.1f)".format(contrast(Clay.Lime, sky)), contrast(Clay.Lime, sky) >= 3.0)
  }

  // ---------------------------------------------------------------- el destello: cuándo sale

  private var lit by mutableStateOf(false)
  private var visible by mutableStateOf(true)
  private var reduce by mutableStateOf(false)

  @Composable
  private fun Probe() {
    CompositionLocalProvider(LocalReduceMotion provides reduce) {
      val g = rememberTodayGlint(lit, visible)
      Box { Text("%.3f".format(Locale.ROOT, g.value), Modifier.testTag("p")) }
    }
  }

  private fun value(): Float = rule.onNodeWithTag("p").fetchSemanticsNode().config[SemanticsProperties.Text].joinToString("") { it.text }.toFloat()
  private fun step(ms: Long) { rule.mainClock.advanceTimeBy(ms); rule.waitForIdle() }
  /** Escribe el estado en el hilo principal y avisa a Compose: con el reloj detenido, un cambio escrito desde el hilo de la prueba no llega a la composición. */
  private fun set(block: () -> Unit) { rule.runOnIdle(block); androidx.compose.runtime.snapshots.Snapshot.sendApplyNotifications() }

  private fun start(lit0: Boolean, visible0: Boolean = true, reduce0: Boolean = false) {
    lit = lit0; visible = visible0; reduce = reduce0
    rule.mainClock.autoAdvance = false
    rule.setContent { Probe() }
    step(100)
  }

  /** Enciende hoy con Hoy a la vista y deja correr el destello entero. */
  private fun lightAndFinish() {
    set { lit = true }; step(16)
    assertTrue("el destello arrancó (%.3f)".format(value()), value() < 0.2f)
    step(700); assertEquals(1f, value(), 0f)
  }

  @Test
  fun el_destello_sale_cuando_hoy_pasa_de_apagado_a_encendido_con_Hoy_a_la_vista_y_dura_600_ms() {
    start(lit0 = false)
    assertEquals("apagado: en reposo", 1f, value(), 0f)
    set { lit = true }; step(16)
    val v0 = value(); assertTrue("al encender, el destello nace al principio (%.3f)".format(v0), v0 < 0.2f)
    step(284); val v300 = value(); assertTrue("a los 300 ms va por la mitad (%.3f)".format(v300), v300 in 0.35f..0.65f)
    step(400); assertEquals("a los 600 ms terminó y queda en reposo", 1f, value(), 0f)
  }

  @Test
  fun si_Hoy_ya_estaba_encendido_al_abrir_la_app_no_destella() {
    start(lit0 = true)
    for (ms in listOf(0L, 100L, 300L, 600L)) { step(ms); assertEquals("abrir con el día ya encendido no destella", 1f, value(), 0f) }
  }

  @Test
  fun al_abrir_la_app_con_el_dia_encendido_y_el_arranque_aun_sin_datos_tampoco_destella() {
    // la semana sale vacía un instante (los datos aún no llegaron, Hoy no está a la vista) y luego se enciende: nunca se vio apagada
    start(lit0 = false, visible0 = false)
    set { lit = true }; step(100)
    set { visible = true }; step(16)
    for (ms in listOf(0L, 100L, 300L, 600L)) { step(ms); assertEquals("nunca se vio apagada con Hoy a la vista", 1f, value(), 0f) }
  }

  @Test
  fun no_se_repite_al_cambiar_de_pestana_y_volver() {
    start(lit0 = false); lightAndFinish()
    set { visible = false }; step(100)                                    // otra pestaña
    set { visible = true }; step(16)
    assertEquals(1f, value(), 0f); step(300); assertEquals(1f, value(), 0f)
  }

  @Test
  fun no_se_repite_al_girar_la_pantalla() {
    val tester = StateRestorationTester(rule)
    rule.mainClock.autoAdvance = false
    lit = false; visible = true; reduce = false
    tester.setContent { Probe() }
    step(100); lightAndFinish()
    tester.emulateSavedInstanceStateRestore()
    step(16); assertEquals("tras girar, el día ya encendido no destella", 1f, value(), 0f)
    step(300); assertEquals(1f, value(), 0f)
  }

  @Test
  fun si_se_gira_la_pantalla_antes_de_la_primera_partida_el_destello_sigue_armado() {
    val tester = StateRestorationTester(rule)
    rule.mainClock.autoAdvance = false
    lit = false; visible = true; reduce = false
    tester.setContent { Probe() }
    step(100)
    tester.emulateSavedInstanceStateRestore()
    step(100)
    set { lit = true }; step(16)
    assertTrue("sigue armado tras girar (%.3f)".format(value()), value() < 0.2f)
  }

  @Test
  fun si_la_primera_partida_se_jugo_con_Hoy_fuera_de_la_pantalla_el_destello_espera_a_verla() {
    start(lit0 = false)
    set { visible = false }; step(50)                                     // Hoy sale de la pantalla (otra pestaña o el final de la partida encima)
    set { lit = true }; step(300); assertEquals("encendida fuera de la vista: nada que ver todavía", 1f, value(), 0f)
    set { visible = true }; step(16)
    assertTrue("al volver a ver Hoy, destella (%.3f)".format(value()), value() < 0.2f)
    step(700); assertEquals(1f, value(), 0f)
    set { visible = false }; step(50); set { visible = true }; step(16); assertEquals("y una sola vez", 1f, value(), 0f)
  }

  @Test
  fun el_dia_siguiente_vuelve_a_destellar() {
    start(lit0 = false); lightAndFinish()
    set { lit = false }; step(100)                                        // medianoche: hoy es otro día y está apagado
    set { lit = true }; step(16)
    assertTrue("otro día, otro destello (%.3f)".format(value()), value() < 0.2f)
  }

  @Test
  fun con_quitar_animaciones_la_llama_aparece_encendida_de_una_vez_sin_destello() {
    start(lit0 = false, reduce0 = true)
    set { lit = true }; step(16)
    assertEquals("sin animaciones no hay destello", 1f, value(), 0f)
    step(300); assertEquals(1f, value(), 0f)
  }

  // ---------------------------------------------------------------- la llamita reemplaza a la farola y a la gota

  private val root = File(".")                                  // la carpeta del módulo `app`, como en las demás pruebas que leen el código

  private fun ktFiles(dir: String) = File(root, dir).walkTopDown().filter { it.isFile && it.extension == "kt" }.toList()

  @Test
  fun la_gota_de_la_racha_ya_no_existe_ni_en_el_codigo_ni_en_los_recursos() {
    val offenders = ktFiles("src/main").filter { "KitLlama" in it.readText() }.map { it.name }
    assertTrue("`KitLlama` ya no existe (la racha es la llamita): $offenders", offenders.isEmpty())
    assertTrue("sin `kit_llama_*` en los dibujos", File(root, "src/main/res/drawable-nodpi").listFiles().orEmpty().none { it.name.startsWith("kit_llama") })
    val json = File(root, "src/main/res/raw/kit_avance.json").readText()
    assertTrue("sin las piezas `llama_on` y `llama_off` del kit", "\"llama_on\"" !in json && "\"llama_off\"" !in json)
  }

  @Test
  fun las_marcas_de_la_semana_de_hoy_del_resumen_de_avance_y_de_ajustes_y_la_racha_de_Tu_son_llamitas() {
    fun text(rel: String) = File(root, "src/main/java/com/example/$rel").readText()
    assertTrue("la semana de Hoy y del resumen (`WeekLampRendered`) dibuja una llamita", "Llamita(" in text("ui/components/WeekLamps.kt") && "FarolaImage" !in text("ui/components/WeekLamps.kt"))
    assertTrue("la racha junto a «N días» es la misma llamita", Regex("""Llamita\(lit = streak > 0""").containsMatchIn(text("ui/components/WeekLamps.kt")))
    assertTrue("la meta semanal de Ajustes usa la llamita y conserva su fundido y su halo de la elegida", "Llamita(" in text("ui/components/WeeklyGoalLamps.kt") && "FarolaImage" !in text("ui/components/WeeklyGoalLamps.kt") && "Modifier.alpha(glow)" in text("ui/components/WeeklyGoalLamps.kt") && "Clay.Sun.copy(alpha = halo)" in text("ui/components/WeeklyGoalLamps.kt"))
    assertTrue("el cuadro de la racha de Tú usa la llamita", "\"racha\" -> Box" in text("ui/screens/TuCuadros.kt") && "Llamita(" in text("ui/screens/TuCuadros.kt"))
    assertTrue("«Tu semana» de Avance usa `WeekLampRendered`", "WeekLampRendered(" in text("ui/screens/AvanceAreas.kt"))
    // la farola se queda SOLO donde marca estaciones del Primer vuelo (y donde se define)
    val farolaUsers = ktFiles("src/main").filter { "FarolaImage(" in it.readText() }.map { it.name }.toSet()
    assertEquals("la farola solo en el Primer vuelo y su definición", setOf("AvanceKit.kt", "FirstFlightScreen.kt"), farolaUsers)
  }

  @Test
  fun el_destello_es_solo_de_Hoy_y_el_resumen_del_dia_no_lo_pide() {
    val sources = ktFiles("src/main")
    val users = sources.filter { "glintVisible =" in it.readText() }.map { it.name }.toSet()
    assertEquals("solo `HomeScreen` pide el destello (`WeekLamps` solo lo recibe como parámetro)", setOf("HomeScreen.kt"), users)
    val summary = File(root, "src/main/java/com/example/ui/screens/SessionSummaryScreen.kt").readText()
    assertTrue("el resumen del día no destella", "glintVisible" !in summary)
  }
}
