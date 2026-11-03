package com.example

import android.app.Application
import androidx.activity.ComponentActivity
import androidx.compose.foundation.layout.Box
import androidx.compose.foundation.layout.height
import androidx.compose.foundation.layout.width
import androidx.compose.runtime.CompositionLocalProvider
import androidx.compose.runtime.collectAsState
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
import androidx.compose.ui.test.performScrollTo
import androidx.compose.ui.test.performSemanticsAction
import androidx.compose.ui.unit.dp
import androidx.test.core.app.ApplicationProvider
import com.example.data.Comparison
import com.example.data.NBSP
import com.example.data.PlusAccess
import com.example.data.TimeTrialDay
import com.example.data.TimeTrialResult
import com.example.data.TimeTrialStats
import com.example.data.local.NeuroVidaDatabase
import com.example.data.local.toEntity
import com.example.games.TimeTrialResultScreen
import com.example.model.AgeBand
import com.example.model.AgeRange
import com.example.model.GamePlayResult
import com.example.model.UserSettings
import com.example.ui.components.LocalReduceMotion
import com.example.ui.screens.ContrarrelojScreen
import com.example.ui.screens.ProgressScreen
import com.example.ui.theme.NeuroVidaTheme
import com.example.viewmodel.AppTab
import com.example.viewmodel.NeuroVidaViewModel
import kotlinx.coroutines.runBlocking
import org.junit.After
import org.junit.Assert.assertEquals
import org.junit.Assert.assertFalse
import org.junit.Assert.assertNotNull
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
 * La pestaña «Contrarreloj», con la pantalla REAL y su ViewModel (Tarea 215; antes, Tareas 199 y 203, era una puerta y una página dentro de Avance): arriba lo gratis a color (el selector con el de hoy marcado «Hoy», el récord, la semana, la división), debajo lo de Plus; con Nubilus Plus
 * el selector se toca entero y el juego elegido lo guarda el ViewModel (no se cierra al cambiar de pestaña); sin Plus los otros tres juegos y las dos mitades de Plus van en gris con candado y abren una hoja corta con un solo botón, y aunque el ViewModel tenga otro juego elegido se ve el de hoy; «Jugar · 2 minutos» lanza
 * el juego elegido y el final vuelve a la pestaña; Avance ya no tiene la puerta; Plus arranca encendido en depuración y es una preferencia pasajera; el atajo «Todas tus marcas ›» del final del Contrarreloj existe solo con Plus y abre esta pestaña.
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
    vm.setTab(AppTab.CONTRARRELOJ)
    return vm
  }

  private fun show(vm: NeuroVidaViewModel, heightDp: Int = 873) {
    rule.setContent {
      NeuroVidaTheme {
        CompositionLocalProvider(LocalReduceMotion provides true, com.example.ui.components.LocalTabSwipeLock provides remember { mutableIntStateOf(0) }) {
          Box(Modifier.width(393.dp).height(heightDp.dp)) { ContrarrelojScreen(viewModel = vm) }
        }
      }
    }
    rule.waitForIdle()
  }

  private fun showAvance(vm: NeuroVidaViewModel, heightDp: Int = 1500) {
    rule.setContent {
      NeuroVidaTheme {
        CompositionLocalProvider(LocalReduceMotion provides true, com.example.ui.components.LocalTabSwipeLock provides remember { mutableIntStateOf(0) }) {
          Box(Modifier.width(393.dp).height(heightDp.dp)) { ProgressScreen(viewModel = vm) }
        }
      }
    }
    rule.waitForIdle()
  }

  private fun count(tag: String) = rule.onAllNodes(hasTestTag(tag), useUnmergedTree = true).fetchSemanticsNodes().size
  private fun textOf(tag: String): String = rule.onNode(hasTestTag(tag), useUnmergedTree = true).fetchSemanticsNode().config.getOrNull(SemanticsProperties.Text)?.joinToString("") { it.text }.orEmpty().replace(NBSP, " ")
  private fun spokenOf(tag: String): String = rule.onNodeWithTag(tag).fetchSemanticsNode().config.getOrNull(SemanticsProperties.ContentDescription)?.joinToString(". ").orEmpty().replace(NBSP, " ")
  private fun isSelected(tag: String): Boolean = rule.onNodeWithTag(tag).fetchSemanticsNode().config.getOrNull(SemanticsProperties.Selected) == true
  private fun pressBack() { rule.runOnUiThread { rule.activity.onBackPressedDispatcher.onBackPressed() }; rule.waitForIdle() }
  private fun top(tag: String) = rule.onNodeWithTag(tag, useUnmergedTree = true).fetchSemanticsNode().positionInRoot.y

  /** Una marca de ESTE momento (de la semana en curso, sea cual sea el día): 101 aciertos está en el último tramo de cualquier franja de la referencia estimada. */
  private fun recordNow(vm: NeuroVidaViewModel) {
    val repo = (app as NeuroVidaApplication).repository
    val today = TimeTrialDay.gameFor(vm.todayKey())
    runBlocking { repo.recordTimeTrial(TimeTrialResult(id = "flow-now", gameId = today, day = vm.todayKey(), trackVersion = TimeTrialDay.trackVersion(today), hits = 101, errors = 2, level = 5, seconds = 120, timestamp = System.currentTimeMillis())) }
    TestSupport.awaitUntil(30_000) { vm.timeTrialResults.value.size >= 12 }
  }

  private fun withRange(vm: NeuroVidaViewModel, range: AgeRange) { vm.setAgeRange(range); TestSupport.awaitUntil(30_000) { vm.ageRange.value == range } }

  // ---------------------------------------------------------------- la pestaña con Plus

  @Test
  fun con_Plus_la_pestana_marca_el_juego_de_hoy_y_dice_el_record_y_la_semana_en_lo_gratis() {
    val vm = withData(); vm.setPlus(true)
    show(vm)
    val today = TimeTrialDay.gameFor(vm.todayKey())
    rule.onNodeWithTag("contrarreloj_page").assertIsDisplayed()
    assertEquals("Contrarreloj", textOf("contrarreloj_title"))
    assertEquals("Tu récord: 44 aciertos.", textOf("contrarreloj_free_record"))
    assertTrue("y la semana, en una frase completa: «${textOf("contrarreloj_free_week")}»", textOf("contrarreloj_free_week").startsWith("Esta semana "))
    assertEquals("solo el de hoy lleva «Hoy»", listOf(today), TimeTrialDay.GAMES.filter { count("contrarreloj_game_today_$it") == 1 })
    assertEquals("Hoy", textOf("contrarreloj_game_today_$today"))
    assertTrue("y abre elegido", isSelected("contrarreloj_game_$today"))
    assertEquals("el botón de jugar", "Jugar · 2 minutos", rule.onNodeWithTag("btn_contrarreloj_play").fetchSemanticsNode().config.getOrNull(SemanticsProperties.Text)?.joinToString(""))
    assertEquals("con Plus no hay nada en gris", 0, count("contrarreloj_locked_place") + count("contrarreloj_locked_marks"))
  }

  @Test
  fun Avance_ya_no_tiene_la_puerta_del_Contrarreloj_ni_su_hoja() {
    val vm = withData(); vm.setPlus(false); vm.setTab(AppTab.PROGRESO)
    showAvance(vm)
    rule.onNodeWithTag("progress_area_memoria").assertExists()
    assertEquals("ni la puerta, ni su losa, ni la hoja del candado", 0, count("avance_contrarreloj_door") + count("contrarreloj_page") + count("plus_sheet"))
    val source = java.io.File("src/main/java/com/example/ui/screens/ProgressScreen.kt").readText()
    assertFalse("ProgressScreen no nombra la puerta ni la página", source.contains("ContrarrelojDoor") || source.contains("ContrarrelojPage(") || source.contains("PlusLockSheet("))
  }

  @Test
  fun con_Plus_elegir_otro_juego_cambia_la_pestana_y_el_ViewModel_lo_recuerda() {
    val vm = withData(); vm.setPlus(true)
    show(vm)
    val today = TimeTrialDay.gameFor(vm.todayKey())
    val other = TimeTrialDay.GAMES.first { it != today }
    assertNull("sin elegir, el de hoy", vm.contrarrelojGame.value)
    rule.onNodeWithTag("contrarreloj_game_$other").performClick(); rule.waitForIdle()
    assertEquals(other, vm.contrarrelojGame.value)
    assertTrue(isSelected("contrarreloj_game_$other")); assertFalse(isSelected("contrarreloj_game_$today"))
    assertEquals("la pestaña muestra el récord de ese juego", "Tu récord: 27 aciertos.", textOf("contrarreloj_free_record"))
    assertEquals("y no hay hoja: con Plus nada está en gris", 0, count("plus_sheet"))
  }

  @Test
  fun el_juego_elegido_no_se_suelta_al_cambiar_de_pestana() {
    val vm = withData(); vm.setPlus(true)
    val other = TimeTrialDay.GAMES.first { it != TimeTrialDay.gameFor(vm.todayKey()) }
    vm.selectContrarrelojGame(other)
    vm.setTab(AppTab.HOY); vm.setTab(AppTab.CONTRARRELOJ)
    assertEquals("es una pestaña, no una lámina: no se cierra", other, vm.contrarrelojGame.value)
  }

  // ---------------------------------------------------------------- la pestaña sin Plus: el gris con candado

  @Test
  fun sin_Plus_los_otros_tres_juegos_van_en_gris_con_su_record_y_abren_la_hoja_sin_elegirse() {
    val vm = withData(); vm.setPlus(false)
    show(vm)
    val today = TimeTrialDay.gameFor(vm.todayKey())
    val others = TimeTrialDay.GAMES.filter { it != today }
    assertTrue("el de hoy se toca y abre elegido", isSelected("contrarreloj_game_$today"))
    for (o in others) {
      assertTrue("«${spokenOf("contrarreloj_game_$o")}» dice que es de Nubilus Plus", spokenOf("contrarreloj_game_$o").endsWith("con Nubilus Plus"))
      assertTrue("y conserva su récord a la vista: «${textOf("contrarreloj_game_value_$o")}»", textOf("contrarreloj_game_value_$o").isNotEmpty())
    }
    assertFalse("el de hoy no dice «con Nubilus Plus»", spokenOf("contrarreloj_game_$today").contains("Nubilus Plus"))
    // Tarea 215b (Fable M2): un juego en gris no lleva `selected` (rol de botón: el lector diría «no seleccionado» tras «con Nubilus Plus»); el de hoy, sí
    for (o in others) assertNull("«$o» en gris no declara selected", rule.onNodeWithTag("contrarreloj_game_$o").fetchSemanticsNode().config.getOrNull(SemanticsProperties.Selected))
    assertEquals(true, rule.onNodeWithTag("contrarreloj_game_$today").fetchSemanticsNode().config.getOrNull(SemanticsProperties.Selected))
    assertEquals("el botón de jugar dice también el juego (Fable A3)", "Jugar 2 minutos de ${textOf("contrarreloj_game_name_$today")}", spokenOf("btn_contrarreloj_play"))
    assertEquals("el récord de otro juego (29 aciertos con 2 errores) sigue a la vista", "27 aciertos", textOf("contrarreloj_game_value_${others.first { vm.timeTrialResults.value.any { r -> r.gameId == it } }}"))
    rule.onNodeWithTag("contrarreloj_game_${others.first()}").performClick(); rule.waitForIdle()
    assertNull("tocar uno en gris no lo elige", vm.contrarrelojGame.value)
    rule.onNodeWithTag("plus_sheet").assertIsDisplayed()
    rule.onNodeWithTag("btn_plus_sheet_ok").performClick(); rule.waitForIdle()
    assertEquals("la hoja se cierra", 0, count("plus_sheet"))
    assertTrue("y el de hoy sigue elegido", isSelected("contrarreloj_game_$today"))
  }

  @Test
  fun sin_Plus_aunque_el_ViewModel_tenga_otro_juego_elegido_se_ve_el_de_hoy() {
    val vm = withData(); vm.setPlus(false)
    val today = TimeTrialDay.gameFor(vm.todayKey())
    vm.selectContrarrelojGame(TimeTrialDay.GAMES.first { it != today })
    show(vm)
    assertTrue(isSelected("contrarreloj_game_$today"))
    assertEquals("Tu récord: 44 aciertos.", textOf("contrarreloj_free_record"))
  }

  @Test
  fun sin_Plus_las_dos_mitades_de_Plus_van_en_gris_con_su_frase_y_abren_la_hoja_con_un_solo_boton() {
    val vm = withData(); vm.setPlus(false); withRange(vm, AgeRange.R45_54)
    show(vm)
    for ((tag, phrase) in listOf("contrarreloj_locked_place" to "Con Nubilus Plus: tu lugar en detalle.", "contrarreloj_locked_marks" to "Con Nubilus Plus: todas tus marcas.")) {
      assertEquals("el lector dice el título y la frase (Tarea 215b, Fable M1)", "${textOf("${tag}_title")}. $phrase", spokenOf(tag))
      assertEquals("y la frase se ve", phrase, textOf("${tag}_phrase"))
    }
    assertEquals(PlusAccess.LOCKED_PLACE_TITLE, textOf("contrarreloj_locked_place_title")); assertEquals(PlusAccess.LOCKED_MARKS_TITLE, textOf("contrarreloj_locked_marks_title"))
    assertEquals("la silueta es decorativa: está, pero el lector no lee nada de ella", 2, count("contrarreloj_locked_place_silhouette") + count("contrarreloj_locked_marks_silhouette"))
    assertTrue("y no dice ni un texto", listOf("contrarreloj_locked_place_silhouette", "contrarreloj_locked_marks_silhouette").all { rule.onNode(hasTestTag(it), useUnmergedTree = true).fetchSemanticsNode().config.getOrNull(SemanticsProperties.Text) == null })
    assertTrue("«tu lugar» va antes de «todas tus marcas»", top("contrarreloj_locked_place") < top("contrarreloj_locked_marks"))
    assertEquals("con Plus, nada de las secciones de Plus se dibuja: ni la escalera ni las 10 marcas", 0, count("contrarreloj_ladder") + count("contrarreloj_marks") + count("contrarreloj_rhythm") + count("contrarreloj_notes"))
    rule.onNodeWithTag("contrarreloj_locked_marks").performScrollTo().performClick(); rule.waitForIdle()
    assertNull("sin Plus no se cambia nada: solo se abre la hoja", vm.contrarrelojGame.value)
    rule.onNodeWithTag("plus_sheet").assertIsDisplayed()
    assertEquals(PlusAccess.SHEET_TITLE, textOf("plus_sheet_title"))
    // la referencia estimada (de fábrica en depuración y en la tienda) llega a la hoja: habla de la referencia estimada, nunca de personas, y trae TRES líneas
    assertEquals(PlusAccess.sheetLead(Comparison.Kind.ESTIMATED), textOf("plus_sheet_lead"))
    assertEquals("tres líneas de lo que trae", 3, (0 until 12).count { count("plus_sheet_item_$it") > 0 })
    assertEquals(PlusAccess.sheetItems(Comparison.Kind.ESTIMATED), (0 until 3).map { textOf("plus_sheet_item_$it") })
    assertEquals("Cerrar", rule.onNodeWithTag("btn_plus_sheet_ok").fetchSemanticsNode().config.getOrNull(SemanticsProperties.Text)?.joinToString("") { it.text })
    rule.onNodeWithTag("btn_plus_sheet_ok").performClick(); rule.waitForIdle()
    assertEquals("la hoja se cierra", 0, count("plus_sheet"))
    rule.onNodeWithTag("contrarreloj_locked_place").performScrollTo().performClick(); rule.waitForIdle()
    rule.onNodeWithTag("plus_sheet").assertIsDisplayed()
  }

  @Test
  fun sin_Plus_lo_que_Plus_no_daria_tampoco_se_vende_en_gris() {
    // sin franja de edad guardada: no hay lugar que vender, solo «todas tus marcas»
    val none = withData(); none.setPlus(false)
    assertNull(none.ageRange.value)
    show(none)
    assertEquals("sin franja: ni «tu lugar» en gris", 0, count("contrarreloj_locked_place"))
    assertEquals("pero sí «todas tus marcas»", 1, count("contrarreloj_locked_marks"))
  }

  @Test
  fun una_persona_de_14_a_17_anos_ve_lo_mismo_que_una_de_18_a_29_con_las_dos_losas_grises_sin_Plus_y_sin_aviso_de_menores() {
    // Tarea 216 (decisión de Ricardo, 2-nov): la app es desde los 14 años y la franja de 14 a 17 usa la tabla de 18 a 29; no hay aviso ni Gate.MINOR
    val vm = withData(); vm.setPlus(false); withRange(vm, AgeRange.R14_17)
    recordNow(vm)
    show(vm)
    assertEquals("las dos losas grises, como cualquiera con franja", 2, count("contrarreloj_locked_place") + count("contrarreloj_locked_marks"))
    assertEquals("ni una línea de «De 13 a 17 años…» ni de 14 a 17: no hay aviso", 0, count("contrarreloj_minors_notice") + count("contrarreloj_age_hint") + count("contrarreloj_early_notice"))
    assertTrue("y ningún texto dice «solo tus propias marcas»", (0 until 1).all { rule.onAllNodes(androidx.compose.ui.test.hasText("solo tus propias marcas", substring = true), useUnmergedTree = true).fetchSemanticsNodes().isEmpty() })
    val today = TimeTrialDay.gameFor(vm.todayKey())
    val young = Comparison.place(vm.estimatedReference.value!!, today, AgeRange.R18_29, 99) as Comparison.Place.Found
    assertEquals("la división es la de 18 a 29 con la misma marca", young.ranked.division.label, textOf("contrarreloj_hero_division"))
    assertEquals("y su lugar entre las cinco", Comparison.divisionPlaceLine(young.ranked.division), textOf("contrarreloj_hero_place"))
    assertEquals("el subtítulo, con comparación", "El Contrarreloj define tu lugar de la semana.", textOf("contrarreloj_subtitle"))
    assertEquals("la fila «Con qué te comparas ›» también", 1, count("contrarreloj_source_link"))
  }

  // ---------------------------------------------------------------- lo gratis: la división y lo que falta (antes lo daba la puerta de Avance)

  @Test
  fun sin_Plus_lo_gratis_dice_la_division_su_lugar_y_lo_que_falta_cuando_hay_una_marca_esta_semana() {
    val vm = withData(); vm.setPlus(false); withRange(vm, AgeRange.R45_54)
    recordNow(vm)
    show(vm)
    assertEquals("101 aciertos con 2 errores son 99 de marca", "Tu récord: 99 aciertos.", textOf("contrarreloj_free_record"))
    assertEquals("la división", Comparison.Division.STARS.label, textOf("contrarreloj_hero_division"))
    assertEquals("su lugar entre las cinco", Comparison.divisionPlaceLine(Comparison.Division.STARS), textOf("contrarreloj_hero_place"))
    assertEquals("y, en la más alta, no falta nada", "Estás en la división más alta de tu franja esta semana.", textOf("contrarreloj_ladder_need"))
    assertEquals("La semana cierra el domingo.", textOf("contrarreloj_ladder_closes"))
    assertEquals("el subtítulo, con comparación", "El Contrarreloj define tu lugar de la semana.", textOf("contrarreloj_subtitle"))
    assertEquals("la fila «Con qué te comparas ›» queda bajo lo gratis", 1, count("contrarreloj_source_link"))
    assertTrue("y antes de lo de Plus en gris", top("contrarreloj_source_link") < top("contrarreloj_locked_place"))
    assertTrue("la división va dentro de la losa gratis: después del récord", top("contrarreloj_free_record") < top("contrarreloj_hero_division") && top("contrarreloj_hero_division") < top("contrarreloj_ladder_need"))
  }

  @Test
  fun con_Plus_lo_gratis_y_lo_de_Plus_no_se_repiten_la_division_y_lo_que_falta_viven_una_sola_vez() {
    val vm = withData(); vm.setPlus(true); withRange(vm, AgeRange.R45_54)
    recordNow(vm)
    show(vm)
    assertEquals("la división, una sola vez", 1, count("contrarreloj_hero_division"))
    assertEquals("lo que falta, una sola vez", 1, count("contrarreloj_ladder_need"))
    assertEquals("«La semana cierra el domingo.», una sola vez", 1, count("contrarreloj_ladder_closes"))
    assertEquals("y debajo vienen las secciones de Plus, a color: la frase del porcentaje y la escalera", 2, count("contrarreloj_hero_sentence") + count("contrarreloj_ladder"))
    assertTrue("la frase del porcentaje va después de lo gratis", top("contrarreloj_ladder_closes") < top("contrarreloj_hero_sentence"))
    assertTrue("y la escalera después de la frase", top("contrarreloj_hero_sentence") < top("contrarreloj_ladder"))
    assertEquals("las tres cifras de la Tarea 203 salieron: el récord y la semana ya los dice lo gratis", 0, count("contrarreloj_stats"))
    assertTrue("«todas tus marcas» después de la comparación", top("contrarreloj_ladder") < top("contrarreloj_marks"))
  }

  @Test
  fun sin_una_franja_de_edad_elegida_no_hay_con_quien_compararse_y_lo_gratis_no_dice_la_division() {
    val vm = withData(); vm.setPlus(false)
    recordNow(vm)
    assertNull("sin franja guardada (quien solo tenía la banda vieja)", vm.ageRange.value)
    show(vm)
    assertEquals("la referencia estimada es por franja: sin ella no hay división", 0, count("contrarreloj_hero_division"))
    assertEquals("la página dice dónde elegirla", Comparison.AGE_HINT, textOf("contrarreloj_age_hint"))
    assertEquals("sigue diciendo el récord (es tuyo)", 1, count("contrarreloj_free_record"))
  }

  @Test
  fun sin_una_marca_esta_semana_lo_gratis_no_inventa_la_division() {
    val vm = withData(); vm.setPlus(false); withRange(vm, AgeRange.R45_54)
    // withData(): las marcas están en los 10 días anteriores; si la de hace un día cae en la semana anterior (los lunes), no hay marca de esta semana
    show(vm)
    val n = count("contrarreloj_hero_division")
    assertTrue("la división es una sola, o no hay", n <= 1)
    if (n == 0) assertTrue("sin marca esta semana, la losa de «Tu lugar esta semana» dice qué hacer", count("contrarreloj_place") == 1 || count("contrarreloj_place_nomark") == 1)
  }

  // ---------------------------------------------------------------- la hoja del candado

  /** Abre la hoja del candado (sin Plus) con la franja [range] guardada (null = ninguna) y devuelve lo que dice. */
  private fun sheetTexts(range: AgeRange?): Pair<String, List<String>> {
    val vm = withData(); vm.setPlus(false)
    if (range != null) withRange(vm, range)
    show(vm)
    rule.onNodeWithTag("contrarreloj_locked_marks").performScrollTo().performClick(); rule.waitForIdle()
    rule.onNodeWithTag("plus_sheet").assertIsDisplayed()
    return textOf("plus_sheet_lead") to (0 until 3).map { textOf("plus_sheet_item_$it") }
  }

  @Test
  fun la_hoja_del_candado_nombra_la_referencia_solo_si_la_persona_tiene_una_franja_con_que_compararse() {
    val (lead, items) = sheetTexts(AgeRange.R45_54)
    assertEquals(PlusAccess.sheetLead(Comparison.Kind.ESTIMATED), lead)
    assertEquals(PlusAccess.sheetItems(Comparison.Kind.ESTIMATED), items)
  }

  @Test
  fun la_hoja_del_candado_de_14_a_17_anos_es_la_de_cualquiera_con_referencia_y_no_habla_de_edad() {
    val (lead, items) = sheetTexts(AgeRange.R14_17)
    assertEquals("Tarea 216: ya no existe la entrada «De 13 a 17 años se comparan solo contigo»", PlusAccess.sheetLead(Comparison.Kind.ESTIMATED), lead)
    assertEquals(PlusAccess.sheetItems(Comparison.Kind.ESTIMATED), items)
    assertEquals("con la razón y la fila de la referencia, como cualquiera", 2, count("plus_sheet_note") + count("plus_sheet_source"))
    assertTrue("ni una edad nombrada en la hoja", (listOf(lead) + items).none { Regex("\\d+ a \\d+ años").containsMatchIn(it) })
  }

  @Test
  fun la_hoja_del_candado_dice_la_razon_y_su_fila_abre_de_donde_sale_la_referencia() {
    val vm = withData(); vm.setPlus(false); withRange(vm, AgeRange.R45_54)
    show(vm)
    rule.onNodeWithTag("contrarreloj_locked_marks").performScrollTo().performClick(); rule.waitForIdle()
    assertEquals("Gratis: jugar, tu récord y tu división de la semana. Con Nubilus Plus, además:", textOf("plus_sheet_lead"))
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
    assertEquals(PlusAccess.sheetLead(Comparison.Kind.NONE), lead)
    assertEquals(PlusAccess.sheetItems(Comparison.Kind.NONE), items)
  }

  @Test
  fun la_referencia_estimada_la_carga_el_ViewModel_y_la_pestana_no_lee_assets() {
    val vm = withData()
    assertEquals("las cuatro tablas del APK", 4, vm.estimatedReference.value!!.loaded.size)
    val screen = java.io.File("src/main/java/com/example/ui/screens/ContrarrelojScreen.kt").readText()
    assertFalse("la pantalla no lee assets al componer (hilo principal)", screen.contains("EstimatedReference.get("))
    assertTrue("la toma del ViewModel", screen.contains("viewModel.estimatedReference"))
    val vmSource = java.io.File("src/main/java/com/example/viewmodel/NeuroVidaViewModel.kt").readText()
    assertTrue("y el ViewModel la lee en IO", vmSource.contains("launch(kotlinx.coroutines.Dispatchers.IO)") && vmSource.contains("EstimatedReference.get(getApplication())"))
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

  // ---------------------------------------------------------------- jugar desde la pestaña y volver a ella

  @Test
  fun Jugar_lanza_el_juego_de_hoy_con_su_pista_y_al_cerrar_el_final_se_vuelve_a_la_pestana() {
    val vm = withData(); vm.setPlus(false)
    show(vm)
    val today = TimeTrialDay.gameFor(vm.todayKey())
    rule.onNodeWithTag("btn_contrarreloj_play").performClick(); rule.waitForIdle()
    val session = vm.activeGame.value
    assertNotNull("se lanzó una partida", session)
    assertEquals(today, session!!.gameDef.id)
    assertEquals("con la pista de hoy", vm.todayKey(), session.timeTrialDay)
    vm.closeGameOrResult()
    assertEquals("el final vuelve a la pestaña Contrarreloj, no a Hoy", AppTab.CONTRARRELOJ, vm.currentTab.value)
    assertNull(vm.activeGame.value)
  }

  @Test
  fun con_Plus_Jugar_lanza_el_juego_elegido() {
    val vm = withData(); vm.setPlus(true)
    val today = TimeTrialDay.gameFor(vm.todayKey())
    val other = TimeTrialDay.GAMES.first { it != today }
    vm.selectContrarrelojGame(other)
    show(vm)
    rule.onNodeWithTag("btn_contrarreloj_play").performClick(); rule.waitForIdle()
    assertEquals(other, vm.activeGame.value?.gameDef?.id)
  }

  @Test
  fun Otra_vez_vuelve_a_donde_se_vino_la_primera_vez_y_el_boton_de_Hoy_vuelve_a_Hoy() {
    val vm = withData(); vm.setPlus(true)
    val today = TimeTrialDay.gameFor(vm.todayKey())
    // desde la pestaña: «Otra vez» no cambia el origen
    vm.startTimeTrial(today, fromTab = true)
    vm.timeTrialAgain(today, vm.todayKey())
    vm.closeGameOrResult()
    assertEquals(AppTab.CONTRARRELOJ, vm.currentTab.value)
    // desde Hoy: como siempre
    vm.setTab(AppTab.PROGRESO)
    vm.startTimeTrial()
    vm.timeTrialAgain(today, vm.todayKey())
    vm.closeGameOrResult()
    assertEquals(AppTab.HOY, vm.currentTab.value)
  }

  @Test
  fun Todas_tus_marcas_deja_el_final_va_a_la_pestana_y_elige_ese_juego() {
    val vm = withData(); vm.setPlus(true)
    vm.setTab(AppTab.HOY)
    val other = TimeTrialDay.GAMES.last()
    vm.openContrarrelojFromFinal(other)
    assertEquals(AppTab.CONTRARRELOJ, vm.currentTab.value)
    assertEquals(other, vm.contrarrelojGame.value)
    assertNull("ya no hay un final a la vista", vm.lastResult.value)
    show(vm)
    rule.onNodeWithTag("contrarreloj_page").assertIsDisplayed()
    assertTrue(isSelected("contrarreloj_game_$other"))
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
    vm.setPlus(plus); vm.setAgeRange(AgeRange.R45_54)
    TestSupport.awaitUntil(30_000) { vm.ageRange.value == AgeRange.R45_54 }
    repeat(count) { recordToday(vm, repo, it) }
    if (count > 0) TestSupport.awaitUntil(30_000) { vm.timeTrialResults.value.size >= count }
    TestSupport.awaitUntil(30_000, "La referencia estimada no se cargó") { vm.estimatedReference.value != null }
    vm.setTab(AppTab.CONTRARRELOJ)
    return vm
  }

  private fun recordToday(vm: NeuroVidaViewModel, repo: com.example.data.NeuroVidaRepository, i: Int) {
    val today = TimeTrialDay.gameFor(vm.todayKey())
    runBlocking { repo.recordTimeTrial(TimeTrialResult(id = "t207-$i", gameId = today, day = vm.todayKey(), trackVersion = TimeTrialDay.trackVersion(today), hits = 101, errors = 2, level = 5, seconds = 120, timestamp = System.currentTimeMillis() - (10 - i) * 1000L)) }
  }

  @Test
  fun sin_Plus_lo_gratis_calla_la_division_con_menos_de_tres_partidas_y_habla_desde_la_tercera() {
    val vm = withGames(2, plus = false)
    val repo = (app as NeuroVidaApplication).repository
    show(vm)
    assertEquals("con dos partidas lo gratis calla la división, como sin franja de edad", 0, count("contrarreloj_hero_division"))
    assertEquals("pero dice el récord y por qué", 1, count("contrarreloj_free_record"))
    assertEquals(Comparison.EARLY_NOTICE, textOf("contrarreloj_early_notice"))
    assertEquals("antes de la tercera las losas grises SÍ van: con Plus se vería desde la tercera", 2, count("contrarreloj_locked_place") + count("contrarreloj_locked_marks"))
    recordToday(vm, repo, 2)
    TestSupport.awaitUntil(30_000) { vm.timeTrialResults.value.size >= 3 }
    rule.waitForIdle()
    assertEquals("con la tercera, la división de siempre", 1, count("contrarreloj_hero_division"))
    assertEquals(Comparison.Division.STARS.label, textOf("contrarreloj_hero_division"))
  }

  @Test
  fun con_Plus_la_pagina_dice_la_frase_con_dos_partidas_y_el_lugar_desde_la_tercera() {
    val vm = withGames(2, plus = true)
    val repo = (app as NeuroVidaApplication).repository
    show(vm)
    assertEquals("Tu lugar aparece desde tu tercera partida: las primeras sirven para conocer el juego.", textOf("contrarreloj_early_notice"))
    assertEquals("sin lugar: ni cifra ni escalera ni campana", 0, count("contrarreloj_hero") + count("contrarreloj_ladder") + count("contrarreloj_bell"))
    recordToday(vm, repo, 2)
    TestSupport.awaitUntil(30_000) { vm.timeTrialResults.value.size >= 3 }
    rule.waitForIdle()
    assertEquals("con la tercera ya no está la frase", 0, count("contrarreloj_early_notice"))
    assertEquals("y aparece el lugar", 1, count("contrarreloj_hero"))
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
    vm.setTab(AppTab.CONTRARRELOJ)
    show(vm)
    assertEquals(1, count("contrarreloj_age_hint"))
    assertEquals("y no la frase de la tercera partida", 0, count("contrarreloj_early_notice"))
  }

  // ------------------------------------------------------------------ al volver de «De dónde sale la referencia», la pestaña sigue donde estaba (Tarea 209b)

  /** El mismo cableado que `MainActivity`: las pestañas (aquí Contrarreloj) se componen solo sin panel de arriba y guardan su estado con `KeepTabsState`; «De dónde sale la referencia» va encima, como `TopPanel.REFERENCIA`. */
  private fun showWithReferenciaPanel(vm: NeuroVidaViewModel, heightDp: Int) {
    rule.setContent {
      NeuroVidaTheme {
        CompositionLocalProvider(LocalReduceMotion provides true, com.example.ui.components.LocalTabSwipeLock provides remember { mutableIntStateOf(0) }) {
          val panel = vm.topPanel.collectAsState().value
          Box(Modifier.width(393.dp).height(heightDp.dp)) {
            com.example.ui.components.KeepTabsState(show = panel == null) { ContrarrelojScreen(viewModel = vm) }
            if (panel == com.example.viewmodel.TopPanel.REFERENCIA) com.example.ui.screens.ReferenciaPanel(onClose = { vm.openTopPanel(null) })
          }
        }
      }
    }
    rule.waitForIdle()
  }

  @Test
  fun al_volver_de_donde_sale_la_referencia_la_pestana_Contrarreloj_sigue_donde_estaba_y_no_arriba() {
    val vm = withData(); vm.setPlus(true); withRange(vm, AgeRange.R45_54)
    recordNow(vm)
    showWithReferenciaPanel(vm, heightDp = 700)                       // una ventana corta: la página se desliza
    rule.onNodeWithTag("contrarreloj_page").assertIsDisplayed()
    rule.onNodeWithTag("contrarreloj_scroll").performSemanticsAction(androidx.compose.ui.semantics.SemanticsActions.ScrollBy) { it(0f, 500f) }; rule.waitForIdle()          // la fila «Con qué te comparas ›» está bajo lo gratis: se baja hasta verla
    val scrolled = rule.onNodeWithTag("contrarreloj_scroll").fetchSemanticsNode().config[SemanticsProperties.VerticalScrollAxisRange].value()
    assertTrue("la página bajó ($scrolled px)", scrolled > 100f)
    val linkY = top("contrarreloj_source_link")
    rule.onNodeWithTag("contrarreloj_source_link").performClick(); rule.waitForIdle()
    assertEquals("se abre el panel de los estudios", com.example.viewmodel.TopPanel.REFERENCIA, vm.topPanel.value)
    rule.onNodeWithTag("referencia_screen").assertIsDisplayed()
    assertEquals("y la página queda fuera de la composición", 0, count("contrarreloj_page"))
    pressBack()
    assertNull("Atrás cierra el panel", vm.topPanel.value)
    rule.onNodeWithTag("contrarreloj_page").assertIsDisplayed()
    assertEquals("el deslizado sigue donde estaba", scrolled, rule.onNodeWithTag("contrarreloj_scroll").fetchSemanticsNode().config[SemanticsProperties.VerticalScrollAxisRange].value(), 0.5f)
    assertEquals("y la fila que abrió el panel, en el mismo lugar de la pantalla", linkY, top("contrarreloj_source_link"), 1f)
  }

  @Test
  fun MainActivity_compone_las_pestanas_dentro_de_KeepTabsState() {
    val main = java.io.File("src/main/java/com/example/MainActivity.kt").readText()
    val keep = main.indexOf("KeepTabsState(show = ")
    val pager = main.indexOf("HorizontalPager(")
    assertTrue("el pager de las cuatro pestañas está dentro de KeepTabsState", keep in 0 until pager)
    assertTrue("y se esconde con las mismas condiciones de siempre (juego, resultado, resumen, panel, Todos los juegos)",
      main.substring(keep, pager).contains("activeGame == null && lastResult == null && sessionSummary == null && topPanel == null && allGames == null"))
    assertTrue("y la pestaña Contrarreloj es una de sus páginas", main.contains("AppTab.CONTRARRELOJ -> com.example.ui.screens.ContrarrelojScreen(viewModel = viewModel)"))
  }
}
