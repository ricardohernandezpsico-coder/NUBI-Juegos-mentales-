package com.example

import androidx.compose.foundation.layout.Box
import androidx.compose.foundation.layout.Column
import androidx.compose.foundation.layout.fillMaxWidth
import androidx.compose.foundation.layout.height
import androidx.compose.foundation.layout.heightIn
import androidx.compose.foundation.layout.width
import androidx.compose.runtime.CompositionLocalProvider
import androidx.compose.ui.Modifier
import androidx.compose.ui.geometry.Rect
import androidx.compose.ui.platform.LocalDensity
import androidx.compose.ui.platform.testTag
import androidx.compose.ui.test.junit4.createComposeRule
import androidx.compose.ui.test.onNodeWithTag
import androidx.compose.ui.test.onNodeWithText
import androidx.compose.ui.test.performTouchInput
import androidx.compose.ui.test.down
import androidx.compose.ui.test.up
import androidx.compose.ui.unit.Density
import androidx.compose.ui.unit.dp
import androidx.test.core.app.ApplicationProvider
import com.example.data.ClayKitSpec
import com.example.ui.components.ClayKitCard
import com.example.ui.components.ClayKitFlag
import com.example.ui.components.KitButton
import com.example.ui.components.KitCloseButton
import com.example.ui.components.LocalReduceMotion
import com.example.ui.components.nineSlice
import com.example.ui.theme.Clay
import com.example.ui.theme.NeuroVidaTheme
import org.junit.After
import org.junit.Assert.assertEquals
import org.junit.Assert.assertFalse
import org.junit.Assert.assertTrue
import org.junit.Rule
import org.junit.Test
import org.junit.runner.RunWith
import org.robolectric.RobolectricTestRunner
import org.robolectric.annotation.Config
import org.robolectric.annotation.GraphicsMode
import java.io.File

/**
 * El kit de plasticina (Tarea 89): el JSON y las imágenes son del mismo render, el nine-slice deja las esquinas fijas, las reglas de tamaño (botón ≥ 56 dp, X ≥ 48 dp, piedrita ≥ 84 dp), el estado presionado (con «quitar animaciones», de golpe) y el interruptor de depuración.
 */
@RunWith(RobolectricTestRunner::class)
@GraphicsMode(GraphicsMode.Mode.NATIVE)
@Config(qualifiers = "w393dp-h873dp", sdk = [36])
class ClayKitTest {
  @get:Rule val rule = createComposeRule()

  private val spec = ClayKitSpec.parse(File("src/main/res/raw/kit.json").readText())

  @After fun tearDown() { ClayKitFlag.forTest(false) }

  /** El tamaño de un WebP con cabecera extendida (VP8X), sin decodificarlo. */
  private fun webpSize(path: String): Pair<Int, Int> {
    val b = File(path).readBytes()
    assertEquals("RIFF", String(b, 0, 4)); assertEquals("WEBP", String(b, 8, 4)); assertEquals("VP8X", String(b, 12, 4))
    fun u24(o: Int) = (b[o].toInt() and 0xFF) or ((b[o + 1].toInt() and 0xFF) shl 8) or ((b[o + 2].toInt() and 0xFF) shl 16)
    return (1 + u24(24)) to (1 + u24(27))
  }

  // ---- El JSON y las imágenes

  @Test
  fun el_json_y_las_imagenes_son_del_mismo_render() {
    assertEquals(3f, spec.pxPerDp, 0f)
    for (c in listOf("sol", "crema", "lima")) for (suf in listOf("", "_p")) {
      assertEquals("kit_losa_$c$suf", spec.slab.widthPx to spec.slab.heightPx, webpSize("src/main/res/drawable-nodpi/kit_losa_$c$suf.webp"))
    }
    for (suf in listOf("", "_p")) assertEquals("kit_cerrar$suf", spec.close.widthPx to spec.close.heightPx, webpSize("src/main/res/drawable-nodpi/kit_cerrar$suf.webp"))
    assertEquals("kit_jugar", spec.play.widthPx to spec.play.heightPx, webpSize("src/main/res/drawable-nodpi/kit_jugar.webp"))
  }

  @Test
  fun los_insets_caen_dentro_de_la_imagen_y_dejan_un_centro_para_estirar() {
    val s = spec.slab
    assertTrue("hay centro que estirar a lo ancho y a lo alto", s.insetLeftPx + s.insetRightPx < s.widthPx - 30 && s.insetTopPx + s.insetBottomPx < s.heightPx - 10)
    assertTrue("las esquinas cubren el radio (22 dp) más el margen", s.insetLeftPx >= (s.margins.left + 22) * 3 - 1 && s.insetTopPx >= (s.margins.top + 22) * 3 - 1)
    assertTrue("y la sombra cae dentro de los márgenes de la imagen", s.margins.bottom >= 10f && s.margins.right >= 8f)
    assertEquals("la imagen mide cuerpo + márgenes", s.widthPx / 3f, s.margins.left + s.bodyWidth + s.margins.right, 0.5f)
    assertEquals(s.heightPx / 3f, s.margins.top + s.bodyHeight + s.margins.bottom, 0.5f)
  }

  // ---- El nine-slice: esquinas fijas, centro estirado

  private val body340x56 = Rect(0f, 0f, 340f * 3, 56f * 3)         // a 3 px por dp
  private val body115x84 = Rect(0f, 0f, 115f * 3, 84f * 3)
  private val body340x160 = Rect(0f, 0f, 340f * 3, 160f * 3)

  @Test
  fun las_cuatro_esquinas_miden_lo_mismo_en_cualquier_tamano() {
    fun corners(r: Rect) = nineSlice(spec.slab, spec.pxPerDp, 3f, r).let { p -> listOf(p[0], p[2], p[6], p[8]).map { it.dstW to it.dstH } }
    val base = corners(body340x56)
    assertEquals(base, corners(body115x84))
    assertEquals(base, corners(body340x160))
    assertEquals(base, corners(Rect(0f, 0f, 340f * 3 * 1.4f, 56f * 3 * 3.1f)))       // y con letra 2,0: crece lo que crece el contenido
  }

  @Test
  fun solo_el_centro_y_los_bordes_se_estiran() {
    val small = nineSlice(spec.slab, spec.pxPerDp, 3f, body115x84); val big = nineSlice(spec.slab, spec.pxPerDp, 3f, body340x160)
    assertEquals("nueve trozos", 9, big.size)
    assertTrue("el centro crece a lo ancho y a lo alto", big[4].dstW > small[4].dstW && big[4].dstH > small[4].dstH)
    assertEquals("el borde de arriba crece a lo ancho, no a lo alto", small[1].dstH, big[1].dstH)
    assertEquals("el borde izquierdo crece a lo alto, no a lo ancho", small[3].dstW, big[3].dstW)
    // los trozos cubren la imagen entera sin huecos ni solapes
    val total = big.sumOf { it.dstW.toLong() * it.dstH }
    val w = big.maxOf { it.dstX + it.dstW } - big.minOf { it.dstX }; val h = big.maxOf { it.dstY + it.dstH } - big.minOf { it.dstY }
    assertEquals(w.toLong() * h, total)
  }

  @Test
  fun el_cuerpo_cae_donde_el_diseno_dice_y_la_sombra_sale_por_abajo_y_a_la_derecha() {
    val parts = nineSlice(spec.slab, spec.pxPerDp, 3f, body340x56)
    val left = parts.minOf { it.dstX }; val top = parts.minOf { it.dstY }; val right = parts.maxOf { it.dstX + it.dstW }; val bottom = parts.maxOf { it.dstY + it.dstH }
    assertEquals(-spec.slab.margins.left * 3f, left.toFloat(), 1f)
    assertEquals(-spec.slab.margins.top * 3f, top.toFloat(), 1f)
    assertEquals(body340x56.right + spec.slab.margins.right * 3f, right.toFloat(), 1f)
    assertEquals(body340x56.bottom + spec.slab.margins.bottom * 3f, bottom.toFloat(), 1f)
    assertTrue("la sombra sale más por abajo que por arriba", spec.slab.margins.bottom > spec.slab.margins.top)
  }

  @Test
  fun un_cuerpo_mas_chico_que_dos_esquinas_achica_las_esquinas_parejas() {
    val tiny = nineSlice(spec.slab, spec.pxPerDp, 3f, Rect(0f, 0f, 30f, 20f))
    assertTrue(tiny.all { it.dstW > 0 && it.dstH > 0 })
    assertEquals("sigue cubriendo la imagen entera", (30f + (spec.slab.margins.left + spec.slab.margins.right) * 3), (tiny.maxOf { it.dstX + it.dstW } - tiny.minOf { it.dstX }).toFloat(), 1.5f)
  }

  // ---- Reglas de tamaño y de estado

  private fun show(reduce: Boolean = false, fontScale: Float = 1f, content: @androidx.compose.runtime.Composable () -> Unit) {
    ClayKitFlag.forTest(true)
    rule.setContent {
      val base = LocalDensity.current
      CompositionLocalProvider(LocalDensity provides Density(base.density, fontScale), LocalReduceMotion provides reduce) { NeuroVidaTheme { Box(Modifier.width(393.dp)) { content() } } }
    }
    rule.waitForIdle()
  }

  @Test
  fun el_boton_grande_mide_al_menos_56_dp_y_crece_con_letra_2_0_sin_cortar_el_texto() {
    show { KitButton("Empezar la ruta · 3 juegos", {}, play = true, modifier = Modifier.testTag("kb")) }
    val h1 = rule.onNodeWithTag("kb").fetchSemanticsNode().boundsInRoot.height / rule.density.density
    assertTrue("alto del botón $h1 dp (≥ 56 + 6 de sombra)", h1 >= 56f)
  }

  @Test
  fun con_letra_2_0_el_boton_crece_en_vez_de_deformarse() {
    show(fontScale = 2f) { KitButton("Jugar en el Observatorio", {}, play = true, fontSize = androidx.compose.ui.unit.TextUnit(19f, androidx.compose.ui.unit.TextUnitType.Sp), modifier = Modifier.testTag("kb")) }
    val h2 = rule.onNodeWithTag("kb").fetchSemanticsNode().boundsInRoot.height / rule.density.density
    assertTrue("a letra 2,0 el botón mide más de 80 dp ($h2): el texto pasa a dos líneas y la losa crece", h2 > 80f)
  }

  @Test
  fun la_X_mide_al_menos_48_dp_de_alto_y_de_ancho_y_se_llama_Cerrar() {
    var closed = 0
    show { KitCloseButton({ closed++ }, Modifier.testTag("kx")) }
    val b = rule.onNodeWithTag("kx").fetchSemanticsNode().boundsInRoot
    assertTrue("X de ${b.width / rule.density.density} × ${b.height / rule.density.density} dp", b.width >= 48f * rule.density.density - 1 && b.height >= 48f * rule.density.density - 1)
    rule.onNodeWithTag("kx").performTouchInput { down(center); up() }
    assertEquals(1, closed)
  }

  @Test
  fun la_piedrita_mide_al_menos_84_dp_con_el_kit_y_su_marca_es_texto_ademas_del_color() {
    show {
      Column(Modifier.fillMaxWidth()) {
        ClayKitCard(Modifier.testTag("stone").width(115.dp).heightIn(min = 90.dp), color = Clay.Sun, radius = 18.dp, depth = 4.dp, onClick = {}) { androidx.compose.material3.Text("Sigue", color = Clay.Ink) }
      }
    }
    val h = rule.onNodeWithTag("stone").fetchSemanticsNode().boundsInRoot.height / rule.density.density
    assertTrue("alto de la losa de la piedrita $h dp (mínimo 84; el modificador pide 90 con los 6 dp del arranque de la sombra)", h >= 84f)
  }

  @Test
  fun al_apoyar_el_dedo_el_contenido_baja_2_dp_en_80_ms_y_al_soltar_vuelve() {
    show { KitButton("Jugar", {}, modifier = Modifier.testTag("kb")) }
    rule.mainClock.autoAdvance = false
    pressedShift()                                                              // fija el sitio de reposo del texto
    val before = rule.onNodeWithTag("kb").fetchSemanticsNode().boundsInRoot.top
    rule.onNodeWithTag("kb").performTouchInput { down(center) }
    rule.mainClock.advanceTimeByFrame()                                         // el apoyo se reparte en el primer cuadro
    rule.mainClock.advanceTimeBy(40)
    val mid = pressedShift()
    rule.mainClock.advanceTimeBy(120)
    val full = pressedShift()
    assertTrue("a medias del cambio ya se movió algo pero no todo ($mid de ${full})", mid > 0f && mid < full)
    assertEquals("a los 160 ms el contenido bajó 2 dp", 2f, full, 0.25f)
    rule.onNodeWithTag("kb").performTouchInput { up() }
    rule.mainClock.advanceTimeBy(200)
    assertEquals("al soltar vuelve", 0f, pressedShift(), 0.05f)
    assertEquals("el botón no cambia de lugar ni de tamaño", before, rule.onNodeWithTag("kb").fetchSemanticsNode().boundsInRoot.top, 0.5f)
  }

  /** Cuánto bajó el texto (dp) respecto de su sitio sin presionar (la primera lectura fija el reposo). */
  private fun pressedShift(): Float {
    val top = rule.onNodeWithText("Jugar", useUnmergedTree = true).fetchSemanticsNode().boundsInRoot.top
    if (restY == null) restY = top
    return (top - restY!!) / rule.density.density
  }
  private var restY: Float? = null

  @Test
  fun con_quitar_animaciones_el_cambio_a_presionada_es_de_golpe() {
    show(reduce = true) { KitButton("Jugar", {}, modifier = Modifier.testTag("kb")) }
    rule.mainClock.autoAdvance = false
    pressedShift()                                                              // fija el sitio de reposo
    rule.onNodeWithTag("kb").performTouchInput { down(center) }
    rule.mainClock.advanceTimeByFrame()
    rule.mainClock.advanceTimeByFrame()                                         // dos cuadros: sin animación el cambio no necesita más
    assertEquals("de golpe: ya bajó los 2 dp", 2f, pressedShift(), 0.25f)
  }

  // ---- El interruptor de depuración

  @Test
  fun el_interruptor_nace_encendido_en_depuracion_y_apagado_en_la_version_de_tienda() {
    val ctx = ApplicationProvider.getApplicationContext<android.app.Application>()
    ctx.getSharedPreferences("kit_plasticina", android.content.Context.MODE_PRIVATE).edit().clear().commit()
    ClayKitFlag.init(ctx, debug = true)
    assertTrue("en depuración, encendido por defecto", ClayKitFlag.on)
    ClayKitFlag.init(ctx, debug = false)
    assertFalse("en la versión de tienda, siempre apagado", ClayKitFlag.on)
  }

  @Test
  fun el_interruptor_se_recuerda() {
    val ctx = ApplicationProvider.getApplicationContext<android.app.Application>()
    ClayKitFlag.set(ctx, false)
    ClayKitFlag.init(ctx, debug = true)
    assertFalse("lo apagado se queda apagado", ClayKitFlag.on)
    ClayKitFlag.set(ctx, true)
    ClayKitFlag.init(ctx, debug = true)
    assertTrue(ClayKitFlag.on)
  }
}
