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

  /**
   * El noche PLANO de las páginas de información (Tarea 199, `#050823`): el fondo de la página «Contrarreloj» bajo su cabecera, donde el cielo (lámina + luceros + la luna difusa) se DESVANECE en 24 dp y no vuelve a asomar entre las losas.
   * Es el mismo tono con que se miden y se graban los finales de partida (las pruebas de captura los apoyan sobre este color) y queda a un paso del primer tono del cielo (`#07071F`), así que el desvanecido de la cabecera no se nota. En la app los finales en sí van sobre el cielo, no sobre este plano.
   */
  val NightFlat = Color(0xFF050823)

  /**
   * La «ventanilla noche» de los gráficos de la página «Contrarreloj» (Tarea 201, opción B de Fable, `auditoria-fable/nubi-plus-comparacion.md` §11.4): `#100F3A`, el tono del MEDIO del cielo único de la app (el mismo de `NeuroStyle.NightMid`, la carga de Unity y `GameCover`).
   * Lo calibrado sobre oscuro va en su ventanilla, dentro de la losa crema (regla del kit). Sobre ella: `OnNight` 15,6:1, `OnNightDim` ≥ 4,5:1, `SunOnNight` 14,9:1 y el coral de los errores ≈ 5,8:1; `ContrarrelojPageTest` los mide.
   */
  val NightWindow = Color(0xFF100F3A)

  // Superficies de Material (mismos valores que tenía el esquema)
  val Surface = Color(0xE6172058)
  val SurfaceVariant = Color(0xFF243078)
  val Outline = Color(0xFF4B5AA8)
  val OutlineVariant = Color(0xFF34418A)
}
