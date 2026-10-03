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
