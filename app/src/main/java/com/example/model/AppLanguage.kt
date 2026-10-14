package com.example.model

/**
 * Los idiomas que la app conoce. El nombre va en su propia lengua y SIN bandera (nada de emojis).
 * [translated] dice si la app está traducida a ese idioma: HOY SOLO EL ESPAÑOL. `strings` (ui/i18n/AppStrings.kt) trae textos de Ajustes, nombres de áreas y de algunos juegos en los otros cuatro, pero el resto de la app (Hoy, Juegos, Avance, los finales,
 * el Primer vuelo, los juegos de Unity) está escrito en español: elegir otro idioma dejaba una app a medias. Por eso solo se ofrecen los [translated] y, mientras sea uno solo, Opciones no muestra el selector. Ricardo decide cuándo se traduce.
 */
enum class AppLanguage(
  val code: String,
  val displayName: String,
  val translated: Boolean
) {
  SPANISH("es", "Español", true),
  ENGLISH("en", "English", false),
  FRENCH("fr", "Français", false),
  GERMAN("de", "Deutsch", false),
  PORTUGUESE("pt", "Português", false);

  companion object {
    /** Los que se pueden elegir: los traducidos. */
    val selectable: List<AppLanguage> get() = entries.filter { it.translated }

    /** Un código guardado → su idioma; uno desconocido, o de un idioma sin traducir (quedó guardado cuando el selector lo ofrecía), vuelve al español. */
    fun fromCode(code: String): AppLanguage {
      return entries.find { it.code.equals(code, ignoreCase = true) && it.translated } ?: SPANISH
    }
  }
}
