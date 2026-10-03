# Freno de Emergencia (`freno`) — ficha técnica

> Ficha técnica movida TAL CUAL desde `CLAUDE.md` el 2-oct (CLAUDE.md quedó como índice). Lo que manda es el código; esta ficha explica cómo y por qué.

**Freno de Emergencia** (`Games/Freno/`, id `freno`, dominio atención): tarea de señal de alto (Logan y Cowan, 1984;
consenso de Verbruggen et al., eLife 2019). Base de lanzamiento con 2-4 plataformas sobre la luna: se enciende un cohete
(baliza lima + botón lima) y se lanza tocando su carril (cualquier toque en la mitad de abajo; cuenta al PRESIONAR);
en ~30% aparece la señal ¡ALTO! (octágono coral con texto + sirena) un instante después y hay que no tocar.
- Reglas y pruebas: `BrakeContract` / `BrakeContractTests` (7). SSD en escalera (250 ms inicial, ±50, entre 50 y el
  límite − 150). DDA común solo para la tarea de ir: plataformas 2/3/4 (niveles 1-4/5-8/9-12) y tiempo para lanzar
  1400 → 800 ms (Precisión +300). 3 primeros sin alto, nunca más de 2 altos seguidos, espera previa 600-1200 ms
  (tocar antes = "¡Espera la luz!" y se repite sin contar). Si empieza a esperar el alto (últimos 6 lanzamientos 30% más
  lentos que los 6 primeros), aviso "¡No esperes al ALTO!". Reto 120 s; Precisión 40 lanzamientos. 60 cuadros/s.
- Enganche: cada despegue con llama, humo y temblor deja una estrella en el cielo; frenar da más puntos (más cuanto
  más tarde llegó el alto) y empuja el medidor "Límite del freno" (marca sol = récord, aviso "¡Nuevo límite!"); no
  frenar = el cohete da un salto y vuelve (sin choques).
- Medida propia: **tu freno** (SSRT por integración con reemplazo de omisiones; -1 con menos de 6 altos o
  p(responder|alto) fuera de 0,25-0,75, el criterio del consenso de Verbruggen et al., 2019; si no hay estimación, el
  final lo explica y pide lanzar sin esperar al ALTO). Viaja en `brake_ms / stops_ok / stops_total / brake_best_ssd_ms` →
  `GamePlayResult.brakeMs / stopsOk / stopsTotal / brakeBestSsdMs` → `GameResultScreen`: velocímetro de arcilla
  (450 ms lento → 150 ms rápido) + "Frenaste N de M altos · récord".
- Arte: `BrakeSprites` (alto, plataforma, botón), `GameWorld.LaunchBase`. Vista previa:
  `python3 tools/art-preview/freno.py <raw>` → `docs/previews/freno.png`. Probado por Ricardo (27-sep): "me gustó,
  funciona muy bien".


## Tutorial guiado, «Cómo se juega» y versión corta del inicio (3-oct, tarea 21a)

Para el inicio nuevo (`docs/diseno-inicio.md`). La pieza común es `Games/Shared/GuidedTutorial` (ficha en `docs/diseno-rastro-de-luz.md`, § Tutorial).

- **Tarjeta de Nubi**: «Freno de Emergencia» · «Lanza el cohete que se enciende. Si aparece ¡ALTO!, no toques.» · «Probar una ronda» / «Saltar tutorial».
- **Ronda guiada** (`BrakeContract.GuidedPlan`: 3 de ir y 1 con alto; entre `// <guided>` y `// </guided>` en `BrakeGameController`): cohetes lentos con un aro sol
  punteado sobre el botón del carril; los pasos de ir esperan sin límite (60 s) y el del alto dura 3,2 s. Avisos de Nubi: «Toca el cohete que se enciende» ·
  «Otra vez: toca el cohete que se enciende» · «Ahora la otra regla: si aparece la señal ¡ALTO!, no toques» · «¡ALTO! Este no: déjalo quieto» · «¡Frenaste a
  tiempo!» · errores sin culpa: «Casi: se toca el cohete que se enciende. Mira otra vez» / «Casi: con el ¡ALTO! el cohete se queda quieto. Probemos otra vez» ·
  cierre «¡Así se juega! Ahora sin ayuda». Un error repite el MISMO paso. No suma puntos, no toca el DDA, las rachas ni el SSRT y no se guarda.
- **«Cómo se juega» desde la pausa** (`PauseMenu`, cuarto botón): descarta el lanzamiento en curso, corre tarjeta + ronda guiada con el reloj andando y vuelve a la partida
  (`HowToSuspend` → `HowToFlow` → `HowToResume`: las anclas de tiempo se corren lo que duró el tutorial; el Reto no pierde segundos).
- **Versión corta** (`Assessment.Active`): `BrakeContract.AssessmentTrials` = **24 lanzamientos** (≈ 70 s, sin reloj), con 8 altos en posiciones fijas
  (`AssessmentStop`: nunca en los 3 primeros, máximo 2 seguidos). Arranque suave (`CreateEngine`: nivel 1 en mayores, 3 en el resto, sin tiempo de reacción).
  `end_rating` normal. **Primer dato** (se lee en la app, `data/FirstData.kt`): «Frenaste a tiempo N de M veces.» (con ≥ 4 altos), de `stops_ok`/`stops_total`.
- Los cohetes llevan ¡ALTO! (octágono): no son «naves».
- Pruebas: `FrenoTutorialTests` (plan, calendario de altos, arranque suave, rating normalizado), `GuidedTutorialTests` (guion, reloj, invariante de la ronda guiada).
