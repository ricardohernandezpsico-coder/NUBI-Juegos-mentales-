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
import androidx.compose.ui.test.performScrollTo
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
 * la medalla de la celebración es la de plasticina, Licencias va sobre una losa crema, el cierre del Primer vuelo muestra a Nubi (la isla salió en la Tarea 185), el Perfil dice «Medallas N» y los textos que afirmaban de más.
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
    // y se conservan lo que siempre tuvo: título «Medalla nueva» y la luz; la línea «Y en tu isla aparece: …» y su sonido se fueron con la isla (Tarea 185)
    for (kept in listOf("\"Medalla nueva\"", "Brush.radialGradient"))
      assertTrue("la celebración conserva $kept", overlay.contains(kept))
    for (gone in listOf("IslaPiezas", "NewPieceSound", "achievement_piece", "Y en tu isla"))
      assertTrue("la celebración ya no menciona $gone (la isla salió de la app)", !overlay.contains(gone))
    // Tarea 148 (P4): sin el abanico de rayos vectoriales que giraba ni los destellos con puntas: un halo sol suave y seis luceros redondos que suben; el título en sol con el contorno de las pestañas
    assertTrue("el halo sol", overlay.contains("Clay.Sun.copy(alpha = 0.5f * a)"))
    assertTrue("seis luceros redondos", overlay.contains("sparks.take(6)") && overlay.contains("drawCircle(sparkColors"))
    assertTrue("el título en el amarillo suave del texto sobre el cielo (Tarea 194; antes el sol) con el contorno de los títulos de las pestañas", overlay.contains("OnBoardText(def.title, color = com.example.ui.theme.NubiTokens.SunOnNight"))
    for (gone in listOf("drawGlint(", "val spin", "rotate("))
      assertTrue("la celebración ya no usa $gone (rayos que giran, destellos con puntas)", !overlay.contains(gone))
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
  fun el_cierre_muestra_a_Nubi_con_las_dos_lineas_y_el_boton_sin_desplazar() {
    closing(393, 873)
    rule.onNodeWithTag("flight_nubi").assertIsDisplayed()
    rule.onNodeWithText("Tu primer vuelo fue tu ruta de hoy").assertIsDisplayed()
    rule.onNodeWithText("Mañana empieza tu ruta de 3 juegos", substring = true).assertIsDisplayed()
    rule.onNodeWithTag("btn_flight_finish").assertIsDisplayed()
    val island = rule.onNodeWithTag("flight_nubi").getUnclippedBoundsInRoot()
    val title = rule.onNodeWithText("Tu primer vuelo fue tu ruta de hoy").getUnclippedBoundsInRoot()
    val sub = rule.onNodeWithText("Mañana empieza tu ruta de 3 juegos", substring = true).getUnclippedBoundsInRoot()
    val button = rule.onNodeWithTag("btn_flight_finish").getUnclippedBoundsInRoot()
    assertTrue("Nubi va sobre las dos líneas: ${island.bottom} y ${title.top}", island.bottom <= title.top + 1.dp)
    assertTrue("y todo entra sobre el botón sin desplazar: ${sub.bottom} y ${button.top}", sub.bottom <= button.top + 1.dp)
    // Tarea 194b (Ricardo: «se ve desplazado hacia abajo, debe estar más arriba»): Nubi ya no se estira; Nubi, el título y la frase son UN bloque centrado en el alto disponible, como «¿Sabías que…?»
    assertTrue("Nubi ya no se estira: su lugar mide lo que mide ella (%.0f dp)".format(island.height.value), island.height <= 200.dp)
    assertTrue("hay aire entre la frase y el botón (bloque centrado, no pegado a él): ${sub.bottom} y ${button.top}", button.top - sub.bottom >= 60.dp)
    val stepText = rule.onNodeWithTag("flight_step_text").getUnclippedBoundsInRoot()
    val above = island.top.value - stepText.bottom.value                      // lo que queda entre «Paso 6 de 6» y Nubi
    val below = (button.top - sub.bottom).value - 10f                         // y entre la frase y el botón (el pie deja 10 dp sobre él)
    assertTrue("el bloque está centrado en el alto disponible: sobra %.0f dp arriba y %.0f dp abajo (casi lo mismo)".format(above, below), kotlin.math.abs(above - below) <= 20f)
    println("CIERRE 393 x 873 letra 1,0: título empieza en %.0f dp, la frase termina en %.0f dp y el botón empieza en %.0f dp".format(title.top.value, sub.bottom.value, button.top.value))
    // Nubi despierta (ojos abiertos, parpadea: Tarea 184) y grande (200 dp de ancho: `NubiSize.L`), centrada; y el cierre ya no habla de la isla (Tarea 185)
    val nubi = rule.onNodeWithTag("nubi_ojos_abiertos", useUnmergedTree = true).getUnclippedBoundsInRoot()
    assertEquals("Nubi mide 200 dp de ancho (el tamaño L)", 200f, (nubi.right - nubi.left).value, 1f)
    assertTrue("y va centrada: ${nubi.left} y ${nubi.right}", kotlin.math.abs((nubi.left + nubi.right).value / 2f - 393f / 2f) < 2f)
    assertEquals("ningún texto del cierre dice «isla»", 0, rule.onAllNodes(androidx.compose.ui.test.hasText("isla", substring = true, ignoreCase = true)).fetchSemanticsNodes().size)
    assertEquals("ni su descripción", 0, rule.onAllNodes(androidx.compose.ui.test.hasContentDescription("isla", substring = true, ignoreCase = true), useUnmergedTree = true).fetchSemanticsNodes().size)
  }

  private fun closingStillShowsEverything(w: Int, h: Int, scale: Float) {
    closing(w, h, scale)
    val at = "${w}x$h letra $scale"
    assertTrue("$at: Nubi existe", rule.onAllNodesWithTagCount("flight_nubi") == 1)
    rule.onNodeWithTag("btn_flight_finish").assertIsDisplayed()                      // el botón de abajo está siempre a la vista
    // con la letra muy grande el bloque se desliza: se llega a la frase y queda SOBRE el botón (nada se corta ni se tapa)
    rule.onNodeWithText("Mañana empieza tu ruta de 3 juegos", substring = true).performScrollTo()
    val sub = rule.onNodeWithText("Mañana empieza tu ruta de 3 juegos", substring = true).getUnclippedBoundsInRoot()
    val button = rule.onNodeWithTag("btn_flight_finish").getUnclippedBoundsInRoot()
    assertTrue("$at: la frase (termina en ${sub.bottom}) no se tapa con el botón (empieza en ${button.top})", sub.bottom <= button.top + 1.dp)
    rule.onNodeWithText("Mañana empieza tu ruta de 3 juegos", substring = true).assertIsDisplayed()
    rule.onNodeWithText("Tu primer vuelo fue tu ruta de hoy").performScrollTo()
    rule.onNodeWithText("Tu primer vuelo fue tu ruta de hoy").assertIsDisplayed()
  }

  @Test fun el_cierre_con_letra_1_3_sigue_mostrando_a_Nubi_y_el_boton() = closingStillShowsEverything(393, 873, 1.3f)
  @Test fun el_cierre_en_360_por_640_sigue_mostrando_a_Nubi_y_el_boton() = closingStillShowsEverything(360, 640, 1f)
  @Test fun el_cierre_con_letra_2_0_sigue_mostrando_a_Nubi_y_el_boton() = closingStillShowsEverything(393, 873, 2f)

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
    assertTrue(source("ui/screens/HomeScreen.kt").contains("\"Tu primer vuelo fue tu ruta de hoy.\""))
  }

  @Test
  fun el_perfil_dice_medallas_N_con_los_niveles_ganados_en_vez_de_mejor() {
    val profile = source("ui/screens/ProfileScreen.kt")
    assertTrue("Tú dice «N de 15 medallas» en su cuadro con el mismo número (Tarea 185)", profile.contains("medals = medalCount"))
    assertTrue("ya no hay «Mejor» ni la fila de tres cifras", !profile.contains("StatText("))
    assertTrue("N = los niveles ganados en las tres familias, el mismo número de «Tus medallas»", profile.contains("medals.sumOf { it.level }"))
    // y ese número es el de los logros que se otorgan y ya están conseguidos
    val unlocked = setOf("primer_paso", "racha_3", "racha_7", "dia_completo", "vuelta_1")
    val stats = com.example.data.AchievementStats(0, 0, 0, 0, 0, 0, 0, 0, 0)
    val levels = AchievementFamily.entries.sumOf { Achievements.familyState(it, unlocked, stats).level }
    assertEquals("Constancia nivel 3 (primer paso, 3 días, 7 días) + Variedad nivel 1 + Volver nivel 1", 3 + 1 + 1, levels)
    assertEquals(0, AchievementFamily.entries.sumOf { Achievements.familyState(it, emptySet(), stats).level })
  }
}
