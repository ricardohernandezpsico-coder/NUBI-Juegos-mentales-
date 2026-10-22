package com.example

import android.app.Dialog
import android.os.Build
import androidx.test.core.app.ApplicationProvider
import com.example.ui.screens.makeDialogWindowEdgeToEdge
import org.junit.Assert.assertEquals
import org.junit.Assert.assertTrue
import org.junit.Test
import org.junit.runner.RunWith
import org.robolectric.RobolectricTestRunner
import org.robolectric.annotation.Config
import java.io.File

/**
 * Tarea 81 (11-oct): el botón «Jugar» de la ficha quedaba BAJO la barra de navegación en el Motorola (Android 16, 3 botones). Causa: con `usePlatformDefaultWidth = false` Compose mide el contenido del diálogo para TODA la pantalla, pero desde Android 15
 * el sistema ajusta el marco de la ventana del diálogo DENTRO de las barras (`fitInsetsTypes`); la tarjeta se desbordaba y la barra la tapaba. Las capturas de Roborazzi no lo ven porque no simulan los insets reales de las barras: se comprobó en el emulador
 * (API 36, 3 botones, gestos y letra grande). Estas pruebas fijan lo que sí se puede fijar sin un teléfono: la ventana queda borde a borde y la ficha la pide.
 */
@RunWith(RobolectricTestRunner::class)
@Config(sdk = [36])
class GameSheetEdgeToEdgeTest {
  @Test
  fun la_ventana_del_dialogo_queda_borde_a_borde_sin_ajustarse_a_las_barras() {
    val dialog = Dialog(ApplicationProvider.getApplicationContext())
    val window = dialog.window!!
    assertTrue("el sistema, por defecto, ajusta la ventana a las barras", Build.VERSION.SDK_INT < 30 || window.attributes.fitInsetsTypes != 0)
    makeDialogWindowEdgeToEdge(window)
    if (Build.VERSION.SDK_INT >= 30) assertEquals("sin ajuste a las barras: los insets llegan al contenido", 0, window.attributes.fitInsetsTypes)
    makeDialogWindowEdgeToEdge(window)   // se puede repetir (corre en cada recomposición) sin cambiar nada
    if (Build.VERSION.SDK_INT >= 30) assertEquals(0, window.attributes.fitInsetsTypes)
  }

  @Test
  fun la_ficha_pide_su_ventana_borde_a_borde_y_deja_la_tarjeta_dentro_de_las_barras() {
    val source = File("src/main/java/com/example/ui/screens/GameSheet.kt").readText()
    val sheet = source.substring(source.indexOf("internal fun GameSheet("), source.indexOf("internal fun GameSheetContent("))
    assertTrue("GameSheet debe volver borde a borde la ventana de su diálogo", sheet.contains("makeDialogWindowEdgeToEdge"))
    assertTrue("GameSheet debe pedir decorFitsSystemWindows = false", sheet.contains("decorFitsSystemWindows = false"))
    assertTrue("y la tarjeta vive dentro de las barras", sheet.contains("windowInsetsPadding(WindowInsets.safeDrawing)"))
  }
}
