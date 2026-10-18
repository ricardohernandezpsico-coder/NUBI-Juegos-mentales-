package com.example.ui.theme

import androidx.compose.ui.graphics.Color
import androidx.compose.ui.graphics.lerp

/**
 * La paleta ÚNICA de la app (Tarea 123; Fable, `auditoria-fable/pulido-app-17-oct.md` §5 ítem 3): antes había tres fuentes (los colores de área de `Color.kt`, los de `Clay` y los del esquema de Material con que se armó la app en AI Studio) y siete copias
 * del texto secundario sobre el cielo. Ahora cada color vive aquí o en `Clay` (los colores del kit: tinta, coral, sol, cielo, uva y lima) y el resto los toma de uno de los dos. `Color.kt` se absorbió acá.
 *
 * - **Áreas:** el color de cada una ([Memoria]…[Lenguaje], antes `DomainMemoria`…) y su versión aclarada para texto sobre el cielo ([onSky]; antes `domainTextColor`, 45 % hacia el blanco). Las losas de plasticina de cada área salen de `slabColorFor` (`ClayKit`).
 * - **Texto sobre el cielo:** [OnNight] (títulos y cuerpo) y [OnNightDim] (secundario). El secundario eran DOS tonos (`#B4BFEA` y `#C7D0FF`) repartidos en 17 sitios; queda `#C7D0FF`, el más claro (12:1 sobre la mediana del fondo «Espacio profundo»): lo que era `#B4BFEA` sube un poco.
 * - **Marca y estado:** [Brand], [Success] y [Neutral] (los que estaban sueltos en `Color.kt`; los usan los gráficos y Ajustes).
 * - **Superficies del esquema de Material** ([Surface], [SurfaceVariant], [Outline], [OutlineVariant]): con los mismos valores de antes (`Theme.kt`).
 */
object NubiTokens {
  // Marca y estado (antes TealPrimary, EmeraldAccent y Slate600 de Color.kt)
  val Brand = Color(0xFF5B9BFF)         // azul eléctrico de la marca
  val Success = Color(0xFF10B981)       // verde esmeralda: mejora, acierto
  val Neutral = Color(0xFF475569)       // pizarra: sin cambio

  // Áreas (antes DomainMemoria… de Color.kt)
  val Memoria = Color(0xFF3B82F6)
  val Atencion = Color(0xFFF59E0B)
  val Razonamiento = Color(0xFF8B5CF6)
  val Lenguaje = Color(0xFF10B981)

  /** Cuánto se aclara el color de un área para leerse como texto sobre el cielo (hacia el blanco). */
  const val ON_SKY_LIGHTEN = 0.45f

  /** El color de un área aclarado para texto sobre el cielo (contraste ≥ 4,5:1 sobre el fondo; `BoardBackgroundContrastTest` lo mide). */
  fun onSky(area: Color): Color = lerp(area, Color.White, ON_SKY_LIGHTEN)

  // Texto sobre el cielo
  val OnNight = Color(0xFFEAF0FF)
  val OnNightDim = Color(0xFFC7D0FF)

  // Superficies de Material (mismos valores que tenía el esquema)
  val Surface = Color(0xE6172058)
  val SurfaceVariant = Color(0xFF243078)
  val Outline = Color(0xFF4B5AA8)
  val OutlineVariant = Color(0xFF34418A)
}
