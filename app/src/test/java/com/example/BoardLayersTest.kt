package com.example

import org.junit.Assert.assertFalse
import org.junit.Assert.assertTrue
import org.junit.Test
import java.io.File

/**
 * Los luceros del fondo van SIEMPRE detrás del contenido y los textos y tarjetas los tapan (Tarea 123; Fable lo vio en Avance y en Licencias). Se mira el código:
 * - el fondo es la PRIMERA capa de `NeuroVidaApp`, antes de las pestañas, la barra y todo lo demás;
 * - la barra de pestañas es una capa fija encima de las páginas (no el `bottomBar` de un `Scaffold`) y ya no hay desvanecido de página;
 * - los textos que van directo sobre el fondo, sin tarjeta (la cabecera de los paneles, el nombre del perfil; Licencias y créditos ya NO: desde la Tarea 145 va sobre una losa crema; el título grande de Ajustes se quitó en la Tarea 143) usan `OnBoardText`, que les pinta una orilla densa del color de la lámina.
 */
class BoardLayersTest {
  /** El código sin los comentarios (los comentarios hablan del Scaffold y del desvanecido que se quitaron). */
  private fun src(path: String) = File("src/main/java/com/example/$path").readLines().filterNot { it.trimStart().startsWith("//") || it.trimStart().startsWith("*") || it.trimStart().startsWith("/*") }.joinToString("\n")

  @Test
  fun el_fondo_es_la_primera_capa_y_la_barra_va_encima_de_las_paginas() {
    val main = src("MainActivity.kt")
    val bg = main.indexOf("BoardBackground()")
    val pager = main.indexOf("HorizontalPager(")
    val bar = main.indexOf("PagerNavBar(")                       // Tarea 142: la barra se pone con `PagerNavBar` (lee el pager en vivo) y esta envuelve a `NeuroNavBar`
    assertTrue("el fondo está en MainActivity", bg > 0 && pager > 0 && bar > 0)
    assertTrue("PagerNavBar dibuja la barra de plasticina", src("ui/components/TabPager.kt").contains("NeuroNavBar(current = "))
    assertTrue("el fondo se compone ANTES que las páginas (queda detrás de todo)", bg < pager)
    assertTrue("la barra se compone DESPUÉS de las páginas (capa fija encima)", pager < bar)
    assertFalse("la barra ya no es el bottomBar de un Scaffold", main.contains("Scaffold(") || main.contains("bottomBar"))
    assertFalse("ya no hay desvanecido de página", main.contains("PAGE_BOTTOM_FADE") || main.contains("DstIn"))
    assertTrue("las páginas dejan la franja de la barra con LocalBarInset", src("ui/screens/HomeScreen.kt").contains("LocalBarInset") && src("ui/screens/ProfileScreen.kt").contains("LocalBarInset") && src("ui/screens/ProgressScreen.kt").contains("LocalBarInset"))
  }

  @Test
  fun el_resumen_de_la_ruta_escribe_sobre_el_cielo_con_OnBoardText_y_solo_la_losa_usa_Text() {
    // Tarea 198: «¡Ruta de hoy completa!» (el título, la frase y el pie van directo sobre el cielo: con OnBoardText, la orilla del tono de la lámina; ningún `Text` a secas, que dejaba ver los luceros entre las letras) y UNA losa crema donde la letra va en tinta con `Text`
    val summary = src("ui/screens/SessionSummaryScreen.kt")
    for (sky in listOf("OnBoardText(\"¡Ruta de hoy completa!\"", "OnBoardText(\n        \"Trabajaste \"", "OnBoardText(\n        \"Nubi te espera mañana para seguir\"")) assertTrue("el texto sobre el cielo usa OnBoardText: $sky", summary.contains(sky))
    val start = summary.indexOf("ClayCard(Modifier.fillMaxWidth().padding(top = 16.dp).testTag(\"summary_card\")")
    val end = summary.indexOf("HoyWeek(lamps")
    assertTrue("la losa está en el código", start > 0 && end > start)
    val outside = summary.substring(0, start) + summary.substring(end, summary.indexOf("private fun SectionTitle"))          // lo de sobre el cielo: antes de la losa y después de ella (sin el título de sección, que vive dentro de la losa)
    assertFalse("fuera de la losa no hay ningún Text a secas sobre el cielo", Regex("""(^|[^A-Za-z.])Text\(""", RegexOption.MULTILINE).containsMatchIn(outside.replace("OnBoardText(", "OnBoard(")))
    assertTrue("dentro de la losa sí: la letra va en tinta sobre crema", summary.substring(start, end).contains("Text(") && summary.substring(start, end).contains("Clay.Ink"))
  }

  @Test
  fun los_textos_sueltos_sobre_el_fondo_usan_OnBoardText() {
    val plainText = Regex("""(^|[^A-Za-z.])Text\(""", RegexOption.MULTILINE)
    // Tarea 145 (ítem 10): Licencias y créditos va sobre UNA losa crema (ClayPanel) con la letra en tinta, no directo sobre el cielo: sus textos son `Text` normales dentro de la losa y ninguno es OnBoardText
    val licenses = src("ui/screens/LicensesScreen.kt")
    assertTrue("el texto largo de Licencias y créditos va dentro de un ClayPanel", licenses.contains("ClayPanel("))
    assertFalse("y ya no hay OnBoardText suelto sobre el cielo en Licencias", licenses.contains("OnBoardText("))
    val profile = src("ui/screens/ProfileScreen.kt")
    assertTrue("el título de los paneles (y el nombre del perfil es texto grande con OnBoardText, con su botón «Cambiar»)", profile.contains("OnBoardText(title,") && profile.contains("testTag(\"profile_name\")"))
    // Tarea 143 (A3): Ajustes ya no trae su título grande suelto; la cabecera del panel («Ajustes») es el `PanelHeader` de arriba, que ya es OnBoardText
    assertFalse("Ajustes ya no tiene un segundo título suelto sobre el fondo", src("ui/screens/SettingsScreen.kt").contains("strings.settingsTitle"))
  }
}
