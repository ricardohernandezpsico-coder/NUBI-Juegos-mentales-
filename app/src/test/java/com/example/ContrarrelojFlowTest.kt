package com.example

import android.app.Application
import androidx.activity.ComponentActivity
import androidx.compose.foundation.layout.Box
import androidx.compose.foundation.layout.height
import androidx.compose.foundation.layout.width
import androidx.compose.runtime.CompositionLocalProvider
import androidx.compose.runtime.mutableIntStateOf
import androidx.compose.runtime.remember
import androidx.compose.ui.Modifier
import androidx.compose.ui.semantics.SemanticsProperties
import androidx.compose.ui.semantics.getOrNull
import androidx.compose.ui.test.assertIsDisplayed
import androidx.compose.ui.test.hasTestTag
import androidx.compose.ui.test.junit4.createAndroidComposeRule
import androidx.compose.ui.test.onNodeWithTag
import androidx.compose.ui.test.performClick
import androidx.compose.ui.unit.dp
import androidx.test.core.app.ApplicationProvider
import com.example.data.NBSP
import com.example.data.PlusAccess
import com.example.data.TimeTrialDay
import com.example.data.TimeTrialResult
import com.example.data.TimeTrialStats
import com.example.data.local.NeuroVidaDatabase
import com.example.data.local.toEntity
import com.example.games.TimeTrialResultScreen
import com.example.model.AgeBand
import com.example.model.GamePlayResult
import com.example.model.UserSettings
import com.example.ui.components.LocalReduceMotion
import com.example.ui.screens.ProgressScreen
import com.example.ui.theme.NeuroVidaTheme
import com.example.viewmodel.AppTab
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
 * La puerta y la página del Contrarreloj en Avance, con la pantalla REAL y su ViewModel (Tarea 199): con Nubi Plus, la losa entre «Tus áreas» y «Tus mejores marcas» dice el récord de hoy y abre la página (reemplaza la lista, Atrás vuelve, la barra sigue en «Avance», cambiar de pestaña la cierra); sin Plus, la misma losa
 * lleva el candado «Con Nubi Plus» y abre una hoja corta con un solo botón; la página no se abre sin Plus aunque el ViewModel la tenga abierta; Plus arranca encendido en depuración y es una preferencia pasajera; el atajo «Todas tus marcas ›» del final del Contrarreloj existe solo con Plus.
 */
@RunWith(RobolectricTestRunner::class)
@GraphicsMode(GraphicsMode.Mode.NATIVE)
@Config(qualifiers = "w393dp-h873dp-440dpi", sdk = [36])
class ContrarrelojFlowTest {
  @get:Rule val rule = createAndroidComposeRule<ComponentActivity>()

  private lateinit var app: Application
  private val viewModels = mutableListOf<NeuroVidaViewModel>()
  private val day = 24L * 60 * 60 * 1000

  @Before fun setUp() {
    app = ApplicationProvider.getApplicationContext()
    TestSupport.resetDatabase()
    TestSupport.fastFrames()
  }

  @After fun tearDown() {
    TestSupport.release(*viewModels.toTypedArray())
    viewModels.clear()
    TestSupport.resetDatabase()
  }

  /** Dos juegos jugados de siempre (para que Avance tenga áreas y «Tus mejores marcas») y diez marcas del Contrarreloj del juego de HOY (44 es el récord) más una de otro juego. */
  private fun withData(history: Boolean = true): NeuroVidaViewModel {
    val repo = (app as NeuroVidaApplication).repository
    val real = System.currentTimeMillis()
    var n = 0
    runBlocking {
      val db = NeuroVidaDatabase.getDatabase(app)
      db.userProfileDao().insertOrUpdate(UserSettings(ageBand = AgeBand.ADULT, name = "Ricardo", weeklyGoal = 4).toEntity())
      if (history) for ((k, id) in listOf("secuencia", "stroop").withIndex()) for (round in 0 until 3) {
        repo.recordGameResult(
          GamePlayResult(gameId = id, score = 70, correctAnswers = 8, totalTrials = 12, timed = false, level = 2, timestamp = real - (21 - 7 * round) * day + k * 3_600_000L, endRating = 0.25f + 0.08f * round, modeTrials = 10, modeHits = 8),
          countsForDailySession = false
        )
        n += 1
      }
    }
    val vm = NeuroVidaViewModel(app).also { viewModels.add(it) }
    if (n > 0) TestSupport.awaitUntil(30_000) { vm.gameHistory.value.size >= n }
    val today = TimeTrialDay.gameFor(vm.todayKey())
    val other = TimeTrialDay.GAMES.first { it != today }
    runBlocking {
      listOf(31, 35, 39, 36, 44, 38, 41, 37, 42, 43).forEachIndexed { i, m ->
        repo.recordTimeTrial(TimeTrialResult(id = "flow-$i", gameId = today, day = vm.todayKey(), trackVersion = TimeTrialDay.trackVersion(today), hits = m + 2, errors = 2, level = 4, seconds = 120, timestamp = real - (10 - i) * day))
      }
      repo.recordTimeTrial(TimeTrialResult(id = "flow-other", gameId = other, day = vm.todayKey(), trackVersion = TimeTrialDay.trackVersion(other), hits = 29, errors = 2, level = 3, seconds = 120, timestamp = real - day))
    }
    TestSupport.awaitUntil(30_000) { vm.timeTrialResults.value.size >= 11 }
    TestSupport.awaitUntil(30_000, "La referencia estimada no se cargó") { vm.estimatedReference.value != null }          // la lee el ViewModel fuera del hilo principal (Tarea 205c)
    vm.setTab(AppTab.PROGRESO)
    return vm
  }

  private fun show(vm: NeuroVidaViewModel, heightDp: Int = 1500) {
    rule.setContent {
      NeuroVidaTheme {
        CompositionLocalProvider(LocalReduceMotion provides true, com.example.ui.components.LocalTabSwipeLock provides remember { mutableIntStateOf(0) }) {
          Box(Modifier.width(393.dp).height(heightDp.dp)) { ProgressScreen(viewModel = vm) }
        }
      }
    }
    rule.waitForIdle()
  }

  private fun count(tag: String) = rule.onAllNodes(hasTestTag(tag)).fetchSemanticsNodes().size
  private fun textOf(tag: String): String = rule.onNode(hasTestTag(tag), useUnmergedTree = true).fetchSemanticsNode().config.getOrNull(SemanticsProperties.Text)?.joinToString("") { it.text }.orEmpty()
  private fun pressBack() { rule.runOnUiThread { rule.activity.onBackPressedDispatcher.onBackPressed() }; rule.waitForIdle() }
  private fun top(tag: String) = rule.onNodeWithTag(tag, useUnmergedTree = true).fetchSemanticsNode().positionInRoot.y

  @Test
  fun con_Plus_la_puerta_va_entre_las_areas_y_las_mejores_marcas_y_dice_el_record_de_hoy() {
    val vm = withData(); vm.setPlus(true)
    show(vm)
    rule.onNodeWithTag("avance_contrarreloj_door").assertIsDisplayed()
    assertEquals("Tu récord de hoy: 44 aciertos", textOf("avance_contrarreloj_line").replace(NBSP, " "))
    assertEquals("Tus marcas del Contrarreloj", textOf("avance_contrarreloj_title"))
    assertEquals("sin candado con Plus", 0, rule.onAllNodes(hasTestTag("avance_contrarreloj_lock"), useUnmergedTree = true).fetchSemanticsNodes().size)
    assertEquals("con el chevron del kit", 1, rule.onAllNodes(hasTestTag("avance_contrarreloj_chevron"), useUnmergedTree = true).fetchSemanticsNodes().size)
    assertTrue("después de «Tus áreas»", top("progress_area_lenguaje") < top("avance_contrarreloj_door"))
    assertTrue("y antes de «Tus mejores marcas»", top("avance_contrarreloj_door") < top("best_marks_empty"))
    // sin título de sección ni línea de ayuda sueltas sobre el cielo (Fable §11): el único texto es el de la losa
    assertEquals(0, rule.onAllNodes(androidx.compose.ui.test.hasText("Tus marcas de los cuatro juegos", substring = true)).fetchSemanticsNodes().size)
  }

  @Test
  fun con_Plus_tocar_la_puerta_abre_la_pagina_con_el_juego_de_hoy_y_Atras_vuelve_a_la_lista() {
    val vm = withData(); vm.setPlus(true)
    show(vm)
    val today = TimeTrialDay.gameFor(vm.todayKey())
    rule.onNodeWithTag("avance_contrarreloj_door").performClick(); rule.waitForIdle()
    assertEquals("la página abre con el juego de hoy elegido", today, vm.contrarreloj.value)
    assertEquals("la barra sigue en Avance", AppTab.PROGRESO, vm.currentTab.value)
    rule.onNodeWithTag("contrarreloj_page").assertIsDisplayed()
    assertEquals("la lista de áreas no se ve mientras la página está abierta", 0, count("progress_area_memoria"))
    assertEquals("44", textOf("contrarreloj_stat_0_figure").replace(NBSP, " "))
    assertEquals("aciertos, tu récord", textOf("contrarreloj_stat_0_label"))
    pressBack()
    assertNull("Atrás cierra la página", vm.contrarreloj.value)
    rule.onNodeWithTag("avance_contrarreloj_door").assertExists()
    assertEquals(0, count("contrarreloj_page"))
  }

  @Test
  fun el_disco_de_volver_cierra_y_elegir_otro_juego_cambia_la_pagina() {
    val vm = withData(); vm.setPlus(true)
    show(vm)
    rule.onNodeWithTag("avance_contrarreloj_door").performClick(); rule.waitForIdle()
    val today = TimeTrialDay.gameFor(vm.todayKey())
    val other = TimeTrialDay.GAMES.first { it != today }
    rule.onNodeWithTag("contrarreloj_game_$other").performClick(); rule.waitForIdle()
    assertEquals(other, vm.contrarreloj.value)
    assertEquals("la página muestra el récord de ese juego", "27", textOf("contrarreloj_stat_0_figure").replace(NBSP, " "))
    rule.onNodeWithTag("contrarreloj_back", useUnmergedTree = true).performClick(); rule.waitForIdle()
    assertNull(vm.contrarreloj.value)
    rule.onNodeWithTag("avance_contrarreloj_door").assertExists()
  }

  @Test
  fun sin_Plus_la_puerta_lleva_el_candado_y_abre_una_hoja_con_un_solo_boton() {
    val vm = withData(); vm.setPlus(false); vm.setAgeRange(com.example.model.AgeRange.R45_54)
    TestSupport.awaitUntil(30_000) { vm.ageRange.value == com.example.model.AgeRange.R45_54 }
    show(vm)
    assertEquals("sigue diciendo el récord de hoy", "Tu récord de hoy: 44 aciertos", textOf("avance_contrarreloj_line").replace(NBSP, " "))
    assertEquals("y lleva el candado", 1, rule.onAllNodes(hasTestTag("avance_contrarreloj_lock"), useUnmergedTree = true).fetchSemanticsNodes().size)
    assertEquals("con comparación, el candado dice qué agrega Plus (Tarea 209)", "Con Nubi Plus: además, dónde queda tu marca frente a tu edad y a otras edades.", textOf("avance_contrarreloj_lock_label"))
    assertEquals("sin chevron", 0, rule.onAllNodes(hasTestTag("avance_contrarreloj_chevron"), useUnmergedTree = true).fetchSemanticsNodes().size)
    rule.onNodeWithTag("avance_contrarreloj_door").performClick(); rule.waitForIdle()
    assertNull("sin Plus no se abre la página", vm.contrarreloj.value)
    rule.onNodeWithTag("plus_sheet").assertIsDisplayed()
    assertEquals(PlusAccess.SHEET_TITLE, textOf("plus_sheet_title"))
    // desde la Tarea 205 la referencia estimada (de fábrica en depuración y en la tienda) llega a la hoja: habla de la referencia estimada, nunca de personas, y trae TRES líneas
    assertEquals(PlusAccess.sheetLead(com.example.data.Comparison.Kind.ESTIMATED), textOf("plus_sheet_lead"))
    assertEquals("tres líneas de lo que trae", 3, (0 until 12).count { rule.onAllNodes(hasTestTag("plus_sheet_item_$it"), useUnmergedTree = true).fetchSemanticsNodes().isNotEmpty() })
    assertEquals(PlusAccess.sheetItems(com.example.data.Comparison.Kind.ESTIMATED), (0 until 3).map { textOf("plus_sheet_item_$it") })
    assertEquals("Cerrar", rule.onNodeWithTag("btn_plus_sheet_ok").fetchSemanticsNode().config.getOrNull(SemanticsProperties.Text)?.joinToString("") { it.text })
    rule.onNodeWithTag("btn_plus_sheet_ok").performClick(); rule.waitForIdle()
    assertEquals("la hoja se cierra", 0, count("plus_sheet"))
    assertNull(vm.contrarreloj.value)
  }

  /** Abre la hoja del candado (sin Plus) con la franja [range] guardada (null = ninguna) y devuelve lo que dice. */
  private fun sheetTexts(range: com.example.model.AgeRange?): Pair<String, List<String>> {
    val vm = withData(); vm.setPlus(false)
    if (range != null) { vm.setAgeRange(range); TestSupport.awaitUntil(30_000) { vm.ageRange.value == range } }
    show(vm)
    rule.onNodeWithTag("avance_contrarreloj_door").performClick(); rule.waitForIdle()
    rule.onNodeWithTag("plus_sheet").assertIsDisplayed()
    return textOf("plus_sheet_lead") to (0 until 3).map { textOf("plus_sheet_item_$it") }
  }

  @Test
  fun la_hoja_del_candado_nombra_la_referencia_solo_si_la_persona_tiene_una_franja_con_que_compararse() {
    val (lead, items) = sheetTexts(com.example.model.AgeRange.R45_54)
    assertEquals(PlusAccess.sheetLead(com.example.data.Comparison.Kind.ESTIMATED), lead)
    assertEquals(PlusAccess.sheetItems(com.example.data.Comparison.Kind.ESTIMATED), items)
  }

  @Test
  fun la_hoja_del_candado_de_13_a_17_anos_no_le_promete_una_referencia_que_nunca_vera() {
    val (lead, items) = sheetTexts(com.example.model.AgeRange.R13_17)
    assertEquals("solo contigo y lo dice (Tarea 209)", PlusAccess.sheetLead(com.example.data.Comparison.Kind.NONE, minor = true), lead)
    assertEquals("Una página con todas tus marcas. De 13 a 17 años se comparan solo contigo:", lead)
    assertEquals(PlusAccess.sheetItems(com.example.data.Comparison.Kind.NONE), items)
    assertFalse("ni una palabra de la referencia", (listOf(lead) + items).any { "referencia" in it })
    assertEquals("y sin la nota ni la fila de la referencia", 0, count("plus_sheet_note") + count("plus_sheet_source"))
    assertEquals("la puerta, con el candado corto: no hay un «además» que ofrecer", PlusAccess.LOCK_LABEL, textOf("avance_contrarreloj_lock_label"))
  }

  @Test
  fun la_hoja_del_candado_dice_la_razon_y_su_fila_abre_de_donde_sale_la_referencia() {
    val vm = withData(); vm.setPlus(false); vm.setAgeRange(com.example.model.AgeRange.R45_54)
    TestSupport.awaitUntil(30_000) { vm.ageRange.value == com.example.model.AgeRange.R45_54 }
    show(vm)
    rule.onNodeWithTag("avance_contrarreloj_door").performClick(); rule.waitForIdle()
    assertEquals("Gratis: tu división de la semana. Con Nubi Plus, además:", textOf("plus_sheet_lead"))
    assertEquals(PlusAccess.SHEET_NOTE_ESTIMATED, textOf("plus_sheet_note"))
    assertEquals("la razón va antes de la fila y la fila antes de «Cerrar»", true, top("plus_sheet_note") < top("plus_sheet_source") && top("plus_sheet_source") < top("btn_plus_sheet_ok"))
    rule.onNode(androidx.compose.ui.test.hasText("De dónde sale la referencia ›"), useUnmergedTree = true).assertExists()
    rule.onNodeWithTag("plus_sheet_source").performClick(); rule.waitForIdle()
    assertEquals("la hoja se cierra y se abre el panel de los estudios", com.example.viewmodel.TopPanel.REFERENCIA, vm.topPanel.value)
    assertEquals(0, count("plus_sheet"))
  }

  @Test
  fun la_hoja_del_candado_sin_franja_tampoco_promete_una_referencia() {
    val (lead, items) = sheetTexts(null)
    assertEquals(PlusAccess.sheetLead(com.example.data.Comparison.Kind.NONE), lead)
    assertEquals(PlusAccess.sheetItems(com.example.data.Comparison.Kind.NONE), items)
  }

  @Test
  fun la_referencia_estimada_la_carga_el_ViewModel_y_ProgressScreen_ya_no_lee_assets() {
    val vm = withData()
    assertEquals("las cuatro tablas del APK", 4, vm.estimatedReference.value!!.loaded.size)
    val screen = java.io.File("src/main/java/com/example/ui/screens/ProgressScreen.kt").readText()
    assertFalse("la pantalla no lee assets al componer (hilo principal)", screen.contains("EstimatedReference.get("))
    assertTrue("la toma del ViewModel", screen.contains("viewModel.estimatedReference"))
    val vmSource = java.io.File("src/main/java/com/example/viewmodel/NeuroVidaViewModel.kt").readText()
    assertTrue("y el ViewModel la lee en IO", vmSource.contains("launch(kotlinx.coroutines.Dispatchers.IO)") && vmSource.contains("EstimatedReference.get(getApplication())"))
  }

  @Test
  fun sin_Plus_la_puerta_dice_gratis_la_division_y_lo_que_falta_cuando_hay_una_marca_esta_semana() {
    val vm = withData(); vm.setPlus(false); vm.setAgeRange(com.example.model.AgeRange.R45_54)
    TestSupport.awaitUntil(30_000) { vm.ageRange.value == com.example.model.AgeRange.R45_54 }
    val repo = (app as NeuroVidaApplication).repository
    val today = TimeTrialDay.gameFor(vm.todayKey())
    // una marca de ESTE momento (de la semana en curso, sea cual sea el día): 99 aciertos está en el último tramo de cualquier franja de la referencia estimada (desde la Tarea 205 la tienda y la depuración de fábrica comparan con ella)
    runBlocking { repo.recordTimeTrial(TimeTrialResult(id = "flow-now", gameId = today, day = vm.todayKey(), trackVersion = TimeTrialDay.trackVersion(today), hits = 101, errors = 2, level = 5, seconds = 120, timestamp = System.currentTimeMillis())) }
    TestSupport.awaitUntil(30_000) { vm.timeTrialResults.value.size >= 12 }
    show(vm)
    assertEquals("Esta semana estás Entre estrellas, la quinta y más alta de las cinco divisiones de la referencia estimada para tu edad.", textOf("avance_contrarreloj_free").replace(NBSP, " "))
    assertEquals("la puerta se lee de corrido con la frase gratis y el candado largo", "Tus marcas del Contrarreloj. Tu récord de hoy: 99 aciertos. Esta semana estás Entre estrellas, la quinta y más alta de las cinco divisiones de la referencia estimada para tu edad. Con Nubi Plus: además, dónde queda tu marca frente a tu edad y a otras edades.",
      rule.onNodeWithTag("avance_contrarreloj_door").fetchSemanticsNode().config.getOrNull(SemanticsProperties.ContentDescription)?.joinToString(". ")?.replace(NBSP, " "))
    assertEquals("el candado sigue (la página entera es Plus)", 1, rule.onAllNodes(hasTestTag("avance_contrarreloj_lock"), useUnmergedTree = true).fetchSemanticsNodes().size)
    assertTrue("la frase va entre el récord y el candado", top("avance_contrarreloj_line") < top("avance_contrarreloj_free") && top("avance_contrarreloj_free") < top("avance_contrarreloj_lock"))
  }

  @Test
  fun con_Plus_la_puerta_no_repite_la_frase_gratis_porque_la_pagina_la_trae_completa() {
    val vm = withData(); vm.setPlus(true); vm.setAgeRange(com.example.model.AgeRange.R45_54)
    TestSupport.awaitUntil(30_000) { vm.ageRange.value == com.example.model.AgeRange.R45_54 }
    val repo = (app as NeuroVidaApplication).repository
    val today = TimeTrialDay.gameFor(vm.todayKey())
    runBlocking { repo.recordTimeTrial(TimeTrialResult(id = "flow-now", gameId = today, day = vm.todayKey(), trackVersion = TimeTrialDay.trackVersion(today), hits = 101, errors = 2, level = 5, seconds = 120, timestamp = System.currentTimeMillis())) }
    TestSupport.awaitUntil(30_000) { vm.timeTrialResults.value.size >= 12 }
    show(vm)
    assertEquals(0, rule.onAllNodes(hasTestTag("avance_contrarreloj_free"), useUnmergedTree = true).fetchSemanticsNodes().size)
  }

  @Test
  fun sin_una_franja_de_edad_elegida_no_hay_con_quien_compararse_y_la_puerta_no_dice_la_frase_gratis() {
    val vm = withData(); vm.setPlus(false)
    val repo = (app as NeuroVidaApplication).repository
    val today = TimeTrialDay.gameFor(vm.todayKey())
    runBlocking { repo.recordTimeTrial(TimeTrialResult(id = "flow-now", gameId = today, day = vm.todayKey(), trackVersion = TimeTrialDay.trackVersion(today), hits = 101, errors = 2, level = 5, seconds = 120, timestamp = System.currentTimeMillis())) }
    TestSupport.awaitUntil(30_000) { vm.timeTrialResults.value.size >= 12 }
    assertNull("sin franja guardada (quien solo tenía la banda vieja)", vm.ageRange.value)
    show(vm)
    assertEquals("la referencia estimada es por franja: sin ella no hay frase gratis", 0, rule.onAllNodes(hasTestTag("avance_contrarreloj_free"), useUnmergedTree = true).fetchSemanticsNodes().size)
  }

  @Test
  fun sin_una_marca_esta_semana_la_puerta_sin_Plus_no_inventa_la_frase_gratis() {
    val vm = withData(history = false); vm.setPlus(false)
    // withData(): las marcas están en los 10 días anteriores; la de hace un día cae en la semana anterior los lunes, así que el texto solo se mira si no hay frase
    show(vm)
    val free = rule.onAllNodes(hasTestTag("avance_contrarreloj_free"), useUnmergedTree = true).fetchSemanticsNodes()
    assertTrue("la frase gratis es una sola, o no hay", free.size <= 1)
    free.firstOrNull()?.config?.getOrNull(SemanticsProperties.Text)?.joinToString("")?.let { assertTrue("«$it»", it.replace(NBSP, " ").startsWith("Esta semana estás ")) }
  }

  @Test
  fun sin_Plus_aunque_el_ViewModel_tenga_la_pagina_abierta_se_ve_la_lista() {
    val vm = withData(); vm.setPlus(false)
    vm.openContrarreloj()
    show(vm)
    assertEquals(0, count("contrarreloj_page"))
    rule.onNodeWithTag("avance_contrarreloj_door").assertExists()
  }

  @Test
  fun cambiar_de_pestana_cierra_la_pagina() {
    val vm = withData(); vm.setPlus(true)
    vm.openContrarreloj()
    assertEquals(TimeTrialDay.gameFor(vm.todayKey()), vm.contrarreloj.value)
    vm.setTab(AppTab.HOY)
    assertNull(vm.contrarreloj.value)
  }

  @Test
  fun Todas_tus_marcas_deja_el_final_va_a_Avance_y_abre_la_pagina_con_ese_juego() {
    val vm = withData(); vm.setPlus(true)
    vm.setTab(AppTab.HOY)
    val other = TimeTrialDay.GAMES.last()
    vm.openContrarrelojFromFinal(other)
    assertEquals(AppTab.PROGRESO, vm.currentTab.value)
    assertEquals(other, vm.contrarreloj.value)
    assertNull("ya no hay un final a la vista", vm.lastResult.value)
    show(vm)
    rule.onNodeWithTag("contrarreloj_page").assertIsDisplayed()
    pressBack()
    assertNull("Atrás vuelve a la lista de Avance", vm.contrarreloj.value)
  }

  @Test
  fun quien_solo_jugo_el_Contrarreloj_tambien_ve_la_puerta() {
    val vm = withData(history = false); vm.setPlus(true)
    show(vm)
    rule.onNodeWithTag("avance_contrarreloj_door").assertExists()
  }

  @Test
  fun Plus_arranca_encendido_en_depuracion_es_una_preferencia_pasajera_y_el_boton_la_cambia() {
    assertTrue("las builds de depuración empiezan con Plus encendido", com.example.BuildConfig.DEBUG)
    val vm = withData(history = false)
    assertTrue(vm.plus.value)
    vm.setPlus(false)
    assertFalse(vm.plus.value)
    assertEquals(false, app.getSharedPreferences(PlusAccess.PREFS, android.content.Context.MODE_PRIVATE).getBoolean(PlusAccess.KEY, true))
    vm.setPlus(true)
    assertEquals(true, app.getSharedPreferences(PlusAccess.PREFS, android.content.Context.MODE_PRIVATE).getBoolean(PlusAccess.KEY, false))
  }

  // ---------------------------------------------------------------- el atajo del final

  private val real = System.currentTimeMillis()
  private fun result(mark: Int, daysAgo: Int) = TimeTrialResult(id = "fin-$daysAgo", gameId = "stroop", day = "2026-10-01", trackVersion = 2, hits = mark, errors = 0, level = 4, seconds = 120, timestamp = real - daysAgo * day)

  private fun showFinal(plus: Boolean, onAllMarks: (() -> Unit)?) {
    val all = listOf(result(31, 4), result(35, 3), result(41, 2), result(36, 1), result(43, 0))
    val outcome = TimeTrialStats.outcome(all, all.last(), real)
    rule.setContent {
      CompositionLocalProvider(LocalReduceMotion provides true) {
        NeuroVidaTheme { Box(Modifier.width(393.dp).height(1200.dp)) { TimeTrialResultScreen(outcome = outcome, onAgain = {}, onDone = {}, soundEnabled = false, plus = plus, onAllMarks = onAllMarks) } }
      }
    }
    rule.waitForIdle()
  }

  @Test
  fun el_final_con_Plus_lleva_Todas_tus_marcas_bajo_la_mejor_de_la_semana_y_lo_toca() {
    var taps = 0
    showFinal(plus = true) { taps += 1 }
    rule.onNodeWithTag("btn_timetrial_all_marks").assertIsDisplayed()
    assertEquals("Todas tus marcas", rule.onNodeWithTag("btn_timetrial_all_marks").fetchSemanticsNode().config.getOrNull(SemanticsProperties.ContentDescription)?.joinToString())
    val shortcut = rule.onNodeWithTag("btn_timetrial_all_marks").fetchSemanticsNode()
    val week = rule.onNodeWithTag("timetrial_week").fetchSemanticsNode()
    assertTrue("bajo «La mejor de esta semana»", shortcut.positionInRoot.y >= week.positionInRoot.y + week.size.height)
    assertTrue("y antes de «Otra vez»", shortcut.positionInRoot.y + shortcut.size.height <= rule.onNodeWithTag("btn_timetrial_again").fetchSemanticsNode().positionInRoot.y)
    assertTrue("la zona que se toca mide 48 dp o más (la losa pide 54 y 6 dp son la sombra de abajo, que no se toca; mide ${shortcut.size.height} px a ${app.resources.displayMetrics.density} px por dp)", shortcut.size.height >= 48 * app.resources.displayMetrics.density - 1)
    rule.onNodeWithTag("btn_timetrial_all_marks").performClick(); rule.waitForIdle()
    assertEquals(1, taps)
  }

  @Test
  fun el_final_sin_Plus_queda_como_siempre_sin_el_atajo() {
    showFinal(plus = false) {}
    assertEquals(0, count("btn_timetrial_all_marks"))
    rule.onNodeWithTag("btn_timetrial_again").assertIsDisplayed()
  }

  @Test
  fun el_final_sin_destino_tampoco_lleva_el_atajo() {
    showFinal(plus = true, onAllMarks = null)
    assertEquals(0, count("btn_timetrial_all_marks"))
  }

  // ---------------------------------------------------------------- Tarea 207: el lugar aparece desde la tercera partida de cada juego

  /** Un ViewModel con el perfil, la franja de 45 a 54 años y [count] partidas del Contrarreloj del juego de HOY, todas de este momento (de esta semana). */
  private fun withGames(count: Int, plus: Boolean): NeuroVidaViewModel {
    val repo = (app as NeuroVidaApplication).repository
    runBlocking { NeuroVidaDatabase.getDatabase(app).userProfileDao().insertOrUpdate(UserSettings(ageBand = AgeBand.ADULT, name = "Ricardo", weeklyGoal = 4).toEntity()) }
    val vm = NeuroVidaViewModel(app).also { viewModels.add(it) }
    vm.setPlus(plus); vm.setAgeRange(com.example.model.AgeRange.R45_54)
    TestSupport.awaitUntil(30_000) { vm.ageRange.value == com.example.model.AgeRange.R45_54 }
    repeat(count) { recordToday(vm, repo, it) }
    if (count > 0) TestSupport.awaitUntil(30_000) { vm.timeTrialResults.value.size >= count }
    TestSupport.awaitUntil(30_000, "La referencia estimada no se cargó") { vm.estimatedReference.value != null }
    vm.setTab(AppTab.PROGRESO)
    return vm
  }

  private fun recordToday(vm: NeuroVidaViewModel, repo: com.example.data.NeuroVidaRepository, i: Int) {
    val today = TimeTrialDay.gameFor(vm.todayKey())
    runBlocking { repo.recordTimeTrial(TimeTrialResult(id = "t207-$i", gameId = today, day = vm.todayKey(), trackVersion = TimeTrialDay.trackVersion(today), hits = 101, errors = 2, level = 5, seconds = 120, timestamp = System.currentTimeMillis() - (10 - i) * 1000L)) }
  }

  private fun freeNodes() = rule.onAllNodes(hasTestTag("avance_contrarreloj_free"), useUnmergedTree = true).fetchSemanticsNodes().size

  @Test
  fun sin_Plus_la_puerta_calla_con_menos_de_tres_partidas_del_juego_de_hoy_y_habla_desde_la_tercera() {
    val vm = withGames(2, plus = false)
    val repo = (app as NeuroVidaApplication).repository
    show(vm)
    assertEquals("con dos partidas la puerta calla, como sin franja de edad", 0, freeNodes())
    val spoken = rule.onNodeWithTag("avance_contrarreloj_door").fetchSemanticsNode().config.getOrNull(SemanticsProperties.ContentDescription)?.joinToString(". ")?.replace(NBSP, " ").orEmpty()
    assertEquals("y se lee solo el récord y el candado (largo: hay comparación para esta persona)", "Tus marcas del Contrarreloj. Tu récord de hoy: 99 aciertos. Con Nubi Plus: además, dónde queda tu marca frente a tu edad y a otras edades.", spoken)
    assertEquals("el candado sigue", 1, rule.onAllNodes(hasTestTag("avance_contrarreloj_lock"), useUnmergedTree = true).fetchSemanticsNodes().size)
    recordToday(vm, repo, 2)
    TestSupport.awaitUntil(30_000) { vm.timeTrialResults.value.size >= 3 }
    rule.waitForIdle()
    assertEquals("con la tercera, la frase gratis de siempre", 1, freeNodes())
    assertEquals("Esta semana estás Entre estrellas, la quinta y más alta de las cinco divisiones de la referencia estimada para tu edad.", textOf("avance_contrarreloj_free").replace(NBSP, " "))
  }

  @Test
  fun con_Plus_la_pagina_dice_la_frase_con_dos_partidas_y_el_lugar_desde_la_tercera() {
    val vm = withGames(2, plus = true)
    val repo = (app as NeuroVidaApplication).repository
    vm.openContrarreloj()
    show(vm)
    assertEquals("Tu lugar aparece desde tu tercera partida: las primeras sirven para conocer el juego.", textOf("contrarreloj_early_notice").replace(NBSP, " "))
    assertEquals("sin lugar: ni cifra ni escalera ni campana", 0, count("contrarreloj_hero") + count("contrarreloj_ladder") + count("contrarreloj_bell"))
    recordToday(vm, repo, 2)
    TestSupport.awaitUntil(30_000) { vm.timeTrialResults.value.size >= 3 }
    rule.waitForIdle()
    assertEquals("con la tercera ya no está la frase", 0, rule.onAllNodes(hasTestTag("contrarreloj_early_notice"), useUnmergedTree = true).fetchSemanticsNodes().size)
    assertEquals("y aparece el lugar", 1, rule.onAllNodes(hasTestTag("contrarreloj_hero"), useUnmergedTree = true).fetchSemanticsNodes().size)
  }

  @Test
  fun sin_franja_la_pagina_dice_donde_elegirla_aunque_tenga_menos_de_tres_partidas() {
    val repo = (app as NeuroVidaApplication).repository
    runBlocking { NeuroVidaDatabase.getDatabase(app).userProfileDao().insertOrUpdate(UserSettings(ageBand = AgeBand.ADULT, name = "Ricardo", weeklyGoal = 4).toEntity()) }
    val vm = NeuroVidaViewModel(app).also { viewModels.add(it) }
    vm.setPlus(true)
    recordToday(vm, repo, 0)
    TestSupport.awaitUntil(30_000) { vm.timeTrialResults.value.size >= 1 }
    TestSupport.awaitUntil(30_000, "La referencia estimada no se cargó") { vm.estimatedReference.value != null }
    assertNull(vm.ageRange.value)
    vm.setTab(AppTab.PROGRESO); vm.openContrarreloj()
    show(vm)
    assertEquals(1, rule.onAllNodes(hasTestTag("contrarreloj_age_hint"), useUnmergedTree = true).fetchSemanticsNodes().size)
    assertEquals("y no la frase de la tercera partida", 0, rule.onAllNodes(hasTestTag("contrarreloj_early_notice"), useUnmergedTree = true).fetchSemanticsNodes().size)
  }
}
