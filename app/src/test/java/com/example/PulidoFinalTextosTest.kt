package com.example

import androidx.compose.foundation.layout.Box
import androidx.compose.foundation.layout.width
import androidx.compose.ui.Modifier
import androidx.compose.ui.test.assertIsDisplayed
import androidx.compose.ui.test.junit4.createComposeRule
import androidx.compose.ui.test.onNodeWithTag
import androidx.compose.ui.test.onNodeWithText
import androidx.compose.ui.unit.dp
import com.example.data.FinalesConSentido
import com.example.data.LibraryOrder
import com.example.ui.components.MeaningfulResultModel
import com.example.ui.components.ResultPhrase
import com.example.ui.theme.NeuroVidaTheme
import com.github.takahirom.roborazzi.RobolectricDeviceQualifiers
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
 * La ronda final de pulido de la app (Tarea 143, revisión de Fable del 19-oct, «Tarea A»): los textos que cambiaron y el final de la primera partida. Las pantallas con ViewModel (Ajustes, Perfil) no se arman acá: lo que dicen se fija leyendo
 * el código (como `GameSheetEdgeToEdgeTest`) y se ve en las capturas de `docs/previews/app-actual/`.
 */
@RunWith(RobolectricTestRunner::class)
@GraphicsMode(GraphicsMode.Mode.NATIVE)
@Config(qualifiers = RobolectricDeviceQualifiers.Pixel8, sdk = [36])
class PulidoFinalTextosTest {
  @get:Rule val rule = createComposeRule()

  private fun src(path: String) = File("src/main/java/com/example/$path").readText().replace("\r\n", "\n")

  private fun model(phrase: String) = MeaningfulResultModel(
    title = "", boxTitle = "Tu rastro", headline = "5 luces", headlineUnit = "seguidas, bien repetidas", dataLine = null, summaryLine = null, progressTitle = "Tu avance", chips = emptyList(), progressPhrase = phrase,
    trickTitle = "Truco", trick = "Sigue la luz", whyTitle = "¿Por qué importa?", why = "Porque sí", source = "Fuente", note = "Medida de esta partida. No es un diagnóstico."
  )

  private fun show(m: MeaningfulResultModel) {
    rule.setContent { NeuroVidaTheme { Box(Modifier.width(393.dp)) { ResultPhrase(m) } } }
    rule.waitForIdle()
  }

  private fun count(tag: String) = rule.onAllNodes(androidx.compose.ui.test.hasTestTag(tag)).fetchSemanticsNodes().size

  // ---- ítem 24 (Tarea 143) en el final «B» (Tarea 146): la primera partida de un juego

  @Test
  fun en_la_primera_partida_solo_se_ve_la_frase_que_dice_que_la_proxima_te_comparas_contigo_y_no_hay_cuadros() {
    show(model(FinalesConSentido.PHRASE_FIRST))
    rule.onNodeWithTag("progress_phrase").assertIsDisplayed()
    rule.onNodeWithText("Primera partida: la próxima te comparas contigo.").assertIsDisplayed()
    assertEquals("ya no hay cuadros de «Tu avance»", 0, count("progress_chips"))
    assertEquals("sin «Tu promedio» vacío", 0, rule.onAllNodes(androidx.compose.ui.test.hasText("Tu promedio")).fetchSemanticsNodes().size)
    assertEquals("sin «Tu mejor» vacío", 0, rule.onAllNodes(androidx.compose.ui.test.hasText("Tu mejor")).fetchSemanticsNodes().size)
  }

  @Test
  fun con_partidas_anteriores_la_frase_trae_el_promedio_y_el_mejor_y_no_hay_cuadros() {
    val rastro = com.example.model.GamePlayResult(
      gameId = "secuencia", score = 70, correctAnswers = 14, totalTrials = 19, timed = false, level = 3, endRating = 0.5f, timestamp = 1_000L,
      rasBestLen = listOf(5, 4, 3, 2), rasRounds = listOf(7, 4, 5, 3), rasHits = listOf(7, 3, 2, 2), rasModesSeen = 15, rasNewModes = 0
    )
    val previous = listOf(4f, 5f, 4f, 5f, 4f).mapIndexed { i, v -> com.example.data.MeasurePoint(100L * (i + 1), "trail", v, 0.5f, false) }
    val m = com.example.games.FinalModels.forGame(rastro, previous)!!
    assertTrue(m.progressPhrase, m.progressPhrase.contains("promedio") && m.progressPhrase.lowercase().contains("mejor"))
    show(m)
    rule.onNodeWithText(m.progressPhrase).assertIsDisplayed()
    assertEquals(0, count("progress_chips"))
  }

  @Test
  fun sin_medida_de_hoy_no_hay_frase_ni_cuadros() {
    // no hay con qué comparar y tampoco hay cifra de hoy: la unidad ya dice por qué, y la frase se omite para no decir dos veces lo mismo
    show(model(""))
    assertEquals(0, count("progress_phrase"))
    assertEquals(0, count("progress_chips"))
  }

  // ---- los textos que cambiaron (se fijan en el código; se ven en app-actual/)

  @Test
  fun ajustes_tiene_una_sola_cabecera_y_sus_textos_dicen_lo_que_hacen() {
    val settings = src("ui/screens/SettingsScreen.kt")
    assertFalse("el título grande «Ajustes y Preferencias» ya no está (A3)", settings.contains("strings.settingsTitle"))
    assertTrue("la cabecera del panel dice «Ajustes»", src("ui/screens/ProfileScreen.kt").contains("PanelHeader(\"Ajustes\", onClose)"))
    assertTrue("«Cómo juegas», no «Experiencia de Juego»", settings.contains("text = \"Cómo juegas\"") && !settings.contains("Experiencia de Juego"))
    assertTrue("«Vibración», no «Vibración y respuesta háptica»", settings.contains("text = \"Vibración\"") && !settings.contains("Vibración y respuesta háptica\","))
    assertTrue("la nota de la edad dice lo que hace (A4)", settings.contains("Cambia el ritmo y el tamaño de los juegos, no lo difícil: eso lo decide cómo juegas."))
    assertFalse("y no el «Por ahora ajusta la dificultad de Constelaciones» que ya no era cierto", settings.contains("Por ahora ajusta la dificultad de Constelaciones"))
    assertTrue("la hora de las 21:00 no repite «21:00 / 21:00» (A5)", settings.contains("Triple(\"Más tarde\", 21, 0)"))
    assertTrue("la tarjeta del nombre es «Tu nombre», no un segundo «Tu perfil» (ítem 19)", settings.contains("text = \"Tu nombre\"") && !settings.contains("text = \"Tu perfil\""))
    assertTrue("la nota de los colores no promete nada que no hace (R4)", settings.contains("Solo se guarda: hoy no cambia nada en los juegos.") && !settings.contains("lo usarán más adelante"))
    // las dos notas van a 16 sp, no en `labelSmall` (ítem 9)
    val notes = listOf("Cambia el ritmo y el tamaño de los juegos", "Solo se guarda: hoy no cambia nada")
    for (n in notes) {
      val at = settings.indexOf(n)
      val block = settings.substring(at, minOf(settings.length, at + 200))
      assertTrue("«$n» va a 16 sp: $block", block.contains("fontSize = 16.sp") && !block.contains("labelSmall"))
    }
  }

  @Test
  fun el_perfil_manda_con_la_isla_el_escudo_queda_chico_y_las_palabras_son_etapa_y_medallas() {
    val profile = src("ui/screens/ProfileScreen.kt")
    // Tarea 147 (P3): Nubi ya no va sobre el nombre (parecía su avatar): ahí va la isla en miniatura con las piezas de la persona
    assertTrue("la isla en miniatura va sobre el nombre", profile.contains("testTag(\"profile_island\")") && profile.contains("IslaPiezas.earned(unlocks.keys)"))
    assertFalse("y Nubi ya no", profile.contains("NubiClay("))
    assertFalse("el escudo grande ya no está (queda chico en «Tu liga»)", profile.contains("LeagueShield(tier = tier, size = 160.dp"))
    assertTrue("«Tu liga» con el escudo chico", profile.contains("SpaceSectionTitle(\"Tu liga\")") && profile.contains("ShieldImage(tier, 40.dp)"))
    assertTrue("«etapa», no «nivel hábil» (ítem 17)", profile.contains("\"Etapa general: \${levelWord(it / 100f)}\"") && !profile.contains("nivel \${levelWord"))
    assertTrue("«Tus medallas», no «Logros» (ítem 17)", profile.contains("SpaceSectionTitle(\"Tus medallas\"") && !profile.contains("SpaceSectionTitle(\"Logros\""))
  }

  @Test
  fun la_celebracion_dice_medalla_nueva_y_cuenta_medallas() {
    val overlay = src("ui/components/AchievementOverlay.kt")
    assertTrue("«Medalla nueva» (B3)", overlay.contains("text = \"Medalla nueva\""))
    assertFalse("sin «¡LOGRO!» en mayúsculas sostenidas con espaciado", overlay.contains("text = \"¡LOGRO!\"") || overlay.contains("letterSpacing = 3.sp"))
    assertTrue("«6 de 15 medallas»", overlay.contains("de \$totalCount medallas") && !overlay.contains("de \$totalCount logros"))
  }

  @Test
  fun el_grupo_de_los_jugados_alguna_vez_se_llama_para_volver_a_jugar() {
    assertEquals("Para volver a jugar", LibraryOrder.Group.HACE_DIAS.title)
    assertEquals(listOf("En tu ruta de hoy", "Para volver a jugar", "Nuevo para ti"), LibraryOrder.Group.entries.map { it.title })
  }

  @Test
  fun hoy_y_avance_dicen_cuatro_juegos_cortos_porque_son_cuatro() {
    assertTrue(src("ui/screens/HomeScreen.kt").contains("Encuentra tu punto de partida"))
    assertTrue(src("ui/screens/ProgressScreen.kt").contains("4 juegos cortos para que cada juego empiece a tu medida"))
    assertEquals("son 4 (`BaselinePlan.steps`)", 4, com.example.data.BaselinePlan.steps.size)
  }

  @Test
  fun avance_no_repite_la_linea_del_descanso_cuando_nubi_ya_la_dice() {
    val sections = src("ui/screens/AvanceSections.kt")
    assertTrue(sections.contains("if (!nubiLine.startsWith(\"Descansaste\")) OnBoardText(streakNote"))
  }
}
