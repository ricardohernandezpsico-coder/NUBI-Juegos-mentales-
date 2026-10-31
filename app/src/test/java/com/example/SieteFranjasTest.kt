package com.example

import android.app.Application
import androidx.compose.foundation.layout.Box
import androidx.compose.foundation.layout.fillMaxSize
import androidx.compose.foundation.layout.height
import androidx.compose.foundation.layout.width
import androidx.compose.runtime.CompositionLocalProvider
import androidx.compose.ui.Modifier
import androidx.compose.ui.platform.LocalDensity
import androidx.compose.ui.semantics.SemanticsProperties
import androidx.compose.ui.test.assertIsDisplayed
import androidx.compose.ui.test.getUnclippedBoundsInRoot
import androidx.compose.ui.test.hasTestTag
import androidx.compose.ui.test.junit4.createComposeRule
import androidx.compose.ui.test.onNodeWithTag
import androidx.compose.ui.test.onNodeWithText
import androidx.compose.ui.test.performClick
import androidx.compose.ui.test.performScrollTo
import androidx.compose.ui.unit.Density
import androidx.compose.ui.unit.dp
import androidx.test.core.app.ApplicationProvider
import com.example.data.FlightMode
import com.example.data.FlightState
import com.example.data.FlightStep
import com.example.data.local.NeuroVidaDatabase
import com.example.data.local.toEntity
import com.example.model.AgeBand
import com.example.model.AgeRange
import com.example.model.UserSettings
import com.example.ui.components.BoardBackground
import com.example.ui.components.LocalBarInset
import com.example.ui.components.LocalReduceMotion
import com.example.ui.screens.FirstFlightScreen
import com.example.ui.screens.FlightActions
import com.example.ui.screens.SettingsScreen
import com.example.ui.theme.NeuroVidaTheme
import com.example.viewmodel.NeuroVidaViewModel
import kotlinx.coroutines.runBlocking
import org.junit.After
import org.junit.Assert.assertEquals
import org.junit.Assert.assertFalse
import org.junit.Assert.assertNull
import org.junit.Assert.assertTrue
import org.junit.Before
import org.junit.Rule
import org.junit.Test
import org.junit.runner.RunWith
import org.robolectric.RobolectricTestRunner
import org.robolectric.annotation.Config
import org.robolectric.annotation.GraphicsMode

/**
 * Las siete franjas de edad en el Primer vuelo y en Ajustes (Tarea 195, decisión de Ricardo del 30-oct): 13 a 17 · 18 a 29 · 30 a 44 · 45 a 54 · 55 a 64 · 65 a 74 · 75 o más, cada número con su unidad y sin opción para menores de 13.
 * El vuelo pregunta lo mismo de siempre («¿En qué rango de edad estás?», con su línea de ayuda) y Ajustes muestra la misma lista; quien ya tenía solo la banda de tres de antes no ve ninguna marcada hasta que elija. La franja vive solo en el teléfono.
 */
@RunWith(RobolectricTestRunner::class)
@GraphicsMode(GraphicsMode.Mode.NATIVE)
@Config(qualifiers = "w393dp-h873dp-xxhdpi", sdk = [36])
class SieteFranjasTest {
  @get:Rule val rule = createComposeRule()

  private lateinit var app: Application
  private val viewModels = mutableListOf<NeuroVidaViewModel>()

  @Before fun setUp() { app = ApplicationProvider.getApplicationContext(); TestSupport.resetDatabase(); TestSupport.fastFrames() }
  @After fun tearDown() { TestSupport.release(*viewModels.toTypedArray()); viewModels.clear(); TestSupport.resetDatabase() }

  private val labels = listOf("13 a 17 años", "18 a 29 años", "30 a 44 años", "45 a 54 años", "55 a 64 años", "65 a 74 años", "75 años o más")
  private val hint = "Cambia el ritmo y el tamaño de los juegos, no lo difícil: eso lo decide cómo juegas."

  private fun exists(tag: String) = rule.onAllNodes(hasTestTag(tag)).fetchSemanticsNodes().isNotEmpty()
  private fun selected(tag: String) = rule.onNodeWithTag(tag).fetchSemanticsNode().config[SemanticsProperties.Selected]

  // ------------------------------------------------------------------ el Primer vuelo

  private fun agePage(widthDp: Int, heightDp: Int, fontScale: Float, chosen: AgeRange?, onAge: (AgeRange) -> Unit = {}) {
    val state = FlightState(FlightMode.FULL, FlightStep.EDAD, name = "Ana", ageRange = chosen, age = chosen?.toBand())
    val actions = FlightActions({}, {}, {}, onAge, {}, {}, {}, {}, {}, {})
    rule.setContent {
      val d = LocalDensity.current
      CompositionLocalProvider(LocalDensity provides Density(d.density, fontScale), LocalReduceMotion provides true) {
        NeuroVidaTheme { Box(Modifier.width(widthDp.dp).height(heightDp.dp)) { BoardBackground(); FirstFlightScreen(state, emptyList(), actions) } }
      }
    }
    rule.waitForIdle()
  }

  @Test
  fun el_vuelo_pregunta_con_siete_opciones_sin_ninguna_marcada_y_con_Continuar_atenuado() {
    agePage(393, 873, 1f, chosen = null)
    rule.onNodeWithText("¿En qué rango de edad estás?").assertIsDisplayed()
    for ((range, label) in AgeRange.entries.zip(labels)) {
      rule.onNodeWithTag("flight_opt_${range.name}").assertExists()
      assertTrue("falta «$label»", rule.onAllNodes(androidx.compose.ui.test.hasText(label)).fetchSemanticsNodes().isNotEmpty())
      assertFalse("${range.name}: ninguna está marcada todavía", selected("flight_opt_${range.name}"))
    }
    assertEquals("siete opciones, ni una más", 7, rule.onAllNodes(androidx.compose.ui.test.hasTestTag("flight_opt_R13_17") or androidx.compose.ui.test.hasTestTag("flight_opt_R18_29") or androidx.compose.ui.test.hasTestTag("flight_opt_R30_44") or androidx.compose.ui.test.hasTestTag("flight_opt_R45_54") or androidx.compose.ui.test.hasTestTag("flight_opt_R55_64") or androidx.compose.ui.test.hasTestTag("flight_opt_R65_74") or androidx.compose.ui.test.hasTestTag("flight_opt_R75_PLUS")).fetchSemanticsNodes().size)
    assertEquals("la línea de ayuda de siempre se queda", 1, rule.onAllNodes(androidx.compose.ui.test.hasText(hint)).fetchSemanticsNodes().size)
    assertTrue("sin respuesta, Continuar está atenuado (no responde)", rule.onNodeWithTag("btn_flight_next").fetchSemanticsNode().config.contains(SemanticsProperties.Disabled))
    for (old in listOf("Menos de 18", "18 a 64", "65 o más")) assertEquals("«$old» (las tres de antes) ya no se ofrece en el vuelo", 0, rule.onAllNodes(androidx.compose.ui.test.hasText(old)).fetchSemanticsNodes().size)
  }

  @Test
  fun tocar_una_opcion_del_vuelo_entrega_esa_franja() {
    val picked = mutableListOf<AgeRange>()
    agePage(393, 873, 1f, chosen = null, onAge = { picked.add(it) })
    rule.onNodeWithTag("flight_opt_R45_54").performClick()
    rule.onNodeWithTag("flight_opt_R75_PLUS").performClick()
    assertEquals(listOf(AgeRange.R45_54, AgeRange.R75_PLUS), picked)
  }

  @Test
  fun con_una_franja_elegida_queda_marcada_solo_esa_y_Continuar_responde() {
    agePage(393, 873, 1f, chosen = AgeRange.R30_44)
    for (r in AgeRange.entries) assertEquals("${r.name}", r == AgeRange.R30_44, selected("flight_opt_${r.name}"))
    assertFalse("con respuesta, Continuar está activo", rule.onNodeWithTag("btn_flight_next").fetchSemanticsNode().config.contains(SemanticsProperties.Disabled))
  }

  @Test
  fun con_letra_1_0_la_pagina_de_la_edad_cabe_sin_deslizar_con_la_ayuda_entera_y_Continuar_a_la_vista() {
    agePage(393, 873, 1f, chosen = null)
    val first = rule.onNodeWithTag("flight_opt_R13_17").getUnclippedBoundsInRoot()
    val last = rule.onNodeWithTag("flight_opt_R75_PLUS").getUnclippedBoundsInRoot()
    val sub = rule.onNodeWithText(hint).getUnclippedBoundsInRoot()
    val button = rule.onNodeWithTag("btn_flight_next").getUnclippedBoundsInRoot()
    // el área que se desliza termina 10 dp sobre Continuar (el pie del marco deja 10 dp arriba del botón)
    val edge = button.top - 10.dp
    println("EDAD 393 x 873 letra 1,0: la primera opción empieza en %.0f dp, la última termina en %.0f dp, la ayuda termina en %.0f dp, el área que se desliza termina en %.0f dp y Continuar empieza en %.0f dp; cada opción mide %.0f dp".format(first.top.value, last.bottom.value, sub.bottom.value, edge.value, button.top.value, (first.bottom - first.top).value))
    // 1) cada opción mide 48 dp o más (el piso de toque)
    for (r in AgeRange.entries) assertTrue("${r.name}: mide 48 dp o más (mide ${rule.onNodeWithTag("flight_opt_${r.name}").getUnclippedBoundsInRoot().let { it.bottom - it.top }})", (rule.onNodeWithTag("flight_opt_${r.name}").getUnclippedBoundsInRoot().let { it.bottom - it.top }) >= 48.dp)
    assertEquals("y miden lo que dice la constante (52 dp)", 52f, (first.bottom - first.top).value, 0.6f)
    // 2) la última línea de la ayuda termina sobre el borde del área que se desliza: no hace falta deslizar para leerla
    assertTrue("la última línea de la ayuda termina en ${sub.bottom} y el área que se desliza termina en $edge", sub.bottom <= edge)
    assertTrue("las siete opciones entran sobre Continuar sin deslizar: la última termina en ${last.bottom}", last.bottom <= edge)
    rule.onNodeWithTag("btn_flight_next").assertIsDisplayed()
  }

  @Test
  fun la_separacion_entre_opciones_sigue_en_10_dp_y_el_texto_queda_centrado_en_su_fila_de_52() {
    agePage(393, 873, 1f, chosen = null)
    val rows = AgeRange.entries.map { rule.onNodeWithTag("flight_opt_${it.name}").getUnclippedBoundsInRoot() }
    for (i in 1 until rows.size) {
      val gap = (rows[i].top - rows[i - 1].bottom).value
      assertEquals("entre la opción ${i} y la ${i + 1} sigue la separación de antes: 10 dp de espacio más el relleno y el canto de la losa, 18 dp de borde a borde (mide %.1f dp)".format(gap), 18f, gap, 0.6f)
    }
    for ((r, label) in AgeRange.entries.zip(labels)) {
      val row = rule.onNodeWithTag("flight_opt_${r.name}").getUnclippedBoundsInRoot()
      val text = rule.onNodeWithText(label, useUnmergedTree = true).getUnclippedBoundsInRoot()
      val off = ((text.top + text.bottom) / 2 - (row.top + row.bottom) / 2).value
      assertTrue("«$label»: el texto queda centrado en su fila (desvío %.1f dp, la losa tiene su canto abajo)".format(off), kotlin.math.abs(off) <= 3f)
    }
  }

  private fun otherQuestion(step: FlightStep, tags: List<String>) {
    val state = FlightState(FlightMode.FULL, step, name = "Ana", age = AgeBand.ADULT, ageRange = AgeRange.R30_44, goals = setOf(com.example.model.DomainType.MEMORIA), days = 4)
    rule.setContent { NeuroVidaTheme { Box(Modifier.width(393.dp).height(873.dp)) { BoardBackground(); FirstFlightScreen(state, emptyList(), FlightActions.None) } } }
    rule.waitForIdle()
    for (tag in tags) {
      val b = rule.onNodeWithTag(tag).getUnclippedBoundsInRoot()
      assertTrue("$step: «$tag» mide 58 dp o más, como siempre (mide ${b.bottom - b.top})", (b.bottom - b.top) >= 57.5.dp)
    }
  }

  @Test fun las_metas_siguen_con_opciones_de_58_dp_o_mas() = otherQuestion(FlightStep.METAS, listOf("MEMORIA", "ATENCION", "RAZONAMIENTO", "LENGUAJE").map { "flight_opt_$it" })
  @Test fun los_dias_siguen_con_opciones_de_58_dp_o_mas() = otherQuestion(FlightStep.DIAS, listOf("3", "4", "5", "7").map { "flight_opt_$it" })
  @Test fun el_aviso_sigue_con_opciones_de_58_dp_o_mas() = otherQuestion(FlightStep.AVISO, listOf("9", "13", "19", "21", "-1").map { "flight_opt_$it" })

  private fun pageStillShowsEverything(w: Int, h: Int, scale: Float) {
    agePage(w, h, scale, chosen = null)
    val at = "${w}x$h letra $scale"
    rule.onNodeWithTag("btn_flight_next").assertIsDisplayed()                                  // el botón está siempre a la vista
    for (r in AgeRange.entries) {
      rule.onNodeWithTag("flight_opt_${r.name}").performScrollTo()
      rule.onNodeWithTag("flight_opt_${r.name}").assertIsDisplayed()
      val b = rule.onNodeWithTag("flight_opt_${r.name}").getUnclippedBoundsInRoot()
      val button = rule.onNodeWithTag("btn_flight_next").getUnclippedBoundsInRoot()
      assertTrue("$at: «${r.label}» (termina en ${b.bottom}) no se tapa con Continuar (empieza en ${button.top})", b.bottom <= button.top + 1.dp)
    }
    rule.onNodeWithText(hint).performScrollTo()
    val sub = rule.onNodeWithText(hint).getUnclippedBoundsInRoot()
    assertTrue("$at: la ayuda no se tapa con Continuar", sub.bottom <= rule.onNodeWithTag("btn_flight_next").getUnclippedBoundsInRoot().top + 1.dp)
  }

  @Test fun con_letra_1_3_se_desliza_y_no_se_corta_ni_se_tapa_nada() = pageStillShowsEverything(393, 873, 1.3f)
  @Test fun con_letra_2_0_se_desliza_y_no_se_corta_ni_se_tapa_nada() = pageStillShowsEverything(393, 873, 2f)
  @Test fun en_360_por_640_se_desliza_y_no_se_corta_ni_se_tapa_nada() = pageStillShowsEverything(360, 640, 1f)

  // ------------------------------------------------------------------ Ajustes

  private fun settingsOf(vm: NeuroVidaViewModel) {
    rule.mainClock.autoAdvance = true
    rule.setContent {
      CompositionLocalProvider(LocalReduceMotion provides true, LocalBarInset provides 88.dp) {
        NeuroVidaTheme { Box(Modifier.width(393.dp).height(3600.dp)) { BoardBackground(); SettingsScreen(viewModel = vm) } }
      }
    }
    rule.waitForIdle()
  }

  private fun vm() = NeuroVidaViewModel(app).also { viewModels.add(it) }
  private fun chip(r: AgeRange) = "chip_age_range_${r.name.lowercase()}"
  private fun prefs() = app.getSharedPreferences("profile_extra", android.content.Context.MODE_PRIVATE)

  @Test
  fun ajustes_muestra_las_mismas_siete_y_quien_solo_tiene_la_banda_de_antes_no_ve_ninguna_marcada() {
    runBlocking { NeuroVidaDatabase.getDatabase(app).userProfileDao().insertOrUpdate(UserSettings(ageBand = AgeBand.SENIOR, name = "Rosa").toEntity()) }
    val v = vm()
    settingsOf(v)
    TestSupport.awaitUntil(message = "El perfil no cargó") { v.userSettings.value.ageBand == AgeBand.SENIOR }
    assertNull("sin age_range", v.ageRange.value)
    for ((range, label) in AgeRange.entries.zip(labels)) {
      rule.onNodeWithTag(chip(range)).performScrollTo().assertIsDisplayed()
      assertTrue("falta «$label» en Ajustes", rule.onAllNodes(androidx.compose.ui.test.hasText(label)).fetchSemanticsNodes().isNotEmpty())
      assertFalse("${range.name}: con solo la banda vieja no se marca ninguna", selected(chip(range)))
      assertTrue("${range.name}: mide 48 dp o más", rule.onNodeWithTag(chip(range)).getUnclippedBoundsInRoot().let { it.bottom - it.top } >= 48.dp)
    }
    for (old in listOf("Menos de 18", "18 a 64", "65 o más")) assertEquals("«$old» ya no se ofrece en Ajustes", 0, rule.onAllNodes(androidx.compose.ui.test.hasText(old)).fetchSemanticsNodes().size)
    assertEquals("la banda del DDA no se tocó al abrir Ajustes", AgeBand.SENIOR, v.userSettings.value.ageBand)
    assertNull("y no se escribió ninguna franja", prefs().getString("age_range", null))
  }

  @Test
  fun elegir_una_franja_en_Ajustes_cambia_las_dos_cosas_y_deja_marcada_solo_esa() {
    runBlocking { NeuroVidaDatabase.getDatabase(app).userProfileDao().insertOrUpdate(UserSettings(ageBand = AgeBand.ADULT, name = "Ana").toEntity()) }
    val v = vm()
    settingsOf(v)
    rule.onNodeWithTag(chip(AgeRange.R65_74)).performScrollTo().performClick()
    TestSupport.awaitUntil(message = "No cambió a 65 a 74") { v.ageRange.value == AgeRange.R65_74 && v.userSettings.value.ageBand == AgeBand.SENIOR }
    assertEquals("R65_74", prefs().getString("age_range", null))
    rule.waitForIdle()
    for (r in AgeRange.entries) assertEquals(r.name, r == AgeRange.R65_74, selected(chip(r)))
    // y de una franja a otra de la misma banda solo cambia la fina
    rule.onNodeWithTag(chip(AgeRange.R75_PLUS)).performScrollTo().performClick()
    TestSupport.awaitUntil { v.ageRange.value == AgeRange.R75_PLUS }
    assertEquals(AgeBand.SENIOR, v.userSettings.value.ageBand)
    // y a la de menores de 18
    rule.onNodeWithTag(chip(AgeRange.R13_17)).performScrollTo().performClick()
    TestSupport.awaitUntil { v.ageRange.value == AgeRange.R13_17 && v.userSettings.value.ageBand == AgeBand.UNDER_18 }
    assertEquals("R13_17", prefs().getString("age_range", null))
  }

  @Test
  fun la_franja_elegida_en_Ajustes_sigue_marcada_al_volver_a_abrir_la_app() {
    runBlocking { NeuroVidaDatabase.getDatabase(app).userProfileDao().insertOrUpdate(UserSettings(ageBand = AgeBand.ADULT, name = "Ana").toEntity()) }
    val first = vm()
    first.setAgeRange(AgeRange.R55_64)
    TestSupport.awaitUntil { prefs().getString("age_range", null) == "R55_64" }
    val second = vm()                                                                          // la app se abre de nuevo
    assertEquals(AgeRange.R55_64, second.ageRange.value)
    settingsOf(second)
    TestSupport.awaitUntil { second.userSettings.value.ageBand == AgeBand.ADULT }
    assertTrue(selected(chip(AgeRange.R55_64)))
  }
}
