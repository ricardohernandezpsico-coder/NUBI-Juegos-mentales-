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

private val Base = Typography()

// Títulos y encabezados: siempre Fredoka. Cuerpo y etiquetas: AppFamily (Nunito, o Fredoka si van en negrita), con
// los tamaños chicos subidos para que se lean bien (mínimo 13-14 sp).
val Typography = Typography(
  displayLarge = Base.displayLarge.copy(fontFamily = FredokaFamily),
  displayMedium = Base.displayMedium.copy(fontFamily = FredokaFamily),
  displaySmall = Base.displaySmall.copy(fontFamily = FredokaFamily),
  headlineLarge = Base.headlineLarge.copy(fontFamily = FredokaFamily),
  headlineMedium = Base.headlineMedium.copy(fontFamily = FredokaFamily),
  headlineSmall = Base.headlineSmall.copy(fontFamily = FredokaFamily),
  titleLarge = Base.titleLarge.copy(fontFamily = FredokaFamily),
  titleMedium = Base.titleMedium.copy(fontFamily = FredokaFamily),
  titleSmall = Base.titleSmall.copy(fontFamily = FredokaFamily),
  bodyLarge = Base.bodyLarge.copy(fontFamily = AppFamily),
  bodyMedium = Base.bodyMedium.copy(fontFamily = AppFamily, fontSize = 15.sp, lineHeight = 21.sp),
  bodySmall = Base.bodySmall.copy(fontFamily = AppFamily, fontSize = 14.sp, lineHeight = 19.sp),
  labelLarge = Base.labelLarge.copy(fontFamily = AppFamily),
  labelMedium = Base.labelMedium.copy(fontFamily = AppFamily, fontSize = 14.sp, lineHeight = 18.sp),
  labelSmall = Base.labelSmall.copy(fontFamily = AppFamily, fontSize = 13.sp, lineHeight = 17.sp)
)
