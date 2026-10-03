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
