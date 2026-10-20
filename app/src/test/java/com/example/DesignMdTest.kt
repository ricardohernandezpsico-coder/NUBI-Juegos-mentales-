package com.example

import androidx.compose.ui.graphics.Color
import androidx.compose.ui.graphics.toArgb
import com.example.ui.components.BoardNavy
import com.example.ui.components.NubiMotion
import com.example.ui.theme.Clay
import com.example.ui.theme.NubiTokens
import com.squareup.moshi.Moshi
import org.junit.Assert.assertEquals
import org.junit.Assert.assertTrue
import org.junit.Test
import java.io.File

/**
 * `docs/DESIGN.md` envejece si nadie lo mira (Tarea 131, A5; `auditoria-fable/spec-adopcion-A1-A7.md` §A5.2): el documento está escrito a mano desde el código y esta prueba lo ata en lo medible. Las pruebas corren en `app/`, por eso la ruta
 * `../docs/DESIGN.md` (como `VisibleTextsGuardTest` lee `src/main/java`).
 *
 * - **Colores (§2):** cada fila con un token (`Clay.X`, `NubiTokens.X`, `BoardNavy` o `kit.json colores.X`) trae el hex del color real.
 * - **Movimiento (§5):** cada `NubiMotion.<CONST>` con su «N ms» coincide con la constante (y no falta ninguna).
 * - **Medidas (§4):** cada `kit.json <ruta>` con su «N dp» coincide con el valor de `res/raw/kit.json`; los radios 14 / 22 / 30 y el 2 dp de la losa presionada están.
 * - **Do / Don't (§8):** las seis frases centinela y las doce líneas.
 * - **Vocabulario:** fuera de la línea «prohibidas:» de §1, el documento no trae las raíces que nunca pueden verse.
 */
class DesignMdTest {
  private val doc = File("../docs/DESIGN.md").readText().replace("\r\n", "\n")

  private fun hex(c: Color) = String.format("#%06X", c.toArgb() and 0xFFFFFF)

  /** El texto de la sección «## N · …» (hasta la siguiente «## »). */
  private fun section(n: Int): String {
    val start = doc.indexOf("\n## $n · ").also { assertTrue("falta la sección «## $n · …»", it >= 0) }
    val next = doc.indexOf("\n## ", start + 4)
    return if (next < 0) doc.substring(start) else doc.substring(start, next)
  }

  /** Las filas de una tabla de Markdown con sus celdas (sin la cabecera ni la línea de guiones). */
  private fun rows(text: String): List<List<String>> =
    text.lines().filter { it.trim().startsWith("|") && !it.contains("---") }.map { l -> l.trim().trim('|').split("|").map { it.trim() } }

  private val colors: Map<String, Color> = mapOf(
    "Clay.Ink" to Clay.Ink, "Clay.Coral" to Clay.Coral, "Clay.Sun" to Clay.Sun, "Clay.Sky" to Clay.Sky, "Clay.Grape" to Clay.Grape, "Clay.Lime" to Clay.Lime, "Clay.Cream" to Clay.Cream,
    "Clay.InkSoft" to Clay.InkSoft, "Clay.InkMuted" to Clay.InkMuted,
    "NubiTokens.OnNight" to NubiTokens.OnNight, "NubiTokens.OnNightDim" to NubiTokens.OnNightDim,
    "NubiTokens.Memoria" to NubiTokens.Memoria, "NubiTokens.Atencion" to NubiTokens.Atencion, "NubiTokens.Razonamiento" to NubiTokens.Razonamiento, "NubiTokens.Lenguaje" to NubiTokens.Lenguaje,
    "NubiTokens.Brand" to NubiTokens.Brand, "NubiTokens.Success" to NubiTokens.Success, "NubiTokens.Neutral" to NubiTokens.Neutral,
    "BoardNavy" to BoardNavy
  )

  @Suppress("UNCHECKED_CAST")
  private val kit: Map<String, Any?> = Moshi.Builder().build().adapter(Map::class.java).fromJson(File("src/main/res/raw/kit.json").readText()) as Map<String, Any?>

  /** El valor de una ruta como «losas.ch.radio_dp» dentro de `kit.json`. */
  private fun kitValue(path: String): Any? {
    var cur: Any? = kit
    for (k in path.split(".")) cur = (cur as? Map<*, *>)?.get(k) ?: return null
    return cur
  }

  private val tokenCell = Regex("^`([^`]+)`$")
  private val hexCell = Regex("^#[0-9A-Fa-f]{6}$")

  @Test
  fun `la semana de Hoy dibuja la farola del kit y DESIGN lo dice`() {
    // Tarea 143, ítem 11 (revisión de Fable del 19-oct): DESIGN §9 decía que Hoy usa la farola y Hoy dibujaba discos; ahora Hoy, Avance, Ajustes y el Primer vuelo dibujan la misma farola
    val weekLamps = File("src/main/java/com/example/ui/components/WeekLamps.kt").readText().replace("\r\n", "\n")
    val hoyWeek = weekLamps.substring(weekLamps.indexOf("fun HoyWeek("), weekLamps.indexOf("/** Un farolito:"))
    assertTrue("HoyWeek dibuja WeekLampRendered", hoyWeek.contains("WeekLampRendered("))
    assertTrue("y ya no los discos de WeekLamp", !hoyWeek.contains("WeekLamp(on"))
    assertTrue("§9 dice que la farola va en la semana de Hoy y de Avance", section(9).contains("`WeekLampRendered`") && section(9).contains("la semana de Hoy y de Avance"))
  }

  @Test
  fun `el documento trae sus trece secciones en orden`() {
    var last = -1
    for (n in 0..12) {
      val at = doc.indexOf("\n## $n · ")
      assertTrue("falta «## $n · …»", at >= 0)
      assertTrue("«## $n · …» está fuera de orden", at > last)
      last = at
    }
    assertTrue("el título", doc.startsWith("# Nubi — Style Reference"))
    assertTrue("la sección de la web", section(12).contains("Web (landing"))
  }

  @Test
  fun `los colores de la tabla de colores son los reales`() {
    var checked = 0
    val kitColors = kitValue("colores") as Map<*, *>
    for (cells in rows(section(2))) {
      if (cells.size < 3) continue
      val token = tokenCell.find(cells[2])?.groupValues?.get(1) ?: continue
      val shown = cells[1]
      assertTrue("la fila «${cells[0]}» no trae un hex (#RRGGBB) en su segunda celda: «$shown»", hexCell.matches(shown))
      val real = if (token.startsWith("kit.json colores.")) (kitColors[token.removePrefix("kit.json colores.")] as? String)?.uppercase() ?: error("no hay «$token» en kit.json")
      else hex(colors[token] ?: error("la prueba no conoce el token «$token» (sumarlo al mapa de DesignMdTest)"))
      assertEquals("la fila «${cells[0]}» (`$token`) dice $shown y el código tiene $real", real, shown.uppercase())
      checked++
    }
    assertTrue("se comprobaron solo $checked colores", checked >= 25)
    // todos los colores del mapa están documentados (si se suma uno al código y al mapa, va a la tabla)
    val documented = rows(section(2)).mapNotNull { tokenCell.find(it.getOrNull(2) ?: "")?.groupValues?.get(1) }.toSet()
    for (k in colors.keys) assertTrue("`$k` no está en la tabla de colores de DESIGN.md", k in documented)
  }

  @Test
  fun `el cielo de tres tonos es el del codigo`() {
    val src = File("src/main/java/com/example/ui/components/CosmosBackground.kt").readText()
    for (h in listOf("07071F", "100F3A", "1C1B52")) {
      assertTrue("DESIGN.md no dice #$h", section(2).contains("#$h"))
      assertTrue("CosmosBackground ya no usa #$h", src.contains("0xFF$h"))
    }
  }

  @Test
  fun `las duraciones de movimiento son las de NubiMotion`() {
    val real = mapOf(
      "PRESS_MS" to NubiMotion.PRESS_MS, "RELEASE_MS" to NubiMotion.RELEASE_MS, "STATE_FAST_MS" to NubiMotion.STATE_FAST_MS, "STATE_MS" to NubiMotion.STATE_MS, "TAB_MS" to NubiMotion.TAB_MS,
      "SHEET_MS" to NubiMotion.SHEET_MS, "CELEBRATE_MS" to NubiMotion.CELEBRATE_MS, "CELEBRATE_LONG_MS" to NubiMotion.CELEBRATE_LONG_MS, "REDUCED_FADE_MS" to NubiMotion.REDUCED_FADE_MS
    )
    val cite = Regex("`NubiMotion\\.([A-Z_]+)`\\s*\\|\\s*(\\d+) ms")
    val found = cite.findAll(section(5)).associate { it.groupValues[1] to it.groupValues[2].toInt() }
    for ((name, ms) in found) assertEquals("`NubiMotion.$name` dice $ms ms en DESIGN.md", real[name] ?: error("la prueba no conoce `NubiMotion.$name`"), ms)
    assertEquals("las duraciones documentadas son todas las de NubiMotion", real.keys, found.keys)
    assertTrue("«quitar animaciones» = snap()", section(5).contains("`snap()`"))
  }

  @Test
  fun `los radios y las medidas de las losas son los de kit json`() {
    val cite = Regex("`kit\\.json ([A-Za-z0-9_.]+)`\\s*\\|\\s*([0-9.]+) dp")
    val found = cite.findAll(section(4)).associate { it.groupValues[1] to it.groupValues[2].toDouble() }
    for ((path, dp) in found) {
      val real = (kitValue(path) as? Number)?.toDouble() ?: error("`kit.json $path` no existe o no es un número")
      assertEquals("`kit.json $path` dice $dp dp en DESIGN.md y el archivo tiene $real", real, dp, 0.001)
    }
    // las que la spec pide fijas
    assertEquals(14.0, found["losas.ch.radio_dp"]!!, 0.0)
    assertEquals(22.0, found["losas.md.radio_dp"]!!, 0.0)
    assertEquals(30.0, found["losas.gr.radio_dp"]!!, 0.0)
    assertEquals(2.0, found["losas.md.presionada.texto_baja_dp"]!!, 0.0)
    assertEquals(48.0, found["discos.cerrar.cuerpo_dp.ancho"]!!, 0.0)
  }

  @Test
  fun `la tabla de Do y Don't trae las doce lineas y las seis frases centinela`() {
    val s = section(8)
    val numbered = s.lines().count { Regex("^\\d+\\. ").containsMatchIn(it) }
    assertTrue("§8 trae $numbered líneas numeradas y deben ser 12", numbered == 12)
    for (frase in listOf("sin estrellas con puntas", "nada solo por color", "sin caras que reaccionen", "sin recuadros para informar", "letra ≥ 16", "toque ≥ 48"))
      assertTrue("§8 ya no dice «$frase»", s.lowercase().contains(frase))
    // las reglas R1-R4 del kit sobre el cielo y la regla «sin pérdida» (A7)
    for (r in listOf("**R1**", "**R2**", "**R3**", "**R4**")) assertTrue("§8 ya no trae $r", s.contains(r))
    assertTrue("§1 habla de la guardia sin pérdida", section(1).contains("Sin culpa ni pérdida") && section(1).contains("NoLossGuard"))
  }

  @Test
  fun `fuera de la linea de prohibidas el documento no usa el vocabulario que no va`() {
    // las raíces de VisibleTextsGuardTest.neverVisible (entren…, cerebro, cognitiv…): solo pueden aparecer en la línea «prohibidas:» de §1
    val roots = listOf("entren", "entrén", "cerebro", "cognitiv")
    val prohibited = section(1).lines().single { it.trimStart().startsWith("- prohibidas:") }
    for (w in listOf("cognitivo", "entrenamiento", "cerebro")) assertTrue("la línea de prohibidas ya no dice «$w»", prohibited.contains(w))
    val found = doc.lines().filter { it != prohibited }.flatMap { l -> roots.filter { it in l.lowercase() }.map { "«$it» en: ${l.take(80)}" } }
    assertTrue("DESIGN.md usa vocabulario que no va fuera de la línea «prohibidas:»:\n" + found.joinToString("\n"), found.isEmpty())
  }

  @Test
  fun `la seccion web trae los tokens de la direccion B`() {
    val web = section(12)
    for (t in listOf("--navy", "--cream", "--ink", "--sun", "--lilac", "#F5EBDB", "#0E0D33", "cuerpo ≥ 18 px", "una idea", "churro sol", "botón único")) assertTrue("§12 ya no dice «$t»", web.lowercase().contains(t.lowercase()))
    // los tokens web que son los de la app tienen el mismo valor
    assertEquals(hex(BoardNavy), "#0E0D33")
    assertEquals(hex(Clay.Ink), "#1A1240")
    assertEquals(hex(Clay.Sun), "#FFC93C")
    assertEquals(hex(Clay.Grape), "#B8A4FF")
    assertEquals("#F5EBDB", (kitValue("colores.crema") as String).uppercase())
  }
}
