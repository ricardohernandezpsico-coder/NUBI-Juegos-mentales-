package com.example

import androidx.compose.foundation.layout.Box
import androidx.compose.foundation.layout.height
import androidx.compose.foundation.layout.width
import androidx.compose.runtime.CompositionLocalProvider
import androidx.compose.ui.Modifier
import androidx.compose.ui.platform.LocalDensity
import androidx.compose.ui.test.assertIsDisplayed
import androidx.compose.ui.test.getUnclippedBoundsInRoot
import androidx.compose.ui.test.junit4.createComposeRule
import androidx.compose.ui.test.onNodeWithTag
import androidx.compose.ui.test.onNodeWithText
import androidx.compose.ui.unit.Density
import androidx.compose.ui.unit.dp
import androidx.compose.ui.unit.height
import com.example.data.AchievementFamily
import com.example.data.Achievements
import com.example.data.FlightMode
import com.example.data.FlightState
import com.example.data.FlightStep
import com.example.data.familyState
import com.example.ui.components.BoardBackground
import com.example.ui.screens.FirstFlightScreen
import com.example.ui.screens.FlightActions
import com.example.ui.theme.NeuroVidaTheme
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
 * Las costuras «de antes» de la app (Tarea 145; `auditoria-fable/revision-pulido-19-oct.md` §4 «Tarea C», ítems 14, 10 y 23 y los textos «C»):
 * la medalla de la celebración es la de plasticina, Licencias va sobre una losa crema, el cierre del Primer vuelo muestra la isla, el Perfil dice «Medallas N» y los textos que afirmaban de más.
 */
@RunWith(RobolectricTestRunner::class)
@GraphicsMode(GraphicsMode.Mode.NATIVE)
@Config(qualifiers = "w393dp-h873dp-xxhdpi", sdk = [36])
class CosturasDeAntesTest {
  @get:Rule val rule = createComposeRule()

  private fun source(path: String) = File("src/main/java/com/example/$path").readText().replace("\r\n", "\n")

  // ---------------------------------------------------------------- ítem 14: la medalla de la celebración

  @Test
  fun la_celebracion_dibuja_la_medalla_de_plasticina_y_no_la_vectorial() {
    val overlay = source("ui/components/AchievementOverlay.kt")
    assertTrue("la celebración usa FamilyMedal (la misma de Avance y del Perfil)", overlay.contains("FamilyMedal(medalState, MEDAL_HEIGHT)"))
    // la moneda vectorial solo queda como respaldo de un logro viejo, de desempeño (ya no se otorgan)
    assertEquals("una sola llamada al dibujo vectorial: el respaldo de los logros sin familia", 1, Regex("AchievementMedal\\(").findAll(overlay).count())
    assertTrue(overlay.contains("if (medalState != null) FamilyMedal(medalState, MEDAL_HEIGHT)\n          else AchievementMedal("))
    // todo logro que SE OTORGA tiene familia: la celebración nunca cae en el respaldo
    assertTrue("todos los logros que se otorgan tienen familia", Achievements.all.all { it.family != null })
    // y se conservan lo que siempre tuvo: título «Medalla nueva», los rayos y la luz, el sonido de la pieza
    for (kept in listOf("\"Medalla nueva\"", "drawGlint(", "Brush.radialGradient", "IslaPiezas.forAchievement", "NewPieceSound.play"))
      assertTrue("la celebración conserva $kept", overlay.contains(kept))
  }

  @Test
  fun la_tarjeta_de_compartir_tambien_lleva_la_medalla_de_plasticina() {
    val card = source("ui/components/ShareCard.kt")
    assertTrue(card.contains("drawKitPiece(medal.image"))
    assertTrue("con el número de su nivel en la placa", card.contains("plate.centerY"))
    assertTrue("y el dibujo vectorial solo para un logro sin familia", card.contains("} else if (achievement != null && medal != null) {") && card.contains("drawAchievementMedal(achievement"))
  }

  @Test
  fun la_tarjeta_de_compartir_se_dibuja_con_la_medalla_de_plasticina_en_el_centro() {
    val context = androidx.test.core.app.ApplicationProvider.getApplicationContext<android.content.Context>()
    val def = Achievements.byId("racha_7")!!
    val bmp = com.example.ui.components.ShareCard.render(context, com.example.ui.components.ShareCard.Content(headline = def.title, subtitle = def.description, achievement = def, stats = listOf("6" to "medallas")))
    assertEquals(1080, bmp.width); assertEquals(1920, bmp.height)
    File("src/test/screenshots").mkdirs()
    File("src/test/screenshots/compartir-medalla.png").outputStream().use { bmp.compress(android.graphics.Bitmap.CompressFormat.PNG, 100, it) }
    // en el centro de la medalla hay plasticina clara (la placa del número), no el cielo oscuro de la tarjeta
    var bright = 0
    for (x in 380 until 700 step 8) for (y in 540 until 900 step 8) { val p = bmp.getPixel(x, y); if (android.graphics.Color.red(p) > 150 && android.graphics.Color.green(p) > 120) bright++ }
    assertTrue("la medalla de plasticina ocupa el centro de la tarjeta (píxeles claros: $bright)", bright > 100)
  }

  // ---------------------------------------------------------------- ítem 10: Licencias sobre una losa

  @Test
  fun licencias_va_sobre_una_losa_crema_y_ningun_texto_queda_suelto_sobre_el_cielo() {
    val licenses = source("ui/screens/LicensesScreen.kt")
    assertTrue("el texto largo va en un ClayPanel", licenses.contains("ClayPanel(Modifier.fillMaxWidth().testTag(\"licenses_panel\")"))
    assertFalse("y ninguno es OnBoardText (texto sobre el cielo, con luceros detrás)", licenses.replace(Regex("/\\*.*?\\*/", RegexOption.DOT_MATCHES_ALL), "").replace(Regex("//.*"), "").contains("OnBoardText("))
    val design = File("../docs/DESIGN.md").readText().replace("\r\n", "\n")
    assertTrue("DESIGN §8 dice que la lectura larga va sobre una losa crema", design.contains("lectura LARGA") && design.contains("`ClayPanel`"))
  }

  // ---------------------------------------------------------------- ítem 23: el cierre del Primer vuelo

  private fun closing(widthDp: Int, heightDp: Int, fontScale: Float = 1f) {
    rule.setContent {
      val d = LocalDensity.current
      CompositionLocalProvider(LocalDensity provides Density(d.density, fontScale)) {
        NeuroVidaTheme {
          Box(Modifier.width(widthDp.dp).height(heightDp.dp)) {
            BoardBackground()
            FirstFlightScreen(FlightState(FlightMode.FULL, FlightStep.CAMINO), listOf("bodega", "calculo", "intrusa"), FlightActions.None)
          }
        }
      }
    }
    rule.waitForIdle()
  }

  @Test
  fun el_cierre_muestra_la_isla_con_las_dos_lineas_y_el_boton_sin_desplazar() {
    closing(393, 873)
    rule.onNodeWithTag("flight_island").assertIsDisplayed()
    rule.onNodeWithText("Tu primer vuelo fue tu ruta de hoy").assertIsDisplayed()
    rule.onNodeWithText("Mañana empieza tu ruta de 3 juegos", substring = true).assertIsDisplayed()
    rule.onNodeWithTag("btn_flight_finish").assertIsDisplayed()
    val island = rule.onNodeWithTag("flight_island").getUnclippedBoundsInRoot()
    val title = rule.onNodeWithText("Tu primer vuelo fue tu ruta de hoy").getUnclippedBoundsInRoot()
    val sub = rule.onNodeWithText("Mañana empieza tu ruta de 3 juegos", substring = true).getUnclippedBoundsInRoot()
    val button = rule.onNodeWithTag("btn_flight_finish").getUnclippedBoundsInRoot()
    assertTrue("la isla va sobre las dos líneas: ${island.bottom} y ${title.top}", island.bottom <= title.top + 1.dp)
    assertTrue("y todo entra sobre el botón sin desplazar: ${sub.bottom} y ${button.top}", sub.bottom <= button.top + 1.dp)
    assertTrue("la isla ocupa el alto que sobra (no es una miniatura): ${island.height}", island.height >= 250.dp)
  }

  private fun closingStillShowsEverything(w: Int, h: Int, scale: Float) {
    closing(w, h, scale)
    val at = "${w}x$h letra $scale"
    assertTrue("$at: la isla existe", rule.onAllNodesWithTagCount("flight_island") == 1)
    rule.onNodeWithTag("btn_flight_finish").assertIsDisplayed()                      // el botón de abajo está siempre a la vista
    val island = rule.onNodeWithTag("flight_island").getUnclippedBoundsInRoot()
    assertTrue("$at: la isla no baja de 170 dp (mide ${island.height})", island.height >= 169.dp)
  }

  @Test fun el_cierre_con_letra_1_3_sigue_mostrando_la_isla_y_el_boton() = closingStillShowsEverything(393, 873, 1.3f)
  @Test fun el_cierre_en_360_por_640_sigue_mostrando_la_isla_y_el_boton() = closingStillShowsEverything(360, 640, 1f)
  @Test fun el_cierre_con_letra_2_0_sigue_mostrando_la_isla_y_el_boton() = closingStillShowsEverything(393, 873, 2f)

  private fun androidx.compose.ui.test.junit4.ComposeContentTestRule.onAllNodesWithTagCount(tag: String) =
    this.onAllNodes(androidx.compose.ui.test.hasTestTag(tag)).fetchSemanticsNodes().size

  // ---------------------------------------------------------------- los textos «C»

  @Test
  fun los_textos_C_ya_no_afirman_de_mas() {
    val flight = source("data/FirstFlight.kt")
    assertTrue("la bienvenida ya no dice «Ya diste el primer paso» (nadie lo hizo todavía)", !flight.contains("\"Ya diste el primer paso\""))
    assertEquals("Empezamos juntos", com.example.data.FirstFlight.stationText(FlightState(FlightMode.FULL, FlightStep.HOLA)))
    assertEquals("el nombre sigue en la bienvenida (el primer paso numerado es el primer juego)", "Empezamos juntos", com.example.data.FirstFlight.stationText(FlightState(FlightMode.FULL, FlightStep.NOMBRE)))
    for (path in listOf("ui/screens/FirstFlightScreen.kt", "ui/screens/HomeScreen.kt"))
      assertTrue("«primer vuelo» va en minúscula en $path", !source(path).contains("Tu Primer vuelo fue"))
    assertTrue(source("ui/screens/FirstFlightScreen.kt").contains("\"Tu primer vuelo fue tu ruta de hoy\""))
    assertTrue(source("ui/screens/HomeScreen.kt").contains("\"Tu primer vuelo fue tu ruta de hoy. Mañana empieza tu ruta.\""))
  }

  @Test
  fun el_perfil_dice_medallas_N_con_los_niveles_ganados_en_vez_de_mejor() {
    val profile = source("ui/screens/ProfileScreen.kt")
    assertTrue(profile.contains("StatText(\"Medallas\", \"\$medalCount\""))
    assertTrue("ya no hay «Mejor» en la fila de cifras", !profile.contains("StatText(\"Mejor\""))
    assertTrue("N = los niveles ganados en las tres familias, el mismo número de «Tus medallas»", profile.contains("medals.sumOf { it.level }"))
    // y ese número es el de los logros que se otorgan y ya están conseguidos
    val unlocked = setOf("primer_paso", "racha_3", "racha_7", "dia_completo", "vuelta_1")
    val stats = com.example.data.AchievementStats(0, 0, 0, 0, 0, 0, 0, 0, 0)
    val levels = AchievementFamily.entries.sumOf { Achievements.familyState(it, unlocked, stats).level }
    assertEquals("Constancia nivel 3 (primer paso, 3 días, 7 días) + Variedad nivel 1 + Volver nivel 1", 3 + 1 + 1, levels)
    assertEquals(0, AchievementFamily.entries.sumOf { Achievements.familyState(it, emptySet(), stats).level })
  }
}
