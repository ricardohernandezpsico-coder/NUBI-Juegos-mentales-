package com.example

import android.app.Application
import androidx.activity.ComponentActivity
import androidx.compose.foundation.layout.Box
import androidx.compose.foundation.layout.height
import androidx.compose.foundation.layout.width
import androidx.compose.runtime.CompositionLocalProvider
import androidx.compose.ui.Modifier
import androidx.compose.ui.platform.LocalDensity
import androidx.compose.ui.layout.positionInRoot
import androidx.compose.ui.semantics.Role
import androidx.compose.ui.semantics.SemanticsProperties
import androidx.compose.ui.test.assertIsDisplayed
import androidx.compose.ui.test.hasTestTag
import androidx.compose.ui.test.junit4.createAndroidComposeRule
import androidx.compose.ui.test.onNodeWithTag
import androidx.compose.ui.test.performClick
import androidx.compose.ui.test.performScrollToIndex
import androidx.compose.ui.test.performSemanticsAction
import androidx.compose.ui.unit.Density
import androidx.compose.ui.unit.dp
import androidx.test.core.app.ApplicationProvider
import com.example.data.local.NeuroVidaDatabase
import com.example.data.local.toEntity
import com.example.model.AgeBand
import com.example.model.UserSettings
import com.example.ui.components.LocalReduceMotion
import com.example.ui.components.TU_BUTTON_END
import com.example.ui.components.TU_BUTTON_LABEL
import com.example.ui.components.TU_BUTTON_TOUCH
import com.example.ui.components.VOLVER_LABEL
import com.example.ui.components.tuInitial
import com.example.ui.theme.NeuroVidaTheme
import com.example.viewmodel.AppTab
import com.example.viewmodel.NeuroVidaViewModel
import com.example.viewmodel.TopPanel
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
 * «Tú» sin pestaña (Tarea 225, 4-nov, decisión de Ricardo): sale de la barra (quedan cuatro) y pasa a UN botón redondo arriba a la derecha, el mismo en Hoy, Juegos, Contrarreloj y Avance (mismo margen derecho, misma altura, 48 dp de toque) con la inicial del nombre; al tocarlo se
 * abre Tú a pantalla completa, sin la barra, con su «‹» arriba a la izquierda (Atrás hace lo mismo) y se vuelve a la pestaña de donde se abrió; lo que se abre desde Tú (Ajustes, «En qué se basa Nubilus», «Preguntas frecuentes», «De dónde sale la referencia») vuelve a Tú, y lo que se abre desde una
 * pestaña, a ella. Estas pruebas componen la app REAL (`NeuroVidaApp`) con su ViewModel.
 */
@RunWith(RobolectricTestRunner::class)
@GraphicsMode(GraphicsMode.Mode.NATIVE)
@Config(qualifiers = "w393dp-h873dp-440dpi", sdk = [36])
class TuFlowTest {
  @get:Rule val rule = createAndroidComposeRule<ComponentActivity>()

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

  private fun viewModel(name: String = "Ricardo"): NeuroVidaViewModel {
    runBlocking { NeuroVidaDatabase.getDatabase(app).userProfileDao().insertOrUpdate(UserSettings(ageBand = AgeBand.ADULT, name = name, weeklyGoal = 4).toEntity()) }
    val vm = NeuroVidaViewModel(app).also { viewModels.add(it) }
    TestSupport.awaitUntil(30_000, "El perfil no se cargó") { vm.userSettings.value.ageBand != null }
    return vm
  }

  private fun show(vm: NeuroVidaViewModel, widthDp: Int = 393, heightDp: Int = 873, fontScale: Float = 1f) {
    rule.setContent {
      val base = LocalDensity.current
      NeuroVidaTheme {
        CompositionLocalProvider(LocalReduceMotion provides true, LocalDensity provides Density(base.density, fontScale)) {
          Box(Modifier.width(widthDp.dp).height(heightDp.dp)) { NeuroVidaApp(viewModel = vm) }
        }
      }
    }
    rule.waitForIdle()
  }

  private fun exists(tag: String) = rule.onAllNodes(hasTestTag(tag), useUnmergedTree = true).fetchSemanticsNodes().isNotEmpty()
  private fun pressBack() { rule.runOnUiThread { rule.activity.onBackPressedDispatcher.onBackPressed() }; rule.waitForIdle() }
  private fun bounds(tag: String) = rule.onNodeWithTag(tag, useUnmergedTree = true).fetchSemanticsNode().boundsInRoot
  // El botón «Tú» vive en la cabecera de cada pestaña (Tarea 225b): se busca en el árbol UNIDO, donde las páginas escondidas de `TabHost` no tienen semántica; así es siempre el de la pestaña a la vista.
  private fun button() = rule.onNodeWithTag("tu_button")
  private fun buttonBounds() = button().fetchSemanticsNode().boundsInRoot
  private fun existsMerged(tag: String) = rule.onAllNodes(hasTestTag(tag)).fetchSemanticsNodes().isNotEmpty()
  private val cfg = androidx.compose.runtime.mutableStateOf(Triple(393, 873, 1f))
  /** Como [show], pero con el tamaño y la letra en un estado: se cambian sin volver a componer la app (una prueba solo puede llamar `setContent` una vez). */
  private fun showConfigurable(vm: NeuroVidaViewModel) {
    rule.setContent {
      val base = LocalDensity.current
      val (w, h, scale) = cfg.value
      NeuroVidaTheme {
        CompositionLocalProvider(LocalReduceMotion provides true, LocalDensity provides Density(base.density, scale)) {
          Box(Modifier.width(w.dp).height(h.dp)) { NeuroVidaApp(viewModel = vm) }
        }
      }
    }
    rule.waitForIdle()
  }
  private fun configure(w: Int, h: Int, scale: Float) { rule.runOnIdle { cfg.value = Triple(w, h, scale) }; rule.waitForIdle() }
  private fun goTo(vm: NeuroVidaViewModel, tab: AppTab) { rule.runOnUiThread { vm.setTab(tab) }; rule.waitForIdle() }

  @Test
  fun la_inicial_es_la_primera_letra_en_mayuscula_y_sin_nombre_va_la_persona() {
    assertEquals('R', tuInitial("ricardo")); assertEquals('A', tuInitial("  ana")); assertEquals('Ñ', tuInitial("ñandú")); assertEquals('7', tuInitial("7 Sabios"))
    assertNull("sin nombre", tuInitial("")); assertNull("solo espacios", tuInitial("   ")); assertNull("empieza con un signo", tuInitial("¿?"))
  }

  @Test
  fun el_boton_Tu_esta_en_las_cuatro_pestanas_en_el_mismo_lugar_con_48_dp_de_toque_y_dice_Tu_perfil_y_ajustes() {
    val vm = viewModel()
    show(vm)
    val d = rule.density.density
    val rects = mutableMapOf<AppTab, androidx.compose.ui.geometry.Rect>()
    for (tab in AppTab.entries) {
      goTo(vm, tab)
      button().assertIsDisplayed()
      assertEquals("UN solo botón Tú en ${tab.name}", 1, rule.onAllNodes(hasTestTag("tu_button")).fetchSemanticsNodes().size)
      val node = button().fetchSemanticsNode()
      assertEquals("lo lee el lector como botón con su frase", listOf(TU_BUTTON_LABEL), node.config[SemanticsProperties.ContentDescription])
      assertEquals(Role.Button, node.config[SemanticsProperties.Role])
      rects[tab] = node.boundsInRoot
      val b = node.boundsInRoot
      assertEquals("$tab: se toca en 48 dp de ancho", TU_BUTTON_TOUCH.value, b.width / d, 0.6f)
      assertEquals("$tab: y en 48 dp de alto", TU_BUTTON_TOUCH.value, b.height / d, 0.6f)
      assertEquals("$tab: el margen derecho es de ${TU_BUTTON_END.value} dp", 393f - TU_BUTTON_END.value, b.right / d, 0.6f)
    }
    val first = rects.getValue(AppTab.HOY)
    for ((tab, r) in rects) {
      assertEquals("$tab: misma altura que en Hoy", first.top, r.top, 0.5f)
      assertEquals("$tab: mismo lugar a lo ancho que en Hoy", first.left, r.left, 0.5f)
    }
    assertEquals("arriba, con su centro a 24 dp del borde de arriba: la línea de los títulos y del logotipo", 24f, first.center.y / d, 0.8f)
  }

  @Test
  fun el_boton_lleva_la_inicial_del_nombre_y_sin_nombre_la_persona_de_la_barra_de_antes() {
    val vm = viewModel("ricardo")
    show(vm)
    button().assertIsDisplayed()
    val withName = rule.onAllNodes(androidx.compose.ui.test.hasText("R"), useUnmergedTree = true).fetchSemanticsNodes()
    assertTrue("la inicial «R» está dentro del botón", withName.any { n -> val b = n.boundsInRoot; val btn = buttonBounds(); b.center.x in btn.left..btn.right && b.center.y in btn.top..btn.bottom })
    rule.runOnUiThread { vm.setName("") }; rule.waitForIdle()
    TestSupport.awaitUntil(10_000) { vm.userSettings.value.name.isBlank() }
    rule.waitForIdle()
    val btn = buttonBounds()
    assertTrue("sin nombre no hay letra dentro del botón", rule.onAllNodes(androidx.compose.ui.test.hasText("R"), useUnmergedTree = true).fetchSemanticsNodes().none { n -> n.boundsInRoot.center.x in btn.left..btn.right && n.boundsInRoot.center.y in btn.top..btn.bottom })
  }

  @Test
  fun tocar_el_boton_abre_Tu_a_pantalla_completa_sin_la_barra_y_Atras_vuelve_a_la_pestana_de_donde_se_abrio() {
    val vm = viewModel()
    show(vm)
    goTo(vm, AppTab.PROGRESO)
    assertTrue("la barra está", exists("bottom_nav_bar"))
    button().performClick(); rule.waitForIdle()
    assertTrue("Tú está abierto en el ViewModel", vm.tuOpen.value)
    assertTrue("Tú se ve (su título y su Nubi)", exists("tu_title") && exists("tu_nubi"))
    assertFalse("a pantalla completa, SIN la barra", exists("bottom_nav_bar"))
    assertFalse("y sin el botón Tú (ya estás en Tú)", existsMerged("tu_button"))
    rule.onNodeWithTag("tu_back", useUnmergedTree = true).assertIsDisplayed()
    val back = rule.onNodeWithTag("tu_back", useUnmergedTree = true).fetchSemanticsNode()
    assertEquals("el «‹» se llama Volver", listOf(VOLVER_LABEL), back.config[SemanticsProperties.ContentDescription])
    assertEquals(Role.Button, back.config[SemanticsProperties.Role])
    assertEquals("48 dp", 48f, back.boundsInRoot.width / rule.density.density, 0.6f)
    assertTrue("arriba a la izquierda", back.boundsInRoot.left / rule.density.density < 24f && back.boundsInRoot.top / rule.density.density < 8f)
    pressBack()
    assertFalse("Atrás cierra Tú", vm.tuOpen.value)
    assertFalse(exists("tu_title"))
    assertTrue("y vuelve la barra y la pestaña de donde se abrió", exists("bottom_nav_bar") && existsMerged("tu_button"))
    assertEquals("en la misma pestaña (Avance)", AppTab.PROGRESO, vm.currentTab.value)
    assertTrue("con su contenido (el título «Avance»)", exists("avance_week") || rule.onAllNodes(androidx.compose.ui.test.hasText("Avance"), useUnmergedTree = true).fetchSemanticsNodes().isNotEmpty())
  }

  @Test
  fun la_flecha_Volver_hace_lo_mismo_que_Atras() {
    val vm = viewModel()
    show(vm)
    button().performClick(); rule.waitForIdle()
    assertTrue(vm.tuOpen.value)
    rule.onNodeWithTag("tu_back", useUnmergedTree = true).performClick(); rule.waitForIdle()
    assertFalse(vm.tuOpen.value)
    assertEquals(AppTab.HOY, vm.currentTab.value)
    assertTrue(exists("bottom_nav_bar"))
  }

  @Test
  fun lo_que_se_abre_desde_Tu_vuelve_a_Tu_y_lo_que_se_abre_desde_una_pestana_vuelve_a_ella() {
    val vm = viewModel()
    show(vm)
    button().performClick(); rule.waitForIdle()
    for (panel in listOf(TopPanel.AJUSTES, TopPanel.FUENTES, TopPanel.PREGUNTAS, TopPanel.REFERENCIA)) {
      rule.runOnUiThread { vm.openTopPanel(panel) }; rule.waitForIdle()
      assertFalse("$panel: el panel va encima de Tú", exists("tu_title"))
      pressBack()
      assertNull("$panel: Atrás cierra el panel", vm.topPanel.value)
      assertTrue("$panel: y vuelve a Tú, no a la pestaña", vm.tuOpen.value && exists("tu_title"))
    }
    pressBack()
    assertFalse(vm.tuOpen.value)
    // el panel de la referencia abierto desde la pestaña Contrarreloj vuelve a la pestaña, no a Tú
    goTo(vm, AppTab.CONTRARRELOJ)
    rule.runOnUiThread { vm.openTopPanel(TopPanel.REFERENCIA) }; rule.waitForIdle()
    pressBack()
    assertFalse("no se abrió Tú", vm.tuOpen.value || exists("tu_title"))
    assertEquals(AppTab.CONTRARRELOJ, vm.currentTab.value)
  }

  @Test
  fun el_titulo_de_cada_pestana_no_choca_con_el_boton_ni_con_letra_2_0_a_360_dp() {
    val vm = viewModel()
    show(vm, 360, 780, fontScale = 2f)
    val titles = listOf(AppTab.JUEGOS to "all_games_title", AppTab.CONTRARRELOJ to "contrarreloj_title", AppTab.PROGRESO to "avance_title")
    for ((tab, tag) in titles) {
      goTo(vm, tab)
      val button = buttonBounds(); val title = bounds(tag)
      assertTrue("$tab: el título termina (${title.right}) antes de donde empieza el botón (${button.left}) o queda arriba/abajo de él", title.right <= button.left + 1f || title.bottom <= button.top + 1f || title.top >= button.bottom - 1f)
    }
    goTo(vm, AppTab.HOY)
    assertTrue("Hoy: el logotipo queda a la izquierda del botón", bounds("home_logo").right <= buttonBounds().left + 1f)
  }

  // ---------------------------------------------------------------- Tarea 225b: el botón es parte de la cabecera de cada pestaña

  @Test
  fun en_Hoy_el_boton_se_va_hacia_arriba_con_la_pagina_y_nunca_queda_sobre_la_tarjeta() {
    val vm = viewModel()
    show(vm, 393, 520)                                                    // una pantalla baja, para que Hoy se deslice
    val d = rule.density.density
    val offsetOf = { rule.onNodeWithTag("home_scroll").fetchSemanticsNode().config[SemanticsProperties.VerticalScrollAxisRange].value() }
    val maxOf = { rule.onNodeWithTag("home_scroll").fetchSemanticsNode().config[SemanticsProperties.VerticalScrollAxisRange].maxValue() }
    assertTrue("Hoy se desliza al menos 200 dp en esta pantalla (${maxOf() / d} dp)", maxOf() >= 200f * d)
    // `boundsInRoot` recorta lo que sale de la pantalla: para seguir al botón por arriba se usa su posición sin recortar
    fun buttonRect(): androidx.compose.ui.geometry.Rect { val n = button().fetchSemanticsNode(); return androidx.compose.ui.geometry.Rect(n.positionInRoot, androidx.compose.ui.geometry.Size(n.size.width.toFloat(), n.size.height.toFloat())) }
    val top0 = buttonRect().top
    assertEquals("sin deslizar, el centro del botón está a 24 dp del borde de arriba", 24f, buttonBounds().center.y / d, 0.8f)
    for (target in listOf(0f, 60f, 120f, 200f)) {
      val delta = target * d - offsetOf()
      rule.onNodeWithTag("home_scroll").performSemanticsAction(androidx.compose.ui.semantics.SemanticsActions.ScrollBy) { it(0f, delta) }
      rule.waitForIdle()
      val scrolled = offsetOf()
      val b = buttonRect()
      assertEquals("con Hoy deslizada ${scrolled / d} dp, el botón subió lo mismo (va en la fila del logotipo)", top0 - scrolled, b.top, 1.5f)
      assertFalse("el botón $b nunca queda sobre la tarjeta de la ruta ${bounds("home_card")}", b.overlaps(bounds("home_card")))
      if (scrolled >= 60f * d) assertTrue("deslizada ${scrolled / d} dp el botón ya salió por arriba (su borde de abajo queda en ${b.bottom / d} dp)", b.bottom <= 0f)
    }
  }

  @Test
  fun lo_que_va_debajo_del_boton_queda_a_8_dp_o_mas_de_su_sombra_en_Juegos_Contrarreloj_y_Avance_con_las_seis_configuraciones() {
    val vm = viewModel()
    showConfigurable(vm)
    val d = rule.density.density
    val below = listOf(AppTab.JUEGOS to "all_games_search", AppTab.CONTRARRELOJ to "contrarreloj_subtitle", AppTab.PROGRESO to "avance_week")
    val report = StringBuilder()
    for ((w, h) in listOf(393 to 873, 360 to 780)) for (scale in listOf(1f, 1.3f, 2f)) {
      configure(w, h, scale)
      for ((tab, tag) in below) {
        goTo(vm, tab)
        // (cambiar la letra en caliente deja la lista de Juegos donde estaba: con letra grande el buscador es su primer elemento y quedaría scrolleado; en el teléfono la Activity se recrea y empieza arriba)
        if (tab == AppTab.JUEGOS) rule.onNodeWithTag("all_games_list").performScrollToIndex(0)
        rule.waitForIdle()
        val b = buttonBounds()
        // la posición sin recortar (`boundsInRoot` da 0 a lo que la lista de Juegos todavía no recortó o no pintó): así el aire es el de verdad, nunca un cero que pase por casualidad
        val nn = rule.onNodeWithTag(tag, useUnmergedTree = true).fetchSemanticsNode()
        assertTrue("$w dp, letra $scale, $tab: «$tag» tiene tamaño (${nn.size})", nn.size.height > 0 && nn.size.width > 0)
        val air = (nn.positionInRoot.y - b.bottom) / d
        report.append("$w dp · letra $scale · $tab: ${"%.1f".format(air)} dp de aire bajo el botón\n")
        assertTrue("$w dp, letra $scale, $tab: «$tag» empieza a ${"%.1f".format(air)} dp de la sombra del botón (pide 8 dp o más)", air >= 7.9f)
        assertEquals("$w dp, letra $scale, $tab: el botón sigue a 24 dp del borde de arriba, margen 16 dp", 24f, b.center.y / d, 0.8f)
        assertEquals("$w dp, letra $scale, $tab: y con 16 dp de margen a la derecha", w - 16f, b.right / d, 0.6f)
      }
    }
    println(report)
  }

  @Test
  fun en_Hoy_el_saludo_empieza_pegado_a_la_fila_del_logotipo_y_el_boton_no_lo_toca_con_letra_2_0_a_360_dp() {
    val vm = viewModel()
    showConfigurable(vm)
    val d = rule.density.density
    for (scale in listOf(1f, 1.3f, 2f)) {
      configure(360, 780, scale)
      goTo(vm, AppTab.HOY)
      val b = buttonBounds(); val greeting = bounds("home_greeting")
      assertTrue("letra $scale: el saludo $greeting no pasa bajo el botón $b", greeting.top >= b.bottom - 1f || greeting.right <= b.left + 1f)
      assertTrue("letra $scale: el logotipo queda a la izquierda del botón", bounds("home_logo").right <= b.left + 1f)
      assertEquals("letra $scale: el saludo sigue empezando 48 dp bajo el borde de arriba de Hoy", 48f, (greeting.top - bounds("home_scroll").top) / d, 0.8f)
    }
  }
}
