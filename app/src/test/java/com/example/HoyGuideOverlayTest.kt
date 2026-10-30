package com.example

import android.app.Application
import androidx.activity.compose.LocalOnBackPressedDispatcherOwner
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
import androidx.compose.ui.geometry.Offset
import androidx.compose.ui.geometry.Rect
import androidx.compose.ui.layout.boundsInRoot
import androidx.compose.ui.layout.positionInRoot
import androidx.compose.ui.platform.LocalDensity
import androidx.compose.ui.semantics.SemanticsProperties
import androidx.compose.ui.semantics.getOrNull
import androidx.compose.ui.test.assertCountEquals
import androidx.compose.ui.test.assertHeightIsAtLeast
import androidx.compose.ui.test.click
import androidx.compose.ui.test.assertWidthIsAtLeast
import androidx.compose.ui.test.hasContentDescription
import androidx.compose.ui.test.hasTestTag
import androidx.compose.ui.test.junit4.createComposeRule
import androidx.compose.ui.test.onNodeWithTag
import androidx.compose.ui.test.onNodeWithText
import androidx.compose.ui.test.onRoot
import androidx.compose.ui.test.performClick
import androidx.compose.ui.test.performTouchInput
import androidx.compose.ui.unit.Density
import androidx.compose.ui.unit.dp
import androidx.test.core.app.ApplicationProvider
import com.example.data.GuideStep
import com.example.data.HoyGuide
import com.example.data.TimeTrialState
import com.example.data.Week
import com.example.data.local.DailySessionEntity
import com.example.data.local.NeuroVidaDatabase
import com.example.model.AppLanguage
import com.example.model.RankTier
import com.example.ui.components.BoardBackground
import com.example.ui.components.GuideTarget
import com.example.ui.components.GuideTargets
import com.example.ui.components.HoyGuideHost
import com.example.ui.components.HoyGuideOverlay
import com.example.ui.components.LocalGuideTargets
import com.example.ui.components.LocalReduceMotion
import com.example.ui.components.NeuroNavBar
import com.example.ui.components.guideHoleOf
import com.example.ui.components.hiddenUntil
import com.example.ui.screens.HomeContent
import com.example.ui.screens.HomeTimeTrial
import com.example.ui.screens.HomeUi
import com.example.ui.screens.homeStops
import com.example.ui.theme.NeuroVidaTheme
import com.example.viewmodel.AppTab
import com.example.viewmodel.NeuroVidaViewModel
import com.example.viewmodel.StartupStage
import com.github.takahirom.roborazzi.RobolectricDeviceQualifiers
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
import java.text.SimpleDateFormat
import java.util.Calendar
import java.util.Date
import java.util.Locale

/**
 * La guía de Nubi sobre Hoy (Tarea 193, `HoyGuideOverlay` y `HoyGuideHost`; `auditoria-fable/guia-primer-ingreso.md` §3 y §6.3). Se monta con el Hoy de verdad (`HomeContent`) y la barra real, y sus rectángulos salen de lo que se mide: Robolectric no lee píxeles, así que el velo y el hueco se prueban por
 * RECTÁNGULOS y por el árbol de accesibilidad; el aspecto, en el emulador (lámina `docs/previews/tarea-193.png`). El orden de lo que pasa en el ViewModel (marca, medalla en cola, día del vuelo) está en `GameFlowTest` y `GuiaPrimerIngresoTest`.
 */
@RunWith(RobolectricTestRunner::class)
@GraphicsMode(GraphicsMode.Mode.NATIVE)
@Config(qualifiers = RobolectricDeviceQualifiers.Pixel8, sdk = [36])
class HoyGuideOverlayTest {
  @get:Rule val composeTestRule = createComposeRule()

  private lateinit var app: Application
  private val viewModels = mutableListOf<NeuroVidaViewModel>()

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

  // ---------------------------------------------------------------- el Hoy de verdad, con la barra, y la guía encima (como la aloja `NeuroVidaApp`)

  private val now = Calendar.getInstance().apply { set(2026, Calendar.OCTOBER, 30, 16, 30, 0) }.timeInMillis
  private val lamps = Week.Lamps(listOf(false, false, false, false, true, false, false), todayIndex = 4)
  private val stroop = HomeTimeTrial("stroop", "Tinta o Palabra", TimeTrialState.PENDIENTE, todayHits = null, record = null)

  /** El día del vuelo: la ruta cumplida con los 4 juegos del vuelo (Tarea 193), el Contrarreloj por jugar. */
  private fun flightDayUi(ids: List<String> = listOf("secuencia", "freno", "aterrizaje", "meteoros")) = HomeUi(
    name = "Ana", now = now, tier = RankTier.fromRating(0), streak = 1, weeklyGoal = 4, lamps = lamps,
    stops = homeStops(ids, ids.size, AppLanguage.SPANISH), pausedTitle = null, flightDay = true, hasBaseline = true, timeTrial = stroop
  )

  private var trialed = 0
  private var allGames = 0
  private val targets = GuideTargets()
  private var step by mutableStateOf<GuideStep?>(null)
  private var seen = false
  private var backOwner: androidx.activity.OnBackPressedDispatcherOwner? = null

  /** Hoy + la barra + la guía con un paso en una variable (`step`, null = cerrada). Los avances y los saltos hacen lo mismo que el ViewModel (`guideNext`, `guideSkip`). */
  private fun show(fontScale: Float = 1f, widthDp: Int = 393, heightDp: Int = 873, ui: HomeUi = flightDayUi(), first: GuideStep? = null) {
    step = first
    composeTestRule.setContent {
      val base = LocalDensity.current
      backOwner = LocalOnBackPressedDispatcherOwner.current
      CompositionLocalProvider(LocalDensity provides Density(base.density, fontScale), LocalReduceMotion provides true, LocalGuideTargets provides targets) {
        NeuroVidaTheme {
          Box(Modifier.width(widthDp.dp).height(heightDp.dp)) {
            BoardBackground()
            // como en `NeuroVidaApp`: lo de abajo queda fuera del lector y sin toques mientras la guía está abierta
            Box(Modifier.fillMaxSize().hiddenUntil(step == null)) {
              HomeContent(
                ui = ui, onAllGames = { allGames++ }, onPlay = {}, onResume = {}, onBaseline = {}, onTimeTrial = { trialed++ },
                modifier = Modifier.padding(top = 24.dp, bottom = 95.dp)
              )
              Box(Modifier.align(Alignment.BottomCenter)) { NeuroNavBar(current = AppTab.HOY, onSelect = {}) }
            }
            step?.let { s ->
              HoyGuideOverlay(
                step = s, targets = targets,
                onNext = { step = HoyGuide.next(s); if (step == null) seen = true },
                onSkip = { step = null; seen = true }
              )
            }
          }
        }
      }
    }
    composeTestRule.waitForIdle()
  }

  private fun start() { composeTestRule.runOnUiThread { step = GuideStep.CONTRARRELOJ }; composeTestRule.waitForIdle() }

  private fun holeOf(s: GuideStep): Rect {
    val pad = 8f * composeTestRule.density.density
    val r = guideHoleOf(s, targets.rects.getValue(targetOfStep(s)))
    return Rect(r.left - pad, r.top - pad, r.right + pad, r.bottom + pad)
  }

  private fun targetOfStep(s: GuideStep): GuideTarget = when (s) { GuideStep.CONTRARRELOJ -> GuideTarget.TIME_TRIAL; GuideStep.TODOS -> GuideTarget.ALL_GAMES; GuideStep.BARRA -> GuideTarget.TAB_BAR }

  private fun bounds(tag: String): Rect = composeTestRule.onNodeWithTag(tag).fetchSemanticsNode().boundsInRoot
  private fun exists(tag: String) = composeTestRule.onAllNodes(hasTestTag(tag)).fetchSemanticsNodes().isNotEmpty()
  private fun describe(tag: String): String? = composeTestRule.onNodeWithTag(tag).fetchSemanticsNode().config.getOrNull(SemanticsProperties.ContentDescription)?.firstOrNull()

  // ---------------------------------------------------------------- lo que se ve

  @Test
  fun al_abrir_hay_velo_globo_con_el_texto_del_paso_1_Seguir_y_Saltar_la_guia() {
    show(); start()
    composeTestRule.onNodeWithTag("guide_veil").assertExists()
    assertEquals("lo que dice el lector es el paso, el lugar de lo iluminado y el texto", HoyGuide.spoken(GuideStep.CONTRARRELOJ), describe("guide_globe"))
    composeTestRule.onNodeWithTag("btn_guide_next").assertExists()
    composeTestRule.onNodeWithText("Seguir ›").assertExists()
    composeTestRule.onNodeWithTag("btn_guide_skip").assertExists()
    composeTestRule.onNodeWithText("Saltar la guía").assertExists()
  }

  @Test
  fun el_hueco_del_paso_1_cae_sobre_el_boton_Contrarreloj_y_el_del_paso_2_sobre_Todos_los_juegos() {
    show(); start()
    val trial = bounds("guide_hole")
    val real = targets.rects.getValue(GuideTarget.TIME_TRIAL)
    val pad = 8f * composeTestRule.density.density
    assertEquals("el hueco = el botón real con 8 dp de aire", real.left - pad, trial.left, 1.5f)
    assertEquals(real.top - pad, trial.top, 1.5f)
    assertEquals(real.right + pad, trial.right, 1.5f)
    assertEquals(real.bottom + pad, trial.bottom, 1.5f)
    composeTestRule.onNodeWithTag("btn_guide_next").performClick()
    composeTestRule.waitForIdle()
    assertEquals(GuideStep.TODOS, step)
    val all = targets.rects.getValue(GuideTarget.ALL_GAMES)
    assertEquals(all.top - pad, bounds("guide_hole").top, 1.5f)
    assertEquals(all.bottom + pad, bounds("guide_hole").bottom, 1.5f)
  }

  @Test
  fun el_hueco_del_paso_3_cubre_las_pestanas_Avance_y_Tu_y_no_Hoy() {
    show(); start()
    composeTestRule.runOnUiThread { step = GuideStep.BARRA }; composeTestRule.waitForIdle()
    val bar = targets.rects.getValue(GuideTarget.TAB_BAR)
    val hole = bounds("guide_hole")
    val pad = 8f * composeTestRule.density.density
    assertEquals("empieza a un tercio de la barra (donde termina Hoy)", bar.left + bar.width / 3f - pad, hole.left, 1.5f)
    assertTrue("y llega hasta el borde de la derecha", hole.right >= bar.right - 1f)
    assertEquals(bar.top - pad, hole.top, 1.5f)
    assertTrue("no entra en la franja de gestos (ni en los 3 botones): abajo llega hasta donde llega la barra", hole.bottom <= bar.bottom + 1f)
  }

  // ---------------------------------------------------------------- cómo avanza y cómo se salta

  @Test
  fun Seguir_tres_veces_cierra_y_marca_y_el_tercer_chip_dice_Listo() {
    show(); start()
    composeTestRule.onNodeWithTag("btn_guide_next").performClick(); composeTestRule.waitForIdle()
    assertEquals(GuideStep.TODOS, step); assertFalse(seen)
    composeTestRule.onNodeWithTag("btn_guide_next").performClick(); composeTestRule.waitForIdle()
    assertEquals(GuideStep.BARRA, step); assertFalse(seen)
    composeTestRule.onNodeWithText("Listo").assertExists()
    composeTestRule.onAllNodes(androidx.compose.ui.test.hasText("Seguir ›")).assertCountEquals(0)
    composeTestRule.onNodeWithTag("btn_guide_next").performClick(); composeTestRule.waitForIdle()
    assertNull("tras «Listo» la guía se cierra", step)
    assertTrue("y queda marcada como vista", seen)
    assertFalse("la guía cerrada deja el velo fuera", exists("guide_veil"))
  }

  @Test
  fun Saltar_la_guia_cierra_y_marca_desde_el_paso_2() {
    show(); start()
    composeTestRule.onNodeWithTag("btn_guide_next").performClick(); composeTestRule.waitForIdle()
    assertEquals(GuideStep.TODOS, step)
    composeTestRule.onNodeWithTag("btn_guide_skip").performClick(); composeTestRule.waitForIdle()
    assertNull(step)
    assertTrue(seen)
  }

  @Test
  fun Atras_de_Android_salta_la_guia() {
    show(); start()
    composeTestRule.runOnUiThread { backOwner!!.onBackPressedDispatcher.onBackPressed() }
    composeTestRule.waitForIdle()
    assertNull("Atrás = «Saltar la guía»: cierra y no vuelve un paso", step)
    assertTrue(seen)
  }

  @Test
  fun tocar_lo_iluminado_avanza_igual_que_Seguir_y_NO_abre_el_Contrarreloj() {
    show(); start()
    val real = targets.rects.getValue(GuideTarget.TIME_TRIAL)
    composeTestRule.onRoot().performTouchInput { click(Offset(real.center.x, real.center.y)) }       // un toque en el botón REAL, que está bajo el hueco
    composeTestRule.waitForIdle()
    assertEquals("el toque sobre lo iluminado hace lo mismo que «Seguir»", GuideStep.TODOS, step)
    assertEquals("y no lanzó el Contrarreloj (una partida de 2 minutos sin haberla pedido)", 0, trialed)
    val all = targets.rects.getValue(GuideTarget.ALL_GAMES)
    composeTestRule.onRoot().performTouchInput { click(Offset(all.center.x, all.center.y)) }
    composeTestRule.waitForIdle()
    assertEquals(GuideStep.BARRA, step)
    assertEquals("«Todos los juegos» tampoco se abrió", 0, allGames)
  }

  @Test
  fun tocar_el_velo_no_hace_nada_y_no_deja_pasar_el_toque_a_Hoy() {
    show(); start()
    composeTestRule.onRoot().performTouchInput { click(Offset(8f, 8f)) }                                // arriba a la izquierda: velo, lejos de todo
    composeTestRule.onRoot().performTouchInput { click(Offset(width / 2f, height * 0.12f)) }            // sobre el saludo de Hoy, bajo el velo
    composeTestRule.waitForIdle()
    assertEquals("el velo no cierra ni avanza la guía", GuideStep.CONTRARRELOJ, step)
    assertFalse(seen)
    assertEquals(0, trialed)
    // un toque sobre la barra de abajo (fuera del hueco del paso 1) tampoco cambia de pestaña: la barra ni siquiera está en el árbol
    assertFalse("la barra de abajo no está en el árbol de accesibilidad", exists("nav_item_tu"))
  }

  // ---------------------------------------------------------------- accesibilidad

  @Test
  fun mientras_esta_abierta_Hoy_y_la_barra_quedan_fuera_del_lector_y_el_globo_dice_lo_de_la_tabla() {
    show(); start()
    for (tag in listOf("btn_all_games", "btn_timetrial", "nav_item_tu", "nav_item_progreso", "home_greeting", "home_card")) assertFalse("«$tag» no debe estar en el árbol de accesibilidad con la guía abierta", exists(tag))
    for (s in HoyGuide.steps) {
      composeTestRule.runOnUiThread { step = s }; composeTestRule.waitForIdle()
      assertEquals(HoyGuide.spoken(s), describe("guide_globe"))
      assertTrue("el globo se anuncia solo (live region)", composeTestRule.onNodeWithTag("guide_globe").fetchSemanticsNode().config.contains(SemanticsProperties.LiveRegion))
    }
    // al cerrarla, Hoy vuelve al lector
    composeTestRule.runOnUiThread { step = null }; composeTestRule.waitForIdle()
    assertTrue(exists("btn_all_games")); assertTrue(exists("nav_item_tu"))
  }

  private fun assertChipsAtLeast48() {
    composeTestRule.onNodeWithTag("btn_guide_next").assertHeightIsAtLeast(48.dp).assertWidthIsAtLeast(48.dp)
    composeTestRule.onNodeWithTag("btn_guide_skip").assertHeightIsAtLeast(48.dp).assertWidthIsAtLeast(48.dp)
  }

  @Test
  fun los_chips_miden_48_dp_o_mas_con_letra_1_0() { show(); start(); assertChipsAtLeast48() }

  @Test
  fun los_chips_miden_48_dp_o_mas_con_letra_2_0() { show(fontScale = 2f); start(); assertChipsAtLeast48() }

  // ---------------------------------------------------------------- dónde cae el globo

  /** El globo entero queda dentro de la pantalla, no cruza el hueco y no tapa «Saltar la guía». */
  private fun assertGlobeClearOfHole(s: GuideStep, widthDp: Int, heightDp: Int, label: String) {
    composeTestRule.runOnUiThread { step = s }; composeTestRule.waitForIdle()
    val d = composeTestRule.density.density
    val globe = bounds("guide_globe_slab")
    val nubi = bounds("guide_nubi")
    val hole = holeOf(s)
    val skip = bounds("btn_guide_skip")
    assertTrue("$label ${s.name}: el globo $globe se sale de la pantalla", globe.left >= -1f && globe.right <= widthDp * d + 1f && globe.top >= -1f && globe.bottom <= heightDp * d + 1f)
    assertFalse("$label ${s.name}: el globo $globe cruza el hueco $hole", globe.overlaps(hole))
    assertFalse("$label ${s.name}: Nubi $nubi cruza el hueco $hole", nubi.overlaps(hole))
    assertFalse("$label ${s.name}: el globo $globe tapa «Saltar la guía» $skip", globe.overlaps(skip))
    assertFalse("$label ${s.name}: el hueco $hole tapa «Saltar la guía» $skip", hole.overlaps(skip))
  }

  @Test
  fun con_letra_1_0_el_globo_de_cada_paso_cae_fuera_del_hueco() {
    show(); start()
    for (s in HoyGuide.steps) assertGlobeClearOfHole(s, 393, 873, "letra 1,0")
  }

  @Test
  fun con_letra_1_3_Nubi_pasa_a_S_y_el_globo_sigue_dentro_de_393_por_873_sin_cruzar_el_hueco() {
    show(fontScale = 1.3f); start()
    for (s in HoyGuide.steps) assertGlobeClearOfHole(s, 393, 873, "letra 1,3")
    val nubi = bounds("guide_nubi")
    assertTrue("con letra 1,3 Nubi es S (72 dp): mide ${nubi.width / composeTestRule.density.density} dp", nubi.width / composeTestRule.density.density <= 80f)
  }

  @Test
  fun con_letra_2_0_el_hueco_queda_a_la_vista_y_el_globo_no_lo_cruza() {
    show(fontScale = 2f); start()
    for (s in HoyGuide.steps) {
      composeTestRule.runOnUiThread { step = s }; composeTestRule.waitForIdle()
      val hole = holeOf(s)
      val d = composeTestRule.density.density
      assertTrue("letra 2,0 ${s.name}: el hueco $hole debe quedar a la vista tras traer el elemento (bringIntoView)", hole.top >= 0f && hole.bottom <= (873 + 9) * d)
      assertGlobeClearOfHole(s, 393, 873, "letra 2,0")
    }
  }

  /**
   * Decisión de Opus (30-oct): el blanco se trae a la vista SIEMPRE que no esté entero sobre la barra (no solo con letra 2,0). Con 1,3 el día del vuelo Hoy se desliza (4 filas: «Todos los juegos» queda 90 dp bajo la barra): en el paso 2 el botón debe quedar ENTERO y fuera de la barra antes de dibujar el hueco.
   */
  @Test
  fun con_letra_1_3_el_dia_del_vuelo_cada_paso_deja_su_blanco_entero_y_sobre_la_barra() {
    show(fontScale = 1.3f); start()
    val d = composeTestRule.density.density
    for (s in listOf(GuideStep.CONTRARRELOJ, GuideStep.TODOS)) {
      composeTestRule.runOnUiThread { step = s }; composeTestRule.waitForIdle()
      val target = targets.rects.getValue(targetOfStep(s))
      val bar = targets.rects.getValue(GuideTarget.TAB_BAR)
      assertTrue("${s.name}: el botón debe verse entero (mide ${target.height / d} dp, un botón mide 48 dp o más)", target.height / d >= 47.5f)
      assertTrue("${s.name}: el botón $target debe quedar sobre la barra (empieza en ${bar.top})", target.bottom <= bar.top + 1f)
      val hole = bounds("guide_hole")
      assertEquals("${s.name}: el hueco se dibujó sobre el botón ya a la vista", target.top - 8f * d, hole.top, 2f)
      assertEquals(target.bottom + 8f * d, hole.bottom, 2f)
    }
  }

  @Test
  fun a_360_dp_de_ancho_con_letra_1_3_tambien_cabe() {
    show(fontScale = 1.3f, widthDp = 360, heightDp = 780); start()
    for (s in HoyGuide.steps) assertGlobeClearOfHole(s, 360, 780, "360 × 780 letra 1,3")
  }

  // ---------------------------------------------------------------- el código

  @Test
  fun la_guia_no_pide_el_sol_ni_un_texto_bajo_16_sp() {
    val code = java.io.File("src/main/java/com/example/ui/components/HoyGuideOverlay.kt").readLines().filterNot { it.trim().startsWith("*") || it.trim().startsWith("/**") || it.trim().startsWith("//") }.joinToString("\n")
    assertFalse("la guía no pide el sol (el único sol de Hoy es el Contrarreloj iluminado)", code.contains("Clay.Sun") || code.contains("SlabColor.SUN"))
    for (m in Regex("""fontSize = (\d+(?:\.\d+)?)\.sp""").findAll(code)) assertTrue("texto de ${m.groupValues[1]} sp: la app no baja de 16 sp", m.groupValues[1].toFloat() >= 16f)
  }

  /** Tarea 194 (Ricardo: «las letras de los recuadros me gustaría que se vean algo más oscuras o remarcadas»): el texto del globo va en tinta, 18 sp y `SemiBold` (con `AppFamily`, Fredoka SemiBold, como los botones); «Paso N de 3» en tinta y peso normal; ya no se usa la tinta apagada. */
  @Test
  fun el_globo_va_en_tinta_con_el_texto_firme_y_el_contador_en_tinta() {
    val code = java.io.File("src/main/java/com/example/ui/components/HoyGuideOverlay.kt").readLines().filterNot { it.trim().startsWith("*") || it.trim().startsWith("/**") || it.trim().startsWith("//") }
    val text = code.single { it.contains("HoyGuide.text(s, startingPoint)") }
    assertTrue("el texto del globo: tinta, 18 sp y SemiBold: $text", text.contains("color = Clay.Ink") && text.contains("fontSize = 18.sp") && text.contains("FontWeight.SemiBold") && !text.contains("FontWeight.Medium"))
    val counter = code.single { it.contains("HoyGuide.counter(step)") }
    assertTrue("«Paso N de 3» en tinta y peso normal: $counter", counter.contains("color = Clay.Ink") && counter.contains("FontWeight.Normal") && !counter.contains("InkMuted"))
    assertFalse("ninguna letra de la guía va en la tinta apagada", code.any { it.contains("InkMuted") })
  }

  // ---------------------------------------------------------------- quien la aloja: cuándo sale (con el ViewModel de verdad)

  private fun today(): String = SimpleDateFormat("yyyy-MM-dd", Locale.getDefault()).format(Date())

  /**
   * Un ViewModel con el arranque ya terminado. [flightDone] = la marca `first_flight_done` (terminó el Primer vuelo completo); [flightDayRoute] = el camino de hoy quedó cumplido con los 4 juegos del vuelo (si no, uno normal sin empezar: un vuelo sin jugar nada, u otro día);
   * [games] = cuántas partidas guardadas hay (Avance muestra «un punto de partida» con al menos una).
   */
  private fun viewModelFor(flightDone: Boolean = true, seenAlready: Boolean = false, flightDayRoute: Boolean = false, games: Int = 0): NeuroVidaViewModel {
    runBlocking {
      val db = NeuroVidaDatabase.getDatabase(app)
      db.dailySessionDao().insertOrUpdate(
        if (flightDayRoute) DailySessionEntity(today(), "secuencia,freno,aterrizaje,meteoros", completedCount = 4, scoresRaw = "")
        else DailySessionEntity(today(), "bodega,calculo,intrusa", completedCount = 0, scoresRaw = "")
      )
      repeat(games) { i -> db.gameResultDao().insert(com.example.data.local.GameResultEntity("g$i", "radar", 80, 8, 10, false, 2, System.currentTimeMillis() - i * 1000L)) }
    }
    val prefs = app.getSharedPreferences("profile_extra", android.content.Context.MODE_PRIVATE).edit()
    if (seenAlready) prefs.putBoolean("hoy_guide_seen", true)
    if (flightDone) prefs.putBoolean("first_flight_done", true)
    prefs.commit()
    val vm = NeuroVidaViewModel(app).also { viewModels.add(it) }
    TestSupport.awaitUntil(message = "el arranque no quedó listo") { vm.startupStage.value == StartupStage.READY }
    return vm
  }

  private var above by mutableStateOf(false)

  private fun showHost(vm: NeuroVidaViewModel) {
    composeTestRule.setContent {
      CompositionLocalProvider(LocalReduceMotion provides true, LocalGuideTargets provides targets) {
        NeuroVidaTheme {
          Box(Modifier.width(393.dp).height(873.dp)) {
            BoardBackground()
            HomeContent(ui = flightDayUi(), onAllGames = {}, onPlay = {}, onResume = {}, onBaseline = {}, onTimeTrial = {}, modifier = Modifier.padding(top = 24.dp, bottom = 95.dp))
            Box(Modifier.align(Alignment.BottomCenter)) { NeuroNavBar(current = AppTab.HOY, onSelect = {}) }
            HoyGuideHost(viewModel = vm, targets = targets, somethingAbove = above)
          }
        }
      }
    }
    composeTestRule.waitForIdle()
  }

  /** Cambia lo que hay encima de Hoy (una medalla, un resultado…) y deja que Compose lo vea, sin mover el reloj de pruebas. */
  private fun putAbove(value: Boolean) { composeTestRule.runOnUiThread { above = value }; composeTestRule.waitForIdle() }

  @Test
  fun al_terminar_el_vuelo_sin_marca_sale_600_ms_despues_y_con_las_medallas_cerradas() {
    val vm = viewModelFor(flightDone = true, flightDayRoute = true, games = 4)
    assertTrue("el camino de hoy quedó cumplido con el vuelo", vm.dailySession.value.isFlightDay)
    above = true                                                      // una medalla encima: no sale, ni pasado el tiempo
    showHost(vm)
    composeTestRule.mainClock.advanceTimeBy(2_000)
    assertNull(vm.guideStep.value); assertFalse(exists("guide_veil"))
    putAbove(false)                                                    // se cerró la medalla: Hoy libre
    composeTestRule.mainClock.advanceTimeBy(500)
    assertNull("a los 500 ms todavía no (espera ${HoyGuide.STEP_DELAY_MS} ms)", vm.guideStep.value)
    composeTestRule.mainClock.advanceTimeBy(500)
    assertEquals("pasado el tiempo sale, desde el paso 1", GuideStep.CONTRARRELOJ, vm.guideStep.value)
    composeTestRule.mainClock.advanceTimeBy(300)
    assertTrue(exists("guide_veil")); assertTrue(exists("btn_guide_next")); assertTrue(exists("btn_guide_skip"))
    assertEquals(HoyGuide.spoken(GuideStep.CONTRARRELOJ), describe("guide_globe"))
  }

  @Test
  fun una_medalla_que_llega_a_mitad_de_la_espera_la_cancela() {
    val vm = viewModelFor(flightDone = true, flightDayRoute = true, games = 4)
    showHost(vm)
    composeTestRule.mainClock.advanceTimeBy(400)
    putAbove(true)                                                     // llega una medalla tarde
    composeTestRule.mainClock.advanceTimeBy(3_000)
    assertNull("la guía no pisa la medalla", vm.guideStep.value)
    putAbove(false)
    composeTestRule.mainClock.advanceTimeBy(1_000)
    assertEquals(GuideStep.CONTRARRELOJ, vm.guideStep.value)
  }

  @Test
  fun si_llega_algo_con_la_guia_abierta_se_esconde_y_vuelve_cuando_Hoy_queda_libre() {
    val vm = viewModelFor(flightDone = true, flightDayRoute = true, games = 4)
    showHost(vm)
    composeTestRule.mainClock.advanceTimeBy(1_000)
    assertEquals(GuideStep.CONTRARRELOJ, vm.guideStep.value)
    composeTestRule.mainClock.advanceTimeBy(300); assertTrue(exists("guide_veil"))
    putAbove(true)
    composeTestRule.mainClock.advanceTimeBy(100)
    assertFalse("con algo encima el velo se esconde", exists("guide_veil"))
    assertEquals("pero la guía sigue en su paso", GuideStep.CONTRARRELOJ, vm.guideStep.value)
    putAbove(false)
    composeTestRule.mainClock.advanceTimeBy(300)
    assertTrue("y vuelve", exists("guide_veil"))
  }

  @Test
  fun sin_haber_terminado_el_vuelo_completo_no_sale_ni_con_la_ruta_cumplida() {
    // el camino cumplido sin la marca (p. ej. quien terminó el vuelo antes de esta versión) NO dispara la guía: lo que la dispara es «terminó el vuelo»
    val vm = viewModelFor(flightDone = false, flightDayRoute = true, games = 4)
    assertTrue(vm.dailySession.value.isFlightDay)
    showHost(vm)
    composeTestRule.mainClock.advanceTimeBy(5_000)
    assertNull(vm.guideStep.value); assertFalse(exists("guide_veil"))
  }

  @Test
  fun con_la_marca_puesta_no_sale_nunca() {
    val vm = viewModelFor(flightDone = true, seenAlready = true, flightDayRoute = true, games = 4)
    assertTrue(vm.hoyGuideSeen.value)
    showHost(vm)
    composeTestRule.mainClock.advanceTimeBy(5_000)
    assertNull(vm.guideStep.value); assertFalse(exists("guide_veil"))
  }

  /** Tarea 193b (Ricardo, 30-oct): el vuelo terminó con «Terminar después» en todo: ningún juego, sin medalla y con la ruta de hoy normal para jugarla. La guía sale igual. */
  @Test
  fun un_vuelo_terminado_sin_jugar_ningun_juego_tambien_sale_con_la_ruta_normal() {
    val vm = viewModelFor(flightDone = true, flightDayRoute = false, games = 0)
    assertFalse("hoy no es un día de vuelo cumplido: la ruta es la normal, sin empezar", vm.dailySession.value.isFlightDay)
    assertEquals(0, vm.dailySession.value.completedCount)
    assertTrue(vm.achievementQueue.value.isEmpty())
    showHost(vm)
    composeTestRule.mainClock.advanceTimeBy(1_000)
    assertEquals(GuideStep.CONTRARRELOJ, vm.guideStep.value)
    composeTestRule.mainClock.advanceTimeBy(300)
    assertTrue(exists("guide_veil")); assertTrue(exists("btn_guide_next")); assertTrue(exists("btn_guide_skip"))
    assertEquals(HoyGuide.spoken(GuideStep.CONTRARRELOJ), describe("guide_globe"))
  }

  @Test
  fun sin_partidas_el_paso_3_dice_como_vas_en_cada_area_y_con_partidas_el_punto_de_partida() {
    val sin = viewModelFor(flightDone = true, games = 0)
    showHost(sin)
    composeTestRule.mainClock.advanceTimeBy(1_300)
    composeTestRule.runOnUiThread { sin.guideNext(); sin.guideNext() }; composeTestRule.waitForIdle()
    assertEquals(GuideStep.BARRA, sin.guideStep.value)
    assertEquals(HoyGuide.spoken(GuideStep.BARRA, startingPoint = false), describe("guide_globe"))
  }

  @Test
  fun con_partidas_el_paso_3_dice_tu_punto_de_partida() {
    val con = viewModelFor(flightDone = true, flightDayRoute = true, games = 2)
    showHost(con)
    composeTestRule.mainClock.advanceTimeBy(1_300)
    composeTestRule.runOnUiThread { con.guideNext(); con.guideNext() }; composeTestRule.waitForIdle()
    assertEquals(GuideStep.BARRA, con.guideStep.value)
    assertEquals(HoyGuide.spoken(GuideStep.BARRA, startingPoint = true), describe("guide_globe"))
  }

  /** Con «Terminar después» Hoy suma «Encuentra tu punto de partida» bajo «Todos los juegos»: el enlace no choca con los huecos ni con el globo, con letra 1,0 y 1,3 (se mide con los rectángulos). */
  private fun zeroGamesUi() = HomeUi(
    name = "Ana", now = now, tier = RankTier.fromRating(0), streak = 0, weeklyGoal = 4, lamps = lamps,
    stops = homeStops(listOf("bodega", "calculo", "intrusa"), 0, AppLanguage.SPANISH), pausedTitle = null, flightDay = false, hasBaseline = false, baselineInvite = true, timeTrial = stroop
  )

  private fun unclippedTop(tag: String): Float { val n = composeTestRule.onNodeWithTag(tag).fetchSemanticsNode(); return n.layoutInfo.coordinates.positionInRoot().y }
  private fun unclippedBottom(tag: String): Float { val n = composeTestRule.onNodeWithTag(tag).fetchSemanticsNode(); return n.layoutInfo.coordinates.positionInRoot().y + n.layoutInfo.height }

  private fun assertInviteClear(fontScale: Float) {
    show(fontScale = fontScale, ui = zeroGamesUi())
    val d = composeTestRule.density.density
    val gap = unclippedTop("btn_home_baseline") - unclippedBottom("btn_all_games")         // lo que separa al enlace de «Todos los juegos»: no cambia al deslizar Hoy
    start()
    for (s in HoyGuide.steps) {
      composeTestRule.runOnUiThread { step = s }; composeTestRule.waitForIdle()
      assertGlobeClearOfHole(s, 393, 873, "sin juegos, letra $fontScale")
      if (s == GuideStep.TODOS) {
        val hole = bounds("guide_hole")
        val linkTop = targets.rects.getValue(GuideTarget.ALL_GAMES).bottom + gap
        assertTrue("sin juegos, letra $fontScale: el aro del paso 2 (llega a ${hole.bottom}) no debe llegar al texto del enlace (empieza ~12 dp bajo $linkTop)", hole.bottom <= linkTop + 12f * d)
        assertTrue("y el globo queda ARRIBA del hueco, lejos del enlace", bounds("guide_globe_slab").bottom <= hole.top + 1f)
      }
    }
  }

  @Test
  fun sin_juegos_con_el_enlace_del_punto_de_partida_los_huecos_y_el_globo_no_chocan_con_letra_1_0() = assertInviteClear(1f)

  @Test
  fun sin_juegos_con_el_enlace_del_punto_de_partida_los_huecos_y_el_globo_no_chocan_con_letra_1_3() = assertInviteClear(1.3f)

  @Test
  fun el_paso_vive_en_el_ViewModel_y_los_botones_lo_mueven_y_terminan_con_la_marca() {
    val vm = viewModelFor(flightDone = true, flightDayRoute = true, games = 4)
    vm.startGuide()
    assertEquals(GuideStep.CONTRARRELOJ, vm.guideStep.value)
    vm.startGuide()                                                     // otra llamada no la reinicia
    vm.guideNext(); assertEquals(GuideStep.TODOS, vm.guideStep.value)
    vm.guideNext(); assertEquals(GuideStep.BARRA, vm.guideStep.value)
    assertFalse("la marca se escribe al TERMINAR, no antes", vm.hoyGuideSeen.value)
    vm.guideNext()
    assertNull(vm.guideStep.value); assertTrue(vm.hoyGuideSeen.value)
    assertNotNull(app.getSharedPreferences("profile_extra", android.content.Context.MODE_PRIVATE).all["hoy_guide_seen"])
    vm.startGuide()
    assertNull("con la marca ya no se vuelve a abrir", vm.guideStep.value)
  }

  @Test
  fun saltar_desde_cualquier_paso_marca_y_si_la_app_se_cierra_a_mitad_vuelve_desde_el_paso_1() {
    val vm = viewModelFor(flightDone = true, flightDayRoute = true, games = 4)
    vm.startGuide(); vm.guideNext()
    assertEquals(GuideStep.TODOS, vm.guideStep.value)
    // otro ViewModel (la app se cerró en el paso 2): sin marca, la guía arranca de nuevo desde el paso 1
    val vm2 = NeuroVidaViewModel(app).also { viewModels.add(it) }
    assertNull(vm2.guideStep.value); assertFalse(vm2.hoyGuideSeen.value)
    vm2.startGuide()
    assertEquals(GuideStep.CONTRARRELOJ, vm2.guideStep.value)
    vm2.guideSkip()
    assertNull(vm2.guideStep.value); assertTrue(vm2.hoyGuideSeen.value)
  }

  /** Tarea 193b: ya no se limita al día del vuelo. Cerrada a mitad, la app se reabre OTRO día (la ruta de hoy es una normal, no la del vuelo) y la guía vuelve desde el paso 1. */
  @Test
  fun cerrada_a_mitad_y_con_la_app_reabierta_otro_dia_vuelve_desde_el_paso_1() {
    val first = viewModelFor(flightDone = true, flightDayRoute = true, games = 4)
    first.startGuide(); first.guideNext()
    assertEquals(GuideStep.TODOS, first.guideStep.value)
    // «otro día»: ya no es el día del vuelo (la ruta de hoy es normal y está sin empezar)
    runBlocking { NeuroVidaDatabase.getDatabase(app).dailySessionDao().insertOrUpdate(DailySessionEntity(today(), "bodega,calculo,intrusa", completedCount = 0, scoresRaw = "")) }
    TestSupport.release(first); viewModels.remove(first)
    val vm = NeuroVidaViewModel(app).also { viewModels.add(it) }
    TestSupport.awaitUntil(message = "el arranque no quedó listo") { vm.startupStage.value == StartupStage.READY }
    TestSupport.awaitUntil(message = "la ruta de hoy no cambió") { !vm.dailySession.value.isFlightDay }
    assertFalse("otro día, sin vuelo cumplido", vm.dailySession.value.isFlightDay)
    assertTrue("la marca del vuelo sigue (en disco)", vm.firstFlightDone.value)
    assertFalse(vm.hoyGuideSeen.value)
    showHost(vm)
    composeTestRule.mainClock.advanceTimeBy(1_000)
    assertEquals("vuelve desde el paso 1", GuideStep.CONTRARRELOJ, vm.guideStep.value)
  }

  @Test
  fun despues_de_Listo_nunca_mas_ni_en_otra_sesion() {
    val vm = viewModelFor(flightDone = true, flightDayRoute = true, games = 4)
    vm.startGuide(); vm.guideNext(); vm.guideNext(); vm.guideNext()                  // el tercer «Listo»
    assertNull(vm.guideStep.value); assertTrue(vm.hoyGuideSeen.value)
    TestSupport.release(vm); viewModels.remove(vm)
    val vm2 = NeuroVidaViewModel(app).also { viewModels.add(it) }
    TestSupport.awaitUntil(message = "el arranque no quedó listo") { vm2.startupStage.value == StartupStage.READY }
    assertTrue("la marca se guardó en disco", vm2.hoyGuideSeen.value)
    showHost(vm2)
    composeTestRule.mainClock.advanceTimeBy(10_000)
    assertNull(vm2.guideStep.value); assertFalse(exists("guide_veil"))
    vm2.startGuide()
    assertNull(vm2.guideStep.value)
  }
}
