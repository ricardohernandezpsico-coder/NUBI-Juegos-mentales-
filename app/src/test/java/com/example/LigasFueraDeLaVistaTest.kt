package com.example

import android.app.Application
import androidx.compose.foundation.layout.Box
import androidx.compose.foundation.layout.height
import androidx.compose.foundation.layout.width
import androidx.compose.runtime.CompositionLocalProvider
import androidx.compose.runtime.remember
import androidx.compose.runtime.mutableIntStateOf
import androidx.compose.ui.Modifier
import androidx.compose.ui.semantics.getOrNull
import androidx.compose.ui.test.hasContentDescription
import androidx.compose.ui.test.hasTestTag
import androidx.compose.ui.test.onRoot
import androidx.compose.ui.test.junit4.createComposeRule
import androidx.compose.ui.unit.dp
import androidx.test.core.app.ApplicationProvider
import com.example.notification.ReminderInput
import com.example.notification.buildReminder
import com.example.ui.components.ShareCard
import com.example.ui.screens.HomeScreen
import com.example.ui.screens.ProfileScreen
import com.example.ui.screens.ProgressScreen
import com.example.ui.theme.NeuroVidaTheme
import com.example.viewmodel.NeuroVidaViewModel
import org.junit.After
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

/**
 * Las ligas quedaron FUERA de la v1 (decisión de Ricardo, 19-oct) y la app las seguía mostrando (auditoría de coherencia del 20-oct, A1; Tarea 149): `Features.LEAGUES = false` oculta TODO lo de liga y trofeos (el escudo del avatar de Hoy, «Tu liga» en Avance
 * y en el Perfil, la celebración de ascenso, el compartir de liga, «trofeos» en los avisos); el dato se sigue guardando. Esta prueba mira (1) el código —cada sitio sigue detrás de la bandera y ningún texto fuera del código dormido habla de ligas—
 * y (2) lo que SE VE en Hoy, Avance y el Perfil con la bandera apagada.
 */
@RunWith(RobolectricTestRunner::class)
@GraphicsMode(GraphicsMode.Mode.NATIVE)
@Config(qualifiers = "w393dp-h4200dp-440dpi", sdk = [36])
class LigasFueraDeLaVistaTest {
  @get:Rule val composeTestRule = createComposeRule()

  private lateinit var app: Application
  private val viewModels = mutableListOf<NeuroVidaViewModel>()

  @Before
  fun setUp() {
    app = ApplicationProvider.getApplicationContext()
    TestSupport.resetDatabase()
  }

  @After
  fun tearDown() {
    TestSupport.release(*viewModels.toTypedArray())
    viewModels.clear()
    TestSupport.resetDatabase()
  }

  private fun source(path: String) = File("src/main/java/com/example/$path").readText().replace("\r\n", "\n")

  /** Las palabras de liga que no pueden verse: «liga(s)», «trofeo(s)» y «escudo(s)» (los nombres de las ligas, «Bronce», «Plata», «Oro»…, son también colores y nombres de medallas: solo cuentan junto a «liga»). */
  private val leagueWords = Regex("\\b(ligas?|trofeos?|escudos?)\\b", RegexOption.IGNORE_CASE)

  private val literal = Regex("\"((?:[^\"\\\\\\n]|\\\\.)*)\"")

  // ------------------------------------------------------------------ el código

  @Test
  fun la_bandera_esta_apagada() {
    assertFalse("las ligas están fuera de la v1", Features.LEAGUES)
  }

  /**
   * El código DORMIDO (lo que se ve solo con la bandera, los nombres de las ligas, las medallas viejas de liga y sus cálculos) puede nombrar ligas. Cualquier otro archivo de la app NO: si un texto nuevo habla de ligas o trofeos, esta prueba lo atrapa.
   */
  private val dormant = setOf(
    "HomeScreen.kt", "ProgressScreen.kt", "ProfileScreen.kt", "AvanceSections.kt", "SpaceSections.kt", "DebugTools.kt", "LeaguePromotionOverlay.kt", "LeagueShield.kt", "ShareCard.kt",
    "AvanceLogic.kt", "Achievements.kt", "Models.kt", "AvanceKit.kt", "ClayKit.kt", "LeagueEvents.kt", "NeuroVidaRepository.kt", "NeuroVidaViewModel.kt"
  )

  @Test
  fun ningun_texto_fuera_del_codigo_dormido_habla_de_ligas_ni_de_trofeos() {
    val found = mutableListOf<String>()
    File("src/main/java").walkTopDown().filter { it.extension == "kt" && it.name !in dormant }.forEach { f ->
      f.readLines().forEachIndexed { i, raw ->
        val line = raw.trim()
        if (line.startsWith("//") || line.startsWith("*") || line.startsWith("/*") || line.startsWith("import ")) return@forEachIndexed
        for (m in literal.findAll(raw)) {
          val text = m.groupValues[1]
          if (Regex("[a-z0-9_]+").matches(text)) continue
          val hit = leagueWords.find(text)?.value ?: continue
          found += "${f.name}:${i + 1} «$text» (por «$hit»)"
        }
      }
    }
    assertTrue("Textos que hablan de ligas o trofeos fuera del código dormido:\n" + found.joinToString("\n"), found.isEmpty())
  }

  /** Mira hacia atrás desde [marker] hasta [window] caracteres y exige que `Features.LEAGUES` esté ahí: el sitio sigue detrás de la bandera. */
  private fun assertGated(file: String, marker: String, window: Int = 420) {
    val s = source(file)
    val at = s.indexOf(marker)
    assertTrue("«$marker» no está en $file", at >= 0)
    val before = s.substring(maxOf(0, at - window), at)
    assertTrue("«$marker» de $file ya no está detrás de Features.LEAGUES", before.contains("Features.LEAGUES"))
  }

  @Test
  fun cada_sitio_de_liga_sigue_detras_de_la_bandera() {
    assertGated("ui/screens/HomeScreen.kt", "LeagueShield(tier = ui.tier, size = 20.dp)")
    assertGated("ui/screens/HomeScreen.kt", "\"Tu perfil, liga \${ui.tier.tierName}\"")
    assertGated("ui/screens/ProgressScreen.kt", "AvanceLeague(leagueRow(avg))")
    assertGated("ui/screens/ProfileScreen.kt", "SpaceSectionTitle(\"Tu liga\")")
    assertGated("ui/screens/DebugTools.kt", "[Debug] Ver celebración de ascenso de liga", window = 80)
    val vm = source("viewmodel/NeuroVidaViewModel.kt")
    assertTrue(vm.contains("fun debugShowPromotion() {\n    if (!com.example.Features.LEAGUES) return"))
    // el ascenso de liga solo llega a la pantalla con la bandera: nunca `_promotion.value = outcome.promotion(...)` sin ella
    for (line in vm.lines().filter { it.contains("_promotion.value = outcome.promotion(") }) assertTrue("sin bandera: $line", line.contains("Features.LEAGUES"))
  }

  @Test
  fun la_tarjeta_de_compartir_dice_cada_dia_un_poco_y_se_guarda_como_nubi_medalla() {
    assertEquals("Juega. Avanza. Cada día un poco.", ShareCard.FOOTER)
    assertEquals("nubi-medalla.png", ShareCard.FILE_NAME)
    for (t in listOf(ShareCard.FOOTER, ShareCard.FILE_NAME)) {
      assertFalse(t, leagueWords.containsMatchIn(t) || t.lowercase().contains("neurovida"))
    }
    val card = source("ui/components/ShareCard.kt")
    assertFalse("el pie viejo ya no se dibuja", card.contains("\"Juega. Avanza. Sube de liga.\""))
    assertFalse("el archivo viejo ya no se usa", card.contains("\"neurovida-liga.png\""))
    assertTrue(card.contains("centered(measurer, FOOTER,") && card.contains("File(dir, FILE_NAME)"))
  }

  @Test
  fun los_avisos_ya_no_hablan_de_trofeos_ni_de_ligas() {
    for (day in 0..30) for (gap in listOf(null, 1, 3, 9)) for (streak in listOf(0, 4, 6)) for (completed in 0..2) for (played in listOf(false, true)) {
      val m = buildReminder(ReminderInput("Ricardo", streak, played, completed, 1, 4, "Rastro de luz", gap, day)) ?: continue
      val t = m.title + " " + m.text
      assertFalse("«$t» habla de ligas o trofeos", leagueWords.containsMatchIn(t))
    }
    // el de «vuelves después de varios días» dice lo que pide Opus: sigues tu ruta
    val back = buildReminder(ReminderInput("Ricardo", 0, false, 0, 0, 4, "Rastro de luz", 5, 0))!!
    assertEquals("Te esperan 3 juegos cortos y sigues tu ruta. Hoy empieza con Rastro de luz.", back.text)
  }

  // ------------------------------------------------------------------ lo que se ve

  private fun assertNoLeagueOnScreen(show: @androidx.compose.runtime.Composable (NeuroVidaViewModel) -> Unit) {
    val vm = NeuroVidaViewModel(app).also { viewModels.add(it) }
    composeTestRule.setContent {
      NeuroVidaTheme {
        CompositionLocalProvider(com.example.ui.components.LocalTabSwipeLock provides remember { mutableIntStateOf(0) }) {
          Box(Modifier.width(393.dp).height(4000.dp)) { show(vm) }
        }
      }
    }
    composeTestRule.waitForIdle()
    fun walk(n: androidx.compose.ui.semantics.SemanticsNode): List<String> =
      (n.config.getOrNull(androidx.compose.ui.semantics.SemanticsProperties.Text)?.map { it.text } ?: emptyList()) +
        listOfNotNull(n.config.getOrNull(androidx.compose.ui.semantics.SemanticsProperties.ContentDescription)?.joinToString(" ")) + n.children.flatMap { walk(it) }
    val texts = walk(composeTestRule.onRoot(useUnmergedTree = true).fetchSemanticsNode())
    val bad = texts.filter { leagueWords.containsMatchIn(it) }
    assertTrue("se ve algo de ligas o trofeos: $bad", bad.isEmpty())
    assertEquals("sin la sección «Tu liga» del Perfil", 0, composeTestRule.onAllNodes(hasTestTag("profile_league")).fetchSemanticsNodes().size)
    assertEquals("el avatar va solo con su inicial", 0, composeTestRule.onAllNodes(hasContentDescription("Tu perfil, liga Bronce")).fetchSemanticsNodes().size)
  }

  @Test fun hoy_no_muestra_el_escudo_ni_habla_de_ligas() = assertNoLeagueOnScreen { vm -> HomeScreen(viewModel = vm, onNavigateToGames = {}) }

  @Test fun avance_no_muestra_tu_liga_ni_compartir_la_liga() = assertNoLeagueOnScreen { vm -> ProgressScreen(viewModel = vm) }

  @Test fun el_perfil_no_muestra_tu_liga() = assertNoLeagueOnScreen { vm -> ProfileScreen(viewModel = vm, onClose = {}) }
}
