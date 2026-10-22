package com.example

import org.junit.Assert.assertTrue
import org.junit.Test
import java.io.File

/**
 * Nada de íconos de Material en la app (Tarea 148, P4; `docs/DESIGN.md` §8.3, «sin íconos que no sean del kit»): las llamas, los tildes, las flechas y los engranajes son piezas de plasticina del kit (`KitGlyph`, `KitLlama`, `GameIcon`…).
 * Se mira el código: ninguna línea de `app/src/main/java` usa `Icons.` ni importa `androidx.compose.material.icons`, y ya no queda un `ImageVector` suelto. Única excepción (Tarea 158): `ui/components/NavIcons.kt`, los íconos de la barra de pestañas y de la losa «Todos los juegos»,
 * que NO son de Material: son vectores propios, de línea de tinta, que Ricardo eligió en la lámina de la Tarea 157 (los glifos de churro ampliados de la Tarea 156 «no se reconocían»).
 */
class MaterialIconsSweepTest {
  @Test
  fun `ningun archivo de la app usa un icono de Material`() {
    val found = mutableListOf<String>()
    File("src/main/java").walkTopDown().filter { it.extension == "kt" }.forEach { f ->
      val rel = f.path.replace('\\', '/').removePrefix("src/main/java/com/example/")
      f.readLines().forEachIndexed { i, raw ->
        val line = raw.trim()
        if (line.startsWith("//") || line.startsWith("*") || line.startsWith("/*")) return@forEachIndexed
        val propios = rel == "ui/components/NavIcons.kt"          // vectores propios de la barra (no de Material)
        if (line.contains("Icons.") || line.contains("material.icons") || (!propios && line.contains("ImageVector"))) found += "$rel:${i + 1}: $line"
      }
    }
    assertTrue("Íconos de Material en la app (usar una pieza del kit):\n" + found.joinToString("\n"), found.isEmpty())
  }
}
