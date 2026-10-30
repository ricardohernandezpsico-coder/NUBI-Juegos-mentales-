package com.example.bridge

import java.io.File
import org.junit.Assert.assertFalse
import org.junit.Assert.assertTrue
import org.junit.Test

/**
 * La pantalla de Unity se declara CON aceleración de dibujo (Tarea 190d, `docs/entrada-al-juego.md` §7): el export de Unity la trae en `false` y la carga de la app (Compose) se agrega a esa misma ventana,
 * así que sin esta línea se dibuja con el procesador, un cuadro cada 150 ms (la luna «pegada»). Debe valer igual en depuración y en release: por eso ya no hay un manifiesto solo de depuración.
 */
class AceleracionDeDibujoTest {
  private val manifest = File("src/main/AndroidManifest.xml").readText()

  private fun unityActivity(): String {
    val start = manifest.indexOf("android:name=\"com.unity3d.player.UnityPlayerGameActivity\"")
    assertTrue("el manifiesto declara la pantalla de Unity", start >= 0)
    val end = manifest.indexOf("</activity>", start)
    return manifest.substring(start, end)
  }

  @Test
  fun `la pantalla de Unity lleva la aceleracion de dibujo y pisa la del export`() {
    val activity = unityActivity()
    assertTrue("hardwareAccelerated en true", activity.contains("android:hardwareAccelerated=\"true\""))
    val replace = Regex("tools:replace=\"([^\"]*)\"").find(activity)?.groupValues?.get(1) ?: ""
    assertTrue("tools:replace pisa hardwareAccelerated, que el export trae en false: $replace", replace.split(",").map { it.trim() }.contains("android:hardwareAccelerated"))
  }

  @Test
  fun `no hay un manifiesto de depuracion que cambie la bandera solo en depuracion`() {
    assertFalse("el experimento de la T190c ya no existe: la bandera vale igual en depuración y en release", File("src/debug/AndroidManifest.xml").exists())
  }
}
