package com.example

import com.example.data.KitAvanceSpec
import com.example.model.GameRegistry
import com.example.model.RankTier
import org.junit.Assert.assertEquals
import org.junit.Assert.assertNotNull
import org.junit.Assert.assertTrue
import org.junit.Test
import org.junit.runner.RunWith
import org.robolectric.RobolectricTestRunner
import org.robolectric.annotation.Config
import java.io.File

/**
 * Las piezas del kit para Avance y Juegos (Tareas 124 y 125): `res/raw/kit_avance.json` y los WebP de `drawable-nodpi` son del mismo render (`tools/isla/kit_avance_app.py`), no sobra ni falta ninguna, y cada pieza trae lo que Compose necesita
 * (insets en el canal y el churro, la placa en las medallas, el pie en los edificios).
 */
@RunWith(RobolectricTestRunner::class)
@Config(sdk = [36])
class AvanceKitTest {
  private val spec = KitAvanceSpec.parse(File("src/main/res/raw/kit_avance.json").readText())

  private fun webpSize(path: String): Pair<Int, Int> {
    val b = File(path).readBytes()
    assertEquals("RIFF", String(b, 0, 4)); assertEquals("WEBP", String(b, 8, 4)); assertEquals("VP8X", String(b, 12, 4))
    fun u24(o: Int) = (b[o].toInt() and 0xFF) or ((b[o + 1].toInt() and 0xFF) shl 8) or ((b[o + 2].toInt() and 0xFF) shl 16)
    return (1 + u24(24)) to (1 + u24(27))
  }

  private val games = GameRegistry.allGames.filter { !GameRegistry.isRetired(it.id) }.map { it.id }
  private val areas = listOf("memoria", "atencion", "razonamiento", "lenguaje")
  private val families = listOf("constancia", "variedad", "volver")

  private val expected: Set<String> = buildSet {
    addAll(listOf("canal_crema", "canal_cielo", "churro_sol", "churro_lima", "bolita", "farola_off", "farola_on", "pedestal", "cola_izq", "cola_der"))
    games.forEach { add("emb_$it") }
    families.forEach { add("med_$it"); add("med_${it}_off") }
    RankTier.entries.forEach { add("escudo_" + it.name.lowercase()) }
    areas.forEach { add("edif_$it") }
  }

  @Test
  fun `el JSON y los WebP son del mismo render y no sobra ni falta ninguna pieza`() {
    assertEquals(3f, spec.pxPerDp, 0f)
    assertEquals(expected, spec.pieces.keys)
    for ((name, p) in spec.pieces) assertEquals("kit_$name", p.widthPx to p.heightPx, webpSize("src/main/res/drawable-nodpi/kit_$name.webp"))
    val files = File("src/main/res/drawable-nodpi").listFiles { f -> f.name.startsWith("kit_") && f.name.endsWith(".webp") }!!.map { it.nameWithoutExtension.removePrefix("kit_") }.toSet()
    assertTrue("WebP de las piezas nuevas sin JSON: ${spec.pieces.keys.filter { it !in files }}", files.containsAll(spec.pieces.keys))
  }

  @Test
  fun `hay un emblema por cada juego de la app y 17 en total`() {
    assertEquals(17, games.size)
    for (g in games) assertNotNull("emblema de $g", spec.pieces["emb_$g"])
    for (g in games) {
      val b = spec.piece("emb_$g")
      assertEquals("la ficha mide 64 dp de ancho: $g", 64.7f, b.bodyWidth, 1.5f)
      assertTrue("la ficha es casi cuadrada: $g", b.bodyHeight in 58f..66f)
    }
  }

  @Test
  fun `el canal y el churro estiran en horizontal con tapas de las mismas medidas`() {
    for (k in listOf("canal_crema", "canal_cielo", "churro_sol", "churro_lima")) {
      val p = spec.piece(k)
      assertTrue("$k es una tira", p.isStrip)
      assertTrue("$k: las tapas dejan franja central", p.insetLeftPx + p.insetRightPx <= p.widthPx - 12)
      assertEquals("$k: tapas parejas", p.insetLeftPx, p.insetRightPx)
    }
    assertEquals("el canal mide 12 dp de alto", 12.3f, spec.piece("canal_crema").bodyHeight, 1f)
    assertEquals("el churro mide 8 dp de alto (y su sombra)", 9f, spec.piece("churro_sol").bodyHeight, 1.5f)
    assertEquals("la bolita mide 10 dp", 10f, spec.piece("bolita").bodyWidth, 0.5f)
  }

  @Test
  fun `las medallas tienen su placa para el numero y los edificios su pie`() {
    for (f in families) for (k in listOf("med_$f", "med_${f}_off")) {
      val plate = spec.piece(k).plate
      assertNotNull("placa de $k", plate)
      assertEquals(52f, plate!!.width, 0.1f); assertEquals(16f, plate.height, 0.1f)
    }
    for (a in areas) {
      val b = spec.piece("edif_$a")
      assertTrue("el pie de $a cae dentro de la imagen", b.footX > 0f && b.footX < b.widthPx / 3f && b.footY > 0f && b.footY < b.heightPx / 3f)
    }
  }

  @Test
  fun `hay un escudo por liga y un pedestal de 150 dp`() {
    assertEquals(7, RankTier.entries.size)
    assertEquals(150f, spec.piece("pedestal").bodyWidth, 3f)
    for (t in RankTier.entries) assertEquals("escudo ${t.name}", 108f, spec.piece("escudo_" + t.name.lowercase()).bodyWidth, 1f)
  }
}
