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
  fun al_final_el_metodo_las_palabras_y_lo_que_no_decimos_con_el_aviso_de_salud() {
    showFuentes()
    rule.onNodeWithTag("fuentes_metodo").performScrollTo().assertIsDisplayed()
    rule.onNodeWithText(Fuentes.METHOD_TEXT).performScrollTo().assertIsDisplayed()
    for (e in Fuentes.method) rule.onNodeWithTag("estudio_${e.doi}").performScrollTo().assertIsDisplayed()
    rule.onNodeWithText(Fuentes.WORDS_TEXT).performScrollTo().assertIsDisplayed()
    rule.onNodeWithTag("fuentes_no_decimos").performScrollTo().assertIsDisplayed()
    rule.onNodeWithTag("fuentes_no_decir_texto").performScrollTo().assertIsDisplayed()
    rule.onNodeWithTag("estudio_${Fuentes.simons.doi}").performScrollTo().performClick()
    assertEquals(Fuentes.simons.url, opened.last())
    rule.onNodeWithText(HealthNotice.TEXT).performScrollTo().assertIsDisplayed()
  }

  @Test
  fun con_letra_1_3_cada_losa_crece_y_nada_se_sale_del_ancho() {
    showFuentes(1.3f)
    val root = rule.onRoot().fetchSemanticsNode().boundsInRoot
    for (id in listOf("parejas", "disparate", "satelites")) {
      val b = rule.onNodeWithTag("fuente_$id").performScrollTo().fetchSemanticsNode().boundsInRoot
      assertTrue("«$id» se sale del ancho: $b", b.left >= 0f && b.right <= root.right + 1f)
    }
  }

  @Test
  fun con_letra_2_0_las_losas_siguen_enteras() {
    showFuentes(2f)
    val root = rule.onRoot().fetchSemanticsNode().boundsInRoot
    for (id in listOf("parejas", "disparate", "satelites")) {
      val b = rule.onNodeWithTag("fuente_$id").performScrollTo().fetchSemanticsNode().boundsInRoot
      assertTrue("«$id» se sale del ancho: $b", b.left >= 0f && b.right <= root.right + 1f)
    }
  }

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
