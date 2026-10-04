# Juegos descartados

> Ficha técnica movida TAL CUAL desde `CLAUDE.md` el 2-oct (CLAUDE.md quedó como índice). Lo que manda es el código; esta ficha explica cómo y por qué.

**Constelación de Palabras (DESCARTADA, 28-sep)**: fluidez verbal por voz (reconocedor de Android, puntuación de
Troyer: agrupar y saltar). Funcionaba, pero a Ricardo no le convenció: la voz no se sentía fluida (palabras que no
reconocía, pausas), incluso tras la "opción A" (escucha continua, lista al reconocedor, parecidos al oído). Se sacó
todo, también el permiso de micrófono. El código completo queda en el historial de git (commit `caf19b8`: juego Unity,
`SpeechBridge.kt`, prueba de voz, léxico de ≈700 palabras). Lección para lenguaje: nada de voz; interacción táctil y fluida.

**Primer Contacto (DESCARTADO, 28-sep)**: aprender palabras de un idioma extraterrestre deduciéndolas de escena en escena
(aprendizaje entre situaciones, Yu y Smith 2007), con voz sintetizada propia (`NuriVoice`, formantes) y diccionario que
crecía día a día. Ricardo: la voz y la fluidez bien, pero "poco entendible"; ni con guía de la primera vez y pistas le
convenció ("una persona que no lo entienda no lo vuelve a jugar"). Código en el historial de git (commit `20eb246`).
Lección (tras Constelación y Primer Contacto): nada de juegos "pesados" de explicar; lo que funciona es lo de Piloto
Estelar: movimiento continuo, se entiende al instante, enganche inmediato. Queda de esa etapa: `Toast.FitSize` cuenta
los renglones reales (los avisos largos ya no se salen del recuadro, en todos los juegos).

**Cambio de Chip (RETIRADO, 3-oct-2026)**, id `cambiochip` (Atención, «flexibilidad cognitiva»): una ficha con una nave de arcilla y la regla del cartel, «hacia dónde APUNTA la nave» (DIRECCIÓN)
o «en qué borde ESTÁ» (POSICIÓN). Razones (Ricardo): (1) Tinta o Palabra «Dos orillas» ya entrena cambiar de regla (`docs/diseno-tinta-o-palabra.md`); (2) la mecánica se parece al
juego de las hojas de Lumosity; (3) se probó una alternativa de regla escondida, tipo Wisconsin, en dos bocetos y a Ricardo le resultó frustrante y sin enganche: descartada. Atención
pasó de 7 a 6 juegos y la app, de 23 a 22.
- **El id `cambiochip` queda RESERVADO** (no se reutiliza para otro juego): `GameRegistry.retiredDomains`.
- **Datos de quien ya lo jugó: NO se borran ni se migran.** Sus partidas y su progreso siguen en Room (`game_results`, `game_progress`) y en las preferencias (`skill`, `progress_log`); la app no
  los muestra en ningún lado (todo lo que se ve sale de `GameRegistry.allGames` / `getById`, null para un id retirado; el historial reciente y la gráfica de Avance los filtran) y no rompen
  nada. Cuentan: la racha y el total de partidas (son días y partidas jugadas). No cuentan: «Explorador» (9 juegos distintos del registro), las ligas de juego más alta (`bestGameRating` ignora
  los retirados), la liga general (promedio de los 22) ni el avance del área Atención (sus 6 juegos). Un camino de hoy guardado antes del retiro que lo nombraba se corrige solo: ese lugar pasa a otro
  juego de Atención (`NeuroVidaRepository.withoutRetiredGames`). Prueba: `flow/RetiredGameTest`.
- **Código**: se borró `Games/CambioChip/` (controlador, contrato `ChipContract`, pruebas). La nave de arcilla (`ChipShipSprite`) la usaban Piloto y Correo: se mudó a `Games/Piloto/PilotShipSprite.cs`
  (solo mirando arriba). `Shared/RuleBadgeSprite` y `Shared/ClayArrowSprite` solo las usaban Cambio de Chip (y Tinta o Palabra antes del rediseño): sin uso, se borraron. El código completo queda en el
  historial de git (último commit con el juego: `4104ec5`/`fa8ae5a` y anteriores).

**Comparación Instantánea (RETIRADA, 4-oct-2026)**, id `comparacion` (Atención, «velocidad perceptiva»): elegir la tarjeta que vale más (puntos, el número mayor o la cuenta de mayor resultado). Razones (Ricardo):
(1) se parece al juego de las dos pizarras de Lumosity (elegir el número o la cuenta que vale más); (2) repite las cuentas de Cálculo Sereno y de Aterrizaje Lunar; (3) la alternativa que se probó, «¿Dónde hay más?»
(sentido aproximado del número: por un instante aparecen dos grupos de luces y se elige cuál tiene más; boceto jugable guardado en `docs/previews/donde-hay-mas-boceto.html`, idea DESCARTADA), no convenció.
Atención pasó de 6 a 5 juegos (Tinta o Palabra, Freno, Piloto, Satélites, Rescate relámpago) y la app, de 22 a 21.
- **El id `comparacion` queda RESERVADO** (no se reutiliza): `GameRegistry.retiredDomains` (igual que `cambiochip`).
- **Datos de quien ya lo jugó: NO se borran ni se migran** (mismo tratamiento que Cambio de Chip): las partidas y el progreso siguen en Room y en `skill` pero no se muestran ni cuentan para «Explorador», las ligas más altas,
  la liga general (promedio de los 21) ni el área Atención (sus 5 juegos); la racha y el total de partidas sí cuentan esos días, y un camino de hoy guardado que lo nombraba se corrige solo (`withoutRetiredGames`).
  Pruebas: `flow/RetiredComparacionTest` y `flow/RetiredComparacionScreensTest` (además de las de Cambio de Chip).
- **Código**: se borró `Games/Comparacion/` (controlador, contrato `ComparisonContract`, `CountStarSprite` y pruebas); `CountStarSprite` solo lo usaba este juego y la vista previa `tools/art-preview`. El mundo de fondo
  `GameWorld.PlanetDuel` («Duelo de planetas») queda sin uso, disponible para otro juego, como `Orbits`. El código completo queda en el historial de git (último commit con el juego: `732e8e3` y anteriores).

**Detective de Series (RETIRADO, 4-oct-2026)**, id `series` (Razonamiento, «lógica secuencial»): descubrir la regla de una serie de números (sumas, cuadrados, cubos, primos…) y elegir el que sigue. Razones (Ricardo):
(1) las series de números intimidan y parecen un examen; (2) repetía números con Carga exacta y Aterrizaje Lunar; (3) la alternativa con figuras (planetas con lunas, anillo, satélite y color: una regla que se descubre mirando)
no convenció; el boceto jugable queda guardado en `docs/previews/series-figuras-boceto.html` (idea DESCARTADA). Razonamiento pasó de 5 a 4 juegos (Carga exacta, Aterrizaje Lunar, Acoplamiento y Tráfico Estelar) y la app, de 21 a 20.
Para volver a 5 después de publicar: la idea «Código secreto» de `docs/ideas-guardadas.md`.
- **El id `series` queda RESERVADO** (no se reutiliza): `GameRegistry.retiredDomains` (igual que `cambiochip` y `comparacion`).
- **Datos de quien ya lo jugó: NO se borran ni se migran**: las partidas y el progreso siguen en Room y en `skill` pero no se muestran ni cuentan para «Explorador», las ligas más altas, la liga general (promedio de los 20)
  ni el área Razonamiento (sus 4 juegos); la racha y el total de partidas sí cuentan esos días, y un camino de hoy guardado que lo nombraba se corrige solo (`withoutRetiredGames`).
  Pruebas: `flow/RetiredGameTest` (el id retirado, su área, racha y logros) además de las de Cambio de Chip y Comparación.
- **Código**: se borró `Games/Series/` (controlador, contrato `SeriesContract`, `MagnifierSprite` —solo lo usaban este juego y la vista previa `tools/art-preview`— y pruebas). El mundo de fondo `GameWorld.MeteorShower`
  («lluvia de meteoros a ritmo regular»; NO es el juego «Lluvia de meteoros») queda sin uso, disponible para otro juego. El código completo queda en el historial de git (último commit con el juego: `f523b23` y anteriores).

**Ruta del Tesoro (RETIRADA, 4-oct-2026)**, id `rutatesoro` (Memoria, «memoria visoespacial»): unas casillas se iluminan en una cuadrícula, se apagan, se tocan las recordadas y la cuadrícula crece. Razones (Ricardo):
(1) esa mecánica es prácticamente «Memory Matrix» de Lumosity, el riesgo legal más alto que quedaba (ver `docs/nombre-marca-y-riesgos.md`); (2) repite lo que ya hacen Rastro de luz (posiciones) y Parejas Ocultas
(dónde estaba algo en una grilla). No hay boceto alternativo. Memoria pasó de 6 a 5 juegos (Parejas, Rastro de luz, Bitácora, Rumbo a Casa y Correo Estelar) y la app, de 20 a 19.
- **El id `rutatesoro` queda RESERVADO** (no se reutiliza): `GameRegistry.retiredDomains`.
- **Datos de quien ya lo jugó: NO se borran ni se migran**: las partidas y el progreso siguen en Room y en `skill` pero no se muestran ni cuentan para «Explorador», las ligas más altas, la liga general (promedio de los 19)
  ni el área Memoria (sus 5 juegos); la racha y el total de partidas sí cuentan esos días, y un camino de hoy guardado que lo nombraba se corrige solo (`withoutRetiredGames`).
  Pruebas: `flow/RetiredGameTest` (el id retirado, su área, racha y logros) además de las de los otros retirados.
- **No estaba** en «Primer vuelo» ni en el punto de partida (`BaselinePlan`: Rastro, Freno, Aterrizaje y Meteoros), así que el inicio no cambia.
- **Código**: se borró `Games/RutaTesoro/` (controlador, contrato `TreasureContract`, `TreasureSprites` —cristales, meteoritos…; solo lo usaban este juego, `SymbolPreviewExporter` y la vista previa `tools/art-preview`— y pruebas),
  la lámina `arte-juegos.png` y `tools/art-preview/juegos.py` (era lo único que quedaba de ella). El mundo de fondo `GameWorld.TreasureMoon` («Luna del tesoro») queda sin uso, disponible para otro juego.
  `Skill.ownTarget` (objetivo propio de aciertos) queda sin ningún juego que lo use, pero disponible. El código completo queda en el historial de git (último commit con el juego: `4c395fb` y anteriores).

