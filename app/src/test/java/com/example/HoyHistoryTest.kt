package com.example

import androidx.compose.foundation.layout.Box
import androidx.compose.foundation.layout.height
import androidx.compose.foundation.layout.padding
import androidx.compose.foundation.layout.width
import androidx.compose.runtime.CompositionLocalProvider
import androidx.compose.ui.Modifier
import androidx.compose.ui.geometry.Offset
import androidx.compose.ui.graphics.luminance
import androidx.compose.ui.platform.LocalDensity
import androidx.compose.ui.semantics.SemanticsActions
import androidx.compose.ui.semantics.getOrNull
import androidx.compose.ui.semantics.SemanticsNode
import androidx.compose.ui.semantics.SemanticsProperties
import androidx.compose.ui.test.assertHasNoClickAction
import androidx.compose.ui.test.hasTestTag
import androidx.compose.ui.test.junit4.createComposeRule
import androidx.compose.ui.test.onNodeWithTag
import androidx.compose.ui.test.onRoot
import androidx.compose.ui.test.performScrollTo
import androidx.compose.ui.test.performTouchInput
import androidx.compose.ui.test.swipeDown
import androidx.compose.ui.test.swipeUp
import androidx.compose.ui.text.TextLayoutResult
import androidx.compose.ui.unit.Density
import androidx.compose.ui.unit.dp
import com.example.data.HistoryCard
import com.example.data.HistoryRow
import com.example.data.TimeTrialState
import com.example.data.Week
import com.example.model.AppLanguage
import com.example.model.RankTier
import com.example.ui.components.BoardBackground
import com.example.ui.screens.HomeContent
import com.example.ui.screens.HomeTimeTrial
import com.example.ui.screens.HomeUi
import com.example.ui.screens.homeStops
import com.example.ui.theme.Clay
import com.example.ui.theme.NeuroVidaTheme
import com.github.takahirom.roborazzi.RobolectricDeviceQualifiers
import org.junit.Assert.assertEquals
import org.junit.Assert.assertFalse
import org.junit.Assert.assertNull
import org.junit.Assert.assertTrue
import org.junit.Rule
import org.junit.Test
import org.junit.runner.RunWith
import org.robolectric.RobolectricTestRunner
import org.robolectric.annotation.Config
import org.robolectric.annotation.GraphicsMode
import java.util.Calendar

/**
 * «Tus últimas partidas» en Hoy (Tarea 221, la opción A que eligió Ricardo; `auditoria-fable/pestana-juegos-historial.md` §5 y §10): la tarjeta con su alto medido EN FILAS (tres y media), la lista que se desliza por dentro, los dos deslizados juntos («hacia abajo, Hoy primero;
 * hacia arriba, la lista primero»), el lector de pantalla, que no se toca y que sin partidas no hay tarjeta. Las medidas (cuánto hay que deslizar Hoy para ver la tarjeta entera) se imprimen con `MEDIDA`.
 */
@RunWith(RobolectricTestRunner::class)
@GraphicsMode(GraphicsMode.Mode.NATIVE)
@Config(qualifiers = RobolectricDeviceQualifiers.Pixel8, sdk = [36])
class HoyHistoryTest {
  @get:Rule val composeTestRule = createComposeRule()

  @org.junit.Before fun fastFrames() = TestSupport.fastFrames()          // sin esto, las pruebas que mueven una lista tardaban más de 60 s en quedar quietas (ver `TestSupport.fastFrames`)

  private val now = Calendar.getInstance().apply { set(2026, Calendar.OCTOBER, 26, 16, 0, 0) }.timeInMillis
  private val lamps = Week.Lamps(listOf(true, true, false, false, false, false, true), todayIndex = 6)

  /** Las diez filas de la lámina de Fable: las dos primeras con la marca en dos líneas (con letra 1,0 a 393 dp). */
  private val sample = listOf(
    HistoryRow("calculo", "Carga exacta", "hoy", "en tu ruta", "70 % de las cargas, sin pista", now - 1),
    HistoryRow("bodega", "Bodega de carga", "hoy", "en tu ruta", "75 % de los objetos, al primer intento", now - 2),
    HistoryRow("stroop", "Tinta o Palabra", "hoy", "Contrarreloj", "38 aciertos", now - 3, timeTrial = true),
    HistoryRow("secuencia", "Rastro de luz", "ayer", "a tu medida", "7 luces seguidas", now - 4),
    HistoryRow("satelites", "Satélites", "ayer", "en Desafío", "3,2 satélites a la vez", now - 5),
    HistoryRow("acoplamiento", "Acoplamiento", "ayer", "Contrarreloj", "21 aciertos", now - 6, timeTrial = true),
    HistoryRow("intrusa", "La estrella intrusa", "ayer", "en tu ruta", "80 % de aciertos", now - 7),
    HistoryRow("freno", "Freno de Emergencia", "hace 2 días", "a tu medida", null, now - 8),
    HistoryRow("parejas", "Constelaciones", "hace 2 días", "suave", "70 % de las veces, directo a una pareja ya vista", now - 9),
    HistoryRow("meteoros", "Lluvia de meteoros", "hace 3 días", "al límite", "61 % de las palabras menos comunes, reconocidas", now - 10)
  )

  private val stroop = HomeTimeTrial("stroop", "Tinta o Palabra", TimeTrialState.PENDIENTE, todayHits = null, record = 41)
  private val jugado = stroop.copy(state = TimeTrialState.JUGADO, todayHits = 38)
  private val marca = stroop.copy(state = TimeTrialState.MARCA_NUEVA, todayHits = 43, record = 43)
  private val sinPlus = stroop.copy(gamesLeft = 3)
  private val sinPlusJugado = jugado.copy(gamesLeft = 2)
  private val sinPlusMarca = marca.copy(gamesLeft = 2)
  private val sinPartidas = jugado.copy(gamesLeft = 0, tomorrowTitle = "¿Verdad o disparate?")
  private val sinPartidasMarca = marca.copy(gamesLeft = 0, tomorrowTitle = "¿Verdad o disparate?")

  private fun ui(completed: Int, trial: HomeTimeTrial?, history: List<HistoryRow>, paused: String? = null) = HomeUi(
    name = "Ricardo", now = now, tier = RankTier.fromRating(0), streak = 3, weeklyGoal = 4, lamps = lamps,
    stops = homeStops(listOf("bodega", "calculo", "intrusa"), completed, AppLanguage.SPANISH), pausedTitle = paused, flightDay = false, hasBaseline = true, timeTrial = trial, history = history
  )

  /** Los 14 estados del día de `HoyTarjetaTest` (§3.1 de Fable), con las diez filas. */
  private val states: List<Pair<String, HomeUi>> by lazy {
    listOf(
      "primera hora" to ui(0, stroop, sample), "a mitad de ruta" to ui(1, stroop, sample), "ruta completa" to ui(3, stroop, sample), "jugado" to ui(3, jugado, sample), "marca nueva" to ui(3, marca, sample),
      "marca nueva con la ruta a medias" to ui(1, marca, sample), "partida en pausa" to ui(1, stroop, sample, paused = "Carga exacta"), "sin Plus, ruta a medias" to ui(1, sinPlus, sample),
      "sin Plus, ruta completa" to ui(3, sinPlus, sample), "sin Plus, jugado" to ui(3, sinPlusJugado, sample), "sin Plus, marca nueva" to ui(3, sinPlusMarca, sample),
      "sin partidas" to ui(3, sinPartidas, sample), "sin partidas con marca nueva" to ui(3, sinPartidasMarca, sample), "sin partidas, ruta a medias" to ui(1, sinPartidas, sample)
    )
  }

  private class Shown(val ui: HomeUi, val widthDp: Int, val heightDp: Int, val fontScale: Float)
  private var shown: androidx.compose.runtime.MutableState<Shown>? = null

  private fun show(ui: HomeUi, widthDp: Int = 393, heightDp: Int = 873, fontScale: Float = 1f) {
    val args = Shown(ui, widthDp, heightDp, fontScale)
    val state = shown
    if (state != null) { state.value = args; composeTestRule.waitForIdle(); return }
    val holder = androidx.compose.runtime.mutableStateOf(args).also { shown = it }
    composeTestRule.setContent {
      val a = holder.value
      val base = LocalDensity.current
      CompositionLocalProvider(LocalDensity provides Density(base.density, a.fontScale), com.example.ui.components.LocalReduceMotion provides true) {
        NeuroVidaTheme {
          Box(Modifier.width(a.widthDp.dp).height(a.heightDp.dp)) {
            BoardBackground()
            HomeContent(ui = a.ui, onPlay = {}, onResume = {}, onBaseline = {}, onTimeTrial = {}, onLocked = {}, modifier = Modifier.padding(top = 24.dp, bottom = 163.dp))
          }
        }
      }
    }
    composeTestRule.waitForIdle()
  }

  private fun dp(px: Float) = px / composeTestRule.density.density
  private fun node(tag: String): SemanticsNode = composeTestRule.onNodeWithTag(tag).fetchSemanticsNode()
  private fun exists(tag: String) = composeTestRule.onAllNodes(hasTestTag(tag)).fetchSemanticsNodes().isNotEmpty()
  private fun topDp(tag: String) = dp(node(tag).positionInRoot.y)
  private fun bottomDp(tag: String): Float { val n = node(tag); return dp(n.positionInRoot.y + n.size.height) }
  private fun sizeHDp(tag: String) = dp(node(tag).size.height.toFloat())
  private fun viewportBottom(heightDp: Int) = (heightDp - 163).toFloat()
  /** El deslizado de Hoy y el de la lista, tal cual los publica su semántica (para la lista: el índice de la primera fila + la parte que se salió). */
  private fun hoyOffset() = node("home_scroll").config[SemanticsProperties.VerticalScrollAxisRange].value()
  private fun hoyMax() = node("home_scroll").config[SemanticsProperties.VerticalScrollAxisRange].maxValue()
  private fun listOffset() = node("home_history_list").config[SemanticsProperties.VerticalScrollAxisRange].value()

  // ---- Sin partidas no hay tarjeta; con 1 a 3, alto natural

  @Test
  fun sin_partidas_no_hay_tarjeta_ni_recuadro_que_lo_diga_y_la_marca_del_final_sigue_ahi() {
    show(ui(3, stroop, emptyList()))
    assertFalse("sin tarjeta", exists("home_history")); assertFalse(exists("home_history_title")); assertFalse(exists("home_history_row_1"))
    assertTrue("y Hoy termina donde siempre", exists("home_end"))
  }

  @Test
  fun con_una_a_tres_partidas_la_tarjeta_tiene_su_alto_natural_sin_recorte_ni_deslizado_por_dentro() {
    for (n in 1..3) for (fs in listOf(1f, 1.3f)) {
      show(ui(3, stroop, sample.take(n)), fontScale = fs)
      for (i in 1..n) assertTrue("con $n fila(s) y letra $fs la fila $i está", exists("home_history_row_$i"))
      assertFalse("sin cuarta fila", exists("home_history_row_${n + 1}"))
      val list = node("home_history_list")
      assertNull("con $n fila(s) la lista no se desliza por dentro", list.config.getOrNull(SemanticsProperties.VerticalScrollAxisRange))
      val last = bottomDp("home_history_row_$n")
      assertEquals("con $n fila(s) y letra $fs la ventana llega hasta el aire del final: la última fila + 10 dp", last + HistoryCard.END_AIR_DP, bottomDp("home_history_list"), 1.5f)
    }
  }

  // ---- El alto: tres filas y media (§10.6.3)

  private fun assertThreeAndAHalf(widthDp: Int, heightDp: Int, fontScale: Float) {
    show(ui(3, stroop, sample), widthDp, heightDp, fontScale)
    val label = "$widthDp × $heightDp, letra $fontScale"
    val list = node("home_history_list"); val listTop = dp(list.positionInRoot.y); val listBottom = listTop + dp(list.size.height.toFloat())
    assertTrue("$label: la tercera fila ENTERA queda dentro de la ventana (termina en ${bottomDp("home_history_row_3")}, la ventana en $listBottom)", bottomDp("home_history_row_3") <= listBottom + 1f)
    assertTrue("$label: la cuarta fila asoma (empieza en ${topDp("home_history_row_4")})", topDp("home_history_row_4") < listBottom - 1f)
    assertTrue("$label: y queda CORTADA por el borde de abajo (termina en ${bottomDp("home_history_row_4")})", bottomDp("home_history_row_4") > listBottom + 1f)
    val h4 = sizeHDp("home_history_row_4"); val visible = listBottom - topDp("home_history_row_4")
    println("MEDIDA tres filas y media $label: la cuarta mide %.1f dp y se ve %.1f (la mitad = %.1f); la ventana mide %.1f dp".format(h4, visible, h4 / 2f, listBottom - listTop))
    assertEquals("$label: lo visible de la cuarta es la mitad (±2 dp)", h4 / 2f, visible, 2f)
    assertFalse("$label: la quinta fila no está compuesta", exists("home_history_row_5"))
    // el alto medido de antemano (con `rememberTextMeasurer`) es el de las filas de verdad
    val rows = (1..4).map { sizeHDp("home_history_row_$it") }
    assertEquals("$label: la ventana = h1 + 1 + h2 + 1 + h3 + 1 + h4/2", HistoryCard.windowHeight(rows), dp(list.size.height.toFloat()), 1.5f)
    assertEquals("con 11 partidas guardadas, 10 filas: no hay una undécima", 10, sample.size)
  }

  @Test fun tres_filas_y_media_a_393_con_letra_1_0() = assertThreeAndAHalf(393, 873, 1f)
  @Test fun tres_filas_y_media_a_393_con_letra_1_3() = assertThreeAndAHalf(393, 873, 1.3f)
  @Test fun tres_filas_y_media_a_393_con_letra_2_0() = assertThreeAndAHalf(393, 873, 2f)
  @Test fun tres_filas_y_media_a_360_con_letra_1_0() = assertThreeAndAHalf(360, 780, 1f)
  @Test fun tres_filas_y_media_a_360_con_letra_1_3() = assertThreeAndAHalf(360, 780, 1.3f)
  @Test fun tres_filas_y_media_a_360_con_letra_2_0() = assertThreeAndAHalf(360, 780, 2f)

  @Test
  fun la_tarjeta_mide_259_dp_a_393_con_letra_1_0_con_las_filas_de_la_lamina_de_Fable() {
    show(ui(3, stroop, sample))
    val rows = (1..4).map { sizeHDp("home_history_row_$it") }
    println("MEDIDA filas a 393 con 1,0: " + rows.joinToString(" · ") { "%.1f".format(it) })
    val card = HistoryCard.height(rows, sizeHDp("home_history_title"))
    assertEquals("la tarjeta (sin su sombra) mide lo que dice la cuenta de §10.1", card, bottomDp("home_history_list") - topDp("home_history_title") + HistoryCard.TITLE_TOP_DP, 1.5f)
    assertTrue("y está cerca de los 259 dp de la lámina ($card)", card in 240f..275f)
  }

  // ---- Lo que se ve sin deslizar y lo que hay que deslizar (§10.6.1, 2, 4 y 7)

  /** La acción principal de Hoy (la ruta; con la ruta completa, lo primero del bloque del Contrarreloj) sigue ENTERA a la vista: la tarjeta va después de todo y no cambia nada de arriba. */
  private fun assertMainActionVisible(u: HomeUi, heightDp: Int, label: String) {
    val vp = viewportBottom(heightDp)
    val pending = u.stops.any { it.state != com.example.ui.screens.StopState.DONE }
    val (tag, needed) = when {
      u.pausedTitle != null -> "btn_resume_paused" to bottomDp("btn_resume_paused")
      pending -> "btn_home_play" to bottomDp("btn_home_play")
      exists("btn_timetrial") -> "btn_timetrial" to bottomDp("btn_timetrial") - 6f
      exists("home_timetrial_done") -> "home_timetrial_done" to topDp("home_timetrial_done") + 40f
      else -> "home_timetrial_none" to topDp("home_timetrial_none") + 40f
    }
    assertTrue("$label: la acción principal «$tag» necesita llegar a %.0f dp y la barra empieza en %.0f dp".format(needed, vp), needed <= vp + 1f)
  }

  @Test
  fun la_accion_principal_sigue_a_la_vista_en_todos_los_estados_a_393_con_1_0_y_1_3_y_a_360_con_1_0() {
    for ((w, h, fs) in listOf(Triple(393, 873, 1f), Triple(393, 873, 1.3f), Triple(360, 780, 1f))) for ((name, u) in states) {
      show(u, w, h, fs)
      assertMainActionVisible(u, h, "$name · $w × $h · letra $fs")
    }
  }

  @Test
  fun el_titulo_de_la_tarjeta_asoma_sin_deslizar_a_393_con_1_0_en_los_14_estados_y_con_1_3_cuando_la_ruta_esta_completa() {
    val vp = viewportBottom(873)
    for ((name, u) in states) {
      show(u, 393, 873, 1f)
      val bottom = bottomDp("home_history_title")
      println("MEDIDA título 393 con 1,0 · $name: termina en %.0f dp de %.0f (%s)".format(bottom, vp, if (bottom <= vp) "asoma" else "bajo la barra"))
      assertTrue("$name con letra 1,0: el título termina en %.0f dp y la barra empieza en %.0f".format(bottom, vp), bottom <= vp + 1f)
    }
    for (name in listOf("ruta completa", "jugado", "marca nueva")) {
      show(states.first { it.first == name }.second, 393, 873, 1.3f)
      assertTrue("$name con letra 1,3: el título asoma", bottomDp("home_history_title") <= vp + 1f)
    }
    for ((name, u) in states) { show(u, 360, 780, 1f); println("MEDIDA título 360 con 1,0 · $name: termina en %.0f dp de %.0f".format(bottomDp("home_history_title"), viewportBottom(780))) }
  }

  /** Cuánto hay que deslizar Hoy para ver la tarjeta entera (dp): el borde de abajo de la tarjeta (sin su sombra de 6 dp) contra donde empieza la barra. */
  private fun overBar(heightDp: Int) = bottomDp("home_history") - 6f - viewportBottom(heightDp)

  @Test
  fun medidas_cuanto_hay_que_deslizar_Hoy_para_ver_la_tarjeta_entera_en_cada_estado() {
    val rows = mutableListOf<String>()
    for ((w, h) in listOf(393 to 873, 360 to 780)) for (fs in listOf(1f, 1.3f, 2f)) {
      val cells = states.map { (name, u) -> show(u, w, h, fs); "%s %+.0f".format(name, overBar(h)) }
      rows += "MEDIDA tarjeta %dx%d letra %.1f: %s".format(w, h, fs, cells.joinToString(" | "))
    }
    rows.forEach { println(it) }
  }

  @Test
  fun deslizada_hasta_el_final_la_tarjeta_queda_entera_sobre_la_barra_en_todos_los_estados() {
    for ((w, h, fs) in listOf(Triple(393, 873, 1f), Triple(393, 873, 1.3f), Triple(393, 873, 2f), Triple(360, 780, 1f), Triple(360, 780, 1.3f))) for ((name, u) in states) {
      show(u, w, h, fs)
      composeTestRule.onNodeWithTag("home_history").performScrollTo()
      composeTestRule.waitForIdle()
      val over = overBar(h)
      assertTrue("$name · $w × $h · letra $fs: deslizada hasta el final, la tarjeta termina %.1f dp después de la barra".format(over), over <= 1f)
      assertTrue("y su título queda a la vista (sobre el borde de arriba)", topDp("home_history_title") >= 0f)
    }
  }

  // ---- Los dos deslizados (§10.3 y §10.6.5): el signo del delta lo fija esta prueba

  private fun listTouch(startDp: Float, endDp: Float, up: Boolean) =
    composeTestRule.onNodeWithTag("home_history_list").performTouchInput {
      if (up) swipeUp(startY = startDp.dp.toPx(), endY = endDp.dp.toPx(), durationMillis = 1500) else swipeDown(startY = startDp.dp.toPx(), endY = endDp.dp.toPx(), durationMillis = 1500)
    }

  @Test
  fun el_dedo_sube_Hoy_se_desliza_primero_y_la_lista_no_se_mueve_hasta_que_Hoy_llega_a_su_final_y_al_bajar_la_lista_vuelve_primero() {
    show(ui(3, stroop, sample))                                         // ruta completa a 393 × 873: la tarjeta queda a medias bajo la barra
    val vp = viewportBottom(873)
    assertTrue("la tarjeta está a medias: su borde de abajo (${bottomDp("home_history")}) pasa de la barra ($vp)", bottomDp("home_history") - 6f > vp + 20f)
    assertTrue("y la parte de arriba de la lista se ve (hay dónde poner el dedo)", topDp("home_history_list") + 60f < vp)
    assertEquals("Hoy está al principio", 0f, hoyOffset(), 0f)
    assertEquals("y la lista también", 0f, listOffset(), 0f)
    val remaining = hoyMax()
    assertTrue("a Hoy le queda por deslizar $remaining px, más de lo que se va a mover primero (30 dp)", remaining > 40f * composeTestRule.density.density)

    // 1. el dedo sube POCO sobre la tarjeta a medias: se mueve Hoy, no las filas
    listTouch(startDp = 50f, endDp = 20f, up = true)
    composeTestRule.waitForIdle()
    val afterFirst = hoyOffset()
    assertTrue("el dedo sube sobre la tarjeta a medias: Hoy avanzó ($afterFirst)", afterFirst > 0f)
    assertEquals("…y la lista sigue en su principio (la tarjeta todavía no está entera)", 0f, listOffset(), 0f)

    // 2. con Hoy en su final (la tarjeta entera sobre la barra), el mismo gesto mueve la lista por dentro. Hoy se lleva a su final con el dedo SOBRE la tarjeta de la ruta (no sobre la lista), de a poco
    var guard = 0
    while (hoyOffset() < hoyMax() - 0.5f && guard++ < 8) {
      composeTestRule.onNodeWithTag("home_card").performTouchInput { swipeUp(startY = height * 0.9f, endY = height * 0.3f, durationMillis = 1500) }
      composeTestRule.waitForIdle()
    }
    assertEquals("Hoy llegó a su final", hoyMax(), hoyOffset(), 0.5f)
    val hoyAtEnd = hoyOffset()
    assertEquals("la lista sigue en su principio mientras Hoy no llegó", 0f, listOffset(), 0f)
    listTouch(startDp = 90f, endDp = 40f, up = true)
    composeTestRule.waitForIdle()
    val moved = listOffset()
    assertTrue("con Hoy en su final, el dedo sube y la lista avanzó ($moved)", moved > 0f)
    assertEquals("…y Hoy no se movió", hoyAtEnd, hoyOffset(), 0.5f)

    // 3. el dedo baja un poco: la lista vuelve primero (y Hoy no retrocede todavía)
    listTouch(startDp = 20f, endDp = 40f, up = false)
    composeTestRule.waitForIdle()
    val back = listOffset()
    assertTrue("el dedo baja: la lista retrocede ($back contra $moved)", back < moved)
    assertEquals("…antes de que Hoy retroceda", hoyAtEnd, hoyOffset(), 0.5f)

    // 4. el dedo baja de sobra: la lista llega a su principio y lo que sobra mueve Hoy (primero la lista, después Hoy)
    listTouch(startDp = 10f, endDp = 140f, up = false)
    composeTestRule.waitForIdle()
    assertEquals("la lista llegó a su principio", 0f, listOffset(), 0f)
    assertTrue("y entonces Hoy retrocedió (${hoyOffset()} contra $hoyAtEnd)", hoyOffset() < hoyAtEnd - 0.5f)
  }

  @Test
  fun el_dedo_fuera_de_la_tarjeta_desliza_Hoy_como_siempre() {
    show(ui(3, stroop, sample))
    val before = hoyOffset()
    composeTestRule.onNodeWithTag("home_card").performTouchInput { swipeUp(startY = height * 0.9f, endY = height * 0.5f, durationMillis = 1500) }
    composeTestRule.waitForIdle()
    assertTrue("deslizar sobre la tarjeta de la ruta mueve Hoy", hoyOffset() > before)
    assertEquals("y no toca la lista", 0f, listOffset(), 0f)
  }

  // ---- Lector de pantalla (§10.4), sin toque (§5.5), textos

  @Test
  fun cada_fila_se_lee_entera_y_fusionada_sin_boton_el_titulo_es_encabezado_y_la_lista_dice_cuantas_son() {
    show(ui(3, stroop, sample))
    assertTrue("el título es un encabezado", node("home_history_title").config.contains(SemanticsProperties.Heading))
    assertEquals(listOf(HISTORY), node("home_history_title").config[SemanticsProperties.Text].map { it.text })
    for (i in 1..4) {
      val n = node("home_history_row_$i")
      assertEquals("la fila $i la lee de un tirón", sample[i - 1].spoken, n.config[SemanticsProperties.ContentDescription].single())
      assertFalse("la fila $i no es un botón", n.config.contains(SemanticsProperties.Role))
      composeTestRule.onNodeWithTag("home_history_row_$i").assertHasNoClickAction()
    }
    assertEquals("Carga exacta, hoy, en tu ruta, 70 % de las cargas, sin pista.", node("home_history_row_1").config[SemanticsProperties.ContentDescription].single())
    assertEquals("Tinta o Palabra, hoy, Contrarreloj, 38 aciertos.", node("home_history_row_3").config[SemanticsProperties.ContentDescription].single())
    val info = node("home_history_list").config[SemanticsProperties.CollectionInfo]
    assertEquals("la lista publica cuántas filas tiene (TalkBack dice «en lista, 10 elementos»)", 10, info.rowCount)
    assertTrue("y su acción de desplazar a una fila (TalkBack trae cada fila a la vista)", node("home_history_list").config.contains(SemanticsActions.ScrollToIndex))
    assertFalse("la tarjeta no es tocable", node("home_history").config.contains(SemanticsActions.OnClick))
  }

  private fun layouts(prefix: String): List<Pair<String, TextLayoutResult>> {
    val out = mutableListOf<Pair<String, TextLayoutResult>>()
    fun walk(n: SemanticsNode) {
      val tag = n.config.getOrNull(SemanticsProperties.TestTag)
      if (tag != null && tag.startsWith(prefix)) { val rs = mutableListOf<TextLayoutResult>(); n.config.getOrNull(SemanticsActions.GetTextLayoutResult)?.action?.invoke(rs); rs.firstOrNull()?.let { out += tag to it } }
      n.children.forEach { walk(it) }
    }
    walk(composeTestRule.onRoot(useUnmergedTree = true).fetchSemanticsNode())
    return out
  }

  /** Ninguna palabra se parte, el texto va a 16 sp o más y nada se corta de lado: con letra 1,0, 1,3 y 2,0 a 393 y 360 dp (la cuarta fila queda cortada a propósito por abajo y no cuenta). */
  private fun assertReadable(widthDp: Int, heightDp: Int, fs: Float) {
    show(ui(3, stroop, sample), widthDp, heightDp, fs)
    val label = "$widthDp dp, letra $fs"
    val all = layouts("home_history_name_") + layouts("home_history_detail_")
    assertTrue("$label: hay textos de filas", all.size >= 6)
    for ((tag, l) in all) {
      val t = l.layoutInput.text.text
      for (i in 0 until l.lineCount - 1) { val e = l.getLineEnd(i); assertTrue("$label: «$tag» parte una palabra: «${t.substring(maxOf(0, e - 8), e)}|${t.substring(e, minOf(t.length, e + 8))}»", e !in 1 until t.length || t[e - 1] == ' ' || t[e] == ' ') }
      assertTrue("$label: «$tag» a ${l.layoutInput.style.fontSize} (mínimo 16 sp)", l.layoutInput.style.fontSize.value >= 16f)
      for (i in 0 until l.lineCount - 1) { val e = l.getLineEnd(i); if (e in 1 until t.length) assertFalse("$label: «$tag» deja un número solo al final de un renglón: «${t.substring(maxOf(0, e - 10), e)}|${t.substring(e, minOf(t.length, e + 10))}»", t[e - 1].isDigit()) }
    }
    // los nombres se leen enteros en la fila (ninguno pasa a más de 2 líneas con letra hasta 2,0 en 393 dp)
    for (i in 1..3) {
      val name = composeTestRule.onNodeWithTag("home_history_name_$i", useUnmergedTree = true).fetchSemanticsNode()
      assertTrue("$label: el nombre de la fila $i cabe en la fila", dp(name.positionInRoot.y + name.size.height) <= bottomDp("home_history_row_$i") + 0.5f)
    }
  }

  /** Tarea 221c: «ayer · en Desafío · 3,2 satélites a la vez» no cabe en un renglón; «a la vez» pasa junto y nunca queda «vez» sola. */
  private fun assertNoLonelyVez(widthDp: Int, heightDp: Int, fs: Float) {
    val satelites = HistoryRow("satelites", "Satélites", "ayer", "en Desafío", "3,2 satélites a la vez", now - 1)
    show(ui(3, stroop, listOf(satelites) + sample.take(3)), widthDp, heightDp, fs)
    val l = layouts("home_history_detail_1").single().second
    val t = l.layoutInput.text.text
    val lines = (0 until l.lineCount).map { t.substring(l.getLineStart(it), l.getLineEnd(it)).replace('\u00A0', ' ').trim() }
    println("MEDIDA «a la vez» a $widthDp dp con letra $fs: " + lines.joinToString(" / "))
    assertTrue("$widthDp dp, letra $fs: ningún renglón es solo «vez»: $lines", lines.none { it == "vez" || it == "la vez" })
    assertTrue("y «a la vez» entero queda en un mismo renglón: $lines", lines.any { it.endsWith("a la vez") })
  }

  @Test fun a_393_con_1_0_nunca_queda_vez_sola_en_la_fila_de_Satelites() = assertNoLonelyVez(393, 873, 1f)
  @Test fun a_360_con_1_0_nunca_queda_vez_sola_en_la_fila_de_Satelites() = assertNoLonelyVez(360, 780, 1f)

  @Test fun los_textos_se_leen_sin_partir_palabras_a_393_con_1_0() = assertReadable(393, 873, 1f)
  @Test fun los_textos_se_leen_sin_partir_palabras_a_393_con_1_3() = assertReadable(393, 873, 1.3f)
  @Test fun los_textos_se_leen_sin_partir_palabras_a_393_con_2_0() = assertReadable(393, 873, 2f)
  @Test fun los_textos_se_leen_sin_partir_palabras_a_360_con_1_0() = assertReadable(360, 780, 1f)
  @Test fun los_textos_se_leen_sin_partir_palabras_a_360_con_2_0() = assertReadable(360, 780, 2f)

  @Test
  fun la_segunda_linea_va_en_tinta_suave_y_se_lee_con_4_5_a_1_sobre_el_crema() {
    fun lum(c: androidx.compose.ui.graphics.Color) = c.luminance().toDouble()
    val a = lum(Clay.InkMuted); val b = lum(Clay.Cream)
    val ratio = (maxOf(a, b) + 0.05) / (minOf(a, b) + 0.05)
    println("CONTRASTE tinta suave sobre crema: %.2f:1".format(ratio))
    assertTrue("la tinta suave sobre el crema mide $ratio:1 (mínimo 4,5)", ratio >= 4.5)
    val ink = (maxOf(lum(Clay.Ink), b) + 0.05) / (minOf(lum(Clay.Ink), b) + 0.05)
    assertTrue("y la tinta, $ink:1", ink >= 4.5)
  }

  @Test
  fun la_tarjeta_va_despues_del_contrarreloj_a_16_dp_de_margen_y_antes_de_la_marca_del_final() {
    show(ui(3, stroop, sample))
    val card = node("home_history")
    assertTrue("va DESPUÉS del Contrarreloj", topDp("home_history") >= bottomDp("btn_timetrial") - 1f)
    assertEquals("a 16 dp del borde izquierdo", 16f, dp(card.positionInRoot.x), 1f)
    assertEquals("y del derecho", 393f - 16f, dp(card.positionInRoot.x + card.size.width), 1.5f)
    assertTrue("la marca del final va después de la tarjeta", node("home_end").positionInRoot.y >= card.positionInRoot.y + card.size.height - 1f)
  }

  @Test
  fun con_el_enlace_del_punto_de_partida_la_tarjeta_va_despues_de_el() {
    show(ui(3, stroop, sample).copy(hasBaseline = false, baselineInvite = true))
    assertTrue("la tarjeta va bajo el enlace", topDp("home_history") >= bottomDp("btn_home_baseline") - 1f)
  }

  private companion object { const val HISTORY = "Tus últimas partidas" }
}
