package com.example.ui.theme

import androidx.compose.ui.graphics.Color

/**
 * La paleta ÚNICA de la app (Tarea 123; Fable, `auditoria-fable/pulido-app-17-oct.md` §5 ítem 3): antes había tres fuentes (los colores de área de `Color.kt`, los de `Clay` y los del esquema de Material con que se armó la app en AI Studio) y siete copias
 * del texto secundario sobre el cielo. Ahora cada color vive aquí o en `Clay` (los colores del kit: tinta, coral, sol, cielo, uva y lima) y el resto los toma de uno de los dos. `Color.kt` se absorbió acá.
 *
 * - **Áreas:** el color de cada una ([Memoria]…[Lenguaje], antes `DomainMemoria`…; su versión aclarada para texto sobre el cielo, `onSky`, se borró en la Tarea 182: nadie la usaba). Las losas de plasticina de cada área salen de `slabColorFor` (`ClayKit`).
 * - **Texto sobre el cielo:** [OnNight] (títulos y cuerpo) y [OnNightDim] (secundario). El secundario eran DOS tonos (`#B4BFEA` y `#C7D0FF`) repartidos en 17 sitios; queda `#C7D0FF`, el más claro (12:1 sobre la mediana del fondo «Espacio profundo»): lo que era `#B4BFEA` sube un poco.
 * - **Amarillo para TEXTO sobre el cielo:** [SunOnNight] (Tarea 194): el sol de los botones (`Clay.Sun`, `#FFC93C`) es tan saturado que, como letra fina sobre el azul, vibra y cansa aunque su contraste numérico sea alto (Ricardo en el teléfono: «las letras amarillas se ven saturadas y no se pueden leer»).
 *   TODA letra amarilla directo sobre el cielo (el Primer vuelo, las medallas, los finales de partida, el resumen del día, la carga del juego…) usa este tono de mantequilla; el sol queda para los botones, las losas, los dibujos y lo que va sobre crema (`SunOnNightTest` falla si una letra vuelve a pedir `Clay.Sun`).
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

  // Texto sobre el cielo
  val OnNight = Color(0xFFEAF0FF)
  val OnNightDim = Color(0xFFC7D0FF)

  /** El amarillo claro y suave (mantequilla) del TEXTO amarillo sobre el cielo: 16,2:1 sobre `#07071F`, 14,9:1 sobre `#100F3A` y 12,9:1 sobre `#1C1B52` (los tres tonos del cielo; 15,2:1 sobre `#0E0D33`, el fondo de la app); `SunOnNightTest` lo mide. El sol de antes daba 12,9, 11,9, 10,3 y 12,2: el número ya era alto, lo que cansaba era la saturación. */
  val SunOnNight = Color(0xFFFFE6A6)

  // Superficies de Material (mismos valores que tenía el esquema)
  val Surface = Color(0xE6172058)
  val SurfaceVariant = Color(0xFF243078)
  val Outline = Color(0xFF4B5AA8)
  val OutlineVariant = Color(0xFF34418A)
}
