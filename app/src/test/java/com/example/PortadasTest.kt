package com.example

import android.graphics.BitmapFactory
import com.example.model.GameRegistry
import com.example.ui.components.COVER_GAME_IDS
import com.example.ui.components.coverRes
import org.junit.Assert.assertEquals
import org.junit.Assert.assertNotNull
import org.junit.Assert.assertNull
import org.junit.Assert.assertTrue
import org.junit.Test
import org.junit.runner.RunWith
import org.robolectric.RobolectricTestRunner
import org.robolectric.annotation.Config
import org.robolectric.annotation.GraphicsMode
import java.io.File

/**
 * Las 17 portadas de la ficha (Tarea 171, 25-oct; `tools/previews/portadas.py`): una por juego de la app, recortada de una captura real de Unity, de 1200 × 626 px (la proporción de la ventanilla de 345 × 180 dp: se ve entera a 180 dp y a 200 dp se recorta un 5 % a cada lado), en webp y ligera (el APK ya pesa más de 170 MB: las portadas suman unos cientos de KB).
 * Sin portada una ficha mostraría solo el cielo; con una de otro tamaño, la ventanilla se vería estirada o recortada de más.
 */
@RunWith(RobolectricTestRunner::class)
@GraphicsMode(GraphicsMode.Mode.NATIVE)
@Config(sdk = [36])
class PortadasTest {
  private val dir = File("src/main/res/drawable-nodpi")

  @Test
  fun los_17_juegos_de_la_app_tienen_su_portada_y_los_retirados_no() {
    assertEquals("los 17 juegos de la app", 17, GameRegistry.allGames.size)
    for (g in GameRegistry.allGames) assertNotNull("${g.id}: sin portada", coverRes(g.id))
    assertEquals(GameRegistry.allGames.map { it.id }.toSet(), COVER_GAME_IDS)
    for (retired in listOf("bitacora", "rumbo", "cosecha")) assertNull("$retired está retirado y no lleva portada", coverRes(retired))
  }

  @Test
  fun cada_portada_mide_1200_por_626_es_webp_y_pesa_poco() {
    var total = 0L
    for (g in GameRegistry.allGames) {
      val f = File(dir, "portada_${g.id}.webp")
      assertTrue("${g.id}: falta ${f.path}", f.exists())
      val bytes = f.readBytes()
      total += bytes.size
      assertTrue("${g.id}: no es webp (RIFF…WEBP)", String(bytes, 0, 4) == "RIFF" && String(bytes, 8, 4) == "WEBP")
      val o = BitmapFactory.Options().apply { inJustDecodeBounds = true }
      BitmapFactory.decodeByteArray(bytes, 0, bytes.size, o)
      assertEquals("${g.id}: ancho", 1200, o.outWidth)
      assertEquals("${g.id}: alto", 626, o.outHeight)
      assertTrue("${g.id}: pesa ${bytes.size / 1024} KB (máximo 250 KB)", bytes.size <= 250 * 1024)
    }
    assertTrue("las 17 portadas pesan ${total / 1024} KB (máximo 2 MB)", total <= 2L * 1024 * 1024)
  }

  @Test
  fun el_script_que_las_hace_tiene_una_entrada_por_juego() {
    val script = File("../tools/previews/portadas.py").readText()
    for (g in GameRegistry.allGames) assertTrue("tools/previews/portadas.py no tiene una entrada para ${g.id}", Regex("""'${g.id}':\s+dict\(""").containsMatchIn(script))
  }
}
