package com.example

import android.app.Application
import android.content.Intent
import androidx.compose.foundation.layout.Box
import androidx.compose.foundation.layout.fillMaxSize
import androidx.compose.foundation.layout.height
import androidx.compose.foundation.layout.width
import androidx.compose.runtime.CompositionLocalProvider
import androidx.compose.ui.Modifier
import androidx.compose.ui.platform.LocalDensity
import androidx.compose.ui.semantics.SemanticsActions
import androidx.compose.ui.semantics.SemanticsProperties
import androidx.compose.ui.semantics.getOrNull
import androidx.compose.ui.test.assertIsDisplayed
import androidx.compose.ui.test.assertIsNotEnabled
import androidx.compose.ui.test.assertTextContains
import androidx.compose.ui.test.hasTestTag
import androidx.compose.ui.test.hasText
import androidx.compose.ui.test.junit4.createComposeRule
import androidx.compose.ui.test.onNodeWithTag
import androidx.compose.ui.test.onNodeWithText
import androidx.compose.ui.test.onRoot
import androidx.compose.ui.test.performClick
import androidx.compose.ui.test.performScrollTo
import androidx.compose.ui.unit.Density
import androidx.compose.ui.unit.dp
import androidx.test.core.app.ApplicationProvider
import com.example.data.Fuentes
import com.example.data.FuentesMas
import com.example.data.HealthNotice
import com.example.data.NubiContact
import com.example.model.DomainType
import com.example.model.GameRegistry
import com.example.ui.components.BoardBackground
import com.example.ui.screens.ContactoSection
import com.example.ui.screens.FuentesScreen
import com.example.ui.theme.NeuroVidaTheme
import org.junit.Assert.assertEquals
import org.junit.Assert.assertFalse
import org.junit.Assert.assertNotNull
import org.junit.Assert.assertNull
import org.junit.Assert.assertTrue
import org.junit.Rule
import org.junit.Test
import org.junit.runner.RunWith
import org.robolectric.RobolectricTestRunner
import org.robolectric.Shadows.shadowOf
import org.robolectric.annotation.Config
import org.robolectric.annotation.GraphicsMode

/**
 * «En qué se basa Nubi» y «Contacto» de Tú (Tarea 185): la pantalla de las fuentes (una losa por juego, por área; tocar abre el DOI; sin navegador lo dice; el encuadre y «Lo que no decimos» con el aviso de salud al final) y la sección de contacto
 * («Escribir a Nubi» sin correo dice «Correo por definir» y no hace nada; con correo abre la app de correo y el correo va en texto plano; el informe de errores y la versión).
 */
@RunWith(RobolectricTestRunner::class)
@GraphicsMode(GraphicsMode.Mode.NATIVE)
@Config(qualifiers = "w393dp-h873dp-440dpi", sdk = [36])
class FuentesYContactoTest {
  @get:Rule val rule = createComposeRule()

  private val opened = mutableListOf<String>()

  private fun showFuentes(fontScale: Float = 1f, browser: Boolean = true) {
    rule.setContent {
      val base = LocalDensity.current
      CompositionLocalProvider(LocalDensity provides Density(base.density, fontScale), com.example.ui.components.LocalReduceMotion provides true) {
        NeuroVidaTheme {
          Box(Modifier.width(393.dp).height(873.dp)) {
            BoardBackground()
            FuentesScreen(openLink = { opened += it; browser })
          }
        }
      }
    }
    rule.waitForIdle()
  }

  private fun showContacto(email: String?, onReport: () -> Unit = {}) {
    rule.setContent {
      NeuroVidaTheme { Box(Modifier.width(393.dp).fillMaxSize()) { BoardBackground(); ContactoSection(onSendReport = onReport, email = email) } }
    }
    rule.waitForIdle()
  }

  // ------------------------------------------------------------------ En qué se basa Nubi

  @Test
  fun las_cuatro_areas_y_los_17_juegos_estan_con_su_fuente_y_se_alcanzan_deslizando() {
    showFuentes()
    rule.onNodeWithTag("fuentes_intro").assertIsDisplayed()
    rule.onNodeWithText(Fuentes.PROMISE).assertIsDisplayed()
    for (area in DomainType.entries) rule.onNodeWithTag("fuentes_area_${area.name.lowercase()}").performScrollTo().assertIsDisplayed()
    val active = GameRegistry.allGames.filter { !GameRegistry.isRetired(it.id) }
    for (g in active) rule.onNodeWithTag("fuente_${g.id}").performScrollTo().assertIsDisplayed()
    // dentro de la losa: el nombre, qué tomamos y la cita con su enlace
    val tile = Fuentes.byGame.getValue("parejas")
    rule.onNodeWithText("Constelaciones", useUnmergedTree = true).assertExists()
    rule.onNodeWithText(tile.taken, useUnmergedTree = true).assertExists()
    rule.onNodeWithText("${tile.cite} · ${Fuentes.SEE_STUDY}", useUnmergedTree = true).assertExists()
  }

  @Test
  fun tocar_una_losa_abre_su_estudio_en_el_navegador_y_cada_una_el_suyo() {
    showFuentes()
    rule.onNodeWithTag("fuente_parejas").performScrollTo().performClick()
    assertEquals(listOf("https://doi.org/10.1037/0882-7974.23.1.104"), opened)
    for (g in GameRegistry.allGames.filter { !GameRegistry.isRetired(it.id) }) {
      rule.onNodeWithTag("fuente_${g.id}").performScrollTo().performClick()
    }
    val expected = listOf("parejas") + GameRegistry.allGames.filter { !GameRegistry.isRetired(it.id) }.map { it.id }
    assertEquals(expected.map { Fuentes.byGame.getValue(it).url }, opened)
    assertEquals("ningún aviso de «sin navegador» si lo hubo", 0, rule.onAllNodes(hasTestTag("fuentes_no_browser")).fetchSemanticsNodes().size)
  }

  @Test
  fun sin_navegador_lo_dice_con_una_linea_y_no_se_cae() {
    showFuentes(browser = false)
    rule.onNodeWithTag("fuente_secuencia").performScrollTo().performClick()
    rule.onNodeWithTag("fuentes_no_browser").assertIsDisplayed()
  }

  @Test
  fun cada_losa_dice_que_abre_el_estudio_y_se_lee_como_una_frase() {
    showFuentes()
    val f = Fuentes.byGame.getValue("stroop")
    val config = rule.onNodeWithTag("fuente_stroop").performScrollTo().fetchSemanticsNode().config
    assertEquals(listOf("Tinta o Palabra. ${f.taken} ${f.cite}. Abre el estudio en el navegador."), config.getOrNull(SemanticsProperties.ContentDescription))
    assertTrue("es tocable", config.contains(SemanticsActions.OnClick))
  }

  @Test
  fun al_final_las_palabras_y_lo_que_no_decimos_con_cuatro_revisiones_y_el_aviso_de_salud() {
    showFuentes()
    assertEquals("«Cómo se ajusta la dificultad» ya no es una sección aparte: es el primer grupo de «Cómo está hecha Nubi»", 0, rule.onAllNodes(hasTestTag("fuentes_metodo")).fetchSemanticsNodes().size)
    rule.onNodeWithText(Fuentes.WORDS_TEXT).performScrollTo().assertIsDisplayed()
    rule.onNodeWithTag("fuentes_no_decimos").performScrollTo().assertIsDisplayed()
    rule.onNodeWithTag("fuentes_no_decir_texto").performScrollTo().assertIsDisplayed()
    for (e in FuentesMas.noDecir) {
      rule.onNodeWithTag("estudio_${e.doi}").performScrollTo().assertIsDisplayed().performClick()
      assertEquals(e.url, opened.last())
      rule.onNodeWithText(e.line, useUnmergedTree = true).assertExists()
    }
    assertEquals(listOf("Simons y otros, 2016", "Melby-Lervåg y otros, 2016", "Sala y otros, 2018", "Gates y otros, 2020"), FuentesMas.noDecir.map { it.cite })
    rule.onNodeWithText(HealthNotice.TEXT).performScrollTo().assertIsDisplayed()
    // el aviso de salud sigue al final, después de las cuatro revisiones
    assertTrue(top("estudio_${FuentesMas.noDecir.last().doi}") < rule.onNodeWithText(HealthNotice.TEXT).fetchSemanticsNode().positionInRoot.y)
  }

  // ------------------------------------------------------------------ Tarea 186: Más estudios, Cómo está hecha Nubi

  private fun top(tag: String): Float = rule.onNodeWithTag(tag).fetchSemanticsNode().positionInRoot.y          // sin recorte: sirve también para lo que está fuera de la ventana
  private fun count(tag: String): Int = rule.onAllNodes(hasTestTag(tag)).fetchSemanticsNodes().size
  private fun stateOf(tag: String): String? = rule.onNodeWithTag(tag).fetchSemanticsNode().config.getOrNull(SemanticsProperties.StateDescription)

  @Test
  fun bajo_cada_losa_hay_Mas_estudios_plegado_y_al_tocarlo_se_despliegan_en_el_mismo_lugar() {
    showFuentes()
    val more = FuentesMas.byGame.getValue("parejas")
    // plegado por defecto: el botón dice cuántos hay y ningún estudio extra está en pantalla
    rule.onNodeWithTag("mas_parejas").performScrollTo().assertIsDisplayed()
    rule.onNodeWithTag("mas_parejas").assertTextContains("Más estudios (${more.size})")
    assertEquals("estudios ocultos", stateOf("mas_parejas"))
    for (e in more) assertEquals("«${e.cite}» no debe verse con «Más estudios» plegado", 0, count("estudio_${e.doi}"))
    // el botón está bajo SU losa y sobre la siguiente
    assertTrue(top("fuente_parejas") < top("mas_parejas") && top("mas_parejas") < top("fuente_secuencia"))
    // desplegar: cada estudio en su fila (cita, línea y «Ver el estudio ›») y, en el mismo lugar, entre su botón y la losa que sigue
    rule.onNodeWithTag("mas_parejas").performClick()
    assertEquals("estudios visibles", stateOf("mas_parejas"))
    for (e in more) {
      rule.onNodeWithTag("estudio_${e.doi}").performScrollTo().assertIsDisplayed()
      rule.onNodeWithText(e.cite, useUnmergedTree = true).assertExists()
      rule.onNodeWithText(e.line, useUnmergedTree = true).assertExists()
      assertTrue("«${e.cite}» queda bajo «Más estudios» y sobre la losa que sigue", top("mas_parejas") < top("estudio_${e.doi}") && top("estudio_${e.doi}") < top("fuente_secuencia"))
    }
    assertEquals("la losa principal sigue igual y abre el estudio principal", 1, count("fuente_parejas"))
    // tocar una fila abre SU DOI; la losa principal, el suyo
    rule.onNodeWithTag("estudio_${more[1].doi}").performScrollTo().performClick()
    assertEquals(more[1].url, opened.last())
    rule.onNodeWithTag("fuente_parejas").performScrollTo().performClick()
    assertEquals(Fuentes.byGame.getValue("parejas").url, opened.last())
    // plegar otra vez
    rule.onNodeWithTag("mas_parejas").performScrollTo().performClick()
    assertEquals("estudios ocultos", stateOf("mas_parejas"))
    for (e in more) assertEquals(0, count("estudio_${e.doi}"))
  }

  @Test
  fun los_17_juegos_traen_su_Mas_estudios_y_cada_estudio_extra_abre_el_suyo() {
    showFuentes()
    val expected = mutableListOf<String>()
    for (g in GameRegistry.allGames.filter { !GameRegistry.isRetired(it.id) }) {
      val more = FuentesMas.byGame.getValue(g.id)
      rule.onNodeWithTag("mas_${g.id}").performScrollTo().assertIsDisplayed()
      rule.onNodeWithTag("mas_${g.id}").assertTextContains("Más estudios (${more.size})")
      rule.onNodeWithTag("mas_${g.id}").performClick()
      for (e in more) {
        rule.onNodeWithTag("estudio_${e.doi}").performScrollTo().performClick()
        expected += e.url
      }
    }
    assertEquals(expected, opened)
    assertEquals("37 estudios extra", 37, opened.size)
    assertEquals("sin aviso de «sin navegador»", 0, count("fuentes_no_browser"))
  }

  /** El botón «Más estudios» y las filas de tres juegos, con la letra [scale]: 48 dp o más de alto y nada fuera del ancho. */
  private fun checkMoreRows(scale: Float) {
    showFuentes(scale)
    val dp = rule.density.density
    val root = rule.onRoot().fetchSemanticsNode().boundsInRoot
    for (id in listOf("parejas", "correo", "intrusa")) {
      val button = rule.onNodeWithTag("mas_$id").performScrollTo().fetchSemanticsNode()
      assertTrue("«Más estudios» de $id mide ${button.size.height / dp} dp con letra $scale", button.size.height >= 48 * dp - 1f)
      assertTrue("«Más estudios» de $id se sale del ancho", button.boundsInRoot.left >= 0f && button.boundsInRoot.right <= root.right + 1f)
      rule.onNodeWithTag("mas_$id").performClick()
      for (e in FuentesMas.byGame.getValue(id)) {
        val row = rule.onNodeWithTag("estudio_${e.doi}").performScrollTo().fetchSemanticsNode()
        assertTrue("«${e.cite}» mide ${row.size.height / dp} dp con letra $scale", row.size.height >= 48 * dp)
        assertTrue("«${e.cite}» se sale del ancho con letra $scale: ${row.boundsInRoot}", row.boundsInRoot.left >= 0f && row.boundsInRoot.right <= root.right + 1f)
      }
    }
  }

  @Test
  fun el_boton_y_las_filas_miden_48_dp_o_mas_y_caben_en_el_ancho_con_letra_1_0() = checkMoreRows(1f)

  @Test
  fun el_boton_y_las_filas_miden_48_dp_o_mas_y_caben_en_el_ancho_con_letra_2_0() = checkMoreRows(2f)

  @Test
  fun Como_esta_hecha_Nubi_va_entre_los_juegos_y_las_palabras_con_sus_diez_grupos_plegados() {
    showFuentes()
    rule.onNodeWithTag("fuentes_mecanismos").performScrollTo().assertIsDisplayed()
    rule.onNodeWithText(FuentesMas.MECHANISMS_TITLE).assertIsDisplayed()
    assertTrue(
      "entre la última losa de juegos y «Las palabras»",
      top("fuente_intrusa") < top("fuentes_mecanismos") && top("fuentes_mecanismos") < top("fuentes_palabras") && top("fuentes_palabras") < top("fuentes_no_decimos")
    )
    var previous = "fuentes_mecanismos"
    for (m in FuentesMas.mechanisms) {
      // a la vista: el título (un encabezado), la frase de lo que hace Nubi y «Estudios (N)» plegado; los estudios, no (Opus, 28-oct: «lo simple primero»)
      val title = rule.onNodeWithTag("mecanismo_titulo_${m.key}").performScrollTo().assertIsDisplayed()
      assertTrue("«${m.title}» es un encabezado para el lector de pantalla", title.fetchSemanticsNode().config.contains(SemanticsProperties.Heading))
      assertTrue("los grupos van en orden", top(previous) < top("mecanismo_titulo_${m.key}")); previous = "mecanismo_titulo_${m.key}"
      rule.onNodeWithTag("mecanismo_texto_${m.key}").performScrollTo().assertIsDisplayed()
      rule.onNodeWithText(m.text, useUnmergedTree = true).assertExists()
      rule.onNodeWithTag("estudios_${m.key}").performScrollTo().assertIsDisplayed().assertTextContains("Estudios (${m.studies.size})")
      assertEquals("estudios ocultos", stateOf("estudios_${m.key}"))
      for (e in m.studies) assertEquals("«${e.cite}» no debe verse con «Estudios» plegado", 0, count("estudio_${e.doi}"))
    }
  }

  @Test
  fun cada_grupo_despliega_sus_fuentes_bajo_su_frase_y_cada_una_abre_la_suya() {
    showFuentes()
    val expected = mutableListOf<String>()
    for (m in FuentesMas.mechanisms) {
      rule.onNodeWithTag("estudios_${m.key}").performScrollTo().performClick()
      assertEquals("estudios visibles", stateOf("estudios_${m.key}"))
      for (e in m.studies) {
        rule.onNodeWithTag("estudio_${e.doi}").performScrollTo().assertIsDisplayed().performClick()
        expected += e.url
        assertTrue("«${e.cite}» va bajo el botón «Estudios» de «${m.title}»", top("estudios_${m.key}") < top("estudio_${e.doi}") && top("mecanismo_texto_${m.key}") < top("estudios_${m.key}"))
      }
    }
    assertEquals(expected, opened)
    assertEquals("32 fuentes en los diez grupos", 32, expected.size)
    // plegar de nuevo
    val first = FuentesMas.mechanisms.first()
    rule.onNodeWithTag("estudios_${first.key}").performScrollTo().performClick()
    assertEquals("estudios ocultos", stateOf("estudios_${first.key}"))
    for (e in first.studies) assertEquals(0, count("estudio_${e.doi}"))
  }

  @Test
  fun la_dificultad_se_acomoda_a_ti_trae_el_metodo_y_Wilson_y_Salthouse_abre_Mas_tiempo_desde_los_65() {
    showFuentes()
    rule.onNodeWithText(Fuentes.METHOD_TEXT).performScrollTo().assertIsDisplayed()
    rule.onNodeWithTag("estudios_dificultad").performScrollTo().performClick()
    for (e in Fuentes.method + FuentesMas.mechanisms.first { it.key == "dificultad" }.studies.last()) {
      rule.onNodeWithTag("estudio_${e.doi}").performScrollTo().assertIsDisplayed()
      rule.onNodeWithText(e.line, useUnmergedTree = true).assertExists()
    }
    rule.onNodeWithText("En modelos de aprendizaje, el punto más provechoso está cerca de 85 aciertos de cada 100.", useUnmergedTree = true).assertExists()
    assertEquals("Salthouse ya no está en la dificultad", 0, count("estudio_${Fuentes.salthouse.doi}"))
    rule.onNodeWithTag("estudios_mas_tiempo").performScrollTo().performClick()
    rule.onNodeWithTag("estudio_${Fuentes.salthouse.doi}").performScrollTo().assertIsDisplayed()
    assertTrue("va primero, bajo su botón", top("estudios_mas_tiempo") < top("estudio_${Fuentes.salthouse.doi}"))
  }

  /** «Estudios (N)» de los grupos: 48 dp o más y dentro del ancho, con la letra [scale]. */
  private fun checkGroupFold(scale: Float) {
    showFuentes(scale)
    val dp = rule.density.density
    val root = rule.onRoot().fetchSemanticsNode().boundsInRoot
    for (key in listOf("dificultad", "meta_racha", "ver_y_tocar")) {
      val button = rule.onNodeWithTag("estudios_$key").performScrollTo().fetchSemanticsNode()
      assertTrue("«Estudios» de $key mide ${button.size.height / dp} dp con letra $scale", button.size.height >= 48 * dp - 1f)
      assertTrue("«Estudios» de $key se sale del ancho", button.boundsInRoot.left >= 0f && button.boundsInRoot.right <= root.right + 1f)
    }
  }

  @Test
  fun el_boton_de_los_grupos_mide_48_dp_o_mas_con_letra_1_0() = checkGroupFold(1f)

  @Test
  fun el_boton_de_los_grupos_mide_48_dp_o_mas_con_letra_2_0() = checkGroupFold(2f)

  // ------------------------------------------------------------------ Contacto

  @Test
  fun sin_correo_la_losa_se_ve_dice_correo_por_definir_y_no_hace_nada() {
    assertNull(NubiContact.EMAIL)
    var reports = 0
    showContacto(null) { reports++ }
    rule.onNodeWithText("Escribir a Nubi", useUnmergedTree = true).assertExists()
    rule.onNodeWithText("Correo por definir", useUnmergedTree = true).assertExists()
    assertEquals("sin correo no hay correo en texto plano", 0, rule.onAllNodes(hasTestTag("contact_email")).fetchSemanticsNodes().size)
    rule.onNodeWithTag("btn_write_to_nubi").assertIsNotEnabled()
    rule.onNodeWithTag("btn_write_to_nubi").performClick()
    val app = ApplicationProvider.getApplicationContext<Application>()
    assertNull("no se abrió ninguna app", shadowOf(app).nextStartedActivity)
    assertEquals(0, reports)
  }

  @Test
  fun con_correo_abre_la_app_de_correo_con_el_destinatario_y_lo_muestra_en_texto() {
    showContacto("hola@ejemplo.org")
    rule.onNodeWithTag("contact_email").assertIsDisplayed()
    assertTrue("el correo está en la losa y en texto plano", rule.onAllNodes(hasText("hola@ejemplo.org"), useUnmergedTree = true).fetchSemanticsNodes().size >= 2)
    rule.onNodeWithTag("btn_write_to_nubi").performClick()
    val app = ApplicationProvider.getApplicationContext<Application>()
    val intent: Intent = shadowOf(app).nextStartedActivity
    assertNotNull("abrió la app de correo", intent)
    assertEquals(Intent.ACTION_SENDTO, intent.action)
    assertEquals("mailto:", intent.dataString)
    assertEquals(listOf("hola@ejemplo.org"), intent.getStringArrayExtra(Intent.EXTRA_EMAIL)!!.toList())
    assertEquals("Nubi", intent.getStringExtra(Intent.EXTRA_SUBJECT))
  }

  @Test
  fun el_informe_de_errores_y_la_version_van_en_contacto() {
    var reports = 0
    showContacto(null) { reports++ }
    rule.onNodeWithTag("btn_send_error_report").performClick()
    assertEquals(1, reports)
    rule.onNodeWithTag("error_report_help").assertIsDisplayed()
    rule.onNodeWithTag("version_line").assertIsDisplayed()
    rule.onNodeWithText("Nubi ${BuildConfig.VERSION_NAME} · sin cuenta, sin internet y sin anuncios", useUnmergedTree = true).assertExists()
    assertFalse("el contacto no lleva política de privacidad todavía", java.io.File("src/main/java/com/example/ui/screens/Contacto.kt").readText().contains("rivacidad"))
  }
}
