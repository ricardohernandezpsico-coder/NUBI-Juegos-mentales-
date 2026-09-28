package com.example

import androidx.compose.foundation.background
import androidx.compose.foundation.layout.Column
import androidx.compose.foundation.layout.padding
import androidx.compose.foundation.layout.width
import androidx.compose.foundation.lazy.items
import androidx.compose.material3.Text
import androidx.compose.ui.Modifier
import androidx.compose.ui.graphics.Color
import androidx.compose.ui.test.junit4.createComposeRule
import androidx.compose.ui.test.onRoot
import androidx.compose.ui.text.font.FontWeight
import androidx.compose.ui.unit.dp
import androidx.compose.ui.unit.sp
import com.example.model.DomainType
import com.example.model.GameRegistry
import com.example.ui.screens.AreaRow
import com.example.ui.screens.ForYou
import com.example.ui.screens.LibraryPlanet
import com.example.ui.theme.Clay
import com.example.ui.theme.NeuroVidaTheme
import com.github.takahirom.roborazzi.RobolectricDeviceQualifiers
import com.github.takahirom.roborazzi.captureRoboImage
import org.junit.Rule
import org.junit.Test
import org.junit.runner.RunWith
import org.robolectric.RobolectricTestRunner
import org.robolectric.annotation.Config
import org.robolectric.annotation.GraphicsMode

/** La pestaña Juegos ("Para ti hoy" + filas por área) con datos de ejemplo, para verla sin teléfono. */
@RunWith(RobolectricTestRunner::class)
@GraphicsMode(GraphicsMode.Mode.NATIVE)
@Config(qualifiers = RobolectricDeviceQualifiers.Pixel8, sdk = [36])
class LibraryScreenshotTest {

  @get:Rule val composeTestRule = createComposeRule()

  @Test
  fun library() {
    val sub = mapOf("rumbo" to "a 18%", "bitacora" to "83%", "freno" to "212 ms", "piloto" to "12% costo", "secuencia" to "nivel 9",
      "parejas" to "nivel 5", "rutatesoro" to "nivel 4", "stroop" to "nivel 7", "cambiochip" to "nivel 6", "trafico" to "5 a la vez",
      "series" to "nivel 5")
    val isNew = setOf("correo", "satelites", "acoplamiento")
    composeTestRule.setContent {
      NeuroVidaTheme {
        Column(Modifier.width(412.dp).background(Color(0xFF0A1040)).padding(top = 12.dp)) {
          Text("Juegos", color = Color(0xFFEAF0FF), fontSize = 28.sp, fontWeight = FontWeight.Bold, modifier = Modifier.padding(horizontal = 20.dp))
          val ana = GameRegistry.getById("anagramas")!!
          ForYou(ana, ana.title, "Tu zona de Lenguaje está quieta hace 6 días.", hasMore = true, onPlay = {}, onOther = {}, onOpen = {})
          listOf(DomainType.MEMORIA, DomainType.ATENCION, DomainType.RAZONAMIENTO).forEach { d ->
            val stars = setOf("rumbo", "bitacora", "correo", "freno", "piloto", "satelites", "trafico", "acoplamiento")
            val games = GameRegistry.allGames.filter { it.domain == d }.sortedBy { it.id !in stars }
            AreaRow(d, d.displayName, games.size) {
              items(games) { g ->
                LibraryPlanet(
                  game = g, title = g.title, level = if (g.id in isNew) null else 0.55f,
                  star = g.id in setOf("rumbo", "bitacora", "correo", "freno", "piloto", "satelites", "trafico", "acoplamiento"),
                  playedToday = g.id == "bitacora" || g.id == "freno", isNew = g.id in isNew,
                  sub = if (g.id in isNew) "Nuevo" else sub[g.id] ?: "nivel 3",
                  subColor = if (g.id in isNew || !(sub[g.id] ?: "").startsWith("nivel")) Clay.Sun else Color(0xFF9AA3D6),
                  onClick = {}
                )
              }
            }
          }
        }
      }
    }
    composeTestRule.onRoot().captureRoboImage(filePath = "src/test/screenshots/library.png")
  }
}
