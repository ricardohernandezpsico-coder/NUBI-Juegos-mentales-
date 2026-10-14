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
import androidx.compose.ui.test.assertHasNoClickAction
import androidx.compose.ui.test.assertIsFocused
import androidx.compose.ui.test.assertIsOff
import androidx.compose.ui.test.assertIsOn
import androidx.compose.ui.test.performClick
import androidx.compose.ui.test.performTextInput
import androidx.compose.ui.test.assertIsSelected
import androidx.compose.ui.test.onNodeWithTag
import androidx.compose.ui.test.onNodeWithText
import androidx.compose.ui.test.performTouchInput
import androidx.compose.ui.test.down
import androidx.compose.ui.test.up
import androidx.compose.ui.unit.Density
import androidx.compose.ui.unit.dp
import com.example.data.ClayKitSpec
import com.example.ui.components.Glyph
import com.example.data.AvatarIds
import com.example.ui.components.ClayChoice
import com.example.ui.components.ClaySwitch
import com.example.ui.components.ClayTextField
import com.example.ui.components.avatarRes
import com.example.ui.components.ClayDisc
import com.example.ui.components.DiscStyle
import com.example.ui.components.KitButton
import com.example.ui.components.KitGlyph
import com.example.ui.components.KitCloseButton
import com.example.ui.components.SlabColor
import com.example.ui.components.SlabSize
import com.example.ui.components.slabImages
import com.example.ui.components.slabImagesOrNull
import com.example.ui.theme.ClayCard
import com.example.ui.components.LocalReduceMotion
import com.example.ui.components.nineSlice
import com.example.ui.theme.Clay
import com.example.ui.theme.NeuroVidaTheme
import org.junit.Assert.assertEquals
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
  private val slab = spec.slab("md")

  /** El tamaño de un WebP con cabecera extendida (VP8X), sin decodificarlo. */
  private fun webpSize(path: String): Pair<Int, Int> {
    val b = File(path).readBytes()
    assertEquals("RIFF", String(b, 0, 4)); assertEquals("WEBP", String(b, 8, 4)); assertEquals("VP8X", String(b, 12, 4))
    fun u24(o: Int) = (b[o].toInt() and 0xFF) or ((b[o + 1].toInt() and 0xFF) shl 8) or ((b[o + 2].toInt() and 0xFF) shl 16)
    return (1 + u24(24)) to (1 + u24(27))
  }

  // ---- El JSON y las imágenes

  private val colorKeys = mapOf(SlabColor.SUN to "sol", SlabColor.CREAM to "crema", SlabColor.LIME to "lima", SlabColor.SKY to "cielo", SlabColor.CORAL to "coral", SlabColor.LILAC to "lila")
  private fun res(name: String) = "src/main/res/drawable-nodpi/$name.webp"

  @Test
  fun el_json_y_las_imagenes_son_del_mismo_render() {
    assertEquals(3f, spec.pxPerDp, 0f)
    assertEquals("los tres radios", setOf("ch", "md", "gr"), spec.slabs.keys)
    for ((clase, slab) in spec.slabs) for (c in slab.colors) for (suf in listOf("", "_p")) {
      assertEquals("kit_losa_${clase}_$c$suf", slab.widthPx to slab.heightPx, webpSize(res("kit_losa_${clase}_$c$suf")))
    }
    for ((name, disc) in spec.discs) for (suf in listOf("", "_p")) assertEquals("kit_$name$suf", disc.widthPx to disc.heightPx, webpSize(res("kit_$name$suf")))
    for ((name, piece) in spec.pieces) {
      val file = if (name == "jugar") "kit_jugar" else "kit_i_$name"
      assertEquals(file, piece.widthPx to piece.heightPx, webpSize(res(file)))
    }
    assertEquals(setOf("cerrar", "disco_crema", "disco_cielo"), spec.discs.keys)
    assertEquals("cada ícono del kit tiene su pieza en el JSON (los de la Tarea 91 reemplazan a los de Material de los paneles)", Glyph.values().map { it.key }.toSet(), spec.pieces.keys)
    assertEquals(setOf("jugar", "volver", "visto", "izq", "der", "compartir", "candado", "engranaje", "globo", "altavoz", "vibracion", "reloj", "sin_reloj", "escudo", "info", "campana", "persona", "arriba", "abajo"), spec.pieces.keys)
  }

  @Test
  fun los_colores_del_codigo_son_los_que_se_renderizaron() {
    for (size in SlabSize.values()) for (color in SlabColor.values()) {
      val inJson = colorKeys.getValue(color) in spec.slab(size.key).colors
      assertEquals("losa ${size.key}/${colorKeys[color]}: el código y el JSON dicen lo mismo", inJson, slabImagesOrNull(size, color) != null)
    }
    // Las combinaciones que usa la app en las tres pantallas de selección: chips y casillas (crema, sol, lima), botones (los seis colores) y láminas (crema).
    for (c in listOf(SlabColor.CREAM, SlabColor.SUN, SlabColor.LIME)) assertTrue(slabImagesOrNull(SlabSize.SMALL, c) != null)
    for (c in SlabColor.values()) assertTrue(slabImagesOrNull(SlabSize.MEDIUM, c) != null)
    assertTrue(slabImagesOrNull(SlabSize.LARGE, SlabColor.CREAM) != null)
    assertEquals("un color que no existe en ese radio cae al mediano, sin fallar", SlabSize.MEDIUM, slabImages(SlabSize.SMALL, SlabColor.CORAL).size)
  }

  @Test
  fun los_radios_son_14_22_y_30_y_los_insets_caen_dentro_de_la_imagen_dejando_un_centro_para_estirar() {
    assertEquals(listOf(14f, 22f, 30f), listOf("ch", "md", "gr").map { spec.slab(it).radius })
    for ((clase, s) in spec.slabs) {
      assertTrue("$clase: hay centro que estirar a lo ancho y a lo alto", s.insetLeftPx + s.insetRightPx < s.widthPx - 30 && s.insetTopPx + s.insetBottomPx < s.heightPx - 10)
      assertTrue("$clase: las esquinas cubren el radio (${s.radius} dp) más el margen", s.insetLeftPx >= (s.margins.left + s.radius) * 3 - 1 && s.insetTopPx >= (s.margins.top + s.radius) * 3 - 1)
      assertTrue("$clase: la sombra cae dentro de los márgenes de la imagen", s.margins.bottom >= 10f && s.margins.right >= 8f)
      assertEquals("$clase: la imagen mide cuerpo + márgenes", s.widthPx / 3f, s.margins.left + s.bodyWidth + s.margins.right, 0.5f)
      assertEquals(s.heightPx / 3f, s.margins.top + s.bodyHeight + s.margins.bottom, 0.5f)
      assertEquals("$clase: el texto baja 2 dp al presionar", 2f, s.pressTextDy, 0f)
    }
    for ((name, d) in spec.discs) assertEquals("disco $name: 48 dp", 48f, d.bodyWidth, 0.5f)
  }

  // ---- El nine-slice: esquinas fijas, centro estirado

  private val body340x56 = Rect(0f, 0f, 340f * 3, 56f * 3)         // a 3 px por dp
  private val body115x84 = Rect(0f, 0f, 115f * 3, 84f * 3)
  private val body340x160 = Rect(0f, 0f, 340f * 3, 160f * 3)

  @Test
  fun las_cuatro_esquinas_miden_lo_mismo_en_cualquier_tamano() {
    fun corners(r: Rect) = nineSlice(slab, spec.pxPerDp, 3f, r).let { p -> listOf(p[0], p[2], p[6], p[8]).map { it.dstW to it.dstH } }
    val base = corners(body340x56)
    assertEquals(base, corners(body115x84))
    assertEquals(base, corners(body340x160))
    assertEquals(base, corners(Rect(0f, 0f, 340f * 3 * 1.4f, 56f * 3 * 3.1f)))       // y con letra 2,0: crece lo que crece el contenido
  }

  @Test
  fun solo_el_centro_y_los_bordes_se_estiran() {
    val small = nineSlice(slab, spec.pxPerDp, 3f, body115x84); val big = nineSlice(slab, spec.pxPerDp, 3f, body340x160)
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
    val parts = nineSlice(slab, spec.pxPerDp, 3f, body340x56)
    val left = parts.minOf { it.dstX }; val top = parts.minOf { it.dstY }; val right = parts.maxOf { it.dstX + it.dstW }; val bottom = parts.maxOf { it.dstY + it.dstH }
    assertEquals(-slab.margins.left * 3f, left.toFloat(), 1f)
    assertEquals(-slab.margins.top * 3f, top.toFloat(), 1f)
    assertEquals(body340x56.right + slab.margins.right * 3f, right.toFloat(), 1f)
    assertEquals(body340x56.bottom + slab.margins.bottom * 3f, bottom.toFloat(), 1f)
    assertTrue("la sombra sale más por abajo que por arriba", slab.margins.bottom > slab.margins.top)
  }

  @Test
  fun un_cuerpo_mas_chico_que_dos_esquinas_achica_las_esquinas_parejas() {
    val tiny = nineSlice(slab, spec.pxPerDp, 3f, Rect(0f, 0f, 30f, 20f))
    assertTrue(tiny.all { it.dstW > 0 && it.dstH > 0 })
    assertEquals("sigue cubriendo la imagen entera", (30f + (slab.margins.left + slab.margins.right) * 3), (tiny.maxOf { it.dstX + it.dstW } - tiny.minOf { it.dstX }).toFloat(), 1.5f)
  }

  // ---- Reglas de tamaño y de estado

  private fun show(reduce: Boolean = false, fontScale: Float = 1f, content: @androidx.compose.runtime.Composable () -> Unit) {
    rule.setContent {
      val base = LocalDensity.current
      CompositionLocalProvider(LocalDensity provides Density(base.density, fontScale), LocalReduceMotion provides reduce) { NeuroVidaTheme { Box(Modifier.width(393.dp)) { content() } } }
    }
    rule.waitForIdle()
  }

  @Test
  fun el_boton_grande_mide_al_menos_56_dp_y_crece_con_letra_2_0_sin_cortar_el_texto() {
    show { KitButton("Empezar la ruta · 3 juegos", {}, glyph = Glyph.PLAY, modifier = Modifier.testTag("kb")) }
    val h1 = rule.onNodeWithTag("kb").fetchSemanticsNode().boundsInRoot.height / rule.density.density
    assertTrue("alto del botón $h1 dp (≥ 56 + 6 de sombra)", h1 >= 56f)
  }

  @Test
  fun con_letra_2_0_el_boton_crece_en_vez_de_deformarse() {
    show(fontScale = 2f) { KitButton("Jugar en el Observatorio", {}, glyph = Glyph.PLAY, fontSize = androidx.compose.ui.unit.TextUnit(19f, androidx.compose.ui.unit.TextUnitType.Sp), modifier = Modifier.testTag("kb")) }
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
        ClayCard(Modifier.testTag("stone").width(115.dp).heightIn(min = 90.dp), color = Clay.Sun, onClick = {}, contentPadding = 0.dp) { androidx.compose.material3.Text("Sigue", color = Clay.Ink) }
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

  // ---- Elegido no es presionado

  /** El texto de una casilla [tag] baja respecto de su reposo (dp): la primera lectura fija el reposo. */
  private fun contentTop(text: String, rest: MutableMap<String, Float>): Float {
    val top = rule.onNodeWithText(text, useUnmergedTree = true).fetchSemanticsNode().boundsInRoot.top
    val r = rest.getOrPut(text) { top }
    return (top - r) / rule.density.density
  }

  @Test
  fun una_casilla_elegida_se_queda_quieta_y_solo_baja_mientras_el_dedo_esta_encima() {
    val rest = mutableMapOf<String, Float>()
    show(reduce = true) {
      androidx.compose.foundation.layout.Column {
        ClayChoice(selected = true, onClick = {}, modifier = Modifier.testTag("elegida")) { androidx.compose.material3.Text("Elegida") }
        ClayChoice(selected = false, onClick = {}, modifier = Modifier.testTag("libre")) { androidx.compose.material3.Text("Libre") }
      }
    }
    rule.mainClock.autoAdvance = false
    contentTop("Elegida", rest); contentTop("Libre", rest)                              // fijan el reposo
    rule.mainClock.advanceTimeByFrame(); rule.mainClock.advanceTimeByFrame()
    assertEquals("la elegida, sin tocarla, no baja: elegido no es presionado", 0f, contentTop("Elegida", rest), 0.05f)
    assertEquals("la libre tampoco", 0f, contentTop("Libre", rest), 0.05f)
    rule.onNodeWithTag("libre").performTouchInput { down(center) }
    rule.mainClock.advanceTimeByFrame(); rule.mainClock.advanceTimeByFrame()
    assertEquals("mientras el dedo está encima, baja 2 dp", 2f, contentTop("Libre", rest), 0.25f)
    assertEquals("la elegida sigue quieta", 0f, contentTop("Elegida", rest), 0.05f)
  }

  @Test
  fun la_casilla_reserva_el_lugar_del_visto_y_no_cambia_de_tamano_al_elegirse() {
    val picked = androidx.compose.runtime.mutableStateOf(false)
    show { ClayChoice(selected = picked.value, onClick = { picked.value = true }, modifier = Modifier.testTag("c")) { androidx.compose.material3.Text("Español") } }
    val before = rule.onNodeWithTag("c").fetchSemanticsNode().boundsInRoot
    rule.onNodeWithTag("c").performTouchInput { down(center); up() }
    rule.waitForIdle()
    val after = rule.onNodeWithTag("c").fetchSemanticsNode().boundsInRoot
    assertEquals(before.width, after.width, 0.5f); assertEquals(before.height, after.height, 0.5f)
    assertTrue("mide 48 dp o más", after.height >= 48f * rule.density.density - 1)
    rule.onNodeWithTag("c").assertIsSelected()
  }

  // ---- Los discos

  @Test
  fun el_disco_mide_48_dp_se_toca_entero_y_si_es_solo_un_adorno_no_es_un_boton() {
    var taps = 0
    show {
      androidx.compose.foundation.layout.Column {
        ClayDisc(DiscStyle.CREAM, { taps++ }, "Opciones", Modifier.testTag("disco")) { KitGlyph(Glyph.GEAR, size = 28.dp) }
        ClayDisc(DiscStyle.SKY, null, "", Modifier.testTag("adorno")) { androidx.compose.material3.Text("A") }
      }
    }
    val b = rule.onNodeWithTag("disco").fetchSemanticsNode().boundsInRoot
    assertTrue("disco de ${b.width / rule.density.density} × ${b.height / rule.density.density} dp", b.width >= 48f * rule.density.density - 1 && b.height >= 48f * rule.density.density - 1)
    rule.onNodeWithTag("disco").performTouchInput { down(center); up() }
    assertEquals(1, taps)
    rule.onNodeWithTag("adorno").assertHasNoClickAction()
  }

  @Test
  fun los_iconos_de_churro_tinta_se_centran_en_su_caja() {
    show { KitGlyph(Glyph.CHECK, Modifier.testTag("g"), size = 24.dp) }
    val b = rule.onNodeWithTag("g").fetchSemanticsNode().boundsInRoot
    assertEquals(24f * rule.density.density, b.width, 1f); assertEquals(24f * rule.density.density, b.height, 1f)
  }

  // ---- Los avatares (Tarea 91)

  @Test
  fun los_siete_avatares_renderizados_estan_en_el_json_con_su_imagen_de_48_dp_y_Nubi_sale_de_la_isla() {
    val json = org.json.JSONObject(File("src/main/res/raw/kit.json").readText()).getJSONObject("avatares")
    val renderizados = AvatarIds.ALL.filter { it != AvatarIds.NUBI }
    assertEquals(renderizados.toSet(), json.keys().asSequence().toSet())
    for (id in renderizados) {
      val size = json.getJSONObject(id).getJSONArray("imagen_px")
      assertEquals("kit_av_$id mide 48 dp a 3 px por dp", 144, size.getInt(0)); assertEquals(144, size.getInt(1))
      assertEquals("kit_av_$id", size.getInt(0) to size.getInt(1), webpSize(res("kit_av_$id")))
    }
    val dibujos = AvatarIds.ALL.map { avatarRes(it) }
    assertEquals("cada avatar tiene su dibujo, todos distintos", 8, dibujos.toSet().size)
    assertEquals("un id desconocido cae al de Nubi", avatarRes(AvatarIds.NUBI), avatarRes("xyz"))
  }

  // ---- El interruptor de plasticina (Tarea 91)

  @Test
  fun el_interruptor_es_un_Switch_con_su_estado_mide_56_por_48_dp_y_se_cambia_con_un_toque() {
    val on = androidx.compose.runtime.mutableStateOf(false)
    show { ClaySwitch(checked = on.value, onCheckedChange = { on.value = it }, modifier = Modifier.testTag("sw"), label = "Efectos de sonido") }
    val b = rule.onNodeWithTag("sw").fetchSemanticsNode().boundsInRoot
    assertTrue("interruptor de ${b.width / rule.density.density} × ${b.height / rule.density.density} dp (mínimo 48 de alto)", b.height >= 48f * rule.density.density - 1 && b.width >= 48f * rule.density.density - 1)
    val node = rule.onNodeWithTag("sw")
    node.assertIsOff()
    assertEquals(androidx.compose.ui.semantics.Role.Switch, node.fetchSemanticsNode().config[androidx.compose.ui.semantics.SemanticsProperties.Role])
    assertEquals("Efectos de sonido", node.fetchSemanticsNode().config[androidx.compose.ui.semantics.SemanticsProperties.ContentDescription].single())
    assertEquals("Apagado", node.fetchSemanticsNode().config[androidx.compose.ui.semantics.SemanticsProperties.StateDescription])
    node.performTouchInput { down(center); up() }
    rule.waitForIdle()
    node.assertIsOn()
    assertEquals("Encendido", node.fetchSemanticsNode().config[androidx.compose.ui.semantics.SemanticsProperties.StateDescription])
    assertTrue(on.value)
    node.performTouchInput { down(center); up() }
    rule.waitForIdle()
    node.assertIsOff()
  }

  @Test
  fun el_interruptor_no_cambia_de_tamano_al_encenderse_y_con_letra_2_0_sigue_mediendo_lo_mismo() {
    val on = androidx.compose.runtime.mutableStateOf(false)
    show(fontScale = 2f) { ClaySwitch(checked = on.value, onCheckedChange = { on.value = it }, modifier = Modifier.testTag("sw")) }
    val a = rule.onNodeWithTag("sw").fetchSemanticsNode().boundsInRoot
    on.value = true; rule.waitForIdle()
    val b = rule.onNodeWithTag("sw").fetchSemanticsNode().boundsInRoot
    assertEquals(a.width, b.width, 0.5f); assertEquals(a.height, b.height, 0.5f)
  }

  // ---- El campo de texto de plasticina (Tarea 91)

  @Test
  fun el_campo_de_texto_recibe_el_foco_el_texto_y_mide_56_dp_o_mas() {
    val text = androidx.compose.runtime.mutableStateOf("")
    show { ClayTextField(value = text.value, onValueChange = { text.value = it }, modifier = Modifier.testTag("campo"), placeholder = "Tu nombre") }
    rule.onNodeWithText("Tu nombre").assertExists()
    val b = rule.onNodeWithTag("campo").fetchSemanticsNode().boundsInRoot
    assertTrue("campo de ${b.height / rule.density.density} dp de alto (56 o más: un toque en cualquier parte de la losa le da el foco)", b.height >= 56f * rule.density.density - 1)
    rule.onNodeWithTag("campo").performClick()
    rule.onNodeWithTag("campo").assertIsFocused()
    rule.onNodeWithTag("campo").performTextInput("Ana")
    rule.waitForIdle()
    assertEquals("Ana", text.value)
    rule.onNodeWithText("Tu nombre").assertDoesNotExist()          // el placeholder se va al escribir
  }

  @Test
  fun con_letra_2_0_el_campo_de_texto_crece_y_no_corta_lo_que_se_escribe() {
    val text = androidx.compose.runtime.mutableStateOf("Alejandra")
    show(fontScale = 2f) {
      androidx.compose.foundation.layout.Column(Modifier.width(360.dp)) {
        ClayTextField(value = text.value, onValueChange = { text.value = it }, modifier = Modifier.testTag("campo"), fontSize = androidx.compose.ui.unit.TextUnit(18f, androidx.compose.ui.unit.TextUnitType.Sp))
      }
    }
    val b = rule.onNodeWithTag("campo").fetchSemanticsNode().boundsInRoot
    assertTrue("a letra 2,0 el texto de 18 sp mide más de 30 dp de alto (${b.height / rule.density.density})", b.height / rule.density.density > 30f)
    assertTrue("el campo ocupa casi todo el ancho de la losa (${b.width / rule.density.density} dp)", b.width / rule.density.density > 250f)
    rule.onNodeWithText("Alejandra").assertExists()
  }
}
