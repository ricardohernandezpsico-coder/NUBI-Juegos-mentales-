package com.example.model

import org.junit.Assert.assertEquals
import org.junit.Assert.assertTrue
import org.junit.Test
import java.io.File

/**
 * UN solo nombre por juego (Tarea 127): el del catálogo de la app (`GameRegistry`). Unity tiene su lista en `Games/Shared/GameTitles.cs` (la usa para vigilar sus controladores: `GameTitlesTests`); aquí se comprueba que sea igual al catálogo.
 * Correo decía «Correo Estelar» en la app y «La estación de correo» en el marcador y la cuenta de Unity.
 */
class GameTitlesUnityTest {
  private fun unityList(): File {
    val rel = "unity/NeuroVidaCore/Assets/Scripts/Games/Shared/GameTitles.cs"
    return listOf(File(rel), File("../$rel")).firstOrNull { it.exists() } ?: error("no encuentro $rel desde ${File(".").absolutePath}")
  }

  @Test
  fun `la lista de nombres de Unity es igual al catalogo de la app`() {
    val unity = Regex("""\("(\w+)",\s*"([^"]+)"\)""").findAll(unityList().readText()).associate { it.groupValues[1] to it.groupValues[2] }
    val app = GameRegistry.allGames.associate { it.id to it.title }
    assertTrue("la lista de Unity no se leyó", unity.size >= 17)
    assertEquals("los mismos juegos", app.keys, unity.keys)
    for ((id, title) in app) assertEquals("el nombre de «$id» en Unity", title, unity[id])
  }
}
