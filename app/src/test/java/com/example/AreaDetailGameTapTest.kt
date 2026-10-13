package com.example

import androidx.compose.foundation.layout.Box
import androidx.compose.foundation.layout.height
import androidx.compose.foundation.layout.width
import androidx.compose.ui.Modifier
import androidx.compose.ui.test.junit4.createComposeRule
import androidx.compose.ui.test.onNodeWithTag
import androidx.compose.ui.test.onNodeWithText
import androidx.compose.ui.test.performClick
import androidx.compose.ui.test.performScrollTo
import androidx.compose.ui.unit.dp
import com.example.data.AreaStatus
import com.example.model.AppLanguage
import com.example.model.DomainType
import com.example.model.GameRegistry
import com.example.ui.components.AREA_ORDER
import com.example.ui.i18n.getGameTitle
import com.example.ui.screens.AreaDetail
import com.example.ui.theme.NeuroVidaTheme
import org.junit.Assert.assertEquals
import org.junit.Assert.assertNull
import org.junit.Rule
import org.junit.Test
import org.junit.runner.RunWith
import org.robolectric.RobolectricTestRunner
import org.robolectric.annotation.Config
import org.robolectric.annotation.GraphicsMode
import com.github.takahirom.roborazzi.RobolectricDeviceQualifiers

/**
 * El detalle de un área en Avance (Tarea 87): tocar el NOMBRE de un juego abre su ficha (`onGame`) y no lo juega; solo el botón grande «¡Sí, vamos!» juega directo (`onPlay`).
 */
@RunWith(RobolectricTestRunner::class)
@GraphicsMode(GraphicsMode.Mode.NATIVE)
@Config(qualifiers = RobolectricDeviceQualifiers.Pixel8, sdk = [36])
class AreaDetailGameTapTest {
  @get:Rule val rule = createComposeRule()

  private val day = 86_400_000L
  private val now = 20_000 * day
  private val names = DomainType.values().associate { it.name to it.displayName }
  private val statuses = listOf(
    AreaStatus("MEMORIA", 0.52f, 0.06f, listOf(0.40f, 0.43f, 0.46f, 0.52f)),
    AreaStatus("ATENCION", 0.62f, 0.04f, listOf(0.50f, 0.56f, 0.58f, 0.62f)),
    AreaStatus("RAZONAMIENTO", 0.43f, 0f, listOf(0.43f, 0.43f, 0.43f, 0.43f)),
    AreaStatus("LENGUAJE", 0.12f, 0.03f, listOf(0.06f, 0.08f, 0.09f, 0.12f))
  )
  private var played: String? = null
  private var card: String? = null

  private fun show() {
    rule.setContent {
      NeuroVidaTheme {
        Box(Modifier.width(393.dp).height(873.dp)) {
          AreaDetail(
            areaKey = AREA_ORDER[0], statuses = statuses, names = names, history = emptyList(), measures = emptyList(), now = now, lang = AppLanguage.SPANISH,
            onArea = {}, onPlay = { played = it }, onGame = { card = it }, onClose = {}
          )
        }
      }
    }
    rule.waitForIdle()
  }

  @Test
  fun tocar_el_nombre_de_un_juego_abre_su_ficha_y_no_lo_juega() {
    show()
    val game = GameRegistry.allGames.first { it.domain.name == AREA_ORDER[0] }
    rule.onNodeWithText(getGameTitle(game.id, AppLanguage.SPANISH, game.title)).performScrollTo().performClick()
    assertEquals(game.id, card)
    assertNull("no juega directo", played)
  }

  @Test
  fun el_boton_grande_si_juega_directo() {
    show()
    rule.onNodeWithTag("area_play").performClick()
    assertNull("el botón no abre una ficha", card)
    assertEquals("juega el juego que Nubi elige (el del área que lleva más sin jugarse)", GameRegistry.allGames.first { it.domain.name == AREA_ORDER[0] }.id, played)
  }
}
