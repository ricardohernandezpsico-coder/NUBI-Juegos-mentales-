package com.example

import org.junit.Assert.assertEquals
import org.junit.Assert.assertFalse
import org.junit.Assert.assertTrue
import org.junit.Test
import java.io.File

/**
 * Lo innecesario que se quitó (Tarea 152; auditoría de coherencia B5.1, B5.2, B6.1, B6.3, B6.5, B6.6, C2, C3 y C5): se mira el código de las pantallas para que no vuelva.
 * Lo que se ve de cada cosa (Ajustes, Perfil, Avance, la ficha) está en las capturas de `AppActualScreenshotTest` y en la hoja `docs/previews/tarea-152.png`.
 */
class AjustesDepuradosTest {
  private fun src(path: String) = File("src/main/java/com/example/$path").readText()

  @Test
  fun `Ajustes ya no pregunta por los colores, no tiene Tu nombre ni el boton Guardar`() {
    val settings = src("ui/screens/SettingsScreen.kt")
    assertFalse("la pregunta de los colores hacía una promesa que la app no cumplía (B5.1)", settings.contains("distinguir algunos colores") || settings.contains("chip_color_vision"))
    assertFalse("el nombre se edita en el Perfil (B6.5)", settings.contains("\"Tu nombre\"") || settings.contains("btn_save_name") || settings.contains("input_user_name"))
    assertFalse("sin botón «Guardar» (B6.3): todo lo de Ajustes se guarda al tocar", settings.contains("\"Guardar\""))
  }

  @Test
  fun `el nivel de estudios esta en Ajustes con su para que sirve y ya no en el Perfil`() {
    val settings = src("ui/screens/SettingsScreen.kt")
    val profile = src("ui/screens/ProfileScreen.kt")
    assertTrue(settings.contains("\"Tu nivel de estudios\"") && settings.contains("Ayuda a calcular tu punto de partida si saltas la evaluación."))
    assertTrue("va dentro de «Cómo juegas», debajo del rango de edad", settings.indexOf("\"Rango de edad\"") < settings.indexOf("\"Tu nivel de estudios\"") && settings.indexOf("\"Tu nivel de estudios\"") < settings.indexOf("\"Efectos de sonido\""))
    assertFalse("el Perfil ya no lo trae (B5.2)", profile.contains("Tu nivel de estudios") || profile.contains("education_"))
  }

  @Test
  fun `el nombre se edita en el Perfil y se guarda al salir del campo`() {
    val profile = src("ui/screens/ProfileScreen.kt")
    assertTrue(profile.contains("testTag(\"input_user_name\")") && profile.contains("viewModel.setName("))
    assertTrue("el nombre es texto grande con un botón «Cambiar» (Opus, 21-oct), no un campo siempre abierto", profile.contains("testTag(\"btn_change_name\")") && profile.contains("text = \"Cambiar\"") && profile.contains("testTag(\"profile_name\")"))
    assertTrue("al tocarlo, un campo con el rótulo «Tu nombre» y el teclado abierto", profile.contains("testTag(\"name_label\")") && profile.contains("requestFocus()"))
    assertTrue("se guarda con «Listo» del teclado, al salir del campo o al salir del Perfil", profile.contains("ImeAction.Done") && profile.contains("onFocusChanged") && profile.contains("onDispose"))
    assertTrue("el lector de pantalla dice «Tu nombre, Ricardo, botón Cambiar»", profile.contains("contentDescription = \"Tu nombre, \$shown, botón Cambiar\""))
    assertFalse("sin botón «Guardar»", profile.contains("\"Guardar\""))
  }

  @Test
  fun `las medallas viven en Tu y Avance solo dice cuantas hay`() {
    val progress = src("ui/screens/ProgressScreen.kt")
    val sections = src("ui/screens/AvanceSections.kt")
    assertTrue(progress.contains("AvanceMedalsLink(") && !progress.contains("AvanceMedals(") && !progress.contains("FamilyMedalDialog"))
    assertTrue(sections.contains("medallas · ver en Tú") && sections.contains("1 medalla · ver en Tú"))
    assertFalse("las tres tarjetas enteras ya no se repiten en Avance (B6.1)", sections.contains("internal fun AvanceMedals("))
    assertTrue("Tú sí las trae", src("ui/screens/ProfileScreen.kt").contains("FamilyMedalCards("))
  }

  @Test
  fun `las horas del aviso son horas y no dicen Mas tarde`() {
    val settings = src("ui/screens/SettingsScreen.kt")
    assertTrue(settings.contains("Triple(\"Tarde\", 15, 0)") && settings.contains("Triple(\"Al atardecer\", 19, 0)") && settings.contains("Triple(\"Noche\", 21, 0)"))
    assertFalse("«Más tarde» no es una hora (C3)", settings.contains("Más tarde"))
  }

  @Test
  fun `Licencias ya no cita Cosecha de palabras, que esta retirada`() {
    assertFalse(src("ui/screens/LicensesScreen.kt").contains("Cosecha"))
  }

  @Test
  fun `el boton de la ficha plegada dice Jugar a secas`() {
    val library = src("ui/screens/GameSheet.kt")
    assertTrue(library.contains("SheetPlayButton(choice, timed, supportsReto, detailed = opciones"))
    assertTrue("plegada: «Jugar» y nada más (C5: la línea de arriba ya dice lo elegido)", library.contains("KitButton(text = \"Jugar\", onClick = onPlay"))
  }

  @Test
  fun `no quedan textos muertos de la sesion, de la auto-adaptativa ni de la XP de dominio`() {
    val models = src("model/Models.kt")
    assertFalse(models.contains("Auto-adaptativa") || models.contains("enum class MasteryTier") || models.contains("class DomainMasteryInfo"))
    assertEquals("la dificultad guardada ya no lleva textos", 5, Regex("PRINCIPIANTE|INTERMEDIO|AVANZADO|CUSTOM|ADAPTIVE").findAll(models.substringAfter("enum class DifficultyMode").substringBefore("}")).count())
    val strings = src("ui/i18n/AppStrings.kt")
    for (dead in listOf("dailySessionTitle", "dailySessionDesc", "sessionCompletedTitle", "startDailySession", "continueDailySession")) assertFalse(dead, strings.contains(dead))
    assertFalse(File("src/main/java/com/example/ui/components/ProgressTrendChart.kt").exists())
    assertFalse(src("data/PlaceSheet.kt").contains("chosenLine"))
  }
}
