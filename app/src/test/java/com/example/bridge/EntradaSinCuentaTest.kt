package com.example.bridge

import android.app.Application
import androidx.compose.foundation.layout.Box
import androidx.compose.foundation.layout.height
import androidx.compose.foundation.layout.width
import androidx.compose.runtime.CompositionLocalProvider
import androidx.compose.ui.Modifier
import androidx.compose.ui.test.junit4.createComposeRule
import androidx.compose.ui.test.hasText
import androidx.compose.ui.test.onNodeWithText
import androidx.compose.ui.unit.dp
import androidx.test.core.app.ApplicationProvider
import com.example.TestSupport
import com.example.model.AgeBand
import com.example.model.GameRegistry
import com.example.ui.components.GameLoadingScreen
import com.example.ui.components.LocalReduceMotion
import java.io.File
import org.junit.After
import org.junit.Assert.assertFalse
import org.junit.Assert.assertTrue
import org.junit.Before
import org.junit.Rule
import org.junit.Test
import org.junit.runner.RunWith
import org.robolectric.RobolectricTestRunner
import org.robolectric.annotation.Config

/**
 * La entrada al juego sin cuenta regresiva (Tarea 190, `auditoria-fable/cuenta-y-tutoriales.md`, opción A): la app ya no manda los pasos de la cuenta, manda la bandera de depuración (`debug_build`: el sello de versión y la medición de la entrada,
 * solo en builds de depuración), y la pantalla de carga NO explica el juego (el planeta, el nombre y la etapa; «Cómo se juega» y su frase viven en la ficha y en la pausa).
 */
@RunWith(RobolectricTestRunner::class)
@Config(sdk = [36])
class EntradaSinCuentaTest {
  @get:Rule val composeTestRule = createComposeRule()
  private lateinit var app: Application

  @Before fun setUp() { app = ApplicationProvider.getApplicationContext(); TestSupport.resetDatabase() }
  @After fun tearDown() { TestSupport.resetDatabase() }

  private fun config(gameId: String): String =
    UnityGameLauncher.buildGameIntent(
      app, gameId, "u1", level = 3, baseIntensity = 50, timed = true, ageBand = AgeBand.ADULT, soundEnabled = true
    ).getStringExtra(UnityGameLauncher.EXTRA_CONFIG_JSON)!!

  @Test
  fun `la app ya no manda los pasos de la cuenta regresiva y manda la bandera de depuracion`() {
    for (game in GameRegistry.allGames.map { it.id }) {
      val cfg = config(game)
      assertFalse("$game: ya no hay countdown_steps", cfg.contains("countdown_steps"))
      assertTrue("$game: la build de pruebas es de depuración, así que va debug_build en true", cfg.contains("\"debug_build\":true"))
    }
  }

  @Test
  fun `ningun archivo de la app habla de la cuenta regresiva ni de sus pasos`() {
    val sources = File("src/main/java/com/example").walkTopDown().filter { it.isFile && it.extension == "kt" }.toList()
    assertTrue(sources.isNotEmpty())
    for (f in sources) {
      val t = f.readText()
      assertFalse("${f.name}: CountdownSteps ya no existe", t.contains("CountdownSteps"))
      assertFalse("${f.name}: countdown_steps ya no se manda", t.contains("countdown_steps") && !f.name.endsWith("UnityGameLauncher.kt"))
    }
  }

  @Test
  fun `el codigo de la pantalla de carga no tiene la explicacion del juego`() {
    val src = File("src/main/java/com/example/ui/components/GameLoadingScreen.kt").readLines()
      // la documentación del componente puede NOMBRAR lo que se quitó; lo que cuenta es el código y los literales
      .filterNot { it.trimStart().startsWith("*") || it.trimStart().startsWith("//") || it.trimStart().startsWith("/**") }
      .joinToString("\n")
    assertFalse("la carga no dice «Cómo se juega»", src.contains("\"Cómo se juega\""))
    assertFalse("la carga no muestra la instrucción del juego", src.contains("game.instruction") || src.contains(".instruction"))
  }

  @Test
  fun `la pantalla de carga muestra el nombre y la etapa pero no explica el juego`() {
    val game = GameRegistry.getById("secuencia")!!
    composeTestRule.mainClock.autoAdvance = false
    composeTestRule.setContent {
      CompositionLocalProvider(LocalReduceMotion provides true) {
        Box(Modifier.width(393.dp).height(800.dp)) { GameLoadingScreen(game = game, level = 3, timed = true) }
      }
    }
    composeTestRule.mainClock.advanceTimeBy(500)
    composeTestRule.onNodeWithText(game.title).assertExists()
    composeTestRule.onNode(hasText("Etapa", substring = true)).assertExists()
    composeTestRule.onNode(hasText("Con reloj", substring = true)).assertExists()
    composeTestRule.onNodeWithText("Cómo se juega").assertDoesNotExist()
    composeTestRule.onNode(hasText(game.instruction.take(24), substring = true)).assertDoesNotExist()
  }
}
