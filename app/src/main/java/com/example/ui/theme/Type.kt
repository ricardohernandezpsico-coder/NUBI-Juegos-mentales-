package com.example.ui.theme

import androidx.compose.material3.Typography
import androidx.compose.ui.text.ExperimentalTextApi
import androidx.compose.ui.text.font.Font
import androidx.compose.ui.text.font.FontFamily
import androidx.compose.ui.text.font.FontVariation
import androidx.compose.ui.text.font.FontWeight
import androidx.compose.ui.unit.sp
import com.example.R

/**
 * Tipografia de NeuroVida: Fredoka (OFL), redondeada, la misma familia visual de los juegos. Es una fuente
 * variable: cada peso pide su variacion (en Android < 8 cae al peso por defecto). Fredoka llega a 700, asi que
 * ExtraBold/Black se mapean a 700.
 */
@OptIn(ExperimentalTextApi::class)
private fun fredoka(weight: FontWeight, variation: Int) = Font(
  resId = R.font.fredoka,
  weight = weight,
  variationSettings = FontVariation.Settings(FontVariation.weight(variation))
)

@OptIn(ExperimentalTextApi::class)
val FredokaFamily = FontFamily(
  fredoka(FontWeight.Light, 300),
  fredoka(FontWeight.Normal, 400),
  fredoka(FontWeight.Medium, 500),
  fredoka(FontWeight.SemiBold, 600),
  fredoka(FontWeight.Bold, 700),
  fredoka(FontWeight.ExtraBold, 700),
  fredoka(FontWeight.Black, 700)
)

/**
 * La familia de TODA la app: elige la fuente por el peso. Normal y medio = Nunito (subtítulos, comentarios, texto
 * secundario); seminegrita y negrita = Fredoka (títulos, encabezados, botones, números). Así la regla se cumple en
 * cada Text sin tocarlo uno por uno.
 */
@OptIn(ExperimentalTextApi::class)
val AppFamily = FontFamily(
  Font(R.font.nunito_regular, FontWeight.Light),
  Font(R.font.nunito_regular, FontWeight.Normal),
  Font(R.font.nunito_medium, FontWeight.Medium),
  fredoka(FontWeight.SemiBold, 600),
  fredoka(FontWeight.Bold, 700),
  fredoka(FontWeight.ExtraBold, 700),
  fredoka(FontWeight.Black, 700)
)

/**
 * La familia de los TÍTULOS de toda la app Kotlin (Tarea 175, 25-oct; Ricardo la probó en Avance —Tarea 174: «me convence»— y la aprobó para el resto): Rubik Bold (700, OFL; `res/font/rubik_bold.ttf`). Un título es el texto que NOMBRA una pantalla, una sección, una tarjeta o una lámina (el saludo de Hoy,
 * «Tu ruta de hoy», «Todos los juegos», el nombre del juego en la ficha, «Sobre este juego», los títulos de Tú, Ajustes y Licencias, del final de la partida, del resumen de la ruta, del Primer vuelo y de los diálogos). NO la usan los botones de arcilla (siguen en Fredoka), las cifras grandes («668»), los chips, las etiquetas,
 * las pestañas de abajo ni el cuerpo (Nunito): por eso [AppFamily] NO cambia (elige por peso y arrastraría a los botones) y los títulos la piden a mano o por los estilos `headline` y `title` de [Typography]. Los juegos de Unity no se tocan.
 */
val TitleFamily = FontFamily(Font(R.font.rubik_bold, FontWeight.Bold), Font(R.font.rubik_bold, FontWeight.SemiBold))

private val Base = Typography()

// Títulos y encabezados: Rubik Bold ([TitleFamily], desde la Tarea 175; antes Fredoka). Cuerpo y etiquetas: AppFamily (Nunito, o Fredoka si van en negrita), con
// los tamaños chicos subidos para que se lean bien (mínimo 13-14 sp).
val Typography = Typography(
  displayLarge = Base.displayLarge.copy(fontFamily = TitleFamily),
  displayMedium = Base.displayMedium.copy(fontFamily = TitleFamily),
  displaySmall = Base.displaySmall.copy(fontFamily = TitleFamily),
  headlineLarge = Base.headlineLarge.copy(fontFamily = TitleFamily),
  headlineMedium = Base.headlineMedium.copy(fontFamily = TitleFamily),
  headlineSmall = Base.headlineSmall.copy(fontFamily = TitleFamily),
  titleLarge = Base.titleLarge.copy(fontFamily = TitleFamily),
  titleMedium = Base.titleMedium.copy(fontFamily = TitleFamily),
  titleSmall = Base.titleSmall.copy(fontFamily = TitleFamily, fontSize = 16.sp, lineHeight = 22.sp),
  bodyLarge = Base.bodyLarge.copy(fontFamily = AppFamily),
  bodyMedium = Base.bodyMedium.copy(fontFamily = AppFamily, fontSize = 16.sp, lineHeight = 22.sp),
  bodySmall = Base.bodySmall.copy(fontFamily = AppFamily, fontSize = 16.sp, lineHeight = 22.sp),
  labelLarge = Base.labelLarge.copy(fontFamily = AppFamily, fontSize = 16.sp, lineHeight = 22.sp),
  labelMedium = Base.labelMedium.copy(fontFamily = AppFamily, fontSize = 16.sp, lineHeight = 21.sp),
  labelSmall = Base.labelSmall.copy(fontFamily = AppFamily, fontSize = 16.sp, lineHeight = 21.sp)
)
