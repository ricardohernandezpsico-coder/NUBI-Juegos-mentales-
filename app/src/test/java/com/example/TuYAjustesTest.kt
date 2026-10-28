package com.example

import android.app.Application
import androidx.activity.compose.LocalActivityResultRegistryOwner
import androidx.activity.result.ActivityResultRegistry
import androidx.activity.result.ActivityResultRegistryOwner
import androidx.activity.result.contract.ActivityResultContract
import androidx.core.app.ActivityOptionsCompat
import androidx.compose.foundation.layout.Box
import androidx.compose.foundation.layout.height
import androidx.compose.foundation.layout.width
import androidx.compose.runtime.CompositionLocalProvider
import androidx.compose.ui.Modifier
import androidx.compose.ui.semantics.SemanticsProperties
import androidx.compose.ui.test.assertIsDisplayed
import androidx.compose.ui.test.hasTestTag
import androidx.compose.ui.test.hasText
import androidx.compose.ui.test.junit4.createComposeRule
import androidx.compose.ui.test.onNodeWithTag
import androidx.compose.ui.test.onNodeWithText
import androidx.compose.ui.test.performClick
import androidx.compose.ui.test.performScrollTo
import androidx.compose.ui.unit.dp
import androidx.test.core.app.ApplicationProvider
import com.example.ui.components.BoardBackground
import com.example.ui.components.LocalBarInset
import com.example.ui.components.LocalReduceMotion
import com.example.ui.screens.ComoJuegasPanel
import com.example.ui.screens.SettingsScreen
import com.example.ui.screens.TuScreen
import com.example.ui.theme.NeuroVidaTheme
import com.example.viewmodel.NeuroVidaViewModel
import org.junit.After
import org.junit.Assert.assertEquals
import org.junit.Assert.assertFalse
import org.junit.Assert.assertTrue
import org.junit.Before
import org.junit.Rule
import org.junit.Test
import org.junit.runner.RunWith
import org.robolectric.RobolectricTestRunner
import org.robolectric.annotation.Config
import org.robolectric.annotation.GraphicsMode
import java.io.File

/**
 * Tú y Ajustes con una sola puerta por ajuste (Tarea 185; Opus, 28-oct: la regla de la Tarea 152): el sonido, la vibración y el sí o no del recordatorio viven SOLO en «Cómo juegas» de Tú; Ajustes son cuatro losas (Juego, Recordatorio, Tus datos y Acerca de Nubi) y guarda la meta, la edad, los
 * estudios y la HORA del recordatorio. Sin `Toast`s de Android: lo único que se explica es lo que no sale como se espera (Android no deja avisar; el aviso de prueba enviado, 2 s). Con un ViewModel de verdad sobre Room en memoria; el permiso de notificaciones se simula con un registro de resultados propio.
 */
@RunWith(RobolectricTestRunner::class)
@GraphicsMode(GraphicsMode.Mode.NATIVE)
@Config(qualifiers = "w393dp-h3600dp-440dpi", sdk = [36])
class TuYAjustesTest {
  @get:Rule val rule = createComposeRule()

  private lateinit var app: Application
  private val viewModels = mutableListOf<NeuroVidaViewModel>()

  @Before fun setUp() { app = ApplicationProvider.getApplicationContext(); TestSupport.resetDatabase() }
  @After fun tearDown() { TestSupport.release(*viewModels.toTypedArray()); viewModels.clear(); TestSupport.resetDatabase() }

  private fun vm() = NeuroVidaViewModel(app).also { viewModels.add(it) }
  private fun settings(vm: NeuroVidaViewModel) = vm.userSettings.value

  /** Un registro de resultados que contesta de una vez a «¿puedo avisar?» con [granted] (la prueba no abre el diálogo de Android). */
  private fun registry(granted: Boolean) = object : ActivityResultRegistryOwner {
    override val activityResultRegistry = object : ActivityResultRegistry() {
      @Suppress("UNCHECKED_CAST")
      override fun <I, O> onLaunch(requestCode: Int, contract: ActivityResultContract<I, O>, input: I, options: ActivityOptionsCompat?) { dispatchResult(requestCode, granted as O) }
    }
  }

  private fun show(granted: Boolean = true, content: @androidx.compose.runtime.Composable () -> Unit) {
    rule.mainClock.autoAdvance = true
    val owner = registry(granted)
    rule.setContent {
      CompositionLocalProvider(LocalActivityResultRegistryOwner provides owner, LocalReduceMotion provides true, LocalBarInset provides 88.dp) {
        NeuroVidaTheme { Box(Modifier.width(393.dp).height(3600.dp)) { BoardBackground(); content() } }
      }
    }
    rule.waitForIdle()
  }

  private fun yOf(tag: String) = rule.onNodeWithTag(tag).fetchSemanticsNode().boundsInRoot.top
  private fun exists(tag: String) = rule.onAllNodes(hasTestTag(tag)).fetchSemanticsNodes().isNotEmpty()

  // ------------------------------------------------------------------ Tú

  @Test
  fun tu_va_de_arriba_a_abajo_con_Nubi_sola_los_cuatro_cuadros_las_medallas_como_juegas_y_contacto_al_final() {
    val v = vm()
    show { TuScreen(viewModel = v) }
    rule.onNodeWithTag("tu_nubi").assertIsDisplayed()
    assertFalse("sin isla (decisión de Ricardo)", exists("profile_island"))
    assertEquals("UNA sola Nubi, con los ojos abiertos", 1, rule.onAllNodes(hasTestTag("nubi_ojos_abiertos"), useUnmergedTree = true).fetchSemanticsNodes().size)
    val order = listOf("tu_title", "tu_nubi", "profile_name", "btn_change_name", "tu_cuadros", "card_como_juegas", "btn_open_settings", "btn_open_fuentes", "tu_contacto")
    val ys = order.map { yOf(it) }
    assertEquals("el orden de arriba abajo: ${order.zip(ys)}", ys.sorted(), ys)
    for (k in listOf("racha", "partidas", "medallas", "juegos")) assertTrue("falta el cuadro $k", exists("tu_cuadro_$k"))
    rule.onNodeWithText("Tus medallas").assertExists()
    // sin partidas todavía: los cuadros dicen cero con su unidad, sin culpa
    val desc = { k: String -> rule.onNodeWithTag("tu_cuadro_$k").fetchSemanticsNode().config[SemanticsProperties.ContentDescription].single() }
    assertEquals("Racha: 0 días seguidos. Tu racha empieza con tu próxima partida.", desc("racha"))
    assertEquals("Medallas: 0 de 15 medallas.", desc("medallas"))
    assertEquals("Juegos: 0 de 17 juegos, probados al menos una vez.", desc("juegos"))
  }

  @Test
  fun mas_ajustes_y_en_que_se_basa_nubi_abren_sus_paneles() {
    val v = vm()
    show { TuScreen(viewModel = v) }
    rule.onNodeWithTag("btn_open_settings").performScrollTo().performClick()
    assertEquals(com.example.viewmodel.TopPanel.AJUSTES, v.topPanel.value)
    v.openTopPanel(null)
    rule.onNodeWithTag("btn_open_fuentes").performScrollTo().performClick()
    assertEquals(com.example.viewmodel.TopPanel.FUENTES, v.topPanel.value)
  }

  // ------------------------------------------------------------------ Cómo juegas: una sola puerta

  @Test
  fun el_sonido_y_la_vibracion_se_apagan_desde_como_juegas() {
    val v = vm()
    show { ComoJuegasPanel(v) }
    assertTrue(settings(v).soundEnabled && settings(v).hapticsEnabled)
    rule.onNodeWithTag("switch_sound").performClick()
    TestSupport.awaitUntil { !settings(v).soundEnabled }
    assertTrue("la vibración sigue como estaba", settings(v).hapticsEnabled)
    rule.onNodeWithTag("switch_haptics").performClick()
    TestSupport.awaitUntil { !settings(v).hapticsEnabled }
    rule.onNodeWithTag("switch_sound").performClick()
    TestSupport.awaitUntil { settings(v).soundEnabled }
  }

  @Test
  fun el_recordatorio_se_apaga_y_se_enciende_desde_como_juegas_y_dice_a_que_hora() {
    val v = vm()
    show(granted = true) { ComoJuegasPanel(v) }
    assertTrue("de fábrica está encendido", settings(v).notificationsEnabled)
    rule.onNodeWithTag("reminder_hint").assertIsDisplayed()
    assertTrue("dice la hora y dónde se cambia", rule.onNodeWithTag("reminder_hint").fetchSemanticsNode().config[SemanticsProperties.Text].single().text.let { it.contains("19:00") && it.contains("Ajustes") })
    rule.onNodeWithTag("switch_notifications_enabled").performClick()
    TestSupport.awaitUntil { !settings(v).notificationsEnabled }
    assertFalse("apagado no explica la hora", exists("reminder_hint"))
    rule.onNodeWithTag("switch_notifications_enabled").performClick()                          // Android 13+: pide permiso; el registro de la prueba dice que sí
    TestSupport.awaitUntil { settings(v).notificationsEnabled }
    assertFalse("y no hay ningún aviso de «no se pudo»", exists("reminder_denied"))
  }

  @Test
  fun si_android_no_deja_avisar_el_interruptor_queda_apagado_y_una_linea_lo_explica() {
    val v = vm()
    v.setReminderEnabled(false)
    TestSupport.awaitUntil { !settings(v).notificationsEnabled }
    show(granted = false) { ComoJuegasPanel(v) }
    rule.onNodeWithTag("switch_notifications_enabled").performClick()
    rule.waitForIdle()
    assertFalse("no se encendió", settings(v).notificationsEnabled)
    rule.onNodeWithTag("reminder_denied").assertIsDisplayed()
    assertTrue(rule.onNodeWithTag("reminder_denied").fetchSemanticsNode().config[SemanticsProperties.Text].single().text.contains("Android no dejó activar los avisos"))
  }

  // ------------------------------------------------------------------ Ajustes: cuatro losas

  @Test
  fun ajustes_son_cuatro_losas_en_su_orden_sin_los_tres_interruptores_ni_el_informe() {
    val v = vm()
    show { SettingsScreen(viewModel = v) }
    val order = listOf("card_juego", "card_notifications_workmanager", "card_tus_datos", "card_acerca")
    val ys = order.map { yOf(it) }
    assertEquals("Juego, Recordatorio, Tus datos y Acerca de Nubi, en ese orden: ${order.zip(ys)}", ys.sorted(), ys)
    for (gone in listOf("switch_sound", "switch_haptics", "switch_notifications_enabled", "btn_send_error_report", "card_language_selector")) assertFalse("«$gone» ya no está en Ajustes", exists(gone))
    assertEquals("ningún texto de Ajustes dice «Efectos de sonido» ni «Vibración»", 0, rule.onAllNodes(hasText("Efectos de sonido") or hasText("Vibración")).fetchSemanticsNodes().size)
    rule.onNodeWithTag("btn_licenses").assertExists()
    rule.onNodeWithTag("health_notice").assertExists()
    rule.onNodeWithTag("btn_reset_data").assertExists()
    assertFalse("sin política de privacidad todavía", rule.onAllNodes(hasText("rivacidad", substring = true)).fetchSemanticsNodes().isNotEmpty())
  }

  @Test
  fun la_linea_de_las_animaciones_esta_en_juego_y_los_estudios_son_una_linea_con_cambiar() {
    val v = vm()
    show { SettingsScreen(viewModel = v) }
    val juegoTop = yOf("card_juego"); val recTop = yOf("card_notifications_workmanager")
    val note = yOf("settings_animations_note")
    assertTrue("la línea de las animaciones va dentro de la losa Juego", note > juegoTop && note < recTop)
    rule.onNodeWithTag("education_line").assertIsDisplayed()
    assertEquals("Tu nivel de estudios: sin elegir", rule.onNodeWithTag("education_line").fetchSemanticsNode().config[SemanticsProperties.Text].single().text)
    assertFalse("las seis opciones están plegadas", exists("education_basica"))
    rule.onNodeWithTag("btn_education_change").performClick()
    for (e in com.example.data.Education.entries) assertTrue("falta ${e.name}", exists("education_${e.name.lowercase()}"))
    rule.onNodeWithTag("education_${com.example.data.Education.entries[2].name.lowercase()}").performClick()
    TestSupport.awaitUntil { v.education.value == com.example.data.Education.entries[2] }
    assertEquals("Tu nivel de estudios: ${com.example.data.Education.entries[2].label.lowercase()}", rule.onNodeWithTag("education_line").fetchSemanticsNode().config[SemanticsProperties.Text].single().text)
  }

  @Test
  fun con_el_recordatorio_apagado_las_horas_se_ven_apagadas_y_una_linea_dice_donde_encenderlo() {
    val v = vm()
    v.setReminderEnabled(false)
    TestSupport.awaitUntil { !settings(v).notificationsEnabled }
    show { SettingsScreen(viewModel = v) }
    rule.onNodeWithTag("reminder_off_note").assertIsDisplayed()
    assertTrue(rule.onNodeWithTag("reminder_off_note").fetchSemanticsNode().config[SemanticsProperties.Text].single().text.contains("Enciéndelo en Tú"))
    assertFalse(exists("reminder_state_note"))
    val before = settings(v).reminderHour
    rule.onNodeWithTag("btn_time_noche").performScrollTo().performClick()
    rule.waitForIdle()
    assertEquals("apagado, tocar una hora no hace nada", before, settings(v).reminderHour)
    rule.onNodeWithTag("btn_test_notification").performClick()
    assertFalse("ni la prueba del aviso", exists("test_notification_note"))
  }

  @Test
  fun con_el_recordatorio_encendido_se_elige_la_hora_y_se_prueba_el_aviso_con_una_linea_de_dos_segundos() {
    val v = vm()
    show { SettingsScreen(viewModel = v) }
    assertTrue(settings(v).notificationsEnabled)
    assertFalse(exists("reminder_off_note"))
    rule.onNodeWithTag("btn_time_noche").performScrollTo().performClick()
    TestSupport.awaitUntil { settings(v).reminderHour == 21 }
    assertEquals(0, settings(v).reminderMinute)
    rule.mainClock.autoAdvance = false
    rule.onNodeWithTag("btn_test_notification").performScrollTo().performClick()
    rule.mainClock.advanceTimeBy(100)
    rule.onNodeWithTag("test_notification_note").assertIsDisplayed()
    assertEquals("Aviso de prueba enviado", rule.onNodeWithTag("test_notification_note").fetchSemanticsNode().config[SemanticsProperties.Text].single().text)
    rule.mainClock.advanceTimeBy(2_200)
    assertFalse("a los 2 s la línea se va sola", exists("test_notification_note"))
  }

  // ------------------------------------------------------------------ el código

  @Test
  fun cada_ajuste_aparece_una_sola_vez_en_toda_la_app() {
    val main = File("src/main/java").walkTopDown().filter { it.extension == "kt" }.map { it.name to it.readText() }.toList()
    fun count(literal: String) = main.sumOf { (_, t) -> Regex(Regex.escape(literal)).findAll(t).count() }
    for (tag in listOf("switch_sound", "switch_haptics", "switch_notifications_enabled")) assertEquals("«$tag» se define una sola vez", 1, count("\"$tag\""))
    for (label in listOf("Efectos de sonido", "Vibración")) assertEquals("«$label» está escrito una sola vez", 1, count("\"$label\""))
    // las demás puertas: cada control de Ajustes sale una sola vez
    for (tag in listOf("settings_weekly_goal", "btn_education_change", "btn_licenses", "btn_reset_data", "btn_test_notification", "btn_send_error_report", "btn_write_to_nubi"))
      assertEquals("«$tag» se define una sola vez", 1, count("\"$tag\""))
    assertEquals("la hora del recordatorio, una sola vez", 1, count("\"Hora del recordatorio\""))
    // los controles de Tú no están en Ajustes y viceversa
    val settings = main.first { it.first == "SettingsScreen.kt" }.second
    val tu = main.first { it.first == "ComoJuegas.kt" }.second
    assertFalse(settings.contains("ClaySwitch(") || settings.contains("SettingSwitchRow("))
    assertFalse("«Tú» no pide la hora ni prueba el aviso", tu.contains("Hora del recordatorio") || tu.contains("triggerTestNotification"))
  }

  @Test
  fun ni_ajustes_ni_tu_usan_toast_de_android() {
    for (f in listOf("ui/screens/SettingsScreen.kt", "ui/screens/ComoJuegas.kt", "ui/screens/ProfileScreen.kt", "ui/screens/Contacto.kt", "ui/screens/FuentesScreen.kt"))
      assertFalse("$f usa Toast", File("src/main/java/com/example/$f").readLines().filter { l -> l.trim().let { !it.startsWith("//") && !it.startsWith("*") && !it.startsWith("/*") } }.joinToString("\n").contains("Toast"))
  }
}
