package com.example

import com.example.ui.components.NUBI_ASPECT
import org.junit.Assert.assertEquals
import org.junit.Assert.assertFalse
import org.junit.Assert.assertTrue
import org.junit.Test
import java.io.File

/**
 * La Nubi 3D (Tarea 92): el Nubi plano de antes (`nubi_hola`, `nubi_mira`, `nubi_celebra` y `nubi_cientifica`, con destellos de cruz; la científica con bata) ya no existe ni se menciona en ninguna parte; el único Nubi de las pantallas es `nubi_3d.webp`
 * (el render de `isla.py --solo-nubi`, 720 × 476 px) y el ícono, sus capas y el ícono de notificación salen de ese mismo render (`tools/isla/icono.py`) en todas las densidades. Y el APK no lleva arte sin uso: cada imagen de `drawable-nodpi` se usa desde el código, un XML o un JSON de `res/raw`.
 */
class NubiAssetsTest {
  private val res = File("src/main/res")
  private val old = listOf("nubi_hola", "nubi_mira", "nubi_celebra", "nubi_cientifica")

  /** Arte de `drawable-nodpi` que se deja aunque nadie lo nombre: NINGUNO. Si hace falta una excepción, se anota acá con su razón. */
  private val UNUSED_ALLOWED: Set<String> = emptySet()

  private fun code(): List<Pair<String, String>> = File("src/main/java").walkTopDown().filter { it.isFile && it.extension == "kt" }.map { it.path.replace('\\', '/') to it.readText() }.toList()
  private fun xmls(): List<Pair<String, String>> = res.walkTopDown().filter { it.isFile && it.extension == "xml" }.map { it.path.replace('\\', '/') to it.readText() }.toList() + File("src/main/AndroidManifest.xml").let { listOf(it.path to it.readText()) }

  /** Ancho y alto de un PNG (cabecera IHDR) o de un WebP (VP8X, VP8 o VP8L), sin decodificarlo. */
  private fun imageSize(f: File): Pair<Int, Int> {
    val b = f.readBytes()
    if (f.extension == "png") {
      fun i32(o: Int) = ((b[o].toInt() and 0xFF) shl 24) or ((b[o + 1].toInt() and 0xFF) shl 16) or ((b[o + 2].toInt() and 0xFF) shl 8) or (b[o + 3].toInt() and 0xFF)
      return i32(16) to i32(20)
    }
    assertEquals("RIFF", String(b, 0, 4)); assertEquals("WEBP", String(b, 8, 4))
    fun le24(o: Int) = (b[o].toInt() and 0xFF) or ((b[o + 1].toInt() and 0xFF) shl 8) or ((b[o + 2].toInt() and 0xFF) shl 16)
    return when (String(b, 12, 4)) {
      "VP8X" -> (1 + le24(24)) to (1 + le24(27))
      "VP8L" -> { val bits = (b[21].toInt() and 0xFF) or ((b[22].toInt() and 0xFF) shl 8) or ((b[23].toInt() and 0xFF) shl 16) or ((b[24].toInt() and 0xFF) shl 24); (1 + (bits and 0x3FFF)) to (1 + ((bits shr 14) and 0x3FFF)) }
      else -> (le24(26) and 0x3FFF) to (le24(28) and 0x3FFF)
    }
  }

  @Test
  fun `el Nubi plano ya no existe en los recursos`() {
    val found = res.walkTopDown().filter { f -> f.isFile && old.any { f.nameWithoutExtension == it } }.map { it.path }.toList()
    assertTrue("Quedan archivos del Nubi plano: $found", found.isEmpty())
  }

  @Test
  fun `nada referencia al Nubi plano ni a sus poses ni a su halo`() {
    val re = Regex("(R\\.drawable\\.|@drawable/)(${old.joinToString("|")})\\b")
    val words = listOf("NubiPose", "NubiWithHalo", "NUBI_BODY_FRACTION")
    val hits = (code() + xmls()).flatMap { (f, t) -> re.findAll(t).map { "$f: ${it.value}" }.toList() + words.filter { it in t }.map { "$f: $it" } }
    assertTrue("Referencias al Nubi plano:\n" + hits.joinToString("\n"), hits.isEmpty())
  }

  @Test
  fun `nubi_3d mide 720 por 476 y la relacion que usa NubiClay es la suya`() {
    val f = File(res, "drawable-nodpi/nubi_3d.webp")
    assertTrue(f.exists())
    val (w, h) = imageSize(f)
    assertEquals(720, w); assertEquals(476, h)
    assertEquals(w.toFloat() / h, NUBI_ASPECT, 0.001f)
    assertTrue("pesa poco (${f.length() / 1024} KB)", f.length() < 150 * 1024)
  }

  @Test
  fun `el icono y sus capas salen en todas las densidades con su tamano`() {
    val dens = listOf("mdpi" to 1f, "hdpi" to 1.5f, "xhdpi" to 2f, "xxhdpi" to 3f, "xxxhdpi" to 4f)
    for ((d, k) in dens) {
      for (n in listOf("ic_launcher_foreground.png", "ic_launcher_monochrome.png")) assertEquals("$d/$n: 108 dp", (108 * k).toInt() to (108 * k).toInt(), imageSize(File(res, "mipmap-$d/$n")))
      for (n in listOf("ic_launcher.webp", "ic_launcher_round.webp")) assertEquals("$d/$n: 48 dp", (48 * k).toInt() to (48 * k).toInt(), imageSize(File(res, "mipmap-$d/$n")))
      assertEquals("$d/ic_stat_neurovida: 24 dp", (24 * k).toInt() to (24 * k).toInt(), imageSize(File(res, "drawable-$d/ic_stat_neurovida.png")))
    }
    for (n in listOf("ic_launcher.xml", "ic_launcher_round.xml")) {
      val xml = File(res, "mipmap-anydpi-v26/$n").readText()
      assertTrue("$n lleva el fondo, la capa de Nubi y la monocroma (ícono temático de Android 13+)", listOf("@drawable/ic_launcher_background", "@mipmap/ic_launcher_foreground", "@mipmap/ic_launcher_monochrome").all { it in xml })
    }
    assertTrue("el fondo es el azul marino de la lámina", File(res, "drawable/ic_launcher_background.xml").readText().contains("#23255A"))
  }

  @Test
  fun `el APK no lleva arte sin uso en drawable-nodpi`() {
    val sources = code() + xmls() + res.resolve("raw").walkTopDown().filter { it.isFile && it.extension == "json" }.map { it.path to it.readText() }.toList()
    val unused = File(res, "drawable-nodpi").listFiles().orEmpty().filter { it.isFile }.map { it.nameWithoutExtension }.filter { it !in UNUSED_ALLOWED }
      .filter { name -> sources.none { (_, t) -> Regex("(R\\.drawable\\.|@drawable/)$name\\b").containsMatchIn(t) || (t.contains("\"$name\"") ) } }
    assertTrue("Arte en drawable-nodpi que nadie usa (borrarlo o anotarlo en UNUSED_ALLOWED):\n" + unused.joinToString("\n"), unused.isEmpty())
    assertFalse(UNUSED_ALLOWED.isNotEmpty())
  }
}
