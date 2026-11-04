package com.example

import androidx.compose.foundation.clickable
import androidx.compose.foundation.layout.Box
import androidx.compose.foundation.layout.fillMaxSize
import androidx.compose.foundation.layout.height
import androidx.compose.foundation.layout.width
import androidx.compose.material3.Text
import androidx.compose.runtime.CompositionLocalProvider
import androidx.compose.runtime.DisposableEffect
import androidx.compose.runtime.SideEffect
import androidx.compose.runtime.getValue
import androidx.compose.runtime.mutableIntStateOf
import androidx.compose.runtime.mutableStateOf
import androidx.compose.runtime.saveable.rememberSaveable
import androidx.compose.runtime.setValue
import androidx.compose.runtime.snapshots.Snapshot
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.platform.testTag
import androidx.compose.ui.test.assertCountEquals
import androidx.compose.ui.test.assertIsDisplayed
import androidx.compose.ui.test.assertIsSelected
import androidx.compose.ui.test.click
import androidx.compose.ui.test.junit4.createComposeRule
import androidx.compose.ui.test.onAllNodesWithText
import androidx.compose.ui.test.onNodeWithTag
import androidx.compose.ui.test.onNodeWithText
import androidx.compose.ui.test.onRoot
import androidx.compose.ui.test.performClick
import androidx.compose.ui.test.performTouchInput
import androidx.compose.ui.test.swipeLeft
import androidx.compose.ui.test.swipeRight
import androidx.compose.ui.unit.dp
import com.example.data.TimeTrialState
import com.example.data.Week
import com.example.model.AppLanguage
import com.example.model.RankTier
import com.example.ui.components.BoardBackground
import com.example.ui.components.KeepTabsState
import com.example.ui.components.LocalBarInset
import com.example.ui.components.LocalReduceMotion
import com.example.ui.components.LocalTabActive
import com.example.ui.components.NeuroNavBar
import com.example.ui.components.NubiIdle
import com.example.ui.components.NubiMotion
import com.example.ui.components.TabFade
import com.example.ui.components.TAB_WARM_GAP_MS
import com.example.ui.components.TAB_WARM_START_MS
import com.example.ui.components.TabHost
import com.example.ui.components.rememberNubiIdle
import com.example.ui.components.rememberTabFade
import com.example.ui.screens.HomeContent
import com.example.ui.screens.HomeTimeTrial
import com.example.ui.screens.HomeUi
import com.example.ui.screens.homeStops
import com.example.ui.theme.NeuroVidaTheme
import com.example.viewmodel.AppTab
import com.github.takahirom.roborazzi.RobolectricDeviceQualifiers
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
import java.util.Calendar

/**
 * Las cinco pestañas sin pager (Tarea 224, 4-nov, idea de Ricardo: «no se siente prolijo ni suave, como que se pega un poco»): la pestaña cambia SOLO al tocar la barra (arrastrar hacia los costados no hace nada), SIEMPRE con el mismo fundido de 300 ms
 * (vecina o no) y, con «quitar animaciones», de golpe; una página ya visitada queda compuesta y escondida (no se vuelve a componer ni pierde lo que tenía, no se ve, no se lee y no recibe toques). Estas pruebas arman el mismo cableado que `MainActivity`
 * (`TabHost` + `NeuroNavBar`) con páginas de mentira que registran cuándo se componen, cuándo están a la vista y cuántas veces se crearon, y REGISTRAN cuadro por cuadro la opacidad y la escala del fundido. Reemplazan a `TabPagerBarTest` y `TabPagerFundidoTest`.
 */
@RunWith(RobolectricTestRunner::class)
@GraphicsMode(GraphicsMode.Mode.NATIVE)
@Config(qualifiers = RobolectricDeviceQualifiers.Pixel8, sdk = [36])
class TabHostTest {
  @get:Rule val rule = createComposeRule()

  @Before fun fastFrames() = TestSupport.fastFrames()

  private var tab by mutableStateOf(AppTab.HOY)
  private lateinit var fade: TabFade
  /** Las páginas compuestas AHORA, cuántas veces se creó cada una (una página que se recompone «desde cero» suma 1 más), si está a la vista, cuántos toques recibió y lo que recuerda (`rememberSaveable`). */
  private val live = mutableSetOf<AppTab>()
  private val created = mutableMapOf<AppTab, Int>()
  private val onScreen = mutableMapOf<AppTab, Boolean>()
  private val clicks = mutableMapOf<AppTab, Int>()
  private val remembered = mutableMapOf<AppTab, androidx.compose.runtime.MutableIntState>()
  private var hostShown by mutableStateOf(true)

  private class Frame(val ms: Long, val alpha: Float, val scale: Float, val active: Set<AppTab>, val composed: Set<AppTab>)

  private fun show(reduce: Boolean = false, startAt: AppTab = AppTab.HOY, warmUp: Boolean = false) {
    tab = startAt
    rule.mainClock.autoAdvance = false
    rule.setContent {
      CompositionLocalProvider(LocalReduceMotion provides reduce) {
        NeuroVidaTheme {
          Box(Modifier.width(393.dp).height(700.dp)) {
            BoardBackground()
            val f = rememberTabFade(); fade = f
            KeepTabsState(show = hostShown) {
              TabHost(currentTab = tab, fade = f, warmUp = warmUp) { t ->
                DisposableEffect(t) { live += t; created[t] = (created[t] ?: 0) + 1; onDispose { live -= t } }
                val active = LocalTabActive.current
                SideEffect { onScreen[t] = active }
                val memory = rememberSaveable(t.name) { mutableIntStateOf(0) }
                remembered[t] = memory
                Box(Modifier.fillMaxSize().testTag("pagina_${t.name}").clickable { clicks[t] = (clicks[t] ?: 0) + 1 }) { Text("página ${t.ordinal}") }
              }
            }
            Box(Modifier.align(Alignment.BottomCenter)) { NeuroNavBar(current = tab, onSelect = { tab = it }) }
          }
        }
      }
    }
    rule.mainClock.advanceTimeBy(100)
    rule.waitForIdle()
  }

  private fun snapshot(ms: Long) = Frame(ms, fade.contentAlpha, fade.contentScale, onScreen.filterValues { it }.keys.filter { it in live }.toSet(), live.toSet())

  /** Avanza [totalMs] de a un cuadro (16 ms) y devuelve lo que se vio en cada uno. */
  private fun frames(totalMs: Long): List<Frame> {
    val out = mutableListOf<Frame>()
    var t = 0L
    while (t < totalMs) { rule.mainClock.advanceTimeBy(16); t += 16; out += snapshot(t) }
    return out
  }

  /** Lo mismo que tocar la pestaña de la barra (`onSelect = { tab = it }`), con el reloj a mano. */
  private fun tap(to: AppTab) { rule.runOnUiThread { tab = to; Snapshot.sendApplyNotifications() } }

  /** Va a [to] y deja pasar el fundido entero. */
  private fun goTo(to: AppTab): List<Frame> { tap(to); return frames(480) }

  private val tabs = AppTab.entries

  @Test
  fun entre_cualquier_par_de_pestanas_es_el_mismo_fundido_de_300_ms_vecinas_o_no() {
    show()
    var transitions = 0
    for (src in tabs) for (dst in tabs) {
      if (src == dst) continue
      if (tab != src) goTo(src)
      val fr = goTo(dst)
      transitions++
      val label = "$src → $dst"
      // el fundido: la opacidad baja casi a cero y vuelve a 1; entra con la escala mínima de 0,96 y termina en 1
      val dip = fr.minBy { it.alpha }
      assertTrue("$label: la opacidad baja casi a cero (${dip.alpha})", dip.alpha <= 0.1f)
      assertEquals("$label: entra con la escala mínima de ${NubiMotion.TAB_FADE_MIN_SCALE}", NubiMotion.TAB_FADE_MIN_SCALE, fr.minOf { it.scale }, 0.01f)
      // la página nueva se pone en lo más transparente (nunca con la de antes todavía a la vista, ni las dos a la vez)…
      val switched = fr.first { dst in it.active }
      assertTrue("$label: la página cambia con la opacidad baja (${switched.alpha}) y no antes de que casi no se vea (a los ${switched.ms} ms)", switched.alpha <= 0.35f && switched.ms in 48L..160L)
      for (f in fr) assertTrue("$label: a los ${f.ms} ms hay ${f.active} a la vez a la vista", f.active.size <= 1)
      assertTrue("$label: la de antes ya no está a la vista cuando entra la nueva", fr.filter { dst in it.active }.none { src in it.active })
      // …el fundido de entrada dura lo de una pestaña: arranca unos cuadros después y todo termina cerca de los 300 ms de un fundido normal
      val done = fr.first { it.ms > switched.ms && it.alpha == 1f && it.scale == 1f }
      assertTrue("$label: todo termina entre 280 y 460 ms (a los ${done.ms} ms)", done.ms in 280L..460L)
      assertEquals("$label: al final solo se ve la de destino", setOf(dst), fr.last().active)
      assertEquals(dst, tab)
    }
    assertEquals("se probaron los 20 pares", 20, transitions)
  }

  /** La primera visita sin tirón: con la app lista, las pestañas que faltan se componen escondidas, de a una, mientras se mira Hoy; al tocarlas ya están y no se vuelven a crear. */
  @Test
  fun con_el_calentamiento_las_pestanas_que_faltan_se_componen_escondidas_de_a_una_y_al_tocarlas_ya_estan() {
    show(warmUp = true)
    rule.mainClock.advanceTimeBy(TAB_WARM_START_MS - 200)
    assertEquals("antes del tiempo de espera solo está Hoy", setOf(AppTab.HOY), live)
    val seen = mutableListOf<Set<AppTab>>()
    var t = 0L
    while (t < 5 * TAB_WARM_GAP_MS + 1500) { rule.mainClock.advanceTimeBy(16); t += 16; if (seen.isEmpty() || seen.last() != live.toSet()) seen += live.toSet() }
    assertEquals("se compusieron las cinco, una por vez (cada paso suma UNA)", listOf(1, 2, 3, 4, 5), seen.map { it.size })
    assertEquals("y solo Hoy está a la vista", setOf(AppTab.HOY), snapshot(0).active)
    rule.onAllNodesWithText("página 1").assertCountEquals(0)                                           // las calentadas no se leen
    val creations = created.toMap()
    for (to in listOf(AppTab.JUEGOS, AppTab.CONTRARRELOJ, AppTab.PROGRESO, AppTab.TU)) goTo(to)
    assertEquals("tocar una pestaña calentada no la crea de nuevo", creations, created.toMap())
    assertEquals(setOf(AppTab.TU), snapshot(0).active)
  }

  @Test
  fun sin_calentamiento_una_pestana_se_compone_la_primera_vez_que_se_visita_y_ninguna_antes() {
    show()
    rule.mainClock.advanceTimeBy(TAB_WARM_START_MS + 5 * TAB_WARM_GAP_MS)
    assertEquals("sin calentamiento solo está la de ahora", setOf(AppTab.HOY), live)
  }

  @Test
  fun una_pestana_que_no_es_vecina_nunca_compone_las_del_medio() {
    show()
    goTo(AppTab.TU)
    assertEquals("de Hoy a Tú solo se compusieron esas dos", setOf(AppTab.HOY, AppTab.TU), live)
    for (middle in listOf(AppTab.JUEGOS, AppTab.CONTRARRELOJ, AppTab.PROGRESO)) assertFalse("$middle no llegó a crearse", middle in created)
  }

  @Test
  fun la_barra_marca_el_destino_de_una_vez_sin_esperar_al_fundido() {
    show()
    rule.mainClock.autoAdvance = true
    rule.onNodeWithTag("nav_item_contrarreloj").performClick()
    rule.onNodeWithTag("nav_item_contrarreloj").assertIsSelected()
  }

  @Test
  fun con_quitar_animaciones_cambia_de_golpe_sin_fundido() {
    show(reduce = true)
    tap(AppTab.TU)
    val fr = frames(160)
    for (f in fr) { assertEquals("sin fundido a los ${f.ms} ms", 1f, f.alpha, 0f); assertEquals(1f, f.scale, 0f) }
    assertEquals("de golpe: a los 48 ms ya se ve Tú", setOf(AppTab.TU), fr.first { it.ms >= 48 }.active)
    assertEquals(AppTab.TU, tab)
  }

  @Test
  fun arrastrar_hacia_los_costados_no_cambia_de_pestana_en_ninguna() {
    show()
    rule.mainClock.autoAdvance = true
    for (start in tabs) {
      rule.runOnUiThread { tab = start; Snapshot.sendApplyNotifications() }
      rule.waitForIdle()
      rule.onRoot().performTouchInput { swipeLeft() }
      rule.waitForIdle()
      assertEquals("$start: arrastrar a la izquierda no cambia la pestaña", start, tab)
      rule.onRoot().performTouchInput { swipeRight() }
      rule.waitForIdle()
      assertEquals("$start: arrastrar a la derecha no cambia la pestaña", start, tab)
      rule.onNodeWithText("página ${start.ordinal}").assertIsDisplayed()
      assertEquals("$start: ni una sola vez empezó un fundido", 1f, fade.contentAlpha, 0f)
    }
  }

  @Test
  fun arrastrar_en_horizontal_sobre_Hoy_de_verdad_no_cambia_la_pestana_ni_mueve_la_pantalla() {
    val now = Calendar.getInstance().apply { set(2026, Calendar.NOVEMBER, 4, 16, 0, 0); set(Calendar.MILLISECOND, 0) }.timeInMillis
    val ui = HomeUi(
      name = "Ricardo", now = now, tier = RankTier.fromRating(0), streak = 3, weeklyGoal = 4, lamps = Week.Lamps(listOf(true, true, false, false, false, false, true), todayIndex = 6),
      stops = homeStops(listOf("bodega", "calculo", "intrusa"), 1, AppLanguage.SPANISH), pausedTitle = null, flightDay = false, hasBaseline = true,
      timeTrial = HomeTimeTrial("stroop", "Tinta o Palabra", TimeTrialState.PENDIENTE, record = 41)
    )
    tab = AppTab.HOY
    rule.setContent {
      CompositionLocalProvider(LocalReduceMotion provides true, LocalBarInset provides 88.dp) {
        NeuroVidaTheme {
          Box(Modifier.width(393.dp).height(873.dp)) {
            BoardBackground()
            TabHost(currentTab = tab) { t -> if (t == AppTab.HOY) HomeContent(ui = ui, onPlay = {}, onResume = {}, onBaseline = {}) else Text("página ${t.ordinal}") }
            Box(Modifier.align(Alignment.BottomCenter)) { NeuroNavBar(current = tab, onSelect = { tab = it }) }
          }
        }
      }
    }
    rule.waitForIdle()
    rule.onNodeWithTag("home_greeting").assertIsDisplayed()
    for (swipe in listOf<androidx.compose.ui.test.TouchInjectionScope.() -> Unit>({ swipeLeft() }, { swipeRight() })) {
      rule.onNodeWithTag("home_scroll").performTouchInput(swipe)
      rule.waitForIdle()
      assertEquals("arrastrar de lado sobre Hoy no cambia la pestaña", AppTab.HOY, tab)
      rule.onNodeWithTag("home_greeting").assertIsDisplayed()
      rule.onNodeWithTag("nav_item_hoy").assertIsSelected()
    }
    rule.onRoot().performTouchInput { swipeLeft() }
    rule.waitForIdle()
    assertEquals("y arrastrando por toda la pantalla, tampoco", AppTab.HOY, tab)
    rule.onNodeWithTag("home_greeting").assertIsDisplayed()
  }

  @Test
  fun una_pestana_ya_visitada_no_se_recompone_al_volver_ni_pierde_lo_que_recuerda() {
    show()
    goTo(AppTab.JUEGOS); remembered.getValue(AppTab.JUEGOS).intValue = 7                              // «la búsqueda» de Juegos
    goTo(AppTab.CONTRARRELOJ); remembered.getValue(AppTab.CONTRARRELOJ).intValue = 3                  // «el juego elegido» del Contrarreloj
    goTo(AppTab.HOY)
    goTo(AppTab.JUEGOS)
    goTo(AppTab.CONTRARRELOJ)
    goTo(AppTab.TU); goTo(AppTab.JUEGOS)
    assertEquals("Juegos recuerda lo suyo", 7, remembered.getValue(AppTab.JUEGOS).intValue)
    assertEquals("Contrarreloj recuerda lo suyo", 3, remembered.getValue(AppTab.CONTRARRELOJ).intValue)
    for ((t, n) in created) assertEquals("$t se creó UNA sola vez (volver no la recompone desde cero)", 1, n)
    assertEquals("las visitadas siguen compuestas, escondidas", setOf(AppTab.HOY, AppTab.JUEGOS, AppTab.CONTRARRELOJ, AppTab.TU), live)
    assertEquals("pero solo la de ahora está a la vista", setOf(AppTab.JUEGOS), snapshot(0).active)
  }

  @Test
  fun lo_que_guarda_KeepTabsState_sigue_igual_cuando_un_juego_o_un_panel_sacan_las_pestanas() {
    show()
    goTo(AppTab.JUEGOS); remembered.getValue(AppTab.JUEGOS).intValue = 7
    goTo(AppTab.CONTRARRELOJ); remembered.getValue(AppTab.CONTRARRELOJ).intValue = 3
    rule.runOnUiThread { hostShown = false; Snapshot.sendApplyNotifications() }
    rule.mainClock.advanceTimeBy(100)
    assertTrue("con el panel encima no hay pestañas compuestas", live.isEmpty())
    rule.runOnUiThread { hostShown = true; Snapshot.sendApplyNotifications() }
    rule.mainClock.advanceTimeBy(100)
    assertEquals("al volver se abre en la pestaña de antes, sin fundido", setOf(AppTab.CONTRARRELOJ), live)
    assertEquals(1f, fade.contentAlpha, 0f)
    assertEquals("la pestaña de antes recuerda lo suyo", 3, remembered.getValue(AppTab.CONTRARRELOJ).intValue)
    goTo(AppTab.JUEGOS)
    assertEquals("y la que no se veía, también, cuando se vuelve a ella", 7, remembered.getValue(AppTab.JUEGOS).intValue)
  }

  @Test
  fun las_paginas_escondidas_no_se_leen_ni_reciben_toques() {
    show()
    goTo(AppTab.JUEGOS)
    goTo(AppTab.HOY)
    assertEquals(setOf(AppTab.HOY, AppTab.JUEGOS), live)
    rule.onNodeWithText("página 0").assertIsDisplayed()
    rule.onAllNodesWithText("página 1").assertCountEquals(0)                                           // la escondida no está en el árbol que lee el lector de pantalla
    rule.onNodeWithTag("pagina_HOY").performClick()
    rule.onRoot().performTouchInput { click() }
    assertEquals("solo la de Hoy recibió los toques", null, clicks[AppTab.JUEGOS])
    assertTrue("y la de Hoy sí", (clicks[AppTab.HOY] ?: 0) >= 1)
  }

  @Test
  fun tocar_otra_pestana_y_arrepentirse_a_mitad_del_fundido_de_salida_vuelve_sin_cambiar_de_pagina_ni_dejarla_a_medias() {
    show()
    tap(AppTab.TU)
    val salida = frames(48)                                                                            // la página de Hoy se está desvaneciendo y todavía no se cambió
    assertEquals(setOf(AppTab.HOY), salida.last().active)
    assertTrue("a medio fundido (${salida.last().alpha})", salida.last().alpha < 1f)
    tap(AppTab.HOY)
    val fr = frames(480)
    assertFalse("Tú nunca llegó a crearse", AppTab.TU in created)
    assertTrue("y siempre se vio Hoy", fr.all { it.active == setOf(AppTab.HOY) })
    assertEquals("la página vuelve a su opacidad", 1f, fr.last().alpha, 0f)
    assertEquals(1f, fr.last().scale, 0f)
    assertEquals(AppTab.HOY, tab)
  }

  @Test
  fun tocar_una_tercera_pestana_mientras_entra_la_nueva_va_a_la_ultima_sin_esperar() {
    show()
    tap(AppTab.JUEGOS)
    frames(160)                                                                                        // Juegos ya se puso y está entrando
    assertEquals(setOf(AppTab.JUEGOS), snapshot(0).active)
    tap(AppTab.PROGRESO)
    val fr = frames(480)
    assertEquals("termina en Avance", setOf(AppTab.PROGRESO), fr.last().active)
    assertEquals(1f, fr.last().alpha, 0f); assertEquals(1f, fr.last().scale, 0f)
    for (f in fr) assertTrue("a los ${f.ms} ms hay ${f.active} a la vez a la vista", f.active.size <= 1)
    assertEquals(AppTab.PROGRESO, tab)
  }

  /** Nubi (el vaivén y los parpadeos) no pide cuadros con su pestaña escondida: compuesta, pero quieta, y al volver arranca suave. */
  @Test
  fun Nubi_se_queda_quieta_con_su_pestana_escondida() {
    var active by mutableStateOf(false)
    rule.mainClock.autoAdvance = false
    rule.setContent {
      CompositionLocalProvider(LocalReduceMotion provides false, LocalTabActive provides active) {
        rememberNubiIdle()
        Box(Modifier.testTag("nubi"))
      }
    }
    val idle = NubiIdle.lastForTests ?: error("Nubi no se compuso")
    for (i in 1..120) { rule.mainClock.advanceTimeBy(50); assertEquals("escondida: ni sube ni baja a los ${i * 50} ms", 0f, idle.floatDp, 0f); assertEquals(0f, idle.eyesClosed, 0f) }
    rule.runOnUiThread { active = true; Snapshot.sendApplyNotifications() }
    var moved = false
    for (i in 1..120) { rule.mainClock.advanceTimeBy(50); if (idle.floatDp != 0f) moved = true }
    assertTrue("a la vista, vuelve a subir y bajar", moved)
  }

  @Test
  fun ya_no_hay_pager_ni_candado_de_deslizado_en_el_codigo() {
    val main = File("src/main/java").walkTopDown().filter { it.isFile && it.extension == "kt" }.toList()
    val offenders = main.filter { f -> val s = f.readText(); "HorizontalPager" in s || "LocalTabSwipeLock" in s || "rememberTabPager" in s || "PagerNavBar" in s || "swipeLock" in s }.map { it.name }
    assertTrue("sin pager ni candado de deslizado: $offenders", offenders.isEmpty())
    assertFalse("TabPager.kt ya no existe", File("src/main/java/com/example/ui/components/TabPager.kt").exists())
    assertFalse("TabSwipeLock.kt ya no existe", File("src/main/java/com/example/ui/components/TabSwipeLock.kt").exists())
    val activity = File("src/main/java/com/example/MainActivity.kt").readText()
    assertTrue("MainActivity usa TabHost y marca la barra con la pestaña del ViewModel", "TabHost(currentTab = currentTab" in activity && "NeuroNavBar(current = currentTab" in activity)
    assertEquals("el fundido de las pestañas es el de siempre (300 ms = 90 + 210)", NubiMotion.TAB_MS, NubiMotion.TAB_FADE_OUT_MS + NubiMotion.TAB_FADE_IN_MS)
  }
}
