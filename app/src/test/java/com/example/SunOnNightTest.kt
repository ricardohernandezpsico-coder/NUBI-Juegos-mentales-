package com.example

import androidx.compose.ui.graphics.Color
import androidx.compose.ui.graphics.luminance
import com.example.ui.theme.NubiTokens
import org.junit.Assert.assertEquals
import org.junit.Assert.assertFalse
import org.junit.Assert.assertTrue
import org.junit.Test

/**
 * El amarillo del TEXTO sobre el cielo (Tarea 194; Ricardo en el teléfono: «las letras amarillas se ven saturadas y no se pueden leer» en «¿Sabías que…?»): el sol de los botones (`Clay.Sun`) vibra como letra fina sobre el azul aunque su contraste numérico sea alto. El texto amarillo del Primer vuelo
 * usa [NubiTokens.SunOnNight] (mantequilla, `#FFE6A6`), con un contraste de 7:1 o más contra los tres tonos del cielo.
 */
class SunOnNightTest {
  /** Contraste WCAG entre dos colores opacos. */
  private fun contrast(a: Color, b: Color): Double {
    val la = a.luminance().toDouble(); val lb = b.luminance().toDouble()
    return (maxOf(la, lb) + 0.05) / (minOf(la, lb) + 0.05)
  }

  /** Los tres tonos del cielo (los mismos del cielo de los juegos, de la carga y del Primer vuelo) y el fondo de la app (`BoardNavy`). */
  private val sky = mapOf("arriba #07071F" to Color(0xFF07071F), "medio #100F3A" to Color(0xFF100F3A), "abajo #1C1B52" to Color(0xFF1C1B52), "fondo de la app #0E0D33" to Color(0xFF0E0D33))

  @Test
  fun el_amarillo_claro_pasa_de_7_a_1_contra_los_tres_tonos_del_cielo() {
    assertEquals(Color(0xFFFFE6A6), NubiTokens.SunOnNight)
    for ((name, bg) in sky) {
      val c = contrast(NubiTokens.SunOnNight, bg)
      println("SunOnNight sobre %s: %.1f:1 (el sol de los botones: %.1f:1)".format(name, c, contrast(Color(0xFFFFC93C), bg)))
      assertTrue("el amarillo claro sobre $name mide %.1f:1 (mínimo 7:1)".format(c), c >= 7.0)
    }
  }

  @Test
  fun es_mas_claro_y_menos_saturado_que_el_sol_de_los_botones() {
    val sun = Color(0xFFFFC93C)
    fun saturation(c: Color): Float { val mx = maxOf(c.red, c.green, c.blue); val mn = minOf(c.red, c.green, c.blue); return if (mx == 0f) 0f else (mx - mn) / mx }
    assertTrue("más claro", NubiTokens.SunOnNight.luminance() > sun.luminance())
    assertTrue("menos saturado: %.2f contra %.2f".format(saturation(NubiTokens.SunOnNight), saturation(sun)), saturation(NubiTokens.SunOnNight) < saturation(sun) - 0.2f)
  }

  /** Ninguna página del vuelo queda con el amarillo viejo en una LETRA: el `Eyebrow` y el resaltado de `Say` (y «¡Nace tu primer lucero!») usan el token; `Clay.Sun` solo queda en lo que se DIBUJA (el lucero). */
  @Test
  fun el_primer_vuelo_no_pide_el_sol_para_ninguna_letra() {
    val src = java.io.File("src/main/java/com/example/ui/screens/FirstFlightScreen.kt").readText().replace("\r\n", "\n")
    val code = src.lines().filterNot { it.trim().startsWith("*") || it.trim().startsWith("/**") || it.trim().startsWith("//") }
    val sunLines = code.filter { it.contains("Clay.Sun") }
    for (line in sunLines) assertTrue("`Clay.Sun` en una letra del vuelo: ${line.trim()}", line.contains("drawCircle"))
    assertTrue("el Eyebrow usa el amarillo claro", code.any { it.contains("color = com.example.ui.theme.NubiTokens.SunOnNight") && it.contains("fontSize = 18.sp") })
    assertTrue("el resaltado de Say también", code.any { it.contains("SpanStyle(color = com.example.ui.theme.NubiTokens.SunOnNight)") })
    assertFalse(code.any { it.contains("SpanStyle(color = Clay.Sun)") })
  }

  /**
   * Decisión de Ricardo (30-oct): el amarillo suave va en TODAS las letras amarillas directo sobre el cielo de la app, no solo en el vuelo. En el código de la app (`src/main`), `Clay.Sun` solo puede quedar en lo que se DIBUJA (medallas, monedas, íconos, barras, luceros), en los botones y losas
   * sol (los colores de losa) y en la tarjeta para compartir (`ShareCard`, una imagen aparte). Cualquier otra línea con `Clay.Sun` es una LETRA sobre el cielo que volvió al sol saturado: la prueba falla y dice cuál.
   */
  @Test
  fun ninguna_letra_sobre_el_cielo_pide_el_sol_saturado_en_toda_la_app() {
    val drawn = Regex("""drawCircle|drawPath|drawRect|drawRoundRect|drawLine|drawArc|background\(|clay\(|shape\(|sparkle\(|curvedArrow|poly\(|Brush\.|sparkColors|ModeBars|ClayPill|SlabColor|Clay\.Sun ->|-> Clay\.Sun|confirmColor|color = if \(baseline|tertiary""")
    val offenders = mutableListOf<String>()
    var checked = 0
    java.io.File("src/main/java").walkTopDown().filter { it.isFile && it.extension == "kt" }.forEach { f ->
      if (f.name == "Clay.kt" || f.name == "ShareCard.kt") return@forEach           // la definición del color y la tarjeta para compartir (una imagen aparte)
      f.readLines().forEachIndexed { i, raw ->
        val line = raw.trim()
        if (line.startsWith("*") || line.startsWith("/**") || line.startsWith("//") || !line.contains("Clay.Sun")) return@forEachIndexed
        checked++
        if (!drawn.containsMatchIn(line)) offenders += "${f.name}:${i + 1}: $line"
      }
    }
    assertTrue("se revisaron $checked usos de Clay.Sun (dibujos y losas)", checked > 20)
    assertTrue("LETRAS sobre el cielo que piden Clay.Sun en vez de NubiTokens.SunOnNight:" + offenders.joinToString(prefix = " ", separator = " | "), offenders.isEmpty())
  }

  @Test
  fun el_amarillo_suave_esta_en_las_letras_de_la_lista_de_Ricardo() {
    fun uses(rel: String) = java.io.File("src/main/java/com/example/$rel").readText().split("NubiTokens.SunOnNight").size - 1
    assertTrue("«Medalla nueva» y el nombre de la medalla", uses("ui/components/AchievementOverlay.kt") >= 2)
    assertTrue("«Tu colección», «Más seguidos» y la línea con el visto", uses("games/GameResultScreen.kt") >= 3)
    assertTrue("el detalle del final: líneas con «¡» y sus títulos", uses("ui/components/MeaningfulResult.kt") >= 2)
    assertTrue("el final del Contrarreloj: los números y «hoy»", uses("games/TimeTrialResultScreen.kt") >= 2)
    assertTrue("el resumen del día", uses("ui/screens/SessionSummaryScreen.kt") >= 3)
    assertTrue("la carga del juego", uses("ui/components/GameLoadingScreen.kt") >= 1)
    assertTrue("el ascenso de liga", uses("ui/components/LeaguePromotionOverlay.kt") >= 1)
  }
}
