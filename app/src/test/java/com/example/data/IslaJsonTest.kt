package com.example.data

import org.junit.Assert.assertEquals
import org.junit.Assert.assertTrue
import org.junit.Test
import org.junit.runner.RunWith
import org.robolectric.RobolectricTestRunner
import org.robolectric.annotation.Config
import java.io.File
import kotlin.math.abs
import kotlin.math.hypot

/**
 * `res/raw/isla.json` (las coordenadas de la isla renderizada en Blender, `docs/diseno-isla.md` §7 y §13.3): sus anclas caen dentro de la imagen y no se pisan, y el JSON y las imágenes son del mismo render.
 * Si alguien regenera la isla con otra cámara (`tools/isla/`), esta prueba falla antes de llegar al teléfono.
 */
@RunWith(RobolectricTestRunner::class)
@Config(sdk = [36])
class IslaJsonTest {
  private val layout = IslaLayout.parse(File("src/main/res/raw/isla.json").readText())

  private fun dist(a: IslaPoint, b: IslaPoint) = hypot((a.x - b.x).toDouble(), (a.y - b.y).toDouble()).toFloat()
  private fun inside(p: IslaPoint) = p.x in 0f..1f && p.y in 0f..1f

  /** El tamaño en píxeles de un WebP con cabecera extendida (VP8X): lo que lee el instalador de Android sin decodificarlo. */
  private fun webpSize(path: String): Pair<Int, Int> {
    val b = File(path).readBytes()
    assertEquals("RIFF", String(b, 0, 4))
    assertEquals("WEBP", String(b, 8, 4))
    assertEquals("VP8X", String(b, 12, 4))
    fun u24(o: Int) = (b[o].toInt() and 0xFF) or ((b[o + 1].toInt() and 0xFF) shl 8) or ((b[o + 2].toInt() and 0xFF) shl 16)
    return (1 + u24(24)) to (1 + u24(27))
  }

  @Test
  fun los_cuatro_lugares_estan_en_su_orden_y_cada_uno_en_su_area() {
    assertEquals(listOf("observatorio", "antena", "biblioteca", "hangar"), layout.places.map { it.key })
    assertEquals(listOf("MEMORIA", "ATENCION", "LENGUAJE", "RAZONAMIENTO"), layout.places.map { it.area })
    for (area in listOf("MEMORIA", "ATENCION", "RAZONAMIENTO", "LENGUAJE")) assertTrue(area, IslaLugar.nameOfArea(area) != null)
    assertEquals("Hangar, Razonamiento", IslaLugar.spoken("RAZONAMIENTO"))
    assertEquals("la Antena", IslaLugar.withArticle("ATENCION"))
    assertEquals("el Observatorio", IslaLugar.withArticle("MEMORIA"))
  }

  @Test
  fun las_anclas_las_lamparas_y_la_sombra_caen_dentro_de_la_imagen() {
    for (p in layout.places) for (pt in listOf(p.foot, p.center, p.top)) assertTrue("${p.key} $pt", inside(pt))
    assertEquals(4, layout.lamps.size)
    for (l in layout.lamps) assertTrue("lámpara $l", inside(l))
    assertTrue("sombra de Nubi", inside(layout.shadow.center))
    assertTrue(layout.shadow.width in 0.1f..1f && layout.shadow.height in 0.05f..1f && layout.shadow.opacity in 0.05f..1f)
    assertTrue(inside(layout.nubiTopLeft) && inside(layout.nubiBottomRight))
    assertTrue("la caja de Nubi tiene alto y ancho", layout.nubiBottomRight.x > layout.nubiTopLeft.x && layout.nubiBottomRight.y > layout.nubiTopLeft.y)
    for (c in layout.antennaHalo.centers + layout.lampHalos.centers) assertTrue("halo $c", inside(c))
    assertEquals("un halo por farola", layout.lamps.size, layout.lampHalos.centers.size)
  }

  @Test
  fun los_edificios_las_lamparas_y_la_sombra_estan_separados() {
    // Los toques de los edificios no se pisan: más de 0,12 del lado de la imagen entre centros.
    for (i in layout.places.indices) for (j in i + 1 until layout.places.size) {
      assertTrue("${layout.places[i].key} y ${layout.places[j].key}", dist(layout.places[i].center, layout.places[j].center) >= 0.12f)
    }
    // Las cuatro farolas del rombo están a 0,10 como mínimo (el plan decía 0,12, pero las dos de la izquierda y de la derecha quedan a 0,1016: es la geometría real de la ruta).
    for (i in layout.lamps.indices) for (j in i + 1 until layout.lamps.size) assertTrue("farolas $i y $j", dist(layout.lamps[i], layout.lamps[j]) >= 0.10f)
    // La sombra de Nubi cae en la losa, lejos de los edificios (no tapa un toque).
    for (p in layout.places) assertTrue("sombra y ${p.key}", dist(layout.shadow.center, p.center) >= 0.12f)
    // Y la lámpara de la torre (el halo grande) está en la torre, no en otro edificio.
    val antena = layout.place("antena")
    assertTrue(dist(layout.antennaHalo.centers.single(), antena.top) < 0.1f)
  }

  @Test
  fun el_json_y_las_imagenes_son_del_mismo_render() {
    val (w2, h2) = webpSize("src/main/res/drawable-nodpi/isla_base.webp")
    assertEquals(layout.widthPx to layout.heightPx, w2 to h2)
    assertEquals("isla_edificios mide lo mismo que isla_base", w2 to h2, webpSize("src/main/res/drawable-nodpi/isla_edificios.webp"))
    assertEquals("Nubi", layout.nubiWidthPx to layout.nubiHeightPx, webpSize("src/main/res/drawable-nodpi/isla_nubi_hola.webp"))
    // La caja de Nubi en el JSON tiene la misma proporción que su imagen.
    val boxRatio = (layout.nubiBottomRight.x - layout.nubiTopLeft.x) * layout.widthPx / ((layout.nubiBottomRight.y - layout.nubiTopLeft.y) * layout.heightPx)
    val imgRatio = layout.nubiWidthPx.toFloat() / layout.nubiHeightPx
    assertTrue("caja de Nubi $boxRatio contra imagen $imgRatio", abs(boxRatio - imgRatio) / imgRatio < 0.01f)
    assertEquals("proporción alto/ancho del recorte", 1.1204f, layout.heightPx.toFloat() / layout.widthPx, 0.001f)
  }
}
