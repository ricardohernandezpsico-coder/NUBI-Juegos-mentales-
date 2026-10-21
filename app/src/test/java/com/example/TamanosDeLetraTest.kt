package com.example

import org.junit.Assert.assertEquals
import org.junit.Assert.assertTrue
import org.junit.Test
import java.io.File

/**
 * La letra de la app (Tarea 153; auditoría de coherencia B7 y C6; regla de `docs/DESIGN.md` §3 y §8.7): el texto que informa va a **16 sp o más**; 14 sp SOLO en los rótulos de los gráficos (la escala de las 5 etapas, el gráfico de las 4 semanas y los de los finales).
 * Se mira el código: ningún `fontSize` de la app baja de 16 sp fuera de esos archivos, y los estilos de Material que usa la app tampoco.
 */
class TamanosDeLetraTest {
  /** Los únicos archivos con 14 sp: rótulos de gráfico. */
  private val chartLabelFiles = setOf("ui/components/StageScale.kt", "ui/screens/HomeScreen.kt", "games/GameResultScreen.kt")
  private val size = Regex("fontSize = (\\d+(?:\\.\\d+)?)\\.sp")

  @Test
  fun `ningun texto baja de 16 sp salvo los rotulos de grafico a 14`() {
    val found = mutableListOf<String>()
    File("src/main/java").walkTopDown().filter { it.extension == "kt" }.forEach { f ->
      val rel = f.path.replace('\\', '/').removePrefix("src/main/java/com/example/")
      f.readLines().forEachIndexed { i, raw ->
        val line = raw.trim()
        if (line.startsWith("//") || line.startsWith("*") || line.startsWith("/*")) return@forEachIndexed
        for (m in size.findAll(raw)) {
          val v = m.groupValues[1].toFloat()
          if (v >= 16f) continue
          if (v == 14f && rel in chartLabelFiles) continue
          found += "$rel:${i + 1} fontSize = ${m.groupValues[1]}.sp"
        }
      }
    }
    assertTrue("Texto por debajo de 16 sp (la regla: 16 sp; 14 solo en rótulos de gráfico):\n" + found.joinToString("\n"), found.isEmpty())
  }

  @Test
  fun `los estilos de Material que usa la app son de 16 sp o mas y labelSmall ya no se usa`() {
    val type = File("src/main/java/com/example/ui/theme/Type.kt").readText()
    for (style in listOf("bodyMedium", "bodySmall", "titleSmall", "labelLarge", "labelMedium")) {
      val line = type.lines().first { it.trimStart().startsWith("$style = ") }
      val sp = size.find(line)?.groupValues?.get(1)?.toFloat() ?: 16f
      assertTrue("$style tiene $sp sp en Type.kt", sp >= 16f)
    }
    var users = 0
    File("src/main/java").walkTopDown().filter { it.extension == "kt" && it.name != "Type.kt" }.forEach { f -> if (f.readText().contains("typography.labelSmall")) users++ }
    assertEquals("nadie usa labelSmall (13 sp): lo que informa va a 16", 0, users)
  }

  @Test
  fun `el grafico de las cuatro semanas dice sem y va a 14 sp`() {
    val home = File("src/main/java/com/example/ui/screens/HomeScreen.kt").readText()
    assertTrue(home.contains("listOf(\"hace 3 sem.\", \"hace 2 sem.\", \"sem. pasada\", \"esta sem.\")"))
    assertTrue("los números y los rótulos del gráfico, a 14 sp (rótulos de gráfico)", !home.contains("fontSize = 13.sp"))
  }

  @Test
  fun `la rejilla de areas de Juegos termina con aire para que con letra grande nada quede bajo la barra`() {
    val games = File("src/main/java/com/example/ui/screens/GamesLibraryScreen.kt").readText()
    assertTrue(games.contains("padding(start = 12.dp, end = 12.dp, bottom = 16.dp)"))
  }
}
